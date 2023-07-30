'Imports LibUsbDotNet
'Imports LibUsbDotNet.Main


'Namespace I2C

'    Public Class EEPROMProgrammer
'        Event PrintConsole(ByVal msg As String)
'        Private LibusbDeviceFinder As UsbDeviceFinder = New UsbDeviceFinder(&H16C0, &H5DE)
'        Private FCUSB As UsbDevice

'        Private v_dataswap As Boolean = False 'Set to true to flip the byte endian

'        Sub New()

'        End Sub

'        Friend Property ReverseEndian As Boolean
'            Get
'                Return v_dataswap
'            End Get
'            Set(ByVal value As Boolean)
'                v_dataswap = value
'            End Set
'        End Property

'#Region "USB Hardware Call Flags"
'        Private Const SPIREQ_ECHO As Byte = &H80
'        Private Const SPIREQ_LEDON As Byte = &H81
'        Private Const SPIREQ_LEDOFF As Byte = &H82
'        Private Const SPIREQ_LEDBLINK As Byte = &H83
'        Private Const SPIREQ_VERSION As Byte = &H86

'        Private Const I2CREQ_INIT As Byte = &H90
'        Private Const I2CREQ_START As Byte = &H91
'        Private Const I2CREQ_STOP As Byte = &H92
'        Private Const I2CREQ_WRITEBYTE As Byte = &H93
'        Private Const I2CREQ_READBYTE As Byte = &H94
'        Private Const I2CREQ_EEWriteByte As Byte = &H95
'        Private Const I2CREQ_EEReadBytes As Byte = &H96
'        Private Const I2CREQ_EESetAddress As Byte = &H97
'        Private Const I2CREQ_EEStatus As Byte = &H98

'#End Region

'#Region "USB Hardware Calls"

'        Friend Sub LEDOn() Implements MemoryDeviceUSB.LEDOn
'            Try
'                If FCUSB Is Nothing Then Exit Sub
'                Dim ret As Integer = 0
'                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
'                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_LEDON, 0, 0, 0)
'                FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
'            Catch ex As Exception
'            End Try
'        End Sub

'        Friend Sub LEDOff() Implements MemoryDeviceUSB.LedOff
'            Try
'                If FCUSB Is Nothing Then Exit Sub
'                Dim ret As Integer = 0
'                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
'                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_LEDOFF, 0, 0, 0)
'                FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
'            Catch ex As Exception
'            End Try
'        End Sub

'        Friend Sub LEDBlink() Implements MemoryDeviceUSB.LEDBlink
'            Try
'                If FCUSB Is Nothing Then Exit Sub
'                Dim ret As Integer = 0
'                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
'                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_LEDBLINK, 0, 0, 0)
'                FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
'            Catch ex As Exception
'            End Try
'        End Sub

'        Private Function EchoTest() As Boolean
'            Try
'                Dim buff As Byte() = New Byte(7) {}
'                Dim usbSetupPacket As New UsbSetupPacket(CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor), CByte(SPIREQ_ECHO), &H1234, &H5678, 8)
'                Dim ret As Integer
'                If FCUSB.ControlTransfer(usbSetupPacket, buff, 8, ret) Then
'                    If buff(0) <> CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor) Then
'                        Return False
'                    End If
'                    If buff(1) <> CByte(SPIREQ_ECHO) Then Return False
'                    If buff(2) <> &H34 Then Return False
'                    If buff(3) <> &H12 Then Return False
'                    If buff(4) <> &H78 Then Return False
'                    If buff(5) <> &H56 Then Return False
'                    If buff(6) <> &H8 Then Return False
'                    If buff(7) <> &H0 Then Return False
'                    Return True
'                End If
'                MyUSBDeviceID = ""
'                FCUSB = Nothing
'                Return False
'            Catch ex As Exception
'                MyUSBDeviceID = ""
'                FCUSB = Nothing
'                Return False
'            End Try
'        End Function
'        'Returns the version of our firmware
'        Public Function GetAvrVersion() As String
'            Try
'                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor)
'                Dim usbSetupPacket As New UsbSetupPacket(usbflag, SPIREQ_VERSION, 0, 0, 4)
'                Dim ret As Integer
'                Dim buff(4) As Byte
'                If FCUSB.ControlTransfer(usbSetupPacket, buff, 4, ret) Then
'                    buff(4) = buff(3)
'                    buff(3) = buff(2)
'                    buff(2) = Asc(".")
'                End If
'                Dim fwstr As String = Utilities.Bytes.ToChrString(buff)
'                Return Utilities.StringToSingle(fwstr).ToString
'            Catch ex As Exception
'                Return ""
'            End Try
'        End Function

'        Friend Function IsConnected() As Boolean Implements MemoryDeviceUSB.IsConnected
'            If FCUSB Is Nothing Then
'                Dim fcusb_list As UsbRegDeviceList = UsbDevice.AllDevices.FindAll(LibusbDeviceFinder)
'                If fcusb_list IsNot Nothing AndAlso fcusb_list.Count > 0 Then
'                    Dim InUse() As String = GetFlashcatInUse()
'                    For i = 0 To fcusb_list.Count - 1
'                        Dim u As WinUsb.WinUsbRegistry = fcusb_list(i)
'                        Dim o As String = u.DeviceProperties("DeviceID")
'                        If MyUSBDeviceID = o Then
'                            Return True
'                        ElseIf MyUSBDeviceID = "" Then
'                            'check other fcusb apps
'                            If InUse Is Nothing Then 'No other instances, we can use this one
'                                Return True
'                            Else
'                                Dim FoundAvailableFcusb As Boolean = True
'                                For Each item In InUse
'                                    If item = o Then FoundAvailableFcusb = False : Exit For
'                                Next
'                                If FoundAvailableFcusb Then Return True 'This ID is not being used
'                            End If
'                        End If
'                    Next
'                End If
'                Return False
'            Else
'                Return FCUSB.UsbRegistryInfo.IsAlive
'            End If
'        End Function
'        'Connects this interface to an available fcusb
'        Friend Function Connect() As Boolean Implements MemoryDeviceUSB.Connect
'            Dim fcusb_list As UsbRegDeviceList = UsbDevice.AllDevices.FindAll(LibusbDeviceFinder)
'            If fcusb_list IsNot Nothing AndAlso fcusb_list.Count > 0 Then
'                Dim InUse() As String = GetFlashcatInUse()
'                For i = 0 To fcusb_list.Count - 1
'                    Dim u As WinUsb.WinUsbRegistry = fcusb_list(i)
'                    Dim o As String = u.DeviceProperties("DeviceID")
'                    If MyUSBDeviceID = o Then
'                        MyUSBDeviceID = o
'                        FCUSB = u.Device
'                        Return OpenUsbDevice()
'                    ElseIf MyUSBDeviceID = "" Then
'                        If InUse Is Nothing Then 'No other instances, we can use this one
'                            MyUSBDeviceID = o
'                            FCUSB = u.Device
'                            Return OpenUsbDevice()
'                        Else
'                            Dim FoundAvailableFcusb As Boolean = True
'                            For Each item In InUse
'                                If item = o Then FoundAvailableFcusb = False : Exit For
'                            Next
'                            If FoundAvailableFcusb Then
'                                MyUSBDeviceID = o
'                                FCUSB = u.Device
'                                Return OpenUsbDevice()
'                            End If
'                        End If
'                    End If
'                Next
'            End If
'            Return False 'No FCUSB found
'        End Function

'        Private Function OpenUsbDevice() As Boolean
'            If FCUSB Is Nothing Then Return False
'            FCUSB.Open()
'            Dim wholeUsbDevice As IUsbDevice = TryCast(FCUSB, IUsbDevice)
'            If wholeUsbDevice IsNot Nothing Then 'Libusb only
'                wholeUsbDevice.SetConfiguration(1)
'                wholeUsbDevice.ClaimInterface(0)
'            End If
'            'SetDeviceConfig(SPI_CLOCK.SPI_CLOCK_FOSC_2, SPI_MODE.SPI_MODE_0)
'            Return EchoTest()
'        End Function
'        'If we are already connected, this will disconnect
'        Friend Function Disconnect() As Boolean Implements MemoryDeviceUSB.Disconnect
'            Try
'                If FCUSB IsNot Nothing Then FCUSB.Close()
'                MyUSBDeviceID = ""
'                FCUSB = Nothing
'            Catch ex As Exception
'            End Try
'            Return True
'        End Function

'#End Region



'        Friend Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
'            'IC2_Init()

'            'Dim b(31) As Byte
'            'For i = 1 To 32
'            '    b(i - 1) = i.ToString
'            'Next
'            ''I2C_WriteBytes(0, b)


'            'Dim data_out() As Byte = Utilities.Bytes.FromString("hello ladies")
'            'I2C_WriteBytes(0, data_out)


'            'Dim b_back() As Byte = Nothing
'            'I2C_ReadBytes(0, b_back, 32)
'            'Beep()

'            '24XX16, 24XX32, 24XX64


'            Return True
'        End Function





'        Friend ReadOnly Property DeviceName() As String Implements MemoryDeviceUSB.DeviceName
'            Get
'                Return "I2C EEPROM"
'            End Get
'        End Property

'        Friend ReadOnly Property DeviceSize As UInt32 Implements MemoryDeviceUSB.DeviceSize
'            Get
'                Return 8192
'            End Get
'        End Property

'        Friend ReadOnly Property SectorSize As UInt32 Implements MemoryDeviceUSB.SectorSize
'            Get
'                Return 32
'            End Get
'        End Property

'        'Erases the entire chip (sets all pages/sectors to 0xFF)
'        Function ChipErase() As Boolean Implements MemoryDeviceUSB.ChipErase

'        End Function

'        Friend Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
'            Utilities.Sleep(100)
'        End Sub

'        Public Function FindSectorBase(ByVal SectorIndex As UInt32) As UInt32 Implements MemoryDeviceUSB.Sector_Find

'        End Function

'        Public Function Sector_Erase(ByVal Sector As UInt32) As Boolean Implements MemoryDeviceUSB.Sector_Erase

'        End Function
'        'Returns the total number of sectors (actually number of flash pages)
'        Public Function Sectors_Count() As UInt32 Implements MemoryDeviceUSB.Sectors_Count

'        End Function
'        'Writes data to a given sector and also swaps bytes (endian for words/halfwords)
'        Public Function WriteSector(ByVal Sector As Integer, ByVal data() As Byte) As Boolean Implements MemoryDeviceUSB.Sector_Write

'        End Function









'    End Class


'End Namespace
