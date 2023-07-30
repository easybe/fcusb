Imports System.ComponentModel
Imports System.IO.Compression
Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.MemoryInterface
Imports FlashcatUSB.USB
Imports FlashcatUSB.Utilities

Public Class MainForm

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        GUI = Me
        Me.MinimumSize = Me.Size
        Me.MyTabs.DrawMode = TabDrawMode.OwnerDrawFixed
        Language_Setup()
        InitStatusMessage()
        LoadSettingsIntoGui()
        Dim os_ver As String = Utilities.PlatformIDToStr(Environment.OSVersion.Platform)
        PrintConsole(RM.GetString("welcome_to_flashcatusb") & ", build: " & FC_BUILD)
        PrintConsole("Copyright " & Date.Now.Year & " - Embedded Computers LLC")
        PrintConsole("Running on: " & os_ver & " (" & Utilities.GetOsBitsString() & ")")
        PrintConsole("FlashcatUSB Script Engine build: " & EC_ScriptEngine.Processor.Build)
        PrintConsole(String.Format(RM.GetString("gui_database_supported"), "Serial NOR memory", FlashDatabase.PartCount(MemoryType.SERIAL_NOR)))
        PrintConsole(String.Format(RM.GetString("gui_database_supported"), "Serial NAND", FlashDatabase.PartCount(MemoryType.SERIAL_NAND)))
        PrintConsole(String.Format(RM.GetString("gui_database_supported"), "Parallel NOR memory", FlashDatabase.PartCount(MemoryType.PARALLEL_NOR)))
        PrintConsole(String.Format(RM.GetString("gui_database_supported"), "Parallel NAND memory", FlashDatabase.PartCount(MemoryType.PARALLEL_NAND)))
        PrintConsole(String.Format(RM.GetString("gui_database_supported"), "OTP/UV EPROM memory", FlashDatabase.PartCount(MemoryType.OTP_EPROM)))
        statuspage_progress.Visible = False
        MyForm_LicenseInit()
        mi_mode_mmc.Visible = False
        mi_jtag_menu.Visible = False
    End Sub

#Region "Language"

    Private Sub Language_Setup()
        Me.mi_main_menu.Text = RM.GetString("gui_menu_main")
        Me.mi_mode_menu.Text = RM.GetString("gui_menu_mode")
        Me.mi_script_menu.Text = RM.GetString("gui_menu_script")
        Me.mi_tools_menu.Text = RM.GetString("gui_menu_tools")
        Me.mi_Language.Text = RM.GetString("gui_menu_language")
        Me.mi_detect.Text = RM.GetString("gui_menu_main_detect")
        Me.mi_repeat.Text = RM.GetString("gui_menu_main_repeat")
        Me.mi_refresh.Text = RM.GetString("gui_menu_main_refresh")
        Me.mi_exit.Text = RM.GetString("gui_menu_main_exit")
        Me.mi_mode_settings.Text = RM.GetString("gui_menu_mode_settings")
        Me.mi_verify.Text = RM.GetString("gui_menu_mode_verify")
        Me.mi_bit_swapping.Text = RM.GetString("gui_menu_mode_bitswap")
        Me.mi_endian.Text = RM.GetString("gui_menu_mode_endian")
        Me.mi_1V8.Text = String.Format(RM.GetString("gui_menu_mode_voltage"), "1.8v")
        Me.mi_3V3.Text = String.Format(RM.GetString("gui_menu_mode_voltage"), "3.3v")
        Me.mi_script_selected.Text = RM.GetString("gui_menu_script_select")
        Me.mi_script_load.Text = RM.GetString("gui_menu_script_load")
        Me.mi_script_unload.Text = RM.GetString("gui_menu_script_unload")
        Me.mi_erase_tool.Text = RM.GetString("gui_menu_tools_erase")
        Me.mi_create_img.Text = RM.GetString("gui_menu_tools_create")
        Me.mi_write_img.Text = RM.GetString("gui_menu_tools_write")
        Me.mi_nand_map.Text = RM.GetString("gui_menu_tools_mem_map")
        Me.mi_device_features.Text = RM.GetString("gui_menu_tools_vendor")
        Me.TabStatus.Text = "  " & RM.GetString("gui_tab_status") & "  "
        Me.TabConsole.Text = "  " & RM.GetString("gui_tab_console") & "  "
        Me.FlashStatusLabel.Text = RM.GetString("gui_status_welcome")
        Me.lblStatus.Text = RM.GetString("gui_fcusb_disconnected")
    End Sub

    Private Sub mi_language_english_Click(sender As Object, e As EventArgs) Handles mi_language_english.Click
        RM = My.Resources.english.ResourceManager : MySettings.LanguageName = "English"
        Language_Setup()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_language_spanish_Click(sender As Object, e As EventArgs) Handles mi_language_spanish.Click
        RM = My.Resources.spanish.ResourceManager : MySettings.LanguageName = "Spanish"
        Language_Setup()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_language_french_Click(sender As Object, e As EventArgs) Handles mi_language_french.Click
        RM = My.Resources.french.ResourceManager : MySettings.LanguageName = "French"
        Language_Setup()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_language_portuguese_Click(sender As Object, e As EventArgs) Handles mi_language_portuguese.Click
        RM = My.Resources.portuguese.ResourceManager : MySettings.LanguageName = "Portuguese"
        Language_Setup()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_language_russian_Click(sender As Object, e As EventArgs) Handles mi_language_russian.Click
        RM = My.Resources.russian.ResourceManager : MySettings.LanguageName = "Russian"
        Language_Setup()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_language_chinese_Click(sender As Object, e As EventArgs) Handles mi_language_chinese.Click
        RM = My.Resources.chinese.ResourceManager : MySettings.LanguageName = "Chinese"
        Language_Setup()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_language_italian_Click(sender As Object, e As EventArgs) Handles mi_language_italian.Click
        RM = My.Resources.italian.ResourceManager : MySettings.LanguageName = "Italian"
        Language_Setup()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_langauge_german_Click(sender As Object, e As EventArgs) Handles mi_langauge_german.Click
        RM = My.Resources.german.ResourceManager : MySettings.LanguageName = "German"
        Language_Setup()
        DetectDeviceEvent()
    End Sub


#End Region

#Region "Status System"
    Private StatusMessageControls() As Control 'Holds the label that the form displays

    Public Sub SetStatusPageProgress(percent As Integer)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() SetStatusPageProgress(percent))
        Else
            If (percent > 100) Then percent = 100
            If (percent = 100) Then
                Me.statuspage_progress.Value = 0
                Me.statuspage_progress.Visible = False
            Else
                Me.statuspage_progress.Value = percent
                Me.statuspage_progress.Visible = True
            End If
        End If
    End Sub

    Private Sub UpdateStatusMessage(Label As String, Msg As String)
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub() UpdateStatusMessage(Label, Msg))
            Else
                For i = 0 To StatusMessageControls.Length - 1
                    Dim o As Object = DirectCast(StatusMessageControls(i), Label).Tag
                    If o IsNot Nothing AndAlso CStr(o).ToUpper = Label.ToUpper Then
                        DirectCast(StatusMessageControls(i), Label).Text = Label & ": " & Msg
                        Exit Sub
                    End If
                Next
                For i = 0 To StatusMessageControls.Length - 1
                    Dim o As Object = DirectCast(StatusMessageControls(i), Label).Tag
                    If o Is Nothing OrElse CStr(o) = "" Then
                        DirectCast(StatusMessageControls(i), Label).Tag = Label
                        DirectCast(StatusMessageControls(i), Label).Text = Label & ": " & Msg
                        Exit Sub
                    End If
                Next
                Me.Refresh()
                Application.DoEvents()
            End If
        Catch ex As Exception
        End Try
    End Sub

    Public Sub RemoveStatusMessage(Label As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() RemoveStatusMessage(Label))
        Else
            Dim LabelCollector As New ArrayList
            For i = 0 To StatusMessageControls.Length - 1
                Dim o As Object = DirectCast(StatusMessageControls(i), Label).Tag
                If o IsNot Nothing AndAlso Not CStr(o).ToUpper = Label.ToUpper Then
                    Dim n As New Label With {.Tag = StatusMessageControls(i).Tag, .Text = StatusMessageControls(i).Text}
                    LabelCollector.Add(n)
                End If
            Next
            ClearStatusMessage()
            For i = 0 To LabelCollector.Count - 1
                DirectCast(StatusMessageControls(i), Label).Tag = DirectCast(LabelCollector(i), Label).Tag
                DirectCast(StatusMessageControls(i), Label).Text = DirectCast(LabelCollector(i), Label).Text
            Next
        End If
    End Sub

    Public Sub RemoveStatusMessageStartsWith(Label As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() RemoveStatusMessageStartsWith(Label))
        Else
            Dim LabelCollector As New ArrayList
            For i = 0 To StatusMessageControls.Length - 1
                Dim o As Object = DirectCast(StatusMessageControls(i), Label).Tag
                If o IsNot Nothing AndAlso Not CStr(o).ToUpper.StartsWith(Label.ToUpper) Then
                    Dim n As New Label With {.Tag = StatusMessageControls(i).Tag, .Text = StatusMessageControls(i).Text}
                    LabelCollector.Add(n)
                End If
            Next
            ClearStatusMessage()
            For i = 0 To LabelCollector.Count - 1
                DirectCast(StatusMessageControls(i), Label).Tag = DirectCast(LabelCollector(i), Label).Tag
                DirectCast(StatusMessageControls(i), Label).Text = DirectCast(LabelCollector(i), Label).Text
                Application.DoEvents()
            Next
        End If
    End Sub

    'Removes all of the text of the status messages
    Public Sub ClearStatusMessage()
        If Me.InvokeRequired Then
            Me.Invoke(Sub() ClearStatusMessage())
        Else
            For i = 0 To StatusMessageControls.Length - 1
                DirectCast(StatusMessageControls(i), Label).Text = ""
                DirectCast(StatusMessageControls(i), Label).Tag = Nothing
            Next
        End If
    End Sub

    Public Sub InitStatusMessage()
        ReDim StatusMessageControls(6)
        StatusMessageControls(0) = sm1
        StatusMessageControls(1) = sm2
        StatusMessageControls(2) = sm3
        StatusMessageControls(3) = sm4
        StatusMessageControls(4) = sm5
        StatusMessageControls(5) = sm6
        StatusMessageControls(6) = sm7
    End Sub
    'This refreshes all of the memory devices currently connected
    Private Sub StatusMessages_LoadMemoryDevices()
        RemoveStatusMessageStartsWith(RM.GetString("gui_memory_device"))
        Dim device_tuples() = MyTabs_GetDeviceTuples()
        If device_tuples Is Nothing OrElse device_tuples.Length = 0 Then Exit Sub
        MyTabs.Refresh()
        Dim counter As Integer = 1
        For Each mem_tuple In device_tuples
            Dim flash_desc As String = mem_tuple.mem_dev.Name & " (" & Format(mem_tuple.mem_dev.Size, "#,###") & " bytes)"
            UpdateStatusMessage(RM.GetString("gui_memory_device") & " " & counter, flash_desc)
        Next
    End Sub
#End Region

#Region "Console Tab"
    Private CommandThread As Threading.Thread
    Private ScriptCommand As String

    Public Sub PrintConsole(Msg As String)
        Try
            If AppIsClosing Then Exit Sub
            If Me.InvokeRequired Then
                Me.Invoke(Sub() PrintConsole(Msg))
            Else
                ConsoleBox.BeginUpdate()
                ConsoleBox.Items.Add(Msg)
                If ConsoleBox.Items.Count > 750 Then
                    Dim i As Integer
                    For i = 0 To 249
                        ConsoleBox.Items.RemoveAt(0)
                    Next
                End If
                ConsoleBox.SelectedIndex = ConsoleBox.Items.Count - 1
                ConsoleBox.EndUpdate()
                Application.DoEvents()
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub cmdSaveLog_Click(sender As Object, e As EventArgs) Handles cmdSaveLog.Click
        If ConsoleBox.Items.Count = 0 Then Exit Sub
        Dim fDiag As New SaveFileDialog
        fDiag.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        fDiag.Title = RM.GetString("gui_save_dialog")
        fDiag.FileName = "FCUSB.console.log.txt"
        If fDiag.ShowDialog = DialogResult.OK Then
            Dim logfile(ConsoleBox.Items.Count - 1) As String
            Dim i As Integer
            For i = 0 To logfile.Length - 1
                logfile(i) = ConsoleBox.Items.Item(i).ToString
            Next
            Try
                Utilities.FileIO.WriteFile(logfile, fDiag.FileName)
            Catch ex As Exception
            End Try
        End If
    End Sub

    Private Sub cmd_console_copy_Click(sender As Object, e As EventArgs) Handles cmd_console_copy.Click
        Try
            Dim clip_txt As String = ""
            For i = 0 To ConsoleBox.Items.Count - 1
                clip_txt &= ConsoleBox.Items.Item(i).ToString
                If i <> ConsoleBox.Items.Count - 1 Then
                    clip_txt &= vbCrLf
                End If
            Next
            My.Computer.Clipboard.SetText(clip_txt)
            SetStatus(RM.GetString("gui_console_text_copied"))
        Catch ex As Exception
        End Try
    End Sub

    Private Sub txtInput_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtInput.KeyPress
        If (Asc(e.KeyChar) = 13) Then 'Enter key was pressed
            ScriptCommand = txtInput.Text
            txtInput.Text = ""
            Application.DoEvents()
            CommandThread = New Threading.Thread(AddressOf CmdThreadExec)
            CommandThread.IsBackground = True
            CommandThread.Name = "ScriptExecThread"
            CommandThread.SetApartmentState(Threading.ApartmentState.STA)
            CommandThread.Start()
        End If
    End Sub
    'This is so that the console command does not tie up the Form or input boxes etc
    Private Sub CmdThreadExec()
        Try
            ScriptProcessor.ExecuteCommand(ScriptCommand)
        Catch ex As Exception
        End Try
    End Sub

#End Region

#Region "Repeat Feature"
    Private MyLastOperation As MemControl_v2.XFER_Operation
    Private MyLastMemoryDevice As MemoryDeviceInstance = Nothing

    Public Sub SuccessfulWriteOperation(mem_dev As MemoryDeviceInstance, wr_oper As MemControl_v2.XFER_Operation)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() SuccessfulWriteOperation(mem_dev, wr_oper))
        Else
            MyLastMemoryDevice = mem_dev
            MyLastOperation = wr_oper
            mi_repeat.Enabled = True
        End If
    End Sub

    Private Sub miRepeatWrite_Click(sender As Object, e As EventArgs) Handles mi_repeat.Click
        Try
            mi_repeat.Enabled = False
            MainApp.PrintConsole(RM.GetString("gui_repeat_beginning"))
            MAIN_FCUSB.Disconnect()
            Utilities.Sleep(200)
            Dim counter As Integer = 0
            Do While (Not MAIN_FCUSB.IS_CONNECTED)
                If counter = 100 Then '10 seconds
                    MainApp.PrintConsole(RM.GetString("gui_repeat_failed_reconnect"))
                    Exit Sub
                End If
                Application.DoEvents()
                Utilities.Sleep(100)
                counter += 1
            Loop
            Utilities.Sleep(1000)
            counter = 0
            Do While MEM_IF.DeviceCount = 0
                If counter = 50 Then '10 seconds
                    MainApp.PrintConsole(RM.GetString("gui_repeat_failed_detect"))
                    Exit Sub
                End If
                Application.DoEvents()
                Utilities.Sleep(100)
                counter += 1
            Loop
            Dim selected_mem = MyTabs_GetDeviceTab(MyLastMemoryDevice)
            If selected_mem.mem_dev Is Nothing Then Exit Sub
            selected_mem.mem_control.PerformWriteOperation(MyLastOperation)
        Catch ex As Exception
        Finally
            mi_repeat.Enabled = True
        End Try
    End Sub

#End Region

#Region "Tab System"
    Private UserTabCount As Integer = 0

    Public Function GetUserTabCount() As Integer
        Return UserTabCount
    End Function
    'Removes all device tabs and user tabs
    Public Sub MyTabs_RemoveAll()
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub() MyTabs_RemoveAll())
            Else
                If AppIsClosing Then Exit Sub
                Dim list As New List(Of TabPage)
                For Each page As TabPage In MyTabs.Controls
                    If page Is TabStatus Then
                    ElseIf page Is TabConsole Then
                    Else
                        list.Add(page)
                    End If
                Next
                For i = 0 To list.Count - 1
                    MyTabs.Controls.Remove(CType(list(i), Control))
                Next
                UserTabCount = 0
            End If
        Catch ex As Exception
        End Try
    End Sub

    Public Sub MyTabs_RemoveDeviceTab(WithThisInstance As MemoryDeviceInstance)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() MyTabs_RemoveDeviceTab(WithThisInstance))
        Else
            Dim PagesToRemove As New List(Of TabPage)
            For Each page As TabPage In MyTabs.Controls
                If page Is TabStatus Then
                ElseIf page Is TabConsole Then
                ElseIf TabContainsMemDevice(page) Then
                    Dim selected_mem = DirectCast(page.Tag, (mem_dev As MemoryDeviceInstance, mem_control As MemControl_v2))
                    If selected_mem.mem_dev Is WithThisInstance Then
                        PagesToRemove.Add(page)
                    End If
                End If
            Next
            For i = 0 To PagesToRemove.Count - 1
                MyTabs.Controls.Remove(CType(PagesToRemove(i), Control))
            Next
        End If
    End Sub

    Private Function MyTabs_GetDeviceTab(WithThisInstance As MemoryDeviceInstance) As (mem_dev As MemoryDeviceInstance, mem_control As MemControl_v2)
        If Me.InvokeRequired Then
            Return CType(Me.Invoke(Function() MyTabs_GetDeviceTab(WithThisInstance)), (mem_dev As MemoryDeviceInstance, mem_control As MemControl_v2))
        Else
            For Each page As TabPage In MyTabs.Controls
                If page Is TabStatus Then
                ElseIf page Is TabConsole Then
                ElseIf TabContainsMemDevice(page) Then
                    Dim selected_mem = DirectCast(page.Tag, (mem_dev As MemoryDeviceInstance, mem_control As MemControl_v2))
                    If selected_mem.mem_dev Is WithThisInstance Then Return selected_mem
                End If
            Next
            Return Nothing
        End If
    End Function

    Private Function MyTabs_GetDeviceTuples() As (mem_dev As MemoryDeviceInstance, mem_control As MemControl_v2)()
        Try
            Dim list_out As New List(Of (mem_dev As MemoryDeviceInstance, mem_control As MemControl_v2))
            For Each page As TabPage In MyTabs.Controls
                If page Is TabStatus Then
                ElseIf page Is TabConsole Then
                ElseIf TabContainsMemDevice(page) Then
                    list_out.Add(DirectCast(page.Tag, (MemoryDeviceInstance, MemControl_v2)))
                End If
            Next
            Return list_out.ToArray()
        Catch ex As Exception
        End Try
        Return Nothing
    End Function





    Private Function MyTabs_GetSelectedTuple() As (mem_dev As MemoryDeviceInstance, mem_control As MemControl_v2)
        If Me.InvokeRequired Then
            Dim result = Me.Invoke(Function() MyTabs_GetSelectedTuple())
            Return DirectCast(result, (mem_dev As MemoryDeviceInstance, mem_control As MemControl_v2))
        Else
            Dim o As Object = MyTabs.SelectedTab.Tag
            If o Is Nothing Then Return Nothing
            Return DirectCast(o, (MemoryDeviceInstance, MemControl_v2))
        End If
    End Function




    Public Sub AddControlToTable(tab_index As Integer, obj As Control)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() AddControlToTable(tab_index, obj))
        Else
            Dim usertab As TabPage = MyTabs_GetUserTab(tab_index)
            If usertab Is Nothing Then Exit Sub
            Dim c As Control = CType(obj, Control)
            usertab.Controls.Add(c)
            c.BringToFront()
        End If
    End Sub

    Public Sub MyTabs_CreateUserTab(TabName As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() MyTabs_CreateUserTab(TabName))
        Else
            Dim newTab As New TabPage(TabName)
            newTab.Name = "IND:" & CStr(UserTabCount)
            Me.MyTabs.Controls.Add(newTab)
            UserTabCount += 1
        End If
    End Sub

    Public Function MyTabs_GetUserTab(page_index As Integer) As TabPage
        Dim MyObj As String = "IND:" & page_index.ToString()
        For Each page As TabPage In MyTabs.Controls
            If page.Name.Equals(MyObj) Then Return page
        Next
        Return Nothing
    End Function
    'Removes tabs created by the script
    Public Sub MyTabs_RemoveUserTabs()
        If Me.MyTabs.InvokeRequired Then
            Me.Invoke(Sub() MyTabs_RemoveUserTabs())
        Else
            Dim list As New List(Of TabPage)
            For Each gui_tab_page As TabPage In MyTabs.Controls
                If gui_tab_page Is TabStatus Then
                ElseIf gui_tab_page Is TabConsole Then
                ElseIf gui_tab_page.Name.StartsWith("IND:") Then
                    list.Add(gui_tab_page)
                End If
            Next
            For i = 0 To list.Count - 1
                MyTabs.Controls.Remove(CType(list(i), Control))
            Next
            UserTabCount = 0
        End If
    End Sub

    Private Sub MyTabs_DrawItem(sender As Object, e As DrawItemEventArgs) Handles MyTabs.DrawItem
        Dim SelectedTab As TabPage = MyTabs.TabPages(e.Index) 'Select the active tab
        Dim HeaderRect As Rectangle = MyTabs.GetTabRect(e.Index) 'Get the area of the header of this TabPage
        Dim TextBrush As New SolidBrush(Color.Black) 'Create a Brush to paint the Text
        'Set the Alignment of the Text
        Dim sf As New StringFormat(StringFormatFlags.NoWrap)
        sf.Alignment = StringAlignment.Center
        sf.LineAlignment = StringAlignment.Center
        'Paint the Text using the appropriate Bold setting 
        If Convert.ToBoolean(e.State And DrawItemState.Selected) Then
            Dim BoldFont As New Font(MyTabs.Font.Name, MyTabs.Font.Size, FontStyle.Bold)
            e.Graphics.DrawString(SelectedTab.Text.Trim, BoldFont, TextBrush, HeaderRect, sf)
            Dim LineY As Integer = HeaderRect.Y + HeaderRect.Height 'This draws the line between the tab and the tab form
            e.Graphics.DrawLine(New Pen(Control.DefaultBackColor), HeaderRect.X, LineY, HeaderRect.X + HeaderRect.Width, LineY)
        Else
            e.Graphics.DrawString(SelectedTab.Text.Trim, e.Font, TextBrush, HeaderRect, sf)
        End If
        TextBrush.Dispose() 'Dispose of the Brush
    End Sub

    Public Sub MyTabs_RefreshAll()
        If Me.InvokeRequired Then
            Me.Invoke(Sub() MyTabs_RefreshAll())
        Else
            Dim device_tuples() = MyTabs_GetDeviceTuples()
            If device_tuples Is Nothing OrElse device_tuples.Length = 0 Then Exit Sub
            For Each device_tuple In device_tuples
                device_tuple.mem_control.RefreshView()
            Next
        End If
    End Sub

    Public Sub MyTabs_DisabledControls(show_cancel_button As Boolean)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() MyTabs_DisabledControls(show_cancel_button))
        Else
            Dim device_tuples() = MyTabs_GetDeviceTuples()
            If device_tuples Is Nothing OrElse device_tuples.Length = 0 Then Exit Sub
            For Each device_tuple In device_tuples
                device_tuple.mem_control.DisableControls(show_cancel_button)
            Next
        End If
    End Sub

    Public Sub MyTabs_EnableControls()
        If Me.InvokeRequired Then
            Me.Invoke(Sub() MyTabs_EnableControls())
        Else
            Dim device_tuples() = MyTabs_GetDeviceTuples()
            If device_tuples Is Nothing OrElse device_tuples.Length = 0 Then Exit Sub
            For Each device_tuple In device_tuples
                device_tuple.mem_control.EnableControls()
            Next
        End If
    End Sub

    Public Sub MyTabs_SetUserTabControlTxt(usertabind As Integer, UserControl As String, NewText As String)
        If Me.MyTabs.InvokeRequired Then
            Me.Invoke(Sub() MyTabs_SetUserTabControlTxt(usertabind, UserControl, NewText))
        Else
            Dim usertab As TabPage = MyTabs_GetUserTab(usertabind)
            If usertab Is Nothing Then Exit Sub
            For Each user_control As Control In usertab.Controls
                If user_control.Name.ToString.ToUpper.Equals(UserControl.ToUpper) Then
                    user_control.Text = NewText
                    If user_control.GetType Is GetType(TextBox) Then
                        Dim t As TextBox = CType(user_control, TextBox)
                        t.SelectionStart = 0
                    End If
                    Exit Sub
                End If
            Next
        End If
    End Sub

    Public Function MyTabs_GetUserTabControlText(usertabind As Integer, UserControl As String) As String
        If Me.MyTabs.InvokeRequired Then
            Return CStr(Me.Invoke(Function() MyTabs_GetUserTabControlText(usertabind, UserControl)))
        Else
            Dim usertab As TabPage = MyTabs_GetUserTab(usertabind)
            If usertab Is Nothing Then Return ""
            For Each user_control As Control In usertab.Controls
                If Not user_control.Name.ToUpper.Equals(UserControl.ToUpper) Then
                    Return user_control.Text
                End If
            Next
            Return ""
        End If
    End Function

    Public Function MyTabs_GetUserTabControl(ControlName As String, TabIndex As Integer) As String
        Dim MyObj As String = "IND:" & CStr(TabIndex)
        For Each page As TabPage In MyTabs.Controls
            If page.Name.Equals(MyObj) Then
                For Each Ct As Control In page.Controls
                    If Ct.Name.ToUpper.Equals(ControlName.ToUpper) Then
                        Return Ct.Text
                    End If
                Next
                Exit For
            End If
        Next
        Return ""
    End Function

    Public Sub HandleButtons(usertabind As Integer, Enabled As Boolean, BtnName As String)
        Dim usertab As TabPage = MyTabs_GetUserTab(usertabind)
        If usertab Is Nothing Then Exit Sub
        For Each user_control As Control In usertab.Controls
            If user_control.GetType Is GetType(Button) Then
                If (user_control.Name.ToString.ToUpper() = BtnName.ToUpper()) Or BtnName.Equals("") Then
                    If Enabled Then
                        EnableButton(CType(user_control, Button))
                    Else
                        DisableButton(CType(user_control, Button))
                    End If
                End If
            End If
        Next
    End Sub

    Public Sub DisableButton(b As Button)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() DisableButton(b))
        Else
            b.Enabled = False
        End If
    End Sub

    Public Sub EnableButton(b As Button)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() EnableButton(b))
        Else
            b.Enabled = True
        End If
    End Sub

    Private Function TabContainsMemDevice(page As TabPage) As Boolean
        Try
            If page.Tag Is Nothing Then Return False
            Dim selected_mem = DirectCast(page.Tag, (mem_dev As MemoryDeviceInstance, mem_control As MemControl_v2))
            If selected_mem.mem_dev Is Nothing Then Return False
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

#End Region

#Region "Menu DropDownOpening"

    Private Sub mi_main_menu_DropDownOpening(sender As Object, e As EventArgs) Handles mi_main_menu.DropDownOpening
        If IsAnyDeviceBusy() Then
            mi_usb_performance.Enabled = False
            mi_detect.Enabled = False
            mi_repeat.Enabled = False
            mi_refresh.Enabled = False
            Exit Sub
        End If
        If MAIN_FCUSB IsNot Nothing AndAlso MAIN_FCUSB.IS_CONNECTED Then
            mi_detect.Enabled = True
        Else
            mi_detect.Enabled = False
        End If
        If MyLastOperation IsNot Nothing Then
            mi_repeat.Enabled = True
        Else
            mi_repeat.Enabled = False
        End If
        If (MEM_IF.DeviceCount > 0) Then
            mi_refresh.Enabled = True
        Else
            mi_refresh.Enabled = False
        End If
        If MAIN_FCUSB IsNot Nothing Then
            If MAIN_FCUSB.IS_CONNECTED AndAlso MAIN_FCUSB.HWBOARD = FCUSB_BOARD.Mach1 Then
                mi_usb_performance.Enabled = True
            ElseIf MAIN_FCUSB.IS_CONNECTED AndAlso MAIN_FCUSB.HWBOARD = FCUSB_BOARD.Professional_PCB5 Then
                mi_usb_performance.Enabled = True
            Else
                mi_usb_performance.Enabled = False
            End If
        Else
            mi_usb_performance.Enabled = False
        End If
    End Sub

    Private Sub mi_mode_uncheckall()
        mi_1V8.Checked = False
        mi_3V3.Checked = False
        mi_bitswap_none.Checked = False
        mi_bitswap_8bit.Checked = False
        mi_bitswap_16bit.Checked = False
        mi_bitswap_32bit.Checked = False
        mi_bitendian_big_32.Checked = False
        mi_bitendian_big_16.Checked = False
        mi_bitendian_little_16.Checked = False
        mi_bitendian_little_8.Checked = False
        'OP MODES
        mi_mode_spi.Checked = False
        mi_mode_sqi.Checked = False
        mi_mode_jtag.Checked = False
        mi_mode_i2c.Checked = False
        mi_mode_spieeprom.Checked = False
        mi_mode_pnor.Checked = False
        mi_mode_pnand.Checked = False
        mi_mode_fwh.Checked = False
        mi_mode_peeprom.Checked = False
        mi_mode_1wire.Checked = False
        mi_mode_spi_nand.Checked = False
        mi_mode_eprom_otp.Checked = False
        mi_mode_hyperflash.Checked = False
        mi_mode_3wire.Checked = False
    End Sub

    Private Sub mi_mode_menu_settings(enabled As Boolean)
        mi_mode_settings.Enabled = enabled
        mi_verify.Enabled = enabled
        mi_bit_swapping.Enabled = enabled
        mi_bitswap_none.Enabled = enabled
        mi_bitswap_8bit.Enabled = enabled
        mi_bitswap_16bit.Enabled = enabled
        mi_bitswap_32bit.Enabled = enabled
        mi_endian.Enabled = enabled
        mi_bitendian_big_32.Enabled = enabled
        mi_bitendian_big_16.Enabled = enabled
        mi_bitendian_little_16.Enabled = enabled
        mi_bitendian_little_8.Enabled = enabled
    End Sub

    Private Sub mi_mode_enable(enabled As Boolean)
        mi_1V8.Enabled = enabled
        mi_3V3.Enabled = enabled
        mi_mode_spi.Enabled = enabled
        mi_mode_sqi.Enabled = enabled
        mi_mode_spi_nand.Enabled = enabled
        mi_mode_spieeprom.Enabled = enabled
        mi_mode_i2c.Enabled = enabled
        mi_mode_1wire.Enabled = enabled
        mi_mode_3wire.Enabled = enabled
        mi_mode_pnor.Enabled = enabled
        mi_mode_pnand.Enabled = enabled
        mi_mode_fwh.Enabled = enabled
        mi_mode_peeprom.Enabled = enabled
        mi_mode_eprom_otp.Enabled = enabled
        mi_mode_hyperflash.Enabled = enabled
        mi_mode_mmc.Enabled = enabled
        mi_mode_jtag.Enabled = enabled
    End Sub

    Private Sub mi_mode_enable_supported_modes()
        If MAIN_FCUSB Is Nothing OrElse (Not MAIN_FCUSB.IS_CONNECTED) Then
            mi_1V8.Enabled = True
            mi_3V3.Enabled = True
            mi_mode_spi.Enabled = True
            mi_mode_sqi.Enabled = True
            mi_mode_spieeprom.Enabled = True
            mi_mode_spi_nand.Enabled = True
            mi_mode_i2c.Enabled = True
            mi_mode_1wire.Enabled = True
            mi_mode_3wire.Enabled = True
            mi_mode_eprom_otp.Enabled = True
            mi_mode_pnor.Enabled = True
            mi_mode_pnand.Enabled = True
            mi_mode_peeprom.Enabled = True
            mi_mode_fwh.Enabled = True
            mi_mode_jtag.Enabled = True
            mi_mode_hyperflash.Enabled = True
            mi_mode_mmc.Enabled = True
        Else
            Dim SupportedModes() As DeviceMode = GetSupportedModes(MAIN_FCUSB)
            If (MAIN_FCUSB.HWBOARD = FCUSB_BOARD.Professional_PCB5) Then
                mi_1V8.Enabled = True
                mi_3V3.Enabled = True
            ElseIf (MAIN_FCUSB.HWBOARD = FCUSB_BOARD.Mach1) Then
                mi_1V8.Enabled = True
                mi_3V3.Enabled = True
            End If
            For Each mode In SupportedModes
                If mode = DeviceMode.SPI Then mi_mode_spi.Enabled = True
                If mode = DeviceMode.SPI_NAND Then mi_mode_spi_nand.Enabled = True
                If mode = DeviceMode.SQI Then mi_mode_sqi.Enabled = True
                If mode = DeviceMode.JTAG Then mi_mode_jtag.Enabled = True
                If mode = DeviceMode.I2C_EEPROM Then mi_mode_i2c.Enabled = True
                If mode = DeviceMode.SPI_EEPROM Then mi_mode_spieeprom.Enabled = True
                If mode = DeviceMode.PNOR Then mi_mode_pnor.Enabled = True
                If mode = DeviceMode.PNAND Then mi_mode_pnand.Enabled = True
                If mode = DeviceMode.P_EEPROM Then mi_mode_peeprom.Enabled = True
                If mode = DeviceMode.FWH Then mi_mode_fwh.Enabled = True
                If mode = DeviceMode.ONE_WIRE Then mi_mode_1wire.Enabled = True
                If mode = DeviceMode.EPROM Then mi_mode_eprom_otp.Enabled = True
                If mode = DeviceMode.HyperFlash Then mi_mode_hyperflash.Enabled = True
                If mode = DeviceMode.Microwire Then mi_mode_3wire.Enabled = True
                If mode = DeviceMode.EMMC Then mi_mode_mmc.Enabled = True
            Next
        End If
    End Sub

    Private Sub mi_mode_DropDownOpening(sender As Object, e As EventArgs) Handles mi_mode_menu.DropDownOpening
        mi_mode_uncheckall()
        Select Case MySettings.VOLT_SELECT
            Case Voltage.V1_8
                mi_1V8.Checked = True
            Case Voltage.V3_3
                mi_3V3.Checked = True
        End Select
        Select Case MySettings.BIT_SWAP
            Case BitSwapMode.None
                mi_bitswap_none.Checked = True
            Case BitSwapMode.Bits_8
                mi_bitswap_8bit.Checked = True
            Case BitSwapMode.Bits_16
                mi_bitswap_16bit.Checked = True
            Case BitSwapMode.Bits_32
                mi_bitswap_32bit.Checked = True
        End Select
        Select Case MySettings.BIT_ENDIAN
            Case BitEndianMode.BigEndian32
                mi_bitendian_big_32.Checked = True
            Case BitEndianMode.BigEndian16
                mi_bitendian_big_16.Checked = True
            Case BitEndianMode.LittleEndian32_16bit
                mi_bitendian_little_16.Checked = True
            Case BitEndianMode.LittleEndian32_8bit
                mi_bitendian_little_8.Checked = True
        End Select
        Select Case MySettings.OPERATION_MODE
            Case DeviceMode.SPI
                mi_mode_spi.Checked = True
            Case DeviceMode.SQI
                mi_mode_sqi.Checked = True
            Case DeviceMode.JTAG
                mi_mode_jtag.Checked = True
            Case DeviceMode.I2C_EEPROM
                mi_mode_i2c.Checked = True
            Case DeviceMode.SPI_EEPROM
                mi_mode_spieeprom.Checked = True
            Case DeviceMode.PNOR
                mi_mode_pnor.Checked = True
            Case DeviceMode.PNAND
                mi_mode_pnand.Checked = True
            Case DeviceMode.FWH
                mi_mode_fwh.Checked = True
            Case DeviceMode.ONE_WIRE
                mi_mode_1wire.Checked = True
            Case DeviceMode.SPI_NAND
                mi_mode_spi_nand.Checked = True
            Case DeviceMode.P_EEPROM
                mi_mode_peeprom.Checked = True
            Case DeviceMode.EPROM
                mi_mode_eprom_otp.Checked = True
            Case DeviceMode.HyperFlash
                mi_mode_hyperflash.Checked = True
            Case DeviceMode.EMMC
                mi_mode_mmc.Checked = True
            Case DeviceMode.Microwire
                mi_mode_3wire.Checked = True
            Case Else
                mi_mode_spi.Checked = True
        End Select
        mi_mode_menu_settings(False)
        mi_mode_enable(False) 'Disables all
        If Not IsAnyDeviceBusy() Then
            mi_mode_menu_settings(True)
            mi_mode_enable_supported_modes() 'Enables modes for selected devices
        End If
    End Sub

    Private Sub mi_script_menu_DropDownOpening(sender As Object, e As EventArgs) Handles mi_script_menu.DropDownOpening
        Try
            If IsAnyDeviceBusy() Then
                For Each item In DirectCast(sender, ToolStripMenuItem).DropDownItems
                    If item.GetType Is GetType(ToolStripMenuItem) Then
                        DirectCast(item, ToolStripMenuItem).Enabled = False
                    End If
                Next
            Else
                For Each item In DirectCast(sender, ToolStripMenuItem).DropDownItems
                    If item.GetType Is GetType(ToolStripMenuItem) Then
                        DirectCast(item, ToolStripMenuItem).Enabled = True
                    End If
                Next
                If mi_script_selected.DropDownItems.Count = 0 Then
                    mi_script_selected.Enabled = False
                Else
                    mi_script_selected.Enabled = True
                End If
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub mi_Language_DropDownOpening(sender As Object, e As EventArgs) Handles mi_Language.DropDownOpening
        Try
            If IsAnyDeviceBusy() Then
                For Each item In DirectCast(sender, ToolStripMenuItem).DropDownItems
                    If item.GetType Is GetType(ToolStripMenuItem) Then
                        DirectCast(item, ToolStripMenuItem).Enabled = False
                    End If
                Next
            Else
                For Each item In DirectCast(sender, ToolStripMenuItem).DropDownItems
                    If item.GetType Is GetType(ToolStripMenuItem) Then
                        DirectCast(item, ToolStripMenuItem).Enabled = True
                    End If
                Next
            End If
            mi_language_english.Image = My.Resources.Resources.English
            mi_language_spanish.Image = My.Resources.Resources.spain
            mi_language_french.Image = My.Resources.Resources.france
            mi_language_portuguese.Image = My.Resources.Resources.portugal
            mi_language_russian.Image = My.Resources.Resources.russia
            mi_language_chinese.Image = My.Resources.Resources.china
            mi_language_italian.Image = My.Resources.Resources.Italy
            mi_langauge_german.Image = My.Resources.Resources.german
            Select Case MySettings.LanguageName.ToUpper
                Case "English".ToUpper
                    mi_language_english.Image = My.Resources.Resources.English_sel
                Case "Spanish".ToUpper
                    mi_language_spanish.Image = My.Resources.Resources.spain_sel
                Case "French".ToUpper
                    mi_language_french.Image = My.Resources.Resources.france_sel
                Case "Portuguese".ToUpper
                    mi_language_portuguese.Image = My.Resources.Resources.portugal_sel
                Case "Russian".ToUpper
                    mi_language_russian.Image = My.Resources.Resources.russia_sel
                Case "Chinese".ToUpper
                    mi_language_chinese.Image = My.Resources.Resources.china_sel
                Case "Italian".ToUpper
                    mi_language_italian.Image = My.Resources.Resources.Italy_sel
                Case "German".ToUpper
                    mi_langauge_german.Image = My.Resources.Resources.german_sel
            End Select
        Catch ex As Exception
        End Try
    End Sub

    Private Sub mi_tools_menu_DropDownOpening(sender As Object, e As EventArgs) Handles mi_tools_menu.DropDownOpening
        If TabContainsMemDevice(MyTabs.SelectedTab) Then 'We are on a memory device
            Dim selected_mem = DirectCast(MyTabs.SelectedTab.Tag, (mem_dev As MemoryDeviceInstance, mem_control As MemControl_v2))
            Dim mem_dev = selected_mem.mem_dev
            If (Not mem_dev.IsBusy) And (Not mem_dev.IsTaskRunning) Then
                mi_erase_tool.Enabled = (Not mem_dev.ReadOnly)
                mi_create_img.Enabled = True
                mi_write_img.Enabled = True
                mi_nand_map.Enabled = False
                mi_cfi_info.Enabled = False
                mi_blank_check.Enabled = False
                If mem_dev.VendorMenu Is Nothing Then
                    mi_device_features.Enabled = False
                Else
                    mi_device_features.Enabled = True
                End If
                If mem_dev.MEM_IF.GetType() Is GetType(BSR_Programmer) Then
                    Dim PROG_IF As BSR_Programmer = DirectCast(mem_dev.MEM_IF, BSR_Programmer)
                    Dim PNOR_IF As P_NOR = PROG_IF.MyFlashDevice
                    mi_create_img.Enabled = False
                    mi_write_img.Enabled = False
                    If PROG_IF.CFI.IS_VALID Then mi_cfi_info.Enabled = True
                    Exit Sub 'Accept the above
                ElseIf mem_dev.MEM_IF.GetType() Is GetType(LINK_Programmer) Then
                    mi_create_img.Enabled = False
                    mi_write_img.Enabled = False
                    Exit Sub 'Accept the above
                ElseIf mem_dev.FlashType = MemoryType.PARALLEL_NAND Then
                    Dim PNAND_IF As PARALLEL_NAND = DirectCast(mem_dev.MEM_IF, PARALLEL_NAND)
                    mi_nand_map.Enabled = True
                    If PNAND_IF.ONFI.IS_VALID Then mi_cfi_info.Enabled = True
                    Exit Sub 'Accept the above
                ElseIf mem_dev.FlashType = MemoryType.SERIAL_NAND Then
                    mi_nand_map.Enabled = True
                    Exit Sub 'Accept the above
                ElseIf mem_dev.FlashType = MemoryType.SERIAL_NOR Then
                    Exit Sub 'Accept the above
                ElseIf mem_dev.FlashType = MemoryType.SERIAL_QUAD Then
                    Exit Sub 'Accept the above
                ElseIf mem_dev.FlashType = MemoryType.PARALLEL_NOR Then
                    Dim PROG_IF As PARALLEL_NOR = DirectCast(mem_dev.MEM_IF, PARALLEL_NOR)
                    Dim PNOR_IF As P_NOR = PROG_IF.MyFlashDevice
                    If PROG_IF.CFI.IS_VALID Then mi_cfi_info.Enabled = True
                    Exit Sub 'Accept the above
                ElseIf mem_dev.FlashType = MemoryType.SERIAL_SWI Then
                    mi_erase_tool.Enabled = False
                    mi_create_img.Enabled = False
                    mi_write_img.Enabled = False
                    mi_nand_map.Enabled = False
                    Exit Sub 'Accept the above
                ElseIf mem_dev.FlashType = MemoryType.HYPERFLASH Then
                    mi_erase_tool.Enabled = True
                    mi_create_img.Enabled = False
                    mi_write_img.Enabled = False
                    mi_nand_map.Enabled = False
                    mi_cfi_info.Enabled = False
                    Exit Sub 'Accept the above
                ElseIf mem_dev.FlashType = MemoryType.OTP_EPROM Then
                    mi_erase_tool.Enabled = False
                    mi_create_img.Enabled = False
                    mi_write_img.Enabled = False
                    mi_blank_check.Enabled = True
                    Exit Sub
                End If
            End If
        End If
        mi_erase_tool.Enabled = False
        mi_create_img.Enabled = False
        mi_write_img.Enabled = False
        mi_nand_map.Enabled = False
        mi_device_features.Enabled = False
        mi_cfi_info.Enabled = False
        mi_blank_check.Enabled = False
    End Sub

#End Region

#Region "Form Events"

    Public Sub CloseApplication()
        If Me.InvokeRequired Then
            Me.Invoke(Sub() CloseApplication())
        Else
            Me.Close()
        End If
    End Sub

    Private Sub EmbLogo_DoubleClick(sender As Object, e As EventArgs) Handles pb_logo.DoubleClick
        If Me.Cursor = Cursors.Hand Then
            Dim sInfo As New ProcessStartInfo("https://www.embeddedcomputers.net/")
            Process.Start(sInfo)
        End If
    End Sub

    Private Sub EmbLogo_MouseLeave(sender As Object, e As EventArgs) Handles pb_logo.MouseLeave
        Me.Cursor = Cursors.Arrow
    End Sub

    Private Sub EmbLogo_MouseMove(sender As Object, e As MouseEventArgs) Handles pb_logo.MouseMove
        Dim x As Integer = (Me.Width \ 2) - 130
        If e.X >= x AndAlso e.X <= x + 222 Then
            Me.Cursor = Cursors.Hand
        Else
            Me.Cursor = Cursors.Arrow
        End If
    End Sub

    Private Sub Main_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        MySettings.Save()
        AppClosing()
    End Sub

    Private Sub cmd_console_clear_Click(sender As Object, e As EventArgs) Handles cmd_console_clear.Click
        ConsoleBox.Items.Clear()
    End Sub

    Private Sub mi_nand_map_Click(sender As Object, e As EventArgs) Handles mi_nand_map.Click
        Try
            Dim selected_mem = MyTabs_GetSelectedTuple()
            If selected_mem.mem_dev Is Nothing Then Exit Sub
            If selected_mem.mem_dev.FlashType = MemoryType.SERIAL_NAND Then
                Dim SNAND_IF As SPINAND_Programmer = CType(selected_mem.mem_dev.MEM_IF, SPINAND_Programmer)
                Dim nand As SPI_NAND = SNAND_IF.MyFlashDevice
                Dim pages_per_block As UInt16 = CUShort(nand.Block_Size \ nand.PAGE_SIZE)
                Dim n_layout As NAND_LAYOUT_TOOL.NANDLAYOUT_STRUCTURE = NAND_LayoutTool.GetStructure(nand)
                Dim n As New NAND_Block_Management(SNAND_IF)
                n.SetDeviceParameters(nand.NAME, nand.PAGE_SIZE + nand.PAGE_EXT, pages_per_block, nand.BLOCK_COUNT, n_layout)
                n.ShowDialog()
            ElseIf selected_mem.mem_dev.FlashType = MemoryType.PARALLEL_NAND Then
                Dim PNAND_IF As PARALLEL_NAND = CType(selected_mem.mem_dev.MEM_IF, PARALLEL_NAND)
                Dim nand As P_NAND = PNAND_IF.MyFlashDevice
                Dim n_layout As NAND_LAYOUT_TOOL.NANDLAYOUT_STRUCTURE = NAND_LayoutTool.GetStructure(nand)
                Dim n As New NAND_Block_Management(PNAND_IF)
                n.SetDeviceParameters(nand.NAME, nand.PAGE_SIZE + nand.PAGE_EXT, nand.PAGE_COUNT, nand.BLOCK_COUNT, n_layout)
                n.ShowDialog()
            End If
            If selected_mem.mem_dev.FlashType = MemoryType.SERIAL_NAND Then
                Dim SNAND_IF As SPINAND_Programmer = CType(selected_mem.mem_dev.MEM_IF, SPINAND_Programmer)
                SNAND_IF.BlockManager.ProcessMap()
                selected_mem.mem_control.InitMemoryDevice(selected_mem.mem_dev)
                selected_mem.mem_control.SetupExtendedLayout()
                StatusMessages_LoadMemoryDevices()
            ElseIf selected_mem.mem_dev.FlashType = MemoryType.PARALLEL_NAND Then
                Dim PNAND_IF As PARALLEL_NAND = CType(selected_mem.mem_dev.MEM_IF, PARALLEL_NAND)
                PNAND_IF.BlockManager.ProcessMap()
                selected_mem.mem_control.InitMemoryDevice(selected_mem.mem_dev)
                selected_mem.mem_control.SetupExtendedLayout()
                StatusMessages_LoadMemoryDevices()
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub LoadSettingsIntoGui()
        'Maybe uncheck all items?
        mi_verify.Checked = MySettings.VERIFY_WRITE
        Select Case MySettings.BIT_SWAP
            Case BitSwapMode.None
                mi_bitswap_none.Checked = True
            Case BitSwapMode.Bits_8
                mi_bitswap_8bit.Checked = True
            Case BitSwapMode.Bits_16
                mi_bitswap_16bit.Checked = True
            Case BitSwapMode.Bits_32
                mi_bitswap_32bit.Checked = True
        End Select
        Select Case MySettings.BIT_ENDIAN
            Case BitEndianMode.BigEndian32
                mi_bitendian_big_32.Checked = True
            Case BitEndianMode.BigEndian16
                mi_bitendian_big_16.Checked = True
            Case BitEndianMode.LittleEndian32_16bit
                mi_bitendian_little_16.Checked = True
            Case BitEndianMode.LittleEndian32_8bit
                mi_bitendian_little_8.Checked = True
        End Select
        Select Case MySettings.VOLT_SELECT
            Case Voltage.V1_8
                mi_1V8.Checked = True
            Case Voltage.V3_3
                mi_3V3.Checked = True
        End Select
        Select Case MySettings.OPERATION_MODE
            Case DeviceMode.SPI
                mi_mode_spi.Checked = True
            Case DeviceMode.SQI
                mi_mode_sqi.Checked = True
            Case DeviceMode.SPI_NAND
                mi_mode_spi_nand.Checked = True
            Case DeviceMode.SPI_EEPROM
                mi_mode_spieeprom.Checked = True
            Case DeviceMode.I2C_EEPROM
                mi_mode_i2c.Checked = True
            Case DeviceMode.Microwire
                mi_mode_3wire.Checked = True
            Case DeviceMode.PNOR
                mi_mode_pnor.Checked = True
            Case DeviceMode.PNAND
                mi_mode_pnand.Checked = True
            Case DeviceMode.P_EEPROM
                mi_mode_peeprom.Checked = True
            Case DeviceMode.FWH
                mi_mode_fwh.Checked = True
            Case DeviceMode.EPROM
                mi_mode_eprom_otp.Checked = True
            Case DeviceMode.HyperFlash
                mi_mode_hyperflash.Checked = True
            Case DeviceMode.EMMC
                mi_mode_mmc.Checked = True
            Case DeviceMode.JTAG
                mi_mode_jtag.Checked = True
        End Select
    End Sub

    Private Sub mi_mode_settings_Click(sender As Object, e As EventArgs) Handles mi_mode_settings.Click
        Dim f As New FrmSettings()
        f.ECC_FEATURE_ENABLED = (LicenseStatus = LicenseStatusEnum.LicensedValid)
        f.ShowDialog()
        MySettings.Save() 'Saves all settings to registry
        If MySettings.OPERATION_MODE = DeviceMode.SPI_NAND Then
            For i = 0 To MEM_IF.DeviceCount - 1
                Dim mem_dev As MemoryDeviceInstance = MEM_IF.GetDevice(i)
                If mem_dev IsNot Nothing Then
                    If mem_dev.FlashType = MemoryType.SERIAL_NAND Then
                        Dim SNAND_IF As SPINAND_Programmer = CType(mem_dev.MEM_IF, SPINAND_Programmer)
                        Dim selected_mem = MyTabs_GetDeviceTab(mem_dev)
                        selected_mem.mem_control?.SetupExtendedLayout()
                        StatusMessages_LoadMemoryDevices()
                        If MySettings.SPI_NAND_DISABLE_ECC Then
                            SNAND_IF.ECC_ENABLED = False
                        Else
                            SNAND_IF.ECC_ENABLED = True
                        End If
                    End If
                End If
            Next
        ElseIf MySettings.OPERATION_MODE = DeviceMode.PNAND Then
            For i = 0 To MEM_IF.DeviceCount - 1
                Dim dv As MemoryDeviceInstance = MEM_IF.GetDevice(i)
                If dv IsNot Nothing AndAlso dv.FlashType = MemoryType.PARALLEL_NAND Then
                    Dim selected_mem = MyTabs_GetDeviceTab(dv)
                    selected_mem.mem_control?.SetupExtendedLayout()
                    StatusMessages_LoadMemoryDevices()
                End If
            Next
        End If
        LoadSettingsIntoGui()
    End Sub

    Private Sub Menu_Mode_UncheckAll()
        mi_mode_spi.Checked = False
        mi_mode_sqi.Checked = False
        mi_mode_spieeprom.Checked = False
        mi_mode_i2c.Checked = False
        mi_mode_pnor.Checked = False
        mi_mode_pnand.Checked = False
        mi_mode_1wire.Checked = False
        mi_mode_3wire.Checked = False
        mi_mode_eprom_otp.Checked = False
        mi_mode_peeprom.Checked = False
        mi_mode_jtag.Checked = False
        mi_mode_spi_nand.Checked = False
        mi_mode_hyperflash.Checked = False
        mi_mode_mmc.Checked = False
        mi_mode_fwh.Checked = False
    End Sub

    Private Sub mi_mode_spi_Click(sender As Object, e As EventArgs) Handles mi_mode_spi.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = DeviceMode.SPI
        MySettings.Save()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_spi_quad_Click(sender As Object, e As EventArgs) Handles mi_mode_sqi.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = DeviceMode.SQI
        MySettings.Save()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_mode_spi_eeprom_Click(sender As Object, e As EventArgs) Handles mi_mode_spieeprom.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = DeviceMode.SPI_EEPROM
        MySettings.Save()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_mode_i2c_Click(sender As Object, e As EventArgs) Handles mi_mode_i2c.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = DeviceMode.I2C_EEPROM
        MySettings.Save()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_mode_1wire_Click(sender As Object, e As EventArgs) Handles mi_mode_1wire.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = DeviceMode.ONE_WIRE
        MySettings.Save()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_mode_jtag_Click(sender As Object, e As EventArgs) Handles mi_mode_jtag.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = DeviceMode.JTAG
        MySettings.Save()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_mode_pnor_Click(sender As Object, e As EventArgs) Handles mi_mode_pnor.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = DeviceMode.PNOR
        MySettings.Save()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_mode_pnand_Click(sender As Object, e As EventArgs) Handles mi_mode_pnand.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = DeviceMode.PNAND
        MySettings.Save()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_mode_fwh_Click(sender As Object, e As EventArgs) Handles mi_mode_fwh.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = DeviceMode.FWH
        MySettings.Save()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_mode_peeprom_Click(sender As Object, e As EventArgs) Handles mi_mode_peeprom.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = DeviceMode.P_EEPROM
        MySettings.Save()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_mode_eprom_otp_Click(sender As Object, e As EventArgs) Handles mi_mode_eprom_otp.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = DeviceMode.EPROM
        MySettings.Save()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_mode_spi_nand_Click(sender As Object, e As EventArgs) Handles mi_mode_spi_nand.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = DeviceMode.SPI_NAND
        MySettings.Save()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_mode_3wire_Click(sender As Object, e As EventArgs) Handles mi_mode_3wire.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = DeviceMode.Microwire
        MySettings.Save()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_mode_hyperflash_Click(sender As Object, e As EventArgs) Handles mi_mode_hyperflash.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = DeviceMode.HyperFlash
        MySettings.Save()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_mode_mmc_Click(sender As Object, e As EventArgs) Handles mi_mode_mmc.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = DeviceMode.EMMC
        MySettings.Save()
        DetectDeviceEvent()
    End Sub

    Private Sub mi_verify_Click(sender As Object, e As EventArgs) Handles mi_verify.Click
        mi_verify.Checked = Not mi_verify.Checked
        MySettings.VERIFY_WRITE = mi_verify.Checked
        Dim device_tuples() = MyTabs_GetDeviceTuples()
        If device_tuples Is Nothing OrElse device_tuples.Length = 0 Then Exit Sub
        For Each device_tuple In device_tuples
            device_tuple.mem_control.VERIFY_WRITE = mi_verify.Checked
        Next
    End Sub

    Private Sub mi_bitswap_none_Click(sender As Object, e As EventArgs) Handles mi_bitswap_none.Click
        MySettings.BIT_SWAP = BitSwapMode.None
        MyTabs_RefreshAll()
    End Sub

    Private Sub mi_bitswap_8bit_Click(sender As Object, e As EventArgs) Handles mi_bitswap_8bit.Click
        MySettings.BIT_SWAP = BitSwapMode.Bits_8
        MyTabs_RefreshAll()
    End Sub

    Private Sub mi_bitswap_16bit_Click(sender As Object, e As EventArgs) Handles mi_bitswap_16bit.Click
        MySettings.BIT_SWAP = BitSwapMode.Bits_16
        MyTabs_RefreshAll()
    End Sub

    Private Sub mi_bitswap_32bit_Click(sender As Object, e As EventArgs) Handles mi_bitswap_32bit.Click
        MySettings.BIT_SWAP = BitSwapMode.Bits_32
        MyTabs_RefreshAll()
    End Sub

    Private Sub mi_endian_big_Click(sender As Object, e As EventArgs) Handles mi_bitendian_big_32.Click
        MySettings.BIT_ENDIAN = BitEndianMode.BigEndian32
        MyTabs_RefreshAll()
    End Sub

    Private Sub mi_bitendian_big_16_Click(sender As Object, e As EventArgs) Handles mi_bitendian_big_16.Click
        MySettings.BIT_ENDIAN = BitEndianMode.BigEndian16
        MyTabs_RefreshAll()
    End Sub

    Private Sub mi_endian_16bit_Click(sender As Object, e As EventArgs) Handles mi_bitendian_little_16.Click
        MySettings.BIT_ENDIAN = BitEndianMode.LittleEndian32_16bit
        MyTabs_RefreshAll()
    End Sub

    Private Sub mi_endian_8bit_Click(sender As Object, e As EventArgs) Handles mi_bitendian_little_8.Click
        MySettings.BIT_ENDIAN = BitEndianMode.LittleEndian32_8bit
        MyTabs_RefreshAll()
    End Sub

    Private Sub mi_1V8_Click(sender As Object, e As EventArgs) Handles mi_1V8.Click
        VCC_Enable1V8()
    End Sub

    Private Sub mi_3V3_Click(sender As Object, e As EventArgs) Handles mi_3V3.Click
        VCC_Enable3V3()
    End Sub

    Private Sub VCC_Enable1V8()
        Try
            mi_1V8.Checked = True
            mi_3V3.Checked = False
            MySettings.VOLT_SELECT = Voltage.V1_8
            MAIN_FCUSB.USB_VCC_ON(Voltage.V1_8)
            MySettings.Save()
            PrintConsole(String.Format(RM.GetString("voltage_set_to"), "1.8v"))
            Dim t As New Threading.Thread(AddressOf VCC_UpdateLogic)
            t.Name = "setLogicVoltTd"
            t.Start()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub VCC_Enable3V3()
        Try
            mi_1V8.Checked = False
            mi_3V3.Checked = True
            MySettings.VOLT_SELECT = Voltage.V3_3
            MAIN_FCUSB.USB_VCC_ON(Voltage.V3_3)
            MySettings.Save()
            PrintConsole(String.Format(RM.GetString("voltage_set_to"), "3.3v"))
            Dim t As New Threading.Thread(AddressOf VCC_UpdateLogic)
            t.Name = "setLogicVoltTd"
            t.Start()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub VCC_UpdateLogic()
        Logic.UpdateLogic(MySettings.OPERATION_MODE, MySettings.VOLT_SELECT)
    End Sub

    Private Sub mi_detect_Click(sender As Object, e As EventArgs) Handles mi_detect.Click
        DetectDeviceEvent()
    End Sub

    Private Sub DetectDeviceEvent()
        MyLastOperation = Nothing
        MyLastMemoryDevice = Nothing
        mi_jtag_menu.Visible = False
        ClearStatusMessage()
        UnloadActiveScript()
        RemoveStatusMessage(RM.GetString("gui_active_script"))
        MEM_IF.Clear() 'Removes all tabs
        MAIN_FCUSB?.Disconnect()
    End Sub

    Private Sub mi_exit_Click(sender As Object, e As EventArgs) Handles mi_exit.Click
        Me.Close()
    End Sub

    Private Sub mi_refresh_Click(sender As Object, e As EventArgs) Handles mi_refresh.Click
        MyTabs_RefreshAll()
    End Sub

    Private Sub mi_usb_performance_Click(sender As Object, e As EventArgs) Handles mi_usb_performance.Click
        Try
            Dim frm_usb As New FrmPerformance
            frm_usb.ShowDialog()
        Catch ex As Exception
        End Try
    End Sub

#End Region

#Region "Backup Tool"
    Private BACKUP_OPERATION_RUNNING As Boolean = False
    Private BACKUP_FILE As String = ""
    Private Delegate Sub OnButtonEnable()
    Private Delegate Function cbPromptUserForSaveLocation(name As String) As String

    Private Class NAND_CONFIG
        Public Property flash_name As String
        Public Property flash_size As Long
        Public Property page_size As UInt16
        Public Property oob_size As UInt16
        Public Property block_count As Integer
        Public Property block_size As Integer
        Public Property chipid As String
        Public Property page_count As Integer

        Sub New(file_data() As Byte)
            Dim config_file() As String = Utilities.Bytes.ToCharStringArray(file_data)
            Me.flash_name = GetNandCfgParam("MEMORY_DEVICE", config_file.ToArray)
            Me.flash_size = CLng(GetNandCfgParam("FLASH_SIZE", config_file.ToArray))
            Me.page_size = CUShort(GetNandCfgParam("PAGE_SIZE", config_file.ToArray))
            Me.oob_size = CUShort(GetNandCfgParam("OOB_SIZE", config_file.ToArray))
            Me.block_count = CInt(GetNandCfgParam("BLOCK_COUNT", config_file.ToArray))
            Me.block_size = CInt(GetNandCfgParam("BLOCK_SIZE", config_file.ToArray))
            Me.chipid = GetNandCfgParam("ID", config_file.ToArray)
            Me.page_count = CInt(GetNandCfgParam("PAGE_COUNT", config_file.ToArray))
        End Sub

        Sub New()

        End Sub

        Public Function Save() As Byte()
            Dim InfoFile As New List(Of String)
            InfoFile.Add("[MEMORY_DEVICE]" & flash_name)
            InfoFile.Add("[FLASH_SIZE]" & flash_size.ToString)
            InfoFile.Add("[PAGE_SIZE]" & page_size.ToString)
            InfoFile.Add("[OOB_SIZE]" & oob_size.ToString)
            InfoFile.Add("[BLOCK_COUNT]" & block_count.ToString)
            InfoFile.Add("[BLOCK_SIZE]" & block_size.ToString)
            InfoFile.Add("[ID]" & chipid)
            InfoFile.Add("[PAGE_COUNT]" & page_count.ToString)
            Dim s() As String = InfoFile.ToArray
            Return Utilities.Bytes.FromCharStringArray(s)
        End Function

        Private Function GetNandCfgParam(ParamName As String, File() As String) As String
            For Each line In File
                If line.StartsWith("[" & ParamName & "]") Then
                    Dim x As Integer = ParamName.Length + 2
                    Return line.Substring(x)
                End If
            Next
            Return ""
        End Function

    End Class

    Private Function ZIP_GetFile(archive As ZipArchive, filename As String) As Byte()
        Dim zipfile As ZipArchiveEntry = archive.GetEntry(filename)
        Using nand_main As New IO.BinaryReader(zipfile.Open)
            Return nand_main.ReadBytes(CInt(zipfile.Length))
        End Using
        Return Nothing
    End Function

    Private Function ZIP_SetFile(archive As ZipArchive, filename As String, filedata() As Byte) As Boolean
        If filedata Is Nothing Then Return False
        Dim zipfile As ZipArchiveEntry = archive.CreateEntry(filename)
        Using nand_main As New IO.BinaryWriter(zipfile.Open)
            nand_main.Write(filedata)
        End Using
        Return True
    End Function

    Private Sub CreateImage_Click(sender As Object, e As EventArgs) Handles mi_create_img.Click
        Dim t As New Threading.Thread(AddressOf CreateFlashImgThread)
        t.Name = "ImgCreatorTd"
        t.Start()
    End Sub

    Private Sub LoadImage_Click(sender As Object, e As EventArgs) Handles mi_write_img.Click
        Dim OpenMe As New OpenFileDialog
        OpenMe.AddExtension = True
        OpenMe.InitialDirectory = DefaultLocation
        OpenMe.Title = RM.GetString("gui_open_img")
        OpenMe.CheckPathExists = True
        Dim FcFname As String = RM.GetString("gui_compressed_img") & " (*.zip)|*.zip"
        Dim AllF As String = "All files (*.*)|*.*"
        OpenMe.Filter = FcFname & "|" & AllF
        If (OpenMe.ShowDialog = DialogResult.OK) Then
            BACKUP_FILE = OpenMe.FileName
            Dim t As New Threading.Thread(AddressOf LoadFlashImgThread)
            t.Name = "ImgLoaderTd"
            t.Start()
        End If
    End Sub

    Private Sub CreateFlashImgThread()
        Dim selected_mem = MyTabs_GetSelectedTuple()
        If selected_mem.mem_dev Is Nothing Then Exit Sub
        Try
            selected_mem.mem_dev.FCUSB.USB_LEDBlink()
            Backup_Start()
            If (MySettings.OPERATION_MODE = DeviceMode.PNAND) Then
                PrintNandFlashDetails(selected_mem.mem_dev)
                NANDBACKUP_SaveImage(selected_mem)
            ElseIf (MySettings.OPERATION_MODE = DeviceMode.SPI_NAND) Then
                PrintNandFlashDetails(selected_mem.mem_dev)
                NANDBACKUP_SaveImage(selected_mem)
            Else 'normal flash
                NORBACKUP_SaveImage(selected_mem)
            End If
        Catch ex As Exception
        Finally
            If selected_mem.mem_dev IsNot Nothing Then
                selected_mem.mem_dev.FCUSB.USB_LEDOn()
                selected_mem.mem_control.SetProgress(0)
            End If
            Backup_Stop()
        End Try
    End Sub

    Private Sub LoadFlashImgThread()
        Dim selected_mem = MyTabs_GetSelectedTuple()
        If selected_mem.mem_dev Is Nothing Then Exit Sub
        Try
            selected_mem.mem_dev.FCUSB.USB_LEDBlink()
            Backup_Start()
            If MySettings.OPERATION_MODE = DeviceMode.PNAND Then
                NANDBACKUP_LoadImage(selected_mem)
            ElseIf MySettings.OPERATION_MODE = DeviceMode.SPI_NAND Then
                NANDBACKUP_LoadImage(selected_mem)
            Else
                NORBACKUP_LoadImage(selected_mem)
            End If
        Catch ex As Exception
        Finally
            If selected_mem.mem_dev IsNot Nothing Then
                selected_mem.mem_dev.FCUSB.USB_LEDOn()
                selected_mem.mem_control.SetProgress(0)
            End If
            Backup_Stop()
        End Try
    End Sub

    Private Sub Backup_Start()
        If Me.InvokeRequired Then
            Dim d As New OnButtonEnable(AddressOf Backup_Start)
            Me.Invoke(d)
        Else
            MyTabs_DisabledControls(True)
            BACKUP_OPERATION_RUNNING = True
        End If
    End Sub

    Private Sub Backup_Stop()
        If Me.InvokeRequired Then
            Dim d As New OnButtonEnable(AddressOf Backup_Stop)
            Me.Invoke(d)
        Else
            MyTabs_EnableControls()
            MyTabs_RefreshAll()
            BACKUP_OPERATION_RUNNING = False
        End If
    End Sub

    Private Sub PrintNandFlashDetails(mem_dev As MemoryDeviceInstance)
        Dim mem_name As String
        Dim flash_size As Long
        Dim page_size As UInt16
        Dim spare_size As UInt16
        Dim block_size As Integer
        If mem_dev.FlashType = MemoryType.SERIAL_NAND Then
            Dim SNAND_IF As SPINAND_Programmer = CType(mem_dev.MEM_IF, SPINAND_Programmer)
            mem_name = SNAND_IF.MyFlashDevice.NAME
            flash_size = SNAND_IF.MyFlashDevice.FLASH_SIZE
            page_size = SNAND_IF.MyFlashDevice.PAGE_SIZE
            spare_size = SNAND_IF.MyFlashDevice.PAGE_EXT
            block_size = SNAND_IF.MyFlashDevice.Block_Size
        ElseIf mem_dev.FlashType = MemoryType.PARALLEL_NAND Then
            Dim PNAND_IF As PARALLEL_NAND = CType(mem_dev.MEM_IF, PARALLEL_NAND)
            mem_name = PNAND_IF.MyFlashDevice.NAME
            flash_size = PNAND_IF.MyFlashDevice.FLASH_SIZE
            page_size = PNAND_IF.MyFlashDevice.PAGE_SIZE
            spare_size = PNAND_IF.MyFlashDevice.PAGE_EXT
            block_size = PNAND_IF.MyFlashDevice.Block_Size
        Else
            Exit Sub
        End If
        MainApp.PrintConsole(RM.GetString("gui_creating_nand_file"))
        MainApp.PrintConsole("Memory device name: " & mem_name)
        MainApp.PrintConsole("Flash size: " & Format(flash_size, "#,###") & " bytes")
        MainApp.PrintConsole("Page size: " & Format(page_size, "#,###") & " bytes")
        MainApp.PrintConsole("Extended size: " & Format(spare_size, "#,###") & " bytes")
        MainApp.PrintConsole("Block size: " & Format(block_size, "#,###") & " bytes")
    End Sub

    Private Function PromptUserForSaveLocation(default_name As String) As String
        If Me.InvokeRequired Then
            Dim d As New cbPromptUserForSaveLocation(AddressOf PromptUserForSaveLocation)
            Return CStr(Me.Invoke(d, {default_name}))
        Else
            Dim SaveMe As New SaveFileDialog
            SaveMe.AddExtension = True
            SaveMe.InitialDirectory = DefaultLocation
            SaveMe.Title = RM.GetString("gui_select_location")
            SaveMe.CheckPathExists = True
            SaveMe.FileName = default_name
            Dim FcFname As String = RM.GetString("gui_compressed_img") & " (*.zip)|*.zip"
            Dim AllF As String = "All files (*.*)|*.*"
            SaveMe.Filter = FcFname & "|" & AllF
            If SaveMe.ShowDialog = DialogResult.OK Then
                Return SaveMe.FileName
            Else
                Return ""
            End If
        End If
    End Function

    Private Sub NORBACKUP_SaveImage(mem_tuple As (mem_dev As MemoryDeviceInstance, mem_control As MemControl_v2))
        BACKUP_FILE = PromptUserForSaveLocation(mem_tuple.mem_dev.Name)
        If BACKUP_FILE.Equals("") Then Exit Sub
        Dim ZipOutputFile As New IO.FileInfo(BACKUP_FILE)
        If ZipOutputFile.Exists Then ZipOutputFile.Delete()
        Using new_zip_file As IO.FileStream = ZipOutputFile.Open(IO.FileMode.Create)
            Using archive As New ZipArchive(new_zip_file, ZipArchiveMode.Update)
                Dim zipfile As ZipArchiveEntry = archive.CreateEntry("Main.bin")
                Using main_bin As New IO.BinaryWriter(zipfile.Open)
                    Dim bytes_left As Long = mem_tuple.mem_dev.Size
                    Dim base_addr As Long = 0
                    Do While (bytes_left > 0)
                        Dim packet_size As Integer = CInt(Math.Min(Kb512, bytes_left))
                        Dim packet() As Byte = mem_tuple.mem_dev.ReadFlash(base_addr, packet_size)
                        main_bin.Write(packet)
                        base_addr += packet_size
                        bytes_left -= packet_size
                        Dim Percent As Integer = CInt(Math.Round(((mem_tuple.mem_dev.Size - bytes_left) / mem_tuple.mem_dev.Size) * 100))
                        mem_tuple.mem_control.SetProgress(Percent)
                        SetStatus(String.Format(RM.GetString("gui_reading_flash"), Format(base_addr, "#,###"), Format(mem_tuple.mem_dev.Size, "#,###"), Percent))
                    Loop
                End Using
            End Using
        End Using
        mem_tuple.mem_control.SetProgress(0)
        SetStatus(String.Format(RM.GetString("gui_img_saved_to_disk"), ZipOutputFile.Name))
    End Sub

    Private Sub NANDBACKUP_SaveImage(mem_tuple As (mem_dev As MemoryDeviceInstance, mem_control As MemControl_v2))
        Try
            mem_tuple.mem_dev.IsTaskRunning = True
            Dim nand_cfg As New NAND_CONFIG
            If (MySettings.OPERATION_MODE = DeviceMode.PNAND) Then
                Dim m As P_NAND = CType(mem_tuple.mem_dev.MEM_IF, PARALLEL_NAND).MyFlashDevice
                nand_cfg.flash_name = m.NAME
                nand_cfg.chipid = Hex(m.MFG_CODE).PadLeft(2, "0"c) & Hex(m.ID1).PadLeft(4, "0"c) & Hex(m.ID2).PadLeft(4, "0"c)
                nand_cfg.flash_size = m.FLASH_SIZE
                nand_cfg.page_size = m.PAGE_SIZE
                nand_cfg.oob_size = m.PAGE_EXT
                nand_cfg.block_count = m.BLOCK_COUNT
                nand_cfg.page_count = m.PAGE_COUNT
                nand_cfg.block_size = m.PAGE_COUNT * CInt(m.PAGE_SIZE + m.PAGE_EXT)
            ElseIf (MySettings.OPERATION_MODE = DeviceMode.SPI_NAND) Then
                Dim m As SPI_NAND = CType(mem_tuple.mem_dev.MEM_IF, SPINAND_Programmer).MyFlashDevice
                nand_cfg.flash_name = m.NAME
                nand_cfg.chipid = Hex(m.MFG_CODE).PadLeft(2, "0"c) & Hex(m.ID1).PadLeft(4, "0"c)
                nand_cfg.flash_size = m.FLASH_SIZE
                nand_cfg.page_size = m.PAGE_SIZE
                nand_cfg.oob_size = m.PAGE_EXT
                nand_cfg.block_count = m.BLOCK_COUNT
                nand_cfg.page_count = m.PAGE_COUNT
                nand_cfg.block_size = m.PAGE_COUNT * CInt(m.PAGE_SIZE + m.PAGE_EXT)
            Else
                Exit Sub
            End If
            BACKUP_FILE = PromptUserForSaveLocation(nand_cfg.flash_name)
            If String.IsNullOrEmpty(BACKUP_FILE) Then Exit Sub
            Dim ZipOutputFile As New IO.FileInfo(BACKUP_FILE)
            If ZipOutputFile.Exists Then ZipOutputFile.Delete()
            Using new_zip_file As IO.FileStream = ZipOutputFile.Open(IO.FileMode.Create)
                Using archive As New ZipArchive(new_zip_file, ZipArchiveMode.Update)
                    ZIP_SetFile(archive, "NAND.cfg", nand_cfg.Save)
                    Dim page_addr As Integer = 0
                    For block_index As Integer = 0 To nand_cfg.block_count - 1
                        If mem_tuple.mem_control.USER_HIT_CANCEL Then
                            SetStatus(RM.GetString("mc_mem_read_canceled"))
                            Exit Sub
                        End If
                        Dim Percent As Integer = CInt(Math.Round(((block_index + 1) / nand_cfg.block_count) * 100))
                        mem_tuple.mem_control.SetProgress(Percent)
                        SetStatus(String.Format(RM.GetString("nand_reading_block"), Format((block_index + 1), "#,###"), Format(nand_cfg.block_count, "#,###"), Percent))
                        Dim block_data(nand_cfg.block_size - 1) As Byte
                        If (MySettings.OPERATION_MODE = DeviceMode.PNAND) Then
                            CType(mem_tuple.mem_dev.MEM_IF, PARALLEL_NAND).NAND_ReadPages(page_addr, 0, block_data.Length, FlashArea.All, block_data)
                        ElseIf (MySettings.OPERATION_MODE = DeviceMode.SPI_NAND) Then
                            CType(mem_tuple.mem_dev.MEM_IF, SPINAND_Programmer).NAND_ReadPages(page_addr, 0, block_data.Length, FlashArea.All, block_data)
                        End If
                        page_addr += nand_cfg.page_count
                        ZIP_SetFile(archive, "BLOCK_" & (block_index + 1).ToString.PadLeft(4, "0"c), block_data)
                    Next
                End Using
            End Using
            mem_tuple.mem_control.SetProgress(0)
            SetStatus(String.Format(RM.GetString("gui_saved_img_to_disk"), ZipOutputFile.Name))
        Catch ex As Exception
        Finally
            mem_tuple.mem_dev.IsTaskRunning = False
        End Try
    End Sub
    'Reads a zip file and writes to the uncompress contents to an attached NOR Flash memory device
    Private Sub NORBACKUP_LoadImage(mem_tuple As (mem_dev As MemoryDeviceInstance, mem_control As MemControl_v2))
        Dim sector_count As Integer = mem_tuple.mem_dev.GetSectorCount()
        Dim bytes_left As Long = mem_tuple.mem_dev.Size
        Dim base_addr As Long = 0
        Dim ZipInputFile As New IO.FileInfo(BACKUP_FILE)
        Using zip_file As IO.FileStream = ZipInputFile.Open(IO.FileMode.Open)
            Using archive As New ZipArchive(zip_file, ZipArchiveMode.Read)
                Dim zipfile As ZipArchiveEntry = archive.GetEntry("Main.bin")
                Using nand_main As New IO.BinaryReader(zipfile.Open)
                    For i = 0 To sector_count - 1
                        Dim sector_size As Integer = mem_tuple.mem_dev.GetSectorSize(i)
                        Dim data_in() As Byte = nand_main.ReadBytes(sector_size)
                        If data_in Is Nothing Then Exit Sub
                        Dim WriteSuccess As Boolean = mem_tuple.mem_dev.WriteBytes(base_addr, data_in, MySettings.VERIFY_WRITE)
                        If Not WriteSuccess Then Exit Sub
                        bytes_left -= sector_size
                        base_addr += sector_size
                        Dim Percent As Integer = CInt(Math.Round(((mem_tuple.mem_dev.Size - bytes_left) / mem_tuple.mem_dev.Size) * 100))
                        mem_tuple.mem_control.SetProgress(Percent)
                        SetStatus(String.Format(RM.GetString("gui_writing_flash"), Format(base_addr, "#,###"), Format(mem_tuple.mem_dev.Size, "#,###"), Percent))
                    Next
                End Using
            End Using
        End Using
        mem_tuple.mem_control.SetProgress(0)
        SetStatus(RM.GetString("gui_img_successful"))
    End Sub
    'Reads a zip file and writes to the uncompress contents to an attached NAND Flash memory device
    Private Sub NANDBACKUP_LoadImage(mem_tuple As (mem_dev As MemoryDeviceInstance, mem_control As MemControl_v2))
        Try
            Dim MyBlockManager As NAND_BLOCK_IF = Nothing
            If (MySettings.OPERATION_MODE = DeviceMode.PNAND) Then
                Dim PNAND_IF As PARALLEL_NAND = CType(mem_tuple.mem_dev.MEM_IF, PARALLEL_NAND)
                MyBlockManager = PNAND_IF.BlockManager
            ElseIf (MySettings.OPERATION_MODE = DeviceMode.SPI_NAND) Then
                Dim SNAND_IF As SPINAND_Programmer = CType(mem_tuple.mem_dev.MEM_IF, SPINAND_Programmer)
                MainApp.PrintConsole("Disabling SPI NAND ECC")
                SNAND_IF.ECC_ENABLED = False
                Utilities.Sleep(20)
                MyBlockManager = SNAND_IF.BlockManager
            End If
            Dim ZipInputFile As New IO.FileInfo(BACKUP_FILE)
            Using zip_file As IO.FileStream = ZipInputFile.Open(IO.FileMode.Open)
                Using archive As New ZipArchive(zip_file, ZipArchiveMode.Read)
                    Dim nand_cfg_data() As Byte = ZIP_GetFile(archive, "NAND.cfg")
                    If nand_cfg_data Is Nothing Then SetStatus(RM.GetString("gui_not_valid_img")) : Exit Sub
                    Dim nand_cfg As New NAND_CONFIG(nand_cfg_data)
                    MainApp.PrintConsole(String.Format(RM.GetString("gui_programming_img"), nand_cfg.flash_name), True)
                    Dim pages_per_block As UInt16 = CUShort(nand_cfg.block_size \ nand_cfg.page_size)
                    Dim page_size_total As UInt16 = (nand_cfg.page_size + nand_cfg.oob_size)
                    Dim block_total As Integer = CInt(pages_per_block) * page_size_total
                    Dim total_pages As Integer = CInt(nand_cfg.flash_size \ nand_cfg.page_size)
                    Dim page_addr As Integer = 0 'Target page address
                    Dim BlocksToWrite As Integer = Math.Min(nand_cfg.block_count, MyBlockManager.VALID_BLOCKS)
                    For block_index As Integer = 0 To BlocksToWrite - 1
                        If mem_tuple.mem_control.USER_HIT_CANCEL Then SetStatus(RM.GetString("mc_wr_user_canceled")) : Exit Sub
                        Dim Percent As Integer = CInt(Math.Round(((block_index + 1) / nand_cfg.block_count) * 100))
                        mem_tuple.mem_control.SetProgress(Percent)
                        SetStatus(String.Format(RM.GetString("nand_writing_block"), Format((block_index + 1), "#,###"), Format(nand_cfg.block_count, "#,###"), Percent))
                        Dim block_data() As Byte = ZIP_GetFile(archive, "BLOCK_" & (block_index + 1).ToString.PadLeft(4, "0"c))
                        If block_data Is Nothing Then
                            SetStatus(String.Format(RM.GetString("nand_missing_block"), block_index)) : Exit Sub
                        End If
                        If NANDBACKUP_IsValid(block_data, nand_cfg.page_size, nand_cfg.oob_size) Then 'This block is valid, we are going
                            If (MySettings.OPERATION_MODE = DeviceMode.PNAND) Then
                                Dim PNAND_IF As PARALLEL_NAND = CType(mem_tuple.mem_dev.MEM_IF, PARALLEL_NAND)
                                Dim phy_addr As Integer = PNAND_IF.BlockManager.GetPhysical(page_addr)
                                PNAND_IF.SectorErase_Physical(phy_addr)
                                If Not Utilities.IsByteArrayFilled(block_data, 255) Then 'We want to skip blank data
                                    PNAND_IF.WritePage_Physical(phy_addr, block_data, FlashArea.All)
                                End If
                            ElseIf (MySettings.OPERATION_MODE = DeviceMode.SPI_NAND) Then
                                Dim SNAND_IF As SPINAND_Programmer = CType(mem_tuple.mem_dev.MEM_IF, SPINAND_Programmer)
                                Dim phy_addr As Integer = SNAND_IF.BlockManager.GetPhysical(page_addr)
                                SNAND_IF.SectorErase_Physical(phy_addr)
                                If Not Utilities.IsByteArrayFilled(block_data, 255) Then 'We want to skip blank data
                                    SNAND_IF.WritePage_Physical(phy_addr, block_data, FlashArea.All)
                                End If
                            End If
                            page_addr += nand_cfg.page_count '64
                        End If
                    Next
                    mem_tuple.mem_control.SetProgress(0)
                    SetStatus(RM.GetString("gui_img_successful"))
                End Using
            End Using
        Catch ex As Exception
        Finally
            If (MySettings.OPERATION_MODE = DeviceMode.SPI_NAND) Then
                Dim SNAND_IF As SPINAND_Programmer = CType(mem_tuple.mem_dev.MEM_IF, SPINAND_Programmer)
                SNAND_IF.ECC_ENABLED = Not MySettings.SPI_NAND_DISABLE_ECC
            End If
        End Try
    End Sub
    'This uses the current settings to indicate if this block is valid
    Private Function NANDBACKUP_IsValid(block_data() As Byte, page_size As UInt16, oob_size As UInt16) As Boolean
        Try
            If MySettings.NAND_BadBlockMode = BadBlockMarker.Disabled Then Return True
            Dim layout_main As UInt16
            Dim layout_oob As UInt16
            If MySettings.NAND_Layout = NandMemLayout.Separated Then
                layout_main = page_size
                layout_oob = oob_size
            ElseIf MySettings.NAND_Layout = NandMemLayout.Segmented Then
                Select Case page_size
                    Case 4096
                        layout_main = (page_size \ 4US)
                        layout_oob = (oob_size \ 4US)
                    Case 2048
                        layout_main = (page_size \ 4US)
                        layout_oob = (oob_size \ 4US)
                    Case Else
                        layout_main = page_size
                        layout_oob = oob_size
                End Select
            End If
            Dim oob As New List(Of Byte()) 'contains oob
            Dim page_size_total As UInt16 = (layout_main + layout_oob)
            Dim page_count As Integer = block_data.Length \ page_size_total
            For i = 0 To page_count - 1
                Dim oob_data(layout_oob - 1) As Byte
                Array.Copy(block_data, i * page_size_total, oob_data, 0, oob_data.Length)
                oob.Add(oob_data)
            Next
            Dim page_one() As Byte = oob(0)
            Dim page_two() As Byte = oob(1)
            Dim page_last() As Byte = oob(oob.Count - 1)
            Dim valid_block As Boolean = True
            Dim markers As Integer = MySettings.NAND_BadBlockMode
            If (markers And BadBlockMarker._1stByte_FirstPage) > 0 Then
                If Not ((page_one(0)) = 255) Then valid_block = False
            End If
            If (markers And BadBlockMarker._1stByte_SecondPage) > 0 Then
                If Not ((page_two(0)) = 255) Then valid_block = False
            End If
            If (markers And BadBlockMarker._1stByte_LastPage) > 0 Then
                If Not ((page_last(0)) = 255) Then valid_block = False
            End If
            If (markers And BadBlockMarker._6thByte_FirstPage) > 0 Then
                If Not ((page_one(5)) = 255) Then valid_block = False
            End If
            If (markers And BadBlockMarker._6thByte_SecondPage) > 0 Then
                If Not ((page_two(5)) = 255) Then valid_block = False
            End If
            Return valid_block
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function IsValidAddress(base As Long, list() As Long) As Boolean
        For Each addr In list
            If addr = base Then Return False
        Next
        Return True
    End Function

#End Region

#Region "Status Bar"
    Private StatusBar_IsNormal As Boolean = True

    Public Sub StatusBar_SwitchToOperation(labels() As ToolStripStatusLabel)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() StatusBar_SwitchToOperation(labels))
        Else
            FlashStatusLabel.Visible = False
            While FlashStatus.Items.Count > 1
                FlashStatus.Items.RemoveAt(1)
            End While
            For Each item In labels
                FlashStatus.Items.Add(item)
            Next
            Application.DoEvents()
            StatusBar_IsNormal = False
        End If
    End Sub

    Public Sub StatusBar_SwitchToNormal()
        If Me.InvokeRequired Then
            Me.Invoke(Sub() StatusBar_SwitchToNormal())
        Else
            While FlashStatus.Items.Count > 1
                FlashStatus.Items.RemoveAt(1)
            End While
            FlashStatusLabel.Visible = True
            StatusBar_IsNormal = True
        End If
    End Sub

    Public Sub SetStatus(Msg As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() SetStatus(Msg))
        Else
            Me.FlashStatusLabel.Text = Msg
            Application.DoEvents()
        End If
    End Sub

    Public Sub OperationStarted(mem_ctrl As MemControl_v2)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() OperationStarted(mem_ctrl))
        Else
            Dim selected_mem = MyTabs_GetSelectedTuple()
            If selected_mem.mem_dev Is Nothing Then Exit Sub
            If selected_mem.mem_control Is mem_ctrl Then
                StatusBar_SwitchToOperation(mem_ctrl.StatusLabels)
            End If
        End If
    End Sub

    Public Sub OperationStopped(mem_ctrl As MemControl_v2)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() OperationStopped(mem_ctrl))
        Else
            Dim selected_mem = MyTabs_GetSelectedTuple()
            If selected_mem.mem_dev Is Nothing Then Exit Sub
            If selected_mem.mem_control Is mem_ctrl Then
                StatusBar_SwitchToNormal()
            End If
        End If
    End Sub

    Private Sub MyTabs_SelectedIndexChanged(sender As Object, e As EventArgs) Handles MyTabs.SelectedIndexChanged
        Dim selected_mem = MyTabs_GetSelectedTuple()
        If selected_mem.mem_dev IsNot Nothing Then
            If selected_mem.mem_control.IN_OPERATION Then
                StatusBar_SwitchToOperation(selected_mem.mem_control.StatusLabels)
            ElseIf (Not StatusBar_IsNormal) Then
                StatusBar_SwitchToNormal()
            End If
        Else
            StatusBar_SwitchToNormal()
        End If
    End Sub

    Public Sub SetProgress(memDevSelected As MemoryDeviceInstance, percent As Integer)
        Dim device_tuples() = MyTabs_GetDeviceTuples()
        If device_tuples Is Nothing OrElse device_tuples.Length = 0 Then Exit Sub
        For Each device_tuple In device_tuples
            If device_tuple.mem_dev Is memDevSelected Then
                device_tuple.mem_control.SetProgress(percent)
                Exit Sub
            End If
        Next
    End Sub

#End Region

#Region "Script Menu"

    Private Class script_option
        Public file_name As String
        Public jedec_id As UInt32
    End Class

    Private Delegate Sub onLoadScripts(JEDEC_ID As UInt32)
    Public Sub LoadScripts(JEDEC_ID As UInt32)
        If Me.InvokeRequired Then
            Dim n As New onLoadScripts(AddressOf LoadScripts)
            Me.Invoke(n, {JEDEC_ID})
        Else
            mi_script_selected.DropDownItems.Clear()
            MainApp.PrintConsole(RM.GetString("gui_script_checking"))
            Dim MyScripts() As AutoRunScript = GetCompatibleScripts(JEDEC_ID)
            Dim SelectScript As Integer = 0
            Dim ScriptToLoad As String = ""
            If MyScripts Is Nothing Then
                MainApp.PrintConsole(RM.GetString("gui_script_non_available"))
            ElseIf MyScripts.Length = 1 Then
                mi_script_selected.Enabled = True
                mi_script_load.Enabled = True
                mi_script_unload.Enabled = True
                Dim tsi As ToolStripMenuItem = CType(mi_script_selected.DropDownItems.Add(MyScripts(0).DeviceName), ToolStripMenuItem)
                tsi.Tag = New script_option With {.file_name = MyScripts(0).FileName, .jedec_id = JEDEC_ID}
                AddHandler tsi.Click, AddressOf LoadSelectedScript
                tsi.Checked = True
                ScriptToLoad = MyScripts(0).FileName
            Else 'Multiple scripts (choose preferrence)
                Dim pre_script_name As String = MySettings.GetPrefferedScript(JEDEC_ID)
                mi_script_selected.Enabled = True
                mi_script_load.Enabled = True
                mi_script_unload.Enabled = True
                For i = 0 To MyScripts.Length - 1
                    Dim tsi As ToolStripMenuItem = CType(mi_script_selected.DropDownItems.Add(MyScripts(i).DeviceName), ToolStripMenuItem)
                    tsi.Tag = New script_option With {.file_name = MyScripts(i).FileName, .jedec_id = JEDEC_ID}
                    AddHandler tsi.Click, AddressOf LoadSelectedScript
                    If i = 0 OrElse pre_script_name.ToUpper.Equals(MyScripts(i).FileName) Then
                        tsi.Checked = True
                        SelectScript = i
                    End If
                Next
                ScriptToLoad = MyScripts(SelectScript).FileName
            End If
            If Not ScriptToLoad.Equals("") Then
                Dim df As New IO.FileInfo(ScriptPath & ScriptToLoad)
                Dim script_text() As String = Utilities.FileIO.ReadFile(df.FullName)
                If script_text IsNot Nothing Then ScriptProcessor.RunScriptAsync(script_text)
            End If
        End If
    End Sub

    Private Function GetCompatibleScripts(CPUID As UInteger) As AutoRunScript()
        Try
            Dim ScriptList As New List(Of AutoRunScript)
            Dim Autorun As New IO.FileInfo(ScriptPath & "autorun.ini")
            If Autorun.Exists Then
                Dim f() As String = Utilities.FileIO.ReadFile(Autorun.FullName)
                For Each line In f
                    line = Utilities.RemoveComment(line).Trim
                    If Not line.Equals("") Then
                        Dim autoline() As String = line.Split(CChar(":"))
                        Dim n As AutoRunScript
                        n.JEDECID = Utilities.HexToUInt(autoline(0))
                        n.FileName = autoline(1)
                        n.DeviceName = autoline(2)
                        ScriptList.Add(n)
                    End If
                Next
                If ScriptList.Count > 0 Then Return ScriptList.ToArray()
            End If
        Catch ex As Exception
        End Try
        Return Nothing
    End Function

    Private Structure AutoRunScript
        Public JEDECID As UInt32 'the JEDEC ID this script is for
        Public DeviceName As String
        Public FileName As String
    End Structure

    Private Sub mi_script_unload_Click(sender As Object, e As EventArgs) Handles mi_script_unload.Click
        Try
            UnloadActiveScript()
            StatusMessages_LoadMemoryDevices()
            SetStatus(RM.GetString("gui_script_reset"))
        Catch ex As Exception
        End Try
    End Sub

    Private Sub RunActiveScript(scriptName As String)
        PrintConsole(String.Format(RM.GetString("gui_script_loading"), scriptName))
        Dim f As New IO.FileInfo(scriptName)
        If Not f.Exists Then
            SetStatus(RM.GetString("gui_script_can_not_load"))
            Exit Sub
        End If
        UnloadActiveScript()
        Dim script_text() As String = Utilities.FileIO.ReadFile(f.FullName)
        If script_text IsNot Nothing AndAlso ScriptProcessor.RunScriptAsync(script_text) Then
            UpdateStatusMessage(RM.GetString("gui_active_script"), f.Name)
            SetStatus(String.Format(RM.GetString("gui_script_loaded"), f.Name)) 'RM.GetString("fcusb_script_loaded")
            mi_script_unload.Enabled = True
        End If
    End Sub

    Private Sub UnloadActiveScript()
        ScriptProcessor.Unload()
        MyTabs_RemoveUserTabs()
        Application.DoEvents()
        RemoveScriptChecks()
        RemoveStatusMessage(RM.GetString("gui_active_script"))
    End Sub

    Private Sub mi_script_load_Click(sender As Object, e As EventArgs) Handles mi_script_load.Click
        Try
            Dim OpenMe As New OpenFileDialog
            OpenMe.AddExtension = True
            OpenMe.InitialDirectory = Application.StartupPath & "\Scripts\"
            OpenMe.Title = RM.GetString("gui_script_open")
            OpenMe.CheckPathExists = True
            Dim FcFname As String = "FlachcatUSB Scripts (*.fcs)|*.fcs"
            Dim AllF As String = "All files (*.*)|*.*"
            OpenMe.Filter = FcFname & "|" & AllF
            If OpenMe.ShowDialog = DialogResult.OK Then RunActiveScript(OpenMe.FileName)
        Catch ex As Exception
        End Try
    End Sub

    Private Sub LoadSelectedScript(sender As Object, e As EventArgs)
        Dim tsi As ToolStripMenuItem = CType(sender, ToolStripMenuItem)
        If (Not tsi.Checked) Then
            Dim scr_obj As script_option = DirectCast(tsi.Tag, script_option)
            RunActiveScript(Application.StartupPath & "\Scripts\" & scr_obj.file_name)
            RemoveScriptChecks()
            tsi.Checked = True
            MySettings.SetPrefferedScript(scr_obj.file_name, scr_obj.jedec_id)
        End If
    End Sub

    Private Sub RemoveScriptChecks()
        Dim tsi As ToolStripMenuItem
        For Each tsi In mi_script_selected.DropDownItems
            tsi.Checked = False
        Next
    End Sub

#End Region

#Region "License"

    Private Sub mi_license_menu_Click(sender As Object, e As EventArgs) Handles mi_license_menu.Click
        Dim n As New FrmLicense
        n.ShowDialog()
        MyForm_LicenseInit()
    End Sub

    Private Sub MyForm_LicenseInit()
        Try
            Dim left_part As String = "FlashcatUSB (Build " & FC_BUILD & ")"
            Select Case LicenseStatus
                Case LicenseStatusEnum.NotLicensed
                    Me.Text = left_part & " - Personal Use Only"
                Case LicenseStatusEnum.LicenseExpired
                    Me.Text = left_part & " - LICENSE EXPIRED"
                Case LicenseStatusEnum.LicensedValid
                    Me.Text = left_part & " - Licensed to " & MySettings.LICENSED_TO
            End Select
        Catch ex As Exception
        End Try
    End Sub

#End Region

    Public Sub USBDeviceConnected(usb_dev As FCUSB_DEVICE)
        Dim mode As DeviceMode = MySettings.OPERATION_MODE
        If (mode = DeviceMode.SPI) Then
            UpdateStatusMessage(RM.GetString("device_mode"), "Serial Programmable Interface (SPI)")
        ElseIf (mode = DeviceMode.SQI) Then
            UpdateStatusMessage(RM.GetString("device_mode"), "Serial Programmable Interface (SQI)")
        ElseIf (mode = DeviceMode.SPI_EEPROM) Then
            UpdateStatusMessage(RM.GetString("device_mode"), "SPI EEPROM")
            If MySettings.SPI_EEPROM.Equals("") Then
                SetStatus("Device mode Set To SPI EEPROM, configure SPI settings Then click 'Detect'")
                Exit Sub
            End If
        ElseIf (mode = DeviceMode.P_EEPROM) Then
            UpdateStatusMessage(RM.GetString("device_mode"), "PARALLEL EEPROM")
            If MySettings.PARALLEL_EEPROM.Equals("") Then
                SetStatus("Device mode Set To PARALLEL EEPROM, configure parallel settings Then click 'Detect'")
                Exit Sub
            End If
        ElseIf (mode = DeviceMode.SPI_NAND) Then
            UpdateStatusMessage(RM.GetString("device_mode"), "Serial Programmable Interface (SPI-NAND)")
        ElseIf (mode = DeviceMode.I2C_EEPROM) Then
            UpdateStatusMessage(RM.GetString("device_mode"), "Inter-Integrated Circuit (I²C)")
            If MySettings.I2C_INDEX = 0 Then
                SetStatus(RM.GetString("device_mode_i2c")) '"Device mode set to I2C EEPROM, configure I2C settings then click 'Detect'"
                Exit Sub
            End If
        ElseIf (mode = DeviceMode.Microwire) Then
            UpdateStatusMessage(RM.GetString("device_mode"), "Microwire (3-wire EEPROM)")
        ElseIf (mode = DeviceMode.PNOR) Then
            UpdateStatusMessage(RM.GetString("device_mode"), "Parallel NOR mode")
        ElseIf (mode = DeviceMode.PNAND) Then
            UpdateStatusMessage(RM.GetString("device_mode"), "Parallel NAND mode")
        ElseIf (mode = DeviceMode.FWH) Then
            UpdateStatusMessage(RM.GetString("device_mode"), "FWH Flash")
        ElseIf (mode = DeviceMode.EPROM) Then
            UpdateStatusMessage(RM.GetString("device_mode"), "EPROM mode")
        ElseIf (mode = DeviceMode.ONE_WIRE) Then
            UpdateStatusMessage(RM.GetString("device_mode"), "SWI mode")
        ElseIf mode = DeviceMode.JTAG Then
            UpdateStatusMessage(RM.GetString("device_mode"), "JTAG")
            JTAG_Init(usb_dev)
            If (usb_dev.HWBOARD = FCUSB_BOARD.Professional_PCB5) Then
                SetStatus(String.Format(RM.GetString("jtag_ready"), "FlashcatUSB Pro"))
            ElseIf (usb_dev.HWBOARD = FCUSB_BOARD.XPORT_PCB2) Then
                usb_dev.USB_LEDOn()
                SetStatus(String.Format(RM.GetString("jtag_ready"), "FlashcatUSB XPORT"))
            Else
                SetStatus(String.Format(RM.GetString("jtag_ready"), "FlashcatUSB Classic"))
            End If
            Exit Sub 'JTAG can not detect memory device
        ElseIf mode = DeviceMode.HyperFlash Then
            UpdateStatusMessage(RM.GetString("device_mode"), "HyperFlash mode")
        End If
        DetectDevice.Device(usb_dev, GetDeviceParams())
    End Sub

    Private Sub JTAG_Init(usb_dev As FCUSB_DEVICE)
        CURRENT_DEVICE_MODE = MySettings.OPERATION_MODE
        If usb_dev.HasLogic Then
            usb_dev.JTAG_IF.TCK_SPEED = MySettings.JTAG_SPEED
        Else
            usb_dev.JTAG_IF.TCK_SPEED = JTAG.JTAG_SPEED._1MHZ
        End If
        If usb_dev.JTAG_IF.Init() Then
            MainApp.PrintConsole(RM.GetString("jtag_setup"))
            UpdateStatusMessage(RM.GetString("device_mode"), "JTAG")
            Dim l As New List(Of String)
            For Each item In usb_dev.JTAG_IF.Devices
                l.Add(item.ToString())
            Next
            JTAGMenu_SetItems(l.ToArray())
            JTAG_Select(0)
        Else
            Dim error_msg As String = RM.GetString("jtag_failed_to_connect")
            SetStatus(error_msg)
            UpdateStatusMessage(RM.GetString("device_mode"), error_msg)
            MainApp.PrintConsole(error_msg)
        End If
    End Sub

    Private Delegate Sub cbJTAGMenu_SetItems(device_list() As String)
    Private Delegate Sub cbJTAGMenu_CheckIndex(index As Integer)

    Private Sub JTAGMenu_SetItems(device_list() As String)
        If Me.InvokeRequired Then
            Dim n As New cbJTAGMenu_SetItems(AddressOf JTAGMenu_SetItems)
            Me.Invoke(n, {device_list})
        Else
            mi_jtag_menu.Visible = True
            mi_jtag_menu.DropDownItems.Clear()
            If device_list IsNot Nothing AndAlso device_list.Length > 0 Then
                For i = 0 To device_list.Length - 1
                    Dim t As New ToolStripMenuItem
                    t.Text = device_list(i)
                    t.Tag = i
                    AddHandler t.Click, AddressOf JTAG_device_OnClick
                    mi_jtag_menu.DropDownItems.Add(t)
                Next
            End If
        End If
    End Sub

    Private Sub JTAGMenu_CheckIndex(index As Integer)
        If Me.InvokeRequired Then
            Dim n As New cbJTAGMenu_CheckIndex(AddressOf JTAGMenu_CheckIndex)
            Me.Invoke(n, {index})
        Else
            Dim c As ToolStripItemCollection = mi_jtag_menu.DropDownItems
            For i = 0 To c.Count - 1
                Dim tsi As ToolStripMenuItem = CType(c(i), ToolStripMenuItem)
                If i = index Then
                    tsi.Checked = True
                Else
                    tsi.Checked = False
                End If
            Next
        End If
    End Sub

    Private Sub JTAG_device_OnClick(sender As Object, e As EventArgs)
        Dim tsi As ToolStripMenuItem = CType(sender, ToolStripMenuItem)
        If tsi.Checked Then Exit Sub
        Dim new_index As Integer = CInt(tsi.Tag)
        JTAG_Select(new_index)
    End Sub
    'Selects a device on a JTAG chain
    Private Function JTAG_Select(index As Integer) As Boolean
        If MAIN_FCUSB Is Nothing Then
            PrintConsole("Error: can not select JTAG device (FlashcatUSB not connected)")
            Return False
        End If
        If (MAIN_FCUSB.JTAG_IF.Devices.Count > 0) AndAlso (index < MAIN_FCUSB.JTAG_IF.Devices.Count) Then
            If MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
                MAIN_FCUSB.JTAG_IF.Chain_Select(index)
                JTAGMenu_CheckIndex(index)
                JTAG_LoadDefaultScript()
                Return True
            Else
                PrintConsole("JTAG chain is not valid, not all devices have BSDL loaded")
            End If
        Else
            PrintConsole("JTAG chain contains no devices")
        End If
        Return False
    End Function

    Private Sub JTAG_LoadDefaultScript()
        Try
            UnloadActiveScript()
            RemoveStatusMessage("Device")
            Dim index As Integer = MAIN_FCUSB.JTAG_IF.Chain_SelectedIndex
            If (Not (MAIN_FCUSB.JTAG_IF.Devices(index).IDCODE = 0)) Then
                GUI.UpdateStatusMessage("Device", MAIN_FCUSB.JTAG_IF.Devices(index).ToString())
                GUI.LoadScripts(MAIN_FCUSB.JTAG_IF.Devices(index).IDCODE)
            End If
        Catch ex As Exception
            PrintConsole("Unable to load default script for JTAG device")
        End Try
    End Sub

    Public Sub SetConnectionStatus()
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub() SetConnectionStatus())
            Else
                If AppIsClosing Then Exit Sub
                StatusMessages_LoadMemoryDevices()
                If (MAIN_FCUSB Is Nothing) Then
                    statuspage_progress.Value = 0
                    statuspage_progress.Visible = False
                    mi_jtag_menu.Visible = False
                    Me.lblStatus.Text = RM.GetString("gui_fcusb_disconnected")
                    ClearStatusMessage()
                    UnloadActiveScript()
                    RemoveStatusMessage(RM.GetString("gui_active_script"))
                    If frm_vendor IsNot Nothing Then frm_vendor.Close() 'If the vendor form is open, close it
                Else
                    Me.lblStatus.Text = RM.GetString("gui_fcusb_connected")
                End If
                Application.DoEvents()
            End If
        Catch ex As Exception
        End Try
    End Sub

    Public Sub OnNewDeviceConnected(mem_dev As MemoryDeviceInstance)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() OnNewDeviceConnected(mem_dev))
        Else
            Dim newTab As New TabPage("  " & GetTypeString(mem_dev.FlashType) & "  ")
            Dim MemControl As New MemControl_v2
            AddHandler MemControl.WriteConsole, AddressOf PrintConsole
            AddHandler MemControl.SetStatus, AddressOf SetStatus

            MemControl.SREC_DATAMODE = MySettings.SREC_DATAMODE
            MemControl.VERIFY_WRITE = MySettings.VERIFY_WRITE
            MemControl.Width = newTab.Width
            MemControl.Height = newTab.Height
            MemControl.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top Or AnchorStyles.Bottom
            MemControl.InitMemoryDevice(mem_dev)
            newTab.Tag = (mem_dev, MemControl)

            newTab.Controls.Add(MemControl)
            MyTabs.Controls.Add(newTab)
            MemControl.SetupExtendedLayout()
            mi_refresh.Enabled = True
            StatusMessages_LoadMemoryDevices()
            SetStatus(String.Format(RM.GetString("gui_fcusb_new_device"), mem_dev.Name))
            mem_dev.VendorMenu = Nothing
            Dim flash_vendor As VENDOR_FEATURE = VENDOR_FEATURE.NotSupported
            If mem_dev.MEM_IF.GetType Is GetType(SPI.SPI_Programmer) Then
                Dim SPI_IF As SPI.SPI_Programmer = CType(mem_dev.MEM_IF, SPI.SPI_Programmer)
                flash_vendor = SPI_IF.MyFlashDevice.VENDOR_SPECIFIC
            ElseIf mem_dev.MEM_IF.GetType Is GetType(SPI.SQI_Programmer) Then
                Dim SQI_IF As SPI.SQI_Programmer = CType(mem_dev.MEM_IF, SPI.SQI_Programmer)
                flash_vendor = SQI_IF.MyFlashDevice.VENDOR_SPECIFIC
            End If
            If Not flash_vendor = VENDOR_FEATURE.NotSupported Then
                If (flash_vendor = FlashMemory.VENDOR_FEATURE.Micron) Then
                    mem_dev.VendorMenu = New vendor_micron(mem_dev.MEM_IF)
                ElseIf (flash_vendor = FlashMemory.VENDOR_FEATURE.Spansion_FL) Then
                    mem_dev.VendorMenu = New vendor_spansion_FL(mem_dev.MEM_IF)
                ElseIf (flash_vendor = FlashMemory.VENDOR_FEATURE.ISSI) Then
                    mem_dev.VendorMenu = New vendor_issi(mem_dev.MEM_IF)
                ElseIf (flash_vendor = FlashMemory.VENDOR_FEATURE.Winbond) Then
                    mem_dev.VendorMenu = New vendor_winbond(mem_dev.MEM_IF)
                End If
            End If
        End If
    End Sub

    Private Sub mi_erase_tool_Click(sender As Object, e As EventArgs) Handles mi_erase_tool.Click
        Dim t As New Threading.Thread(AddressOf EraseChipThread)
        t.Name = "tdChipErase"
        t.Start()
    End Sub

    Private Sub EraseChipThread()
        Try
            Dim selected_mem = MyTabs_GetSelectedTuple()
            If selected_mem.mem_dev Is Nothing Then Exit Sub
            selected_mem.mem_dev.FCUSB.USB_LEDBlink()
            If MsgBox(RM.GetString("mc_erase_warning"), MsgBoxStyle.YesNo, String.Format(RM.GetString("mc_erase_confirm"), selected_mem.mem_dev.Name)) = MsgBoxResult.Yes Then
                MainApp.PrintConsole(String.Format(RM.GetString("mc_erase_command_sent"), selected_mem.mem_dev.Name))
                SetStatus(RM.GetString("mem_erasing_device"))
                selected_mem.mem_control.DisableControls(False)
                selected_mem.mem_dev.EraseFlash()
                selected_mem.mem_dev.WaitUntilReady()
                selected_mem.mem_control.EnableControls()
                selected_mem.mem_control.RefreshView()
            End If
            selected_mem.mem_dev.FCUSB.USB_LEDOn()
            SetStatus(RM.GetString("mem_erase_device_success"))
            mi_erase_tool.Enabled = True
        Catch ex As Exception
        End Try
    End Sub

    Private frm_vendor As Form

    Private Sub mi_device_features_Click(sender As Object, e As EventArgs) Handles mi_device_features.Click
        Dim selected_mem = MyTabs_GetSelectedTuple()
        If selected_mem.mem_dev Is Nothing Then Exit Sub
        If selected_mem.mem_dev.VendorMenu Is Nothing Then Exit Sub
        frm_vendor = New Form With {.Text = "Vendor specific / device configuration"}
        If selected_mem.mem_dev.VendorMenu.GetType Is GetType(vendor_microchip_at21) Then
            AddHandler DirectCast(selected_mem.mem_dev.VendorMenu, vendor_microchip_at21).CloseVendorForm, AddressOf mi_close_vendor_form
        End If
        frm_vendor.Width = DirectCast(selected_mem.mem_dev.VendorMenu, Control).Width + 10
        frm_vendor.Height = DirectCast(selected_mem.mem_dev.VendorMenu, Control).Height + 50
        frm_vendor.FormBorderStyle = FormBorderStyle.FixedSingle
        frm_vendor.ShowIcon = False
        frm_vendor.ShowInTaskbar = False
        frm_vendor.MaximizeBox = False
        frm_vendor.MinimizeBox = False
        frm_vendor.Controls.Add(DirectCast(selected_mem.mem_dev.VendorMenu, Control))
        frm_vendor.StartPosition = FormStartPosition.CenterParent
        frm_vendor.ShowDialog()
    End Sub

    Private Sub mi_close_vendor_form(sender As Object)
        Try
            frm_vendor.Close()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub mi_cfi_info_Click(sender As Object, e As EventArgs) Handles mi_cfi_info.Click
        Dim selected_mem = MyTabs_GetSelectedTuple()
        If selected_mem.mem_dev Is Nothing Then Exit Sub
        Dim frmCFI As New Form With {.Width = 330, .Height = 10}
        frmCFI.FormBorderStyle = FormBorderStyle.FixedSingle
        frmCFI.ShowIcon = False
        frmCFI.ShowInTaskbar = False
        frmCFI.MaximizeBox = False
        frmCFI.StartPosition = FormStartPosition.CenterParent
        Dim lbl_cfg_list As New List(Of Label)
        Dim flash_cfi As NOR_CFI = Nothing
        Dim flash_onfi As NAND_ONFI = Nothing
        If selected_mem.mem_dev.MEM_IF.GetType() Is GetType(BSR_Programmer) Then
            flash_cfi = CType(selected_mem.mem_dev.MEM_IF, BSR_Programmer).CFI
        ElseIf selected_mem.mem_dev.MEM_IF.GetType() Is GetType(PARALLEL_NAND) Then
            flash_onfi = CType(selected_mem.mem_dev.MEM_IF, PARALLEL_NAND).ONFI
        ElseIf selected_mem.mem_dev.MEM_IF.GetType() Is GetType(PARALLEL_NOR) Then
            flash_cfi = CType(selected_mem.mem_dev.MEM_IF, PARALLEL_NOR).CFI
        Else
            Exit Sub
        End If
        If flash_cfi IsNot Nothing Then
            frmCFI.Text = "Common Flash Interface (CFI)"
            lbl_cfg_list.Add(New Label With {.Text = "Minimum VCC for program/erase: " & flash_cfi.VCC_MIN_PROGERASE & " V"})
            lbl_cfg_list.Add(New Label With {.Text = "Maxiumum VCC for program/erase: " & flash_cfi.VCC_MAX_PROGERASE & " V"})
            lbl_cfg_list.Add(New Label With {.Text = "Minimum VPP for program/erase: " & flash_cfi.VPP_MIN_PROGERASE & " V"})
            lbl_cfg_list.Add(New Label With {.Text = "Maxiumum VPP for program/erase: " & flash_cfi.VPP_MAX_PROGERASE & " V"})
            lbl_cfg_list.Add(New Label With {.Text = "Typical word programing time: " & flash_cfi.WORD_WRITE_TIMEOUT & " µs"})
            lbl_cfg_list.Add(New Label With {.Text = "Typical max. buffer write time-out: " & flash_cfi.BUFFER_WRITE_TIMEOUT & " µs"})
            lbl_cfg_list.Add(New Label With {.Text = "Typical block erase time-out: " & flash_cfi.BLOCK_ERASE_TIMEOUT & " ms"})
            lbl_cfg_list.Add(New Label With {.Text = "Typical full chip erase time-out: " & flash_cfi.ERASE_TIMEOUT & " ms"})
            lbl_cfg_list.Add(New Label With {.Text = "Maximum word program time-out: " & flash_cfi.WORD_WRITE_MAX_TIMEOUT & " µs"})
            lbl_cfg_list.Add(New Label With {.Text = "Maximum buffer write time-out: " & flash_cfi.BUFFER_WRITE_MAX_TIMEOUT & " µs"})
            lbl_cfg_list.Add(New Label With {.Text = "Maximum block erase time-out: " & flash_cfi.BLOCK_ERASE_MAX_TIMEOUT & " seconds"})
            lbl_cfg_list.Add(New Label With {.Text = "Maximum chip erase time-out: " & flash_cfi.ERASE_MAX_TIMEOUT & " seconds"})
            lbl_cfg_list.Add(New Label With {.Text = "Device size: " & Format(flash_cfi.DEVICE_SIZE, "#,###") & " bytes"})
            lbl_cfg_list.Add(New Label With {.Text = "Data bus interface: " & flash_cfi.DESCRIPTION})
            lbl_cfg_list.Add(New Label With {.Text = "Write buffer size: " & flash_cfi.WRITE_BUFFER_SIZE & " bytes"})
        ElseIf flash_onfi IsNot Nothing Then
            frmCFI.Text = "Open NAND Flash Interface"
            lbl_cfg_list.Add(New Label With {.Text = "Device Manufacturer: " & flash_onfi.DEVICE_MFG})
            lbl_cfg_list.Add(New Label With {.Text = "Device Model: " & flash_onfi.DEVICE_MODEL})
            lbl_cfg_list.Add(New Label With {.Text = "Page size: " & flash_onfi.PAGE_SIZE})
            lbl_cfg_list.Add(New Label With {.Text = "Spare size: " & flash_onfi.SPARE_SIZE})
            lbl_cfg_list.Add(New Label With {.Text = "Pages per block: " & flash_onfi.PAGES_PER_BLOCK})
            lbl_cfg_list.Add(New Label With {.Text = "Blocks per LUN: " & flash_onfi.BLOCKS_PER_LUN})
            lbl_cfg_list.Add(New Label With {.Text = "LUN count: " & flash_onfi.LUN_COUNT})
            lbl_cfg_list.Add(New Label With {.Text = "Bits per cell: " & flash_onfi.BITS_PER_CELL})
        End If
        Dim y As Integer = 8
        For Each cfi_label In lbl_cfg_list
            cfi_label.AutoSize = True
            cfi_label.Location = New Point(40, y)
            y += 18
            frmCFI.Controls.Add(cfi_label)
        Next
        frmCFI.Height = y + 42
        frmCFI.ShowDialog()
    End Sub

    Private Sub Mi_blank_check_Click(sender As Object, e As EventArgs) Handles mi_blank_check.Click
        Try
            Dim td As New Threading.Thread(AddressOf eprom_blank_check)
            td.Name = "eBlankCheck"
            td.Start()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub eprom_blank_check()
        Dim selected_mem = MyTabs_GetSelectedTuple()
        If selected_mem.mem_dev Is Nothing Then Exit Sub
        Try
            SetStatus("Performing EPROM blank check")
            selected_mem.mem_dev.FCUSB.USB_LEDBlink()
            selected_mem.mem_control.DisableControls(False)
            Dim is_blank As Boolean = True
            Dim block_size As Integer = 32768
            Dim block_count As Integer = CInt(selected_mem.mem_dev.Size \ block_size)
            Dim data_addr As Long
            selected_mem.mem_control.SetProgress(0)
            For i = 0 To block_count - 1
                data_addr = i * block_size
                selected_mem.mem_control.SetProgress((i \ block_count) * 100)
                Dim d() As Byte = selected_mem.mem_dev.ReadFlash(data_addr, block_size)
                For x = 0 To d.Length - 1
                    If (Not d(x) = 255) Then
                        is_blank = False
                        data_addr += x
                        Exit For
                    End If
                Next
                If Not is_blank Then Exit For
            Next
            If is_blank Then
                MainApp.PrintConsole("Blank check performed: device is blank (erased)")
                SetStatus("EPROM device is blank (erased)")
            Else
                MainApp.PrintConsole("Blank check performed: device is not blank")
                SetStatus("EPROM device is not blank (0x" & Hex(data_addr).PadLeft(8, "0"c) & " contains data)")
            End If
        Catch ex As Exception
        Finally
            selected_mem.mem_control.SetProgress(0)
            selected_mem.mem_control.EnableControls()
            selected_mem.mem_dev.FCUSB.USB_LEDOn()
        End Try
    End Sub

    Private Function IsAnyDeviceBusy() As Boolean
        Dim device_tuples() = MyTabs_GetDeviceTuples()
        If device_tuples Is Nothing OrElse device_tuples.Length = 0 Then Return False
        For Each device_tuple In device_tuples
            If device_tuple.mem_control.IN_OPERATION Then Return True
        Next
        Return False
    End Function

End Class
