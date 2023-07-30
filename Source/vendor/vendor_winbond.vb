Public Class vendor_winbond
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

    Private Sub cmd_write_config_Click(sender As Object, e As EventArgs) Handles cmd_write_config.Click
        Try
            PrintConsole("Writing non-vol registers")
            Dim sr1_to_write(0) As Byte
            Dim sr2_to_write(0) As Byte
            If cb_bp0.Checked Then sr1_to_write(0) = sr1_to_write(0) Or CByte(1 << 2)
            If cb_bp1.Checked Then sr1_to_write(0) = sr1_to_write(0) Or CByte(1 << 3)
            If cb_bp2.Checked Then sr1_to_write(0) = sr1_to_write(0) Or CByte(1 << 4)
            If cb_bp3.Checked Then sr1_to_write(0) = sr1_to_write(0) Or CByte(1 << 5)
            If cb_qspi.Checked Then sr2_to_write(0) = sr2_to_write(0) Or CByte(1 << 1)
            PrintConsole("Programming non-vol register-1: 0x" & Hex(sr1_to_write(0)).PadLeft(2, "0"c))
            If FCUSB_PROG.GetType Is GetType(SPI.SPI_Programmer) Then
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).SPIBUS_WriteEnable()
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).SPIBUS_WriteRead({&H1, sr1_to_write(0)})
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).WaitUntilReady()
            ElseIf FCUSB_PROG.GetType Is GetType(SPI.SQI_Programmer) Then
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).SQIBUS_WriteEnable()
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).SQIBUS_WriteRead({&H1, sr1_to_write(0)})
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).WaitUntilReady()
            End If
            PrintConsole("Programming non-vol register-2: 0x" & Hex(sr1_to_write(0)).PadLeft(2, "0"c))
            If FCUSB_PROG.GetType Is GetType(SPI.SPI_Programmer) Then
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).SPIBUS_WriteEnable()
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).SPIBUS_WriteRead({&H31, sr2_to_write(0)})
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).WaitUntilReady()
            ElseIf FCUSB_PROG.GetType Is GetType(SPI.SQI_Programmer) Then
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).SQIBUS_WriteEnable()
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).SQIBUS_WriteRead({&H31, sr2_to_write(0)})
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).WaitUntilReady()
            End If
            SetStatus("Nonvolatile configuration successfully changed")
        Catch ex As Exception
        End Try
    End Sub

    Private Sub cmd_read_config_Click(sender As Object, e As EventArgs) Handles cmd_read_config.Click
        Try
            cmd_read_config.Enabled = False
            PrintConsole("Reading non-vol status registers")
            Dim sr1(0) As Byte
            Dim sr2(0) As Byte
            If FCUSB_PROG.GetType Is GetType(SPI.SPI_Programmer) Then
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).SPIBUS_WriteRead({&H5}, sr1)
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).SPIBUS_WriteRead({&H35}, sr2)
            ElseIf FCUSB_PROG.GetType Is GetType(SPI.SQI_Programmer) Then
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).SQIBUS_WriteRead({&H5}, sr1)
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).SQIBUS_WriteRead({&H35}, sr2)
            End If
            PrintConsole("Status register-1: 0x" & Hex(sr1(0)).PadLeft(2, "0"c))
            PrintConsole("Status register-2: 0x" & Hex(sr2(0)).PadLeft(2, "0"c))
            If ((sr1(0) >> 2) And 1) = 1 Then 'bit 2 
                cb_bp0.Checked = True
            Else
                cb_bp0.Checked = False
            End If
            If ((sr1(0) >> 3) And 1) = 1 Then 'bit 3
                cb_bp1.Checked = True
            Else
                cb_bp1.Checked = False
            End If
            If ((sr1(0) >> 4) And 1) = 1 Then 'bit 4
                cb_bp2.Checked = True
            Else
                cb_bp2.Checked = False
            End If
            If ((sr1(0) >> 5) And 1) = 1 Then 'bit 5
                cb_bp3.Checked = True
            Else
                cb_bp3.Checked = False
            End If
            If ((sr2(0) >> 1) And 1) = 1 Then
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




End Class
