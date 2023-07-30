using System;

namespace FlashcatUSB {
    public class Microwire_Programmer : FlashcatUSB.MemoryDeviceUSB {
        private FlashcatUSB.USB.FCUSB_DEVICE FCUSB;
        private FlashcatUSB.FlashMemory.MICROWIRE MICROWIRE_DEVIE;

        public event PrintConsoleEventHandler PrintConsole;

        public delegate void PrintConsoleEventHandler(string message);

        public event SetProgressEventHandler SetProgress;

        public delegate void SetProgressEventHandler(int percent);

        public Microwire_Programmer(FlashcatUSB.USB.FCUSB_DEVICE parent_if) {
            FCUSB = parent_if;
        }

        public bool DeviceInit() {
            var s93_devices = FlashcatUSB.MainApp.FlashDatabase.GetFlashDevices(FlashcatUSB.FlashMemory.MemoryType.SERIAL_MICROWIRE);
            MICROWIRE_DEVIE = null;
            uint org_mode = (uint)FlashcatUSB.MainApp.MySettings.S93_DEVICE_ORG; // 0=8-bit,1=16-bit
            if (!FlashcatUSB.MainApp.MySettings.S93_DEVICE.Equals("")) {
                foreach (var device in s93_devices) {
                    if (device.NAME.ToUpper().Equals(FlashcatUSB.MainApp.MySettings.S93_DEVICE.ToUpper())) {
                        MICROWIRE_DEVIE = (FlashcatUSB.FlashMemory.MICROWIRE)device;
                        break;
                    }
                }
            }

            if (MICROWIRE_DEVIE is null) {
                PrintConsole?.Invoke("No Microwire device selected");
                FlashcatUSB.MainApp.SetStatus("No Microwire device selected");
                return false;
            }

            uint addr_bits = 0U;
            string org_str;
            if (org_mode == 0L) { // 8-bit
                org_str = "X8";
                addr_bits = (uint)MICROWIRE_DEVIE.X8_ADDRSIZE;
            } else { // 16-bit mode
                org_str = "X16";
                addr_bits = (uint)MICROWIRE_DEVIE.X16_ADDRSIZE;
            }

            PrintConsole?.Invoke("Microwire device: " + DeviceName + " (" + MICROWIRE_DEVIE.FLASH_SIZE + " bytes) " + org_str + " mode");
            uint setup_data = addr_bits << 8 | org_mode;
            bool result = FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.S93_INIT, null, setup_data);
            return result;
        }

        public string DeviceName {
            get {
                return MICROWIRE_DEVIE.NAME;
            }
        }

        public long DeviceSize {
            get {
                return MICROWIRE_DEVIE.FLASH_SIZE;
            }
        }

        public uint SectorSize(uint sector, FlashcatUSB.FlashMemory.FlashArea area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            return (uint)DeviceSize;
        }

        public byte[] ReadData(long flash_offset, long data_count, FlashcatUSB.FlashMemory.FlashArea area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            try {
                var setup_data = new byte[8];
                bool result;
                setup_data[0] = (byte)(data_count >> 24 & 255L);
                setup_data[1] = (byte)(data_count >> 16 & 255L);
                setup_data[2] = (byte)(data_count >> 8 & 255L);
                setup_data[3] = (byte)(data_count & 255L);
                setup_data[4] = (byte)(flash_offset >> 24 & 255L);
                setup_data[5] = (byte)(flash_offset >> 16 & 255L);
                setup_data[6] = (byte)(flash_offset >> 8 & 255L);
                setup_data[7] = (byte)(flash_offset & 255L);
                var data_out = new byte[(int)(data_count - 1L + 1)];
                result = FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.S93_READEEPROM, setup_data, ref data_out, 0U);
                if (!result)
                    return null;
                return data_out;
            } catch (Exception ex) {
            }

            return null;
        }

        public bool WriteData(long flash_offset, byte[] data_to_write, FlashcatUSB.WriteParameters Params = null) {
            try {
                uint data_count = (uint)data_to_write.Length;
                var setup_data = new byte[8];
                bool result;
                setup_data[0] = (byte)(data_count >> 24 & 255L);
                setup_data[1] = (byte)(data_count >> 16 & 255L);
                setup_data[2] = (byte)(data_count >> 8 & 255L);
                setup_data[3] = (byte)(data_count & 255L);
                setup_data[4] = (byte)(flash_offset >> 24 & 255L);
                setup_data[5] = (byte)(flash_offset >> 16 & 255L);
                setup_data[6] = (byte)(flash_offset >> 8 & 255L);
                setup_data[7] = (byte)(flash_offset & 255L);
                result = FCUSB.USB_SETUP_BULKOUT(FlashcatUSB.USB.USBREQ.S93_WRITEEEPROM, setup_data, data_to_write, data_count);
                FlashcatUSB.Utilities.Main.Sleep(100);
                FCUSB.USB_WaitForComplete();
                if (result)
                    ReadData(0L, 16L); // Some devices need us to read a page of data
                return result;
            } catch (Exception ex) {
            }

            return false;
        }

        public bool EraseDevice() {
            bool result = FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.S93_ERASE);
            return result;
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
            return WriteData(0L, data);
        }
    }
}