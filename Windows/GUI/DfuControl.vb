Public Class DfuControl
    Public FCUSB As USB.FCUSB_DEVICE

    Private FwHexName As String
    Private FwHexBin() As Byte = Nothing

    Delegate Sub cbUpdateDfuStatusBar(Value As Integer)

    Sub New(usb_dev As USB.FCUSB_DEVICE)
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        FCUSB = usb_dev
        lblAvrFn.Text = "File: no file currently loaded"
        lblAvrRange.Text = "Range: 0x0000-0x0000"
        lblAvrCrc.Text = "CRC: 0x000000"
        FwHexBin = Nothing
        FwHexName = ""
        cmdAvrProg.Enabled = False
        cmdAvrStart.Enabled = False
        AddHandler FCUSB.DFU_IF.SetProgress, AddressOf UpdateDfuStatusBar
    End Sub

    Private Sub cmdAvrLoad_Click(sender As Object, e As EventArgs) Handles cmdAvrLoad.Click
        Dim OpenMe As New OpenFileDialog
        OpenMe.AddExtension = True
        OpenMe.InitialDirectory = Application.StartupPath & "\Firmware"
        OpenMe.Title = "Choose firmware to program"
        OpenMe.CheckPathExists = True
        If FCUSB.USBHANDLE.UsbRegistryInfo.Vid = &H3EB AndAlso FCUSB.USBHANDLE.UsbRegistryInfo.Pid = &H2FF9 Then
            OpenMe.Filter = "Intel Hex Format (*.hex)|*XPORT*.hex"
        ElseIf FCUSB.USBHANDLE.UsbRegistryInfo.Vid = &H3EB AndAlso FCUSB.USBHANDLE.UsbRegistryInfo.Pid = &H2FF0 Then
            OpenMe.Filter = "Intel Hex Format (*.hex)|FCUSB.CLASSIC.*.U2.hex"
        ElseIf FCUSB.USBHANDLE.UsbRegistryInfo.Vid = &H3EB AndAlso FCUSB.USBHANDLE.UsbRegistryInfo.Pid = &H2FF4 Then
            OpenMe.Filter = "Intel Hex Format (*.hex)|FCUSB.CLASSIC.*.U4.hex"
        End If
        If OpenMe.ShowDialog = DialogResult.OK Then
            Dim finfo As New IO.FileInfo(OpenMe.FileName)
            Dim ihex_tool As New IHEX.StreamReader(finfo.OpenText)
            If ihex_tool.IsValid Then
                ReDim FwHexBin(ihex_tool.Length - 1)
                ihex_tool.Read(FwHexBin, 0, FwHexBin.Length)
                FwHexName = finfo.Name
                LoadHexFileInfo()
            Else
                SetStatus("Error: file is corrupt or not a AVR Hex file")
            End If
        End If
    End Sub

    Private Sub cmdAvrProg_Click(sender As Object, e As EventArgs) Handles cmdAvrProg.Click
        Try
            FCUSB.DFU_IF.DeviceInit()
            Dim Res As Boolean = False
            cmdAvrProg.Enabled = False 'Prevents user from double clicking program button
            cmdAvrStart.Enabled = False
            Dim DfuSize As Integer = FCUSB.DFU_IF.DeviceSize()
            If (DfuSize = 0) Then
                SetStatus("Device is no longer in DFU mode") : Exit Sub
            End If
            If (FwHexBin.Length > DfuSize) Then
                SetStatus("Error: failed to retrieve board firmware version") : Exit Sub
            End If
            UpdateDfuStatusBar(0)
            SetStatus("Programming new AVR firmware over USB")
            If (Not FCUSB.DFU_IF.EraseDevice()) Then
                SetStatus("Error: device erase was not successful") : Exit Sub
            Else
                PrintConsole("AVR device erased successful")
            End If
            Application.DoEvents()
            Threading.Thread.Sleep(250)
            PrintConsole(String.Format("Beginning AVR flash write ({0} bytes)", FwHexBin.Length)) 'Beginning AVR flash write ({0} bytes)
            Application.DoEvents()
            Threading.Thread.Sleep(250)
            Res = FCUSB.DFU_IF.WriteData(0, FwHexBin)
            If Not Res Then
                PrintConsole("Error: AVR flash write failed", True) : Exit Sub
            End If
            PrintConsole("AVR flash written successfully")
            SetStatus("New AVR firmware programmed (click 'Start Appplication' to begin)")
            Application.DoEvents()
            Threading.Thread.Sleep(250)
        Catch ex As Exception
        Finally
            cmdAvrStart.Enabled = True
            cmdAvrProg.Enabled = True
            UpdateDfuStatusBar(0)
        End Try
    End Sub

    Private Sub cmdAvrStart_Click(sender As Object, e As EventArgs) Handles cmdAvrStart.Click
        cmdAvrLoad.Enabled = False
        cmdAvrProg.Enabled = False
        cmdAvrStart.Enabled = False
        FCUSB.DFU_IF.RunApp() 'Start application (hardware reset)
    End Sub
    'Loads the gui information and loads up the hex editor
    Private Sub LoadHexFileInfo()
        cmdAvrProg.Enabled = True
        cmdAvrStart.Enabled = True
        lblAvrFn.Text = String.Format("File: {0}", FwHexName)
        lblAvrRange.Text = "Range: 0x0000-0x" & Hex(FwHexBin.Length - 1).PadLeft(4, CChar("0"))
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

    Private Sub UpdateDfuStatusBar(Perc As Integer)
        If Me.InvokeRequired Then
            Dim d As New cbUpdateDfuStatusBar(AddressOf UpdateDfuStatusBar)
            Me.Invoke(d, New Object() {Perc})
        Else
            DfuPbBar.Value = Perc
        End If
    End Sub

End Class
