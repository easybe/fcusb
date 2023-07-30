Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.SPI
Imports LibUsbDotNet
Imports LibUsbDotNet.Main

Public Class ExtPort
    Public FCUSB As UsbDevice
    Public MyFlashDevice As FlashMemory.Device  'Contains the definition for the EXT I/O device that is connected
    Public MyAdapter As AdatperType 'This is the kind of socket adapter connected and the mode it is in
    Private CURRENT_WRITE_MODE As E_EXPIO_WRITEDATA
    Public CHIPID_MFG As Byte = 0
    Public CHIPID_PART As UInt32 = 0
    Private Const USB_TIMEOUT As Integer = 5000


    Public Event PrintConsole(ByVal msg As String)

#Region "USB Hardware Flags"
    Public Const EXPIOREQ_INIT As Byte = &HA0
    Public Const EXPIOREQ_ADDRESS As Byte = &HA1
    Public Const EXPIOREQ_WRITEDATA As Byte = &HA7
    Public Const EXPIOREQ_READDATA As Byte = &HA8
    Public Const EXPIOREQ_RDID As Byte = &HA9
    Public Const EXPIOREQ_CHIPERASE As Byte = &HAB
    Public Const EXPIOREQ_SECTORERASE As Byte = &HAC
    Public Const EXPIOREQ_WRITEPAGE As Byte = &HAE
    Public Const EXPIOREQ_NAND_WAIT As Byte = &HB0
    Public Const EXPIOREQ_NAND_SR As Byte = &HB1
    Public Const EXPIOREQ_NAND_PAGEOFFSET As Byte = &HB4

    Public Const EXPIOREQ_MODE_ADDRESS As Byte = &HC0 'Sets the write address mode
    Public Const EXPIOREQ_MODE_IDENT As Byte = &HC1 'Detects the ident
    Public Const EXPIOREQ_MODE_ERSCR As Byte = &HC2 'Erases the sector
    Public Const EXPIOREQ_MODE_ERCHP As Byte = &HC3 'erases the chip
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
        X16_Type1 = 13 'SO-44 Devices
        X16_Type2 = 14 'TSOP48 / SO-44 / TSOP-56 29F devices
        NAND = 15 'Currently only x2 mode
    End Enum

    Public Enum EXPIO_Mode
        Setup = 0
        NOR_x8 = 1
        NOR_x16 = 2
        NAND_x8 = 3
    End Enum

    Public Function Init() As ConnectionStatus
        If Not EXPIO_SETUP_USB(EXPIO_Mode.Setup) Then
            RaiseEvent PrintConsole("Error: unable to connect to EXT I/O board over SPI")
            Return ConnectionStatus.ExtIoNotConnected
        Else
            RaiseEvent PrintConsole("EXT I/O board successfulled initialized")
        End If
        Dim Connected As Boolean = False
        If Not Connected Then Connected = DetectFlashDevice(AdatperType.NAND) 'SLC NAND devices
        If Not Connected Then Connected = DetectFlashDevice(AdatperType.X16_Type2) 'TSOP48 / TSOP-56 29F devices
        If Not Connected Then Connected = DetectFlashDevice(AdatperType.X16_Type1) 'SO-44
        If Not Connected Then Connected = DetectFlashDevice(AdatperType.X8_Type3) 'Used by DIP32
        If Not Connected Then Connected = DetectFlashDevice(AdatperType.X8_Type1) 'Used by PLCC32
        If Not Connected Then Connected = DetectFlashDevice(AdatperType.X8_Type2) 'Used by TSOP48-X8 
        If Connected Then
            RaiseEvent PrintConsole(RM.GetString("fcusb_spi_connflash") & " (CHIP ID: 0x" & Hex(CHIPID_MFG).PadLeft(2, "0") & Hex(CHIPID_PART).PadLeft(8, "0") & ")")
            Dim ID1 As UInt16 = (CHIPID_PART >> 16)
            Dim ID2 As UInt16 = (CHIPID_PART And &HFFFF)
            If MyAdapter = AdatperType.NAND Then
                MyFlashDevice = FlashDatabase.FindDevice(CHIPID_MFG, ID1, ID2, False, MemoryType.SLC_NAND)
            Else
                If MyAdapter = AdatperType.X8_Type1 Or MyAdapter = AdatperType.X8_Type2 Or MyAdapter = AdatperType.X8_Type3 Then
                    MyFlashDevice = FlashDatabase.FindDevice(CHIPID_MFG, ID1, ID2, True, MemoryType.PARALLEL_NOR)
                Else
                    MyFlashDevice = FlashDatabase.FindDevice(CHIPID_MFG, ID1, ID2, False, MemoryType.PARALLEL_NOR)
                End If
            End If
            If (MyFlashDevice IsNot Nothing) Then
                RaiseEvent PrintConsole(String.Format(RM.GetString("fcusb_spi_flashdetected"), MyFlashDevice.NAME, Format(MyFlashDevice.FLASH_SIZE, "#,###")))
                RaiseEvent PrintConsole(RM.GetString("fcusb_cmos_progmode"))
                If MyFlashDevice.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                    Dim NOR_FLASH As MFP_Flash = DirectCast(MyFlashDevice, MFP_Flash)
                    EXPIO_SETUP_WRITEDELAY(NOR_FLASH.WRITE_HARDWARE_DELAY)
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
                        EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type3) '0x555=0xAA;0x2AA=0x55;0x555=0xA0,SA=DATA;DELAY
                    ElseIf MyAdapter = AdatperType.X16_Type2 Then
                        EXPIO_SETUP_CHIPERASE(E_EXPIO_CHIPERASE.Type3) '0x555=0xAA;0x2AA=0x55;0x555=0x80;0x555=0xAA;0x2AA=0x55;0x555=0x10
                        EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.Type3) '0x555=0xAA,0x2AA=0x55,0x555=0x80,0x555=0xAA,0x2AA=0x55;SA=0x30
                        EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type3) '0x555=0xAA;0x2AA=0x55;0x555=0xA0,SA=DATA;DELAY
                    End If
                    If (MyAdapter = AdatperType.X8_Type1 Or MyAdapter = AdatperType.X8_Type3) AndAlso (NOR_FLASH.FLASH_SIZE = Mb004) Then
                        RaiseEvent PrintConsole("DIP32/PLCC32 4MBIT device detected, enabling A18 on Pin 1 (reset)")
                        EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.Parallel_DQ8) 'DQ8 is used for A18 (PLCC32-P1)
                    Else
                        Select Case NOR_FLASH.WriteMode
                            Case MFP_PROG.IntelSharp
                                EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.Type4) 'SA=0x50;SA=0x60;SA=0xD0(SR.7)SA=0x20;SA=0xD0(SR.7)
                                EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type4)'SA=0x40;SA=DATA;SR.7
                            Case MFP_PROG.IntelBuffer 'Writes to a buffer that is than auto-programmed
                                EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.Type4) 'SA=0x50;SA=0x60;SA=0xD0,SR.7,SA=0x20;SA=0xD0,SR.7
                                EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type7)
                            Case MFP_PROG.BypassMode 'Writes 64 bytes using ByPass sequence
                                EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type5) '(Bypass) 0x555=0xAA;0x2AA=55;0x555=20;(0x00=0xA0;SA=DATA;...)0x00=0x90;0x00=0x00
                            Case MFP_PROG.PageMode 'Writes an entire page of data (128 bytes etc.)
                                EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type6)
                            Case MFP_PROG.SpansionBuffer
                                EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type8)
                        End Select
                    End If
                    If (MyFlashDevice.FLASH_SIZE = Mb512) Then 'Device is a Mb512 device (must be x16 mode)
                        EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.Parallel_25bit) 'Uses CE line for A24 (X16 only)
                    ElseIf (MyFlashDevice.FLASH_SIZE > Mb512) Then '1Gbit or 2Gbit
                        If (HWBOARD = HwVariant.Classic) Then
                            Dim MbitStr As String = Utilities.FormatToMegabits(MyFlashDevice.FLASH_SIZE).Replace(" ", "")
                            RaiseEvent PrintConsole("Notice: " & MbitStr & " device detected, Extension Port can only access up to Mb512")
                            EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.Parallel_25bit) 'Extension port can only read/write up to 256Mbit
                        ElseIf HWBOARD = HwVariant.xPort Then
                            EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.Parallel_27bit) 'xPort board supports A25,A26 lines
                        End If
                    End If
                    WaitForReady()
                ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
                    RaiseEvent PrintConsole("Flash page size: " & MyFlashDevice.PAGE_SIZE & " bytes (" & DirectCast(MyFlashDevice, NAND_Flash).PAGE_SIZE_EXTENDED & " bytes extended)")
                    Select Case DirectCast(MyFlashDevice, NAND_Flash).PAGE_SIZE
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
                            EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.NAND2)
                            EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.NAND2)
                            EXPIO_SETUP_NANDOFFSET(13)
                    End Select
                    CreateNandMap()
                    ManagerNandMap() 'If enabled
                    ProcessNandMap()
                End If
                EXPIO_PrintCurrentWriteMode()
                Return ConnectionStatus.Supported
            Else
                MyFlashDevice = Nothing
                RaiseEvent PrintConsole(RM.GetString("fcusb_spi_email"))
                Return ConnectionStatus.NotSupported
            End If
        Else
            RaiseEvent PrintConsole("Unable to detect any Flash device")
            Return ConnectionStatus.NotDetected
        End If
    End Function

    Private Function debug_get_row(ByVal asc As String) As Byte()
        Dim d() As Byte = Utilities.Bytes.FromString(asc)
        ReDim Preserve d(15)
        Return d
    End Function

    Private Function DetectFlashDevice(ByVal mode As AdatperType) As Boolean
        Select Case mode
            Case AdatperType.X8_Type1 'PLCC-32 devices
                If Not EXPIO_SETUP_USB(EXPIO_Mode.NOR_x8) Then Return False  'Sends commands to setup ExtI/O adapter
                RaiseEvent PrintConsole("Attemping to detect Flash in NOR x8 mode (Type-1)")
                EXPIO_SETUP_READIDENT(E_EXPIO_IDENT.Type1) '(0x5555=0xAA;0x2AAA=0x55;0x5555=0x90)
                EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.Parallel_PLCC) 'Uses Parallel but with DQ8 (set to HIGH)
            Case AdatperType.X8_Type2 'TSOP-48 (X8)
                If Not EXPIO_SETUP_USB(EXPIO_Mode.NOR_x8) Then Return False 'Sends commands to setup ExtI/O adapter
                RaiseEvent PrintConsole("Attemping to detect Flash in NOR x8 mode (Type-2)")
                EXPIO_SETUP_READIDENT(E_EXPIO_IDENT.Type3) '(0x555=0xAA;2AA=0x55;0x555=0x90)
                EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.Parallel_DQ15)'DQ15 is used for A-1
            Case AdatperType.X8_Type3 'DIP32
                If Not EXPIO_SETUP_USB(EXPIO_Mode.NOR_x8) Then Return False 'Sends commands to setup ExtI/O adapter
                RaiseEvent PrintConsole("Attemping to detect Flash in NOR x8 mode (Type-3)")
                EXPIO_SETUP_READIDENT(E_EXPIO_IDENT.Type1) '(0x555=0xAA;2AA=0x55;0x555=0x90)
                EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.Parallel_24bit)
            Case AdatperType.X16_Type1
                If Not EXPIO_SETUP_USB(EXPIO_Mode.NOR_x16) Then Return False 'Older devices
                RaiseEvent PrintConsole("Attemping to detect Flash in NOR x16 mode (Type-1)")
                EXPIO_SETUP_READIDENT(E_EXPIO_IDENT.Type1) '(0x5555=0xAA;0x2AAA=0x55;0x5555=0x90)
            Case AdatperType.X16_Type2
                If Not EXPIO_SETUP_USB(EXPIO_Mode.NOR_x16) Then Return False
                RaiseEvent PrintConsole("Attemping to detect Flash in NOR x16 mode (Type-2)")
                EXPIO_SETUP_READIDENT(E_EXPIO_IDENT.Type2) '(0x555=0xAA;2AA=0x55;0x555=0x90)
            Case AdatperType.NAND
                If Not EXPIO_SETUP_USB(EXPIO_Mode.NAND_x8) Then Return False
                RaiseEvent PrintConsole("Attemping to detect Flash in NAND x8 mode (SLC)")
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
        Parallel_24bit = 1 'Standard 24-bit address (up to 128Mbit devices)
        Parallel_PLCC = 2 'DIP32/PLCC32 (<=2mbit) - Sets DQ8 to HIGH (for RESET pin)
        Parallel_DQ8 = 3 'DIP32/PLCC32 (4mbit) - DQ8 is used for A18
        Parallel_DQ15 = 4 'TSOP48 X8 - DQ15 is used for A-1
        NAND1 = 5 '512 (Legacy)
        NAND2 = 6 '2048 / 4096 / 8192
        Parallel_25bit = 7 'TSOP-56 256Mbit devices (uses A0 to A24)
        Parallel_27bit = 8 'TSOP-56 1Gbit-2Gbit devices (adds A25,A26) - Only xPort compatible
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
        NAND1 = 5 '512 (Legacy)
        NAND2 = 6 '2048 / 4096 / 8192
    End Enum

    Private Enum E_EXPIO_CHIPERASE As UInt16
        Type1 = 1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0x80;0x5555=0xAA;0x2AAA=0x55;0x5555=0x10 (used by older devices)
        Type2 = 2 '0xAAA=0xAA;0x555=0x55;0xAAA=0x80;0xAAA=0xAA;0x555=0x55;0xAAA=0x10 (used by x8 devices)
        Type3 = 3 '0x555=0xAA;0x2AA=0x55;0x555=0x80;0x555=0xAA;0x2AA=0x55;0x555=0x10 (used by x16 devices)
        Type4 = 4 '0x00=0x30;0x00=0xD0;
    End Enum

    Private Enum E_EXPIO_WRITEDATA As UInt16
        Type1 = 1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0xA0;SA=DATA;DELAY
        Type2 = 2 '0xAAA=0xAA;0x555=0x55;0xAAA=0xA0;SA=DATA;DELAY
        Type3 = 3 '0x555=0xAA;0x2AA=0x55;0x555=0xA0,SA=DATA;DELAY
        Type4 = 4 'SA=0x40;SA=DATA;SR.7
        Type5 = 5 '(Bypass) 0x555=0xAA;0x2AA=55;0x555=20;(0x00=0xA0;SA=DATA;...)0x00=0x90;0x00=0x00
        Type6 = 6 '(Page)0x5555,0x2AAA,0x5555;(BA/DATA)
        Type7 = 7 '(Buffer)0xE8...0xD0
        Type8 = 8 '(buffer)0x555=0xAA,0x2AA=0x55,SA=0x25,SA=(WC-1)..
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
            CURRENT_WRITE_MODE = mode
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

    Private Sub EXPIO_VPP_START()
        'We should only allow this for devices that have a 12V option/chip
        If MySettings.VPP_VCC = FlashcatSettings.VPP_SETTING.Write_12v Then
            EXPIO_CHIPENABLE_LOW() 'VPP=12V
        End If
    End Sub

    Private Sub EXPIO_VPP_STOP()
        'We should only allow this for devices that have a 12V option/chip
        If MySettings.VPP_VCC = FlashcatSettings.VPP_SETTING.Write_12v Then
            EXPIO_CHIPENABLE_HIGH() 'VPP=5V
        End If
    End Sub

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

    Private Sub EXPIO_PrintCurrentWriteMode()
        Select Case CURRENT_WRITE_MODE
            Case E_EXPIO_WRITEDATA.Type1
                RaiseEvent PrintConsole("Write mode supported: Type-1")
            Case E_EXPIO_WRITEDATA.Type2
                RaiseEvent PrintConsole("Write mode supported: Type-2")
            Case E_EXPIO_WRITEDATA.Type3
                RaiseEvent PrintConsole("Write mode supported: Type-3")
            Case E_EXPIO_WRITEDATA.Type4
                RaiseEvent PrintConsole("Write mode supported: Type-4")
            Case E_EXPIO_WRITEDATA.Type5
                RaiseEvent PrintConsole("Write mode supported: Type-5")
            Case E_EXPIO_WRITEDATA.Type6
                RaiseEvent PrintConsole("Write mode supported: Type-6")
            Case E_EXPIO_WRITEDATA.Type7
                RaiseEvent PrintConsole("Write mode supported: Type-7")
        End Select
    End Sub

#End Region

    Public Function GetFlashSize() As UInt32
        If MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
            Return NAND_AVAILABLE_DATA
        Else
            If (HWBOARD = HwVariant.Classic) AndAlso (MyFlashDevice.FLASH_SIZE > Mb512) Then
                Return Mb512 'EXT IO Board only supports up to Mb512 (64MB)
            Else
                Return EXT_IF.MyFlashDevice.FLASH_SIZE
            End If
        End If
    End Function

    Public Function GetJedecID() As String
        Return Hex(MyFlashDevice.MFG_CODE).PadLeft(2, CChar("0")) & " " & Hex(MyFlashDevice.ID1).PadLeft(4, CChar("0"))
    End Function

    Private Function DetectFlash(ByVal mode As AdatperType, ByRef id_mfg As Byte, ByRef id_did As UInt32) As Boolean
        Dim BufferOut(11) As Byte
        Try
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor)
            Dim usbSetupPacket As New UsbSetupPacket(usbflag, EXPIOREQ_RDID, CUShort(0), CUShort(0), CShort(BufferOut.Length))
            If Not FCUSB.ControlTransfer(usbSetupPacket, BufferOut, BufferOut.Length, Nothing) Then Return False
            Array.Reverse(BufferOut)
            Dim Match As Boolean = True
            For i = 0 To 5
                If BufferOut(i) <> BufferOut(i + 6) Then Match = False
            Next
            If Match Then Return False 'Read the same BEFORE and AFTER read chip id 
            If BufferOut(0) = 0 AndAlso BufferOut(2) = 0 Then Return False '0x0000
            If BufferOut(0) = &H90 AndAlso BufferOut(2) = &H90 Then Return False '0x9090 
            If BufferOut(0) = &H90 AndAlso BufferOut(2) = 0 Then Return False '0x9000 
            If BufferOut(0) = &HFF AndAlso BufferOut(2) = &HFF Then Return False '0xFFFF 
            If BufferOut(0) = &HFF AndAlso BufferOut(2) = 0 Then Return False '0xFF00
            id_mfg = BufferOut(0)
            If mode = AdatperType.NAND Then
                id_did = (CUInt(BufferOut(2)) << 24) Or (CUInt(BufferOut(3)) << 16) Or (CUInt(BufferOut(4)) << 8) Or (CUInt(BufferOut(5)))
            Else
                id_did = (CUInt(BufferOut(1)) << 24) Or (CUInt(BufferOut(2)) << 16) Or (CUInt(BufferOut(3)) << 8) Or (CUInt(BufferOut(4)))
            End If
            Return True
        Catch ex As Exception
        End Try
        Return False
    End Function

    Private Function EXPIO_SETUP_USB(ByVal mode As EXPIO_Mode) As Boolean
        Try
            If FCUSB Is Nothing Then Return False
            Dim data(0) As Byte
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_INIT, CUShort(mode), CUShort(0), CShort(4))
            Dim Result As Boolean = FCUSB.ControlTransfer(usbPacket2, data, 1, ret)
            If Not Result Then Return False
            If (data(0) <> &H17) Then Return False 'Checks the flag for MCP23S17
            Threading.Thread.Sleep(100) 'Give the USB time to change modes
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function Sector_Erase(ByVal sector_index As UInt32, Optional ByVal memory_area As FlashArea = FlashArea.Main) As Boolean
        Try
            If FCUSB Is Nothing Then Return False
            If Not MyFlashDevice.ERASE_REQUIRED Then Return True
            If (MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND) Then
                Dim Logical_Address As UInt32 = (DirectCast(MyFlashDevice, NAND_Flash).BLOCK_SIZE * sector_index)
                Dim physical As UInt32 = NAND_GetAddressMapping(Logical_Address)
                Return NAND_ERASEBLOCK(physical, memory_area, MySettings.NAND_Preserve)
            Else
                If sector_index = 0 AndAlso DirectCast(MyFlashDevice, MFP_Flash).GetSectorSize(0) = MyFlashDevice.FLASH_SIZE Then
                    Return EraseChip() 'Single sector, must do a full chip erase instead
                Else
                    Dim Logical_Address As UInt32 = 0
                    If (sector_index > 0) Then
                        For i As UInt32 = 0 To sector_index - 1
                            Dim SectorSize As Integer = DirectCast(MyFlashDevice, MFP_Flash).GetSectorSize(i)
                            Logical_Address += SectorSize
                        Next
                    End If
                    EXPIO_VPP_START() 'Enables +12V for supported devices
                    Dim Result As Boolean = False
                    Try
                        Dim setup() As Byte = GetSetupPacket(Logical_Address, 0, 0, 0, FlashArea.Main)
                        Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                        Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_SECTORERASE, 0, 0, CShort(setup.Length))
                        Result = FCUSB.ControlTransfer(usbPacket2, setup, setup.Length, 0)
                    Catch ex As Exception
                    End Try
                    EXPIO_VPP_STOP()
                    If Not Result Then Return False
                    Utilities.Sleep(100)
                    If DirectCast(MyFlashDevice, MFP_Flash).RESET_REQUIRED Then ResetDevice()
                    Return BlankCheck(Logical_Address)
                End If
            End If
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function ReadData(ByVal logical_address As UInt32, ByVal Length As UInt32, Optional ByVal memory_area As FlashArea = FlashArea.Main) As Byte()
        If MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
            If (memory_area = FlashArea.OOB) Then 'We need to adjust the logical address to point to the main area address
                Dim ext_page_size As Integer = DirectCast(MyFlashDevice, NAND_Flash).PAGE_SIZE_EXTENDED - MyFlashDevice.PAGE_SIZE
                Dim page_index As Long = Math.Floor(CDbl(logical_address) / CDbl(ext_page_size))
                Dim page_offset As Long = logical_address - (page_index * ext_page_size)
                logical_address = (page_index * CLng(MyFlashDevice.PAGE_SIZE)) + page_offset
            End If
            logical_address = NAND_GetAddressMapping(logical_address)
        End If
        Return ReadBulk(logical_address, Length, memory_area)
    End Function

    Public Function ReadBulk(ByVal page_address As UInt32, ByVal count As UInt32, Optional ByVal memory_area As FlashArea = FlashArea.Main) As Byte()
        If MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
            Return NAND_ReadBulk(page_address, count, memory_area)
        Else
            Return EXPIO_ReadBulk(page_address, count)
        End If
    End Function

    Public Function EraseChip() As Boolean
        Try
            If FCUSB Is Nothing Then Return False
            If MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
                NAND_EraseChip()
            Else
                Try
                    Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                    EXPIO_VPP_START()
                    Dim wm As MFP_PROG = DirectCast(MyFlashDevice, MFP_Flash).WriteMode
                    If wm = MFP_PROG.IntelSharp Or wm = MFP_PROG.IntelBuffer Then
                        Dim BlockCount As Integer = DirectCast(MyFlashDevice, MFP_Flash).SECTOR_COUNT
                        SetProgress(0)
                        For i = 0 To BlockCount - 1
                            If (Not Sector_Erase(i, 0)) Then
                                SetProgress(0)
                                Return False 'Error erasing sector
                            Else
                                Dim percent As Single = (i / BlockCount) * 100
                                SetProgress(Math.Floor(percent))
                            End If
                        Next
                        SetProgress(0)
                        Return True 'Device successfully erased
                    Else
                        Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_CHIPERASE, 0, 0, 0)
                        Dim CMD_RES As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, 0)
                        Utilities.Sleep(200) 'Perform blank check
                        If DirectCast(MyFlashDevice, MFP_Flash).RESET_REQUIRED Then ResetDevice()
                        For i = 0 To 179 '3 minutes
                            If BlankCheck(0) Then Return True
                            Utilities.Sleep(900)
                        Next
                        Return False 'Timeout (device erase failed)
                    End If
                Catch ex As Exception
                Finally
                    EXPIO_VPP_STOP()
                End Try
            End If
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Function ResetDevice() As Boolean
        Try
            If FCUSB Is Nothing Then Return False
            If MyFlashDevice.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
                Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_RESET, CUShort(0), CUShort(0), CShort(0))
                Dim result As Boolean = FCUSB.ControlTransfer(usbPacket2, Nothing, 0, 0)
                Utilities.Sleep(50)
                Return result
            Else
                Return True 'Device does not have RESET mode
            End If
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Sub WaitForReady()
        If MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
            Utilities.Sleep(10) 'Checks READ/BUSY# pin
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbSetupPacket As New UsbSetupPacket(usbflag, EXPIOREQ_NAND_WAIT, 0, 0, 0)
            FCUSB.ControlTransfer(usbSetupPacket, Nothing, 0, Nothing)
        ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
            Utilities.Sleep(100) 'Some flash devices have registers, some rely on delays
        End If
    End Sub

    Public Function WriteData(ByVal logical_address As UInt32, ByVal data_out() As Byte, Optional ByVal memory_area As FlashArea = FlashArea.Main) As Boolean
        If (MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND) Then
            If memory_area = 1 Then 'We need to adjust the logical address to point to the main area address
                Dim ext_page_size As Integer = DirectCast(MyFlashDevice, NAND_Flash).PAGE_SIZE_EXTENDED - MyFlashDevice.PAGE_SIZE
                Dim page_index As UInt32 = Math.Floor(CDbl(logical_address) / CDbl(ext_page_size))
                Dim page_offset As UInt32 = logical_address - (page_index * ext_page_size)
                logical_address = (page_index * MyFlashDevice.PAGE_SIZE) + page_offset
            End If
            Dim physical As Long = NAND_GetAddressMapping(logical_address)
            Return NAND_WRITEPAGE(physical, data_out, memory_area) 'We will write the whole block instead
        Else
            Try
                If MySettings.VPP_VCC = FlashcatSettings.VPP_SETTING.Write_12v Then EXPIO_CHIPENABLE_LOW() 'VPP=12V
                Dim data_left As UInt32 = data_out.Length
                Dim pointer As UInt32 = 0
                Do While (data_left > 0)
                    Dim packet_size As UInt32 = Math.Min(Kb256, data_left) '32KB packets 
                    Dim packet_data(packet_size - 1) As Byte
                    Array.Copy(data_out, pointer, packet_data, 0, packet_data.Length)
                    Dim ReturnValue As Boolean = EXPIO_WriteBulk(logical_address, packet_data)
                    If Not ReturnValue Then Return False
                    Utilities.Sleep(2) 'Delay for 2ms to allow any previous operation to complete
                    logical_address += packet_size
                    pointer += packet_size
                    data_left = data_left - packet_size
                Loop
                Utilities.Sleep(DirectCast(MyFlashDevice, MFP_Flash).WRITE_SOFTWARE_DELAY)
            Catch ex As Exception
            Finally
                If MySettings.VPP_VCC = FlashcatSettings.VPP_SETTING.Write_12v Then EXPIO_CHIPENABLE_HIGH() 'VPP=5V
            End Try
            If DirectCast(MyFlashDevice, MFP_Flash).RESET_REQUIRED Then ResetDevice()
            Return True
        End If
        Return False
    End Function

    Private Function EXPIO_WriteBulk(ByVal address As UInt32, ByVal data_out() As Byte) As Boolean
        Try
            Dim NORFLASH As MFP_Flash = DirectCast(MyFlashDevice, MFP_Flash)
            Dim setup() As Byte = GetSetupPacket(address, data_out.Length, 0, NORFLASH.PAGE_SIZE, FlashArea.Main)
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_WRITEDATA, 0, 0, CShort(setup.Length))
            Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, setup, setup.Length, Nothing)
            If Not res Then Return False
            Dim writer As UsbEndpointWriter = FCUSB.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
            Dim ec As ErrorCode = writer.Write(data_out, 0, data_out.Length, USB_TIMEOUT, Nothing)
            If (ec = ErrorCode.None) Then Return True 'Successful
            Return False 'Error
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_ReadBulk(ByVal address As UInt32, ByVal count As UInt32) As Byte()
        Try
            Dim setup() As Byte = GetSetupPacket(address, count, 0, 0, FlashArea.Main)
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbpacket1 As New UsbSetupPacket(usbflag, EXPIOREQ_READDATA, 0, 0, CShort(setup.Length))
            Dim xfer_count As Integer = 0
            Dim xfer_result As Boolean = FCUSB.ControlTransfer(usbpacket1, setup, setup.Length, xfer_count)
            Dim data_out(count - 1) As Byte
            Dim reader As UsbEndpointReader = FCUSB.OpenEndpointReader(ReadEndpointID.Ep01, data_out.Length, EndpointType.Bulk)
            Dim ec As ErrorCode = reader.Read(data_out, 0, CInt(data_out.Length), USB_TIMEOUT, Nothing)
            If (Not ec = ErrorCode.None) Then Return Nothing
            Return data_out
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
                Dim w() As Byte = ReadData(base_addr, 4, FlashArea.Main)
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
    Public NAND_MAP As New List(Of MAPPING)
    Public NAND_AVAILABLE_DATA As UInt32 'Number of bytes available to the current MAP

    Public Enum NAND_BLOCK_STATUS
        Valid 'Marked valid by the manager or by the user
        Bad_Manager 'This block was marked bad because of the manager setting
        Bad_Marked 'This block was marked bad because of the user
        Bad_ByError 'This block was marked bad because of programming
        Unknown
    End Enum

    Public Class MAPPING
        Public Status As NAND_BLOCK_STATUS
        Public Physical_Start As UInt32 'Address of this block physically
        Public Logical_Start As UInt32 'Address of this block that is mapped
    End Class
    'Called once on device init to create a map of all blocks
    Public Sub CreateNandMap()
        NAND_MAP.Clear()
        Dim BaseAddress As UInt32 = 0
        Dim BlockSize As Integer = DirectCast(MyFlashDevice, NAND_Flash).BLOCK_SIZE
        Dim PageSize As UInt32 = DirectCast(MyFlashDevice, NAND_Flash).PAGE_SIZE
        Dim BlockCount As Integer = MyFlashDevice.FLASH_SIZE / BlockSize
        For i As UInt32 = 0 To BlockCount - 1
            Dim block_info As New MAPPING
            block_info.Status = NAND_BLOCK_STATUS.Valid
            block_info.Physical_Start = BaseAddress
            block_info.Logical_Start = BaseAddress
            NAND_MAP.Add(block_info)
            BaseAddress += BlockSize
        Next
    End Sub

    Public Sub ManagerNandMap()
        If MySettings.NAND_BadBlockManager = FlashcatSettings.BadBlockMarker.Disabled Then
            RaiseEvent PrintConsole("NAND block manager disabled")
            Exit Sub
        Else
            SetStatus("NAND memory device detected, loading valid memory map")
            RaiseEvent PrintConsole("Loading NAND memory map for valid memory")
        End If
        Dim BlockSize As Integer = DirectCast(MyFlashDevice, NAND_Flash).BLOCK_SIZE
        Dim PageSize As UInt32 = DirectCast(MyFlashDevice, NAND_Flash).PAGE_SIZE
        Dim BlockCount As Integer = MyFlashDevice.FLASH_SIZE / BlockSize
        Dim PagesPerBlock As UInt32 = BlockSize / PageSize
        Dim BaseAddress As UInt32 = 0
        For i As UInt32 = 0 To BlockCount - 1
            Dim BAD_BLOCK As Boolean = False
            Select Case MySettings.NAND_BadBlockManager
                Case FlashcatSettings.BadBlockMarker.SixthByte_FirstPage 'SLC-small page
                    Dim e() As Byte = NAND_ReadBulk(BaseAddress, 16, FlashArea.OOB)
                    If Not (e(5) = 255) Then BAD_BLOCK = True
                Case FlashcatSettings.BadBlockMarker.FirstSixthByte_FirstPage 'SLC-large page
                    Dim e() As Byte = NAND_ReadBulk(BaseAddress, 16, FlashArea.OOB)
                    If (Not e(0) = 255) AndAlso (Not e(0) = 255) Then BAD_BLOCK = True
                Case FlashcatSettings.BadBlockMarker.FirstByte_LastPage 'MLC
                    Dim LastPageAddr As UInt32 = BaseAddress + (PagesPerBlock - 1) * PageSize
                    Dim e() As Byte = NAND_ReadBulk(LastPageAddr, 16, FlashArea.OOB) 'The last page of this block
                    If (Not e(0) = 255) Then BAD_BLOCK = True
            End Select
            If BAD_BLOCK Then
                Application.DoEvents()
                WriteConsole("BAD NAND BLOCK AT address: 0x" & Hex(BaseAddress).PadLeft(8, "0"))
                NAND_MAP(i).Status = NAND_BLOCK_STATUS.Bad_Manager
            Else
                NAND_MAP(i).Status = NAND_BLOCK_STATUS.Valid
            End If
            BaseAddress += BlockSize
        Next
    End Sub

    Public Sub ProcessNandMap()
        Dim BlockSize As UInt32 = DirectCast(MyFlashDevice, NAND_Flash).BLOCK_SIZE
        NAND_AVAILABLE_DATA = 0
        Dim LogicalPointer As UInt32 = 0
        For i As UInt32 = 0 To NAND_MAP.Count - 1
            If NAND_MAP(i).Status = NAND_BLOCK_STATUS.Valid Then
                NAND_MAP(i).Logical_Start = LogicalPointer
                LogicalPointer += BlockSize
                NAND_AVAILABLE_DATA += BlockSize
            Else
                NAND_MAP(i).Logical_Start = 0
            End If
        Next
        RaiseEvent PrintConsole("NAND memory map complete: " & Format(NAND_AVAILABLE_DATA, "#,###") & " bytes available for access")
    End Sub
    'Returns the physical address from the logical address
    Public Function NAND_GetAddressMapping(ByVal logical_address As UInt32) As UInt32
        Dim BlockSize As Integer = DirectCast(MyFlashDevice, NAND_Flash).BLOCK_SIZE
        Dim offset As UInt32 = (logical_address Mod BlockSize)
        Dim logical_Base As UInt32 = logical_address - offset
        Dim address As UInt32 = 0
        For i As UInt32 = 0 To NAND_MAP.Count - 1
            If NAND_MAP(i).Status = NAND_BLOCK_STATUS.Valid Then
                If logical_Base = NAND_MAP(i).Logical_Start Then
                    Return (NAND_MAP(i).Physical_Start + offset)
                End If
            End If
        Next
        Return 0 'NOT FOUND
    End Function

    Public Function NAND_ReadBulk(ByVal page_address As UInt32, ByVal count As UInt32, ByVal memory_area As FlashArea) As Byte()
        Try
            Dim page_size_ext As UInt16 = DirectCast(MyFlashDevice, NAND_Flash).PAGE_SIZE_EXTENDED - MyFlashDevice.PAGE_SIZE '16 bytes
            Dim page_size As UInt16 'Number of bytes to read before we change pages
            If memory_area = FlashArea.Main Then
                page_size = MyFlashDevice.PAGE_SIZE
            ElseIf memory_area = FlashArea.OOB Then
                page_size = page_size_ext
            End If
            Dim data_in() As Byte = GetSetupPacket(page_address, count, page_size, MyFlashDevice.PAGE_SIZE, memory_area)
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbpacket1 As New UsbSetupPacket(usbflag, EXPIOREQ_READDATA, 0, 0, CShort(data_in.Length))
            Dim cmd_result As Boolean = FCUSB.ControlTransfer(usbpacket1, data_in, data_in.Length, Nothing)
            If Not cmd_result Then
                Return Nothing 'Error
            End If
            Dim data_out(count - 1) As Byte
            Dim reader As UsbEndpointReader = FCUSB.OpenEndpointReader(ReadEndpointID.Ep01, data_out.Length, EndpointType.Bulk)
            Dim ec As ErrorCode = reader.Read(data_out, 0, CInt(data_out.Length), 5000, Nothing)
            If Not (ec = ErrorCode.None) Then Return Nothing
            Utilities.Sleep(2)
            'WaitForReady()
            Return data_out
        Catch ex As Exception
        End Try
        Return Nothing
    End Function
    Public Function NAND_ERASEBLOCK(ByVal page_address As UInt32, ByVal Memory_area As FlashArea, ByVal CopyExtenedArea As Boolean) As Boolean
        Dim BlockSize As Integer = DirectCast(MyFlashDevice, NAND_Flash).BLOCK_SIZE
        Dim page_count As Integer = BlockSize / MyFlashDevice.PAGE_SIZE 'number of pages per block
        Dim ext_size As Integer = DirectCast(MyFlashDevice, NAND_Flash).PAGE_SIZE_EXTENDED - MyFlashDevice.PAGE_SIZE 'i.e. 16
        Dim other_memory_area() As Byte = Nothing
        If CopyExtenedArea Then
            If Memory_area = FlashArea.Main Then
                other_memory_area = NAND_ReadBulk(page_address, page_count * ext_size, FlashArea.OOB) 'We want to read all of the extended area
            ElseIf Memory_area = FlashArea.OOB Then
                other_memory_area = NAND_ReadBulk(page_address, page_count * MyFlashDevice.PAGE_SIZE, FlashArea.Main)
            End If
        End If
        Try 'START BLOCK ERASE
            Dim data_in() As Byte = GetSetupPacket(page_address, 0, 0, 0, FlashArea.Main)
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_SECTORERASE, 0, 0, CShort(data_in.Length))
            Dim Result As Boolean = FCUSB.ControlTransfer(usbPacket2, data_in, data_in.Length, Nothing)
            If Not Result Then Return False
            If DirectCast(MyFlashDevice, NAND_Flash).PAGE_SIZE = 512 Then 'LEGACY NAND DEVICE
                Utilities.Sleep(250) 'Micron NAND legacy delay (was 200)
            Else
                Utilities.Sleep(50) 'Normal delay
            End If
            WaitForReady()
        Catch ex As Exception
        End Try  'BLOCK ERASE COMPLETE
        If CopyExtenedArea Then
            If other_memory_area Is Nothing Then Return False 'Error
            If Memory_area = FlashArea.Main Then
                NAND_WRITEPAGE(page_address, other_memory_area, FlashArea.OOB) 'We want to write back the origional ext area data
            ElseIf Memory_area = FlashArea.OOB Then
                NAND_WRITEPAGE(page_address, other_memory_area, FlashArea.Main) 'We want to write back the origional ext area data
            End If
        End If
        Return True
    End Function
    'This writes the data to a page
    Public Function NAND_WRITEPAGE(ByVal page_address As UInt32, ByVal data_out() As Byte, ByVal area As FlashArea) As Boolean
        Try
            Dim page_size_ext As UInt16 = (DirectCast(MyFlashDevice, NAND_Flash).PAGE_SIZE_EXTENDED - MyFlashDevice.PAGE_SIZE) '16 bytes
            Dim page_size As UInt16 = 0 'Number of bytes to read before we change pages
            If area = FlashArea.Main Then
                page_size = MyFlashDevice.PAGE_SIZE
            ElseIf area = FlashArea.OOB Then
                page_size = page_size_ext
            End If
            Dim data_in() As Byte = GetSetupPacket(page_address, data_out.Length, page_size, MyFlashDevice.PAGE_SIZE, area)
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EXPIOREQ_WRITEDATA, 0, 0, CShort(data_in.Length))
            Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, data_in, data_in.Length, Nothing)
            If Not res Then Return False
            Dim writer As UsbEndpointWriter = FCUSB.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
            Dim ec As ErrorCode = writer.Write(data_out, 0, data_out.Length, 5000, Nothing) 'DEBUG
            If Not (ec = ErrorCode.None) Then Return False 'Error
            If DirectCast(MyFlashDevice, NAND_Flash).PAGE_SIZE = 512 Then 'LEGACY NAND DEVICE
                Utilities.Sleep(100) 'Was 100
            Else
                Utilities.Sleep(10)
            End If
            WaitForReady() 'PB5 signals busy/ready
            Return True
        Catch ex As Exception
            Return False
        End Try
        Return False
    End Function
    'Returns the total space of the extra data
    Public Function NAND_Extra_GetSize() As Long
        Dim PageCount As Long = MyFlashDevice.FLASH_SIZE / MyFlashDevice.PAGE_SIZE 'Total number of pages in this device
        Dim ExtPageSize As Long = DirectCast(MyFlashDevice, NAND_Flash).PAGE_SIZE_EXTENDED - MyFlashDevice.PAGE_SIZE
        Dim BlockCount As Long = MyFlashDevice.FLASH_SIZE / DirectCast(MyFlashDevice, NAND_Flash).BLOCK_SIZE
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

    Public Function NAND_EraseChip() As Boolean
        Dim BlockSize As Integer = DirectCast(MyFlashDevice, NAND_Flash).BLOCK_SIZE
        Dim BlockCount As Integer = MyFlashDevice.FLASH_SIZE / BlockSize
        Dim BaseAddress As UInt32 = 0
        For i = 0 To BlockCount - 1
            NAND_ERASEBLOCK(BaseAddress, FlashArea.Main, MySettings.NAND_Preserve)
            BaseAddress += BlockSize
            If i Mod 10 = 0 Then
                Dim Percent As Integer = Math.Round((i / BlockCount) * 100)
                SetProgress(Percent)
            End If
        Next
        SetProgress(0)
        Return True
    End Function

#End Region

    Private Function GetSetupPacket(ByVal Address As UInt32, ByVal Count As UInt32, ByVal AreaSize As UInt16, ByVal PageSize As UInt16, ByVal area As FlashArea) As Byte()
        Dim addr_bytes As Byte = 0
        Dim TX_OFFSET As UInt16 = 0 'Specifies an offset in the current page to begin writing
        If (AreaSize > 0) Then TX_OFFSET = (Address Mod AreaSize) 'Where is the page to begin writing
        If (MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND) Then
            Dim NAND_DEV As NAND_Flash = DirectCast(MyFlashDevice, NAND_Flash)
            Select Case NAND_DEV.PAGE_SIZE
                Case 512 'LEGACY ONLY
                    If (MyFlashDevice.FLASH_SIZE > &HFFFFFFUI) Then
                        addr_bytes = 4
                    Else
                        addr_bytes = 3
                    End If
                Case 2048
                    Dim flash_bits As Integer = Utilities.BitSize(NAND_DEV.FLASH_SIZE - 1) 'Number of bits (without OOB) this device has
                    addr_bytes = 2 + Math.Ceiling((flash_bits - 11) / 8) '11 - ROW OFFSET VALUE
                Case 4096
                    Dim flash_bits As Integer = Utilities.BitSize(NAND_DEV.FLASH_SIZE - 1) 'Number of bits (without OOB) this device has
                    addr_bytes = 2 + Math.Ceiling((flash_bits - 12) / 8) '12 - ROW OFFSET VALUE
                Case 8192
                    Dim flash_bits As Integer = Utilities.BitSize(NAND_DEV.FLASH_SIZE - 1) 'Number of bits (without OOB) this device has
                    addr_bytes = 2 + Math.Ceiling((flash_bits - 13) / 8) '13 - ROW OFFSET VALUE
            End Select
        End If
        Dim data_in(15) As Byte
        data_in(0) = CByte(Address And 255)
        data_in(1) = CByte((Address >> 8) And 255)
        data_in(2) = CByte((Address >> 16) And 255)
        data_in(3) = CByte((Address >> 24) And 255)
        data_in(4) = CByte(Count And 255)
        data_in(5) = CByte((Count >> 8) And 255)
        data_in(6) = CByte((Count >> 16) And 255)
        data_in(7) = CByte((Count >> 24) And 255)
        data_in(8) = CByte(AreaSize And 255)
        data_in(9) = CByte((AreaSize >> 8) And 255)
        data_in(10) = CByte(PageSize And 255) 'This is how many bytes to increment between operations
        data_in(11) = CByte((PageSize >> 8) And 255)
        data_in(12) = area 'Where to read data from
        data_in(13) = addr_bytes
        data_in(14) = CByte(TX_OFFSET And 255)
        data_in(15) = CByte((TX_OFFSET >> 8) And 255)
        Return data_in
    End Function



End Class
