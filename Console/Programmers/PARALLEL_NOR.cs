using FlashMemory;
using System;
using System.Collections.Generic;
using USB;



public class PARALLEL_NOR : MemoryDeviceUSB {
    private FCUSB_DEVICE FCUSB;
    public event MemoryDeviceUSB.PrintConsoleEventHandler PrintConsole;
    public event MemoryDeviceUSB.SetProgressEventHandler SetProgress;

    public P_NOR MyFlashDevice { get; set; }
    public DeviceStatus MyFlashStatus { get; set; } = DeviceStatus.NotDetected;
    public NOR_CFI CFI { get; set; } // Contains CFI table information (NOR)
    public MEM_PROTOCOL MyAdapter { get; set; } // This is the kind of socket adapter connected and the mode it is in
    public bool DUALDIE_EN { get; set; } = false; // Indicates two DIE are connected and using a CE
    public int DUALDIE_CE2 { get; set; } // The Address pin that goes to the second chip-enable
    public int DIE_SELECTED { get; set; } = 0;

    private FlashDetectResult FLASH_IDENT;

    public PARALLEL_NOR(FCUSB_DEVICE parent_if) {
        SetProgress?.Invoke(0);
        FCUSB = parent_if;
    }

    public bool DeviceInit() {
        DIE_SELECTED = 0;
        DUALDIE_EN = false;
        MyFlashDevice = null;
        CFI = null;
        if (!this.EXPIO_SETUP_USB(MEM_PROTOCOL.SETUP)) {
            PrintConsole?.Invoke("Parallel I/O failed to initialize");
            MyFlashStatus = DeviceStatus.ExtIoNotConnected;
            return false;
        } else {
            PrintConsole?.Invoke("Parallel mode successfully initialized");
        }
        if (DetectFlashDevice()) {
            string chip_id_str = (FLASH_IDENT.MFG).ToString("X").PadLeft(2, '0') + (FLASH_IDENT.PART).ToString("X").PadLeft(8, '0');
            PrintConsole?.Invoke(string.Format("Connected to Flash (CHIP ID: 0x{0})", chip_id_str));
            if (FCUSB.HWBOARD == FCUSB_BOARD.XPORT_PCB2) {
                if ((int)(FLASH_IDENT.ID1 >> 8) == 255)
                    FLASH_IDENT.ID1 = (ushort)((int)FLASH_IDENT.ID1 & 255);
            }
            Device[] device_matches;
            if (MyAdapter == MEM_PROTOCOL.NOR_X8) {
                device_matches = MainApp.FlashDatabase.FindDevices(FLASH_IDENT.MFG, FLASH_IDENT.ID1, 0, MemoryType.PARALLEL_NOR);
            } else {
                device_matches = MainApp.FlashDatabase.FindDevices(FLASH_IDENT.MFG, FLASH_IDENT.ID1, FLASH_IDENT.ID2, MemoryType.PARALLEL_NOR);
            }
            CFI = new NOR_CFI(EXPIO_NOR_GetCFI());
            if (device_matches is object && device_matches.Length > 0) {
                MyFlashDevice_SelectBest(device_matches);
                PrintConsole?.Invoke(string.Format("Flash detected: {0} ({1} bytes)", MyFlashDevice.NAME, MyFlashDevice.FLASH_SIZE.ToString("N0")));
                PrintConsole?.Invoke("Programming mode: Parallel I/O");
                PrintDeviceInterface();
                if (MainApp.MySettings.MULTI_CE > 0) {
                    PrintConsole?.Invoke("Multi-chip select feature is enabled");
                    DUALDIE_EN = true;
                    DUALDIE_CE2 = MainApp.MySettings.MULTI_CE;
                } else if (MyFlashDevice.DUAL_DIE) { // This IC package has two-die with CE access
                    DUALDIE_EN = true;
                    DUALDIE_CE2 = MyFlashDevice.CE2;
                }
                if (MyFlashDevice.RESET_ENABLED)
                    ResetDevice(); // This is needed for some devices
                this.EXPIO_SETUP_WRITEDELAY(MyFlashDevice.HARDWARE_DELAY);
                this.EXPIO_SETUP_DELAY(MyFlashDevice.DELAY_MODE);
                switch (MyFlashDevice.WriteMode) {
                    case MFP_PRG.Standard: {
                            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Standard);
                            break;
                        }
                    case MFP_PRG.IntelSharp: {
                            CURRENT_SECTOR_ERASE = E_EXPIO_SECTOR.Intel;
                            CURRENT_CHIP_ERASE = E_EXPIO_CHIPERASE.Intel;
                            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Intel);
                            break;
                        }
                    case MFP_PRG.BypassMode: { // Writes 64 bytes using ByPass sequence
                            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Bypass);
                            break;
                        }
                    case MFP_PRG.PageMode: { // Writes an entire page of data (128 bytes etc.)
                            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Page);
                            break;
                        }
                    case MFP_PRG.Buffer1: { // Writes to a buffer that is than auto-programmed
                            CURRENT_SECTOR_ERASE = E_EXPIO_SECTOR.Intel;
                            CURRENT_CHIP_ERASE = E_EXPIO_CHIPERASE.Intel;
                            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Buffer_1);
                            break;
                        }
                    case MFP_PRG.Buffer2: {
                            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Buffer_2);
                            break;
                        }
                }
                WaitUntilReady();
                EXPIO_PrintCurrentWriteMode();
                Utilities.Sleep(10); // We need to wait here (device is being configured)
                MyFlashStatus = DeviceStatus.Supported;
                return true;
            } else {
                MyFlashDevice = null;
                MyFlashStatus = DeviceStatus.NotSupported;
            }
        } else {
            MyFlashStatus = DeviceStatus.NotDetected;
        }
        return false;
    }

    private void MyFlashDevice_SelectBest(Device[] device_matches) {
        if (device_matches.Length == 1) {
            MyFlashDevice = (P_NOR)device_matches[0];
            return;
        }
        if ((int)device_matches[0].MFG_CODE == 0x1 && (int)device_matches[0].ID1 == 0xAD) { // AM29F016x (we need to figure out which one)
            if (!CFI.IS_VALID) {
                MyFlashDevice = (P_NOR)device_matches[0];
                return; // AM29F016B (Uses Legacy programming)
            } else {
                MyFlashDevice = (P_NOR)device_matches[1];
                return;
            } // AM29F016D (Uses Bypass programming)
        } else if (CFI.IS_VALID) {
            var flash_dev = new List<Device>();
            for (int i = 0, loopTo = device_matches.Length - 1; i <= loopTo; i++) {
                if ((uint)device_matches[i].FLASH_SIZE == CFI.DEVICE_SIZE) {
                    flash_dev.Add(device_matches[i]);
                }
            }
            if (flash_dev.Count == 1) {
                MyFlashDevice = (P_NOR)flash_dev[0];
                return;
            } else {
                for (int i = 0, loopTo1 = flash_dev.Count - 1; i <= loopTo1; i++) {
                    if ((long)flash_dev[i].PAGE_SIZE == (long)CFI.WRITE_BUFFER_SIZE) {
                        MyFlashDevice = (P_NOR)flash_dev[i];
                        return;
                    }
                }
            }
        }
        if (MyFlashDevice is null)
            MyFlashDevice = (P_NOR)device_matches[0];
    }

    public Device GetDevice {
        get {
            return this.MyFlashDevice;
        }
    }

    public string DeviceName {
        get {
            switch (MyFlashStatus) {
                case DeviceStatus.Supported: {
                        return MyFlashDevice.NAME;
                    }
                case DeviceStatus.NotSupported: {
                        return (MyFlashDevice.MFG_CODE).ToString("X").PadLeft(2, '0') + " " + (MyFlashDevice.ID1).ToString("X").PadLeft(4, '0');
                    }
                default: {
                        return "No Flash Detected";
                    }
            }
        }
    }

    public long DeviceSize {
        get {
            long FLASH_SIZE = MyFlashDevice.FLASH_SIZE;
            if (DUALDIE_EN)
                return FLASH_SIZE * 2L;
            return FLASH_SIZE;
        }
    }

    public byte[] ReadData(long logical_address, long data_count) {
        if (DUALDIE_EN) {
            var data_to_read = new byte[(int)(data_count - 1L + 1)];
            uint buffer_size = 0U;
            uint array_ptr = 0U;
            while (data_count != 0L) {
                uint argflash_offset = (uint)logical_address;
                uint argcount = (uint)data_count;
                uint die_address = GetAddressForMultiDie(ref argflash_offset, ref argcount, ref buffer_size);
                var die_data = ReadBulk(die_address, buffer_size);
                if (die_data is null)
                    return null;
                Array.Copy(die_data, 0L, data_to_read, array_ptr, die_data.Length);
                array_ptr += buffer_size;
            }
            return data_to_read;
        } else {
            return ReadBulk((uint)logical_address, (uint)data_count);
        }
    }
    // Returns the die address from the flash_offset (and increases by the buffersize) and also selects the correct die
    private uint GetAddressForMultiDie(ref uint flash_offset, ref uint count, ref uint buffer_size) {
        uint die_size = (uint)MyFlashDevice.FLASH_SIZE;
        byte die_id = (byte)Math.Floor(flash_offset / (double)die_size);
        uint die_addr = flash_offset % die_size;
        buffer_size = Math.Min(count, die_size - die_addr);
        if (die_id != DIE_SELECTED) {
            if (die_id == 0) {
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_ADDRESS_CE, null, 0U);
            } else {
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_ADDRESS_CE, null, (uint)DUALDIE_CE2);
            }
            DIE_SELECTED = die_id;
        }
        count -= buffer_size;
        flash_offset += buffer_size;
        return die_addr;
    }

    public bool SectorErase(uint sector_index) {
        if (!MyFlashDevice.ERASE_REQUIRED)
            return true;
        try {
            if (sector_index == 0L && SectorSize(0U) == MyFlashDevice.FLASH_SIZE) {
                return EraseDevice(); // Single sector, must do a full chip erase instead
            } else {
                uint Logical_Address = 0U;
                if (sector_index > 0L) {
                    for (uint i = 0U, loopTo = (uint)(sector_index - 1L); i <= loopTo; i++) {
                        uint s_size = SectorSize(i);
                        Logical_Address += s_size;
                    }
                }
                EXPIO_VPP_ENABLE(); // Enables +12V for supported devices
                uint sector_start_addr = Logical_Address;
                if (DUALDIE_EN) {
                    uint argcount = 0U;
                    uint argbuffer_size = 0U;
                    sector_start_addr = GetAddressForMultiDie(ref Logical_Address, ref argcount, ref argbuffer_size);
                }
                EXPIO_EraseSector(sector_start_addr);
                EXPIO_VPP_DISABLE();
                if (MyFlashDevice.DELAY_MODE == MFP_DELAY.SR1 | MyFlashDevice.DELAY_MODE == MFP_DELAY.SR2 | MyFlashDevice.DELAY_MODE == MFP_DELAY.RYRB) {
                    FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WAIT); // Calls the assigned WAIT function (uS, mS, SR, DQ7)
                    FCUSB.USB_WaitForComplete(); // Checks for WAIT flag to clear
                } else {
                    Utilities.Sleep((int)MyFlashDevice.ERASE_DELAY);
                } // Some flashes (like MX29LV040C) need more than 100ms delay
                bool blank_result = false;
                uint timeout = 0U;
                while (!blank_result) {
                    if (MyFlashDevice.RESET_ENABLED)
                        ResetDevice();
                    blank_result = BlankCheck(Logical_Address);
                    timeout = (uint)(timeout + 1L);
                    if (timeout == 10L)
                        return false;
                    if (!blank_result)
                        Utilities.Sleep(100);
                }
                return true;
            }
        } catch {
            return false;
        }
    }

    public bool WriteData(long logical_address, byte[] data_to_write, WriteParameters Params = null) {
        try {
            EXPIO_VPP_ENABLE();
            bool ReturnValue;
            uint DataToWrite = (uint)data_to_write.Length;
            uint PacketSize = 8192U; // Possibly /2 for IsFlashX8Mode
            int Loops = (int)Math.Ceiling(DataToWrite / (double)PacketSize); // Calcuates iterations
            for (int i = 0, loopTo = Loops - 1; i <= loopTo; i++) {
                int BufferSize = (int)DataToWrite;
                if (BufferSize > PacketSize)
                    BufferSize = (int)PacketSize;
                var data = new byte[BufferSize];
                Array.Copy(data_to_write, i * PacketSize, data, 0L, data.Length);
                if (DUALDIE_EN) {
                    uint argflash_offset = (uint)logical_address;
                    uint argcount = 0U;
                    uint argbuffer_size = 0U;
                    uint die_address = GetAddressForMultiDie(ref argflash_offset, ref argcount, ref argbuffer_size);
                    ReturnValue = WriteBulk(die_address, data);
                } else {
                    ReturnValue = WriteBulk((uint)logical_address, data);
                }
                if (!ReturnValue)
                    return false;
                if (FCUSB.HWBOARD == FCUSB_BOARD.Mach1 && MyFlashDevice.WriteMode == MFP_PRG.BypassMode) {
                    Utilities.Sleep(300); // Board is too fast! We need a delay between writes (i.e. AM29LV160B)
                }
                FCUSB.USB_WaitForComplete();
                logical_address += data.Length;
                DataToWrite = (uint)(DataToWrite - data.Length);
            }
            if (MyFlashDevice.DELAY_MODE == MFP_DELAY.DQ7 | MyFlashDevice.DELAY_MODE == MFP_DELAY.SR1 | MyFlashDevice.DELAY_MODE == MFP_DELAY.SR2) {
                EXPIO_WAIT();
            } else {
                Utilities.Sleep((int)MyFlashDevice.SOFTWARE_DELAY);
            }
        } catch {
        } finally {
            EXPIO_VPP_DISABLE();
            if (MyFlashDevice.RESET_ENABLED)
                ResetDevice();
        }
        return true;
    }

    public void WaitUntilReady() {
        Utilities.Sleep(100); // Some flash devices have registers, some rely on delays
    }

    public long SectorFind(uint sector_index) {
        uint base_addr = 0U;
        if (sector_index > 0L) {
            for (uint i = 0U, loopTo = (uint)(sector_index - 1L); i <= loopTo; i++)
                base_addr += SectorSize(i);
        }
        return base_addr;
    }

    public bool SectorWrite(uint sector_index, byte[] data, WriteParameters Params = null) {
        uint Addr32 = (uint)this.SectorFind(sector_index);
        return WriteData(Addr32, data, Params);
    }

    public uint SectorCount() {
        if (DUALDIE_EN) {
            return (uint)((long)MyFlashDevice.Sector_Count * 2L);
        } else {
            return MyFlashDevice.Sector_Count;
        }
    }

    public bool EraseDevice() {
        try {
            try {
                EXPIO_VPP_ENABLE();
                var wm = MyFlashDevice.WriteMode;
                if (wm == MFP_PRG.IntelSharp | wm == MFP_PRG.Buffer1) {
                    int BlockCount = (int)MyFlashDevice.Sector_Count;
                    SetProgress?.Invoke(0);
                    for (int i = 0, loopTo = BlockCount - 1; i <= loopTo; i++) {
                        if (!SectorErase((uint)i)) {
                            SetProgress?.Invoke(0);
                            return false; // Error erasing sector
                        } else {
                            float percent = (float)(i / (double)BlockCount * 100d);
                            SetProgress?.Invoke((int)Math.Floor(percent));
                        }
                    }
                    SetProgress?.Invoke(0);
                    return true; // Device successfully erased
                } else {
                    EXPIO_EraseChip();
                    Utilities.Sleep(200); // Perform blank check
                    for (int i = 0; i <= 179; i++) { // 3 minutes
                        if (BlankCheck(0U))
                            return true;
                        Utilities.Sleep(900);
                    }
                    return false;
                } // Timeout (device erase failed)
            } catch {
            } finally {
                EXPIO_VPP_DISABLE();
            }
        } catch {
        } finally {
            if (MyFlashDevice.RESET_ENABLED)
                ResetDevice();
        } // Lets do a chip reset too
        return false;
    }

    public uint SectorSize(uint sector) {
        if (!(MyFlashStatus == DeviceStatus.Supported))
            return 0U;
        if (DUALDIE_EN)
            sector = (uint)((long)MyFlashDevice.Sector_Count - 1L & sector);
        return (uint)MyFlashDevice.GetSectorSize((int)sector);
    }

    private enum E_BUS_WIDTH { // Number of bits transfered per operation
        X0 = 0, // Default
        X8 = 8,
        X16 = 16
    }

    private enum E_EXPIO_SECTOR : ushort {
        Standard = 1, // 0x5555=0xAA;0x2AAA=0x55;0x5555=0x80;0x5555=0xAA;0x2AAA=0x55;SA=0x30
        Intel = 2 // SA=0x50;SA=0x60;SA=0xD0,SR.7,SA=0x20;SA=0xD0,SR.7 (used by Intel/Sharp devices)
    }

    private enum E_EXPIO_CHIPERASE : ushort {
        Standard = 1, // 0x5555=0xAA;0x2AAA=0x55;0x5555=0x80;0x5555=0xAA;0x2AAA=0x55;0x5555=0x10
        Intel = 2 // 0x00=0x30;0x00=0xD0; (used by Intel/Sharp devices)
    }

    private enum E_EXPIO_WRITEDATA : ushort {
        Standard = 1, // 0x5555=0xAA;0x2AAA=0x55;0x5555=0xA0;SA=DATA;DELAY
        Intel = 2, // SA=0x40;SA=DATA;SR.7
        Bypass = 3, // 0x555=0xAA;0x2AA=55;0x555=20;(0x00=0xA0;SA=DATA;...)0x00=0x90;0x00=0x00
        Page = 4, // 0x5555,0x2AAA,0x5555;(BA/DATA)
        Buffer_1 = 5, // 0xE8...0xD0 (Used by Intel/Sharp)
        Buffer_2 = 6, // 0x555=0xAA,0x2AA=0x55,SA=0x25,SA=(WC-1).. (Used by Spanion/Cypress)
        EPROM_X8 = 7, // 8-BIT EPROM DEVICE
        EPROM_X16 = 8 // 16-BIT EPROM DEVICE
    }

    private E_BUS_WIDTH CURRENT_BUS_WIDTH { get; set; } = E_BUS_WIDTH.X0;

    private bool EXPIO_SETUP_USB(MEM_PROTOCOL mode) {
        try {
            var result_data = new byte[1];
            uint setup_data = (uint)mode;
            bool result = FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_INIT, ref result_data, setup_data);
            if (!result)
                return false;
            if (result_data[0] == 0x17) { // Extension port returns 0x17 if it can communicate with the MCP23S17
                System.Threading.Thread.Sleep(50); // Give the USB time to change modes
                switch (mode) {
                    case MEM_PROTOCOL.NOR_X8: {
                            CURRENT_BUS_WIDTH = E_BUS_WIDTH.X8;
                            break;
                        }

                    case MEM_PROTOCOL.NOR_X16: {
                            CURRENT_BUS_WIDTH = E_BUS_WIDTH.X16;
                            break;
                        }

                    case MEM_PROTOCOL.NOR_X16_X8: {
                            CURRENT_BUS_WIDTH = E_BUS_WIDTH.X16;
                            break;
                        }
                }
                return true; // Communication successful
            } else {
                return false;
            }
        } catch {
            return false;
        }
    }

    private bool EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA mode) {
        try {
            bool result = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_MODE_WRITE, null, (uint)mode);
            CURRENT_WRITE_MODE = mode;
            return result;
        } catch {
            return false;
        }
    }

    private bool EXPIO_SETUP_DELAY(MFP_DELAY delay_mode) {
        try {
            bool result = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_MODE_DELAY, null, (uint)delay_mode);
            System.Threading.Thread.Sleep(25);
            return result;
        } catch {
            return false;
        }
    }

    private bool EXPIO_SETUP_WRITEDELAY(ushort delay_cycles) {
        try {
            bool result = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_DELAY, null, delay_cycles);
            System.Threading.Thread.Sleep(25);
            return result;
        } catch {
            return false;
        }
    }
    // We should only allow this for devices that have a 12V option/chip
    private void EXPIO_VPP_ENABLE() {
        bool VPP_FEAT_EN = false;
        var if_type = MyFlashDevice.IFACE;
        if (if_type == VCC_IF.X16_5V_12VPP) {
            VPP_FEAT_EN = true;
        } else if (if_type == VCC_IF.X16_3V_12VPP) {
            VPP_FEAT_EN = true;
        } else if (if_type == VCC_IF.X8_5V_12VPP) {
            VPP_FEAT_EN = true;
        }
        if (VPP_FEAT_EN) {
            this.HardwareControl(FCUSB_HW_CTRL.VPP_12V);
            Utilities.Sleep(100); // We need to wait
        }
    }
    // We should only allow this for devices that have a 12V option/chip
    private void EXPIO_VPP_DISABLE() {
        bool VPP_FEAT_EN = false;
        var if_type = MyFlashDevice.IFACE;
        if (if_type == VCC_IF.X16_5V_12VPP) {
            VPP_FEAT_EN = true;
        } else if (if_type == VCC_IF.X16_3V_12VPP) {
            VPP_FEAT_EN = true;
        } else if (if_type == VCC_IF.X8_5V_12VPP) {
            VPP_FEAT_EN = true;
        }
        if (VPP_FEAT_EN) {
            this.HardwareControl(FCUSB_HW_CTRL.VPP_5V);
            Utilities.Sleep(100); // We need to wait
        }
    }

    private void EXPIO_PrintCurrentWriteMode() {
        switch (CURRENT_WRITE_MODE) {
            case E_EXPIO_WRITEDATA.Standard: {  // 0x5555=0xAA;0x2AAA=0x55;0x5555=0xA0;SA=DATA;DELAY
                    PrintConsole?.Invoke("Write mode supported" + ": Standard");
                    break;
                }
            case E_EXPIO_WRITEDATA.Intel: { // SA=0x40;SA=DATA;SR.7
                    PrintConsole?.Invoke("Write mode supported" + ": Auto-Word Program");
                    break;
                }
            case E_EXPIO_WRITEDATA.Bypass: { // 0x555=0xAA;0x2AA=55;0x555=20;(0x00=0xA0;SA=DATA;...)0x00=0x90;0x00=0x00
                    PrintConsole?.Invoke("Write mode supported" + ": Bypass Mode");
                    break;
                }
            case E_EXPIO_WRITEDATA.Page: {  // 0x5555,0x2AAA,0x5555;(BA/DATA)
                    PrintConsole?.Invoke("Write mode supported" + ": Page Write");
                    break;
                }
            case E_EXPIO_WRITEDATA.Buffer_1: {  // 0xE8...0xD0
                    PrintConsole?.Invoke("Write mode supported" + ": Buffer (Intel)");
                    break;
                }
            case E_EXPIO_WRITEDATA.Buffer_2: { // 0x555=0xAA,0x2AA=0x55,SA=0x25,SA=(WC-1)..
                    PrintConsole?.Invoke("Write mode supported" + ": Buffer (Cypress)");
                    break;
                }
        }
    }

    private E_EXPIO_WRITEDATA CURRENT_WRITE_MODE { get; set; }
    private E_EXPIO_SECTOR CURRENT_SECTOR_ERASE { get; set; }
    private E_EXPIO_CHIPERASE CURRENT_CHIP_ERASE { get; set; }

    private delegate void cfi_cmd_sub();

    private byte[] cfi_data;

    // 0xAAA=0xAA;0x555=0x55;0xAAA=0x90; (X8/X16 DEVICES)
    private byte[] EXPIO_ReadIdent(bool X16_MODE) {
        var ident = new byte[8];
        uint SHIFT = 0U;
        if (X16_MODE)
            SHIFT = 1U;
        EXPIO_ResetDevice();
        Utilities.Sleep(10);
        WriteCommandData(0x5555U, 0xAA);
        WriteCommandData(0x2AAAU, 0x55);
        WriteCommandData(0x5555U, 0x90);
        Utilities.Sleep(10);
        ident[0] = (byte)(ReadMemoryAddress(0U) & 0xFF);             // MFG
        ushort ID1 = ReadMemoryAddress((uint)(1 << (int)SHIFT));
        if (!X16_MODE)
            ID1 = (ushort)(ID1 & 0xFF);                   // X8 ID1
        ident[1] = (byte)(ID1 >> 8 & 0xFF);                       // ID1(UPPER)
        ident[2] = (byte)(ID1 & 0xFF);                              // ID1(LOWER)
        ident[3] = (byte)(ReadMemoryAddress((uint)(0xE << (int)SHIFT)) & 0xFF);  // ID2
        ident[4] = (byte)(ReadMemoryAddress((uint)(0xF << (int)SHIFT)) & 0xFF);  // ID3
        EXPIO_ResetDevice();
        Utilities.Sleep(1);
        CURRENT_SECTOR_ERASE = E_EXPIO_SECTOR.Standard;
        CURRENT_CHIP_ERASE = E_EXPIO_CHIPERASE.Standard;
        CURRENT_WRITE_MODE = E_EXPIO_WRITEDATA.Standard;
        return ident;
    }
    // Sets access and write pulse timings for MACH1 using NOR PARALLEL mode
    public void EXPIO_SetTiming(int read_access, int we_pulse) {
        if (FCUSB.HWBOARD == FCUSB_BOARD.Mach1) {
            FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_TIMING, null, (uint)(read_access << 8 | we_pulse));
        }
    }

    private void EXPIO_WAIT() {
        Utilities.Sleep(10);
        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WAIT);
        FCUSB.USB_WaitForComplete(); // Checks for WAIT flag to clear
    }

    // (X8/X16 DEVICES)
    private void EXPIO_EraseSector_Standard(uint addr) {
        // Write Unlock Cycles
        WriteCommandData(0x5555U, 0xAA);
        WriteCommandData(0x2AAAU, 0x55);
        // Write Sector Erase Cycles
        WriteCommandData(0x5555U, 0x80);
        WriteCommandData(0x5555U, 0xAA);
        WriteCommandData(0x2AAAU, 0x55);
        WriteMemoryAddress(addr, 0x30);
    }

    private void EXPIO_EraseSector_Intel(uint addr) {
        WriteMemoryAddress(addr, 0x50); // clear register
        WriteMemoryAddress(addr, 0x60); // Unlock block (just in case)
        WriteMemoryAddress(addr, 0xD0); // Confirm Command
        EXPIO_WAIT();
        WriteMemoryAddress(addr, 0x20);
        WriteMemoryAddress(addr, 0xD0);
        EXPIO_WAIT();
        WriteMemoryAddress(0U, 0xFF); // Puts the device back into READ mode
        WriteMemoryAddress(0U, 0xF0);
    }

    private void EXPIO_EraseChip_Standard() {
        WriteCommandData(0x5555U, 0xAA);
        WriteCommandData(0x2AAAU, 0x55);
        WriteCommandData(0x5555U, 0x80);
        WriteCommandData(0x5555U, 0xAA);
        WriteCommandData(0x2AAAU, 0x55);
        WriteCommandData(0x5555U, 0x10);
    }

    private void EXPIO_EraseChip_Intel() {
        WriteMemoryAddress(0x0U, 0x30);
        WriteMemoryAddress(0x0U, 0xD0);
    }

    private void EXPIO_ResetDevice() {
        WriteCommandData(0x5555U, 0xAA); // Standard
        WriteCommandData(0x2AAAU, 0x55);
        WriteCommandData(0x5555U, 0xF0);
        WriteCommandData(0U, 0xF0); // Intel
        WriteCommandData(0U, 0xFF); // Intel
    }

    private void EXPIO_EraseChip() {
        switch (CURRENT_CHIP_ERASE) {
            case E_EXPIO_CHIPERASE.Standard: {
                    EXPIO_EraseChip_Standard();
                    break;
                }

            case E_EXPIO_CHIPERASE.Intel: {
                    EXPIO_EraseChip_Intel();
                    break;
                }
        }
    }

    private void EXPIO_EraseSector(uint sector_addr) {
        switch (CURRENT_SECTOR_ERASE) {
            case E_EXPIO_SECTOR.Standard: {
                    EXPIO_EraseSector_Standard(sector_addr);
                    break;
                }

            case E_EXPIO_SECTOR.Intel: {
                    EXPIO_EraseSector_Intel(sector_addr);
                    break;
                }
        }
    }

    private byte[] EXPIO_NOR_GetCFI() {
        try {
            cfi_data = null;
            if (CFI_ExecuteCommand(() => WriteCommandData(0x55U, 0x98))) { // Issue Enter CFI command
                MainApp.PrintConsole("Common Flash Interface information present");
                return cfi_data;
            } else if (CFI_ExecuteCommand(() => {
                WriteCommandData(0x5555U, 0xAA);
                WriteCommandData(0x2AAAU, 0x55);
                WriteCommandData(0x5555U, 0x98);
            })) {
                MainApp.PrintConsole("Common Flash Interface information present");
                return cfi_data;
            } else {
                cfi_data = null;
                MainApp.PrintConsole("Common Flash Interface information not present");
            }
        } catch {
        } finally {
            EXPIO_ResetDevice();
            Utilities.Sleep(50);
        }

        return null;
    }

    private bool CFI_ExecuteCommand(cfi_cmd_sub cfi_cmd) {
        cfi_cmd.Invoke();
        cfi_data = new byte[32];
        uint SHIFT = 0U;
        if (CURRENT_BUS_WIDTH == E_BUS_WIDTH.X16)
            SHIFT = 1U;
        cfi_data = new byte[32];
        for (int i = 0, loopTo = cfi_data.Length - 1; i <= loopTo; i++)
            cfi_data[i] = (byte)(ReadMemoryAddress((uint)(0x10 + i << (int)SHIFT)) & 255);
        if (cfi_data[0] == 0x51 & cfi_data[1] == 0x52 & cfi_data[2] == 0x59) { // QRY
            return true;
        }

        return false;
    }
    // This is used to write data (8/16 bit) to the EXTIO IO (parallel NOR) port. CMD ADDRESS
    public bool WriteCommandData(uint cmd_addr, ushort cmd_data) {
        var addr_data = new byte[6];
        addr_data[0] = (byte)(cmd_addr >> 24 & 255L);
        addr_data[1] = (byte)(cmd_addr >> 16 & 255L);
        addr_data[2] = (byte)(cmd_addr >> 8 & 255L);
        addr_data[3] = (byte)(cmd_addr & 255L);
        addr_data[4] = (byte)(cmd_data >> 8 & 255);
        addr_data[5] = (byte)(cmd_data & 255);
        if (FCUSB.HWBOARD == FCUSB_BOARD.Mach1) {
            FCUSB.USB_CONTROL_MSG_OUT(USBREQ.LOAD_PAYLOAD, addr_data);
            return FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WRCMDDATA);
        } else {
            return FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WRCMDDATA, addr_data);
        }
    }

    public bool WriteMemoryAddress(uint mem_addr, ushort mem_data) {
        var addr_data = new byte[6];
        addr_data[0] = (byte)(mem_addr >> 24 & 255L);
        addr_data[1] = (byte)(mem_addr >> 16 & 255L);
        addr_data[2] = (byte)(mem_addr >> 8 & 255L);
        addr_data[3] = (byte)(mem_addr & 255L);
        addr_data[4] = (byte)(mem_data >> 8 & 255);
        addr_data[5] = (byte)(mem_data & 255);
        if (FCUSB.HWBOARD == FCUSB_BOARD.Mach1) {
            FCUSB.USB_CONTROL_MSG_OUT(USBREQ.LOAD_PAYLOAD, addr_data);
            return FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WRMEMDATA);
        } else {
            return FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WRMEMDATA, addr_data);
        }
    }

    public ushort ReadMemoryAddress(uint mem_addr) {
        var data_out = new byte[2];
        FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_RDMEMDATA, ref data_out, mem_addr);
        return (ushort)(data_out[1] << 8 | data_out[0]);
    }

    private bool DetectFlashDevice() {
        PrintConsole?.Invoke("Attempting to automatically detect Flash device"); // Attempting to automatically detect Flash device
        FlashDetectResult LAST_DETECT = default;
        LAST_DETECT.MFG = 0;
        FLASH_IDENT = this.DetectFlash(MEM_PROTOCOL.NOR_X16);
        if (FLASH_IDENT.Successful) {
            var d = MainApp.FlashDatabase.FindDevices(FLASH_IDENT.MFG, FLASH_IDENT.ID1, FLASH_IDENT.ID2, MemoryType.PARALLEL_NOR);
            if (d.Length > 0 && this.IsIFACE16X(((P_NOR)d[0]).IFACE)) {
                PrintConsole?.Invoke(string.Format("Successfully detected device in {0} mode", "NOR X16 (Word addressing)"));
                MyAdapter = MEM_PROTOCOL.NOR_X16;
                return true;
            } else {
                LAST_DETECT = FLASH_IDENT;
            }
        }
        FLASH_IDENT = this.DetectFlash(MEM_PROTOCOL.NOR_X16_X8);
        if (FLASH_IDENT.Successful) {
            var d = MainApp.FlashDatabase.FindDevices(FLASH_IDENT.MFG, FLASH_IDENT.ID1, 0, MemoryType.PARALLEL_NOR);
            if (d.Length > 0 && this.IsIFACE16X(((P_NOR)d[0]).IFACE)) {
                PrintConsole?.Invoke(string.Format("Successfully detected device in {0} mode", "NOR X16 (Byte addressing)"));
                MyAdapter = MEM_PROTOCOL.NOR_X16_X8;
                return true;
            } else {
                LAST_DETECT = FLASH_IDENT;
            }
        }
        FLASH_IDENT = this.DetectFlash(MEM_PROTOCOL.NOR_X8);
        if (FLASH_IDENT.Successful) {
            var d = MainApp.FlashDatabase.FindDevices(FLASH_IDENT.MFG, FLASH_IDENT.ID1, 0, MemoryType.PARALLEL_NOR);
            if (d.Length > 0 && this.IsIFACE8X(((P_NOR)d[0]).IFACE)) {
                PrintConsole?.Invoke(string.Format("Successfully detected device in {0} mode", "NOR X8"));
                MyAdapter = MEM_PROTOCOL.NOR_X8;
                return true;
            } else {
                LAST_DETECT = FLASH_IDENT;
            }
        }
        if (!((int)LAST_DETECT.MFG == 0)) {
            FLASH_IDENT = LAST_DETECT;
            return true; // Found, but not in library
        }
        return false; // No devices detected
    }

    private FlashDetectResult DetectFlash(MEM_PROTOCOL mode) {
        string mode_name = "";
        var ident_data = new byte[8]; // 8 bytes total
        var result = default(FlashDetectResult);
        switch (mode) {
            case MEM_PROTOCOL.NOR_X16: {
                    mode_name = "NOR X16 (Word addressing)";
                    result = EXPIO_DetectX16();
                    break;
                }
            case MEM_PROTOCOL.NOR_X16_X8: {
                    mode_name = "NOR X16 (Byte addressing)";
                    result = EXPIO_DetectX16_X8();
                    break;
                }
            case MEM_PROTOCOL.NOR_X8: {
                    mode_name = "NOR X8";
                    result = EXPIO_DetectX8();
                    break;
                }
        }
        if (result.Successful) {
            uint part = (uint)result.ID1 << 16 | (uint)result.ID2;
            string chip_id_str = result.MFG.ToString("X").PadLeft(2, '0') + part.ToString("X").PadLeft(8, '0');
            PrintConsole?.Invoke("Mode " + mode_name + " returned ident code: 0x" + chip_id_str);
        }
        return result;
    }

    private FlashDetectResult EXPIO_DetectX16() {
        byte[] ident_data = null;
        this.EXPIO_SETUP_USB(MEM_PROTOCOL.NOR_X16);
        ident_data = EXPIO_ReadIdent(true);
        return Tools.GetFlashResult(ident_data);
    }

    private FlashDetectResult EXPIO_DetectX16_X8() {
        byte[] ident_data = null;
        this.EXPIO_SETUP_USB(MEM_PROTOCOL.NOR_X16_X8);
        ident_data = EXPIO_ReadIdent(true);
        return Tools.GetFlashResult(ident_data);
    }

    private FlashDetectResult EXPIO_DetectX8() {
        byte[] ident_data = null;
        this.EXPIO_SETUP_USB(MEM_PROTOCOL.NOR_X8);
        ident_data = EXPIO_ReadIdent(false);
        return Tools.GetFlashResult(ident_data);
    }

    private void PrintDeviceInterface() {
        switch (MyFlashDevice.IFACE) {
            case VCC_IF.X8_3V: {
                    PrintConsole?.Invoke("Device interface" + ": NOR X8 (3V)");
                    break;
                }
            case VCC_IF.X8_5V: {
                    PrintConsole?.Invoke("Device interface" + ": NOR X8 (5V)");
                    break;
                }
            case VCC_IF.X8_5V_12VPP: {
                    PrintConsole?.Invoke("Device interface" + ": NOR X16 (5V/12V VPP)");
                    break;
                }
            case VCC_IF.X16_1V8: {
                    PrintConsole?.Invoke("Device interface" + ": NOR X16 (1.8V)");
                    break;
                }
            case VCC_IF.X16_3V: {
                    PrintConsole?.Invoke("Device interface" + ": NOR X16 (3V)");
                    break;
                }
            case VCC_IF.X16_5V: {
                    PrintConsole?.Invoke("Device interface" + ": NOR X16 (5V)");
                    break;
                }
            case VCC_IF.X16_5V_12VPP: {
                    PrintConsole?.Invoke("Device interface" + ": NOR X16 (5V/12V VPP)");
                    break;
                }
        }
    }

    private bool IsIFACE8X(VCC_IF input) {
        switch (input) {
            case VCC_IF.X8_3V: {
                    return true;
                }
            case VCC_IF.X8_5V: {
                    return true;
                }
            case VCC_IF.X8_5V_12VPP: {
                    return true;
                }
            default: {
                    return false;
                }
        }
    }

    private bool IsIFACE16X(VCC_IF input) {
        switch (input) {
            case VCC_IF.X16_1V8: {
                    return true;
                }
            case VCC_IF.X16_3V: {
                    return true;
                }
            case VCC_IF.X16_5V: {
                    return true;
                }
            case VCC_IF.X16_5V_12VPP: {
                    return true;
                }
            default: {
                    return false;
                }
        }
    }

    public bool ResetDevice() {
        try {
            if (MyFlashDevice.FLASH_TYPE == MemoryType.PARALLEL_NOR) {
                EXPIO_ResetDevice();
            }
        } catch {
            return false;
        } finally {
            Utilities.Sleep(50);
        }
        return true;
    }

    private byte[] GetSetupPacket_NOR(uint Address, uint Count, ushort PageSize) {
        var data_in = new byte[20]; // 18 bytes total
        data_in[0] = (byte)(Address & 255L);
        data_in[1] = (byte)(Address >> 8 & 255L);
        data_in[2] = (byte)(Address >> 16 & 255L);
        data_in[3] = (byte)(Address >> 24 & 255L);
        data_in[4] = (byte)(Count & 255L);
        data_in[5] = (byte)(Count >> 8 & 255L);
        data_in[6] = (byte)(Count >> 16 & 255L);
        data_in[7] = (byte)(Count >> 24 & 255L);
        data_in[8] = (byte)(PageSize & 255); // This is how many bytes to increment between operations
        data_in[9] = (byte)(PageSize >> 8 & 255);
        return data_in;
    }

    private bool BlankCheck(uint base_addr) {
        try {
            bool IsBlank = false;
            int Counter = 0;
            while (!IsBlank) {
                Utilities.Sleep(10);
                var w = this.ReadData((long)base_addr, 4L);
                if (w is null)
                    return false;
                if (w[0] == 255 && w[1] == 255 && w[2] == 255 && w[3] == 255)
                    IsBlank = true;
                Counter += 1;
                if (Counter == 50)
                    return false; // Timeout (500 ms)
            }
            return true;
        } catch {
            return false;
        }
    }

    private byte[] ReadBulk(uint address, uint count) {
        try {
            uint read_count = count;
            bool addr_offset = false;
            if (!(MyAdapter == MEM_PROTOCOL.NOR_X8)) {
                if (address % 2L == 1L) {
                    addr_offset = true;
                    address = (uint)(address - 1L);
                    read_count = (uint)(read_count + 1L);
                }
                if (read_count % 2L == 1L) {
                    read_count = (uint)(read_count + 1L);
                }
            }
            var data_out = new byte[(int)(read_count - 1L + 1)]; // Bytes we want to read
            int page_size = 512;
            if (MyFlashDevice is object)
                page_size = (int)MyFlashDevice.PAGE_SIZE;
            var setup_data = GetSetupPacket_NOR(address, read_count, (ushort)page_size);
            bool result = FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_READDATA, setup_data, ref data_out, 0U);
            if (!result)
                return null;
            if (addr_offset) {
                var new_data = new byte[(int)(count - 1L + 1)];
                Array.Copy(data_out, 1, new_data, 0, new_data.Length);
                data_out = new_data;
            } else {
                Array.Resize(ref data_out, (int)(count - 1L + 1));
            }
            return data_out;
        } catch {
        }
        return null;
    }

    private bool WriteBulk(uint address, byte[] data_out) {
        try {
            var setup_data = GetSetupPacket_NOR(address, (uint)data_out.Length, (ushort)MyFlashDevice.PAGE_SIZE);
            return FCUSB.USB_SETUP_BULKOUT(USBREQ.EXPIO_WRITEDATA, setup_data, data_out, 0U);
        } catch {
        }
        return false;
    }

    private void HardwareControl(FCUSB_HW_CTRL cmd) {
        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, null, (uint)cmd);
        Utilities.Sleep(10);
    }

    public void PARALLEL_PORT_TEST() {
        MainApp.SetStatus("Performing parallel I/O output test");
        this.EXPIO_SETUP_USB(MEM_PROTOCOL.NOR_X16);
        WriteCommandData(0xFFFFFFFFU, 0xFFFF);
        this.HardwareControl(FCUSB_HW_CTRL.BYTE_HIGH);
        this.HardwareControl(FCUSB_HW_CTRL.RB0_HIGH);
        this.HardwareControl(FCUSB_HW_CTRL.OE_HIGH);
        this.HardwareControl(FCUSB_HW_CTRL.CE_HIGH);
        this.HardwareControl(FCUSB_HW_CTRL.WE_HIGH);
        this.HardwareControl(FCUSB_HW_CTRL.CLE_HIGH);
        this.HardwareControl(FCUSB_HW_CTRL.ALE_HIGH);
        this.HardwareControl(FCUSB_HW_CTRL.RELAY_OFF);
        this.HardwareControl(FCUSB_HW_CTRL.VPP_5V);
        Utilities.Sleep(500);
        WriteCommandData(0U, 0);
        this.HardwareControl(FCUSB_HW_CTRL.BYTE_LOW);
        this.HardwareControl(FCUSB_HW_CTRL.OE_LOW);
        this.HardwareControl(FCUSB_HW_CTRL.CE_LOW);
        this.HardwareControl(FCUSB_HW_CTRL.WE_LOW);
        this.HardwareControl(FCUSB_HW_CTRL.CLE_LOW);
        this.HardwareControl(FCUSB_HW_CTRL.ALE_LOW);
        this.HardwareControl(FCUSB_HW_CTRL.RB0_LOW);
        Utilities.Sleep(500);
        this.HardwareControl(FCUSB_HW_CTRL.VPP_5V);
        Utilities.Sleep(300);
        this.HardwareControl(FCUSB_HW_CTRL.VPP_0V);
        this.HardwareControl(FCUSB_HW_CTRL.CLE_HIGH);
        Utilities.Sleep(300);
        this.HardwareControl(FCUSB_HW_CTRL.CLE_LOW);
        this.HardwareControl(FCUSB_HW_CTRL.ALE_HIGH);
        Utilities.Sleep(300);
        this.HardwareControl(FCUSB_HW_CTRL.ALE_LOW);
        this.HardwareControl(FCUSB_HW_CTRL.BYTE_HIGH);
        Utilities.Sleep(300);
        this.HardwareControl(FCUSB_HW_CTRL.BYTE_LOW);
        this.HardwareControl(FCUSB_HW_CTRL.RB0_HIGH);
        Utilities.Sleep(300);
        this.HardwareControl(FCUSB_HW_CTRL.RB0_LOW);
        for (int i = 0; i <= 7; i++) {
            WriteCommandData(0U, (ushort)(1 << i));
            Utilities.Sleep(300);
        }
        for (int i = 15; i >= 8; i -= 1) {
            WriteCommandData(0U, (ushort)(1 << i));
            Utilities.Sleep(300);
        }
        WriteCommandData(0U, 0);
        this.HardwareControl(FCUSB_HW_CTRL.WE_LOW);
        this.HardwareControl(FCUSB_HW_CTRL.CE_HIGH);
        Utilities.Sleep(300);
        this.HardwareControl(FCUSB_HW_CTRL.CE_LOW);
        this.HardwareControl(FCUSB_HW_CTRL.WE_HIGH);
        Utilities.Sleep(300);
        this.HardwareControl(FCUSB_HW_CTRL.WE_LOW);
        this.HardwareControl(FCUSB_HW_CTRL.OE_HIGH);
        Utilities.Sleep(300);
        this.HardwareControl(FCUSB_HW_CTRL.OE_LOW);
        for (int i = 0; i <= 27; i++) {
            WriteCommandData((uint)(1 << i), 0);
            Utilities.Sleep(300);
        }
        WriteCommandData(0U, 0);
        MainApp.SetStatus("Parallel I/O output test complete");
    }

}