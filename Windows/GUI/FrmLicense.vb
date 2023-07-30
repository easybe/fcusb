Public Class FrmLicense

    Private Sub FrmLicense_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim license_txt() As String = Utilities.FileIO.ReadFile("LICENSE_README.txt")
        If license_txt IsNot Nothing Then
            txt_license_file.Lines = license_txt
        Else
            txt_license_file.Lines = {"LICENSE FILE NOT FOUND"}
        End If
        txt_license_file.SelectedText = ""
        If MySettings.LICENSED_TO.Equals("") Then
            lbl_licensed_to.Text = "Not Licensed: inquire at license@embeddedcomputers.net"
            lbl_exp.Visible = False
        Else
            lbl_exp.Visible = True
            lbl_licensed_to.Text = "Licensed to: " & MySettings.LICENSED_TO
            If MySettings.LICENSE_EXP.Date.Year = 1 Then
                lbl_exp.Text = "Expiration date: None"
            Else
                lbl_exp.Text = "Expiration date: " & MySettings.LICENSE_EXP.Date.ToShortDateString
            End If
        End If
        cmdClose.Select()
    End Sub

    Private Sub cmdClose_Click(sender As Object, e As EventArgs) Handles cmdClose.Click
        Me.Close()
    End Sub

    Private Sub cmd_enter_key_Click(sender As Object, e As EventArgs) Handles cmd_enter_key.Click
        Try
            Dim txt_input As String = InputBox("Please enter your license key").Trim
            If txt_input.Equals("DELETE") Then
                MySettings.LICENSE_KEY = ""
                MySettings.LICENSED_TO = ""
                MySettings.LICENSE_EXP = New DateTime
            ElseIf txt_input.Equals("") Then
            Else
                If License_LoadKey(txt_input) Then
                    Me.Close()
                Else
                    MsgBox("The key you entered is not valid, please check it and try again", vbCritical, "Invalid key")
                End If
            End If
        Catch ex As Exception
        End Try
    End Sub

End Class