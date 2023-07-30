Imports FlashcatUSB.FlashMemory

Public Class SWI_Programmer : Implements MemoryDeviceUSB
    Private FCUSB As USB.FCUSB_DEVICE

    Public Event PrintConsole(message As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress

    Public MyFlashDevice As SWI

    Sub New(parent_if As USB.FCUSB_DEVICE)
        FCUSB = parent_if
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        Dim chip_id(2) As Byte
        Dim detect_result As Boolean = FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.SWI_DETECT, chip_id, MySettings.SWI_ADDRESS)
        Dim SWI_ID_DATA As UInt32 = (CUInt(chip_id(0)) << 16) Or (CUInt(chip_id(1)) << 8) Or CUInt(chip_id(2))
        Dim MFG_CODE As Byte = CByte(CUInt(SWI_ID_DATA >> 12) And &HF)
        Dim PART As UInt16 = CUShort(SWI_ID_DATA >> 3) And &H1FFUS
        If MFG_CODE = &HD Then Return False
        If PART = &H40 Then
            MyFlashDevice = New SWI("Microchip AT21CS01", &HD, &H40, 128, 8)
        ElseIf PART = &H70 Then
            MyFlashDevice = New SWI("Microchip AT21CS11", &HD, &H70, 128, 8)
        Else
            Return False
        End If
        Return True
    End Function

    Public ReadOnly Property GetDevice As Device Implements MemoryDeviceUSB.GetDevice
        Get
            Return Me.MyFlashDevice
        End Get
    End Property

    Public ReadOnly Property DeviceName As String Implements MemoryDeviceUSB.DeviceName
        Get
            Return Me.MyFlashDevice.NAME
        End Get
    End Property

    Public ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
        Get
            Return Me.MyFlashDevice.FLASH_SIZE
        End Get
    End Property

    Public Function SectorSize(sector As Integer) As Integer Implements MemoryDeviceUSB.SectorSize
        Return CInt(Me.DeviceSize)
    End Function

    Public Function ReadData(flash_offset As Long, data_count As Integer) As Byte() Implements MemoryDeviceUSB.ReadData
        Dim setup_data() As Byte = GetSetupPacket(CUInt(flash_offset), CUInt(data_count), CUShort(Me.MyFlashDevice.PAGE_SIZE))
        Dim data_out(CInt(data_count) - 1) As Byte
        Dim result As Boolean = FCUSB.USB_SETUP_BULKIN(USB.USBREQ.SWI_READ, setup_data, data_out, 0)
        If Not result Then Return Nothing
        Return data_out
    End Function

    Public Function WriteData(flash_offset As Long, data_to_write() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        Dim setup_data() As Byte = GetSetupPacket(CUInt(flash_offset), CUInt(data_to_write.Length), CUShort(Me.MyFlashDevice.PAGE_SIZE))
        Dim result As Boolean = FCUSB.USB_SETUP_BULKOUT(USB.USBREQ.SWI_WRITE, setup_data, data_to_write, 0)
        Return result
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        Return True 'EEPROM does not support erase commands
    End Function

    Public Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
        Utilities.Sleep(10)
    End Sub

    Public Function SectorFind(SectorIndex As Integer) As Long Implements MemoryDeviceUSB.SectorFind
        Return 0
    End Function

    Public Function SectorErase(SectorIndex As Integer) As Boolean Implements MemoryDeviceUSB.SectorErase
        Return True
    End Function

    Public Function SectorCount() As Integer Implements MemoryDeviceUSB.SectorCount
        Return 1
    End Function

    Public Function SectorWrite(SectorIndex As Integer, data() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.SectorWrite
        Return False 'Not supported
    End Function

    Private Function GetSetupPacket(Address As UInt32, Count As UInt32, PageSize As UInt16) As Byte()
        Dim addr_bytes As Byte = 0
        Dim data_in(10) As Byte
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
        data_in(10) = 1
        Return data_in
    End Function

End Class
