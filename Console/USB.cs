using LibUsbDotNet;
using LibUsbDotNet.Main;
using SPI;
using System;
using System.Collections.Generic;
using System.Threading;

namespace USB {

    public static class Shared {
        internal const int DEFAULT_TIMEOUT = 5000;
        internal const UInt16 USB_VID_ATMEL = 0x3EB;
        internal const UInt16 USB_VID_EC = 0x16C0;
        internal const UInt16 USB_PID_AT90USB162 = 0x2FFA; // FCUSB PCB 1.x
        internal const UInt16 USB_PID_AT90USB1287 = 0x2FFB; // FCUSB EX (PROTO)
        internal const UInt16 USB_PID_AT90USB646 = 0x2FF9; // FCUSB EX (PRODUCTION)
        internal const UInt16 USB_PID_ATMEGA32U2 = 0x2FF0; // FCUSB PCB 2.1-2.2
        internal const UInt16 USB_PID_ATMEGA32U4 = 0x2FF4; // FCUSB PCB 2.3
        internal const UInt16 USB_PID_FCUSB_PRO = 0x5E0;
        internal const UInt16 USB_PID_FCUSB_MACH = 0x5E1;
        internal const int USB_PID_FCUSB = 0x5DE; // Classic
    }

    public class HostClient {
        public event DeviceConnectedEventHandler DeviceConnected;

        public delegate void DeviceConnectedEventHandler(FCUSB_DEVICE usb_dev);

        public event DeviceDisconnectedEventHandler DeviceDisconnected;

        public delegate void DeviceDisconnectedEventHandler(FCUSB_DEVICE usb_dev);

        private List<FCUSB_DEVICE> ConnectedDevices = new List<FCUSB_DEVICE>();

        public bool CloseService { get; set; } = false;

        public HostClient()
        {
        }

        public void StartService() {
            try {
                Thread td = new Thread(ConnectionThread);
                td.Name = "tdUsbMonitor";
                td.Start();
            } catch {
            }
        }

        private void ConnectionThread() {
            bool is_connected = false;
            while ((!CloseService)) {
                CheckConnectedDevices();
                if (!is_connected) {
                    is_connected = ConnectToDevice();
                }
                Utilities.Sleep(200);
            }
            DisconnectAll();
        }

        private bool ConnectToDevice() {
            try {
                var fcusb_list = FindUsbDevices(); // Returns a list of all FCUSB devices connected
                if (fcusb_list != null && fcusb_list.Length > 0) {
                    foreach (var fc_reg in fcusb_list) {
                        UsbDevice fcusb_dev = fc_reg.Device;
                        if (fcusb_dev == null) { break; }
                        FCUSB_DEVICE new_fcusb_dev = null;
                        if (Connect(fcusb_dev, ref new_fcusb_dev)) {
                            ConnectedDevices.Add(new_fcusb_dev);
                            new_fcusb_dev.OnDisconnected += FCUSB_Device_Disconnected;
                            DeviceConnected?.Invoke(new_fcusb_dev);
                            return true;
                        }
                    }
                }
            } catch {
            }
            return false;
        }

        private void CheckConnectedDevices() {
            List<FCUSB_DEVICE> disconnected_dev = new List<FCUSB_DEVICE>();
            foreach (FCUSB_DEVICE dev in ConnectedDevices) {
                if (dev.IS_CONNECTED) {
                    bool is_online = dev.CheckConnection();
                    if (!is_online) { disconnected_dev.Add(dev); }
                }
             }
            foreach (FCUSB_DEVICE dev in disconnected_dev) {
                dev.Disconnect();
            }
        }

        private void FCUSB_Device_Disconnected(FCUSB_DEVICE disconnected_device) {
            try {
                ConnectedDevices.Remove(disconnected_device);
            } catch {
            }
            DeviceDisconnected?.Invoke(disconnected_device);
        }

        private bool Connect(UsbDevice usb_device, ref FCUSB_DEVICE fcusb_dev) {
            try {
                if (OpenUsbDevice(usb_device)) {
                    FCUSB_DEVICE new_device = new FCUSB_DEVICE(usb_device);
                    if (usb_device.UsbRegistryInfo.Vid == Shared.USB_VID_ATMEL) {
                        new_device.IS_CONNECTED = true;
                        new_device.HWBOARD = FCUSB_BOARD.ATMEL_DFU;
                        fcusb_dev = new_device;
                        return true;
                    } else if (new_device.USB_Echo()) {
                        new_device.IS_CONNECTED = true;
                        bool Success = new_device.LoadFirmwareVersion();
                        if (!Success)
                            return false;
                        new_device.USB_LEDOn(); // Call after firmware version is loaded
                        fcusb_dev = new_device;
                        return true;
                    }
                    return false;
                }
            } catch {
            }
            return false;
        }

        private bool OpenUsbDevice(UsbDevice usb_dev) {
            try {
                if (usb_dev != null) {
                    usb_dev.Open();

                    IUsbDevice wholeUsbDevice = (IUsbDevice)usb_dev;
                    wholeUsbDevice.SetConfiguration(1);
                    wholeUsbDevice.ClaimInterface(0);
                    wholeUsbDevice.SetAltInterface(1);
                }
                return true;
            } catch {
                return false;
            }
        }

        private UsbRegistry[] FindUsbDevices() {
            try {
                var devices = new List<UsbRegistry>();
                AddDevicesToList(Shared.USB_VID_ATMEL, Shared.USB_PID_AT90USB162, devices);
                AddDevicesToList(Shared.USB_VID_ATMEL, Shared.USB_PID_AT90USB1287, devices);
                AddDevicesToList(Shared.USB_VID_ATMEL, Shared.USB_PID_ATMEGA32U2, devices);
                AddDevicesToList(Shared.USB_VID_ATMEL, Shared.USB_PID_AT90USB646, devices);
                AddDevicesToList(Shared.USB_VID_ATMEL, Shared.USB_PID_ATMEGA32U4, devices);
                AddDevicesToList(Shared.USB_VID_EC, Shared.USB_PID_FCUSB, devices);
                AddDevicesToList(Shared.USB_VID_EC, Shared.USB_PID_FCUSB_PRO, devices);
                AddDevicesToList(Shared.USB_VID_EC, Shared.USB_PID_FCUSB_MACH, devices);
                if (devices.Count == 0)
                    return null;
                return devices.ToArray();
            } catch {
            }
            return null;
        }

        private void AddDevicesToList(ushort VID, ushort PID, List<UsbRegistry> DeviceList) {
            var fcusb_usb_device = new UsbDeviceFinder(VID, PID);
            var fcusb_list = UsbDevice.AllDevices.FindAll(fcusb_usb_device);
            if (fcusb_list is object && fcusb_list.Count > 0) {
                for (int i = 0, loopTo = fcusb_list.Count - 1; i <= loopTo; i++) {
                    if (!ReferenceEquals(fcusb_list[i].GetType(), typeof(LibUsbDotNet.WinUsb.WinUsbRegistry))) {
                        DeviceList.Add(fcusb_list[i]);
                    }
                }
            }
        }

        public void DisconnectAll() {
            try {
                var dev_list = new List<FCUSB_DEVICE>();
                foreach (var fc_devs in this.ConnectedDevices) {
                    dev_list.Add(fc_devs);
                }
                foreach (var fc_devs in dev_list) {
                    fc_devs.Disconnect();
                }
            } catch {
            }
        }
    }

    public class FCUSB_DEVICE {  
        public UsbDevice USBHANDLE = null;
        public FCUSB_BOARD HWBOARD { get; set; }
        public bool IS_CONNECTED { get; set; } = false;
        public string FW_VERSION { get; set; } = "";
        public MemoryDeviceUSB PROGRAMMER { get; set; }
        public UsbCtrlFlags USBFLAG_OUT { get; set; }
        public UsbCtrlFlags USBFLAG_IN { get; set; }

        public SPI_Programmer SPI_NOR_IF;
        public SQI_Programmer SQI_NOR_IF;
        public SPINAND_Programmer SPI_NAND_IF;
        public PARALLEL_NOR PARALLEL_NOR_IF;
        public PARALLEL_NAND PARALLEL_NAND_IF;
        public FWH_Programmer FWH_IF;
        public EPROM_Programmer EPROM_IF;
        public HF_Programmer HF_IF;
        public I2C_Programmer I2C_IF;
        public Microwire_Programmer MW_IF;
        public SWI_Programmer SWI_IF;

        public JTAG.JTAG_IF JTAG_IF;
        public NAND_BLOCK_IF NAND_IF = new NAND_BLOCK_IF();

        private int USB_TIMEOUT_VALUE = Shared.DEFAULT_TIMEOUT;

        public event OnDisconnectedEventHandler OnDisconnected;

        public delegate void OnDisconnectedEventHandler(FCUSB_DEVICE this_dev);

        public event UpdateProgressEventHandler UpdateProgress;

        public delegate void UpdateProgressEventHandler(int percent, FCUSB_DEVICE device);

        public bool HasLogic {
            get {
                if (this.HWBOARD == FCUSB_BOARD.Professional_PCB5)
                    return true;
                else if (this.HWBOARD == FCUSB_BOARD.Mach1)
                    return true;
                else
                    return false;
            }
        }

        public FCUSB_DEVICE(UsbDevice my_handle) {
            this.USBHANDLE = my_handle;
            
            SPI_NOR_IF = new SPI_Programmer(this);
            SQI_NOR_IF = new SQI_Programmer(this);
            SPI_NAND_IF = new SPINAND_Programmer(this);
            PARALLEL_NOR_IF = new PARALLEL_NOR(this);
            PARALLEL_NAND_IF = new PARALLEL_NAND(this);
            FWH_IF = new FWH_Programmer(this);
            EPROM_IF = new EPROM_Programmer(this);
            HF_IF = new HF_Programmer(this);
            JTAG_IF = new JTAG.JTAG_IF(this);
            I2C_IF = new I2C_Programmer(this);
            MW_IF = new Microwire_Programmer(this);
            SWI_IF = new SWI_Programmer(this);

            SPI_NOR_IF.PrintConsole += MainApp.PrintConsole;
            SQI_NOR_IF.PrintConsole += MainApp.PrintConsole;
            SPI_NAND_IF.PrintConsole += MainApp.PrintConsole;
            I2C_IF.PrintConsole += MainApp.PrintConsole;
            PARALLEL_NOR_IF.PrintConsole += MainApp.PrintConsole;
            PARALLEL_NAND_IF.PrintConsole += MainApp.PrintConsole;
            MW_IF.PrintConsole += MainApp.PrintConsole;
            HF_IF.PrintConsole += MainApp.PrintConsole;
            FWH_IF.PrintConsole += MainApp.PrintConsole;
            EPROM_IF.PrintConsole += MainApp.PrintConsole;

            if (HasLogic) {
                this.USBFLAG_OUT = (UsbCtrlFlags.RequestType_Vendor | UsbCtrlFlags.Recipient_Interface | UsbCtrlFlags.Direction_Out);
                this.USBFLAG_IN = (UsbCtrlFlags.RequestType_Vendor | UsbCtrlFlags.Recipient_Interface | UsbCtrlFlags.Direction_In);
            } else {
                this.USBFLAG_OUT = (UsbCtrlFlags.RequestType_Vendor | UsbCtrlFlags.Recipient_Device | UsbCtrlFlags.Direction_Out);
                this.USBFLAG_IN = (UsbCtrlFlags.RequestType_Vendor | UsbCtrlFlags.Recipient_Device | UsbCtrlFlags.Direction_In);
            }
        }

        public bool CheckConnection() {
            if (USBHANDLE == null)
                return false;
            if (!USBHANDLE.UsbRegistryInfo.IsAlive)
                return false;
            return true;
        }

        public void Disconnect() {
            try {
                if (!this.IS_CONNECTED)
                    return;
                this.IS_CONNECTED = false;
                if (this.USBHANDLE != null) {
                    this.USB_LEDOff();
                    IUsbDevice wholeUsbDevice = this.USBHANDLE as IUsbDevice;
                    if (wholeUsbDevice != null)
                        wholeUsbDevice.ReleaseInterface(0);
                    this.USBHANDLE.Close();
                    this.USBHANDLE = null;
                }
            } catch {
            }
            OnDisconnected?.Invoke(this);
        }

        public void SelectProgrammer(DeviceMode dev) {
            if (dev== DeviceMode.SPI) {
                this.PROGRAMMER = SPI_NOR_IF;
            }
            else if (dev == DeviceMode.SQI) {
                this.PROGRAMMER = SQI_NOR_IF;
            }
            else if (dev == DeviceMode.SPI_NAND) {
                this.PROGRAMMER = SPI_NAND_IF;
            }
            else if (dev == DeviceMode.PNOR) {
                this.PROGRAMMER = PARALLEL_NOR_IF;
            }
            else if (dev == DeviceMode.PNAND) {
                this.PROGRAMMER = PARALLEL_NAND_IF;
            }
            else if (dev == DeviceMode.FWH) {
                this.PROGRAMMER = FWH_IF;
            }
            else if (dev == DeviceMode.EPROM) {
                this.PROGRAMMER = EPROM_IF;
            }
            else if (dev == DeviceMode.HyperFlash) {
                this.PROGRAMMER = HF_IF;
            }
            else if (dev == DeviceMode.I2C_EEPROM) {
                this.PROGRAMMER = I2C_IF;
            }
            else if (dev == DeviceMode.Microwire) {
                this.PROGRAMMER = MW_IF;
            }
            else if (dev == DeviceMode.ONE_WIRE) {
                this.PROGRAMMER = SWI_IF;
            }
            else if (dev == DeviceMode.SPI_EEPROM) {
                this.PROGRAMMER = SPI_NOR_IF;
            }
        }

        public bool USB_SPI_INIT(UInt32 mode, UInt32 clock_speed) {
            UInt32 clock_mhz = (clock_speed / (UInt32)1000000);
            return USB_CONTROL_MSG_OUT(USBREQ.SPI_INIT, null, (mode << 24) | clock_mhz);
        }

        public bool USB_SETUP_BULKOUT(USBREQ RQ, byte[] SETUP, byte[] BULK_OUT, UInt32 control_dt, int timeout = -1)
        {
            try
            {
                if (this.HasLogic)
                {
                    int ErrCounter = 0;
                    bool result = true;
                    do
                    {
                        result = true;
                        if (SETUP != null)
                            result = USB_CONTROL_MSG_OUT(USBREQ.LOAD_PAYLOAD, SETUP, (UInt32)(SETUP.Length));
                        if (result)
                            result = USB_CONTROL_MSG_OUT(RQ, null/* TODO Change to default(_) if this is not a reference type */, control_dt);
                        if (result)
                        {
                            if (BULK_OUT == null)
                                return true;
                            Thread.Sleep(2);
                            result = USB_BULK_OUT(BULK_OUT, timeout);
                        }

                        if (result)
                            return true;
                        if (!result)
                            ErrCounter += 1;
                        if (ErrCounter == 3)
                            return false;
                    }
                    while (true);   // Sends setup data// Sends setup command
                }
                else
                {
                    bool result = true == USB_CONTROL_MSG_OUT(RQ, SETUP, control_dt); // Sends setup command and data
                    if (!result)
                        return false;
                    if (BULK_OUT == null)
                        return true;
                    result = USB_BULK_OUT(BULK_OUT);
                    return result;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool USB_SETUP_BULKIN(USBREQ RQ, byte[] SETUP, ref byte[] DATA_IN, UInt32 control_dt, int timeout = -1) {
            try {
                bool result;
                if (this.HasLogic) {
                    int ErrCounter = 0;
                    do {
                        result = true;
                        if (SETUP != null) {
                            result = USB_CONTROL_MSG_OUT(USBREQ.LOAD_PAYLOAD, SETUP, (UInt32)SETUP.Length);
                        }
                        if (result) {
                            result = USB_CONTROL_MSG_OUT(RQ, null, control_dt);
                        }
                        if (result) {
                            result = USB_BULK_IN(ref DATA_IN, timeout);
                        }
                        if (!result)
                            ErrCounter += 1;
                        if (ErrCounter == 3)
                            return false;
                    } while (!result);
                    return true;
                } else {
                    result = USB_CONTROL_MSG_OUT(RQ, SETUP, control_dt); // Sends setup command and data
                    if (!result)
                        return false;
                    result = USB_BULK_IN(ref DATA_IN, timeout);
                    return result;
                }
            } catch {
                return false;
            }
        }
        // Sends a control message with an optional byte buffer to write
        public bool USB_CONTROL_MSG_OUT(USBREQ RQ, byte[] buffer_out = null, UInt32 data = 0) {
            try {
                if (USBHANDLE == null) { return false; }         
                UInt16 wValue = (UInt16)((data & 0xFFFF0000U) >> 16);
                UInt16 wIndex = (UInt16)(data & 0xFFFF);
                int bytes_out = 0;
                if (buffer_out != null) { bytes_out = buffer_out.Length; }
                UsbSetupPacket usbSetupPacket = new UsbSetupPacket((byte)this.USBFLAG_OUT, (byte)RQ, wValue, wIndex, bytes_out);
                int bytes_xfer = 0;
                bool result;
                if (buffer_out==null) {
                    result = USBHANDLE.ControlTransfer(ref usbSetupPacket, null, 0, out bytes_xfer);
                } else {
                    result = USBHANDLE.ControlTransfer(ref usbSetupPacket, buffer_out, buffer_out.Length, out bytes_xfer);
                }
                Thread.Sleep(4);
                return result;
            } catch {
                return false;
            }
        }
        // Sends a control message with a byte buffer to receive data
        public bool USB_CONTROL_MSG_IN(USBREQ RQ, ref byte[] Buffer_in, UInt32 data = 0) {
            try {
                if (USBHANDLE == null) { return false; }
                UInt16 wValue = (UInt16)((data & 0xFFFF0000U) >> 16);
                UInt16 wIndex = (UInt16)(data & 0xFFFF);
                UsbSetupPacket usb_setup = new UsbSetupPacket((byte)this.USBFLAG_IN, (byte)RQ, wValue, wIndex, (Int16)Buffer_in.Length);
                int bytes_xfer = 0;
                return USBHANDLE.ControlTransfer(ref usb_setup, Buffer_in, Buffer_in.Length, out bytes_xfer);
            } catch {
                return false;
            }
        }

        public bool USB_BULK_IN(ref byte[] buffer_in, int Timeout = -1) {
            try {
                if (Timeout == -1)
                    Timeout = USB_TIMEOUT_VALUE;
                if ((this.HasLogic)) {
                    int xfer = 0;
                    UsbEndpointReader ep_reader = USBHANDLE.OpenEndpointReader(ReadEndpointID.Ep01, buffer_in.Length, EndpointType.Bulk);
                    ErrorCode ec2 = ep_reader.Read(buffer_in, 0, Convert.ToInt32(buffer_in.Length), Timeout, out xfer); // 5 second timeout
                    if ((ec2 != ErrorCode.Success)) {
                        bool result = USB_CONTROL_MSG_OUT(USBREQ.ABORT);
                        ep_reader.Reset();
                        return false;
                    }
                    return true;
                } else {
                    int BytesRead = 0;
                    UsbEndpointReader reader = USBHANDLE.OpenEndpointReader(ReadEndpointID.Ep01, buffer_in.Length, EndpointType.Bulk);
                    ErrorCode ec = reader.Read(buffer_in, 0, buffer_in.Length, Timeout, out BytesRead);
                    if (ec == ErrorCode.Success)
                        return true;
                }
            } catch {
            }
            return false;
        }

        public bool USB_BULK_OUT(byte[] buffer_out, int Timeout = -1) {
            try {
                if (Timeout == -1)
                    Timeout = USB_TIMEOUT_VALUE;
                if ((this.HasLogic)) {
                    int xfer = 0;
                    UsbEndpointWriter ep_writer = USBHANDLE.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk);
                    ErrorCode ec2 = ep_writer.Write(buffer_out, 0, Convert.ToInt32(buffer_out.Length), Timeout, out xfer); // 5 second timeout
                    if ((ec2 != ErrorCode.Success)) {
                        bool result = USB_CONTROL_MSG_OUT(USBREQ.ABORT);
                        ep_writer.Reset();
                        return false;
                    }
                    return true;
                } else {
                    int BytesWritten = 0;
                    UsbEndpointWriter writer = USBHANDLE.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk);
                    ErrorCode ec = writer.Write(buffer_out, 0, buffer_out.Length, Timeout, out BytesWritten);
                    if (ec != ErrorCode.Success | BytesWritten != buffer_out.Length)
                        return false;
                    return true;
                }
            } catch {
                return false;
            }
        }

        public bool USB_WaitForComplete() {
            int timeout_counter = 0;
            byte task_id = 255;
            do
            {
                byte[] packet_out = new byte[1];
                Thread.Sleep(5);
                bool result = USB_CONTROL_MSG_IN(USBREQ.GET_TASK, ref packet_out);
                if (!result)
                    task_id = 1;
                else
                    task_id = packet_out[0];
                timeout_counter += 1;
                if ((timeout_counter == 1000))
                    return false;
            }
            while ((task_id > 0));
            return true;
        }

        public void USB_LEDOn() {
            try {
                if (HWBOARD == FCUSB_BOARD.ATMEL_DFU)
                    return; // Bootloader does not have LED
                USB_CONTROL_MSG_OUT(USBREQ.LEDON); // SPIREQ.LEDON
            } catch {
            }
        }

        public void USB_LEDOff() {
            try {
                if (HWBOARD == FCUSB_BOARD.ATMEL_DFU)
                    return; // Bootloader does not have LED
                USB_CONTROL_MSG_OUT(USBREQ.LEDOFF); // SPIREQ.LEDOFF
            } catch {
            }
        }

        public void USB_LEDBlink() {
            try {
                if (HWBOARD == FCUSB_BOARD.ATMEL_DFU)
                    return; // Bootloader does not have LED
                USB_CONTROL_MSG_OUT(USBREQ.LEDBLINK);
            } catch {
            }
        }

        public bool USB_Echo() {
            try {
                if ((USBHANDLE.UsbRegistryInfo.Pid == Shared.USB_PID_FCUSB_PRO) || (USBHANDLE.UsbRegistryInfo.Pid == Shared.USB_PID_FCUSB_MACH)) {
                    byte[] packet_out = new byte[4];
                    if (!USB_CONTROL_MSG_IN(USBREQ.ECHO, ref packet_out, 0x45434643U))
                        return false;
                    if (packet_out[0] != 0x45)
                        return false;
                    if (packet_out[1] != 0x43)
                        return false;
                    if (packet_out[2] != 0x46)
                        return false;
                    if (packet_out[3] != 0x43)
                        return false;
                } else {
                    byte[] packet_out = new byte[8];
                    UInt32 data_in = 0x12345678U;
                    if (!USB_CONTROL_MSG_IN(USBREQ.ECHO, ref packet_out, data_in))
                        return false; // SPIREQ.ECHO
                    if (packet_out[1] != Convert.ToByte(USBREQ.ECHO))
                        return false;
                    if (packet_out[2] != 0x34)
                        return false;
                    if (packet_out[3] != 0x12)
                        return false;
                    if (packet_out[4] != 0x78)
                        return false;
                    if (packet_out[5] != 0x56)
                        return false;
                    if (packet_out[6] != 0x8)
                        return false;
                    if (packet_out[7] != 0x0)
                        return false;
                }
                return true; // Echo successful
            } catch {
                return false;
            }
        }

        public void USB_VCC_OFF() {
            if (this.HasLogic)
            {
                USB_CONTROL_MSG_OUT(USBREQ.LOGIC_OFF);
                Thread.Sleep(100);
            }
        }

        public void USB_VCC_ON(Voltage vcc_level = Voltage.V3_3) {
            if (this.HasLogic) {
                USB_CONTROL_MSG_OUT(USBREQ.LOGIC_OFF);
                Thread.Sleep(250);
                if ((vcc_level == Voltage.V1_8))
                    USB_CONTROL_MSG_OUT(USBREQ.LOGIC_1V8);
                else
                    USB_CONTROL_MSG_OUT(USBREQ.LOGIC_3V3);
                Thread.Sleep(100);
            }
        }

        public bool BOOTLOADER { get; set; } = false;

        public bool LoadFirmwareVersion() {
            try {
                this.BOOTLOADER = false;
                if (USBHANDLE.UsbRegistryInfo.Vid == Shared.USB_VID_ATMEL) {
                    this.HWBOARD = FCUSB_BOARD.ATMEL_DFU;
                    this.FW_VERSION = "1.00";
                    this.BOOTLOADER = true;
                } else if (USBHANDLE.UsbRegistryInfo.Pid == Shared.USB_PID_FCUSB_PRO) {
                    byte[] b = new byte[4];
                    if (!USB_CONTROL_MSG_IN(USBREQ.VERSION, ref b))
                        return false;
                    char[] array = new char[4];
                    array[0] = Convert.ToChar(b[1]);
                    array[1] = '.';
                    array[2] = Convert.ToChar(b[2]);
                    array[3] = Convert.ToChar(b[3]);
                    this.FW_VERSION = new string(array);
                    if ((Convert.ToChar(b[0]) == 'B'))
                    {
                        this.BOOTLOADER = true;
                        if (Convert.ToChar(b[1]) == '5')
                            this.HWBOARD = FCUSB_BOARD.Professional_PCB5;
                        else
                            this.HWBOARD = FCUSB_BOARD.NotSupported;
                    }
                    else if (Convert.ToChar(b[0]) == 'P')
                        this.HWBOARD = FCUSB_BOARD.NotSupported;
                    else if (Convert.ToChar(b[0]) == 'T')
                        this.HWBOARD = FCUSB_BOARD.Professional_PCB5;
                } else if (USBHANDLE.UsbRegistryInfo.Pid == Shared.USB_PID_FCUSB_MACH) {
                    byte[] b = new byte[4];
                    if (!USB_CONTROL_MSG_IN(USBREQ.VERSION, ref b))
                        return false;
                    if (b[0] == (byte)'B')
                        this.BOOTLOADER = true;
                    char[] array = new char[4];
                    array[0] = Convert.ToChar(b[1]);
                    array[1] = '.';
                    array[2] = Convert.ToChar(b[2]);
                    array[3] = Convert.ToChar(b[3]);
                    this.FW_VERSION = new string(array);
                    this.HWBOARD = FCUSB_BOARD.Mach1;
                    return true;
                } else {
                    byte[] buff = new byte[4];
                    byte[] data_out = new byte[4];
                    if (!USB_CONTROL_MSG_IN(USBREQ.VERSION, ref buff))
                        return false;
                    char hw_char = Convert.ToChar(buff[0]);
                    if (hw_char == 'C')
                        this.HWBOARD = FCUSB_BOARD.Classic;
                    else if (hw_char == 'E')
                        this.HWBOARD = FCUSB_BOARD.NotSupported;
                    else if (hw_char == 'X')
                        this.HWBOARD = FCUSB_BOARD.XPORT_PCB2;
                    else if (hw_char == '0')
                        this.HWBOARD = FCUSB_BOARD.Classic;
                    data_out[3] = buff[3];
                    data_out[2] = buff[2];
                    data_out[1] = (byte)'.';
                    data_out[0] = buff[1];
                    string fwstr = System.Text.Encoding.UTF8.GetString(data_out);
                    if (fwstr.StartsWith("0"))
                        fwstr = fwstr.Substring(1);
                    this.FW_VERSION = fwstr;
                    return true;
                }
            } catch {
                return false;
            }
            return true;
        }

        public bool FirmwareUpdate(byte[] new_fw, float fw_version) {
            try
            {
                if (!this.BOOTLOADER)
                    return false;
                bool result = USB_CONTROL_MSG_OUT(USBREQ.FW_UPDATE, null, (UInt32)new_fw.Length);
                if (!result)
                    return false;
                Int32 bytes_left = new_fw.Length;
                int ptr = 0;
                UpdateProgress?.Invoke(0, this);
                while ((bytes_left > 0))
                {
                    int count = bytes_left;
                    if ((count > 4096))
                        count = 4096;
                    byte[] buffer = new byte[count - 1 + 1];
                    Array.Copy(new_fw, ptr, buffer, 0, buffer.Length);
                    result = USB_BULK_OUT(buffer);
                    if (!result)
                        return false;
                    ptr += count;
                    bytes_left -= count;
                    int p = (int)Math.Floor((ptr / (double)new_fw.Length) * 100);
                    UpdateProgress?.Invoke(p, this);
                    Thread.Sleep(100);
                }
                UInt32 fw_ver_data = 0xFC000000U | ((UInt32)Math.Floor(fw_version) << 8) | ((UInt32)(fw_version * 100) & 255);
                USB_CONTROL_MSG_OUT(USBREQ.FW_REBOOT, null, fw_ver_data);
                UpdateProgress?.Invoke(100, this);
                return true;
            } catch {
                return false;
            }
        }

        public bool BootloaderUpdate(byte[] bl_data) {
            try
            {
                if (this.BOOTLOADER)
                    return false; // Can only update bootloader when running in APP mode
                bool result = USB_CONTROL_MSG_OUT(USBREQ.FW_UPDATE, null, (UInt32)bl_data.Length);
                if (!result)
                    return false;
                Int32 bytes_left = bl_data.Length;
                int ptr = 0;
                UpdateProgress?.Invoke(0, this);
                while ((bytes_left > 0))
                {
                    int count = bytes_left;
                    if ((count > 2048))
                        count = 2048;
                    byte[] buffer = new byte[count - 1 + 1];
                    Array.Copy(bl_data, ptr, buffer, 0, buffer.Length);
                    result = USB_BULK_OUT(buffer);
                    if (!result)
                        return false;
                    ptr += count;
                    bytes_left -= count;
                    int p = (int)Math.Floor((ptr / (double)bl_data.Length) * 100);
                    UpdateProgress?.Invoke(p, this);
                    Thread.Sleep(100);
                }
                return true;
            } catch {
                return false;
            }
        }

        public UInt32 LOGIC_GetVersion() {
            byte[] cpld_data = new byte[4];
            USB_CONTROL_MSG_IN(USBREQ.LOGIC_VERSION_GET, ref cpld_data);
            Array.Reverse(cpld_data);
            UInt32 result = Utilities.Bytes.ToUInt32(cpld_data);
            return result;
        }

        public bool LOGIC_SetVersion(UInt32 new_ver) {
            bool result = USB_CONTROL_MSG_OUT(USBREQ.LOGIC_VERSION_SET, null/* TODO Change to default(_) if this is not a reference type */, new_ver);
            return result;
        }

    }

    public enum USBREQ : byte {
        JTAG_DETECT = 0x10,
        JTAG_RESET = 0x11,
        JTAG_SELECT = 0x12,
        JTAG_READ_B = 0x13,
        JTAG_READ_H = 0x14,
        JTAG_READ_W = 0x15,
        JTAG_WRITE = 0x16,
        JTAG_READMEM = 0x17,
        JTAG_WRITEMEM = 0x18,
        JTAG_ARM_INIT = 0x19,
        JTAG_INIT = 0x1A,
        JTAG_FLASHSPI_BRCM = 0x1B,
        JTAG_FLASHSPI_ATH = 0x1,
        JTAG_FLASHWRITE_I16 = 0x1D,
        JTAG_FLASHWRITE_A16 = 0x1E,
        JTAG_FLASHWRITE_SST = 0x1F,
        JTAG_FLASHWRITE_AMDNB = 0x20,
        JTAG_SCAN = 0x21,
        JTAG_TOGGLE = 0x22,
        JTAG_GOTO_STATE = 0x23,
        JTAG_SET_OPTION = 0x24,
        JTAG_REGISTERS = 0x25,
        JTAG_SHIFT_DATA = 0x27,
        JTAG_BDR_SETUP = 0x28,
        JTAG_BDR_INIT = 0x29,
        JTAG_BDR_ADDPIN = 0x2A,
        JTAG_BDR_WRCMD = 0x2B,
        JTAG_BDR_WRMEM = 0x2,
        JTAG_BDR_RDMEM = 0x2D,
        JTAG_BDR_RDFLASH = 0x2E,
        JTAG_BDR_WRFLASH = 0x2F,
        JTAG_BDR_SETBSR = 0x30,
        JTAG_BDR_WRITEBSR = 0x31,
        JTAG_SHIFT_IR = 0x32,
        JTAG_SHIFT_DR = 0x33,
        SPI_INIT = 0x40,
        SPI_SS_ENABLE = 0x41,
        SPI_SS_DISABLE = 0x42,
        SPI_PROG = 0x43,
        SPI_RD_DATA = 0x44,
        SPI_WR_DATA = 0x45,
        SPI_READFLASH = 0x46,
        SPI_WRITEFLASH = 0x47,
        SPI_WRITEDATA_AAI = 0x48,
        S93_INIT = 0x49,
        S93_READEEPROM = 0x4A,
        S93_WRITEEEPROM = 0x4B,
        S93_ERASE = 0x4,
        SQI_SETUP = 0x50,
        SQI_SS_ENABLE = 0x51,
        SQI_SS_DISABLE = 0x52,
        SQI_RD_DATA = 0x53,
        SQI_WR_DATA = 0x54,
        SQI_RD_FLASH = 0x55,
        SQI_WR_FLASH = 0x56,
        SPINAND_READFLASH = 0x5B,
        SPINAND_WRITEFLASH = 0x5,
        I2C_INIT = 0x60,
        I2C_READEEPROM = 0x61,
        I2C_WRITEEEPROM = 0x62,
        I2C_RESULT = 0x63,
        EXPIO_INIT = 0x64,
        EXPIO_ADDRESS = 0x65,
        EXPIO_WRITEDATA = 0x66,
        EXPIO_READDATA = 0x67,
        EXPIO_RDID = 0x68,
        EXPIO_CHIPERASE = 0x69,
        EXPIO_SECTORERASE = 0x6A,
        EXPIO_WRITEPAGE = 0x6B,
        EXPIO_ADDRESS_CE = 0x6F,
        EXPIO_MODE_READ = 0x73,
        EXPIO_MODE_WRITE = 0x74,
        EXPIO_MODE_DELAY = 0x75,
        EXPIO_CTRL = 0x76,
        EXPIO_DELAY = 0x78,
        EXPIO_WRCMDDATA = 0x7A,
        EXPIO_WRMEMDATA = 0x7B,
        EXPIO_RDMEMDATA = 0x7,
        EXPIO_WAIT = 0x7D,
        EXPIO_CPEN = 0x7E,
        EXPIO_SR = 0x7F,
        EXPIO_TIMING = 0x5E,
        VERSION = 0x80,
        ECHO = 0x81,
        LEDON = 0x82,
        LEDOFF = 0x83,
        LEDBLINK = 0x84,
        START_SENDING_FIRM = 0x85,
        SEND_FIRM_SIZE = 0x86,
        SEND_FIRM_DATA = 0x87,
        STOP_SEND_FIRM_DATA = 0x88,
        ABORT = 0x89,
        VCC_1V8 = 0x8A,
        VCC_3V = 0x8B,
        VCC_5V = 0x8,
        VCC_ON = 0x8D,
        VCC_OFF = 0x8E,
        GET_TASK = 0x8F,
        LOAD_PAYLOAD = 0x90,
        READ_PAYLOAD = 0x91,
        FW_UPDATE = 0x94,
        FW_REBOOT = 0x97,
        TEST_READ = 0xA1,
        TEST_WRITE = 0xA2,
        SWI_DETECT = 0xB0,
        SWI_READ = 0xB1,
        SWI_WRITE = 0xB2,
        SWI_RD_REG = 0xB3,
        SWI_WR_REG = 0xB4,
        SWI_LOCK_REG = 0xB5,
        PULSE_RESET = 0xB6,
        LOGIC_STATUS = 0xC0,
        LOGIC_OFF = 0xC1,
        LOGIC_1V8 = 0xC2,
        LOGIC_3V3 = 0xC3,
        LOGIC_VERSION_GET = 0xC4,
        LOGIC_VERSION_SET = 0xC5,
        SPI_REPEAT = 0xC6,
        EPROM_RESULT = 0xC7,
        LOGIC_START = 0xC8,
        NAND_ONFI = 0xD0,
        NAND_SR = 0xD1,
        NAND_SETTYPE = 0xD2
    }

    public enum FCUSB_HW_CTRL : byte {
        WE_HIGH = 1,
        WE_LOW = 2,
        OE_HIGH = 3,
        OE_LOW = 4,
        CE_HIGH = 5,
        CE_LOW = 6,
        VPP_0V = 7,
        VPP_5V = 8,
        VPP_12V = 9,
        RELAY_ON = 10, // PE7=HIGH
        RELAY_OFF = 11, // PE7=LOW
        VPP_DISABLE = 12, // CLE_LOW
        VPP_ENABLE = 13, // CLE_HIGH
        CLE_HIGH = 14,
        CLE_LOW = 15,
        ALE_HIGH = 16,
        ALE_LOW = 17,
        RB0_HIGH = 18,
        RB0_LOW = 19,
        BYTE_HIGH = 20,
        BYTE_LOW = 21
    }

    public enum DeviceStatus {
        ExtIoNotConnected = 0,
        NotDetected = 1,
        Supported = 2,
        NotSupported = 3,
        NotCompatible = 4
    }

    public enum FCUSB_BOARD {
        NotSupported,
        ATMEL_DFU,
        Classic,
        XPORT_PCB2,
        Professional_PCB5,
        Mach1,
    }

}

