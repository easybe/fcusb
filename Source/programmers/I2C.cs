using System;
using System.Collections.Generic;

namespace FlashcatUSB {
    public class I2C_Programmer : FlashcatUSB.MemoryDeviceUSB {
        private FlashcatUSB.USB.FCUSB_DEVICE FCUSB;

        public event PrintConsoleEventHandler PrintConsole;

        public delegate void PrintConsoleEventHandler(string message);

        public event SetProgressEventHandler SetProgress;

        public delegate void SetProgressEventHandler(int percent);

        public List<I2C_DEVICE> I2C_EEPROM_LIST;
        private I2C_DEVICE I2C_EEPROM_SELECTED = null;

        public I2C_Programmer(FlashcatUSB.USB.FCUSB_DEVICE parent_if) {
            FCUSB = parent_if;
            I2C_EEPROM_LIST = new List<I2C_DEVICE>();
            I2C_EEPROM_LIST.Add(new I2C_DEVICE("24XX01", 128U, 1, 8));
            I2C_EEPROM_LIST.Add(new I2C_DEVICE("24XX02", 256U, 1, 8));
            I2C_EEPROM_LIST.Add(new I2C_DEVICE("24XX03", 256U, 1, 16));
            I2C_EEPROM_LIST.Add(new I2C_DEVICE("24XX04", 512U, 1, 16));
            I2C_EEPROM_LIST.Add(new I2C_DEVICE("24XX05", 512U, 1, 32));
            I2C_EEPROM_LIST.Add(new I2C_DEVICE("24XX08", 1024U, 1, 16));
            I2C_EEPROM_LIST.Add(new I2C_DEVICE("24XX16", 2048U, 1, 16));
            I2C_EEPROM_LIST.Add(new I2C_DEVICE("24XX32", 4096U, 2, 32));
            I2C_EEPROM_LIST.Add(new I2C_DEVICE("24XX64", 8192U, 2, 32));
            I2C_EEPROM_LIST.Add(new I2C_DEVICE("24XX128", 16384U, 2, 32));
            I2C_EEPROM_LIST.Add(new I2C_DEVICE("24XX256", 32768U, 2, 32));
            I2C_EEPROM_LIST.Add(new I2C_DEVICE("24XX256", 65536U, 2, 32));
            I2C_EEPROM_LIST.Add(new I2C_DEVICE("24XXM01", 131072U, 2, 32));
            I2C_EEPROM_LIST.Add(new I2C_DEVICE("24XXM02", 262144U, 2, 32));
        }

        public void SelectDeviceIndex(int ic2_eeprom_index) {
            I2C_EEPROM_SELECTED = I2C_EEPROM_LIST[ic2_eeprom_index];
        }

        public void SelectDeviceIndex(string ic2_eeprom_name) {
            I2C_EEPROM_SELECTED = null;
            foreach (var device in I2C_EEPROM_LIST) {
                if (device.Name.ToUpper().Equals(ic2_eeprom_name.ToUpper())) {
                    I2C_EEPROM_SELECTED = device;
                    return;
                }
            }
        }

        public bool DeviceInit() {
            if (I2C_EEPROM_SELECTED is null)
                return false;
            ushort cd_value = (ushort)FlashcatUSB.MainApp.MySettings.I2C_SPEED << 8 | (ushort)FlashcatUSB.MainApp.MySettings.I2C_ADDRESS; // 02A0
            ushort cd_index = (ushort)((ushort)I2C_EEPROM_SELECTED.AddressSize << 8 | I2C_EEPROM_SELECTED.PageSize); // addr size, page size   '0220
            uint config_data = (uint)cd_value << 16 | cd_index;
            bool detect_result = FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.I2C_INIT, null, config_data);
            FlashcatUSB.Utilities.Main.Sleep(50); // Wait for IO VCC to charge up
            return detect_result;
        }

        public class I2C_DEVICE {
            public string Name { get; private set; }
            public uint Size { get; private set; } // Number of bytes in this Flash device
            public int AddressSize { get; private set; }
            public int PageSize { get; private set; }

            public I2C_DEVICE(string DisplayName, uint SizeInBytes, int EEAddrSize, int EEPageSize) {
                Name = DisplayName;
                Size = SizeInBytes;
                AddressSize = EEAddrSize; // Number of bytes that are used to store the address
                PageSize = EEPageSize;
            }
        }

        public string DeviceName {
            get {
                if (I2C_EEPROM_SELECTED is null)
                    return "";
                return I2C_EEPROM_SELECTED.Name;
            }
        }

        public long DeviceSize {
            get {
                if (I2C_EEPROM_SELECTED is null)
                    return 0L;
                return I2C_EEPROM_SELECTED.Size;
            }
        }

        public uint SectorSize(uint sector, FlashcatUSB.FlashMemory.FlashArea area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            return (uint)DeviceSize;
        }

        public bool IsConnected() {
            try {
                var test_data = ReadData(0L, 16L); // This does a test read to see if data is read
                if (test_data is null)
                    return false;
                return true;
            } catch (Exception ex) {
            }

            return false;
        }

        private I2C_STATUS GetResultStatus() {
            try {
                var packet_out = new byte[1];
                if (!FCUSB.USB_CONTROL_MSG_IN(FlashcatUSB.USB.USBREQ.I2C_RESULT, ref packet_out))
                    return I2C_STATUS.USBFAIL;
                return (I2C_STATUS)packet_out[0];
            } catch (Exception ex) {
                return I2C_STATUS.ERROR;
            }
        }

        private enum I2C_STATUS : byte {
            USBFAIL = 0,
            NOERROR = 0x50,
            ERROR = 0x51
        }

        public byte[] ReadData(long flash_offset, long data_count, FlashcatUSB.FlashMemory.FlashArea area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            try {
                var setup_data = new byte[7];
                bool result = false;
                setup_data[0] = (byte)(flash_offset >> 24 & 255L);
                setup_data[1] = (byte)(flash_offset >> 16 & 255L);
                setup_data[2] = (byte)(flash_offset >> 8 & 255L);
                setup_data[3] = (byte)(flash_offset & 255L);
                setup_data[4] = (byte)(data_count >> 16 & 255L);
                setup_data[5] = (byte)(data_count >> 8 & 255L);
                setup_data[6] = (byte)(data_count & 255L);
                var data_out = new byte[(int)(data_count - 1L + 1)];
                result = FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.I2C_READEEPROM, setup_data, ref data_out, (uint)data_count);
                if (!result)
                    return null;
                if (GetResultStatus() == I2C_STATUS.NOERROR)
                    return data_out;
            } catch (Exception ex) {
            }
            return null;
        }

        public bool WriteData(long flash_offset, byte[] data_to_write, FlashcatUSB.WriteParameters Params = null) {
            try {
                var setup_data = new byte[7];
                uint data_count = (uint)data_to_write.Length;
                bool result = false;
                setup_data[0] = (byte)(flash_offset >> 24 & 255L);
                setup_data[1] = (byte)(flash_offset >> 16 & 255L);
                setup_data[2] = (byte)(flash_offset >> 8 & 255L);
                setup_data[3] = (byte)(flash_offset & 255L);
                setup_data[4] = (byte)(data_count >> 16 & 255L);
                setup_data[5] = (byte)(data_count >> 8 & 255L);
                setup_data[6] = (byte)(data_count & 255L);
                result = FCUSB.USB_SETUP_BULKOUT(FlashcatUSB.USB.USBREQ.I2C_WRITEEEPROM, setup_data, data_to_write, data_count);
                if (!result)
                    return false;
                FCUSB.USB_WaitForComplete(); // It may take a few microseconds to complete
                if (GetResultStatus() == I2C_STATUS.NOERROR)
                    return true;
            } catch (Exception ex) {
            }

            return false;
        }

        public bool EraseDevice() {
            return true; // EEPROM does not support erase commands
        }

        public void WaitUntilReady() {
            FlashcatUSB.Utilities.Main.Sleep(100);
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
    }
}