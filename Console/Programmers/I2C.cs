using FlashMemory;
using System;
using System.Collections.Generic;
using USB;

public class I2C_Programmer : MemoryDeviceUSB
{
    private FCUSB_DEVICE FCUSB;

    public event MemoryDeviceUSB.PrintConsoleEventHandler PrintConsole;
    public event MemoryDeviceUSB.SetProgressEventHandler SetProgress;

    public List<I2C_DEVICE> I2C_EEPROM_LIST;
    private I2C_DEVICE MyFlashDevice = null;

    public I2C_Programmer(FCUSB_DEVICE parent_if) {
        SetProgress?.Invoke(0);
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
        MyFlashDevice = I2C_EEPROM_LIST[ic2_eeprom_index];
    }

    public void SelectDeviceIndex(string ic2_eeprom_name) {
        MyFlashDevice = null;
        foreach (var device in I2C_EEPROM_LIST) {
            if (device.NAME.ToUpper().Equals(ic2_eeprom_name.ToUpper())) {
                MyFlashDevice = device;
                return;
            }
        }
    }

    public bool DeviceInit() {
        if (MyFlashDevice is null)
            return false;
        ushort cd_value = (ushort)((int)MainApp.MySettings.I2C_SPEED << 8 | MainApp.MySettings.I2C_ADDRESS); // 02A0
        ushort cd_index = (ushort)((ushort)MyFlashDevice.AddressSize << 8 | (ushort)MyFlashDevice.PAGE_SIZE); // addr size, page size   '0220
        uint config_data = (uint)cd_value << 16 | cd_index;
        bool detect_result = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.I2C_INIT, null, config_data);
        Utilities.Sleep(50); // Wait for IO VCC to charge up
        return detect_result;
    }

    public Device GetDevice {
        get {
            return this.MyFlashDevice;
        }
    }

    public string DeviceName {
        get {
            if (MyFlashDevice is null)
                return "";
            return MyFlashDevice.NAME;
        }
    }

    public long DeviceSize {
        get {
            if (MyFlashDevice is null)
                return 0L;
            return MyFlashDevice.FLASH_SIZE;
        }
    }

    public bool EraseDevice() {
        PrintConsole?.Invoke("I2C ERASE device not supported");
        return false;
    }

    public byte[] ReadData(long flash_offset, long data_count) {
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
            result = FCUSB.USB_SETUP_BULKIN(USBREQ.I2C_READEEPROM, setup_data, ref data_out, (uint)data_count);
            if (!result)
                return null;
            if (GetResultStatus() == I2C_STATUS.NOERROR)
                return data_out;
        } catch {
        }
        return null;
    }

    public uint SectorCount()
    {
        return 1U;
    }

    public bool SectorErase(uint SectorIndex)
    {
        return true;
    }

    public long SectorFind(uint SectorIndex)
    {
        return 0L;
    }

    public uint SectorSize(long sector) {
        return (uint)DeviceSize;
    }

    public bool SectorWrite(uint SectorIndex, byte[] data, WriteParameters Params) {
        return false; // Not supported
    }

    public void WaitUntilReady() {
        Utilities.Sleep(100);
    }

    public bool WriteData(long flash_offset, byte[] data_to_write, WriteParameters Params) {
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
            result = FCUSB.USB_SETUP_BULKOUT(USBREQ.I2C_WRITEEEPROM, setup_data, data_to_write, data_count);
            if (!result)
                return false;
            FCUSB.USB_WaitForComplete(); // It may take a few microseconds to complete
            if (GetResultStatus() == I2C_STATUS.NOERROR)
                return true;
        } catch {
        }
        return false;
    }

    public uint SectorSize(uint sector) {
        throw new NotImplementedException();
    }

    private I2C_STATUS GetResultStatus() {
        try {
            var packet_out = new byte[1];
            if (!FCUSB.USB_CONTROL_MSG_IN(USBREQ.I2C_RESULT, ref packet_out))
                return I2C_STATUS.USBFAIL;
            return (I2C_STATUS)packet_out[0];
        } catch {
            return I2C_STATUS.ERROR;
        }
    }

    private enum I2C_STATUS : byte {
        USBFAIL = 0,
        NOERROR = 0x50,
        ERROR = 0x51
    }

    internal bool WriteData(long address, byte[] packet_data) {
        throw new NotImplementedException();
    }

    internal bool IsConnected() {
        throw new NotImplementedException();
    }
}


