'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2017 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: This is the main module that is loaded first.

Imports FlashcatUSB.SPI
Imports System.Runtime.Remoting
Imports System.Runtime.Remoting.Channels
Imports System.Runtime.Remoting.Channels.Ipc
Imports System.Threading
Imports System.Runtime.InteropServices
Imports FlashcatUSB.FlashMemory

Public Module MainApp
    Public Property RM As Resources.ResourceManager = My.Resources.English.ResourceManager
    Public Const Build As Integer = 462
    Public Platform As String
    Public GUI As MainForm
    Public MySettings As FlashcatSettings
    Public ScriptPath As String = Application.StartupPath & "\Scripts\" 'Holds the full directory name of where scripts are located
    Public OperationMode As AvrMode = AvrMode.NotConnected
    Public MinBoardRev As Single = 4.25 'Min revision allowed for software
    Public AppIsClosing As Boolean = False
    Public FlashDatabase As New FlashDatabase 'This contains definitions of all of the supported Flash devices
    Public HWBOARD As HwVariant = HwVariant.Classic 'Byte used to specify a special operation only mode

    Public WithEvents ScriptEngine As New Script
    Public WithEvents LegacyNonVol As New BcmNonVol
    Public WithEvents EJ_IF As New EJTAG
    Public WithEvents DFU_IF As New DFU_API
    Public WithEvents SPI_IF As New SPI.FlashProgrammer
    Public WithEvents EXT_IF As New ExtPort

    Sub Main(ByVal Args() As String)
        'Args = {"-write", "-i2c", "24XX64", "-Offset", "0X2C", "-Length", "1", "-file", "d:\file.bin"}
        'Args = {"-read", "-i2c", "24XX64", "-length", "83", "-file", "d:\output.bin"}
        StartComServer() 'Allows our application to post messages between multiple instances
        I2CDatabaseInit() 'Adds the I2C flash devices
        Thread.CurrentThread.Name = "rootApp"
        Platform = My.Computer.Info.OSFullName & " (" & GetOsBitsString() & ")"
        MySettings = New FlashcatSettings
        If Args IsNot Nothing AndAlso Args.Count > 0 Then 'We are running as CONSOLE
            RunConsoleMode(Args)
        Else 'We are running normal GUI
            AddHandler SPI_IF.PrintConsole, AddressOf WriteConsole 'Lets set text output to the console
            AddHandler EXT_IF.PrintConsole, AddressOf WriteConsole
            GUI = New MainForm
            Dim t As New Thread(AddressOf BeginUsbCheckLoop)
            t.Start()
            Application.Run(GUI)
        End If
        AppClosing()
    End Sub

    Public Class FlashcatSettings
        Public Property LanguageName As String
        Public Property VERIFY_WRITE As Boolean = False 'Holds the verify data flag
        Public Property OPERATION_MODE As DeviceMode = DeviceMode.SPI
        Public Property SPI_EEPROM As SPI_EEPROM_DEVICE
        Public Property BIT_ENDIAN As BitEndianMode = BitEndianMode.BigEndian 'Mirrors bits
        Public Property BIT_SWAP As BitSwapMode = BitSwapMode.None 'Swaps nibbles/bytes/words
        Public Property NAND_Preserve As Boolean = True 'We want to copy SPARE data before erase
        Public Property NAND_BadBlockManager As BadBlockMarker 'Indicates how BAD BLOCKS are detected
        Public Property EXTIO_VPP As SO44_VPP_SETTING = SO44_VPP_SETTING.Disabled 'Enables the SO-44 Adapter's 12v VPP feature

        Sub New()
            VERIFY_WRITE = GetRegistryValue("VerifyData", True)
            NAND_BadBlockManager = GetRegistryValue("NAND_BadBlock", BadBlockMarker.Disabled)
            NAND_Preserve = GetRegistryValue("NandPreserve", True)
            OPERATION_MODE = CInt(GetRegistryValue("OpMode", "1")) 'Default is normal
            EXTIO_VPP = CInt(GetRegistryValue("VPP", "1"))
            SPI_EEPROM = GetRegistryValue("SPI_EEPROM", SPI_EEPROM_DEVICE.None)
            LoadLanguageSettings()
        End Sub

        Public Sub Save()
            Dim i_mode As Integer = OPERATION_MODE
            SetRegistryValue("VerifyData", VERIFY_WRITE)
            SetRegistryValue("NAND_BadBlock", NAND_BadBlockManager)
            SetRegistryValue("NandPreserve", NAND_Preserve)
            SetRegistryValue("OpMode", CInt(OPERATION_MODE).ToString)
            SetRegistryValue("VPP", CInt(EXTIO_VPP).ToString)
            SetRegistryValue("SPI_EEPROM", SPI_EEPROM)
            SetRegistryValue("Language", LanguageName)
        End Sub

        Public Enum SO44_VPP_SETTING As Integer
            Disabled = 1 'Do not use
            Write_12v = 2 'Erase and write will enable 12v VPP
        End Enum

        Public Enum BadBlockMarker As Integer
            Disabled = 1
            SixthByte_FirstPage = 2
            FirstSixthByte_FirstPage = 3
            FirstByte_LastPage = 4
        End Enum

        Public Enum DeviceMode As Byte
            SPI = 1
            JTAG = 2
            I2C_EEPROM = 3
            SPI_EEPROM = 4
            EXTIO = 5
        End Enum

        Public Enum SPI_EEPROM_DEVICE As Integer
            None 'User must select SPI EEPROM device
            nRF24LE1 = 1 '16384 bytes
            nRF24LUIP_16KB  '16384 bytes
            nRF24LUIP_32KB '32768 bytes
            AT25010A  '128 bytes
            AT25020A  '256 bytes
            AT25040A  '512 bytes
            AT25080  '1024 bytes
            AT25160  '2048 bytes
            AT25320  '4096 bytes
            AT25640  '8192 bytes
            AT25128B  '16384 bytes
            AT25256B  '32768 bytes
            AT25512  '65536 bytes
            M95010   '128 bytes
            M95020  '256 bytes
            M95040  '512 bytes
            M95080  '1024 bytes
            M95160  '2048 bytes
            M95320  '4096 bytes
            M95640   '8192 bytes
            M95128  '16384 bytes
            M95256  '32768 bytes
            M95512  '65536 bytes
            M95M01  '131072 bytes
            M95M02  '262144 bytes
            M25AA512  'Microchip 64 bytes
            M25AA160A  '2048 bytes
            M25AA160B  '2048 bytes
        End Enum

        Private Sub LoadLanguageSettings()
            Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\EmbComputers\FlashcatUSB\")
            If key Is Nothing Then
                LoadDefaultLanguage()
            Else
                Dim o As Object = key.GetValue("Language")
                If o IsNot Nothing Then
                    Dim SelLangauge As String = CStr(o)
                    Select Case SelLangauge.ToUpper
                        Case "ENGLISH"
                            RM = My.Resources.English.ResourceManager : LanguageName = "English"
                        Case "SPANISH"
                            RM = My.Resources.Spanish.ResourceManager : LanguageName = "Spanish"
                        Case "FRENCH"
                            RM = My.Resources.French.ResourceManager : LanguageName = "French"
                        Case "GERMAN"
                            RM = My.Resources.German.ResourceManager : LanguageName = "German"
                        Case "PORTUGUESE"
                            RM = My.Resources.Portuguese.ResourceManager : LanguageName = "Portuguese"
                        Case "CHINESE"
                            RM = My.Resources.Chinese.ResourceManager : LanguageName = "Chinese"
                        Case Else
                            RM = My.Resources.English.ResourceManager : LanguageName = "English"
                    End Select
                Else
                    LoadDefaultLanguage()
                End If
            End If
        End Sub

        Private Sub LoadDefaultLanguage()
            'http://www1.cs.columbia.edu/~lok/csharp/refdocs/System.Globalization/types/CultureInfo.html
            Try
                Dim strLanguage As String = System.Globalization.CultureInfo.CurrentCulture.ToString.ToUpper.Substring(0, 2)
                Select Case strLanguage
                    Case "EN"
                        RM = My.Resources.English.ResourceManager : Me.LanguageName = "English"
                    Case "ES"
                        RM = My.Resources.Spanish.ResourceManager : Me.LanguageName = "Spanish"
                    Case "PT"
                        RM = My.Resources.Portuguese.ResourceManager : Me.LanguageName = "Portuguese"
                    Case "FR"
                        RM = My.Resources.Spanish.ResourceManager : Me.LanguageName = "French"
                    Case "DE"
                        RM = My.Resources.German.ResourceManager : Me.LanguageName = "German"
                    Case "ZH"
                        RM = My.Resources.Chinese.ResourceManager : Me.LanguageName = "Chinese"
                    Case Else
                        RM = My.Resources.English.ResourceManager : Me.LanguageName = "English"
                End Select
            Catch ex As Exception
                Me.LanguageName = "English"
            End Try
        End Sub

#Region "Registry"
        Private Const REGKEY As String = "Software\EmbComputers\FlashcatUSB\"

        Public Function GetRegistryValue(ByVal Name As String, ByVal DefaultValue As String) As String
            Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY)
            If key Is Nothing Then Return DefaultValue
            Dim o As Object = key.GetValue(Name)
            If o Is Nothing Then Return DefaultValue
            Return CStr(o)
        End Function

        Public Function SetRegistryValue(ByVal Name As String, ByVal Value As String) As Boolean
            Try
                Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY, Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree)
                key.SetValue(Name, Value)
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function GetRegistryValue(ByVal Name As String, ByVal DefaultValue As Integer) As Integer
            Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY)
            If key Is Nothing Then Return DefaultValue
            Dim o As Object = key.GetValue(Name)
            If o Is Nothing Then Return DefaultValue
            Return CInt(o)
        End Function

        Public Function SetRegistryValue(ByVal Name As String, ByVal Value As Integer) As Boolean
            Try
                Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY, Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree)
                key.SetValue(Name, Value)
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function GetRegistryValue(ByVal Name As String, ByVal DefaultValue As Boolean) As Boolean
            Try
                Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY)
                If key Is Nothing Then Return DefaultValue
                Dim o As Object = key.GetValue(Name)
                If o Is Nothing Then Return DefaultValue
                Return CBool(o)
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function SetRegistryValue(ByVal Name As String, ByVal Value As Boolean) As Boolean
            Try
                Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY, Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree)
                If key Is Nothing Then
                    Microsoft.Win32.Registry.CurrentUser.CreateSubKey(REGKEY)
                    key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY, True)
                End If
                key.SetValue(Name, Value)
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

#End Region

    End Class

    Public Enum AvrMode
        NotConnected = 0
        JTAG = 1
        SPI = 2 'SPI mode
        I2C = 3 'I2C EEPROM (24 series)
        EXPIO = 4 'Extention IO mode
        DFU = 5 'Atmel Bootloader mode
    End Enum

    Public Enum HwVariant As Byte
        Classic = 0 'Standard PCB 2.x
        xPort = CByte(Asc("E")) 'xPORT PCB 2.x (extension IO only)
    End Enum

    Public Class WriteParameters
        Public Address As UInt32 = 0 'flash address to write to
        Public Count As UInt32 = 0 'Number of bytes to write from this stream
        Public UpdateProgress As [Delegate] 'This is used to update a progress bar
        Public UpdateSpeed As [Delegate] 'This is used to update a speed text label
        Public DisplayStatus As Boolean 'Indicates we want to update the status label
        Public Memory_Area As FlashArea = FlashArea.Main 'Indicates the sub area we want to write to
        Public Timer As Stopwatch 'To monitor the transfer speed
        'Write Specific Parameters:
        Public EraseSector As Boolean = True  'True if we want to erase each sector prior to write
        Public Verify As Boolean = True 'True if we want to read back the data
        Public AbortOperation As Boolean = False
    End Class

    Public Class ReadParameters
        Public Address As UInt32 = 0
        Public Count As UInt32 = 0
        Public UpdateProgress As [Delegate] 'This is used to update a progress bar
        Public UpdateSpeed As [Delegate] 'This is used to update a speed text label
        Public DisplayStatus As Boolean 'Indicates we want to update the status label
        Public Memory_Area As FlashArea = FlashArea.Main 'Indicates the sub area we want to read
        Public Timer As Stopwatch 'To monitor the transfer speed
        Public AbortOperation As Boolean = False
    End Class


#Region "Multi-Channel USB Tasking"
    Public IpcChannel As String 'The channel we are listening on
    Public MyUSBDeviceID As String = "" 'When connected, this shows the device id string

    Public Function GetFlashcatInUse() As String()
        Dim InUseIds As New List(Of String)
        Dim RemoteChannelName As String 'FlashcatCOMx
        For i = 1 To 16
            RemoteChannelName = "FlashcatCOM" & i.ToString
            Try
                Dim obj As ICommunicationService = DirectCast(Activator.GetObject(GetType(ICommunicationService), "ipc://" & RemoteChannelName & "/SreeniRemoteObj"), ICommunicationService)
                Dim device_id As String = obj.GetUsbDeviceID
                If Not device_id = "" Then InUseIds.Add(device_id)
            Catch ex As Exception
            End Try
        Next
        If InUseIds.Count = 0 Then Return Nothing
        Return InUseIds.ToArray
    End Function

    Public Interface ICommunicationService
        Function GetUsbDeviceID() As String

    End Interface

    Public Class CommunicationService : Inherits MarshalByRefObject : Implements ICommunicationService

        Public Function GetUsbDeviceID() As String Implements ICommunicationService.GetUsbDeviceID
            Return MyUSBDeviceID
        End Function

    End Class

    Public Sub StartComServer()
        Dim ChannelName As String = "FlashcatCOM"
        Dim ServerStarted As Boolean = False
        Dim InstanceCounter As Integer = 0
        Do Until ServerStarted
            InstanceCounter += 1
            If InstanceCounter = 17 Then Exit Sub 'We only want to allow up to 16 devices on one machine
            Try
                IpcChannel = ChannelName & InstanceCounter.ToString
                Dim ipcCh As New IpcChannel(IpcChannel)
                ChannelServices.RegisterChannel(ipcCh, False)
                RemotingConfiguration.RegisterWellKnownServiceType(GetType(CommunicationService), "SreeniRemoteObj", WellKnownObjectMode.Singleton)
                ServerStarted = True
            Catch ex As Exception
            End Try
        Loop
    End Sub

#End Region

#Region "SPI Helper"
    Private ScanInProgress As Boolean = False
    Private ScanThread As Threading.Thread
    Public SPI_UseCustom As Boolean = False
    Public SPICUSTOM_SIZE As UInt32 = Mb001
    Public SPICUSTOM_PAGESIZE As UInt32 = 256
    Public SPICUSTOM_ADDRESSBITS As UInt32 = 24
    Public SPICUSTOM_SECTORSIZE As UInt32
    Public SPICUSTOM_MODE As SPI_ProgramMode = SPI_ProgramMode.PageMode
    Public SPICUSTOM_EWSR As Byte
    Public SPICUSTOM_WRSR As Byte
    Public SPICUSTOM_PROG As Byte
    Public SPICUSTOM_WREN As Byte
    Public SPICUSTOM_RDSR As Byte
    Public SPICUSTOM_READ As Byte
    Public SPICUSTOM_SE As Byte
    Public SPICUSTOM_BE As Byte
    Public SPICUSTOM_4BYTE As Boolean = False

    Public Sub SPI_Detect()
        ScanInProgress = False
        If MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI Then
            If SPI_UseCustom Then
                Dim NewCustomDevice As New SPI_FLASH("SPI_SPECIFIC", SPICUSTOM_SIZE, SPICUSTOM_PAGESIZE)
                NewCustomDevice.ADDRESSBITS = SPICUSTOM_ADDRESSBITS
                NewCustomDevice.ERASE_SIZE = SPICUSTOM_SECTORSIZE
                NewCustomDevice.ProgramMode = SPICUSTOM_MODE
                NewCustomDevice.SEND_4BYTE = SPICUSTOM_4BYTE
                NewCustomDevice.OP_COMMANDS.EWSR = SPICUSTOM_EWSR
                NewCustomDevice.OP_COMMANDS.WRSR = SPICUSTOM_WRSR
                NewCustomDevice.OP_COMMANDS.PROG = SPICUSTOM_PROG
                NewCustomDevice.OP_COMMANDS.WREN = SPICUSTOM_WREN
                NewCustomDevice.OP_COMMANDS.RDSR = SPICUSTOM_RDSR
                NewCustomDevice.OP_COMMANDS.READ = SPICUSTOM_READ
                NewCustomDevice.OP_COMMANDS.SE = SPICUSTOM_SE
                NewCustomDevice.OP_COMMANDS.BE = SPICUSTOM_BE
                SPI_IF.MyFlashDevice = NewCustomDevice
                SPI_IF.MyFlashStatus = ConnectionStatus.Supported
            Else
                ScanInProgress = True
                ScanThread = New Threading.Thread(AddressOf SpiScanningThread)
                ScanThread.Name = "SpiDetect"
                ScanThread.IsBackground = True
                ScanThread.Start()
            End If
        ElseIf MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.EXTIO Then
            ScanInProgress = True
            ScanThread = New Threading.Thread(AddressOf SpiScanningThread)
            ScanThread.Name = "ExtIODetect"
            ScanThread.IsBackground = True
            ScanThread.Start()
        ElseIf MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI_EEPROM Then
            If Not MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.None Then
                SPIEEPROM_Configure()
                WriteConsole(RM.GetString("fcusb_spi_usingcustom"))
            End If
        End If
        Do While ScanInProgress
            Application.DoEvents()
            Utilities.Sleep(200)
        Loop
    End Sub

    Public Sub SpiScanningThread()
        Static IsDetecting As Boolean = False
        If IsDetecting Then Exit Sub 'We are already detecting
        Try
            IsDetecting = True
            SPI_IF.DeviceInit() 'SpiScanningThread
        Catch ex As Exception
        Finally
            IsDetecting = False
        End Try
        ScanInProgress = False
    End Sub

    Private Sub SPIEEPROM_Configure()
        Select Case MySettings.SPI_EEPROM
            Case FlashcatSettings.SPI_EEPROM_DEVICE.nRF24LE1  '16384 bytes
                SPI_IF.EnableProgMode()
                SPI_IF.SPIBUS_Setup(SPI_SPEED.MHZ_1)
                Dim nordic_spi As New SPI_FLASH("Nordic nRF24LE1", 16384, 512)
                nordic_spi.OP_COMMANDS.SE = &H52
                nordic_spi.ERASE_SIZE = 512
                nordic_spi.ADDRESSBITS = 16
                nordic_spi.ProgramMode = SPI_ProgramMode.Nordic
                SPI_IF.MyFlashDevice = nordic_spi
            Case FlashcatSettings.SPI_EEPROM_DEVICE.nRF24LUIP_16KB   '16384 bytes
                SPI_IF.EnableProgMode()
                SPI_IF.SPIBUS_Setup(SPI_SPEED.MHZ_1)
                Dim nordic_spi As New SPI_FLASH("Nordic nRF24LUI+ (16KB)", 16384, 256)
                nordic_spi.OP_COMMANDS.SE = &H52
                nordic_spi.ERASE_SIZE = 512
                nordic_spi.ADDRESSBITS = 16
                nordic_spi.ProgramMode = SPI_ProgramMode.Nordic
                SPI_IF.MyFlashDevice = nordic_spi
            Case FlashcatSettings.SPI_EEPROM_DEVICE.nRF24LUIP_32KB   '32768 bytes
                SPI_IF.EnableProgMode()
                SPI_IF.SPIBUS_Setup(SPI_SPEED.MHZ_1)
                Dim nordic_spi As New SPI_FLASH("Nordic nRF24LUI+ (32KB)", 32768, 256)
                nordic_spi.OP_COMMANDS.SE = &H52
                nordic_spi.ERASE_SIZE = 512
                nordic_spi.ADDRESSBITS = 16
                nordic_spi.ProgramMode = SPI_ProgramMode.Nordic
                SPI_IF.MyFlashDevice = nordic_spi
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25128B  '16384 bytes
                Dim AT25128B As New SPI_FLASH("Atmel AT25128B", 16384, 64)
                AT25128B.ADDRESSBITS = 16
                AT25128B.ERASE_REQUIRED = False 'We will not send erase commands
                AT25128B.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = AT25128B
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25256B   '32768 bytes
                Dim AT25256B As New SPI_FLASH("Atmel AT25256B", 32768, 64) '64 bytes per page/sector
                AT25256B.ADDRESSBITS = 16
                AT25256B.ERASE_REQUIRED = False 'We will not send erase commands
                AT25256B.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = AT25256B
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25512  '65536 bytes
                Dim AT25512 As New SPI_FLASH("Atmel AT25512", 65536, 128) '128 bytes per page/sector
                AT25512.ADDRESSBITS = 16
                AT25512.ERASE_REQUIRED = False 'We will not send erase commands
                AT25512.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = AT25512
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95010    '128 bytes
                Dim M95010 As New SPI_FLASH("ST M95010", 128, 16)
                M95010.ADDRESSBITS = 8
                M95010.ERASE_REQUIRED = False 'We will not send erase commands
                M95010.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = M95010
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95020   '256 bytes
                Dim M95020 As New SPI_FLASH("ST M95020", 256, 16)
                M95020.ADDRESSBITS = 8
                M95020.ERASE_REQUIRED = False 'We will not send erase commands
                M95020.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = M95020
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95040   '512 bytes
                Dim M95040 As New SPI_FLASH("ST M95040", 512, 16)
                M95040.ADDRESSBITS = 8
                M95040.ERASE_REQUIRED = False 'We will not send erase commands
                M95040.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = M95040
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95080   '1024 bytes
                Dim M95080 As New SPI_FLASH("ST M95080", 1024, 32)
                M95080.ADDRESSBITS = 16
                M95080.ERASE_REQUIRED = False 'We will not send erase commands
                M95080.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = M95080
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95160  '2048 bytes
                Dim M95160 As New SPI_FLASH("ST M95160", 2048, 32)
                M95160.ADDRESSBITS = 16
                M95160.ERASE_REQUIRED = False 'We will not send erase commands
                M95160.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = M95160
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95320  '4096 bytes
                Dim M95320 As New SPI_FLASH("ST M95320", 4096, 32)
                M95320.ADDRESSBITS = 16
                M95320.ERASE_REQUIRED = False 'We will not send erase commands
                M95320.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = M95320
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95640   '8192 bytes
                Dim M95640 As New SPI_FLASH("ST M95640", 8192, 32)
                M95640.ADDRESSBITS = 16
                M95640.ERASE_REQUIRED = False 'We will not send erase commands
                M95640.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = M95640
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95128   '16384 bytes
                Dim M95128 As New SPI_FLASH("ST M95128", 16384, 64)
                M95128.ADDRESSBITS = 16
                M95128.ERASE_REQUIRED = False 'We will not send erase commands
                M95128.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = M95128
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95256   '32768 bytes
                Dim M95256 As New SPI_FLASH("ST M95256", 32768, 64)
                M95256.ADDRESSBITS = 16
                M95256.ERASE_REQUIRED = False 'We will not send erase commands
                M95256.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = M95256
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95512   '65536 bytes
                Dim M95512 As New SPI_FLASH("ST M95512", 65536, 128)
                M95512.ADDRESSBITS = 16
                M95512.ERASE_REQUIRED = False 'We will not send erase commands
                M95512.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = M95512
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95M01   '131072 bytes
                Dim M95M01 As New SPI_FLASH("ST M95M01", 131072, 256)
                M95M01.ADDRESSBITS = 24
                M95M01.ERASE_REQUIRED = False 'We will not send erase commands
                M95M01.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = M95M01
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95M02   '262144 bytes
                Dim M95M02 As New SPI_FLASH("ST M95M02", 262144, 256)
                M95M02.ADDRESSBITS = 24
                M95M02.ERASE_REQUIRED = False 'We will not send erase commands
                M95M02.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = M95M02
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25010A  '128 bytes
                Dim AT25010A As New SPI_FLASH("Atmel AT25010A", 128, 8)
                AT25010A.ADDRESSBITS = 8 'check
                AT25010A.ERASE_REQUIRED = False 'We will not send erase commands
                AT25010A.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = AT25010A
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25020A  '256 bytes
                Dim AT25020A As New SPI_FLASH("Atmel AT25020A", 256, 8)
                AT25020A.ADDRESSBITS = 8
                AT25020A.ERASE_REQUIRED = False 'We will not send erase commands
                AT25020A.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = AT25020A
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25040A   '512 bytes
                Dim AT25040A As New SPI_FLASH("Atmel AT25040A", 512, 8)
                AT25040A.ADDRESSBITS = 8
                AT25040A.ERASE_REQUIRED = False 'We will not send erase commands
                AT25040A.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = AT25040A
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25080  '1024 bytes
                Dim AT25080 As New SPI_FLASH("Atmel AT25080", 1024, 32)
                AT25080.ADDRESSBITS = 16
                AT25080.ERASE_REQUIRED = False 'We will not send erase commands
                AT25080.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = AT25080
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25160  '2048 bytes
                Dim AT25160 As New SPI_FLASH("Atmel AT25160", 2048, 32)
                AT25160.ADDRESSBITS = 16
                AT25160.ERASE_REQUIRED = False 'We will not send erase commands
                AT25160.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = AT25160
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25320  '4096 bytes
                Dim AT25320 As New SPI_FLASH("Atmel AT25320", 4096, 32)
                AT25320.ADDRESSBITS = 16
                AT25320.ERASE_REQUIRED = False 'We will not send erase commands
                AT25320.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = AT25320
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25640  '8192 bytes 
                Dim AT25640 As New SPI_FLASH("Atmel AT25640", 8192, 32)
                AT25640.ADDRESSBITS = 16
                AT25640.ERASE_REQUIRED = False 'We will not send erase commands
                AT25640.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = AT25640
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M25AA512  'Microchip 64 bytes
                Dim AT25512 As New SPI_FLASH("Microchip 25AA512", 65536, 128) '128 bytes per page/sector
                AT25512.ADDRESSBITS = 16
                AT25512.ERASE_REQUIRED = False 'We will not send erase commands
                AT25512.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = AT25512
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M25AA160A  '2048 bytes
                Dim M25AA160A As New SPI_FLASH("Microchip 25AA160A", 2048, 16)
                M25AA160A.ADDRESSBITS = 16
                M25AA160A.ERASE_REQUIRED = False 'We will not send erase commands
                M25AA160A.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = M25AA160A
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M25AA160B   '2048 bytes
                Dim M25AA160B As New SPI_FLASH("Microchip 25AA160B", 2048, 32)
                M25AA160B.ADDRESSBITS = 16
                M25AA160B.ERASE_REQUIRED = False 'We will not send erase commands
                M25AA160B.ProgramMode = SPI_ProgramMode.SPI_EEPROM
                SPI_IF.MyFlashDevice = M25AA160B
            Case Else
                Exit Sub
        End Select
        SPI_IF.MyFlashStatus = ConnectionStatus.Supported
    End Sub

#End Region

#Region "Console mode"
    Declare Function AllocConsole Lib "kernel32" () As Integer
    Declare Function FreeConsole Lib "kernel32" () As Integer

    Private Sub RunConsoleMode(ByVal Args() As String)
        If Not Convert.ToBoolean(AllocConsole()) Then Exit Sub
        Dim device_size As UInt32 'Size of the memory
        Dim opt_i2c_device As I2C_Device = Nothing
        Dim opt_chiperase As Boolean = False
        Dim opt_filename As String = ""
        Dim opt_flashoffset As UInt32 = 0
        Dim opt_filelength As UInt32 = 0
        Dim opt_i2c_addr As Byte = &HA0
        Dim opt_verify As Boolean = True
        Dim opt_exit As Boolean = False
        Dim opt_log As Boolean = False
        Dim opt_logappend As Boolean = False
        Dim opt_logfilename As String = ""
        Dim opt_device As FlashcatSettings.SPI_EEPROM_DEVICE = FlashcatSettings.SPI_EEPROM_DEVICE.None
        OperationMode = AvrMode.NotConnected
        ConsoleWriteLine(String.Format(RM.GetString("fcusb_welcome"), Build))
        ConsoleWriteLine("Copyright 2017 - www.EmbeddedComputers.net")
        ConsoleWriteLine(String.Format(RM.GetString("fcusb_running"), Platform))
        If Args Is Nothing OrElse Args.Length = 0 Then
            ConsoleWriteLine("You must specify at least mode")
            GoTo ExitConsoleMode
        End If
        Dim task As ConsoleTask = ConsoleTask.NoTask
        Dim file_io As IO.FileInfo = Nothing
        Select Case Args(0).ToUpper
            Case "-H", "-?", "-HELP"
                task = ConsoleTask.Help
            Case "-LISTPATHS"
                task = ConsoleTask.Path
            Case "-READ"
                task = ConsoleTask.ReadMemory
            Case "-WRITE"
                task = ConsoleTask.WriteMemory
            Case "-ERASE"
                task = ConsoleTask.EraseMemory
            Case "-EXECUTE"
                task = ConsoleTask.ExecuteScript
        End Select
        If (Args.Length > 1) Then 'Lets load options
            For i = 1 To Args.Length - 1
                Dim LastOption As Boolean = False
                If i = Args.Length - 1 Then LastOption = True
                Select Case Args(i).ToUpper
                    Case "-PATH" 'User is requesting a specific device
                        If LastOption OrElse Args(i + 1).StartsWith("-") Then
                            ConsoleWriteLine("You must specify a USB device path following -PATH") : GoTo ExitConsoleMode
                        End If
                        MyUSBDeviceID = Args(i + 1) : i += 1
                        ConsoleWriteLine("USB path set to: " & MyUSBDeviceID)
                    Case "-ERASE"
                        If (Not task = ConsoleTask.WriteMemory) Then
                            ConsoleWriteLine("Erase option is only for -WRITE mode") : GoTo ExitConsoleMode
                        End If
                        opt_chiperase = True
                    Case "-FILE"
                        If LastOption OrElse Args(i + 1).StartsWith("-") Then
                            ConsoleWriteLine("You must specify a filename following -FILE") : GoTo ExitConsoleMode
                        End If
                        opt_filename = Args(i + 1) : i += 1
                    Case "-LOG"
                        If LastOption OrElse Args(i + 1).StartsWith("-") Then
                            ConsoleWriteLine("You must specify a filename following -LOG") : GoTo ExitConsoleMode
                        End If
                        opt_log = True
                        opt_logfilename = Args(i + 1) : i += 1
                    Case "-LOGAPPEND"
                        opt_logappend = True
                    Case "-OFFSET"
                        If LastOption OrElse Args(i + 1).StartsWith("-") Then
                            ConsoleWriteLine("You must specify a value following -OFFSET") : GoTo ExitConsoleMode
                        End If
                        Dim offset_value As String = Args(i + 1) : i += 1
                        If (Not Utilities.IsDataType.HexString(offset_value)) AndAlso (Not IsNumeric(offset_value)) Then
                            ConsoleWriteLine("-OFFSET value must be numeric or hexadecimal") : GoTo ExitConsoleMode
                        End If
                        Try
                            If IsNumeric(offset_value) Then
                                opt_flashoffset = CUInt(offset_value)
                            ElseIf Utilities.IsDataType.HexString(offset_value) Then
                                opt_flashoffset = Utilities.HexToUInt(offset_value)
                            End If
                        Catch ex As Exception
                        End Try
                    Case "-LENGTH"
                        If LastOption OrElse Args(i + 1).StartsWith("-") Then
                            ConsoleWriteLine("You must specify a value following -LENGTH") : GoTo ExitConsoleMode
                        End If
                        Dim offset_value As String = Args(i + 1) : i += 1
                        If (Not Utilities.IsDataType.HexString(offset_value)) AndAlso (Not IsNumeric(offset_value)) Then
                            ConsoleWriteLine("-LENGTH value must be numeric or hexadecimal") : GoTo ExitConsoleMode
                        End If
                        Try
                            If IsNumeric(offset_value) Then
                                opt_filelength = CUInt(offset_value)
                            ElseIf Utilities.IsDataType.HexString(offset_value) Then
                                opt_filelength = Utilities.HexToUInt(offset_value)
                            End If
                        Catch ex As Exception
                        End Try
                    Case "-VERIFY_OFF"
                        opt_verify = False
                    Case "-EXIT"
                        opt_exit = True
                    Case "-I2C"
                        If LastOption OrElse Args(i + 1).StartsWith("-") Then
                            ConsoleWriteLine("You must specify a value following -I2C (i.e. 24XX64)") : GoTo ExitConsoleMode
                        End If
                        Dim offset_value As String = Args(i + 1) : i += 1
                        For Each dev In MyI2CDevices
                            Dim first_part As String = Mid(dev.Name, 1, InStr(dev.Name, " ") - 1)
                            If offset_value = first_part Or offset_value = dev.Name Then
                                opt_i2c_device = dev
                                Exit For
                            End If
                        Next
                        If (opt_i2c_device Is Nothing) Then
                            ConsoleWriteLine("Error: I2C device not loaded. Example: -i2c 24XX64") : GoTo ExitConsoleMode
                        End If
                        MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.I2C_EEPROM
                    Case "-EXTIO"
                        MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.EXTIO
                    Case "-ADDRESS"
                        If LastOption OrElse Args(i + 1).StartsWith("-") Then
                            ConsoleWriteLine("You must specify a value following -ADDRESS") : GoTo ExitConsoleMode
                        End If
                        Dim offset_value As String = Args(i + 1) : i += 1
                        If (Not Utilities.IsDataType.HexString(offset_value)) AndAlso (Not IsNumeric(offset_value)) Then
                            ConsoleWriteLine("-ADDRESS value must be numeric or hexadecimal") : GoTo ExitConsoleMode
                        End If
                        Try
                            If IsNumeric(offset_value) Then
                                opt_i2c_addr = CByte(CUInt(offset_value) And 255)
                            ElseIf Utilities.IsDataType.HexString(offset_value) Then
                                opt_i2c_addr = CByte(Utilities.HexToUInt(offset_value) And 255)
                            End If
                        Catch ex As Exception
                        End Try
                    Case "-DEVICE"
                        Dim NextItem As String = Args(i + 1) : i += 1
                        Select Case NextItem.ToUpper
                            Case "AT25128B"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.AT25128B
                            Case "AT25256B"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.AT25256B
                            Case "AT25512"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.AT25512
                            Case "M95010"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.M95010
                            Case "M95020"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.M95020
                            Case "M95040"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.M95040
                            Case "M95080"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.M95080
                            Case "M95160"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.M95160
                            Case "M95320"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.M95320
                            Case "M95640"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.M95640
                            Case "M95128"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.M95128
                            Case "M95256"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.M95256
                            Case "M95512"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.M95512
                            Case "M95M01"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.M95M01
                            Case "M95M02"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.M95M02
                            Case "AT25010A"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.AT25010A
                            Case "AT25020A"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.AT25020A
                            Case "AT25040A"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.AT25040A
                            Case "AT25080"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.AT25080
                            Case "AT25160"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.AT25160
                            Case "AT25320"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.AT25320
                            Case "AT25640"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.AT25640
                            Case "M25AA160A"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.M25AA160A
                            Case "M25AA160B"
                                opt_device = FlashcatSettings.SPI_EEPROM_DEVICE.M25AA160B
                            Case Else
                                ConsoleWriteLine("SPI EEPROM device not supported: " & NextItem) : GoTo ExitConsoleMode
                        End Select
                    Case Else
                        ConsoleWriteLine("Parameter not supported: " & Args(i))
                End Select
            Next
        End If
        Select Case task
            Case ConsoleTask.Help
                Console_DisplayHelp() : GoTo ExitConsoleMode
            Case ConsoleTask.Path
                Console_DisplayPaths() : GoTo ExitConsoleMode
            Case ConsoleTask.ReadMemory
                If (opt_filename = "") Then
                    ConsoleWriteLine("ReadMemory requires option -FILE to specify where to save to.") : GoTo ExitConsoleMode
                End If
                file_io = New IO.FileInfo(opt_filename)
            Case ConsoleTask.WriteMemory
                If opt_filename = "" Then
                    ConsoleWriteLine("WriteMemory requires option -FILE to specify where to save to.") : GoTo ExitConsoleMode
                End If
                file_io = New IO.FileInfo(opt_filename)
                If Not file_io.Exists Then
                    ConsoleWriteLine("Error: file not found: " & file_io.FullName) : GoTo ExitConsoleMode
                End If
            Case ConsoleTask.ExecuteScript
                If opt_filename = "" Then
                    ConsoleWriteLine("ExecuteScript requires option -FILE to specify which script to run.") : GoTo ExitConsoleMode
                End If
                file_io = New IO.FileInfo(Application.StartupPath & "\Scripts\" & opt_filename)
                If Not file_io.Exists Then
                    file_io = New IO.FileInfo(opt_filename)
                    If Not file_io.Exists Then
                        ConsoleWriteLine("Error: file not found: " & file_io.FullName) : GoTo ExitConsoleMode
                    End If
                End If
            Case ConsoleTask.NoTask
                ConsoleWriteLine("No mode specified. Use -H to list all available modes.")
        End Select
        If Not Operation_Connect() Then
            ConsoleWriteLine("Error: Unable to connect to FlashcatUSB") : GoTo ExitConsoleMode
        End If
        Select Case OperationMode
            Case AvrMode.JTAG
                If Not JtagSetup() Then GoTo ExitConsoleMode
            Case AvrMode.SPI
                ConsoleWriteLine(RM.GetString("cnts_mode") & ": Serial Programmable Interface (SPI)")
                ConsoleWriteLine(RM.GetString("cnts_avrver") & ": " & SPI_IF.GetAvrVersion)
                If (opt_device = FlashcatSettings.DeviceMode.SPI) Then
                    SPI_IF.DeviceInit() 'Console mode
                Else
                    MySettings.OPERATION_MODE = opt_device
                    SPI_Detect() 'This will instead load with the predefined settings
                End If
                Select Case SPI_IF.MyFlashStatus
                    Case ConnectionStatus.NotDetected
                        ConsoleWriteLine(RM.GetString("cnts_device") & ": " & RM.GetString("fcusb_noflash"))
                        ConsoleWriteLine(RM.GetString("fcusb_spi_err1"))
                        GoTo ExitConsoleMode
                    Case ConnectionStatus.NotSupported
                        ConsoleWriteLine(RM.GetString("cnts_device") & ": " & SPI_IF.DeviceName)
                        ConsoleWriteLine(RM.GetString("fcusb_spi_err2"))
                        GoTo ExitConsoleMode
                    Case ConnectionStatus.Supported
                        Dim device_id As String = Hex(SPI_IF.MyFlashDevice.MFG_CODE).PadLeft(2, "0") & " " & Hex(SPI_IF.MyFlashDevice.ID1).PadLeft(4, "0")
                        ConsoleWriteLine(RM.GetString("cnts_device") & ": SPI compatible device (CHIP ID: " & device_id & ")")
                        AddMemoryDevice(DeviceTypes.SPI, 0, SPI_IF.DeviceSize, Nothing, SPI_IF.DeviceName)
                        device_size = SPI_IF.DeviceSize
                End Select
            Case AvrMode.EXPIO
                ConsoleWriteLine(RM.GetString("cnts_mode") & ": Extension Port Interface")
                ConsoleWriteLine(RM.GetString("cnts_avrver") & ": " & SPI_IF.GetAvrVersion)
                SPI_IF.DeviceInit() 'Console mode
                Select Case SPI_IF.MyFlashStatus
                    Case ConnectionStatus.NotDetected
                        ConsoleWriteLine(RM.GetString("cnts_device") & ": " & RM.GetString("fcusb_noflash"))
                        ConsoleWriteLine(RM.GetString("fcusb_spi_err1"))
                        GoTo ExitConsoleMode
                    Case ConnectionStatus.NotSupported
                        ConsoleWriteLine(RM.GetString("cnts_device") & ": " & SPI_IF.DeviceName)
                        ConsoleWriteLine(RM.GetString("fcusb_spi_err2"))
                        GoTo ExitConsoleMode
                    Case ConnectionStatus.Supported
                        Dim device_id As String = Hex(EXT_IF.MyFlashDevice.MFG_CODE).PadLeft(2, "0") & " " & Hex(EXT_IF.MyFlashDevice.ID1).PadLeft(4, "0")
                        ConsoleWriteLine(RM.GetString("cnts_device") & ": Multi-purpose Flash device (CHIP ID: " & device_id & ")")
                        AddMemoryDevice(DeviceTypes.ExtIO, 0, SPI_IF.DeviceSize, Nothing, SPI_IF.DeviceName)
                        device_size = SPI_IF.DeviceSize
                End Select
            Case AvrMode.DFU
                ConsoleWriteLine("Board is currently in DFU mode.") : GoTo ExitConsoleMode
            Case AvrMode.NotConnected
                ConsoleWriteLine("Status: not connected to FlashcatUSB board.") : GoTo ExitConsoleMode
            Case AvrMode.I2C
                SPI_IF.IC2_Init() 'Initiates the I2C engine
                SPI_IF.SetI2CSettings(opt_i2c_device.PageSize, opt_i2c_device.Size, opt_i2c_device.AddressSize, opt_i2c_addr)
                device_size = opt_i2c_device.Size
        End Select
        ConsoleProgressReset = True
        If (Not task = ConsoleTask.ExecuteScript) AndAlso (Not OperationMode = AvrMode.I2C) Then
            If (MyMemDevices.Count = 0) Then
                ConsoleWriteLine("Unable to perform any actions because there are no detected Flash devices.") : GoTo ExitConsoleMode
            End If
        End If
        If (task = ConsoleTask.ReadMemory) Then
            If (opt_flashoffset > device_size) Then opt_flashoffset = 0 'Out of bounds
            If (opt_filelength = 0) Or ((opt_flashoffset + opt_filelength) > device_size) Then
                opt_filelength = (device_size - opt_flashoffset)
            End If
            Dim data_to_read() As Byte = Nothing
            If (OperationMode = AvrMode.I2C) Then
                ConsoleWriteLine("Reading data from I2C Flash device: " & opt_i2c_device.Name & " (" & Format(opt_i2c_device.Size, "#,###") & " bytes)")
                ConsoleWriteLine("I2C parameters: slave-address 0x" & Hex(opt_i2c_addr) & ", offset: 0x" & Hex(opt_flashoffset) & ", length: " & Format(opt_filelength, "#,###") & " bytes")
                data_to_read = SPI_IF.ReadData_I2C_EEPROM(opt_flashoffset, opt_filelength)
            Else
                Dim cb As New MemoryDeviceInstance.StatusCallback
                cb.Progress = New UpdateFunction_Progress(AddressOf SetConsoleProgress)
                cb.Speed = New UpdateFunction_SpeedLabel(AddressOf SetConsoleSpeed)
                MyMemDevices(0).ReadBytes(opt_flashoffset, opt_filelength, FlashArea.Main, cb)
            End If
            If data_to_read Is Nothing OrElse data_to_read.Length = 0 Then
                ConsoleWriteLine("Error: Read was not successful because there is no data to save!") : GoTo ExitConsoleMode
            End If
            Utilities.FileIO.WriteBytes(data_to_read, file_io.FullName)
            ConsoleWriteLine("Saved data to: " & file_io.FullName)
        ElseIf task = ConsoleTask.WriteMemory Then
            Dim data_out() As Byte = Utilities.FileIO.ReadBytes(file_io.FullName)
            If (opt_flashoffset > device_size) Then opt_flashoffset = 0 'Out of bounds
            Dim max_write_count As UInt32 = Math.Min(device_size, data_out.Length)
            If (opt_filelength = 0) Then
                opt_filelength = max_write_count
            ElseIf opt_filelength > max_write_count Then
                opt_filelength = max_write_count
            End If
            If (OperationMode = AvrMode.I2C) Then
                If data_out Is Nothing OrElse data_out.Length = 0 Then
                    ConsoleWriteLine("Error: Write was not successful because there is no data to write!") : GoTo ExitConsoleMode
                End If
                ReDim Preserve data_out(opt_filelength - 1)
                ConsoleWriteLine("Writing data to I2C Flash device: " & opt_i2c_device.Name & " (" & Format(opt_i2c_device.Size, "#,###") & " bytes)")
                ConsoleWriteLine("I2C parameters: slave-address 0x" & Hex(opt_i2c_addr) & ", offset: 0x" & Hex(opt_flashoffset) & ", length: " & Format(opt_filelength, "#,###") & " bytes")
                If SPI_IF.WriteData_I2C_EEPROM(opt_flashoffset, data_out) Then
                    ConsoleWriteLine("I2C EEPROM write was successful")
                Else
                    ConsoleWriteLine("Error: unable to write to I2C EEPROM device")
                End If
            Else
                If opt_chiperase Then
                    ConsoleWriteLine("Erasing Flash memory device... (this may take up to 2 minutes)")
                    MyMemDevices(0).EraseBulk()
                End If
                If data_out Is Nothing OrElse data_out.Length = 0 Then
                    ConsoleWriteLine("Error: Write was not successful because there is no data to write!") : GoTo ExitConsoleMode
                End If
                ReDim Preserve data_out(opt_filelength - 1)
                Dim cb As New MemoryDeviceInstance.StatusCallback
                cb.Progress = New UpdateFunction_Progress(AddressOf SetConsoleProgress)
                cb.Speed = New UpdateFunction_SpeedLabel(AddressOf SetConsoleSpeed)
                FCUSB_FlashWrite(opt_flashoffset, data_out, 0, cb)
            End If
        ElseIf task = ConsoleTask.EraseMemory Then
            ConsoleWriteLine("Performing a full chip erase")
            Try
                If MyMemDevices(0).EraseBulk() Then
                    ConsoleWriteLine("Memory device erased successfully")
                Else
                    ConsoleWriteLine("Error: erasing device failed")
                End If
            Catch ex As Exception
                ConsoleWriteLine("Error: erasing device failed")
            End Try
        ElseIf task = ConsoleTask.ExecuteScript Then
            ConsoleWriteLine("Executing FlashcatUSB script file: " & file_io.Name)
            ScriptEngine.LoadScriptFile(file_io)
        End If
        If opt_exit Then
            ConsoleWriteLine("--------------------------------------------")
            ConsoleWriteLine("Application completed")
            If opt_log Then
                If opt_logappend Then Utilities.FileIO.AppendFile(ConsoleLog.ToArray, opt_logfilename) Else Utilities.FileIO.WriteFile(ConsoleLog.ToArray, opt_logfilename)
            End If
            FreeConsole()
            Exit Sub
        End If
ExitConsoleMode:
        ConsoleWriteLine("--------------------------------------------")
        ConsoleWriteLine("Press any key to close")
        Console.ReadKey()
        If opt_log Then
            If opt_logappend Then Utilities.FileIO.AppendFile(ConsoleLog.ToArray, opt_logfilename) Else Utilities.FileIO.WriteFile(ConsoleLog.ToArray, opt_logfilename)
        End If
        FreeConsole()
    End Sub

    Private ConsoleLog As New List(Of String)

    Private Sub ConsoleWriteLine(ByVal Line As String)
        Console.WriteLine(Line)
        ConsoleLog.Add(Line)
    End Sub

    Private Function JtagSetup() As Boolean
        ConsoleWriteLine(RM.GetString("fcusb_initjtag"))
        Dim FirmVer As String = EJ_IF.GetAvrVersion
        If FirmVer Is Nothing OrElse FirmVer = "" Then
            SetStatus(RM.GetString("fcusb_err1"))
            WriteConsole(RM.GetString("fcusb_err1"))
            Return False
        End If
        ConsoleWriteLine(RM.GetString("cnts_mode") & ": Enhanced JTAG")
        ConsoleWriteLine(RM.GetString("cnts_avrver") & ": " & FirmVer)
        If EJ_IF.Init Then
            ConsoleWriteLine(RM.GetString("fcusb_jtagsetup"))
        Else
            ConsoleWriteLine(RM.GetString("fcusb_jtag_err1")) : Return False
        End If
        If EJ_IF.TargetDevice.IDCODE = 0 Then 'Not connected
            Console.WriteLine(RM.GetString("fcusb_jtag_err2")) : Return False
        End If
        ConsoleWriteLine("Detected CPU ID: 0x" & Hex(EJ_IF.TargetDevice.IDCODE) & " IMP CODE: 0x" & Hex(EJ_IF.TargetDevice.IMPCODE))
        ConsoleWriteLine("Manufacturer ID: 0x" & Hex(EJ_IF.TargetDevice.MANUID) & " Part ID: 0x" & Hex(EJ_IF.TargetDevice.PARTNU))
        ConsoleWriteLine("EJTAG Version support: " & EJ_IF.TargetDevice.IMPVER)
        If EJ_IF.TargetDevice.NoDMA Then
            ConsoleWriteLine(RM.GetString("fcusb_jtag_nodma"))
        Else
            ConsoleWriteLine(RM.GetString("fcusb_jtag_dma"))
        End If
        Dim MyScripts(,) As String = GetCompatibleScripts(EJ_IF.TargetDevice.IDCODE)
        Dim DefaultScript As String = Reg_GetPref_DefaultScript(Hex(EJ_IF.TargetDevice.IDCODE))
        Dim SelectScript As Integer = 0
        If MyScripts IsNot Nothing AndAlso (MyScripts.Length / 2) = 1 Then
            ConsoleWriteLine(String.Format(RM.GetString("fcusb_script_loading"), MyScripts(0, 0)))
            ScriptEngine.LoadScriptFile(New IO.FileInfo(ScriptPath & MyScripts(0, 0)))
        ElseIf MyScripts IsNot Nothing Then
            If Not DefaultScript = "" Then
                For i = 0 To CInt((MyScripts.Length / 2) - 1)
                    If UCase(MyScripts(i, 0)) = UCase(DefaultScript) Then
                        SelectScript = i
                        Exit For
                    End If
                Next
            End If
            ConsoleWriteLine(String.Format(RM.GetString("fcusb_script_loading"), MyScripts(SelectScript, 0)))
            Dim df As New IO.FileInfo(ScriptPath & MyScripts(SelectScript, 0))
            ScriptEngine.LoadScriptFile(df)
        End If
        Return True
    End Function

    Private Enum ConsoleTask
        NoTask
        Help
        Path
        ReadMemory
        WriteMemory
        EraseMemory
        ExecuteScript
    End Enum

    Private ConsoleProgressReset As Boolean
    Private Delegate Sub UpdateFunction_Progress(ByVal percent As Integer)
    Private Delegate Sub UpdateFunction_SpeedLabel(ByVal speed_str As String)

    Private Sub SetConsoleProgress(ByVal percent As Integer)
        If ConsoleProgressReset Then
            Console.WriteLine("")
            ConsoleProgressReset = False
        End If
        Console.SetCursorPosition(0, Console.CursorTop - 1)
        Console.Write("[" & percent.ToString.PadLeft(3, " ") & "% complete]")
        Console.SetCursorPosition(0, Console.CursorTop + 1)
        'If p = 100 Then Console.Write(vbCrLf)
    End Sub

    Private Sub SetConsoleSpeed(ByVal speed_str As String)
        If ConsoleProgressReset Then
            Console.WriteLine("")
            ConsoleProgressReset = False
        End If
        Console.SetCursorPosition(15, Console.CursorTop - 1)
        Console.Write(" [" & speed_str & "]          ")
        Console.SetCursorPosition(0, Console.CursorTop + 1)
    End Sub

    Private Sub Console_DisplayHelp()
        ConsoleWriteLine("--------------------------------------------")
        ConsoleWriteLine("Syntax: FlashcatUSB.exe [mode] (options)")
        ConsoleWriteLine("")
        ConsoleWriteLine("Modes:")
        ConsoleWriteLine("-read             Will perform a flash memory read operation")
        ConsoleWriteLine("-write            Will perform a flash memory write operation")
        ConsoleWriteLine("-execute          Allows you to execute a FlashcatUSB script file (*.fcs)")
        ConsoleWriteLine("-listpaths        Displays the USB paths for all conneted FlashcatUSB devices")
        ConsoleWriteLine("-help             Shows this dialog")
        ConsoleWriteLine("")
        ConsoleWriteLine("Options:")
        ConsoleWriteLine("-File (fielname)  Specifies the file to use for read/write/execute")
        ConsoleWriteLine("-Length (value)   Specifies the number of bytes to read/write")
        ConsoleWriteLine("-Offset (value)   Specifies the offset to write the file to flash")
        ConsoleWriteLine("-Device (part)    Specifies a specific SPI EEPROM device to use (i.e. M95080)")
        ConsoleWriteLine("-I2C (part)       Enables I2C EEPROM mode for a specific device")
        ConsoleWriteLine("-Address (hex)    Specifies the I2C slave address (i.e. 0xA0)")
        ConsoleWriteLine("-ExtIO            Enables the extension port")
        ConsoleWriteLine("-Mode (value)     Use to enable EXTIO or I2C mode instead of SPI mode")
        ConsoleWriteLine("-Erase            Perform a whole memory erase prior to writing data")
        ConsoleWriteLine("-Path (string)    Select which FlashcatUSB to use (use quotes around path)")
        ConsoleWriteLine("-Verify_Off       Turns off data verification for flash write operations")
        ConsoleWriteLine("-Exit             Automatically close window when completed")
        ConsoleWriteLine("-Log (fielname)   Save the output from the console to a file")
        ConsoleWriteLine("-LogAppend        Append the console text to an existing file")
    End Sub

    Private Sub Console_DisplayPaths()
        Dim paths() As String = GetConnectedFlashcatUSBPaths()
        ConsoleWriteLine("--------------------------------------------")
        If paths Is Nothing OrElse paths.Count = 0 Then
            ConsoleWriteLine("No FlashcatUSB devices connected")
        Else
            ConsoleWriteLine("Listing the USB root paths for connected FlashcatUSB devices")
            Dim i As Integer = 0
            For Each usbdev In paths
                ConsoleWriteLine("Index " & i.ToString & " FlashcatUSB device: " & usbdev)
                i += 1
            Next
        End If
    End Sub

    Private Function GetConnectedFlashcatUSBPaths() As String()
        Dim paths As New List(Of String)
        Dim usb_dev1 As New LibUsbDotNet.Main.UsbDeviceFinder(&H16C0, &H5DD)
        Dim fcusb_list1 As LibUsbDotNet.Main.UsbRegDeviceList = LibUsbDotNet.UsbDevice.AllDevices.FindAll(usb_dev1)
        If fcusb_list1 IsNot Nothing AndAlso fcusb_list1.Count > 0 Then
            For i = 0 To fcusb_list1.Count - 1
                Dim u As LibUsbDotNet.WinUsb.WinUsbRegistry = fcusb_list1(i)
                Dim o As String = u.DeviceProperties("DeviceID")
                paths.Add(o)
            Next
        End If
        Dim usb_dev2 As New LibUsbDotNet.Main.UsbDeviceFinder(&H16C0, &H5DE)
        Dim fcusb_list2 As LibUsbDotNet.Main.UsbRegDeviceList = LibUsbDotNet.UsbDevice.AllDevices.FindAll(usb_dev2)
        If fcusb_list2 IsNot Nothing AndAlso fcusb_list2.Count > 0 Then
            For i = 0 To fcusb_list2.Count - 1
                Dim u As LibUsbDotNet.WinUsb.WinUsbRegistry = fcusb_list2(i)
                Dim o As String = u.DeviceProperties("DeviceID")
                paths.Add(o)
            Next
        End If
        Dim usb_dev3 As New LibUsbDotNet.Main.UsbDeviceFinder(&H16C0, &H5DF)
        Dim fcusb_list3 As LibUsbDotNet.Main.UsbRegDeviceList = LibUsbDotNet.UsbDevice.AllDevices.FindAll(usb_dev3)
        If fcusb_list3 IsNot Nothing AndAlso fcusb_list3.Count > 0 Then
            For i = 0 To fcusb_list3.Count - 1
                Dim u As LibUsbDotNet.WinUsb.WinUsbRegistry = fcusb_list3(i)
                Dim o As String = u.DeviceProperties("DeviceID")
                paths.Add(o)
            Next
        End If
        Return paths.ToArray
    End Function


#End Region

#Region "Software DLL Wrapper Calls"

    Public Sub FCUSB_LedOff()
        If OperationMode = AvrMode.JTAG Then
            EJ_IF.LEDOff()
        ElseIf OperationMode = AvrMode.SPI Then
            SPI_IF.LEDOff()
        ElseIf OperationMode = AvrMode.I2C Then
            SPI_IF.LEDOff()
        ElseIf OperationMode = AvrMode.EXPIO Then
            SPI_IF.LEDOff()
        End If
    End Sub

    Public Sub USB_LEDOn()
        If OperationMode = AvrMode.JTAG Then
            EJ_IF.LEDOn()
        ElseIf OperationMode = AvrMode.SPI Then
            SPI_IF.LEDOn()
        ElseIf OperationMode = AvrMode.I2C Then
            SPI_IF.LEDOn()
        ElseIf OperationMode = AvrMode.EXPIO Then
            SPI_IF.LEDOn()
        End If
    End Sub

    Public Sub FCUSB_LedBlink()
        Dim datRet() As Byte = Nothing
        If OperationMode = AvrMode.JTAG Then
            EJ_IF.LEDBlink()
        ElseIf OperationMode = AvrMode.SPI Then
            SPI_IF.LEDBlink()
        ElseIf OperationMode = AvrMode.I2C Then
            SPI_IF.LEDBlink()
        ElseIf OperationMode = AvrMode.EXPIO Then
            SPI_IF.LEDBlink()
        End If
    End Sub

#End Region

#Region "Bit Swapping and Endian Feature"

    Public Enum BitSwapMode
        None = 0
        Bits_4 = 1 '0xF0 to 0x0F
        Bits_8 = 2 '0xFF00 to 0x00FF
        Bits_16 = 3 '0xF1F2F3F4 to 0xF3F4F1F2
    End Enum

    Public Enum BitEndianMode
        'big endian / little endian
        BigEndian = 0 'All data is MSB
        LittleEndian_8bit = 1 '0b10000000 = 0b00000001
        LittleEndian_16bit = 2 '0b1000000001000000 = 0b0000001000000001
        LittleEndian_32bit = 2 '0b10000000010000000010000000010000 = 0b00001000000001000000001000000001
    End Enum
    'FILE-->MEMORY
    Public Sub BitSwap_Forward(ByRef data() As Byte)
        Select Case MySettings.BIT_ENDIAN
            Case BitEndianMode.LittleEndian_8bit
                Utilities.ReverseByteEndian_8bit(data)
            Case BitEndianMode.LittleEndian_16bit
                Utilities.ReverseByteEndian_16bit(data)
            Case BitEndianMode.LittleEndian_32bit
                Utilities.ReverseByteEndian_32bit(data)
        End Select
        Select Case MySettings.BIT_SWAP
            Case BitSwapMode.Bits_4
                Utilities.SwapByteArray_Nibble(data)
            Case BitSwapMode.Bits_8
                Utilities.SwapByteArray_Byte(data)
            Case BitSwapMode.Bits_16
                Utilities.SwapByteArray_Word(data)
        End Select
    End Sub
    'MEMORY-->FILE
    Public Sub BitSwap_Reverse(ByRef data() As Byte)
        Select Case MySettings.BIT_SWAP
            Case BitSwapMode.Bits_4
                Utilities.SwapByteArray_Nibble(data)
            Case BitSwapMode.Bits_8
                Utilities.SwapByteArray_Byte(data)
            Case BitSwapMode.Bits_16
                Utilities.SwapByteArray_Word(data)
        End Select
        Select Case MySettings.BIT_ENDIAN
            Case BitEndianMode.LittleEndian_8bit
                Utilities.ReverseByteEndian_8bit(data)
            Case BitEndianMode.LittleEndian_16bit
                Utilities.ReverseByteEndian_16bit(data)
            Case BitEndianMode.LittleEndian_32bit
                Utilities.ReverseByteEndian_32bit(data)
        End Select
    End Sub

#End Region

#Region "I2C"
    Public Class I2C_Device
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

    Public MyI2CDevices As New List(Of I2C_Device)

    Public Sub I2CDatabaseInit()
        MyI2CDevices.Add(New I2C_Device("24XX02 (256 x 8)", 256, 1, 8))
        MyI2CDevices.Add(New I2C_Device("24XX04 (512 x 8)", 512, 1, 16))
        MyI2CDevices.Add(New I2C_Device("24XX08 (1K x 8)", 1024, 1, 16))
        MyI2CDevices.Add(New I2C_Device("24XX16 (2K x 8)", 2048, 1, 16)) '<-- CHECK FOR AMTEL 24C16 <--NO A0/1/2
        MyI2CDevices.Add(New I2C_Device("24XX32 (4K x 8)", 4096, 2, 32))
        MyI2CDevices.Add(New I2C_Device("24XX64 (8K x 8)", 8192, 2, 32))
        MyI2CDevices.Add(New I2C_Device("24XX128 (16K x 8)", 16384, 2, 32))
        MyI2CDevices.Add(New I2C_Device("24XX256 (32K x 8)", 32768, 2, 32))
        MyI2CDevices.Add(New I2C_Device("24XX512 (64K x 8)", 65536, 2, 32))
        'Add ROHM BR24G Series
        MyI2CDevices.Add(New I2C_Device("24G128 (16K x 8)", 16384, 2, 32))
        MyI2CDevices.Add(New I2C_Device("24G256 (32K x 8)", 32768, 2, 32))
        MyI2CDevices.Add(New I2C_Device("24G1M (128K x 8)", 131072, 2, 32)) '65536 x 2 Use A0 (named P0) to select page. <-- NO A0
    End Sub




#End Region

    Public Function Reg_GetPref_DefaultScript(ByVal CPUID As String) As String
        Try
            Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\EmbComputers\FlashcatUSB\Default")
            If key Is Nothing Then Return ""
            Dim o As Object = key.GetValue(CPUID)
            If o Is Nothing Then Return ""
            Return CStr(o)
        Catch ex As Exception
            Return ""
        End Try
    End Function

    Public Sub Reg_SavePref_DefaultScript(ByVal CPUID As String, ByVal Script As String)
        Try
            Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\EmbComputers\FlashcatUSB\Default", True)
            If key Is Nothing Then
                Microsoft.Win32.Registry.CurrentUser.CreateSubKey("Software\EmbComputers\FlashcatUSB\Default")
                key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\EmbComputers\FlashcatUSB\Default", True)
            End If
            key.SetValue(CPUID, Script)
        Catch ex As Exception
        End Try
    End Sub

    Private Sub BeginUsbCheckLoop()
        Thread.CurrentThread.Name = "usbCheckLoop"
        If GUI IsNot Nothing Then
            Do Until GUI.FormIsLoaded
                Thread.Sleep(50)
            Loop
        End If
        Do Until AppIsClosing
            Application.DoEvents()
            Operation_CheckStatus()
            Thread.Sleep(500)
        Loop
    End Sub

    Private Function Operation_CheckStatus() As Boolean
        If OperationMode = AvrMode.NotConnected Then
            Return Operation_Connect()
        ElseIf OperationMode = AvrMode.JTAG Then
            'If Not EJTAG.CheckConnection = 0 Then GoTo Disconnected
            If Not EJ_IF.IsConnected Then GoTo Disconnected
        ElseIf OperationMode = AvrMode.DFU Then
            If Not DFU_IF.IsConnected Then GoTo Disconnected
        ElseIf OperationMode = AvrMode.SPI Then
            If Not SPI_IF.IsConnected Then SPI_IF.Disconnect() : GoTo Disconnected
        ElseIf OperationMode = AvrMode.I2C Then
            If Not SPI_IF.IsConnected Then SPI_IF.Disconnect() : GoTo Disconnected 'The SPI IF contains the I2C code
        ElseIf OperationMode = AvrMode.EXPIO Then
            If Not SPI_IF.IsConnected Then SPI_IF.Disconnect() : GoTo Disconnected
        End If
        Return True 'We are still connected
Disconnected:
        Disconnect()
        Return False
    End Function

    Private Function Operation_Connect() As Boolean
        If EJ_IF.IsConnected() Then
            If GUI IsNot Nothing AndAlso (Not AppIsClosing) Then GUI.ShowSpiSettings(False)
            If EJ_IF.Connect() Then
                OperationMode = AvrMode.JTAG
                If GUI IsNot Nothing AndAlso (Not AppIsClosing) Then GUI.OnDeviceConnected()
            End If
        ElseIf DFU_IF.IsConnected Then
            If GUI IsNot Nothing AndAlso (Not AppIsClosing) Then GUI.ShowSpiSettings(False)
            If DFU_IF.Connect() Then
                OperationMode = AvrMode.DFU
                If GUI IsNot Nothing AndAlso (Not AppIsClosing) Then GUI.OnDeviceConnected()
            End If
        ElseIf SPI_IF.IsConnected Then
            If GUI IsNot Nothing AndAlso (Not AppIsClosing) Then
                GUI.ShowSpiSettings(False)
                If MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.I2C_EEPROM Then GUI.ShowI2CSettings(True)
            End If
                If SPI_IF.Connect Then
                If MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.I2C_EEPROM Then
                    OperationMode = AvrMode.I2C
                ElseIf MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.EXTIO Then
                    OperationMode = AvrMode.EXPIO
                Else
                    OperationMode = AvrMode.SPI
                End If
                If GUI IsNot Nothing AndAlso (Not AppIsClosing) Then GUI.OnDeviceConnected()
            End If
        Else
            Return False 'Not connected
        End If
        Return True 'Connected
    End Function

    Public Sub Disconnect(Optional SkipMsg As Boolean = False)
        OperationMode = AvrMode.NotConnected
        HWBOARD = HwVariant.Classic 'Set to default
        MainFlashLoaded = False
        DFU_IF.Disconnect()
        EJ_IF.Disconnect()
        SPI_IF.Disconnect()
        MyMemDevices.Clear() 'Removes all of our devices
        MemoryInterface.FlashCounter = -1
        If GUI IsNot Nothing Then
            GUI.OnDeviceDisconnected()
            If MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI Then
                GUI.ShowSpiSettings(True)
            Else
                GUI.ShowSpiSettings(False)
            End If
            GUI.ShowI2CSettings(False)
        End If
        If Not SkipMsg Then WriteConsole(RM.GetString("fcusb_disconnected"))
    End Sub
    'Called whent the device is closing
    Public Sub AppClosing()
        AppIsClosing = True
        For Each memdev In MyMemDevices
            memdev.GuiControl.AbortAnyOperation
            Do While memdev.IsBusy
                Thread.Sleep(100)
            Loop
        Next
        If OperationMode = AvrMode.JTAG Then
            EJ_IF.Disconnect()
        ElseIf OperationMode = AvrMode.SPI Then
            SPI_IF.Disconnect()
        ElseIf OperationMode = AvrMode.I2C Then
            SPI_IF.Disconnect()
        ElseIf OperationMode = AvrMode.EXPIO Then
            SPI_IF.Disconnect()
        End If
    End Sub

    Private MyLock As New Object

    Public Sub OnDfuStatusUpdate(ByVal percent As Integer) Handles DFU_IF.OnStatus
        If GUI IsNot Nothing Then GUI.UpdateDfuStatusBar(percent)
    End Sub

    Private Sub OnScriptPrint(ByVal Msg As String) Handles ScriptEngine.printf
        WriteConsole(Msg)
    End Sub

    Private Sub OnNonVolPrint(ByVal Msg As String) Handles LegacyNonVol.Printf
        WriteConsole(Msg)
    End Sub

    Public Sub WriteConsole(ByVal Msg As String)
        If AppIsClosing Then Exit Sub
        Try : Monitor.Enter(MyLock)
            If GUI IsNot Nothing Then
                GUI.PrintConsole(Msg)
            Else 'We are writing to console
                ConsoleWriteLine(Msg)
                ConsoleProgressReset = True
            End If
        Catch ex As Exception
        Finally
            Monitor.Exit(MyLock)
        End Try
    End Sub

    Public Sub SetStatus(ByVal Msg As String)
        If Not GUI Is Nothing Then
            GUI.SetStatus(Msg)
        End If
    End Sub

    Public Function GetCompatibleScripts(ByVal CPUID As UInteger) As String(,)
        Dim Autorun As New IO.FileInfo(ScriptPath & "autorun.ini")
        If Autorun.Exists Then
            Dim autoscripts(,) As String = Nothing
            If ProcessAutorun(Autorun, CPUID, autoscripts) Then
                Return autoscripts
            End If
        End If
        Return Nothing
    End Function

    Private Function ProcessAutorun(ByVal Autorun As IO.FileInfo, ByVal ID As UInteger, ByRef scripts(,) As String) As Boolean
        Try
            Dim f() As String = Utilities.FileIO.ReadFile(Autorun.FullName)
            Dim autoline() As String
            Dim sline As String
            Dim MyCode As UInteger
            Dim out As New ArrayList 'Holds str()
            For Each sline In f
                sline = Trim(Utilities.RemoveComment(sline))
                If Not sline = "" Then
                    autoline = sline.Split(CChar(":"))
                    If autoline.Length = 3 Then
                        MyCode = Utilities.HexToUInt(autoline(0))
                        If MyCode = ID Then
                            out.Add(New String() {autoline(1), autoline(2)})
                        End If
                    End If
                End If
            Next
            If out.Count > 0 Then
                Dim ret(out.Count - 1, 1) As String
                Dim i As Integer
                Dim s() As String
                For i = 0 To out.Count - 1
                    s = CType(out(i), String())
                    ret(i, 0) = s(0)
                    ret(i, 1) = s(1)
                Next
                scripts = ret
                Return True 'Scripts are available
            End If
        Catch ex As Exception
            WriteConsole("Error processing Autorun.ini")
        End Try
        Return False
    End Function

    Public Function FCUSB_GetPreferredBlockSize() As Integer
        If OperationMode = AvrMode.JTAG Then
            If EJ_IF.TargetDevice.NoDMA Then
                Return 512 'non dma is REALLY slow
            Else
                'Return 1024 '78KB on 3349
                'Return 2048 '101.5KB on 3349
                Return 4096 '113KB on 3349
            End If
        ElseIf OperationMode = AvrMode.SPI Then
            'Return 1024 '60KB
            'Return 4096 '128KB
            'Return 8192 155KB
            'Return 16384 '170KB
            Return 32768 '180KB
            'Return 65536
        ElseIf OperationMode = AvrMode.EXPIO Then
            'Return 8192
            'Return 16384
            Return 32768
        End If
        Return 1024
    End Function

    Public Function FCUSB_GetFlashSize(ByVal index As Integer) As Integer
        Dim memDev As MemoryDeviceInstance = MemoryInterface.GetDeviceInstance(index)
        If memDev Is Nothing Then Return 0 'Not found
        Return memDev.Size
    End Function

    Public Function FCUSB_ReadMode(ByVal index As Integer) As Integer
        Dim memDev As MemoryDeviceInstance = MemoryInterface.GetDeviceInstance(index)
        If memDev Is Nothing Then Return 0 'Not found
        memDev.ReadMode()
        Return 1
    End Function

    Public Function FCUSB_FlashWrite(ByVal flash_offset As Long, ByRef data() As Byte, ByVal mem_index As Integer, ByVal callback As MemoryDeviceInstance.StatusCallback) As Boolean
        Dim memDev As MemoryDeviceInstance = MemoryInterface.GetDeviceInstance(mem_index)
        If memDev Is Nothing Then Return False 'Not found
        Dim FlashBase As UInt32 = memDev.BaseAddress
        Dim FlashSize As Integer = memDev.Size
        Dim LastAddress As UInt32 = CUInt(FlashBase + flash_offset + data.Length - 1)
        Dim TopAddr As UInt32 = CUInt(FlashBase + FlashSize - 1)
        If LastAddress > TopAddr Then
            WriteConsole(RM.GetString("fcusb_flasherr1"))
            Return False
        End If
        Return memDev.WriteBytes(flash_offset, data, FlashArea.Main, callback)
    End Function

    Public Function FCUSB_FlashSectorSize(ByVal sector As UInteger, ByVal mem_index As Integer) As Integer
        Dim memDev As MemoryDeviceInstance = MemoryInterface.GetDeviceInstance(mem_index)
        If memDev Is Nothing Then Return 0 'Not found
        Return memDev.GetSectorSize(sector, FlashArea.Main)
    End Function

    Public Function FCUSB_ReadFlashByte(ByVal address As UInteger, ByVal mem_index As Integer) As Byte
        Dim memDev As MemoryDeviceInstance = MemoryInterface.GetDeviceInstance(mem_index)
        If memDev Is Nothing Then Return 0 'Not found
        Return memDev.ReadByte(address)
    End Function

    Public Function FCUSB_FlashSectorCount(ByVal mem_index As Integer) As Integer
        Dim memDev As MemoryDeviceInstance = MemoryInterface.GetDeviceInstance(mem_index)
        If memDev Is Nothing Then Return 0 'Not found
        Return memDev.GetSectorCount
    End Function

    Public Sub FCUSB_FlashEraseSector(ByVal sector As UInt32, ByVal mem_index As Integer)
        Dim memDev As MemoryDeviceInstance = MemoryInterface.GetDeviceInstance(mem_index)
        If memDev Is Nothing Then Exit Sub 'Not found
        memDev.EraseSector(sector)
        memDev.GuiControl.RefreshView()
    End Sub

    Public Function FCUSB_FlashEraseSection(ByVal address As Long, ByVal count As Long, ByVal mem_index As Integer) As Boolean
        Dim memDev As MemoryDeviceInstance = MemoryInterface.GetDeviceInstance(mem_index)
        If memDev Is Nothing Then Return False 'Not found
        Dim SectorCounter As Integer = GetSectorFromAddress(memDev, address)
        If SectorCounter = -1 Then Return False
        Dim BytesErased As Integer = 0
        Do Until BytesErased >= count
            BytesErased += memDev.GetSectorSize(SectorCounter, FlashArea.Main)
            memDev.EraseSector(SectorCounter)
            SectorCounter = SectorCounter + 1
        Loop
        Return True
    End Function

    Public Sub FCUSB_FlashEraseBulk(ByVal mem_index As Integer)
        Dim memDev As MemoryDeviceInstance = MemoryInterface.GetDeviceInstance(mem_index)
        If memDev Is Nothing Then Exit Sub 'Not found
        memDev.EraseBulk()
    End Sub
    'On error or busy, return nothing
    Public Function FCUSB_ReadMemory(ByVal flash_offset As Long, ByVal count As Long, ByVal mem_index As Integer, ByVal callback As MemoryDeviceInstance.StatusCallback) As Byte()
        Dim memDev As MemoryDeviceInstance = MemoryInterface.GetDeviceInstance(mem_index)
        If memDev Is Nothing Then Return Nothing 'Not found
        Return memDev.ReadBytes(flash_offset, count, mem_index, callback)
    End Function
    'Attempts to write blindly to a CFI compatible flash device, this is used for hacking techniques to fix devices
    Public Function FCUSB_FlashBlindly(ByVal address As Long, ByVal data() As Byte, ByVal index As Integer) As Boolean
        Dim memDev As MemoryDeviceInstance = MemoryInterface.GetDeviceInstance(index)
        If memDev Is Nothing Then Return False 'Not found
        Return memDev.WriteBytes(address, data, True)
        Return True
    End Function

    Public Function FCUSB_MemoryUpdate(ByVal mem_index As Integer) As Boolean
        Dim memDev As MemoryDeviceInstance = MemoryInterface.GetDeviceInstance(mem_index)
        If memDev Is Nothing Then Return False 'Not found
        memDev.GuiControl.RefreshView()
        Return True
    End Function

    <DllImport("kernel32.dll", SetLastError:=True, CallingConvention:=CallingConvention.Winapi)>
    Private Function IsWow64Process(<[In]> hProcess As IntPtr, <Out> ByRef wow64Process As Boolean) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    Public Function GetOsBitsString() As String
        If (Environment.OSVersion.Version.Major = 5 AndAlso Environment.OSVersion.Version.Minor >= 1) OrElse Environment.OSVersion.Version.Major >= 6 Then
            Using p As Process = Process.GetCurrentProcess()
                Dim retVal As Boolean
                If IsWow64Process(p.Handle, retVal) Then
                    If retVal Then Return "64 bit"
                End If
            End Using
        End If
        Return "32 bit"
    End Function

    Public Function GetManu(ByVal ManuID As Integer) As String
        Select Case ManuID
            Case 1
                Return "Spansion"
            Case 4
                Return "Fujitsu"
            Case 7
                Return "Hitachi"
            Case 9
                Return "Intel"
            Case 21
                Return "Philips"
            Case 31
                Return "Atmel"
            Case 32
                Return "ST"
            Case 52
                Return "Cypress"
            Case 53
                Return "DEC"
            Case 73
                Return "Xilinx"
            Case 110
                Return "Altera"
            Case 112 '0x70
                Return "QUALCOMM"
            Case 191 '0xBF
                Return "Broadcom"
            Case 194
                Return "MXIC"
            Case 239
                Return "Winbond"
            Case 336
                Return "Signetics"
            Case Else
                Return Hex(ManuID) ' Not Found
        End Select
    End Function
    'Returns the name of the flash device
    Public Function GetDeviceManufacture(ByVal ManuID As Byte) As String
        Select Case ManuID
            Case &H89
                Return "Intel"
            Case &H20
                Return "ST"
            Case &H2C
                Return "Micron"
            Case &H1
                Return "AMD / Spansion"
            Case &H98
                Return "TOSHIBA"
            Case &H4
                Return "FUJITSU"
            Case &HB0
                Return "SHARP"
            Case &HC2
                Return "MXIC"
            Case &H1F
                Return "ATMEL"
            Case &HAD
                Return "HYHYNIX"
            Case &HBF
                Return "SST" 'Silicon Storage
            Case &HEC
                Return "Samsung"
            Case Else
                Return "(Unknown)"
        End Select
    End Function

    Public Function GetMessageBoxForVerify(ByVal address As Long) As Boolean
        Dim TitleTxt As String = "Failed to successfully write data to address 0x" & Hex(address) & vbCrLf & vbCrLf
        TitleTxt &= "Continue write operation?"
        If MsgBox(TitleTxt, MsgBoxStyle.YesNo, "Error verification failed") = MsgBoxResult.No Then
            Return False 'Stop working
        Else
            Return True
        End If
    End Function

    'Alternative IO.Compression (for .net 4.0 framework)
    Public Class ZipHelper
        Implements IDisposable
        Private zip As IO.Packaging.Package

        Sub New(file As IO.FileInfo)
            Me.FILENAME = file
            If Me.FILENAME.Exists AndAlso Me.FILENAME.Length = 0 Then Me.FILENAME.Delete()
            zip = IO.Packaging.Package.Open(Me.FILENAME.FullName, IO.FileMode.OpenOrCreate)
        End Sub
        'Returns the number of files inside the archive
        Public ReadOnly Property Count As Integer
            Get
                If Not FILENAME.Exists Then Return 0
                Dim p As IO.Packaging.PackagePartCollection = zip.GetParts()
                Return p.Count
            End Get
        End Property
        'Contains the filename of the zip file
        Public ReadOnly Property FILENAME As IO.FileInfo

        Public Sub AddFile(ByVal name As String, ByVal data() As Byte)
            Dim destFilename As String = ".\" & IO.Path.GetFileName(name)
            Dim uri_path As Uri = IO.Packaging.PackUriHelper.CreatePartUri(New Uri(destFilename, UriKind.Relative))
            If (zip.PartExists(uri_path)) Then zip.DeletePart(uri_path)
            Dim part As IO.Packaging.PackagePart = zip.CreatePart(uri_path, "", IO.Packaging.CompressionOption.Normal)
            Using fs As New IO.MemoryStream(data)
                Using dest As IO.Stream = part.GetStream
                    fs.CopyTo(dest)
                End Using
            End Using
        End Sub

        Public Function GetFileData(ByVal name As String) As Byte()
            If Not FILENAME.Exists Then Return Nothing
            Dim destFilename As String = ".\" & IO.Path.GetFileName(name)
            Dim uri_path As Uri = IO.Packaging.PackUriHelper.CreatePartUri(New Uri(destFilename, UriKind.Relative))
            If (Not zip.PartExists(uri_path)) Then Return Nothing 'File not found
            Dim part As IO.Packaging.PackagePart = zip.GetPart(uri_path)
            Using fs As IO.Stream = part.GetStream
                Dim data_out(fs.Length - 1) As Byte
                fs.Read(data_out, 0, data_out.Length)
                Return data_out
            End Using
            Return Nothing
        End Function

        Public Function GetFileStream(ByVal name As String) As IO.Stream
            Try
                If Not FILENAME.Exists Then Return Nothing
                Dim destFilename As String = ".\" & IO.Path.GetFileName(name)
                Dim uri_path As Uri = IO.Packaging.PackUriHelper.CreatePartUri(New Uri(destFilename, UriKind.Relative))
                If (Not zip.PartExists(uri_path)) Then Return Nothing 'File not found
                Dim part As IO.Packaging.PackagePart = zip.GetPart(uri_path)
                Return part.GetStream
            Catch ex As Exception
            End Try
            Return Nothing
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            Try
                If zip IsNot Nothing Then
                    zip.Flush() 'Writes the data
                    zip.Close()
                End If
            Catch ex As Exception
            End Try
        End Sub

        Protected Overrides Sub Finalize()
            Me.Dispose()
        End Sub

    End Class

    Public Sub SetProgress(ByVal Percent As Integer, Optional mem_index As Integer = 0)
        Try
            Dim memDev As MemoryDeviceInstance = MemoryInterface.GetDeviceInstance(mem_index) 'We might need to change Index here in the future
            If memDev IsNot Nothing Then memDev.GuiControl.SetProgress(Percent)
        Catch ex As Exception
        End Try
    End Sub


End Module


