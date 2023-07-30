// COPYRIGHT EMBEDDED COMPUTERS LLC 2020 - ALL RIGHTS RESERVED
// THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB PRODUCTS
// CONTACT EMAIL: support@embeddedcomputers.net
// ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
// INFO: This is the main module that is loaded first.

using System;
using Microsoft.VisualBasic;

namespace FlashcatUSB {
    public class EPROM_Programmer : FlashcatUSB.MemoryDeviceUSB {
        private FlashcatUSB.USB.FCUSB_DEVICE FCUSB;

        public FlashcatUSB.FlashMemory.Device MyFlashDevice { get; set; }
        public FlashcatUSB.USB.DeviceStatus MyFlashStatus { get; set; } = FlashcatUSB.USB.DeviceStatus.NotDetected;
        public FlashcatUSB.MEM_PROTOCOL MyAdapter { get; set; } // This is the kind of socket adapter connected and the mode it is in

        public event PrintConsoleEventHandler PrintConsole;

        public delegate void PrintConsoleEventHandler(string message);

        public event SetProgressEventHandler SetProgress;

        public delegate void SetProgressEventHandler(int percent);

        public EPROM_Programmer(FlashcatUSB.USB.FCUSB_DEVICE parent_if) {
            FCUSB = parent_if;
        }

        public string DeviceName {
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

        public long DeviceSize {
            get {
                FlashcatUSB.FlashMemory.OTP_EPROM NOR_FLASH = (FlashcatUSB.FlashMemory.OTP_EPROM)MyFlashDevice;
                return NOR_FLASH.FLASH_SIZE;
            }
        }

        public uint SectorSize(uint sector, FlashcatUSB.FlashMemory.FlashArea area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            return 8192U; // Program 8KB at a time
        }

        public void WaitUntilReady() {
            FlashcatUSB.Utilities.Main.Sleep(100);
        }

        public bool DeviceInit() {
            MyFlashDevice = null;
            if (EPROM_Detect()) {
                FlashcatUSB.FlashMemory.OTP_EPROM o = (FlashcatUSB.FlashMemory.OTP_EPROM)MyFlashDevice;
                if (o.IFACE == FlashcatUSB.FlashMemory.VCC_IF.X16_5V_12VPP) {
                    this.EXPIO_SETUP_USB(FlashcatUSB.MEM_PROTOCOL.EPROM_X16);
                    FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.EXPIO_MODE_WRITE, null, (uint)E_DEV_MODE.EPROM_X16);
                    MyAdapter = FlashcatUSB.MEM_PROTOCOL.EPROM_X16;
                    PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("ext_write_mode_supported") + ": EPROM (16-bit)");
                } else if (o.IFACE == FlashcatUSB.FlashMemory.VCC_IF.X8_5V_12VPP) {
                    this.EXPIO_SETUP_USB(FlashcatUSB.MEM_PROTOCOL.EPROM_X8);
                    MyAdapter = FlashcatUSB.MEM_PROTOCOL.EPROM_X8;
                    FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.EXPIO_MODE_WRITE, null, (uint)E_DEV_MODE.EPROM_X8);
                    MyAdapter = FlashcatUSB.MEM_PROTOCOL.EPROM_X8;
                    PrintConsole?.Invoke(FlashcatUSB.MainApp.RM.GetString("ext_write_mode_supported") + ": EPROM (8-bit)");
                }
            } else {
                PrintConsole?.Invoke("Unable to automatically detect EPROM/OTP device");
                return false;
            }

            PrintConsole?.Invoke("EPROM successfully detected!");
            PrintConsole?.Invoke("EPROM device: " + MyFlashDevice.NAME + ", size: " + Strings.Format((object)MyFlashDevice.FLASH_SIZE, "#,###") + " bytes");
            FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.EXPIO_DELAY, null, (uint)((FlashcatUSB.FlashMemory.OTP_EPROM)MyFlashDevice).HARDWARE_DELAY);
            ((FlashcatUSB.FlashMemory.OTP_EPROM)MyFlashDevice).IS_BLANK = true;
            // DirectCast(MyFlashDevice, OTP_EPROM).IS_BLANK = EPROM_BlankCheck()
            FlashcatUSB.MainApp.SetStatus("EPROM mode ready for operation");
            MyFlashStatus = FlashcatUSB.USB.DeviceStatus.Supported;
            return true;
        }

        public byte[] ReadData(long flash_offset, long data_count, FlashcatUSB.FlashMemory.FlashArea area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            FlashcatUSB.FlashMemory.OTP_EPROM M27C160 = (FlashcatUSB.FlashMemory.OTP_EPROM)FlashcatUSB.MainApp.FlashDatabase.FindDevice(0x20, 0xB1, 0, FlashcatUSB.FlashMemory.MemoryType.OTP_EPROM);
            FlashcatUSB.FlashMemory.OTP_EPROM M27C801 = (FlashcatUSB.FlashMemory.OTP_EPROM)FlashcatUSB.MainApp.FlashDatabase.FindDevice(0x20, 0x42, 0, FlashcatUSB.FlashMemory.MemoryType.OTP_EPROM);
            FlashcatUSB.FlashMemory.OTP_EPROM M27C1001 = (FlashcatUSB.FlashMemory.OTP_EPROM)FlashcatUSB.MainApp.FlashDatabase.FindDevice(0x20, 0x5, 0, FlashcatUSB.FlashMemory.MemoryType.OTP_EPROM);
            if (object.ReferenceEquals(MyFlashDevice, M27C160)) {
                this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_5V);
                this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.OE_LOW);
                this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_ENABLE); // Must enable VPP for BYTEvpp=HIGH(5V)
            } else if (object.ReferenceEquals(MyFlashDevice, M27C1001)) {
                this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_0V);
                this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.OE_LOW);
                this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_DISABLE);
            } else {
                this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_0V);
                this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_DISABLE);
            }

            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.WE_LOW);
            FlashcatUSB.Utilities.Main.Sleep(100);
            var data_out = ReadBulk((uint)flash_offset, (uint)data_count);
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.WE_HIGH);
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_0V);
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_DISABLE);
            return data_out;
        }

        public bool WriteData(long flash_offset, byte[] data_to_write, FlashcatUSB.WriteParameters Params = null) {
            try {
                uint PacketSize = 2048U;
                FlashcatUSB.FlashMemory.OTP_EPROM M27C160 = (FlashcatUSB.FlashMemory.OTP_EPROM)FlashcatUSB.MainApp.FlashDatabase.FindDevice(0x20, 0xB1, 0, FlashcatUSB.FlashMemory.MemoryType.OTP_EPROM);
                FlashcatUSB.FlashMemory.OTP_EPROM M27C801 = (FlashcatUSB.FlashMemory.OTP_EPROM)FlashcatUSB.MainApp.FlashDatabase.FindDevice(0x20, 0x42, 0, FlashcatUSB.FlashMemory.MemoryType.OTP_EPROM);
                FlashcatUSB.FlashMemory.OTP_EPROM M27C1001 = (FlashcatUSB.FlashMemory.OTP_EPROM)FlashcatUSB.MainApp.FlashDatabase.FindDevice(0x20, 0x5, 0, FlashcatUSB.FlashMemory.MemoryType.OTP_EPROM);
                uint BytesWritten = 0U;
                uint DataToWrite = (uint)data_to_write.Length;
                int Loops = (int)Math.Ceiling(DataToWrite / (double)PacketSize); // Calcuates iterations
                this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.WE_HIGH);
                this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_12V);
                this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_ENABLE);
                if (object.ReferenceEquals(MyFlashDevice, M27C160))
                    this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.OE_HIGH);
                if (object.ReferenceEquals(MyFlashDevice, M27C1001))
                    this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.OE_HIGH);
                FlashcatUSB.Utilities.Main.Sleep(200);
                for (int i = 0, loopTo = Loops - 1; i <= loopTo; i++) {
                    if (Params.AbortOperation)
                        return false;
                    int BufferSize = (int)DataToWrite;
                    if (BufferSize > PacketSize)
                        BufferSize = (int)PacketSize;
                    var data_packet = new byte[BufferSize];
                    Array.Copy(data_to_write, i * PacketSize, data_packet, 0L, data_packet.Length);
                    bool result = WriteBulk((uint)flash_offset, data_packet);
                    if (!result)
                        return false;
                    flash_offset += data_packet.Length;
                    DataToWrite = (uint)(DataToWrite - data_packet.Length);
                    BytesWritten = (uint)(BytesWritten + data_packet.Length);
                }

                this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_0V);
                this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_DISABLE);
                if (object.ReferenceEquals(MyFlashDevice, M27C160))
                    this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.OE_LOW);
            } catch (Exception ex) {
            }

            return true;
        }

        public long SectorFind(uint sector_index, FlashcatUSB.FlashMemory.FlashArea memory_area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            uint base_addr = 0U;
            if (sector_index > 0L) {
                for (uint i = 0U, loopTo = (uint)(sector_index - 1L); i <= loopTo; i++)
                    base_addr += SectorSize(i, memory_area);
            }

            return base_addr;
        }

        public uint SectorCount() {
            return MyFlashDevice.Sector_Count;
        }

        public bool SectorWrite(uint sector_index, byte[] data, FlashcatUSB.WriteParameters Params = null) {
            uint Addr32 = (uint)this.SectorFind(sector_index, Params.Memory_Area);
            return WriteData(Addr32, data, Params);
        }

        public bool EraseDevice() {
            throw new NotSupportedException();
        }

        public bool SectorErase(uint SectorIndex, FlashcatUSB.FlashMemory.FlashArea area = FlashcatUSB.FlashMemory.FlashArea.Main) {
            throw new NotSupportedException();
        }

        public bool EPROM_Detect() {
            if (!this.EXPIO_SETUP_USB(FlashcatUSB.MEM_PROTOCOL.EPROM_X8))
                return false;
            FlashcatUSB.Utilities.Main.Sleep(200);
            byte[] IDENT_DATA;
            IDENT_DATA = EPROM_ReadEletronicID_1();
            MyFlashDevice = FlashcatUSB.MainApp.FlashDatabase.FindDevice(IDENT_DATA[0], IDENT_DATA[1], 0, FlashcatUSB.FlashMemory.MemoryType.OTP_EPROM);
            if (MyFlashDevice is object)
                return true; // Detected!
            IDENT_DATA = EPROM_ReadEletronicID_2();
            MyFlashDevice = FlashcatUSB.MainApp.FlashDatabase.FindDevice(IDENT_DATA[0], IDENT_DATA[1], 0, FlashcatUSB.FlashMemory.MemoryType.OTP_EPROM);
            if (MyFlashDevice is object)
                return true; // Detected!
            return false;
        }

        private bool EXPIO_SETUP_USB(FlashcatUSB.MEM_PROTOCOL mode) {
            try {
                var result_data = new byte[1];
                uint setup_data = (uint)mode;
                bool result = FCUSB.USB_CONTROL_MSG_IN(FlashcatUSB.USB.USBREQ.EXPIO_INIT, ref result_data, setup_data);
                if (!result)
                    return false;
                if (result_data[0] == 0x17) {
                    System.Threading.Thread.Sleep(50); // Give the USB time to change modes
                    return true; // Communication successful
                } else {
                    return false;
                }
            } catch (Exception ex) {
                return false;
            }
        }

        private byte[] EPROM_ReadEletronicID_1() {
            var IDENT_DATA = new byte[2];
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_ENABLE); // Enables VPP on adapter (CLE=HIGH)
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.OE_LOW);
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.WE_LOW);
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_12V);
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.RELAY_ON); // A9=12V and VPP=12V
            FlashcatUSB.Utilities.Main.Sleep(300); // Need this to be somewhat high to allow ID CODE to load
            FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.EXPIO_READDATA, GetSetupPacket(0U, 2U, 0), ref IDENT_DATA, 0U);
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.RELAY_OFF);
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_0V);
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.WE_HIGH);
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_DISABLE);
            if (IDENT_DATA[0] == 0 | IDENT_DATA[1] == 0) {
            } else if (IDENT_DATA[0] == 255 | IDENT_DATA[1] == 255) {
            } else {
                PrintConsole?.Invoke("EPROM IDENT CODE 1 returned MFG: 0x" + Conversion.Hex(IDENT_DATA[0]) + " and PART 0x" + Conversion.Hex(IDENT_DATA[1]));
            }

            return IDENT_DATA;
        }

        private byte[] EPROM_ReadEletronicID_2() {
            var IDENT_DATA = new byte[2];
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_DISABLE); // Disables VPP Pin on Adapter (CLE=LOW)
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.OE_LOW);
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.WE_LOW);
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_12V);
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.RELAY_ON); // A9=12V and VPP=0V
            FlashcatUSB.Utilities.Main.Sleep(300); // Need this to be somewhat high to allow ID CODE to load
            FCUSB.USB_SETUP_BULKIN(FlashcatUSB.USB.USBREQ.EXPIO_READDATA, GetSetupPacket(0U, 2U, 0), ref IDENT_DATA, 0U);
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.RELAY_OFF);
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.VPP_0V);
            this.HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL.WE_HIGH);
            if (IDENT_DATA[0] == 0 | IDENT_DATA[1] == 0) {
            } else if (IDENT_DATA[0] == 255 | IDENT_DATA[1] == 255) {
            } else {
                PrintConsole?.Invoke("EPROM IDENT CODE 2 returned MFG: 0x" + Conversion.Hex(IDENT_DATA[0]) + " and PART 0x" + Conversion.Hex(IDENT_DATA[1]));
            }

            return IDENT_DATA;
        }

        public bool EPROM_BlankCheck() {
            FlashcatUSB.MainApp.SetStatus("Performing EPROM blank check");
            SetProgress?.Invoke(0);
            var entire_data = new byte[(int)(MyFlashDevice.FLASH_SIZE - 1L + 1)];
            int BlockCount = (int)(entire_data.Length / 8192d);
            for (int i = 0, loopTo = BlockCount - 1; i <= loopTo; i++) {
                if (FlashcatUSB.MainApp.AppIsClosing)
                    return false;
                var block = ReadBulk((uint)(i * 8191), 8191U);
                Array.Copy(block, 0, entire_data, i * 8191, 8191);
                float percent = (float)(i / (double)BlockCount * 100d);
                SetProgress?.Invoke((int)Math.Floor(percent));
            }

            if (FlashcatUSB.Utilities.Main.IsByteArrayFilled(ref entire_data, 255)) {
                PrintConsole?.Invoke("EPROM device is blank and can be programmed");
                ((FlashcatUSB.FlashMemory.OTP_EPROM)MyFlashDevice).IS_BLANK = true;
                return true;
            } else {
                PrintConsole?.Invoke("EPROM device is not blank");
                ((FlashcatUSB.FlashMemory.OTP_EPROM)MyFlashDevice).IS_BLANK = false;
                return false;
            }
        }

        private void HardwareControl(FlashcatUSB.USB.FCUSB_HW_CTRL cmd) {
            FCUSB.USB_CONTROL_MSG_OUT(FlashcatUSB.USB.USBREQ.EXPIO_CTRL, null, (uint)cmd);
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

        private ushort[] ByteToWordArray(byte[] byte_arr) {
            var @out = new ushort[(int)(byte_arr.Length / 2d - 1d + 1)];
            int ptr = 0;
            for (int i = 0, loopTo = @out.Length - 1; i <= loopTo; i++) {
                @out[i] = byte_arr[ptr + 1] << 8 | byte_arr[ptr];
                ptr += 2;
            }

            return @out;
        }

        private byte[] ReadBulk(uint address, uint count) {
            try {
                uint read_count = count;
                bool addr_offset = false;
                bool count_offset = false;
                if (MyAdapter == FlashcatUSB.MEM_PROTOCOL.EPROM_X16) {
                    if (address % 2L == 1L) {
                        addr_offset = true;
                        address = (uint)(address - 1L);
                        read_count = (uint)(read_count + 1L);
                    }

                    if (read_count % 2L == 1L) {
                        count_offset = true;
                        read_count = (uint)(read_count + 1L);
                    }
                }

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

        private bool WriteBulk(uint address, byte[] data_out) {
            try {
                var setup_data = GetSetupPacket(address, (uint)data_out.Length, (ushort)MyFlashDevice.PAGE_SIZE);
                bool result = FCUSB.USB_SETUP_BULKOUT(FlashcatUSB.USB.USBREQ.EXPIO_WRITEDATA, setup_data, data_out, 0U);
                return result;
            } catch (Exception ex) {
            }

            return false;
        }

        private enum E_DEV_MODE : ushort {
            EPROM_X8 = 7, // 8-BIT EPROM DEVICE
            EPROM_X16 = 8 // 16-BIT EPROM DEVICE
        }
    }
}