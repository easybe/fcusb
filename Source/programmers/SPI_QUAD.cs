// COPYRIGHT EMBEDDED COMPUTERS LLC 2018 - ALL RIGHTS RESERVED
// THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
// CONTACT EMAIL: support@embeddedcomputers.net
// ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
// ACKNOWLEDGEMENT: USB driver functionality provided by LibUsbDotNet (sourceforge.net/projects/libusbdotnet) 

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace FlashcatUSB.SPI {
    // This class is used for devices with DUAL and QUAD I/O modes
    public class SQI_Programmer : FlashcatUSB.MemoryDeviceUSB {




        private FlashcatUSB.USB.FCUSB_DEVICE FCUSB;

        public event PrintConsoleEventHandler PrintConsole;

        public delegate void PrintConsoleEventHandler(string msg);

        public event SetProgressEventHandler SetProgress;

        public delegate void SetProgressEventHandler(int percent);

        public FlashcatUSB.FlashMemory.SPI_NOR MyFlashDevice { get; set; } // Contains the definition of the Flash device that is connected
        public FlashcatUSB.USB.DeviceStatus MyFlashStatus { get; set; } = FlashcatUSB.USB.DeviceStatus.NotDetected;
        public int DIE_SELECTED { get; set; } = 0;
        public MULTI_IO_MODE SQI_IO_MODE { get; set; } // IO=1/2/4 bits per clock cycle
        private SQI_IO_MODE SQI_DEVICE_MODE { get; set; } // 0=SPI_ONLY,1=QUAD_ONLY,2=DUAL_ONLY,3=SPI_QUAD,4=SPI_DUAL

        public SQI_Programmer(FlashcatUSB.USB.FCUSB_DEVICE parent_if) {
            FCUSB = parent_if;
        }

        public bool DeviceInit() {
            MyFlashStatus = FlashcatUSB.USB.DeviceStatus.NotDetected;
            SPI.SPI_IDENT DEVICEID = null;
            DEVICEID = ReadDeviceID(MULTI_IO_MODE.Single); // Read ID first, then see if we can access QUAD/DUAL
            DEVICEID = ReadDeviceID(MULTI_IO_MODE.Quad);
            if (!DEVICEID.DETECTED)
                DEVICEID = ReadDeviceID(MULTI_IO_MODE.Dual);
            if (!DEVICEID.DETECTED)
                DEVICEID = ReadDeviceID(MULTI_IO_MODE.Single);
            if (DEVICEID.DETECTED) {
                PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("spi_successfully_opened_sqi"));
                string RDID_Str = "0x" + Conversion.Hex(DEVICEID.MANU).PadLeft(2, '0') + Conversion.Hex(DEVICEID.RDID).PadLeft(4, '0');
                PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("spi_connected_to_flash_sqi"), RDID_Str));
                ushort ID1 = (ushort)(DEVICEID.RDID >> 16);
                ushort ID2 = (ushort)((long)DEVICEID.RDID & 0xFFFFL);
                MyFlashDevice = (FlashcatUSB.FlashMemory.SPI_NOR)FlashcatUSB.MainApp.FlashDatabase.FindDevice(DEVICEID.MANU, ID1, ID2, FlashcatUSB.FlashMemory.MemoryType.SERIAL_NOR, DEVICEID.FMY);
                if (MyFlashDevice is object) {
                    MyFlashStatus = FlashcatUSB.USB.DeviceStatus.Supported;
                    PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("flash_detected"), DeviceName, Strings.Format((object)DeviceSize, "#,###")));
                    PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("spi_mode_sqi"));
                    if (MyFlashDevice.SQI_MODE == FlashcatUSB.FlashMemory.SPI_QUAD_SUPPORT.QUAD) {
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
                    } else if (MyFlashDevice.SQI_MODE == FlashcatUSB.FlashMemory.SPI_QUAD_SUPPORT.SPI_QUAD) {
                        PrintConsole?.Invoke("Detected Flash in SPI mode (1-bit)");
                        byte[] argReadBuffer = null;
                        this.SQIBUS_WriteRead(new[] { 0xF0 }, ReadBuffer: ref argReadBuffer); // SPI RESET COMMAND
                        FlashcatUSB.Utilities.Main.Sleep(20);
                        if ((int)DEVICEID.MANU == 0xEF) { // Winbond
                            // Winbond_EnableQUAD()
                        } else if ((int)DEVICEID.MANU == 1) { // Cypress/Spansion
                            var sr2 = new byte[1];
                            this.SQIBUS_WriteRead(new[] { 0x35 }, ref sr2);
                            if (Conversions.ToBoolean(sr2[0] >> 1 & 1)) {
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
                    if (MyFlashDevice.VENDOR_SPECIFIC == FlashcatUSB.FlashMemory.VENDOR_FEATURE.NotSupported) { // We don't want to do this for vendor enabled devices
                        this.WriteStatusRegister(new[] { 0 });
                        FlashcatUSB.Utilities.Main.Sleep(100); // Needed, some devices will lock up if else.
                    }

                    if (MyFlashDevice.SEND_EN4B)
                        this.SQIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.EN4B); // 0xB7
                    this.SQIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.ULBPR); // 0x98 (global block unprotect)
                    LoadVendorSpecificConfigurations(); // Some devices may need additional configurations
                    return true;
                } else {
                    PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("unknown_device_email"));
                    MyFlashDevice = new FlashcatUSB.FlashMemory.SPI_NOR("Unknown", FlashcatUSB.FlashMemory.VCC_IF.SERIAL_3V, 0U, DEVICEID.MANU, (ushort)((long)DEVICEID.RDID & 0xFFFFL));
                    MyFlashStatus = FlashcatUSB.USB.DeviceStatus.NotSupported;
                    return false;
                }
            } else {
                MyFlashStatus = FlashcatUSB.USB.DeviceStatus.NotDetected;
                PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("spi_flash_not_detected"));
                return false;
            }
        }

        private SPI.SPI_IDENT ReadDeviceID(MULTI_IO_MODE mode) {
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
            if (SQIBUS_WriteRead(new[] { id_code }, ref rdid) == 7L) { // MULTIPLE I/O READ ID
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
                    this.SQIBUS_WriteRead(new[] { 0x42, 0, 0, 0, 0, 0, 0 }, ReadBuffer: ref argReadBuffer); // 6 blank bytes
                    FlashcatUSB.Utilities.Main.Sleep(200);
                } else if ((int)MyFlashDevice.ID1 == 0x2602) { // SST26VF032
                    SQIBUS_WriteEnable();
                    byte[] argReadBuffer1 = null;
                    this.SQIBUS_WriteRead(new[] { 0x42, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, ReadBuffer: ref argReadBuffer1); // 10 blank bytes
                    FlashcatUSB.Utilities.Main.Sleep(200);
                }
            } else if ((int)MyFlashDevice.MFG_CODE == 0x9D) { // ISSI
                this.WriteStatusRegister(new[] { 0 }); // Erase protection bits
            }

            if ((int)MyFlashDevice.MFG_CODE == 0xEF && (int)MyFlashDevice.ID1 == 0x4018) {
                byte[] argReadBuffer2 = null;
                this.SQIBUS_WriteRead(new[] { 0xC2, 1 }, ReadBuffer: ref argReadBuffer2);
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
                    this.SQIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.EWSR }, ReadBuffer: ref argReadBuffer3);
                    byte[] argReadBuffer4 = null;
                    this.SQIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.WRSR, 0, 0, 0x88, 0x18 }, ReadBuffer: ref argReadBuffer4); // Set sector to uniform 256KB / 512B Page size
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
                    this.SQIBUS_WriteRead(new[] { 0x71, 0x80, 0, 4, 0x18 }, ReadBuffer: ref argReadBuffer5); // Enables 512-byte buffer
                    SQIBUS_WriteEnable();
                    byte[] argReadBuffer6 = null;
                    this.SQIBUS_WriteRead(new[] { 0x71, 0x80, 0, 3, 0x80 }, ReadBuffer: ref argReadBuffer6); // Enables 4-byte mode
                }
            }
        }

        private void Winbond_EnableQUAD() {
            PrintConsole?.Invoke("Entering QPI mode for Winbond device");
            byte[] argReadBuffer = null;
            this.SQIBUS_WriteRead(new[] { 0x50 }, ReadBuffer: ref argReadBuffer); // WREN VOLATILE
            byte[] argReadBuffer1 = null;
            this.SQIBUS_WriteRead(new[] { 0x1, 0, 2 }, ReadBuffer: ref argReadBuffer1); // WRSR(0,2) - Sets QE bit
            var sr = new byte[1];
            this.SQIBUS_WriteRead(new[] { 0x35 }, ref sr); // Read SR-2
            if (Conversions.ToBoolean((sr[0] & 2) >> 1)) {
                PrintConsole?.Invoke("QE bit set in Status Register-2");
                byte[] argReadBuffer2 = null;
                this.SQIBUS_WriteRead(new[] { 0x38 }, ReadBuffer: ref argReadBuffer2);
                FlashcatUSB.Utilities.Main.Sleep(20);
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
                this.SQIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.WRSR, 0, 2 }, ref argReadBuffer); // 0x01 00 02
                var status_reg = new byte[1];
                WaitUntilReady();
                this.SQIBUS_WriteRead(new[] { 0x35 }, ref status_reg); // 0x5
                if (Conversions.ToBoolean(status_reg[0] & Conversions.ToShort(2 == 2)))
                    return true; // QE bit is set
            } catch (Exception ex) {
            }

            return false; // Quad mode is not enabled or supported
        }

        private bool DisableWinbondSQIMode() {
            try {
                SQIBUS_WriteEnable();
                byte[] argReadBuffer = null;
                this.SQIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.WRSR, 0, 0 }, ref argReadBuffer); // 0x01 00 02
                var status_reg = new byte[1];
                WaitUntilReady();
                this.SQIBUS_WriteRead(new[] { 0x35 }, ref status_reg);
                if (Conversions.ToBoolean(status_reg[0] & Conversions.ToShort(2 == 0)))
                    return true; // QE bit is unset
            } catch (Exception ex) {
            }

            return false;
        }

        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        internal string DeviceName {
            get {
                switch (MyFlashStatus) {
                    case FlashcatUSB.USB.DeviceStatus.Supported: {
                            return MyFlashDevice.NAME;
                        }

                    case FlashcatUSB.USB.DeviceStatus.NotSupported: {
                            return Conversion.Hex(MyFlashDevice.MFG_CODE).PadLeft(2, '0') + " " + Conversion.Hex(MyFlashDevice.ID1).PadLeft(4, '0');
                        }

                    default: {
                            return FlashcatUSB.MainApp.RM.GetString("no_flash_detected");
                        }
                }
            }
        }

        internal long DeviceSize {
            get {
                if (!(MyFlashStatus == FlashcatUSB.USB.DeviceStatus.Supported))
                    return 0L;
                return MyFlashDevice.FLASH_SIZE;
            }
        }

        internal uint SectorSize(uint sector, FlashcatUSB.FlashMemory.FlashArea memory_area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            if (!(MyFlashStatus == FlashcatUSB.USB.DeviceStatus.Supported))
                return 0U;
            if (MyFlashDevice.ERASE_REQUIRED) {
                return MyFlashDevice.ERASE_SIZE;
            } else {
                return (uint)MyFlashDevice.FLASH_SIZE;
            }
        }

        internal long SectorFind(uint sector_index, FlashcatUSB.FlashMemory.FlashArea memory_area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            if (sector_index == 0L)
                return 0L; // Addresses start at the base address 
            return SectorSize(0U, memory_area) * sector_index;
        }

        internal uint SectorCount() {
            if (MyFlashStatus == FlashcatUSB.USB.DeviceStatus.Supported) {
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

        internal byte[] ReadData(long flash_offset, long data_count, FlashcatUSB.FlashMemory.FlashArea memory_area = FlashcatUSB.FlashMemory.FlashArea.Main) {
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

            var setup_class = new SPI.ReadSetupPacket(READ_CMD, (uint)flash_offset, (uint)data_to_read.Length, (byte)MyFlashDevice.AddressBytes);
            setup_class.SPI_MODE = SQI_DEVICE_MODE;
            setup_class.DUMMY = DUMMY;
            bool result = FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.SQI_RD_FLASH, setup_class.ToBytes(), ref data_to_read, 0U);
            if (!result)
                return null;
            return data_to_read;
        }

        internal bool WriteData(long flash_offset, byte[] data_out, FlashcatUSB.WriteParameters Params = null) {
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
                var setup_class = new SPI.WriteSetupPacket(MyFlashDevice, (uint)flash_offset, (uint)BufferSize);
                setup_class.SPI_MODE = (FlashcatUSB.FlashMemory.SPI_QUAD_SUPPORT)SQI_DEVICE_MODE;
                setup_class.CMD_PROG = PROG_CMD;
                bool result = FCUSB.USB_SETUP_BULKOUT(FlashcatUSB.USB.USBREQ.SQI_WR_FLASH, setup_class.ToBytes(), sector_data, 0U);
                if (!result)
                    return false;
                FlashcatUSB.Utilities.Main.Sleep(10);
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

        internal bool SectorErase(uint sector_index, FlashcatUSB.FlashMemory.FlashArea memory_area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            if (!MyFlashDevice.ERASE_REQUIRED)
                return true; // Erase not needed
            uint flash_offset = (uint)SectorFind(sector_index, memory_area);
            SQIBUS_WriteEnable();
            var DataToWrite = this.GetArrayWithCmdAndAddr(MyFlashDevice.OP_COMMANDS.SE, flash_offset); // 0xD8
            byte[] argReadBuffer = null;
            SQIBUS_WriteRead(DataToWrite, ref argReadBuffer);
            WaitUntilReady();
            return true;
        }

        internal bool SectorWrite(uint sector_index, byte[] data, FlashcatUSB.WriteParameters Params = null) {
            uint Addr32 = (uint)this.SectorFind(sector_index, Params.Memory_Area);
            return WriteData(Addr32, data, Params);
        }

        internal bool EraseDevice() {
            if (MyFlashDevice.ProgramMode == FlashcatUSB.FlashMemory.SPI_ProgramMode.Atmel45Series) {
                byte[] argReadBuffer = null;
                this.SQIBUS_WriteRead(new[] { 0xC7, 0x94, 0x80, 0x9A }, ref argReadBuffer);
            } else if (MyFlashDevice.ProgramMode == FlashcatUSB.FlashMemory.SPI_ProgramMode.SPI_EEPROM) {
            } else if (MyFlashDevice.ProgramMode == FlashcatUSB.FlashMemory.SPI_ProgramMode.Nordic) {
            } else {
                PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("spi_erasing_flash_device"), Strings.Format((object)DeviceSize, "#,###")));
                var erase_timer = new Stopwatch();
                erase_timer.Start();
                switch (MyFlashDevice.CHIP_ERASE) {
                    case FlashcatUSB.FlashMemory.EraseMethod.Standard: {
                            SQIBUS_WriteEnable();
                            byte[] argReadBuffer1 = null;
                            this.SQIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.BE }, ref argReadBuffer1); // &HC7
                            if (MyFlashDevice.SEND_RDFS)
                                ReadFlagStatusRegister();
                            WaitUntilReady();
                            break;
                        }

                    case FlashcatUSB.FlashMemory.EraseMethod.BySector: {
                            uint SectorCount = MyFlashDevice.Sector_Count;
                            SetProgress?.Invoke(0);
                            for (uint i = 0U, loopTo = (uint)(SectorCount - 1L); i <= loopTo; i++) {
                                if (!this.SectorErase(i, FlashcatUSB.FlashMemory.FlashArea.NotSpecified)) {
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

                    case FlashcatUSB.FlashMemory.EraseMethod.DieErase: {
                            EraseDie();
                            break;
                        }

                    case FlashcatUSB.FlashMemory.EraseMethod.Micron: {
                            var internal_timer = new Stopwatch();
                            internal_timer.Start();
                            SQIBUS_WriteEnable(); // Try Chip Erase first
                            byte[] argReadBuffer2 = null;
                            this.SQIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.BE }, ref argReadBuffer2);
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

                PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("spi_erase_complete"), Strings.Format((object)((double)erase_timer.ElapsedMilliseconds / 1000d), "#.##")));
            }

            return true;
        }
        // Reads the SPI status register and waits for the device to complete its current operation
        internal void WaitUntilReady() {
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
                    this.SQIBUS_WriteData(new[] { MyFlashDevice.OP_COMMANDS.RDFR }, IO);
                    do
                        SQIBUS_ReadData(ref sr, IO);
                    while ((sr[0] >> 7 & 1) == 0);
                    SQIBUS_SlaveSelect_Disable();
                }

                SQIBUS_SlaveSelect_Enable();
                this.SQIBUS_WriteData(new[] { MyFlashDevice.OP_COMMANDS.RDSR }, IO);
                do
                    SQIBUS_ReadData(ref sr, IO);
                while ((sr[0] & 1) == 1);
                SQIBUS_SlaveSelect_Disable();
            } catch (Exception ex) {
            }
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        public void SQIBUS_Setup(SPI.SQI_SPEED bus_speed) {
            int clock_div = 0; // Divides the clock speed
            if (FCUSB.HWBOARD == FlashcatUSB.USB.FCUSB_BOARD.Professional_PCB5 | FCUSB.HWBOARD == FlashcatUSB.USB.FCUSB_BOARD.Mach1) {
                if (bus_speed == SPI.SQI_SPEED.MHZ_40) {
                    FlashcatUSB.MainApp.GUI.PrintConsole(string.Format("SQI clock set to: {0}", "40 MHz"));
                } else if (bus_speed == SPI.SQI_SPEED.MHZ_20) {
                    FlashcatUSB.MainApp.GUI.PrintConsole(string.Format("SQI clock set to: {0}", "20 MHz"));
                    clock_div = 1;
                } else if (bus_speed == SPI.SQI_SPEED.MHZ_10) {
                    FlashcatUSB.MainApp.GUI.PrintConsole(string.Format("SQI clock set to: {0}", "10 MHz"));
                    clock_div = 2;
                } else if (bus_speed == SPI.SQI_SPEED.MHZ_5) {
                    FlashcatUSB.MainApp.GUI.PrintConsole(string.Format("SQI clock set to: {0}", "5 MHz"));
                    clock_div = 3;
                } else if (bus_speed == SPI.SQI_SPEED.MHZ_1) {
                    FlashcatUSB.MainApp.GUI.PrintConsole(string.Format("SQI clock set to: {0}", "1 MHz"));
                    clock_div = 4;
                }
            } else {
                FlashcatUSB.MainApp.GUI.PrintConsole(string.Format("SQI clock set to: {0}", "1 MHz"));
            }

            bool result = FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.SQI_SETUP, null, (uint)clock_div);
            FlashcatUSB.Utilities.Main.Sleep(50); // Allow time for device to change IO
        }

        public bool SQIBUS_WriteEnable() {
            byte[] argReadBuffer = null;
            if (this.SQIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.WREN }, ref argReadBuffer) == 1L) {
                return true;
            } else {
                return false;
            }
        }

        public bool SQIBUS_SendCommand(byte spi_cmd) {
            bool we = SQIBUS_WriteEnable();
            if (!we)
                return false;
            byte[] argReadBuffer = null;
            return Conversions.ToBoolean(SQIBUS_WriteRead(new[] { spi_cmd }, ref argReadBuffer));
        }

        public uint SQIBUS_WriteRead(byte[] WriteBuffer, [Optional, DefaultParameterValue(null)] ref byte[] ReadBuffer) {
            if (WriteBuffer is null & ReadBuffer is null)
                return 0U;
            uint TotalBytesTransfered = 0U;
            SQIBUS_SlaveSelect_Enable();
            if (WriteBuffer is object) {
                int BytesWritten = 0;
                bool Result = SQIBUS_WriteData(WriteBuffer, SQI_IO_MODE);
                if (WriteBuffer.Length > 2048)
                    FlashcatUSB.Utilities.Main.Sleep(2);
                if (Result)
                    TotalBytesTransfered = (uint)(TotalBytesTransfered + WriteBuffer.Length);
            }

            if (ReadBuffer is object) {
                int BytesRead = 0;
                bool Result = SQIBUS_ReadData(ref ReadBuffer, SQI_IO_MODE);
                if (Result)
                    TotalBytesTransfered = (uint)(TotalBytesTransfered + ReadBuffer.Length);
            }

            SQIBUS_SlaveSelect_Disable();
            return TotalBytesTransfered;
        }
        // Makes the CS/SS pin go low
        private void SQIBUS_SlaveSelect_Enable() {
            try {
                FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.SQI_SS_ENABLE);
            } catch (Exception ex) {
            }
        }
        // Releases the CS/SS pin
        private void SQIBUS_SlaveSelect_Disable() {
            try {
                FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.SQI_SS_DISABLE);
            } catch (Exception ex) {
            }
        }

        private bool SQIBUS_WriteData(byte[] DataOut, MULTI_IO_MODE io_mode) {
            uint value_index = (uint)((uint)io_mode << 24 | (long)(DataOut.Length & 0xFFFFFF));
            bool Success = FCUSB.USB_SETUP_BULKOUT(FlashcatUSB.USB.USBREQ.SQI_WR_DATA, null, DataOut, value_index);
            FlashcatUSB.Utilities.Main.Sleep(2);
            return Success;
        }

        private bool SQIBUS_ReadData(ref byte[] Data_In, MULTI_IO_MODE io_mode) {
            uint value_index = (uint)((uint)io_mode << 24 | (long)(Data_In.Length & 0xFFFFFF));
            bool Success = FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.SQI_RD_DATA, null, ref Data_In, value_index);
            return Success;
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
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
                    this.SQIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.EWSR }, ref argReadBuffer); // Send the command that we are going to enable-write to register
                    System.Threading.Thread.Sleep(20); // Wait a brief moment
                }

                var cmd = new byte[NewValues.Length + 1];
                cmd[0] = MyFlashDevice.OP_COMMANDS.WRSR;
                Array.Copy(NewValues, 0, cmd, 1, NewValues.Length);
                byte[] argReadBuffer1 = null;
                if (!(SQIBUS_WriteRead(cmd, ref argReadBuffer1) == cmd.Length))
                    return false;
                return true;
            } catch (Exception ex) {
                return false;
            }
        }

        public byte[] ReadStatusRegister(int Count = 1) {
            try {
                var Output = new byte[Count];
                this.SQIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.RDSR }, ref Output);
                return Output;
            } catch (Exception ex) {
                return null;
            } // Erorr
        }

        private void ReadFlagStatusRegister() {
            FlashcatUSB.Utilities.Main.Sleep(10);
            var flag = new byte[] { 0 };
            do
                this.SQIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.RDFR }, ref flag);
            while (!(flag[0] >> 7 & 1));
        }

        private void EraseDie() {
            uint die_size = 0x2000000U;
            uint die_count = (uint)((double)MyFlashDevice.FLASH_SIZE / die_size);
            for (uint x = 1U, loopTo = die_count; x <= loopTo; x++) {
                PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("spi_erasing_die"), x.ToString(), Strings.Format((object)die_size, "#,###")));
                var die_addr = FlashcatUSB.Utilities.Bytes.Bytes.FromUInt32((uint)((x - 1L) * die_size));
                SQIBUS_WriteEnable();
                byte[] argReadBuffer = null;
                this.SQIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.DE, die_addr[0], die_addr[1], die_addr[1], die_addr[1] }, ref argReadBuffer); // &HC4
                FlashcatUSB.Utilities.Main.Sleep(1000);
                if (MyFlashDevice.SEND_RDFS)
                    ReadFlagStatusRegister();
                WaitUntilReady();
            }
        }

        public void ResetDevice() {
            byte[] argReadBuffer = null;
            this.SQIBUS_WriteRead(new[] { 0xF0 }, ReadBuffer: ref argReadBuffer); // SPI RESET COMMAND
            // Other commands: 0x66 and 0x99
            FlashcatUSB.Utilities.Main.Sleep(10);
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