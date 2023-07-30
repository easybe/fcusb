'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2017 - ALL RIGHTS RESERVED
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: this class creates a flash memory interface that is used by the main program

Imports FlashcatUSB.FlashMemory

Public Module MemoryInterface
    'Used by MemoryDeviceInstance to specify what type of memory device is attached
    Public Enum DeviceTypes
        NotSpecified = 0
        Dram = 1
        CFI = 2
        SPI = 3
        ExtIO = 4
        I2C = 5
    End Enum

    'Contains all of the memory devices (dram, cfi flash, spi flash) - resets at disconnect
    Public MyMemDevices As New List(Of MemoryDeviceInstance)
    Public MainFlashLoaded As Boolean = False
    Public FlashCounter As Integer = -1

    Public Function GetDeviceInstance(ByVal IndexName As Integer) As MemoryDeviceInstance
        If MyMemDevices.Count = 0 Then Return Nothing
        Dim i As Integer
        Dim memDev As MemoryDeviceInstance
        For i = 0 To MyMemDevices.Count - 1
            memDev = MyMemDevices(i)
            If memDev.Index = IndexName Then Return memDev
        Next
        Return Nothing
    End Function
    'Returns true if the memory interface at the specified index exists
    Public Function HasMemoryAttached(ByVal IndexName As Integer) As Boolean
        If MyMemDevices.Count = 0 Then Return False
        For i = 0 To MyMemDevices.Count - 1
            If MyMemDevices(i).Index = IndexName Then Return True
        Next
        Return False
    End Function

    Public Function AddMemoryDevice(ByVal TypeStr As DeviceTypes, ByVal BaseAddress As UInt32, ByVal DeviceSize As UInt32, ByRef IndexCreated As Integer, Optional ByVal Name As String = "(Not specified)") As MemoryDeviceInstance
        If (OperationMode = AvrMode.JTAG) Then
            If EJ_IF.TargetDevice.NoDMA Then
                If Not EJ_IF.SUPPORT_PRACC Then
                    WriteConsole(RM.GetString("fcusb_memif_nopraccsupport"))
                    Return Nothing
                End If
            Else
                If (Not EJ_IF.SUPPORT_DMA) Then
                    WriteConsole(RM.GetString("fcusb_memif_nopraccsupport"))
                    Return Nothing
                End If
            End If
        End If
        Dim TabString As String = ""
        Select Case TypeStr
            Case DeviceTypes.CFI
                TabString = "CFI Flash"
            Case DeviceTypes.Dram
                TabString = "Memory"
            Case DeviceTypes.SPI
                TabString = "SPI Flash"
            Case DeviceTypes.ExtIO
                TabString = "Flash"
        End Select
        Dim memDev As New MemoryDeviceInstance(TypeStr)
        memDev.Size = DeviceSize
        memDev.BaseAddress = BaseAddress
        memDev.Name = Name
        If memDev.Init() Then 'Must be called after events are setup
            FlashCounter += 1
            IndexCreated = FlashCounter
            memDev.Index = FlashCounter
            If GUI IsNot Nothing Then
                Dim newTab As New TabPage(TabString)
                newTab.Tag = memDev
                memDev.GuiControl.Width = newTab.Width
                memDev.GuiControl.Height = newTab.Height
                memDev.GuiControl.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top Or AnchorStyles.Bottom
                memDev.GuiControl.InitMemoryDevice(memDev.Name, memDev.BaseAddress, memDev.Size, MemControl_v2.access_mode.ReadWrite)
                If (Not TypeStr = DeviceTypes.Dram) Then
                    GUI.UpdateStatusMessage(RM.GetString("cnts_flashname"), memDev.Name)
                    GUI.UpdateStatusMessage(RM.GetString("cnts_flashsize"), Format(memDev.Size, "#,###") & " bytes")
                Else 'This is a DRAM, disable
                    memDev.GuiControl.DisableErase()
                End If
                If memDev.ReadOnly Then
                    memDev.GuiControl.DisableErase()
                    memDev.GuiControl.DisableWrite()
                End If
                newTab.Controls.Add(memDev.GuiControl)
                Application.DoEvents()
                GUI.AddTab(newTab)
            End If
            MyMemDevices.Add(memDev)
            Return memDev
        Else
            WriteConsole(RM.GetString("fcusb_memif_err1"))
            Return Nothing
        End If
    End Function

    Public Sub RemoveMemoryDevice(ByVal IndexName As Integer)
        Dim counter As Integer = 0
        For Each memDev In MyMemDevices
            If memDev.Index = IndexName Then
                MyMemDevices.RemoveAt(counter)
                Exit Sub
            End If
            counter += 1
        Next
    End Sub

    Public Function GetSectorFromAddress(ByRef memDev As MemoryDeviceInstance, ByVal AddressToFind As Integer) As Integer
        Dim x As Integer = memDev.GetSectorCount
        For i = 0 To x - 1
            Dim a As Integer = CInt(memDev.FindSectorBase(i, 0))
            Dim s As Integer = memDev.GetSectorSize(i, FlashArea.Main)
            If (a = AddressToFind) Or ((a + s) > AddressToFind) Then
                Return i
            End If
        Next
        Return -1
    End Function

    Public Class MemoryDeviceInstance
        Public WithEvents GuiControl As MemControl_v2
        Public Property Name As String
        Public Property Size As UInt32 'Number of bytes of the memory device
        Public Property BaseAddress As UInt32 = 0 'Only changes for JTAG devices
        Public Property FlashType As MemoryType = MemoryType.UNSPECIFIED
        Public Property DeviceType As DeviceTypes = DeviceTypes.NotSpecified
        Public Property Index As Integer = -1 'Contains a unque id
        Public Property [ReadOnly] As Boolean = False 'Set to true to disable write/erase functions
        Private Property IsErasing As Boolean = False
        Private Property IsBulkErasing As Boolean = False
        Private Property IsReading As Boolean = False
        Private Property IsWriting As Boolean = False
        Public ReadOnly Property IsBusy As Boolean
            Get
                If IsErasing Or IsReading Or IsWriting Or IsBulkErasing Then Return True
                Return False
            End Get
        End Property

        Private WithEvents CFIFlash As CFI = Nothing
        Private InterfaceLock As New Object

        Sub New(ByVal devType As DeviceTypes)
            GuiControl = New MemControl_v2
            DeviceType = devType
        End Sub


#Region "Callback and ThreadHelpers"

        Public Class StatusCallback
            Public DisplayStatus As Boolean 'Update the bottom status bar
            Public Progress As [Delegate] 'Update a progress function
            Public Speed As [Delegate] 'Update a Speed label function
        End Class

        Private Sub ThreadHelper_UpdateProgress(func As [Delegate], percent_done As Integer)
            Dim td As New Threading.Thread(Sub() Me.ThreadHelper_UpdateProgress_td(func, percent_done))
            td.Start()
        End Sub

        Private Sub ThreadHelper_UpdateProgress_td(func As [Delegate], percent_done As Integer)
            Try
                func.DynamicInvoke(CInt(percent_done))
                Application.DoEvents()
            Catch ex As Exception
            End Try
        End Sub

        Private Sub ThreadHelper_UpdateSpeed(func As [Delegate], speed_label As String)
            Dim td As New Threading.Thread(Sub() Me.ThreadHelper_UpdateSpeed_td(func, speed_label))
            td.Start()
        End Sub

        Private Sub ThreadHelper_UpdateSpeed_td(func As [Delegate], speed_label As String)
            Try
                func.DynamicInvoke(speed_label)
                Application.DoEvents()
            Catch ex As Exception
            End Try
        End Sub

        Private Sub ThreadHelper_SetStatus(ByVal msg_out As String)
            Dim td As New Threading.Thread(Sub() Me.ThreadHelper_SetStatus_td(msg_out))
            td.Start()
        End Sub

        Private Sub ThreadHelper_SetStatus_td(ByVal msg_out As String)
            Try
                SetStatus(msg_out)
            Catch ex As Exception
            End Try
        End Sub

#End Region

        'Returns TRUE if OKAY or FALSE if timeout
        Private Function WaitForNotBusy() As Boolean
            Dim i As Integer = 0
            Do While Me.IsBusy
                Threading.Thread.Sleep(5)
                i += 1
                If i = 1000 Then
                    Return False '10 second timeout
                End If
            Loop
            Return True
        End Function

        Public Function Init() As Boolean
            If DeviceType = DeviceTypes.CFI Then
                CFIFlash = New CFI
                If CFIFlash.DetectFlash(BaseAddress) Then
                    Size = CFIFlash.FlashSize
                    Name = CFIFlash.FlashName
                Else
                    Return False
                End If
            ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.JTAG Then
                If Not EJ_IF.SPI_API_LOADED Then
                    WriteConsole(RM.GetString("fcusb_memif_err2")) 'Can not detect SPI flash (API not loaded for current MCU)
                    Return False
                End If
                If EJ_IF.SPI_Detect Then
                    Size = EJ_IF.SPI_Part.FLASH_SIZE
                    Name = EJ_IF.SPI_Part.NAME
                Else
                    Return False
                End If
            ElseIf DeviceType = DeviceTypes.Dram Then
                Name = "Generic DRAM"
            End If
            GuiControl.SetUserLanguage(RM)
            Return True
        End Function

        Public Function GetTypeString() As String
            Select Case DeviceType
                Case DeviceTypes.CFI
                    Return "CFI Flash"
                Case DeviceTypes.Dram
                    Return "DRAM"
                Case DeviceTypes.SPI
                    Return "SPI Flash"
                Case DeviceTypes.I2C
                    Return "I2C EEPROM"
                Case DeviceTypes.ExtIO
                    Return "Flash"
            End Select
            Return ""
        End Function

        Private Sub OnWriteConsole(ByVal msg_out As String) Handles GuiControl.WriteConsole
            WriteConsole(msg_out)
        End Sub

        Private Sub OnSetStatus(ByVal status_text As String) Handles GuiControl.SetStatus
            SetStatus(status_text)
        End Sub

        Private Sub OnEraseDataRequest() Handles GuiControl.EraseMemory
            Try
                EraseBulk()
                WaitUntilReady()
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
                Dim data_back() As Byte = ReadBytes(base_addr, data.Length, memory_area)
                If data_back IsNot Nothing Then data = data_back
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
                WaitUntilReady()
            Catch ex As Exception
            End Try
        End Sub

        Private Sub OnEraseBlockRequest(ByVal Logical_Address As Long) Handles GuiControl.EraseNandBlock
            Try : Threading.Monitor.Enter(InterfaceLock)
                Dim physical_addr As Long = EXT_IF.NAND_GetAddressMapping(Logical_Address)
                EXT_IF.NAND_ERASEBLOCK(physical_addr, 0, False)
                WaitUntilReady()
            Finally
                Threading.Monitor.Exit(InterfaceLock)
            End Try
        End Sub

        Private Sub OnSuccessfulWrite(ByVal x As MemControl_v2.XFER_Operation) Handles GuiControl.SuccessfulWrite
            If GUI IsNot Nothing Then
                GUI.SuccessfulWriteOperation(Index, x)
            End If
        End Sub

        Public Function ReadBytes(ByVal base_addr As UInt32, ByVal count As UInt32, memory_area As FlashArea, Optional callback As StatusCallback = Nothing) As Byte()
            Dim data_out() As Byte = Nothing
            Using n As New IO.MemoryStream
                Dim f_params As New ReadParameters
                f_params.Address = base_addr
                f_params.Count = count
                f_params.Memory_Area = memory_area
                If callback IsNot Nothing Then
                    f_params.DisplayStatus = callback.DisplayStatus
                    f_params.UpdateProgress = callback.Progress
                    f_params.UpdateSpeed = callback.Speed
                End If
                If ReadStream(n, f_params) Then
                    data_out = n.GetBuffer()
                    ReDim Preserve data_out(n.Length - 1)
                End If
            End Using
            Return data_out
        End Function

        Public Function WriteBytes(ByVal Address As UInt32, ByRef Data() As Byte, ByVal memory_area As FlashArea, Optional callback As StatusCallback = Nothing) As Boolean
            Try
                Dim f_params As New WriteParameters
                f_params.Address = Address
                f_params.Count = Data.Length
                f_params.Memory_Area = memory_area
                f_params.Verify = MySettings.VERIFY_WRITE
                If callback IsNot Nothing Then
                    f_params.DisplayStatus = callback.DisplayStatus
                    f_params.UpdateProgress = callback.Progress
                    f_params.UpdateSpeed = callback.Speed
                End If
                Using n As New IO.MemoryStream(Data)
                    Return WriteStream(n, f_params)
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function ReadStream(ByVal data_stream As IO.Stream, ByRef Params As ReadParameters) As Boolean
            If (Threading.Thread.CurrentThread.Name = "") Then
                Dim td_int As Integer = Threading.Thread.CurrentThread.ManagedThreadId
                Threading.Thread.CurrentThread.Name = "MemIf.ReadStream_" & td_int
            End If
            Try
                Dim BytesTransfered As Long = 0
                Dim BlockSize As UInt32 = FCUSB_GetPreferredBlockSize()
                Dim Loops As Integer = CUInt(Math.Ceiling(Params.Count / BlockSize)) 'Calcuates iterations
                Dim b() As Byte 'Temp Byte buffer
                Dim LocationAddr As Long = 0 'location of FILE not stream
                Params.Timer = New Stopwatch
                Params.Timer.Start()
                Dim BytesRemaining As Integer = Params.Count
                For i = 1 To Loops
                    Dim BytesCountToRead As Long = BytesRemaining
                    If BytesCountToRead > BlockSize Then BytesCountToRead = BlockSize
                    ReDim b(BytesCountToRead - 1) 'Erase block data
                    b = ReadFlash(CLng(Params.Address + LocationAddr), BytesCountToRead, Params.Memory_Area)
                    If Params.AbortOperation Then Return False
                    If b Is Nothing Then Return False 'ERROR
                    BytesTransfered += BytesCountToRead
                    data_stream.Write(b, 0, b.Length)
                    LocationAddr += BytesCountToRead 'Increment location address
                    BytesRemaining -= BytesCountToRead
                    If i = 1 OrElse i = Loops OrElse (i Mod 4 = 0) Then
                        Params.Timer.Stop()
                        Try
                            Threading.Thread.CurrentThread.Join(10) 'Pump a message
                            Dim percent_done As Single = CSng((i / Loops) * 100) 'Calulate % done
                            If Params.UpdateSpeed IsNot Nothing Then
                                Dim speed_str As String = Format(Math.Round(LocationAddr / (Params.Timer.ElapsedMilliseconds / 1000)), "#,###") & " Bytes/s"
                                ThreadHelper_UpdateSpeed(Params.UpdateSpeed, speed_str)
                            End If
                            If Params.UpdateProgress IsNot Nothing Then
                                ThreadHelper_UpdateProgress(Params.UpdateProgress, CInt(percent_done))
                            End If
                            If Params.DisplayStatus Then 'Sets the console window
                                Dim StatusLoc As String = Format(LocationAddr, "#,###") & " of " & Format(Params.Count, "#,###") & " Bytes " 'Format Status
                                Dim StatusPrec As String = "(" & Math.Round(percent_done, 0) & "%) " 'Format Status
                                ThreadHelper_SetStatus("Reading " & GetTypeString() & " " & StatusLoc & StatusPrec) '& StatusSpeeed 'Print Status
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
            Finally
                Params.Timer.Stop()
            End Try
            Return False
        End Function

        Public Function WriteStream(ByVal data_stream As IO.Stream, ByVal params As WriteParameters) As Boolean
            If (Threading.Thread.CurrentThread.Name = "") Then
                Dim td_int As Integer = Threading.Thread.CurrentThread.ManagedThreadId
                Threading.Thread.CurrentThread.Name = "MemIf.WriteBytes_" & td_int
            End If
            If Me.ReadOnly Then Return False
            Try
                params.Timer = New Stopwatch
                params.Timer.Start()
                If DeviceType = DeviceTypes.Dram Then
                    Return WriteBytes_LoopForVol(data_stream, params)
                Else 'Non-Volatile memory
                    Return WriteBytes_LoopForNonVol(data_stream, params)
                End If
            Finally
                params.Timer.Stop()
                ReadMode()
            End Try
            Return False
        End Function

        Private Function WriteBytes_LoopForVol(ByVal data_stream As IO.Stream, ByVal Params As WriteParameters) As Boolean
            Dim BlockSize As Integer = 4096
            If EJ_IF.TargetDevice.NoDMA Then BlockSize = 1024
            Dim BytesLeft As Integer = data_stream.Length
            Dim BytesWritten As Integer = 0
            While (BytesLeft > 0)
                If Params.AbortOperation Then Return False
                Dim BytesThisPacket As Integer = BytesLeft
                If BytesThisPacket > BlockSize Then BytesThisPacket = BlockSize
                Dim DataOut(BytesThisPacket - 1) As Byte
                data_stream.Read(DataOut, 0, BytesThisPacket)
                Dim result As Boolean = EJ_IF.Memory_Write_Bulk(Params.Address + BytesWritten, DataOut)
                If Not result Then Return False
                If Params.UpdateSpeed IsNot Nothing Then
                    Dim speed_str As String = Format(Math.Round(BytesWritten / (Params.Timer.ElapsedMilliseconds / 1000)), "#,###") & " Bytes/s"
                    ThreadHelper_UpdateSpeed(Params.UpdateSpeed, speed_str)
                End If
                If Params.UpdateProgress IsNot Nothing Then
                    Dim percent As Single = CSng((BytesWritten / Params.Count) * 100)
                    ThreadHelper_UpdateProgress(Params.UpdateProgress, CInt(percent))
                End If
                BytesWritten += BytesThisPacket
                BytesLeft -= BytesThisPacket
            End While
            Return True
        End Function

        Private Function WriteBytes_LoopForNonVol(ByVal data_stream As IO.Stream, ByRef Params As WriteParameters) As Boolean
            Dim TotalSectors As Integer = GetSectorCount()
            Dim BytesTransfered As UInt32 = 0
            Dim BytesLeft As UInt32 = Params.Count
            Dim SectorLength As Integer
            Dim SectorBase As Long
            Dim SectorBuffer() As Byte
            Dim percent As Single = 0
            WaitUntilReady() 'Some flash devices requires us to wait before sending data
            For i = 0 To (TotalSectors - 1)
                SectorBase = FindSectorBase(i, Params.Memory_Area)
                SectorLength = GetSectorSize(i, Params.Memory_Area)
                ReDim SectorBuffer(SectorLength - 1)
                If (Params.Address = SectorBase) Then 'Common
                    If (BytesLeft > SectorBuffer.Length) Then
                        data_stream.Read(SectorBuffer, 0, SectorBuffer.Length)
                        BytesLeft -= SectorBuffer.Length
                    ElseIf BytesLeft = SectorBuffer.Length Then
                        data_stream.Read(SectorBuffer, 0, SectorBuffer.Length)
                        data_stream.Dispose()
                        BytesLeft = 0
                    Else 'Data has less data, read in from flash to fill buffer
                        WaitUntilReady()
                        Dim b() As Byte = ReadFlash(CUInt(Params.Address + BytesLeft), (SectorLength - BytesLeft), Params.Memory_Area)
                        If b Is Nothing Then Return False
                        data_stream.Read(SectorBuffer, 0, BytesLeft)
                        Array.Copy(b, 0, SectorBuffer, BytesLeft, b.Length) 'Copy over the read data
                        data_stream.Dispose()
                        BytesLeft = 0
                    End If
                    Dim WriteResult As Boolean = WriteDataSub(i, SectorBuffer, Math.Round(percent, 0), Params) 'Writes data
                    If WriteResult Then
                        BytesTransfered += SectorBuffer.Length
                        percent = CSng(Math.Floor((CSng(BytesTransfered) / CSng(Params.Count)) * 100))
                        If Params.UpdateSpeed IsNot Nothing Then
                            Dim speed_str As String = Format(Math.Round(BytesTransfered / (Params.Timer.ElapsedMilliseconds / 1000)), "#,###") & " Bytes/s"
                            ThreadHelper_UpdateSpeed(Params.UpdateSpeed, speed_str)
                        End If
                        If Params.UpdateProgress IsNot Nothing Then
                            ThreadHelper_UpdateProgress(Params.UpdateProgress, CInt(percent))
                        End If
                        If BytesLeft = 0 Then Exit For 'No more data to write
                    Else
                        If (Not FlashType = FlashMemory.MemoryType.SLC_NAND) OrElse (i = TotalSectors - 1) Then Return False
                        data_stream.Position -= SectorBuffer.Length 'We are going to re-write these bytes to the next block
                    End If
                    Params.Address = CLng(Params.Address + SectorBuffer.Length) 'Increment the address we are writing
                ElseIf (Params.Address > SectorBase) And Params.Address < (SectorBase + SectorLength) Then 'Address is greater than base address
                    Dim DestBase As Long = CLng(Params.Address - SectorBase) 'Where the data is going (array)
                    Dim BytesToCopy As Integer = CInt(SectorLength - DestBase) 'How many bytes to copy from data array
                    Dim b() As Byte = ReadFlash(CUInt(SectorBase), CInt(DestBase), Params.Memory_Area) 'Read in what we are not writing
                    If b Is Nothing Then Return False
                    Params.Address = SectorBase 'push the address pointer back to the begining of the sector
                    Array.Copy(b, 0, SectorBuffer, 0, b.Length) 'TempBuffer has data before Address
                    If (BytesLeft > BytesToCopy) Then 'We have more to write
                        data_stream.Read(SectorBuffer, DestBase, BytesToCopy) 'TempBuffer = all data to write
                        BytesLeft -= BytesToCopy
                    ElseIf BytesLeft = BytesToCopy Then 'We wrote everything (excactly)
                        data_stream.Read(SectorBuffer, DestBase, BytesToCopy)
                        data_stream.Dispose()
                        BytesLeft = 0
                    Else 'We need to fill the end of the array
                        Dim BytesFilled As Long = CLng(Params.Address + DestBase + BytesLeft)
                        b = ReadFlash(BytesFilled, CInt(SectorLength - (DestBase + BytesLeft)), Params.Memory_Area)
                        If b Is Nothing Then Return False
                        Array.Copy(b, 0, SectorBuffer, (DestBase + BytesLeft), b.Length) 'Fills end of array with data from flash
                        data_stream.Read(SectorBuffer, DestBase, BytesLeft) 'Finally, copy the bytes we want to write into the dest array
                        data_stream.Dispose()
                        BytesLeft = 0
                    End If
                    Dim WriteResult As Boolean = WriteDataSub(i, SectorBuffer, Math.Round(percent, 0), Params) 'Writes data
                    If Params.AbortOperation Then Return False
                    Threading.Thread.CurrentThread.Join(10) 'Pump a message
                    If WriteResult Then
                        BytesTransfered += SectorBuffer.Length
                        percent = CSng((BytesTransfered / Params.Count) * 100)
                        If Params.UpdateSpeed IsNot Nothing Then
                            Dim speed_str As String = Format(Math.Round(BytesTransfered / (Params.Timer.ElapsedMilliseconds / 1000)), "#,###") & " Bytes/s"
                            ThreadHelper_UpdateSpeed(Params.UpdateSpeed, speed_str)
                        End If
                        If Params.UpdateProgress IsNot Nothing Then
                            ThreadHelper_UpdateProgress(Params.UpdateProgress, CInt(percent))
                        End If
                        If BytesLeft = 0 Then Exit For 'No more data to write
                    Else
                        If (Not FlashType = FlashMemory.MemoryType.SLC_NAND) OrElse (i = TotalSectors - 1) Then Return False
                        data_stream.Position -= SectorBuffer.Length 'We are going to re-write these bytes to the next block
                    End If
                    Params.Address = CUInt(SectorBase + SectorLength)
                End If
            Next
            Utilities.Sleep(10)
            Return True 'Operation was successful
        End Function
        'Does the actual erase sector and program functions
        Private Function WriteDataSub(ByRef sector As UInt32, ByVal data() As Byte, ByVal Percent As Integer, ByRef Params As WriteParameters) As Boolean
            Try
                Dim FailedAttempts As Integer = 0
                Dim ReadResult As Boolean
                Do
                    If Params.AbortOperation Then Return False
                    If Params.EraseSector Then EraseSector(sector, Params.Memory_Area)
                    If Params.DisplayStatus Then
                        ThreadHelper_SetStatus(String.Format(RM.GetString("fcusb_memif_writeaddr"), "0x" & Hex(Params.Address), Format(data.Length, "#,###"), Percent.ToString & "%"))
                        Application.DoEvents()
                    End If
                    WriteSector(sector, data, Params.Memory_Area, Params.Timer)
                    If Params.AbortOperation Then Return False
                    If Params.Verify Then 'Verify is enabled and we are monitoring this
                        If Params.DisplayStatus Then ThreadHelper_SetStatus(String.Format(RM.GetString("fcusb_memif_verify"), "0x" & Hex(Params.Address)))
                        ReadResult = VerifyDataSub(Params.Address, data, Params.Memory_Area)
                        If Params.AbortOperation Then Return False
                        If ReadResult Then
                            FailedAttempts = 0
                            If Params.DisplayStatus Then ThreadHelper_SetStatus(RM.GetString("fcusb_memif_verifyokay"))
                            Application.DoEvents()
                        Else
                            FailedAttempts = FailedAttempts + 1
                            If FailedAttempts = 3 Then
                                If (FlashType = FlashMemory.MemoryType.SLC_NAND) Then
                                    WriteConsole("BAD NAND BLOCK AT address: 0x" & Hex(Params.Address))
                                    Return False
                                Else
                                    WriteConsole(String.Format(RM.GetString("fcusb_memif_verifyfailed"), "0x" & Hex(Params.Address)))
                                    If Params.DisplayStatus Then ThreadHelper_SetStatus(String.Format(RM.GetString("fcusb_memif_verifyfailed"), "0x" & Hex(Params.Address)))
                                    Application.DoEvents()
                                    Return GetMessageBoxForVerify(Params.Address)
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
            Dim BlockSize As Integer = Data.Length
            Dim Verify() As Byte 'The data to check against
            Dim MiscountCounter As Integer = 0
            Dim FirstWrongByte As Byte = Nothing
            Dim FirstWrongAddr As Integer = 0
            WaitUntilReady()
            Verify = ReadFlash(BaseAddress, BlockSize, memory_area)
            If Verify Is Nothing OrElse (Not Verify.Length = Data.Length) Then
                WriteConsole(RM.GetString("fcusb_memif_verifyerr1"))
                Return False
            End If
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
                Dim DataShouldBe As Byte = Data(CInt(FirstWrongAddr - BaseAddress))
                WriteConsole(String.Format(RM.GetString("fcusb_memif_verifyerr2"), "0x" & Hex(FirstWrongAddr), "0x" & Hex(DataShouldBe), "0x" & Hex(FirstWrongByte), MiscountCounter))
                Return False 'Error!
            End If
        End Function

        Public Function ReadFlash(ByVal Address As UInt32, ByVal Count As UInt32, ByVal memory_area As FlashArea) As Byte()
            If Not WaitForNotBusy() Then Return Nothing
            Try : Me.IsReading = True
                Dim data_out() As Byte = Nothing
                Try : Threading.Monitor.Enter(InterfaceLock)
                    If DeviceType = DeviceTypes.Dram Then
                        data_out = EJ_IF.Memory_Read_Bulk(BaseAddress + Address, Count)
                    ElseIf DeviceType = DeviceTypes.CFI Then
                        data_out = CFIFlash.ReadData(Address, Count)
                    ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.JTAG Then
                        data_out = EJ_IF.SPI_ReadBulk(BaseAddress + Address, Count)
                    ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.SPI Then
                        If Not SPI_IF.MyFlashStatus = SPI.ConnectionStatus.Supported Then Return Nothing
                        data_out = SPI_IF.ReadData(Address, Count, memory_area)
                    ElseIf DeviceType = DeviceTypes.ExtIO Then
                        data_out = EXT_IF.ReadData(Address, Count, memory_area)
                    End If
                Catch ex As Exception
                Finally
                    Threading.Monitor.Exit(InterfaceLock)
                End Try
                BitSwap_Reverse(data_out)
                Return data_out
            Finally
                Me.IsReading = False
                Application.DoEvents()
            End Try
        End Function

        Public Sub EraseSector(ByVal SectorNum As Integer, Optional Memory_area As FlashArea = FlashArea.NotSpecified)
            If Not WaitForNotBusy() Then Exit Sub
            Try : Me.IsErasing = True
                If DeviceType = DeviceTypes.CFI Then
                    CFIFlash.Sector_Erase(SectorNum)
                ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.JTAG Then
                    EJ_IF.SPI_EraseSector(SectorNum)
                ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.SPI Then
                    SPI_IF.Sector_Erase(SectorNum, Memory_area)
                ElseIf DeviceType = DeviceTypes.ExtIO Then
                    EXT_IF.Sector_Erase(SectorNum, Memory_area)
                End If
            Catch ex As Exception
            Finally
                Me.IsErasing = False
                Application.DoEvents()
            End Try
        End Sub

        Private Sub WriteSector(ByRef sector As Integer, ByVal data() As Byte, ByVal area As FlashArea, ByRef SW As Stopwatch)
            If Not WaitForNotBusy() Then Exit Sub
            Try : Me.IsWriting = True : Threading.Monitor.Enter(InterfaceLock)
                Dim DataToWrite(data.Length - 1) As Byte
                Array.Copy(data, DataToWrite, data.Length)
                BitSwap_Forward(DataToWrite)
                SW.Start()
                If DeviceType = DeviceTypes.CFI Then
                    CFIFlash.WriteSector(sector, DataToWrite)
                ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.JTAG Then
                    EJ_IF.SPI_WriteSector(sector, DataToWrite)
                ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.SPI Then
                    SPI_IF.WriteSector(sector, DataToWrite, area)
                ElseIf DeviceType = DeviceTypes.ExtIO Then
                    Dim Addr32 As Long = Me.FindSectorBase(sector, area)
                    EXT_IF.WriteData(Addr32, DataToWrite, area)
                End If
                SW.Stop()
            Catch ex As Exception
            Finally
                Threading.Monitor.Exit(InterfaceLock)
                Me.IsWriting = False
                Application.DoEvents()
            End Try
        End Sub

        Public Sub WaitUntilReady()
            If DeviceType = DeviceTypes.CFI Then
                CFIFlash.WaitUntilReady()
            ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.JTAG Then
                EJ_IF.SPI_WaitUntilReady()
            ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.SPI Then
                SPI_IF.WaitUntilReady()
            ElseIf DeviceType = DeviceTypes.ExtIO Then
                SPI_IF.WaitUntilReady()
            Else
                Utilities.Sleep(100)
            End If
        End Sub

        Public Function GetSectorCount() As Integer
            If DeviceType = DeviceTypes.CFI Then
                Return CFIFlash.GetFlashSectors()
            ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.JTAG Then
                Return EJ_IF.SPI_GetFlashSectors()
            ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.SPI Then
                Return SPI_IF.Sectors_Count()
            ElseIf DeviceType = DeviceTypes.ExtIO Then
                Return EXT_IF.MyFlashDevice.SECTOR_COUNT
            Else
                Return -1
            End If
        End Function

        Public Function FindSectorBase(ByVal sectorInt As UInt32, ByVal memory_area As FlashArea) As UInt32
            If DeviceType = DeviceTypes.CFI Then
                Return CFIFlash.FindSectorBase(sectorInt)
            ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.JTAG Then
                Return EJ_IF.SPI_FindSectorBase(sectorInt)
            ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.SPI Then
                Return SPI_IF.FindSectorBase(sectorInt, memory_area)
            ElseIf DeviceType = DeviceTypes.ExtIO Then
                Dim base_addr As Long = 0
                If sectorInt > 0 Then
                    For i As UInt32 = 0 To sectorInt - 1
                        base_addr += GetSectorSize(i, memory_area)
                    Next
                End If
                Return base_addr
            Else
                Return 0
            End If
        End Function

        Public Function GetSectorSize(ByVal sector As Integer, ByVal memory_area As FlashArea) As UInt32
            If DeviceType = DeviceTypes.CFI Then
                Return CFIFlash.GetSectorSize(sector)
            ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.JTAG Then
                Return EJ_IF.SPI_GetSectorSize()
            ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.SPI Then
                Return SPI_IF.SectorSize(0, memory_area) 'all sizes are the same size
            ElseIf DeviceType = DeviceTypes.ExtIO Then
                If EXT_IF.MyFlashDevice.FLASH_TYPE = FlashMemory.MemoryType.PARALLEL_NOR Then
                    Return DirectCast(EXT_IF.MyFlashDevice, MFP_Flash).GetSectorSize(sector)
                Else
                    If memory_area = FlashArea.Main Then
                        Return DirectCast(EXT_IF.MyFlashDevice, NAND_Flash).BLOCK_SIZE
                    Else 'Spare area
                        Dim page_count As Integer = DirectCast(EXT_IF.MyFlashDevice, NAND_Flash).BLOCK_SIZE / EXT_IF.MyFlashDevice.PAGE_SIZE
                        Dim page_ext_size As Integer = DirectCast(EXT_IF.MyFlashDevice, NAND_Flash).PAGE_SIZE_EXTENDED - EXT_IF.MyFlashDevice.PAGE_SIZE 'i.e. 64
                        Dim sector_size As Integer = (page_count * page_ext_size)
                        Return sector_size
                    End If
                End If
            Else
                Return 0
            End If
        End Function

        Public Sub ReadMode()
            If DeviceType = DeviceTypes.CFI Then
                CFIFlash.Read_Mode()
            ElseIf DeviceType = DeviceTypes.ExtIO Then
                'This probably supports READ MODE
                EXT_IF.ResetDevice()
            End If
        End Sub

        Public Function EraseBulk() As Boolean
            If Not WaitForNotBusy() Then Return False
            Try : Me.IsBulkErasing = True : Threading.Monitor.Enter(InterfaceLock)
                If DeviceType = DeviceTypes.Dram Then
                ElseIf DeviceType = DeviceTypes.CFI Then
                    CFIFlash.EraseBulk()
                ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.JTAG Then
                    EJ_IF.SPI_EraseBulk()
                ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.SPI Then
                    SPI_IF.ChipErase()
                ElseIf DeviceType = DeviceTypes.ExtIO Then
                    EXT_IF.EraseChip()
                End If
            Finally
                Threading.Monitor.Exit(InterfaceLock)
                Me.IsBulkErasing = False
            End Try
            Return True
        End Function
        'Reads a single byte from the memory device
        Public Function ReadByte(ByVal address As UInt64) As Byte
            If Not WaitForNotBusy() Then Return Nothing
            If DeviceType = DeviceTypes.Dram Then
                Dim ReadAddr As UInteger = CUInt(Math.Floor(address / 4) * 4)   'JTAG reads only words correctly
                Dim ret As UInt32 = EJ_IF.Memory_Read_W(ReadAddr)
                Dim d As UInt32 = CUInt((24 - ((address - ReadAddr) * 8)))
                Return CByte(((ret >> CInt(d)) And &HFF))
            ElseIf DeviceType = DeviceTypes.CFI Then
                Return CFIFlash.ReadByte(address)
            ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.JTAG Then
                Dim d() As Byte = EJ_IF.SPI_ReadBulk(address, 1)
                If Not d Is Nothing Then Return d(0)
            ElseIf DeviceType = DeviceTypes.SPI And OperationMode = AvrMode.SPI Then
                Dim d() As Byte = SPI_IF.ReadData(address, 1, FlashArea.Main)
                If Not d Is Nothing Then Return d(0)
            End If
            Return Nothing
        End Function

        Friend Sub DisableGuiControls()
            If GuiControl IsNot Nothing Then GuiControl.DisableControls(False)
        End Sub

        Friend Sub EnableGuiControls()
            If GuiControl IsNot Nothing Then
                GuiControl.EnableControls()
            End If
        End Sub


    End Class

End Module
