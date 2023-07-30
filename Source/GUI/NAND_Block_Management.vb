Imports FlashcatUSB.FlashMemory.NAND_LAYOUT_TOOL

Public Class NAND_Block_Management
    Private SELECTED_BLOCK As Integer = -1
    Private PERFORMING_ANALYZE As Boolean = False
    Delegate Sub cbSetStatus(msg As String)
    Delegate Sub cbSetImage(img As Image)
    Delegate Sub cbSetExit(allow As Boolean)

    Private CancelAnalyze As Boolean = False

    Private BLK_GRN As Image = My.Resources.BLOCK_GREEN
    Private BLK_BLU As Image = My.Resources.BLOCK_BLUE
    Private BLK_BLK As Image = My.Resources.BLOCK_BLACK
    Private BLK_RED As Image = My.Resources.BLOCK_RED
    Private BLK_CHK As Image = My.Resources.BLOCK_CHK
    Private BLK_UNK As Image = My.Resources.BLOCK_MARIO

    Private Property NAND_NAME As String 'Name of this nand device
    Private Property PAGE_SIZE_TOTAL As UInt16 'This is the entire size of the pages 
    Private Property PAGE_COUNT As UInt16 'Number of pages per block
    Private Property BLOCK_COUNT As Integer  'Number of blocks per device
    Private Property NAND_LAYOUT As NANDLAYOUT_STRUCTURE

    Private NAND_IF As MemoryDeviceUSB

    Public Sub SetDeviceParameters(Name As String, p_size As UInt16, pages_per_block As UInt16, block_count As Integer, n_layout As NANDLAYOUT_STRUCTURE)
        Me.NAND_NAME = Name
        Me.PAGE_SIZE_TOTAL = p_size
        Me.PAGE_COUNT = pages_per_block
        Me.BLOCK_COUNT = block_count
        Me.NAND_LAYOUT = n_layout
    End Sub

    Private MyMap As List(Of NAND_BLOCK_IF.MAPPING)
    Private NandIF As FlashMemory.G_NAND

    Sub New(mem_if As MemoryDeviceUSB)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        NAND_IF = mem_if
    End Sub

    Private Sub NAND_Block_Management_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MinimumSize = Me.Size
        Me.MaximumSize = New Size(Me.Size.Width, 5000)
        If NAND_IF.GetType().Equals(GetType(PARALLEL_NAND)) Then
            Dim PNAND_IF As PARALLEL_NAND = CType(NAND_IF, PARALLEL_NAND)
            MyMap = PNAND_IF.BlockManager.MAP
        ElseIf NAND_IF.GetType().Equals(GetType(SPINAND_Programmer)) Then
            Dim SNAND_IF As SPINAND_Programmer = CType(NAND_IF, SPINAND_Programmer)
            MyMap = SNAND_IF.BlockManager.MAP
        End If
        Dim TotalRowsNeeded As Integer = (Me.BLOCK_COUNT \ 32)
        BlockMap.Width = 600
        BlockMap.Height = (TotalRowsNeeded * 14) + 8
        DrawImage()
        Language_Setup()
    End Sub

    Private Sub Language_Setup()
        Dim total_block_size As UInt32 = CUInt(Me.PAGE_SIZE_TOTAL) * Me.PAGE_COUNT
        Me.Text = String.Format(RM.GetString("nandmngr_title"), Me.NAND_NAME) '"NAND Block Management ({0})"
        Dim block_count_str As String = Format(Me.BLOCK_COUNT, "#,###")
        Dim block_size_str As String = Format(total_block_size, "#,###")
        Me.lbl_desc.Text = String.Format(RM.GetString("nandmngr_block_map"), block_count_str, block_size_str)
        Me.lbl_no_error.Text = RM.GetString("nandmngr_no_error") '"No error"
        Me.lbl_bad_block.Text = RM.GetString("nandmngr_bad_block") '"Bad block marker"
        Me.lbl_user_marked.Text = RM.GetString("nandmngr_user_marked") '"User marked"
        Me.lbl_write_error.Text = RM.GetString("nandmngr_write_error") '"Write error"
        Me.cmdAnalyze.Text = RM.GetString("nandmngr_analyze") '"Analyze"
        Me.cb_write_bad_block_marker.Text = RM.GetString("nandmngr_write_marker") '"Write BAD BLOCK markers to spare area"
        Me.cmdClose.Text = RM.GetString("nandmngr_close") '"Close"
    End Sub

    Private Declare Function ShowScrollBar Lib "user32.dll" (hWnd As IntPtr, wBar As Integer, bShow As Boolean) As Boolean

    Protected Overrides Sub WndProc(ByRef m As Message)
        Try
            ShowScrollBar(Panel1.Handle, 0, False)
        Catch ex As Exception
            Exit Sub
        End Try
        MyBase.WndProc(m)
    End Sub

    Private Sub DrawBlockImage(x As Integer, y As Integer, status As NAND_BLOCK_IF.BLOCK_STATUS, ByRef g As Graphics)
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
            Dim x As Integer = CInt(Math.Floor((e.X - 60) / 14))
            Dim y As Integer = CInt(Math.Floor((e.Y - 4) / 14))
            SELECTED_BLOCK = (y * 32) + x
            If SELECTED_BLOCK > MyMap.Count - 1 Then SELECTED_BLOCK = -1
        End If
        If Not PreviousBlock = SELECTED_BLOCK Then
            DrawImage()
        End If
        If (SELECTED_BLOCK = -1) Then
            MyStatus.Text = ""
        Else
            Dim block_info As NAND_BLOCK_IF.MAPPING = MyMap(SELECTED_BLOCK)
            Dim page_addr As Integer = (block_info.BlockIndex * PAGE_COUNT)
            Dim page_addr_str As String = "0x" & page_addr.ToString("X").PadLeft(6, "0"c)
            MyStatus.Text = String.Format(RM.GetString("nandmngr_selected_page"), page_addr_str, (block_info.BlockIndex + 1)) '"Selected page: {0} [block: {1}]"
            Select Case block_info.Status
                Case NAND_BLOCK_IF.BLOCK_STATUS.Valid
                    MyStatus.Text &= " (" & RM.GetString("nandmngr_valid") & ")"
                Case NAND_BLOCK_IF.BLOCK_STATUS.Bad_Marked
                    MyStatus.Text &= " (" & RM.GetString("nandmngr_user_discarded") & ")"
                Case NAND_BLOCK_IF.BLOCK_STATUS.Bad_Manager
                    MyStatus.Text &= " (" & RM.GetString("nandmngr_bad_marker") & ")"
                Case NAND_BLOCK_IF.BLOCK_STATUS.Bad_ByError
                    MyStatus.Text &= " (" & RM.GetString("nandmngr_write_error").ToLower & ")"
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
            For i As Integer = 0 To Me.BLOCK_COUNT - 1
                Dim block_info As NAND_BLOCK_IF.MAPPING = MyMap(i)
                If x = 0 Then
                    Dim hex_str As String = "0x" & (block_info.PagePhysical).ToString("X").PadLeft(6, "0"c)
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
            Dim block_info As NAND_BLOCK_IF.MAPPING = MyMap(SELECTED_BLOCK)
            If block_info.Status = NAND_BLOCK_IF.BLOCK_STATUS.Valid Then
                block_info.Status = NAND_BLOCK_IF.BLOCK_STATUS.Bad_Marked
            Else
                block_info.Status = NAND_BLOCK_IF.BLOCK_STATUS.Valid
            End If
            DrawImage()
        Catch ex As Exception
        End Try
    End Sub

    Public Sub SetStatus(Msg As String)
        If Me.InvokeRequired Then
            Dim d As New cbSetStatus(AddressOf SetStatus)
            Me.Invoke(d, New Object() {[Msg]})
        Else
            MyStatus.Text = Msg
            Application.DoEvents()
        End If
    End Sub

    Public Sub SetImage(img As Image)
        If Me.InvokeRequired Then
            Dim d As New cbSetImage(AddressOf SetImage)
            Me.Invoke(d, New Object() {img})
        Else
            BlockMap.Image = img
            BlockMap.Refresh()
            Application.DoEvents()
        End If
    End Sub

    Public Sub SetExit(Allow As Boolean)
        If Me.InvokeRequired Then
            Dim d As New cbSetExit(AddressOf SetExit)
            Me.Invoke(d, New Object() {Allow})
        Else
            cmdClose.Enabled = Allow
            If Allow Then
                cmdAnalyze.Text = RM.GetString("nandmngr_analyze")
            Else
                cmdAnalyze.Text = RM.GetString("mc_button_cancel")
            End If
            Me.Enabled = True
        End If
    End Sub

    Private Sub cmdAnalyze_Click(sender As Object, e As EventArgs) Handles cmdAnalyze.Click
        Me.Enabled = False
        If (cmdAnalyze.Text = RM.GetString("nandmngr_analyze")) Then
            If MsgBox(RM.GetString("nandmngr_warning"), MsgBoxStyle.YesNo, RM.GetString("nandmngr_confim")) = MsgBoxResult.Yes Then
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

        CancelAnalyze = False
        Threading.Thread.CurrentThread.Name = "AnalyzeTd"
        Dim BadBlockCounter As Integer = 0
        SetExit(False)
        PERFORMING_ANALYZE = True
        SELECTED_BLOCK = -1
        Try
            For i As Integer = 0 To MyMap.Count - 1
                MyMap(i).Status = NAND_BLOCK_IF.BLOCK_STATUS.Unknown
            Next
            DrawImage()
            Dim total_block_size As Integer = CInt(Me.PAGE_SIZE_TOTAL * Me.PAGE_COUNT)
            Dim test_data(total_block_size - 1) As Byte
            For i = 0 To test_data.Length - 1
                test_data(i) = CByte((i + 1) And 255)
            Next
            For i As Integer = 0 To MyMap.Count - 1
                If CancelAnalyze Then
                    For counter As Integer = i To MyMap.Count - 1
                        MyMap(counter).Status = NAND_BLOCK_IF.BLOCK_STATUS.Valid
                    Next
                    Exit For
                End If
                Dim block_info As NAND_BLOCK_IF.MAPPING = MyMap(i)
                SELECTED_BLOCK = i
                DrawImage() 'Draw checkbox
                Dim block_addr_str As String = "0x" & block_info.BlockIndex.ToString.PadLeft(4, "0"c)
                SetStatus(String.Format(RM.GetString("nandmngr_verifing_block"), block_addr_str))
                Dim ErrorCount As Integer = 0
                Dim ValidBlock As Boolean = True
                Do 'Write block up to 3 times
                    ValidBlock = True
                    Dim verify_data() As Byte = Nothing
                    If NAND_IF.GetType().Equals(GetType(PARALLEL_NAND)) Then
                        Dim PNAND_IF As PARALLEL_NAND = CType(NAND_IF, PARALLEL_NAND)
                        PNAND_IF.SectorErase_Physical(block_info.PagePhysical)
                        PNAND_IF.WritePage_Physical(block_info.PagePhysical, test_data, FlashMemory.FlashArea.All)
                        Utilities.Sleep(20)
                        verify_data = PNAND_IF.PageRead_Physical(block_info.PagePhysical, 0, test_data.Length, FlashMemory.FlashArea.All)
                    ElseIf NAND_IF.GetType().Equals(GetType(SPINAND_Programmer)) Then
                        Dim SNAND_IF As SPINAND_Programmer = CType(NAND_IF, SPINAND_Programmer)
                        SNAND_IF.SectorErase_Physical(block_info.PagePhysical)
                        SNAND_IF.WritePage_Physical(block_info.PagePhysical, test_data, FlashMemory.FlashArea.All)
                        Utilities.Sleep(20)
                        verify_data = SNAND_IF.PageRead_Physical(block_info.PagePhysical, 0, test_data.Length, FlashMemory.FlashArea.All)
                    End If
                    If Not Utilities.ArraysMatch(verify_data, test_data) Then
                        ErrorCount += 1
                        ValidBlock = False
                    End If
                    If ValidBlock Then Exit Do
                Loop While (ErrorCount < 3)
                If NAND_IF.GetType().Equals(GetType(PARALLEL_NAND)) Then
                    Dim PNAND_IF As PARALLEL_NAND = CType(NAND_IF, PARALLEL_NAND)
                    PNAND_IF.SectorErase_Physical(block_info.PagePhysical)
                ElseIf NAND_IF.GetType().Equals(GetType(SPINAND_Programmer)) Then
                    Dim SNAND_IF As SPINAND_Programmer = CType(NAND_IF, SPINAND_Programmer)
                    SNAND_IF.SectorErase_Physical(block_info.PagePhysical)
                End If
                If ValidBlock Then
                    MyMap(i).Status = NAND_BLOCK_IF.BLOCK_STATUS.Valid
                Else
                    If (cb_write_bad_block_marker.Enabled AndAlso cb_write_bad_block_marker.Checked) Then 'Lets mark the block
                        Dim LastPageAddr As Integer = (block_info.PagePhysical + Me.PAGE_COUNT - 1) 'The last page of this block
                        Dim first_page() As Byte = Nothing
                        Dim second_page() As Byte = Nothing
                        Dim last_page() As Byte = Nothing
                        Dim oob_area As Integer = NAND_LAYOUT.Layout_Main 'offset of where the oob starts
                        Dim markers As Integer = MySettings.NAND_BadBlockMode
                        If (markers And BadBlockMarker._1stByte_FirstPage) > 0 Then
                            If first_page Is Nothing Then ReDim first_page(PAGE_SIZE_TOTAL - 1) : Utilities.FillByteArray(first_page, 255)
                            first_page(oob_area) = 0
                        End If
                        If (markers And BadBlockMarker._1stByte_SecondPage) > 0 Then
                            If second_page Is Nothing Then ReDim second_page(PAGE_SIZE_TOTAL - 1) : Utilities.FillByteArray(second_page, 255)
                            second_page(oob_area) = 0
                        End If
                        If (markers And BadBlockMarker._1stByte_LastPage) > 0 Then
                            If last_page Is Nothing Then ReDim last_page(PAGE_SIZE_TOTAL - 1) : Utilities.FillByteArray(last_page, 255)
                            last_page(oob_area) = 0
                        End If
                        If (markers And BadBlockMarker._6thByte_FirstPage) > 0 Then
                            If first_page Is Nothing Then ReDim first_page(PAGE_SIZE_TOTAL - 1) : Utilities.FillByteArray(first_page, 255)
                            first_page(oob_area + 5) = 0
                        End If
                        If (markers And BadBlockMarker._6thByte_SecondPage) > 0 Then
                            If second_page Is Nothing Then ReDim second_page(PAGE_SIZE_TOTAL - 1) : Utilities.FillByteArray(second_page, 255)
                            second_page(oob_area + 5) = 0
                        End If
                        If NAND_IF.GetType().Equals(GetType(PARALLEL_NAND)) Then
                            Dim PNAND_IF As PARALLEL_NAND = CType(NAND_IF, PARALLEL_NAND)
                            If first_page IsNot Nothing Then
                                PNAND_IF.SectorWrite(block_info.PagePhysical, first_page)
                            End If
                            If second_page IsNot Nothing Then
                                PNAND_IF.SectorWrite(block_info.PagePhysical + 1, second_page)
                            End If
                            If last_page IsNot Nothing Then
                                PNAND_IF.SectorWrite(LastPageAddr, last_page)
                            End If
                        ElseIf NAND_IF.GetType().Equals(GetType(SPINAND_Programmer)) Then
                            Dim SNAND_IF As SPINAND_Programmer = CType(NAND_IF, SPINAND_Programmer)
                            If first_page IsNot Nothing Then
                                SNAND_IF.SectorWrite(block_info.PagePhysical, first_page)
                            End If
                            If second_page IsNot Nothing Then
                                SNAND_IF.SectorWrite(block_info.PagePhysical + 1, second_page)
                            End If
                            If last_page IsNot Nothing Then
                                SNAND_IF.SectorWrite(LastPageAddr, last_page)
                            End If
                        End If
                    End If
                    MyMap(i).Status = NAND_BLOCK_IF.BLOCK_STATUS.Bad_ByError
                    BadBlockCounter += 1
                End If
            Next
        Catch ex As Exception
        Finally
            SetStatus(String.Format(RM.GetString("nandmngr_analyzed_done"), BadBlockCounter))
            SELECTED_BLOCK = -1
            PERFORMING_ANALYZE = False
            SetExit(True)
            DrawImage()
        End Try
    End Sub

End Class