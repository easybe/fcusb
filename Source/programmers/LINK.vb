Imports FlashcatUSB.EC_ScriptEngine
Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB

Public Class LINK_Programmer : Implements MemoryDeviceUSB
    Private FCUSB As FCUSB_DEVICE
    Public Event PrintConsole(message As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress
    Public Property MyFlashDevice As Device
    Public Property MyFlashType As MemoryType = MemoryType.UNSPECIFIED

    Public PrefferedFlash As String = ""

    Public LinkFnc_Detect As ScriptEvent
    Public LinkFnc_Read As ScriptEvent
    Public LinkFnc_Write As ScriptEvent
    Public LinkFnc_EraseAll As ScriptEvent
    Public LinkFnc_EraseSector As ScriptEvent
    Public LinkFnc_Reset As ScriptEvent

    Sub New(parent_if As FCUSB_DEVICE)
        Me.FCUSB = parent_if
    End Sub

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

    Public Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
        Utilities.Sleep(50)
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        Dim vr As ScriptVariable = ScriptProcessor.Execute_Event(LinkFnc_Detect, Nothing)
        If vr Is Nothing OrElse Not vr.Data.VarType = DataType.Data Then
            RaiseEvent PrintConsole("Link: Detect function did not return data array")
            Return False
        End If
        Dim ID_DATA() As Byte = CType(vr.Value, Byte())
        If ID_DATA Is Nothing OrElse ID_DATA.Length < 2 Then
            RaiseEvent PrintConsole("Link: Detect data is not valid")
            Return False
        End If
        Dim MFG As Byte = ID_DATA(0)
        Dim ID1 As UInt16 = 0
        Dim ID2 As UInt16 = 0
        If (ID_DATA.Length = 2) Then
            ID1 = ID_DATA(1)
        Else
            ID1 = (CUShort(ID_DATA(1)) << 8) Or ID_DATA(2)
        End If
        If ID_DATA.Length = 5 Then
            ID2 = (CUShort(ID_DATA(3)) << 8) Or ID_DATA(4)
        End If
        Dim chip_id_str As String = Hex(MFG).PadLeft(2, "0"c) & Hex(CUInt(ID1) << 16 Or ID2).PadLeft(8, "0"c)
        Dim device_list() As Device = FlashDatabase.FindDevices(MFG, ID1, ID2, Me.MyFlashType)
        If device_list Is Nothing OrElse device_list.Count = 0 Then
            RaiseEvent PrintConsole("Link: Flash device not found: Chip ID 0x" & chip_id_str)
            Return False
        ElseIf device_list.Length = 1 Then
            MyFlashDevice = device_list(0)
        ElseIf Not PrefferedFlash = "" Then
            For Each n In device_list
                If n.NAME.ToUpper.EndsWith(PrefferedFlash.ToUpper) Then
                    MyFlashDevice = n
                    Exit For
                End If
            Next
        End If
        If MyFlashDevice Is Nothing Then MyFlashDevice = device_list(0)
        RaiseEvent PrintConsole(String.Format(RM.GetString("flash_detected"), MyFlashDevice.NAME, Format(MyFlashDevice.FLASH_SIZE, "#,###")))
        Return True
    End Function

    Public Function SectorSize(sector As Integer) As Integer Implements MemoryDeviceUSB.SectorSize
        If MyFlashType = MemoryType.PARALLEL_NOR Then
            Return DirectCast(MyFlashDevice, P_NOR).GetSectorSize(sector)
        ElseIf MyFlashType = MemoryType.SERIAL_NOR Then
            Return CInt(DirectCast(MyFlashDevice, SPI_NOR).ERASE_SIZE)
        End If
        Return 0
    End Function

    Public Function SectorFind(sector_index As Integer) As Long Implements MemoryDeviceUSB.SectorFind
        If MyFlashType = MemoryType.PARALLEL_NOR Then
            Dim base_addr As Long = 0
            If sector_index > 0 Then
                For i As Integer = 0 To sector_index - 1
                    base_addr += CLng(Me.SectorSize(i))
                Next
            End If
            Return base_addr
        End If
        Return 0
    End Function

    Public Function SectorCount() As Integer Implements MemoryDeviceUSB.SectorCount
        Return MyFlashDevice.Sector_Count
    End Function

    Public Function SectorWrite(sector_index As Integer, data() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.SectorWrite
        Dim Addr32 As Long = Me.SectorFind(sector_index)
        Return WriteData(Addr32, data, Params)
    End Function

    Public Function ReadData(flash_offset As Long, data_count As Integer) As Byte() Implements MemoryDeviceUSB.ReadData
        Dim sv1 As New ScriptVariable("arg_1", DataType.UInteger)
        Dim sv2 As New ScriptVariable("arg_2", DataType.Integer)
        sv1.Value = CUInt(flash_offset) 'Script language does not support 64-bit int
        sv2.Value = CInt(data_count)
        Dim vr As ScriptVariable = ScriptProcessor.Execute_Event(LinkFnc_Read, {sv1, sv2})
        If Not vr.Data.VarType = DataType.Data Then Return Nothing
        Dim data_buffer() As Byte = CType(vr.Value, Byte())
        If Not data_buffer.Length = data_count Then Return Nothing
        Return data_buffer
    End Function

    Public Function WriteData(flash_offset As Long, data_to_write() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        Dim sv1 As New ScriptVariable("arg_1", DataType.UInteger)
        Dim sv2 As New ScriptVariable("arg_2", DataType.Integer)
        Dim sv3 As New ScriptVariable("arg_3", DataType.Data)
        sv1.Value = CUInt(flash_offset) 'Script language does not support 64-bit int
        sv2.Value = CInt(data_to_write.Length)
        sv3.Value = data_to_write
        Dim vr As ScriptVariable = ScriptProcessor.Execute_Event(LinkFnc_Write, {sv1, sv2, sv3})
        If vr Is Nothing Then Return False
        Return CBool(vr.Value)
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        If LinkFnc_EraseAll IsNot Nothing Then
            Dim vr As ScriptVariable = ScriptProcessor.Execute_Event(LinkFnc_EraseAll, Nothing)
            Return CBool(vr.Value)
        Else
            Return False
        End If
    End Function

    Public Function SectorErase(SectorIndex As Integer) As Boolean Implements MemoryDeviceUSB.SectorErase
        Dim sv1 As New ScriptVariable("arg_1", DataType.Integer)
        sv1.Value = SectorIndex
        Dim vr As ScriptVariable = ScriptProcessor.Execute_Event(LinkFnc_EraseSector, {sv1})
        Return CBool(vr.Value)
    End Function

    Public Sub ResetDevice()
        If LinkFnc_Reset IsNot Nothing Then
            ScriptProcessor.Execute_Event(LinkFnc_Reset, Nothing)
        End If
    End Sub

End Class
