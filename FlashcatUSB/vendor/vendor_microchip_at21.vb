Public Class vendor_microchip_at21
    Private FCUSB As USB.HostClient.FCUSB_DEVICE

    Public Event CloseVendorForm(sender As Object)

    Sub New(ByVal usb_dev As USB.HostClient.FCUSB_DEVICE)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        FCUSB = usb_dev
    End Sub

    Private Sub vendor_microchip_at21_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim sec_data(32) As Byte
        If Not FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.SWI_RD_REG, sec_data) Then Exit Sub
        Dim u_data(5) As Byte
        Array.Copy(sec_data, 1, u_data, 0, 6)
        Array.Reverse(u_data)
        Dim u As UInt64 = Utilities.Bytes.ToUInt64(u_data)
        lblSerialNumber.Text = u.ToString

        txtSecreg0.Text = Hex(sec_data(0)).PadLeft(2, "0")
        txtSecreg1.Text = Hex(sec_data(1)).PadLeft(2, "0")
        txtSecreg2.Text = Hex(sec_data(2)).PadLeft(2, "0")
        txtSecreg3.Text = Hex(sec_data(3)).PadLeft(2, "0")
        txtSecreg4.Text = Hex(sec_data(4)).PadLeft(2, "0")
        txtSecreg5.Text = Hex(sec_data(5)).PadLeft(2, "0")
        txtSecreg6.Text = Hex(sec_data(6)).PadLeft(2, "0")
        txtSecreg7.Text = Hex(sec_data(7)).PadLeft(2, "0")

        txtSecreg16.Text = Hex(sec_data(16)).PadLeft(2, "0")
        txtSecreg17.Text = Hex(sec_data(17)).PadLeft(2, "0")
        txtSecreg18.Text = Hex(sec_data(18)).PadLeft(2, "0")
        txtSecreg19.Text = Hex(sec_data(19)).PadLeft(2, "0")
        txtSecreg20.Text = Hex(sec_data(20)).PadLeft(2, "0")
        txtSecreg21.Text = Hex(sec_data(21)).PadLeft(2, "0")
        txtSecreg22.Text = Hex(sec_data(22)).PadLeft(2, "0")
        txtSecreg23.Text = Hex(sec_data(23)).PadLeft(2, "0")
        txtSecreg24.Text = Hex(sec_data(24)).PadLeft(2, "0")
        txtSecreg25.Text = Hex(sec_data(25)).PadLeft(2, "0")
        txtSecreg26.Text = Hex(sec_data(26)).PadLeft(2, "0")
        txtSecreg27.Text = Hex(sec_data(27)).PadLeft(2, "0")
        txtSecreg28.Text = Hex(sec_data(28)).PadLeft(2, "0")
        txtSecreg29.Text = Hex(sec_data(29)).PadLeft(2, "0")
        txtSecreg30.Text = Hex(sec_data(30)).PadLeft(2, "0")
        txtSecreg31.Text = Hex(sec_data(31)).PadLeft(2, "0")

        If (sec_data(32) = 1) Then 'LOCKED
            DisableUserReg()
        End If
        cmdClose.Select()
    End Sub

    Private Sub DisableUserReg()
        txtSecreg16.Enabled = False
        txtSecreg17.Enabled = False
        txtSecreg18.Enabled = False
        txtSecreg19.Enabled = False
        txtSecreg20.Enabled = False
        txtSecreg21.Enabled = False
        txtSecreg22.Enabled = False
        txtSecreg23.Enabled = False
        txtSecreg24.Enabled = False
        txtSecreg25.Enabled = False
        txtSecreg26.Enabled = False
        txtSecreg27.Enabled = False
        txtSecreg28.Enabled = False
        txtSecreg29.Enabled = False
        txtSecreg30.Enabled = False
        txtSecreg31.Enabled = False
        cmdLockSecurityReg.Enabled = False
        cmdSave.Enabled = False
    End Sub

    Private Sub cmdClose_Click(sender As Object, e As EventArgs) Handles cmdClose.Click
        RaiseEvent CloseVendorForm(Me)
    End Sub

    Private Sub cmbSave_Click(sender As Object, e As EventArgs) Handles cmdSave.Click
        Try
            Dim user_reg(15) As Byte
            user_reg(0) = Utilities.HexToInt(txtSecreg16.Text) And 255
            user_reg(1) = Utilities.HexToInt(txtSecreg17.Text) And 255
            user_reg(2) = Utilities.HexToInt(txtSecreg18.Text) And 255
            user_reg(3) = Utilities.HexToInt(txtSecreg19.Text) And 255
            user_reg(4) = Utilities.HexToInt(txtSecreg20.Text) And 255
            user_reg(5) = Utilities.HexToInt(txtSecreg21.Text) And 255
            user_reg(6) = Utilities.HexToInt(txtSecreg22.Text) And 255
            user_reg(7) = Utilities.HexToInt(txtSecreg23.Text) And 255
            user_reg(8) = Utilities.HexToInt(txtSecreg24.Text) And 255
            user_reg(9) = Utilities.HexToInt(txtSecreg25.Text) And 255
            user_reg(10) = Utilities.HexToInt(txtSecreg26.Text) And 255
            user_reg(11) = Utilities.HexToInt(txtSecreg27.Text) And 255
            user_reg(12) = Utilities.HexToInt(txtSecreg28.Text) And 255
            user_reg(13) = Utilities.HexToInt(txtSecreg29.Text) And 255
            user_reg(14) = Utilities.HexToInt(txtSecreg30.Text) And 255
            user_reg(15) = Utilities.HexToInt(txtSecreg31.Text) And 255
            If FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.SWI_WR_REG, user_reg) Then
                MsgBox("Security data saved successfully", vbOK, "ONE-WIRE SECURITY DATA")
            Else
                MsgBox("Error: unable to save security data", vbCritical, "ONE-WIRE SECURITY DATA")
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub cmdLockSecurityReg_Click(sender As Object, e As EventArgs) Handles cmdLockSecurityReg.Click
        Try
            Dim msg As String = "This will permanently lock the security and prevent all future writes."
            msg &= vbCrLf & vbCrLf & "Are you sure?"
            If MsgBox(msg, vbYesNo, "Confirm") = MsgBoxResult.Yes Then
                If FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.SWI_LOCK_REG) Then DisableUserReg()
            End If
        Catch ex As Exception
        End Try
    End Sub

End Class
