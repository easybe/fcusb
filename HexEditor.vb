'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2016 - ALL RIGHTS RESERVED
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: This control is a simple hexeditor for .net!

Public Class HexEditor

    Enum AccessType
        _NotLoaded = 0
        _ReadOnly = 1 'Only allows user to view the data
        _ReadWrite = 2 'Allows edit editor to write instantly to the stream/buffer
        _ReadWriteOnce = 3 'Allows the editor to store a buffer of data that can be used to write-once
    End Enum

    Enum AccessMode
        _Cached = 0 'All of the data is pre-loaded
        _Stream = 1 'Data is loaded as requested as needed (and cached)
        _StreamNoCache = 2 'Data is loaded as requested and never cached
    End Enum

    Public ReadOnly Property DataSize() As Integer
        Get
            If MyDataBuffer Is Nothing Then Return 0
            Return MyDataBuffer.Length
        End Get
    End Property

    Private EnterBox As TextBox 'The box used to hold user input (For editing hex)

    Private MyDataBuffer() As Byte 'Holds all of the data, or some that has been streamed
    Private MyDataStream() As Boolean 'Indicates which bytes have been read (stream only) 
    Private MyDataCache() As Byte 'Used for ReadWriteOnce
    Private MyScreenData() As Byte 'Data that is currently displayed in the hex box

    Public Event HasCachedData() 'Indicates the user has put at least one byte into our cache
    Public Event ReadStream(ByVal addr As UInt32, ByRef data() As Byte)
    Public Event AddressUpdate(ByVal addr As UInt32) 'Updates the TopAddress
    Public Event StreamUpdate(ByVal addr As UInt32, ByVal data As Byte) 'Writes a byte back to the stream that the user entered

    Private Sub HexEditor_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
 
    End Sub

    Private LastScroll As DateTime = DateTime.Now
    Private InRefresh As Boolean = False
    Private Sub VSbar_Scroll(ByVal sender As Object, ByVal e As System.Windows.Forms.ScrollEventArgs) Handles ScrollBar.Scroll
        If InRefresh Then Exit Sub
        If DateTime.Compare(LastScroll.AddMilliseconds(100), DateTime.Now) > 0 Then
            Exit Sub 'We only want to allow a scroll to happen no closer than 250 ms
        End If
        LastScroll = DateTime.Now
        PB.Controls.Remove(EnterBox)
        RefreshScreen()
    End Sub

    Private Function GetInternalData(ByVal Addr As UInt32, ByVal numberofbytes As Integer) As Byte()
        If numberofbytes < 1 Then Return Nothing
        Dim dataOut(numberofbytes - 1) As Byte
        If MyAccessMode = AccessMode._Stream Then
            Dim DataCollector As New ArrayList
            Dim StartAddr As UInt32 = Addr
            Dim BuffAddr As UInt32 = 0
            Dim Count As Integer = 0
            Dim CollectingForStream As Boolean = False
            Dim CollectingForCache As Boolean = False
            For i = 0 To numberofbytes - 1
                If MyDataStream(Addr + i) Then 'We can load from MyDataBuffer (cache)
                    If CollectingForStream Then
                        Dim b() As Byte = FlushDataRequest(StartAddr, Count)
                        Array.Copy(b, 0, dataOut, BuffAddr, Count)
                        StartAddr = Addr + i
                        BuffAddr += Count
                        Count = 0
                        CollectingForStream = False
                    End If
                    CollectingForCache = True
                Else 'We need to refresh from outside source (stream)
                    If CollectingForCache Then
                        Array.Copy(MyDataBuffer, StartAddr, dataOut, BuffAddr, Count)
                        StartAddr = Addr + i
                        BuffAddr += Count
                        Count = 0
                        CollectingForCache = False
                    End If
                    CollectingForStream = True
                End If
                Count += 1
            Next
            If CollectingForCache Then
                Array.Copy(MyDataBuffer, StartAddr, dataOut, BuffAddr, Count)
            Else 'Load data from stream
                Dim b() As Byte = FlushDataRequest(StartAddr, Count)
                Array.Copy(b, 0, dataOut, BuffAddr, Count)
            End If
        ElseIf MyAccessMode = AccessMode._StreamNoCache Then
            RaiseEvent ReadStream(Addr, dataOut)
        ElseIf MyAccessMode = AccessMode._Cached Then
            For i = 0 To numberofbytes - 1
                dataOut(i) = MyDataBuffer(Addr + i)
            Next
        End If
        Return dataOut
    End Function

    Private Function FlushDataRequest(ByVal Addr As UInt32, ByVal Count As Integer) As Byte()
        Dim d(Count - 1) As Byte
        RaiseEvent ReadStream(Addr, d)
        For i = 0 To Count - 1
            MyDataStream(Addr + i) = True
            MyDataBuffer(Addr + i) = d(i)
        Next
        Return d
    End Function

    Private CurrentTop As UInt32 = 0 'The current top address (does not contain offset)
    Private MyAccessType As AccessType = AccessType._NotLoaded
    Private MyAccessMode As AccessMode
    Private MyDataOffset As UInt32 = 0 'The offset that the hexeditor should display
    Private MyDataBits As Integer = 0 '8bit,16bit,24bit,32bit
    Private TopAddress As Integer = 0 'Address at the top of the editor
    Private ByteCountShowing As Integer = 0 'Number of bytes our hex editor is currently displaying
    Delegate Sub cbRefreshScreen()

    Public Sub CloseHexViewer()
        MyDataOffset = 0
        MyDataBits = 0
        TopAddress = 0
        ByteCountShowing = 0
        MyDataBuffer = Nothing
        PB.Image = Nothing
        ScrollBar.Enabled = False
        'RefreshScreen()
    End Sub

    Public Sub CreateHexViewer(ByVal HexSize As UInt32, ByVal Offset As UInt32, ByVal aType As AccessType, ByVal aMode As AccessMode)
        ReDim MyDataBuffer(HexSize - 1)
        MyDataOffset = Offset
        MyAccessType = aType
        MyAccessMode = aMode
        If aType = AccessType._ReadWriteOnce Then
            ReDim MyDataCache(HexSize - 1)
        End If
        If aMode = AccessMode._Stream Then
            ReDim MyDataStream(HexSize - 1)
        End If
        If Offset + HexSize < 65536 Then
            MyDataBits = 16   'We can display 0000 wide
        ElseIf Offset + HexSize < 16777216 Then
            MyDataBits = 24  'We can display 000000 wide
        Else
            MyDataBits = 32 'We can display 00000000
        End If
        ScrollBar.Minimum = 1
    End Sub

    Public Sub BeginHexViewer(Optional ByVal Buffer() As Byte = Nothing)
        If Buffer IsNot Nothing Then
            MyDataBuffer = Buffer
        End If
        'Possibly do other things here for one-time operations
        GraphBG = CreateBackground()
        PB.Image = GraphBG.Clone
        RefreshScreen()
        DoRefreshOnPaint = True
    End Sub
    'Indicates that we should re-stream all data
    Public Sub RefreshStream()
        If MyDataStream IsNot Nothing Then
            For i = 0 To MyDataStream.Length - 1
                MyDataStream(i) = False
            Next
            RefreshScreen() 'Update all data on the editor
        End If
    End Sub

    Public IsLoaded As Boolean = False 'Set to true when ready to use
    Private GraphBG As Image
    Private MyFont As New System.Drawing.Font("Lucida Console", 8)
    'Re-draws all of the data on the screen
    Private Sub RefreshScreen()
        If ScrollBar.InvokeRequired Then
            Dim d As New cbRefreshScreen(AddressOf RefreshScreen)
            Me.Invoke(d)
        Else
            InRefresh = True
            Dim newBG As Image = GraphBG.Clone
            Dim gfx As Graphics = Graphics.FromImage(newBG)
            Dim TotalLines As Int32 = GetNumOfVisibleLines()
            Dim BytesPerLine As Int32 = GetNumOfHeaderLines() 'Each column = 1 byte
            Dim DataSize As Integer = MyDataBuffer.Length
            Dim MaxDataShown As Int32 = TotalLines * BytesPerLine 'Total amount of bytes that we can show
            If DataSize > MaxDataShown Then
                ScrollBar.Enabled = True 'More lines than we can display
                Dim BarPercent As Single = ((ScrollBar.Value - 1) / (ScrollBar.Maximum - ScrollBar.LargeChange))
                ScrollBar.LargeChange = TotalLines
                ScrollBar.Maximum = Math.Ceiling(DataSize / BytesPerLine)
                Dim NewValue As Integer = Math.Round((ScrollBar.Maximum - ScrollBar.LargeChange) * BarPercent) + 1
                If NewValue < 1 Then NewValue = 1
                If NewValue > ScrollBar.Maximum Then NewValue = ScrollBar.Maximum
                ScrollBar.Value = NewValue
                TopAddress = (NewValue - 1) * BytesPerLine
            Else
                ScrollBar.Enabled = False 'We can display all data
                TopAddress = 0
            End If
            If DataSize > MaxDataShown Then
                ScrollBar.Enabled = True 'More lines than we can display
                Dim BarPercent As Single = ((ScrollBar.Value - 1) / (ScrollBar.Maximum - ScrollBar.LargeChange))
                ScrollBar.LargeChange = TotalLines
                ScrollBar.Maximum = Math.Ceiling(DataSize / BytesPerLine)
                Dim NewValue As Integer = Math.Round((ScrollBar.Maximum - ScrollBar.LargeChange) * BarPercent) + 1
                If NewValue < 1 Then NewValue = 1
                If NewValue > ScrollBar.Maximum Then NewValue = ScrollBar.Maximum
                ScrollBar.Value = NewValue
                TopAddress = (NewValue - 1) * BytesPerLine
            Else
                ScrollBar.Enabled = False 'We can display all data
                TopAddress = 0
            End If
            ScrollBar.Refresh() 'Recently added
            If TopAddress < 0 Then TopAddress = 0
            Dim DataToGet As Integer = DataSize - TopAddress 'The amount of bytes we need to display in the box
            If DataToGet > MaxDataShown Then
                DataToGet = MaxDataShown
            End If
            ByteCountShowing = DataToGet
            MyScreenData = GetInternalData(TopAddress, DataToGet)
            If MyScreenData IsNot Nothing Then
                Dim AddrIndex As Integer = 0
                Dim LinesToDraw As Integer = CInt(Math.Ceiling(DataToGet / BytesPerLine))
                For i = 0 To LinesToDraw - 1
                    Dim BytesForLine() As Byte
                    If DataToGet > BytesPerLine Then
                        ReDim BytesForLine(BytesPerLine - 1)
                    Else
                        ReDim BytesForLine(DataToGet - 1)
                    End If
                    Array.Copy(MyScreenData, AddrIndex, BytesForLine, 0, BytesForLine.Length)
                    Drawline(i, TopAddress + AddrIndex + MyDataOffset, BytesPerLine, BytesForLine, gfx)
                    AddrIndex += BytesPerLine
                    DataToGet -= BytesPerLine
                Next
            End If
            PB.Image = newBG
            gfx = Nothing
            newBG = Nothing
            InRefresh = False
            If Not TopAddress = CurrentTop Then
                CurrentTop = TopAddress
                RaiseEvent AddressUpdate(CurrentTop)
            End If
        End If
    End Sub

    Private Sub Drawline(ByVal LineIndex As Integer, ByVal FullAddr As UInt64, ByVal ByteCount As Integer, ByRef data() As Byte, ByRef gfx As Graphics)
        Dim YLOC As Integer = (LineIndex * 13) + 1
        Dim NumOfAddrBytes As Integer = MyDataBits / 4
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

    Private Function GetNumOfVisibleLines() As Int32
        Return CInt(Math.Floor(PB.Height / 13))
    End Function
    'Returns the number of hex (32bit boundry) that we can display
    Private Function GetNumOfHeaderLines() As Int32
        Dim NumOfAddrBytes As Integer = MyDataBits / 4
        Dim x As Integer = PB.Width - ((NumOfAddrBytes * 8) - (NumOfAddrBytes / 2) + 6)
        Dim y As Integer = Math.Floor((x / 21) - 1)
        Do Until y Mod 4 = 0
            y = y - 1
        Loop
        Return y
    End Function

    Private Function GetAsciiForByte(ByVal b As Byte) As Char
        If b >= 32 And b <= 126 Then '32 to 126
            Return Chr(b)
        Else
            Return Chr(46) '"."
        End If
    End Function

    Private Function CreateBackground() As Image
        Dim img As Image = New Bitmap(PB.Width, PB.Height)
        SetBitmapColor(img, Brushes.White)
        'Dim gfx As Graphics = Graphics.FromImage(img) 'Incase we want to make further changes of the background
        Return img
    End Function

    Private Sub SetBitmapColor(ByRef img As Bitmap, ByVal color As Brush)
        Dim Img2 As Graphics = Graphics.FromImage(img)
        Dim myRect As New Rectangle(0, 0, img.Width, img.Height)
        Img2.FillRectangle(color, myRect)
    End Sub

    Private Sub HexEditor_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Resize
        If Not IsLoaded Then Exit Sub 'Only resize if we are shown
        GraphBG = CreateBackground()
        RefreshScreen()
    End Sub

    Private Sub PB_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles PB.MouseUp
        If MyAccessType = AccessType._ReadWrite Then
            FlushEnterBox()
            Dim InAscii As Boolean = False
            Dim p As Point = GetMouseDataIndex(e, InAscii)
            If p = Nothing Then Exit Sub
            Dim t As New InputBoxParams
            Dim BytesPerLine As Int32 = GetNumOfHeaderLines()
            t.InAscii = InAscii
            t.Address = TopAddress + ((p.Y - 1) * BytesPerLine) + (p.X - 1)
            AddTextBox(t)
        End If
    End Sub

    Private Structure InputBoxParams
        Public InAscii As Boolean 'Indicates the box is located in the "ascii" sections
        Public Text As String
        Public Address As UInt32
    End Structure

    Private Sub AddTextBox(ByVal Params As InputBoxParams)
        Dim BytesPerLine As Int32 = GetNumOfHeaderLines()
        PB.Controls.Remove(EnterBox)
        Dim offset As UInt32 = Params.Address - TopAddress
        Dim LOCY As Integer = Math.Floor(offset / BytesPerLine)
        Dim LOCX As Integer = (offset - (LOCY * BytesPerLine) + 1)
        LOCY += 1
        Dim i As Integer = ((LOCY - 1) * BytesPerLine) + LOCX
        If i > ByteCountShowing Then Exit Sub
        Dim NumOfAddrBytes As Integer = MyDataBits / 4
        Dim HexAsciiStart As Integer = (NumOfAddrBytes * 8) - (NumOfAddrBytes / 2) + 3
        Dim AsciiStart As Integer = HexAsciiStart + (BytesPerLine * 14) + 2
        Dim x As Integer
        Dim y As Integer = ((LOCY - 1) * 13)
        EnterBox = New TextBox
        EnterBox.BorderStyle = System.Windows.Forms.BorderStyle.None
        EnterBox.Font = MyFont
        EnterBox.Visible = True
        If Params.InAscii Then
            EnterBox.Width = 7
            EnterBox.Height = 8
            EnterBox.CharacterCasing = CharacterCasing.Normal
            x = (AsciiStart + ((LOCX - 1) * 7)) + 3
        Else
            EnterBox.Width = 14
            EnterBox.Height = 8
            EnterBox.CharacterCasing = CharacterCasing.Upper
            x = (HexAsciiStart + ((LOCX - 1) * 14))
        End If
        AddHandler EnterBox.KeyPress, AddressOf EnterBox_KeyPress
        AddHandler EnterBox.KeyDown, AddressOf EnterBox_KeyDown
        EnterBox.Location = New System.Drawing.Point(x, y)
        If Params.Text = "" Then
            If Params.InAscii Then
                Dim b As Byte = MyScreenData(offset)
                If b >= 32 And b <= 126 Then EnterBox.Text = GetAsciiForByte(b)
            Else
                EnterBox.Text = Hex(MyScreenData(offset)).PadLeft(2, "0")
            End If
        Else
            EnterBox.Text = Params.Text
        End If
        EnterBox.SelectionStart = 0
        EnterBox.SelectionLength = EnterBox.Text.Length
        PB.Controls.Add(EnterBox)
        EnterBox.Tag = Params
        EnterBox.Focus()
    End Sub
    'Processes only UP DOWN LEFT and RIGHT keys
    Private Sub EnterBox_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs)
        If e.KeyCode = Keys.Up Then
            e.Handled = True
            FlushEnterBox()
            Dim t As InputBoxParams = EnterBox.Tag
            Dim BytesPerLine As Int32 = GetNumOfHeaderLines()
            If Not BytesPerLine > t.Address Then
                t.Address -= BytesPerLine
                AddTextBox(t)
            End If
        ElseIf e.KeyCode = Keys.Down Then
            e.Handled = True
            FlushEnterBox()
            Dim BytesPerLine As Int32 = GetNumOfHeaderLines()
            Dim t As InputBoxParams = EnterBox.Tag
            t.Address += BytesPerLine
            AddTextBox(t)
        ElseIf e.KeyCode = Keys.Left Then
            e.Handled = True
            FlushEnterBox()
            Dim t As InputBoxParams = EnterBox.Tag
            t.Address -= 1 'Decreases our current address
            AddTextBox(t)
        ElseIf e.KeyCode = Keys.Right Then
            e.Handled = True
            FlushEnterBox()
            Dim t As InputBoxParams = EnterBox.Tag
            t.Address += 1 'Increases our current address
            AddTextBox(t)
        End If
    End Sub

    Private Sub EnterBox_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs)
        Dim t As InputBoxParams = EnterBox.Tag
        If e.KeyChar = Chr(Keys.Enter) Then
            e.Handled = True
            FlushEnterBox()
            t.Address += 1 'Increases our current address
            t.Text = ""
            AddTextBox(t)
        Else
            If t.InAscii Then
                e.Handled = True
                Dim b As Byte = Asc(e.KeyChar)
                If (b >= 32 And b <= 126) Then
                    FlushEnterBox()
                    t.Address += 1
                    AddTextBox(t)
                End If
            Else
                If EnterBox.SelectionLength = 0 AndAlso EnterBox.Text.Length > 1 Then
                    e.Handled = True
                    FlushEnterBox()
                    t.Address += 1 'Increases our current address
                    If Utilities.IsDataType.Hex(CStr(e.KeyChar)) Then
                        t.Text = CStr(e.KeyChar)
                        AddTextBox(t)
                        EnterBox.SelectionLength = 0
                        EnterBox.SelectionStart = 1
                    Else
                        AddTextBox(t)
                    End If
                Else
                    If Not Utilities.IsDataType.Hex(e.KeyChar) Then e.Handled = True
                End If
            End If
        End If
    End Sub

    Private Sub FlushEnterBox()
        If EnterBox Is Nothing Then Exit Sub
        Dim textinput As String = EnterBox.Text.Trim
        If textinput = "" Then Exit Sub
        Dim t As InputBoxParams = EnterBox.Tag
        If MyAccessMode = AccessMode._Cached Then
            MyDataBuffer(t.Address) = CByte(Utilities.HexToInt(textinput))
        ElseIf MyAccessMode = AccessMode._Stream Or MyAccessMode = AccessMode._StreamNoCache Then 'We need to update the stream
            If t.InAscii Then
                RaiseEvent StreamUpdate(t.Address, CByte(Asc(textinput)))
            Else
                RaiseEvent StreamUpdate(t.Address, CByte(Utilities.HexToInt(textinput)))
            End If
        End If
        RefreshScreen() 'Reloads data
    End Sub

    Private Function GetMouseDataIndex(ByVal e As System.Windows.Forms.MouseEventArgs, ByRef InAscii As Boolean) As Point
        Dim X_INDEX As Integer 'The position of the selected character
        Dim Y_INDEX As Integer
        Dim BytesPerLine As Int32 = GetNumOfHeaderLines()
        Dim NumOfAddrBytes As Integer = MyDataBits / 4
        Dim HexAsciiStart As Integer = (NumOfAddrBytes * 8) - (NumOfAddrBytes / 2) + 3
        Dim AsciiStart As Integer = HexAsciiStart + (BytesPerLine * 14) + 2
        Dim AsciiEnd As Integer = AsciiStart + (BytesPerLine * 7) + 2
        If e.X > HexAsciiStart And e.X < (AsciiStart - 1) Then
            Dim offset As Single = ((e.X - HexAsciiStart) - 1) / 14
            X_INDEX = Math.Floor(offset) + 1
            InAscii = False
        ElseIf e.X > (AsciiStart + 4) And e.X < (AsciiEnd - 1) Then
            Dim offset As Single = ((e.X - (AsciiStart + 4)) / 7)
            X_INDEX = Math.Floor(offset) + 1
            InAscii = True
        Else
            Return Nothing
        End If
        Dim s As Single = (e.Y / 13)
        Y_INDEX = Math.Floor(s) + 1
        Return New Point(X_INDEX, Y_INDEX)
    End Function
    'Causes the editor to redraw at the specified address
    Public Sub GotoAddress(ByVal ThisAddr As UInt64)
        Try
            If ThisAddr + 1 > MyDataBuffer.Length Then ThisAddr = MyDataBuffer.Length
            Dim BytesPerLine As Int32 = GetNumOfHeaderLines() 'Each column = 1 byte
            Dim MyValue As Integer = Math.Floor(ThisAddr / BytesPerLine) + 1
            If MyValue > ScrollBar.Maximum Then MyValue = ScrollBar.Maximum
            ScrollBar.Value = MyValue
        Catch ex As Exception
        End Try
        RefreshScreen()
    End Sub
    'Causes the editor to update (and refresh) all of the data currently being shown (avoids cache)
    Public Sub UpdateScreen()
        If MyDataStream IsNot Nothing Then
            For i = 0 To MyDataStream.Length - 1
                MyDataStream(i) = False
            Next
        End If
        RefreshScreen()
    End Sub

    Private DoRefreshOnPaint As Boolean = False

    Private Sub PB_Paint(sender As Object, e As System.Windows.Forms.PaintEventArgs) Handles PB.Paint
        If DoRefreshOnPaint Then RefreshScreen() : DoRefreshOnPaint = False
    End Sub


End Class
