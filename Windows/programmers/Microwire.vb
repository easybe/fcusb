Imports FlashcatUSB.FlashMemory
Public Class Microwire_Programmer : Implements MemoryDeviceUSB
    Private FCUSB As USB.FCUSB_DEVICE

    Private MICROWIRE_DEVIE As MICROWIRE

    Public Event PrintConsole(message As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress

    Sub New(parent_if As USB.FCUSB_DEVICE)
        FCUSB = parent_if
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        Dim s93_devices() As Device = FlashDatabase.GetFlashDevices(MemoryType.SERIAL_MICROWIRE)
        Me.MICROWIRE_DEVIE = Nothing
        Dim org_mode As UInt32 = MySettings.S93_DEVICE_ORG '0=8-bit,1=16-bit
        If Not MySettings.S93_DEVICE.Equals("") Then
            For Each mem_device In s93_devices
                If mem_device.NAME.ToUpper.Equals(MySettings.S93_DEVICE.ToUpper) Then
                    Me.MICROWIRE_DEVIE = mem_device
                    Exit For
                End If
            Next
        End If
        If Me.MICROWIRE_DEVIE Is Nothing Then
            RaiseEvent PrintConsole("No Microwire device selected")
            Return False
        End If
        Dim addr_bits As UInt32 = 0
        Dim org_str As String
        If org_mode = 0 Then '8-bit
            org_str = "X8"
            addr_bits = Me.MICROWIRE_DEVIE.X8_ADDRSIZE
        Else '16-bit mode
            org_str = "X16"
            addr_bits = Me.MICROWIRE_DEVIE.X16_ADDRSIZE
        End If
        RaiseEvent PrintConsole("Microwire device: " & Me.DeviceName & " (" & Me.MICROWIRE_DEVIE.FLASH_SIZE & " bytes) " & org_str & " mode")
        Dim setup_data As UInt32 = (addr_bits << 8) Or (org_mode)
        Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.S93_INIT, Nothing, setup_data)
        Return result
    End Function

    Public ReadOnly Property GetDevice As Device Implements MemoryDeviceUSB.GetDevice
        Get
            Return Me.MICROWIRE_DEVIE
        End Get
    End Property

    Public ReadOnly Property DeviceName As String Implements MemoryDeviceUSB.DeviceName
        Get
            Return Me.MICROWIRE_DEVIE.NAME
        End Get
    End Property

    Public ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
        Get
            Return Me.MICROWIRE_DEVIE.FLASH_SIZE
        End Get
    End Property

    Public Function SectorSize(sector As UInteger) As UInt32 Implements MemoryDeviceUSB.SectorSize
        Return Me.DeviceSize
    End Function

    Public Function ReadData(flash_offset As Long, data_count As Long) As Byte() Implements MemoryDeviceUSB.ReadData
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

    Public Function WriteData(flash_offset As Long, data_to_write() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
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
            Utilities.Sleep(100)
            FCUSB.USB_WaitForComplete()
            If result Then ReadData(0, 16) 'Some devices need us to read a page of data
            Return result
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.S93_ERASE)
        Return result
    End Function

    Public Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
        Utilities.Sleep(10)
    End Sub

    Public Function SectorFind(SectorIndex As UInt32) As Long Implements MemoryDeviceUSB.SectorFind
        Return 0
    End Function

    Public Function SectorErase(SectorIndex As UInteger) As Boolean Implements MemoryDeviceUSB.SectorErase
        Return True
    End Function

    Public Function SectorCount() As UInteger Implements MemoryDeviceUSB.SectorCount
        Return 1
    End Function

    Public Function SectorWrite(SectorIndex As UInteger, data() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.SectorWrite
        Return WriteData(0, data)
    End Function


End Class
