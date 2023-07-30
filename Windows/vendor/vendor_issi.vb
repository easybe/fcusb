'This non-vol control is for ISSI IS25LP080D IS25WP080D IS25WP040D IS25WP020D

Public Class vendor_issi
    Private FCUSB_PROG As MemoryDeviceUSB

    Sub New(mem_dev_programmer As MemoryDeviceUSB)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        FCUSB_PROG = mem_dev_programmer
    End Sub

    Private Sub vendor_Load(sender As Object, e As EventArgs) Handles Me.Load
        group_sr1.Enabled = False
        cmd_write_config.Enabled = False
    End Sub

    Private Sub cmd_read_config_Click(sender As Object, e As EventArgs) Handles cmd_read_config.Click
        Try
            cmd_read_config.Enabled = False
            PrintConsole("Reading non-vol status registers")
            Dim sr() As Byte = Nothing
            If FCUSB_PROG.GetType Is GetType(SPI.SPI_Programmer) Then
                sr = DirectCast(FCUSB_PROG, SPI.SPI_Programmer).ReadStatusRegister(1)
            ElseIf FCUSB_PROG.GetType Is GetType(SPI.SQI_Programmer) Then
                sr = DirectCast(FCUSB_PROG, SPI.SQI_Programmer).ReadStatusRegister(1)
            End If
            PrintConsole("Status register: 0x" & Hex(sr(0)).PadLeft(2, "0"))
            If ((sr(0) >> 2) And 1) Then 'bit 2
                cb_bp0.Checked = True
            Else
                cb_bp0.Checked = False
            End If
            If ((sr(0) >> 3) And 1) Then 'bit 3
                cb_bp1.Checked = True
            Else
                cb_bp1.Checked = False
            End If
            If ((sr(0) >> 4) And 1) Then 'bit 4
                cb_bp2.Checked = True
            Else
                cb_bp2.Checked = False
            End If
            If ((sr(0) >> 5) And 1) Then 'bit 5
                cb_bp3.Checked = True
            Else
                cb_bp3.Checked = False
            End If
            If ((sr(0) >> 6) And 1) Then 'bit 6
                cb_qspi.Checked = True
            Else
                cb_qspi.Checked = False
            End If
            SetStatus("Loaded current non-vol settings")
            group_sr1.Enabled = True
        Catch ex As Exception
        Finally
            cmd_read_config.Enabled = True
            cmd_write_config.Enabled = True 'We can now write changes
        End Try
    End Sub

    Private Sub cmd_write_config_Click(sender As Object, e As EventArgs) Handles cmd_write_config.Click
        Try
            PrintConsole("Writing non-vol register")
            Dim sr_to_write(0) As Byte
            If cb_bp0.Checked Then sr_to_write(0) = sr_to_write(0) Or (1 << 2)
            If cb_bp1.Checked Then sr_to_write(0) = sr_to_write(0) Or (1 << 3)
            If cb_bp2.Checked Then sr_to_write(0) = sr_to_write(0) Or (1 << 4)
            If cb_bp3.Checked Then sr_to_write(0) = sr_to_write(0) Or (1 << 5)
            If cb_qspi.Checked Then sr_to_write(0) = sr_to_write(0) Or (1 << 6)
            PrintConsole("Verifing the nonvolatile register have been successfully programmed")
            Dim sr_read_back() As Byte = Nothing
            If FCUSB_PROG.GetType Is GetType(SPI.SPI_Programmer) Then
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).WriteStatusRegister(sr_to_write)
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).WaitUntilReady()
                sr_read_back = DirectCast(FCUSB_PROG, SPI.SPI_Programmer).ReadStatusRegister(1)
            ElseIf FCUSB_PROG.GetType Is GetType(SPI.SQI_Programmer) Then
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).WriteStatusRegister(sr_to_write)
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).WaitUntilReady()
                sr_read_back = DirectCast(FCUSB_PROG, SPI.SQI_Programmer).ReadStatusRegister(1)
            End If
            Dim Successful As Boolean = True
            If (Not sr_to_write(0) = sr_read_back(0)) Then
                Successful = False
                Dim wrote_str As String = "0x" & Hex(sr_to_write(0)).PadLeft(2, "0")
                Dim read_str As String = "0x" & Hex(sr_read_back(0)).PadLeft(2, "0")
                PrintConsole("Error programming status register, wrote: " & wrote_str & ", and read back: " & read_str)
            End If
            If Successful Then
                SetStatus("Nonvolatile configuration bits successfully programmed")
                PrintConsole("Status register: 0x" & Hex(sr_read_back(0)).PadLeft(2, "0"))
            Else
                SetStatus("Nonvolatile configuration programming failed")
            End If
        Catch ex As Exception
        End Try
    End Sub

End Class
