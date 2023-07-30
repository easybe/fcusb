
namespace FlashcatUSB {
    public class SWI_Programmer : FlashcatUSB.MemoryDeviceUSB {
        private FlashcatUSB.USB.FCUSB_DEVICE FCUSB;

        public event PrintConsoleEventHandler PrintConsole;

        public delegate void PrintConsoleEventHandler(string message);

        public event SetProgressEventHandler SetProgress;

        public delegate void SetProgressEventHandler(int percent);

        private string SWI_DEV_NAME;
        private int SWI_DEV_SIZE; // Total bytes of the device 
        private ushort SWI_DEV_PAGE;

        public SWI_Programmer(FlashcatUSB.USB.FCUSB_DEVICE parent_if) {
            FCUSB = parent_if;
        }

        public bool DeviceInit() {
            var chip_id = new byte[3];
            bool detect_result = FCUSB.USB_CONTROL_MSG_IN(FlashcatUSB.USB.USBREQ.SWI_DETECT, ref chip_id, (uint)FlashcatUSB.MainApp.MySettings.SWI_ADDRESS);
            uint SWI_ID_DATA = (uint)(chip_id[0] << 16 | chip_id[1] << 8 | chip_id[2]);
            switch (SWI_ID_DATA) {
                case 0xD200U: {
                        SWI_DEV_NAME = "Microchip AT21CS01";
                        SWI_DEV_SIZE = 128;
                        SWI_DEV_PAGE = 8;
                        break;
                    }

                case 0xD380U: {
                        SWI_DEV_NAME = "Microchip AT21CS11";
                        SWI_DEV_SIZE = 128;
                        SWI_DEV_PAGE = 8;
                        break;
                    }

                default: {
                        return false;
                    }
            }

            return true;
        }

        public string DeviceName {
            get {
                return SWI_DEV_NAME;
            }
        }

        public long DeviceSize {
            get {
                return SWI_DEV_SIZE;
            }
        }

        public uint SectorSize(uint sector, FlashcatUSB.FlashMemory.FlashArea area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            return (uint)DeviceSize;
        }

        public byte[] ReadData(long flash_offset, long data_count, FlashcatUSB.FlashMemory.FlashArea area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            var setup_data = GetSetupPacket((uint)flash_offset, (uint)data_count, SWI_DEV_PAGE);
            var data_out = new byte[(int)(data_count - 1L + 1)];
            bool result = FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.SWI_READ, setup_data, ref data_out, 0U);
            if (!result)
                return null;
            return data_out;
        }

        public bool WriteData(long flash_offset, byte[] data_to_write, FlashcatUSB.WriteParameters Params = null) {
            var setup_data = GetSetupPacket((uint)flash_offset, (uint)data_to_write.Length, SWI_DEV_PAGE);
            bool result = FCUSB.USB_SETUP_BULKOUT(FlashcatUSB.USB.USBREQ.SWI_WRITE, setup_data, data_to_write, 0U);
            return result;
        }

        public bool EraseDevice() {
            return true; // EEPROM does not support erase commands
        }

        public void WaitUntilReady() {
            FlashcatUSB.Utilities.Main.Sleep(10);
        }

        public long SectorFind(uint SectorIndex, FlashcatUSB.FlashMemory.FlashArea area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            return 0L;
        }

        public bool SectorErase(uint SectorIndex, FlashcatUSB.FlashMemory.FlashArea area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            return true;
        }

        public uint SectorCount() {
            return 1U;
        }

        public bool SectorWrite(uint SectorIndex, byte[] data, FlashcatUSB.WriteParameters Params = null) {
            return false; // Not supported
        }

        private byte[] GetSetupPacket(uint Address, uint Count, ushort PageSize) {
            byte addr_bytes = 0;
            var data_in = new byte[11];
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
            data_in[10] = 1;
            return data_in;
        }
    }
}