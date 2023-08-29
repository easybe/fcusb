Imports FlashcatUSB.USB

Public Class vendor_at28
    Private FCUSB_PROG As MemoryDeviceUSB
    Private NOR_IF As PARALLEL_NOR
    Private FCUSB As FCUSB_DEVICE

    Private MyHexBoxes As New List(Of HexByteBox)
    Private MyLabels As New List(Of Label)

    Sub New(mem_dev_programmer As MemoryDeviceUSB)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Me.FCUSB_PROG = mem_dev_programmer
        Me.NOR_IF = DirectCast(FCUSB_PROG, PARALLEL_NOR)
        Me.FCUSB = NOR_IF.GetUsbDevice()
        For Each item In MyHexBoxes
            If gb_eeprom.Controls.Contains(item) Then
                gb_eeprom.Controls.Remove(item)
            End If
        Next
        For Each item In MyLabels
            If gb_eeprom.Controls.Contains(item) Then
                gb_eeprom.Controls.Remove(item)
            End If
        Next
        MyHexBoxes.Clear()
        MyLabels.Clear()

        Dim eeprom_size As Integer = NOR_IF.MyFlashDevice.EEPROM_SIZE
        Dim row_index As Integer = 0
        Dim column_index As Integer = 0

        For i = 0 To eeprom_size - 1
            Dim n As New HexByteBox
            n.BorderStyle = BorderStyle.FixedSingle
            n.Size = New Size(22, 20)
            n.Location = New Point(53 + (column_index * 25), 43 + (row_index * 26))
            Me.gb_eeprom.Controls.Add(n)
            MyHexBoxes.Add(n)
            n.BringToFront()
            column_index += 1
            If column_index = 16 AndAlso Not (i = eeprom_size - 1) Then
                row_index += 1
                column_index = 0
                Dim l As New Label With {.Width = 38}
                l.Location = New Point(9, 44 + (row_index * 26))
                l.Font = New Font("Microsoft Sans Serif", 9.0!, FontStyle.Bold, GraphicsUnit.Point, CType(0, Byte))
                l.Text = "0x" & Hex(16 * row_index)
                Me.gb_eeprom.Controls.Add(l)
                MyLabels.Add(l)
            End If
        Next

        Dim top_pos As Integer = 80 + (row_index * 26)
        cmdReadToFile.Location = New Point(53, top_pos)
        cmdLoad.Location = New Point(134, top_pos)
        cmdProgramAT28.Location = New Point(377, top_pos)

        gb_eeprom.Height = top_pos + 40
        Me.Height = gb_eeprom.Height + 8
    End Sub

    Private Sub vendor_at28_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim eeprom = EEPROM_ReadData()
        LoadTxtBoxes(eeprom)
        cmdReadToFile.Select()
    End Sub

    Private Function EEPROM_ReadData() As Byte()
        Dim eeprom_size As Integer = NOR_IF.MyFlashDevice.EEPROM_SIZE
        Dim read_data(eeprom_size - 1) As Byte
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.WE_HIGH)
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.CE_LOW)
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.OE_LOW)
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.VPP_12V)
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.RELAY_ON)
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.VPP_ENABLE)
        Utilities.Sleep(100)
        Dim start_addr As UInt32 = NOR_IF.MyFlashDevice.EEPROM_ADDR
        For i = 0 To eeprom_size - 1
            read_data(i) = CByte(NOR_IF.ReadMemoryAddress(start_addr) And 255)
            start_addr += 1UI
        Next
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.VPP_DISABLE)
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.RELAY_OFF)
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.VPP_0V)
        Return read_data
    End Function

    Private Sub EEPROM_WriteData(eeprom_data() As Byte)
        Dim eeprom_size As Integer = NOR_IF.MyFlashDevice.EEPROM_SIZE
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.WE_HIGH)
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.CE_LOW)
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.OE_LOW)
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.VPP_12V)
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.RELAY_ON)
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.VPP_ENABLE)
        Utilities.Sleep(100)
        Dim start_addr As UInt32 = NOR_IF.MyFlashDevice.EEPROM_ADDR
        For i = 0 To eeprom_size - 1
            NOR_IF.WriteMemoryAddress(start_addr, eeprom_data(i))
            Utilities.Sleep(2)
            start_addr += 1UI
        Next
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.VPP_DISABLE)
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.RELAY_OFF)
        Me.FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, FCUSB_HW_CTRL.VPP_0V)
    End Sub

    Private Sub LoadTxtBoxes(data() As Byte)
        For i = 0 To data.Length - 1
            MyHexBoxes.Item(i).ByteData = data(i)
        Next
    End Sub

    Private Function ReadTxtBoxes() As Byte()
        Dim eeprom_size As Integer = NOR_IF.MyFlashDevice.EEPROM_SIZE
        Dim current_data(eeprom_size - 1) As Byte
        For i = 0 To eeprom_size - 1
            current_data(i) = MyHexBoxes.Item(i).ByteData
        Next
        Return current_data
    End Function

    Private Sub cmdReadToFile_Click(sender As Object, e As EventArgs) Handles cmdReadToFile.Click
        Dim current_data() As Byte = ReadTxtBoxes()
        Dim SaveMe As New SaveFileDialog
        SaveMe.AddExtension = True
        SaveMe.InitialDirectory = Application.StartupPath
        SaveMe.Title = "Select location to save EEPROM buffer data"
        SaveMe.CheckPathExists = True
        Dim BinFile As String = "Binary Files (*.bin)|*.bin"
        Dim AllFiles As String = "All files (*.*)|*.*"
        SaveMe.Filter = BinFile & "|" & AllFiles
        If (SaveMe.ShowDialog = DialogResult.OK) Then
            Utilities.FileIO.WriteBytes(current_data, SaveMe.FileName)
        End If
    End Sub

    Private Sub cmdLoad_Click(sender As Object, e As EventArgs) Handles cmdLoad.Click
        Dim BinFile As String = "Binary Files (*.bin)|*.bin"
        Dim AllFiles As String = "All files (*.*)|*.*"
        Dim OpenMe As New OpenFileDialog
        OpenMe.AddExtension = True
        OpenMe.InitialDirectory = Application.StartupPath
        OpenMe.Title = "Select Binary file to load into EEPROM buffer"
        OpenMe.CheckPathExists = True
        OpenMe.Filter = BinFile & "|" & AllFiles
        If (OpenMe.ShowDialog = DialogResult.OK) Then
            Dim incoming_data() As Byte = Utilities.FileIO.ReadBytes(OpenMe.FileName)
            If incoming_data Is Nothing OrElse Not incoming_data.Length = 64 Then
                MsgBox("EEPROM file must be 64-bytes", MsgBoxStyle.Critical, "Invalid EEPROM binary file")
            Else
                LoadTxtBoxes(incoming_data)
            End If
        End If
    End Sub

    Private Sub cmdProgramAT28_Click(sender As Object, e As EventArgs) Handles cmdProgramAT28.Click
        Dim data_to_program() As Byte = ReadTxtBoxes()
        EEPROM_WriteData(data_to_program)
        Dim data_compare() = EEPROM_ReadData()
        If Utilities.ArraysMatch(data_to_program, data_compare) Then
            MsgBox("EEPROM data written successfully", MsgBoxStyle.Information, "EEPROM Program")
        Else
            MsgBox("Error! EEPROM data verification failed", MsgBoxStyle.Critical, "EEPROM Program")
        End If
    End Sub

End Class
