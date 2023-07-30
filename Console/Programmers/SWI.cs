using USB;
using System;
using FlashMemory;

public class SWI_Programmer : MemoryDeviceUSB {
    private FCUSB_DEVICE FCUSB;
    public event MemoryDeviceUSB.PrintConsoleEventHandler PrintConsole;
    public event MemoryDeviceUSB.SetProgressEventHandler SetProgress;

    private SWI MyFlashDevice;

    public SWI_Programmer(FCUSB_DEVICE parent_if) {
        SetProgress?.Invoke(0);
        FCUSB = parent_if;
    }

    public bool DeviceInit() {
        var chip_id = new byte[3];
        bool detect_result = FCUSB.USB_CONTROL_MSG_IN(USBREQ.SWI_DETECT, ref chip_id, (uint)MainApp.MySettings.SWI_ADDRESS);
        uint SWI_ID_DATA = (uint)(chip_id[0] << 16 | chip_id[1] << 8 | chip_id[2]);
        byte MFG_CODE = (byte)(SWI_ID_DATA >> 12 & 0xFL);
        ushort PART = (ushort)(SWI_ID_DATA >> 3 & 0x1FFL);
        if (MFG_CODE == 0xD)
            return false;
        if (PART == 0x40) {
            MyFlashDevice = new SWI("Microchip AT21CS01", 0xD, 0x40, 128, 8);
        } else if (PART == 0x70) {
            MyFlashDevice = new SWI("Microchip AT21CS11", 0xD, 0x70, 128, 8);
        } else {
            return false;
        }
        return true;
    }

    public Device GetDevice {
        get {
            return this.MyFlashDevice;
        }
    }

    public string DeviceName {
        get {
            return this.MyFlashDevice.NAME;
        }
    }

    public long DeviceSize {
        get {
            return this.MyFlashDevice.FLASH_SIZE;
        }
    }

    public uint SectorSize(uint sector) {
        return (uint)DeviceSize;
    }

    public byte[] ReadData(long flash_offset, long data_count) {
        var setup_data = GetSetupPacket((uint)flash_offset, (uint)data_count, (ushort)this.MyFlashDevice.PAGE_SIZE);
        var data_out = new byte[(int)(data_count - 1L + 1)];
        bool result = FCUSB.USB_SETUP_BULKIN(USBREQ.SWI_READ, setup_data, ref data_out, 0U);
        if (!result)
            return null;
        return data_out;
    }

    public bool WriteData(long flash_offset, byte[] data_to_write, WriteParameters Params = null) {
        var setup_data = GetSetupPacket((uint)flash_offset, (uint)data_to_write.Length, (ushort)this.MyFlashDevice.PAGE_SIZE);
        bool result = FCUSB.USB_SETUP_BULKOUT(USBREQ.SWI_WRITE, setup_data, data_to_write, 0U);
        return result;
    }

    public bool EraseDevice() {
        return true; // EEPROM does not support erase commands
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
        return false; // Not supported
    }

    private byte[] GetSetupPacket(uint Address, uint Count, ushort PageSize) {
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