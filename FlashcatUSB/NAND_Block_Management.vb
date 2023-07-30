Imports FlashcatUSB.FlashMemory.NAND_LAYOUT_TOOL

Public Class NAND_Block_Management
    Private FCUSB As USB.HostClient.FCUSB_DEVICE

    Private SELECTED_BLOCK As Integer = -1
    Private PERFORMING_ANALYZE As Boolean = False
    Delegate Sub cbSetStatus(ByVal msg As String)
    Delegate Sub cbSetImage(ByVal img As Image)
    Delegate Sub cbSetExit(ByVal allow As Boolean)

    Private CancelAnalyze As Boolean = False

    Private BLK_GRN As Image = My.Resources.BLOCK_GREEN
    Private BLK_BLU As Image = My.Resources.BLOCK_BLUE
    Private BLK_BLK As Image = My.Resources.BLOCK_BLACK
    Private BLK_RED As Image = My.Resources.BLOCK_RED
    Private BLK_CHK As Image = My.Resources.BLOCK_CHK
    Private BLK_UNK As Image = My.Resources.BLOCK_MARIO

    Private Property NAND_NAME As String 'Name of this nand device
    Private Property PAGE_SIZE_TOTAL As UInt32 'This is the entire size of the pages 
    Private Property PAGE_COUNT As UInt32 'Number of pages per block
    Private Property BLOCK_COUNT As UInt32 'Number of blocks per device
    Private Property NAND_LAYOUT As NANDLAYOUT_STRUCTURE

    Public Sub SetDeviceParameters(ByVal Name As String, ByVal p_size As UInt32, ByVal pages_per_block As UInt32, ByVal block_count As UInt32, ByVal n_layout As NANDLAYOUT_STRUCTURE)
        Me.NAND_NAME = Name
        Me.PAGE_SIZE_TOTAL = p_size
        Me.PAGE_COUNT = pages_per_block
        Me.BLOCK_COUNT = block_count
        Me.NAND_LAYOUT = n_layout
    End Sub

    Sub New(ByVal usb_dev As USB.HostClient.FCUSB_DEVICE)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        FCUSB = usb_dev
    End Sub

    Private Sub NAND_Block_Management_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim total_block_size As UInt32 = Me.PAGE_SIZE_TOTAL * Me.PAGE_COUNT
        Me.MinimumSize = Me.Size
        Me.MaximumSize = New Point(Me.Size.Width, 5000)
        Me.Text = "NAND Block Management (" & Me.NAND_NAME & ")"
        Dim TotalRowsNeeded As Integer = Me.BLOCK_COUNT / 32
        Me.lbl_desc.Text = "NAND Block Map (" & Format(Me.BLOCK_COUNT, "#,###") & " total blocks; " & Format(total_block_size, "#,###") & " bytes each)"
        BlockMap.Width = 600
        BlockMap.Height = (TotalRowsNeeded * 14) + 8
        DrawImage()
        If MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Combined Then
            cb_write_bad_block_marker.Enabled = False
        End If
    End Sub

    Private Declare Function ShowScrollBar Lib "user32.dll" (ByVal hWnd As IntPtr, ByVal wBar As Integer, ByVal bShow As Boolean) As Boolean

    Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
        Try
            ShowScrollBar(Panel1.Handle, 0, False)
        Catch ex As Exception
            Exit Sub
        End Try
        MyBase.WndProc(m)
    End Sub

    Private Sub DrawBlockImage(ByVal x As Integer, ByVal y As Integer, ByVal status As NAND_BLOCK_IF.BLOCK_STATUS, ByRef g As Graphics)
        Try
            Dim offset As Integer = 56
            Select Case status
                Case NAND_BLOCK_IF.BLOCK_STATUS.Valid
                    g.DrawImage(BLK_GRN, offset + (x * 14) + 4, (y * 14) + 4, 14, 14)
                Case NAND_BLOCK_IF.BLOCK_STATUS.Bad_Marked
                    g.DrawImage(BLK_BLU, offset + (x * 14) + 4, (y * 14) + 4, 14, 14) 'Lets make this blue in the future
                Case NAND_BLOCK_IF.BLOCK_STATUS.Bad_Manager
                    g.DrawImage(BLK_BLK, offset + (x * 14) + 4, (y * 14) + 4, 14, 14)
                Case NAND_BLOCK_IF.BLOCK_STATUS.Bad_ByError
                    g.DrawImage(BLK_RED, offset + (x * 14) + 4, (y * 14) + 4, 14, 14)
                Case NAND_BLOCK_IF.BLOCK_STATUS.Unknown
                    g.DrawImage(BLK_UNK, offset + (x * 14) + 4, (y * 14) + 4, 14, 14)
            End Select
        Catch ex As Exception
        End Try
    End Sub

    Private Sub cmdClose_Click(sender As Object, e As EventArgs) Handles cmdClose.Click
        Me.Close()
    End Sub

    Private Sub BlockMap_MouseMove(sender As Object, e As MouseEventArgs) Handles BlockMap.MouseMove
        If PERFORMING_ANALYZE Then Exit Sub
        Dim PreviousBlock As Integer = SELECTED_BLOCK
        If ((e.X <= 60) Or (e.Y <= 4)) Or (e.X >= 508) Then
            SELECTED_BLOCK = -1
        Else
            Dim x As Integer = Math.Floor((e.X - 60) / 14)
            Dim y As Integer = Math.Floor((e.Y - 4) / 14)
            SELECTED_BLOCK = (y * 32) + x
            If SELECTED_BLOCK > FCUSB.NAND_IF.MAP.Count - 1 Then SELECTED_BLOCK = -1
        End If
        If Not PreviousBlock = SELECTED_BLOCK Then
            DrawImage()
        End If
        If (SELECTED_BLOCK = -1) Then
            MyStatus.Text = ""
        Else
            Dim block_info As NAND_BLOCK_IF.MAPPING = FCUSB.NAND_IF.MAP(SELECTED_BLOCK)
            Dim page_addr As UInt32 = (block_info.BlockIndex * PAGE_COUNT)
            MyStatus.Text = "Selected page: 0x" & Hex(page_addr).PadLeft(6, "0") & " [block: " & block_info.BlockIndex & "]"
            Select Case block_info.Status
                Case NAND_BLOCK_IF.BLOCK_STATUS.Valid
                    MyStatus.Text &= " (valid)"
                Case NAND_BLOCK_IF.BLOCK_STATUS.Bad_Marked
                    MyStatus.Text &= " (user discarded)"
                Case NAND_BLOCK_IF.BLOCK_STATUS.Bad_Manager
                    MyStatus.Text &= " (bad marker)"
                Case NAND_BLOCK_IF.BLOCK_STATUS.Bad_ByError
                    MyStatus.Text &= " (write error)"
            End Select
        End If
    End Sub

    Private Sub BlockMap_MouseLeave(sender As Object, e As EventArgs) Handles BlockMap.MouseLeave
        If PERFORMING_ANALYZE Then Exit Sub
        MyStatus.Text = ""
        Dim PreviousBlock As Integer = SELECTED_BLOCK
        SELECTED_BLOCK = -1
        If Not PreviousBlock = SELECTED_BLOCK Then
            DrawImage()
        End If
    End Sub

    Private Sub DrawImage()
        Try
            Dim my_bmp As Bitmap = New Bitmap(BlockMap.Width, BlockMap.Height)
            Dim gfx As Graphics = Graphics.FromImage(my_bmp)
            gfx.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias
            Dim x As Integer = 0
            Dim y As Integer = 0
            Dim MyFont As New Font("Lucida Console", 8)
            For i As UInt32 = 0 To Me.BLOCK_COUNT - 1
                Dim block_info As NAND_BLOCK_IF.MAPPING = FCUSB.NAND_IF.MAP(i)
                If x = 0 Then
                    Dim hex_str As String = "0x" & Hex(block_info.PageAddress).PadLeft(6, "0")
                    gfx.DrawString(hex_str, MyFont, Brushes.Black, 0, (y * 14) + 5)
                End If
                DrawBlockImage(x, y, block_info.Status, gfx)
                If (i = SELECTED_BLOCK) Then
                    If PERFORMING_ANALYZE Then
                        gfx.DrawImage(BLK_CHK, 60 + (x * 14), (y * 14) + 4, 14, 14)
                    Else
                        gfx.DrawRectangle(Pens.Black, 60 + (x * 14), 4 + (y * 14), 13, 13)
                    End If
                End If
                x = x + 1
                If x = 32 Then
                    x = 0
                    y = y + 1
                End If
            Next
            SetImage(my_bmp)
        Catch ex As Exception
        End Try
    End Sub

    Private Sub BlockMap_DoubleClick(sender As Object, e As EventArgs) Handles BlockMap.DoubleClick
        Try
            If PERFORMING_ANALYZE Then Exit Sub
            If SELECTED_BLOCK = -1 Then Exit Sub
            Dim block_info As NAND_BLOCK_IF.MAPPING = FCUSB.NAND_IF.MAP(SELECTED_BLOCK)
            If block_info.Status = NAND_BLOCK_IF.BLOCK_STATUS.Valid Then
                block_info.Status = NAND_BLOCK_IF.BLOCK_STATUS.Bad_Marked
            Else
                block_info.Status = NAND_BLOCK_IF.BLOCK_STATUS.Valid
            End If
            DrawImage()
        Catch ex As Exception
        End Try
    End Sub

    Public Sub SetStatus(ByVal Msg As String)
        If Me.InvokeRequired Then
            Dim d As New cbSetStatus(AddressOf SetStatus)
            Me.Invoke(d, New Object() {[Msg]})
        Else
            MyStatus.Text = Msg
            Application.DoEvents()
        End If
    End Sub

    Public Sub SetImage(ByVal img As Image)
        If Me.InvokeRequired Then
            Dim d As New cbSetImage(AddressOf SetImage)
            Me.Invoke(d, New Object() {img})
        Else
            BlockMap.Image = img
            BlockMap.Refresh()
            Application.DoEvents()
        End If
    End Sub

    Public Sub SetExit(ByVal Allow As Boolean)
        If Me.InvokeRequired Then
            Dim d As New cbSetExit(AddressOf SetExit)
            Me.Invoke(d, New Object() {Allow})
        Else
            cmdClose.Enabled = Allow
            If Allow Then
                cmdAnalyze.Text = "Analyze"
            Else
                cmdAnalyze.Text = "Cancel"
            End If
            Me.Enabled = True
        End If
    End Sub

    Private Sub cmdAnalyze_Click(sender As Object, e As EventArgs) Handles cmdAnalyze.Click
        Me.Enabled = False
        If (cmdAnalyze.Text = "Analyze") Then
            If MsgBox("Warning, this operation will erase and overwrite all blocks, continue?", MsgBoxStyle.YesNo, "Confirm operation") = MsgBoxResult.Yes Then
                Dim td As New Threading.Thread(AddressOf AnalyzeTd)
                td.Start()
            Else
                Me.Enabled = True
            End If
        Else
            CancelAnalyze = True
            Application.DoEvents()
            Threading.Thread.Sleep(50)
        End If
    End Sub

    Private Sub AnalyzeTd()
        Dim verify_data() As Byte
        CancelAnalyze = False
        Threading.Thread.CurrentThread.Name = "AnalyzeTd"
        Dim BadBlockCounter As Integer = 0
        SetExit(False)
        PERFORMING_ANALYZE = True
        SELECTED_BLOCK = -1
        Try
            For i As UInt32 = 0 To FCUSB.NAND_IF.MAP.Count - 1
                FCUSB.NAND_IF.MAP(i).Status = NAND_BLOCK_IF.BLOCK_STATUS.Unknown
            Next
            DrawImage()
            Dim total_block_size As UInt32 = Me.PAGE_SIZE_TOTAL * Me.PAGE_COUNT
            Dim test_data(total_block_size - 1) As Byte
            For i = 0 To test_data.Length - 1
                test_data(i) = ((i + 1) And 255)
            Next
            For i As UInt32 = 0 To FCUSB.NAND_IF.MAP.Count - 1
                If CancelAnalyze Then
                    For counter As UInt32 = i To FCUSB.NAND_IF.MAP.Count - 1
                        FCUSB.NAND_IF.MAP(counter).Status = NAND_BLOCK_IF.BLOCK_STATUS.Valid
                    Next
                    Exit For
                End If
                Dim block_info As NAND_BLOCK_IF.MAPPING = FCUSB.NAND_IF.MAP(i)
                SELECTED_BLOCK = i
                DrawImage() 'Draw checkbox
                SetStatus("Verifying block: 0x" & block_info.BlockIndex.ToString.PadLeft(4, "0"))
                Dim ErrorCount As Integer = 0
                Dim ValidBlock As Boolean = True
                Do 'Write block up to 3 times
                    ValidBlock = True
                    FCUSB.NAND_IF.ERASEBLOCK(block_info.PageAddress, FlashMemory.FlashArea.Main, False)
                    FCUSB.NAND_IF.WRITEPAGE(block_info.PageAddress, test_data, FlashMemory.FlashArea.All)
                    Utilities.Sleep(20)
                    verify_data = FCUSB.NAND_IF.READPAGE(block_info.PageAddress, 0, test_data.Length, FlashMemory.FlashArea.All)
                    If Not Utilities.ArraysMatch(verify_data, test_data) Then
                        ErrorCount += 1
                        ValidBlock = False
                    End If
                    If ValidBlock Then Exit Do
                Loop While (ErrorCount < 3)
                FCUSB.NAND_IF.ERASEBLOCK(block_info.PageAddress, FlashMemory.FlashArea.Main, False) 'Erase again
                If ValidBlock Then
                    FCUSB.NAND_IF.MAP(i).Status = NAND_BLOCK_IF.BLOCK_STATUS.Valid
                Else
                    If (cb_write_bad_block_marker.Enabled AndAlso cb_write_bad_block_marker.Checked) Then 'Lets mark the block
                        Dim LastPageAddr As UInt32 = (block_info.PageAddress + Me.PAGE_COUNT - 1) 'The last page of this block
                        Dim first_page() As Byte = Nothing
                        Dim second_page() As Byte = Nothing
                        Dim last_page() As Byte = Nothing
                        Dim oob_area As Integer = NAND_LAYOUT.Layout_Main 'offset of where the oob starts
                        Dim markers As Integer = MySettings.NAND_BadBlockMarkers
                        If (markers And FlashcatSettings.BadBlockMarker._1stByte_FirstPage) > 0 Then
                            If first_page Is Nothing Then ReDim first_page(PAGE_SIZE_TOTAL - 1) : Utilities.FillByteArray(first_page, 255)
                            first_page(oob_area) = 0
                        End If
                        If (markers And FlashcatSettings.BadBlockMarker._1stByte_SecondPage) > 0 Then
                            If second_page Is Nothing Then ReDim second_page(PAGE_SIZE_TOTAL - 1) : Utilities.FillByteArray(second_page, 255)
                            second_page(oob_area) = 0
                        End If
                        If (markers And FlashcatSettings.BadBlockMarker._1stByte_LastPage) > 0 Then
                            If last_page Is Nothing Then ReDim last_page(PAGE_SIZE_TOTAL - 1) : Utilities.FillByteArray(last_page, 255)
                            last_page(oob_area) = 0
                        End If
                        If (markers And FlashcatSettings.BadBlockMarker._6thByte_FirstPage) > 0 Then
                            If first_page Is Nothing Then ReDim first_page(PAGE_SIZE_TOTAL - 1) : Utilities.FillByteArray(first_page, 255)
                            first_page(oob_area + 5) = 0
                        End If
                        If (markers And FlashcatSettings.BadBlockMarker._6thByte_SecondPage) > 0 Then
                            If second_page Is Nothing Then ReDim second_page(PAGE_SIZE_TOTAL - 1) : Utilities.FillByteArray(second_page, 255)
                            second_page(oob_area + 5) = 0
                        End If
                        If first_page IsNot Nothing Then
                            FCUSB.NAND_IF.WRITEPAGE(block_info.PageAddress, first_page, FlashMemory.FlashArea.All)
                        End If
                        If second_page IsNot Nothing Then
                            FCUSB.NAND_IF.WRITEPAGE(block_info.PageAddress + 1, second_page, FlashMemory.FlashArea.All)
                        End If
                        If last_page IsNot Nothing Then
                            FCUSB.NAND_IF.WRITEPAGE(LastPageAddr, last_page, FlashMemory.FlashArea.All)
                        End If
                    End If
                    FCUSB.NAND_IF.MAP(i).Status = NAND_BLOCK_IF.BLOCK_STATUS.Bad_ByError
                    BadBlockCounter += 1
                End If
            Next
        Catch ex As Exception
        Finally
            SetStatus("NAND blocks analyzed (" & BadBlockCounter & " bad blocks found)")
            SELECTED_BLOCK = -1
            PERFORMING_ANALYZE = False
            SetExit(True)
            DrawImage()
        End Try
    End Sub

End Class