'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2020 - ALL RIGHTS RESERVED
'CONTACT EMAIL: support@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: This class is the entire scripting engine which can control the software
'via user supplied text files. The langauge format is similar to BASIC.

Imports FlashcatUSB.JTAG
Imports FlashcatUSB.MemoryInterface

Namespace FlashcatScript

    Public Class Processor : Implements IDisposable
        Public Const Build As Integer = 306

        Friend CmdFunctions As New ScriptCmd
        Friend CurrentScript As New ScriptFile
        Friend CurrentVars As New ScriptVariableManager

        Public Delegate Sub UpdateFunction_Progress(percent As Integer)
        Public Delegate Sub UpdateFunction_Status(msg As String)

        Private Delegate Function ScriptFunction(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
        Public Property CURRENT_DEVICE_MODE As DeviceMode

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
            CmdFunctions.AddNest(STR_CMD)
            Dim DATA_CMD As New ScriptCmd("DATA")
            DATA_CMD.Add("new", {CmdPrm.Integer, CmdPrm.Data}, New ScriptFunction(AddressOf c_data_new))
            DATA_CMD.Add("fromhex", {CmdPrm.String}, New ScriptFunction(AddressOf c_data_fromhex))
            DATA_CMD.Add("compare", {CmdPrm.Data}, New ScriptFunction(AddressOf c_data_compare))
            DATA_CMD.Add("length", {CmdPrm.Data}, New ScriptFunction(AddressOf c_data_length))
            DATA_CMD.Add("resize", {CmdPrm.Data, CmdPrm.Integer, CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_data_resize))
            DATA_CMD.Add("hword", {CmdPrm.Data, CmdPrm.Integer}, New ScriptFunction(AddressOf c_data_hword))
            DATA_CMD.Add("word", {CmdPrm.Data, CmdPrm.Integer}, New ScriptFunction(AddressOf c_data_word))
            DATA_CMD.Add("tostr", {CmdPrm.Data}, New ScriptFunction(AddressOf c_data_tostr))
            DATA_CMD.Add("copy", {CmdPrm.Data}, New ScriptFunction(AddressOf c_data_copy))
            DATA_CMD.Add("combine", {CmdPrm.Data}, New ScriptFunction(AddressOf c_data_combine))
            CmdFunctions.AddNest(DATA_CMD)
            Dim IO_CMD As New ScriptCmd("IO")
            IO_CMD.Add("open", {CmdPrm.String_Optional, CmdPrm.String_Optional}, New ScriptFunction(AddressOf c_io_open))
            IO_CMD.Add("save", {CmdPrm.Data, CmdPrm.String_Optional, CmdPrm.String_Optional}, New ScriptFunction(AddressOf c_io_save))
            IO_CMD.Add("read", {CmdPrm.String}, New ScriptFunction(AddressOf c_io_read))
            IO_CMD.Add("write", {CmdPrm.Data, CmdPrm.String}, New ScriptFunction(AddressOf c_io_write))
            CmdFunctions.AddNest(IO_CMD)
            Dim MEM_CMD As New ScriptCmd("MEMORY")
            MEM_CMD.Add("name", Nothing, New ScriptFunction(AddressOf c_mem_name))
            MEM_CMD.Add("size", Nothing, New ScriptFunction(AddressOf c_mem_size))
            MEM_CMD.Add("write", {CmdPrm.Data, CmdPrm.UInteger, CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_mem_write))
            MEM_CMD.Add("read", {CmdPrm.UInteger, CmdPrm.Integer, CmdPrm.Bool_Optional}, New ScriptFunction(AddressOf c_mem_read))
            MEM_CMD.Add("readstring", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_mem_readstring))
            MEM_CMD.Add("readverify", {CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_mem_readverify))
            MEM_CMD.Add("sectorcount", Nothing, New ScriptFunction(AddressOf c_mem_sectorcount))
            MEM_CMD.Add("sectorsize", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_mem_sectorsize))
            MEM_CMD.Add("erasesector", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_mem_erasesector))
            MEM_CMD.Add("erasebulk", Nothing, New ScriptFunction(AddressOf c_mem_erasebulk))
            MEM_CMD.Add("exist", Nothing, New ScriptFunction(AddressOf c_mem_exist))
            CmdFunctions.AddNest(MEM_CMD)
            Dim TAB_CMD As New ScriptCmd("TAB")
            TAB_CMD.Add("create", {CmdPrm.String}, New ScriptFunction(AddressOf c_tab_create))
            TAB_CMD.Add("addgroup", {CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addgroup))
            TAB_CMD.Add("addbox", {CmdPrm.String, CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addbox))
            TAB_CMD.Add("addtext", {CmdPrm.String, CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addtext))
            TAB_CMD.Add("addimage", {CmdPrm.String, CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addimage))
            TAB_CMD.Add("addbutton", {CmdPrm.String, CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addbutton))
            TAB_CMD.Add("addprogress", {CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addprogress))
            TAB_CMD.Add("remove", {CmdPrm.String}, New ScriptFunction(AddressOf c_tab_remove))
            TAB_CMD.Add("settext", {CmdPrm.String, CmdPrm.String}, New ScriptFunction(AddressOf c_tab_settext))
            TAB_CMD.Add("gettext", {CmdPrm.String}, New ScriptFunction(AddressOf c_tab_gettext))
            TAB_CMD.Add("buttondisable", {CmdPrm.String_Optional}, New ScriptFunction(AddressOf c_tab_buttondisable))
            TAB_CMD.Add("buttonenable", {CmdPrm.String_Optional}, New ScriptFunction(AddressOf c_tab_buttonenable))
            CmdFunctions.AddNest(TAB_CMD)
            Dim SPI_CMD As New ScriptCmd("SPI")
            SPI_CMD.Add("clock", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_spi_clock))
            SPI_CMD.Add("mode", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_spi_mode))
            SPI_CMD.Add("database", {CmdPrm.Bool_Optional}, New ScriptFunction(AddressOf c_spi_database))
            SPI_CMD.Add("getsr", {CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_spi_getsr))
            SPI_CMD.Add("setsr", {CmdPrm.Data}, New ScriptFunction(AddressOf c_spi_setsr))
            SPI_CMD.Add("writeread", {CmdPrm.Data, CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_spi_writeread))
            SPI_CMD.Add("prog", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_spi_prog)) 'Undocumented
            CmdFunctions.AddNest(SPI_CMD)
            Dim JTAG_CMD As New ScriptCmd("JTAG")
            JTAG_CMD.Add("idcode", Nothing, New ScriptFunction(AddressOf c_jtag_idcode))
            JTAG_CMD.Add("config", {CmdPrm.String_Optional}, New ScriptFunction(AddressOf c_jtag_config))
            JTAG_CMD.Add("select", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_jtag_select))
            JTAG_CMD.Add("print", Nothing, New ScriptFunction(AddressOf c_jtag_print))
            JTAG_CMD.Add("clear", Nothing, New ScriptFunction(AddressOf c_jtag_clear))
            JTAG_CMD.Add("set", {CmdPrm.Integer, CmdPrm.String}, New ScriptFunction(AddressOf c_jtag_set))
            JTAG_CMD.Add("add", {CmdPrm.String}, New ScriptFunction(AddressOf c_jtag_add))
            JTAG_CMD.Add("validate", Nothing, New ScriptFunction(AddressOf c_jtag_validate))
            JTAG_CMD.Add("writeword", {CmdPrm.UInteger, CmdPrm.UInteger}, New ScriptFunction(AddressOf c_jtag_write32))
            JTAG_CMD.Add("readword", {CmdPrm.UInteger}, New ScriptFunction(AddressOf c_jtag_read32))
            JTAG_CMD.Add("control", {CmdPrm.UInteger}, New ScriptFunction(AddressOf c_jtag_control))
            JTAG_CMD.Add("memoryinit", {CmdPrm.String, CmdPrm.UInteger_Optional, CmdPrm.UInteger_Optional}, New ScriptFunction(AddressOf c_jtag_memoryinit))
            JTAG_CMD.Add("debug", {CmdPrm.Bool}, New ScriptFunction(AddressOf c_jtag_debug))
            JTAG_CMD.Add("cpureset", Nothing, New ScriptFunction(AddressOf c_jtag_cpureset))
            JTAG_CMD.Add("runsvf", {CmdPrm.Data}, New ScriptFunction(AddressOf c_jtag_runsvf))
            JTAG_CMD.Add("runxsvf", {CmdPrm.Data}, New ScriptFunction(AddressOf c_jtag_runxsvf))
            JTAG_CMD.Add("shiftdr", {CmdPrm.Data, CmdPrm.Integer, CmdPrm.Bool_Optional}, New ScriptFunction(AddressOf c_jtag_shiftdr))
            JTAG_CMD.Add("shiftir", {CmdPrm.Data, CmdPrm.Bool_Optional}, New ScriptFunction(AddressOf c_jtag_shiftir))
            JTAG_CMD.Add("shiftout", {CmdPrm.Data, CmdPrm.Integer, CmdPrm.Bool_Optional}, New ScriptFunction(AddressOf c_jtag_shiftout))
            JTAG_CMD.Add("tapreset", Nothing, New ScriptFunction(AddressOf c_jtag_tapreset))
            JTAG_CMD.Add("state", {CmdPrm.String}, New ScriptFunction(AddressOf c_jtag_state))
            JTAG_CMD.Add("graycode", {CmdPrm.Integer, CmdPrm.Bool_Optional}, New ScriptFunction(AddressOf c_jtag_graycode))
            JTAG_CMD.Add("setdelay", {CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_jtag_setdelay)) 'Legacy support
            JTAG_CMD.Add("exitstate", {CmdPrm.Bool}, New ScriptFunction(AddressOf c_jtag_exitstate)) 'SVF player option
            JTAG_CMD.Add("epc2_read", Nothing, New ScriptFunction(AddressOf c_jtag_epc2_read))
            JTAG_CMD.Add("epc2_write", {CmdPrm.Data, CmdPrm.Data}, New ScriptFunction(AddressOf c_jtag_epc2_write))
            JTAG_CMD.Add("epc2_erase", Nothing, New ScriptFunction(AddressOf c_jtag_epc2_erase))
            CmdFunctions.AddNest(JTAG_CMD)
            Dim BSDL_CMD As New ScriptCmd("BSDL")
            BSDL_CMD.Add("new", {CmdPrm.String}, New ScriptFunction(AddressOf c_bsdl_new))
            BSDL_CMD.Add("find", {CmdPrm.String}, New ScriptFunction(AddressOf c_bsdl_find))
            BSDL_CMD.Add("parameter", {CmdPrm.String, CmdPrm.UInteger}, New ScriptFunction(AddressOf c_bsdl_param))
            CmdFunctions.AddNest(BSDL_CMD)
            Dim JTAG_BSP As New ScriptCmd("BoundaryScan")
            JTAG_BSP.Add("setup", Nothing, New ScriptFunction(AddressOf c_bsp_setup))
            JTAG_BSP.Add("init", {CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_bsp_init))
            JTAG_BSP.Add("addpin", {CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_bsp_addpin))
            JTAG_BSP.Add("setbsr", {CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Bool}, New ScriptFunction(AddressOf c_bsp_setbsr))
            JTAG_BSP.Add("writebsr", Nothing, New ScriptFunction(AddressOf c_bsp_writebsr))
            JTAG_BSP.Add("detect", Nothing, New ScriptFunction(AddressOf c_bsp_detect))
            CmdFunctions.AddNest(JTAG_BSP)
            Dim PAR_CMD As New ScriptCmd("parallel")
            PAR_CMD.Add("test", Nothing, New ScriptFunction(AddressOf c_parallel_test))
            PAR_CMD.Add("command", {CmdPrm.UInteger, CmdPrm.UInteger}, New ScriptFunction(AddressOf c_parallel_command))
            PAR_CMD.Add("write", {CmdPrm.UInteger, CmdPrm.UInteger}, New ScriptFunction(AddressOf c_parallel_write))
            PAR_CMD.Add("read", {CmdPrm.UInteger}, New ScriptFunction(AddressOf c_parallel_read))
            CmdFunctions.AddNest(PAR_CMD)
            Dim LOADOPT As New ScriptCmd("load") 'Undocumented
            LOADOPT.Add("firmware", Nothing, New ScriptFunction(AddressOf c_load_firmware))
            LOADOPT.Add("logic", Nothing, New ScriptFunction(AddressOf c_load_logic))
            LOADOPT.Add("erase", Nothing, New ScriptFunction(AddressOf c_load_erase))
            LOADOPT.Add("bootloader", {CmdPrm.Data}, New ScriptFunction(AddressOf c_load_bootloader))
            CmdFunctions.AddNest(LOADOPT)
            Dim FLSHOPT As New ScriptCmd("flash") 'Undocumented
            Dim del_add As New ScriptFunction(AddressOf c_flash_add)
            FLSHOPT.Add("add", {CmdPrm.String, CmdPrm.UInteger, CmdPrm.UInteger, CmdPrm.UInteger, CmdPrm.UInteger, CmdPrm.UInteger, CmdPrm.UInteger, CmdPrm.UInteger}, del_add)
            CmdFunctions.AddNest(FLSHOPT)
            'Generic functions
            CmdFunctions.Add("writeline", {CmdPrm.Any, CmdPrm.Bool_Optional}, New ScriptFunction(AddressOf c_writeline))
            CmdFunctions.Add("print", {CmdPrm.Any, CmdPrm.Bool_Optional}, New ScriptFunction(AddressOf c_writeline))
            CmdFunctions.Add("msgbox", {CmdPrm.Any}, New ScriptFunction(AddressOf c_msgbox))
            CmdFunctions.Add("status", {CmdPrm.String}, New ScriptFunction(AddressOf c_setstatus))
            CmdFunctions.Add("refresh", Nothing, New ScriptFunction(AddressOf c_refresh))
            CmdFunctions.Add("sleep", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_sleep))
            CmdFunctions.Add("verify", {CmdPrm.Bool}, New ScriptFunction(AddressOf c_verify))
            CmdFunctions.Add("mode", Nothing, New ScriptFunction(AddressOf c_mode))
            CmdFunctions.Add("ask", {CmdPrm.String}, New ScriptFunction(AddressOf c_ask))
            CmdFunctions.Add("endian", {CmdPrm.String}, New ScriptFunction(AddressOf c_endian))
            CmdFunctions.Add("abort", Nothing, New ScriptFunction(AddressOf c_abort))
            CmdFunctions.Add("catalog", Nothing, New ScriptFunction(AddressOf c_catalog))
            CmdFunctions.Add("cpen", {CmdPrm.Bool}, New ScriptFunction(AddressOf c_cpen))
            CmdFunctions.Add("crc16", {CmdPrm.Data}, New ScriptFunction(AddressOf c_crc16))
            CmdFunctions.Add("crc32", {CmdPrm.Data}, New ScriptFunction(AddressOf c_crc32))
            CmdFunctions.Add("cint", {CmdPrm.UInteger}, New ScriptFunction(AddressOf c_cint))
            CmdFunctions.Add("cuint", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_cuint))

            'CmdFunctions.Add("debug", {CmdPrm.UInteger}, New ScriptFunction(AddressOf c_debug))
        End Sub

        Friend Function c_debug(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim v As UInt32 = arguments(0).Value

            Return Nothing
        End Function

        Friend Function ExecuteCommand(cmd_line As String) As Boolean
            Me.ABORT_SCRIPT = False
            Dim scripe_line As New ScriptElement(Me)
            scripe_line.Parse(cmd_line, True)
            If (scripe_line.HAS_ERROR) Then
                RaiseEvent PrintConsole("Error: " & scripe_line.ERROR_MSG)
                Return False
            Else
                If Not ExecuteScriptElement(scripe_line, Nothing) Then
                    RaiseEvent PrintConsole("Error: " & scripe_line.ERROR_MSG)
                End If
            End If
            Return True
        End Function
        'Unloads any current script
        Friend Function Unload() As Boolean
            Me.ABORT_SCRIPT = True
            Me.CurrentVars.Clear()
            Me.CurrentScript.Reset()
            For i = 0 To OurFlashDevices.Count - 1
                Dim this_devie As MemoryDeviceInstance = OurFlashDevices(i)
                If GUI IsNot Nothing Then GUI.RemoveTab(this_devie)
                MEM_IF.Remove(this_devie)
                Application.DoEvents()
            Next
            If GUI IsNot Nothing Then GUI.RemoveUserTabs()
            OurFlashDevices.Clear()
            UserTabCount = 0
            Return True
        End Function
        'This loads the script file
        Friend Function LoadFile(file_name As IO.FileInfo) As Boolean
            Me.Unload()
            RaiseEvent PrintConsole("Loading FlashcatUSB script: " & file_name.Name)
            Dim f() As String = Utilities.FileIO.ReadFile(file_name.FullName)
            Dim err_str As String = ""
            Dim line_err As UInt32 = 0 'The line within the file that has the error
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
            Dim line_err As UInt32
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

        Friend Function ExecuteElements(e() As ScriptLineElement, ByRef params As ExecuteParam) As Boolean
            If Me.ABORT_SCRIPT Then Return False
            If params.exit_task = ExitMode.LeaveScript Then Return True
            If e IsNot Nothing AndAlso e.Length > 0 Then
                For i = 0 To e.Length - 1
                    Select Case e(i).ElementType
                        Case ScriptFileElementType.ELEMENT
                            ExecuteScriptElement(e(i), params.exit_task)
                            params.err_reason = DirectCast(e(i), ScriptElement).ERROR_MSG
                            If Not params.err_reason = "" Then params.err_line = e(i).INDEX : Return False
                            If params.exit_task = ExitMode.LeaveScript Then Return True
                        Case ScriptFileElementType.FOR_LOOP
                            Dim se As ScriptLoop = e(i)
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
                            Dim se As ScriptCondition = e(i)
                            Dim test_condition As ScriptVariable = se.CONDITION.Compile(params.exit_task)
                            If test_condition Is Nothing OrElse se.CONDITION.HAS_ERROR Then
                                params.err_reason = se.CONDITION.ERROR_MSG
                                params.err_line = se.INDEX
                                Return False
                            End If
                            Dim result As Boolean = test_condition.Value
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
                            Dim so As ScriptGoto = e(i)
                            params.goto_label = so.TO_LABEL.ToUpper
                            params.exit_task = ExitMode.GotoLabel
                        Case ScriptFileElementType.EXIT
                            Dim so As ScriptExit = e(i)
                            params.exit_task = so.MODE
                            Return True
                        Case ScriptFileElementType.RETURN
                            Dim sr As ScriptReturn = e(i)
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
                                If DirectCast(e(x), ScriptLabel).NAME.ToUpper = params.goto_label Then
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

        Friend Function ExecuteScriptElement(e As ScriptElement, ByRef exit_task As ExitMode) As Boolean
            Try
                Dim sv As ScriptVariable = e.Compile(exit_task)
                If e.HAS_ERROR Then Return False
                If sv Is Nothing Then Return True 'Compiled successfully but no value to save
                If (Not e.TARGET_NAME = "") AndAlso Not e.TARGET_OPERATION = TargetOper.NONE Then
                    If (Not e.TARGET_VAR = "") Then
                        If CurrentVars.IsVariable(e.TARGET_VAR) AndAlso CurrentVars.GetVariable(e.TARGET_VAR).Data.VarType = DataType.UInteger Then
                            e.TARGET_INDEX = CurrentVars.GetVariable(e.TARGET_VAR).Value 'Gets the variable and assigns it to the index
                        Else
                            e.ERROR_MSG = "Target index is not an integer or integer variable" : Return False
                        End If
                    End If
                    If (e.TARGET_INDEX > -1) Then 'We are assinging this result to an index within a data array
                        Dim current_var As ScriptVariable = CurrentVars.GetVariable(e.TARGET_NAME)
                        If current_var Is Nothing Then e.ERROR_MSG = "Target index used on a variable that does not exist" : Return False
                        If current_var.Data.VarType = DataType.NULL Then e.ERROR_MSG = "Target index used on a variable that does not yet exist" : Return False
                        If Not current_var.Data.VarType = DataType.Data Then e.ERROR_MSG = "Target index used on a variable that is not a DATA array" : Return False
                        Dim data_out() As Byte = current_var.Value
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
                                CurrentVars.SetVariable(new_var) : Return True
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
                            Dim result_var As ScriptVariable = CompileSVars(existing_var, new_var, var_op, e.ERROR_MSG)
                            If Not e.ERROR_MSG.Equals("") Then Return False
                            Dim compiled_var As New ScriptVariable(e.TARGET_NAME, result_var.Data.VarType)
                            compiled_var.Value = result_var.Value
                            CurrentVars.SetVariable(compiled_var)
                        End If
                    End If
                End If
                Return True
            Catch ex As Exception
                e.ERROR_MSG = "General purpose error"
                Return False
            End Try
        End Function

        Friend Function ExecuteScriptEvent(s_event As ScriptEvent, arguments() As ScriptVariable, ByRef exit_task As ExitMode) As ScriptVariable
            If arguments.Count > 0 Then
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
            Return event_result
        End Function

        Friend Function GetScriptEvent(input As String) As ScriptEvent
            Dim main_event_name As String = ""
            ParseToFunctionAndSub(input, main_event_name, Nothing, Nothing, Nothing)
            For Each item In CurrentScript.TheScript
                If item.ElementType = ScriptFileElementType.EVENT Then
                    Dim se As ScriptEvent = item
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

        Public Sub PrintInformation()
            RaiseEvent PrintConsole("FlashcatUSB Script Engine build: " & Build)
        End Sub





#Region "Progress Callbacks"
        'Private ScriptBar As ProgressBar 'Our one and only progress bar
        'Private Delegate Sub UpdateFunction_Progress(percent As Integer)
        'Private Delegate Sub UpdateFunction_Status(txt As String)
        'Private Delegate Sub UpdateFunction_Base(addr As Long)
        'Private Property PROGRESS_BASE As UInt32 = 0 'Address we have a operation at

        'Private Sub ProgressUpdateBase(addr As UInt32)
        '    Try
        '        Me.PROGRESS_BASE = addr
        '    Catch ex As Exception
        '    End Try
        'End Sub
        ''Sets the status bar on the GUI (if one exists)
        'Private Sub ProgressUpdate_Percent(percent As Integer)
        '    Try
        '        If GUI IsNot Nothing Then
        '            If ScriptBar IsNot Nothing Then
        '                If GUI.InvokeRequired Then
        '                    Dim d As New UpdateFunction_Progress(AddressOf ProgressUpdate_Percent)
        '                    GUI.Invoke(d, New Object() {percent})
        '                Else
        '                    If (percent > 100) Then percent = 100
        '                    ScriptBar.Value = percent
        '                End If
        '            End If
        '        End If
        '    Catch ex As Exception
        '    End Try
        'End Sub

        'Private Sub ProgressUpdate_Status(msg As String)
        '    RaiseEvent SetStatus(msg)
        'End Sub






#End Region






#Region "User control handlers"
        Private UserTabCount As UInt32
        Private OurFlashDevices As New List(Of MemoryDeviceInstance)

        'Reads the data from flash and verifies it (returns nothing on error)
        Private Function ReadMemoryVerify(address As UInt32, data_len As UInt32, index As FlashMemory.FlashArea) As Byte()
            Dim cb As New MemoryDeviceInstance.StatusCallback
            cb.UpdatePercent = New UpdateFunction_Progress(AddressOf MainApp.ProgressBar_Percent)
            cb.UpdateTask = New UpdateFunction_Status(AddressOf MainApp.SetStatus)
            Dim memDev As MemoryDeviceInstance = MEM_IF.GetDevice(index)
            Dim FlashData1() As Byte = memDev.ReadBytes(address, data_len, cb)
            If FlashData1 Is Nothing Then Return Nothing
            Dim FlashData2() As Byte = memDev.ReadBytes(address, data_len, cb)
            If FlashData2 Is Nothing Then Return Nothing
            If Not FlashData1.Length = FlashData2.Length Then Return Nothing 'Error already?
            If FlashData1.Length = 0 Then Return Nothing
            If FlashData2.Length = 0 Then Return Nothing
            Dim DataWords1() As UInt32 = Utilities.Bytes.ToUIntArray(FlashData1) 'This is the one corrected
            Dim DataWords2() As UInt32 = Utilities.Bytes.ToUIntArray(FlashData2)
            Dim Counter As Integer
            Dim CheckAddr, CheckValue, CheckArray() As UInt32
            Dim Data() As Byte
            Dim ErrCount As Integer = 0
            For Counter = 0 To DataWords1.Length - 1
                If Not DataWords1(Counter) = DataWords2(Counter) Then
                    If ErrCount = 100 Then Return Nothing 'Too many errors
                    ErrCount = ErrCount + 1
                    CheckAddr = CUInt(address + (Counter * 4)) 'Address to verify
                    Data = memDev.ReadBytes(CheckAddr, 4)
                    CheckArray = Utilities.Bytes.ToUIntArray(Data) 'Will only read one element
                    CheckValue = CheckArray(0)
                    If DataWords1(Counter) = CheckValue Then 'Our original data matched
                    ElseIf DataWords2(Counter) = CheckValue Then 'Our original was incorrect
                        DataWords1(Counter) = DataWords2(Counter)
                    Else
                        Return Nothing '3 reads of the same data did not match, return error!
                    End If
                End If
            Next
            Dim DataOut() As Byte = Utilities.Bytes.FromUint32Array(DataWords1)
            ReDim Preserve DataOut(FlashData1.Length - 1)
            Return DataOut 'Checked ok!
        End Function
        'Removes a user control from NAME
        Private Sub RemoveUserControl(ctr_name As String)
            If GUI Is Nothing Then Exit Sub
            If UserTabCount = 0 Then Exit Sub
            For i = 0 To UserTabCount - 1
                Dim uTab As TabPage = GUI.GetUserTab(i)
                For Each user_control As Control In uTab.Controls
                    If user_control.Name.ToUpper.Equals(ctr_name.ToUpper) Then
                        uTab.Controls.Remove(user_control)
                        Exit Sub
                    End If
                Next
            Next
        End Sub
        'Handles when the user clicks a button
        Private Sub ButtonHandler(sender As Object, e As EventArgs)
            Dim MyButton As Button = CType(sender, Button)
            Dim EventToCall As String = MyButton.Name
            Dim EventThread As New Threading.Thread(AddressOf CallEvent)
            EventThread.Name = "Event:" & EventToCall
            EventThread.SetApartmentState(Threading.ApartmentState.STA)
            EventThread.Start(EventToCall)
            MyButton.Select()
        End Sub
        'Calls a event (wrapper for runscript)
        Private Sub CallEvent(EventName As String)
            RaiseEvent PrintConsole("Button Hander::Calling Event: " & EventName)
            Dim se As ScriptEvent = GetScriptEvent(EventName)
            If se IsNot Nothing Then
                ExecuteScriptEvent(se, Nothing, Nothing)
            Else
                RaiseEvent PrintConsole("Error: Event does not exist")
            End If
            RaiseEvent PrintConsole("Button Hander::Calling Event: Done")
        End Sub

#End Region

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

#Region "COMMANDS"

#Region "String commands"

        Friend Function c_str_upper(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim input As String = arguments(0).Value
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.String)
            sv.Value = input.ToUpper
            Return sv
        End Function

        Friend Function c_str_lower(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim input As String = arguments(0).Value
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.String)
            sv.Value = input.ToLower
            Return sv
        End Function

        Friend Function c_str_hex(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim input As Integer = arguments(0).Value
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.String)
            sv.Value = "0x" & Hex(input)
            Return sv
        End Function

        Friend Function c_str_length(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim input As String = arguments(0).Value
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Integer)
            sv.Value = CInt(input.Length)
            Return sv
        End Function

        Friend Function c_str_toint(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim input As String = arguments(0).Value
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Integer)
            If input.Trim = "" Then
                sv.Value = 0
            Else
                sv.Value = CInt(input)
            End If
            Return sv
        End Function

        Friend Function c_str_fromint(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim input As Int32 = arguments(0).Value
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.String)
            sv.Value = input.ToString
            Return sv
        End Function

#End Region

#Region "Data commands"

        Friend Function c_data_new(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim size As Int32 = arguments(0).Value 'Size of the Data Array
            Dim data(size - 1) As Byte
            If (arguments.Length > 1) Then
                Dim data_init() As Byte = arguments(1).Value
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

        Friend Function c_data_fromhex(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim input As String = arguments(0).Value
            Dim data() As Byte = Utilities.Bytes.FromHexString(input)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Data)
            sv.Value = data
            Return sv
        End Function

        Friend Function c_data_compare(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim data1() As Byte = arguments(0).Value
            Dim data2() As Byte = arguments(1).Value
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

        Friend Function c_data_length(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim data1() As Byte = arguments(0).Value
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.UInteger)
            sv.Value = data1.Length
            Return sv
        End Function

        Friend Function c_data_resize(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim data_arr() As Byte = arguments(0).Value
            Dim copy_index As Int32 = arguments(1).Value
            Dim copy_length As Int32 = data_arr.Length - copy_index
            If arguments.Length = 3 Then copy_length = arguments(2).Value
            Dim data_out(copy_length - 1) As Byte
            Array.Copy(data_arr, copy_index, data_out, 0, copy_length)
            arguments(0).Value = data_out
            CurrentVars.SetVariable(arguments(0))
            Return Nothing
        End Function

        Friend Function c_data_hword(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim data1() As Byte = arguments(0).Value
            Dim offset As Int32 = arguments(1).Value
            Dim b(1) As Byte
            b(0) = data1(offset)
            b(1) = data1(offset + 1)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Integer)
            sv.Value = Utilities.Bytes.ToUInt16(b)
            Return sv
        End Function

        Friend Function c_data_word(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim data1() As Byte = arguments(0).Value
            Dim offset As Int32 = arguments(1).Value
            Dim b(3) As Byte
            b(0) = data1(offset)
            b(1) = data1(offset + 1)
            b(2) = data1(offset + 2)
            b(3) = data1(offset + 3)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Integer)
            sv.Value = Utilities.Bytes.ToUInt32(b)
            Return sv
        End Function

        Friend Function c_data_tostr(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim data1() As Byte = arguments(0).Value
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.String)
            sv.Value = Utilities.Bytes.ToHexString(data1)
            Return sv
        End Function

        Friend Function c_data_copy(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim data1() As Byte = arguments(0).Value
            Dim src_ind As UInt32 = arguments(1).Value
            Dim data_len As UInt32 = data1.Length - src_ind
            If arguments.Length > 2 Then
                data_len = arguments(3).Value
            End If
            Dim new_data(data_len - 1) As Byte
            Array.Copy(data1, src_ind, new_data, 0, new_data.Length)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Data)
            sv.Value = new_data
            Return sv
        End Function

        Friend Function c_data_combine(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim data1() As Byte = arguments(0).Value
            Dim data2() As Byte = arguments(1).Value
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

        Friend Function c_io_open(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
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

        Friend Function c_io_save(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim var_data() As Byte = arguments(0).Value
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

        Friend Function c_io_read(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim input As String = arguments(0).Value
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

        Friend Function c_io_write(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim data1() As Byte = arguments(0).Value
            Dim destination As String = arguments(1).Value
            If Not Utilities.FileIO.WriteBytes(data1, destination) Then
                RaiseEvent PrintConsole("Error in IO.Write: failed to write data")
            End If
            Return Nothing
        End Function

#End Region

#Region "Memory commands"

        Friend Function c_mem_name(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim name_out As String = MEM_IF.GetDevice(Index).Name
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.String)
            sv.Value = name_out
            Return sv
        End Function

        Friend Function c_mem_size(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            If (MEM_IF.GetDevice(Index).Size > CLng(Int32.MaxValue)) Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device is larger than 2GB"}
            End If
            Dim size_value As Int32 = CInt(MEM_IF.GetDevice(Index).Size)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Integer)
            sv.Value = size_value
            Return sv
            Return Nothing
        End Function

        Friend Function c_mem_write(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim data_to_write() As Byte = arguments(0).Value
            Dim offset As Int32 = arguments(1).Value
            Dim data_len As Int32 = data_to_write.Length
            If (arguments.Length > 2) Then data_len = arguments(2).Value
            ReDim Preserve data_to_write(data_len - 1)
            ProgressBar_Percent(0)
            Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(Index)
            If mem_device Is Nothing Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device not connected"}
            End If
            Dim cb As New MemoryDeviceInstance.StatusCallback
            cb.UpdatePercent = New UpdateFunction_Progress(AddressOf MainApp.ProgressBar_Percent)
            cb.UpdateTask = New UpdateFunction_Status(AddressOf MainApp.SetStatus)
            MEM_IF.GetDevice(Index).DisableGuiControls()
            MEM_IF.GetDevice(Index).FCUSB.USB_LEDBlink()
            Try
                Dim mem_dev As MemoryDeviceInstance = MEM_IF.GetDevice(Index)
                Dim write_result As Boolean = mem_dev.WriteBytes(offset, data_to_write, MySettings.VERIFY_WRITE, cb)
                If write_result Then
                    RaiseEvent PrintConsole("Sucessfully programmed " & data_len.ToString("N0") & " bytes")
                Else
                    RaiseEvent PrintConsole("Canceled memory write operation")
                End If
            Catch ex As Exception
            Finally
                MEM_IF.GetDevice(Index).EnableGuiControls()
                MEM_IF.GetDevice(Index).FCUSB.USB_LEDOn()
                ProgressBar_Percent(0)
            End Try
            Return Nothing
        End Function

        Friend Function c_mem_read(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(Index)
            If mem_device Is Nothing Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device not connected"}
            End If
            Dim offset As Int32 = arguments(0).Value
            Dim count As Int32 = arguments(1).Value
            Dim display As Boolean = True
            If (arguments.Length > 2) Then display = arguments(2).Value
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Data)
            Dim cb As New MemoryDeviceInstance.StatusCallback
            If display Then
                cb.UpdatePercent = New UpdateFunction_Progress(AddressOf MainApp.ProgressBar_Percent)
                cb.UpdateTask = New UpdateFunction_Status(AddressOf MainApp.SetStatus)
            End If
            mem_device.DisableGuiControls()
            Try
                ProgressBar_Percent(0)
                Dim data_read() As Byte = Nothing
                data_read = mem_device.ReadBytes(offset, count, cb)
                sv.Value = data_read
            Catch ex As Exception
            Finally
                mem_device.EnableGuiControls()
            End Try
            ProgressBar_Percent(0)
            Return sv
        End Function

        Friend Function c_mem_readstring(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(Index)
            If mem_device Is Nothing Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device not connected"}
            End If
            Dim offset As Int32 = arguments(0).Value
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.String)
            Dim FlashSize As Int32 = mem_device.Size
            If offset + 1 > FlashSize Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Offset is greater than flash size"}
            End If
            Dim strBuilder As String = ""
            For i = offset To FlashSize - 1
                Dim flash_data() As Byte = mem_device.ReadBytes(CLng(i), 1)
                Dim b As Byte = flash_data(0)
                If b > 31 And b < 127 Then
                    strBuilder &= Chr(b)
                ElseIf b = 0 Then
                    Exit For
                Else
                    Return Nothing 'Error
                End If
            Next
            sv.Value = strBuilder
            Return sv
        End Function

        Friend Function c_mem_readverify(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(Index)
            If mem_device Is Nothing Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device not connected"}
                Return Nothing
            End If
            Dim FlashAddress As Int32 = arguments(0).Value
            Dim FlashLen As Int32 = arguments(1).Value
            Dim data() As Byte = Nothing
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Data)
            ProgressBar_Percent(0)
            mem_device.DisableGuiControls()
            Try
                data = ReadMemoryVerify(FlashAddress, FlashLen, Index)
            Catch ex As Exception
            Finally
                mem_device.EnableGuiControls()
                ProgressBar_Percent(0)
            End Try
            If data Is Nothing Then
                RaiseEvent PrintConsole("Read operation failed")
                Return Nothing
            End If
            sv.Value = data
            Return sv
        End Function

        Friend Function c_mem_sectorcount(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(Index)
            If mem_device Is Nothing Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device not connected"}
                Return Nothing
            End If
            Dim sector_count As Int32 = CInt(mem_device.GetSectorCount)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Integer)
            sv.Value = sector_count
            Return sv
        End Function

        Friend Function c_mem_sectorsize(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(Index)
            If mem_device Is Nothing Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device not connected"}
                Return Nothing
            End If
            Dim sector_int As Int32 = arguments(0).Value
            Dim sector_size As Int32 = CInt(mem_device.GetSectorSize(sector_int))
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Integer)
            sv.Value = sector_size
            Return sv
        End Function

        Friend Function c_mem_erasesector(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(Index)
            If mem_device Is Nothing Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device not connected"}
                Return Nothing
            End If
            Dim mem_sector As Int32 = arguments(0).Value
            mem_device.EraseSector(mem_sector)
            Dim target_addr As Long = mem_device.GetSectorBaseAddress(mem_sector)
            Dim target_area As String = "0x" & Hex(target_addr).PadLeft(8, "0") & " to 0x" & Hex(target_addr + mem_device.GetSectorSize(mem_sector) - 1).PadLeft(8, "0")
            If mem_device.NoErrors Then
                RaiseEvent PrintConsole("Successfully erased sector index: " & mem_sector & " (" & target_area & ")")
            Else
                RaiseEvent PrintConsole("Failed to erase sector index: " & mem_sector & " (" & target_area & ")")
            End If
            mem_device.GuiControl.RefreshView()
            mem_device.ReadMode()
            Return Nothing
        End Function

        Friend Function c_mem_erasebulk(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(Index)
            If mem_device Is Nothing Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device not connected"}
                Return Nothing
            End If
            Try
                MEM_IF.GetDevice(Index).DisableGuiControls()
                mem_device.EraseFlash()
            Catch ex As Exception
            Finally
                MEM_IF.GetDevice(Index).EnableGuiControls()
            End Try
            Return Nothing
        End Function

        Friend Function c_mem_exist(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(Index)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Bool)
            If mem_device Is Nothing Then
                sv.Value = False
            Else
                sv.Value = True
            End If
            Return sv
            Return Nothing
        End Function

#End Region

#Region "TAB commands"

        Friend Function c_tab_create(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim tab_name As String = arguments(0).Value
            GUI.CreateFormTab(UserTabCount, " " & tab_name & " ") 'Thread-Safe
            UserTabCount = UserTabCount + 1
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.UInteger)
            sv.Value = UserTabCount - 1
            Return sv
        End Function

        Friend Function c_tab_addgroup(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim NewGroup As New GroupBox
            NewGroup.Name = arguments(0).Value
            NewGroup.Text = arguments(0).Value
            NewGroup.Left = arguments(1).Value
            NewGroup.Top = arguments(2).Value
            NewGroup.Width = arguments(3).Value
            NewGroup.Height = arguments(4).Value
            GUI.AddControlToTable(Index, NewGroup)
            Return Nothing
        End Function

        Friend Function c_tab_addbox(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim NewTextBox As New TextBox
            NewTextBox.Name = arguments(0).Value
            NewTextBox.Text = arguments(1).Value
            NewTextBox.Width = (NewTextBox.Text.Length * 8) + 2
            NewTextBox.TextAlign = HorizontalAlignment.Center
            NewTextBox.Left = arguments(2).Value
            NewTextBox.Top = arguments(3).Value
            GUI.AddControlToTable(Index, NewTextBox)
            Return Nothing
        End Function

        Friend Function c_tab_addtext(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim NewTextLabel As New Label
            NewTextLabel.Name = arguments(0).Value
            NewTextLabel.AutoSize = True
            NewTextLabel.Text = arguments(1).Value
            NewTextLabel.Width = (NewTextLabel.Text.Length * 7)
            NewTextLabel.Left = arguments(2).Value
            NewTextLabel.Top = arguments(3).Value
            NewTextLabel.BringToFront()
            GUI.AddControlToTable(Index, NewTextLabel)
            Return Nothing
        End Function

        Friend Function c_tab_addimage(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim filen As String = arguments(1).Value
            Dim finfo As New IO.FileInfo(ScriptPath & filen)
            If Not finfo.Exists Then RaiseEvent PrintConsole("Tab.AddImage, specified image not found: " & filen) : Return Nothing
            Dim newImage As Image = Image.FromFile(finfo.FullName)
            Dim NewPB As New PictureBox
            NewPB.Name = arguments(0).Value
            NewPB.Image = newImage
            NewPB.Left = arguments(2).Value
            NewPB.Top = arguments(3).Value
            NewPB.Width = newImage.Width + 5
            NewPB.Height = newImage.Height + 5
            NewPB.BringToFront() 'does not work
            GUI.AddControlToTable(Index, NewPB)
            Return Nothing
        End Function

        Friend Function c_tab_addbutton(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim NewButton As New Button
            NewButton.AutoSize = True
            NewButton.Name = arguments(0).Value
            NewButton.Text = arguments(1).Value
            AddHandler NewButton.Click, AddressOf ButtonHandler
            NewButton.Left = arguments(2).Value
            NewButton.Top = arguments(3).Value
            NewButton.BringToFront() 'does not work
            GUI.AddControlToTable(Index, NewButton)
            Return Nothing
        End Function

        Friend Function c_tab_addprogress(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim bar_left As Integer = CInt(arguments(0).Value)
            Dim bar_top As Integer = CInt(arguments(1).Value)
            Dim bar_width As Integer = CInt(arguments(2).Value)
            MainApp.ProgressBar_Add(Index, bar_left, bar_top, bar_width)
            Return Nothing
        End Function

        Friend Function c_tab_remove(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim item_name As String = arguments(0).Value
            RemoveUserControl(item_name)
            Return Nothing
        End Function

        Friend Function c_tab_settext(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim ctrl_name As String = arguments(0).Value
            Dim new_text As String = arguments(1).Value
            GUI.SetControlText(Index, ctrl_name, new_text)
            Return Nothing
        End Function

        Friend Function c_tab_gettext(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim ctrl_name As String = arguments(0).Value
            Dim result_str As String = GUI.GetControlText(Index, ctrl_name)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.String)
            sv.Value = result_str
            Return sv
            Return Nothing
        End Function

        Friend Function c_tab_buttondisable(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim specific_button As String = ""
            If specific_button.Length = 1 Then
                specific_button = arguments(0).Value
            End If
            GUI.HandleButtons(Index, False, specific_button)
            Return Nothing
        End Function

        Friend Function c_tab_buttonenable(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim specific_button As String = ""
            If specific_button.Length = 1 Then
                specific_button = arguments(0).Value
            End If
            GUI.HandleButtons(Index, True, specific_button)
            Return Nothing
        End Function

#End Region

#Region "SPI commands"

        Friend Function c_spi_clock(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim clock_int As Int32 = arguments(0).Value
            MySettings.SPI_CLOCK_MAX = clock_int
            If MySettings.SPI_CLOCK_MAX < 1000000 Then MySettings.SPI_CLOCK_MAX = 1000000
            Return Nothing
        End Function

        Friend Function c_spi_mode(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim mode_int As Int32 = arguments(0).Value
            Select Case mode_int
                Case 0
                    MySettings.SPI_MODE = SPI.SPI_CLOCK_POLARITY.SPI_MODE_0
                Case 1
                    MySettings.SPI_MODE = SPI.SPI_CLOCK_POLARITY.SPI_MODE_1
                Case 2
                    MySettings.SPI_MODE = SPI.SPI_CLOCK_POLARITY.SPI_MODE_2
                Case 3
                    MySettings.SPI_MODE = SPI.SPI_CLOCK_POLARITY.SPI_MODE_3
            End Select
            Return Nothing
        End Function

        Friend Function c_spi_database(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim DisplayJedecID As Boolean = False
            If arguments.Length = 1 Then
                DisplayJedecID = arguments(0).Value
            End If
            RaiseEvent PrintConsole("The internal Flash database consists of " & FlashDatabase.FlashDB.Count & " devices")
            For Each device In FlashDatabase.FlashDB
                If device.FLASH_TYPE = FlashMemory.MemoryType.SERIAL_NOR Then
                    Dim size_str As String = ""
                    Dim size_int As Integer = device.FLASH_SIZE
                    If (size_int < 128) Then
                        size_str = (size_int / 8) & "bits"
                    ElseIf (size_int < 131072) Then
                        size_str = (size_int / 128) & "Kbits"
                    Else
                        size_str = (size_int / 131072) & "Mbits"
                    End If
                    If DisplayJedecID Then
                        Dim jedec_str As String = Hex(device.MFG_CODE).PadLeft(2, "0") & Hex(device.ID1).PadLeft(4, "0")
                        If (jedec_str = "000000") Then
                            RaiseEvent PrintConsole(device.NAME & " (" & size_str & ") EEPROM")
                        Else
                            RaiseEvent PrintConsole(device.NAME & " (" & size_str & ") JEDEC: 0x" & jedec_str)
                        End If
                    Else
                        RaiseEvent PrintConsole(device.NAME & " (" & size_str & ")")
                    End If
                End If
            Next
            RaiseEvent PrintConsole("SPI Flash database list complete")
            Return Nothing
        End Function

        Friend Function c_spi_getsr(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            If Me.CURRENT_DEVICE_MODE = DeviceMode.SPI Then
            ElseIf Me.CURRENT_DEVICE_MODE = DeviceMode.SQI Then
            Else
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Device is not in SPI/QUAD operation mode"}
            End If
            If Not MAIN_FCUSB.IS_CONNECTED Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "FlashcatUSB device is not connected"}
            End If
            Dim bytes_to_read As Int32 = 1
            If arguments.Length > 0 Then bytes_to_read = arguments(0).Value
            Dim sv As New ScriptVariable(CurrentVars.GetNewName(), DataType.Data)
            If Me.CURRENT_DEVICE_MODE = DeviceMode.SPI Then
                sv.Value = MAIN_FCUSB.SPI_NOR_IF.ReadStatusRegister(bytes_to_read)
            ElseIf Me.CURRENT_DEVICE_MODE = DeviceMode.SQI Then
                sv.Value = MAIN_FCUSB.SQI_NOR_IF.ReadStatusRegister(bytes_to_read)
            End If
            Return sv
            Return Nothing
        End Function

        Friend Function c_spi_setsr(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            If Me.CURRENT_DEVICE_MODE = DeviceMode.SPI Then
            ElseIf Me.CURRENT_DEVICE_MODE = DeviceMode.SQI Then
            Else
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Device is not in SPI/QUAD operation mode"}
            End If
            If Not MAIN_FCUSB.IS_CONNECTED Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "FlashcatUSB device is not connected"}
            End If
            Dim data_out() As Byte = arguments(0).Value
            If Me.CURRENT_DEVICE_MODE = DeviceMode.SPI Then
                MAIN_FCUSB.SPI_NOR_IF.WriteStatusRegister(data_out)
            ElseIf Me.CURRENT_DEVICE_MODE = DeviceMode.SQI Then
                MAIN_FCUSB.SQI_NOR_IF.WriteStatusRegister(data_out)
            End If
            Return Nothing
        End Function

        Friend Function c_spi_writeread(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            If Not Me.CURRENT_DEVICE_MODE = DeviceMode.SPI Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Device is not in SPI operation mode"}
            End If
            If Not MAIN_FCUSB.IS_CONNECTED Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "FlashcatUSB device is not connected"}
            End If
            Dim DataToWrite() As Byte = arguments(0).Value
            Dim ReadBack As Int32 = 0
            If arguments.Length = 2 Then ReadBack = arguments(1).Value
            If ReadBack = 0 Then
                MAIN_FCUSB.SPI_NOR_IF.SPIBUS_WriteRead(DataToWrite)
                Return Nothing
            Else
                Dim return_data(ReadBack - 1) As Byte
                MAIN_FCUSB.SPI_NOR_IF.SPIBUS_WriteRead(DataToWrite, return_data)
                Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Data)
                sv.Value = return_data
                Return sv
            End If
        End Function

        Friend Function c_spi_prog(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim spi_port As SPI.SPI_Programmer = MAIN_FCUSB.SPI_NOR_IF
            Dim state As Integer = arguments(0).Value
            If state = 1 Then 'Set the PROGPIN to HIGH
                spi_port.SetProgPin(True)
            Else 'Set the PROGPIN to LOW
                spi_port.SetProgPin(False)
            End If
            Return Nothing
        End Function

#End Region

#Region "JTAG"

        Friend Function c_jtag_idcode(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim sv As New ScriptVariable(CurrentVars.GetNewName(), DataType.UInteger)
            Dim current_index As Integer = MAIN_FCUSB.JTAG_IF.Chain_SelectedIndex
            sv.Value = MAIN_FCUSB.JTAG_IF.Devices(current_index).IDCODE
            Return sv
        End Function

        Friend Function c_jtag_config(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            If arguments.Length = 1 Then
                Select Case arguments(0).Value.ToUpper
                    Case "MIPS"
                        MAIN_FCUSB.JTAG_IF.Configure(JTAG.PROCESSOR.MIPS)
                    Case "ARM"
                        MAIN_FCUSB.JTAG_IF.Configure(JTAG.PROCESSOR.ARM)
                    Case Else
                        Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Unknown mode: " & arguments(0).Value}
                End Select
            Else
                MAIN_FCUSB.JTAG_IF.Configure(JTAG.PROCESSOR.NONE)
            End If
            Return Nothing
        End Function

        Friend Function c_jtag_select(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Select_JTAG_Device(arguments(0).Value)
            Return Nothing
        End Function

        Friend Function c_jtag_print(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            MAIN_FCUSB.JTAG_IF.Chain_Print()
            Return Nothing
        End Function

        Friend Function c_jtag_clear(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            MAIN_FCUSB.JTAG_IF.Chain_Clear()
            Return Nothing
        End Function

        Friend Function c_jtag_set(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim jtag_device_index As Int32 = arguments(0).Value
            Dim bsdl_name As String = arguments(1).Value
            If MAIN_FCUSB.JTAG_IF.Chain_Set(jtag_device_index, bsdl_name) Then
                RaiseEvent PrintConsole("Successful set chain index " & jtag_device_index.ToString & " to " & bsdl_name)
            Else
                RaiseEvent PrintConsole("Error: unable to find internal BSDL device with name " & bsdl_name)
            End If
            Return Nothing
        End Function

        Friend Function c_jtag_add(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim bsdl_lib As String = arguments(0).Value
            If MAIN_FCUSB.JTAG_IF.Chain_Add(bsdl_lib) Then
                RaiseEvent PrintConsole("Successful added BSDL to JTAG chain")
            Else
                RaiseEvent PrintConsole("Error: BSDL library " & bsdl_lib & " not found")
            End If
            Return Nothing
        End Function

        Friend Function c_jtag_validate(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            If MAIN_FCUSB.JTAG_IF.Chain_Validate() Then
                RaiseEvent PrintConsole("JTAG chain is valid")
            Else
                RaiseEvent PrintConsole("JTAG chain is invalid")
            End If
            Return Nothing
        End Function

        Friend Function c_jtag_control(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim control_value As UInt32 = arguments(0).Value
            Dim j As BSDL_DEF = MAIN_FCUSB.JTAG_IF.Chain_Get(MAIN_FCUSB.JTAG_IF.Chain_SelectedIndex)
            If j IsNot Nothing Then
                Dim result As UInt32 = MAIN_FCUSB.JTAG_IF.AccessDataRegister32(j.MIPS_CONTROL, control_value)
                RaiseEvent PrintConsole("JTAT CONTROL command issued: 0x" & Hex(control_value) & " result: 0x" & Hex(result))
                Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.UInteger)
                sv.Value = result
                Return sv
            End If
            Return Nothing
        End Function

        Friend Function c_jtag_memoryinit(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim flash_type As String = arguments(0).Value
            Dim new_dev As MemoryDeviceInstance = Nothing
            Select Case flash_type.ToUpper
                Case "CFI"
                    Dim base_address As UInt32 = arguments(1).Value
                    RaiseEvent PrintConsole(String.Format(RM.GetString("jtag_cfi_attempt_detect"), Hex(base_address).PadLeft(8, "0")))
                    If MAIN_FCUSB.JTAG_IF.CFI_Detect(base_address) Then
                        new_dev = Connected_Event(MAIN_FCUSB, 16384)
                    Else
                        RaiseEvent PrintConsole(RM.GetString("jtag_cfi_no_detect"))
                    End If
                Case "SPI"
                    RaiseEvent PrintConsole(RM.GetString("jtag_spi_attempt_detect"))
                    If MAIN_FCUSB.JTAG_IF.SPI_Detect(CInt(arguments(1).Value)) Then
                        new_dev = Connected_Event(MAIN_FCUSB, 16384)
                    Else
                        RaiseEvent PrintConsole(RM.GetString("jtag_spi_no_detect")) '"Error: unable to detect SPI flash device over JTAG"
                    End If
                Case Else
                    Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Error in JTAG.MemoryInit: device type not specified"}
            End Select
            If new_dev IsNot Nothing Then
                OurFlashDevices.Add(new_dev)
                Return New ScriptVariable(CurrentVars.GetNewName, DataType.UInteger) With {.Value = (OurFlashDevices.Count - 1)}
            Else
                RaiseEvent PrintConsole("JTAG.MemoryInit: failed to create new memory device interface")
            End If
            Return Nothing
        End Function

        Friend Function c_jtag_debug(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim enable As Boolean = arguments(0).Value
            If enable Then
                MAIN_FCUSB.JTAG_IF.EJTAG_Debug_Enable()
            Else
                MAIN_FCUSB.JTAG_IF.EJTAG_Debug_Disable()
            End If
            Return Nothing
        End Function

        Friend Function c_jtag_cpureset(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            MAIN_FCUSB.JTAG_IF.EJTAG_Reset()
            Return Nothing
        End Function

        Friend Function c_jtag_runsvf(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not valid"}
            End If
            ProgressBar_Percent(0)
            RemoveHandler MAIN_FCUSB.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
            AddHandler MAIN_FCUSB.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
            RaiseEvent PrintConsole("Running SVF file in internal JTAG SVF player")
            Dim DataBytes() As Byte = arguments(0).Value
            Dim FileStr() As String = Utilities.Bytes.ToCharStringArray(DataBytes)
            Dim result As Boolean = MAIN_FCUSB.JTAG_IF.JSP.RunFile_SVF(FileStr)
            If result Then
                RaiseEvent PrintConsole("SVF file successfully played")
            Else
                RaiseEvent PrintConsole("Error playing the SVF file")
            End If
            ProgressBar_Percent(0)
            RemoveHandler MAIN_FCUSB.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Bool)
            sv.Value = result
            Return sv
        End Function

        Friend Function c_jtag_runxsvf(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not valid"}
            End If
            ProgressBar_Percent(0)
            RemoveHandler MAIN_FCUSB.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
            AddHandler MAIN_FCUSB.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
            RaiseEvent PrintConsole("Running XSVF file in internal JTAG XSVF player")
            Dim DataBytes() As Byte = arguments(0).Value
            Dim result As Boolean = MAIN_FCUSB.JTAG_IF.JSP.RunFile_XSVF(DataBytes)
            If result Then
                RaiseEvent PrintConsole("XSVF file successfully played")
            Else
                RaiseEvent PrintConsole("Error playing the XSVF file")
            End If
            ProgressBar_Percent(0)
            RemoveHandler MAIN_FCUSB.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Bool)
            sv.Value = result
            Return sv
        End Function

        Friend Function c_jtag_shiftdr(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not valid"}
            End If
            Dim exit_mode As Boolean = True
            Dim data_in() As Byte = arguments(0).Value
            Dim bit_count As Integer = arguments(1).Value
            Dim data_out() As Byte = Nothing
            If arguments.Length = 3 Then exit_mode = arguments(2).Value
            MAIN_FCUSB.JTAG_IF.JSP_ShiftDR(data_in, data_out, bit_count, exit_mode)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Data)
            sv.Value = data_out
            Return sv
        End Function

        Friend Function c_jtag_shiftir(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not valid"}
            End If
            Dim exit_mode As Boolean = True
            Dim data_in() As Byte = arguments(0).Value
            If arguments.Length = 2 Then exit_mode = arguments(1).Value
            Dim ir_size As Integer = MAIN_FCUSB.JTAG_IF.GetSelected_IRLength()
            MAIN_FCUSB.JTAG_IF.JSP_ShiftIR(data_in, Nothing, ir_size, exit_mode)
            Return Nothing
        End Function

        Friend Function c_jtag_shiftout(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not valid"}
            End If
            Dim tdi_data() As Byte = arguments(0).Value
            Dim bit_count As Integer = arguments(1).Value
            Dim exit_tms As Boolean = True
            If arguments.Length = 3 Then exit_tms = CBool(arguments(2).Value)
            Dim tdo_data() As Byte = Nothing
            MAIN_FCUSB.JTAG_IF.ShiftTDI(bit_count, tdi_data, tdo_data, exit_tms)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Data)
            sv.Value = tdo_data
            Return sv
        End Function

        Friend Function c_jtag_tapreset(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not valid"}
            End If
            MAIN_FCUSB.JTAG_IF.Reset_StateMachine()
            Return Nothing
        End Function

        Friend Function c_jtag_write32(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not valid"}
            End If
            Dim addr32 As UInt32 = arguments(0).Value
            Dim data As UInt32 = arguments(1).Value
            MAIN_FCUSB.JTAG_IF.WriteMemory(addr32, data, DATA_WIDTH.Word)
            Return Nothing
        End Function

        Friend Function c_jtag_read32(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not valid"}
            End If
            Dim addr32 As UInt32 = arguments(0).Value
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.UInteger)
            sv.Value = MAIN_FCUSB.JTAG_IF.ReadMemory(addr32, DATA_WIDTH.Word)
            Return sv
        End Function

        Friend Function c_jtag_state(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not valid"}
            End If
            Dim state_str As String = arguments(0).Value
            Select Case state_str.ToUpper
                Case "RunTestIdle".ToUpper
                    MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
                Case "Select_DR".ToUpper
                    MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Select_DR)
                Case "Capture_DR".ToUpper
                    MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Capture_DR)
                Case "Shift_DR".ToUpper
                    MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
                Case "Exit1_DR".ToUpper
                    MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Exit1_DR)
                Case "Pause_DR".ToUpper
                    MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Pause_DR)
                Case "Exit2_DR".ToUpper
                    MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Exit2_DR)
                Case "Update_DR".ToUpper
                    MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Update_DR)
                Case "Select_IR".ToUpper
                    MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Select_IR)
                Case "Capture_IR".ToUpper
                    MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Capture_IR)
                Case "Shift_IR".ToUpper
                    MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR)
                Case "Exit1_IR".ToUpper
                    MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Exit1_IR)
                Case "Pause_IR".ToUpper
                    MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
                Case "Exit2_IR".ToUpper
                    MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Exit2_IR)
                Case "Update_IR".ToUpper
                    MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Update_IR)
                Case Else
                    Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG.State: unknown state: " & state_str}
            End Select
            Return Nothing
        End Function

        Friend Function c_jtag_graycode(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim use_reserve As Boolean = False
            Dim table_ind As Integer = arguments(0).Value
            If arguments.Length = 2 Then use_reserve = arguments(1).Value
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.UInteger)
            If use_reserve Then
                sv.Value = gray_code_table_reverse(table_ind)
            Else
                sv.Value = gray_code_table(table_ind)
            End If
            Return sv
        End Function
        'Undocumented. This is for setting delays on FCUSB Classic EJTAG firmware
        Friend Function c_jtag_setdelay(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim dev_ind As Int32 = arguments(0).Value
            Dim delay_val As Int32 = arguments(1).Value
            Select Case dev_ind
                Case 1 'Intel
                    MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SET_OPTION, Nothing, (delay_val << 16) + 1)
                Case 2 'AMD
                    MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SET_OPTION, Nothing, (delay_val << 16) + 2)
                Case 3 'DMA
                    MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SET_OPTION, Nothing, (delay_val << 16) + 3)
            End Select
            Return Nothing
        End Function

        Friend Function c_jtag_exitstate(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim exit_state As Boolean = arguments(0).Value
            MAIN_FCUSB.JTAG_IF.JSP.ExitStateMachine = exit_state
            If exit_state Then
                RaiseEvent PrintConsole("SVF exit to test-logic-reset enabled")
            Else
                RaiseEvent PrintConsole("SVF exit to test-logic-reset disabled")
            End If
            Return Nothing
        End Function

        Friend Function c_jtag_epc2_read(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            ProgressBar_Percent(0)
            Dim cbProgress As New JTAG_IF.EPC2_ProgressCallback(AddressOf ProgressBar_Percent)
            Dim e_data() As Byte = MAIN_FCUSB.JTAG_IF.EPC2_ReadBinary(cbProgress)
            ProgressBar_Percent(0)
            If e_data IsNot Nothing Then
                Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Data)
                sv.Value = e_data
                Return sv
            End If
            Return Nothing
        End Function

        Friend Function c_jtag_epc2_write(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim BootData() As Byte = arguments(0).Value
            Dim CfgData() As Byte = arguments(1).Value
            ProgressBar_Percent(0)
            Dim cbProgress As New JTAG_IF.EPC2_ProgressCallback(AddressOf ProgressBar_Percent)
            Dim result As Boolean = MAIN_FCUSB.JTAG_IF.EPC2_WriteBinary(BootData, CfgData, cbProgress)
            ProgressBar_Percent(0)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Bool)
            sv.Value = result
            Return sv
        End Function

        Friend Function c_jtag_epc2_erase(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim e_result As Boolean = MAIN_FCUSB.JTAG_IF.EPC2_Erase()
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Bool)
            sv.Value = e_result
            Return sv
        End Function

#End Region

#Region "BSDL"

        Friend Function c_bsdl_new(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim n As New BSDL_DEF
            n.PART_NAME = CStr(arguments(0).Value)
            Dim index_created As Integer = MAIN_FCUSB.JTAG_IF.BSDL_Add(n)
            Return New ScriptVariable(CurrentVars.GetNewName(), DataType.Integer, index_created)
        End Function

        Friend Function c_bsdl_find(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim param_name As String = arguments(0).Value
            Dim lib_ind As Integer = MAIN_FCUSB.JTAG_IF.BSDL_Find(param_name)
            Return New ScriptVariable(CurrentVars.GetNewName(), DataType.Integer, lib_ind)
        End Function

        Friend Function c_bsdl_param(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim param_name As String = arguments(0).Value
            Dim param_value As UInt32 = arguments(1).Value
            Dim result As Boolean = MAIN_FCUSB.JTAG_IF.BSDL_SetParamater(Index, param_name, param_value)
            Return New ScriptVariable(CurrentVars.GetNewName(), DataType.Bool, result)
        End Function

#End Region

#Region "Boundary Scan Programmer"

        Friend Function c_bsp_setup(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            If MAIN_FCUSB Is Nothing Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "FlashcatUSB Professional must be connected via USB"}
            End If
            If Not MAIN_FCUSB.HWBOARD = USB.FCUSB_BOARD.Professional_PCB5 Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "This command is only supported for FlashcatUSB Professional"}
            End If
            If Not MySettings.OPERATION_MODE = DeviceMode.JTAG Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "This command is only supported when in JTAG mode"}
            End If
            MAIN_FCUSB.JTAG_IF.BoundaryScan_Setup()
            Return Nothing
        End Function

        Friend Function c_bsp_init(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim flash_mode As Integer = 0 '0=Automatic, 1=X8_OVER_X16
            If arguments.Length = 1 Then flash_mode = CInt(arguments(0).Value)
            Dim result As Boolean = MAIN_FCUSB.JTAG_IF.BoundaryScan_Init(flash_mode)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName(), DataType.Bool)
            sv.Value = result
            Return sv
        End Function

        Friend Function c_bsp_addpin(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim pin_name As String = arguments(0).Value
            Dim pin_output As Integer = arguments(1).Value 'cell associated with the bidir or output cell
            Dim pin_control As Integer = arguments(2).Value  'cell associated with the control register bit
            Dim pin_input As Integer = -1 'cell associated with the input cell when output cell is not bidir
            If arguments.Length = 4 Then
                pin_input = arguments(3).Value
            End If
            MAIN_FCUSB.JTAG_IF.BoundaryScan_AddPin(pin_name, pin_output, pin_control, pin_input)
            Return Nothing
        End Function

        Friend Function c_bsp_setbsr(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim pin_output As Integer = CInt(arguments(0).Value)
            Dim pin_control As Integer = CInt(arguments(1).Value)
            Dim pin_level As Boolean = CBool(arguments(2).Value)
            MAIN_FCUSB.JTAG_IF.BoundaryScan_SetBSR(pin_output, pin_control, pin_level)
            Return Nothing
        End Function

        Friend Function c_bsp_writebsr(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            MAIN_FCUSB.JTAG_IF.BoundaryScan_WriteBSR()
            Return Nothing
        End Function

        Friend Function c_bsp_detect(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim result As Boolean = MAIN_FCUSB.JTAG_IF.BoundaryScan_Detect()
            Dim sv As New ScriptVariable(CurrentVars.GetNewName(), DataType.Bool)
            sv.Value = result
            Return sv
        End Function

#End Region

#Region "LOAD"

        Friend Function c_load_firmware(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Select Case MAIN_FCUSB.HWBOARD
                Case USB.FCUSB_BOARD.Professional_PCB5
                Case USB.FCUSB_BOARD.Mach1
                Case Else
                    Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Only available for PRO or MACH1"}
            End Select
            MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.FW_REBOOT, Nothing, &HFFFFFFFFUI)
            Return Nothing
        End Function

        Friend Function c_load_logic(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Select Case MAIN_FCUSB.HWBOARD
                Case USB.FCUSB_BOARD.Professional_PCB5
                    MAIN_FCUSB.LOGIC_SetVersion(&HFFFFFFFFUI)
                    FCUSBPRO_PCB5_Init(MAIN_FCUSB, MySettings.OPERATION_MODE)
                Case USB.FCUSB_BOARD.Mach1
                    MAIN_FCUSB.LOGIC_SetVersion(&HFFFFFFFFUI)
                    FCUSBMACH1_Init(MAIN_FCUSB, MySettings.OPERATION_MODE)
                Case Else
                    Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Only available for PRO or MACH1"}
            End Select
            Return Nothing
        End Function
        'Performs an erase of the logic
        Friend Function c_load_erase(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Select Case MAIN_FCUSB.HWBOARD
                Case USB.FCUSB_BOARD.Mach1
                    MACH1_FPGA_ERASE(MAIN_FCUSB)
            End Select
            Return Nothing
        End Function

        Friend Function c_load_bootloader(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Select Case MAIN_FCUSB.HWBOARD
                Case USB.FCUSB_BOARD.Professional_PCB5
                Case USB.FCUSB_BOARD.Mach1
                Case Else
                    Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Only available for PRO or MACH1"}
            End Select
            Dim bl_data() As Byte = arguments(0).Value
            If bl_data IsNot Nothing AndAlso bl_data.Length > 0 Then
                If MAIN_FCUSB.BootloaderUpdate(bl_data) Then
                    RaiseEvent PrintConsole("Bootloader successfully updated")
                    MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.FW_REBOOT, Nothing, &HFFFFFFFFUI)
                Else
                    RaiseEvent PrintConsole("Bootloader update was not successful")
                End If
            Else
                RaiseEvent PrintConsole("Error: Load.Bootloader requires data variable with valid data")
            End If
            Return Nothing
        End Function

#End Region

#Region "FLASH commands"

        Friend Function c_flash_add(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim flash_name As String = CStr(arguments(0).Value)
            Dim ID_MFG As Integer = CInt(arguments(1).Value)
            Dim ID_PART As Integer = CInt(arguments(2).Value)
            Dim flash_size As Integer = CInt(arguments(3).Value) 'Upgrade this to LONG in the future
            Dim flash_if As Integer = CInt(arguments(4).Value)
            Dim block_layout As Integer = CInt(arguments(5).Value)
            Dim prog_mode As Integer = CInt(arguments(6).Value)
            Dim delay_mode As Integer = CInt(arguments(7).Value)
            Dim new_mem_part As New FlashMemory.P_NOR(flash_name, ID_MFG, ID_PART, flash_size, flash_if, block_layout, prog_mode, delay_mode)
            FlashDatabase.FlashDB.Add(new_mem_part)
            Return Nothing
        End Function

#End Region

#Region "Parallel"

        Friend Function c_parallel_test(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim td As New Threading.Thread(AddressOf MAIN_FCUSB.PARALLEL_NOR_IF.PARALLEL_PORT_TEST)
            td.Start()
            Return Nothing
        End Function

        Friend Function c_parallel_command(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim cmd_addr As UInt32 = arguments(0).Value
            Dim cmd_data As UInt16 = CUShort(CUInt(arguments(1).Value) And &HFFFF)
            MAIN_FCUSB.PARALLEL_NOR_IF.WriteCommandData(cmd_addr, cmd_data)
            Return Nothing
        End Function

        Friend Function c_parallel_write(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim cmd_addr As UInt32 = arguments(0).Value
            Dim cmd_data As UInt16 = CUShort(CUInt(arguments(1).Value) And &HFFFF)
            MAIN_FCUSB.PARALLEL_NOR_IF.WriteMemoryAddress(cmd_addr, cmd_data)
            Return Nothing
        End Function

        Friend Function c_parallel_read(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim cmd_addr As UInt32 = arguments(0).Value
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.UInteger)
            sv.Value = MAIN_FCUSB.PARALLEL_NOR_IF.ReadMemoryAddress(cmd_addr)
            Return sv
        End Function

#End Region

#Region "Misc commands"

        Friend Function c_msgbox(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim message_text As String = ""
            If arguments(0).Data.VarType = DataType.Data Then
                Dim d() As Byte = arguments(0).Value
                message_text = "Data (" & Format(d.Length, "#,###") & " bytes)"
            Else
                message_text = CStr(arguments(0).Value)
            End If
            MainApp.PromptUser_Msg(message_text)
            Return Nothing
        End Function

        Friend Function c_writeline(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            If arguments(0).Data.VarType = DataType.Data Then
                Dim d() As Byte = arguments(0).Value
                Dim display_addr As Boolean = True
                If arguments.Length > 1 Then
                    display_addr = arguments(1).Value
                End If
                Dim bytesLeft As Integer = d.Length
                Dim i As Integer = 0
                Do Until bytesLeft = 0
                    Dim bytes_to_display As Integer = Math.Min(bytesLeft, 16)
                    Dim sec(bytes_to_display - 1) As Byte
                    Array.Copy(d, i, sec, 0, sec.Length)
                    Dim line_out As String = Utilities.Bytes.ToPaddedHexString(sec)
                    If display_addr Then
                        RaiseEvent PrintConsole("0x" & Hex(i).PadLeft(6, "0") & ":  " & line_out)
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

        Friend Function c_setstatus(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim message_text As String = arguments(0).Value
            RaiseEvent SetStatus(message_text)
            Return Nothing
        End Function

        Friend Function c_refresh(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim count As Integer = MEM_IF.DeviceCount
            For i = 0 To count - 1
                Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(i)
                mem_device.RefreshControls()
            Next
            Return Nothing
        End Function

        Friend Function c_sleep(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim wait_ms As Integer = arguments(0).Value
            Utilities.Sleep(wait_ms)
            Dim sw As New Stopwatch
            sw.Start()
            Do Until sw.ElapsedMilliseconds >= wait_ms
                Application.DoEvents() 'We do this as not to lock up the other threads or processes
                Utilities.Sleep(50)
            Loop
            Return Nothing
        End Function

        Friend Function c_verify(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim verify_bool As Boolean = arguments(0).Value
            MySettings.VERIFY_WRITE = verify_bool
            Return Nothing
        End Function

        Friend Function c_mode(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim rv As New ScriptVariable(CurrentVars.GetNewName, DataType.String)
            Select Case Me.CURRENT_DEVICE_MODE
                Case DeviceMode.SPI
                    rv.Value = "SPI"
                Case DeviceMode.SPI_EEPROM
                    rv.Value = "SPI (EEPROM)"
                Case DeviceMode.JTAG
                    rv.Value = "JTAG"
                Case DeviceMode.I2C_EEPROM
                    rv.Value = "I2C"
                Case DeviceMode.PNOR
                    rv.Value = "Parallel NOR"
                Case DeviceMode.PNAND
                    rv.Value = "Parallel NAND"
                Case DeviceMode.ONE_WIRE
                    rv.Value = "1-WIRE"
                Case DeviceMode.SPI_NAND
                    rv.Value = "SPI-NAND"
                Case DeviceMode.EPROM
                    rv.Value = "EPROM/OTP"
                Case DeviceMode.HyperFlash
                    rv.Value = "HyperFlash"
                Case DeviceMode.Microwire
                    rv.Value = "Microwire"
                Case DeviceMode.SQI
                    rv.Value = "QUAD SPI"
                Case DeviceMode.FWH
                    rv.Value = "Firmware HUB"
                Case Else
                    rv.Value = "Other"
            End Select
            Return rv
        End Function

        Friend Function c_ask(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim the_question As String = arguments(0).Value
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Bool)
            sv.Value = MainApp.PromptUser_Ask(the_question)
            Return sv
        End Function

        Friend Function c_endian(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim endian_mode As String = arguments(0).Value.ToString.ToUpper
            Select Case endian_mode
                Case "MSB"
                    MySettings.BIT_ENDIAN = BitEndianMode.BigEndian32
                Case "LSB"
                    MySettings.BIT_ENDIAN = BitEndianMode.LittleEndian32_16bit
                Case "LSB16"
                    MySettings.BIT_ENDIAN = BitEndianMode.LittleEndian32_16bit
                Case "LSB8"
                    MySettings.BIT_ENDIAN = BitEndianMode.LittleEndian32_8bit
                Case Else
                    MySettings.BIT_ENDIAN = BitEndianMode.BigEndian32
            End Select
            Return Nothing
        End Function

        Friend Function c_abort(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Me.ABORT_SCRIPT = True
            RaiseEvent PrintConsole("Aborting any running script")
            Return Nothing
        End Function

        Friend Function c_catalog(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
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

        Friend Function c_cpen(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim cp_en As Boolean = arguments(0).Value
            If Not MAIN_FCUSB.IS_CONNECTED Then
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "FlashcatUSB device is not connected"}
            End If
            Dim w_index As Integer = 0
            If cp_en Then w_index = 1
            MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.EXPIO_CPEN, Nothing, w_index)
            If cp_en Then
                RaiseEvent PrintConsole("CPEN pin set to HIGH")
            Else
                RaiseEvent PrintConsole("CPEN pin set to LOW")
            End If
            Return Nothing
        End Function

        Friend Function c_crc16(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim DataBytes() As Byte = arguments(0).Value
            Dim crc16_value As UInt32 = Utilities.CRC16.ComputeChecksum(DataBytes)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.UInteger)
            sv.Value = crc16_value
            Return sv
        End Function

        Friend Function c_crc32(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim DataBytes() As Byte = arguments(0).Value
            Dim crc32_value As UInt32 = Utilities.CRC32.ComputeChecksum(DataBytes)
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.UInteger)
            sv.Value = crc32_value
            Return sv
        End Function

        Friend Function c_cint(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim value As UInt32 = arguments(0).Value
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.Integer)
            sv.Value = CInt(value)
            Return sv
        End Function

        Friend Function c_cuint(arguments() As ScriptVariable, Index As UInt32) As ScriptVariable
            Dim value As Int32 = arguments(0).Value
            Dim sv As New ScriptVariable(CurrentVars.GetNewName, DataType.UInteger)
            sv.Value = CUInt(value)
            Return sv
        End Function

#End Region


#End Region


    End Class

    Friend Class ScriptCmd
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
                    If item.Name.ToUpper = main_fnc.ToUpper Then Return True
                Next
                For Each s In Me.Cmds
                    If s.cmd.ToUpper = main_fnc.ToUpper Then Return True
                Next
            Else
                For Each item In Nests
                    If item.Name.ToUpper = main_fnc.ToUpper Then
                        For Each s In item.Cmds
                            If s.cmd.ToUpper = sub_fnc.ToUpper Then Return True
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
                    If s.cmd.ToUpper = fnc_name.ToUpper Then
                        params = s.parameters
                        e = s.fnc
                        Return True
                    End If
                Next
            Else
                For Each item In Nests
                    If item.Name.ToUpper = fnc_name.ToUpper Then
                        For Each s In item.Cmds
                            If s.cmd.ToUpper = sub_fnc.ToUpper Then
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
        Public err_line As UInt32
        Public err_reason As String
        Public goto_label As String
    End Class

    Friend Class ScriptElementOperand
        Private MyParent As Processor
        Public OPERANDS As New List(Of ScriptElementOperandEntry)
        Public Property ERROR_MSG As String = ""

        Sub New(oParent As Processor, oper_text As String)
            Me.MyParent = oParent
            Parse(oper_text.Trim)
        End Sub

        Private Sub Parse(text_input As String)
            Do Until text_input.Equals("")
                If text_input.StartsWith("(") Then
                    Dim sub_section As String = FeedParameter(text_input)
                    Dim x As New ScriptElementOperandEntry(MyParent, ScriptElementDataType.SubItems)
                    x.SubOperands = New ScriptElementOperand(MyParent, sub_section)
                    If (x.SubOperands.ERROR_MSG = "") Then
                        OPERANDS.Add(x)
                    Else
                        Me.ERROR_MSG = x.SubOperands.ERROR_MSG : Exit Sub
                    End If
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
                    ElseIf FlashcatScript.IsDataArrayType(main_element) Then
                        Dim dr() As Byte = FlashcatScript.DataArrayTypeToBytes(main_element)
                        OPERANDS.Add(New ScriptElementOperandEntry(MyParent, DataType.Data, dr))
                    ElseIf (main_element.ToUpper.StartsWith("0X") AndAlso Utilities.IsDataType.Hex(main_element)) Then
                        Dim d() As Byte = Utilities.Bytes.FromHexString(main_element)
                        If (d.Length > 4) Then
                            OPERANDS.Add(New ScriptElementOperandEntry(MyParent, DataType.Data, d))
                        Else
                            OPERANDS.Add(New ScriptElementOperandEntry(MyParent, DataType.UInteger, Utilities.Bytes.ToUInt32(d)))
                        End If
                    ElseIf main_element.ToUpper.Equals("NOTHING") Then
                        OPERANDS.Add(New ScriptElementOperandEntry(MyParent, DataType.NULL))
                    ElseIf IsVariableArgument(main_element) Then
                        OPERANDS.Add(New ScriptElementOperandEntry(MyParent, ScriptElementDataType.Variable) With {.FUNC_NAME = main_element})
                    Else
                        If main_element.Equals("") Then
                            Me.ERROR_MSG = "Unknown function or command: " & text_input : Exit Sub
                        Else
                            Me.ERROR_MSG = "Unknown function or command: " & main_element : Exit Sub
                        End If
                    End If
                    If (Not text_input.Equals("")) Then
                        Dim oper_seperator As OperandOper = OperandOper.NOTSPECIFIED
                        If FeedOperator(text_input, oper_seperator) Then
                            OPERANDS.Add(New ScriptElementOperandEntry(MyParent, ScriptElementDataType.Operator) With {.Oper = oper_seperator})
                        Else
                            Me.ERROR_MSG = "Invalid operator" : Exit Sub
                        End If
                    End If
                End If
            Loop
        End Sub
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
                If Not Me.ERROR_MSG.Equals("") Then Return Nothing
            End If
            Return new_fnc
        End Function

        Private Function ParseEventInput(to_parse As String) As ScriptElementOperandEntry
            Dim new_evnt As New ScriptElementOperandEntry(MyParent, ScriptElementDataType.Event)
            Dim arguments As String = ""
            ParseToFunctionAndSub(to_parse, new_evnt.FUNC_NAME, Nothing, Nothing, arguments)
            If (Not arguments = "") Then
                new_evnt.FUNC_ARGS = ParseArguments(arguments)
                If Not Me.ERROR_MSG = "" Then Return Nothing
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
                If Not Me.ERROR_MSG = "" Then Return Nothing
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
                    Dim n As New ScriptElementOperand(MyParent, arg_builder.ToString)
                    If Not n.ERROR_MSG.Equals("") Then Me.ERROR_MSG = n.ERROR_MSG : Return Nothing
                    argument_list.Add(n) : arg_builder.Clear()
                End If
            Next
            If (arg_builder.Length > 0) Then
                Dim n As New ScriptElementOperand(MyParent, arg_builder.ToString)
                If Not n.ERROR_MSG.Equals("") Then Me.ERROR_MSG = n.ERROR_MSG : Return Nothing
                argument_list.Add(n)
            End If
            Return argument_list.ToArray
        End Function

        Friend Function CompileToVariable(ByRef exit_task As ExitMode) As ScriptVariable
            Dim current_var As ScriptVariable = Nothing
            Dim current_oper As OperandOper = OperandOper.NOTSPECIFIED
            Dim arg_count As Integer = OPERANDS.Count
            Dim x As Integer = 0
            Do While (x < arg_count)
                Dim new_var As ScriptVariable = Nothing
                If OPERANDS(x).EntryType = ScriptElementDataType.Operator Then
                    Me.ERROR_MSG = "Expected value to compute" : Return Nothing
                End If
                new_var = OPERANDS(x).Compile(exit_task)
                If Not OPERANDS(x).ERROR_MSG = "" Then Me.ERROR_MSG = OPERANDS(x).ERROR_MSG : Return Nothing
                If (Not current_oper = OperandOper.NOTSPECIFIED) Then
                    Dim result_var As ScriptVariable = CompileSVars(current_var, new_var, current_oper, Me.ERROR_MSG)
                    If Not Me.ERROR_MSG = "" Then Return Nothing
                    current_var = result_var
                Else
                    current_var = new_var
                End If
                x += 1 'increase pointer
                If (x < arg_count) Then 'There are more items
                    If Not OPERANDS(x).Oper = OperandOper.NOTSPECIFIED Then
                        current_oper = OPERANDS(x).Oper
                        x += 1
                        If Not (x < arg_count) Then Me.ERROR_MSG = "Statement ended in an operand operation" : Return Nothing
                    Else
                        Me.ERROR_MSG = "Expected an operand operation" : Return Nothing
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
        Public Property ERROR_MSG As String = ""

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
                            Return Utilities.Bytes.ToPaddedHexString(Me.FUNC_DATA.Value)
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
                Case ScriptElementDataType.Data
                    Dim new_sv As New ScriptVariable(MyParent.CurrentVars.GetNewName, Me.FUNC_DATA.VarType)
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
                                If Not Me.FUNC_ARGS(i).ERROR_MSG = "" Then Me.ERROR_MSG = Me.FUNC_ARGS(i).ERROR_MSG : Return Nothing
                                If ret IsNot Nothing Then input_vars.Add(ret)
                            Next
                        End If
                        Dim args_var() As ScriptVariable = input_vars.ToArray
                        If Not CheckFunctionArguments(fnc_params, args_var) Then Return Nothing
                        Try
                            Me.ERROR_MSG = ""
                            Dim func_index As UInt32 = 0
                            If IsNumeric(Me.FUNC_IND) Then
                                func_index = CUInt(FUNC_IND)
                            ElseIf Utilities.IsDataType.HexString(Me.FUNC_IND) Then
                                func_index = Utilities.HexToUInt(Me.FUNC_IND)
                            ElseIf MyParent.CurrentVars.IsVariable(Me.FUNC_IND) Then
                                Dim v As ScriptVariable = MyParent.CurrentVars.GetVariable(Me.FUNC_IND)
                                If v.Data.VarType = DataType.Integer Then
                                    func_index = CInt(MyParent.CurrentVars.GetVariable(Me.FUNC_IND).Value)
                                ElseIf v.Data.VarType = DataType.UInteger Then
                                    func_index = CUInt(MyParent.CurrentVars.GetVariable(Me.FUNC_IND).Value)
                                Else
                                    Me.ERROR_MSG = "Index " & Me.FUNC_IND & " must be either an Integer or UInteger" : Return Nothing
                                End If
                            Else
                                Me.ERROR_MSG = "Unable to evaluate index: " & Me.FUNC_IND : Return Nothing
                            End If
                            Dim result As ScriptVariable = Nothing
                            Try
                                result = fnc.DynamicInvoke(args_var, func_index)
                            Catch ex As Exception
                                result = New ScriptVariable("ERROR", DataType.FncError)
                                result.Value = FUNC_NAME & " function exception"
                            End Try
                            If result Is Nothing Then Return Nothing
                            If result.Data.VarType = DataType.FncError Then
                                Me.ERROR_MSG = result.Value : Return Nothing
                            End If
                            Return result
                        Catch ex As Exception
                            Me.ERROR_MSG = "Error executing function: " & Me.FUNC_NAME
                        End Try
                    Else
                        Me.ERROR_MSG = "Unknown function or sub procedure"
                    End If
                Case ScriptElementDataType.Event
                    Dim input_vars As New List(Of ScriptVariable)
                    If Me.FUNC_ARGS IsNot Nothing Then
                        For i = 0 To Me.FUNC_ARGS.Length - 1
                            Dim ret As ScriptVariable = Me.FUNC_ARGS(i).CompileToVariable(exit_task)
                            If Not Me.FUNC_ARGS(i).ERROR_MSG = "" Then Me.ERROR_MSG = Me.FUNC_ARGS(i).ERROR_MSG : Return Nothing
                            If ret IsNot Nothing Then input_vars.Add(ret)
                        Next
                    End If
                    Dim se As ScriptEvent = MyParent.GetScriptEvent(Me.FUNC_NAME)
                    If se Is Nothing Then
                        Me.ERROR_MSG = "Event does not exist: " & Me.FUNC_NAME : Return Nothing
                    End If
                    Dim n_sv As ScriptVariable = MyParent.ExecuteScriptEvent(se, input_vars.ToArray, exit_task)
                    Return n_sv
                Case ScriptElementDataType.Variable
                    Dim n_sv As ScriptVariable = MyParent.CurrentVars.GetVariable(Me.FUNC_NAME)
                    If n_sv.Data.VarType = DataType.NULL Then Return Nothing
                    If n_sv.Data.VarType = DataType.Data AndAlso Me.FUNC_ARGS IsNot Nothing Then
                        Try
                            If Me.FUNC_ARGS.Length = 1 Then
                                Dim data_index_var As ScriptVariable = Me.FUNC_ARGS(0).CompileToVariable(exit_task)
                                If data_index_var.Data.VarType = DataType.UInteger Then
                                    Dim data() As Byte = n_sv.Value
                                    Dim data_index As UInt32 = data_index_var.Value
                                    Dim new_sv As New ScriptVariable(MyParent.CurrentVars.GetNewName, DataType.UInteger)
                                    new_sv.Value = data(data_index)
                                    Return new_sv
                                End If
                            End If
                        Catch ex As Exception
                            Me.ERROR_MSG = "Error processing variable index value"
                        End Try
                    Else
                        Return n_sv
                    End If
                Case ScriptElementDataType.SubItems
                    Dim output_vars As ScriptVariable = SubOperands.CompileToVariable(exit_task)
                    If Not SubOperands.ERROR_MSG = "" Then Me.ERROR_MSG = SubOperands.ERROR_MSG : Return Nothing
                    Return output_vars
            End Select
            Return Nothing
        End Function

        Private Function CheckFunctionArguments(fnc_params() As CmdPrm, ByRef my_vars() As ScriptVariable) As Boolean
            Dim var_count As UInt32 = 0
            If my_vars Is Nothing OrElse my_vars.Length = 0 Then
                var_count = 0
            Else
                var_count = my_vars.Length
            End If
            If fnc_params Is Nothing AndAlso (Not var_count = 0) Then
                Me.ERROR_MSG = "Function " & Me.GetFuncString & ": arguments supplied but none are allowed"
                Return False
            ElseIf fnc_params IsNot Nothing Then
                For i = 0 To fnc_params.Length - 1
                    Select Case fnc_params(i)
                        Case CmdPrm.Integer
                            If (i >= var_count) Then
                                Me.ERROR_MSG = "Function " & Me.GetFuncString & ": argument " & (i + 1) & " requires an Integer type parameter" : Return Nothing
                            Else
                                If (my_vars(i).Data.VarType = DataType.UInteger) Then 'We can possibly auto convert data type
                                    Dim auto_con As UInt32 = CUInt(my_vars(i).Value)
                                    If auto_con <= Int32.MaxValue Then my_vars(i) = New ScriptVariable(my_vars(i).Name, DataType.Integer, CInt(auto_con))
                                End If
                                If Not my_vars(i).Data.VarType = DataType.Integer Then
                                    Me.ERROR_MSG = "Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs an Integer but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType) : Return Nothing
                                End If
                            End If
                        Case CmdPrm.UInteger
                            If (i >= var_count) Then
                                Me.ERROR_MSG = "Function " & Me.GetFuncString & ": argument " & (i + 1) & " requires an UInteger type parameter" : Return Nothing
                            Else
                                If (my_vars(i).Data.VarType = DataType.Integer) Then 'We can possibly auto convert data type
                                    Dim auto_con As Integer = CInt(my_vars(i).Value)
                                    If (auto_con >= 0) Then my_vars(i) = New ScriptVariable(my_vars(i).Name, DataType.UInteger, CUInt(auto_con))
                                End If
                                If (Not my_vars(i).Data.VarType = DataType.UInteger) Then
                                    Me.ERROR_MSG = "Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs an UInteger but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType) : Return Nothing
                                End If
                            End If
                        Case CmdPrm.String
                            If (i >= var_count) Then
                                Me.ERROR_MSG = "Function " & Me.GetFuncString & ": argument " & (i + 1) & " requires a String type parameter" : Return Nothing
                            Else
                                If Not my_vars(i).Data.VarType = DataType.String Then
                                    Me.ERROR_MSG = "Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs a String but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType) : Return Nothing
                                End If
                            End If
                        Case CmdPrm.Data
                            If (i >= var_count) Then
                                Me.ERROR_MSG = "Function " & Me.GetFuncString & ": argument " & (i + 1) & " requires a Data type parameter" : Return Nothing
                            Else
                                If my_vars(i).Data.VarType = DataType.Data Then
                                ElseIf my_vars(i).Data.VarType = DataType.UInteger Then
                                    Dim c As New ScriptVariable(my_vars(i).Name, DataType.Data)
                                    c.Value = Utilities.Bytes.FromUInt32(my_vars(i).Value)
                                    my_vars(i) = c
                                Else
                                    Me.ERROR_MSG = "Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs an Data but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType) : Return Nothing
                                End If
                            End If
                        Case CmdPrm.Bool
                            If (i >= var_count) Then
                                Me.ERROR_MSG = "Function " & Me.GetFuncString & ": argument " & (i + 1) & " requires a Bool type parameter" : Return Nothing
                            Else
                                If Not my_vars(i).Data.VarType = DataType.Bool Then
                                    Me.ERROR_MSG = "Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs an Bool but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType) : Return Nothing
                                End If
                            End If
                        Case CmdPrm.Any
                            If (i >= var_count) Then
                                Me.ERROR_MSG = "Function " & Me.GetFuncString & ": argument " & (i + 1) & " requires a parameter" : Return Nothing
                            End If
                        Case CmdPrm.Integer_Optional
                            If Not (i >= var_count) Then 'Only check if we have supplied this argument
                                If (my_vars(i).Data.VarType = DataType.UInteger) Then 'We can possibly auto convert data type
                                    Dim auto_con As UInt32 = CUInt(my_vars(i).Value)
                                    If auto_con <= Int32.MaxValue Then my_vars(i) = New ScriptVariable(my_vars(i).Name, DataType.Integer, CInt(auto_con))
                                End If
                                If Not my_vars(i).Data.VarType = DataType.Integer Then
                                    Me.ERROR_MSG = "Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs an Integer but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType) : Return Nothing
                                End If
                            End If
                        Case CmdPrm.UInteger_Optional
                            If Not (i >= var_count) Then 'Only check if we have supplied this argument
                                If (my_vars(i).Data.VarType = DataType.Integer) Then 'We can possibly auto convert data type
                                    Dim auto_con As Integer = CInt(my_vars(i).Value)
                                    If (auto_con >= 0) Then my_vars(i) = New ScriptVariable(my_vars(i).Name, DataType.UInteger, CUInt(auto_con))
                                End If
                                If Not my_vars(i).Data.VarType = DataType.UInteger Then
                                    Me.ERROR_MSG = "Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs an UInteger but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType) : Return Nothing
                                End If
                            End If
                        Case CmdPrm.String_Optional
                            If Not (i >= var_count) Then 'Only check if we have supplied this argument
                                If Not my_vars(i).Data.VarType = DataType.String Then
                                    Me.ERROR_MSG = "Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs an String but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType) : Return Nothing
                                End If
                            End If
                        Case CmdPrm.Data_Optional
                            If Not (i >= var_count) Then 'Only check if we have supplied this argument
                                If Not my_vars(i).Data.VarType = DataType.Data Then
                                    Me.ERROR_MSG = "Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs Data but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType) : Return Nothing
                                End If
                            End If
                        Case CmdPrm.Bool_Optional
                            If Not (i >= var_count) Then 'Only check if we have supplied this argument
                                If Not my_vars(i).Data.VarType = DataType.Bool Then
                                    Me.ERROR_MSG = "Function " & Me.GetFuncString & ": argument " & (i + 1) & " inputs Bool but was supplied a " & GetDataTypeString(my_vars(i).Data.VarType) : Return Nothing
                                End If
                            End If
                        Case CmdPrm.Any_Optional
                    End Select
                Next
            End If
            Return True
        End Function

        Friend Function GetFuncString() As String
            If Not FUNC_SUB = "" Then
                Return FUNC_NAME & "." & FUNC_SUB
            Else
                Return FUNC_NAME
            End If
        End Function

    End Class

    Friend Class ScriptFile
        Private MyParent As Processor
        Public TheScript As New List(Of ScriptLineElement)
        Public EventList As New List(Of String)

        Sub New()

        End Sub

        Public Sub Reset()
            TheScript.Clear()
        End Sub

        Friend Function LoadFile(oParent As FlashcatScript.Processor, lines() As String, ByRef ErrInd As Integer, ByRef ErrorMsg As String) As Boolean
            TheScript.Clear()
            MyParent = oParent
            Dim line_index(lines.Length - 1) As UInt32
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

        Friend Function ProcessText(lines() As String, line_index() As UInt32, ByRef ErrInd As UInt32, ByRef ErrorMsg As String) As ScriptLineElement()
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
                    If (Not cmd_line = "") Then
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
                            Dim normal As New ScriptElement(MyParent)
                            normal.INDEX = line_index(line_pointer)
                            normal.Parse(cmd_line, True)
                            If normal.HAS_ERROR Then
                                ErrorMsg = normal.ERROR_MSG
                                ErrInd = line_index(line_pointer)
                                Return Nothing
                            End If
                            If Not normal.TARGET_NAME = "" Then 'This element creates a new variable
                                MyParent.CurrentVars.AddExpected(normal.TARGET_NAME)
                            End If
                            Processed.Add(normal)
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

        Friend Function CreateIfCondition(ByRef Pointer As Integer, lines() As String, ind() As UInt32, ByRef ErrInd As UInt32, ByRef ErrorMsg As String) As ScriptLineElement
            Dim this_if As New ScriptCondition(Me.MyParent) 'Also loads NOT modifier
            this_if.INDEX = ind(Pointer)
            If Not this_if.Parse(lines(Pointer)) Then
                ErrInd = ind(Pointer)
                ErrorMsg = this_if.ERROR_MSG
                Return Nothing
            End If
            If this_if.CONDITION Is Nothing Then
                ErrInd = Pointer
                ErrorMsg = "IF condition is not valid"
                Return Nothing
            End If
            Dim IFMain As New List(Of String)
            Dim IfElse As New List(Of String)
            Dim IFMain_Index As New List(Of UInt32)
            Dim IfElse_Index As New List(Of UInt32)
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

        Friend Function CreateForLoop(ByRef Pointer As Integer, lines() As String, ind() As UInt32, ByRef ErrInd As UInt32, ByRef ErrorMsg As String) As ScriptLineElement
            Dim this_for As New ScriptLoop(Me.MyParent) 'Also loads NOT modifier
            this_for.INDEX = ind(Pointer)
            Dim success As Boolean = this_for.Parse(lines(Pointer))
            If Not success Then
                ErrInd = ind(Pointer)
                ErrorMsg = "FOR LOOP statement is not valid"
                Return Nothing
            End If
            Dim EndForTrigger As Boolean = False
            Dim level As Integer = 0
            Dim LoopMain As New List(Of String)
            Dim LoopMain_Index As New List(Of UInt32)
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

        Friend Function CreateGoto(ByRef Pointer As Integer, lines() As String, ind() As UInt32, ByRef ErrInd As UInt32, ByRef ErrorMsg As String) As ScriptLineElement
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

        Friend Function CreateExit(ByRef Pointer As Integer, lines() As String, ind() As UInt32, ByRef ErrInd As UInt32, ByRef ErrorMsg As String) As ScriptLineElement
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

        Friend Function CreateReturn(ByRef Pointer As Integer, lines() As String, ind() As UInt32, ByRef ErrInd As UInt32, ByRef ErrorMsg As String) As ScriptLineElement
            Dim this_return As New ScriptReturn(Me.MyParent)
            this_return.INDEX = ind(Pointer)
            Dim input As String = lines(Pointer)
            'Pointer += 1
            If input.ToUpper.StartsWith("RETURN ") Then input = input.Substring(7).Trim
            this_return.Parse(input)
            ErrorMsg = this_return.ERROR_MSG
            Return this_return
        End Function

        Friend Function CreateLabel(ByRef Pointer As Integer, lines() As String, ind() As UInt32, ByRef ErrInd As UInt32, ByRef ErrorMsg As String) As ScriptLineElement
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

        Friend Function CreateEvent(ByRef Pointer As Integer, lines() As String, ind() As UInt32, ByRef ErrInd As UInt32, ByRef ErrorMsg As String) As ScriptLineElement
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
            Dim EventBody_Index As New List(Of UInt32)
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

    Friend Enum ExitMode
        KeepRunning
        Leave
        LeaveEvent
        LeaveScript
        GotoLabel
    End Enum

    Friend Enum TargetOper
        [NONE] 'We are not going to create a SV
        [EQ] '=
        [ADD] '+=
        [SUB] '-=
    End Enum
    'what to do with two variables/functions
    Friend Enum OperandOper
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

    Friend Enum CmdPrm
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

    Friend Enum ScriptElementDataType
        [Data] 'Means the entry contains int,str,data,bool
        [Operator] 'Means this is a ADD/SUB/MULT/DIV etc.
        [SubItems] 'Means this contains a sub instance of entries
        [Variable]
        [Function]
        [Event]
    End Enum

    Friend Enum DataType
        [NULL]
        [Integer]
        [UInteger]
        [String]
        [Data]
        [Bool]
        [FncError]
    End Enum

    Friend Enum ScriptFileElementType
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

    Friend Class ScriptVariable
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
                Return Data.Value
            End Get
            Set(value As Object)
                Data.Value = value
            End Set
        End Property

        Public Overrides Function ToString() As String
            Return Me.Name & " = " & Me.Value.ToString
        End Function

    End Class

    Friend Class DataTypeObject
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
                        If new_value Then
                            InternalData(0) = 1
                        Else
                            InternalData(0) = 2
                        End If
                    Case DataType.Data
                        Me.InternalData = new_value
                End Select
            End Set
        End Property

    End Class



#Region "ScriptLineElements"

    Friend Class ScriptVariableManager
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

    Friend MustInherit Class ScriptLineElement
        Friend MyParent As Processor

        Public Property INDEX As UInt32 'This is the line index of this element
        Public Property ElementType As ScriptFileElementType '[IF_CONDITION] [FOR_LOOP] [LABEL] [GOTO] [EVENT] [ELEMENT]

        Sub New(oParent As FlashcatScript.Processor)
            MyParent = oParent
        End Sub

    End Class

    Friend Class ScriptElement
        Inherits ScriptLineElement

        Public ReadOnly Property HAS_ERROR As Boolean
            Get
                If ERROR_MSG.Equals("") Then Return False Else Return True
            End Get
        End Property
        Public Property ERROR_MSG As String = ""
        Friend Property TARGET_OPERATION As TargetOper = TargetOper.NONE 'What to do with the compiled variable
        Friend Property TARGET_NAME As String = "" 'This is the name of the variable to create
        Friend Property TARGET_INDEX As Integer = -1 'For DATA arrays, this is the index within the array
        Friend Property TARGET_VAR As String = "" 'Instead of INDEX, a variable (int) can be used instead

        Private OPERLIST As ScriptElementOperand '(Element)(+/-)(Element) etc.

        Sub New(oParent As Processor)
            MyBase.New(oParent)
            MyBase.ElementType = ScriptFileElementType.ELEMENT
        End Sub

        Friend Function Parse(to_parse As String, parse_target As Boolean) As Boolean
            to_parse = to_parse.Trim
            If parse_target Then LoadTarget(to_parse)
            Me.OPERLIST = New ScriptElementOperand(MyBase.MyParent, to_parse)
            If (Not OPERLIST.ERROR_MSG.Equals("")) Then
                Me.ERROR_MSG = OPERLIST.ERROR_MSG
                Return False
            End If
            Return True
        End Function
        'This parses the initial string to check for a var assignment
        Private Sub LoadTarget(ByRef to_parse As String)
            Dim str_out As String = ""
            For i = 0 To to_parse.Length - 1
                If ((to_parse.Length - i) > 2) AndAlso to_parse.Substring(i, 2) = "==" Then 'This is a compare
                    Exit Sub
                ElseIf ((to_parse.Length - i) > 2) AndAlso to_parse.Substring(i, 2) = "+=" Then
                    TARGET_OPERATION = TargetOper.ADD
                    TARGET_NAME = str_out.Trim
                    to_parse = to_parse.Substring(i + 2).Trim
                    Exit Sub
                ElseIf ((to_parse.Length - i) > 2) AndAlso to_parse.Substring(i, 2) = "-=" Then
                    TARGET_OPERATION = TargetOper.SUB
                    TARGET_NAME = str_out.Trim
                    to_parse = to_parse.Substring(i + 2).Trim
                    Exit Sub
                ElseIf to_parse.Substring(i, 1) = "=" Then
                    TARGET_OPERATION = TargetOper.EQ
                    Dim input As String = str_out.Trim
                    Dim arg As String = ""
                    ParseToFunctionAndSub(input, TARGET_NAME, Nothing, Nothing, arg)
                    to_parse = to_parse.Substring(i + 1).Trim
                    If (Not arg = "") Then
                        If IsNumeric(arg) Then
                            TARGET_INDEX = CUInt(arg) 'Fixed INDEX
                        ElseIf Utilities.IsDataType.HexString(arg) Then
                            TARGET_INDEX = Utilities.HexToUInt(arg) 'Fixed INDEX
                        ElseIf MyParent.CurrentVars.IsVariable(arg) AndAlso MyParent.CurrentVars.GetVariable(arg).Data.VarType = DataType.UInteger Then
                            TARGET_INDEX = -1
                            TARGET_VAR = arg
                        Else
                            Me.ERROR_MSG = "Target index must be able to evaluate to an integer"
                        End If
                    End If
                    Exit Sub
                ElseIf to_parse.Substring(i, 1) = "." Then
                    Exit Sub
                ElseIf to_parse.Substring(i, 1) = """" Then
                    Exit Sub
                ElseIf to_parse.Substring(i, 1) = ">" Then
                    Exit Sub
                ElseIf to_parse.Substring(i, 1) = "<" Then
                    Exit Sub
                ElseIf to_parse.Substring(i, 1) = "+" Then
                    Exit Sub
                ElseIf to_parse.Substring(i, 1) = "-" Then
                    Exit Sub
                ElseIf to_parse.Substring(i, 1) = "/" Then
                    Exit Sub
                ElseIf to_parse.Substring(i, 1) = "*" Then
                    Exit Sub
                ElseIf to_parse.Substring(i, 1) = "&" Then
                    Exit Sub
                ElseIf to_parse.Substring(i, 1) = "|" Then
                    Exit Sub
                Else
                    str_out &= to_parse.Substring(i, 1)
                End If
            Next
        End Sub

        Friend Function Compile(ByRef exit_task As ExitMode) As ScriptVariable
            Dim sv As ScriptVariable = OPERLIST.CompileToVariable(exit_task)
            Me.ERROR_MSG = OPERLIST.ERROR_MSG
            Return sv
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

        Public ERROR_MSG As String = ""

        Sub New(oParent As Processor)
            MyBase.New(oParent)
            MyBase.ElementType = ScriptFileElementType.IF_CONDITION
        End Sub

        Friend Function Parse(input As String) As Boolean
            Try
                If input.ToUpper.StartsWith("IF ") Then input = input.Substring(3).Trim
                Me.NOT_MODIFIER = False 'Indicates the not modifier is being used
                If input.ToUpper.StartsWith("NOT") Then
                    Me.NOT_MODIFIER = True
                    input = input.Substring(3).Trim
                End If
                CONDITION = New ScriptElement(MyBase.MyParent)
                CONDITION.Parse(input, False)
                Me.ERROR_MSG = CONDITION.ERROR_MSG
                If Not Me.ERROR_MSG = "" Then Return False
                Return True
            Catch ex As Exception
                Return False
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

        Friend Function Parse(input As String) As Boolean
            Try
                If input.ToUpper.StartsWith("FOR ") Then input = input.Substring(4).Trim
                If input.StartsWith("(") AndAlso input.EndsWith(")") Then
                    input = input.Substring(1, input.Length - 2)
                    Me.VAR_NAME = FeedWord(input, {"="})
                    If Me.VAR_NAME = "" Then Return False
                    input = input.Substring(1).Trim
                    Dim first_part As String = FeedElement(input)
                    input = input.Trim
                    If input = "" Then Return False 'More info needed
                    Dim to_part As String = FeedElement(input)
                    input = input.Trim
                    If input = "" Then Return False 'More info needed
                    If Not to_part.ToUpper = "TO" Then Return False
                    LOOPSTART_OPER = New ScriptElementOperand(MyBase.MyParent, first_part)
                    LOOPSTOP_OPER = New ScriptElementOperand(MyBase.MyParent, input)
                    If (Not LOOPSTART_OPER.ERROR_MSG = "") Then Return False
                    If (Not LOOPSTOP_OPER.ERROR_MSG = "") Then Return False
                    Return True
                Else
                    Return False
                End If
            Catch ex As Exception
                Return False
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
                Me.START_IND = sv1.Value
                Me.END_IND = sv2.Value
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

        Sub New(oParent As FlashcatScript.Processor)
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

        Sub New(oParent As FlashcatScript.Processor)
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

        Sub New(oParent As FlashcatScript.Processor)
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

        Sub New(oParent As FlashcatScript.Processor)
            MyBase.New(oParent)
            MyBase.ElementType = ScriptFileElementType.RETURN
        End Sub

        Friend Function Parse(to_parse As String) As Boolean
            to_parse = to_parse.Trim
            OPERLIST = New ScriptElementOperand(MyBase.MyParent, to_parse)
            If (Not OPERLIST.ERROR_MSG = "") Then
                Me.ERROR_MSG = OPERLIST.ERROR_MSG
                Return False
            End If
            Return True
        End Function

        Friend Function Compile(ByRef exit_task As ExitMode) As ScriptVariable
            Dim sv As ScriptVariable = OPERLIST.CompileToVariable(exit_task)
            Me.ERROR_MSG = OPERLIST.ERROR_MSG
            Return sv
        End Function

        Public Overrides Function ToString() As String
            Return "RETURN " & OPERLIST.ToString
        End Function

    End Class

    Friend Class ScriptEvent
        Inherits ScriptLineElement

        Sub New(oParent As FlashcatScript.Processor)
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
                Dim t() As String = input.Split(";")
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
                Dim t() As String = input.Split(";")
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
                Dim c As Char = Mid(input, i + 1, 1)
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
        Friend Function CompileSVars(var1 As ScriptVariable, var2 As ScriptVariable, oper As OperandOper, ByRef error_reason As String) As ScriptVariable
            Try
                If oper = OperandOper.AND Or oper = OperandOper.OR Then
                    If Not var1.Data.VarType = DataType.Bool Then
                        error_reason = "OR / AND bitwise operators only valid for Bool data types" : Return Nothing
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
                                    error_reason = "Operand data type mismatch" : Return Nothing
                                End If
                            Case DataType.UInteger
                                If var2.Data.VarType = DataType.UInteger Then
                                    Return New ScriptVariable("RESULT", DataType.UInteger, CUInt(var1.Value) + CUInt(var2.Value))
                                ElseIf var2.Data.VarType = DataType.UInteger Then
                                    Return New ScriptVariable("RESULT", DataType.UInteger, CUInt(var1.Value) + CInt(var2.Value))
                                Else
                                    error_reason = "Operand data type mismatch" : Return Nothing
                                End If
                            Case DataType.String
                                Dim new_result As New ScriptVariable("RESULT", DataType.String)
                                If var2.Data.VarType = DataType.UInteger Then
                                    new_result.Value = CStr(var1.Value) & CUInt(var2.Value).ToString
                                ElseIf var2.Data.VarType = DataType.String Then
                                    new_result.Value = CStr(var1.Value) & CStr(var2.Value)
                                Else
                                    error_reason = "Operand data type mismatch" : Return Nothing
                                End If
                                Return new_result
                            Case DataType.Data
                                Dim new_result As New ScriptVariable("RESULT", DataType.Data)
                                If Not var1.Data.VarType = var2.Data.VarType Then error_reason = "Operand data type mismatch" : Return Nothing
                                Dim data1() As Byte = var1.Value
                                Dim data2() As Byte = var2.Value
                                Dim new_size As Integer = data1.Length + data2.Length
                                Dim new_data(new_size) As Byte
                                Array.Copy(data1, 0, new_data, 0, data1.Length)
                                Array.Copy(data2, 0, new_data, data1.Length, data2.Length)
                                new_result.Value = new_data
                                Return new_result
                            Case DataType.Bool
                                error_reason = "Add operand not valid for Bool data type" : Return Nothing
                        End Select
                    Case OperandOper.SUB
                        Select Case var1.Data.VarType
                            Case DataType.Integer
                                If var2.Data.VarType = DataType.Integer Then
                                    Return New ScriptVariable("RESULT", DataType.Integer, CInt(var1.Value) - CInt(var2.Value))
                                ElseIf var2.Data.VarType = DataType.UInteger Then
                                    Return New ScriptVariable("RESULT", DataType.Integer, CInt(var1.Value) - CUInt(var2.Value))
                                Else
                                    error_reason = "Operand data type mismatch" : Return Nothing
                                End If
                            Case DataType.UInteger
                                If var2.Data.VarType = DataType.UInteger Then
                                    Return New ScriptVariable("RESULT", DataType.UInteger, CUInt(var1.Value) - CUInt(var2.Value))
                                ElseIf var2.Data.VarType = DataType.UInteger Then
                                    Return New ScriptVariable("RESULT", DataType.UInteger, CUInt(var1.Value) - CInt(var2.Value))
                                Else
                                    error_reason = "Operand data type mismatch" : Return Nothing
                                End If
                            Case Else
                                Dim new_result As New ScriptVariable("RESULT", DataType.UInteger)
                                If Not var1.Data.VarType = var2.Data.VarType Then error_reason = "Operand data type mismatch" : Return Nothing
                                If (Not var1.Data.VarType = DataType.UInteger) Then error_reason = "Subtract operand only valid for Integer data type" : Return Nothing
                                new_result.Value = CUInt(var1.Value) - CUInt(var2.Value)
                                Return new_result
                        End Select
                    Case OperandOper.DIV
                        If Not var1.Data.VarType = var2.Data.VarType Then error_reason = "Operand data type mismatch" : Return Nothing
                        If var1.Data.VarType = DataType.Integer Then
                            Dim new_result As New ScriptVariable("RESULT", DataType.Integer)
                            new_result.Value = CInt(var1.Value) / CInt(var2.Value)
                            Return new_result
                        ElseIf var2.Data.VarType = DataType.UInteger Then
                            Dim new_result As New ScriptVariable("RESULT", DataType.UInteger)
                            new_result.Value = CUInt(var1.Value) / CUInt(var2.Value)
                            Return new_result
                        Else
                            error_reason = "Division operand only valid for Integer or UInteger data type" : Return Nothing
                        End If
                    Case OperandOper.MULT
                        If Not var1.Data.VarType = var2.Data.VarType Then error_reason = "Operand data type mismatch" : Return Nothing
                        If var1.Data.VarType = DataType.Integer Then
                            Dim new_result As New ScriptVariable("RESULT", DataType.Integer)
                            new_result.Value = CInt(var1.Value) * CInt(var2.Value)
                            Return new_result
                        ElseIf var2.Data.VarType = DataType.UInteger Then
                            Dim new_result As New ScriptVariable("RESULT", DataType.UInteger)
                            new_result.Value = CUInt(var1.Value) * CUInt(var2.Value)
                            Return new_result
                        Else
                            error_reason = "Mulitple operand only valid for Integer or UInteger data type" : Return Nothing
                        End If
                    Case OperandOper.S_LEFT
                        Dim new_result As New ScriptVariable("RESULT", DataType.Integer)
                        If Not var1.Data.VarType = var2.Data.VarType Then error_reason = "Operand data type mismatch" : Return Nothing
                        If (Not var1.Data.VarType = DataType.Integer) Then error_reason = "Shift-left operand only valid for Integer data type" : Return Nothing
                        Dim shift_value As Int32 = var2.Value
                        If shift_value > 31 Then error_reason = "Shift-left value is greater than 31-bits" : Return Nothing
                        new_result.Value = CUInt(var1.Value) << shift_value
                        Return new_result
                    Case OperandOper.S_RIGHT
                        Dim new_result As New ScriptVariable("RESULT", DataType.Integer)
                        If Not var1.Data.VarType = var2.Data.VarType Then error_reason = "Operand data type mismatch" : Return Nothing
                        If (Not var1.Data.VarType = DataType.Integer) Then error_reason = "Shift-right operand only valid for Integer data type" : Return Nothing
                        Dim shift_value As Int32 = var2.Value
                        If shift_value > 31 Then error_reason = "Shift-right value is greater than 31-bits" : Return Nothing
                        new_result.Value = CUInt(var1.Value) >> shift_value
                        Return new_result
                    Case OperandOper.AND 'We already checked to make sure these are BOOL
                        Dim new_result As New ScriptVariable("RESULT", DataType.Bool)
                        If Not var1.Data.VarType = var2.Data.VarType Then error_reason = "Operand data type mismatch" : Return Nothing
                        new_result.Value = CBool(var1.Value) And CBool(var2.Value)
                        Return new_result
                    Case OperandOper.OR 'We already checked to make sure these are BOOL
                        Dim new_result As New ScriptVariable("RESULT", DataType.Bool)
                        If Not var1.Data.VarType = var2.Data.VarType Then error_reason = "Operand data type mismatch" : Return Nothing
                        new_result.Value = CBool(var1.Value Or var2.Value)
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
                            If Not var1.Data.VarType = var2.Data.VarType Then error_reason = "Operand data type mismatch" : Return Nothing
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
                                Dim d1() As Byte = var1.Value
                                Dim d2() As Byte = var2.Value
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
                        If Not var1.Data.VarType = var2.Data.VarType Then error_reason = "Operand data type mismatch" : Return Nothing
                        If (var1.Data.VarType = DataType.Integer) Then
                            new_result.Value = (CInt(var1.Value) < CInt(var2.Value))
                        ElseIf (var1.Data.VarType = DataType.UInteger) Then
                            new_result.Value = (CUInt(var1.Value) < CUInt(var2.Value))
                        Else
                            error_reason = "Less-than compare operand only valid for Integer/UInteger data type" : Return Nothing
                        End If
                        Return new_result
                    Case OperandOper.GRT_THAN 'Boolean compare operators
                        Dim new_result As New ScriptVariable("RESULT", DataType.Bool)
                        If Not var1.Data.VarType = var2.Data.VarType Then error_reason = "Operand data type mismatch" : Return Nothing
                        If (var1.Data.VarType = DataType.Integer) Then
                            new_result.Value = (CInt(var1.Value) > CInt(var2.Value))
                        ElseIf (var1.Data.VarType = DataType.UInteger) Then
                            new_result.Value = (CUInt(var1.Value) > CUInt(var2.Value))
                        Else
                            error_reason = "Greater-than compare operand only valid for Integer/UInteger data type" : Return Nothing
                        End If
                        Return new_result
                End Select
            Catch ex As Exception
                error_reason = "Error compiling operands"
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
