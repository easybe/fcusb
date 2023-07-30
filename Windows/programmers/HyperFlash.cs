using System;
using System.Linq;
using Microsoft.VisualBasic;

namespace FlashcatUSB {
    public class HF_Programmer : FlashcatUSB.MemoryDeviceUSB {
        private FlashcatUSB.USB.FCUSB_DEVICE FCUSB;

        public FlashcatUSB.FlashMemory.HYPERFLASH MyFlashDevice { get; set; }  // Contains the definition for the parallel mode device that is connected
        public FlashcatUSB.USB.DeviceStatus MyFlashStatus { get; set; } = FlashcatUSB.USB.DeviceStatus.NotDetected;

        public event PrintConsoleEventHandler PrintConsole;

        public delegate void PrintConsoleEventHandler(string msg);

        public event SetProgressEventHandler SetProgress;

        public delegate void SetProgressEventHandler(int percent); // Not used

        public HF_Programmer(FlashcatUSB.USB.FCUSB_DEVICE parent_if) {
            FCUSB = parent_if;
        }

        public bool DeviceInit() {
            FCUSB.USB_VCC_OFF();
            MyFlashDevice = null;
            this.EXPIO_SETUP_USB(FlashcatUSB.MEM_PROTOCOL.HYPERFLASH);
            FCUSB.USB_VCC_ON(FlashcatUSB.MainApp.MySettings.VOLT_SELECT);
            FlashcatUSB.Utilities.Main.Sleep(200);
            var HF_DETECT = DetectFlash();
            if (HF_DETECT.Successful) {
                string chip_id_str = Conversion.Hex(HF_DETECT.MFG).PadLeft(2, '0') + Conversion.Hex(HF_DETECT.ID1).PadLeft(8, '0');
                PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("ext_connected_chipid"), chip_id_str));
                var device_matches = FlashcatUSB.MainApp.FlashDatabase.FindDevices(HF_DETECT.MFG, HF_DETECT.ID1, 0, FlashcatUSB.FlashMemory.MemoryType.HYPERFLASH);
                if (device_matches is object && device_matches.Count() > 0) {
                    if (MyFlashDevice is null)
                        MyFlashDevice = (FlashcatUSB.FlashMemory.HYPERFLASH)device_matches[0];
                    PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("flash_detected"), MyFlashDevice.NAME, Strings.Format((object)MyFlashDevice.FLASH_SIZE, "#,###")));
                    MyFlashStatus = FlashcatUSB.USB.DeviceStatus.Supported;
                } else {
                    PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("unknown_device_email"));
                    MyFlashStatus = FlashcatUSB.USB.DeviceStatus.NotSupported;
                }

                return true;
            } else {
                FlashcatUSB.MainApp.GUI.PrintConsole(FlashcatUSB.MainApp.RM.GetString("ext_not_detected"));
                MyFlashStatus = FlashcatUSB.USB.DeviceStatus.NotDetected;
                return false;
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
                return MyFlashDevice.FLASH_SIZE;
            }
        }

        public byte[] ReadData(long logical_address, long data_count, FlashcatUSB.FlashMemory.FlashArea memory_area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            return ReadBulk_NOR((uint)logical_address, (uint)data_count);
        }

        public bool SectorErase(uint sector_index, FlashcatUSB.FlashMemory.FlashArea memory_area = FlashcatUSB.FlashMemory.FlashArea.Main) {
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

                    bool Result = false;
                    try {
                        Result = FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.EXPIO_SECTORERASE, null, Logical_Address);
                    } catch (Exception ex) {
                    }

                    if (!Result)
                        return false;
                    FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.EXPIO_WAIT); // Calls the assigned WAIT function (uS, mS, SR, DQ7)
                    FCUSB.USB_WaitForComplete(); // Checks for WAIT flag to clear
                    bool blank_result = false;
                    uint timeout = 0U;
                    while (!blank_result) {
                        byte sr = GetStatusRegister();
                        if ((sr >> 7 & 1) == 1)
                            break;
                        timeout = (uint)(timeout + 1L);
                        if (timeout == 10L)
                            return false;
                        if (!blank_result)
                            FlashcatUSB.Utilities.Main.Sleep(100);
                    }
                }
            } catch {
            }

            return true;
        }

        public bool WriteData(long logical_address, byte[] data_to_write, FlashcatUSB.WriteParameters Params = null) {
            try {
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
                    ReturnValue = WriteBulk_NOR((uint)logical_address, data);
                    if (!ReturnValue)
                        return false;
                    logical_address += data.Length;
                    DataToWrite = (uint)(DataToWrite - data.Length);
                    FCUSB.USB_WaitForComplete();
                }

                WaitUntilReady();
            } catch (Exception ex) {
            }

            return true;
        }

        public void WaitUntilReady() {
            try {
                FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.EXPIO_WAIT);
                FCUSB.USB_WaitForComplete(); // Checks for WAIT flag to clear
            } catch (Exception ex) {
            }
        }

        public void WaitForReady() => WaitUntilReady();

        public byte GetStatusRegister() {
            var d = new byte[1];
            if (!FCUSB.USB_CONTROL_MSG_IN(FlashcatUSB.USB.USBREQ.EXPIO_SR, ref d))
                return 0;
            return d[0];
        }

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
                try {
                    FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.EXPIO_CHIPERASE);
                    FlashcatUSB.Utilities.Main.Sleep(1000); // Perform blank check
                    for (int i = 0; i <= 119; i++) { // 2 minutes
                        byte sr = GetStatusRegister();
                        if ((sr >> 7 & 1) == 1)
                            return true;
                        FlashcatUSB.Utilities.Main.Sleep(1000);
                    }

                    return false; // Timeout (device erase failed)
                } catch (Exception ex) {
                }
            } catch (Exception ex) {
            }

            return false;
        }

        internal uint SectorSize(uint sector, FlashcatUSB.FlashMemory.FlashArea memory_area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            return MyFlashDevice.SECTOR_SIZE;
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        private bool EXPIO_SETUP_USB(FlashcatUSB.MEM_PROTOCOL mode) {
            try {
                var result_data = new byte[1];
                bool result = FCUSB.USB_CONTROL_MSG_IN(FlashcatUSB.USB.USBREQ.EXPIO_INIT, ref result_data, (uint)((int)mode | 10 << 16));
                return result;
            } catch (Exception ex) {
            }

            return false;
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        private FlashcatUSB.FlashMemory.FlashDetectResult DetectFlash() {
            var result = new FlashcatUSB.FlashMemory.FlashDetectResult();
            result.Successful = false;
            try {
                var ident_data = new byte[8]; // Contains 8 bytes
                if (!FCUSB.USB_CONTROL_MSG_IN(FlashcatUSB.USB.USBREQ.EXPIO_RDID, ref ident_data))
                    return result;
                if (ident_data[0] == 0 && ident_data[2] == 0)
                    return result; // 0x0000
                if (ident_data[0] == 0x90 && ident_data[2] == 0x90)
                    return result; // 0x9090 
                if (ident_data[0] == 0x90 && ident_data[2] == 0)
                    return result; // 0x9000 
                if (ident_data[0] == 0xFF && ident_data[2] == 0xFF)
                    return result; // 0xFFFF 
                if (ident_data[0] == 0xFF && ident_data[2] == 0)
                    return result; // 0xFF00
                if (ident_data[0] == 0x1 && ident_data[1] == 0 && ident_data[2] == 0x1 && ident_data[3] == 0)
                    return result; // 0x01000100
                result.MFG = ident_data[0];
                result.ID1 = (ushort)((uint)ident_data[1] << 8 | ident_data[2]);
                result.ID2 = (ushort)((uint)ident_data[3] << 8 | ident_data[4]);
                if ((int)result.ID1 == 0 && (int)result.ID2 == 0)
                    return result;
                result.Successful = true;
            } catch (Exception ex) {
            }

            return result;
        }

        private byte[] GetSetupPacket(uint Address, uint Count, ushort PageSize) {
            byte addr_bytes = 0;
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

        private byte[] ReadBulk_NOR(uint address, uint count) {
            try {
                uint read_count = count;
                bool addr_offset = false;
                bool count_offset = false;
                if (address % 2L == 1L) {
                    addr_offset = true;
                    address = (uint)(address - 1L);
                    read_count = (uint)(read_count + 1L);
                }

                if (read_count % 2L == 1L) {
                    count_offset = true;
                    read_count = (uint)(read_count + 1L);
                }

                var setup_data = GetSetupPacket(address, read_count, (ushort)MyFlashDevice.PAGE_SIZE);
                var data_out = new byte[(int)(read_count - 1L + 1)]; // Bytes we want to read
                bool result = FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.EXPIO_READDATA, setup_data, ref data_out, 0U);
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
            } catch (Exception ex) {
            }

            return null;
        }

        private bool WriteBulk_NOR(uint address, byte[] data_out) {
            try {
                var setup_data = GetSetupPacket(address, (uint)data_out.Length, (ushort)MyFlashDevice.PAGE_SIZE);
                bool result = FCUSB.USB_SETUP_BULKOUT(FlashcatUSB.USB.USBREQ.EXPIO_WRITEDATA, setup_data, data_out, 0U);
                return result;
            } catch (Exception ex) {
            }

            return false;
        }
    }
}