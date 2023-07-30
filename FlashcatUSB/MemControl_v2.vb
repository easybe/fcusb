'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2019 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK

Imports FlashcatUSB.ECC_LIB

Public Class MemControl_v2
    Private ParentMemDevice As MemoryInterface.MemoryDeviceInstance

    Private AreaSelected As FlashMemory.FlashArea = FlashMemory.FlashArea.Main
    Private FCUSB As USB.HostClient.FCUSB_DEVICE
    Private FlashName As String 'Contains the MFG and PART NUMBER
    Private FlashAvailable As Long  'The total bytes available for the hex editor
    Private FlashBase As Long 'Offset if this device is not at 0x0
    Private HexLock As New Object 'Used to lock the gui
    Private EnableChipErase As Boolean = True 'EEPROM devices do not allow this
    Private DisplayIdent As Boolean = False

    Public LAST_WRITE_OPERATION As XFER_Operation = Nothing

    Public Event SetExternalProgress(ByVal Percent As Integer)

    Public Event WriteConsole(msg As String) 'Writes the console/windows console
    Public Event SetStatus(msg As String) 'Sets the text on the status bar
    Public Event ReadMemory(base_addr As Long, ByRef data() As Byte, ByVal area As FlashMemory.FlashArea) 'We want to get data from the normal memory area
    Public Event ReadStream(data_stream As IO.Stream, f_params As ReadParameters)
    Public Event WriteMemory(base_addr As Long, data() As Byte, verify_wr As Boolean, area As FlashMemory.FlashArea, ByRef Successful As Boolean) 'Write data to the normal area
    Public Event WriteStream(data_stream As IO.Stream, f_params As WriteParameters, ByRef Successful As Boolean)
    Public Event GetSectorCount(ByRef count As UInt32)
    Public Event GetSectorIndex(addr As Long, area As FlashMemory.FlashArea, ByRef sector_int As UInt32)
    Public Event GetSectorBaseAddress(sector_int As UInt32, area As FlashMemory.FlashArea, ByRef addr As Long)
    Public Event GetSectorSize(sector_int As UInt32, area As FlashMemory.FlashArea, ByRef sector_size As UInt32)
    Public Event EraseMemory()
    Public Event SuccessfulWrite(ByVal mydev As USB.HostClient.FCUSB_DEVICE, ByVal x As XFER_Operation)
    Public Event GetEccLastResult(ByRef result As ECC_DECODE_RESULT)

    Public Property IN_OPERATION As Boolean = False
    Public Property USER_HIT_CANCEL As Boolean = False
    Public Property MY_ACCESS As access_mode = access_mode.Writable

    Public ReadingParams As ReadParameters
    Public WritingParams As WriteParameters

    Public StatusLabels(4) As ToolStripStatusLabel 'This contains our status labels

    Sub New(dev As MemoryInterface.MemoryDeviceInstance)
        Me.ParentMemDevice = dev
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
    End Sub

    Private Sub MemControl_v2_Load(ByVal sender As Object, e As EventArgs) Handles Me.Load
        Me.menu_tip.SetToolTip(Me.cmd_read, RM.GetString("mc_button_read"))
        Me.menu_tip.SetToolTip(Me.cmd_write, RM.GetString("mc_button_write"))
        Me.menu_tip.SetToolTip(Me.cmd_erase, RM.GetString("mc_button_erase"))
        Me.menu_tip.SetToolTip(Me.cmd_compare, RM.GetString("mc_button_compare"))
        StatusBar_Create()
        SetProgress(0)
        cmd_cancel.Visible = False
        cmd_ident.Visible = False
    End Sub

#Region "Status Bar"
    Private Delegate Sub cbStatusBar_Index(ByVal ind As Integer)
    Private Delegate Sub cbStatusBar_Address(ByVal ind As Long)
    Private Delegate Sub cbStatusBar_String(ByVal value As String)
    Private Delegate Sub cbStatusBar_Percent(ByVal value As Integer)

    Private Sub StatusBar_Create()
        StatusLabels(0) = New ToolStripStatusLabel 'Img
        StatusLabels(1) = New ToolStripStatusLabel '"0x00000000"
        StatusLabels(2) = New ToolStripStatusLabel '"Erasing memory sector"
        StatusLabels(3) = New ToolStripStatusLabel '"400,000 bytes/s"
        StatusLabels(4) = New ToolStripStatusLabel '"100%"
        StatusLabels(0).Image = Nothing
        StatusLabels(0).Width = 20

        StatusLabels(1).BorderSides = ToolStripStatusLabelBorderSides.Left
        StatusLabels(1).BorderStyle = Border3DStyle.Etched
        StatusLabels(1).AutoSize = False
        StatusLabels(1).Text = ""
        StatusLabels(1).TextAlign = ContentAlignment.MiddleLeft
        StatusLabels(1).Width = 80
        StatusLabels(1).Font = New Font("Courier New", 9.0F, FontStyle.Bold)

        StatusLabels(2).BorderSides = ToolStripStatusLabelBorderSides.Left
        StatusLabels(2).BorderStyle = Border3DStyle.Etched
        StatusLabels(2).Spring = True
        StatusLabels(2).Text = ""
        StatusLabels(2).TextAlign = ContentAlignment.MiddleLeft
        StatusLabels(2).Width = 100
        'StatusLabels(2).Font = New Font("Courier New", 9.0F, FontStyle.Regular)

        StatusLabels(3).BorderSides = ToolStripStatusLabelBorderSides.Left
        StatusLabels(3).BorderStyle = Border3DStyle.Etched
        StatusLabels(3).AutoSize = False
        StatusLabels(3).Text = ""
        StatusLabels(3).TextAlign = ContentAlignment.MiddleLeft
        StatusLabels(3).Width = 80 '104
        'StatusLabels(3).Font = New Font("Courier New", 9.0F, FontStyle.Regular)

        StatusLabels(4).BorderSides = ToolStripStatusLabelBorderSides.Left
        StatusLabels(4).BorderStyle = Border3DStyle.Etched
        StatusLabels(4).Text = ""
        StatusLabels(4).TextAlign = ContentAlignment.MiddleLeft
        StatusLabels(4).Width = 60
        'StatusLabels(4).Font = New Font("Courier New", 9.0F, FontStyle.Regular)

    End Sub

    Public Sub StatusBar_ImgIndex(ByVal ind As Integer)
        If Me.InvokeRequired Then
            Dim d As New cbStatusBar_Index(AddressOf StatusBar_ImgIndex)
            Me.Invoke(d, {ind})
        Else
            Select Case ind
                Case 0 'None
                    StatusLabels(0).Image = Nothing
                Case 1 'Reading
                    StatusLabels(0).Image = My.Resources.BLOCK_GREEN
                Case 2 'Writing
                    StatusLabels(0).Image = My.Resources.BLOCK_RED
                Case 3 'Verify write
                    StatusLabels(0).Image = My.Resources.BLOCK_CHK
                Case 4 'Erasing
                    StatusLabels(0).Image = My.Resources.BLOCK_BLACK
                Case 5 'Error
                    StatusLabels(0).Image = My.Resources.BLOCK_ERROR
            End Select
            Application.DoEvents()
        End If
    End Sub

    Public Sub StatusBar_SetTextBaseAddress(mem_addr As Long)
        Try
            If Me.InvokeRequired Then
                Dim d As New cbStatusBar_Address(AddressOf StatusBar_SetTextBaseAddress)
                Me.Invoke(d, {mem_addr})
            Else
                StatusLabels(1).Text = "0x" & Hex(mem_addr.ToString).PadLeft(8, "0")
                Application.DoEvents()
            End If
        Catch ex As Exception
        End Try
    End Sub

    Public Sub StatusBar_SetTextTask(ByVal current_task As String)
        If Me.InvokeRequired Then
            Dim d As New cbStatusBar_String(AddressOf StatusBar_SetTextTask)
            Me.Invoke(d, {current_task})
        Else
            Static last_update As DateTime = DateTime.Now
            Static thead_count As Integer = 0
            Try
                thead_count += 1
                Do While DateTime.Now.Subtract(last_update).TotalMilliseconds < 250
                    If thead_count > 1 Then Exit Do
                Loop
                StatusLabels(2).Text = current_task
                Application.DoEvents() 'Forces the form to redraw this label
                last_update = DateTime.Now
            Catch ex As Exception
            Finally
                thead_count = -1
            End Try
        End If
    End Sub

    Public Sub StatusBar_SetTextSpeed(ByVal speed_str As String)
        Try
            If Me.InvokeRequired Then
                Dim d As New cbStatusBar_String(AddressOf StatusBar_SetTextSpeed)
                Me.Invoke(d, {speed_str})
            Else
                StatusLabels(3).Text = speed_str
                Application.DoEvents()
            End If
        Catch ex As Exception
        End Try
    End Sub

    Public Sub StatusBar_SetPercent(ByVal value As Integer)
        Try
            If Me.InvokeRequired Then
                Dim d As New cbStatusBar_Percent(AddressOf StatusBar_SetPercent)
                Me.Invoke(d, {value})
            Else
                StatusLabels(4).Text = value & "%"
                Application.DoEvents()
            End If
        Catch ex As Exception
        End Try
    End Sub

#End Region

    Public Class XFER_Operation
        Public FileName As IO.FileInfo  'Contains the shortname of the file being opened or written to
        Public DataStream As IO.Stream
        Public Offset As Long
        Public Size As Long
        Public FileTypeIndex As Integer 'Default is binary
    End Class

    'Call this to setup this control
    Public Sub InitMemoryDevice(usb_dev As USB.HostClient.FCUSB_DEVICE, Name As String, flash_size As Long, access As access_mode, Optional mem_base As UInt32 = 0)
        Me.FCUSB = usb_dev
        Me.FlashName = Name
        Me.FlashAvailable = flash_size 'Main area first
        Me.FlashBase = mem_base 'Only used for devices that share the same memory base (i.e JTAG)
        pb_ecc.Visible = False
        ExtendedAreaVisibility(False) 'Only enable this on device that have more than one internal area to view
        gb_flash.Text = Name
        SetProgress(0)
        HexEditor64.CreateHexViewer(Me.FlashBase, Me.FlashAvailable)
        txtAddress.Text = "0x0"
        If access = access_mode.Writable Then
            cmd_erase.Enabled = True
            cmd_write.Enabled = True
        ElseIf access = access_mode.WriteOnce Then
            cmd_erase.Enabled = False
            cmd_write.Enabled = True
        ElseIf access = access_mode.ReadOnly Then
            cmd_erase.Enabled = False
            cmd_write.Enabled = False
        End If
        Me.MY_ACCESS = access
        RefreshView()
    End Sub

    Public Enum access_mode
        [ReadOnly] 'We can read but can not write
        [Writable]
        [WriteOnce]
    End Enum

    Friend Class DynamicRangeBox
        Private BaseTxt As New TextBox
        Private LenTxt As New TextBox
        'Allows you to click and move the form around
        Private MouseDownOnForm As Boolean = False
        Private ClickPoint As Point
        Private CurrentBase As Long
        Private CurrentSize As Long
        Private CurrentMax As Long

        Sub New()

        End Sub

        Public Function ShowRangeBox(ByRef BaseAddress As Long, ByRef Size As Long, ByVal MaxData As Long) As Boolean
            If Size > MaxData Then Size = MaxData
            Dim InputSelectionForm As New Form With {.Width = 172, .Height = 80}
            InputSelectionForm.FormBorderStyle = FormBorderStyle.FixedToolWindow
            InputSelectionForm.ShowInTaskbar = False
            InputSelectionForm.ShowIcon = False
            InputSelectionForm.ControlBox = False
            Dim BtnOK As New Button With {.Text = RM.GetString("mc_button_ok"), .Width = 60, .Height = 20, .Left = 90, .Top = 50}
            Dim BtnCAN As New Button With {.Text = RM.GetString("mc_button_cancel"), .Width = 60, .Height = 20, .Left = 20, .Top = 50}
            Dim Lbl1 As New Label With {.Text = RM.GetString("mc_rngbox_base"), .Left = 10, .Top = 5}
            Dim Lbl2 As New Label With {.Text = RM.GetString("mc_rngbox_len"), .Left = 105, .Top = 5}
            BaseTxt = New TextBox With {.Text = "0x" & Hex(BaseAddress), .Width = 70, .Top = 20, .Left = 10}
            LenTxt = New TextBox With {.Text = Size.ToString, .Width = 70, .Top = 20, .Left = 90}
            InputSelectionForm.Controls.Add(BtnOK)
            InputSelectionForm.Controls.Add(BtnCAN)
            InputSelectionForm.Controls.Add(BaseTxt)
            InputSelectionForm.Controls.Add(LenTxt)
            InputSelectionForm.Controls.Add(Lbl2)
            InputSelectionForm.Controls.Add(Lbl1)
            AddHandler BtnCAN.Click, AddressOf Dyn_CancelClick
            AddHandler BtnOK.Click, AddressOf Dyn_OkClick
            AddHandler InputSelectionForm.MouseDown, AddressOf Dyn_MouseDown
            AddHandler InputSelectionForm.MouseUp, AddressOf Dyn_MouseUp
            AddHandler InputSelectionForm.MouseMove, AddressOf Dyn_MouseMove
            AddHandler Lbl2.MouseDown, AddressOf Dyn_MouseDown
            AddHandler Lbl2.MouseUp, AddressOf Dyn_MouseUp
            AddHandler Lbl2.MouseMove, AddressOf DynLabel_MouseMove
            AddHandler Lbl1.MouseDown, AddressOf Dyn_MouseDown
            AddHandler Lbl1.MouseUp, AddressOf Dyn_MouseUp
            AddHandler Lbl1.MouseMove, AddressOf DynLabel_MouseMove
            AddHandler InputSelectionForm.Load, AddressOf DynForm_Load
            AddHandler LenTxt.KeyDown, AddressOf DynForm_Keydown
            AddHandler LenTxt.LostFocus, AddressOf DynFormLength_LostFocus
            AddHandler BaseTxt.KeyDown, AddressOf DynForm_Keydown
            AddHandler BaseTxt.LostFocus, AddressOf DynFormBase_LostFocus
            BtnOK.Select()
            CurrentBase = BaseAddress
            CurrentSize = Size
            CurrentMax = MaxData
            If InputSelectionForm.ShowDialog() = DialogResult.OK Then
                BaseAddress = CurrentBase
                Size = CurrentSize
                Return True
            Else
                Return False
            End If
        End Function

        Private Sub DynFormLength_LostFocus(ByVal sender As System.Object, ByVal e As System.EventArgs)
            Dim t As TextBox = DirectCast(sender, TextBox)
            Try
                If IsNumeric(t.Text) Then
                    CurrentSize = CLng(t.Text)
                ElseIf Utilities.IsDataType.HexString(t.Text) AndAlso t.Text.Length < 9 Then
                    CurrentSize = Utilities.HexToLng(t.Text)
                End If
            Finally
                If CurrentSize > CurrentMax Then CurrentSize = CurrentMax
                If CurrentSize < 1 Then CurrentSize = 1
            End Try
            t.Text = CurrentSize
            If CurrentBase + CurrentSize > CurrentMax Then
                CurrentBase = CurrentMax - CurrentSize
                BaseTxt.Text = "0x" & Hex(CurrentBase)
            End If
        End Sub

        Private Sub DynFormBase_LostFocus(ByVal sender As System.Object, ByVal e As System.EventArgs)
            Dim t As TextBox = DirectCast(sender, TextBox)
            Try
                If IsNumeric(t.Text) AndAlso (CLng(t.Text) < (CurrentMax + 1)) Then
                    CurrentBase = CLng(t.Text)
                ElseIf Utilities.IsDataType.HexString(t.Text) Then
                    CurrentBase = Utilities.HexToLng(t.Text)
                End If
            Finally
                If CurrentBase > (CurrentMax + 1) Then CurrentBase = CurrentMax - 1
            End Try
            t.Text = "0x" & Hex(CurrentBase)
            If CurrentBase + CurrentSize > CurrentMax Then
                CurrentSize = CurrentMax - CurrentBase
                LenTxt.Text = CurrentSize.ToString
            End If
        End Sub

        Private Sub DynForm_Keydown(ByVal sender As System.Object, ByVal e As System.Windows.Forms.KeyEventArgs)
            If e.KeyCode = 13 Then 'Enter pressed
                Dim Btn As TextBox = CType(sender, TextBox)
                Dim SendFrm As Form = Btn.FindForm
                SendFrm.DialogResult = DialogResult.OK
            End If
        End Sub
        'Always centers the dynamic input form on top of the original form
        Private Sub DynForm_Load(ByVal sender As System.Object, ByVal e As System.EventArgs)
            Dim frm As Form = CType(sender, Form)
            frm.Top = CInt(GUI.Top + ((GUI.Height / 2) - (frm.Height / 2)))
            frm.Left = CInt(GUI.Left + ((GUI.Width / 2) - (frm.Width / 2)))
        End Sub
        'Handles the dynamic form for a click
        Private Sub Dyn_OkClick(ByVal sender As System.Object, ByVal e As System.EventArgs)
            Dim Btn As Button = CType(sender, Button)
            Dim SendFrm As Form = Btn.FindForm
            SendFrm.DialogResult = DialogResult.OK
        End Sub

        Private Sub Dyn_CancelClick(ByVal sender As System.Object, ByVal e As System.EventArgs)
            Dim Btn As Button = CType(sender, Button)
            Dim SendFrm As Form = Btn.FindForm
            SendFrm.DialogResult = DialogResult.Cancel
        End Sub

        Private Sub Dyn_MouseDown(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            MouseDownOnForm = True
            ClickPoint = New Point(Cursor.Position.X, Cursor.Position.Y)
        End Sub

        Private Sub Dyn_MouseUp(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            MouseDownOnForm = False
        End Sub

        Private Sub Dyn_MouseMove(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            If MouseDownOnForm Then
                Dim newPoint As New Point(Cursor.Position.X, Cursor.Position.Y)
                Dim ThisForm As Form = CType(sender, Form)
                ThisForm.Top = ThisForm.Top + (newPoint.Y - ClickPoint.Y)
                ThisForm.Left = ThisForm.Left + (newPoint.X - ClickPoint.X)
                ClickPoint = newPoint
            End If
        End Sub
        'Hanldes the move if a label is being dragged
        Private Sub DynLabel_MouseMove(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            If MouseDownOnForm Then
                Dim newPoint As New Point(Cursor.Position.X, Cursor.Position.Y)
                Dim Btn As Label = CType(sender, Label)
                Dim Form1 As Form = Btn.FindForm
                Form1.Top = Form1.Top + (newPoint.Y - ClickPoint.Y)
                Form1.Left = Form1.Left + (newPoint.X - ClickPoint.X)
                ClickPoint = newPoint
            End If
        End Sub

    End Class

    Friend Sub DisableWrite()
        If Me.InvokeRequired Then
            Dim d As New cbInvokeControl(AddressOf DisableWrite)
            d.Invoke()
        Else
            cmd_write.Enabled = False
        End If
    End Sub

    Friend Sub DisableErase()
        If Me.InvokeRequired Then
            Dim d As New cbInvokeControl(AddressOf DisableErase)
            d.Invoke()
        Else
            cmd_erase.Enabled = False
        End If
    End Sub

    Public Sub ShowIdentButton(ByVal display As Boolean)
        If Me.InvokeRequired Then
            Dim d As New cbDisableControls(AddressOf ShowIdentButton)
            d.Invoke(display)
        Else
            If display Then
                cmd_ident.Visible = True
                DisplayIdent = True
            Else
                cmd_ident.Visible = False
                DisplayIdent = False
            End If
        End If
    End Sub

#Region "Delegates"
    Private Delegate Sub cbSetProgress(ByVal value As Integer)
    Private Delegate Sub cbInvokeControl()
    Private Delegate Sub cbAddressUpdate(ByVal Address As Long)
    Private Delegate Sub cbControls()
    Private Delegate Sub cbDisableControls(ByVal show_cancel As Boolean)
#End Region

#Region "Status Bar Hooks"
    Private Delegate Sub cbStatus_UpdateOper(ByVal ind As Integer)
    Private Delegate Sub cbStatus_UpdateBase(ByVal addr As Long)
    Private Delegate Sub cbStatus_UpdateTask(ByVal value As String)
    Private Delegate Sub cbStatus_UpdateSpeed(ByVal speed_str As String)
    Private Delegate Sub cbStatus_UpdatePercent(ByVal percent As Integer)

    Private Sub Status_UpdateOper(ind As MEM_OPERATION)
        StatusBar_ImgIndex(ind)
    End Sub
    Private Sub Status_UpdateBase(addr As Long)
        StatusBar_SetTextBaseAddress(addr)
    End Sub

    Private Sub Status_UpdateTask(task As String)
        StatusBar_SetTextTask(task)
    End Sub

    Private Sub Status_UpdateSpeed(speed_str As String)
        StatusBar_SetTextSpeed(speed_str)
    End Sub

    Private Sub Status_UpdatePercent(percent As Integer)
        Me.SetProgress(percent)
        StatusBar_SetPercent(percent)
    End Sub

    Private Enum MEM_OPERATION As Integer
        NoOp = 0
        ReadData = 1
        WriteData = 2
        VerifyData = 3
        EraseSector = 4
        ErrOp = 5
    End Enum

#End Region

#Region "Page Layout - NAND devices"
    Private Delegate Sub cbExtendedAreaVisibility(ByVal show As Boolean)
    Private Delegate Sub cbAddExtendedArea(ByVal page_count As UInt32, ByVal page_size As UInt16, ByVal ext_size As UInt16, ByVal pages_per_block As UInt32)
    Private Delegate Sub cbSetSelectedArea(ByVal area As FlashMemory.FlashArea)

    Private Property EXTAREA_PAGECOUNT As UInt32 'Total number of pages
    Private Property EXTAREA_BLOCK_PAGES As UInt32 'Number of pages in a block/sector
    Private Property EXTAREA_MAIN_PAGE As UInt32 'Number of bytes per page in the main area
    Private Property EXTAREA_EXT_PAGE As UInt32 'Number of bytes per page in the oob area
    Private Property HAS_EXTAREA As Boolean = False 'Indicates we have split memory (main/spare)

    'This setups the editor to use a Flash with an extended area (such as spare data)
    Private Sub AddExtendedArea(ByVal page_count As UInt32, ByVal page_size As UInt16, ByVal ext_size As UInt16, ByVal pages_per_block As UInt32)
        If Me.InvokeRequired Then
            Dim d As New cbAddExtendedArea(AddressOf AddExtendedArea)
            Me.Invoke(d, {page_count, page_size, ext_size, pages_per_block})
        Else
            Me.EXTAREA_PAGECOUNT = page_count
            Me.EXTAREA_BLOCK_PAGES = pages_per_block
            Me.EXTAREA_MAIN_PAGE = page_size 'i.e. 2048
            Me.EXTAREA_EXT_PAGE = ext_size
            Me.HAS_EXTAREA = True
            ExtendedAreaVisibility(True)
            SetSelectedArea(FlashMemory.FlashArea.Main)
            Me.RefreshView()
        End If
    End Sub

    Private Sub SetSelectedArea(ByVal area As FlashMemory.FlashArea)
        If Me.InvokeRequired Then
            Dim d As New cbSetSelectedArea(AddressOf SetSelectedArea)
            Me.Invoke(d, area)
        Else
            AreaSelected = area
            pb_ecc.Visible = False
            If Me.HAS_EXTAREA Then
                Select Case area
                    Case FlashMemory.FlashArea.Main
                        cmd_area.Text = RM.GetString("mc_button_main")
                        Me.FlashAvailable = (CLng(Me.EXTAREA_PAGECOUNT) * CLng(Me.EXTAREA_MAIN_PAGE))
                        If MySettings.ECC_READ_ENABLED Then pb_ecc.Visible = True
                        UpdateEccResultImg()
                    Case FlashMemory.FlashArea.OOB
                        cmd_area.Text = RM.GetString("mc_button_spare")
                        Me.FlashAvailable = (CLng(Me.EXTAREA_PAGECOUNT) * CLng(Me.EXTAREA_EXT_PAGE))
                End Select
            End If
            HexEditor64.CreateHexViewer(Me.FlashBase, Me.FlashAvailable)
            txtAddress.Text = "0x0"
            RefreshView()
        End If
    End Sub

    Private Sub ExtendedAreaVisibility(ByVal show As Boolean)
        If Me.InvokeRequired Then
            Dim d As New cbExtendedAreaVisibility(AddressOf ExtendedAreaVisibility)
            Me.Invoke(d, {show})
        Else
            cmd_area.Visible = show
        End If
    End Sub

    'Sets up the memory control for devices with main/extended areas
    Public Sub SetupLayout()
        ExtendedAreaVisibility(False)
        Me.AreaSelected = FlashMemory.FlashArea.Main 'Lets always default to the main area
        If Me.ParentMemDevice.FlashType = FlashMemory.MemoryType.SERIAL_NAND Then
            Dim d As FlashMemory.SPI_NAND = DirectCast(FCUSB.SPI_NAND_IF.MyFlashDevice, FlashMemory.SPI_NAND)
            Dim pages_per_block As UInt32 = (d.BLOCK_SIZE / d.PAGE_SIZE)
            Dim available_pages As UInt32 = FCUSB.NAND_IF.MAPPED_PAGES
            Me.FlashAvailable = FCUSB.SPI_NAND_IF.DeviceSize
            Me.ParentMemDevice.Size = Me.FlashAvailable
            If MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Combined Then 'We need to show all data
                Me.HAS_EXTAREA = False
                SetSelectedArea(FlashMemory.FlashArea.All)
            Else
                Me.AddExtendedArea(available_pages, d.PAGE_SIZE, d.EXT_PAGE_SIZE, pages_per_block)
            End If
        ElseIf Me.ParentMemDevice.FlashType = FlashMemory.MemoryType.NAND Then
            Dim d As FlashMemory.P_NAND = DirectCast(FCUSB.EXT_IF.MyFlashDevice, FlashMemory.P_NAND)
            Dim pages_per_block As UInt32 = (d.BLOCK_SIZE / d.PAGE_SIZE)
            Dim available_pages As UInt32 = FCUSB.NAND_IF.MAPPED_PAGES
            Me.FlashAvailable = FCUSB.EXT_IF.DeviceSize
            Me.ParentMemDevice.Size = Me.FlashAvailable
            If MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Combined Then 'We need to show all data
                Me.HAS_EXTAREA = False
                SetSelectedArea(FlashMemory.FlashArea.All)
            Else
                Me.AddExtendedArea(available_pages, d.PAGE_SIZE, d.EXT_PAGE_SIZE, pages_per_block)
            End If
        End If
    End Sub

    Private Sub ReadingMem_Status_Start(ByRef Params As ReadParameters)
        Try
            If Params.Status.UpdateOperation IsNot Nothing Then Params.Status.UpdateOperation.DynamicInvoke(1) 'READ IMG
            If Params.Status.UpdateBase IsNot Nothing Then Params.Status.UpdateBase.DynamicInvoke(Params.Address)
            If Params.Status.UpdatePercent IsNot Nothing Then Params.Status.UpdatePercent.DynamicInvoke(CInt(0))
        Catch ex As Exception
        End Try
    End Sub

    Private Sub ReadingMem_Status_Update(ByVal total_count As UInt32, ByRef Params As ReadParameters)
        Try
            Dim Percent As Single = CSng(((total_count - Params.Count) / total_count) * 100) 'Calulate % done
            Dim current_str As String = Format((total_count - Params.Count), "#,###")
            Dim total_str As String = Format(total_count, "#,###")
            If Params.Status.UpdateTask IsNot Nothing Then Params.Status.UpdateTask.DynamicInvoke(String.Format(RM.GetString("mc_reading"), current_str, total_str))
            If Params.Status.UpdatePercent IsNot Nothing Then Params.Status.UpdatePercent.DynamicInvoke(CInt(Percent))
            Dim BytesTransfered As UInt32 = total_count - Params.Count
            If Params.Timer IsNot Nothing AndAlso Params.Status.UpdateSpeed IsNot Nothing Then
                Dim speed_str As String = Format(Math.Round(BytesTransfered / (Params.Timer.ElapsedMilliseconds / 1000)), "#,###") & " Bytes/s"
                Params.Status.UpdateSpeed.DynamicInvoke(speed_str)
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub cmd_area_Click(ByVal sender As Object, e As EventArgs) Handles cmd_area.Click
        Select Case AreaSelected
            Case FlashMemory.FlashArea.Main
                SetSelectedArea(FlashMemory.FlashArea.OOB)
            Case FlashMemory.FlashArea.OOB
                SetSelectedArea(FlashMemory.FlashArea.Main)
        End Select
    End Sub

    Private Sub UpdateEccResultImg()
        If MySettings.ECC_READ_ENABLED Then
            Dim result As ECC_DECODE_RESULT
            RaiseEvent GetEccLastResult(result)
            If result = ECC_DECODE_RESULT.NoErrors Then
                pb_ecc.Image = My.Resources.ecc_valid
            Else
                pb_ecc.Image = My.Resources.ecc_blue
            End If
        End If
    End Sub

#End Region

    Public Sub RefreshView()
        Try
            HexEditor64.Width = (Me.Width - 12)
            HexEditor64.Height = (Me.Height - (pbar.Bottom + 18))
            HexEditor64.UpdateScreen()
        Catch ex As Exception
        End Try
    End Sub

    Public Sub SetProgress(ByVal Percent As Integer)
        Try
            If Me.InvokeRequired Then
                Dim d As New cbSetProgress(AddressOf SetProgress)
                Me.Invoke(d, New Object() {Percent})
            Else
                If (Percent > 100) Then Percent = 100
                pbar.Value = Percent
                RaiseEvent SetExternalProgress(Percent)
            End If
        Catch ex As Exception
        End Try
    End Sub

    Public Property AllowFullErase As Boolean
        Get
            Return EnableChipErase
        End Get
        Set(value As Boolean)
            EnableChipErase = value
            SetChipEraseButton()
        End Set
    End Property

    Private Sub SetChipEraseButton()
        If Me.InvokeRequired Then
            Dim d As New cbControls(AddressOf SetChipEraseButton)
            Me.Invoke(d)
        Else
            If Me.EnableChipErase Then
                If Me.MY_ACCESS = access_mode.Writable Then cmd_erase.Enabled = True
            Else
                cmd_erase.Enabled = False
            End If
        End If
    End Sub

    Public Sub EnableControls()
        If Me.InvokeRequired Then
            Dim d As New cbControls(AddressOf EnableControls)
            Me.Invoke(d)
        Else
            cmd_read.Enabled = True
            If Me.MY_ACCESS = access_mode.Writable Then
                cmd_write.Enabled = True
            ElseIf Me.MY_ACCESS = access_mode.WriteOnce Then
                cmd_write.Enabled = True
            End If
            SetChipEraseButton()
            cmd_compare.Enabled = True
            cmd_edit.Enabled = True
            HexEditor64.EDIT_MODE = cmd_edit.Checked
            cmd_read.Visible = True
            cmd_write.Visible = True
            cmd_erase.Visible = True
            cmd_edit.Visible = True
            If DisplayIdent Then cmd_ident.Visible = True
            cmd_compare.Visible = True
            cmd_area.Enabled = True
            cmd_cancel.Visible = False
            Me.USER_HIT_CANCEL = False
        End If
    End Sub
    'We want to disable read/write/erase controls
    Public Sub DisableControls(ByVal show_cancel As Boolean)
        If Me.InvokeRequired Then
            Dim d As New cbDisableControls(AddressOf DisableControls)
            Me.Invoke(d, {show_cancel})
        Else
            HexEditor64.EDIT_MODE = False
            cmd_read.Enabled = False
            cmd_write.Enabled = False
            cmd_erase.Enabled = False
            cmd_compare.Enabled = False
            cmd_area.Enabled = False
            cmd_edit.Enabled = False
            If show_cancel Then
                cmd_cancel.Visible = True
                cmd_cancel.Enabled = True
                cmd_read.Visible = False
                cmd_write.Visible = False
                cmd_erase.Visible = False
                cmd_ident.Visible = False
                cmd_edit.Visible = False
                cmd_compare.Visible = False
            Else
                cmd_cancel.Visible = False
                cmd_cancel.Enabled = False
                cmd_read.Visible = True
                cmd_write.Visible = True
                cmd_erase.Visible = True
                cmd_edit.Visible = True
                If DisplayIdent Then cmd_ident.Visible = True
                cmd_compare.Visible = True
            End If
            HexEditor64.Focus()
        End If
    End Sub

    Public Sub GetFocus()
        If Me.InvokeRequired Then
            Dim d As New cbControls(AddressOf GetFocus)
            Me.Invoke(d)
        Else
            HexEditor64.Focus()
        End If
    End Sub

#Region "Address Box"

    Private Sub AddressUpdate(ByVal Address As Long) Handles HexEditor64.AddressUpdate
        If txtAddress.InvokeRequired Then
            Dim d As New cbAddressUpdate(AddressOf AddressUpdate)
            Me.Invoke(d, New Object() {Address})
        Else
            txtAddress.Text = "0x" & Hex(Address).ToUpper
            txtAddress.SelectionStart = txtAddress.Text.Length
        End If
    End Sub

    Private Sub txtAddress_KeyPress(ByVal sender As Object, ByVal e As KeyPressEventArgs) Handles txtAddress.KeyPress
        If Asc(e.KeyChar) = Keys.Enter Then
            HexEditor64.Focus() 'Makes this control loose focus and trigger the other event (lostfocus)
        ElseIf Asc(e.KeyChar) = 97 Then 'a
            e.KeyChar = "A"
        ElseIf Asc(e.KeyChar) = 98 Then 'b
            e.KeyChar = "B"
        ElseIf Asc(e.KeyChar) = 99 Then 'c
            e.KeyChar = "C"
        ElseIf Asc(e.KeyChar) = 100 Then 'd
            e.KeyChar = "D"
        ElseIf Asc(e.KeyChar) = 101 Then 'e
            e.KeyChar = "E"
        ElseIf Asc(e.KeyChar) = 102 Then 'f
            e.KeyChar = "F"
        End If
    End Sub

    Private Sub txtAddress_KeyDown(sender As Object, e As KeyEventArgs) Handles txtAddress.KeyDown

    End Sub

    Private Sub txtAddress_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtAddress.LostFocus
        Try
            Dim input As String = Trim(txtAddress.Text.Replace(" ", ""))
            If IsNumeric(input) Then
                HexEditor64.GotoAddress(CLng(input))
            ElseIf Utilities.IsDataType.HexString(txtAddress.Text) Then
                HexEditor64.GotoAddress(Utilities.HexToLng(input))
            Else
                txtAddress.Text = "0x" & Hex(HexEditor64.TopAddress).ToUpper
            End If
        Catch ex As Exception
            txtAddress.Text = "0x" & Hex(HexEditor64.TopAddress).ToUpper
        End Try
    End Sub

#End Region

    'Our hex viewer is asking for data to display
    Private Sub DataRequest(address As Long, ByRef data_buffer() As Byte) Handles HexEditor64.RequestData
        Static RequestedData As Boolean = False
        If RequestedData Then Exit Sub
        Try : RequestedData = True
            Dim editor_reader As New ReadParameters
            editor_reader.Address = address
            editor_reader.Count = data_buffer.Length
            editor_reader.Memory_Area = AreaSelected
            Using m As New IO.MemoryStream()
                Dim read_count As UInt32 = data_buffer.Count
                RaiseEvent ReadStream(m, editor_reader)
                data_buffer = m.GetBuffer
                ReDim Preserve data_buffer(read_count - 1)
            End Using
            UpdateEccResultImg()
        Finally
            RequestedData = False
        End Try
    End Sub

    Private Function CreateFileForRead(ByVal DefaultName As String, ByRef file As IO.FileInfo, ByRef file_type As FileFilterIndex) As Boolean
        Try
            Dim SaveMe As New SaveFileDialog
            SaveMe.AddExtension = True
            SaveMe.InitialDirectory = Application.StartupPath
            SaveMe.Title = RM.GetString("mc_io_save_type")
            SaveMe.CheckPathExists = True
            SaveMe.FileName = DefaultName.Replace("/", "-")
            Dim BinFile As String = "Binary Files (*.bin)|*.bin"
            Dim IntelHexFormat As String = "Intel Hex Format (*.hex)|*.hex"
            Dim SrecFormat As String = "S-REC Format (*.srec)|*.srec"
            Dim AllFiles As String = "All files (*.*)|*.*"
            SaveMe.Filter = BinFile & "|" & IntelHexFormat & "|" & SrecFormat & "|" & AllFiles
            If SaveMe.ShowDialog = DialogResult.OK Then
                Dim n As New IO.FileInfo(SaveMe.FileName)
                If n.Exists Then n.Delete()
                file = n
                Select Case SaveMe.FilterIndex
                    Case 1
                        file_type = FileFilterIndex.Binary
                    Case 2
                        file_type = FileFilterIndex.IntelHex
                    Case 3
                        file_type = FileFilterIndex.SRecord
                    Case 4
                        file_type = FileFilterIndex.AllFiles
                End Select
                Return True
            End If
        Catch ex As Exception
        End Try
        Return False
    End Function

    Private Function OpenFileForWrite(ByRef file As IO.FileInfo, ByRef file_type As FileFilterIndex) As Boolean
        Dim BinFile As String = "Binary Files (*.bin)|*.bin"
        Dim IntelHexFormat As String = "Intel Hex Format (*.hex)|*.hex"
        Dim SrecFormat As String = "S-REC Format (*.srec)|*.srec"
        Dim AllFiles As String = "All files (*.*)|*.*"
        Dim OpenMe As New OpenFileDialog
        OpenMe.AddExtension = True
        OpenMe.InitialDirectory = Application.StartupPath
        OpenMe.Title = String.Format(RM.GetString("mc_io_file_choose"), FlashName)
        OpenMe.CheckPathExists = True
        OpenMe.Filter = BinFile & "|" & IntelHexFormat & "|" & SrecFormat & "|" & AllFiles 'Bin Files, Hex Files, SREC, All Files
        If OpenMe.ShowDialog = DialogResult.OK Then
            file = New IO.FileInfo(OpenMe.FileName)
            Select Case OpenMe.FilterIndex
                Case 1
                    file_type = FileFilterIndex.Binary
                Case 2
                    file_type = FileFilterIndex.IntelHex
                Case 3
                    file_type = FileFilterIndex.SRecord
                Case 4
                    file_type = FileFilterIndex.AllFiles
            End Select
            RaiseEvent SetStatus(String.Format(RM.GetString("mc_io_file_writing"), FlashName))
            Return True
        Else
            RaiseEvent SetStatus(String.Format(RM.GetString("mc_io_file_cancel_to"), FlashName))
            Return False
        End If
    End Function

    Private Function OpenFileForCompare(ByRef file As IO.FileInfo) As Boolean
        Dim BinFile As String = "Binary Files (*.bin)|*.bin"
        Dim IntelHexFormat As String = "Intel Hex Format (*.hex)|*.hex"
        Dim SrecFormat As String = "S-REC Format (*.srec)|*.srec"
        Dim AllFiles As String = "All files (*.*)|*.*"
        Dim OpenMe As New OpenFileDialog
        OpenMe.AddExtension = True
        OpenMe.InitialDirectory = Application.StartupPath
        OpenMe.Title = String.Format(RM.GetString("mc_compare_selected"), FlashName) '"File selected, verifying {0}"
        OpenMe.CheckPathExists = True
        OpenMe.Filter = BinFile & "|" & IntelHexFormat & "|" & SrecFormat & "|" & AllFiles
        If OpenMe.ShowDialog = DialogResult.OK Then
            file = New IO.FileInfo(OpenMe.FileName)
            RaiseEvent SetStatus(String.Format(RM.GetString("mc_compare_selected"), FlashName)) ' "File selected, verifying {0}"
            Return True
        Else
            RaiseEvent SetStatus(String.Format(RM.GetString("mc_compare_canceled"), FlashName)) '"User canceled compare"
            Return False
        End If
    End Function

    Public Sub ReadMemoryThread(read_params As XFER_Operation)
        Try
            GUI.OperationStarted(Me) 'This adds the status bar at the bottom
            Me.IN_OPERATION = True
            FCUSB.USB_LEDBlink()
            SetProgress(0)
            DisableControls(True)
            Try
                Try
                    Dim n As New IO.FileInfo(read_params.FileName.FullName)
                    If n.Exists Then n.Delete()
                Catch ex As Exception
                End Try
                RaiseEvent WriteConsole(String.Format(RM.GetString("mc_mem_read_begin"), FlashName))
                RaiseEvent WriteConsole(String.Format(RM.GetString("mc_mem_start_addr"), read_params.Offset, "0x" & Utilities.Pad(Hex((read_params.Offset))), Format(read_params.Size, "#,###")))
                ReadingParams = New ReadParameters
                ReadingParams.Address = read_params.Offset
                ReadingParams.Count = read_params.Size
                ReadingParams.Timer = New Stopwatch
                ReadingParams.Memory_Area = Me.AreaSelected
                ReadingParams.Status.UpdateOperation = New cbStatus_UpdateOper(AddressOf Status_UpdateOper)
                ReadingParams.Status.UpdateBase = New cbStatus_UpdateBase(AddressOf Status_UpdateBase)
                ReadingParams.Status.UpdateTask = New cbStatus_UpdateTask(AddressOf Status_UpdateTask)
                ReadingParams.Status.UpdateSpeed = New cbStatus_UpdateSpeed(AddressOf Status_UpdateSpeed)
                ReadingParams.Status.UpdatePercent = New cbStatus_UpdatePercent(AddressOf Status_UpdatePercent)
                ReadingParams.Timer.Start()
                Using data_stream As IO.Stream = read_params.FileName.OpenWrite
                    RaiseEvent ReadStream(data_stream, ReadingParams)
                End Using
                If ReadingParams.AbortOperation Then
                    RaiseEvent SetStatus(RM.GetString("mc_mem_user_cancel"))
                    Try
                        Dim n2 As New IO.FileInfo(read_params.FileName.FullName)
                        If n2.Exists Then n2.Delete()
                    Catch ex As Exception
                    End Try
                Else
                    Dim StatusSpeed As String = Format(Math.Round(read_params.Size / (ReadingParams.Timer.ElapsedMilliseconds / 1000)), "#,###") & " Bytes/s"
                    RaiseEvent WriteConsole(RM.GetString("mc_mem_read_done"))
                    RaiseEvent WriteConsole(String.Format(RM.GetString("mc_mem_read_result"), Format(read_params.Size, "#,###"), (ReadingParams.Timer.ElapsedMilliseconds / 1000), StatusSpeed))
                    If DirectCast(read_params.FileTypeIndex, FileFilterIndex) = FileFilterIndex.IntelHex Then
                        Try
                            RaiseEvent WriteConsole(String.Format(RM.GetString("mc_mem_converting_format"), "Intel HEX"))
                            Dim data() As Byte = Utilities.FileIO.ReadBytes(read_params.FileName.FullName)
                            If data IsNot Nothing AndAlso data.Length > 0 Then
                                data = Utilities.BinToIntelHex(data)
                                Utilities.FileIO.WriteBytes(data, read_params.FileName.FullName)
                            End If
                        Catch ex As Exception
                        End Try
                    ElseIf DirectCast(read_params.FileTypeIndex, FileFilterIndex) = FileFilterIndex.SRecord Then 'Convert and save to SREC
                        RaiseEvent WriteConsole(String.Format(RM.GetString("mc_mem_converting_format"), "S-REC"))
                        Dim data() As Byte = Utilities.FileIO.ReadBytes(read_params.FileName.FullName)
                        If data IsNot Nothing AndAlso data.Length > 0 Then
                            Dim data_size As Integer = 8
                            If MySettings.SREC_BITMODE = 0 Then
                                data_size = 8
                            ElseIf MySettings.SREC_BITMODE = 1 Then
                                data_size = 16
                            End If
                            data = Utilities.SREC_FromBin(data, read_params.FileName.Name, 0, data_size)
                            Utilities.FileIO.WriteBytes(data, read_params.FileName.FullName)
                        End If
                    End If
                    RaiseEvent SetStatus(String.Format(RM.GetString("mc_mem_write_success"), read_params.FileName.Name))
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        Finally
            If read_params.DataStream IsNot Nothing Then read_params.DataStream.Dispose()
            FCUSB.USB_LEDOn()
            EnableControls()
            SetProgress(0)
            GetFocus()
            Me.IN_OPERATION = False
            GUI.OperationStopped(Me)
            ReadingParams = Nothing
        End Try
    End Sub

    Friend Sub WriteMemoryThread(ByVal file_out As XFER_Operation)
        Try
            GUI.OperationStarted(Me)
            Me.IN_OPERATION = True
            FCUSB.USB_LEDBlink()
            SetProgress(0)
            DisableControls(True)
            Try
                Dim write_success As Boolean = False
                WritingParams = New WriteParameters
                WritingParams.Address = file_out.Offset
                WritingParams.BytesLeft = file_out.Size
                WritingParams.BytesTotal = file_out.Size
                WritingParams.Verify = MySettings.VERIFY_WRITE
                WritingParams.EraseSector = True
                WritingParams.Memory_Area = Me.AreaSelected
                WritingParams.Status.UpdateOperation = New cbStatus_UpdateOper(AddressOf Status_UpdateOper)
                WritingParams.Status.UpdateBase = New cbStatus_UpdateBase(AddressOf Status_UpdateBase)
                WritingParams.Status.UpdateTask = New cbStatus_UpdateTask(AddressOf Status_UpdateTask)
                WritingParams.Status.UpdateSpeed = New cbStatus_UpdateSpeed(AddressOf Status_UpdateSpeed)
                WritingParams.Status.UpdatePercent = New cbStatus_UpdatePercent(AddressOf Status_UpdatePercent)
                'Reset current labels
                Status_UpdateOper(MEM_OPERATION.NoOp)
                Status_UpdateBase(file_out.Offset)
                Status_UpdateTask("")
                Status_UpdateSpeed("")
                Status_UpdatePercent(0)
                RaiseEvent WriteStream(file_out.DataStream, WritingParams, write_success)
                file_out.DataStream.Dispose()
                file_out.DataStream = Nothing
                If WritingParams.AbortOperation Then
                    LAST_WRITE_OPERATION = Nothing
                    RaiseEvent SetStatus(RM.GetString("mc_wr_user_canceled"))
                ElseIf (Not write_success) Then
                    LAST_WRITE_OPERATION = Nothing
                    RaiseEvent SetStatus(RM.GetString("mc_wr_oper_failed"))
                Else
                    Dim Speed As String = CStr(Format(Math.Round(file_out.Size / (WritingParams.Timer.ElapsedMilliseconds / 1000)), "#,###"))
                    RaiseEvent SetStatus(String.Format(RM.GetString("mc_wr_oper_complete"), Format(file_out.Size, "#,###")))
                    RaiseEvent WriteConsole(String.Format(RM.GetString("mc_wr_oper_complete"), Format(file_out.Size, "#,###")))
                    RaiseEvent WriteConsole(String.Format(RM.GetString("mc_wr_oper_result"), Format(file_out.Size, "#,###"), (WritingParams.Timer.ElapsedMilliseconds / 1000), Speed))
                    RaiseEvent SuccessfulWrite(Me.FCUSB, LAST_WRITE_OPERATION)
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        Finally
            SetProgress(0)
            HexEditor64.UpdateScreen()
            FCUSB.USB_LEDOn()
            EnableControls()
            Me.IN_OPERATION = False
            GUI.OperationStopped(Me)
            WritingParams = Nothing
        End Try
    End Sub

    Private Sub cmd_cancel_Click(sender As Object, e As EventArgs) Handles cmd_cancel.Click
        Try
            Me.cmd_cancel.Enabled = False
            AbortAnyOperation()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub cmd_read_Click(sender As Object, e As EventArgs) Handles cmd_read.Click
        Try
            Dim BaseOffset As Long = 0 'The starting address to read the from data
            Dim UserCount As Long = FlashAvailable 'The total number of bytes to read
            RaiseEvent SetStatus(String.Format(RM.GetString("mc_mem_read_from"), FlashName))
            Dim dbox As New DynamicRangeBox
            If Not dbox.ShowRangeBox(BaseOffset, UserCount, FlashAvailable) Then
                RaiseEvent SetStatus(RM.GetString("mc_mem_read_canceled"))
                Exit Sub
            End If
            RaiseEvent SetStatus(RM.GetString("mc_mem_read_start"))
            If UserCount = 0 Then Exit Sub
            Dim DefaultName As String = FlashName.Replace(" ", "_") & "_" & Utilities.Pad(Hex((BaseOffset))) & "-" & Utilities.Pad(Hex((BaseOffset + UserCount - 1)))
            Dim TargetIO As IO.FileInfo = Nothing
            Dim create_file_type As FileFilterIndex
            If CreateFileForRead(DefaultName, TargetIO, create_file_type) Then
                Dim read_params As New XFER_Operation 'We want to remember the last operation
                read_params.FileTypeIndex = create_file_type
                read_params.FileName = TargetIO
                read_params.Offset = BaseOffset
                read_params.Size = UserCount
                Dim t As New Threading.Thread(AddressOf ReadMemoryThread)
                t.Start(read_params)
                HexEditor64.Focus()
            Else
                RaiseEvent SetStatus(RM.GetString("mc_io_save_canceled"))
                Exit Sub
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub cmd_compare_Click(sender As Object, e As EventArgs) Handles cmd_compare.Click
        Dim v_parm As New CompareParams
        If OpenFileForCompare(v_parm.Local_File) Then
            v_parm.Count = v_parm.Local_File.Length
            Dim dbox As New DynamicRangeBox
            If (v_parm.Local_File.Extension.ToUpper.Equals(".HEX")) AndAlso v_parm.Local_File.Length < (1048576 * 16) Then
                Dim entire_file() As Byte = Utilities.FileIO.ReadBytes(v_parm.Local_File.FullName)
                If Utilities.IsIntelHex(entire_file) Then
                    v_parm.converted_file = Utilities.IntelHexToBin(entire_file)
                    v_parm.Count = v_parm.converted_file.Length
                End If
            ElseIf (v_parm.Local_File.Extension.ToUpper.Equals(".SREC")) AndAlso v_parm.Local_File.Length < (1048576 * 16) Then
                Dim entire_file() As Byte = Utilities.FileIO.ReadBytes(v_parm.Local_File.FullName)
                If Utilities.SREC_IsValid(entire_file) Then
                    Dim data_size As Integer = 8
                    If MySettings.SREC_BITMODE = 1 Then data_size = 16
                    v_parm.converted_file = Utilities.SREC_ToBin(entire_file, "", 0, data_size)
                    v_parm.Count = v_parm.converted_file.Length
                End If
            End If
            If Not dbox.ShowRangeBox(v_parm.BaseOffset, v_parm.Count, FlashAvailable) Then
                RaiseEvent SetStatus(RM.GetString("mc_io_compare_canceled"))
                Exit Sub
            End If
            HexEditor64.Focus()
            Dim td As New Threading.Thread(AddressOf CompareFlashTd)
            td.Start(v_parm)
        End If
    End Sub

    Private Sub CompareFlashTd(param As CompareParams)
        Dim TotalMismatches As UInt32 = 0
        Dim CompareCount As UInt32 = param.Count 'org size
        Dim StartingAddress As UInt32 = param.BaseOffset
        Dim ErrorList As New List(Of CompareDifference)
        Dim local_stream As IO.BinaryReader = Nothing
        If param.converted_file IsNot Nothing Then
            local_stream = New IO.BinaryReader(New IO.MemoryStream(param.converted_file))
        Else
            local_stream = New IO.BinaryReader(param.Local_File.OpenRead)
        End If
        Try
            GUI.OperationStarted(Me) 'This adds the status bar at the bottom
            Me.IN_OPERATION = True
            FCUSB.USB_LEDBlink()
            SetProgress(0)
            DisableControls(True)
            Status_UpdateOper(MEM_OPERATION.VerifyData)
            RaiseEvent WriteConsole(RM.GetString("mc_compare_start"))
            RaiseEvent WriteConsole(RM.GetString("mc_compare_filename") & ": " & param.Local_File.Name)
            RaiseEvent WriteConsole(String.Format(RM.GetString("mc_compare_info"), Hex(param.BaseOffset).PadLeft(8, "0"), Format(param.Count, "#,###")))
            Dim BytesTransfered As UInt32 = 0
            Dim ReadTimer As New Stopwatch
            Dim sector_count As UInt32 = 0
            Dim sector_ind As UInt32
            RaiseEvent GetSectorCount(sector_count)
            Do While (param.Count > 0)
                Dim packet_size As UInt32
                If sector_count = 1 Then 'single sector
                    packet_size = 65536 '64KB section
                Else
                    RaiseEvent GetSectorSize(sector_ind, Me.AreaSelected, packet_size)
                End If
                packet_size = Math.Min(packet_size, param.Count)
                Dim file_data() As Byte = local_stream.ReadBytes(packet_size)
                ReadTimer.Start()
                Dim flash_addr As UInt32 = param.BaseOffset
                Dim buffer() As Byte = CompareFlash_Read(flash_addr, packet_size)
                ReadTimer.Stop()
                If buffer Is Nothing Then Exit Do 'Abort occured
                param.Count -= buffer.Length
                param.BaseOffset += buffer.Length
                BytesTransfered += buffer.Length
                Dim speed_str As String = UpdateSpeed_GetText(Math.Round(BytesTransfered / (ReadTimer.ElapsedMilliseconds / 1000)))
                Status_UpdateSpeed(speed_str)
                Dim percent_done As Single = CSng((BytesTransfered / CompareCount) * 100)
                Status_UpdatePercent(CInt(percent_done))
                Dim vee As New CompareDifference
                For x = 0 To buffer.Length - 1
                    If (Not buffer(x) = file_data(x)) Then
                        vee.MISMATCH += 1
                        TotalMismatches += 1
                        If vee.MISMATCH = 1 Then
                            vee.BASR_ADR = flash_addr
                            vee.FIRST_OFFSET = x
                            vee.BYTE_FILE = file_data(x)
                            vee.BYTE_FLASH = buffer(x)
                        End If
                    End If
                Next
                If (vee.MISMATCH > 0) Then
                    Dim verify_str As String = RM.GetString("mem_verify_mismatches") '"Address {0}: file {1} and memory {2} ({3} mismatches)"
                    RaiseEvent WriteConsole(String.Format(verify_str, "0x" & Hex(vee.BASR_ADR + vee.FIRST_OFFSET), "0x" & Hex(vee.BYTE_FILE), "0x" & Hex(vee.BYTE_FLASH), vee.MISMATCH))
                    ErrorList.Add(vee)
                End If
                sector_ind += 1
            Loop
        Catch ex As Exception
        Finally
            local_stream.Close()
            local_stream.Dispose()
            FCUSB.USB_LEDOn()
            EnableControls()
            SetProgress(0)
            GetFocus()
            Me.IN_OPERATION = False
            GUI.OperationStopped(Me)
            Status_UpdateOper(MEM_OPERATION.NoOp)
        End Try
        Try
            If (param.Count = 0) Then 'We compared all data, lets show the user!
                Dim percent_success As Single = ((CSng(CompareCount - TotalMismatches) / CSng(CompareCount)) * 100)
                Dim percent_formatted As String = percent_success.ToString
                If (Not percent_formatted = "100") AndAlso percent_formatted.IndexOf(".") > 0 Then
                    percent_formatted = percent_formatted.Substring(0, percent_formatted.IndexOf(".") + 2)
                End If
                RaiseEvent WriteConsole(String.Format(RM.GetString("mc_compare_complete_tot"), TotalMismatches, percent_formatted))
                Dim filename As String = param.Local_File.Name
                Dim string_size As Size = TextRenderer.MeasureText(RM.GetString("mc_compare_filename") & filename, (New Label).Font)
                Dim CompareResultForm As New Form
                CompareResultForm.Text = RM.GetString("mc_compare_results") '"Memory Compare Results"
                CompareResultForm.Width = 280
                If (string_size.Width + 60) > 280 Then CompareResultForm.Width = string_size.Width + 60
                CompareResultForm.Height = 170
                CompareResultForm.FormBorderStyle = FormBorderStyle.FixedSingle
                CompareResultForm.ShowIcon = False
                CompareResultForm.ShowInTaskbar = False
                CompareResultForm.MinimizeBox = False
                CompareResultForm.MaximizeBox = False
                Dim fn_lbl As New Label With {.Width = CompareResultForm.Width + 20, .Height = 18, .Text = RM.GetString("mc_compare_filename") & ": " & filename, .Location = New Point(10, 4)}
                CompareResultForm.Controls.Add(fn_lbl)
                Dim s1 As String = RM.GetString("mc_compare_flash_addr")
                Dim s2 As String = RM.GetString("mc_compare_total_processed")
                Dim s3 As String = RM.GetString("mc_compare_mismatch") 's3 = "Mismatch count: {0} bytes ({1}% match)" "s3 = "Adresse flash: 0x{0}, taille: {1} octets""
                Dim match_str As String = String.Format(s3, TotalMismatches, percent_formatted)
                CompareResultForm.Controls.Add(New Label With {.Width = CompareResultForm.Width + 20, .Height = 18, .Text = s1 & ": 0x" & Hex(StartingAddress).PadLeft(8, "0") & " - 0x" & Hex(StartingAddress + CompareCount - 1).PadLeft(8, "0"), .Location = New Point(10, 24)})
                CompareResultForm.Controls.Add(New Label With {.Width = CompareResultForm.Width + 20, .Height = 18, .Text = s2 & ": " & Format(CompareCount, "#,###"), .Location = New Point(10, 44)})
                CompareResultForm.Controls.Add(New Label With {.Width = CompareResultForm.Width + 20, .Height = 18, .Text = match_str, .Location = New Point(10, 64)})
                Dim cmbClose As New Button With {.Text = RM.GetString("mc_button_close"), .Width = 80, .Location = New Point(CompareResultForm.Width / 2 - 50, 92)}
                AddHandler cmbClose.Click, Sub()
                                               CompareResultForm.DialogResult = DialogResult.OK
                                               CompareResultForm.Close()
                                           End Sub
                CompareResultForm.Controls.Add(cmbClose)
                AddHandler CompareResultForm.Load, Sub() 'This makes the form load on top of our current form
                                                       CompareResultForm.Top = CInt(GUI.Top + ((GUI.Height / 2) - (CompareResultForm.Height / 2)))
                                                       CompareResultForm.Left = CInt(GUI.Left + ((GUI.Width / 2) - (CompareResultForm.Width / 2)))
                                                   End Sub

                RaiseEvent SetStatus(RM.GetString("mc_compare_results") & ": " & percent_formatted & "% match")

                CompareResultForm.ShowDialog()
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Class CompareParams
        Public BaseOffset As UInt32 = 0
        Public Count As UInt32 = 0
        Public Local_File As IO.FileInfo
        Public converted_file() As Byte = Nothing
    End Class

    Private Function CompareFlash_Read(ByVal base As UInt32, ByVal count As UInt32) As Byte()
        Try
            ReadingParams = New ReadParameters
            ReadingParams.Address = base
            ReadingParams.Count = count
            ReadingParams.Timer = New Stopwatch
            ReadingParams.Memory_Area = AreaSelected
            ReadingParams.Status.UpdateBase = New cbStatus_UpdateBase(AddressOf Status_UpdateBase)
            ReadingParams.Status.UpdateTask = New cbStatus_UpdateTask(AddressOf Status_UpdateTask)
            Using data_stream As New IO.MemoryStream
                RaiseEvent ReadStream(data_stream, ReadingParams)
                If ReadingParams.AbortOperation Then Return Nothing
                data_stream.Position = 0
                Dim data_out(count - 1) As Byte
                data_stream.Read(data_out, 0, data_out.Length)
                Return data_out
            End Using
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Private Shared Function UpdateSpeed_GetText(ByVal bytes_per_second As Integer) As String
        Dim Mb008 As UInt32 = 1048576
        Dim speed_str As String
        If (bytes_per_second > (Mb008 - 1)) Then '1MB or higher
            speed_str = Format(CSng(bytes_per_second / Mb008), "#,###.000") & " MB/s"
        ElseIf (bytes_per_second > 8191) Then
            speed_str = Format(CSng(bytes_per_second / 1024), "#,###.00") & " KB/s"
        Else
            speed_str = Format(bytes_per_second, "#,###") & " B/s"
        End If
        Return speed_str
    End Function

    Private Class CompareDifference
        Public SECTOR As UInt32 'Index of the sector
        Public BASR_ADR As UInt32 = 0 'Start of the sector
        Public MISMATCH As UInt32 = 0 'Number of bytes that don't match in this sector
        Public FIRST_OFFSET As UInt32 ' The first offset where the data does not match
        Public BYTE_FILE As Byte 'This is the byte in the file
        Public BYTE_FLASH As Byte 'This is the byte in the flash
    End Class

    Private Enum FileFilterIndex As Integer
        AllFiles = -1 'Uses Binary mode
        Binary = 0
        IntelHex = 1
        SRecord = 2
    End Enum

    Private Sub cmd_write_Click(sender As Object, e As EventArgs) Handles cmd_write.Click
        Try
            Dim BaseOffset As UInt32 = 0 'The starting address to write data to
            Dim fn As IO.FileInfo = Nothing
            Dim open_file_type As FileFilterIndex
            If Not OpenFileForWrite(fn, open_file_type) Then Exit Sub
            If (Not fn.Exists) OrElse (fn.Length = 0) Then
                RaiseEvent SetStatus(RM.GetString("mc_wr_oper_file_err")) : Exit Sub
            End If
            RaiseEvent SetStatus(RM.GetString("mc_wr_oper_start"))
            LAST_WRITE_OPERATION = New XFER_Operation
            LAST_WRITE_OPERATION.FileTypeIndex = open_file_type
            LAST_WRITE_OPERATION.FileName = fn
            LAST_WRITE_OPERATION.Offset = BaseOffset
            LAST_WRITE_OPERATION.Size = 0
            PerformWriteOperation(LAST_WRITE_OPERATION)
            HexEditor64.Focus()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub cmd_erase_Click(sender As Object, e As EventArgs) Handles cmd_erase.Click
        If MsgBox(RM.GetString("mc_erase_warning"), MsgBoxStyle.YesNo, String.Format(RM.GetString("mc_erase_confirm"), FlashName)) = MsgBoxResult.Yes Then
            RaiseEvent WriteConsole(String.Format(RM.GetString("mc_erase_command_sent"), FlashName))
            RaiseEvent SetStatus(RM.GetString("mem_erasing_device"))
            Dim t As New Threading.Thread(AddressOf EraseFlashTd)
            t.Name = "mem.eraseFlash"
            t.Start()
            Application.DoEvents()
            HexEditor64.Focus()
        End If
    End Sub

    Private Sub EraseFlashTd()
        Try
            DisableControls(False) 'You can not cancel this
            GUI.OperationStarted(Me)
            Me.IN_OPERATION = True
            FCUSB.USB_LEDBlink()
            Status_UpdateOper(MEM_OPERATION.EraseSector)
            Status_UpdateBase(0)
            Status_UpdatePercent(0)
            Status_UpdateSpeed("")
            Status_UpdateTask(RM.GetString("mem_erase_device"))
            RaiseEvent EraseMemory()
            RaiseEvent SetStatus(RM.GetString("mem_erase_device_success"))
        Catch ex As Exception
        Finally
            Me.IN_OPERATION = False
            GUI.OperationStopped(Me)
            HexEditor64.UpdateScreen()
            FCUSB.USB_LEDOn()
            EnableControls()
        End Try
    End Sub

    Public Sub PerformWriteOperation(ByRef x As XFER_Operation)
        If DirectCast(x.FileTypeIndex, FileFilterIndex) = FileFilterIndex.IntelHex Then
            Dim hex_data() As Byte = Utilities.FileIO.ReadBytes(x.FileName.FullName)
            If Not Utilities.IsIntelHex(hex_data) Then
                RaiseEvent SetStatus(String.Format(RM.GetString("mc_mem_incorrect_format"), "Intel HEX"))
                Exit Sub
            End If
            Dim b() As Byte = Utilities.IntelHexToBin(hex_data)
            x.DataStream = New IO.MemoryStream
            x.DataStream.Write(b, 0, b.Length)
            RaiseEvent WriteConsole(String.Format(RM.GetString("mc_mem_open_file_write"), x.FileName.Name, "Intel HEX", Format(hex_data.Length, "#,###")))
        ElseIf DirectCast(x.FileTypeIndex, FileFilterIndex) = FileFilterIndex.SRecord Then 'Convert and save to SREC
            Dim hex_data() As Byte = Utilities.FileIO.ReadBytes(x.FileName.FullName)
            If Not Utilities.SREC_IsValid(hex_data) Then
                RaiseEvent SetStatus(String.Format(RM.GetString("mc_mem_incorrect_format"), "S-REC"))
                Exit Sub
            End If
            Dim data_size As Integer = 8
            If MySettings.SREC_BITMODE = 0 Then
                data_size = 8
            ElseIf MySettings.SREC_BITMODE = 1 Then
                data_size = 16
            End If
            Dim b() As Byte = Utilities.SREC_ToBin(hex_data, "", 0, data_size)
            x.DataStream = New IO.MemoryStream
            x.DataStream.Write(b, 0, b.Length)
            RaiseEvent WriteConsole(String.Format(RM.GetString("mc_mem_open_file_write"), x.FileName.Name, "S-REC", Format(hex_data.Length, "#,###")))
        Else
            x.DataStream = x.FileName.OpenRead
        End If
        Dim BaseAddress As UInt32 = 0 'The starting address to write the data
        If (x.Size = 0) Then
            RaiseEvent SetStatus(String.Format(RM.GetString("mc_select_range"), FlashName))
            Dim dbox As New DynamicRangeBox
            Dim NumberToWrite As UInt32 = x.DataStream.Length 'The total number of bytes to write
            If Not dbox.ShowRangeBox(BaseAddress, NumberToWrite, FlashAvailable) Then
                RaiseEvent SetStatus(RM.GetString("mc_wr_user_canceled"))
                Exit Sub
            End If
            If NumberToWrite = 0 Then Exit Sub
            x.Offset = BaseAddress
            x.Size = NumberToWrite
        End If
        RaiseEvent SetStatus(String.Format(RM.GetString("mc_wr_oper_status"), x.FileName.Name, FlashName, Format(x.Size, "#,###")))
        x.DataStream.Position = 0
        Dim t As New Threading.Thread(AddressOf WriteMemoryThread)
        t.Name = "memWriteTd"
        t.Start(x)
        RaiseEvent WriteConsole(String.Format(RM.GetString("mc_io_open_file"), x.FileName.Name, Format(x.DataStream.Length, "#,###")))
        RaiseEvent WriteConsole(String.Format(RM.GetString("mc_io_destination"), Hex(x.Offset).PadLeft(8, "0"), Format(x.Size, "#,###")))
    End Sub

    Public Sub AbortAnyOperation()
        Try
            Me.USER_HIT_CANCEL = True
            If WritingParams IsNot Nothing Then
                WritingParams.AbortOperation = True
            End If
            If ReadingParams IsNot Nothing Then
                ReadingParams.AbortOperation = True
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub cmd_ident_Click(sender As Object, e As EventArgs) Handles cmd_ident.Click
        Try
            cmd_ident.Enabled = False
            For i = 0 To 8
                FCUSB.USB_LEDOff()
                FCUSB.USB_WaitForComplete()
                Application.DoEvents()
                Utilities.Sleep(100)
                FCUSB.USB_LEDOn()
                FCUSB.USB_WaitForComplete()
                Application.DoEvents()
                Utilities.Sleep(100)
            Next
        Catch ex As Exception
        Finally
            cmd_ident.Enabled = True
        End Try
    End Sub

    Private Sub cmd_edit_CheckedChanged(sender As Object, e As EventArgs) Handles cmd_edit.CheckedChanged
        If cmd_edit.Checked AndAlso cmd_edit.Enabled Then
            HexEditor64.EDIT_MODE = True
        Else
            If cmd_edit.Enabled Then
                If HexEditor64.HexEdit_Changes.Count > 0 Then
                    If MsgBox("Save changes and write data to Flash?", vbYesNo, "Confirm data write operation") = vbYes Then
                        Dim t As New Threading.Thread(AddressOf WriteChangesMadeInEditMode)
                        t.Name = "tdWriteEditChanges"
                        t.Start()
                    End If
                End If
            End If
            HexEditor64.EDIT_MODE = False
        End If
    End Sub

    'Number of bytes shown on the left hand side of the control
    Public Function GetHexAddrSize() As Integer
        Return HexEditor64.HexDataByteSize
    End Function
    'Number of bytes show in the middle
    Public Function GetHexDataSize() As Integer
        Return (HexEditor64.GetVisisbleDataAreaCount / 2)
    End Function

#Region "Edit Mode"

    Private Sub WriteChangesMadeInEditMode()
        Try
            DisableControls(False) 'You can not cancel this
            Me.IN_OPERATION = True
            FCUSB.USB_LEDBlink()
            em_sector_list = New List(Of editmode_sector)
            RaiseEvent SetStatus("Programming changes to Flash device")
            For Each change In HexEditor64.HexEdit_Changes
                editmode_addchange(change.address, change.new_byte)
            Next
            For Each sector In em_sector_list
                RaiseEvent WriteMemory(sector.sector_addr, sector.sector_data, False, Me.AreaSelected, True)
            Next
        Catch ex As Exception
        Finally
            RaiseEvent SetStatus("Flash program operation has completed")
            FCUSB.USB_LEDOn()
            EnableControls()
            GetFocus()
            HexEditor64.UpdateScreen()
            Me.IN_OPERATION = False
        End Try
    End Sub

    Private em_sector_list As List(Of editmode_sector)

    Private Class editmode_sector
        Public sector_addr As Long
        Public sector_index As Integer
        Public sector_size As UInt32
        Public sector_data() As Byte
    End Class

    Private Function editmode_getitem(sec_index) As editmode_sector
        For Each item As editmode_sector In em_sector_list
            If item.sector_index = sec_index Then Return item
        Next
        Dim new_item As New editmode_sector
        new_item.sector_index = sec_index
        RaiseEvent GetSectorBaseAddress(sec_index, Me.AreaSelected, new_item.sector_addr)
        RaiseEvent GetSectorSize(sec_index, Me.AreaSelected, new_item.sector_size)
        ReDim new_item.sector_data(new_item.sector_size - 1)
        RaiseEvent ReadMemory(new_item.sector_addr, new_item.sector_data, Me.AreaSelected) 'Preload data into sector
        em_sector_list.Add(new_item)
        Return new_item
    End Function

    Private Sub editmode_addchange(addr As Long, dt As Byte)
        Dim sector_index As UInt32 = 0
        Dim base_addr As Long = 0 'base of the sector address
        RaiseEvent GetSectorIndex(addr, Me.AreaSelected, sector_index)
        RaiseEvent GetSectorBaseAddress(sector_index, Me.AreaSelected, base_addr)
        Dim item As editmode_sector = editmode_getitem(sector_index)
        item.sector_data(addr - base_addr) = dt
    End Sub

#End Region

End Class
