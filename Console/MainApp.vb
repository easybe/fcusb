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
    'Public GUI As MainForm
    Public MyConsole As ConsoleMode
    Public FlashDatabase As New FlashDatabase 'This contains definitions of all of the supported Flash devices
    Public MySettings As New FlashcatSettings
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
        CreateGrayCodeTable()
        If NAND_ECC_CFG Is Nothing Then NAND_ECC_CFG = GenerateLocalEccConfigurations()
        Thread.CurrentThread.Name = "rootApp"
        Platform = Environment.OSVersion.Platform & " (" & GetOsBitsString() & ")"
        ScriptApplication.AddInternalMethods()
        'ScriptGUI.AddInternalMethods()
        USBCLIENT.StartService()
        'Application.Run(GUI)

        'Dim arg_list As New List(Of String)
        'arg_list.Add("-WRITE")
        'arg_list.Add("-READ")
        'arg_list.Add("-PNAND")
        'arg_list.Add("-SPI")
        'arg_list.Add("-MHZ")
        'arg_list.Add("32")
        'arg_list.Add("-FILE")
        'arg_list.Add("Tesla2.bin")
        'arg_list.Add("-LENGTH")
        'arg_list.Add("1638400")
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

#Region "Bit Swapping / Endian Feature, and Gray Code tables"
    'FILE-->MEMORY
    Public Sub BitSwap_Forward(ByRef data() As Byte)
        Select Case MySettings.BIT_ENDIAN
            Case BitEndianMode.BigEndian16
                Utilities.ChangeEndian16_MSB(data)
            Case BitEndianMode.LittleEndian32_8bit
                Utilities.ChangeEndian32_LSB8(data)
            Case BitEndianMode.LittleEndian32_16bit
                Utilities.ChangeEndian32_LSB16(data)
        End Select
        Select Case MySettings.BIT_SWAP
            Case BitSwapMode.Bits_8
                Utilities.ReverseBits_Byte(data)
            Case BitSwapMode.Bits_16
                Utilities.ReverseBits_HalfWord(data)
            Case BitSwapMode.Bits_32
                Utilities.ReverseBits_Word(data)
        End Select
    End Sub
    'MEMORY-->FILE
    Public Sub BitSwap_Reverse(ByRef data() As Byte)
        Select Case MySettings.BIT_SWAP
            Case BitSwapMode.Bits_8
                Utilities.ReverseBits_Byte(data)
            Case BitSwapMode.Bits_16
                Utilities.ReverseBits_HalfWord(data)
            Case BitSwapMode.Bits_32
                Utilities.ReverseBits_Word(data)
        End Select
        Select Case MySettings.BIT_ENDIAN
            Case BitEndianMode.BigEndian16
                Utilities.ChangeEndian16_MSB(data)
            Case BitEndianMode.LittleEndian32_8bit
                Utilities.ChangeEndian32_LSB8(data)
            Case BitEndianMode.LittleEndian32_16bit
                Utilities.ChangeEndian32_LSB16(data)
        End Select
    End Sub
    'Number of bytes needed
    Public Function BitSwap_Offset() As Integer
        Dim bits_needed As Integer = 0
        Select Case MySettings.BIT_SWAP
            Case BitSwapMode.Bits_16
                bits_needed = 2
            Case BitSwapMode.Bits_32
                bits_needed = 4
        End Select
        Select Case MySettings.BIT_ENDIAN
            Case BitEndianMode.BigEndian16
                bits_needed = 4
            Case BitEndianMode.LittleEndian32_16bit
                bits_needed = 4
            Case BitEndianMode.LittleEndian32_8bit
                bits_needed = 4
        End Select
        Return bits_needed
    End Function

    Public gray_code_table_reverse(255) As Byte
    Public gray_code_table(255) As Byte

    Public Sub CreateGrayCodeTable()
        For i As Integer = 0 To 255
            Dim data_in() As Byte = {CByte((i >> 1) Xor i)}
            gray_code_table(i) = data_in(0)
            Utilities.ReverseBits_Byte(data_in)
            gray_code_table_reverse(i) = data_in(0)
        Next
    End Sub

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

    Public Function GetDevices_SPI_EEPROM() As SPI_NOR()
        Dim spi_eeprom As New List(Of SPI_NOR)
        Dim d() As Device = FlashDatabase.GetFlashDevices(MemoryType.SERIAL_NOR)
        For Each dev As SPI_NOR In d
            If DirectCast(dev, SPI_NOR).ProgramMode = SPI_ProgramMode.SPI_EEPROM Then
                spi_eeprom.Add(dev)
            ElseIf DirectCast(dev, SPI_NOR).ProgramMode = SPI_ProgramMode.Nordic Then
                spi_eeprom.Add(dev)
            End If
        Next
        Return spi_eeprom.ToArray
    End Function

    Public Function SPIEEPROM_Configure(SPI_IF As SPI_Programmer, eeprom_name As String) As Boolean
        Dim all_eeprom_devices() As SPI_NOR = GetDevices_SPI_EEPROM()
        Dim eeprom As SPI_NOR = Nothing
        For Each ee_dev In all_eeprom_devices
            If ee_dev.NAME.Equals(eeprom_name) Then
                eeprom = ee_dev
            End If
        Next
        If eeprom Is Nothing Then Return False
        Dim nRF24_mode As Boolean = False
        SPI_IF.SPIBUS_Setup(GetMaxSpiClock(MAIN_FCUSB.HWBOARD, SPI_SPEED.MHZ_1))
        Select Case eeprom.NAME
            Case "Nordic nRF24LE1"
                nRF24_mode = True
            Case "Nordic nRF24LU1+ (16KB)"
                nRF24_mode = True
            Case "Nordic nRF24LU1+ (32KB)"
                nRF24_mode = True
        End Select
        If nRF24_mode Then
            MAIN_FCUSB.USB_VCC_ON()
            Utilities.Sleep(100)
            SPI_IF.SetProgPin(True) 'Sets PROG.PIN to HIGH
            SPI_IF.SetProgPin(False) 'Sets PROG.PIN to LOW
            SPI_IF.SetProgPin(True) 'Sets PROG.PIN to HIGH
            Utilities.Sleep(10)
            If (MAIN_FCUSB.HWBOARD = FCUSB_BOARD.Professional_PCB5) Then
                SPI_IF.SPIBUS_Setup(GetMaxSpiClock(MAIN_FCUSB.HWBOARD, SPI_SPEED.MHZ_8))
            ElseIf (MAIN_FCUSB.HWBOARD = FCUSB_BOARD.XPORT_PCB2) Then
                SPI_IF.SPIBUS_Setup(GetMaxSpiClock(MAIN_FCUSB.HWBOARD, SPI_SPEED.MHZ_8))
            Else
                SPI_IF.SPIBUS_Setup(GetMaxSpiClock(MAIN_FCUSB.HWBOARD, SPI_SPEED.MHZ_8))
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
                    FCUSBPRO_Bootloader(usb_dev, "PCB5_Source.bin") : Exit Sub
                End If
                PrintConsole(String.Format(RM.GetString("connected_fw_ver"), {"FlashcatUSB Pro (PCB 5.x)", fw_str}))
                If Not FirmwareCheck(usb_dev, PRO_PCB5_FW) Then Exit Sub
            Case FCUSB_BOARD.Mach1 'Designed for high-density/high-speed devices (such as 1Gbit+ NOR/MLC NAND)
                If usb_dev.BOOTLOADER() Then
                    FCUSBPRO_Bootloader(usb_dev, "Mach1_v2_Source.bin") : Exit Sub
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
                If Not FCUSBPRO_PCB5_Init(usb_dev, MySettings.OPERATION_MODE) Then
                    PrintConsole("Error: unable to load FPGA bitstream", True)
                    Exit Sub
                End If
            Case FCUSB_BOARD.Mach1
                If Not FCUSBMACH1_Init(usb_dev, MySettings.OPERATION_MODE) Then Exit Sub
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

    Public Function Connected_Event(PRG_IF As MemoryDeviceUSB, block_size As Integer) As MemoryDeviceInstance
        Try
            Utilities.Sleep(150) 'Some devices (such as Spansion 128mbit devices) need a delay here
            Dim dev_inst As MemoryDeviceInstance = MEM_IF.Add(MAIN_FCUSB, PRG_IF)
            AddHandler dev_inst.PrintConsole, AddressOf MainApp.PrintConsole
            AddHandler dev_inst.SetStatus, AddressOf MainApp.SetStatus
            AddHandler dev_inst.WriteOperationSucceded, AddressOf MainApp.IF_WriteSuccessful
            AddHandler dev_inst.WriteOperationFailed, AddressOf MainApp.IF_WriteFailed
            dev_inst.PreferredBlockSize = block_size
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

#Region "FlashcatUSB Pro and Mach1"

    Private Sub FCUSBPRO_Bootloader(usb_dev As FCUSB_DEVICE, board_firmware As String)
        Dim fw_ver As Single = 0
        If usb_dev.HWBOARD = FCUSB_BOARD.Mach1 Then
            fw_ver = MACH1_PCB2_FW
        ElseIf usb_dev.HWBOARD = FCUSB_BOARD.Professional_PCB5 Then
            fw_ver = PRO_PCB5_FW
        End If
        MainApp.PrintConsole(RM.GetString("connected_bl_mode"))
        'GUI.UpdateStatusMessage(RM.GetString("device_mode"), RM.GetString("bootloader_mode"))
        'Application.DoEvents()
        MainApp.SetStatus(RM.GetString("fw_update_performing")) 'Performing firmware unit update
        Utilities.Sleep(500)
        Dim Current_fw() As Byte = Utilities.GetResourceAsBytes(board_firmware)
        MainApp.SetStatus(String.Format(RM.GetString("fw_update_starting"), Format(Current_fw.Length, "#,###")))
        Dim result As Boolean = usb_dev.FirmwareUpdate(Current_fw, fw_ver)
        SetProgress(100)
        If result Then
            PrintConsole("Firmware update was a success!")
        Else
            SetStatus(RM.GetString("fw_update_error"))
        End If
    End Sub

    Private Sub FCUSBPRO_SetDeviceVoltage(usb_dev As FCUSB_DEVICE, Optional silent As Boolean = False)
        Dim console_message As String
        If MySettings.VOLT_SELECT = Voltage.V1_8 Then
            console_message = String.Format(RM.GetString("voltage_set_to"), "1.8V")
            usb_dev.USB_VCC_ON(Voltage.V1_8)
        Else
            MySettings.VOLT_SELECT = Voltage.V3_3
            console_message = String.Format(RM.GetString("voltage_set_to"), "3.3V")
            usb_dev.USB_VCC_ON(Voltage.V3_3)
        End If
        If Not silent Then PrintConsole(console_message)
        Utilities.Sleep(200)
    End Sub

    Public Sub FCUSBPRO_Update_Logic()
        Try
            If MAIN_FCUSB.IS_CONNECTED Then
                If MAIN_FCUSB.HWBOARD = FCUSB_BOARD.Professional_PCB5 Then
                    FCUSBPRO_PCB5_Init(MAIN_FCUSB, MySettings.OPERATION_MODE)
                ElseIf MAIN_FCUSB.HWBOARD = FCUSB_BOARD.Mach1 Then
                    PrintConsole("Updating all FPGA logic", True)
                    FCUSBMACH1_Init(MAIN_FCUSB, MySettings.OPERATION_MODE)
                    PrintConsole("FPGA logic successfully updated", True)
                End If
            End If
        Catch ex As Exception
        End Try
    End Sub

    Public Function FCUSBPRO_PCB5_Init(usb_dev As FCUSB_DEVICE, CurrentMode As DeviceMode) As Boolean
        usb_dev.USB_VCC_OFF()
        Utilities.Sleep(100)
        If (Not usb_dev.HWBOARD = FCUSB_BOARD.Professional_PCB5) Then Return False
        Dim bit_data() As Byte = Nothing
        If Utilities.StringToSingle(usb_dev.FW_VERSION()) = PRO_PCB5_FW Then
            If MySettings.VOLT_SELECT = Voltage.V1_8 Then
                bit_data = Utilities.GetResourceAsBytes("PRO5_1V8.bit")
                usb_dev.USB_VCC_ON(Voltage.V1_8)
            ElseIf MySettings.VOLT_SELECT = Voltage.V3_3 Then
                bit_data = Utilities.GetResourceAsBytes("PRO5_3V.bit")
                usb_dev.USB_VCC_ON(Voltage.V3_3)
            End If
        End If
        Dim SPI_CFG_IF As New ISC_LOGIC_PROG(MAIN_FCUSB)
        Return SPI_CFG_IF.SSPI_ProgramICE(bit_data)
    End Function

    Private Enum LOGIC_MODE
        NotSelected 'Default
        SPI_3V 'Standard GPIO/SPI @ 3.3V
        SPI_1V8 'Standard GPIO/SPI @ 1.8V
        QSPI_3V
        QSPI_1V8
        I2C 'I2C only mode @ 3.3V
        JTAG 'JTAG mode @ 3.3V
        NAND_1V8 'NAND mode @ 1.8V
        NAND_3V3 'NAND mode @ 3.3V
        HF_1V8 'HyperFlash @ 1.8V
        HF_3V3 'HyperFlash @ 3.3V
    End Enum

    Public Sub MACH1_FPGA_ERASE(usb_dev As FCUSB_DEVICE)
        PrintConsole("Erasing FPGA device", True)
        MEM_IF.Clear() 'Remove all devices that are on this usb port
        Dim svf_data() As Byte = Utilities.GetResourceAsBytes("MACH1_ERASE.svf")
        Dim jtag_successful As Boolean = usb_dev.JTAG_IF.Init()
        If (Not jtag_successful) Then
            PrintConsole("Error: failed to connect to FPGA via JTAG")
            Exit Sub
        End If
        Dim svf_file() As String = Utilities.Bytes.ToCharStringArray(svf_data)
        usb_dev.LOGIC_SetVersion(&HFFFFFFFFUI)
        Dim result As Boolean = usb_dev.JTAG_IF.JSP.RunFile_SVF(svf_file)
        If (Not result) Then
            Dim err_msg As String = "FPGA erase failed"
            PrintConsole(err_msg, True)
            Exit Sub
        Else
            PrintConsole("FPGA erased successfully", True)
            usb_dev.USB_VCC_OFF()
            FCUSBPRO_SetDeviceVoltage(usb_dev)
        End If
    End Sub

    Public Function FCUSBMACH1_Init(usb_dev As FCUSB_DEVICE, CurrentMode As DeviceMode) As Boolean
        If Not usb_dev.HWBOARD = FCUSB_BOARD.Mach1 Then Return False
        FCUSBPRO_SetDeviceVoltage(usb_dev) 'Power on CPLD
        Dim cpld32 As UInt32 = usb_dev.LOGIC_GetVersion()
        If IS_DEBUG_VER Then Return True 'We dont want to update the FPGA
        Dim bit_data() As Byte = Nothing
        Dim svf_code As UInt32 = 0
        If CurrentMode = DeviceMode.SPI Or CurrentMode = DeviceMode.SPI_EEPROM Or CurrentMode = DeviceMode.SPI_NAND Then
            If MySettings.VOLT_SELECT = Voltage.V1_8 And (Not cpld32 = MACH1_SPI_1V8) Then
                bit_data = Utilities.GetResourceAsBytes("MACH1_SPI_1V8.bit")
                svf_code = MACH1_SPI_1V8
            ElseIf MySettings.VOLT_SELECT = Voltage.V3_3 And (Not cpld32 = MACH1_SPI_3V3) Then
                bit_data = Utilities.GetResourceAsBytes("MACH1_SPI_3V.bit")
                svf_code = MACH1_SPI_3V3
            End If
        Else
            If MySettings.VOLT_SELECT = Voltage.V1_8 And (Not cpld32 = MACH1_FGPA_1V8) Then
                bit_data = Utilities.GetResourceAsBytes("MACH1_1V8.bit")
                svf_code = MACH1_FGPA_1V8
            ElseIf MySettings.VOLT_SELECT = Voltage.V3_3 And (Not cpld32 = MACH1_FGPA_3V3) Then
                bit_data = Utilities.GetResourceAsBytes("MACH1_3V3.bit")
                svf_code = MACH1_FGPA_3V3
            End If
        End If
        If (bit_data IsNot Nothing) Then
            Return MACH1_ProgramLogic(usb_dev, bit_data, svf_code)
        End If
        Return True
    End Function

    Private Function MACH1_ProgramLogic(usb_dev As FCUSB_DEVICE, bit_data() As Byte, bit_code As UInt32) As Boolean
        Try
            Dim SPI_CFG_IF As New ISC_LOGIC_PROG(usb_dev)
            AddHandler SPI_CFG_IF.PrintConsole, AddressOf PrintConsole
            AddHandler SPI_CFG_IF.SetProgress, AddressOf SetProgress
            Dim SPI_INIT_RES As Boolean = SPI_CFG_IF.SSPI_Init(0, 1, 24) 'CS_1
            Dim SPI_ID As UInt32 = SPI_CFG_IF.SSPI_ReadIdent()
            If Not (SPI_ID = &H12BC043) Then
                MACH1_FPGA_ERASE(usb_dev)
                SPI_CFG_IF.SSPI_Init(0, 1, 24)
                SPI_ID = SPI_CFG_IF.SSPI_ReadIdent()
            End If
            If Not (SPI_ID = &H12BC043) Then
                PrintConsole("FPGA error: unable to communicate via SPI", True)
                Return False
            End If
            PrintConsole("Programming on board FPGA with new logic", True)
            If SPI_CFG_IF.SSPI_ProgramMACHXO(bit_data) Then
                Dim status_msg As String = "FPGA device successfully programmed"
                PrintConsole(status_msg, True)
                usb_dev.LOGIC_SetVersion(bit_code)
                Return True
            Else
                Dim status_msg As String = "FPGA device programming failed"
                PrintConsole(status_msg, True)
                Return False
            End If
        Catch ex As Exception
        End Try
        Return True
    End Function

    Private Sub ProgramSVF(usb_dev As FCUSB_DEVICE, svf_data() As Byte, svf_code As UInt32)
        Try
            PrintConsole("Programming on board FPGA with new logic", True)
            usb_dev.USB_VCC_OFF()
            Utilities.Sleep(1000)
            If Not usb_dev.JTAG_IF.Init() Then
                PrintConsole("Error: unable to connect to on board FPGA via JTAG", True)
                Exit Sub
            End If
            usb_dev.USB_VCC_ON(MySettings.VOLT_SELECT)
            Dim svf_file() As String = Utilities.Bytes.ToCharStringArray(svf_data)
            RemoveHandler usb_dev.JTAG_IF.JSP.Progress, AddressOf SetProgress
            AddHandler usb_dev.JTAG_IF.JSP.Progress, AddressOf SetProgress
            PrintConsole("Programming SVF data into Logic device")
            usb_dev.LOGIC_SetVersion(&HFFFFFFFFUI)
            Dim result As Boolean = usb_dev.JTAG_IF.JSP.RunFile_SVF(svf_file)
            SetProgress(100)
            If result Then
                PrintConsole("FPGA successfully programmed!", True)
                usb_dev.LOGIC_SetVersion(svf_code)
            Else
                PrintConsole("Error, unable to program in-circuit FPGA", True)
                Exit Sub
            End If
            Utilities.Sleep(250)
            usb_dev.USB_CONTROL_MSG_OUT(USBREQ.FW_REBOOT, Nothing, 0) 'We need to reboot to clean up USB memory
            Utilities.Sleep(250)
        Catch ex As Exception
            PrintConsole("Exception in programming FPGA", True)
        End Try
    End Sub

#End Region

#Region "Shell User Interface"

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

    Public Sub ProgressBar_Add(index As Integer, bar_left As Integer, bar_top As Integer, bar_width As Integer)
        MyConsole.Progress_Create()
    End Sub

    Public Sub ProgressBar_Percent(percent As Integer)
        MyConsole.Progress_Set(percent)
    End Sub

    Public Sub ProgressBar_Dispose()
        MyConsole.Progress_Remove()
    End Sub

#End Region

    Public Function GetCompatibleScripts(CPUID As UInteger) As String(,)
        Dim Autorun As New IO.FileInfo(ScriptPath & "autorun.ini")
        If Autorun.Exists Then
            Dim autoscripts(,) As String = Nothing
            If ProcessAutorun(Autorun, CPUID, autoscripts) Then
                Return autoscripts
            End If
        End If
        Return Nothing
    End Function

    Public Function ProcessAutorun(Autorun As IO.FileInfo, ID As UInteger, ByRef scripts(,) As String) As Boolean
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
            PrintConsole("Error processing Autorun.ini")
        End Try
        Return False
    End Function

    Public Function GetOsBitsString() As String
        If Environment.Is64BitOperatingSystem Then
            Return "64 bit"
        Else
            Return "32 bit"
        End If
    End Function
    'Returns all of the modes we can support (first one is the default)
    Public Function GetSupportedModes(usb_dev As FCUSB_DEVICE) As DeviceMode()
        Dim modes As New List(Of DeviceMode)
        Select Case usb_dev.HWBOARD
            Case FCUSB_BOARD.Classic
                modes.Add(DeviceMode.SPI)
                modes.Add(DeviceMode.SQI)
                modes.Add(DeviceMode.SPI_NAND)
                modes.Add(DeviceMode.I2C_EEPROM)
                modes.Add(DeviceMode.SPI_EEPROM)
                modes.Add(DeviceMode.Microwire)
                modes.Add(DeviceMode.ONE_WIRE)
                modes.Add(DeviceMode.JTAG)
            Case FCUSB_BOARD.XPORT_PCB2
                modes.Add(DeviceMode.PNOR)
                modes.Add(DeviceMode.PNAND)
                modes.Add(DeviceMode.FWH)
                modes.Add(DeviceMode.SPI)
                modes.Add(DeviceMode.SQI)
                modes.Add(DeviceMode.SPI_NAND)
                modes.Add(DeviceMode.I2C_EEPROM)
                modes.Add(DeviceMode.SPI_EEPROM)
                modes.Add(DeviceMode.SPI_NAND)
                modes.Add(DeviceMode.EPROM)
                modes.Add(DeviceMode.JTAG)
            Case FCUSB_BOARD.Professional_PCB5
                modes.Add(DeviceMode.SPI)
                modes.Add(DeviceMode.I2C_EEPROM)
                modes.Add(DeviceMode.SPI_EEPROM)
                modes.Add(DeviceMode.SPI_NAND)
                modes.Add(DeviceMode.Microwire)
                modes.Add(DeviceMode.SQI)
                modes.Add(DeviceMode.JTAG)
            Case FCUSB_BOARD.Mach1
                modes.Add(DeviceMode.SPI)
                modes.Add(DeviceMode.SPI_NAND)
                modes.Add(DeviceMode.SQI)
                modes.Add(DeviceMode.PNOR)
                modes.Add(DeviceMode.PNAND)
                modes.Add(DeviceMode.HyperFlash)
        End Select
        Return modes.ToArray()
    End Function

    Private Function FirmwareCheck(usb_dev As FCUSB_DEVICE, supported_fw As Single) As Boolean
        Dim current_fw As Single = Utilities.StringToSingle(usb_dev.FW_VERSION())
        If IS_DEBUG_VER Then Return True
        If (Not current_fw = supported_fw) Then
            PrintConsole(String.Format(RM.GetString("sw_requires_fw"), supported_fw.ToString)) 'Software requires firmware version {0}
            PrintConsole(RM.GetString("fw_update_available"), True) 'Firmware update available, performing automatic update
            RebootToBootloader(usb_dev)
            Return False
        End If
        Return True
    End Function

    Private Sub RebootToBootloader(usb_dev As FCUSB_DEVICE)
        Utilities.Sleep(1000)
        If usb_dev.HasLogic() Then
            usb_dev.USB_CONTROL_MSG_OUT(USBREQ.FW_REBOOT, Nothing, &HFFFFFFFFUI) 'Removes firmware version
        Else
            usb_dev.USB_CONTROL_MSG_OUT(USBREQ.JUMP_BOOT) 'Jumps to DFU bootloader
        End If
        usb_dev.Disconnect()
        Utilities.Sleep(300)
    End Sub

    Private Sub AVR_UpdateFirmware(usb_dev As FCUSB_DEVICE)
        Dim DFU_IF As DFU_Programmer = CType(CreateProgrammer(MAIN_FCUSB, DeviceMode.DFU), DFU_Programmer)
        PrintConsole("Initializing DFU programming mode")
        AddHandler DFU_IF.SetProgress, AddressOf MainApp.SetProgress
        Dim emb_fw_hex() As Byte = Nothing
        Dim hw_model As String = ""
        If usb_dev.USBHANDLE.UsbRegistryInfo.Vid = &H3EB AndAlso usb_dev.USBHANDLE.UsbRegistryInfo.Pid = &H2FF9 Then
            emb_fw_hex = Utilities.GetResourceAsBytes("XPORT_PCB2.hex")
            hw_model = "XPORT (PCB 2.x)"
        ElseIf usb_dev.USBHANDLE.UsbRegistryInfo.Vid = &H3EB AndAlso usb_dev.USBHANDLE.UsbRegistryInfo.Pid = &H2FF0 Then
            emb_fw_hex = Utilities.GetResourceAsBytes("CLASSIC_U2.hex")
            hw_model = "Classic (U2)"
        ElseIf usb_dev.USBHANDLE.UsbRegistryInfo.Vid = &H3EB AndAlso usb_dev.USBHANDLE.UsbRegistryInfo.Pid = &H2FF4 Then
            emb_fw_hex = Utilities.GetResourceAsBytes("CLASSIC_U4.hex")
            hw_model = "Classic (U4)"
        End If
        SetStatus("Programming new FlashcatUSB " & hw_model & " firmware (" & emb_fw_hex.Length.ToString("#,###") & " bytes)")
        DFU_IF.DeviceInit()
        If (Not DFU_IF.EraseDevice()) Then
            SetStatus("Error: device erase was not successful") : Exit Sub
        End If
        Dim hex_stream As New IO.StreamReader(New IO.MemoryStream(emb_fw_hex))
        Dim ihex_tool As New IHEX.StreamReader(hex_stream)
        If ihex_tool.IsValid Then
            Dim emb_firmware(CInt(ihex_tool.Length) - 1) As Byte
            ihex_tool.Read(emb_firmware, 0, emb_firmware.Length)
            If DFU_IF.WriteData(0, emb_firmware) Then
                Utilities.Sleep(250)
                DFU_IF.RunApp() 'Start application (hardware reset)
            Else
                SetStatus("Error: programming firmware via DFU mode was not successful")
            End If
        Else
            SetStatus("Error: file is corrupt or not a valid Intel Hex file")
        End If
        SetProgress(100) 'Hides progress bar
    End Sub

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
        my_params.I2C_INDEX = MyConsole.MyOperation.I2C_EEPROM
        my_params.I2C_SPEED = I2C_SPEED_MODE._400kHz
        my_params.I2C_ADDRESS = &HA0
        my_params.NOR_READ_ACCESS = MainApp.MySettings.NOR_READ_ACCESS
        my_params.NOR_WE_PULSE = MainApp.MySettings.NOR_WE_PULSE
        my_params.NAND_Layout = MainApp.MySettings.NAND_Layout
        Return my_params
    End Function

End Module