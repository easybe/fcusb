Imports FlashcatUSB.PARALLEL_NOR
Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB
Imports FlashcatUSB.USB.HostClient

Public Class HF_Programmer : Implements MemoryDeviceUSB
    Private FCUSB As FCUSB_DEVICE
    Public Property MyFlashDevice As HYPERFLASH  'Contains the definition for the parallel mode device that is connected
    Public Property MyFlashStatus As DeviceStatus = DeviceStatus.NotDetected

    Public Event PrintConsole(msg As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress 'Not used

    Sub New(parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        FCUSB.USB_VCC_OFF()
        MyFlashDevice = Nothing
        EXPIO_SETUP_USB(MEM_PROTOCOL.HYPERFLASH)
        FCUSB.USB_VCC_ON(MySettings.VOLT_SELECT)
        Utilities.Sleep(200)
        Dim HF_DETECT As FlashDetectResult = DetectFlash()
        If HF_DETECT.Successful Then
            Dim chip_id_str As String = Hex(HF_DETECT.MFG).PadLeft(2, "0"c) & Hex(HF_DETECT.ID1).PadLeft(8, "0"c)
            RaiseEvent PrintConsole(String.Format(RM.GetString("ext_connected_chipid"), chip_id_str))
            Dim device_matches() As Device = FlashDatabase.FindDevices(HF_DETECT.MFG, HF_DETECT.ID1, 0, MemoryType.HYPERFLASH)
            If (device_matches IsNot Nothing AndAlso device_matches.Length > 0) Then
                If MyFlashDevice Is Nothing Then MyFlashDevice = CType(device_matches(0), HYPERFLASH)
                RaiseEvent PrintConsole(String.Format(RM.GetString("flash_detected"), MyFlashDevice.NAME, Format(MyFlashDevice.FLASH_SIZE, "#,###")))
                MyFlashStatus = DeviceStatus.Supported
            Else
                MyFlashStatus = DeviceStatus.NotSupported
            End If
            Return True
        Else
            MyFlashStatus = DeviceStatus.NotDetected
            Return False
        End If
    End Function

#Region "Public Interface"

    Public ReadOnly Property GetDevice As Device Implements MemoryDeviceUSB.GetDevice
        Get
            Return Me.MyFlashDevice
        End Get
    End Property

    Friend ReadOnly Property DeviceName() As String Implements MemoryDeviceUSB.DeviceName
        Get
            Select Case MyFlashStatus
                Case DeviceStatus.Supported
                    Return MyFlashDevice.NAME
                Case DeviceStatus.NotSupported
                    Return Hex(MyFlashDevice.MFG_CODE).PadLeft(2, "0"c) & " " & Hex(MyFlashDevice.ID1).PadLeft(4, "0"c)
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

    Public Function ReadData(logical_address As Long, data_count As Integer) As Byte() Implements MemoryDeviceUSB.ReadData
        Return ReadBulk_NOR(CUInt(logical_address), CInt(data_count))
    End Function

    Public Function SectorErase(sector_index As Integer) As Boolean Implements MemoryDeviceUSB.SectorErase
        Try
            If sector_index = 0 AndAlso SectorSize(0) = MyFlashDevice.FLASH_SIZE Then
                Return EraseDevice() 'Single sector, must do a full chip erase instead
            Else
                Dim Logical_Address As UInt32 = 0
                If (sector_index > 0) Then
                    For i As Integer = 0 To sector_index - 1
                        Dim s_size As Integer = SectorSize(i)
                        Logical_Address += CUInt(s_size)
                    Next
                End If
                Dim Result As Boolean = False
                Try
                    Result = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_SECTORERASE, Nothing, Logical_Address)
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
                    timeout += 1UI
                    If (timeout = 10) Then Return False
                    If Not blank_result Then Utilities.Sleep(100)
                Loop
            End If
        Catch
        End Try
        Return True
    End Function

    Public Function WriteData(logical_address As Long, data_to_write() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        Try
            Dim ReturnValue As Boolean
            Dim DataToWrite As Integer = data_to_write.Length
            Dim PacketSize As Integer = 8192 'Possibly /2 for IsFlashX8Mode
            Dim Loops As Integer = CInt(Math.Ceiling(DataToWrite / PacketSize)) 'Calcuates iterations
            For i As Integer = 0 To Loops - 1
                Dim BufferSize As Integer = DataToWrite
                If (BufferSize > PacketSize) Then BufferSize = PacketSize
                Dim data(BufferSize - 1) As Byte
                Array.Copy(data_to_write, (i * PacketSize), data, 0, data.Length)
                ReturnValue = WriteBulk_NOR(CUInt(logical_address), data)
                If (Not ReturnValue) Then Return False
                logical_address += data.Length
                DataToWrite -= data.Length
                FCUSB.USB_WaitForComplete()
            Next
            WaitUntilReady()
        Catch ex As Exception
        End Try
        Return True
    End Function

    Public Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
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

    Public Function SectorFind(sector_index As Integer) As Long Implements MemoryDeviceUSB.SectorFind
        Dim base_addr As UInt32 = 0
        If sector_index > 0 Then
            For i As Integer = 0 To sector_index - 1
                base_addr += CUInt(Me.SectorSize(i))
            Next
        End If
        Return base_addr
    End Function

    Public Function SectorWrite(sector_index As Integer, data() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.SectorWrite
        Dim Addr32 As Long = Me.SectorFind(sector_index)
        Return WriteData(Addr32, data, Params)
    End Function

    Public Function SectorCount() As Integer Implements MemoryDeviceUSB.SectorCount
        Return MyFlashDevice.Sector_Count
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
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
        Return False
    End Function

    Friend Function SectorSize(sector As Integer) As Integer Implements MemoryDeviceUSB.SectorSize
        Return MyFlashDevice.SECTOR_SIZE
    End Function

#End Region

#Region "EXPIO_SETUP"

    Private Function EXPIO_SETUP_USB(mode As MEM_PROTOCOL) As Boolean
        Try
            Dim result_data(0) As Byte
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_INIT, result_data, CUInt(mode Or (10 << 16)))
            Return result
        Catch ex As Exception
        End Try
        Return False
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
            result.ID1 = (CUShort(ident_data(1)) << 8) Or CUShort(ident_data(2))
            result.ID2 = (CUShort(ident_data(3)) << 8) Or CUShort(ident_data(4))
            If result.ID1 = 0 AndAlso result.ID2 = 0 Then Return result
            result.Successful = True
        Catch ex As Exception
        End Try
        Return result
    End Function

    Private Function GetSetupPacket(Address As UInt32, Count As UInt32, PageSize As UInt16) As Byte()
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

    Private Function ReadBulk_NOR(address As UInt32, count As Integer) As Byte()
        Try
            Dim read_count As Integer = count
            Dim addr_offset As Boolean = False
            If (address Mod 2 = 1) Then
                addr_offset = True
                address = (address - 1UI)
                read_count += 1
            End If
            If (read_count Mod 2 = 1) Then
                read_count += 1
            End If
            Dim setup_data() As Byte = GetSetupPacket(address, CUInt(read_count), MyFlashDevice.PAGE_SIZE)
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

    Private Function WriteBulk_NOR(address As UInt32, data_out() As Byte) As Boolean
        Try
            Dim setup_data() As Byte = GetSetupPacket(address, CUInt(data_out.Length), MyFlashDevice.PAGE_SIZE)
            Dim result As Boolean = FCUSB.USB_SETUP_BULKOUT(USBREQ.EXPIO_WRITEDATA, setup_data, data_out, 0)
            Return result
        Catch ex As Exception
        End Try
        Return False
    End Function

End Class
