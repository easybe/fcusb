Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB.HostClient

Public Class I2C_Programmer : Implements MemoryDeviceUSB
    Private FCUSB As FCUSB_DEVICE

    Public Event PrintConsole(message As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress

    Public I2C_EEPROM_LIST As List(Of I2C_DEVICE)
    Private I2C_EEPROM_SELECTED As I2C_DEVICE = Nothing

    Sub New(parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
        I2C_EEPROM_LIST = New List(Of I2C_DEVICE)
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XX01", 128, 1, 8))
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XX02", 256, 1, 8))
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XX03", 256, 1, 16))
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XX04", 512, 1, 16))
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XX05", 512, 1, 32))
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XX08", 1024, 1, 16))
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XX16", 2048, 1, 16))
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XX32", 4096, 2, 32))
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XX64", 8192, 2, 32))
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XX128", 16384, 2, 32))
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XX256", 32768, 2, 32))
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XX256", 65536, 2, 32))
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XXM01", 131072, 2, 32))
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XXM02", 262144, 2, 32))
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        If MySettings.I2C_INDEX = 0 Then
            Return False 'No device selected
        Else
            I2C_EEPROM_SELECTED = I2C_EEPROM_LIST.Item(MySettings.I2C_INDEX - 1)
        End If
        Dim cd_value As UInt16 = (CUShort(MySettings.I2C_SPEED) << 8) Or (MySettings.I2C_ADDRESS) '02A0
        Dim cd_index As UInt16 = (CUShort(I2C_EEPROM_SELECTED.AddressSize) << 8) Or (I2C_EEPROM_SELECTED.PageSize) 'addr size, page size   '0220
        Dim config_data As UInt32 = (CUInt(cd_value) << 16) Or cd_index
        Dim detect_result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.I2C_INIT, Nothing, config_data)
        Utilities.Sleep(50) 'Wait for IO VCC to charge up
        Return detect_result
    End Function

    Public Class I2C_DEVICE
        Public ReadOnly Property Name As String
        Public ReadOnly Property Size As UInt32 'Number of bytes in this Flash device
        Public ReadOnly Property AddressSize As Integer
        Public ReadOnly Property PageSize As Integer

        Sub New(ByVal DisplayName As String, ByVal SizeInBytes As UInt32, ByVal EEAddrSize As Integer, ByVal EEPageSize As Integer)
            Me.Name = DisplayName
            Me.Size = SizeInBytes
            Me.AddressSize = EEAddrSize 'Number of bytes that are used to store the address
            Me.PageSize = EEPageSize
        End Sub

    End Class

    Public ReadOnly Property DeviceName As String Implements MemoryDeviceUSB.DeviceName
        Get
            If I2C_EEPROM_SELECTED Is Nothing Then Return ""
            Return I2C_EEPROM_SELECTED.Name
        End Get
    End Property

    Public ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
        Get
            If I2C_EEPROM_SELECTED Is Nothing Then Return 0
            Return I2C_EEPROM_SELECTED.Size
        End Get
    End Property

    Public ReadOnly Property SectorSize(sector As UInteger, Optional area As FlashArea = FlashArea.Main) As UInteger Implements MemoryDeviceUSB.SectorSize
        Get
            Return Me.DeviceSize
        End Get
    End Property

    Public Function IsConnected() As Boolean
        Try
            Dim test_data() As Byte = Me.ReadData(0, 16) 'This does a test read to see if data is read
            If test_data Is Nothing Then Return False
            Return True
        Catch ex As Exception
        End Try
        Return False
    End Function

    Private Function GetResultStatus() As I2C_STATUS
        Try
            Dim packet_out(0) As Byte
            If Not FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.I2C_RESULT, packet_out) Then Return I2C_STATUS.USBFAIL
            Return packet_out(0)
        Catch ex As Exception
            Return I2C_STATUS.ERROR
        End Try
    End Function

    Private Enum I2C_STATUS As Byte
        USBFAIL = 0
        NOERROR = &H50
        [ERROR] = &H51
    End Enum

    Public Function ReadData(flash_offset As Long, data_count As UInteger, Optional area As FlashArea = FlashArea.Main) As Byte() Implements MemoryDeviceUSB.ReadData
        Try
            Dim setup_data(6) As Byte
            Dim result As Boolean = False
            setup_data(0) = ((flash_offset >> 24) And 255)
            setup_data(1) = ((flash_offset >> 16) And 255)
            setup_data(2) = ((flash_offset >> 8) And 255)
            setup_data(3) = (flash_offset And 255)
            setup_data(4) = ((data_count >> 16) And 255)
            setup_data(5) = ((data_count >> 8) And 255)
            setup_data(6) = (data_count And 255)
            Dim data_out(data_count - 1) As Byte
            result = FCUSB.USB_SETUP_BULKIN(USB.USBREQ.I2C_READEEPROM, setup_data, data_out, data_count)
            If Not result Then Return Nothing
            If GetResultStatus() = I2C_STATUS.NOERROR Then Return data_out
        Catch ex As Exception
        End Try
        Return Nothing
    End Function

    Public Function WriteData(flash_offset As Long, data_to_write() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        Try
            Dim setup_data(6) As Byte
            Dim data_count As UInt32 = data_to_write.Length
            Dim result As Boolean = False
            setup_data(0) = ((flash_offset >> 24) And 255)
            setup_data(1) = ((flash_offset >> 16) And 255)
            setup_data(2) = ((flash_offset >> 8) And 255)
            setup_data(3) = (flash_offset And 255)
            setup_data(4) = ((data_count >> 16) And 255)
            setup_data(5) = ((data_count >> 8) And 255)
            setup_data(6) = (data_count And 255)
            result = FCUSB.USB_SETUP_BULKOUT(USB.USBREQ.I2C_WRITEEEPROM, setup_data, data_to_write, data_count)
            If Not result Then Return False
            FCUSB.USB_WaitForComplete() 'It may take a few microseconds to complete
            If GetResultStatus() = I2C_STATUS.NOERROR Then Return True
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        Return True 'EEPROM does not support erase commands
    End Function

    Public Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
        Utilities.Sleep(100)
    End Sub

    Public Function SectorFind(SectorIndex As UInteger, Optional area As FlashArea = FlashArea.Main) As Long Implements MemoryDeviceUSB.SectorFind
        Return 0
    End Function

    Public Function SectorErase(SectorIndex As UInteger, Optional area As FlashArea = FlashArea.Main) As Boolean Implements MemoryDeviceUSB.SectorErase
        Return True
    End Function

    Public Function SectorCount() As UInteger Implements MemoryDeviceUSB.SectorCount
        Return 1
    End Function

    Public Function SectorWrite(SectorIndex As UInteger, data() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.SectorWrite
        Return False 'Not supported
    End Function

End Class
