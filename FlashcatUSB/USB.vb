Imports System.Threading
Imports FlashcatUSB.MemoryInterface
Imports FlashcatUSB.SPI
Imports LibUsbDotNet
Imports LibUsbDotNet.Main

Namespace USB

    Public Class HostClient
        Public Event DeviceConnected(ByVal usb_dev As FCUSB_DEVICE)
        Public Event DeviceDisconnected(ByVal usb_dev As FCUSB_DEVICE)

        Private Const DEFAULT_TIMEOUT As Integer = 5000
        Private Const USB_VID_ATMEL As UInt16 = &H3EB
        Private Const USB_VID_EC As UInt16 = &H16C0
        Private Const USB_PID_FCUSB_PRO As UInt16 = &H5E0
        Private Const USB_PID_FCUSB_MACH As UInt16 = &H5E1
        Private Const BUFFER_SIZE As UInt16 = 2048

        Public FCUSB() As FCUSB_DEVICE
        'The first board to connect sets the hardware for multi-device
        Public Property HW_MODE As FCUSB_BOARD = FCUSB_BOARD.NotConnected
        Public Property HW_BUSY As Boolean = False

        Sub New()
            ReDim FCUSB(4)
            FCUSB(0) = New FCUSB_DEVICE With {.USB_INDEX = 0}
            FCUSB(1) = New FCUSB_DEVICE With {.USB_INDEX = 1}
            FCUSB(2) = New FCUSB_DEVICE With {.USB_INDEX = 2}
            FCUSB(3) = New FCUSB_DEVICE With {.USB_INDEX = 3}
            FCUSB(4) = New FCUSB_DEVICE With {.USB_INDEX = 4}
            AddHandler FCUSB(0).UpdateProgress, AddressOf OnDeviceUpdateProgress
            AddHandler FCUSB(1).UpdateProgress, AddressOf OnDeviceUpdateProgress
            AddHandler FCUSB(2).UpdateProgress, AddressOf OnDeviceUpdateProgress
            AddHandler FCUSB(3).UpdateProgress, AddressOf OnDeviceUpdateProgress
            AddHandler FCUSB(4).UpdateProgress, AddressOf OnDeviceUpdateProgress
        End Sub

        Public Sub StartService()
            Try
                Dim td As New Thread(AddressOf ConnectionThread)
                td.Name = "tdUsbMonitor"
                td.Start()
            Catch ex As Exception
            End Try
        End Sub

        Public Class FCUSB_DEVICE
            Public Property USB_PATH As String
            Public Property IS_CONNECTED As Boolean = False
            Public Property USB_INDEX As Integer = -1 'Ports 0 - 4
            Public Property FW_VERSION As String = ""
            Public Property UPDATE_IN_PROGRESS As Boolean = False

            Public ATTACHED As New List(Of MemoryDeviceInstance)

            Public USBHANDLE As UsbDevice

            Public SPI_NOR_IF As New SPI_Programmer(Me)
            Public SQI_NOR_IF As New SQI_Programmer(Me)
            Public SPI_NAND_IF As New SPINAND_Programmer(Me)
            Public EXT_IF As New PARALLEL_NOR_NAND(Me)
            Public HF_IF As New HF_Port(Me)
            Public JTAG_IF As New JTAG.JTAG_IF(Me)
            Public I2C_IF As New I2C_Programmer(Me)
            Public DFU_IF As New DFU_API(Me)
            Public NAND_IF As New NAND_BLOCK_IF 'BAD block management system
            Public MW_IF As New Microwire_Programmer(Me)
            Public SWI_IF As New SWI_Programmer(Me)

            Private USB_TIMEOUT_VALUE As Integer = DEFAULT_TIMEOUT

            Public Property HWBOARD As FCUSB_BOARD = FCUSB_BOARD.NotConnected

            Public Event UpdateProgress(ByVal percent As Integer, ByRef device As FCUSB_DEVICE)

            Public ReadOnly Property IsProfessional As Boolean
                Get
                    If Me.HWBOARD = FCUSB_BOARD.Professional_PCB4 Then
                        Return True
                    ElseIf Me.HWBOARD = FCUSB_BOARD.Professional_PCB5 Then
                        Return True
                    Else
                        Return False
                    End If
                End Get
            End Property

            Sub New()
                AddHandler SPI_NOR_IF.PrintConsole, AddressOf WriteConsole 'Lets set text output to the console
                AddHandler SQI_NOR_IF.PrintConsole, AddressOf WriteConsole
                AddHandler SPI_NAND_IF.PrintConsole, AddressOf WriteConsole
                AddHandler I2C_IF.PrintConsole, AddressOf WriteConsole
                AddHandler EXT_IF.PrintConsole, AddressOf WriteConsole
                AddHandler MW_IF.PrintConsole, AddressOf WriteConsole
                AddHandler HF_IF.PrintConsole, AddressOf WriteConsole

                If IS_DEBUG_VER Then USB_TIMEOUT_VALUE = 5000000
            End Sub

            Public ReadOnly Property IsAlive() As Boolean
                Get
                    If USBHANDLE Is Nothing Then Return False
                    Return USBHANDLE.UsbRegistryInfo.IsAlive
                End Get
            End Property

            Private SPI_MODE_BYTE As Byte
            Private SPI_ORDER_BYTE As Byte

            Public Sub USB_SPI_SETUP(ByVal mode As SPIBUS_MODE, ByVal bit_order As SPI_ORDER)
                Try
                    Dim clock_speed As UInt32 = GetSpiClock(Me.HWBOARD, 8000000)
                    If (Me.HWBOARD = FCUSB_BOARD.Professional_PCB4) Then
                        USB_CONTROL_MSG_OUT(USBREQ.SPI_INIT, Nothing, clock_speed)
                    Else
                        SPI_ORDER_BYTE = 0
                        If bit_order = SPI_ORDER.SPI_ORDER_MSB_FIRST Then
                            SPI_ORDER_BYTE = 0
                        ElseIf bit_order = SPI_ORDER.SPI_ORDER_LSB_FIRST Then
                            SPI_ORDER_BYTE = &H20
                        End If
                        SPI_MODE_BYTE = 0
                        Select Case mode
                            Case SPI.SPIBUS_MODE.SPI_MODE_0
                                SPI_MODE_BYTE = 0
                            Case SPI.SPIBUS_MODE.SPI_MODE_1
                                SPI_MODE_BYTE = &H4
                            Case SPI.SPIBUS_MODE.SPI_MODE_2
                                SPI_MODE_BYTE = &H8
                            Case SPI.SPIBUS_MODE.SPI_MODE_3
                                SPI_MODE_BYTE = &HC
                        End Select
                        Dim clock_byte As Byte = &H80
                        If clock_speed = 8000000 Then
                            clock_byte = &H80 'SPI_CLOCK_FOSC_2
                        ElseIf clock_speed = 4000000 Then
                            clock_byte = &H0 'SPI_CLOCK_FOSC_4
                        ElseIf clock_speed = 2000000 Then
                            clock_byte = &H81 'SPI_CLOCK_FOSC_8
                        ElseIf clock_speed = 1000000 Then
                            clock_byte = &H1 'SPI_CLOCK_FOSC_16
                        End If
                        Dim spiConf As UInt16 = CUShort(clock_byte Or SPI_MODE_BYTE Or SPI_ORDER_BYTE)
                        USB_CONTROL_MSG_OUT(USBREQ.SPI_INIT, Nothing, CUInt(spiConf))
                    End If
                    Thread.Sleep(50)
                Catch ex As Exception
                End Try
            End Sub

            Public Sub USB_SPI_SETSPEED(ByVal MHZ As UInt32)
                If (Me.IsProfessional) OrElse (Me.HWBOARD = FCUSB_BOARD.Mach1) Then
                    USB_CONTROL_MSG_OUT(USBREQ.SPI_INIT, Nothing, MHZ)
                Else
                    Dim clock_byte As Byte
                    Select Case MHZ
                        Case 8000000
                            clock_byte = &H80 'SPI_CLOCK_FOSC_2
                        Case 4000000
                            clock_byte = &H0 'SPI_CLOCK_FOSC_4
                        Case 2000000
                            clock_byte = &H81 'SPI_CLOCK_FOSC_8
                        Case 1000000
                            clock_byte = &H1 'SPI_CLOCK_FOSC_16
                    End Select
                    Dim spiConf As UInt16 = CUShort(clock_byte Or SPI_MODE_BYTE Or SPI_ORDER_BYTE)
                    USB_CONTROL_MSG_OUT(USBREQ.SPI_INIT, Nothing, CUInt(spiConf))
                End If
            End Sub

            Public Function USB_SETUP_BULKOUT(RQ As USBREQ, SETUP() As Byte, BULK_OUT() As Byte, ByVal control_dt As UInt32, Optional timeout As Integer = -1) As Boolean
                Try
                    If (Me.IsProfessional) OrElse (Me.HWBOARD = FCUSB_BOARD.Mach1) Then
                        Dim ErrCounter As Integer = 0
                        Dim result As Boolean = True
                        Do
                            result = True
                            If SETUP IsNot Nothing Then
                                result = USB_CONTROL_MSG_OUT(USBREQ.LOAD_PAYLOAD, SETUP, SETUP.Length) 'Sends setup data
                            End If
                            If result Then
                                result = USB_CONTROL_MSG_OUT(RQ, Nothing, control_dt) 'Sends setup command
                            End If
                            If result Then
                                If BULK_OUT Is Nothing Then Return True
                                Utilities.Sleep(2)
                                result = USB_BULK_OUT(BULK_OUT, timeout)
                            End If
                            If result Then Return True
                            If Not result Then ErrCounter += 1
                            If ErrCounter = 3 Then
                                Return False
                            End If
                        Loop
                    Else
                        Dim result As Boolean = True = USB_CONTROL_MSG_OUT(RQ, SETUP, control_dt) 'Sends setup command and data
                        If Not result Then Return False
                        If BULK_OUT Is Nothing Then Return True
                        result = USB_BULK_OUT(BULK_OUT)
                        Return result
                    End If
                Catch ex As Exception
                    Return False
                End Try
            End Function

            Public Function USB_SETUP_BULKIN(RQ As USBREQ, SETUP() As Byte, ByRef DATA_IN() As Byte, control_dt As UInt32, Optional timeout As Integer = -1) As Boolean
                Try
                    Dim result As Boolean
                    If (Me.IsProfessional) OrElse (Me.HWBOARD = FCUSB_BOARD.Mach1) Then
                        Dim ErrCounter As Integer = 0
                        Do
                            result = True
                            If SETUP IsNot Nothing Then
                                result = USB_CONTROL_MSG_OUT(USBREQ.LOAD_PAYLOAD, SETUP, SETUP.Length)
                            End If
                            If result Then
                                result = USB_CONTROL_MSG_OUT(RQ, Nothing, control_dt) 'Sends the USB REQ and the CONTROL data
                            End If
                            If result Then
                                Utilities.Sleep(5)
                                result = USB_BULK_IN(DATA_IN, timeout)
                            End If
                            If Not result Then ErrCounter += 1
                            If ErrCounter = 3 Then Return False
                        Loop While Not result
                        Return True
                    Else
                        result = USB_CONTROL_MSG_OUT(RQ, SETUP, control_dt) 'Sends setup command and data
                        If Not result Then Return False
                        result = USB_BULK_IN(DATA_IN, timeout)
                        Return result
                    End If
                Catch ex As Exception
                    Return False
                End Try
            End Function
            'Sends a control message with an optional byte buffer to write
            Public Function USB_CONTROL_MSG_OUT(RQ As USBREQ, Optional buffer_out() As Byte = Nothing, Optional ByVal data As UInt32 = 0) As Boolean
                Try
                    If USBHANDLE Is Nothing Then Return False
                    Dim result As Boolean
                    Dim usb_flag As Byte
                    If (Me.IsProfessional) OrElse (Me.HWBOARD = FCUSB_BOARD.Mach1) Then
                        usb_flag = (UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Interface Or UsbCtrlFlags.Direction_Out)
                    Else
                        usb_flag = (UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.Direction_Out)
                    End If
                    Dim wValue As UInt16 = (data And &HFFFF0000UI) >> 16
                    Dim wIndex As UInt16 = (data And &HFFFF)
                    Dim bytes_out As Short = 0
                    If buffer_out IsNot Nothing Then bytes_out = CShort(buffer_out.Length)
                    Dim usbSetupPacket As New UsbSetupPacket(usb_flag, RQ, wValue, wIndex, bytes_out)
                    Dim bytes_xfer As Integer = 0
                    If buffer_out Is Nothing Then
                        result = USBHANDLE.ControlTransfer(usbSetupPacket, Nothing, 0, bytes_xfer)
                    Else
                        result = USBHANDLE.ControlTransfer(usbSetupPacket, buffer_out, buffer_out.Length, bytes_xfer)
                    End If
                    Return result
                Catch ex As Exception
                    Return False
                End Try
            End Function
            'Sends a control message with a byte buffer to receive data
            Public Function USB_CONTROL_MSG_IN(RQ As USBREQ, ByRef Buffer_in() As Byte, Optional ByVal data As UInt32 = 0) As Boolean
                Try
                    If USBHANDLE Is Nothing Then Return False
                    Dim usb_flag As Byte
                    Dim bytes_xfer As Integer = 0
                    If (Me.IsProfessional) OrElse (Me.HWBOARD = FCUSB_BOARD.Mach1) Then
                        usb_flag = UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Interface Or UsbCtrlFlags.Direction_In
                    Else
                        usb_flag = UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.Direction_In
                    End If
                    Dim wValue As UInt16 = (data And &HFFFF0000UI) >> 16
                    Dim wIndex As UInt16 = (data And &HFFFF)
                    Dim usb_setup As New UsbSetupPacket(usb_flag, RQ, wValue, wIndex, CShort(Buffer_in.Length))
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
                    If (Me.IsProfessional) OrElse (Me.HWBOARD = FCUSB_BOARD.Mach1) Then
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

            Public Function USB_BULK_OUT(ByVal buffer_out() As Byte, Optional Timeout As Integer = -1) As Boolean
                Try
                    If Timeout = -1 Then Timeout = USB_TIMEOUT_VALUE
                    If (Me.IsProfessional) OrElse (Me.HWBOARD = FCUSB_BOARD.Mach1) Then
                        Dim xfer As Integer = 0
                        Using ep_writer As UsbEndpointWriter = USBHANDLE.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
                            Dim ec2 As ErrorCode = ep_writer.Write(buffer_out, 0, CInt(buffer_out.Length), Timeout, xfer) '5 second timeout
                            If (Not ec2 = ErrorCode.None) Then
                                Dim result As Boolean = USB_CONTROL_MSG_OUT(USBREQ.ABORT)
                                ep_writer.Reset()
                                Return False 'LibUsbDotNet.Main.ErrorCode Win32Error {&HFFFFC011}
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

            Public Sub Disconnect()
                Try
                    ATTACHED.Clear()
                    Me.IS_CONNECTED = False
                    Me.FW_VERSION = ""
                    Me.USB_PATH = ""
                    If Me.USBHANDLE IsNot Nothing Then
                        Try
                            Me.USB_LEDOff()
                        Catch ex As Exception
                        End Try
                        Dim wholeUsbDevice As IUsbDevice = TryCast(Me.USBHANDLE, IUsbDevice)
                        If wholeUsbDevice IsNot Nothing Then 'Libusb only
                            wholeUsbDevice.ReleaseInterface(0)
                        End If
                        Me.USBHANDLE.Close()
                    End If
                    Me.USBHANDLE = Nothing
                Catch ex As Exception
                End Try
            End Sub

            Public Sub USB_LEDOn()
                Try
                    If HWBOARD = FCUSB_BOARD.DFU_BL Then Exit Sub 'Bootloader does not have LED
                    USB_CONTROL_MSG_OUT(USBREQ.LEDON) 'SPIREQ.LEDON
                Catch ex As Exception
                End Try
            End Sub

            Public Sub USB_LEDOff()
                Try
                    If HWBOARD = FCUSB_BOARD.DFU_BL Then Exit Sub 'Bootloader does not have LED
                    USB_CONTROL_MSG_OUT(USBREQ.LEDOFF) 'SPIREQ.LEDOFF
                Catch ex As Exception
                End Try
            End Sub

            Public Sub USB_LEDBlink()
                Try
                    If HWBOARD = FCUSB_BOARD.DFU_BL Then Exit Sub 'Bootloader does not have LED
                    USB_CONTROL_MSG_OUT(USBREQ.LEDBLINK)
                Catch ex As Exception
                End Try
            End Sub

            Public Function USB_Echo() As Boolean
                Try
                    If (USBHANDLE.UsbRegistryInfo.Pid = USB_PID_FCUSB_PRO) OrElse (USBHANDLE.UsbRegistryInfo.Pid = USB_PID_FCUSB_MACH) Then
                        Dim packet_out(3) As Byte
                        If Not USB_CONTROL_MSG_IN(USBREQ.ECHO, packet_out, &H45434643UI) Then Return False
                        If Not packet_out(0) = &H45 Then Return False
                        If Not packet_out(1) = &H43 Then Return False
                        If Not packet_out(2) = &H46 Then Return False
                        If Not packet_out(3) = &H43 Then Return False
                    Else
                        Dim packet_out(7) As Byte
                        Dim data_in As UInt32 = &H12345678UI
                        If Not USB_CONTROL_MSG_IN(USBREQ.ECHO, packet_out, data_in) Then Return False 'SPIREQ.ECHO
                        If packet_out(1) <> CByte(USBREQ.ECHO) Then Return False
                        If packet_out(2) <> &H34 Then Return False
                        If packet_out(3) <> &H12 Then Return False
                        If packet_out(4) <> &H78 Then Return False
                        If packet_out(5) <> &H56 Then Return False
                        If packet_out(6) <> &H8 Then Return False
                        If packet_out(7) <> &H0 Then Return False
                    End If
                    Return True 'Echo successful
                Catch ex As Exception
                    Return False
                End Try
            End Function

            Public Sub USB_VCC_OFF()
                If (Me.IsProfessional) OrElse (Me.HWBOARD = FCUSB_BOARD.Mach1) Then
                    USB_CONTROL_MSG_OUT(USBREQ.LOGIC_OFF)
                End If
            End Sub

            Public Sub USB_VCC_ON()
                If (Me.IsProfessional) OrElse (Me.HWBOARD = FCUSB_BOARD.Mach1) Then
                    If (MySettings.VOLT_SELECT = Voltage.V1_8) Then
                        USB_VCC_1V8()
                    Else
                        USB_VCC_3V()
                    End If
                End If
            End Sub

            Public Sub USB_VCC_1V8()
                If (Me.IsProfessional) OrElse (Me.HWBOARD = FCUSB_BOARD.Mach1) Then
                    USB_CONTROL_MSG_OUT(USBREQ.LOGIC_OFF)
                    Utilities.Sleep(250)
                    USB_CONTROL_MSG_OUT(USBREQ.LOGIC_1V8)
                    Utilities.Sleep(100)
                    MySettings.VOLT_SELECT = Voltage.V1_8
                End If
            End Sub

            Public Sub USB_VCC_3V()
                If (Me.IsProfessional) OrElse (Me.HWBOARD = FCUSB_BOARD.Mach1) Then
                    USB_CONTROL_MSG_OUT(USBREQ.LOGIC_OFF)
                    Utilities.Sleep(250)
                    USB_CONTROL_MSG_OUT(USBREQ.LOGIC_3V3)
                    Utilities.Sleep(100)
                    MySettings.VOLT_SELECT = Voltage.V3_3
                End If
            End Sub

            Public Property USB_IsBootloaderMode As Boolean = False
            Public Property USB_IsAppUpdaterMode As Boolean = False

            Public Function USB_WaitForComplete() As Boolean
                Dim timeout_counter As Integer
                Dim task_id As Byte = 255
                Do
                    Dim packet_out(0) As Byte
                    Utilities.Sleep(5) 'Prevents slamming the USB port
                    Dim result As Boolean = USB_CONTROL_MSG_IN(USBREQ.GET_TASK, packet_out)
                    If Not result Then
                        task_id = 1 'WAITING
                    Else
                        task_id = packet_out(0)
                    End If
                    timeout_counter += 1
                    If (timeout_counter = 1000) Then Return False 'Changed from 500
                Loop While (task_id > 0)
                Return True
            End Function

            Public Function LoadFirmwareVersion() As Boolean
                Try
                    USB_IsBootloaderMode = False
                    USB_IsAppUpdaterMode = False
                    If (USBHANDLE.UsbRegistryInfo.Vid = USB_VID_ATMEL) Then
                        Me.HWBOARD = FCUSB_BOARD.DFU_BL
                        Me.FW_VERSION = "1.00"
                    ElseIf USBHANDLE.UsbRegistryInfo.Pid = USB_PID_FCUSB_PRO Then
                        Me.HWBOARD = FCUSB_BOARD.Professional_PCB4
                        Dim b(3) As Byte
                        If Not USB_CONTROL_MSG_IN(USBREQ.VERSION, b) Then Return False
                        Me.FW_VERSION = Utilities.Bytes.ToChrString({b(1), Asc("."), b(2), b(3)})
                        If (b(0) = Asc("B")) Then
                            Me.USB_IsBootloaderMode = True
                        ElseIf (b(0) = Asc("F")) Then
                            Me.USB_IsAppUpdaterMode = True
                        ElseIf (b(0) = Asc("T")) Then
                            Me.HWBOARD = FCUSB_BOARD.Professional_PCB5
                        End If
                        Return True
                    ElseIf USBHANDLE.UsbRegistryInfo.Pid = USB_PID_FCUSB_MACH Then
                        Dim b(3) As Byte
                        If Not USB_CONTROL_MSG_IN(USBREQ.VERSION, b) Then Return False
                        If (b(0) = Asc("B")) Then
                            Me.USB_IsBootloaderMode = True
                        End If
                        Me.FW_VERSION = Utilities.Bytes.ToChrString({b(1), Asc("."), b(2), b(3)})
                        Me.HWBOARD = FCUSB_BOARD.Mach1
                        Return True
                    Else
                        Dim buff(3) As Byte
                        Dim data_out(3) As Byte
                        If Not USB_CONTROL_MSG_IN(USBREQ.VERSION, buff) Then Return False
                        Dim hw_byte As Byte = buff(0)
                        If hw_byte = CByte(Asc("C")) Then
                            Me.HWBOARD = FCUSB_BOARD.Classic
                        ElseIf hw_byte = CByte(Asc("E")) Then
                            Me.HWBOARD = FCUSB_BOARD.XPORT_PCB1
                        ElseIf hw_byte = CByte(Asc("X")) Then
                            Me.HWBOARD = FCUSB_BOARD.XPORT_PCB2
                        ElseIf hw_byte = CByte(Asc("0")) Then
                            Me.HWBOARD = FCUSB_BOARD.Classic
                        End If
                        data_out(3) = buff(3)
                        data_out(2) = buff(2)
                        data_out(1) = Asc(".")
                        data_out(0) = buff(1)
                        Dim fwstr As String = Utilities.Bytes.ToChrString(data_out)
                        If fwstr.StartsWith("0") Then fwstr = Mid(fwstr, 2)
                        Me.FW_VERSION = fwstr
                        Return True
                    End If
                    Return False
                Catch ex As Exception
                    Return False
                End Try
                Return True
            End Function

            Public Function SAM3U_FirmwareUpdate(ByVal new_fw() As Byte, fw_version As Single) As Boolean
                Try
                    Dim result As Boolean = USB_CONTROL_MSG_OUT(USBREQ.FW_UPDATE, Nothing, new_fw.Length)
                    If Not result Then Return False
                    Dim bytes_left As UInt32 = new_fw.Length
                    Dim ptr As Integer = 0
                    RaiseEvent UpdateProgress(0, Me)
                    While (bytes_left > 0)
                        Dim count As Integer = bytes_left
                        If (count > 4096) Then count = 4096
                        Dim buffer(count - 1) As Byte
                        Array.Copy(new_fw, ptr, buffer, 0, buffer.Length)
                        result = USB_BULK_OUT(buffer)
                        If Not result Then Return False
                        ptr += count
                        bytes_left -= count
                        Dim p As Integer = Math.Floor((ptr / new_fw.Length) * 100)
                        RaiseEvent UpdateProgress(p, Me)
                        Utilities.Sleep(100)
                    End While
                    Dim fw_ver_data As UInt32 = &HFC000000UI Or (Math.Floor(fw_version) << 8) Or ((fw_version * 100) And 255)
                    USB_CONTROL_MSG_OUT(USBREQ.FW_REBOOT, Nothing, fw_ver_data)
                    RaiseEvent UpdateProgress(100, Me)
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

        Private Sub ConnectionThread()
            Do While (Not AppIsClosing)
                For i = 0 To FCUSB.Count - 1
                    If (Not FCUSB(i).UPDATE_IN_PROGRESS) Then
                        If FCUSB(i).IS_CONNECTED AndAlso Not FCUSB(i).IsAlive Then
                            FCUSB(i).Disconnect()
                            RaiseEvent DeviceDisconnected(FCUSB(i))
                            If Me.Count = 0 Then HW_MODE = FCUSB_BOARD.NotConnected
                        End If
                    End If
                Next
                Dim fcusb_list() As UsbRegistry = FindUsbDevices()
                If fcusb_list IsNot Nothing Then
                    For Each this_fcusb In fcusb_list
                        Dim fcusb_path As String = GetDeviceID(this_fcusb)
                        Dim Found As Boolean = False
                        For i = 0 To FCUSB.Count - 1
                            If FCUSB(i).USB_PATH = fcusb_path Then Found = True : Exit For
                        Next
                        If (Not Found) Then 'New device connected
                            For i = 0 To FCUSB.Count - 1
                                If (FCUSB(i).USB_PATH = "") Then 'This slot is available
                                    Dim this_dev As UsbDevice = this_fcusb.Device
                                    If this_dev Is Nothing Then Exit For
                                    If OpenUsbDevice(this_dev) Then
                                        FCUSB(i).USBHANDLE = this_dev
                                        FCUSB(i).USB_PATH = fcusb_path
                                        FCUSB(i).UPDATE_IN_PROGRESS = False
                                        FCUSB(i).IS_CONNECTED = True
                                        FCUSB(i).LoadFirmwareVersion()
                                        If (Not this_dev.UsbRegistryInfo.Vid = USB_VID_ATMEL) Then 'DFU Bootloader mode
                                            Dim echo_cmd As Boolean = FCUSB(i).USB_Echo
                                            If echo_cmd Then
                                                If HW_MODE = FCUSB_BOARD.NotConnected Then
                                                    HW_MODE = FCUSB(i).HWBOARD
                                                ElseIf HW_MODE = FCUSB(i).HWBOARD Then
                                                Else
                                                    FCUSB(i).USB_PATH = ""
                                                    FCUSB(i).IS_CONNECTED = False
                                                    FCUSB(i).USBHANDLE = Nothing
                                                    Exit For
                                                End If
                                            Else
                                                FCUSB(i).USB_PATH = ""
                                                FCUSB(i).IS_CONNECTED = False
                                                FCUSB(i).USBHANDLE = Nothing
                                                Exit For
                                            End If
                                        End If
                                        RaiseEvent DeviceConnected(FCUSB(i))
                                    End If
                                    Exit For
                                End If
                            Next
                        End If
                    Next
                End If
                Thread.Sleep(250)
            Loop
            USBCLIENT.Disconnect_All()
        End Sub

        Public Function Connect(ByVal usb_device_path As String) As FCUSB_DEVICE
            Try
                Dim fcusb_list() As UsbRegistry = FindUsbDevices()
                If fcusb_list Is Nothing OrElse fcusb_list.Count = 0 Then Return Nothing
                Dim this_dev As UsbDevice = Nothing
                If (usb_device_path = "") Then
                    this_dev = fcusb_list(0).Device
                Else
                    For i = 0 To fcusb_list.Count - 1
                        Dim devpath As String = GetDeviceID(fcusb_list(i))
                        If (devpath.ToUpper = usb_device_path.ToUpper) Then
                            this_dev = fcusb_list(0).Device : Exit For
                        End If
                    Next
                End If
                If this_dev Is Nothing Then Return Nothing
                If this_dev.UsbRegistryInfo.Vid = USB_VID_ATMEL Then Return Nothing
                If OpenUsbDevice(this_dev) Then
                    Dim n As New FCUSB_DEVICE
                    n.USBHANDLE = this_dev
                    If n.USB_Echo Then
                        n.UPDATE_IN_PROGRESS = False
                        n.IS_CONNECTED = True
                        n.LoadFirmwareVersion()
                        Return n
                    End If
                End If
            Catch ex As Exception
            End Try
            Return Nothing
        End Function

        Private Function OpenUsbDevice(ByVal usb_dev As UsbDevice) As Boolean
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

        Private Function FindUsbDevices() As UsbRegistry()
            Try
                Dim USB_PID_AT90USB162 As Integer = &H2FFA 'FCUSB PCB 1.x
                Dim USB_PID_AT90USB1287 As Integer = &H2FFB 'FCUSB EX (PROTO)
                Dim USB_PID_AT90USB646 As Integer = &H2FF9 'FCUSB EX (PRODUCTION)
                Dim USB_PID_ATMEGA32U2 As Integer = &H2FF0 'FCUSB PCB 2.1-2.2
                Dim USB_PID_ATMEGA32U4 As Integer = &H2FF4 'FCUSB PCB 3.2
                Dim USB_PID_FCUSB As Integer = &H5DE 'Classic
                Dim devices As New List(Of UsbRegistry)
                AddDevicesToList(USB_VID_ATMEL, USB_PID_AT90USB162, devices)
                AddDevicesToList(USB_VID_ATMEL, USB_PID_AT90USB1287, devices)
                AddDevicesToList(USB_VID_ATMEL, USB_PID_ATMEGA32U2, devices)
                AddDevicesToList(USB_VID_ATMEL, USB_PID_AT90USB646, devices)
                AddDevicesToList(USB_VID_ATMEL, USB_PID_ATMEGA32U4, devices)
                AddDevicesToList(USB_VID_EC, USB_PID_FCUSB, devices)
                AddDevicesToList(USB_VID_EC, USB_PID_FCUSB_PRO, devices)
                AddDevicesToList(USB_VID_EC, USB_PID_FCUSB_MACH, devices)
                If devices.Count = 0 Then Return Nothing
                Return devices.ToArray
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

        Private Function GetDeviceID(ByVal device As UsbRegistry) As String
            Try
                Dim dev_loc As String = "USB\VID_" & Hex(device.Vid).PadLeft(4, "0") & "&PID_" & Hex(device.Pid).PadLeft(4, "0")
                If device.GetType Is GetType(LibUsb.LibUsbRegistry) Then
                    dev_loc &= "\" & device.DeviceProperties("LocationInformation")
                ElseIf device.GetType Is GetType(LegacyUsbRegistry) Then
                    Dim legacy As LegacyUsbRegistry = DirectCast(device, LegacyUsbRegistry)
                    Dim DeviceFilename As String = DirectCast(legacy.Device, LibUsb.LibUsbDevice).DeviceFilename
                    dev_loc &= DeviceFilename
                End If
                Return dev_loc
            Catch ex As Exception
                Return ""
            End Try
        End Function

        Public Sub Disconnect_All()
            Try
                For Each dev In FCUSB
                    dev.Disconnect()
                Next
            Catch ex As Exception
            End Try
        End Sub

        Public Function GetConnectedPaths() As String()
            Try
                Dim paths As New List(Of String)
                Dim cnt_devices() As UsbRegistry = FindUsbDevices()
                If cnt_devices IsNot Nothing AndAlso cnt_devices.Count > 0 Then
                    For i = 0 To cnt_devices.Count - 1
                        Dim u As UsbRegistry = cnt_devices(i)
                        Dim o As String = GetDeviceID(u)
                        paths.Add(o)
                    Next
                End If
                Return paths.ToArray
            Catch ex As Exception
                Return Nothing
            End Try
        End Function

        Public Sub USB_VCC_OFF()
            For Each dev In FCUSB
                If dev.IS_CONNECTED Then dev.USB_VCC_OFF()
            Next
        End Sub

        Public Sub USB_VCC_ON()
            For Each dev In FCUSB
                If dev.IS_CONNECTED Then dev.USB_VCC_ON()
            Next
        End Sub

        Public Sub USB_VCC_1V8()
            For Each dev In FCUSB
                If dev.IS_CONNECTED Then dev.USB_VCC_1V8()
            Next
        End Sub

        Public Sub USB_VCC_3V()
            For Each dev In FCUSB
                If dev.IS_CONNECTED Then dev.USB_VCC_3V()
            Next
        End Sub

        Public Function Count() As Integer 'Number of FlashcatUSB connectedInteger
            Dim Counter As Integer = 0
            For Each dev In FCUSB
                If dev.IS_CONNECTED Then Counter += 1
            Next
            Return Counter
        End Function

    End Class

    Public Enum Voltage As Integer
        OFF = 0
        V1_8 = 1 'Low (300ma max)
        V3_3 = 2 'Default
    End Enum

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
        JTAG_TOGGLE = &H22
        JTAG_GOTO_STATE = &H23
        JTAG_SET_OPTION = &H24
        JTAG_REGISTERS = &H25
        JTAG_SHIFT_TMS = &H26
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
        EXPIO_NAND_ONFI = &H6C
        EXPIO_NAND_SR = &H6D
        EXPIO_NAND_PAGEOFFSET = &H6E
        EXPIO_MODE_ADDRESS = &H6F
        EXPIO_MODE_READ = &H73
        EXPIO_MODE_WRITE = &H74
        EXPIO_MODE_DELAY = &H75
        EXPIO_CTRL = &H76
        EXPIO_DELAY = &H78
        EXPIO_WRCMDDATA = &H7A
        EXPIO_WRMEMDATA = &H7B
        EXPIO_RDMEMDATA = &H7C
        EXPIO_WAIT = &H7D
        EXPIO_CPEN = &H7E
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
        FW_UPDATE = &H94 'Update the firmware
        FW_REBOOT = &H97
        TEST_READ = &HA1
        TEST_WRITE = &HA2
        SWI_DETECT = &HB0
        SWI_READ = &HB1
        SWI_WRITE = &HB2
        SWI_RD_REG = &HB3
        SWI_WR_REG = &HB4
        SWI_LOCK_REG = &HB5
        LOGIC_STATUS = &HC0
        LOGIC_OFF = &HC1  'Turns off LOGIC circuit
        LOGIC_1V8 = &HC2  'Turns on 1.8V and then LOGIC
        LOGIC_3V3 = &HC3  'Turns on 3.3V and then LOGIC
        LOGIC_VERSION_GET = &HC4  'returns the LOGIC version from the Flash
        LOGIC_VERSION_SET = &HC5  'Writes the LOGIC to the Flash
    End Enum

    Public Enum EXPIO_CTRL As Byte
        WE_HIGH = 1
        WE_LOW = 2
        OE_HIGH = 3
        OE_LOW = 4
        CE_HIGH = 5
        CE_LOW = 6
        VPP_0V = 7
        VPP_5V = 8
        VPP_12V = 9
        RELAY_ON = 10
        RELAY_OFF = 11
        VPP_DISABLE = 12
        VPP_ENABLE = 13
    End Enum

    Public Enum DeviceStatus
        ExtIoNotConnected = 0
        NotDetected = 1
        Supported = 2
        NotSupported = 3
        NotCompatible = 4
    End Enum

    Public Enum FCUSB_BOARD
        NotConnected = 0
        DFU_BL 'Bootloader
        Classic 'Classic (PCB 2.x)
        XPORT_PCB1 'XPORT firmware (PCB 1.x)
        XPORT_PCB2 'XPORT firmware (PCB 2.x)
        Professional_PCB4 'Professional (PCB 4.x)
        Professional_PCB5 'FPGA version
        Mach1 '(PCB 2.x)
    End Enum

End Namespace