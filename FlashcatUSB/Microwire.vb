
Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB.HostClient

Public Class Microwire_Programmer : Implements MemoryDeviceUSB
    Private FCUSB As FCUSB_DEVICE

    Public Event PrintConsole(message As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress

    Private EEPROM_SIZE As UInt32 = 0

    Sub New(ByVal parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        FCUSB.USB_VCC_3V() 'Ignored by PCB 2.x
        Dim org_mode As UInt32 = MySettings.S93_DEVICE_ORG
        Dim org_str As String
        If org_mode = 0 Then org_str = "X8" Else org_str = "X16"
        Dim addr_bits As UInt32
        Select Case MySettings.S93_DEVICE_INDEX
            Case 0
                EEPROM_SIZE = 0
                RaiseEvent PrintConsole("Error: no Microwire device selected")
                Return False
            Case 1 '93xx46  128 bytes (1Kbit)
                addr_bits = (7 - org_mode)
                EEPROM_SIZE = 128
            Case 2 '93xx56  256 bytes (2Kbit)
                addr_bits = (9 - org_mode) 'Yes, uses the same address size as 93xx66
                EEPROM_SIZE = 256
            Case 3 '93xx66  512 bytes (4Kbit)
                addr_bits = (9 - org_mode)
                EEPROM_SIZE = 512
            Case 4 '93xx76  1024 bytes (8Kbit)
                addr_bits = (10 - org_mode)
                EEPROM_SIZE = 1024
            Case 5 '93xx86  2048 bytes (16Kbit)
                addr_bits = (11 - org_mode)
                EEPROM_SIZE = 2048
        End Select
        RaiseEvent PrintConsole("Microwire device: " & Me.DeviceName & " (" & EEPROM_SIZE & " bytes) " & org_str & " mode")
        Dim setup_data As UInt32 = (addr_bits << 8) Or (org_mode)
        Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.S93_INIT, Nothing, setup_data)
        Return result
    End Function

    Public ReadOnly Property DeviceName As String Implements MemoryDeviceUSB.DeviceName
        Get
            Select Case MySettings.S93_DEVICE_INDEX
                Case 0
                    Return "Non-selected"
                Case 1 '93xx46  128 bytes (1Kbit)
                    Return "93xx46"
                Case 2 '93xx56  256 bytes (2Kbit)
                    Return "93xx56"
                Case 3 '93xx66  512 bytes (4Kbit)
                    Return "93xx66"
                Case 4 '93xx76  1024 bytes (8Kbit)
                    Return "93xx76"
                Case 5 '93xx86  2048 bytes (16Kbit)
                    Return "93xx86"
                Case Else
                    Return ""
            End Select
        End Get
    End Property

    Public ReadOnly Property DeviceSize As UInteger Implements MemoryDeviceUSB.DeviceSize
        Get
            Return EEPROM_SIZE
        End Get
    End Property

    Public ReadOnly Property SectorSize(sector As UInteger, Optional area As FlashArea = FlashArea.Main) As UInteger Implements MemoryDeviceUSB.SectorSize
        Get
            Return Me.DeviceSize
        End Get
    End Property

    Public Function ReadData(flash_offset As UInteger, data_count As UInteger, Optional area As FlashArea = FlashArea.Main) As Byte() Implements MemoryDeviceUSB.ReadData
        Try
            Dim setup_data(7) As Byte
            Dim result As Boolean
            setup_data(0) = ((data_count >> 24) And 255)
            setup_data(1) = ((data_count >> 16) And 255)
            setup_data(2) = ((data_count >> 8) And 255)
            setup_data(3) = (data_count And 255)
            setup_data(4) = ((flash_offset >> 24) And 255)
            setup_data(5) = ((flash_offset >> 16) And 255)
            setup_data(6) = ((flash_offset >> 8) And 255)
            setup_data(7) = (flash_offset And 255)
            Dim data_out(data_count - 1) As Byte
            result = FCUSB.USB_SETUP_BULKIN(USB.USBREQ.S93_READEEPROM, setup_data, data_out, 0)
            If Not result Then Return Nothing
            Return data_out
        Catch ex As Exception
        End Try
        Return Nothing
    End Function

    Public Function WriteData(flash_offset As UInteger, data_to_write() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        Try
            Dim data_count As UInt32 = data_to_write.Length
            Dim setup_data(7) As Byte
            Dim result As Boolean
            setup_data(0) = ((data_count >> 24) And 255)
            setup_data(1) = ((data_count >> 16) And 255)
            setup_data(2) = ((data_count >> 8) And 255)
            setup_data(3) = (data_count And 255)
            setup_data(4) = ((flash_offset >> 24) And 255)
            setup_data(5) = ((flash_offset >> 16) And 255)
            setup_data(6) = ((flash_offset >> 8) And 255)
            setup_data(7) = (flash_offset And 255)
            result = FCUSB.USB_SETUP_BULKOUT(USB.USBREQ.S93_WRITEEEPROM, setup_data, data_to_write, data_count)
            FCUSB.USB_WaitForComplete()
            Return result
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.S93_ERASE)
        Return True
    End Function

    Public Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
        Utilities.Sleep(10)
    End Sub

    Public Function SectorFind(SectorIndex As UInteger, Optional area As FlashArea = FlashArea.Main) As UInteger Implements MemoryDeviceUSB.SectorFind
        Return 0
    End Function

    Public Function Sector_Erase(SectorIndex As UInteger, Optional area As FlashArea = FlashArea.Main) As Boolean Implements MemoryDeviceUSB.Sector_Erase
        Return True
    End Function

    Public Function Sector_Count() As UInteger Implements MemoryDeviceUSB.Sector_Count
        Return 1
    End Function

    Public Function Sector_Write(SectorIndex As UInteger, data() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.Sector_Write
        Return WriteData(0, data)
    End Function


End Class
