'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2015 - ALL RIGHTS RESERVED
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: This class implements the SVF / XSVF file format developed by Texas Instroments
Imports FlashcatUSB.JTAG_STATE_CONTROLLER

Namespace JTAG

    Public Class SVF_Player
        Public IgnoreErrors As Boolean = False 'If set to true, this player will not stop executing on a readback error

        Public Event Progress(ByVal percent As Integer)
        Public Event SetFrequency(ByVal Hz As Integer)
        Public Event SetTRST(ByVal Enabled As Boolean)
        Public Event Printf(ByVal msg As String)

        Public Current_Hertz As Integer = 1000000 'Default of 1 MHz
        Private MY_JTAG As JTAG_IF

        Sub New(ByRef jtag As JTAG_IF)
            MY_JTAG = jtag
        End Sub

        Private Sub Setup()
            RaiseEvent SetFrequency(Current_Hertz)
        End Sub

        Public Function RunFile_XSVF(ByVal user_file() As Byte) As Boolean
            Setup()
            Dim xsvf_file() As xsvf_param = ConvertDataToProperFormat(user_file)
            Dim XENDIR As MachineState = MachineState.RunTestIdle
            Dim XENDDR As MachineState = MachineState.RunTestIdle
            Dim IR_TAIL As New svf_param
            Dim IR_HEAD As New svf_param
            Dim DR_TAIL As New svf_param
            Dim DR_HEAD As New svf_param
            Dim TDO_MASK As svf_data = Nothing
            Dim TDO_REPEAT As UInt32 = 16 'Number of times to retry
            Dim XRUNTEST As UInt32 = 0
            Dim XSDRSIZE As UInt32 = 0
            Dim line_counter As Integer = 0
            MY_JTAG.TAP.GotoState(MachineState.RunTestIdle)
            For Each line In xsvf_file
                line_counter += 1
                RaiseEvent Progress((line_counter / xsvf_file.Length) * 100)
                Select Case line.instruction
                    Case xsvf_instruction.XTDOMASK
                        TDO_MASK = line.value_data
                    Case xsvf_instruction.XREPEAT
                        TDO_REPEAT = line.value_uint
                    Case xsvf_instruction.XRUNTEST
                        XRUNTEST = line.value_uint
                    Case xsvf_instruction.XSIR
                        MY_JTAG.TAP.ShiftIR(line.value_data.data, Nothing, line.value_data.bits)
                        If Not XRUNTEST = 0 Then
                            MY_JTAG.TAP.GotoState(MachineState.RunTestIdle)
                            DoXRunTest(XRUNTEST) 'wait for the last specified XRUNTEST time. 
                        Else
                            MY_JTAG.TAP.GotoState(XENDIR)  'Otherwise, go to the last specified XENDIR state.
                        End If
                    Case xsvf_instruction.XSDR
                        Dim Counter As UInt32 = TDO_REPEAT
                        Dim Result As Boolean = False
                        Do
                            Dim TDO() As Byte = Nothing
                            MY_JTAG.TAP.ShiftDR(line.value_data.data, TDO, line.value_data.bits)
                            Result = CompareResult(TDO, line.value_expected.data, TDO_MASK.data) 'compare TDO with line.value_expected (use TDOMask from last XTDOMASK)
                            If (Not Result) Then
                                RaiseEvent Printf("Failed sending XSDR command (command number: " & line_counter & ")")
                                RaiseEvent Printf("TDO: 0x" & Utilities.Bytes.ToHexString(TDO) & " Expected: 0x" &
                                                  Utilities.Bytes.ToHexString(line.value_expected.data) & " Mask: 0x" & Utilities.Bytes.ToHexString(TDO_MASK.data))
                                If Counter = 0 Then If IgnoreErrors Then Exit Do Else Return False
                            End If
                            If Counter > 0 Then Counter -= 1
                        Loop Until Result
                        If Not XRUNTEST = 0 Then
                            MY_JTAG.TAP.GotoState(MachineState.RunTestIdle)
                            DoXRunTest(XRUNTEST) 'wait for the last specified XRUNTEST time. 
                        Else
                            MY_JTAG.TAP.GotoState(XENDDR)  'Otherwise, go to the last specified XENDDR state.
                        End If
                    Case xsvf_instruction.XSDRSIZE
                        XSDRSIZE = line.value_uint    'Specifies the length of all XSDR/XSDRTDO records that follow.
                    Case xsvf_instruction.XSDRTDO
                        Dim Counter As UInt32 = TDO_REPEAT
                        Dim Result As Boolean = False
                        Do
                            If Counter > 0 Then Counter -= 1
                            Dim TDO() As Byte = Nothing
                            MY_JTAG.TAP.ShiftDR(line.value_data.data, TDO, line.value_data.bits)
                            Result = CompareResult(TDO, line.value_expected.data, TDO_MASK.data) 'compare TDO with line.value_expected (use TDOMask from last XTDOMASK)
                            If Not Result Then
                                RaiseEvent Printf("Failed sending XSDRTDO command (command number: " & line_counter & ")")
                                RaiseEvent Printf("TDO: 0x" & Utilities.Bytes.ToHexString(TDO) & " Expected: 0x" &
                                                  Utilities.Bytes.ToHexString(line.value_expected.data) & " Mask: 0x" & Utilities.Bytes.ToHexString(TDO_MASK.data))
                                If Counter = 0 Then If IgnoreErrors Then Exit Do Else Return False
                            End If
                        Loop Until Result
                        If Not XRUNTEST = 0 Then
                            MY_JTAG.TAP.GotoState(MachineState.RunTestIdle)
                            DoXRunTest(XRUNTEST) 'wait for the last specified XRUNTEST time. 
                        Else
                            MY_JTAG.TAP.GotoState(XENDDR)  'Otherwise, go to the last specified XENDDR state.
                        End If
                    Case xsvf_instruction.XSDRB
                        MY_JTAG.TAP.ShiftDR(line.value_data.data, Nothing, line.value_data.bits, False)'Stay in DR
                    Case xsvf_instruction.XSDRC
                        MY_JTAG.TAP.ShiftDR(line.value_data.data, Nothing, line.value_data.bits, False) 'Stay in DR
                    Case xsvf_instruction.XSDRE
                        MY_JTAG.TAP.ShiftDR(line.value_data.data, Nothing, line.value_data.bits, False)
                        MY_JTAG.TAP.GotoState(XENDDR)
                    Case xsvf_instruction.XSDRTDOB
                        Dim Counter As UInt32 = TDO_REPEAT
                        Dim Result As Boolean = False
                        Do
                            If Counter > 0 Then Counter -= 1
                            Dim TDO() As Byte = Nothing
                            MY_JTAG.TAP.ShiftDR(line.value_data.data, TDO, line.value_data.bits, False)
                            Result = CompareResult(TDO, line.value_expected.data, TDO_MASK.data) 'compare TDO with line.value_expected (use TDOMask from last XTDOMASK)
                            If Not Result Then
                                RaiseEvent Printf("Failed sending XSDRTDOB command (command number: " & line_counter & ")")
                                RaiseEvent Printf("TDO: 0x" & Utilities.Bytes.ToHexString(TDO) & " Expected: 0x" &
                                                  Utilities.Bytes.ToHexString(line.value_expected.data) & " Mask: 0x" & Utilities.Bytes.ToHexString(TDO_MASK.data))
                                If Counter = 0 Then If IgnoreErrors Then Exit Do Else Return False
                            End If
                            If Counter > 0 Then Counter -= 1
                        Loop Until Result
                    Case xsvf_instruction.XSDRTDOC
                        Dim Counter As UInt32 = TDO_REPEAT
                        Dim Result As Boolean = False
                        Do
                            Dim TDO() As Byte = Nothing
                            MY_JTAG.TAP.ShiftDR(line.value_data.data, TDO, line.value_data.bits, False)
                            Result = CompareResult(TDO, line.value_expected.data, TDO_MASK.data) 'compare TDO with line.value_expected (use TDOMask from last XTDOMASK)
                            If IgnoreErrors Then Exit Do
                            If Not Result Then
                                RaiseEvent Printf("Failed sending XSDRTDOC command (command number: " & line_counter & ")")
                                RaiseEvent Printf("TDO: 0x" & Utilities.Bytes.ToHexString(TDO) & " Expected: 0x" &
                                                  Utilities.Bytes.ToHexString(line.value_expected.data) & " Mask: 0x" & Utilities.Bytes.ToHexString(TDO_MASK.data))
                                If Counter = 0 Then If IgnoreErrors Then Exit Do Else Return False
                            End If
                        Loop Until Result
                    Case xsvf_instruction.XSDRTDOE
                        Dim Counter As UInt32 = TDO_REPEAT
                        Dim Result As Boolean = False
                        Do
                            If Counter > 0 Then Counter -= 1
                            Dim TDO() As Byte = Nothing
                            MY_JTAG.TAP.ShiftDR(line.value_data.data, TDO, line.value_data.bits, False)
                            Result = CompareResult(TDO, line.value_expected.data, TDO_MASK.data)
                            If Not Result Then
                                RaiseEvent Printf("Failed sending XSDRTDOE command (command number: " & line_counter & ")")
                                RaiseEvent Printf("TDO: 0x" & Utilities.Bytes.ToHexString(TDO) & " Expected: 0x" &
                                                  Utilities.Bytes.ToHexString(line.value_expected.data) & " Mask: 0x" & Utilities.Bytes.ToHexString(TDO_MASK.data))
                                If Counter = 0 Then If IgnoreErrors Then Exit Do Else Return False
                            End If
                        Loop Until Result
                        MY_JTAG.TAP.GotoState(XENDDR)
                    Case xsvf_instruction.XSETSDRMASKS 'Obsolete
                    Case xsvf_instruction.XSDRINC 'Obsolete
                    Case xsvf_instruction.XCOMPLETE
                        Exit For
                    Case xsvf_instruction.XSTATE
                        If line.state = MachineState.TestLogicReset Then
                            MY_JTAG.TAP.Reset()
                        Else
                            MY_JTAG.TAP.GotoState(line.state)
                        End If
                    Case xsvf_instruction.XENDIR
                        XENDIR = line.state
                    Case xsvf_instruction.XENDDR
                        XENDDR = line.state
                    Case xsvf_instruction.XSIR2 'Same as XSIR (since we should all support 255 bit lengths or more)
                        MY_JTAG.TAP.ShiftIR(line.value_data.data, Nothing, line.value_data.bits)
                        If Not XRUNTEST = 0 Then
                            MY_JTAG.TAP.GotoState(MachineState.RunTestIdle)
                            DoXRunTest(line.value_uint) 'wait for the last specified XRUNTEST time. 
                        Else
                            MY_JTAG.TAP.GotoState(XENDIR)  'Otherwise, go to the last specified XENDIR state.
                        End If
                    Case xsvf_instruction.XCOMMENT 'No need to display this
                    Case xsvf_instruction.XWAIT
                        MY_JTAG.TAP.GotoState(line.state)
                        Dim Sleep As Double = line.value_uint / 1000
                        Threading.Thread.Sleep(Sleep)
                        MY_JTAG.TAP.GotoState(line.state_end)
                    Case Else
                End Select
            Next
            MY_JTAG.TAP.GotoState(MachineState.TestLogicReset)
            Return True
        End Function

        Public Function RunFile_SVF(ByVal user_file() As String) As Boolean
            Setup()
            Dim svf_file() As String = ConvertFileToProperFormat(user_file)
            Dim ENDIR As MachineState = MachineState.RunTestIdle
            Dim ENDDR As MachineState = MachineState.RunTestIdle
            Dim IR_TAIL As New svf_param
            Dim IR_HEAD As New svf_param
            Dim DR_TAIL As New svf_param
            Dim DR_HEAD As New svf_param
            Dim SIR_LAST_MASK() As Byte = Nothing
            Dim SIR_LAST_LEN As Integer = -1 'Number of bits the last mask length was
            Dim SDR_LAST_MASK() As Byte = Nothing
            Dim SDR_LAST_LEN As Integer = -1 'Number of bits the last mask length was
            Dim RUNTEST As UInt32 = 0
            Dim line_counter As Integer = 0
            MY_JTAG.TAP.GotoState(MachineState.RunTestIdle)
            For Each line In svf_file
                line_counter += 1
                RaiseEvent Progress((line_counter / svf_file.Length) * 100)
                If line.ToUpper.StartsWith("SIR ") Then
                    Dim line_svf As New svf_param(line)
                    Dim TDO() As Byte = Nothing
                    If (IR_HEAD.LEN > 0) Then MY_JTAG.TAP.ShiftIR(IR_HEAD.TDI, Nothing, IR_HEAD.LEN, False)
                    If (IR_TAIL.LEN > 0) Then
                        MY_JTAG.TAP.ShiftIR(line_svf.TDI, TDO, line_svf.LEN, False)
                        MY_JTAG.TAP.ShiftIR(IR_TAIL.TDI, Nothing, IR_TAIL.LEN)
                    Else
                        MY_JTAG.TAP.ShiftIR(line_svf.TDI, TDO, line_svf.LEN)
                    End If
                    Dim MASK_TO_COMPARE() As Byte = line_svf.MASK
                    If MASK_TO_COMPARE Is Nothing Then
                        If line_svf.LEN = SIR_LAST_LEN AndAlso SIR_LAST_MASK IsNot Nothing Then MASK_TO_COMPARE = SIR_LAST_MASK
                    Else
                        SIR_LAST_MASK = line_svf.MASK
                    End If
                    SIR_LAST_LEN = line_svf.LEN
                    Dim Result As Boolean = CompareResult(TDO, line_svf.TDO, MASK_TO_COMPARE)
                    If (Not Result) Then
                        RaiseEvent Printf("Failed sending SIR command (command number: " & line_counter & ")")
                        Dim LineOut As String = "TDO: 0x" & Utilities.Bytes.ToHexString(TDO) & " Expected: 0x" & Utilities.Bytes.ToHexString(line_svf.TDO)
                        If (line_svf.MASK IsNot Nothing) Then
                            LineOut &= " Mask: 0x" & Utilities.Bytes.ToHexString(line_svf.MASK)
                        End If
                        RaiseEvent Printf(LineOut)
                        If Not IgnoreErrors Then Return False
                    End If
                    MY_JTAG.TAP.GotoState(ENDIR)
                ElseIf line.ToUpper.StartsWith("SDR ") Then
                    Dim line_svf As New svf_param(line)
                    Dim TDO() As Byte = Nothing
                    If (DR_HEAD.LEN > 0) Then MY_JTAG.TAP.ShiftDR(DR_HEAD.TDI, Nothing, DR_HEAD.LEN, False)
                    If (DR_TAIL.LEN > 0) Then
                        MY_JTAG.TAP.ShiftDR(line_svf.TDI, TDO, line_svf.LEN, False)
                        MY_JTAG.TAP.ShiftDR(DR_TAIL.TDI, Nothing, DR_TAIL.LEN)
                    Else
                        MY_JTAG.TAP.ShiftDR(line_svf.TDI, TDO, line_svf.LEN)
                    End If
                    Dim MASK_TO_COMPARE() As Byte = line_svf.MASK
                    If MASK_TO_COMPARE Is Nothing Then
                        If line_svf.LEN = SDR_LAST_LEN AndAlso SDR_LAST_MASK IsNot Nothing Then MASK_TO_COMPARE = SDR_LAST_MASK
                    Else
                        SDR_LAST_MASK = line_svf.MASK
                    End If
                    SDR_LAST_LEN = line_svf.LEN
                    Dim Result As Boolean = CompareResult(TDO, line_svf.TDO, MASK_TO_COMPARE)
                    If (Not Result) Then
                        RaiseEvent Printf("Failed sending SDR command (command number: " & line_counter & ")")
                        Dim LineOut As String = "TDO: 0x" & Utilities.Bytes.ToHexString(TDO) & " Expected: 0x" & Utilities.Bytes.ToHexString(line_svf.TDO)
                        If (line_svf.MASK IsNot Nothing) Then
                            LineOut &= " Mask: 0x" & Utilities.Bytes.ToHexString(line_svf.MASK)
                        End If
                        RaiseEvent Printf(LineOut)
                        If Not IgnoreErrors Then Return False
                    End If
                    MY_JTAG.TAP.GotoState(ENDDR)
                ElseIf line.ToUpper.StartsWith("ENDIR ") Then
                    ENDIR = GetStateFromInput(Mid(line, 7).Trim)
                ElseIf line.ToUpper.StartsWith("ENDDR ") Then
                    ENDDR = GetStateFromInput(Mid(line, 7).Trim)
                ElseIf line.ToUpper.StartsWith("TRST ") Then 'Disable Test Reset line
                    Dim s As String = Mid(line, 6).Trim.ToUpper
                    Dim EnableTrst As Boolean = False
                    If s = "ON" OrElse s = "YES" OrElse s = "TRUE" Then EnableTrst = True
                    RaiseEvent SetTRST(EnableTrst)
                ElseIf line.ToUpper.StartsWith("FREQUENCY ") Then 'Sets the max freq of the device
                    Dim s As String = Mid(line, 11).Trim
                    If s.ToUpper.EndsWith("HZ") Then s = Mid(s, 1, s.Length - 2).Trim
                    Current_Hertz = Decimal.Parse(s, Globalization.NumberStyles.Float)
                    RaiseEvent SetFrequency(Current_Hertz)
                ElseIf line.ToUpper.StartsWith("RUNTEST ") Then
                    DoRuntest(Mid(line, 9).Trim)
                ElseIf line.ToUpper.StartsWith("STATE ") Then
                    Dim Desired_State As String = Mid(line, 7).Trim.ToUpper 'Possibly a list?
                    MY_JTAG.TAP.GotoState(GetStateFromInput(Desired_State))
                ElseIf line.ToUpper.StartsWith("TIR ") Then
                    IR_TAIL.LoadParams(line)
                ElseIf line.ToUpper.StartsWith("HIR ") Then
                    IR_HEAD.LoadParams(line)
                ElseIf line.ToUpper.StartsWith("TDR ") Then
                    DR_TAIL.LoadParams(line)
                ElseIf line.ToUpper.StartsWith("HDR ") Then
                    DR_HEAD.LoadParams(line)
                Else
                    RaiseEvent Printf("Unknown SVF command at line " & line_counter & " : " & line)
                End If
            Next
            MY_JTAG.TAP.GotoState(MachineState.TestLogicReset)
            Return True
        End Function

        Private Function CompareResult(ByVal TDO() As Byte, ByVal Expected() As Byte, ByVal MASK() As Byte) As Boolean
            Try
                If TDO Is Nothing Then Return False
                If MASK IsNot Nothing AndAlso Expected IsNot Nothing Then
                    For i = 0 To TDO.Length - 1
                        Dim masked_tdo As Byte = TDO(i) And MASK(i)
                        Dim masked_exp As Byte = Expected(i) And MASK(i)
                        If Not (masked_tdo = masked_exp) Then Return False
                    Next
                ElseIf Expected IsNot Nothing Then 'No MASK, use ALL CARE bits
                    For i = 0 To TDO.Length - 1
                        If Not TDO(i) = Expected(i) Then Return False
                    Next
                End If
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Private Sub DoXRunTest(ByVal wait_amount As UInt32)
            Dim s As Integer = wait_amount / 1000
            If s < 30 Then s = 30
            Threading.Thread.Sleep(s)
        End Sub

        Private Function IsValidRunState(ByVal input As String) As Boolean
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

        Private Sub DoRuntest(ByVal line As String)
            Try
                Dim start_state As MachineState = MachineState.RunTestIdle 'Default
                Dim Params() As String = line.Split(" ")
                If Params Is Nothing Then Exit Sub
                Dim Counter As Integer = 0
                If IsValidRunState(Params(Counter)) Then
                    start_state = GetStateFromInput(Params(Counter))
                    Counter += 1
                End If
                MY_JTAG.TAP.GotoState(start_state)
                If Not IsNumeric(Params(Counter)) Then Exit Sub
                Dim wait_time As Decimal = Decimal.Parse(Params(Counter), Globalization.NumberStyles.Float) 'GetDoubleFromExpString(Params(Counter))
                Select Case Params(Counter + 1).Trim.ToUpper
                    Case "TCK" 'Toggle test-clock
                        Dim ticks As UInt32 = CUInt(wait_time)
                        If MY_JTAG.FCUSB.HWBOARD = USB.FCUSB_BOARD.Classic_JTAG Then
                            MY_JTAG.TAP.ShiftOut(Nothing, ticks, False)
                        Else
                            MY_JTAG.Tap_Toggle(ticks) 'New boards can toggle via a command
                        End If
                    Case "SCK" 'Toggle system-clock
                        Threading.Thread.Sleep(wait_time)
                    Case "SEC"
                        Dim sleep_int As Integer = (wait_time * 1000)
                        If sleep_int < 1 Then sleep_int = 20
                        Threading.Thread.Sleep(sleep_int)
                    Case Else
                        Exit Sub
                End Select
                Counter += 2
                If (Counter = Params.Length) Then Exit Sub 'The rest are optional
                If (Params(Counter + 1).Trim.ToUpper = "SEC") Then
                    Dim min_time As Decimal = Decimal.Parse(Params(Counter), Globalization.NumberStyles.Float) 'GetDoubleFromExpString(Params(Counter))
                    Dim sleep_int As Integer = (min_time * 1000)
                    If sleep_int < 1 Then sleep_int = 20
                    Threading.Thread.Sleep(sleep_int)
                    Counter += 2
                End If
                If (Counter = Params.Length) Then Exit Sub 'The rest are optional
                If (Params(Counter).Trim.ToUpper = "MAXIMUM") Then
                    Dim max_time As Decimal = Decimal.Parse(Params(Counter + 1), Globalization.NumberStyles.Float) ' GetDoubleFromExpString(Params(Counter + 1))
                    Counter += 3 'THIRD ARG MUST BE SEC
                End If
                If (Counter = Params.Length) Then Exit Sub 'The rest are optional
                If (Params(Counter).Trim.ToUpper = "ENDSTATE") AndAlso IsValidRunState(Params(Counter + 1)) Then
                    Dim end_state As MachineState = GetStateFromInput(Params(Counter))
                    MY_JTAG.TAP.GotoState(end_state)
                End If
            Catch ex As Exception
                Utilities.Sleep(50)
            End Try
        End Sub

        Private Function GetStateFromInput(ByVal input As String) As MachineState
            input = RemoveComment(input)
            If input.EndsWith(";") Then input = Mid(input, 1, input.Length - 1).Trim
            Select Case input.ToUpper
                Case "IRPAUSE"
                    Return MachineState.Pause_IR
                Case "DRPAUSE"
                    Return MachineState.Pause_DR
                Case "RESET"
                    Return MachineState.TestLogicReset
                Case "IDLE"
                    Return MachineState.RunTestIdle
                Case Else
                    Return MachineState.RunTestIdle
            End Select
        End Function

        Private Function ConvertFileToProperFormat(ByVal input() As String) As String()
            Dim FormatedFileOne As New List(Of String)
            For Each line In input
                line = RemoveComment(line).Replace(vbTab, " ").Trim
                If Not line = "" Then FormatedFileOne.Add(line)
            Next
            Dim FormatedFileTwo As New List(Of String)
            Dim WorkInProgress As String = ""
            For Each line In FormatedFileOne
                WorkInProgress &= line.ToString.TrimStart
                If WorkInProgress.EndsWith(";") Then
                    WorkInProgress = Mid(WorkInProgress, 1, WorkInProgress.Length - 1).TrimEnd
                    FormatedFileTwo.Add(WorkInProgress)
                    WorkInProgress = ""
                Else
                    WorkInProgress &= " "
                End If
            Next
            Return FormatedFileTwo.ToArray
        End Function

        Private Function ConvertDataToProperFormat(ByVal data() As Byte) As xsvf_param()
            Dim pointer As Integer = 0
            Dim x As New ArrayList
            Dim XSDRSIZE As UInt32 = 8 'number of bits
            Do Until pointer = data.Length
                Dim n As New xsvf_param(data(pointer))
                Select Case n.instruction
                    Case xsvf_instruction.XTDOMASK
                        Load_TDI(data, pointer, n, XSDRSIZE)
                    Case xsvf_instruction.XREPEAT
                        n.value_uint = data(pointer + 1)
                        pointer += 2
                    Case xsvf_instruction.XRUNTEST
                        n.value_uint = Load_Uint32_Value(data, pointer)
                    Case xsvf_instruction.XSIR
                        Dim num_bytes As Integer = Math.Ceiling(data(pointer + 1) / 8)
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
                        Dim num_bytes As Integer = Math.Ceiling(XSDRSIZE / 8)
                        Dim data1(num_bytes - 1) As Byte
                        Dim data2(num_bytes - 1) As Byte
                        Array.Copy(data, pointer + 1, data1, 0, num_bytes)
                        Array.Copy(data, pointer + 1 + num_bytes, data2, 0, num_bytes)
                        n.value_data = New svf_data With {.data = data1, .bits = XSDRSIZE}
                        n.value_expected = New svf_data With {.data = data2, .bits = XSDRSIZE}
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
                        Return DirectCast(x.ToArray(GetType(xsvf_param)), xsvf_param())
                    Case xsvf_instruction.XSTATE
                        n.value_uint = data(pointer + 1)
                        n.state = GetStateFromInput(data(pointer + 1))
                        pointer += 2
                    Case xsvf_instruction.XENDIR
                        n.value_uint = data(pointer + 1)
                        Select Case n.value_uint
                            Case 0
                                n.state = MachineState.RunTestIdle
                            Case 1
                                n.state = MachineState.Pause_IR
                        End Select
                        pointer += 2
                    Case xsvf_instruction.XENDDR
                        n.value_uint = data(pointer + 1)
                        Select Case n.value_uint
                            Case 0
                                n.state = MachineState.RunTestIdle
                            Case 1
                                n.state = MachineState.Pause_DR
                        End Select
                        pointer += 2
                    Case xsvf_instruction.XSIR2 'Same as XSIR (since we should all support 255 bit lengths or more)
                        Dim num_bytes As Integer = Math.Ceiling(data(pointer + 1) / 8)
                        Dim new_Data(num_bytes - 1) As Byte
                        Array.Copy(data, pointer + 2, new_Data, 0, num_bytes)
                        n.value_data = New svf_data With {.data = new_Data, .bits = XSDRSIZE}
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
            Return DirectCast(x.ToArray(GetType(xsvf_param)), xsvf_param())
        End Function

        Private Sub Load_TDI(ByRef data() As Byte, ByRef pointer As Integer, ByRef line As xsvf_param, ByVal XSDRSIZE As Integer)
            Dim num_bytes As Integer = Math.Ceiling(XSDRSIZE / 8)
            Dim new_Data(num_bytes - 1) As Byte
            Array.Copy(data, pointer + 1, new_Data, 0, num_bytes)
            line.value_data = New svf_data With {.data = new_Data, .bits = XSDRSIZE}
            pointer += num_bytes + 1
        End Sub

        Private Sub Load_TDI_Expected(ByRef data() As Byte, ByRef pointer As Integer, ByRef line As xsvf_param, ByVal XSDRSIZE As Integer)
            Dim num_bytes As Integer = XSDRSIZE / 8 'Possible problem
            Dim data1(num_bytes - 1) As Byte
            Dim data2(num_bytes - 1) As Byte
            Array.Copy(data, pointer + 1, data1, 0, num_bytes)
            Array.Copy(data, pointer + 5, data2, 0, num_bytes)
            line.value_data = New svf_data With {.data = data1, .bits = XSDRSIZE}
            line.value_expected = New svf_data With {.data = data2, .bits = XSDRSIZE}
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

        Private Function GetBytes_FromUint(ByVal input As UInt32, ByVal MinBits As Integer) As Byte()
            Dim current(3) As Byte
            current(0) = (input And &HFF000000) >> 24
            current(1) = (input And &HFF0000) >> 16
            current(2) = (input And &HFF00) >> 8
            current(3) = (input And &HFF)
            Dim MaxSize As Integer = Math.Ceiling(MinBits / 8)
            Dim out(MaxSize - 1) As Byte
            For i = 0 To MaxSize - 1
                out(out.Length - (1 + i)) = current(current.Length - (1 + i))
            Next
            Return out
        End Function

    End Class

    Public Structure svf_data
        Dim data() As Byte
        Dim bits As Integer
    End Structure

    Public Class svf_param
        Public LEN As Integer = 0
        Public TDI As Byte()
        Public SMASK As Byte()
        Public TDO As Byte()
        Public MASK As Byte()

        Sub New()

        End Sub

        Sub New(ByVal input As String)
            LoadParams(input)
        End Sub

        Public Sub LoadParams(ByVal input As String)
            input = Mid(input, InStr(input, " ") + 1) 'We remove the first part (TIR, etc)
            If Not input.Contains(" ") Then
                LEN = input
            Else
                LEN = Mid(input, 1, InStr(input, " ") - 1)
                input = Mid(input, InStr(input, " ") + 1)
                Do Until input = ""
                    If input.ToUpper.StartsWith("TDI") Then
                        input = Mid(input, 4).Trim
                        TDI = GetBytes_Param(input)
                    ElseIf input.ToUpper.StartsWith("TDO") Then
                        input = Mid(input, 4).Trim
                        TDO = GetBytes_Param(input)
                    ElseIf input.ToUpper.StartsWith("SMASK") Then
                        input = Mid(input, 6).Trim
                        SMASK = GetBytes_Param(input)
                    ElseIf input.ToUpper.StartsWith("MASK") Then
                        input = Mid(input, 5).Trim
                        MASK = GetBytes_Param(input)
                    End If
                Loop
            End If
        End Sub

        Private Function GetBytes_Param(ByRef line As String) As Byte()
            Dim x1 As Integer = InStr(line, "(")
            Dim x2 As Integer = InStr(line, ")")
            Dim HexStr As String = line.Substring(x1, x2 - 2).Replace(" ", "")
            line = Mid(line, x2 + 1).Trim
            Return GetBytes_FromHexString(HexStr)
        End Function

    End Class

    Public Class xsvf_param
        Public instruction As xsvf_instruction
        Public state As MachineState
        Public state_end As MachineState
        Public value_uint As UInt64
        Public value_data As svf_data
        Public value_expected As svf_data

        Sub New(ByVal code As Byte)
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

    Public Module Common
        'Removes ! and // lines
        Public Function RemoveComment(ByVal input As String) As String
            If input.Contains("!") Then
                input = Mid(input, 1, InStr(input, "!") - 1)
            End If
            If input.Contains("//") Then
                input = Mid(input, 1, InStr(input, "//") - 1)
            End If
            Return input
        End Function

        Public Function GetBytes_FromHexString(ByVal hex_string As String) As Byte()
            If hex_string Is Nothing OrElse hex_string.Trim = "" Then Return Nothing
            hex_string = hex_string.Replace(" ", "").Trim.ToUpper
            If hex_string.StartsWith("0X") Then hex_string = Mid(hex_string, 3)
            If UCase(hex_string).EndsWith("H") Then hex_string = Mid(hex_string, 1, hex_string.Length - 1)
            If Not hex_string.Length Mod 2 = 0 Then hex_string = "0" & hex_string
            Dim out((hex_string.Length / 2) - 1) As Byte
            For i = 0 To out.Length - 1
                out(i) = CByte(Utilities.HexToInt(Mid(hex_string, (i * 2) + 1, 2)))
            Next
            Return out
        End Function

        Public Function FeedWord(ByRef line As String) As String
            Dim word_out As String = ""
            If line.Contains(" ") Then
                word_out = Mid(line, 1, InStr(line, " ") - 1)
                line = Mid(line, InStr(line, " ") + 1) 'Feeds a word
            Else
                word_out = line
                line = ""
            End If
            Return word_out
        End Function

    End Module

End Namespace


