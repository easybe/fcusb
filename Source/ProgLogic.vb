Imports FlashcatUSB.USB

Namespace Logic

    Public Class ISC_LOGIC_PROG
        Private sel_usb_dev As FCUSB_DEVICE

        Private IDCODE_PUB() As Byte = {&HE0, 0, 0, 0}
        Private ISC_ENABLE() As Byte = {&HC6, &H8, 0, 0}
        Private ISC_ERASE() As Byte = {&HE, &H4, 0, 0}
        Private ISC_PROGRAMDONE() As Byte = {&H5E, 0, 0, 0}
        Private LSC_INITADDRESS() As Byte = {&H46, 0, 0, 0}
        Private LSC_PROGINCRNV() As Byte = {&H70, 0, 0, 1}
        Private LSC_READ_STATUS() As Byte = {&H3C, 0, 0, 0}
        Private LSC_REFRESH() As Byte = {&H79, 0, 0, 0}

        Public Event PrintConsole(msg As String)
        Public Event SetProgress(value As Integer)

        Private Const MACHXO2_PAGE_SIZE As Integer = 16

        Public Sub New(usb_dev As FCUSB_DEVICE)
            Me.sel_usb_dev = usb_dev
        End Sub

        Public Function SSPI_Init(spi_mode As UInt32, spi_select As UInt32, speed_mhz As UInt32) As Boolean
            Try
                Dim w32 As UInt32 = (spi_select << 24) Or (spi_mode << 16) Or speed_mhz
                Return sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.SPI_INIT, Nothing, w32)
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Sub SSPI_SS(enabled As Boolean)
            If enabled Then
                sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.SPI_SS_ENABLE) 'SS=LOW
            Else
                sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.SPI_SS_DISABLE) 'SS=HIGH
            End If
        End Sub

        Public Function SSPI_WriteData(data() As Byte) As Boolean
            Dim result As Boolean = sel_usb_dev.USB_SETUP_BULKOUT(USBREQ.SPI_WR_DATA, Nothing, data, CUInt(data.Length))
            Utilities.Sleep(2)
            Return result
        End Function

        Public Function SSPI_ReadData(ByRef Data_In() As Byte) As Boolean
            Dim Success As Boolean = False
            Try
                Success = sel_usb_dev.USB_SETUP_BULKIN(USBREQ.SPI_RD_DATA, Nothing, Data_In, CUInt(Data_In.Length))
            Catch ex As Exception
            End Try
            Return Success
        End Function

        Public Function SSPI_WriteRead(WriteBuffer() As Byte, Optional ByRef ReadBuffer() As Byte = Nothing) As UInt32
            If WriteBuffer Is Nothing And ReadBuffer Is Nothing Then Return 0
            Dim TotalBytesTransfered As UInt32 = 0
            SSPI_SS(True)
            If (WriteBuffer IsNot Nothing) Then
                Dim Result As Boolean = SSPI_WriteData(WriteBuffer)
                If Result Then TotalBytesTransfered += CUInt(WriteBuffer.Length)
            End If
            If (ReadBuffer IsNot Nothing) Then
                Dim Result As Boolean = SSPI_ReadData(ReadBuffer)
                If Result Then TotalBytesTransfered += CUInt(ReadBuffer.Length)
            End If
            SSPI_SS(False)
            Return TotalBytesTransfered
        End Function

        Public Class SSPI_Status
            Public STATUS32 As UInt32
            Public ReadOnly Property DONE_FLAG As Boolean
            Public ReadOnly Property CHECK_STATUS As CFG_CHECK_STATUS
            Public ReadOnly Property CFG_IF_ENABLED As Boolean
            Public ReadOnly Property BUSY_FLAG As Boolean
            Public ReadOnly Property FAIL_FLAG As Boolean

            Sub New(cfg_register() As Byte)
                STATUS32 = Utilities.Bytes.ToUInt32(cfg_register)
                Me.DONE_FLAG = CBool((STATUS32 >> 8) And 1)
                Me.CFG_IF_ENABLED = CBool((STATUS32 >> 9) And 1)
                Me.BUSY_FLAG = CBool((STATUS32 >> 12) And 1)
                Me.FAIL_FLAG = CBool((STATUS32 >> 13) And 1)
                Me.CHECK_STATUS = CType(((STATUS32 >> 23) And 7), CFG_CHECK_STATUS)
            End Sub

            Public Enum CFG_CHECK_STATUS As Byte
                No_error = 0
                ID_error = 1
                CMD_error = 2
                CRC_error = 3
                Preamble_error = 4
                Abort_error = 5
                Overflow_error = 6
                SDM_EOF = 7
            End Enum

        End Class

        Private Function SSPI_Wait() As Boolean
            Dim MACHXO2_MAX_BUSY_LOOP As Integer = 128
            Dim counter As Integer = 0
            Dim current_status As SSPI_Status = Nothing
            Do
                current_status = SSPI_ReadStatus()
                Utilities.Sleep(10)
                counter += 1
                If counter = MACHXO2_MAX_BUSY_LOOP Then Return False 'TIMEOUT
                If current_status Is Nothing Then Return False 'ERROR
            Loop While current_status.BUSY_FLAG
            Return True
        End Function
        'If SPI Slave port is enabled, this will return 0x012BC043
        Public Function SSPI_ReadIdent() As UInt32
            Try
                Dim ID(3) As Byte
                SSPI_WriteRead(IDCODE_PUB, ID)
                Return Utilities.Bytes.ToUInt32(ID)
            Catch ex As Exception
            End Try
            Return 0
        End Function

        Private Function SSPI_ReadStatus() As SSPI_Status
            Try
                Dim STATUS(3) As Byte
                SSPI_WriteRead(LSC_READ_STATUS, STATUS)
                Return New SSPI_Status(STATUS)
            Catch ex As Exception
            End Try
            Return Nothing
        End Function
        'Fast! This will load an entire bitstream into the FPGA in 3 seconds.
        Public Function SSPI_ProgramMACHXO(logic() As Byte) As Boolean
            Dim bytes_left As Integer = logic.Length
            MainApp.PrintConsole("Programming FPGA with bitstream (" & bytes_left.ToString("#,###") & " bytes)")
            RaiseEvent SetProgress(0)
            Dim status As SSPI_Status = SSPI_ReadStatus()
            If status.BUSY_FLAG Then
                MainApp.PrintConsole("Error: FPGA device is busy") : Return False
            End If
            SSPI_WriteRead(ISC_ENABLE)
            status = SSPI_ReadStatus()
            If (status.FAIL_FLAG) Or (Not status.CFG_IF_ENABLED) Then
                MainApp.PrintConsole("Error: unable to enabling configuration interface") : Return False
            End If
            Dim erase_attemps As Integer = 0
            Dim erase_failed As Boolean
            Do
                erase_failed = False
                SSPI_WriteRead(ISC_ERASE)
                SSPI_Wait()
                status = SSPI_ReadStatus()
                If (status.FAIL_FLAG Or (Not status.CHECK_STATUS = SSPI_Status.CFG_CHECK_STATUS.No_error)) Then
                    Utilities.Sleep(200)
                    erase_failed = True
                    erase_attemps += 1
                    If erase_attemps = 3 Then
                        MainApp.PrintConsole("Error: logic erase command failed") : Return False
                    End If
                End If
            Loop While erase_failed
            SSPI_WriteRead(LSC_INITADDRESS)
            Dim spi_size As Integer = CInt(Math.Ceiling(logic.Length / MACHXO2_PAGE_SIZE) * 4) + logic.Length
            Dim spi_buffer(spi_size - 1) As Byte
            Dim buffer_ptr As Integer = 0
            Dim logic_ptr As Integer = 0
            While (bytes_left > 0)
                Dim data_count As Integer = Math.Min(bytes_left, MACHXO2_PAGE_SIZE)
                Array.Copy(LSC_PROGINCRNV, 0, spi_buffer, buffer_ptr, LSC_PROGINCRNV.Length)
                buffer_ptr += LSC_PROGINCRNV.Length
                Array.Copy(logic, logic_ptr, spi_buffer, buffer_ptr, data_count)
                buffer_ptr += data_count
                logic_ptr += data_count
                bytes_left -= data_count
            End While
            bytes_left = spi_buffer.Length
            buffer_ptr = 0
            While (bytes_left > 0)
                Dim data_count As Integer = Math.Min(bytes_left, (MACHXO2_PAGE_SIZE + LSC_PROGINCRNV.Length) * 128) '128 pages
                Dim buffer_out(data_count - 1) As Byte
                Array.Copy(spi_buffer, buffer_ptr, buffer_out, 0, buffer_out.Length)
                Dim setup As UInt32 = CUInt(((MACHXO2_PAGE_SIZE + LSC_PROGINCRNV.Length) << 16) Or data_count)
                Dim result As Boolean = sel_usb_dev.USB_SETUP_BULKOUT(USBREQ.SPI_REPEAT, Nothing, buffer_out, setup)
                If Not result Then Return False
                sel_usb_dev.USB_WaitForComplete()
                bytes_left -= data_count
                buffer_ptr += data_count
                Dim percent_done As Integer = CInt(((spi_buffer.Length - bytes_left) / spi_buffer.Length) * 100)
                If (percent_done > 100) Then percent_done = 100
                RaiseEvent SetProgress(percent_done)
            End While
            SSPI_WriteRead(ISC_PROGRAMDONE)
            SSPI_Wait()
            RaiseEvent SetProgress(0) 'Done, remove bar
            status = SSPI_ReadStatus()
            If Not status.DONE_FLAG Then Return False
            SSPI_WriteRead(LSC_REFRESH)
            status = SSPI_ReadStatus()
            If (Not status.BUSY_FLAG) AndAlso status.CHECK_STATUS = SSPI_Status.CFG_CHECK_STATUS.No_error Then
                Return True
            Else
                Return False
            End If
        End Function
        'For programming FlashcatUSB Professional PCB 5.0
        Public Function SSPI_ProgramICE(logic() As Byte) As Boolean
            Try
                SSPI_Init(3, 1, 24) 'MODE_3, CS_0, 24MHZ; PIN_FPGA_RESET=HIGH
                SSPI_SS(True) 'SS_LOW
                sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.PULSE_RESET) 'PCRESET_B=LOW; delay_us(200); PCRESET_B=HIGH
                SSPI_SS(False) 'SS_HIGH
                SSPI_WriteData({0}) '8 dummy clocks
                SSPI_SS(True) 'SS_LOW
                SSPI_WriteData(logic)
                SSPI_SS(False) 'SS_HIGH
                SSPI_WriteData({0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0})
                Dim Success As Boolean = SSPI_ICE_GetCDONE()
                If Success Then
                    sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.LOGIC_START) 'SMC_Init(); PIN_FPGA_RESET=LOW
                End If
                Return Success
            Catch ex As Exception
            End Try
            Return False
        End Function

        Private Function SSPI_ICE_GetCDONE() As Boolean
            Dim s(3) As Byte
            sel_usb_dev.USB_CONTROL_MSG_IN(USBREQ.LOGIC_STATUS, s)
            If (s(0) = 0) Then Return False
            Return True
        End Function

    End Class

    Public Module ProgTool

        Public Sub Bootloader_UpdateFirmware(usb_dev As FCUSB_DEVICE, board_firmware As String)
            Dim fw_ver As Single = 0
            If usb_dev.HWBOARD = FCUSB_BOARD.Mach1 Then
                fw_ver = MACH1_PCB2_FW
            ElseIf usb_dev.HWBOARD = FCUSB_BOARD.Professional_PCB5 Then
                fw_ver = PRO_PCB5_FW
            End If
            MainApp.PrintConsole(RM.GetString("connected_bl_mode"))
            MainApp.DoEvents()
            MainApp.SetStatus(RM.GetString("fw_update_performing")) 'Performing firmware unit update
            Utilities.Sleep(500)
            Dim Current_fw() As Byte = Utilities.GetResourceAsBytes(board_firmware)
            MainApp.SetStatus(String.Format(RM.GetString("fw_update_starting"), Format(Current_fw.Length, "#,###")))
            Dim result As Boolean = usb_dev.FirmwareUpdate(Current_fw, fw_ver)
            SetProgress(0)
            If result Then
                PrintConsole("Firmware update was a success!")
            Else
                MainApp.SetStatus(RM.GetString("fw_update_error"))
            End If
        End Sub

        Public Sub UpdateLogic(fcusb As FCUSB_DEVICE, Mode As DeviceMode, TargetVoltage As Voltage)
            Try
                If fcusb.IS_CONNECTED Then
                    If fcusb.HWBOARD = FCUSB_BOARD.Professional_PCB5 Then
                        FCUSBPRO_LoadBitstream(fcusb, Mode, TargetVoltage)
                    ElseIf fcusb.HWBOARD = FCUSB_BOARD.Mach1 Then
                        PrintConsole("Updating all FPGA logic", True)
                        MACH1_Init(fcusb, Mode, TargetVoltage)
                        PrintConsole("FPGA logic successfully updated", True)
                    End If
                End If
            Catch ex As Exception
            End Try
        End Sub

        Public Function FCUSBPRO_LoadBitstream(usb_dev As FCUSB_DEVICE, CurrentMode As DeviceMode, TargetVoltage As Voltage) As Boolean
            usb_dev.USB_VCC_OFF()
            Utilities.Sleep(100)
            If (Not usb_dev.HWBOARD = FCUSB_BOARD.Professional_PCB5) Then Return False
            Dim bit_data() As Byte = Nothing
            If Utilities.StringToSingle(usb_dev.FW_VERSION()) = PRO_PCB5_FW Then
                If TargetVoltage = Voltage.V1_8 Then
                    bit_data = Utilities.GetResourceAsBytes("PRO5_1V8.bit")
                ElseIf TargetVoltage = Voltage.V3_3 Then
                    bit_data = Utilities.GetResourceAsBytes("PRO5_3V.bit")
                End If
            End If
            SetDeviceVoltage(usb_dev, TargetVoltage)
            Dim SPI_CFG_IF As New ISC_LOGIC_PROG(usb_dev)
            Return SPI_CFG_IF.SSPI_ProgramICE(bit_data)
        End Function
        'This writes random data to the SMC and then reads it back and compares it
        Public Function SMC_Integrity_Check(usb_dev As FCUSB_DEVICE) As Boolean
            Dim buffer_out(63) As Byte
            Dim buffer_in() As Byte = Utilities.CreateRandomBuffer(64)
            Dim result As Boolean = usb_dev.USB_CONTROL_MSG_OUT(USBREQ.SMC_WR, buffer_in)
            If Not result Then Return False
            result = usb_dev.USB_CONTROL_MSG_IN(USBREQ.SMC_RD, buffer_out)
            If Not result Then Return False
            Return buffer_in.SequenceEqual(buffer_out)
        End Function

        Public Sub MACH1_EraseLogic(usb_dev As FCUSB_DEVICE)
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
                Dim voltage As Voltage = usb_dev.CurrentVCC
                usb_dev.USB_VCC_OFF()
                usb_dev.USB_VCC_ON(voltage)
            End If
        End Sub

        Public Function MACH1_Init(usb_dev As FCUSB_DEVICE, CurrentMode As DeviceMode, TargetVoltage As Voltage) As Boolean
            If Not usb_dev.HWBOARD = FCUSB_BOARD.Mach1 Then Return False
            SetDeviceVoltage(usb_dev, TargetVoltage)
            Dim cpld32 As UInt32 = usb_dev.LOGIC_GetVersion()
            If usb_dev.DEBUG_MODE Then Return True 'We dont want To update the FPGA
            Dim bit_data() As Byte = Nothing
            Dim svf_code As UInt32 = 0
            If CurrentMode = DeviceMode.SPI Or CurrentMode = DeviceMode.SPI_EEPROM Or CurrentMode = DeviceMode.SPI_NAND Then
                If TargetVoltage = Voltage.V1_8 And (Not cpld32 = MACH1_SPI_1V8) Then
                    bit_data = Utilities.GetResourceAsBytes("MACH1_SPI_1V8.bit")
                    svf_code = MACH1_SPI_1V8
                ElseIf TargetVoltage = Voltage.V3_3 And (Not cpld32 = MACH1_SPI_3V3) Then
                    bit_data = Utilities.GetResourceAsBytes("MACH1_SPI_3V.bit")
                    svf_code = MACH1_SPI_3V3
                End If
            Else
                If TargetVoltage = Voltage.V1_8 And (Not cpld32 = MACH1_FGPA_1V8) Then
                    bit_data = Utilities.GetResourceAsBytes("MACH1_1V8.bit")
                    svf_code = MACH1_FGPA_1V8
                ElseIf TargetVoltage = Voltage.V3_3 And (Not cpld32 = MACH1_FGPA_3V3) Then
                    bit_data = Utilities.GetResourceAsBytes("MACH1_3V3.bit")
                    svf_code = MACH1_FGPA_3V3
                End If
            End If
            If (bit_data IsNot Nothing) Then
                Return MACH1_ProgramLogic(usb_dev, bit_data, svf_code)
            End If
            Return True
        End Function

        Public Function MACH1_ProgramLogic(usb_dev As FCUSB_DEVICE, bit_data() As Byte, bit_code As UInt32) As Boolean
            Try
                Dim SPI_CFG_IF As New ISC_LOGIC_PROG(usb_dev)
                AddHandler SPI_CFG_IF.PrintConsole, AddressOf PrintConsole
                AddHandler SPI_CFG_IF.SetProgress, AddressOf SetProgress
                Dim SPI_INIT_RES As Boolean = SPI_CFG_IF.SSPI_Init(0, 1, 24) 'CS_1
                Dim SPI_ID As UInt32 = SPI_CFG_IF.SSPI_ReadIdent()
                If Not (SPI_ID = &H12BC043) Then
                    MACH1_EraseLogic(usb_dev)
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
        'Legacy method to program logic via JTAG
        Public Sub MACH1_ProgramSVF(usb_dev As FCUSB_DEVICE, svf_data() As Byte, svf_code As UInt32)
            Try
                PrintConsole("Programming on board FPGA with new logic", True)
                usb_dev.USB_VCC_OFF()
                Utilities.Sleep(1000)
                If Not usb_dev.JTAG_IF.Init() Then
                    PrintConsole("Error: unable to connect to on board FPGA via JTAG", True)
                    Exit Sub
                End If
                usb_dev.USB_VCC_ON(Voltage.V3_3)
                Dim svf_file() As String = Utilities.Bytes.ToCharStringArray(svf_data)
                RemoveHandler usb_dev.JTAG_IF.JSP.Progress, AddressOf SetProgress
                AddHandler usb_dev.JTAG_IF.JSP.Progress, AddressOf SetProgress
                PrintConsole("Programming SVF data into Logic device")
                usb_dev.LOGIC_SetVersion(&HFFFFFFFFUI)
                Dim result As Boolean = usb_dev.JTAG_IF.JSP.RunFile_SVF(svf_file)
                SetProgress(0)
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

    End Module

    Public Enum LOGIC_MODE
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

End Namespace