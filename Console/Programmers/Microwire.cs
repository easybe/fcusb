using USB;
using System;
using FlashMemory;

public class Microwire_Programmer : MemoryDeviceUSB {
    private FCUSB_DEVICE FCUSB;
    public event MemoryDeviceUSB.PrintConsoleEventHandler PrintConsole;
    public event MemoryDeviceUSB.SetProgressEventHandler SetProgress;

    private MICROWIRE MyFlashDevice;

    public Microwire_Programmer(FCUSB_DEVICE parent_if) {
        FCUSB = parent_if;
    }

    public bool DeviceInit() {
        SetProgress?.Invoke(0);
        var s93_devices = MainApp.FlashDatabase.GetFlashDevices(MemoryType.SERIAL_MICROWIRE);
        MyFlashDevice = null;
        uint org_mode = (uint)MainApp.MySettings.S93_DEVICE_ORG; // 0=8-bit,1=16-bit
        if (!MainApp.MySettings.S93_DEVICE.Equals("")) {
            foreach (var device in s93_devices) {
                if (device.NAME.ToUpper().Equals(MainApp.MySettings.S93_DEVICE.ToUpper())) {
                    MyFlashDevice = (MICROWIRE)device;
                    break;
                }
            }
        }
        if (MyFlashDevice is null) {
            PrintConsole?.Invoke("No Microwire device selected");
            return false;
        }
        uint addr_bits = 0U;
        string org_str;
        if (org_mode == 0L) { // 8-bit
            org_str = "X8";
            addr_bits = (uint)MyFlashDevice.X8_ADDRSIZE;
        } else { // 16-bit mode
            org_str = "X16";
            addr_bits = (uint)MyFlashDevice.X16_ADDRSIZE;
        }

        PrintConsole?.Invoke("Microwire device: " + DeviceName + " (" + MyFlashDevice.FLASH_SIZE + " bytes) " + org_str + " mode");
        uint setup_data = addr_bits << 8 | org_mode;
        bool result = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.S93_INIT, null, setup_data);
        return result;
    }

    public Device GetDevice {
        get {
            return this.MyFlashDevice;
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

    public uint SectorSize(uint sector) {
        return (uint)DeviceSize;
    }

    public byte[] ReadData(long flash_offset, long data_count) {
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
            result = FCUSB.USB_SETUP_BULKIN(USBREQ.S93_READEEPROM, setup_data, ref data_out, 0U);
            if (!result)
                return null;
            return data_out;
        } catch {
        }
        return null;
    }

    public bool WriteData(long flash_offset, byte[] data_to_write, WriteParameters Params = null) {
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
            result = FCUSB.USB_SETUP_BULKOUT(USBREQ.S93_WRITEEEPROM, setup_data, data_to_write, data_count);
            Utilities.Sleep(100);
            FCUSB.USB_WaitForComplete();
            if (result)
                ReadData(0L, 16L); // Some devices need us to read a page of data
            return result;
        } catch {
        }
        return false;
    }

    public bool EraseDevice() {
        bool result = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.S93_ERASE);
        return result;
    }

    public void WaitUntilReady() {
        Utilities.Sleep(10);
    }

    public long SectorFind(uint SectorIndex) {
        return 0L;
    }

    public bool SectorErase(uint SectorIndex) {
        return true;
    }

    public uint SectorCount() {
        return 1U;
    }

    public bool SectorWrite(uint SectorIndex, byte[] data, WriteParameters Params = null) {
        return WriteData(0L, data);
    }
}
