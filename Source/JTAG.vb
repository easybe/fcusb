'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2021 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
'CONTACT EMAIL: support@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'ACKNOWLEDGEMENT: USB driver functionality provided by LibUsbDotNet (sourceforge.net/projects/libusbdotnet)

Imports FlashcatUSB.FlashMemory

Namespace JTAG

    Public Class JTAG_IF
        Public FCUSB As USB.FCUSB_DEVICE
        Public Devices As New List(Of JTAG_DEVICE)
        Public Property Chain_IsValid As Boolean = False 'Indicates if the chain has all BSDL loaded
        Public Property Chain_BitLength As Byte 'Number of total bits in the TDI-TDO through IR REG
        Public Property Chain_SelectedIndex As Integer 'Selected device in the chain

        Public Property IR_LENGTH As Byte 'Number of bits of the IR register for the selected device
        Public Property IR_LEADING As Byte 'Number of bits after the IR register
        Public Property IR_TRAILING As Byte 'Number of bits before the IR register
        Public Property DR_LEADING As Byte 'Number of bits after the DR register
        Public Property DR_TRAILING As Byte 'Number of bits before the DR register

        Public Property TCK_SPEED As JTAG_SPEED = JTAG_SPEED._10MHZ

        Sub New(parent_if As USB.FCUSB_DEVICE)
            FCUSB = parent_if
        End Sub
        'Connects to the target device
        Public Function Init() As Boolean
            Try
                BSDL_Init()
                Me.Chain_BitLength = 0
                Me.Chain_SelectedIndex = 0
                Me.Chain_IsValid = False
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_INIT, Nothing, CUInt(Me.TCK_SPEED))
                JTAG_PrintClockSpeed()
                Utilities.Sleep(200) 'We need to wait
                If Not TAP_Detect() Then Return False
                Chain_Print()
                Chain_Validate()
                If Devices(0).IDCODE = &HBA02477 Then BroadcomM7()
                Return True
            Catch ex As Exception
            End Try
            Return False
        End Function

        Private Sub BroadcomM7()
            Dim tdo(3) As Byte
            Dim tdi(3) As Byte
            Dim tdo32 As UInt32 = 0
            Dim REG_VALUE As UInt32
            Reset_StateMachine() 'Now at Select_DR
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR)
            ShiftTDI(4, {CByte(Devices(0).BSDL.IDCODE)}, Nothing, True)
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            ShiftTDI(32, {0, 0, 0, 0}, tdo, True)
            TAP_GotoState(JTAG_MACHINE_STATE.Update_DR)
            Array.Reverse(tdo)
            Dim ARM_ID As UInt32 = Utilities.Bytes.ToUInt32(tdo) '0x0BA02477
            PrintConsole("ARM ID code: 0x" & Conversion.Hex(ARM_ID).PadLeft(8, "0"c))
            ARM_MDAP_INIT()
            REG_VALUE = CTRLSTAT.CSYSPWRUPREQ Or CTRLSTAT.CDBGPWRUPREQ Or CTRLSTAT.STICKYERR
            ARM_DPACC(REG_VALUE, ARM_DP_REG.CTRL_STAT, ARM_RnW.WR)
            ARM_APACC(&H0, ARM_AP_REG.IDR, ARM_RnW.RD)
            Dim AHB_AP As UInt32 = ARM_APACC(&H0, ARM_AP_REG.IDR, ARM_RnW.RD, False) '0x04770041
            PrintConsole("AHB-AP (IDR: 0x" & Conversion.Hex(AHB_AP).PadLeft(8, "0"c) & ")")
            ARM_APACC(&H0, ARM_AP_REG.BASE, ARM_RnW.RD)
            Dim BASE_REG As UInt32 = ARM_APACC(&H0, ARM_AP_REG.BASE, ARM_RnW.RD, False) '0xE00FB003

            'Dim d1, d2, d3, d4 As UInt32
            'Dim r1, r2, r3, r4 As ARM_DP_REG
            'Dim w1, w2, w3, w4 As ARM_RnW

            'ARM_DP_Decode(&H280000102, d1, r1, w1)
            'ARM_DP_Decode(&H784, d2, r2, w2)
            'ARM_AP_Decode(&H5, d3, r3, w3)
            'ARM_DP_Decode(&H7, d4, r4, w4)
            'AHB-AP
            '"PPB ROM Table" at 0xe00ff000
            '0xE00FE000	Cortex-M7 PPB ROM Table
            '0xE00FB000 <-- from our debugger
            'Find ROM base (0xE00FB000)
        End Sub

#Region "EPC2 Support"

        Public Delegate Sub EPC2_ProgressCallback(i As Integer)

        Public Function EPC2_ReadBinary(Optional progress_update As EPC2_ProgressCallback = Nothing) As Byte()
            Reset_StateMachine()
            If Not EPC2_Check_JedecID() Then
                Return Nothing
            End If
            JSP_ShiftIR({0, &H44}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            JSP_ToggleClock(10000)
            If Not EPC2_Check_SiliconID() Then
                Return Nothing
            End If
            JSP_ShiftIR({&H1, &HA}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            JSP_ToggleClock(50)
            JSP_ShiftIR({&H1, &H22}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            JSP_ToggleClock(200)
            Dim tdi_high_64() As Byte = {255, 255, 255, 255, 255, 255, 255, 255}
            Dim epc2_data(211959) As Byte
            Dim loops As Integer = (epc2_data.Length \ 8)
            For i = 0 To loops - 1
                Dim tdo_out(7) As Byte '64 bits / 8 bytes at a time
                JSP_ShiftDR(tdi_high_64, tdo_out, 64, True) : JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
                Array.Reverse(tdo_out)
                Array.Copy(tdo_out, 0, epc2_data, i * 8, 8)
                If (progress_update IsNot Nothing) Then
                    progress_update.DynamicInvoke(CInt(((i + 1) / loops) * 100))
                End If
            Next
            JSP_ShiftIR({&H0, &H3E}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            JSP_ToggleClock(200)
            Dim success As Boolean = EPC2_GetStatus()
            Return epc2_data
        End Function

        Private Function EPC2_Check_JedecID() As Boolean
            Dim id_code(3) As Byte
            JSP_ShiftIR({0, &H59}, Nothing, -1, True)
            JSP_ShiftDR({255, 255, 255, 255}, id_code, 32, True)
            If Not ((id_code(0) = 1) And (id_code(1) = 0) And (id_code(2) = &H20) And (id_code(3) = &HDD)) Then 'Check ID CODE
                PrintConsole("JTAG EPC2 error: device not detected")
                Return False
            End If
            Return True
        End Function

        Private Function EPC2_Check_SiliconID() As Boolean
            Dim id_code(3) As Byte
            JSP_ShiftIR({0, &H42}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            JSP_ToggleClock(50)
            JSP_ShiftDR({255, 255, 255, 255}, id_code, 32, True) : JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            If Not ((id_code(0) = &H41) And (id_code(1) = &H39) And (id_code(2) = &H38)) Then 'Check ID CODE
                PrintConsole("JTAG EPC2 error: silicon ID failed")
                Return False
            End If
            Return True
        End Function

        Public Function EPC2_Erase() As Boolean
            Reset_StateMachine()
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            JSP_ShiftIR({0, &H44}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            JSP_ToggleClock(10000)
            If Not EPC2_Check_SiliconID() Then
                PrintConsole("EPC2 Error: silicon ID check failed") : Return False
            End If
            PrintConsole("Performing erase opreation on EPC2 device")
            JSP_ShiftIR({&H1, &H92}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            JSP_ToggleClock(440000000UI)
            Return EPC2_GetStatus()
        End Function

        Private Function EPC2_GetStatus() As Boolean
            Dim status(0) As Byte
            JSP_ShiftIR({&H0, &H3E}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_ToggleClock(200)
            JSP_ShiftDR({7}, status, 3, True) : JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            Return (status(0) = 0)
        End Function

        Public Function EPC2_WriteBinary(rbf() As Byte, ctr_reg() As Byte, Optional progress_update As EPC2_ProgressCallback = Nothing) As Boolean
            PrintConsole("Programming EPC2 device with raw binary file (RBF)")
            If (rbf.Length > 211960) Then
                PrintConsole("JTAG EPC2 error: raw binary file is too large to fit into memory")
                Return Nothing
            End If
            Dim epc2_data() As Byte
            Dim extra As Integer = 8 - (rbf.Length Mod 8)
            If (extra > 0) Then
                ReDim epc2_data(rbf.Length + extra - 1)
                Array.Copy(rbf, 0, epc2_data, 0, rbf.Length)
                For i = 0 To extra - 1
                    epc2_data(rbf.Length + i) = 255
                Next
            Else
                ReDim epc2_data(rbf.Length - 1)
                Array.Copy(rbf, 0, epc2_data, 0, rbf.Length)
            End If
            ReDim Preserve ctr_reg(7)
            ctr_reg(7) = &H20
            ctr_reg(6) = &HBD
            ctr_reg(5) = &HCA
            ctr_reg(4) = &HFF 'or &HBF
            Reset_StateMachine()
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            JSP_ShiftIR({0, &H44}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            JSP_ToggleClock(10000)
            If Not EPC2_Check_SiliconID() Then
                PrintConsole("EPC2 Error: silicon ID check failed") : Return False
            End If
            'ERASE
            PrintConsole("Performing erase opreation on EPC2 device")
            JSP_ShiftIR({&H1, &H92}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            JSP_ToggleClock(440000000UI)
            If Not EPC2_GetStatus() Then
                PrintConsole("EPC2 Error: unable to erase device") : Return False
            End If
            'PROGRAM
            PrintConsole("Programming EPC2 device")
            JSP_ShiftIR({&H0, &H6}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_ToggleClock(50)
            JSP_ShiftDR(CType(ctr_reg.Clone(), Byte()), Nothing, 64, True) : JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            JSP_ShiftIR({&H1, &HA}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_ToggleClock(50)
            JSP_ShiftIR({&H1, &H96}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_ToggleClock(9000)
            Dim loops As Integer = (epc2_data.Length \ 8)
            For i = 0 To loops - 1
                Dim tdi_in(7) As Byte
                Array.Copy(epc2_data, i * 8, tdi_in, 0, 8)
                Array.Reverse(tdi_in)
                JSP_ShiftDR(tdi_in, Nothing, 64, True) : JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
                JSP_ToggleClock(9000)
                If progress_update IsNot Nothing Then
                    Dim progress As Integer = CInt(((i + 1) / loops) * 100)
                    progress_update.DynamicInvoke(progress)
                End If
            Next
            Dim was_sucessful As Boolean = EPC2_GetStatus()
            If Not was_sucessful Then
                PrintConsole("EPC2 Error: Programming failed") : Return False
            End If
            'PROGRAMMING DONE BIT
            JSP_ShiftIR({&H0, &H6}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_ToggleClock(50)
            ctr_reg(4) = (ctr_reg(4) And CByte(&HBF)) 'Set BIT 7 to LOW
            JSP_ShiftDR(ctr_reg, Nothing, 64, True) : JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            JSP_ShiftIR({&H1, &HA}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_ToggleClock(50)
            JSP_ShiftIR({&H1, &H96}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_ToggleClock(9000)
            was_sucessful = EPC2_GetStatus()
            JSP_ShiftIR({&H0, &H4A}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_ToggleClock(10000)
            JSP_ShiftIR({&H3, &HFF}, Nothing, -1, True) : JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            JSP_ToggleClock(10000)
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            PrintConsole("EPC2 device programming successfully completed")
            Return True
        End Function

#End Region

#Region "ARM DAP"
        'IR=0xA
        Private Sub ARM_DP_Decode(data_in As UInt64, ByRef data_out As UInt32, ByRef dp_reg As ARM_DP_REG, ByRef r_w As ARM_RnW)
            r_w = CType((CInt(data_in) And 1), ARM_RnW)
            Dim b As Byte = CByte(CInt((data_in >> 1)) And 3)
            dp_reg = CType((b << CByte(2)), ARM_DP_REG)
            data_out = CUInt((data_in >> 3))
        End Sub
        'IR=0xB
        Private Sub ARM_AP_Decode(data_in As UInt64, ByRef data_out As UInt32, ByRef ap_reg As ARM_AP_REG, ByRef r_w As ARM_RnW)
            r_w = CType((CInt(data_in) And 1), ARM_RnW)
            Dim b As Byte = CByte(CInt((data_in >> 1)) And 3)
            ap_reg = CType((b << CByte(2)), ARM_AP_REG)
            data_out = CUInt((data_in >> 3))
        End Sub
        'Accesses CTRL/STAT, SELECT and RDBUFF
        Private Function ARM_DPACC(reg32 As UInt32, dp_reg As ARM_DP_REG, read_write As ARM_RnW, Optional goto_state As Boolean = True) As UInt32
            Dim tdo(3) As Byte
            If goto_state Then
                TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR)
                ShiftTDI(Devices(0).BSDL.IR_LEN, {Devices(0).BSDL.ARM_DPACC}, Nothing, True)
            End If
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            Dim b5 As Byte = (CByte(reg32 And &H1F) << 3) Or (CByte(dp_reg And &HF) >> 1) Or read_write : reg32 >>= 5
            Dim b4 As Byte = CByte(reg32 And &HFF) : reg32 >>= 8
            Dim b3 As Byte = CByte(reg32 And &HFF) : reg32 >>= 8
            Dim b2 As Byte = CByte(reg32 And &HFF) : reg32 >>= 8
            Dim b1 As Byte = CByte(reg32 And &H7)
            ShiftTDI(35, {b5, b4, b3, b2, b1}, tdo, True)
            TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            Dim status As Byte = CByte(tdo(0) And 3)
            Dim reg32_out As UInt32 = CByte((tdo(1) And 7) << 5) Or CByte(tdo(0) >> 3)
            reg32_out = reg32_out Or (CUInt(((tdo(2) And 7) << 5) Or (tdo(1) >> 3)) << 8)
            reg32_out = reg32_out Or (CUInt(((tdo(3) And 7) << 5) Or (tdo(2) >> 3)) << 16)
            reg32_out = reg32_out Or (CUInt(((tdo(4) And 7) << 5) Or (tdo(3) >> 3)) << 24)
            Return reg32_out
        End Function
        'Accesses port registers (AHB-AP)
        Private Function ARM_APACC(reg32 As UInt32, ap_reg As ARM_AP_REG, read_write As ARM_RnW, Optional goto_state As Boolean = True) As UInt32
            If Not ((ap_reg >> 4) = (ARM_REG_ADDR >> 4)) Then
                ARM_DPACC((ap_reg And CByte(&HF0)), ARM_DP_REG.ADDR, ARM_RnW.WR)
            End If
            ARM_REG_ADDR = ap_reg
            Dim tdo(3) As Byte
            If goto_state Then
                TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR)
                ShiftTDI(Devices(0).BSDL.IR_LEN, {Devices(0).BSDL.ARM_APACC}, Nothing, True)
            End If
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            Dim b5 As Byte = CByte((reg32 And CByte(&H1F)) << CByte(3)) Or ((ap_reg And CByte(&HF)) >> CByte(1)) Or read_write : reg32 >>= CByte(5)
            Dim b4 As Byte = CByte(reg32 And CByte(&HFF)) : reg32 >>= 8
            Dim b3 As Byte = CByte(reg32 And CByte(&HFF)) : reg32 >>= 8
            Dim b2 As Byte = CByte(reg32 And CByte(&HFF)) : reg32 >>= 8
            Dim b1 As Byte = CByte(reg32 And CByte(&H7))
            ShiftTDI(35, {b5, b4, b3, b2, b1}, tdo, True)
            TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            Dim status As Byte = (tdo(0) And CByte(3))
            Dim reg32_out As UInt32 = ((tdo(1) And CByte(7)) << 5) Or (tdo(0) >> CByte(3))
            reg32_out = reg32_out Or (CUInt(((tdo(2) And CByte(7)) << 5) Or (tdo(1) >> CByte(3))) << CByte(8))
            reg32_out = reg32_out Or (CUInt(((tdo(3) And CByte(7)) << 5) Or (tdo(2) >> CByte(3))) << CByte(16))
            reg32_out = reg32_out Or (CUInt(((tdo(4) And CByte(7)) << 5) Or (tdo(3) >> CByte(3))) << CByte(24))
            Return reg32_out
        End Function

        Private ARM_REG_ADDR As Byte

        Private Sub ARM_MDAP_INIT()
            ARM_DPACC(&H0, ARM_DP_REG.ADDR, ARM_RnW.WR)
            ARM_REG_ADDR = 0 'A[7:2] A1 and A0 are ignored
        End Sub

        Enum ARM_RnW As Byte
            WR = 0 'Write operation
            RD = 1 'Read operation
        End Enum

        Private Enum ARM_DP_REG As Byte
            None = 0 '0b00xx
            CTRL_STAT = &H4 '0b01xx
            ADDR = &H8 '0b10xx
            RDBUFF = &HC '0b11xx
        End Enum

        Private Enum ARM_AP_REG As Byte
            CSW = &H0   '0b00xx Transfer direction
            TAR = &H4   '0b01xx Transfer address
            DRW = &HC   '0b11xx Data read/Write
            CFG = &HF4  '0b01xx
            BASE = &HF8 '0b10xx DEBUG AHB ROM
            IDR = &HFC  '0b11xx
        End Enum

        Private Enum CTRLSTAT As UInt32
            CSYSPWRUPACK = 1UI << 31
            CSYSPWRUPREQ = 1UI << 30
            CDBGPWRUPACK = 1UI << 29
            CDBGPWRUPREQ = 1UI << 28
            CDBGRSTACK = 1UI << 27
            CDBRSTREQ = 1UI << 26
            WDATAERR = 1UI << 7
            READOK = 1UI << 6
            STICKYERR = 1UI << 5
            STICKYCMP = 1UI << 4
            STICKYORUN = 1UI << 1
            ORUNDETECT = 1UI << 0
        End Enum

#End Region

        Private Sub JTAG_PrintClockSpeed()
            Select Case Me.TCK_SPEED
                Case JTAG_SPEED._40MHZ
                    PrintConsole("JTAG TCK speed: 40 MHz")
                Case JTAG_SPEED._20MHZ
                    PrintConsole("JTAG TCK speed: 20 MHz")
                Case JTAG_SPEED._10MHZ
                    PrintConsole("JTAG TCK speed: 10 MHz")
                Case JTAG_SPEED._1MHZ
                    PrintConsole("JTAG TCK speed: 1 MHz")
            End Select
        End Sub

        Private Function JTAG_GetTckInHerz() As UInt32
            Select Case Me.TCK_SPEED
                Case JTAG_SPEED._1MHZ
                    Return 1000000
                Case JTAG_SPEED._10MHZ
                    Return 10000000
                Case JTAG_SPEED._20MHZ
                    Return 20000000
                Case JTAG_SPEED._40MHZ
                    Return 40000000
                Case Else
                    Return 0
            End Select
        End Function
        'Attempts to auto-detect a JTAG device on the TAP, returns the IR Length of the device
        Private Function TAP_Detect() As Boolean
            Me.Devices.Clear()
            Dim r_data(63) As Byte
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_DETECT, r_data)
            If Not result Then Return False
            Dim dev_count As Integer = r_data(0)
            Me.Chain_BitLength = r_data(1)
            If dev_count = 0 Then Return False
            Dim ptr As Integer = 2
            Dim ID32 As UInt32 = 0
            For i = 0 To dev_count - 1
                ID32 = (CUInt(r_data(ptr)) << 24)
                ID32 = ID32 Or (CUInt(r_data(ptr + 1)) << 16)
                ID32 = ID32 Or (CUInt(r_data(ptr + 2)) << 8)
                ID32 = ID32 Or (CUInt(r_data(ptr + 3)) << 0)
                ptr += 4
                If Not (ID32 = 0 Or ID32 = &HFFFFFFFFUI) Then
                    Dim n As New JTAG_DEVICE
                    n.IDCODE = ID32
                    n.VERSION = CShort((ID32 And &HF0000000L) >> 28)
                    n.PARTNU = CUShort((ID32 And &HFFFF000) >> 12)
                    n.MANUID = CUShort((ID32 And &HFFE) >> 1)
                    n.BSDL = BSDL_GetDefinition(ID32)
                    If n.BSDL IsNot Nothing Then n.IR_LENGTH = n.BSDL.IR_LEN
                    Me.Devices.Add(n)
                End If
            Next
            If (Me.Devices.Count = 0) Then Return False
            If Me.Devices.Count = 1 AndAlso Me.Devices(0).BSDL Is Nothing Then
                Me.Devices(0).IR_LENGTH = r_data(1)
            End If
            Me.Devices.Reverse() 'Put devices in order closest to HOST
            Return True
        End Function

        Public Sub Configure(proc_type As PROCESSOR)
            Me.Devices(Me.Chain_SelectedIndex).ACCESS = JTAG_MEM_ACCESS.NONE
            If proc_type = PROCESSOR.MIPS Then
                PrintConsole("Configure JTAG engine for MIPS processor")
                Dim IMPCODE As UInt32 = AccessDataRegister32(Devices(Chain_SelectedIndex).BSDL.MIPS_IMPCODE)
                EJTAG_LoadCapabilities(IMPCODE) 'Only supported by MIPS/EJTAG devices
                If Me.DMA_SUPPORTED Then
                    Dim r As UInteger = ReadMemory(&HFF300000UI, DATA_WIDTH.Word) 'Returns 2000001E 
                    r = r And &HFFFFFFFBUI '2000001A
                    WriteMemory(&HFF300000UI, r, DATA_WIDTH.Word)
                    Me.Devices(Me.Chain_SelectedIndex).ACCESS = JTAG_MEM_ACCESS.EJTAG_DMA
                    PrintConsole(RM.GetString("jtag_dma"))
                Else
                    Me.Devices(Me.Chain_SelectedIndex).ACCESS = JTAG_MEM_ACCESS.EJTAG_PRACC
                    PrintConsole(RM.GetString("jtag_no_dma"))
                End If
            ElseIf proc_type = PROCESSOR.ARM Then
                PrintConsole("Configure JTAG engine for ARM processor")
                Me.Devices(Me.Chain_SelectedIndex).ACCESS = JTAG_MEM_ACCESS.ARM
                'This does processor disable_cache, disable_mmu, and halt
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_ARM_INIT, Nothing)
            End If
        End Sub

#Region "Boundary Scan Programmer"
        Private Const IO_INPUT As Boolean = False '0
        Private Const IO_OUTPUT As Boolean = True '1
        Private Const LOGIC_LOW As Boolean = False '0
        Private Const LOGIC_HIGH As Boolean = True '1

        Private BoundaryMap As New List(Of BoundaryCell)
        'Private BSDL_IF As MemoryInterface.MemoryDeviceInstance = Nothing
        Private SetupReady As Boolean = False

        Private Property BDR_DQ_SIZE As Integer = 0
        Private Property BDR_ADDR_SIZE As Integer = 0

        Private BSR_IF As BSR_Programmer

        Public Sub BoundaryScan_Setup()
            SetupReady = False
            If Not LicenseStatus = LicenseStatusEnum.LicensedValid Then
                PrintConsole("Boundary Scan Library only available with commercial license")
                SetStatus("Error: commercial license required")
                Exit Sub
            End If
            If Devices(Me.Chain_SelectedIndex).BSDL.BS_LEN = 0 Then
                PrintConsole("Boundary Scan error: BSDL scan length not not specified")
                Exit Sub
            End If
            BoundaryMap.Clear()
            Me.BDR_DQ_SIZE = 0
            Me.BDR_ADDR_SIZE = 0
            BSR_IF = Nothing
            Dim setup_data(10) As Byte
            setup_data(0) = CByte((Devices(Me.Chain_SelectedIndex).BSDL.EXTEST) And 255)
            setup_data(1) = CByte((Devices(Me.Chain_SelectedIndex).BSDL.EXTEST >> 8) And 255)
            setup_data(2) = CByte((Devices(Me.Chain_SelectedIndex).BSDL.EXTEST >> 16) And 255)
            setup_data(3) = CByte((Devices(Me.Chain_SelectedIndex).BSDL.EXTEST >> 24) And 255)
            setup_data(4) = CByte((Devices(Me.Chain_SelectedIndex).BSDL.SAMPLE) And 255)
            setup_data(5) = CByte((Devices(Me.Chain_SelectedIndex).BSDL.SAMPLE >> 8) And 255)
            setup_data(6) = CByte((Devices(Me.Chain_SelectedIndex).BSDL.SAMPLE >> 16) And 255)
            setup_data(7) = CByte((Devices(Me.Chain_SelectedIndex).BSDL.SAMPLE >> 24) And 255)
            setup_data(8) = CByte((Devices(Me.Chain_SelectedIndex).BSDL.BS_LEN) And 255)
            setup_data(9) = CByte((Devices(Me.Chain_SelectedIndex).BSDL.BS_LEN >> 8) And 255)
            setup_data(10) = CByte(Devices(Me.Chain_SelectedIndex).BSDL.DISVAL)
            SetupReady = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_BDR_SETUP, setup_data)
        End Sub

        Public Function BoundaryScan_Init() As Boolean
            If Not SetupReady Then Return False
            PrintConsole("JTAG Boundary Scan Programmer")
            If (Not FCUSB.HasLogic()) Then
                PrintConsole("This feature is only available using FlashcatUSB Professional") : Return False
            End If
            If Not BSDL_Is_Configured() Then Return False
            BSR_IF = New BSR_Programmer(Me.FCUSB)
            AddHandler BSR_IF.PrintConsole, AddressOf MainApp.PrintConsole
            AddHandler BSR_IF.SetProgress, AddressOf MainApp.SetProgress
            For Each item In BoundaryMap
                Dim pin_data(7) As Byte
                pin_data(0) = item.TYPE 'AD,DQ,WE,OE,CE,WP,RST,BYTE
                pin_data(1) = item.pin_index 'ADx, DQx
                pin_data(2) = CByte(item.OUTPUT And 255)
                pin_data(3) = CByte(item.OUTPUT >> 8)
                pin_data(4) = CByte(item.CONTROL And 255)
                pin_data(5) = CByte(item.CONTROL >> 8)
                pin_data(6) = CByte(item.INPUT And 255)
                pin_data(7) = CByte(item.INPUT >> 8)
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_BDR_ADDPIN, pin_data)
            Next
            If Me.BDR_DQ_SIZE = 8 Then
                BSR_IF.CURRENT_BUS_WIDTH = BSR_Programmer.E_BSR_BUS_WIDTH.X8
            ElseIf Me.BDR_DQ_SIZE = 16 Then
                BSR_IF.CURRENT_BUS_WIDTH = BSR_Programmer.E_BSR_BUS_WIDTH.X16
            End If
            Return True
        End Function

        Public Function BoundaryScan_Detect() As Boolean
            If BSR_IF.DeviceInit() Then
                MainApp.Connected_Event(BSR_IF, 4096)
                Return True
            End If
            Return False
        End Function

        Private Function BSDL_Is_Configured() As Boolean
            Me.BDR_DQ_SIZE = 0
            Me.BDR_ADDR_SIZE = 0
            For Each item In BoundaryMap
                If item.pin_name.ToUpper.StartsWith("AD") Then Me.BDR_ADDR_SIZE += 1
            Next
            For Each item In BoundaryMap
                If item.pin_name.ToUpper.StartsWith("DQ") Then Me.BDR_DQ_SIZE += 1
            Next
            If (Me.BDR_ADDR_SIZE < 16) Then
                PrintConsole("Error, AQ pins need to be have at least 16 bits") : Return False
            End If
            If Not (Me.BDR_DQ_SIZE = 8 Or Me.BDR_DQ_SIZE = 16) Then
                PrintConsole("Error, DQ pins need to be assigned either 8 or 16 bits") : Return False
            End If
            For i = 0 To Me.BDR_ADDR_SIZE - 1
                If BoundaryScan_GetPinIndex("AD" & i.ToString) = -1 Then
                    PrintConsole("Error, missing address pin: AD" & i.ToString) : Return False
                End If
            Next
            For i = 0 To Me.BDR_DQ_SIZE - 1
                If BoundaryScan_GetPinIndex("DQ" & i.ToString) = -1 Then
                    PrintConsole("Error, missing address pin: DQ" & i.ToString) : Return False
                End If
            Next
            If BoundaryScan_GetPinIndex("WE#") = -1 Then
                PrintConsole("Error, missing address pin: WE#") : Return False
            End If
            If BoundaryScan_GetPinIndex("OE#") = -1 Then
                PrintConsole("Error, missing address pin: OE#") : Return False
            End If
            PrintConsole("Interface configured: X" & Me.BDR_DQ_SIZE & " (" & Me.BDR_ADDR_SIZE & "-bit address)")
            Return True
        End Function
        'Defines a pin. Output cell can be output/bidir, control_cell can be -1 if it is output_cell+1, and input_cell is used when not bidir
        Public Sub BoundaryScan_AddPin(signal_name As String, output_cell As Int16, control_cell As Int16, input_cell As Int16)
            Dim pin_desc As New BoundaryCell
            pin_desc.pin_name = signal_name.ToUpper
            pin_desc.pin_index = 0
            If pin_desc.pin_name.StartsWith("AD") Then
                pin_desc.TYPE = BoundaryScan_PinType.AD
                pin_desc.pin_index = CByte(pin_desc.pin_name.Substring(2))
                BDR_ADDR_SIZE += 1
            ElseIf pin_desc.pin_name.StartsWith("DQ") Then
                pin_desc.TYPE = BoundaryScan_PinType.DQ
                pin_desc.pin_index = CByte(pin_desc.pin_name.Substring(2))
                BDR_DQ_SIZE += 1
            ElseIf pin_desc.pin_name.Equals("WE#") Then
                pin_desc.TYPE = BoundaryScan_PinType.WE
            ElseIf pin_desc.pin_name.Equals("OE#") Then
                pin_desc.TYPE = BoundaryScan_PinType.OE
            ElseIf pin_desc.pin_name.Equals("CE#") Then
                pin_desc.TYPE = BoundaryScan_PinType.CE
            ElseIf pin_desc.pin_name.Equals("WP#") Then '(optional)
                pin_desc.TYPE = BoundaryScan_PinType.WP
            ElseIf pin_desc.pin_name.Equals("RESET#") Then '(optional)
                pin_desc.TYPE = BoundaryScan_PinType.RESET
            ElseIf pin_desc.pin_name.Equals("BYTE#") Then '(optional)
                pin_desc.TYPE = BoundaryScan_PinType.BYTE_MODE
            Else
                PrintConsole("Boundary Scan Programmer: Pin name not reconized: " & pin_desc.pin_name)
                Exit Sub 'ERROR
            End If
            pin_desc.OUTPUT = output_cell
            pin_desc.CONTROL = control_cell
            If (input_cell = -1) Then
                pin_desc.INPUT = output_cell
            Else
                pin_desc.INPUT = input_cell
            End If
            BoundaryMap.Add(pin_desc)
        End Sub

        Public Sub BoundaryScan_SetBSR(output_cell As Int16, control_cell As Int16, level As Boolean)
            Dim dw As UInt32 = CUInt(control_cell) << 16 Or CUInt(output_cell)
            If level Then dw = (dw Or &H80000000UI)
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_BDR_SETBSR, Nothing, dw)
        End Sub

        Public Sub BoundaryScan_WriteBSR()
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_BDR_WRITEBSR)
        End Sub

        Public Function BoundaryScan_GetPinIndex(signal_name As String) As Integer
            Dim index As Integer = 0
            For Each item In BoundaryMap
                If item.pin_name.ToUpper.Equals(signal_name.ToUpper) Then Return index
                index += 1
            Next
            Return -1
        End Function

        Private Function GetBoundaryCell(signal_name As String) As BoundaryCell
            For Each item In BoundaryMap
                If item.pin_name.ToUpper.Equals(signal_name.ToUpper) Then Return item
            Next
            Return Nothing
        End Function

        Private Structure BoundaryCell
            Public pin_name As String
            Public pin_index As Byte 'This is the DQx or ADx value
            Public OUTPUT As Int16
            Public INPUT As Int16 'Some devices use bidir cells (this is for seperate o/i cells)
            Public CONTROL As Int16
            Public TYPE As BoundaryScan_PinType
        End Structure

        Private Enum BoundaryScan_PinType As Byte
            AD = 1
            DQ = 2
            WE = 3
            OE = 4
            CE = 5
            WP = 6
            RESET = 7
            BYTE_MODE = 8
            CNT_LOW = 9 'Contant HIGH output
            CNT_HIGH = 10 'Constant LOW output
        End Enum

#End Region

#Region "ARM Support"
        Private Const NO_SYSSPEED As Boolean = False
        Private Const YES_SYSSPEED As Boolean = True
        Private Const ARM_NOOP As UInt32 = &HE1A00000UI

        Public Function ARM_ReadMemory32(addr As UInt32) As UInt32
            ARM_WriteRegister_r0(addr)
            ARM_PushInstruction(NO_SYSSPEED, &HE5901000UI, 0)   '"LDR r1, [r0]"
            ARM_PushInstruction(YES_SYSSPEED, &HE1A00000UI, 0)  '"NOP"
            ARM_IR(Devices(0).BSDL.RESTART)
            TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            Return ARM_ReadRegister_r1()
        End Function

        Public Sub ARM_WriteMemory32(addr As UInt32, data As UInt32)
            ARM_WriteRegister_r0r1(addr, data)
            ARM_PushInstruction(NO_SYSSPEED, &HE5801000UI, 0)   '"STR r1, [r0]"
            ARM_PushInstruction(YES_SYSSPEED, &HE1A00000UI, 0)  '"NOP"
            ARM_IR(Devices(0).BSDL.RESTART)
            TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
        End Sub

        Public Sub ARM_WriteRegister_r0(r0 As UInt32)
            Dim r0_rev = REVERSE32(r0)
            ARM_SelectChain1()
            ARM_PushInstruction(NO_SYSSPEED, &HE59F0000UI, 0)   '"LDR r0, [pc]"
            ARM_PushInstruction(NO_SYSSPEED, ARM_NOOP, 1)       '"NOP"
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            Dim tdi_data() As Byte = Utilities.Bytes.FromUInt32(r0_rev)
            ReDim Preserve tdi_data(4) 'Add on one extra byte
            ShiftTDI(34, tdi_data, Nothing, True)                 'shits an extra 0b00 at the End
            ARM_PushInstruction(NO_SYSSPEED, &HE1A00000UI, 1)
        End Sub

        Public Sub ARM_WriteRegister_r0r1(r0 As UInt32, r1 As UInt32)
            Dim r0_rev As UInt32 = REVERSE32(r0)
            Dim r1_rev As UInt32 = REVERSE32(r1)
            ARM_SelectChain1()
            ARM_PushInstruction(NO_SYSSPEED, &HE89F0003UI, 0)   '"LDMIA pc, {r0-r1}"
            ARM_PushInstruction(NO_SYSSPEED, ARM_NOOP, 1)       '"NOP"
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            ShiftTDI(32, Utilities.Bytes.FromUInt32(r0_rev), Nothing, True)
            ARM_PushInstruction(NO_SYSSPEED, &HE1A00000UI, 0)
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            ShiftTDI(32, Utilities.Bytes.FromUInt32(r1_rev), Nothing, True)
            ARM_PushInstruction(NO_SYSSPEED, ARM_NOOP, 1)
        End Sub

        Public Function ARM_ReadRegister_r0(r0 As UInt32, r1 As UInt32) As UInt32
            ARM_SelectChain1()
            ARM_PushInstruction(NO_SYSSPEED, &HE58F0000UI, 0)  '"STR r0, [pc]"
            ARM_PushInstruction(NO_SYSSPEED, ARM_NOOP, 1)      '"NOP"
            Dim tdo(31) As Byte
            ShiftTDI(32, Nothing, tdo, True)
            TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            Dim reg_out As UInt32 = REVERSE32(Utilities.Bytes.ToUInt32(tdo))
            Return reg_out
        End Function

        Public Function ARM_ReadRegister_r1() As UInt32
            ARM_SelectChain1()
            ARM_PushInstruction(NO_SYSSPEED, &HE58F1000UI, 0) '"STR r1, [pc]"
            ARM_PushInstruction(NO_SYSSPEED, ARM_NOOP, 1) '"NOP"
            Dim tdo(31) As Byte
            ShiftTDI(32, Nothing, tdo, True)
            TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            Dim reg_out As UInt32 = REVERSE32(Utilities.Bytes.ToUInt32(tdo))
            Return reg_out
        End Function

        Public Sub ARM_SelectChain1()
            ARM_IR(Devices(0).BSDL.SCAN_N)
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            ShiftTDI(5, {1}, Nothing, True) 'shift: 0x10 (correct to 0b00001)
            ARM_IR(CByte(Devices(0).BSDL.INTEST))
        End Sub

        Public Sub ARM_SelectChain2()
            ARM_IR(Devices(0).BSDL.SCAN_N)
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            ShiftTDI(5, {2}, Nothing, True) 'shift: 0x08 (reversed to 0b00010)
            ARM_IR(CByte(Devices(0).BSDL.INTEST))
        End Sub

        Public Sub ARM_PushInstruction(SYSSPEED As Boolean, op_cmd As UInt32, rti_cycles As Byte)
            Dim op_rev As UInt32 = REVERSE32(op_cmd) 'So we shift MSB first
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            Dim tdi64 As UInt64
            If (SYSSPEED) Then
                tdi64 = (CULng(op_rev) << 1) Or 1UI
            Else
                tdi64 = (CULng(op_rev) << 1)
            End If
            Dim tdi() As Byte = Utilities.Bytes.FromUInt64(tdi64)
            ReDim tdi(4) '5 bytes only
            ShiftTDI(33, tdi, Nothing, False)
            TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            If (rti_cycles > 0) Then Tap_Toggle(rti_cycles, False)
        End Sub

        Public Sub ARM_IR(ir_data As Byte)
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR)
            ShiftTDI(4, {ir_data}, Nothing, True)
        End Sub

        Public Function REVERSE32(u32 As UInt32) As UInt32
            Dim out32 As UInt32 = 0
            For i = 0 To 31
                If ((u32 And 1) = 1) Then out32 = out32 Or 1UI
                out32 = out32 << 1
                u32 = u32 >> 1
            Next
            Return out32
        End Function

#End Region

#Region "SVF Player"
        Public WithEvents JSP As New SVF_Player() 'SVF / XSVF Parser and player
        Private CurrentHz As UInt32 = 1000000

        Private Sub JSP_SetFrequency(Hz As UInt32) Handles JSP.SetFrequency
            CurrentHz = Hz
        End Sub

        Private Sub JSP_ResetTap() Handles JSP.ResetTap
            Reset_StateMachine()
        End Sub

        Private Sub JSP_GotoState(dst_state As JTAG_MACHINE_STATE) Handles JSP.GotoState
            TAP_GotoState(dst_state)
        End Sub

        Private Function JSP_ToggleClock(ticks As UInt32, Optional exit_tms As Boolean = False) As Boolean Handles JSP.ToggleClock
            Return Tap_Toggle(ticks, exit_tms)
        End Function

        Public Sub JSP_ShiftIR(tdi_bits() As Byte, ByRef tdo_bits() As Byte, bit_count As Int16, exit_tms As Boolean) Handles JSP.ShiftIR
            ShiftIR(tdi_bits, tdo_bits, bit_count, exit_tms)
        End Sub

        Public Sub JSP_ShiftDR(tdi_bits() As Byte, ByRef tdo_bits() As Byte, bit_count As UInt16, exit_tms As Boolean) Handles JSP.ShiftDR
            ShiftDR(tdi_bits, tdo_bits, bit_count, exit_tms)
        End Sub

        Private Sub JSP_Writeconsole(msg As String) Handles JSP.Writeconsole
            PrintConsole("SVF Player: " & msg)
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
        Private Sub EJTAG_LoadCapabilities(features As UInt32)
            Dim e_ver As UInt32 = ((features >> 29) And 7UI)
            Dim e_nodma As UInt32 = (features And (1UI << 14))
            Dim e_priv As UInt32 = (features And (1UI << 28))
            Dim e_dint As UInt32 = (features And (1UI << 24))
            Dim e_mips As UInt32 = (features And 1UI)
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
            AccessDataRegister32(Devices(Chain_SelectedIndex).BSDL.MIPS_CONTROL, (EJTAG_CTRL.PrRst Or EJTAG_CTRL.PerRst))
        End Sub

        Public Function EJTAG_Debug_Enable() As Boolean
            Try
                Dim debug_flag As UInt32 = EJTAG_CTRL.PrAcc Or EJTAG_CTRL.ProbEn Or EJTAG_CTRL.SetDev Or EJTAG_CTRL.JtagBrk
                Dim ctrl_reg As UInt32 = AccessDataRegister32(Devices(Chain_SelectedIndex).BSDL.MIPS_CONTROL, debug_flag)
                If CBool((AccessDataRegister32(Devices(Chain_SelectedIndex).BSDL.MIPS_CONTROL, EJTAG_CTRL.PrAcc Or EJTAG_CTRL.ProbEn Or EJTAG_CTRL.SetDev) And EJTAG_CTRL.BrkSt)) Then
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
                AccessDataRegister32(Devices(Chain_SelectedIndex).BSDL.MIPS_CONTROL, flag)
            Catch ex As Exception
            End Try
        End Sub

        Private Const MAX_USB_BUFFER_SIZE As UShort = 2048 'Max number of bytes we should send via USB bulk endpoints
        'Target device needs to support DMA and FCUSB needs to include flashmode
        Public Sub DMA_WriteFlash(dma_addr As UInt32, data_to_write() As Byte, prog_mode As CFI.CFI_FLASH_MODE)
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
                Dim BytesLeft As Integer = data_to_write.Length
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
                Loop
            Catch
            End Try
        End Sub

        Private Function DMA_WriteFlash_Block(dma_addr As UInt32, data() As Byte, sub_cmd As CFI.CFI_FLASH_MODE) As Boolean
            Dim setup_data(7) As Byte
            Dim data_count As Integer = data.Length
            setup_data(0) = CByte(dma_addr And 255)
            setup_data(1) = CByte((dma_addr >> 8) And 255)
            setup_data(2) = CByte((dma_addr >> 16) And 255)
            setup_data(3) = CByte((dma_addr >> 24) And 255)
            setup_data(4) = CByte(data_count And 255)
            setup_data(5) = CByte((data_count >> 8) And 255)
            setup_data(6) = CByte((data_count >> 16) And 255)
            setup_data(7) = CByte((data_count >> 24) And 255)
            If Not FCUSB.USB_CONTROL_MSG_OUT(CType(sub_cmd, USB.USBREQ), setup_data) Then Return Nothing
            Dim write_result As Boolean = FCUSB.USB_BULK_OUT(data)
            If write_result Then FCUSB.USB_WaitForComplete()
            Return write_result
        End Function

#End Region

#Region "CFI Plugin"

        Public Function ReadMemory(addr As UInt32, width As DATA_WIDTH) As UInt32
            Dim ReadBack As Byte() = New Byte(3) {}
            Select Case Me.Devices(Me.Chain_SelectedIndex).ACCESS
                Case JTAG_MEM_ACCESS.EJTAG_DMA
                    Select Case width
                        Case DATA_WIDTH.Word
                            FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_READ_W, ReadBack, addr)
                            Array.Reverse(ReadBack)
                            Return Utilities.Bytes.ToUInt32(ReadBack)
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
                    Return Utilities.Bytes.ToUInt32(ReadBack)
            End Select
            Return 0
        End Function

        Public Sub WriteMemory(addr As UInt32, data As UInt32, width As DATA_WIDTH)
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
            setup_data(8) = Me.Devices(Me.Chain_SelectedIndex).ACCESS
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_WRITE, setup_data, bits)
        End Sub

        'Reads data from DRAM (optomized for speed)
        Public Function ReadMemory(Address As UInt32, count As Integer) As Byte()
            Dim DramStart As UInt32 = Address
            Dim LargeCount As Integer = count 'The total amount of data we need to read in
            Do Until Address Mod 4 = 0 Or Address = 0
                Address = CUInt(Address - 1)
                LargeCount = LargeCount + 1
            Loop
            Do Until LargeCount Mod 4 = 0
                LargeCount = LargeCount + 1
            Loop 'Now StartAdd2 and ByteLen2 are on bounds of 4
            Dim TotalBuffer(CInt(LargeCount - 1)) As Byte
            Dim BytesLeft As Integer = LargeCount
            While BytesLeft > 0
                Dim BytesToRead As Integer = CInt(BytesLeft)
                If BytesToRead > MAX_USB_BUFFER_SIZE Then BytesToRead = MAX_USB_BUFFER_SIZE
                Dim Offset As Integer = (LargeCount - BytesLeft)
                Dim TempBuffer() As Byte = Nothing
                Select Case Me.Devices(Me.Chain_SelectedIndex).ACCESS
                    Case JTAG_MEM_ACCESS.EJTAG_DMA
                        TempBuffer = JTAG_ReadMemory(Address + CUInt(Offset), BytesToRead)
                    Case JTAG_MEM_ACCESS.EJTAG_PRACC
                    Case JTAG_MEM_ACCESS.ARM
                End Select
                If TempBuffer Is Nothing OrElse Not TempBuffer.Length = BytesToRead Then
                    ReDim Preserve TempBuffer(CInt(BytesToRead - 1)) 'Fill buffer with blank data
                End If
                Array.Copy(TempBuffer, 0, TotalBuffer, Offset, TempBuffer.Length)
                BytesLeft -= BytesToRead
            End While
            Dim OutByte(CInt(count - 1)) As Byte
            Array.Copy(TotalBuffer, LargeCount - count, OutByte, 0, count)
            Return OutByte
        End Function
        'Writes an unspecified amount of b() into memory (usually DRAM)
        Public Function WriteMemory(Address As UInt32, data() As Byte) As Boolean
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
                    Select Case Me.Devices(Me.Chain_SelectedIndex).ACCESS
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

        Private Function JTAG_WriteMemory(dma_addr As UInteger, data_out() As Byte) As Boolean
            Dim setup_data(8) As Byte
            Dim data_len As Integer = data_out.Length
            setup_data(0) = CByte(dma_addr And 255)
            setup_data(1) = CByte((dma_addr >> 8) And 255)
            setup_data(2) = CByte((dma_addr >> 16) And 255)
            setup_data(3) = CByte((dma_addr >> 24) And 255)
            setup_data(4) = CByte(data_len And 255)
            setup_data(5) = CByte((data_len >> 8) And 255)
            setup_data(6) = CByte((data_len >> 16) And 255)
            setup_data(7) = CByte((data_len >> 24) And 255)
            setup_data(8) = Me.Devices(Me.Chain_SelectedIndex).ACCESS
            If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_WRITEMEM, setup_data) Then Return Nothing 'Sends setup command and data
            Return FCUSB.USB_BULK_OUT(data_out)
        End Function

        Private Function JTAG_ReadMemory(dma_addr As UInt32, count As Integer) As Byte()
            Dim setup_data(8) As Byte
            setup_data(0) = CByte(dma_addr And 255)
            setup_data(1) = CByte((dma_addr >> 8) And 255)
            setup_data(2) = CByte((dma_addr >> 16) And 255)
            setup_data(3) = CByte((dma_addr >> 24) And 255)
            setup_data(4) = CByte(count And 255)
            setup_data(5) = CByte((count >> 8) And 255)
            setup_data(6) = CByte((count >> 16) And 255)
            setup_data(7) = CByte((count >> 24) And 255)
            setup_data(8) = Me.Devices(Me.Chain_SelectedIndex).ACCESS
            Dim data_back(count - 1) As Byte
            If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_READMEM, setup_data) Then Return Nothing 'Sends setup command and data
            If Not FCUSB.USB_BULK_IN(data_back) Then Return Nothing
            Return data_back
        End Function



#End Region

#Region "CFI over JTAG"
        Private WithEvents CFI_IF As New CFI.FLASH_INTERFACE() 'Handles all of the CFI flash protocol

        Public Function CFI_Detect(DMA_ADDR As UInt32) As Boolean
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

        Public Function CFI_SectorCount() As Integer
            Return CFI_IF.GetFlashSectors()
        End Function

        Public Function CFI_GetSectorSize(sector_ind As Integer) As Integer
            Return CFI_IF.GetSectorSize(sector_ind)
        End Function

        Public Function CFI_FindSectorBase(sector_ind As Integer) As UInt32
            Return CFI_IF.FindSectorBase(sector_ind)
        End Function

        Public Function CFI_Sector_Erase(sector_ind As Integer) As Boolean
            CFI_IF.Sector_Erase(sector_ind)
            Return True
        End Function

        Public Function CFI_ReadFlash(Address As UInt32, count As Integer) As Byte()
            Return CFI_IF.ReadData(Address, count)
        End Function

        Public Function CFI_SectorWrite(sector_index As Integer, data_out() As Byte) As Boolean
            CFI_IF.WriteSector(sector_index, data_out)
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

        Private Sub CFIEVENT_WriteB(addr As UInt32, data As Byte) Handles CFI_IF.Memory_Write_B
            WriteMemory(addr, data, DATA_WIDTH.Byte)
        End Sub

        Private Sub CFIEVENT_WriteH(addr As UInt32, data As UInt16) Handles CFI_IF.Memory_Write_H
            WriteMemory(addr, data, DATA_WIDTH.HalfWord)
        End Sub

        Private Sub CFIEVENT_WriteW(addr As UInt32, data As UInt32) Handles CFI_IF.Memory_Write_W
            WriteMemory(addr, data, DATA_WIDTH.Word)
        End Sub

        Private Sub CFIEVENT_ReadB(addr As UInt32, ByRef data As Byte) Handles CFI_IF.Memory_Read_B
            data = CByte(ReadMemory(addr, DATA_WIDTH.Byte))
        End Sub

        Private Sub CFIEVENT_ReadH(addr As UInt32, ByRef data As UInt16) Handles CFI_IF.Memory_Read_H
            data = CByte(ReadMemory(addr, DATA_WIDTH.HalfWord))
        End Sub

        Private Sub CFIEVENT_ReadW(addr As UInt32, ByRef data As UInt32) Handles CFI_IF.Memory_Read_W
            data = ReadMemory(addr, DATA_WIDTH.Word)
        End Sub

        Private Sub CFIEVENT_SetBase(base As UInt32) Handles CFI_IF.SetBaseAddress
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SET_OPTION, BitConverter.GetBytes(base), 5)
        End Sub

        Private Sub CFIEVENT_ReadFlash(dma_addr As UInt32, ByRef data() As Byte) Handles CFI_IF.ReadFlash
            data = JTAG_ReadMemory(dma_addr, data.Length)
        End Sub

        Private Sub CFIEVENT_WriteFlash(dma_addr As UInt32, data_to_write() As Byte, prog_mode As CFI.CFI_FLASH_MODE) Handles CFI_IF.WriteFlash
            DMA_WriteFlash(dma_addr, data_to_write, prog_mode)
        End Sub

        Private Sub CFIEVENT_WriteConsole(message As String) Handles CFI_IF.WriteConsole
            PrintConsole(message)
        End Sub

#End Region

#End Region

#Region "SPI over JTAG"
        Public SPI_Part As SPI_NOR 'Contains the SPI Flash definition
        Private SPI_JTAG_PROTOCOL As JTAG_SPI_Type
        Private SPI_MIPS_JTAG_IF As MIPS_SPI_API
        'Returns TRUE if the JTAG can detect a connected flash to the SPI port
        Public Function SPI_Detect(spi_if As JTAG_SPI_Type) As Boolean
            SPI_JTAG_PROTOCOL = spi_if
            If spi_if = JTAG_SPI_Type.ATH_MIPS Or spi_if = JTAG_SPI_Type.BCM_MIPS Then
                SPI_MIPS_JTAG_IF = New MIPS_SPI_API(SPI_JTAG_PROTOCOL)
                Dim reg As UInt32 = SPI_SendCommand(SPI_MIPS_JTAG_IF.READ_ID, 1, 3)
                PrintConsole(String.Format("JTAG: SPI register returned {0}", "0x" & Hex(reg)))
                If reg = 0 OrElse reg = &HFFFFFFFFUI Then
                    Return False
                Else
                    Dim MFG_BYTE As Byte = CByte((reg And &HFF0000) >> 16)
                    Dim PART_ID As UInt16 = CUShort(reg And &HFFFF)
                    SPI_Part = CType(FlashDatabase.FindDevice(MFG_BYTE, PART_ID, 0, MemoryType.SERIAL_NOR), SPI_NOR)
                    If SPI_Part IsNot Nothing Then
                        PrintConsole(String.Format("JTAG: SPI flash detected ({0})", SPI_Part.NAME))
                        If SPI_JTAG_PROTOCOL = JTAG_SPI_Type.BCM_MIPS Then
                            WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, 0, DATA_WIDTH.Word)
                            Dim data() As Byte = SPI_ReadFlash(0, 4)
                        End If
                        Return True
                    Else
                        PrintConsole("JTAG: SPI flash not found in database")
                    End If
                End If
            ElseIf spi_if = JTAG_SPI_Type.BCM_ARM Then
                If Not FCUSB.HWBOARD = USB.FCUSB_BOARD.Professional_PCB5 Then
                    PrintConsole("JTAG: Error, ARM extension is only supported on FlashcatUSB Professional")
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
        Private Class MIPS_SPI_API
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

            Sub New(spi_type As JTAG_SPI_Type)
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

        End Class

        Public Function SPI_EraseBulk() As Boolean
            Try
                PrintConsole("Erasing entire SPI flash (this could take a moment)")
                WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, 0, DATA_WIDTH.Word) 'Might need to be for Ath too
                SPI_SendCommand(SPI_MIPS_JTAG_IF.WREN, 1, 0)
                WriteMemory(SPI_MIPS_JTAG_IF.REG_OPCODE, SPI_MIPS_JTAG_IF.BASE, DATA_WIDTH.Word)
                WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, &H800000C7UI, DATA_WIDTH.Word) 'Might need to be for Ath too
                SPI_WaitUntilReady()
                PrintConsole("Erase operation complete!")
                Return True
            Catch ex As Exception
            End Try
            Return False
        End Function

        Public Function SPI_SectorErase(secotr_ind As Integer) As Boolean
            Try
                Dim Addr24 As UInteger = SPI_FindSectorBase(secotr_ind)
                Dim reg As UInt32 = SPI_GetControlReg()
                If SPI_JTAG_PROTOCOL = JTAG_SPI_Type.BCM_MIPS Then
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, 0, DATA_WIDTH.Word)
                    SPI_SendCommand(SPI_MIPS_JTAG_IF.WREN, 1, 0)
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_OPCODE, Addr24 Or SPI_MIPS_JTAG_IF.SECTORERASE, DATA_WIDTH.Word)
                    reg = (reg And &HFFFFFF00UI) Or SPI_MIPS_JTAG_IF.SECTORERASE Or SPI_MIPS_JTAG_IF.CTL_Start
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, reg, DATA_WIDTH.Word)
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, 0, DATA_WIDTH.Word)
                ElseIf SPI_JTAG_PROTOCOL = JTAG_SPI_Type.ATH_MIPS Then
                    SPI_SendCommand(SPI_MIPS_JTAG_IF.WREN, 1, 0)
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_OPCODE, (Addr24 << 8) Or SPI_MIPS_JTAG_IF.SECTORERASE, DATA_WIDTH.Word)
                    reg = (reg And &HFFFFFF00UI) Or &H4UI Or SPI_MIPS_JTAG_IF.CTL_Start
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, reg, DATA_WIDTH.Word)
                End If
                SPI_WaitUntilReady()
                SPI_GetControlReg()
            Catch ex As Exception
            End Try
            Return True
        End Function

        Public Function SPI_WriteData(Address As UInt32, data() As Byte) As Boolean
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

        Public Function SPI_SendCommand(SPI_OPCODE As UInt16, BytesToWrite As UInt32, BytesToRead As UInt32) As UInt32
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
                        reg = (reg And 255UI)
                    Case 2
                        reg = ((reg And 255UI) << 8) Or ((reg And &HFF00UI) >> 8)
                    Case 3
                        reg = ((reg And 255UI) << 16) Or (reg And &HFF00UI) Or ((reg And &HFF0000UI) >> 16)
                    Case 4
                        reg = ((reg And 255UI) << 24) Or ((reg And &HFF00UI) << 8) Or ((reg And &HFF0000UI) >> 8) Or ((reg And &HFF000000UI) >> 24)
                End Select
                Return reg
            End If
            Return 0
        End Function
        'Returns the total number of sectors
        Public Function SPI_SectorCount() As Integer
            Dim secSize As Long = &H10000L '64KB
            Return CInt(SPI_Part.FLASH_SIZE \ secSize)
        End Function

        Public Function SPI_SectorWrite(sector_index As Integer, data() As Byte) As Boolean
            Dim Addr As UInteger = SPI_FindSectorBase(sector_index)
            SPI_WriteData(Addr, data)
            Return True
        End Function

        Public Function SPI_FindSectorBase(sectorInt As Integer) As UInteger
            Return CUInt(SPI_GetSectorSize(0) * sectorInt)
        End Function

        Public Function SPI_GetSectorSize(sector_ind As Integer) As Integer
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

        Public Function SPI_ReadFlash(addr32 As UInt32, count As Integer) As Byte()
            Return ReadMemory(SPI_MIPS_JTAG_IF.BASE + addr32, count)
        End Function

        Private Function SPI_WriteFlash(addr32 As UInt32, data_out() As Byte) As Boolean
            Dim OpCode As Byte
            Select Case SPI_JTAG_PROTOCOL
                Case JTAG_SPI_Type.BCM_MIPS
                    OpCode = USB.USBREQ.JTAG_FLASHSPI_BRCM
                Case JTAG_SPI_Type.ATH_MIPS
                    OpCode = USB.USBREQ.JTAG_FLASHSPI_ATH
            End Select
            Dim setup_data(7) As Byte
            Dim data_len As Integer = data_out.Length
            setup_data(0) = CByte(addr32 And 255)
            setup_data(1) = CByte((addr32 >> 8) And 255)
            setup_data(2) = CByte((addr32 >> 16) And 255)
            setup_data(3) = CByte((addr32 >> 24) And 255)
            setup_data(4) = CByte(data_len And 255)
            setup_data(5) = CByte((data_len >> 8) And 255)
            setup_data(6) = CByte((data_len >> 16) And 255)
            setup_data(7) = CByte((data_len >> 24) And 255)
            If Not FCUSB.USB_CONTROL_MSG_OUT(CType(OpCode, USB.USBREQ), setup_data) Then Return Nothing 'Sends setup command and data
            Return FCUSB.USB_BULK_OUT(data_out)
        End Function

        Public Enum JTAG_SPI_Type As Integer
            BCM_MIPS = 1
            ATH_MIPS = 2
            BCM_ARM = 3
        End Enum

#End Region

#Region "Chain Support"

        Public Sub Chain_Select(device_index As Integer)
            Dim ACCESS_TYPE As Byte = 0
            If Not Me.Chain_IsValid Then
                PrintConsole("JTAG chain is not valid, unable to select device") : Exit Sub
            End If
            If (device_index < Me.Devices.Count) Then
                Dim J As JTAG_DEVICE = Me.Devices(device_index)
                Me.IR_LEADING = 0
                Me.IR_TRAILING = 0
                Me.DR_LEADING = 0
                Me.DR_TRAILING = 0
                Me.IR_LENGTH = J.BSDL.IR_LEN
                ACCESS_TYPE = J.ACCESS
                Me.Chain_SelectedIndex = device_index
                If J.BSDL IsNot Nothing Then
                    For i = 0 To (device_index - 1)
                        Me.IR_TRAILING += Me.Devices(i).BSDL.IR_LEN
                        Me.DR_TRAILING += CByte(1) 'BYPASS REG is always one bit wide
                    Next
                    For i = (device_index + 1) To Me.Devices.Count - 1
                        Me.IR_LEADING += J.BSDL.IR_LEN
                        Me.DR_LEADING += CByte(1) 'BYPASS REG is always one bit wide
                    Next
                    PrintConsole(String.Format("JTAG index {0} selected: {1} (IR length {2})", device_index, J.ToString(), J.BSDL.IR_LEN.ToString()))
                End If
            ElseIf (device_index = Me.Devices.Count) Then 'Select all
                Me.Chain_SelectedIndex = device_index
                Me.IR_LEADING = 0
                Me.IR_TRAILING = 0
                Me.DR_LEADING = 0
                Me.DR_TRAILING = 0
                Me.IR_LENGTH = Me.Chain_BitLength
                PrintConsole("JTAG selected all devices")
            Else
                Exit Sub
            End If
            Dim select_data(7) As Byte
            select_data(0) = Me.IR_LENGTH
            select_data(1) = Me.IR_LEADING
            select_data(2) = Me.IR_TRAILING
            select_data(3) = Me.DR_LEADING
            select_data(4) = Me.DR_TRAILING
            select_data(5) = ACCESS_TYPE
            select_data(6) = 0 'RFU1
            select_data(7) = 0 'RFU2
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SELECT, select_data)
        End Sub

        Public Sub Chain_Print()
            PrintConsole("JTAG chain detected: " & Me.Devices.Count & " devices")
            PrintConsole("JTAG TDI IR chain size: " & Me.Chain_BitLength & " bits")
            For i = 0 To Me.Devices.Count - 1
                Dim ID As String = "0x" & Hex(Devices(i).IDCODE).PadLeft(8, "0"c)
                If Devices(i).BSDL IsNot Nothing Then
                    PrintConsole("Index " & i.ToString() & ": JEDEC ID " & ID & " (" & Devices(i).BSDL.ToString() & ")")
                Else
                    PrintConsole("Index " & i.ToString() & ": JEDEC ID " & ID & " - BSDL definition not found")
                End If
            Next
            If (Me.Devices.Count > 1) Then
                PrintConsole("Index " & Me.Devices.Count.ToString() & ": [select all devices]")
            End If
        End Sub

        Public Sub Chain_Clear()
            Me.Devices.Clear()
            PrintConsole("JTAG chain cleared")
        End Sub
        'Sets the BSDL definition for a given device in a JTAG chain
        Public Function Chain_Set(index As Integer, part_name As String) As Boolean
            If (index > (Me.Devices.Count - 1)) Then Return False
            For Each selected_device In Me.BSDL_DATABASE
                If selected_device.PART_NAME.ToUpper.Equals(part_name.ToUpper) Then
                    Me.Devices(index).BSDL = selected_device
                    Me.Devices(index).IR_LENGTH = selected_device.IR_LEN
                    Return True
                End If
            Next
            Return False
        End Function

        Public Function Chain_Add(part_name As String) As Boolean
            For Each selected_device In Me.BSDL_DATABASE
                If selected_device.PART_NAME.ToUpper.Equals(part_name.ToUpper) Then
                    Dim j As New JTAG_DEVICE
                    j.IDCODE = selected_device.IDCODE
                    j.BSDL = selected_device
                    j.IR_LENGTH = selected_device.IR_LEN
                    Me.Devices.Add(j)
                    Return True
                End If
            Next
            Return False
        End Function
        'Returns the BSDL definition
        Public Function Chain_Get(index As Integer) As BSDL_DEF
            Return Me.Devices(index).BSDL
        End Function

        Public Function Chain_Validate() As Boolean
            Dim ChainIsValid As Boolean = True
            Dim TotalSize As UInt32 = 0
            For i = 0 To Me.Devices.Count - 1
                If Devices(i).BSDL Is Nothing Then
                    ChainIsValid = False
                Else
                    TotalSize += Me.Devices(i).IR_LENGTH
                End If
            Next
            If ChainIsValid AndAlso TotalSize = Me.Chain_BitLength Then
                Me.Chain_IsValid = True
                Return True
            Else
                Me.Chain_IsValid = False
                Return False
            End If
        End Function

#End Region

#Region "Public function"

        Public Sub Reset_StateMachine()
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_RESET)
        End Sub

        Public Sub TAP_GotoState(J_STATE As JTAG_MACHINE_STATE)
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_GOTO_STATE, Nothing, J_STATE)
        End Sub

        Public Function Tap_Toggle(count As UInt32, exit_tms As Boolean) As Boolean
            Try
                Dim tck_hz As UInt32 = Me.JTAG_GetTckInHerz()
                Dim mult As UInt32 = (tck_hz \ CurrentHz)
                count = count * mult
                Dim ticks_left As UInt32 = count
                While (ticks_left > 0)
                    Dim toggle_count As UInt32 = CUInt(IIf(ticks_left <= 10000000UI, ticks_left, 10000000UI)) 'MAX 10 million cycles
                    Dim toggle_cmd As UInt32 = toggle_count
                    If exit_tms Then toggle_cmd = toggle_cmd Or (1UI << 31) 'The MSB indicates STAY/LEAVE for toggles
                    Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_TOGGLE, Nothing, toggle_cmd)
                    If Not result Then Return False
                    Dim delay_ms As Integer = CInt((CDbl(toggle_count) / CDbl(tck_hz)) * 1000)
                    'Utilities.Sleep(delay_ms + 5)
                    If (delay_ms > 5) Then
                        FCUSB.USB_WaitForComplete()
                    ElseIf (delay_ms > 0) Then
                        Utilities.Sleep(delay_ms)
                    Else
                        Utilities.Sleep(1)
                    End If
                    ticks_left -= toggle_count
                    If (ticks_left > 0) Then Threading.Thread.CurrentThread.Join(10) 'Pump a message
                End While
                Return True
            Catch ex As Exception
            End Try
            Return False
        End Function
        'Selects the IR and shifts data into them and exits
        Public Sub ShiftIR(tdi_bits() As Byte, ByRef tdo_bits() As Byte, bit_count As Int16, exit_tms As Boolean)
            If bit_count = -1 Then bit_count = Me.IR_LENGTH
            ReDim tdo_bits(tdi_bits.Length - 1)
            Dim cmd As UInt32 = CUInt(IIf(exit_tms, CUInt((1UI << 16) Or CUInt(bit_count)), CUInt(bit_count)))
            Array.Reverse(tdi_bits)
            FCUSB.USB_LOADPAYLOAD(tdi_bits) 'Preloads the TDI data
            FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_SHIFT_IR, tdo_bits, cmd)
            Array.Reverse(tdo_bits)
        End Sub

        Public Sub ShiftDR(tdi_bits() As Byte, ByRef tdo_bits() As Byte, bit_count As UInt16, exit_tms As Boolean)
            ReDim tdo_bits(tdi_bits.Length - 1)
            Dim cmd As UInt32 = CUInt(IIf(exit_tms, CUInt((1UI << 16) Or bit_count), CUInt(bit_count)))
            Array.Reverse(tdi_bits)
            FCUSB.USB_LOADPAYLOAD(tdi_bits) 'Preloads the TDI data
            FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_SHIFT_DR, tdo_bits, cmd)
            Array.Reverse(tdo_bits)
        End Sub

        Public Sub ShiftTDI(BitCount As UInt32, tdi_bits() As Byte, ByRef tdo_bits() As Byte, exit_tms As Boolean)
            Dim byte_count As Integer = CInt(Math.Ceiling(BitCount / 8))
            If tdo_bits Is Nothing Then ReDim tdo_bits(byte_count - 1)
            If tdi_bits Is Nothing Then ReDim tdi_bits(byte_count - 1)
            Dim BytesLeft As Integer = byte_count
            Dim BitsLeft As UInt32 = BitCount
            Dim ptr As Integer = 0
            Do Until BytesLeft = 0
                Dim packet_size As Integer = CInt(Math.Min(64, BytesLeft))
                Dim packet_data(packet_size - 1) As Byte
                Array.Copy(tdi_bits, ptr, packet_data, 0, packet_data.Length)
                BytesLeft -= packet_size
                Dim bits_size As UInt32 = Math.Min(512UI, BitsLeft)
                BitsLeft -= bits_size
                If BytesLeft = 0 AndAlso exit_tms Then bits_size = bits_size Or (1UI << 16)
                Dim tdo_data(packet_size - 1) As Byte
                If Not FCUSB.USB_LOADPAYLOAD(packet_data) Then Exit Sub 'Preloads the TDI/TMS data
                If Not FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_SHIFT_DATA, tdo_data, bits_size) Then Exit Sub  'Shifts data
                Array.Copy(tdo_data, 0, tdo_bits, ptr, tdo_data.Length)
                ptr += packet_data.Length
            Loop
        End Sub

        Public Function AccessDataRegister32(ir_value As UInt32, Optional dr_value As UInt32 = 0) As UInt32
            Dim dr_data() As Byte = Utilities.Bytes.FromUInt32(dr_value)
            Array.Reverse(dr_data)
            Dim tdo(3) As Byte
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR)
            If (Me.IR_LEADING > 0) Then Tap_Toggle(Me.IR_LEADING, False)
            If (Me.IR_TRAILING > 0) Then
                ShiftTDI(Me.Devices(Me.Chain_SelectedIndex).IR_LENGTH, {CByte(ir_value)}, Nothing, False)
                Tap_Toggle(Me.IR_TRAILING, True)
            Else
                ShiftTDI(Me.Devices(Me.Chain_SelectedIndex).IR_LENGTH, {CByte(ir_value)}, Nothing, True)
            End If
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            If (DR_LEADING > 0) Then Tap_Toggle(Me.DR_LEADING, False)
            If (DR_TRAILING > 0) Then
                ShiftTDI(32, dr_data, tdo, False)
                Tap_Toggle(Me.DR_TRAILING, True)
            Else
                ShiftTDI(32, dr_data, tdo, True)
            End If
            TAP_GotoState(JTAG_MACHINE_STATE.Update_DR)
            Array.Reverse(tdo)
            Return Utilities.Bytes.ToUInt32(tdo)
        End Function

        Public Function GetSelected_IRLength() As UInt32
            Return Me.Devices(Me.Chain_SelectedIndex).IR_LENGTH
        End Function

#End Region

#Region "BSDL"
        Private BSDL_DATABASE As New List(Of BSDL_DEF)
        Private PrintAddedToDB As Boolean = True

        Private Sub BSDL_Init()
            PrintAddedToDB = False
            BSDL_DATABASE.Clear()
            Dim ScriptPath As String = (New IO.FileInfo(MyLocation)).DirectoryName & "\Scripts\"
            Dim script_io As New IO.DirectoryInfo(ScriptPath)
            Dim fn_out As New IO.DirectoryInfo(script_io.Parent.FullName & "\JTAG_BSDL\")
            If fn_out.Exists Then
                Dim device_files() = fn_out.GetFiles("*.fcs")
                For Each bsdl In device_files
                    Try
                        Dim bsdl_text() As String = Utilities.FileIO.ReadFile(bsdl.FullName)
                        ScriptProcessor.RunScript(bsdl_text)
                    Catch ex As Exception
                    End Try
                Next
            End If
            PrintAddedToDB = True
            PrintConsole(String.Format("BSDL database loaded: {0} total definitions", BSDL_DATABASE.Count))
        End Sub

        Private Sub WriteBSDL(bsdl_name As String, d As BSDL_DEF)
            Dim ScriptPath As String = (New IO.FileInfo(MyLocation)).DirectoryName & "\Scripts\"
            Dim script_io As New IO.DirectoryInfo(ScriptPath)
            Dim fn_out As New IO.DirectoryInfo(script_io.Parent.FullName & "\JTAG_BSDL\")
            Dim fnname As String = fn_out.FullName & bsdl_name & ".fcs"
            Dim bsdl_file As New List(Of String)
            bsdl_file.Add(String.Format("index = BSDL.New(""{0}"",""{1}"")", d.PART_NAME, bsdl_name))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""ID_JEDEC"", {0})", "0x" & d.ID_JEDEC.ToString("X").PadLeft(8, "0"c)))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""ID_MASK"", {0})", "0x" & d.ID_MASK.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""IR_LEN"", {0})", d.IR_LEN.ToString()))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""BS_LEN"", {0})", d.BS_LEN.ToString()))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""IDCODE"", {0})", "0x" & d.IDCODE.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""BYPASS"", {0})", "0x" & d.BYPASS.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""INTEST"", {0})", "0x" & d.INTEST.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""EXTEST"", {0})", "0x" & d.EXTEST.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""SAMPLE"", {0})", "0x" & d.SAMPLE.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""CLAMP"", {0})", "0x" & d.CLAMP.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""HIGHZ"", {0})", "0x" & d.HIGHZ.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""PRELOAD"", {0})", "0x" & d.PRELOAD.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""USERCODE"", {0})", "0x" & d.USERCODE.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""SCAN_N"", {0})", "0x" & d.SCAN_N.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""ARM_ABORT"", {0})", "0x" & d.ARM_ABORT.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""ARM_DPACC"", {0})", "0x" & d.ARM_DPACC.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""ARM_APACC"", {0})", "0x" & d.ARM_APACC.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""RESTART"", {0})", "0x" & d.RESTART.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""MIPS_IMPCODE"", {0})", "0x" & d.MIPS_IMPCODE.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""MIPS_ADDRESS"", {0})", "0x" & d.MIPS_ADDRESS.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""MIPS_CONTROL"", {0})", "0x" & d.MIPS_CONTROL.ToString("X")))
            bsdl_file.Add(String.Format("BSDL(index).parameter(""DISVAL"", {0})", Utilities.BoolToInt(d.DISVAL)))
            Utilities.FileIO.WriteFile(bsdl_file.ToArray(), fnname)
        End Sub

        Public Function BSDL_Add(MFG As String, PART As String, PCKG As String) As Integer
            BSDL_Remove(PART, PCKG)
            Dim bsdl_obj As New BSDL_DEF
            bsdl_obj.MFG_NAME = MFG
            bsdl_obj.PART_NAME = PART
            bsdl_obj.PACKAGE = PCKG
            BSDL_DATABASE.Add(bsdl_obj)
            Dim created_index As Integer = BSDL_DATABASE.Count - 1
            If PrintAddedToDB Then
                PrintConsole("BSDL databased added device '" & bsdl_obj.ToString() & "' to index: " & created_index.ToString)
            End If
            Return created_index
        End Function

        Public Sub BSDL_Remove(PART As String, Optional package As String = "")
            For i = 0 To BSDL_DATABASE.Count - 1
                If BSDL_DATABASE(i).PART_NAME.ToUpper().Equals(PART.ToUpper) Then
                    If package.Equals("") OrElse BSDL_DATABASE(i).PACKAGE.Equals(package) Then
                        BSDL_DATABASE.RemoveAt(i)
                        Exit Sub
                    End If
                End If
            Next
        End Sub

        Public Function BSDL_Find(part_name As String, Optional package As String = "") As Integer
            For i = 0 To BSDL_DATABASE.Count - 1
                If BSDL_DATABASE(i).PART_NAME.ToUpper().Equals(part_name.ToUpper) Then
                    If package.Equals("") OrElse BSDL_DATABASE(i).PACKAGE.Equals(package) Then
                        Return i
                    End If
                End If
            Next
            Return -1
        End Function

        Public Function BSDL_SetParamater(library_index As Integer, param_name As String, param_value As UInt32) As Boolean
            If library_index > BSDL_DATABASE.Count - 1 Then Return False
            Dim bsdl As BSDL_DEF = BSDL_DATABASE(library_index)
            Select Case param_name.ToUpper
                Case "ID_JEDEC"
                    bsdl.ID_JEDEC = param_value
                Case "ID_MASK"
                    bsdl.ID_MASK = param_value
                Case "IR_LEN"
                    bsdl.IR_LEN = CByte(param_value And &HFF)
                Case "BS_LEN"
                    bsdl.BS_LEN = CUShort(param_value And &HFFFF)
                Case "IDCODE"
                    bsdl.IDCODE = param_value
                Case "BYPASS"
                    bsdl.BYPASS = param_value
                Case "INTEST"
                    bsdl.INTEST = param_value
                Case "EXTEST"
                    bsdl.EXTEST = param_value
                Case "SAMPLE"
                    bsdl.SAMPLE = param_value
                Case "CLAMP"
                    bsdl.CLAMP = param_value
                Case "HIGHZ"
                    bsdl.HIGHZ = param_value
                Case "PRELOAD"
                    bsdl.PRELOAD = param_value
                Case "USERCODE"
                    bsdl.USERCODE = param_value
                Case "SCAN_N"
                    bsdl.SCAN_N = CByte(param_value)
                Case "ARM_ABORT"
                    bsdl.ARM_ABORT = CByte(param_value)
                Case "ARM_DPACC"
                    bsdl.ARM_DPACC = CByte(param_value)
                Case "ARM_APACC"
                    bsdl.ARM_APACC = CByte(param_value)
                Case "RESTART"
                    bsdl.RESTART = CByte(param_value)
                Case "MIPS_IMPCODE"
                    bsdl.MIPS_IMPCODE = param_value
                Case "MIPS_ADDRESS"
                    bsdl.MIPS_ADDRESS = param_value
                Case "MIPS_CONTROL"
                    bsdl.MIPS_CONTROL = param_value
                Case "DISVAL"
                    bsdl.DISVAL = CBool(param_value)
                Case Else
                    Return False
            End Select
            Return True
        End Function

        Public Function BSDL_GetDefinition(jedec_id As UInt32) As BSDL_DEF
            For Each jtag_def In Me.BSDL_DATABASE
                If Not jtag_def.IDCODE = 0 Then
                    If jtag_def.ID_JEDEC = (jedec_id And jtag_def.ID_MASK) Then Return jtag_def
                End If
            Next
            Return Nothing
        End Function

#End Region

    End Class

    Public Enum JTAG_SPEED As Integer
        _40MHZ = 0  'Not supported on PCB 4.x
        _20MHZ = 1
        _10MHZ = 2
        _1MHZ = 3
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
        NONE = 3
    End Enum

    Public Class JTAG_DEVICE
        Public Property IDCODE As UInt32 'Contains ID_CODEs
        Public Property MANUID As UInt16
        Public Property PARTNU As UInt16
        Public Property VERSION As Short = 0
        Public Property ACCESS As JTAG_MEM_ACCESS
        Public Property IR_LENGTH As UInt32 'Loaded from BSDL/single device chain/console command
        Public Property BSDL As BSDL_DEF
        'Returns the name of this device
        Public Overrides Function ToString() As String
            If BSDL Is Nothing Then
                Return JEDEC_GetManufacturer(Me.IDCODE) & " 0x" & Me.PARTNU.ToString("X")
            Else
                Return JEDEC_GetManufacturer(Me.IDCODE) & " " & Me.BSDL.PART_NAME
            End If
        End Function

    End Class

    Public Class BSDL_DEF
        Property ID_JEDEC As UInt32
        Property ID_MASK As UInt32 = &HFFFFFFFFUI
        Property MFG_NAME As String
        Property PART_NAME As String
        Property PACKAGE As String
        Property IR_LEN As Byte  'Number of bits the IR uses
        Property BS_LEN As UInt16 'Number of bits for PRELOAD/EXTEST
        Property IDCODE As UInt32
        Property BYPASS As UInt32
        Property INTEST As UInt32
        Property EXTEST As UInt32
        Property SAMPLE As UInt32
        Property CLAMP As UInt32
        Property HIGHZ As UInt32
        Property PRELOAD As UInt32
        Property USERCODE As UInt32
        Property DISVAL As Boolean 'This is the value that will disable the control cell (false=0, true=1)
        'ARM SPECIFIC REGISTERS
        Property ARM_ABORT As Byte
        Property ARM_DPACC As Byte
        Property ARM_APACC As Byte
        Property SCAN_N As Byte
        Property RESTART As Byte
        'MIPS SPECIFIC
        Property MIPS_IMPCODE As UInt32
        Property MIPS_ADDRESS As UInt32
        Property MIPS_CONTROL As UInt32

        Public Overrides Function ToString() As String
            If Me.PACKAGE = "" Then
                Return Me.MFG_NAME & "_" & Me.PART_NAME
            Else
                Return Me.MFG_NAME & "_" & Me.PART_NAME & "_(" & Me.PACKAGE & ")"
            End If
        End Function

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

    Public Module JTAG_TOOLS

        Public Function JEDEC_GetManufacturer(JEDECID As UInt32) As String
            'Dim PARTNU = CUShort((JEDECID And &HFFFF000) >> 12)
            Dim MANUID = CUShort((JEDECID And &HFFE) >> 1)
            Select Case MANUID
                Case 1
                    Return "Spansion"
                Case 4
                    Return "Fujitsu"
                Case 7
                    Return "Hitachi"
                Case 9
                    Return "Intel"
                Case 21
                    Return "Philips"
                Case 31
                    Return "Atmel"
                Case 32
                    Return "ST"
                Case 33
                    Return "Lattice"
                Case 52
                    Return "Cypress"
                Case 53
                    Return "DEC"
                Case 73
                    Return "Xilinx"
                Case 110
                    Return "Altera"
                Case 112 '0x70
                    Return "Qualcomm"
                Case 191 '0xBF
                    Return "Broadcom"
                Case 194
                    Return "MXIC"
                Case 231
                    Return "Microsemi"
                Case 239
                    Return "Winbond"
                Case 336
                    Return "Signetics"
                Case 571 '0x23B
                    Return "ARM" 'ARM Ltd.
                Case Else
                    Return "Unknown"
            End Select
        End Function

    End Module


End Namespace

