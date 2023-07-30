'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2017 - ALL RIGHTS RESERVED
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
        Public Property Multi_IO As SPI_IO_MODE = SPI_IO_MODE.SPI 'This flag indicates if the device IO is in dual/quad mode
        Public Property DIE_SELECTED As Integer = 0

        Sub New()

        End Sub

        Enum SPI_IO_MODE As Byte
            SPI = 0
            DUAL = 2
            QUAD = 4
        End Enum

#Region "USB Hardware Call Flags"
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
        Private Const SPIREQ_READFLASH As Byte = &H8B
        Private Const SPIREQ_WRITEBULK As Byte = &H8C
        Private Const SPIREQ_WRITEBULK_AAIBYTE As Byte = &H8D
        Private Const SPIREQ_WRITEBULK_AAIWORD As Byte = &H8E
        Private Const SPIREQ_WRITEBULK_AT45 As Byte = &H8F
        Private Const SQIREQ_SETUP As Byte = &H70
        Private Const SQIREQ_READDATA As Byte = &H71 'Only control EP supported (64 bytes)
        Private Const SQIREQ_WRITEDATA As Byte = &H72 'Only control EP supported (64 bytes)
        Private Const SQIREQ_READFLASH As Byte = &H74
        Private Const SQIREQ_WRITEFLASH As Byte = &H75
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
                Dim spi_connected As Boolean = False
                If Not spi_connected Then
                    spi_connected = SPI_InitDevice()
                End If
                If Not spi_connected Then
                    spi_connected = SQI_InitDevice()
                End If
                'DO DEBUG HERE
                Return spi_connected
            ElseIf (OperationMode = AvrMode.EXPIO) Then
                EXT_IF.FCUSB = FCUSB
                RaiseEvent PrintConsole("Initializing EXT I/O board")
                MyFlashStatus = EXT_IF.Init
            End If
            Return True
        End Function

        Private Function SPI_InitDevice() As Boolean
            SPIBUS_Setup(SPI_SPEED.MHZ_8)
            Dim ReadSuccess As Boolean = False
            Dim DEVICEID As SPI_IDENT = SPIBUS_ReadDeviceID() 'Sends RDID/REMS/RES command and reads back
            If DEVICEID.MANU = &HFF Or DEVICEID.MANU = 0 Then
            ElseIf (DEVICEID.RDID = &HFFFFFFFFUI) Or (DEVICEID.RDID = 0) Then
                If Not ((DEVICEID.REMS = &HFFFF) Or (DEVICEID.REMS = &H0)) Then
                    ReadSuccess = True 'RDID did not return anything, but REMS did
                End If
            Else 'Read successful!
                ReadSuccess = True
            End If
            If ReadSuccess Then
                WriteConsole("Successfully opened device in SPI mode (8 MHz)") 'RM.GetString("fcusb_spi_openinmode") 
            Else
                RaiseEvent PrintConsole(RM.GetString("fcusb_spi_err5")) 'Unable to connect to compatible SPI device
                Return False
            End If
            Dim RDID_Str As String = "0x" & Hex(DEVICEID.MANU).PadLeft(2, "0") & Hex((DEVICEID.RDID And &HFFFF0000UL) >> 16).PadLeft(4, "0")
            Dim RDID2_Str As String = Hex(DEVICEID.RDID And &HFFFF).PadLeft(4, "0")
            Dim REMS_Str As String = "0x" & Hex(DEVICEID.REMS).PadLeft(4, "0")
            RaiseEvent PrintConsole(RM.GetString("fcusb_spi_connflash") & " (RDID:" & RDID_Str & " " & RDID2_Str & " REMS:" & REMS_Str & ")")
            Dim ID1 As UInt16 = (DEVICEID.RDID >> 16)
            Dim ID2 As UInt16 = (DEVICEID.RDID And &HFFFF)
            MyFlashDevice = FlashDatabase.FindDevice(DEVICEID.MANU, ID1, ID2, False, MemoryType.SERIAL_NOR)
            If MyFlashDevice IsNot Nothing Then
                MyFlashStatus = ConnectionStatus.Supported
                LoadDeviceConfigurations() 'Does device settings (4BYTE mode, unlock global block)
                LoadVendorSpecificConfigurations() 'Some devices may need additional configurations
                RaiseEvent PrintConsole(String.Format(RM.GetString("fcusb_spi_flashdetected"), Me.DeviceName, Format(Me.DeviceSize, "#,###")))
                RaiseEvent PrintConsole("Page erase size: " & Format(MyFlashDevice.ERASE_SIZE, "#,###") & " bytes")
                RaiseEvent PrintConsole(RM.GetString("fcusb_spi_progmode"))
                Return True
            Else
                RaiseEvent PrintConsole(RM.GetString("fcusb_spi_email")) 'Maybe update this 
                MyFlashDevice = New SPI_FLASH("Unknown", 0, DEVICEID.MANU, DEVICEID.RDID)
                MyFlashStatus = ConnectionStatus.NotSupported
                Return False
            End If
        End Function
        'Inits the device using SQI protocol
        Private Function SQI_InitDevice() As Boolean
            SQIBUS_Setup() 'Setup for SQI mode
            Dim DEVICEID As New SPI_IDENT
            Dim out_buffer(3) As Byte
            If SPIBUS_WriteRead({&HAF}, out_buffer) = 5 Then 'MULTIPLE I/O READ ID
                DEVICEID.MANU = out_buffer(0)
                DEVICEID.RDID = Utilities.Bytes.ToUInteger({0, 0, out_buffer(1), out_buffer(2)})
            Else
                Return False
            End If
            If DEVICEID.MANU = &HFF Or DEVICEID.MANU = 0 Then Return False
            WriteConsole("Successfully opened device in SQI mode (2 MHz)")
            Dim RDID_Str As String = "0x" & Hex(DEVICEID.MANU).PadLeft(2, "0") & Hex(DEVICEID.RDID).PadLeft(4, "0")
            RaiseEvent PrintConsole(RM.GetString("fcusb_spi_connflash") & " (RDID:" & RDID_Str & ")")
            Dim ID1 As UInt16 = (DEVICEID.RDID >> 16)
            Dim ID2 As UInt16 = (DEVICEID.RDID And &HFFFF)
            MyFlashDevice = FlashDatabase.FindDevice(DEVICEID.MANU, ID1, ID2, False, MemoryType.SERIAL_NOR)
            If MyFlashDevice IsNot Nothing Then
                If MyFlashDevice.QUAD = SPI_QUADMODE.NotSupported Then
                    RaiseEvent PrintConsole("Device opened in SQI mode but not supported in Flash library") 'Maybe update this 
                    MyFlashDevice = New SPI_FLASH("Unknown", 0, DEVICEID.MANU, DEVICEID.RDID)
                    MyFlashStatus = ConnectionStatus.NotSupported
                    Return False
                End If
                MyFlashStatus = ConnectionStatus.Supported
                LoadDeviceConfigurations() 'Does device settings (4BYTE mode, unlock global block)
                LoadVendorSpecificConfigurations() 'Some devices may need additional configurations
                RaiseEvent PrintConsole(String.Format(RM.GetString("fcusb_spi_flashdetected"), Me.DeviceName, Format(Me.DeviceSize, "#,###")))
                RaiseEvent PrintConsole("Programming mode: SQI (SPI-QUAD)")
                Return True
            Else
                RaiseEvent PrintConsole(RM.GetString("fcusb_spi_email"))
                MyFlashDevice = New SPI_FLASH("Unknown", 0, DEVICEID.MANU, DEVICEID.RDID)
                MyFlashStatus = ConnectionStatus.NotSupported
                Return False
            End If
        End Function

        Private Sub LoadVendorSpecificConfigurations()
            If (MyFlashDevice.ProgramMode = SPI_ProgramMode.Atmel45Series) Then 'May need to load the current page mode
                Dim sr() As Byte = ReadStatusRegister() 'Some devices have 2 SR
                Dim page_size As UInt32 = MyFlashDevice.PAGE_SIZE
                MyFlashDevice.EXTENDED_MODE = False
                If (sr(0) And 1) = 0 Then 'Device uses extended pages
                    MyFlashDevice.EXTENDED_MODE = True
                    page_size = MyFlashDevice.PAGE_SIZE_EXTENDED
                    MyFlashDevice.FLASH_SIZE = MyFlashDevice.PAGE_COUNT * MyFlashDevice.PAGE_SIZE_EXTENDED
                    MyFlashDevice.ERASE_SIZE = (MyFlashDevice.PAGE_SIZE_EXTENDED * 8) 'Block erase = 8 pages
                End If
                RaiseEvent PrintConsole("Device configured to page size: " & page_size & " bytes")
            End If
            If (MyFlashDevice.MFG_CODE = &HBF) Then 'SST26VF016/SST26VF032 requires block protection to be removed in SQI only
                If MyFlashDevice.ID1 = &H2601 Or MyFlashDevice.ID1 = &H2602 Then
                    If Multi_IO = SPI_IO_MODE.SPI Then SQIBUS_Setup()
                    Dim ManuData(2) As Byte
                    SPIBUS_WriteRead({&HAF}, ManuData)
                    If (ManuData(0) = &HBF) And (ManuData(1) = CByte((MyFlashDevice.ID1 And &HFF00) >> 8)) And (ManuData(2) = (MyFlashDevice.ID1 And 255)) Then
                        SPIBUS_WriteEnable() 'We want to remove the default block protection
                        SPIBUS_WriteRead({&H42, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}) 'WriteBlockProtection (6 bytes for 26VF016, 10 for 26VF032)
                        RaiseEvent PrintConsole("SQI mode enabled for Microchip / SST device.")
                    Else
                        RaiseEvent PrintConsole("Failed to enter SQI mode, please make sure SIO2 and SIO3 are connected")
                        RaiseEvent PrintConsole("This is required for erasing/programming of the memory device")
                        'MyFlashDevice.readonly = true
                        WaitUntilReady()
                        SPIBUS_WriteRead({&HFF}) 'Back to SPI mode
                        SPIBUS_Setup(SPI_SPEED.MHZ_8)
                    End If
                End If
            End If
        End Sub

        Private Sub LoadDeviceConfigurations()
            If MyFlashDevice.VENDOR_SPECIFIC = VENDOR_FEATURE.NotSupported Then 'We don't want to do this for vendor enabled devices
                WriteStatusRegister({0})
            End If
            If (MyFlashDevice.STACKED_DIES > 1) Then 'Multi-die support
                For i = 0 To MyFlashDevice.STACKED_DIES - 1
                    SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.DIESEL, i}) : WaitUntilReady() 'We need to make sure DIE 0 is selected
                    If MyFlashDevice.SEND_4BYTE Then SPIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.EN4B) 'Set options for each DIE
                    SPIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.ULBPR) '0x98 (global block unprotect)
                Next
                SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.DIESEL, 0}) : WaitUntilReady() 'We need to make sure DIE 0 is selected
                Me.DIE_SELECTED = 0
            Else
                If MyFlashDevice.SEND_4BYTE Then SPIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.EN4B) '0xB7
                SPIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.ULBPR) '0x98 (global block unprotect)
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
            If SPIBUS_WriteRead({SPI_Command_DEF.RDID}, out_buffer) = 6 Then 'READ CHIP ID (most common) - Default 0x9F
                out_id.MANU = out_buffer(0)
                out_id.RDID = Utilities.Bytes.ToUInteger({out_buffer(1), out_buffer(2), out_buffer(3), out_buffer(4)})
            End If
            ReDim out_buffer(1) 'Erase the buffer
            If SPIBUS_WriteRead({SPI_Command_DEF.REMS, 0, 0, 0}, out_buffer) = 6 Then 'Read Electronic Manufacturer Signature 
                'If out_id.MANU = 0 Or out_id.MANU = 255 Then out_id.MANU = out_buffer(0)
                'If out_id.RDID = 0 Or out_id.RDID = 255 Then out_id.RDID = out_buffer(1)
                out_id.REMS = (CUShort(out_buffer(0)) << 8) Or out_buffer(1)
            End If
            ReDim out_buffer(0) 'Erase the buffer
            If SPIBUS_WriteRead({SPI_Command_DEF.RES, 0, 0, 0}, out_buffer) = 5 Then 'Read Electronic Signature (PMC / ST M25P10)
                out_id.RES = out_buffer(0)
            End If
            Return out_id
        End Function

#Region "Selected device properties"

        Friend ReadOnly Property DeviceName() As String Implements MemoryDeviceUSB.DeviceName
            Get
                Select Case MyFlashStatus
                    Case ConnectionStatus.Supported
                        If OperationMode = AvrMode.SPI Then
                            Return MyFlashDevice.NAME
                        Else
                            Return EXT_IF.MyFlashDevice.NAME
                        End If
                    Case ConnectionStatus.NotSupported
                        If OperationMode = AvrMode.SPI Then
                            Return Hex(MyFlashDevice.MFG_CODE).PadLeft(2, CChar("0")) & " " & Hex(MyFlashDevice.ID1).PadLeft(4, CChar("0"))
                        Else
                            Return "Unable to detect"
                        End If
                    Case Else
                        Return "No Flash Detacted"
                End Select
            End Get
        End Property

        Friend ReadOnly Property DeviceSize As UInt32 Implements MemoryDeviceUSB.DeviceSize
            Get
                If Not MyFlashStatus = ConnectionStatus.Supported Then Return 0
                If OperationMode = AvrMode.SPI Then
                    Return MyFlashDevice.FLASH_SIZE
                ElseIf OperationMode = AvrMode.EXPIO Then
                    Return EXT_IF.MyFlashDevice.FLASH_SIZE
                Else
                    Return 0
                End If
            End Get
        End Property

        Friend ReadOnly Property SectorSize(ByVal sector As UInt32, ByVal area As FlashArea) As UInt32 Implements MemoryDeviceUSB.SectorSize
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

        Friend Sub LEDOn()
            Try
                If FCUSB Is Nothing Then Exit Sub
                Dim ret As Integer = 0
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_LEDON, 0, 0, 0)
                FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Catch ex As Exception
            End Try
        End Sub

        Friend Sub LEDOff()
            Try
                If FCUSB Is Nothing Then Exit Sub
                Dim ret As Integer = 0
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_LEDOFF, 0, 0, 0)
                FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Catch ex As Exception
            End Try
        End Sub

        Friend Sub LEDBlink()
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
                Dim buff(3) As Byte
                Dim data_out(3) As Byte
                If FCUSB.ControlTransfer(usbSetupPacket, buff, 4, ret) Then
                    HWBOARD = buff(0)
                    data_out(3) = buff(3)
                    data_out(2) = buff(2)
                    data_out(1) = Asc(".")
                    data_out(0) = buff(1)
                End If
                Dim fwstr As String = Utilities.Bytes.ToChrString(data_out)
                If fwstr.StartsWith("0") Then fwstr = Mid(fwstr, 2)
                Return fwstr
            Catch ex As Exception
                Return ""
            End Try
        End Function

        Friend Function IsConnected() As Boolean
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
        Friend Function Connect() As Boolean
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
            Return EchoTest()
        End Function
        'If we are already connected, this will disconnect
        Friend Function Disconnect() As Boolean
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

        Public Function SPIBUS_WriteEnable() As Boolean
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
                If MyFlashDevice.SEND_EWSR Then
                    SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.EWSR}, Nothing) 'Send the command that we are going to enable-write to register
                    Threading.Thread.Sleep(20) 'Wait a brief moment
                End If
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
        Private Function WriteData_Page(ByVal flash_offset As UInt32, ByVal DataOut() As Byte) As Boolean
            Try
                Dim setup() As Byte = GetWriteSetupPacket(flash_offset, DataOut.Length, MyFlashDevice.OP_COMMANDS.PROG)
                Dim ret As Integer = 0
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_WRITEBULK, CUShort(0), CUShort(0), CShort(setup.Length))
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, setup, setup.Length, ret)
                If Not res Then Return False
                Dim writer As UsbEndpointWriter = FCUSB.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
                Dim ec As ErrorCode = writer.Write(DataOut, 0, DataOut.Length, 5000000, ret)
                If (ec = ErrorCode.None) Then Return True
                Return False
            Catch ex As Exception
            End Try
            Return False
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
                Dim setup_packet() As Byte = GetArrayWithCmdAndAddr(MyFlashDevice.OP_COMMANDS.AAI_WORD, offset)
                ReDim Preserve setup_packet(setup_packet.Length + 1) 'We are adding 2 bytes
                setup_packet(setup_packet.Length - 2) = data_out(0)
                setup_packet(setup_packet.Length - 1) = data_out(1)
                SPIBUS_WriteEnable()
                SPIBUS_WriteRead(setup_packet, Nothing)
                Dim op_cmd As UInt16 = (CUInt(MyFlashDevice.OP_COMMANDS.AAI_WORD) << 8) Or CUInt((MyFlashDevice.OP_COMMANDS.RDSR))
                Dim ByteCount As UInt16 = (data_out.Length - 2) 'We write 2 in setup packet
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_WRITEBULK_AAIWORD, op_cmd, ByteCount, CShort(0))
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, Nothing)
                If Not res Then Return False
                Dim writer As UsbEndpointWriter = FCUSB.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
                Dim ec As ErrorCode = writer.Write(data_out, 2, data_out.Length - 2, 2500, Nothing)
                If Not ec = ErrorCode.None Then Return False
                SPIBUS_WriteDisable()
                WaitUntilReady()
                offset += data_out.Length 'Increase the flash address offset
                BytePointer += data_out.Length 'Increase the buffer pointer
            Loop
            Return True 'Write successful
        End Function
        'Writes the data one byte at a time using AAI
        Private Function WriteData_AAI_Byte(ByVal offset As UInteger, ByVal data_in() As Byte) As Boolean
            Dim BulkSize As UInt32 = 1024
            Dim BytesRemaining As UInt32 = data_in.Length
            Dim BytePointer As UInt32 = 0
            Do Until BytesRemaining = 0
                Dim data_out() As Byte
                If (BytesRemaining > BulkSize) Then ReDim data_out(BulkSize - 1) Else ReDim data_out(BytesRemaining - 1)
                Array.Copy(data_in, BytePointer, data_out, 0, data_out.Length)
                BytesRemaining -= data_out.Length
                Dim setup_packet() As Byte = GetArrayWithCmdAndAddr(MyFlashDevice.OP_COMMANDS.AAI_BYTE, offset)
                ReDim Preserve setup_packet(setup_packet.Length) 'We are adding 1 byte
                setup_packet(setup_packet.Length - 1) = data_out(0)
                SPIBUS_WriteEnable()
                SPIBUS_WriteRead(setup_packet, Nothing)
                Dim op_cmd As UInt16 = (CUInt(MyFlashDevice.OP_COMMANDS.AAI_BYTE) << 8) Or CUInt((MyFlashDevice.OP_COMMANDS.RDSR))
                Dim ByteCount As UInt16 = (data_out.Length - 1) 'We write 1 byte in setup packet
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_WRITEBULK_AAIBYTE, op_cmd, ByteCount, CShort(0))
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, Nothing)
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
        'Uses an internal sram buffer to transfer data from the board to the flash (used by Atmel AT45DBxxx)
        Private Function WriteData_AT45(ByVal offset As UInteger, ByVal total_data() As Byte) As Boolean
            Dim PageSize As UInt32 = MyFlashDevice.PAGE_SIZE
            If MyFlashDevice.EXTENDED_MODE Then PageSize = MyFlashDevice.PAGE_SIZE_EXTENDED
            Dim AddrOffset As Integer = Math.Ceiling(Math.Log(PageSize, 2)) 'Number of bits the address is offset
            Dim BytesLeft As Integer = total_data.Length
            Do Until BytesLeft = 0
                Dim BytesToWrite As Integer = BytesLeft
                If BytesToWrite > PageSize Then BytesToWrite = PageSize
                Dim DataToBuffer(BytesToWrite + 3) As Byte
                DataToBuffer(0) = MyFlashDevice.OP_COMMANDS.WRTB '0x84
                Dim src_ind As Integer = total_data.Length - BytesLeft
                Array.Copy(total_data, src_ind, DataToBuffer, 4, BytesToWrite)
                Dim count_upper As UShort = (DataToBuffer.Length And &HFFFF0000) >> 16
                Dim count_lower As UShort = (DataToBuffer.Length And &HFFFF)
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_WRITEBULK_AT45, count_upper, count_lower, CShort(0))
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, Nothing) 'Setup command for buffer write
                Dim writer As UsbEndpointWriter = FCUSB.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
                Dim ec As ErrorCode = writer.Write(DataToBuffer, 0, DataToBuffer.Length, 2500, Nothing)
                If Not ec = ErrorCode.None Then Return False
                WaitUntilReady()
                Dim PageAddr As Integer = Math.Floor(offset / PageSize)
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
            Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_WRITEBULK_AT45, count_upper, count_lower, CShort(0))
            Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, Nothing) 'Setup command for buffer write
            Dim writer As UsbEndpointWriter = FCUSB.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
            Dim ec As ErrorCode = writer.Write(dataout, 0, dataout.Length, 2500, Nothing)
            WaitUntilReady()
            Return True
        End Function
        'Designed for SPI EEPROMS where each page needs to wait until ready
        Private Function WriteData_SPI_EEPROM(ByVal offset As UInt32, ByVal data_to_write() As Byte) As Boolean
            Dim PageSize As UInt32 = MyFlashDevice.PAGE_SIZE
            Dim DataToWrite As Integer = data_to_write.Length
            For i As Integer = 0 To Math.Ceiling(DataToWrite / PageSize) - 1
                Dim BufferSize As Integer = DataToWrite
                If (BufferSize > PageSize) Then BufferSize = PageSize
                Dim data(BufferSize - 1) As Byte
                Array.Copy(data_to_write, (i * PageSize), data, 0, data.Length)
                Dim addr_size As Integer = (MyFlashDevice.ADDRESSBITS / 8)
                Dim packet(data.Length + addr_size) As Byte 'OPCMD,ADDR,DATA
                packet(0) = MyFlashDevice.OP_COMMANDS.PROG 'First byte is the write command
                If (addr_size = 1) Then
                    Dim addr8 As Byte
                    If (offset > 255) Then
                        packet(0) = CByte(MyFlashDevice.OP_COMMANDS.PROG Or 8) 'Enables 4th bit
                        addr8 = CByte(offset And 255) 'Lower 8 bits only
                    Else
                        packet(0) = CByte(MyFlashDevice.OP_COMMANDS.PROG And &HF7) 'Disables 4th bit
                        addr8 = CByte(offset)
                    End If
                    packet(1) = addr8
                    Array.Copy(data, 0, packet, 2, data.Length)
                ElseIf (addr_size = 2) Then
                    packet(1) = CByte((offset >> 8) And 255)
                    packet(2) = CByte(offset And 255)
                    Array.Copy(data, 0, packet, 3, data.Length)
                ElseIf (addr_size = 3) Then
                    packet(1) = CByte((offset >> 16) And 255)
                    packet(2) = CByte((offset >> 8) And 255)
                    packet(3) = CByte(offset And 255)
                    Array.Copy(data, 0, packet, 4, data.Length)
                End If
                SPIBUS_WriteEnable()
                SPIBUS_SlaveSelect_Enable()
                SPIBUS_WriteData(packet)
                SPIBUS_SlaveSelect_Disable()
                Utilities.Sleep(10)
                WaitUntilReady()
                offset += data.Length
                DataToWrite -= data.Length
            Next
            Return True
        End Function

#End Region

        Friend Function ReadData(ByVal flash_offset As UInt32, ByVal data_count As UInt32, ByVal area As FlashArea) As Byte() Implements MemoryDeviceUSB.ReadData
            Dim data_to_read(data_count - 1) As Byte
            Dim bytes_left As UInt32 = data_count
            Dim buffer_size As UInt32 = 0
            Dim array_ptr As UInt32 = 0
            If (OperationMode = AvrMode.SPI) Then
                If Me.MyFlashDevice.ProgramMode = FlashMemory.SPI_ProgramMode.Atmel45Series Then
                    Dim PageSize As UInt32 = MyFlashDevice.PAGE_SIZE
                    If MyFlashDevice.EXTENDED_MODE Then PageSize = MyFlashDevice.PAGE_SIZE_EXTENDED
                    '528 mode: (13-bits page index) (10 bits is page offset)
                    '512 mode: (13-bits page index) (9 bits is page offset)
                    Dim AddrOffset As Integer = Math.Ceiling(Math.Log(PageSize, 2)) 'Number of bits the address is offset
                    Dim PageAddr As Integer = Math.Floor(flash_offset / PageSize)
                    Dim PageOffset As Integer = flash_offset - (PageAddr * PageSize)
                    Dim addr_bytes() As Byte = Utilities.Bytes.FromUInt24((PageAddr << AddrOffset) + PageOffset, False)
                    'Write OPCMD, 3 byte address, and 4 dummy bytes
                    Dim at45_addr As UInt32 = (PageAddr << AddrOffset) + PageOffset
                    Dim dymmy_cycles As Byte = 4 * 8 '(4 extra bytes)
                    Dim setup_read() As Byte = GetReadSetupPacket(MyFlashDevice.OP_COMMANDS.READ, at45_addr, data_to_read.Length, dymmy_cycles)
                    SPIBUS_ReadFlash(setup_read, data_to_read)
                ElseIf Me.MyFlashDevice.ProgramMode = FlashMemory.SPI_ProgramMode.SPI_EEPROM Then
                    If MyFlashDevice.ADDRESSBITS = 8 Then 'Used on ST M95010 - M95040 (8bit) and ATMEL devices (AT25010A - AT25040A)
                        Dim read_cmd As Byte = MyFlashDevice.OP_COMMANDS.READ
                        If (flash_offset > 255) Then read_cmd = CByte(read_cmd Or 8) 'Used on M95040 / AT25040A
                        Dim setup_read() As Byte = GetReadSetupPacket(read_cmd, CUInt(flash_offset And 255), data_to_read.Length)
                        SPIBUS_ReadFlash(setup_read, data_to_read)
                    Else
                        Dim setup_read() As Byte = GetReadSetupPacket(MyFlashDevice.OP_COMMANDS.READ, flash_offset, data_to_read.Length)
                        SPIBUS_ReadFlash(setup_read, data_to_read) 'This is what sends the actual USB hardware signals
                    End If
                ElseIf (Not Multi_IO = SPI_IO_MODE.SPI) Then
                    Dim dummy_cycles As Byte = MyFlashDevice.DUMMY_CLOCK_CYCLES + 2  'We need 2 extra?
                    If (MyFlashDevice.STACKED_DIES > 1) Then
                        Do Until bytes_left = 0
                            Dim die_address As UInt32 = GetAddressForMultiDie(flash_offset, bytes_left, buffer_size)
                            Dim die_data(buffer_size - 1) As Byte
                            SQIBUS_ReadFlash(GetReadSetupPacket(MyFlashDevice.OP_COMMANDS.FAST_READ, die_address, die_data.Length, dummy_cycles), die_data)
                            Array.Copy(die_data, 0, data_to_read, array_ptr, die_data.Length) : array_ptr += buffer_size
                        Loop
                    Else
                        Dim setup_read() As Byte = GetReadSetupPacket(MyFlashDevice.OP_COMMANDS.FAST_READ, flash_offset, data_to_read.Length, dummy_cycles)
                        SQIBUS_ReadFlash(setup_read, data_to_read)
                    End If
                Else 'Normal SPI READ
                    If (MyFlashDevice.STACKED_DIES > 1) Then
                        Do Until bytes_left = 0
                            Dim die_address As UInt32 = GetAddressForMultiDie(flash_offset, bytes_left, buffer_size)
                            Dim die_data(buffer_size - 1) As Byte
                            SPIBUS_ReadFlash(GetReadSetupPacket(MyFlashDevice.OP_COMMANDS.READ, die_address, die_data.Length), die_data)
                            Array.Copy(die_data, 0, data_to_read, array_ptr, die_data.Length) : array_ptr += buffer_size
                        Loop
                    Else
                        Dim setup_read() As Byte = GetReadSetupPacket(MyFlashDevice.OP_COMMANDS.READ, flash_offset, data_to_read.Length)
                        SPIBUS_ReadFlash(setup_read, data_to_read) 'This is what sends the actual USB hardware signals
                    End If
                End If
            End If
            Return data_to_read
        End Function

        Friend Function WriteData(ByVal flash_offset As UInt32, ByVal data_to_write() As Byte, ByVal area As FlashArea) As Boolean Implements MemoryDeviceUSB.WriteData
            Dim bytes_left As UInt32 = data_to_write.Length
            Dim buffer_size As UInt32 = 0
            Dim array_ptr As UInt32 = 0
            If (Multi_IO = SPI_IO_MODE.QUAD) Or (Multi_IO = SPI_IO_MODE.DUAL) Then
                If (MyFlashDevice.STACKED_DIES > 1) Then 'Multi-die support
                    Dim write_result As Boolean
                    Do Until bytes_left = 0
                        Dim die_address As UInt32 = GetAddressForMultiDie(flash_offset, bytes_left, buffer_size)
                        Dim die_data(buffer_size - 1) As Byte
                        Array.Copy(data_to_write, array_ptr, die_data, 0, die_data.Length) : array_ptr += buffer_size
                        Dim setup_packet() As Byte = GetWriteSetupPacket(die_address, die_data.Length, MyFlashDevice.OP_COMMANDS.PROG)
                        write_result = SQIBUS_WriteFlash(setup_packet, die_data)
                        If Not write_result Then Return False
                    Loop
                    Return write_result
                Else
                    Dim setup_packet() As Byte = GetWriteSetupPacket(flash_offset, data_to_write.Length, MyFlashDevice.OP_COMMANDS.PROG)
                    Return SQIBUS_WriteFlash(setup_packet, data_to_write)
                End If
            ElseIf (OperationMode = AvrMode.SPI) Then
                Select Case MyFlashDevice.ProgramMode
                    Case SPI_ProgramMode.PageMode
                        If (MyFlashDevice.STACKED_DIES > 1) Then 'Multi-die support
                            Dim write_result As Boolean
                            Do Until bytes_left = 0
                                Dim die_address As UInt32 = GetAddressForMultiDie(flash_offset, bytes_left, buffer_size)
                                Dim die_data(buffer_size - 1) As Byte
                                Array.Copy(data_to_write, array_ptr, die_data, 0, die_data.Length) : array_ptr += buffer_size
                                write_result = WriteData_Page(die_address, die_data)
                                If Not write_result Then Return False
                            Loop
                            Return write_result
                        Else
                            Return WriteData_Page(flash_offset, data_to_write)
                        End If
                    Case SPI_ProgramMode.SPI_EEPROM 'Used on most ST M95080 and above
                        Return WriteData_SPI_EEPROM(flash_offset, data_to_write)
                    Case SPI_ProgramMode.AAI_Byte
                        Return WriteData_AAI_Byte(flash_offset, data_to_write)
                    Case SPI_ProgramMode.AAI_Word
                        Return WriteData_AAI_Word(flash_offset, data_to_write)
                    Case SPI_ProgramMode.Atmel45Series
                        Return WriteData_AT45(flash_offset, data_to_write)
                    Case SPI_ProgramMode.Nordic
                        Return WriteData_Nordic(flash_offset, data_to_write)
                End Select
                'If MyFlashDevice.SEND_RDFS Then ReadFlagStatusRegister()
            End If
            Return False
        End Function
        'Erases the entire chip (sets all pages/sectors to 0xFF)
        Friend Function ChipErase() As Boolean Implements MemoryDeviceUSB.ChipErase
            If MyFlashDevice.ProgramMode = FlashMemory.SPI_ProgramMode.Atmel45Series Then
                If Not SPIBUS_WriteRead({&HC7, &H94, &H80, &H9A}, Nothing) = 4 Then Return False
            ElseIf MyFlashDevice.ProgramMode = SPI_ProgramMode.SPI_EEPROM Then
                Dim data(MyFlashDevice.FLASH_SIZE - 1) As Byte
                For i = 0 To data.Length - 1
                    data(i) = 255
                Next
                WriteData(0, data, FlashArea.Main)
            ElseIf MyFlashDevice.ProgramMode = SPI_ProgramMode.Nordic Then 'We do NOT want to use bulk erase, since that erases NV data and IP page!
                Dim nord_timer As New Stopwatch : nord_timer.Start()
                RaiseEvent PrintConsole(String.Format(RM.GetString("fcusb_spi_erasingbulk"), Format(Me.DeviceSize, "#,###")))
                Dim TotalPages As Integer = MyFlashDevice.FLASH_SIZE / MyFlashDevice.PAGE_SIZE
                For i = 0 To TotalPages - 1
                    SPIBUS_WriteEnable() : Utilities.Sleep(50)
                    SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.SE, i}, Nothing)
                    WaitUntilReady()
                Next
                RaiseEvent PrintConsole(String.Format(RM.GetString("fcusb_spi_erasecomplete"), Format(nord_timer.ElapsedMilliseconds / 1000, "#.##")))
                Return True
            Else
                RaiseEvent PrintConsole(String.Format(RM.GetString("fcusb_spi_erasingbulk"), Format(Me.DeviceSize, "#,###")))
                Dim erase_timer As New Stopwatch : erase_timer.Start()
                Select Case MyFlashDevice.CHIP_ERASE
                    Case EraseMethod.Standard
                        SPIBUS_WriteEnable()
                        SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.BE}, Nothing) '&HC7
                        If MyFlashDevice.SEND_RDFS Then ReadFlagStatusRegister()
                        WaitUntilReady()
                    Case EraseMethod.BySector
                        Dim SectorCount As UInt32 = MyFlashDevice.SECTOR_COUNT
                        SetProgress(0)
                        For i As UInt32 = 0 To SectorCount - 1
                            If (Not Sector_Erase(i, FlashArea.NotSpecified)) Then
                                SetProgress(0) : Return False 'Error erasing sector
                            Else
                                Dim progress As Single = CSng((i / SectorCount) * 100)
                                SetProgress(Math.Floor(progress))
                            End If
                        Next
                        SetProgress(0) 'Device successfully erased
                    Case EraseMethod.DieErase
                        EraseDie()
                    Case EraseMethod.Micron
                        Dim internal_timer As New Stopwatch
                        internal_timer.Start()
                        SPIBUS_WriteEnable() 'Try Chip Erase first
                        SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.BE}, Nothing)
                        If MyFlashDevice.SEND_RDFS Then ReadFlagStatusRegister()
                        WaitUntilReady()
                        internal_timer.Stop()
                        If (internal_timer.ElapsedMilliseconds < 1000) Then 'Command not supported, use DIE ERASE instead
                            EraseDie()
                        End If
                End Select
                RaiseEvent PrintConsole(String.Format(RM.GetString("fcusb_spi_erasecomplete"), Format(erase_timer.ElapsedMilliseconds / 1000, "#.##")))
            End If
            Return True
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
                        'Dim counter As UInt32 = 0
                        Do
                            Dim sr() As Byte = ReadStatusRegister()
                            Status = sr(0)
                            'counter += 1
                            'If counter = 100 Then Exit Do '2.5 seconds
                            If AppIsClosing Then Exit Sub
                            If Status = 255 Then Exit Do
                            If (Status And 1) Then Utilities.Sleep(25)
                        Loop While (Status And 1)
                        If MyFlashDevice IsNot Nothing AndAlso MyFlashDevice.ProgramMode = SPI_ProgramMode.Nordic Then
                            Utilities.Sleep(50)
                        End If
                    End If
                ElseIf OperationMode = AvrMode.EXPIO Then
                    EXT_IF.WaitForReady()
                End If
            Catch ex As Exception
            End Try
        End Sub

        Public Function FindSectorBase(ByVal sector_index As UInt32, ByVal area As FlashArea) As UInt32 Implements MemoryDeviceUSB.Sector_Find
            If sector_index = 0 Then Return 0 'Addresses start at the base address 
            If OperationMode = AvrMode.SPI Then 'Uniform sectors
                Return Me.SectorSize(0, area) * sector_index
            Else
                Return 0
            End If
        End Function

        Public Function Sector_Erase(ByVal sector_index As UInt32, ByVal area As FlashArea) As Boolean Implements MemoryDeviceUSB.Sector_Erase
            If (Not MyFlashDevice.ERASE_REQUIRED) Then Return True 'Erase not needed
            Dim flash_offset As UInt32 = FindSectorBase(sector_index, area)
            If OperationMode = AvrMode.SPI Then 'Following is compatible with SPI and SQI
                If MyFlashDevice.ProgramMode = SPI_ProgramMode.Atmel45Series Then
                    Dim PageSize As UInt32 = MyFlashDevice.PAGE_SIZE
                    If MyFlashDevice.EXTENDED_MODE Then PageSize = MyFlashDevice.PAGE_SIZE_EXTENDED
                    Dim EraseSize As UInt32 = MyFlashDevice.ERASE_SIZE
                    Dim AddrOffset As Integer = Math.Ceiling(Math.Log(PageSize, 2)) 'Number of bits the address is offset
                    Dim blocknum As Integer = Math.Floor(flash_offset / EraseSize)
                    Dim addrbytes() As Byte = Utilities.Bytes.FromUInt24(blocknum << (AddrOffset + 3), False)
                    SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.SE, addrbytes(0), addrbytes(1), addrbytes(2)}, Nothing)
                ElseIf MyFlashDevice.ProgramMode = SPI_ProgramMode.Nordic Then
                    SPIBUS_WriteEnable() : Utilities.Sleep(50)
                    Dim PageNum As Byte = Math.Floor(flash_offset / 512)
                    SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.SE, PageNum}, Nothing)
                Else
                    SPIBUS_WriteEnable()
                    If (MyFlashDevice.STACKED_DIES > 1) Then 'Multi-die support
                        flash_offset = GetAddressForMultiDie(flash_offset, 0, 0)
                    End If
                    Dim DataToWrite() As Byte = GetArrayWithCmdAndAddr(MyFlashDevice.OP_COMMANDS.SE, flash_offset)
                    SPIBUS_WriteRead(DataToWrite, Nothing)
                    If MyFlashDevice.SEND_RDFS Then
                        ReadFlagStatusRegister()
                    Else
                        Utilities.Sleep(100) 'Was 200
                    End If
                End If
                WaitUntilReady()
            End If
            Return True
        End Function
        'Returns the total number of sectors (actually number of flash pages)
        Public Function Sectors_Count() As UInt32 Implements MemoryDeviceUSB.Sectors_Count
            If MyFlashDevice IsNot Nothing Then
                Return MyFlashDevice.SECTOR_COUNT
            Else
                Return 0
            End If
        End Function
        'Writes data to a given sector and also swaps bytes (endian for words/halfwords)
        Public Function WriteSector(ByVal Sector As UInt32, ByVal data() As Byte, ByVal area As FlashArea) As Boolean Implements MemoryDeviceUSB.Sector_Write
            Dim Addr32 As UInteger = Me.FindSectorBase(Sector, FlashArea.NotSpecified)
            Return WriteData(Addr32, data, area)
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

        Private Sub EraseDie()
            Dim die_size As UInt32 = &H2000000
            Dim die_count As UInt32 = MyFlashDevice.FLASH_SIZE / die_size
            For x As UInt32 = 1 To die_count
                RaiseEvent PrintConsole("Erasing flash die index: " & x.ToString & " (" & Format(die_size, "#,###") & " bytes)")
                Dim die_addr() As Byte = Utilities.Bytes.FromUInt32((x - 1) * die_size, False)
                SPIBUS_WriteEnable()
                SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.DE, die_addr(0), die_addr(1), die_addr(1), die_addr(1)}, Nothing) '&HC4
                Utilities.Sleep(1000)
                If MyFlashDevice.SEND_RDFS Then ReadFlagStatusRegister()
                WaitUntilReady()
            Next
        End Sub

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
        Public Function SPIBUS_WriteRead(ByVal WriteBuffer() As Byte, Optional ByRef ReadBuffer() As Byte = Nothing) As UInt32
            If WriteBuffer Is Nothing And ReadBuffer Is Nothing Then Return 0
            If WriteBuffer IsNot Nothing AndAlso WriteBuffer.Length > 64 Then Return 0
            If ReadBuffer IsNot Nothing AndAlso ReadBuffer.Length > 64 Then Return 0
            Dim TotalBytesTransfered As UInt32 = 0
            If (WriteBuffer IsNot Nothing) Then
                Dim BytesWritten As Integer = 0
                Dim Result As Boolean = False
                If (Multi_IO = SPI_IO_MODE.QUAD) Or (Multi_IO = SPI_IO_MODE.DUAL) Then
                    Result = SQIBUS_WriteData(WriteBuffer, BytesWritten)
                ElseIf Multi_IO = SPI_IO_MODE.SPI Then
                    Result = SPIBUS_WriteData(WriteBuffer, BytesWritten)
                End If
                If Result Then TotalBytesTransfered += BytesWritten
            Else
                SPIBUS_SlaveSelect_Enable()
            End If
            If (ReadBuffer IsNot Nothing) Then
                Dim BytesRead As Integer = 0
                Dim Result As Boolean = False
                If (Multi_IO = SPI_IO_MODE.QUAD) Or (Multi_IO = SPI_IO_MODE.DUAL) Then
                    Result = SQIBUS_ReadData(ReadBuffer, BytesRead)
                ElseIf Multi_IO = SPI_IO_MODE.SPI Then
                    Result = SPIBUS_ReadData(ReadBuffer, BytesRead)
                End If
                If Result Then TotalBytesTransfered += BytesRead
            ElseIf WriteBuffer IsNot Nothing Then
                SPIBUS_SlaveSelect_Disable()
            End If
            Return TotalBytesTransfered
        End Function

        Private Sub ReadFlagStatusRegister()
            Utilities.Sleep(10)
            Dim flag() As Byte = {0}
            Do
                SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.RDFR}, flag)
            Loop Until ((flag(0) >> 7) And 1)
        End Sub

        Friend Function SPIBUS_Setup(select_speed As SPI_SPEED) As Boolean
            Try
                Dim clock As SPI_CLOCK = SPI_CLOCK.SPI_CLOCK_FOSC_2 'MCLK/2 (8MHz)
                Select Case select_speed
                    Case SPI_SPEED.MHZ_8
                        WriteConsole("SPI clock set to: 8 MHz")
                        clock = SPI_CLOCK.SPI_CLOCK_FOSC_2
                    Case SPI_SPEED.MHZ_4
                        WriteConsole("SPI clock set to: 4 MHz")
                        clock = SPI_CLOCK.SPI_CLOCK_FOSC_4
                    Case SPI_SPEED.MHZ_2
                        WriteConsole("SPI clock set to: 2 MHz")
                        clock = SPI_CLOCK.SPI_CLOCK_FOSC_8
                    Case SPI_SPEED.MHZ_1
                        WriteConsole("SPI clock set to: 1 MHz")
                        clock = SPI_CLOCK.SPI_CLOCK_FOSC_16
                End Select
                Dim mode As SPI_FOSC_MODE = SPI_FOSC_MODE.SPI_MODE_0
                Dim order As SPI_ORDER = SPI_ORDER.SPI_ORDER_MSB_FIRST
                Dim spiConf As Short = CShort(CByte(clock) Or CByte(mode) Or CByte(order))
                Dim usb_flag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor)
                Dim usbSetupPacket As New UsbSetupPacket(usb_flag, SPIREQ_SETCFG, spiConf, 0, 0)
                Dim ret As Integer
                If FCUSB.ControlTransfer(usbSetupPacket, Nothing, 0, ret) Then
                    UpdateClockDevider(clock)
                    Multi_IO = SPI_IO_MODE.SPI
                    Return True
                End If
                Return False
            Catch ex As Exception
                Return False
            End Try
        End Function
        'Writes SPI data (64-bytes at a time)
        Private Function SPIBUS_WriteData(ByVal Data_Out() As Byte, Optional ByRef BytesWritten As Integer = 0) As Boolean
            Try
                Dim Count As UInt32 = Data_Out.Length
                Do While Count > 0
                    Dim packet_size As Integer = Math.Min(64, Count)
                    Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                    Dim usbPacket2 As New UsbSetupPacket(usbflag, SPIREQ_WRITEDATA, 0, 0, CShort(packet_size))
                    Dim write_result As Integer = 0
                    Dim packet_out(packet_size - 1) As Byte
                    Array.Copy(Data_Out, BytesWritten, packet_out, 0, packet_out.Length)
                    Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, packet_out, packet_out.Length, write_result)
                    If Not res Then Return False
                    BytesWritten += write_result
                    Count -= write_result
                Loop
                Return True
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

        Private Function SPIBUS_ReadFlash(ByVal setup_packet() As Byte, ByRef data_out() As Byte) As Boolean
            Try
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbpacket1 As New UsbSetupPacket(usbflag, SPIREQ_READFLASH, CUShort(0), CUShort(0), CShort(setup_packet.Length))
                FCUSB.ControlTransfer(usbpacket1, setup_packet, setup_packet.Length, Nothing)
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
                res = res
            Catch ex As Exception
            End Try
        End Sub


#Region "QUAD MODE"

        'Enables FCUSB to setup into SQI mode
        Private Function SQIBUS_Setup() As Boolean
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, SQIREQ_SETUP, CShort(0), 0, 0)
            Dim Result As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Utilities.Sleep(50) 'Allow time for device to change IO
            Multi_IO = SPI_IO_MODE.QUAD
            Return Result
        End Function

        Private Function SQIBUS_ReadData(ByRef Data_In() As Byte, ByRef BytesRead As Integer) As Boolean
            Try
                Dim io_mode As UInt16 = Multi_IO
                If MyFlashDevice IsNot Nothing Then
                    If MyFlashDevice.QUAD = SPI_QUADMODE.spisetup_quadio Then
                        io_mode = SPI_IO_MODE.SPI
                    End If
                End If
                If Data_In Is Nothing OrElse Data_In.Length = 0 Then Return False
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SQIREQ_READDATA, io_mode, CUShort(0), CShort(Data_In.Length))
                Return FCUSB.ControlTransfer(usbPacket2, Data_In, Data_In.Length, BytesRead)
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Function SQIBUS_WriteData(ByVal Data_Out() As Byte, ByRef BytesWritten As Integer) As Boolean
            Try
                Dim io_mode As UInt16 = Multi_IO
                If MyFlashDevice IsNot Nothing Then
                    If MyFlashDevice.QUAD = SPI_QUADMODE.spisetup_quadio Then
                        io_mode = SPI_IO_MODE.SPI
                    End If
                End If
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SQIREQ_WRITEDATA, io_mode, CUShort(0), CShort(Data_Out.Length))
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, Data_Out, Data_Out.Length, BytesWritten)
                Return res
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Function SQIBUS_ReadFlash(ByVal setup_packet() As Byte, ByRef data_out() As Byte) As Boolean
            Try
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbpacket1 As New UsbSetupPacket(usbflag, SQIREQ_READFLASH, CUShort(0), CUShort(0), CShort(setup_packet.Length))
                FCUSB.ControlTransfer(usbpacket1, setup_packet, setup_packet.Length, Nothing)
                Dim reader As UsbEndpointReader = FCUSB.OpenEndpointReader(ReadEndpointID.Ep01, data_out.Length, EndpointType.Bulk)
                reader.Read(data_out, 0, CInt(data_out.Length), 5000, Nothing)
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Function SQIBUS_WriteFlash(ByVal setup_packet() As Byte, ByVal Data() As Byte) As Boolean
            Try
                Dim ret As Integer = 0
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, SQIREQ_WRITEFLASH, CUShort(0), CUShort(0), CShort(setup_packet.Length))
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, setup_packet, setup_packet.Length, ret)
                If Not res Then Return False
                Dim writer As UsbEndpointWriter = FCUSB.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
                Dim ec As ErrorCode = writer.Write(Data, 0, Data.Length, -1, ret)
                If (ec = ErrorCode.None) Then
                    Utilities.Sleep(2)
                    Return True
                Else
                    Return False
                End If
            Catch ex As Exception
                Return False
            End Try
        End Function

#End Region

        Private Function GetWriteSetupPacket(ByVal offset As UInt32, ByVal count As UInt32, ByVal program_cmd As Byte) As Byte()
            Dim PageSize As UInt32 = MyFlashDevice.PAGE_SIZE
            Dim setup_data(15) As Byte '16 bytes
            setup_data(0) = program_cmd
            setup_data(1) = MyFlashDevice.OP_COMMANDS.WREN
            setup_data(2) = MyFlashDevice.OP_COMMANDS.RDSR
            setup_data(3) = MyFlashDevice.OP_COMMANDS.RDFR
            setup_data(4) = CByte(MyFlashDevice.AddressBytes) 'Number of bytes to write
            setup_data(5) = CByte((PageSize And &HFF00) >> 8)
            setup_data(6) = CByte(PageSize And &HFF)
            setup_data(7) = CByte((offset And &HFF000000) >> 24)
            setup_data(8) = CByte((offset And &HFF0000) >> 16)
            setup_data(9) = CByte((offset And &HFF00) >> 8)
            setup_data(10) = CByte(offset And &HFF)
            setup_data(11) = CByte((count And &HFF0000) >> 16)
            setup_data(12) = CByte((count And &HFF00) >> 8)
            setup_data(13) = CByte(count And &HFF)
            setup_data(14) = 0 'QUAD OP MODE
            setup_data(15) = 0 'SQI IO MODE
            If (Not MyFlashDevice.SEND_RDFS) Then setup_data(3) = 0 'Only use flag-reg if required
            setup_data(14) = MyFlashDevice.QUAD 'OP COMMAND (FOR SQI ONLY)
            setup_data(15) = Me.Multi_IO 'SPI/DPI/QPI
            Return setup_data
        End Function

        Private Function GetReadSetupPacket(ByVal read_cmd As Byte, ByVal offset As UInt32, ByVal count As UInt32, Optional dummy As Byte = 0) As Byte()
            Dim setup_data(11) As Byte '12 bytes
            setup_data(0) = read_cmd 'READ/FAST_READ/ETC.
            setup_data(1) = CByte(MyFlashDevice.AddressBytes)
            setup_data(2) = CByte((offset And &HFF000000) >> 24)
            setup_data(3) = CByte((offset And &HFF0000) >> 16)
            setup_data(4) = CByte((offset And &HFF00) >> 8)
            setup_data(5) = CByte(offset And &HFF)
            setup_data(6) = CByte((count And &HFF0000) >> 16)
            setup_data(7) = CByte((count And &HFF00) >> 8)
            setup_data(8) = CByte(count And &HFF)
            setup_data(9) = dummy 'Number of dummy bytes
            setup_data(10) = MyFlashDevice.QUAD 'OP COMMAND (FOR SQI ONLY)
            setup_data(11) = Me.Multi_IO 'SPI/DPI/QPI
            Return setup_data
        End Function
        'Returns the die address from the flash_offset (and increases by the buffersize) and also selects the correct die
        Private Function GetAddressForMultiDie(ByRef flash_offset As UInt32, ByRef count As UInt32, ByRef buffer_size As UInt32) As UInt32
            Dim die_size As UInt32 = (MyFlashDevice.FLASH_SIZE / MyFlashDevice.STACKED_DIES)
            Dim die_id As Byte = CByte(Math.Floor(flash_offset / die_size))
            Dim die_addr As UInt32 = (flash_offset Mod die_size)
            buffer_size = Math.Min(count, ((MyFlashDevice.FLASH_SIZE / MyFlashDevice.STACKED_DIES) - die_addr))
            If (die_id <> Me.DIE_SELECTED) Then
                SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.DIESEL, die_id})
                WaitUntilReady()
                Me.DIE_SELECTED = die_id
            End If
            count -= buffer_size
            flash_offset += buffer_size
            Return die_addr
        End Function


    End Class

    Public Enum SPI_CLOCK As Byte
        SPI_CLOCK_FOSC_2 = &H80
        SPI_CLOCK_FOSC_4 = &H0
        SPI_CLOCK_FOSC_8 = &H81
        SPI_CLOCK_FOSC_16 = &H1
        SPI_CLOCK_FOSC_32 = &H82
        SPI_CLOCK_FOSC_64 = &H2 'or 0x83
        SPI_CLOCK_FOSC_128 = &H3
        SPI_UNSPECIFIED = &HFF
    End Enum

    Public Enum SPI_FOSC_MODE As Byte
        SPI_MODE_0 = &H0
        SPI_MODE_1 = &H4
        SPI_MODE_2 = &H8
        SPI_MODE_3 = &HC
        SPI_UNSPECIFIED = &HFF
    End Enum

    Public Enum SPI_ORDER As Byte
        SPI_ORDER_MSB_FIRST = &H0
        SPI_ORDER_LSB_FIRST = &H20
        SPI_UNSPECIFIED = &HFF
    End Enum

    Public Enum ConnectionStatus
        ExtIoNotConnected = 0
        NotDetected = 1
        Supported = 2
        NotSupported = 3
    End Enum

    Public Enum SPI_SPEED
        MHZ_8
        MHZ_4
        MHZ_2
        MHZ_1
    End Enum


End Namespace
