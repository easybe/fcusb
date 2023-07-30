Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB.HostClient

Public Class I2C_Programmer : Implements MemoryDeviceUSB
    Private FCUSB As FCUSB_DEVICE

    Public Event PrintConsole(message As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress

    Private eeprom_size As UInt32 = 0

    Sub New(ByVal parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
        Create_I2C_EEPROM_List()
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        eeprom_size = MySettings.I2C_SIZE
        Dim page_size As Byte
        Dim addr_size As Byte
        GetAddressPageSize(addr_size, page_size)
        Dim cd_value As UInt16 = (CUShort(MySettings.I2C_SPEED) << 8) Or (MySettings.I2C_ADDRESS) '02A0
        Dim cd_index As UInt16 = (CUShort(addr_size) << 8) Or (page_size) 'addr size, page size   '0220
        Dim config_data As UInt32 = (CUInt(cd_value) << 16) Or cd_index
        Dim detect_result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.I2C_INIT, Nothing, config_data)
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

    Public I2C_EEPROM_LIST As New List(Of I2C_DEVICE)

    Public ReadOnly Property DeviceName As String Implements MemoryDeviceUSB.DeviceName
        Get
            Return GetConfiguredDeviceName(Me.DeviceSize)
        End Get
    End Property

    Public ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
        Get
            Return eeprom_size
        End Get
    End Property

    Public ReadOnly Property SectorSize(sector As UInteger, Optional area As FlashArea = FlashArea.Main) As UInteger Implements MemoryDeviceUSB.SectorSize
        Get
            Return Me.DeviceSize
        End Get
    End Property

    Private Sub Create_I2C_EEPROM_List()
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XX01", 128, 1, 8))
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XX02", 256, 1, 8))
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XX04", 512, 1, 16))
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

    Public Function IsConnected() As Boolean
        Try
            Me.DeviceInit()
            Utilities.Sleep(20) 'Wait for IO VCC to charge up
            Dim test_data() As Byte = Me.ReadData(0, 16) 'This does a test read to see if data is read
            If test_data Is Nothing Then Return False
            Return True
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Function GetConfiguredDeviceName(ByVal device_size As Integer) As String
        Select Case device_size
            Case 128
                Return "24XX01"
            Case 256
                Return "24XX02"
            Case 512
                Return "24XX04"
            Case 1024
                Return "24XX08"
            Case 2048
                Return "24XX16"
            Case 4096
                Return "24XX32"
            Case 8192
                Return "24XX64"
            Case 16384
                Return "24XX128"
            Case 32768
                Return "24XX256"
            Case 65536
                Return "24XX512"
            Case 131072
                Return "M24M01"
            Case 262144
                Return "M24M02"
            Case Else
                Return ""
        End Select
    End Function

    Private Sub GetAddressPageSize(ByRef addr_size As Byte, ByRef page_size As Byte)
        Select Case MySettings.I2C_SIZE
            Case 128
                addr_size = 1
                page_size = 8
            Case 256
                addr_size = 1
                page_size = 8
            Case 512
                addr_size = 1
                page_size = 16
            Case 1024
                addr_size = 1
                page_size = 16
            Case 2048
                addr_size = 1
                page_size = 16
            Case 4096
                addr_size = 2
                page_size = 32
            Case 8192
                addr_size = 2
                page_size = 32
            Case 16384
                addr_size = 2
                page_size = 32
            Case 32768
                addr_size = 2
                page_size = 32
            Case 65536
                addr_size = 2
                page_size = 32
            Case 131072
                addr_size = 2
                page_size = 32
            Case 262144
                addr_size = 2
                page_size = 32
        End Select
    End Sub

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

End Class
