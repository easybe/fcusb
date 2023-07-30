Imports FlashcatUSB.PARALLEL_NOR_NAND
Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB
Imports FlashcatUSB.USB.HostClient

Public Class HF_Port : Implements MemoryDeviceUSB
    Private FCUSB As FCUSB_DEVICE
    Public Property MyFlashDevice As HF_Flash  'Contains the definition for the EXT I/O device that is connected
    Public Property MyFlashStatus As DeviceStatus = DeviceStatus.NotDetected

    Public Event PrintConsole(ByVal msg As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(ByVal percent As Integer) Implements MemoryDeviceUSB.SetProgress 'Not used

    Sub New(ByVal parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        FCUSB.USB_VCC_OFF()
        MyFlashDevice = Nothing
        EXPIO_SETUP_USB(EXPIO_Mode.HYPERFLASH)
        EXPIO_SETUP_READIDENT(E_EXPIO_IDENT.HF)
        EXPIO_SETUP_CHIPERASE(E_EXPIO_CHIPERASE.HF)
        EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.HF)
        If MySettings.VOLT_SELECT = Voltage.V1_8 Then
            FCUSB.USB_VCC_1V8()
        ElseIf MySettings.VOLT_SELECT = Voltage.V3_3 Then
            FCUSB.USB_VCC_3V()
        End If
        Utilities.Sleep(200)
        Dim HF_DETECT As FlashDetectResult = DetectFlash()
        If HF_DETECT.Successful Then
            Dim chip_id_str As String = Hex(HF_DETECT.MFG).PadLeft(2, "0") & Hex(HF_DETECT.ID1).PadLeft(8, "0")
            RaiseEvent PrintConsole(String.Format(RM.GetString("ext_connected_chipid"), chip_id_str))
            Dim device_matches() As Device = FlashDatabase.FindDevices(HF_DETECT.MFG, HF_DETECT.ID1, 0, MemoryType.HYPERFLASH)
            If (device_matches IsNot Nothing AndAlso device_matches.Count > 0) Then
                If MyFlashDevice Is Nothing Then MyFlashDevice = device_matches(0)
                RaiseEvent PrintConsole(String.Format(RM.GetString("flash_detected"), MyFlashDevice.NAME, Format(MyFlashDevice.FLASH_SIZE, "#,###")))
                MyFlashStatus = DeviceStatus.Supported
            Else
                RaiseEvent PrintConsole(RM.GetString("unknown_device_email"))
                MyFlashStatus = DeviceStatus.NotSupported
            End If
            Return True
        Else
            GUI.PrintConsole(RM.GetString("ext_not_detected"))
            MyFlashStatus = DeviceStatus.NotDetected
            Return False
        End If
    End Function

#Region "Public Interface"

    Friend ReadOnly Property DeviceName() As String Implements MemoryDeviceUSB.DeviceName
        Get
            Select Case MyFlashStatus
                Case DeviceStatus.Supported
                    Return MyFlashDevice.NAME
                Case DeviceStatus.NotSupported
                    Return Hex(MyFlashDevice.MFG_CODE).PadLeft(2, CChar("0")) & " " & Hex(MyFlashDevice.ID1).PadLeft(4, CChar("0"))
                Case Else
                    Return RM.GetString("no_flash_detected")
            End Select
        End Get
    End Property

    Friend ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
        Get
            Return MyFlashDevice.FLASH_SIZE
        End Get
    End Property

    Public Function ReadData(ByVal logical_address As Long, ByVal data_count As UInt32, Optional ByVal memory_area As FlashArea = FlashArea.Main) As Byte() Implements MemoryDeviceUSB.ReadData
        Return ReadBulk_NOR(logical_address, data_count)
    End Function

    Public Function SectorErase(ByVal sector_index As UInt32, Optional ByVal memory_area As FlashArea = FlashArea.Main) As Boolean Implements MemoryDeviceUSB.SectorErase
        Try
            If sector_index = 0 AndAlso SectorSize(0) = MyFlashDevice.FLASH_SIZE Then
                Return EraseDevice() 'Single sector, must do a full chip erase instead
            Else
                Dim Logical_Address As UInt32 = 0
                If (sector_index > 0) Then
                    For i As UInt32 = 0 To sector_index - 1
                        Dim s_size As UInt32 = SectorSize(i)
                        Logical_Address += s_size
                    Next
                End If
                Dim Result As Boolean = False
                Try
                    Dim setup_data() As Byte = GetSetupPacket(Logical_Address, 0, 0)
                    Result = FCUSB.USB_SETUP_BULKOUT(USBREQ.EXPIO_SECTORERASE, setup_data, Nothing, 0)
                Catch ex As Exception
                End Try
                If Not Result Then Return False
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WAIT) 'Calls the assigned WAIT function (uS, mS, SR, DQ7)
                FCUSB.USB_WaitForComplete() 'Checks for WAIT flag to clear
                Dim blank_result As Boolean = False
                Dim timeout As UInt32 = 0
                Do Until blank_result
                    Dim sr As Byte = GetStatusRegister()
                    If ((sr >> 7) And 1) = 1 Then Exit Do
                    timeout += 1
                    If (timeout = 10) Then Return False
                    If Not blank_result Then Utilities.Sleep(100)
                Loop
            End If
        Catch
        End Try
        Return True
    End Function

    Public Function WriteData(ByVal logical_address As Long, ByVal data_to_write() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        Try
            Dim ReturnValue As Boolean
            Dim DataToWrite As UInt32 = data_to_write.Length
            Dim PacketSize As UInt32 = 8192 'Possibly /2 for IsFlashX8Mode
            Dim Loops As Integer = CInt(Math.Ceiling(DataToWrite / PacketSize)) 'Calcuates iterations
            For i As Integer = 0 To Loops - 1
                Dim BufferSize As Integer = DataToWrite
                If (BufferSize > PacketSize) Then BufferSize = PacketSize
                Dim data(BufferSize - 1) As Byte
                Array.Copy(data_to_write, (i * PacketSize), data, 0, data.Length)
                ReturnValue = WriteBulk_NOR(logical_address, data)
                If (Not ReturnValue) Then Return False
                logical_address += data.Length
                DataToWrite -= data.Length
                FCUSB.USB_WaitForComplete()
            Next
            WaitForReady()
        Catch ex As Exception
        End Try
        Return True
    End Function

    Public Sub WaitForReady() Implements MemoryDeviceUSB.WaitUntilReady
        Try
            FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WAIT)
            FCUSB.USB_WaitForComplete() 'Checks for WAIT flag to clear
        Catch ex As Exception
        End Try
    End Sub

    Public Function GetStatusRegister() As Byte
        Dim d(0) As Byte
        If Not FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_SR, d) Then Return 0
        Return d(0)
    End Function

    Public Function SectorFind(ByVal sector_index As UInt32, Optional ByVal memory_area As FlashArea = FlashArea.Main) As Long Implements MemoryDeviceUSB.SectorFind
        Dim base_addr As UInt32 = 0
        If sector_index > 0 Then
            For i As UInt32 = 0 To sector_index - 1
                base_addr += Me.SectorSize(i, memory_area)
            Next
        End If
        Return base_addr
    End Function

    Public Function SectorWrite(ByVal sector_index As UInt32, ByVal data() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.SectorWrite
        Dim Addr32 As UInteger = Me.SectorFind(sector_index, Params.Memory_Area)
        Return WriteData(Addr32, data, Params)
    End Function

    Public Function SectorCount() As UInt32 Implements MemoryDeviceUSB.SectorCount
        Return MyFlashDevice.SECTOR_COUNT
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        Try
            Try
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CHIPERASE)
                Utilities.Sleep(1000) 'Perform blank check
                For i = 0 To 119 '2 minutes
                    Dim sr As Byte = GetStatusRegister()
                    If ((sr >> 7) And 1) = 1 Then Return True
                    Utilities.Sleep(1000)
                Next
                Return False 'Timeout (device erase failed)
            Catch ex As Exception
            End Try
        Catch ex As Exception
        End Try
        Return False
    End Function

    Friend ReadOnly Property SectorSize(ByVal sector As UInt32, Optional ByVal memory_area As FlashArea = FlashArea.Main) As UInt32 Implements MemoryDeviceUSB.SectorSize
        Get
            Return MyFlashDevice.SECTOR_SIZE
        End Get
    End Property

#End Region

#Region "EXPIO_SETUP"

    Private Function EXPIO_SETUP_USB(ByVal mode As EXPIO_Mode) As Boolean
        Try
            Dim result_data(0) As Byte
            Dim setup_data As UInt32 = mode Or (10 << 16)
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_INIT, result_data, setup_data)
            Return result
        Catch ex As Exception
        End Try
        Return False
    End Function

    Private Function EXPIO_SETUP_READIDENT(ByVal mode As E_EXPIO_IDENT) As Boolean
        Try
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.EXPIO_MODE_IDENT, Nothing, mode)
            Threading.Thread.Sleep(25)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_CHIPERASE(ByVal mode As E_EXPIO_CHIPERASE) As Boolean
        Try
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.EXPIO_MODE_ERCHP, Nothing, mode)
            Threading.Thread.Sleep(25)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_ERASESECTOR(ByVal mode As E_EXPIO_SECTOR) As Boolean
        Try
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.EXPIO_MODE_ERSCR, Nothing, mode)
            Threading.Thread.Sleep(25)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

#End Region

    Private Function DetectFlash() As FlashDetectResult
        Dim result As New FlashDetectResult
        result.Successful = False
        Try
            Dim ident_data(7) As Byte 'Contains 8 bytes
            If Not FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_RDID, ident_data) Then Return result
            If ident_data(0) = 0 AndAlso ident_data(2) = 0 Then Return result '0x0000
            If ident_data(0) = &H90 AndAlso ident_data(2) = &H90 Then Return result '0x9090 
            If ident_data(0) = &H90 AndAlso ident_data(2) = 0 Then Return result '0x9000 
            If ident_data(0) = &HFF AndAlso ident_data(2) = &HFF Then Return result '0xFFFF 
            If ident_data(0) = &HFF AndAlso ident_data(2) = 0 Then Return result '0xFF00
            If ident_data(0) = &H1 AndAlso ident_data(1) = 0 AndAlso ident_data(2) = &H1 AndAlso ident_data(3) = 0 Then Return result '0x01000100
            result.MFG = ident_data(0)
            result.ID1 = (CUInt(ident_data(1)) << 8) Or CUInt(ident_data(2))
            result.ID2 = (CUInt(ident_data(3)) << 8) Or CUInt(ident_data(4))
            If result.ID1 = 0 AndAlso result.ID2 = 0 Then Return result
            result.BOOT_LOCK = ident_data(5)
            result.SUPPORTED = ident_data(6)
            result.CFI_MULTI = ident_data(7)
            result.Successful = True
        Catch ex As Exception
        End Try
        Return result
    End Function

    Private Function GetSetupPacket(ByVal Address As UInt32, ByVal Count As UInt32, ByVal PageSize As UInt16) As Byte()
        Dim addr_bytes As Byte = 0
        Dim data_in(19) As Byte '18 bytes total
        data_in(0) = CByte(Address And 255)
        data_in(1) = CByte((Address >> 8) And 255)
        data_in(2) = CByte((Address >> 16) And 255)
        data_in(3) = CByte((Address >> 24) And 255)
        data_in(4) = CByte(Count And 255)
        data_in(5) = CByte((Count >> 8) And 255)
        data_in(6) = CByte((Count >> 16) And 255)
        data_in(7) = CByte((Count >> 24) And 255)
        data_in(8) = CByte(PageSize And 255) 'This is how many bytes to increment between operations
        data_in(9) = CByte((PageSize >> 8) And 255)
        Return data_in
    End Function

    Private Function ReadBulk_NOR(ByVal address As UInt32, ByVal count As UInt32) As Byte()
        Try
            Dim read_count As UInt32 = count
            Dim addr_offset As Boolean = False
            Dim count_offset As Boolean = False
            If (address Mod 2 = 1) Then
                addr_offset = True
                address = (address - 1)
                read_count += 1
            End If
            If (read_count Mod 2 = 1) Then
                count_offset = True
                read_count += 1
            End If
            Dim setup_data() As Byte = GetSetupPacket(address, read_count, MyFlashDevice.PAGE_SIZE)
            Dim data_out(read_count - 1) As Byte 'Bytes we want to read
            Dim result As Boolean = FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_READDATA, setup_data, data_out, 0)
            If Not result Then Return Nothing
            If addr_offset Then
                Dim new_data(count - 1) As Byte
                Array.Copy(data_out, 1, new_data, 0, new_data.Length)
                data_out = new_data
            Else
                ReDim Preserve data_out(count - 1)
            End If
            Return data_out
        Catch ex As Exception
        End Try
        Return Nothing
    End Function

    Private Function WriteBulk_NOR(ByVal address As UInt32, ByVal data_out() As Byte) As Boolean
        Try
            Dim setup_data() As Byte = GetSetupPacket(address, data_out.Length, MyFlashDevice.PAGE_SIZE)
            Dim result As Boolean = FCUSB.USB_SETUP_BULKOUT(USBREQ.EXPIO_WRITEDATA, setup_data, data_out, 0)
            Return result
        Catch ex As Exception
        End Try
        Return False
    End Function

End Class
