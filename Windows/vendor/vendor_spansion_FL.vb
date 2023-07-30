'This non-vol control is for Spansion S25FL116K,132K,164K

Public Class vendor_spansion_FL
    Private FCUSB_PROG As MemoryDeviceUSB

    Private status_reg_1 As Byte
    Private status_reg_2 As Byte 'Also the configuration register

    Sub New(mem_dev_programmer As MemoryDeviceUSB)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        FCUSB_PROG = mem_dev_programmer
    End Sub

    Private Sub vendor_control_Load(sender As Object, e As EventArgs) Handles Me.Load
        group_sr1.Enabled = False
        group_sr2.Enabled = False
        cmd_write_config.Enabled = False
        cb_sr2_1.Checked = False 'QUAD MODE
        cb_sr1_7.Checked = False
        cb_sr1_6.Checked = False
        cb_sr1_5.Checked = False
        cb_sr1_4.Checked = False
        cb_sr1_3.Checked = False
        cb_sr1_2.Checked = False
    End Sub

    Private Sub cmd_read_config_Click(sender As Object, e As EventArgs) Handles cmd_read_config.Click
        Try
            cmd_read_config.Enabled = False
            PrintConsole("Reading non-vol status registers")
            Dim sr1(0) As Byte
            Dim sr2(0) As Byte
            If FCUSB_PROG.GetType Is GetType(SPI.SPI_Programmer) Then
                sr1 = DirectCast(FCUSB_PROG, SPI.SPI_Programmer).ReadStatusRegister(1)
            ElseIf FCUSB_PROG.GetType Is GetType(SPI.SQI_Programmer) Then
                sr1 = DirectCast(FCUSB_PROG, SPI.SQI_Programmer).ReadStatusRegister(1)
            End If
            If FCUSB_PROG.GetType Is GetType(SPI.SPI_Programmer) Then
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).SPIBUS_WriteRead({&H35}, sr2)
            ElseIf FCUSB_PROG.GetType Is GetType(SPI.SQI_Programmer) Then
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).SQIBUS_WriteRead({&H35}, sr2)
            End If
            status_reg_1 = sr1(0)
            status_reg_2 = sr2(0)
            PrintConsole("Status register-1: 0x" & Hex(status_reg_1).PadLeft(2, "0"))
            PrintConsole("Status register-2: 0x" & Hex(status_reg_2).PadLeft(2, "0"))
            If ((status_reg_1 >> 7) And 1) Then
                cb_sr1_7.Checked = True
            Else
                cb_sr1_7.Checked = False
            End If
            If ((status_reg_1 >> 6) And 1) Then
                cb_sr1_6.Checked = True
            Else
                cb_sr1_6.Checked = False
            End If
            If ((status_reg_1 >> 5) And 1) Then
                cb_sr1_5.Checked = True
            Else
                cb_sr1_5.Checked = False
            End If
            If ((status_reg_1 >> 4) And 1) Then
                cb_sr1_4.Checked = True
            Else
                cb_sr1_4.Checked = False
            End If
            If ((status_reg_1 >> 3) And 1) Then
                cb_sr1_3.Checked = True
            Else
                cb_sr1_3.Checked = False
            End If
            If ((status_reg_1 >> 2) And 1) Then
                cb_sr1_2.Checked = True
            Else
                cb_sr1_2.Checked = False
            End If
            If ((status_reg_2 >> 1) And 1) Then 'QUAD EN
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
            PrintConsole("Writing status and non-vol registers")
            status_reg_1 = 0
            status_reg_2 = 0
            If cb_sr1_7.Checked Then status_reg_1 = status_reg_1 Or (1 << 7)
            If cb_sr1_6.Checked Then status_reg_1 = status_reg_1 Or (1 << 6)
            If cb_sr1_5.Checked Then status_reg_1 = status_reg_1 Or (1 << 5)
            If cb_sr1_4.Checked Then status_reg_1 = status_reg_1 Or (1 << 4)
            If cb_sr1_3.Checked Then status_reg_1 = status_reg_1 Or (1 << 3)
            If cb_sr1_2.Checked Then status_reg_1 = status_reg_1 Or (1 << 2)
            If cb_sr2_1.Checked Then status_reg_2 = status_reg_2 Or (1 << 1) 'QUAD EN
            PrintConsole("Verifing the nonvolatile registers have been successfully programmed")
            Dim sr1_confirm() As Byte = Nothing
            Dim sr2_confirm(0) As Byte
            If FCUSB_PROG.GetType Is GetType(SPI.SPI_Programmer) Then
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).WriteStatusRegister({status_reg_1, status_reg_2}) 'We write SR1 and SR2 using the WRSR 0x01
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).WaitUntilReady()
                sr1_confirm = DirectCast(FCUSB_PROG, SPI.SPI_Programmer).ReadStatusRegister(1)
                DirectCast(FCUSB_PROG, SPI.SPI_Programmer).SPIBUS_WriteRead({&H35}, sr2_confirm)
            ElseIf FCUSB_PROG.GetType Is GetType(SPI.SQI_Programmer) Then
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).WriteStatusRegister({status_reg_1, status_reg_2})
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).WaitUntilReady()
                sr1_confirm = DirectCast(FCUSB_PROG, SPI.SQI_Programmer).ReadStatusRegister(1)
                DirectCast(FCUSB_PROG, SPI.SQI_Programmer).SQIBUS_WriteRead({&H35}, sr2_confirm)
            End If
            Dim Successful As Boolean = True
            If (Not sr1_confirm(0) = status_reg_1) Then
                Successful = False
                Dim wrote_str As String = "0x" & Hex(status_reg_1).PadLeft(2, "0")
                Dim read_str As String = "0x" & Hex(sr1_confirm(0)).PadLeft(2, "0")
                PrintConsole("Error programming status register-1, wrote: " & wrote_str & ", and read back: " & read_str)
            End If
            If (Not (status_reg_2 And &HFB) = (sr2_confirm(0) And &HFB)) Then
                Successful = False
                Dim wrote_str As String = "0x" & Hex(status_reg_2 And &HFB).PadLeft(2, "0")
                Dim read_str As String = "0x" & Hex(sr2_confirm(0) And &HFB).PadLeft(2, "0")
                PrintConsole("Error programming status register-2, wrote: " & wrote_str & ", and read back: " & read_str)
            End If
            If Successful Then
                SetStatus("Nonvolatile configuration bits successfully programmed")
                PrintConsole("Status register-1: 0x" & Hex(status_reg_1).PadLeft(2, "0"))
                PrintConsole("Status register-2: 0x" & Hex(status_reg_2).PadLeft(2, "0"))
            Else
                SetStatus("Nonvolatile configuration programming failed")
            End If
        Catch ex As Exception
        End Try
    End Sub

End Class
