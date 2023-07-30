// COPYRIGHT EMBEDDEDCOMPUTERS.NET 2020 - ALL RIGHTS RESERVED
// THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
// CONTACT EMAIL: support@embeddedcomputers.net
// ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
// ACKNOWLEDGEMENT: USB driver functionality provided by LibUsbDotNet (sourceforge.net/projects/libusbdotnet) 

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace FlashcatUSB {
    public class SPINAND_Programmer : FlashcatUSB.MemoryDeviceUSB {
        private FlashcatUSB.USB.FCUSB_DEVICE FCUSB;

        public event PrintConsoleEventHandler PrintConsole;

        public delegate void PrintConsoleEventHandler(string message);

        public event SetProgressEventHandler SetProgress;

        public delegate void SetProgressEventHandler(int percent);

        public FlashcatUSB.FlashMemory.SPI_NAND MyFlashDevice { get; set; }
        public FlashcatUSB.USB.DeviceStatus MyFlashStatus { get; set; } = FlashcatUSB.USB.DeviceStatus.NotDetected;
        public int DIE_SELECTED { get; set; } = 0;

        private delegate byte[] USB_Readages(uint page_addr, ushort page_offset, uint data_count, FlashcatUSB.FlashMemory.FlashArea memory_area);

        public SPINAND_Programmer(FlashcatUSB.USB.FCUSB_DEVICE parent_if) {
            FCUSB = parent_if;
        }

        public bool DeviceInit() {
            var rdid = new byte[4];
            var sr = new byte[1];
            byte MFG;
            ushort PART;
            if (FCUSB.SPI_NOR_IF.W25M121AV_Mode) {
                byte[] argReadBuffer = null;
                this.SPIBUS_WriteRead(new[] { 0xC2, 1 }, ReadBuffer: ref argReadBuffer);
                WaitUntilReady();
                MFG = 0xEF;
                PART = 0xAA21; // We need to override AB21 to indicate 1Gbit NAND die
            } else {
                int clk_speed = (int)FlashcatUSB.MainApp.GetMaxSpiClock(FCUSB.HWBOARD, FlashcatUSB.MainApp.MySettings.SPI_CLOCK_MAX);
                FCUSB.USB_SPI_INIT((uint)FlashcatUSB.MainApp.MySettings.SPI_MODE, (uint)clk_speed);
                this.SPIBUS_WriteRead(new[] { FlashcatUSB.FlashMemory.SPI_CmdDef.RDID }, ref rdid); // NAND devives use 1 dummy byte, then MFG and ID1 (and sometimes, ID2)
                if (rdid[0] == 0xC8 && rdid[3] == 0xC8) { // GigaDevice device
                    MFG = rdid[0];
                    PART = (ushort)((rdid[1] << 8) + rdid[2]);
                } else { // Other Manufacturers use this
                    if (!(rdid[0] == 0 | rdid[0] == 255))
                        return false;
                    if (rdid[1] == 0 || rdid[1] == 255)
                        return false;
                    if (rdid[2] == 0 || rdid[2] == 255)
                        return false;
                    if (rdid[1] == rdid[2])
                        return false;
                    MFG = rdid[1];
                    PART = rdid[2];
                    if (!(rdid[3] == MFG))
                        PART = (ushort)((rdid[2] << 8) + rdid[3]);
                }
            }

            string RDID_Str = "0x" + Conversion.Hex(MFG).PadLeft(2, '0') + Conversion.Hex(PART).PadLeft(4, '0');
            PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("spinand_opened_device"));
            PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("spinand_connected"), RDID_Str));
            MyFlashDevice = (FlashcatUSB.FlashMemory.SPI_NAND)FlashcatUSB.MainApp.FlashDatabase.FindDevice(MFG, PART, 0, FlashcatUSB.FlashMemory.MemoryType.SERIAL_NAND);
            if (MyFlashDevice is object) {
                MyFlashStatus = FlashcatUSB.USB.DeviceStatus.Supported;
                PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("spinand_flash_size"), MyFlashDevice.NAME, (object)MyFlashDevice.FLASH_SIZE));
                PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("spinand_page_size"), (object)MyFlashDevice.PAGE_SIZE, (object)MyFlashDevice.PAGE_EXT));
                NANDHELPER_SetupHandlers();
                ECC_ENABLED = !FlashcatUSB.MainApp.MySettings.SPI_NAND_DISABLE_ECC;
                if (MFG == 0xEF && PART == 0xAA21) { // W25M01GV/W25M121AV
                    this.SPIBUS_WriteRead(new[] { OPCMD_GETFEAT, 0xB0 }, ref sr);
                    byte[] argReadBuffer1 = null;
                    this.SPIBUS_WriteRead(new[] { OPCMD_SETFEAT, 0xB0, (int)sr[0] | 8 }, ReadBuffer: ref argReadBuffer1); // Set bit 3 to ON (BUF=1)
                } else if (MFG == 0xEF && PART == 0xAB21) { // W25M02GV
                    this.SPIBUS_WriteRead(new[] { OPCMD_GETFEAT, 0xB0 }, ref sr);
                    byte[] argReadBuffer3 = null;
                    this.SPIBUS_WriteRead(new[] { OPCMD_SETFEAT, 0xB0, (int)sr[0] | 8 }, ReadBuffer: ref argReadBuffer3); // Set bit 3 to ON (BUF=1)
                } else {
                    byte[] argReadBuffer2 = null;
                    SPIBUS_WriteRead(new[] { OPCMD_RESET }, ReadBuffer: ref argReadBuffer2); // Dont reset W25M121AV
                    FlashcatUSB.Utilities.Main.Sleep(1);
                }

                if ((long)MyFlashDevice.STACKED_DIES > 1L) { // Multi-die support
                    for (long i = 0L, loopTo = (long)MyFlashDevice.STACKED_DIES - 1L; i <= loopTo; i++) {
                        byte[] argReadBuffer4 = null;
                        this.SPIBUS_WriteRead(new[] { OPCMD_DIESELECT, i }, ReadBuffer: ref argReadBuffer4);
                        WaitUntilReady();
                        SPIBUS_WriteEnable();
                        byte[] argReadBuffer5 = null;
                        this.SPIBUS_WriteRead(new[] { OPCMD_SETFEAT, 0xA0, 0 }, ReadBuffer: ref argReadBuffer5); // Remove block protection
                    }

                    byte[] argReadBuffer6 = null;
                    this.SPIBUS_WriteRead(new[] { OPCMD_DIESELECT, 0 }, ReadBuffer: ref argReadBuffer6);
                    WaitUntilReady();
                    DIE_SELECTED = 0;
                } else {
                    SPIBUS_WriteEnable();
                    byte[] argReadBuffer7 = null;
                    this.SPIBUS_WriteRead(new[] { OPCMD_SETFEAT, 0xA0, 0 }, ReadBuffer: ref argReadBuffer7);
                } // Remove block protection

                FCUSB.NAND_IF.CreateMap(MyFlashDevice.FLASH_SIZE, MyFlashDevice.PAGE_SIZE, (uint)MyFlashDevice.PAGE_EXT, MyFlashDevice.PAGE_COUNT, MyFlashDevice.Sector_Count);
                FCUSB.NAND_IF.EnableBlockManager(); // If enabled
                FCUSB.NAND_IF.ProcessMap();
                FlashcatUSB.MainApp.ECC_LoadConfiguration((int)MyFlashDevice.PAGE_SIZE, (int)MyFlashDevice.PAGE_EXT);
                return true;
            } else {
                PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("unknown_device_email"));
                MyFlashStatus = FlashcatUSB.USB.DeviceStatus.NotSupported;
                return false;
            }
        }
        // Indicates if the device has its internal ECC engine enabled
        public bool ECC_ENABLED {
            get {
                var sr = new byte[1];
                this.SPIBUS_WriteRead(new[] { OPCMD_GETFEAT, 0xB0 }, ref sr);
                byte config_reg = sr[0];
                if (Conversions.ToBoolean((short)(config_reg >> 4) & Conversions.ToShort(1 == 1)))
                    return true;
                return false;
            }

            set {
                var sr = new byte[1];
                this.SPIBUS_WriteRead(new[] { OPCMD_GETFEAT, 0xB0 }, ref sr);
                byte config_reg = sr[0];
                if (value) {
                    config_reg = (byte)(config_reg | 0x10); // Set bit 4 to ON
                } else {
                    config_reg = (byte)(config_reg & 0xEF);
                } // Set bit 4 to OFF

                SPIBUS_WriteEnable();
                byte[] argReadBuffer = null;
                this.SPIBUS_WriteRead(new[] { OPCMD_SETFEAT, 0xB0, config_reg }, ReadBuffer: ref argReadBuffer);
            }
        }

        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        internal string DeviceName {
            get {
                switch (MyFlashStatus) {
                    case FlashcatUSB.USB.DeviceStatus.Supported: {
                            return MyFlashDevice.NAME;
                        }

                    case FlashcatUSB.USB.DeviceStatus.NotSupported: {
                            return Conversion.Hex(MyFlashDevice.MFG_CODE).PadLeft(2, '0') + " " + Conversion.Hex(MyFlashDevice.ID1).PadLeft(2, '0');
                        }

                    default: {
                            return FlashcatUSB.MainApp.RM.GetString("no_flash_detected");
                        }
                }
            }
        }

        internal long DeviceSize {
            get {
                uint available_pages = FCUSB.NAND_IF.MAPPED_PAGES;
                if (FlashcatUSB.MainApp.NAND_LayoutTool.Layout == FlashcatUSB.NandMemLayout.Combined) {
                    return (long)(available_pages * (MyFlashDevice.PAGE_SIZE + (uint)MyFlashDevice.PAGE_EXT));
                } else {
                    return (long)(available_pages * MyFlashDevice.PAGE_SIZE);
                }
            }
        }

        internal uint SectorSize(uint sector, FlashcatUSB.FlashMemory.FlashArea memory_area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            if (!(MyFlashStatus == FlashcatUSB.USB.DeviceStatus.Supported))
                return 0U;
            switch (memory_area) {
                case FlashcatUSB.FlashMemory.FlashArea.Main: {
                        return (uint)MyFlashDevice.PAGE_COUNT * MyFlashDevice.PAGE_SIZE;
                    }

                case FlashcatUSB.FlashMemory.FlashArea.OOB: {
                        return (uint)(MyFlashDevice.PAGE_COUNT * MyFlashDevice.PAGE_EXT);
                    }

                case FlashcatUSB.FlashMemory.FlashArea.All: {
                        return (uint)MyFlashDevice.PAGE_COUNT * (MyFlashDevice.PAGE_SIZE + (uint)MyFlashDevice.PAGE_EXT);
                    }
            }

            return 0U;
        }

        internal void WaitUntilReady() {
            try {
                byte[] sr;
                do {
                    sr = ReadStatusRegister();
                    if (FlashcatUSB.MainApp.AppIsClosing)
                        return;
                    if (sr[0] == 255)
                        break;
                }
                while (sr[0] & 1);
            } catch (Exception ex) {
            }
        }

        internal byte[] ReadData(long logical_address, long data_count, FlashcatUSB.FlashMemory.FlashArea memory_area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            if (FCUSB.SPI_NOR_IF.W25M121AV_Mode) {
                byte[] argReadBuffer = null;
                this.SPIBUS_WriteRead(new[] { 0xC2, 1 }, ReadBuffer: ref argReadBuffer);
                WaitUntilReady();
            }

            var page_addr = default(uint); // This is the page address
            var page_offset = default(ushort); // this is the start offset within the page
            var page_size = default(ushort);
            if (memory_area == FlashcatUSB.FlashMemory.FlashArea.Main) {
                page_size = (ushort)MyFlashDevice.PAGE_SIZE;
                page_addr = (uint)Math.Floor((double)logical_address / (double)MyFlashDevice.PAGE_SIZE);
                page_offset = (ushort)(logical_address - (long)(page_addr * MyFlashDevice.PAGE_SIZE));
            } else if (memory_area == FlashcatUSB.FlashMemory.FlashArea.OOB) {
                page_size = MyFlashDevice.PAGE_EXT;
                page_addr = (uint)Math.Floor((double)logical_address / (double)MyFlashDevice.PAGE_EXT);
                page_offset = (ushort)(logical_address - (long)(page_addr * (uint)MyFlashDevice.PAGE_EXT));
            } else if (memory_area == FlashcatUSB.FlashMemory.FlashArea.All) {   // we need to adjust large address to logical address
                page_size = (ushort)(MyFlashDevice.PAGE_SIZE + (uint)MyFlashDevice.PAGE_EXT);
                uint full_page_size = MyFlashDevice.PAGE_SIZE + (uint)MyFlashDevice.PAGE_EXT;
                page_addr = (uint)Math.Floor(logical_address / (double)full_page_size);
                page_offset = (ushort)(logical_address - page_addr * full_page_size);
            }

            uint pages_per_block = (uint)((double)MyFlashDevice.Block_Size / (double)MyFlashDevice.PAGE_SIZE);
            var data_out = new byte[(int)(data_count - 1L + 1)];
            int data_ptr = 0;
            while (data_count > 0L) {
                uint pages_left = pages_per_block - page_addr % pages_per_block;
                uint bytes_left_in_block = pages_left * page_size - page_offset;
                uint packet_size = (uint)Math.Min(bytes_left_in_block, data_count);
                page_addr = FCUSB.NAND_IF.GetPageMapping(page_addr);
                var data = ReadBulk_NAND(page_addr, page_offset, packet_size, memory_area);
                if (data is null)
                    return null;
                Array.Copy(data, 0, data_out, data_ptr, data.Length);
                data_ptr = (int)(data_ptr + packet_size);
                data_count -= packet_size;
                page_addr = (uint)(page_addr + Math.Ceiling(bytes_left_in_block / (double)page_size));
                page_offset = 0;
            }

            return data_out;
        }

        private byte[] ReadBulk_NAND(uint page_addr, ushort page_offset, uint data_count, FlashcatUSB.FlashMemory.FlashArea memory_area) {
            if ((long)MyFlashDevice.STACKED_DIES > 1L) {
                var data_to_read = new byte[(int)(data_count - 1L + 1)];
                uint array_ptr = 0U;
                uint bytes_left = data_count;
                while (bytes_left != 0L) {
                    uint buffer_size = 0U;
                    uint die_page_addr = GetPageForMultiDie(ref page_addr, page_offset, ref bytes_left, ref buffer_size, memory_area);
                    var die_data = ReadPages(die_page_addr, page_offset, buffer_size, memory_area);
                    Array.Copy(die_data, 0L, data_to_read, array_ptr, die_data.Length);
                    array_ptr += buffer_size;
                }

                return data_to_read;
            } else {
                return ReadPages(page_addr, page_offset, data_count, memory_area);
            }
        }

        internal uint SectorCount() {
            return MyFlashDevice.Sector_Count;
        }

        internal long SectorFind(uint sector_index, FlashcatUSB.FlashMemory.FlashArea memory_area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            uint base_addr = 0U;
            if (sector_index > 0L) {
                for (uint i = 0U, loopTo = (uint)(sector_index - 1L); i <= loopTo; i++)
                    base_addr += SectorSize(i, memory_area);
            }

            return base_addr;
        }

        internal bool EraseDevice() {
            if (FCUSB.SPI_NOR_IF.W25M121AV_Mode) {
                byte[] argReadBuffer = null;
                this.SPIBUS_WriteRead(new[] { 0xC2, 1 }, ReadBuffer: ref argReadBuffer);
                WaitUntilReady();
            }

            bool Result = FCUSB.NAND_IF.EraseChip();
            if (Result) {
                PrintConsole?.Invoke("Successfully erased NAND Flash device");
            } else {
                PrintConsole?.Invoke("Error while erasing NAND flash device");
            }

            return Result;
        }

        internal bool SectorErase(uint sector_index, FlashcatUSB.FlashMemory.FlashArea memory_area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            if (FCUSB.SPI_NOR_IF.W25M121AV_Mode) {
                byte[] argReadBuffer = null;
                this.SPIBUS_WriteRead(new[] { 0xC2, 1 }, ReadBuffer: ref argReadBuffer);
                WaitUntilReady();
            }

            uint pages_per_block = (uint)((double)MyFlashDevice.Block_Size / (double)MyFlashDevice.PAGE_SIZE);
            uint page_addr = pages_per_block * sector_index;
            uint local_page_addr = FCUSB.NAND_IF.GetPageMapping(page_addr);
            return FCUSB.NAND_IF.ERASEBLOCK(local_page_addr, memory_area, FlashcatUSB.MainApp.MySettings.NAND_Preserve);
        }

        internal bool SectorWrite(uint sector_index, byte[] data, FlashcatUSB.WriteParameters Params = null) {
            if (FCUSB.SPI_NOR_IF.W25M121AV_Mode) {
                byte[] argReadBuffer = null;
                this.SPIBUS_WriteRead(new[] { 0xC2, 1 }, ReadBuffer: ref argReadBuffer);
                WaitUntilReady();
            }

            uint Addr32 = (uint)this.SectorFind(sector_index, Params.Memory_Area);
            return WriteData(Addr32, data, Params);
        }

        internal bool WriteData(long logical_address, byte[] data_to_write, FlashcatUSB.WriteParameters Params = null) {
            if (FCUSB.SPI_NOR_IF.W25M121AV_Mode) {
                byte[] argReadBuffer = null;
                this.SPIBUS_WriteRead(new[] { 0xC2, 1 }, ReadBuffer: ref argReadBuffer);
                WaitUntilReady();
            }

            var page_addr = default(uint); // This is the page address
            ushort page_offset; // this is the start offset within the page (obsolete for now)
            if (Params.Memory_Area == FlashcatUSB.FlashMemory.FlashArea.Main) {
                page_addr = (uint)((double)logical_address / (double)MyFlashDevice.PAGE_SIZE);
                page_offset = (ushort)(logical_address - (long)(page_addr * MyFlashDevice.PAGE_SIZE));
            } else if (Params.Memory_Area == FlashcatUSB.FlashMemory.FlashArea.OOB) {
                page_addr = (uint)Math.Floor((double)logical_address / (double)MyFlashDevice.PAGE_EXT);
                page_offset = (ushort)(logical_address - (long)(page_addr * (uint)MyFlashDevice.PAGE_EXT));
            } else if (Params.Memory_Area == FlashcatUSB.FlashMemory.FlashArea.All) {   // we need to adjust large address to logical address
                page_addr = (uint)((double)logical_address / (double)MyFlashDevice.PAGE_EXT);
                page_offset = (ushort)(logical_address - (long)(page_addr * (MyFlashDevice.PAGE_SIZE + (uint)MyFlashDevice.PAGE_EXT)));
            }

            page_addr = FCUSB.NAND_IF.GetPageMapping(page_addr); // Adjusts the page to point to a valid page
            bool result = FCUSB.NAND_IF.WRITEPAGE(page_addr, data_to_write, Params.Memory_Area); // We will write the whole block instead
            FCUSB.USB_WaitForComplete();
            return result;
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        private void NANDHELPER_SetupHandlers() {
            this.FCUSB.NAND_IF.PrintConsole -= this.NAND_PrintConsole;
            this.FCUSB.NAND_IF.SetProgress -= this.NAND_SetProgress;
            this.FCUSB.NAND_IF.ReadPages -= this.NAND_ReadPages;
            this.FCUSB.NAND_IF.WritePages -= this.NAND_WritePages;
            this.FCUSB.NAND_IF.EraseSector -= this.NAND_EraseSector;
            this.FCUSB.NAND_IF.Ready -= this.WaitUntilReady;
            this.FCUSB.NAND_IF.PrintConsole += this.NAND_PrintConsole;
            this.FCUSB.NAND_IF.SetProgress += this.NAND_SetProgress;
            this.FCUSB.NAND_IF.ReadPages += this.NAND_ReadPages;
            this.FCUSB.NAND_IF.WritePages += this.NAND_WritePages;
            this.FCUSB.NAND_IF.EraseSector += this.NAND_EraseSector;
            this.FCUSB.NAND_IF.Ready += this.WaitUntilReady;
        }

        private void NAND_PrintConsole(string msg) {
            PrintConsole?.Invoke(msg);
        }

        private void NAND_SetProgress(int percent) {
            SetProgress?.Invoke(percent);
        }

        public void NAND_ReadPages(uint page_addr, ushort page_offset, uint data_count, FlashcatUSB.FlashMemory.FlashArea memory_area, ref byte[] data) {
            if ((long)MyFlashDevice.STACKED_DIES > 1L) {
                data = new byte[(int)(data_count - 1L + 1)];
                uint array_ptr = 0U;
                uint bytes_left = data_count;
                while (bytes_left != 0L) {
                    uint buffer_size = 0U;
                    uint die_page_addr = GetPageForMultiDie(ref page_addr, page_offset, ref bytes_left, ref buffer_size, memory_area);
                    var die_data = ReadPages(die_page_addr, page_offset, buffer_size, memory_area);
                    Array.Copy(die_data, 0L, data, array_ptr, die_data.Length);
                    array_ptr += buffer_size;
                }
            } else {
                data = ReadPages(page_addr, page_offset, data_count, memory_area);
            }
        }

        private void NAND_WritePages(uint page_addr, byte[] main, byte[] oob, FlashcatUSB.FlashMemory.FlashArea memory_area, ref bool write_result) {
            if ((long)MyFlashDevice.STACKED_DIES > 1L) {
                uint main_ptr = 0U;
                uint oob_ptr = 0U;
                if (memory_area == FlashcatUSB.FlashMemory.FlashArea.All) { // ignore oob()
                    if (main is null) {
                        write_result = false;
                        return;
                    }

                    uint bytes_left = (uint)main.Length;
                    while (bytes_left != 0L) {
                        uint main_buffer_size = 0U;
                        uint die_page_addr = GetPageForMultiDie(ref page_addr, 0, ref bytes_left, ref main_buffer_size, memory_area);
                        var die_data = new byte[(int)(main_buffer_size - 1L + 1)];
                        Array.Copy(main, main_ptr, die_data, 0L, die_data.Length);
                        main_ptr += main_buffer_size;
                        write_result = this.WriteBulk_SNAND(die_page_addr, die_data, null, FlashcatUSB.FlashMemory.FlashArea.All);
                        if (!write_result)
                            return;
                    }
                } else if (main is object) {
                    uint bytes_left = (uint)main.Length;
                    while (bytes_left != 0L) {
                        uint main_buffer_size = 0U;
                        uint die_page_addr = GetPageForMultiDie(ref page_addr, 0, ref bytes_left, ref main_buffer_size, memory_area);
                        var die_data = new byte[(int)(main_buffer_size - 1L + 1)];
                        byte[] die_oob = null;
                        Array.Copy(main, main_ptr, die_data, 0L, die_data.Length);
                        main_ptr += main_buffer_size;
                        if (oob is object) {
                            uint main_pages = (uint)(main_buffer_size / (double)MyFlashDevice.PAGE_SIZE);
                            uint oob_buffer_size = main_pages * (uint)MyFlashDevice.PAGE_EXT;
                            die_oob = new byte[(int)(oob_buffer_size - 1L + 1)];
                            Array.Copy(oob, oob_ptr, die_oob, 0L, die_oob.Length);
                            oob_ptr += oob_buffer_size;
                        }

                        write_result = this.WriteBulk_SNAND(die_page_addr, die_data, die_oob, FlashcatUSB.FlashMemory.FlashArea.Main);
                        if (!write_result)
                            return;
                    }
                } else if (oob is object) {
                    uint bytes_left = (uint)oob.Length;
                    while (bytes_left != 0L) {
                        uint oob_buffer_size = 0U;
                        uint die_page_addr = GetPageForMultiDie(ref page_addr, 0, ref bytes_left, ref oob_buffer_size, memory_area);
                        var die_oob = new byte[(int)(oob_buffer_size - 1L + 1)];
                        Array.Copy(oob, oob_ptr, die_oob, 0L, die_oob.Length);
                        oob_ptr += oob_buffer_size;
                        write_result = this.WriteBulk_SNAND(die_page_addr, null, die_oob, FlashcatUSB.FlashMemory.FlashArea.OOB);
                        if (!write_result)
                            return;
                    }
                } else {
                    write_result = false;
                }
            } else {
                write_result = WriteBulk_SNAND(page_addr, main, oob, memory_area);
            }
        }

        private void NAND_EraseSector(uint page_addr, ref bool erase_result) {
            if ((long)MyFlashDevice.STACKED_DIES > 1L) { // Multi-die support
                uint argcount = 0U;
                uint argbuffer_size = 0U;
                page_addr = this.GetPageForMultiDie(ref page_addr, (ushort)0, ref argcount, ref argbuffer_size, FlashcatUSB.FlashMemory.FlashArea.Main);
            }

            var block = new byte[3];
            block[2] = (byte)(page_addr >> 16 & 255L);
            block[1] = (byte)(page_addr >> 8 & 255L);
            block[0] = (byte)(page_addr & 255L);
            SPIBUS_WriteEnable();
            byte[] argReadBuffer = null;
            SPIBUS_WriteRead(new[] { OPCMD_BLOCKERASE, block[2], block[1], block[0] }, ReadBuffer: ref argReadBuffer);
            var reg = ReadStatusRegister();
            WaitUntilReady();
            erase_result = true;
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        private const byte OPCMD_WREN = 0x6;
        private const byte OPCMD_GETFEAT = 0xF;
        private const byte OPCMD_SETFEAT = 0x1F;
        private const byte OPCMD_SR = 0xC0;
        private const byte OPCMD_PAGEREAD = 0x13;
        private const byte OPCMD_PAGELOAD = 0x2; // Program Load / &H84
        private const byte OPCMD_READCACHE = 0xB;
        private const byte OPCMD_PROGCACHE = 0x10; // Program execute
        private const byte OPCMD_BLOCKERASE = 0xD8;
        private const byte OPCMD_DIESELECT = 0xC2;
        private const byte OPCMD_RESET = 0xFF;

        public uint SPIBUS_WriteRead(byte[] WriteBuffer, [Optional, DefaultParameterValue(null)] ref byte[] ReadBuffer) {
            if (WriteBuffer is null & ReadBuffer is null)
                return 0U;
            uint TotalBytesTransfered = 0U;
            SPIBUS_SlaveSelect_Enable();
            if (WriteBuffer is object) {
                int BytesWritten = 0;
                bool Result = SPIBUS_WriteData(WriteBuffer);
                if (WriteBuffer.Length > 2048)
                    FlashcatUSB.Utilities.Main.Sleep(2);
                if (Result)
                    TotalBytesTransfered = (uint)(TotalBytesTransfered + WriteBuffer.Length);
            }

            if (ReadBuffer is object) {
                int BytesRead = 0;
                bool Result = SPIBUS_ReadData(ref ReadBuffer);
                if (Result)
                    TotalBytesTransfered = (uint)(TotalBytesTransfered + ReadBuffer.Length);
            }

            SPIBUS_SlaveSelect_Disable();
            return TotalBytesTransfered;
        }
        // Makes the CS/SS pin go low
        private void SPIBUS_SlaveSelect_Enable() {
            FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.SPI_SS_ENABLE);
        }
        // Releases the CS/SS pin
        private void SPIBUS_SlaveSelect_Disable() {
            try {
                FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.SPI_SS_DISABLE);
            } catch (Exception ex) {
            }
        }

        private bool SPIBUS_WriteData(byte[] DataOut) {
            bool Success = false;
            try {
                Success = FCUSB.USB_SETUP_BULKOUT(FlashcatUSB.USB.USBREQ.SPI_WR_DATA, null, DataOut, (uint)DataOut.Length);
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
                Success = false;
            }

            if (!Success)
                PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("spi_error_reading"));
            return Success;
        }

        private bool SPIBUS_WriteEnable() {
            byte[] argReadBuffer = null;
            if (SPIBUS_WriteRead(new[] { OPCMD_WREN }, ref argReadBuffer) == 1L) {
                return true;
            } else {
                return false;
            }
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        public byte[] ReadStatusRegister() {
            try {
                var sr = new byte[1];
                SPIBUS_WriteRead(new[] { OPCMD_GETFEAT, OPCMD_SR }, ref sr);
                return sr;
            } catch (Exception ex) {
                return null;
            } // Erorr
        }

        public byte[] ReadPages(uint page_addr, ushort page_offset, uint count, FlashcatUSB.FlashMemory.FlashArea memory_area) {
            USB_Readages read_fnc;
            try {
                if (FCUSB.HasLogic) { // Hardware-enabled routine
                    read_fnc = new USB_Readages(ReadPages_Hardware);
                } else { // Software mode only
                    read_fnc = new USB_Readages(ReadPages_Software);
                }

                if (FlashcatUSB.MainApp.NAND_ECC is object && memory_area == FlashcatUSB.FlashMemory.FlashArea.Main) {
                    var NAND_DEV = MyFlashDevice;
                    uint page_count = (uint)Math.Ceiling((double)(count + (uint)page_offset) / (double)NAND_DEV.PAGE_SIZE); // Number of complete pages and OOB to read and correct
                    uint total_main_bytes = page_count * NAND_DEV.PAGE_SIZE;
                    uint total_oob_bytes = page_count * (uint)NAND_DEV.PAGE_EXT;
                    var main_area_data = read_fnc(page_addr, (ushort)0, total_main_bytes, FlashcatUSB.FlashMemory.FlashArea.Main);
                    var oob_area_data = read_fnc(page_addr, (ushort)0, total_oob_bytes, FlashcatUSB.FlashMemory.FlashArea.OOB);
                    var ecc_data = FlashcatUSB.MainApp.NAND_ECC.GetEccFromSpare(oob_area_data, (ushort)NAND_DEV.PAGE_SIZE, NAND_DEV.PAGE_EXT); // This strips out the ecc data from the spare area
                    FlashcatUSB.MainApp.ECC_LAST_RESULT = FlashcatUSB.MainApp.NAND_ECC.ReadData(main_area_data, ecc_data); // This processes the flash data (512 bytes at a time) and corrects for any errors using the ECC
                    if (FlashcatUSB.MainApp.ECC_LAST_RESULT == FlashcatUSB.ECC_LIB.ECC_DECODE_RESULT.Uncorractable) {
                        long logical_addr = page_addr * (long)MyFlashDevice.PAGE_SIZE + page_offset;
                        PrintConsole?.Invoke("ECC failed at: 0x" + Conversion.Hex(logical_addr).PadLeft(8, '0'));
                    }

                    var data_out = new byte[(int)(count - 1L + 1)]; // This is the data the user requested
                    Array.Copy(main_area_data, page_offset, data_out, 0, data_out.Length);
                    return data_out;
                } else {
                    return read_fnc(page_addr, page_offset, count, memory_area);
                }
            } catch (Exception ex) {
            }

            return null;
        }

        private byte[] ReadPages_Hardware(uint page_addr, ushort page_offset, uint data_count, FlashcatUSB.FlashMemory.FlashArea memory_area) {
            var setup = SetupPacket_NAND(page_addr, page_offset, data_count, memory_area);
            uint param = 0U;
            if (MyFlashDevice.PLANE_SELECT)
                param = (uint)(param | 1L);
            if (MyFlashDevice.READ_CMD_DUMMY)
                param = (uint)(param | (long)(1 << 1));
            var data_out = new byte[(int)(data_count - 1L + 1)];
            if (!FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.SPINAND_READFLASH, setup, ref data_out, param))
                return null;
            return data_out;
        }
        // SPI NAND software mode
        private byte[] ReadPages_Software(uint page_addr, ushort page_offset, uint count, FlashcatUSB.FlashMemory.FlashArea memory_area) {
            var data_out = new byte[(int)(count - 1L + 1)];
            uint bytes_left = count;
            ushort page_size_tot = (ushort)(MyFlashDevice.PAGE_SIZE + (uint)MyFlashDevice.PAGE_EXT);
            var nand_layout = FlashcatUSB.MainApp.NAND_LayoutTool.GetStructure(MyFlashDevice);
            uint data_ptr = 0U;
            if (FlashcatUSB.MainApp.MySettings.NAND_Layout == FlashcatUSB.NandMemLayout.Combined)
                memory_area = FlashcatUSB.FlashMemory.FlashArea.All;
            ushort logical_block = (ushort)(nand_layout.Layout_Main + nand_layout.Layout_Spare);
            while (bytes_left > 0L) {
                ushort read_offset = 0; // We always want to read the entire page
                if (MyFlashDevice.PLANE_SELECT)
                    read_offset = (ushort)(read_offset | 0x1000); // Sets plane select to HIGH
                var op_setup = GetReadCacheCommand(read_offset);
                var read_packet = new byte[page_size_tot];
                byte[] argReadBuffer = null;
                this.SPIBUS_WriteRead(new[] { OPCMD_PAGEREAD, (long)(page_addr >> 16) & 255L, (long)(page_addr >> 8) & 255L, (long)page_addr & 255L }, ReadBuffer: ref argReadBuffer);
                SPIBUS_WriteRead(op_setup, ref read_packet);
                switch (memory_area) {
                    case FlashcatUSB.FlashMemory.FlashArea.Main: {
                            ushort sub_index = (ushort)Math.Floor((double)page_offset / (double)nand_layout.Layout_Main); // the sub block we are in			
                            ushort adj_offset = (ushort)((ushort)(sub_index * logical_block) + (ushort)(page_offset % nand_layout.Layout_Main));
                            sub_index = (ushort)(sub_index + 1);
                            while (!(adj_offset == page_size_tot)) {
                                ushort sub_left = (ushort)(nand_layout.Layout_Main - (ushort)(page_offset % nand_layout.Layout_Main));
                                if (sub_left > bytes_left)
                                    sub_left = (ushort)bytes_left;
                                Array.Copy(read_packet, adj_offset, data_out, data_ptr, sub_left);
                                data_ptr += sub_left;
                                page_offset += sub_left;
                                bytes_left -= sub_left;
                                if (bytes_left == 0L)
                                    break;
                                adj_offset = (ushort)(sub_index * logical_block);
                                sub_index = (ushort)(sub_index + 1);
                            }

                            if (page_offset == MyFlashDevice.PAGE_SIZE) {
                                page_offset = 0;
                                page_addr = (uint)(page_addr + 1L);
                            }

                            break;
                        }

                    case FlashcatUSB.FlashMemory.FlashArea.OOB: {
                            ushort sub_index = (ushort)(Math.Floor((double)page_offset / (double)nand_layout.Layout_Spare) + 1d); // the sub block we are in			
                            ushort adj_offset = (ushort)((ushort)((ushort)(sub_index * logical_block) - nand_layout.Layout_Spare) + (ushort)(page_offset % nand_layout.Layout_Spare));
                            sub_index = (ushort)(sub_index + 1);
                            while (!((ushort)(adj_offset - nand_layout.Layout_Main) == page_size_tot)) {
                                ushort sub_left = (ushort)(nand_layout.Layout_Spare - (ushort)(page_offset % nand_layout.Layout_Spare));
                                if (sub_left > bytes_left)
                                    sub_left = (ushort)bytes_left;
                                Array.Copy(read_packet, adj_offset, data_out, data_ptr, sub_left);
                                data_ptr += sub_left;
                                page_offset += sub_left;
                                bytes_left -= sub_left;
                                if (bytes_left == 0L)
                                    break;
                                adj_offset = (ushort)((ushort)(sub_index * logical_block) - nand_layout.Layout_Spare);
                                sub_index = (ushort)(sub_index + 1);
                            }

                            if (page_offset == MyFlashDevice.PAGE_EXT) {
                                page_offset = 0;
                                page_addr = (uint)(page_addr + 1L);
                            }

                            break;
                        }

                    case FlashcatUSB.FlashMemory.FlashArea.All: {
                            ushort sub_left = (ushort)(page_size_tot - page_offset);
                            ushort transfer_count = sub_left;
                            if (transfer_count > bytes_left)
                                transfer_count = (ushort)bytes_left;
                            Array.Copy(read_packet, page_offset, data_out, data_ptr, transfer_count);
                            data_ptr += transfer_count;
                            bytes_left -= transfer_count;
                            sub_left -= transfer_count;
                            if (sub_left == 0) {
                                page_addr = (uint)(page_addr + 1L);
                                page_offset = 0;
                            } else {
                                page_offset = (ushort)(page_size_tot - sub_left);
                            }

                            break;
                        }
                }
            }

            return data_out;
        }

        private byte[] GetReadCacheCommand(ushort read_offset) {
            byte[] rd_cmd;
            if (MyFlashDevice.READ_CMD_DUMMY) {
                rd_cmd = new byte[5];
                rd_cmd[0] = OPCMD_READCACHE;
                rd_cmd[2] = (byte)(read_offset >> 8 & 255); // Column address (upper)
                rd_cmd[3] = (byte)(read_offset & 255); // Lower
            } else {
                rd_cmd = new byte[4];
                rd_cmd[0] = OPCMD_READCACHE;
                rd_cmd[1] = (byte)(read_offset >> 8 & 255); // Column address (upper)
                rd_cmd[2] = (byte)(read_offset & 255);
            } // Lower

            return rd_cmd;
        }

        private void SNAND_Wait() {
            SPIBUS_SlaveSelect_Enable();
            this.SPIBUS_WriteData(new[] { 0xF, 0xC0 });
            var sr = new byte[1];
            do {
                SPIBUS_ReadData(ref sr);
                FlashcatUSB.Utilities.Main.Sleep(5);
            }
            while (sr[0] & Conversions.ToShort(1 == 1));
            SPIBUS_SlaveSelect_Disable();
        }

        private bool WriteBulk_SNAND(uint page_addr, byte[] main_data, byte[] oob_data, FlashcatUSB.FlashMemory.FlashArea memory_area) {
            try {
                if (main_data is null & oob_data is null)
                    return false;
                var NAND_DEV = MyFlashDevice;
                ushort page_size_tot = (ushort)(MyFlashDevice.PAGE_SIZE + (uint)NAND_DEV.PAGE_EXT);
                byte[] page_aligned = null;
                if (memory_area == FlashcatUSB.FlashMemory.FlashArea.All) { // Ignore OOB/SPARE
                    oob_data = null;
                    uint total_pages = (uint)Math.Ceiling(main_data.Length / (double)page_size_tot);
                    page_aligned = new byte[(int)(total_pages * page_size_tot - 1L + 1)];
                    for (int i = 0, loopTo = page_aligned.Length - 1; i <= loopTo; i++)
                        page_aligned[i] = 255;
                    Array.Copy(main_data, 0, page_aligned, 0, main_data.Length);
                } else if (memory_area == FlashcatUSB.FlashMemory.FlashArea.Main) {
                    if (FlashcatUSB.MainApp.NAND_ECC is object) {
                        if (oob_data is null) {
                            oob_data = new byte[(int)(main_data.Length / (double)NAND_DEV.PAGE_SIZE * (double)NAND_DEV.PAGE_EXT - 1d + 1)];
                            FlashcatUSB.Utilities.Main.FillByteArray(ref oob_data, 255);
                        }

                        byte[] ecc_data = null;
                        FlashcatUSB.MainApp.NAND_ECC.WriteData(main_data, ref ecc_data);
                        FlashcatUSB.MainApp.NAND_ECC.SetEccToSpare(oob_data, ecc_data, (ushort)NAND_DEV.PAGE_SIZE, NAND_DEV.PAGE_EXT);
                    }

                    page_aligned = FlashcatUSB.MainApp.NAND_LayoutTool.CreatePageAligned(MyFlashDevice, main_data, oob_data);
                } else if (memory_area == FlashcatUSB.FlashMemory.FlashArea.OOB) {
                    page_aligned = FlashcatUSB.MainApp.NAND_LayoutTool.CreatePageAligned(MyFlashDevice, main_data, oob_data);
                }

                return USB_WritePageAlignedData(ref page_addr, page_aligned);
            } catch (Exception ex) {
            }

            return false;
        }

        private bool USB_WritePageAlignedData(ref uint page_addr, byte[] page_aligned) {
            ushort page_size_tot = (ushort)(MyFlashDevice.PAGE_SIZE + (uint)MyFlashDevice.PAGE_EXT);
            uint pages_to_write = (uint)(page_aligned.Length / (double)page_size_tot);
            if (FCUSB.HasLogic) { // Hardware-enabled routine
                uint array_ptr = 0U;
                while (pages_to_write != 0L) {
                    int max_page_count = (int)(8192d / (double)MyFlashDevice.PAGE_SIZE);
                    uint count = (uint)Math.Min(max_page_count, pages_to_write); // Write up to 4 pages (fcusb pro buffer has 12KB total)
                    var packet = new byte[(int)(count * page_size_tot - 1L + 1)];
                    Array.Copy(page_aligned, array_ptr, packet, 0L, packet.Length);
                    array_ptr = (uint)(array_ptr + packet.Length);
                    var setup = this.SetupPacket_NAND(page_addr, (ushort)0, (uint)packet.Length, FlashcatUSB.FlashMemory.FlashArea.All); // We will write the entire page
                    uint param = (uint)FlashcatUSB.Utilities.Main.BoolToInt(MyFlashDevice.PLANE_SELECT);
                    bool result = FCUSB.USB_SETUP_BULKOUT(FlashcatUSB.USB.USBREQ.SPINAND_WRITEFLASH, setup, packet, param);
                    if (!result)
                        return false;
                    FCUSB.USB_WaitForComplete();
                    page_addr += count;
                    pages_to_write -= count;
                }
            } else {
                for (long i = 0L, loopTo = pages_to_write - 1L; i <= loopTo; i++) {
                    var cache_data = new byte[page_size_tot];
                    Array.Copy(page_aligned, i * page_size_tot, cache_data, 0L, cache_data.Length);
                    SPIBUS_WriteEnable();
                    ushort cache_offset = 0;
                    if (MyFlashDevice.PLANE_SELECT) {
                        if (Conversions.ToBoolean((long)Math.Floor(page_addr / 64d) & Conversions.ToLong(1 == 1)))
                            cache_offset = (ushort)(cache_offset | 0x1000);
                    } // Sets plane select to HIGH

                    LoadPageCache(0, cache_data);
                    ProgramPageCache(page_addr);
                    page_addr = (uint)(page_addr + 1L);
                    WaitUntilReady();
                }
            }

            return true;
        }

        private void LoadPageCache(ushort page_offset, byte[] data_to_load) {
            var setup_packet = new byte[data_to_load.Length + 2 + 1];
            setup_packet[0] = OPCMD_PAGELOAD;
            setup_packet[1] = (byte)(page_offset >> 8 & 255); // Column address (upper)
            setup_packet[2] = (byte)(page_offset & 255); // Lower
            Array.Copy(data_to_load, 0, setup_packet, 3, data_to_load.Length);
            byte[] argReadBuffer = null;
            SPIBUS_WriteRead(setup_packet, ReadBuffer: ref argReadBuffer);
        }

        private void ProgramPageCache(uint page_addr) {
            var exe_packet = new byte[4];
            exe_packet[0] = OPCMD_PROGCACHE;
            exe_packet[1] = (byte)(page_addr >> 16 & 255L);
            exe_packet[2] = (byte)(page_addr >> 8 & 255L);
            exe_packet[3] = (byte)(page_addr & 255L);
            byte[] argReadBuffer = null;
            SPIBUS_WriteRead(exe_packet, ReadBuffer: ref argReadBuffer);
        }

        private byte[] SetupPacket_NAND(uint page_addr, ushort page_offset, uint Count, FlashcatUSB.FlashMemory.FlashArea area) {
            var nand_layout = FlashcatUSB.MainApp.NAND_LayoutTool.GetStructure(MyFlashDevice);
            if (FlashcatUSB.MainApp.MySettings.NAND_Layout == FlashcatUSB.NandMemLayout.Combined)
                area = FlashcatUSB.FlashMemory.FlashArea.All;
            ushort spare_size = MyFlashDevice.PAGE_EXT;
            var setup_data = new byte[20];
            setup_data[0] = (byte)(page_addr & 255L);
            setup_data[1] = (byte)(page_addr >> 8 & 255L);
            setup_data[2] = (byte)(page_addr >> 16 & 255L);
            setup_data[3] = (byte)(page_addr >> 24 & 255L);
            setup_data[4] = (byte)(Count & 255L);
            setup_data[5] = (byte)(Count >> 8 & 255L);
            setup_data[6] = (byte)(Count >> 16 & 255L);
            setup_data[7] = (byte)(Count >> 24 & 255L);
            setup_data[8] = (byte)(page_offset & 255);
            setup_data[9] = (byte)(page_offset >> 8 & 255);
            setup_data[10] = (byte)((long)MyFlashDevice.PAGE_SIZE & 255L);
            setup_data[11] = (byte)((long)(MyFlashDevice.PAGE_SIZE >> 8) & 255L);
            setup_data[12] = (byte)(spare_size & 255);
            setup_data[13] = (byte)(spare_size >> 8 & 255);
            setup_data[14] = (byte)((int)nand_layout.Layout_Main & 255);
            setup_data[15] = (byte)((int)(nand_layout.Layout_Main >> 8) & 255);
            setup_data[16] = (byte)((int)nand_layout.Layout_Spare & 255);
            setup_data[17] = (byte)((int)(nand_layout.Layout_Spare >> 8) & 255);
            setup_data[18] = 0; // (Addresssize) Not needed for SPI-NAND
            setup_data[19] = (byte)area; // Area (0=main,1=spare,2=all), note: all ignores layout settings
            return setup_data;
        }

        private uint GetPageForMultiDie(ref uint page_addr, ushort page_offset, ref uint count, ref uint buffer_size, FlashcatUSB.FlashMemory.FlashArea area) {
            uint total_pages = (uint)((double)MyFlashDevice.FLASH_SIZE / (double)MyFlashDevice.PAGE_SIZE);
            uint pages_per_die = (uint)(total_pages / (double)MyFlashDevice.STACKED_DIES);
            byte die_id = (byte)Math.Floor(page_addr / (double)pages_per_die);
            if (die_id != DIE_SELECTED) {
                byte[] argReadBuffer = null;
                SPIBUS_WriteRead(new[] { OPCMD_DIESELECT, die_id }, ReadBuffer: ref argReadBuffer);
                WaitUntilReady();
                DIE_SELECTED = die_id;
            }

            uint pages_left = pages_per_die - page_addr % pages_per_die;
            uint bytes_left; // Total number of bytes left in this die (for the selected area)
            var page_area_size = default(uint); // Number of bytes we are accessing
            switch (area) {
                case FlashcatUSB.FlashMemory.FlashArea.Main: {
                        page_area_size = MyFlashDevice.PAGE_SIZE;
                        break;
                    }

                case FlashcatUSB.FlashMemory.FlashArea.OOB: {
                        page_area_size = (uint)MyFlashDevice.PAGE_EXT;
                        break;
                    }

                case FlashcatUSB.FlashMemory.FlashArea.All: {
                        page_area_size = MyFlashDevice.PAGE_SIZE + (uint)MyFlashDevice.PAGE_EXT;
                        break;
                    }
            }

            bytes_left = (uint)(page_area_size - page_offset + page_area_size * (pages_left - 1L));
            buffer_size = Math.Min(count, bytes_left);
            count -= buffer_size;
            uint die_page_addr = page_addr % pages_per_die;
            return die_page_addr;
        }
    }
}