﻿Public Class FrmPerformance
    Delegate Sub cbStatusPageProgress(percent As Integer)
    Delegate Sub cbSpeed(lbl As String)
    Delegate Sub cbTestComplete()
    Private MB_DN_INDEX As Integer = 0
    Private IS_ERROR As Boolean = False


    Private Sub FrmUSBPerformance_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        lblreadspeed.Visible = False
        lblwritespeed.Visible = False
        cbMachDownloadSize.SelectedIndex = 0
    End Sub

    Private Sub cmdstart_Click(sender As Object, e As EventArgs) Handles cmdstart.Click
        SetProgress(0)
        MB_DN_INDEX = cbMachDownloadSize.SelectedIndex
        cmdstart.Enabled = False
        cbMachDownloadSize.Enabled = False
        lblreadspeed.Visible = False
        lblwritespeed.Visible = False
        Dim t As New Threading.Thread(AddressOf SpeedTest)
        t.Name = "tdSpeedTest"
        t.Start()
    End Sub

    Public Sub SetProgress(percent As Integer)
        If Me.InvokeRequired Then
            Dim d As New cbStatusPageProgress(AddressOf SetProgress)
            Me.Invoke(d, New Object() {percent})
        Else
            If (percent > 100) Then percent = 100
            Me.progress.Value = percent
            Application.DoEvents()
        End If
    End Sub

    Public Sub SetStatus(msg As String)
        If Me.InvokeRequired Then
            Dim d As New cbSpeed(AddressOf SetStatus)
            Me.Invoke(d, New Object() {msg})
        Else
            lblstatus.Text = msg
            Application.DoEvents()
        End If
    End Sub

    Public Sub SetReadSpeed(speed_label As String)
        If Me.InvokeRequired Then
            Dim d As New cbSpeed(AddressOf SetReadSpeed)
            Me.Invoke(d, New Object() {speed_label})
        Else
            lblreadspeed.Text = "Read speed: " & speed_label
            lblreadspeed.Visible = True
            Application.DoEvents()
        End If
    End Sub

    Public Sub SetWriteSpeed(speed_label As String)
        If Me.InvokeRequired Then
            Dim d As New cbSpeed(AddressOf SetWriteSpeed)
            Me.Invoke(d, New Object() {speed_label})
        Else
            lblwritespeed.Text = "Write speed: " & speed_label
            lblwritespeed.Visible = True
            Application.DoEvents()
        End If
    End Sub

    Public Sub TestComplete()
        If Me.InvokeRequired Then
            Dim d As New cbTestComplete(AddressOf TestComplete)
            Me.Invoke(d)
        Else
            If IS_ERROR Then Me.Close()
            cmdstart.Enabled = True
            cbMachDownloadSize.Enabled = True
            SetStatus("USB high-speed test completed!")
        End If
    End Sub

    Private Sub SpeedTest()
        Dim mb_count As Integer = 100 * (MB_DN_INDEX + 1)
        SetStatus("Reading " & mb_count & "MB data from device over USB")
        SAM3U_SpeedTest_Read()
        If Not IS_ERROR Then
            SetProgress(0)
            Utilities.Sleep(1000)
            SetStatus("Writing " & mb_count & "MB data to device over USB")
            SAM3U_SpeedTest_Write()
        End If
        TestComplete()
    End Sub

    Private Sub SAM3U_SpeedTest_Read()
        Dim t As New Stopwatch
        Dim mb_count As Integer = (100 * (MB_DN_INDEX + 1))
        Dim data_count As Integer = 1048576
        Dim data_test(data_count - 1) As Byte
        Dim bytes_transfered As Integer = 0
        Dim counter As Integer = 0
        t.Start()
        Do
            Dim result As Boolean = MAIN_FCUSB.USB_SETUP_BULKIN(USB.USBREQ.TEST_READ, Nothing, data_test, CUInt(data_test.Length))
            If Not result Then
                MsgBox("Error on USB Bulk In operation")
                IS_ERROR = True
                Exit Sub
            End If
            counter += 1
            SetProgress(CInt((CSng(counter) / mb_count) * 100))
            bytes_transfered += data_count
            Dim bytes_per_second As Integer = CInt(Math.Round(bytes_transfered / (t.ElapsedMilliseconds / 1000)))
            If counter = 0 OrElse counter Mod 4 = 0 Then SetReadSpeed(UpdateSpeed_GetText(bytes_per_second))
            If (counter) = mb_count Then Exit Do
        Loop
        t.Stop()
    End Sub

    Private Sub SAM3U_SpeedTest_Write()
        Dim t As New Stopwatch
        Dim mb_count As Integer = (100 * (MB_DN_INDEX + 1))
        Dim data_count As Integer = 1048576
        Dim data_test(data_count - 1) As Byte
        Dim counter As Integer = 0
        Dim bytes_transfered As Integer = 0
        t.Start()
        Do
            Dim result As Boolean = MAIN_FCUSB.USB_SETUP_BULKOUT(USB.USBREQ.TEST_WRITE, Nothing, data_test, CUInt(data_test.Length))
            If Not result Then
                MsgBox("Error on USB Bulk Out operation")
                IS_ERROR = True
                Exit Sub
            End If
            counter += 1
            SetProgress(CInt((CSng(counter) / mb_count) * 100))
            bytes_transfered += data_count
            Dim bytes_per_second As Integer = CInt(Math.Round(bytes_transfered / (t.ElapsedMilliseconds / 1000)))
            If counter = 0 OrElse counter Mod 4 = 0 Then SetWriteSpeed(UpdateSpeed_GetText(bytes_per_second))
            If (counter) = mb_count Then Exit Do
        Loop
        t.Stop()
    End Sub

    Private Function UpdateSpeed_GetText(bytes_per_second As Integer) As String
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


End Class