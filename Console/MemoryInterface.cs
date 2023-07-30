using FlashMemory;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public partial class MemoryInterface
{
    private List<MemoryDeviceInstance> MyDevices = new List<MemoryDeviceInstance>();

    public MemoryInterface()
    {
    }

    public int DeviceCount
    {
        get
        {
            return MyDevices.Count;
        }
    }

    public void Clear()
    {
        MyDevices.Clear(); // Remove all devices
    }

    public MemoryDeviceInstance[] GetDevices(USB.FCUSB_DEVICE usb_dev)
    {
        try
        {
            var devices_on_this_usbport = new List<MemoryDeviceInstance>();
            foreach (var i in MyDevices)
            {
                if (object.ReferenceEquals(i.FCUSB, usb_dev))
                    devices_on_this_usbport.Add(i);
            }

            if (devices_on_this_usbport.Count == 0)
                return null;
            return devices_on_this_usbport.ToArray();
        }
        catch
        {
            return null;
        }
    }

    public MemoryDeviceInstance Add(USB.FCUSB_DEVICE usb_dev, Device device) {
        var memDev = new MemoryDeviceInstance(usb_dev, device);
        memDev.Name = device.NAME;
        memDev.Size = device.FLASH_SIZE;
        memDev.FlashType = device.FLASH_TYPE;
        MyDevices.Add(memDev);
        return memDev;
    }

    public void Remove(MemoryDeviceInstance device)
    {
        MyDevices.Remove(device);
    }

    public MemoryDeviceInstance GetDevice(uint index)
    {
        if (index >= MyDevices.Count)
            return null;
        return MyDevices[(int)index];
    }

    public partial class MemoryDeviceInstance {
        public USB.FCUSB_DEVICE FCUSB;
        public Device MEMDEVICE;
        public string Name { get; set; }
        public long Size { get; set; } // Number of bytes of the memory device
        public uint BaseAddress { get; set; } = 0U; // Only changes for JTAG devices
        public MemoryType FlashType { get; set; } = MemoryType.UNSPECIFIED;
        public bool ReadOnly { get; set; } = false; // Set to true to disable write/erase functions
        public uint PreferredBlockSize { get; set; } = 32768U;
        //public Control VendorMenu { get; set; } = default;
        public bool NoErrors { get; set; } = false; // Indicates there was a physical error
        private bool IsErasing { get; set; } = false;
        private bool IsBulkErasing { get; set; } = false;
        private bool IsReading { get; set; } = false;
        private bool IsWriting { get; set; } = false;
        public bool IsTaskRunning { get; set; } = false;
        public bool SkipBadBlocks { get; set; } = true;
        public int RetryWriteCount { get; set; } = 0;
        public bool IsBusy
        {
            get
            {
                if (IsErasing | IsReading | IsWriting | IsBulkErasing)
                    return true;
                return false;
            }
        }

        public event PrintConsoleEventHandler PrintConsole;

        public delegate void PrintConsoleEventHandler(string msg); // Prints text to the console

        public event SetStatusEventHandler SetStatus;

        public delegate void SetStatusEventHandler(string msg); // Sets the status of the main gui

        private object InterfaceLock = new object();

        public MemoryDeviceInstance(USB.FCUSB_DEVICE usb_interface, Device flash_device) {
            //GuiControl = new MemControl_v2(this);
            this.FCUSB = usb_interface;
            this.MEMDEVICE = flash_device;
        }

        public partial class StatusCallback {
            public Delegate UpdateOperation; // (Int) 1=Read,2=Write,3=Verify,4=Erasing,5=Error
            public Delegate UpdateBase; // (Uint32) Updates the base address we are erasing/reading/writing
            public Delegate UpdateTask; // (String) Contains the task we are doing
            public Delegate UpdateSpeed; // (String) This is used to update a speed text label
            public Delegate UpdatePercent; // (Integer) This is the percent complete
        }

        internal void DisableGuiControls(bool show_cancel = false) {
            throw new NotImplementedException();
            //if (GuiControl is object)
            //GuiControl.DisableControls(show_cancel);
        }

        internal void EnableGuiControls() {
            throw new NotImplementedException();
            //if (GuiControl is object)
            //GuiControl.EnableControls();
        }

        internal void RefreshControls() {
            throw new NotImplementedException();
            //if (GuiControl is object)
            //GuiControl.RefreshView();
        }

        private void OnWriteConsole(string msg_out)
        {
            PrintConsole?.Invoke(msg_out);
        }

        private void OnSetStatus(string status_text)
        {
            SetStatus?.Invoke(status_text);
        }

        private void OnEraseDataRequest()
        {
            try
            {
                EraseFlash();
                WaitUntilReady();
            }
            catch
            {
            }
        }

        private void OnReadDataRequest(long base_addr, ref byte[] data)
        {
            try
            {
                if (IsBulkErasing)
                {
                    for (int i = 0, loopTo = data.Length - 1; i <= loopTo; i++)
                        data[i] = 255;
                    return;
                }

                data = ReadBytes(base_addr, data.Length);
            }
            catch
            {
            }
        }

        private void OnReadStreamRequest(Stream data_stream, ReadParameters f_params)
        {
            try
            {
                if (IsBulkErasing)
                {
                    for (long i = 0, loopTo = f_params.Count - 1; i <= loopTo; i++)
                        data_stream.WriteByte(255);
                    return;
                }

                ReadStream(data_stream, f_params);
            }
            catch
            {
            }
        }

        private void OnWriteRequest(long addr, byte[] data, bool verify_wr, ref bool Success)
        {
            try
            {
                Success = WriteBytes(addr, data, verify_wr);
            }
            catch
            {
            }
        }

        private void OnWriteStreamRequest(Stream data_stream, WriteParameters f_params, ref bool Success)
        {
            try
            {
                Success = WriteStream(data_stream, f_params);
                WaitUntilReady();
            }
            catch
            {
            }
        }

        private void OnGetSectorSize(uint sector_int, ref uint sector_size)
        {
            sector_size = GetSectorSize(sector_int);
            if (sector_size == 0L)
                sector_size = (uint)Size;
        }

        private void OnGetSectorCount(ref uint count)
        {
            count = GetSectorCount();
            if (count == 0L)
                count = 1U;
        }

        private void OnGetSectorIndex(long addr, ref uint sector_int) {
            sector_int = 0U;
            uint s_count = GetSectorCount();
            for (uint i = 0, loopTo = s_count - 1; i <= loopTo; i++)
            {
                var sector = GetSectorInfo(i);
                if (addr >= sector.BaseAddress && addr < sector.BaseAddress + sector.Size)
                {
                    sector_int = (uint)i;
                    return;
                }
            }
        }

        private void OnGetSectorAddress(uint sector_int, ref long addr) {
            addr = GetSectorBaseAddress(sector_int);
        }

        private bool WaitForNotBusy() {
            int i = 0;
            while (IsBusy) {
                Thread.Sleep(5);
                i += 1;
                if (i == 1000)
                    return false; // 10 second timeout
            }
            return true;
        }

        public byte[] ReadBytes(long base_addr, long count, StatusCallback callback = null) {
            NoErrors = true;
            byte[] data_out = null;
            using (var n = new MemoryStream())
            {
                var f_params = new ReadParameters();
                f_params.Address = base_addr;
                f_params.Count = count;
                if (callback is object)
                {
                    f_params.Status = callback;
                }

                if (ReadStream(n, f_params))
                {
                    data_out = n.GetBuffer();
                    Array.Resize(ref data_out, (int)(n.Length - 1L + 1));
                }
            }

            return data_out;
        }

        public bool WriteBytes(long mem_addr, byte[] mem_data, bool verify_wr, StatusCallback callback = null) {
            try
            {
                NoErrors = true;
                var f_params = new WriteParameters();
                f_params.Address = mem_addr;
                f_params.BytesLeft = mem_data.Length;
                f_params.Verify = verify_wr;
                if (callback is object)
                {
                    f_params.Status = callback;
                }

                using (var n = new MemoryStream(mem_data))
                {
                    return WriteStream(n, f_params);
                }
            }
            catch
            {
                return false;
            }
        }

        public bool ReadStream(Stream data_stream, ReadParameters Params) {
            NoErrors = true;
            if (Thread.CurrentThread.Name is null) {
                int td_int = Thread.CurrentThread.ManagedThreadId;
                Thread.CurrentThread.Name = "MemIf.ReadStream_" + td_int;
            }
            try {
                long BytesTransfered = 0L;
                uint BlockSize = PreferredBlockSize;
                int Loops = (int)Math.Ceiling((double)Params.Count / BlockSize); // Calcuates iterations
                byte[] read_buffer; // Temp Byte buffer
                long BytesRead = 0L; // Number of bytes read from the Flash device
                if (Params.Status.UpdateOperation is object) {
                    Params.Status.UpdateOperation.DynamicInvoke(1); // READ IMG
                }
                if (Params.Status.UpdateTask is object) {
                    var bytes_read_str = String.Format("{0:n0}", Params.Count);
                    string rd_label = string.Format("Reading memory of {0} bytes", bytes_read_str);
                    Params.Status.UpdateTask.DynamicInvoke(rd_label);
                }
                long BytesRemaining = Params.Count;
                for (int i = 1, loopTo = Loops; i <= loopTo; i++) {
                    long BytesCountToRead = BytesRemaining;
                    if (BytesCountToRead > BlockSize)
                        BytesCountToRead = BlockSize;
                    read_buffer = new byte[(int)(BytesCountToRead - 1L + 1)]; // Erase block data
                    long FlashAddress = Params.Address + BytesRead;
                    if (Params.Status.UpdateBase is object) {
                        Params.Status.UpdateBase.DynamicInvoke(FlashAddress);
                    }
                    if (Params.Status.UpdatePercent is object){
                        float percent_done = (float)(i / (double)Loops * 100d); // Calulate % done
                        Params.Status.UpdatePercent.DynamicInvoke((int)percent_done);
                    }
                    var packet_timer = new Stopwatch();
                    packet_timer.Start();
                    read_buffer = ReadFlash(FlashAddress, BytesCountToRead);
                    packet_timer.Stop();
                    if (Params.AbortOperation || !NoErrors || read_buffer is null)
                        return false;
                    BytesTransfered += BytesCountToRead;
                    data_stream.Write(read_buffer, 0, (int)BytesCountToRead);
                    BytesRead += BytesCountToRead; // Increment location address
                    BytesRemaining -= BytesCountToRead;
                    if (i == 1 || i == Loops || i % 4 == 0) {
                        try {
                            Thread.CurrentThread.Join(10); // Pump a message
                            if (Params.Status.UpdateSpeed is object) {
                                uint bytes_per_second = (uint)Math.Round(BytesCountToRead / (packet_timer.ElapsedMilliseconds / 1000d));
                                string speed_text = UpdateSpeed_GetText((int)bytes_per_second);
                                Params.Status.UpdateSpeed.DynamicInvoke(speed_text);
                            }
                            data_stream.Flush();
                        } catch {
                        }
                    }
                }
                return true;
            } catch {
                PrintConsole?.Invoke("Error in ReadStream");
            } finally {
                if (Params.Timer is object)
                    Params.Timer.Stop();
            }
            return false;
        }

        public bool WriteStream(Stream data_stream, WriteParameters @params) {
            try {
                IsTaskRunning = true;
                NoErrors = true;
                if (Thread.CurrentThread.Name is null)
                {
                    int td_int = Thread.CurrentThread.ManagedThreadId;
                    Thread.CurrentThread.Name = "MemIf.WriteBytes_" + td_int;
                }
                if (ReadOnly)
                    return false;
                try {
                    @params.Timer = new Stopwatch();
                    if (FlashType == MemoryType.SERIAL_I2C)
                    {
                        return WriteBytes_I2C(data_stream, @params);
                    }
                    else if (FlashType == MemoryType.OTP_EPROM)
                    {
                        return WriteBytes_EPROM(data_stream, @params);
                    }
                    else if (FlashType == MemoryType.SERIAL_SWI)
                    {
                        return WriteBytes_EPROM(data_stream, @params);
                    }
                    else // Non-Volatile memory
                    {
                        return WriteBytes_NonVolatile(data_stream, @params);
                    }
                }
                finally
                {
                    @params.Timer.Stop();
                    ReadMode();
                }
            }
            catch
            {
            }
            finally
            {
                IsTaskRunning = false;
            }

            return false;
        }

        private bool WriteBytes_EPROM(Stream data_stream, WriteParameters Params)
        {
            WaitUntilReady(); // Some flash devices requires us to wait before sending data
            int FailedAttempts = 0;
            bool ReadResult;
            uint BlockSize = 8192U;
            while (Params.BytesLeft > 0)
            {
                if (Params.AbortOperation)
                    return false;
                long PacketSize = Params.BytesLeft;
                if (PacketSize > BlockSize)
                    PacketSize = BlockSize;
                if (Params.Status.UpdateBase is object)
                    Params.Status.UpdateBase.DynamicInvoke(Params.Address);
                if (Params.Status.UpdateOperation is object)
                    Params.Status.UpdateOperation.DynamicInvoke(2); // WRITE IMG
                if (Params.Status.UpdateTask is object)
                {
                    var packet_size_str = String.Format("{0:n0}", PacketSize);
                    string wr_label = string.Format("Writing memory with {0} bytes", packet_size_str);
                    Params.Status.UpdateTask.DynamicInvoke(wr_label);
                }
                var packet_data = new byte[PacketSize];
                data_stream.Read(packet_data, 0, (int)PacketSize); // Reads data from the stream
                Params.Timer.Start();
                bool write_result = FCUSB.PROGRAMMER.WriteData(Params.Address, packet_data, Params);
                Params.Timer.Stop();
                if (!write_result)
                    return false;
                if (Params.AbortOperation)
                    return false;
                if (!NoErrors)
                    return false;
                Thread.CurrentThread.Join(10); // Pump a message
                if (Params.Verify && FlashType == MemoryType.SERIAL_SWI) // Verify is enabled and we are monitoring this
                {
                    if (Params.Status.UpdateOperation is object)
                        Params.Status.UpdateOperation.DynamicInvoke(3); // VERIFY IMG
                    if (Params.Status.UpdateTask is object)
                    {
                        Params.Status.UpdateTask.DynamicInvoke("Verifying written data");
                    }
                    Thread.Sleep(50);
                    if (FlashType == MemoryType.OTP_EPROM)
                    {
                        FCUSB.EPROM_IF.ReadData(Params.Address, BlockSize); // Before we verify, we should read the entire block once
                    }

                    ReadResult = WriteBytes_VerifyWrite(Params.Address, packet_data);
                    if (ReadResult)
                    {
                        FailedAttempts = 0;
                        if (Params.Status.UpdateTask is object)
                        {
                            Params.Status.UpdateTask.DynamicInvoke("Data verification was successful");
                            Thread.Sleep(500);
                        }
                    }
                    else
                    {
                        if (FailedAttempts == this.RetryWriteCount)
                        {
                            PrintConsole?.Invoke(string.Format("Data verification failed at 0x{0}", Params.Address.ToString("X")));
                            if (Params.Status.UpdateOperation is object)
                            {
                                Params.Status.UpdateOperation.DynamicInvoke(5); // ERROR IMG
                            }

                            if (Params.Status.UpdateTask is object)
                            {
                                Params.Status.UpdateTask.DynamicInvoke("Data verification failed!");
                                Thread.Sleep(1000);
                            }
                            return false;
                        }
                        FailedAttempts += 1;
                        Thread.Sleep(500);
                    }
                }

                Params.BytesWritten += PacketSize;
                Params.BytesLeft -= PacketSize;
                Params.Address += PacketSize;
                float percent_done = Params.BytesWritten / Params.BytesTotal * 100f;
                if (Params.Status.UpdateSpeed is object)
                {
                    try
                    {
                        var s = ((double)Params.BytesWritten / (Params.Timer.ElapsedMilliseconds / 1000));
                        uint bytes_per_second = (uint)Math.Round(s);
                        string speed_text = UpdateSpeed_GetText((int)bytes_per_second);
                        Params.Status.UpdateSpeed.DynamicInvoke(speed_text);
                    }
                    catch
                    {
                    }
                }

                if (Params.Status.UpdatePercent is object)
                {
                    Params.Status.UpdatePercent.DynamicInvoke((int)percent_done);
                }
            }

            return true; // Operation was successful
        }

        private bool WriteBytes_NonVolatile(Stream data_stream, WriteParameters Params)
        {
            WaitUntilReady(); // Some flash devices requires us to wait before sending data
            uint TotalSectors = GetSectorCount();
            Params.BytesTotal = Params.BytesLeft; // Total size of the data we are writing
            float percent_done = 0f;
            for (uint i = 0U, loopTo = (uint)(TotalSectors - 1L); i <= loopTo; i++)
            {
                var sector = GetSectorInfo(i);
                long sector_start = sector.BaseAddress; // First byte of the sector
                long sector_end = (sector_start + sector.Size - 1L); // Last byte of the sector
                if (Params.Address >= sector_start & Params.Address <= sector_end) // This sector contains data we want to change
                {
                    var SectorData = new byte[(int)(sector.Size - 1L + 1)]; // The array that will contain the sector data to write
                    long SectorStart = Params.Address - sector.BaseAddress; // This is where in the sector we are going to fill from stream
                    long SectorEnd = Math.Min(sector.Size, SectorStart + Params.BytesLeft) - 1;  // This is where we will stop filling from stream
                    int StreamCount = (int)(SectorEnd - SectorStart + 1); // This is the number of bytes we are going to read for this sector
                    if (SectorStart > 0L) // We need to fill beginning
                    {
                        var data_segment = ReadFlash(sector.BaseAddress, SectorStart);
                        Array.Copy(data_segment, 0, SectorData, 0, data_segment.Length);
                        Params.Address = sector.BaseAddress; // This is to adjust the base address, as we are going to write data before our starting point
                    }

                    data_stream.Read(SectorData, (int)SectorStart, (int)StreamCount); // This reads data from our stream
                    if (SectorEnd < sector.Size - 1L) // We need to fill the end
                    {
                        uint BytesNeeded = (uint)(sector.Size - (SectorEnd + 1L));
                        WaitUntilReady();
                        var data_segment = ReadFlash(sector.BaseAddress + (long)SectorEnd + 1L, BytesNeeded);
                        Array.Copy(data_segment, 0L, SectorData, SectorEnd + 1L, data_segment.Length);
                    }

                    bool WriteResult = WriteBytes_EraseSectorAndWrite(ref i, SectorData,(int)Math.Floor((double)percent_done), Params); // Writes data
                    if (Params.AbortOperation)
                        return false;
                    if (!NoErrors)
                        return false;
                    Thread.CurrentThread.Join(10); // Pump a message
                    if (WriteResult)
                    {
                        Params.BytesWritten += SectorData.Length;
                        Params.BytesLeft -= StreamCount;
                        Params.Address = sector.BaseAddress + sector.Size;
                        percent_done = Params.BytesWritten / Params.BytesTotal * 100f;
                        if (Params.Status.UpdateSpeed is object)
                        {
                            var s = ((double)Params.BytesWritten / (Params.Timer.ElapsedMilliseconds / 1000));
                            uint bytes_per_second = (uint)Math.Round(s);
                            string speed_text = UpdateSpeed_GetText((int)bytes_per_second);
                            Params.Status.UpdateSpeed.DynamicInvoke(speed_text);
                        }

                        if (Params.Status.UpdatePercent is object)
                        {
                            Params.Status.UpdatePercent.DynamicInvoke((int)percent_done);
                        }
                    }
                    else if (FlashType == MemoryType.PARALLEL_NAND || FlashType == MemoryType.SERIAL_NAND)
                    {
                        if (this.SkipBadBlocks) // Bad block
                        {
                            if (i == TotalSectors - 1L)
                                return false; // No more blocks to write
                            data_stream.Position -= StreamCount; // We are going to re-write these bytes to the next block
                            Params.Address += SectorData.Length; // and to this base address
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }

                    if (Params.BytesLeft == 0)
                    {
                        data_stream.Dispose();
                        break;
                    }
                }
            }

            return true; // Operation was successful
        }

        private bool WriteBytes_I2C(Stream data_stream, WriteParameters Params)
        {
            try
            {
                long TotalSize = Params.BytesLeft;
                uint BytesPerPacket = PreferredBlockSize;
                float percent_done = 0f;
                uint BytesTransfered = 0U;
                while (Params.BytesLeft > 0)
                {
                    if (Params.AbortOperation)
                        return false;
                    Thread.CurrentThread.Join(10);
                    ushort PacketSize = (ushort)(Math.Min(Params.BytesLeft, (long)BytesPerPacket));
                    var packet_data = new byte[PacketSize];
                    data_stream.Read(packet_data, 0, PacketSize); // Reads data from the stream
                    if (Params.Status.UpdateTask is object)
                    {
                        var packet_size_str = String.Format("{0:n0}", PacketSize);
                        string wr_label = string.Format("Writing memory with {0} bytes", packet_size_str);
                        Params.Status.UpdateTask.DynamicInvoke(wr_label);
                    }
                    if (Params.Status.UpdateOperation is object)
                    {
                        Params.Status.UpdateOperation.DynamicInvoke(2); // WRITE IMG
                    }

                    if (Params.Status.UpdateBase is object)
                    {
                        Params.Status.UpdateBase.DynamicInvoke(Params.Address);
                    }
                    Params.Timer.Start();
                    bool i2c_result = FCUSB.I2C_IF.WriteData(Params.Address, packet_data);
                    Params.Timer.Stop();
                    if (!i2c_result)
                    {
                        PrintConsole?.Invoke("Error communicating with I2C device");
                        return false;
                    }

                    bool write_result = true;
                    if (Params.Verify)
                    {
                        if (Params.Status.UpdateTask is object)
                        {
                            Params.Status.UpdateTask.DynamicInvoke("Verifing written data at {0}");
                        }

                        if (Params.Status.UpdateOperation is object)
                        {
                            Params.Status.UpdateOperation.DynamicInvoke(3); // VERIFY IMG
                        }
                        write_result = WriteBytes_VerifyWrite(Params.Address, packet_data);
                        if (write_result)
                        {
                            if (Params.Status.UpdateTask is object)
                            {
                                Params.Status.UpdateTask.DynamicInvoke("Data verification was successful");
                                Thread.Sleep(500);
                            }
                        }
                        else // Write failed
                        {
                            PrintConsole?.Invoke(string.Format("Data verification failed at 0x{0}", Params.Address.ToString("X")));
                            if (Params.Status.UpdateOperation is object)
                            {
                                Params.Status.UpdateOperation.DynamicInvoke(5); // ERROR IMG
                            }

                            if (Params.Status.UpdateTask is object)
                            {
                                Params.Status.UpdateTask.DynamicInvoke("Data verification failed!");
                                Thread.Sleep(1000);
                            }
                        }
                    }

                    if (write_result)
                    {
                        BytesTransfered += PacketSize;
                        Params.BytesLeft -= PacketSize;
                        Params.Address += PacketSize;
                        percent_done = BytesTransfered / (float)TotalSize * 100f;
                        if (Params.Status.UpdateSpeed is object)
                        {
                            var s = ((double)BytesTransfered / (Params.Timer.ElapsedMilliseconds / 1000));
                            uint bytes_per_second = (uint)Math.Round(s);
                            string speed_text = UpdateSpeed_GetText((int)bytes_per_second);
                            Params.Status.UpdateSpeed.DynamicInvoke(speed_text);
                        }

                        if (Params.Status.UpdatePercent is object)
                        {
                            Params.Status.UpdatePercent.DynamicInvoke((int)percent_done);
                        }
                    }
                    else // Write/verification failed
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private SectorInfo GetSectorInfo(uint sector_index)
        {
            SectorInfo si;
            si.BaseAddress = GetSectorBaseAddress(sector_index);
            si.Size = GetSectorSize(sector_index);
            return si;
        }

        private partial struct SectorInfo
        {
            public long BaseAddress;
            public uint Size;
        }
        // Does the actual erase sector and program functions
        private bool WriteBytes_EraseSectorAndWrite(ref uint sector, byte[] data, int Percent, WriteParameters Params)
        {
            try
            {
                int FailedAttempts = 0;
                bool ReadResult;
                do
                {
                    if (Params.Status.UpdateBase is object)
                        Params.Status.UpdateBase.DynamicInvoke(Params.Address);
                    if (Params.AbortOperation)
                        return false;
                    if (!NoErrors)
                        return false;
                    if (Params.EraseSector)
                    {
                        if (Params.Status.UpdateOperation is object)
                        {
                            Params.Status.UpdateOperation.DynamicInvoke(4); // ERASE IMG
                        }

                        if (Params.Status.UpdateTask is object)
                        {
                            Params.Status.UpdateTask.DynamicInvoke("Erasing memory sector");
                        }
                        EraseSector(sector);
                        if (!NoErrors)
                        {
                            PrintConsole?.Invoke("Failed to erase memory at address: 0x" + Params.Address.ToString("X").PadLeft(8, '0'));
                            if (!GetMessageBoxForSectorErase(Params.Address, sector))
                                return false;
                        }
                    }

                    if (Params.Status.UpdateOperation is object)
                    {
                        Params.Status.UpdateOperation.DynamicInvoke(2); // WRITE IMG
                    }

                    if (Params.Status.UpdateTask is object)
                    {
                        var data_size_str = String.Format("{0:n0}", data.Length);
                        string wr_label = string.Format("Writing memory with {0} bytes", data_size_str);
                        Params.Status.UpdateTask.DynamicInvoke(wr_label);
                    }

                    WriteSector(sector, data, Params);
                    if (Params.AbortOperation)
                        return false;
                    if (!NoErrors)
                        return false;
                    if (Params.Verify) // Verify is enabled and we are monitoring this
                    {
                        if (Params.Status.UpdateOperation is object)
                        {
                            Params.Status.UpdateOperation.DynamicInvoke(3); // VERIFY IMG
                        }

                        if (Params.Status.UpdateTask is object)
                        {
                            Params.Status.UpdateTask.DynamicInvoke("Verifing written data at {0}");
                        }
                        if (FlashType == MemoryType.PARALLEL_NOR)
                        {
                            Thread.Sleep(200); // Some older devices need a delay here after writing data (such as AM29F040B)
                        }
                        ReadResult = WriteBytes_VerifyWrite(Params.Address, data);
                        if (Params.AbortOperation)
                            return false;
                        if (!NoErrors)
                            return false;
                        if (ReadResult)
                        {
                            FailedAttempts = 0;
                            if (Params.Status.UpdateTask is object)
                            {
                                Params.Status.UpdateTask.DynamicInvoke("Data verification was successful");
                            }
                        } else {
                            if (FailedAttempts == this.RetryWriteCount) {
                                if (FlashType == MemoryType.PARALLEL_NAND) {
                                    P_NAND n_dev = FCUSB.PARALLEL_NAND_IF.MyFlashDevice;
                                    uint pages_per_block = n_dev.Block_Size / n_dev.PAGE_SIZE;
                                    uint page_addr = MainApp.NAND_LayoutTool.GetNandPageAddress(n_dev, Params.Address, FCUSB.PARALLEL_NAND_IF.MemoryArea);
                                    uint block_addr = (uint)Math.Floor(page_addr / (double)pages_per_block);
                                    PrintConsole?.Invoke(string.Format("BAD NAND BLOCK at page index: 0x{0} (block index: {1})", page_addr.ToString("X").PadLeft(6, '0'), block_addr));
                                    return false;
                                } else if (FlashType == MemoryType.SERIAL_NAND) {
                                    SPI_NAND n_dev = FCUSB.SPI_NAND_IF.MyFlashDevice;
                                    uint pages_per_block = n_dev.Block_Size / n_dev.PAGE_SIZE;
                                    uint page_addr = MainApp.NAND_LayoutTool.GetNandPageAddress(n_dev, Params.Address, FCUSB.PARALLEL_NAND_IF.MemoryArea);
                                    uint block_addr = (uint)Math.Floor(page_addr / (double)pages_per_block);
                                    PrintConsole?.Invoke(string.Format("BAD NAND BLOCK at page index: 0x{0} (block index: {1})", page_addr.ToString("X").PadLeft(6, '0'), block_addr));
                                    return false;
                                }
                                else if (FlashType == MemoryType.PARALLEL_NOR && (FCUSB.PARALLEL_NOR_IF.MyFlashDevice.GetType() == typeof(OTP_EPROM)))
                                {
                                    PrintConsole?.Invoke(string.Format("Data verification failed at 0x{0}", Params.Address.ToString("X")));
                                    if (Params.Status.UpdateOperation is object)
                                    {
                                        Params.Status.UpdateOperation.DynamicInvoke(5); // ERROR IMG
                                    }

                                    if (Params.Status.UpdateTask is object)
                                    {
                                        Params.Status.UpdateTask.DynamicInvoke("Data verification failed!");
                                        Thread.Sleep(1000);
                                    }
                                    return false;
                                }
                                else
                                {
                                    PrintConsole?.Invoke(string.Format("Data verification failed at 0x{0}", Params.Address.ToString("X")));
                                    if (Params.Status.UpdateOperation is object)
                                    {
                                        Params.Status.UpdateOperation.DynamicInvoke(5); // ERROR IMG
                                    }

                                    if (Params.Status.UpdateTask is object)
                                    {
                                        Params.Status.UpdateTask.DynamicInvoke("Data verification failed!");
                                        Thread.Sleep(1000);
                                    }
                                    return WriteErrorOnVerifyWrite(Params.Address);
                                }
                            }

                            FailedAttempts += 1;
                            Thread.Sleep(500);
                        }
                    }
                    else
                    {
                        ReadResult = true;
                    } // We are skiping verification
                }
                while (!(ReadResult | !Params.Verify));
            }
            catch
            {
                return false;
            } // ERROR

            return true;
        }

        private bool WriteBytes_VerifyWrite(long BaseAddress, byte[] Data) {
            byte[] Verify; // The data to check against
            int MiscountCounter = 0;
            byte FirstWrongByteIs = default;
            int FirstWrongAddr = 0;
            byte FirstWrongByteShould = 0;
            WaitUntilReady();
            Verify = ReadFlash(BaseAddress, (uint)Data.Length);
            if (Verify is null || !(Verify.Length == Data.Length))
                return false;
            for (int i = 0, loopTo = Data.Length - 1; i <= loopTo; i++)
            {
                if (!(Data[i] == Verify[i]))
                {
                    if (MiscountCounter == 0)
                    {
                        FirstWrongByteIs = Verify[i];
                        FirstWrongByteShould = Data[i];
                        FirstWrongAddr = (int)(BaseAddress + i);
                    }

                    MiscountCounter = MiscountCounter + 1;
                }
            }
            if (MiscountCounter == 0) // Verification successful
            {
                return true;
            }
            else // Error!
            {
                var param1 = "0x" + FirstWrongAddr.ToString("X");
                var param2 = "0x" + FirstWrongByteShould.ToString("X");
                var param3 = "0x" + FirstWrongByteIs.ToString("X");
                PrintConsole?.Invoke(string.Format("Address {0}: wrote {1} and read {2} ({3} mismatches)", param1, param2, param3, MiscountCounter));
                return false;
            } // Error!
        }

        public bool WriteErrorOnVerifyWrite(long address)
        {
            PrintConsole?.Invoke(string.Format("Data verification failed at 0x{0}", address.ToString("X").PadLeft(8, '0')));
            return false;
        }

        public bool GetMessageBoxForSectorErase(long address, uint sector_index)
        {
            string TitleTxt = string.Format("Failed to erase Flash at address: 0x{0} (sector index: {1})", address.ToString("X").PadLeft(8, '0'), sector_index);
            TitleTxt += "\nContinue write operation? (YES/NO)";
            Console.WriteLine(TitleTxt);
            var user_response = Console.ReadLine();
            if (user_response.ToUpper().Equals("YES"))
            {
                return true;
            } else
            {
                return false; // Stop working
            }
        }

        public void WaitUntilReady()
        {
            switch (FlashType)
            {
                case var @case when @case == MemoryType.JTAG_CFI:
                    {
                        FCUSB.JTAG_IF.CFI_WaitUntilReady();
                        break;
                    }

                case var case1 when case1 == MemoryType.JTAG_SPI:
                    {
                        FCUSB.JTAG_IF.SPI_WaitUntilReady();
                        break;
                    }

                case var case2 when case2 == MemoryType.JTAG_BSDL:
                    {
                        FCUSB.JTAG_IF.BoundaryScan_WaitForReady();
                        break;
                    }

                default:
                    {
                        FCUSB.PROGRAMMER.WaitUntilReady();
                        break;
                    }
            }
        }

        public void ReadMode()
        {
            switch (FlashType)
            {
                case var @case when @case == MemoryType.PARALLEL_NOR:
                    {
                        FCUSB.PARALLEL_NOR_IF.ResetDevice();
                        break;
                    }

                case var case1 when case1 == MemoryType.JTAG_CFI:
                    {
                        FCUSB.JTAG_IF.CFI_ReadMode();
                        break;
                    }
            }
        }

        public byte[] ReadFlash(long Address, long Count) {
            if (!WaitForNotBusy())
                return null;
            try {
                IsReading = true;
                byte[] data_out = null;
                int offset = MainApp.BitSwap_Offset();
                long data_read_count = Count;
                long data_offset = Address;
                long align = 0L;
                if (offset > 0)
                    align = Address % offset;
                if (align > 0L) {
                    data_offset -= align;
                    data_read_count = (uint)(data_read_count + align);
                    while (data_read_count % offset != 0L)
                        data_read_count = (uint)(data_read_count + 1L);
                }
                try {
                    Monitor.Enter(InterfaceLock);
                    switch (FlashType) {
                        case var case1 when case1 == MemoryType.JTAG_CFI:
                            {
                                data_out = FCUSB.JTAG_IF.CFI_ReadFlash((uint)data_offset, (uint)data_read_count);
                                break;
                            }
                        case var case2 when case2 == MemoryType.JTAG_SPI:
                            {
                                data_out = FCUSB.JTAG_IF.SPI_ReadFlash((uint)data_offset, (uint)data_read_count);
                                break;
                            }
                        case var case3 when case3 == MemoryType.JTAG_BSDL:
                            {
                                data_out = FCUSB.JTAG_IF.BoundaryScan_ReadFlash((uint)data_offset, (uint)data_read_count);
                                break;
                            }
                        default:
                            {
                                data_out = FCUSB.PROGRAMMER.ReadData(data_offset, data_read_count);
                                break;
                            }
                    }
                } catch {
                } finally {
                    Monitor.Exit(InterfaceLock);
                }
                if (data_out is null) {
                    NoErrors = false; // We have a read operation read
                    return null;
                }
                MainApp.BitSwap_Reverse(ref data_out);
                if (align > 0L) {
                    var processed_data = new byte[(int)(Count - 1L + 1)];
                    Array.Copy(data_out, align, processed_data, 0L, processed_data.Length);
                    return processed_data;
                } else {
                    return data_out;
                }
            } finally {
                IsReading = false;
            }
        }

        public bool EraseFlash() {
            if (!WaitForNotBusy())
                return false;
            try {
                Monitor.Enter(InterfaceLock);
                IsBulkErasing = true;
                switch (FlashType) {
                    case var @case when @case == MemoryType.JTAG_CFI:
                        {
                            FCUSB.JTAG_IF.CFI_EraseDevice();
                            break;
                        }
                    case var case1 when case1 == MemoryType.JTAG_SPI:
                        {
                            FCUSB.JTAG_IF.SPI_EraseBulk();
                            break;
                        }
                    case var case2 when case2 == MemoryType.JTAG_BSDL:
                        {
                            FCUSB.JTAG_IF.BoundaryScan_EraseDevice();
                            break;
                        }
                    default:
                        {
                            FCUSB.PROGRAMMER.EraseDevice();
                            break;
                        }
                }
            } finally {
                Monitor.Exit(InterfaceLock);
                IsBulkErasing = false;
            }
            return true;
        }

        internal void DisableGuiControls() {
            throw new NotImplementedException();
        }

        public void EraseSector(uint sector_index)
        {
            if (!WaitForNotBusy())
                return;
            IsErasing = true;
            try
            {
                Monitor.Enter(InterfaceLock);
                switch (FlashType)
                {
                    case var @case when @case == MemoryType.JTAG_CFI:
                        {
                            NoErrors = FCUSB.JTAG_IF.CFI_Sector_Erase(sector_index);
                            break;
                        }

                    case var case1 when case1 == MemoryType.JTAG_SPI:
                        {
                            NoErrors = FCUSB.JTAG_IF.SPI_SectorErase(sector_index);
                            break;
                        }

                    case var case2 when case2 == MemoryType.JTAG_BSDL:
                        {
                            NoErrors = FCUSB.JTAG_IF.BoundaryScan_SectorErase(sector_index);
                            break;
                        }

                    default:
                        {
                            NoErrors = FCUSB.PROGRAMMER.SectorErase(sector_index);
                            break;
                        }
                }
            }
            finally
            {
                Monitor.Exit(InterfaceLock);
            }
            IsErasing = false;
        }

        public void WriteSector(UInt32 sector_index, byte[] Data, WriteParameters Params)
        {
            if (!WaitForNotBusy())
                return;
            IsWriting = true;
            try
            {
                Monitor.Enter(InterfaceLock);
                var DataToWrite = new byte[Data.Length];
                Array.Copy(Data, DataToWrite, Data.Length);
                MainApp.BitSwap_Forward(ref DataToWrite);
                if (Params is object)
                    Params.Timer.Start();
                switch (FlashType)
                {
                    case var @case when @case == MemoryType.JTAG_CFI:
                        {
                            NoErrors = FCUSB.JTAG_IF.CFI_SectorWrite(sector_index, DataToWrite);
                            break;
                        }

                    case var case1 when case1 == MemoryType.JTAG_SPI:
                        {
                            NoErrors = FCUSB.JTAG_IF.SPI_SectorWrite(sector_index, DataToWrite);
                            break;
                        }

                    case var case2 when case2 == MemoryType.JTAG_BSDL:
                        {
                            NoErrors = FCUSB.JTAG_IF.BoundaryScan_SectorWrite(sector_index, DataToWrite);
                            break;
                        }

                    default:
                        {
                            NoErrors = FCUSB.PROGRAMMER.SectorWrite(sector_index, DataToWrite, Params);
                            break;
                        }
                }

                if (Params is object)
                    Params.Timer.Stop();
            }
            finally
            {
                Monitor.Exit(InterfaceLock);
            }

            IsWriting = false;
        }

        public uint GetSectorCount()
        {
            switch (FlashType)
            {
                case var @case when @case == MemoryType.JTAG_CFI:
                    {
                        return FCUSB.JTAG_IF.CFI_SectorCount();
                    }

                case var case1 when case1 == MemoryType.JTAG_SPI:
                    {
                        return FCUSB.JTAG_IF.SPI_SectorCount();
                    }

                case var case2 when case2 == MemoryType.JTAG_BSDL:
                    {
                        return FCUSB.JTAG_IF.BoundaryScan_SectorCount();
                    }

                default:
                    {
                        return FCUSB.PROGRAMMER.SectorCount();
                    }
            }
        }

        public uint GetSectorSize(uint sector_index)
        {
            switch (FlashType)
            {
                case var @case when @case == MemoryType.JTAG_CFI:
                    {
                        return FCUSB.JTAG_IF.CFI_GetSectorSize(sector_index);
                    }

                case var case1 when case1 == MemoryType.JTAG_SPI:
                    {
                        return FCUSB.JTAG_IF.SPI_GetSectorSize(sector_index);
                    }

                case var case2 when case2 == MemoryType.JTAG_BSDL:
                    {
                        return FCUSB.JTAG_IF.BoundaryScan_GetSectorSize(sector_index);
                    }

                default:
                    {
                        return FCUSB.PROGRAMMER.SectorSize(sector_index);
                    }
            }
        }

        public long GetSectorBaseAddress(uint sector_index)
        {
            switch (FlashType)
            {
                case var @case when @case == MemoryType.JTAG_CFI:
                    {
                        return FCUSB.JTAG_IF.CFI_FindSectorBase(sector_index);
                    }

                case var case1 when case1 == MemoryType.JTAG_SPI:
                    {
                        return FCUSB.JTAG_IF.CFI_FindSectorBase(sector_index);
                    }

                case var case2 when case2 == MemoryType.JTAG_BSDL:
                    {
                        return FCUSB.JTAG_IF.BoundaryScan_SectorFind(sector_index);
                    }

                default:
                    {
                        return FCUSB.PROGRAMMER.SectorFind(sector_index);
                    }
            }
        }

    }

    public void AbortOperations() {
        try {
            ushort Counter = 0;
            foreach (var memdev in MyDevices) {
                while (memdev.IsBusy) {
                    Thread.Sleep(100);
                    Counter = (ushort)(Counter + 1);
                    if (Counter == 100) { return; } // 10 seconds
                }
            }
        } catch {
        }
    }

    private static string UpdateSpeed_GetText(int bytes_per_second) {
        string speed_str;
        var Mb008 = (Single)1048576;
        if (bytes_per_second > Mb008 - 1) {
            speed_str = String.Format("{0:n3}", (Single)(bytes_per_second / Mb008)) + " MB/s";
        } else if (bytes_per_second > 8191) {
            speed_str = String.Format("{0:n2}", (Single)(bytes_per_second / 1024d)) + " KB/s";
        } else {
            speed_str = String.Format("{0:n3}", bytes_per_second) + " B/s";
        }
        return speed_str;
    }

}