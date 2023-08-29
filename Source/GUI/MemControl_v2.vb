'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2021 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
'CONTACT EMAIL: support@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK

Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.MemoryInterface

Public Class MemControl_v2
    Private MemDevice As MemoryDeviceInstance
    Private AreaSelected As FlashArea = FlashArea.Main
    Private FlashAvailable As Long 'Changes for some devices
    Private FlashBase As Long 'Offset if this device is not at 0x0
    Private HexLock As New Object 'Used to lock the gui
    Private HexEditorEnabled As Boolean = True 'We can show or not show the hex editor

    Public LAST_WRITE_OPERATION As XFER_Operation = Nothing
    Public IN_OPERATION As Boolean = False
    Public USER_HIT_CANCEL As Boolean = False

    Public Event WriteConsole(msg As String) 'Writes the console/windows console
    Public Event SetStatus(msg As String) 'Sets the text on the status bar

    Public Property SREC_DATAMODE As SREC.RECORD_DATAWIDTH
    Public Property VERIFY_WRITE As Boolean = False
    Public Property ShowHexEditor As Boolean
        Get
            Return HexEditorEnabled
        End Get
        Set(value As Boolean)
            HexEditorEnabled = value
            If HexEditorEnabled Then
                HexEditor64.Visible = True
                txtAddress.Visible = True
                IO_Control.AllowEdit = True
                Me.Height = HexEditor64.Bottom + 18
            Else
                HexEditor64.Visible = False
                txtAddress.Visible = False
                IO_Control.AllowEdit = False
                Me.Height = pbar.Bottom + 18
            End If
        End Set
    End Property

    Public Overrides Property Text As String
        Get
            Return gb_flash.Text
        End Get
        Set(value As String)
            gb_flash.Text = value
        End Set
    End Property

    Public ReadingParams As ReadParameters
    Public WritingParams As WriteParameters

    Public StatusLabels(4) As ToolStripStatusLabel 'This contains our status labels

    Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
    End Sub

    Private Sub MemControl_v2_Load(sender As Object, e As EventArgs) Handles Me.Load
        Try
            StatusBar_Create()
            SetProgress(0)
        Catch ex As Exception
        End Try
    End Sub

#Region "Status Bar"

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

        StatusLabels(3).BorderSides = ToolStripStatusLabelBorderSides.Left
        StatusLabels(3).BorderStyle = Border3DStyle.Etched
        StatusLabels(3).AutoSize = False
        StatusLabels(3).Text = ""
        StatusLabels(3).TextAlign = ContentAlignment.MiddleLeft
        StatusLabels(3).Width = 80 '104

        StatusLabels(4).BorderSides = ToolStripStatusLabelBorderSides.Left
        StatusLabels(4).BorderStyle = Border3DStyle.Etched
        StatusLabels(4).Text = ""
        StatusLabels(4).TextAlign = ContentAlignment.MiddleLeft
        StatusLabels(4).Width = 40
        StatusLabels(4).AutoSize = False
    End Sub

    Public Sub StatusBar_ImgIndex(ind As Integer)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() StatusBar_ImgIndex(ind))
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
                Me.Invoke(Sub() StatusBar_SetTextBaseAddress(mem_addr))
            Else
                StatusLabels(1).Text = "0x" & Hex(mem_addr.ToString).PadLeft(8, "0"c)
                Application.DoEvents()
            End If
        Catch ex As Exception
        End Try
    End Sub

    Public Sub StatusBar_SetTextTask(current_task As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() StatusBar_SetTextTask(current_task))
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

    Public Sub StatusBar_SetTextSpeed(speed_str As String)
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub() StatusBar_SetTextSpeed(speed_str))
            Else
                StatusLabels(3).Text = speed_str
                Application.DoEvents()
            End If
        Catch ex As Exception
        End Try
    End Sub

    Public Sub StatusBar_SetPercent(value As Integer)
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub() StatusBar_SetPercent(value))
            Else
                StatusLabels(4).Text = (value & "%").PadLeft(4, " "c)
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
        Public FileType As FileFilterIndex
    End Class

    'Call this to setup this control
    Public Sub InitMemoryDevice(mem_dev As MemoryDeviceInstance, Optional mem_base As UInt32 = 0)
        Me.MemDevice = mem_dev
        Me.FlashAvailable = MemDevice.Size
        Me.FlashBase = mem_base 'Only used for devices that share the same memory base (i.e JTAG)
        IO_Control.SREC_DATAWIDTH = Me.SREC_DATAMODE
        IO_Control.InitialDirectory = DefaultLocation
        IO_Control.FlashBase = mem_base
        IO_Control.FlashName = mem_dev.MEM_IF.DeviceName
        IO_Control.FlashAvailable = mem_dev.MEM_IF.DeviceSize
        IO_Control.EccStatus(False)
        IO_Control.MemAreaVisible(False)
        Me.Text = mem_dev.MEM_IF.DeviceName
        SetProgress(0)
        HexEditor64.CreateHexViewer(Me.FlashBase, Me.FlashAvailable)
        txtAddress.Text = "0x0"
        Select Case mem_dev.ACCESS
            Case FlashAccess.Read
                IO_Control.EraseButton(False)
                IO_Control.WriteButton(False)
            Case FlashAccess.ReadWriteErase
                IO_Control.EraseButton(True)
                IO_Control.WriteButton(True)
            Case FlashAccess.ReadWriteOnce
                IO_Control.EraseButton(False)
                IO_Control.WriteButton(True)
            Case FlashAccess.ReadWrite
                IO_Control.EraseButton(False)
                IO_Control.WriteButton(True)
        End Select

        AddHandler mem_dev.MEM_IF.SetProgress, AddressOf SetProgress
        AddHandler mem_dev.ControlsDisable, AddressOf DisableControls
        AddHandler mem_dev.ControlsEnable, AddressOf EnableControls
        AddHandler mem_dev.ControlsRefresh, AddressOf RefreshView

        RefreshView()
    End Sub

    Public Sub UninitMemoryDevice()
        Me.MemDevice = Nothing
        IO_Control.MemAreaVisible(False)
        Me.FlashAvailable = 0
        Me.FlashBase = 0
        Me.RefreshView()
    End Sub

    Public Function GetLastDirectory() As String
        Return IO_Control.LastDirectory
    End Function


#Region "IO Control Handlers"

    Private Sub IOControl_WriteConsole(msg As String) Handles IO_Control.WriteConsole
        RaiseEvent WriteConsole(msg)
    End Sub

    Private Sub IOControl_SetStatus(msg As String) Handles IO_Control.SetStatus
        RaiseEvent SetStatus(msg)
    End Sub

    Private Sub IOControl_AbortOperation() Handles IO_Control.AbortOperation
        AbortAnyOperation()
    End Sub

    Private Sub IOControl_ReadOperation(rd As XFER_Operation) Handles IO_Control.ReadOperation
        Dim t As New Threading.Thread(AddressOf ReadMemoryThread)
        t.Start(rd)
        GetFocus()
    End Sub

    Private Sub IOControl_WriteOperation(wr As XFER_Operation) Handles IO_Control.WriteOperation
        PerformWriteOperation(wr)
    End Sub

    Private Sub IOControl_EraseOperation() Handles IO_Control.EraseOperation
        Dim t As New Threading.Thread(AddressOf EraseFlashTd)
        t.Name = "mem.eraseFlash"
        t.Start()
        GetFocus()
    End Sub

    Private Sub IOControl_CompareOperation(vr As CompareParams) Handles IO_Control.CompareOperation
        Dim td As New Threading.Thread(AddressOf CompareFlashTd)
        td.Start(vr)
        GetFocus()
    End Sub

    Private Sub IOControl_EditModeToggle(is_checked As Boolean) Handles IO_Control.EditModeToggle
        If is_checked Then
            HexEditor64.EDIT_MODE = True
        Else
            If HexEditor64.HexEdit_Changes.Count > 0 Then
                If MsgBox("Save changes and write data to Flash?", vbYesNo, "Confirm data write operation") = vbYes Then
                    Dim t As New Threading.Thread(AddressOf WriteChangesMadeInEditMode)
                    t.Name = "tdWriteEditChanges"
                    t.Start()
                End If
            End If
            HexEditor64.EDIT_MODE = False
        End If
    End Sub

#End Region

#Region "Status Bar Hooks"
    Private Delegate Sub cbStatus_UpdateOper(index As Integer, oper As MEM_OPERATION)
    Private Delegate Sub cbStatus_UpdateBase(index As Integer, addr As Long)
    Private Delegate Sub cbStatus_UpdateTask(index As Integer, value As String)
    Private Delegate Sub cbStatus_UpdateSpeed(index As Integer, speed_str As String)
    Private Delegate Sub cbStatus_UpdatePercent(index As Integer, percent As Integer)

    Private Sub Status_UpdateOper(index As Integer, oper As MEM_OPERATION)
        StatusBar_ImgIndex(oper)
    End Sub
    Private Sub Status_UpdateBase(index As Integer, addr As Long)
        StatusBar_SetTextBaseAddress(addr)
    End Sub

    Private Sub Status_UpdateTask(index As Integer, task As String)
        StatusBar_SetTextTask(task)
    End Sub

    Private Sub Status_UpdateSpeed(index As Integer, speed_str As String)
        StatusBar_SetTextSpeed(speed_str)
    End Sub

    Private Sub Status_UpdatePercent(index As Integer, percent As Integer)
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

#Region "Extended Page Support"
    Private Delegate Sub cbAddExtendedArea(page_count As Integer, page_size As UInt16, ext_size As UInt16, pages_per_block As Integer)
    Private Delegate Sub cbSetSelectedArea(area As FlashArea)

    Private Property EXTAREA_PAGECOUNT As Integer  'Total number of pages
    Private Property EXTAREA_BLOCK_PAGES As Integer 'Number of pages in a block/sector
    Private Property EXTAREA_MAIN_PAGE As UInt16 'Number of bytes per page in the main area
    Private Property EXTAREA_EXT_PAGE As UInt16 'Number of bytes per page in the oob area
    Private Property HAS_EXTAREA As Boolean = False 'Indicates we have split memory (main/spare)

    'Sets up the memory control for devices with main/extended areas
    Public Sub SetupExtendedLayout()
        If Me.MemDevice.MEM_IF.GetType() Is GetType(SPINAND_Programmer) Then
            Dim SNAND_IF As SPINAND_Programmer = CType(Me.MemDevice.MEM_IF, SPINAND_Programmer)
            Dim NandDevice As SPI_NAND = SNAND_IF.MyFlashDevice
            Dim pages_per_block As Integer = (NandDevice.Block_Size \ NandDevice.PAGE_SIZE)
            Dim available_pages As Integer = SNAND_IF.BlockManager.MAPPED_PAGES
            Me.FlashAvailable = SNAND_IF.DeviceSize
            Me.AddExtendedArea(available_pages, NandDevice.PAGE_SIZE, NandDevice.PAGE_EXT, pages_per_block)
        ElseIf Me.MemDevice.MEM_IF.GetType() Is GetType(PNAND_Programmer) Then
            Dim PNAND_IF As PNAND_Programmer = CType(Me.MemDevice.MEM_IF, PNAND_Programmer)
            Dim NandDevice As P_NAND = PNAND_IF.MyFlashDevice
            Dim pages_per_block As Integer = (NandDevice.Block_Size \ NandDevice.PAGE_SIZE)
            Dim available_pages As Integer = PNAND_IF.BlockManager.MAPPED_PAGES
            Me.FlashAvailable = PNAND_IF.DeviceSize
            Me.AddExtendedArea(available_pages, NandDevice.PAGE_SIZE, NandDevice.PAGE_EXT, pages_per_block)
        End If
    End Sub
    'This setups the editor to use a Flash with an extended area (such as spare data)
    Private Sub AddExtendedArea(page_count As Integer, page_size As UInt16, ext_size As UInt16, pages_per_block As Integer)
        If Me.InvokeRequired Then
            Dim d As New cbAddExtendedArea(AddressOf AddExtendedArea)
            Me.Invoke(d, {page_count, page_size, ext_size, pages_per_block})
        Else
            Me.EXTAREA_PAGECOUNT = page_count
            Me.EXTAREA_BLOCK_PAGES = pages_per_block
            Me.EXTAREA_MAIN_PAGE = page_size 'i.e. 2048
            Me.EXTAREA_EXT_PAGE = ext_size
            Me.HAS_EXTAREA = True
            IO_Control.MemAreaVisible(True)
            SetSelectedArea(FlashArea.Main)
            Me.RefreshView()
        End If
    End Sub

    Private Sub SetSelectedArea(area As FlashArea)
        If Me.InvokeRequired Then
            Dim d As New cbSetSelectedArea(AddressOf SetSelectedArea)
            Me.Invoke(d, area)
        Else
            Me.AreaSelected = area
            IO_Control.EccStatus(False)
            IO_Control.MemoryArea = area
            If Me.HAS_EXTAREA Then
                If Me.MemDevice.MEM_IF.GetType() Is GetType(PNAND_Programmer) Then
                    Dim PNAND_IF As PNAND_Programmer = CType(Me.MemDevice.MEM_IF, PNAND_Programmer)
                    PNAND_IF.MemoryArea = area
                    Me.FlashAvailable = PNAND_IF.DeviceSize
                ElseIf Me.MemDevice.MEM_IF.GetType() Is GetType(SPINAND_Programmer) Then
                    Dim SNAND_IF As SPINAND_Programmer = CType(Me.MemDevice.MEM_IF, SPINAND_Programmer)
                    SNAND_IF.MemoryArea = area
                    Me.FlashAvailable = SNAND_IF.DeviceSize
                End If
            End If
            IO_Control.FlashAvailable = Me.FlashAvailable
            HexEditor64.CreateHexViewer(Me.FlashBase, Me.FlashAvailable)
            txtAddress.Text = "0x0"
            RefreshView()
        End If
    End Sub

    Private Sub SelectedArea_Changed(new_area As FlashArea) Handles IO_Control.MemoryAreaChanged
        Dim top_addr As Long = HexEditor64.TopAddress
        Dim new_addr As Long
        SetSelectedArea(new_area)
        Select Case new_area
            Case FlashArea.Main
                new_addr = (top_addr \ (Me.EXTAREA_MAIN_PAGE + Me.EXTAREA_EXT_PAGE)) * Me.EXTAREA_MAIN_PAGE
            Case FlashArea.OOB
                new_addr = (top_addr \ Me.EXTAREA_MAIN_PAGE) * Me.EXTAREA_EXT_PAGE
            Case FlashArea.All
                new_addr = (top_addr \ Me.EXTAREA_EXT_PAGE) * (Me.EXTAREA_MAIN_PAGE + Me.EXTAREA_EXT_PAGE)
        End Select
        HexEditor64.GotoAddress(new_addr)
    End Sub

    Private Sub ReadingMem_Status_Start(ByRef Params As ReadParameters)
        Try
            If Params.Status.UpdateOperation IsNot Nothing Then Params.Status.UpdateOperation.DynamicInvoke(1) 'READ IMG
            If Params.Status.UpdateBase IsNot Nothing Then Params.Status.UpdateBase.DynamicInvoke(Params.Address)
            If Params.Status.UpdatePercent IsNot Nothing Then Params.Status.UpdatePercent.DynamicInvoke(CInt(0))
        Catch ex As Exception
        End Try
    End Sub

    Private Sub ReadingMem_Status_Update(total_count As Long, ByRef Params As ReadParameters)
        Try
            Dim Percent As Single = CSng(((total_count - Params.Count) / total_count) * 100) 'Calulate % done
            Dim current_str As String = Format((total_count - Params.Count), "#,###")
            Dim total_str As String = Format(total_count, "#,###")
            If Params.Status.UpdateTask IsNot Nothing Then Params.Status.UpdateTask.DynamicInvoke(String.Format(RM.GetString("mc_reading"), current_str, total_str))
            If Params.Status.UpdatePercent IsNot Nothing Then Params.Status.UpdatePercent.DynamicInvoke(CInt(Percent))
            Dim BytesTransfered As Long = total_count - Params.Count
            If Params.Timer IsNot Nothing AndAlso Params.Status.UpdateSpeed IsNot Nothing Then
                Dim speed_str As String = Format(Math.Round(BytesTransfered / (Params.Timer.ElapsedMilliseconds / 1000)), "#,###") & " Bytes/s"
                Params.Status.UpdateSpeed.DynamicInvoke(speed_str)
            End If
        Catch ex As Exception
        End Try
    End Sub

#End Region

    Private Sub UpdateEccResultImg()
        Dim ECC_Enabled As Boolean = False
        Dim ECC_LastResult As ECC_LIB.ECC_DECODE_RESULT
        If MemDevice.MEM_IF.GetType() Is GetType(PNAND_Programmer) Then
            Dim PNAND_IF As PNAND_Programmer = CType(Me.MemDevice.MEM_IF, PNAND_Programmer)
            If PNAND_IF.ECC_ENG IsNot Nothing Then ECC_Enabled = True : ECC_LastResult = PNAND_IF.ECC_ENG.GetLastResult()
        End If
        If MemDevice.MEM_IF.GetType() Is GetType(SPINAND_Programmer) Then
            Dim SNAND_IF As SPINAND_Programmer = CType(Me.MemDevice.MEM_IF, SPINAND_Programmer)
            If SNAND_IF.ECC_ENG IsNot Nothing Then ECC_Enabled = True : ECC_LastResult = SNAND_IF.ECC_ENG.GetLastResult()
        End If
        If ECC_Enabled AndAlso Me.AreaSelected = FlashMemory.FlashArea.Main Then
            IO_Control.EccStatus(True)
            If ECC_LastResult = ECC_LIB.ECC_DECODE_RESULT.NoErrors Then
                IO_Control.EccImage(My.Resources.ecc_valid)
            Else
                IO_Control.EccImage(My.Resources.ecc_blue)
            End If
        End If
    End Sub

    Public Sub RefreshView()
        Try
            HexEditor64.Width = (Me.Width - 12)
            HexEditor64.Height = (Me.Height - (pbar.Bottom + 18))
            HexEditor64.UpdateScreen()
        Catch ex As Exception
        End Try
    End Sub

    Public Sub SetProgress(Percent As Integer)
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub() SetProgress(Percent))
            Else
                If (Percent > 100) Then Percent = 100
                pbar.Value = Percent
            End If
        Catch ex As Exception
        End Try
    End Sub

    Public Sub EnableControls()
        IO_Control.CancelButton(False)
        IO_Control.ReadButton(True)
        IO_Control.CompareButton(True)
        IO_Control.EditButton(True)
        IO_Control.MemAreaButton(True)
        Select Case Me.MemDevice.ACCESS
            Case FlashAccess.Read
            Case FlashAccess.ReadWriteErase
                IO_Control.WriteButton(True)
                IO_Control.EraseButton(True)
            Case FlashAccess.ReadWriteOnce
                IO_Control.WriteButton(True)
                IO_Control.EraseButton(False)
            Case FlashAccess.ReadWrite
                IO_Control.WriteButton(True)
                IO_Control.EraseButton(False)
        End Select
        HexEditor64.EDIT_MODE = IO_Control.GetEditButtonStatus()
        HexEditor_Enabled(True)
        Me.USER_HIT_CANCEL = False
    End Sub
    'We want to disable read/write/erase controls
    Public Sub DisableControls(show_cancel As Boolean)
        HexEditor64.EDIT_MODE = False
        IO_Control.Buttons(False)
        IO_Control.CancelButton(show_cancel)
        HexEditor_Enabled(False)
        GetFocus()
    End Sub

    Public Sub GetFocus()
        If Me.InvokeRequired Then
            Me.Invoke(Sub() GetFocus())
        Else
            Application.DoEvents()
            HexEditor64.Focus()
        End If
    End Sub

    Private Sub HexEditor_Enabled(is_enabled As Boolean)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() HexEditor_Enabled(is_enabled))
        Else
            txtAddress.Enabled = is_enabled
            HexEditor64.Enabled = is_enabled
        End If
    End Sub

#Region "Address Box"

    Private Sub AddressUpdate(Address As Long) Handles HexEditor64.AddressUpdate
        If txtAddress.InvokeRequired Then
            Me.Invoke(Sub() AddressUpdate(Address))
        Else
            txtAddress.Text = "0x" & Hex(Address).ToUpper
            txtAddress.SelectionStart = txtAddress.Text.Length
        End If
    End Sub

    Private Sub txtAddress_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtAddress.KeyPress
        If Asc(e.KeyChar) = Keys.Enter Then
            HexEditor64.Focus() 'Makes this control loose focus and trigger the other event (lostfocus)
        ElseIf Asc(e.KeyChar) = 97 Then 'a
            e.KeyChar = "A"c
        ElseIf Asc(e.KeyChar) = 98 Then 'b
            e.KeyChar = "B"c
        ElseIf Asc(e.KeyChar) = 99 Then 'c
            e.KeyChar = "C"c
        ElseIf Asc(e.KeyChar) = 100 Then 'd
            e.KeyChar = "D"c
        ElseIf Asc(e.KeyChar) = 101 Then 'e
            e.KeyChar = "E"c
        ElseIf Asc(e.KeyChar) = 102 Then 'f
            e.KeyChar = "F"c
        End If
    End Sub

    Private Sub txtAddress_KeyDown(sender As Object, e As KeyEventArgs) Handles txtAddress.KeyDown

    End Sub

    Private Sub txtAddress_LostFocus(sender As Object, e As EventArgs) Handles txtAddress.LostFocus
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
            Using m As New IO.MemoryStream()
                ReadStream(m, editor_reader)
                data_buffer = m.GetBuffer
                ReDim Preserve data_buffer(data_buffer.Length - 1)
            End Using
            UpdateEccResultImg()
        Finally
            RequestedData = False
        End Try
    End Sub

    Private Sub ReadMemoryThread(o As Object)
        Dim read_params As XFER_Operation = CType(o, XFER_Operation)
        Try
            GUI.OperationStarted(Me) 'This adds the status bar at the bottom
            Me.IN_OPERATION = True
            Me.MemDevice.FCUSB.USB_LEDBlink()
            SetProgress(0)
            DisableControls(True)
            Try
                Try
                    Dim n As New IO.FileInfo(read_params.FileName.FullName)
                    If n.Exists Then n.Delete()
                Catch ex As Exception
                End Try
                RaiseEvent WriteConsole(String.Format(RM.GetString("mc_mem_read_begin"), Me.MemDevice.Name))
                RaiseEvent WriteConsole(String.Format(RM.GetString("mc_mem_start_addr"), read_params.Offset, "0x" & Utilities.Pad(Hex((read_params.Offset))), Format(read_params.Size, "#,###")))
                ReadingParams = New ReadParameters
                ReadingParams.Address = read_params.Offset
                ReadingParams.Count = read_params.Size
                ReadingParams.Timer = New Stopwatch
                ReadingParams.Status.UpdateOperation = New cbStatus_UpdateOper(AddressOf Status_UpdateOper)
                ReadingParams.Status.UpdateBase = New cbStatus_UpdateBase(AddressOf Status_UpdateBase)
                ReadingParams.Status.UpdateTask = New cbStatus_UpdateTask(AddressOf Status_UpdateTask)
                ReadingParams.Status.UpdateSpeed = New cbStatus_UpdateSpeed(AddressOf Status_UpdateSpeed)
                ReadingParams.Status.UpdatePercent = New cbStatus_UpdatePercent(AddressOf Status_UpdatePercent)
                ReadingParams.Timer.Start()
                Dim output_stream As IO.Stream = Nothing
                Select Case read_params.FileType
                    Case FileFilterIndex.Binary
                        output_stream = read_params.FileName.OpenWrite()
                    Case FileFilterIndex.SRecord
                        Dim addr_byte As Integer
                        If (read_params.Size > &HFFFFFF) Then
                            addr_byte = 4
                        ElseIf (read_params.Size > &HFFFF) Then
                            addr_byte = 3
                        Else
                            addr_byte = 2
                        End If
                        output_stream = New SREC.StreamWriter(New IO.StreamWriter(read_params.FileName.Open(IO.FileMode.Create)), addr_byte, Me.SREC_DATAMODE)
                    Case FileFilterIndex.IntelHex
                        output_stream = New IHEX.StreamWriter(New IO.StreamWriter(read_params.FileName.Open(IO.FileMode.Create)))
                End Select
                ReadStream(output_stream, ReadingParams)
                output_stream.Close()
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
                    RaiseEvent SetStatus(String.Format(RM.GetString("mc_mem_write_success"), read_params.FileName.Name))
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        Finally
            If read_params.DataStream IsNot Nothing Then read_params.DataStream.Dispose()
            Me.MemDevice.FCUSB.USB_LEDOn()
            EnableControls()
            SetProgress(0)
            GetFocus()
            Me.IN_OPERATION = False
            GUI.OperationStopped(Me)
            ReadingParams = Nothing
        End Try
    End Sub

    Private Sub WriteMemoryThread(o As Object)
        Dim file_out As XFER_Operation = CType(o, XFER_Operation)
        Try
            GUI.OperationStarted(Me)
            Me.IN_OPERATION = True
            Me.MemDevice.FCUSB.USB_LEDBlink()
            SetProgress(0)
            DisableControls(True)
            Try
                WritingParams = New WriteParameters
                WritingParams.Address = file_out.Offset
                WritingParams.BytesLeft = file_out.Size
                WritingParams.BytesTotal = file_out.Size
                WritingParams.Verify = Me.VERIFY_WRITE
                WritingParams.EraseSector = True
                WritingParams.Status.UpdateOperation = New cbStatus_UpdateOper(AddressOf Status_UpdateOper)
                WritingParams.Status.UpdateBase = New cbStatus_UpdateBase(AddressOf Status_UpdateBase)
                WritingParams.Status.UpdateTask = New cbStatus_UpdateTask(AddressOf Status_UpdateTask)
                WritingParams.Status.UpdateSpeed = New cbStatus_UpdateSpeed(AddressOf Status_UpdateSpeed)
                WritingParams.Status.UpdatePercent = New cbStatus_UpdatePercent(AddressOf Status_UpdatePercent)
                'Reset current labels
                Status_UpdateOper(0, MEM_OPERATION.NoOp)
                Status_UpdateBase(0, file_out.Offset)
                Status_UpdateTask(0, "")
                Status_UpdateSpeed(0, "")
                Status_UpdatePercent(0, 0)
                Dim write_success As Boolean = Me.MemDevice.WriteStream(file_out.DataStream, WritingParams)
                If write_success Then Me.MemDevice.WaitUntilReady()
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
                    RaiseEvent WriteConsole(String.Format(RM.GetString("mc_wr_oper_result"), Format(file_out.Size, "#,###"), (WritingParams.Timer.ElapsedMilliseconds / 1000), Speed))
                    Me.MemDevice.WriteSuccessful(Me.LAST_WRITE_OPERATION)
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        Finally
            SetProgress(0)
            HexEditor64.UpdateScreen()
            Me.MemDevice.FCUSB.USB_LEDOn()
            EnableControls()
            Me.IN_OPERATION = False
            GUI.OperationStopped(Me)
            WritingParams = Nothing
        End Try
    End Sub

    Private Sub CompareFlashTd(o As Object)
        Dim param As CompareParams = CType(o, CompareParams)
        Dim TotalMismatches As UInt32 = 0
        Dim CompareCount As Long = param.Count
        Dim StartingAddress As Long = param.BaseOffset
        Dim ErrorList As New List(Of CompareDifference)
        Dim local_stream As New IO.BinaryReader(param.CompareData)
        Try
            GUI.OperationStarted(Me) 'This adds the status bar at the bottom
            Me.IN_OPERATION = True
            Me.MemDevice.FCUSB.USB_LEDBlink()
            SetProgress(0)
            DisableControls(True)
            Status_UpdateOper(0, MEM_OPERATION.VerifyData)
            RaiseEvent WriteConsole(RM.GetString("mc_compare_start"))
            RaiseEvent WriteConsole(RM.GetString("mc_compare_filename") & ": " & param.local_file.Name)
            RaiseEvent WriteConsole(String.Format(RM.GetString("mc_compare_info"), Hex(param.BaseOffset).PadLeft(8, "0"c), Format(param.Count, "#,###")))
            Dim BytesTransfered As UInt32 = 0
            Dim ReadTimer As New Stopwatch
            Dim sector_count As Integer = 0
            Dim sector_ind As Integer
            sector_count = Me.MemDevice.GetSectorCount()
            Do While (param.Count > 0)
                Dim packet_size As Integer
                If sector_count = 1 Then 'single sector
                    packet_size = 65536 '64KB section
                Else
                    packet_size = Me.MemDevice.GetSectorSize(sector_ind)
                    If packet_size = 0 Then packet_size = CInt(Me.MemDevice.Size)
                End If
                packet_size = Math.Min(packet_size, CInt(param.Count))
                Dim file_data() As Byte = local_stream.ReadBytes(packet_size)
                ReadTimer.Start()
                Dim flash_addr As Long = param.BaseOffset
                Dim buffer() As Byte = CompareFlash_Read(flash_addr, packet_size)
                ReadTimer.Stop()
                If buffer Is Nothing Then Exit Do 'Abort occured
                If MAIN_FCUSB Is Nothing Then Exit Do
                param.Count -= CUInt(buffer.Length)
                param.BaseOffset += buffer.Length
                BytesTransfered += CUInt(buffer.Length)
                Dim speed_str As String = UpdateSpeed_GetText(CInt(Math.Round(BytesTransfered / (ReadTimer.ElapsedMilliseconds / 1000))))
                Status_UpdateSpeed(0, speed_str)
                Dim percent_done As Single = CSng((BytesTransfered / CompareCount) * 100)
                Status_UpdatePercent(0, CInt(percent_done))
                Dim vee As New CompareDifference
                For x As UInt32 = 0 To CUInt(buffer.Length - 1)
                    If (Not buffer(CInt(x)) = file_data(CInt(x))) Then
                        vee.MISMATCH += 1UI
                        TotalMismatches += 1UI
                        If vee.MISMATCH = 1 Then
                            vee.BASR_ADR = flash_addr
                            vee.FIRST_OFFSET = x
                            vee.BYTE_FILE = file_data(CInt(x))
                            vee.BYTE_FLASH = buffer(CInt(x))
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
            Me.MemDevice.FCUSB.USB_LEDOn()
            EnableControls()
            SetProgress(0)
            GetFocus()
            Me.IN_OPERATION = False
            GUI.OperationStopped(Me)
            Status_UpdateOper(0, MEM_OPERATION.NoOp)
        End Try
        Try
            If (param.Count = 0) Then 'We compared all data, lets show the user!
                Dim percent_success As Single = ((CSng(CompareCount - TotalMismatches) / CSng(CompareCount)) * 100)
                Dim percent_formatted As String = percent_success.ToString
                If (Not percent_formatted = "100") AndAlso percent_formatted.IndexOf(".") > 0 Then
                    percent_formatted = percent_formatted.Substring(0, percent_formatted.IndexOf(".") + 2)
                End If
                RaiseEvent WriteConsole(String.Format(RM.GetString("mc_compare_complete_tot"), TotalMismatches, percent_formatted))
                Dim filename As String = param.local_file.Name
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
                CompareResultForm.Controls.Add(New Label With {.Width = CompareResultForm.Width + 20, .Height = 18, .Text = s1 & ": 0x" & Hex(StartingAddress).PadLeft(8, "0"c) & " - 0x" & Hex(StartingAddress + CompareCount - 1).PadLeft(8, "0"c), .Location = New Point(10, 24)})
                CompareResultForm.Controls.Add(New Label With {.Width = CompareResultForm.Width + 20, .Height = 18, .Text = s2 & ": " & Format(CompareCount, "#,###"), .Location = New Point(10, 44)})
                CompareResultForm.Controls.Add(New Label With {.Width = CompareResultForm.Width + 20, .Height = 18, .Text = match_str, .Location = New Point(10, 64)})
                Dim cmbClose As New Button With {.Text = RM.GetString("mc_button_close"), .Width = 80, .Location = New Point(CompareResultForm.Width \ 2 - 50, 92)}
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

    Public Class CompareParams
        Public file_type As FileFilterIndex
        Public BaseOffset As Long = 0
        Public Count As Long = 0
        Public local_file As IO.FileInfo 'The physical file
        Public CompareData As IO.Stream
    End Class

    Public Class CompareDifference
        Public SECTOR As UInt32 'Index of the sector
        Public BASR_ADR As Long = 0 'Start of the sector
        Public MISMATCH As UInt32 = 0 'Number of bytes that don't match in this sector
        Public FIRST_OFFSET As UInt32 ' The first offset where the data does not match
        Public BYTE_FILE As Byte 'This is the byte in the file
        Public BYTE_FLASH As Byte 'This is the byte in the flash
    End Class

    Public Enum FileFilterIndex
        Binary
        IntelHex
        SRecord
    End Enum

    Private Function CompareFlash_Read(base As Long, count As Integer) As Byte()
        Try
            ReadingParams = New ReadParameters
            ReadingParams.Address = base
            ReadingParams.Count = count
            ReadingParams.Timer = New Stopwatch
            ReadingParams.Status.UpdateBase = New cbStatus_UpdateBase(AddressOf Status_UpdateBase)
            ReadingParams.Status.UpdateTask = New cbStatus_UpdateTask(AddressOf Status_UpdateTask)
            Using data_stream As New IO.MemoryStream
                ReadStream(data_stream, ReadingParams)
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

    Private Shared Function UpdateSpeed_GetText(bytes_per_second As Integer) As String
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

    Private Sub EraseFlashTd()
        Try
            DisableControls(False) 'You can not cancel this
            GUI.OperationStarted(Me)
            Me.IN_OPERATION = True
            Me.MemDevice.FCUSB.USB_LEDBlink()
            Status_UpdateOper(0, MEM_OPERATION.EraseSector)
            Status_UpdateBase(0, 0)
            Status_UpdatePercent(0, 0)
            Status_UpdateSpeed(0, "")
            Status_UpdateTask(0, RM.GetString("mem_erase_device"))
            Me.MemDevice.EraseFlash()
            Me.MemDevice.WaitUntilReady()
            RaiseEvent SetStatus(RM.GetString("mem_erase_device_success"))
        Catch ex As Exception
        Finally
            Me.IN_OPERATION = False
            GUI.OperationStopped(Me)
            HexEditor64.UpdateScreen()
            Me.MemDevice.FCUSB.USB_LEDOn()
            EnableControls()
        End Try
    End Sub

    Public Sub PerformWriteOperation(ByRef x As XFER_Operation)
        LAST_WRITE_OPERATION = x
        Select Case x.FileType
            Case FileFilterIndex.Binary
                x.DataStream = x.FileName.OpenRead()
            Case FileFilterIndex.IntelHex
                x.DataStream = New IHEX.StreamReader(x.FileName.OpenText)
            Case FileFilterIndex.SRecord
                x.DataStream = New SREC.StreamReader(x.FileName.OpenText)
        End Select
        RaiseEvent SetStatus(String.Format(RM.GetString("mc_wr_oper_status"), x.FileName.Name, Me.MemDevice.Name, Format(x.Size, "#,###")))
        RaiseEvent WriteConsole(String.Format(RM.GetString("mc_io_open_file"), x.FileName.Name, Format(x.DataStream.Length, "#,###")))
        RaiseEvent WriteConsole(String.Format(RM.GetString("mc_io_destination"), Hex(x.Offset).PadLeft(8, "0"c), Format(x.Size, "#,###")))
        x.DataStream.Position = 0
        Dim t As New Threading.Thread(AddressOf WriteMemoryThread)
        t.Name = "memWriteTd"
        t.Start(x)
        GetFocus()
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
    'Number of bytes shown on the left hand side of the control
    Public Function GetHexAddrSize() As Integer
        Return HexEditor64.HexDataByteSize
    End Function

#Region "Edit Mode"

    Private Sub WriteChangesMadeInEditMode()
        Try
            DisableControls(False) 'You can not cancel this
            Me.IN_OPERATION = True
            Me.MemDevice.FCUSB.USB_LEDBlink()
            em_sector_list = New List(Of editmode_sector)
            RaiseEvent SetStatus("Programming changes to Flash device")
            For Each change In HexEditor64.HexEdit_Changes
                editmode_addchange(change.address, change.new_byte)
            Next
            For Each sector In em_sector_list
                Me.MemDevice.WriteBytes(sector.sector_addr, sector.sector_data, False)
            Next
        Catch ex As Exception
        Finally
            RaiseEvent SetStatus("Flash program operation has completed")
            Me.MemDevice.FCUSB.USB_LEDOn()
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
        Public sector_size As Integer
        Public sector_data() As Byte
    End Class

    Private Function editmode_getitem(sector_int As Integer) As editmode_sector
        For Each item As editmode_sector In em_sector_list
            If item.sector_index = sector_int Then Return item
        Next
        Dim new_item As New editmode_sector
        new_item.sector_index = sector_int
        new_item.sector_addr = Me.MemDevice.GetSectorAddress(sector_int)
        new_item.sector_size = Me.MemDevice.GetSectorSize(sector_int)
        If new_item.sector_size = 0 Then new_item.sector_size = CInt(Me.MemDevice.Size)
        new_item.sector_data = ReadMemory(new_item.sector_addr, new_item.sector_size)
        em_sector_list.Add(new_item)
        Return new_item
    End Function

    Private Sub editmode_addchange(addr As Long, dt As Byte)
        Dim base_addr As Long = 0 'base of the sector address
        Dim sector_index As Integer = Me.MemDevice.GetSectorIndex(addr)
        base_addr = Me.MemDevice.GetSectorAddress(sector_index)
        Dim item As editmode_sector = editmode_getitem(sector_index)
        item.sector_data(CInt(addr - base_addr)) = dt
    End Sub


#End Region

    Private Function ReadMemory(base_addr As Long, count As Integer) As Byte()
        If Me.MemDevice.IsBulkErasing Then
            Dim blank_data(count - 1) As Byte
            Utilities.FillByteArray(blank_data, 255)
            Return blank_data
        Else
            Return Me.MemDevice.ReadBytes(base_addr, count)
        End If
    End Function

    Private Sub ReadStream(data_stream As IO.Stream, f_params As ReadParameters)
        If Me.MemDevice.IsBulkErasing Then
            Dim blank_data(CInt(f_params.Count - 1)) As Byte
            Utilities.FillByteArray(blank_data, 255)
            data_stream.Write(blank_data, 0, blank_data.Length)
        Else
            Me.MemDevice.ReadStream(data_stream, f_params)
        End If
    End Sub

End Class