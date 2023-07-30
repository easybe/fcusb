Public Class HexByteBox
    Inherits TextBox

    Private Declare Function HideCaret Lib "user32.dll" (ByVal hWnd As IntPtr) As Boolean

    Public Event CarryByte(k As Char) 'Indicates the user has entered more data
    Public Event EnterKeyPressed()
    Public Event EscapeKeyPress()

    Private my_data As Byte
    Public Property HexAddress As Long = 0

    Public Property InitialData As Byte
        Get
            Return my_data
        End Get
        Set(value As Byte)
            my_data = value
            Me.Text = Hex(value).PadLeft(2, "0").ToUpper
        End Set
    End Property

    Public ReadOnly Property ByteData As Byte
        Get
            Return CByte(Utilities.HexToInt(Me.Text))
        End Get
    End Property

    Public Property HexString As String
        Get
            Return Hex(Me.ByteData).PadLeft(2, "0").ToUpper
        End Get
        Set(value As String)
            Me.Text = Hex(value).PadLeft(2, "0").ToUpper
        End Set
    End Property

    Sub New()

    End Sub

    Private Sub txthex_KeyPress(sender As Object, e As KeyPressEventArgs) Handles Me.KeyPress
        If Asc(e.KeyChar) = Keys.Back Then Exit Sub
        If Asc(e.KeyChar) = Keys.Delete Then Exit Sub
        If Me.Text.Length = 2 Then
            If Me.SelectedText.Length > 0 Then
            Else
                e.Handled = True
                RaiseEvent CarryByte(e.KeyChar)
                Exit Sub
            End If
        End If
        If e.KeyChar = "." Then
            e.Handled = True : Exit Sub
        End If
        If Not IsNumeric(e.KeyChar) Then
            Select Case e.KeyChar.ToString.ToUpper
                Case "A"
                Case "B"
                Case "C"
                Case "D"
                Case "E"
                Case "F"
                Case Else
                    e.Handled = True : Exit Sub
            End Select
        End If
        e.KeyChar = e.KeyChar.ToString.ToUpper
    End Sub

    Private Sub HexByteBox_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyData = Keys.Enter Then
            RaiseEvent EnterKeyPressed()
        ElseIf e.KeyData = Keys.Escape Then
            RaiseEvent EscapeKeyPress()
        End If
    End Sub

    Private Sub txthex_LostFocus(sender As Object, e As EventArgs) Handles Me.LostFocus
        If Utilities.IsDataType.Hex(Me.Text) Then
            Me.Text = Hex(Utilities.HexToInt(Me.Text)).PadLeft(2, "0")
        Else
            Me.Text = Hex(Me.InitialData).PadLeft(2, "0")
        End If
    End Sub

    Private Sub txthex_GotFocus(sender As Object, e As EventArgs) Handles Me.GotFocus
        HideCaret(Me.Handle)
    End Sub

End Class

Public Class AsciiByteBox
    Inherits TextBox

    Private Declare Function HideCaret Lib "user32.dll" (ByVal hWnd As IntPtr) As Boolean

    Public Event CarryByte(k As Char) 'Indicates the user has entered more data
    Public Event EnterKeyPressed()

    Private my_data As Byte
    Public Property HexAddress As Long = 0

    Public Property InitialData As Byte
        Get
            Return my_data
        End Get
        Set(value As Byte)
            my_data = value
            Me.Text = GetAsciiForByte(value)
        End Set
    End Property

    Public ReadOnly Property ByteData As Byte
        Get
            If Me.Text = "" Then Return 0
            Return AscW(Me.Text)
        End Get
    End Property

    Public Property AsciiChar As Char
        Get
            Return ChrW(Me.ByteData)
        End Get
        Set(value As Char)
            Me.Text = value.ToString
        End Set
    End Property

    Sub New()

    End Sub

    Private Sub AsciiByteBox_KeyPress(sender As Object, e As KeyPressEventArgs) Handles Me.KeyPress
        If Asc(e.KeyChar) = Keys.Back Then Exit Sub
        If Asc(e.KeyChar) = Keys.Delete Then Exit Sub
        If Me.Text.Length = 1 Then
            If Me.SelectedText.Length > 0 Then
            Else
                e.Handled = True
                RaiseEvent CarryByte(e.KeyChar)
                Exit Sub
            End If
        End If
    End Sub

    Private Sub AsciiByteBox_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyData = Keys.Enter Then
            RaiseEvent EnterKeyPressed()
        End If
    End Sub

    Private Function GetAsciiForByte(ByVal b As Byte) As Char
        If b >= 32 And b <= 126 Then '32 to 126
            Return ChrW(b)
        Else
            Return ""
        End If
    End Function

    Private Sub AsciiByteBox_GotFocus(sender As Object, e As EventArgs) Handles Me.GotFocus
        HideCaret(Me.Handle)
    End Sub

End Class
