Imports System.Threading
Imports FlashcatUSB.SPI
Imports LibUsbDotNet
Imports LibUsbDotNet.Main

Namespace USB

    Friend Module SharedCommon
        Friend Const DEFAULT_TIMEOUT As Integer = 5000
        Friend Const USB_VID_ATMEL As UInt16 = &H3EB
        Friend Const USB_VID_EC As UInt16 = &H16C0
        Friend Const USB_PID_AT90USB646 As UInt16 = &H2FF9 'FCUSB XPORT
        Friend Const USB_PID_ATMEGA32U2 As UInt16 = &H2FF0 'FCUSB PCB 2.1-2.2
        Friend Const USB_PID_ATMEGA32U4 As UInt16 = &H2FF4 'FCUSB PCB 2.3
        Friend Const USB_PID_FCUSB_PRO As UInt16 = &H5E0
        Friend Const USB_PID_FCUSB_MACH As UInt16 = &H5E1
        Friend Const USB_PID_FCUSB As Integer = &H5DE 'Classic and XPORT
        Friend Const USB_OUT_DELAY As Integer = 5

        Friend Function GetDevicePath(device As UsbRegistry) As String
            Try
                Dim dev_loc As String = "USB\VID_" & Hex(device.Vid).PadLeft(4, "0"c) & "&PID_" & Hex(device.Pid).PadLeft(4, "0"c)
                If device.GetType Is GetType(LibUsb.LibUsbRegistry) Then
                    dev_loc &= "\" & CStr(device.DeviceProperties("LocationInformation"))
                ElseIf device.GetType Is GetType(LegacyUsbRegistry) Then
                    Dim legacy As LegacyUsbRegistry = DirectCast(device, LegacyUsbRegistry)
                    Dim DeviceFilename As String = DirectCast(legacy.Device, LibUsb.LibUsbDevice).DeviceFilename
                    dev_loc &= DeviceFilename
                End If
                Return dev_loc
            Catch ex As Exception
                Return String.Empty
            End Try
        End Function

        Public Sub SetDeviceVoltage(usb_dev As FCUSB_DEVICE, TargetVoltage As Voltage)
            Dim console_message As String
            If (TargetVoltage = Voltage.V1_8) Then
                console_message = String.Format(RM.GetString("voltage_set_to"), "1.8V")
                usb_dev.USB_VCC_ON(Voltage.V1_8)
            Else
                console_message = String.Format(RM.GetString("voltage_set_to"), "3.3V")
                usb_dev.USB_VCC_ON(Voltage.V3_3)
            End If
            PrintConsole(console_message)
            Utilities.Sleep(200)
        End Sub

    End Module

    Public Class HostClient
        Public Event DeviceConnected(usb_dev As FCUSB_DEVICE)
        Public Event DeviceDisconnected(usb_dev As FCUSB_DEVICE)

        Private ConnectedDevices As New List(Of FCUSB_DEVICE) 'Contains all devices that are connected
        Private DEBUG_MODE As Boolean

        Public Property IsRunning As Boolean = False
        Public Property CloseService As Boolean = False

        Sub New(Optional debug_mode As Boolean = False)
            Me.DEBUG_MODE = debug_mode
        End Sub

        Public Sub StartService()
            Try
                Dim td As New Thread(AddressOf ConnectionThread)
                td.Name = "tdUsbMonitor"
                td.Start()
            Catch ex As Exception
            End Try
        End Sub

        Private Sub ConnectionThread()
            Try : Me.IsRunning = True
                Do While (Not Me.CloseService)
                    CheckConnectedDevices()
                    If (Not Me.CloseService) Then
                        Service_ConnectToDevices()
                        Thread.Sleep(150)
                    End If
                Loop
                DisconnectAll()
            Catch ex As Exception
            Finally
                Me.IsRunning = False
            End Try
        End Sub

        Private Sub Service_ConnectToDevices()
            Try
                Dim fcusb_list() As UsbRegistry = GetUsbDevices() 'Returns a list of all FCUSB devices connected
                If fcusb_list IsNot Nothing AndAlso fcusb_list.Length > 0 Then
                    For Each this_fcusb In fcusb_list
                        Dim fcusb_path As String = GetDevicePath(this_fcusb)
                        If Not DevicePathIsConnected(fcusb_path) Then 'This device is not in our list
                            Dim this_dev As UsbDevice = this_fcusb.Device
                            If this_dev Is Nothing Then Exit For
                            Dim new_fcusb_dev As FCUSB_DEVICE = Nothing
                            If Connect(this_dev, new_fcusb_dev) Then
                                ConnectedDevices.Add(new_fcusb_dev)
                                AddHandler new_fcusb_dev.OnDisconnected, AddressOf FCUSB_Device_Disconnected
                                RaiseEvent DeviceConnected(new_fcusb_dev)
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
            End Try
        End Sub

        Private Sub CheckConnectedDevices()
            Dim disconnected_dev As New List(Of FCUSB_DEVICE)
            For Each dev In ConnectedDevices
                If dev.IS_CONNECTED Then
                    Dim is_online As Boolean = dev.CheckConnection()
                    If Not is_online Then disconnected_dev.Add(dev)
                End If
            Next
            For Each dev In disconnected_dev
                dev.Disconnect()
            Next
        End Sub

        Private Sub FCUSB_Device_Disconnected(usb_dev As FCUSB_DEVICE)
            Try
                ConnectedDevices.Remove(usb_dev)
            Catch ex As Exception
            End Try
            RaiseEvent DeviceDisconnected(usb_dev)
        End Sub

        Private Function DevicePathIsConnected(path As String) As Boolean
            Try
                For Each dev In ConnectedDevices
                    If dev.USB_PATH.Equals(path) Then Return True
                Next
            Catch ex As Exception
            End Try
            Return False 'Not found
        End Function

        Public Function Connect(usb_device As UsbDevice, ByRef fcusb_dev As FCUSB_DEVICE) As Boolean
            Try
                If OpenUsbDevice(usb_device) Then
                    Dim new_device As New FCUSB_DEVICE(usb_device, Me.DEBUG_MODE)
                    If (usb_device.UsbRegistryInfo.Vid = USB_VID_ATMEL) Then 'DFU Mode
                        new_device.IS_CONNECTED = True
                        new_device.HWBOARD = FCUSB_BOARD.ATMEL_DFU
                        fcusb_dev = new_device
                        Return True
                    Else
                        If new_device.USB_Echo() Then
                            new_device.IS_CONNECTED = True
                            Dim Success As Boolean = new_device.LoadFirmwareVersion()
                            If Not Success Then Return False
                            new_device.USB_LEDOn() 'Call after firmware version is loaded
                            fcusb_dev = new_device
                            Return True
                        End If
                    End If
                    Return False
                End If
            Catch ex As Exception
            End Try
            Return False
        End Function

        Private Function OpenUsbDevice(usb_dev As UsbDevice) As Boolean
            Try
                Dim wholeUsbDevice As IUsbDevice = TryCast(usb_dev, IUsbDevice)
                If wholeUsbDevice IsNot Nothing Then 'Libusb only
                    wholeUsbDevice.SetConfiguration(1)
                    wholeUsbDevice.ClaimInterface(0)
                    Try
                        wholeUsbDevice.SetAltInterface(1)
                    Catch ex As Exception
                    End Try
                End If
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function GetUsbDevices() As UsbRegistry()
            Try
                Dim devices As New List(Of UsbRegistry)
                AddDevicesToList(USB_VID_ATMEL, USB_PID_ATMEGA32U2, devices)
                AddDevicesToList(USB_VID_ATMEL, USB_PID_AT90USB646, devices)
                AddDevicesToList(USB_VID_ATMEL, USB_PID_ATMEGA32U4, devices)
                AddDevicesToList(USB_VID_EC, USB_PID_FCUSB, devices)
                AddDevicesToList(USB_VID_EC, USB_PID_FCUSB_PRO, devices)
                AddDevicesToList(USB_VID_EC, USB_PID_FCUSB_MACH, devices)
                If devices.Count = 0 Then Return Nothing
                Return devices.ToArray()
            Catch ex As Exception
            End Try
            Return Nothing
        End Function

        Private Sub AddDevicesToList(VID As UInt16, PID As UInt16, DeviceList As List(Of UsbRegistry))
            Dim fcusb_usb_device As New UsbDeviceFinder(VID, PID)
            Dim fcusb_list As UsbRegDeviceList = UsbDevice.AllDevices.FindAll(fcusb_usb_device)
            If fcusb_list IsNot Nothing AndAlso (fcusb_list.Count > 0) Then
                For i = 0 To fcusb_list.Count - 1
                    If fcusb_list(i).GetType IsNot GetType(WinUsb.WinUsbRegistry) Then
                        DeviceList.Add(fcusb_list(i))
                    End If
                Next
            End If
        End Sub

        Public Function GetConnectedPaths() As String()
            Try
                Dim paths As New List(Of String)
                Dim cnt_devices() As UsbRegistry = GetUsbDevices()
                If cnt_devices IsNot Nothing AndAlso cnt_devices.Count > 0 Then
                    For i = 0 To cnt_devices.Count - 1
                        Dim u As UsbRegistry = cnt_devices(i)
                        Dim o As String = GetDevicePath(u)
                        paths.Add(o)
                    Next
                End If
                Return paths.ToArray
            Catch ex As Exception
                Return Nothing
            End Try
        End Function

        Public Sub DisconnectAll()
            Try
                Dim dev_list As New List(Of FCUSB_DEVICE)
                For Each fc_devs In Me.ConnectedDevices
                    dev_list.Add(fc_devs)
                Next
                For Each fc_devs In dev_list
                    fc_devs.Disconnect()
                Next
            Catch ex As Exception
            End Try
        End Sub

    End Class

    Public Class FCUSB_DEVICE
        Public USBHANDLE As UsbDevice = Nothing
        Public Property HWBOARD As FCUSB_BOARD
        Public ReadOnly Property USB_PATH As String
        Public Property IS_CONNECTED As Boolean = False
        Public Property FW_VERSION As String = ""
        Public Property USBFLAG_OUT As UsbCtrlFlags
        Public Property USBFLAG_IN As UsbCtrlFlags

        Public JTAG_IF As New JTAG.JTAG_IF(Me)

        Public ReadOnly Property DEBUG_MODE As Boolean = False

        Private ReadOnly Property USB_TIMEOUT_VALUE As Integer = DEFAULT_TIMEOUT

        Public Event OnDisconnected(this_dev As FCUSB_DEVICE)
        Public Event OnUpdateProgress(device As FCUSB_DEVICE, percent As Integer)
        Public Event OnPrintConsole(device As FCUSB_DEVICE, msg As String)

        Public ReadOnly Property HasLogic() As Boolean
            Get
                If Me.HWBOARD = FCUSB_BOARD.Professional_PCB5 Then
                    Return True
                ElseIf Me.HWBOARD = FCUSB_BOARD.Mach1 Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        Sub New(my_handle As UsbDevice, debug_mode As Boolean)
            Me.USBHANDLE = my_handle
            Me.USB_PATH = GetDevicePath(my_handle.UsbRegistryInfo)
            Me.DEBUG_MODE = debug_mode
            If (Me.HasLogic) Then
                Me.USBFLAG_OUT = (UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Interface Or UsbCtrlFlags.Direction_Out)
                Me.USBFLAG_IN = (UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Interface Or UsbCtrlFlags.Direction_In)
            Else
                Me.USBFLAG_OUT = (UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.Direction_Out)
                Me.USBFLAG_IN = (UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.Direction_In)
            End If
            If Me.DEBUG_MODE Then USB_TIMEOUT_VALUE = 5000000
        End Sub

        Public Function CheckConnection() As Boolean
            If USBHANDLE Is Nothing Then Return False
            If Not USBHANDLE.UsbRegistryInfo.IsAlive Then Return False
            Return True
        End Function

        Public Sub Disconnect()
            Try
                If Not Me.IS_CONNECTED Then Exit Sub
                Me.IS_CONNECTED = False
                If Me.USBHANDLE IsNot Nothing Then
                    Me.USB_LEDOff()
                    Dim wholeUsbDevice As IUsbDevice = TryCast(Me.USBHANDLE, IUsbDevice)
                    If wholeUsbDevice IsNot Nothing Then 'Libusb only
                        wholeUsbDevice.ReleaseInterface(0)
                    End If
                    Me.USBHANDLE.Close()
                    Me.USBHANDLE = Nothing
                End If
            Catch ex As Exception
            End Try
            RaiseEvent OnDisconnected(Me)
        End Sub

        Private Sub PrintConsole(msg As String)
            RaiseEvent OnPrintConsole(Me, msg)
        End Sub

#Region "USB Control"

        Public Function USB_SPI_INIT(mode As UInt32, clock_speed As UInt32) As Boolean
            Dim clock_mhz As UInt32 = (clock_speed \ 1000000UI)
            Return USB_CONTROL_MSG_OUT(USBREQ.SPI_INIT, Nothing, (mode << 16) Or clock_mhz)
        End Function

        Public Function USB_LOADPAYLOAD(PayloadData() As Byte) As Boolean
            Return USB_CONTROL_MSG_OUT(USBREQ.LOAD_PAYLOAD, PayloadData, CUInt(PayloadData.Length))
        End Function

        Public Function USB_SETUP_BULKOUT(RQ As USBREQ, SETUP() As Byte, BULK_OUT() As Byte, control_dt As UInt32, Optional timeout As Integer = -1) As Boolean
            Dim result As Boolean = True = USB_CONTROL_MSG_OUT(RQ, SETUP, control_dt) 'Sends setup command and data
            If Not result Then Return False
            If BULK_OUT Is Nothing Then Return True
            result = USB_BULK_OUT(BULK_OUT)
            Return result
        End Function

        Public Function USB_SETUP_BULKIN(RQ As USBREQ, SETUP() As Byte, ByRef DATA_IN() As Byte, control_dt As UInt32, Optional timeout As Integer = -1) As Boolean
            Dim result As Boolean = USB_CONTROL_MSG_OUT(RQ, SETUP, control_dt) 'Sends setup command and data
            If Not result Then Return False
            result = USB_BULK_IN(DATA_IN, timeout)
            Return result
        End Function
        'Sends a control message with an optional byte buffer to write
        Public Function USB_CONTROL_MSG_OUT(RQ As USBREQ, Optional buffer_out() As Byte = Nothing, Optional data As UInt32 = 0) As Boolean
            Try
                If USBHANDLE Is Nothing Then Return False
                Dim result As Boolean
                Dim wValue As UInt16 = CUShort((data And &HFFFF0000UI) >> 16)
                Dim wIndex As UInt16 = CUShort(data And &HFFFF)
                Dim bytes_out As Integer = 0
                If buffer_out IsNot Nothing Then bytes_out = buffer_out.Length
                Dim usbSetupPacket As New UsbSetupPacket(Me.USBFLAG_OUT, RQ, wValue, wIndex, CShort(bytes_out))
                Dim bytes_xfer As Integer = 0
                Dim buffer_len As Integer = 0
                If buffer_out IsNot Nothing Then buffer_len = buffer_out.Length
                result = USBHANDLE.ControlTransfer(usbSetupPacket, buffer_out, buffer_len, bytes_xfer)
                Utilities.Sleep(USB_OUT_DELAY)
                Return result
            Catch ex As Exception
                Return False
            End Try
        End Function
        'Sends a control message with a byte buffer to receive data
        Public Function USB_CONTROL_MSG_IN(RQ As USBREQ, ByRef Buffer_in() As Byte, Optional data As UInt32 = 0) As Boolean
            Try
                If USBHANDLE Is Nothing Then Return False
                Dim bytes_xfer As Integer = 0
                Dim wValue As UInt16 = CUShort((data And &HFFFF0000UI) >> 16)
                Dim wIndex As UInt16 = CUShort(data And &HFFFF)
                Dim usb_setup As New UsbSetupPacket(Me.USBFLAG_IN, RQ, wValue, wIndex, CShort(Buffer_in.Length))
                Dim result As Boolean = USBHANDLE.ControlTransfer(usb_setup, Buffer_in, Buffer_in.Length, bytes_xfer)
                If Not result Then Return False
                If Not Buffer_in.Length = bytes_xfer Then Return False
                Return True 'No error
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function USB_BULK_IN(ByRef buffer_in() As Byte, Optional Timeout As Integer = -1) As Boolean
            Try
                If Timeout = -1 Then Timeout = USB_TIMEOUT_VALUE
                If (Me.HasLogic()) Then
                    Dim xfer As Integer = 0
                    Using ep_reader As UsbEndpointReader = USBHANDLE.OpenEndpointReader(ReadEndpointID.Ep01, buffer_in.Length, EndpointType.Bulk)
                        Dim ec2 As ErrorCode = ep_reader.Read(buffer_in, 0, CInt(buffer_in.Length), Timeout, xfer) '5 second timeout
                        If ec2 = ErrorCode.IoCancelled Then Return False
                        If (Not ec2 = ErrorCode.None) Then
                            Dim result As Boolean = USB_CONTROL_MSG_OUT(USBREQ.ABORT)
                            ep_reader.Reset()
                            Return False
                        End If
                        Return True
                    End Using
                Else
                    Dim BytesRead As Integer = 0
                    Dim reader As UsbEndpointReader = USBHANDLE.OpenEndpointReader(ReadEndpointID.Ep01, buffer_in.Length, EndpointType.Bulk)
                    Dim ec As ErrorCode = reader.Read(buffer_in, 0, buffer_in.Length, Timeout, BytesRead)
                    If ec = ErrorCode.None Then Return True
                End If
            Catch ex As Exception
            End Try
            Return False
        End Function

        Public Function USB_BULK_OUT(buffer_out() As Byte, Optional Timeout As Integer = -1) As Boolean
            Try
                If Timeout = -1 Then Timeout = USB_TIMEOUT_VALUE
                If (Me.HasLogic()) Then
                    Dim xfer As Integer = 0
                    Using ep_writer As UsbEndpointWriter = USBHANDLE.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
                        Dim ec2 As ErrorCode = ep_writer.Write(buffer_out, 0, CInt(buffer_out.Length), Timeout, xfer) '5 second timeout
                        If (Not ec2 = ErrorCode.None) Then
                            Dim result As Boolean = USB_CONTROL_MSG_OUT(USBREQ.ABORT)
                            ep_writer.Reset()
                            Return False
                        End If
                        Return True
                    End Using
                Else
                    Dim BytesWritten As Integer = 0
                    Dim writer As UsbEndpointWriter = USBHANDLE.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
                    Dim ec As ErrorCode = writer.Write(buffer_out, 0, buffer_out.Length, Timeout, BytesWritten)
                    If Not ec = ErrorCode.None Or Not BytesWritten = buffer_out.Length Then Return False
                    Return True
                End If
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function USB_WaitForComplete() As Boolean
            Dim timeout_counter As Integer
            Dim task_id As Byte = 255
            Do
                Dim packet_out(0) As Byte
                Utilities.Sleep(5) 'Prevents slamming the USB port
                If Not USB_CONTROL_MSG_IN(USBREQ.GET_TASK, packet_out) Then Return False
                task_id = packet_out(0)
                timeout_counter += 1
                If (timeout_counter = 1000) Then Return False 'Changed from 500
            Loop While (task_id > 0)
            Return True
        End Function

#End Region

#Region "LED, VCC, and ECHO"
        Public Property CurrentVCC As Voltage = Voltage.OFF

        Public Sub USB_LEDOn()
            Try
                If HWBOARD = FCUSB_BOARD.ATMEL_DFU Then Exit Sub 'Bootloader does not have LED
                USB_CONTROL_MSG_OUT(USBREQ.LEDON) 'SPIREQ.LEDON
            Catch ex As Exception
            End Try
        End Sub

        Public Sub USB_LEDOff()
            Try
                If HWBOARD = FCUSB_BOARD.ATMEL_DFU Then Exit Sub 'Bootloader does not have LED
                USB_CONTROL_MSG_OUT(USBREQ.LEDOFF) 'SPIREQ.LEDOFF
            Catch ex As Exception
            End Try
        End Sub

        Public Sub USB_LEDBlink()
            Try
                If HWBOARD = FCUSB_BOARD.ATMEL_DFU Then Exit Sub 'Bootloader does not have LED
                USB_CONTROL_MSG_OUT(USBREQ.LEDBLINK)
            Catch ex As Exception
            End Try
        End Sub

        Public Function USB_Echo() As Boolean
            Try
                Dim packet_out(3) As Byte
                If Not USB_CONTROL_MSG_IN(USBREQ.ECHO, packet_out, &H454D4243UI) Then Return False
                If Not packet_out(0) = &H45 Then Return False
                If Not packet_out(1) = &H4D Then Return False
                If Not packet_out(2) = &H42 Then Return False
                If Not packet_out(3) = &H43 Then Return False
                Return True 'Echo successful
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Sub USB_VCC_OFF()
            If (Me.HasLogic()) Then
                USB_CONTROL_MSG_OUT(USBREQ.LOGIC_OFF)
                Me.CurrentVCC = Voltage.OFF
                Utilities.Sleep(100)
            End If
        End Sub

        Public Sub USB_VCC_ON(vcc_level As Voltage)
            If (Me.HasLogic()) Then
                USB_CONTROL_MSG_OUT(USBREQ.LOGIC_OFF)
                Utilities.Sleep(250)
                If (vcc_level = Voltage.V1_8) Then
                    USB_CONTROL_MSG_OUT(USBREQ.LOGIC_1V8)
                Else
                    USB_CONTROL_MSG_OUT(USBREQ.LOGIC_3V3)
                End If
                Me.CurrentVCC = vcc_level
                Utilities.Sleep(100)
            End If
        End Sub

#End Region

        Public Property BOOTLOADER As Boolean = False

        Public Function LoadFirmwareVersion() As Boolean
            Try
                Me.BOOTLOADER = False
                If (USBHANDLE.UsbRegistryInfo.Vid = USB_VID_ATMEL) Then
                    Me.HWBOARD = FCUSB_BOARD.ATMEL_DFU
                    Me.FW_VERSION = "1.00"
                    Me.BOOTLOADER = True
                ElseIf USBHANDLE.UsbRegistryInfo.Pid = USB_PID_FCUSB_PRO Then
                    Dim b(3) As Byte
                    If Not USB_CONTROL_MSG_IN(USBREQ.VERSION, b) Then Return False
                    Me.FW_VERSION = Text.Encoding.UTF8.GetString({b(1), Asc("."c), b(2), b(3)})

                    If (b(0) = Asc("B")) Then
                        Me.BOOTLOADER = True
                        If (b(1) = Asc("5")) Then 'PCB 5.0
                            Me.HWBOARD = FCUSB_BOARD.Professional_PCB5
                        Else 'Professional PCB 4.0
                            Me.HWBOARD = FCUSB_BOARD.NotSupported
                        End If
                    ElseIf (b(0) = Asc("P")) Then ' Professional_PCB4
                        Me.HWBOARD = FCUSB_BOARD.NotSupported
                    ElseIf (b(0) = Asc("T")) Then
                        Me.HWBOARD = FCUSB_BOARD.Professional_PCB5
                    End If
                ElseIf USBHANDLE.UsbRegistryInfo.Pid = USB_PID_FCUSB_MACH Then
                    Dim b(3) As Byte
                    If Not USB_CONTROL_MSG_IN(USBREQ.VERSION, b) Then Return False
                    If (b(0) = Asc("B")) Then
                        Me.BOOTLOADER = True
                    End If
                    Me.FW_VERSION = Text.Encoding.UTF8.GetString({b(1), Asc("."), b(2), b(3)})
                    Me.HWBOARD = FCUSB_BOARD.Mach1
                    Return True
                Else
                    Dim buff(3) As Byte
                    Dim data_out(3) As Byte
                    If Not USB_CONTROL_MSG_IN(USBREQ.VERSION, buff) Then Return False
                    Dim hw_byte As Integer = CInt(buff(0))
                    If hw_byte = Asc("C"c) Then
                        Me.HWBOARD = FCUSB_BOARD.Classic
                    ElseIf hw_byte = Asc("E"c) Then 'XPORT_PCB1
                        Me.HWBOARD = FCUSB_BOARD.NotSupported
                    ElseIf hw_byte = Asc("X"c) Then
                        Me.HWBOARD = FCUSB_BOARD.XPORT_PCB2
                    ElseIf hw_byte = Asc("0"c) Then
                        Me.HWBOARD = FCUSB_BOARD.Classic
                    End If
                    data_out(3) = buff(3)
                    data_out(2) = buff(2)
                    data_out(1) = Asc(".")
                    data_out(0) = buff(1)
                    Dim fwstr As String = Utilities.Bytes.ToChrString(data_out)
                    If fwstr.StartsWith("0") Then fwstr = fwstr.Substring(1)
                    Me.FW_VERSION = fwstr
                    Return True
                End If
            Catch ex As Exception
                Return False
            End Try
            Return True
        End Function

        Public Function FirmwareUpdate(new_fw() As Byte, fw_version As Single) As Boolean
            Try
                If Not Me.BOOTLOADER Then Return False
                Dim result As Boolean = USB_CONTROL_MSG_OUT(USBREQ.FW_UPDATE, Nothing, CUInt(new_fw.Length))
                If Not result Then Return False
                Dim bytes_left As Integer = new_fw.Length
                Dim ptr As Integer = 0
                RaiseEvent OnUpdateProgress(Me, 0)
                While (bytes_left > 0)
                    Dim count As Integer = bytes_left
                    If (count > 4096) Then count = 4096
                    Dim buffer(count - 1) As Byte
                    Array.Copy(new_fw, ptr, buffer, 0, buffer.Length)
                    result = USB_BULK_OUT(buffer)
                    If Not result Then Return False
                    ptr += count
                    bytes_left -= count
                    Dim p As Integer = CInt(Math.Floor((ptr / new_fw.Length) * 100))
                    RaiseEvent OnUpdateProgress(Me, p)
                    Utilities.Sleep(100)
                End While
                Dim fw_ver_data As UInt32 = CUInt(&HFC000000UI Or CUInt(CInt(Math.Floor(fw_version)) << 8) Or (CInt((fw_version * 100)) And 255))
                USB_CONTROL_MSG_OUT(USBREQ.FW_REBOOT, Nothing, fw_ver_data)
                RaiseEvent OnUpdateProgress(Me, 100)
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function BootloaderUpdate(bl_data() As Byte) As Boolean
            Try
                If Me.BOOTLOADER Then Return False 'Can only update bootloader when running in APP mode
                Dim result As Boolean = USB_CONTROL_MSG_OUT(USBREQ.FW_UPDATE, Nothing, CUInt(bl_data.Length))
                If Not result Then Return False
                Dim bytes_left As Integer = bl_data.Length
                Dim ptr As Integer = 0
                RaiseEvent OnUpdateProgress(Me, 0)
                While (bytes_left > 0)
                    Dim count As Integer = bytes_left
                    If (count > 2048) Then count = 2048
                    Dim buffer(count - 1) As Byte
                    Array.Copy(bl_data, ptr, buffer, 0, buffer.Length)
                    result = USB_BULK_OUT(buffer)
                    If Not result Then Return False
                    ptr += count
                    bytes_left -= count
                    Dim p As Integer = CInt(Math.Floor((ptr / bl_data.Length) * 100))
                    RaiseEvent OnUpdateProgress(Me, p)
                    Utilities.Sleep(100)
                End While
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function LOGIC_GetVersion() As UInt32
            Dim cpld_data(3) As Byte
            USB_CONTROL_MSG_IN(USBREQ.LOGIC_VERSION_GET, cpld_data)
            Array.Reverse(cpld_data)
            Dim result As UInt32 = Utilities.Bytes.ToUInt32(cpld_data)
            Return result
        End Function

        Public Function LOGIC_SetVersion(new_ver As UInt32) As Boolean
            Dim result As Boolean = USB_CONTROL_MSG_OUT(USBREQ.LOGIC_VERSION_SET, Nothing, new_ver)
            Return result
        End Function

    End Class
    'USB commands
    Public Enum USBREQ As Byte
        JTAG_DETECT = &H10
        JTAG_RESET = &H11
        JTAG_SELECT = &H12
        JTAG_READ_B = &H13
        JTAG_READ_H = &H14
        JTAG_READ_W = &H15
        JTAG_WRITE = &H16
        JTAG_READMEM = &H17
        JTAG_WRITEMEM = &H18
        JTAG_ARM_INIT = &H19
        JTAG_INIT = &H1A
        JTAG_FLASHSPI_BRCM = &H1B
        JTAG_FLASHSPI_ATH = &H1C
        JTAG_FLASHWRITE_I16 = &H1D
        JTAG_FLASHWRITE_A16 = &H1E
        JTAG_FLASHWRITE_SST = &H1F
        JTAG_FLASHWRITE_AMDNB = &H20
        JTAG_SCAN = &H21
        JTAG_TOGGLE = &H22 'Toggle in HIGH values into TDI
        JTAG_GOTO_STATE = &H23
        JTAG_SET_OPTION = &H24
        JTAG_REGISTERS = &H25
        JTAG_SHIFT_DATA = &H27
        JTAG_BDR_SETUP = &H28
        JTAG_BDR_INIT = &H29
        JTAG_BDR_ADDPIN = &H2A
        JTAG_BDR_WRCMD = &H2B
        JTAG_BDR_WRMEM = &H2C
        JTAG_BDR_RDMEM = &H2D
        JTAG_BDR_RDFLASH = &H2E
        JTAG_BDR_WRFLASH = &H2F
        JTAG_BDR_SETBSR = &H30
        JTAG_BDR_WRITEBSR = &H31
        JTAG_SHIFT_IR = &H32
        JTAG_SHIFT_DR = &H33
        SPI_INIT = &H40
        SPI_SS_ENABLE = &H41
        SPI_SS_DISABLE = &H42
        SPI_PROG = &H43
        SPI_RD_DATA = &H44
        SPI_WR_DATA = &H45
        SPI_READFLASH = &H46
        SPI_WRITEFLASH = &H47
        SPI_WRITEDATA_AAI = &H48
        S93_INIT = &H49
        S93_READEEPROM = &H4A
        S93_WRITEEEPROM = &H4B
        S93_ERASE = &H4C
        SQI_SETUP = &H50
        SQI_SS_ENABLE = &H51
        SQI_SS_DISABLE = &H52
        SQI_RD_DATA = &H53
        SQI_WR_DATA = &H54
        SQI_RD_FLASH = &H55
        SQI_WR_FLASH = &H56
        SPINAND_READFLASH = &H5B
        SPINAND_WRITEFLASH = &H5C
        EXPIO_TIMING = &H5E
        I2C_INIT = &H60
        I2C_READEEPROM = &H61
        I2C_WRITEEEPROM = &H62
        I2C_RESULT = &H63
        EXPIO_INIT = &H64
        EXPIO_ADDRESS = &H65
        EXPIO_WRITEDATA = &H66
        EXPIO_READDATA = &H67
        EXPIO_RDID = &H68
        EXPIO_CHIPERASE = &H69
        EXPIO_SECTORERASE = &H6A
        EXPIO_WRITEPAGE = &H6B
        EXPIO_ADDRESS_CE = &H6F
        EXPIO_MODE_READ = &H73
        EXPIO_MODE_WRITE = &H74
        EXPIO_MODE_DELAY = &H75
        EXPIO_CTRL = &H76
        EXPIO_DELAY = &H78
        EXPIO_WRMEMDATA = &H7B
        EXPIO_RDMEMDATA = &H7C
        EXPIO_WAIT = &H7D
        EXPIO_SR = &H7F
        VERSION = &H80
        ECHO = &H81
        LEDON = &H82
        LEDOFF = &H83
        LEDBLINK = &H84
        START_SENDING_FIRM = &H85
        SEND_FIRM_SIZE = &H86
        SEND_FIRM_DATA = &H87
        STOP_SEND_FIRM_DATA = &H88
        ABORT = &H89
        VCC_1V8 = &H8A
        VCC_3V = &H8B
        VCC_5V = &H8C
        VCC_ON = &H8D
        VCC_OFF = &H8E
        GET_TASK = &H8F
        LOAD_PAYLOAD = &H90
        READ_PAYLOAD = &H91
        FW_UPDATE = &H94 'Update the firmware/bootloader
        FW_REBOOT = &H97
        TEST_READ = &HA1
        TEST_WRITE = &HA2
        SMC_WR = &HA3 'This writes data to our SMC BUS (PRO and MACH1)
        SMC_RD = &HA4 'And read data from SMC BUS
        SWI_DETECT = &HB0
        SWI_READ = &HB1
        SWI_WRITE = &HB2
        SWI_RD_REG = &HB3
        SWI_WR_REG = &HB4
        SWI_LOCK_REG = &HB5
        PULSE_RESET = &HB6
        LOGIC_STATUS = &HC0
        LOGIC_OFF = &HC1  'Turns off LOGIC circuit
        LOGIC_1V8 = &HC2  'Turns on 1.8V and then LOGIC
        LOGIC_3V3 = &HC3  'Turns on 3.3V and then LOGIC
        LOGIC_VERSION_GET = &HC4  'returns the LOGIC version from the Flash
        LOGIC_VERSION_SET = &HC5  'Writes the LOGIC to the Flash
        SPI_REPEAT = &HC6
        EPROM_RESULT = &HC7
        LOGIC_START = &HC8
        NAND_ONFI = &HD0
        NAND_SR = &HD1
        NAND_SETTYPE = &HD2
        JUMP_BOOT = &HF4
        EEPROM_WR = &HF5     'Write internal EEPROM
        EEPROM_RD = &HF6     'Read internal EEPROM
    End Enum

    Public Enum FCUSB_HW_CTRL As Byte
        WE_HIGH = 1
        WE_LOW = 2
        OE_HIGH = 3
        OE_LOW = 4
        CE_HIGH = 5
        CE_LOW = 6
        VPP_0V = 7
        VPP_5V = 8
        VPP_12V = 9
        RELAY_ON = 10 'PE7=HIGH
        RELAY_OFF = 11 'PE7=LOW
        VPP_DISABLE = 12 'CLE_LOW
        VPP_ENABLE = 13 'CLE_HIGH
        CLE_HIGH = 14
        CLE_LOW = 15
        ALE_HIGH = 16
        ALE_LOW = 17
        RB0_HIGH = 18
        RB0_LOW = 19
        BYTE_HIGH = 20
        BYTE_LOW = 21
    End Enum

    Public Enum DeviceStatus
        ExtIoNotConnected = 0
        NotDetected = 1
        Supported = 2
        NotSupported = 3
        NotCompatible = 4
    End Enum

    Public Enum FCUSB_BOARD
        NotSupported 'PCB4.0/XPORT 1.x
        ATMEL_DFU 'Bootloader (either ATMEL_DFU or FC_BL)
        Classic 'Classic (PCB 2.x)
        XPORT_PCB2 'XPORT firmware (PCB 2.x)
        Professional_PCB5
        Mach1 '(PCB 2.x)
    End Enum

End Namespace