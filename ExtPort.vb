Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.SPI
Imports LibUsbDotNet
Imports LibUsbDotNet.Main

Public Class ExtPort
    Public FCUSB As UsbDevice
    Public CHIPID_MFG As Byte = 0
    Public CHIPID_PART As UInt32 = 0
    Public MyMPDevice As FlashMemory.Device  'Contains the definition for the EXT I/O device that is connected
    Public MyAdapter As AdatperType 'This is the kind of socket adapter connected and the mode it is in
    Public Property AbortOperation As Boolean = False

    Public Event PrintConsole(ByVal msg As String)
    'Public Event SetProgress(ByVal percent As Integer)
    'Public Event SetStatus(ByVal msg As String)


#Region "USB Hardware Flags"
    Public Const EXPIOREQ_INIT As Byte = &HA0
    Public Const EXPIOREQ_ADDRESS As Byte = &HA1
    Public Const EXPIOREQ_READBYTE As Byte = &HA2
    Public Const EXPIOREQ_READWORD As Byte = &HA3
    Public Const EXPIOREQ_WRITEBYTE As Byte = &HA4
    Public Const EXPIOREQ_WRITEWORD As Byte = &HA5
    Public Const EXPIOREQ_NANDREADBULK As Byte = &HA6
    Public Const EXPIOREQ_WRITEDATA As Byte = &HA7
    Public Const EXPIOREQ_READDATA As Byte = &HA8
    Public Const EXPIOREQ_RDID As Byte = &HA9
    'Public Const EXPIOREQ_WRITEDELAY As Byte = &HAA
    Public Const EXPIOREQ_CHIPERASE As Byte = &HAB
    Public Const EXPIOREQ_SECTORERASE As Byte = &HAC
    Public Const EXPIOREQ_WRITEPAGE As Byte = &HAE
    Public Const EXPIOREQ_NAND_WAIT As Byte = &HB0
    Public Const EXPIOREQ_NAND_SR As Byte = &HB1
    Public Const EXPIOREQ_NAND_WR_SA As Byte = &HB2
    Public Const EXPIOREQ_NANDWRBULK As Byte = &HB3
    Public Const EXPIOREQ_NAND_PAGEOFFSET As Byte = &HB4

    Public Const EXPIOREQ_MODE_ADDRESS As Byte = &HC0 'Sets the write address mode
    Public Const EXPIOREQ_MODE_IDENT As Byte = &HC1 'Detects the ident
    Public Const EXPIOREQ_MODE_ERSCR As Byte = &HC2 'Erases the sector
    Public Const EXPIOREQ_MODE_ERCHP As Byte = &HC3 'erases the chip
    'Public Const EXPIOREQ_MODE_READ As Byte = &HC4 'Read data (64 bytes)
    Public Const EXPIOREQ_MODE_WRITE As Byte = &HC5 'Write data (64 bytes)
    Public Const EXPIOREQ_CE_HIGH As Byte = &HC6 'Sets CHIPENABLE to HIGH
    Public Const EXPIOREQ_CE_LOW As Byte = &HC7 'Sets CHIPENABLE to LOW
    Public Const EXPIOREQ_DELAY As Byte = &HC8
    Public Const EXPIOREQ_RESET As Byte = &HC9 'Issue device reset/read mode
#End Region

    Public Enum AdatperType As Byte
        X8_Type1 = 10 'PLCC32 devices
        X8_Type2 = 11 'TSOP48 X8 devices
        X8_Type3 = 12 'DIP32 devices
        X16_Type1 = 13
        X16_Type2 = 14
        NAND = 15 'Currently only x2 mode
    End Enum

    Public Enum EXPIO_Mode
        x8 = 1
        x16 = 2
        NAND = 3
    End Enum

    Public Function Init() As ConnectionStatus
        RaiseEvent PrintConsole("Initializing EXT I/O board")
        Dim Connected As Boolean = False
        If Not Connected Then Connected = DetectFlashDevice(AdatperType.NAND) 'SLC NAND devices
        If Not Connected Then Connected = DetectFlashDevice(AdatperType.X16_Type2) 'TSOP48 / SO-44 / TSOP-56 29F devices
        If Not Connected Then Connected = DetectFlashDevice(AdatperType.X16_Type1) 'SO-44
        If Not Connected Then Connected = DetectFlashDevice(AdatperType.X8_Type3) 'Used by DIP32 or 29F X8
        If Not Connected Then Connected = DetectFlashDevice(AdatperType.X8_Type1) 'Used by PLCC-32 devices
        If Not Connected Then Connected = DetectFlashDevice(AdatperType.X8_Type2) 'Used by TSOP48 X8 devices only
        If Connected Then
            RaiseEvent PrintConsole(RM.GetString("fcusb_spi_connflash") & " (JEDEC ID: 0x" & Hex(CHIPID_MFG).PadLeft(2, "0") & Hex(CHIPID_PART).PadLeft(8, "0") & ")")
            If MyAdapter = AdatperType.NAND Then
                MyMPDevice = FlashDatabase.FindDevice(CHIPID_MFG, CHIPID_PART, MemoryType.SLC_NAND)
            Else
                MyMPDevice = FlashDatabase.FindDevice(CHIPID_MFG, CHIPID_PART, MemoryType.PARALLEL_NOR)
            End If
            If (MyMPDevice IsNot Nothing) Then
                RaiseEvent PrintConsole(String.Format(RM.GetString("fcusb_spi_flashdetected"), MyMPDevice.NAME, Format(MyMPDevice.FLASH_SIZE, "#,###")))
                RaiseEvent PrintConsole(RM.GetString("fcusb_cmos_progmode"))
                If MyMPDevice.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                    Dim NOR_FLASH As MFP_Flash = DirectCast(MyMPDevice, MFP_Flash)
                    EXPIO_SETUP_WRITEDELAY(NOR_FLASH.WRITE_DELAY_CYCLES)
                    If MyAdapter = AdatperType.X8_Type1 OrElse MyAdapter = AdatperType.X16_Type1 Then
                        EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.Type1) '0x5555=0xAA;0x2AAA=0x55;0x5555=0x80;0x5555=0xAA;0x2AAA=0x55;SA=0x30
                        EXPIO_SETUP_CHIPERASE(E_EXPIO_CHIPERASE.Type1) '0x5555=0xAA;0x2AAA=0x55;0x5555=0x80;0x5555=0xAA;0x2AAA=0x55;0x5555=0x10
                        EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type1) '0x5555=0xAA;0x2AAA=0x55;0x5555=0xA0;SA=DATA;DELAY
                    ElseIf MyAdapter = AdatperType.X8_Type2 Then '8x mode (TSOP48)
                        EXPIO_SETUP_CHIPERASE(E_EXPIO_CHIPERASE.Type2) '0xAAA=0xAA;0x555=0x55;0xAAA=0x80;0xAAA=0xAA;0x555=0x55;0xAAA=0x10
                        EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.Type2) '0xAAA=0xAA;0x555=0x55;0xAAA=0x80;0xAAA=0xAA;0x555=0x55;SA=0x30
                        EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type2) '0xAAA=0xAA;0x555=0x55;0xAAA=0xA0;SA=DATA;DELAY 
                    ElseIf MyAdapter = AdatperType.X8_Type3 Then 'HERE
                        EXPIO_SETUP_CHIPERASE(E_EXPIO_CHIPERASE.Type3) '0x555=0xAA;0x2AA=0x55;0x555=0x80;0x555=0xAA;0x2AA=0x55;0x555=0x10
                        EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.Type3) '0x555=0xAA,0x2AA=0x55,0x555=0x80,0x555=0xAA,0x2AA=0x55;SA=0x30
                        EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type3_X8) '0x555=0xAA;0x2AA=0x55;0x555=0xA0,SA=DATA;DELAY
                    ElseIf MyAdapter = AdatperType.X16_Type2 Then
                        EXPIO_SETUP_CHIPERASE(E_EXPIO_CHIPERASE.Type3) '0x555=0xAA;0x2AA=0x55;0x555=0x80;0x555=0xAA;0x2AA=0x55;0x555=0x10
                        EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.Type3) '0x555=0xAA,0x2AA=0x55,0x555=0x80,0x555=0xAA,0x2AA=0x55;SA=0x30
                        EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type3_X16) '0x555=0xAA;0x2AA=0x55;0x555=0xA0,SA=DATA;DELAY
                    End If
                    If (MyAdapter = AdatperType.X8_Type2) AndAlso (NOR_FLASH.FLASH_SIZE = Mb004) Then
                        RaiseEvent PrintConsole("PLCC-32 4MBIT device detected, enabling A18 on Pin 1 (reset)")
                        'EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.Parallel_4Mbit) 'IOEXT CE pin is used for A18 (PLCC32-P1)
                    Else
                        Select Case NOR_FLASH.WriteMode
                            Case MFP_ProgramMode.IntelSharp
                                EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.Type4) 'SA=0x50;SA=0x60;SA=0xD0(SR.7)SA=0x20;SA=0xD0(SR.7)
                                If (MyAdapter = AdatperType.X8_Type2) OrElse (MyAdapter = AdatperType.X8_Type1) Then 'We need X8 commands
                                    EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type4_X8)
                                ElseIf (MyAdapter = AdatperType.X16_Type1) OrElse (MyAdapter = AdatperType.X16_Type2) Then 'We need X16 commands
                                    EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type4_X16)
                                End If
                            Case MFP_ProgramMode.BypassMode 'Writes 64 bytes using ByPass sequence
                                If (MyAdapter = AdatperType.X8_Type2) OrElse (MyAdapter = AdatperType.X8_Type1) Then 'We need X8 commands
                                ElseIf (MyAdapter = AdatperType.X16_Type1) OrElse (MyAdapter = AdatperType.X16_Type2) Then 'We need X16 commands
                                    EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type5) '(Bypass) 0x555=0xAA;0x2AA=55;0x555=20;(0x00=0xA0;SA=DATA;...)0x00=0x90;0x00=0x00
                                End If
                            Case MFP_ProgramMode.PageMode 'Writes an entire page of data (128 bytes etc.)
                                EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type6)
                            Case MFP_ProgramMode.Buffer 'Writes to a buffer that is than auto-programmed
                                EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.Type4) 'SA=0x50;SA=0x60;SA=0xD0,SR.7,SA=0x20;SA=0xD0,SR.7
                                If (MyAdapter = AdatperType.X8_Type2) OrElse (MyAdapter = AdatperType.X8_Type1) Then 'We need X8 commands
                                ElseIf (MyAdapter = AdatperType.X16_Type1) OrElse (MyAdapter = AdatperType.X16_Type2) Then 'We need X16 commands
                                    EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type7)
                                End If
                        End Select
                    End If
                    Select Case NOR_FLASH.SectorEraseMode
                        Case MFP_SectorEraseMode.IntelSharp
                            EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.Type4) 'SA=0x50;SA=0x60;SA=0xD0,SR.7,SA=0x20;SA=0xD0,SR.7
                    End Select
                ElseIf MyMPDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
                    RaiseEvent PrintConsole("Flash page size: " & MyMPDevice.PAGE_SIZE & " bytes (" & DirectCast(MyMPDevice, NAND_Flash).PAGE_SIZE_EXTENDED & " bytes extended)")
                    Select Case DirectCast(MyMPDevice, NAND_Flash).PAGE_SIZE
                        Case 512
                            EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.NAND1)
                            EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.NAND1)
                            EXPIO_SETUP_NANDOFFSET(8)
                        Case 2048
                            EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.NAND2)
                            EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.NAND2)
                            EXPIO_SETUP_NANDOFFSET(11)
                        Case 4096
                            EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.NAND2)
                            EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.NAND2)
                            EXPIO_SETUP_NANDOFFSET(12)
                        Case 8192
                            EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.NAND3)
                            EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.NAND2)
                            EXPIO_SETUP_NANDOFFSET(13)
                    End Select
                    NAND_CreateBlockMap() 'If enabled.
                End If
                Return ConnectionStatus.Supported
            Else
                MyMPDevice = Nothing
                RaiseEvent PrintConsole(RM.GetString("fcusb_spi_email"))
                Return ConnectionStatus.NotSupported
            End If
        Else
            RaiseEvent PrintConsole("Unable to detect any Flash device")
            Return ConnectionStatus.NotDetected
        End If
    End Function

    Private Function DetectFlashDevice(ByVal mode As AdatperType) As Boolean
        Select Case mode
            Case AdatperType.X8_Type1
                RaiseEvent PrintConsole("Attemping to detect Flash in NOR x8 mode (Type-1)")
                SetupUSB(EXPIO_Mode.x8) 'Sends commands to setup ExtI/O adapter
                EXPIO_SETUP_READIDENT(E_EXPIO_IDENT.Type1) '(0x5555=0xAA;0x2AAA=0x55;0x5555=0x90)
                EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.Parallel_PLCC)
            Case AdatperType.X8_Type2
                RaiseEvent PrintConsole("Attemping to detect Flash in NOR x8 mode (Type-2)")
                SetupUSB(EXPIO_Mode.x8) 'Sends commands to setup ExtI/O adapter
                EXPIO_SETUP_READIDENT(E_EXPIO_IDENT.Type3) '(0x555=0xAA;2AA=0x55;0x555=0x90)
                EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.Parallel_A18)'Used by TSOP48 in x8 mode
            Case AdatperType.X8_Type3
                RaiseEvent PrintConsole("Attemping to detect Flash in NOR x8 mode (Type-3)")
                SetupUSB(EXPIO_Mode.x8) 'Sends commands to setup ExtI/O adapter
                EXPIO_SETUP_READIDENT(E_EXPIO_IDENT.Type2) '(0x555=0xAA;2AA=0x55;0x555=0x90)
                EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.Parallel)
            Case AdatperType.X16_Type1
                RaiseEvent PrintConsole("Attemping to detect Flash in NOR x16 mode (Type-1)")
                SetupUSB(EXPIO_Mode.x16) 'Older devices
                EXPIO_SETUP_READIDENT(E_EXPIO_IDENT.Type1) '(0x5555=0xAA;0x2AAA=0x55;0x5555=0x90)
            Case AdatperType.X16_Type2
                RaiseEvent PrintConsole("Attemping to detect Flash in NOR x16 mode (Type-2)")
                SetupUSB(EXPIO_Mode.x16)
                EXPIO_SETUP_READIDENT(E_EXPIO_IDENT.Type2) '(0x555=0xAA;2AA=0x55;0x555=0x90)
            Case AdatperType.NAND
                RaiseEvent PrintConsole("Attemping to detect Flash in NAND x8 mode")
                SetupUSB(EXPIO_Mode.NAND)
        End Select
        Threading.Thread.Sleep(100)
        If DetectFlash(mode, CHIPID_MFG, CHIPID_PART) Then 'Returns true if we detect a CFI EXT I/O Flash
            MyAdapter = mode
            Return True 'Detected
        Else
            Return False 'Not detected
        End If
    End Function

#Region "EXPIO SETUP"

    Private Enum E_EXPIO_WRADDR As UInt16
        Parallel = 1
        Parallel_PLCC = 2 'PLCC devices (2mbit Or smaller)
        Parallel_4Mbit = 3 'PLCC devices (4mbit)
        Parallel_A18 = 4 'Uses DQ15 for A-1, used by TSOP48 in x8 mode
        NAND1 = 5 '512
        NAND2 = 6 '2048 / 4096
        NAND3 = 7 '8192
    End Enum

    Private Enum E_EXPIO_IDENT As UInt16
        Type1 = 1 '(0x5555=0xAA;0x2AAA=0x55;0x5555=0x90) READ 0x00,0x01,0x02 (0x5555=0xAA;0x2AAA=0x55;0x5555=0xF0)
        Type2 = 2 '(0x555=0xAA;2AA=0x55;0x555=0x90) READ 0x00,0x01,0x0E,0x0F,0x03 (0x00=0x00F0)
        Type3 = 3 '(0xAAA=0xAA;0x555=0x55;0xAAA=0x90)
        NAND = 4
    End Enum

    Private Enum E_EXPIO_SECTOR As UInt16
        Type1 = 1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0x80;0x5555=0xAA;0x2AAA=0x55;SA=0x30 (used by older devices)
        Type2 = 2 '0xAAA=0xAA;0x555=0x55;0xAAA=0x80;0xAAA=0xAA;0x555=0x55;SA=0x30 (used by x8 devices)
        Type3 = 3 '0x555=0xAA,0x2AA=0x55,0x555=0x80,0x555=0xAA,0x2AA=0x55;SA=0x30 (used by x16 devices)
        Type4 = 4 'SA=0x50;SA=0x60;SA=0xD0,SR.7,SA=0x20;SA=0xD0,SR.7 (used by Intel/Sharp devices)
        NAND1 = 5
        NAND2 = 6
    End Enum

    Private Enum E_EXPIO_CHIPERASE As UInt16
        Type1 = 1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0x80;0x5555=0xAA;0x2AAA=0x55;0x5555=0x10 (used by older devices)
        Type2 = 2 '0xAAA=0xAA;0x555=0x55;0xAAA=0x80;0xAAA=0xAA;0x555=0x55;0xAAA=0x10 (used by x8 devices)
        Type3 = 3 '0x555=0xAA;0x2AA=0x55;0x555=0x80;0x555=0xAA;0x2AA=0x55;0x555=0x10 (used by x16 devices)
        Type4 = 4 '0x00=0x30;0x00=0xD0;
    End Enum

    Private Enum E_EXPIO_WRITEDATA As UInt16
        Type1 = 1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0xA0;SA=DATA;DELAY (used by older devices)
        Type2 = 2 '0xAAA=0xAA;0x555=0x55;0xAAA=0xA0;SA=DATA;DELAY (used by x8 devices)
        Type3_X8 = 3
        Type3_X16 = 4 '0x555=0xAA;0x2AA=0x55;0x555=0xA0,SA=DATA;DELAY (used by x16 devices)
        Type4_X8 = 5 'SA=0x40;SA=DATA;SR.7 (used by Intel/Sharp devices) X8 version
        Type4_X16 = 6 'SA=0x40;SA=DATA;SR.7 (used by Intel/Sharp devices) X16 version 
        Type5 = 7 '(Bypass) 0x555=0xAA;0x2AA=55;0x555=20;(0x00=0xA0;SA=DATA;...)0x00=0x90;0x00=0x00
        Type6 = 8 '(Page)0x5555,0x2AAA,0x5555;(BA/DATA)
        Type7 = 9 '(Buffer)0xE8...0xD0 (16x only)
    End Enum

    Private Function EXPIO_SETUP_WRITEADDRESS(ByVal mode As E_EXPIO_WRADDR) As Boolean
        Try
            If FCUSB Is Nothing Then Return False
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_MODE_ADDRESS, CUShort(0), mode, CShort(0))
            Dim result As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Threading.Thread.Sleep(25)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_READIDENT(ByVal mode As E_EXPIO_IDENT) As Boolean
        Try
            If FCUSB Is Nothing Then Return False
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_MODE_IDENT, CUShort(0), mode, CShort(0))
            Dim result As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Threading.Thread.Sleep(25)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_ERASESECTOR(ByVal mode As E_EXPIO_SECTOR) As Boolean
        Try
            If FCUSB Is Nothing Then Return False
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_MODE_ERSCR, CUShort(0), mode, CShort(0))
            Dim result As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Threading.Thread.Sleep(25)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_CHIPERASE(ByVal mode As E_EXPIO_CHIPERASE) As Boolean
        Try
            If FCUSB Is Nothing Then Return False
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_MODE_ERCHP, CUShort(0), mode, CShort(0))
            Dim result As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Threading.Thread.Sleep(25)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_WRITEDATA(ByVal mode As E_EXPIO_WRITEDATA) As Boolean
        Try
            If FCUSB Is Nothing Then Return False
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_MODE_WRITE, CUShort(0), mode, CShort(0))
            Dim result As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Threading.Thread.Sleep(25)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_NANDOFFSET(ByVal page_offset_bits As UInt16) As Boolean
        Try
            If FCUSB Is Nothing Then Return False
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_NAND_PAGEOFFSET, CUShort(0), page_offset_bits, CShort(0))
            Dim result As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Threading.Thread.Sleep(25)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_WRITEDELAY(ByVal delay_cycles As UInt16) As Boolean
        Try
            If FCUSB Is Nothing Then Return False
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_DELAY, CUShort(delay_cycles), CUShort(0), CShort(0))
            Dim result As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Threading.Thread.Sleep(25)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Sub EXPIO_CHIPENABLE_HIGH()
        Try
            If FCUSB Is Nothing Then Exit Sub
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_CE_HIGH, 0, 0, 0)
            FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
        Catch ex As Exception
        End Try
    End Sub

    Private Sub EXPIO_CHIPENABLE_LOW()
        Try
            If FCUSB Is Nothing Then Exit Sub
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_CE_LOW, 0, 0, 0)
            FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
        Catch ex As Exception
        End Try
    End Sub

#End Region

    Public Function GetFlashSize() As Long
        If MyMPDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
            Return NAND_VALID_SIZE
        Else
            Return EXT_IF.MyMPDevice.FLASH_SIZE
        End If
    End Function

    Public Function GetJedecID() As String
        Return Hex(MyMPDevice.MFG_CODE).PadLeft(2, CChar("0")) & " " & Hex(MyMPDevice.PART_CODE).PadLeft(4, CChar("0"))
    End Function

    Private Function DetectFlash(ByVal mode As AdatperType, ByRef id_mfg As Byte, ByRef id_did As UInt32) As Boolean
        Try
            id_mfg = 0 '&HDA
            id_did = 0 ' &H0B
            Dim ident_data() As Byte = Nothing
            If Not ReadFlashIdent(mode, ident_data) Then Return False
            id_mfg = ident_data(0)
            id_did = (CUInt(ident_data(1)) << 24) Or (CUInt(ident_data(2)) << 16) Or (CUInt(ident_data(3)) << 8)
            'ident_data(4) = bootlock or secure data
            'Dim BootBlockLock As Boolean = CBool(((ident And &HFF00) >> 24) And 1) 'The top 8 bits is the lock status
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function ReadFlashIdent(ByVal mode As AdatperType, ByRef IdentData() As Byte) As Boolean
        Try
            Dim ret As Integer = 0
            Dim BufferOut(9) As Byte
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor)
            Dim usbSetupPacket As New UsbSetupPacket(usbflag, EXPIOREQ_RDID, CUShort(0), CUShort(0), CShort(BufferOut.Length))
            If Not FCUSB.ControlTransfer(usbSetupPacket, BufferOut, BufferOut.Length, ret) Then Return False
            Array.Reverse(BufferOut)
            Dim Match As Boolean = True
            For i = 0 To 4
                If BufferOut(i) <> BufferOut(i + 5) Then Match = False
            Next
            If Match Then Return False 'Read the same BEFORE and AFTER read chip id
            If BufferOut(0) = 0 AndAlso BufferOut(1) = 0 Then Return False '0x0000
            If BufferOut(0) = &H90 AndAlso BufferOut(1) = &H90 Then Return False '0x9090
            If BufferOut(0) = &H90 AndAlso BufferOut(1) = 0 Then Return False '0x9000
            If BufferOut(0) = &HFF AndAlso BufferOut(1) = &HFF Then Return False '0xFFFF
            If BufferOut(0) = &HFF AndAlso BufferOut(1) = 0 Then Return False '0xFF00
            IdentData = BufferOut
            ReDim IdentData(4)
            Array.Copy(BufferOut, 0, IdentData, 0, 5)
            Return True
        Catch ex As Exception
        End Try
        Return False
    End Function

    Private Function SetupUSB(ByVal mode As EXPIO_Mode) As Boolean
        Try
            If FCUSB Is Nothing Then Return False
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_INIT, CUShort(mode), CUShort(0), CShort(4))
            Dim Result As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Threading.Thread.Sleep(100) 'Give the USB time to change modes
            Return Result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function SectorErase(ByVal sector_index As UInt32, ByVal memory_area As Byte) As Boolean
        Try
            If FCUSB Is Nothing Then Return False
            If Not MyMPDevice.ERASE_REQUIRED Then Return True
            If (MyMPDevice.FLASH_TYPE = MemoryType.SLC_NAND) Then
                Dim Logical_Address As Long = (DirectCast(MyMPDevice, NAND_Flash).BLOCK_SIZE * sector_index)
                Dim physical As Long = NAND_GetAddressMapping(Logical_Address)
                Return NAND_ERASEBLOCK(physical, memory_area, NAND_Preserve)
            Else
                If sector_index = 0 AndAlso DirectCast(MyMPDevice, MFP_Flash).GetSectorSize(0) = MyMPDevice.FLASH_SIZE Then
                    Return EraseChip() 'Single sector, must do a full chip erase instead
                Else
                    Dim Logical_Address As Long = 0
                    If (sector_index > 0) Then
                        For i As UInt32 = 0 To sector_index - 1
                            Dim SectorSize As Integer = DirectCast(MyMPDevice, MFP_Flash).GetSectorSize(i)
                            Logical_Address += SectorSize
                        Next
                    End If
                    If SO44_VPP = SO44_VPP_SETTING.Write_12v Then EXPIO_CHIPENABLE_LOW() 'VPP=12V
                    Dim Result As Boolean = False
                    Try
                        Dim ret As Integer = 0
                        Dim upper16 As UInt16 = CUShort((Logical_Address And &HFFFF0000) >> 16)
                        Dim lower16 As UInt16 = CUShort(Logical_Address And &HFFFF)
                        Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                        Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_SECTORERASE, upper16, lower16, CShort(0))
                        Result = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
                    Catch ex As Exception
                    End Try
                    If SO44_VPP = SO44_VPP_SETTING.Write_12v Then EXPIO_CHIPENABLE_HIGH() 'VPP=5V
                    If Not Result Then Return False
                    Utilities.Sleep(100)
                    If DirectCast(MyMPDevice, MFP_Flash).RESET_REQUIRED Then ResetDevice()
                    Return BlankCheck(Logical_Address)
                End If
            End If
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function ReadData(ByVal logical_address As Long, ByVal Length As Long, ByVal memory_area As NAND_AREA) As Byte()
        Dim BULK_ENABLE As Boolean = True 'DEBUG ONLY
        Dim data_out() As Byte = Nothing
        If MyMPDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
            If memory_area = NAND_AREA.Extended Then 'We need to adjust the logical address to point to the main area address
                Dim ext_page_size As Integer = DirectCast(MyMPDevice, NAND_Flash).PAGE_SIZE_EXTENDED - MyMPDevice.PAGE_SIZE
                Dim page_index As Long = Math.Floor(CDbl(logical_address) / CDbl(ext_page_size))
                Dim page_offset As Long = logical_address - (page_index * ext_page_size)
                logical_address = (page_index * CLng(MyMPDevice.PAGE_SIZE)) + page_offset
            End If
            logical_address = NAND_GetAddressMapping(logical_address)
        End If
        If MyMPDevice.FLASH_TYPE = MemoryType.SLC_NAND And BULK_ENABLE Then
            If memory_area = 0 Then
                data_out = NAND_ReadBulk(logical_address, Length, NAND_AREA.Data)
            ElseIf memory_area = 1 Then
                data_out = NAND_ReadBulk(logical_address, Length, NAND_AREA.Extended)
            Else
                Return Nothing
            End If
        Else
            ReDim data_out(Length - 1)
            Dim bytesleft As Integer = Length
            Dim pointer As Integer = 0
            Dim i As Integer = -1
            Do Until bytesleft = 0
                i += 1
                Dim packet() As Byte = ReadPacket(logical_address, bytesleft)
                If packet Is Nothing Then Return Nothing 'Error reading from USB
                Array.Copy(packet, 0, data_out, pointer, packet.Length)
                pointer += packet.Length
                logical_address += packet.Length
                bytesleft -= packet.Length
            Loop
            Return data_out
        End If
        Return data_out
    End Function

    Public Function EraseChip() As Boolean
        Try
            If FCUSB Is Nothing Then Return False
            If MyMPDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
                NAND_EraseChip()
            Else
                Try
                    Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                    If SO44_VPP = SO44_VPP_SETTING.Write_12v Then EXPIO_CHIPENABLE_LOW() 'VPP=12V
                    Select Case DirectCast(MyMPDevice, MFP_Flash).ChipEraseMode
                        Case EraseMethod.Standard
                            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_CHIPERASE, 0, 0, 0)
                            Dim CMD_RES As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, 0)
                            Utilities.Sleep(200) 'Perform blank check
                            If DirectCast(MyMPDevice, MFP_Flash).RESET_REQUIRED Then ResetDevice()
                            For i = 0 To 179 '3 minutes
                                If BlankCheck(0) Then Return True
                                Utilities.Sleep(900)
                            Next
                            Return False 'Timeout (device erase failed)
                        Case EraseMethod.BySector 'Erase entire device by sector only
                            Dim BlockCount As Integer = DirectCast(MyMPDevice, MFP_Flash).BLOCK_COUNT
                            SetProgress(0)
                            For i = 0 To BlockCount - 1
                                If (Not SectorErase(i, 0)) Then
                                    SetProgress(0)
                                    Return False 'Error erasing sector
                                Else
                                    Dim percent As Single = (i / BlockCount) * 100
                                    SetProgress(Math.Floor(percent))
                                End If
                            Next
                            SetProgress(0)
                            Return True 'Device successfully erased
                    End Select
                Catch ex As Exception
                Finally
                    If SO44_VPP = SO44_VPP_SETTING.Write_12v Then EXPIO_CHIPENABLE_HIGH() 'VPP=5V
                End Try
            End If
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Function ResetDevice() As Boolean
        Try
            If FCUSB Is Nothing Then Return False
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_RESET, CUShort(0), CUShort(0), CShort(0))
            Dim result As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, 0)
            Utilities.Sleep(50)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Sub NOR_WaitForReady()
        Utilities.Sleep(100)
    End Sub

    'Allows you to write up to 64 bytes to the EXT I/O
    Public Function WriteData(ByVal logical_address As Long, ByVal data_out() As Byte, ByVal memory_area As Integer) As Boolean
        AbortOperation = False
        If MyMPDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
            If memory_area = 1 Then 'We need to adjust the logical address to point to the main area address
                Dim ext_page_size As Integer = DirectCast(MyMPDevice, NAND_Flash).PAGE_SIZE_EXTENDED - MyMPDevice.PAGE_SIZE
                Dim page_index As UInt32 = Math.Floor(CDbl(logical_address) / CDbl(ext_page_size))
                Dim page_offset As UInt32 = logical_address - (page_index * ext_page_size)
                logical_address = (page_index * MyMPDevice.PAGE_SIZE) + page_offset
            End If
            Dim physical As Long = NAND_GetAddressMapping(logical_address)
            Return NAND_WRITEPAGE(physical, data_out, memory_area) 'We will write the whole block instead
        Else
            Dim NORFLASH As MFP_Flash = DirectCast(MyMPDevice, MFP_Flash)
            Try
                If SO44_VPP = SO44_VPP_SETTING.Write_12v Then EXPIO_CHIPENABLE_LOW() 'VPP=12V
                Dim DataToWrite As Integer = data_out.Length
                Dim Loops As Integer = CInt(Math.Ceiling(DataToWrite / 64)) 'Calcuates iterations
                Dim Update As Integer = (Loops / 4)
                For i As Integer = 0 To Loops - 1 'Math.Ceiling(bytesleft / 64) - 1
                    If AbortOperation Then Return False
                    Dim BufferSize As Integer = DataToWrite
                    If (BufferSize > 64) Then BufferSize = 64
                    Dim data(BufferSize - 1) As Byte
                    Array.Copy(data_out, (i * 64), data, 0, data.Length)
                    Dim ReturnValue As Boolean = EXPIO_WRITEDATA(logical_address, data)
                    If Not ReturnValue Then Return False
                    If NORFLASH.WriteMode = MFP_ProgramMode.PageMode Then
                        Utilities.Sleep(5) 'Tested. 5 Works.
                    End If
                    logical_address += data.Length
                    DataToWrite -= data.Length
                Next
            Catch ex As Exception
            Finally
                If SO44_VPP = SO44_VPP_SETTING.Write_12v Then EXPIO_CHIPENABLE_HIGH() 'VPP=5V
            End Try
            If DirectCast(MyMPDevice, MFP_Flash).RESET_REQUIRED Then ResetDevice()
            Return True
            End If
            Return False
    End Function
    'Writes up to 64-bytes using the standard JEDEC Write SEQ op codes
    Private Function EXPIO_WRITEDATA(ByVal Address As UInteger, ByVal data_out() As Byte) As Boolean
        Try
            Dim upper16 As UInt16 = CUShort((Address And &HFFFF0000) >> 16)
            Dim lower16 As UInt16 = CUShort(Address And &HFFFF)
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_WRITEDATA, upper16, lower16, CShort(data_out.Length))
            Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, data_out, data_out.Length, ret)
            Return res
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function WriteAddressWord(ByVal addr24 As UInt32, ByVal value As UInt16) As Boolean
        If Not WriteAddress(0) Then Return False
        If Not WriteWord(value) Then Return False
        Return True
    End Function

    'Sets the A0 to A23 pins on the EXP board
    Private Function WriteAddress(ByVal addr24 As UInteger) As Boolean
        Try
            Dim upper16 As UInt16 = CUShort((addr24 And &HFF0000) >> 16)
            Dim lower16 As UInt16 = CUShort(addr24 And &HFFFF)
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_ADDRESS, upper16, lower16, CShort(0))
            Dim ret As Integer = 0
            Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Return res
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function ReadByte(ByRef data As Byte) As Boolean
        Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor)
        Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_READBYTE, CUShort(0), CUShort(0), CShort(1))
        Dim ret As Integer = 0
        Dim data_back(0) As Byte
        If FCUSB.ControlTransfer(usbPacket2, data_back, data_back.Length, ret) Then
            data = data_back(0)
            Return True
        Else
            Return False
        End If
    End Function

    Private Function ReadWord(ByRef data As UInt16) As Boolean
        Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor)
        Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_READWORD, CUShort(0), CUShort(0), CShort(2))
        Dim ret As Integer = 0
        Dim data_back(1) As Byte
        If FCUSB.ControlTransfer(usbPacket2, data_back, data_back.Length, ret) Then
            data = (CUShort(data_back(1)) << 8) + data_back(0)
            Return True
        Else
            Return False
        End If
    End Function

    Private Function WriteByte(ByVal data As Byte) As Boolean
        Try
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_WRITEBYTE, CUShort(0), CUShort(data), CShort(0))
            Dim ret As Integer = 0
            Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Return res
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function WriteWord(ByVal data As UInt16) As Boolean
        Try
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_WRITEWORD, CUShort(0), CUShort(data), CShort(0))
            Dim ret As Integer = 0
            Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
            Return res
        Catch ex As Exception
            Return False
        End Try
    End Function
    'Does a read operation using a single USB packet (up to 64 bytes)
    Private Function ReadPacket(ByVal address As UInt32, ByVal count As Integer) As Byte()
        Dim packet_size As Integer = count
        If packet_size > 64 Then packet_size = 64
        Dim packet(packet_size - 1) As Byte
        Try
            Dim ret As Integer = 0
            Dim ByteCount As Integer = packet.Length
            Dim upper16 As UInt16 = CUShort((address And &HFFFF0000) >> 16)
            Dim lower16 As UInt16 = CUShort(address And &HFFFF)
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_READDATA, upper16, lower16, CShort(ByteCount))
            Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, packet, packet.Length, ret)
            If Not res Then Return Nothing
            Return packet
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Private Function BlankCheck(ByVal base_addr As Long) As Boolean
        Try
            Dim IsBlank As Boolean = False
            Dim Counter As Integer = 0
            Do Until IsBlank
                Utilities.Sleep(50)
                Dim w() As Byte = ReadData(base_addr, 4, 0)
                If w Is Nothing Then Return False
                If w(0) = 255 AndAlso w(1) = 255 AndAlso w(2) = 255 AndAlso w(3) = 255 Then IsBlank = True
                Counter += 1
                If Counter = 20 Then Return False
            Loop
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function



#Region "NAND Engine"
    Public NAND_MAP As New List(Of Long) 'List of valid blocks
    Public NAND_BAD_BLOCK As New List(Of Long) 'List of BAD blocks
    Public NAND_VALID_SIZE As Long 'Total amount of valid bytes

    Private Sub NAND_CreateBlockMap()
        'Dim SKIP As Boolean = False 'SET TO TRUE FOR DEBUG
        If NAND_Manager Then
            SetStatus("NAND memory device detected, loading valid memory map")
            RaiseEvent PrintConsole("Loading NAND memory map for valid memory")
        Else
            RaiseEvent PrintConsole("NAND block manager disabled")
        End If
        NAND_MAP.Clear()
        NAND_BAD_BLOCK.Clear()
        NAND_VALID_SIZE = 0
        Dim BlockSize As Integer = DirectCast(MyMPDevice, NAND_Flash).BLOCK_SIZE
        Dim BlockCount As Integer = MyMPDevice.FLASH_SIZE / BlockSize
        Dim BaseAddress As UInt64 = 0
        For i = 0 To BlockCount - 1
            If NAND_Manager Then
                Dim e() As Byte = NAND_ReadBulk(BaseAddress, 16, NAND_AREA.Extended)
                Dim BAD_BLOCK As Boolean = False
                If MyMPDevice.PAGE_SIZE = 512 Then 'SLC-small page
                    If (Not e(5) = 255) Then BAD_BLOCK = True
                ElseIf MyMPDevice.PAGE_SIZE = 2048 Then 'SLC-large page
                    If (Not e(0) = 255) AndAlso (Not e(0) = 255) Then BAD_BLOCK = True
                Else
                    If (Not e(0) = 255) Then BAD_BLOCK = True
                End If
                If BAD_BLOCK Then 'Bad block (6th byte is not FF)
                    NAND_BAD_BLOCK.Add(BaseAddress)
                    RaiseEvent PrintConsole("BAD NAND BLOCK AT address: 0x" & Hex(BaseAddress).PadLeft(8, "0"))
                Else 'valid block
                    NAND_MAP.Add(BaseAddress)
                End If
            Else
                NAND_MAP.Add(BaseAddress)
            End If
            BaseAddress += BlockSize
        Next
        NAND_VALID_SIZE = CULng(NAND_MAP.Count) * BlockSize
        RaiseEvent PrintConsole("NAND memory map complete: " & Format(NAND_VALID_SIZE, "#,###") & " bytes available for access")
    End Sub

    Public Sub NAND_MarkBadBlock(ByVal logical_address As Long, ByVal memory_area As NAND_AREA)
        If memory_area = NAND_AREA.Extended Then 'We need to adjust the logical address to point to the main area address
            Dim ext_page_size As Integer = DirectCast(MyMPDevice, NAND_Flash).PAGE_SIZE_EXTENDED - MyMPDevice.PAGE_SIZE
            Dim page_index As UInt32 = Math.Floor(CDbl(logical_address) / CDbl(ext_page_size))
            Dim page_offset As UInt32 = logical_address - (page_index * ext_page_size)
            logical_address = (page_index * MyMPDevice.PAGE_SIZE) + page_offset
        End If
        Dim physical_address As Long = NAND_GetAddressMapping(logical_address)
        Dim e() As Byte = NAND_ReadBulk(physical_address, 16, NAND_AREA.Extended) 'We grab one line
        If MyMPDevice.PAGE_SIZE = 512 Then 'SLC-small page
            e(5) = 0
        ElseIf MyMPDevice.PAGE_SIZE = 2048 Then 'SLC-large page
            e(0) = 0
            e(5) = 0
        Else
            e(0) = 0
        End If
        NAND_WRITEPAGE(physical_address, e, NAND_AREA.Extended)
    End Sub
    'Returns the physical address from the logical address
    Public Function NAND_GetAddressMapping(ByVal logical_address As Long) As Long
        Dim BlockSize As Integer = DirectCast(MyMPDevice, NAND_Flash).BLOCK_SIZE
        Dim offset As UInt64 = (logical_address Mod BlockSize)
        Dim logical_Base As UInt64 = logical_address - offset
        Dim address As UInt64 = 0
        For i = 0 To NAND_MAP.Count - 1
            If address = logical_Base Then
                Return (NAND_MAP(i) + offset)
            End If
            address += BlockSize
        Next
        Return 0 'NOT FOUND
    End Function

    Public Function NAND_ReadBulk(ByVal page_address As Long, ByVal count As Long, ByVal memory_area As NAND_AREA) As Byte()
        Try
            Dim page_size_ext As UInt16 = DirectCast(MyMPDevice, NAND_Flash).PAGE_SIZE_EXTENDED - MyMPDevice.PAGE_SIZE '16 bytes
            Dim page_size As UInt16 = 0 'Number of bytes to read before we change pages
            If memory_area = NAND_AREA.Data Then
                page_size = MyMPDevice.PAGE_SIZE
            ElseIf memory_area = NAND_AREA.Extended Then
                page_size = page_size_ext
            End If
            Dim data_in() As Byte = NAND_GetSetupPacket(page_address, count, page_size, MyMPDevice.PAGE_SIZE, memory_area)
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbpacket1 As New UsbSetupPacket(usbflag, EXPIOREQ_NANDREADBULK, 0, 0, CShort(data_in.Length))
            FCUSB.ControlTransfer(usbpacket1, data_in, data_in.Length, Nothing)
            Dim data_out(count - 1) As Byte
            Dim reader As UsbEndpointReader = FCUSB.OpenEndpointReader(ReadEndpointID.Ep01, data_out.Length, EndpointType.Bulk)
            Dim ec As ErrorCode = reader.Read(data_out, 0, CInt(data_out.Length), 5000, Nothing) 'DEBUG!
            If (Not ec = ErrorCode.None) Then Return Nothing
            Return data_out
        Catch ex As Exception
            Return Nothing
        End Try
    End Function
    'Checks the PB5 Ready/Busy pin
    Public Sub NAND_WaitForReady()
        Try
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbSetupPacket As New UsbSetupPacket(usbflag, EXPIOREQ_NAND_WAIT, 0, 0, 0)
            FCUSB.ControlTransfer(usbSetupPacket, Nothing, 0, Nothing)
        Catch ex As Exception
        End Try
    End Sub
    'This writes the data to a page
    Public Function NAND_WRITEPAGE(ByVal page_address As Long, ByVal data_out() As Byte, ByVal area As NAND_AREA) As Boolean
        Try
            Dim page_size_ext As UInt16 = (DirectCast(MyMPDevice, NAND_Flash).PAGE_SIZE_EXTENDED - MyMPDevice.PAGE_SIZE) '16 bytes
            Dim page_size As UInt16 = 0 'Number of bytes to read before we change pages
            If area = NAND_AREA.Data Then
                page_size = MyMPDevice.PAGE_SIZE
            ElseIf area = NAND_AREA.Extended Then
                page_size = page_size_ext
            End If
            Dim data_in() As Byte = NAND_GetSetupPacket(page_address, data_out.Length, page_size, MyMPDevice.PAGE_SIZE, area)
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            If area = NAND_AREA.Data Then
                Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_NANDWRBULK, 0, 0, CShort(data_in.Length))
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, data_in, data_in.Length, Nothing)
                If Not res Then Return False
            Else
                Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_NAND_WR_SA, 0, 0, CShort(data_in.Length))
                Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, data_in, data_in.Length, Nothing)
                If Not res Then Return False
            End If
            Dim writer As UsbEndpointWriter = FCUSB.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
            Dim ec As ErrorCode = writer.Write(data_out, 0, data_out.Length, 5000, Nothing) 'DEBUG
            If ec = ErrorCode.None Then
                Utilities.Sleep(2) 'Definalty need some kind of slight delay
                NAND_WaitForReady() 'PB5 signals busy/ready
                Return True
            End If
        Catch ex As Exception
            Return False
        End Try
        Return False
    End Function
    'Returns the total space of the extra data
    Public Function NAND_Extra_GetSize() As Long
        Dim PageCount As Long = MyMPDevice.FLASH_SIZE / MyMPDevice.PAGE_SIZE 'Total number of pages in this device
        Dim ExtPageSize As Long = DirectCast(MyMPDevice, NAND_Flash).PAGE_SIZE_EXTENDED - MyMPDevice.PAGE_SIZE
        Dim BlockCount As Long = MyMPDevice.FLASH_SIZE / DirectCast(MyMPDevice, NAND_Flash).BLOCK_SIZE
        Dim PageExtraSize As Long = (PageCount * ExtPageSize)
        Return PageExtraSize
    End Function

    Public Function NAND_ReadStatusRegister() As Byte
        Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor)
        Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_NAND_SR, CUShort(0), CUShort(0), CShort(1))
        Dim data_back(0) As Byte
        FCUSB.ControlTransfer(usbPacket2, data_back, data_back.Length, Nothing)
        Return data_back(0)
    End Function

    Public Function NAND_ERASEBLOCK(ByVal page_address As Long, ByVal Memory_area As NAND_AREA, ByVal CopyExtenedArea As Boolean) As Boolean
        'CopyExtenedArea = False 'DEBUG
        Dim BlockSize As Integer = DirectCast(MyMPDevice, NAND_Flash).BLOCK_SIZE
        Dim page_count As Integer = BlockSize / MyMPDevice.PAGE_SIZE 'number of pages per block
        Dim ext_size As Integer = DirectCast(MyMPDevice, NAND_Flash).PAGE_SIZE_EXTENDED - MyMPDevice.PAGE_SIZE 'i.e. 16
        Dim other_memory_area() As Byte = Nothing
        If CopyExtenedArea Then
            If Memory_area = NAND_AREA.Data Then
                other_memory_area = NAND_ReadBulk(page_address, page_count * ext_size, NAND_AREA.Extended) 'We want to read all of the extended area
            ElseIf Memory_area = NAND_AREA.Extended Then
                other_memory_area = NAND_ReadBulk(page_address, page_count * MyMPDevice.PAGE_SIZE, NAND_AREA.Data)
            End If
        End If
        Try 'START BLOCK ERASE
            Dim data_in() As Byte = NAND_GetSetupPacket(page_address, 0, 0, 0, NAND_AREA.Data)
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_SECTORERASE, 0, 0, CShort(data_in.Length))
            Dim Result As Boolean = FCUSB.ControlTransfer(usbPacket2, data_in, data_in.Length, Nothing)
            If Not Result Then Return False
        Catch ex As Exception
        End Try  'BLOCK ERASE COMPLETE
        If CopyExtenedArea Then
            NAND_WaitForReady()
            If Memory_area = NAND_AREA.Data Then
                NAND_WRITEPAGE(page_address, other_memory_area, NAND_AREA.Extended) 'We want to write back the origional ext area data
            ElseIf Memory_area = NAND_AREA.Extended Then
                NAND_WRITEPAGE(page_address, other_memory_area, NAND_AREA.Data) 'We want to write back the origional ext area data
            End If
        End If
        Utilities.Sleep(20) 'We definately need this here
        NAND_WaitForReady()
        Return True
    End Function

    Public Enum NAND_AREA As Byte
        Data = 0
        Extended = 1
    End Enum

    Public Function NAND_EraseChip() As Boolean
        Dim BlockSize As Integer = DirectCast(MyMPDevice, NAND_Flash).BLOCK_SIZE
        Dim BlockCount As Integer = MyMPDevice.FLASH_SIZE / BlockSize
        Dim BaseAddress As UInt32 = 0
        For i = 0 To BlockCount - 1
            NAND_ERASEBLOCK(BaseAddress, NAND_AREA.Data, NAND_Preserve)
            BaseAddress += BlockSize
            If i Mod 10 = 0 Then
                Dim Percent As Integer = Math.Round((i / BlockCount) * 100)
                SetProgress(Percent)
            End If
        Next
        SetProgress(0)
        Return True
    End Function

    Private Function NAND_GetSetupPacket(ByVal Address As UInt32, ByVal Count As UInt32, ByVal PageSize As UInt16, ByVal Inc As UInt16, ByVal area As NAND_AREA) As Byte()
        Dim addr_bytes As Byte = 0
        If DirectCast(MyMPDevice, NAND_Flash).PAGE_SIZE = 512 Then
            If (MyMPDevice.FLASH_SIZE > &HFFFFFFUI) Then
                addr_bytes = 4
            Else
                addr_bytes = 3
            End If
        ElseIf DirectCast(MyMPDevice, NAND_Flash).PAGE_SIZE = 2048 Then
            addr_bytes = 4
        ElseIf DirectCast(MyMPDevice, NAND_Flash).PAGE_SIZE = 4096 Then
            addr_bytes = 4
        End If
        Dim data_in(13) As Byte
        data_in(0) = CByte(Address And 255)
        data_in(1) = CByte((Address >> 8) And 255)
        data_in(2) = CByte((Address >> 16) And 255)
        data_in(3) = CByte((Address >> 24) And 255)
        data_in(4) = CByte(Count And 255)
        data_in(5) = CByte((Count >> 8) And 255)
        data_in(6) = CByte((Count >> 16) And 255)
        data_in(7) = CByte((Count >> 24) And 255)
        data_in(8) = CByte(PageSize And 255)
        data_in(9) = CByte((PageSize >> 8) And 255)
        data_in(10) = CByte(Inc And 255)
        data_in(11) = CByte((Inc >> 8) And 255)
        data_in(12) = area 'Where to read data from
        data_in(13) = addr_bytes
        Return data_in
    End Function

#End Region




End Class
