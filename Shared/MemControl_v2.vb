'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2017 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK

Public Class MemControl_v2
    Private AreaSelected As FlashMemory.FlashArea = FlashMemory.FlashArea.All
    Private FlashName As String 'Contains the MFG and PART NUMBER
    Private FlashSize As UInt32 'The size of the flash (not including spare area)
    Private FlashBase As UInt32 'Offset if this device is not at 0x0
    Private FlashAvailable As UInt32 'The total bytes available for the hex editor
    Private HexLock As New Object 'Used to lock the gui
    Private EnableChipErase As Boolean = True 'EEPROM devices do not allow this

    Public LAST_WRITE_OPERATION As XFER_Operation = Nothing

    Public Event WriteConsole(ByVal msg As String) 'Writes the console/windows console
    Public Event SetStatus(ByVal msg As String) 'Sets the text on the status bar
    Public Event ReadMemory(ByVal base_addr As UInt32, ByRef data() As Byte, ByVal area As FlashMemory.FlashArea) 'We want to get data from the normal memory area
    Public Event ReadStream(ByVal data_stream As IO.Stream, ByRef f_params As ReadParameters)
    Public Event WriteMemory(ByVal base_addr As UInt32, ByVal data() As Byte, ByVal area As FlashMemory.FlashArea, ByRef Successful As Boolean) 'Write data to the normal area
    Public Event WriteStream(ByVal data_stream As IO.Stream, ByRef f_params As WriteParameters, ByRef Successful As Boolean)
    Public Event EraseMemory()
    Public Event EraseNandBlock(ByVal base_addr As UInt32)
    Public Event SuccessfulWrite(ByVal x As XFER_Operation)

    Public ReadingParams As ReadParameters
    Public WritingParams As WriteParameters

#Region "GUI TEXT"
    Public Shared RNGBOX_OK As String = "OK" 'RM.GetString("fcusb_okbutton")
    Public Shared RNGBOX_CANCEL As String = "Cancel" 'RM.GetString("fcusb_cancel")
    Public Shared RNGBOX_BASE As String = "Base address" 'RM.GetString("fcusb_baseaddress")
    Public Shared RNGBOX_LEN As String = "Length" 'RM.GetString("fcusb_length")
    Public Shared MEM_VERIFY_AT As String = "Verifing written data at {0}" 'RM.GetString("fcusb_memif_verify")
    Public Shared MEM_VERIFY_OK As String = "Data verification was successful" 'RM.GetString("fcusb_memif_verifyokay")
    Public Shared MEM_VERIFY_ERR As String = "Address {0}: wrote {1} and read {2} ({3} mismatches)" 'RM.GetString("fcusb_memif_verifyerr2")
    Public Shared IO_SAVE_TYPE As String = "Choose hard drive location and output data format" 'RM.GetString("fcusb_filesave_type")
    Public Shared IO_FILE_CHOOSE As String = "Choose file to write into {0}" 'RM.GetString("fcusb_mem_choosefile")
    Public Shared IO_FILE_WRITING As String = "File chosen, writting into {0}" 'RM.GetString("fcusb_mem_filechosen")
    Public Shared IO_FILE_CANCEL As String = "User canceled. No data written to {0}"  'RM.GetString("fcusb_mem_usercanwrite")
    Public Shared IO_FILESAVE_CANCELED As String = "User canceled. No data has been saved" 'RM.GetString("fcusb_filesave_canceled")
    Public Shared MEM_BEGIN_READ As String = "Beginning memory read from {0}" 'String.Format(RM.GetString("fcusb_memif_beginread")
    Public Shared MEM_START_ADR As String = "Start address: {0} ({1}) Length: {2}" 'RM.GetString("fcusb_memif_startaddr")
    Public Shared MEM_USER_CANCEL As String = "User cancelled read operation" 'RM.GetString("fcusb_mem_usercanceledread")
    Public Shared MEM_READ_DONE As String = "Read operation complete"   'RM.GetString("fcusb_memif_readdone")
    Public Shared MEM_INTEL_HEX As String = "Converted data read into Intel hex format for saving" 'RM.GetString("fcusb_filesave_tohex")
    Public Shared MEM_WRITE_SUCCESS As String = "{0} successfully saved to disk" 'RM.GetString("fcusb_filesave_sucess")
    Public Shared MEM_OPEN_INTEL As String = "Opened file for writing: {0} (Intel hex format), total file size: {1} bytes" 'RM.GetString("fcusb_mem_opened_intel")
    Public Shared MEM_SEL_RANGE As String = "Select range to write data to {0}" 'RM.GetString("fcusb_mem_selectwriterange")
    Public Shared MEM_WRITING_FILE As String = "Writing file {0} into {1} ({2} bytes to write)" 'RM.GetString("fcusb_mem_writing")
    Public Shared MEM_OPENED_BIN As String = "Opened file for writing: {0} (binary hex format), total file size: {1} bytes" 'RM.GetString("fcusb_mem_opened_bin")
    Public Shared MEM_ERR1 As String = "Error, file does not exist or contains no data" 'RM.GetString("fcusb_mem_err1")
    Public Shared MEM_READ_FROM As String = "Select range to read from {0}" 'RM.GetString("fcusb_mem_readfrom")
    Public Shared MEM_READ_CANCELED As String = "Read operation canceled" 'RM.GetString("fcusb_mem_readcanceled")
    Public Shared MEM_READ_START As String = "Beginning memory read operation" 'RM.GetString("fcusb_mem_readstart")
    Public Shared WR_OPER_CANCELED As String = "Flash write operation canceled" 'RM.GetString("fcusb_mem_writecanceled")
    Public Shared WR_OPER_START As String = "Beginning memory write operation" 'RM.GetString("fcusb_mem_writestart")
    Public Shared WR_USER_CANCELED As String = "User cancelled write operation" 'RM.GetString("fcusb_mem_cancelled")
    Public Shared WR_OPER_FAILED As String = "Write operation was not successful" 'No text for this yet
    Public Shared WR_OPER_COMPLETE1 As String = "Write operation complete, {0} bytes written." 'RM.GetString("fcusb_memif_writedone")
    Public Shared WR_OPER_COMPLETE2 As String = "Write Flash operation complete!" 'RM.GetString("fcusb_memif_writecomplete")
    Public Shared WR_SUMMARY_SPEED As String = "{0} bytes written in {1} seconds, {2} Bytes/s" 'RM.GetString("fcusb_memif_writespeed")

    Public Sub SetUserLanguage(ByRef ResMngr As Resources.ResourceManager)
        RNGBOX_OK = ResMngr.GetString("fcusb_okbutton")
        RNGBOX_CANCEL = ResMngr.GetString("fcusb_cancel")
        RNGBOX_BASE = ResMngr.GetString("fcusb_baseaddress")
        RNGBOX_LEN = ResMngr.GetString("fcusb_length")
        MEM_VERIFY_AT = ResMngr.GetString("fcusb_memif_verify")
        MEM_VERIFY_OK = ResMngr.GetString("fcusb_memif_verifyokay")
        MEM_VERIFY_ERR = ResMngr.GetString("fcusb_memif_verifyerr2")
        IO_SAVE_TYPE = ResMngr.GetString("fcusb_filesave_type")
        IO_FILE_CHOOSE = ResMngr.GetString("fcusb_mem_choosefile")
        IO_FILE_WRITING = ResMngr.GetString("fcusb_mem_filechosen")
        IO_FILE_CANCEL = ResMngr.GetString("fcusb_mem_usercanwrite")
        IO_FILESAVE_CANCELED = ResMngr.GetString("fcusb_filesave_canceled")
        MEM_BEGIN_READ = ResMngr.GetString("fcusb_memif_beginread")
        MEM_START_ADR = ResMngr.GetString("fcusb_memif_startaddr")
        MEM_USER_CANCEL = ResMngr.GetString("fcusb_mem_usercanceledread")
        MEM_READ_DONE = ResMngr.GetString("fcusb_memif_readdone")
        MEM_INTEL_HEX = ResMngr.GetString("fcusb_filesave_tohex")
        MEM_WRITE_SUCCESS = ResMngr.GetString("fcusb_filesave_sucess")
        MEM_OPEN_INTEL = ResMngr.GetString("fcusb_mem_opened_intel")
        MEM_SEL_RANGE = ResMngr.GetString("fcusb_mem_selectwriterange")
        MEM_WRITING_FILE = ResMngr.GetString("fcusb_mem_writing")
        MEM_OPENED_BIN = ResMngr.GetString("fcusb_mem_opened_bin")
        MEM_ERR1 = ResMngr.GetString("fcusb_mem_err1")
        MEM_READ_FROM = ResMngr.GetString("fcusb_mem_readfrom")
        MEM_READ_CANCELED = ResMngr.GetString("fcusb_mem_readcanceled")
        MEM_READ_START = ResMngr.GetString("fcusb_mem_readstart")
        WR_OPER_CANCELED = ResMngr.GetString("fcusb_mem_writecanceled")
        WR_OPER_START = ResMngr.GetString("fcusb_mem_writestart")
        WR_USER_CANCELED = ResMngr.GetString("fcusb_mem_cancelled")
        WR_OPER_COMPLETE1 = ResMngr.GetString("fcusb_memif_writedone")
        WR_OPER_COMPLETE2 = ResMngr.GetString("fcusb_memif_writecomplete")
        WR_SUMMARY_SPEED = ResMngr.GetString("fcusb_memif_writespeed")
    End Sub

#End Region

    Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

    End Sub

    Private Sub MemControl_v2_Load(ByVal sender As Object, e As EventArgs) Handles Me.Load
        SetProgress(0)
        SetSpeedStatus("")
        cmd_cancel.Visible = False
    End Sub

    Public Class XFER_Operation
        Public FileName As IO.FileInfo  'Contains the shortname of the file being opened or written to
        Public DataStream As IO.Stream
        Public Offset As UInt32
        Public Size As UInt32
    End Class

    'Call this to setup this control
    Public Sub InitMemoryDevice(ByVal Name As String, ByVal flash_base As UInt32, ByVal flash_size As UInt32, ByVal access As access_mode)
        Me.FlashName = Name
        Me.FlashSize = flash_size
        Me.FlashAvailable = flash_size 'Main area first
        Me.FlashBase = flash_base
        cmd_area.Visible = False 'Only enable this on device that have more than one internal area to view
        gb_flash.Text = Name
        SetProgress(0)
        SetSpeedStatus("")
        memory_map = Nothing
        SetSelectedArea(FlashMemory.FlashArea.Main)
    End Sub

    Public Enum access_mode
        [ReadOnly] 'We can read but can not write
        [ReadWrite]
    End Enum

    Friend Class DynamicRangeBox
        Private BaseTxt As New TextBox
        Private LenTxt As New TextBox
        'Allows you to click and move the form around
        Private MouseDownOnForm As Boolean = False
        Private ClickPoint As Point
        Private CurrentBase As UInt32
        Private CurrentSize As UInt32
        Private CurrentMax As UInt32

        Sub New()

        End Sub

        Public Function ShowRangeBox(ByRef BaseAddress As UInt32, ByRef Size As UInt32, ByVal MaxData As UInt32) As Boolean
            If Size > MaxData Then Size = MaxData
            Dim InputSelectionForm As New Form With {.Width = 172, .Height = 80}
            InputSelectionForm.FormBorderStyle = FormBorderStyle.FixedToolWindow
            InputSelectionForm.ShowInTaskbar = False
            InputSelectionForm.ShowIcon = False
            InputSelectionForm.ControlBox = False
            Dim BtnOK As New Button With {.Text = RNGBOX_OK, .Width = 60, .Height = 20, .Left = 90, .Top = 50}
            Dim BtnCAN As New Button With {.Text = RNGBOX_CANCEL, .Width = 60, .Height = 20, .Left = 20, .Top = 50}
            Dim Lbl1 As New Label With {.Text = RNGBOX_BASE, .Left = 10, .Top = 5}
            Dim Lbl2 As New Label With {.Text = RNGBOX_LEN, .Left = 105, .Top = 5}
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
            If InputSelectionForm.ShowDialog() = Windows.Forms.DialogResult.OK Then
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
                Dim Btn As Windows.Forms.TextBox = CType(sender, TextBox)
                Dim SendFrm As Form = Btn.FindForm
                SendFrm.DialogResult = Windows.Forms.DialogResult.OK
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
            Dim Btn As Windows.Forms.Button = CType(sender, Button)
            Dim SendFrm As Form = Btn.FindForm
            SendFrm.DialogResult = Windows.Forms.DialogResult.OK
        End Sub

        Private Sub Dyn_CancelClick(ByVal sender As System.Object, ByVal e As System.EventArgs)
            Dim Btn As Button = CType(sender, Button)
            Dim SendFrm As Form = Btn.FindForm
            SendFrm.DialogResult = DialogResult.Cancel
        End Sub

        Private Sub Dyn_MouseDown(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            MouseDownOnForm = True
            ClickPoint = New Point(Windows.Forms.Cursor.Position.X, Windows.Forms.Cursor.Position.Y)
        End Sub

        Private Sub Dyn_MouseUp(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            MouseDownOnForm = False
        End Sub

        Private Sub Dyn_MouseMove(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            If MouseDownOnForm Then
                Dim newPoint As New Point(Windows.Forms.Cursor.Position.X, Windows.Forms.Cursor.Position.Y)
                Dim ThisForm As Form = CType(sender, Form)
                ThisForm.Top = ThisForm.Top + (newPoint.Y - ClickPoint.Y)
                ThisForm.Left = ThisForm.Left + (newPoint.X - ClickPoint.X)
                ClickPoint = newPoint
            End If
        End Sub
        'Hanldes the move if a label is being dragged
        Private Sub DynLabel_MouseMove(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            If MouseDownOnForm Then
                Dim newPoint As New Point(Windows.Forms.Cursor.Position.X, Windows.Forms.Cursor.Position.Y)
                Dim Btn As Windows.Forms.Label = CType(sender, Label)
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

#Region "Delegates"
    Private Delegate Sub cbSetProgress(ByVal value As Integer)
    Private Delegate Sub cbSetSpeedStatus(ByVal Msg As String)
    Private Delegate Sub cbInvokeControl()
    Private Delegate Sub cbAddressUpdate(ByVal Address As UInt32)
    Private Delegate Sub cbControls()
    Private Delegate Sub cbDisableControls(ByVal show_cancel As Boolean)
#End Region

#Region "Memory Map (Multiple memory areas)"
    Private Delegate Sub cbExtendedAreaVisibility(ByVal show As Boolean)
    Private memory_map() As memory_map_item
    Private PageCount As UInt32 'Total number of pages in this flash device
    Private PagesPerBlock As UInt32 'Number of pages that make up a single block
    Private PageSize As UInt16 'Bytes per page
    Private PageExtended As UInt16 'Number of additional bytes

    Private Structure memory_map_item
        Public logical_start As UInt32 'This is the combined location of the start address
        Public logical_end As UInt32
        Public physical_start As UInt32 'This is the area by itself
        Public physical_end As UInt32
        Public block_address As UInt32 'the address of the block
        Public memory_area As FlashMemory.FlashArea
    End Structure

    Private Sub SetSelectedArea(ByVal area As FlashMemory.FlashArea)
        AreaSelected = area
        Select Case area
            Case FlashMemory.FlashArea.Main
                cmd_area.Text = "Main"
                FlashAvailable = FlashSize
            Case FlashMemory.FlashArea.OOB
                cmd_area.Text = "Spare"
                FlashAvailable = PageCount * PageExtended
            Case FlashMemory.FlashArea.All
                cmd_area.Text = "All Data"
                FlashAvailable = FlashSize + (PageCount * PageExtended)
        End Select
        Editor.CreateHexViewer(Me.FlashBase, FlashAvailable)
        txtAddress.Text = "0x0"
        RefreshView()
    End Sub
    'This setups the editor to use a Flash with an extended area (such as spare data)
    Public Sub AddExtendedArea(ByVal page_count As UInt32, ByVal page_size As UInt16, ByVal ext_size As UInt16, ByVal pages_per_block As UInt32)
        cmd_area.Visible = True
        PageCount = page_count
        PagesPerBlock = pages_per_block
        PageSize = page_size 'i.e. 2048
        PageExtended = ext_size
        Dim addr_x As UInt32 = 0
        Dim main_x As UInt32 = 0
        Dim ext_x As UInt32 = 0
        Dim block_address As UInt32 = 0
        ReDim memory_map((PageCount * 2) - 1)
        For i = 0 To PageCount - 1
            If i Mod PagesPerBlock = 0 Then
                block_address = main_x
            End If
            Dim main As New memory_map_item
            main.block_address = block_address
            main.memory_area = FlashMemory.FlashArea.Main
            main.logical_start = addr_x
            main.logical_end = addr_x + page_size - 1
            main.physical_start = main_x
            main.physical_end = main_x + page_size - 1
            main_x += page_size
            addr_x += page_size
            Dim ext As New memory_map_item
            ext.block_address = block_address
            ext.memory_area = FlashMemory.FlashArea.OOB
            ext.logical_start = addr_x
            ext.logical_end = addr_x + ext_size - 1
            ext.physical_start = ext_x
            ext.physical_end = ext_x + ext_size - 1
            ext_x += ext_size
            addr_x += ext_size
            memory_map(i * 2) = main
            memory_map((i * 2) + 1) = ext
        Next
        'Possibly set something here
    End Sub

    Private Sub ExtendedAreaVisibility(ByVal show As Boolean)
        If Me.InvokeRequired Then
            Dim d As New cbExtendedAreaVisibility(AddressOf ExtendedAreaVisibility)
            Me.Invoke(d, {show})
        Else
            cmd_area.Visible = show
        End If
    End Sub

    Private Function SetMemoryFromMap(ByVal map_ind As UInt32, ByVal logical_addr As UInt32, ByVal count As UInt32, ByRef data_stream As IO.Stream) As UInt32
        Dim m As memory_map_item = memory_map(map_ind)
        Dim offset As Long = logical_addr - m.logical_start 'Offset = number of bytes into this map
        Dim area_size As Long = (m.physical_end - m.physical_start) + 1
        Dim BytesAvailable As Integer = area_size - offset
        If count > BytesAvailable Then count = BytesAvailable
        Dim f_params As New WriteParameters
        f_params.Address = offset
        f_params.Count = count
        f_params.UpdateProgress = Nothing 'We do not want updates
        f_params.UpdateSpeed = Nothing
        f_params.Memory_Area = m.memory_area
        f_params.Verify = False
        f_params.EraseSector = True
        RaiseEvent WriteStream(data_stream, f_params, Nothing)
        Return count 'This is the number of bytes we wrote to the memory
    End Function

    Private Sub MemoryMap_ReadStream(ByVal addr_hybrid As UInt32, ByVal count As UInt32, ByVal data_stream As IO.Stream)
        Dim Clock As New Stopwatch : Clock.Start()
        Dim bytes_left As UInt32 = count
        Try
            Dim base_index As Integer = GetMemoryMapIndex(addr_hybrid)
            Dim map As memory_map_item = memory_map(base_index)
            If (map.memory_area = FlashMemory.FlashArea.Main) Then
                If (map.logical_start <> addr_hybrid) Then
                    Dim main() As Byte = MemoryMap_ReadPage(map, addr_hybrid, bytes_left)
                    addr_hybrid += main.Length 'Now we are at oob
                    data_stream.Write(main, 0, main.Length)
                    bytes_left = bytes_left - main.Length
                    If (bytes_left > 0) Then
                        Dim oob() As Byte = MemoryMap_ReadPage(memory_map(base_index + 1), addr_hybrid, bytes_left)
                        addr_hybrid += oob.Length 'Now we are at start of next block
                        data_stream.Write(oob, 0, oob.Length)
                        bytes_left = bytes_left - oob.Length
                    End If
                End If
            ElseIf (map.memory_area = FlashMemory.FlashArea.OOB) Then
                Dim oob() As Byte = MemoryMap_ReadPage(map, addr_hybrid, bytes_left)
                addr_hybrid += oob.Length 'Now we are at start of next block
                data_stream.Write(oob, 0, oob.Length)
                bytes_left = bytes_left - oob.Length
            End If
            If bytes_left = 0 Then Exit Sub
            data_stream.Flush()
            Do While (bytes_left > 0)
                Dim EntireBlock() As Byte = MemoryMap_ReadEntireBlock(addr_hybrid)
                Dim AmountToWrite As Integer = Math.Min(EntireBlock.Length, bytes_left)
                data_stream.Write(EntireBlock, 0, AmountToWrite)
                data_stream.Flush()
                addr_hybrid += EntireBlock.Length
                bytes_left = bytes_left - AmountToWrite
                Dim Percent As Single = CSng(((count - bytes_left) / count) * 100) 'Calulate % done
                SetProgress(CInt(Percent))
                Dim StatusLoc As String = Format((count - bytes_left), "#,###") & " of " & Format(count, "#,###") & " Bytes " 'Format Status
                Dim StatusPrec As String = "(" & Math.Round(Percent, 0) & "%) " 'Format Status
                RaiseEvent SetStatus("Reading Flash " & StatusLoc & StatusPrec) '& StatusSpeeed 'Print Status
                Dim StatusSpeeed As String = Format(Math.Round((count - bytes_left) / (Clock.ElapsedMilliseconds / 1000)), "#,###") & " Bytes/s"  'Format Status
                SetSpeedStatus(StatusSpeeed)
            Loop
        Catch ex As Exception
        Finally
            data_stream.Flush()
            Clock.Stop()
        End Try
    End Sub

    Private Function MemoryMap_ReadEntireBlock(ByVal addr_hybrid As UInt32) As Byte()
        Dim BlockSize As UInt32 = PageSize * PagesPerBlock
        Dim BlockSizeExt As UInt32 = (PageSize + PageExtended) * PagesPerBlock 'A full block with oob
        Dim OOBSize As UInt32 = BlockSizeExt - BlockSize 'The size of all the OOB for this block
        Dim base_index As Integer = GetMemoryMapIndex(addr_hybrid)
        Dim map As memory_map_item = memory_map(base_index)
        Dim BlockData(BlockSize - 1) As Byte
        RaiseEvent ReadMemory(map.physical_start, BlockData, FlashMemory.FlashArea.Main)
        If BlockData Is Nothing Then Return Nothing
        Dim OOBData(OOBSize - 1) As Byte
        Dim addr_oob As UInt32 = (map.physical_start / BlockSize) * OOBSize
        RaiseEvent ReadMemory(addr_oob, OOBData, FlashMemory.FlashArea.OOB)
        If OOBData Is Nothing Then Return Nothing
        Dim EntireBlock(BlockSizeExt - 1) As Byte
        Dim blk_ptr As UInt32 = 0
        Dim ext_ptr As UInt32 = 0
        Dim dst_ptr As UInt32 = 0
        For i = 0 To PagesPerBlock - 1
            Array.Copy(BlockData, blk_ptr, EntireBlock, dst_ptr, PageSize)
            Array.Copy(OOBData, ext_ptr, EntireBlock, dst_ptr + PageSize, PageExtended)
            blk_ptr += PageSize
            ext_ptr += PageExtended
            dst_ptr += (PageSize + PageExtended)
        Next
        Return EntireBlock
    End Function
    'Reads data from the memory map (i.e. all data) Page,OOB,Page,OOB, etc.
    Private Sub MemoryMap_ReadData(ByVal addr_logical As UInt32, ByVal count As UInt32, ByRef data_stream As IO.Stream)
        Dim BytesLeft As UInt32 = count
        Dim map_ind As Integer = GetMemoryMapIndex(addr_logical) 'Gets the current map index
        Do Until BytesLeft = 0
            Dim map As memory_map_item = memory_map(map_ind)
            Dim page_data() As Byte = MemoryMap_ReadPage(map, addr_logical, BytesLeft)
            map_ind += 1 'Next time we need to look at the next base
            BytesLeft -= page_data.Length
            addr_logical += page_data.Length
            data_stream.Write(page_data, 0, page_data.Length)
        Loop
    End Sub

    Private Function MemoryMap_ReadPage(ByVal map As memory_map_item, ByVal hybrid_addr As UInt32, ByVal count As UInt32) As Byte()
        Dim offset As Long = hybrid_addr - map.logical_start  'Offset = number of bytes into this map
        Dim area_size As Long = (map.physical_end - map.physical_start) + 1
        Dim BytesAvailable As Integer = area_size - offset
        If count > BytesAvailable Then count = BytesAvailable
        Dim data_out(count - 1) As Byte
        If (map.memory_area = FlashMemory.FlashArea.Main) Then
            RaiseEvent ReadMemory(map.physical_start + offset, data_out, FlashMemory.FlashArea.Main)
        Else 'spare area
            RaiseEvent ReadMemory(map.physical_start + offset, data_out, FlashMemory.FlashArea.OOB)
        End If
        Return data_out
    End Function

    Private Function GetMemoryMapIndex(ByVal hybrid_addr As UInt32) As Integer
        For i = 0 To memory_map.Length - 1
            If hybrid_addr >= memory_map(i).logical_start AndAlso hybrid_addr <= memory_map(i).logical_end Then
                Return i
            End If
        Next
        Return -1 'Means not found
    End Function

    Private Sub MemoryMap_WriteStream(ByVal base_addr As UInt32, ByVal count As UInt32, ByVal data_stream As IO.Stream, ByRef Successful As Boolean, ByRef Clock As Stopwatch)
        Clock = New Stopwatch
        Clock.Start()
        Try
            Dim BlockSize As UInt32 = (PagesPerBlock * PageSize) 'This is the size of the block, without spare
            Dim BlockSizeSpare As UInt32 = (PagesPerBlock * PageExtended)  'This is the size of the spare area within the block
            Dim BlockSizeTotal As UInt32 = (PagesPerBlock * (PageSize + PageExtended)) 'This should be the total size of the block
            Dim logical_addr As UInt32 = base_addr
            Dim BytesLeft As UInt32 = count
            Do Until BytesLeft = 0
                Dim new_block(BlockSizeTotal - 1) As Byte
                Dim map_ind As Integer = GetMemoryMapIndex(logical_addr)
                If map_ind = -1 Then
                    RaiseEvent WriteConsole("Opperation completed as Flash device has reached the last block")
                    RaiseEvent WriteConsole("Number of bytes left: " & BytesLeft.ToString)
                    Exit Do
                End If
                Dim m As memory_map_item = memory_map(map_ind)
                Dim offset As UInt32 = (logical_addr - m.logical_start)
                If (offset > 0) Then
                    logical_addr = m.logical_start
                    Using memreader As New IO.MemoryStream
                        MemoryMap_ReadData(logical_addr, offset, memreader)
                        memreader.Position = 0
                        memreader.Read(new_block, 0, offset)
                    End Using
                End If
                Dim BytesToRead As UInt32 = (BlockSizeTotal - offset)
                If (BytesLeft > BytesToRead) Then
                    data_stream.Read(new_block, offset, BytesToRead)
                ElseIf BytesLeft < BytesToRead Then
                    Dim Difference As UInt32 = BytesToRead - BytesLeft
                    BytesToRead = BytesLeft
                    data_stream.Read(new_block, offset, BytesToRead)
                    offset += BytesToRead
                    Using memreader As New IO.MemoryStream
                        MemoryMap_ReadData(logical_addr + offset, Difference, memreader)
                        memreader.Position = 0
                        memreader.Read(new_block, 0, offset)
                    End Using
                Else 'MATCH
                    data_stream.Read(new_block, offset, BytesToRead)
                End If
                Dim BlockData(BlockSize - 1) As Byte
                Dim SpareData(BlockSizeSpare - 1) As Byte
                Dim MainOffset As UInt32 = 0
                Dim SpareOffset As UInt32 = 0
                Using memreader As New IO.MemoryStream(new_block)
                    For i = 0 To PagesPerBlock - 1
                        memreader.Read(BlockData, MainOffset, PageSize)
                        memreader.Read(SpareData, SpareOffset, PageExtended)
                        MainOffset += PageSize
                        SpareOffset += PageExtended
                    Next
                End Using
                RaiseEvent EraseNandBlock(m.block_address)
                Using block_stream As New IO.MemoryStream(BlockData)
                    Dim f_params As New WriteParameters
                    f_params.Address = m.block_address
                    f_params.Count = BlockSize
                    f_params.DisplayStatus = False 'We will not display
                    f_params.Memory_Area = FlashMemory.FlashArea.Main
                    f_params.Verify = False
                    f_params.EraseSector = False
                    RaiseEvent WriteStream(block_stream, f_params, Successful)
                End Using
                Using spare_stream As New IO.MemoryStream(SpareData)
                    Dim f_params As New WriteParameters
                    Dim Block_Ind As UInt32 = m.block_address / BlockSize
                    f_params.Address = (Block_Ind * BlockSizeSpare)
                    f_params.Count = BlockSizeSpare
                    f_params.DisplayStatus = False 'We will not display
                    f_params.Memory_Area = FlashMemory.FlashArea.OOB
                    f_params.Verify = False
                    f_params.EraseSector = False 'We have already previously erased this block
                    RaiseEvent WriteStream(spare_stream, f_params, Successful)
                End Using
                If MySettings.VERIFY_WRITE Then 'If successful
                    Clock.Stop()
                    If VerifyWrittenBlock(logical_addr, new_block) Then
                        BytesLeft -= BytesToRead
                    Else 'This block was BAD
                    End If
                    Clock.Start()
                Else
                    BytesLeft -= BytesToRead
                End If
                logical_addr += new_block.Length 'We will increase the block address
                If count <> BytesLeft Then
                    Dim Percent As Single = CSng(((count - BytesLeft) / count) * 100) 'Calulate % done
                    SetProgress(CInt(Percent))
                    Dim StatusLoc As String = Format((count - BytesLeft), "#,###") & " of " & Format(count, "#,###") & " Bytes " 'Format Status
                    Dim StatusPrec As String = "(" & Math.Round(Percent, 0) & "%) " 'Format Status
                    Dim StatusSpeeed As String = Format(Math.Round((count - BytesLeft) / (Clock.ElapsedMilliseconds / 1000)), "#,###") & " Bytes/s"  'Format Status
                    RaiseEvent SetStatus("Writing Flash " & StatusLoc & StatusPrec) '& StatusSpeeed 'Print Status
                    SetSpeedStatus(StatusSpeeed)
                End If
            Loop
        Catch ex As Exception
        Finally
            Clock.Stop()
        End Try
    End Sub

    Private Function VerifyWrittenBlock(ByVal hybrid_addr As UInt32, ByVal BlockData() As Byte) As Boolean
        RaiseEvent SetStatus(String.Format(MEM_VERIFY_AT, "0x" & Hex(hybrid_addr)))
        Dim FailedAttempts As Integer = 0
        Dim ReadResult As Boolean = False
        Do
            Dim DataVerify() As Byte = MemoryMap_ReadEntireBlock(hybrid_addr)
            If DataVerify Is Nothing Then Return False
            Dim Byte_Correct As Byte
            Dim Byte_Incorrect As Byte
            Dim MiscountCounter As Integer = 0
            Dim Address_Error As Long = 0
            For i = 0 To BlockData.Length - 1
                If (Not BlockData(i) = DataVerify(i)) Then
                    If MiscountCounter = 0 Then Address_Error = hybrid_addr + i : Byte_Correct = BlockData(i) : Byte_Incorrect = DataVerify(i)
                    MiscountCounter += 1
                End If
            Next
            If MiscountCounter = 0 Then ReadResult = True 'Verification successful
            If ReadResult Then
                RaiseEvent SetStatus(MEM_VERIFY_OK)
                Application.DoEvents()
                Utilities.Sleep(500)
            Else
                Dim addr_str As String = "0x" & Hex(Address_Error)
                Dim wrote_str As String = "0x" & Hex(Byte_Correct)
                Dim read_str As String = "0x" & Hex(Byte_Incorrect)
                RaiseEvent WriteConsole(String.Format(MEM_VERIFY_ERR, "0x" & Hex(Address_Error), "0x" & Hex(Byte_Correct), "0x" & Hex(Byte_Incorrect), MiscountCounter))
                FailedAttempts += 1
                If FailedAttempts = 1 Then 'Make this changeable in the future
                    Dim map_ind As Integer = GetMemoryMapIndex(hybrid_addr)
                    Dim m As memory_map_item = memory_map(map_ind)
                    Dim offset As Long = hybrid_addr - m.logical_start 'Offset = number of bytes into this map
                    Dim logical_addr As Long = m.physical_start + offset
                    RaiseEvent WriteConsole("BAD NAND BLOCK AT address: 0x" & Hex(hybrid_addr) & " (Mapped to 0x" & Hex(logical_addr) & " )")
                    Return False 'Skip
                End If
                Utilities.Sleep(500)
            End If
        Loop Until ReadResult
        Return True 'Verification successful!
    End Function

#End Region

    Private Sub cmd_area_Click(ByVal sender As Object, e As EventArgs) Handles cmd_area.Click
        Select Case AreaSelected
            Case FlashMemory.FlashArea.Main
                SetSelectedArea(FlashMemory.FlashArea.OOB)
            Case FlashMemory.FlashArea.OOB
                SetSelectedArea(FlashMemory.FlashArea.All)
            Case FlashMemory.FlashArea.All
                SetSelectedArea(FlashMemory.FlashArea.Main)
        End Select
    End Sub

    Public Sub RefreshView()
        Try
            Editor.UpdateScreen()
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
            End If
        Catch ex As Exception
        End Try
    End Sub

    Public Sub SetSpeedStatus(ByVal Msg As String)
        Try
            If Me.InvokeRequired Then
                Dim d As New cbSetSpeedStatus(AddressOf SetSpeedStatus)
                Me.Invoke(d, New Object() {[Msg]})
            Else
                If Not Msg = "" Then
                    lbl_status.Text = "Transfer speed: " & Msg
                Else
                    lbl_status.Text = ""
                End If
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
                cmd_erase.Enabled = True
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
            cmd_write.Enabled = True
            SetChipEraseButton()
            cmd_read.Visible = True
            cmd_write.Visible = True
            cmd_erase.Visible = True
            cmd_area.Enabled = True
            cmd_cancel.Visible = False
        End If
    End Sub
    'We want to disable read/write/erase controls
    Public Sub DisableControls(ByVal show_cancel As Boolean)
        If Me.InvokeRequired Then
            Dim d As New cbDisableControls(AddressOf DisableControls)
            Me.Invoke(d, {show_cancel})
        Else
            cmd_read.Enabled = False
            cmd_write.Enabled = False
            cmd_erase.Enabled = False
            cmd_area.Enabled = False
            If show_cancel Then
                cmd_cancel.Visible = True
                cmd_cancel.Enabled = True
                cmd_read.Visible = False
                cmd_write.Visible = False
                cmd_erase.Visible = False
            Else
                cmd_cancel.Visible = False
                cmd_cancel.Enabled = False
                cmd_read.Visible = True
                cmd_write.Visible = True
                cmd_erase.Visible = True
            End If
        End If
    End Sub

    Public Sub GetFocus()
        If Me.InvokeRequired Then
            Dim d As New cbControls(AddressOf GetFocus)
            Me.Invoke(d)
        Else
            Editor.Focus()
        End If
    End Sub

#Region "Address Box"

    Private Sub AddressUpdate(ByVal Address As UInt32) Handles Editor.AddressUpdate
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
            Editor.Focus() 'Makes this control loose focus and trigger the other event (lostfocus)
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
                Dim l As UInt32 = CUInt(input)
                Editor.GotoAddress(l)
            ElseIf Utilities.IsDataType.HexString(txtAddress.Text) Then
                Dim l As UInt32 = Utilities.HexToUInt(input)
                Editor.GotoAddress(l)
            Else
                txtAddress.Text = "0x" & Hex(Editor.TopAddress).ToUpper
            End If
        Catch ex As Exception
            txtAddress.Text = "0x" & Hex(Editor.TopAddress).ToUpper
        End Try
    End Sub

#End Region

    'Our hex viewer is asking for data to display
    Private Sub DataRequest(ByVal address As UInt32, ByRef data() As Byte) Handles Editor.RequestData
        Static RequestedData As Boolean = False
        If RequestedData Then Exit Sub
        Try : RequestedData = True
            Select Case AreaSelected
                Case FlashMemory.FlashArea.Main 'We only want to see main data
                    RaiseEvent ReadMemory(address, data, FlashMemory.FlashArea.Main) 'DONT DISPLAY PROGRESS
                Case FlashMemory.FlashArea.OOB  'We only want to see spare data
                    RaiseEvent ReadMemory(address, data, FlashMemory.FlashArea.OOB)'DONT DISPLAY PROGRESS
                Case FlashMemory.FlashArea.All  'We want to display a mix of main data and spare data
                    Using m As New IO.MemoryStream()
                        MemoryMap_ReadData(address, data.Length, m)
                        data = m.GetBuffer()
                        ReDim Preserve data(m.Length - 1)
                    End Using
            End Select
        Finally
            RequestedData = False
        End Try
    End Sub

    Private Function CreateFileForFlashRead(ByVal DefaultName As String, ByRef file As IO.FileInfo) As Boolean
        Try
            Dim Saveme As New SaveFileDialog
            Saveme.AddExtension = True
            Saveme.InitialDirectory = Application.StartupPath
            Saveme.Title = IO_SAVE_TYPE
            Saveme.CheckPathExists = True
            Saveme.FileName = DefaultName.Replace("/", "-")
            Dim BinFile As String = "Binary Files (*.bin)|*.bin"
            Dim IntelHexFrmt As String = "Intel Hex Format (*.hex)|*.hex"
            Saveme.Filter = BinFile & "|" & IntelHexFrmt & "|All files (*.*)|*.*"
            If Saveme.ShowDialog = Windows.Forms.DialogResult.OK Then
                Dim n As New IO.FileInfo(Saveme.FileName)
                If n.Exists Then n.Delete()
                file = n
                Return True
            End If
        Catch ex As Exception
            RaiseEvent SetStatus("Error opening file for writing")
        End Try
        Return False
    End Function

    Private Function OpenFileForWriteWrite(ByRef file As IO.FileInfo) As Boolean
        Dim BinFile As String = "Binary Files (*.bin)|*.bin"
        Dim IHexFormat As String = "Intel Hex Format (*.hex)|*.hex"
        Dim AllFiles As String = "All files (*.*)|*.*"
        Dim OpenMe As New OpenFileDialog
        OpenMe.AddExtension = True
        OpenMe.InitialDirectory = Application.StartupPath
        OpenMe.Title = String.Format(IO_FILE_CHOOSE, FlashName)
        OpenMe.CheckPathExists = True
        OpenMe.Filter = BinFile & "|" & IHexFormat & "|" & AllFiles 'Bin Files, Hex Files, All Files
        If OpenMe.ShowDialog = Windows.Forms.DialogResult.OK Then
            file = New IO.FileInfo(OpenMe.FileName)
            RaiseEvent SetStatus(String.Format(IO_FILE_WRITING, FlashName))
            Return True
        Else
            RaiseEvent SetStatus(String.Format(IO_FILE_CANCEL, FlashName))
            Return False
        End If
    End Function

    Private Sub ReadMemoryThread(ByVal read_params As XFER_Operation)
        Try
            GUI.OperationStarted()
            FCUSB_LedBlink()
            SetProgress(0)
            DisableControls(True)
            Try
                Try
                    Dim n As New IO.FileInfo(read_params.FileName.FullName)
                    If n.Exists Then n.Delete()
                Catch ex As Exception
                End Try
                RaiseEvent WriteConsole(String.Format(MEM_BEGIN_READ, FlashName))
                RaiseEvent WriteConsole(String.Format(MEM_START_ADR, read_params.Offset, "0x" & Utilities.Pad(Hex((read_params.Offset))), Format(read_params.Size, "#,###")))
                ReadingParams = New ReadParameters
                ReadingParams.Address = read_params.Offset
                ReadingParams.Count = read_params.Size
                ReadingParams.Timer = New Stopwatch
                ReadingParams.Memory_Area = AreaSelected
                ReadingParams.DisplayStatus = True
                ReadingParams.UpdateProgress = New cbSetProgress(AddressOf SetProgress)
                ReadingParams.UpdateSpeed = New cbSetSpeedStatus(AddressOf SetSpeedStatus)
                ReadingParams.Timer.Start()
                Using data_stream As IO.Stream = read_params.FileName.OpenWrite
                    Select Case AreaSelected
                        Case FlashMemory.FlashArea.Main 'We only want to see main data
                            RaiseEvent ReadStream(data_stream, ReadingParams)
                        Case FlashMemory.FlashArea.OOB  'We only want to see spare data
                            RaiseEvent ReadStream(data_stream, ReadingParams)
                        Case FlashMemory.FlashArea.All  'We want to display a mix of main data and spare data
                            MemoryMap_ReadStream(read_params.Offset, read_params.Size, data_stream)
                    End Select
                End Using
                If ReadingParams.AbortOperation Then
                    RaiseEvent SetStatus(MEM_USER_CANCEL)
                    Try
                        Dim n2 As New IO.FileInfo(read_params.FileName.FullName)
                        If n2.Exists Then n2.Delete()
                    Catch ex As Exception
                    End Try
                Else
                    Dim StatusSpeed As String = Format(Math.Round(read_params.Size / (ReadingParams.Timer.ElapsedMilliseconds / 1000)), "#,###") & " Bytes/s"
                    RaiseEvent WriteConsole(MEM_READ_DONE)
                    RaiseEvent WriteConsole("Read " & Format(read_params.Size, "#,###") & " bytes in " & (ReadingParams.Timer.ElapsedMilliseconds / 1000) & " seconds, " & StatusSpeed)
                    If read_params.FileName.Extension.ToUpper.EndsWith(".HEX") Then
                        Try
                            RaiseEvent WriteConsole("Converting binary file to Intel HEX format")
                            Dim data() As Byte = Utilities.FileIO.ReadBytes(read_params.FileName.FullName)
                            If data IsNot Nothing AndAlso data.Length > 0 Then
                                data = Utilities.BinToIntelHex(data)
                                Utilities.FileIO.WriteBytes(data, read_params.FileName.FullName)
                            End If
                            RaiseEvent WriteConsole(MEM_INTEL_HEX)
                        Catch ex As Exception
                        End Try
                    End If
                    RaiseEvent SetStatus(String.Format(MEM_WRITE_SUCCESS, read_params.FileName.Name))
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        Finally
            If read_params.DataStream IsNot Nothing Then read_params.DataStream.Dispose()
            USB_LEDOn() 'in case we where blinking
            EnableControls()
            SetProgress(0)
            SetSpeedStatus("")
            GetFocus()
            GUI.OperationStopped()
            ReadingParams = Nothing
        End Try
    End Sub

    Private Sub WriteMemoryThread(ByVal file_out As XFER_Operation)
        Try
            GUI.OperationStarted()
            FCUSB_LedBlink()
            SetProgress(0)
            DisableControls(True)
            Try
                Dim write_success As Boolean = False
                WritingParams = New WriteParameters
                WritingParams.Address = file_out.Offset
                WritingParams.Count = file_out.Size
                WritingParams.Verify = MySettings.VERIFY_WRITE
                WritingParams.EraseSector = True
                WritingParams.DisplayStatus = True
                WritingParams.UpdateProgress = New cbSetProgress(AddressOf SetProgress)
                WritingParams.UpdateSpeed = New cbSetSpeedStatus(AddressOf SetSpeedStatus)
                Select Case AreaSelected
                    Case FlashMemory.FlashArea.Main
                        WritingParams.Memory_Area = 0
                        RaiseEvent WriteStream(file_out.DataStream, WritingParams, write_success)
                    Case FlashMemory.FlashArea.OOB
                        WritingParams.Memory_Area = 1
                        RaiseEvent WriteStream(file_out.DataStream, WritingParams, write_success)
                    Case FlashMemory.FlashArea.All
                        MemoryMap_WriteStream(file_out.Offset, file_out.Size, file_out.DataStream, write_success, WritingParams.Timer)
                End Select
                file_out.DataStream.Dispose()
                file_out.DataStream = Nothing
                If WritingParams.AbortOperation Then
                    LAST_WRITE_OPERATION = Nothing
                    RaiseEvent SetStatus(WR_USER_CANCELED)
                ElseIf (Not write_success) Then
                    LAST_WRITE_OPERATION = Nothing
                    RaiseEvent SetStatus(WR_OPER_FAILED)
                Else
                    Dim Speed As String = CStr(Format(Math.Round(file_out.Size / (WritingParams.Timer.ElapsedMilliseconds / 1000)), "#,###"))
                    RaiseEvent SetStatus(String.Format(WR_OPER_COMPLETE1, Format(file_out.Size, "#,###")))
                    RaiseEvent WriteConsole(WR_OPER_COMPLETE2)
                    RaiseEvent WriteConsole(String.Format(WR_SUMMARY_SPEED, Format(file_out.Size, "#,###"), (WritingParams.Timer.ElapsedMilliseconds / 1000), Speed))
                    RaiseEvent SuccessfulWrite(LAST_WRITE_OPERATION)
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        Finally
            SetSpeedStatus("")
            SetProgress(0)
            Editor.UpdateScreen()
            USB_LEDOn() 'in case we where blinking
            EnableControls()
            GUI.OperationStopped()
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
            Dim BaseOffset As UInt32 = 0 'The starting address to read the from data
            Dim UserCount As UInt32 = FlashAvailable 'The total number of bytes to read
            RaiseEvent SetStatus(String.Format(MEM_READ_FROM, FlashName))
            Dim dbox As New DynamicRangeBox
            If Not dbox.ShowRangeBox(BaseOffset, UserCount, FlashAvailable) Then
                RaiseEvent SetStatus(MEM_READ_CANCELED)
                Exit Sub
            End If
            RaiseEvent SetStatus(MEM_READ_START)
            If UserCount = 0 Then Exit Sub
            Dim DefaultName As String = FlashName.Replace(" ", "_") & "_" & Utilities.Pad(Hex((BaseOffset))) & "-" & Utilities.Pad(Hex((BaseOffset + UserCount - 1)))
            Dim TargetIO As IO.FileInfo = Nothing
            If CreateFileForFlashRead(DefaultName, TargetIO) Then
                Dim read_params As New XFER_Operation 'We want to remember the last operation
                read_params.FileName = TargetIO
                read_params.Offset = BaseOffset
                read_params.Size = UserCount
                Dim t As New Threading.Thread(AddressOf ReadMemoryThread)
                t.Start(read_params)
                Editor.Focus()
            Else
                RaiseEvent SetStatus(IO_FILESAVE_CANCELED)
                Exit Sub
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub cmd_write_Click(sender As Object, e As EventArgs) Handles cmd_write.Click
        Try
            Dim BaseOffset As UInt32 = 0 'The starting address to write data to
            Dim fn As IO.FileInfo = Nothing
            If Not OpenFileForWriteWrite(fn) Then Exit Sub
            If Not fn.Exists OrElse fn.Length = 0 Then
                RaiseEvent SetStatus(MEM_ERR1)
                Exit Sub
            End If
            RaiseEvent SetStatus(WR_OPER_START)
            LAST_WRITE_OPERATION = New XFER_Operation
            LAST_WRITE_OPERATION.FileName = fn
            LAST_WRITE_OPERATION.Offset = BaseOffset
            LAST_WRITE_OPERATION.Size = 0
            PerformWriteOperation(LAST_WRITE_OPERATION)
            Editor.Focus()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub cmd_erase_Click(sender As Object, e As EventArgs) Handles cmd_erase.Click
        If MsgBox("This action will permanently delete all data.", MsgBoxStyle.YesNo, "Confirm erase of " & FlashName) = MsgBoxResult.Yes Then
            RaiseEvent WriteConsole("Sent memory erase command to device: " & FlashName)
            RaiseEvent SetStatus("Erasing Flash memory device... (this may take up to 2 minutes)")
            Dim t As New Threading.Thread(AddressOf EraseFlashTd)
            t.Name = "mem.eraseFlash"
            t.Start()
            Application.DoEvents()
            Editor.Focus()
        End If
    End Sub

    Private Sub EraseFlashTd()
        Try
            DisableControls(False) 'You can not cancel this
            GUI.OperationStarted()
            FCUSB_LedBlink()
            RaiseEvent EraseMemory()
            RaiseEvent SetStatus("Erase operation successfully completed")
        Catch ex As Exception
        Finally
            Editor.UpdateScreen()
            USB_LEDOn() 'in case we where blinking
            EnableControls()
        End Try
    End Sub

    Public Sub PerformWriteOperation(ByRef x As XFER_Operation)
        Dim FileIntelHexFormat As Boolean = False
        Using file_st As IO.Stream = x.FileName.OpenRead
            If Utilities.IsIntelHex(file_st) Then FileIntelHexFormat = True
        End Using
        If FileIntelHexFormat Then
            Dim hex_data() As Byte = Utilities.FileIO.ReadBytes(x.FileName.FullName)
            Dim b() As Byte = Utilities.IntelHexToBin(hex_data)
            x.DataStream = New IO.MemoryStream
            x.DataStream.Write(b, 0, b.Length)
            RaiseEvent WriteConsole(String.Format(MEM_OPEN_INTEL, x.FileName.Name, Format(hex_data.Length, "#,###")))
        Else
            x.DataStream = x.FileName.OpenRead
        End If
        Dim BaseAddress As UInt32 = 0 'The starting address to write the data
        If x.Size = 0 Then
            RaiseEvent SetStatus(String.Format(MEM_SEL_RANGE, FlashName))
            Dim dbox As New DynamicRangeBox
            Dim NumberToWrite As UInt32 = x.DataStream.Length 'The total number of bytes to write
            If Not dbox.ShowRangeBox(BaseAddress, NumberToWrite, FlashAvailable) Then
                RaiseEvent SetStatus(WR_OPER_CANCELED)
                Exit Sub
            End If
            If NumberToWrite = 0 Then Exit Sub
            x.Offset = BaseAddress
            x.Size = NumberToWrite
        End If
        RaiseEvent SetStatus(String.Format(MEM_WRITING_FILE, x.FileName.Name, FlashName, Format(x.Size, "#,###")))
        x.DataStream.Position = 0
        Dim t As New Threading.Thread(AddressOf WriteMemoryThread)
        t.Name = "memWriteTd"
        t.Start(x)
        RaiseEvent WriteConsole(String.Format(MEM_OPENED_BIN, x.FileName.Name, Format(x.DataStream.Length, "#,###")))
        RaiseEvent WriteConsole("Starting Flash memory write operation (address: 0x" & Hex(x.Offset).PadLeft(8, "0") & "; " & Format(x.Size, "#,###") & " bytes)")
    End Sub

    Public Sub AbortAnyOperation()
        Try
            If WritingParams IsNot Nothing Then
                WritingParams.AbortOperation = True
            End If
            If ReadingParams IsNot Nothing Then
                ReadingParams.AbortOperation = True
            End If
        Catch ex As Exception
        End Try
    End Sub

End Class
