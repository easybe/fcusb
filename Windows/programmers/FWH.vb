'COPYRIGHT EMBEDDED COMPUTERS LLC 2021 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB PRODUCTS
'CONTACT EMAIL: support@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: This object interfaces FCUSB with a Firmware Hub memory device using LPC protocol

Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB

Public Class FWH_Programmer : Implements MemoryDeviceUSB
    Private FCUSB As FCUSB_DEVICE
    Public Property MyFlashDevice As FWH
    Public Property MyFlashStatus As DeviceStatus = DeviceStatus.NotDetected
    Private FLASH_IDENT As FlashDetectResult

    Public Event PrintConsole(message As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress

    Sub New(parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        Me.MyFlashStatus = DeviceStatus.NotDetected
        Dim ident_data(7) As Byte
        Dim result_data(0) As Byte
        If Not FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_INIT, result_data, MEM_PROTOCOL.FWH) Then Return False
        If Not (result_data(0) = &H17) Then Return False
        FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_RDID, ident_data)
        Me.FLASH_IDENT = GetFlashResult(ident_data)
        If Not FLASH_IDENT.Successful Then Return False
        Dim part As UInt32 = (CUInt(FLASH_IDENT.ID1) << 16) Or (FLASH_IDENT.ID2)
        Dim chip_id_str As String = Hex(FLASH_IDENT.MFG).PadLeft(2, "0"c) & Hex(part).PadLeft(8, "0"c)
        RaiseEvent PrintConsole("Mode FWH returned ident code: 0x" & chip_id_str)
        Dim DevList() As Device = FlashDatabase.FindDevices(Me.FLASH_IDENT.MFG, Me.FLASH_IDENT.ID1, 0, MemoryType.FWH_NOR)
        If (DevList.Length = 1) Then
            RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "FWH"))
            MyFlashDevice = CType(DevList(0), FWH)
            MyFlashStatus = DeviceStatus.Supported
            Return True
        Else
            MyFlashDevice = Nothing
            MyFlashStatus = DeviceStatus.NotSupported
            Return False
        End If
    End Function

    Public ReadOnly Property GetDevice As Device Implements MemoryDeviceUSB.GetDevice
        Get
            Return Me.MyFlashDevice
        End Get
    End Property

    Public ReadOnly Property DeviceName As String Implements MemoryDeviceUSB.DeviceName
        Get
            Return MyFlashDevice.NAME
        End Get
    End Property

    Public ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
        Get
            Return MyFlashDevice.FLASH_SIZE
        End Get
    End Property

    Public Function SectorSize(sector As Integer) As Integer Implements MemoryDeviceUSB.SectorSize
        If MyFlashDevice Is Nothing Then Return 0
        Return DirectCast(MyFlashDevice, FWH).SECTOR_SIZE
    End Function

    Public Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
        Utilities.Sleep(100)
    End Sub

    Public Function ReadData(flash_offset As Long, data_count As Integer) As Byte() Implements MemoryDeviceUSB.ReadData
        Dim data_out(data_count - 1) As Byte
        Dim ptr As Integer = 0
        Dim bytes_left As Integer = data_count
        Dim PacketSize As Integer = 2048
        While (bytes_left > 0)
            Dim BufferSize As Integer = bytes_left
            If (BufferSize > PacketSize) Then BufferSize = PacketSize
            Dim data() As Byte = ReadBulk_NOR(CUInt(flash_offset), BufferSize)
            If data Is Nothing Then Return Nothing
            Array.Copy(data, 0, data_out, ptr, BufferSize)
            flash_offset += data.Length
            bytes_left -= data.Length
            ptr += data.Length
        End While
        Return data_out
    End Function

    Public Function WriteData(flash_offset As Long, data_to_write() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        Try
            Dim PacketSize As Integer = 4096
            Dim DataToWrite As Integer = data_to_write.Length
            Dim Loops As Integer = CInt(Math.Ceiling(DataToWrite / PacketSize)) 'Calcuates iterations
            For i As Integer = 0 To Loops - 1
                If Params IsNot Nothing Then If Params.AbortOperation Then Return False
                Dim BufferSize As Integer = DataToWrite
                If (BufferSize > PacketSize) Then BufferSize = PacketSize
                Dim data(BufferSize - 1) As Byte
                Array.Copy(data_to_write, (i * PacketSize), data, 0, data.Length)
                Dim ReturnValue As Boolean = WriteBulk_NOR(CUInt(flash_offset), data)
                If (Not ReturnValue) Then Return False
                FCUSB.USB_WaitForComplete()
                flash_offset += data.Length
                DataToWrite -= data.Length
            Next
        Catch ex As Exception
        End Try
        Return True
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        RaiseEvent SetProgress(0)
        Dim sec_count As Integer = Me.SectorCount()
        Try
            For i = 0 To sec_count - 1
                If Not SectorErase(i) Then Return False
                RaiseEvent SetProgress(CInt((i + 1) / sec_count) * 100)
            Next
        Catch ex As Exception
            Return False
        Finally
            RaiseEvent SetProgress(0)
        End Try
        Return True
    End Function

    Public Function SectorFind(SectorIndex As Integer) As Long Implements MemoryDeviceUSB.SectorFind
        Dim base_addr As UInt32 = 0
        If SectorIndex > 0 Then
            For i As Integer = 0 To SectorIndex - 1
                base_addr += CUInt(Me.SectorSize(i))
            Next
        End If
        Return base_addr
    End Function

    Public Function SectorErase(SectorIndex As Integer) As Boolean Implements MemoryDeviceUSB.SectorErase
        Dim Result As Boolean = False
        Dim Logical_Address As UInt32 = 0
        If (SectorIndex > 0) Then
            For i As Integer = 0 To SectorIndex - 1
                Dim s_size As Integer = SectorSize(i)
                Logical_Address += CUInt(s_size)
            Next
        End If
        Dim erase_setup As UInt32 = (CUInt(MyFlashDevice.ERASE_CMD) << 24) Or Logical_Address
        Result = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_SECTORERASE, Nothing, erase_setup)
        If Not Result Then Return False
        Utilities.Sleep(50)
        Return BlankCheck(Logical_Address)
    End Function

    Public Function SectorCount() As Integer Implements MemoryDeviceUSB.SectorCount
        Return MyFlashDevice.Sector_Count
    End Function

    Public Function SectorWrite(SectorIndex As Integer, data() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.SectorWrite
        Dim Addr32 As Long = Me.SectorFind(SectorIndex)
        Return WriteData(Addr32, data, Params)
    End Function

    Private Function BlankCheck(base_addr As UInt32) As Boolean
        Try
            Dim IsBlank As Boolean = False
            Dim Counter As Integer = 0
            Do Until IsBlank
                Utilities.Sleep(10)
                Dim w() As Byte = ReadData(base_addr, 4)
                If w Is Nothing Then Return False
                If w(0) = 255 AndAlso w(1) = 255 AndAlso w(2) = 255 AndAlso w(3) = 255 Then IsBlank = True
                Counter += 1
                If Counter = 50 Then Return False 'Timeout (500 ms)
            Loop
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function ReadBulk_NOR(address As UInt32, count As Integer) As Byte()
        Try
            Dim read_count As Integer = count
            Dim addr_offset As Boolean = False
            Dim data_out(read_count - 1) As Byte 'Bytes we want to read
            Dim page_size As UInt16 = 512
            If MyFlashDevice IsNot Nothing Then page_size = MyFlashDevice.PAGE_SIZE
            Dim setup_data() As Byte = GetSetupPacket(address, read_count, page_size)
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
            Dim setup_data() As Byte = GetSetupPacket(address, data_out.Length, MyFlashDevice.PAGE_SIZE)
            Dim result As Boolean = FCUSB.USB_SETUP_BULKOUT(USBREQ.EXPIO_WRITEDATA, setup_data, data_out, 0)
            Return result
        Catch ex As Exception
        End Try
        Return False
    End Function

    Private Function GetSetupPacket(Address As UInt32, Count As Integer, PageSize As UInt16) As Byte()
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

End Class
