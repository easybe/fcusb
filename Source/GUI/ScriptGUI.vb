Imports System.Threading
Imports FlashcatUSB.EC_ScriptEngine

Module ScriptGUI
    Delegate Function ScriptFunction(arguments() As ScriptVariable, Index As Int32) As ScriptVariable

    Public Sub AddInternalMethods()
        Dim TAB_CMD As New ScriptCmd("TAB")
        TAB_CMD.Add("create", {CmdPrm.String}, New ScriptFunction(AddressOf c_tab_create))
        TAB_CMD.Add("addgroup", {CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addgroup))
        TAB_CMD.Add("addbox", {CmdPrm.String, CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addbox))
        TAB_CMD.Add("addtext", {CmdPrm.String, CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addtext))
        TAB_CMD.Add("addimage", {CmdPrm.String, CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addimage))
        TAB_CMD.Add("addbutton", {CmdPrm.String, CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addbutton))
        TAB_CMD.Add("addprogress", {CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addprogress))
        TAB_CMD.Add("remove", {CmdPrm.String}, New ScriptFunction(AddressOf c_tab_remove))
        TAB_CMD.Add("settext", {CmdPrm.String, CmdPrm.String}, New ScriptFunction(AddressOf c_tab_settext))
        TAB_CMD.Add("gettext", {CmdPrm.String}, New ScriptFunction(AddressOf c_tab_gettext))
        TAB_CMD.Add("buttondisable", {CmdPrm.String_Optional}, New ScriptFunction(AddressOf c_tab_buttondisable))
        TAB_CMD.Add("buttonenable", {CmdPrm.String_Optional}, New ScriptFunction(AddressOf c_tab_buttonenable))
        ScriptProcessor.AddScriptNest(TAB_CMD)
    End Sub

#Region "TAB commands"

    Private Function c_tab_create(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim tab_name As String = CStr(arguments(0).Value)
        GUI.CreateUserTab(" " & tab_name & " ") 'Thread-Safe
        Dim CurrentCount As Integer = GUI.GetUserTabCount()
        Dim sv As New ScriptVariable(CreateVarName(), DataType.UInteger)
        sv.Value = CurrentCount - 1
        Return sv
    End Function

    Private Function c_tab_addgroup(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim NewGroup As New GroupBox
        NewGroup.Name = CStr(arguments(0).Value)
        NewGroup.Text = CStr(arguments(0).Value)
        NewGroup.Left = CInt(arguments(1).Value)
        NewGroup.Top = CInt(arguments(2).Value)
        NewGroup.Width = CInt(arguments(3).Value)
        NewGroup.Height = CInt(arguments(4).Value)
        GUI.AddControlToTable(CInt(Index), NewGroup)
        Return Nothing
    End Function

    Private Function c_tab_addbox(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim NewTextBox As New TextBox
        NewTextBox.Name = CStr(arguments(0).Value)
        NewTextBox.Text = CStr(arguments(1).Value)
        NewTextBox.Width = (NewTextBox.Text.Length * 8) + 2
        NewTextBox.TextAlign = HorizontalAlignment.Center
        NewTextBox.Left = CInt(arguments(2).Value)
        NewTextBox.Top = CInt(arguments(3).Value)
        GUI.AddControlToTable(CInt(Index), NewTextBox)
        Return Nothing
    End Function

    Private Function c_tab_addtext(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim NewTextLabel As New Label
        NewTextLabel.AutoSize = True
        NewTextLabel.Name = CStr(arguments(0).Value)
        NewTextLabel.Text = CStr(arguments(1).Value)
        NewTextLabel.Width = (NewTextLabel.Text.Length * 7)
        NewTextLabel.Left = CInt(arguments(2).Value)
        NewTextLabel.Top = CInt(arguments(3).Value)
        NewTextLabel.BringToFront()
        GUI.AddControlToTable(CInt(Index), NewTextLabel)
        Return Nothing
    End Function

    Private Function c_tab_addimage(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim filen As String = CStr(arguments(1).Value)
        Dim finfo As New IO.FileInfo(ScriptPath & filen)
        If Not finfo.Exists Then PrintConsole("Tab.AddImage, specified image not found: " & filen) : Return Nothing
        Dim newImage As Image = Image.FromFile(finfo.FullName)
        Dim NewPB As New PictureBox
        NewPB.Name = CStr(arguments(0).Value)
        NewPB.Image = newImage
        NewPB.Left = CInt(arguments(2).Value)
        NewPB.Top = CInt(arguments(3).Value)
        NewPB.Width = newImage.Width + 5
        NewPB.Height = newImage.Height + 5
        NewPB.BringToFront() 'does not work
        GUI.AddControlToTable(CInt(Index), NewPB)
        Return Nothing
    End Function

    Private Function c_tab_addbutton(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim NewButton As New Button
        NewButton.AutoSize = True
        NewButton.Name = CStr(arguments(0).Value)
        NewButton.Text = CStr(arguments(1).Value)
        AddHandler NewButton.Click, AddressOf ButtonHandler
        NewButton.Left = CInt(arguments(2).Value)
        NewButton.Top = CInt(arguments(3).Value)
        NewButton.BringToFront() 'does not work
        GUI.AddControlToTable(CInt(Index), NewButton)
        Return Nothing
    End Function

    Private Function c_tab_addprogress(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim bar_left As Integer = CInt(arguments(0).Value)
        Dim bar_top As Integer = CInt(arguments(1).Value)
        Dim bar_width As Integer = CInt(arguments(2).Value)
        MainApp.ProgressBar_Add(CInt(Index), bar_left, bar_top, bar_width)
        Return Nothing
    End Function

    Private Function c_tab_remove(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim item_name As String = CStr(arguments(0).Value)
        RemoveUserControl(item_name)
        Return Nothing
    End Function

    Private Function c_tab_settext(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim ctrl_name As String = CStr(arguments(0).Value)
        Dim new_text As String = CStr(arguments(1).Value)
        GUI.SetControlText(CInt(Index), ctrl_name, new_text)
        Return Nothing
    End Function

    Private Function c_tab_gettext(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim ctrl_name As String = CStr(arguments(0).Value)
        Dim result_str As String = GUI.GetControlText(CInt(Index), ctrl_name)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.String)
        sv.Value = result_str
        Return sv
        Return Nothing
    End Function

    Private Function c_tab_buttondisable(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim specific_button As String = ""
        If specific_button.Length = 1 Then
            specific_button = CStr(arguments(0).Value)
        End If
        GUI.HandleButtons(CInt(Index), False, specific_button)
        Return Nothing
    End Function

    Private Function c_tab_buttonenable(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim specific_button As String = ""
        If specific_button.Length = 1 Then
            specific_button = CStr(arguments(0).Value)
        End If
        GUI.HandleButtons(CInt(Index), True, specific_button)
        Return Nothing
    End Function

#End Region

    'Removes a user control from NAME
    Private Sub RemoveUserControl(ctr_name As String)
        If GUI Is Nothing Then Exit Sub
        Dim CurrentCount As Integer = GUI.GetUserTabCount()
        If CurrentCount = 0 Then Exit Sub
        For i As Integer = 0 To CurrentCount - 1
            Dim uTab As TabPage = GUI.GetUserTab(i)
            For Each user_control As Control In uTab.Controls
                If user_control.Name.ToUpper.Equals(ctr_name.ToUpper) Then
                    uTab.Controls.Remove(user_control)
                    Exit Sub
                End If
            Next
        Next
    End Sub
    'Handles when the user clicks a button
    Private Sub ButtonHandler(sender As Object, e As EventArgs)
        Dim MyButton As Button = CType(sender, Button)
        Dim EventToCall As String = MyButton.Name
        Dim EventThread As New Thread(AddressOf ScriptProcessor.CallEvent)
        EventThread.Name = "Event:" & EventToCall
        EventThread.SetApartmentState(ApartmentState.STA)
        EventThread.Start(EventToCall)
        MyButton.Select()
    End Sub

    Private Function CreateVarName() As String
        Return ScriptProcessor.CurrentVars.GetNewName()
    End Function




End Module
