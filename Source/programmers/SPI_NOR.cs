// COPYRIGHT EMBEDDEDCOMPUTERS.NET 2020 - ALL RIGHTS RESERVED
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
    public class SPI_Programmer : FlashcatUSB.MemoryDeviceUSB {
        private FlashcatUSB.USB.FCUSB_DEVICE FCUSB;

        public event PrintConsoleEventHandler PrintConsole;

        public delegate void PrintConsoleEventHandler(string msg);

        public event SetProgressEventHandler SetProgress;

        public delegate void SetProgressEventHandler(int percent);

        public FlashcatUSB.FlashMemory.SPI_NOR MyFlashDevice { get; set; } // Contains the definition of the Flash device that is connected
        public FlashcatUSB.USB.DeviceStatus MyFlashStatus { get; set; } = FlashcatUSB.USB.DeviceStatus.NotDetected;
        public int DIE_SELECTED { get; set; } = 0;
        public bool W25M121AV_Mode { get; set; } = false;
        public bool ExtendedPage { get; set; } = false; // Indicates we should use extended pages

        public SPI_Programmer(FlashcatUSB.USB.FCUSB_DEVICE parent_if) {
            FCUSB = parent_if;
        }

        internal bool DeviceInit() {
            W25M121AV_Mode = false;
            ExtendedPage = false;
            MyFlashStatus = FlashcatUSB.USB.DeviceStatus.NotDetected;
            byte[] argReadBuffer = null;
            this.SPIBUS_WriteRead(new[] { 0xC2, 0 }, ReadBuffer: ref argReadBuffer); // always select die 0
            bool ReadSuccess = false;
            var MFG = default(byte);
            var ID1 = default(ushort);
            var ID2 = default(ushort);
            var DEVICEID = ReadDeviceID(); // Sends RDID/REMS/RES command and reads back
            if (DEVICEID.MANU == 0xFF | DEVICEID.MANU == 0) { // Check REMS
                if (!(DEVICEID.REMS == 0xFFFF | DEVICEID.REMS == 0x0)) {
                    MFG = (byte)(DEVICEID.REMS >> 8);
                    ID1 = (ushort)(DEVICEID.REMS & 255);
                    ReadSuccess = true; // RDID did not return anything, but REMS did
                }
            } else if (DEVICEID.RDID == 0xFFFFFFFFU | DEVICEID.RDID == 0L) {
            } else {
                MFG = DEVICEID.MANU;
                ID1 = (ushort)(DEVICEID.RDID >> 16);
                ID2 = (ushort)(DEVICEID.RDID & 0xFFFFL);
                ReadSuccess = true;
            }

            if (ReadSuccess) {
                PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("spi_device_opened"));
                string RDID_Str = "0x" + Conversion.Hex(DEVICEID.MANU).PadLeft(2, '0') + Conversion.Hex((DEVICEID.RDID & 0xFFFF0000UL) >> 16).PadLeft(4, '0');
                string RDID2_Str = Conversion.Hex(DEVICEID.RDID & 0xFFFFL).PadLeft(4, '0');
                string REMS_Str = "0x" + Conversion.Hex(DEVICEID.REMS).PadLeft(4, '0');
                PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("spi_connected_to_flash_spi"), RDID_Str, REMS_Str));
                MyFlashDevice = (FlashcatUSB.FlashMemory.SPI_NOR)FlashcatUSB.MainApp.FlashDatabase.FindDevice(MFG, ID1, ID2, FlashcatUSB.FlashMemory.MemoryType.SERIAL_NOR, DEVICEID.FMY);
                if (MyFlashDevice is object) {
                    MyFlashStatus = FlashcatUSB.USB.DeviceStatus.Supported;
                    ResetDevice();
                    LoadDeviceConfigurations(); // Does device settings (4BYTE mode, unlock global block)
                    LoadVendorSpecificConfigurations(); // Some devices may need additional configurations
                    PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("flash_detected"), DeviceName, Strings.Format((object)DeviceSize, "#,###")));
                    PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("spi_flash_page_size"), Strings.Format((object)SectorSize(0U), "#,###")));
                    FlashcatUSB.Utilities.Main.Sleep(50);
                    return true;
                } else {
                    MyFlashDevice = new FlashcatUSB.FlashMemory.SPI_NOR("Unknown", FlashcatUSB.FlashMemory.VCC_IF.SERIAL_3V, 0U, DEVICEID.MANU, ID1);
                    MyFlashStatus = FlashcatUSB.USB.DeviceStatus.NotSupported;
                    return false;
                }
            } else {
                MyFlashStatus = FlashcatUSB.USB.DeviceStatus.NotDetected;
                PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("spi_flash_not_detected"));
                return false;
            }
        }

        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        public FlashcatUSB.FlashMemory.Device GetDevice {
            get {
                return MyFlashDevice;
            }
        }

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
                if (ExtendedPage) {
                    return (long)(MyFlashDevice.PAGE_COUNT * MyFlashDevice.PAGE_SIZE_EXTENDED);
                } else {
                    return MyFlashDevice.FLASH_SIZE;
                }
            }
        }

        internal uint SectorSize(uint sector) {
            if (!(MyFlashStatus == FlashcatUSB.USB.DeviceStatus.Supported))
                return 0U;
            if (MyFlashDevice.ERASE_REQUIRED) {
                if (ExtendedPage) {
                    int page_count = (int)((double)MyFlashDevice.ERASE_SIZE / (double)MyFlashDevice.PAGE_SIZE);
                    return (uint)((long)MyFlashDevice.PAGE_SIZE_EXTENDED * page_count);
                } else {
                    return MyFlashDevice.ERASE_SIZE;
                }
            } else {
                return (uint)MyFlashDevice.FLASH_SIZE;
            }
        }

        internal long SectorFind(uint sector_index) {
            if (sector_index == 0L)
                return 0L; // Addresses start at the base address 
            return SectorSize(0U) * sector_index;
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

        internal byte[] ReadData(long flash_offset, long data_count) {
            if (W25M121AV_Mode) {
                byte[] argReadBuffer = null;
                this.SPIBUS_WriteRead(new[] { 0xC2, 0 }, ReadBuffer: ref argReadBuffer);
                WaitUntilReady();
            }

            var data_to_read = new byte[(int)(data_count - 1L + 1)];
            uint bytes_left = (uint)data_count;
            uint buffer_size = 0U;
            uint array_ptr = 0U;
            byte read_cmd = MyFlashDevice.OP_COMMANDS.READ;
            byte dummy_clocks = 0;
            if (FlashcatUSB.MainApp.MySettings.SPI_FASTREAD && FCUSB.HasLogic) {
                read_cmd = MyFlashDevice.OP_COMMANDS.FAST_READ;
            }

            if (MyFlashDevice.ProgramMode == FlashcatUSB.FlashMemory.SPI_ProgramMode.Atmel45Series) {
                return AT45_ReadData((uint)flash_offset, (uint)data_count);
            } else if (MyFlashDevice.ProgramMode == FlashcatUSB.FlashMemory.SPI_ProgramMode.SPI_EEPROM) {
                if ((long)MyFlashDevice.ADDRESSBITS == 8L) { // Used on ST M95010 - M95040 (8bit) and ATMEL devices (AT25010A - AT25040A)
                    if (flash_offset > 255L)
                        read_cmd = (byte)(read_cmd | 8); // Used on M95040 / AT25040A
                    var setup_class = new ReadSetupPacket(read_cmd, (uint)(flash_offset & 255L), (uint)data_to_read.Length, (byte)MyFlashDevice.AddressBytes);
                    FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.SPI_READFLASH, setup_class.ToBytes(), ref data_to_read, 0U);
                } else {
                    var setup_class = new ReadSetupPacket(read_cmd, (uint)flash_offset, (uint)data_to_read.Length, (byte)MyFlashDevice.AddressBytes) { DUMMY = dummy_clocks };
                    FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.SPI_READFLASH, setup_class.ToBytes(), ref data_to_read, 0U);
                }
            } else { // Normal SPI READ
                if (FlashcatUSB.MainApp.MySettings.SPI_FASTREAD && FCUSB.HasLogic) {
                    dummy_clocks = MyFlashDevice.SPI_DUMMY;
                }

                if ((long)MyFlashDevice.STACKED_DIES > 1L) {
                    while (bytes_left != 0L) {
                        uint argflash_offset = (uint)flash_offset;
                        uint die_address = GetAddressForMultiDie(ref argflash_offset, ref bytes_left, ref buffer_size);
                        var die_data = new byte[(int)(buffer_size - 1L + 1)];
                        var setup_class = new ReadSetupPacket(read_cmd, die_address, (uint)die_data.Length, (byte)MyFlashDevice.AddressBytes) { DUMMY = dummy_clocks };
                        FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.SPI_READFLASH, setup_class.ToBytes(), ref die_data, 0U);
                        Array.Copy(die_data, 0L, data_to_read, array_ptr, die_data.Length);
                        array_ptr += buffer_size;
                    }
                } else {
                    var setup_class = new ReadSetupPacket(read_cmd, (uint)flash_offset, (uint)data_to_read.Length, (byte)MyFlashDevice.AddressBytes) { DUMMY = dummy_clocks };
                    FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.SPI_READFLASH, setup_class.ToBytes(), ref data_to_read, 0U);
                    FCUSB.USB_WaitForComplete();
                }
            }

            return data_to_read;
        }

        internal bool WriteData(long flash_offset, byte[] data_to_write, FlashcatUSB.WriteParameters Params = null) {
            if (W25M121AV_Mode) {
                byte[] argReadBuffer = null;
                this.SPIBUS_WriteRead(new[] { 0xC2, 0 }, ReadBuffer: ref argReadBuffer);
                WaitUntilReady();
            }

            uint bytes_left = (uint)data_to_write.Length;
            uint buffer_size = 0U;
            uint array_ptr = 0U;
            switch (MyFlashDevice.ProgramMode) {
                case FlashcatUSB.FlashMemory.SPI_ProgramMode.PageMode: {
                        if ((long)MyFlashDevice.STACKED_DIES > 1L) { // Multi-die support
                            var write_result = default(bool);
                            while (bytes_left != 0L) {
                                uint argflash_offset = (uint)flash_offset;
                                uint die_address = GetAddressForMultiDie(ref argflash_offset, ref bytes_left, ref buffer_size);
                                var die_data = new byte[(int)(buffer_size - 1L + 1)];
                                Array.Copy(data_to_write, array_ptr, die_data, 0L, die_data.Length);
                                array_ptr += buffer_size;
                                var setup_packet = new WriteSetupPacket(MyFlashDevice, die_address, (uint)die_data.Length);
                                write_result = WriteData_Flash(setup_packet, die_data);
                                if (!write_result)
                                    return false;
                            }

                            return write_result;
                        } else {
                            var setup_packet = new WriteSetupPacket(MyFlashDevice, (uint)flash_offset, (uint)data_to_write.Length);
                            bool result = WriteData_Flash(setup_packet, data_to_write);
                            return result;
                        }

                        break;
                    }

                case FlashcatUSB.FlashMemory.SPI_ProgramMode.SPI_EEPROM: { // Used on most ST M95080 and above
                        return WriteData_SPI_EEPROM((uint)flash_offset, data_to_write);
                    }

                case FlashcatUSB.FlashMemory.SPI_ProgramMode.AAI_Byte: {
                        return WriteData_AAI((uint)flash_offset, data_to_write, false);
                    }

                case FlashcatUSB.FlashMemory.SPI_ProgramMode.AAI_Word: {
                        return WriteData_AAI((uint)flash_offset, data_to_write, true);
                    }

                case FlashcatUSB.FlashMemory.SPI_ProgramMode.Atmel45Series: {
                        return AT45_WriteData((uint)flash_offset, data_to_write);
                    }

                case FlashcatUSB.FlashMemory.SPI_ProgramMode.Nordic: {
                        int data_left = data_to_write.Length;
                        int ptr = 0;
                        while (data_left > 0) {
                            SPIBUS_WriteEnable();
                            var packet_data = new byte[(int)((long)MyFlashDevice.PAGE_SIZE - 1L + 1)];
                            Array.Copy(data_to_write, ptr, packet_data, 0, packet_data.Length);
                            var setup_packet = new WriteSetupPacket(MyFlashDevice, (uint)(flash_offset + ptr), (uint)packet_data.Length);
                            bool write_result = WriteData_Flash(setup_packet, packet_data);
                            if (!write_result)
                                return false;
                            WaitUntilReady();
                            ptr += packet_data.Length;
                            data_left -= packet_data.Length;
                        }

                        return true;
                    }
            }

            return false;
        }

        internal bool SectorErase(uint sector_index) {
            if (W25M121AV_Mode) {
                byte[] argReadBuffer = null;
                this.SPIBUS_WriteRead(new[] { 0xC2, 0 }, ReadBuffer: ref argReadBuffer);
                WaitUntilReady();
            }

            if (!MyFlashDevice.ERASE_REQUIRED)
                return true; // Erase not needed
            uint flash_offset = (uint)SectorFind(sector_index);
            if (MyFlashDevice.ProgramMode == FlashcatUSB.FlashMemory.SPI_ProgramMode.Atmel45Series) {
                AT45_EraseSector(flash_offset);
            } else if (MyFlashDevice.ProgramMode == FlashcatUSB.FlashMemory.SPI_ProgramMode.Nordic) {
                SPIBUS_WriteEnable(); // : Utilities.Sleep(50)
                byte PageNum = (byte)Math.Floor((double)flash_offset / (double)MyFlashDevice.ERASE_SIZE);
                byte[] argReadBuffer2 = null;
                this.SPIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.SE, PageNum }, ref argReadBuffer2);
            } else {
                SPIBUS_WriteEnable();
                if ((long)MyFlashDevice.STACKED_DIES > 1L) { // Multi-die support
                    uint argcount = 0U;
                    uint argbuffer_size = 0U;
                    flash_offset = GetAddressForMultiDie(ref flash_offset, ref argcount, ref argbuffer_size);
                }

                var DataToWrite = this.GetArrayWithCmdAndAddr(MyFlashDevice.OP_COMMANDS.SE, flash_offset);
                byte[] argReadBuffer1 = null;
                SPIBUS_WriteRead(DataToWrite, ref argReadBuffer1);
                if (MyFlashDevice.SEND_RDFS) {
                    ReadFlagStatusRegister();
                } else {
                    FlashcatUSB.Utilities.Main.Sleep(10);
                }
            }

            WaitUntilReady();
            return true;
        }

        internal bool SectorWrite(uint sector_index, byte[] data, FlashcatUSB.WriteParameters Params = null) {
            if (W25M121AV_Mode) {
                byte[] argReadBuffer = null;
                this.SPIBUS_WriteRead(new[] { 0xC2, 0 }, ReadBuffer: ref argReadBuffer);
                WaitUntilReady();
            }

            uint Addr32 = (uint)SectorFind(sector_index);
            return WriteData(Addr32, data, Params);
        }

        internal bool EraseDevice() {
            PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("spi_erasing_flash_device"), Strings.Format((object)DeviceSize, "#,###")));
            var erase_timer = new Stopwatch();
            erase_timer.Start();
            if (W25M121AV_Mode) {
                byte[] argReadBuffer = null;
                this.SPIBUS_WriteRead(new[] { 0xC2, 0 }, ReadBuffer: ref argReadBuffer);
                WaitUntilReady();
            }

            if (MyFlashDevice.ProgramMode == FlashcatUSB.FlashMemory.SPI_ProgramMode.Atmel45Series) {
                byte[] argReadBuffer1 = null;
                this.SPIBUS_WriteRead(new[] { 0xC7, 0x94, 0x80, 0x9A }, ref argReadBuffer1); // Chip erase command
                FlashcatUSB.Utilities.Main.Sleep(100);
                WaitUntilReady();
            } else if (MyFlashDevice.ProgramMode == FlashcatUSB.FlashMemory.SPI_ProgramMode.SPI_EEPROM) {
                var data = new byte[(int)(MyFlashDevice.FLASH_SIZE - 1L + 1)];
                FlashcatUSB.Utilities.Main.FillByteArray(ref data, 255);
                WriteData(0L, data);
            } else if (MyFlashDevice.ProgramMode == FlashcatUSB.FlashMemory.SPI_ProgramMode.Nordic) {
                // This device does support chip-erase, but it will also erase the InfoPage content
                var nord_timer = new Stopwatch();
                nord_timer.Start();
                PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("spi_erasing_flash_device"), Strings.Format((object)DeviceSize, "#,###")));
                int TotalPages = (int)((double)MyFlashDevice.FLASH_SIZE / (double)MyFlashDevice.PAGE_SIZE);
                for (int i = 0, loopTo1 = TotalPages - 1; i <= loopTo1; i++) {
                    SPIBUS_WriteEnable();
                    FlashcatUSB.Utilities.Main.Sleep(50);
                    byte[] argReadBuffer4 = null;
                    this.SPIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.SE, i }, ref argReadBuffer4);
                    WaitUntilReady();
                }
            } else {
                switch (MyFlashDevice.CHIP_ERASE) {
                    case FlashcatUSB.FlashMemory.EraseMethod.Standard: {
                            SPIBUS_WriteEnable();
                            byte[] argReadBuffer2 = null;
                            this.SPIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.BE }, ref argReadBuffer2); // &HC7
                            if (MyFlashDevice.SEND_RDFS)
                                ReadFlagStatusRegister();
                            WaitUntilReady();
                            break;
                        }

                    case FlashcatUSB.FlashMemory.EraseMethod.BySector: {
                            uint SectorCount = MyFlashDevice.Sector_Count;
                            SetProgress?.Invoke(0);
                            for (uint i = 0U, loopTo = (uint)(SectorCount - 1L); i <= loopTo; i++) {
                                if (!SectorErase(i)) {
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
                            SPIBUS_WriteEnable(); // Try Chip Erase first
                            byte[] argReadBuffer3 = null;
                            this.SPIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.BE }, ref argReadBuffer3);
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

            PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("spi_erase_complete"), Strings.Format((object)((double)erase_timer.ElapsedMilliseconds / 1000d), "#.##")));
            return true;
        }
        // Reads the SPI status register and waits for the device to complete its current operation
        internal void WaitUntilReady() {
            try {
                uint Status;
                if (MyFlashDevice.ProgramMode == FlashcatUSB.FlashMemory.SPI_ProgramMode.Atmel45Series) {
                    do {
                        var sr = ReadStatusRegister(); // Check Bit 7 (RDY/BUSY#)
                        Status = sr[0];
                        if (!((Status & 0x80L) > 0L))
                            FlashcatUSB.Utilities.Main.Sleep(50);
                    }
                    while (!((Status & 0x80L) > 0L));
                } else {
                    do {
                        var sr = ReadStatusRegister();
                        Status = sr[0];
                        if (FlashcatUSB.MainApp.AppIsClosing)
                            return;
                        if (Status == 255L)
                            break;
                        if (Conversions.ToBoolean(Status & 1L))
                            FlashcatUSB.Utilities.Main.Sleep(5);
                    }
                    while (Status & 1L);
                    if (MyFlashDevice is object && MyFlashDevice.ProgramMode == FlashcatUSB.FlashMemory.SPI_ProgramMode.Nordic) {
                        FlashcatUSB.Utilities.Main.Sleep(50);
                    }
                }
            } catch (Exception ex) {
            }
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        private void LoadDeviceConfigurations() {
            if (MyFlashDevice.VENDOR_SPECIFIC == FlashcatUSB.FlashMemory.VENDOR_FEATURE.NotSupported) { // We don't want to do this for vendor enabled devices
                this.WriteStatusRegister(new[] { 0 });
                FlashcatUSB.Utilities.Main.Sleep(100); // Needed, some devices will lock up if else.
            }

            if ((long)MyFlashDevice.STACKED_DIES > 1L) { // Multi-die support
                for (byte i = 0, loopTo = (byte)((long)MyFlashDevice.STACKED_DIES - 1L); i <= loopTo; i++) {
                    byte[] argReadBuffer = null;
                    this.SPIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.DIESEL, i }, ReadBuffer: ref argReadBuffer);
                    WaitUntilReady(); // We need to make sure DIE 0 is selected
                    if (MyFlashDevice.SEND_EN4B) {
                        this.SPIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.EN4B); // Set options for each DIE
                    }

                    this.SPIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.ULBPR); // 0x98 (global block unprotect)
                }

                byte[] argReadBuffer1 = null;
                this.SPIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.DIESEL, 0 }, ReadBuffer: ref argReadBuffer1);
                WaitUntilReady(); // We need to make sure DIE 0 is selected
                DIE_SELECTED = 0;
            } else if (MyFlashDevice.ProgramMode == FlashcatUSB.FlashMemory.SPI_ProgramMode.Atmel45Series) {
                byte[] argReadBuffer2 = null;
                this.SPIBUS_WriteRead(new[] { 0x3D, 0x2A, 0x7F, 0x9A }, ref argReadBuffer2); // Disable sector protection
            } else {
                if (MyFlashDevice.SEND_EN4B)
                    this.SPIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.EN4B); // 0xB7
                this.SPIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.ULBPR);
            } // 0x98 (global block unprotect)
        }

        private SPI_IDENT ReadDeviceID() {
            var DEVICEID = new SPI_IDENT();
            var rdid = new byte[6];
            this.SPIBUS_WriteRead(new[] { FlashcatUSB.FlashMemory.SPI_CmdDef.RDID }, ref rdid); // This reads SPI CHIP ID
            var rems = new byte[2];
            this.SPIBUS_WriteRead(new[] { FlashcatUSB.FlashMemory.SPI_CmdDef.REMS, 0, 0, 0 }, ref rems); // Some devices (such as SST25VF512) only support REMS
            var res = new byte[1];
            this.SPIBUS_WriteRead(new[] { FlashcatUSB.FlashMemory.SPI_CmdDef.RES, 0, 0, 0 }, ref res);
            DEVICEID.MANU = rdid[0];
            DEVICEID.RDID = (uint)rdid[1] << 24 | (uint)rdid[2] << 16 | (uint)rdid[3] << 8 | rdid[4];
            DEVICEID.FMY = rdid[5];
            DEVICEID.REMS = rems[0] << 8 | rems[1];
            DEVICEID.RES = res[0];
            return DEVICEID;
        }
        // This writes to the SR (multi-bytes can be input to write as well)
        public bool WriteStatusRegister(byte[] NewValues) {
            try {
                if (NewValues is null)
                    return false;
                SPIBUS_WriteEnable(); // Some devices such as AT25DF641 require the WREN and the status reg cleared before we can write data
                if (MyFlashDevice.SEND_EWSR) {
                    byte[] argReadBuffer = null;
                    this.SPIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.EWSR }, ref argReadBuffer); // Send the command that we are going to enable-write to register
                    System.Threading.Thread.Sleep(20); // Wait a brief moment
                }

                var cmd = new byte[NewValues.Length + 1];
                cmd[0] = MyFlashDevice.OP_COMMANDS.WRSR;
                Array.Copy(NewValues, 0, cmd, 1, NewValues.Length);
                byte[] argReadBuffer1 = null;
                if (!(SPIBUS_WriteRead(cmd, ref argReadBuffer1) == cmd.Length))
                    return false;
                return true;
            } catch (Exception ex) {
                return false;
            }
        }

        public byte[] ReadStatusRegister(int Count = 1) {
            try {
                var Output = new byte[Count];
                this.SPIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.RDSR }, ref Output);
                return Output;
            } catch (Exception ex) {
                return null;
            } // Erorr
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

        private void ReadFlagStatusRegister() {
            FlashcatUSB.Utilities.Main.Sleep(10);
            var flag = new byte[] { 0 };
            do
                this.SPIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.RDFR }, ref flag);
            while (!(flag[0] >> 7 & 1));
        }

        private void EraseDie() {
            uint die_size = 0x2000000U;
            uint die_count = (uint)((double)MyFlashDevice.FLASH_SIZE / die_size);
            for (uint x = 1U, loopTo = die_count; x <= loopTo; x++) {
                PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("spi_erasing_die"), x.ToString(), Strings.Format((object)die_size, "#,###")));
                var die_addr = FlashcatUSB.Utilities.Bytes.Bytes.FromUInt32((uint)((x - 1L) * die_size));
                SPIBUS_WriteEnable();
                byte[] argReadBuffer = null;
                this.SPIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.DE, die_addr[0], die_addr[1], die_addr[1], die_addr[1] }, ref argReadBuffer); // &HC4
                FlashcatUSB.Utilities.Main.Sleep(1000);
                if (MyFlashDevice.SEND_RDFS)
                    ReadFlagStatusRegister();
                WaitUntilReady();
            }
        }

        public void ResetDevice() {
            byte[] argReadBuffer = null;
            this.SPIBUS_WriteRead(new[] { 0xF0 }, ReadBuffer: ref argReadBuffer); // SPI RESET COMMAND
            // Other commands: 0x66 and 0x99
            FlashcatUSB.Utilities.Main.Sleep(10);
        }

        private void LoadVendorSpecificConfigurations() {
            if (MyFlashDevice.ProgramMode == FlashcatUSB.FlashMemory.SPI_ProgramMode.Atmel45Series) { // May need to load the current page mode
                var sr = ReadStatusRegister(); // Some devices have 2 SR
                ExtendedPage = (sr[0] & 1) == 0;
                uint page_size = Conversions.ToUInteger(Interaction.IIf(ExtendedPage, (object)MyFlashDevice.PAGE_SIZE_EXTENDED, (object)MyFlashDevice.PAGE_SIZE));
                PrintConsole?.Invoke("Device configured to page size: " + page_size + " bytes");
            }

            if ((int)MyFlashDevice.MFG_CODE == 0xBF) { // SST26VF016/SST26VF032 requires block protection to be removed in SQI only
                if ((int)MyFlashDevice.ID1 == 0x2601 | (int)MyFlashDevice.ID1 == 0x2602) {
                    PrintConsole?.Invoke("SQI mode must be used to remove block protection");
                }
            } else if ((int)MyFlashDevice.MFG_CODE == 0x9D) { // ISSI
                this.WriteStatusRegister(new[] { 0 }); // Erase protection bits
            }

            if ((int)MyFlashDevice.MFG_CODE == 0xEF && (int)MyFlashDevice.ID1 == 0x4018) {
                byte[] argReadBuffer = null;
                this.SPIBUS_WriteRead(new[] { 0xC2, 1 }, ReadBuffer: ref argReadBuffer);
                WaitUntilReady(); // Check to see if this device has two dies
                var id = ReadDeviceID();
                if (id.RDID == 0xEFAB2100U)
                    W25M121AV_Mode = true;
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
                    SPIBUS_WriteEnable();
                    byte[] argReadBuffer1 = null;
                    this.SPIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.EWSR }, ReadBuffer: ref argReadBuffer1);
                    byte[] argReadBuffer2 = null;
                    this.SPIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.WRSR, 0, 0, 0x88, 0x18 }, ReadBuffer: ref argReadBuffer2); // Set sector to uniform 256KB / 512B Page size
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
                    SPIBUS_WriteEnable();
                    byte[] argReadBuffer3 = null;
                    this.SPIBUS_WriteRead(new[] { 0x71, 0x80, 0, 4, 0x18 }, ReadBuffer: ref argReadBuffer3); // Enables 512-byte buffer
                    SPIBUS_WriteEnable();
                    byte[] argReadBuffer4 = null;
                    this.SPIBUS_WriteRead(new[] { 0x71, 0x80, 0, 3, 0x80 }, ReadBuffer: ref argReadBuffer4); // Enables 4-byte mode
                }
            }
        }

        /* TODO ERROR: Skipped RegionDirectiveTrivia */        // Changes the Page Size configuration bit in nonvol
        public bool AT45_SetPageConfiguration(bool EnableExtPage) {
            if (EnableExtPage) {
                byte[] argReadBuffer = null;
                this.SPIBUS_WriteRead(new[] { 0x3D, 0x2A, 0x80, 0xA7 }, ref argReadBuffer);
            } else {
                byte[] argReadBuffer1 = null;
                this.SPIBUS_WriteRead(new[] { 0x3D, 0x2A, 0x80, 0xA6 }, ref argReadBuffer1);
            } // One-time programmable!

            WaitUntilReady();
            ExtendedPage = EnableExtPage;
            return true;
        }

        private byte[] AT45_ReadData(uint flash_offset, uint data_count) {
            uint page_size = Conversions.ToUInteger(Interaction.IIf(ExtendedPage, (object)MyFlashDevice.PAGE_SIZE_EXTENDED, (object)MyFlashDevice.PAGE_SIZE));
            var data_out = new byte[(int)(data_count - 1L + 1)];
            int AddrOffset = (int)Math.Ceiling(Math.Log(page_size, 2d)); // Number of bits the address is offset
            uint PageAddr = (uint)Math.Floor(flash_offset / (double)page_size);
            uint PageOffset = flash_offset - PageAddr * page_size;
            var addr_bytes = FlashcatUSB.Utilities.Bytes.Bytes.FromUInt24((PageAddr << AddrOffset) + PageOffset);
            uint at45_addr = (PageAddr << AddrOffset) + PageOffset;
            int dummy_clocks = 4 * 8; // (4 extra bytes)
            var setup_class = new ReadSetupPacket(MyFlashDevice.OP_COMMANDS.READ, at45_addr, (uint)data_out.Length, (byte)MyFlashDevice.AddressBytes);
            setup_class.DUMMY = dummy_clocks;
            FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.SPI_READFLASH, setup_class.ToBytes(), ref data_out, 0U);
            return data_out;
        }

        private bool AT45_EraseSector(uint flash_offset) {
            uint page_size = Conversions.ToUInteger(Interaction.IIf(ExtendedPage, (object)MyFlashDevice.PAGE_SIZE_EXTENDED, (object)MyFlashDevice.PAGE_SIZE));
            uint EraseSize = SectorSize(0U);
            uint AddrOffset = (uint)Math.Ceiling(Math.Log(page_size, 2d)); // Number of bits the address is offset
            uint blocknum = (uint)Math.Floor(flash_offset / (double)EraseSize);
            var addrbytes = FlashcatUSB.Utilities.Bytes.Bytes.FromUInt24(blocknum << (int)(AddrOffset + 3L));
            byte[] argReadBuffer = null;
            this.SPIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.SE, addrbytes[0], addrbytes[1], addrbytes[2] }, ref argReadBuffer);
            return true;
        }
        // Uses an internal sram buffer to transfer data from the board to the flash (used by Atmel AT45DBxxx)
        private bool AT45_WriteData(uint offset, byte[] DataOut) {
            try {
                uint data_size = (uint)DataOut.Length;
                uint page_size = Conversions.ToUInteger(Interaction.IIf(ExtendedPage, (object)MyFlashDevice.PAGE_SIZE_EXTENDED, (object)MyFlashDevice.PAGE_SIZE));
                int AddrOffset = (int)Math.Ceiling(Math.Log(page_size, 2d)); // Number of bits the address is offset
                uint BytesLeft = (uint)DataOut.Length;
                while (BytesLeft != 0L) {
                    int BytesToWrite = (int)BytesLeft;
                    if (BytesToWrite > page_size)
                        BytesToWrite = (int)page_size;
                    var DataToBuffer = new byte[BytesToWrite + 3 + 1];
                    DataToBuffer[0] = MyFlashDevice.OP_COMMANDS.WRTB; // 0x84
                    int src_ind = (int)(DataOut.Length - BytesLeft);
                    Array.Copy(DataOut, src_ind, DataToBuffer, 4, BytesToWrite);
                    SPIBUS_SlaveSelect_Enable();
                    SPIBUS_WriteData(DataToBuffer);
                    SPIBUS_SlaveSelect_Disable();
                    WaitUntilReady();
                    uint PageAddr = (uint)Math.Floor(offset / (double)page_size);
                    var PageCmd = FlashcatUSB.Utilities.Bytes.Bytes.FromUInt24(PageAddr << AddrOffset);
                    var Cmd2 = new byte[] { MyFlashDevice.OP_COMMANDS.WRFB, PageCmd[0], PageCmd[1], PageCmd[2] }; // 0x88
                    byte[] argReadBuffer = null;
                    SPIBUS_WriteRead(Cmd2, ReadBuffer: ref argReadBuffer);
                    WaitUntilReady();
                    offset = (uint)(offset + BytesToWrite);
                    BytesLeft = (uint)(BytesLeft - BytesToWrite);
                }

                return true;
            } catch (Exception ex) {
            }

            return false;
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        /* TODO ERROR: Skipped RegionDirectiveTrivia */        // Returns the die address from the flash_offset (and increases by the buffersize) and also selects the correct die
        private uint GetAddressForMultiDie(ref uint flash_offset, ref uint count, ref uint buffer_size) {
            uint die_size = (uint)((double)MyFlashDevice.FLASH_SIZE / (double)MyFlashDevice.STACKED_DIES);
            byte die_id = (byte)Math.Floor(flash_offset / (double)die_size);
            uint die_addr = flash_offset % die_size;
            if ((int)MyFlashDevice.MFG_CODE == 0x20) { // Micron uses a different die system
                buffer_size = (uint)Math.Min((double)count, (double)MyFlashDevice.FLASH_SIZE / (double)MyFlashDevice.STACKED_DIES - (double)die_addr);
                count -= buffer_size;
                die_addr = flash_offset;
                flash_offset += buffer_size;
            } else {
                buffer_size = (uint)Math.Min((double)count, (double)MyFlashDevice.FLASH_SIZE / (double)MyFlashDevice.STACKED_DIES - (double)die_addr);
                if (die_id != DIE_SELECTED) {
                    byte[] argReadBuffer = null;
                    this.SPIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.DIESEL, die_id }, ReadBuffer: ref argReadBuffer);
                    WaitUntilReady();
                    DIE_SELECTED = die_id;
                }

                count -= buffer_size;
                flash_offset += buffer_size;
            }

            return die_addr;
        }

        private bool WriteData_Flash(WriteSetupPacket setup_packet, byte[] data_out) {
            try {
                var result = default(bool);
                if (FCUSB.HasLogic) {
                    uint DataToWrite = (uint)data_out.Length;
                    uint PacketSize = 8192U;
                    int Loops = (int)Math.Ceiling(DataToWrite / (double)PacketSize); // Calcuates iterations
                    for (int i = 0, loopTo = Loops - 1; i <= loopTo; i++) {
                        int BufferSize = (int)DataToWrite;
                        if (BufferSize > PacketSize)
                            BufferSize = (int)PacketSize;
                        var data = new byte[BufferSize];
                        setup_packet.WR_COUNT = (uint)BufferSize;
                        Array.Copy(data_out, i * PacketSize, data, 0L, data.Length);
                        result = FCUSB.USB_SETUP_BULKOUT(FlashcatUSB.USB.USBREQ.SPI_WRITEFLASH, setup_packet.ToBytes(), data, 0U, 1000);
                        if (!result)
                            return false;
                        FlashcatUSB.Utilities.Main.Sleep(5);
                        setup_packet.DATA_OFFSET = (uint)(setup_packet.DATA_OFFSET + data.Length);
                        DataToWrite = (uint)(DataToWrite - data.Length);
                        result = FCUSB.USB_WaitForComplete();
                    }
                } else {
                    result = FCUSB.USB_SETUP_BULKOUT(FlashcatUSB.USB.USBREQ.SPI_WRITEFLASH, setup_packet.ToBytes(), data_out, 0U);
                }

                FlashcatUSB.Utilities.Main.Sleep(6); // Needed
                return result;
            } catch (Exception ex) {
                return false;
            }
        }
        // Designed for SPI EEPROMS where each page needs to wait until ready
        private bool WriteData_SPI_EEPROM(uint offset, byte[] data_to_write) {
            uint PageSize = MyFlashDevice.PAGE_SIZE;
            int DataToWrite = data_to_write.Length;
            for (int i = 0, loopTo = (int)(Math.Ceiling(DataToWrite / (double)PageSize) - 1d); i <= loopTo; i++) {
                int BufferSize = DataToWrite;
                if (BufferSize > PageSize)
                    BufferSize = (int)PageSize;
                var data = new byte[BufferSize];
                Array.Copy(data_to_write, i * PageSize, data, 0L, data.Length);
                int addr_size = (int)((double)MyFlashDevice.ADDRESSBITS / 8d);
                var packet = new byte[data.Length + addr_size + 1]; // OPCMD,ADDR,DATA
                packet[0] = MyFlashDevice.OP_COMMANDS.PROG; // First byte is the write command
                if (addr_size == 1) {
                    byte addr8;
                    if (offset > 255L) {
                        packet[0] = (byte)((int)MyFlashDevice.OP_COMMANDS.PROG | 8); // Enables 4th bit
                        addr8 = (byte)(offset & 255L); // Lower 8 bits only
                    } else {
                        packet[0] = (byte)((int)MyFlashDevice.OP_COMMANDS.PROG & 0xF7); // Disables 4th bit
                        addr8 = (byte)offset;
                    }

                    packet[1] = addr8;
                    Array.Copy(data, 0, packet, 2, data.Length);
                } else if (addr_size == 2) {
                    packet[1] = (byte)(offset >> 8 & 255L);
                    packet[2] = (byte)(offset & 255L);
                    Array.Copy(data, 0, packet, 3, data.Length);
                } else if (addr_size == 3) {
                    packet[1] = (byte)(offset >> 16 & 255L);
                    packet[2] = (byte)(offset >> 8 & 255L);
                    packet[3] = (byte)(offset & 255L);
                    Array.Copy(data, 0, packet, 4, data.Length);
                }

                SPIBUS_WriteEnable();
                SPIBUS_SlaveSelect_Enable();
                SPIBUS_WriteData(packet);
                SPIBUS_SlaveSelect_Disable();
                FlashcatUSB.Utilities.Main.Sleep(10);
                WaitUntilReady();
                offset = (uint)(offset + data.Length);
                DataToWrite -= data.Length;
            }

            return true;
        }

        private bool WriteData_AAI(uint flash_offset, byte[] data, bool word_mode) {
            if (word_mode && !(data.Length % 2 == 0)) { // We must write a even number of bytes
                Array.Resize(ref data, data.Length + 1);
                data[data.Length - 1] = 255; // Fill the last byte with 0xFF
            }

            SPIBUS_WriteEnable();
            MyFlashDevice.PAGE_SIZE = 1024U; // No page size needed when doing AAI
            var setup_packet = new WriteSetupPacket(MyFlashDevice, flash_offset, (uint)data.Length);
            if (word_mode) {
                setup_packet.CMD_PROG = MyFlashDevice.OP_COMMANDS.AAI_WORD;
            } else {
                setup_packet.CMD_PROG = MyFlashDevice.OP_COMMANDS.AAI_BYTE;
            }

            uint ctrl = (uint)(FlashcatUSB.Utilities.Main.BoolToInt(word_mode) + 1);
            bool Result = FCUSB.USB_SETUP_BULKOUT(FlashcatUSB.USB.USBREQ.SPI_WRITEDATA_AAI, setup_packet.ToBytes(), data, ctrl);
            if (!Result)
                return false;
            FlashcatUSB.Utilities.Main.Sleep(6); // Needed for some reason
            FCUSB.USB_WaitForComplete();
            SPIBUS_WriteDisable();
            return true;
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        public void SPIBUS_Setup(SPI_SPEED bus_speed) {
            uint clock_speed = (uint)FlashcatUSB.MainApp.GetMaxSpiClock(FCUSB.HWBOARD, bus_speed);
            FCUSB.USB_SPI_INIT((uint)FlashcatUSB.MainApp.MySettings.SPI_MODE, clock_speed);
            FlashcatUSB.Utilities.Main.Sleep(50); // Allow time for device to change IO
        }

        public bool SPIBUS_WriteEnable() {
            byte[] argReadBuffer = null;
            if (this.SPIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.WREN }, ref argReadBuffer) == 1L) {
                return true;
            } else {
                return false;
            }
        }

        public bool SPIBUS_WriteDisable() {
            byte[] argReadBuffer = null;
            if (this.SPIBUS_WriteRead(new[] { MyFlashDevice.OP_COMMANDS.WRDI }, ref argReadBuffer) == 1L) {
                return true;
            } else {
                return false;
            }
        }

        public bool SPIBUS_SendCommand(byte spi_cmd) {
            bool we = SPIBUS_WriteEnable();
            if (!we)
                return false;
            byte[] argReadBuffer = null;
            if (SPIBUS_WriteRead(new[] { spi_cmd }, ref argReadBuffer) == 1L)
                return true;
            return false;
        }

        public uint SPIBUS_WriteRead(byte[] WriteBuffer, [Optional, DefaultParameterValue(null)] ref byte[] ReadBuffer) {
            if (WriteBuffer is null & ReadBuffer is null)
                return 0U;
            uint TotalBytesTransfered = 0U;
            SPIBUS_SlaveSelect_Enable();
            if (WriteBuffer is object) {
                bool Result = SPIBUS_WriteData(WriteBuffer);
                if (Result)
                    TotalBytesTransfered = (uint)(TotalBytesTransfered + WriteBuffer.Length);
            }

            if (ReadBuffer is object) {
                bool Result = SPIBUS_ReadData(ref ReadBuffer);
                if (Result)
                    TotalBytesTransfered = (uint)(TotalBytesTransfered + ReadBuffer.Length);
            }

            SPIBUS_SlaveSelect_Disable();
            return TotalBytesTransfered;
        }
        // Makes the CS/SS pin go low
        private void SPIBUS_SlaveSelect_Enable() {
            try {
                FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.SPI_SS_ENABLE);
            } catch (Exception ex) {
            }
        }
        // Releases the CS/SS pin
        private void SPIBUS_SlaveSelect_Disable() {
            try {
                FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.SPI_SS_DISABLE);
            } catch (Exception ex) {
            }
        }

        private bool SPIBUS_WriteData(byte[] DataOut) {
            bool Success;
            try {
                Success = FCUSB.USB_SETUP_BULKOUT(FlashcatUSB.USB.USBREQ.SPI_WR_DATA, null, DataOut, (uint)DataOut.Length);
                FlashcatUSB.Utilities.Main.Sleep(2);
            } catch (Exception ex) {
                return false;
            }

            if (!Success)
                PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("spi_error_writing"));
            return true;
        }

        private bool SPIBUS_ReadData(ref byte[] Data_In) {
            bool Success = false;
            try {
                Success = FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.SPI_RD_DATA, null, ref Data_In, (uint)Data_In.Length);
            } catch (Exception ex) {
            }

            if (!Success)
                PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("spi_error_reading"));
            return Success;
        }

        public void SetProgPin(bool enabled) {
            try {
                uint value = 0U; // Set to LOW
                if (enabled)
                    value = 1U; // Set to HIGH
                FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.SPI_PROG, null, value);
            } catch (Exception ex) {
            }
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
    }

    public enum SPI_ORDER : int {
        SPI_ORDER_MSB_FIRSTField = 0,
        SPI_ORDER_LSB_FIRSTField = 1
    }

    public enum SPI_CLOCK_POLARITY : int {
        SPI_MODE_0 = 0, // CPOL(0),CPHA(0),CKE(1)
        SPI_MODE_1 = 1, // CPOL(0),CPHA(1),CKE(0)
        SPI_MODE_2 = 2, // CPOL(1),CPHA(0),CKE(1)
        SPI_MODE_3 = 3 // CPOL(1),CPHA(1),CKE(0)
    }

    public enum SPI_SPEED : uint {
        MHZ_32 = 32000000U,
        MHZ_24 = 24000000U,
        MHZ_16 = 16000000U,
        MHZ_12 = 12000000U,
        MHZ_8 = 8000000U,
        MHZ_4 = 4000000U,
        MHZ_2 = 2000000U,
        MHZ_1 = 1000000U
    }

    public enum SQI_SPEED : uint {
        MHZ_40 = 40000000U,
        MHZ_20 = 20000000U,
        MHZ_10 = 10000000U,
        MHZ_5 = 5000000U,
        MHZ_2 = 2000000U,
        MHZ_1 = 1000000U
    }

    internal class WriteSetupPacket {
        public byte CMD_PROG { get; set; }
        public byte CMD_WREN { get; set; }
        public byte CMD_RDSR { get; set; }
        public byte CMD_RDFR { get; set; }
        public byte CMD_WR { get; set; }
        public uint PAGE_SIZE { get; set; }
        public bool SEND_RDFS { get; set; } = false;
        public uint DATA_OFFSET { get; set; }
        public uint WR_COUNT { get; set; }
        public byte ADDR_BYTES { get; set; } // Number of bytes used for the read command 
        public FlashcatUSB.FlashMemory.SPI_QUAD_SUPPORT SPI_MODE { get; set; } = FlashcatUSB.FlashMemory.SPI_QUAD_SUPPORT.NO_QUAD;

        public WriteSetupPacket(FlashcatUSB.FlashMemory.SPI_NOR spi_dev, uint offset, uint d_count) {
            CMD_PROG = spi_dev.OP_COMMANDS.PROG;
            CMD_WREN = spi_dev.OP_COMMANDS.WREN;
            CMD_RDSR = spi_dev.OP_COMMANDS.RDSR;
            CMD_RDFR = spi_dev.OP_COMMANDS.RDFR; // Flag Status Register
            ADDR_BYTES = (byte)spi_dev.AddressBytes;
            DATA_OFFSET = offset;
            WR_COUNT = d_count;
            SEND_RDFS = spi_dev.SEND_RDFS;
            PAGE_SIZE = spi_dev.PAGE_SIZE;
        }

        public byte[] ToBytes() {
            var setup_data = new byte[15];
            setup_data[0] = CMD_PROG;
            setup_data[1] = CMD_WREN;
            setup_data[2] = CMD_RDSR;
            setup_data[3] = CMD_RDFR;
            setup_data[4] = ADDR_BYTES; // Number of bytes to write
            setup_data[5] = (byte)((PAGE_SIZE & 0xFF00L) >> 8);
            setup_data[6] = (byte)(PAGE_SIZE & 0xFFL);
            setup_data[7] = (byte)((DATA_OFFSET & 0xFF000000L) >> 24);
            setup_data[8] = (byte)((DATA_OFFSET & 0xFF0000L) >> 16);
            setup_data[9] = (byte)((DATA_OFFSET & 0xFF00L) >> 8);
            setup_data[10] = (byte)(DATA_OFFSET & 0xFFL);
            setup_data[11] = (byte)((WR_COUNT & 0xFF0000L) >> 16);
            setup_data[12] = (byte)((WR_COUNT & 0xFF00L) >> 8);
            setup_data[13] = (byte)(WR_COUNT & 0xFFL);
            setup_data[14] = (byte)SPI_MODE;
            if (!SEND_RDFS)
                setup_data[3] = 0; // Only use flag-reg if required
            return setup_data;
        }
    }

    internal class ReadSetupPacket {
        public byte READ_CMD { get; set; }
        public uint DATA_OFFSET { get; set; }
        public uint COUNT { get; set; }
        public byte ADDR_BYTES { get; set; } // Number of bytes used for the read command (MyFlashDevice.AddressBytes)
        public int DUMMY { get; set; } = 0; // Number of clock toggles before reading data
        public SPI.SQI_IO_MODE SPI_MODE { get; set; } = SPI.SQI_IO_MODE.SPI_ONLY;

        public ReadSetupPacket(byte cmd, uint offset, uint d_count, byte addr_size) {
            READ_CMD = cmd;
            DATA_OFFSET = offset;
            COUNT = d_count;
            ADDR_BYTES = addr_size;
        }

        public byte[] ToBytes() {
            var setup_data = new byte[11]; // 12 bytes
            setup_data[0] = READ_CMD; // READ/FAST_READ/ETC.
            setup_data[1] = ADDR_BYTES;
            setup_data[2] = (byte)((DATA_OFFSET & 0xFF000000L) >> 24);
            setup_data[3] = (byte)((DATA_OFFSET & 0xFF0000L) >> 16);
            setup_data[4] = (byte)((DATA_OFFSET & 0xFF00L) >> 8);
            setup_data[5] = (byte)(DATA_OFFSET & 0xFFL);
            setup_data[6] = (byte)((COUNT & 0xFF0000L) >> 16);
            setup_data[7] = (byte)((COUNT & 0xFF00L) >> 8);
            setup_data[8] = (byte)(COUNT & 0xFFL);
            setup_data[9] = (byte)DUMMY; // Number of dummy bytes
            setup_data[10] = (byte)SPI_MODE;
            return setup_data;
        }
    }

    internal class SPI_IDENT {
        public byte MANU; // (MFG)
        public uint RDID; // (ID1,ID2,ID3,ID4)
        public byte FMY; // (ID5)
        public ushort REMS; // (MFG)(ID1)
        public byte RES; // (MFG)

        public SPI_IDENT() {
        }

        public bool DETECTED {
            get {
                if (MANU == 0 || MANU == 0xFF)
                    return false;
                if (RDID == 0L || RDID == 0xFFFFL)
                    return false;
                if (MANU == (byte)(RDID & 255L) & MANU == (byte)(RDID >> 8 & 255L))
                    return false;
                return true;
            }
        }
    }

    public enum SPIBUS_MODE : byte {
        SPI_MODE_0 = 1,
        SPI_MODE_1 = 2,
        SPI_MODE_2 = 3,
        SPI_MODE_3 = 4,
        SPI_UNSPECIFIED
    }
}