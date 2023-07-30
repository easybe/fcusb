'COPYRIGHT EMBEDDED COMPUTERS LLC 2021 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB PRODUCTS
'CONTACT EMAIL: support@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: This is the main module that is loaded first.

Option Strict On

Imports System.Threading
Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.MemoryInterface
Imports FlashcatUSB.SPI
Imports FlashcatUSB.USB

Public Module MainApp
    Public Property RM As Resources.ResourceManager = My.Resources.english.ResourceManager
    Public MyLocation As String = Reflection.Assembly.GetEntryAssembly().Location
    Public MyConsole As ConsoleMode
    Public FlashDatabase As New FlashDatabase 'This contains definitions of all of the supported Flash devices
    Public MySettings As New FlashcatSettings(SettingsMode.FromIniFile)
    Public CURRENT_DEVICE_MODE As DeviceMode
    Public AppIsClosing As Boolean = False
    Public WithEvents ScriptProcessor As New EC_ScriptEngine.Processor
    Public WithEvents USBCLIENT As New HostClient
    Public ScriptPath As String = (New IO.FileInfo(MyLocation)).DirectoryName & "\Scripts\"
    Public Platform As String
    Public CUSTOM_SPI_DEV As SPI_NOR
    Private FcMutex As Mutex
    Public WithEvents MEM_IF As New MemoryInterface 'Contains API to access the various memory devices
    Public WithEvents MAIN_FCUSB As FCUSB_DEVICE = Nothing
    Public NAND_LayoutTool As NAND_LAYOUT_TOOL

    Public Const IS_DEBUG_VER As Boolean = False

    Sub Main(ParamArray args() As String)
        Try 'This makes it only allow one instance
            Dim created As Boolean = False
            FcMutex = New Mutex(False, "FCUSB", created)
            If Not FcMutex.WaitOne(0, False) Then
                FcMutex = Nothing
                Exit Sub
            End If
        Catch ex As Exception
            Exit Sub
        End Try
        Thread.CurrentThread.CurrentUICulture = Globalization.CultureInfo.CreateSpecificCulture("en-US")
        Thread.CurrentThread.CurrentCulture = Globalization.CultureInfo.CreateSpecificCulture("en-US")
        LicenseSystem_Init()
        CUSTOM_SPI_DEV = New SPI_NOR("User-defined", VCC_IF.SERIAL_3V, 1048576, 0, 0)
        If NAND_ECC_CFG Is Nothing Then NAND_ECC_CFG = GenerateLocalEccConfigurations()
        Thread.CurrentThread.Name = "rootApp"
        Platform = Environment.OSVersion.Platform & " (" & Utilities.GetOsBitsString() & ")"
        ScriptApplication.AddInternalMethods()
        USBCLIENT.StartService()

        'Dim arg_list As New List(Of String)
        'arg_list.Add("-DETECT")
        'arg_list.Add("-PNAND")

        'arg_list.Add("-SCRIPT")
        'arg_list.Add("-FILE")
        'arg_list.Add("NavTool.fcs")
        'arg_list.Add("-LOG")
        'arg_list.Add("status.txt")
        'arg_list.Add("-EXIT")

        'arg_list.Add("-COMPARE")
        'arg_list.Add("-SPI")
        'arg_list.Add("-FILE")
        'arg_list.Add("ewj.bin")
        'arg_list.Add("-LOG")
        'arg_list.Add("status.txt")
        'arg_list.Add("-EXIT")

        'args = arg_list.ToArray()

        MyConsole = New ConsoleMode()
        MyConsole.Start(args)
        AppClosing()
    End Sub

    Public Sub PrintConsole(msg As String, Optional set_status As Boolean = False)
        If AppIsClosing Then Exit Sub
        MyConsole.PrintConsole(msg)
        If set_status Then MainApp.SetStatus(msg)
    End Sub

    Public Sub SetStatus(message As String)
        'PrintConsole(message, True)
    End Sub


#Region "License System"

    Public Property LicenseStatus As LicenseStatusEnum = LicenseStatusEnum.NotLicensed

    Public Sub LicenseSystem_Init()
        Try
            If MySettings.LICENSED_TO.Equals("") Then
                LicenseStatus = LicenseStatusEnum.NotLicensed
            ElseIf MySettings.LICENSE_EXP.Date.Year = 1 Then
                LicenseStatus = LicenseStatusEnum.LicensedValid
            ElseIf Date.Compare(DateTime.Now, MySettings.LICENSE_EXP.Date) < 0 Then
                LicenseStatus = LicenseStatusEnum.LicensedValid
            Else
                LicenseStatus = LicenseStatusEnum.LicenseExpired
            End If
        Catch ex As Exception
        End Try
        If LicenseStatus = LicenseStatusEnum.LicenseExpired Then
            If License_LoadKey(MySettings.LICENSE_KEY) Then 'This will update existing license if its been renewed
                If Date.Compare(DateTime.Now, MySettings.LICENSE_EXP) < 0 Then
                    LicenseStatus = LicenseStatusEnum.LicensedValid
                End If
            End If
            If LicenseStatus = LicenseStatusEnum.LicenseExpired Then
                Dim msg As String = "The license for this software has expired, please consider "
                msg &= "renewing your license. If you need assistance, email license@embeddedcomputers.net" & vbCrLf
                msg &= "Thank you"
                MsgBox(msg, vbInformation, "Commercial License")
            End If
        End If
        If (Not LicenseStatus = LicenseStatusEnum.LicensedValid) Then
            MySettings.ECC_FEATURE_ENABLED = False
        End If
    End Sub

    Public Function License_LoadKey(key As String) As Boolean
        Dim w() As Byte = Utilities.DownloadFile("https://www.embeddedcomputers.net/licensing/index.php?key=" & key)
        If (w IsNot Nothing AndAlso w.Length > 0) Then
            Dim response As String = Utilities.Bytes.ToChrString(w).Replace(vbLf, "").Replace(vbCr, "")
            If (response.Equals("ERROR")) Then Return False
            Dim result() As String = response.Split(ChrW(9)) 'Split by tab
            Dim data_str As String = result(1)
            Dim LicensedDate As DateTime = New DateTime
            If Not data_str.Equals("01/01/0001") Then
                LicensedDate = DateTime.Parse(data_str)
            End If
            If LicensedDate.Date.Year = 1 Then
                LicenseStatus = LicenseStatusEnum.LicensedValid
            ElseIf Date.Compare(DateTime.Now, LicensedDate) < 0 Then
                LicenseStatus = LicenseStatusEnum.LicensedValid
            Else
                Return False
            End If
            MySettings.LICENSED_TO = result(0)
            MySettings.LICENSE_KEY = key
            MySettings.LICENSE_EXP = LicensedDate
            Return True
        End If
        Return False
    End Function

#End Region

#Region "Error correcting code"
    Public NAND_ECC_CFG() As ECC_LIB.ECC_Configuration_Entry
    Public NAND_ECC As ECC_LIB.Engine

    Public Property ECC_LAST_RESULT As ECC_LIB.ECC_DECODE_RESULT = ECC_LIB.ECC_DECODE_RESULT.NoErrors
    'Returns number of bytes for a given ECC configuration
    Public Sub ECC_LoadConfiguration(page_size As Integer, spare_size As Integer)
        NAND_ECC = Nothing
        If NAND_ECC_CFG Is Nothing OrElse (Not MySettings.ECC_FEATURE_ENABLED) Then Exit Sub
        PrintConsole("ECC Feature is enabled")
        For i = 0 To NAND_ECC_CFG.Length - 1
            If NAND_ECC_CFG(i).IsValid() Then
                If NAND_ECC_CFG(i).PageSize = page_size AndAlso NAND_ECC_CFG(i).SpareSize = spare_size Then
                    NAND_ECC = New ECC_LIB.Engine(NAND_ECC_CFG(i).Algorithm, NAND_ECC_CFG(i).BitError, NAND_ECC_CFG(i).SymSize)
                    NAND_ECC.REVERSE_ARRAY = NAND_ECC_CFG(i).ReverseData
                    NAND_ECC.ECC_DATA_LOCATION = NAND_ECC_CFG(i).EccRegion
                    PrintConsole("Compatible profile found at index " & (i + 1).ToString)
                    Exit Sub
                End If
            End If
        Next
        PrintConsole("No compatible profile found")
    End Sub

    Public Function GenerateLocalEccConfigurations() As ECC_LIB.ECC_Configuration_Entry()
        Dim cfg_list As New List(Of ECC_LIB.ECC_Configuration_Entry)

        Dim n1 As New ECC_LIB.ECC_Configuration_Entry()
        n1.PageSize = 512
        n1.SpareSize = 16
        n1.Algorithm = ECC_LIB.ecc_algorithum.hamming
        n1.BitError = 1
        n1.SymSize = 0
        n1.ReverseData = False
        n1.AddRegion(&HD)

        Dim n2 As New ECC_LIB.ECC_Configuration_Entry()
        n2.PageSize = 2048
        n2.SpareSize = 64
        n2.Algorithm = ECC_LIB.ecc_algorithum.reedsolomon
        n2.BitError = 4
        n2.SymSize = 9
        n2.ReverseData = False
        n2.AddRegion(&H7)
        n2.AddRegion(&H17)
        n2.AddRegion(&H27)
        n2.AddRegion(&H37)

        Dim n3 As New ECC_LIB.ECC_Configuration_Entry()
        n3.PageSize = 2048
        n3.SpareSize = 128
        n3.Algorithm = ECC_LIB.ecc_algorithum.bhc
        n3.BitError = 8
        n3.SymSize = 0
        n3.ReverseData = False
        n3.AddRegion(&H13)
        n3.AddRegion(&H33)
        n3.AddRegion(&H53)
        n3.AddRegion(&H73)

        cfg_list.Add(n1)
        cfg_list.Add(n2)
        cfg_list.Add(n3)

        Return cfg_list.ToArray
    End Function

#End Region

#Region "SPI Settings"

    Public Function GetSpiClockString(usb_dev As FCUSB_DEVICE, desired_speed As UInt32) As String
        Dim current_speed As UInt32 = GetMaxSpiClock(usb_dev.HWBOARD, CType(desired_speed, SPI_SPEED))
        Return (current_speed / 1000000).ToString & " Mhz"
    End Function

    Public Function GetMaxSpiClock(brd As FCUSB_BOARD, desired_speed As SPI_SPEED) As SPI_SPEED
        Select Case brd
            Case FCUSB_BOARD.Classic, FCUSB_BOARD.XPORT_PCB2
                If (desired_speed >= SPI_SPEED.MHZ_8) Then Return SPI_SPEED.MHZ_8
            Case Else 'PRO and MACH1
                If (desired_speed > SPI_SPEED.MHZ_32) Then Return SPI_SPEED.MHZ_32
        End Select
        If (desired_speed <= SPI_SPEED.MHZ_1) Then Return SPI_SPEED.MHZ_1
        Return desired_speed
    End Function

    Public Function GetMaxSqiClock(brd As FCUSB_BOARD, desired_speed As UInt32) As SQI_SPEED
        Select Case brd
            Case FCUSB_BOARD.Mach1
                If (desired_speed > SQI_SPEED.MHZ_40) Then Return SQI_SPEED.MHZ_40
            Case FCUSB_BOARD.Professional_PCB5
                If (desired_speed > SQI_SPEED.MHZ_40) Then Return SQI_SPEED.MHZ_40
        End Select
        Return CType(desired_speed, SQI_SPEED)
    End Function

    Public Function GetDevices_SERIAL_EEPROM() As SPI_NOR()
        Dim spi_eeprom As New List(Of SPI_NOR)
        Dim d() As Device = FlashDatabase.GetFlashDevices(MemoryType.SERIAL_NOR)
        For Each dev As SPI_NOR In d
            If DirectCast(dev, SPI_NOR).ProgramMode = SPI_PROG.SPI_EEPROM Then
                spi_eeprom.Add(dev)
            ElseIf DirectCast(dev, SPI_NOR).ProgramMode = SPI_PROG.Nordic Then
                spi_eeprom.Add(dev)
            End If
        Next
        Return spi_eeprom.ToArray
    End Function

    Public Function GetDevices_PARALLEL_EEPROM() As P_NOR()
        Dim par_ee_list As New List(Of P_NOR)
        Dim d() As Device = FlashDatabase.GetFlashDevices(MemoryType.PARALLEL_NOR)
        For Each dev As P_NOR In d
            If dev.WriteMode = MFP_PRG.EEPROM Then par_ee_list.Add(dev)
        Next
        Return par_ee_list.ToArray
    End Function

    Public Function SPIEEPROM_Configure(SPI_IF As SPI_Programmer, eeprom_name As String) As Boolean
        Dim all_eeprom_devices() As SPI_NOR = GetDevices_SERIAL_EEPROM()
        Dim eeprom As SPI_NOR = Nothing
        For Each ee_dev In all_eeprom_devices
            If ee_dev.NAME.Equals(eeprom_name) Then
                eeprom = ee_dev
            End If
        Next
        If eeprom Is Nothing Then Return False
        Dim nRF24_mode As Boolean = False
        SPI_IF.SPIBUS_Setup(GetMaxSpiClock(MAIN_FCUSB.HWBOARD, SPI_SPEED.MHZ_1), MySettings.SPI_MODE)
        Select Case eeprom.NAME
            Case "Nordic nRF24LE1"
                nRF24_mode = True
            Case "Nordic nRF24LU1+ (16KB)"
                nRF24_mode = True
            Case "Nordic nRF24LU1+ (32KB)"
                nRF24_mode = True
        End Select
        If nRF24_mode Then
            MAIN_FCUSB.USB_VCC_ON(Voltage.V3_3)
            Utilities.Sleep(100)
            SPI_IF.SetProgPin(True) 'Sets PROG.PIN to HIGH
            SPI_IF.SetProgPin(False) 'Sets PROG.PIN to LOW
            SPI_IF.SetProgPin(True) 'Sets PROG.PIN to HIGH
            Utilities.Sleep(10)
            If (MAIN_FCUSB.HWBOARD = FCUSB_BOARD.Professional_PCB5) Then
                SPI_IF.SPIBUS_Setup(GetMaxSpiClock(MAIN_FCUSB.HWBOARD, SPI_SPEED.MHZ_8), MySettings.SPI_MODE)
            ElseIf (MAIN_FCUSB.HWBOARD = FCUSB_BOARD.XPORT_PCB2) Then
                SPI_IF.SPIBUS_Setup(GetMaxSpiClock(MAIN_FCUSB.HWBOARD, SPI_SPEED.MHZ_8), MySettings.SPI_MODE)
            Else
                SPI_IF.SPIBUS_Setup(GetMaxSpiClock(MAIN_FCUSB.HWBOARD, SPI_SPEED.MHZ_8), MySettings.SPI_MODE)
            End If
        End If
        SPI_IF.MyFlashDevice = eeprom
        SPI_IF.MyFlashStatus = DeviceStatus.Supported
        If eeprom.NAME.StartsWith("ST ") Then
            SPI_IF.WriteStatusRegister({0}) 'Disable BP0/BP1
        End If
        Return True
    End Function

#End Region

#Region "USB CONNECTED EVENTS"
    'Called when the device connects to USB
    Private Sub OnUsbDevice_Connected(usb_dev As FCUSB_DEVICE) Handles USBCLIENT.DeviceConnected
        If MAIN_FCUSB Is Nothing Then
            MAIN_FCUSB = usb_dev
        ElseIf MAIN_FCUSB Is usb_dev Then
        Else
            Exit Sub
        End If
        MEM_IF.Clear()
        PrintConsole(RM.GetString("successfully_connected")) '"Successfully connected to FlashcatUSB over USB"
        Dim fw_str As String = usb_dev.FW_VERSION()
        Select Case usb_dev.HWBOARD
            Case FCUSB_BOARD.ATMEL_DFU
                PrintConsole(RM.GetString("connected_bl_mode"), True)
                AVR_UpdateFirmware(usb_dev) 'Programs the current AVR firmware into ATMEL device
                Exit Sub
            Case FCUSB_BOARD.Classic
                PrintConsole(String.Format(RM.GetString("connected_fw_ver"), {"FlashcatUSB Classic", fw_str}))
                If Not FirmwareCheck(usb_dev, CLASSIC_FW) Then Exit Sub
            Case FCUSB_BOARD.XPORT_PCB2
                PrintConsole(String.Format(RM.GetString("connected_fw_ver"), {"FlashcatUSB XPORT", fw_str}))
                If Not FirmwareCheck(usb_dev, XPORT_PCB2_FW) Then Exit Sub
            Case FCUSB_BOARD.Professional_PCB5
                If usb_dev.BOOTLOADER() Then
                    Logic.Bootloader_UpdateFirmware(usb_dev, "PCB5_Source.bin") : Exit Sub
                End If
                PrintConsole(String.Format(RM.GetString("connected_fw_ver"), {"FlashcatUSB Pro (PCB 5.x)", fw_str}))
                If Not FirmwareCheck(usb_dev, PRO_PCB5_FW) Then Exit Sub
            Case FCUSB_BOARD.Mach1 'Designed for high-density/high-speed devices (such as 1Gbit+ NOR/MLC NAND)
                If usb_dev.BOOTLOADER() Then
                    Logic.Bootloader_UpdateFirmware(usb_dev, "Mach1_v2_Source.bin") : Exit Sub
                End If
                PrintConsole(String.Format(RM.GetString("connected_fw_ver"), {"FlashcatUSB Mach¹", fw_str}))
                If Not FirmwareCheck(usb_dev, MACH1_PCB2_FW) Then Exit Sub
            Case FCUSB_BOARD.NotSupported
                PrintConsole("Hardware version is no longer supported", True)
                Exit Sub
        End Select
        Dim SupportedModes() As DeviceMode = GetSupportedModes(usb_dev)
        If SupportedModes IsNot Nothing AndAlso SupportedModes.Length > 0 Then
            If (Array.IndexOf(SupportedModes, MySettings.OPERATION_MODE) = -1) Then
                PrintConsole("Error: " & FlashcatSettings.DeviceModetoString(MySettings.OPERATION_MODE) & " mode not hardware supported", True)
                Exit Sub
            End If
        End If
        Select Case usb_dev.HWBOARD
            Case FCUSB_BOARD.Professional_PCB5
                If Not Logic.FCUSBPRO_LoadBitstream(usb_dev, MySettings.OPERATION_MODE, MySettings.VOLT_SELECT) Then
                    PrintConsole("Error: unable to load FPGA bitstream", True)
                    Exit Sub
                End If
            Case FCUSB_BOARD.Mach1
                If Not Logic.MACH1_Init(usb_dev, MySettings.OPERATION_MODE, MySettings.VOLT_SELECT) Then Exit Sub
        End Select
        MyConsole.device_connected = True
    End Sub

    Private Sub OnUsbDevice_Disconnected(usb_dev As FCUSB_DEVICE) Handles USBCLIENT.DeviceDisconnected
        If MAIN_FCUSB IsNot usb_dev Then Exit Sub
        MAIN_FCUSB = Nothing
        MEM_IF.Clear() 'Remove all devices that are on this usb port
        Dim fcusb_hardware As String
        If (usb_dev.HWBOARD = FCUSB_BOARD.Professional_PCB5) Then
            fcusb_hardware = "FlashcatUSB Pro"
        ElseIf usb_dev.HWBOARD = FCUSB_BOARD.Mach1 Then
            fcusb_hardware = "FlashcatUSB Mach¹"
        ElseIf usb_dev.HWBOARD = FCUSB_BOARD.XPORT_PCB2 Then
            fcusb_hardware = "FlashcatUSB XPORT"
        Else
            fcusb_hardware = "FlashcatUSB Classic"
        End If
        PrintConsole(String.Format(RM.GetString("disconnected_from_device"), fcusb_hardware), True)
    End Sub
    'Called whent the device is closing
    Public Sub AppClosing()
        MEM_IF.AbortOperations()
        USBCLIENT.DisconnectAll()
        USBCLIENT.CloseService = True
        AppIsClosing = True 'Do this last
        Utilities.Sleep(200)
        MySettings.Save()
    End Sub

    Public Function Connected_Event(PRG_IF As MemoryDeviceUSB, block_size As Integer, Optional access As FlashAccess = FlashAccess.ReadWriteErase) As MemoryDeviceInstance
        Try
            Utilities.Sleep(150) 'Some devices (such as Spansion 128mbit devices) need a delay here
            Dim dev_inst As MemoryDeviceInstance = MEM_IF.Add(MAIN_FCUSB, PRG_IF, access)
            AddHandler dev_inst.PrintConsole, AddressOf MainApp.PrintConsole
            AddHandler dev_inst.SetStatus, AddressOf MainApp.SetStatus
            AddHandler dev_inst.WriteOperationSucceded, AddressOf MainApp.IF_WriteSuccessful
            AddHandler dev_inst.WriteOperationFailed, AddressOf MainApp.IF_WriteFailed

            dev_inst.PreferredBlockSize = block_size
            dev_inst.RetryWriteCount = MySettings.RETRY_WRITE_ATTEMPTS
            dev_inst.NAND_SkipBadBlock = MySettings.NAND_SkipBadBlock
            dev_inst.BinarySwap = New Utilities.BitSwap(MySettings.BIT_ENDIAN, MySettings.BIT_SWAP)

            Return dev_inst
        Catch ex As Exception
        End Try
        Return Nothing
    End Function

    Public Sub OnDeviceUpdateProgress(device As FCUSB_DEVICE, percent As Integer) Handles MAIN_FCUSB.OnUpdateProgress
        SetProgress(percent)
    End Sub

    Public Sub OnDevicePrintconsole(device As FCUSB_DEVICE, msg As String) Handles MAIN_FCUSB.OnPrintConsole
        PrintConsole(msg, False)
    End Sub

#End Region

#Region "Shell User Interface"
    Private MemDevSelected As MemoryDeviceInstance

    Public Function PromptUser_OpenFile(Optional title As String = "Choose file to open", Optional filter As String = "All files (*.*)|*.*", Optional opt_path As String = "\") As String
        Return MyConsole.Console_Ask(title)
    End Function

    Public Function PromptUser_SaveFile(Optional title As String = "Choose location to save", Optional filter As String = "All files (*.*)|*.*", Optional default_file As String = "") As String
        Return MyConsole.Console_Ask(title)
    End Function

    Public Function PromptUser_Ask(the_question As String) As Boolean
        Dim response As String = MyConsole.Console_Ask(the_question)
        Return response.ToUpper().Equals("YES")
    End Function

    Public Sub PromptUser_Msg(message_txt As String)
        MsgBox(message_txt, MsgBoxStyle.Information, "FlashcatUSB")
    End Sub

    Public Sub SetProgress(percent As Integer)
        MyConsole.Progress_Set(percent)
    End Sub

    Public Sub ProgressBar_SetDevice(mem_dev As MemoryDeviceInstance)
        MemDevSelected = mem_dev
        MyConsole.Progress_Create()
    End Sub

    Public Sub ProgressBar_Percent(percent As Integer)
        MyConsole.Progress_Set(percent)
    End Sub

    Public Sub ProgressBar_Dispose()
        'ProgressBar_Percent(0)
        MemDevSelected = Nothing
        MyConsole.Progress_Remove()
    End Sub

#End Region

    Public Sub RemoveAllTabs()
        'GUI.RemoveAllTabs()
    End Sub

    Public Sub DoEvents()
        'Application.DoEvents()
    End Sub

    Private Sub IF_WriteSuccessful(mydev As MemoryDeviceInstance, x_oper As MemControl_v2.XFER_Operation)
        'GUI.SuccessfulWriteOperation(mydev, x_oper)
    End Sub

    Private Sub IF_WriteFailed(mydev As MemoryDeviceInstance, Params As WriteParameters, ByRef ContinueOperation As Boolean)
        PrintConsole(String.Format(RM.GetString("mem_verify_failed_at"), Hex(Params.Address).PadLeft(8, "0"c)))
        ContinueOperation = False 'Stop console operation
    End Sub

    Private Sub ScriptProcessor_PrintConsole(msg As String) Handles ScriptProcessor.PrintConsole
        PrintConsole(msg)
    End Sub

    Private Sub ScriptProcessor_SetStatus(msg As String) Handles ScriptProcessor.SetStatus
        SetStatus(msg)
    End Sub

    Private Sub ScriptProcessor_DoEvents() Handles ScriptProcessor.DoEvents
        DoEvents()
    End Sub

    Public Function GetDeviceParams() As DetectParams
        Dim my_params As DetectParams
        my_params.OPER_MODE = MyConsole.MyOperation.Mode
        my_params.SPI_AUTO = True
        my_params.SPI_CLOCK = MyConsole.MyOperation.SPI_FREQ
        my_params.SQI_CLOCK = SQI_SPEED.MHZ_10
        my_params.SPI_EEPROM = MyConsole.MyOperation.SPI_EEPROM
        my_params.PARALLEL_EEPROM = MyConsole.MyOperation.PARALLEL_EEPROM
        my_params.I2C_INDEX = MyConsole.MyOperation.I2C_EEPROM
        my_params.I2C_SPEED = I2C_SPEED_MODE._400kHz
        my_params.I2C_ADDRESS = &HA0
        my_params.NOR_READ_ACCESS = MainApp.MySettings.NOR_READ_ACCESS
        my_params.NOR_WE_PULSE = MainApp.MySettings.NOR_WE_PULSE
        my_params.NAND_Layout = MainApp.MySettings.NAND_Layout
        Return my_params
    End Function

End Module