'This non-vol control is for Spansion S25FL116K,132K,164K

Public Class NonVol_2

    Private Sub NonVol_2_Load(sender As Object, e As EventArgs) Handles Me.Load
        group_sr1.Enabled = False
        group_sr2.Enabled = False
        cmd_write_config.Enabled = False
    End Sub

    Private Sub cmd_read_config_Click(sender As Object, e As EventArgs) Handles cmd_read_config.Click
        Try
            cmd_read_config.Enabled = False
            WriteConsole("Reading non-vol status registers")
            Dim sr1() As Byte = SPI_IF.ReadStatusRegister(1)
            Dim sr2(0) As Byte
            SPI_IF.SPIBUS_WriteRead({&H35}, sr2)
            WriteConsole("Status register-1: 0x" & Hex(sr1(0)).PadLeft(2, "0"))
            WriteConsole("Status register-2: 0x" & Hex(sr2(0)).PadLeft(2, "0"))
            If ((sr1(0) >> 7) And 1) Then
                cb_sr1_7.Checked = True
            Else
                cb_sr1_7.Checked = False
            End If
            If ((sr1(0) >> 6) And 1) Then
                cb_sr1_6.Checked = True
            Else
                cb_sr1_6.Checked = False
            End If
            If ((sr1(0) >> 5) And 1) Then
                cb_sr1_5.Checked = True
            Else
                cb_sr1_5.Checked = False
            End If
            If ((sr1(0) >> 4) And 1) Then
                cb_sr1_4.Checked = True
            Else
                cb_sr1_4.Checked = False
            End If
            If ((sr1(0) >> 3) And 1) Then
                cb_sr1_3.Checked = True
            Else
                cb_sr1_3.Checked = False
            End If
            If ((sr1(0) >> 2) And 1) Then
                cb_sr1_2.Checked = True
            Else
                cb_sr1_2.Checked = False
            End If
            If ((sr2(0) >> 1) And 1) Then
                cb_sr2_1.Checked = True
            Else
                cb_sr2_1.Checked = False
            End If
            SetStatus("Loaded current non-vol settings")
            group_sr1.Enabled = True
            group_sr2.Enabled = True
        Catch ex As Exception
        Finally
            cmd_read_config.Enabled = True
            cmd_write_config.Enabled = True 'We can now write changes
        End Try
    End Sub

    Private Sub cmd_write_config_Click(sender As Object, e As EventArgs) Handles cmd_write_config.Click
        Try
            WriteConsole("Writing status and non-vol registers")
            Dim sr(1) As Byte
            If cb_sr1_7.Checked Then sr(0) = sr(0) Or (1 << 7)
            If cb_sr1_6.Checked Then sr(0) = sr(0) Or (1 << 6)
            If cb_sr1_5.Checked Then sr(0) = sr(0) Or (1 << 5)
            If cb_sr1_4.Checked Then sr(0) = sr(0) Or (1 << 4)
            If cb_sr1_3.Checked Then sr(0) = sr(0) Or (1 << 3)
            If cb_sr1_2.Checked Then sr(0) = sr(0) Or (1 << 2)
            If cb_sr2_1.Checked Then sr(1) = sr(1) Or (1 << 1)
            WriteConsole("Verifing the nonvolatile registers have been successfully programmed")
            SPI_IF.WriteStatusRegister(sr)
            SPI_IF.WaitUntilReady()
            Dim sr1() As Byte = SPI_IF.ReadStatusRegister(1)
            Dim sr2(0) As Byte
            SPI_IF.SPIBUS_WriteRead({&H35}, sr2)
            Dim Successful As Boolean = True
            If (Not sr(0) = sr1(0)) Then
                Successful = False
                Successful = False
                Dim wrote_str As String = "0x" & Hex(sr(0)).PadLeft(2, "0")
                Dim read_str As String = "0x" & Hex(sr1(0)).PadLeft(2, "0")
                WriteConsole("Error programming status register-1, wrote: " & read_str & ", and read back: " & read_str)
            End If
            If (Not (sr(1) And &HFB) = (sr2(0) And &HFB)) Then
                Successful = False
                Dim wrote_str As String = "0x" & Hex(sr(1) And &HFB).PadLeft(2, "0")
                Dim read_str As String = "0x" & Hex(sr2(0) And &HFB).PadLeft(2, "0")
                WriteConsole("Error programming status register-2, wrote: " & read_str & ", and read back: " & read_str)
            End If
            If Successful Then
                SetStatus("Nonvolatile configuration bits successfully programmed")
                WriteConsole("Status register-1: 0x" & Hex(sr1(0)).PadLeft(2, "0"))
                WriteConsole("Status register-2: 0x" & Hex(sr2(0)).PadLeft(2, "0"))
            Else
                SetStatus("Nonvolatile configuration programming failed")
            End If
        Catch ex As Exception
        End Try
    End Sub

End Class
