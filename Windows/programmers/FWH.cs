// COPYRIGHT EMBEDDED COMPUTERS LLC 2020 - ALL RIGHTS RESERVED
// THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB PRODUCTS
// CONTACT EMAIL: support@embeddedcomputers.net
// ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
// INFO: This object interfaces FCUSB with a Firmware Hub memory device using LPC protocol

using System;
using System.Linq;
using Microsoft.VisualBasic;

namespace FlashcatUSB {
    public class FWH_Programmer : FlashcatUSB.MemoryDeviceUSB {
        private FlashcatUSB.USB.FCUSB_DEVICE FCUSB;

        public FlashcatUSB.FlashMemory.FWH MyFlashDevice { get; set; }
        public FlashcatUSB.USB.DeviceStatus MyFlashStatus { get; set; } = FlashcatUSB.USB.DeviceStatus.NotDetected;

        private FlashcatUSB.FlashMemory.FlashDetectResult FLASH_IDENT;

        public event PrintConsoleEventHandler PrintConsole;

        public delegate void PrintConsoleEventHandler(string message);

        public event SetProgressEventHandler SetProgress;

        public delegate void SetProgressEventHandler(int percent);

        public FWH_Programmer(FlashcatUSB.USB.FCUSB_DEVICE parent_if) {
            FCUSB = parent_if;
        }

        public bool DeviceInit() {
            MyFlashStatus = FlashcatUSB.USB.DeviceStatus.NotDetected;
            PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("ext_device_interface") + ": NOR (FWH)");
            var ident_data = new byte[8];
            var result_data = new byte[1];
            if (!FCUSB.USB_CONTROL_MSG_IN(FlashcatUSB.USB.USBREQ.EXPIO_INIT, ref result_data, (uint)FlashcatUSB.MEM_PROTOCOL.FWH))
                return false;
            if (!(result_data[0] == 0x17))
                return false;
            FCUSB.USB_CONTROL_MSG_IN(FlashcatUSB.USB.USBREQ.EXPIO_RDID, ref ident_data);
            FLASH_IDENT = FlashcatUSB.FlashMemory.Tools.GetFlashResult(ident_data);
            if (!FLASH_IDENT.Successful)
                return false;
            uint part = (uint)FLASH_IDENT.ID1 << 16 | (uint)FLASH_IDENT.ID2;
            string chip_id_str = Conversion.Hex(FLASH_IDENT.MFG).PadLeft(2, '0') + Conversion.Hex(part).PadLeft(8, '0');
            PrintConsole?.Invoke("Mode FWH returned ident code: 0x" + chip_id_str);
            var DevList = FlashcatUSB.MainApp.FlashDatabase.FindDevices(FLASH_IDENT.MFG, FLASH_IDENT.ID1, 0, FlashcatUSB.FlashMemory.MemoryType.FWH_NOR);
            if (DevList.Count() == 1) {
                PrintConsole?.Invoke(string.Format(FlashcatUSB.MainApp.RM.GetString("ext_device_detected"), "FWH"));
                MyFlashDevice = (FlashcatUSB.FlashMemory.FWH)DevList[0];
                MyFlashStatus = FlashcatUSB.USB.DeviceStatus.Supported;
                return true;
            } else {
                PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("unknown_device_email"));
                MyFlashDevice = null;
                MyFlashStatus = FlashcatUSB.USB.DeviceStatus.NotSupported;
                return false;
            }
        }

        public string DeviceName {
            get {
                return MyFlashDevice.NAME;
            }
        }

        public long DeviceSize {
            get {
                return MyFlashDevice.FLASH_SIZE;
            }
        }

        public uint SectorSize(uint sector, FlashcatUSB.FlashMemory.FlashArea area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            if (MyFlashDevice is null)
                return 0U;
            return MyFlashDevice.SECTOR_SIZE;
        }

        public void WaitUntilReady() {
            FlashcatUSB.Utilities.Main.Sleep(100);
        }

        public byte[] ReadData(long flash_offset, long data_count, FlashcatUSB.FlashMemory.FlashArea area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            var data_out = new byte[(int)(data_count - 1L + 1)];
            int ptr = 0;
            int bytes_left = (int)data_count;
            uint PacketSize = 2048U;
            while (bytes_left > 0) {
                int BufferSize = bytes_left;
                if (BufferSize > PacketSize)
                    BufferSize = (int)PacketSize;
                var data = ReadBulk_NOR((uint)flash_offset, (uint)BufferSize);
                if (data is null)
                    return null;
                Array.Copy(data, 0, data_out, ptr, BufferSize);
                flash_offset += data.Length;
                bytes_left -= data.Length;
                ptr += data.Length;
            }

            return data_out;
        }

        public bool WriteData(long flash_offset, byte[] data_to_write, FlashcatUSB.WriteParameters Params = null) {
            try {
                uint PacketSize = 4096U;
                uint BytesWritten = 0U;
                uint DataToWrite = (uint)data_to_write.Length;
                int Loops = (int)Math.Ceiling(DataToWrite / (double)PacketSize); // Calcuates iterations
                for (int i = 0, loopTo = Loops - 1; i <= loopTo; i++) {
                    if (Params is object) {
                        if (Params.AbortOperation)
                            return false;
                    }

                    int BufferSize = (int)DataToWrite;
                    if (BufferSize > PacketSize)
                        BufferSize = (int)PacketSize;
                    var data = new byte[BufferSize];
                    Array.Copy(data_to_write, i * PacketSize, data, 0L, data.Length);
                    bool ReturnValue = WriteBulk_NOR((uint)flash_offset, data);
                    if (!ReturnValue)
                        return false;
                    FCUSB.USB_WaitForComplete();
                    flash_offset += data.Length;
                    DataToWrite = (uint)(DataToWrite - data.Length);
                    BytesWritten = (uint)(BytesWritten + data.Length);
                    if (Params is object && Loops > 1) {
                        uint UpdatedTotal = (uint)(Params.BytesWritten + BytesWritten);
                        float percent = (float)UpdatedTotal / (float)Params.BytesTotal * 100f;
                        if (Params.Status.UpdateSpeed is object) {
                            string speed_str = Strings.Format((object)Math.Round((double)UpdatedTotal / ((double)Params.Timer.ElapsedMilliseconds / 1000d)), "#,###") + " B/s";
                            Params.Status.UpdateSpeed.DynamicInvoke(speed_str);
                        }

                        if (Params.Status.UpdatePercent is object)
                            Params.Status.UpdatePercent.DynamicInvoke((int)percent);
                    }
                }
            } catch (Exception ex) {
            }

            return true;
        }

        public bool EraseDevice() {
            SetProgress?.Invoke(0);
            int sec_count = (int)SectorCount();
            try {
                for (int i = 0, loopTo = sec_count - 1; i <= loopTo; i++) {
                    if (!SectorErase((uint)i))
                        return false;
                    SetProgress?.Invoke((int)((i + 1) / (double)sec_count * 100d));
                }
            } catch (Exception ex) {
                return false;
            } finally {
                SetProgress?.Invoke(0);
            }

            return true;
        }

        public long SectorFind(uint SectorIndex, FlashcatUSB.FlashMemory.FlashArea area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            uint base_addr = 0U;
            if (SectorIndex > 0L) {
                for (uint i = 0U, loopTo = (uint)(SectorIndex - 1L); i <= loopTo; i++)
                    base_addr += SectorSize(i, area);
            }

            return base_addr;
        }

        public bool SectorErase(uint SectorIndex, FlashcatUSB.FlashMemory.FlashArea area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            bool Result = false;
            uint Logical_Address = 0U;
            if (SectorIndex > 0L) {
                for (uint i = 0U, loopTo = (uint)(SectorIndex - 1L); i <= loopTo; i++) {
                    uint s_size = SectorSize(i);
                    Logical_Address += s_size;
                }
            }

            uint erase_setup = (uint)MyFlashDevice.ERASE_CMD << 24 | Logical_Address;
            Result = FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.EXPIO_SECTORERASE, null, erase_setup);
            if (!Result)
                return false;
            FlashcatUSB.Utilities.Main.Sleep(50);
            return BlankCheck(Logical_Address);
        }

        public uint SectorCount() {
            return MyFlashDevice.Sector_Count;
        }

        public bool SectorWrite(uint SectorIndex, byte[] data, FlashcatUSB.WriteParameters Params = null) {
            uint Addr32 = (uint)this.SectorFind(SectorIndex, Params.Memory_Area);
            return WriteData(Addr32, data, Params);
        }

        private bool BlankCheck(uint base_addr) {
            try {
                bool IsBlank = false;
                int Counter = 0;
                while (!IsBlank) {
                    FlashcatUSB.Utilities.Main.Sleep(10);
                    var w = this.ReadData((long)base_addr, 4L, FlashcatUSB.FlashMemory.FlashArea.Main);
                    if (w is null)
                        return false;
                    if (w[0] == 255 && w[1] == 255 && w[2] == 255 && w[3] == 255)
                        IsBlank = true;
                    Counter += 1;
                    if (Counter == 50)
                        return false; // Timeout (500 ms)
                }

                return true;
            } catch (Exception ex) {
                return false;
            }
        }

        private byte[] ReadBulk_NOR(uint address, uint count) {
            try {
                uint read_count = count;
                bool addr_offset = false;
                bool count_offset = false;
                var data_out = new byte[(int)(read_count - 1L + 1)]; // Bytes we want to read
                int page_size = 512;
                if (MyFlashDevice is object)
                    page_size = (int)MyFlashDevice.PAGE_SIZE;
                var setup_data = GetSetupPacket(address, read_count, (ushort)page_size);
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
    }
}