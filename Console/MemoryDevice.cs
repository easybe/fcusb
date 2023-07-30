using System;

public interface MemoryDeviceUSB {

    public event PrintConsoleEventHandler PrintConsole;
    delegate void PrintConsoleEventHandler(string message);

    public event SetProgressEventHandler SetProgress;
    delegate void SetProgressEventHandler(int percent);

    public bool DeviceInit();

    public FlashMemory.Device GetDevice { get; }

    public string DeviceName { get; }

    public long DeviceSize { get; }

    public UInt32 SectorSize(uint sector);

    public byte[] ReadData(long flash_offset, long data_count);

    public bool WriteData(long flash_offset, byte[] data_to_write, WriteParameters Params);

    public bool EraseDevice();
    public void WaitUntilReady();
    public long SectorFind(UInt32 SectorIndex);
    public bool SectorErase(UInt32 SectorIndex);
    public UInt32 SectorCount();
    public bool SectorWrite(UInt32 SectorIndex, byte[] data, WriteParameters Params);

}
