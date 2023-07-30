Public Class FrmRangeForm
    Private MouseDownOnForm As Boolean = False
    Private ClickPoint As Point
    Public ReadOnly Property BaseAddress As Long 'The logical address base
    Public ReadOnly Property BaseMax As Long 'The maximum number of bytes that this address contains
    Public ReadOnly Property RangeMax As Long 'The maximum number of bytes that the range can be
    Public Property BaseOffset As Long = 0 'Must be less than BaseMax
    Public Property RangeSize As Long = 0 'Must be less than BaseMax

    Sub New(base_addr As Long, base_size As Long, Optional range_max As Long = 0)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Me.BaseAddress = base_addr
        Me.BaseMax = base_size
        If range_max = 0 Then
            Me.RangeMax = Me.BaseMax
        Else
            Me.RangeMax = range_max
        End If
        Me.RangeSize = Me.RangeMax
    End Sub

    Private Sub FrmRangeForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.txtBaseOffset.Text = "0x" & (Me.BaseAddress + Me.BaseOffset).ToString("X")
        Me.txtLength.Text = Me.RangeSize.ToString()
        cmd_okay.Select()
    End Sub

    Private Sub FrmRangeForm_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If (e.KeyCode = 13) Then 'Enter pressed
            Me.DialogResult = DialogResult.OK
            Me.Close()
        End If
    End Sub

    Private Sub pb_background_MouseDown(sender As Object, e As MouseEventArgs) Handles pb_background.MouseDown
        MouseDownOnForm = True
        ClickPoint = New Point(Cursor.Position.X, Cursor.Position.Y)
    End Sub

    Private Sub FrmRangeForm_lbl_base(sender As Object, e As MouseEventArgs) Handles lbl_base.MouseDown
        MouseDownOnForm = True
        ClickPoint = New Point(Cursor.Position.X, Cursor.Position.Y)
    End Sub

    Private Sub FrmRangeForm_lbl_length(sender As Object, e As MouseEventArgs) Handles lbl_length.MouseDown
        MouseDownOnForm = True
        ClickPoint = New Point(Cursor.Position.X, Cursor.Position.Y)
    End Sub

    Private Sub pb_background_MouseUp(sender As Object, e As MouseEventArgs) Handles pb_background.MouseUp
        MouseDownOnForm = False
    End Sub

    Private Sub lbl_base_MouseUp(sender As Object, e As MouseEventArgs) Handles lbl_base.MouseUp
        MouseDownOnForm = False
    End Sub

    Private Sub lbl_length_MouseUp(sender As Object, e As MouseEventArgs) Handles lbl_length.MouseUp
        MouseDownOnForm = False
    End Sub

    Private Sub pb_background_MouseMove(sender As Object, e As MouseEventArgs) Handles pb_background.MouseMove
        control_move(CType(sender, Control))
    End Sub

    Private Sub cmd_okay_Click(sender As Object, e As EventArgs) Handles cmd_okay.Click
        If (Me.RangeSize = 0) Then
            MsgBox("The length field can not be zero", MsgBoxStyle.Critical, "Error!")
            Exit Sub
        End If
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub cmd_cancel_Click(sender As Object, e As EventArgs) Handles cmd_cancel.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub txtBaseOffset_LostFocus(sender As Object, e As EventArgs) Handles txtBaseOffset.LostFocus
        Try
            Dim NewValue As Long
            If IsNumeric(txtBaseOffset.Text) Then
                NewValue = CLng(txtBaseOffset.Text)
            ElseIf Utilities.IsDataType.HexString(txtBaseOffset.Text) Then
                NewValue = Utilities.HexToLng(txtBaseOffset.Text)
            Else
                Exit Sub
            End If
            If (NewValue >= Me.BaseAddress) AndAlso (NewValue <= (Me.BaseAddress + Me.BaseMax)) Then
                Me.BaseOffset = (NewValue - Me.BaseAddress)
            End If
            Dim available = (Me.BaseMax - Me.BaseOffset)
            If RangeSize > available Then
                RangeSize = available
                txtLength.Text = RangeSize.ToString()
            End If
        Finally
            txtBaseOffset.Text = "0x" & (Me.BaseAddress + Me.BaseOffset).ToString("X")
        End Try
    End Sub

    Private Sub txtLength_LostFocus(sender As Object, e As EventArgs) Handles txtLength.LostFocus
        Try
            Dim NewValue As Long
            If IsNumeric(txtLength.Text) Then
                NewValue = CLng(txtLength.Text)
            ElseIf Utilities.IsDataType.HexString(txtLength.Text) AndAlso (txtLength.Text.Length < 9) Then
                NewValue = Utilities.HexToLng(txtLength.Text)
            Else
                Exit Sub
            End If
            Dim available = (Me.BaseMax - Me.BaseOffset)
            If NewValue <= available Then
                RangeSize = NewValue
            Else
                RangeSize = available
            End If
        Finally
            txtLength.Text = RangeSize.ToString()
        End Try
    End Sub

    Private Sub lbl_base_MouseMove(sender As Object, e As MouseEventArgs) Handles lbl_base.MouseMove
        control_move(CType(sender, Control))
    End Sub

    Private Sub lbl_length_MouseMove(sender As Object, e As MouseEventArgs) Handles lbl_length.MouseMove
        control_move(CType(sender, Control))
    End Sub

    Private Sub control_move(sender As Control)
        If MouseDownOnForm Then
            Dim newPoint As New Point(Cursor.Position.X, Cursor.Position.Y)
            Dim Form1 As Form = sender.FindForm
            Form1.Top = Form1.Top + (newPoint.Y - ClickPoint.Y)
            Form1.Left = Form1.Left + (newPoint.X - ClickPoint.X)
            ClickPoint = newPoint
        End If
    End Sub

End Class