Public Class FrmRangeForm
    Private MouseDownOnForm As Boolean = False
    Private ClickPoint As Point

    Private ReadOnly BaseAddress As Long 'The logical address base
    Private ReadOnly MaxStream As Long 'The maximum number of bytes that can be read from/to (-1 for no limit)
    Private ReadOnly MaxMedium As Long 'The size of the memory device

    Public Property BaseOffset As Long
    Public Property RangeSize As Long

    Sub New(BaseAddress As Long, MaxMedium As Long, Optional MaxStream As Long = -1)
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Me.BaseAddress = BaseAddress
        Me.MaxStream = MaxStream
        Me.MaxMedium = MaxMedium

        Me.BaseOffset = BaseAddress
        If (MaxStream = -1) Then MaxStream = MaxMedium Else Math.Min(MaxStream, MaxMedium)
        If (MaxStream > MaxMedium) Then MaxStream = MaxMedium

        RangeSize = MaxStream
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
            If (NewValue >= Me.BaseAddress) AndAlso (NewValue <= (Me.BaseAddress + Me.MaxMedium)) Then
                Me.BaseOffset = (NewValue - Me.BaseAddress)
            End If
            Dim available As Long = (Me.MaxMedium - Me.BaseOffset)
            If (Me.RangeSize > available) Then
                Me.RangeSize = available
                txtLength.Text = Me.RangeSize.ToString()
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
            If (Not MaxStream = -1) Then
                If (NewValue > MaxStream) Then NewValue = MaxStream
            End If
            Dim available As Long = (Me.MaxMedium - Me.BaseOffset)
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