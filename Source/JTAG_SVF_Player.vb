'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2021 - ALL RIGHTS RESERVED
'CONTACT EMAIL: support@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: This class implements the SVF / XSVF file format developed by Texas Instroments
'12/29/18: Added Lattice LOOP/LOOPEND commands

Namespace JTAG

    Public Class SVF_Player
        Public Property ExitStateMachine As Boolean = True
        Public Property IgnoreErrors As Boolean = False 'If set to true, this player will not stop executing on a readback error

        Public Event Progress(percent As Integer)
        Public Event SetFrequency(Hz As UInt32)
        Public Event SetTRST(Enabled As Boolean)
        Public Event Writeconsole(msg As String)

        Public Event ResetTap()
        Public Event GotoState(dst_state As JTAG_MACHINE_STATE)
        Public Event ShiftIR(tdi_bits() As Byte, ByRef tdo_bits() As Byte, bit_count As Int16, exit_tms As Boolean)
        Public Event ShiftDR(tdi_bits() As Byte, ByRef tdo_bits() As Byte, bit_count As UInt16, exit_tms As Boolean)
        Public Event ToggleClock(clk_tck As UInt32)

        Sub New()

        End Sub

        Public Property ENDIR As JTAG_MACHINE_STATE
        Public Property ENDDR As JTAG_MACHINE_STATE
        Public Property IR_TAIL As svf_param
        Public Property IR_HEAD As svf_param
        Public Property DR_TAIL As svf_param
        Public Property DR_HEAD As svf_param
        Public Property SIR_LAST_LEN As Integer 'Number of bits the last mask length was
        Public Property SDR_LAST_LEN As Integer 'Number of bits the last mask length was

        Public SIR_LAST_MASK() As Byte
        Public SDR_LAST_MASK() As Byte

        Private Sub Setup()
            ENDIR = JTAG_MACHINE_STATE.RunTestIdle
            ENDDR = JTAG_MACHINE_STATE.RunTestIdle
            IR_TAIL = New svf_param
            IR_HEAD = New svf_param
            DR_TAIL = New svf_param
            DR_HEAD = New svf_param
            SIR_LAST_MASK = Nothing
            SIR_LAST_LEN = -1
            SDR_LAST_MASK = Nothing
            SDR_LAST_LEN = -1
        End Sub

        Public Function RunFile_XSVF(user_file() As Byte) As Boolean
            Setup()
            Dim xsvf_file() As xsvf_param = ConvertDataToProperFormat(user_file)
            Dim TDO_MASK As svf_data = Nothing
            Dim TDO_REPEAT As UInt32 = 16 'Number of times to retry
            Dim XRUNTEST As UInt32 = 0
            Dim XSDRSIZE As UInt32 = 0
            Dim line_counter As Integer = 0
            RaiseEvent GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            For Each line In xsvf_file
                line_counter += 1
                RaiseEvent Progress((line_counter \ xsvf_file.Length) * 100)
                Select Case line.instruction
                    Case xsvf_instruction.XTDOMASK
                        TDO_MASK = line.value_data
                    Case xsvf_instruction.XREPEAT
                        TDO_REPEAT = CUInt(line.value_uint)
                    Case xsvf_instruction.XRUNTEST
                        XRUNTEST = CUInt(line.value_uint)
                    Case xsvf_instruction.XSIR
                        RaiseEvent ShiftIR(line.value_data.data, Nothing, CShort(line.value_data.bits), True)
                        If Not XRUNTEST = 0 Then
                            RaiseEvent GotoState(JTAG_MACHINE_STATE.RunTestIdle)
                            DoXRunTest(XRUNTEST) 'wait for the last specified XRUNTEST time. 
                        Else
                            RaiseEvent GotoState(ENDIR)  'Otherwise, go to the last specified XENDIR state.
                        End If
                    Case xsvf_instruction.XSDR
                        Dim Counter As UInt32 = TDO_REPEAT
                        Dim Result As Boolean = False
                        Do
                            Dim TDO() As Byte = Nothing
                            RaiseEvent ShiftDR(line.value_data.data, TDO, line.value_data.bits, True)
                            Result = CompareResult(TDO, line.value_expected.data, TDO_MASK.data) 'compare TDO with line.value_expected (use TDOMask from last XTDOMASK)
                            If (Not Result) Then
                                PrintCompareError(line_counter, "XSDR", TDO, TDO_MASK.data, line.value_expected.data)
                                If Counter = 0 Then If IgnoreErrors Then Exit Do Else Return False
                            End If
                            If Counter > 0 Then Counter -= 1UI
                        Loop Until Result
                        If Not XRUNTEST = 0 Then
                            RaiseEvent GotoState(JTAG_MACHINE_STATE.RunTestIdle)
                            DoXRunTest(XRUNTEST) 'wait for the last specified XRUNTEST time. 
                        Else
                            RaiseEvent GotoState(ENDDR)  'Otherwise, go to the last specified XENDDR state.
                        End If
                    Case xsvf_instruction.XSDRSIZE
                        XSDRSIZE = CUInt(line.value_uint)    'Specifies the length of all XSDR/XSDRTDO records that follow.
                    Case xsvf_instruction.XSDRTDO
                        Dim Counter As UInt32 = TDO_REPEAT
                        Dim Result As Boolean = False
                        Do
                            If Counter > 0 Then Counter -= 1UI
                            Dim TDO() As Byte = Nothing
                            RaiseEvent ShiftDR(line.value_data.data, TDO, line.value_data.bits, True)
                            Result = CompareResult(TDO, line.value_expected.data, TDO_MASK.data) 'compare TDO with line.value_expected (use TDOMask from last XTDOMASK)
                            If Not Result Then
                                PrintCompareError(line_counter, "XSDRTDO", TDO, TDO_MASK.data, line.value_expected.data)
                                If Counter = 0 Then If IgnoreErrors Then Exit Do Else Return False
                            End If
                        Loop Until Result
                        If Not XRUNTEST = 0 Then
                            RaiseEvent GotoState(JTAG_MACHINE_STATE.RunTestIdle)
                            DoXRunTest(XRUNTEST) 'wait for the last specified XRUNTEST time. 
                        Else
                            RaiseEvent GotoState(ENDDR)  'Otherwise, go to the last specified XENDDR state.
                        End If
                    Case xsvf_instruction.XSDRB
                        RaiseEvent ShiftDR(line.value_data.data, Nothing, line.value_data.bits, False)'Stay in DR
                    Case xsvf_instruction.XSDRC
                        RaiseEvent ShiftDR(line.value_data.data, Nothing, line.value_data.bits, False) 'Stay in DR
                    Case xsvf_instruction.XSDRE
                        RaiseEvent ShiftDR(line.value_data.data, Nothing, line.value_data.bits, False)
                        RaiseEvent GotoState(ENDDR)
                    Case xsvf_instruction.XSDRTDOB
                        Dim counter As UInt32 = TDO_REPEAT
                        Dim Result As Boolean = False
                        Do
                            If counter > 0 Then counter -= 1UI
                            Dim TDO() As Byte = Nothing
                            RaiseEvent ShiftDR(line.value_data.data, TDO, line.value_data.bits, False)
                            Result = CompareResult(TDO, line.value_expected.data, TDO_MASK.data) 'compare TDO with line.value_expected (use TDOMask from last XTDOMASK)
                            If Not Result Then
                                PrintCompareError(line_counter, "XSDRTDOB", TDO, TDO_MASK.data, line.value_expected.data)
                                If Counter = 0 Then If IgnoreErrors Then Exit Do Else Return False
                            End If
                            If counter > 0 Then counter -= 1UI
                        Loop Until Result
                    Case xsvf_instruction.XSDRTDOC
                        Dim Counter As UInt32 = TDO_REPEAT
                        Dim Result As Boolean = False
                        Do
                            Dim TDO() As Byte = Nothing
                            RaiseEvent ShiftDR(line.value_data.data, TDO, line.value_data.bits, False)
                            Result = CompareResult(TDO, line.value_expected.data, TDO_MASK.data) 'compare TDO with line.value_expected (use TDOMask from last XTDOMASK)
                            If IgnoreErrors Then Exit Do
                            If Not Result Then
                                PrintCompareError(line_counter, "XSDRTDOC", TDO, TDO_MASK.data, line.value_expected.data)
                                If Counter = 0 Then If IgnoreErrors Then Exit Do Else Return False
                            End If
                        Loop Until Result
                    Case xsvf_instruction.XSDRTDOE
                        Dim counter As UInt32 = TDO_REPEAT
                        Dim Result As Boolean = False
                        Do
                            If counter > 0 Then counter -= 1UI
                            Dim TDO() As Byte = Nothing
                            RaiseEvent ShiftDR(line.value_data.data, TDO, line.value_data.bits, False)
                            Result = CompareResult(TDO, line.value_expected.data, TDO_MASK.data)
                            If Not Result Then
                                PrintCompareError(line_counter, "XSDRTDOE", TDO, TDO_MASK.data, line.value_expected.data)
                                If Counter = 0 Then If IgnoreErrors Then Exit Do Else Return False
                            End If
                        Loop Until Result
                        RaiseEvent GotoState(ENDDR)
                    Case xsvf_instruction.XSETSDRMASKS 'Obsolete
                    Case xsvf_instruction.XSDRINC 'Obsolete
                    Case xsvf_instruction.XCOMPLETE
                        Exit For
                    Case xsvf_instruction.XSTATE
                        If line.state = JTAG_MACHINE_STATE.TestLogicReset Then
                            RaiseEvent ResetTap()
                        Else
                            RaiseEvent GotoState(line.state)
                        End If
                    Case xsvf_instruction.XENDIR
                        ENDIR = line.state
                    Case xsvf_instruction.XENDDR
                        ENDDR = line.state
                    Case xsvf_instruction.XSIR2 'Same as XSIR (since we should all support 255 bit lengths or more)
                        RaiseEvent ShiftIR(line.value_data.data, Nothing, CShort(line.value_data.bits), True)
                        If Not XRUNTEST = 0 Then
                            RaiseEvent GotoState(JTAG_MACHINE_STATE.RunTestIdle)
                            DoXRunTest(CUInt(line.value_uint)) 'wait for the last specified XRUNTEST time. 
                        Else
                            RaiseEvent GotoState(ENDIR)  'Otherwise, go to the last specified XENDIR state.
                        End If
                    Case xsvf_instruction.XCOMMENT 'No need to display this
                    Case xsvf_instruction.XWAIT
                        RaiseEvent GotoState(line.state)
                        Dim Sleep As Integer = CInt(line.value_uint) \ 1000
                        Threading.Thread.Sleep(Sleep)
                        RaiseEvent GotoState(line.state_end)
                    Case Else
                End Select
            Next
            If ExitStateMachine Then RaiseEvent GotoState(JTAG_MACHINE_STATE.TestLogicReset)
            Return True
        End Function

        Public Function RunFile_SVF(user_file() As String) As Boolean
            Setup()
            RaiseEvent ResetTap()
            Dim svf_file() As String = Nothing
            Dim svf_index() As Integer = Nothing
            ConvertFileToProperFormat(user_file, svf_file, svf_index)
            RaiseEvent GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            Dim LOOP_COUNTER As Integer = 0
            Dim LOOP_CACHE As New List(Of String)
            Try
                For x = 0 To svf_file.Count - 1
                    Dim line As String = svf_file(x)
                    If x Mod 10 = 0 Then
                        Dim percent As Integer = CInt((CSng(x + 1) / CSng(svf_file.Length)) * 100)
                        RaiseEvent Progress(percent)
                    End If
                    If LOOP_COUNTER = 0 Then
                        If line.ToUpper.StartsWith("LOOP ") Then 'Lattice's Extended SVF command
                            Dim loop_count_str As String = line.Substring(5).Trim
                            LOOP_COUNTER = CInt(loop_count_str)
                            LOOP_CACHE.Clear()
                        Else
                            If Not RunFile_Execute(line, svf_index(x), False) Then Return False
                        End If
                    ElseIf line.ToUpper.StartsWith("ENDLOOP") Then
                        Dim end_loop_extra As String = line.Substring(7).Trim
                        For i = 1 To LOOP_COUNTER
                            Dim Loop_commands() As String = LOOP_CACHE.ToArray
                            Dim result As Boolean = True
                            For sub_i = 0 To Loop_commands.Length - 1
                                result = result And RunFile_Execute(Loop_commands(sub_i), svf_index(x) - Loop_commands.Length + sub_i, True)
                            Next
                            If result Then Exit For
                        Next
                        LOOP_COUNTER = 0
                    Else 'We are collecting for LOOP
                        LOOP_CACHE.Add(line)
                    End If
                Next
                If ExitStateMachine Then RaiseEvent GotoState(JTAG_MACHINE_STATE.TestLogicReset)
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Function RunFile_Execute(line As String, line_index As Integer, lattice_loop As Boolean) As Boolean
            If line.ToUpper.StartsWith("SIR ") Then
                Dim line_svf As New svf_param(line)
                Dim TDO() As Byte = Nothing
                If (IR_HEAD.LEN > 0) Then
                    RaiseEvent ShiftIR(IR_HEAD.TDI, Nothing, CShort(IR_HEAD.LEN), False)
                End If
                If (IR_TAIL.LEN > 0) Then
                    RaiseEvent ShiftIR(line_svf.TDI, TDO, CShort(line_svf.LEN), False)
                    RaiseEvent ShiftIR(IR_TAIL.TDI, Nothing, CShort(IR_TAIL.LEN), True)
                Else
                    RaiseEvent ShiftIR(line_svf.TDI, TDO, CShort(line_svf.LEN), True)
                End If
                Dim MASK_TO_COMPARE() As Byte = line_svf.MASK
                If MASK_TO_COMPARE Is Nothing Then
                    If line_svf.LEN = SIR_LAST_LEN AndAlso SIR_LAST_MASK IsNot Nothing Then
                        MASK_TO_COMPARE = SIR_LAST_MASK
                    Else
                        SIR_LAST_LEN = -1
                    End If
                Else
                    SIR_LAST_MASK = line_svf.MASK
                    SIR_LAST_LEN = line_svf.LEN
                End If
                Dim Result As Boolean = CompareResult(TDO, line_svf.TDO, MASK_TO_COMPARE)
                If (Not Result) AndAlso (Not lattice_loop) Then
                    PrintCompareError(line_index, "SIR", TDO, line_svf.MASK, line_svf.TDO)
                End If
                RaiseEvent GotoState(ENDIR)
                If (Not Result) AndAlso (Not IgnoreErrors) Then Return False
            ElseIf line.ToUpper.StartsWith("SDR ") Then
                Dim line_svf As New svf_param(line)
                Dim TDO() As Byte = Nothing
                If (DR_HEAD.LEN > 0) Then
                    RaiseEvent ShiftDR(DR_HEAD.TDI, Nothing, DR_HEAD.LEN, False)
                End If
                If (DR_TAIL.LEN > 0) Then
                    RaiseEvent ShiftDR(line_svf.TDI, TDO, line_svf.LEN, False)
                    RaiseEvent ShiftDR(DR_TAIL.TDI, Nothing, DR_TAIL.LEN, True)
                Else
                    RaiseEvent ShiftDR(line_svf.TDI, TDO, line_svf.LEN, True)
                End If
                Dim MASK_TO_COMPARE() As Byte = line_svf.MASK
                If MASK_TO_COMPARE Is Nothing Then
                    If line_svf.LEN = SDR_LAST_LEN AndAlso SDR_LAST_MASK IsNot Nothing Then
                        MASK_TO_COMPARE = SDR_LAST_MASK
                    Else
                        SDR_LAST_LEN = -1
                    End If
                Else
                    SDR_LAST_MASK = line_svf.MASK
                    SDR_LAST_LEN = line_svf.LEN
                End If
                Dim Result As Boolean = CompareResult(TDO, line_svf.TDO, MASK_TO_COMPARE)
                If (Not Result) AndAlso (Not lattice_loop) Then
                    PrintCompareError(line_index, "SDR", TDO, line_svf.MASK, line_svf.TDO)
                End If
                RaiseEvent GotoState(ENDDR)
                If (Not Result) AndAlso (Not IgnoreErrors) Then Return False
            ElseIf line.ToUpper.StartsWith("ENDIR ") Then
                Dim stable_state As String = line.Substring(6).Trim
                If ValidStableState(stable_state) Then
                    ENDIR = GetStateFromInput(stable_state)
                End If
            ElseIf line.ToUpper.StartsWith("ENDDR ") Then
                Dim stable_state As String = line.Substring(6).Trim
                If ValidStableState(stable_state) Then
                    ENDDR = GetStateFromInput(stable_state)
                End If
            ElseIf line.ToUpper.StartsWith("TRST ") Then 'Disable Test Reset line
                Dim s As String = line.Substring(5).Trim.ToUpper
                Dim EnableTrst As Boolean = False
                If s.Equals("ON") OrElse s.Equals("YES") OrElse s.Equals("TRUE") Then EnableTrst = True
                RaiseEvent SetTRST(EnableTrst)
            ElseIf line.ToUpper.StartsWith("FREQUENCY ") Then 'Sets the max freq of the device
                Try
                    Dim s As String = line.Substring(10).Trim
                    If s.ToUpper.EndsWith("HZ") Then
                        s = s.Substring(0, s.Length - 2).Trim()
                    End If
                    Dim FREQ32 As UInt32 = CUInt(Decimal.Parse(s, Globalization.NumberStyles.Float))
                    RaiseEvent SetFrequency(FREQ32)
                Catch ex As Exception
                    RaiseEvent SetFrequency(1000000)
                End Try
            ElseIf line.ToUpper.StartsWith("RUNTEST ") Then
                DoRuntest(line.Substring(8).Trim)
            ElseIf line.ToUpper.StartsWith("STATE ") Then
                Dim stable_state As String = line.Substring(6).Trim
                Dim state_list() As String = stable_state.Split(" "c)
                For Each e_state In state_list
                    If ValideTapState(e_state) Then
                        RaiseEvent GotoState(GetStateFromInput(e_state))
                    End If
                Next
            ElseIf line.ToUpper.StartsWith("TIR ") Then
                IR_TAIL.LoadParams(line)
            ElseIf line.ToUpper.StartsWith("HIR ") Then
                IR_HEAD.LoadParams(line)
            ElseIf line.ToUpper.StartsWith("TDR ") Then
                DR_TAIL.LoadParams(line)
            ElseIf line.ToUpper.StartsWith("HDR ") Then
                DR_HEAD.LoadParams(line)
            Else
                RaiseEvent Writeconsole("Unknown SVF command at line " & line_index & " : " & line)
                Return False
            End If
            Return True
        End Function

        Private Function CompareResult(TDO() As Byte, Expected() As Byte, MASK() As Byte) As Boolean
            Try
                If MASK IsNot Nothing AndAlso Expected IsNot Nothing Then
                    If TDO Is Nothing Then Return False
                    For i = 0 To TDO.Length - 1
                        Dim masked_tdo As Byte = TDO(i) And MASK(i)
                        Dim masked_exp As Byte = Expected(i) And MASK(i)
                        If Not (masked_tdo = masked_exp) Then Return False
                    Next
                ElseIf Expected IsNot Nothing Then 'No MASK, use ALL CARE bits
                    If TDO Is Nothing Then Return False
                    For i = 0 To TDO.Length - 1
                        If Not TDO(i) = Expected(i) Then Return False
                    Next
                End If
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Sub PrintCompareError(LineIndex As Integer, command As String, TDO() As Byte, TDO_MASK() As Byte, EXPECTED() As Byte)
            RaiseEvent Writeconsole("Failed sending " & command & " command (line index: " & LineIndex.ToString() & ")")
            RaiseEvent Writeconsole("TDO: 0x" & Utilities.Bytes.ToHexString(TDO))
            RaiseEvent Writeconsole("Expected: 0x" & Utilities.Bytes.ToHexString(EXPECTED))
            If TDO_MASK IsNot Nothing Then
                RaiseEvent Writeconsole("Mask: 0x" & Utilities.Bytes.ToHexString(TDO_MASK))
            End If
        End Sub

        Private Sub DoXRunTest(wait_amount As UInt32)
            Dim s As Integer = CInt(wait_amount \ 1000)
            If (s < 30) Then s = 30
            Threading.Thread.Sleep(s)
        End Sub

        Private Function ValidStableState(input As String) As Boolean
            Select Case input.Trim.ToUpper
                Case "IRPAUSE"
                Case "DRPAUSE"
                Case "RESET"
                Case "IDLE"
                Case Else
                    Return False
            End Select
            Return True
        End Function

        Private Function ValideTapState(input As String) As Boolean
            Select Case input.Trim.ToUpper
                Case "RESET"
                Case "IDLE"
                Case "DRSELECT"
                Case "DRCAPTURE"
                Case "DRSHIFT"
                Case "DREXIT1"
                Case "DRPAUSE"
                Case "DREXIT2"
                Case "DRUPDATE"
                Case "IRSELECT"
                Case "IRCAPTURE"
                Case "IRSHIFT"
                Case "IREXIT1"
                Case "IRPAUSE"
                Case "IREXIT2"
                Case "IRUPDATE"
                Case Else
                    Return False
            End Select
            Return True
        End Function

        Private Sub DoRuntest(line As String)
            Try
                Dim start_state As JTAG_MACHINE_STATE = JTAG_MACHINE_STATE.RunTestIdle 'Default
                Dim Params() As String = line.Split(" "c)
                If Params Is Nothing Then Exit Sub
                Dim Counter As Integer = 0
                If ValidStableState(Params(Counter)) Then
                    start_state = GetStateFromInput(Params(Counter))
                    Counter += 1
                End If
                RaiseEvent GotoState(start_state)
                If Not IsNumeric(Params(Counter)) Then Exit Sub
                Dim wait_time As Decimal = Decimal.Parse(Params(Counter), Globalization.NumberStyles.Float)
                Select Case Params(Counter + 1).Trim.ToUpper
                    Case "TCK" 'Toggle test-clock
                        RaiseEvent ToggleClock(CUInt(wait_time))
                    Case "SCK" 'Toggle system-clock
                        Threading.Thread.Sleep(CInt(wait_time))
                    Case "SEC"
                        Dim sleep_int As Integer = CInt(wait_time * 1000)
                        If sleep_int < 1 Then sleep_int = 20
                        Threading.Thread.Sleep(sleep_int)
                    Case Else
                        Exit Sub
                End Select
                Counter += 2
                If (Counter = Params.Length) Then Exit Sub 'The rest are optional
                If (Params(Counter + 1).Trim.ToUpper = "SEC") Then
                    Dim min_time As Decimal = Decimal.Parse(Params(Counter), Globalization.NumberStyles.Float)
                    Dim sleep_int As Integer = CInt((min_time * 1000))
                    If sleep_int < 1 Then sleep_int = 20
                    Threading.Thread.Sleep(sleep_int)
                    Counter += 2
                End If
                If (Counter = Params.Length) Then Exit Sub 'The rest are optional
                If (Params(Counter).Trim.ToUpper = "MAXIMUM") Then
                    Dim max_time As Decimal = Decimal.Parse(Params(Counter + 1), Globalization.NumberStyles.Float)
                    Counter += 3 'THIRD ARG MUST BE SEC
                End If
                If (Counter = Params.Length) Then Exit Sub 'The rest are optional
                If (Params(Counter).Trim.ToUpper = "ENDSTATE") AndAlso ValidStableState(Params(Counter + 1)) Then
                    Dim end_state As JTAG_MACHINE_STATE = GetStateFromInput(Params(Counter))
                    RaiseEvent GotoState(end_state)
                End If
            Catch ex As Exception
                Utilities.Sleep(50)
            End Try
        End Sub

        Private Function GetStateFromInput(input As String) As JTAG_MACHINE_STATE
            input = Utilities.RemoveComment(input, "!")
            input = Utilities.RemoveComment(input, "//")
            If input.EndsWith(";") Then input.Substring(0, input.Length - 1).Trim()
            Select Case input.ToUpper
                Case "RESET"
                    Return JTAG_MACHINE_STATE.TestLogicReset
                Case "IDLE"
                    Return JTAG_MACHINE_STATE.RunTestIdle
                Case "DRSELECT"
                    Return JTAG_MACHINE_STATE.Select_DR
                Case "DRCAPTURE"
                    Return JTAG_MACHINE_STATE.Capture_DR
                Case "DRSHIFT"
                    Return JTAG_MACHINE_STATE.Shift_DR
                Case "DREXIT1"
                    Return JTAG_MACHINE_STATE.Exit1_DR
                Case "DRPAUSE"
                    Return JTAG_MACHINE_STATE.Pause_DR
                Case "DREXIT2"
                    Return JTAG_MACHINE_STATE.Exit2_DR
                Case "DRUPDATE"
                    Return JTAG_MACHINE_STATE.Update_DR
                Case "IRSELECT"
                    Return JTAG_MACHINE_STATE.Select_IR
                Case "IRCAPTURE"
                    Return JTAG_MACHINE_STATE.Capture_IR
                Case "IRSHIFT"
                    Return JTAG_MACHINE_STATE.Shift_IR
                Case "IREXIT1"
                    Return JTAG_MACHINE_STATE.Exit1_IR
                Case "IRPAUSE"
                    Return JTAG_MACHINE_STATE.Pause_IR
                Case "IREXIT2"
                    Return JTAG_MACHINE_STATE.Exit2_IR
                Case "IRUPDATE"
                    Return JTAG_MACHINE_STATE.Update_IR
                Case Else
                    Throw New Exception("STATE not valid: " & input)
            End Select
        End Function

        Private Function GetStateFromInput(input As Byte) As JTAG_MACHINE_STATE
            Select Case input
                Case 0
                    Return JTAG_MACHINE_STATE.TestLogicReset
                Case 1
                    Return JTAG_MACHINE_STATE.RunTestIdle
                Case 2
                    Return JTAG_MACHINE_STATE.Select_DR
                Case 3
                    Return JTAG_MACHINE_STATE.Capture_DR
                Case 4
                    Return JTAG_MACHINE_STATE.Shift_DR
                Case 5
                    Return JTAG_MACHINE_STATE.Exit1_DR
                Case 6
                    Return JTAG_MACHINE_STATE.Pause_DR
                Case 7
                    Return JTAG_MACHINE_STATE.Exit2_DR
                Case 8
                    Return JTAG_MACHINE_STATE.Update_DR
                Case 9
                    Return JTAG_MACHINE_STATE.Select_IR
                Case 10
                    Return JTAG_MACHINE_STATE.Capture_IR
                Case 11
                    Return JTAG_MACHINE_STATE.Shift_IR
                Case 12
                    Return JTAG_MACHINE_STATE.Exit1_IR
                Case 13
                    Return JTAG_MACHINE_STATE.Pause_IR
                Case 14
                    Return JTAG_MACHINE_STATE.Exit2_IR
                Case 15
                    Return JTAG_MACHINE_STATE.Update_IR
                Case Else
                    Throw New Exception("XSTATE not valid: 0x" & input.ToString("X"))
            End Select
        End Function

        Private Sub ConvertFileToProperFormat(input() As String, ByRef output() As String, ByRef line_numbers() As Integer)
            Dim FormatedListOut As New List(Of String)
            Dim LineNumberList As New List(Of Integer)
            Dim line_counter As Integer = 1
            For Each line In input
                line = line.Replace(vbTab, " ").Trim
                line = Utilities.RemoveComment(line, "!")
                line = Utilities.RemoveComment(line, "//")
                If Not line.Equals("") Then
                    FormatedListOut.Add(line)
                    LineNumberList.Add(line_counter)
                End If
                line_counter += 1
            Next
            Dim FormatedFileTwo As New List(Of String)
            Dim LineNumberListTwo As New List(Of Integer)
            Dim WorkInProgress As String = ""
            line_counter = -1
            Dim index As Integer = 0
            For Each line In FormatedListOut
                WorkInProgress &= line.ToString.TrimStart
                If line_counter = -1 Then line_counter = LineNumberList(index)
                If WorkInProgress.EndsWith(";") Then
                    WorkInProgress = Mid(WorkInProgress, 1, WorkInProgress.Length - 1).TrimEnd
                    FormatedFileTwo.Add(WorkInProgress)
                    LineNumberListTwo.Add(line_counter)
                    WorkInProgress = ""
                    line_counter = -1
                Else
                    WorkInProgress &= " "
                End If
                index += 1
            Next
            output = FormatedFileTwo.ToArray
            line_numbers = LineNumberListTwo.ToArray
        End Sub

        Private Function ConvertDataToProperFormat(data() As Byte) As xsvf_param()
            Dim pointer As Integer = 0
            Dim x As New List(Of xsvf_param)
            Dim XSDRSIZE As UInt32 = 8 'number of bits
            Do Until pointer = data.Length
                Dim n As New xsvf_param(CType(data(pointer), xsvf_instruction))
                Select Case n.instruction
                    Case xsvf_instruction.XTDOMASK
                        Load_TDI(data, pointer, n, XSDRSIZE)
                    Case xsvf_instruction.XREPEAT
                        n.value_uint = data(pointer + 1)
                        pointer += 2
                    Case xsvf_instruction.XRUNTEST
                        n.value_uint = Load_Uint32_Value(data, pointer)
                    Case xsvf_instruction.XSIR
                        Dim num_bytes As Integer = CInt(Math.Ceiling(data(pointer + 1) / 8))
                        Dim new_Data(num_bytes - 1) As Byte
                        Array.Copy(data, pointer + 2, new_Data, 0, num_bytes)
                        n.value_data = New svf_data With {.data = new_Data, .bits = data(pointer + 1)}
                        pointer += num_bytes + 2
                    Case xsvf_instruction.XSDR
                        Load_TDI(data, pointer, n, XSDRSIZE)
                    Case xsvf_instruction.XSDRSIZE
                        XSDRSIZE = Load_Uint32_Value(data, pointer)
                        n.value_uint = XSDRSIZE
                    Case xsvf_instruction.XSDRTDO
                        Dim num_bytes As Integer = CInt(Math.Ceiling(XSDRSIZE / 8))
                        Dim data1(num_bytes - 1) As Byte
                        Dim data2(num_bytes - 1) As Byte
                        Array.Copy(data, pointer + 1, data1, 0, num_bytes)
                        Array.Copy(data, pointer + 1 + num_bytes, data2, 0, num_bytes)
                        n.value_data = New svf_data With {.data = data1, .bits = CUShort(XSDRSIZE)}
                        n.value_expected = New svf_data With {.data = data2, .bits = CUShort(XSDRSIZE)}
                        pointer += num_bytes + num_bytes + 1
                    Case xsvf_instruction.XSDRB
                        Load_TDI(data, pointer, n, XSDRSIZE)
                    Case xsvf_instruction.XSDRC
                        Load_TDI(data, pointer, n, XSDRSIZE)
                    Case xsvf_instruction.XSDRE
                        Load_TDI(data, pointer, n, XSDRSIZE)
                    Case xsvf_instruction.XSDRTDOB
                        Load_TDI_Expected(data, pointer, n, XSDRSIZE)
                    Case xsvf_instruction.XSDRTDOC
                        Load_TDI_Expected(data, pointer, n, XSDRSIZE)
                    Case xsvf_instruction.XSDRTDOE
                        Load_TDI_Expected(data, pointer, n, XSDRSIZE)
                    Case xsvf_instruction.XSETSDRMASKS 'Obsolete
                    Case xsvf_instruction.XSDRINC 'Obsolete
                    Case xsvf_instruction.XCOMPLETE
                        Return x.ToArray
                    Case xsvf_instruction.XSTATE
                        n.value_uint = data(pointer + 1)
                        n.state = GetStateFromInput(data(pointer + 1))
                        pointer += 2
                    Case xsvf_instruction.XENDIR
                        n.value_uint = data(pointer + 1)
                        Select Case n.value_uint
                            Case 0
                                n.state = JTAG_MACHINE_STATE.RunTestIdle
                            Case 1
                                n.state = JTAG_MACHINE_STATE.Pause_IR
                        End Select
                        pointer += 2
                    Case xsvf_instruction.XENDDR
                        n.value_uint = data(pointer + 1)
                        n.state = GetStateFromInput(data(pointer + 1))

                        pointer += 2
                    Case xsvf_instruction.XSIR2 'Same as XSIR (since we should all support 255 bit lengths or more)
                        Dim num_bytes As Integer = CInt(Math.Ceiling(data(pointer + 1) / 8))
                        Dim new_Data(num_bytes - 1) As Byte
                        Array.Copy(data, pointer + 2, new_Data, 0, num_bytes)
                        n.value_data = New svf_data With {.data = new_Data, .bits = CUShort(XSDRSIZE)}
                        pointer += num_bytes + 2
                    Case xsvf_instruction.XCOMMENT
                        Do
                            pointer += 1
                            If pointer = data.Length Then Exit Select
                        Loop Until data(pointer) = 0
                        pointer += 1
                    Case xsvf_instruction.XWAIT
                        n.state = GetStateFromInput(data(pointer + 1))
                        n.state_end = GetStateFromInput(data(pointer + 2))
                        n.value_uint = CUInt(data(pointer + 3)) << 24
                        n.value_uint += CUInt(data(pointer + 4)) << 16
                        n.value_uint += CUInt(data(pointer + 5)) << 8
                        n.value_uint += CUInt(data(pointer + 6))
                        pointer += 7
                End Select
                x.Add(n)
            Loop
            Return x.ToArray
        End Function

        Private Sub Load_TDI(ByRef data() As Byte, ByRef pointer As Integer, ByRef line As xsvf_param, XSDRSIZE As UInt32)
            Dim num_bytes As Integer = CInt(Math.Ceiling(XSDRSIZE / 8))
            Dim new_Data(num_bytes - 1) As Byte
            Array.Copy(data, pointer + 1, new_Data, 0, num_bytes)
            line.value_data = New svf_data With {.data = new_Data, .bits = CUShort(XSDRSIZE)}
            pointer += num_bytes + 1
        End Sub

        Private Sub Load_TDI_Expected(ByRef data() As Byte, ByRef pointer As Integer, ByRef line As xsvf_param, XSDRSIZE As UInt32)
            Dim num_bytes As Integer = CInt(XSDRSIZE \ 8)
            Dim data1(num_bytes - 1) As Byte
            Dim data2(num_bytes - 1) As Byte
            Array.Copy(data, pointer + 1, data1, 0, num_bytes)
            Array.Copy(data, pointer + 5, data2, 0, num_bytes)
            line.value_data = New svf_data With {.data = data1, .bits = CUShort(XSDRSIZE)}
            line.value_expected = New svf_data With {.data = data2, .bits = CUShort(XSDRSIZE)}
            pointer += 9
        End Sub

        Private Function Load_Uint32_Value(ByRef data() As Byte, ByRef pointer As Integer) As UInt32
            Dim ret As UInt32
            ret = CUInt(data(pointer + 1)) << 24
            ret += CUInt(data(pointer + 2)) << 16
            ret += CUInt(data(pointer + 3)) << 8
            ret += CUInt(data(pointer + 4))
            pointer += 5
            Return ret
        End Function

        Private Function GetBytes_FromUint(input As UInt32, MinBits As Integer) As Byte()
            Dim current(3) As Byte
            current(0) = CByte((input >> 24) And &HFF)
            current(1) = CByte((input >> 16) And &HFF)
            current(2) = CByte((input >> 8) And &HFF)
            current(3) = CByte((input >> 0) And &HFF)
            Dim MaxSize As Integer = CInt(Math.Ceiling(MinBits / 8))
            Dim out(MaxSize - 1) As Byte
            For i = 0 To MaxSize - 1
                out(out.Length - (1 + i)) = current(current.Length - (1 + i))
            Next
            Return out
        End Function

    End Class

    Public Structure svf_data
        Dim data() As Byte
        Dim bits As UInt16
    End Structure

    Public Class svf_param
        Public LEN As UShort = 0
        Public TDI As Byte()
        Public SMASK As Byte()
        Public TDO As Byte()
        Public MASK As Byte()

        Sub New()

        End Sub

        Sub New(input As String)
            LoadParams(input)
        End Sub

        Public Sub LoadParams(input As String)
            input = input.Substring(input.IndexOf(" "c) + 1).Trim 'We remove the first part (TIR, etc)
            If Not input.Contains(" ") AndAlso IsNumeric(input) Then
                LEN = UShort.Parse(input)
            Else
                LEN = UShort.Parse(input.Substring(0, input.IndexOf(" "c)))
                input = input.Substring(input.IndexOf(" "c) + 1).Trim
                Do Until input.Equals("")
                    If input.ToUpper.StartsWith("TDI ") Then
                        input = input.Substring(4).Trim
                        TDI = GetBytes_Param(input)
                    ElseIf input.ToUpper.StartsWith("TDO ") Then
                        input = input.Substring(4).Trim
                        TDO = GetBytes_Param(input)
                    ElseIf input.ToUpper.StartsWith("SMASK ") Then
                        input = input.Substring(6).Trim
                        SMASK = GetBytes_Param(input)
                    ElseIf input.ToUpper.StartsWith("MASK ") Then
                        input = input.Substring(5).Trim
                        MASK = GetBytes_Param(input)
                    End If
                Loop
            End If
        End Sub

        Private Function GetBytes_Param(ByRef line As String) As Byte()
            Dim x1 As Integer = InStr(line, "(")
            Dim x2 As Integer = InStr(line, ")")
            Dim HexStr As String = line.Substring(x1, x2 - 2).Replace(" ", "")
            line = line.Substring(x2).Trim
            Return Utilities.Bytes.FromHexString(HexStr)
        End Function

    End Class

    Public Class xsvf_param
        Public instruction As xsvf_instruction
        Public state As JTAG_MACHINE_STATE
        Public state_end As JTAG_MACHINE_STATE
        Public value_uint As UInt64
        Public value_data As svf_data
        Public value_expected As svf_data

        Sub New(code As xsvf_instruction)
            instruction = code
        End Sub

        Public Overrides Function ToString() As String

            Return instruction.ToString()

        End Function

    End Class

    Public Enum xsvf_instruction
        XCOMPLETE = &H0
        XTDOMASK = &H1
        XSIR = &H2
        XSDR = &H3
        XRUNTEST = &H4
        XREPEAT = &H7
        XSDRSIZE = &H8
        XSDRTDO = &H9
        XSETSDRMASKS = &HA
        XSDRINC = &HB
        XSDRB = &HC
        XSDRC = &HD
        XSDRE = &HE
        XSDRTDOB = &HF
        XSDRTDOC = &H10
        XSDRTDOE = &H11
        XSTATE = &H12
        XENDIR = &H13
        XENDDR = &H14
        XSIR2 = &H15
        XCOMMENT = &H16
        XWAIT = &H17
    End Enum

End Namespace


