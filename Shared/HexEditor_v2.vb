Public Class HexEditor_v2
    Public Property BaseOffset As UInt32 = 0 'This is to offset the drawing
    Public Property BaseSize As UInt32 = 0 'Number of bytes this hex view can display
    Public Property TopAddress As UInt32 = 0 'The first address of the hex editor we can see

    Public Event AddressUpdate(ByVal top_address As UInt32) 'Updates the TopAddress
    Public Event RequestData(ByVal address As UInt32, ByRef data() As Byte)

    Private MyFont As New Font("Lucida Console", 8)
    Private PreCache() As Byte = Nothing 'Can contain the entire data to display
    Private ScreenData() As Byte 'Contains a cache of the data that the editor can see
    Private HexDataBitLength As Integer 'number of bits the left side displays
    Private IsLoaded As Boolean = False
    Private Background As Image
    Private Delegate Sub cbDrawScreen()

    Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

    End Sub

    Private Sub HexEditor_v2_Load(sender As Object, e As EventArgs) Handles Me.Load

    End Sub

    'This is what creates the hex viewer (streams data via event calls)
    Public Sub CreateHexViewer(ByVal Base As UInt32, ByVal Size As UInt32)
        Me.BaseOffset = Base
        Me.BaseSize = Size
        Me.TopAddress = 0
        If (Me.BaseSize <= &HFFFF) Then
            HexDataBitLength = 16 '0000:
        ElseIf (Me.BaseSize <= &HFFFFFF) Then '000000:
            HexDataBitLength = 24
        ElseIf (Me.BaseSize <= &HFFFFFFFFL) Then '00000000:
            HexDataBitLength = 32
        End If
        ScrollBar.Minimum = 1
        Background = CreateBackground()
        PB.Image = Background.Clone
        IsLoaded = True
    End Sub
    'This creates the hex viewer from a cached resorce
    Public Sub CreateHexViewer(ByVal Base As UInt32, ByVal PreLoadData() As Byte)
        Me.BaseOffset = Base
        Me.BaseSize = PreLoadData.Length
        Me.TopAddress = 0
        PreCache = PreLoadData
        If (Me.BaseSize <= &HFFFF) Then
            HexDataBitLength = 16 '0000:
        ElseIf (Me.BaseSize <= &HFFFFFF) Then '000000:
            HexDataBitLength = 24
        ElseIf (Me.BaseSize <= &HFFFFFFFFL) Then '00000000:
            HexDataBitLength = 32
        End If
        ScrollBar.Minimum = 1
        Background = CreateBackground()
        PB.Image = Background.Clone
        IsLoaded = True
        Me.UpdateScreen()
    End Sub

    Public Sub CloseHexViewer()
        TopAddress = 0
        HexDataBitLength = 0
        PB.Image = Nothing
        IsLoaded = False
        ScrollBar.Enabled = False
    End Sub

    Private Sub HexEditor_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Resize
        If Not IsLoaded Then Exit Sub 'Only resize if we are shown
        Background = CreateBackground()
        UpdateScreen()
    End Sub

    Private LastScroll As DateTime = DateTime.Now
    Private InRefresh As Boolean = False

    Private Function GetNumOfVisibleLines() As Integer
        Return CInt(Math.Floor(PB.Height / 13))
    End Function
    'Returns the number of hex (32bit boundry) that we can display
    Private Function GetNumOfHeaderLines() As UInt32
        Try
            If (PB.Width < 100) Then Return 0
            Dim NumOfAddrBytes As Integer = HexDataBitLength / 4
            Dim x As UInt32 = PB.Width - ((NumOfAddrBytes * 8) - (NumOfAddrBytes / 2) + 6)
            If x = 0 Then Return 0
            Dim y As UInt32 = Math.Floor((x / 21) - 1)
            Do Until y Mod 4 = 0
                y = y - 1
            Loop
            Return y
        Catch ex As Exception
        End Try
        Return 0
    End Function

    Private Sub VSbar_Scroll(ByVal sender As Object, ByVal e As System.Windows.Forms.ScrollEventArgs) Handles ScrollBar.Scroll
        If InRefresh Then Exit Sub
        If DateTime.Compare(LastScroll.AddMilliseconds(100), DateTime.Now) > 0 Then
            Exit Sub 'We only want to allow a scroll to happen no closer than 250 ms
        End If
        LastScroll = DateTime.Now
        UpdateScreen()
    End Sub

    Private Function CreateBackground() As Image
        Dim img As Image = New Bitmap(PB.Width, PB.Height)
        SetBitmapColor(img, Brushes.White)
        Return img
    End Function

    Private Sub SetBitmapColor(ByRef img As Bitmap, ByVal color As Brush)
        Dim Img2 As Graphics = Graphics.FromImage(img)
        Dim myRect As New Rectangle(0, 0, img.Width, img.Height)
        Img2.FillRectangle(color, myRect)
        Img2.Dispose()
    End Sub

    Public Sub UpdateScreen()
        If (Not IsLoaded) Then Exit Sub
        If ScrollBar.InvokeRequired Then
            Dim d As New cbDrawScreen(AddressOf UpdateScreen)
            Me.Invoke(d)
        Else
            Try
                InRefresh = True
                Static CurrentTop As UInt32 = 0
                Dim newBG As Image = Background.Clone
                Dim gfx As Graphics = Graphics.FromImage(newBG)
                Dim TotalLines As UInt32 = GetNumOfVisibleLines()
                Dim BytesPerLine As UInt32 = GetNumOfHeaderLines() 'Each column = 1 byte
                If TotalLines = 0 Or BytesPerLine = 0 Then Exit Sub
                Dim MaxDataShown As UInt32 = TotalLines * BytesPerLine 'Total amount of bytes that we can show
                If (BaseSize > MaxDataShown) Then
                    ScrollBar.Enabled = True 'More lines than we can display
                    Dim BarPercent As Single = ((ScrollBar.Value - 1) / (ScrollBar.Maximum - ScrollBar.LargeChange))
                    ScrollBar.LargeChange = TotalLines
                    ScrollBar.Maximum = Math.Ceiling(BaseSize / BytesPerLine)
                    Dim NewValue As UInt32 = Math.Round((ScrollBar.Maximum - ScrollBar.LargeChange) * BarPercent) + 1
                    If NewValue < 1 Then NewValue = 1
                    If NewValue > ScrollBar.Maximum Then NewValue = ScrollBar.Maximum
                    ScrollBar.Value = NewValue
                    TopAddress = (NewValue - 1) * CLng(BytesPerLine)
                Else
                    ScrollBar.Enabled = False 'We can display all data
                    TopAddress = 0
                End If
                If (BaseSize > MaxDataShown) Then
                    ScrollBar.Enabled = True 'More lines than we can display
                    Dim BarPercent As Single = ((ScrollBar.Value - 1) / (ScrollBar.Maximum - ScrollBar.LargeChange))
                    ScrollBar.LargeChange = TotalLines
                    ScrollBar.Maximum = Math.Ceiling(BaseSize / BytesPerLine)
                    Dim NewValue As Integer = Math.Round((ScrollBar.Maximum - ScrollBar.LargeChange) * BarPercent) + 1
                    If NewValue < 1 Then NewValue = 1
                    If NewValue > ScrollBar.Maximum Then NewValue = ScrollBar.Maximum
                    ScrollBar.Value = NewValue
                    TopAddress = (NewValue - 1) * CLng(BytesPerLine)
                Else
                    ScrollBar.Enabled = False 'We can display all data
                    TopAddress = 0
                End If
                ScrollBar.Refresh() 'Recently added
                Dim DataToGet As UInt32 = BaseSize - TopAddress 'The amount of bytes we need to display in the box
                If (DataToGet > MaxDataShown) Then
                    DataToGet = MaxDataShown
                End If
                ReDim ScreenData(DataToGet - 1)
                If PreCache Is Nothing Then
                    RaiseEvent RequestData(TopAddress, ScreenData)
                Else
                    Array.Copy(PreCache, TopAddress, ScreenData, 0, ScreenData.Length)
                End If
                If ScreenData IsNot Nothing Then
                    Dim AddrIndex As UInt32 = 0
                    Dim LinesToDraw As UInt32 = CInt(Math.Ceiling(DataToGet / BytesPerLine))
                    For i = 0 To LinesToDraw - 1
                        Dim BytesForLine() As Byte
                        If DataToGet > BytesPerLine Then
                            ReDim BytesForLine(BytesPerLine - 1)
                        Else
                            ReDim BytesForLine(DataToGet - 1)
                        End If
                        Array.Copy(ScreenData, AddrIndex, BytesForLine, 0, BytesForLine.Length)
                        Drawline(i, TopAddress + AddrIndex + BaseOffset, BytesPerLine, BytesForLine, gfx)
                        AddrIndex += BytesPerLine
                        DataToGet -= BytesForLine.Length
                    Next
                End If
                PB.Image = newBG
                gfx = Nothing
                newBG = Nothing
                InRefresh = False
                If (Not TopAddress = CurrentTop) Then
                    CurrentTop = TopAddress
                    RaiseEvent AddressUpdate(CurrentTop)
                End If
            Catch ex As Exception
            End Try
        End If
    End Sub

    Private Sub Drawline(ByVal LineIndex As Integer, ByVal FullAddr As UInt64, ByVal ByteCount As Integer, ByRef data() As Byte, ByRef gfx As Graphics)
        Dim YLOC As Integer = (LineIndex * 13) + 1
        Dim NumOfAddrBytes As Integer = (HexDataBitLength / 4)
        Dim AddrStr As String = Hex(FullAddr).PadLeft(NumOfAddrBytes, "0") & ": "
        Dim HexAscii() As String = Nothing
        Dim Ascii() As Char = Nothing
        GetAsciiForLine(data, HexAscii, Ascii)
        gfx.DrawString(AddrStr, MyFont, Drawing.Brushes.Gray, 0, YLOC)
        Dim HexAsciiStart As Integer = (NumOfAddrBytes * 8) - (NumOfAddrBytes / 2) + 2
        Dim AsciiStart As Integer = HexAsciiStart + (ByteCount * 14) + 4
        For i = 0 To HexAscii.Length - 1
            gfx.DrawString(HexAscii(i), MyFont, Brushes.Black, New Point(HexAsciiStart + (i * 14), YLOC))
        Next
        For i = 0 To Ascii.Length - 1
            gfx.DrawString(Ascii(i), MyFont, Brushes.Black, New Point(AsciiStart + (i * 7), YLOC))
        Next
    End Sub

    Private Sub GetAsciiForLine(ByRef d() As Byte, ByRef HexAscii() As String, ByRef Ascii() As Char)
        Dim l As Integer = d.Length - 1
        ReDim HexAscii(l)
        ReDim Ascii(l)
        For i = 0 To l
            HexAscii(i) = Hex(d(i)).PadLeft(2, "0")
            Ascii(i) = GetAsciiForByte(d(i))
        Next
    End Sub

    Private Function GetAsciiForByte(ByVal b As Byte) As Char
        If b >= 32 And b <= 126 Then '32 to 126
            Return Chr(b)
        Else
            Return Chr(46) '"."
        End If
    End Function

    Private DoRefreshOnPaint As Boolean = False

    Private Sub PB_Paint(sender As Object, e As System.Windows.Forms.PaintEventArgs) Handles PB.Paint
        If DoRefreshOnPaint Then UpdateScreen() : DoRefreshOnPaint = False
    End Sub

    'Causes the editor to redraw at the specified address
    Public Sub GotoAddress(ByVal ThisAddr As Long)
        Try
            If (ThisAddr + 1) > BaseSize Then ThisAddr = BaseSize
            Dim BytesPerLine As UInt32 = GetNumOfHeaderLines() 'Each column = 1 byte
            Dim MyValue As Long = Math.Floor(ThisAddr / BytesPerLine) + 1
            If MyValue > ScrollBar.Maximum Then MyValue = ScrollBar.Maximum
            ScrollBar.Value = MyValue
        Catch ex As Exception
        End Try
        UpdateScreen()
    End Sub

End Class
