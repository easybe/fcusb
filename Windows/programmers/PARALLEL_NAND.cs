// COPYRIGHT EMBEDDED COMPUTERS LLC 2020 - ALL RIGHTS RESERVED
// THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB PRODUCTS
// CONTACT EMAIL: support@embeddedcomputers.net
// ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
// INFO: This is the main module that is loaded first.

using System;
using System.Linq;
using Microsoft.VisualBasic;

namespace FlashcatUSB {
    public class PARALLEL_NAND : FlashcatUSB.MemoryDeviceUSB {
        private FlashcatUSB.USB.FCUSB_DEVICE FCUSB;

        public FlashcatUSB.FlashMemory.P_NAND MyFlashDevice { get; set; }
        public FlashcatUSB.USB.DeviceStatus MyFlashStatus { get; set; } = FlashcatUSB.USB.DeviceStatus.NotDetected;
        public FlashcatUSB.FlashMemory.NAND_ONFI ONFI { get; set; }
        public FlashcatUSB.MEM_PROTOCOL MyAdapter { get; set; } // This is the kind of socket adapter connected and the mode it is in

        private FlashcatUSB.FlashMemory.FlashDetectResult FLASH_IDENT;

        public event PrintConsoleEventHandler PrintConsole;

        public delegate void PrintConsoleEventHandler(string msg);

        public event SetProgressEventHandler SetProgress;

        public delegate void SetProgressEventHandler(int percent);

        public PARALLEL_NAND(FlashcatUSB.USB.FCUSB_DEVICE parent_if) {
            FCUSB = parent_if;
        }

        public bool DeviceInit() {
            MyFlashDevice = null;
            ONFI = null;
            if (!this.EXPIO_SETUP_USB(FlashcatUSB.MEM_PROTOCOL.SETUP)) {
                PrintConsole?.Invoke("Parallel I/O failed to initialize");
                MyFlashStatus = FlashcatUSB.USB.DeviceStatus.ExtIoNotConnected;
                return false;
            } else {
                PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("io_mode_initalized"));
            }

            if (DetectFlashDevice()) {
                string chip_id_str = Conversion.Hex(FLASH_IDENT.MFG).PadLeft(2, '0') + Conversion.Hex(FLASH_IDENT.PART).PadLeft(8, '0');
                PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("ext_connected_chipid"), chip_id_str));
                if (FCUSB.HWBOARD == FlashcatUSB.USB.FCUSB_BOARD.XPORT_PCB2) {
                    if ((int)(FLASH_IDENT.ID1 >> 8) == 255)
                        FLASH_IDENT.ID1 = (ushort)((int)FLASH_IDENT.ID1 & 255);
                }

                var device_matches = FlashcatUSB.MainApp.FlashDatabase.FindDevices(FLASH_IDENT.MFG, FLASH_IDENT.ID1, FLASH_IDENT.ID2, FlashcatUSB.FlashMemory.MemoryType.PARALLEL_NAND);
                ONFI = new FlashcatUSB.FlashMemory.NAND_ONFI(NAND_GetONFITable());
                if (ONFI is null || !ONFI.IS_VALID) {
                    PrintConsole?.Invoke("NAND device failed to load ONFI table");
                }

                if (device_matches is object && device_matches.Count() > 0) {
                    this.MyFlashDevice_SelectBest(device_matches);
                    PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("flash_detected"), MyFlashDevice.NAME, Strings.Format((object)MyFlashDevice.FLASH_SIZE, "#,###")));
                    PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("ext_prog_mode"));
                    string page_info = string.Format(FlashcatUSB.MainApp.RM.GetString("ext_page_size"), Strings.Format((object)MyFlashDevice.PAGE_SIZE, "#,###"), (object)MyFlashDevice.PAGE_EXT);
                    PrintConsole?.Invoke(page_info);
                    PrintConsole?.Invoke("Block size: " + Strings.Format((object)MyFlashDevice.Block_Size, "#,###") + " bytes");
                    var nand_mem = MyFlashDevice;
                    if (nand_mem.IFACE == FlashcatUSB.FlashMemory.VCC_IF.X8_3V) {
                        PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("ext_device_interface") + ": NAND (X8 3.3V)");
                    } else if (nand_mem.IFACE == FlashcatUSB.FlashMemory.VCC_IF.X8_1V8) {
                        PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("ext_device_interface") + ": NAND (X8 1.8V)");
                    } else if (nand_mem.IFACE == FlashcatUSB.FlashMemory.VCC_IF.X16_3V) {
                        PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("ext_device_interface") + ": NAND (X16 3.3V)");
                    } else if (nand_mem.IFACE == FlashcatUSB.FlashMemory.VCC_IF.X16_1V8) {
                        PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("ext_device_interface") + ": NAND (X16 1.8V)");
                    }

                    if (FCUSB.HWBOARD == FlashcatUSB.USB.FCUSB_BOARD.XPORT_PCB2) {
                        if (!(nand_mem.IFACE == FlashcatUSB.FlashMemory.VCC_IF.X8_3V)) {
                            PrintConsole?.Invoke("This NAND device is not compatible with this programmer");
                            MyFlashStatus = FlashcatUSB.USB.DeviceStatus.NotCompatible;
                            return false;
                        }
                    }

                    if (nand_mem.IFACE == FlashcatUSB.FlashMemory.VCC_IF.X16_3V | nand_mem.IFACE == FlashcatUSB.FlashMemory.VCC_IF.X16_1V8) {
                        PrintConsole?.Invoke("NAND interface changed to X16");
                        this.EXPIO_SETUP_USB(FlashcatUSB.MEM_PROTOCOL.NAND_X16_ASYNC);
                        MyAdapter = FlashcatUSB.MEM_PROTOCOL.NAND_X16_ASYNC;
                    }

                    if (nand_mem.FLASH_SIZE > (long)FlashcatUSB.FlashMemory.Constants.Gb004) { // Remove this check if you wish
                        if (FCUSB.HWBOARD == FlashcatUSB.USB.FCUSB_BOARD.XPORT_PCB2) {
                            PrintConsole?.Invoke("XPORT is not compatible with NAND Flash larger than 4Gbit");
                            PrintConsole?.Invoke("Please upgrade to Mach1");
                            MyFlashStatus = FlashcatUSB.USB.DeviceStatus.NotCompatible;
                            return false;
                        }
                    }

                    NAND_SetupHandlers();
                    FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.NAND_SETTYPE, null, (uint)nand_mem.ADDRESS_SCHEME);
                    FCUSB.NAND_IF.CreateMap(nand_mem.FLASH_SIZE, nand_mem.PAGE_SIZE, (uint)nand_mem.PAGE_EXT, nand_mem.PAGE_COUNT, nand_mem.Sector_Count);
                    FCUSB.NAND_IF.EnableBlockManager(); // If enabled 
                    FCUSB.NAND_IF.ProcessMap();
                    FlashcatUSB.MainApp.ECC_LoadConfiguration((int)nand_mem.PAGE_SIZE, (int)nand_mem.PAGE_EXT);
                    FlashcatUSB.Utilities.Main.Sleep(10); // We need to wait here (device is being configured)
                    MyFlashStatus = FlashcatUSB.USB.DeviceStatus.Supported;
                    return true;
                } else {
                    PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("unknown_device_email"));
                    MyFlashDevice = null;
                    MyFlashStatus = FlashcatUSB.USB.DeviceStatus.NotSupported;
                }
            } else {
                FlashcatUSB.MainApp.GUI.PrintConsole(FlashcatUSB.MainApp.RM.GetString("ext_not_detected"));
                MyFlashStatus = FlashcatUSB.USB.DeviceStatus.NotDetected;
            }

            return false;
        }

        private void MyFlashDevice_SelectBest(FlashcatUSB.FlashMemory.Device[] device_matches) {
            if (device_matches.Length == 1) {
                MyFlashDevice = (FlashcatUSB.FlashMemory.P_NAND)device_matches[0];
                return;
            }

            if (ONFI.IS_VALID) {
                for (int i = 0, loopTo = device_matches.Count() - 1; i <= loopTo; i++) {
                    string PART = device_matches[i].NAME.ToUpper();
                    if (PART.IndexOf(" ") > 0)
                        PART = PART.Substring(PART.IndexOf(" ") + 1);
                    if (PART.Equals(ONFI.DEVICE_MODEL)) {
                        MyFlashDevice = (FlashcatUSB.FlashMemory.P_NAND)device_matches[i];
                        return;
                    }
                }
            }

            if (MyFlashDevice is null)
                MyFlashDevice = (FlashcatUSB.FlashMemory.P_NAND)device_matches[0];
        }

        private byte NAND_GetSR() {
            var result_data = new byte[1];
            bool result = FCUSB.USB_CONTROL_MSG_IN(FlashcatUSB.USB.USBREQ.NAND_SR, ref result_data);
            return result_data[0]; // E0 11100000
        }

        private byte[] NAND_GetONFITable() {
            var onfi = new byte[256];
            if (FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.NAND_ONFI, null, ref onfi, 0U)) {
                return onfi;
            }

            return null;
        }

        private bool DetectFlashDevice() {
            PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("ext_detecting_device")); // Attempting to automatically detect Flash device
            FlashcatUSB.FlashMemory.FlashDetectResult LAST_DETECT = default;
            LAST_DETECT.MFG = 0;
            FLASH_IDENT = this.DetectFlash(FlashcatUSB.MEM_PROTOCOL.NAND_X8_ASYNC); // X16 and X8 are detected with X8
            if (FLASH_IDENT.Successful) {
                var d = FlashcatUSB.MainApp.FlashDatabase.FindDevices(FLASH_IDENT.MFG, FLASH_IDENT.ID1, FLASH_IDENT.ID2, FlashcatUSB.FlashMemory.MemoryType.PARALLEL_NAND);
                if (d.Count() > 0) {
                    PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("ext_device_detected"), "NAND"));
                    MyAdapter = FlashcatUSB.MEM_PROTOCOL.NAND_X8_ASYNC;
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

        private FlashcatUSB.FlashMemory.FlashDetectResult DetectFlash(FlashcatUSB.MEM_PROTOCOL mode) {
            string mode_name = "";
            var ident_data = new byte[8]; // 8 bytes total
            var result = default(FlashcatUSB.FlashMemory.FlashDetectResult);
            switch (mode) {
                case FlashcatUSB.MEM_PROTOCOL.NAND_X8_ASYNC: {
                        mode_name = "NAND X8 ASYNC";
                        this.EXPIO_SETUP_USB(FlashcatUSB.MEM_PROTOCOL.NAND_X8_ASYNC);
                        result = EXPIO_DetectNAND();
                        break;
                    }
            }

            if (result.Successful) {
                uint part = (uint)result.ID1 << 16 | (uint)result.ID2;
                string chip_id_str = Conversion.Hex(result.MFG).PadLeft(2, '0') + Conversion.Hex(part).PadLeft(8, '0');
                PrintConsole?.Invoke("Mode " + mode_name + " returned ident code: 0x" + chip_id_str);
            }

            return result;
        }

        private FlashcatUSB.FlashMemory.FlashDetectResult EXPIO_DetectNAND() {
            var ident_data = new byte[8];
            if (FCUSB.USB_CONTROL_MSG_IN(FlashcatUSB.USB.USBREQ.EXPIO_RDID, ref ident_data)) {
                return FlashcatUSB.FlashMemory.Tools.GetFlashResult(ident_data);
            } else {
                PrintConsole?.Invoke("Error detecting NAND device");
                return default;
            }
        }


        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        private void NAND_SetupHandlers() {
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
            data = ReadBulk(page_addr, page_offset, data_count, memory_area);
        }

        private void NAND_WritePages(uint page_addr, byte[] main, byte[] oob, FlashcatUSB.FlashMemory.FlashArea memory_area, ref bool write_result) {
            write_result = WriteBulk(page_addr, main, oob, memory_area);
        }

        private void NAND_EraseSector(uint page_addr, ref bool erase_result) {
            erase_result = FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.EXPIO_SECTORERASE, null, page_addr);
            if ((long)MyFlashDevice.PAGE_SIZE == 512L) { // LEGACY NAND DEVICE
                FlashcatUSB.Utilities.Main.Sleep(250); // Micron NAND legacy delay (was 200), always wait! Just to be sure.
            } else if (!(FCUSB.HWBOARD == FlashcatUSB.USB.FCUSB_BOARD.Mach1)) { // Mach1 uses HW to get correct wait
                FlashcatUSB.Utilities.Main.Sleep(50); // Normal delay
            }
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        private enum E_BUS_WIDTH { // Number of bits transfered per operation
            X0 = 0, // Default
            X8 = 8,
            X16 = 16
        }

        private E_BUS_WIDTH CURRENT_BUS_WIDTH { get; set; } = E_BUS_WIDTH.X0;

        private bool EXPIO_SETUP_USB(FlashcatUSB.MEM_PROTOCOL mode) {
            try {
                var result_data = new byte[1];
                uint setup_data = (uint)mode;
                if (FCUSB.HWBOARD == FlashcatUSB.USB.FCUSB_BOARD.Mach1) {
                    if (mode == FlashcatUSB.MEM_PROTOCOL.NAND_X16_ASYNC) {
                        setup_data = (uint)(setup_data | (long)((int)FlashcatUSB.MainApp.MySettings.NAND_Speed << 16));
                        PrintConsole?.Invoke("NAND clock speed set to: " + FlashcatUSB.FlashcatSettings.NandMemSpeedToString(FlashcatUSB.MainApp.MySettings.NAND_Speed));
                    } else if (mode == FlashcatUSB.MEM_PROTOCOL.NAND_X8_ASYNC) {
                        setup_data = (uint)(setup_data | (long)((int)FlashcatUSB.MainApp.MySettings.NAND_Speed << 16));
                        PrintConsole?.Invoke("NAND clock speed set to: " + FlashcatUSB.FlashcatSettings.NandMemSpeedToString(FlashcatUSB.MainApp.MySettings.NAND_Speed));
                    }
                }

                bool result = FCUSB.USB_CONTROL_MSG_IN(FlashcatUSB.USB.USBREQ.EXPIO_INIT, ref result_data, setup_data);
                if (!result)
                    return false;
                if (result_data[0] == 0x17) { // Extension port returns 0x17 if it can communicate with the MCP23S17
                    System.Threading.Thread.Sleep(50); // Give the USB time to change modes
                    switch (mode) {
                        case FlashcatUSB.MEM_PROTOCOL.NAND_X8_ASYNC: {
                                CURRENT_BUS_WIDTH = E_BUS_WIDTH.X8;
                                break;
                            }

                        case FlashcatUSB.MEM_PROTOCOL.NAND_X16_ASYNC: {
                                CURRENT_BUS_WIDTH = E_BUS_WIDTH.X16;
                                break;
                            }
                    }

                    return true; // Communication successful
                } else {
                    return false;
                }
            } catch (Exception ex) {
                return false;
            }
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        public string DeviceName {
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

        public long DeviceSize {
            get {
                var d = MyFlashDevice;
                long available_pages = (long)FCUSB.NAND_IF.MAPPED_PAGES;
                if (FlashcatUSB.MainApp.NAND_LayoutTool.Layout == FlashcatUSB.NandMemLayout.Combined) {
                    return available_pages * (long)(d.PAGE_SIZE + (uint)d.PAGE_EXT);
                } else {
                    return available_pages * (long)d.PAGE_SIZE;
                }
            }
        }

        public byte[] ReadData(long logical_address, long data_count, FlashcatUSB.FlashMemory.FlashArea memory_area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            var nand_dev = MyFlashDevice;
            var page_addr = default(long);  // This is the page address
            var page_offset = default(ushort); // this is the start offset within the page
            var page_size = default(uint);
            if (memory_area == FlashcatUSB.FlashMemory.FlashArea.Main) {
                page_addr = (long)Math.Floor((double)logical_address / (double)(long)MyFlashDevice.PAGE_SIZE);
                page_size = nand_dev.PAGE_SIZE;
                page_offset = (ushort)(logical_address - page_addr * (long)MyFlashDevice.PAGE_SIZE);
            } else if (memory_area == FlashcatUSB.FlashMemory.FlashArea.OOB) {
                page_addr = (long)Math.Floor((double)logical_address / (double)nand_dev.PAGE_EXT);
                page_offset = (ushort)(logical_address - page_addr * (long)nand_dev.PAGE_EXT);
                page_size = (uint)nand_dev.PAGE_EXT;
            } else if (memory_area == FlashcatUSB.FlashMemory.FlashArea.All) {   // we need to adjust large address to logical address
                long full_page_size = (long)(MyFlashDevice.PAGE_SIZE + (uint)nand_dev.PAGE_EXT);
                page_addr = (long)Math.Floor(logical_address / (double)full_page_size);
                page_offset = (ushort)(logical_address - page_addr * full_page_size);
                page_size = nand_dev.PAGE_SIZE + (uint)nand_dev.PAGE_EXT;
            }
            // The following code is so we can read past invalid blocks
            uint pages_per_block = (uint)nand_dev.PAGE_COUNT;
            var data_out = new byte[(int)(data_count - 1L + 1)];
            int data_ptr = 0;
            while (data_count > 0L) {
                uint pages_left = (uint)(pages_per_block - page_addr % pages_per_block);
                uint bytes_left_in_block = pages_left * page_size - page_offset;
                uint packet_size = (uint)Math.Min(bytes_left_in_block, data_count);
                page_addr = (long)FCUSB.NAND_IF.GetPageMapping((uint)page_addr);
                var data = ReadBulk((uint)page_addr, page_offset, packet_size, memory_area);
                if (data is null)
                    return null;
                Array.Copy(data, 0, data_out, data_ptr, data.Length);
                data_ptr = (int)(data_ptr + packet_size);
                data_count -= packet_size;
                page_addr = (long)(page_addr + Math.Ceiling(bytes_left_in_block / (double)page_size));
                page_offset = 0;
            }

            return data_out;
        }

        public bool SectorErase(uint sector_index, FlashcatUSB.FlashMemory.FlashArea memory_area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            if (!MyFlashDevice.ERASE_REQUIRED)
                return true;
            uint page_addr = (uint)MyFlashDevice.PAGE_COUNT * sector_index;
            uint local_page_addr = FCUSB.NAND_IF.GetPageMapping(page_addr);
            return FCUSB.NAND_IF.ERASEBLOCK(local_page_addr, memory_area, FlashcatUSB.MainApp.MySettings.NAND_Preserve);
        }

        public bool WriteData(long logical_address, byte[] data_to_write, FlashcatUSB.WriteParameters Params = null) {
            uint page_addr = FlashcatUSB.MainApp.NAND_LayoutTool.GetNandPageAddress(MyFlashDevice, logical_address, Params.Memory_Area);
            page_addr = FCUSB.NAND_IF.GetPageMapping(page_addr); // Adjusts the page to point to a valid page
            bool result = FCUSB.NAND_IF.WRITEPAGE(page_addr, data_to_write, Params.Memory_Area); // We will write the whole block instead
            FCUSB.USB_WaitForComplete();
            return result;
            return false;
        }

        public void WaitUntilReady() {
            FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.EXPIO_WAIT);
            FCUSB.USB_WaitForComplete(); // Checks for WAIT flag to clear
        }

        public void WaitForReady() => WaitUntilReady();

        public long SectorFind(uint sector_index, FlashcatUSB.FlashMemory.FlashArea memory_area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            uint base_addr = 0U;
            if (sector_index > 0L) {
                for (uint i = 0U, loopTo = (uint)(sector_index - 1L); i <= loopTo; i++)
                    base_addr += SectorSize(i, memory_area);
            }

            return base_addr;
        }

        public bool SectorWrite(uint sector_index, byte[] data, FlashcatUSB.WriteParameters Params = null) {
            uint Addr32 = (uint)this.SectorFind(sector_index, Params.Memory_Area);
            return WriteData(Addr32, data, Params);
        }

        public uint SectorCount() {
            return MyFlashDevice.Sector_Count;
        }

        public bool EraseDevice() {
            try {
                bool Result = FCUSB.NAND_IF.EraseChip();
                if (Result) {
                    PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("nand_erase_successful"));
                } else {
                    PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("nand_erase_failed"));
                }
            } catch (Exception ex) {
            }

            return false;
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

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        public byte[] ReadBulk(uint page_addr, ushort page_offset, uint count, FlashcatUSB.FlashMemory.FlashArea memory_area) {
            try {
                bool result;
                if (FlashcatUSB.MainApp.NAND_ECC is object && memory_area == FlashcatUSB.FlashMemory.FlashArea.Main) { // We need to auto-correct data uisng ECC
                    uint page_count = (uint)Math.Ceiling((double)(count + (uint)page_offset) / (double)MyFlashDevice.PAGE_SIZE); // Number of complete pages and OOB to read and correct
                    uint total_main_bytes = page_count * MyFlashDevice.PAGE_SIZE;
                    uint total_oob_bytes = page_count * (uint)MyFlashDevice.PAGE_EXT;
                    var main_area_data = new byte[(int)(total_main_bytes - 1L + 1)]; // Data from the main page
                    var setup_data = this.GetSetupPacket(page_addr, (ushort)0, (uint)main_area_data.Length, FlashcatUSB.FlashMemory.FlashArea.Main);
                    result = FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.EXPIO_READDATA, setup_data, ref main_area_data, 1U);
                    if (!result)
                        return null;
                    var oob_area_data = new byte[(int)(total_oob_bytes - 1L + 1)]; // Data from the spare page, containing flags, metadata and ecc data
                    setup_data = this.GetSetupPacket(page_addr, (ushort)0, (uint)oob_area_data.Length, FlashcatUSB.FlashMemory.FlashArea.OOB);
                    result = FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.EXPIO_READDATA, setup_data, ref oob_area_data, 1U);
                    if (!result)
                        return null;
                    var ecc_data = FlashcatUSB.MainApp.NAND_ECC.GetEccFromSpare(oob_area_data, (ushort)MyFlashDevice.PAGE_SIZE, MyFlashDevice.PAGE_EXT); // This strips out the ecc data from the spare area
                    FlashcatUSB.MainApp.ECC_LAST_RESULT = FlashcatUSB.MainApp.NAND_ECC.ReadData(main_area_data, ecc_data); // This processes the flash data (512 bytes at a time) and corrects for any errors using the ECC
                    if (FlashcatUSB.MainApp.ECC_LAST_RESULT == FlashcatUSB.ECC_LIB.ECC_DECODE_RESULT.Uncorractable) {
                        long logical_addr = page_addr * (long)MyFlashDevice.PAGE_SIZE + page_offset;
                        PrintConsole?.Invoke("ECC failed at: 0x" + Conversion.Hex(logical_addr).PadLeft(8, '0'));
                    }

                    var data_out = new byte[(int)(count - 1L + 1)]; // This is the data the user requested
                    Array.Copy(main_area_data, page_offset, data_out, 0, data_out.Length);
                    return data_out;
                } else { // Normal read from device
                    var data_out = new byte[(int)(count - 1L + 1)]; // Bytes we want to read
                    var setup_data = GetSetupPacket(page_addr, page_offset, count, memory_area);
                    result = FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.EXPIO_READDATA, setup_data, ref data_out, 1U);
                    if (!result)
                        return null;
                    return data_out;
                }
            } catch (Exception ex) {
            }

            return null;
        }

        private bool WriteBulk(uint page_addr, byte[] main_data, byte[] oob_data, FlashcatUSB.FlashMemory.FlashArea memory_area) {
            try {
                if (main_data is null & oob_data is null)
                    return false;
                ushort page_size_tot = (ushort)(MyFlashDevice.PAGE_SIZE + (uint)MyFlashDevice.PAGE_EXT);
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
                            oob_data = new byte[(int)(main_data.Length / (double)MyFlashDevice.PAGE_SIZE * (double)MyFlashDevice.PAGE_EXT - 1d + 1)];
                            FlashcatUSB.Utilities.Main.FillByteArray(ref oob_data, 255);
                        }

                        byte[] ecc_data = null;
                        FlashcatUSB.MainApp.NAND_ECC.WriteData(main_data, ref ecc_data);
                        FlashcatUSB.MainApp.NAND_ECC.SetEccToSpare(oob_data, ecc_data, (ushort)MyFlashDevice.PAGE_SIZE, MyFlashDevice.PAGE_EXT);
                    }

                    page_aligned = FlashcatUSB.MainApp.NAND_LayoutTool.CreatePageAligned(MyFlashDevice, main_data, oob_data);
                } else if (memory_area == FlashcatUSB.FlashMemory.FlashArea.OOB) {
                    page_aligned = FlashcatUSB.MainApp.NAND_LayoutTool.CreatePageAligned(MyFlashDevice, main_data, oob_data);
                }

                uint pages_to_write = (uint)(page_aligned.Length / (double)page_size_tot);
                uint array_ptr = 0U;
                while (pages_to_write != 0L) {
                    int page_count_max = this.GetMaxPacketCount(MyFlashDevice.PAGE_SIZE);
                    uint page_count = (uint)Math.Min(page_count_max, pages_to_write);
                    var packet = new byte[(int)(page_count * page_size_tot - 1L + 1)];
                    Array.Copy(page_aligned, array_ptr, packet, 0L, packet.Length);
                    array_ptr = (uint)(array_ptr + packet.Length);
                    var setup = this.GetSetupPacket(page_addr, (ushort)0, (uint)packet.Length, FlashcatUSB.FlashMemory.FlashArea.All); // We will write the entire page
                    bool result = FCUSB.USB_SETUP_BULKOUT(FlashcatUSB.USB.USBREQ.EXPIO_WRITEDATA, setup, packet, 1U);
                    if (!result)
                        return default;
                    FCUSB.USB_WaitForComplete();
                    page_addr += page_count;
                    pages_to_write -= page_count;
                }

                return true;
            } catch (Exception ex) {
            }

            return false;
        }
        // Returns the max number of pages the hardware can support via bulk write
        private int GetMaxPacketCount(uint PageSize) {
            int page_count_max = 0; // Number of total pages to write per operation
            if (PageSize == 512L) {
                page_count_max = 8;
            } else if (PageSize == 2048L) {
                page_count_max = 4;
            } else if (PageSize == 4096L) {
                page_count_max = 2;
            } else if (PageSize == 8192L) {
                page_count_max = 1;
            }

            return page_count_max;
        }

        private byte[] GetSetupPacket(uint page_addr, ushort page_offset, uint transfer_size, FlashcatUSB.FlashMemory.FlashArea memory_area) {
            var nand_layout = FlashcatUSB.MainApp.NAND_LayoutTool.GetStructure(MyFlashDevice);
            if (FlashcatUSB.MainApp.NAND_LayoutTool.Layout == FlashcatUSB.NandMemLayout.Combined)
                memory_area = FlashcatUSB.FlashMemory.FlashArea.All;
            byte TX_NAND_ADDRSIZE; // Number of bytes the address command table uses
            if ((long)MyFlashDevice.PAGE_SIZE == 512L) { // Small page
                if (MyFlashDevice.FLASH_SIZE > (long)FlashcatUSB.FlashMemory.Constants.Mb256) {
                    TX_NAND_ADDRSIZE = 4;
                } else {
                    TX_NAND_ADDRSIZE = 3;
                }
            } else if (MyFlashDevice.FLASH_SIZE < (long)FlashcatUSB.FlashMemory.Constants.Gb002) {
                TX_NAND_ADDRSIZE = 4; // <=1Gbit
            } else {
                TX_NAND_ADDRSIZE = 5;
            } // 2Gbit+

            var setup_data = new byte[22]; // 18 bytes total
            setup_data[0] = (byte)(page_addr & 255L);
            setup_data[1] = (byte)(page_addr >> 8 & 255L);
            setup_data[2] = (byte)(page_addr >> 16 & 255L);
            setup_data[3] = (byte)(page_addr >> 24 & 255L);
            setup_data[4] = (byte)(transfer_size & 255L);
            setup_data[5] = (byte)(transfer_size >> 8 & 255L);
            setup_data[6] = (byte)(transfer_size >> 16 & 255L);
            setup_data[7] = (byte)(transfer_size >> 24 & 255L);
            setup_data[8] = (byte)(page_offset & 255);
            setup_data[9] = (byte)(page_offset >> 8 & 255);
            setup_data[10] = (byte)((long)MyFlashDevice.PAGE_SIZE & 255L);
            setup_data[11] = (byte)((long)(MyFlashDevice.PAGE_SIZE >> 8) & 255L);
            setup_data[12] = (byte)((int)MyFlashDevice.PAGE_EXT & 255);
            setup_data[13] = (byte)((int)(MyFlashDevice.PAGE_EXT >> 8) & 255);
            setup_data[14] = (byte)((int)nand_layout.Layout_Main & 255);
            setup_data[15] = (byte)((int)(nand_layout.Layout_Main >> 8) & 255);
            setup_data[16] = (byte)((int)nand_layout.Layout_Spare & 255);
            setup_data[17] = (byte)((int)(nand_layout.Layout_Spare >> 8) & 255);
            setup_data[18] = (byte)((int)MyFlashDevice.PAGE_COUNT & 255);
            setup_data[19] = (byte)((int)(MyFlashDevice.PAGE_COUNT >> 8) & 255);
            setup_data[20] = TX_NAND_ADDRSIZE;
            setup_data[21] = (byte)memory_area; // Area (0=main,1=spare,2=all), note: all ignores layout settings
            return setup_data;
        }
    }
}