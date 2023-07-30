Imports System.ComponentModel
Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.MemoryInterface
Imports FlashcatUSB.USB.HostClient

Public Class MainForm

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        GUI = Me
        Me.Text = "FlashcatUSB Software (Build " & Build & ")"
        Me.MinimumSize = Me.Size
        Me.MyTabs.DrawMode = TabDrawMode.OwnerDrawFixed
        MyTabs.TabPages.Remove(TabMultiDevice)
        InitStatusMessage()
        LoadSettingsIntoGui()
        ScriptEngine.PrintInformation() 'Script Engine
        Dim libVer As String = Reflection.AssemblyName.GetAssemblyName("LibUsbDotNet.dll").Version.ToString
        PrintConsole(RM.GetString("welcome_to_flashcatusb") & ", build: " & Build)
        PrintConsole("LibUsbDotNet version: " & libVer)
        PrintConsole("Running on: " & My.Computer.Info.OSFullName & " (" & GetOsBitsString() & ")")
        PrintConsole(String.Format(RM.GetString("gui_database_supported"), "Serial NOR memory", FlashDatabase.PartCount(MemoryType.SERIAL_NOR)))
        PrintConsole(String.Format(RM.GetString("gui_database_supported"), "Serial NAND", FlashDatabase.PartCount(MemoryType.SERIAL_NAND)))
        PrintConsole(String.Format(RM.GetString("gui_database_supported"), "Parallel NOR memory (x8/x16)", FlashDatabase.PartCount(MemoryType.PARALLEL_NOR)))
        PrintConsole(String.Format(RM.GetString("gui_database_supported"), "SLC NAND memory (x8)", FlashDatabase.PartCount(MemoryType.SLC_NAND)))
        mi_mode_1wire.Visible = False
        Language_Setup()
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
        Me.mi_vpp.Text = RM.GetString("gui_menu_mode_vpp")
        Me.mi_1V8.Text = String.Format(RM.GetString("gui_menu_mode_voltage"), "1.8v")
        Me.mi_3V3.Text = String.Format(RM.GetString("gui_menu_mode_voltage"), "3.3v")
        Me.mi_5V0.Text = String.Format(RM.GetString("gui_menu_mode_voltage"), "5.0v")
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
        Me.TabMultiDevice.Text = "  " & RM.GetString("gui_tab_multi") & "  "
        Me.FlashStatusLabel.Text = RM.GetString("gui_status_welcome")
        Me.cmd_gang_erase.Text = RM.GetString("gui_gang_erase")
        Me.cmd_gang_write.Text = RM.GetString("gui_gane_write")
        Me.lbl_gang_info.Text = RM.GetString("gui_gang_info")
        Me.lblStatus.Text = RM.GetString("gui_fcusb_disconnected")
    End Sub

    Private Sub mi_language_english_Click(sender As Object, e As EventArgs) Handles mi_language_english.Click
        RM = My.Resources.english.ResourceManager : MySettings.LanguageName = "English"
        Language_Setup()
        detect_event()
    End Sub

    Private Sub mi_language_spanish_Click(sender As Object, e As EventArgs) Handles mi_language_spanish.Click
        RM = My.Resources.spanish.ResourceManager : MySettings.LanguageName = "Spanish"
        Language_Setup()
        detect_event()
    End Sub

    Private Sub mi_language_french_Click(sender As Object, e As EventArgs) Handles mi_language_french.Click
        RM = My.Resources.french.ResourceManager : MySettings.LanguageName = "French"
        Language_Setup()
        detect_event()
    End Sub

    Private Sub mi_language_portuguese_Click(sender As Object, e As EventArgs) Handles mi_language_portuguese.Click
        RM = My.Resources.portuguese.ResourceManager : MySettings.LanguageName = "Portuguese"
        Language_Setup()
        detect_event()
    End Sub

    Private Sub mi_language_russian_Click(sender As Object, e As EventArgs) Handles mi_language_russian.Click
        RM = My.Resources.russian.ResourceManager : MySettings.LanguageName = "Russian"
        Language_Setup()
        detect_event()
    End Sub

#End Region

#Region "Status System"
    Delegate Sub cbSetConnectionStatus(ByVal usb_dev As FCUSB_DEVICE)
    Delegate Sub cbUpdateStatusMessage(ByVal Label As String, ByVal Msg As String)
    Delegate Sub cbRemoveStatusMessage(ByVal Label As String)
    Delegate Sub cbClearStatusMessage()

    Private StatusMessageControls() As Control 'Holds the label that the form displays

    Public Sub UpdateStatusMessage(ByVal Label As String, ByVal Msg As String)
        If Me.InvokeRequired Then
            Dim d As New cbUpdateStatusMessage(AddressOf UpdateStatusMessage)
            Me.Invoke(d, New Object() {Label, Msg})
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
    End Sub

    Public Sub RemoveStatusMessage(ByVal Label As String)
        If Me.InvokeRequired Then
            Dim d As New cbRemoveStatusMessage(AddressOf RemoveStatusMessage)
            Me.Invoke(d, New Object() {Label})
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

    Public Sub RemoveStatusMessageStartsWith(ByVal Label As String)
        If Me.InvokeRequired Then
            Dim d As New cbRemoveStatusMessage(AddressOf RemoveStatusMessageStartsWith)
            Me.Invoke(d, New Object() {Label})
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
            Dim d As New cbClearStatusMessage(AddressOf ClearStatusMessage)
            Me.Invoke(d)
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

#End Region

#Region "Console Tab"

    Delegate Sub cbPrintConsole(ByVal msg As String)
    Private CommandThread As Threading.Thread
    Private ScriptCommand As String

    Public Sub PrintConsole(ByVal Msg As String)
        Try
            If AppIsClosing Then Exit Sub
            If Me.InvokeRequired Then
                Dim d As New cbPrintConsole(AddressOf PrintConsole)
                Me.Invoke(d, New Object() {[Msg]})
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

    Private Sub cmdSaveLog_Click(ByVal sender As Object, ByVal e As EventArgs) Handles cmdSaveLog.Click
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

    Private Sub txtInput_KeyPress(ByVal sender As Object, ByVal e As KeyPressEventArgs) Handles txtInput.KeyPress
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
            ScriptEngine.ExecuteCommand(ScriptCommand)
        Catch ex As Exception
        End Try
    End Sub


#End Region

#Region "Repeat Feature"
    Private MyLastOperation As MemControl_v2.XFER_Operation
    Private MyLastUsbInterface As FCUSB_DEVICE 'This is the PORT of the device that was used
    Private Delegate Sub cbSuccessfulWriteOperation(ByVal usb_dev As FCUSB_DEVICE, ByVal x As MemControl_v2.XFER_Operation)

    Public Sub SuccessfulWriteOperation(ByVal usb_dev As FCUSB_DEVICE, ByVal x As MemControl_v2.XFER_Operation)
        If Me.InvokeRequired Then
            Dim d As New cbSuccessfulWriteOperation(AddressOf SuccessfulWriteOperation)
            Me.Invoke(d, New Object() {usb_dev, x})
        Else
            MyLastOperation = x
            MyLastUsbInterface = usb_dev
            mi_repeat.Enabled = True
        End If
    End Sub

    Private Sub miRepeatWrite_Click(sender As Object, e As EventArgs) Handles mi_repeat.Click
        Try
            mi_repeat.Enabled = False
            WriteConsole(RM.GetString("gui_repeat_beginning"))
            MyLastUsbInterface.Disconnect()
            Dim counter As Integer = 0
            Do While (Not MyLastUsbInterface.IS_CONNECTED)
                If counter = 100 Then '10 seconds
                    WriteConsole(RM.GetString("gui_repeat_failed_reconnect"))
                    Exit Sub
                End If
                Application.DoEvents()
                Utilities.Sleep(100)
                counter += 1
            Loop
            Utilities.Sleep(1000)
            counter = 0
            Do While MyLastUsbInterface.ATTACHED.Count = 0
                If counter = 50 Then '10 seconds
                    WriteConsole(RM.GetString("gui_repeat_failed_detect"))
                    Exit Sub
                End If
                Application.DoEvents()
                Utilities.Sleep(100)
                counter += 1
            Loop
            MyLastUsbInterface.ATTACHED(0).GuiControl.PerformWriteOperation(MyLastOperation)
        Catch ex As Exception
        Finally
            mi_repeat.Enabled = True
        End Try
    End Sub

#End Region

#Region "Tab System"
    Private Delegate Sub cbAddToTab(ByVal usertab As Integer, ByVal Value As Object)
    Private Delegate Sub cbAddTab(ByVal tb As TabPage)
    Private Delegate Sub cbRemoveTab(ByVal i As MemoryDeviceInstance)
    Private Delegate Sub cbCreateFormTab(ByVal Index As Integer, ByVal Name As String)
    Private Delegate Sub cbRemoveAllTabs()
    Private Delegate Sub cbSetStatus(ByVal msg As String)
    Private Delegate Function cbGetSelectedMemoryInterface() As MemoryDeviceInstance
    Private Delegate Sub SetBtnCallback(ByVal Value As Button)
    Delegate Sub cbSetControlText(ByVal usertabind As Integer, ByVal Value As String, ByVal NewText As String)

    Public Sub RemoveAllTabs()
        If Me.MyTabs.InvokeRequired Then
            Dim d As New cbRemoveAllTabs(AddressOf RemoveAllTabs)
            Me.Invoke(d)
        Else
            Dim list As New List(Of TabPage)
            For Each tP As TabPage In MyTabs.Controls
                If tP Is TabStatus Then
                ElseIf tP Is TabConsole Then
                Else
                    list.Add(tP)
                End If
            Next
            For i = 0 To list.Count - 1
                MyTabs.Controls.Remove(CType(list(i), Control))
            Next
        End If
    End Sub

    Public Sub AddTab(ByVal tb As TabPage)
        If Me.MyTabs.InvokeRequired Then
            Dim d As New cbAddTab(AddressOf AddTab)
            Me.Invoke(d, New Object() {tb})
        Else
            tb.Text = " " & tb.Text & " "
            MyTabs.Controls.Add(tb)
        End If
    End Sub

    Public Sub RemoveTab(WithThisInstance As MemoryDeviceInstance)
        If Me.MyTabs.InvokeRequired Then
            Dim d As New cbRemoveTab(AddressOf RemoveTab)
            Me.Invoke(d, New Object() {WithThisInstance})
        Else
            Dim PagesToRemove As New List(Of TabPage)
            For Each tP As TabPage In MyTabs.Controls
                If tP Is TabStatus Then
                ElseIf tP Is TabConsole Then
                Else
                    If (tP.Tag IsNot Nothing) AndAlso tP.Tag.GetType Is GetType(MemoryDeviceInstance) Then
                        Dim this_instance As MemoryDeviceInstance = DirectCast(tP.Tag, MemoryDeviceInstance)
                        If this_instance Is WithThisInstance Then
                            PagesToRemove.Add(tP)
                        End If
                    End If
                End If
            Next
            For i = 0 To PagesToRemove.Count - 1
                MyTabs.Controls.Remove(CType(PagesToRemove(i), Control))
            Next
        End If
    End Sub
    'This refreshes all of the memory devices currently connected
    Private Sub StatusMessages_LoadMemoryDevices()
        GUI.RemoveStatusMessageStartsWith(RM.GetString("gui_memory_device"))
        Dim current_devices() As MemoryDeviceInstance = MyTabs_GetDeviceInstances()
        If current_devices Is Nothing OrElse current_devices.Count = 0 Then Exit Sub
        If MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.JTAG Then
            If MyTabs.TabPages.Contains(TabMultiDevice) Then MyTabs.TabPages.Remove(TabMultiDevice)
        Else
            If (current_devices.Count > 1) Then
                If Not MyTabs.TabPages.Contains(TabMultiDevice) Then
                    MyTabs.TabPages.Insert(2, TabMultiDevice)
                End If
            Else 'Not in multi mode
                If MyTabs.TabPages.Contains(TabMultiDevice) Then
                    MyTabs.TabPages.Remove(TabMultiDevice)
                End If
            End If
        End If
        lbl_gang1.Text = String.Format(RM.GetString("gui_mem_device_status"), "1")
        lbl_gang2.Text = String.Format(RM.GetString("gui_mem_device_status"), "2")
        lbl_gang3.Text = String.Format(RM.GetString("gui_mem_device_status"), "3")
        lbl_gang4.Text = String.Format(RM.GetString("gui_mem_device_status"), "4")
        lbl_gang5.Text = String.Format(RM.GetString("gui_mem_device_status"), "5")
        MyTabs.Refresh()
        Dim counter As Integer = 1
        For Each mem_device In current_devices
            If mem_device.GuiControl Is Nothing Then Continue For
            Dim flash_desc As String = mem_device.Name & " (" & Format(mem_device.Size, "#,###") & " bytes)"
            GUI.UpdateStatusMessage(RM.GetString("gui_memory_device") & " " & counter, flash_desc)
            If Not MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.JTAG Then
                mem_device.GuiControl.ShowIdentButton(CBool(Not (current_devices.Count < 2)))
            End If
            Select Case counter
                Case 1
                    lbl_gang1.Text = flash_desc
                    RemoveHandler mem_device.GuiControl.SetExternalProgress, AddressOf SetGangProgress_1
                    AddHandler mem_device.GuiControl.SetExternalProgress, AddressOf SetGangProgress_1
                Case 2
                    lbl_gang2.Text = flash_desc
                    RemoveHandler mem_device.GuiControl.SetExternalProgress, AddressOf SetGangProgress_2
                    AddHandler mem_device.GuiControl.SetExternalProgress, AddressOf SetGangProgress_2
                Case 3
                    lbl_gang3.Text = flash_desc
                    RemoveHandler mem_device.GuiControl.SetExternalProgress, AddressOf SetGangProgress_3
                    AddHandler mem_device.GuiControl.SetExternalProgress, AddressOf SetGangProgress_3
                Case 4
                    lbl_gang4.Text = flash_desc
                    RemoveHandler mem_device.GuiControl.SetExternalProgress, AddressOf SetGangProgress_4
                    AddHandler mem_device.GuiControl.SetExternalProgress, AddressOf SetGangProgress_4
                Case 5
                    lbl_gang5.Text = flash_desc
                    RemoveHandler mem_device.GuiControl.SetExternalProgress, AddressOf SetGangProgress_5
                    AddHandler mem_device.GuiControl.SetExternalProgress, AddressOf SetGangProgress_5
            End Select
            Application.DoEvents()
            counter += 1
        Next
    End Sub

    Private Function MyTabs_GetDeviceInstances() As MemoryDeviceInstance()
        Try
            Dim list_out As New List(Of MemoryDeviceInstance)
            For Each page As TabPage In MyTabs.Controls
                If page Is TabStatus Then
                ElseIf page Is TabConsole Then
                Else
                    If (page.Tag IsNot Nothing) AndAlso page.Tag.GetType Is GetType(MemoryDeviceInstance) Then
                        Dim this_instance As MemoryDeviceInstance = DirectCast(page.Tag, MemoryDeviceInstance)
                        If Not list_out.Contains(this_instance) Then list_out.Add(this_instance)
                    End If
                End If
            Next
            Return list_out.ToArray
        Catch ex As Exception
        End Try
        Return Nothing
    End Function

    Public Sub AddControlToTable(ByVal tab_index As Integer, ByVal obj As Object)
        If Me.MyTabs.InvokeRequired Then
            Dim d As New cbAddToTab(AddressOf AddControlToTable)
            Me.Invoke(d, New Object() {tab_index, [obj]})
        Else
            Dim usertab As TabPage = GetUserTab(tab_index)
            If usertab Is Nothing Then Exit Sub
            Dim c As Control = CType(obj, Control)
            usertab.Controls.Add(c)
            c.BringToFront()
        End If
    End Sub

    Public Sub CreateFormTab(ByVal TabIndex As Integer, ByVal TabName As String)
        If Me.MyTabs.InvokeRequired Then
            Dim d As New cbCreateFormTab(AddressOf CreateFormTab)
            Me.Invoke(d, New Object() {TabIndex, [TabName]})
        Else
            Dim newTab As New TabPage(TabName)
            newTab.Name = "IND:" & CStr(TabIndex)
            Me.MyTabs.Controls.Add(newTab)
        End If
    End Sub

    Public Function GetUserTab(ByVal ind As Integer) As TabPage
        Dim MyObj As String = "IND:" & CStr(ind)
        Dim tP As TabPage
        For Each tP In MyTabs.Controls
            If tP.Name = MyObj Then Return tP
        Next
        Return Nothing
    End Function
    'Removes tabs created by the script
    Public Sub RemoveUserTabs()
        If Me.MyTabs.InvokeRequired Then
            Dim d As New cbRemoveAllTabs(AddressOf RemoveUserTabs)
            Me.Invoke(d)
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
        End If
    End Sub

    Private Sub MyTabs_DrawItem(sender As Object, e As System.Windows.Forms.DrawItemEventArgs) Handles MyTabs.DrawItem
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

    Private Function GetSelectedMemoryInterface() As MemoryDeviceInstance
        If Me.MyTabs.InvokeRequired Then
            Dim d As New cbGetSelectedMemoryInterface(AddressOf GetSelectedMemoryInterface)
            Return Me.Invoke(d)
        Else
            Dim o As Object = MyTabs.SelectedTab.Tag
            If o Is Nothing Then Return Nothing
            Return DirectCast(o, MemoryDeviceInstance)
        End If
    End Function

    Public Sub HandleButtons(ByVal usertabind As Integer, ByVal Enabled As Boolean, ByVal BtnName As String)
        Dim usertab As TabPage = GetUserTab(usertabind)
        If usertab Is Nothing Then Exit Sub
        For Each user_control In usertab.Controls
            If user_control.GetType Is GetType(Button) Then
                If UCase(user_control.Name) = UCase(BtnName) Or BtnName = "" Then
                    If Enabled Then
                        EnableButton(CType(user_control, Button))
                    Else
                        DisableButton(CType(user_control, Button))
                    End If
                End If
            End If
        Next
    End Sub

    Public Sub DisableButton(ByVal b As Button)
        If b.InvokeRequired Then
            Dim d As New SetBtnCallback(AddressOf DisableButton)
            Me.Invoke(d, New Object() {b})
        Else
            b.Enabled = False
        End If
    End Sub

    Public Sub EnableButton(ByVal b As Button)
        If b.InvokeRequired Then
            Dim d As New SetBtnCallback(AddressOf EnableButton)
            Me.Invoke(d, New Object() {b})
        Else
            b.Enabled = True
        End If
    End Sub

    Public Sub SetControlText(ByVal usertabind As Integer, ByVal UserControl As String, ByVal NewText As String)
        If Me.MyTabs.InvokeRequired Then
            Dim d As New cbSetControlText(AddressOf SetControlText)
            Me.Invoke(d, New Object() {usertabind, UserControl, NewText})
        Else
            Dim usertab As TabPage = GetUserTab(usertabind)
            If usertab Is Nothing Then Exit Sub
            For Each user_control In usertab.Controls
                If user_control.Name.ToString.ToUpper = UserControl.ToUpper Then
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

    Public Function GetTabObjectText(ByVal ControlName As String, ByVal TabIndex As Integer) As String
        Dim MyObj As String = "IND:" & CStr(TabIndex)
        Dim tP As TabPage
        For Each tP In MyTabs.Controls
            If tP.Name = MyObj Then
                Dim Ct As Control
                For Each Ct In tP.Controls
                    If UCase(Ct.Name) = UCase(ControlName) Then
                        Return Ct.Text
                    End If
                Next
                Return "" 'not found
            End If
        Next
        Return ""
    End Function

#End Region

#Region "Menu DropDownOpening"

    Private Sub mi_main_menu_DropDownOpening(sender As Object, e As EventArgs) Handles mi_main_menu.DropDownOpening
        If IsAnyDeviceBusy() Then
            mi_detect.Enabled = False
            mi_repeat.Enabled = False
            mi_refresh.Enabled = False
            Exit Sub
        End If
        If (USBCLIENT.Count > 0) Then
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
    End Sub

    Private Sub mi_mode_DropDownOpening(sender As Object, e As EventArgs) Handles mi_mode_menu.DropDownOpening
        mi_1V8.Checked = False
        mi_3V3.Checked = False
        mi_5V0.Checked = False
        Select Case MySettings.VOLT_SELECT
            Case FlashcatSettings.Voltage.V1_8
                mi_1V8.Checked = True
            Case FlashcatSettings.Voltage.V3_3
                mi_3V3.Checked = True
            Case FlashcatSettings.Voltage.V5_0
                mi_5V0.Checked = True
        End Select
        If IsAnyDeviceBusy() Then
            mi_mode_settings.Enabled = False
            mi_verify.Enabled = False
            mi_bit_swapping.Enabled = False
            mi_endian.Enabled = False
            mi_vpp.Enabled = False
            mi_1V8.Enabled = False
            mi_3V3.Enabled = False
            mi_5V0.Enabled = False
            mi_mode_spi.Enabled = False
            mi_mode_spieeprom.Enabled = False
            mi_mode_spi_nand.Enabled = False
            mi_mode_i2c.Enabled = False
            mi_mode_extio.Enabled = False
            mi_mode_jtag.Enabled = False
            Exit Sub
        Else
            mi_mode_settings.Enabled = True
            mi_verify.Enabled = True
            mi_bit_swapping.Enabled = True
            mi_endian.Enabled = True
        End If
        mi_mode_spi.Checked = False
        mi_mode_spieeprom.Checked = False
        mi_mode_spi_nand.Checked = False
        mi_mode_i2c.Checked = False
        mi_mode_extio.Checked = False
        mi_mode_jtag.Checked = False
        mi_bitswap_none.Checked = False
        mi_bitswap_8bit.Checked = False
        mi_bitswap_16bit.Checked = False
        mi_bitswap_32bit.Checked = False
        mi_vpp.Enabled = False
        Enable_VCC_Menu(False)
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
        mi_bitendian_big_32.Checked = False
        mi_bitendian_big_16.Checked = False
        mi_bitendian_little_16.Checked = False
        mi_bitendian_little_8.Checked = False
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
        If MySettings.I2C_SIZE = 0 Then
            mi_mode_i2c.Enabled = False
        Else
            mi_mode_i2c.Enabled = True
        End If
        If MySettings.SPI_EEPROM = SPI_EEPROM.None Then
            mi_mode_spieeprom.Enabled = False
        Else
            mi_mode_spieeprom.Enabled = True
        End If
        Enable_Mode_Menu(False)
        If USBCLIENT.HW_MODE = FCUSB_BOARD.Professional Then
            mi_mode_spi.Enabled = True
            mi_mode_spieeprom.Enabled = True
            mi_mode_spi_nand.Enabled = True
            mi_mode_i2c.Enabled = True
            mi_mode_extio.Enabled = True
            Enable_VCC_Menu(True)
        ElseIf USBCLIENT.HW_MODE = FCUSB_BOARD.Classic_XPORT Then
            mi_mode_spi.Enabled = True
            mi_mode_spieeprom.Enabled = True
            mi_mode_spi_nand.Enabled = True
            mi_mode_i2c.Enabled = True
            mi_mode_extio.Enabled = True
        ElseIf USBCLIENT.HW_MODE = FCUSB_BOARD.Classic_JTAG Then
            mi_mode_jtag.Enabled = True
        ElseIf USBCLIENT.HW_MODE = FCUSB_BOARD.Classic_SPI Then
            mi_mode_spi.Enabled = True
            mi_mode_spieeprom.Enabled = True
            mi_mode_spi_nand.Enabled = True
            mi_mode_i2c.Enabled = True
            mi_mode_extio.Enabled = True
        ElseIf USBCLIENT.HW_MODE = FCUSB_BOARD.Classic_BL Then
        ElseIf USBCLIENT.HW_MODE = FCUSB_BOARD.NotConnected Then
            Enable_VCC_Menu(True)
            Enable_Mode_Menu(True)
        End If
        Select Case MySettings.OPERATION_MODE
            Case FlashcatSettings.DeviceMode.SPI
                mi_mode_spi.Checked = True
            Case FlashcatSettings.DeviceMode.SPI_EEPROM
                mi_mode_spieeprom.Checked = True
            Case FlashcatSettings.DeviceMode.SPI_NAND
                mi_mode_spi_nand.Checked = True
            Case FlashcatSettings.DeviceMode.I2C_EEPROM
                mi_mode_i2c.Checked = True
            Case FlashcatSettings.DeviceMode.EXTIO
                mi_mode_extio.Checked = True
                mi_vpp.Enabled = True
            Case FlashcatSettings.DeviceMode.JTAG
                mi_mode_jtag.Checked = True
        End Select
    End Sub

    Private Sub Enable_VCC_Menu(ByVal enabled As Boolean)
        If enabled Then
            mi_1V8.Enabled = True
            mi_3V3.Enabled = True
            mi_5V0.Enabled = True
        Else
            mi_1V8.Enabled = False
            mi_3V3.Enabled = False
            mi_5V0.Enabled = False
        End If
    End Sub

    Private Sub Enable_Mode_Menu(ByVal enabled As Boolean)
        If enabled Then
            mi_mode_spi.Enabled = True
            mi_mode_spieeprom.Enabled = True
            mi_mode_spi_nand.Enabled = True
            mi_mode_i2c.Enabled = True
            mi_mode_extio.Enabled = True
            mi_mode_jtag.Enabled = True
        Else
            mi_mode_spi.Enabled = False
            mi_mode_spieeprom.Enabled = False
            mi_mode_spi_nand.Enabled = False
            mi_mode_i2c.Enabled = False
            mi_mode_extio.Enabled = False
            mi_mode_jtag.Enabled = False
        End If
    End Sub

    Private Sub mi_script_menu_DropDownOpening(sender As Object, e As EventArgs) Handles mi_script_menu.DropDownOpening
        Try
            If IsAnyDeviceBusy() Then
                mi_script_selected.Enabled = False
                mi_script_load.Enabled = False
                mi_script_unload.Enabled = False
            Else
                mi_script_selected.Enabled = True
                mi_script_load.Enabled = True
                mi_script_unload.Enabled = True
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub mi_tools_menu_DropDownOpening(sender As Object, e As EventArgs) Handles mi_tools_menu.DropDownOpening
        If (MyTabs.SelectedTab.Tag IsNot Nothing) AndAlso (MyTabs.SelectedTab.Tag IsNot Nothing) Then
            If TryCast(MyTabs.SelectedTab.Tag, MemoryDeviceInstance) IsNot Nothing Then 'We are on a memory device
                Dim mem_instance As MemoryDeviceInstance = DirectCast(MyTabs.SelectedTab.Tag, MemoryDeviceInstance)
                If (Not mem_instance.IsBusy) And (Not mem_instance.IsTaskRunning) Then
                    mi_erase_tool.Enabled = (Not mem_instance.ReadOnly)
                    mi_create_img.Enabled = True
                    mi_write_img.Enabled = True
                    mi_nand_map.Enabled = False
                    If mem_instance.VendorMenu Is Nothing Then
                        mi_device_features.Enabled = False
                    Else
                        mi_device_features.Enabled = True
                    End If
                    If mem_instance.FlashType = MemoryType.SLC_NAND Then
                        mi_nand_map.Enabled = True
                        Exit Sub 'Accept the above
                    ElseIf mem_instance.FlashType = MemoryType.SERIAL_NAND Then
                        mi_nand_map.Enabled = True
                        Exit Sub 'Accept the above
                    ElseIf mem_instance.FlashType = MemoryType.SERIAL_NOR Then
                        Exit Sub 'Accept the above
                    ElseIf mem_instance.FlashType = MemoryType.PARALLEL_NOR Then
                        Exit Sub 'Accept the above
                    End If
                End If
            End If
        End If
        mi_erase_tool.Enabled = False
        mi_create_img.Enabled = False
        mi_write_img.Enabled = False
        mi_nand_map.Enabled = False
        mi_device_features.Enabled = False
    End Sub

#End Region

#Region "Form Events"

    Private Delegate Sub cbOnNewDeviceConnected(ByVal usb_dev As FCUSB_DEVICE)
    Private Delegate Sub cbOperation(ByVal mem_ctrl As MemControl_v2)
    Private Delegate Sub cbCloseApplication()

    Public Sub CloseApplication()
        If Me.InvokeRequired Then
            Dim d As New cbCloseApplication(AddressOf CloseApplication)
            Me.Invoke(d)
        Else
            Me.Close()
        End If
    End Sub

    Private Sub EmbLogo_DoubleClick(sender As Object, e As EventArgs) Handles pb_logo.DoubleClick
        If Me.Cursor = Cursors.Hand Then
            Dim sInfo As New ProcessStartInfo("http://www.embeddedcomputers.net/products/FlashcatUSB_Pro/")
            Process.Start(sInfo)
        End If
    End Sub

    Private Sub EmbLogo_MouseLeave(sender As Object, e As EventArgs) Handles pb_logo.MouseLeave
        Me.Cursor = Cursors.Arrow
    End Sub

    Private Sub EmbLogo_MouseMove(sender As Object, e As MouseEventArgs) Handles pb_logo.MouseMove
        Dim x As Integer = (Me.Width / 2) - 130
        If e.X >= x AndAlso e.X <= x + 222 Then
            Me.Cursor = Cursors.Hand
        Else
            Me.Cursor = Cursors.Arrow
        End If
    End Sub

    Private Sub Main_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        AppIsClosing = True
        MySettings.Save()
    End Sub

    Private Sub cmd_console_clear_Click(sender As Object, e As EventArgs) Handles cmd_console_clear.Click
        ConsoleBox.Items.Clear()
    End Sub

    Private Sub mi_nand_map_Click(sender As Object, e As EventArgs) Handles mi_nand_map.Click
        Try
            Dim mem_dev As MemoryDeviceInstance = GetSelectedMemoryInterface()
            If mem_dev Is Nothing Then Exit Sub
            Dim n As New NAND_Block_Management(mem_dev.FCUSB)
            If mem_dev.FlashType = MemoryType.SERIAL_NAND Then
                Dim nand As SPI_NAND_Flash = mem_dev.FCUSB.SPI_NAND_IF.MyFlashDevice
                Dim pages_per_block As UInt32 = (nand.BLOCK_SIZE / nand.PAGE_SIZE)
                Dim n_layout As NAND_LAYOUT_TOOL.NANDLAYOUT_STRUCTURE = FlashMemory.NANDLAYOUT_Get(nand)
                n.SetDeviceParameters(nand.NAME, nand.PAGE_SIZE + nand.EXT_PAGE_SIZE, pages_per_block, nand.Sector_Count, n_layout)
            ElseIf mem_dev.FlashType = MemoryType.SLC_NAND Then
                Dim nand As NAND_Flash = DirectCast(mem_dev.FCUSB.EXT_IF.MyFlashDevice, NAND_Flash)
                Dim pages_per_block As UInt32 = (nand.BLOCK_SIZE / nand.PAGE_SIZE)
                Dim n_layout As NAND_LAYOUT_TOOL.NANDLAYOUT_STRUCTURE = FlashMemory.NANDLAYOUT_Get(nand)
                n.SetDeviceParameters(nand.NAME, nand.PAGE_SIZE + nand.EXT_PAGE_SIZE, pages_per_block, nand.Sector_Count, n_layout)
            End If
            n.ShowDialog()
            mem_dev.FCUSB.NAND_IF.ProcessMap()
            If mem_dev.FlashType = MemoryType.SERIAL_NAND Then
                Dim flash_available As UInt32 = mem_dev.FCUSB.SPI_NAND_IF.DeviceSize()
                mem_dev.GuiControl.InitMemoryDevice(mem_dev.FCUSB, mem_dev.Name, mem_dev.Size, MemControl_v2.access_mode.ReadWrite)
                mem_dev.GuiControl.SetupLayout()
                StatusMessages_LoadMemoryDevices()
            ElseIf mem_dev.FlashType = MemoryType.SLC_NAND Then
                Dim flash_available As UInt32 = mem_dev.FCUSB.EXT_IF.DeviceSize()
                mem_dev.GuiControl.InitMemoryDevice(mem_dev.FCUSB, mem_dev.Name, mem_dev.Size, MemControl_v2.access_mode.ReadWrite)
                mem_dev.GuiControl.SetupLayout()
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
            Case FlashcatSettings.Voltage.V1_8
                mi_1V8.Checked = True
            Case FlashcatSettings.Voltage.V3_3
                mi_3V3.Checked = True
            Case FlashcatSettings.Voltage.V5_0
                mi_5V0.Checked = True
        End Select
        Select Case MySettings.OPERATION_MODE
            Case FlashcatSettings.DeviceMode.SPI
                mi_mode_spi.Checked = True
            Case FlashcatSettings.DeviceMode.SPI_EEPROM
                mi_mode_spieeprom.Checked = True
            Case FlashcatSettings.DeviceMode.SPI_NAND
                mi_mode_spi_nand.Checked = True
            Case FlashcatSettings.DeviceMode.JTAG
                mi_mode_jtag.Checked = True
            Case FlashcatSettings.DeviceMode.I2C_EEPROM
                mi_mode_i2c.Checked = True
            Case FlashcatSettings.DeviceMode.EXTIO
                mi_mode_extio.Checked = True
        End Select
        Select Case MySettings.VPP_VCC
            Case FlashcatSettings.VPP_SETTING.Disabled
                mi_so44_normal.Checked = True
            Case FlashcatSettings.VPP_SETTING.Write_12v
                mi_so44_12v_write.Checked = True
        End Select
        If MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI_EEPROM Then
            If MySettings.SPI_EEPROM = SPI_EEPROM.None Then
                MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI
                Menu_Mode_UncheckAll()
                mi_mode_spi.Checked = True
            End If
        ElseIf MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.I2C_EEPROM Then
            If MySettings.I2C_SIZE = 0 Then
                MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI
                Menu_Mode_UncheckAll()
                mi_mode_spi.Checked = True
            End If
        End If
    End Sub

    Private Sub mi_mode_settings_Click(sender As Object, e As EventArgs) Handles mi_mode_settings.Click
        Dim f As New FrmSettings()
        f.ShowDialog()
        MySettings.Save() 'Saves all settings to registry
        If MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI Then
            For Each dev In USBCLIENT.FCUSB
                If dev.IS_CONNECTED Then
                    If dev.SPI_NOR_IF.PORT_SELECT = SPI.SPI_Programmer.SPIBUS_PORT.Port_A Then
                        Dim clock_mhz As UInt32 = GetCurrentSpiClock(dev)
                        Dim clock_str As String = (clock_mhz / 1000000).ToString & " MHz"
                        GUI.PrintConsole(String.Format(RM.GetString("spi_set_clock"), clock_str)) 'Now set clock to user selected value
                        dev.USB_SPI_SETSPEED(SPI.SPI_Programmer.SPIBUS_PORT.Port_A, clock_mhz)
                    End If
                End If
            Next
        ElseIf MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI_NAND Then
            For i = 0 To MEM_IF.DeviceCount - 1
                Dim dv As MemoryDeviceInstance = MEM_IF.GetDevice(i)
                If dv IsNot Nothing Then
                    If dv.FlashType = MemoryType.SERIAL_NAND Then
                        dv.GuiControl.SetupLayout()
                        StatusMessages_LoadMemoryDevices()
                        If MySettings.SPI_NAND_DISABLE_ECC Then
                            dv.FCUSB.SPI_NAND_IF.ECC_ENABLED = False
                        Else
                            dv.FCUSB.SPI_NAND_IF.ECC_ENABLED = True
                        End If
                    End If
                End If
            Next
        ElseIf MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.EXTIO Then
            For i = 0 To MEM_IF.DeviceCount - 1
                Dim dv As MemoryDeviceInstance = MEM_IF.GetDevice(i)
                If dv IsNot Nothing Then
                    If dv.FlashType = MemoryType.SLC_NAND Then
                        dv.GuiControl.SetupLayout()
                        StatusMessages_LoadMemoryDevices()
                    End If
                End If
            Next
        End If
        LoadSettingsIntoGui()
    End Sub

    Private Sub Menu_Mode_UncheckAll()
        Try
            mi_mode_spi.Checked = False
            mi_mode_spieeprom.Checked = False
            mi_mode_i2c.Checked = False
            mi_mode_extio.Checked = False
            mi_mode_1wire.Checked = False
            mi_mode_jtag.Checked = False
            mi_mode_spi_nand.Checked = False
        Catch ex As Exception
        End Try
    End Sub

    Private Sub mi_mode_spi_Click(sender As Object, e As EventArgs) Handles mi_mode_spi.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI
    End Sub

    Private Sub mi_mode_spi_eeprom_Click(sender As Object, e As EventArgs) Handles mi_mode_spieeprom.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI_EEPROM
    End Sub

    Private Sub mi_mode_i2c_Click(sender As Object, e As EventArgs) Handles mi_mode_i2c.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.I2C_EEPROM
    End Sub

    Private Sub mi_mode_1wire_Click(sender As Object, e As EventArgs) Handles mi_mode_1wire.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.DOW
    End Sub

    Private Sub mi_mode_jtag_Click(sender As Object, e As EventArgs) Handles mi_mode_jtag.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.JTAG
    End Sub

    Private Sub mi_mode_extio_Click(sender As Object, e As EventArgs) Handles mi_mode_extio.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.EXTIO
    End Sub

    Private Sub mi_mode_spi_nand_Click(sender As Object, e As EventArgs) Handles mi_mode_spi_nand.Click
        Menu_Mode_UncheckAll()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI_NAND
    End Sub

    Private Sub mi_verify_Click(sender As Object, e As EventArgs) Handles mi_verify.Click
        mi_verify.Checked = Not mi_verify.Checked
        MySettings.VERIFY_WRITE = mi_verify.Checked
    End Sub

    Private Sub mi_bitswap_none_Click(sender As Object, e As EventArgs) Handles mi_bitswap_none.Click
        MySettings.BIT_SWAP = BitSwapMode.None
        MEM_IF.RefreshAll()
    End Sub

    Private Sub mi_bitswap_8bit_Click(sender As Object, e As EventArgs) Handles mi_bitswap_8bit.Click
        MySettings.BIT_SWAP = BitSwapMode.Bits_8
        MEM_IF.RefreshAll()
    End Sub

    Private Sub mi_bitswap_16bit_Click(sender As Object, e As EventArgs) Handles mi_bitswap_16bit.Click
        MySettings.BIT_SWAP = BitSwapMode.Bits_16
        MEM_IF.RefreshAll()
    End Sub

    Private Sub mi_bitswap_32bit_Click(sender As Object, e As EventArgs) Handles mi_bitswap_32bit.Click
        MySettings.BIT_SWAP = BitSwapMode.Bits_32
        MEM_IF.RefreshAll()
    End Sub

    Private Sub mi_endian_big_Click(sender As Object, e As EventArgs) Handles mi_bitendian_big_32.Click
        MySettings.BIT_ENDIAN = BitEndianMode.BigEndian32
        MEM_IF.RefreshAll()
    End Sub

    Private Sub mi_bitendian_big_16_Click(sender As Object, e As EventArgs) Handles mi_bitendian_big_16.Click
        MySettings.BIT_ENDIAN = BitEndianMode.BigEndian16
        MEM_IF.RefreshAll()
    End Sub

    Private Sub mi_endian_16bit_Click(sender As Object, e As EventArgs) Handles mi_bitendian_little_16.Click
        MySettings.BIT_ENDIAN = BitEndianMode.LittleEndian32_16bit
        MEM_IF.RefreshAll()
    End Sub

    Private Sub mi_endian_8bit_Click(sender As Object, e As EventArgs) Handles mi_bitendian_little_8.Click
        MySettings.BIT_ENDIAN = BitEndianMode.LittleEndian32_8bit
        MEM_IF.RefreshAll()
    End Sub

    Private Sub mi_1V8_Click(sender As Object, e As EventArgs) Handles mi_1V8.Click
        mi_1V8.Checked = True
        mi_3V3.Checked = False
        mi_5V0.Checked = False
        MySettings.VOLT_SELECT = FlashcatSettings.Voltage.V1_8
        USBCLIENT.USB_VCC_1V8()
        MySettings.Save()
        PrintConsole(String.Format(RM.GetString("voltage_set_to"), "1.8v"))
    End Sub

    Private Sub mi_3V3_Click(sender As Object, e As EventArgs) Handles mi_3V3.Click
        mi_1V8.Checked = False
        mi_3V3.Checked = True
        mi_5V0.Checked = False
        MySettings.VOLT_SELECT = FlashcatSettings.Voltage.V3_3
        USBCLIENT.USB_VCC_3V()
        MySettings.Save()
        PrintConsole(String.Format(RM.GetString("voltage_set_to"), "3.3v"))
    End Sub

    Private Sub mi_5V0_Click(sender As Object, e As EventArgs) Handles mi_5V0.Click
        mi_1V8.Checked = False
        mi_3V3.Checked = False
        mi_5V0.Checked = True
        MySettings.VOLT_SELECT = FlashcatSettings.Voltage.V5_0
        USBCLIENT.USB_VCC_5V()
        MySettings.Save()
        PrintConsole(String.Format(RM.GetString("voltage_set_to"), "5.0v"))
    End Sub

    Private Sub mi_detect_Click(sender As Object, e As EventArgs) Handles mi_detect.Click
        detect_event()
    End Sub

    Private Sub detect_event()
        ClearStatusMessage()
        ScriptEngine.Unload() 'Unloads the device script and any objects/tabs
        RemoveStatusMessage(RM.GetString("gui_active_script"))
        MEM_IF.Clear(Nothing) 'Removes all tabs
        USBCLIENT.Disconnect_All()
    End Sub

    Private Sub mi_exit_Click(sender As Object, e As EventArgs) Handles mi_exit.Click
        Me.Close()
    End Sub

    Private Sub mi_refresh_Click(sender As Object, e As EventArgs) Handles mi_refresh.Click
        MEM_IF.RefreshAll()
    End Sub

    Private Sub mi_so44_normal_Click(sender As Object, e As EventArgs) Handles mi_so44_normal.Click
        MySettings.VPP_VCC = FlashcatSettings.VPP_SETTING.Disabled
        mi_so44_normal.Checked = True
        mi_so44_12v_write.Checked = False
    End Sub

    Private Sub mi_so44_12v_write_Click(sender As Object, e As EventArgs) Handles mi_so44_12v_write.Click
        Dim confirm_msg As String = RM.GetString("gui_vpp_warning")
        confirm_msg &= vbCrLf & vbCrLf & RM.GetString("gui_vpp_confirm")
        If MsgBox(confirm_msg, MsgBoxStyle.OkCancel, RM.GetString("gui_vpp_setting")) = MsgBoxResult.Ok Then
            MySettings.VPP_VCC = FlashcatSettings.VPP_SETTING.Write_12v
            mi_so44_normal.Checked = False
            mi_so44_12v_write.Checked = True
        End If
    End Sub

#End Region

#Region "Backup Tool"
    Private BACKUP_OPERATION_RUNNING As Boolean = False
    Private BACKUP_FILE As String = ""
    Private Delegate Sub OnButtonEnable()
    Private Delegate Sub cbPromptUserForSaveLocation(ByVal name As String)

    Private Sub CreateImage_Click(sender As Object, e As EventArgs) Handles mi_create_img.Click
        Dim t As New Threading.Thread(AddressOf CreateFlashImgThread)
        t.Name = "ImgCreatorTd"
        t.Start()
    End Sub

    Private Sub LoadImage_Click(sender As Object, e As EventArgs) Handles mi_write_img.Click
        Dim OpenMe As New OpenFileDialog
        OpenMe.AddExtension = True
        OpenMe.InitialDirectory = Application.StartupPath
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
        Dim memDev As MemoryDeviceInstance = GetSelectedMemoryInterface()
        If memDev Is Nothing Then Exit Sub
        Try
            memDev.FCUSB.USB_LEDBlink()
            Backup_Start()
            If (MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.EXTIO) AndAlso (memDev.FCUSB.EXT_IF.MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND) Then
                PrintNandFlashDetails(memDev)
                NANDBACKUP_CreateBackup(memDev)
            ElseIf (MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI_NAND) Then
                PrintNandFlashDetails(memDev)
                NANDBACKUP_CreateBackup(memDev)
            Else 'normal flash
                Dim flash_size As UInt32 = memDev.Size
                Dim flash_data(flash_size) As Byte 'We need to get TOTAL size
                Dim bytes_left As UInt32 = flash_data.Length
                Dim base_addr As UInt32 = 0
                Do While (bytes_left > 0)
                    Dim packet_size As UInt32 = Math.Min(Kb512, bytes_left)
                    Dim packet() As Byte = memDev.ReadFlash(base_addr, packet_size, FlashArea.All) 'ALL in case of 
                    Array.Copy(packet, 0, flash_data, base_addr, packet_size)
                    base_addr += packet_size
                    bytes_left -= packet_size
                    Dim Percent As Integer = Math.Round(((flash_data.Length - bytes_left) / flash_data.Length) * 100)
                    memDev.GuiControl.SetProgress(Percent)
                    SetStatus(String.Format(RM.GetString("gui_reading_flash"), Format(base_addr, "#,###"), Format(flash_data.Length, "#,###"), Percent))
                Loop
                PromptUserForSaveLocation(memDev.Name)
                If BACKUP_FILE = "" Then Exit Sub
                Dim FlashOutputFile As New IO.FileInfo(BACKUP_FILE)
                If FlashOutputFile.Exists Then FlashOutputFile.Delete()
                Dim NandDumpArchive As New ZipHelper(FlashOutputFile)
                NandDumpArchive.AddFile("Main.bin", flash_data)
                NandDumpArchive.Dispose()
                memDev.GuiControl.SetProgress(0)
                SetStatus(String.Format(RM.GetString("gui_img_saved_to_disk"), FlashOutputFile.Name))
            End If
        Catch ex As Exception
        Finally
            memDev.FCUSB.USB_LEDOn()
            If memDev IsNot Nothing Then memDev.GuiControl.SetProgress(0)
            Backup_Stop()
        End Try
    End Sub

    Private Sub LoadFlashImgThread()
        Dim mem_dev As MemoryDeviceInstance = GetSelectedMemoryInterface()
        If mem_dev Is Nothing Then Exit Sub
        Try
            mem_dev.FCUSB.USB_LEDBlink()
            Backup_Start()
            If MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.EXTIO AndAlso mem_dev.FCUSB.EXT_IF.MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
                NANDBACKUP_LoadBackup(mem_dev)
            ElseIf MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI_NAND Then
                NANDBACKUP_LoadBackup(mem_dev)
            Else
                Dim flash_size As UInt32 = mem_dev.Size
                Dim sector_count As UInt32 = mem_dev.GetSectorCount
                Dim bytes_left As UInt32 = flash_size
                Dim base_addr As UInt32 = 0
                Dim FlashBin As New IO.FileInfo(BACKUP_FILE)
                Using FlashImg = New ZipHelper(FlashBin)
                    If FlashImg Is Nothing OrElse FlashImg.Count = 0 Then
                        SetStatus(RM.GetString("gui_not_valid_img"))
                        Exit Sub
                    End If
                    Dim main_io As IO.Stream = FlashImg.GetFileStream("Main.bin")
                    Using nand_main As IO.BinaryReader = New IO.BinaryReader(main_io)
                        For i = 0 To sector_count - 1
                            Dim sector_size As UInt32 = mem_dev.GetSectorSize(i, FlashArea.Main) 'change?
                            Dim data_in() As Byte = nand_main.ReadBytes(sector_size)
                            Dim WriteSuccess As Boolean = mem_dev.WriteBytes(base_addr, data_in, FlashArea.Main)
                            bytes_left -= sector_size
                            base_addr += sector_size
                            Dim Percent As Integer = Math.Round(((flash_size - bytes_left) / flash_size) * 100)
                            mem_dev.GuiControl.SetProgress(Percent)
                            SetStatus(String.Format(RM.GetString("gui_writing_flash"), Format(base_addr, "#,###"), Format(flash_size, "#,###"), Percent))
                        Next
                    End Using
                    main_io.Dispose()
                End Using
                mem_dev.GuiControl.SetProgress(0)
                SetStatus(RM.GetString("gui_img_successful"))
            End If
        Catch ex As Exception
        Finally
            mem_dev.FCUSB.USB_LEDOn()
            Backup_Stop()
        End Try
    End Sub

    Private Sub Backup_Start()
        If Me.InvokeRequired Then
            Dim d As New OnButtonEnable(AddressOf Backup_Start)
            Me.Invoke(d)
        Else
            MEM_IF.DisabledControls(True)
            BACKUP_OPERATION_RUNNING = True
        End If
    End Sub

    Private Sub Backup_Stop()
        If Me.InvokeRequired Then
            Dim d As New OnButtonEnable(AddressOf Backup_Stop)
            Me.Invoke(d)
        Else
            MEM_IF.EnableControls()
            MEM_IF.RefreshAll()
            BACKUP_OPERATION_RUNNING = False
        End If
    End Sub

    Private Sub PrintNandFlashDetails(ByVal mem_dev As MemoryDeviceInstance)
        WriteConsole(RM.GetString("gui_creating_nand_file"))
        WriteConsole("Memory device name: " & mem_dev.FCUSB.EXT_IF.MyFlashDevice.NAME)
        WriteConsole("Flash size: " & Format(mem_dev.FCUSB.EXT_IF.MyFlashDevice.FLASH_SIZE, "#,###") & " bytes")
        WriteConsole("Extended/Spare area: " & Format(mem_dev.FCUSB.NAND_IF.Extra_GetSize(), "#,###") & " bytes")
        WriteConsole("Page size: " & Format(mem_dev.FCUSB.EXT_IF.MyFlashDevice.PAGE_SIZE, "#,###") & " bytes")
        WriteConsole("Block size: " & Format(DirectCast(mem_dev.FCUSB.EXT_IF.MyFlashDevice, NAND_Flash).BLOCK_SIZE, "#,###") & " bytes")
    End Sub

    Private Function GetNandCfgParam(ByVal ParamName As String, ByVal File() As String) As String
        For Each line In File
            If line.StartsWith("[" & ParamName & "]") Then
                Dim x As Integer = ParamName.Length + 2
                Return line.Substring(x)
            End If
        Next
        Return ""
    End Function

    Private Function GetNandCfgParams(ByVal ParamName As String, ByVal File() As String) As String()
        Dim out As New List(Of String)
        For Each line In File
            If line.StartsWith("[" & ParamName & "]") Then
                Dim x As Integer = ParamName.Length + 2
                out.Add(line.Substring(x))
            End If
        Next
        Return out.ToArray
    End Function

    Private Sub PromptUserForSaveLocation(ByVal default_name As String)
        If Me.InvokeRequired Then
            Dim d As New cbPromptUserForSaveLocation(AddressOf PromptUserForSaveLocation)
            Me.Invoke(d, {default_name})
        Else
            Dim SaveMe As New SaveFileDialog
            SaveMe.AddExtension = True
            SaveMe.InitialDirectory = Application.StartupPath
            SaveMe.Title = RM.GetString("gui_select_location")
            SaveMe.CheckPathExists = True
            SaveMe.FileName = default_name
            Dim FcFname As String = RM.GetString("gui_compressed_img") & " (*.zip)|*.zip"
            Dim AllF As String = "All files (*.*)|*.*"
            SaveMe.Filter = FcFname & "|" & AllF
            If SaveMe.ShowDialog = DialogResult.OK Then
                BACKUP_FILE = SaveMe.FileName
            Else
                BACKUP_FILE = ""
            End If
        End If
    End Sub

    Private Sub NANDBACKUP_CreateBackup(ByVal mem_dev As MemoryDeviceInstance)
        Try
            mem_dev.IsTaskRunning = True
            Dim double_read As Boolean = MySettings.NAND_Verify
            Dim flash_name As String
            Dim flash_size As UInt32
            Dim page_size As UInt32
            Dim oob_size As UInt32
            Dim block_count As UInt32
            Dim block_size As UInt32
            Dim chipid As String
            If (MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.EXTIO) Then
                Dim m As NAND_Flash = DirectCast(mem_dev.FCUSB.EXT_IF.MyFlashDevice, NAND_Flash)
                flash_name = m.NAME
                chipid = Hex(m.MFG_CODE).PadLeft(2, "0") & Hex(m.ID1).PadLeft(4, "0") & Hex(m.ID2).PadLeft(4, "0")
                flash_size = m.FLASH_SIZE
                page_size = m.PAGE_SIZE
                oob_size = m.EXT_PAGE_SIZE
                block_count = m.Sector_Count
                block_size = m.BLOCK_SIZE
            ElseIf (MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI_NAND) Then
                Dim m As SPI_NAND_Flash = DirectCast(mem_dev.FCUSB.SPI_NAND_IF.MyFlashDevice, SPI_NAND_Flash)
                flash_name = m.NAME
                chipid = Hex(m.MFG_CODE).PadLeft(2, "0") & Hex(m.ID1).PadLeft(4, "0")
                flash_size = m.FLASH_SIZE
                page_size = m.PAGE_SIZE
                oob_size = m.EXT_PAGE_SIZE
                block_count = m.Sector_Count
                block_size = m.BLOCK_SIZE
            Else
                Exit Sub
            End If
            PromptUserForSaveLocation(flash_name)
            Dim NandOutputFile As New IO.FileInfo(BACKUP_FILE)
            If NandOutputFile.Exists Then NandOutputFile.Delete()
            Dim NandDumpArchive As New ZipHelper(NandOutputFile)
            Dim InfoFile As New List(Of String)
            InfoFile.Add("[MEMORY_DEVICE]" & flash_name)
            InfoFile.Add("[ID]" & chipid)
            InfoFile.Add("[FLASH_SIZE]" & flash_size.ToString)
            InfoFile.Add("[BLOCK_SIZE]" & block_size.ToString)
            InfoFile.Add("[BLOCK_COUNT]" & block_count.ToString)
            InfoFile.Add("[PAGE_SIZE]" & page_size.ToString)
            InfoFile.Add("[OOB_SIZE]" & oob_size.ToString)
            NandDumpArchive.AddFile("NAND.cfg", Utilities.Bytes.FromCharStringArray(InfoFile.ToArray))
            Dim pages_per_block As UInt32 = (block_size / page_size)
            Dim page_size_total As UInt32 = (page_size + oob_size)
            Dim block_total As UInt32 = pages_per_block * (page_size_total)
            Dim page_addr As UInt32 = 0
            For i = 0 To block_count - 1
                If mem_dev.GuiControl.USER_HIT_CANCEL Then
                    SetStatus(RM.GetString("mc_mem_read_canceled"))
                    NandDumpArchive.Dispose()
                    Exit Sub
                End If
                Dim Percent As Integer = Math.Round(((i + 1) / block_count) * 100)
                mem_dev.GuiControl.SetProgress(Percent)
                SetStatus(String.Format(RM.GetString("nand_reading_block"), Format((i + 1), "#,###"), Format(block_count, "#,###"), Percent))
                Dim block_data_1(block_total - 1) As Byte
                Dim block_data_2(block_total - 1) As Byte
                If (MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.EXTIO) Then
                    mem_dev.FCUSB.EXT_IF.NAND_ReadPages(page_addr, 0, block_data_1.Length, FlashArea.All, block_data_1)
                    If double_read Then mem_dev.FCUSB.EXT_IF.NAND_ReadPages(page_addr, 0, block_data_2.Length, FlashArea.All, block_data_2)
                ElseIf (MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI_NAND) Then
                    mem_dev.FCUSB.SPI_NAND_IF.NAND_ReadPages(page_addr, 0, block_data_1.Length, FlashArea.All, block_data_1)
                    If double_read Then mem_dev.FCUSB.SPI_NAND_IF.NAND_ReadPages(page_addr, 0, block_data_2.Length, FlashArea.All, block_data_2)
                End If
                If double_read Then
                    If Not Utilities.ArraysMatch(block_data_1, block_data_2) Then
                        SetStatus(RM.GetString("gui_nand_creating_backup")) 'Error creating backup: read memory returned inconsistance results
                        NandDumpArchive.Dispose()
                        Exit Sub
                    End If
                End If
                page_addr += pages_per_block
                NandDumpArchive.AddFile("BLOCK_" & i.ToString.PadLeft(4, "0"), block_data_1)
            Next
            NandDumpArchive.Dispose()
            mem_dev.GuiControl.SetProgress(0)
            SetStatus(String.Format(RM.GetString("gui_saved_img_to_disk"), NandOutputFile.Name)) 'Saved Flash image to disk: {0}
        Catch ex As Exception
        Finally
            mem_dev.IsTaskRunning = False
        End Try
    End Sub

    Private Sub NANDBACKUP_LoadBackup(ByVal mem_dev As MemoryDeviceInstance)
        Try
            Dim NameFileLocation As New IO.FileInfo(BACKUP_FILE)
            Using NandImage = New ZipHelper(NameFileLocation)
                If NandImage Is Nothing OrElse NandImage.Count = 0 Then
                    SetStatus(RM.GetString("gui_not_valid_img")) : Exit Sub
                End If
                Dim cfg_io As IO.Stream = NandImage.GetFileStream("NAND.cfg")
                If cfg_io Is Nothing Then
                    SetStatus(RM.GetString("gui_not_valid_img")) : Exit Sub
                End If
                Dim config_file As New List(Of String)
                Using nand_cfg As IO.StreamReader = New IO.StreamReader(cfg_io)
                    Do Until nand_cfg.Peek = -1
                        config_file.Add(nand_cfg.ReadLine)
                    Loop
                    nand_cfg.Close()
                End Using
                cfg_io.Dispose()
                WriteConsole(String.Format(RM.GetString("gui_programming_img"), GetNandCfgParam("MEMORY_DEVICE", config_file.ToArray))) 'Programming Flash image: {0}
                Dim flash_name As String = GetNandCfgParam("MEMORY_DEVICE", config_file.ToArray)
                Dim flash_size As UInt32 = GetNandCfgParam("FLASH_SIZE", config_file.ToArray)
                Dim page_size As UInt32 = GetNandCfgParam("PAGE_SIZE", config_file.ToArray)
                Dim oob_size As UInt32 = GetNandCfgParam("OOB_SIZE", config_file.ToArray)
                Dim block_count As UInt32 = GetNandCfgParam("BLOCK_COUNT", config_file.ToArray)
                Dim block_size As UInt32 = GetNandCfgParam("BLOCK_SIZE", config_file.ToArray)
                Dim chipid As String = GetNandCfgParam("ID", config_file.ToArray)
                Dim pages_per_block As UInt32 = (block_size / page_size)
                Dim page_size_total As UInt32 = (page_size + oob_size)
                Dim block_total As UInt32 = pages_per_block * (page_size_total)
                Dim blocks_left As UInt32 = block_count
                Dim total_pages As UInt32 = (flash_size / page_size)
                Dim page_addr As UInt32 = 0 'Target page address
                Dim block_index As UInt32 = 0
                While (blocks_left > 0)
                    If mem_dev.GuiControl.USER_HIT_CANCEL Then
                        SetStatus(RM.GetString("mc_wr_user_canceled"))
                        Exit Sub
                    End If
                    Dim block_io As IO.Stream = NandImage.GetFileStream("BLOCK_" & block_index.ToString.PadLeft(4, "0"))
                    Dim block_data() As Byte = Nothing
                    Try
                        Using nand_main As IO.BinaryReader = New IO.BinaryReader(block_io)
                            block_data = nand_main.ReadBytes(block_total)
                        End Using
                    Catch ex As Exception
                        SetStatus(String.Format(RM.GetString("nand_missing_block"), block_index))
                        Exit Sub
                    End Try
                    block_io.Dispose()
                    If NANDBACKUP_IsValid(block_data, page_size, oob_size) Then
                        Dim Percent As Integer = Math.Round(((block_index + 1) / block_count) * 100)
                        mem_dev.GuiControl.SetProgress(Percent)
                        SetStatus(String.Format(RM.GetString("nand_writing_block"), Format(block_index, "#,###"), Format(block_count, "#,###"), Percent))
                        mem_dev.FCUSB.NAND_IF.ERASEBLOCK(page_addr, FlashArea.All, False) 'We are going to erase the entire block and not make a copy
                        mem_dev.FCUSB.NAND_IF.WRITEPAGE(page_addr, block_data, FlashArea.All) 'We are going to write the entire block (with spare)
                        If MySettings.VERIFY_WRITE Then 'Lets read back and compare
                            SetStatus(RM.GetString("mem_verify_data"))
                            Dim verify_data() As Byte = mem_dev.FCUSB.NAND_IF.READPAGE(page_addr, 0, block_total, FlashArea.All) 'Read back the entire page
                            If Utilities.ArraysMatch(block_data, verify_data) Then
                                SetStatus(RM.GetString("mem_verify_okay"))
                            Else
                                SetStatus(RM.GetString("mem_verify_failed"))
                                PrintConsole(String.Format(RM.GetString("mem_bad_nand_block"), Hex(page_addr).PadLeft(6, "0"), block_index))
                                If MySettings.NAND_MismatchSkip Then 'Bad block (We will write this same block to the next page)
                                    blocks_left = blocks_left + 1 'This will cause the next block to be written instead
                                Else
                                    Dim block_addr As UInt32 = (page_addr * page_size)
                                    Dim TitleTxt As String = String.Format(RM.GetString("mem_verify_failed_at"), Hex(block_addr).PadLeft(8, "0"))
                                    TitleTxt &= vbCrLf & vbCrLf & RM.GetString("mem_ask_continue")
                                    If MsgBox(TitleTxt, MsgBoxStyle.YesNo, RM.GetString("mem_verify_failed_title")) = MsgBoxResult.No Then
                                        SetStatus(RM.GetString("mc_wr_user_canceled"))
                                        Exit Sub
                                    End If
                                End If
                            End If
                        End If
                        page_addr += pages_per_block
                    End If
                    blocks_left = blocks_left - 1
                    block_index += 1
                    If (page_addr = total_pages) Then
                        PrintConsole(String.Format(RM.GetString("nand_completed_with_blocks_left"), blocks_left.ToString))
                        Exit While
                    End If
                End While
                mem_dev.GuiControl.SetProgress(0)
                SetStatus(RM.GetString("gui_img_successful"))
            End Using
        Catch ex As Exception
        End Try
    End Sub
    'This uses the current settings to indicate if this block is valid
    Private Function NANDBACKUP_IsValid(ByVal block_data() As Byte, ByVal page_size As UInt32, ByVal oob_size As UInt32) As Boolean
        If MySettings.NAND_BadBlockManager = FlashcatSettings.BadBlockMode.Disabled Then Return True
        Dim layout_main As UInt32
        Dim layout_oob As UInt32
        If MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Seperated Then
            layout_main = page_size
            layout_oob = oob_size
        ElseIf MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Combined Then
            layout_main = page_size
            layout_oob = oob_size
        ElseIf MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Segmented Then
            Select Case page_size
                Case 2048
                    layout_main = (page_size / 4)
                    layout_oob = (oob_size / 4)
                Case Else
                    layout_main = page_size
                    layout_oob = oob_size
            End Select
        End If
        Dim oob As New List(Of Byte()) 'contains oob
        Dim bytes_left As Byte = block_data.Length
        Dim ptr As UInt32 = 0
        Do Until bytes_left = 0
            Dim p(layout_oob - 1) As Byte
            Array.Copy(block_data, ptr + layout_oob, p, 0, p.Length)
            oob.Add(p)
            ptr += layout_main + layout_oob
            bytes_left = bytes_left - layout_main + layout_oob
        Loop
        Dim page_one() As Byte = oob(0)
        Dim page_two() As Byte = oob(1)
        Dim page_last() As Byte = oob(oob.Count - 1)
        Dim valid_block As Boolean = True
        Dim markers As Integer = MySettings.NAND_BadBlockMarkers
        If (markers And FlashcatSettings.BadBlockMarker._1stByte_FirstPage) > 0 Then
            If Not ((page_one(0)) = 255) Then valid_block = False
        End If
        If (markers And FlashcatSettings.BadBlockMarker._1stByte_SecondPage) > 0 Then
            If Not ((page_two(0)) = 255) Then valid_block = False
        End If
        If (markers And FlashcatSettings.BadBlockMarker._1stByte_LastPage) > 0 Then
            If Not ((page_last(0)) = 255) Then valid_block = False
        End If
        If (markers And FlashcatSettings.BadBlockMarker._6thByte_FirstPage) > 0 Then
            If Not ((page_one(5)) = 255) Then valid_block = False
        End If
        If (markers And FlashcatSettings.BadBlockMarker._6thByte_SecondPage) > 0 Then
            If Not ((page_two(5)) = 255) Then valid_block = False
        End If
        Return valid_block
    End Function

    Private Function IsValidAddress(ByVal base As Long, ByRef list() As Long) As Boolean
        For Each addr In list
            If addr = base Then Return False
        Next
        Return True
    End Function

#End Region

#Region "Status Bar"
    Private StatusBar_IsNormal As Boolean = True
    Private Delegate Sub cbStatusBar_SwitchToOperation(ByVal labels() As ToolStripStatusLabel)
    Private Delegate Sub cbStatusBar_SwitchToNormal()

    Public Sub StatusBar_SwitchToOperation(ByVal labels() As ToolStripStatusLabel)
        If Me.InvokeRequired Then
            Dim d As New cbStatusBar_SwitchToOperation(AddressOf StatusBar_SwitchToOperation)
            Me.Invoke(d)
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
            Dim d As New cbStatusBar_SwitchToNormal(AddressOf StatusBar_SwitchToNormal)
            Me.Invoke(d)
        Else
            While FlashStatus.Items.Count > 1
                FlashStatus.Items.RemoveAt(1)
            End While
            FlashStatusLabel.Visible = True
            StatusBar_IsNormal = True
        End If
    End Sub

    Public Sub SetStatus(ByVal Msg As String)
        If Me.InvokeRequired Then
            Dim d As New cbSetStatus(AddressOf SetStatus)
            Me.Invoke(d, New Object() {[Msg]})
        Else
            Me.FlashStatusLabel.Text = Msg
            Application.DoEvents()
        End If
    End Sub

    Public Sub OperationStarted(mem_ctrl As MemControl_v2)
        If Me.InvokeRequired Then
            Dim d As New cbOperation(AddressOf OperationStarted)
            Me.Invoke(d, {mem_ctrl})
        Else
            Dim sel_tab As TabPage = MyTabs.SelectedTab
            If (sel_tab.Tag IsNot Nothing) AndAlso sel_tab.Tag.GetType Is GetType(MemoryDeviceInstance) Then
                Dim this_instance As MemoryDeviceInstance = DirectCast(sel_tab.Tag, MemoryDeviceInstance)
                If this_instance.GuiControl Is mem_ctrl Then
                    StatusBar_SwitchToOperation(mem_ctrl.StatusLabels)
                End If
            End If
        End If
    End Sub

    Public Sub OperationStopped(mem_ctrl As MemControl_v2)
        If Me.InvokeRequired Then
            Dim d As New cbOperation(AddressOf OperationStopped)
            Me.Invoke(d, {mem_ctrl})
        Else
            Dim sel_tab As TabPage = MyTabs.SelectedTab
            If (sel_tab.Tag IsNot Nothing) AndAlso sel_tab.Tag.GetType Is GetType(MemoryDeviceInstance) Then
                Dim this_instance As MemoryDeviceInstance = DirectCast(sel_tab.Tag, MemoryDeviceInstance)
                If this_instance.GuiControl Is mem_ctrl Then
                    StatusBar_SwitchToNormal()
                End If
            End If
        End If
    End Sub

    Private Sub MyTabs_SelectedIndexChanged(sender As Object, e As EventArgs) Handles MyTabs.SelectedIndexChanged
        Dim sel_tab As TabPage = MyTabs.SelectedTab
        If (sel_tab.Tag IsNot Nothing) AndAlso sel_tab.Tag.GetType Is GetType(MemoryDeviceInstance) Then
            Dim this_instance As MemoryDeviceInstance = DirectCast(sel_tab.Tag, MemoryDeviceInstance)
            If this_instance.GuiControl IsNot Nothing Then
                If this_instance.GuiControl.IN_OPERATION Then
                    StatusBar_SwitchToOperation(this_instance.GuiControl.StatusLabels)
                ElseIf Not StatusBar_IsNormal Then
                    StatusBar_SwitchToNormal()
                End If
            End If
        ElseIf Not StatusBar_IsNormal Then
            StatusBar_SwitchToNormal()
        End If
    End Sub

#End Region

#Region "Script Menu"

    Public Sub LoadScripts(ByVal JEDEC_ID As UInt32)
        mi_script_selected.DropDownItems.Clear()
        WriteConsole(RM.GetString("gui_script_checking"))
        Dim MyScripts(,) As String = GetCompatibleScripts(JEDEC_ID)
        Dim SelectScript As Integer = 0
        Dim i As Integer
        If MyScripts Is Nothing Then
            WriteConsole(RM.GetString("gui_script_non_available"))
        ElseIf (MyScripts.Length / 2) = 1 Then
            WriteConsole(String.Format(RM.GetString("gui_script_loading"), MyScripts(0, 0)))
            If ScriptEngine.LoadFile(New IO.FileInfo(ScriptPath & MyScripts(0, 0))) Then
                GUI.UpdateStatusMessage(RM.GetString("gui_active_script"), MyScripts(0, 1))
                mi_script_selected.Enabled = True
                mi_script_load.Enabled = True
                mi_script_unload.Enabled = True
                Dim tsi As ToolStripMenuItem = mi_script_selected.DropDownItems.Add(MyScripts(0, 0))
                tsi.Tag = MyScripts(0, 0)
                AddHandler tsi.Click, AddressOf LoadSelectedScript
                tsi.Checked = True
            End If
        Else 'Multiple scripts (choose preferrence)
            mi_script_selected.Enabled = True
            mi_script_load.Enabled = True
            mi_script_unload.Enabled = True
            For i = 0 To CInt((MyScripts.Length / 2) - 1)
                Dim tsi As ToolStripMenuItem = mi_script_selected.DropDownItems.Add(MyScripts(i, 1))
                tsi.Tag = MyScripts(i, 0)
                AddHandler tsi.Click, AddressOf LoadSelectedScript
                If SelectScript = i Then
                    tsi.Checked = True
                End If
            Next
            UpdateStatusMessage(RM.GetString("gui_active_script"), MyScripts(SelectScript, 0))
            Dim df As New IO.FileInfo(ScriptPath & MyScripts(SelectScript, 0))
            ScriptEngine.LoadFile(df)
        End If
    End Sub

    Private Function GetCompatibleScripts(ByVal CPUID As UInteger) As String(,)
        Dim Autorun As New IO.FileInfo(ScriptPath & "autorun.ini")
        If Autorun.Exists Then
            Dim autoscripts(,) As String = Nothing
            If ProcessAutorun(Autorun, CPUID, autoscripts) Then
                Return autoscripts
            End If
        End If
        Return Nothing
    End Function

    Private Function ProcessAutorun(ByVal Autorun As IO.FileInfo, ByVal ID As UInteger, ByRef scripts(,) As String) As Boolean
        Try
            Dim f() As String = Utilities.FileIO.ReadFile(Autorun.FullName)
            Dim autoline() As String
            Dim sline As String
            Dim MyCode As UInteger
            Dim out As New ArrayList 'Holds str()
            For Each sline In f
                sline = Trim(Utilities.RemoveComment(sline))
                If Not sline = "" Then
                    autoline = sline.Split(CChar(":"))
                    If autoline.Length = 3 Then
                        MyCode = Utilities.HexToUInt(autoline(0))
                        If MyCode = ID Then
                            out.Add(New String() {autoline(1), autoline(2)})
                        End If
                    End If
                End If
            Next
            If out.Count > 0 Then
                Dim ret(out.Count - 1, 1) As String
                Dim i As Integer
                Dim s() As String
                For i = 0 To out.Count - 1
                    s = CType(out(i), String())
                    ret(i, 0) = s(0)
                    ret(i, 1) = s(1)
                Next
                scripts = ret
                Return True 'Scripts are available
            End If
        Catch ex As Exception
        End Try
        Return False
    End Function

    Private Sub mi_script_unload_Click(sender As Object, e As EventArgs) Handles mi_script_unload.Click
        Try
            ScriptEngine.Unload()
            RemoveScriptChecks()
            RemoveStatusMessage(RM.GetString("gui_active_script"))
            StatusMessages_LoadMemoryDevices()
            SetStatus(RM.GetString("gui_script_reset"))
        Catch ex As Exception
        End Try
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
            If OpenMe.ShowDialog = DialogResult.OK Then
                LoadScriptFile(OpenMe.FileName)
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub LoadScriptFile(ByVal scriptName As String)
        Dim f As New IO.FileInfo(scriptName)
        If f.Exists Then
            ScriptEngine.Unload()
            If ScriptEngine.LoadFile(f) Then
                UpdateStatusMessage(RM.GetString("gui_active_script"), f.Name)
                SetStatus(String.Format(RM.GetString("gui_script_loaded"), f.Name)) 'RM.GetString("fcusb_script_loaded")
                mi_script_unload.Enabled = True
            Else
                RemoveStatusMessage(RM.GetString("gui_active_script"))
            End If
        Else
            SetStatus(RM.GetString("gui_script_can_not_load"))
        End If
    End Sub

    Private Sub LoadSelectedScript(ByVal sender As Object, ByVal e As EventArgs)
        Dim tsi As ToolStripMenuItem = sender
        If Not tsi.Checked Then
            Dim ScriptName As String = tsi.Tag
            LoadScriptFile(Application.StartupPath & "\Scripts\" & ScriptName)
            RemoveScriptChecks()
            tsi.Checked = True
        End If
    End Sub

    Private Sub RemoveScriptChecks()
        Dim tsi As ToolStripMenuItem
        For Each tsi In mi_script_selected.DropDownItems
            tsi.Checked = False
        Next
    End Sub

#End Region

#Region "GANG MODE"

    Private Sub cmd_gang_write_Click(sender As Object, e As EventArgs) Handles cmd_gang_write.Click
        If IsAnyDeviceBusy() Then
            MsgBox(RM.GetString("gui_gang_device_busy"), MsgBoxStyle.Information, "FlashcatUSB Multi-IO")
            Exit Sub
        End If
        Dim src_file As IO.FileInfo = Nothing
        Dim max_prog_size As UInt32 = 0
        Dim smallest_flash As UInt32 = &HFFFFFFFFUI
        For i = 0 To MEM_IF.DeviceCount - 1
            smallest_flash = Math.Min(MEM_IF.GetDevice(i).Size, smallest_flash)
        Next
        If OpenFileForMultiProg(src_file) Then
            Dim dbox As New MemControl_v2.DynamicRangeBox
            Dim FileSize As UInt32 = src_file.Length
            Dim BaseAddress As UInt32 = 0 'The starting address to write the data
            If dbox.ShowRangeBox(BaseAddress, FileSize, smallest_flash) Then
                For i = 0 To MEM_IF.DeviceCount - 1
                    Dim x As New MemControl_v2.XFER_Operation
                    x.FileName = src_file
                    x.DataStream = src_file.OpenRead
                    x.Offset = BaseAddress
                    x.Size = FileSize
                    Dim t As New Threading.Thread(AddressOf MEM_IF.GetDevice(i).GuiControl.WriteMemoryThread)
                    t.Name = "memWriteTd"
                    t.Start(x)
                Next
                Dim td As New Threading.Thread(AddressOf GANGMODE_WriteAll)
                td.Name = "gangWriteAll"
                td.Start()
            End If
        End If
    End Sub

    Private Sub cmd_gang_erase_Click(sender As Object, e As EventArgs) Handles cmd_gang_erase.Click
        If IsAnyDeviceBusy() Then
            MsgBox(RM.GetString("gui_gang_device_busy"), MsgBoxStyle.Information, "FlashcatUSB Multi-IO")
            Exit Sub
        End If
        If MsgBox(RM.GetString("gui_gand_erase_all_confirm"), MsgBoxStyle.YesNo, "Confirm") = MsgBoxResult.Yes Then
            Dim td As New Threading.Thread(AddressOf GANGMODE_EraseAll)
            td.Name = "gangEraseAll"
            td.Start()
        End If
    End Sub

    Private Sub GANGMODE_EraseAll()
        Try
            SetStatus(RM.GetString("gui_gang_erasing"))
            GANGMODE_Buttons(False)
            For i = 0 To MEM_IF.DeviceCount - 1
                Dim erasetd As New Threading.Thread(AddressOf MEM_IF.GetDevice(i).EraseFlash)
                erasetd.Start()
            Next
            For i = 0 To MEM_IF.DeviceCount - 1
                Do While MEM_IF.GetDevice(i).IsBusy
                    Utilities.Sleep(100)
                Loop
            Next
            Utilities.Sleep(500)
            For i = 0 To MEM_IF.DeviceCount - 1
                MEM_IF.GetDevice(i).GuiControl.RefreshView()
            Next
            SetStatus(RM.GetString("gui_gang_erase_complete"))
        Catch ex As Exception
        Finally
            GANGMODE_Buttons(True)
        End Try
    End Sub

    Private Sub GANGMODE_WriteAll()
        Try
            GANGMODE_Buttons(False)
            Application.DoEvents()
            Utilities.Sleep(200) 'Wait for an operation to start
            Do While IsAnyDeviceBusy()
                Application.DoEvents()
                Utilities.Sleep(250)
            Loop
            SetStatus(RM.GetString("gui_gang_devices_programmed"))
        Catch ex As Exception
        Finally
            GANGMODE_Buttons(True)
        End Try
    End Sub

    Private Delegate Sub CbGangButtons(ByVal enabled As Boolean)

    Private Sub GANGMODE_Buttons(ByVal Enabled As Boolean)
        If Me.InvokeRequired Then
            Dim n As New CbGangButtons(AddressOf GANGMODE_Buttons)
            Me.Invoke(n, {Enabled})
        Else
            cmd_gang_write.Enabled = Enabled
            cmd_gang_erase.Enabled = Enabled
        End If
    End Sub

    Private Function OpenFileForMultiProg(ByRef file As IO.FileInfo) As Boolean
        Dim BinFile As String = "Binary files (*.bin)|*.bin"
        Dim AllFiles As String = "All files (*.*)|*.*"
        Dim OpenMe As New OpenFileDialog
        OpenMe.AddExtension = True
        OpenMe.InitialDirectory = Application.StartupPath
        OpenMe.Title = RM.GetString("gui_gang_choose_binary")
        OpenMe.CheckPathExists = True
        OpenMe.Filter = BinFile & "|" & AllFiles 'Bin Files, All Files
        If OpenMe.ShowDialog = DialogResult.OK Then
            file = New IO.FileInfo(OpenMe.FileName)
            Return True
        Else
            Return False
        End If
    End Function

    Private Delegate Sub cbSetGangProgress_1(ByVal percent As Integer)

    Private Sub SetGangProgress_1(ByVal percent As Integer)
        If Me.InvokeRequired Then
            Dim n As New CbGangButtons(AddressOf SetGangProgress_1)
            Me.Invoke(n, {percent})
        Else
            pb_gang1.Value = percent
        End If
    End Sub

    Private Sub SetGangProgress_2(ByVal percent As Integer)
        If Me.InvokeRequired Then
            Dim n As New CbGangButtons(AddressOf SetGangProgress_2)
            Me.Invoke(n, {percent})
        Else
            pb_gang2.Value = percent
        End If
    End Sub

    Private Sub SetGangProgress_3(ByVal percent As Integer)
        If Me.InvokeRequired Then
            Dim n As New CbGangButtons(AddressOf SetGangProgress_3)
            Me.Invoke(n, {percent})
        Else
            pb_gang3.Value = percent
        End If
    End Sub

    Private Sub SetGangProgress_4(ByVal percent As Integer)
        If Me.InvokeRequired Then
            Dim n As New CbGangButtons(AddressOf SetGangProgress_4)
            Me.Invoke(n, {percent})
        Else
            pb_gang4.Value = percent
        End If
    End Sub

    Private Sub SetGangProgress_5(ByVal percent As Integer)
        If Me.InvokeRequired Then
            Dim n As New CbGangButtons(AddressOf SetGangProgress_5)
            Me.Invoke(n, {percent})
        Else
            pb_gang5.Value = percent
        End If
    End Sub

    Private Function IsAnyDeviceBusy() As Boolean
        For i = 0 To MEM_IF.DeviceCount - 1
            If MEM_IF.GetDevice(i).IsBusy Or MEM_IF.GetDevice(i).IsTaskRunning Then Return True
            If MEM_IF.GetDevice(i).GuiControl.IN_OPERATION Then Return True
        Next
        Return False
    End Function

#End Region

    Public Sub SetConnectionStatus(ByVal usb_dev As FCUSB_DEVICE)
        If Me.InvokeRequired Then
            Dim d As New cbSetConnectionStatus(AddressOf SetConnectionStatus)
            Me.Invoke(d, {usb_dev})
        Else
            StatusMessages_LoadMemoryDevices()
            Dim NumberOfDevices As Integer = USBCLIENT.Count()
            If NumberOfDevices = 0 Then
                Me.lblStatus.Text = RM.GetString("gui_fcusb_disconnected")
                ClearStatusMessage()
                ScriptEngine.Unload() 'Unloads the device script and any objects/tabs
                RemoveStatusMessage(RM.GetString("gui_active_script"))
            ElseIf NumberOfDevices = 1 Then
                Me.lblStatus.Text = RM.GetString("gui_fcusb_connected")
            Else 'Multi programming!
                Me.lblStatus.Text = String.Format(RM.GetString("gui_fcusb_connected_multi"), NumberOfDevices)
            End If
            Application.DoEvents()
        End If
    End Sub

    Public Sub OnNewDeviceConnected(ByVal usb_dev As FCUSB_DEVICE)
        If Me.InvokeRequired Then
            Dim d As New cbOnNewDeviceConnected(AddressOf OnNewDeviceConnected)
            Me.Invoke(d, {usb_dev})
        Else
            mi_refresh.Enabled = True
            StatusMessages_LoadMemoryDevices()
            Dim mem_instance As MemoryDeviceInstance = usb_dev.ATTACHED.Last
            SetStatus(String.Format(RM.GetString("gui_fcusb_new_device"), mem_instance.Name))
            If mem_instance.FlashType = MemoryType.SERIAL_NOR Then
                Dim spi_dev As SPI_NOR_FLASH = usb_dev.SPI_NOR_IF.MyFlashDevice
                If Not (spi_dev.VENDOR_SPECIFIC = FlashMemory.VENDOR_FEATURE.NotSupported) Then
                    mem_instance.VendorMenu = Nothing
                    If (spi_dev.VENDOR_SPECIFIC = FlashMemory.VENDOR_FEATURE.Micron) Then
                        mem_instance.VendorMenu = New NonVol_1(usb_dev)
                    ElseIf (spi_dev.VENDOR_SPECIFIC = FlashMemory.VENDOR_FEATURE.Spansion) Then
                        mem_instance.VendorMenu = New NonVol_2(usb_dev)
                    ElseIf (spi_dev.VENDOR_SPECIFIC = FlashMemory.VENDOR_FEATURE.ISSI) Then
                        mem_instance.VendorMenu = New NonVol_3(usb_dev)
                    End If
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
            Dim memDev As MemoryDeviceInstance = GetSelectedMemoryInterface()
            If memDev Is Nothing Then Exit Sub
            memDev.FCUSB.USB_LEDBlink()
            If MsgBox(RM.GetString("mc_erase_warning"), MsgBoxStyle.YesNo, String.Format(RM.GetString("mc_erase_confirm"), memDev.Name)) = MsgBoxResult.Yes Then
                WriteConsole(String.Format(RM.GetString("mc_erase_command_sent"), memDev.Name))
                SetStatus(RM.GetString("mem_erasing_device"))
                memDev.DisableGuiControls()
                memDev.EraseFlash()
                memDev.WaitUntilReady()
                memDev.EnableGuiControls()
                memDev.GuiControl.RefreshView()
            End If
            memDev.FCUSB.USB_LEDOn()
            SetStatus(RM.GetString("mem_erase_device_success"))
            mi_erase_tool.Enabled = True
        Catch ex As Exception
        End Try
    End Sub

    Private Sub mi_device_features_Click(sender As Object, e As EventArgs) Handles mi_device_features.Click
        Dim memDev As MemoryDeviceInstance = GetSelectedMemoryInterface()
        If memDev Is Nothing Then Exit Sub
        If memDev.VendorMenu Is Nothing Then Exit Sub
        Dim n As New Form With {.Text = "Vendor specific / device configuration"}
        n.Width = memDev.VendorMenu.Width + 10
        n.Height = memDev.VendorMenu.Height + 50
        n.FormBorderStyle = FormBorderStyle.FixedSingle
        n.ShowIcon = False
        n.ShowInTaskbar = False
        n.MaximizeBox = False
        n.MinimizeBox = False
        n.Controls.Add(memDev.VendorMenu)
        n.StartPosition = FormStartPosition.CenterParent
        n.ShowDialog()
    End Sub



End Class
