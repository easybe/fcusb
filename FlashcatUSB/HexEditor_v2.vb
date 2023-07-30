Public Class HexEditor_v2
    Public Property BaseOffset As Long = 0 'This is to offset the drawing
    Public Property BaseSize As Long = 0 'Number of bytes this hex view can display
    Public Property TopAddress As Long = 0 'The first address of the hex editor we can see
    Public Property HexDataByteSize As Integer 'number of bits the left side displays

    Public Event AddressUpdate(ByVal top_address As Long) 'Updates the TopAddress
    Public Event RequestData(ByVal address As Long, ByRef data() As Byte)

    Private HexView_AtBottom As Boolean = False
    Private MyFont As New Font("Lucida Console", 8)
    Private PreCache() As Byte = Nothing 'Can contain the entire data to display
    Private ScreenData() As Byte 'Contains a cache of the data that the editor can see
    Private IsLoaded As Boolean = False
    Private Background As Image
    Private BytesPerLine As Integer = -1
    Private LastScroll As DateTime = DateTime.Now
    Private InRefresh As Boolean = False
    Private char_width As Single 'number of pixels (with fraction) that it takes to print a single char
    Private char_height As Single
    Private Delegate Sub cbDrawScreen()

    Private WithEvents SBAR As ScrollBar
    Private WithEvents PBOX As PictureBox

    Private PTR_HEX_POINT As Integer
    Private PTR_ASCII_POINT As Integer
    Private PTR_END As Integer

    Private PTR_CHAR_SIZE As Integer

    Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        SBAR = New VScrollBar
        PBOX = New PictureBox
        Me.Controls.Add(PBOX)
        Me.Controls.Add(SBAR)
        ResizeControls()
        SBAR.Enabled = False
    End Sub

    Private Sub ResizeControls()
        If Me.Width < 100 Then Me.Width = 100
        If Me.Height < 100 Then Me.Height = 100
        PBOX.BorderStyle = BorderStyle.FixedSingle
        PBOX.Location = New Point(4, 0)
        PBOX.Width = Me.Width - 8
        PBOX.Height = Me.Height - 8
        PBOX.BackColor = Color.LightGray
        PBOX.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top Or AnchorStyles.Bottom
        SBAR.Top = 1
        SBAR.Height = Me.Height - 9
        SBAR.Left = Me.Width - 23
        SBAR.Width = 18
        SBAR.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Right
        SBAR.BringToFront()
    End Sub

    Private Sub HexEditor_v2_Load(sender As Object, e As EventArgs) Handles Me.Load
        HexEdit_Init()
    End Sub
    'This is what creates the hex viewer (streams data via event calls)
    Public Sub CreateHexViewer(ByVal Base As Long, ByVal Size As Long)
        ResizeControls()
        Me.BaseOffset = Base
        Me.BaseSize = Size
        Me.TopAddress = 0
        If (Me.BaseSize <= &HFFFF) Then
            HexDataByteSize = 2 '0000:
        ElseIf (Me.BaseSize <= &HFFFFFF) Then '000000:
            HexDataByteSize = 3
        ElseIf (Me.BaseSize <= &HFFFFFFFFL) Then '00000000:
            HexDataByteSize = 4
        Else '0000000000:
            HexDataByteSize = 5
        End If
        SBAR.Minimum = 1
        If (PBOX.Height > 0) Then
            Background = CreateBackground()
            PBOX.Image = Background.Clone
        End If
        IsLoaded = True
    End Sub
    'This creates the hex viewer from a cached resorce
    Public Sub CreateHexViewer(ByVal Base As Long, ByVal PreLoadData() As Byte)
        ResizeControls()
        Me.BaseOffset = Base
        Me.BaseSize = PreLoadData.Length
        Me.TopAddress = 0
        PreCache = PreLoadData
        If (Me.BaseSize <= &HFFFFL) Then
            HexDataByteSize = 2 '0000:
        ElseIf (Me.BaseSize <= &HFFFFFFL) Then '000000:
            HexDataByteSize = 3
        ElseIf (Me.BaseSize <= &HFFFFFFFFL) Then '00000000:
            HexDataByteSize = 4
        Else
            HexDataByteSize = 5
        End If
        SBAR.Minimum = 1
        If (PBOX.Height > 0) Then
            Background = CreateBackground()
            PBOX.Image = Background.Clone
        End If
        IsLoaded = True
        Me.UpdateScreen()
    End Sub

    Public Sub CloseHexViewer()
        Try
            TopAddress = 0
            HexDataByteSize = 0
            PBOX.Image = Nothing
            IsLoaded = False
            SBAR.Enabled = False
        Catch ex As Exception
        End Try
    End Sub

    Public Sub HexEditor_Resize(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Resize
        Try
            If Not IsLoaded Then Exit Sub 'Only resize if we are shown
            If (PBOX.Height > 0) Then
                Background = CreateBackground()
                Dim visible_lines As Integer = GetNumOfVisibleLines()
                BytesPerLine = GetVisisbleDataAreaCount() 'Each column = 1 byte
                If BytesPerLine > 0 Then
                    SBAR.LargeChange = visible_lines
                    SBAR.Maximum = CInt(Math.Ceiling(BaseSize / BytesPerLine))
                    If HexView_AtBottom Then
                        SBAR.Value = SBAR.Maximum - visible_lines + 1
                    ElseIf (SBAR.Value + visible_lines) > SBAR.Maximum Then
                        If Not SBAR.Maximum - visible_lines < 0 Then
                            SBAR.Value = SBAR.Maximum - visible_lines + 1
                        End If
                    Else
                        Dim x As Integer = GetVisisbleDataAreaCount()
                        SBAR.Value = Math.Floor(TopAddress / x) + 1
                    End If
                    UpdateScreen()
                End If
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Function GetNumOfVisibleLines() As Integer
        Return CInt(Math.Floor(PBOX.Height / (char_height + 1)))
    End Function
    'Returns the number of hex (32bit boundry) that we can display
    Public Function GetVisisbleDataAreaCount() As Integer
        Try
            If (PBOX.Width < 100) Then Return 0
            Dim addr_area As Integer = ((Me.HexDataByteSize * 2) * char_width)
            Dim x As Single = PBOX.Width - (SBAR.Width + addr_area)
            Dim y As Single = (x / 3)
            Dim hex_area As Single = (y * 2) - (CSng(PBOX.Width) / 12.0F) - 2
            Dim hex_pair_size As Single = (char_width * 2) + 1.2
            Dim hex_count As Integer = Math.Floor(hex_area / hex_pair_size)
            Do Until hex_count Mod 4 = 0
                hex_count = hex_count - 1
            Loop
            Return hex_count
        Catch ex As Exception
        End Try
        Return 0
    End Function

    Private Sub VSbar_Scroll(sender As Object, e As ScrollEventArgs) Handles SBAR.Scroll
        If InRefresh Then Exit Sub
        If DateTime.Compare(LastScroll.AddMilliseconds(50), DateTime.Now) > 0 Then
            Exit Sub 'We only want to allow a scroll to happen no closer than 250 ms
        End If
        LastScroll = DateTime.Now
        UpdateScreen()
    End Sub

    Private Function CreateBackground() As Image
        Try
            Dim bg_img As Image = New Bitmap(PBOX.Width, PBOX.Height)
            SetBitmapColor(bg_img, Brushes.White)
            If char_width = 0 Then
                Dim gfx As Graphics = Graphics.FromImage(bg_img)
                Dim blank_chars As String = "00000000000000000000"
                Dim chr_size As SizeF = gfx.MeasureString(blank_chars, MyFont) 'We want to know how wide it takes to draw 20 chars
                char_width = chr_size.Width / blank_chars.Length
                char_height = chr_size.Height
            End If
            Return bg_img
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Private Sub SetBitmapColor(ByRef img As Bitmap, color As Brush)
        Dim Img2 As Graphics = Graphics.FromImage(img)
        Dim myRect As New Rectangle(0, 0, img.Width, img.Height)
        Img2.FillRectangle(color, myRect)
        Img2.Dispose()
    End Sub

    Public Sub UpdateScreen()
        If (Not IsLoaded) Then Exit Sub
        If InRefresh Then Exit Sub
        If SBAR.InvokeRequired Then
            Dim d As New cbDrawScreen(AddressOf UpdateScreen)
            Me.Invoke(d)
        Else
            Try : InRefresh = True
                Static CurrentTop As Long = 0
                BytesPerLine = GetVisisbleDataAreaCount() 'Each column = 1 byte
                Dim TotalLines As Int32 = GetNumOfVisibleLines()
                If TotalLines = 0 Or BytesPerLine = 0 Then Exit Sub
                Dim newBG As Image = Background.Clone
                Using gfx As Graphics = Graphics.FromImage(newBG)
                    Dim MaxDataShown As Long = (TotalLines * BytesPerLine) 'Total amount of bytes that we can show
                    HexView_AtBottom = False
                    If (BaseSize > MaxDataShown) Then
                        SBAR.LargeChange = CInt(TotalLines)
                        SBAR.Maximum = CInt(Math.Ceiling(BaseSize / BytesPerLine))
                        SBAR.Enabled = True 'More lines than we can display
                        TopAddress = (CLng(SBAR.Value - 1) * CLng(BytesPerLine))
                        If (TopAddress + MaxDataShown) >= BaseSize Then
                            HexView_AtBottom = True
                        End If
                    Else
                        SBAR.Enabled = False 'We can display all data
                        TopAddress = 0
                    End If
                    SBAR.Refresh() 'Recently added
                    Dim DataToGet As Long = BaseSize - TopAddress 'The amount of bytes we need to display in the box
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
                        Dim AddrIndex As Long = 0
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
                        EditMode_DrawSelectedItem(newBG)
                    End If
                    PBOX.Image = newBG
                End Using
                If (Not TopAddress = CurrentTop) Then
                    CurrentTop = TopAddress
                    RaiseEvent AddressUpdate(CurrentTop)
                End If
            Catch ex As Exception
            Finally
                InRefresh = False
            End Try
        End If
    End Sub

    Private Sub Drawline(LineIndex As Int32, FullAddr As UInt64, ByteCount As Integer, data() As Byte, gfx As Graphics)
        Dim YLOC As Integer = (LineIndex * Math.Round(char_height + 1)) + 1
        Dim AddrStr As String = Hex(FullAddr).PadLeft(Me.HexDataByteSize * 2, "0") & ": "
        gfx.DrawString(AddrStr, MyFont, Brushes.Gray, 0, YLOC)
        Dim w As SizeF = gfx.MeasureString(AddrStr, MyFont)
        PTR_CHAR_SIZE = Math.Ceiling(w.Width / CSng(AddrStr.Length))
        PTR_HEX_POINT = w.Width
        PTR_ASCII_POINT = w.Width + (BytesPerLine * ((PTR_CHAR_SIZE * 2) + 2)) + 8
        Dim hex_location As Integer = PTR_HEX_POINT
        Dim ascii_location As Integer = PTR_ASCII_POINT
        For i = 0 To data.Length - 1
            Dim current_byte As Byte = data(i)
            If Me.EDIT_MODE AndAlso HexEditMode_IsChanged(Me.TopAddress + (LineIndex * ByteCount) + i, current_byte) Then
                gfx.DrawString(Hex(current_byte).PadLeft(2, "0"), MyFont, Brushes.Red, New Point(hex_location, YLOC))
                gfx.DrawString(GetAsciiForByte(current_byte), MyFont, Brushes.Red, New Point(ascii_location, YLOC))
            Else
                gfx.DrawString(Hex(current_byte).PadLeft(2, "0"), MyFont, Brushes.Black, New Point(hex_location, YLOC))
                gfx.DrawString(GetAsciiForByte(current_byte), MyFont, Brushes.Gray, New Point(ascii_location, YLOC))
            End If
            hex_location += (PTR_CHAR_SIZE * 2) + 2
            ascii_location += PTR_CHAR_SIZE + 2
        Next
        PTR_END = ascii_location
    End Sub

    Private Function GetAsciiForByte(b As Byte) As Char
        If b >= 32 And b <= 126 Then '32 to 126
            Return ChrW(b)
        Else
            Return ChrW(46) '"."
        End If
    End Function
    'Causes the editor to redraw at the specified address
    Public Sub GotoAddress(ThisAddr As Long)
        Try
            If BytesPerLine = -1 Then Exit Sub
            If (ThisAddr + 1) > BaseSize Then ThisAddr = BaseSize
            Dim MyValue As Long = Math.Floor(ThisAddr / BytesPerLine) + 1
            If MyValue > SBAR.Maximum Then MyValue = SBAR.Maximum
            SBAR.Value = MyValue
        Catch ex As Exception
        End Try
        UpdateScreen()
    End Sub

    Private Sub PBOX_MouseWheel(sender As Object, e As MouseEventArgs) Handles PBOX.MouseWheel
        If InRefresh Then Exit Sub
        If DateTime.Compare(LastScroll.AddMilliseconds(100), DateTime.Now) > 0 Then Exit Sub
        CloseEditBoxes()
        Dim num_vis_rows As Integer = GetNumOfVisibleLines()
        Dim total_rows As Integer = SBAR.Maximum
        If e.Delta > 0 Then 'Moving up
            Dim new_value As Integer = SBAR.Value - num_vis_rows
            If new_value < 1 Then new_value = 1
            SBAR.Value = new_value
        Else 'Moving down
            Dim new_value As Integer = SBAR.Value + num_vis_rows
            If (new_value + num_vis_rows) > SBAR.Maximum Then
                new_value = SBAR.Maximum - num_vis_rows + 1
            End If
            If new_value < SBAR.Minimum Then new_value = SBAR.Minimum
            SBAR.Value = new_value
        End If
        UpdateScreen()
        LastScroll = DateTime.Now
    End Sub

#Region "Edit Mode"
    Public HexEdit_Changes As New List(Of DATA_HEX_CHANGE)

    Private EditMode_Enabled As Boolean = False
    Private SELECTED_HEX_ITEM As New Point(-1, -1)
    Private SELECTED_ASCII_ITEM As New Point(-1, -1)
    Private EditBoxMouseClick As New Point(-1, -1)

    Private WithEvents HEX_BOX As HexByteBox
    Private WithEvents ASCII_BOX As AsciiByteBox

    Private ASCII_CARRY As Char

    Private Sub HexEdit_Init()
        HEX_BOX = New HexByteBox()
        HEX_BOX.Visible = True
        HEX_BOX.Font = MyFont
        HEX_BOX.Width = 20
        HEX_BOX.BorderStyle = BorderStyle.None
        ASCII_BOX = New AsciiByteBox
        ASCII_BOX.Visible = True
        ASCII_BOX.Font = MyFont
        ASCII_BOX.Width = 12
        ASCII_BOX.BorderStyle = BorderStyle.None
    End Sub

    Public Class DATA_HEX_CHANGE
        Public address As Long
        Public original_byte As Byte
        Public new_byte As Byte
    End Class

    Public Property EDIT_MODE As Boolean
        Get
            Return EditMode_Enabled
        End Get
        Set(value As Boolean)
            EditMode_Enabled = value
            If value Then
                HexEdit_Changes.Clear()
            Else
                CloseEditBoxes()
                EditBoxMouseClick = New Point(-1, -1)
                SELECTED_HEX_ITEM = New Point(-1, -1)
                SELECTED_ASCII_ITEM = New Point(-1, -1)
            End If
            UpdateScreen()
        End Set
    End Property

    'Draw a box around the item that is currently selected
    Private Sub EditMode_DrawSelectedItem(img As Image)
        Dim TotalLines As Int32 = GetNumOfVisibleLines()
        If EditMode_HasEditBox() Then Exit Sub
        If (SELECTED_HEX_ITEM.X > -1) Then
            If SELECTED_HEX_ITEM.Y > (TotalLines - 1) Then Exit Sub
            Dim box_x As Integer = (PTR_HEX_POINT + (SELECTED_HEX_ITEM.X * PTR_CHAR_SIZE * 2)) + (SELECTED_HEX_ITEM.X * 2) + 1
            Dim box_y As Integer = (SELECTED_HEX_ITEM.Y * (char_height + 1))
            Dim box_width As Integer = (PTR_CHAR_SIZE * 2) + 1
            Dim box_height As Integer = Math.Round(char_height - 2) + 1
            For x = box_x To box_x + box_width - 1
                For y = box_y To box_y + box_height - 1
                    If (x < img.Width) AndAlso (y < img.Height) Then
                        Dim pixcolor As Color = DirectCast(img, Bitmap).GetPixel(x, y)
                        pixcolor = Color.FromArgb(Not pixcolor.R, Not pixcolor.G, Not pixcolor.B)
                        DirectCast(img, Bitmap).SetPixel(x, y, pixcolor)
                    End If
                Next
            Next
        ElseIf (SELECTED_ASCII_ITEM.X > -1) Then
            If SELECTED_ASCII_ITEM.Y > (TotalLines - 1) Then Exit Sub
            Dim box_x As Integer = (PTR_ASCII_POINT + (SELECTED_ASCII_ITEM.X * PTR_CHAR_SIZE * 1)) + (SELECTED_ASCII_ITEM.X * 2) + 1
            Dim box_y As Integer = (SELECTED_ASCII_ITEM.Y * (char_height + 1))
            Dim box_width As Integer = (PTR_CHAR_SIZE) + 1
            Dim box_height As Integer = Math.Round(char_height - 2) + 1
            For x = box_x To box_x + box_width - 1
                For y = box_y To box_y + box_height - 1
                    If (x < img.Width) AndAlso (y < img.Height) Then
                        Dim pixcolor As Color = DirectCast(img, Bitmap).GetPixel(x, y)
                        pixcolor = Color.FromArgb(Not pixcolor.R, Not pixcolor.G, Not pixcolor.B)
                        DirectCast(img, Bitmap).SetPixel(x, y, pixcolor)
                    End If
                Next
            Next
        End If
    End Sub

    Private Function HexEditMode_IsChanged(addr As Long, ByRef new_data As Byte) As Boolean
        For i = 0 To HexEdit_Changes.Count - 1
            If HexEdit_Changes(i).address = addr Then
                new_data = HexEdit_Changes(i).new_byte
                Return True
            End If
        Next
        Return False
    End Function

    Private Sub PBOX_MouseClick(sender As Object, e As MouseEventArgs) Handles PBOX.MouseClick
        If Me.EDIT_MODE Then
            If (SELECTED_HEX_ITEM.X > -1) Then
                EditBoxMouseClick = Cursor.Position
                SelectHexItem()
            ElseIf (SELECTED_ASCII_ITEM.X > -1) Then
                EditBoxMouseClick = Cursor.Position
                SelectAsciiItem()
            Else
                EditBoxMouseClick = New Point(-1, -1)
            End If
        End If
    End Sub

    Private Sub PBOX_MouseMove(sender As Object, e As MouseEventArgs) Handles PBOX.MouseMove
        Static MouseIsMoving As Boolean = False
        If MouseIsMoving Then Exit Sub
        If Not Me.EDIT_MODE Then Exit Sub
        Dim PerformScreenUpdate As Boolean = False
        If Not (PointDiff(EditBoxMouseClick, Cursor.Position) > 4) Then Exit Sub
        Try : MouseIsMoving = True
            If (e.X >= PTR_HEX_POINT AndAlso e.X < PTR_ASCII_POINT - 8) Then
                SELECTED_ASCII_ITEM = New Point(-1, -1)
                Dim hex_x_offset As Integer = Math.Floor(((e.X - PTR_HEX_POINT) / (PTR_CHAR_SIZE + 1)) / 2)
                Dim hex_y_offset As Integer = Math.Floor((e.Y / (char_height + 1)) + 0)
                If Not SELECTED_HEX_ITEM.Equals(New Point(hex_x_offset, hex_y_offset)) Then
                    SELECTED_HEX_ITEM = New Point(hex_x_offset, hex_y_offset)
                    PerformScreenUpdate = True
                End If
            ElseIf (e.X >= PTR_ASCII_POINT AndAlso e.X < PTR_END) Then
                SELECTED_HEX_ITEM = New Point(-1, -1)
                Dim hex_x_offset As Integer = Math.Floor(((e.X - PTR_ASCII_POINT) / (PTR_CHAR_SIZE + 2)) / 1)
                Dim hex_y_offset As Integer = Math.Floor((e.Y / (char_height + 1)) + 0)
                If (hex_x_offset < BytesPerLine) Then
                    If Not SELECTED_ASCII_ITEM.Equals(New Point(hex_x_offset, hex_y_offset)) Then
                        SELECTED_ASCII_ITEM = New Point(hex_x_offset, hex_y_offset)
                        PerformScreenUpdate = True
                    End If
                End If
            Else
                Dim BlankPt As New Point(-1, -1)
                PerformScreenUpdate = (Not SELECTED_ASCII_ITEM.Equals(BlankPt) Or Not SELECTED_HEX_ITEM.Equals(BlankPt))
                SELECTED_ASCII_ITEM = New Point(-1, -1)
                SELECTED_HEX_ITEM = New Point(-1, -1)
            End If
        Finally
            MouseIsMoving = False
        End Try
        If PerformScreenUpdate Then
            CloseEditBoxes()
            UpdateScreen()
        End If
    End Sub

    Private Sub PBOX_MouseLeave(sender As Object, e As EventArgs) Handles PBOX.MouseLeave
        If Not Me.EDIT_MODE Then Exit Sub
        If (Not EditMode_HasEditBox()) Then
            Dim PerformScreenUpdate As Boolean = False
            Dim BlankPt As New Point(-1, -1)
            PerformScreenUpdate = (Not SELECTED_ASCII_ITEM.Equals(BlankPt) Or Not SELECTED_HEX_ITEM.Equals(BlankPt))
            SELECTED_ASCII_ITEM = New Point(-1, -1)
            SELECTED_HEX_ITEM = New Point(-1, -1)
            If PerformScreenUpdate Then UpdateScreen()
        End If
    End Sub

    Private Sub CloseEditBoxes()
        Dim PerformScreenUpdate As Boolean = False
        If (PBOX.Controls.Contains(HEX_BOX)) Then
            If (Not HEX_BOX.ByteData = HEX_BOX.InitialData) Then
                EditMode_AddChange(HEX_BOX.HexAddress, HEX_BOX.ByteData, HEX_BOX.InitialData)
                PerformScreenUpdate = True
            End If
            PBOX.Controls.Remove(HEX_BOX)
            EditBoxMouseClick = New Point(-1, -1)
            SELECTED_HEX_ITEM = New Point(-1, -1)
            ShowCursor()
        ElseIf (PBOX.Controls.Contains(ASCII_BOX)) Then
            If (Not ASCII_BOX.ByteData = ASCII_BOX.InitialData) AndAlso ASCII_BOX.ByteData <> 0 Then
                EditMode_AddChange(ASCII_BOX.HexAddress, ASCII_BOX.ByteData, ASCII_BOX.InitialData)
                PerformScreenUpdate = True
            End If
            PBOX.Controls.Remove(ASCII_BOX)
            EditBoxMouseClick = New Point(-1, -1)
            SELECTED_ASCII_ITEM = New Point(-1, -1)
            ShowCursor()
        End If
        If PerformScreenUpdate Then UpdateScreen()
    End Sub

    Private Sub EditMode_AddChange(device_addr As Long, new_data As Byte, org_data As Byte)
        For i = 0 To HexEdit_Changes.Count - 1
            If HexEdit_Changes(i).address = device_addr Then
                If HexEdit_Changes(i).original_byte = new_data Then
                    HexEdit_Changes.RemoveAt(i)
                Else
                    HexEdit_Changes(i).new_byte = new_data
                End If
                Exit Sub
            End If
        Next
        HexEdit_Changes.Add(New DATA_HEX_CHANGE With {.address = device_addr, .new_byte = new_data, .original_byte = org_data})
    End Sub

    Private Function EditMode_HasEditBox() As Boolean
        If PBOX.Controls.Contains(HEX_BOX) Then Return True
        If PBOX.Controls.Contains(ASCII_BOX) Then Return True
        Return False
    End Function

    Private Sub SelectHexItem()
        Dim box_x As Integer = (PTR_HEX_POINT + (SELECTED_HEX_ITEM.X * PTR_CHAR_SIZE * 2)) + (SELECTED_HEX_ITEM.X * 2) + 2
        Dim box_y As Integer = (SELECTED_HEX_ITEM.Y * (char_height + 1))
        Dim sel_data As Integer = (SELECTED_HEX_ITEM.Y * BytesPerLine) + SELECTED_HEX_ITEM.X
        If sel_data >= ScreenData.Length Then
            SELECTED_HEX_ITEM = New Point(-1, -1)
            SELECTED_ASCII_ITEM = New Point(-1, -1)
            Exit Sub
        End If
        Dim data As Byte = ScreenData(sel_data)
        HexEditMode_IsChanged(Me.TopAddress + sel_data, data)
        HEX_BOX.Location = New Point(box_x, box_y)
        HEX_BOX.InitialData = data
        HEX_BOX.HexAddress = Me.TopAddress + sel_data
        HEX_BOX.Width = (PTR_CHAR_SIZE * 2) + 1
        For i = 0 To 10
            DirectCast(PBOX.Image, Bitmap).SetPixel(box_x - 1, box_y + i, Color.White)
        Next
        PBOX.Refresh()
        PBOX.Controls.Add(HEX_BOX)
        HEX_BOX.Focus()
        HEX_BOX.SelectAll()
        HideCursor()
    End Sub

    Private Sub SelectHexItem(address As Long)
        Try
            Dim topaddr As Long = Me.TopAddress
            Dim v_lines As Integer = GetNumOfVisibleLines()
            Dim b_per_line As Integer = BytesPerLine
            If address < Me.TopAddress Then Exit Sub
            If address > (Me.TopAddress + (v_lines * b_per_line)) Then Exit Sub
            Dim offset As Long = address - Me.TopAddress
            SELECTED_HEX_ITEM.X = (offset Mod b_per_line)
            SELECTED_HEX_ITEM.Y = Math.Floor(offset / v_lines)
            SELECTED_ASCII_ITEM = New Point(-1, -1)
            SelectHexItem()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub SelectAsciiItem()
        Dim box_x As Integer = (PTR_ASCII_POINT + (SELECTED_ASCII_ITEM.X * PTR_CHAR_SIZE * 1)) + (SELECTED_ASCII_ITEM.X * 2) + 2
        Dim box_y As Integer = (SELECTED_ASCII_ITEM.Y * (char_height + 1))
        Dim sel_data As Integer = (SELECTED_ASCII_ITEM.Y * BytesPerLine) + SELECTED_ASCII_ITEM.X
        If sel_data >= ScreenData.Length Then
            SELECTED_HEX_ITEM = New Point(-1, -1)
            SELECTED_ASCII_ITEM = New Point(-1, -1)
            Exit Sub
        End If
        Dim data As Byte = ScreenData(sel_data)
        HexEditMode_IsChanged(Me.TopAddress + sel_data, data)
        If ASCII_BOX IsNot Nothing AndAlso PBOX.Controls.Contains(ASCII_BOX) Then
            PBOX.Controls.Remove(ASCII_BOX)
        End If
        ASCII_BOX.Location = New Point(box_x, box_y)
        ASCII_BOX.InitialData = data
        ASCII_BOX.HexAddress = Me.TopAddress + sel_data
        ASCII_BOX.Width = (PTR_CHAR_SIZE) + 1
        For i = 0 To 10
            DirectCast(PBOX.Image, Bitmap).SetPixel(box_x - 1, box_y + i, Color.White)
        Next
        PBOX.Refresh()
        PBOX.Controls.Add(ASCII_BOX)
        ASCII_BOX.Focus()
        If ASCII_CARRY.Equals("") Then
            ASCII_BOX.SelectAll()
        Else
            ASCII_BOX.Text = ASCII_CARRY
            ASCII_BOX.SelectionLength = 0
            ASCII_BOX.SelectionStart = 1
        End If
        HideCursor()
    End Sub

    Private Sub SelectAsciiItem(address As Long)
        Try
            Dim topaddr As Long = Me.TopAddress
            Dim v_lines As Integer = GetNumOfVisibleLines()
            Dim b_per_line As Integer = BytesPerLine
            If address < Me.TopAddress Then Exit Sub
            If address > (Me.TopAddress + (v_lines * b_per_line)) Then Exit Sub
            Dim offset As Long = address - Me.TopAddress
            SELECTED_ASCII_ITEM.X = (offset Mod b_per_line)
            SELECTED_ASCII_ITEM.Y = Math.Floor(offset / v_lines)
            SELECTED_HEX_ITEM = New Point(-1, -1)
            SelectAsciiItem()
        Catch ex As Exception
        End Try
    End Sub

#Region "HEX_BOX Events"
    Private Sub EditHexBox_EnterPressed() Handles HEX_BOX.EnterKeyPressed
        If (Not HEX_BOX.ByteData = HEX_BOX.InitialData) Then
            EditMode_AddChange(HEX_BOX.HexAddress, HEX_BOX.ByteData, HEX_BOX.InitialData)
            UpdateScreen()
        End If
        If (HEX_BOX.HexAddress <> Me.BaseSize - 1) Then
            If Not HEX_BOX.HexAddress + 1 >= Me.BaseSize Then
                SelectHexItem(HEX_BOX.HexAddress + 1)
            End If
        End If
    End Sub

    Private Sub EditHexBox_Escape() Handles HEX_BOX.EscapeKeyPress
        CloseEditBoxes()
    End Sub

    Private Sub EditHexBox_CarryByte(carry_char As Char) Handles HEX_BOX.CarryByte
        If carry_char = vbCr Then Exit Sub
        If carry_char = ChrW(27) Then Exit Sub
        Dim carry_str As String = carry_char.ToString.ToUpper
        If Not ((carry_str Like "[A-F]") Or (carry_str Like "#")) Then Exit Sub 'HEX value check
        If (Not HEX_BOX.ByteData = HEX_BOX.InitialData) Then
            EditMode_AddChange(HEX_BOX.HexAddress, HEX_BOX.ByteData, HEX_BOX.InitialData)
            UpdateScreen()
        End If
        If (HEX_BOX.HexAddress <> Me.BaseSize - 1) Then
            If Not HEX_BOX.HexAddress + 1 >= Me.BaseSize Then
                SelectHexItem(HEX_BOX.HexAddress + 1)
                HEX_BOX.Text = carry_str
            End If
        End If
    End Sub

    Private Sub HEX_BOX_MouseLeave(sender As Object, e As EventArgs) Handles HEX_BOX.MouseLeave
        If Not MouseIsOverControl(PBOX) Then
            CloseEditBoxes()
        End If
    End Sub

#End Region

#Region "ASCII_BOX Events"
    Private Sub EditAsciiBox_EnterPressed() Handles ASCII_BOX.EnterKeyPressed
        If (Not ASCII_BOX.ByteData = ASCII_BOX.InitialData) AndAlso ASCII_BOX.ByteData <> 0 Then
            EditMode_AddChange(ASCII_BOX.HexAddress, ASCII_BOX.ByteData, ASCII_BOX.InitialData)
            UpdateScreen()
        End If
        If (ASCII_BOX.HexAddress <> BaseSize - 1) Then
            If Not ASCII_BOX.HexAddress + 1 >= Me.BaseSize Then
                SelectAsciiItem(ASCII_BOX.HexAddress + 1)
            End If
        End If
    End Sub

    Private Sub EditAsciiBox_Carry(carry_char As Char) Handles ASCII_BOX.CarryByte
        If carry_char = vbCr Then
            CloseEditBoxes()
            Exit Sub
        End If
        If carry_char = ChrW(27) Then Exit Sub
        If (Not ASCII_BOX.ByteData = ASCII_BOX.InitialData) Then
            EditMode_AddChange(ASCII_BOX.HexAddress, ASCII_BOX.ByteData, ASCII_BOX.InitialData)
            UpdateScreen()
        End If
        If (ASCII_BOX.HexAddress <> Me.BaseSize - 1) Then
            If Not ASCII_BOX.HexAddress + 1 >= Me.BaseSize Then
                SelectAsciiItem(ASCII_BOX.HexAddress + 1)
                ASCII_BOX.Text = carry_char.ToString
            End If
        End If
    End Sub

    Private Sub ASCII_BOX_MouseLeave(sender As Object, e As EventArgs) Handles ASCII_BOX.MouseLeave
        If Not MouseIsOverControl(PBOX) Then
            CloseEditBoxes()
        End If
    End Sub

#End Region

#End Region

    Dim CursorVisible As Boolean = True

    Private Sub HideCursor()
        If CursorVisible Then
            Cursor.Hide()
            CursorVisible = False
        End If
    End Sub

    Private Sub ShowCursor()
        If Not CursorVisible Then
            Cursor.Show()
            CursorVisible = True
        End If
    End Sub
    'Number of pixels between two points
    Private Function PointDiff(p1 As Point, p2 As Point) As Integer
        Return Math.Sqrt((Math.Abs(p2.X - p1.X) ^ 2) + (Math.Abs(p2.Y - p1.Y) ^ 2))
    End Function

    Public Function MouseIsOverControl(c As Control) As Boolean
        Return c.ClientRectangle.Contains(c.PointToClient(Control.MousePosition))
    End Function

End Class
