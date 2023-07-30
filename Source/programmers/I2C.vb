Imports FlashcatUSB.FlashMemory

Public Class I2C_Programmer : Implements MemoryDeviceUSB
    Private FCUSB As USB.FCUSB_DEVICE

    Public Event PrintConsole(message As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress

    Public I2C_EEPROM_LIST As List(Of I2C_DEVICE)
    Private MyFlashDevice As I2C_DEVICE = Nothing

    Public Property SPEED As I2C_SPEED_MODE = I2C_SPEED_MODE._400kHz
    Public Property ADDRESS As Byte = &HA0

    Sub New(parent_if As USB.FCUSB_DEVICE)
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
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XX512", 65536, 2, 32))
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XXM01", 131072, 2, 32))
        I2C_EEPROM_LIST.Add(New I2C_DEVICE("24XXM02", 262144, 2, 32))
    End Sub

    Public Sub SelectDeviceIndex(ic2_eeprom_index As Integer)
        Me.MyFlashDevice = I2C_EEPROM_LIST.Item(ic2_eeprom_index)
    End Sub

    Public Sub SelectDeviceIndex(ic2_eeprom_name As String)
        Me.MyFlashDevice = Nothing
        For Each i2c_device In I2C_EEPROM_LIST
            If i2c_device.NAME.ToUpper.Equals(ic2_eeprom_name.ToUpper) Then
                Me.MyFlashDevice = i2c_device
                Exit Sub
            End If
        Next
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        If MyFlashDevice Is Nothing Then Return False
        Dim cd_value As UInt16 = (CUShort(Me.SPEED) << 8) Or (Me.ADDRESS) '02A0
        Dim cd_index As UInt16 = (CUShort(MyFlashDevice.AddressSize) << 8) Or (MyFlashDevice.PAGE_SIZE) 'addr size, page size   '0220
        Dim config_data As UInt32 = (CUInt(cd_value) << 16) Or cd_index
        Dim detect_result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.I2C_INIT, Nothing, config_data)
        Utilities.Sleep(50) 'Wait for IO VCC to charge up
        Return detect_result
    End Function

    Public ReadOnly Property GetDevice As Device Implements MemoryDeviceUSB.GetDevice
        Get
            Return Me.MyFlashDevice
        End Get
    End Property

    Public ReadOnly Property DeviceName As String Implements MemoryDeviceUSB.DeviceName
        Get
            If MyFlashDevice Is Nothing Then Return ""
            Return MyFlashDevice.Name
        End Get
    End Property

    Public ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
        Get
            If MyFlashDevice Is Nothing Then Return 0
            Return MyFlashDevice.FLASH_SIZE
        End Get
    End Property

    Public Function SectorSize(sector As Integer) As Integer Implements MemoryDeviceUSB.SectorSize
        Return CInt(Me.DeviceSize)
    End Function

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
            Return CType(packet_out(0), I2C_STATUS)
        Catch ex As Exception
            Return I2C_STATUS.ERROR
        End Try
    End Function

    Private Enum I2C_STATUS As Byte
        USBFAIL = 0
        NOERROR = &H50
        [ERROR] = &H51
    End Enum

    Public Function ReadData(flash_offset As Long, data_count As Integer) As Byte() Implements MemoryDeviceUSB.ReadData
        Try
            Dim setup_data(6) As Byte
            Dim result As Boolean = False
            setup_data(0) = CByte((flash_offset >> 24) And 255)
            setup_data(1) = CByte((flash_offset >> 16) And 255)
            setup_data(2) = CByte((flash_offset >> 8) And 255)
            setup_data(3) = CByte(flash_offset And 255)
            setup_data(4) = CByte((data_count >> 16) And 255)
            setup_data(5) = CByte((data_count >> 8) And 255)
            setup_data(6) = CByte(data_count And 255)
            Dim data_out(data_count - 1) As Byte
            result = FCUSB.USB_SETUP_BULKIN(USB.USBREQ.I2C_READEEPROM, setup_data, data_out, CUInt(data_count))
            If Not result Then Return Nothing
            If GetResultStatus() = I2C_STATUS.NOERROR Then Return data_out
        Catch ex As Exception
        End Try
        Return Nothing
    End Function

    Public Function WriteData(flash_offset As Long, data_to_write() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        Try
            Dim setup_data(6) As Byte
            Dim data_count As Integer = data_to_write.Length
            Dim result As Boolean = False
            setup_data(0) = CByte((flash_offset >> 24) And 255)
            setup_data(1) = CByte((flash_offset >> 16) And 255)
            setup_data(2) = CByte((flash_offset >> 8) And 255)
            setup_data(3) = CByte(flash_offset And 255)
            setup_data(4) = CByte((data_count >> 16) And 255)
            setup_data(5) = CByte((data_count >> 8) And 255)
            setup_data(6) = CByte(data_count And 255)
            result = FCUSB.USB_SETUP_BULKOUT(USB.USBREQ.I2C_WRITEEEPROM, setup_data, data_to_write, CUInt(data_count))
            If Not result Then Return False
            FCUSB.USB_WaitForComplete() 'It may take a few microseconds to complete
            If GetResultStatus() = I2C_STATUS.NOERROR Then Return True
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        Dim black_data(CInt(Me.DeviceSize - 1)) As Byte
        Utilities.FillByteArray(black_data, 255)
        Return WriteData(0, black_data)
    End Function

    Public Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
        Utilities.Sleep(100)
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

End Class
