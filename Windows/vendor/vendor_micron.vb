Public Class vendor_micron
    Private FCUSB_PROG As MemoryDeviceUSB

    Sub New(mem_dev_programmer As MemoryDeviceUSB)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        FCUSB_PROG = mem_dev_programmer
    End Sub

    Private Sub vendor_micron_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        group_sr.Enabled = False
        group_nonvol.Enabled = False
        cmd_write_config.Enabled = False
    End Sub

    Private Sub cmd_read_config_Click(sender As Object, e As EventArgs) Handles cmd_read_config.Click
        Try
            cmd_read_config.Enabled = False
            PrintConsole("Reading status and non-vol registers")
            Dim sr(0) As Byte
            If FCUSB_PROG.GetType Is GetType(SPI.SPI_Programmer) Then
                sr = DirectCast(FCUSB_PROG, SPI.SPI_Programmer).ReadStatusRegister(1)
            ElseIf FCUSB_PROG.GetType Is GetType(SPI.SQI_Programmer) Then
                sr = DirectCast(FCUSB_PROG, SPI.SQI_Programmer).ReadStatusRegister(1)
            End If
            Dim cr(1) As Byte
            If FCUSB_PROG.GetType Is GetType(SPI.SPI_Programmer) Then
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).SPIBUS_WriteRead({&HB5}, cr)
            ElseIf FCUSB_PROG.GetType Is GetType(SPI.SQI_Programmer) Then
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).SQIBUS_WriteRead({&HB5}, cr)
            End If
            PrintConsole("Status register: 0x" & Hex(sr(0)).PadLeft(2, "0"))
            PrintConsole("Nonvol config register: 0x" & Hex(cr(1)).PadLeft(2, "0") & Hex(cr(0)).PadLeft(2, "0"))
            SetStatus("Loaded current nonvolatile configuration settings")
            LoadCurrentConfigBits()
            group_nonvol.Enabled = True
            group_sr.Enabled = True
        Catch ex As Exception
        Finally
            cmd_read_config.Enabled = True
            cmd_write_config.Enabled = True 'We can now write changes
        End Try
    End Sub

    Private Sub LoadCurrentConfigBits()
        Dim sr() As Byte = Nothing
        If FCUSB_PROG.GetType Is GetType(SPI.SPI_Programmer) Then
            sr = DirectCast(FCUSB_PROG, SPI.SPI_Programmer).ReadStatusRegister(1)
        ElseIf FCUSB_PROG.GetType Is GetType(SPI.SQI_Programmer) Then
            sr = DirectCast(FCUSB_PROG, SPI.SQI_Programmer).ReadStatusRegister(1)
        End If
        Dim cr(1) As Byte
        If FCUSB_PROG.GetType Is GetType(SPI.SPI_Programmer) Then
            DirectCast(FCUSB_PROG, SPI.SPI_Programmer).SPIBUS_WriteRead({&HB5}, cr)
        ElseIf FCUSB_PROG.GetType Is GetType(SPI.SQI_Programmer) Then
            DirectCast(FCUSB_PROG, SPI.SQI_Programmer).SQIBUS_WriteRead({&HB5}, cr)
        End If
        cb_block_bp0.Checked = ((sr(0) >> 2) And 1)
        cb_block_bp1.Checked = ((sr(0) >> 3) And 1)
        cb_block_bp2.Checked = ((sr(0) >> 4) And 1)
        cb_block_bp3.Checked = ((sr(0) >> 6) And 1)
        cb_protected_area.SelectedIndex = ((sr(0) >> 5) And 1)
        cb_status_ro.Checked = ((sr(0) >> 7) And 1)
        cb_address_mode.SelectedIndex = (cr(0) And 1)
        cb_segment.SelectedIndex = ((cr(0) >> 1) And 1)
        If ((cr(0) >> 3) And 1) = 0 Then
            cb_serial_mode.SelectedIndex = 2 'QUAD MODE
        ElseIf ((cr(0) >> 2) And 1) = 0 Then
            cb_serial_mode.SelectedIndex = 1 'DUAL mode
        Else
            cb_serial_mode.SelectedIndex = 0 'SPI mode
        End If
        If ((cr(0) >> 4) And 1) = 1 Then
            cb_reset_disable.Checked = False
        Else
            cb_reset_disable.Checked = True
        End If
        Dim output_drv As UShort = ((cr(1) And 1) << 2) Or (cr(0) >> 6)
        Select Case output_drv
            Case 1 '90 Ohms
                cb_output_drv.SelectedIndex = 0
            Case 2 '60 Ohms
                cb_output_drv.SelectedIndex = 1
            Case 3 '45 Ohms
                cb_output_drv.SelectedIndex = 2
            Case 5 '20 Ohms
                cb_output_drv.SelectedIndex = 4
            Case 6 '15 Ohms
                cb_output_drv.SelectedIndex = 5
            Case Else '30 Ohms
                cb_output_drv.SelectedIndex = 3
        End Select
        Dim xip_val As UInt16 = ((cr(1) And &HE) >> 1)
        Select Case xip_val
            Case 0 'XIP: Fast Read
                cb_xip_mode.SelectedIndex = 0
            Case 1 'XIP: Dual Output Fast Read
                cb_xip_mode.SelectedIndex = 1
            Case 2 'XIP: Dual I/O Fast Read
                cb_xip_mode.SelectedIndex = 2
            Case 3 'XIP: Quad Output Fast Read
                cb_xip_mode.SelectedIndex = 3
            Case 4 'XIP: Quad I/O Fast Read
                cb_xip_mode.SelectedIndex = 4
            Case Else 'Disabled
                cb_xip_mode.SelectedIndex = 5 'Default
        End Select
        cb_dummy.SelectedIndex = ((cr(1) And &HF0) >> 4)
    End Sub

    Private Sub cmd_write_config_Click(sender As Object, e As EventArgs) Handles cmd_write_config.Click
        Try
            Dim sr(0) As Byte
            Dim cr(1) As Byte
            If cb_block_bp0.Checked Then sr(0) = sr(0) Or (1 << 2)
            If cb_block_bp1.Checked Then sr(0) = sr(0) Or (1 << 3)
            If cb_block_bp2.Checked Then sr(0) = sr(0) Or (1 << 4)
            If cb_block_bp3.Checked Then sr(0) = sr(0) Or (1 << 6)
            If cb_protected_area.SelectedIndex Then sr(0) = sr(0) Or (1 << 5)
            If cb_status_ro.Checked Then sr(0) = sr(0) Or (1 << 7)
            If cb_address_mode.SelectedIndex Then cr(0) = cr(0) Or 1
            If cb_segment.SelectedIndex Then cr(0) = cr(0) Or (1 << 1)
            Select Case cb_serial_mode.SelectedIndex
                Case 0 'SPI mode
                    cr(0) = cr(0) Or (1 << 2)
                    cr(0) = cr(0) Or (1 << 3)
                Case 1 'DUAL mode
                    cr(0) = cr(0) Or (1 << 3)
                Case 2 'QUAD MODE
                    cr(0) = cr(0) Or (1 << 2)
            End Select
            If (Not cb_reset_disable.Checked) Then cr(0) = cr(0) Or (1 << 4)
            cr(0) = cr(0) Or (1 << 5)
            Select Case cb_output_drv.SelectedIndex
                Case 0 '90 Ohms
                    cr(0) = cr(0) Or (1 << 6)
                Case 1 '60 Ohms
                    cr(0) = cr(0) Or (1 << 7)
                Case 2 '45 Ohms
                    cr(0) = cr(0) Or (1 << 6)
                    cr(0) = cr(0) Or (1 << 7)
                Case 3 '30 Ohms
                    cr(0) = cr(0) Or (1 << 6)
                    cr(0) = cr(0) Or (1 << 7)
                    cr(1) = cr(1) Or 1
                Case 4 '20 Ohms
                    cr(0) = cr(0) Or (1 << 6)
                    cr(1) = cr(1) Or 1
                Case 5 '15 Ohms
                    cr(0) = cr(0) Or (1 << 7)
                    cr(1) = cr(1) Or 1
            End Select
            Select Case cb_xip_mode.SelectedIndex
                Case 0'XIP: Fast Read
                Case 1 'XIP: Dual Output Fast Read
                    cr(1) = cr(1) Or (1 << 1)
                Case 2 'XIP: Dual I/O Fast Read
                    cr(1) = cr(1) Or (1 << 2)
                Case 3 'XIP: Quad Output Fast Read
                    cr(1) = cr(1) Or (1 << 1)
                    cr(1) = cr(1) Or (1 << 2)
                Case 4 'XIP: Quad I/O Fast Read
                    cr(1) = cr(1) Or (1 << 3)
                Case 5 'Disabled
                    cr(1) = cr(1) Or (1 << 1)
                    cr(1) = cr(1) Or (1 << 2)
                    cr(1) = cr(1) Or (1 << 3)
            End Select
            cr(1) = cr(1) Or (cb_dummy.SelectedIndex << 4)
            PrintConsole("Writing status and non-vol registers")
            Dim verify_cr(1) As Byte
            Dim sf(0) As Byte
            Dim verify_sr() As Byte = Nothing
            If FCUSB_PROG.GetType Is GetType(SPI.SPI_Programmer) Then
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).WriteStatusRegister(sr)
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).WaitUntilReady()
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).SPIBUS_WriteEnable()
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).SPIBUS_WriteRead({&HB1, cr(0), cr(1)})
                Do While ((sf(0) And &H80) = 0)
                    DirectCast(FCUSB_PROG, SPI.SPI_Programmer).SPIBUS_WriteRead({&H70}, sf)
                Loop
                Utilities.Sleep(10)
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).WaitUntilReady()
                verify_sr = DirectCast(FCUSB_PROG, SPI.SPI_Programmer).ReadStatusRegister(1)
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).SPIBUS_WriteRead({&HB5}, verify_cr)
            ElseIf FCUSB_PROG.GetType Is GetType(SPI.SQI_Programmer) Then
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).WriteStatusRegister(sr)
                Utilities.Sleep(200)
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).SQIBUS_WriteEnable()
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).SQIBUS_WriteRead({&HB1, cr(0), cr(1)}) 'F4 AF
                Utilities.Sleep(200)
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).SQIBUS_WriteRead({&HB5}, verify_cr) 'FC AF (correct F4 AF)
                verify_sr = DirectCast(FCUSB_PROG, SPI.SQI_Programmer).ReadStatusRegister(1) '02
            End If
            PrintConsole("Verifing the nonvolatile registers have been successfully programmed")
            Dim Failed As Boolean = False
            If verify_sr(0) = sr(0) Then
            Else
                Failed = True
                Dim wrote_str As String = "0x" & Hex(sr(0)).PadLeft(2, "0")
                Dim read_str As String = "0x" & Hex(verify_sr(0)).PadLeft(2, "0")
                PrintConsole("Error programming status register, wrote: " & wrote_str & ", and read back: " & read_str)
            End If
            If verify_cr(0) = cr(0) And verify_cr(1) = cr(1) Then
            Else
                Failed = True
                Dim wrote_str As String = "0x" & Hex(cr(0)).PadLeft(2, "0") & Hex(cr(1)).PadLeft(2, "0")
                Dim read_str As String = "0x" & Hex(verify_cr(1)).PadLeft(2, "0") & Hex(verify_cr(0)).PadLeft(2, "0")
                PrintConsole("Error programming nonvol config register, wrote: " & wrote_str & ", and read back: " & read_str)
            End If
            If Failed Then
                LoadCurrentConfigBits()
                SetStatus("Error: failed to program Nonvolatile configuration registers")
            Else
                SetStatus("Nonvolatile configuration bits successfully programmed")
                PrintConsole("Status register: 0x" & Hex(sr(0)).PadLeft(2, "0"))
                PrintConsole("Nonvol config register: 0x" & Hex(cr(1)).PadLeft(2, "0") & Hex(cr(0)).PadLeft(2, "0"))
            End If
        Catch ex As Exception
        End Try
    End Sub


End Class
