Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB.HostClient

Public Class SWI_Programmer : Implements MemoryDeviceUSB
    Private FCUSB As FCUSB_DEVICE

    Public Event PrintConsole(message As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress

    Private SWI_DEV_NAME As String
    Private SWI_DEV_SIZE As Integer 'Total bytes of the device 
    Private SWI_DEV_PAGE As UInt16


    Sub New(ByVal parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        Dim chip_id(2) As Byte
        Dim detect_result As Boolean = FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.SWI_DETECT, chip_id, MySettings.SWI_ADDRESS)
        Dim SWI_ID_DATA As UInt32 = (CInt(chip_id(0)) << 16) Or (CInt(chip_id(1)) << 8) Or CInt(chip_id(2))
        Select Case SWI_ID_DATA
            Case &HD200UI
                SWI_DEV_NAME = "Microchip AT21CS01"
                SWI_DEV_SIZE = 128
                SWI_DEV_PAGE = 8
            Case &HD380UI
                SWI_DEV_NAME = "Microchip AT21CS11"
                SWI_DEV_SIZE = 128
                SWI_DEV_PAGE = 8
            Case Else
                Return False
        End Select
        Return True
    End Function

    Public ReadOnly Property DeviceName As String Implements MemoryDeviceUSB.DeviceName
        Get
            Return SWI_DEV_NAME
        End Get
    End Property

    Public ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
        Get
            Return SWI_DEV_SIZE
        End Get
    End Property

    Public ReadOnly Property SectorSize(sector As UInteger, Optional area As FlashArea = FlashArea.Main) As UInteger Implements MemoryDeviceUSB.SectorSize
        Get
            Return Me.DeviceSize
        End Get
    End Property

    Public Function ReadData(flash_offset As Long, data_count As UInteger, Optional area As FlashArea = FlashArea.Main) As Byte() Implements MemoryDeviceUSB.ReadData
        Dim setup_data() As Byte = GetSetupPacket(flash_offset, data_count, SWI_DEV_PAGE)
        Dim data_out(data_count - 1) As Byte
        Dim result As Boolean = FCUSB.USB_SETUP_BULKIN(USB.USBREQ.SWI_READ, setup_data, data_out, 0)
        If Not result Then Return Nothing
        Return data_out
    End Function

    Public Function WriteData(flash_offset As Long, data_to_write() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        Dim setup_data() As Byte = GetSetupPacket(flash_offset, data_to_write.Length, SWI_DEV_PAGE)
        Dim result As Boolean = FCUSB.USB_SETUP_BULKOUT(USB.USBREQ.SWI_WRITE, setup_data, data_to_write, 0)
        Return result
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        Return True 'EEPROM does not support erase commands
    End Function

    Public Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
        Utilities.Sleep(10)
    End Sub

    Public Function SectorFind(SectorIndex As UInteger, Optional area As FlashArea = FlashArea.Main) As Long Implements MemoryDeviceUSB.SectorFind
        Return 0
    End Function

    Public Function Sector_Erase(SectorIndex As UInteger, Optional area As FlashArea = FlashArea.Main) As Boolean Implements MemoryDeviceUSB.Sector_Erase
        Return True
    End Function

    Public Function Sector_Count() As UInteger Implements MemoryDeviceUSB.Sector_Count
        Return 1
    End Function

    Public Function Sector_Write(SectorIndex As UInteger, data() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.Sector_Write
        Return False 'Not supported
    End Function

    Private Function GetSetupPacket(ByVal Address As UInt32, ByVal Count As UInt32, ByVal PageSize As UInt16) As Byte()
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
