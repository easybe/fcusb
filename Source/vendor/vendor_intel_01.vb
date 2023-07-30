Public Class vendor_intel_01
    Private FCUSB_PROG As MemoryDeviceUSB
    Private NOR_PROG As PARALLEL_NOR

    Sub New(mem_dev_programmer As MemoryDeviceUSB)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        FCUSB_PROG = mem_dev_programmer
        NOR_PROG = DirectCast(Me.FCUSB_PROG, PARALLEL_NOR)
    End Sub

    Private Sub vendor_Load(sender As Object, e As EventArgs) Handles Me.Load
        txt_pr_01.Enabled = False
        txt_pr_02.Enabled = False
        txt_pr_03.Enabled = False
        txt_pr_04.Enabled = False
        txt_pr_05.Enabled = False
        txt_pr_06.Enabled = False
        txt_pr_07.Enabled = False
        txt_pr_08.Enabled = False

        txt_pr_09.Enabled = False
        txt_pr_10.Enabled = False
        txt_pr_11.Enabled = False
        txt_pr_12.Enabled = False
        txt_pr_13.Enabled = False
        txt_pr_14.Enabled = False
        txt_pr_15.Enabled = False
        txt_pr_16.Enabled = False

        txt_pr_09.MaxLength = 2
        txt_pr_10.MaxLength = 2
        txt_pr_11.MaxLength = 2
        txt_pr_12.MaxLength = 2
        txt_pr_13.MaxLength = 2
        txt_pr_14.MaxLength = 2
        txt_pr_15.MaxLength = 2
        txt_pr_16.MaxLength = 2

        txt_pr_09.CharacterCasing = CharacterCasing.Upper
        txt_pr_09.ShortcutsEnabled = False
        AddHandler txt_pr_09.KeyDown, AddressOf txtUser_KeyDown
        AddHandler txt_pr_09.LostFocus, AddressOf txtUser_LostFocus

        txt_pr_10.CharacterCasing = CharacterCasing.Upper
        txt_pr_10.ShortcutsEnabled = False
        AddHandler txt_pr_10.KeyDown, AddressOf txtUser_KeyDown
        AddHandler txt_pr_10.LostFocus, AddressOf txtUser_LostFocus

        txt_pr_11.CharacterCasing = CharacterCasing.Upper
        txt_pr_11.ShortcutsEnabled = False
        AddHandler txt_pr_11.KeyDown, AddressOf txtUser_KeyDown
        AddHandler txt_pr_11.LostFocus, AddressOf txtUser_LostFocus

        txt_pr_12.CharacterCasing = CharacterCasing.Upper
        txt_pr_12.ShortcutsEnabled = False
        AddHandler txt_pr_12.KeyDown, AddressOf txtUser_KeyDown
        AddHandler txt_pr_12.LostFocus, AddressOf txtUser_LostFocus

        txt_pr_13.CharacterCasing = CharacterCasing.Upper
        txt_pr_13.ShortcutsEnabled = False
        AddHandler txt_pr_13.KeyDown, AddressOf txtUser_KeyDown
        AddHandler txt_pr_13.LostFocus, AddressOf txtUser_LostFocus

        txt_pr_14.CharacterCasing = CharacterCasing.Upper
        txt_pr_14.ShortcutsEnabled = False
        AddHandler txt_pr_14.KeyDown, AddressOf txtUser_KeyDown
        AddHandler txt_pr_14.LostFocus, AddressOf txtUser_LostFocus

        txt_pr_15.CharacterCasing = CharacterCasing.Upper
        txt_pr_15.ShortcutsEnabled = False
        AddHandler txt_pr_15.KeyDown, AddressOf txtUser_KeyDown
        AddHandler txt_pr_15.LostFocus, AddressOf txtUser_LostFocus

        txt_pr_16.CharacterCasing = CharacterCasing.Upper
        txt_pr_16.ShortcutsEnabled = False
        AddHandler txt_pr_16.KeyDown, AddressOf txtUser_KeyDown
        AddHandler txt_pr_16.LostFocus, AddressOf txtUser_LostFocus

        cmd_write.Enabled = False

        ReadProtectionRegister()
    End Sub

    Private Sub ReadProtectionRegister()
        cmd_write.Enabled = False
        NOR_PROG.WriteCommandData(0, &H90)
        Dim lock_register As Byte = CByte(NOR_PROG.ReadMemoryAddress(&H80) And &HFF)
        Dim protection(15) As Byte '128 bits
        For i As UInt32 = 1 To 8
            Dim w As UShort = NOR_PROG.ReadMemoryAddress(&H80UI + i)
            protection(CInt((i - 1) / 2)) = CByte(w And 255)
            protection(CInt((i - 1) / 2) + 1) = CByte((w >> 8) And 255)
        Next

        Dim lock As Byte = CByte(NOR_PROG.ReadMemoryAddress(&H100UI) And 255)
        Dim pr01 As UShort = NOR_PROG.ReadMemoryAddress(&H102UI)
        Dim pr02 As UShort = NOR_PROG.ReadMemoryAddress(&H104UI)
        Dim pr03 As UShort = NOR_PROG.ReadMemoryAddress(&H106UI)
        Dim pr04 As UShort = NOR_PROG.ReadMemoryAddress(&H108UI)
        Dim pr05 As UShort = NOR_PROG.ReadMemoryAddress(&H10AUI)
        Dim pr06 As UShort = NOR_PROG.ReadMemoryAddress(&H10CUI)
        Dim pr07 As UShort = NOR_PROG.ReadMemoryAddress(&H10EUI)
        Dim pr08 As UShort = NOR_PROG.ReadMemoryAddress(&H110UI)
        NOR_PROG.ResetDevice(0)

        txt_pr_01.Text = (pr01 And 255).ToString("X").PadLeft(2, "0"c)
        txt_pr_02.Text = (pr01 >> 8).ToString("X").PadLeft(2, "0"c)
        txt_pr_03.Text = (pr02 And 255).ToString("X").PadLeft(2, "0"c)
        txt_pr_04.Text = (pr02 >> 8).ToString("X").PadLeft(2, "0"c)
        txt_pr_05.Text = (pr03 And 255).ToString("X").PadLeft(2, "0"c)
        txt_pr_06.Text = (pr03 >> 8).ToString("X").PadLeft(2, "0"c)
        txt_pr_07.Text = (pr04 And 255).ToString("X").PadLeft(2, "0"c)
        txt_pr_08.Text = (pr04 >> 8).ToString("X").PadLeft(2, "0"c)

        txt_pr_09.Text = (pr05 And 255).ToString("X").PadLeft(2, "0"c)
        txt_pr_10.Text = (pr05 >> 8).ToString("X").PadLeft(2, "0"c)
        txt_pr_11.Text = (pr06 And 255).ToString("X").PadLeft(2, "0"c)
        txt_pr_12.Text = (pr06 >> 8).ToString("X").PadLeft(2, "0"c)
        txt_pr_13.Text = (pr07 And 255).ToString("X").PadLeft(2, "0"c)
        txt_pr_14.Text = (pr07 >> 8).ToString("X").PadLeft(2, "0"c)
        txt_pr_15.Text = (pr08 And 255).ToString("X").PadLeft(2, "0"c)
        txt_pr_16.Text = (pr08 >> 8).ToString("X").PadLeft(2, "0"c)

        Dim otp_disabled As Boolean = (((lock >> 1) And 1) = 1)
        cmd_write.Enabled = otp_disabled
        txt_pr_09.Enabled = otp_disabled
        txt_pr_10.Enabled = otp_disabled
        txt_pr_11.Enabled = otp_disabled
        txt_pr_12.Enabled = otp_disabled
        txt_pr_13.Enabled = otp_disabled
        txt_pr_14.Enabled = otp_disabled
        txt_pr_15.Enabled = otp_disabled
        txt_pr_16.Enabled = otp_disabled

    End Sub

    Private Sub txtUser_KeyDown(sender As Object, e As KeyEventArgs)
        Dim MyTxtBox As TextBox = DirectCast(sender, TextBox)
        Dim i = e.KeyValue
        If e.KeyValue = 8 Then Exit Sub 'Backspace
        If MyTxtBox.Text.Length = 2 Then
            e.Handled = True
        ElseIf i >= 48 AndAlso i <= 57 Then '0-9
        ElseIf i >= 65 AndAlso i <= 70 Then 'A-F
        ElseIf i >= 97 AndAlso i <= 102 Then 'a-z
        End If
    End Sub

    Private Sub txtUser_LostFocus(sender As Object, e As EventArgs)
        Dim MyTxtBox As TextBox = DirectCast(sender, TextBox)
        MyTxtBox.Text = MyTxtBox.Text.PadLeft(2, "0"c)
    End Sub

    Private Sub cmd_write_config_Click(sender As Object, e As EventArgs) Handles cmd_write.Click
        Dim user_msg As String = "You are about to write to the one-time user programmable area." & vbCr & vbCr
        user_msg &= "This process can not be reversed." & vbCr & vbCr
        user_msg &= "Are you sure you want to do this?"
        If MsgBox(user_msg, MsgBoxStyle.OkCancel, "Confirm OTP write operation") = MsgBoxResult.Ok Then
            Dim user_data(7) As Byte
            user_data(0) = CByte(Utilities.HexToInt(txt_pr_09.Text))
            user_data(1) = CByte(Utilities.HexToInt(txt_pr_10.Text))
            user_data(2) = CByte(Utilities.HexToInt(txt_pr_11.Text))
            user_data(3) = CByte(Utilities.HexToInt(txt_pr_12.Text))
            user_data(4) = CByte(Utilities.HexToInt(txt_pr_13.Text))
            user_data(5) = CByte(Utilities.HexToInt(txt_pr_14.Text))
            user_data(6) = CByte(Utilities.HexToInt(txt_pr_15.Text))
            user_data(7) = CByte(Utilities.HexToInt(txt_pr_16.Text))
            Dim addr As UShort = &H10A
            For i = 0 To 7 Step 2
                Dim w_data As UShort = (CUShort(user_data(i + 1)) << 8) Or user_data(i)
                NOR_PROG.WriteCommandData(0, &HC0)
                NOR_PROG.WriteCommandData(addr, w_data)
                Utilities.Sleep(50)
                addr += 2US
            Next
            NOR_PROG.WriteCommandData(0, &HC0)
            NOR_PROG.WriteCommandData(&H100UI, &HFFFD)
            Utilities.Sleep(50)
            MsgBox("User programmable area programmed")
            cmd_write.Enabled = False
            txt_pr_09.Enabled = False
            txt_pr_10.Enabled = False
            txt_pr_11.Enabled = False
            txt_pr_12.Enabled = False
            txt_pr_13.Enabled = False
            txt_pr_14.Enabled = False
            txt_pr_15.Enabled = False
            txt_pr_16.Enabled = False
        End If
    End Sub




End Class
