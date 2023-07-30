'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2016 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'ACKNOWLEDGEMENT: USB driver functionality provided by LibUsbDotNet (sourceforge.net/projects/libusbdotnet) 

Imports FlashcatUSB.FlashMemory
Imports LibUsbDotNet
Imports LibUsbDotNet.Main

Namespace SPI

    Public Class FlashProgrammer : Implements MemoryDeviceUSB
        Private FCUSB As UsbDevice
        Public Event PrintConsole(ByVal msg As String) Implements MemoryDeviceUSB.PrintConsole
        Public Event UpdateProgress(ByVal DataTransfered As Integer)
        Private LibusbDeviceFinder As UsbDeviceFinder = New UsbDeviceFinder(&H16C0, &H5DE) 'FCUSB 1.x/2.x
        Public MyFlashDevice As SPI_FLASH 'Contains the definition of the Flash device that is connected
        Public MyFlashStatus As ConnectionStatus = ConnectionStatus.NotDetected

        Private SQI_Mode As Boolean = False 'For devices that boot in SPI mode and change to SQI in real-time

        Sub New()

        End Sub

#Region "USB Hardware Call Flags"
        Private Const SQIREQ_SETUP As Byte = &H70
        Private Const SQIREQ_READBULK1 As Byte = &H71 'Winbond devices
        Private Const SQIREQ_READBULK2 As Byte = &H77 'SST devices
        Private Const SQIREQ_WRITEDATA As Byte = &H72
        Private Const SQIREQ_READDATA As Byte = &H73
        Private Const SQIREQ_SPI_WRITEDATA As Byte = &H74
        Private Const SQIREQ_SPI_READDATA As Byte = &H75
        Private Const SQIREQ_WRITEBULK1 As Byte = &H76 'Used on Winbond devices
        Private Const SQIREQ_WRITEBULK2 As Byte = &H78

        Private Const SPIREQ_ECHO As Byte = &H80
        Private Const SPIREQ_LEDON As Byte = &H81
        Private Const SPIREQ_LEDOFF As Byte = &H82
        Private Const SPIREQ_LEDBLINK As Byte = &H83
        Private Const SPIREQ_SETCFG As Byte = &H84
        Private Const SPIREQ_ENPROGIF As Byte = &H85
        Private Const SPIREQ_VERSION As Byte = &H86
        Private Const SPIREQ_WRITEDATA As Byte = &H87
        Private Const SPIREQ_READDATA As Byte = &H88
        Private Const SPIREQ_SS_HIGH As Byte = &H89
        Private Const SPIREQ_SS_LOW As Byte = &H8A
        Private Const SPIREQ_READBULK As Byte = &H8B
        Private Const SPIREQ_WRITEBULK As Byte = &H8C
        Private Const SPIREQ_WRITEBULK_AAIBYTE As Byte = &H8D
        Private Const SPIREQ_WRITEBULK_AAIWORD As Byte = &H8E
        Private Const SPIREQ_WRITEBULK_ATMEL As Byte = &H8F

        Private Const I2CREQ_INIT As Byte = &H90
        Private Const I2CREQ_START As Byte = &H91
        Private Const I2CREQ_STOP As Byte = &H92
        Private Const I2CREQ_WRITEBYTE As Byte = &H93
        Private Const I2CREQ_READBYTE As Byte = &H94
        Private Const I2CREQ_WRITEPAGE16 As Byte = &H95
        Private Const I2CREQ_READPAGE As Byte = &H96
        Private Const I2CREQ_EESetAddress As Byte = &H97
        Private Const I2CREQ_EEStatus As Byte = &H98
#End Region

        Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
            MyFlashStatus = ConnectionStatus.NotDetected
            If (OperationMode = AvrMode.SPI) Then
                Return SPI_InitDevice()
            ElseIf (OperationMode = AvrMode.EXPIO) Then
                EXT_IF.FCUSB = FCUSB
                MyFlashStatus = EXT_IF.Init
            End If
            Return True
        End Function

        Private Function SPI_InitDevice() As Boolean
            Dim ReadSuccess As Boolean = False
            If (SPI_MODE = SpiDeviceMode.SQI) Then
                SQIBUS_Setup() 'Setup for SQI mode
            Else 'For all other SPI devices
                SPIBUS_Setup()
            End If
            Dim DEVICEID As SPI_IDENT = SPIBUS_ReadDeviceID() 'Sends RDID/REMS/RES command and reads back
            If DEVICEID.MANU = &HFF Or DEVICEID.MANU = 0 Then
            ElseIf (DEVICEID.RDID = &HFFFFFFFFUI) Or (DEVICEID.RDID = 0) Then
                If (DEVICEID.REMS = &HFFFF) Or (DEVICEID.REMS = &H0) Then
                Else
                    ReadSuccess = True 'RDID did not return anything, but REMS did
                End If
            Else 'Read successful!
                ReadSuccess = True
            End If
            If ReadSuccess Then
                If (SPI_MODE = SpiDeviceMode.SQI) Then
                    WriteConsole("Successfully opened device in SQI mode")
                Else
                    WriteConsole("Successfully opened device in SPI mode (8 MHz)") 'RM.GetString("fcusb_spi_openinmode")
                End If
            Else
                RaiseEvent PrintConsole(RM.GetString("fcusb_spi_err5")) 'Unable to connect to compatible SPI device
                Return False
            End If

            Dim RDID_Str As String = "0x" & Hex(DEVICEID.MANU).PadLeft(2, "0") & Hex((DEVICEID.RDID And &HFFFFFF00UL) >> 8).PadLeft(6, "0")
            Dim REMS_Str As String = "0x" & Hex(DEVICEID.REMS).PadLeft(4, "0")

            RaiseEvent PrintConsole(RM.GetString("fcusb_spi_connflash") & " (RDID:" & RDID_Str & " REMS:0x" & REMS_Str & ")")
            MyFlashDevice = FlashDatabase.FindDevice(DEVICEID.MANU, DEVICEID.RDID, MemoryType.SERIAL_NOR)
            If MyFlashDevice IsNot Nothing Then
                MyFlashStatus = ConnectionStatus.Supported
                LoadVendorSpecificConfigurations() 'Some devices will need vendor specific information to load correctly
                If MyFlashDevice.SEND_4BYTE Then SPIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.EN4B) '0xB7
                'Incase any blocks were locked (maybe check SPIDEF) 
                SPIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.ULBPR) '0x98 
                WriteStatusRegister({0})
                RaiseEvent PrintConsole(String.Format(RM.GetString("fcusb_spi_flashdetected"), Me.DeviceName, Format(Me.DeviceSize, "#,###")))
                RaiseEvent PrintConsole(RM.GetString("fcusb_spi_progmode"))
                If (Me.MyFlashDevice.QUAD = SPI_QUADMODE.SST_Micro) Then 'Microchip SST26VF016 etc.  
                    SQIBUS_Setup()
                    Dim ManuData(2) As Byte
                    SPIBUS_WriteRead({&HAF}, ManuData)
                    If (ManuData(0) = DEVICEID.MANU) And (ManuData(1) = CByte((DEVICEID.RDID And &HFF00) >> 8)) And (ManuData(2) = (DEVICEID.RDID And 255)) Then
                        SPIBUS_WriteEnable() 'We want to remove the default block protection
                        SPIBUS_WriteRead({&H42, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}) 'WriteBlockProtection (6 bytes for 26VF016, 10 for 26VF032)
                        RaiseEvent PrintConsole("SQI mode enabled for Microchip / SST device.")
                    Else 'Device failed to enable SQI
                        RaiseEvent PrintConsole("Failed to enter SQI mode, please make sure SIO2 and SIO3 are connected")
                        RaiseEvent PrintConsole("This is required for erasing/programming of the memory device")
                        SQI_EnableSPI()
                    End If
                End If
                Return True
            Else
                RaiseEvent PrintConsole(RM.GetString("fcusb_spi_email")) 'Maybe update this 
                MyFlashDevice = New SPI_FLASH("Unknown", 0, DEVICEID.MANU, DEVICEID.RDID)
                MyFlashStatus = ConnectionStatus.NotSupported
                Return False
            End If
        End Function

        Private Sub LoadVendorSpecificConfigurations()
            If (MyFlashDevice.ProgramMode = SPI_ProgramMode.Atmel45Series) Then 'May need to load the current page mode
                Dim sr() As Byte = ReadStatusRegister() 'Some devices have 2 SR
                If (sr(0) And 1) = 0 Then MyFlashDevice.PAGE_EXTENDED = True
                RaiseEvent PrintConsole("Device configured to page size: " & MyFlashDevice.PAGE_SIZE & " bytes")
            End If
        End Sub

        Private Function EnableWinbondSQIMode() As Boolean
            Try
                SPIBUS_WriteEnable()
                SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.WRSR, 0, 2}, Nothing) '0x01 00 02
                Dim status_reg(0) As Byte
                WaitUntilReady()
                SPIBUS_WriteRead({&H35}, status_reg) '0x5
                If status_reg(0) And 2 = 2 Then Return True 'QE bit is set
            Catch ex As Exception
            End Try
            Return False 'Quad mode is not enabled or supported
        End Function

        Private Function DisableWinbondSQIMode() As Boolean
            Try
                SPIBUS_WriteEnable()
                SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.WRSR, 0, 0}, Nothing) '0x01 00 02
                Dim status_reg(0) As Byte
                WaitUntilReady()
                SPIBUS_WriteRead({&H35}, status_reg)
                If status_reg(0) And 2 = 0 Then Return True 'QE bit is unset
            Catch ex As Exception
            End Try
            Return False
        End Function
        'Sends various READ ID commands to get a response from the chip (9F 90 AB)
        Private Function SPIBUS_ReadDeviceID() As SPI_IDENT
            Dim out_id As New SPI_IDENT
            Dim out_buffer() As Byte
            ReDim out_buffer(4) 'Erase the buffer (and we are going to read back 5 bytes)
            If SPIBUS_WriteRead({SPI_Command_DEF.RDID}, out_buffer) = 6 Then 'READ JEDEC ID (most common) - Default 0x9F
                out_id.MANU = out_buffer(0)
                out_id.RDID = Utilities.Bytes.ToUInteger({out_buffer(1), out_buffer(2), out_buffer(3), out_buffer(4)})
            End If
            ReDim out_buffer(1) 'Erase the buffer
            If SPIBUS_WriteRead({SPI_Command_DEF.REMS, 0, 0, 0}, out_buffer) = 6 Then 'Read Electronic Manufacturer Signature 
                If out_id.MANU = 0 Or out_id.MANU = 255 Then out_id.MANU = out_buffer(0)
                If out_id.RDID = 0 Or out_id.RDID = 255 Then out_id.RDID = out_buffer(1)
                out_id.REMS = (CUShort(out_buffer(0)) << 8) Or out_buffer(1)
            End If
            ReDim out_buffer(0) 'Erase the buffer
            If SPIBUS_WriteRead({SPI_Command_DEF.RES, 0, 0, 0}, out_buffer) = 5 Then 'Read Electronic Signature (PMC / ST M25P10)
                out_id.RES = out_buffer(0)
            End If
            Return out_id
        End Function

        Private Sub SQI_EnableSPI()
            WaitUntilReady()
            SPIBUS_WriteRead({&HFF}) 'Back to SPI mode
            SPIBUS_Setup()
        End Sub


#Region "Selected device properties"

        Friend ReadOnly Property DeviceName() As String Implements MemoryDeviceUSB.DeviceName
            Get
                Select Case MyFlashStatus
                    Case ConnectionStatus.Supported
                        If OperationMode = AvrMode.SPI Then
                            Return MyFlashDevice.NAME
                        Else
                            Return EXT_IF.MyMPDevice.NAME
                        End If
                    Case ConnectionStatus.NotSupported
                        If OperationMode = AvrMode.SPI Then
                            Return Hex(MyFlashDevice.MFG_CODE).PadLeft(2, CChar("0")) & " " & Hex(MyFlashDevice.PART_CODE).PadLeft(4, CChar("0"))
                        Else
                            Return "Unable to detect"
                        End If
                    Case Else
                        Return "No Flash Detacted"
                End Select
            End Get
        End Property

        Friend ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
            Get
                If Not MyFlashStatus = ConnectionStatus.Supported Then Return 0
                If OperationMode = AvrMode.SPI Then
                    Return MyFlashDevice.FLASH_SIZE
                ElseIf OperationMode = AvrMode.EXPIO Then
                    Return EXT_IF.MyMPDevice.FLASH_SIZE
                Else
                    Return 0
                End If
            End Get
        End Property

        Friend ReadOnly Property SectorSize(ByVal sector As Integer) As Long Implements MemoryDeviceUSB.SectorSize
            Get
                If Not MyFlashStatus = ConnectionStatus.Supported Then Return 0
                If (OperationMode = AvrMode.SPI) Then
                    If MyFlashDevice.ERASE_REQUIRED Then
                        Return MyFlashDevice.ERASE_SIZE
                    Else
                        Return MyFlashDevice.FLASH_SIZE
                    End If
                Else
                    Return 0
                End If
            End Get
        End Property

#End Region

#Region "I2C"

        Private I2C_PageSize As Integer 'Number of bytes per page
        Private I2C_FlashSize As Integer 'Number of bytes per device
        Private I2C_AddrSize As Integer 'Number of bytes for the address space
        Private I2C_AddrByte As Byte

        Friend Sub SetI2CSettings(ByVal PageSize As Integer, ByVal FlashSize As Integer, ByVal AddrSize As Integer, ByVal AddrByte As Byte)
            I2C_PageSize = PageSize
            I2C_FlashSize = FlashSize
            I2C_AddrSize = AddrSize
            I2C_AddrByte = AddrByte
        End Sub

        Friend Function ReadData_I2C_EEPROM(ByVal flash_offset As UInt32, ByVal data_count As UInt32) As Byte()
            Dim dataout(data_count - 1) As Byte
            Dim BytesLeft As Integer = data_count
            Dim AddressOffset As Integer = 0
            Do Until BytesLeft = 0
                Dim DataRead() As Byte = Nothing
                If (BytesLeft >= I2C_PageSize) Then
                    ReDim DataRead(I2C_PageSize - 1)
                Else 'less than the page size
                    ReDim DataRead(BytesLeft - 1)
                End If
                Dim ReadResult As Boolean = I2C_ReadBytes(flash_offset + AddressOffset, DataRead, DataRead.Length)
                If Not ReadResult Then Return Nothing
                Array.Copy(DataRead, 0, dataout, AddressOffset, DataRead.Length)
                AddressOffset += DataRead.Length
                BytesLeft -= DataRead.Length
            Loop
            BitSwap_Reverse(dataout)
            Return dataout
        End Function

        Friend Function WriteData_I2C_EEPROM(ByVal flash_offset As UInt32, ByVal data_to_write() As Byte) As Boolean
            BitSwap_Forward(data_to_write)
            Dim BytesLeft As Integer = data_to_write.Length
            Dim AddressOffset As Integer = 0
            Do Until BytesLeft = 0
                Dim DataWrite() As Byte = Nothing
                If (BytesLeft >= I2C_PageSize) Then
                    ReDim DataWrite(I2C_PageSize - 1)
                Else 'less than 32
                    ReDim DataWrite(BytesLeft - 1)
                End If
                Array.Copy(data_to_write, AddressOffset, DataWrite, 0, DataWrite.Length)
                I2C_WriteBytes(flash_offset + AddressOffset, DataWrite)
                AddressOffset += DataWrite.Length
                BytesLeft -= DataWrite.Length
            Loop
            Return True
        End Function

        'Writes up to 64-bytes to I2C EEPROM
        Private Function I2C_WriteBytes(ByVal Addr32 As UInt32, ByVal data() As Byte) As Boolean
            Dim AddrFlag As Byte = I2C_AddrByte
            Dim EE_Addr16 As UInt16 = (Addr32 And &HFFFF) 'Bottom 16 bits of address
            Addr32 = (Addr32 >> (I2C_AddrSize * 8)) 'Carry bits
            If (Addr32 And 1) Then AddrFlag = AddrFlag Or (1 << 1) 'Sets P0 to TRUE
            If (Addr32 And 2) Then AddrFlag = AddrFlag Or (1 << 2) 'Sets P1 to TRUE
            If (Addr32 And 4) Then AddrFlag = AddrFlag Or (1 << 3) 'Sets P2 to TRUE
            I2C_SetAddress(AddrFlag) 'This sets the A0 address byte initial setting

            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbSetupPacket As New UsbSetupPacket(usbflag, I2CREQ_WRITEPAGE16, EE_Addr16, CUShort(I2C_AddrSize), CShort(data.Length))
            Dim ret As Integer = 0
            Dim result As Boolean = FCUSB.ControlTransfer(usbSetupPacket, data, data.Length, ret)
            If Not result Then Return False
            Return True
        End Function
        'Reads up to 64-bytes from I2C EEPROM
        Private Function I2C_ReadBytes(ByVal Addr32 As UInt32, ByRef data_out() As Byte, ByVal count As Integer) As Boolean
            Dim AddrFlag As Byte = I2C_AddrByte
            Dim EE_Addr16 As UInt16 = (Addr32 And &HFFFF) 'Bottom 16 bits of address
            Addr32 = (Addr32 >> (I2C_AddrSize * 8)) 'Carry bits
            If (Addr32 And 1) Then AddrFlag = AddrFlag Or (1 << 1) 'Sets P0 to TRUE
            If (Addr32 And 2) Then AddrFlag = AddrFlag Or (1 << 2) 'Sets P1 to TRUE
            If (Addr32 And 4) Then AddrFlag = AddrFlag Or (1 << 3) 'Sets P2 to TRUE
            I2C_SetAddress(AddrFlag) 'This sets the A0 address byte initial setting
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbSetupPacket As New UsbSetupPacket(usbflag, I2CREQ_READPAGE, EE_Addr16, CUShort(I2C_AddrSize), CShort(count + 1))
            Dim ret As Integer = 0 'Returns how many bytes were transfered
            ReDim data_out(count - 1)
            Dim result As Boolean = FCUSB.ControlTransfer(usbSetupPacket, data_out, data_out.Length, ret)
            If Not result Then Return False
            If (count > 0) AndAlso ret = 0 Then Return False 'Error!
            Return True
        End Function
        'This writes the address byte
        Private Function I2C_SetAddress(ByVal ic2_addr As Byte) As Boolean
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, I2CREQ_EESetAddress, CShort(ic2_addr), 0, 0)
            Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Return res
        End Function

        Public Function IC2_Init() As Boolean
            Try
                If FCUSB Is Nothing Then Return False
                Dim ret As Integer = 0
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, I2CREQ_INIT, 0, 0, 0)
                Return FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Catch ex As Exception
                Return False
            End Try
        End Function

#End Region

#Region "USB Hardware Calls"
        Private SPI_Clock_Divider As Integer = 2 'Check to see if this is used

        Friend Sub LEDOn() Implements MemoryDeviceUSB.LEDOn
            Try
                If FCUSB Is Nothing Then Exit Sub
                Dim ret As Integer = 0
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_LEDON, 0, 0, 0)
                FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Catch ex As Exception
            End Try
        End Sub

        Friend Sub LEDOff() Implements MemoryDeviceUSB.LedOff
            Try
                If FCUSB Is Nothing Then Exit Sub
                Dim ret As Integer = 0
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_LEDOFF, 0, 0, 0)
                FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Catch ex As Exception
            End Try
        End Sub

        Friend Sub LEDBlink() Implements MemoryDeviceUSB.LEDBlink
            Try
                If FCUSB Is Nothing Then Exit Sub
                Dim ret As Integer = 0
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_LEDBLINK, 0, 0, 0)
                FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Catch ex As Exception
            End Try
        End Sub

        Private Function EchoTest() As Boolean
            Try
                Dim buff As Byte() = New Byte(7) {}
                Dim usbSetupPacket As New UsbSetupPacket(CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor), CByte(SPIREQ_ECHO), &H1234, &H5678, 8)
                Dim ret As Integer
                If FCUSB.ControlTransfer(usbSetupPacket, buff, 8, ret) Then
                    If buff(0) <> CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor) Then
                        Return False
                    End If
                    If buff(1) <> CByte(SPIREQ_ECHO) Then Return False
                    If buff(2) <> &H34 Then Return False
                    If buff(3) <> &H12 Then Return False
                    If buff(4) <> &H78 Then Return False
                    If buff(5) <> &H56 Then Return False
                    If buff(6) <> &H8 Then Return False
                    If buff(7) <> &H0 Then Return False
                    Return True
                End If
                MyUSBDeviceID = ""
                FCUSB = Nothing
                Return False
            Catch ex As Exception
                MyUSBDeviceID = ""
                FCUSB = Nothing
                Return False
            End Try
        End Function
        'Returns the version of our firmware
        Public Function GetAvrVersion() As String
            Try
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor)
                Dim usbSetupPacket As New UsbSetupPacket(usbflag, SPIREQ_VERSION, 0, 0, 4)
                Dim ret As Integer
                Dim buff(4) As Byte
                If FCUSB.ControlTransfer(usbSetupPacket, buff, 4, ret) Then
                    buff(4) = buff(3)
                    buff(3) = buff(2)
                    buff(2) = Asc(".")
                End If
                Dim fwstr As String = Utilities.Bytes.ToChrString(buff)
                If fwstr.StartsWith("0") Then fwstr = Mid(fwstr, 2)
                Return fwstr
                'Return Format(Utilities.StringToSingle(fwstr).ToString, "#.##")
            Catch ex As Exception
                Return ""
            End Try
        End Function

        Friend Function IsConnected() As Boolean Implements MemoryDeviceUSB.IsConnected
            If FCUSB Is Nothing Then
                Dim fcusb_list As UsbRegDeviceList = UsbDevice.AllDevices.FindAll(LibusbDeviceFinder)
                If fcusb_list IsNot Nothing AndAlso fcusb_list.Count > 0 Then
                    Dim InUse() As String = GetFlashcatInUse()
                    For i = 0 To fcusb_list.Count - 1
                        Dim u As WinUsb.WinUsbRegistry = fcusb_list(i)
                        Dim o As String = u.DeviceProperties("DeviceID")
                        If MyUSBDeviceID = o Then
                            Return True
                        ElseIf MyUSBDeviceID = "" Then
                            'check other fcusb apps
                            If InUse Is Nothing Then 'No other instances, we can use this one
                                Return True
                            Else
                                Dim FoundAvailableFcusb As Boolean = True
                                For Each item In InUse
                                    If item = o Then FoundAvailableFcusb = False : Exit For
                                Next
                                If FoundAvailableFcusb Then Return True 'This ID is not being used
                            End If
                        End If
                    Next
                End If
                Return False
            Else
                Return FCUSB.UsbRegistryInfo.IsAlive
            End If
        End Function
        'Connects this interface to an available fcusb
        Friend Function Connect() As Boolean Implements MemoryDeviceUSB.Connect
            Dim fcusb_list As UsbRegDeviceList = UsbDevice.AllDevices.FindAll(LibusbDeviceFinder)
            If fcusb_list IsNot Nothing AndAlso fcusb_list.Count > 0 Then
                Dim InUse() As String = GetFlashcatInUse()
                For i = 0 To fcusb_list.Count - 1
                    Dim u As WinUsb.WinUsbRegistry = fcusb_list(i)
                    Dim o As String = u.DeviceProperties("DeviceID")
                    If MyUSBDeviceID = o Then
                        MyUSBDeviceID = o
                        FCUSB = u.Device
                        Return OpenUsbDevice()
                    ElseIf MyUSBDeviceID = "" Then
                        If InUse Is Nothing Then 'No other instances, we can use this one
                            MyUSBDeviceID = o
                            FCUSB = u.Device
                            Return OpenUsbDevice()
                        Else
                            Dim FoundAvailableFcusb As Boolean = True
                            For Each item In InUse
                                If item = o Then FoundAvailableFcusb = False : Exit For
                            Next
                            If FoundAvailableFcusb Then
                                MyUSBDeviceID = o
                                FCUSB = u.Device
                                Return OpenUsbDevice()
                            End If
                        End If
                    End If
                Next
            End If
            Return False 'No FCUSB found
        End Function

        Private Function OpenUsbDevice() As Boolean
            If FCUSB Is Nothing Then Return False
            FCUSB.Open()
            Dim wholeUsbDevice As IUsbDevice = TryCast(FCUSB, IUsbDevice)
            If wholeUsbDevice IsNot Nothing Then 'Libusb only
                wholeUsbDevice.SetConfiguration(1)
                wholeUsbDevice.ClaimInterface(0)
            End If
            SPIBUS_Setup()
            Return EchoTest()
        End Function
        'If we are already connected, this will disconnect
        Friend Function Disconnect() As Boolean Implements MemoryDeviceUSB.Disconnect
            Try
                If FCUSB IsNot Nothing Then FCUSB.Close()
                MyUSBDeviceID = ""
                FCUSB = Nothing
            Catch ex As Exception
            End Try
            Return True
        End Function

        Private Sub UpdateClockDevider(ByVal clock As SPI_CLOCK)
            Select Case clock
                Case SPI_CLOCK.SPI_CLOCK_FOSC_2
                    SPI_Clock_Divider = 2
                Case SPI_CLOCK.SPI_CLOCK_FOSC_4
                    SPI_Clock_Divider = 4
                Case SPI_CLOCK.SPI_CLOCK_FOSC_8
                    SPI_Clock_Divider = 8
                Case SPI_CLOCK.SPI_CLOCK_FOSC_16
                    SPI_Clock_Divider = 16
                Case SPI_CLOCK.SPI_CLOCK_FOSC_32
                    SPI_Clock_Divider = 32
                Case SPI_CLOCK.SPI_CLOCK_FOSC_64
                    SPI_Clock_Divider = 64
                Case SPI_CLOCK.SPI_CLOCK_FOSC_128
                    SPI_Clock_Divider = 128
                Case Else
                    SPI_Clock_Divider = 2
            End Select
        End Sub

        Public Function SPIBUS_SendCommand(ByVal spi_cmd As Byte) As Boolean
            Dim we As Boolean = SPIBUS_WriteEnable()
            If Not we Then Return False
            Return SPIBUS_WriteRead({spi_cmd}, Nothing)
        End Function

        Private Function SPIBUS_WriteEnable() As Boolean
            If SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.WREN}, Nothing) = 1 Then
                Return True
            Else
                Return False
            End If
        End Function

        Private Function SPIBUS_WriteDisable() As Boolean
            If SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.WRDI}, Nothing) = 1 Then
                Return True
            Else
                Return False
            End If
        End Function
        'This writes to the SR (multi-bytes can be input to write as well)
        Public Function WriteStatusRegister(ByVal NewValues() As Byte) As Boolean
            Try
                If NewValues Is Nothing Then Return False
                SPIBUS_WriteEnable() 'Some devices such as AT25DF641 require the WREN and the status reg cleared before we can write data
                'SPIBUS_WriteRead({MyFlashDevice.DEF.EWSR}, Nothing) 'Send the command that we are going to enable-write to register
                'Threading.Thread.Sleep(20) 'Wait a brief moment
                Dim cmd(NewValues.Length) As Byte
                cmd(0) = MyFlashDevice.OP_COMMANDS.WRSR
                Array.Copy(NewValues, 0, cmd, 1, NewValues.Length)
                If Not SPIBUS_WriteRead(cmd, Nothing) = cmd.Length Then Return False
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function ReadStatusRegister(Optional Count As Integer = 1) As Byte()
            Try
                If Count > 64 Then Count = 64
                Dim Output(Count - 1) As Byte
                SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.RDSR}, Output)
                Return Output
                'Read Status Register-1 05h SR1[7: 0] (2) (4)
                'Read Status Register-2 35h SR2[7: 0] (2) (4)
                'Read Status Register-3 33h SR3[7: 0] (2)
            Catch ex As Exception
                Return Nothing 'Erorr
            End Try
        End Function
        'Pulses the RESET pin to enable device program mode for SPI slave (nRF24LE1)
        Public Sub EnableProgMode()
            Try
                Dim DirectionFlag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor)
                Dim usbSetupPacket As New UsbSetupPacket(DirectionFlag, SPIREQ_ENPROGIF, 0, 0, 0)
                Dim ret As Integer
                FCUSB.ControlTransfer(usbSetupPacket, Nothing, 0, ret)
            Catch ex As Exception
            End Try
        End Sub

#End Region

#Region "WriteData Functions"

        Private Function WriteData_Page(ByVal Offset As UInt32, ByVal Data() As Byte) As Boolean
            Try
                Dim psize As UInt32 = MyFlashDevice.PAGE_SIZE
                Dim ret As Integer = 0
                Dim count_upper As UShort = (Data.Length And &HFFFF0000) >> 16
                Dim count_lower As UShort = (Data.Length And &HFFFF)
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim buffer(10) As Byte
                buffer(0) = MyFlashDevice.OP_COMMANDS.PROG
                buffer(1) = MyFlashDevice.OP_COMMANDS.WREN
                buffer(2) = MyFlashDevice.OP_COMMANDS.RDSR
                buffer(3) = CByte(MyFlashDevice.AddressBytes)
                buffer(4) = CByte((psize And &HFF00) >> 8)
                buffer(5) = CByte(psize And &HFF)
                buffer(6) = CByte((Offset And &HFF000000) >> 24)
                buffer(7) = CByte((Offset And &HFF0000) >> 16)
                buffer(8) = CByte((Offset And &HFF00) >> 8)
                buffer(9) = CByte(Offset And &HFF)
                If MyFlashDevice.SEND_RDFS Then 'ReadFlagStatusReg 
                    buffer(10) = MyFlashDevice.OP_COMMANDS.RDFR
                End If
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_WRITEBULK, count_upper, count_lower, CShort(buffer.Length))
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, buffer, buffer.Length, ret)
                If Not res Then Return False
                Dim writer As UsbEndpointWriter = FCUSB.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
                Dim ec As ErrorCode = writer.Write(Data, 0, Data.Length, 5000, ret)
                If ec = ErrorCode.None Then Return True
                Return False
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Function SQI_WriteData_Winbond(ByVal Offset As UInt32, ByVal Data() As Byte) As Boolean
            Try
                Dim psize As UInt32 = MyFlashDevice.PAGE_SIZE
                Dim ret As Integer = 0
                Dim count_upper As UShort = (Data.Length And &HFFFF0000) >> 16
                Dim count_lower As UShort = (Data.Length And &HFFFF)
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim buffer(9) As Byte
                buffer(0) = &H32 'MyFlashDevice.DEF.PROG
                buffer(1) = MyFlashDevice.OP_COMMANDS.WREN
                buffer(2) = MyFlashDevice.OP_COMMANDS.RDSR
                buffer(3) = CByte(MyFlashDevice.AddressBytes)
                buffer(4) = CByte((psize And &HFF00) >> 8)
                buffer(5) = CByte(psize And &HFF)
                buffer(6) = CByte((Offset And &HFF000000) >> 24)
                buffer(7) = CByte((Offset And &HFF0000) >> 16)
                buffer(8) = CByte((Offset And &HFF00) >> 8)
                buffer(9) = CByte(Offset And &HFF)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SQIREQ_WRITEBULK1, count_upper, count_lower, CShort(buffer.Length))
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, buffer, buffer.Length, ret)
                If Not res Then Return False
                Dim writer As UsbEndpointWriter = FCUSB.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
                Dim ec As ErrorCode = writer.Write(Data, 0, Data.Length, 5000, ret)
                If (ec = ErrorCode.None) Then Return True
                Return False
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Function SQI_WriteData(ByVal Offset As UInt32, ByVal Data() As Byte) As Boolean
            Try
                Dim psize As UInt32 = MyFlashDevice.PAGE_SIZE
                Dim ret As Integer = 0
                Dim count_upper As UShort = (Data.Length And &HFFFF0000) >> 16
                Dim count_lower As UShort = (Data.Length And &HFFFF)
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim buffer(9) As Byte
                buffer(0) = MyFlashDevice.OP_COMMANDS.PROG
                buffer(1) = MyFlashDevice.OP_COMMANDS.WREN
                buffer(2) = MyFlashDevice.OP_COMMANDS.RDSR
                buffer(3) = CByte(MyFlashDevice.AddressBytes)
                buffer(4) = CByte((psize And &HFF00) >> 8)
                buffer(5) = CByte(psize And &HFF)
                buffer(6) = CByte((Offset And &HFF000000) >> 24)
                buffer(7) = CByte((Offset And &HFF0000) >> 16)
                buffer(8) = CByte((Offset And &HFF00) >> 8)
                buffer(9) = CByte(Offset And &HFF)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SQIREQ_WRITEBULK2, count_upper, count_lower, CShort(buffer.Length))
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, buffer, buffer.Length, ret)
                If Not res Then Return False
                Dim writer As UsbEndpointWriter = FCUSB.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
                Dim ec As ErrorCode = writer.Write(Data, 0, Data.Length, -1, ret)
                If (ec = ErrorCode.None) Then
                    'Possibly put wait here
                    Return True
                Else
                    Return False
                End If
            Catch ex As Exception
                Return False
            End Try
        End Function
        'Writes the data two bytes at a time using AAI
        Private Function WriteData_AAI_Word(ByVal offset As UInteger, ByVal data_in() As Byte) As Boolean
            If (Not data_in.Length Mod 2 = 0) Then 'We must write a even number of bytes
                ReDim Preserve data_in(data_in.Length)
                data_in(data_in.Length - 1) = 255
            End If
            Dim BulkSize As UInt32 = 2048
            Dim BytesRemaining As UInt32 = data_in.Length
            Dim BytePointer As UInt32 = 0
            Do Until BytesRemaining = 0
                Dim data_out() As Byte
                If (BytesRemaining > BulkSize) Then ReDim data_out(BulkSize - 1) Else ReDim data_out(BytesRemaining - 1)
                Array.Copy(data_in, BytePointer, data_out, 0, data_out.Length)
                BytesRemaining -= data_out.Length
                SPIBUS_WriteEnable()
                Dim BytesToWrite As UInt32 = (data_out.Length - 2) 'We subtract the first two we write on setup packet
                Dim count_upper As UShort = (BytesToWrite And &HFFFF0000) >> 16
                Dim count_lower As UShort = (BytesToWrite And &HFFFF)
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim buffer(8) As Byte
                buffer(0) = MyFlashDevice.OP_COMMANDS.AAI_WORD
                buffer(1) = MyFlashDevice.OP_COMMANDS.RDSR
                buffer(2) = CByte(MyFlashDevice.AddressBytes) 'Should be 3-bytes
                buffer(3) = CByte((offset And &HFF000000) >> 24)
                buffer(4) = CByte((offset And &HFF0000) >> 16)
                buffer(5) = CByte((offset And &HFF00) >> 8)
                buffer(6) = CByte(offset And &HFF)
                buffer(7) = data_out(0) 'We write the first and second byte with the setup packet
                buffer(8) = data_out(1)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_WRITEBULK_AAIWORD, count_upper, count_lower, CShort(buffer.Length))
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, buffer, buffer.Length, Nothing)
                If Not res Then Return False
                Dim writer As UsbEndpointWriter = FCUSB.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
                Dim ec As ErrorCode = writer.Write(data_out, 2, data_out.Length - 1, 2500, Nothing)
                If Not ec = ErrorCode.None Then Return False
                SPIBUS_WriteDisable()
                WaitUntilReady()
                offset += data_out.Length 'Increase the flash address offset
                BytePointer += data_out.Length 'Increase the buffer pointer
            Loop
            Return True 'Write successful
        End Function
        'Writes the data one byte at a time using AAI
        Private Function WriteData_AAI_Byte(ByVal offset As UInteger, ByVal data() As Byte) As Boolean
            SPIBUS_WriteEnable()
            Dim BytesToWrite As UInt32 = (data.Length - 1) 'We subtract 1 because we already write the first byte
            Dim count_upper As UShort = (BytesToWrite And &HFFFF0000) >> 16
            Dim count_lower As UShort = (BytesToWrite And &HFFFF)
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim buffer(7) As Byte
            buffer(0) = MyFlashDevice.OP_COMMANDS.AAI_BYTE
            buffer(1) = MyFlashDevice.OP_COMMANDS.RDSR
            buffer(2) = CByte(MyFlashDevice.AddressBytes)
            buffer(3) = CByte((offset And &HFF000000) >> 24)
            buffer(4) = CByte((offset And &HFF0000) >> 16)
            buffer(5) = CByte((offset And &HFF00) >> 8)
            buffer(6) = CByte(offset And &HFF)
            buffer(7) = data(0) 'We write the first byte with the setup packet
            Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_WRITEBULK_AAIBYTE, count_upper, count_lower, CShort(buffer.Length))
            Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, buffer, buffer.Length, Nothing)
            Dim writer As UsbEndpointWriter = FCUSB.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
            Dim ec As ErrorCode = writer.Write(data, 1, data.Length - 1, 2500, Nothing)
            If Not ec = ErrorCode.None Then Return False
            SPIBUS_WriteDisable()
            WaitUntilReady()
            Return True
        End Function
        'Uses an internal sram buffer to transfer data from the board to the flash (used by Atmel AT45DBxxx)
        Private Function WriteData_ATMEL(ByVal offset As UInteger, ByVal total_data() As Byte) As Boolean
            Dim AddrOffset As Integer = Math.Ceiling(Math.Log(MyFlashDevice.PAGE_SIZE, 2)) 'Number of bits the address is offset
            Dim BytesLeft As Integer = total_data.Length
            Do Until BytesLeft = 0
                Dim BytesToWrite As Integer = BytesLeft
                If BytesToWrite > MyFlashDevice.PAGE_SIZE Then BytesToWrite = MyFlashDevice.PAGE_SIZE
                Dim DataToBuffer(BytesToWrite + 3) As Byte
                DataToBuffer(0) = MyFlashDevice.OP_COMMANDS.WRTB '0x84
                Dim src_ind As Integer = total_data.Length - BytesLeft
                Array.Copy(total_data, src_ind, DataToBuffer, 4, BytesToWrite)
                Dim count_upper As UShort = (DataToBuffer.Length And &HFFFF0000) >> 16
                Dim count_lower As UShort = (DataToBuffer.Length And &HFFFF)
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_WRITEBULK_ATMEL, count_upper, count_lower, CShort(0))
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, Nothing) 'Setup command for buffer write
                Dim writer As UsbEndpointWriter = FCUSB.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
                Dim ec As ErrorCode = writer.Write(DataToBuffer, 0, DataToBuffer.Length, 2500, Nothing)
                If Not ec = ErrorCode.None Then Return False
                WaitUntilReady()
                Dim PageAddr As Integer = Math.Floor(offset / MyFlashDevice.PAGE_SIZE)
                Dim PageCmd() As Byte = Utilities.Bytes.FromUInt24(PageAddr << AddrOffset, False)
                Dim Cmd2() As Byte = {MyFlashDevice.OP_COMMANDS.WRFB, PageCmd(0), PageCmd(1), PageCmd(2)} '0x88
                SPIBUS_WriteRead(Cmd2, Nothing)
                WaitUntilReady()
                offset += BytesToWrite
                BytesLeft -= BytesToWrite
            Loop
            Return True
        End Function
        'Used by the Nordic MCU device
        Private Function WriteData_Nordic(ByVal Offset As UInt32, ByVal Data() As Byte) As Boolean
            'We can write up to 1024 '2 pages
            Dim dataout(Data.Length + 2) As Byte
            dataout(0) = MyFlashDevice.OP_COMMANDS.PROG
            dataout(1) = CByte((Offset >> 8) And &HFF)
            dataout(2) = CByte(Offset And &HFF)
            Array.Copy(Data, 0, dataout, 3, Data.Length)
            SPIBUS_WriteEnable()
            Dim count_upper As UShort = (dataout.Length And &HFFFF0000) >> 16
            Dim count_lower As UShort = (dataout.Length And &HFFFF)
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_WRITEBULK_ATMEL, count_upper, count_lower, CShort(0))
            Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, Nothing) 'Setup command for buffer write
            Dim writer As UsbEndpointWriter = FCUSB.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
            Dim ec As ErrorCode = writer.Write(dataout, 0, dataout.Length, 2500, Nothing)
            WaitUntilReady()
            Return True
        End Function
        'Designed for SPI EEPROMS where each page needs to wait until ready
        Private Function WriteData_SPI_EEPROM(ByVal offset As UInt32, ByVal data_to_write() As Byte) As Boolean
            Dim ReturnValue As Boolean
            Dim DataToWrite As Integer = data_to_write.Length
            For i As Integer = 0 To Math.Ceiling(DataToWrite / MyFlashDevice.PAGE_SIZE) - 1
                Dim BufferSize As Integer = DataToWrite
                If (BufferSize > MyFlashDevice.PAGE_SIZE) Then BufferSize = MyFlashDevice.PAGE_SIZE
                Dim data(BufferSize - 1) As Byte
                Array.Copy(data_to_write, (i * MyFlashDevice.PAGE_SIZE), data, 0, data.Length)
                ReturnValue = WriteData_Page(offset, data)
                'Lets consider putting 100ms delay
                WaitUntilReady()
                If Not ReturnValue Then Return False
                offset += data.Length
                DataToWrite -= data.Length
            Next
            Return True
        End Function

        Private Function WriteData_SPI_EEPROM_8BIT(ByVal flash_offset As UInt32, ByVal data_to_write() As Byte) As Boolean
            Dim DataToWrite As Integer = data_to_write.Length
            For i As Integer = 0 To Math.Ceiling(DataToWrite / MyFlashDevice.PAGE_SIZE) - 1
                Dim BufferSize As Integer = DataToWrite
                If (BufferSize > MyFlashDevice.PAGE_SIZE) Then BufferSize = MyFlashDevice.PAGE_SIZE
                Dim data(BufferSize - 1) As Byte
                Array.Copy(data_to_write, (i * MyFlashDevice.PAGE_SIZE), data, 0, data.Length)
                Dim addr8 As Byte
                If (flash_offset > 255) Then
                    MyFlashDevice.OP_COMMANDS.PROG = CByte(MyFlashDevice.OP_COMMANDS.PROG Or 8) 'Enables 4th bit
                    addr8 = CByte(flash_offset And 255) 'Lower 8 bits only
                Else
                    MyFlashDevice.OP_COMMANDS.PROG = CByte(MyFlashDevice.OP_COMMANDS.PROG And &HF7) 'Disables 4th bit
                    addr8 = CByte(flash_offset)
                End If
                If Not WriteData_Page(addr8, data) Then Return False
                WaitUntilReady()
                flash_offset += data.Length
                DataToWrite -= data.Length
            Next
            Return True
        End Function

#End Region

        Friend Function ReadData(ByVal flash_offset As Long, ByVal data_count As Long) As Byte() Implements MemoryDeviceUSB.ReadData
            Dim dataout(data_count - 1) As Byte
            If (OperationMode = AvrMode.SPI) Then
                If Me.MyFlashDevice.ProgramMode = FlashMemory.SPI_ProgramMode.Atmel45Series Then
                    '528 mode: (13-bits page index) (10 bits is page offset)
                    '512 mode: (13-bits page index) (9 bits is page offset)
                    Dim AddrOffset As Integer = Math.Ceiling(Math.Log(MyFlashDevice.PAGE_SIZE, 2)) 'Number of bits the address is offset
                    Dim PageAddr As Integer = Math.Floor(flash_offset / MyFlashDevice.PAGE_SIZE)
                    Dim PageOffset As Integer = flash_offset - (PageAddr * MyFlashDevice.PAGE_SIZE)
                    Dim addr_bytes() As Byte = Utilities.Bytes.FromUInt24((PageAddr << AddrOffset) + PageOffset, False)
                    'Write OPCMD, 3 byte address, and 4 dummy bytes
                    Dim ReadArraySetup() As Byte = {MyFlashDevice.OP_COMMANDS.READ, addr_bytes(0), addr_bytes(1), addr_bytes(2), 0, 0, 0, 0}
                    SPIBUS_ReadBulk(ReadArraySetup, dataout)
                ElseIf Me.MyFlashDevice.ProgramMode = FlashMemory.SPI_ProgramMode.SPI_EEPROM_8BIT Then  'Used on ST M95010 - M95040 (8bit) and ATMEL devices (AT25010A - AT25040A)
                    Dim read_cmd As Byte = MyFlashDevice.OP_COMMANDS.READ
                    If (flash_offset > 255) Then read_cmd = CByte(read_cmd Or 8) 'Used on M95040 / AT25040A
                    Dim ReadArraySetup() As Byte = GetArrayWithCmdAndAddr(read_cmd, CUInt(flash_offset And 255))
                    SPIBUS_ReadBulk(ReadArraySetup, dataout)
                ElseIf SQI_Mode Then
                    'If (Me.MyFlashDevice.QUAD = QuadMode.Winbond) Then
                    '    ReadArraySetup = GetArrayWithCmdAndAddr(&H6B, flash_offset)
                    '    ReDim Preserve ReadArraySetup(ReadArraySetup.Length) 'Add 1 dummy byte
                    '    SQIBUS_ReadBulk(ReadArraySetup, dataout)
                    'Else
                    If (Me.MyFlashDevice.QUAD = FlashMemory.SPI_QUADMODE.SST_Micro) Then
                        Dim ReadArraySetup() As Byte = GetArrayWithCmdAndAddr(&HB, flash_offset) '0x0B or 0x6B
                        ReDim Preserve ReadArraySetup(ReadArraySetup.Length) 'Add 1 dummy byte
                        Utilities.Sleep(50) 'This needs a delay, otherwise device will not go into SPI mode
                        SQI_EnableSPI()
                        SPIBUS_ReadBulk(ReadArraySetup, dataout)
                        SQIBUS_Setup()
                    Else 'Normal SQI mode
                        Dim ReadArraySetup() As Byte = GetArrayWithCmdAndAddr(MyFlashDevice.OP_COMMANDS.READ, flash_offset)
                        ReDim Preserve ReadArraySetup(ReadArraySetup.Length) 'Add 1 dummy byte
                        SQIBUS_ReadBulk(ReadArraySetup, dataout)
                    End If
                Else
                    Dim ReadArraySetup() As Byte = GetArrayWithCmdAndAddr(MyFlashDevice.OP_COMMANDS.READ, flash_offset)
                    SPIBUS_ReadBulk(ReadArraySetup, dataout) 'This is what sends the actual USB hardware signals
                End If
            End If
            Return dataout
        End Function

        Friend Function WriteData(ByVal flash_offset As Long, ByVal data_to_write() As Byte) As Boolean Implements MemoryDeviceUSB.WriteData
            Dim ReturnValue As Boolean = False
            If (SQI_Mode AndAlso (MyFlashDevice.QUAD = FlashMemory.SPI_QUADMODE.Winbond)) Then
                Return SQI_WriteData_Winbond(flash_offset, data_to_write)
            ElseIf (SQI_Mode AndAlso (MyFlashDevice.QUAD = FlashMemory.SPI_QUADMODE.SST_Micro)) Then
                Return SQI_WriteData(flash_offset, data_to_write)
            ElseIf (SPI_MODE = SpiDeviceMode.SQI) Then
                Return SQI_WriteData(flash_offset, data_to_write)
            ElseIf (OperationMode = AvrMode.SPI) Then
                Select Case MyFlashDevice.ProgramMode
                    Case FlashMemory.SPI_ProgramMode.PageMode
                        ReturnValue = WriteData_Page(flash_offset, data_to_write)
                    Case FlashMemory.SPI_ProgramMode.SPI_EEPROM 'Used on most ST M95080 and above
                        ReturnValue = WriteData_SPI_EEPROM(flash_offset, data_to_write)
                    Case FlashMemory.SPI_ProgramMode.SPI_EEPROM_8BIT 'Used on ST M95010 - M95040
                        ReturnValue = WriteData_SPI_EEPROM_8BIT(flash_offset, data_to_write)
                    Case FlashMemory.SPI_ProgramMode.AAI_Byte
                        ReturnValue = WriteData_AAI_Byte(flash_offset, data_to_write)
                    Case FlashMemory.SPI_ProgramMode.AAI_Word
                        ReturnValue = WriteData_AAI_Word(flash_offset, data_to_write)
                    Case FlashMemory.SPI_ProgramMode.Atmel45Series
                        ReturnValue = WriteData_ATMEL(flash_offset, data_to_write)
                    Case FlashMemory.SPI_ProgramMode.Nordic
                        ReturnValue = WriteData_Nordic(flash_offset, data_to_write)
                End Select
                'If MyFlashDevice.SEND_RDFS Then ReadFlagStatusRegister()
                Return ReturnValue
            End If
            Return False
        End Function
        'Erases the entire chip (sets all pages/sectors to 0xFF)
        Friend Function ChipErase() As Boolean Implements MemoryDeviceUSB.ChipErase
            If OperationMode = AvrMode.SPI Then
                Dim res As Integer
                Dim readbackcount As Integer
                If MyFlashDevice.ProgramMode = FlashMemory.SPI_ProgramMode.Atmel45Series Then
                    readbackcount = 4
                    res = SPIBUS_WriteRead({&HC7, &H94, &H80, &H9A}, Nothing)
                ElseIf MyFlashDevice.ProgramMode = FlashMemory.SPI_ProgramMode.SPI_EEPROM_8BIT Then
                    Dim data(MyFlashDevice.FLASH_SIZE - 1) As Byte
                    For i = 0 To data.Length - 1
                        data(i) = 255
                    Next
                    WriteData(0, data)
                ElseIf MyFlashDevice.ProgramMode = FlashMemory.SPI_ProgramMode.Nordic Then 'We do NOT want to use bulk erase, since that erases NV data and IP page!
                    Dim t As New Stopwatch : t.Start()
                    RaiseEvent PrintConsole(String.Format(RM.GetString("fcusb_spi_erasingbulk"), Format(Me.DeviceSize, "#,###")))
                    Dim TotalPages As Integer = MyFlashDevice.FLASH_SIZE / MyFlashDevice.PAGE_SIZE
                    For i = 0 To TotalPages - 1
                        SPIBUS_WriteEnable() : Utilities.Sleep(50)
                        SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.BE, i}, Nothing)
                        WaitUntilReady()
                    Next
                    RaiseEvent PrintConsole(String.Format(RM.GetString("fcusb_spi_erasecomplete"), Format(t.ElapsedMilliseconds / 1000, "#.##")))
                    Return True
                Else
                    SPIBUS_WriteEnable()
                    readbackcount = 1
                    res = SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.BE}, Nothing)
                End If
                If res = readbackcount Then
                    Dim t As New Stopwatch : t.Start()
                    RaiseEvent PrintConsole(String.Format(RM.GetString("fcusb_spi_erasingbulk"), Format(Me.DeviceSize, "#,###")))
                    WaitUntilReady()
                    RaiseEvent PrintConsole(String.Format(RM.GetString("fcusb_spi_erasecomplete"), Format(t.ElapsedMilliseconds / 1000, "#.##")))
                    Return True
                Else
                    Return False
                End If
            End If
            Return False
        End Function
        'Reads the SPI status register and waits for the device to complete its current operation
        Friend Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
            Try
                If (OperationMode = AvrMode.SPI) Then
                    Dim Status As UInt32
                    If MyFlashDevice.ProgramMode = SPI_ProgramMode.Atmel45Series Then
                        Do
                            Dim sr() As Byte = ReadStatusRegister()
                            Status = sr(0)
                            If Not ((Status And &H80) > 0) Then Utilities.Sleep(50)
                        Loop While Not ((Status And &H80) > 0)
                    Else
                        Do
                            Dim sr() As Byte = ReadStatusRegister()
                            Status = sr(0)
                            If AppIsClosing Then Exit Sub
                            If Status = 255 Then Exit Do
                            If (Status And 1) Then Utilities.Sleep(25)
                        Loop While (Status And 1)
                        If MyFlashDevice IsNot Nothing AndAlso MyFlashDevice.ProgramMode = SPI_ProgramMode.Nordic Then
                            Utilities.Sleep(50)
                        End If
                    End If
                ElseIf OperationMode = AvrMode.EXPIO Then
                    If EXT_IF.MyMPDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
                        EXT_IF.NAND_WaitForReady()
                    Else
                        EXT_IF.NOR_WaitForReady()
                    End If
                End If
            Catch ex As Exception
            End Try
        End Sub

        Public Function FindSectorBase(ByVal SectorIndex As Long) As Long Implements MemoryDeviceUSB.Sector_Find
            If SectorIndex = 0 Then Return 0 'Addresses start at the base address 
            If OperationMode = AvrMode.SPI Then 'Uniform sectors
                Return Me.SectorSize(0) * SectorIndex
            Else
                Return 0
            End If
        End Function

        Public Function Sector_Erase(ByVal Sector As Long) As Boolean Implements MemoryDeviceUSB.Sector_Erase
            Dim offset As UInteger = FindSectorBase(Sector)
            If OperationMode = AvrMode.SPI Then 'Following is compatible with SPI and SQI
                If MyFlashDevice.ProgramMode = SPI_ProgramMode.Atmel45Series Then
                    Dim AddrOffset As Integer = Math.Ceiling(Math.Log(MyFlashDevice.PAGE_SIZE, 2)) 'Number of bits the address is offset
                    Dim blocknum As Integer = Math.Floor(offset / MyFlashDevice.ERASE_SIZE)
                    Dim addrbytes() As Byte = Utilities.Bytes.FromUInt24(blocknum << (AddrOffset + 3), False)
                    SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.SE, addrbytes(0), addrbytes(1), addrbytes(2)}, Nothing)
                ElseIf MyFlashDevice.ProgramMode = SPI_ProgramMode.Nordic Then
                    SPIBUS_WriteEnable() : Utilities.Sleep(50)
                    Dim PageNum As Byte = Math.Floor(offset / 512)
                    SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.SE, PageNum}, Nothing)
                Else
                    If (MyFlashDevice.ERASE_REQUIRED) Then
                        SPIBUS_WriteEnable()
                        Dim DataToWrite() As Byte = GetArrayWithCmdAndAddr(MyFlashDevice.OP_COMMANDS.SE, offset)
                        SPIBUS_WriteRead(DataToWrite, Nothing)
                        If MyFlashDevice.SEND_RDFS Then
                            ReadFlagStatusRegister()
                        Else
                            Utilities.Sleep(100) 'Was 200
                        End If
                    End If
                End If
                WaitUntilReady()
            End If
            Return True
        End Function
        'Returns the total number of sectors (actually number of flash pages)
        Public Function Sectors_Count() As Long Implements MemoryDeviceUSB.Sectors_Count
            If MyFlashStatus = ConnectionStatus.Supported Then
                If (OperationMode = AvrMode.SPI) Then
                    Dim EraseSize As UInt32 = MyFlashDevice.ERASE_SIZE
                    Dim FlashSize As UInt32 = Me.DeviceSize()
                    If FlashSize < EraseSize Then Return 1
                    Return CInt(FlashSize / EraseSize)
                End If
            End If
            Return 0
        End Function
        'Writes data to a given sector and also swaps bytes (endian for words/halfwords)
        Public Function WriteSector(ByVal Sector As Integer, ByVal data() As Byte) As Boolean Implements MemoryDeviceUSB.Sector_Write
            Dim Addr32 As UInteger = Me.FindSectorBase(Sector)
            Return WriteData(Addr32, data)
        End Function
        'Changes the Page Size configuration bit in nonvol
        Public Function Atmel_SetPageConfiguration(ByVal EnableExtPage As Boolean) As Boolean
            If (Not EnableExtPage) Then
                Dim ReadBack As Integer = SPIBUS_WriteRead({&H3D, &H2A, &H80, &HA6}, Nothing)
                WaitUntilReady()
                If ReadBack = 4 Then Return True
            Else
                Dim ReadBack As Integer = SPIBUS_WriteRead({&H3D, &H2A, &H80, &HA7}, Nothing)
                WaitUntilReady()
                If ReadBack = 4 Then Return True
            End If
            Return False
        End Function

        Private Function GetArrayWithCmdAndAddr(ByVal cmd As Byte, ByVal addr_offset As UInt32) As Byte()
            Dim addr_data() As Byte = BitConverter.GetBytes(addr_offset)
            ReDim Preserve addr_data(MyFlashDevice.AddressBytes - 1)
            Array.Reverse(addr_data)
            Dim data_out(MyFlashDevice.AddressBytes) As Byte
            data_out(0) = cmd
            For i = 1 To data_out.Length - 1
                data_out(i) = addr_data(i - 1)
            Next
            Return data_out
        End Function

        Friend Structure SPI_IDENT
            Friend MANU As Byte '(MFG)
            Friend RDID As UInt32 '(ID1,ID2,ID3,ID4)
            Friend REMS As UInt16 '(MFG)(ID1)
            Friend RES As Byte '(MFG)
        End Structure
        'Writes (optional) and Reads (optional) up to 64 byte buffers. SPI and SQI compatible.
        Private Function SPIBUS_WriteRead(ByVal WriteBuffer() As Byte, Optional ByRef ReadBuffer() As Byte = Nothing) As UInt32
            If WriteBuffer Is Nothing And ReadBuffer Is Nothing Then Return 0
            If WriteBuffer IsNot Nothing AndAlso WriteBuffer.Length > 64 Then Return 0
            If ReadBuffer IsNot Nothing AndAlso ReadBuffer.Length > 64 Then Return 0
            Dim TotalBytesTransfered As UInt32 = 0
            If (WriteBuffer IsNot Nothing) Then
                Dim BytesWritten As Integer = 0
                Dim Result As Boolean
                If (Global.FlashcatUSB.MainApp.SPI_MODE = Global.FlashcatUSB.MainApp.SpiDeviceMode.SQI) Then
                    Result = SQIBUS_WriteData(WriteBuffer, BytesWritten)
                Else
                    If SQI_Mode Then
                        If (Me.MyFlashDevice.QUAD = FlashMemory.SPI_QUADMODE.Winbond) Then
                            Result = SQIBUS_SPI_WriteData(WriteBuffer, BytesWritten)
                        ElseIf (Me.MyFlashDevice.QUAD = FlashMemory.SPI_QUADMODE.SST_Micro) Then
                            Result = SQIBUS_WriteData(WriteBuffer, BytesWritten)
                        End If
                    Else
                        Result = SPIBUS_WriteData(WriteBuffer, BytesWritten)
                    End If
                End If
                If Result Then TotalBytesTransfered += BytesWritten
            Else
                SPIBUS_SlaveSelect_Enable()
            End If
            If (ReadBuffer IsNot Nothing) Then
                Dim BytesRead As Integer = 0
                Dim Result As Boolean = False
                If (Global.FlashcatUSB.MainApp.SPI_MODE = Global.FlashcatUSB.MainApp.SpiDeviceMode.SQI) Then
                    Result = SQIBUS_ReadData(ReadBuffer, BytesRead)
                Else
                    If SQI_Mode Then
                        If (Me.MyFlashDevice.QUAD = FlashMemory.SPI_QUADMODE.Winbond) Then
                            Result = SQIBUS_SPI_ReadData(ReadBuffer, BytesRead)
                        ElseIf (Me.MyFlashDevice.QUAD = FlashMemory.SPI_QUADMODE.SST_Micro) Then
                            Result = SQIBUS_ReadData(ReadBuffer, BytesRead)
                        End If
                    Else
                        Result = SPIBUS_ReadData(ReadBuffer, BytesRead)
                    End If
                End If
                If Result Then TotalBytesTransfered += BytesRead
            ElseIf WriteBuffer IsNot Nothing Then
                SPIBUS_SlaveSelect_Disable()
            End If
            Return TotalBytesTransfered
        End Function

        Private Sub ReadFlagStatusRegister()
            Dim flag() As Byte = {0}
            Do
                SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.RDFR}, flag)
            Loop Until ((flag(0) >> 7) And 1)
        End Sub

        Friend Function SPIBUS_Setup() As Boolean
            Try
                Dim clock As SPI_CLOCK = SPI_CLOCK.SPI_CLOCK_FOSC_2
                Dim mode As SPI_FOSC_MODE = SPI_FOSC_MODE.SPI_MODE_0
                Dim order As SPI_ORDER = SPI_ORDER.SPI_ORDER_MSB_FIRST
                Dim spiConf As Short = CShort(CByte(clock) Or CByte(mode) Or CByte(order))
                Dim usbSetupPacket As New UsbSetupPacket(CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor), SPIREQ_SETCFG, spiConf, 0, 0)
                Dim ret As Integer
                If FCUSB.ControlTransfer(usbSetupPacket, Nothing, 0, ret) Then
                    UpdateClockDevider(clock)
                    SQI_Mode = False
                    Return True
                End If
                Return False
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Function SPIBUS_WriteData(ByVal Data_Out() As Byte, Optional ByRef BytesWritten As Integer = 0) As Boolean
            Try
                Dim ret As Integer = 0
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_WRITEDATA, 0, 0, CShort(Data_Out.Length))
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, Data_Out, Data_Out.Length, BytesWritten)
                Return res
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Function SPIBUS_ReadData(ByRef Data_In() As Byte, Optional ByRef BytesRead As Integer = 0) As Boolean
            Try
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_READDATA, 0, 0, CShort(Data_In.Length))
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, Data_In, Data_In.Length, BytesRead)
                Return res
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Function SPIBUS_ReadBulk(ByVal data_packet() As Byte, ByRef data_out() As Byte) As Boolean
            Try
                Dim count_upper As UShort = (data_out.Length And &HFFFF0000) >> 16
                Dim count_lower As UShort = (data_out.Length And &HFFFF)
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbpacket1 As New UsbSetupPacket(usbflag, SPIREQ_READBULK, count_upper, count_lower, CShort(data_packet.Length))
                FCUSB.ControlTransfer(usbpacket1, data_packet, data_packet.Length, Nothing)
                Dim reader As UsbEndpointReader = FCUSB.OpenEndpointReader(ReadEndpointID.Ep01, data_out.Length, EndpointType.Bulk)
                Dim ec As ErrorCode = reader.Read(data_out, 0, CInt(data_out.Length), 500, Nothing)
                If (Not ec = ErrorCode.None) Then Return False
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function
        'Makes the CS/SS pin go low
        Private Sub SPIBUS_SlaveSelect_Enable()
            Try
                Dim ret As Integer = 0
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_SS_HIGH, 0, 0, 0)
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Catch ex As Exception
            End Try
        End Sub
        'Releases the CS/SS pin
        Private Sub SPIBUS_SlaveSelect_Disable()
            Try
                Dim ret As Integer = 0
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_SS_LOW, 0, 0, 0)
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Catch ex As Exception
            End Try
        End Sub
        'Enables FCUSB to setup into SQI mode
        Private Function SQIBUS_Setup() As Boolean
            If (MyFlashDevice IsNot Nothing) AndAlso (MyFlashDevice.QUAD = SPI_QUADMODE.SST_Micro) Then 'Winbond does not need this
                SPIBUS_SendCommand(&H38) 'EQIO (Enable Quad IO) via SPI
            End If
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, SQIREQ_SETUP, CShort(0), 0, 0)
            Dim Result As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            If Result Then SQI_Mode = True Else SQI_Mode = False
            If (MyFlashDevice.QUAD = SPI_QUADMODE.SST_Micro) Then
                Dim Status As UInt32
                Dim Counter As Integer = 0
                Do
                    SQIBUS_WriteData({MyFlashDevice.OP_COMMANDS.RDSR})
                    Dim status_reg(0) As Byte
                    SQIBUS_ReadData(status_reg)
                    Status = status_reg(0)
                    If AppIsClosing Then Return False
                    If (Status And 1) Then Utilities.Sleep(50)
                    Counter += 1
                    If Counter = 50 Then Return False
                Loop While (Status And 1)
            End If
            Return Result
        End Function

        Private Function SQIBUS_WriteData(ByVal Data_Out() As Byte, Optional ByRef BytesWritten As Integer = 0) As Boolean
            Try
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SQIREQ_WRITEDATA, 0, 0, CShort(Data_Out.Length))
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, Data_Out, Data_Out.Length, BytesWritten)
                Return res
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Function SQIBUS_ReadData(ByRef Data_In() As Byte, Optional ByRef BytesRead As Integer = 0) As Boolean
            Try
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SQIREQ_READDATA, 0, 0, CShort(Data_In.Length))
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, Data_In, Data_In.Length, BytesRead)
                Return res
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Function SQIBUS_SPI_WriteData(ByVal WriteBuffer() As Byte, Optional ByRef BytesWritten As Integer = 0) As Boolean
            If WriteBuffer Is Nothing OrElse WriteBuffer.Length = 0 Then Return False
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, SQIREQ_SPI_WRITEDATA, 0, 0, CShort(WriteBuffer.Length))
            Return FCUSB.ControlTransfer(usbPacket2, WriteBuffer, WriteBuffer.Length, BytesWritten)
        End Function

        Private Function SQIBUS_SPI_ReadData(ByRef ReadBuffer() As Byte, Optional ByRef BytesRead As Integer = 0) As Boolean
            If ReadBuffer Is Nothing OrElse ReadBuffer.Length = 0 Then Return False
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, SQIREQ_SPI_READDATA, 0, 0, CShort(ReadBuffer.Length))
            Return FCUSB.ControlTransfer(usbPacket2, ReadBuffer, ReadBuffer.Length, BytesRead)
        End Function
        'Writes the command using SQI, then reads the data using SQI. Used by SST devices. 
        Private Function SQIBUS_ReadBulk(ByVal data_packet() As Byte, ByRef data_out() As Byte) As Boolean
            Try
                'SQIBUS_SPI_WriteData(data_packet) 'First write data packet using normal SPI mode (and SS enabled gets enabled)
                Dim count_upper As UShort = (data_out.Length And &HFFFF0000) >> 16
                Dim count_lower As UShort = (data_out.Length And &HFFFF)
                'Writes the command to put the device into read-array mode
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbpacket1 As New UsbSetupPacket(usbflag, SQIREQ_READBULK2, count_upper, count_lower, CShort(data_packet.Length))
                FCUSB.ControlTransfer(usbpacket1, data_packet, data_packet.Length, Nothing)
                'Open up our endpoint reader to do mass-reading from the device
                Dim reader As UsbEndpointReader = FCUSB.OpenEndpointReader(ReadEndpointID.Ep01, data_out.Length, EndpointType.Bulk)
                reader.Read(data_out, 0, CInt(data_out.Length), 5000, Nothing)
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function
        'Used by Winbond devices. Writes the command using SPI, then reads the data using SQI.
        Private Function SQIBUS_ReadBulk_Winbond(ByVal data_packet() As Byte, ByRef data_out() As Byte) As Boolean
            Try
                'SQIBUS_SPI_WriteData(data_packet) 'First write data packet using normal SPI mode (and SS enabled gets enabled)
                Dim count_upper As UShort = (data_out.Length And &HFFFF0000) >> 16
                Dim count_lower As UShort = (data_out.Length And &HFFFF)
                'Writes the command to put the device into read-array mode
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbpacket1 As New UsbSetupPacket(usbflag, SQIREQ_READBULK1, count_upper, count_lower, CShort(data_packet.Length))
                FCUSB.ControlTransfer(usbpacket1, data_packet, data_packet.Length, Nothing)
                'Open up our endpoint reader to do mass-reading from the device
                Dim reader As UsbEndpointReader = FCUSB.OpenEndpointReader(ReadEndpointID.Ep01, data_out.Length, EndpointType.Bulk)
                reader.Read(data_out, 0, CInt(data_out.Length), 5000, Nothing)
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

    End Class

    Enum SPI_CLOCK As Byte
        SPI_CLOCK_FOSC_2 = &H80
        SPI_CLOCK_FOSC_4 = &H0
        SPI_CLOCK_FOSC_8 = &H81
        SPI_CLOCK_FOSC_16 = &H1
        SPI_CLOCK_FOSC_32 = &H82
        SPI_CLOCK_FOSC_64 = &H2 'or 0x83
        SPI_CLOCK_FOSC_128 = &H3
        SPI_UNSPECIFIED = &HFF
    End Enum

    Enum SPI_FOSC_MODE As Byte
        SPI_MODE_0 = &H0
        SPI_MODE_1 = &H4
        SPI_MODE_2 = &H8
        SPI_MODE_3 = &HC
        SPI_UNSPECIFIED = &HFF
    End Enum

    Enum SPI_ORDER As Byte
        SPI_ORDER_MSB_FIRST = &H0
        SPI_ORDER_LSB_FIRST = &H20
        SPI_UNSPECIFIED = &HFF
    End Enum

    Public Enum ConnectionStatus
        NotDetected = 0
        Supported = 1
        NotSupported = 2
    End Enum


End Namespace
