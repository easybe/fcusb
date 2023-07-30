'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2016 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK

Imports FlashcatUSB.FlashMemory

Public Class MemControl_v2
    Private AreaSelected As FlashArea = FlashArea.All
    Private FlashName As String 'Contains the MFG and PART NUMBER
    Private FlashSize As Long 'The size of the flash (not including spare area)
    Private FlashAvailable As Long 'The total bytes available for the hex editor
    Private HexLock As New Object 'Used to lock the gui

    Private Operation_Aborted As Boolean = False
    Public LAST_WRITE_OPERATION As XFER_Operation = Nothing

    Public Event ReadMemory(ByVal base_addr As Long, ByRef data() As Byte, ByVal area As FlashArea) 'We want to get data from the normal memory area
    Public Event ReadStream(ByVal data_stream As IO.Stream, ByVal f_params As ReadParameters)
    Public Event WriteMemory(ByVal base_addr As Long, ByVal data() As Byte, ByVal area As FlashArea, ByRef Successful As Boolean) 'Write data to the normal area
    Public Event WriteStream(ByVal data_stream As IO.Stream, ByVal f_params As WriteParameters, ByRef Successful As Boolean)

    Public Event EraseMemory()
    Public Event EraseNandBlock(ByVal base_addr As Long)

    Public Event StopOperation() 'When user clicks the "STOP" button
    Public Event SuccessfulWrite(ByVal x As XFER_Operation)

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
        Public Offset As Long
        Public Size As Long
    End Class

    'Call this to setup this control
    Public Sub InitMemoryDevice(ByVal Name As String, ByVal flash_size As Long, ByVal access As access_mode)
        FlashName = Name
        FlashSize = flash_size
        FlashAvailable = flash_size 'Main area first
        cmd_area.Visible = False 'Only enable this on device that have more than one internal area to view
        gb_flash.Text = Name
        SetProgress(0)
        SetSpeedStatus("")
        memory_map = Nothing
        SetSelectedArea(FlashArea.Main)
    End Sub

    Public Enum access_mode
        [ReadOnly] 'We can read but can not write
        [ReadWrite]
    End Enum

    Friend Class DynamicRangeBox
        Private BaseTxt As New Windows.Forms.TextBox
        Private LenTxt As New Windows.Forms.TextBox
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
            InputSelectionForm.FormBorderStyle = Windows.Forms.FormBorderStyle.FixedToolWindow
            InputSelectionForm.ShowInTaskbar = False
            InputSelectionForm.ShowIcon = False
            InputSelectionForm.ControlBox = False
            Dim BtnOK As New Windows.Forms.Button With {.Text = RM.GetString("fcusb_okbutton"), .Width = 60, .Height = 20, .Left = 90, .Top = 50}
            Dim BtnCAN As New Windows.Forms.Button With {.Text = RM.GetString("fcusb_cancel"), .Width = 60, .Height = 20, .Left = 20, .Top = 50}
            Dim Lbl1 As New Windows.Forms.Label With {.Text = RM.GetString("fcusb_baseaddress"), .Left = 10, .Top = 5}
            Dim Lbl2 As New Windows.Forms.Label With {.Text = RM.GetString("fcusb_length"), .Left = 105, .Top = 5}
            BaseTxt = New Windows.Forms.TextBox With {.Text = "0x" & Hex(BaseAddress), .Width = 70, .Top = 20, .Left = 10}
            LenTxt = New Windows.Forms.TextBox With {.Text = Size.ToString, .Width = 70, .Top = 20, .Left = 90}
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
                ElseIf Utilities.IsDataType.HexString(t.Text) AndAlso t.Text.Length < 9 Then
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
            frm.Top = CInt(GuiForm.Top + ((GuiForm.Height / 2) - (frm.Height / 2)))
            frm.Left = CInt(GuiForm.Left + ((GuiForm.Width / 2) - (frm.Width / 2)))
        End Sub
        'Handles the dynamic form for a click
        Private Sub Dyn_OkClick(ByVal sender As System.Object, ByVal e As System.EventArgs)
            Dim Btn As Windows.Forms.Button = CType(sender, Button)
            Dim SendFrm As Form = Btn.FindForm
            SendFrm.DialogResult = Windows.Forms.DialogResult.OK
        End Sub

        Private Sub Dyn_CancelClick(ByVal sender As System.Object, ByVal e As System.EventArgs)
            Dim Btn As Windows.Forms.Button = CType(sender, Button)
            Dim SendFrm As Form = Btn.FindForm
            SendFrm.DialogResult = Windows.Forms.DialogResult.Cancel
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
    Private Delegate Sub cbAddressUpdate(ByVal Address As Long)
    Private Delegate Sub cbControls()
    Private Delegate Sub cbDisableControls(ByVal show_cancel As Boolean)
#End Region

#Region "Memory Map (Multiple memory areas)"
    Private memory_map() As memory_map_item
    Private PageCount As UInt32 'Total number of pages in this flash device
    Private PagesPerBlock As UInt32 'Number of pages that make up a single block
    Private PageSize As UInt16 'Bytes per page
    Private PageExtened As UInt16 'Number of additional bytes

    Private Structure memory_map_item
        Public addr_start As Long 'This is the combined location of the start address
        Public addr_end As Long
        Public area_start As Long 'This is the area by itself
        Public area_end As Long
        Public block_address As Long 'the address of the block
        Public area As FlashArea
    End Structure

    Private Sub SetSelectedArea(ByVal area As FlashArea)
        AreaSelected = area
        Select Case area
            Case FlashArea.Main
                cmd_area.Text = "Main"
                FlashAvailable = FlashSize
            Case FlashArea.OOB
                cmd_area.Text = "Spare"
                FlashAvailable = PageCount * PageExtened
            Case FlashArea.All
                cmd_area.Text = "All Data"
                FlashAvailable = FlashSize + (PageCount * PageExtened)
        End Select
        'BaseAddress = 0 'Reset to the top
        Editor.CreateHexViewer(0, FlashAvailable)
        txtAddress.Text = "0x0"
        RefreshView()
    End Sub
    'This setups the editor to use a Flash with an extended area (such as spare data)
    Public Sub AddExtendedArea(ByVal page_count As UInt32, ByVal page_size As UInt16, ByVal ext_size As UInt16, ByVal pages_per_block As UInt32)
        cmd_area.Visible = True
        PageCount = page_count
        PagesPerBlock = pages_per_block
        PageSize = page_size 'i.e. 2048
        PageExtened = ext_size
        Dim addr_x As Long = 0
        Dim main_x As Long = 0
        Dim ext_x As Long = 0
        Dim block_address As Long = 0
        ReDim memory_map((PageCount * 2) - 1)
        For i = 0 To PageCount - 1
            If i Mod PagesPerBlock = 0 Then
                block_address = main_x
            End If
            Dim main As New memory_map_item
            main.block_address = block_address
            main.area = FlashArea.Main
            main.addr_start = addr_x
            main.addr_end = addr_x + page_size - 1
            main.area_start = main_x
            main.area_end = main_x + page_size - 1
            main_x += page_size
            addr_x += page_size
            Dim ext As New memory_map_item
            ext.block_address = block_address
            ext.area = FlashArea.OOB
            ext.addr_start = addr_x
            ext.addr_end = addr_x + ext_size - 1
            ext.area_start = ext_x
            ext.area_end = ext_x + ext_size - 1
            ext_x += ext_size
            addr_x += ext_size
            memory_map(i * 2) = main
            memory_map((i * 2) + 1) = ext
        Next
        'Possibly set something here
    End Sub

    Private Function GetMemoryMapIndex(ByVal hybrid_addr As Long) As Integer
        For i = 0 To memory_map.Length - 1
            If hybrid_addr >= memory_map(i).addr_start AndAlso hybrid_addr <= memory_map(i).addr_end Then
                Return i
            End If
        Next
        Return -1 'Means not found
    End Function

    Private Function GetMemoryFromMap(ByVal map_ind As Integer, ByVal hybrid_addr As Long, ByVal count As UInt32) As Byte()
        Dim m As memory_map_item = memory_map(map_ind)
        Dim offset As Long = hybrid_addr - m.addr_start 'Offset = number of bytes into this map
        Dim area_size As Long = (m.area_end - m.area_start) + 1
        Dim BytesAvailable As Integer = area_size - offset
        If count > BytesAvailable Then count = BytesAvailable
        Dim data_out(count - 1) As Byte
        If (m.area = FlashArea.Main) Then
            RaiseEvent ReadMemory(m.area_start + offset, data_out, FlashArea.Main)
        Else 'spare area
            RaiseEvent ReadMemory(m.area_start + offset, data_out, FlashArea.OOB)
        End If
        Return data_out
    End Function

    Private Function SetMemoryFromMap(ByVal map_ind As Integer, ByVal logical_addr As Long, ByVal count As UInt32, ByRef data_stream As IO.Stream) As UInt32
        Dim m As memory_map_item = memory_map(map_ind)
        Dim offset As Long = logical_addr - m.addr_start 'Offset = number of bytes into this map
        Dim area_size As Long = (m.area_end - m.area_start) + 1
        Dim BytesAvailable As Integer = area_size - offset
        If count > BytesAvailable Then count = BytesAvailable
        Dim f_params As New WriteParameters
        f_params.Address = offset
        f_params.Count = count
        f_params.UpdateProgress = Nothing 'We do not want updates
        f_params.UpdateSpeed = Nothing
        f_params.Memory_Area = m.area
        f_params.Verify = False
        f_params.EraseSector = True
        RaiseEvent WriteStream(data_stream, f_params, Nothing)
        Return count 'This is the number of bytes we wrote to the memory
    End Function

    Private Sub MemoryMap_ReadFromAllData(ByVal address As Long, ByVal count As Long, ByRef data_stream As IO.Stream)
        Dim BytesLeft As Long = count
        Dim map_ind As Integer = GetMemoryMapIndex(address)
        Do Until BytesLeft = 0
            Dim b() As Byte = GetMemoryFromMap(map_ind, address, BytesLeft)
            map_ind += 1 'Next time we need to look at the next base
            BytesLeft -= b.Length
            address += b.Length
            data_stream.Write(b, 0, b.Length)
        Loop
    End Sub

    Private Sub MemoryMap_ReadStream(ByVal base_addr As Long, ByVal count As Long, ByVal data_stream As IO.Stream)
        Dim Clock As New Stopwatch
        Clock.Start()
        Dim BlockSize As Integer = FCUSB_GetPreferredBlockSize()
        Dim Loops As Integer = CInt(Math.Ceiling(count / BlockSize)) 'Calcuates iterations
        Dim BytesLeft As Long = count
        For i = 1 To Loops
            If Operation_Aborted Then Exit For
            Dim packet_size As Long = BytesLeft
            If packet_size > BlockSize Then packet_size = BlockSize
            BytesLeft = BytesLeft - BlockSize
            MemoryMap_ReadFromAllData(base_addr, packet_size, data_stream)
            If i Mod 4 = 0 Then
                Dim Percent As Single = CSng((i / Loops) * 100) 'Calulate % done
                SetProgress(CInt(Percent))
                Dim StatusLoc As String = Format((count - BytesLeft), "#,###") & " of " & Format(count, "#,###") & " Bytes " 'Format Status
                Dim StatusPrec As String = "(" & Math.Round(Percent, 0) & "%) " 'Format Status
                SetStatus("Reading Flash " & StatusLoc & StatusPrec) '& StatusSpeeed 'Print Status
                Dim StatusSpeeed As String = Format(Math.Round((count - BytesLeft) / (Clock.ElapsedMilliseconds / 1000)), "#,###") & " Bytes/s"  'Format Status
                SetSpeedStatus(StatusSpeeed)
                data_stream.Flush()
            End If
            base_addr += packet_size
        Next
        Clock.Stop()
    End Sub

    Private Sub MemoryMap_WriteStream(ByVal base_addr As Long, ByVal count As Long, ByVal data_stream As IO.Stream, ByRef Successful As Boolean)
        Dim Clock As New Stopwatch
        Clock.Start()
        Try
            Dim BlockSize As UInt32 = (PagesPerBlock * PageSize) 'This is the size of the block, without spare
            Dim BlockSizeSpare As UInt32 = (PagesPerBlock * PageExtened)  'This is the size of the spare area within the block
            Dim BlockSizeTotal As UInt32 = (PagesPerBlock * (PageSize + PageExtened)) 'This should be the total size of the block
            Dim logical_addr As Long = base_addr
            Dim BytesLeft As Long = count
            Do Until BytesLeft = 0
                If Operation_Aborted Then Exit Do
                Dim new_block(BlockSizeTotal - 1) As Byte
                Dim map_ind As Integer = GetMemoryMapIndex(logical_addr)
                If map_ind = -1 Then
                    WriteConsole("Opperation completed as Flash device has reached the last block")
                    WriteConsole("Number of bytes left: " & BytesLeft.ToString)
                    Exit Do
                End If
                Dim m As memory_map_item = memory_map(map_ind)
                Dim offset As UInt32 = (logical_addr - m.addr_start)
                If (offset > 0) Then
                    logical_addr = m.addr_start
                    Using memreader As New IO.MemoryStream
                        MemoryMap_ReadFromAllData(logical_addr, offset, memreader)
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
                        MemoryMap_ReadFromAllData(logical_addr + offset, Difference, memreader)
                        memreader.Position = 0
                        memreader.Read(new_block, 0, offset)
                    End Using
                Else 'MATCH
                    data_stream.Read(new_block, offset, BytesToRead)
                End If
                Dim BlockData(BlockSize - 1) As Byte
                Dim SpareData(BlockSizeSpare - 1) As Byte
                Dim MainOffset As Integer = 0
                Dim SpareOffset As Integer = 0
                Using memreader As New IO.MemoryStream(new_block)
                    For i = 0 To PagesPerBlock - 1
                        memreader.Read(BlockData, MainOffset, PageSize)
                        memreader.Read(SpareData, SpareOffset, PageExtened)
                        MainOffset += PageSize
                        SpareOffset += PageExtened
                    Next
                End Using
                RaiseEvent EraseNandBlock(m.block_address)
                Using block_stream As New IO.MemoryStream(BlockData)
                    Dim f_params As New WriteParameters
                    f_params.Address = m.block_address
                    f_params.Count = BlockSize
                    f_params.DisplayStatus = False 'We will not display
                    f_params.Memory_Area = 0 'Main Area
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
                    f_params.Memory_Area = 1 'Spare Area
                    f_params.Verify = False
                    f_params.EraseSector = False 'We have already previously erased this block
                    RaiseEvent WriteStream(spare_stream, f_params, Successful)
                End Using
                If (Not Operation_Aborted) AndAlso VerifyData Then 'If successful
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
                Dim Percent As Single = CSng(((count - BytesLeft) / count) * 100) 'Calulate % done
                SetProgress(CInt(Percent))
                Dim StatusLoc As String = Format((count - BytesLeft), "#,###") & " of " & Format(count, "#,###") & " Bytes " 'Format Status
                Dim StatusPrec As String = "(" & Math.Round(Percent, 0) & "%) " 'Format Status
                Dim StatusSpeeed As String = Format(Math.Round((count - BytesLeft) / (Clock.ElapsedMilliseconds / 1000)), "#,###") & " Bytes/s"  'Format Status
                SetStatus("Writing Flash " & StatusLoc & StatusPrec) '& StatusSpeeed 'Print Status
                SetSpeedStatus(StatusSpeeed)
            Loop
        Catch ex As Exception
        Finally
            Clock.Stop()
        End Try
    End Sub

    Private Function VerifyWrittenBlock(ByVal hybrid_addr As Long, ByVal BlockData() As Byte) As Boolean
        SetStatus(String.Format(RM.GetString("fcusb_memif_verify"), "0x" & Hex(hybrid_addr)))
        Dim FailedAttempts As Integer = 0
        Dim ReadResult As Boolean = False
        Do
            Dim DataVerify() As Byte
            Using memRead As New IO.MemoryStream
                MemoryMap_ReadFromAllData(hybrid_addr, BlockData.Length, memRead)
                DataVerify = memRead.GetBuffer()
                ReDim Preserve DataVerify(memRead.Length - 1)
            End Using
            Dim Byte_Correct As Byte
            Dim Byte_Incorrect As Byte
            Dim MiscountCounter As Integer = 0
            Dim Address_Error As Long = 0
            For i = 0 To BlockData.Length - 1
                If (Not BlockData(i) = DataVerify(i)) Then
                    If MiscountCounter = 0 Then Address_Error = hybrid_addr + i : Byte_Incorrect = BlockData(i) : Byte_Correct = DataVerify(i)
                    MiscountCounter += 1
                End If
            Next
            If MiscountCounter = 0 Then ReadResult = True 'Verification successful
            If ReadResult Then
                SetStatus(RM.GetString("fcusb_memif_verifyokay"))
                Application.DoEvents()
                Utilities.Sleep(500)
            Else
                WriteConsole(String.Format(RM.GetString("fcusb_memif_verifyerr2"), "0x" & Hex(Address_Error), "0x" & Hex(Byte_Correct), "0x" & Hex(Byte_Incorrect), MiscountCounter))
                FailedAttempts += 1
                If FailedAttempts = 1 Then 'Make this changeable in the future
                    Dim map_ind As Integer = GetMemoryMapIndex(hybrid_addr)
                    Dim m As memory_map_item = memory_map(map_ind)
                    Dim offset As Long = hybrid_addr - m.addr_start 'Offset = number of bytes into this map
                    Dim logical_addr As Long = m.area_start + offset
                    WriteConsole("BAD NAND BLOCK AT address: 0x" & Hex(hybrid_addr) & " (Mapped to 0x" & Hex(logical_addr) & " )")
                    If NAND_Manager Then EXT_IF.NAND_MarkBadBlock(logical_addr, 0) 'This can only be a mix
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
            Case FlashArea.Main
                SetSelectedArea(FlashArea.OOB)
            Case FlashArea.OOB
                SetSelectedArea(FlashArea.All)
            Case FlashArea.All
                SetSelectedArea(FlashArea.Main)
        End Select
    End Sub

    Public Sub RefreshView()
        Editor.UpdateScreen()
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

    Public Sub EnableControls()
        If Me.InvokeRequired Then
            Dim d As New cbControls(AddressOf EnableControls)
            Me.Invoke(d)
        Else
            cmd_read.Enabled = True
            cmd_write.Enabled = True
            cmd_erase.Enabled = True

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

    Private Sub AddressUpdate(ByVal Address As Long) Handles Editor.AddressUpdate
        If txtAddress.InvokeRequired Then
            Dim d As New cbAddressUpdate(AddressOf AddressUpdate)
            Me.Invoke(d, New Object() {Address})
        Else
            txtAddress.Text = "0x" & UCase(Hex(Address))
            txtAddress.SelectionStart = txtAddress.Text.Length
        End If
    End Sub

    Private Sub txtAddress_Enter(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtAddress.Enter
        'txtAddress.Clear()
    End Sub

    Private Sub txtAddress_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtAddress.KeyPress
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
                Dim l As Long = CLng(input)
                Editor.GotoAddress(l)
            ElseIf Utilities.IsDataType.HexString(txtAddress.Text) Then
                Dim l As Long = Utilities.HexToLng(input)
                Editor.GotoAddress(l)
            Else
                txtAddress.Text = "0x" & UCase(Hex(Editor.BaseAddress))
            End If
        Catch ex As Exception
            txtAddress.Text = "0x" & UCase(Hex(Editor.BaseAddress))
        End Try
    End Sub

#End Region

    'Our hex viewer is asking for data to display
    Private Sub DataRequest(ByVal address As Long, ByRef data() As Byte) Handles Editor.RequestData
        Select Case AreaSelected
            Case FlashArea.Main 'We only want to see main data
                RaiseEvent ReadMemory(address, data, FlashArea.Main) 'DONT DISPLAY PROGRESS
            Case FlashArea.OOB  'We only want to see spare data
                RaiseEvent ReadMemory(address, data, FlashArea.OOB)'DONT DISPLAY PROGRESS
            Case FlashArea.All  'We want to display a mix of main data and spare data
                Using m As New IO.MemoryStream()
                    MemoryMap_ReadFromAllData(address, data.Length, m)
                    data = m.GetBuffer()
                    ReDim Preserve data(m.Length - 1)
                End Using
        End Select
    End Sub

    Private Function CreateFileForFlashRead(ByVal DefaultName As String, ByRef file As IO.FileInfo) As Boolean
        Try
            Dim Saveme As New SaveFileDialog
            Saveme.AddExtension = True
            Saveme.InitialDirectory = Application.StartupPath
            Saveme.Title = RM.GetString("fcusb_filesave_type")
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
            SetStatus("Error opening file for writing")
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
        OpenMe.Title = String.Format(RM.GetString("fcusb_mem_choosefile"), FlashName)
        OpenMe.CheckPathExists = True
        OpenMe.Filter = BinFile & "|" & IHexFormat & "|" & AllFiles 'Bin Files, Hex Files, All Files
        If OpenMe.ShowDialog = Windows.Forms.DialogResult.OK Then
            file = New IO.FileInfo(OpenMe.FileName)
            SetStatus(String.Format(RM.GetString("fcusb_mem_filechosen"), FlashName))
            Return True
        Else
            SetStatus(String.Format(RM.GetString("fcusb_mem_usercanwrite"), FlashName))
            Return False
        End If
    End Function

    Private Sub ReadMemoryThread(ByVal read_params As XFER_Operation)
        Try
            FCUSB_LedBlink()
            SetProgress(0)
            DisableControls(True)
            Try
                Try
                    Dim n As New IO.FileInfo(read_params.FileName.FullName)
                    If n.Exists Then n.Delete()
                Catch ex As Exception
                End Try
                WriteConsole(String.Format(RM.GetString("fcusb_memif_beginread"), FlashName))
                WriteConsole(String.Format(RM.GetString("fcusb_memif_startaddr"), read_params.Offset, "0x" & Utilities.Pad(Hex((read_params.Offset))), Format(read_params.Size, "#,###")))
                Dim params As New ReadParameters
                params.Address = read_params.Offset
                params.Count = read_params.Size
                params.Timer = New Stopwatch
                params.Memory_Area = AreaSelected
                params.DisplayStatus = True
                params.UpdateProgress = New cbSetProgress(AddressOf SetProgress)
                params.UpdateSpeed = New cbSetSpeedStatus(AddressOf SetSpeedStatus)
                params.Timer.Start()
                Using data_stream As IO.Stream = read_params.FileName.OpenWrite
                    Select Case AreaSelected
                        Case FlashArea.Main 'We only want to see main data
                            RaiseEvent ReadStream(data_stream, params)
                        Case FlashArea.OOB  'We only want to see spare data
                            RaiseEvent ReadStream(data_stream, params)
                        Case FlashArea.All  'We want to display a mix of main data and spare data
                            MemoryMap_ReadStream(read_params.Offset, read_params.Size, data_stream)
                    End Select
                End Using
                If Operation_Aborted Then
                    SetStatus(RM.GetString("fcusb_mem_usercanceledread"))
                    Try
                        Dim n2 As New IO.FileInfo(read_params.FileName.FullName)
                        If n2.Exists Then n2.Delete()
                    Catch ex As Exception
                    End Try
                Else
                    Dim StatusSpeed As String = Format(Math.Round(read_params.Offset / (params.Timer.ElapsedMilliseconds / 1000)), "#,###") & " Bytes/s"
                    WriteConsole(RM.GetString("fcusb_memif_readdone"))
                    WriteConsole("Read " & Format(read_params.Size, "#,###") & " bytes in " & (params.Timer.ElapsedMilliseconds / 1000) & " seconds, " & StatusSpeed)
                    If read_params.FileName.Extension.ToUpper.EndsWith(".HEX") Then
                        Try
                            WriteConsole("Converting binary file to Intel HEX format")
                            Dim data() As Byte = Utilities.FileIO.ReadBytes(read_params.FileName.FullName)
                            If data IsNot Nothing AndAlso data.Length > 0 Then
                                data = Utilities.BinToIntelHex(data)
                                Utilities.FileIO.WriteBytes(data, read_params.FileName.FullName)
                            End If
                            WriteConsole(RM.GetString("fcusb_filesave_tohex"))
                        Catch ex As Exception
                        End Try
                    End If
                    SetStatus(String.Format(RM.GetString("fcusb_filesave_sucess"), read_params.FileName.Name))
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        Finally
            If read_params.DataStream IsNot Nothing Then read_params.DataStream.Dispose()
            FCUSB_LedOn() 'in case we where blinking
            EnableControls()
            SetProgress(0)
            SetSpeedStatus("")
            GetFocus()
        End Try
    End Sub

    Private Sub WriteMemoryThread(ByVal file_out As XFER_Operation)
        Try
            FCUSB_LedBlink()
            SetProgress(0)
            DisableControls(True)
            Try
                Dim write_success As Boolean = False
                Dim f_params As New WriteParameters
                f_params.Address = file_out.Offset
                f_params.Count = file_out.Size
                f_params.Verify = VerifyData
                f_params.EraseSector = True
                f_params.DisplayStatus = True
                f_params.UpdateProgress = New cbSetProgress(AddressOf SetProgress)
                f_params.UpdateSpeed = New cbSetSpeedStatus(AddressOf SetSpeedStatus)
                Select Case AreaSelected
                    Case FlashArea.Main
                        f_params.Memory_Area = 0
                        RaiseEvent WriteStream(file_out.DataStream, f_params, write_success)
                    Case FlashArea.OOB
                        f_params.Memory_Area = 1
                        RaiseEvent WriteStream(file_out.DataStream, f_params, write_success)
                    Case FlashArea.All
                        MemoryMap_WriteStream(file_out.Offset, file_out.Size, file_out.DataStream, write_success)
                End Select
                file_out.DataStream.Dispose()
                file_out.DataStream = Nothing
                If Operation_Aborted Or Not write_success Then
                    LAST_WRITE_OPERATION = Nothing
                    SetStatus(RM.GetString("fcusb_mem_cancelled"))
                Else
                    Dim Speed As String = CStr(Format(Math.Round(f_params.Count / (f_params.Timer.ElapsedMilliseconds / 1000)), "#,###"))
                    SetStatus(String.Format(RM.GetString("fcusb_memif_writedone"), Format(f_params.Count, "#,###")))
                    WriteConsole(RM.GetString("fcusb_memif_writecomplete"))
                    WriteConsole(String.Format(RM.GetString("fcusb_memif_writespeed"), Format(f_params.Count, "#,###"), (f_params.Timer.ElapsedMilliseconds / 1000), Speed))
                    RaiseEvent SuccessfulWrite(LAST_WRITE_OPERATION)
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        Finally
            SetSpeedStatus("")
            SetProgress(0)
            Editor.UpdateScreen()
            FCUSB_LedOn() 'in case we where blinking
            EnableControls()
        End Try
    End Sub

    Private Sub cmd_cancel_Click(sender As Object, e As EventArgs) Handles cmd_cancel.Click
        Try
            Me.cmd_cancel.Enabled = False
            Operation_Aborted = True
            RaiseEvent StopOperation()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub cmd_read_Click(sender As Object, e As EventArgs) Handles cmd_read.Click
        Operation_Aborted = False
        Dim BaseOffset As Long = 0 'The starting address to read the from data
        Dim UserCount As Long = FlashAvailable 'The total number of bytes to read
        SetStatus(String.Format(RM.GetString("fcusb_mem_readfrom"), FlashName))
        Dim dbox As New DynamicRangeBox
        If Not dbox.ShowRangeBox(BaseOffset, UserCount, FlashAvailable) Then
            SetStatus(RM.GetString("fcusb_mem_readcanceled"))
            Exit Sub
        End If
        SetStatus(RM.GetString("fcusb_mem_readstart"))
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
            SetStatus(RM.GetString("fcusb_filesave_canceled"))
            Exit Sub
        End If
    End Sub

    Private Sub cmd_write_Click(sender As Object, e As EventArgs) Handles cmd_write.Click
        Operation_Aborted = False
        Dim BaseOffset As Long = 0 'The starting address to write data to
        Dim fn As IO.FileInfo = Nothing
        If Not OpenFileForWriteWrite(fn) Then Exit Sub
        If Not fn.Exists OrElse fn.Length = 0 Then
            SetStatus(RM.GetString("fcusb_mem_err1"))
            Exit Sub
        End If
        SetStatus(RM.GetString("fcusb_mem_writestart"))
        LAST_WRITE_OPERATION = New XFER_Operation
        LAST_WRITE_OPERATION.FileName = fn
        LAST_WRITE_OPERATION.Offset = BaseOffset
        LAST_WRITE_OPERATION.Size = -1
        PerformWriteOperation(LAST_WRITE_OPERATION)
        Editor.Focus()
    End Sub

    Private Sub cmd_erase_Click(sender As Object, e As EventArgs) Handles cmd_erase.Click
        If MsgBox("This action will permanently delete all data.", MsgBoxStyle.YesNo, "Confirm erase of " & FlashName) = MsgBoxResult.Yes Then
            WriteConsole("Sent memory erase command to device: " & FlashName)
            SetStatus("Erasing Flash memory device... (this may take up to 2 minutes)")
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
            FCUSB_LedBlink()
            RaiseEvent EraseMemory()
            SetStatus("Erase operation successfully completed")
        Catch ex As Exception
        Finally
            Editor.UpdateScreen()
            FCUSB_LedOn() 'in case we where blinking
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
            WriteConsole(String.Format(RM.GetString("fcusb_mem_opened_intel"), x.FileName.Name, Format(hex_data.Length, "#,###")))
        Else
            x.DataStream = x.FileName.OpenRead
        End If
        Dim BaseAddress As Long = 0 'The starting address to write the data
        If x.Size = -1 Then
            SetStatus(String.Format(RM.GetString("fcusb_mem_selectwriterange"), FlashName))
            Dim dbox As New DynamicRangeBox
            Dim NumberToWrite As Long = x.DataStream.Length 'The total number of bytes to write
            If Not dbox.ShowRangeBox(BaseAddress, NumberToWrite, FlashAvailable) Then
                SetStatus(RM.GetString("fcusb_mem_writecanceled"))
                Exit Sub
            End If
            If NumberToWrite = 0 Then Exit Sub
            x.Offset = BaseAddress
            x.Size = NumberToWrite
        End If
        SetStatus(String.Format(RM.GetString("fcusb_mem_writing"), x.FileName.Name, FlashName, Format(x.Size, "#,###")))
        x.DataStream.Position = 0
        Dim t As New Threading.Thread(AddressOf WriteMemoryThread)
        t.Name = "memWriteTd"
        t.Start(x)
        WriteConsole(String.Format(RM.GetString("fcusb_mem_opened_bin"), x.FileName.Name, Format(x.DataStream.Length, "#,###")))
        WriteConsole("Starting Flash memory write operation (address: 0x" & Hex(x.Offset).PadLeft(8, "0") & "; " & Format(x.Size, "#,###") & " bytes)")
    End Sub


End Class
