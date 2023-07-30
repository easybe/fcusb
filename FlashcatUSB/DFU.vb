'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2019 - ALL RIGHTS RESERVED
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: This class interfaces the DFU protocol bootloader

Imports FlashcatUSB.USB.HostClient
Imports LibUsbDotNet.Main

Public Class DFU_API
    Public FCUSB As FCUSB_DEVICE

    Private Const USB_VID_ATMEL As Integer = &H3EB
    Private Const USB_PID_AT90USB162 As Integer = &H2FFA 'FCUSB PCB 1.x
    Private Const USB_PID_AT90USB1287 As Integer = &H2FFB 'FCUSB EX (PROTO)
    Private Const USB_PID_AT90USB646 As Integer = &H2FF9 'FCUSB EX (PRODUCTION)
    Private Const USB_PID_ATMEGA32U2 As Integer = &H2FF0 'FCUSB PCB 2.x

    Sub New(ByVal parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
    End Sub

    Private Enum DFU_OPCODE As Byte
        DETACH = 0
        DNLOAD = 1
        UPLOAD = 2
        GETSTATUS = 3
        CLRSTATUS = 4
        GETSTATE = 5
        ABORT = 6
    End Enum

    Private Const USB_DT_DEVICE As Byte = 1
    Private transaction As Integer = 0

    Public Event OnStatus(ByVal Percent As Integer)

    Structure DFU_STATUS
        Dim Err As Boolean 'True if device failed to retrieve this object
        Dim StatusCode As DFU_STATUS_CODE 'The status code
        Dim Timeout As Integer 'Minimum time in milliseconds that the host should wait
        Dim State As DFU_STATE_CODE
        Dim iString As Integer 'Index of status description in string table.
    End Structure

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

    Public USB_VERSION As Integer '0x0200 = 2.0
    Public MAX_PCK As Integer 'Max packet size for endpoint zero (limited to 32 due to Host side driver)
    Public VEND_ID As Integer
    Public PRD_ID As Integer
    Public IfIndex As Integer 'bInterfaceNumber

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
    'Returns the size of the Atmel flash
    Public Function GetFlashSize() As Integer
        Select Case FCUSB.USBHANDLE.Info.Descriptor.ProductID
            Case USB_PID_AT90USB162
                Return 12288 '0 to 0x2FFF (16KB total)
            Case USB_PID_AT90USB1287 '0x0000 - 0xEFFF
                Return 122880'(120KB data, 8KB bootloader)
            Case USB_PID_AT90USB646 '0x0000 - 0x77FF WORD
                Return 61440 '(60KB, 4KB bootloader)
            Case USB_PID_ATMEGA32U2
                Return 28672 '0 to 0x2FFF (32KB total, 4KB bootloader)
            Case Else
                Return 0
        End Select
    End Function
    'Returns the number of bytes allowed for the bootloader
    Public Function GetBootloaderSize() As Integer
        Select Case FCUSB.USBHANDLE.Info.Descriptor.ProductID
            Case USB_PID_AT90USB162
                Return 4096 '(4KB total)
            Case USB_PID_AT90USB1287
                Return 8192 '(8KB total)
            Case USB_PID_AT90USB646
                Return 4096 '(4KB total)
            Case USB_PID_ATMEGA32U2
                Return 4096 '(4KB total)
            Case Else
                Return 0
        End Select
    End Function

    Public Function GetAtmelPart() As String
        Return FCUSB.USBHANDLE.Info.ProductString
    End Function
    'Reads the entire flash from FCUSB (12KB)
    Public Function ReadFlash() As Boolean
        'Not impletemented yet
        Return False
    End Function
    'Starts the application
    Public Function RunApp() As Boolean
        Dim Res As Integer = 0
        Res = SendData(New Byte() {4, 3, 0}) 'Start App command
        Dim UsbStatus As DFU_STATUS = GetStatus()
        If Not UsbStatus.StatusCode = DFU_STATUS_CODE.OK Then
            PrintErrorMsg(UsbStatus)
            ClearStatus()
            Return False
        End If
        Return True
    End Function
    'Erases the flash (not bootloader section)
    Public Function EraseFlash() As Boolean
        Dim Res As Integer = 0
        Res = SendData(New Byte() {4, 0, 255}) 'Chip Erase command
        Dim UsbStatus As DFU_STATUS = GetStatus()
        If Not UsbStatus.StatusCode = DFU_STATUS_CODE.OK Then
            PrintErrorMsg(UsbStatus)
            ClearStatus()
            Return False
        End If
        Return True
    End Function

    Public Function WriteFlash(ByVal data() As Byte) As Boolean
        RaiseEvent OnStatus(0)
        Dim UsbStatus As DFU_STATUS
        Dim EndAddress As Integer = data.Length
        Dim pEnd As Integer = 0
        Dim currentAddress As Integer = 0 'Test address of flash
        Dim Packet() As Byte
        Do Until currentAddress = EndAddress
            RaiseEvent OnStatus(CInt((currentAddress / EndAddress) * 100))
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
        RaiseEvent OnStatus(100)
        SendData(Nothing) 'End of firmware transmission
        Return True
    End Function

    Private Sub PrintErrorMsg(ByVal input As DFU_STATUS)
        Dim State As String = DfuStateToString(input.State)
        Dim ErrorReason As String = DfuStatusToString(input.StatusCode)
        WriteConsole("AVR DFU Error")
        WriteConsole("State: " & State)
        WriteConsole("Status: " & ErrorReason)
    End Sub

    Private Function DfuStateToString(ByVal StateVal As DFU_STATE_CODE) As String
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

    Private Function DfuStatusToString(ByVal StatusCode As DFU_STATUS_CODE) As String
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
    Private Function PrepareDnData(ByVal data() As Byte, ByVal start As Integer, ByRef endaddress As Integer) As Byte()
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

    Private Function GetDfuSuffix() As Byte()
        Dim ret(15) As Byte
        ret(4) = 16 'Size of this suffix
        ret(5) = &H44
        ret(6) = &H46
        ret(7) = &H55
        ret(8) = &H1
        ret(9) = &H0
        ret(10) = &HFF
        ret(11) = &HFF
        ret(12) = &HFF
        ret(13) = &HFF
        ret(14) = &HFF
        ret(15) = &HFF
        Return ret
    End Function

    Public Function GetStatus(ByRef buff() As Byte) As Boolean
        Try
            Return USB_IF_IN(DFU_OPCODE.GETSTATUS, buff)
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Function SendData(ByRef data() As Byte) As Boolean
        Try
            Dim setup_data As UInt32 = (CUInt(Math.Max(Threading.Interlocked.Increment(transaction), transaction - 1)) << 16)
            If data Is Nothing Then
                Return USB_IF_OUT(DFU_OPCODE.DNLOAD, Nothing, setup_data)
            Else
                Return USB_IF_OUT(DFU_OPCODE.DNLOAD, data, setup_data)
            End If
        Catch ex As Exception
            Return False
        End Try
    End Function

    'Sends a control message with an optional byte buffer to write
    Public Function USB_IF_OUT(RQ As Byte, Optional buffer_out() As Byte = Nothing, Optional ByVal data As UInt32 = 0) As Boolean
        Try
            If FCUSB.USBHANDLE Is Nothing Then Return False
            Dim wValue As UInt16 = (data And &HFFFF0000UI) >> 16
            Dim wIndex As UInt16 = (data And &HFFFF)
            Dim count_out As Short = 0
            If buffer_out IsNot Nothing Then count_out = CShort(buffer_out.Length)
            Dim usb_res As Boolean
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Class Or UsbCtrlFlags.Recipient_Interface)
            Dim usbSetupPacket As New UsbSetupPacket(usbflag, RQ, wValue, wIndex, count_out)
            Dim bytes_xfer As Integer = 0
            If buffer_out Is Nothing Then
                usb_res = FCUSB.USBHANDLE.ControlTransfer(usbSetupPacket, Nothing, 0, bytes_xfer)
            Else
                usb_res = FCUSB.USBHANDLE.ControlTransfer(usbSetupPacket, buffer_out, buffer_out.Length, bytes_xfer)
            End If
            Return usb_res
        Catch ex As Exception
            Return False
        End Try
    End Function
    'Sends a control message with a byte buffer to receive data
    Public Function USB_IF_IN(RQ As Byte, ByRef Buffer_in() As Byte, Optional ByVal data As UInt32 = 0) As Boolean
        Try
            If FCUSB.USBHANDLE Is Nothing Then Return False
            Dim wValue As UInt16 = (data And &HFFFF0000UI) >> 16
            Dim wIndex As UInt16 = (data And &HFFFF)
            Dim bytes_xfer As Integer = 0
            Dim usb_res As Boolean
            Dim usb_flag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Class Or UsbCtrlFlags.Recipient_Interface)
            Dim usb_setup As New UsbSetupPacket(usb_flag, RQ, wValue, wIndex, CShort(Buffer_in.Length))
            usb_res = FCUSB.USBHANDLE.ControlTransfer(usb_setup, Buffer_in, Buffer_in.Length, bytes_xfer)
            Return usb_res
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function ReadData(ByRef data() As Byte) As Boolean
        Try
            Dim setup_data As UInt32 = (CShort(Math.Max(Threading.Interlocked.Increment(transaction), transaction - 1)) << 16)
            Return USB_IF_IN(DFU_OPCODE.UPLOAD, data, setup_data)
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function ClearStatus() As Boolean
        Try
            transaction = 0
            Return USB_IF_OUT(DFU_OPCODE.CLRSTATUS)
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function GetState() As Integer
        Try
            Dim s_byte(0) As Byte
            Return USB_IF_IN(DFU_OPCODE.GETSTATE, s_byte)
            Return s_byte(0)
        Catch ex As Exception
            Return -1 'Error
        End Try
    End Function

    Public Function Abort() As Boolean
        Try
            Return USB_IF_OUT(DFU_OPCODE.ABORT)
        Catch ex As Exception
            Return False
        End Try
    End Function

End Class
