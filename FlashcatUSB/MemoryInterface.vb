'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2019 - ALL RIGHTS RESERVED
'CONTACT EMAIL: contact@embeddedcomputers.net
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

    Public Sub Clear(Optional usb_dev As USB.HostClient.FCUSB_DEVICE = Nothing)
        If (usb_dev Is Nothing) Then
            MyDevices.Clear() 'Remove all devices
            GUI.RemoveAllTabs()
        Else
            Dim ToRemove As New List(Of MemoryDeviceInstance)
            For Each i In MyDevices
                If i.FCUSB Is usb_dev Then ToRemove.Add(i)
            Next
            If (ToRemove.Count > 0) Then
                For Each item In ToRemove
                    GUI.RemoveTab(item)
                    MyDevices.Remove(item)
                Next
            End If
            ToRemove.Clear()
        End If
    End Sub

    Public Function GetDevices(usb_dev As USB.HostClient.FCUSB_DEVICE) As MemoryDeviceInstance()
        Try
            Dim devices_on_this_usbport As New List(Of MemoryDeviceInstance)
            For Each i In MyDevices
                If i.FCUSB Is usb_dev Then devices_on_this_usbport.Add(i)
            Next
            If devices_on_this_usbport.Count = 0 Then Return Nothing
            Return devices_on_this_usbport.ToArray
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Function Add(usb_if As USB.HostClient.FCUSB_DEVICE, mem_type As MemoryType, mem_name As String, mem_size As Long) As MemoryDeviceInstance
        Dim memDev As New MemoryDeviceInstance(usb_if)
        memDev.Name = mem_name
        memDev.Size = mem_size
        memDev.FlashType = mem_type
        MyDevices.Add(memDev)
        Return memDev
    End Function

    Public Function Add(usb_if As USB.HostClient.FCUSB_DEVICE, device As Device) As MemoryDeviceInstance
        Dim memDev As New MemoryDeviceInstance(usb_if)
        memDev.Name = device.NAME
        memDev.Size = device.FLASH_SIZE
        memDev.FlashType = device.FLASH_TYPE
        MyDevices.Add(memDev)
        Return memDev
    End Function

    Public Sub Remove(ByVal device As MemoryDeviceInstance)
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

    Public Function GetDevice(ByVal index As UInt32) As MemoryDeviceInstance
        If index >= MyDevices.Count Then Return Nothing
        Return MyDevices(index)
    End Function

    Public Class MemoryDeviceInstance
        Public WithEvents GuiControl As MemControl_v2
        Public FCUSB As USB.HostClient.FCUSB_DEVICE
        Public Property Name As String
        Public Property Size As Long 'Number of bytes of the memory device
        Public Property BaseAddress As UInt32 = 0 'Only changes for JTAG devices
        Public Property FlashType As MemoryType = MemoryType.UNSPECIFIED
        Public Property [ReadOnly] As Boolean = False 'Set to true to disable write/erase functions
        Public Property PreferredBlockSize As UInt32 = 32768
        Public Property VendorMenu As Control = Nothing
        Public Property NoErrors As Boolean = False 'Indicates there was a physical error
        Private Property IsErasing As Boolean = False
        Private Property IsBulkErasing As Boolean = False
        Private Property IsReading As Boolean = False
        Private Property IsWriting As Boolean = False
        Public Property IsTaskRunning As Boolean = False
        Public ReadOnly Property IsBusy As Boolean
            Get
                If IsErasing Or IsReading Or IsWriting Or IsBulkErasing Then Return True
                Return False
            End Get
        End Property

        Public Event PrintConsole(msg As String) 'Prints text to the console
        Public Event SetStatus(msg As String) 'Sets the status of the main gui

        Private InterfaceLock As New Object

        Sub New(ByVal usb_interface As USB.HostClient.FCUSB_DEVICE)
            Me.GuiControl = New MemControl_v2(Me)
            Me.FCUSB = usb_interface
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

        Public Function GetTypeString() As String
            Select Case Me.FlashType
                Case MemoryType.PARALLEL_NOR 'CFI devices
                    Return "Parallel NOR"
                Case MemoryType.SERIAL_NOR 'SPI devices
                    Return "SPI NOR Flash"
                Case MemoryType.SERIAL_NAND
                    Return "SPI NAND Flash"
                Case MemoryType.PARALLEL_NAND 'NAND devices
                    Return "SLC NAND Flash"
                Case MemoryType.JTAG_DMA_RAM  'RAM
                    Return "DRAM"
                Case MemoryType.JTAG_CFI
                    Return "CFI Flash"
                Case MemoryType.JTAG_SPI
                    Return "SPI Flash"
                Case MemoryType.JTAG_BSDL
                    Return "CFI Flash"
                Case MemoryType.DFU_MODE
                    Return "AVR Firmware"
                Case MemoryType.OTP_EPROM
                    Return "EPROM"
                Case MemoryType.HYPERFLASH
                    Return "HyperFlash"
                Case MemoryType.SERIAL_SWI
                    Return "SWI EEPROM"
                Case Else
                    Return ""
            End Select
        End Function

#Region "GUICONTROL EVENTS"

        Private Sub OnWriteConsole(msg_out As String) Handles GuiControl.WriteConsole
            RaiseEvent PrintConsole(msg_out)
        End Sub

        Private Sub OnSetStatus(status_text As String) Handles GuiControl.SetStatus
            RaiseEvent SetStatus(status_text)
        End Sub

        Private Sub OnSuccessfulWrite(mydev As USB.HostClient.FCUSB_DEVICE, x As MemControl_v2.XFER_Operation) Handles GuiControl.SuccessfulWrite
            If GUI IsNot Nothing Then
                GUI.SuccessfulWriteOperation(mydev, x)
            End If
        End Sub

        Private Sub OnEraseDataRequest() Handles GuiControl.EraseMemory
            Try
                Me.EraseFlash()
                Me.WaitUntilReady()
            Catch ex As Exception
            End Try
        End Sub

        Private Sub OnReadDataRequest(base_addr As Long, ByRef data() As Byte, memory_area As FlashArea) Handles GuiControl.ReadMemory
            Try
                If Me.IsBulkErasing Then
                    For i = 0 To data.Length - 1
                        data(i) = 255
                    Next
                    Exit Sub
                End If
                data = ReadBytes(base_addr, data.Length, memory_area)
            Catch ex As Exception
            End Try
        End Sub

        Private Sub OnReadStreamRequest(data_stream As IO.Stream, f_params As ReadParameters) Handles GuiControl.ReadStream
            Try
                If Me.IsBulkErasing Then
                    For i = 0 To f_params.Count - 1
                        data_stream.WriteByte(255)
                    Next
                    Exit Sub
                End If
                ReadStream(data_stream, f_params)
            Catch ex As Exception
            End Try
        End Sub

        Private Sub OnWriteRequest(addr As Long, data() As Byte, verify_wr As Boolean, memory_area As FlashArea, ByRef Success As Boolean) Handles GuiControl.WriteMemory
            Try
                Success = WriteBytes(addr, data, memory_area, verify_wr)
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

        Private Sub OnGetSectorSize(sector_int As UInt32, area As FlashArea, ByRef sector_size As UInt32) Handles GuiControl.GetSectorSize
            sector_size = Me.GetSectorSize(sector_int, area)
            If sector_size = 0 Then sector_size = Me.Size
        End Sub

        Private Sub OnGetSectorCount(ByRef count As UInt32) Handles GuiControl.GetSectorCount
            count = Me.GetSectorCount()
            If count = 0 Then count = 1
        End Sub

        Private Sub OnGetSectorIndex(addr As Long, area As FlashArea, ByRef sector_int As UInt32) Handles GuiControl.GetSectorIndex
            sector_int = 0
            Dim s_count As UInt32 = GetSectorCount()
            For i = 0 To s_count - 1
                Dim sector As SectorInfo = GetSectorInfo(i, area)
                If addr >= sector.BaseAddress AndAlso addr < (sector.BaseAddress + sector.Size) Then
                    sector_int = i
                    Exit Sub
                End If
            Next
        End Sub

        Private Sub OnGetSectorAddress(sector_int As UInt32, area As FlashArea, ByRef addr As Long) Handles GuiControl.GetSectorBaseAddress
            addr = GetSectorBaseAddress(sector_int, area)
        End Sub

        Private Sub OnGetEccLastResult(ByRef result As ECC_LIB.ECC_DECODE_RESULT) Handles GuiControl.GetEccLastResult
            Select Case Me.FlashType
                Case MemoryType.PARALLEL_NAND
                    result = FCUSB.PARALLEL_IF.ECC_LAST_RESULT
            End Select
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

        Public Function ReadBytes(base_addr As Long, count As Long, memory_area As FlashArea, Optional callback As StatusCallback = Nothing) As Byte()
            Me.NoErrors = True
            Dim data_out() As Byte = Nothing
            Using n As New IO.MemoryStream
                Dim f_params As New ReadParameters
                f_params.Address = base_addr
                f_params.Count = count
                f_params.Memory_Area = memory_area
                If callback IsNot Nothing Then
                    f_params.Status = callback
                End If
                If ReadStream(n, f_params) Then
                    data_out = n.GetBuffer()
                    ReDim Preserve data_out(n.Length - 1)
                End If
            End Using
            Return data_out
        End Function

        Public Function WriteBytes(mem_addr As Long, mem_data() As Byte, memory_area As FlashArea, verify_wr As Boolean, Optional callback As StatusCallback = Nothing) As Boolean
            Try
                Me.NoErrors = True
                Dim f_params As New WriteParameters
                f_params.Address = mem_addr
                f_params.BytesLeft = mem_data.Length
                f_params.Memory_Area = memory_area
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

        Private Function ReadStream(data_stream As IO.Stream, Params As ReadParameters) As Boolean
            Me.NoErrors = True
            If (Threading.Thread.CurrentThread.Name = "") Then
                Dim td_int As Integer = Threading.Thread.CurrentThread.ManagedThreadId
                Threading.Thread.CurrentThread.Name = "MemIf.ReadStream_" & td_int
            End If
            Try
                Dim BytesTransfered As Long = 0
                Dim BlockSize As UInt32 = Me.PreferredBlockSize
                Dim Loops As Integer = CUInt(Math.Ceiling(Params.Count / BlockSize)) 'Calcuates iterations
                Dim read_buffer() As Byte 'Temp Byte buffer
                Dim BytesRead As Long = 0 'Number of bytes read from the Flash device
                If Params.Status.UpdateOperation IsNot Nothing Then
                    Params.Status.UpdateOperation.DynamicInvoke(1) 'READ IMG
                End If
                If Params.Status.UpdateTask IsNot Nothing Then
                    Dim rd_label As String = String.Format(RM.GetString("mem_reading_memory"), Format(Params.Count, "#,###"))
                    Params.Status.UpdateTask.DynamicInvoke(rd_label)
                End If
                Dim BytesRemaining As Long = Params.Count
                For i = 1 To Loops
                    Dim BytesCountToRead As Long = BytesRemaining
                    If (BytesCountToRead > BlockSize) Then BytesCountToRead = BlockSize
                    ReDim read_buffer(BytesCountToRead - 1) 'Erase block data
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
                    read_buffer = ReadFlash(FlashAddress, BytesCountToRead, Params.Memory_Area)
                    packet_timer.Stop()
                    If Params.AbortOperation OrElse (Not Me.NoErrors) OrElse read_buffer Is Nothing Then Return False
                    BytesTransfered += BytesCountToRead
                    data_stream.Write(read_buffer, 0, BytesCountToRead)
                    BytesRead += BytesCountToRead 'Increment location address
                    BytesRemaining -= BytesCountToRead
                    If i = 1 OrElse i = Loops OrElse (i Mod 4 = 0) Then
                        Try
                            Threading.Thread.CurrentThread.Join(10) 'Pump a message
                            If Params.Status.UpdateSpeed IsNot Nothing Then
                                Dim bytes_per_second As UInt32 = Math.Round(BytesCountToRead / (packet_timer.ElapsedMilliseconds / 1000))
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

        Private Function WriteStream(data_stream As IO.Stream, params As WriteParameters) As Boolean
            Try : Me.IsTaskRunning = True
                Me.NoErrors = True
                If (Threading.Thread.CurrentThread.Name.Equals("")) Then
                    Dim td_int As Integer = Threading.Thread.CurrentThread.ManagedThreadId
                    Threading.Thread.CurrentThread.Name = "MemIf.WriteBytes_" & td_int
                End If
                If Me.ReadOnly Then Return False
                Try
                    params.Timer = New Stopwatch
                    If FlashType = MemoryType.JTAG_DMA_RAM Then
                        Return WriteBytes_Volatile(data_stream, params)
                    ElseIf FlashType = MemoryType.SERIAL_I2C Then
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

        Private Function WriteBytes_Volatile(data_stream As IO.Stream, Params As WriteParameters) As Boolean
            Params.Timer.Start()
            Dim BlockSize As Integer = 4096
            Dim BytesLeft As Integer = data_stream.Length
            Dim BytesTransfered As Integer = 0
            While (BytesLeft > 0)
                If Params.AbortOperation Then Return False
                Dim BytesThisPacket As Integer = BytesLeft
                If BytesThisPacket > BlockSize Then BytesThisPacket = BlockSize
                Dim DataOut(BytesThisPacket - 1) As Byte
                data_stream.Read(DataOut, 0, BytesThisPacket)
                Dim result As Boolean = FCUSB.JTAG_IF.WriteMemory(Params.Address + BytesTransfered, DataOut)
                If Not result Then Return False
                If Params.Status.UpdateSpeed IsNot Nothing Then
                    Dim bytes_per_second As UInt32 = Math.Round(BytesTransfered / (Params.Timer.ElapsedMilliseconds / 1000))
                    Dim speed_text As String = UpdateSpeed_GetText(bytes_per_second)
                    Params.Status.UpdateSpeed.DynamicInvoke(speed_text)
                End If
                If Params.Status.UpdatePercent IsNot Nothing Then
                    Dim percent_done As Single = CSng((BytesTransfered / Params.BytesLeft) * 100)
                    Params.Status.UpdatePercent.DynamicInvoke(CInt(percent_done))
                End If
                BytesTransfered += BytesThisPacket
                BytesLeft -= BytesThisPacket
            End While
            Return True
        End Function

        Private Function WriteBytes_EPROM(data_stream As IO.Stream, Params As WriteParameters) As Boolean
            Me.WaitUntilReady() 'Some flash devices requires us to wait before sending data
            Dim FailedAttempts As Integer = 0
            Dim ReadResult As Boolean
            Dim BlockSize As UInt32 = 8192
            While (Params.BytesLeft > 0)
                If Params.AbortOperation Then Return False
                Dim PacketSize As Integer = Params.BytesLeft
                If PacketSize > BlockSize Then PacketSize = BlockSize
                If Params.Status.UpdateBase IsNot Nothing Then Params.Status.UpdateBase.DynamicInvoke(Params.Address)
                If Params.Status.UpdateOperation IsNot Nothing Then Params.Status.UpdateOperation.DynamicInvoke(2) 'WRITE IMG
                If Params.Status.UpdateTask IsNot Nothing Then
                    Dim wr_label As String = String.Format(RM.GetString("mem_writing_memory"), Format(PacketSize, "#,###"))
                    Params.Status.UpdateTask.DynamicInvoke(wr_label)
                End If
                Dim packet_data(PacketSize - 1) As Byte
                data_stream.Read(packet_data, 0, PacketSize) 'Reads data from the stream
                Params.Timer.Start()
                Dim write_result As Boolean = FCUSB.PROGRAMMER.WriteData(Params.Address, packet_data, Params)
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
                    Application.DoEvents()
                    Utilities.Sleep(50)
                    If FlashType = MemoryType.OTP_EPROM Then
                        FCUSB.EPROM_IF.ReadData(Params.Address, BlockSize) 'Before we verify, we should read the entire block once
                    End If
                    ReadResult = WriteBytes_VerifyWrite(Params.Address, packet_data, Params.Memory_Area)
                    If ReadResult Then
                        FailedAttempts = 0
                        If Params.Status.UpdateTask IsNot Nothing Then
                            Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_okay"))
                            Application.DoEvents()
                            Utilities.Sleep(500)
                        End If
                    Else
                        FailedAttempts += 1
                        If FailedAttempts = (MySettings.VERIFY_COUNT + 1) Then
                            RaiseEvent PrintConsole(String.Format(RM.GetString("mem_verify_failed_at"), Hex(Params.Address)))
                            If Params.Status.UpdateOperation IsNot Nothing Then
                                Params.Status.UpdateOperation.DynamicInvoke(5) 'ERROR IMG
                            End If
                            If Params.Status.UpdateTask IsNot Nothing Then
                                Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_failed"))
                                Utilities.Sleep(1000)
                                Application.DoEvents()
                            End If
                            Return False
                        End If
                        Utilities.Sleep(500)
                    End If
                End If
                Params.BytesWritten += PacketSize
                Params.BytesLeft -= PacketSize
                Params.Address += PacketSize
                Dim percent_done As Single = CSng(CSng((Params.BytesWritten) / CSng(Params.BytesTotal)) * 100)
                If Params.Status.UpdateSpeed IsNot Nothing Then
                    Try
                        Dim bytes_per_second As UInt32 = Math.Round(Params.BytesWritten / (Params.Timer.ElapsedMilliseconds / 1000))
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
            Dim TotalSectors As UInt32 = GetSectorCount()
            Params.BytesTotal = Params.BytesLeft 'Total size of the data we are writing
            Dim percent_done As Single = 0
            For i As UInt32 = 0 To (TotalSectors - 1)
                Dim sector As SectorInfo = GetSectorInfo(i, Params.Memory_Area)
                Dim sector_start As UInt32 = sector.BaseAddress 'First byte of the sector
                Dim sector_end As UInt32 = sector_start + sector.Size - 1 'Last byte of the sector
                If (Params.Address >= sector_start) And (Params.Address <= sector_end) Then 'This sector contains data we want to change
                    Dim SectorData(sector.Size - 1) As Byte 'The array that will contain the sector data to write
                    Dim SectorStart As UInt32 = Params.Address - sector.BaseAddress 'This is where in the sector we are going to fill from stream
                    Dim SectorEnd As UInt32 = Math.Min(sector.Size, (SectorStart + Params.BytesLeft)) - 1  'This is where we will stop filling from stream
                    Dim StreamCount As UInt32 = (SectorEnd - SectorStart) + 1 'This is the number of bytes we are going to read for this sector
                    If (SectorStart > 0) Then 'We need to fill beginning
                        Dim data_segment() As Byte = ReadFlash(sector.BaseAddress, SectorStart, Params.Memory_Area)
                        Array.Copy(data_segment, 0, SectorData, 0, data_segment.Length)
                        Params.Address = sector.BaseAddress 'This is to adjust the base address, as we are going to write data before our starting point
                    End If
                    data_stream.Read(SectorData, SectorStart, StreamCount) 'This reads data from our stream
                    If (SectorEnd < (sector.Size - 1)) Then 'We need to fill the end
                        Dim BytesNeeded As UInt32 = sector.Size - (SectorEnd + 1)
                        WaitUntilReady()
                        Dim data_segment() As Byte = ReadFlash(sector.BaseAddress + SectorEnd + 1, BytesNeeded, Params.Memory_Area)
                        Array.Copy(data_segment, 0, SectorData, SectorEnd + 1, data_segment.Length)
                    End If
                    Dim WriteResult As Boolean = WriteBytes_EraseSectorAndWrite(i, SectorData, Math.Floor(percent_done), Params) 'Writes data
                    If Params.AbortOperation Then Return False
                    If Not Me.NoErrors Then Return False
                    Threading.Thread.CurrentThread.Join(10) 'Pump a message
                    If WriteResult Then
                        Params.BytesWritten += SectorData.Length
                        Params.BytesLeft -= StreamCount
                        Params.Address = sector.BaseAddress + sector.Size
                        percent_done = CSng(CSng((Params.BytesWritten) / CSng(Params.BytesTotal)) * 100)
                        If Params.Status.UpdateSpeed IsNot Nothing Then
                            Dim bytes_per_second As UInt32 = Math.Round(Params.BytesWritten / (Params.Timer.ElapsedMilliseconds / 1000))
                            Dim speed_text As String = UpdateSpeed_GetText(bytes_per_second)
                            Params.Status.UpdateSpeed.DynamicInvoke(speed_text)
                        End If
                        If Params.Status.UpdatePercent IsNot Nothing Then
                            Params.Status.UpdatePercent.DynamicInvoke(CInt(percent_done))
                        End If
                    Else
                        If (FlashType = MemoryType.PARALLEL_NAND) OrElse (FlashType = MemoryType.SERIAL_NAND) Then
                            If MySettings.NAND_MismatchSkip Then 'Bad block
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
                Dim TotalSize As UInt32 = Params.BytesLeft
                Dim BytesPerPacket As UInt32 = PreferredBlockSize
                Dim percent_done As Single = 0
                Dim BytesTransfered As UInt32 = 0
                Do While Params.BytesLeft > 0
                    If Params.AbortOperation Then Return False
                    Threading.Thread.CurrentThread.Join(10)
                    Dim PacketSize As UInt16 = Math.Min(Params.BytesLeft, BytesPerPacket)
                    Dim packet_data(PacketSize - 1) As Byte
                    data_stream.Read(packet_data, 0, PacketSize) 'Reads data from the stream
                    If Params.Status.UpdateTask IsNot Nothing Then
                        Dim wr_label As String = String.Format(RM.GetString("mem_writing_memory"), Format(PacketSize, "#,###"))
                        Params.Status.UpdateTask.DynamicInvoke(wr_label)
                        Application.DoEvents()
                    End If
                    If Params.Status.UpdateOperation IsNot Nothing Then
                        Params.Status.UpdateOperation.DynamicInvoke(2) 'WRITE IMG
                    End If
                    If Params.Status.UpdateBase IsNot Nothing Then
                        Params.Status.UpdateBase.DynamicInvoke(Params.Address)
                    End If
                    Params.Timer.Start()
                    Dim i2c_result As Boolean = FCUSB.I2C_IF.WriteData(Params.Address, packet_data)
                    Params.Timer.Stop()
                    If Not i2c_result Then
                        RaiseEvent PrintConsole(RM.GetString("mem_i2c_error"))
                        Return False
                    End If
                    Dim write_result As Boolean = True
                    If Params.Verify Then
                        If Params.Status.UpdateTask IsNot Nothing Then
                            Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_data"))
                            Application.DoEvents()
                        End If
                        If Params.Status.UpdateOperation IsNot Nothing Then
                            Params.Status.UpdateOperation.DynamicInvoke(3) 'VERIFY IMG
                        End If
                        write_result = WriteBytes_VerifyWrite(Params.Address, packet_data, 0)
                        If write_result Then
                            If Params.Status.UpdateTask IsNot Nothing Then
                                Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_okay"))
                                Application.DoEvents()
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
                                Application.DoEvents()
                            End If
                        End If
                    End If
                    If write_result Then
                        BytesTransfered += PacketSize
                        Params.BytesLeft -= PacketSize
                        Params.Address += PacketSize
                        percent_done = CSng(CSng((BytesTransfered) / CSng(TotalSize)) * 100)
                        If Params.Status.UpdateSpeed IsNot Nothing Then
                            Dim bytes_per_second As UInt32 = Math.Round(BytesTransfered / (Params.Timer.ElapsedMilliseconds / 1000))
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

        Private Function GetSectorInfo(sector_index As UInt32, area As Byte) As SectorInfo
            Dim si As SectorInfo
            si.BaseAddress = GetSectorBaseAddress(sector_index, area)
            si.Size = GetSectorSize(sector_index, area)
            Return si
        End Function

        Private Structure SectorInfo
            Dim BaseAddress As UInt32
            Dim Size As UInt32
        End Structure
        'Does the actual erase sector and program functions
        Private Function WriteBytes_EraseSectorAndWrite(ByRef sector As UInt32, data() As Byte, Percent As Integer, Params As WriteParameters) As Boolean
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
                        EraseSector(sector, Params.Memory_Area)
                        If Not Me.NoErrors Then
                            RaiseEvent PrintConsole("Failed to erase memory at address: 0x" & Hex(Params.Address).PadLeft(8, "0"))
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
                        Application.DoEvents()
                        If (Me.FlashType = MemoryType.PARALLEL_NOR) Then
                            Utilities.Sleep(200) 'Some older devices need a delay here after writing data (such as AM29F040B)
                        End If
                        ReadResult = WriteBytes_VerifyWrite(Params.Address, data, Params.Memory_Area)
                        If Params.AbortOperation Then Return False
                        If Not Me.NoErrors Then Return False
                        If ReadResult Then
                            FailedAttempts = 0
                            If Params.Status.UpdateTask IsNot Nothing Then
                                Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_okay"))
                            End If
                        Else
                            FailedAttempts += 1
                            If FailedAttempts = (MySettings.VERIFY_COUNT + 1) Then
                                If (FlashType = FlashMemory.MemoryType.PARALLEL_NAND) Then
                                    Dim n_dev As P_NAND = DirectCast(FCUSB.PARALLEL_IF.MyFlashDevice, P_NAND)
                                    Dim pages_per_block As UInt32 = (n_dev.BLOCK_SIZE / n_dev.PAGE_SIZE)
                                    Dim page_addr As UInt32 = GetNandPageAddress(n_dev, Params.Address, Params.Memory_Area)
                                    Dim block_addr As UInt32 = Math.Floor(page_addr / pages_per_block)
                                    RaiseEvent PrintConsole(String.Format(RM.GetString("mem_bad_nand_block"), Hex(page_addr).PadLeft(6, "0"), block_addr))
                                    Return False
                                ElseIf (FlashType = FlashMemory.MemoryType.SERIAL_NAND) Then
                                    Dim n_dev As SPI_NAND = DirectCast(FCUSB.SPI_NAND_IF.MyFlashDevice, SPI_NAND)
                                    Dim pages_per_block As UInt32 = (n_dev.BLOCK_SIZE / n_dev.PAGE_SIZE)
                                    Dim page_addr As UInt32 = GetNandPageAddress(n_dev, Params.Address, Params.Memory_Area)
                                    Dim block_addr As UInt32 = Math.Floor(page_addr / pages_per_block)
                                    RaiseEvent PrintConsole(String.Format(RM.GetString("mem_bad_nand_block"), Hex(page_addr).PadLeft(6, "0"), block_addr))
                                    Return False
                                ElseIf (FlashType = FlashMemory.MemoryType.PARALLEL_NOR) AndAlso (FCUSB.PARALLEL_IF.MyFlashDevice.GetType Is GetType(OTP_EPROM)) Then
                                    RaiseEvent PrintConsole(String.Format(RM.GetString("mem_verify_failed_at"), Hex(Params.Address)))
                                    If Params.Status.UpdateOperation IsNot Nothing Then
                                        Params.Status.UpdateOperation.DynamicInvoke(5) 'ERROR IMG
                                    End If
                                    If Params.Status.UpdateTask IsNot Nothing Then
                                        Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_failed"))
                                        Utilities.Sleep(1000)
                                    End If
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
                                    Return WriteErrorOnVerifyWrite(Params.Address)
                                End If
                            End If
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

        Private Function WriteBytes_VerifyWrite(BaseAddress As Long, Data() As Byte, memory_area As Byte) As Boolean
            Dim Verify() As Byte 'The data to check against
            Dim MiscountCounter As Integer = 0
            Dim FirstWrongByteIs As Byte = Nothing
            Dim FirstWrongAddr As Integer = 0
            Dim FirstWrongByteShould As Byte = 0
            WaitUntilReady()
            Verify = ReadFlash(BaseAddress, Data.Length, memory_area)
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

        Public Function WriteErrorOnVerifyWrite(address As UInt32) As Boolean
            If (GUI IsNot Nothing) Then
                Dim TitleTxt As String = String.Format(RM.GetString("mem_verify_failed_at"), Hex(address).PadLeft(8, "0"))
                TitleTxt &= vbCrLf & vbCrLf & RM.GetString("mem_ask_continue")
                If MsgBox(TitleTxt, MsgBoxStyle.YesNo, RM.GetString("mem_verify_failed_title")) = MsgBoxResult.No Then
                    Return False 'Stop operation
                Else
                    Return True
                End If
            Else
                RaiseEvent PrintConsole(String.Format(RM.GetString("mem_verify_failed_at"), Hex(address).PadLeft(8, "0")))
                Return False 'Stop console operation
            End If
        End Function

        Public Function GetMessageBoxForSectorErase(address As UInt32, sector_index As Integer) As Boolean
            Dim TitleTxt As String = String.Format(RM.GetString("mem_erase_failed_at"), Hex(address).PadLeft(8, "0"), sector_index)
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
                Case MemoryType.JTAG_BSDL
                    FCUSB.JTAG_IF.BoundaryScan_WaitForReady()
                Case Else
                    FCUSB.PROGRAMMER.WaitUntilReady()
            End Select
        End Sub

        Public Sub ReadMode()
            Select Case Me.FlashType
                Case MemoryType.PARALLEL_NOR
                    FCUSB.PARALLEL_IF.ResetDevice()
                Case MemoryType.JTAG_CFI
                    FCUSB.JTAG_IF.CFI_ReadMode()
            End Select
        End Sub

        Public Function ReadFlash(Address As Long, Count As UInt32, memory_area As FlashArea) As Byte()
            If Not WaitForNotBusy() Then Return Nothing
            Try : Me.IsReading = True
                Dim data_out() As Byte = Nothing
                Dim offset As Integer = BitSwap_Offset()
                Dim data_read_count As UInt32 = Count
                Dim data_offset As Long = Address
                Dim align As Long = 0
                If (offset > 0) Then align = Address Mod offset
                If (align > 0) Then
                    data_offset -= align
                    data_read_count += align
                    Do Until data_read_count Mod offset = 0
                        data_read_count += 1
                    Loop
                End If
                Try : Threading.Monitor.Enter(InterfaceLock)
                    Select Case Me.FlashType
                        Case MemoryType.JTAG_DMA_RAM
                            data_out = FCUSB.JTAG_IF.ReadMemory(CUInt(Me.BaseAddress + data_offset), data_read_count)
                        Case MemoryType.JTAG_CFI
                            data_out = FCUSB.JTAG_IF.CFI_ReadFlash(data_offset, data_read_count)
                        Case MemoryType.JTAG_SPI
                            data_out = FCUSB.JTAG_IF.SPI_ReadFlash(data_offset, data_read_count)
                        Case MemoryType.JTAG_BSDL
                            data_out = FCUSB.JTAG_IF.BoundaryScan_ReadFlash(data_offset, data_read_count)
                        Case Else
                            data_out = FCUSB.PROGRAMMER.ReadData(data_offset, data_read_count, memory_area)
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
                Application.DoEvents()
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
                    Case MemoryType.JTAG_BSDL
                        FCUSB.JTAG_IF.BoundaryScan_EraseDevice()
                    Case Else
                        FCUSB.PROGRAMMER.EraseDevice()
                End Select
            Finally
                Threading.Monitor.Exit(InterfaceLock)
                Me.IsBulkErasing = False
            End Try
            Application.DoEvents()
            Return True
        End Function

        Public Sub EraseSector(sector_index As UInt32, Optional area As FlashArea = FlashArea.NotSpecified)
            If Not WaitForNotBusy() Then Exit Sub
            Me.IsErasing = True
            Try : Threading.Monitor.Enter(InterfaceLock)
                Select Case Me.FlashType
                    Case MemoryType.JTAG_CFI
                        Me.NoErrors = FCUSB.JTAG_IF.CFI_Sector_Erase(sector_index)
                    Case MemoryType.JTAG_SPI
                        Me.NoErrors = FCUSB.JTAG_IF.SPI_SectorErase(sector_index)
                    Case MemoryType.JTAG_BSDL
                        Me.NoErrors = FCUSB.JTAG_IF.BoundaryScan_SectorErase(sector_index)
                    Case Else
                        Me.NoErrors = FCUSB.PROGRAMMER.SectorErase(sector_index, area)
                End Select
            Finally
                Threading.Monitor.Exit(InterfaceLock)
            End Try
            Application.DoEvents()
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
                    Case MemoryType.JTAG_BSDL
                        Me.NoErrors = FCUSB.JTAG_IF.BoundaryScan_SectorWrite(sector_index, DataToWrite)
                    Case Else
                        Me.NoErrors = FCUSB.PROGRAMMER.SectorWrite(sector_index, DataToWrite, Params)
                End Select
                If Params IsNot Nothing Then Params.Timer.Stop()
            Finally
                Threading.Monitor.Exit(InterfaceLock)
            End Try
            Me.IsWriting = False
            Application.DoEvents()
        End Sub

        Public Function GetSectorCount() As UInt32
            Select Case Me.FlashType
                Case MemoryType.JTAG_CFI
                    Return FCUSB.JTAG_IF.CFI_SectorCount()
                Case MemoryType.JTAG_SPI
                    Return FCUSB.JTAG_IF.SPI_SectorCount()
                Case MemoryType.JTAG_BSDL
                    Return FCUSB.JTAG_IF.BoundaryScan_SectorCount()
                Case Else
                    Return FCUSB.PROGRAMMER.SectorCount()
            End Select
        End Function

        Public Function GetSectorSize(sector_index As UInt32, area As FlashArea) As UInt32
            Select Case Me.FlashType
                Case MemoryType.JTAG_CFI
                    Return FCUSB.JTAG_IF.CFI_GetSectorSize(sector_index)
                Case MemoryType.JTAG_SPI
                    Return FCUSB.JTAG_IF.SPI_GetSectorSize(sector_index)
                Case MemoryType.JTAG_BSDL
                    Return FCUSB.JTAG_IF.BoundaryScan_GetSectorSize(sector_index)
                Case Else
                    Return FCUSB.PROGRAMMER.SectorSize(sector_index, area)
            End Select
        End Function

        Public Function GetSectorBaseAddress(sector_index As UInt32, area As FlashArea) As Long
            Select Case Me.FlashType
                Case MemoryType.JTAG_CFI
                    Return FCUSB.JTAG_IF.CFI_FindSectorBase(sector_index)
                Case MemoryType.JTAG_SPI
                    Return FCUSB.JTAG_IF.CFI_FindSectorBase(sector_index)
                Case MemoryType.JTAG_BSDL
                    Return FCUSB.JTAG_IF.BoundaryScan_SectorFind(sector_index)
                Case Else
                    Return FCUSB.PROGRAMMER.SectorFind(sector_index, area)
            End Select
        End Function

#End Region

    End Class

    Public Sub DisabledControls(Optional show_cancel As Boolean = False)
        For Each device In MyDevices
            device.DisableGuiControls(show_cancel)
        Next
    End Sub

    Public Sub EnableControls()
        For Each device In MyDevices
            device.EnableGuiControls()
        Next
    End Sub

    Public Sub AbortOperations()
        Try
            Dim Counter As UInt16 = 0
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

    Private Shared Function UpdateSpeed_GetText(bytes_per_second As Integer) As String
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
