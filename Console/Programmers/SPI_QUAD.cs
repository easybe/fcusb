using FlashMemory;
using System;
using System.Diagnostics;
using USB;

namespace SPI {
    // This class is used for devices with DUAL and QUAD I/O modes
    public class SQI_Programmer : MemoryDeviceUSB {
        private FCUSB_DEVICE FCUSB;
        public event MemoryDeviceUSB.PrintConsoleEventHandler PrintConsole;
        public event MemoryDeviceUSB.SetProgressEventHandler SetProgress;

        public SPI_NOR MyFlashDevice { get; set; } // Contains the definition of the Flash device that is connected
        public DeviceStatus MyFlashStatus { get; set; } = DeviceStatus.NotDetected;
        public int DIE_SELECTED { get; set; } = 0;
        public MULTI_IO_MODE SQI_IO_MODE { get; set; } // IO=1/2/4 bits per clock cycle
        private SQI_IO_MODE SQI_DEVICE_MODE { get; set; } // 0=SPI_ONLY,1=QUAD_ONLY,2=DUAL_ONLY,3=SPI_QUAD,4=SPI_DUAL

        public SQI_Programmer(FCUSB_DEVICE parent_if) {
            SetProgress?.Invoke(0);
            FCUSB = parent_if;
        }

        public bool DeviceInit() {
            MyFlashStatus = DeviceStatus.NotDetected;
            SPI.SPI_IDENT DEVICEID = null;
            DEVICEID = ReadDeviceID(MULTI_IO_MODE.Single); // Read ID first, then see if we can access QUAD/DUAL
            DEVICEID = ReadDeviceID(MULTI_IO_MODE.Quad);
            if (!DEVICEID.DETECTED)
                DEVICEID = ReadDeviceID(MULTI_IO_MODE.Dual);
            if (!DEVICEID.DETECTED)
                DEVICEID = ReadDeviceID(MULTI_IO_MODE.Single);
            if (DEVICEID.DETECTED) {
                PrintConsole?.Invoke("Successfully opened device in SQI mode");
                string RDID_Str = "0x" + DEVICEID.MANU.ToString("X").PadLeft(2, '0') + DEVICEID.RDID.ToString("X").PadLeft(4, '0');
                PrintConsole?.Invoke(string.Format("Connected to SQI Flash (RDID:{0})", RDID_Str));
                ushort ID1 = (ushort)(DEVICEID.RDID >> 16);
                ushort ID2 = (ushort)((long)DEVICEID.RDID & 0xFFFFL);
                MyFlashDevice = (SPI_NOR)MainApp.FlashDatabase.FindDevice(DEVICEID.MANU, ID1, ID2, MemoryType.SERIAL_NOR, DEVICEID.FMY);
                if (MyFlashDevice is object) {
                    MyFlashStatus = DeviceStatus.Supported;
                    PrintConsole?.Invoke(string.Format("CFI compatible Flash detected at 0x{0}", DeviceName, DeviceSize.ToString("N0")));
                    PrintConsole?.Invoke("Programming mode: SQI (SPI-QUAD)");
                    if (MyFlashDevice.SQI_MODE == SPI_QUAD_SUPPORT.QUAD) {
                        if (SQI_IO_MODE == MULTI_IO_MODE.Quad) {
                            PrintConsole?.Invoke("Detected Flash in QUAD-SPI mode (4-bit)");
                            SQI_DEVICE_MODE = SPI.SQI_IO_MODE.QUAD_ONLY;
                        } else if (SQI_IO_MODE == MULTI_IO_MODE.Dual) {
                            PrintConsole?.Invoke("Detected Flash in DUAL-SPI mode (2-bit)");
                            SQI_DEVICE_MODE = SPI.SQI_IO_MODE.DUAL_ONLY;
                        } else if (SQI_IO_MODE == MULTI_IO_MODE.Single) {
                            PrintConsole?.Invoke("Detected Flash in SPI mode (1-bit)");
                            SQI_DEVICE_MODE = SPI.SQI_IO_MODE.SPI_ONLY;
                        }
                    } else if (MyFlashDevice.SQI_MODE == SPI_QUAD_SUPPORT.SPI_QUAD) {
                        PrintConsole?.Invoke("Detected Flash in SPI mode (1-bit)");
                        byte[] argReadBuffer = null;
                        this.SQIBUS_WriteRead(new byte[] { 0xF0 }, ReadBuffer: ref argReadBuffer); // SPI RESET COMMAND
                        Utilities.Sleep(20);
                        if ((int)DEVICEID.MANU == 0xEF) { // Winbond
                            // Winbond_EnableQUAD()
                        } else if ((int)DEVICEID.MANU == 1) { // Cypress/Spansion
                            var sr2 = new byte[1];
                            this.SQIBUS_WriteRead(new byte[] { 0x35 }, ref sr2);
                            if ((sr2[0] >> 1 & 1)==1) {
                                PrintConsole?.Invoke("IO mode switched to SPI/QUAD (1-bit/4-bit)");
                                SQI_DEVICE_MODE = SPI.SQI_IO_MODE.SPI_QUAD;
                            } else {
                                PrintConsole?.Invoke("QUAD mode not enabled, using SPI (1-bit)");
                                SQI_DEVICE_MODE = SPI.SQI_IO_MODE.SPI_ONLY;
                            }
                        }
                    } else {
                        PrintConsole?.Invoke("Detected Flash in SPI mode (1-bit)");
                    }
                    SQIBUS_SendCommand(0xF0); // SPI RESET COMMAND
                    if (MyFlashDevice.VENDOR_SPECIFIC == VENDOR_FEATURE.NotSupported) { // We don't want to do this for vendor enabled devices
                        this.WriteStatusRegister(new byte[] { 0 });
                        Utilities.Sleep(100); // Needed, some devices will lock up if else.
                    }
                    if (MyFlashDevice.SEND_EN4B)
                        this.SQIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.EN4B); // 0xB7
                    this.SQIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.ULBPR); // 0x98 (global block unprotect)
                    LoadVendorSpecificConfigurations(); // Some devices may need additional configurations
                    return true;
                } else {
                    MyFlashDevice = new SPI_NOR("Unknown", VCC_IF.SERIAL_3V, 0U, DEVICEID.MANU, (ushort)((long)DEVICEID.RDID & 0xFFFFL));
                    MyFlashStatus = DeviceStatus.NotSupported;
                    return false;
                }
            } else {
                MyFlashStatus = DeviceStatus.NotDetected;
                PrintConsole?.Invoke("Unable to detect compatible SPI device");
                return false;
            }
        }

        private SPI_IDENT ReadDeviceID(MULTI_IO_MODE mode) {
            var DEVICEID = new SPI.SPI_IDENT();
            var rdid = new byte[6];
            byte id_code = 0;
            if (mode == MULTI_IO_MODE.Single) {
                id_code = 0x9F;
            } else if (mode == MULTI_IO_MODE.Dual) {
                id_code = 0xAF;
            } else if (mode == MULTI_IO_MODE.Quad) {
                id_code = 0xAF;
            }
            SQI_IO_MODE = mode;
            if (SQIBUS_WriteRead(new byte[] { id_code }, ref rdid) == 7L) { // MULTIPLE I/O READ ID
                DEVICEID.MANU = rdid[0];
                DEVICEID.RDID = (uint)rdid[1] << 24 | (uint)rdid[2] << 16 | (uint)rdid[3] << 8 | rdid[4];
                DEVICEID.FMY = rdid[5];
            }
            return DEVICEID;
        }

        private void LoadVendorSpecificConfigurations() {
            if ((int)MyFlashDevice.MFG_CODE == 0xBF) { // SST26VF016/SST26VF032 requires block protection to be removed in SQI only
                if ((int)MyFlashDevice.ID1 == 0x2601) { // SST26VF016
                    SQIBUS_WriteEnable();
                    byte[] argReadBuffer = null;
                    this.SQIBUS_WriteRead(new byte[] { 0x42, 0, 0, 0, 0, 0, 0 }, ReadBuffer: ref argReadBuffer); // 6 blank bytes
                    Utilities.Sleep(200);
                } else if ((int)MyFlashDevice.ID1 == 0x2602) { // SST26VF032
                    SQIBUS_WriteEnable();
                    byte[] argReadBuffer1 = null;
                    this.SQIBUS_WriteRead(new byte[] { 0x42, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, ReadBuffer: ref argReadBuffer1); // 10 blank bytes
                    Utilities.Sleep(200);
                }
            } else if ((int)MyFlashDevice.MFG_CODE == 0x9D) { // ISSI
                this.WriteStatusRegister(new byte[] { 0 }); // Erase protection bits
            }

            if ((int)MyFlashDevice.MFG_CODE == 0xEF && (int)MyFlashDevice.ID1 == 0x4018) {
                byte[] argReadBuffer2 = null;
                this.SQIBUS_WriteRead(new byte[] { 0xC2, 1 }, ReadBuffer: ref argReadBuffer2);
                WaitUntilReady(); // Check to see if this device has two dies
            }

            if ((int)MyFlashDevice.MFG_CODE == 0x34) { // Cypress MFG ID
                bool SEMPER_SPI = false;
                if ((int)MyFlashDevice.ID1 == 0x2A19)
                    SEMPER_SPI = true;
                if ((int)MyFlashDevice.ID1 == 0x2A1A)
                    SEMPER_SPI = true;
                if ((int)MyFlashDevice.ID1 == 0x2A1B)
                    SEMPER_SPI = true;
                if ((int)MyFlashDevice.ID1 == 0x2B19)
                    SEMPER_SPI = true;
                if ((int)MyFlashDevice.ID1 == 0x2B1A)
                    SEMPER_SPI = true;
                if ((int)MyFlashDevice.ID1 == 0x2B1B)
                    SEMPER_SPI = true;
                if (SEMPER_SPI) {
                    SQIBUS_WriteEnable();
                    byte[] argReadBuffer3 = null;
                    this.SQIBUS_WriteRead(new byte[] { MyFlashDevice.OP_COMMANDS.EWSR }, ReadBuffer: ref argReadBuffer3);
                    byte[] argReadBuffer4 = null;
                    this.SQIBUS_WriteRead(new byte[] { MyFlashDevice.OP_COMMANDS.WRSR, 0, 0, 0x88, 0x18 }, ReadBuffer: ref argReadBuffer4); // Set sector to uniform 256KB / 512B Page size
                }
                bool SEMPER_SPI_HF = false; // Semper HF/SPI version
                if ((int)MyFlashDevice.MFG_CODE == 0x34) {
                    if ((int)MyFlashDevice.ID1 == 0x6B && (int)MyFlashDevice.ID2 == 0x19)
                        SEMPER_SPI_HF = true;
                    if ((int)MyFlashDevice.ID1 == 0x6B && (int)MyFlashDevice.ID2 == 0x1A)
                        SEMPER_SPI_HF = true;
                    if ((int)MyFlashDevice.ID1 == 0x6B && (int)MyFlashDevice.ID2 == 0x1B)
                        SEMPER_SPI_HF = true;
                    if ((int)MyFlashDevice.ID1 == 0x6A && (int)MyFlashDevice.ID2 == 0x19)
                        SEMPER_SPI_HF = true;
                    if ((int)MyFlashDevice.ID1 == 0x6A && (int)MyFlashDevice.ID2 == 0x1A)
                        SEMPER_SPI_HF = true;
                    if ((int)MyFlashDevice.ID1 == 0x6A && (int)MyFlashDevice.ID2 == 0x1B)
                        SEMPER_SPI_HF = true;
                }
                if (SEMPER_SPI_HF) {
                    SQIBUS_WriteEnable();
                    byte[] argReadBuffer5 = null;
                    this.SQIBUS_WriteRead(new byte[] { 0x71, 0x80, 0, 4, 0x18 }, ReadBuffer: ref argReadBuffer5); // Enables 512-byte buffer
                    SQIBUS_WriteEnable();
                    byte[] argReadBuffer6 = null;
                    this.SQIBUS_WriteRead(new byte[] { 0x71, 0x80, 0, 3, 0x80 }, ReadBuffer: ref argReadBuffer6); // Enables 4-byte mode
                }
            }
        }

        private void Winbond_EnableQUAD() {
            PrintConsole?.Invoke("Entering QPI mode for Winbond device");
            byte[] argReadBuffer = null;
            this.SQIBUS_WriteRead(new byte[] { 0x50 }, ReadBuffer: ref argReadBuffer); // WREN VOLATILE
            byte[] argReadBuffer1 = null;
            this.SQIBUS_WriteRead(new byte[] { 0x1, 0, 2 }, ReadBuffer: ref argReadBuffer1); // WRSR(0,2) - Sets QE bit
            var sr = new byte[1];
            this.SQIBUS_WriteRead(new byte[] { 0x35 }, ref sr); // Read SR-2
            if (((sr[0] & 2) >> 1) == 1) {
                PrintConsole?.Invoke("QE bit set in Status Register-2");
                byte[] argReadBuffer2 = null;
                this.SQIBUS_WriteRead(new byte[] { 0x38 }, ReadBuffer: ref argReadBuffer2);
                Utilities.Sleep(20);
                SQI_DEVICE_MODE = SPI.SQI_IO_MODE.SPI_QUAD;
                PrintConsole?.Invoke("IO mode switched to SPI/QUAD (1-bit/4-bit)");
            } else {
                PrintConsole?.Invoke("Error: failed to set the QE bit");
            }
        }

        private bool EnableWinbondSQIMode() {
            try {
                SQIBUS_WriteEnable();
                byte[] argReadBuffer = null;
                this.SQIBUS_WriteRead(new byte[] { MyFlashDevice.OP_COMMANDS.WRSR, 0, 2 }, ref argReadBuffer); // 0x01 00 02
                var status_reg = new byte[1];
                WaitUntilReady();
                this.SQIBUS_WriteRead(new byte[] { 0x35 }, ref status_reg); // 0x5
                if ((status_reg[0] & 2) == 2)
                    return true; // QE bit is set
            } catch {
            }
            return false; // Quad mode is not enabled or supported
        }

        private bool DisableWinbondSQIMode() {
            try {
                SQIBUS_WriteEnable();
                byte[] argReadBuffer = null;
                this.SQIBUS_WriteRead(new byte[] { MyFlashDevice.OP_COMMANDS.WRSR, 0, 0 }, ref argReadBuffer); // 0x01 00 02
                var status_reg = new byte[1];
                WaitUntilReady();
                this.SQIBUS_WriteRead(new byte[] { 0x35 }, ref status_reg);
                if ((status_reg[0] & 2)==0)
                    return true; // QE bit is unset
            } catch {
            }
            return false;
        }

        public Device GetDevice {
            get {
                return this.MyFlashDevice;
            }
        }

        public string DeviceName {
            get {

                if (MyFlashStatus == DeviceStatus.Supported) {
                    return MyFlashDevice.NAME;
                } else if (MyFlashStatus == DeviceStatus.NotSupported) {
                    return MyFlashDevice.MFG_CODE.ToString("X").PadLeft(2, '0') + " " + MyFlashDevice.ID1.ToString("X").PadLeft(4, '0');
                } else {
                    return "No Flash Detected";
                }
            }
        }

        public long DeviceSize {
            get {
                if (!(MyFlashStatus == DeviceStatus.Supported))
                    return 0L;
                return MyFlashDevice.FLASH_SIZE;
            }
        }

        public uint SectorSize(uint sector) {
            if (!(MyFlashStatus == DeviceStatus.Supported))
                return 0U;
            if (MyFlashDevice.ERASE_REQUIRED) {
                return MyFlashDevice.ERASE_SIZE;
            } else {
                return (uint)MyFlashDevice.FLASH_SIZE;
            }
        }

        public long SectorFind(uint sector_index) {
            if (sector_index == 0L)
                return 0L; // Addresses start at the base address 
            return SectorSize(0U) * sector_index;
        }

        public uint SectorCount() {
            if (MyFlashStatus == DeviceStatus.Supported) {
                uint EraseSize = MyFlashDevice.ERASE_SIZE;
                if (EraseSize == 0L)
                    return 1U;
                uint FlashSize = (uint)DeviceSize;
                if (FlashSize < EraseSize)
                    return 1U;
                return (uint)(int)(FlashSize / (double)EraseSize);
            }
            return 0U;
        }

        public byte[] ReadData(long flash_offset, long data_count) {
            var data_to_read = new byte[(int)(data_count - 1L + 1)];
            byte READ_CMD;
            byte DUMMY = 0; // Number of dummy clock cycles
            if (SQI_DEVICE_MODE == SPI.SQI_IO_MODE.SPI_ONLY) { // DUAL/QUAD require dummy bits and different read command
                READ_CMD = MyFlashDevice.OP_COMMANDS.READ;
            } else if (SQI_DEVICE_MODE == SPI.SQI_IO_MODE.DUAL_ONLY) {
                READ_CMD = MyFlashDevice.OP_COMMANDS.DUAL_READ;
                DUMMY = MyFlashDevice.SQI_DUMMY;
            } else if (SQI_DEVICE_MODE == SPI.SQI_IO_MODE.SPI_QUAD) {
                READ_CMD = MyFlashDevice.OP_COMMANDS.QUAD_READ;
                DUMMY = MyFlashDevice.SPI_DUMMY; // This needs to be SPI, since dummy bits are shifted before IO is QUAD
            } else { // We are in quad mode
                READ_CMD = MyFlashDevice.OP_COMMANDS.QUAD_READ;
                DUMMY = MyFlashDevice.SQI_DUMMY;
            }
            var setup_class = new ReadSetupPacket(READ_CMD, (uint)flash_offset, (uint)data_to_read.Length, (byte)MyFlashDevice.AddressBytes);
            setup_class.SPI_MODE = SQI_DEVICE_MODE;
            setup_class.DUMMY = DUMMY;
            bool result = FCUSB.USB_SETUP_BULKIN(USBREQ.SQI_RD_FLASH, setup_class.ToBytes(), ref data_to_read, 0U);
            if (!result)
                return null;
            return data_to_read;
        }

        public bool WriteData(long flash_offset, byte[] data_out, WriteParameters Params) {
            uint DataToWrite = (uint)data_out.Length;
            uint PacketSize = 8192U;
            int Loops = (int)Math.Ceiling(DataToWrite / (double)PacketSize); // Calcuates iterations
            byte PROG_CMD;
            if (SQI_DEVICE_MODE == SPI.SQI_IO_MODE.SPI_ONLY) {
                PROG_CMD = MyFlashDevice.OP_COMMANDS.PROG;
            } else if (SQI_DEVICE_MODE == SPI.SQI_IO_MODE.DUAL_ONLY) {
                PROG_CMD = MyFlashDevice.OP_COMMANDS.DUAL_PROG;
            } else if (SQI_DEVICE_MODE == SPI.SQI_IO_MODE.SPI_QUAD) {
                PROG_CMD = MyFlashDevice.OP_COMMANDS.QUAD_PROG;
            } else { // We are in quad mode
                PROG_CMD = MyFlashDevice.OP_COMMANDS.QUAD_PROG;
            }
            for (int i = 0, loopTo = Loops - 1; i <= loopTo; i++) {
                int BufferSize = (int)DataToWrite;
                if (BufferSize > PacketSize)
                    BufferSize = (int)PacketSize;
                var sector_data = new byte[BufferSize];
                Array.Copy(data_out, i * PacketSize, sector_data, 0L, sector_data.Length);
                var setup_class = new WriteSetupPacket(MyFlashDevice, (uint)flash_offset, (uint)BufferSize);
                setup_class.SPI_MODE = (FlashMemory.SPI_QUAD_SUPPORT)SQI_DEVICE_MODE;
                setup_class.CMD_PROG = PROG_CMD;
                bool result = FCUSB.USB_SETUP_BULKOUT(USBREQ.SQI_WR_FLASH, setup_class.ToBytes(), sector_data, 0U);
                if (!result)
                    return false;
                Utilities.Sleep(10);
                FCUSB.USB_WaitForComplete();
                flash_offset += sector_data.Length;
                DataToWrite = (uint)(DataToWrite - sector_data.Length);
            }

            return true;
        }

        private byte[] SQI_GetSetup(byte cmd, uint flash_offset) {
            var payload = new byte[MyFlashDevice.AddressBytes + 1];
            payload[0] = cmd;
            if (MyFlashDevice.AddressBytes == 4) {
                payload[1] = (byte)((flash_offset & 0xFF000000L) >> 24);
                payload[2] = (byte)((flash_offset & 0xFF0000L) >> 16);
                payload[3] = (byte)((flash_offset & 0xFF00L) >> 8);
                payload[4] = (byte)(flash_offset & 0xFFL);
            } else if (MyFlashDevice.AddressBytes == 3) {
                payload[1] = (byte)((flash_offset & 0xFF0000L) >> 16);
                payload[2] = (byte)((flash_offset & 0xFF00L) >> 8);
                payload[3] = (byte)(flash_offset & 0xFFL);
            } else if (MyFlashDevice.AddressBytes == 2) {
                payload[1] = (byte)((flash_offset & 0xFF00L) >> 8);
                payload[2] = (byte)(flash_offset & 0xFFL);
            }
            return payload;
        }

        public bool SectorErase(uint sector_index) {
            if (!MyFlashDevice.ERASE_REQUIRED)
                return true; // Erase not needed
            uint flash_offset = (uint)SectorFind(sector_index);
            SQIBUS_WriteEnable();
            var DataToWrite = this.GetArrayWithCmdAndAddr(MyFlashDevice.OP_COMMANDS.SE, flash_offset); // 0xD8
            byte[] argReadBuffer = null;
            SQIBUS_WriteRead(DataToWrite, ref argReadBuffer);
            WaitUntilReady();
            return true;
        }

        public bool SectorWrite(uint sector_index, byte[] data, WriteParameters Params) {
            uint Addr32 = (uint)this.SectorFind(sector_index);
            return WriteData(Addr32, data, Params);
        }

        public bool EraseDevice() {
            var m = "Erasing entire flash device, total size: {0} bytes (this may take a moment)";
            PrintConsole?.Invoke(string.Format(m, DeviceSize.ToString("N0")));
            var erase_timer = new Stopwatch();
            erase_timer.Start();
            if (MyFlashDevice.ProgramMode == SPI_ProgramMode.Atmel45Series) {
                byte[] argReadBuffer = null;
                this.SQIBUS_WriteRead(new byte[] { 0xC7, 0x94, 0x80, 0x9A }, ref argReadBuffer);
            } else if (MyFlashDevice.ProgramMode == SPI_ProgramMode.SPI_EEPROM) {
            } else if (MyFlashDevice.ProgramMode == SPI_ProgramMode.Nordic) {
            } else {
                switch (MyFlashDevice.CHIP_ERASE) {
                    case EraseMethod.Standard: {
                            SQIBUS_WriteEnable();
                            byte[] argReadBuffer1 = null;
                            SQIBUS_WriteRead(new byte[] { MyFlashDevice.OP_COMMANDS.BE }, ref argReadBuffer1); // &HC7
                            if (MyFlashDevice.SEND_RDFS)
                                ReadFlagStatusRegister();
                            WaitUntilReady();
                            break;
                        }
                    case EraseMethod.BySector: {
                            uint SectorCount = MyFlashDevice.Sector_Count;
                            SetProgress?.Invoke(0);
                            for (uint i = 0U, loopTo = (uint)(SectorCount - 1L); i <= loopTo; i++) {
                                if (!this.SectorErase(i)) {
                                    SetProgress?.Invoke(0);
                                    return false; // Error erasing sector
                                } else {
                                    float progress = (float)(i / (double)SectorCount * 100d);
                                    SetProgress?.Invoke((int)Math.Floor(progress));
                                }
                            }
                            SetProgress?.Invoke(0); // Device successfully erased
                            break;
                        }
                    case EraseMethod.DieErase: {
                            EraseDie();
                            break;
                        }
                    case EraseMethod.Micron: {
                            var internal_timer = new Stopwatch();
                            internal_timer.Start();
                            SQIBUS_WriteEnable(); // Try Chip Erase first
                            byte[] argReadBuffer2 = null;
                            this.SQIBUS_WriteRead(new byte[] { MyFlashDevice.OP_COMMANDS.BE }, ref argReadBuffer2);
                            if (MyFlashDevice.SEND_RDFS)
                                ReadFlagStatusRegister();
                            WaitUntilReady();
                            internal_timer.Stop();
                            if (internal_timer.ElapsedMilliseconds < 1000L) { // Command not supported, use DIE ERASE instead
                                EraseDie();
                            }
                            break;
                        }
                }
            }
            PrintConsole?.Invoke(string.Format("Flash erase complete in {0} seconds", (erase_timer.ElapsedMilliseconds / 1000).ToString("N2")));
            return true;
        }
        // Reads the SPI status register and waits for the device to complete its current operation
        public void WaitUntilReady() {
            try {
                var IO = MULTI_IO_MODE.Single;
                switch (SQI_DEVICE_MODE) {
                    case SPI.SQI_IO_MODE.QUAD_ONLY: {
                            IO = MULTI_IO_MODE.Quad;
                            break;
                        }

                    case SPI.SQI_IO_MODE.DUAL_ONLY: {
                            IO = MULTI_IO_MODE.Dual;
                            break;
                        }
                }

                var sr = new byte[1];
                if (MyFlashDevice.SEND_RDFS) {
                    SQIBUS_SlaveSelect_Enable();
                    this.SQIBUS_WriteData(new byte[] { MyFlashDevice.OP_COMMANDS.RDFR }, IO);
                    do
                        SQIBUS_ReadData(ref sr, IO);
                    while ((sr[0] >> 7 & 1) == 0);
                    SQIBUS_SlaveSelect_Disable();
                }

                SQIBUS_SlaveSelect_Enable();
                this.SQIBUS_WriteData(new byte[] { MyFlashDevice.OP_COMMANDS.RDSR }, IO);
                do
                    SQIBUS_ReadData(ref sr, IO);
                while ((sr[0] & 1) == 1);
                SQIBUS_SlaveSelect_Disable();
            } catch {
            }
        }

        public void SQIBUS_Setup(SQI_SPEED bus_speed) {
            int clock_div = 0; // Divides the clock speed
            if (FCUSB.HWBOARD == FCUSB_BOARD.Professional_PCB5 | FCUSB.HWBOARD == FCUSB_BOARD.Mach1) {
                if (bus_speed == SQI_SPEED.MHZ_40) {
                    PrintConsole?.Invoke(string.Format("SQI clock set to: {0}", "40 MHz"));
                } else if (bus_speed == SQI_SPEED.MHZ_20) {
                    PrintConsole?.Invoke(string.Format("SQI clock set to: {0}", "20 MHz"));
                    clock_div = 1;
                } else if (bus_speed == SQI_SPEED.MHZ_10) {
                    PrintConsole?.Invoke(string.Format("SQI clock set to: {0}", "10 MHz"));
                    clock_div = 2;
                } else if (bus_speed == SQI_SPEED.MHZ_5) {
                    PrintConsole?.Invoke(string.Format("SQI clock set to: {0}", "5 MHz"));
                    clock_div = 3;
                } else if (bus_speed == SQI_SPEED.MHZ_1) {
                    PrintConsole?.Invoke(string.Format("SQI clock set to: {0}", "1 MHz"));
                    clock_div = 4;
                }
            } else {
                PrintConsole?.Invoke(string.Format("SQI clock set to: {0}", "1 MHz"));
            }
            bool result = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.SQI_SETUP, null, (uint)clock_div);
            Utilities.Sleep(50); // Allow time for device to change IO
        }

        public bool SQIBUS_WriteEnable() {
            byte[] argReadBuffer = null;
            if (this.SQIBUS_WriteRead(new byte[] { MyFlashDevice.OP_COMMANDS.WREN }, ref argReadBuffer) == 1L) {
                return true;
            } else {
                return false;
            }
        }

        public void SQIBUS_SendCommand(byte spi_cmd) {
            bool result = SQIBUS_WriteEnable();
            if (!result)
                return;
            byte[] argReadBuffer = null;
            SQIBUS_WriteRead(new byte[] { spi_cmd }, ref argReadBuffer);
        }

        public uint SQIBUS_WriteRead(byte[] WriteBuffer, ref byte[] ReadBuffer) {
            uint TotalBytesTransfered = 0U;
            SQIBUS_SlaveSelect_Enable();
            if (WriteBuffer is object) {
                bool Result = SQIBUS_WriteData(WriteBuffer, SQI_IO_MODE);
                if (Result)
                    TotalBytesTransfered = (uint)(TotalBytesTransfered + WriteBuffer.Length);
            }
            if (ReadBuffer is object) {
                bool Result = SQIBUS_ReadData(ref ReadBuffer, SQI_IO_MODE);
                if (Result)
                    TotalBytesTransfered = (uint)(TotalBytesTransfered + ReadBuffer.Length);
            }
            SQIBUS_SlaveSelect_Disable();
            return TotalBytesTransfered;
        }

        public uint SQIBUS_Write(byte[] WriteBuffer) {
             uint TotalBytesTransfered = 0U;
            SQIBUS_SlaveSelect_Enable();
            if (WriteBuffer is object) {
                bool Result = SQIBUS_WriteData(WriteBuffer, SQI_IO_MODE);
                if (Result)
                    TotalBytesTransfered = (uint)(TotalBytesTransfered + WriteBuffer.Length);
            }
            SQIBUS_SlaveSelect_Disable();
            return TotalBytesTransfered;
        }
        // Makes the CS/SS pin go low
        private void SQIBUS_SlaveSelect_Enable() {
            FCUSB.USB_CONTROL_MSG_OUT(USBREQ.SQI_SS_ENABLE);
        }
        // Releases the CS/SS pin
        private void SQIBUS_SlaveSelect_Disable() {
            FCUSB.USB_CONTROL_MSG_OUT(USBREQ.SQI_SS_DISABLE);
        }

        private bool SQIBUS_WriteData(byte[] DataOut, MULTI_IO_MODE io_mode) {
            uint value_index = (uint)((uint)io_mode << 24 | (long)(DataOut.Length & 0xFFFFFF));
            bool Success = FCUSB.USB_SETUP_BULKOUT(USBREQ.SQI_WR_DATA, null, DataOut, value_index);
            Utilities.Sleep(2);
            return Success;
        }

        private bool SQIBUS_ReadData(ref byte[] Data_In, MULTI_IO_MODE io_mode) {
            uint value_index = (uint)((uint)io_mode << 24 | (long)(Data_In.Length & 0xFFFFFF));
            bool Success = FCUSB.USB_SETUP_BULKIN(USBREQ.SQI_RD_DATA, null, ref Data_In, value_index);
            return Success;
        }

        private byte[] GetArrayWithCmdAndAddr(byte cmd, uint addr_offset) {
            var addr_data = BitConverter.GetBytes(addr_offset);
            Array.Resize(ref addr_data, MyFlashDevice.AddressBytes);
            Array.Reverse(addr_data);
            var data_out = new byte[MyFlashDevice.AddressBytes + 1];
            data_out[0] = cmd;
            for (int i = 1, loopTo = data_out.Length - 1; i <= loopTo; i++)
                data_out[i] = addr_data[i - 1];
            return data_out;
        }
        // This writes to the SR (multi-bytes can be input to write as well)
        public bool WriteStatusRegister(byte[] NewValues) {
            try {
                if (NewValues is null)
                    return false;
                SQIBUS_WriteEnable(); // Some devices such as AT25DF641 require the WREN and the status reg cleared before we can write data
                if (MyFlashDevice.SEND_EWSR) {
                    byte[] argReadBuffer = null;
                    this.SQIBUS_WriteRead(new byte[] { MyFlashDevice.OP_COMMANDS.EWSR }, ref argReadBuffer); // Send the command that we are going to enable-write to register
                    System.Threading.Thread.Sleep(20); // Wait a brief moment
                }

                var cmd = new byte[NewValues.Length + 1];
                cmd[0] = MyFlashDevice.OP_COMMANDS.WRSR;
                Array.Copy(NewValues, 0, cmd, 1, NewValues.Length);
                byte[] argReadBuffer1 = null;
                if (!(SQIBUS_WriteRead(cmd, ref argReadBuffer1) == cmd.Length))
                    return false;
                return true;
            } catch {
                return false;
            }
        }

        public byte[] ReadStatusRegister(int Count = 1) {
            try {
                var Output = new byte[Count];
                this.SQIBUS_WriteRead(new byte[] { MyFlashDevice.OP_COMMANDS.RDSR }, ref Output);
                return Output;
            } catch {
                return null;
            } // Erorr
        }

        private void ReadFlagStatusRegister() {
            Utilities.Sleep(10);
            var flag = new byte[] { 0 };
            do
                SQIBUS_WriteRead(new byte[] { MyFlashDevice.OP_COMMANDS.RDFR }, ref flag);
            while (((flag[0] >> 7) & 1) != 1);
        }

        private void EraseDie() {
            uint die_size = 0x2000000U;
            uint die_count = (uint)((double)MyFlashDevice.FLASH_SIZE / die_size);
            for (uint x = 1U, loopTo = die_count; x <= loopTo; x++) {
                PrintConsole?.Invoke(string.Format("Erasing flash die index: {0} ({1} bytes)", x.ToString(), die_size.ToString("N0")));
                var die_addr = Utilities.Bytes.FromUInt32((uint)((x - 1L) * die_size));
                SQIBUS_WriteEnable();
                byte[] argReadBuffer = null;
                this.SQIBUS_WriteRead(new byte[] { MyFlashDevice.OP_COMMANDS.DE, die_addr[0], die_addr[1], die_addr[1], die_addr[1] }, ref argReadBuffer); // &HC4
                Utilities.Sleep(1000);
                if (MyFlashDevice.SEND_RDFS)
                    ReadFlagStatusRegister();
                WaitUntilReady();
            }
        }

        public void ResetDevice() {
            byte[] argReadBuffer = null;
            this.SQIBUS_WriteRead(new byte[] { 0xF0 }, ReadBuffer: ref argReadBuffer); // SPI RESET COMMAND
            // Other commands: 0x66 and 0x99
            Utilities.Sleep(10);
        }
    }

    public enum MULTI_IO_MODE : byte {
        Single = 1,
        Dual = 2,
        Quad = 4
    }

    internal enum SQI_IO_MODE : byte {
        SPI_ONLY = 0, // SETUP=SPI;DATA=SPI
        QUAD_ONLY = 1, // SETUP=QUAD;DATA=SPI
        DUAL_ONLY = 2, // SET=DUAL;DATA=DUAL
        SPI_QUAD = 3, // SETUP=SPI,DATA=QUAD
        SPI_DUAL = 4 // SETUP=DUAL;DATA=DUAL
    }
}