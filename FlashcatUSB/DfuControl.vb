Imports FlashcatUSB.USB.HostClient

Public Class DfuControl
    Public FCUSB As FCUSB_DEVICE

    Private FwHexName As String
    Private FwHexFile() As String = Nothing
    Private FwHexBin() As Byte = Nothing
    Private HexFileSize As Integer = 0
    Delegate Sub cbUpdateDfuStatusBar(ByVal Value As Integer)

    Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

    End Sub

    Public Sub LoadWindow(ByVal usb_dev As FCUSB_DEVICE)
        FCUSB = usb_dev
        lblAvrFn.Text = "File: no file currently loaded"
        lblAvrRange.Text = "Range: 0x0000-0x0000"
        lblAvrCrc.Text = "CRC: 0x000000"
        FwHexBin = Nothing
        HexFileSize = 0
        FwHexName = ""
        cmdAvrProg.Enabled = False
        cmdAvrStart.Enabled = False
        AddHandler FCUSB.DFU_IF.OnStatus, AddressOf UpdateDfuStatusBar
    End Sub

    Private Sub cmdAvrLoad_Click(sender As Object, e As EventArgs) Handles cmdAvrLoad.Click
        Dim OpenMe As New OpenFileDialog
        OpenMe.AddExtension = True
        OpenMe.InitialDirectory = Application.StartupPath & "\Firmware"
        OpenMe.Title = "Choose firmware to program"
        OpenMe.CheckPathExists = True
        If FCUSB.USBHANDLE.UsbRegistryInfo.Vid = &H3EB AndAlso FCUSB.USBHANDLE.UsbRegistryInfo.Pid = &H2FF9 Then
            OpenMe.Filter = "XPORT Hex Format (*XPORT*.hex)|*XPORT*.hex"
        Else
            OpenMe.Filter = "Classic Hex Format (*CLASSIC*.hex)|*CLASSIC*.hex"
        End If
        If OpenMe.ShowDialog = DialogResult.OK Then
            Dim finfo As New IO.FileInfo(OpenMe.FileName)
            Dim FileData() As Byte = Utilities.FileIO.ReadBytes(finfo.FullName)
            If Utilities.IsIntelHex(FileData) Then
                FwHexBin = Utilities.IntelHexToBin(FileData)
                FwHexName = finfo.Name
                HexFileSize = finfo.Length
                LoadHexFileInfo()
            Else
                SetStatus("Error: file is corrupt or not a AVR Hex file") 'Error: file is corrupt or not a AVR Hex file
            End If
        End If
    End Sub

    Private Sub cmdAvrProg_Click(sender As Object, e As EventArgs) Handles cmdAvrProg.Click
        Dim Res As Boolean = False
        cmdAvrProg.Enabled = False 'Prevents user from double clicking program button
        cmdAvrStart.Enabled = False
        Dim DfuSize As Integer = FCUSB.DFU_IF.GetFlashSize()
        If DfuSize = 0 Then
            SetStatus("Device is no longer in DFU mode")
            GoTo ExitAvrProg
        End If
        If (FwHexBin.Length > DfuSize) Then
            SetStatus("Error: failed to retrieve board firmware version") 'Error: The hex file data is larger than the size of the DFU memory
            GoTo ExitAvrProg
        End If
        UpdateDfuStatusBar(0)
        SetStatus("Programming new AVR firmware over USB")
        Res = FCUSB.DFU_IF.EraseFlash()
        If (Not Res) Then
            SetStatus("FlashcatUSB failed to connect to target board using JTAG")
            GoTo ExitAvrProg
        Else
            WriteConsole("AVR DFU command successful")
        End If
        Application.DoEvents()
        Threading.Thread.Sleep(250)
        WriteConsole(String.Format("Beginning AVR flash write ({0} bytes)", FwHexBin.Length)) 'Beginning AVR flash write ({0} bytes)
        Application.DoEvents()
        Threading.Thread.Sleep(250)
        Res = FCUSB.DFU_IF.WriteFlash(FwHexBin)
        If Not Res Then
            WriteConsole("Error: AVR flash write failed")
            SetStatus("Error: AVR flash write failed")
            GoTo ExitAvrProg
        End If
        WriteConsole("AVR flash written successfully")
        SetStatus("New AVR firmware programmed (click 'Start Appplication' to begin)")
        Application.DoEvents()
        Threading.Thread.Sleep(250)
ExitAvrProg:
        cmdAvrStart.Enabled = True
        cmdAvrProg.Enabled = True
        UpdateDfuStatusBar(0)
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

    Private Sub UpdateDfuStatusBar(ByVal Perc As Integer)
        If Me.InvokeRequired Then
            Dim d As New cbUpdateDfuStatusBar(AddressOf UpdateDfuStatusBar)
            Me.Invoke(d, New Object() {Perc})
        Else
            DfuPbBar.Value = Perc
        End If
    End Sub

End Class
