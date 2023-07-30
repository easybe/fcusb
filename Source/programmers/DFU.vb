Imports LibUsbDotNet.Main
Imports FlashcatUSB.FlashMemory

Public Class DFU_Programmer : Implements MemoryDeviceUSB
    Public FCUSB As USB.FCUSB_DEVICE

    Private transaction As Integer = 0

    Private Const USB_VID_ATMEL As Integer = &H3EB
    Private Const USB_PID_AT90USB646 As Integer = &H2FF9 'FCUSB XPORT 2.x
    Private Const USB_PID_ATMEGA32U2 As Integer = &H2FF0 'FCUSB CLASSIC PCB 2.2
    Private Const USB_PID_ATMEGA32U4 As Integer = &H2FF4 'FCUSB CLASSIC PCB 2.3

    Public Event PrintConsole(message As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress

    Sub New(parent_if As USB.FCUSB_DEVICE)
        FCUSB = parent_if
    End Sub

#Region "Public Interface"

    Public ReadOnly Property GetDevice As Device Implements MemoryDeviceUSB.GetDevice
        Get
            Return Nothing
        End Get
    End Property

    Public ReadOnly Property DeviceName As String Implements MemoryDeviceUSB.DeviceName
        Get
            Select Case FCUSB.USBHANDLE.Info.Descriptor.ProductID
                Case USB_PID_AT90USB646
                    Return "Atmega AT90USB646"
                Case USB_PID_ATMEGA32U2
                    Return "Atmega ATMEGA32U2"
                Case USB_PID_ATMEGA32U4
                    Return "Atmega ATMEGA32U4"
                Case Else
                    Return "DFU Device"
            End Select
        End Get
    End Property

    Public ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
        Get
            Select Case FCUSB.USBHANDLE.Info.Descriptor.ProductID
                Case USB_PID_AT90USB646 '0x0000 - 0x77FF WORD
                    Return 61440 '(60KB, 4KB bootloader)
                Case USB_PID_ATMEGA32U2, USB_PID_ATMEGA32U4
                    Return 28672 '0 to 0x2FFF (32KB total, 4KB bootloader)
                Case Else
                    Return 0
            End Select
        End Get
    End Property

    Public Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
        Utilities.Sleep(50)
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        FCUSB.USBFLAG_IN = (UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Class Or UsbCtrlFlags.Recipient_Interface)
        FCUSB.USBFLAG_OUT = (UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Class Or UsbCtrlFlags.Recipient_Interface)
        Return True
    End Function

    Public Function ReadData(flash_offset As Long, data_count As Integer) As Byte() Implements MemoryDeviceUSB.ReadData
        Return Nothing 'NOT SUPPORTED
    End Function

    Public Function WriteData(flash_offset As Long, data() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        RaiseEvent SetProgress(0)
        Dim UsbStatus As DFU_STATUS
        Dim EndAddress As Integer = data.Length
        Dim pEnd As Integer = 0
        Dim currentAddress As Integer = 0 'Test address of flash
        Dim Packet() As Byte
        Do Until currentAddress = EndAddress
            RaiseEvent SetProgress(CInt((currentAddress / EndAddress) * 100))
            UsbStatus = GetStatus()
            If Not UsbStatus.StatusCode = DFU_STATUS_CODE.OK Then
                PrintErrorMsg(UsbStatus)
                ClearStatus()
                Return False
            End If
            Packet = PrepareDnData(data, currentAddress, pEnd)
            currentAddress = pEnd + 1
            If (Not SendData(Packet)) Then
                PrintErrorMsg(GetStatus)
                ClearStatus()
                Return False
            End If
            Utilities.Sleep(10)
        Loop
        RaiseEvent SetProgress(100)
        SendData(Nothing) 'End of firmware transmission
        Return True
    End Function
    'Erases the flash (not bootloader section)
    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        SendData(New Byte() {4, 0, 255}) 'Chip Erase command (not all device support this)
        Dim UsbStatus As DFU_STATUS = GetStatus()
        If Not UsbStatus.StatusCode = DFU_STATUS_CODE.OK Then
            PrintErrorMsg(UsbStatus)
            ClearStatus()
            Return False
        End If
        Return True
    End Function

    Public Function SectorFind(SectorIndex As Integer) As Long Implements MemoryDeviceUSB.SectorFind
        Throw New NotImplementedException()
    End Function

    Public Function SectorErase(SectorIndex As Integer) As Boolean Implements MemoryDeviceUSB.SectorErase
        Throw New NotImplementedException()
    End Function

    Public Function SectorCount() As Integer Implements MemoryDeviceUSB.SectorCount
        Throw New NotImplementedException()
    End Function

    Public Function SectorWrite(SectorIndex As Integer, data() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.SectorWrite
        Throw New NotImplementedException()
    End Function

    Public Function SectorSize(sector As Integer) As Integer Implements MemoryDeviceUSB.SectorSize
        Throw New NotImplementedException()
    End Function

#End Region

#Region "Enums"

    Private Enum DFU_OPCODE As Byte
        DETACH = 0
        DNLOAD = 1
        UPLOAD = 2
        GETSTATUS = 3
        CLRSTATUS = 4
        GETSTATE = 5
        ABORT = 6
    End Enum

    Enum DFU_STATUS_CODE
        OK = 0 'No error condition is present
        errTARGET = 1 'File is not targeted for use by this device
        errFILE = 2 'File is for this device but fails some vendor-specific verification test
        errWRITE = 3 'Device id unable to write memory
        errERASE = 4 'Memory erase function failed
        errCHECK_ERASED = 5 'Memory erase check failed
        errPROG = 6 'Program memory function failed
        errVERIFY = 7 'Programmed memory failed verification
        errADDRESS = 8 'Cannot program memory due to received address that is out of range
        errNOTDONE = 9 'Received DFU_DNLOAD with wLength = 0, but device does not think it has all thedata yet.
        errFIRMWARE = 10 'Device’s firmware is corrupted. It cannot return to run-time operations
        errVENDOR = 11 'iString indicates a vendor-specific error
        errUSBR = 12 'Device detected unexpected USB reset signaling
        errPOR = 13 'Device detected unexpected power on reset
        errUNKNOWN = 14 'Something went wrong, but the device does not know what it was
        errSTALLEDPK = 15 'Device stalled an unexpected request
    End Enum

    Enum DFU_STATE_CODE
        appIDLE = 0 'Device is running its normal application
        appDETACH = 1 'Device is running its normal application, has received the DFU_DETACH request, and is waiting for a USB reset
        dfuIDLE = 2 'Device is operating in the DFU mode and is waiting for requests
        dfuDNLOAD_SYNC = 3 'Device has received a block and is waiting for the Host to solicit the status via DFU_GETSTATUS
        dfuDNBUSY = 4 'Device is programming a control-write block into its non volatile memories
        dfuDNLOAD_IDLE = 5 'Device is processing a download operation. Expecting DFU_DNLOAD requests
        dfuMANIFEST_SYNC = 6 'Device has received the final block of firmware
        dfuMANIFEST = 7 'Device is in the Manifestation phase.
        dfuMANIFESTWAITRESET = 8 'Device has programmed its memories and is waiting for a USB reset or a power on reset.
        dfuUPLOAD_IDLE = 9 'The device is processing an upload operation. Expecting DFU_UPLOAD requests.
        dfuERROR = 10 'An error has occurred. Awaiting the DFU_CLRSTATUS request.
    End Enum

#End Region

    Structure DFU_STATUS
        Dim Err As Boolean 'True if device failed to retrieve this object
        Dim StatusCode As DFU_STATUS_CODE 'The status code
        Dim Timeout As Integer 'Minimum time in milliseconds that the host should wait
        Dim State As DFU_STATE_CODE
        Dim iString As Integer 'Index of status description in string table.
    End Structure

    Public Function GetStatus() As DFU_STATUS
        Dim retStat As DFU_STATUS
        Dim rMem(5) As Byte
        retStat.Err = Not GetStatus(rMem)
        retStat.StatusCode = CType(rMem(0), DFU_STATUS_CODE)
        retStat.Timeout = (CInt(rMem(3)) << 16) + (CInt(rMem(2)) << 8) + rMem(1)
        retStat.State = CType(rMem(4), DFU_STATE_CODE)
        retStat.iString = rMem(5)
        Return retStat
    End Function
    'Starts the application
    Public Function RunApp() As Boolean
        If Not SendData(New Byte() {4, 3, 0}) Then Return False 'Start App command
        Dim UsbStatus As DFU_STATUS = GetStatus()
        If Not UsbStatus.StatusCode = DFU_STATUS_CODE.OK Then
            PrintErrorMsg(UsbStatus)
            ClearStatus()
            Return False
        End If
        Return True
    End Function

    Private Sub PrintErrorMsg(input As DFU_STATUS)
        Dim State As String = DfuStateToString(input.State)
        Dim ErrorReason As String = DfuStatusToString(input.StatusCode)
        RaiseEvent PrintConsole("AVR DFU Error")
        RaiseEvent PrintConsole("State: " & State)
        RaiseEvent PrintConsole("Status: " & ErrorReason)
    End Sub

    Private Function DfuStateToString(StateVal As DFU_STATE_CODE) As String
        Dim State As String = ""
        Select Case StateVal
            Case DFU_STATE_CODE.appDETACH
                State = "Device is running its normal application, has received the DFU_DETACH request, and is waiting for a USB reset"
            Case DFU_STATE_CODE.appIDLE
                State = "Device is running its normal application"
            Case DFU_STATE_CODE.dfuDNBUSY
                State = "Device is programming a control-write block into its non volatile memories"
            Case DFU_STATE_CODE.dfuDNLOAD_IDLE
                State = "Device is processing a download operation. Expecting DFU_DNLOAD requests"
            Case DFU_STATE_CODE.dfuDNLOAD_SYNC
                State = "Device has received a block and is waiting for the Host to solicit the status via DFU_GETSTATUS"
            Case DFU_STATE_CODE.dfuERROR
                State = "An error has occurred. Awaiting the DFU_CLRSTATUS request."
            Case DFU_STATE_CODE.dfuIDLE
                State = "Device is operating in the DFU mode and is waiting for requests"
            Case DFU_STATE_CODE.dfuMANIFEST
                State = "Device is in the Manifestation phase."
            Case DFU_STATE_CODE.dfuMANIFEST_SYNC
                State = "Device has received the final block of firmware"
            Case DFU_STATE_CODE.dfuMANIFESTWAITRESET
                State = "Device has programmed its memories and is waiting for a USB reset or a power on reset."
            Case DFU_STATE_CODE.dfuUPLOAD_IDLE
                State = "The device is processing an upload operation. Expecting DFU_UPLOAD requests."
        End Select
        Return State
    End Function

    Private Function DfuStatusToString(StatusCode As DFU_STATUS_CODE) As String
        Dim ErrorReason As String = ""
        Select Case StatusCode
            Case DFU_STATUS_CODE.OK
                ErrorReason = "No error condition is present"
            Case DFU_STATUS_CODE.errADDRESS
                ErrorReason = "Cannot program memory due to received address that is out of range"
            Case DFU_STATUS_CODE.errCHECK_ERASED
                ErrorReason = "Memory erase check failed"
            Case DFU_STATUS_CODE.errERASE
                ErrorReason = "Memory erase function failed"
            Case DFU_STATUS_CODE.errFILE
                ErrorReason = "File is for this device but fails some vendor-specific verification test"
            Case DFU_STATUS_CODE.errFIRMWARE
                ErrorReason = "Device’s firmware is corrupted. It cannot return to run-time operations"
            Case DFU_STATUS_CODE.errNOTDONE
                ErrorReason = "Received DFU_DNLOAD with wLength = 0, but device does not think it has all thedata yet."
            Case DFU_STATUS_CODE.errPOR
                ErrorReason = "Device detected unexpected power on reset"
            Case DFU_STATUS_CODE.errPROG
                ErrorReason = "Program memory function failed"
            Case DFU_STATUS_CODE.errSTALLEDPK
                ErrorReason = "Device stalled an unexpected request"
            Case DFU_STATUS_CODE.errTARGET
                ErrorReason = "File is not targeted for use by this device"
            Case DFU_STATUS_CODE.errUNKNOWN
                ErrorReason = "Something went wrong, but the device does not know what it was"
            Case DFU_STATUS_CODE.errUSBR
                ErrorReason = "Device detected unexpected USB reset signaling"
            Case DFU_STATUS_CODE.errVENDOR
                ErrorReason = "iString indicates a vendor-specific error"
            Case DFU_STATUS_CODE.errVERIFY
                ErrorReason = "Programmed memory failed verification"
            Case DFU_STATUS_CODE.errWRITE
                ErrorReason = "Device id unable to write memory"
        End Select
        Return ErrorReason
    End Function
    'Prepares the usb send packate containing header+firmware+suffix
    Private Function PrepareDnData(data() As Byte, start As Integer, ByRef endaddress As Integer) As Byte()
        Dim DataSize As Integer = 512 '512 bytes per packet of fw data
        If (start + DataSize) > data.Length Then
            DataSize = data.Length - start
        End If
        endaddress = (start + DataSize) - 1
        Dim RetData(DataSize + 47) As Byte
        RetData(0) = 1
        RetData(2) = CByte((start And &HFF00) >> 8)
        RetData(3) = CByte((start And &HFF))
        RetData(4) = CByte((endaddress And &HFF00) >> 8)
        RetData(5) = CByte((endaddress And &HFF))
        Array.Copy(data, start, RetData, 32, DataSize)
        Return RetData
    End Function

    Private Function GetStatus(ByRef buff() As Byte) As Boolean
        Try
            Return Me.FCUSB.USB_CONTROL_MSG_IN(CType(DFU_OPCODE.GETSTATUS, USB.USBREQ), buff)
        Catch ex As Exception
        End Try
        Return False
    End Function

    Private Function SendData(data() As Byte) As Boolean
        Try
            Dim setup_data As UInt32 = (CUInt(Math.Max(Threading.Interlocked.Increment(transaction), transaction - 1)) << 16)
            Return Me.FCUSB.USB_CONTROL_MSG_OUT(CType(DFU_OPCODE.DNLOAD, USB.USBREQ), data, setup_data)
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function GetData(ByRef data() As Byte) As Boolean
        Try
            Dim setup_data As UInt32 = (CUInt(Math.Max(Threading.Interlocked.Increment(transaction), transaction - 1)) << 16)
            Return Me.FCUSB.USB_CONTROL_MSG_IN(CType(DFU_OPCODE.UPLOAD, USB.USBREQ), data, setup_data)
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function ClearStatus() As Boolean
        Try
            transaction = 0
            Return Me.FCUSB.USB_CONTROL_MSG_OUT(CType(DFU_OPCODE.CLRSTATUS, USB.USBREQ))
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function GetState() As Integer
        Try
            Dim s_byte(0) As Byte
            Me.FCUSB.USB_CONTROL_MSG_IN(CType(DFU_OPCODE.GETSTATE, USB.USBREQ), s_byte)
            Return s_byte(0)
        Catch ex As Exception
            Return -1 'Error
        End Try
    End Function

    Private Function Abort() As Boolean
        Try
            Return Me.FCUSB.USB_CONTROL_MSG_OUT(CType(DFU_OPCODE.ABORT, USB.USBREQ))
        Catch ex As Exception
            Return False
        End Try
    End Function

End Class
