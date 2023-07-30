'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2019 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'ACKNOWLEDGEMENT: USB driver functionality provided by LibUsbDotNet (sourceforge.net/projects/libusbdotnet)

Imports FlashcatUSB.FlashMemory
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

        Public WithEvents SW_TAP As JTAG_STATE_CONTROLLER 'JTAG state machine for Classic

        Sub New(ByVal parent_if As FCUSB_DEVICE)
            FCUSB = parent_if
            BSDL_Init()
        End Sub
        'Connects to the target device
        Public Function Init(Optional internal_logic As Boolean = False) As Boolean
            Try
                Me.Detected = False
                Me.Count = 0
                Me.Chain_Length = -1
                Me.SELECTED_INDEX = 0
                Devices.Clear()
                If ((FCUSB.HWBOARD = USB.FCUSB_BOARD.Professional_PCB4) Or (FCUSB.HWBOARD = USB.FCUSB_BOARD.Mach1)) And (Not internal_logic) Then
                    If Me.TCK_SPEED = JTAG_SPEED._10MHZ Then
                        WriteConsole("JTAG TCK speed: 10 MHz")
                        JSP.Actual_Hertz = 10000000
                    ElseIf Me.TCK_SPEED = JTAG_SPEED._20MHZ Then
                        WriteConsole("JTAG TCK speed: 20 MHz")
                        JSP.Actual_Hertz = 20000000
                    End If
                    FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_INIT, Nothing, Me.TCK_SPEED)
                Else
                    If ((FCUSB.HWBOARD = USB.FCUSB_BOARD.Professional_PCB4) Or (FCUSB.HWBOARD = USB.FCUSB_BOARD.Mach1)) Then
                        FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_INIT, Nothing, (1 << 16) Or Me.TCK_SPEED)
                    Else
                        FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_INIT, Nothing, 0)
                    End If
                    JSP.Actual_Hertz = 500000
                    TAP_EnableSoftwareStateMachine()
                End If
                If Not TAP_Detect() Then Return False
                WriteConsole("JTAG chain detected: " & Me.Count & " devices")
                WriteConsole("JTAG TDI size: " & Me.Chain_Length & " bits")
                For i = 0 To Me.Count - 1
                    Dim jtag_dev_name As String = GetJedecDescription(Devices(i).IDCODE)
                    WriteConsole("JEDEC ID: 0x" & Hex(Devices(i).IDCODE).PadLeft(8, "0") & " (" & jtag_dev_name & ")")
                Next
                Select_Device(0) 'By default, select the first chain item
                If Devices(0).IDCODE = &HBA02477 Then
                    Dim tdo(3) As Byte
                    Dim tdi(3) As Byte
                    Dim tdo32 As UInt32 = 0
                    Dim REG_VALUE As UInt32
                    Reset_StateMachine() 'Now at Select_DR
                    TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR)
                    ShiftTDI(4, {Devices(0).BSDL.IDCODE}, Nothing, True)
                    TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
                    ShiftTDI(32, {0, 0, 0, 0}, tdo, True)
                    TAP_GotoState(JTAG_MACHINE_STATE.Update_DR)
                    Array.Reverse(tdo)
                    Dim ARM_ID As UInt32 = Utilities.Bytes.ToUInt32(tdo) '0x0BA02477
                    WriteConsole("ARM ID code: 0x" & Conversion.Hex(ARM_ID).PadLeft(8, "0"))
                    ARM_MDAP_INIT()
                    REG_VALUE = CTRLSTAT.CSYSPWRUPREQ Or CTRLSTAT.CDBGPWRUPREQ Or CTRLSTAT.STICKYERR
                    ARM_DPACC(REG_VALUE, ARM_DP_REG.CTRL_STAT, ARM_RnW.WR)
                    ARM_APACC(&H0, ARM_AP_REG.IDR, ARM_RnW.RD)
                    Dim AHB_AP As UInt32 = ARM_APACC(&H0, ARM_AP_REG.IDR, ARM_RnW.RD, False) '0x04770041
                    WriteConsole("AHB-AP (IDR: 0x" & Conversion.Hex(AHB_AP).PadLeft(8, "0") & ")")
                    ARM_APACC(&H0, ARM_AP_REG.BASE, ARM_RnW.RD)
                    Dim BASE_REG As UInt32 = ARM_APACC(&H0, ARM_AP_REG.BASE, ARM_RnW.RD, False) '0xE00FB003

                    'Dim d1, d2, d3, d4 As UInt32
                    'Dim r1, r2, r3, r4 As ARM_DP_REG
                    'Dim w1, w2, w3, w4 As ARM_RnW

                    'ARM_DP_Decode(&H280000102, d1, r1, w1)
                    'ARM_DP_Decode(&H784, d2, r2, w2)
                    'ARM_AP_Decode(&H5, d3, r3, w3)
                    'ARM_DP_Decode(&H7, d4, r4, w4)
                    'Beep()

                    'AHB-AP
                    '"PPB ROM Table" at 0xe00ff000
                    '0xE00FE000	Cortex-M7 PPB ROM Table
                    '0xE00FB000 <-- from our debugger

                    'Find ROM base (0xE00FB000)
                ElseIf Devices(0).IDCODE = &H1BA141CF Then
                    ' AAI_DEBUG()
                End If
                Return True
            Catch ex As Exception
            End Try
            Return False
        End Function
        'IR=0xA
        Private Sub ARM_DP_Decode(data_in As UInt64, ByRef data_out As UInt32, ByRef dp_reg As ARM_DP_REG, ByRef r_w As ARM_RnW)
            r_w = (data_in And 1)
            Dim b As Byte = ((data_in >> 1) And 3)
            dp_reg = (b << 2)
            data_out = (data_in >> 3)
            'data_out = REVERSE32(data_out)
        End Sub
        'IR=0xB
        Private Sub ARM_AP_Decode(data_in As UInt64, ByRef data_out As UInt32, ByRef ap_reg As ARM_AP_REG, ByRef r_w As ARM_RnW)
            r_w = (data_in And 1)
            Dim b As Byte = ((data_in >> 1) And 3)
            ap_reg = (b << 2)
            data_out = (data_in >> 3)
            'data_out = REVERSE32(data_out)
        End Sub
        'Accesses CTRL/STAT, SELECT and RDBUFF
        Private Function ARM_DPACC(reg32 As UInt32, dp_reg As ARM_DP_REG, read_write As ARM_RnW, Optional goto_state As Boolean = True) As UInt32
            Dim tdo(3) As Byte
            If goto_state Then
                TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR)
                ShiftTDI(Devices(0).BSDL.IR_LEN, {Devices(0).BSDL.ARM_DPACC}, Nothing, True)
            End If
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            Dim b5 As Byte = ((reg32 And &H1F) << 3) Or ((dp_reg And &HF) >> 1) Or read_write : reg32 >>= 5
            Dim b4 As Byte = (reg32 And &HFF) : reg32 >>= 8
            Dim b3 As Byte = (reg32 And &HFF) : reg32 >>= 8
            Dim b2 As Byte = (reg32 And &HFF) : reg32 >>= 8
            Dim b1 As Byte = (reg32 And &H7)
            ShiftTDI(35, {b5, b4, b3, b2, b1}, tdo, True)
            TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            Dim status As Byte = (tdo(0) And 3)
            Dim reg32_out As UInt32 = ((tdo(1) And 7) << 5) Or (tdo(0) >> 3)
            reg32_out = reg32_out Or (CUInt(((tdo(2) And 7) << 5) Or (tdo(1) >> 3)) << 8)
            reg32_out = reg32_out Or (CUInt(((tdo(3) And 7) << 5) Or (tdo(2) >> 3)) << 16)
            reg32_out = reg32_out Or (CUInt(((tdo(4) And 7) << 5) Or (tdo(3) >> 3)) << 24)
            Return reg32_out
        End Function
        'Accesses port registers (AHB-AP)
        Private Function ARM_APACC(reg32 As UInt32, ap_reg As ARM_AP_REG, read_write As ARM_RnW, Optional goto_state As Boolean = True) As UInt32
            If Not ((ap_reg >> 4) = (ARM_REG_ADDR >> 4)) Then
                ARM_DPACC((ap_reg And &HF0), ARM_DP_REG.ADDR, ARM_RnW.WR)
            End If
            ARM_REG_ADDR = ap_reg
            Dim tdo(3) As Byte
            If goto_state Then
                TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR)
                ShiftTDI(Devices(0).BSDL.IR_LEN, {Devices(0).BSDL.ARM_APACC}, Nothing, True)
            End If
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            Dim b5 As Byte = ((reg32 And &H1F) << 3) Or ((ap_reg And &HF) >> 1) Or read_write : reg32 >>= 5
            Dim b4 As Byte = (reg32 And &HFF) : reg32 >>= 8
            Dim b3 As Byte = (reg32 And &HFF) : reg32 >>= 8
            Dim b2 As Byte = (reg32 And &HFF) : reg32 >>= 8
            Dim b1 As Byte = (reg32 And &H7)
            ShiftTDI(35, {b5, b4, b3, b2, b1}, tdo, True)
            TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            Dim status As Byte = (tdo(0) And 3)
            Dim reg32_out As UInt32 = ((tdo(1) And 7) << 5) Or (tdo(0) >> 3)
            reg32_out = reg32_out Or (CUInt(((tdo(2) And 7) << 5) Or (tdo(1) >> 3)) << 8)
            reg32_out = reg32_out Or (CUInt(((tdo(3) And 7) << 5) Or (tdo(2) >> 3)) << 16)
            reg32_out = reg32_out Or (CUInt(((tdo(4) And 7) << 5) Or (tdo(3) >> 3)) << 24)
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

        'Attempts to auto-detect a JTAG device on the TAP, returns the IR Length of the device
        Private Function TAP_Detect() As Boolean
            Dim r_data(63) As Byte
            FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_DETECT, r_data)
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
                    n.BSDL = BSDL_GetDefinition(ID32)
                    If n.BSDL Is Nothing Then WriteConsole("BSDL definition not found for JEDEC ID: 0x" & Hex(ID32).PadLeft(8, "0"))
                    Devices.Add(n)
                    Me.Detected = True
                End If
            Next
            If Me.Detected Then
                Me.Count = Devices.Count
                Return True
            End If
            Return False
        End Function

        Public Sub Configure(proc_type As PROCESSOR)
            Me.Devices(Me.SELECTED_INDEX).ACCESS = JTAG_MEM_ACCESS.NONE
            If proc_type = PROCESSOR.MIPS Then
                GUI.PrintConsole("Configure JTAG engine for MIPS processor")
                Dim IMPCODE As UInt32 = ReadWriteReg32(Devices(SELECTED_INDEX).BSDL.MIPS_IMPCODE)
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

#Region "Boundary Scan Programmer"
        Public Property CFI As NOR_CFI 'Contains CFI table information (NOR)

        Private Const IO_INPUT As Boolean = False '0
        Private Const IO_OUTPUT As Boolean = True '1
        Private Const LOGIC_LOW As Boolean = False '0
        Private Const LOGIC_HIGH As Boolean = True '1

        Private BoundaryMap As New List(Of BoundaryCell)
        Private BSDL_FLASH_DEVICE As FlashMemory.Device
        Private BSDL_IF As MemoryInterface.MemoryDeviceInstance = Nothing
        Private SetupReady As Boolean = False
        Private Property BSDL_IO As BSDL_DQ_IO
        Private Property BDR_DQ_SIZE As Integer = 0
        Private Property BDR_ADDR_SIZE As Integer = 0
        Private Property BSR_SIZE As Integer = 0
        Private Property BDR_CTRL_EN As Boolean = False

        Private BSR_REG(127) As Byte

        Private Enum BSDL_DQ_IO As UInt32
            X16 = 1
            X8 = 2
            X8_OVER_X16 = 3
        End Enum

        Public Sub BoundaryScan_Setup()
            SetupReady = False
            If MySettings.LICENSED_TO.Equals("") Then
                WriteConsole("Boundary Scan Library only available with commercial license")
                SetStatus("Error: commercial license required")
                Exit Sub
            End If
            If Devices(0).BSDL.BS_LEN = 0 Then
                WriteConsole("Boundary Scan error: BSDL scan length not not specified")
                Exit Sub
            End If
            BoundaryMap.Clear()
            Me.BDR_DQ_SIZE = 0
            Me.BDR_ADDR_SIZE = 0
            Me.BSR_SIZE = Devices(0).BSDL.BS_LEN
            Me.BDR_CTRL_EN = Devices(0).BSDL.CONTROL_DISABLE  'SOMETHING
            ReDim BSR_REG(127)
            BSDL_FLASH_DEVICE = Nothing
            Dim setup_data(6) As Byte
            setup_data(0) = CByte((Devices(0).BSDL.EXTEST) And 255)
            setup_data(1) = CByte((Devices(0).BSDL.EXTEST >> 8) And 255)
            setup_data(2) = CByte((Devices(0).BSDL.SAMPLE) And 255)
            setup_data(3) = CByte((Devices(0).BSDL.SAMPLE >> 8) And 255)
            setup_data(4) = CByte((Devices(0).BSDL.BS_LEN) And 255)
            setup_data(5) = CByte((Devices(0).BSDL.BS_LEN >> 8) And 255)
            setup_data(6) = CByte(Devices(0).BSDL.CONTROL_DISABLE)
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data)
            SetupReady = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_BDR_SETUP)
        End Sub

        Public Function BoundaryScan_Init(EnableX8Mode As Boolean) As Boolean
            If Not SetupReady Then Return False
            WriteConsole("JTAG Boundary Scan Programmer")
            If (Not FCUSB.HasLogic) Then
                WriteConsole("This feature is only available using FlashcatUSB Professional") : Return False
            End If
            If Not BSDL_Is_Configured() Then Return False
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
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, pin_data)
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_BDR_ADDPIN)
                Utilities.Sleep(5)
            Next
            If EnableX8Mode Then
                Me.BSDL_IO = BSDL_DQ_IO.X8_OVER_X16
            Else
                If Me.BDR_DQ_SIZE = 8 Then
                    Me.BSDL_IO = BSDL_DQ_IO.X8
                ElseIf Me.BDR_DQ_SIZE = 16 Then
                    Me.BSDL_IO = BSDL_DQ_IO.X16
                End If
            End If
            Dim dw As UInt32 = 0 'X8
            If Me.BSDL_IO = BSDL_DQ_IO.X16 Or Me.BSDL_IO = BSDL_DQ_IO.X8_OVER_X16 Then dw = 1
            If (Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_BDR_INIT, Nothing, dw)) Then
                WriteConsole("Error: Boundary Scan init failed") : Return False
            End If
            Return True
        End Function

#Region "Software mode"

        Public Sub BoundaryScan_SW_Init()
            Reset_StateMachine() 'Now at Select_DR
            JTAG_BDR_Sample(BSR_REG) 'Load our REG with good values
            JTAG_BDR_CreateOutput(GetBoundaryCell("WE#"), LOGIC_HIGH)
            JTAG_BDR_CreateOutput(GetBoundaryCell("OE#"), LOGIC_LOW)
            JTAG_BDR_CreateOutput(GetBoundaryCell("CE#"), LOGIC_LOW)
            JTAG_BDR_CreateOutput(GetBoundaryCell("RESET#"), LOGIC_HIGH)
            For i = 0 To Me.BDR_DQ_SIZE - 1
                JTAG_BDR_CreateInput(GetBoundaryCell("DQ" & i.ToString))
            Next
            For i = 0 To Me.BDR_ADDR_SIZE - 1
                JTAG_BDR_CreateOutput(GetBoundaryCell("AD" & i.ToString), LOGIC_LOW)
            Next
            JTAG_BDR_WriteBSR()
        End Sub

        Private Sub BoundaryScan_SW_WriteAddress(addr32 As UInt32)
            For i = 0 To Me.BDR_ADDR_SIZE - 1
                Dim state As Boolean
                If ((addr32 >> i) And 1) Then
                    state = LOGIC_HIGH
                Else
                    state = LOGIC_LOW
                End If
                JTAG_BDR_CreateOutput(GetBoundaryCell("AD" & i.ToString), state)
            Next
        End Sub

        Public Function BoundaryScan_SW_ReadWord(base_addr As UInt32) As UInt16
            BoundaryScan_SW_WriteAddress(base_addr)
            JTAG_BDR_ReadMode()
            Dim DQ16 As UInt16 = 0
            Dim CLONE() As Byte = Nothing
            JTAG_BDR_ReadBSR(CLONE)
            For i As Integer = 0 To BDR_DQ_SIZE - 1
                Dim c As BoundaryCell = GetBoundaryCell("DQ" & i.ToString)
                Dim value As Boolean = BSR_GetBit(CLONE, c.INPUT)
                If value Then DQ16 = DQ16 Or (1 << i)
            Next
            Return DQ16
        End Function

        Private Sub JTAG_BDR_ReadMode()
            For i As Integer = 0 To BDR_DQ_SIZE - 1
                BSR_SetControlState(GetBoundaryCell("DQ" & i.ToString).CONTROL, IO_INPUT)
            Next
            BSR_SetBit(BSR_REG, GetBoundaryCell("OE#").OUTPUT, LOGIC_LOW)
            JTAG_BDR_WriteBSR()
        End Sub

        Private Sub JTAG_BDR_WriteMode()
            For i As Integer = 0 To BDR_DQ_SIZE - 1
                BSR_SetControlState(GetBoundaryCell("DQ" & i.ToString).CONTROL, IO_OUTPUT)
            Next
            BSR_SetBit(BSR_REG, GetBoundaryCell("OE#").OUTPUT, LOGIC_HIGH)
            JTAG_BDR_WriteBSR()
        End Sub

        Private Sub JTAG_BDR_CreateOutput(cell As BoundaryCell, logic_level As Boolean)
            BSR_SetBit(BSR_REG, cell.OUTPUT, logic_level)
            BSR_SetBit(BSR_REG, cell.INPUT, LOGIC_LOW)
            BSR_SetControlState(cell.CONTROL, IO_OUTPUT)
        End Sub

        Private Sub JTAG_BDR_CreateInput(cell As BoundaryCell)
            BSR_SetControlState(cell.CONTROL, IO_INPUT)
        End Sub

        Private Sub BSR_SetControlState(control_pin As Integer, is_output As Boolean)
            If BDR_CTRL_EN Then
                BSR_SetBit(BSR_REG, control_pin, Not is_output)
            Else
                BSR_SetBit(BSR_REG, control_pin, is_output)
            End If
        End Sub

        Private Sub BSR_SetBit(data_reg() As Byte, bit_index As UInt16, value As Boolean)
            Dim arr_ind As UInt16 = Math.Floor(bit_index / 8)
            Dim offset As Byte = (bit_index Mod 8)
            If value Then
                data_reg(arr_ind) = (BSR_REG(arr_ind) Or (1 << offset)) 'bit_value = HIGH
            Else
                data_reg(arr_ind) = BSR_REG(arr_ind) And (Not CByte(1 << offset)) 'bit_value=LOW
            End If
        End Sub

        Private Function BSR_GetBit(data_reg() As Byte, bit_index As UInt16) As Boolean
            Dim arr_ind As UInt16 = Math.Floor(bit_index / 8)
            Dim offset As Byte = (bit_index Mod 8)
            If (data_reg(arr_ind) And (CByte(1) << offset)) > 0 Then
                Return True
            Else
                Return False
            End If
        End Function

        Private Sub JTAG_BDR_WriteBSR()
            Dim BDR_REG_CLONE(BSR_REG.Length - 1) As Byte
            Array.Copy(BSR_REG, BDR_REG_CLONE, BDR_REG_CLONE.Length)
            JTAG_BDR_Extest(BDR_REG_CLONE)
        End Sub

        Private Sub JTAG_BDR_ReadBSR(ByRef BDR() As Byte)
            Dim BDR_REG_CLONE(BSR_REG.Length - 1) As Byte
            Array.Copy(BSR_REG, BDR_REG_CLONE, BDR_REG_CLONE.Length)
            JTAG_BDR_Extest(BDR_REG_CLONE)
            BDR = BDR_REG_CLONE
        End Sub

        Private Sub JTAG_BDR_Extest(ByRef REG() As Byte)
            Dim TDO(REG.Length - 1) As Byte
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR)
            ShiftTDI(8, {Devices(0).BSDL.EXTEST}, Nothing, True)
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            ShiftTDI(Me.BSR_SIZE, REG, REG, True)
            TAP_GotoState(JTAG_MACHINE_STATE.Update_DR)
        End Sub

        Private Sub JTAG_BDR_Sample(ByRef REG() As Byte)
            Dim TDO(REG.Length - 1) As Byte
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR)
            ShiftTDI(8, {Devices(0).BSDL.SAMPLE}, Nothing, True)
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            ShiftTDI(Me.BSR_SIZE, REG, REG, True)
            TAP_GotoState(JTAG_MACHINE_STATE.Update_DR)
        End Sub

        Private Sub PrintCellData(bsr_size As Integer, data_reg() As Byte, filename As String)
            Dim str As New Text.StringBuilder(bsr_size)
            Dim ptr As Integer
            For i = 0 To Math.Ceiling(bsr_size / 8) - 1
                For x = 0 To 7
                    Dim b As Boolean = CBool((BSR_REG(i) >> x) And 1)
                    If b Then
                        str.Insert(ptr, "1"c)
                    Else
                        str.Insert(ptr, "0"c)
                    End If
                    ptr += 1
                    If ptr = 708 Then Exit For
                Next
            Next
            ptr = 0
            Dim cell((bsr_size / 3) - 1) As String
            Dim str2 As New List(Of String)
            For i = 0 To cell.Length - 1
                Dim s As String = "Scan cell " & i.ToString.PadLeft(3, "0") & " ("
                s &= str.Chars(ptr) & "[O] "
                s &= str.Chars(ptr + 1) & "[C] "
                s &= str.Chars(ptr + 2) & "[I])"
                ptr += 3
                str2.Add(s)
            Next
            Utilities.FileIO.WriteFile(str2.ToArray, filename)
        End Sub

#End Region

        Public Function BoundaryScan_Detect() As Boolean
            Me.CFI = Nothing
            Dim FLASH_RESULT As PARALLEL_NOR_NAND.FlashDetectResult
            If Me.BSDL_IO = BSDL_DQ_IO.X8 Then
                FLASH_RESULT = PARALLEL_NOR_NAND.GetFlashResult(BSDL_ReadIdent(False)) 'X8 Device
            Else
                FLASH_RESULT = PARALLEL_NOR_NAND.GetFlashResult(BSDL_ReadIdent(True)) 'X16 Device
            End If
            If FLASH_RESULT.Successful Then
                Me.CFI = New NOR_CFI(BSDL_GetCFI())
                Dim device_matches() As FlashMemory.Device
                Dim chip_id_str As String = Hex(FLASH_RESULT.MFG).PadLeft(2, "0") & Hex((CUInt(FLASH_RESULT.ID1) << 16) Or FLASH_RESULT.ID2).PadLeft(8, "0")
                WriteConsole("Flash detected: DEVICE ID: 0x" & chip_id_str)
                device_matches = FlashDatabase.FindDevices(FLASH_RESULT.MFG, FLASH_RESULT.ID1, FLASH_RESULT.ID2, MemoryType.PARALLEL_NOR)
                If (device_matches IsNot Nothing AndAlso device_matches.Count > 0) Then
                    If (device_matches.Count > 1) AndAlso Me.CFI.IS_VALID Then
                        For i = 0 To device_matches.Count - 1
                            If device_matches(i).PAGE_SIZE = Me.CFI.WRITE_BUFFER_SIZE Then
                                BSDL_FLASH_DEVICE = device_matches(i) : Exit For
                            End If
                        Next
                    End If
                    If BSDL_FLASH_DEVICE Is Nothing Then BSDL_FLASH_DEVICE = device_matches(0)
                    WriteConsole(String.Format(RM.GetString("flash_detected"), BSDL_FLASH_DEVICE.NAME, Format(BSDL_FLASH_DEVICE.FLASH_SIZE, "#,###")))
                    Me.BSDL_IF = JTAG_Connect_BSDL(FCUSB)
                Else
                    WriteConsole("CFI Flash device not found in library")
                End If
            Else
                WriteConsole("No CFI Flash (X8/X16) device detected")
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
                WriteConsole("Error, AQ pins need to be have at least 16 bits") : Return False
            End If
            If Not (Me.BDR_DQ_SIZE = 8 Or Me.BDR_DQ_SIZE = 16) Then
                WriteConsole("Error, DQ pins need to be assigned either 8 or 16 bits") : Return False
            End If
            For i = 0 To Me.BDR_ADDR_SIZE - 1
                If BoundaryScan_GetPinIndex("AD" & i.ToString) = -1 Then
                    WriteConsole("Error, missing address pin: AD" & i.ToString) : Return False
                End If
            Next
            For i = 0 To Me.BDR_DQ_SIZE - 1
                If BoundaryScan_GetPinIndex("DQ" & i.ToString) = -1 Then
                    WriteConsole("Error, missing address pin: DQ" & i.ToString) : Return False
                End If
            Next
            If BoundaryScan_GetPinIndex("WE#") = -1 Then
                WriteConsole("Error, missing address pin: WE#") : Return False
            End If
            If BoundaryScan_GetPinIndex("OE#") = -1 Then
                WriteConsole("Error, missing address pin: OE#") : Return False
            End If
            WriteConsole("Interface configured: X" & Me.BDR_DQ_SIZE.ToString & " (" & Me.BDR_ADDR_SIZE & "-bit address)")
            Return True
        End Function

        Private Function BSDL_ReadIdent(X16_MODE As Boolean) As Byte()
            Dim ident(7) As Byte
            Dim SHIFT As UInt32 = 0
            If X16_MODE Then SHIFT = 1
            BSDL_ResetDevice()
            Utilities.Sleep(1)
            BSDL_WriteCmdData(&H5555, &HAA)
            BSDL_WriteCmdData(&H2AAA, &H55)
            BSDL_WriteCmdData(&H5555, &H90)
            Utilities.Sleep(10)
            ident(0) = CByte(BSDL_ReadWord(0) And &HFF)             'MFG
            Dim ID1 As UInt16 = BSDL_ReadWord(1 << SHIFT)
            If Not X16_MODE Then ID1 = (ID1 And &HFF)               'X8 ID1
            ident(1) = CByte((ID1 >> 8) And &HFF)                   'ID1(UPPER)
            ident(2) = CByte(ID1 And &HFF)                          'ID1(LOWER)
            ident(3) = CByte(BSDL_ReadWord(&HE << SHIFT) And &HFF)  'ID2
            ident(4) = CByte(BSDL_ReadWord(&HF << SHIFT) And &HFF)  'ID3
            BSDL_ResetDevice()
            Utilities.Sleep(1)
            Return ident
        End Function

        Private Sub BSDL_ResetDevice()
            BSDL_WriteCmdData(&H5555, &HAA) 'Standard
            BSDL_WriteCmdData(&H2AAA, &H55)
            BSDL_WriteCmdData(&H5555, &HF0)
            BSDL_WriteCmdData(0, &HFF)
            BSDL_WriteCmdData(0, &HF0) 'Intel
        End Sub

        Private Function BSDL_GetCFI() As Byte()
            Dim cfi_data(31) As Byte
            Dim SHIFT As UInt32 = 0
            If Me.BSDL_IO = BSDL_DQ_IO.X16 Or Me.BSDL_IO = BSDL_DQ_IO.X8_OVER_X16 Then SHIFT = 1
            Try
                BSDL_WriteCmdData(&H55, &H98)
                For i = 0 To cfi_data.Length - 1
                    cfi_data(i) = CByte(BSDL_ReadWord((&H10 + i) << SHIFT) And 255)
                Next
                If cfi_data(0) = &H51 And cfi_data(1) = &H52 And cfi_data(2) = &H59 Then Return cfi_data
                BSDL_WriteCmdData(&H5555, &HAA)
                BSDL_WriteCmdData(&H2AAA, &H55)
                BSDL_WriteCmdData(&H5555, &H98)
                For i = 0 To cfi_data.Length - 1
                    cfi_data(i) = CByte(BSDL_ReadWord((&H10 + i) << SHIFT) And 255)
                Next
                If cfi_data(0) = &H51 And cfi_data(1) = &H52 And cfi_data(2) = &H59 Then Return cfi_data
            Catch ex As Exception
            Finally
                BSDL_ResetDevice()
            End Try
            Return Nothing
        End Function

        'Defines a pin. Output cell can be output/bidir, control_cell can be -1 if it is output_cell+1, and input_cell is used when not bidir
        Public Sub BoundaryScan_AddPin(signal_name As String, output_cell As Integer, control_cell As Integer, input_cell As Integer)
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
                WriteConsole("Boundary Scan Programmer: Pin name not reconized: " & pin_desc.pin_name)
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

        Public Sub BoundaryScan_SetBSR(output_cell As Integer, control_cell As Integer, level As Boolean)
            Dim dw As UInt32 = (control_cell << 16) Or output_cell
            If level Then dw = (dw Or (1UL << 31))
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
            Public OUTPUT As UInt16
            Public INPUT As UInt16 'Some devices use bidir cells (this is for seperate o/i cells)
            Public CONTROL As UInt16
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

        Public Function BSDL_ReadWord(base_addr As UInt32) As UInt16
            Dim dt(3) As Byte
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_BDR_RDMEM, dt, base_addr)
            Dim DQ16 As UInt16 = (CUShort(dt(2)) << 8) Or CUShort(dt(3))
            If Me.BSDL_IO = BSDL_DQ_IO.X16 Then
                Return DQ16
            ElseIf Me.BSDL_IO = BSDL_DQ_IO.X8 Then
                Return (DQ16 And 255)
            ElseIf Me.BSDL_IO = BSDL_DQ_IO.X8_OVER_X16 Then
                Return (DQ16 And 255)
            End If
            Return 0
        End Function

        Public Sub BSDL_WriteCmdData(base_addr As UInt32, data16 As UInt16)
            Dim dt_out(5) As Byte
            dt_out(0) = CByte(base_addr And 255)
            dt_out(1) = CByte((base_addr >> 8) And 255)
            dt_out(2) = CByte((base_addr >> 16) And 255)
            dt_out(3) = CByte((base_addr >> 24) And 255)
            dt_out(4) = CByte(data16 And 255)
            dt_out(5) = CByte((data16 >> 8) And 255)
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, dt_out)
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_BDR_WRCMD)
        End Sub

        Public Sub BSDL_WriteMemAddress(base_addr As UInt32, data16 As UInt16)
            Dim dt_out(5) As Byte
            dt_out(0) = CByte(base_addr And 255)
            dt_out(1) = CByte((base_addr >> 8) And 255)
            dt_out(2) = CByte((base_addr >> 16) And 255)
            dt_out(3) = CByte((base_addr >> 24) And 255)
            dt_out(4) = CByte(data16 And 255)
            dt_out(5) = CByte((data16 >> 8) And 255)
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, dt_out)
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_BDR_WRMEM)
        End Sub

        Private Function BoundaryScan_GetSetupPacket(Address As UInt32, Count As UInt32, PageSize As UInt16) As Byte()
            Dim addr_bytes As Byte = 0
            Dim data_in(19) As Byte '18 bytes total
            data_in(0) = CByte(Address And 255)
            data_in(1) = CByte((Address >> 8) And 255)
            data_in(2) = CByte((Address >> 16) And 255)
            data_in(3) = CByte((Address >> 24) And 255)
            data_in(4) = CByte(Count And 255)
            data_in(5) = CByte((Count >> 8) And 255)
            data_in(6) = CByte((Count >> 16) And 255)
            data_in(7) = CByte((Count >> 24) And 255)
            data_in(8) = CByte(PageSize And 255) 'This is how many bytes to increment between operations
            data_in(9) = CByte((PageSize >> 8) And 255)
            Return data_in
        End Function

        Public ReadOnly Property BoundaryScan_DeviceName() As String
            Get
                Dim NOR_FLASH As P_NOR = DirectCast(BSDL_FLASH_DEVICE, P_NOR)
                Return NOR_FLASH.NAME
            End Get
        End Property

        Public ReadOnly Property BoundaryScan_DeviceSize As Long
            Get
                Dim NOR_FLASH As FlashMemory.P_NOR = DirectCast(BSDL_FLASH_DEVICE, FlashMemory.P_NOR)
                Return NOR_FLASH.AVAILABLE_SIZE
            End Get
        End Property

        Public Function BoundaryScan_ReadFlash(base_addr As UInt32, read_count As UInt32) As Byte()
            Dim byte_count As Integer = read_count
            If Me.BSDL_IO = BSDL_DQ_IO.X8_OVER_X16 Then
                byte_count = byte_count * 2
                base_addr = base_addr << 1
            End If
            Dim data_out(byte_count - 1) As Byte 'Bytes we want to read
            Dim data_left As UInt32 = byte_count
            Dim ptr As Integer = 0
            Dim PacketSize As UInt32 = 8192
            While data_left > 0
                Dim packet_size As UInt32 = Math.Min(8192, data_left)
                Dim packet_data(packet_size - 1) As Byte
                Dim setup_data() As Byte = BoundaryScan_GetSetupPacket(base_addr, packet_size, 0)
                Dim result As Boolean = FCUSB.USB_SETUP_BULKIN(USB.USBREQ.JTAG_BDR_RDFLASH, setup_data, packet_data, 0)
                If Not result Then Return Nothing
                Array.Copy(packet_data, 0, data_out, ptr, packet_size)
                ptr += packet_size
                base_addr += packet_size
                data_left -= packet_size
            End While
            If Me.BSDL_IO = BSDL_DQ_IO.X8_OVER_X16 Then
                Dim new_data_out(read_count - 1) As Byte
                For i = 0 To byte_count - 1 Step 2
                    new_data_out(i >> 1) = data_out(i)
                Next
                Return new_data_out
            Else
                Return data_out
            End If
        End Function

        Public Sub BoundaryScan_WaitForReady()
            Utilities.Sleep(100)
        End Sub

        Public Function BoundaryScan_SectorFind(sector_index As UInt32) As Long
            Dim base_addr As UInt32 = 0
            If sector_index > 0 Then
                For i As UInt32 = 0 To sector_index - 1
                    base_addr += BoundaryScan_GetSectorSize(i)
                Next
            End If
            Return base_addr
        End Function

        Public Function BoundaryScan_SectorWrite(sector_index As UInt32, data() As Byte) As Boolean
            Dim Addr32 As UInteger = BoundaryScan_SectorFind(sector_index)
            Return BoundaryScan_WriteFlash(Addr32, data)
        End Function

        Public Function BoundaryScan_SectorCount() As UInt32
            Dim NOR_FLASH As P_NOR = DirectCast(BSDL_FLASH_DEVICE, P_NOR)
            Return NOR_FLASH.Sector_Count
        End Function

        Public Function BoundaryScan_WriteFlash(base_addr As UInt32, data_to_write() As Byte) As Boolean
            Dim NOR_FLASH As P_NOR = DirectCast(BSDL_FLASH_DEVICE, P_NOR)
            Dim DataToWrite As UInt32 = data_to_write.Length
            Dim PacketSize As UInt32 = 8192
            If Me.BSDL_IO = BSDL_DQ_IO.X8_OVER_X16 Then
                base_addr = (base_addr << 1)
                Dim data_bloated((DataToWrite * 2) - 1) As Byte
                For i = 0 To DataToWrite - 1
                    data_bloated(i * 2) = data_to_write(i)
                Next
                data_to_write = data_bloated
                DataToWrite = data_to_write.Length
            End If
            Dim Loops As Integer = CInt(Math.Ceiling(DataToWrite / PacketSize)) 'Calcuates iterations
            For i As Integer = 0 To Loops - 1
                Dim BufferSize As Integer = DataToWrite
                If (BufferSize > PacketSize) Then BufferSize = PacketSize
                Dim data(BufferSize - 1) As Byte
                Array.Copy(data_to_write, (i * PacketSize), data, 0, data.Length)
                Dim setup_data() As Byte = BoundaryScan_GetSetupPacket(base_addr, data.Length, NOR_FLASH.PAGE_SIZE)
                Dim BSDL_PROG_CMD As UInt32 = NOR_FLASH.WriteMode
                Dim result As Boolean = FCUSB.USB_SETUP_BULKOUT(USB.USBREQ.JTAG_BDR_WRFLASH, setup_data, data, BSDL_PROG_CMD)
                If (Not result) Then Return False
                Utilities.Sleep(350) 'We need a long pause here after each program <-- critical that this is 350
                base_addr += data.Length
                Dim Success As Boolean = FCUSB.USB_WaitForComplete()
            Next
            Return True
        End Function

        Public Function BoundaryScan_SectorErase(sector_index As UInt32) As Boolean
            Dim NOR_FLASH As P_NOR = DirectCast(BSDL_FLASH_DEVICE, P_NOR)
            Dim sector_addr As UInt32 = BoundaryScan_SectorFind(sector_index)
            If Me.BSDL_IO = BSDL_DQ_IO.X8_OVER_X16 Then
                sector_addr <<= 1
            End If
            If NOR_FLASH.WriteMode = MFP_PRG.IntelSharp Or NOR_FLASH.WriteMode = MFP_PRG.Buffer1 Then
                BSDL_WriteMemAddress(sector_addr, &H50) 'clear register
                BSDL_WriteMemAddress(sector_addr, &H60) 'Unlock block (just in case)
                BSDL_WriteMemAddress(sector_addr, &HD0) 'Confirm Command
                'EXPIO_WAIT()
                BSDL_WriteMemAddress(sector_addr, &H20)
                BSDL_WriteMemAddress(sector_addr, &HD0)
                'EXPIO_WAIT()
                BSDL_WriteMemAddress(0, &HFF) 'Puts the device back into READ mode
                BSDL_WriteMemAddress(0, &HF0)
            Else
                'Write Unlock Cycles
                BSDL_WriteCmdData(&H5555, &HAA)
                BSDL_WriteCmdData(&H2AAA, &H55)
                'Write Sector Erase Cycles
                BSDL_WriteCmdData(&H5555, &H80)
                BSDL_WriteCmdData(&H5555, &HAA)
                BSDL_WriteCmdData(&H2AAA, &H55)
                BSDL_WriteMemAddress(sector_addr, &H30)
            End If
            Utilities.Sleep(100)
            Dim counter As Integer = 0
            Dim dw As UInt16 = 0
            Dim erased_value As UInt16 = &HFF
            If BSDL_IO = BSDL_DQ_IO.X16 Then erased_value = &HFFFF
            Do Until dw = erased_value
                Utilities.Sleep(20)
                dw = BSDL_ReadWord(sector_addr)
                counter += 1
                If counter = 100 Then Return False
            Loop
            Return True
        End Function

        Public Function BoundaryScan_EraseDevice() As Boolean
            Dim NOR_FLASH As P_NOR = DirectCast(BSDL_FLASH_DEVICE, P_NOR)
            If NOR_FLASH.WriteMode = MFP_PRG.IntelSharp Or NOR_FLASH.WriteMode = MFP_PRG.Buffer1 Then
                BSDL_WriteMemAddress(&H0, &H30)
                BSDL_WriteMemAddress(&H0, &HD0)
            Else
                BSDL_WriteCmdData(&H5555, &HAA)
                BSDL_WriteCmdData(&H2AAA, &H55)
                BSDL_WriteCmdData(&H5555, &H80)
                BSDL_WriteCmdData(&H5555, &HAA)
                BSDL_WriteCmdData(&H2AAA, &H55)
                BSDL_WriteCmdData(&H5555, &H10)
            End If
            Utilities.Sleep(500)
            Dim counter As Integer = 0
            Dim dw As UInt16
            Do Until dw = &HFFFF
                Utilities.Sleep(100)
                dw = BSDL_ReadWord(0)
                counter += 1
                If counter = 100 Then Return False
            Loop
            Return True
        End Function

        Public Function BoundaryScan_GetSectorSize(sector_index As UInt32) As UInt32
            Dim NOR_FLASH As P_NOR = DirectCast(BSDL_FLASH_DEVICE, P_NOR)
            If Me.BSDL_IO = BSDL_DQ_IO.X8_OVER_X16 Then
                Return NOR_FLASH.GetSectorSize(sector_index) / 2
            Else
                Return NOR_FLASH.GetSectorSize(sector_index)
            End If
        End Function

#End Region

#Region "ARM Support"
        Const NO_SYSSPEED = 0
        Const YES_SYSSPEED = 1
        Const ARM_NOOP = &HE1A00000UI

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
            Dim tdi_data() As Byte = Utilities.Bytes.FromUInt32(r0_rev, False)
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
            ShiftTDI(32, Utilities.Bytes.FromUInt32(r0_rev, False), Nothing, True)
            ARM_PushInstruction(NO_SYSSPEED, &HE1A00000UI, 0)
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            ShiftTDI(32, Utilities.Bytes.FromUInt32(r1_rev, False), Nothing, True)
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
            ARM_IR(Devices(0).BSDL.INTEST)
        End Sub

        Public Sub ARM_SelectChain2()
            ARM_IR(Devices(0).BSDL.SCAN_N)
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            ShiftTDI(5, {2}, Nothing, True) 'shift: 0x08 (reversed to 0b00010)
            ARM_IR(Devices(0).BSDL.INTEST)
        End Sub

        Public Sub ARM_PushInstruction(SYSSPEED As Boolean, op_cmd As UInt32, rti_cycles As Byte)
            Dim op_rev As UInt32 = REVERSE32(op_cmd) 'So we shift MSB first
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            Dim tdi64 As UInt64
            If (SYSSPEED) Then
                tdi64 = (CULng(op_rev) << 1) Or 1
            Else
                tdi64 = (CULng(op_rev) << 1)
            End If
            Dim tdi() As Byte = Utilities.Bytes.FromUInt64(tdi64, False)
            ReDim tdi(4) '5 bytes only
            ShiftTDI(33, tdi, Nothing, False)
            TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            If (rti_cycles > 0) Then Tap_Toggle(rti_cycles)
        End Sub

        Public Sub ARM_IR(ir_data As Byte)
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR)
            ShiftTDI(4, {ir_data}, Nothing, True)
        End Sub

        Public Function REVERSE32(u32 As UInt32) As UInt32
            Dim out32 As UInt32 = 0
            For i = 0 To 31
                If ((u32 And 1) = 1) Then out32 = out32 Or 1
                out32 = out32 << 1
                u32 = u32 >> 1
            Next
            Return out32
        End Function

#End Region

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
            If SW_TAP IsNot Nothing Then
                SW_TAP.ShiftIR(tdi_bits, tdo_bits, bit_count, exit_mode)
                Utilities.Sleep(2)
            Else
                Dim packet_param As UInt32 = bit_count
                If exit_mode Then packet_param = packet_param Or (1 << 16)
                ReDim tdo_bits(tdi_bits.Length - 1)
                TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR)
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, tdi_bits, tdi_bits.Length) : Utilities.Sleep(2) 'Preloads the TDI/TMS data
                FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_SHIFT_DATA, tdo_bits, packet_param)
                Array.Reverse(tdo_bits)
            End If
        End Sub

        Public Sub JSP_ShiftDR(tdi_bits() As Byte, ByRef tdo_bits() As Byte, bit_count As UInt16, exit_mode As Boolean) Handles JSP.ShiftDR
            If SW_TAP IsNot Nothing Then
                SW_TAP.ShiftDR(tdi_bits, tdo_bits, bit_count, exit_mode)
                Utilities.Sleep(2)
            Else
                Dim packet_param As UInt32 = bit_count
                If exit_mode Then packet_param = packet_param Or (1 << 16)
                ReDim tdo_bits(tdi_bits.Length - 1)
                Array.Reverse(tdi_bits)
                TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, tdi_bits, tdi_bits.Length) : Utilities.Sleep(2) 'Preloads the TDI/TMS data
                FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_SHIFT_DATA, tdo_bits, packet_param)
                Array.Reverse(tdo_bits)
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
            ReadWriteReg32(Devices(SELECTED_INDEX).BSDL.MIPS_CONTROL, (EJTAG_CTRL.PrRst Or EJTAG_CTRL.PerRst))
        End Sub

        Public Function EJTAG_Debug_Enable() As Boolean
            Try
                Dim debug_flag As UInt32 = EJTAG_CTRL.PrAcc Or EJTAG_CTRL.ProbEn Or EJTAG_CTRL.SetDev Or EJTAG_CTRL.JtagBrk
                Dim ctrl_reg As UInt32 = ReadWriteReg32(Devices(SELECTED_INDEX).BSDL.MIPS_CONTROL, debug_flag)
                If (ReadWriteReg32(Devices(SELECTED_INDEX).BSDL.MIPS_CONTROL, EJTAG_CTRL.PrAcc Or EJTAG_CTRL.ProbEn Or EJTAG_CTRL.SetDev) And EJTAG_CTRL.BrkSt) Then
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
                ReadWriteReg32(Devices(SELECTED_INDEX).BSDL.MIPS_CONTROL, flag)
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

        Public Function CFI_SectorCount() As UInt32
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

        Public Function CFI_SectorWrite(ByVal Address As UInt32, ByVal data_out() As Byte) As Boolean
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
        Public SPI_Part As FlashMemory.SPI_NOR 'Contains the SPI Flash definition
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
                If Not FCUSB.HWBOARD = USB.FCUSB_BOARD.Professional_PCB4 Then
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
        Public Function SPI_SectorCount() As UInt32
            Dim secSize As UInt32 = &H10000 '64KB
            Dim totalsize As UInt32 = SPI_Part.FLASH_SIZE
            Return CUInt(totalsize / secSize)
        End Function

        Public Function SPI_SectorWrite(ByVal sector_index As UInt32, ByVal data() As Byte) As Boolean
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
                Dim J As JTAG_DEVICE = Me.Devices(Me.SELECTED_INDEX)
                If J.BSDL IsNot Nothing Then
                    WriteConsole(String.Format("JTAG Device selected: {0} (IR length {1})", device_index, J.BSDL.IR_LEN.ToString))
                    Dim Leading_Bits As UInt16 = (CUShort(Devices(Me.SELECTED_INDEX).IR_LEADING) << 8) Or CUShort(Devices(Me.SELECTED_INDEX).IR_TRAILING)
                    Dim select_value As UInt32 = (CUInt(Leading_Bits) << 16) Or (Devices(Me.SELECTED_INDEX).ACCESS << 8) Or (Me.Devices(Me.SELECTED_INDEX).BSDL.IR_LEN And 255)
                    FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SELECT, Nothing, select_value)
                Else
                    WriteConsole("Unable to select JTAG device: BSDL not loaded")
                End If
                Utilities.Sleep(10)
            Catch ex As Exception
            End Try
        End Sub

        Public Function GetSelectedDevice() As JTAG_DEVICE
            Return Me.Devices(SELECTED_INDEX)
        End Function

        Public Sub Reset_StateMachine()
            If SW_TAP IsNot Nothing Then
                SW_TAP.Reset()
            Else
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_RESET)
            End If
        End Sub

        Public Sub TAP_GotoState(ByVal J_STATE As JTAG_MACHINE_STATE)
            If SW_TAP IsNot Nothing Then
                SW_TAP.GotoState(J_STATE)
            Else
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_GOTO_STATE, Nothing, J_STATE)
            End If
        End Sub

        Public Sub Tap_Toggle(count As UInt32)
            Try
                Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_TOGGLE, Nothing, count)
                FCUSB.USB_WaitForComplete()
            Catch ex As Exception
            End Try
        End Sub
        'Writes to the instruction-register and then shifts out 32-bits from the DR
        Public Function ReadWriteReg32(ByVal ir_value As UInt32, Optional dr_value As UInt32 = 0) As UInt32
            If SW_TAP IsNot Nothing Then
                Dim byte_count As Integer = Math.Ceiling(Devices(Me.SELECTED_INDEX).BSDL.IR_LEN / 8)
                If (byte_count = 1) Then
                    SW_TAP.ShiftIR({(ir_value And 255)}, Nothing, Devices(Me.SELECTED_INDEX).BSDL.IR_LEN, True)
                ElseIf (byte_count = 2) Then
                    SW_TAP.ShiftIR({((ir_value >> 8) And 255), (ir_value And 255)}, Nothing, Devices(Me.SELECTED_INDEX).BSDL.IR_LEN, True)
                End If
                Dim data_in() As Byte = Utilities.Bytes.FromUInt32(dr_value)
                Dim data_out() As Byte = Nothing
                SW_TAP.ShiftDR(data_in, data_out, 32)
                SW_TAP.GotoState(JTAG_MACHINE_STATE.Select_DR) 'Default parking
                Return Utilities.Bytes.ToUInt32(data_out)
            Else
                Dim ir_data() As Byte = Utilities.Bytes.FromUInt32(ir_value, False)
                Dim dr_data() As Byte = Utilities.Bytes.FromUInt32(dr_value, False)
                Array.Reverse(ir_data)
                Array.Reverse(dr_data)
                Dim setup(15) As Byte '16 bytes (8 byte area for both IR/DR)
                Dim return_data(3) As Byte
                Array.Copy(ir_data, 0, setup, 0, 4)
                Array.Copy(dr_data, 0, setup, 8, 4)
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup) : Utilities.Sleep(2)
                FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_REGISTERS, return_data, 32)
                Array.Reverse(return_data)
                Return Utilities.Bytes.ToUInt32(return_data)
            End If
        End Function
        'Returns DR value (32 bit)
        Public Function ReadRegister32(ir_opcode As UInt32, ir_len As Integer) As UInt32
            Dim tdo(3) As Byte
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR)
            ShiftTDI(ir_len, {ir_opcode}, Nothing, True)
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            ShiftTDI(32, {0, 0, 0, 0}, tdo, True)
            TAP_GotoState(JTAG_MACHINE_STATE.Update_DR)
            Array.Reverse(tdo)
            Return Utilities.Bytes.ToUInt32(tdo)
        End Function

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
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, packet_data, packet_data.Length) : Utilities.Sleep(2)
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SHIFT_TMS, Nothing, bits_size)   'Shifts data and reads result
            Loop
        End Sub

        Public Sub ShiftTDI(BitCount As UInt32, tdi_bits() As Byte, ByRef tdo_bits() As Byte, exit_tms As Boolean)
            Dim byte_count As Integer = Math.Ceiling(BitCount / 8)
            If tdo_bits Is Nothing Then ReDim tdo_bits(byte_count - 1)
            If tdi_bits Is Nothing Then ReDim tdi_bits(byte_count - 1)
            Dim BytesLeft As Integer = byte_count
            Dim BitsLeft As UInt32 = BitCount
            Dim ptr As Integer = 0
            Do Until BytesLeft = 0
                Dim packet_size As UInt32 = Math.Min(64, BytesLeft)
                Dim packet_data(packet_size - 1) As Byte
                Array.Copy(tdi_bits, ptr, packet_data, 0, packet_data.Length)
                BytesLeft -= packet_size
                Dim bits_size As UInt32 = Math.Min(512, BitsLeft)
                BitsLeft -= bits_size
                If BytesLeft = 0 AndAlso exit_tms Then bits_size = bits_size Or (1 << 16)
                Dim tdo_data(packet_size - 1) As Byte
                If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, packet_data, packet_data.Length) Then Exit Sub 'Preloads the TDI/TMS data
                If Not FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_SHIFT_DATA, tdo_data, bits_size) Then Exit Sub  'Shifts data
                Array.Copy(tdo_data, 0, tdo_bits, ptr, tdo_data.Length)
                ptr += packet_data.Length
            Loop
        End Sub

#End Region

#Region "BSDL"
        Private ReadOnly BSDL_DATABASE As New List(Of BSDL_DEF)

        Private Sub BSDL_Init()
            BSDL_DATABASE.Clear()
            ARM_CORTEXM7()
            Microsemi_A3P250_FG144()
            Xilinx_XC2C64A()
            Xilinx_XC9572XL()
            Altera_5M160ZE64()
            Altera_5M570ZT144()
            Altera_EPM7032ST44()
            LCMXO2_4000HC_XFTG256()
            LCMXO2_7000HC_XXTG144()
            LC4032V_TQFP44()
            LC4064V_TQFP44()
            BCM3348()
        End Sub

        Public Function BSDL_GetDefinition(jedec_id As UInt32) As BSDL_DEF
            For Each jtag_def In Me.BSDL_DATABASE
                If jtag_def.JEDEC_ID = jedec_id Then Return jtag_def
            Next
            Return Nothing
        End Function

        Private Sub Microsemi_A3P250_FG144()
            Dim J_DEVICE As New BSDL_DEF
            J_DEVICE.PART_NAME = "A3P250"
            J_DEVICE.JEDEC_ID = &H1BA141CFUI
            J_DEVICE.IR_LEN = 8
            J_DEVICE.BS_LEN = 708
            J_DEVICE.BYPASS = &HFF
            J_DEVICE.IDCODE = &HF
            J_DEVICE.EXTEST = 0
            J_DEVICE.SAMPLE = 1
            J_DEVICE.HIGHZ = &H7
            J_DEVICE.CLAMP = &H5
            J_DEVICE.INTEST = &H6
            J_DEVICE.USERCODE = &HE
            J_DEVICE.CONTROL_DISABLE = False
            BSDL_DATABASE.Add(J_DEVICE)
        End Sub

        Private Sub Xilinx_XC9572XL()
            Dim J_DEVICE As New BSDL_DEF
            J_DEVICE.PART_NAME = "XC9572XL"
            J_DEVICE.JEDEC_ID = &H59604093UI
            J_DEVICE.IR_LEN = 8
            J_DEVICE.BS_LEN = 216
            J_DEVICE.IDCODE = &HFE
            J_DEVICE.BYPASS = &HFF
            J_DEVICE.INTEST = 2
            J_DEVICE.EXTEST = 0
            J_DEVICE.SAMPLE = 1
            J_DEVICE.CLAMP = &HFA
            J_DEVICE.HIGHZ = &HFC
            J_DEVICE.USERCODE = &HFD
            J_DEVICE.CONTROL_DISABLE = False
            '"ISPEX ( 11110000)," &
            '"FBULK ( 11101101),"&
            '"FBLANK ( 11100101),"&
            '"FERASE ( 11101100),"&
            '"FPGM ( 11101010)," &
            '"FPGMI ( 11101011)," &
            '"FVFY ( 11101110)," &
            '"FVFYI ( 11101111)," &
            '"ISPEN ( 11101000)," &
            '"ISPENC ( 11101001)," &
            BSDL_DATABASE.Add(J_DEVICE)
        End Sub

        Private Sub Xilinx_XC2C64A()
            Dim J_DEVICE As New BSDL_DEF
            J_DEVICE.PART_NAME = "XC2C64A"
            J_DEVICE.JEDEC_ID = &H6E58093UI
            J_DEVICE.IR_LEN = 8
            J_DEVICE.BS_LEN = 192
            J_DEVICE.IDCODE = 1
            J_DEVICE.BYPASS = &HFF
            J_DEVICE.INTEST = 2
            J_DEVICE.EXTEST = 0
            J_DEVICE.SAMPLE = 3
            J_DEVICE.HIGHZ = &HFC
            J_DEVICE.USERCODE = &HFD
            J_DEVICE.CONTROL_DISABLE = False
            '"ISC_ENABLE_CLAMP (11101001)," &
            '"ISC_ENABLEOTF  (11100100)," &
            '"ISC_ENABLE     (11101000)," &
            '"ISC_SRAM_READ  (11100111)," &
            '"ISC_SRAM_WRITE (11100110)," &
            '"ISC_ERASE      (11101101)," &
            '"ISC_PROGRAM    (11101010)," &
            '"ISC_READ       (11101110)," &
            '"ISC_INIT       (11110000)," &
            '"ISC_DISABLE    (11000000)," &
            '"TEST_ENABLE    (00010001)," &
            '"BULKPROG       (00010010)," &
            '"ERASE_ALL      (00010100)," &
            '"MVERIFY        (00010011)," &
            '"TEST_DISABLE   (00010101)," &
            '"ISC_NOOP       (11100000)";
            BSDL_DATABASE.Add(J_DEVICE)
        End Sub

        Private Sub BCM3348()
            Dim J_DEVICE As New BSDL_DEF
            J_DEVICE.PART_NAME = "BCM3348"
            J_DEVICE.JEDEC_ID = &H334817FUI
            J_DEVICE.IR_LEN = 5
            J_DEVICE.BS_LEN = 0
            J_DEVICE.IDCODE = &H1 'Selects Device Identiﬁcation (ID) register
            J_DEVICE.BYPASS = &H1F 'Select Bypass register
            J_DEVICE.SAMPLE = 2
            J_DEVICE.MIPS_IMPCODE = &H3 'Selects Implementation register
            J_DEVICE.MIPS_ADDRESS = &H8 'Selects Address register
            J_DEVICE.MIPS_CONTROL = &HA 'Selects EJTAG Control register
            'DATA_IR = &H9 'Selects Data register
            'IR_ALL = &HB 'Selects the Address, Data and EJTAG Control registers
            'EJTAGBOOT = &HC 'Makes the processor take a debug exception after rese
            'NORMALBOOT = &HD 'Makes the processor execute the reset handler after rese
            'FASTDATA = &HE 'Selects the Data and Fastdata registers
            'EJWATCH = &H1C
            BSDL_DATABASE.Add(J_DEVICE)
        End Sub
        '44-pin TQFP
        Private Sub Altera_5M570ZT144()
            Dim J_DEVICE As New BSDL_DEF
            J_DEVICE.PART_NAME = "5M570ZT144"
            J_DEVICE.JEDEC_ID = &H20A60DDUI
            J_DEVICE.IR_LEN = 10
            J_DEVICE.BS_LEN = 480
            J_DEVICE.IDCODE = 6
            J_DEVICE.EXTEST = &HF
            J_DEVICE.SAMPLE = 5
            J_DEVICE.BYPASS = &H3FF
            J_DEVICE.CLAMP = &HA
            J_DEVICE.HIGHZ = &HB
            J_DEVICE.USERCODE = &H7
            J_DEVICE.CONTROL_DISABLE = True
            BSDL_DATABASE.Add(J_DEVICE)
        End Sub
        '64-pin EQFP
        Private Sub Altera_5M160ZE64()
            Dim J_DEVICE As New BSDL_DEF
            J_DEVICE.PART_NAME = "5M160ZE64"
            J_DEVICE.JEDEC_ID = &H20A50DDUI
            J_DEVICE.IR_LEN = 10
            J_DEVICE.BS_LEN = 240
            J_DEVICE.IDCODE = 6
            J_DEVICE.EXTEST = &HF
            J_DEVICE.SAMPLE = 5
            J_DEVICE.BYPASS = &H3FF
            J_DEVICE.CLAMP = &HA
            J_DEVICE.HIGHZ = &HB
            J_DEVICE.USERCODE = 7
            J_DEVICE.CONTROL_DISABLE = True
            BSDL_DATABASE.Add(J_DEVICE)
        End Sub
        '44-pin TQFP
        Private Sub Altera_EPM7032ST44()
            Dim J_DEVICE As New BSDL_DEF
            J_DEVICE.PART_NAME = "EPM7032"
            J_DEVICE.JEDEC_ID = &H70320DDUI
            J_DEVICE.IR_LEN = 10
            J_DEVICE.BS_LEN = 1
            J_DEVICE.IDCODE = &H59
            J_DEVICE.EXTEST = &H3
            J_DEVICE.SAMPLE = &H57
            J_DEVICE.BYPASS = &H3FF
            BSDL_DATABASE.Add(J_DEVICE)
        End Sub

        Private Sub ARM_CORTEXM7()
            Dim J_DEVICE As New BSDL_DEF
            J_DEVICE.PART_NAME = "ARM-CORTEX-M7"
            J_DEVICE.JEDEC_ID = &HBA02477UI
            J_DEVICE.IR_LEN = 4
            J_DEVICE.BS_LEN = 0
            J_DEVICE.IDCODE = &HE '0b1110 - JTAG Device ID Code Register (DR width: 32)
            J_DEVICE.BYPASS = &HF '0b1111 - JTAG Bypass Register (DR width: 1)
            J_DEVICE.RESTART = &H4 '0b0100
            J_DEVICE.SCAN_N = &H2 '0b0010
            J_DEVICE.ARM_ABORT = &H8 'b1000 - JTAG-DP Abort Register (DR width: 35)
            J_DEVICE.ARM_DPACC = &HA 'b1010 - JTAG DP Access Register (DR width: 35)
            J_DEVICE.ARM_APACC = &HB 'b1011 - JTAG AP Access Register (DR width: 35)
            BSDL_DATABASE.Add(J_DEVICE)
        End Sub

        Private Sub LCMXO2_4000HC_XFTG256()
            Dim J_DEVICE As New BSDL_DEF
            J_DEVICE.PART_NAME = "LCMXO2_4000HC"
            J_DEVICE.JEDEC_ID = &H12BC043UI
            J_DEVICE.IR_LEN = 8
            J_DEVICE.BS_LEN = 552
            J_DEVICE.IDCODE = &HE0 '0b11100000
            J_DEVICE.BYPASS = &HFF '0b11111111
            J_DEVICE.CLAMP = &H78 '01111000
            J_DEVICE.PRELOAD = &H1C '00011100
            J_DEVICE.SAMPLE = &H1C '00011100
            J_DEVICE.HIGHZ = &H18 '00011000
            J_DEVICE.EXTEST = &H15 '00010101
            J_DEVICE.USERCODE = 0 '(11000000)
            J_DEVICE.CONTROL_DISABLE = True
            '"          ISC_ENABLE		(11000110)," &
            '"    ISC_PROGRAM_DONE		(01011110)," &
            '" LSC_PROGRAM_SECPLUS		(11001111)," &
            '"ISC_PROGRAM_USERCODE		(11000010)," &
            '"ISC_PROGRAM_SECURITY		(11001110)," &
            '"         ISC_PROGRAM		(01100111)," &
            '"        LSC_ENABLE_X		(01110100)," &
            '"      ISC_DATA_SHIFT		(00001010)," &
            '"       ISC_DISCHARGE		(00010100)," &
            '"      ISC_ERASE_DONE		(00100100)," &
            '"   ISC_ADDRESS_SHIFT		(01000010)," &
            '"            ISC_READ		(10000000)," &
            '"         ISC_DISABLE		(00100110)," &
            '"           ISC_ERASE		(00001110)," &
            '"            ISC_NOOP		(00110000)," &
            BSDL_DATABASE.Add(J_DEVICE)
        End Sub
        'TQFP-144
        Private Sub LCMXO2_7000HC_XXTG144()
            Dim J_DEVICE As New BSDL_DEF
            J_DEVICE.PART_NAME = "LCMXO2-7000HC"
            J_DEVICE.JEDEC_ID = &H12BD043UI
            J_DEVICE.IR_LEN = 8
            J_DEVICE.BS_LEN = 664
            J_DEVICE.IDCODE = &HE0 '0b11100000
            J_DEVICE.BYPASS = &HFF '0b11111111
            J_DEVICE.CLAMP = &H78 '01111000
            J_DEVICE.PRELOAD = &H1C '00011100
            J_DEVICE.SAMPLE = &H1C '00011100
            J_DEVICE.HIGHZ = &H18 '00011000
            J_DEVICE.EXTEST = &H15 '00010101
            J_DEVICE.USERCODE = 0 '(11000000)
            J_DEVICE.CONTROL_DISABLE = True
            '"          ISC_ENABLE		(11000110)," &
            '"    ISC_PROGRAM_DONE		(01011110)," &
            '" LSC_PROGRAM_SECPLUS		(11001111)," &
            '"ISC_PROGRAM_USERCODE		(11000010)," &
            '"ISC_PROGRAM_SECURITY		(11001110)," &
            '"         ISC_PROGRAM		(01100111)," &
            '"        LSC_ENABLE_X		(01110100)," &
            '"      ISC_DATA_SHIFT		(00001010)," &
            '"       ISC_DISCHARGE		(00010100)," &
            '"      ISC_ERASE_DONE		(00100100)," &
            '"   ISC_ADDRESS_SHIFT		(01000010)," &
            '"            ISC_READ		(10000000)," &
            '"         ISC_DISABLE		(00100110)," &
            '"           ISC_ERASE		(00001110)," &
            '"            ISC_NOOP		(00110000)," &
            BSDL_DATABASE.Add(J_DEVICE)
        End Sub

        Private Sub LC4032V_TQFP44()
            Dim J_DEVICE As New BSDL_DEF
            J_DEVICE.PART_NAME = "LC4032V"
            J_DEVICE.JEDEC_ID = &H1805043UI
            J_DEVICE.IR_LEN = 8
            J_DEVICE.BS_LEN = 68
            J_DEVICE.IDCODE = &H16 '00010110
            J_DEVICE.BYPASS = &HFF '11111111
            J_DEVICE.CLAMP = &H20 '00100000
            J_DEVICE.PRELOAD = &H1C '00011100
            J_DEVICE.SAMPLE = &H1C '00011100
            J_DEVICE.HIGHZ = &H18 '00011000
            J_DEVICE.EXTEST = 0 '00000000
            J_DEVICE.USERCODE = &H17 '00010111
            J_DEVICE.CONTROL_DISABLE = False

            '-- ISC instructions
            '      "ISC_ENABLE                        (00010101),"&
            '      "ISC_DISABLE                       (00011110),"&
            '      "ISC_NOOP                          (00110000),"&
            '      "ISC_ADDRESS_SHIFT                 (00000001),"&
            '      "ISC_DATA_SHIFT                    (00000010),"&
            '      "ISC_ERASE                         (00000011),"&
            '      "ISC_DISCHARGE                     (00010100),"&
            '      "ISC_PROGRAM_INCR                  (00100111),"&
            '      "ISC_READ_INCR                     (00101010),"&
            '      "ISC_PROGRAM_SECURITY              (00001001),"&
            '      "ISC_PROGRAM_DONE                  (00101111),"&
            '      "ISC_ERASE_DONE                    (00100100),"&
            '      "ISC_PROGRAM_USERCODE              (00011010),"&
            '      "LSC_ADDRESS_INIT                  (00100001)";
            BSDL_DATABASE.Add(J_DEVICE)
        End Sub

        Private Sub LC4064V_TQFP44()
            Dim J_DEVICE As New BSDL_DEF
            J_DEVICE.PART_NAME = "LC4064V"
            J_DEVICE.JEDEC_ID = &H1809043UI
            J_DEVICE.IR_LEN = 8
            J_DEVICE.BS_LEN = 68
            J_DEVICE.IDCODE = &H16 '00010110
            J_DEVICE.BYPASS = &HFF '11111111
            J_DEVICE.CLAMP = &H20 '00100000
            J_DEVICE.PRELOAD = &H1C '00011100
            J_DEVICE.SAMPLE = &H1C '00011100
            J_DEVICE.HIGHZ = &H18 '00011000
            J_DEVICE.EXTEST = 0 '00000000
            J_DEVICE.USERCODE = &H17 '00010111
            J_DEVICE.CONTROL_DISABLE = False
            '-- ISC instructions
            '      "ISC_ENABLE                        (00010101),"&
            '      "ISC_DISABLE                       (00011110),"&
            '      "ISC_NOOP                          (00110000),"&
            '      "ISC_ADDRESS_SHIFT                 (00000001),"&
            '      "ISC_DATA_SHIFT                    (00000010),"&
            '      "ISC_ERASE                         (00000011),"&
            '      "ISC_DISCHARGE                     (00010100),"&
            '      "ISC_PROGRAM_INCR                  (00100111),"&
            '      "ISC_READ_INCR                     (00101010),"&
            '      "ISC_PROGRAM_SECURITY              (00001001),"&
            '      "ISC_PROGRAM_DONE                  (00101111),"&
            '      "ISC_ERASE_DONE                    (00100100),"&
            '      "ISC_PROGRAM_USERCODE              (00011010),"&
            '      "LSC_ADDRESS_INIT                  (00100001)";
            BSDL_DATABASE.Add(J_DEVICE)
        End Sub

        Public Function GetJedecDescription(JEDECID As UInt32) As String
            Dim PARTNU = CUShort((JEDECID And &HFFFF000) >> 12)
            Dim MANUID = CUShort((JEDECID And &HFFE) >> 1)
            Dim mfg_name As String = "0x" & Hex(MANUID)
            Dim part_name As String = "0x" & Hex(PARTNU).PadLeft(4, "0")
            Select Case MANUID
                Case 1
                    mfg_name = "Spansion"
                Case 4
                    mfg_name = "Fujitsu"
                Case 7
                    mfg_name = "Hitachi"
                Case 9
                    mfg_name = "Intel"
                Case 21
                    mfg_name = "Philips"
                Case 31
                    mfg_name = "Atmel"
                Case 32
                    mfg_name = "ST"
                Case 33
                    mfg_name = "Lattice"
                Case 52
                    mfg_name = "Cypress"
                Case 53
                    mfg_name = "DEC"
                Case 73
                    mfg_name = "Xilinx"
                Case 110
                    mfg_name = "Altera"
                Case 112 '0x70
                    mfg_name = "Qualcomm"
                Case 191 '0xBF
                    mfg_name = "Broadcom"
                Case 194
                    mfg_name = "MXIC"
                Case 231
                    mfg_name = "Microsemi"
                Case 239
                    mfg_name = "Winbond"
                Case 336
                    mfg_name = "Signetics"
                Case 571 '0x23B
                    mfg_name = "ARM" 'ARM Ltd.
            End Select
            For Each jtag_def In Me.BSDL_DATABASE
                If jtag_def.JEDEC_ID = JEDECID Then
                    part_name = jtag_def.PART_NAME
                    Exit For
                End If
            Next
            Return mfg_name & " " & part_name
        End Function

#End Region

        Private Sub TAP_EnableSoftwareStateMachine()
            SW_TAP = New JTAG_STATE_CONTROLLER
            AddHandler SW_TAP.Shift_TDI, AddressOf ShiftTDI
            AddHandler SW_TAP.Shift_TMS, AddressOf ShiftTMS
            SW_TAP.Reset()
        End Sub

    End Class

    Public Enum JTAG_SPEED As UInt32
        _10MHZ = 1 'Divider=4: 40MHZ/4=10MHz
        _20MHZ = 2 'Divider=2: 40MHZ/2=20MHz
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
        Public Property IR_LEADING As Integer
        Public Property IR_TRAILING As Integer
        Public Property ACCESS As JTAG_MEM_ACCESS
        Public Property BSDL As BSDL_DEF

    End Class

    Public Class BSDL_DEF
        Property JEDEC_ID As UInt32
        Property PART_NAME As String
        Property IR_LEN As Integer 'Number of bits the IR uses
        Property BS_LEN As Integer 'Number of bits for PRELOAD/EXTEST
        Property IDCODE As UInt32
        Property BYPASS As UInt32
        Property INTEST As UInt32
        Property EXTEST As UInt32
        Property SAMPLE As UInt32
        Property CLAMP As UInt32
        Property HIGHZ As UInt32
        Property PRELOAD As UInt32
        Property USERCODE As UInt32
        Property CONTROL_DISABLE As Boolean 'The value that disables the control cell
        'ARM SPECIFIC REGISTERS
        Property ARM_ABORT As UInt32
        Property ARM_DPACC As UInt32
        Property ARM_APACC As UInt32
        Property SCAN_N As UInt32
        Property RESTART As UInt32
        'MIPS SPECIFIC
        Property MIPS_IMPCODE As UInt32
        Property MIPS_ADDRESS As UInt32
        Property MIPS_CONTROL As UInt32

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

