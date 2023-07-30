'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2021 - ALL RIGHTS RESERVED
'CONTACT EMAIL: support@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: this class creates a flash memory interface that is used by the main program

Imports FlashcatUSB.FlashMemory

Public Class MemoryInterface
    Private MyDevices As New List(Of MemoryDeviceInstance)

    Sub New()

    End Sub

    Public ReadOnly Property DeviceCount As Integer
        Get
            Return MyDevices.Count
        End Get
    End Property

    Public Sub Clear()
        MyDevices.Clear() 'Remove all devices
        MainApp.RemoveAllTabs()
    End Sub

    Public Function GetDevices() As MemoryDeviceInstance()
        If MyDevices.Count = 0 Then Return Nothing
        Return MyDevices.ToArray()
    End Function

    Public Function Add(usb_dev As USB.FCUSB_DEVICE, PRG_IF As MemoryDeviceUSB) As MemoryDeviceInstance
        Dim memDev As New MemoryDeviceInstance(usb_dev, PRG_IF)
        memDev.Name = PRG_IF.DeviceName
        memDev.Size = PRG_IF.DeviceSize
        If usb_dev.HWBOARD = USB.FCUSB_BOARD.ATMEL_DFU Then
            memDev.FlashType = MemoryType.DFU_MODE
        Else
            memDev.FlashType = PRG_IF.GetDevice().FLASH_TYPE
        End If
        MyDevices.Add(memDev)
        Return memDev
    End Function

    Public Sub Remove(device As MemoryDeviceInstance)
        MyDevices.Remove(device)
    End Sub

    Public Sub RefreshAll()
        Try
            For i = 0 To MyDevices.Count - 1
                MyDevices(i).GuiControl.RefreshView()
            Next
        Catch ex As Exception
        End Try
    End Sub

    Public Function GetDevice(index As Integer) As MemoryDeviceInstance
        If index >= MyDevices.Count Then Return Nothing
        Return MyDevices(index)
    End Function

    Public Class MemoryDeviceInstance
        Public WithEvents GuiControl As MemControl_v2
        Public FCUSB As USB.FCUSB_DEVICE
        Public MEM_IF As MemoryDeviceUSB
        Public DEV_MODE As DeviceMode
        Public Property Name As String
        Public Property Size As Long 'Number of bytes of the memory device
        Public Property BaseAddress As UInt32 = 0 'Only changes for JTAG devices
        Public Property FlashType As MemoryType = MemoryType.UNSPECIFIED
        Public Property [ReadOnly] As Boolean = False 'Set to true to disable write/erase functions
        Public Property PreferredBlockSize As Int32 = 32768
        Public Property VendorMenu As Object = Nothing 'Control
        Public Property NoErrors As Boolean = False 'Indicates there was a physical error
        Private Property IsErasing As Boolean = False
        Private Property IsBulkErasing As Boolean = False
        Private Property IsReading As Boolean = False
        Private Property IsWriting As Boolean = False
        Public Property IsTaskRunning As Boolean = False
        Public Property SkipBadBlocks As Boolean = True
        Public Property RetryWriteCount As Integer = 0

        Public ReadOnly Property IsBusy As Boolean
            Get
                If IsErasing Or IsReading Or IsWriting Or IsBulkErasing Then Return True
                Return False
            End Get
        End Property

        Public Event PrintConsole(msg As String)
        Public Event SetStatus(msg As String)
        Public Event WriteOperationSucceded(mydev As MemoryDeviceInstance, x As MemControl_v2.XFER_Operation)
        Public Event WriteOperationFailed(mydev As MemoryDeviceInstance, Params As WriteParameters, ByRef ContinueOperation As Boolean)

        Private InterfaceLock As New Object

        Sub New(usb_interface As USB.FCUSB_DEVICE, PRG_IF As MemoryDeviceUSB)
            Me.GuiControl = New MemControl_v2(Me)
            Me.FCUSB = usb_interface
            Me.MEM_IF = PRG_IF
        End Sub

        Public Class StatusCallback
            Public UpdateOperation As [Delegate] '(Int) 1=Read,2=Write,3=Verify,4=Erasing,5=Error
            Public UpdateBase As [Delegate] '(Uint32) Updates the base address we are erasing/reading/writing
            Public UpdateTask As [Delegate] '(String) Contains the task we are doing
            Public UpdateSpeed As [Delegate] '(String) This is used to update a speed text label
            Public UpdatePercent As [Delegate] '(Integer) This is the percent complete
        End Class

        Friend Sub DisableGuiControls(Optional show_cancel As Boolean = False)
            If GuiControl IsNot Nothing Then GuiControl.DisableControls(show_cancel)
        End Sub

        Friend Sub EnableGuiControls()
            If GuiControl IsNot Nothing Then GuiControl.EnableControls()
        End Sub

        Friend Sub RefreshControls()
            If GuiControl IsNot Nothing Then GuiControl.RefreshView()
        End Sub

#Region "GUICONTROL EVENTS"

        Private Sub OnWriteConsole(msg_out As String) Handles GuiControl.WriteConsole
            RaiseEvent PrintConsole(msg_out)
        End Sub

        Private Sub OnSetStatus(status_text As String) Handles GuiControl.SetStatus
            RaiseEvent SetStatus(status_text)
        End Sub

        Private Sub OnSuccessfulWrite(mydev As USB.FCUSB_DEVICE, x As MemControl_v2.XFER_Operation) Handles GuiControl.SuccessfulWrite
            RaiseEvent WriteOperationSucceded(Me, x)
        End Sub

        Private Sub OnEraseDataRequest() Handles GuiControl.EraseMemory
            Try
                Me.EraseFlash()
                Me.WaitUntilReady()
            Catch ex As Exception
            End Try
        End Sub

        Private Sub OnReadDataRequest(base_addr As Long, ByRef data() As Byte) Handles GuiControl.ReadMemory
            Try
                If Me.IsBulkErasing Then
                    For i = 0 To data.Length - 1
                        data(i) = 255
                    Next
                    Exit Sub
                End If
                data = ReadBytes(base_addr, data.Length)
            Catch ex As Exception
            End Try
        End Sub

        Private Sub OnReadStreamRequest(data_stream As IO.Stream, f_params As ReadParameters) Handles GuiControl.ReadStream
            Try
                If Me.IsBulkErasing Then
                    For i As Long = 0 To f_params.Count - 1
                        data_stream.WriteByte(255)
                    Next
                    Exit Sub
                End If
                ReadStream(data_stream, f_params)
            Catch ex As Exception
            End Try
        End Sub

        Private Sub OnWriteRequest(addr As Long, data() As Byte, verify_wr As Boolean, ByRef Success As Boolean) Handles GuiControl.WriteMemory
            Try
                Success = WriteBytes(addr, data, verify_wr)
            Catch ex As Exception
            End Try
        End Sub

        Private Sub OnWriteStreamRequest(data_stream As IO.Stream, f_params As WriteParameters, ByRef Success As Boolean) Handles GuiControl.WriteStream
            Try
                Success = WriteStream(data_stream, f_params)
                Me.WaitUntilReady()
            Catch ex As Exception
            End Try
        End Sub

        Private Sub OnGetSectorSize(sector_int As Integer, ByRef sector_size As Integer) Handles GuiControl.GetSectorSize
            sector_size = Me.GetSectorSize(sector_int)
            If sector_size = 0 Then sector_size = CInt(Me.Size)
        End Sub

        Private Sub OnGetSectorCount(ByRef count As Integer) Handles GuiControl.GetSectorCount
            count = Me.GetSectorCount()
            If count = 0 Then count = 1
        End Sub

        Private Sub OnGetSectorIndex(addr As Long, ByRef sector_int As Integer) Handles GuiControl.GetSectorIndex
            sector_int = 0
            Dim s_count As Integer = GetSectorCount()
            For i As Integer = 0 To CInt(s_count) - 1
                Dim sector As SectorInfo = GetSectorInfo(i)
                If addr >= sector.BaseAddress AndAlso addr < (sector.BaseAddress + sector.Size) Then
                    sector_int = i
                    Exit Sub
                End If
            Next
        End Sub

        Private Sub OnGetSectorAddress(sector_int As Integer, ByRef addr As Long) Handles GuiControl.GetSectorBaseAddress
            addr = GetSectorBaseAddress(sector_int)
        End Sub

        Private Sub OnGetEccLastResult(ByRef result As Object) Handles GuiControl.GetEccLastResult
            result = ECC_LAST_RESULT
        End Sub

#End Region

        Private Function WaitForNotBusy() As Boolean
            Dim i As Integer = 0
            Do While Me.IsBusy
                Threading.Thread.Sleep(5)
                i += 1
                If i = 1000 Then Return False '10 second timeout
            Loop
            Return True
        End Function

        Public Function ReadBytes(base_addr As Long, count As Long, Optional callback As StatusCallback = Nothing) As Byte()
            Me.NoErrors = True
            Dim data_out() As Byte = Nothing
            Using n As New IO.MemoryStream
                Dim f_params As New ReadParameters
                f_params.Address = base_addr
                f_params.Count = count
                If callback IsNot Nothing Then
                    f_params.Status = callback
                End If
                If ReadStream(n, f_params) Then
                    data_out = n.GetBuffer()
                    ReDim Preserve data_out(CInt(n.Length) - 1)
                End If
            End Using
            Return data_out
        End Function

        Public Function WriteBytes(mem_addr As Long, mem_data() As Byte, verify_wr As Boolean, Optional callback As StatusCallback = Nothing) As Boolean
            Try
                Me.NoErrors = True
                Dim f_params As New WriteParameters
                f_params.Address = mem_addr
                f_params.BytesLeft = mem_data.Length
                f_params.Verify = verify_wr
                If callback IsNot Nothing Then
                    f_params.Status = callback
                End If
                Using n As New IO.MemoryStream(mem_data)
                    Return WriteStream(n, f_params)
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function ReadStream(data_stream As IO.Stream, Params As ReadParameters) As Boolean
            Me.NoErrors = True
            If (Threading.Thread.CurrentThread.Name Is Nothing) Then
                Dim td_int As Integer = Threading.Thread.CurrentThread.ManagedThreadId
                Threading.Thread.CurrentThread.Name = "MemIf.ReadStream_" & td_int
            End If
            Try
                Dim BlockSize As Integer = Me.PreferredBlockSize
                Dim Loops As Integer = CInt(Math.Ceiling(Params.Count / BlockSize)) 'Calcuates iterations
                Dim read_buffer() As Byte 'Temp Byte buffer
                Dim BytesRead As Long = 0 'Number of bytes read from the Flash device
                If Params.Status.UpdateOperation IsNot Nothing Then
                    Params.Status.UpdateOperation.DynamicInvoke(1) 'READ IMG
                End If
                If Params.Status.UpdateTask IsNot Nothing Then
                    Dim rd_label As String = String.Format(RM.GetString("mem_reading_memory"), Strings.Format(Params.Count, "#,###"))
                    Params.Status.UpdateTask.DynamicInvoke(rd_label)
                End If
                Dim BytesRemaining As Long = Params.Count
                For i = 1 To Loops
                    Dim buffer_size As Integer
                    If (BytesRemaining > CLng(BlockSize)) Then
                        buffer_size = BlockSize
                    Else
                        buffer_size = CInt(BytesRemaining)
                    End If
                    Dim FlashAddress As Long = Params.Address + BytesRead
                    If Params.Status.UpdateBase IsNot Nothing Then
                        Params.Status.UpdateBase.DynamicInvoke(FlashAddress)
                    End If
                    If Params.Status.UpdatePercent IsNot Nothing Then
                        Dim percent_done As Single = CSng((i / Loops) * 100) 'Calulate % done
                        Params.Status.UpdatePercent.DynamicInvoke(CInt(percent_done))
                    End If
                    Dim packet_timer As New Stopwatch
                    packet_timer.Start()
                    read_buffer = ReadFlash(FlashAddress, buffer_size)
                    packet_timer.Stop()
                    If Params.AbortOperation OrElse (Not Me.NoErrors) OrElse read_buffer Is Nothing Then Return False
                    data_stream.Write(read_buffer, 0, buffer_size)
                    BytesRemaining -= buffer_size
                    BytesRead += buffer_size
                    If i = 1 OrElse i = Loops OrElse (i Mod 4 = 0) Then
                        Try
                            Threading.Thread.CurrentThread.Join(10) 'Pump a message
                            If Params.Status.UpdateSpeed IsNot Nothing Then
                                Dim transfer_seconds As Single = CSng(packet_timer.ElapsedMilliseconds) / 1000
                                Dim bytes_per_second As UInt32 = CUInt(Math.Round(buffer_size / transfer_seconds))
                                Dim speed_text As String = UpdateSpeed_GetText(bytes_per_second)
                                Params.Status.UpdateSpeed.DynamicInvoke(speed_text)
                            End If
                            data_stream.Flush()
                        Catch ex As Exception
                        End Try
                    End If
                Next
                Return True
            Catch ex As Exception
                RaiseEvent PrintConsole("Error in ReadStream")
            Finally
                If Params.Timer IsNot Nothing Then Params.Timer.Stop()
            End Try
            Return False
        End Function

        Public Function WriteStream(data_stream As IO.Stream, params As WriteParameters) As Boolean
            Try : Me.IsTaskRunning = True
                Me.NoErrors = True
                If (Threading.Thread.CurrentThread.Name Is Nothing) Then
                    Dim td_int As Integer = Threading.Thread.CurrentThread.ManagedThreadId
                    Threading.Thread.CurrentThread.Name = "MemIf.WriteBytes_" & td_int
                End If
                If Me.ReadOnly Then Return False
                Try
                    params.Timer = New Stopwatch
                    If FlashType = MemoryType.SERIAL_I2C Then
                        Return WriteBytes_I2C(data_stream, params)
                    ElseIf FlashType = MemoryType.OTP_EPROM Then
                        Return WriteBytes_EPROM(data_stream, params)
                    ElseIf FlashType = MemoryType.SERIAL_SWI Then
                        Return WriteBytes_EPROM(data_stream, params)
                    Else 'Non-Volatile memory
                        Return WriteBytes_NonVolatile(data_stream, params)
                    End If
                Finally
                    params.Timer.Stop()
                    ReadMode()
                End Try
            Catch ex As Exception
            Finally
                Me.IsTaskRunning = False
            End Try
            Return False
        End Function

        Private Function WriteBytes_EPROM(data_stream As IO.Stream, Params As WriteParameters) As Boolean
            Me.WaitUntilReady() 'Some flash devices requires us to wait before sending data
            Dim FailedAttempts As Integer = 0
            Dim ReadResult As Boolean
            Dim BlockSize As Integer = 8192
            While (Params.BytesLeft > 0)
                If Params.AbortOperation Then Return False
                Dim PacketSize As Integer = CInt(Params.BytesLeft)
                If PacketSize > BlockSize Then PacketSize = BlockSize
                If Params.Status.UpdateBase IsNot Nothing Then Params.Status.UpdateBase.DynamicInvoke(Params.Address)
                If Params.Status.UpdateOperation IsNot Nothing Then Params.Status.UpdateOperation.DynamicInvoke(2) 'WRITE IMG
                If Params.Status.UpdateTask IsNot Nothing Then
                    Dim wr_label As String = String.Format(RM.GetString("mem_writing_memory"), Format(PacketSize, "#,###"))
                    Params.Status.UpdateTask.DynamicInvoke(wr_label)
                End If
                Dim packet_data(PacketSize - 1) As Byte
                data_stream.Read(packet_data, 0, CInt(PacketSize)) 'Reads data from the stream
                Params.Timer.Start()
                Dim write_result As Boolean = Me.MEM_IF.WriteData(Params.Address, packet_data, Params)
                Params.Timer.Stop()
                If Not write_result Then Return False
                If Params.AbortOperation Then Return False
                If Not Me.NoErrors Then Return False
                Threading.Thread.CurrentThread.Join(10) 'Pump a message
                If Params.Verify AndAlso FlashType = MemoryType.SERIAL_SWI Then 'Verify is enabled and we are monitoring this
                    If Params.Status.UpdateOperation IsNot Nothing Then Params.Status.UpdateOperation.DynamicInvoke(3) 'VERIFY IMG
                    If Params.Status.UpdateTask IsNot Nothing Then
                        Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_data"))
                    End If
                    MainApp.DoEvents()
                    Utilities.Sleep(50)
                    If FlashType = MemoryType.OTP_EPROM Then
                        Me.MEM_IF.ReadData(Params.Address, BlockSize) 'Before we verify, we should read the entire block once
                    End If
                    ReadResult = WriteBytes_VerifyWrite(Params.Address, packet_data)
                    If ReadResult Then
                        FailedAttempts = 0
                        If Params.Status.UpdateTask IsNot Nothing Then
                            Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_okay"))
                            MainApp.DoEvents()
                            Utilities.Sleep(500)
                        End If
                    Else
                        If FailedAttempts = Me.RetryWriteCount Then
                            RaiseEvent PrintConsole(String.Format(RM.GetString("mem_verify_failed_at"), Hex(Params.Address)))
                            If Params.Status.UpdateOperation IsNot Nothing Then
                                Params.Status.UpdateOperation.DynamicInvoke(5) 'ERROR IMG
                            End If
                            If Params.Status.UpdateTask IsNot Nothing Then
                                Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_failed"))
                                Utilities.Sleep(1000)
                                MainApp.DoEvents()
                            End If
                            Return False
                        End If
                        FailedAttempts += 1
                        Utilities.Sleep(500)
                    End If
                End If
                Params.BytesWritten += PacketSize
                Params.BytesLeft -= PacketSize
                Params.Address += PacketSize
                Dim percent_done As Single = CSng(CSng((Params.BytesWritten) / CSng(Params.BytesTotal)) * 100)
                If Params.Status.UpdateSpeed IsNot Nothing Then
                    Try
                        Dim bytes_per_second As UInt32 = CUInt(Math.Round(Params.BytesWritten / (Params.Timer.ElapsedMilliseconds / 1000)))
                        Dim speed_text As String = UpdateSpeed_GetText(bytes_per_second)
                        Params.Status.UpdateSpeed.DynamicInvoke(speed_text)
                    Catch ex As Exception
                    End Try
                End If
                If Params.Status.UpdatePercent IsNot Nothing Then
                    Params.Status.UpdatePercent.DynamicInvoke(CInt(percent_done))
                End If
            End While
            Return True 'Operation was successful
        End Function

        Private Function WriteBytes_NonVolatile(data_stream As IO.Stream, Params As WriteParameters) As Boolean
            Me.WaitUntilReady() 'Some flash devices requires us to wait before sending data
            Dim TotalSectors As Integer = GetSectorCount()
            Params.BytesTotal = Params.BytesLeft 'Total size of the data we are writing
            Dim percent_done As Single = 0
            For i As Integer = 0 To TotalSectors - 1
                Dim sector As SectorInfo = GetSectorInfo(i)
                Dim sector_start As Long = sector.BaseAddress 'First byte of the sector
                Dim sector_end As Long = sector_start + sector.Size - 1 'Last byte of the sector
                If (Params.Address >= sector_start) And (Params.Address <= sector_end) Then 'This sector contains data we want to change
                    Dim SectorData(sector.Size - 1) As Byte 'The array that will contain the sector data to write
                    Dim SectorStart As Long = (Params.Address - sector.BaseAddress) 'This is where in the sector we are going to fill from stream
                    Dim SectorEnd As Long = Math.Min(sector.Size, (SectorStart + Params.BytesLeft)) - 1  'This is where we will stop filling from stream
                    Dim StreamCount As Integer = CInt((SectorEnd - SectorStart) + 1) 'This is the number of bytes we are going to read for this sector
                    If (SectorStart > 0) Then 'We need to fill beginning
                        Dim data_segment() As Byte = ReadFlash(sector.BaseAddress, CInt(SectorStart))
                        Array.Copy(data_segment, 0, SectorData, 0, data_segment.Length)
                        Params.Address = sector.BaseAddress 'This is to adjust the base address, as we are going to write data before our starting point
                    End If
                    data_stream.Read(SectorData, CInt(SectorStart), StreamCount) 'This reads data from our stream
                    If (SectorEnd < (sector.Size - 1)) Then 'We need to fill the end
                        WaitUntilReady()
                        Dim BytesNeeded As Integer = CInt(sector.Size - (SectorEnd + 1))
                        Dim data_segment() As Byte = ReadFlash(sector.BaseAddress + SectorEnd + 1L, BytesNeeded)
                        Array.Copy(data_segment, 0, SectorData, SectorEnd + 1, data_segment.Length)
                    End If
                    Dim WriteResult As Boolean = WriteBytes_EraseSectorAndWrite(i, SectorData, CInt(Math.Floor(percent_done)), Params) 'Writes data
                    If Params.AbortOperation Then Return False
                    If Not Me.NoErrors Then Return False
                    Threading.Thread.CurrentThread.Join(10) 'Pump a message
                    If WriteResult Then
                        Params.BytesWritten += SectorData.Length
                        Params.BytesLeft -= StreamCount
                        Params.Address = sector.BaseAddress + sector.Size
                        percent_done = CSng(CSng((Params.BytesWritten) / CSng(Params.BytesTotal)) * 100)
                        If Params.Status.UpdateSpeed IsNot Nothing Then
                            Dim bytes_per_second As UInt32 = CUInt(Math.Round(Params.BytesWritten / (Params.Timer.ElapsedMilliseconds / 1000)))
                            Dim speed_text As String = UpdateSpeed_GetText(bytes_per_second)
                            Params.Status.UpdateSpeed.DynamicInvoke(speed_text)
                        End If
                        If Params.Status.UpdatePercent IsNot Nothing Then
                            Params.Status.UpdatePercent.DynamicInvoke(CInt(percent_done))
                        End If
                    Else
                        If (FlashType = MemoryType.PARALLEL_NAND) OrElse (FlashType = MemoryType.SERIAL_NAND) Then
                            If MySettings.NAND_SkipBadBlock Then 'Bad block
                                If (i = TotalSectors - 1) Then Return False 'No more blocks to write
                                data_stream.Position -= StreamCount 'We are going to re-write these bytes to the next block
                                Params.Address += SectorData.Length 'and to this base address
                            Else
                                Return False
                            End If
                        Else
                            Return False
                        End If
                    End If
                    If Params.BytesLeft = 0 Then data_stream.Dispose() : Exit For
                End If
            Next
            Return True 'Operation was successful
        End Function

        Private Function WriteBytes_I2C(data_stream As IO.Stream, Params As WriteParameters) As Boolean
            Try
                Dim TotalSize As Integer = CInt(Params.BytesLeft)
                Dim BytesPerPacket As Integer = CInt(PreferredBlockSize)
                Dim percent_done As Single = 0
                Dim BytesTransfered As UInt32 = 0
                Do While Params.BytesLeft > 0
                    If Params.AbortOperation Then Return False
                    Threading.Thread.CurrentThread.Join(10)
                    Dim PacketSize As Integer = Math.Min(CInt(Params.BytesLeft), BytesPerPacket)
                    Dim packet_data(PacketSize - 1) As Byte
                    data_stream.Read(packet_data, 0, PacketSize) 'Reads data from the stream
                    If Params.Status.UpdateTask IsNot Nothing Then
                        Dim wr_label As String = String.Format(RM.GetString("mem_writing_memory"), Format(PacketSize, "#,###"))
                        Params.Status.UpdateTask.DynamicInvoke(wr_label)
                        MainApp.DoEvents()
                    End If
                    If Params.Status.UpdateOperation IsNot Nothing Then
                        Params.Status.UpdateOperation.DynamicInvoke(2) 'WRITE IMG
                    End If
                    If Params.Status.UpdateBase IsNot Nothing Then
                        Params.Status.UpdateBase.DynamicInvoke(Params.Address)
                    End If
                    Params.Timer.Start()
                    Dim i2c_result As Boolean = Me.MEM_IF.WriteData(Params.Address, packet_data)
                    Params.Timer.Stop()
                    If Not i2c_result Then
                        RaiseEvent PrintConsole(RM.GetString("mem_i2c_error"))
                        Return False
                    End If
                    Dim write_result As Boolean = True
                    If Params.Verify Then
                        If Params.Status.UpdateTask IsNot Nothing Then
                            Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_data"))
                            MainApp.DoEvents()
                        End If
                        If Params.Status.UpdateOperation IsNot Nothing Then
                            Params.Status.UpdateOperation.DynamicInvoke(3) 'VERIFY IMG
                        End If
                        write_result = WriteBytes_VerifyWrite(Params.Address, packet_data)
                        If write_result Then
                            If Params.Status.UpdateTask IsNot Nothing Then
                                Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_okay"))
                                MainApp.DoEvents()
                                Utilities.Sleep(500)
                            End If
                        Else 'Write failed
                            RaiseEvent PrintConsole(String.Format(RM.GetString("mem_verify_failed_at"), Hex(Params.Address)))
                            If Params.Status.UpdateOperation IsNot Nothing Then
                                Params.Status.UpdateOperation.DynamicInvoke(5) 'ERROR IMG
                            End If
                            If Params.Status.UpdateTask IsNot Nothing Then
                                Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_failed"))
                                Utilities.Sleep(1000)
                                MainApp.DoEvents()
                            End If
                        End If
                    End If
                    If write_result Then
                        BytesTransfered += CUInt(PacketSize)
                        Params.BytesLeft -= PacketSize
                        Params.Address += PacketSize
                        percent_done = ((CSng(BytesTransfered) / CSng(TotalSize)) * 100)
                        If Params.Status.UpdateSpeed IsNot Nothing Then
                            Dim bytes_per_second As UInt32 = CUInt(Math.Round(BytesTransfered / (Params.Timer.ElapsedMilliseconds / 1000)))
                            Dim speed_text As String = UpdateSpeed_GetText(bytes_per_second)
                            Params.Status.UpdateSpeed.DynamicInvoke(speed_text)
                        End If
                        If Params.Status.UpdatePercent IsNot Nothing Then
                            Params.Status.UpdatePercent.DynamicInvoke(CInt(percent_done))
                        End If
                    Else 'Write/verification failed
                        Return False
                    End If
                Loop
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Function GetSectorInfo(sector_index As Integer) As SectorInfo
            Dim si As SectorInfo
            si.BaseAddress = GetSectorBaseAddress(sector_index)
            si.Size = GetSectorSize(sector_index)
            Return si
        End Function

        Private Structure SectorInfo
            Dim BaseAddress As Long
            Dim Size As Integer
        End Structure
        'Does the actual erase sector and program functions
        Private Function WriteBytes_EraseSectorAndWrite(ByRef sector As Integer, data() As Byte, Percent As Integer, Params As WriteParameters) As Boolean
            Try
                Dim FailedAttempts As Integer = 0
                Dim ReadResult As Boolean
                Do
                    If Params.Status.UpdateBase IsNot Nothing Then Params.Status.UpdateBase.DynamicInvoke(Params.Address)
                    If Params.AbortOperation Then Return False
                    If Not Me.NoErrors Then Return False
                    If Params.EraseSector Then
                        If Params.Status.UpdateOperation IsNot Nothing Then
                            Params.Status.UpdateOperation.DynamicInvoke(4) 'ERASE IMG
                        End If
                        If Params.Status.UpdateTask IsNot Nothing Then
                            Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_erasing_sector"))
                        End If
                        EraseSector(sector)
                        If Not Me.NoErrors Then
                            RaiseEvent PrintConsole("Failed to erase memory at address: 0x" & Hex(Params.Address).PadLeft(8, "0"c))
                            If Not GetMessageBoxForSectorErase(Params.Address, sector) Then Return False
                        End If
                    End If
                    If Params.Status.UpdateOperation IsNot Nothing Then
                        Params.Status.UpdateOperation.DynamicInvoke(2) 'WRITE IMG
                    End If
                    If Params.Status.UpdateTask IsNot Nothing Then
                        Dim wr_label As String = String.Format(RM.GetString("mem_writing_memory"), Format(data.Length, "#,###"))
                        Params.Status.UpdateTask.DynamicInvoke(wr_label)
                    End If
                    WriteSector(sector, data, Params)
                    If Params.AbortOperation Then Return False
                    If Not Me.NoErrors Then Return False
                    If Params.Verify Then 'Verify is enabled and we are monitoring this
                        If Params.Status.UpdateOperation IsNot Nothing Then
                            Params.Status.UpdateOperation.DynamicInvoke(3) 'VERIFY IMG
                        End If
                        If Params.Status.UpdateTask IsNot Nothing Then
                            Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_data"))
                        End If
                        MainApp.DoEvents()
                        If (Me.FlashType = MemoryType.PARALLEL_NOR) Then
                            Utilities.Sleep(200) 'Some older devices need a delay here after writing data (such as AM29F040B)
                        End If
                        ReadResult = WriteBytes_VerifyWrite(Params.Address, data)
                        If Params.AbortOperation Then Return False
                        If Not Me.NoErrors Then Return False
                        If ReadResult Then
                            FailedAttempts = 0
                            If Params.Status.UpdateTask IsNot Nothing Then
                                Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_okay"))
                            End If
                        Else
                            If FailedAttempts = MySettings.RETRY_WRITE_ATTEMPTS Then
                                If (FlashType = FlashMemory.MemoryType.PARALLEL_NAND) Then
                                    Dim PNAND_IF As PARALLEL_NAND = CType(Me.MEM_IF, PARALLEL_NAND)
                                    Dim n_dev As P_NAND = PNAND_IF.MyFlashDevice
                                    Dim pages_per_block As Integer = (n_dev.Block_Size \ n_dev.PAGE_SIZE)
                                    Dim page_addr As Integer = NAND_LayoutTool.GetNandPageAddress(n_dev, Params.Address, PNAND_IF.MemoryArea)
                                    Dim block_addr As UInt32 = CUInt(page_addr \ pages_per_block)
                                    RaiseEvent PrintConsole(String.Format(RM.GetString("mem_bad_nand_block"), Hex(page_addr).PadLeft(6, "0"c), block_addr))
                                    Return False
                                ElseIf (FlashType = FlashMemory.MemoryType.SERIAL_NAND) Then
                                    Dim SNAND_IF As SPINAND_Programmer = CType(Me.MEM_IF, SPINAND_Programmer)
                                    Dim n_dev As SPI_NAND = DirectCast(SNAND_IF.MyFlashDevice, SPI_NAND)
                                    Dim pages_per_block As Integer = (n_dev.Block_Size \ n_dev.PAGE_SIZE)
                                    Dim page_addr As Integer = NAND_LayoutTool.GetNandPageAddress(n_dev, Params.Address, SNAND_IF.MemoryArea)
                                    Dim block_addr As UInt32 = CUInt(page_addr \ pages_per_block)
                                    RaiseEvent PrintConsole(String.Format(RM.GetString("mem_bad_nand_block"), Hex(page_addr).PadLeft(6, "0"c), block_addr))
                                    Return False
                                Else
                                    RaiseEvent PrintConsole(String.Format(RM.GetString("mem_verify_failed_at"), Hex(Params.Address)))
                                    If Params.Status.UpdateOperation IsNot Nothing Then
                                        Params.Status.UpdateOperation.DynamicInvoke(5) 'ERROR IMG
                                    End If
                                    If Params.Status.UpdateTask IsNot Nothing Then
                                        Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_failed"))
                                        Utilities.Sleep(1000)
                                    End If
                                    If (FlashType = FlashMemory.MemoryType.OTP_EPROM) Then Return False 'We can not erase twice
                                    Dim ContinueProg As Boolean = False
                                    RaiseEvent WriteOperationFailed(Me, Params, ContinueProg)
                                    Return ContinueProg
                                End If
                            End If
                            FailedAttempts += 1
                            Utilities.Sleep(500)
                        End If
                    Else
                        ReadResult = True 'We are skiping verification
                    End If
                Loop Until ReadResult Or (Not Params.Verify)
            Catch ex As Exception
                Return False 'ERROR
            End Try
            Return True
        End Function

        Private Function WriteBytes_VerifyWrite(BaseAddress As Long, Data() As Byte) As Boolean
            Dim Verify() As Byte 'The data to check against
            Dim MiscountCounter As Integer = 0
            Dim FirstWrongByteIs As Byte = Nothing
            Dim FirstWrongAddr As Integer = 0
            Dim FirstWrongByteShould As Byte = 0
            WaitUntilReady()
            Verify = ReadFlash(BaseAddress, Data.Length)
            If Verify Is Nothing OrElse (Not Verify.Length = Data.Length) Then Return False
            For i As Integer = 0 To Data.Length - 1
                If (Not Data(i) = Verify(i)) Then
                    If MiscountCounter = 0 Then
                        FirstWrongByteIs = Verify(i)
                        FirstWrongByteShould = Data(i)
                        FirstWrongAddr = CInt(BaseAddress + i)
                    End If
                    MiscountCounter = MiscountCounter + 1
                End If
            Next
            If (MiscountCounter = 0) Then 'Verification successful
                Return True
            Else 'Error!
                RaiseEvent PrintConsole(String.Format(RM.GetString("mem_verify_mismatches"), "0x" & Hex(FirstWrongAddr), "0x" & Hex(FirstWrongByteShould), "0x" & Hex(FirstWrongByteIs), MiscountCounter))
                Return False 'Error!
            End If
        End Function

        Public Function GetMessageBoxForSectorErase(address As Long, sector_index As Integer) As Boolean
            Dim TitleTxt As String = String.Format(RM.GetString("mem_erase_failed_at"), Hex(address).PadLeft(8, "0"c), sector_index)
            TitleTxt &= vbCrLf & vbCrLf & RM.GetString("mem_ask_continue")
            If MsgBox(TitleTxt, MsgBoxStyle.YesNo, RM.GetString("mem_erase_failed_title")) = MsgBoxResult.No Then
                Return False 'Stop working
            Else
                Return True
            End If
        End Function

#Region "Protocol Hooks"

        Public Sub WaitUntilReady()
            Select Case Me.FlashType
                Case MemoryType.JTAG_CFI
                    FCUSB.JTAG_IF.CFI_WaitUntilReady()
                Case MemoryType.JTAG_SPI
                    FCUSB.JTAG_IF.SPI_WaitUntilReady()
                Case Else
                    Me.MEM_IF.WaitUntilReady()
            End Select
        End Sub

        Public Sub ReadMode()
            If Me.MEM_IF.GetType() Is GetType(BSR_Programmer) Then
                DirectCast(Me.MEM_IF, BSR_Programmer).ResetDevice()
            ElseIf Me.MEM_IF.GetType() Is GetType(LINK_Programmer) Then
                DirectCast(Me.MEM_IF, LINK_Programmer).ResetDevice()
            Else
                Select Case Me.FlashType
                    Case MemoryType.PARALLEL_NOR
                        CType(Me.MEM_IF, PARALLEL_NOR).ResetDevice()
                    Case MemoryType.JTAG_CFI
                        FCUSB.JTAG_IF.CFI_ReadMode()
                End Select
            End If
        End Sub

        Public Function ReadFlash(Address As Long, Count As Integer) As Byte()
            If Not WaitForNotBusy() Then Return Nothing
            Try : Me.IsReading = True
                Dim data_out() As Byte = Nothing
                Dim offset As Integer = BitSwap_Offset()
                Dim data_read_count As Integer = Count
                Dim data_offset As Long = Address
                Dim align As Integer = 0
                If (offset > 0) Then align = CInt(Address Mod offset)
                If (align > 0) Then
                    data_offset -= align
                    data_read_count += align
                    Do Until data_read_count Mod offset = 0
                        data_read_count += 1
                    Loop
                End If
                Try : Threading.Monitor.Enter(InterfaceLock)
                    Select Case Me.FlashType
                        Case MemoryType.JTAG_CFI
                            data_out = FCUSB.JTAG_IF.CFI_ReadFlash(CUInt(data_offset), data_read_count)
                        Case MemoryType.JTAG_SPI
                            data_out = FCUSB.JTAG_IF.SPI_ReadFlash(CUInt(data_offset), data_read_count)
                        Case Else
                            data_out = Me.MEM_IF.ReadData(data_offset, data_read_count)
                    End Select
                Catch ex As Exception
                Finally
                    Threading.Monitor.Exit(InterfaceLock)
                End Try
                If data_out Is Nothing Then
                    Me.NoErrors = False 'We have a read operation read
                    Return Nothing
                End If
                BitSwap_Reverse(data_out)
                If (align > 0) Then
                    Dim processed_data(Count - 1) As Byte
                    Array.Copy(data_out, align, processed_data, 0, processed_data.Length)
                    Return processed_data
                Else
                    Return data_out
                End If
            Finally
                Me.IsReading = False
                MainApp.DoEvents()
            End Try
        End Function

        Public Function EraseFlash() As Boolean
            If Not WaitForNotBusy() Then Return False
            Try : Threading.Monitor.Enter(InterfaceLock)
                Me.IsBulkErasing = True
                Select Case Me.FlashType
                    Case MemoryType.JTAG_CFI
                        FCUSB.JTAG_IF.CFI_EraseDevice()
                    Case MemoryType.JTAG_SPI
                        FCUSB.JTAG_IF.SPI_EraseBulk()
                    Case Else
                        Me.MEM_IF.EraseDevice()
                End Select
            Finally
                Threading.Monitor.Exit(InterfaceLock)
                Me.IsBulkErasing = False
            End Try
            MainApp.DoEvents()
            Return True
        End Function

        Public Sub EraseSector(sector_index As Integer)
            If Not WaitForNotBusy() Then Exit Sub
            Me.IsErasing = True
            Try : Threading.Monitor.Enter(InterfaceLock)
                Select Case Me.FlashType
                    Case MemoryType.JTAG_CFI
                        Me.NoErrors = FCUSB.JTAG_IF.CFI_Sector_Erase(sector_index)
                    Case MemoryType.JTAG_SPI
                        Me.NoErrors = FCUSB.JTAG_IF.SPI_SectorErase(sector_index)
                    Case Else
                        Me.NoErrors = Me.MEM_IF.SectorErase(sector_index)
                End Select
            Finally
                Threading.Monitor.Exit(InterfaceLock)
            End Try
            MainApp.DoEvents()
            Me.IsErasing = False
        End Sub

        Public Sub WriteSector(sector_index As Integer, Data() As Byte, Params As WriteParameters)
            If Not WaitForNotBusy() Then Exit Sub
            Me.IsWriting = True
            Try : Threading.Monitor.Enter(InterfaceLock)
                Dim DataToWrite(Data.Length - 1) As Byte
                Array.Copy(Data, DataToWrite, Data.Length)
                BitSwap_Forward(DataToWrite)
                If Params IsNot Nothing Then Params.Timer.Start()
                Select Case Me.FlashType
                    Case MemoryType.JTAG_CFI
                        Me.NoErrors = FCUSB.JTAG_IF.CFI_SectorWrite(sector_index, DataToWrite)
                    Case MemoryType.JTAG_SPI
                        Me.NoErrors = FCUSB.JTAG_IF.SPI_SectorWrite(sector_index, DataToWrite)
                    Case Else
                        Me.NoErrors = Me.MEM_IF.SectorWrite(sector_index, DataToWrite, Params)
                End Select
                If Params IsNot Nothing Then Params.Timer.Stop()
            Finally
                Threading.Monitor.Exit(InterfaceLock)
            End Try
            Me.IsWriting = False
            MainApp.DoEvents()
        End Sub

        Public Function GetSectorCount() As Integer
            Select Case Me.FlashType
                Case MemoryType.JTAG_CFI
                    Return FCUSB.JTAG_IF.CFI_SectorCount()
                Case MemoryType.JTAG_SPI
                    Return FCUSB.JTAG_IF.SPI_SectorCount()
                Case Else
                    Return Me.MEM_IF.SectorCount()
            End Select
        End Function

        Public Function GetSectorSize(sector_index As Integer) As Integer
            Select Case Me.FlashType
                Case MemoryType.JTAG_CFI
                    Return FCUSB.JTAG_IF.CFI_GetSectorSize(sector_index)
                Case MemoryType.JTAG_SPI
                    Return FCUSB.JTAG_IF.SPI_GetSectorSize(sector_index)
                Case Else
                    Return Me.MEM_IF.SectorSize(sector_index)
            End Select
        End Function

        Public Function GetSectorBaseAddress(sector_index As Integer) As Long
            Select Case Me.FlashType
                Case MemoryType.JTAG_CFI
                    Return FCUSB.JTAG_IF.CFI_FindSectorBase(sector_index)
                Case MemoryType.JTAG_SPI
                    Return FCUSB.JTAG_IF.CFI_FindSectorBase(sector_index)
                Case Else
                    Return Me.MEM_IF.SectorFind(sector_index)
            End Select
        End Function

#End Region

    End Class

    Public Sub DisabledControls(Optional show_cancel As Boolean = False)
        For Each mem_entry In MyDevices
            mem_entry.DisableGuiControls(show_cancel)
        Next
    End Sub

    Public Sub EnableControls()
        For Each mem_entry In MyDevices
            mem_entry.EnableGuiControls()
        Next
    End Sub

    Public Sub AbortOperations()
        Try
            Dim Counter As Integer = 0
            For Each memdev In MyDevices
                If memdev.GuiControl IsNot Nothing Then memdev.GuiControl.AbortAnyOperation()
                Do While memdev.IsBusy
                    Utilities.Sleep(100)
                    Counter += 1
                    If Counter = 100 Then Exit Sub '10 seconds
                Loop
            Next
        Catch ex As Exception
        End Try
    End Sub

    Private Shared Function UpdateSpeed_GetText(bytes_per_second As UInt32) As String
        Dim speed_str As String
        If (bytes_per_second > (Mb008 - 1)) Then '1MB or higher
            speed_str = Format((CSng(bytes_per_second) / CSng(Mb008)), "#,###.000") & " MB/s"
        ElseIf (bytes_per_second > 8191) Then
            speed_str = Format((CSng(bytes_per_second) / CSng(1024)), "#,###.00") & " KB/s"
        Else
            speed_str = Format(bytes_per_second, "#,###") & " B/s"
        End If
        Return speed_str
    End Function

    Public Shared Function CreateProgrammer(usb_dev As USB.FCUSB_DEVICE, dev As DeviceMode) As MemoryDeviceUSB
        Dim PROG_IF As MemoryDeviceUSB = Nothing
        Select Case dev
            Case DeviceMode.SPI
                PROG_IF = New SPI.SPI_Programmer(usb_dev)
            Case DeviceMode.SQI
                PROG_IF = New SPI.SQI_Programmer(usb_dev)
            Case DeviceMode.SPI_NAND
                PROG_IF = New SPINAND_Programmer(usb_dev)
            Case DeviceMode.PNOR, DeviceMode.P_EEPROM
                PROG_IF = New PARALLEL_NOR(usb_dev)
            Case DeviceMode.PNAND
                PROG_IF = New PARALLEL_NAND(usb_dev)
            Case DeviceMode.FWH
                PROG_IF = New FWH_Programmer(usb_dev)
            Case DeviceMode.EPROM
                PROG_IF = New EPROM_Programmer(usb_dev)
            Case DeviceMode.HyperFlash
                PROG_IF = New HF_Programmer(usb_dev)
            Case DeviceMode.I2C_EEPROM
                PROG_IF = New I2C_Programmer(usb_dev)
            Case DeviceMode.Microwire
                PROG_IF = New Microwire_Programmer(usb_dev)
            Case DeviceMode.ONE_WIRE
                PROG_IF = New SWI_Programmer(usb_dev)
            Case DeviceMode.SPI_EEPROM
                PROG_IF = New SPI.SPI_Programmer(usb_dev)
            Case DeviceMode.DFU
                PROG_IF = New DFU_Programmer(usb_dev)
            Case Else
                Return Nothing
        End Select
        AddHandler PROG_IF.PrintConsole, AddressOf PrintConsole
        AddHandler PROG_IF.SetProgress, AddressOf SetProgress
        Return PROG_IF
    End Function


End Class
