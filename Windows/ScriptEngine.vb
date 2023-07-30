﻿'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2021 - ALL RIGHTS RESERVED
'CONTACT EMAIL: support@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: This class is the entire scripting engine which can control the software
'via user supplied text files. The langauge format is similar to BASIC.

Namespace EC_ScriptEngine

    Public Class Processor : Implements IDisposable
        Public Const Build As Integer = 310

        Public CmdFunctions As New ScriptCmd
        Public CurrentVars As New ScriptVariableManager
        Public CurrentScript As New ScriptFile

        Delegate Function ScriptFunction(arguments() As ScriptVariable, Index As Integer) As ScriptVariable

        Private script_is_running As Boolean = False

        Public Event PrintConsole(msg As String)
        Public Event SetStatus(msg As String)

        Private ABORT_SCRIPT As Boolean = False

        Public ReadOnly Property IsRunning As Boolean
            Get
                Return script_is_running
            End Get
        End Property

        Sub New()
            Dim STR_CMD As New ScriptCmd("STRING")
            STR_CMD.Add("upper", {CmdPrm.String}, New ScriptFunction(AddressOf c_str_upper))
            STR_CMD.Add("lower", {CmdPrm.String}, New ScriptFunction(AddressOf c_str_lower))
            STR_CMD.Add("hex", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_str_hex))
            STR_CMD.Add("length", {CmdPrm.String}, New ScriptFunction(AddressOf c_str_length))
            STR_CMD.Add("toint", {CmdPrm.String}, New ScriptFunction(AddressOf c_str_toint))
            STR_CMD.Add("fromint", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_str_fromint))
            AddScriptNest(STR_CMD)
            Dim DATA_CMD As New ScriptCmd("DATA")
            DATA_CMD.Add("new", {CmdPrm.Integer, CmdPrm.Data}, New ScriptFunction(AddressOf c_data_new))
            DATA_CMD.Add("fromhex", {CmdPrm.String}, New ScriptFunction(AddressOf c_data_fromhex))
            DATA_CMD.Add("compare", {CmdPrm.Data}, New ScriptFunction(AddressOf c_data_compare))
            DATA_CMD.Add("length", {CmdPrm.Data}, New ScriptFunction(AddressOf c_data_length))
            DATA_CMD.Add("resize", {CmdPrm.Data, CmdPrm.Integer, CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_data_resize))
            DATA_CMD.Add("hword", {CmdPrm.Data, CmdPrm.Integer}, New ScriptFunction(AddressOf c_data_hword))
            DATA_CMD.Add("word", {CmdPrm.Data, CmdPrm.Integer}, New ScriptFunction(AddressOf c_data_word))
            DATA_CMD.Add("tostr", {CmdPrm.Data}, New ScriptFunction(AddressOf c_data_tostr))
            DATA_CMD.Add("copy", {CmdPrm.Data, CmdPrm.Integer, CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_data_copy))
            DATA_CMD.Add("combine", {CmdPrm.Data}, New ScriptFunction(AddressOf c_data_combine))
            AddScriptNest(DATA_CMD)
            Dim IO_CMD As New ScriptCmd("IO")
            IO_CMD.Add("open", {CmdPrm.String_Optional, CmdPrm.String_Optional}, New ScriptFunction(AddressOf c_io_open))
            IO_CMD.Add("save", {CmdPrm.Data, CmdPrm.String_Optional, CmdPrm.String_Optional}, New ScriptFunction(AddressOf c_io_save))
            IO_CMD.Add("read", {CmdPrm.String}, New ScriptFunction(AddressOf c_io_read))
            IO_CMD.Add("write", {CmdPrm.Data, CmdPrm.String}, New ScriptFunction(AddressOf c_io_write))
            AddScriptNest(IO_CMD)
            'Generic functions
            AddScriptCommand("writeline", {CmdPrm.Any, CmdPrm.Bool_Optional}, New ScriptFunction(AddressOf c_writeline))
            AddScriptCommand("print", {CmdPrm.Any, CmdPrm.Bool_Optional}, New ScriptFunction(AddressOf c_writeline))
            AddScriptCommand("msgbox", {CmdPrm.Any}, New ScriptFunction(AddressOf c_msgbox))
            AddScriptCommand("status", {CmdPrm.String}, New ScriptFunction(AddressOf c_setstatus))
            AddScriptCommand("sleep", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_sleep))
            AddScriptCommand("ask", {CmdPrm.String}, New ScriptFunction(AddressOf c_ask))
            AddScriptCommand("abort", Nothing, New ScriptFunction(AddressOf c_abort))
            AddScriptCommand("catalog", Nothing, New ScriptFunction(AddressOf c_catalog))
            AddScriptCommand("cpen", {CmdPrm.Bool}, New ScriptFunction(AddressOf c_cpen))
            AddScriptCommand("crc16", {CmdPrm.Data}, New ScriptFunction(AddressOf c_crc16))
            AddScriptCommand("crc32", {CmdPrm.Data}, New ScriptFunction(AddressOf c_crc32))
            AddScriptCommand("cint", {CmdPrm.UInteger}, New ScriptFunction(AddressOf c_cint))
            AddScriptCommand("cuint", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_cuint))
            'CmdFunctions.Add("debug", {CmdPrm.UInteger}, New ScriptFunction(AddressOf c_debug))
        End Sub

        Public Function AddScriptNest(c As ScriptCmd) As Boolean
            CmdFunctions.AddNest(c)
            Return True
        End Function

        Public Function AddScriptCommand(cmd As String, params() As CmdPrm, e As [Delegate]) As Boolean
            CmdFunctions.Add(cmd, params, e)
            Return True
        End Function

        Friend Function c_debug(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Return Nothing
        End Function

        Friend Function ExecuteCommand(cmd_line As String) As Boolean
            Me.ABORT_SCRIPT = False
            Dim scripe_line As New ScriptElement(Me)
            Dim result As ParseResult = scripe_line.Parse(cmd_line, True)
            If result.IsError Then
                RaiseEvent PrintConsole("Error: " & result.ErrorMsg)
                Return False
            End If
            result = ExecuteScriptElement(scripe_line, Nothing)
            If result.IsError Then
                RaiseEvent PrintConsole("Error: " & result.ErrorMsg)
            End If
            Return True 'No error!
        End Function
        'Unloads any current script
        Friend Function Unload() As Boolean
            Me.ABORT_SCRIPT = True
            Me.CurrentVars.Clear()
            Me.CurrentScript.Reset()
            Return True
        End Function
        'This loads the script file
        Friend Function LoadFile(file_name As IO.FileInfo) As Boolean
            Me.Unload()
            RaiseEvent PrintConsole("Loading FlashcatUSB script: " & file_name.Name)
            Dim f() As String = Utilities.FileIO.ReadFile(file_name.FullName)
            Dim err_str As String = ""
            Dim line_err As Integer = 0 'The line within the file that has the error
            If CurrentScript.LoadFile(Me, f, line_err, err_str) Then
                RaiseEvent PrintConsole("Script successfully loaded")
                Me.script_is_running = True
                Dim td As New Threading.Thread(AddressOf RunScript)
                td.SetApartmentState(Threading.ApartmentState.STA)
                td.IsBackground = True
                td.Start()
                Return True
            Else
                If err_str.Equals("") Then
                    RaiseEvent PrintConsole("Error loading script: " & err_str & " (line " & (line_err + 1) & ")")
                End If
                Return False
            End If
        End Function

        Friend Function RunScriptFile(script_text() As String) As Boolean
            Dim line_err As Integer
            Dim line_reason As String = ""
            If CurrentScript.LoadFile(Me, script_text, line_err, line_reason) Then
                RaiseEvent PrintConsole("Script successfully loaded")
                Dim td As New Threading.Thread(AddressOf RunScript)
                td.SetApartmentState(Threading.ApartmentState.STA)
                td.IsBackground = True
                td.Start()
                Return True
            Else
                If Not line_reason = "" Then
                    RaiseEvent PrintConsole("Error loading script: " & line_reason & " (line " & (line_err + 1) & ")")
                End If
                Return False
            End If
        End Function

        Friend Function RunScript() As Boolean
            Try
                Me.ABORT_SCRIPT = False
                Dim main_param As New ExecuteParam
                Dim result As Boolean = ExecuteElements(CurrentScript.TheScript.ToArray, main_param)
                If Not result Then
                    If Not main_param.err_reason = "" Then
                        RaiseEvent PrintConsole("Error in script: " & main_param.err_reason & " (line " & (main_param.err_line + 1) & ")")
                    End If
                    Return False
                End If
                If main_param.exit_task = ExitMode.GotoLabel Then
                    RaiseEvent PrintConsole("Error in script, unable to find label: " & main_param.goto_label)
                    Return False
                End If
            Catch ex As Exception
            Finally
                Me.script_is_running = False
            End Try
            Return True
        End Function
        'Calls a event (wrapper for runscript)
        Public Sub CallEvent(o As Object)
            Dim EventName As String = CStr(o)
            RaiseEvent PrintConsole("Button Hander::Calling Event: " & EventName)
            Dim se As ScriptEvent = GetScriptEvent(EventName)
            If se IsNot Nothing Then
                ExecuteScriptEvent(se, Nothing, Nothing)
            Else
                RaiseEvent PrintConsole("Error: Event does not exist")
            End If
            RaiseEvent PrintConsole("Button Hander::Calling Event: Done")
        End Sub

        Friend Function ExecuteElements(e() As ScriptLineElement, ByRef params As ExecuteParam) As Boolean
            If Me.ABORT_SCRIPT Then Return False
            If params.exit_task = ExitMode.LeaveScript Then Return True
            If e IsNot Nothing AndAlso e.Length > 0 Then
                For i = 0 To e.Length - 1
                    Select Case e(i).ElementType
                        Case ScriptFileElementType.ELEMENT
                            Dim result As ParseResult = ExecuteScriptElement(CType(e(i), ScriptElement), params.exit_task)
                            If result.IsError Then
                                params.err_reason = result.ErrorMsg
                                params.err_line = e(i).INDEX
                                Return False
                            End If
                            If params.exit_task = ExitMode.LeaveScript Then Return True
                        Case ScriptFileElementType.FOR_LOOP
                            Dim se As ScriptLoop = CType(e(i), ScriptLoop)
                            If Not se.Evaluate Then
                                params.err_line = e(i).INDEX
                                params.err_reason = "Failed to evaluate LOOP parameters"
                                Return False
                            End If
                            Dim counter_sv As New ScriptVariable(se.VAR_NAME, DataType.UInteger)
                            For loop_index As UInt32 = se.START_IND To se.END_IND Step se.STEP_VAL
                                counter_sv.Value = loop_index
                                CurrentVars.SetVariable(counter_sv)
                                Dim loop_result As Boolean = ExecuteElements(se.LOOP_MAIN, params)
                                If Not loop_result Then Return False
                                If params.exit_task = ExitMode.Leave Then
                                    params.exit_task = ExitMode.KeepRunning
                                    Exit For
                                ElseIf params.exit_task = ExitMode.LeaveEvent Then
                                    Return True
                                ElseIf params.exit_task = ExitMode.LeaveScript Then
                                    Return True
                                End If
                            Next
                        Case ScriptFileElementType.IF_CONDITION
                            Dim se As ScriptCondition = CType(e(i), ScriptCondition)
                            Dim test_condition As ScriptVariable = se.CONDITION.Compile(params.exit_task)
                            If test_condition Is Nothing OrElse test_condition.Data.VarType = DataType.FncError Then
                                params.err_reason = CStr(test_condition.Value)
                                params.err_line = se.INDEX
                                Return False
                            End If
                            Dim result As Boolean = CBool(test_condition.Value)
                            If se.NOT_MODIFIER Then result = Not result
                            Dim execute_result As Boolean
                            If result Then
                                execute_result = ExecuteElements(se.IF_MAIN, params)
                            Else
                                execute_result = ExecuteElements(se.IF_ELSE, params)
                            End If
                            If Not execute_result Then Return False
                            If params.exit_task = ExitMode.Leave Or params.exit_task = ExitMode.LeaveScript Or params.exit_task = ExitMode.LeaveEvent Then Return True
                        Case ScriptFileElementType.GOTO
                            Dim so As ScriptGoto = CType(e(i), ScriptGoto)
                            params.goto_label = so.TO_LABEL.ToUpper
                            params.exit_task = ExitMode.GotoLabel
                        Case ScriptFileElementType.EXIT
                            Dim so As ScriptExit = CType(e(i), ScriptExit)
                            params.exit_task = so.MODE
                            Return True
                        Case ScriptFileElementType.RETURN
                            Dim sr As ScriptReturn = CType(e(i), ScriptReturn)
                            Dim ret_val As ScriptVariable = sr.Compile(params.exit_task) 'Now compute the return result
                            params.err_reason = sr.ERROR_MSG
                            params.err_line = sr.INDEX
                            If sr.HAS_ERROR Then Return False
                            CurrentVars.ClearVariable("EVENTRETURN")
                            If ret_val IsNot Nothing Then
                                Dim n As New ScriptVariable("EVENTRETURN", ret_val.Data.VarType)
                                n.Value = ret_val.Value
                                CurrentVars.SetVariable(n)
                            End If
                            params.exit_task = ExitMode.LeaveEvent
                            Return True
                    End Select
                    If params.exit_task = ExitMode.GotoLabel Then
                        Dim label_found As Boolean = False
                        For x = 0 To e.Length - 1 'Search local labels first
                            If e(x).ElementType = ScriptFileElementType.LABEL Then
                                If DirectCast(e(x), ScriptLabel).NAME.ToUpper.Equals(params.goto_label) Then
                                    i = (x - 1) 'This sets the execution to the label
                                    params.exit_task = ExitMode.KeepRunning
                                    label_found = True
                                    Exit For
                                End If
                            End If
                        Next
                        If Not label_found Then Return True 'We didn't find the label, go up a level
                    End If
                Next
            End If
            Return True
        End Function

        Friend Function ExecuteScriptElement(e As ScriptElement, ByRef exit_task As ExitMode) As ParseResult
            Try
                Dim sv As ScriptVariable = e.Compile(exit_task)
                If sv Is Nothing Then Return New ParseResult
                If sv.Data.VarType = DataType.FncError Then Return New ParseResult(CStr(sv.Data.Value))
                If sv Is Nothing Then Return New ParseResult 'Compiled successfully but no value to save
                If (Not e.TARGET_NAME = "") AndAlso Not e.TARGET_OPERATION = TargetOper.NONE Then
                    If (Not e.TARGET_VAR = "") Then
                        If CurrentVars.IsVariable(e.TARGET_VAR) AndAlso CurrentVars.GetVariable(e.TARGET_VAR).Data.VarType = DataType.UInteger Then
                            e.TARGET_INDEX = CInt(CurrentVars.GetVariable(e.TARGET_VAR).Value) 'Gets the variable and assigns it to the index
                        Else
                            Return New ParseResult("Target index is not an integer or integer variable")
                        End If
                    End If
                    If (e.TARGET_INDEX > -1) Then 'We are assinging this result to an index within a data array
                        Dim current_var As ScriptVariable = CurrentVars.GetVariable(e.TARGET_NAME)
                        If current_var Is Nothing Then Return New ParseResult("Target index used on a variable that does not exist")
                        If current_var.Data.VarType = DataType.NULL Then Return New ParseResult("Target index used on a variable that does not yet exist")
                        If Not current_var.Data.VarType = DataType.Data Then Return New ParseResult("Target index used on a variable that is not a DATA array")
                        Dim data_out() As Byte = CType(current_var.Value, Byte())
                        If sv.Data.VarType = DataType.UInteger Then
                            Dim byte_out As Byte = CByte(CUInt(sv.Value) And 255)
                            data_out(e.TARGET_INDEX) = byte_out
                        End If
                        Dim set_var As New ScriptVariable(e.TARGET_NAME, DataType.Data)
                        set_var.Value = data_out
                        CurrentVars.SetVariable(set_var)
                    Else 'No Target Index
                        Dim new_var As New ScriptVariable(e.TARGET_NAME, sv.Data.VarType)
                        new_var.Value = sv.Value
                        Dim var_op As OperandOper = OperandOper.NOTSPECIFIED
                        Select Case e.TARGET_OPERATION
                            Case TargetOper.EQ
                                CurrentVars.SetVariable(new_var) : Return New ParseResult()
                            Case TargetOper.ADD
                                var_op = OperandOper.ADD
                            Case TargetOper.SUB
                                var_op = OperandOper.SUB
                        End Select
                        Dim existing_var As ScriptVariable = CurrentVars.GetVariable(e.TARGET_NAME)
                        If existing_var Is Nothing OrElse existing_var.Data.VarType = DataType.NULL Then
                            CurrentVars.SetVariable(new_var)
                        ElseIf Not existing_var.Data.VarType = new_var.Data.VarType Then
                            CurrentVars.SetVariable(new_var)
                        Else
                            Dim result_var As ScriptVariable = CompileSVars(existing_var, new_var, var_op)
                            If result_var.Data.VarType = DataType.FncError Then Return New ParseResult(CStr(result_var.Data.Value))
                            Dim compiled_var As New ScriptVariable(e.TARGET_NAME, result_var.Data.VarType)
                            compiled_var.Value = result_var.Value
                            CurrentVars.SetVariable(compiled_var)
                        End If
                    End If
                End If
                Return New ParseResult()
            Catch ex As Exception
                Return New ParseResult("General purpose error")
            End Try
        End Function

        Friend Function ExecuteScriptEvent(s_event As ScriptEvent, arguments() As ScriptVariable, ByRef exit_task As ExitMode) As ScriptVariable
            If arguments IsNot Nothing AndAlso arguments.Count > 0 Then
                Dim i As Integer = 1
                For Each item In arguments
                    Dim n As New ScriptVariable("$" & i.ToString, item.Data.VarType)
                    n.Value = item.Value
                    CurrentVars.SetVariable(n)
                    i = i + 1
                Next
            End If
            Dim event_param As New ExecuteParam
            If Not ExecuteElements(s_event.Elements, event_param) Then
                RaiseEvent PrintConsole("Error in Event: " & event_param.err_reason & " (line " & (event_param.err_line + 1) & ")")
                Return Nothing
            End If
            If event_param.exit_task = ExitMode.GotoLabel Then
                RaiseEvent PrintConsole("Error in Event, unable to find label: " & event_param.goto_label)
                Return Nothing
            End If
            Dim event_result As ScriptVariable = CurrentVars.GetVariable("EVENTRETURN")
            If event_result IsNot Nothing AndAlso Not event_result.Data.VarType = DataType.NULL Then
                Dim new_var As New ScriptVariable(CurrentVars.GetNewName, event_result.Data.VarType)
                new_var.Value = event_result.Value
                CurrentVars.ClearVariable("EVENTRETURN")
                Return new_var
            Else
                Return Nothing
            End If
        End Function

        Friend Function GetScriptEvent(input As String) As ScriptEvent
            Dim main_event_name As String = ""
            ParseToFunctionAndSub(input, main_event_name, Nothing, Nothing, Nothing)
            For Each item In CurrentScript.TheScript
                If item.ElementType = ScriptFileElementType.EVENT Then
                    Dim se As ScriptEvent = CType(item, ScriptEvent)
                    If se.EVENT_NAME.ToUpper.Equals(main_event_name.ToUpper) Then Return se
                End If
            Next
            Return Nothing
        End Function

        Friend Function IsScriptEvent(input As String) As Boolean
            Dim main_event_name As String = ""
            ParseToFunctionAndSub(input, main_event_name, Nothing, Nothing, Nothing)
            For Each item In CurrentScript.EventList
                If item.ToUpper = main_event_name.ToUpper Then Return True
            Next
            Return False
        End Function

#Region "IDisposable Support"
        Private disposedValue As Boolean ' To detect redundant calls

        Protected Overridable Sub Dispose(disposing As Boolean)
            If (Not disposedValue) Then
                MainApp.ProgressBar_Dispose()
                CurrentScript = Nothing
                CurrentVars = Nothing
            End If
            disposedValue = True
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        Public Overrides Function ToString() As String
            Return MyBase.ToString()
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return MyBase.Equals(obj)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return MyBase.GetHashCode()
        End Function

        Protected Overrides Sub Finalize()
            MyBase.Finalize()
        End Sub
#End Region

#Region "Internal Commands"

#Region "String commands"

        Friend Function c_str_upper(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim input As String = CStr(arguments(0).Value)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.String)
            sv.Value = input.ToUpper
            Return sv
        End Function

        Friend Function c_str_lower(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim input As String = CStr(arguments(0).Value)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.String)
            sv.Value = input.ToLower
            Return sv
        End Function

        Friend Function c_str_hex(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim input As Integer = CInt(arguments(0).Value)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.String)
            sv.Value = "0x" & input.ToString("X")
            Return sv
        End Function

        Friend Function c_str_length(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim input As String = CStr(arguments(0).Value)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Integer)
            sv.Value = CInt(input.Length)
            Return sv
        End Function

        Friend Function c_str_toint(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim input As String = CStr(arguments(0).Value)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Integer)
            If input.Trim = "" Then
                sv.Value = 0
            Else
                sv.Value = CInt(input)
            End If
            Return sv
        End Function

        Friend Function c_str_fromint(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim input As Int32 = CInt(arguments(0).Value)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.String)
            sv.Value = input.ToString
            Return sv
        End Function

#End Region

#Region "Data commands"

        Friend Function c_data_new(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim size As Int32 = CInt(arguments(0).Value) 'Size of the Data Array
            Dim data(size - 1) As Byte
            If (arguments.Length > 1) Then
                Dim data_init() As Byte = CType(arguments(1).Value, Byte())
                Dim bytes_to_repeat As Integer = data_init.Length
                Dim ptr As Integer = 0
                For i = 0 To data.Length - 1
                    data(i) = data_init(ptr)
                    ptr += 1
                    If ptr = bytes_to_repeat Then ptr = 0
                Next
            Else
                Utilities.FillByteArray(data, 255)
            End If
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Data)
            sv.Value = data
            Return sv
        End Function

        Friend Function c_data_fromhex(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim input As String = CStr(arguments(0).Value)
            Dim data() As Byte = Utilities.Bytes.FromHexString(input)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Data)
            sv.Value = data
            Return sv
        End Function

        Friend Function c_data_compare(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim data1() As Byte = CType(arguments(0).Value, Byte())
            Dim data2() As Byte = CType(arguments(1).Value, Byte())
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Bool)
            If data1 Is Nothing And data2 Is Nothing Then
                sv.Value = True
            ElseIf data1 Is Nothing AndAlso data2 IsNot Nothing Then
                sv.Value = False
            ElseIf data1 IsNot Nothing AndAlso data2 Is Nothing Then
                sv.Value = False
            ElseIf Not data1.Length = data2.Length Then
                sv.Value = False
            Else
                sv.Value = True 'Set to true and if byte mismatch then return false
                For i = 0 To data1.Length - 1
                    If Not data1(i) = data2(i) Then sv.Value = False : Exit For
                Next
            End If
            Return sv
        End Function

        Friend Function c_data_length(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim data1() As Byte = CType(arguments(0).Value, Byte())
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.UInteger)
            sv.Value = data1.Length
            Return sv
        End Function

        Friend Function c_data_resize(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim data_arr() As Byte = CType(arguments(0).Value, Byte())
            Dim copy_index As Int32 = CInt(arguments(1).Value)
            Dim copy_length As Int32 = data_arr.Length - copy_index
            If arguments.Length = 3 Then copy_length = CInt(arguments(2).Value)
            Dim data_out(copy_length - 1) As Byte
            Array.Copy(data_arr, copy_index, data_out, 0, copy_length)
            arguments(0).Value = data_out
            CurrentVars.SetVariable(arguments(0))
            Return Nothing
        End Function

        Friend Function c_data_hword(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim data1() As Byte = CType(arguments(0).Value, Byte())
            Dim offset As Int32 = CInt(arguments(1).Value)
            Dim b(1) As Byte
            b(0) = data1(offset)
            b(1) = data1(offset + 1)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Integer)
            sv.Value = Utilities.Bytes.ToUInt16(b)
            Return sv
        End Function

        Friend Function c_data_word(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim data1() As Byte = CType(arguments(0).Value, Byte())
            Dim offset As Int32 = CInt(arguments(1).Value)
            Dim b(3) As Byte
            b(0) = data1(offset)
            b(1) = data1(offset + 1)
            b(2) = data1(offset + 2)
            b(3) = data1(offset + 3)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Integer)
            sv.Value = Utilities.Bytes.ToUInt32(b)
            Return sv
        End Function

        Friend Function c_data_tostr(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim data1() As Byte = CType(arguments(0).Value, Byte())
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.String)
            sv.Value = Utilities.Bytes.ToHexString(data1)
            Return sv
        End Function

        Friend Function c_data_copy(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim data1() As Byte = CType(arguments(0).Value, Byte())
            Dim src_ind As Integer = CInt(arguments(1).Value)
            Dim data_len As Integer = data1.Length - src_ind
            If (arguments.Length > 2) Then data_len = CInt(arguments(3).Value)
            Dim new_data(data_len - 1) As Byte
            Array.Copy(data1, src_ind, new_data, 0, new_data.Length)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Data)
            sv.Value = new_data
            Return sv
        End Function

        Friend Function c_data_combine(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim data1() As Byte = CType(arguments(0).Value, Byte())
            Dim data2() As Byte = CType(arguments(1).Value, Byte())
            If Not (data1 IsNot Nothing AndAlso data1.Length > 0) Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Argument 1 must be a valid data array"}
            End If
            If Not (data2 IsNot Nothing AndAlso data2.Length > 0) Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Argument 2 must be a valid data array"}
            End If
            Dim new_data_arr(data1.Length + data2.Length - 1) As Byte
            Array.Copy(data1, 0, new_data_arr, 0, data1.Length)
            Array.Copy(data2, 0, new_data_arr, data1.Length, data2.Length)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Data)
            sv.Value = new_data_arr
            Return sv
        End Function

#End Region

#Region "IO commands"

        Friend Function c_io_open(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim title As String = "Choose file to open"
            Dim filter As String = "All files (*.*)|*.*"
            Dim opt_path As String = "\"
            If arguments.Length > 0 Then title = CStr(arguments(0).Value)
            If arguments.Length > 1 Then filter = CStr(arguments(1).Value)
            If arguments.Length > 2 Then opt_path = CStr(arguments(2).Value)
            Dim user_reponse As String = MainApp.PromptUser_OpenFile(title, filter, opt_path)
            If user_reponse = "" Then Return Nothing
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Data)
            sv.Value = Utilities.FileIO.ReadBytes(user_reponse) 'There was an error here!
            Return sv
        End Function

        Friend Function c_io_save(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim var_data() As Byte = CType(arguments(0).Value, Byte())
            Dim filter As String = "All files (*.*)|*.*"
            Dim prompt_text As String = ""
            Dim default_file As String = ""
            If (arguments.Length > 1) Then
                prompt_text = CStr(arguments(1).Value)
            End If
            If (arguments.Length > 2) Then
                default_file = CStr(arguments(2).Value)
            End If
            Dim user_reponse As String = MainApp.PromptUser_SaveFile(prompt_text, filter, default_file)
            If user_reponse = "" Then
                RaiseEvent PrintConsole("User canceled operation to save data")
            Else
                Utilities.FileIO.WriteBytes(var_data, user_reponse)
                RaiseEvent PrintConsole("Data saved: " & var_data.Length & " bytes written")
            End If
            Return Nothing
        End Function

        Friend Function c_io_read(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim input As String = CStr(arguments(0).Value)
            Dim local_file As New IO.FileInfo(input)
            If local_file.Exists Then
                Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Data)
                sv.Value = Utilities.FileIO.ReadBytes(local_file.FullName)
                Return sv
            Else
                RaiseEvent PrintConsole("Error in IO.Read: file not found: " & local_file.FullName)
            End If
            Return Nothing
        End Function

        Friend Function c_io_write(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim data1() As Byte = CType(arguments(0).Value, Byte())
            Dim destination As String = CStr(arguments(1).Value)
            If Not Utilities.FileIO.WriteBytes(data1, destination) Then
                RaiseEvent PrintConsole("Error in IO.Write: failed to write data")
            End If
            Return Nothing
        End Function

#End Region

#Region "Misc commands"

        Friend Function c_msgbox(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim message_text As String = ""
            If arguments(0).Data.VarType = DataType.Data Then
                Dim d() As Byte = CType(arguments(0).Value, Byte())
                message_text = "Data (" & Format(d.Length, "#,###") & " bytes)"
            Else
                message_text = CStr(arguments(0).Value)
            End If
            MainApp.PromptUser_Msg(message_text)
            Return Nothing
        End Function

        Friend Function c_writeline(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            If arguments(0).Data.VarType = DataType.Data Then
                Dim d() As Byte = CType(arguments(0).Value, Byte())
                Dim display_addr As Boolean = True
                If arguments.Length > 1 Then
                    display_addr = CBool(arguments(1).Value)
                End If
                Dim bytesLeft As Integer = d.Length
                Dim i As Integer = 0
                Do Until bytesLeft = 0
                    Dim bytes_to_display As Integer = Math.Min(bytesLeft, 16)
                    Dim sec(bytes_to_display - 1) As Byte
                    Array.Copy(d, i, sec, 0, sec.Length)
                    Dim line_out As String = Utilities.Bytes.ToPaddedHexString(sec)
                    If display_addr Then
                        RaiseEvent PrintConsole("0x" & Hex(i).PadLeft(6, "0"c) & ":  " & line_out)
                    Else
                        RaiseEvent PrintConsole(line_out)
                    End If
                    i += bytes_to_display
                    bytesLeft -= bytes_to_display
                Loop
            Else
                Dim message_text As String = CStr(arguments(0).Value)
                RaiseEvent PrintConsole(message_text)
            End If
            Return Nothing
        End Function

        Friend Function c_setstatus(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim message_text As String = CStr(arguments(0).Value)
            RaiseEvent SetStatus(message_text)
            Return Nothing
        End Function

        Friend Function c_sleep(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim wait_ms As Integer = CInt(arguments(0).Value)
            Utilities.Sleep(wait_ms)
            Dim sw As New Stopwatch
            sw.Start()
            Do Until sw.ElapsedMilliseconds >= wait_ms
                Application.DoEvents() 'We do this as not to lock up the other threads or processes
                Utilities.Sleep(50)
            Loop
            Return Nothing
        End Function

        Friend Function c_ask(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim the_question As String = CStr(arguments(0).Value)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Bool)
            sv.Value = MainApp.PromptUser_Ask(the_question)
            Return sv
        End Function

        Friend Function c_abort(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Me.ABORT_SCRIPT = True
            RaiseEvent PrintConsole("Aborting any running script")
            Return Nothing
        End Function

        Friend Function c_catalog(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim Gb004 As UInt32 = 536870912
            RaiseEvent PrintConsole("Creating HTML catalogs for all supported parts")
            FlashDatabase.CreateHtmlCatalog(FlashMemory.MemoryType.SERIAL_NOR, 3, "spi_database.html")
            FlashDatabase.CreateHtmlCatalog(FlashMemory.MemoryType.SERIAL_NAND, 3, "spinand_database.html")
            FlashDatabase.CreateHtmlCatalog(FlashMemory.MemoryType.PARALLEL_NOR, 3, "mpf_database.html")
            FlashDatabase.CreateHtmlCatalog(FlashMemory.MemoryType.PARALLEL_NAND, 3, "nand_all_database.html")
            FlashDatabase.CreateHtmlCatalog(FlashMemory.MemoryType.PARALLEL_NAND, 3, "nand_small_database.html", Gb004)
            FlashDatabase.CreateHtmlCatalog(FlashMemory.MemoryType.HYPERFLASH, 3, "hf_database.html")
            FlashDatabase.CreateHtmlCatalog(FlashMemory.MemoryType.OTP_EPROM, 3, "otp_database.html")
            FlashDatabase.CreateHtmlCatalog(FlashMemory.MemoryType.FWH_NOR, 3, "fwh_database.html")
            Return Nothing
        End Function

        Friend Function c_cpen(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim cp_en As Boolean = CBool(arguments(0).Value)
            If Not MAIN_FCUSB.IS_CONNECTED Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "FlashcatUSB device is not connected"}
            End If
            Dim w_index As UInt32 = 0UL
            If cp_en Then w_index = 1UL
            MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.EXPIO_CPEN, Nothing, w_index)
            If cp_en Then
                RaiseEvent PrintConsole("CPEN pin set to HIGH")
            Else
                RaiseEvent PrintConsole("CPEN pin set to LOW")
            End If
            Return Nothing
        End Function

        Friend Function c_crc16(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim DataBytes() As Byte = CType(arguments(0).Value, Byte())
            Dim crc16_value As UInt32 = Utilities.CRC16.ComputeChecksum(DataBytes)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.UInteger)
            sv.Value = crc16_value
            Return sv
        End Function

        Friend Function c_crc32(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim DataBytes() As Byte = CType(arguments(0).Value, Byte())
            Dim crc32_value As UInt32 = Utilities.CRC32.ComputeChecksum(DataBytes)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.UInteger)
            sv.Value = crc32_value
            Return sv
        End Function

        Friend Function c_cint(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim value As UInt32 = CUInt(arguments(0).Value)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Integer)
            sv.Value = CInt(value)
            Return sv
        End Function

        Friend Function c_cuint(arguments() As ScriptVariable, Index As Integer) As ScriptVariable
            Dim value As Int32 = CInt(arguments(0).Value)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.UInteger)
            sv.Value = CUInt(value)
            Return sv
        End Function

#End Region

#End Region

    End Class

    Public Class ScriptCmd
        Private Nests As New List(Of ScriptCmd)
        Private Cmds As New List(Of CmdEntry)

        Public Property Name As String

        Sub New(Optional group_name As String = "")
            Me.Name = group_name
        End Sub

        Friend Sub Add(cmd As String, params() As CmdPrm, e As [Delegate])
            Dim n_cmd As New CmdEntry
            n_cmd.cmd = cmd
            n_cmd.parameters = params
            n_cmd.fnc = e
            Cmds.Add(n_cmd)
        End Sub

        Friend Sub AddNest(sub_commands As ScriptCmd)
            Nests.Add(sub_commands)
        End Sub

        Friend Function IsScriptFunction(input As String) As Boolean
            Dim main_fnc As String = ""
            Dim sub_fnc As String = ""
            ParseToFunctionAndSub(input, main_fnc, sub_fnc, Nothing, Nothing)
            If (sub_fnc = "") Then
                For Each item In Nests
                    If item.Name.ToUpper.Equals(main_fnc.ToUpper) Then Return True
                Next
                For Each s In Me.Cmds
                    If s.cmd.ToUpper.Equals(main_fnc.ToUpper) Then Return True
                Next
            Else
                For Each item In Nests
                    If item.Name.ToUpper.Equals(main_fnc.ToUpper) Then
                        For Each s In item.Cmds
                            If s.cmd.ToUpper.Equals(sub_fnc.ToUpper) Then Return True
                        Next
                    End If
                Next
                Return False
            End If
            Return False
        End Function

        Friend Function GetScriptFunction(fnc_name As String, sub_fnc As String, ByRef params() As CmdPrm, ByRef e As [Delegate]) As Boolean
            If (sub_fnc = "") Then
                For Each s In Me.Cmds
                    If s.cmd.ToUpper.Equals(fnc_name.ToUpper) Then
                        params = s.parameters
                        e = s.fnc
                        Return True
                    End If
                Next
            Else
                For Each item In Nests
                    If item.Name.ToUpper.Equals(fnc_name.ToUpper) Then
                        For Each s In item.Cmds
                            If s.cmd.ToUpper.Equals(sub_fnc.ToUpper) Then
                                params = s.parameters
                                e = s.fnc
                                Return True
                            End If
                        Next
                    End If
                Next
                Return False
            End If
            Return Nothing
        End Function


    End Class

    Friend Structure CmdEntry
        Public cmd As String
        Public parameters() As CmdPrm
        Public fnc As [Delegate]
    End Structure

    Friend Class ExecuteParam
        Public exit_task As ExitMode
        Public err_line As Integer
        Public err_reason As String
        Public goto_label As String
    End Class

    Public Class ParseResult
        Public ErrorMsg As String
        Public ReadOnly Property IsError As Boolean

        Sub New()
            Me.IsError = False
        End Sub

        Sub New(error_msg As String)
            Me.ErrorMsg = error_msg
            Me.IsError = True
        End Sub

    End Class

    Friend Class ScriptElementOperand
        Private MyParent As Processor
        Public OPERANDS As New List(Of ScriptElementOperandEntry)

        Public ParsingResult As ParseResult

        Sub New(oParent As Processor)
            Me.ParsingResult = New ParseResult() 'No Error
            Me.MyParent = oParent
        End Sub

        Friend Function Parse(text_input As String) As ParseResult
            text_input = text_input.Trim()
            Do Until text_input.Equals("")
                If text_input.StartsWith("(") Then
                    Dim sub_section As String = FeedParameter(text_input)
                    Dim x As New ScriptElementOperandEntry(MyParent, ScriptElementDataType.SubItems)
                    x.SubOperands = New ScriptElementOperand(MyParent)
                    Dim result As ParseResult = x.SubOperands.Parse(sub_section)
                    If result.IsError Then Return result
                    OPERANDS.Add(x)
                Else
                    Dim main_element As String = FeedElement(text_input)
                    If MyParent.CmdFunctions.IsScriptFunction(main_element) Then
                        OPERANDS.Add(ParseFunctionInput(main_element))
                    ElseIf MyParent.IsScriptEvent(main_element) Then
                        OPERANDS.Add(ParseEventInput(main_element))
                    ElseIf MyParent.CurrentVars.IsVariable(main_element) Then
                        OPERANDS.Add(ParseVarInput(main_element))
                    ElseIf main_element.ToUpper.Equals("TRUE") OrElse main_element.ToUpper.Equals("FALSE") Then
                        OPERANDS.Add(New ScriptElementOperandEntry(MyParent, DataType.Bool, CBool(main_element)))
                    ElseIf Utilities.IsDataType.Integer(main_element) Then
                        OPERANDS.Add(New ScriptElementOperandEntry(MyParent, DataType.Integer, CInt(main_element)))
                    ElseIf Utilities.IsDataType.Uinteger(main_element) Then
                        OPERANDS.Add(New ScriptElementOperandEntry(MyParent, DataType.UInteger, CUInt(main_element)))
                    ElseIf main_element.EndsWith("U") AndAlso Utilities.IsDataType.Uinteger(main_element.Substring(0, main_element.Length - 1)) Then
                        OPERANDS.Add(New ScriptElementOperandEntry(MyParent, DataType.UInteger, CUInt(main_element.Substring(0, main_element.Length - 1))))
                    ElseIf Utilities.IsDataType.String(main_element) Then
                        OPERANDS.Add(New ScriptElementOperandEntry(MyParent, DataType.String, Utilities.RemoveQuotes(main_element)))
                    ElseIf Utilities.IsDataType.Bool(main_element) Then
                        OPERANDS.Add(New ScriptElementOperandEntry(MyParent, DataType.Bool, CBool(main_element)))
                    ElseIf EC_ScriptEngine.IsDataArrayType(main_element) Then
                        Dim dr() As Byte = EC_ScriptEngine.DataArrayTypeToBytes(main_element)
                        OPERANDS.Add(New ScriptElementOperandEntry(MyParent, DataType.Data, dr))
                    ElseIf (main_element.ToUpper.StartsWith("0X") AndAlso Utilities.IsDataType.Hex(main_element)) Then
                        Dim d() As Byte = Utilities.Bytes.FromHexString(main_element)
                        If (d.Length > 4) Then
                            OPERANDS.Add(New ScriptElementOperandEntry(MyParent, DataType.Data, d))
                        Else
                            OPERANDS.Add(New ScriptElementOperandEntry(MyParent, DataType.UInteger, Utilities.Bytes.ToUInt32(d)))
                        End If
                    ElseIf main_element.ToUpper.Equals("NOTHING") Then
                        OPERANDS.Add(New ScriptElementOperandEntry(MyParent, ScriptElementDataType.Nothing))
                    ElseIf IsVariableArgument(main_element) Then
                        OPERANDS.Add(New ScriptElementOperandEntry(MyParent, ScriptElementDataType.Variable) With {.FUNC_NAME = main_element})
                    ElseIf main_element.Equals("") Then
                        Return New ParseResult("Unknown function or command: " & text_input)
                    Else
                        Return New ParseResult("Unknown function or command: " & main_element)
                    End If
                    If (Not text_input.Equals("")) Then
                        Dim oper_seperator As OperandOper = OperandOper.NOTSPECIFIED
                        If FeedOperator(text_input, oper_seperator) Then
                            OPERANDS.Add(New ScriptElementOperandEntry(MyParent, ScriptElementDataType.Operator) With {.Oper = oper_seperator})
                        Else
                            Return New ParseResult("Invalid operator")
                        End If
                    End If
                End If
            Loop
            Return New ParseResult() 'No Error
        End Function
        'For $1 $2 etc.
        Private Function IsVariableArgument(input As String) As Boolean
            If Not input.StartsWith("$") Then Return False
            input = input.Substring(1)
            If input = "" Then Return False
            If Not IsNumeric(input) Then Return False
            Return True
        End Function

        Private Function ParseFunctionInput(to_parse As String) As ScriptElementOperandEntry
            Dim new_fnc As New ScriptElementOperandEntry(MyParent, ScriptElementDataType.Function)
            Dim arguments As String = ""
            ParseToFunctionAndSub(to_parse, new_fnc.FUNC_NAME, new_fnc.FUNC_SUB, new_fnc.FUNC_IND, arguments)
            If (Not arguments.Equals("")) Then
                new_fnc.FUNC_ARGS = ParseArguments(arguments)
                If Me.ParsingResult.IsError Then Return Nothing
            End If
            Return new_fnc
        End Function

        Private Function ParseEventInput(to_parse As String) As ScriptElementOperandEntry
            Dim new_evnt As New ScriptElementOperandEntry(MyParent, ScriptElementDataType.Event)
            Dim arguments As String = ""
            ParseToFunctionAndSub(to_parse, new_evnt.FUNC_NAME, Nothing, Nothing, arguments)
            If (Not arguments = "") Then
                new_evnt.FUNC_ARGS = ParseArguments(arguments)
                If Me.ParsingResult.IsError Then Return Nothing
            End If
            Return new_evnt
        End Function

        Private Function ParseVarInput(to_parse As String) As ScriptElementOperandEntry
            Dim new_Var As New ScriptElementOperandEntry(MyParent, ScriptElementDataType.Variable)
            Dim var_name As String = ""
            Dim arguments As String = ""
            ParseToFunctionAndSub(to_parse, var_name, Nothing, Nothing, arguments)
            new_Var.FUNC_NAME = var_name
            If (Not arguments = "") Then
                new_Var.FUNC_ARGS = ParseArguments(arguments)
                If Me.ParsingResult.IsError Then Return Nothing
            End If
            Return new_Var
        End Function

        Private Function ParseArguments(argument_line As String) As ScriptElementOperand()
            Dim argument_list As New List(Of ScriptElementOperand)
            Dim arg_builder As New Text.StringBuilder
            Dim in_quote As Boolean = False
            Dim in_param As Boolean = False
            For i = 0 To argument_line.Length - 1
                Dim c As Char = CChar(argument_line.Substring(i, 1))
                arg_builder.Append(c)
                Dim add_clear As Boolean = False
                If in_quote Then
                    If c.Equals(""""c) Then in_quote = False
                ElseIf in_param Then
                    If c.Equals(")"c) Then in_param = False
                    If c.Equals(""""c) Then in_quote = True
                Else
                    Select Case c
                        Case "("c
                            in_param = True
                        Case """"c
                            in_quote = True
                        Case ","c
                            add_clear = True
                            arg_builder.Remove(arg_builder.Length - 1, 1) 'Remove last item
                    End Select
                End If
                If add_clear Then
                    Dim n As New ScriptElementOperand(MyParent)
                    Dim result As ParseResult = n.Parse(arg_builder.ToString)
                    If result.IsError Then

                        Return Nothing
                    End If
                    argument_list.Add(n) : arg_builder.Clear()
                End If
            Next
            If (arg_builder.Length > 0) Then
                Dim n As New ScriptElementOperand(MyParent)
                Me.ParsingResult = n.Parse(arg_builder.ToString)
                If Me.ParsingResult.IsError Then Return Nothing
                argument_list.Add(n)
            End If
            Return argument_list.ToArray()
        End Function

        Friend Function CompileToVariable(ByRef exit_task As ExitMode) As ScriptVariable
            Dim current_var As ScriptVariable = Nothing
            Dim current_oper As OperandOper = OperandOper.NOTSPECIFIED
            Dim arg_count As Integer = OPERANDS.Count
            Dim x As Integer = 0
            Do While (x < arg_count)
                If OPERANDS(x).EntryType = ScriptElementDataType.Operator Then
                    Return CreateError("Expected value to compute")
                End If
                Dim new_var As ScriptVariable = OPERANDS(x).Compile(exit_task)
                If new_var IsNot Nothing AndAlso new_var.Data.VarType = DataType.FncError Then Return new_var
                If (Not current_oper = OperandOper.NOTSPECIFIED) Then
                    current_var = CompileSVars(current_var, new_var, current_oper)
                Else
                    current_var = new_var
                End If
                x += 1 'increase pointer
                If (x < arg_count) Then 'There are more items
                    If Not OPERANDS(x).Oper = OperandOper.NOTSPECIFIED Then
                        current_oper = OPERANDS(x).Oper
                        x += 1
                        If Not (x < arg_count) Then
                            Return CreateError("Statement ended in an operand operation")
                        End If
                    Else
                        Return CreateError("Expected an operand operation")
                    End If
                End If
            Loop
            Return current_var
        End Function

        Public Overrides Function ToString() As String
            Dim str_builder As New Text.StringBuilder
            For Each item In OPERANDS
                str_builder.Append("(" & item.ToString & ")")
            Next
            Return str_builder.ToString.Trim
        End Function

    End Class

    Friend Class ScriptElementOperandEntry
        Private MyParent As Processor
        Public ReadOnly Property EntryType As ScriptElementDataType '[Data] [Operator] [SubItems] [Variable] [Function] [Event]
        Public Property Oper As OperandOper = OperandOper.NOTSPECIFIED '[ADD] [SUB] [MULT] [DIV] [AND] [OR] [S_LEFT] [S_RIGHT] [IS] [LESS_THAN] [GRT_THAN]
        Public Property SubOperands As ScriptElementOperand
        Public Property FUNC_NAME As String 'Name of the function, event or variable
        Public Property FUNC_SUB As String = "" 'Name of the function.sub
        Public Property FUNC_IND As String 'Index for a given function (integer or variable)
        Public Property FUNC_ARGS As ScriptElementOperand()
        Public Property FUNC_DATA As DataTypeObject = Nothing

        Sub New(oParent As Processor, entry_t As ScriptElementDataType)
            Me.EntryType = entry_t
            Me.MyParent = oParent
        End Sub

        Sub New(oParent As Processor, dt As DataType, dv As Object)
            Me.MyParent = oParent
            Me.EntryType = ScriptElementDataType.Data
            Me.FUNC_DATA = New DataTypeObject(dt, dv)
        End Sub

        Public Overrides Function ToString() As String
            Select Case EntryType
                Case ScriptElementDataType.Data
                    Select Case FUNC_DATA.VarType
                        Case DataType.Integer
                            Return CInt(Me.FUNC_DATA.Value).ToString()
                        Case DataType.UInteger
                            Return CUInt(Me.FUNC_DATA.Value).ToString()
                        Case DataType.String
                            Return """" & CStr(Me.FUNC_DATA.Value) & """"
                        Case DataType.Data
                            Return Utilities.Bytes.ToPaddedHexString(CType(Me.FUNC_DATA.Value, Byte()))
                        Case DataType.Bool
                            Select Case CBool(Me.FUNC_DATA.Value)
                                Case True
                                    Return "True"
                                Case False
                                    Return "False"
                            End Select
                        Case DataType.FncError
                            Return "Error: " & CStr(Me.FUNC_DATA.Value)
                    End Select
                Case ScriptElementDataType.Event
                    Return "Event: " & Me.FUNC_NAME
                Case ScriptElementDataType.Function
                    If Not Me.FUNC_SUB = "" Then
                        Return "Function: " & Me.FUNC_NAME & "." & Me.FUNC_SUB
                    Else
                        Return "Function: " & Me.FUNC_NAME
                    End If
                Case ScriptElementDataType.Variable
                    Return "Variable (" & Me.FUNC_NAME & ")"
                Case ScriptElementDataType.Operator
                    Select Case Me.Oper
                        Case OperandOper.ADD
                            Return "ADD Operator"
                        Case OperandOper.SUB
                            Return "SUB Operator"
                        Case OperandOper.MULT
                            Return "MULT Operator"
                        Case OperandOper.DIV
                            Return "DIV Operator"
                        Case OperandOper.AND
                            Return "AND Operator"
                        Case OperandOper.OR
                            Return "OR Operator"
                        Case OperandOper.S_LEFT
                            Return "<< Operator"
                        Case OperandOper.S_RIGHT
                            Return ">> Operator"
                        Case OperandOper.IS
                            Return "Is Operator"
                        Case OperandOper.LESS_THAN
                            Return "< Operator"
                        Case OperandOper.GRT_THAN
                            Return "> Operator"
                    End Select
                Case ScriptElementDataType.SubItems
                    Return "Sub Items: " & SubOperands.OPERANDS.Count
            End Select
            Return ""
        End Function

        Friend Function Compile(ByRef exit_task As ExitMode) As ScriptVariable
            Select Case EntryType
                Case ScriptElementDataType.Nothing
                    Return New ScriptVariable(MyParent.CurrentVars.GetNewName(), DataType.NULL)
                Case ScriptElementDataType.Data
                    Dim new_sv As New ScriptVariable(MyParent.CurrentVars.GetNewName(), Me.FUNC_DATA.VarType)
                    new_sv.Value = Me.FUNC_DATA.Value
                    Return new_sv
                Case ScriptElementDataType.Function
                    Dim fnc_params() As CmdPrm = Nothing
                    Dim fnc As [Delegate] = Nothing
                    If MyParent.CmdFunctions.GetScriptFunction(FUNC_NAME, FUNC_SUB, fnc_params, fnc) Then
                        Dim input_vars As New List(Of ScriptVariable)
                        If Me.FUNC_ARGS IsNot Nothing Then
                            For i = 0 To Me.FUNC_ARGS.Length - 1
                                Dim ret As ScriptVariable = Me.FUNC_ARGS(i).CompileToVariable(exit_task)
                                If ret.Data.VarType = DataType.FncError Then Return ret
                                If ret IsNot Nothing Then input_vars.Add(ret)
                            Next
                        End If
                        Dim args_var() As ScriptVariable = input_vars.ToArray
                        Dim result As ParseResult = CheckFunctionArguments(fnc_params, args_var)
                        If result.IsError Then Return CreateError(result.ErrorMsg)
                        Try
                            Dim func_index As Integer = 0
                            If IsNumeric(Me.FUNC_IND) Then
                                func_index = CInt(FUNC_IND)
                            ElseIf Utilities.IsDataType.HexString(Me.FUNC_IND) Then
                                func_index = Utilities.HexToInt(Me.FUNC_IND)
                            ElseIf MyParent.CurrentVars.IsVariable(Me.FUNC_IND) Then
                                Dim v As ScriptVariable = MyParent.CurrentVars.GetVariable(Me.FUNC_IND)
                                If v.Data.VarType = DataType.Integer Then
                                    func_index = CInt(MyParent.CurrentVars.GetVariable(Me.FUNC_IND).Value)
                                ElseIf v.Data.VarType = DataType.UInteger Then
                                    func_index = CInt(MyParent.CurrentVars.GetVariable(Me.FUNC_IND).Value)
                                Else
                                    Return CreateError("Index " & Me.FUNC_IND & " must be either an Integer or UInteger")
                                End If
                            Else
                                Return CreateError("Unable to evaluate index: " & Me.FUNC_IND)
                            End If
                            Dim dynamic_result As ScriptVariable = Nothing
                            Try
                                dynamic_result = CType(fnc.DynamicInvoke(args_var, func_index), ScriptVariable)
                            Catch ex As Exception
                                Return CreateError(FUNC_NAME & " function exception")
                            End Try
                            Return dynamic_result
                        Catch ex As Exception
                            Return CreateError("Error executing function: " & Me.FUNC_NAME)
                        End Try
                    Else
                        Return CreateError("Unknown function or sub procedure")
                    End If
                Case ScriptElementDataType.Event
                    Dim input_vars As New List(Of ScriptVariable)
                    If Me.FUNC_ARGS IsNot Nothing Then
                        For i = 0 To Me.FUNC_ARGS.Length - 1
                            Dim ret As ScriptVariable = Me.FUNC_ARGS(i).CompileToVariable(exit_task)
                            If ret.Data.VarType = DataType.FncError Then Return ret
                            If ret IsNot Nothing Then input_vars.Add(ret)
                        Next
                    End If
                    Dim se As ScriptEvent = MyParent.GetScriptEvent(Me.FUNC_NAME)
                    If se Is Nothing Then Return CreateError("Event does not exist: " & Me.FUNC_NAME)
                    Dim n_sv As ScriptVariable = MyParent.ExecuteScriptEvent(se, input_vars.ToArray, exit_task)
                    Return n_sv
                Case ScriptElementDataType.Variable
                    Dim n_sv As ScriptVariable = MyParent.CurrentVars.GetVariable(Me.FUNC_NAME)
                    If n_sv.Data.VarType = DataType.Data AndAlso Me.FUNC_ARGS IsNot Nothing AndAlso Me.FUNC_ARGS.Length = 1 Then
                        Return DataArray_GetValue(CType(n_sv.Value, Byte()), exit_task)
                    Else
                        Return n_sv
                    End If
                Case ScriptElementDataType.SubItems
                    Return SubOperands.CompileToVariable(exit_task)
            End Select
            Return Nothing
        End Function

        Private Function CheckFunctionArguments(fnc_params() As CmdPrm, ByRef my_vars() As ScriptVariable) As ParseResult
            Dim var_count As Integer = 0
            If my_vars Is Nothing OrElse my_vars.Length = 0 Then
                var_count = 0
            Else
                var_count = my_vars.Length
            End If
            If fnc_params Is Nothing AndAlso (Not var_count = 0) Then
                Return New ParseResult("Function " & Me.GetFuncString & ": arguments supplied but none are allowed")
            ElseIf fnc_params IsNot Nothing Then
                For i = 0 To fnc_params.Length - 1
                    Select Case fnc_params(i)
                        Case CmdPrm.Integer
                            If (i >= var_count) Then
                                Return New ParseResult("Function " & Me.GetFuncString & ": argument " & (i + 1) & " requires an Integer type parameter")
                            Else
                                If (my_vars(i).Data.VarType = DataType.UInteger) Then 'We can possibly auto convert data type
                                    Dim auto_con As UInt32 = CUInt(my_vars(i).Value)
                                    If auto_con <= Int32.MaxValue Then my_vars(i) = New ScriptVariable(my_vars(i).Name, DataType.Integer, CInt(auto_con))
                                End If
                                If Not my_vars(i).Data.VarType = DataType.Integer Then
                                    Return New ParseResult("Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs an Integer but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType))
                                End If
                            End If
                        Case CmdPrm.UInteger
                            If (i >= var_count) Then
                                Return New ParseResult("Function " & Me.GetFuncString & ": argument " & (i + 1) & " requires an UInteger type parameter")
                            Else
                                If (my_vars(i).Data.VarType = DataType.Integer) Then 'We can possibly auto convert data type
                                    Dim auto_con As Integer = CInt(my_vars(i).Value)
                                    If (auto_con >= 0) Then my_vars(i) = New ScriptVariable(my_vars(i).Name, DataType.UInteger, CUInt(auto_con))
                                End If
                                If (Not my_vars(i).Data.VarType = DataType.UInteger) Then
                                    Return New ParseResult("Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs an UInteger but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType))
                                End If
                            End If
                        Case CmdPrm.String
                            If (i >= var_count) Then
                                Return New ParseResult("Function " & Me.GetFuncString & ": argument " & (i + 1) & " requires a String type parameter")
                            Else
                                If Not my_vars(i).Data.VarType = DataType.String Then
                                    Return New ParseResult("Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs a String but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType))
                                End If
                            End If
                        Case CmdPrm.Data
                            If (i >= var_count) Then
                                Return New ParseResult("Function " & Me.GetFuncString & ": argument " & (i + 1) & " requires a Data type parameter")
                            Else
                                If my_vars(i).Data.VarType = DataType.Data Then
                                ElseIf my_vars(i).Data.VarType = DataType.UInteger Then
                                    Dim c As New ScriptVariable(my_vars(i).Name, DataType.Data)
                                    c.Value = Utilities.Bytes.FromUInt32(CUInt(my_vars(i).Value))
                                    my_vars(i) = c
                                Else
                                    Return New ParseResult("Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs an Data but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType))
                                End If
                            End If
                        Case CmdPrm.Bool
                            If (i >= var_count) Then
                                Return New ParseResult("Function " & Me.GetFuncString & ": argument " & (i + 1) & " requires a Bool type parameter")
                            Else
                                If Not my_vars(i).Data.VarType = DataType.Bool Then
                                    Return New ParseResult("Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs an Bool but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType))
                                End If
                            End If
                        Case CmdPrm.Any
                            If (i >= var_count) Then
                                Return New ParseResult("Function " & Me.GetFuncString & ": argument " & (i + 1) & " requires a parameter")
                            End If
                        Case CmdPrm.Integer_Optional
                            If Not (i >= var_count) Then 'Only check if we have supplied this argument
                                If (my_vars(i).Data.VarType = DataType.UInteger) Then 'We can possibly auto convert data type
                                    Dim auto_con As UInt32 = CUInt(my_vars(i).Value)
                                    If auto_con <= Int32.MaxValue Then my_vars(i) = New ScriptVariable(my_vars(i).Name, DataType.Integer, CInt(auto_con))
                                End If
                                If Not my_vars(i).Data.VarType = DataType.Integer Then
                                    Return New ParseResult("Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs an Integer but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType))
                                End If
                            End If
                        Case CmdPrm.UInteger_Optional
                            If Not (i >= var_count) Then 'Only check if we have supplied this argument
                                If (my_vars(i).Data.VarType = DataType.Integer) Then 'We can possibly auto convert data type
                                    Dim auto_con As Integer = CInt(my_vars(i).Value)
                                    If (auto_con >= 0) Then my_vars(i) = New ScriptVariable(my_vars(i).Name, DataType.UInteger, CUInt(auto_con))
                                End If
                                If Not my_vars(i).Data.VarType = DataType.UInteger Then
                                    Return New ParseResult("Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs an UInteger but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType))
                                End If
                            End If
                        Case CmdPrm.String_Optional
                            If Not (i >= var_count) Then 'Only check if we have supplied this argument
                                If Not my_vars(i).Data.VarType = DataType.String Then
                                    Return New ParseResult("Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs an String but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType))
                                End If
                            End If
                        Case CmdPrm.Data_Optional
                            If Not (i >= var_count) Then 'Only check if we have supplied this argument
                                If Not my_vars(i).Data.VarType = DataType.Data Then
                                    Return New ParseResult("Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs Data but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType))
                                End If
                            End If
                        Case CmdPrm.Bool_Optional
                            If Not (i >= var_count) Then 'Only check if we have supplied this argument
                                If Not my_vars(i).Data.VarType = DataType.Bool Then
                                    Return New ParseResult("Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs Bool but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType))
                                End If
                            End If
                        Case CmdPrm.Any_Optional
                    End Select
                Next
            End If
            Return New ParseResult
        End Function

        Friend Function GetFuncString() As String
            If String.IsNullOrEmpty(FUNC_SUB) Then
                Return Me.FUNC_NAME
            Else
                Return Me.FUNC_NAME & "." & Me.FUNC_SUB
            End If
        End Function

        Private Function DataArray_GetValue(DataArr() As Byte, ByRef exit_task As ExitMode) As ScriptVariable
            Try
                Dim ArrayIndex As Integer = 0
                Dim data_index_var As ScriptVariable = Me.FUNC_ARGS(0).CompileToVariable(exit_task)
                If data_index_var.Data.VarType = DataType.UInteger Then
                    ArrayIndex = CInt(CUInt(data_index_var.Value))
                ElseIf data_index_var.Data.VarType = DataType.Integer Then
                    ArrayIndex = CInt(data_index_var.Value)
                Else
                    Return CreateError("Error processing variable index value")
                End If
                Dim new_sv As New ScriptVariable(MyParent.CurrentVars.GetNewName(), DataType.UInteger) 'We will return Unit32 for 1 BYTE
                new_sv.Value = DataArr(ArrayIndex)
                Return new_sv
            Catch ex As Exception
                Return CreateError("Error processing variable index value")
            End Try
        End Function

    End Class

    Public Class ScriptFile
        Private MyParent As Processor
        Public TheScript As New List(Of ScriptLineElement)
        Public EventList As New List(Of String)

        Sub New()

        End Sub

        Public Sub Reset()
            TheScript.Clear()
        End Sub

        Friend Function LoadFile(oParent As Processor, lines() As String, ByRef ErrInd As Integer, ByRef ErrorMsg As String) As Boolean
            TheScript.Clear()
            MyParent = oParent
            Dim line_index(lines.Length - 1) As Integer
            For i = 0 To lines.Length - 1
                line_index(i) = i
            Next
            ProcessEvents(lines)
            Dim Result() As ScriptLineElement = ProcessText(lines, line_index, ErrInd, ErrorMsg)
            If (ErrorMsg = "") Then 'No Error
                For Each item In Result
                    TheScript.Add(item)
                Next
            Else
                Return False
            End If
            Return True 'No errors, all lines successfully parsed in
        End Function
        'Begins an initial process of the script and populates the EventList list
        Private Sub ProcessEvents(lines() As String)
            EventList.Clear()
            For Each line In lines
                Dim cmd_line As String = Utilities.RemoveComment(line.Replace(vbTab, " ")).Trim
                If cmd_line.ToUpper.StartsWith("CREATEEVENT") Then
                    If cmd_line.ToUpper.StartsWith("CREATEEVENT") Then cmd_line = cmd_line.Substring(11).Trim
                    Dim event_name As String = FeedParameter(cmd_line)
                    If Not (event_name = "") Then
                        EventList.Add(event_name)
                    End If
                End If
            Next
        End Sub

        Friend Function ProcessText(lines() As String, line_index() As Integer, ByRef ErrInd As Integer, ByRef ErrorMsg As String) As ScriptLineElement()
            If lines Is Nothing Then Return Nothing
            If Not lines.Length = line_index.Length Then Return Nothing
            For i = 0 To lines.Length - 1
                lines(i) = Utilities.RemoveComment(lines(i).Replace(vbTab, " ")).Trim() 'This is the initial formatting of each text line
            Next
            Dim line_pointer As Integer = 0
            Try
                Dim Processed As New List(Of ScriptLineElement)
                While (line_pointer < lines.Length)
                    Dim cmd_line As String = lines(line_pointer)
                    If Not String.IsNullOrEmpty(cmd_line) Then
                        If cmd_line.ToUpper.StartsWith("IF ") Then 'We are doing an if condition
                            Dim s As ScriptLineElement = CreateIfCondition(line_pointer, lines, line_index, ErrInd, ErrorMsg) 'Increments line pointer
                            If s Is Nothing OrElse Not ErrorMsg = "" Then Return Nothing
                            Processed.Add(s)
                        ElseIf cmd_line.ToUpper.StartsWith("FOR ") Then
                            Dim s As ScriptLineElement = CreateForLoop(line_pointer, lines, line_index, ErrInd, ErrorMsg) 'Increments line pointer
                            If s Is Nothing OrElse Not ErrorMsg = "" Then Return Nothing
                            Processed.Add(s)
                        ElseIf cmd_line.ToUpper.StartsWith("CREATEEVENT(") OrElse cmd_line.ToUpper.StartsWith("CREATEEVENT ") Then
                            Dim s As ScriptLineElement = CreateEvent(line_pointer, lines, line_index, ErrInd, ErrorMsg)
                            If s Is Nothing OrElse Not ErrorMsg = "" Then Return Nothing
                            Processed.Add(s)
                        ElseIf cmd_line.ToUpper.StartsWith("GOTO ") Then
                            Dim s As ScriptLineElement = CreateGoto(line_pointer, lines, line_index, ErrInd, ErrorMsg)
                            If s Is Nothing OrElse Not ErrorMsg = "" Then Return Nothing
                            Processed.Add(s)
                        ElseIf cmd_line.ToUpper.StartsWith("EXIT ") OrElse cmd_line.ToUpper = "EXIT" Then
                            Dim s As ScriptLineElement = CreateExit(line_pointer, lines, line_index, ErrInd, ErrorMsg)
                            If s Is Nothing OrElse Not ErrorMsg = "" Then Return Nothing
                            Processed.Add(s)
                        ElseIf cmd_line.ToUpper.StartsWith("RETURN ") Then
                            Dim s As ScriptLineElement = CreateReturn(line_pointer, lines, line_index, ErrInd, ErrorMsg)
                            If s Is Nothing OrElse Not ErrorMsg = "" Then Return Nothing
                            Processed.Add(s)
                        ElseIf cmd_line.ToUpper.EndsWith(":") AndAlso (cmd_line.IndexOf(" ") = -1) Then
                            Dim s As ScriptLineElement = CreateLabel(line_pointer, lines, line_index, ErrInd, ErrorMsg)
                            If s Is Nothing OrElse Not ErrorMsg = "" Then Return Nothing
                            Processed.Add(s)
                        Else
                            Dim s As New ScriptElement(MyParent)
                            s.INDEX = line_index(line_pointer)
                            Dim result As ParseResult = s.Parse(cmd_line, True)
                            If result.IsError Then
                                ErrorMsg = result.ErrorMsg
                                ErrInd = line_index(line_pointer)
                                Return Nothing
                            End If
                            If Not s.TARGET_NAME = "" Then 'This element creates a new variable
                                MyParent.CurrentVars.AddExpected(s.TARGET_NAME)
                            End If
                            Processed.Add(s)
                        End If
                    End If
                    line_pointer += 1
                End While
                Return Processed.ToArray
            Catch ex As Exception
                ErrInd = line_index(line_pointer)
                ErrorMsg = "General statement evaluation error"
                Return Nothing
            End Try
        End Function

        Friend Function CreateIfCondition(ByRef Pointer As Integer, lines() As String, ind() As Integer, ByRef ErrInd As Integer, ByRef ErrorMsg As String) As ScriptLineElement
            Dim this_if As New ScriptCondition(Me.MyParent) 'Also loads NOT modifier
            this_if.INDEX = ind(Pointer)
            Dim result As ParseResult = this_if.Parse(lines(Pointer))
            If result.IsError Then
                ErrInd = ind(Pointer)
                ErrorMsg = result.ErrorMsg
                Return Nothing
            End If
            If this_if.CONDITION Is Nothing Then
                ErrInd = Pointer
                ErrorMsg = "IF condition is not valid"
                Return Nothing
            End If
            Dim IFMain As New List(Of String)
            Dim IfElse As New List(Of String)
            Dim IFMain_Index As New List(Of Integer)
            Dim IfElse_Index As New List(Of Integer)
            Dim ElseTrigger As Boolean = False
            Dim EndIfTrigger As Boolean = False
            Dim level As Integer = 0
            While (Pointer < lines.Length)
                Dim eval As String = Utilities.RemoveComment(lines(Pointer).Replace(vbTab, " ")).Trim
                If (Not eval = "") Then
                    If eval.ToUpper.StartsWith("IF ") Then
                        level += 1
                    ElseIf eval.ToUpper.StartsWith("ENDIF") OrElse eval.ToUpper.StartsWith("END IF") Then
                        level -= 1
                        If (level = 0) Then EndIfTrigger = True : Exit While
                    ElseIf eval.ToUpper.StartsWith("ELSE") AndAlso level = 1 Then
                        If ElseTrigger Then
                            ErrInd = ind(Pointer)
                            ErrorMsg = "IF condition: duplicate ELSE statement"
                            Return Nothing
                        Else
                            ElseTrigger = True
                        End If
                    Else
                        If (Not ElseTrigger) Then
                            IFMain.Add(eval)
                            IFMain_Index.Add(ind(Pointer))
                        Else
                            IfElse.Add(eval)
                            IfElse_Index.Add(ind(Pointer))
                        End If
                    End If
                End If
                Pointer += 1
            End While
            If (Not EndIfTrigger) Then
                ErrInd = ind(Pointer)
                ErrorMsg = "IF condition: EndIf statement not present"
                Return Nothing
            End If
            this_if.IF_MAIN = ProcessText(IFMain.ToArray, IFMain_Index.ToArray, ErrInd, ErrorMsg)
            If Not ErrorMsg = "" Then Return Nothing
            this_if.IF_ELSE = ProcessText(IfElse.ToArray, IfElse_Index.ToArray, ErrInd, ErrorMsg)
            If Not ErrorMsg = "" Then Return Nothing
            Return this_if
        End Function

        Friend Function CreateForLoop(ByRef Pointer As Integer, lines() As String, ind() As Integer, ByRef ErrInd As Integer, ByRef ErrorMsg As String) As ScriptLineElement
            Dim this_for As New ScriptLoop(Me.MyParent) 'Also loads NOT modifier
            this_for.INDEX = ind(Pointer)
            Dim success As ParseResult = this_for.Parse(lines(Pointer))
            If success.IsError Then
                ErrInd = ind(Pointer)
                ErrorMsg = "FOR LOOP statement is not valid"
                Return Nothing
            End If
            Dim EndForTrigger As Boolean = False
            Dim level As Integer = 0
            Dim LoopMain As New List(Of String)
            Dim LoopMain_Index As New List(Of Integer)
            While (Pointer < lines.Length)
                Dim eval As String = Utilities.RemoveComment(lines(Pointer).Replace(vbTab, " ")).Trim
                If (Not eval = "") Then
                    If eval.ToUpper.StartsWith("FOR ") Then
                        level += 1
                        If Not level = 1 Then
                            LoopMain.Add(eval) : LoopMain_Index.Add(ind(Pointer))
                        End If
                    ElseIf eval.ToUpper.StartsWith("ENDFOR") OrElse eval.ToUpper.StartsWith("END FOR") Then
                        level -= 1
                        If (level = 0) Then EndForTrigger = True : Exit While
                        LoopMain.Add(eval) : LoopMain_Index.Add(ind(Pointer))
                    Else
                        LoopMain.Add(eval) : LoopMain_Index.Add(ind(Pointer))
                    End If
                End If
                Pointer += 1
            End While
            If (Not EndForTrigger) Then
                ErrInd = ind(Pointer)
                ErrorMsg = "FOR Loop: EndFor statement not present"
                Return Nothing
            End If
            Dim loopvar As New ScriptVariable(this_for.VAR_NAME, DataType.UInteger)
            MyParent.CurrentVars.SetVariable(loopvar)
            this_for.LOOP_MAIN = ProcessText(LoopMain.ToArray, LoopMain_Index.ToArray, ErrInd, ErrorMsg)
            If Not ErrorMsg = "" Then Return Nothing
            Return this_for
        End Function

        Friend Function CreateGoto(ByRef Pointer As Integer, lines() As String, ind() As Integer, ByRef ErrInd As Integer, ByRef ErrorMsg As String) As ScriptLineElement
            Dim this_goto As New ScriptGoto(Me.MyParent)
            this_goto.INDEX = ind(Pointer)
            Dim input As String = lines(Pointer)
            If input.ToUpper.StartsWith("GOTO ") Then input = input.Substring(5).Trim
            If (input = "") Then
                ErrInd = Pointer
                ErrorMsg = "GOTO statement is missing target label"
                Return Nothing
            End If
            this_goto.TO_LABEL = input
            Return this_goto
        End Function

        Friend Function CreateExit(ByRef Pointer As Integer, lines() As String, ind() As Integer, ByRef ErrInd As Integer, ByRef ErrorMsg As String) As ScriptLineElement
            Dim this_exit As New ScriptExit(Me.MyParent)
            this_exit.INDEX = ind(Pointer)
            this_exit.MODE = ExitMode.Leave
            Dim input As String = lines(Pointer)
            If input.ToUpper = "EXIT" Then Return this_exit
            If input.ToUpper.StartsWith("EXIT ") Then input = input.Substring(5).Trim
            Select Case input.ToUpper
                Case "SCRIPT"
                    this_exit.MODE = ExitMode.LeaveScript
                Case "EVENT"
                    this_exit.MODE = ExitMode.LeaveEvent
                Case Else
                    this_exit.MODE = ExitMode.Leave
            End Select
            Return this_exit
        End Function

        Friend Function CreateReturn(ByRef Pointer As Integer, lines() As String, ind() As Integer, ByRef ErrInd As Integer, ByRef ErrorMsg As String) As ScriptLineElement
            Dim this_return As New ScriptReturn(Me.MyParent)
            this_return.INDEX = ind(Pointer)
            Dim input As String = lines(Pointer)
            'Pointer += 1
            If input.ToUpper.StartsWith("RETURN ") Then input = input.Substring(7).Trim
            this_return.Parse(input)
            ErrorMsg = this_return.ERROR_MSG
            Return this_return
        End Function

        Friend Function CreateLabel(ByRef Pointer As Integer, lines() As String, ind() As Integer, ByRef ErrInd As Integer, ByRef ErrorMsg As String) As ScriptLineElement
            Dim label_this As New ScriptLabel(Me.MyParent)
            label_this.INDEX = ind(Pointer)
            Dim input As String = lines(Pointer)
            If input.ToUpper.EndsWith(":") Then input = input.Substring(0, input.Length - 1).Trim
            If (input = "") Then
                ErrInd = Pointer
                ErrorMsg = "Label statement is missing target label"
                Return Nothing
            End If
            label_this.NAME = input
            Return label_this
        End Function

        Friend Function CreateEvent(ByRef Pointer As Integer, lines() As String, ind() As Integer, ByRef ErrInd As Integer, ByRef ErrorMsg As String) As ScriptLineElement
            Dim this_ev As New ScriptEvent(Me.MyParent)
            this_ev.INDEX = ind(Pointer)
            Dim input As String = lines(Pointer)
            Pointer += 1
            If input.ToUpper.StartsWith("CREATEEVENT") Then input = input.Substring(11).Trim
            Dim event_name As String = FeedParameter(input)
            If (event_name = "") Then
                ErrInd = Pointer
                ErrorMsg = "CreateEvent statement is missing event name"
                Return Nothing
            End If
            this_ev.EVENT_NAME = event_name
            Dim EndEventTrigger As Boolean = False
            Dim EventBody As New List(Of String)
            Dim EventBody_Index As New List(Of Integer)
            While (Pointer < lines.Length)
                Dim eval As String = Utilities.RemoveComment(lines(Pointer).Replace(vbTab, " ")).Trim
                If (Not eval = "") Then
                    If eval.ToUpper.StartsWith("CREATEEVENT") Then
                        ErrInd = Pointer
                        ErrorMsg = "Error: CreateEvent statement within event"
                    ElseIf eval.ToUpper.StartsWith("ENDEVENT") OrElse eval.ToUpper.StartsWith("END EVENT") Then
                        EndEventTrigger = True : Exit While
                    Else
                        EventBody.Add(eval)
                        EventBody_Index.Add(ind(Pointer))
                    End If
                End If
                Pointer += 1
            End While
            If (Not EndEventTrigger) Then
                ErrInd = Pointer
                ErrorMsg = "CreateEvent: EndEvent statement not present"
                Return Nothing
            End If
            this_ev.Elements = ProcessText(EventBody.ToArray, EventBody_Index.ToArray, ErrInd, ErrorMsg)
            If Not ErrorMsg = "" Then Return Nothing
            Return this_ev
        End Function

    End Class


#Region "Enumerators"

    Public Enum ExitMode
        KeepRunning
        Leave
        LeaveEvent
        LeaveScript
        GotoLabel
    End Enum

    Public Enum TargetOper
        [NONE] 'We are not going to create a SV
        [EQ] '=
        [ADD] '+=
        [SUB] '-=
    End Enum
    'what to do with two variables/functions
    Public Enum OperandOper
        [NOTSPECIFIED]
        [ADD] 'Add (for integer), combine (for DATA or STRING)
        [SUB]
        [MULT]
        [DIV]
        [AND]
        [OR]
        [S_LEFT]
        [S_RIGHT]
        [IS]
        [LESS_THAN]
        [GRT_THAN]
    End Enum

    Public Enum CmdPrm
        [UInteger]
        [UInteger_Optional]
        [Integer]
        [Integer_Optional]
        [String]
        [String_Optional]
        [Data]
        [Data_Optional]
        [Bool]
        [Bool_Optional]
        [Any]
        [Any_Optional]
    End Enum

    Public Enum ScriptElementDataType
        [Nothing] 'Option not set
        [Data] 'Means the entry contains int,str,data,bool
        [Operator] 'Means this is a ADD/SUB/MULT/DIV etc.
        [SubItems] 'Means this contains a sub instance of entries
        [Variable]
        [Function]
        [Event]
    End Enum

    Public Enum DataType
        [NULL]
        [Integer]
        [UInteger]
        [String]
        [Data]
        [Bool]
        [FncError]
    End Enum

    Public Enum ScriptFileElementType
        [IF_CONDITION]
        [FOR_LOOP]
        [LABEL]
        [GOTO]
        [EVENT]
        [ELEMENT]
        [EXIT]
        [RETURN]
    End Enum

#End Region

    Public Class ScriptVariable
        Public ReadOnly Property Name As String = Nothing
        Public ReadOnly Property Data As DataTypeObject

        Sub New(new_name As String, defined_type As DataType, Optional default_value As Object = Nothing)
            Me.Name = new_name
            Me.Data = New DataTypeObject(defined_type, default_value)
        End Sub

        Sub New(new_name As String, data As DataTypeObject)
            Me.Name = new_name
        End Sub

        Public Property Value() As Object
            Get
                Return Me.Data.Value
            End Get
            Set(value As Object)
                Me.Data.Value = value
            End Set
        End Property

        Public Overrides Function ToString() As String
            Return Me.Name & " = " & Me.Value.ToString
        End Function

    End Class

    Public Class DataTypeObject
        Public ReadOnly Property VarType As DataType

        Private InternalData() As Byte 'This holds the data for this variable

        Sub New(defined_type As DataType, default_value As Object)
            Me.VarType = defined_type
            Me.Value = default_value
        End Sub

        Public Property Value() As Object
            Get
                Select Case Me.VarType
                    Case DataType.Integer
                        Return Utilities.Bytes.ToInt32(Me.InternalData)
                    Case DataType.UInteger
                        Return Utilities.Bytes.ToUInt32(Me.InternalData)
                    Case DataType.String
                        Return Utilities.Bytes.ToChrString(Me.InternalData)
                    Case DataType.Bool
                        If Me.InternalData(0) = 1 Then
                            Return True
                        ElseIf Me.InternalData(0) = 2 Then
                            Return False
                        End If
                        Return False
                    Case DataType.Data
                        Return InternalData
                    Case DataType.FncError
                        Return Utilities.Bytes.ToChrString(Me.InternalData)
                    Case Else
                        Return Nothing
                End Select
            End Get
            Set(new_value As Object)
                Select Case Me.VarType
                    Case DataType.Integer
                        Me.InternalData = Utilities.Bytes.FromInt32(CInt(new_value))
                    Case DataType.UInteger
                        Me.InternalData = Utilities.Bytes.FromUInt32(CUInt(new_value))
                    Case DataType.String
                        Me.InternalData = Utilities.Bytes.FromChrString(CStr(new_value))
                    Case DataType.Bool
                        ReDim InternalData(0)
                        If CBool(new_value) Then
                            InternalData(0) = 1
                        Else
                            InternalData(0) = 2
                        End If
                    Case DataType.Data
                        Me.InternalData = CType(new_value, Byte())
                    Case DataType.FncError
                        Me.InternalData = Utilities.Bytes.FromChrString(CStr(new_value))
                End Select
            End Set
        End Property

    End Class


#Region "ScriptLineElements"

    Public Class ScriptVariableManager
        Private MyVariables As New List(Of ScriptVariable)

        Sub New()

        End Sub

        Public Sub Clear()
            MyVariables.Clear()
        End Sub

        Friend Function IsVariable(input As String) As Boolean
            Dim var_name As String = ""
            ParseToFunctionAndSub(input, var_name, Nothing, Nothing, Nothing)
            For Each item In MyVariables
                If item.Name IsNot Nothing AndAlso Not item.Name = "" Then
                    If item.Name.ToUpper = var_name.ToUpper Then Return True
                End If
            Next
            Return False
        End Function

        Friend Function GetVariable(var_name As String) As ScriptVariable
            Dim var_name_to_find As String = var_name.ToUpper()
            For Each item In MyVariables
                If item.Name.ToUpper().Equals(var_name_to_find) Then Return item
            Next
            Return Nothing
        End Function

        Friend Function SetVariable(input_var As ScriptVariable) As Boolean
            Dim var_name_to_find As String = input_var.Name.ToUpper()
            If var_name_to_find Is Nothing OrElse var_name_to_find.Equals("") Then Return False
            For i = 0 To MyVariables.Count - 1
                If MyVariables(i).Name.ToUpper().Equals(var_name_to_find) Then
                    MyVariables(i) = input_var
                    Return True
                End If
            Next
            MyVariables.Add(input_var)
            Return True
        End Function

        Friend Function ClearVariable(name As String) As Boolean
            For i = 0 To MyVariables.Count - 1
                If MyVariables(i).Name IsNot Nothing AndAlso Not MyVariables(i).Name = "" Then
                    If MyVariables(i).Name.ToUpper = name.ToUpper Then
                        MyVariables.RemoveAt(i)
                        Return True
                    End If
                End If
            Next
            Return False
        End Function

        Friend Function GetValue(var_name As String) As Object
            Dim sv As ScriptVariable = GetVariable(var_name)
            Return sv.Value
        End Function

        Friend Function GetNewName() As String
            Dim Found As Boolean = False
            Dim new_name As String = ""
            Dim counter As Integer = 1
            Do
                new_name = "$t" & counter
                Dim sv As ScriptVariable = GetVariable(new_name)
                If sv Is Nothing Then Found = True
                counter += 1
            Loop While Not Found
            Return new_name
        End Function
        'This tells our pre-processor that a value is an expected variable
        Friend Sub AddExpected(name As String)
            Me.ClearVariable(name)
            Me.MyVariables.Add(New ScriptVariable(name, DataType.NULL))
        End Sub

    End Class

    Public MustInherit Class ScriptLineElement
        Friend MyParent As Processor

        Public Property INDEX As Integer 'This is the line index of this element
        Public Property ElementType As ScriptFileElementType '[IF_CONDITION] [FOR_LOOP] [LABEL] [GOTO] [EVENT] [ELEMENT]

        Sub New(oParent As EC_ScriptEngine.Processor)
            MyParent = oParent
        End Sub

    End Class

    Friend Class ScriptElement
        Inherits ScriptLineElement

        Friend Property TARGET_OPERATION As TargetOper = TargetOper.NONE 'What to do with the compiled variable
        Friend Property TARGET_NAME As String = "" 'This is the name of the variable to create
        Friend Property TARGET_INDEX As Integer = -1 'For DATA arrays, this is the index within the array
        Friend Property TARGET_VAR As String = "" 'Instead of INDEX, a variable (int) can be used instead

        Private OPERLIST As ScriptElementOperand '(Element)(+/-)(Element) etc.

        Sub New(oParent As Processor)
            MyBase.New(oParent)
            MyBase.ElementType = ScriptFileElementType.ELEMENT
        End Sub

        Friend Function Parse(to_parse As String, parse_target As Boolean) As ParseResult
            to_parse = to_parse.Trim
            If parse_target Then
                Dim result As ParseResult = LoadTarget(to_parse)
                If result.IsError Then Return result
            End If
            Me.OPERLIST = New ScriptElementOperand(MyBase.MyParent)
            Return Me.OPERLIST.Parse(to_parse)
        End Function
        'This parses the initial string to check for a var assignment
        Private Function LoadTarget(ByRef to_parse As String) As ParseResult
            Dim str_out As String = ""
            For i = 0 To to_parse.Length - 1
                If ((to_parse.Length - i) > 2) AndAlso to_parse.Substring(i, 2) = "==" Then 'This is a compare
                    Return New ParseResult
                ElseIf ((to_parse.Length - i) > 2) AndAlso to_parse.Substring(i, 2) = "+=" Then
                    TARGET_OPERATION = TargetOper.ADD
                    TARGET_NAME = str_out.Trim
                    to_parse = to_parse.Substring(i + 2).Trim
                    Return New ParseResult
                ElseIf ((to_parse.Length - i) > 2) AndAlso to_parse.Substring(i, 2) = "-=" Then
                    TARGET_OPERATION = TargetOper.SUB
                    TARGET_NAME = str_out.Trim
                    to_parse = to_parse.Substring(i + 2).Trim
                    Return New ParseResult
                ElseIf to_parse.Substring(i, 1) = "=" Then
                    TARGET_OPERATION = TargetOper.EQ
                    Dim input As String = str_out.Trim
                    Dim arg As String = ""
                    ParseToFunctionAndSub(input, TARGET_NAME, Nothing, Nothing, arg)
                    to_parse = to_parse.Substring(i + 1).Trim
                    If (Not arg = "") Then
                        If IsNumeric(arg) Then
                            TARGET_INDEX = CInt(arg)
                        ElseIf Utilities.IsDataType.HexString(arg) Then
                            TARGET_INDEX = Utilities.HexToInt(arg) 'Fixed INDEX
                        ElseIf MyParent.CurrentVars.IsVariable(arg) AndAlso MyParent.CurrentVars.GetVariable(arg).Data.VarType = DataType.UInteger Then
                            TARGET_INDEX = -1
                            TARGET_VAR = arg
                        Else
                            Return New ParseResult("Target index must be able to evaluate to an integer")
                        End If
                    End If
                    Return New ParseResult
                ElseIf to_parse.Substring(i, 1) = "." Then
                    Return New ParseResult
                ElseIf to_parse.Substring(i, 1) = """" Then
                    Return New ParseResult
                ElseIf to_parse.Substring(i, 1) = ">" Then
                    Return New ParseResult
                ElseIf to_parse.Substring(i, 1) = "<" Then
                    Return New ParseResult
                ElseIf to_parse.Substring(i, 1) = "+" Then
                    Return New ParseResult
                ElseIf to_parse.Substring(i, 1) = "-" Then
                    Return New ParseResult
                ElseIf to_parse.Substring(i, 1) = "/" Then
                    Return New ParseResult
                ElseIf to_parse.Substring(i, 1) = "*" Then
                    Return New ParseResult
                ElseIf to_parse.Substring(i, 1) = "&" Then
                    Return New ParseResult
                ElseIf to_parse.Substring(i, 1) = "|" Then
                    Return New ParseResult
                Else
                    str_out &= to_parse.Substring(i, 1)
                End If
            Next
            Return New ParseResult
        End Function

        Friend Function Compile(ByRef exit_task As ExitMode) As ScriptVariable
            Return OPERLIST.CompileToVariable(exit_task)
        End Function

        Public Overrides Function ToString() As String
            If TARGET_NAME = "" Then
                Return OPERLIST.ToString
            Else
                Select Case Me.TARGET_OPERATION
                    Case TargetOper.EQ
                        Return TARGET_NAME & " = " & OPERLIST.ToString
                    Case TargetOper.ADD
                        Return TARGET_NAME & " += " & OPERLIST.ToString
                    Case TargetOper.SUB
                        Return TARGET_NAME & " -= " & OPERLIST.ToString
                End Select
            End If
            Return "[ELEMENT]"
        End Function

    End Class

    Friend Class ScriptCondition
        Inherits ScriptLineElement

        Public Property CONDITION As ScriptElement
        Public Property NOT_MODIFIER As Boolean = False

        Public IF_MAIN() As ScriptLineElement 'Elements to execute if condition is true
        Public IF_ELSE() As ScriptLineElement 'And if FALSE 

        Sub New(oParent As Processor)
            MyBase.New(oParent)
            MyBase.ElementType = ScriptFileElementType.IF_CONDITION
        End Sub

        Friend Function Parse(input As String) As ParseResult
            Try
                If input.ToUpper.StartsWith("IF ") Then input = input.Substring(3).Trim
                Me.NOT_MODIFIER = False 'Indicates the not modifier is being used
                If input.ToUpper.StartsWith("NOT") Then
                    Me.NOT_MODIFIER = True
                    input = input.Substring(3).Trim
                End If
                CONDITION = New ScriptElement(MyBase.MyParent)
                Return CONDITION.Parse(input, False)
            Catch ex As Exception
                Return New ParseResult("Exception in If Condition")
            End Try
        End Function

        Public Overrides Function ToString() As String
            Return "IF CONDITION (" & CONDITION.ToString & ")"
        End Function

    End Class

    Friend Class ScriptLoop
        Inherits ScriptLineElement
        Friend Property VAR_NAME As String 'This is the name of the variable
        Friend Property START_IND As UInt32 = 0
        Friend Property END_IND As UInt32 = 0
        Friend Property STEP_VAL As UInt32 = 1

        Public LOOP_MAIN() As ScriptLineElement

        Private LOOPSTART_OPER As ScriptElementOperand 'The argument for the first part (pre TO)
        Private LOOPSTOP_OPER As ScriptElementOperand 'Argument for the stop part (post TO)

        Sub New(oParent As Processor)
            MyBase.New(oParent)
            MyBase.ElementType = ScriptFileElementType.FOR_LOOP
        End Sub

        Friend Function Parse(next_part As String) As ParseResult
            Try
                If next_part.ToUpper.StartsWith("FOR ") Then next_part = next_part.Substring(4).Trim
                If next_part.StartsWith("(") AndAlso next_part.EndsWith(")") Then
                    next_part = next_part.Substring(1, next_part.Length - 2)
                    Me.VAR_NAME = FeedWord(next_part, {"="})
                    If Me.VAR_NAME = "" Then Return New ParseResult("For Loop syntax is invalid")
                    next_part = next_part.Substring(1).Trim
                    Dim first_part As String = FeedElement(next_part)
                    next_part = next_part.Trim
                    If next_part = "" Then Return New ParseResult("For Loop syntax is invalid")
                    Dim to_part As String = FeedElement(next_part)
                    next_part = next_part.Trim
                    If next_part = "" Then Return New ParseResult("For Loop syntax is invalid")
                    If Not to_part.ToUpper = "TO" Then Return New ParseResult("For Loop syntax is invalid")
                    LOOPSTART_OPER = New ScriptElementOperand(MyBase.MyParent)
                    LOOPSTOP_OPER = New ScriptElementOperand(MyBase.MyParent)
                    Dim p1 As ParseResult = LOOPSTART_OPER.Parse(first_part)
                    Dim p2 As ParseResult = LOOPSTOP_OPER.Parse(next_part)
                    If p1.IsError Then Return p1
                    If p2.IsError Then Return p2
                    Return New ParseResult() 'Should we return an error?
                Else
                    Return New ParseResult("For Loop syntax is invalid")
                End If
            Catch ex As Exception
                Return New ParseResult("Exception in For Loop")
            End Try
        End Function
        'Compiles the FROM TO variables
        Friend Function Evaluate() As Boolean
            Try
                Dim sv1 As ScriptVariable = LOOPSTART_OPER.CompileToVariable(Nothing)
                Dim sv2 As ScriptVariable = LOOPSTOP_OPER.CompileToVariable(Nothing)
                If sv1 Is Nothing Then Return False
                If sv2 Is Nothing Then Return False
                If Not sv1.Data.VarType = DataType.UInteger Then Return False
                If Not sv2.Data.VarType = DataType.UInteger Then Return False
                Me.START_IND = CUInt(sv1.Value)
                Me.END_IND = CUInt(sv2.Value)
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Overrides Function ToString() As String
            Return "FOR LOOP (" & VAR_NAME & " = " & START_IND & " to " & END_IND & ") STEP " & STEP_VAL
        End Function

    End Class

    Friend Class ScriptLabel
        Inherits ScriptLineElement

        Sub New(oParent As Processor)
            MyBase.New(oParent)
            MyBase.ElementType = ScriptFileElementType.LABEL
        End Sub

        Friend Property NAME As String

        Public Overrides Function ToString() As String
            Return "LABEL: " & NAME
        End Function

    End Class

    Friend Class ScriptGoto
        Inherits ScriptLineElement

        Friend Property TO_LABEL As String

        Sub New(oParent As Processor)
            MyBase.New(oParent)
            MyBase.ElementType = ScriptFileElementType.GOTO
        End Sub

        Public Overrides Function ToString() As String
            Return "GOTO: " & TO_LABEL
        End Function

    End Class

    Friend Class ScriptExit
        Inherits ScriptLineElement

        Friend Property MODE As ExitMode

        Sub New(oParent As Processor)
            MyBase.New(oParent)
            MyBase.ElementType = ScriptFileElementType.EXIT
        End Sub

        Public Overrides Function ToString() As String
            Select Case MODE
                Case ExitMode.KeepRunning
                    Return "KEEP-ALIVE"
                Case ExitMode.Leave
                    Return "EXIT (LEAVE)"
                Case ExitMode.LeaveEvent
                    Return "EXIT (EVENT)"
                Case ExitMode.LeaveScript
                    Return "EXIT (SCRIPT)"
                Case Else
                    Return ""
            End Select
        End Function

    End Class

    Friend Class ScriptReturn
        Inherits ScriptLineElement

        Public ReadOnly Property HAS_ERROR As Boolean
            Get
                If ERROR_MSG.Equals("") Then Return False Else Return True
            End Get
        End Property
        Public Property ERROR_MSG As String = ""

        Private OPERLIST As ScriptElementOperand

        Sub New(oParent As Processor)
            MyBase.New(oParent)
            MyBase.ElementType = ScriptFileElementType.RETURN
        End Sub

        Friend Function Parse(to_parse As String) As ParseResult
            to_parse = to_parse.Trim
            OPERLIST = New ScriptElementOperand(MyBase.MyParent)
            Return OPERLIST.Parse(to_parse)
        End Function

        Friend Function Compile(ByRef exit_task As ExitMode) As ScriptVariable
            Return OPERLIST.CompileToVariable(exit_task)
        End Function

        Public Overrides Function ToString() As String
            Return "RETURN " & OPERLIST.ToString
        End Function

    End Class

    Friend Class ScriptEvent
        Inherits ScriptLineElement

        Sub New(oParent As Processor)
            MyBase.New(oParent)
            MyBase.ElementType = ScriptFileElementType.EVENT
        End Sub

        Friend Property EVENT_NAME As String
        Friend Elements() As ScriptLineElement

        Public Overrides Function ToString() As String
            Return "SCRIPT EVENT: " & EVENT_NAME
        End Function

    End Class

#End Region

    Friend Module Tools

        Friend Function CreateError(error_msg As String) As ScriptVariable
            Return New ScriptVariable("ERROR", DataType.FncError, error_msg)
        End Function

        Friend Function GetDataTypeString(input As DataType) As String
            Select Case input
                Case DataType.Integer
                    Return "Integer"
                Case DataType.[UInteger]
                    Return "UInteger"
                Case DataType.[String]
                    Return "String"
                Case DataType.[Data]
                    Return "Data"
                Case DataType.[Bool]
                    Return "Bool"
                Case Else
                    Return "NotDefined"
            End Select
        End Function

        Friend Function IsDataArrayType(input As String) As Boolean
            Try
                If input.IndexOf(";") = -1 Then Return False
                If input.EndsWith(";") Then input = input.Substring(0, input.Length - 1)
                Dim t() As String = input.Split(";"c)
                For Each item In t
                    If Not Utilities.IsDataType.Hex(item) Then Return False
                    Dim t2 As UInt32 = Utilities.HexToUInt(item)
                    If (t2 > 255) Then Return False
                Next
                Return True
            Catch ex As Exception
            End Try
            Return False
        End Function

        Friend Function DataArrayTypeToBytes(input As String) As Byte()
            If Not IsDataArrayType(input) Then Return Nothing
            Try
                If input.EndsWith(";") Then input = input.Substring(0, input.Length - 1)
                Dim d As New List(Of Byte)
                Dim t() As String = input.Split(";"c)
                For Each item In t
                    Dim t2 As UInt32 = Utilities.HexToUInt(item)
                    d.Add(CByte(Utilities.HexToUInt(item) And 255))
                Next
                Return d.ToArray
            Catch ex As Exception
            End Try
            Return Nothing
        End Function

        Friend Function FeedParameter(ByRef input As String) As String
            If Not input.StartsWith("(") Then Return ""
            Dim strout As String = ""
            Dim counter As Integer = 0
            Dim level As Integer = 0
            Dim is_in_string As Boolean = False
            For i = 0 To input.Length - 1
                counter += 1
                Dim c As Char = CChar(input.Substring(i, 1))
                strout &= c
                If is_in_string AndAlso c = """" Then
                    is_in_string = False
                ElseIf c = """" Then
                    is_in_string = True
                Else
                    If c = "(" Then level = level + 1
                    If c = ")" Then level = level - 1
                    If c = ")" And level = 0 Then Exit For
                End If
            Next
            input = Mid(input, counter + 1).TrimStart
            Return Mid(strout, 2, strout.Length - 2).Trim
        End Function
        'This feeds the input string up to a char specified in the stop_char array
        Friend Function FeedWord(ByRef input As String, stop_chars() As String) As String
            Dim first_index As Integer = input.Length
            For Each c As String In stop_chars
                Dim i As Integer = input.IndexOf(c)
                If (i > -1) Then first_index = Math.Min(first_index, i)
            Next
            Dim output As String = input.Substring(0, first_index).Trim
            input = input.Substring(first_index).Trim
            Return output
        End Function

        Friend Function FeedString(ByRef objline As String) As String
            If Not objline.StartsWith("""") Then Return ""
            Dim counter As Integer = 0
            For Each c As Char In objline
                If c = """"c And Not counter = 0 Then Exit For
                counter += 1
            Next
            Dim InsideParam As String = objline.Substring(1, counter - 1)
            objline = objline.Substring(counter + 1).TrimStart
            Return InsideParam
        End Function
        'Returns the first word,function, etc. and mutates the input object
        Friend Function FeedElement(ByRef objline As String) As String
            Dim PARAM_LEVEL As Integer = 0
            Dim IN_STRING As Boolean = False
            Dim x As Integer = 0
            Do Until x = objline.Length
                Dim pull As Char = objline.Chars(x)
                If IN_STRING Then
                    If pull = """"c Then IN_STRING = False
                ElseIf pull = """"c Then
                    IN_STRING = True
                ElseIf pull = "("c Then
                    PARAM_LEVEL += 1
                ElseIf pull = ")"c Then
                    PARAM_LEVEL -= 1
                    If PARAM_LEVEL = -1 Then Return "" 'ERROR
                    If PARAM_LEVEL = 0 And ((x + 1 = objline.Length) OrElse Not objline.Chars(x + 1).Equals("."c)) Then
                        x += 1
                        Exit Do
                    End If
                ElseIf PARAM_LEVEL = 0 Then
                    If pull = "="c Then Exit Do
                    If pull = "+"c Then Exit Do
                    If pull = "-"c Then
                        If Not (x = 0 AndAlso objline.Length > 1 AndAlso IsNumeric(objline.Chars(x + 1))) Then
                            Exit Do
                        End If
                    End If
                    If pull = "*"c Then Exit Do
                    If pull = "/"c Then Exit Do
                    If pull = "<"c Then Exit Do
                    If pull = ">"c Then Exit Do
                    If pull = "&"c Then Exit Do
                    If pull = "|"c Then Exit Do
                    If pull = " "c Then Exit Do
                End If
                x += 1
            Loop
            If x = 0 Then Return ""
            Dim NewElement As String = objline.Substring(0, x)
            objline = objline.Substring(x).TrimStart()
            Return NewElement
        End Function

        Friend Function FeedOperator(ByRef text_input As String, ByRef sel_operator As OperandOper) As Boolean
            If text_input.StartsWith("+") Then 'Valid for string, data, and int
                sel_operator = OperandOper.ADD
                text_input = text_input.Substring(1).TrimStart
            ElseIf text_input.StartsWith("-") Then
                sel_operator = OperandOper.SUB
                text_input = text_input.Substring(1).TrimStart
            ElseIf text_input.StartsWith("/") Then
                sel_operator = OperandOper.DIV
                text_input = text_input.Substring(1).TrimStart
            ElseIf text_input.StartsWith("*") Then
                sel_operator = OperandOper.MULT
                text_input = text_input.Substring(1).TrimStart
            ElseIf text_input.StartsWith("&") Then
                sel_operator = OperandOper.AND
                text_input = text_input.Substring(1).TrimStart
            ElseIf text_input.StartsWith("|") Then
                sel_operator = OperandOper.OR
                text_input = text_input.Substring(1).TrimStart
            ElseIf text_input.StartsWith("<<") Then
                sel_operator = OperandOper.S_LEFT
                text_input = text_input.Substring(2).TrimStart
            ElseIf text_input.StartsWith(">>") Then
                sel_operator = OperandOper.S_RIGHT
                text_input = text_input.Substring(2).TrimStart
            ElseIf text_input.StartsWith("==") Then
                sel_operator = OperandOper.IS
                text_input = text_input.Substring(2).TrimStart
            ElseIf text_input.StartsWith("<") Then
                sel_operator = OperandOper.LESS_THAN
                text_input = text_input.Substring(1).TrimStart
            ElseIf text_input.StartsWith(">") Then
                sel_operator = OperandOper.GRT_THAN
                text_input = text_input.Substring(1).TrimStart
            Else
                Return False
            End If
            Return True
        End Function
        'Compiles two variables, returns a string if there is an error
        Friend Function CompileSVars(var1 As ScriptVariable, var2 As ScriptVariable, oper As OperandOper) As ScriptVariable
            Try
                If oper = OperandOper.AND Or oper = OperandOper.OR Then
                    If Not var1.Data.VarType = DataType.Bool Then
                        Return CreateError("OR / AND bitwise operators only valid for Bool data types")
                    End If
                End If
                Select Case oper
                    Case OperandOper.ADD
                        Select Case var1.Data.VarType
                            Case DataType.Integer
                                If var2.Data.VarType = DataType.Integer Then
                                    Return New ScriptVariable("RESULT", DataType.Integer, CInt(var1.Value) + CInt(var2.Value))
                                ElseIf var2.Data.VarType = DataType.UInteger Then
                                    Return New ScriptVariable("RESULT", DataType.Integer, CInt(var1.Value) + CUInt(var2.Value))
                                Else
                                    Return CreateError("Operand data type mismatch")
                                End If
                            Case DataType.UInteger
                                If var2.Data.VarType = DataType.UInteger Then
                                    Return New ScriptVariable("RESULT", DataType.UInteger, CUInt(var1.Value) + CUInt(var2.Value))
                                ElseIf var2.Data.VarType = DataType.UInteger Then
                                    Return New ScriptVariable("RESULT", DataType.UInteger, CUInt(var1.Value) + CInt(var2.Value))
                                Else
                                    Return CreateError("Operand data type mismatch")
                                End If
                            Case DataType.String
                                Dim new_result As New ScriptVariable("RESULT", DataType.String)
                                If var2.Data.VarType = DataType.UInteger Then
                                    new_result.Value = CStr(var1.Value) & CUInt(var2.Value).ToString
                                ElseIf var2.Data.VarType = DataType.String Then
                                    new_result.Value = CStr(var1.Value) & CStr(var2.Value)
                                Else
                                    Return CreateError("Operand data type mismatch")
                                End If
                                Return new_result
                            Case DataType.Data
                                Dim new_result As New ScriptVariable("RESULT", DataType.Data)
                                If Not var1.Data.VarType = var2.Data.VarType Then
                                    Return CreateError("Operand data type mismatch")
                                End If
                                Dim data1() As Byte = CType(var1.Value, Byte())
                                Dim data2() As Byte = CType(var2.Value, Byte())
                                Dim new_size As Integer = data1.Length + data2.Length
                                Dim new_data(new_size) As Byte
                                Array.Copy(data1, 0, new_data, 0, data1.Length)
                                Array.Copy(data2, 0, new_data, data1.Length, data2.Length)
                                new_result.Value = new_data
                                Return new_result
                            Case DataType.Bool
                                Return CreateError("Add operand not valid for Bool data type")
                        End Select
                    Case OperandOper.SUB
                        Select Case var1.Data.VarType
                            Case DataType.Integer
                                If var2.Data.VarType = DataType.Integer Then
                                    Return New ScriptVariable("RESULT", DataType.Integer, CInt(var1.Value) - CInt(var2.Value))
                                ElseIf var2.Data.VarType = DataType.UInteger Then
                                    Return New ScriptVariable("RESULT", DataType.Integer, CInt(var1.Value) - CUInt(var2.Value))
                                Else
                                    Return CreateError("Operand data type mismatch")
                                End If
                            Case DataType.UInteger
                                If var2.Data.VarType = DataType.UInteger Then
                                    Return New ScriptVariable("RESULT", DataType.UInteger, CUInt(var1.Value) - CUInt(var2.Value))
                                ElseIf var2.Data.VarType = DataType.UInteger Then
                                    Return New ScriptVariable("RESULT", DataType.UInteger, CUInt(var1.Value) - CInt(var2.Value))
                                Else
                                    Return CreateError("Operand data type mismatch")
                                End If
                            Case Else
                                Dim new_result As New ScriptVariable("RESULT", DataType.UInteger)
                                If Not var1.Data.VarType = var2.Data.VarType Then Return CreateError("Operand data type mismatch")
                                If (Not var1.Data.VarType = DataType.UInteger) Then Return CreateError("Subtract operand only valid for Integer data type")
                                new_result.Value = CUInt(var1.Value) - CUInt(var2.Value)
                                Return new_result
                        End Select
                    Case OperandOper.DIV
                        If Not var1.Data.VarType = var2.Data.VarType Then Return CreateError("Operand data type mismatch")
                        If var1.Data.VarType = DataType.Integer Then
                            Dim new_result As New ScriptVariable("RESULT", DataType.Integer)
                            new_result.Value = CInt(var1.Value) / CInt(var2.Value)
                            Return new_result
                        ElseIf var2.Data.VarType = DataType.UInteger Then
                            Dim new_result As New ScriptVariable("RESULT", DataType.UInteger)
                            new_result.Value = CUInt(var1.Value) / CUInt(var2.Value)
                            Return new_result
                        Else
                            Return CreateError("Division operand only valid for Integer or UInteger data type")
                        End If
                    Case OperandOper.MULT
                        If Not var1.Data.VarType = var2.Data.VarType Then Return CreateError("Operand data type mismatch")
                        If var1.Data.VarType = DataType.Integer Then
                            Dim new_result As New ScriptVariable("RESULT", DataType.Integer)
                            new_result.Value = CInt(var1.Value) * CInt(var2.Value)
                            Return new_result
                        ElseIf var2.Data.VarType = DataType.UInteger Then
                            Dim new_result As New ScriptVariable("RESULT", DataType.UInteger)
                            new_result.Value = CUInt(var1.Value) * CUInt(var2.Value)
                            Return new_result
                        Else
                            Return CreateError("Mulitple operand only valid for Integer or UInteger data type")
                        End If
                    Case OperandOper.S_LEFT
                        Dim new_result As New ScriptVariable("RESULT", DataType.Integer)
                        If Not var1.Data.VarType = var2.Data.VarType Then Return CreateError("Operand data type mismatch")
                        If (Not var1.Data.VarType = DataType.Integer) Then Return CreateError("Shift-left operand only valid for Integer data type")
                        Dim shift_value As Int32 = CInt(var2.Value)
                        If shift_value > 31 Then Return CreateError("Shift-left value is greater than 31-bits")
                        new_result.Value = CUInt(var1.Value) << shift_value
                        Return new_result
                    Case OperandOper.S_RIGHT
                        Dim new_result As New ScriptVariable("RESULT", DataType.Integer)
                        If Not var1.Data.VarType = var2.Data.VarType Then Return CreateError("Operand data type mismatch")
                        If (Not var1.Data.VarType = DataType.Integer) Then Return CreateError("Shift-right operand only valid for Integer data type")
                        Dim shift_value As Int32 = CInt(var2.Value)
                        If shift_value > 31 Then Return CreateError("Shift-right value is greater than 31-bits")
                        new_result.Value = CUInt(var1.Value) >> shift_value
                        Return new_result
                    Case OperandOper.AND 'We already checked to make sure these are BOOL
                        Dim new_result As New ScriptVariable("RESULT", DataType.Bool)
                        If Not var1.Data.VarType = var2.Data.VarType Then Return CreateError("Operand data type mismatch")
                        new_result.Value = CBool(var1.Value) And CBool(var2.Value)
                        Return new_result
                    Case OperandOper.OR 'We already checked to make sure these are BOOL
                        Dim new_result As New ScriptVariable("RESULT", DataType.Bool)
                        If Not var1.Data.VarType = var2.Data.VarType Then Return CreateError("Operand data type mismatch")
                        new_result.Value = CBool(var1.Value) Or CBool(var2.Value)
                        Return new_result
                    Case OperandOper.IS 'Boolean compare operators
                        Dim new_result As New ScriptVariable("RESULT", DataType.Bool)
                        Dim result As Boolean = False
                        If var1 IsNot Nothing AndAlso var1.Data.VarType = DataType.NULL Then
                            If var2 Is Nothing Then
                                result = True
                            ElseIf var2.Value Is Nothing Then
                                result = True
                            End If
                        ElseIf var2 IsNot Nothing AndAlso var2.Data.VarType = DataType.NULL Then
                            If var1 Is Nothing Then
                                result = True
                            ElseIf var1.Value Is Nothing Then
                                result = True
                            End If
                        Else
                            If Not var1.Data.VarType = var2.Data.VarType Then Return CreateError("Operand data type mismatch")
                            If var1.Data.VarType = DataType.String Then
                                Dim s1 As String = CStr(var1.Value)
                                Dim s2 As String = CStr(var2.Value)
                                If s1.Length = s2.Length Then
                                    For i = 0 To s1.Length - 1
                                        If (Not s1.Substring(i, 1) = s2.Substring(i, 1)) Then result = False : Exit For
                                    Next
                                End If
                            ElseIf var1.Data.VarType = DataType.Integer Then
                                result = (CInt(var1.Value) = CInt(var2.Value))
                            ElseIf var1.Data.VarType = DataType.UInteger Then
                                result = (CUInt(var1.Value) = CUInt(var2.Value))
                            ElseIf var1.Data.VarType = DataType.Data Then
                                Dim d1() As Byte = CType(var1.Value, Byte())
                                Dim d2() As Byte = CType(var2.Value, Byte())
                                If d1.Length = d2.Length Then
                                    result = True
                                    For i = 0 To d1.Length - 1
                                        If (Not d1(i) = d2(i)) Then result = False : Exit For
                                    Next
                                End If
                            End If
                        End If
                        new_result.Value = result
                        Return new_result
                    Case OperandOper.LESS_THAN 'Boolean compare operators 
                        Dim new_result As New ScriptVariable("RESULT", DataType.Bool)
                        If Not var1.Data.VarType = var2.Data.VarType Then Return CreateError("Operand data type mismatch")
                        If (var1.Data.VarType = DataType.Integer) Then
                            new_result.Value = (CInt(var1.Value) < CInt(var2.Value))
                        ElseIf (var1.Data.VarType = DataType.UInteger) Then
                            new_result.Value = (CUInt(var1.Value) < CUInt(var2.Value))
                        Else
                            Return CreateError("Less-than compare operand only valid for Integer/UInteger data type")
                        End If
                        Return new_result
                    Case OperandOper.GRT_THAN 'Boolean compare operators
                        Dim new_result As New ScriptVariable("RESULT", DataType.Bool)
                        If Not var1.Data.VarType = var2.Data.VarType Then Return CreateError("Operand data type mismatch")
                        If (var1.Data.VarType = DataType.Integer) Then
                            new_result.Value = (CInt(var1.Value) > CInt(var2.Value))
                        ElseIf (var1.Data.VarType = DataType.UInteger) Then
                            new_result.Value = (CUInt(var1.Value) > CUInt(var2.Value))
                        Else
                            Return CreateError("Greater-than compare operand only valid for Integer/UInteger data type")
                        End If
                        Return new_result
                End Select
            Catch ex As Exception
                Return CreateError("Error compiling operands")
            End Try
            Return Nothing
        End Function

        Public Sub ParseToFunctionAndSub(to_parse As String, ByRef main_fnc As String, ByRef sub_fnc As String, ByRef ind_fnc As String, ByRef arguments As String)
            Try
                ind_fnc = "0"
                main_fnc = FeedWord(to_parse, {"(", "."})
                If (to_parse = "") Then 'element is only one item
                    sub_fnc = ""
                    arguments = ""
                ElseIf to_parse.StartsWith("()") Then
                    to_parse = to_parse.Substring(2).Trim
                    If to_parse = "" OrElse Not to_parse.StartsWith(".") Then
                        sub_fnc = ""
                        arguments = ""
                        Exit Sub
                    End If
                    sub_fnc = FeedWord(to_parse, {"("})
                    arguments = FeedParameter(to_parse)
                    Exit Sub
                ElseIf to_parse.StartsWith("(") Then
                    Dim section As String = FeedParameter(to_parse)
                    If to_parse.StartsWith(".") Then
                        ind_fnc = section
                        to_parse = to_parse.Substring(1).Trim()
                        sub_fnc = FeedWord(to_parse, {"("})
                        If (Not to_parse = "") AndAlso to_parse.StartsWith("(") Then
                            arguments = FeedParameter(to_parse)
                        End If
                    Else
                        sub_fnc = ""
                        arguments = section
                    End If
                ElseIf to_parse.StartsWith(".") Then
                    to_parse = to_parse.Substring(1).Trim()
                    sub_fnc = FeedWord(to_parse, {"("})
                    If (Not to_parse = "") AndAlso to_parse.StartsWith("(") Then
                        arguments = FeedParameter(to_parse)
                    End If
                End If
            Catch ex As Exception
                main_fnc = ""
                sub_fnc = ""
            End Try
        End Sub

    End Module

End Namespace