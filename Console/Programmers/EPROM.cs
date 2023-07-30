using USB;
using System;
using FlashMemory;

public class EPROM_Programmer : MemoryDeviceUSB {
    private FCUSB_DEVICE FCUSB;
    public event MemoryDeviceUSB.PrintConsoleEventHandler PrintConsole;
    public event MemoryDeviceUSB.SetProgressEventHandler SetProgress;

    public Device MyFlashDevice { get; set; }
    public DeviceStatus MyFlashStatus { get; set; } = DeviceStatus.NotDetected;
    public MEM_PROTOCOL MyAdapter { get; set; } // This is the kind of socket adapter connected and the mode it is in


    public EPROM_Programmer(FCUSB_DEVICE parent_if) {
        SetProgress?.Invoke(0);
        FCUSB = parent_if;
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
            OTP_EPROM NOR_FLASH = (OTP_EPROM)MyFlashDevice;
            return NOR_FLASH.FLASH_SIZE;
        }
    }

    public uint SectorSize(uint sector) {
        return 8192U; // Program 8KB at a time
    }

    public void WaitUntilReady() {
        Utilities.Sleep(100);
    }

    public bool DeviceInit() {
        MyFlashDevice = null;
        if (EPROM_Detect()) {
            OTP_EPROM o = (OTP_EPROM)MyFlashDevice;
            if (o.IFACE == VCC_IF.X16_5V_12VPP) {
                this.EXPIO_SETUP_USB(MEM_PROTOCOL.EPROM_X16);
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_MODE_WRITE, null, (uint)E_DEV_MODE.EPROM_X16);
                MyAdapter = MEM_PROTOCOL.EPROM_X16;
                PrintConsole?.Invoke("Write mode supported" + ": EPROM (16-bit)");
            } else if (o.IFACE == VCC_IF.X8_5V_12VPP) {
                this.EXPIO_SETUP_USB(MEM_PROTOCOL.EPROM_X8);
                MyAdapter = MEM_PROTOCOL.EPROM_X8;
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_MODE_WRITE, null, (uint)E_DEV_MODE.EPROM_X8);
                MyAdapter = MEM_PROTOCOL.EPROM_X8;
                PrintConsole?.Invoke("Write mode supported" + ": EPROM (8-bit)");
            }
        } else {
            PrintConsole?.Invoke("Unable to automatically detect EPROM/OTP device");
            return false;
        }

        PrintConsole?.Invoke("EPROM successfully detected!");
        PrintConsole?.Invoke("EPROM device: " + MyFlashDevice.NAME + ", size: " + MyFlashDevice.FLASH_SIZE.ToString("N0") + " bytes");
        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_DELAY, null, (uint)((OTP_EPROM)MyFlashDevice).HARDWARE_DELAY);
        ((OTP_EPROM)MyFlashDevice).IS_BLANK = true;
        // DirectCast(MyFlashDevice, OTP_EPROM).IS_BLANK = EPROM_BlankCheck()
        PrintConsole?.Invoke("EPROM mode ready for operation");
        MyFlashStatus = DeviceStatus.Supported;
        return true;
    }

    public byte[] ReadData(long flash_offset, long data_count) {
        OTP_EPROM M27C160 = (OTP_EPROM)MainApp.FlashDatabase.FindDevice(0x20, 0xB1, 0, MemoryType.OTP_EPROM);
        OTP_EPROM M27C801 = (OTP_EPROM)MainApp.FlashDatabase.FindDevice(0x20, 0x42, 0, MemoryType.OTP_EPROM);
        OTP_EPROM M27C1001 = (OTP_EPROM)MainApp.FlashDatabase.FindDevice(0x20, 0x5, 0, MemoryType.OTP_EPROM);
        if (object.ReferenceEquals(MyFlashDevice, M27C160)) {
            this.HardwareControl(FCUSB_HW_CTRL.VPP_5V);
            this.HardwareControl(FCUSB_HW_CTRL.OE_LOW);
            this.HardwareControl(FCUSB_HW_CTRL.VPP_ENABLE); // Must enable VPP for BYTEvpp=HIGH(5V)
        } else if (object.ReferenceEquals(MyFlashDevice, M27C1001)) {
            this.HardwareControl(FCUSB_HW_CTRL.VPP_0V);
            this.HardwareControl(FCUSB_HW_CTRL.OE_LOW);
            this.HardwareControl(FCUSB_HW_CTRL.VPP_DISABLE);
        } else {
            this.HardwareControl(FCUSB_HW_CTRL.VPP_0V);
            this.HardwareControl(FCUSB_HW_CTRL.VPP_DISABLE);
        }

        this.HardwareControl(FCUSB_HW_CTRL.WE_LOW);
        Utilities.Sleep(100);
        var data_out = ReadBulk((uint)flash_offset, (uint)data_count);
        this.HardwareControl(FCUSB_HW_CTRL.WE_HIGH);
        this.HardwareControl(FCUSB_HW_CTRL.VPP_0V);
        this.HardwareControl(FCUSB_HW_CTRL.VPP_DISABLE);
        return data_out;
    }

    public bool WriteData(long flash_offset, byte[] data_to_write, WriteParameters Params = null) {
        try {
            uint PacketSize = 2048U;
            OTP_EPROM M27C160 = (OTP_EPROM)MainApp.FlashDatabase.FindDevice(0x20, 0xB1, 0, MemoryType.OTP_EPROM);
            OTP_EPROM M27C801 = (OTP_EPROM)MainApp.FlashDatabase.FindDevice(0x20, 0x42, 0, MemoryType.OTP_EPROM);
            OTP_EPROM M27C1001 = (OTP_EPROM)MainApp.FlashDatabase.FindDevice(0x20, 0x5, 0, MemoryType.OTP_EPROM);
            uint BytesWritten = 0U;
            uint DataToWrite = (uint)data_to_write.Length;
            int Loops = (int)Math.Ceiling(DataToWrite / (double)PacketSize); // Calcuates iterations
            this.HardwareControl(FCUSB_HW_CTRL.WE_HIGH);
            this.HardwareControl(FCUSB_HW_CTRL.VPP_12V);
            this.HardwareControl(FCUSB_HW_CTRL.VPP_ENABLE);
            if (object.ReferenceEquals(MyFlashDevice, M27C160))
                this.HardwareControl(FCUSB_HW_CTRL.OE_HIGH);
            if (object.ReferenceEquals(MyFlashDevice, M27C1001))
                this.HardwareControl(FCUSB_HW_CTRL.OE_HIGH);
            Utilities.Sleep(200);
            for (int i = 0, loopTo = Loops - 1; i <= loopTo; i++) {
                if (Params.AbortOperation)
                    return false;
                int BufferSize = (int)DataToWrite;
                if (BufferSize > PacketSize)
                    BufferSize = (int)PacketSize;
                var data_packet = new byte[BufferSize];
                Array.Copy(data_to_write, i * PacketSize, data_packet, 0L, data_packet.Length);
                bool result = WriteBulk((uint)flash_offset, data_packet);
                if (!result)
                    return false;
                flash_offset += data_packet.Length;
                DataToWrite = (uint)(DataToWrite - data_packet.Length);
                BytesWritten = (uint)(BytesWritten + data_packet.Length);
            }

            this.HardwareControl(FCUSB_HW_CTRL.VPP_0V);
            this.HardwareControl(FCUSB_HW_CTRL.VPP_DISABLE);
            if (object.ReferenceEquals(MyFlashDevice, M27C160))
                this.HardwareControl(FCUSB_HW_CTRL.OE_LOW);
        } catch {
        }

        return true;
    }

    public long SectorFind(uint sector_index) {
        uint base_addr = 0U;
        if (sector_index > 0L) {
            for (uint i = 0U, loopTo = (uint)(sector_index - 1L); i <= loopTo; i++)
                base_addr += SectorSize(i);
        }

        return base_addr;
    }

    public uint SectorCount() {
        return MyFlashDevice.Sector_Count;
    }

    public bool SectorWrite(uint sector_index, byte[] data, WriteParameters Params = null) {
        uint Addr32 = (uint)this.SectorFind(sector_index);
        return WriteData(Addr32, data, Params);
    }

    public bool EraseDevice() {
        throw new NotSupportedException();
    }

    public bool SectorErase(uint SectorIndex) {
        throw new NotSupportedException();
    }

    public bool EPROM_Detect() {
        if (!this.EXPIO_SETUP_USB(MEM_PROTOCOL.EPROM_X8))
            return false;
        Utilities.Sleep(200);
        byte[] IDENT_DATA;
        IDENT_DATA = EPROM_ReadEletronicID_1();
        MyFlashDevice = MainApp.FlashDatabase.FindDevice(IDENT_DATA[0], IDENT_DATA[1], 0, MemoryType.OTP_EPROM);
        if (MyFlashDevice is object)
            return true; // Detected!
        IDENT_DATA = EPROM_ReadEletronicID_2();
        MyFlashDevice = MainApp.FlashDatabase.FindDevice(IDENT_DATA[0], IDENT_DATA[1], 0, MemoryType.OTP_EPROM);
        if (MyFlashDevice is object)
            return true; // Detected!
        return false;
    }

    private bool EXPIO_SETUP_USB(MEM_PROTOCOL mode) {
        try {
            var result_data = new byte[1];
            uint setup_data = (uint)mode;
            bool result = FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_INIT, ref result_data, setup_data);
            if (!result)
                return false;
            if (result_data[0] == 0x17) {
                System.Threading.Thread.Sleep(50); // Give the USB time to change modes
                return true; // Communication successful
            } else {
                return false;
            }
        } catch {
            return false;
        }
    }

    private byte[] EPROM_ReadEletronicID_1() {
        var IDENT_DATA = new byte[2];
        this.HardwareControl(FCUSB_HW_CTRL.VPP_ENABLE); // Enables VPP on adapter (CLE=HIGH)
        this.HardwareControl(FCUSB_HW_CTRL.OE_LOW);
        this.HardwareControl(FCUSB_HW_CTRL.WE_LOW);
        this.HardwareControl(FCUSB_HW_CTRL.VPP_12V);
        this.HardwareControl(FCUSB_HW_CTRL.RELAY_ON); // A9=12V and VPP=12V
        Utilities.Sleep(300); // Need this to be somewhat high to allow ID CODE to load
        FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_READDATA, GetSetupPacket(0U, 2U, 0), ref IDENT_DATA, 0U);
        this.HardwareControl(FCUSB_HW_CTRL.RELAY_OFF);
        this.HardwareControl(FCUSB_HW_CTRL.VPP_0V);
        this.HardwareControl(FCUSB_HW_CTRL.WE_HIGH);
        this.HardwareControl(FCUSB_HW_CTRL.VPP_DISABLE);
        if (IDENT_DATA[0] == 0 | IDENT_DATA[1] == 0) {
        } else if (IDENT_DATA[0] == 255 | IDENT_DATA[1] == 255) {
        } else {
            PrintConsole?.Invoke("EPROM IDENT CODE 1 returned MFG: 0x" + IDENT_DATA[0].ToString("X") + " and PART 0x" + IDENT_DATA[1].ToString("X"));
        }

        return IDENT_DATA;
    }

    private byte[] EPROM_ReadEletronicID_2() {
        var IDENT_DATA = new byte[2];
        this.HardwareControl(FCUSB_HW_CTRL.VPP_DISABLE); // Disables VPP Pin on Adapter (CLE=LOW)
        this.HardwareControl(FCUSB_HW_CTRL.OE_LOW);
        this.HardwareControl(FCUSB_HW_CTRL.WE_LOW);
        this.HardwareControl(FCUSB_HW_CTRL.VPP_12V);
        this.HardwareControl(FCUSB_HW_CTRL.RELAY_ON); // A9=12V and VPP=0V
        Utilities.Sleep(300); // Need this to be somewhat high to allow ID CODE to load
        FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_READDATA, GetSetupPacket(0U, 2U, 0), ref IDENT_DATA, 0U);
        this.HardwareControl(FCUSB_HW_CTRL.RELAY_OFF);
        this.HardwareControl(FCUSB_HW_CTRL.VPP_0V);
        this.HardwareControl(FCUSB_HW_CTRL.WE_HIGH);
        if (IDENT_DATA[0] == 0 | IDENT_DATA[1] == 0) {
        } else if (IDENT_DATA[0] == 255 | IDENT_DATA[1] == 255) {
        } else {
            PrintConsole?.Invoke("EPROM IDENT CODE 2 returned MFG: 0x" + IDENT_DATA[0].ToString("X") + " and PART 0x" + IDENT_DATA[1].ToString("X"));
        }

        return IDENT_DATA;
    }

    public bool EPROM_BlankCheck() {
        PrintConsole?.Invoke("Performing EPROM blank check");
        SetProgress?.Invoke(0);
        var entire_data = new byte[(int)(MyFlashDevice.FLASH_SIZE - 1L + 1)];
        int BlockCount = (int)(entire_data.Length / 8192d);
        for (int i = 0, loopTo = BlockCount - 1; i <= loopTo; i++) {
            if (MainApp.AppIsClosing)
                return false;
            var block = ReadBulk((uint)(i * 8191), 8191U);
            Array.Copy(block, 0, entire_data, i * 8191, 8191);
            float percent = (float)(i / (double)BlockCount * 100d);
            SetProgress?.Invoke((int)Math.Floor(percent));
        }

        if (Utilities.IsByteArrayFilled(ref entire_data, 255)) {
            PrintConsole?.Invoke("EPROM device is blank and can be programmed");
            ((OTP_EPROM)MyFlashDevice).IS_BLANK = true;
            return true;
        } else {
            PrintConsole?.Invoke("EPROM device is not blank");
            ((OTP_EPROM)MyFlashDevice).IS_BLANK = false;
            return false;
        }
    }

    private void HardwareControl(FCUSB_HW_CTRL cmd) {
        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, null, (uint)cmd);
    }

    private byte[] GetSetupPacket(uint Address, uint Count, ushort PageSize) {
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

    private ushort[] ByteToWordArray(byte[] byte_arr) {
        var @out = new ushort[(int)(byte_arr.Length / 2d - 1d + 1)];
        int ptr = 0;
        for (int i = 0, loopTo = @out.Length - 1; i <= loopTo; i++) {
            @out[i] = (ushort)(byte_arr[ptr + 1] << 8 | byte_arr[ptr]);
            ptr += 2;
        }

        return @out;
    }

    private byte[] ReadBulk(uint address, uint count) {
        try {
            uint read_count = count;
            bool addr_offset = false;
            if (MyAdapter == MEM_PROTOCOL.EPROM_X16) {
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
            var setup_data = GetSetupPacket(address, read_count, (ushort)page_size);
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
            var setup_data = GetSetupPacket(address, (uint)data_out.Length, (ushort)MyFlashDevice.PAGE_SIZE);
            bool result = FCUSB.USB_SETUP_BULKOUT(USBREQ.EXPIO_WRITEDATA, setup_data, data_out, 0U);
            return result;
        } catch {
        }
        return false;
    }

    private enum E_DEV_MODE : ushort {
        EPROM_X8 = 7, // 8-BIT EPROM DEVICE
        EPROM_X16 = 8 // 16-BIT EPROM DEVICE
    }
}