'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2018 - ALL RIGHTS RESERVED
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

    Public Function GetDevices(ByVal usb_dev As USB.HostClient.FCUSB_DEVICE) As MemoryDeviceInstance()
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

    Public Function Add(ByVal usb_if As USB.HostClient.FCUSB_DEVICE, ByVal mem_type As MemoryType, ByVal mem_name As String, ByVal mem_size As UInt32) As MemoryDeviceInstance
        Dim memDev As New MemoryDeviceInstance(usb_if)
        memDev.Name = mem_name
        memDev.Size = mem_size
        memDev.FlashType = mem_type
        MyDevices.Add(memDev)
        Return memDev
    End Function

    Public Function Add(ByVal usb_if As USB.HostClient.FCUSB_DEVICE, device As Device) As MemoryDeviceInstance
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
        Public Property Size As UInt32 'Number of bytes of the memory device
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

        Public Event PrintConsole(ByVal msg As String) 'Prints text to the console
        Public Event SetStatus(ByVal msg As String) 'Sets the status of the main gui

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
                Case MemoryType.SLC_NAND 'NAND devices
                    Return "SLC NAND Flash"
                Case MemoryType.JTAG_DMA_RAM  'RAM
                    Return "DRAM"
                Case MemoryType.JTAG_DMA_CFI
                    Return "CFI Flash"
                Case MemoryType.JTAG_SPI
                    Return "SPI Flash"
                Case MemoryType.DFU_MODE
                    Return "DFU Mode"
                Case Else
                    Return ""
            End Select
        End Function

#Region "GUICONTROL EVENTS"

        Private Sub OnWriteConsole(ByVal msg_out As String) Handles GuiControl.WriteConsole
            RaiseEvent PrintConsole(msg_out)
        End Sub

        Private Sub OnSetStatus(ByVal status_text As String) Handles GuiControl.SetStatus
            RaiseEvent SetStatus(status_text)
        End Sub

        Private Sub OnSuccessfulWrite(ByVal mydev As USB.HostClient.FCUSB_DEVICE, ByVal x As MemControl_v2.XFER_Operation) Handles GuiControl.SuccessfulWrite
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

        Private Sub OnReadDataRequest(ByVal base_addr As Long, ByRef data() As Byte, ByVal memory_area As FlashArea) Handles GuiControl.ReadMemory
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

        Private Sub OnReadStreamRequest(ByVal data_stream As IO.Stream, ByRef f_params As ReadParameters) Handles GuiControl.ReadStream
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

        Private Sub OnWriteRequest(ByVal addr As Long, ByVal data() As Byte, ByVal memory_area As FlashArea, ByRef Success As Boolean) Handles GuiControl.WriteMemory
            Try
                Success = WriteBytes(addr, data, memory_area)
            Catch ex As Exception
            End Try
        End Sub

        Private Sub OnWriteStreamRequest(ByVal data_stream As IO.Stream, ByRef f_params As WriteParameters, ByRef Success As Boolean) Handles GuiControl.WriteStream
            Try
                Success = WriteStream(data_stream, f_params)
                Me.WaitUntilReady()
            Catch ex As Exception
            End Try
        End Sub

        Private Sub OnGetSectorSize(ByVal sector_int As UInt32, ByVal area As FlashArea, ByRef sector_size As UInt32) Handles GuiControl.GetSectorSize
            sector_size = Me.GetSectorSize(sector_int, area)
            If sector_size = 0 Then sector_size = Me.Size
        End Sub

        Private Sub OnGetSectorCount(ByRef count As UInt32) Handles GuiControl.GetSectorCount
            count = Me.GetSectorCount()
            If count = 0 Then count = 1
        End Sub

        Private Sub OnGetEccLastResult(ByRef result As ECC_LIB.decode_result) Handles GuiControl.GetEccLastResult
            Select Case Me.FlashType
                Case MemoryType.SLC_NAND
                    result = FCUSB.EXT_IF.ECC_LAST_RESULT
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

        Public Function ReadBytes(ByVal base_addr As UInt32, ByVal count As UInt32, memory_area As FlashArea, Optional callback As StatusCallback = Nothing) As Byte()
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

        Public Function WriteBytes(ByVal Address As UInt32, ByVal Data() As Byte, ByVal memory_area As FlashArea, Optional callback As StatusCallback = Nothing) As Boolean
            Try
                Me.NoErrors = True
                Dim f_params As New WriteParameters
                f_params.Address = Address
                f_params.Count = Data.Length
                f_params.Memory_Area = memory_area
                f_params.Verify = MySettings.VERIFY_WRITE
                If callback IsNot Nothing Then
                    f_params.Status = callback
                End If
                Using n As New IO.MemoryStream(Data)
                    Return WriteStream(n, f_params)
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Function ReadStream(ByVal data_stream As IO.Stream, ByRef Params As ReadParameters) As Boolean
            Me.NoErrors = True
            If (Threading.Thread.CurrentThread.Name = "") Then
                Dim td_int As Integer = Threading.Thread.CurrentThread.ManagedThreadId
                Threading.Thread.CurrentThread.Name = "MemIf.ReadStream_" & td_int
            End If
            Try
                Dim BytesTransfered As UInt32 = 0
                Dim BlockSize As UInt32 = Me.PreferredBlockSize
                Dim Loops As Integer = CUInt(Math.Ceiling(Params.Count / BlockSize)) 'Calcuates iterations
                Dim b() As Byte 'Temp Byte buffer
                Dim BytesRead As UInt32 = 0 'Number of bytes read from the Flash device
                If Params.Status.UpdateOperation IsNot Nothing Then
                    Params.Status.UpdateOperation.DynamicInvoke(1) 'READ IMG
                End If
                If Params.Status.UpdateTask IsNot Nothing Then
                    Dim rd_label As String = String.Format(RM.GetString("mem_reading_memory"), Format(Params.Count, "#,###"))
                    Params.Status.UpdateTask.DynamicInvoke(rd_label)
                End If
                Params.Timer = New Stopwatch
                Params.Timer.Start()
                Dim BytesRemaining As Integer = Params.Count
                For i = 1 To Loops
                    Dim BytesCountToRead As UInt32 = BytesRemaining
                    If (BytesCountToRead > BlockSize) Then BytesCountToRead = BlockSize
                    ReDim b(BytesCountToRead - 1) 'Erase block data
                    Dim FlashAddress As UInt32 = Params.Address + BytesRead
                    If Params.Status.UpdateBase IsNot Nothing Then
                        Params.Status.UpdateBase.DynamicInvoke(FlashAddress)
                    End If
                    If Params.Status.UpdatePercent IsNot Nothing Then
                        Dim percent_done As Single = CSng((i / Loops) * 100) 'Calulate % done
                        Params.Status.UpdatePercent.DynamicInvoke(CInt(percent_done))
                    End If
                    b = ReadFlash(FlashAddress, BytesCountToRead, Params.Memory_Area)
                    If Params.AbortOperation Then Return False
                    If Not Me.NoErrors Then Return False
                    If b Is Nothing Then Return False 'ERROR
                    BytesTransfered += BytesCountToRead
                    data_stream.Write(b, 0, BytesCountToRead)
                    BytesRead += BytesCountToRead 'Increment location address
                    BytesRemaining -= BytesCountToRead
                    If i = 1 OrElse i = Loops OrElse (i Mod 4 = 0) Then
                        Params.Timer.Stop()
                        Try
                            Threading.Thread.CurrentThread.Join(10) 'Pump a message
                            If Params.Status.UpdateSpeed IsNot Nothing Then
                                Dim speed_str As String = Format(Math.Round(BytesRead / (Params.Timer.ElapsedMilliseconds / 1000)), "#,###") & " Bytes/s"
                                Params.Status.UpdateSpeed.DynamicInvoke(speed_str)
                            End If
                            data_stream.Flush()
                        Catch ex As Exception
                        Finally
                            Params.Timer.Start()
                        End Try
                    End If
                Next
                Return True
            Catch ex As Exception
                RaiseEvent PrintConsole("Error in ReadStream")
            Finally
                Params.Timer.Stop()
            End Try
            Return False
        End Function

        Private Function WriteStream(ByVal data_stream As IO.Stream, ByVal params As WriteParameters) As Boolean
            Try : Me.IsTaskRunning = True
                Me.NoErrors = True
                If (Threading.Thread.CurrentThread.Name = "") Then
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

        Private Function WriteBytes_Volatile(ByVal data_stream As IO.Stream, ByVal Params As WriteParameters) As Boolean
            Dim BlockSize As Integer = 4096
            If Not FCUSB.EJ_IF.TargetDevice.DMA_SUPPORTED Then BlockSize = 1024
            Dim BytesLeft As Integer = data_stream.Length
            Dim BytesTransfered As Integer = 0
            While (BytesLeft > 0)
                If Params.AbortOperation Then Return False
                Dim BytesThisPacket As Integer = BytesLeft
                If BytesThisPacket > BlockSize Then BytesThisPacket = BlockSize
                Dim DataOut(BytesThisPacket - 1) As Byte
                data_stream.Read(DataOut, 0, BytesThisPacket)
                Dim result As Boolean = FCUSB.EJ_IF.WriteMemory(Params.Address + BytesTransfered, DataOut)
                If Not result Then Return False
                If Params.Status.UpdateSpeed IsNot Nothing Then
                    Params.Status.UpdateSpeed.DynamicInvoke(Format(Math.Round(BytesTransfered / (Params.Timer.ElapsedMilliseconds / 1000)), "#,###") & " Bytes/s")
                End If
                If Params.Status.UpdatePercent IsNot Nothing Then
                    Dim percent_done As Single = CSng((BytesTransfered / Params.Count) * 100)
                    Params.Status.UpdatePercent.DynamicInvoke(CInt(percent_done))
                End If
                BytesTransfered += BytesThisPacket
                BytesLeft -= BytesThisPacket
            End While
            Return True
        End Function

        Private Function WriteBytes_NonVolatile(ByVal data_stream As IO.Stream, ByRef Params As WriteParameters) As Boolean
            Me.WaitUntilReady() 'Some flash devices requires us to wait before sending data
            Dim BytesTransfered As UInt32 = 0
            Dim TotalSectors As UInt32 = GetSectorCount()
            Dim TotalSize As UInt32 = Params.Count 'Total size of the data we are writing
            Dim percent_done As Single = 0
            For i As UInt32 = 0 To (TotalSectors - 1)
                Dim sector As SectorInfo = GetSectorInfo(i, Params.Memory_Area)
                Dim sector_start As UInt32 = sector.BaseAddress 'First byte of the sector
                Dim sector_end As UInt32 = sector_start + sector.Size - 1 'Last byte of the sector
                If (Params.Address >= sector_start) And (Params.Address <= sector_end) Then 'This sector contains data we want to change
                    Dim SectorData(sector.Size - 1) As Byte 'The array that will contain the sector data to write
                    Dim SectorStart As UInt32 = Params.Address - sector.BaseAddress 'This is where in the sector we are going to fill from stream
                    Dim SectorEnd As UInt32 = Math.Min(sector.Size, (SectorStart + Params.Count)) - 1  'This is where we will stop filling from stream
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
                    Dim WriteResult As Boolean = WriteDataSub(i, SectorData, Math.Floor(percent_done), Params) 'Writes data
                    If Params.AbortOperation Then Return False
                    If Not Me.NoErrors Then Return False
                    Threading.Thread.CurrentThread.Join(10) 'Pump a message
                    If WriteResult Then
                        BytesTransfered += SectorData.Length
                        Params.Count -= StreamCount
                        Params.Address = sector.BaseAddress + sector.Size
                        percent_done = CSng(CSng((BytesTransfered) / CSng(TotalSize)) * 100)
                        If Params.Status.UpdateSpeed IsNot Nothing Then
                            Dim speed_str As String = Format(Math.Round(BytesTransfered / (Params.Timer.ElapsedMilliseconds / 1000)), "#,###") & " Bytes/s"
                            Params.Status.UpdateSpeed.DynamicInvoke(speed_str)
                        End If
                        If Params.Status.UpdatePercent IsNot Nothing Then
                            Params.Status.UpdatePercent.DynamicInvoke(CInt(percent_done))
                        End If
                    Else
                        If (FlashType = MemoryType.SLC_NAND) OrElse (FlashType = MemoryType.SERIAL_NAND) Then
                            If MySettings.NAND_MismatchSkip Then 'Bad block
                                If (i = TotalSectors - 1) Then Return False 'No more blocks to write
                                data_stream.Position -= SectorData.Length 'We are going to re-write these bytes to the next block
                                Params.Address += SectorData.Length 'and to this base address
                            Else
                                Return False
                            End If
                        Else
                            Return False
                        End If
                    End If
                    If Params.Count = 0 Then data_stream.Dispose() : Exit For
                End If
            Next
            Return True 'Operation was successful
        End Function

        Private Function WriteBytes_I2C(ByVal data_stream As IO.Stream, ByRef Params As WriteParameters) As Boolean
            Try
                Dim TotalSize As UInt32 = Params.Count
                Dim BytesPerPacket As UInt32 = PreferredBlockSize
                Dim percent_done As Single = 0
                Dim BytesTransfered As UInt32 = 0
                Do While Params.Count > 0
                    If Params.AbortOperation Then Return False
                    Threading.Thread.CurrentThread.Join(10)
                    Dim PacketSize As UInt16 = Math.Min(Params.Count, BytesPerPacket)
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
                        write_result = VerifyDataSub(Params.Address, packet_data, 0)
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
                        Params.Count -= PacketSize
                        Params.Address += PacketSize
                        percent_done = CSng(CSng((BytesTransfered) / CSng(TotalSize)) * 100)
                        If Params.Status.UpdateSpeed IsNot Nothing Then
                            Dim speed_str As String = Format(Math.Round(BytesTransfered / (Params.Timer.ElapsedMilliseconds / 1000)), "#,###") & " Bytes/s"
                            Params.Status.UpdateSpeed.DynamicInvoke(speed_str)
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

        Private Function GetSectorInfo(ByVal sector_index As UInt32, ByVal area As Byte) As SectorInfo
            Dim si As SectorInfo
            si.BaseAddress = GetSectorBase(sector_index, area)
            si.Size = GetSectorSize(sector_index, area)
            Return si
        End Function

        Private Structure SectorInfo
            Dim BaseAddress As UInt32
            Dim Size As UInt32
        End Structure

        'Does the actual erase sector and program functions
        Private Function WriteDataSub(ByRef sector As UInt32, ByVal data() As Byte, ByVal Percent As Integer, ByRef Params As WriteParameters) As Boolean
            Try
                Dim FailedAttempts As Integer = 0
                Dim ReadResult As Boolean
                Do
                    If Params.Status.UpdateBase IsNot Nothing Then
                        Params.Status.UpdateBase.DynamicInvoke(Params.Address)
                    End If
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
                    WriteSector(sector, data, Params.Memory_Area, Params.Timer)
                    If Params.AbortOperation Then Return False
                    If Not Me.NoErrors Then Return False
                    If Params.Verify Then 'Verify is enabled and we are monitoring this
                        If Params.Status.UpdateOperation IsNot Nothing Then
                            Params.Status.UpdateOperation.DynamicInvoke(3) 'VERIFY IMG
                        End If
                        If Params.Status.UpdateTask IsNot Nothing Then
                            Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_data"))
                        End If
                        ReadResult = VerifyDataSub(Params.Address, data, Params.Memory_Area)
                        If Params.AbortOperation Then Return False
                        If Not Me.NoErrors Then Return False
                        If ReadResult Then
                            FailedAttempts = 0
                            If Params.Status.UpdateTask IsNot Nothing Then
                                Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_okay"))
                            End If
                        Else
                            FailedAttempts = FailedAttempts + 1
                            If FailedAttempts = 3 Then
                                If (FlashType = FlashMemory.MemoryType.SLC_NAND) Then
                                    Dim n_dev As NAND_Flash = DirectCast(FCUSB.EXT_IF.MyFlashDevice, NAND_Flash)
                                    Dim pages_per_block As UInt32 = (n_dev.BLOCK_SIZE / n_dev.PAGE_SIZE)
                                    Dim page_addr As UInt32 = GetNandPageAddress(n_dev, Params.Address, Params.Memory_Area)
                                    Dim block_addr As UInt32 = Math.Floor(page_addr / pages_per_block)
                                    RaiseEvent PrintConsole(String.Format(RM.GetString("mem_bad_nand_block"), Hex(page_addr).PadLeft(6, "0"), block_addr))
                                    Return False
                                ElseIf (FlashType = FlashMemory.MemoryType.SERIAL_NAND) Then
                                    Dim n_dev As SPI_NAND_Flash = DirectCast(FCUSB.EXT_IF.MyFlashDevice, SPI_NAND_Flash)
                                    Dim pages_per_block As UInt32 = (n_dev.BLOCK_SIZE / n_dev.PAGE_SIZE)
                                    Dim page_addr As UInt32 = GetNandPageAddress(n_dev, Params.Address, Params.Memory_Area)
                                    Dim block_addr As UInt32 = Math.Floor(page_addr / pages_per_block)
                                    RaiseEvent PrintConsole(String.Format(RM.GetString("mem_bad_nand_block"), Hex(page_addr).PadLeft(6, "0"), block_addr))
                                    Return False
                                Else
                                    RaiseEvent PrintConsole(String.Format(RM.GetString("mem_verify_failed_at"), "0x" & Hex(Params.Address)))
                                    If Params.Status.UpdateOperation IsNot Nothing Then
                                        Params.Status.UpdateOperation.DynamicInvoke(5) 'ERROR IMG
                                    End If
                                    If Params.Status.UpdateTask IsNot Nothing Then
                                        Params.Status.UpdateTask.DynamicInvoke(RM.GetString("mem_verify_failed"))
                                        Utilities.Sleep(1000)
                                    End If
                                    Return GetMessageBoxForVerifyFailed(Params.Address)
                                End If
                            End If
                            Utilities.Sleep(500)
                        End If
                    Else
                        ReadResult = True 'We are skiping verification
                    End If
                Loop Until ReadResult Or (Not MySettings.VERIFY_WRITE)
            Catch ex As Exception
            End Try
            Return True
        End Function

        Private Function VerifyDataSub(ByVal BaseAddress As UInt32, ByVal Data() As Byte, ByVal memory_area As Byte) As Boolean
            Dim Verify() As Byte 'The data to check against
            Dim MiscountCounter As Integer = 0
            Dim FirstWrongByte As Byte = Nothing
            Dim FirstWrongAddr As Integer = 0
            WaitUntilReady()
            Verify = ReadFlash(BaseAddress, Data.Length, memory_area)
            If Verify Is Nothing OrElse (Not Verify.Length = Data.Length) Then Return False
            For i As Integer = 0 To Data.Length - 1
                If Not Data(i) = Verify(i) Then
                    If MiscountCounter = 0 Then
                        FirstWrongByte = Verify(i)
                        FirstWrongAddr = CInt(BaseAddress + i)
                    End If
                    MiscountCounter = MiscountCounter + 1
                End If
            Next
            If MiscountCounter = 0 Then 'Verification successful
                Return True
            Else 'Error!
                Dim DataShouldBe As Byte = Data(CUInt(FirstWrongAddr - BaseAddress))
                RaiseEvent PrintConsole(String.Format(RM.GetString("mem_verify_mismatches"), "0x" & Hex(FirstWrongAddr), "0x" & Hex(DataShouldBe), "0x" & Hex(FirstWrongByte), MiscountCounter))
                Return False 'Error!
            End If
        End Function

        Public Function GetMessageBoxForVerifyFailed(ByVal address As UInt32) As Boolean
            Dim TitleTxt As String = String.Format(RM.GetString("mem_verify_failed_at"), Hex(address).PadLeft(8, "0"))
            TitleTxt &= vbCrLf & vbCrLf & RM.GetString("mem_ask_continue")
            If MsgBox(TitleTxt, MsgBoxStyle.YesNo, RM.GetString("mem_verify_failed_title")) = MsgBoxResult.No Then
                Return False 'Stop working
            Else
                Return True
            End If
        End Function

        Public Function GetMessageBoxForSectorErase(ByVal address As UInt32, ByVal sector_index As Integer) As Boolean
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
                Case MemoryType.SERIAL_NOR
                    FCUSB.SPI_NOR_IF.WaitUntilReady()
                Case MemoryType.SERIAL_NAND
                    FCUSB.SPI_NAND_IF.WaitUntilReady()
                Case MemoryType.PARALLEL_NOR
                    FCUSB.EXT_IF.WaitForReady()
                Case MemoryType.SLC_NAND
                    FCUSB.EXT_IF.WaitForReady()
                Case MemoryType.JTAG_DMA_CFI
                    FCUSB.EJ_IF.CFI_WaitUntilReady()
                Case MemoryType.JTAG_SPI
                    FCUSB.EJ_IF.SPI_WaitUntilReady()
                Case Else 'Including I2C
                    Utilities.Sleep(100)
            End Select
        End Sub

        Public Sub ReadMode()
            Select Case Me.FlashType
                Case MemoryType.SERIAL_NOR 'SPI
                Case MemoryType.SERIAL_NAND
                Case MemoryType.SERIAL_I2C 'I2C
                Case MemoryType.PARALLEL_NOR
                    FCUSB.EXT_IF.ResetDevice()
                Case MemoryType.JTAG_DMA_CFI
                    FCUSB.EJ_IF.CFI_ReadMode()
                Case MemoryType.JTAG_SPI
            End Select
        End Sub

        Public Function ReadFlash(ByVal Address As UInt32, ByVal Count As UInt32, ByVal memory_area As FlashArea) As Byte()
            If Not WaitForNotBusy() Then Return Nothing
            Try : Me.IsReading = True
                Dim data_out() As Byte = Nothing
                Dim offset As Integer = BitSwap_Offset()
                Dim data_read_count As UInt32 = Count
                Dim data_offset As UInt32 = Address
                Dim align As UInt32 = 0
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
                        Case MemoryType.SERIAL_NOR
                            data_out = FCUSB.SPI_NOR_IF.ReadData(data_offset, data_read_count, memory_area)
                        Case MemoryType.SERIAL_NAND
                            data_out = FCUSB.SPI_NAND_IF.ReadData(data_offset, data_read_count, memory_area)
                        Case MemoryType.PARALLEL_NOR
                            data_out = FCUSB.EXT_IF.ReadData(data_offset, data_read_count, memory_area)
                        Case MemoryType.SLC_NAND
                            data_out = FCUSB.EXT_IF.ReadData(data_offset, data_read_count, memory_area)
                        Case MemoryType.SERIAL_I2C
                            data_out = FCUSB.I2C_IF.ReadData(data_offset, data_read_count)
                        Case MemoryType.JTAG_DMA_RAM
                            data_out = FCUSB.EJ_IF.ReadMemory(Me.BaseAddress + data_offset, data_read_count)
                        Case MemoryType.JTAG_DMA_CFI
                            data_out = FCUSB.EJ_IF.CFI_ReadFlash(data_offset, data_read_count)
                        Case MemoryType.JTAG_SPI
                            data_out = FCUSB.EJ_IF.SPI_ReadFlash(data_offset, data_read_count)
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
            Try : Me.IsBulkErasing = True
                Threading.Monitor.Enter(InterfaceLock)
                Select Case Me.FlashType
                    Case MemoryType.SERIAL_NOR 'SPI
                        Return FCUSB.SPI_NOR_IF.EraseDevice()
                    Case MemoryType.SERIAL_NAND
                        Return FCUSB.SPI_NAND_IF.EraseDevice
                    Case MemoryType.PARALLEL_NOR
                        Return FCUSB.EXT_IF.EraseDevice()
                    Case MemoryType.SLC_NAND
                        Return FCUSB.EXT_IF.EraseDevice()
                    Case MemoryType.JTAG_DMA_CFI
                        FCUSB.EJ_IF.CFI_EraseDevice()
                    Case MemoryType.JTAG_SPI
                        FCUSB.EJ_IF.SPI_EraseBulk()
                    Case MemoryType.DFU_MODE
                        Return True 'Not supported
                End Select
                Return True
            Finally
                Threading.Monitor.Exit(InterfaceLock)
                Me.IsBulkErasing = False
                Application.DoEvents()
            End Try
        End Function

        Public Sub EraseSector(ByVal sector_index As UInt32, Optional area As FlashArea = FlashArea.NotSpecified)
            If Not WaitForNotBusy() Then Exit Sub
            Try : Me.IsErasing = True
                Threading.Monitor.Enter(InterfaceLock)
                Select Case Me.FlashType
                    Case MemoryType.SERIAL_NOR 'SPI
                        Me.NoErrors = FCUSB.SPI_NOR_IF.Sector_Erase(sector_index, area)
                    Case MemoryType.SERIAL_NAND
                        Me.NoErrors = FCUSB.SPI_NAND_IF.Sector_Erase(sector_index, area)
                    Case MemoryType.PARALLEL_NOR
                        Me.NoErrors = FCUSB.EXT_IF.Sector_Erase(sector_index, area)
                    Case MemoryType.SLC_NAND
                        Me.NoErrors = FCUSB.EXT_IF.Sector_Erase(sector_index, area)
                    Case MemoryType.JTAG_DMA_CFI
                        Me.NoErrors = FCUSB.EJ_IF.CFI_Sector_Erase(sector_index)
                    Case MemoryType.JTAG_SPI
                        Me.NoErrors = FCUSB.EJ_IF.SPI_SectorErase(sector_index)
                End Select
            Finally
                Threading.Monitor.Exit(InterfaceLock)
                Me.IsErasing = False
                Application.DoEvents()
            End Try
        End Sub

        Public Sub WriteSector(ByVal sector_index As Integer, ByVal Data() As Byte, ByVal area As FlashArea, ByRef SW As Stopwatch)
            If Not WaitForNotBusy() Then Exit Sub
            Try : Me.IsWriting = True
                Threading.Monitor.Enter(InterfaceLock)
                Dim DataToWrite(Data.Length - 1) As Byte
                Array.Copy(Data, DataToWrite, Data.Length)
                BitSwap_Forward(DataToWrite)
                SW.Start()
                Select Case Me.FlashType
                    Case MemoryType.SERIAL_NOR 'SPI
                        Me.NoErrors = FCUSB.SPI_NOR_IF.Sector_Write(sector_index, DataToWrite, area)
                    Case MemoryType.SERIAL_NAND
                        Me.NoErrors = FCUSB.SPI_NAND_IF.Sector_Write(sector_index, DataToWrite, area)
                    Case MemoryType.PARALLEL_NOR
                        Me.NoErrors = FCUSB.EXT_IF.Sector_Write(sector_index, DataToWrite, area)
                    Case MemoryType.SLC_NAND
                        Me.NoErrors = FCUSB.EXT_IF.Sector_Write(sector_index, DataToWrite, area)
                    Case MemoryType.JTAG_DMA_CFI
                        Me.NoErrors = FCUSB.EJ_IF.CFI_Sector_Write(sector_index, DataToWrite)
                    Case MemoryType.JTAG_SPI
                        Me.NoErrors = FCUSB.EJ_IF.SPI_WriteSector(sector_index, DataToWrite)
                End Select
                SW.Stop()
            Finally
                Threading.Monitor.Exit(InterfaceLock)
                Me.IsWriting = False
                Application.DoEvents()
            End Try
        End Sub

        Public Function GetSectorCount() As UInt32
            Select Case Me.FlashType
                Case MemoryType.SERIAL_NOR 'SPI
                    Return FCUSB.SPI_NOR_IF.Sector_Count()
                Case MemoryType.SERIAL_NAND
                    Return FCUSB.SPI_NAND_IF.Sector_Count()
                Case MemoryType.PARALLEL_NOR
                    Return FCUSB.EXT_IF.Sector_Count()
                Case MemoryType.SLC_NAND
                    Return FCUSB.EXT_IF.Sector_Count()
                Case MemoryType.JTAG_DMA_CFI
                    Return FCUSB.EJ_IF.CFI_Sector_Count()
                Case MemoryType.JTAG_SPI
                    Return FCUSB.EJ_IF.SPI_Sector_Count()
                Case Else
                    Return 1
            End Select
        End Function

        Public Function GetSectorSize(ByVal sector_index As UInt32, ByVal area As FlashArea) As UInt32
            Select Case Me.FlashType
                Case MemoryType.SERIAL_NOR 'SPI
                    Return FCUSB.SPI_NOR_IF.SectorSize(sector_index, area)
                Case MemoryType.SERIAL_NAND
                    Return FCUSB.SPI_NAND_IF.SectorSize(sector_index, area)
                Case MemoryType.PARALLEL_NOR
                    Return FCUSB.EXT_IF.SectorSize(sector_index, area)
                Case MemoryType.SLC_NAND
                    Return FCUSB.EXT_IF.SectorSize(sector_index, area)
                Case MemoryType.JTAG_DMA_CFI
                    Return FCUSB.EJ_IF.CFI_GetSectorSize(sector_index)
                Case MemoryType.JTAG_SPI
                    Return FCUSB.EJ_IF.SPI_GetSectorSize(sector_index)
            End Select
            Return 0
        End Function

        Public Function GetSectorBase(ByVal sector_index As UInt32, ByVal area As FlashArea) As UInt32
            Select Case Me.FlashType
                Case MemoryType.SERIAL_NOR 'SPI
                    Return FCUSB.SPI_NOR_IF.SectorFind(sector_index, area)
                Case MemoryType.SERIAL_NAND
                    Return FCUSB.SPI_NAND_IF.SectorFind(sector_index, area)
                Case MemoryType.PARALLEL_NOR
                    Return FCUSB.EXT_IF.SectorFind(sector_index, area)
                Case MemoryType.SLC_NAND
                    Return FCUSB.EXT_IF.SectorFind(sector_index, area)
                Case MemoryType.JTAG_DMA_CFI
                    Return FCUSB.EJ_IF.CFI_FindSectorBase(sector_index)
                Case MemoryType.JTAG_SPI
                    Return FCUSB.EJ_IF.CFI_FindSectorBase(sector_index)
                Case Else
                    Return 0
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


End Class
