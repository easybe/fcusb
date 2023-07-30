'COPYRIGHT EMBEDDED COMPUTERS LLC 2023 - ALL RIGHTS RESERVED
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
    Public MyLocation As String
    Public GUI As MainForm
    Public FlashDatabase As New FlashDatabase 'This contains definitions of all of the supported Flash devices
    Public MySettings As New FlashcatSettings(SettingsMode.FromRegistry)
    Public CURRENT_DEVICE_MODE As DeviceMode
    Public AppIsClosing As Boolean = False
    Public WithEvents ScriptProcessor As New EC_ScriptEngine.Processor
    Public WithEvents ScriptExt As ScriptExtension
    Public WithEvents USBCLIENT As HostClient
    Public Platform As String
    Public CUSTOM_SPI_DEV As SPI_NOR
    Private FcMutex As Mutex
    Public WithEvents MEM_IF As New MemoryInterface 'Contains API to access the various memory devices
    Public WithEvents MAIN_FCUSB As FCUSB_DEVICE = Nothing
    Public NAND_LayoutTool As NAND_LAYOUT_TOOL
    Public Const IS_DEBUG_VER As Boolean = False
    Public Property DefaultLocation As String = Application.StartupPath

    Sub Main(ParamArray args() As String)
        'Dim file() = Utilities.FileIO.ReadBytes("D:\Software\Firmware.bin")
        'Dim local_file As New IO.FileInfo("D:\Software\Firmware.hex")
        'Dim output_stream = New IHEX.StreamWriter(New IO.StreamWriter(local_file.Open(IO.FileMode.Create)))
        'output_stream.Write(file, 0, file.Length)
        'output_stream.Close()
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
        MyLocation = Reflection.Assembly.GetEntryAssembly().FullName
        Thread.CurrentThread.CurrentUICulture = Globalization.CultureInfo.CreateSpecificCulture("en-US")
        Thread.CurrentThread.CurrentCulture = Globalization.CultureInfo.CreateSpecificCulture("en-US")
        My.Application.ChangeUICulture("en-US")
        My.Application.ChangeCulture("en-US")
        If args.Length = 2 AndAlso args(0).ToUpper.StartsWith("-PATH") Then
            Dim n As New IO.DirectoryInfo(args(1))
            If n.Exists Then DefaultLocation = n.FullName.ToString
        End If
        LicenseSystem_Init()
        CUSTOM_SPI_DEV = New SPI_NOR("User-defined", VCC_IF.SERIAL_3V, 1048576, 0, 0)
        ECC_LIB.ECC_Init()
        Thread.CurrentThread.Name = "rootApp"
        Platform = Environment.OSVersion.Platform & " (" & Utilities.GetOsBitsString() & ")"
        ScriptExt = New ScriptExtension(ScriptProcessor)
        ScriptGUI.AddInternalMethods()
        GUI = New MainForm
        USBCLIENT = New HostClient(IS_DEBUG_VER)
        USBCLIENT.StartService()
        Application.Run(GUI)
        AppClosing()
    End Sub

    Public Sub PrintConsole(message As String, Optional set_status As Boolean = False)
        If AppIsClosing Then Exit Sub
        GUI.PrintConsole(message)
        If set_status Then
            MainApp.SetStatus(message)
        End If
    End Sub

    Public Sub SetStatus(message As String)
        GUI.SetStatus(message)
    End Sub

    Private Sub ScriptExt_GetSelectedDevce(ByRef usb_dev As FCUSB_DEVICE) Handles ScriptExt.GetSelectedDevce
        usb_dev = MAIN_FCUSB
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

    Public Function SPIEEPROM_Configure(SPI_IF As SPI_Programmer, eeprom_name As String) As Boolean
        Dim all_eeprom_devices() As SPI_NOR = FlashDatabase.GetDevices_SERIAL_EEPROM()
        Dim FCUSB = SPI_IF.GetUsbDevice()
        Dim eeprom As SPI_NOR = Nothing
        For Each ee_dev In all_eeprom_devices
            If ee_dev.NAME.Equals(eeprom_name) Then
                eeprom = ee_dev
            End If
        Next
        If eeprom Is Nothing Then Return False
        Dim nRF24_mode As Boolean = False
        SPI_IF.SPIBUS_Setup(GetMaxSpiClock(FCUSB.HWBOARD, SPI_SPEED.MHZ_1), MySettings.SPI_MODE)
        Select Case eeprom.NAME
            Case "Nordic nRF24LE1"
                nRF24_mode = True
            Case "Nordic nRF24LU1+ (16KB)"
                nRF24_mode = True
            Case "Nordic nRF24LU1+ (32KB)"
                nRF24_mode = True
        End Select
        If nRF24_mode Then
            FCUSB.USB_VCC_ON(Voltage.V3_3)
            Utilities.Sleep(100)
            SPI_IF.SetProgPin(True) 'Sets PROG.PIN to HIGH
            SPI_IF.SetProgPin(False) 'Sets PROG.PIN to LOW
            SPI_IF.SetProgPin(True) 'Sets PROG.PIN to HIGH
            Utilities.Sleep(10)
            If (FCUSB.HWBOARD = FCUSB_BOARD.Professional_PCB5) Then
                SPI_IF.SPIBUS_Setup(GetMaxSpiClock(FCUSB.HWBOARD, SPI_SPEED.MHZ_8), MySettings.SPI_MODE)
            ElseIf (FCUSB.HWBOARD = FCUSB_BOARD.XPORT_PCB2) Then
                SPI_IF.SPIBUS_Setup(GetMaxSpiClock(FCUSB.HWBOARD, SPI_SPEED.MHZ_8), MySettings.SPI_MODE)
            Else
                SPI_IF.SPIBUS_Setup(GetMaxSpiClock(FCUSB.HWBOARD, SPI_SPEED.MHZ_8), MySettings.SPI_MODE)
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
        GUI.SetConnectionStatus()
        Dim fw_str As String = usb_dev.FW_VERSION()
        Select Case usb_dev.HWBOARD
            Case FCUSB_BOARD.ATMEL_DFU
                PrintConsole(RM.GetString("connected_bl_mode"), True)
                AVR_UpdateFirmware(usb_dev) 'Programs the current AVR firmware into ATMEL device
                Exit Sub
            Case FCUSB_BOARD.Classic
                PrintConsole(String.Format(RM.GetString("connected_fw_ver"), {"FlashcatUSB Classic", fw_str}))
                If Not FirmwareCheck(usb_dev, CLASSIC_FW) Then Exit Sub
                Print_PCB_Version(usb_dev)
            Case FCUSB_BOARD.XPORT_PCB2
                PrintConsole(String.Format(RM.GetString("connected_fw_ver"), {"FlashcatUSB XPORT", fw_str}))
                If Not FirmwareCheck(usb_dev, XPORT_PCB2_FW) Then Exit Sub
                Print_PCB_Version(usb_dev)
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
                If usb_dev.HWBOARD = FCUSB_BOARD.Mach1 Then
                    MySettings.OPERATION_MODE = DeviceMode.PNAND
                Else
                    MySettings.OPERATION_MODE = SupportedModes(0)
                End If
            End If
        End If
        Select Case usb_dev.HWBOARD
            Case FCUSB_BOARD.Professional_PCB5
                If Not Logic.FCUSBPRO_LoadBitstream(usb_dev, MySettings.OPERATION_MODE, MySettings.VOLT_SELECT) Then
                    PrintConsole("Error: unable to load FPGA bitstream", True)
                    Exit Sub
                End If
                If Not Logic.SMC_Integrity_Check(usb_dev) Then
                    PrintConsole("Error: SMC failed integrity check", True)
                    Exit Sub
                End If
            Case FCUSB_BOARD.Mach1
                If Not Logic.MACH1_Init(usb_dev, MySettings.OPERATION_MODE, MySettings.VOLT_SELECT) Then Exit Sub
                If Not Logic.SMC_Integrity_Check(usb_dev) Then
                    PrintConsole("Error: SMC failed integrity check", True)
                    Exit Sub
                End If
        End Select
        GUI?.USBDeviceConnected(usb_dev)
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
        If GUI IsNot Nothing Then GUI.SetConnectionStatus()
    End Sub
    'Called whent the device is closing
    Public Sub AppClosing()
        MEM_IF.AbortOperations()
        USBCLIENT.DisconnectAll()
        USBCLIENT.CloseService = True
        Application.DoEvents()
        AppIsClosing = True 'Do this last
        Utilities.Sleep(200)
        MySettings.Save()
    End Sub

    Public Function Connected_Event(PRG_IF As MemoryDeviceUSB, block_size As Integer, Optional access As FlashAccess = FlashAccess.ReadWriteErase) As MemoryDeviceInstance
        Try
            Utilities.Sleep(150) 'Some devices (such as Spansion 128mbit devices) need a delay here
            Dim FCUSB = PRG_IF.GetUsbDevice()
            Dim dev_inst As MemoryDeviceInstance = MEM_IF.Add(FCUSB, PRG_IF, access)
            AddHandler dev_inst.PrintConsole, AddressOf MainApp.PrintConsole
            AddHandler dev_inst.SetStatus, AddressOf MainApp.SetStatus
            AddHandler dev_inst.WriteOperationSucceded, AddressOf MainApp.IF_WriteSuccessful
            AddHandler dev_inst.WriteOperationFailed, AddressOf MainApp.IF_WriteFailed
            dev_inst.PreferredBlockSize = block_size
            dev_inst.RetryWriteCount = MySettings.RETRY_WRITE_ATTEMPTS
            dev_inst.NAND_SkipBadBlock = MySettings.NAND_SkipBadBlock
            dev_inst.BinarySwap = New Utilities.BitSwap(MySettings.BIT_ENDIAN, MySettings.BIT_SWAP)
            GUI?.OnNewDeviceConnected(dev_inst)
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
    Private ScriptBar As ProgressBar = Nothing
    Private Delegate Sub cbProgressBarPercent(percent As Integer)
    Private Delegate Sub cbProgressBarDispose()
    Private MemDevSelected As MemoryDeviceInstance

    Public Function PromptUser_OpenFile(Optional title As String = "Choose file to open", Optional filter As String = "All files (*.*)|*.*", Optional opt_path As String = "\") As String
        Dim fileio_diagbox As New OpenFileDialog
        fileio_diagbox.CheckFileExists = True
        fileio_diagbox.Title = title
        fileio_diagbox.Filter = filter
        fileio_diagbox.InitialDirectory = DefaultLocation & opt_path
        If (fileio_diagbox.ShowDialog = DialogResult.OK) Then
            Return fileio_diagbox.FileName
        End If
        Return String.Empty
    End Function

    Public Function PromptUser_SaveFile(Optional title As String = "Choose location to save", Optional filter As String = "All files (*.*)|*.*", Optional default_file As String = "") As String
        Dim fileio_diagbox As New SaveFileDialog
        fileio_diagbox.Filter = filter
        fileio_diagbox.Title = title
        fileio_diagbox.FileName = default_file
        fileio_diagbox.InitialDirectory = DefaultLocation
        If fileio_diagbox.ShowDialog = DialogResult.OK Then
            Return fileio_diagbox.FileName
        End If
        Return String.Empty
    End Function

    Public Function PromptUser_Ask(the_question As String) As Boolean
        If MsgBox(the_question, MsgBoxStyle.YesNo, "FlashcatUSB") = MsgBoxResult.Yes Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub PromptUser_Msg(message_txt As String)
        MsgBox(message_txt, MsgBoxStyle.Information, "FlashcatUSB")
    End Sub

    Public Sub SetProgress(percent As Integer)
        Static LastPercent As Integer = -1
        If LastPercent = percent Then Exit Sub
        If GUI IsNot Nothing Then
            GUI.SetStatusPageProgress(percent)
        End If
    End Sub
    'Adds a GUI Progress Bar to a user tab
    Public Sub ProgressBar_Add(index As Integer, bar_left As Integer, bar_top As Integer, bar_width As Integer)
        If GUI IsNot Nothing Then
            If GUI.Controls.Contains(ScriptBar) Then GUI.Controls.Remove(ScriptBar)
            ScriptBar = New ProgressBar
            ScriptBar.Name = "ScriptProgressBar"
            ScriptBar.Left = bar_left
            ScriptBar.Top = bar_top
            ScriptBar.Width = bar_width
            ScriptBar.Height = 12
            GUI.AddControlToTable(index, ScriptBar)
        End If
    End Sub

    Public Sub ProgressBar_SetDevice(mem_dev As MemoryDeviceInstance)
        MemDevSelected = mem_dev
    End Sub

    Public Sub ProgressBar_Percent(percent As Integer)
        If ScriptBar Is Nothing AndAlso MemDevSelected IsNot Nothing Then
            GUI?.SetProgress(MemDevSelected, percent)
        ElseIf ScriptBar IsNot Nothing Then
            If ScriptBar.InvokeRequired Then
                Dim n As New cbProgressBarPercent(AddressOf ProgressBar_Percent)
                ScriptBar.Invoke(n, {percent})
            Else
                ScriptBar.Value = percent
            End If
        End If
    End Sub

    Public Sub ProgressBar_Dispose()
        ProgressBar_Percent(0)
        MemDevSelected = Nothing
        If ScriptBar Is Nothing Then Exit Sub
        If ScriptBar.InvokeRequired Then
            Dim n As New cbProgressBarDispose(AddressOf ProgressBar_Dispose)
            ScriptBar.Invoke(n)
        Else
            ScriptBar.Dispose()
            ScriptBar = Nothing
        End If
    End Sub

#End Region

    Public Sub RemoveAllTabs()
        GUI?.MyTabs_RemoveAll()
    End Sub

    Public Sub DoEvents()
        Application.DoEvents()
    End Sub

    Private Sub IF_WriteSuccessful(mydev As MemoryDeviceInstance, x_oper As MemControl_v2.XFER_Operation)
        GUI?.SuccessfulWriteOperation(mydev, x_oper)
    End Sub

    Private Sub IF_WriteFailed(mydev As MemoryDeviceInstance, Params As WriteParameters, ByRef ContinueOperation As Boolean)
        Dim TitleTxt As String = String.Format(RM.GetString("mem_verify_failed_at"), Hex(Params.Address).PadLeft(8, "0"c))
        TitleTxt &= vbCrLf & vbCrLf & RM.GetString("mem_ask_continue")
        If MsgBox(TitleTxt, MsgBoxStyle.YesNo, RM.GetString("mem_verify_failed_title")) = MsgBoxResult.No Then
            ContinueOperation = False 'Stop operation
        Else
            ContinueOperation = True
        End If
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
        my_params.OPER_MODE = MySettings.OPERATION_MODE
        my_params.SPI_AUTO = MySettings.SPI_AUTO
        my_params.SPI_CLOCK = MySettings.SPI_CLOCK_MAX
        my_params.SQI_CLOCK = MySettings.SQI_CLOCK_MAX
        my_params.SPI_EEPROM = MySettings.SPI_EEPROM
        my_params.PARALLEL_EEPROM = MySettings.PARALLEL_EEPROM
        my_params.I2C_INDEX = MySettings.I2C_INDEX
        my_params.I2C_SPEED = MySettings.I2C_SPEED
        my_params.I2C_ADDRESS = MySettings.I2C_ADDRESS
        my_params.NOR_READ_ACCESS = MySettings.NOR_READ_ACCESS
        my_params.NOR_WE_PULSE = MySettings.NOR_WE_PULSE
        my_params.NAND_Layout = MySettings.NAND_Layout
        Return my_params
    End Function

    Public Sub ECC_Init(mem_dev As MemoryDeviceUSB)
        If ECC_LIB.NAND_ECC_CFG Is Nothing OrElse (Not MySettings.ECC_FEATURE_ENABLED) Then Exit Sub
        Dim PNAND_IF As PNAND_Programmer = Nothing
        Dim SNAND_IF As SPINAND_Programmer = Nothing
        Dim page_size As UInt16 = 0
        Dim spare_size As UInt16 = 0
        If mem_dev.GetType() Is GetType(PNAND_Programmer) Then
            PNAND_IF = CType(mem_dev, PNAND_Programmer)
            PNAND_IF.ECC_ENG = Nothing
            page_size = PNAND_IF.MyFlashDevice.PAGE_SIZE
            spare_size = PNAND_IF.MyFlashDevice.PAGE_EXT
        ElseIf mem_dev.GetType() Is GetType(SPINAND_Programmer) Then
            SNAND_IF = CType(mem_dev, SPINAND_Programmer)
            SNAND_IF.ECC_ENG = Nothing
            page_size = SNAND_IF.MyFlashDevice.PAGE_SIZE
            spare_size = SNAND_IF.MyFlashDevice.PAGE_EXT
        End If
        PrintConsole("ECC Feature is enabled")
        For i = 0 To ECC_LIB.NAND_ECC_CFG.Length - 1
            If ECC_LIB.NAND_ECC_CFG(i).IsValid() Then
                If ECC_LIB.NAND_ECC_CFG(i).PageSize = page_size AndAlso ECC_LIB.NAND_ECC_CFG(i).SpareSize = spare_size Then
                    Dim new_ecc As ECC_LIB.Engine = New ECC_LIB.Engine(ECC_LIB.NAND_ECC_CFG(i).Algorithm, ECC_LIB.NAND_ECC_CFG(i).BitError, ECC_LIB.NAND_ECC_CFG(i).SymSize)
                    new_ecc.REVERSE_ARRAY = ECC_LIB.NAND_ECC_CFG(i).ReverseData
                    new_ecc.ECC_DATA_LOCATION = ECC_LIB.NAND_ECC_CFG(i).EccRegion
                    If PNAND_IF IsNot Nothing Then PNAND_IF.ECC_ENG = new_ecc
                    If SNAND_IF IsNot Nothing Then SNAND_IF.ECC_ENG = new_ecc
                    PrintConsole("Compatible profile found at index " & (i + 1).ToString)
                    Exit Sub
                End If
            End If
        Next
        PrintConsole("No compatible profile found")
    End Sub

    Private Sub Print_PCB_Version(usb_dev As FCUSB_DEVICE)
        Dim eeprom(5) As Byte
        usb_dev.USB_CONTROL_MSG_IN(USBREQ.EEPROM_RD, eeprom, (CUInt(6) << 16))
        If eeprom(0) = 255 OrElse eeprom(0) = 0 Then Exit Sub
        Dim pcb_version As String = Utilities.Bytes.ToChrString(eeprom)
        PrintConsole(String.Format("Hardware version: {0}", pcb_version))
    End Sub


End Module