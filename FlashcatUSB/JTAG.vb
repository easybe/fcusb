'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2018 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'ACKNOWLEDGEMENT: USB driver functionality provided by LibUsbDotNet (sourceforge.net/projects/libusbdotnet)

Imports FlashcatUSB.USB.HostClient

Namespace JTAG

    Public Class JTAG_IF
        Public FCUSB As FCUSB_DEVICE
        Public Property Detected As Boolean = False
        Public Property Count As Integer = 0
        Public Property Chain_Length As Integer = -1

        Public Devices As New List(Of JTAG_DEVICE)
        Public Property SELECTED_INDEX As Integer = 0
        Public Property TCK_SPEED As JTAG_SPEED = JTAG_SPEED._10MHZ
        Private Property CPLD_PROG_MODE As Boolean = False 'We are using internal JTAG to reprogram CPLD
        Public WithEvents TAP As JTAG_STATE_CONTROLLER 'JTAG state machine

        Sub New(ByVal parent_if As FCUSB_DEVICE)
            FCUSB = parent_if
            TAP = Nothing
        End Sub
        'Connects to the target device
        Public Function Init(Optional select_cpld As Boolean = False) As Boolean
            Try
                Me.Detected = False
                Me.Count = 0
                Me.Chain_Length = -1
                Me.SELECTED_INDEX = 0
                Me.CPLD_PROG_MODE = select_cpld
                Devices.Clear()
                TAP = Nothing
                If (FCUSB.HWBOARD = USB.FCUSB_BOARD.Professional) AndAlso Not CPLD_PROG_MODE Then
                    If Me.TCK_SPEED = JTAG_SPEED._10MHZ Then
                        WriteConsole("JTAG TCK speed: 10 MHz")
                    ElseIf Me.TCK_SPEED = JTAG_SPEED._20MHZ Then
                        WriteConsole("JTAG TCK speed: 20 MHz")
                    End If
                    Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_INIT, Nothing, Me.TCK_SPEED)
                    If Not result Then Return False
                End If
                If Not TAP_Detect() Then Return False
                WriteConsole("JTAG chain detected: " & Me.Count & " devices")
                WriteConsole("JTAG TDI size: " & Me.Chain_Length & " bits")
                For i = 0 To Me.Count - 1
                    Dim id_str As String = "0x" & Hex(Devices(i).IDCODE).PadLeft(8, "0")
                    Dim mfg_str As String = GetManu(Devices(i).MANUID)
                    WriteConsole("JEDEC ID: " & id_str & " (" & mfg_str & " 0x" & Hex(Devices(i).PARTNU).PadLeft(4, "0") & ")")
                Next
                Return True
            Catch ex As Exception
            End Try
            Return False
        End Function

        Public Sub TAP_Init()
            Me.Devices(0).IR_LEN = Me.Chain_Length
            Select_Device(0)
            If (Not FCUSB.HWBOARD = USB.FCUSB_BOARD.Professional) Or CPLD_PROG_MODE Then
                TAP = New JTAG_STATE_CONTROLLER
                AddHandler TAP.Shift_TDI, AddressOf ShiftTDI
                AddHandler TAP.Shift_TMS, AddressOf ShiftTMS
                TAP.Reset()
            End If
        End Sub

        'Attempts to auto-detect a JTAG device on the TAP, returns the IR Length of the device
        Private Function TAP_Detect() As Boolean
            Dim r_data(63) As Byte
            If CPLD_PROG_MODE Then
                FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.CPLD_JTAG_DETECT, r_data)
            Else
                FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_DETECT, r_data)
            End If
            Dim dev_count As Integer = r_data(0)
            Me.Chain_Length = r_data(1)
            If dev_count = 0 Then Return False
            Dim ptr As Integer = 2
            Dim ID32 As UInt32 = 0
            For i = 0 To dev_count - 1
                ID32 = (CUInt(r_data(ptr)) << 24)
                ID32 = ID32 Or (CUInt(r_data(ptr + 1)) << 16)
                ID32 = ID32 Or (CUInt(r_data(ptr + 2)) << 8)
                ID32 = ID32 Or (CUInt(r_data(ptr + 3)) << 0)
                ptr += 4
                If ID32 = 0 Or ID32 = &HFFFFFFFFUI Then
                Else
                    Dim n As New JTAG_DEVICE
                    n.IDCODE = ID32
                    n.VERSION = CShort((ID32 And &HF0000000L) >> 28)
                    n.PARTNU = CUShort((ID32 And &HFFFF000) >> 12)
                    n.MANUID = CUShort((ID32 And &HFFE) >> 1)
                    n.IR_LEN = 0 'Load BSDL class here and load this from known values
                    Devices.Add(n)
                    Me.Detected = True
                End If
            Next
            If Me.Detected Then
                Me.Count = Devices.Count
                If Me.Count = 1 Then Devices(0).IR_LEN = Me.Chain_Length
                Return True
            End If
            Return False
        End Function

        Public Sub Configure(proc_type As PROCESSOR)
            Me.Devices(Me.SELECTED_INDEX).ACCESS = JTAG_MEM_ACCESS.NONE
            If proc_type = PROCESSOR.MIPS Then
                GUI.PrintConsole("Configure JTAG engine for MIPS processor")
                Dim IMPCODE As UInt32 = ReadWriteReg32(EJTAG_OPCODE.IMPCODE)
                EJTAG_LoadCapabilities(IMPCODE) 'Only supported by MIPS/EJTAG devices
                If Me.DMA_SUPPORTED Then
                    Dim r As UInteger = ReadMemory(&HFF300000UI, DATA_WIDTH.Word) 'Returns 2000001E 
                    r = r And &HFFFFFFFBUI '2000001A
                    WriteMemory(&HFF300000UI, r, DATA_WIDTH.Word)
                    Me.Devices(Me.SELECTED_INDEX).ACCESS = JTAG_MEM_ACCESS.EJTAG_DMA
                    GUI.PrintConsole(RM.GetString("jtag_dma"))
                Else
                    Me.Devices(Me.SELECTED_INDEX).ACCESS = JTAG_MEM_ACCESS.EJTAG_PRACC
                    GUI.PrintConsole(RM.GetString("jtag_no_dma"))
                End If
            ElseIf proc_type = PROCESSOR.ARM Then
                GUI.PrintConsole("Configure JTAG engine for ARM processor")
                Me.Devices(Me.SELECTED_INDEX).ACCESS = JTAG_MEM_ACCESS.ARM
                'This does processor disable_cache, disable_mmu, and halt
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_ARM_INIT, Nothing)
            End If
        End Sub

#Region "SVF Player"
        Public WithEvents JSP As New SVF_Player() 'SVF / XSVF Parser and player

        Private Sub JSP_ResetTap() Handles JSP.ResetTap
            Reset_StateMachine()
        End Sub

        Private Sub JSP_GotoState(dst_state As JTAG_MACHINE_STATE) Handles JSP.GotoState
            TAP_GotoState(dst_state)
        End Sub

        Private Sub JSP_ToggleClock(ByVal ticks As UInt32) Handles JSP.ToggleClock
            Tap_Toggle(ticks)
        End Sub

        Private Sub HandleJtagPrintRequest(ByVal msg As String) Handles JSP.Writeconsole
            GUI.PrintConsole("SVF Player: " & msg)
        End Sub

        Public Sub JSP_ShiftIR(ByVal tdi_bits() As Byte, ByRef tdo_bits() As Byte, ByVal bit_count As UInt16, exit_mode As Boolean) Handles JSP.ShiftIR
            If TAP Is Nothing Then
                Dim packet_param As UInt32 = bit_count Or (1 << 17)
                If exit_mode Then packet_param = packet_param Or (1 << 16)
                ReDim tdo_bits(tdi_bits.Length - 1)
                TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR)
                If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, tdi_bits, tdi_bits.Length) Then Exit Sub 'Preloads the TDI/TMS data
                FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_SHIFT_DATA, tdo_bits, packet_param)
            Else
                TAP.ShiftIR(tdi_bits, tdo_bits, bit_count, exit_mode)
                Utilities.Sleep(2)
            End If
        End Sub

        Public Sub JSP_ShiftDR(ByVal tdi_bits() As Byte, ByRef tdo_bits() As Byte, ByVal bit_count As UInt16, exit_mode As Boolean) Handles JSP.ShiftDR
            If TAP Is Nothing Then
                Dim packet_param As UInt32 = bit_count Or (1 << 17)
                If exit_mode Then packet_param = packet_param Or (1 << 16)
                ReDim tdo_bits(tdi_bits.Length - 1)
                TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
                If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, tdi_bits, tdi_bits.Length) Then Exit Sub 'Preloads the TDI/TMS data
                FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_SHIFT_DATA, tdo_bits, packet_param)
            Else
                TAP.ShiftDR(tdi_bits, tdo_bits, bit_count, exit_mode)
                Utilities.Sleep(2)
            End If
        End Sub

#End Region

#Region "EJTAG Extension"
        Public IMPVER As String 'converted to text readable
        Public RK4_ENV As Boolean 'Indicates host is a R4000
        Public RK3_ENV As Boolean 'Indicates host is a R3000
        Public DINT_SUPPORT As Boolean 'Probe can use DINT signal to debug int on this cpu
        Public ASID_SIZE As Short '0=no ASIS,1=6bit,2=8bit
        Public MIPS16e As Boolean 'Indicates MIPS16e ASE support
        Public DMA_SUPPORTED As Boolean 'Indicates no DMA support and must use PrAcc mode
        Public MIPS32 As Boolean
        Public MIPS64 As Boolean

        Public Enum EJTAG_CTRL As UInt32
            '/* EJTAG 3.1 Control Register Bits */
            VPED = (1 << 23)       '/* R    */
            '/* EJTAG 2.6 Control Register Bits */
            Rocc = (1UI << 31)     '/* R/W0 */
            Psz1 = (1 << 30)       '/* R    */
            Psz0 = (1 << 29)       '/* R    */
            Doze = (1 << 22)       '/* R    */
            ProbTrap = (1 << 14)   '/* R/W  */
            DebugMode = (1 << 3)   '/* R    */
            '/* EJTAG 1.5.3 Control Register Bits */
            Dnm = (1 << 28)        '/* */
            Sync = (1 << 23)       '/* R/W  */
            Run = (1 << 21)        '/* R    */
            PerRst = (1 << 20)     '/* R/W  */
            PRnW = (1 << 19)       '/* R    0 = Read, 1 = Write */
            PrAcc = (1 << 18)      '/* R/W0 */
            DmaAcc = (1 << 17)     '/* R/W  */
            PrRst = (1 << 16)      '/* R/W  */
            ProbEn = (1 << 15)     '/* R/W  */
            SetDev = (1 << 14)     '/* R    */
            JtagBrk = (1 << 12)    '/* R/W1 */
            DStrt = (1 << 11)      '/* R/W1 */
            DeRR = (1 << 10)       '/* R    */
            DrWn = (1 << 9)        '/* R/W  */
            Dsz1 = (1 << 8)        '/* R/W  */
            Dsz0 = (1 << 7)        '/* R/W  */
            DLock = (1 << 5)       '/* R/W  */
            BrkSt = (1 << 3)       '/* R    */
            TIF = (1 << 2)         '/* W0/R */
            TOF = (1 << 1)         '/* W0/R */
            ClkEn = (1 << 0)       '/* R/W  */
        End Enum
        'Loads specific features this device using EJTAG IMP OPCODE
        Private Sub EJTAG_LoadCapabilities(ByVal features As UInt32)
            Dim e_ver As UInt32 = ((features >> 29) And 7)
            Dim e_nodma As UInt32 = (features And (1 << 14))
            Dim e_priv As UInt32 = (features And (1 << 28))
            Dim e_dint As UInt32 = (features And (1 << 24))
            Dim e_mips As UInt32 = (features And 1)
            Select Case e_ver
                Case 0
                    IMPVER = "1 and 2.0"
                Case 1
                    IMPVER = "2.5"
                Case 2
                    IMPVER = "2.6"
                Case 3
                    IMPVER = "3.1"
            End Select
            If (e_nodma = 0) Then
                DMA_SUPPORTED = True
            Else
                DMA_SUPPORTED = False
            End If
            If (e_priv = 0) Then
                RK3_ENV = False
                RK4_ENV = True
            Else
                RK3_ENV = True
                RK4_ENV = False
            End If
            If (e_dint = 0) Then
                DINT_SUPPORT = False
            Else
                DINT_SUPPORT = True
            End If
            If (e_mips = 0) Then
                MIPS32 = True
                MIPS64 = False
            Else
                MIPS32 = False
                MIPS64 = True
            End If
        End Sub
        'Resets the processor (EJTAG ONLY)
        Public Sub EJTAG_Reset()
            ReadWriteReg32(EJTAG_OPCODE.CONTROL_IR, (EJTAG_CTRL.PrRst Or EJTAG_CTRL.PerRst))
        End Sub

        Public Function EJTAG_Debug_Enable() As Boolean
            Try
                Dim debug_flag As UInt32 = EJTAG_CTRL.PrAcc Or EJTAG_CTRL.ProbEn Or EJTAG_CTRL.SetDev Or EJTAG_CTRL.JtagBrk
                Dim ctrl_reg As UInt32 = ReadWriteReg32(EJTAG_OPCODE.CONTROL_IR, debug_flag)
                If (ReadWriteReg32(EJTAG_OPCODE.CONTROL_IR, EJTAG_CTRL.PrAcc Or EJTAG_CTRL.ProbEn Or EJTAG_CTRL.SetDev) And EJTAG_CTRL.BrkSt) Then
                    Return True
                Else
                    Return False
                End If
            Catch ex As Exception
            End Try
            Return False
        End Function

        Public Sub EJTAG_Debug_Disable()
            Try
                Dim flag As UInt32 = (EJTAG_CTRL.ProbEn Or EJTAG_CTRL.SetDev) 'This clears the JTAGBRK bit
                ReadWriteReg32(EJTAG_OPCODE.CONTROL_IR, flag)
            Catch ex As Exception
            End Try
        End Sub

        Private Const MAX_USB_BUFFER_SIZE As UShort = 2048 'Max number of bytes we should send via USB bulk endpoints
        'Target device needs to support DMA and FCUSB needs to include flashmode
        Public Sub DMA_WriteFlash(ByVal dma_addr As UInt32, ByVal data_to_write() As Byte, ByVal prog_mode As CFI.CFI_FLASH_MODE)
            Dim data_blank As Boolean = True
            For i = 0 To data_to_write.Length - 1
                If Not data_to_write(i) = 255 Then
                    data_blank = False
                    Exit For
                End If
            Next
            If data_blank Then Exit Sub 'Sector is already blank, no need to write data
            Dim BytesWritten As UInt32 = 0
            Try
                Dim BufferIndex As UInt32 = 0
                Dim BytesLeft As UInt32 = data_to_write.Length
                Dim Counter As UInt32 = 0
                Do Until BytesLeft = 0
                    If BytesLeft > MAX_USB_BUFFER_SIZE Then
                        Dim Packet(MAX_USB_BUFFER_SIZE - 1) As Byte
                        Array.Copy(data_to_write, BufferIndex, Packet, 0, Packet.Length)
                        DMA_WriteFlash_Block(dma_addr, Packet, prog_mode)
                        dma_addr = dma_addr + MAX_USB_BUFFER_SIZE
                        BufferIndex = BufferIndex + MAX_USB_BUFFER_SIZE
                        BytesLeft = BytesLeft - MAX_USB_BUFFER_SIZE
                        BytesWritten += MAX_USB_BUFFER_SIZE
                    Else
                        Dim Packet(BytesLeft - 1) As Byte
                        Array.Copy(data_to_write, BufferIndex, Packet, 0, Packet.Length)
                        DMA_WriteFlash_Block(dma_addr, Packet, prog_mode)
                        BytesLeft = 0
                    End If
                    Counter += 1
                Loop
            Catch
            End Try
        End Sub

        Private Function DMA_WriteFlash_Block(ByVal dma_addr As UInt32, ByVal data() As Byte, ByVal sub_cmd As CFI.CFI_FLASH_MODE) As Boolean
            Dim setup_data(7) As Byte
            Dim data_count As UInt32 = data.Length
            setup_data(0) = CByte(dma_addr And 255)
            setup_data(1) = CByte((dma_addr >> 8) And 255)
            setup_data(2) = CByte((dma_addr >> 16) And 255)
            setup_data(3) = CByte((dma_addr >> 24) And 255)
            setup_data(4) = CByte(data_count And 255)
            setup_data(5) = CByte((data_count >> 8) And 255)
            setup_data(6) = CByte((data_count >> 16) And 255)
            setup_data(7) = CByte((data_count >> 24) And 255)
            If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data, setup_data.Length) Then Return Nothing
            If Not FCUSB.USB_CONTROL_MSG_OUT(sub_cmd) Then Return Nothing
            Dim write_result As Boolean = FCUSB.USB_BULK_OUT(data)
            If write_result Then FCUSB.USB_WaitForComplete()
            Return write_result
        End Function

#End Region

#Region "Reading/Writing to Memory"

        Public Function ReadMemory(addr As UInt32, width As DATA_WIDTH) As UInt32
            Dim ReadBack As Byte() = New Byte(3) {}
            Select Case Me.Devices(Me.SELECTED_INDEX).ACCESS
                Case JTAG_MEM_ACCESS.EJTAG_DMA
                    Select Case width
                        Case DATA_WIDTH.Word
                            FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_READ_W, ReadBack, addr)
                            Array.Reverse(ReadBack)
                            Return Utilities.Bytes.ToUInteger(ReadBack)
                        Case DATA_WIDTH.HalfWord
                            FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_READ_H, ReadBack, addr)
                            Array.Reverse(ReadBack)
                            Return (CUShort(ReadBack(0)) << 8) + ReadBack(1)
                        Case DATA_WIDTH.Byte
                            If (addr Mod 2) = 0 Then
                                FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_READ_B, ReadBack, addr)
                            Else
                                FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_READ_H, ReadBack, addr)
                            End If
                            Array.Reverse(ReadBack)
                            Return CByte(ReadBack(0) And &HFF)
                    End Select
                Case JTAG_MEM_ACCESS.EJTAG_PRACC
                Case JTAG_MEM_ACCESS.ARM
                    FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_READ_W, ReadBack, addr)
                    Array.Reverse(ReadBack)
                    Return Utilities.Bytes.ToUInteger(ReadBack)
            End Select
            Return 0
        End Function

        Public Sub WriteMemory(ByVal addr As UInt32, ByVal data As UInt32, ByVal width As DATA_WIDTH)
            Dim bits As UInt16 = 32
            If width = DATA_WIDTH.Byte Then
                data = (data << 24) Or (data << 16) Or (data << 8) Or data
                bits = 8
            ElseIf width = DATA_WIDTH.HalfWord Then
                data = (data << 16) Or data
                bits = 16
            End If
            Dim setup_data(8) As Byte
            setup_data(0) = CByte(addr And 255)
            setup_data(1) = CByte((addr >> 8) And 255)
            setup_data(2) = CByte((addr >> 16) And 255)
            setup_data(3) = CByte((addr >> 24) And 255)
            setup_data(4) = CByte(data And 255)
            setup_data(5) = CByte((data >> 8) And 255)
            setup_data(6) = CByte((data >> 16) And 255)
            setup_data(7) = CByte((data >> 24) And 255)
            setup_data(8) = Me.Devices(Me.SELECTED_INDEX).ACCESS
            If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data, setup_data.Length) Then Exit Sub
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_WRITE, Nothing, bits)
        End Sub

        'Reads data from DRAM (optomized for speed)
        Public Function ReadMemory(ByVal Address As UInt32, ByVal count As UInt32) As Byte()
            Dim DramStart As UInt32 = Address
            Dim LargeCount As Integer = count 'The total amount of data we need to read in
            Do Until Address Mod 4 = 0 Or Address = 0
                Address = CUInt(Address - 1)
                LargeCount = LargeCount + 1
            Loop
            Do Until LargeCount Mod 4 = 0
                LargeCount = LargeCount + 1
            Loop 'Now StartAdd2 and ByteLen2 are on bounds of 4
            Dim TotalBuffer(LargeCount - 1) As Byte
            Dim BytesLeft As Integer = LargeCount
            While BytesLeft > 0
                Dim BytesToRead As Integer = BytesLeft
                If BytesToRead > MAX_USB_BUFFER_SIZE Then BytesToRead = MAX_USB_BUFFER_SIZE
                Dim Offset As UInt32 = LargeCount - BytesLeft
                Dim TempBuffer() As Byte = Nothing
                Select Case Me.Devices(Me.SELECTED_INDEX).ACCESS
                    Case JTAG_MEM_ACCESS.EJTAG_DMA
                        TempBuffer = JTAG_ReadMemory(Address + Offset, BytesToRead)
                    Case JTAG_MEM_ACCESS.EJTAG_PRACC
                    Case JTAG_MEM_ACCESS.ARM
                End Select
                If TempBuffer Is Nothing OrElse Not TempBuffer.Length = BytesToRead Then
                    ReDim Preserve TempBuffer(BytesToRead - 1) 'Fill buffer with blank data
                End If
                Array.Copy(TempBuffer, 0, TotalBuffer, Offset, TempBuffer.Length)
                BytesLeft -= BytesToRead
            End While
            Dim OutByte(count - 1) As Byte
            Array.Copy(TotalBuffer, LargeCount - count, OutByte, 0, count)
            Return OutByte
        End Function
        'Writes an unspecified amount of b() into memory (usually DRAM)
        Public Function WriteMemory(ByVal Address As UInt32, ByVal data() As Byte) As Boolean
            Try
                Dim TotalBytes As Integer = data.Length
                Dim WordBytes As Integer = CInt(Math.Floor(TotalBytes / 4) * 4)
                Dim DataToWrite(WordBytes - 1) As Byte 'Word aligned
                Array.Copy(data, DataToWrite, WordBytes)
                Dim BytesRemaining As Integer = DataToWrite.Length
                Dim BytesWritten As Integer = 0
                Dim BlockToWrite() As Byte
                While (BytesRemaining > 0)
                    If BytesRemaining > MAX_USB_BUFFER_SIZE Then
                        ReDim BlockToWrite(MAX_USB_BUFFER_SIZE - 1)
                    Else
                        ReDim BlockToWrite(BytesRemaining - 1)
                    End If
                    Array.Copy(DataToWrite, BytesWritten, BlockToWrite, 0, BlockToWrite.Length)
                    Select Case Me.Devices(Me.SELECTED_INDEX).ACCESS
                        Case JTAG_MEM_ACCESS.EJTAG_DMA
                            JTAG_WriteMemory(Address, BlockToWrite)
                        Case JTAG_MEM_ACCESS.EJTAG_PRACC
                        Case JTAG_MEM_ACCESS.ARM
                    End Select
                    BytesWritten += BlockToWrite.Length
                    BytesRemaining -= BlockToWrite.Length
                    Address += CUInt(BlockToWrite.Length)
                End While
                If Not TotalBytes = WordBytes Then 'Writes the bytes left over is less than 4
                    For i = 0 To (TotalBytes - WordBytes) - 1
                        WriteMemory(CUInt(Address + WordBytes + i), data(WordBytes + i), DATA_WIDTH.Byte)
                    Next
                End If
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Function JTAG_WriteMemory(ByVal dma_addr As UInteger, ByVal data_out() As Byte) As Boolean
            Dim setup_data(8) As Byte
            Dim data_len As UInt32 = data_out.Length
            setup_data(0) = CByte(dma_addr And 255)
            setup_data(1) = CByte((dma_addr >> 8) And 255)
            setup_data(2) = CByte((dma_addr >> 16) And 255)
            setup_data(3) = CByte((dma_addr >> 24) And 255)
            setup_data(4) = CByte(data_len And 255)
            setup_data(5) = CByte((data_len >> 8) And 255)
            setup_data(6) = CByte((data_len >> 16) And 255)
            setup_data(7) = CByte((data_len >> 24) And 255)
            setup_data(8) = Me.Devices(Me.SELECTED_INDEX).ACCESS
            If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data, setup_data.Length) Then Return Nothing
            If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_WRITEMEM) Then Return Nothing 'Sends setup command and data
            Return FCUSB.USB_BULK_OUT(data_out)
        End Function

        Private Function JTAG_ReadMemory(ByVal dma_addr As UInt32, ByVal count As UInt32) As Byte()
            Dim setup_data(8) As Byte
            setup_data(0) = CByte(dma_addr And 255)
            setup_data(1) = CByte((dma_addr >> 8) And 255)
            setup_data(2) = CByte((dma_addr >> 16) And 255)
            setup_data(3) = CByte((dma_addr >> 24) And 255)
            setup_data(4) = CByte(count And 255)
            setup_data(5) = CByte((count >> 8) And 255)
            setup_data(6) = CByte((count >> 16) And 255)
            setup_data(7) = CByte((count >> 24) And 255)
            setup_data(8) = Me.Devices(Me.SELECTED_INDEX).ACCESS
            Dim data_back(count - 1) As Byte
            If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data, setup_data.Length) Then Return Nothing
            If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_READMEM) Then Return Nothing 'Sends setup command and data
            If Not FCUSB.USB_BULK_IN(data_back) Then Return Nothing
            Return data_back
        End Function



#End Region

#Region "CFI over JTAG"
        Private WithEvents CFI_IF As New CFI.FLASH_INTERFACE() 'Handles all of the CFI flash protocol

        Public Function CFI_Detect(ByVal DMA_ADDR As UInt32) As Boolean
            Return CFI_IF.DetectFlash(DMA_ADDR)
        End Function

        Public Sub CFI_ReadMode()
            CFI_IF.Read_Mode()
        End Sub

        Public Sub CFI_EraseDevice()
            CFI_IF.EraseBulk()
        End Sub

        Public Sub CFI_WaitUntilReady()
            CFI_IF.WaitUntilReady()
        End Sub

        Public Function CFI_Sector_Count() As UInt32
            Return CFI_IF.GetFlashSectors
        End Function

        Public Function CFI_GetSectorSize(ByVal sector_ind As UInt32) As UInt32
            Return CFI_IF.GetSectorSize(sector_ind)
        End Function

        Public Function CFI_FindSectorBase(ByVal sector_ind As UInt32) As UInt32
            Return CFI_IF.FindSectorBase(sector_ind)
        End Function

        Public Function CFI_Sector_Erase(ByVal sector_ind As UInt32) As Boolean
            CFI_IF.Sector_Erase(sector_ind)
            Return True
        End Function

        Public Function CFI_ReadFlash(ByVal Address As UInt32, ByVal count As UInt32) As Byte()
            Return CFI_IF.ReadData(Address, count)
        End Function

        Public Function CFI_Sector_Write(ByVal Address As UInt32, ByVal data_out() As Byte) As Boolean
            CFI_IF.WriteSector(Address, data_out)
            Return True
        End Function

        Public Function CFI_GetFlashName() As String
            Return CFI_IF.FlashName
        End Function

        Public Function CFI_GetFlashSize() As UInt32
            Return CFI_IF.FlashSize
        End Function

        Public Sub CFI_GetFlashPart(ByRef MFG As Byte, ByRef CHIPID As UInt32)
            MFG = CFI_IF.MyDeviceID.MFG
            CHIPID = (CFI_IF.MyDeviceID.ID1 << 16) Or CFI_IF.MyDeviceID.ID2
        End Sub

#Region "CFI EVENTS"

        Private Sub CFIEVENT_WriteB(ByVal addr As UInt32, ByVal data As Byte) Handles CFI_IF.Memory_Write_B
            WriteMemory(addr, data, DATA_WIDTH.Byte)
        End Sub

        Private Sub CFIEVENT_WriteH(ByVal addr As UInt32, ByVal data As UInt16) Handles CFI_IF.Memory_Write_H
            WriteMemory(addr, data, DATA_WIDTH.HalfWord)
        End Sub

        Private Sub CFIEVENT_WriteW(ByVal addr As UInt32, ByVal data As UInt32) Handles CFI_IF.Memory_Write_W
            WriteMemory(addr, data, DATA_WIDTH.Word)
        End Sub

        Private Sub CFIEVENT_ReadB(ByVal addr As UInt32, ByRef data As Byte) Handles CFI_IF.Memory_Read_B
            data = ReadMemory(addr, DATA_WIDTH.Byte)
        End Sub

        Private Sub CFIEVENT_ReadH(ByVal addr As UInt32, ByRef data As UInt16) Handles CFI_IF.Memory_Read_H
            data = ReadMemory(addr, DATA_WIDTH.HalfWord)
        End Sub

        Private Sub CFIEVENT_ReadW(ByVal addr As UInt32, ByRef data As UInt32) Handles CFI_IF.Memory_Read_W
            data = ReadMemory(addr, DATA_WIDTH.Word)
        End Sub

        Private Sub CFIEVENT_SetBase(ByVal base As UInt32) Handles CFI_IF.SetBaseAddress
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SET_OPTION, BitConverter.GetBytes(base), 5)
        End Sub

        Private Sub CFIEVENT_ReadFlash(ByVal dma_addr As UInt32, ByRef data() As Byte) Handles CFI_IF.ReadFlash
            data = JTAG_ReadMemory(dma_addr, data.Length)
        End Sub

        Private Sub CFIEVENT_WriteFlash(ByVal dma_addr As UInt32, ByVal data_to_write() As Byte, ByVal prog_mode As CFI.CFI_FLASH_MODE) Handles CFI_IF.WriteFlash
            DMA_WriteFlash(dma_addr, data_to_write, prog_mode)
        End Sub

        Private Sub CFIEVENT_WriteConsole(ByVal message As String) Handles CFI_IF.WriteConsole
            WriteConsole(message)
        End Sub

#End Region

#End Region

#Region "SPI over JTAG"
        Public SPI_Part As FlashMemory.SPI_NOR_FLASH 'Contains the SPI Flash definition
        Private SPI_JTAG_PROTOCOL As JTAG_SPI_Type
        Private SPI_MIPS_JTAG_IF As MIPS_SPI_API
        'Returns TRUE if the JTAG can detect a connected flash to the SPI port
        Public Function SPI_Detect(spi_if As JTAG_SPI_Type) As Boolean
            SPI_JTAG_PROTOCOL = spi_if
            If spi_if = JTAG_SPI_Type.ATH_MIPS Or spi_if = JTAG_SPI_Type.BCM_MIPS Then
                SPI_MIPS_JTAG_IF = New MIPS_SPI_API(SPI_JTAG_PROTOCOL)
                Dim reg As UInt32 = SPI_SendCommand(SPI_MIPS_JTAG_IF.READ_ID, 1, 3)
                WriteConsole(String.Format("JTAG: SPI register returned {0}", "0x" & Hex(reg)))
                If reg = 0 OrElse reg = &HFFFFFFFFUI Then
                    Return False
                Else
                    Dim MFG_BYTE As Byte = CByte((reg And &HFF0000) >> 16)
                    Dim PART_ID As UInt16 = CUShort(reg And &HFFFF)
                    SPI_Part = FlashDatabase.FindDevice(MFG_BYTE, PART_ID, 0, FlashMemory.MemoryType.SERIAL_NOR)
                    If SPI_Part IsNot Nothing Then
                        WriteConsole(String.Format("JTAG: SPI flash detected ({0})", SPI_Part.NAME))
                        If SPI_JTAG_PROTOCOL = JTAG_SPI_Type.BCM_MIPS Then
                            WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, 0, DATA_WIDTH.Word)
                            Dim data() As Byte = SPI_ReadFlash(0, 4)
                        End If
                        Return True
                    Else
                        WriteConsole("JTAG: SPI flash not found in database")
                    End If
                End If
            ElseIf spi_if = JTAG_SPI_Type.BCM_ARM Then
                If Not FCUSB.HWBOARD = USB.FCUSB_BOARD.Professional Then
                    WriteConsole("JTAG: Error, ARM extension is only supported on FlashcatUSB Professional")
                    Return False
                End If
                'This is a test
                'WriteMemory(&H1B000400, &H5A010203, DATA_WIDTH.Word)
                'Dim result As UInt32 = ReadMemory(&H1B000400, DATA_WIDTH.Word)

                'Dim QSPI_MSPI_BASE As UInt32 = &H18027000
                'Dim QSPI_mspi_SPCR1_LSB As UInt32 = QSPI_MSPI_BASE + &H208
                'Dim QSPI_mspi_SPCR1_MSB As UInt32 = QSPI_MSPI_BASE + &H20C
                'Dim QSPI_mspi_NEWQP As UInt32 = QSPI_MSPI_BASE + &H210
                'Dim QSPI_mspi_ENDQP As UInt32 = QSPI_MSPI_BASE + &H214
                'Dim QSPI_mspi_SPCR2 As UInt32 = QSPI_MSPI_BASE + &H218
                'Dim QSPI_mspi_SPCR0_LSB As UInt32 = QSPI_MSPI_BASE + &H200
                'Dim QSPI_mspi_SPCR0_MSB As UInt32 = QSPI_MSPI_BASE + &H204
                'Dim QSPI_bspi_registers_MAST_N_BOOT_CTRL As UInt32 = QSPI_MSPI_BASE + &H8

                'Dim cru_addr As UInt32 = &H1803E000
                'Dim cru_reg As UInt32 = ReadMemory(cru_addr, DATA_WIDTH.Word)
                'WriteMemory(cru_addr, cru_reg And (Not cru_reg), DATA_WIDTH.Word) 'BSPI clock configuration

                'WriteMemory(QSPI_mspi_SPCR1_LSB, 0, DATA_WIDTH.Word)
                'WriteMemory(QSPI_mspi_SPCR1_MSB, 0, DATA_WIDTH.Word)
                'WriteMemory(QSPI_mspi_NEWQP, 0, DATA_WIDTH.Word)
                'WriteMemory(QSPI_mspi_ENDQP, 0, DATA_WIDTH.Word)
                'WriteMemory(QSPI_mspi_SPCR2, 0, DATA_WIDTH.Word)
                'WriteMemory(QSPI_mspi_SPCR0_LSB, 0, DATA_WIDTH.Word)
                'WriteMemory(QSPI_mspi_SPCR0_MSB, 0, DATA_WIDTH.Word)
                'WriteMemory(QSPI_bspi_registers_MAST_N_BOOT_CTRL, 0, DATA_WIDTH.Word)

                'Dim uboot() As Byte = Utilities.GetResourceAsBytes("arm_spi.bin")
                'Dim UBOOT_ADDR As UInt32 = &H1B000400 'Where in RAM the ARM exec is stored
                'Dim SPI_ADDR As UInt32 = &H1B001000 'Where in RAM SPI data is stored
                'WriteMemory(UBOOT_ADDR, uboot) 'This stores our ARM code

            End If
            Return False
        End Function
        'Includes chip-specific API for connecting JTAG->SPI
        Private Structure MIPS_SPI_API
            Public Property BASE As UInt32
            'Register addrs
            Public Property REG_CNTR As UInt32
            Public Property REG_DATA As UInt32
            Public Property REG_OPCODE As UInt32
            'Control CODES
            Public Property CTL_Start As UInt32
            Public Property CTL_Busy As UInt32
            'OP CODES
            Public Property READ_ID As UShort '16 bits
            Public Property WREN As UShort
            Public Property SECTORERASE As UShort
            Public Property RD_STATUS As UShort
            Public Property PAGEPRG As UShort

            Sub New(ByVal spi_type As JTAG_SPI_Type)
                Me.BASE = &H1FC00000UI
                Select Case spi_type
                    Case JTAG_SPI_Type.BCM_MIPS
                        REG_CNTR = &H18000040
                        REG_OPCODE = &H18000044
                        REG_DATA = &H18000048
                        CTL_Start = &H80000000UI
                        CTL_Busy = &H80000000UI
                        READ_ID = &H49F
                        WREN = &H6
                        SECTORERASE = &H2D8
                        RD_STATUS = &H105
                        PAGEPRG = &H402
                    Case JTAG_SPI_Type.BCM_ARM
                        REG_CNTR = &H11300000
                        REG_OPCODE = &H11300004
                        REG_DATA = &H11300008
                        CTL_Start = &H11300100
                        CTL_Busy = &H11310000
                        READ_ID = &H9F
                        WREN = &H6
                        SECTORERASE = &HD8
                        RD_STATUS = &H5
                        PAGEPRG = &H2
                End Select
            End Sub

        End Structure

        Public Function SPI_EraseBulk() As Boolean
            Try
                WriteConsole("Erasing entire SPI flash (this could take a moment)")
                WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, 0, DATA_WIDTH.Word) 'Might need to be for Ath too
                SPI_SendCommand(SPI_MIPS_JTAG_IF.WREN, 1, 0)
                WriteMemory(SPI_MIPS_JTAG_IF.REG_OPCODE, SPI_MIPS_JTAG_IF.BASE, DATA_WIDTH.Word)
                WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, &H800000C7UI, DATA_WIDTH.Word) 'Might need to be for Ath too
                SPI_WaitUntilReady()
                WriteConsole("Erase operation complete!")
                Return True
            Catch ex As Exception
            End Try
            Return False
        End Function

        Public Function SPI_SectorErase(ByVal secotr_ind As UInt32) As Boolean
            Try
                Dim Addr24 As UInteger = SPI_FindSectorBase(secotr_ind)
                Dim reg As UInt32 = SPI_GetControlReg()
                If SPI_JTAG_PROTOCOL = JTAG_SPI_Type.BCM_MIPS Then
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, 0, DATA_WIDTH.Word)
                    SPI_SendCommand(SPI_MIPS_JTAG_IF.WREN, 1, 0)
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_OPCODE, Addr24 Or SPI_MIPS_JTAG_IF.SECTORERASE, DATA_WIDTH.Word)
                    reg = (reg And &HFFFFFF00) Or SPI_MIPS_JTAG_IF.SECTORERASE Or SPI_MIPS_JTAG_IF.CTL_Start
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, reg, DATA_WIDTH.Word)
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, 0, DATA_WIDTH.Word)
                ElseIf SPI_JTAG_PROTOCOL = JTAG_SPI_Type.ATH_MIPS Then
                    SPI_SendCommand(SPI_MIPS_JTAG_IF.WREN, 1, 0)
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_OPCODE, (Addr24 << 8) Or SPI_MIPS_JTAG_IF.SECTORERASE, DATA_WIDTH.Word)
                    reg = (reg And &HFFFFFF00) Or &H4 Or SPI_MIPS_JTAG_IF.CTL_Start
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, reg, DATA_WIDTH.Word)
                End If
                SPI_WaitUntilReady()
                SPI_GetControlReg()
            Catch ex As Exception
            End Try
            Return True
        End Function

        Public Function SPI_WriteData(ByVal Address As UInt32, ByVal data() As Byte) As Boolean
            Try
                Do Until data.Length Mod 4 = 0
                    ReDim Preserve data(data.Length)
                    data(data.Length - 1) = 255
                Loop
                Dim TotalBytes As Integer = data.Length
                Dim WordBytes As Integer = CInt(Math.Floor(TotalBytes / 4) * 4)
                Dim DataToWrite(WordBytes - 1) As Byte 'Word aligned
                Array.Copy(data, DataToWrite, WordBytes)
                Dim BytesRemaining As Integer = DataToWrite.Length
                Dim BytesWritten As Integer = 0
                Dim BlockToWrite() As Byte
                While BytesRemaining > 0
                    If BytesRemaining > MAX_USB_BUFFER_SIZE Then
                        ReDim BlockToWrite(MAX_USB_BUFFER_SIZE - 1)
                    Else
                        ReDim BlockToWrite(BytesRemaining - 1)
                    End If
                    Array.Copy(DataToWrite, BytesWritten, BlockToWrite, 0, BlockToWrite.Length)
                    SPI_WriteFlash(Address, BlockToWrite)
                    BytesWritten += BlockToWrite.Length
                    BytesRemaining -= BlockToWrite.Length
                    Address += CUInt(BlockToWrite.Length)
                End While
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function SPI_SendCommand(ByVal SPI_OPCODE As UInt16, ByVal BytesToWrite As UInt32, ByVal BytesToRead As UInt32) As UInt32
            WriteMemory(&HBF000000UI, 0, DATA_WIDTH.Word)
            If SPI_JTAG_PROTOCOL = JTAG_SPI_Type.BCM_MIPS Then
                WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, 0, DATA_WIDTH.Word)
            End If
            Dim reg As UInt32 = SPI_GetControlReg() 'Zero
            Select Case SPI_JTAG_PROTOCOL
                Case JTAG_SPI_Type.BCM_MIPS
                    reg = (reg And &HFFFFFF00UI) Or SPI_OPCODE Or SPI_MIPS_JTAG_IF.CTL_Start
                Case JTAG_SPI_Type.ATH_MIPS
                    reg = (reg And &HFFFFFF00UI) Or BytesToWrite Or (BytesToRead << 4) Or SPI_MIPS_JTAG_IF.CTL_Start
            End Select
            WriteMemory(SPI_MIPS_JTAG_IF.REG_OPCODE, SPI_OPCODE, DATA_WIDTH.Word)
            WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, reg, DATA_WIDTH.Word)
            reg = SPI_GetControlReg()
            If (BytesToRead > 0) Then
                reg = ReadMemory(SPI_MIPS_JTAG_IF.REG_DATA, DATA_WIDTH.Word)
                Select Case BytesToRead
                    Case 1
                        reg = (reg And 255)
                    Case 2
                        reg = ((reg And 255) << 8) Or ((reg And &HFF00) >> 8)
                    Case 3
                        reg = ((reg And 255) << 16) Or (reg And &HFF00) Or ((reg And &HFF0000) >> 16)
                    Case 4
                        reg = ((reg And 255) << 24) Or ((reg And &HFF00) << 8) Or ((reg And &HFF0000) >> 8) Or ((reg And &HFF000000) >> 24)
                End Select
                Return reg
            End If
            Return 0
        End Function
        'Returns the total number of sectors
        Public Function SPI_Sector_Count() As UInt32
            Dim secSize As UInt32 = &H10000 '64KB
            Dim totalsize As UInt32 = SPI_Part.FLASH_SIZE
            Return CUInt(totalsize / secSize)
        End Function

        Public Function SPI_WriteSector(ByVal sector_index As UInt32, ByVal data() As Byte) As Boolean
            Dim Addr As UInteger = SPI_FindSectorBase(sector_index)
            SPI_WriteData(Addr, data)
            Return True
        End Function

        Public Function SPI_FindSectorBase(ByVal sectorInt As Integer) As UInteger
            Return CUInt(SPI_GetSectorSize(0) * sectorInt)
        End Function

        Public Function SPI_GetSectorSize(ByVal sector_ind As UInt32) As UInteger
            Return &H10000
        End Function

        Public Function SPI_GetControlReg() As UInt32
            Dim i As Integer = 0
            Dim reg As UInt32
            Do
                If Not i = 0 Then Threading.Thread.Sleep(25)
                reg = ReadMemory(SPI_MIPS_JTAG_IF.REG_CNTR, DATA_WIDTH.Word)
                i = i + 1
                If i = 20 Then Return 0
            Loop While ((reg And SPI_MIPS_JTAG_IF.CTL_Busy) > 0)
            Return reg
        End Function

        Public Sub SPI_WaitUntilReady()
            Dim reg As UInt32
            Do
                reg = SPI_SendCommand(SPI_MIPS_JTAG_IF.RD_STATUS, 1, 4)
            Loop While ((reg And 1) > 0)
        End Sub

        Public Function SPI_ReadFlash(ByVal addr32 As UInt32, ByVal count As UInt32) As Byte()
            Return ReadMemory(SPI_MIPS_JTAG_IF.BASE + addr32, count)
        End Function

        Private Function SPI_WriteFlash(ByVal addr32 As UInt32, ByVal data_out() As Byte) As Boolean
            Dim OpCode As Byte
            Select Case SPI_JTAG_PROTOCOL
                Case JTAG_SPI_Type.BCM_MIPS
                    OpCode = USB.USBREQ.JTAG_FLASHSPI_BRCM
                Case JTAG_SPI_Type.ATH_MIPS
                    OpCode = USB.USBREQ.JTAG_FLASHSPI_ATH
            End Select
            Dim setup_data(7) As Byte
            Dim data_len As UInt32 = data_out.Length
            setup_data(0) = CByte(addr32 And 255)
            setup_data(1) = CByte((addr32 >> 8) And 255)
            setup_data(2) = CByte((addr32 >> 16) And 255)
            setup_data(3) = CByte((addr32 >> 24) And 255)
            setup_data(4) = CByte(data_len And 255)
            setup_data(5) = CByte((data_len >> 8) And 255)
            setup_data(6) = CByte((data_len >> 16) And 255)
            setup_data(7) = CByte((data_len >> 24) And 255)
            If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data, setup_data.Length) Then Return Nothing
            If Not FCUSB.USB_CONTROL_MSG_OUT(OpCode) Then Return Nothing 'Sends setup command and data
            Return FCUSB.USB_BULK_OUT(data_out)
        End Function

        Public Enum JTAG_SPI_Type As Integer
            BCM_MIPS = 1
            ATH_MIPS = 2
            BCM_ARM = 3
        End Enum

#End Region


#Region "Public function"
        Public Sub Select_Device(ByVal device_index As Integer)
            Try
                Me.SELECTED_INDEX = device_index
                WriteConsole(String.Format("JTAG Device selected: {0} (IR length {1})", device_index, Me.Devices(Me.SELECTED_INDEX).IR_LEN.ToString))
                Dim Leading_Bits As UInt16 = (CUShort(Devices(Me.SELECTED_INDEX).IR_LEADING) << 8) Or CUShort(Devices(Me.SELECTED_INDEX).IR_TRAILING)
                Dim select_value As UInt32 = (CUInt(Leading_Bits) << 16) Or (Devices(Me.SELECTED_INDEX).ACCESS << 8) Or (Me.Devices(Me.SELECTED_INDEX).IR_LEN And 255)
                If CPLD_PROG_MODE Then
                    FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.CPLD_JTAG_SELECT, Nothing, select_value)
                Else
                    FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SELECT, Nothing, select_value)
                End If
                Utilities.Sleep(10)
            Catch ex As Exception
            End Try
        End Sub

        Public Function Reset_StateMachine() As Boolean
            Try
                If TAP Is Nothing Then
                    Return FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_RESET)
                Else
                    TAP.Reset()
                    Return True
                End If
            Catch ex As Exception
            End Try
            Return False
        End Function

        Public Sub TAP_GotoState(ByVal J_STATE As JTAG_MACHINE_STATE)
            Try
                If TAP Is Nothing Then
                    FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_GOTO_STATE, Nothing, J_STATE)
                Else
                    TAP.GotoState(J_STATE)
                End If
            Catch ex As Exception
            End Try
        End Sub

        Public Sub Tap_Toggle(ByVal count As UInt32)
            Try
                If CPLD_PROG_MODE Then
                    FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.CPLD_JTAG_TOGGLE, Nothing, count)
                Else
                    FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_TOGGLE, Nothing, count)
                End If
                FCUSB.USB_WaitForComplete()
            Catch ex As Exception
            End Try
        End Sub
        'Writes to the instruction-register and then shifts out 32-bits from the DR
        Public Function ReadWriteReg32(ByVal ir_value As UInt32, Optional dr_value As UInt32 = 0) As UInt32
            If TAP Is Nothing Then
                Dim ir_data() As Byte = Utilities.Bytes.FromUInt32(ir_value, False)
                Dim dr_data() As Byte = Utilities.Bytes.FromUInt32(dr_value, False)
                Dim setup(11) As Byte
                Dim return_data(3) As Byte
                Array.Copy(ir_data, 0, setup, 0, 4)
                Array.Copy(dr_data, 0, setup, 8, 4)
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_REGISTERS, setup, 32)
                Utilities.Sleep(5)
                FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.READ_PAYLOAD, return_data)
                Return Utilities.Bytes.ToUInteger(return_data)
            Else
                Dim byte_count As Integer = Math.Ceiling(Devices(Me.SELECTED_INDEX).IR_LEN / 8)
                If (byte_count = 1) Then
                    TAP.ShiftIR({(ir_value And 255)}, Nothing, Devices(Me.SELECTED_INDEX).IR_LEN, True)
                ElseIf (byte_count = 2) Then
                    TAP.ShiftIR({((ir_value >> 8) And 255), (ir_value And 255)}, Nothing, Devices(Me.SELECTED_INDEX).IR_LEN, True)
                End If
                Dim data_in() As Byte = Utilities.Bytes.FromUInt32(dr_value)
                Dim data_out() As Byte = Nothing
                TAP.ShiftDR(data_in, data_out, 32)
                TAP.GotoState(JTAG_MACHINE_STATE.Select_DR) 'Default parking
                Return Utilities.Bytes.ToUInteger(data_out)
            End If
        End Function

        Public Sub ShiftTDI(ByVal BitCount As UInt32, ByVal tdi_bits() As Byte, ByRef tdo_bits() As Byte, exit_tms As Boolean)
            Dim byte_count As Integer = Math.Ceiling(BitCount / 8)
            ReDim tdo_bits(byte_count - 1)
            Dim BytesLeft As Integer = tdi_bits.Length
            Dim BitsLeft As UInt32 = BitCount
            Dim ptr As Integer = 0
            Do Until BytesLeft = 0
                Dim packet_size As UInt32 = Math.Min(64, BytesLeft)
                Dim packet_data(packet_size - 1) As Byte
                Array.Copy(tdi_bits, ptr, packet_data, 0, packet_data.Length)
                BytesLeft -= packet_size
                Dim bits_size As UInt32 = Math.Min(512, BitsLeft)
                BitsLeft -= bits_size
                If BytesLeft = 0 AndAlso exit_tms Then
                    bits_size = bits_size Or (1 << 16)
                End If
                If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, packet_data, packet_data.Length) Then Exit Sub 'Preloads the TDI/TMS data
                Dim tdo_data(packet_size - 1) As Byte
                If CPLD_PROG_MODE Then
                    If Not FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.CPLD_JTAG_SHIFT_DATA, tdo_data, bits_size) Then Exit Sub  'Shifts data
                Else
                    If Not FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_SHIFT_DATA, tdo_data, bits_size) Then Exit Sub  'Shifts data
                End If
                Array.Copy(tdo_data, 0, tdo_bits, ptr, tdo_data.Length)
                ptr += packet_data.Length
            Loop
        End Sub

        Public Sub ShiftTMS(ByVal BitCount As UInt32, ByVal tms_bits() As Byte)
            Dim BytesLeft As Integer = tms_bits.Length
            Dim BitsLeft As UInt32 = BitCount
            Dim ptr As Integer = 0
            Do Until BytesLeft = 0
                Dim packet_size As UInt32 = Math.Min(64, BytesLeft)
                Dim packet_data(packet_size - 1) As Byte
                Array.Copy(tms_bits, ptr, packet_data, 0, packet_data.Length)
                ptr += packet_data.Length
                BytesLeft -= packet_size
                Dim bits_size As UInt32 = Math.Min(512, BitsLeft)
                BitsLeft -= bits_size
                If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, packet_data, packet_data.Length) Then Exit Sub 'Preloads our TDI/TMS data
                If CPLD_PROG_MODE Then
                    If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.CPLD_JTAG_SHIFT_TMS, Nothing, bits_size) Then Exit Sub  'Shifts data and reads result
                Else
                    If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SHIFT_TMS, Nothing, bits_size) Then Exit Sub  'Shifts data and reads result
                End If
            Loop
        End Sub

#End Region

    End Class

    Public Enum JTAG_SPEED As UInt32
        _10MHZ = 1 'Divider=4: 40MHZ/4=10MHz
        _20MHZ = 0 'Divider=2: 40MHZ/2=20MHz
    End Enum

    Public Enum EJTAG_OPCODE As UInt32 'IR LEN 5
        EXTEST = &H0
        IDCODE = &H1 'Selects Device Identiﬁcation (ID) register
        SAMPLE = &H2 'Free for other use, such as JTAG boundary scan
        IMPCODE = &H3 'Selects Implementation register
        ADDRESS_IR = &H8 'Selects Address register
        DATA_IR = &H9 'Selects Data register
        CONTROL_IR = &HA 'Selects EJTAG Control register
        IR_ALL = &HB 'Selects the Address, Data and EJTAG Control registers
        EJTAGBOOT = &HC 'Makes the processor take a debug exception after rese
        NORMALBOOT = &HD 'Makes the processor execute the reset handler after rese
        FASTDATA = &HE 'Selects the Data and Fastdata registers
        EJWATCH = &H1C
        BYPASS = &H1F 'Select Bypass register
    End Enum

    Public Enum XILINX_OPCODE As UInt32 'IR LEN 8
        INTEST = &H2
        BYPASS = &HFF 'Typically all 1s for the size of the register
        SAMPLE = &H3
        EXTEST = &H0
        IDCODE = &H1
        USERCODE = &HFD
        HIGHZ = &HFC
        ISC_ENABLE_CLAMP = &HE9
        ISC_ENABLE_OTF = &HE4
        ISC_ENABLE = &HE8
        ISC_SRAM_READ = &HE7
        ISC_WRITE = &HE6
        ISC_ERASE = &HED
        ISC_PROGRAM = &HEA
        ISC_READ = &HEE
        ISC_INIT = &HF0
        ISC_DISABLE = &HC0
        TEST_ENABLE = &H11
        BULKPROG = &H12
        ERASE_ALL = &H14
        MVERIFY = &H13
        TEST_DISABLE = &H15
        ISC_NOOP = &HE0
    End Enum
    'IR instructions common on CPLD (such as MAX V) 
    Public Enum ALTERA_OPCODE As UInt32 'IR LEN 10
        EXTEST = &HF
        IDCODE = &H6
        SAMPLE = &H5
        BYPASS = &H3FF
        USERCODE = &H7
        HIGHZ = &HB
        CLAMP = &HA
        USER0 = &HC
        USER1 = &HE
    End Enum

    Public Enum JTAG_MACHINE_STATE As Byte
        TestLogicReset = 0
        RunTestIdle = 1
        Select_DR = 2
        Capture_DR = 3
        Shift_DR = 4
        Exit1_DR = 5
        Pause_DR = 6
        Exit2_DR = 7
        Update_DR = 8
        Select_IR = 9
        Capture_IR = 10
        Shift_IR = 11
        Exit1_IR = 12
        Pause_IR = 13
        Exit2_IR = 14
        Update_IR = 15
    End Enum

    Public Enum PROCESSOR
        MIPS = 1
        ARM = 2
    End Enum

    Public Class JTAG_DEVICE
        Public Property IDCODE As UInt32 'Contains ID_CODEs
        Public Property MANUID As UInt16
        Public Property PARTNU As UInt16
        Public Property VERSION As Short = 0
        Public Property IR_LEN As Integer
        Public Property IR_LEADING As Integer
        Public Property IR_TRAILING As Integer
        Public Property ACCESS As JTAG_MEM_ACCESS
    End Class

    Public Enum DATA_WIDTH As Byte
        [Byte] = 8
        [HalfWord] = 16
        [Word] = 32
    End Enum
    Public Enum JTAG_MEM_ACCESS As Byte
        NONE = 0
        EJTAG_DMA = 1
        EJTAG_PRACC = 2
        ARM = 3
    End Enum

End Namespace

