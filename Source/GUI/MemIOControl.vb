Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.MemControl_v2

Public Class MemIOControl
    Public Event WriteConsole(msg As String) 'Writes the console/windows console
    Public Event SetStatus(msg As String) 'Sets the text on the status bar
    Public Event MemoryAreaChanged(new_area As FlashArea)
    Public Event AbortOperation()
    Public Event ReadOperation(rd As XFER_Operation)
    Public Event WriteOperation(wr As XFER_Operation)
    Public Event EraseOperation()
    Public Event CompareOperation(vr As CompareParams)
    Public Event EditModeToggle(is_checked As Boolean)
    Public Property FlashName As String
    Public Property FlashAvailable As Long
    Public Property FlashBase As Long 'The base address of the device
    Public Property SREC_DATAWIDTH As SREC.RECORD_DATAWIDTH = SREC.RECORD_DATAWIDTH.BYTE
    Public Property InitialDirectory As String = ""
    Public Property LastDirectory As String = ""

    Private MyMemArea As FlashArea
    Private AllowEditButton As Boolean = True

    Public Property MemoryArea As FlashArea
        Get
            Return Me.MyMemArea
        End Get
        Set(value As FlashArea)
            Select Case value
                Case FlashArea.Main
                    cmd_area.Text = RM?.GetString("mc_button_main")
                Case FlashArea.OOB
                    cmd_area.Text = RM?.GetString("mc_button_spare")
                Case FlashArea.All
                    cmd_area.Text = RM?.GetString("mc_button_all")
            End Select
            Me.MyMemArea = value
        End Set
    End Property

    Public Property AllowEdit As Boolean
        Get
            Return AllowEditButton
        End Get
        Set(value As Boolean)
            AllowEditButton = value
            cmd_edit.Visible = value
        End Set
    End Property

    Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
    End Sub

    Private Sub MemIOControl_Load(sender As Object, e As EventArgs) Handles Me.Load
        Try
            If RM IsNot Nothing Then
                Me.menu_tip.SetToolTip(Me.cmd_read, RM.GetString("mc_button_read"))
                Me.menu_tip.SetToolTip(Me.cmd_write, RM.GetString("mc_button_write"))
                Me.menu_tip.SetToolTip(Me.cmd_erase, RM.GetString("mc_button_erase"))
                Me.menu_tip.SetToolTip(Me.cmd_compare, RM.GetString("mc_button_compare"))
                Me.MemoryArea = FlashArea.Main
            End If
            MemAreaVisible(False)
            CancelButton(False)
            If AllowEditButton Then
                cmd_edit.Visible = True
            Else
                cmd_edit.Visible = False
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub SetFormToMiddle(frm As Form)
        frm.StartPosition = FormStartPosition.Manual
        Dim parent_control As MemControl_v2 = CType(Me.Parent.Parent, MemControl_v2)
        Dim parent_point As Point = parent_control.PointToScreen(Point.Empty)
        Dim middle As Point = New Point(CInt(parent_point.X + (parent_control.Width / 2)), CInt(parent_point.Y + (parent_control.Height / 2)))
        frm.Location = New Point(CInt(middle.X - (frm.Width / 2)), CInt(middle.Y - (frm.Height / 2)))
    End Sub

    Private Sub cmd_read_Click(sender As Object, e As EventArgs) Handles cmd_read.Click
        Try
            RaiseEvent SetStatus(String.Format(RM.GetString("mc_mem_read_from"), FlashName))
            Dim RangeBox As New FrmRangeForm(Me.FlashBase, FlashAvailable)
            SetFormToMiddle(RangeBox)
            RangeBox.cmd_okay.Text = RM.GetString("mc_button_ok")
            RangeBox.cmd_cancel.Text = RM.GetString("mc_button_cancel")
            RangeBox.lbl_base.Text = RM.GetString("mc_rngbox_base")
            RangeBox.lbl_length.Text = RM.GetString("mc_rngbox_len")
            If (RangeBox.ShowDialog = DialogResult.Cancel) Then
                RaiseEvent SetStatus(RM.GetString("mc_mem_read_canceled")) : Exit Sub
            End If
            RaiseEvent SetStatus(RM.GetString("mc_mem_read_start"))
            Dim StartingAddr As Long = RangeBox.BaseAddress + RangeBox.BaseOffset
            Dim DefaultName As String = FlashName.Replace(" ", "_") & "_" & Utilities.Pad(Hex(StartingAddr)) &
                "-" & Utilities.Pad(Hex((StartingAddr + RangeBox.RangeSize - 1)))
            Dim TargetIO As IO.FileInfo = Nothing
            Dim create_file_type As FileFilterIndex
            If CreateFileForRead(DefaultName, TargetIO, create_file_type) Then
                Dim read_params As New XFER_Operation 'We want to remember the last operation
                read_params.FileType = create_file_type
                read_params.FileName = TargetIO
                read_params.Offset = RangeBox.BaseAddress
                read_params.Size = RangeBox.RangeSize
                RaiseEvent ReadOperation(read_params)
            Else
                RaiseEvent SetStatus(RM.GetString("mc_io_save_canceled"))
                Exit Sub
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub cmd_write_Click(sender As Object, e As EventArgs) Handles cmd_write.Click
        Try
            Dim fn As IO.FileInfo = Nothing
            Dim open_file_type As FileFilterIndex
            If Not OpenFileForWrite(fn, open_file_type) Then Exit Sub
            If (Not fn.Exists) OrElse (fn.Length = 0) Then
                RaiseEvent SetStatus(RM.GetString("mc_wr_oper_file_err")) : Exit Sub
            End If
            RaiseEvent SetStatus(RM.GetString("mc_wr_oper_start"))
            RaiseEvent SetStatus(String.Format(RM.GetString("mc_select_range"), FlashName))
            Dim wr_operation As New XFER_Operation
            wr_operation.FileType = open_file_type
            wr_operation.FileName = fn
            Dim RangeBox As New FrmRangeForm(Me.FlashBase, FlashAvailable, fn.Length)
            SetFormToMiddle(RangeBox)
            RangeBox.cmd_okay.Text = RM.GetString("mc_button_ok")
            RangeBox.cmd_cancel.Text = RM.GetString("mc_button_cancel")
            RangeBox.lbl_base.Text = RM.GetString("mc_rngbox_base")
            RangeBox.lbl_length.Text = RM.GetString("mc_rngbox_len")
            If (RangeBox.ShowDialog = DialogResult.Cancel) Then
                RaiseEvent SetStatus(RM.GetString("mc_wr_user_canceled")) : Exit Sub
            End If
            wr_operation.Offset = (RangeBox.BaseAddress + RangeBox.BaseOffset)
            wr_operation.Size = RangeBox.RangeSize
            RaiseEvent WriteOperation(wr_operation)
        Catch ex As Exception
        End Try
    End Sub

    Private Sub cmd_erase_Click(sender As Object, e As EventArgs) Handles cmd_erase.Click
        If MsgBox(RM.GetString("mc_erase_warning"), MsgBoxStyle.YesNo, String.Format(RM.GetString("mc_erase_confirm"), FlashName)) = MsgBoxResult.Yes Then
            RaiseEvent WriteConsole(String.Format(RM.GetString("mc_erase_command_sent"), FlashName))
            RaiseEvent SetStatus(RM.GetString("mem_erasing_device"))
            RaiseEvent EraseOperation()
        End If
    End Sub

    Private Sub cmd_cancel_Click(sender As Object, e As EventArgs) Handles cmd_cancel.Click
        Try
            Me.cmd_cancel.Enabled = False
            RaiseEvent AbortOperation()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub cmd_compare_Click(sender As Object, e As EventArgs) Handles cmd_compare.Click
        Dim v_parm As New CompareParams
        If OpenFileForCompare(v_parm.local_file, v_parm.file_type) Then
            Select Case v_parm.file_type
                Case FileFilterIndex.Binary
                    v_parm.CompareData = v_parm.local_file.OpenRead()
                    v_parm.Count = v_parm.local_file.Length
                Case FileFilterIndex.IntelHex
                    Dim sr As New IHEX.StreamReader(v_parm.local_file.OpenText)
                    v_parm.CompareData = sr
                    v_parm.Count = sr.Length
                Case FileFilterIndex.SRecord
                    Dim sr As New SREC.StreamReader(v_parm.local_file.OpenText, Me.SREC_DATAWIDTH)
                    v_parm.CompareData = sr
                    v_parm.Count = sr.Length
            End Select
            Dim RangeBox As New FrmRangeForm(Me.FlashBase, FlashAvailable, v_parm.Count)
            SetFormToMiddle(RangeBox)
            RangeBox.cmd_okay.Text = RM.GetString("mc_button_ok")
            RangeBox.cmd_cancel.Text = RM.GetString("mc_button_cancel")
            RangeBox.lbl_base.Text = RM.GetString("mc_rngbox_base")
            RangeBox.lbl_length.Text = RM.GetString("mc_rngbox_len")
            If (RangeBox.ShowDialog = DialogResult.Cancel) Then
                RaiseEvent SetStatus(RM.GetString("mc_io_compare_canceled")) : Exit Sub
            End If
            v_parm.BaseOffset = RangeBox.BaseOffset
            v_parm.Count = RangeBox.RangeSize
            RaiseEvent CompareOperation(v_parm)
        End If
    End Sub

    Private Function OpenFileForWrite(ByRef file As IO.FileInfo, ByRef file_type As FileFilterIndex) As Boolean
        Dim BinFile As String = "Binary Files (*.bin)|*.bin"
        Dim IntelHexFormat As String = "Intel Hex Format (*.hex)|*.hex"
        Dim SrecFormat As String = "S-REC Format (*.srec)|*.srec"
        Dim AllFiles As String = "All files (*.*)|*.*"
        Dim OpenMe As New OpenFileDialog
        OpenMe.AddExtension = True
        OpenMe.InitialDirectory = Me.InitialDirectory
        OpenMe.Title = String.Format(RM.GetString("mc_io_file_choose"), FlashName)
        OpenMe.CheckPathExists = True
        OpenMe.Filter = BinFile & "|" & IntelHexFormat & "|" & SrecFormat & "|" & AllFiles 'Bin Files, Hex Files, SREC, All Files
        If OpenMe.ShowDialog = DialogResult.OK Then
            file = New IO.FileInfo(OpenMe.FileName)
            Me.LastDirectory = file.Directory.FullName
            Select Case OpenMe.FilterIndex
                Case 1
                    file_type = FileFilterIndex.Binary
                Case 2
                    file_type = FileFilterIndex.IntelHex
                Case 3
                    file_type = FileFilterIndex.SRecord
                Case 4
                    file_type = FileFilterIndex.Binary
            End Select
            RaiseEvent SetStatus(String.Format(RM.GetString("mc_io_file_writing"), FlashName))
            Return True
        Else
            RaiseEvent SetStatus(String.Format(RM.GetString("mc_io_file_cancel_to"), FlashName))
            Return False
        End If
    End Function

    Private Function CreateFileForRead(DefaultName As String, ByRef file As IO.FileInfo, ByRef file_type As FileFilterIndex) As Boolean
        Try
            Dim SaveMe As New SaveFileDialog
            SaveMe.AddExtension = True
            SaveMe.InitialDirectory = Me.InitialDirectory
            SaveMe.Title = RM.GetString("mc_io_save_type")
            SaveMe.CheckPathExists = True
            SaveMe.FileName = DefaultName.Replace("/", "-")
            Dim BinFile As String = "Binary Files (*.bin)|*.bin"
            Dim IntelHexFormat As String = "Intel Hex Format (*.hex)|*.hex"
            Dim SrecFormat As String = "S-REC Format (*.srec)|*.srec"
            Dim AllFiles As String = "All files (*.*)|*.*"
            SaveMe.Filter = BinFile & "|" & IntelHexFormat & "|" & SrecFormat & "|" & AllFiles
            If SaveMe.ShowDialog = DialogResult.OK Then
                file = New IO.FileInfo(SaveMe.FileName)
                Me.LastDirectory = file.Directory.FullName
                If file.Exists Then file.Delete()
                Select Case SaveMe.FilterIndex
                    Case 1
                        file_type = FileFilterIndex.Binary
                    Case 2
                        file_type = FileFilterIndex.IntelHex
                    Case 3
                        file_type = FileFilterIndex.SRecord
                    Case 4
                        file_type = FileFilterIndex.Binary
                End Select
                Return True
            End If
        Catch ex As Exception
        End Try
        Return False
    End Function

    Private Function OpenFileForCompare(ByRef file As IO.FileInfo, ByRef file_type As FileFilterIndex) As Boolean
        Dim BinFile As String = "Binary Files (*.bin)|*.bin"
        Dim IntelHexFormat As String = "Intel Hex Format (*.hex)|*.hex"
        Dim SrecFormat As String = "S-REC Format (*.srec)|*.srec"
        Dim AllFiles As String = "All files (*.*)|*.*"
        Dim OpenMe As New OpenFileDialog
        OpenMe.AddExtension = True
        OpenMe.InitialDirectory = Me.InitialDirectory
        OpenMe.Title = String.Format(RM.GetString("mc_compare_selected"), FlashName) '"File selected, verifying {0}"
        OpenMe.CheckPathExists = True
        OpenMe.Filter = BinFile & "|" & IntelHexFormat & "|" & SrecFormat & "|" & AllFiles
        If OpenMe.ShowDialog = DialogResult.OK Then
            file = New IO.FileInfo(OpenMe.FileName)
            Me.LastDirectory = file.Directory.FullName
            Select Case OpenMe.FilterIndex
                Case 1
                    file_type = FileFilterIndex.Binary
                Case 2
                    file_type = FileFilterIndex.IntelHex
                Case 3
                    file_type = FileFilterIndex.SRecord
                Case 4
                    file_type = FileFilterIndex.Binary
            End Select
            RaiseEvent SetStatus(String.Format(RM.GetString("mc_compare_selected"), FlashName)) ' "File selected, verifying {0}"
            Return True
        Else
            RaiseEvent SetStatus(String.Format(RM.GetString("mc_compare_canceled"), FlashName)) '"User canceled compare"
            Return False
        End If
    End Function

    Private Sub cmd_area_Click(sender As Object, e As EventArgs) Handles cmd_area.Click
        Select Case Me.MemoryArea
            Case FlashArea.Main
                Me.MemoryArea = FlashArea.OOB
            Case FlashArea.OOB
                Me.MemoryArea = FlashArea.All
            Case FlashArea.All
                Me.MemoryArea = FlashArea.Main
        End Select
        RaiseEvent MemoryAreaChanged(Me.MemoryArea)
    End Sub

    Private Sub cmd_edit_CheckedChanged(sender As Object, e As EventArgs) Handles cmd_edit.CheckedChanged
        If cmd_edit.Checked AndAlso cmd_edit.Enabled Then
            RaiseEvent EditModeToggle(True)
        ElseIf Not cmd_edit.Checked AndAlso cmd_edit.Enabled Then
            RaiseEvent EditModeToggle(False)
        End If
    End Sub

    Public Sub ReadButton(enabled As Boolean)
        If cmd_read.InvokeRequired Then
            cmd_read.Invoke(Sub() ReadButton(enabled))
        Else
            cmd_read.Enabled = enabled
        End If
    End Sub

    Public Sub WriteButton(enabled As Boolean)
        If cmd_write.InvokeRequired Then
            cmd_write.Invoke(Sub() WriteButton(enabled))
        Else
            cmd_write.Enabled = enabled
        End If
    End Sub

    Public Sub EraseButton(enabled As Boolean)
        If cmd_erase.InvokeRequired Then
            cmd_erase.Invoke(Sub() EraseButton(enabled))
        Else
            cmd_erase.Enabled = enabled
        End If
    End Sub

    Public Sub CompareButton(enabled As Boolean)
        If cmd_compare.InvokeRequired Then
            cmd_compare.Invoke(Sub() CompareButton(enabled))
        Else
            cmd_compare.Enabled = enabled
        End If
    End Sub

    Public Sub EditButton(enabled As Boolean)
        If cmd_edit.InvokeRequired Then
            cmd_edit.Invoke(Sub() EditButton(enabled))
        Else
            cmd_edit.Enabled = enabled
        End If
    End Sub

    Public Sub MemAreaButton(enabled As Boolean)
        If cmd_area.InvokeRequired Then
            cmd_area.Invoke(Sub() MemAreaButton(enabled))
        Else
            cmd_area.Enabled = enabled
        End If
    End Sub

    Public Sub CancelButton(visible As Boolean)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() CancelButton(visible))
        Else
            If visible Then
                cmd_cancel.Enabled = True
                cmd_cancel.Visible = True
                cmd_read.Visible = False
                cmd_write.Visible = False
                cmd_erase.Visible = False
                cmd_edit.Visible = False
                cmd_compare.Visible = False
            Else
                cmd_cancel.Enabled = False
                cmd_cancel.Visible = False
                cmd_read.Visible = True
                cmd_write.Visible = True
                cmd_erase.Visible = True
                If Me.AllowEdit Then cmd_edit.Visible = True
                cmd_compare.Visible = True
            End If
        End If
    End Sub

    Public Sub EccStatus(visible As Boolean)
        If pb_ecc.InvokeRequired Then
            Dim callback As Action = Sub() EccStatus(visible)
            pb_ecc.Invoke(callback)
        Else
            pb_ecc.Visible = visible
        End If
    End Sub

    Public Sub MemAreaVisible(visible As Boolean)
        If cmd_area.InvokeRequired Then
            Dim callback As Action = Sub() MemAreaVisible(visible)
            cmd_area.Invoke(callback)
        Else
            cmd_area.Visible = visible
        End If
    End Sub

    Public Sub Buttons(enabled As Boolean)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() Buttons(enabled))
        Else
            cmd_read.Enabled = enabled
            cmd_write.Enabled = enabled
            cmd_erase.Enabled = enabled
            cmd_compare.Enabled = enabled
            cmd_area.Enabled = enabled
            cmd_edit.Enabled = enabled
        End If
    End Sub

    Public Sub EccImage(status_img As Image)
        If pb_ecc.InvokeRequired Then
            pb_ecc.Invoke(Sub() EccImage(status_img))
        Else
            pb_ecc.Image = status_img
        End If
    End Sub

    Public Function GetEditButtonStatus() As Boolean
        If cmd_edit.InvokeRequired Then
            Return CBool(cmd_edit.Invoke(Function() GetEditButtonStatus()))
        Else
            Return cmd_edit.Checked
        End If
    End Function

End Class
