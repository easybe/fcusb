Imports FlashcatUSB.FlashMemory

'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2017 - ALL RIGHTS RESERVED
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK

Public Class MainForm
    Private StatusMessageControls() As Control 'Holds the label that the form displays

    Private Sub MainForm_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        miRepeatWrite.Enabled = False
        SetupText()
        Me.MinimumSize = Me.Size
        ReDim StatusMessageControls(6)
        StatusMessageControls(0) = sm1
        StatusMessageControls(1) = sm2
        StatusMessageControls(2) = sm3
        StatusMessageControls(3) = sm4
        StatusMessageControls(4) = sm5
        StatusMessageControls(5) = sm6
        StatusMessageControls(6) = sm7
        ClearStatusMessage()
        Me.Text = "FlashcatUSB Classic (Build " & Build & ")"
        Dim libdll As New IO.FileInfo(Application.StartupPath & "\LibUsbDotNet.dll")
        If Not libdll.Exists Then
            MsgBox("Unable to load LibUsbDotNet.dll", MsgBoxStyle.Critical, "Error starting application")
            Me.Close() : Exit Sub
        End If
        Dim libVer As String = Reflection.AssemblyName.GetAssemblyName("LibUsbDotNet.dll").Version.ToString
        WriteConsole(String.Format(RM.GetString("fcusb_libusb_ver"), libVer))
        LoadSpiTabSettings()
        MyTabs.Controls.Remove(AvrTab)
        AvrTabInit()
        ScriptEngine.PrintInformation() 'Script Engine
        miDetectDevice.Enabled = False
        VerifyMenuItem.Checked = MySettings.VERIFY_WRITE
        BlockManagerItem_Select()
        mi_NandPreserve.Checked = MySettings.NAND_Preserve
        Select Case MySettings.EXTIO_VPP
            Case FlashcatSettings.SO44_VPP_SETTING.Disabled
                mi_vpp_vcc_normal.Checked = True
            Case FlashcatSettings.SO44_VPP_SETTING.Write_12v
                mi_so44_12v_write.Checked = True
            Case Else
                MySettings.EXTIO_VPP = FlashcatSettings.SO44_VPP_SETTING.Disabled
                mi_vpp_vcc_normal.Checked = True
        End Select
        SetCheckmarkForMode()
        CurrentScript_MI.Enabled = False
        LoadScript_MI.Enabled = False
        UnloadScript_MI.Enabled = False
        SetupI2CGuiControls()
        PrintConsole("NAND memory (x8/SLC) database loaded: " & FlashDatabase.PartCount(FlashMemory.MemoryType.SLC_NAND) & " devices supported")
        PrintConsole("NOR memory (x8/x16) database loaded: " & FlashDatabase.PartCount(FlashMemory.MemoryType.PARALLEL_NOR) & " devices supported")
        PrintConsole("SPI memory database loaded: " & FlashDatabase.PartCount(FlashMemory.MemoryType.SERIAL_NOR) & " devices supported")
        PrintConsole(String.Format(RM.GetString("fcusb_welcome"), Build))
        PrintConsole(String.Format(RM.GetString("fcusb_running"), Platform))
        SetEnableEraseMenuItem(False)
        SetEnableNandMapMenuItem(False)
        SetBackupToolMenuItem(False)
        mi_bitendian_big.Checked = True
        mi_bitswap_none.Checked = True
        FormIsLoaded = True
    End Sub


#Region "Script Tab Control"
    'Removes all of the tabs except for Status and Console
    Public Sub RemoveAllTabs()
        Dim i As Integer
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

    Public Function GetUserTab(ByVal ind As Integer) As TabPage
        Dim MyObj As String = "IND:" & CStr(ind)
        Dim tP As TabPage
        For Each tP In MyTabs.Controls
            If tP.Name = MyObj Then Return tP
        Next
        Return Nothing
    End Function

    Public Sub SetControlText(ByVal usertabind As Integer, ByVal UserControl As String, ByVal NewText As String)
        If Me.MyTabs.InvokeRequired Then
            Dim d As New cbSetControlText(AddressOf SetControlText)
            Me.Invoke(d, New Object() {usertabind, UserControl, NewText})
        Else
            Dim usertab As TabPage = GetUserTab(usertabind)
            If usertab Is Nothing Then Exit Sub
            Dim C As Control
            For Each C In usertab.Controls
                If UCase(C.Name) = UCase(UserControl) Then
                    C.Text = NewText
                    If C.GetType Is GetType(Windows.Forms.TextBox) Then
                        Dim t As TextBox = CType(C, TextBox)
                        t.SelectionStart = 0
                    End If
                    Exit Sub
                End If
            Next
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

    Public Sub AddTab(ByVal tb As TabPage)
        If Me.MyTabs.InvokeRequired Then
            Dim d As New cbAddTab(AddressOf AddTab)
            Me.Invoke(d, New Object() {tb})
        Else
            MyTabs.Controls.Add(tb)
        End If
    End Sub

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

    Public Sub HandleButtons(ByVal usertabind As Integer, ByVal Enabled As Boolean, ByVal BtnName As String)
        Dim usertab As TabPage = GetUserTab(usertabind)
        If usertab Is Nothing Then Exit Sub
        Dim C As Control
        For Each C In usertab.Controls
            If C.GetType Is GetType(Windows.Forms.Button) Then
                If UCase(C.Name) = UCase(BtnName) Or BtnName = "" Then
                    If Enabled Then
                        EnableButton(CType(C, Button))
                    Else
                        DisableButton(CType(C, Button))
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

#End Region

#Region "Scripts"

    Private Sub LoadScripts(ByVal JEDEC_ID As UInteger)
        CurrentScript_MI.DropDownItems.Clear()
        WriteConsole(RM.GetString("fcusb_script_check"))
        Dim MyScripts(,) As String = GetCompatibleScripts(JEDEC_ID)
        Dim DefaultScript As String = Reg_GetPref_DefaultScript(Hex(JEDEC_ID))
        Dim SelectScript As Integer = 0
        Dim i As Integer
        If MyScripts Is Nothing Then
            WriteConsole(RM.GetString("fcusb_script_none"))
        ElseIf (MyScripts.Length / 2) = 1 Then
            WriteConsole(String.Format(RM.GetString("fcusb_script_loading"), MyScripts(0, 0)))
            ScriptEngine.LoadScriptFile(New IO.FileInfo(ScriptPath & MyScripts(0, 0)))
            UpdateStatusMessage(RM.GetString("cnts_script"), MyScripts(0, 1))
            CurrentScript_MI.Enabled = True
            LoadScript_MI.Enabled = True
            UnloadScript_MI.Enabled = True
            Dim tsi As ToolStripMenuItem = CurrentScript_MI.DropDownItems.Add(MyScripts(0, 0))
            tsi.Tag = MyScripts(0, 0)
            AddHandler tsi.Click, AddressOf LoadSelectedScript
            tsi.Checked = True
        Else 'Multiple scripts (choose preferrence)
            CurrentScript_MI.Enabled = True
            LoadScript_MI.Enabled = True
            UnloadScript_MI.Enabled = True
            If Not DefaultScript = "" Then 'No prefference
                For i = 0 To CInt((MyScripts.Length / 2) - 1)
                    If UCase(MyScripts(i, 0)) = UCase(DefaultScript) Then
                        SelectScript = i
                        Exit For
                    End If
                Next
            End If
            For i = 0 To CInt((MyScripts.Length / 2) - 1)
                Dim tsi As ToolStripMenuItem = CurrentScript_MI.DropDownItems.Add(MyScripts(i, 1))
                tsi.Tag = MyScripts(i, 0)
                AddHandler tsi.Click, AddressOf LoadSelectedScript
                If SelectScript = i Then
                    tsi.Checked = True
                End If
            Next
            UpdateStatusMessage(RM.GetString("cnts_script"), MyScripts(SelectScript, 0))
            Dim df As New IO.FileInfo(ScriptPath & MyScripts(SelectScript, 0))
            ScriptEngine.LoadScriptFile(df)
        End If
    End Sub
    'Opens a script from the file 
    Private Sub LoadScript_MI_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles LoadScript_MI.Click
        Dim OpenMe As New OpenFileDialog
        OpenMe.AddExtension = True
        OpenMe.InitialDirectory = Application.StartupPath & "\Scripts\"
        OpenMe.Title = RM.GetString("fcusb_script_open")
        OpenMe.CheckPathExists = True
        Dim FcFname As String = "Flachcat Scripts (*.fcs)|*.fcs"
        Dim AllF As String = "All files (*.*)|*.*"
        OpenMe.Filter = FcFname & "|" & AllF
        If OpenMe.ShowDialog = Windows.Forms.DialogResult.OK Then
            LoadScriptFile(OpenMe.FileName)
        End If
    End Sub
    'Unloads a script from the engine
    Private Sub UnloadScript_MI_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles UnloadScript_MI.Click
        Dim CurrentScriptName As String = ScriptEngine.ScriptName
        If Not CurrentScriptName = "" Then
            Dim WasUnloaded As Boolean = ScriptEngine.UnloadDeviceScript()
            If WasUnloaded AndAlso OperationMode = AvrMode.SPI Then Script_DisconnectSpiAndReload()
            RemoveScriptChecks()
            RemoveStatusMessage(RM.GetString("cnts_script"))
            SetStatus(String.Format(RM.GetString("fcusb_script_unload"), CurrentScriptName))
        End If
    End Sub

    Private Sub LoadSelectedScript(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Dim tsi As ToolStripMenuItem = sender
        If Not tsi.Checked Then
            Dim ScriptName As String = tsi.Tag
            LoadScriptFile(Application.StartupPath & "\Scripts\" & ScriptName)
            RemoveScriptChecks()
            tsi.Checked = True
            Reg_SavePref_DefaultScript(Hex(EJ_IF.TargetDevice.IDCODE), ScriptName)
        End If
    End Sub

    Private Sub RemoveScriptChecks()
        Dim tsi As ToolStripMenuItem
        For Each tsi In CurrentScript_MI.DropDownItems
            tsi.Checked = False
        Next
    End Sub

    Private Sub LoadScriptFile(ByVal scriptName As String)
        Dim f As New IO.FileInfo(scriptName)
        If f.Exists Then
            Dim WasUnloaded As Boolean = ScriptEngine.UnloadDeviceScript()
            If WasUnloaded AndAlso OperationMode = AvrMode.SPI Then Script_DisconnectSpiAndReload()
            UpdateStatusMessage(RM.GetString("cnts_script"), f.Name)
            If ScriptEngine.LoadScriptFile(f) Then
                SetStatus(String.Format(RM.GetString("fcusb_script_loaded"), f.Name))
                UnloadScript_MI.Enabled = True
            End If
        Else
            SetStatus(RM.GetString("fcusb_script_notexist"))
        End If
    End Sub

    Private Sub miDetectDevice_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles miDetectDevice.Click
        Disconnect(True)
    End Sub

    Private ScriptIsReloading As Boolean = False

    Private Sub Script_DisconnectSpiAndReload()
        ScriptIsReloading = True
        SPI_IF.Disconnect()
        Disconnect(True)
        Dim Counter As Integer = 0
        Do While OperationMode = AvrMode.NotConnected And ScriptIsReloading
            If Counter = 1000 Then
                ScriptIsReloading = False
                SetStatus("Unable to connect to device via SPI")
                Exit Sub
            End If
            Threading.Thread.Sleep(100)
            Application.DoEvents()
            Counter += 1
        Loop
    End Sub

#End Region

#Region "SPI Form Settings"

    Private Sub cbUseEnWS_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cbUseEnWS.CheckedChanged
        'SPI_IF.CustomDevice.Definition.SEND_EWSR = cbUseEnWS.Checked
        txtEnWS.Enabled = cbUseEnWS.Checked
    End Sub

    Private Sub txtEnWS_LostFocus(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtEnWS.LostFocus
        Try
            Dim inputval As String = txtEnWS.Text.Trim
            Dim lngValue As Long
            If IsNumeric(inputval) Then
                lngValue = CLng(inputval)
            ElseIf Utilities.IsDataType.HexString(inputval) Then
                lngValue = Utilities.HexToLng(inputval)
            Else 'Error
                SetStatus(RM.GetString("fcusb_err5"))
                GoTo ExitSub
            End If
            If lngValue > 255 Then
                SetStatus(RM.GetString("fcusb_err6"))
            Else
                SPICUSTOM_EWSR = CByte(lngValue)
            End If
        Catch ex As Exception
        End Try
ExitSub:
        txtEnWS.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_EWSR)))
    End Sub

    Private Sub cbSpiProgMode_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cbSpiProgMode.SelectedIndexChanged
        Select Case cbSpiProgMode.SelectedIndex
            Case 0
                SPICUSTOM_MODE = SPI_ProgramMode.PageMode
            Case 1
                SPICUSTOM_MODE = SPI_ProgramMode.AAI_Byte
            Case 2
                SPICUSTOM_MODE = SPI_ProgramMode.AAI_Word
            Case 3
                SPICUSTOM_MODE = SPI_ProgramMode.Atmel45Series
        End Select
    End Sub

    Private Sub txtEraseSize_LostFocus(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtEraseSize.LostFocus
        Try
            Dim inputval As String = txtEraseSize.Text.Trim
            Dim lngValue As Long
            If IsNumeric(inputval) Then
                lngValue = CLng(inputval)
            ElseIf Utilities.IsDataType.HexString(inputval) Then
                lngValue = Utilities.HexToLng(inputval)
            Else 'Error
                SetStatus(RM.GetString("fcusb_err5"))
                GoTo ExitSub
            End If
            If lngValue > 2147483647 Then
                SetStatus(RM.GetString("fcusb_err7"))
            Else
                SPICUSTOM_SECTORSIZE = CInt(lngValue)
            End If
        Catch ex As Exception
        End Try
ExitSub:
        txtEraseSize.Text = "0x" & Utilities.Pad(Hex(SPICUSTOM_SECTORSIZE))
    End Sub

    Private Sub txtChipSize_LostFocus(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtChipSize.LostFocus
        Try
            Dim inputval As String = txtChipSize.Text.Trim
            Dim lngValue As Long
            If IsNumeric(inputval) Then
                lngValue = CLng(inputval)
            ElseIf Utilities.IsDataType.HexString(inputval) Then
                lngValue = Utilities.HexToLng(inputval)
            Else 'Error
                SetStatus(RM.GetString("fcusb_err5"))
                GoTo ExitSub
            End If
            If lngValue > 2147483647 Then
                SetStatus(RM.GetString("fcusb_err7"))
            ElseIf Not (lngValue Mod 16 = 0) Then
                SetStatus(RM.GetString("fcusb_spi_err4"))
            Else
                SPICUSTOM_SIZE = CInt(lngValue)
                If SPICUSTOM_SIZE <= &H10000 Then 'Flash devices less than 64KB
                    WriteConsole("SPI addressing mode set to 16-bit")
                    SPICUSTOM_ADDRESSBITS = 16
                    SPICUSTOM_4BYTE = False
                ElseIf SPICUSTOM_SIZE <= &H1000000 Then
                    WriteConsole("SPI addressing mode set to 24-bit")
                    SPICUSTOM_ADDRESSBITS = 24
                    SPICUSTOM_4BYTE = False
                Else
                    WriteConsole("SPI addressing mode set to 32-bit")
                    SPICUSTOM_ADDRESSBITS = 32
                    SPICUSTOM_4BYTE = True
                End If
            End If
        Catch ex As Exception
        End Try
ExitSub:
        txtChipSize.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_SIZE)))
    End Sub

    Private Sub txtPageSize_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtPageSize.LostFocus
        Dim inputval As String = txtPageSize.Text.Trim
        Dim lngValue As Long
        If IsNumeric(inputval) Then
            lngValue = CLng(inputval)
        ElseIf Utilities.IsDataType.HexString(inputval) Then
            lngValue = Utilities.HexToLng(inputval)
        Else 'Error
            SetStatus(RM.GetString("fcusb_err5")) 'Unable to set value (invalid input)
            GoTo ExitSub
        End If
        If Not lngValue Mod 8 = 0 Then
            SetStatus("Page size must be a multiple of 8")
            GoTo ExitSub
        End If
        SPICUSTOM_PAGESIZE = lngValue
ExitSub:
        txtPageSize.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_PAGESIZE)))
    End Sub

    Private Sub txtWriteStatus_LostFocus(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtWriteStatus.LostFocus
        Try
            Dim inputval As String = txtWriteStatus.Text.Trim
            Dim lngValue As Long
            If IsNumeric(inputval) Then
                lngValue = CLng(inputval)
            ElseIf Utilities.IsDataType.HexString(inputval) Then
                lngValue = Utilities.HexToLng(inputval)
            Else 'Error
                SetStatus(RM.GetString("fcusb_err5"))
                GoTo ExitSub
            End If
            If lngValue > 255 Then
                SetStatus(RM.GetString("fcusb_err6"))
            Else
                SPICUSTOM_WRSR = CByte(lngValue)
            End If
        Catch ex As Exception
        End Try
ExitSub:
        txtWriteStatus.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_WRSR)))
    End Sub

    Private Sub txtPageProgram_LostFocus(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtPageProgram.LostFocus
        Try
            Dim inputval As String = txtPageProgram.Text.Trim
            Dim lngValue As Long
            If IsNumeric(inputval) Then
                lngValue = CLng(inputval)
            ElseIf Utilities.IsDataType.HexString(inputval) Then
                lngValue = Utilities.HexToLng(inputval)
            Else 'Error
                SetStatus(RM.GetString("fcusb_err5"))
                GoTo ExitSub
            End If
            If lngValue > 255 Then
                SetStatus(RM.GetString("fcusb_err6"))
            Else
                SPICUSTOM_PROG = CByte(lngValue)
            End If
        Catch ex As Exception
        End Try
ExitSub:
        txtPageProgram.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_PROG)))
    End Sub

    Private Sub txtWriteEnable_LostFocus(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtWriteEnable.LostFocus
        Try
            Dim inputval As String = txtWriteEnable.Text.Trim
            Dim lngValue As Long
            If IsNumeric(inputval) Then
                lngValue = CLng(inputval)
            ElseIf Utilities.IsDataType.HexString(inputval) Then
                lngValue = Utilities.HexToLng(inputval)
            Else 'Error
                SetStatus(RM.GetString("fcusb_err5"))
                GoTo ExitSub
            End If
            If lngValue > 255 Then
                SetStatus(RM.GetString("fcusb_err6"))
            Else
                SPICUSTOM_WREN = CByte(lngValue)
            End If
        Catch ex As Exception
        End Try
ExitSub:
        txtWriteEnable.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_WREN)))
    End Sub

    Private Sub txtReadStatus_LostFocus(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtReadStatus.LostFocus
        Try
            Dim inputval As String = txtReadStatus.Text.Trim
            Dim lngValue As Long
            If IsNumeric(inputval) Then
                lngValue = CLng(inputval)
            ElseIf Utilities.IsDataType.HexString(inputval) Then
                lngValue = Utilities.HexToLng(inputval)
            Else 'Error
                SetStatus(RM.GetString("fcusb_err5"))
                GoTo ExitSub
            End If
            If lngValue > 255 Then
                SetStatus(RM.GetString("fcusb_err6"))
            Else
                SPICUSTOM_RDSR = CByte(lngValue)
            End If
        Catch ex As Exception
        End Try
ExitSub:
        txtReadStatus.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_RDSR)))
    End Sub

    Private Sub txtRead_LostFocus(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtRead.LostFocus
        Try
            Dim inputval As String = txtRead.Text.Trim
            Dim lngValue As Long
            If IsNumeric(inputval) Then
                lngValue = CLng(inputval)
            ElseIf Utilities.IsDataType.HexString(inputval) Then
                lngValue = Utilities.HexToLng(inputval)
            Else 'Error
                SetStatus(RM.GetString("fcusb_err5"))
                GoTo ExitSub
            End If
            If lngValue > 255 Then
                SetStatus(RM.GetString("fcusb_err6"))
            Else
                SPICUSTOM_READ = CByte(lngValue)
            End If
        Catch ex As Exception
        End Try
ExitSub:
        txtRead.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_READ)))
    End Sub

    Private Sub txtSectorErase_LostFocus(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtSectorErase.LostFocus
        Try
            Dim inputval As String = txtSectorErase.Text.Trim
            Dim lngValue As Long
            If IsNumeric(inputval) Then
                lngValue = CLng(inputval)
            ElseIf Utilities.IsDataType.HexString(inputval) Then
                lngValue = Utilities.HexToLng(inputval)
            Else 'Error
                SetStatus(RM.GetString("fcusb_err5"))
                GoTo ExitSub
            End If
            If lngValue > 255 Then
                SetStatus(RM.GetString("fcusb_err6"))
            Else
                SPICUSTOM_SE = CByte(lngValue)
            End If
        Catch ex As Exception
        End Try
ExitSub:
        txtSectorErase.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_SE)))
    End Sub

    Private Sub txtChipErase_LostFocus(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtChipErase.LostFocus
        Try
            Dim inputval As String = txtChipErase.Text.Trim
            Dim lngValue As Long
            If IsNumeric(inputval) Then
                lngValue = CLng(inputval)
            ElseIf Utilities.IsDataType.HexString(inputval) Then
                lngValue = Utilities.HexToLng(inputval)
            Else 'Error
                SetStatus(RM.GetString("fcusb_err5"))
                GoTo ExitSub
            End If
            If lngValue > 255 Then
                SetStatus(RM.GetString("fcusb_err6"))
            Else
                SPICUSTOM_BE = CByte(lngValue)
            End If
        Catch ex As Exception
        End Try
ExitSub:
        txtChipErase.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_BE)))
    End Sub

    Private Sub RadioUseSpiSettings_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioUseSpiSettings.CheckedChanged
        If RadioUseSpiSettings.Checked Then
            cbSPI.Enabled = True
            SPI_UseCustom = True
        End If
    End Sub

    Private Sub RadioUseSpiAuto_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioUseSpiAuto.CheckedChanged
        If RadioUseSpiAuto.Checked Then
            cbSPI.Enabled = False
            SPI_UseCustom = False
        End If
    End Sub

    Private Sub LoadSpiTabSettings()
        cbSPI.Enabled = False
        cbUseEnWS.Checked = False
        txtChipSize.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_SIZE)))
        txtPageSize.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_PAGESIZE)))
        txtEraseSize.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_SECTORSIZE)))
        txtWriteStatus.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_WRSR)))
        txtPageProgram.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_PROG)))
        txtWriteEnable.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_WREN)))
        txtReadStatus.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_RDSR)))
        txtRead.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_READ)))
        txtSectorErase.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_SE)))
        txtChipErase.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_BE)))
        txtEnWS.Text = "0x" & Utilities.Pad(Hex((SPICUSTOM_EWSR)))
        cbSpiProgMode.Items.Add("Page")
        cbSpiProgMode.Items.Add("AAI (Byte)")
        cbSpiProgMode.Items.Add("AAI (Word)")
        cbSpiProgMode.Items.Add("Atmel")
        cbSpiProgMode.SelectedIndex = 0
    End Sub

    Public Sub ShowSpiSettings(ByVal ShowTab As Boolean)
        If Me.InvokeRequired Then
            Try
                Dim d As New cbShowSpiSettings(AddressOf ShowSpiSettings)
                Me.Invoke(d, New Object() {ShowTab})
            Catch ex As Exception
            End Try
        Else
            Try
                If ShowTab Then
                    If Not MyTabs.TabPages.Contains(SpiTab) Then
                        MyTabs.TabPages.Add(SpiTab)
                    End If
                Else
                    If MyTabs.TabPages.Contains(SpiTab) Then
                        MyTabs.TabPages.Remove(SpiTab)
                    End If
                End If
            Catch ex As Exception
            End Try
        End If
    End Sub

    Public Sub ShowI2CSettings(ByVal ShowTab As Boolean)
        If Me.InvokeRequired Then
            Try
                Dim d As New cbShowI2CSettings(AddressOf ShowI2CSettings)
                Me.Invoke(d, New Object() {ShowTab})
            Catch ex As Exception
            End Try
        Else
            Try
                If ShowTab Then
                    If Not MyTabs.TabPages.Contains(I2CTab) Then
                        MyTabs.TabPages.Add(I2CTab)
                    End If
                Else
                    If MyTabs.TabPages.Contains(I2CTab) Then
                        MyTabs.TabPages.Remove(I2CTab)
                    End If
                End If
            Catch ex As Exception
            End Try
        End If
    End Sub


#End Region

#Region "DFU Form Tab"
    Private FwHexName As String
    Private FwHexFile() As String = Nothing
    Private FwHexBin() As Byte = Nothing
    Private HexFileSize As Integer = 0
    Private CommandThread As Threading.Thread
    Private ScriptCommand As String
    'Called once at startup to setup the tab
    Private Sub AvrTabInit()
        DelHexFileInfo()
        cmdAvrProg.Enabled = False
        cmdAvrStart.Enabled = False
    End Sub
    'Called when the app connects to the DFU bootloader
    Private Sub AvrDFUconnect()
        cmdAvrLoad.Enabled = True
        If Not FwHexBin Is Nothing Then
            cmdAvrProg.Enabled = True
            cmdAvrStart.Enabled = True
            cmdAvrProg.BringToFront()
            cmdAvrProg.Select()
        End If
    End Sub
    'Deletes all of the hex file in memory and clears gui labels
    Private Sub DelHexFileInfo()
        lblAvrFn.Text = RM.GetString("fcusb_hex_nofile")
        lblAvrRange.Text = RM.GetString("fcusb_range") & " 0x0000 - 0x0000"
        lblAvrCrc.Text = "CRC: 0x000000"
        FwHexBin = Nothing
        HexFileSize = 0
        FwHexName = ""
        'AvrEditor.UnloadData()
    End Sub

    Private Sub txtInput_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtInput.KeyPress
        If Asc(e.KeyChar) = 13 Then 'Enter key was pressed
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
        ScriptEngine.ExecuteCommand(ScriptCommand)
    End Sub

    Private Sub cmdAvrLoad_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAvrLoad.Click
        Dim OpenMe As New OpenFileDialog
        OpenMe.AddExtension = True
        OpenMe.InitialDirectory = Application.StartupPath & "\Firmware"
        OpenMe.Title = RM.GetString("fcusb_common_avrtoprog") 'Choose AVR firmware to program
        OpenMe.CheckPathExists = True
        OpenMe.Filter = "Intel Hex Format (*.hex)|*.hex"
        If OpenMe.ShowDialog = Windows.Forms.DialogResult.OK Then
            Dim finfo As New IO.FileInfo(OpenMe.FileName)
            Dim FileData() As Byte = Utilities.FileIO.ReadBytes(finfo.FullName)
            If Utilities.IsIntelHex(FileData) Then
                FwHexBin = Utilities.IntelHexToBin(FileData)
                FwHexName = finfo.Name
                HexFileSize = finfo.Length
                LoadHexFileInfo()
            Else
                SetStatus(RM.GetString("fcusb_err8")) 'Error: file is corrupt or not a AVR Hex file
            End If
        End If
    End Sub

    Private Sub cmdAvrProg_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAvrProg.Click
        Dim Res As Boolean = False
        cmdAvrProg.Enabled = False 'Prevents user from double clicking program button
        cmdAvrStart.Enabled = False
        Dim DfuSize As Integer = DFU_IF.GetFlashSize
        If FwHexBin.Length > DfuSize Then
            SetStatus(RM.GetString("fcusb_avr_err1")) 'Error: The hex file data is larger than the size of the DFU memory
            GoTo ExitAvrProg
        End If
        UpdateDfuStatusBar(0)
        SetStatus(RM.GetString("fcusb_avr_err2"))
        WriteConsole(RM.GetString("fcusb_avr_err3"))
        Res = DFU_IF.EraseFlash()
        If Not Res Then
            WriteConsole(RM.GetString("fcusb_avr_err4"))
            SetStatus(RM.GetString("fcusb_avr_err4"))
            GoTo ExitAvrProg
        Else
            WriteConsole(RM.GetString("fcusb_avr_sucess"))
        End If
        Application.DoEvents()
        Threading.Thread.Sleep(250)
        SetStatus(RM.GetString("fcusb_newavrfw"))
        WriteConsole(String.Format(RM.GetString("fcusb_avrwrite"), FwHexBin.Length)) 'Beginning AVR flash write ({0} bytes)
        Application.DoEvents()
        Threading.Thread.Sleep(250)
        Res = DFU_IF.WriteFlash(FwHexBin)
        If Not Res Then
            WriteConsole(RM.GetString("fcusb_avr_err5"))
            SetStatus(RM.GetString("fcusb_avr_err5"))
            GoTo ExitAvrProg
        End If
        WriteConsole(RM.GetString("fcusb_avr_writedone"))
        SetStatus(RM.GetString("fcusb_avr_writedonegui"))
        Application.DoEvents()
        Threading.Thread.Sleep(250)
ExitAvrProg:
        cmdAvrStart.Enabled = True
        cmdAvrProg.Enabled = True
        UpdateDfuStatusBar(0)
    End Sub

    Private Sub cmdAvrStart_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAvrStart.Click
        cmdAvrLoad.Enabled = False
        cmdAvrProg.Enabled = False
        cmdAvrStart.Enabled = False
        DFU_IF.RunApp() 'Start application (hardware reset)
        Utilities.Sleep(100)
        MainApp.Disconnect()
    End Sub
    'Loads the gui information and loads up the hex editor
    Public Sub LoadHexFileInfo()
        cmdAvrProg.Enabled = True
        cmdAvrStart.Enabled = True
        lblAvrFn.Text = String.Format(RM.GetString("fcusb_file"), FwHexName)
        lblAvrRange.Text = RM.GetString("fcusb_range") & " 0x0000 - 0x" & Hex(FwHexBin.Length - 1).PadLeft(4, CChar("0"))
        Dim crc As Int32
        Dim i As Integer
        For i = 0 To FwHexBin.Length - 1
            crc += FwHexBin(0)
        Next
        crc = crc Xor &HFFFFFF
        crc = crc + 1
        lblAvrCrc.Text = "CRC: 0x" & Hex(crc And &HFFFFFF)
        AvrEditor.CreateHexViewer(0, FwHexBin)
    End Sub

    Public Sub UpdateDfuStatusBar(ByVal Perc As Integer)
        If Me.InvokeRequired Then
            Dim d As New cbUpdateDfuStatusBar(AddressOf UpdateDfuStatusBar)
            Me.Invoke(d, New Object() {Perc})
        Else
            DfuPbBar.Value = Perc
        End If
    End Sub

#End Region

#Region "Delegates"
    Delegate Sub cbSetConnectionStatus(ByVal Connected As Boolean)
    Delegate Sub cbClearStatusMessage()
    Delegate Sub cbUpdateStatusMessage(ByVal Label As String, ByVal Msg As String)
    Delegate Sub cbRemoveStatusMessage(ByVal Label As String)
    Delegate Sub cbShowSpiSettings(ByVal Show As Boolean)
    Delegate Sub cbShowI2CSettings(ByVal Show As Boolean)
    Delegate Sub cbUpdateDfuStatusBar(ByVal Value As Integer)
    Delegate Sub cbSetControlText(ByVal usertabind As Integer, ByVal Value As String, ByVal NewText As String)
    Delegate Sub SetBtnCallback(ByVal Value As Windows.Forms.Button)
    Delegate Sub cbAddToTab(ByVal usertab As Integer, ByVal Value As Object)
    Delegate Sub cbAddTab(ByVal tb As TabPage)
    Delegate Sub cbCreateFormTab(ByVal Index As Integer, ByVal Name As String)
    Delegate Sub cbPrintConsole(ByVal msg As String)
    Delegate Sub cbSetStatus(ByVal msg As String)
    Delegate Sub cbOnDeviceDisconnected()
    Delegate Sub cbOnDeviceConnected()
    Delegate Function cbGetSelectedMemoryInterface() As MemoryDeviceInstance
    Delegate Sub cbEnableChipToolMenu(ByVal Enabled As Boolean)
#End Region

#Region "SPI EEPROM MenuItems"

    Private Sub mi_spieeprom_none_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_none.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.None
    End Sub

    Private Sub mi_spieeprom_NRF24LE1_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_NRF24LE1.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.nRF24LE1
    End Sub

    Private Sub mi_spieeprom_nRF24LUIP_16KB_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_nRF24LUIP_16KB.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.nRF24LUIP_16KB
    End Sub

    Private Sub mi_spieeprom_nRF24LUIP_32KB_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_nRF24LUIP_32KB.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.nRF24LUIP_32KB
    End Sub

    Private Sub mi_spieeprom_AT25010A_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_AT25010A.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.AT25010A
    End Sub

    Private Sub mi_spieeprom_AT25020A_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_AT25020A.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.AT25020A
    End Sub

    Private Sub mi_spieeprom_AT25040A_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_AT25040A.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.AT25040A
    End Sub

    Private Sub mi_spieeprom_AT25080_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_AT25080.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.AT25080
    End Sub

    Private Sub mi_spieeprom_AT25160_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_AT25160.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.AT25160
    End Sub

    Private Sub mi_spieeprom_AT25320_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_AT25320.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.AT25320
    End Sub

    Private Sub mi_spieeprom_AT25640_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_AT25640.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.AT25640
    End Sub

    Private Sub mi_spieeprom_AT25128BT_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_AT25128B.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.AT25128B
    End Sub

    Private Sub mi_spieeprom_AT25256B_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_AT25256B.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.AT25256B
    End Sub

    Private Sub mi_spieeprom_AT25512_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_AT25512.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.AT25512
    End Sub

    Private Sub mi_spieeprom_STM95010_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_STM95010.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.M95010
    End Sub

    Private Sub mi_spieeprom_STM95020_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_STM95020.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.M95020
    End Sub

    Private Sub mi_spieeprom_STM95040_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_STM95040.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.M95040
    End Sub

    Private Sub mi_spieeprom_STM95080_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_STM95080.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.M95080
    End Sub

    Private Sub mi_spieeprom_STM95160_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_STM95160.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.M95160
    End Sub

    Private Sub mi_spieeprom_STM95320_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_STM95320.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.M95320
    End Sub

    Private Sub mi_spieeprom_STM95640_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_STM95640.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.M95640
    End Sub

    Private Sub mi_spieeprom_STM95128_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_STM95128.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.M95128
    End Sub

    Private Sub mi_spieeprom_STM95256_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_STM95256.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.M95256
    End Sub

    Private Sub mi_spieeprom_STM95512_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_STM95512.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.M95512
    End Sub

    Private Sub mi_spieeprom_STM95M01_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_STM95M01.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.M95M01
    End Sub

    Private Sub mi_spieeprom_STM95M02_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_STM95M02.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.M95M02
    End Sub

    Private Sub mi_spieeprom_25AA512_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_25AA512.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.M25AA512
    End Sub

    Private Sub mi_spieeprom_M25AA160A_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_M25AA160A.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.M25AA160A
    End Sub

    Private Sub mi_spieeprom_M25AA160B_Click(sender As Object, e As EventArgs) Handles mi_spieeprom_M25AA160B.Click
        UncheckAllEeepromDevices()
        DirectCast(sender, ToolStripMenuItem).Checked = True
        MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.M25AA160B
    End Sub

#End Region

#Region "I2C Support"
    Delegate Sub cbSetIc2Progress(ByVal perc As Integer)
    Delegate Sub cbSetI2CReadText(ByVal txt As String)
    Delegate Sub cbSetI2CWriteText(ByVal txt As String)
    Delegate Sub cbSetI2CReadEnable(ByVal en As Boolean)
    Delegate Sub cbSetI2CWriteEnable(ByVal en As Boolean)

    Private MySelectedI2CDevice As I2C_Device = Nothing
    Private Abort_I2C_Loop As Boolean = False
    Private MemoryThread As Threading.Thread

    Private Sub SetupI2CGuiControls()
        cmdI2cRead.Enabled = False
        cmdI2cWrite.Enabled = False
        I2CEditor.Enabled = False
        Dim i2cdevices As New DropPanelList
        AddHandler i2cdevices.ItemClicked, AddressOf NewItemClicked
        i2cdevices.AddItems("24 Series", {"24XX02 (256 x 8)", "24XX04 (512 x 8)", "24XX08 (1K x 8)", "24XX16 (2K x 8)", "24XX32 (4K x 8)", "24XX64 (8K x 8)", "24XX128 (16K x 8)", "24XX256 (32K x 8)", "24XX512 (64K x 8)"})
        i2cdevices.AddItems("BR24G Series", {"24G128 (16K x 8)", "24G256 (32K x 8)", "24G1M (128K x 8)"})
        cbEepromDevices.DropDownControl = i2cdevices
        cbEepromDevices.AllowResizeDropDown = False
        cbEepromDevices.DropDownSizeMode = CustomComboPlus.CustomComboPlus.SizeMode.UseDropDownSize
        cbEepromDevices.DropDownStyle = ComboBoxStyle.DropDownList
        cbEepromDevices.BackColor = Color.White

        ShowI2CSettings(False)
    End Sub
    'Fires when FCUSB is connected
    Private Sub SetupI2C_OnConnect()
        cbEepromDevices.Text = ""
        MySelectedI2CDevice = Nothing
        cbI2C_A2.Checked = False
        cbI2C_A1.Checked = False
        cbI2C_A0.Checked = False
        cmdI2cRead.Enabled = False
        cmdI2cWrite.Enabled = False
        I2CEditor.Enabled = False
        I2CEditor.CloseHexViewer()
    End Sub

    Private Sub cmdI2cConnect_Click(sender As Object, e As EventArgs) Handles cmdI2cConnect.Click
        I2CEditor.Enabled = False
        I2CEditor.CloseHexViewer()
        ' MySelectedI2CDevice = Nothing
        If MySelectedI2CDevice Is Nothing Then
            MsgBox("Please select a EEPROM device from the drop down menu", MsgBoxStyle.Critical, "No EEPROM selected")
            Exit Sub
        End If
        SPI_IF.IC2_Init() 'Initiates the I2C engine
        Dim Addr_Byte As Byte = &HA0 'Initial address
        If cbI2C_A2.Checked Then Addr_Byte = Addr_Byte Or (1 << 3)
        If cbI2C_A1.Checked Then Addr_Byte = Addr_Byte Or (1 << 2)
        If cbI2C_A0.Checked Then Addr_Byte = Addr_Byte Or (1 << 1)
        'This tells our I2C Engine about the EEPROM device connected
        SPI_IF.SetI2CSettings(MySelectedI2CDevice.PageSize, MySelectedI2CDevice.Size, MySelectedI2CDevice.AddressSize, Addr_Byte)
        Dim data() As Byte = SPI_IF.ReadData_I2C_EEPROM(0, 16)
        If (data Is Nothing) Then
            SetStatus("Error: unable to communicate with I2C EEPROM device")
        Else 'We are connected!
            SetStatus("Successfully connected to I2C EEPROM device")
            cmdI2cRead.Enabled = True
            cmdI2cWrite.Enabled = True
            I2CEditor.Enabled = True
            I2CEditor.CreateHexViewer(0, MySelectedI2CDevice.Size)
            I2CEditor.UpdateScreen()
        End If
    End Sub

    Private Sub I2CEditor_ReadStream(ByVal addr As UInteger, ByRef data() As Byte) Handles I2CEditor.RequestData
        Dim data_from_bus() As Byte = SPI_IF.ReadData_I2C_EEPROM(addr, data.Length)
        If data_from_bus IsNot Nothing AndAlso data_from_bus.Length = data.Length Then
            data = data_from_bus
        End If
    End Sub

    Private Sub cmdI2cRead_Click(sender As Object, e As EventArgs) Handles cmdI2cRead.Click
        If cmdI2cRead.Text = RM.GetString("fcusb_stop").ToUpper.Trim Then
            cmdI2cRead.Enabled = False
            Abort_I2C_Loop = True
            Exit Sub
        End If
        Dim BaseAddress As UInt32 = 0 'The starting address to read the from data
        Dim NumberToWrite As UInt32 = I2CEditor.BaseSize 'The total number of bytes to write
        SetStatus(String.Format(RM.GetString("fcusb_mem_readfrom"), "I2C_EEPROM"))
        Dim dbox As New MemControl_v2.DynamicRangeBox
        If Not dbox.ShowRangeBox(BaseAddress, NumberToWrite, I2CEditor.BaseSize) Then
            SetStatus(RM.GetString("fcusb_mem_readcanceled"))
            Exit Sub
        End If
        SetStatus(RM.GetString("fcusb_mem_readstart"))
        If NumberToWrite = 0 Then Exit Sub
        Dim data_back(NumberToWrite - 1) As Byte
        cmdI2cRead.Text = RM.GetString("fcusb_stop")
        cmdI2cWrite.Enabled = False
        MemoryThread = New Threading.Thread(Sub() ReadEEPROMLoop(BaseAddress, data_back))
        MemoryThread.Name = "tdReadI2C"
        MemoryThread.SetApartmentState(Threading.ApartmentState.STA)
        MemoryThread.IsBackground = True
        MemoryThread.Start()
    End Sub

    Private Sub cmdI2cWrite_Click(sender As Object, e As EventArgs) Handles cmdI2cWrite.Click
        If cmdI2cWrite.Text = RM.GetString("fcusb_stop").ToUpper.Trim Then
            cmdI2cWrite.Enabled = False
            Abort_I2C_Loop = True
            Exit Sub
        End If
        Dim fn As String = ""
        If Not OpenFileToWrite(fn) Then Exit Sub
        Dim FileIntelHexFormat As Boolean = False
        Dim FileNFO As New IO.FileInfo(fn)
        If Not FileNFO.Exists OrElse FileNFO.Length = 0 Then
            SetStatus(RM.GetString("fcusb_mem_err1"))
            Exit Sub
        End If
        SetStatus(RM.GetString("fcusb_mem_writestart"))
        Dim data_out() As Byte = Utilities.FileIO.ReadBytes(FileNFO.FullName)
        If FileNFO.Extension.ToUpper.EndsWith(".HEX") Then
            If Utilities.IsIntelHex(data_out) Then FileIntelHexFormat = True
        End If
        If FileIntelHexFormat Then
            data_out = Utilities.IntelHexToBin(data_out)   'Convert HEX to bin file
            WriteConsole(String.Format(RM.GetString("fcusb_mem_opened_intel"), FileNFO.Name, Format(data_out.Length, "#,###")))
        Else
            WriteConsole(String.Format(RM.GetString("fcusb_mem_opened_bin"), FileNFO.Name, Format(data_out.Length, "#,###")))
        End If
        Dim BaseAddress As UInt32 = 0 'The starting address to write the data
        Dim DataCount As UInt32 = data_out.Length
        SetStatus(String.Format(RM.GetString("fcusb_mem_selectwriterange"), "EEPROM"))
        Dim dbox As New MemControl_v2.DynamicRangeBox
        If Not dbox.ShowRangeBox(BaseAddress, DataCount, I2CEditor.BaseSize) Then
            SetStatus(RM.GetString("fcusb_mem_writecanceled"))
            Exit Sub
        End If
        If data_out.Length = 0 Then Exit Sub
        SetStatus(String.Format(RM.GetString("fcusb_mem_writing"), FileNFO.Name, "EEPROM", Format(data_out.Length, "#,###")))
        cmdI2cRead.Enabled = False
        cmdI2cWrite.Text = RM.GetString("fcusb_stop")
        MemoryThread = New Threading.Thread(Sub() WriteEEPROMLoop(BaseAddress, data_out, DataCount))
        MemoryThread.Name = "tdI2cWrite"
        MemoryThread.SetApartmentState(Threading.ApartmentState.STA)
        MemoryThread.IsBackground = True
        MemoryThread.Start()
    End Sub

    Private Sub WriteEEPROMLoop(ByVal address As UInt32, ByVal data_out() As Byte, ByVal count As UInt32)
        Dim BytesPerLoop As Integer = 1024
        Abort_I2C_Loop = False
        Dim BytesLeft As Integer = count
        Dim AddressOffset As Integer = 0
        FCUSB_LedBlink()
        SetIc2Progress(0)
        Do Until BytesLeft = 0
            If Abort_I2C_Loop Then Exit Do
            Dim DataOut() As Byte = Nothing
            If (BytesLeft >= BytesPerLoop) Then
                ReDim DataOut(BytesPerLoop - 1)
            Else
                ReDim DataOut(BytesLeft - 1)
            End If
            Array.Copy(data_out, AddressOffset, DataOut, 0, DataOut.Length)
            Dim WriteResult As Boolean = SPI_IF.WriteData_I2C_EEPROM(address + AddressOffset, DataOut)
            AddressOffset += DataOut.Length
            BytesLeft -= DataOut.Length
            Dim percent As Single = CSng((AddressOffset / data_out.Length) * 100) 'Calulate % done
            Dim StatusLocTxt As String = Format(AddressOffset, "#,###") & " of " & Format(data_out.Length, "#,###") & " Bytes" 'Format Status
            SetIc2Progress(Math.Round(percent, 0))
            SetStatus("Writing I2C EEPROM memory " & StatusLocTxt & " (" & Math.Round(percent, 0) & "%)")
            Application.DoEvents()
        Loop
        SetIc2Progress(0)
        USB_LEDOn()
        SetI2CWriteText(RM.GetString("fcusb_Write"))
        SetI2CReadEnable(True)
        I2CEditor.UpdateScreen()
        If Abort_I2C_Loop Then
            SetStatus(RM.GetString("fcusb_mem_cancelled"))
        Else
            SetStatus(String.Format(RM.GetString("fcusb_mem_writecomplete"), Format(data_out.Length, "#,###")))
        End If
    End Sub

    Private Sub ReadEEPROMLoop(ByVal address As UInt32, ByRef data_in() As Byte)
        Dim BytesPerLoop As Integer = 1024
        Abort_I2C_Loop = False
        Dim BytesLeft As Integer = data_in.Length
        Dim AddressOffset As Integer = 0
        FCUSB_LedBlink()
        SetIc2Progress(0)
        Do Until BytesLeft = 0
            If Abort_I2C_Loop Then Exit Do
            Dim DataRead() As Byte = Nothing
            If (BytesLeft >= BytesPerLoop) Then
                ReDim DataRead(BytesPerLoop - 1)
            Else
                ReDim DataRead(BytesLeft - 1)
            End If
            DataRead = SPI_IF.ReadData_I2C_EEPROM(address + AddressOffset, DataRead.Length)
            Array.Copy(DataRead, 0, data_in, AddressOffset, DataRead.Length)
            AddressOffset += DataRead.Length
            BytesLeft -= DataRead.Length
            Dim percent As Single = CSng((AddressOffset / data_in.Length) * 100) 'Calulate % done
            Dim StatusLocTxt As String = Format(AddressOffset, "#,###") & " of " & Format(data_in.Length, "#,###") & " Bytes" 'Format Status
            SetIc2Progress(Math.Round(percent, 0))
            SetStatus("Reading I2C EEPROM memory " & StatusLocTxt & " (" & Math.Round(percent, 0) & "%)")
            Application.DoEvents()
        Loop
        SetIc2Progress(0)
        USB_LEDOn()
        SetI2CReadText(RM.GetString("fcusb_Read"))
        SetI2CWriteEnable(True)
        If Abort_I2C_Loop Then
            SetStatus(RM.GetString("fcusb_mem_usercanceledread"))
        ElseIf data_in Is Nothing Then
            SetStatus(RM.GetString("fcusb_mem_err2"))
        Else
            SetStatus(RM.GetString("fcusb_mem_readcomplete"))
            Dim TargetFilename As String = "EEPROM_" & Utilities.Pad(Hex((address))) & "-" &
                Utilities.Pad(Hex((address + data_in.Length - 2)))
            SaveFileFromRead(data_in, TargetFilename)
        End If
    End Sub

    Private Sub SaveFileFromRead(ByVal data() As Byte, ByVal DefaultName As String)
        Dim Saveme As New SaveFileDialog
        Saveme.AddExtension = True
        Saveme.InitialDirectory = Application.StartupPath
        Saveme.Title = RM.GetString("fcusb_filesave_type")
        Saveme.CheckPathExists = True
        Saveme.FileName = DefaultName
        Dim BinFile As String = "Binary Files (*.bin)|*.bin"
        Dim iHexFiles As String = "Intel Hex Format (*.hex)|*.hex"
        Saveme.Filter = BinFile & "|" & iHexFiles & "|All files (*.*)|*.*"
        If Saveme.ShowDialog = Windows.Forms.DialogResult.OK Then
            If Saveme.FileName.ToUpper.EndsWith(".HEX") Then
                data = Utilities.BinToIntelHex(data)
                WriteConsole(RM.GetString("fcusb_filesave_tohex"))
            End If
            Utilities.FileIO.WriteBytes(data, Saveme.FileName) 'Writes buffer of bytes to Disk
            SetStatus(String.Format(RM.GetString("fcusb_filesave_sucess"), DefaultName))
        Else
            SetStatus(RM.GetString("fcusb_filesave_canceled"))
        End If
    End Sub

    Private Function OpenFileToWrite(ByRef Filename As String) As Boolean
        Dim BinFile As String = "Binary Files (*.bin)|*.bin"
        Dim IHexFormat As String = "Intel Hex Format (*.hex)|*.hex"
        Dim AllFiles As String = "All files (*.*)|*.*"
        Dim OpenMe As New OpenFileDialog
        OpenMe.AddExtension = True
        OpenMe.InitialDirectory = Application.StartupPath
        OpenMe.Title = String.Format(RM.GetString("fcusb_mem_choosefile"), "EEPROM")
        OpenMe.CheckPathExists = True
        OpenMe.Filter = BinFile & "|" & IHexFormat & "|" & AllFiles 'Bin Files, Hex Files, All Files
        If OpenMe.ShowDialog = Windows.Forms.DialogResult.OK Then
            Filename = OpenMe.FileName
            SetStatus(String.Format(RM.GetString("fcusb_mem_filechosen"), "EEPROM"))
            Return True
        Else
            SetStatus(String.Format(RM.GetString("fcusb_mem_usercanwrite"), "EEPROM"))
            Return False
        End If
    End Function

    Private Sub SetIc2Progress(ByVal perc As Integer)
        If pbI2C.InvokeRequired Then
            Dim d As New cbSetIc2Progress(AddressOf SetIc2Progress)
            Me.Invoke(d, New Object() {perc})
        Else
            pbI2C.Value = perc
        End If
    End Sub

    Private Sub SetI2CReadText(ByVal txt As String)
        If cmdI2cRead.InvokeRequired Then
            Dim d As New cbSetI2CReadText(AddressOf SetI2CReadText)
            Me.Invoke(d, New Object() {txt})
        Else
            cmdI2cRead.Text = txt
        End If
    End Sub

    Private Sub SetI2CWriteText(ByVal txt As String)
        If cmdI2cWrite.InvokeRequired Then
            Dim d As New cbSetI2CWriteText(AddressOf SetI2CWriteText)
            Me.Invoke(d, New Object() {txt})
        Else
            cmdI2cWrite.Text = txt
        End If
    End Sub

    Private Sub SetI2CReadEnable(ByVal en As Boolean)
        If cmdI2cRead.InvokeRequired Then
            Dim d As New cbSetI2CReadEnable(AddressOf SetI2CReadEnable)
            Me.Invoke(d, New Object() {en})
        Else
            cmdI2cRead.Enabled = en
        End If
    End Sub

    Private Sub SetI2CWriteEnable(ByVal en As Boolean)
        If cmdI2cWrite.InvokeRequired Then
            Dim d As New cbSetI2CWriteEnable(AddressOf SetI2CWriteEnable)
            Me.Invoke(d, New Object() {en})
        Else
            cmdI2cWrite.Enabled = en
        End If
    End Sub

    Private Sub NewItemClicked(ByVal NewValue As String)
        cbEepromDevices.HideDropDown()
        cbEepromDevices.Text = NewValue
        cbI2C_A2.Enabled = True
        cbI2C_A1.Enabled = True
        cbI2C_A0.Enabled = True
        cbI2C_A2.Checked = False
        cbI2C_A1.Checked = False
        cbI2C_A0.Checked = False
        MySelectedI2CDevice = Nothing
        If cbEepromDevices.Text = "" Then Exit Sub
        For Each dev In MyI2CDevices
            If (dev.Name = cbEepromDevices.Text) Then
                MySelectedI2CDevice = dev
                Exit For
            End If
        Next
        If MySelectedI2CDevice Is Nothing Then Exit Sub
        Dim TotalSize As UInt32 = MySelectedI2CDevice.Size - 1
        TotalSize = (TotalSize >> (MySelectedI2CDevice.AddressSize * 8))
        If (TotalSize And 1) Then cbI2C_A0.Enabled = False
        If (TotalSize And 2) Then cbI2C_A1.Enabled = False
        If (TotalSize And 4) Then cbI2C_A2.Enabled = False
        OnTextChanged(Nothing)
    End Sub


#End Region

#Region "Repeat Feature"
    Private MyLastOperation As MemControl_v2.XFER_Operation
    Private MyLastMemInterfaceIndex As Integer = -1
    Private Delegate Sub cbSuccessfulWriteOperation(ByVal memIndex As Integer, ByVal x As MemControl_v2.XFER_Operation)

    Public Sub SuccessfulWriteOperation(ByVal memIndex As Integer, ByVal x As MemControl_v2.XFER_Operation)
        If Me.InvokeRequired Then
            Dim d As New cbSuccessfulWriteOperation(AddressOf SuccessfulWriteOperation)
            Me.Invoke(d, New Object() {memIndex, x})
        Else
            MyLastMemInterfaceIndex = memIndex
            MyLastOperation = x
            miRepeatWrite.Enabled = True
        End If
    End Sub

    Private Sub miRepeatWrite_Click(sender As Object, e As EventArgs) Handles miRepeatWrite.Click
        WriteConsole("Performing repeat write operation, resetting device")
        Disconnect(True)
        Dim Reconnected As Boolean = False
        For i = 0 To 100 '5 second time-out
            If (Not OperationMode = AvrMode.NotConnected) Then
                Reconnected = True
                Exit For
            Else
                Application.DoEvents()
                Utilities.Sleep(50)
            End If
        Next
        If (Not Reconnected) Then
            WriteConsole("Error, unable to reconnect to FlashcatUSB")
            Exit Sub
        End If
        Reconnected = False
        For i = 0 To 100 '5 second time-out
            If HasMemoryAttached(MyLastMemInterfaceIndex) Then
                Reconnected = True
                Exit For
            Else
                Application.DoEvents()
                Utilities.Sleep(50)
            End If
        Next
        If Not Reconnected Then
            WriteConsole("Error, unable to communicate with memory device")
            Exit Sub
        End If
        Try
            If MyMemDevices(MyLastMemInterfaceIndex).GuiControl Is Nothing Then
                WriteConsole("Error: flash tab does not exist")
                Exit Sub
            End If
            MyMemDevices(MyLastMemInterfaceIndex).GuiControl.PerformWriteOperation(MyLastOperation)
        Catch ex As Exception
        End Try
    End Sub

#End Region

#Region "BIT SWAP FEATURE"

    Private Sub mi_bitswap_none_CheckedChanged(sender As Object, e As EventArgs) Handles mi_bitswap_none.CheckedChanged
        If (Not FormIsLoaded) Then Exit Sub
        If mi_bitswap_none.Checked Then
            mi_bitswap_8bit.Checked = False
            mi_bitswap_4bit.Checked = False
            mi_bitswap_16bit.Checked = False
            MySettings.BIT_SWAP = BitSwapMode.None
            RefreshAllMemoryDevices()
        End If
    End Sub

    Private Sub mi_bitswap_4bit_CheckedChanged(sender As Object, e As EventArgs) Handles mi_bitswap_4bit.CheckedChanged
        If (Not FormIsLoaded) Then Exit Sub
        If mi_bitswap_4bit.Checked Then
            mi_bitswap_none.Checked = False
            mi_bitswap_8bit.Checked = False
            mi_bitswap_16bit.Checked = False
            MySettings.BIT_SWAP = BitSwapMode.Bits_4
            RefreshAllMemoryDevices()
        End If
    End Sub

    Private Sub mi_bitswap_8bit_CheckedChanged(sender As Object, e As EventArgs) Handles mi_bitswap_8bit.CheckedChanged
        If (Not FormIsLoaded) Then Exit Sub
        If mi_bitswap_8bit.Checked Then
            mi_bitswap_none.Checked = False
            mi_bitswap_4bit.Checked = False
            mi_bitswap_16bit.Checked = False
            MySettings.BIT_SWAP = BitSwapMode.Bits_8
            RefreshAllMemoryDevices()
        End If
    End Sub

    Private Sub mi_bitswap_16bit_CheckedChanged(sender As Object, e As EventArgs) Handles mi_bitswap_16bit.CheckedChanged
        If (Not FormIsLoaded) Then Exit Sub
        If mi_bitswap_16bit.Checked Then
            mi_bitswap_none.Checked = False
            mi_bitswap_8bit.Checked = False
            mi_bitswap_4bit.Checked = False
            MySettings.BIT_SWAP = BitSwapMode.Bits_16
            RefreshAllMemoryDevices()
        End If
    End Sub

    Private Sub mi_bitendian_big_CheckedChanged(sender As Object, e As EventArgs) Handles mi_bitendian_big.CheckedChanged
        If (Not FormIsLoaded) Then Exit Sub
        If mi_bitendian_big.Checked Then
            mi_bitendian_little_8.Checked = False
            mi_bitendian_little_16.Checked = False
            mi_bitendian_little_32.Checked = False
            MySettings.BIT_ENDIAN = BitEndianMode.BigEndian
            RefreshAllMemoryDevices()
        End If
    End Sub

    Private Sub mi_bitendian_little_8_CheckedChanged(sender As Object, e As EventArgs) Handles mi_bitendian_little_8.CheckedChanged
        If (Not FormIsLoaded) Then Exit Sub
        If mi_bitendian_little_8.Checked Then
            mi_bitendian_big.Checked = False
            mi_bitendian_little_16.Checked = False
            mi_bitendian_little_32.Checked = False
            MySettings.BIT_ENDIAN = BitEndianMode.LittleEndian_8bit
            RefreshAllMemoryDevices()
        End If
    End Sub

    Private Sub mi_bitendian_little_16_CheckedChanged(sender As Object, e As EventArgs) Handles mi_bitendian_little_16.CheckedChanged
        If (Not FormIsLoaded) Then Exit Sub
        If mi_bitendian_little_16.Checked Then
            mi_bitendian_big.Checked = False
            mi_bitendian_little_8.Checked = False
            mi_bitendian_little_32.Checked = False
            MySettings.BIT_ENDIAN = BitEndianMode.LittleEndian_16bit
            RefreshAllMemoryDevices()
        End If
    End Sub

    Private Sub mi_bitendian_little_32_CheckedChanged(sender As Object, e As EventArgs) Handles mi_bitendian_little_32.CheckedChanged
        If mi_bitendian_little_32.Checked Then
            mi_bitendian_big.Checked = False
            mi_bitendian_little_8.Checked = False
            mi_bitendian_little_16.Checked = False
            MySettings.BIT_ENDIAN = BitEndianMode.LittleEndian_32bit
        End If
    End Sub

    Private Sub mi_bitswap_none_Click(sender As Object, e As EventArgs) Handles mi_bitswap_none.Click
        mi_bitswap_none.Checked = True
    End Sub

    Private Sub mi_bitswap_4bit_Click(sender As Object, e As EventArgs) Handles mi_bitswap_4bit.Click
        mi_bitswap_4bit.Checked = True
    End Sub

    Private Sub mi_bitswap_8bit_Click(sender As Object, e As EventArgs) Handles mi_bitswap_8bit.Click
        mi_bitswap_8bit.Checked = True
    End Sub

    Private Sub mi_bitswap_16bit_Click(sender As Object, e As EventArgs) Handles mi_bitswap_16bit.Click
        mi_bitswap_16bit.Checked = True
    End Sub

    Private Sub MSBToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles mi_bitendian_big.Click
        mi_bitendian_big.Checked = True
    End Sub

    Private Sub mi_bitendian_little_8_Click(sender As Object, e As EventArgs) Handles mi_bitendian_little_8.Click
        mi_bitendian_little_8.Checked = True
    End Sub

    Private Sub mi_bitendian_little_16_Click(sender As Object, e As EventArgs) Handles mi_bitendian_little_16.Click
        mi_bitendian_little_16.Checked = True
    End Sub

    Private Sub mi_bitendian_little_32_Click(sender As Object, e As EventArgs) Handles mi_bitendian_little_32.Click
        mi_bitendian_little_32.Checked = True
    End Sub

    Private Sub SettingsToolStripMenuItem_DropDownOpening(sender As Object, e As EventArgs) Handles SettingsToolStripMenuItem.DropDownOpening
        Select Case MySettings.BIT_SWAP
            Case BitSwapMode.None
                mi_bitswap_none.Checked = True
            Case BitSwapMode.Bits_4
                mi_bitswap_4bit.Checked = True
            Case BitSwapMode.Bits_8
                mi_bitswap_8bit.Checked = True
            Case BitSwapMode.Bits_16
                mi_bitswap_16bit.Checked = True
        End Select
        Select Case MySettings.BIT_ENDIAN
            Case BitEndianMode.BigEndian
                mi_bitendian_big.Checked = True
            Case BitEndianMode.LittleEndian_8bit
                mi_bitendian_little_8.Checked = True
            Case BitEndianMode.LittleEndian_16bit
                mi_bitendian_little_16.Checked = True
            Case BitEndianMode.LittleEndian_32bit
                mi_bitendian_little_32.Checked = True
        End Select
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
        OpenMe.Title = "Open Flash image file to program"
        OpenMe.CheckPathExists = True
        Dim FcFname As String = "Compressed File Archive (*.zip)|*.zip"
        Dim AllF As String = "All files (*.*)|*.*"
        OpenMe.Filter = FcFname & "|" & AllF
        If (OpenMe.ShowDialog = Windows.Forms.DialogResult.OK) Then
            BACKUP_FILE = OpenMe.FileName
            Dim t As New Threading.Thread(AddressOf LoadFlashImgThread)
            t.Name = "ImgLoaderTd"
            t.Start()
        End If
    End Sub

    Private Sub CreateFlashImgThread()
        Try
            Backup_Start()
            If OperationMode = AvrMode.EXPIO AndAlso EXT_IF.MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
                PrintNandFlashDetails()
                NANDBACKUP_CreateBackup()
            Else 'normal flash
                Dim memDev As MemoryDeviceInstance = GetSelectedMemoryInterface()
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
                    SetProgress(Percent)
                    SetStatus("Reading Flash data: " & Format(base_addr, "#,###") & " of " & Format(flash_data.Length, "#,###") & " bytes (" & Percent & "% complete)")
                Loop
                PromptUserForSaveLocation(memDev.Name)
                If BACKUP_FILE = "" Then Exit Sub
                Dim FlashOutputFile As New IO.FileInfo(BACKUP_FILE)
                If FlashOutputFile.Exists Then FlashOutputFile.Delete()
                Dim NandDumpArchive As New ZipHelper(FlashOutputFile)
                NandDumpArchive.AddFile("Main.bin", flash_data)
                NandDumpArchive.Dispose()
                SetProgress(0)
                SetStatus("Flash image saved to disk: " & FlashOutputFile.Name)
            End If
        Catch ex As Exception
        Finally
            Backup_Stop()
        End Try
    End Sub

    Private Sub LoadFlashImgThread()
        Try
            Backup_Start()
            If OperationMode = AvrMode.EXPIO AndAlso EXT_IF.MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
                NANDBACKUP_LoadBackup()
            Else
                Dim memDev As MemoryDeviceInstance = GetSelectedMemoryInterface()
                Dim flash_size As UInt32 = memDev.Size
                Dim sector_count As UInt32 = memDev.GetSectorCount
                Dim bytes_left As UInt32 = flash_size
                Dim base_addr As UInt32 = 0
                Dim FlashBin As New IO.FileInfo(BACKUP_FILE)
                Using FlashImg = New ZipHelper(FlashBin)
                    If FlashImg Is Nothing OrElse FlashImg.Count = 0 Then
                        SetStatus("Error: file is not a valid Flash image")
                        Exit Sub
                    End If
                    Dim main_io As IO.Stream = FlashImg.GetFileStream("Main.bin")
                    Using nand_main As IO.BinaryReader = New IO.BinaryReader(main_io)
                        For i = 0 To sector_count - 1
                            Dim sector_size As UInt32 = memDev.GetSectorSize(i, FlashArea.Main) 'change?
                            Dim data_in() As Byte = nand_main.ReadBytes(sector_size)
                            Dim WriteSuccess As Boolean = memDev.WriteBytes(base_addr, data_in, FlashArea.Main)
                            bytes_left -= sector_size
                            base_addr += sector_size
                            Dim Percent As Integer = Math.Round(((flash_size - bytes_left) / flash_size) * 100)
                            SetProgress(Percent)
                            SetStatus("Writing Flash data: " & Format(base_addr, "#,###") & " of " & Format(flash_size, "#,###") & " bytes (" & Percent & "% complete)")
                        Next
                    End Using
                    main_io.Dispose()
                End Using
                SetProgress(0)
                SetStatus("Successfully programmed Flash image into current device")
            End If
        Catch ex As Exception
        Finally
            Backup_Stop()
            RefreshAllMemoryDevices()
        End Try
    End Sub

    Private Sub Backup_Start()
        If Me.InvokeRequired Then
            Dim d As New OnButtonEnable(AddressOf Backup_Start)
            Me.Invoke(d)
        Else
            For Each device In MyMemDevices
                device.DisableGuiControls()
            Next
            FCUSB_LedBlink()
            SetEnableEraseMenuItem(False)
            SetEnableNandMapMenuItem(False)
            SetBackupToolMenuItem(False)
            BACKUP_OPERATION_RUNNING = True
        End If
    End Sub

    Private Sub Backup_Stop()
        If Me.InvokeRequired Then
            Dim d As New OnButtonEnable(AddressOf Backup_Stop)
            Me.Invoke(d)
        Else
            USB_LEDOn()
            SetProgress(0)
            SetEnableEraseMenuItem(True)
            For Each device In MyMemDevices
                device.EnableGuiControls()
            Next
            OnEnableMenuItems()
            RefreshAllMemoryDevices()
            BACKUP_OPERATION_RUNNING = False
        End If
    End Sub

    Private Sub PrintNandFlashDetails()
        WriteConsole("Creating NAND Image file")
        WriteConsole("Memory device name: " & EXT_IF.MyFlashDevice.NAME)
        WriteConsole("Flash size: " & Format(EXT_IF.MyFlashDevice.FLASH_SIZE, "#,###") & " bytes")
        WriteConsole("Extended/Spare area: " & Format(EXT_IF.NAND_Extra_GetSize(), "#,###") & " bytes")
        WriteConsole("Page size: " & Format(EXT_IF.MyFlashDevice.PAGE_SIZE, "#,###") & " bytes")
        WriteConsole("Block size: " & Format(DirectCast(EXT_IF.MyFlashDevice, NAND_Flash).BLOCK_SIZE, "#,###") & " bytes")
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
            SaveMe.Title = "Select location to save Flash image file"
            SaveMe.CheckPathExists = True
            SaveMe.FileName = default_name
            Dim FcFname As String = "Compressed File Archive (*.zip)|*.zip"
            Dim AllF As String = "All files (*.*)|*.*"
            SaveMe.Filter = FcFname & "|" & AllF
            If SaveMe.ShowDialog = Windows.Forms.DialogResult.OK Then
                BACKUP_FILE = SaveMe.FileName
            Else
                BACKUP_FILE = ""
            End If
        End If
    End Sub

    Private Function NANDBACKUP_CreateMapFile() As String()
        Dim MapFile As New List(Of String)
        MapFile.Add("[MEMORY_DEVICE]" & EXT_IF.MyFlashDevice.NAME)
        MapFile.Add("[MAIN_SIZE]" & EXT_IF.MyFlashDevice.FLASH_SIZE.ToString)
        MapFile.Add("[EXT_SIZE]" & EXT_IF.NAND_Extra_GetSize())
        MapFile.Add("[PAGE_SIZE]" & EXT_IF.MyFlashDevice.PAGE_SIZE)
        MapFile.Add("[PAGE_SIZE_EXT]" & DirectCast(EXT_IF.MyFlashDevice, NAND_Flash).PAGE_SIZE_EXTENDED)
        MapFile.Add("[BLOCK_SIZE]" & DirectCast(EXT_IF.MyFlashDevice, NAND_Flash).BLOCK_SIZE)
        MapFile.Add("[FILE]Main.bin") 'File(0) is the main flash
        MapFile.Add("[FILE]Ext.bin") 'File(1) is the extended data
        For i As UInt32 = 0 To EXT_IF.NAND_MAP.Count - 1
            If Not EXT_IF.NAND_MAP(i).Status = ExtPort.NAND_BLOCK_STATUS.Valid Then
                MapFile.Add("[BAD_BLOCK]" & EXT_IF.NAND_MAP(i).Physical_Start)
            End If
        Next
        Return MapFile.ToArray
    End Function

    Private Sub NANDBACKUP_CreateBackup()
        Dim FlashSize As UInt32 = EXT_IF.MyFlashDevice.FLASH_SIZE
        Dim ExtSize As UInt32 = EXT_IF.NAND_Extra_GetSize()
        Dim FlashImg(FlashSize - 1) As Byte
        Dim ExtImg(ExtSize - 1) As Byte
        Dim Pointer As UInt32 = 0
        For i = 0 To (FlashSize / Mb001) - 1
            Dim read_copy_1() As Byte = Nothing
            Dim read_copy_2() As Byte = Nothing
            Dim ReadErrors As Integer = -1
            Do
                ReadErrors += 1
                If ReadErrors = 3 Then
                    SetStatus("Error creating backup: read memory returned inconsistance results")
                    Exit Sub
                End If
                read_copy_1 = EXT_IF.NAND_ReadBulk(i * Mb001, Mb001, FlashArea.Main)
                read_copy_2 = EXT_IF.NAND_ReadBulk(i * Mb001, Mb001, FlashArea.Main)
            Loop Until Utilities.ArraysMatch(read_copy_1, read_copy_2)
            Array.Copy(read_copy_1, 0, FlashImg, Pointer, read_copy_1.Length)
            Pointer += Mb001
            If (i Mod 1 = 0) OrElse i = 0 Then 'Update status
                Dim Percent As Integer = Math.Round((Pointer / FlashSize) * 100)
                SetProgress(Percent)
                SetStatus("Reading NAND Flash: " & Format(Pointer, "#,###") & " of " & Format(FlashSize, "#,###") & " bytes (" & Percent & "% complete)")
            End If
        Next
        Pointer = 0
        Dim KB64 As UInt32 = 65536 '64KByte
        For i = 0 To (ExtSize / KB64) - 1
            Dim b() As Byte = EXT_IF.NAND_ReadBulk(i * KB64, KB64, FlashArea.OOB)
            Array.Copy(b, 0, ExtImg, Pointer, b.Length)
            Pointer += KB64
            If (i Mod 1 = 0) OrElse i = 0 Then 'Update status
                Dim Percent As Integer = Math.Round((Pointer / ExtImg.Length) * 100)
                SetProgress(Percent)
                SetStatus("Reading NAND spare area: " & Format(Pointer, "#,###") & " of " & Format(ExtImg.Length, "#,###") & " bytes (" & Percent & "% complete)")
            End If
        Next
        Dim MapFile() As String = NANDBACKUP_CreateMapFile()
        Dim MapData() As Byte = Utilities.Bytes.FromCharStringArray(MapFile)
        PromptUserForSaveLocation(EXT_IF.MyFlashDevice.NAME)
        If BACKUP_FILE = "" Then Exit Sub
        Dim NandOutputFile As New IO.FileInfo(BACKUP_FILE)
        If NandOutputFile.Exists Then NandOutputFile.Delete()
        Dim NandDumpArchive As New ZipHelper(NandOutputFile)
        NandDumpArchive.AddFile("Main.bin", FlashImg)
        NandDumpArchive.AddFile("Ext.bin", ExtImg)
        NandDumpArchive.AddFile("NAND.cfg", MapData) 'Adding file automatically saves
        NandDumpArchive.Dispose()
        SetProgress(0)
        SetStatus("NAND Flash image saved to disk: " & NandOutputFile.Name)
    End Sub

    Private Sub NANDBACKUP_LoadBackup()
        Dim NameFileLocation As New IO.FileInfo(BACKUP_FILE)
        Using NandImage = New ZipHelper(NameFileLocation)
            If NandImage Is Nothing OrElse NandImage.Count = 0 Then
                SetStatus("Error: file is not a valid NAND image")
                Exit Sub
            End If
            Dim cfg_io As IO.Stream = NandImage.GetFileStream("NAND.cfg")
            If cfg_io Is Nothing Then
                SetStatus("Error: file is not a valid NAND image")
                Exit Sub
            End If
            Dim config_file As New List(Of String)
            Using nand_cfg As IO.StreamReader = New IO.StreamReader(cfg_io)
                Do Until nand_cfg.Peek = -1
                    config_file.Add(nand_cfg.ReadLine)
                Loop
                nand_cfg.Close()
            End Using
            cfg_io.Dispose()
            WriteConsole("Programming NAND image: " & GetNandCfgParam("MEMORY_DEVICE", config_file.ToArray))
            Dim IMG_SIZE As Long = CLng(GetNandCfgParam("MAIN_SIZE", config_file.ToArray))
            Dim BLKSIZE As UInt32 = CUInt(GetNandCfgParam("BLOCK_SIZE", config_file.ToArray))
            Dim PAGE_SIZE As UInt32 = CUInt(GetNandCfgParam("PAGE_SIZE", config_file.ToArray))
            Dim PAGE_SIZE_EXT As UInt32 = CUInt(GetNandCfgParam("PAGE_SIZE_EXT", config_file.ToArray))
            Dim EXT_SIZE As UInt32 = PAGE_SIZE_EXT - PAGE_SIZE
            Dim PAGE_COUNT As UInt32 = IMG_SIZE / PAGE_SIZE 'Total number of pages in this device 
            Dim BLK_COUNT As UInt32 = IMG_SIZE / BLKSIZE
            Dim PAGES_PER_BLK As UInt32 = PAGE_COUNT / BLK_COUNT
            Dim EXT_PER_BLK As UInt32 = PAGES_PER_BLK * EXT_SIZE
            Dim BBLOCKS() As String = GetNandCfgParams("BAD_BLOCK", config_file.ToArray)
            Dim BAD_BLOCKS(BBLOCKS.Length - 1) As Long
            For i = 0 To BAD_BLOCKS.Count - 1
                BAD_BLOCKS(i) = CLng(BBLOCKS(i))
            Next
            Dim main_io As IO.Stream = NandImage.GetFileStream("Main.bin")
            Dim ext_io As IO.Stream = NandImage.GetFileStream("Ext.bin")
            Using nand_main As IO.BinaryReader = New IO.BinaryReader(main_io)
                Using nand_ext As IO.BinaryReader = New IO.BinaryReader(ext_io)
                    Dim BASE_ADDRESS As Long = 0
                    Dim DST_ADDRESS As Long = 0
                    For i = 0 To BLK_COUNT - 1
                        Dim data_in() As Byte = nand_main.ReadBytes(BLKSIZE)
                        Dim data_ext() As Byte = nand_ext.ReadBytes(EXT_PER_BLK)
                        If IsValidAddress(BASE_ADDRESS, BAD_BLOCKS) Then 'IS VALID BLOCK
                            Dim PHY_ADDR As UInt32 = EXT_IF.NAND_GetAddressMapping(DST_ADDRESS)
                            EXT_IF.NAND_ERASEBLOCK(PHY_ADDR, 0, False) 'We do not need to copy
                            EXT_IF.NAND_WRITEPAGE(PHY_ADDR, data_in, FlashArea.Main)
                            EXT_IF.NAND_WRITEPAGE(PHY_ADDR, data_ext, FlashArea.OOB)
                            DST_ADDRESS += BLKSIZE
                        End If
                        BASE_ADDRESS += BLKSIZE
                        If (i Mod 10 = 0) OrElse i = 0 Then 'Update status
                            Dim Percent As Integer = Math.Round((i / BLK_COUNT) * 100)
                            SetProgress(Percent)
                            SetStatus("Programming NAND Flash: " & Format(BASE_ADDRESS, "#,###") & " of " & Format(IMG_SIZE, "#,###") & " bytes (" & Percent & "% complete)")
                        End If
                    Next
                End Using
            End Using
            SetProgress(0)
            SetStatus("Successfully programmed NAND image into current device")
            main_io.Dispose()
            ext_io.Dispose()
        End Using
    End Sub

    Private Function IsValidAddress(ByVal base As Long, ByRef list() As Long) As Boolean
        For Each addr In list
            If addr = base Then Return False
        Next
        Return True
    End Function

#End Region

#Region "Form Events"
    Public FormIsLoaded As Boolean = False
    Private Delegate Sub cbOperation()

    Public Sub OperationStarted()
        If Me.InvokeRequired Then
            Dim d As New cbOperation(AddressOf OperationStarted)
            Me.Invoke(d)
        Else
            'mi_mode_settings.Enabled = False
            'mi_5V0.Enabled = False
            'mi_3V3.Enabled = False
            'mi_1V8.Enabled = False
            mi_erase_tool.Enabled = False
            SetBackupToolMenuItem(False)
            SetEnableNandMapMenuItem(False)
        End If
    End Sub

    Public Sub OperationStopped()
        If Me.InvokeRequired Then
            Dim d As New cbOperation(AddressOf OperationStopped)
            Me.Invoke(d)
        Else
            'mi_mode_settings.Enabled = True
            'mi_5V0.Enabled = True
            'mi_3V3.Enabled = True
            'mi_1V8.Enabled = True
            OnEnableMenuItems()
        End If
    End Sub

    Private Sub MainForm_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles Me.FormClosing
        AppIsClosing = True
        FCUSB_LedOff() 'Send LED off anyways

        MySettings.Save()
    End Sub

    Private Sub EnglishToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles EnglishToolStripMenuItem.Click
        RM = My.Resources.English.ResourceManager : MySettings.LanguageName = "English"
        ConsoleBox.Items.Clear()
        SetupText()
        Disconnect(True)
    End Sub

    Private Sub GermanToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles GermanToolStripMenuItem.Click
        RM = My.Resources.German.ResourceManager : MySettings.LanguageName = "German"
        ConsoleBox.Items.Clear()
        SetupText()
        Disconnect(True)
    End Sub

    Private Sub SpanishToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles SpanishToolStripMenuItem.Click
        RM = My.Resources.Spanish.ResourceManager : MySettings.LanguageName = "Spanish"
        ConsoleBox.Items.Clear()
        SetupText()
        Disconnect(True)
    End Sub

    Private Sub FrenchToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles FrenchToolStripMenuItem.Click
        RM = My.Resources.French.ResourceManager : MySettings.LanguageName = "French"
        ConsoleBox.Items.Clear()
        SetupText()
        Disconnect(True)
    End Sub

    Private Sub PortugueseToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles PortugueseToolStripMenuItem.Click
        RM = My.Resources.Portuguese.ResourceManager : MySettings.LanguageName = "Portuguese"
        ConsoleBox.Items.Clear()
        SetupText()
        Disconnect(True)
    End Sub

    Private Sub ChineseToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles ChineseToolStripMenuItem.Click
        RM = My.Resources.Chinese.ResourceManager : MySettings.LanguageName = "Chinese"
        ConsoleBox.Items.Clear()
        SetupText()
        Disconnect(True)
    End Sub

#End Region


    Private Sub SetupText()
        SetStatus(RM.GetString("fcusb_welcome_status")) 'Welcome to FlashcatUSB!
        SetConnectionStatus(False)
        MainToolStripMenuItem.Text = RM.GetString("fcusb_menu_main")
        miDetectDevice.Text = RM.GetString("fcusb_menu_detectdevice")
        ExitToolStripMenuItem.Text = RM.GetString("fcusb_menu_exit")
        SettingsToolStripMenuItem.Text = RM.GetString("fcusb_menu_settings")
        ScriptToolStripMenuItem.Text = RM.GetString("fcusb_menu_script")
        CurrentScript_MI.Text = RM.GetString("fcusb_menu_cscript")
        LoadScript_MI.Text = RM.GetString("fcusb_menu_loadscript")
        UnloadScript_MI.Text = RM.GetString("fcusb_menu_unloadscript")
        LanguageToolStripMenuItem.Text = RM.GetString("fcusb_menu_language")
        TabStatus.Text = RM.GetString("fcusb_menu_status")
        TabConsole.Text = RM.GetString("fcusb_menu_console")
        SpiTab.Text = RM.GetString("fcusb_menu_spisettings")
        VerifyMenuItem.Text = RM.GetString("fcusb_menu_verify")
        'SPI Tab
        RadioUseSpiAuto.Text = RM.GetString("fcusb_menu_spi_auto")
        RadioUseSpiSettings.Text = RM.GetString("fcusb_menu_spi_usesettings")
        cbSPI.Text = RM.GetString("fcusb_menu_spi_devcmds")
        lblSpiChipSize.Text = RM.GetString("fcusb_menu_spi_chipsize")
        lblSpiPageProgram.Text = RM.GetString("fcusb_menu_spi_pp")
        lblSpiRead.Text = RM.GetString("fcusb_menu_spi_read")
        'lblSpiMode.Text = RM.GetString("fcusb_menu_spi_spimode")
        lblSpiEraseSize.Text = RM.GetString("fcusb_menu_spi_erasesize")
        lblSpiWriteEn.Text = RM.GetString("fcusb_menu_spi_we")
        lblSpiSectorErase.Text = RM.GetString("fcusb_menu_spi_serase")
        'lblSpiClockDiv.Text = RM.GetString("fcusb_menu_spi_clodiv")
        lblSpiWriteStatus.Text = RM.GetString("fcusb_menu_spi_wstatus")
        lblSpiReadStatus.Text = RM.GetString("fcusb_menu_spi_rstatus")
        lblSpiChipErase.Text = RM.GetString("fcusb_menu_spi_chiperase")
        'lblSpiBitOrder.Text = RM.GetString("fcusb_menu_spi_bitorder")
        'cbUseEnWS.Text = RM.GetString("fcusb_menu_spi_enwrstatus")
        lblSpiProgMode.Text = RM.GetString("fcusb_menu_spi_pmode")
        lblSpiInfo.Text = RM.GetString("fcusb_menu_spi_info")
        'DFU Tab
        AvrTab.Text = RM.GetString("fcusb_menu_avrfw")
        lblAvrFn.Text = RM.GetString("fcusb_dfu_file_default")
        cmdAvrLoad.Text = RM.GetString("fcusb_dfu_loadfile")
        cmdAvrProg.Text = RM.GetString("fcusb_dfu_progfile")
        cmdAvrStart.Text = RM.GetString("fcusb_dfu_startprog")
        'I2C Tab
        cmdI2cWrite.Text = RM.GetString("fcusb_Write")
        cmdI2cRead.Text = RM.GetString("fcusb_Read")
    End Sub

    Public Sub OnDeviceConnected()
        If Me.InvokeRequired Then
            Try
                Dim d As New cbOnDeviceConnected(AddressOf OnDeviceConnected)
                Me.Invoke(d)
            Catch ex As Exception
            End Try
        Else
            miDetectDevice.Enabled = True
            SetConnectionStatus(True)
            ClearStatusMessage()
            USB_LEDOn()
            Try
                If (Not OperationMode = AvrMode.JTAG) Then
                    Dim fw_str As String = SPI_IF.GetAvrVersion()
                    If (HWBOARD = HwVariant.xPort) Then
                        mi_mode_spi.Enabled = False
                        mi_mode_i2c.Enabled = False
                        mi_mode_spieeprom.Enabled = False
                        If (Not OperationMode = AvrMode.EXPIO) Then
                            SetToExtensionMode()
                            OperationMode = AvrMode.EXPIO
                        End If
                    End If
                End If
            Catch ex As Exception
            End Try
            Try
                Select Case OperationMode
                    Case AvrMode.JTAG
                        WriteConsole(RM.GetString("fcusb_initjtag"))
                        Dim FirmVer As String = EJ_IF.GetAvrVersion
                        If FirmVer Is Nothing OrElse FirmVer = "" Then
                            SetStatus(RM.GetString("fcusb_err1"))
                            WriteConsole(RM.GetString("fcusb_err1"))
                            Exit Sub
                        End If
                        UpdateStatusMessage(RM.GetString("cnts_mode"), "Enhanced JTAG")
                        UpdateStatusMessage(RM.GetString("cnts_avrver"), FirmVer)
                        WriteConsole(String.Format(RM.GetString("fcusb_jtag_conn"), FirmVer))
                        If EJ_IF.Init Then
                            WriteConsole(RM.GetString("fcusb_jtagsetup"))
                        Else
                            SetStatus(RM.GetString("fcusb_err4"))
                            UpdateStatusMessage(RM.GetString("cnts_device"), RM.GetString("fcusb_jtag_err1"))
                            WriteConsole(RM.GetString("fcusb_jtag_err1"))
                            Exit Sub
                        End If
                        If (Not (EJ_IF.TargetDevice.IDCODE = 0)) Then
                            UpdateStatusMessage(RM.GetString("cnts_device"), GetManu(EJ_IF.TargetDevice.MANUID) & " " & Hex(EJ_IF.TargetDevice.PARTNU))
                            WriteConsole("Detected CPU ID: 0x" & Hex(EJ_IF.TargetDevice.IDCODE) & " IMP CODE: 0x" & Hex(EJ_IF.TargetDevice.IMPCODE))
                            WriteConsole("Manufacturer ID: 0x" & Hex(EJ_IF.TargetDevice.MANUID) & " Part ID: 0x" & Hex(EJ_IF.TargetDevice.PARTNU))
                            WriteConsole("EJTAG Version support: " & EJ_IF.TargetDevice.IMPVER)
                            If EJ_IF.TargetDevice.NoDMA Then
                                WriteConsole(RM.GetString("fcusb_jtag_nodma"))
                            Else
                                WriteConsole(RM.GetString("fcusb_jtag_dma"))
                            End If
                            LoadScripts(EJ_IF.TargetDevice.IDCODE)
                        Else
                            WriteConsole("Device did not return CHIP IDCODE")
                            UpdateStatusMessage(RM.GetString("cnts_device"), "Unknown JTAG device")
                        End If
                        LoadScript_MI.Enabled = True
                        SetStatus(RM.GetString("fcusb_jtag_ready"))
                    Case AvrMode.SPI
                        UpdateStatusMessage(RM.GetString("cnts_mode"), "Serial Programmable Interface (SPI)")
                        UpdateStatusMessage(RM.GetString("cnts_avrver"), SPI_IF.GetAvrVersion)
                        WriteConsole(String.Format(RM.GetString("fcusb_spi_ver"), SPI_IF.GetAvrVersion))
                        Dim AvrVer As String = SPI_IF.GetAvrVersion
                        Dim AvrNow As Single = Utilities.StringToSingle(AvrVer)
                        If (AvrNow < MinBoardRev) Then
                            WriteConsole("Software requires firmware version " & MinBoardRev.ToString & " or newer")
                            SetStatus("FlashcatUSB firmware is out of date, please update.")
                            Exit Sub
                        End If
                        If MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI_EEPROM Then
                            If MySettings.SPI_EEPROM = FlashcatSettings.SPI_EEPROM_DEVICE.None Then
                                SetStatus("Please select a SPI EEPROM from the Settings Menu")
                                Exit Sub
                            End If
                        End If
                        UpdateStatusMessage(RM.GetString("cnts_device"), RM.GetString("fcusb_spi_scanning"))
                        SPI_Detect()
                        Select Case SPI_IF.MyFlashStatus
                            Case SPI.ConnectionStatus.NotDetected
                                UpdateStatusMessage(RM.GetString("cnts_device"), RM.GetString("fcusb_noflash"))
                                ShowSpiSettings(True)
                                SetStatus(RM.GetString("fcusb_spi_err1"))
                                Exit Sub
                            Case SPI.ConnectionStatus.NotSupported
                                UpdateStatusMessage(RM.GetString("cnts_device"), SPI_IF.DeviceName)
                                ShowSpiSettings(True)
                                SetStatus(RM.GetString("fcusb_spi_err2"))
                                Exit Sub
                            Case SPI.ConnectionStatus.Supported
                                Dim JEDEC_ID As String = Hex(SPI_IF.MyFlashDevice.MFG_CODE).PadLeft(2, "0") & " " & Hex(SPI_IF.MyFlashDevice.ID1).PadLeft(4, "0")
                                If MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI Then
                                    UpdateStatusMessage(RM.GetString("cnts_device"), "SPI compatible device (CHIP ID: " & JEDEC_ID & ")")
                                ElseIf MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI_EEPROM Then
                                    UpdateStatusMessage(RM.GetString("cnts_device"), "SPI compatible EEPROM device")
                                Else
                                    UpdateStatusMessage(RM.GetString("cnts_device"), "SPI compatible device (CHIP ID: " & JEDEC_ID & ")")
                                End If
                                UpdateStatusMessage(RM.GetString("cnts_flashsize"), Format(SPI_IF.DeviceSize, "#,###") & " bytes")
                                Dim md As MemoryDeviceInstance = AddMemoryDevice(DeviceTypes.SPI, 0, SPI_IF.DeviceSize, Nothing, SPI_IF.DeviceName)
                                If Not SPI_IF.MyFlashDevice.ERASE_REQUIRED Then
                                    md.GuiControl.AllowFullErase = False
                                End If
                                If Not ScriptIsReloading Then
                                    Dim JedecIdUint32 As UInt32 = Utilities.HexToUInt(JEDEC_ID.Replace(" ", ""))
                                    LoadScripts(JedecIdUint32)
                                Else
                                    ScriptIsReloading = False
                                End If
                                LoadScript_MI.Enabled = True
                                If Not (SPI_IF.MyFlashDevice.VENDOR_SPECIFIC = VENDOR_FEATURE.NotSupported) Then
                                    Dim setting_ctrl As Control = Nothing
                                    If (SPI_IF.MyFlashDevice.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron) Then
                                        setting_ctrl = New NonVol_1
                                    ElseIf (SPI_IF.MyFlashDevice.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion) Then
                                        setting_ctrl = New NonVol_2
                                    End If
                                    Dim setting_tab As New TabPage
                                    setting_tab.BackColor = SystemColors.Control
                                    setting_tab.Text = "Nonvolatile Configuration"
                                    setting_tab.Name = "VENDOR"
                                    setting_tab.Controls.Add(setting_ctrl)
                                    MyTabs.TabPages.Add(setting_tab)
                                End If
                                SetStatus(RM.GetString("fcusb_spi_ready"))
                        End Select
                    Case AvrMode.EXPIO
                        UpdateStatusMessage(RM.GetString("cnts_mode"), "Extension Port mode")
                        WriteConsole(String.Format(RM.GetString("fcusb_cmos_ver"), SPI_IF.GetAvrVersion))
                        UpdateStatusMessage(RM.GetString("cnts_avrver"), SPI_IF.GetAvrVersion)
                        Dim AvrVer As String = SPI_IF.GetAvrVersion
                        Dim AvrNow As Single = Utilities.StringToSingle(AvrVer)
                        If (AvrNow < MinBoardRev) Then
                            WriteConsole("Software requires firmware version " & MinBoardRev.ToString & " or newer")
                            SetStatus("FlashcatUSB firmware is out of date, please update.")
                            Exit Sub
                        End If
                        UpdateStatusMessage(RM.GetString("cnts_device"), "Scanning for compatible memory device")
                        SPI_Detect()
                        Select Case SPI_IF.MyFlashStatus
                            Case SPI.ConnectionStatus.ExtIoNotConnected
                                UpdateStatusMessage(RM.GetString("cnts_device"), "(Unable to connect to EXT I/O)")
                                SetStatus("FlashcatUSB unable to connect to Extension I/O board")
                                Exit Sub
                            Case SPI.ConnectionStatus.NotDetected
                                UpdateStatusMessage(RM.GetString("cnts_device"), RM.GetString("fcusb_noflash")) '"(no flash detected)"
                                SetStatus("Extension I/O board unable to detect Flash device")
                                Exit Sub
                            Case SPI.ConnectionStatus.NotSupported
                                'UpdateStatusMessage(RM.GetString("cnts_device"), SPI_IF.DeviceName)
                                Dim JEDEC_ID As String = Hex(EXT_IF.CHIPID_MFG).PadLeft(2, "0") & " " & Hex(EXT_IF.CHIPID_PART).PadLeft(4, "0")
                                UpdateStatusMessage(RM.GetString("cnts_device"), "Flash device (CHIP ID: " & JEDEC_ID & ")")
                                SetStatus(RM.GetString("fcusb_spi_err2"))
                                Exit Sub
                            Case SPI.ConnectionStatus.Supported
                                Dim JEDEC_ID As String = Hex(EXT_IF.CHIPID_MFG).PadLeft(2, "0") & " " & Hex(EXT_IF.CHIPID_PART).PadLeft(4, "0")
                                UpdateStatusMessage(RM.GetString("cnts_device"), "Flash device (CHIP ID: " & JEDEC_ID & ")")
                                UpdateStatusMessage(RM.GetString("cnts_flashsize"), Format(EXT_IF.GetFlashSize, "#,###") & " bytes")
                                Application.DoEvents()
                                Dim mem_ind As Integer = -1
                                Dim mem_dev As MemoryDeviceInstance = AddMemoryDevice(DeviceTypes.ExtIO, 0, EXT_IF.GetFlashSize, mem_ind, EXT_IF.MyFlashDevice.NAME)
                                mem_dev.FlashType = EXT_IF.MyFlashDevice.FLASH_TYPE
                                Dim JedecIdUint32 As UInt32 = (CUInt(EXT_IF.MyFlashDevice.MFG_CODE) << 24) + EXT_IF.MyFlashDevice.ID1
                                LoadScripts(JedecIdUint32)
                                LoadScript_MI.Enabled = True
                                SetStatus(RM.GetString("fcusb_cmos_ready"))
                                If EXT_IF.MyFlashDevice.FLASH_TYPE = FlashMemory.MemoryType.SLC_NAND Then
                                    SetEnableNandMapMenuItem(True)
                                    Dim nand As NAND_Flash = DirectCast(EXT_IF.MyFlashDevice, NAND_Flash)
                                    Dim page_size As UInt16 = EXT_IF.MyFlashDevice.PAGE_SIZE
                                    Dim ext_size As UInt16 = (nand.PAGE_SIZE_EXTENDED - page_size)
                                    Dim pages As UInt32 = EXT_IF.GetFlashSize / page_size
                                    Dim pages_per_block As UInt32 = (nand.BLOCK_SIZE / page_size)
                                    mem_dev.GuiControl.AddExtendedArea(pages, page_size, ext_size, pages_per_block)
                                End If
                        End Select
                    Case AvrMode.I2C
                        UpdateStatusMessage(RM.GetString("cnts_mode"), "Inter-Integrated Circuit (I²C)")
                        WriteConsole(String.Format(RM.GetString("fcusb_spi_ver"), SPI_IF.GetAvrVersion))
                        UpdateStatusMessage(RM.GetString("cnts_avrver"), SPI_IF.GetAvrVersion)
                        SetStatus(RM.GetString("fcusb_spi_ready"))
                        SetupI2C_OnConnect()
                    Case AvrMode.DFU
                        Dim FlashSizeTot As Integer = DFU_IF.GetFlashSize + DFU_IF.GetBootloaderSize
                        WriteConsole(RM.GetString("fcusb_dfu_conn"))
                        UpdateStatusMessage(RM.GetString("cnts_mode"), "Device Firmware Upgrade (DFU)")
                        UpdateStatusMessage(RM.GetString("cnts_device"), DFU_IF.GetAtmelPart)
                        UpdateStatusMessage(RM.GetString("cnts_flashtype"), "ATMEL AVR Flash")
                        UpdateStatusMessage(RM.GetString("cnts_flashsize"), Format(FlashSizeTot, "#,###") & " bytes")
                        If Not MyTabs.Controls.Contains(AvrTab) Then
                            MyTabs.Controls.Add(AvrTab) 'If not added, Add it
                        End If
                        AvrDFUconnect()
                        SetStatus(RM.GetString("fcusb_dfu_ready"))
                End Select
            Catch ex As Exception
                OnDeviceDisconnected()
            End Try
        End If
    End Sub

    Public Sub OnDeviceDisconnected()
        If Me.InvokeRequired Then
            Dim d As New cbOnDeviceDisconnected(AddressOf OnDeviceDisconnected)
            Me.Invoke(d)
        Else
            miRepeatWrite.Enabled = False
            SetConnectionStatus(False)
            SetStatus(RM.GetString("fcusb_waitingforusb"))
            ScriptEngine.Abort()
            ClearStatusMessage()
            CurrentScript_MI.Enabled = False
            LoadScript_MI.Enabled = False
            UnloadScript_MI.Enabled = False
            miDetectDevice.Enabled = False
            GUI.mi_erase_tool.Enabled = False
            mi_mode_spi.Enabled = True
            mi_mode_i2c.Enabled = True
            mi_mode_spieeprom.Enabled = True
            SetEnableNandMapMenuItem(False)
            SetBackupToolMenuItem(False)
            SetEnableEraseMenuItem(False)
            ScriptEngine.UnloadDeviceScript() 'Unloads the device script and any objects/tabs
            RemoveAllTabs()
        End If
    End Sub

    Public Sub PrintConsole(ByVal Msg As String)
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
        End If
    End Sub

    Public Sub SetStatus(ByVal Msg As String)
        If Me.InvokeRequired Then
            Dim d As New cbSetStatus(AddressOf SetStatus)
            Me.Invoke(d, New Object() {[Msg]})
        Else
            Me.Status.Text = Msg
        End If
    End Sub

    Public Sub SetConnectionStatus(ByVal Connected As Boolean)
        If Me.InvokeRequired Then
            Dim d As New cbSetConnectionStatus(AddressOf SetConnectionStatus)
            Me.Invoke(d, New Object() {Connected})
        Else
            If Connected Then
                Me.lblStatus.Text = RM.GetString("fcusb_status_connected")
            Else
                Me.lblStatus.Text = RM.GetString("fcusb_status_disconnect")
            End If
            Application.DoEvents()
        End If
    End Sub

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

    Private Sub ExitToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ExitToolStripMenuItem.Click
        Me.Close()
    End Sub
    'Saves the console log to text
    Private Sub cmdSaveLog_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdSaveLog.Click
        If ConsoleBox.Items.Count = 0 Then Exit Sub
        Dim fDiag As New SaveFileDialog
        fDiag.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        fDiag.Title = RM.GetString("fcusb_console_save")
        fDiag.FileName = "FCUSB.console.log.txt"
        If fDiag.ShowDialog = Windows.Forms.DialogResult.OK Then
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

    Private Sub VerifyMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles VerifyMenuItem.Click
        Dim NewValue As Boolean = Not MySettings.VERIFY_WRITE
        MySettings.VERIFY_WRITE = NewValue
        VerifyMenuItem.Checked = NewValue
    End Sub

    Private Sub pb_logo_Click(sender As Object, e As EventArgs) Handles pb_logo.DoubleClick
        If Me.Cursor = Windows.Forms.Cursors.Hand Then
            Dim sInfo As New ProcessStartInfo("http://www.embeddedcomputers.net/products/FlashcatUSB/")
            Process.Start(sInfo)
        End If
    End Sub

    Private Sub pb_logo_MouseLeave(sender As Object, e As EventArgs) Handles pb_logo.MouseLeave
        Me.Cursor = Windows.Forms.Cursors.Arrow
    End Sub

    Private Sub pb_logo_MouseMove(sender As Object, e As MouseEventArgs) Handles pb_logo.MouseMove
        Dim x As Integer = (Me.Width / 2) - 130
        If e.X >= x AndAlso e.X <= x + 222 Then
            Me.Cursor = Windows.Forms.Cursors.Hand
        Else
            Me.Cursor = Windows.Forms.Cursors.Arrow
        End If
    End Sub

    Private Sub EraseChipToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles mi_erase_tool.Click
        Dim t As New Threading.Thread(AddressOf EraseChipThread)
        t.Name = "tdChipErase"
        t.Start()
    End Sub

    Private Sub EraseChipThread()
        Try
            SetEnableEraseMenuItem(False)
            Dim memDev As MemoryDeviceInstance = GetSelectedMemoryInterface()
            If memDev Is Nothing Then Exit Sub
            FCUSB_LedBlink()
            If MsgBox("This action will permanently delete all data.", MsgBoxStyle.YesNo, "Confirm erase of " & memDev.Name) = MsgBoxResult.Yes Then
                WriteConsole("Sent memory erase command to device: " & memDev.Name)
                SetStatus("Erasing Flash memory device... (this may take up to 2 minutes)")
                memDev.DisableGuiControls()
                memDev.EraseBulk()
                memDev.WaitUntilReady()
                memDev.EnableGuiControls()
                memDev.GuiControl.RefreshView()
            End If
            USB_LEDOn()
            OnEnableMenuItems()
            SetStatus("Erase operation successfully completed")
            mi_erase_tool.Enabled = True
        Catch ex As Exception
        End Try
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

    Public Sub SetEnableEraseMenuItem(ByVal Enabled As Boolean)
        If Me.InvokeRequired Then
            Dim d As New cbEnableChipToolMenu(AddressOf SetEnableEraseMenuItem)
            Me.Invoke(d, {Enabled})
        Else
            mi_erase_tool.Enabled = False
            If Enabled Then
                Dim memDev As MemoryDeviceInstance = GetSelectedMemoryInterface()
                If memDev Is Nothing Then Exit Sub
                If memDev.GuiControl.AllowFullErase Then
                    mi_erase_tool.Enabled = True
                End If
            End If
        End If
    End Sub

    Public Sub SetEnableNandMapMenuItem(ByVal Enabled As Boolean)
        If Me.InvokeRequired Then
            Dim d As New cbEnableChipToolMenu(AddressOf SetEnableNandMapMenuItem)
            Me.Invoke(d, {Enabled})
        Else
            mi_nand_map.Enabled = Enabled
        End If
    End Sub

    Public Sub SetBackupToolMenuItem(ByVal Enabled As Boolean)
        If Me.InvokeRequired Then
            Dim d As New cbEnableChipToolMenu(AddressOf SetBackupToolMenuItem)
            Me.Invoke(d, {Enabled})
        Else
            mi_write_img.Enabled = Enabled
            mi_create_img.Enabled = Enabled
        End If
    End Sub

    Private Sub NormalSPIModeToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles mi_mode_spi.Click
        Try
            UncheckAllModes()
            mi_mode_spi.Checked = True
            If (Not MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI) Then
                MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI
                Disconnect()
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub I2CEEPROMModeToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles mi_mode_i2c.Click
        Try
            UncheckAllModes()
            mi_mode_i2c.Checked = True
            MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.I2C_EEPROM
            ShowSpiSettings(False)
        Catch ex As Exception
        End Try
    End Sub

    Private Sub miSPIEEPROM_Click(sender As Object, e As EventArgs) Handles mi_mode_spieeprom.Click
        Try
            UncheckAllModes()
            mi_mode_spieeprom.Checked = True
            mi_eeprom.Enabled = True
            MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI_EEPROM
            ShowSpiSettings(False)
            ShowI2CSettings(False)
        Catch ex As Exception
        End Try
    End Sub

    Private Sub mi_ext_mode_Click(sender As Object, e As EventArgs) Handles mi_mode_ext.Click
        SetToExtensionMode()
    End Sub

    Private Sub SetToExtensionMode()
        Try
            UncheckAllModes()
            mi_mode_ext.Checked = True
            mi_vpp.Enabled = True
            mi_nand.Enabled = True
            MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.EXTIO
            ShowSpiSettings(False)
            ShowI2CSettings(False)
        Catch ex As Exception
        End Try
    End Sub

    Private Sub UncheckAllModes()
        Try
            mi_mode_spi.Checked = False
            mi_mode_ext.Checked = False
            mi_mode_i2c.Checked = False
            mi_mode_spieeprom.Checked = False
            mi_vpp.Enabled = False
            mi_nand.Enabled = False
            mi_eeprom.Enabled = False
        Catch ex As Exception
        End Try
    End Sub

    Private Sub UncheckAllEeepromDevices()
        Try
            mi_spieeprom_none.Checked = False
            mi_spieeprom_NRF24LE1.Checked = False
            mi_spieeprom_nRF24LUIP_16KB.Checked = False
            mi_spieeprom_nRF24LUIP_32KB.Checked = False
            mi_spieeprom_AT25010A.Checked = False
            mi_spieeprom_AT25020A.Checked = False
            mi_spieeprom_AT25040A.Checked = False
            mi_spieeprom_AT25080.Checked = False
            mi_spieeprom_AT25160.Checked = False
            mi_spieeprom_AT25320.Checked = False
            mi_spieeprom_AT25640.Checked = False
            mi_spieeprom_AT25128B.Checked = False
            mi_spieeprom_AT25256B.Checked = False
            mi_spieeprom_AT25512.Checked = False
            mi_spieeprom_STM95010.Checked = False
            mi_spieeprom_STM95020.Checked = False
            mi_spieeprom_STM95040.Checked = False
            mi_spieeprom_STM95080.Checked = False
            mi_spieeprom_STM95160.Checked = False
            mi_spieeprom_STM95320.Checked = False
            mi_spieeprom_STM95640.Checked = False
            mi_spieeprom_STM95128.Checked = False
            mi_spieeprom_STM95256.Checked = False
            mi_spieeprom_STM95512.Checked = False
            mi_spieeprom_STM95M01.Checked = False
            mi_spieeprom_STM95M02.Checked = False
            mi_spieeprom_25AA512.Checked = False
            mi_spieeprom_M25AA160A.Checked = False
            mi_spieeprom_M25AA160B.Checked = False
        Catch ex As Exception
        End Try
    End Sub

    Private Sub SetCheckmarkForMode()
        UncheckAllModes()
        ShowSpiSettings(False)
        If MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI Then
            mi_mode_spi.Checked = True
            ShowSpiSettings(True)
        ElseIf MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.SPI_EEPROM Then
            mi_mode_spieeprom.Checked = True
            mi_eeprom.Enabled = True
        ElseIf MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.I2C_EEPROM Then
            mi_mode_i2c.Checked = True
        ElseIf MySettings.OPERATION_MODE = FlashcatSettings.DeviceMode.EXTIO Then
            mi_mode_ext.Checked = True
            mi_vpp.Enabled = True
            mi_nand.Enabled = True
        End If
        UncheckAllEeepromDevices()
        Select Case MySettings.SPI_EEPROM
            Case FlashcatSettings.SPI_EEPROM_DEVICE.None
                mi_spieeprom_none.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.nRF24LE1
                mi_spieeprom_NRF24LE1.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.nRF24LUIP_16KB
                mi_spieeprom_nRF24LUIP_16KB.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.nRF24LUIP_32KB
                mi_spieeprom_nRF24LUIP_32KB.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25010A
                mi_spieeprom_AT25010A.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25020A
                mi_spieeprom_AT25020A.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25040A
                mi_spieeprom_AT25040A.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25080
                mi_spieeprom_AT25080.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25160
                mi_spieeprom_AT25160.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25320
                mi_spieeprom_AT25320.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25640
                mi_spieeprom_AT25640.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25128B
                mi_spieeprom_AT25128B.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25256B
                mi_spieeprom_AT25256B.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.AT25512
                mi_spieeprom_AT25512.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95010
                mi_spieeprom_STM95010.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95020
                mi_spieeprom_STM95020.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95040
                mi_spieeprom_STM95040.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95080
                mi_spieeprom_STM95080.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95160
                mi_spieeprom_STM95160.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95320
                mi_spieeprom_STM95320.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95640
                mi_spieeprom_STM95640.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95128
                mi_spieeprom_STM95128.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95256
                mi_spieeprom_STM95256.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95512
                mi_spieeprom_STM95512.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95M01
                mi_spieeprom_STM95M01.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M95M02
                mi_spieeprom_STM95M02.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M25AA512
                mi_spieeprom_25AA512.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M25AA160A
                mi_spieeprom_M25AA160A.Checked = True
            Case FlashcatSettings.SPI_EEPROM_DEVICE.M25AA160B
                mi_spieeprom_M25AA160B.Checked = True
        End Select
    End Sub

    Private Sub RefreshAllMemoryDevices()
        For Each memchip In MyMemDevices    'Refresh hexeditor
            memchip.GuiControl.RefreshView()
        Next
    End Sub

    Private Sub mi_RefreshFlash_Click(sender As Object, e As EventArgs) Handles mi_RefreshFlash.Click
        RefreshAllMemoryDevices()
    End Sub

    Private Sub MyTabs_SelectedIndexChanged(sender As Object, e As EventArgs) Handles MyTabs.SelectedIndexChanged
        OnEnableMenuItems()
    End Sub

    Private Delegate Sub cbOnEnableMenuItems()
    Private Sub OnEnableMenuItems()
        If Me.InvokeRequired Then
            Dim d As New cbOnEnableMenuItems(AddressOf OnEnableMenuItems)
            Me.Invoke(d)
        Else
            Try
                SetEnableEraseMenuItem(False)
                SetBackupToolMenuItem(False)
                If BACKUP_OPERATION_RUNNING Then Exit Sub
                If OperationMode = AvrMode.EXPIO Then
                    If EXT_IF.MyFlashDevice IsNot Nothing AndAlso EXT_IF.MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
                        If MyMemDevices.Count > 0 Then
                            If Not MyMemDevices(0).IsBusy Then SetEnableNandMapMenuItem(True)
                        End If
                    End If
                End If
                If MyTabs.SelectedTab.Tag IsNot Nothing Then
                    Dim o As Object = MyTabs.SelectedTab.Tag
                    If TryCast(o, MemoryDeviceInstance) IsNot Nothing Then
                        Dim memDev As MemoryDeviceInstance = DirectCast(o, MemoryDeviceInstance)
                        If memDev.IsBusy Then Exit Sub
                        SetBackupToolMenuItem(True)
                        SetEnableEraseMenuItem(True)
                    End If
                End If
            Catch ex As Exception
            End Try
        End If
    End Sub

    Private Sub mi_NandPreserve_Click(sender As Object, e As EventArgs) Handles mi_NandPreserve.Click
        MySettings.NAND_Preserve = Not MySettings.NAND_Preserve
        mi_NandPreserve.Checked = MySettings.NAND_Preserve
    End Sub

    Private Sub mi_so44_normal_Click(sender As Object, e As EventArgs) Handles mi_vpp_vcc_normal.Click
        MySettings.EXTIO_VPP = FlashcatSettings.SO44_VPP_SETTING.Disabled
        mi_vpp_vcc_normal.Checked = True
        mi_so44_12v_write.Checked = False
    End Sub

    Private Sub mi_so44_12v_write_Click(sender As Object, e As EventArgs) Handles mi_so44_12v_write.Click
        MySettings.EXTIO_VPP = FlashcatSettings.SO44_VPP_SETTING.Write_12v
        mi_vpp_vcc_normal.Checked = False
        mi_so44_12v_write.Checked = True
    End Sub

    Private Sub cmd_console_clear_Click(sender As Object, e As EventArgs) Handles cmd_console_clear.Click
        ConsoleBox.Items.Clear()
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
            SetStatus("Console text copied to the clipboard")
        Catch ex As Exception
        End Try
    End Sub

    Private Sub mi_blkmap_disabled_Click(sender As Object, e As EventArgs) Handles mi_blkmap_disabled.Click
        MySettings.NAND_BadBlockManager = FlashcatSettings.BadBlockMarker.Disabled
        BlockManagerItem_Select()
    End Sub

    Private Sub mi_blkmap_opt1_Click(sender As Object, e As EventArgs) Handles mi_blkmap_opt1.Click
        MySettings.NAND_BadBlockManager = FlashcatSettings.BadBlockMarker.SixthByte_FirstPage
        BlockManagerItem_Select()
    End Sub

    Private Sub mi_blkmap_opt2_Click(sender As Object, e As EventArgs) Handles mi_blkmap_opt2.Click
        MySettings.NAND_BadBlockManager = FlashcatSettings.BadBlockMarker.FirstSixthByte_FirstPage
        BlockManagerItem_Select()
    End Sub

    Private Sub mi_blkmap_opt3_Click(sender As Object, e As EventArgs) Handles mi_blkmap_opt3.Click
        MySettings.NAND_BadBlockManager = FlashcatSettings.BadBlockMarker.FirstByte_LastPage
        BlockManagerItem_Select()
    End Sub

    Private Sub BlockMangerItems_UncheckAll()
        mi_blkmap_disabled.Checked = False
        mi_blkmap_opt1.Checked = False
        mi_blkmap_opt2.Checked = False
        mi_blkmap_opt3.Checked = False
    End Sub

    Private Sub BlockManagerItem_Select()
        BlockMangerItems_UncheckAll()
        Select Case MySettings.NAND_BadBlockManager
            Case FlashcatSettings.BadBlockMarker.Disabled
                mi_blkmap_disabled.Checked = True
            Case FlashcatSettings.BadBlockMarker.SixthByte_FirstPage
                mi_blkmap_opt1.Checked = True
            Case FlashcatSettings.BadBlockMarker.FirstSixthByte_FirstPage
                mi_blkmap_opt2.Checked = True
            Case FlashcatSettings.BadBlockMarker.FirstByte_LastPage
                mi_blkmap_opt3.Checked = True
        End Select
    End Sub

    Private Sub mi_nand_map_Click(sender As Object, e As EventArgs) Handles mi_nand_map.Click
        Try
            Dim n As New NAND_Block_Management
            n.ShowDialog()
            EXT_IF.ProcessNandMap()

            Dim flash_size_processed As UInt32 = EXT_IF.GetFlashSize()
            UpdateStatusMessage("Flash memory size", Format(flash_size_processed, "#,###") & " bytes")
            Dim mem_dev As MemoryDeviceInstance = GetDeviceInstance(0)
            mem_dev.Size = flash_size_processed
            Dim nand As NAND_Flash = DirectCast(EXT_IF.MyFlashDevice, NAND_Flash)
            Dim page_size As UInt16 = EXT_IF.MyFlashDevice.PAGE_SIZE
            Dim ext_size As UInt16 = (nand.PAGE_SIZE_EXTENDED - page_size)
            Dim pages As UInt32 = flash_size_processed / page_size
            mem_dev.GuiControl.InitMemoryDevice(mem_dev.Name, mem_dev.BaseAddress, mem_dev.Size, MemControl_v2.access_mode.ReadWrite)
            mem_dev.GuiControl.AddExtendedArea(pages, page_size, ext_size, (nand.BLOCK_SIZE / page_size))
            mem_dev.GuiControl.RefreshView()
        Catch ex As Exception
        End Try
    End Sub

End Class
