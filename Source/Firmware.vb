Imports FlashcatUSB.USB
Imports FlashcatUSB.MemoryInterface

Public Module Firmware

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
                modes.Add(DeviceMode.P_EEPROM)
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

    Public Function FirmwareCheck(usb_dev As FCUSB_DEVICE, supported_fw As Single) As Boolean
        If usb_dev.DEBUG_MODE Then Return True
        Dim current_fw As Single = Utilities.StringToSingle(usb_dev.FW_VERSION())
        If (Not current_fw = supported_fw) Then
            PrintConsole(String.Format(RM.GetString("sw_requires_fw"), supported_fw.ToString)) 'Software requires firmware version {0}
            PrintConsole(RM.GetString("fw_update_available"), True) 'Firmware update available, performing automatic update
            RebootToBootloader(usb_dev)
            Return False
        End If
        Return True
    End Function

    Public Sub RebootToBootloader(usb_dev As FCUSB_DEVICE)
        Utilities.Sleep(1000)
        If usb_dev.HasLogic() Then
            usb_dev.USB_CONTROL_MSG_OUT(USBREQ.FW_REBOOT, Nothing, &HFFFFFFFFUI) 'Removes firmware version
        Else
            usb_dev.USB_CONTROL_MSG_OUT(USBREQ.JUMP_BOOT) 'Jumps to DFU bootloader
        End If
        usb_dev.Disconnect()
        DoEvents()
        Utilities.Sleep(300)
    End Sub

    Public Sub AVR_UpdateFirmware(usb_dev As FCUSB_DEVICE)
        Dim DFU_IF As DFU_Programmer = CType(CreateProgrammer(usb_dev, DeviceMode.DFU), DFU_Programmer)
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


End Module
