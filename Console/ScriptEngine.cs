// COPYRIGHT EMBEDDEDCOMPUTERS.NET 2020 - ALL RIGHTS RESERVED
// CONTACT EMAIL: support@embeddedcomputers.net
// ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
// INFO: This class is the entire scripting engine which can control the software
// via user supplied text files. The langauge format is similar to BASIC.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace FlashcatScript {
    public class Processor : IDisposable {
        public const int Build = 306;
        internal ScriptCmd CmdFunctions = new ScriptCmd();
        internal ScriptFile CurrentScript = new ScriptFile();
        internal ScriptVariableManager CurrentVars = new ScriptVariableManager();

        public delegate void UpdateFunction_Progress(int percent);
        public delegate void UpdateFunction_Status(string msg);

        private delegate ScriptVariable ScriptFunction(ScriptVariable[] arguments, uint Index);

        public DeviceMode CURRENT_DEVICE_MODE { get; set; }

        private bool script_is_running = false;

        public event PrintConsoleEventHandler PrintConsole;

        public delegate void PrintConsoleEventHandler(string msg);

        public event SetStatusEventHandler SetStatus;

        public delegate void SetStatusEventHandler(string msg);

        private bool ABORT_SCRIPT = false;

        public bool IsRunning {
            get {
                return script_is_running;
            }
        }

        public Processor() {
            var STR_CMD = new ScriptCmd("STRING");
            STR_CMD.Add("upper", new[] { CmdPrm.String }, new ScriptFunction(c_str_upper));
            STR_CMD.Add("lower", new[] { CmdPrm.String }, new ScriptFunction(c_str_lower));
            STR_CMD.Add("hex", new[] { CmdPrm.Integer }, new ScriptFunction(c_str_hex));
            STR_CMD.Add("length", new[] { CmdPrm.String }, new ScriptFunction(c_str_length));
            STR_CMD.Add("toint", new[] { CmdPrm.String }, new ScriptFunction(c_str_toint));
            STR_CMD.Add("fromint", new[] { CmdPrm.Integer }, new ScriptFunction(c_str_fromint));
            CmdFunctions.AddNest(STR_CMD);
            var DATA_CMD = new ScriptCmd("DATA");
            DATA_CMD.Add("new", new[] { CmdPrm.Integer, CmdPrm.Data }, new ScriptFunction(c_data_new));
            DATA_CMD.Add("fromhex", new[] { CmdPrm.String }, new ScriptFunction(c_data_fromhex));
            DATA_CMD.Add("compare", new[] { CmdPrm.Data }, new ScriptFunction(c_data_compare));
            DATA_CMD.Add("length", new[] { CmdPrm.Data }, new ScriptFunction(c_data_length));
            DATA_CMD.Add("resize", new[] { CmdPrm.Data, CmdPrm.Integer, CmdPrm.Integer_Optional }, new ScriptFunction(c_data_resize));
            DATA_CMD.Add("hword", new[] { CmdPrm.Data, CmdPrm.Integer }, new ScriptFunction(c_data_hword));
            DATA_CMD.Add("word", new[] { CmdPrm.Data, CmdPrm.Integer }, new ScriptFunction(c_data_word));
            DATA_CMD.Add("tostr", new[] { CmdPrm.Data }, new ScriptFunction(c_data_tostr));
            DATA_CMD.Add("copy", new[] { CmdPrm.Data }, new ScriptFunction(c_data_copy));
            DATA_CMD.Add("combine", new[] { CmdPrm.Data }, new ScriptFunction(c_data_combine));
            CmdFunctions.AddNest(DATA_CMD);
            var IO_CMD = new ScriptCmd("IO");
            IO_CMD.Add("open", new[] { CmdPrm.String_Optional, CmdPrm.String_Optional }, new ScriptFunction(c_io_open));
            IO_CMD.Add("save", new[] { CmdPrm.Data, CmdPrm.String_Optional, CmdPrm.String_Optional }, new ScriptFunction(c_io_save));
            IO_CMD.Add("read", new[] { CmdPrm.String }, new ScriptFunction(c_io_read));
            IO_CMD.Add("write", new[] { CmdPrm.Data, CmdPrm.String }, new ScriptFunction(c_io_write));
            CmdFunctions.AddNest(IO_CMD);
            var MEM_CMD = new ScriptCmd("MEMORY");
            MEM_CMD.Add("name", null, new ScriptFunction(c_mem_name));
            MEM_CMD.Add("size", null, new ScriptFunction(c_mem_size));
            MEM_CMD.Add("write", new[] { CmdPrm.Data, CmdPrm.UInteger, CmdPrm.Integer_Optional }, new ScriptFunction(c_mem_write));
            MEM_CMD.Add("read", new[] { CmdPrm.UInteger, CmdPrm.Integer, CmdPrm.Bool_Optional }, new ScriptFunction(c_mem_read));
            MEM_CMD.Add("readstring", new[] { CmdPrm.Integer }, new ScriptFunction(c_mem_readstring));
            MEM_CMD.Add("readverify", new[] { CmdPrm.Integer, CmdPrm.Integer }, new ScriptFunction(c_mem_readverify));
            MEM_CMD.Add("sectorcount", null, new ScriptFunction(c_mem_sectorcount));
            MEM_CMD.Add("sectorsize", new[] { CmdPrm.Integer }, new ScriptFunction(c_mem_sectorsize));
            MEM_CMD.Add("erasesector", new[] { CmdPrm.Integer }, new ScriptFunction(c_mem_erasesector));
            MEM_CMD.Add("erasebulk", null, new ScriptFunction(c_mem_erasebulk));
            MEM_CMD.Add("exist", null, new ScriptFunction(c_mem_exist));
            CmdFunctions.AddNest(MEM_CMD);
            var TAB_CMD = new ScriptCmd("TAB");
            TAB_CMD.Add("create", new[] { CmdPrm.String }, new ScriptFunction(c_tab_create));
            TAB_CMD.Add("addgroup", new[] { CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer }, new ScriptFunction(c_tab_addgroup));
            TAB_CMD.Add("addbox", new[] { CmdPrm.String, CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer }, new ScriptFunction(c_tab_addbox));
            TAB_CMD.Add("addtext", new[] { CmdPrm.String, CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer }, new ScriptFunction(c_tab_addtext));
            TAB_CMD.Add("addimage", new[] { CmdPrm.String, CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer }, new ScriptFunction(c_tab_addimage));
            TAB_CMD.Add("addbutton", new[] { CmdPrm.String, CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer }, new ScriptFunction(c_tab_addbutton));
            TAB_CMD.Add("addprogress", new[] { CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer }, new ScriptFunction(c_tab_addprogress));
            TAB_CMD.Add("remove", new[] { CmdPrm.String }, new ScriptFunction(c_tab_remove));
            TAB_CMD.Add("settext", new[] { CmdPrm.String, CmdPrm.String }, new ScriptFunction(c_tab_settext));
            TAB_CMD.Add("gettext", new[] { CmdPrm.String }, new ScriptFunction(c_tab_gettext));
            TAB_CMD.Add("buttondisable", new[] { CmdPrm.String_Optional }, new ScriptFunction(c_tab_buttondisable));
            TAB_CMD.Add("buttonenable", new[] { CmdPrm.String_Optional }, new ScriptFunction(c_tab_buttonenable));
            CmdFunctions.AddNest(TAB_CMD);
            var SPI_CMD = new ScriptCmd("SPI");
            SPI_CMD.Add("clock", new[] { CmdPrm.Integer }, new ScriptFunction(c_spi_clock));
            SPI_CMD.Add("mode", new[] { CmdPrm.Integer }, new ScriptFunction(c_spi_mode));
            SPI_CMD.Add("database", new[] { CmdPrm.Bool_Optional }, new ScriptFunction(c_spi_database));
            SPI_CMD.Add("getsr", new[] { CmdPrm.Integer_Optional }, new ScriptFunction(c_spi_getsr));
            SPI_CMD.Add("setsr", new[] { CmdPrm.Data }, new ScriptFunction(c_spi_setsr));
            SPI_CMD.Add("writeread", new[] { CmdPrm.Data, CmdPrm.Integer_Optional }, new ScriptFunction(c_spi_writeread));
            SPI_CMD.Add("prog", new[] { CmdPrm.Integer }, new ScriptFunction(c_spi_prog)); // Undocumented
            CmdFunctions.AddNest(SPI_CMD);
            var JTAG_CMD = new ScriptCmd("JTAG");
            JTAG_CMD.Add("idcode", null, new ScriptFunction(c_jtag_idcode));
            JTAG_CMD.Add("config", new[] { CmdPrm.String_Optional }, new ScriptFunction(c_jtag_config));
            JTAG_CMD.Add("select", new[] { CmdPrm.Integer }, new ScriptFunction(c_jtag_select));
            JTAG_CMD.Add("print", null, new ScriptFunction(c_jtag_print));
            JTAG_CMD.Add("clear", null, new ScriptFunction(c_jtag_clear));
            JTAG_CMD.Add("set", new[] { CmdPrm.Integer, CmdPrm.String }, new ScriptFunction(c_jtag_set));
            JTAG_CMD.Add("add", new[] { CmdPrm.String }, new ScriptFunction(c_jtag_add));
            JTAG_CMD.Add("validate", null, new ScriptFunction(c_jtag_validate));
            JTAG_CMD.Add("writeword", new[] { CmdPrm.UInteger, CmdPrm.UInteger }, new ScriptFunction(c_jtag_write32));
            JTAG_CMD.Add("readword", new[] { CmdPrm.UInteger }, new ScriptFunction(c_jtag_read32));
            JTAG_CMD.Add("control", new[] { CmdPrm.UInteger }, new ScriptFunction(c_jtag_control));
            JTAG_CMD.Add("memoryinit", new[] { CmdPrm.String, CmdPrm.UInteger_Optional, CmdPrm.UInteger_Optional }, new ScriptFunction(c_jtag_memoryinit));
            JTAG_CMD.Add("debug", new[] { CmdPrm.Bool }, new ScriptFunction(c_jtag_debug));
            JTAG_CMD.Add("cpureset", null, new ScriptFunction(c_jtag_cpureset));
            JTAG_CMD.Add("runsvf", new[] { CmdPrm.Data }, new ScriptFunction(c_jtag_runsvf));
            JTAG_CMD.Add("runxsvf", new[] { CmdPrm.Data }, new ScriptFunction(c_jtag_runxsvf));
            JTAG_CMD.Add("shiftdr", new[] { CmdPrm.Data, CmdPrm.Integer, CmdPrm.Bool_Optional }, new ScriptFunction(c_jtag_shiftdr));
            JTAG_CMD.Add("shiftir", new[] { CmdPrm.Data, CmdPrm.Bool_Optional }, new ScriptFunction(c_jtag_shiftir));
            JTAG_CMD.Add("shiftout", new[] { CmdPrm.Data, CmdPrm.Integer, CmdPrm.Bool_Optional }, new ScriptFunction(c_jtag_shiftout));
            JTAG_CMD.Add("tapreset", null, new ScriptFunction(c_jtag_tapreset));
            JTAG_CMD.Add("state", new[] { CmdPrm.String }, new ScriptFunction(c_jtag_state));
            JTAG_CMD.Add("graycode", new[] { CmdPrm.Integer, CmdPrm.Bool_Optional }, new ScriptFunction(c_jtag_graycode));
            JTAG_CMD.Add("setdelay", new[] { CmdPrm.Integer, CmdPrm.Integer }, new ScriptFunction(c_jtag_setdelay)); // Legacy support
            JTAG_CMD.Add("exitstate", new[] { CmdPrm.Bool }, new ScriptFunction(c_jtag_exitstate)); // SVF player option
            JTAG_CMD.Add("epc2_read", null, new ScriptFunction(c_jtag_epc2_read));
            JTAG_CMD.Add("epc2_write", new[] { CmdPrm.Data, CmdPrm.Data }, new ScriptFunction(c_jtag_epc2_write));
            JTAG_CMD.Add("epc2_erase", null, new ScriptFunction(c_jtag_epc2_erase));
            CmdFunctions.AddNest(JTAG_CMD);
            var BSDL_CMD = new ScriptCmd("BSDL");
            BSDL_CMD.Add("new", new[] { CmdPrm.String }, new ScriptFunction(c_bsdl_new));
            BSDL_CMD.Add("find", new[] { CmdPrm.String }, new ScriptFunction(c_bsdl_find));
            BSDL_CMD.Add("parameter", new[] { CmdPrm.String, CmdPrm.UInteger }, new ScriptFunction(c_bsdl_param));
            CmdFunctions.AddNest(BSDL_CMD);
            var JTAG_BSP = new ScriptCmd("BoundaryScan");
            JTAG_BSP.Add("setup", null, new ScriptFunction(c_bsp_setup));
            JTAG_BSP.Add("init", new[] { CmdPrm.Integer_Optional }, new ScriptFunction(c_bsp_init));
            JTAG_BSP.Add("addpin", new[] { CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer_Optional }, new ScriptFunction(c_bsp_addpin));
            JTAG_BSP.Add("setbsr", new[] { CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Bool }, new ScriptFunction(c_bsp_setbsr));
            JTAG_BSP.Add("writebsr", null, new ScriptFunction(c_bsp_writebsr));
            JTAG_BSP.Add("detect", null, new ScriptFunction(c_bsp_detect));
            CmdFunctions.AddNest(JTAG_BSP);
            var PAR_CMD = new ScriptCmd("parallel");
            PAR_CMD.Add("test", null, new ScriptFunction(c_parallel_test));
            PAR_CMD.Add("command", new[] { CmdPrm.UInteger, CmdPrm.UInteger }, new ScriptFunction(c_parallel_command));
            PAR_CMD.Add("write", new[] { CmdPrm.UInteger, CmdPrm.UInteger }, new ScriptFunction(c_parallel_write));
            PAR_CMD.Add("read", new[] { CmdPrm.UInteger }, new ScriptFunction(c_parallel_read));
            CmdFunctions.AddNest(PAR_CMD);
            var LOADOPT = new ScriptCmd("load"); // Undocumented
            LOADOPT.Add("firmware", null, new ScriptFunction(c_load_firmware));
            LOADOPT.Add("logic", null, new ScriptFunction(c_load_logic));
            LOADOPT.Add("erase", null, new ScriptFunction(c_load_erase));
            LOADOPT.Add("bootloader", new[] { CmdPrm.Data }, new ScriptFunction(c_load_bootloader));
            CmdFunctions.AddNest(LOADOPT);
            var FLSHOPT = new ScriptCmd("flash"); // Undocumented
            var del_add = new ScriptFunction(c_flash_add);
            FLSHOPT.Add("add", new[] { CmdPrm.String, CmdPrm.UInteger, CmdPrm.UInteger, CmdPrm.UInteger, CmdPrm.UInteger, CmdPrm.UInteger, CmdPrm.UInteger, CmdPrm.UInteger }, del_add);
            CmdFunctions.AddNest(FLSHOPT);
            // Generic functions
            CmdFunctions.Add("writeline", new[] { CmdPrm.Any, CmdPrm.Bool_Optional }, new ScriptFunction(c_writeline));
            CmdFunctions.Add("print", new[] { CmdPrm.Any, CmdPrm.Bool_Optional }, new ScriptFunction(c_writeline));
            CmdFunctions.Add("msgbox", new[] { CmdPrm.Any }, new ScriptFunction(c_msgbox));
            CmdFunctions.Add("status", new[] { CmdPrm.String }, new ScriptFunction(c_setstatus));
            CmdFunctions.Add("refresh", null, new ScriptFunction(c_refresh));
            CmdFunctions.Add("sleep", new[] { CmdPrm.Integer }, new ScriptFunction(c_sleep));
            CmdFunctions.Add("verify", new[] { CmdPrm.Bool }, new ScriptFunction(c_verify));
            CmdFunctions.Add("mode", null, new ScriptFunction(c_mode));
            CmdFunctions.Add("ask", new[] { CmdPrm.String }, new ScriptFunction(c_ask));
            CmdFunctions.Add("endian", new[] { CmdPrm.String }, new ScriptFunction(c_endian));
            CmdFunctions.Add("abort", null, new ScriptFunction(c_abort));
            CmdFunctions.Add("catalog", null, new ScriptFunction(c_catalog));
            CmdFunctions.Add("cpen", new[] { CmdPrm.Bool }, new ScriptFunction(c_cpen));
            CmdFunctions.Add("crc16", new[] { CmdPrm.Data }, new ScriptFunction(c_crc16));
            CmdFunctions.Add("crc32", new[] { CmdPrm.Data }, new ScriptFunction(c_crc32));
            CmdFunctions.Add("cint", new[] { CmdPrm.UInteger }, new ScriptFunction(c_cint));
            CmdFunctions.Add("cuint", new[] { CmdPrm.Integer }, new ScriptFunction(c_cuint));

            // CmdFunctions.Add("debug", {CmdPrm.UInteger}, New ScriptFunction(AddressOf c_debug))
        }

        internal ScriptVariable c_debug(ScriptVariable[] arguments, uint Index) {
            uint v = Conversions.ToUInteger(arguments[0].Value);
            return null;
        }

        internal bool ExecuteCommand(string cmd_line) {
            ABORT_SCRIPT = false;
            var scripe_line = new ScriptElement(this);
            var result = scripe_line.Parse(cmd_line, true);
            if (result.IsError) {
                PrintConsole?.Invoke("Error: " + result.ErrorMsg);
                return false;
            }
            ExitMode argexit_task = default;
            result = ExecuteScriptElement(scripe_line, ref argexit_task);
            if (result.IsError) {
                PrintConsole?.Invoke("Error: " + result.ErrorMsg);
            }
            return true; // No error!
        }
        // Unloads any current script
        internal bool Unload() {
            ABORT_SCRIPT = true;
            CurrentVars.Clear();
            CurrentScript.Reset();
            for (int i = 0, loopTo = OurFlashDevices.Count - 1; i <= loopTo; i++) {
                var this_devie = OurFlashDevices[i];
                //if (MainApp.GUI is object) { MainApp.GUI.RemoveTab(this_devie); }  
                MainApp.MEM_IF.Remove(this_devie);
                //Application.DoEvents();
            }
            //if (MainApp.GUI is object) { MainApp.GUI.RemoveUserTabs();  } 
            OurFlashDevices.Clear();
            UserTabCount = 0U;
            return true;
        }
        // This loads the script file
        internal bool LoadFile(System.IO.FileInfo file_name) {
            Unload();
            PrintConsole?.Invoke("Loading FlashcatUSB script: " + file_name.Name);
            var f = Utilities.FileIO.ReadFile(file_name.FullName);
            string err_str = "";
            uint line_err = 0U; // The line within the file that has the error
            int argErrInd = (int)line_err;
            if (CurrentScript.LoadFile(this, f, ref argErrInd, ref err_str)) {
                PrintConsole?.Invoke("Script successfully loaded");
                script_is_running = true;
                var td = new System.Threading.Thread(() => RunScript());
                //td.SetApartmentState(System.Threading.ApartmentState.STA);
                td.IsBackground = true;
                td.Start();
                return true;
            } else {
                if (err_str.Equals("")) {
                    PrintConsole?.Invoke("Error loading script: " + err_str + " (line " + (line_err + 1L) + ")");
                }
                return false;
            }
        }

        internal bool RunScriptFile(string[] script_text) {
            var line_err = default(uint);
            string line_reason = "";
            int argErrInd = (int)line_err;
            if (CurrentScript.LoadFile(this, script_text, ref argErrInd, ref line_reason)) {
                PrintConsole?.Invoke("Script successfully loaded");
                var td = new System.Threading.Thread(() => RunScript());
                //td.SetApartmentState(System.Threading.ApartmentState.STA);
                td.IsBackground = true;
                td.Start();
                return true;
            } else {
                if (!string.IsNullOrEmpty(line_reason)) {
                    PrintConsole?.Invoke("Error loading script: " + line_reason + " (line " + (line_err + 1L) + ")");
                }
                return false;
            }
        }

        internal bool RunScript() {
            try {
                ABORT_SCRIPT = false;
                var main_param = new ExecuteParam();
                bool result = ExecuteElements(CurrentScript.TheScript.ToArray(), ref main_param);
                if (!result) {
                    if (!string.IsNullOrEmpty(main_param.err_reason)) {
                        PrintConsole?.Invoke("Error in script: " + main_param.err_reason + " (line " + (main_param.err_line + 1L) + ")");
                    }

                    return false;
                }

                if (main_param.exit_task == ExitMode.GotoLabel) {
                    PrintConsole?.Invoke("Error in script, unable to find label: " + main_param.goto_label);
                    return false;
                }
            } catch {
            } finally {
                script_is_running = false;
            }
            return true;
        }

        internal bool ExecuteElements(ScriptLineElement[] e, ref ExecuteParam @params) {
            if (ABORT_SCRIPT)
                return false;
            if (@params.exit_task == ExitMode.LeaveScript)
                return true;
            if (e is object && e.Length > 0) {
                for (int i = 0, loopTo = e.Length - 1; i <= loopTo; i++) {
                    switch (e[i].ElementType) {
                        case ScriptFileElementType.ELEMENT: {
                                var result = ExecuteScriptElement((ScriptElement)e[i], ref @params.exit_task);
                                if (result.IsError) {
                                    @params.err_reason = result.ErrorMsg;
                                    @params.err_line = e[i].INDEX;
                                    return false;
                                }
                                if (@params.exit_task == ExitMode.LeaveScript)
                                    return true;
                                break;
                            }

                        case ScriptFileElementType.FOR_LOOP: {
                                ScriptLoop se = (ScriptLoop)e[i];
                                if (!se.Evaluate()) {
                                    @params.err_line = e[i].INDEX;
                                    @params.err_reason = "Failed to evaluate LOOP parameters";
                                    return false;
                                }

                                var counter_sv = new ScriptVariable(se.VAR_NAME, DataType.UInteger);
                                for (uint loop_index = se.START_IND, loopTo1 = se.END_IND; se.STEP_VAL >= 0 ? loop_index <= loopTo1 : loop_index >= loopTo1; loop_index += se.STEP_VAL) {
                                    counter_sv.Value = loop_index;
                                    CurrentVars.SetVariable(counter_sv);
                                    bool loop_result = ExecuteElements(se.LOOP_MAIN, ref @params);
                                    if (!loop_result)
                                        return false;
                                    if (@params.exit_task == ExitMode.Leave) {
                                        @params.exit_task = ExitMode.KeepRunning;
                                        break;
                                    } else if (@params.exit_task == ExitMode.LeaveEvent) {
                                        return true;
                                    } else if (@params.exit_task == ExitMode.LeaveScript) {
                                        return true;
                                    }
                                }

                                break;
                            }

                        case ScriptFileElementType.IF_CONDITION: {
                                ScriptCondition se = (ScriptCondition)e[i];
                                var test_condition = se.CONDITION.Compile(ref @params.exit_task);
                                if (test_condition is null || test_condition.Data.VarType == DataType.FncError) {
                                    @params.err_reason = Conversions.ToString(test_condition.Data.Value);
                                    @params.err_line = se.INDEX;
                                    return false;
                                }

                                bool result = Conversions.ToBoolean(test_condition.Value);
                                if (se.NOT_MODIFIER)
                                    result = !result;
                                bool execute_result;
                                if (result) {
                                    execute_result = ExecuteElements(se.IF_MAIN, ref @params);
                                } else {
                                    execute_result = ExecuteElements(se.IF_ELSE, ref @params);
                                }

                                if (!execute_result)
                                    return false;
                                if (@params.exit_task == ExitMode.Leave | @params.exit_task == ExitMode.LeaveScript | @params.exit_task == ExitMode.LeaveEvent)
                                    return true;
                                break;
                            }

                        case ScriptFileElementType.GOTO: {
                                ScriptGoto so = (ScriptGoto)e[i];
                                @params.goto_label = so.TO_LABEL.ToUpper();
                                @params.exit_task = ExitMode.GotoLabel;
                                break;
                            }

                        case ScriptFileElementType.EXIT: {
                                ScriptExit so = (ScriptExit)e[i];
                                @params.exit_task = so.MODE;
                                return true;
                            }

                        case ScriptFileElementType.RETURN: {
                                ScriptReturn sr = (ScriptReturn)e[i];
                                var ret_val = sr.Compile(ref @params.exit_task); // Now compute the return result
                                @params.err_reason = sr.ERROR_MSG;
                                @params.err_line = sr.INDEX;
                                if (sr.HAS_ERROR)
                                    return false;
                                CurrentVars.ClearVariable("EVENTRETURN");
                                if (ret_val is object) {
                                    var n = new ScriptVariable("EVENTRETURN", ret_val.Data.VarType);
                                    n.Value = ret_val.Value;
                                    CurrentVars.SetVariable(n);
                                }

                                @params.exit_task = ExitMode.LeaveEvent;
                                return true;
                            }
                    }

                    if (@params.exit_task == ExitMode.GotoLabel) {
                        bool label_found = false;
                        for (int x = 0, loopTo2 = e.Length - 1; x <= loopTo2; x++) { // Search local labels first
                            if (e[x].ElementType == ScriptFileElementType.LABEL) {
                                if ((((ScriptLabel)e[x]).NAME.ToUpper() ?? "") == (@params.goto_label ?? "")) {
                                    i = x - 1; // This sets the execution to the label
                                    @params.exit_task = ExitMode.KeepRunning;
                                    label_found = true;
                                    break;
                                }
                            }
                        }

                        if (!label_found)
                            return true; // We didn't find the label, go up a level
                    }
                }
            }

            return true;
        }

        internal ParseResult ExecuteScriptElement(ScriptElement e, ref ExitMode exit_task) {
            try {
                var sv = e.Compile(ref exit_task);
                if (sv is null)
                    return new ParseResult();
                if (sv.Data.VarType == DataType.FncError)
                    return new ParseResult(Conversions.ToString(sv.Data.Value));
                if (sv is null)
                    return new ParseResult(); // Compiled successfully but no value to save
                if (!string.IsNullOrEmpty(e.TARGET_NAME) && !(e.TARGET_OPERATION == TargetOper.NONE)) {
                    if (!string.IsNullOrEmpty(e.TARGET_VAR)) {
                        if (CurrentVars.IsVariable(e.TARGET_VAR) && CurrentVars.GetVariable(e.TARGET_VAR).Data.VarType == DataType.UInteger) {
                            e.TARGET_INDEX = Conversions.ToInteger(CurrentVars.GetVariable(e.TARGET_VAR).Value); // Gets the variable and assigns it to the index
                        } else {
                            return new ParseResult("Target index is not an integer or integer variable");
                        }
                    }

                    if (e.TARGET_INDEX > -1) { // We are assinging this result to an index within a data array
                        var current_var = CurrentVars.GetVariable(e.TARGET_NAME);
                        if (current_var is null)
                            return new ParseResult("Target index used on a variable that does not exist");
                        if (current_var.Data.VarType == DataType.NULL)
                            return new ParseResult("Target index used on a variable that does not yet exist");
                        if (!(current_var.Data.VarType == DataType.Data))
                            return new ParseResult("Target index used on a variable that is not a DATA array");
                        byte[] data_out = (byte[])current_var.Value;
                        if (sv.Data.VarType == DataType.UInteger) {
                            byte byte_out = (byte)(Conversions.ToUInteger(sv.Value) & 255L);
                            data_out[e.TARGET_INDEX] = byte_out;
                        }

                        var set_var = new ScriptVariable(e.TARGET_NAME, DataType.Data);
                        set_var.Value = data_out;
                        CurrentVars.SetVariable(set_var);
                    } else { // No Target Index
                        var new_var = new ScriptVariable(e.TARGET_NAME, sv.Data.VarType);
                        new_var.Value = sv.Value;
                        var var_op = OperandOper.NOTSPECIFIED;
                        switch (e.TARGET_OPERATION) {
                            case TargetOper.EQ: {
                                    CurrentVars.SetVariable(new_var);
                                    return new ParseResult();
                                }

                            case TargetOper.ADD: {
                                    var_op = OperandOper.ADD;
                                    break;
                                }

                            case TargetOper.SUB: {
                                    var_op = OperandOper.SUB;
                                    break;
                                }
                        }

                        var existing_var = CurrentVars.GetVariable(e.TARGET_NAME);
                        if (existing_var is null || existing_var.Data.VarType == DataType.NULL) {
                            CurrentVars.SetVariable(new_var);
                        } else if (!(existing_var.Data.VarType == new_var.Data.VarType)) {
                            CurrentVars.SetVariable(new_var);
                        } else {
                            var result_var = Tools.CompileSVars(existing_var, new_var, var_op);
                            if (result_var.Data.VarType == DataType.FncError)
                                return new ParseResult(Conversions.ToString(result_var.Data.Value));
                            var compiled_var = new ScriptVariable(e.TARGET_NAME, result_var.Data.VarType);
                            compiled_var.Value = result_var.Value;
                            CurrentVars.SetVariable(compiled_var);
                        }
                    }
                }

                return new ParseResult();
            } catch {
                return new ParseResult("General purpose error");
            }
        }

        internal ScriptVariable ExecuteScriptEvent(ScriptEvent s_event, ScriptVariable[] arguments, ref ExitMode exit_task) {
            if (arguments.Count() > 0) {
                int i = 1;
                foreach (var item in arguments) {
                    var n = new ScriptVariable("$" + i.ToString(), item.Data.VarType);
                    n.Value = item.Value;
                    CurrentVars.SetVariable(n);
                    i = i + 1;
                }
            }

            var event_param = new ExecuteParam();
            if (!ExecuteElements(s_event.Elements, ref event_param)) {
                PrintConsole?.Invoke("Error in Event: " + event_param.err_reason + " (line " + (event_param.err_line + 1L) + ")");
                return null;
            }

            if (event_param.exit_task == ExitMode.GotoLabel) {
                PrintConsole?.Invoke("Error in Event, unable to find label: " + event_param.goto_label);
                return null;
            }

            var event_result = CurrentVars.GetVariable("EVENTRETURN");
            if (event_result is object && !(event_result.Data.VarType == DataType.NULL)) {
                var new_var = new ScriptVariable(CurrentVars.GetNewName(), event_result.Data.VarType);
                new_var.Value = event_result.Value;
                CurrentVars.ClearVariable("EVENTRETURN");
                return new_var;
            } else {
                return null;
            }
        }

        internal ScriptEvent GetScriptEvent(string input) {
            string main_event_name = "";
            string argsub_fnc = null;
            string argind_fnc = null;
            string argarguments = null;
            Tools.ParseToFunctionAndSub(input, ref main_event_name, ref argsub_fnc, ref argind_fnc, ref argarguments);
            foreach (var item in CurrentScript.TheScript) {
                if (item.ElementType == ScriptFileElementType.EVENT) {
                    ScriptEvent se = (ScriptEvent)item;
                    if (se.EVENT_NAME.ToUpper().Equals(main_event_name.ToUpper()))
                        return se;
                }
            }
            return null;
        }

        internal bool IsScriptEvent(string input) {
            string main_event_name = "";
            string argsub_fnc = null;
            string argind_fnc = null;
            string argarguments = null;
            Tools.ParseToFunctionAndSub(input, ref main_event_name, ref argsub_fnc, ref argind_fnc, ref argarguments);
            foreach (var item in CurrentScript.EventList) {
                if ((item.ToUpper() ?? "") == (main_event_name.ToUpper() ?? ""))
                    return true;
            }
            return false;
        }

        public void PrintInformation() {
            PrintConsole?.Invoke("FlashcatUSB Script Engine build: " + Build);
        }

        private uint UserTabCount;
        private List<MemoryInterface.MemoryDeviceInstance> OurFlashDevices = new List<MemoryInterface.MemoryDeviceInstance>();

        // Reads the data from flash and verifies it (returns nothing on error)
        private byte[] ReadMemoryVerify(uint address, uint data_len, FlashMemory.FlashArea index) {
            var cb = new MemoryInterface.MemoryDeviceInstance.StatusCallback();
            cb.UpdatePercent = new UpdateFunction_Progress(MainApp.ProgressBar_Percent);
            cb.UpdateTask = new UpdateFunction_Status(MainApp.SetStatus);
            var memDev = MainApp.MEM_IF.GetDevice((uint)index);
            var FlashData1 = memDev.ReadBytes(address, data_len, cb);
            if (FlashData1 is null)
                return null;
            var FlashData2 = memDev.ReadBytes(address, data_len, cb);
            if (FlashData2 is null)
                return null;
            if (!(FlashData1.Length == FlashData2.Length))
                return null; // Error already?
            if (FlashData1.Length == 0)
                return null;
            if (FlashData2.Length == 0)
                return null;
            var DataWords1 = Utilities.Bytes.ToUIntArray(FlashData1); // This is the one corrected
            var DataWords2 = Utilities.Bytes.ToUIntArray(FlashData2);
            int Counter;
            uint CheckAddr, CheckValue;
            uint[] CheckArray;
            byte[] Data;
            int ErrCount = 0;
            var loopTo = DataWords1.Length - 1;
            for (Counter = 0; Counter <= loopTo; Counter++) {
                if (!(DataWords1[Counter] == DataWords2[Counter])) {
                    if (ErrCount == 100)
                        return null; // Too many errors
                    ErrCount = ErrCount + 1;
                    CheckAddr = (uint)(address + Counter * 4); // Address to verify
                    Data = memDev.ReadBytes(CheckAddr, 4L);
                    CheckArray = Utilities.Bytes.ToUIntArray(Data); // Will only read one element
                    CheckValue = CheckArray[0];
                    if (DataWords1[Counter] == CheckValue) { // Our original data matched
                    } else if (DataWords2[Counter] == CheckValue) { // Our original was incorrect
                        DataWords1[Counter] = DataWords2[Counter];
                    } else {
                        return null;
                    } // 3 reads of the same data did not match, return error!
                }
            }

            var DataOut = Utilities.Bytes.FromUint32Array(DataWords1);
            Array.Resize(ref DataOut, FlashData1.Length);
            return DataOut; // Checked ok!
        }
        // Removes a user control from NAME
        private void RemoveUserControl(string ctr_name) {
            //not supported
        }
        // Handles when the user clicks a button
        private void ButtonHandler(object sender, EventArgs e) {
            //not supported
        }
        // Calls a event (wrapper for runscript)
        private void CallEvent(string EventName) {
            PrintConsole?.Invoke("Button Hander::Calling Event: " + EventName);
            var se = GetScriptEvent(EventName);
            if (se is object) {
                ExitMode argexit_task = default;
                ExecuteScriptEvent(se, null, ref argexit_task);
            } else {
                PrintConsole?.Invoke("Error: Event does not exist");
            }

            PrintConsole?.Invoke("Button Hander::Calling Event: Done");
        }

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                MainApp.ProgressBar_Dispose();
                CurrentScript = null;
                CurrentVars = null;
            }

            disposedValue = true;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override string ToString() {
            return base.ToString();
        }

        public override bool Equals(object obj) {
            return base.Equals(obj);
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        ~Processor() {
        }

        internal ScriptVariable c_str_upper(ScriptVariable[] arguments, uint Index) {
            string input = Conversions.ToString(arguments[0].Value);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.String);
            sv.Value = input.ToUpper();
            return sv;
        }

        internal ScriptVariable c_str_lower(ScriptVariable[] arguments, uint Index) {
            string input = Conversions.ToString(arguments[0].Value);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.String);
            sv.Value = input.ToLower();
            return sv;
        }

        internal ScriptVariable c_str_hex(ScriptVariable[] arguments, uint Index) {
            int input = Conversions.ToInteger(arguments[0].Value);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.String);
            sv.Value = "0x" + Conversion.Hex(input);
            return sv;
        }

        internal ScriptVariable c_str_length(ScriptVariable[] arguments, uint Index) {
            string input = Conversions.ToString(arguments[0].Value);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Integer);
            sv.Value = input.Length;
            return sv;
        }

        internal ScriptVariable c_str_toint(ScriptVariable[] arguments, uint Index) {
            string input = Conversions.ToString(arguments[0].Value);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Integer);
            if (string.IsNullOrEmpty(input.Trim())) {
                sv.Value = 0;
            } else {
                sv.Value = Conversions.ToInteger(input);
            }

            return sv;
        }

        internal ScriptVariable c_str_fromint(ScriptVariable[] arguments, uint Index) {
            int input = Conversions.ToInteger(arguments[0].Value);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.String);
            sv.Value = input.ToString();
            return sv;
        }

        internal ScriptVariable c_data_new(ScriptVariable[] arguments, uint Index) {
            int size = Conversions.ToInteger(arguments[0].Value); // Size of the Data Array
            var data = new byte[size];
            if (arguments.Length > 1) {
                byte[] data_init = (byte[])arguments[1].Value;
                int bytes_to_repeat = data_init.Length;
                int ptr = 0;
                for (int i = 0, loopTo = data.Length - 1; i <= loopTo; i++) {
                    data[i] = data_init[ptr];
                    ptr += 1;
                    if (ptr == bytes_to_repeat)
                        ptr = 0;
                }
            } else {
                Utilities.FillByteArray(ref data, 255);
            }

            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Data);
            sv.Value = data;
            return sv;
        }

        internal ScriptVariable c_data_fromhex(ScriptVariable[] arguments, uint Index) {
            string input = Conversions.ToString(arguments[0].Value);
            var data = Utilities.Bytes.FromHexString(input);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Data);
            sv.Value = data;
            return sv;
        }

        internal ScriptVariable c_data_compare(ScriptVariable[] arguments, uint Index) {
            byte[] data1 = (byte[])arguments[0].Value;
            byte[] data2 = (byte[])arguments[1].Value;
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Bool);
            if (data1 is null & data2 is null) {
                sv.Value = true;
            } else if (data1 is null && data2 is object) {
                sv.Value = false;
            } else if (data1 is object && data2 is null) {
                sv.Value = false;
            } else if (!(data1.Length == data2.Length)) {
                sv.Value = false;
            } else {
                sv.Value = true; // Set to true and if byte mismatch then return false
                for (int i = 0, loopTo = data1.Length - 1; i <= loopTo; i++) {
                    if (!(data1[i] == data2[i])) {
                        sv.Value = false;
                        break;
                    }
                }
            }

            return sv;
        }

        internal ScriptVariable c_data_length(ScriptVariable[] arguments, uint Index) {
            byte[] data1 = (byte[])arguments[0].Value;
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.UInteger);
            sv.Value = data1.Length;
            return sv;
        }

        internal ScriptVariable c_data_resize(ScriptVariable[] arguments, uint Index) {
            byte[] data_arr = (byte[])arguments[0].Value;
            int copy_index = Conversions.ToInteger(arguments[1].Value);
            int copy_length = data_arr.Length - copy_index;
            if (arguments.Length == 3)
                copy_length = Conversions.ToInteger(arguments[2].Value);
            var data_out = new byte[copy_length];
            Array.Copy(data_arr, copy_index, data_out, 0, copy_length);
            arguments[0].Value = data_out;
            CurrentVars.SetVariable(arguments[0]);
            return null;
        }

        internal ScriptVariable c_data_hword(ScriptVariable[] arguments, uint Index) {
            byte[] data1 = (byte[])arguments[0].Value;
            int offset = Conversions.ToInteger(arguments[1].Value);
            var b = new byte[2];
            b[0] = data1[offset];
            b[1] = data1[offset + 1];
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Integer);
            sv.Value = (object)Utilities.Bytes.ToUInt16(b);
            return sv;
        }

        internal ScriptVariable c_data_word(ScriptVariable[] arguments, uint Index) {
            byte[] data1 = (byte[])arguments[0].Value;
            int offset = Conversions.ToInteger(arguments[1].Value);
            var b = new byte[4];
            b[0] = data1[offset];
            b[1] = data1[offset + 1];
            b[2] = data1[offset + 2];
            b[3] = data1[offset + 3];
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Integer);
            sv.Value = (object)Utilities.Bytes.ToUInt32(b);
            return sv;
        }

        internal ScriptVariable c_data_tostr(ScriptVariable[] arguments, uint Index) {
            byte[] data1 = (byte[])arguments[0].Value;
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.String);
            sv.Value = Utilities.Bytes.ToHexString(data1);
            return sv;
        }

        internal ScriptVariable c_data_copy(ScriptVariable[] arguments, uint Index) {
            byte[] data1 = (byte[])arguments[0].Value;
            uint src_ind = Conversions.ToUInteger(arguments[1].Value);
            uint data_len = (uint)(data1.Length - src_ind);
            if (arguments.Length > 2) {
                data_len = Conversions.ToUInteger(arguments[3].Value);
            }

            var new_data = new byte[(int)(data_len - 1L + 1)];
            Array.Copy(data1, src_ind, new_data, 0L, new_data.Length);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Data);
            sv.Value = new_data;
            return sv;
        }

        internal ScriptVariable c_data_combine(ScriptVariable[] arguments, uint Index) {
            byte[] data1 = (byte[])arguments[0].Value;
            byte[] data2 = (byte[])arguments[1].Value;
            if (!(data1 is object && data1.Length > 0)) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "Argument 1 must be a valid data array" };
            }

            if (!(data2 is object && data2.Length > 0)) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "Argument 2 must be a valid data array" };
            }

            var new_data_arr = new byte[(data1.Length + data2.Length)];
            Array.Copy(data1, 0, new_data_arr, 0, data1.Length);
            Array.Copy(data2, 0, new_data_arr, data1.Length, data2.Length);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Data);
            sv.Value = new_data_arr;
            return sv;
        }

        internal ScriptVariable c_io_open(ScriptVariable[] arguments, uint Index) {
            string title = "Choose file to open";
            string filter = "All files (*.*)|*.*";
            string opt_path = @"\";
            if (arguments.Length > 0)
                title = Conversions.ToString(arguments[0].Value);
            if (arguments.Length > 1)
                filter = Conversions.ToString(arguments[1].Value);
            if (arguments.Length > 2)
                opt_path = Conversions.ToString(arguments[2].Value);
            string user_reponse = MainApp.PromptUser_OpenFile(title, filter, opt_path);
            if (string.IsNullOrEmpty(user_reponse))
                return null;
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Data);
            sv.Value = Utilities.FileIO.ReadBytes(user_reponse); // There was an error here!
            return sv;
        }

        internal ScriptVariable c_io_save(ScriptVariable[] arguments, uint Index) {
            byte[] var_data = (byte[])arguments[0].Value;
            string filter = "All files (*.*)|*.*";
            string prompt_text = "";
            string default_file = "";
            if (arguments.Length > 1) {
                prompt_text = Conversions.ToString(arguments[1].Value);
            }

            if (arguments.Length > 2) {
                default_file = Conversions.ToString(arguments[2].Value);
            }

            string user_reponse = MainApp.PromptUser_SaveFile(prompt_text, filter, default_file);
            if (string.IsNullOrEmpty(user_reponse)) {
                PrintConsole?.Invoke("User canceled operation to save data");
            } else {
                Utilities.FileIO.WriteBytes(var_data, user_reponse);
                PrintConsole?.Invoke("Data saved: " + var_data.Length + " bytes written");
            }

            return null;
        }

        internal ScriptVariable c_io_read(ScriptVariable[] arguments, uint Index) {
            string input = Conversions.ToString(arguments[0].Value);
            var local_file = new System.IO.FileInfo(input);
            if (local_file.Exists) {
                var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Data);
                sv.Value = Utilities.FileIO.ReadBytes(local_file.FullName);
                return sv;
            } else {
                PrintConsole?.Invoke("Error in IO.Read: file not found: " + local_file.FullName);
            }

            return null;
        }

        internal ScriptVariable c_io_write(ScriptVariable[] arguments, uint Index) {
            byte[] data1 = (byte[])arguments[0].Value;
            string destination = Conversions.ToString(arguments[1].Value);
            if (!Utilities.FileIO.WriteBytes(data1, destination)) {
                PrintConsole?.Invoke("Error in IO.Write: failed to write data");
            }

            return null;
        }
        internal ScriptVariable c_mem_name(ScriptVariable[] arguments, uint Index) {
            string name_out = MainApp.MEM_IF.GetDevice(Index).Name;
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.String);
            sv.Value = name_out;
            return sv;
        }

        internal ScriptVariable c_mem_size(ScriptVariable[] arguments, uint Index) {
            if (MainApp.MEM_IF.GetDevice(Index).Size > int.MaxValue) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "Memory device is larger than 2GB" };
            }

            int size_value = (int)MainApp.MEM_IF.GetDevice(Index).Size;
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Integer);
            sv.Value = size_value;
            return sv;
        }

        internal ScriptVariable c_mem_write(ScriptVariable[] arguments, uint Index) {
            byte[] data_to_write = (byte[])arguments[0].Value;
            int offset = Conversions.ToInteger(arguments[1].Value);
            int data_len = data_to_write.Length;
            if (arguments.Length > 2)
                data_len = Conversions.ToInteger(arguments[2].Value);
            Array.Resize(ref data_to_write, data_len);
            MainApp.ProgressBar_Percent(0);
            var mem_device = MainApp.MEM_IF.GetDevice(Index);
            if (mem_device is null) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "Memory device not connected" };
            }

            var cb = new MemoryInterface.MemoryDeviceInstance.StatusCallback();
            cb.UpdatePercent = new UpdateFunction_Progress(MainApp.ProgressBar_Percent);
            cb.UpdateTask = new UpdateFunction_Status(MainApp.SetStatus);
            MainApp.MEM_IF.GetDevice(Index).DisableGuiControls();
            MainApp.MEM_IF.GetDevice(Index).FCUSB.USB_LEDBlink();
            try {
                var mem_dev = MainApp.MEM_IF.GetDevice(Index);
                bool write_result = mem_dev.WriteBytes(offset, data_to_write, MainApp.MySettings.VERIFY_WRITE, cb);
                if (write_result) {
                    PrintConsole?.Invoke("Sucessfully programmed " + data_len.ToString("N0") + " bytes");
                } else {
                    PrintConsole?.Invoke("Canceled memory write operation");
                }
            } catch {
            } finally {
                MainApp.MEM_IF.GetDevice(Index).EnableGuiControls();
                MainApp.MEM_IF.GetDevice(Index).FCUSB.USB_LEDOn();
                MainApp.ProgressBar_Percent(0);
            }

            return null;
        }

        internal ScriptVariable c_mem_read(ScriptVariable[] arguments, uint Index) {
            var mem_device = MainApp.MEM_IF.GetDevice(Index);
            if (mem_device is null) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "Memory device not connected" };
            }

            int offset = Conversions.ToInteger(arguments[0].Value);
            int count = Conversions.ToInteger(arguments[1].Value);
            bool display = true;
            if (arguments.Length > 2)
                display = Conversions.ToBoolean(arguments[2].Value);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Data);
            var cb = new MemoryInterface.MemoryDeviceInstance.StatusCallback();
            if (display) {
                cb.UpdatePercent = new UpdateFunction_Progress(MainApp.ProgressBar_Percent);
                cb.UpdateTask = new UpdateFunction_Status(MainApp.SetStatus);
            }

            mem_device.DisableGuiControls();
            try {
                MainApp.ProgressBar_Percent(0);
                byte[] data_read = null;
                data_read = mem_device.ReadBytes(offset, count, cb);
                sv.Value = data_read;
            } catch {
            } finally {
                mem_device.EnableGuiControls();
            }

            MainApp.ProgressBar_Percent(0);
            return sv;
        }

        internal ScriptVariable c_mem_readstring(ScriptVariable[] arguments, uint Index) {
            var mem_device = MainApp.MEM_IF.GetDevice(Index);
            if (mem_device is null) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "Memory device not connected" };
            }

            int offset = Conversions.ToInteger(arguments[0].Value);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.String);
            int FlashSize = (int)mem_device.Size;
            if (offset + 1 > FlashSize) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "Offset is greater than flash size" };
            }

            string strBuilder = "";
            for (int i = offset, loopTo = FlashSize - 1; i <= loopTo; i++) {
                var flash_data = mem_device.ReadBytes(i, 1L);
                byte b = flash_data[0];
                if (b > 31 & b < 127) {
                    strBuilder += Conversions.ToString((char)b);
                } else if (b == 0) {
                    break;
                } else {
                    return null;
                } // Error
            }

            sv.Value = strBuilder;
            return sv;
        }

        internal ScriptVariable c_mem_readverify(ScriptVariable[] arguments, uint Index) {
            var mem_device = MainApp.MEM_IF.GetDevice(Index);
            if (mem_device is null) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "Memory device not connected" };
            }
            int FlashAddress = Conversions.ToInteger(arguments[0].Value);
            int FlashLen = Conversions.ToInteger(arguments[1].Value);
            byte[] data = null;
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Data);
            MainApp.ProgressBar_Percent(0);
            mem_device.DisableGuiControls();
            try {
                data = ReadMemoryVerify((uint)FlashAddress, (uint)FlashLen, (FlashMemory.FlashArea)Index);
            } catch {
            } finally {
                mem_device.EnableGuiControls();
                MainApp.ProgressBar_Percent(0);
            }

            if (data is null) {
                PrintConsole?.Invoke("Read operation failed");
                return null;
            }
            sv.Value = data;
            return sv;
        }

        internal ScriptVariable c_mem_sectorcount(ScriptVariable[] arguments, uint Index) {
            var mem_device = MainApp.MEM_IF.GetDevice(Index);
            if (mem_device is null) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "Memory device not connected" };
            }
            int sector_count = (int)mem_device.GetSectorCount();
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Integer);
            sv.Value = sector_count;
            return sv;
        }

        internal ScriptVariable c_mem_sectorsize(ScriptVariable[] arguments, uint Index) {
            var mem_device = MainApp.MEM_IF.GetDevice(Index);
            if (mem_device is null) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "Memory device not connected" };
            }

            int sector_int = Conversions.ToInteger(arguments[0].Value);
            int sector_size = (int)mem_device.GetSectorSize((uint)sector_int);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Integer);
            sv.Value = sector_size;
            return sv;
        }

        internal ScriptVariable c_mem_erasesector(ScriptVariable[] arguments, uint Index) {
            var mem_device = MainApp.MEM_IF.GetDevice(Index);
            if (mem_device is null) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "Memory device not connected" };
            }
            int mem_sector = Conversions.ToInteger(arguments[0].Value);
            mem_device.EraseSector((uint)mem_sector);
            long target_addr = mem_device.GetSectorBaseAddress((uint)mem_sector);
            string target_area = "0x" + Conversion.Hex(target_addr).PadLeft(8, '0') + " to 0x" + Conversion.Hex(target_addr + (long)mem_device.GetSectorSize((uint)mem_sector) - 1L).PadLeft(8, '0');
            if (mem_device.NoErrors) {
                PrintConsole?.Invoke("Successfully erased sector index: " + mem_sector + " (" + target_area + ")");
            } else {
                PrintConsole?.Invoke("Failed to erase sector index: " + mem_sector + " (" + target_area + ")");
            }
            //mem_device.GuiControl.RefreshView();
            mem_device.ReadMode();
            return null;
        }

        internal ScriptVariable c_mem_erasebulk(ScriptVariable[] arguments, uint Index) {
            var mem_device = MainApp.MEM_IF.GetDevice(Index);
            if (mem_device is null) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "Memory device not connected" };
            }
            try {
                MainApp.MEM_IF.GetDevice(Index).DisableGuiControls();
                mem_device.EraseFlash();
            } catch {
            } finally {
                MainApp.MEM_IF.GetDevice(Index).EnableGuiControls();
            }
            return null;
        }

        internal ScriptVariable c_mem_exist(ScriptVariable[] arguments, uint Index) {
            var mem_device = MainApp.MEM_IF.GetDevice(Index);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Bool);
            if (mem_device is null) {
                sv.Value = false;
            } else {
                sv.Value = true;
            }
            return sv;
        }

        internal ScriptVariable c_tab_create(ScriptVariable[] arguments, uint Index) {
            return null;
        }

        internal ScriptVariable c_tab_addgroup(ScriptVariable[] arguments, uint Index) {
            return null;
        }

        internal ScriptVariable c_tab_addbox(ScriptVariable[] arguments, uint Index) {
            return null;
        }

        internal ScriptVariable c_tab_addtext(ScriptVariable[] arguments, uint Index) {
            return null;
        }

        internal ScriptVariable c_tab_addimage(ScriptVariable[] arguments, uint Index) {
            return null;
        }

        internal ScriptVariable c_tab_addbutton(ScriptVariable[] arguments, uint Index) {
            return null;
        }

        internal ScriptVariable c_tab_addprogress(ScriptVariable[] arguments, uint Index) {
            int bar_left = Conversions.ToInteger(arguments[0].Value);
            int bar_top = Conversions.ToInteger(arguments[1].Value);
            int bar_width = Conversions.ToInteger(arguments[2].Value);
            MainApp.ProgressBar_Add((int)Index, bar_left, bar_top, bar_width);
            return null;
        }

        internal ScriptVariable c_tab_remove(ScriptVariable[] arguments, uint Index) {
            string item_name = Conversions.ToString(arguments[0].Value);
            RemoveUserControl(item_name);
            return null;
        }

        internal ScriptVariable c_tab_settext(ScriptVariable[] arguments, uint Index) {

            return null;
        }

        internal ScriptVariable c_tab_gettext(ScriptVariable[] arguments, uint Index) {
            return null;
        }

        internal ScriptVariable c_tab_buttondisable(ScriptVariable[] arguments, uint Index) {
            return null;
        }

        internal ScriptVariable c_tab_buttonenable(ScriptVariable[] arguments, uint Index) {
            return null;
        }

        internal ScriptVariable c_spi_clock(ScriptVariable[] arguments, uint Index) {
            int clock_int = Conversions.ToInteger(arguments[0].Value);
            MainApp.MySettings.SPI_CLOCK_MAX = (SPI.SPI_SPEED)clock_int;
            if ((long)MainApp.MySettings.SPI_CLOCK_MAX < 1000000L)
                MainApp.MySettings.SPI_CLOCK_MAX = (SPI.SPI_SPEED)1000000;
            return null;
        }

        internal ScriptVariable c_spi_mode(ScriptVariable[] arguments, uint Index) {
            int mode_int = Conversions.ToInteger(arguments[0].Value);
            switch (mode_int) {
                case 0: {
                        MainApp.MySettings.SPI_MODE = SPI.SPI_CLOCK_POLARITY.SPI_MODE_0;
                        break;
                    }

                case 1: {
                        MainApp.MySettings.SPI_MODE = SPI.SPI_CLOCK_POLARITY.SPI_MODE_1;
                        break;
                    }

                case 2: {
                        MainApp.MySettings.SPI_MODE = SPI.SPI_CLOCK_POLARITY.SPI_MODE_2;
                        break;
                    }

                case 3: {
                        MainApp.MySettings.SPI_MODE = SPI.SPI_CLOCK_POLARITY.SPI_MODE_3;
                        break;
                    }
            }

            return null;
        }

        internal ScriptVariable c_spi_database(ScriptVariable[] arguments, uint Index) {
            bool DisplayJedecID = false;
            if (arguments.Length == 1) {
                DisplayJedecID = Conversions.ToBoolean(arguments[0].Value);
            }

            PrintConsole?.Invoke("The internal Flash database consists of " + MainApp.FlashDatabase.FlashDB.Count + " devices");
            foreach (var device in MainApp.FlashDatabase.FlashDB) {
                if (device.FLASH_TYPE == FlashMemory.MemoryType.SERIAL_NOR) {
                    string size_str = "";
                    int size_int = (int)device.FLASH_SIZE;
                    if (size_int < 128) {
                        size_str = size_int / 8d + "bits";
                    } else if (size_int < 131072) {
                        size_str = size_int / 128d + "Kbits";
                    } else {
                        size_str = size_int / 131072d + "Mbits";
                    }

                    if (DisplayJedecID) {
                        string jedec_str = Conversion.Hex(device.MFG_CODE).PadLeft(2, '0') + Conversion.Hex(device.ID1).PadLeft(4, '0');
                        if (jedec_str == "000000") {
                            PrintConsole?.Invoke(device.NAME + " (" + size_str + ") EEPROM");
                        } else {
                            PrintConsole?.Invoke(device.NAME + " (" + size_str + ") JEDEC: 0x" + jedec_str);
                        }
                    } else {
                        PrintConsole?.Invoke(device.NAME + " (" + size_str + ")");
                    }
                }
            }

            PrintConsole?.Invoke("SPI Flash database list complete");
            return null;
        }

        internal ScriptVariable c_spi_getsr(ScriptVariable[] arguments, uint Index) {
            if (CURRENT_DEVICE_MODE == DeviceMode.SPI) {
            } else if (CURRENT_DEVICE_MODE == DeviceMode.SQI) {
            } else {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "Device is not in SPI/QUAD operation mode" };
            }

            if (!MainApp.MAIN_FCUSB.IS_CONNECTED) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "FlashcatUSB device is not connected" };
            }

            int bytes_to_read = 1;
            if (arguments.Length > 0)
                bytes_to_read = Conversions.ToInteger(arguments[0].Value);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Data);
            if (CURRENT_DEVICE_MODE == DeviceMode.SPI) {
                sv.Value = MainApp.MAIN_FCUSB.SPI_NOR_IF.ReadStatusRegister(bytes_to_read);
            } else if (CURRENT_DEVICE_MODE == DeviceMode.SQI) {
                sv.Value = MainApp.MAIN_FCUSB.SQI_NOR_IF.ReadStatusRegister(bytes_to_read);
            }
            return sv;
        }

        internal ScriptVariable c_spi_setsr(ScriptVariable[] arguments, uint Index) {
            if (CURRENT_DEVICE_MODE == DeviceMode.SPI) {
            } else if (CURRENT_DEVICE_MODE == DeviceMode.SQI) {
            } else {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "Device is not in SPI/QUAD operation mode" };
            }

            if (!MainApp.MAIN_FCUSB.IS_CONNECTED) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "FlashcatUSB device is not connected" };
            }

            byte[] data_out = (byte[])arguments[0].Value;
            if (CURRENT_DEVICE_MODE == DeviceMode.SPI) {
                MainApp.MAIN_FCUSB.SPI_NOR_IF.WriteStatusRegister(data_out);
            } else if (CURRENT_DEVICE_MODE == DeviceMode.SQI) {
                MainApp.MAIN_FCUSB.SQI_NOR_IF.WriteStatusRegister(data_out);
            }
            return null;
        }

        internal ScriptVariable c_spi_writeread(ScriptVariable[] arguments, uint Index) {
            if (!(CURRENT_DEVICE_MODE == DeviceMode.SPI)) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "Device is not in SPI operation mode" };
            }

            if (!MainApp.MAIN_FCUSB.IS_CONNECTED) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "FlashcatUSB device is not connected" };
            }

            byte[] DataToWrite = (byte[])arguments[0].Value;
            int ReadBack = 0;
            if (arguments.Length == 2)
                ReadBack = Conversions.ToInteger(arguments[1].Value);
            if (ReadBack == 0) {
                byte[] argReadBuffer = null;
                MainApp.MAIN_FCUSB.SPI_NOR_IF.SPIBUS_WriteRead(DataToWrite, ReadBuffer: ref argReadBuffer);
                return null;
            } else {
                var return_data = new byte[ReadBack];
                MainApp.MAIN_FCUSB.SPI_NOR_IF.SPIBUS_WriteRead(DataToWrite, ref return_data);
                var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Data);
                sv.Value = return_data;
                return sv;
            }
        }

        internal ScriptVariable c_spi_prog(ScriptVariable[] arguments, uint Index) {
            var spi_port = MainApp.MAIN_FCUSB.SPI_NOR_IF;
            int state = Conversions.ToInteger(arguments[0].Value);
            if (state == 1) { // Set the PROGPIN to HIGH
                spi_port.SetProgPin(true);
            } else { // Set the PROGPIN to LOW
                spi_port.SetProgPin(false);
            }

            return null;
        }

        internal ScriptVariable c_jtag_idcode(ScriptVariable[] arguments, uint Index) {
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.UInteger);
            int current_index = MainApp.MAIN_FCUSB.JTAG_IF.Chain_SelectedIndex;
            sv.Value = (object)MainApp.MAIN_FCUSB.JTAG_IF.Devices[current_index].IDCODE;
            return sv;
        }

        internal ScriptVariable c_jtag_config(ScriptVariable[] arguments, uint Index) {
            if (arguments.Length == 1) {
                switch (((string)(arguments[0].Value)).ToUpper()) {
                    case "MIPS": {
                            MainApp.MAIN_FCUSB.JTAG_IF.Configure(JTAG.PROCESSOR.MIPS);
                            break;
                        }
                    case "ARM": {
                            MainApp.MAIN_FCUSB.JTAG_IF.Configure(JTAG.PROCESSOR.ARM);
                            break;
                        }
                    default: {
                            return new ScriptVariable("ERROR", DataType.FncError) { Value = Operators.ConcatenateObject("Unknown mode: ", arguments[0].Value) };
                        }
                }
            } else {
                MainApp.MAIN_FCUSB.JTAG_IF.Configure(JTAG.PROCESSOR.NONE);
            }

            return null;
        }

        internal ScriptVariable c_jtag_select(ScriptVariable[] arguments, uint Index) {
            if (MainApp.MAIN_FCUSB is null || !MainApp.MAIN_FCUSB.JTAG_IF.Chain_IsValid) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "JTAG operations are not currently valid" };
            }

            int select_index = Conversions.ToInteger(arguments[0].Value);
            if (MainApp.MAIN_FCUSB.JTAG_IF.Chain_IsValid) {
                MainApp.MAIN_FCUSB.JTAG_IF.Chain_Select((int)Index);
                return new ScriptVariable(CurrentVars.GetNewName(), DataType.Bool, true);
            } else {
                PrintConsole?.Invoke("JTAG chain is not valid, not all devices have BSDL loaded");
            }

            return new ScriptVariable(CurrentVars.GetNewName(), DataType.Bool, false);
        }

        internal ScriptVariable c_jtag_print(ScriptVariable[] arguments, uint Index) {
            MainApp.MAIN_FCUSB.JTAG_IF.Chain_Print();
            return null;
        }

        internal ScriptVariable c_jtag_clear(ScriptVariable[] arguments, uint Index) {
            MainApp.MAIN_FCUSB.JTAG_IF.Chain_Clear();
            return null;
        }

        internal ScriptVariable c_jtag_set(ScriptVariable[] arguments, uint Index) {
            int jtag_device_index = Conversions.ToInteger(arguments[0].Value);
            string bsdl_name = Conversions.ToString(arguments[1].Value);
            if (MainApp.MAIN_FCUSB.JTAG_IF.Chain_Set(jtag_device_index, bsdl_name)) {
                PrintConsole?.Invoke("Successful set chain index " + jtag_device_index.ToString() + " to " + bsdl_name);
            } else {
                PrintConsole?.Invoke("Error: unable to find internal BSDL device with name " + bsdl_name);
            }

            return null;
        }

        internal ScriptVariable c_jtag_add(ScriptVariable[] arguments, uint Index) {
            string bsdl_lib = Conversions.ToString(arguments[0].Value);
            if (MainApp.MAIN_FCUSB.JTAG_IF.Chain_Add(bsdl_lib)) {
                PrintConsole?.Invoke("Successful added BSDL to JTAG chain");
            } else {
                PrintConsole?.Invoke("Error: BSDL library " + bsdl_lib + " not found");
            }

            return null;
        }

        internal ScriptVariable c_jtag_validate(ScriptVariable[] arguments, uint Index) {
            if (MainApp.MAIN_FCUSB.JTAG_IF.Chain_Validate()) {
                PrintConsole?.Invoke("JTAG chain is valid");
            } else {
                PrintConsole?.Invoke("JTAG chain is invalid");
            }

            return null;
        }

        internal ScriptVariable c_jtag_control(ScriptVariable[] arguments, uint Index) {
            uint control_value = Conversions.ToUInteger(arguments[0].Value);
            var j = MainApp.MAIN_FCUSB.JTAG_IF.Chain_Get(MainApp.MAIN_FCUSB.JTAG_IF.Chain_SelectedIndex);
            if (j is object) {
                uint result = MainApp.MAIN_FCUSB.JTAG_IF.AccessDataRegister32(j.MIPS_CONTROL, control_value);
                PrintConsole?.Invoke("JTAT CONTROL command issued: 0x" + Conversion.Hex(control_value) + " result: 0x" + Conversion.Hex(result));
                var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.UInteger);
                sv.Value = result;
                return sv;
            }

            return null;
        }

        internal ScriptVariable c_jtag_memoryinit(ScriptVariable[] arguments, uint Index) {
            string flash_type = Conversions.ToString(arguments[0].Value);
            MemoryInterface.MemoryDeviceInstance new_dev = null;
            switch (flash_type.ToUpper() ?? "") {
                case "CFI": {
                        uint base_address = Conversions.ToUInteger(arguments[1].Value);
                        PrintConsole?.Invoke(string.Format("Attempting to detect CFI flash at address 0x{0}", Conversion.Hex(base_address).PadLeft(8, '0')));
                        if (MainApp.MAIN_FCUSB.JTAG_IF.CFI_Detect(base_address)) {
                            new_dev = MainApp.Connected_Event(MainApp.MAIN_FCUSB, 16384U);
                        } else {
                            PrintConsole?.Invoke("Error: unable to detect CFI flash device over JTAG");
                        }

                        break;
                    }

                case "SPI": {
                        PrintConsole?.Invoke("Attempting to detect SPI flash connected to MCU via JTAG");
                        if (MainApp.MAIN_FCUSB.JTAG_IF.SPI_Detect((JTAG.JTAG_IF.JTAG_SPI_Type)Conversions.ToInteger(arguments[1].Value))) {
                            new_dev = MainApp.Connected_Event(MainApp.MAIN_FCUSB, 16384U);
                        } else {
                            PrintConsole?.Invoke("Error: unable to detect SPI flash device over JTAG");
                        } // "Error: unable to detect SPI flash device over JTAG"

                        break;
                    }

                default: {
                        return new ScriptVariable("ERROR", DataType.FncError) { Value = "Error in JTAG.MemoryInit: device type not specified" };
                    }
            }

            if (new_dev is object) {
                OurFlashDevices.Add(new_dev);
                return new ScriptVariable(CurrentVars.GetNewName(), DataType.UInteger) { Value = OurFlashDevices.Count - 1 };
            } else {
                PrintConsole?.Invoke("JTAG.MemoryInit: failed to create new memory device interface");
            }
            return null;
        }

        internal ScriptVariable c_jtag_debug(ScriptVariable[] arguments, uint Index) {
            bool enable = Conversions.ToBoolean(arguments[0].Value);
            if (enable) {
                MainApp.MAIN_FCUSB.JTAG_IF.EJTAG_Debug_Enable();
            } else {
                MainApp.MAIN_FCUSB.JTAG_IF.EJTAG_Debug_Disable();
            }

            return null;
        }

        internal ScriptVariable c_jtag_cpureset(ScriptVariable[] arguments, uint Index) {
            MainApp.MAIN_FCUSB.JTAG_IF.EJTAG_Reset();
            return null;
        }

        internal ScriptVariable c_jtag_runsvf(ScriptVariable[] arguments, uint Index) {
            if (MainApp.MAIN_FCUSB is null || !MainApp.MAIN_FCUSB.JTAG_IF.Chain_IsValid) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "JTAG operations are not currently valid" };
            }

            MainApp.ProgressBar_Percent(0);
            MainApp.MAIN_FCUSB.JTAG_IF.JSP.Progress -= MainApp.ProgressBar_Percent;
            MainApp.MAIN_FCUSB.JTAG_IF.JSP.Progress += MainApp.ProgressBar_Percent;
            PrintConsole?.Invoke("Running SVF file in internal JTAG SVF player");
            byte[] DataBytes = (byte[])arguments[0].Value;
            var FileStr = Utilities.Bytes.ToCharStringArray(DataBytes);
            bool result = MainApp.MAIN_FCUSB.JTAG_IF.JSP.RunFile_SVF(FileStr);
            if (result) {
                PrintConsole?.Invoke("SVF file successfully played");
            } else {
                PrintConsole?.Invoke("Error playing the SVF file");
            }

            MainApp.ProgressBar_Percent(0);
            MainApp.MAIN_FCUSB.JTAG_IF.JSP.Progress -= MainApp.ProgressBar_Percent;
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Bool);
            sv.Value = result;
            return sv;
        }

        internal ScriptVariable c_jtag_runxsvf(ScriptVariable[] arguments, uint Index) {
            if (MainApp.MAIN_FCUSB is null || !MainApp.MAIN_FCUSB.JTAG_IF.Chain_IsValid) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "JTAG operations are not currently valid" };
            }

            MainApp.ProgressBar_Percent(0);
            MainApp.MAIN_FCUSB.JTAG_IF.JSP.Progress -= MainApp.ProgressBar_Percent;
            MainApp.MAIN_FCUSB.JTAG_IF.JSP.Progress += MainApp.ProgressBar_Percent;
            PrintConsole?.Invoke("Running XSVF file in internal JTAG XSVF player");
            byte[] DataBytes = (byte[])arguments[0].Value;
            bool result = MainApp.MAIN_FCUSB.JTAG_IF.JSP.RunFile_XSVF(DataBytes);
            if (result) {
                PrintConsole?.Invoke("XSVF file successfully played");
            } else {
                PrintConsole?.Invoke("Error playing the XSVF file");
            }

            MainApp.ProgressBar_Percent(0);
            MainApp.MAIN_FCUSB.JTAG_IF.JSP.Progress -= MainApp.ProgressBar_Percent;
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Bool);
            sv.Value = result;
            return sv;
        }

        internal ScriptVariable c_jtag_shiftdr(ScriptVariable[] arguments, uint Index) {
            if (MainApp.MAIN_FCUSB is null || !MainApp.MAIN_FCUSB.JTAG_IF.Chain_IsValid) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "JTAG operations are not currently valid" };
            }

            bool exit_mode = true;
            byte[] data_in = (byte[])arguments[0].Value;
            int bit_count = Conversions.ToInteger(arguments[1].Value);
            byte[] data_out = null;
            if (arguments.Length == 3)
                exit_mode = Conversions.ToBoolean(arguments[2].Value);
            MainApp.MAIN_FCUSB.JTAG_IF.JSP_ShiftDR(data_in, ref data_out, (ushort)bit_count, exit_mode);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Data);
            sv.Value = data_out;
            return sv;
        }

        internal ScriptVariable c_jtag_shiftir(ScriptVariable[] arguments, uint Index) {
            if (MainApp.MAIN_FCUSB is null || !MainApp.MAIN_FCUSB.JTAG_IF.Chain_IsValid) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "JTAG operations are not currently valid" };
            }

            bool exit_mode = true;
            byte[] data_in = (byte[])arguments[0].Value;
            if (arguments.Length == 2)
                exit_mode = Conversions.ToBoolean(arguments[1].Value);
            int ir_size = MainApp.MAIN_FCUSB.JTAG_IF.GetSelected_IRLength();
            byte[] argtdo_bits = null;
            MainApp.MAIN_FCUSB.JTAG_IF.JSP_ShiftIR(data_in, ref argtdo_bits, (short)ir_size, exit_mode);
            return null;
        }

        internal ScriptVariable c_jtag_shiftout(ScriptVariable[] arguments, uint Index) {
            if (MainApp.MAIN_FCUSB is null || !MainApp.MAIN_FCUSB.JTAG_IF.Chain_IsValid) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "JTAG operations are not currently valid" };
            }

            byte[] tdi_data = (byte[])arguments[0].Value;
            int bit_count = Conversions.ToInteger(arguments[1].Value);
            bool exit_tms = true;
            if (arguments.Length == 3)
                exit_tms = Conversions.ToBoolean(arguments[2].Value);
            byte[] tdo_data = null;
            MainApp.MAIN_FCUSB.JTAG_IF.ShiftTDI((uint)bit_count, tdi_data, ref tdo_data, exit_tms);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Data);
            sv.Value = tdo_data;
            return sv;
        }

        internal ScriptVariable c_jtag_tapreset(ScriptVariable[] arguments, uint Index) {
            if (MainApp.MAIN_FCUSB is null || !MainApp.MAIN_FCUSB.JTAG_IF.Chain_IsValid) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "JTAG operations are not currently valid" };
            }

            MainApp.MAIN_FCUSB.JTAG_IF.Reset_StateMachine();
            return null;
        }

        internal ScriptVariable c_jtag_write32(ScriptVariable[] arguments, uint Index) {
            if (MainApp.MAIN_FCUSB is null || !MainApp.MAIN_FCUSB.JTAG_IF.Chain_IsValid) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "JTAG operations are not currently valid" };
            }

            uint addr32 = Conversions.ToUInteger(arguments[0].Value);
            uint data = Conversions.ToUInteger(arguments[1].Value);
            MainApp.MAIN_FCUSB.JTAG_IF.WriteMemory(addr32, data, JTAG.DATA_WIDTH.Word);
            return null;
        }

        internal ScriptVariable c_jtag_read32(ScriptVariable[] arguments, uint Index) {
            if (MainApp.MAIN_FCUSB is null || !MainApp.MAIN_FCUSB.JTAG_IF.Chain_IsValid) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "JTAG operations are not currently valid" };
            }

            uint addr32 = Conversions.ToUInteger(arguments[0].Value);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.UInteger);
            sv.Value = (object)MainApp.MAIN_FCUSB.JTAG_IF.ReadMemory(addr32, JTAG.DATA_WIDTH.Word);
            return sv;
        }

        internal ScriptVariable c_jtag_state(ScriptVariable[] arguments, uint Index) {
            if (MainApp.MAIN_FCUSB is null || !MainApp.MAIN_FCUSB.JTAG_IF.Chain_IsValid) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "JTAG operations are not currently valid" };
            }

            string state_str = Conversions.ToString(arguments[0].Value);
            switch (state_str.ToUpper() ?? "") {
                case var @case when @case == ("RunTestIdle".ToUpper() ?? ""): {
                        MainApp.MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG.JTAG_MACHINE_STATE.RunTestIdle);
                        break;
                    }

                case var case1 when case1 == ("Select_DR".ToUpper() ?? ""): {
                        MainApp.MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG.JTAG_MACHINE_STATE.Select_DR);
                        break;
                    }

                case var case2 when case2 == ("Capture_DR".ToUpper() ?? ""): {
                        MainApp.MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG.JTAG_MACHINE_STATE.Capture_DR);
                        break;
                    }

                case var case3 when case3 == ("Shift_DR".ToUpper() ?? ""): {
                        MainApp.MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG.JTAG_MACHINE_STATE.Shift_DR);
                        break;
                    }

                case var case4 when case4 == ("Exit1_DR".ToUpper() ?? ""): {
                        MainApp.MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG.JTAG_MACHINE_STATE.Exit1_DR);
                        break;
                    }

                case var case5 when case5 == ("Pause_DR".ToUpper() ?? ""): {
                        MainApp.MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG.JTAG_MACHINE_STATE.Pause_DR);
                        break;
                    }

                case var case6 when case6 == ("Exit2_DR".ToUpper() ?? ""): {
                        MainApp.MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG.JTAG_MACHINE_STATE.Exit2_DR);
                        break;
                    }

                case var case7 when case7 == ("Update_DR".ToUpper() ?? ""): {
                        MainApp.MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG.JTAG_MACHINE_STATE.Update_DR);
                        break;
                    }

                case var case8 when case8 == ("Select_IR".ToUpper() ?? ""): {
                        MainApp.MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG.JTAG_MACHINE_STATE.Select_IR);
                        break;
                    }

                case var case9 when case9 == ("Capture_IR".ToUpper() ?? ""): {
                        MainApp.MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG.JTAG_MACHINE_STATE.Capture_IR);
                        break;
                    }

                case var case10 when case10 == ("Shift_IR".ToUpper() ?? ""): {
                        MainApp.MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG.JTAG_MACHINE_STATE.Shift_IR);
                        break;
                    }

                case var case11 when case11 == ("Exit1_IR".ToUpper() ?? ""): {
                        MainApp.MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG.JTAG_MACHINE_STATE.Exit1_IR);
                        break;
                    }

                case var case12 when case12 == ("Pause_IR".ToUpper() ?? ""): {
                        MainApp.MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG.JTAG_MACHINE_STATE.Pause_IR);
                        break;
                    }

                case var case13 when case13 == ("Exit2_IR".ToUpper() ?? ""): {
                        MainApp.MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG.JTAG_MACHINE_STATE.Exit2_IR);
                        break;
                    }

                case var case14 when case14 == ("Update_IR".ToUpper() ?? ""): {
                        MainApp.MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG.JTAG_MACHINE_STATE.Update_IR);
                        break;
                    }

                default: {
                        return new ScriptVariable("ERROR", DataType.FncError) { Value = "JTAG.State: unknown state: " + state_str };
                    }
            }

            return null;
        }

        internal ScriptVariable c_jtag_graycode(ScriptVariable[] arguments, uint Index) {
            bool use_reserve = false;
            int table_ind = Conversions.ToInteger(arguments[0].Value);
            if (arguments.Length == 2)
                use_reserve = Conversions.ToBoolean(arguments[1].Value);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.UInteger);
            if (use_reserve) {
                sv.Value = (object)MainApp.gray_code_table_reverse[table_ind];
            } else {
                sv.Value = (object)MainApp.gray_code_table[table_ind];
            }

            return sv;
        }
        // Undocumented. This is for setting delays on FCUSB Classic EJTAG firmware
        internal ScriptVariable c_jtag_setdelay(ScriptVariable[] arguments, uint Index) {
            int dev_ind = Conversions.ToInteger(arguments[0].Value);
            int delay_val = Conversions.ToInteger(arguments[1].Value);
            switch (dev_ind) {
                case 1: { // Intel
                        MainApp.MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SET_OPTION, null, (uint)((delay_val << 16) + 1));
                        break;
                    }

                case 2: { // AMD
                        MainApp.MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SET_OPTION, null, (uint)((delay_val << 16) + 2));
                        break;
                    }

                case 3: { // DMA
                        MainApp.MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SET_OPTION, null, (uint)((delay_val << 16) + 3));
                        break;
                    }
            }

            return null;
        }

        internal ScriptVariable c_jtag_exitstate(ScriptVariable[] arguments, uint Index) {
            bool exit_state = Conversions.ToBoolean(arguments[0].Value);
            MainApp.MAIN_FCUSB.JTAG_IF.JSP.ExitStateMachine = exit_state;
            if (exit_state) {
                PrintConsole?.Invoke("SVF exit to test-logic-reset enabled");
            } else {
                PrintConsole?.Invoke("SVF exit to test-logic-reset disabled");
            }

            return null;
        }

        internal ScriptVariable c_jtag_epc2_read(ScriptVariable[] arguments, uint Index) {
            MainApp.ProgressBar_Percent(0);
            var cbProgress = new JTAG.JTAG_IF.EPC2_ProgressCallback(MainApp.ProgressBar_Percent);
            var e_data = MainApp.MAIN_FCUSB.JTAG_IF.EPC2_ReadBinary(cbProgress);
            MainApp.ProgressBar_Percent(0);
            if (e_data is object) {
                var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Data);
                sv.Value = e_data;
                return sv;
            }

            return null;
        }

        internal ScriptVariable c_jtag_epc2_write(ScriptVariable[] arguments, uint Index) {
            byte[] BootData = (byte[])arguments[0].Value;
            byte[] CfgData = (byte[])arguments[1].Value;
            MainApp.ProgressBar_Percent(0);
            var cbProgress = new JTAG.JTAG_IF.EPC2_ProgressCallback(MainApp.ProgressBar_Percent);
            bool result = MainApp.MAIN_FCUSB.JTAG_IF.EPC2_WriteBinary(BootData, CfgData, cbProgress);
            MainApp.ProgressBar_Percent(0);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Bool);
            sv.Value = result;
            return sv;
        }

        internal ScriptVariable c_jtag_epc2_erase(ScriptVariable[] arguments, uint Index) {
            bool e_result = MainApp.MAIN_FCUSB.JTAG_IF.EPC2_Erase();
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Bool);
            sv.Value = e_result;
            return sv;
        }

        internal ScriptVariable c_bsdl_new(ScriptVariable[] arguments, uint Index) {
            var n = new JTAG.BSDL_DEF();
            n.PART_NAME = Conversions.ToString(arguments[0].Value);
            int index_created = MainApp.MAIN_FCUSB.JTAG_IF.BSDL_Add(n);
            return new ScriptVariable(CurrentVars.GetNewName(), DataType.Integer, index_created);
        }

        internal ScriptVariable c_bsdl_find(ScriptVariable[] arguments, uint Index) {
            string param_name = Conversions.ToString(arguments[0].Value);
            int lib_ind = MainApp.MAIN_FCUSB.JTAG_IF.BSDL_Find(param_name);
            return new ScriptVariable(CurrentVars.GetNewName(), DataType.Integer, lib_ind);
        }

        internal ScriptVariable c_bsdl_param(ScriptVariable[] arguments, uint Index) {
            string param_name = Conversions.ToString(arguments[0].Value);
            uint param_value = Conversions.ToUInteger(arguments[1].Value);
            bool result = MainApp.MAIN_FCUSB.JTAG_IF.BSDL_SetParamater((int)Index, param_name, param_value);
            return new ScriptVariable(CurrentVars.GetNewName(), DataType.Bool, result);
        }

        internal ScriptVariable c_bsp_setup(ScriptVariable[] arguments, uint Index) {
            if (MainApp.MAIN_FCUSB is null) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "FlashcatUSB Professional must be connected via USB" };
            }
            if (!(MainApp.MAIN_FCUSB.HWBOARD == USB.FCUSB_BOARD.Professional_PCB5)) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "This command is only supported for FlashcatUSB Professional" };
            }
            if (!(MainApp.MyConsole.MyOperation.Mode == DeviceMode.JTAG)) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "This command is only supported when in JTAG mode" };
            }
            MainApp.MAIN_FCUSB.JTAG_IF.BoundaryScan_Setup();
            return null;
        }

        internal ScriptVariable c_bsp_init(ScriptVariable[] arguments, uint Index) {
            int flash_mode = 0; // 0=Automatic, 1=X8_OVER_X16
            if (arguments.Length == 1)
                flash_mode = Conversions.ToInteger(arguments[0].Value);
            bool result = MainApp.MAIN_FCUSB.JTAG_IF.BoundaryScan_Init(Conversions.ToBoolean(flash_mode));
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Bool);
            sv.Value = result;
            return sv;
        }

        internal ScriptVariable c_bsp_addpin(ScriptVariable[] arguments, uint Index) {
            string pin_name = Conversions.ToString(arguments[0].Value);
            int pin_output = Conversions.ToInteger(arguments[1].Value); // cell associated with the bidir or output cell
            int pin_control = Conversions.ToInteger(arguments[2].Value);  // cell associated with the control register bit
            int pin_input = -1; // cell associated with the input cell when output cell is not bidir
            if (arguments.Length == 4) {
                pin_input = Conversions.ToInteger(arguments[3].Value);
            }

            MainApp.MAIN_FCUSB.JTAG_IF.BoundaryScan_AddPin(pin_name, pin_output, pin_control, pin_input);
            return null;
        }

        internal ScriptVariable c_bsp_setbsr(ScriptVariable[] arguments, uint Index) {
            int pin_output = Conversions.ToInteger(arguments[0].Value);
            int pin_control = Conversions.ToInteger(arguments[1].Value);
            bool pin_level = Conversions.ToBoolean(arguments[2].Value);
            MainApp.MAIN_FCUSB.JTAG_IF.BoundaryScan_SetBSR(pin_output, pin_control, pin_level);
            return null;
        }

        internal ScriptVariable c_bsp_writebsr(ScriptVariable[] arguments, uint Index) {
            MainApp.MAIN_FCUSB.JTAG_IF.BoundaryScan_WriteBSR();
            return null;
        }

        internal ScriptVariable c_bsp_detect(ScriptVariable[] arguments, uint Index) {
            bool result = MainApp.MAIN_FCUSB.JTAG_IF.BoundaryScan_Detect();
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Bool);
            sv.Value = result;
            return sv;
        }

        internal ScriptVariable c_load_firmware(ScriptVariable[] arguments, uint Index) {
            switch (MainApp.MAIN_FCUSB.HWBOARD) {
                case USB.FCUSB_BOARD.Professional_PCB5: {
                        break;
                    }
                case USB.FCUSB_BOARD.Mach1: {
                        break;
                    }
                default: {
                        return new ScriptVariable("ERROR", DataType.FncError) { Value = "Only available for PRO or MACH1" };
                    }
            }
            MainApp.MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.FW_REBOOT, null, 0xFFFFFFFFU);
            return null;
        }

        internal ScriptVariable c_load_logic(ScriptVariable[] arguments, uint Index) {
            switch (MainApp.MAIN_FCUSB.HWBOARD) {
                case USB.FCUSB_BOARD.Professional_PCB5: {
                        MainApp.MAIN_FCUSB.LOGIC_SetVersion(0xFFFFFFFFU);
                        MainApp.FCUSBPRO_PCB5_Init(MainApp.MAIN_FCUSB, MainApp.MyConsole.MyOperation.Mode);
                        break;
                    }
                case USB.FCUSB_BOARD.Mach1: {
                        MainApp.MAIN_FCUSB.LOGIC_SetVersion(0xFFFFFFFFU);
                        MainApp.FCUSBMACH1_Init(MainApp.MAIN_FCUSB, MainApp.MyConsole.MyOperation.Mode);
                        break;
                    }
                default: {
                        return new ScriptVariable("ERROR", DataType.FncError) { Value = "Only available for PRO or MACH1" };
                    }
            }

            return null;
        }
        // Performs an erase of the logic
        internal ScriptVariable c_load_erase(ScriptVariable[] arguments, uint Index) {
            switch (MainApp.MAIN_FCUSB.HWBOARD) {
                case USB.FCUSB_BOARD.Mach1: {
                        MainApp.MACH1_FPGA_ERASE(MainApp.MAIN_FCUSB);
                        break;
                    }
            }

            return null;
        }

        internal ScriptVariable c_load_bootloader(ScriptVariable[] arguments, uint Index) {
            switch (MainApp.MAIN_FCUSB.HWBOARD) {
                case USB.FCUSB_BOARD.Professional_PCB5: {
                        break;
                    }

                case USB.FCUSB_BOARD.Mach1: {
                        break;
                    }

                default: {
                        return new ScriptVariable("ERROR", DataType.FncError) { Value = "Only available for PRO or MACH1" };
                    }
            }

            byte[] bl_data = (byte[])arguments[0].Value;
            if (bl_data is object && bl_data.Length > 0) {
                if (MainApp.MAIN_FCUSB.BootloaderUpdate(bl_data)) {
                    PrintConsole?.Invoke("Bootloader successfully updated");
                    MainApp.MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.FW_REBOOT, null, 0xFFFFFFFFU);
                } else {
                    PrintConsole?.Invoke("Bootloader update was not successful");
                }
            } else {
                PrintConsole?.Invoke("Error: Load.Bootloader requires data variable with valid data");
            }

            return null;
        }

        internal ScriptVariable c_flash_add(ScriptVariable[] arguments, uint Index) {
            string flash_name = Conversions.ToString(arguments[0].Value);
            int ID_MFG = Conversions.ToInteger(arguments[1].Value);
            int ID_PART = Conversions.ToInteger(arguments[2].Value);
            int flash_size = Conversions.ToInteger(arguments[3].Value); // Upgrade this to LONG in the future
            int flash_if = Conversions.ToInteger(arguments[4].Value);
            int block_layout = Conversions.ToInteger(arguments[5].Value);
            int prog_mode = Conversions.ToInteger(arguments[6].Value);
            int delay_mode = Conversions.ToInteger(arguments[7].Value);
            var new_mem_part = new FlashMemory.P_NOR(flash_name, (byte)ID_MFG, (ushort)ID_PART, (uint)flash_size, (FlashMemory.VCC_IF)flash_if, (FlashMemory.BLKLYT)block_layout, (FlashMemory.MFP_PRG)prog_mode, (FlashMemory.MFP_DELAY)delay_mode);
            MainApp.FlashDatabase.FlashDB.Add(new_mem_part);
            return null;
        }

        internal ScriptVariable c_parallel_test(ScriptVariable[] arguments, uint Index) {
            var td = new System.Threading.Thread(MainApp.MAIN_FCUSB.PARALLEL_NOR_IF.PARALLEL_PORT_TEST);
            td.Start();
            return null;
        }

        internal ScriptVariable c_parallel_command(ScriptVariable[] arguments, uint Index) {
            uint cmd_addr = Conversions.ToUInteger(arguments[0].Value);
            ushort cmd_data = (ushort)(Conversions.ToUInteger(arguments[1].Value) & 0xFFFFL);
            MainApp.MAIN_FCUSB.PARALLEL_NOR_IF.WriteCommandData(cmd_addr, cmd_data);
            return null;
        }

        internal ScriptVariable c_parallel_write(ScriptVariable[] arguments, uint Index) {
            uint cmd_addr = Conversions.ToUInteger(arguments[0].Value);
            ushort cmd_data = (ushort)(Conversions.ToUInteger(arguments[1].Value) & 0xFFFFL);
            MainApp.MAIN_FCUSB.PARALLEL_NOR_IF.WriteMemoryAddress(cmd_addr, cmd_data);
            return null;
        }

        internal ScriptVariable c_parallel_read(ScriptVariable[] arguments, uint Index) {
            uint cmd_addr = Conversions.ToUInteger(arguments[0].Value);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.UInteger);
            sv.Value = (object)MainApp.MAIN_FCUSB.PARALLEL_NOR_IF.ReadMemoryAddress(cmd_addr);
            return sv;
        }

        internal ScriptVariable c_msgbox(ScriptVariable[] arguments, uint Index) {
            string message_text = "";
            if (arguments[0].Data.VarType == DataType.Data) {
                byte[] d = (byte[])arguments[0].Value;
                message_text = "Data (" + Strings.Format(d.Length, "#,###") + " bytes)";
            } else {
                message_text = Conversions.ToString(arguments[0].Value);
            }

            MainApp.PromptUser_Msg(message_text);
            return null;
        }

        internal ScriptVariable c_writeline(ScriptVariable[] arguments, uint Index) {
            if (arguments[0].Data.VarType == DataType.Data) {
                byte[] d = (byte[])arguments[0].Value;
                bool display_addr = true;
                if (arguments.Length > 1) {
                    display_addr = Conversions.ToBoolean(arguments[1].Value);
                }

                int bytesLeft = d.Length;
                int i = 0;
                while (bytesLeft != 0) {
                    int bytes_to_display = Math.Min(bytesLeft, 16);
                    var sec = new byte[bytes_to_display];
                    Array.Copy(d, i, sec, 0, sec.Length);
                    string line_out = Utilities.Bytes.ToPaddedHexString(sec);
                    if (display_addr) {
                        PrintConsole?.Invoke("0x" + Conversion.Hex(i).PadLeft(6, '0') + ":  " + line_out);
                    } else {
                        PrintConsole?.Invoke(line_out);
                    }

                    i += bytes_to_display;
                    bytesLeft -= bytes_to_display;
                }
            } else {
                string message_text = Conversions.ToString(arguments[0].Value);
                PrintConsole?.Invoke(message_text);
            }

            return null;
        }

        internal ScriptVariable c_setstatus(ScriptVariable[] arguments, uint Index) {
            string message_text = Conversions.ToString(arguments[0].Value);
            SetStatus?.Invoke(message_text);
            return null;
        }

        internal ScriptVariable c_refresh(ScriptVariable[] arguments, uint Index) {
            int count = MainApp.MEM_IF.DeviceCount;
            for (int i = 0, loopTo = count - 1; i <= loopTo; i++) {
                var mem_device = MainApp.MEM_IF.GetDevice((uint)i);
                mem_device.RefreshControls();
            }

            return null;
        }

        internal ScriptVariable c_sleep(ScriptVariable[] arguments, uint Index) {
            int wait_ms = Conversions.ToInteger(arguments[0].Value);
            Utilities.Sleep(wait_ms);
            var sw = new Stopwatch();
            sw.Start();
            while (sw.ElapsedMilliseconds < wait_ms) {
                Utilities.Sleep(50);
            }

            return null;
        }

        internal ScriptVariable c_verify(ScriptVariable[] arguments, uint Index) {
            bool verify_bool = Conversions.ToBoolean(arguments[0].Value);
            MainApp.MySettings.VERIFY_WRITE = verify_bool;
            return null;
        }

        internal ScriptVariable c_mode(ScriptVariable[] arguments, uint Index) {
            var rv = new ScriptVariable(CurrentVars.GetNewName(), DataType.String);
            switch (CURRENT_DEVICE_MODE) {
                case DeviceMode.SPI: {
                        rv.Value = "SPI";
                        break;
                    }

                case DeviceMode.SPI_EEPROM: {
                        rv.Value = "SPI (EEPROM)";
                        break;
                    }

                case DeviceMode.JTAG: {
                        rv.Value = "JTAG";
                        break;
                    }

                case DeviceMode.I2C_EEPROM: {
                        rv.Value = "I2C";
                        break;
                    }

                case DeviceMode.PNOR: {
                        rv.Value = "Parallel NOR";
                        break;
                    }

                case DeviceMode.PNAND: {
                        rv.Value = "Parallel NAND";
                        break;
                    }

                case DeviceMode.ONE_WIRE: {
                        rv.Value = "1-WIRE";
                        break;
                    }

                case DeviceMode.SPI_NAND: {
                        rv.Value = "SPI-NAND";
                        break;
                    }

                case DeviceMode.EPROM: {
                        rv.Value = "EPROM/OTP";
                        break;
                    }

                case DeviceMode.HyperFlash: {
                        rv.Value = "HyperFlash";
                        break;
                    }

                case DeviceMode.Microwire: {
                        rv.Value = "Microwire";
                        break;
                    }

                case DeviceMode.SQI: {
                        rv.Value = "QUAD SPI";
                        break;
                    }

                case DeviceMode.FWH: {
                        rv.Value = "Firmware HUB";
                        break;
                    }

                default: {
                        rv.Value = "Other";
                        break;
                    }
            }

            return rv;
        }

        internal ScriptVariable c_ask(ScriptVariable[] arguments, uint Index) {
            string the_question = Conversions.ToString(arguments[0].Value);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Bool);
            sv.Value = (object)MainApp.PromptUser_Ask(the_question);
            return sv;
        }

        internal ScriptVariable c_endian(ScriptVariable[] arguments, uint Index) {
            string endian_mode = arguments[0].Value.ToString().ToUpper();
            switch (endian_mode ?? "") {
                case "MSB": {
                        MainApp.MySettings.BIT_ENDIAN = BitEndianMode.BigEndian32;
                        break;
                    }

                case "LSB": {
                        MainApp.MySettings.BIT_ENDIAN = BitEndianMode.LittleEndian32_16bit;
                        break;
                    }

                case "LSB16": {
                        MainApp.MySettings.BIT_ENDIAN = BitEndianMode.LittleEndian32_16bit;
                        break;
                    }

                case "LSB8": {
                        MainApp.MySettings.BIT_ENDIAN = BitEndianMode.LittleEndian32_8bit;
                        break;
                    }

                default: {
                        MainApp.MySettings.BIT_ENDIAN = BitEndianMode.BigEndian32;
                        break;
                    }
            }

            return null;
        }

        internal ScriptVariable c_abort(ScriptVariable[] arguments, uint Index) {
            ABORT_SCRIPT = true;
            PrintConsole?.Invoke("Aborting any running script");
            return null;
        }

        internal ScriptVariable c_catalog(ScriptVariable[] arguments, uint Index) {
            return null;
        }

        internal ScriptVariable c_cpen(ScriptVariable[] arguments, uint Index) {
            bool cp_en = Conversions.ToBoolean(arguments[0].Value);
            if (!MainApp.MAIN_FCUSB.IS_CONNECTED) {
                return new ScriptVariable("ERROR", DataType.FncError) { Value = "FlashcatUSB device is not connected" };
            }
            int w_index = 0;
            if (cp_en)
                w_index = 1;
            MainApp.MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.EXPIO_CPEN, null, (uint)w_index);
            if (cp_en) {
                PrintConsole?.Invoke("CPEN pin set to HIGH");
            } else {
                PrintConsole?.Invoke("CPEN pin set to LOW");
            }

            return null;
        }

        internal ScriptVariable c_crc16(ScriptVariable[] arguments, uint Index) {
            byte[] DataBytes = (byte[])arguments[0].Value;
            uint crc16_value = (uint)Utilities.CRC16.ComputeChecksum(DataBytes);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.UInteger);
            sv.Value = crc16_value;
            return sv;
        }

        internal ScriptVariable c_crc32(ScriptVariable[] arguments, uint Index) {
            byte[] DataBytes = (byte[])arguments[0].Value;
            uint crc32_value = Utilities.CRC32.ComputeChecksum(DataBytes);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.UInteger);
            sv.Value = crc32_value;
            return sv;
        }

        internal ScriptVariable c_cint(ScriptVariable[] arguments, uint Index) {
            uint value = Conversions.ToUInteger(arguments[0].Value);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.Integer);
            sv.Value = (int)value;
            return sv;
        }

        internal ScriptVariable c_cuint(ScriptVariable[] arguments, uint Index) {
            int value = Conversions.ToInteger(arguments[0].Value);
            var sv = new ScriptVariable(CurrentVars.GetNewName(), DataType.UInteger);
            sv.Value = (uint)value;
            return sv;
        }

    }

    internal class ScriptCmd {
        private List<ScriptCmd> Nests = new List<ScriptCmd>();
        private List<CmdEntry> Cmds = new List<CmdEntry>();

        public string Name { get; set; }

        public ScriptCmd(string group_name = "") {
            Name = group_name;
        }

        internal void Add(string cmd, CmdPrm[] @params, Delegate e) {
            var n_cmd = new CmdEntry();
            n_cmd.cmd = cmd;
            n_cmd.parameters = @params;
            n_cmd.fnc = e;
            Cmds.Add(n_cmd);
        }

        internal void AddNest(ScriptCmd sub_commands) {
            Nests.Add(sub_commands);
        }

        internal bool IsScriptFunction(string input) {
            string main_fnc = "";
            string sub_fnc = "";
            string argind_fnc = null;
            string argarguments = null;
            Tools.ParseToFunctionAndSub(input, ref main_fnc, ref sub_fnc, ref argind_fnc, ref argarguments);
            if (string.IsNullOrEmpty(sub_fnc)) {
                foreach (var item in Nests) {
                    if ((item.Name.ToUpper() ?? "") == (main_fnc.ToUpper() ?? ""))
                        return true;
                }

                foreach (var s in Cmds) {
                    if ((s.cmd.ToUpper() ?? "") == (main_fnc.ToUpper() ?? ""))
                        return true;
                }
            } else {
                foreach (var item in Nests) {
                    if ((item.Name.ToUpper() ?? "") == (main_fnc.ToUpper() ?? "")) {
                        foreach (var s in item.Cmds) {
                            if ((s.cmd.ToUpper() ?? "") == (sub_fnc.ToUpper() ?? ""))
                                return true;
                        }
                    }
                }

                return false;
            }

            return false;
        }

        internal bool GetScriptFunction(string fnc_name, string sub_fnc, ref CmdPrm[] @params, ref Delegate e) {
            if (string.IsNullOrEmpty(sub_fnc)) {
                foreach (var s in Cmds) {
                    if ((s.cmd.ToUpper() ?? "") == (fnc_name.ToUpper() ?? "")) {
                        @params = s.parameters;
                        e = s.fnc;
                        return true;
                    }
                }
            } else {
                foreach (var item in Nests) {
                    if ((item.Name.ToUpper() ?? "") == (fnc_name.ToUpper() ?? "")) {
                        foreach (var s in item.Cmds) {
                            if ((s.cmd.ToUpper() ?? "") == (sub_fnc.ToUpper() ?? "")) {
                                @params = s.parameters;
                                e = s.fnc;
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            return default;
        }
    }

    internal struct CmdEntry {
        public string cmd;
        public CmdPrm[] parameters;
        public Delegate fnc;
    }

    internal class ExecuteParam {
        public ExitMode exit_task;
        public uint err_line;
        public string err_reason;
        public string goto_label;
    }

    public class ParseResult {
        public string ErrorMsg;

        public bool IsError { get; private set; }

        public ParseResult() {
            IsError = false;
        }

        public ParseResult(string error_msg) {
            ErrorMsg = error_msg;
            IsError = true;
        }
    }

    internal class ScriptElementOperand {
        private Processor MyParent;
        public List<ScriptElementOperandEntry> OPERANDS = new List<ScriptElementOperandEntry>();
        public ParseResult ParsingResult;

        public ScriptElementOperand(Processor oParent) {
            ParsingResult = new ParseResult(); // No Error
            MyParent = oParent;
        }

        internal ParseResult Parse(string text_input) {
            text_input = text_input.Trim();
            while (!text_input.Equals("")) {
                if (text_input.StartsWith("(")) {
                    string sub_section = Tools.FeedParameter(ref text_input);
                    var x = new ScriptElementOperandEntry(MyParent, ScriptElementDataType.SubItems);
                    x.SubOperands = new ScriptElementOperand(MyParent);
                    var result = x.SubOperands.Parse(sub_section);
                    if (result.IsError)
                        return result;
                    OPERANDS.Add(x);
                } else {
                    string main_element = Tools.FeedElement(ref text_input);
                    if (MyParent.CmdFunctions.IsScriptFunction(main_element)) {
                        OPERANDS.Add(ParseFunctionInput(main_element));
                    } else if (MyParent.IsScriptEvent(main_element)) {
                        OPERANDS.Add(ParseEventInput(main_element));
                    } else if (MyParent.CurrentVars.IsVariable(main_element)) {
                        OPERANDS.Add(ParseVarInput(main_element));
                    } else if (main_element.ToUpper().Equals("TRUE") || main_element.ToUpper().Equals("FALSE")) {
                        OPERANDS.Add(new ScriptElementOperandEntry(MyParent, DataType.Bool, Conversions.ToBoolean(main_element)));
                    } else if (Utilities.IsDataType.Integer(main_element)) {
                        OPERANDS.Add(new ScriptElementOperandEntry(MyParent, DataType.Integer, Conversions.ToInteger(main_element)));
                    } else if (Utilities.IsDataType.Uinteger(main_element)) {
                        OPERANDS.Add(new ScriptElementOperandEntry(MyParent, DataType.UInteger, Conversions.ToUInteger(main_element)));
                    } else if (main_element.EndsWith("U") && Utilities.IsDataType.Uinteger(main_element.Substring(0, main_element.Length - 1))) {
                        OPERANDS.Add(new ScriptElementOperandEntry(MyParent, DataType.UInteger, Conversions.ToUInteger(main_element.Substring(0, main_element.Length - 1))));
                    } else if (Utilities.IsDataType.String(main_element)) {
                        OPERANDS.Add(new ScriptElementOperandEntry(MyParent, DataType.String, Utilities.RemoveQuotes(main_element)));
                    } else if (Utilities.IsDataType.Bool(main_element)) {
                        OPERANDS.Add(new ScriptElementOperandEntry(MyParent, DataType.Bool, Conversions.ToBoolean(main_element)));
                    } else if (Tools.IsDataArrayType(main_element)) {
                        var dr = Tools.DataArrayTypeToBytes(main_element);
                        OPERANDS.Add(new ScriptElementOperandEntry(MyParent, DataType.Data, dr));
                    } else if (main_element.ToUpper().StartsWith("0X") && Utilities.IsDataType.Hex(main_element)) {
                        var d = Utilities.Bytes.FromHexString(main_element);
                        if (d.Length > 4) {
                            OPERANDS.Add(new ScriptElementOperandEntry(MyParent, DataType.Data, d));
                        } else {
                            OPERANDS.Add(new ScriptElementOperandEntry(MyParent, DataType.UInteger, (object)Utilities.Bytes.ToUInt32(d)));
                        }
                    } else if (main_element.ToUpper().Equals("NOTHING")) {
                        OPERANDS.Add(new ScriptElementOperandEntry(MyParent, (ScriptElementDataType)DataType.NULL));
                    } else if (IsVariableArgument(main_element)) {
                        OPERANDS.Add(new ScriptElementOperandEntry(MyParent, ScriptElementDataType.Variable) { FUNC_NAME = main_element });
                    } else if (main_element.Equals("")) {
                        return new ParseResult("Unknown function or command: " + text_input);
                    } else {
                        return new ParseResult("Unknown function or command: " + main_element);
                    }

                    if (!text_input.Equals("")) {
                        var oper_seperator = OperandOper.NOTSPECIFIED;
                        if (Tools.FeedOperator(ref text_input, ref oper_seperator)) {
                            OPERANDS.Add(new ScriptElementOperandEntry(MyParent, ScriptElementDataType.Operator) { Oper = oper_seperator });
                        } else {
                            return new ParseResult("Invalid operator");
                        }
                    }
                }
            }

            return new ParseResult(); // No Error
        }
        // For $1 $2 etc.
        private bool IsVariableArgument(string input) {
            if (!input.StartsWith("$"))
                return false;
            input = input.Substring(1);
            if (string.IsNullOrEmpty(input))
                return false;
            if (!Information.IsNumeric(input))
                return false;
            return true;
        }

        private ScriptElementOperandEntry ParseFunctionInput(string to_parse) {
            var new_fnc = new ScriptElementOperandEntry(MyParent, ScriptElementDataType.Function);
            string arguments = "";
            string argmain_fnc = new_fnc.FUNC_NAME;
            string argsub_fnc = new_fnc.FUNC_SUB;
            string argind_fnc = new_fnc.FUNC_IND;
            Tools.ParseToFunctionAndSub(to_parse, ref argmain_fnc, ref argsub_fnc, ref argind_fnc, ref arguments);
            new_fnc.FUNC_NAME = argmain_fnc;
            new_fnc.FUNC_SUB = argsub_fnc;
            new_fnc.FUNC_IND = argind_fnc;
            if (!arguments.Equals("")) {
                new_fnc.FUNC_ARGS = ParseArguments(arguments);
                if (ParsingResult.IsError)
                    return null;
            }

            return new_fnc;
        }

        private ScriptElementOperandEntry ParseEventInput(string to_parse) {
            var new_evnt = new ScriptElementOperandEntry(MyParent, ScriptElementDataType.Event);
            string arguments = "";
            string argmain_fnc = new_evnt.FUNC_NAME;
            string argsub_fnc = null;
            string argind_fnc = null;
            Tools.ParseToFunctionAndSub(to_parse, ref argmain_fnc, ref argsub_fnc, ref argind_fnc, ref arguments);
            new_evnt.FUNC_NAME = argmain_fnc;
            if (!string.IsNullOrEmpty(arguments)) {
                new_evnt.FUNC_ARGS = ParseArguments(arguments);
                if (ParsingResult.IsError)
                    return null;
            }

            return new_evnt;
        }

        private ScriptElementOperandEntry ParseVarInput(string to_parse) {
            var new_Var = new ScriptElementOperandEntry(MyParent, ScriptElementDataType.Variable);
            string var_name = "";
            string arguments = "";
            string argsub_fnc = null;
            string argind_fnc = null;
            Tools.ParseToFunctionAndSub(to_parse, ref var_name, ref argsub_fnc, ref argind_fnc, ref arguments);
            new_Var.FUNC_NAME = var_name;
            if (!string.IsNullOrEmpty(arguments)) {
                new_Var.FUNC_ARGS = ParseArguments(arguments);
                if (ParsingResult.IsError)
                    return null;
            }

            return new_Var;
        }

        private ScriptElementOperand[] ParseArguments(string argument_line) {
            var argument_list = new List<ScriptElementOperand>();
            var arg_builder = new System.Text.StringBuilder();
            bool in_quote = false;
            bool in_param = false;
            for (int i = 0, loopTo = argument_line.Length - 1; i <= loopTo; i++) {
                char c = Conversions.ToChar(argument_line.Substring(i, 1));
                arg_builder.Append(c);
                bool add_clear = false;
                if (in_quote) {
                    if (c.Equals('"'))
                        in_quote = false;
                } else if (in_param) {
                    if (c.Equals(')'))
                        in_param = false;
                    if (c.Equals('"'))
                        in_quote = true;
                } else {
                    switch (c) {
                        case '(': {
                                in_param = true;
                                break;
                            }

                        case '"': {
                                in_quote = true;
                                break;
                            }

                        case ',': {
                                add_clear = true;
                                arg_builder.Remove(arg_builder.Length - 1, 1); // Remove last item
                                break;
                            }
                    }
                }

                if (add_clear) {
                    var n = new ScriptElementOperand(MyParent);
                    var result = n.Parse(arg_builder.ToString());
                    if (result.IsError) {
                        return null;
                    }

                    argument_list.Add(n);
                    arg_builder.Clear();
                }
            }

            if (arg_builder.Length > 0) {
                var n = new ScriptElementOperand(MyParent);
                ParsingResult = n.Parse(arg_builder.ToString());
                if (ParsingResult.IsError)
                    return null;
                argument_list.Add(n);
            }

            return argument_list.ToArray();
        }

        internal ScriptVariable CompileToVariable(ref ExitMode exit_task) {
            ScriptVariable current_var = null;
            var current_oper = OperandOper.NOTSPECIFIED;
            int arg_count = OPERANDS.Count;
            int x = 0;
            while (x < arg_count) {
                if (OPERANDS[x].EntryType == ScriptElementDataType.Operator) {
                    return Tools.CreateError("Expected value to compute");
                }

                var new_var = OPERANDS[x].Compile(ref exit_task);
                if (new_var is object && new_var.Data.VarType == DataType.FncError)
                    return new_var;
                if (!(current_oper == OperandOper.NOTSPECIFIED)) {
                    current_var = Tools.CompileSVars(current_var, new_var, current_oper);
                } else {
                    current_var = new_var;
                }

                x += 1; // increase pointer
                if (x < arg_count) { // There are more items
                    if (!(OPERANDS[x].Oper == OperandOper.NOTSPECIFIED)) {
                        current_oper = OPERANDS[x].Oper;
                        x += 1;
                        if (!(x < arg_count)) {
                            return Tools.CreateError("Statement ended in an operand operation");
                        }
                    } else {
                        return Tools.CreateError("Expected an operand operation");
                    }
                }
            }

            return current_var;
        }

        public override string ToString() {
            var str_builder = new System.Text.StringBuilder();
            foreach (var item in OPERANDS)
                str_builder.Append("(" + item.ToString() + ")");
            return str_builder.ToString().Trim();
        }
    }

    internal class ScriptElementOperandEntry {
        private Processor MyParent;

        public ScriptElementDataType EntryType { get; private set; } // [Data] [Operator] [SubItems] [Variable] [Function] [Event]
        public OperandOper Oper { get; set; } = OperandOper.NOTSPECIFIED; // [ADD] [SUB] [MULT] [DIV] [AND] [OR] [S_LEFT] [S_RIGHT] [IS] [LESS_THAN] [GRT_THAN]
        public ScriptElementOperand SubOperands { get; set; }
        public string FUNC_NAME { get; set; } // Name of the function, event or variable
        public string FUNC_SUB { get; set; } = ""; // Name of the function.sub
        public string FUNC_IND { get; set; } // Index for a given function (integer or variable)
        public ScriptElementOperand[] FUNC_ARGS { get; set; }
        public DataTypeObject FUNC_DATA { get; set; } = null;

        public ScriptElementOperandEntry(Processor oParent, ScriptElementDataType entry_t) {
            EntryType = entry_t;
            MyParent = oParent;
        }

        public ScriptElementOperandEntry(Processor oParent, DataType dt, object dv) {
            MyParent = oParent;
            EntryType = ScriptElementDataType.Data;
            FUNC_DATA = new DataTypeObject(dt, dv);
        }

        public override string ToString() {
            switch (EntryType) {
                case ScriptElementDataType.Data: {
                        switch (FUNC_DATA.VarType) {
                            case DataType.Integer: {
                                    return Conversions.ToInteger(FUNC_DATA.Value).ToString();
                                }

                            case DataType.UInteger: {
                                    return Conversions.ToUInteger(FUNC_DATA.Value).ToString();
                                }

                            case DataType.String: {
                                    return "\"" + Conversions.ToString(FUNC_DATA.Value) + "\"";
                                }

                            case DataType.Data: {
                                    return Utilities.Bytes.ToPaddedHexString((byte[])FUNC_DATA.Value);
                                }

                            case DataType.Bool: {
                                    switch (Conversions.ToBoolean(FUNC_DATA.Value)) {
                                        case true: {
                                                return "True";
                                            }

                                        case false: {
                                                return "False";
                                            }
                                    }
                                }

                            case DataType.FncError: {
                                    return "Error: " + Conversions.ToString(FUNC_DATA.Value);
                                }
                        }

                        break;
                    }

                case ScriptElementDataType.Event: {
                        return "Event: " + FUNC_NAME;
                    }

                case ScriptElementDataType.Function: {
                        if (!string.IsNullOrEmpty(FUNC_SUB)) {
                            return "Function: " + FUNC_NAME + "." + FUNC_SUB;
                        } else {
                            return "Function: " + FUNC_NAME;
                        }
                    }

                case ScriptElementDataType.Variable: {
                        return "Variable (" + FUNC_NAME + ")";
                    }

                case ScriptElementDataType.Operator: {
                        switch (Oper) {
                            case OperandOper.ADD: {
                                    return "ADD Operator";
                                }

                            case OperandOper.SUB: {
                                    return "SUB Operator";
                                }

                            case OperandOper.MULT: {
                                    return "MULT Operator";
                                }

                            case OperandOper.DIV: {
                                    return "DIV Operator";
                                }

                            case OperandOper.AND: {
                                    return "AND Operator";
                                }

                            case OperandOper.OR: {
                                    return "OR Operator";
                                }

                            case OperandOper.S_LEFT: {
                                    return "<< Operator";
                                }

                            case OperandOper.S_RIGHT: {
                                    return ">> Operator";
                                }

                            case OperandOper.IS: {
                                    return "Is Operator";
                                }

                            case OperandOper.LESS_THAN: {
                                    return "< Operator";
                                }

                            case OperandOper.GRT_THAN: {
                                    return "> Operator";
                                }
                        }

                        break;
                    }

                case ScriptElementDataType.SubItems: {
                        return "Sub Items: " + SubOperands.OPERANDS.Count;
                    }
            }

            return "";
        }

        internal ScriptVariable Compile(ref ExitMode exit_task) {
            switch (EntryType) {
                case ScriptElementDataType.Data: {
                        var new_sv = new ScriptVariable(MyParent.CurrentVars.GetNewName(), FUNC_DATA.VarType);
                        new_sv.Value = FUNC_DATA.Value;
                        return new_sv;
                    }

                case ScriptElementDataType.Function: {
                        CmdPrm[] fnc_params = null;
                        Delegate fnc = null;
                        if (MyParent.CmdFunctions.GetScriptFunction(FUNC_NAME, FUNC_SUB, ref fnc_params, ref fnc)) {
                            var input_vars = new List<ScriptVariable>();
                            if (FUNC_ARGS is object) {
                                for (int i = 0, loopTo = FUNC_ARGS.Length - 1; i <= loopTo; i++) {
                                    var ret = FUNC_ARGS[i].CompileToVariable(ref exit_task);
                                    if (ret.Data.VarType == DataType.FncError)
                                        return ret;
                                    if (ret is object)
                                        input_vars.Add(ret);
                                }
                            }

                            var args_var = input_vars.ToArray();
                            var result = CheckFunctionArguments(fnc_params, ref args_var);
                            if (result.IsError)
                                return Tools.CreateError(result.ErrorMsg);
                            try {
                                uint func_index = 0U;
                                if (Information.IsNumeric(FUNC_IND)) {
                                    func_index = Conversions.ToUInteger(FUNC_IND);
                                } else if (Utilities.IsDataType.HexString(FUNC_IND)) {
                                    func_index = Utilities.HexToUInt(FUNC_IND);
                                } else if (MyParent.CurrentVars.IsVariable(FUNC_IND)) {
                                    var v = MyParent.CurrentVars.GetVariable(FUNC_IND);
                                    if (v.Data.VarType == DataType.Integer) {
                                        func_index = (uint)Conversions.ToInteger(MyParent.CurrentVars.GetVariable(FUNC_IND).Value);
                                    } else if (v.Data.VarType == DataType.UInteger) {
                                        func_index = Conversions.ToUInteger(MyParent.CurrentVars.GetVariable(FUNC_IND).Value);
                                    } else {
                                        return Tools.CreateError("Index " + FUNC_IND + " must be either an Integer or UInteger");
                                    }
                                } else {
                                    return Tools.CreateError("Unable to evaluate index: " + FUNC_IND);
                                }

                                ScriptVariable dynamic_result = null;
                                try {
                                    dynamic_result = (ScriptVariable)fnc.DynamicInvoke(args_var, func_index);
                                } catch {
                                    return Tools.CreateError(FUNC_NAME + " function exception");
                                }

                                return dynamic_result;
                            } catch {
                                return Tools.CreateError("Error executing function: " + FUNC_NAME);
                            }
                        } else {
                            return Tools.CreateError("Unknown function or sub procedure");
                        }
                    }
                case ScriptElementDataType.Event: {
                        var input_vars = new List<ScriptVariable>();
                        if (FUNC_ARGS is object) {
                            for (int i = 0, loopTo1 = FUNC_ARGS.Length - 1; i <= loopTo1; i++) {
                                var ret = FUNC_ARGS[i].CompileToVariable(ref exit_task);
                                if (ret.Data.VarType == DataType.FncError)
                                    return ret;
                                if (ret is object)
                                    input_vars.Add(ret);
                            }
                        }

                        var se = MyParent.GetScriptEvent(FUNC_NAME);
                        if (se is null)
                            return Tools.CreateError("Event does not exist: " + FUNC_NAME);
                        var n_sv = MyParent.ExecuteScriptEvent(se, input_vars.ToArray(), ref exit_task);
                        return n_sv;
                    }

                case ScriptElementDataType.Variable: {
                        var n_sv = MyParent.CurrentVars.GetVariable(FUNC_NAME);
                        if (n_sv.Data.VarType == DataType.NULL)
                            return null;
                        if (n_sv.Data.VarType == DataType.Data && FUNC_ARGS is object) {
                            try {
                                if (FUNC_ARGS.Length == 1) {
                                    var data_index_var = FUNC_ARGS[0].CompileToVariable(ref exit_task);
                                    if (data_index_var.Data.VarType == DataType.UInteger) {
                                        byte[] data = (byte[])n_sv.Value;
                                        uint data_index = Conversions.ToUInteger(data_index_var.Value);
                                        var new_sv = new ScriptVariable(MyParent.CurrentVars.GetNewName(), DataType.UInteger);
                                        new_sv.Value = data[(int)data_index];
                                        return new_sv;
                                    }
                                }
                            } catch {
                                return Tools.CreateError("Error processing variable index value");
                            }
                        } else {
                            return n_sv;
                        }

                        break;
                    }

                case ScriptElementDataType.SubItems: {
                        return SubOperands.CompileToVariable(ref exit_task);
                    }
            }

            return null;
        }

        private ParseResult CheckFunctionArguments(CmdPrm[] fnc_params, ref ScriptVariable[] my_vars) {
            uint var_count = 0U;
            if (my_vars is null || my_vars.Length == 0) {
                var_count = 0U;
            } else {
                var_count = (uint)my_vars.Length;
            }

            if (fnc_params is null && !(var_count == 0L)) {
                return new ParseResult("Function " + GetFuncString() + ": arguments supplied but none are allowed");
            } else if (fnc_params is object) {
                for (int i = 0, loopTo = fnc_params.Length - 1; i <= loopTo; i++) {
                    switch (fnc_params[i]) {
                        case CmdPrm.Integer: {
                                if (i >= var_count) {
                                    return new ParseResult("Function " + GetFuncString() + ": argument " + (i + 1) + " requires an Integer type parameter");
                                } else {
                                    if (my_vars[i].Data.VarType == DataType.UInteger) { // We can possibly auto convert data type
                                        uint auto_con = Conversions.ToUInteger(my_vars[i].Value);
                                        if (auto_con <= (long)int.MaxValue)
                                            my_vars[i] = new ScriptVariable(my_vars[i].Name, DataType.Integer, (int)auto_con);
                                    }

                                    if (!(my_vars[i].Data.VarType == DataType.Integer)) {
                                        return new ParseResult("Function " + GetFuncString() + ": argument " + (i + 1) + " inputs an Integer but was supplied a " + Tools.GetDataTypeString(my_vars[i].Data.VarType));
                                    }
                                }

                                break;
                            }

                        case CmdPrm.UInteger: {
                                if (i >= var_count) {
                                    return new ParseResult("Function " + GetFuncString() + ": argument " + (i + 1) + " requires an UInteger type parameter");
                                } else {
                                    if (my_vars[i].Data.VarType == DataType.Integer) { // We can possibly auto convert data type
                                        int auto_con = Conversions.ToInteger(my_vars[i].Value);
                                        if (auto_con >= 0)
                                            my_vars[i] = new ScriptVariable(my_vars[i].Name, DataType.UInteger, (uint)auto_con);
                                    }

                                    if (!(my_vars[i].Data.VarType == DataType.UInteger)) {
                                        return new ParseResult("Function " + GetFuncString() + ": argument " + (i + 1) + " inputs an UInteger but was supplied a " + Tools.GetDataTypeString(my_vars[i].Data.VarType));
                                    }
                                }

                                break;
                            }

                        case CmdPrm.String: {
                                if (i >= var_count) {
                                    return new ParseResult("Function " + GetFuncString() + ": argument " + (i + 1) + " requires a String type parameter");
                                } else if (!(my_vars[i].Data.VarType == DataType.String)) {
                                    return new ParseResult("Function " + GetFuncString() + ": argument " + (i + 1) + " inputs a String but was supplied a " + Tools.GetDataTypeString(my_vars[i].Data.VarType));
                                }

                                break;
                            }

                        case CmdPrm.Data: {
                                if (i >= var_count) {
                                    return new ParseResult("Function " + GetFuncString() + ": argument " + (i + 1) + " requires a Data type parameter");
                                } else if (my_vars[i].Data.VarType == DataType.Data) {
                                } else if (my_vars[i].Data.VarType == DataType.UInteger) {
                                    var c = new ScriptVariable(my_vars[i].Name, DataType.Data);
                                    c.Value = Utilities.Bytes.FromUInt32(Conversions.ToUInteger(my_vars[i].Value));
                                    my_vars[i] = c;
                                } else {
                                    return new ParseResult("Function " + GetFuncString() + ": argument " + (i + 1) + " inputs an Data but was supplied a " + Tools.GetDataTypeString(my_vars[i].Data.VarType));
                                }

                                break;
                            }

                        case CmdPrm.Bool: {
                                if (i >= var_count) {
                                    return new ParseResult("Function " + GetFuncString() + ": argument " + (i + 1) + " requires a Bool type parameter");
                                } else if (!(my_vars[i].Data.VarType == DataType.Bool)) {
                                    return new ParseResult("Function " + GetFuncString() + ": argument " + (i + 1) + " inputs an Bool but was supplied a " + Tools.GetDataTypeString(my_vars[i].Data.VarType));
                                }

                                break;
                            }

                        case CmdPrm.Any: {
                                if (i >= var_count) {
                                    return new ParseResult("Function " + GetFuncString() + ": argument " + (i + 1) + " requires a parameter");
                                }

                                break;
                            }

                        case CmdPrm.Integer_Optional: {
                                if (!(i >= var_count)) { // Only check if we have supplied this argument
                                    if (my_vars[i].Data.VarType == DataType.UInteger) { // We can possibly auto convert data type
                                        uint auto_con = Conversions.ToUInteger(my_vars[i].Value);
                                        if (auto_con <= (long)int.MaxValue)
                                            my_vars[i] = new ScriptVariable(my_vars[i].Name, DataType.Integer, (int)auto_con);
                                    }

                                    if (!(my_vars[i].Data.VarType == DataType.Integer)) {
                                        return new ParseResult("Function " + GetFuncString() + ": argument " + (i + 1) + " inputs an Integer but was supplied a " + Tools.GetDataTypeString(my_vars[i].Data.VarType));
                                    }
                                }

                                break;
                            }

                        case CmdPrm.UInteger_Optional: {
                                if (!(i >= var_count)) { // Only check if we have supplied this argument
                                    if (my_vars[i].Data.VarType == DataType.Integer) { // We can possibly auto convert data type
                                        int auto_con = Conversions.ToInteger(my_vars[i].Value);
                                        if (auto_con >= 0)
                                            my_vars[i] = new ScriptVariable(my_vars[i].Name, DataType.UInteger, (uint)auto_con);
                                    }

                                    if (!(my_vars[i].Data.VarType == DataType.UInteger)) {
                                        return new ParseResult("Function " + GetFuncString() + ": argument " + (i + 1) + " inputs an UInteger but was supplied a " + Tools.GetDataTypeString(my_vars[i].Data.VarType));
                                    }
                                }

                                break;
                            }

                        case CmdPrm.String_Optional: {
                                if (!(i >= var_count)) { // Only check if we have supplied this argument
                                    if (!(my_vars[i].Data.VarType == DataType.String)) {
                                        return new ParseResult("Function " + GetFuncString() + ": argument " + (i + 1) + " inputs an String but was supplied a " + Tools.GetDataTypeString(my_vars[i].Data.VarType));
                                    }
                                }

                                break;
                            }

                        case CmdPrm.Data_Optional: {
                                if (!(i >= var_count)) { // Only check if we have supplied this argument
                                    if (!(my_vars[i].Data.VarType == DataType.Data)) {
                                        return new ParseResult("Function " + GetFuncString() + ": argument " + (i + 1) + " inputs Data but was supplied a " + Tools.GetDataTypeString(my_vars[i].Data.VarType));
                                    }
                                }

                                break;
                            }

                        case CmdPrm.Bool_Optional: {
                                if (!(i >= var_count)) { // Only check if we have supplied this argument
                                    if (!(my_vars[i].Data.VarType == DataType.Bool)) {
                                        return new ParseResult("Function " + GetFuncString() + ": argument " + (i + 1) + " inputs Bool but was supplied a " + Tools.GetDataTypeString(my_vars[i].Data.VarType));
                                    }
                                }

                                break;
                            }

                        case CmdPrm.Any_Optional: {
                                break;
                            }
                    }
                }
            }

            return new ParseResult();
        }

        internal string GetFuncString() {
            if (string.IsNullOrEmpty(FUNC_SUB)) {
                return FUNC_NAME;
            } else {
                return FUNC_NAME + "." + FUNC_SUB;
            }
        }
    }

    internal class ScriptFile {
        private Processor MyParent;
        public List<ScriptLineElement> TheScript = new List<ScriptLineElement>();
        public List<string> EventList = new List<string>();

        public ScriptFile() {
        }

        public void Reset() {
            TheScript.Clear();
        }

        internal bool LoadFile(Processor oParent, string[] lines, ref int ErrInd, ref string ErrorMsg) {
            TheScript.Clear();
            MyParent = oParent;
            var line_index = new uint[lines.Length];
            for (int i = 0, loopTo = lines.Length - 1; i <= loopTo; i++)
                line_index[i] = (uint)i;
            ProcessEvents(lines);
            uint argErrInd = (uint)ErrInd;
            var Result = ProcessText(lines, line_index, ref argErrInd, ref ErrorMsg);
            if (string.IsNullOrEmpty(ErrorMsg)) { // No Error
                foreach (var item in Result)
                    TheScript.Add(item);
            } else {
                return false;
            }

            return true; // No errors, all lines successfully parsed in
        }
        // Begins an initial process of the script and populates the EventList list
        private void ProcessEvents(string[] lines) {
            EventList.Clear();
            foreach (var line in lines) {
                string cmd_line = Utilities.RemoveComment(line.Replace(Constants.vbTab, " ")).Trim();
                if (cmd_line.ToUpper().StartsWith("CREATEEVENT")) {
                    if (cmd_line.ToUpper().StartsWith("CREATEEVENT"))
                        cmd_line = cmd_line.Substring(11).Trim();
                    string event_name = Tools.FeedParameter(ref cmd_line);
                    if (!string.IsNullOrEmpty(event_name)) {
                        EventList.Add(event_name);
                    }
                }
            }
        }

        internal ScriptLineElement[] ProcessText(string[] lines, uint[] line_index, ref uint ErrInd, ref string ErrorMsg) {
            if (lines is null)
                return null;
            if (!(lines.Length == line_index.Length))
                return null;
            for (int i = 0, loopTo = lines.Length - 1; i <= loopTo; i++)
                lines[i] = Utilities.RemoveComment(lines[i].Replace(Constants.vbTab, " ")).Trim(); // This is the initial formatting of each text line
            int line_pointer = 0;
            try {
                var Processed = new List<ScriptLineElement>();
                while (line_pointer < lines.Length) {
                    string cmd_line = lines[line_pointer];
                    if (!string.IsNullOrEmpty(cmd_line)) {
                        if (cmd_line.ToUpper().StartsWith("IF ")) { // We are doing an if condition
                            var s = CreateIfCondition(ref line_pointer, lines, line_index, ref ErrInd, ref ErrorMsg); // Increments line pointer
                            if (s is null || !string.IsNullOrEmpty(ErrorMsg))
                                return null;
                            Processed.Add(s);
                        } else if (cmd_line.ToUpper().StartsWith("FOR ")) {
                            var s = CreateForLoop(ref line_pointer, lines, line_index, ref ErrInd, ref ErrorMsg); // Increments line pointer
                            if (s is null || !string.IsNullOrEmpty(ErrorMsg))
                                return null;
                            Processed.Add(s);
                        } else if (cmd_line.ToUpper().StartsWith("CREATEEVENT(") || cmd_line.ToUpper().StartsWith("CREATEEVENT ")) {
                            var s = CreateEvent(ref line_pointer, lines, line_index, ref ErrInd, ref ErrorMsg);
                            if (s is null || !string.IsNullOrEmpty(ErrorMsg))
                                return null;
                            Processed.Add(s);
                        } else if (cmd_line.ToUpper().StartsWith("GOTO ")) {
                            var s = CreateGoto(ref line_pointer, lines, line_index, ref ErrInd, ref ErrorMsg);
                            if (s is null || !string.IsNullOrEmpty(ErrorMsg))
                                return null;
                            Processed.Add(s);
                        } else if (cmd_line.ToUpper().StartsWith("EXIT ") || cmd_line.ToUpper() == "EXIT") {
                            var s = CreateExit(ref line_pointer, lines, line_index, ref ErrInd, ref ErrorMsg);
                            if (s is null || !string.IsNullOrEmpty(ErrorMsg))
                                return null;
                            Processed.Add(s);
                        } else if (cmd_line.ToUpper().StartsWith("RETURN ")) {
                            var s = CreateReturn(ref line_pointer, lines, line_index, ref ErrInd, ref ErrorMsg);
                            if (s is null || !string.IsNullOrEmpty(ErrorMsg))
                                return null;
                            Processed.Add(s);
                        } else if (cmd_line.ToUpper().EndsWith(":") && cmd_line.IndexOf(" ") == -1) {
                            var s = CreateLabel(ref line_pointer, lines, line_index, ref ErrInd, ref ErrorMsg);
                            if (s is null || !string.IsNullOrEmpty(ErrorMsg))
                                return null;
                            Processed.Add(s);
                        } else {
                            var s = new ScriptElement(MyParent);
                            s.INDEX = line_index[line_pointer];
                            var result = s.Parse(cmd_line, true);
                            if (result.IsError) {
                                ErrorMsg = result.ErrorMsg;
                                ErrInd = line_index[line_pointer];
                                return null;
                            }

                            if (!string.IsNullOrEmpty(s.TARGET_NAME)) { // This element creates a new variable
                                MyParent.CurrentVars.AddExpected(s.TARGET_NAME);
                            }

                            Processed.Add(s);
                        }
                    }

                    line_pointer += 1;
                }

                return Processed.ToArray();
            } catch {
                ErrInd = line_index[line_pointer];
                ErrorMsg = "General statement evaluation error";
                return null;
            }
        }

        internal ScriptLineElement CreateIfCondition(ref int Pointer, string[] lines, uint[] ind, ref uint ErrInd, ref string ErrorMsg) {
            var this_if = new ScriptCondition(MyParent); // Also loads NOT modifier
            this_if.INDEX = ind[Pointer];
            var result = this_if.Parse(lines[Pointer]);
            if (result.IsError) {
                ErrInd = ind[Pointer];
                ErrorMsg = result.ErrorMsg;
                return null;
            }

            if (this_if.CONDITION is null) {
                ErrInd = (uint)Pointer;
                ErrorMsg = "IF condition is not valid";
                return null;
            }

            var IFMain = new List<string>();
            var IfElse = new List<string>();
            var IFMain_Index = new List<uint>();
            var IfElse_Index = new List<uint>();
            bool ElseTrigger = false;
            bool EndIfTrigger = false;
            int level = 0;
            while (Pointer < lines.Length) {
                string eval = Utilities.RemoveComment(lines[Pointer].Replace(Constants.vbTab, " ")).Trim();
                if (!string.IsNullOrEmpty(eval)) {
                    if (eval.ToUpper().StartsWith("IF ")) {
                        level += 1;
                    } else if (eval.ToUpper().StartsWith("ENDIF") || eval.ToUpper().StartsWith("END IF")) {
                        level -= 1;
                        if (level == 0) {
                            EndIfTrigger = true;
                            break;
                        }
                    } else if (eval.ToUpper().StartsWith("ELSE") && level == 1) {
                        if (ElseTrigger) {
                            ErrInd = ind[Pointer];
                            ErrorMsg = "IF condition: duplicate ELSE statement";
                            return null;
                        } else {
                            ElseTrigger = true;
                        }
                    } else if (!ElseTrigger) {
                        IFMain.Add(eval);
                        IFMain_Index.Add(ind[Pointer]);
                    } else {
                        IfElse.Add(eval);
                        IfElse_Index.Add(ind[Pointer]);
                    }
                }

                Pointer += 1;
            }

            if (!EndIfTrigger) {
                ErrInd = ind[Pointer];
                ErrorMsg = "IF condition: EndIf statement not present";
                return null;
            }

            this_if.IF_MAIN = ProcessText(IFMain.ToArray(), IFMain_Index.ToArray(), ref ErrInd, ref ErrorMsg);
            if (!string.IsNullOrEmpty(ErrorMsg))
                return null;
            this_if.IF_ELSE = ProcessText(IfElse.ToArray(), IfElse_Index.ToArray(), ref ErrInd, ref ErrorMsg);
            if (!string.IsNullOrEmpty(ErrorMsg))
                return null;
            return this_if;
        }

        internal ScriptLineElement CreateForLoop(ref int Pointer, string[] lines, uint[] ind, ref uint ErrInd, ref string ErrorMsg) {
            var this_for = new ScriptLoop(MyParent); // Also loads NOT modifier
            this_for.INDEX = ind[Pointer];
            var success = this_for.Parse(lines[Pointer]);
            if (success.IsError) {
                ErrInd = ind[Pointer];
                ErrorMsg = "FOR LOOP statement is not valid";
                return null;
            }

            bool EndForTrigger = false;
            int level = 0;
            var LoopMain = new List<string>();
            var LoopMain_Index = new List<uint>();
            while (Pointer < lines.Length) {
                string eval = Utilities.RemoveComment(lines[Pointer].Replace(Constants.vbTab, " ")).Trim();
                if (!string.IsNullOrEmpty(eval)) {
                    if (eval.ToUpper().StartsWith("FOR ")) {
                        level += 1;
                        if (!(level == 1)) {
                            LoopMain.Add(eval);
                            LoopMain_Index.Add(ind[Pointer]);
                        }
                    } else if (eval.ToUpper().StartsWith("ENDFOR") || eval.ToUpper().StartsWith("END FOR")) {
                        level -= 1;
                        if (level == 0) {
                            EndForTrigger = true;
                            break;
                        }

                        LoopMain.Add(eval);
                        LoopMain_Index.Add(ind[Pointer]);
                    } else {
                        LoopMain.Add(eval);
                        LoopMain_Index.Add(ind[Pointer]);
                    }
                }

                Pointer += 1;
            }

            if (!EndForTrigger) {
                ErrInd = ind[Pointer];
                ErrorMsg = "FOR Loop: EndFor statement not present";
                return null;
            }

            var loopvar = new ScriptVariable(this_for.VAR_NAME, DataType.UInteger);
            MyParent.CurrentVars.SetVariable(loopvar);
            this_for.LOOP_MAIN = ProcessText(LoopMain.ToArray(), LoopMain_Index.ToArray(), ref ErrInd, ref ErrorMsg);
            if (!string.IsNullOrEmpty(ErrorMsg))
                return null;
            return this_for;
        }

        internal ScriptLineElement CreateGoto(ref int Pointer, string[] lines, uint[] ind, ref uint ErrInd, ref string ErrorMsg) {
            var this_goto = new ScriptGoto(MyParent);
            this_goto.INDEX = ind[Pointer];
            string input = lines[Pointer];
            if (input.ToUpper().StartsWith("GOTO "))
                input = input.Substring(5).Trim();
            if (string.IsNullOrEmpty(input)) {
                ErrInd = (uint)Pointer;
                ErrorMsg = "GOTO statement is missing target label";
                return null;
            }

            this_goto.TO_LABEL = input;
            return this_goto;
        }

        internal ScriptLineElement CreateExit(ref int Pointer, string[] lines, uint[] ind, ref uint ErrInd, ref string ErrorMsg) {
            var this_exit = new ScriptExit(MyParent);
            this_exit.INDEX = ind[Pointer];
            this_exit.MODE = ExitMode.Leave;
            string input = lines[Pointer];
            if (input.ToUpper() == "EXIT")
                return this_exit;
            if (input.ToUpper().StartsWith("EXIT "))
                input = input.Substring(5).Trim();
            switch (input.ToUpper() ?? "") {
                case "SCRIPT": {
                        this_exit.MODE = ExitMode.LeaveScript;
                        break;
                    }

                case "EVENT": {
                        this_exit.MODE = ExitMode.LeaveEvent;
                        break;
                    }

                default: {
                        this_exit.MODE = ExitMode.Leave;
                        break;
                    }
            }

            return this_exit;
        }

        internal ScriptLineElement CreateReturn(ref int Pointer, string[] lines, uint[] ind, ref uint ErrInd, ref string ErrorMsg) {
            var this_return = new ScriptReturn(MyParent);
            this_return.INDEX = ind[Pointer];
            string input = lines[Pointer];
            // Pointer += 1
            if (input.ToUpper().StartsWith("RETURN "))
                input = input.Substring(7).Trim();
            this_return.Parse(input);
            ErrorMsg = this_return.ERROR_MSG;
            return this_return;
        }

        internal ScriptLineElement CreateLabel(ref int Pointer, string[] lines, uint[] ind, ref uint ErrInd, ref string ErrorMsg) {
            var label_this = new ScriptLabel(MyParent);
            label_this.INDEX = ind[Pointer];
            string input = lines[Pointer];
            if (input.ToUpper().EndsWith(":"))
                input = input.Substring(0, input.Length - 1).Trim();
            if (string.IsNullOrEmpty(input)) {
                ErrInd = (uint)Pointer;
                ErrorMsg = "Label statement is missing target label";
                return null;
            }

            label_this.NAME = input;
            return label_this;
        }

        internal ScriptLineElement CreateEvent(ref int Pointer, string[] lines, uint[] ind, ref uint ErrInd, ref string ErrorMsg) {
            var this_ev = new ScriptEvent(MyParent);
            this_ev.INDEX = ind[Pointer];
            string input = lines[Pointer];
            Pointer += 1;
            if (input.ToUpper().StartsWith("CREATEEVENT"))
                input = input.Substring(11).Trim();
            string event_name = Tools.FeedParameter(ref input);
            if (string.IsNullOrEmpty(event_name)) {
                ErrInd = (uint)Pointer;
                ErrorMsg = "CreateEvent statement is missing event name";
                return null;
            }

            this_ev.EVENT_NAME = event_name;
            bool EndEventTrigger = false;
            var EventBody = new List<string>();
            var EventBody_Index = new List<uint>();
            while (Pointer < lines.Length) {
                string eval = Utilities.RemoveComment(lines[Pointer].Replace(Constants.vbTab, " ")).Trim();
                if (!string.IsNullOrEmpty(eval)) {
                    if (eval.ToUpper().StartsWith("CREATEEVENT")) {
                        ErrInd = (uint)Pointer;
                        ErrorMsg = "Error: CreateEvent statement within event";
                    } else if (eval.ToUpper().StartsWith("ENDEVENT") || eval.ToUpper().StartsWith("END EVENT")) {
                        EndEventTrigger = true;
                        break;
                    } else {
                        EventBody.Add(eval);
                        EventBody_Index.Add(ind[Pointer]);
                    }
                }

                Pointer += 1;
            }

            if (!EndEventTrigger) {
                ErrInd = (uint)Pointer;
                ErrorMsg = "CreateEvent: EndEvent statement not present";
                return null;
            }

            this_ev.Elements = ProcessText(EventBody.ToArray(), EventBody_Index.ToArray(), ref ErrInd, ref ErrorMsg);
            if (!string.IsNullOrEmpty(ErrorMsg))
                return null;
            return this_ev;
        }
    }

    internal enum ExitMode {
        KeepRunning,
        Leave,
        LeaveEvent,
        LeaveScript,
        GotoLabel
    }

    internal enum TargetOper {
        NONE, // We are not going to create a SV
        EQ, // =
        ADD, // +=
        SUB // -=
    }
    // what to do with two variables/functions
    internal enum OperandOper {
        NOTSPECIFIED,
        ADD, // Add (for integer), combine (for DATA or STRING)
        SUB,
        MULT,
        DIV,
        AND,
        OR,
        S_LEFT,
        S_RIGHT,
        IS,
        LESS_THAN,
        GRT_THAN
    }

    internal enum CmdPrm {
        UInteger,
        UInteger_Optional,
        Integer,
        Integer_Optional,
        String,
        String_Optional,
        Data,
        Data_Optional,
        Bool,
        Bool_Optional,
        Any,
        Any_Optional
    }

    internal enum ScriptElementDataType {
        Data, // Means the entry contains int,str,data,bool
        Operator, // Means this is a ADD/SUB/MULT/DIV etc.
        SubItems, // Means this contains a sub instance of entries
        Variable,
        Function,
        Event
    }

    internal enum DataType {
        NULL,
        Integer,
        UInteger,
        String,
        Data,
        Bool,
        FncError
    }

    internal enum ScriptFileElementType {
        IF_CONDITION,
        FOR_LOOP,
        LABEL,
        GOTO,
        EVENT,
        ELEMENT,
        EXIT,
        RETURN
    }

    internal class ScriptVariable {
        public string Name { get; private set; } = null;
        public DataTypeObject Data { get; private set; }

        public ScriptVariable(string new_name, DataType defined_type, object default_value = null) {
            Name = new_name;
            Data = new DataTypeObject(defined_type, default_value);
        }

        public ScriptVariable(string new_name, DataTypeObject data) {
            Name = new_name;
        }

        public object Value {
            get {
                return Data.Value;
            }

            set {
                Data.Value = value;
            }
        }

        public override string ToString() {
            return Name + " = " + Value.ToString();
        }
    }

    internal class DataTypeObject {
        public DataType VarType { get; private set; }

        private byte[] InternalData; // This holds the data for this variable

        public DataTypeObject(DataType defined_type, object default_value) {
            VarType = defined_type;
            Value = default_value;
        }

        public object Value {
            get {
                switch (VarType) {
                    case DataType.Integer: {
                            return (object)Utilities.Bytes.ToInt32(InternalData);
                        }

                    case DataType.UInteger: {
                            return (object)Utilities.Bytes.ToUInt32(InternalData);
                        }

                    case DataType.String: {
                            return Utilities.Bytes.ToChrString(InternalData);
                        }

                    case DataType.Bool: {
                            if (InternalData[0] == 1) {
                                return true;
                            } else if (InternalData[0] == 2) {
                                return false;
                            }

                            return false;
                        }

                    case DataType.Data: {
                            return InternalData;
                        }

                    case DataType.FncError: {
                            return Utilities.Bytes.ToChrString(InternalData);
                        }

                    default: {
                            return null;
                        }
                }
            }

            set {
                switch (VarType) {
                    case DataType.Integer: {
                            InternalData = Utilities.Bytes.FromInt32(Conversions.ToInteger(value));
                            break;
                        }

                    case DataType.UInteger: {
                            InternalData = Utilities.Bytes.FromUInt32(Conversions.ToUInteger(value));
                            break;
                        }

                    case DataType.String: {
                            InternalData = Utilities.Bytes.FromChrString(Conversions.ToString(value));
                            break;
                        }

                    case DataType.Bool: {
                            InternalData = new byte[1];
                            if (Conversions.ToBoolean(value)) {
                                InternalData[0] = 1;
                            } else {
                                InternalData[0] = 2;
                            }

                            break;
                        }

                    case DataType.Data: {
                            InternalData = (byte[])value;
                            break;
                        }

                    case DataType.FncError: {
                            InternalData = Utilities.Bytes.FromChrString(Conversions.ToString(value));
                            break;
                        }
                }
            }
        }
    }
    internal class ScriptVariableManager {
        private List<ScriptVariable> MyVariables = new List<ScriptVariable>();

        public ScriptVariableManager() {
        }

        public void Clear() {
            MyVariables.Clear();
        }

        internal bool IsVariable(string input) {
            string var_name = "";
            string argsub_fnc = null;
            string argind_fnc = null;
            string argarguments = null;
            Tools.ParseToFunctionAndSub(input, ref var_name, ref argsub_fnc, ref argind_fnc, ref argarguments);
            foreach (var item in MyVariables) {
                if (item.Name is object && !string.IsNullOrEmpty(item.Name)) {
                    if ((item.Name.ToUpper() ?? "") == (var_name.ToUpper() ?? ""))
                        return true;
                }
            }

            return false;
        }

        internal ScriptVariable GetVariable(string var_name) {
            string var_name_to_find = var_name.ToUpper();
            foreach (var item in MyVariables) {
                if (item.Name.ToUpper().Equals(var_name_to_find))
                    return item;
            }

            return null;
        }

        internal bool SetVariable(ScriptVariable input_var) {
            string var_name_to_find = input_var.Name.ToUpper();
            if (var_name_to_find is null || var_name_to_find.Equals(""))
                return false;
            for (int i = 0, loopTo = MyVariables.Count - 1; i <= loopTo; i++) {
                if (MyVariables[i].Name.ToUpper().Equals(var_name_to_find)) {
                    MyVariables[i] = input_var;
                    return true;
                }
            }

            MyVariables.Add(input_var);
            return true;
        }

        internal bool ClearVariable(string name) {
            for (int i = 0, loopTo = MyVariables.Count - 1; i <= loopTo; i++) {
                if (MyVariables[i].Name is object && !string.IsNullOrEmpty(MyVariables[i].Name)) {
                    if ((MyVariables[i].Name.ToUpper() ?? "") == (name.ToUpper() ?? "")) {
                        MyVariables.RemoveAt(i);
                        return true;
                    }
                }
            }

            return false;
        }

        internal object GetValue(string var_name) {
            var sv = GetVariable(var_name);
            return sv.Value;
        }

        internal string GetNewName() {
            bool Found = false;
            string new_name = "";
            int counter = 1;
            do {
                new_name = "$t" + counter;
                var sv = GetVariable(new_name);
                if (sv is null)
                    Found = true;
                counter += 1;
            }
            while (!Found);
            return new_name;
        }
        // This tells our pre-processor that a value is an expected variable
        internal void AddExpected(string name) {
            ClearVariable(name);
            MyVariables.Add(new ScriptVariable(name, DataType.NULL));
        }
    }

    internal abstract class ScriptLineElement {
        internal Processor MyParent;

        public uint INDEX { get; set; } // This is the line index of this element
        public ScriptFileElementType ElementType { get; set; } // [IF_CONDITION] [FOR_LOOP] [LABEL] [GOTO] [EVENT] [ELEMENT]

        public ScriptLineElement(Processor oParent) {
            MyParent = oParent;
        }
    }

    internal class ScriptElement : ScriptLineElement {
        internal TargetOper TARGET_OPERATION { get; set; } = TargetOper.NONE; // What to do with the compiled variable
        internal string TARGET_NAME { get; set; } = ""; // This is the name of the variable to create
        internal int TARGET_INDEX { get; set; } = -1; // For DATA arrays, this is the index within the array
        internal string TARGET_VAR { get; set; } = ""; // Instead of INDEX, a variable (int) can be used instead

        private ScriptElementOperand OPERLIST; // (Element)(+/-)(Element) etc.

        public ScriptElement(Processor oParent) : base(oParent) {
            ElementType = ScriptFileElementType.ELEMENT;
        }

        internal ParseResult Parse(string to_parse, bool parse_target) {
            to_parse = to_parse.Trim();
            if (parse_target) {
                var result = LoadTarget(ref to_parse);
                if (result.IsError)
                    return result;
            }

            OPERLIST = new ScriptElementOperand(MyParent);
            return OPERLIST.Parse(to_parse);
        }
        // This parses the initial string to check for a var assignment
        private ParseResult LoadTarget(ref string to_parse) {
            string str_out = "";
            for (int i = 0, loopTo = to_parse.Length - 1; i <= loopTo; i++) {
                if (to_parse.Length - i > 2 && to_parse.Substring(i, 2) == "==") { // This is a compare
                    return new ParseResult();
                } else if (to_parse.Length - i > 2 && to_parse.Substring(i, 2) == "+=") {
                    TARGET_OPERATION = TargetOper.ADD;
                    TARGET_NAME = str_out.Trim();
                    to_parse = to_parse.Substring(i + 2).Trim();
                    return new ParseResult();
                } else if (to_parse.Length - i > 2 && to_parse.Substring(i, 2) == "-=") {
                    TARGET_OPERATION = TargetOper.SUB;
                    TARGET_NAME = str_out.Trim();
                    to_parse = to_parse.Substring(i + 2).Trim();
                    return new ParseResult();
                } else if (to_parse.Substring(i, 1) == "=") {
                    TARGET_OPERATION = TargetOper.EQ;
                    string input = str_out.Trim();
                    string arg = "";
                    string argmain_fnc = TARGET_NAME;
                    string argsub_fnc = null;
                    string argind_fnc = null;
                    Tools.ParseToFunctionAndSub(input, ref argmain_fnc, ref argsub_fnc, ref argind_fnc, ref arg);
                    TARGET_NAME = argmain_fnc;
                    to_parse = to_parse.Substring(i + 1).Trim();
                    if (!string.IsNullOrEmpty(arg)) {
                        if (Information.IsNumeric(arg)) {
                            TARGET_INDEX = (int)Conversions.ToUInteger(arg); // Fixed INDEX
                        } else if (Utilities.IsDataType.HexString(arg)) {
                            TARGET_INDEX = (int)Utilities.HexToUInt(arg); // Fixed INDEX
                        } else if (MyParent.CurrentVars.IsVariable(arg) && MyParent.CurrentVars.GetVariable(arg).Data.VarType == DataType.UInteger) {
                            TARGET_INDEX = -1;
                            TARGET_VAR = arg;
                        } else {
                            return new ParseResult("Target index must be able to evaluate to an integer");
                        }
                    }

                    return new ParseResult();
                } else if (to_parse.Substring(i, 1) == ".") {
                    return new ParseResult();
                } else if (to_parse.Substring(i, 1) == "\"") {
                    return new ParseResult();
                } else if (to_parse.Substring(i, 1) == ">") {
                    return new ParseResult();
                } else if (to_parse.Substring(i, 1) == "<") {
                    return new ParseResult();
                } else if (to_parse.Substring(i, 1) == "+") {
                    return new ParseResult();
                } else if (to_parse.Substring(i, 1) == "-") {
                    return new ParseResult();
                } else if (to_parse.Substring(i, 1) == "/") {
                    return new ParseResult();
                } else if (to_parse.Substring(i, 1) == "*") {
                    return new ParseResult();
                } else if (to_parse.Substring(i, 1) == "&") {
                    return new ParseResult();
                } else if (to_parse.Substring(i, 1) == "|") {
                    return new ParseResult();
                } else {
                    str_out += to_parse.Substring(i, 1);
                }
            }

            return new ParseResult();
        }

        internal ScriptVariable Compile(ref ExitMode exit_task) {
            return OPERLIST.CompileToVariable(ref exit_task);
        }

        public override string ToString() {
            if (string.IsNullOrEmpty(TARGET_NAME)) {
                return OPERLIST.ToString();
            } else {
                switch (TARGET_OPERATION) {
                    case TargetOper.EQ: {
                            return TARGET_NAME + " = " + OPERLIST.ToString();
                        }

                    case TargetOper.ADD: {
                            return TARGET_NAME + " += " + OPERLIST.ToString();
                        }

                    case TargetOper.SUB: {
                            return TARGET_NAME + " -= " + OPERLIST.ToString();
                        }
                }
            }

            return "[ELEMENT]";
        }
    }

    internal class ScriptCondition : ScriptLineElement {
        public ScriptElement CONDITION { get; set; }
        public bool NOT_MODIFIER { get; set; } = false;

        public ScriptLineElement[] IF_MAIN; // Elements to execute if condition is true
        public ScriptLineElement[] IF_ELSE; // And if FALSE 

        public ScriptCondition(Processor oParent) : base(oParent) {
            ElementType = ScriptFileElementType.IF_CONDITION;
        }

        internal ParseResult Parse(string input) {
            try {
                if (input.ToUpper().StartsWith("IF "))
                    input = input.Substring(3).Trim();
                NOT_MODIFIER = false; // Indicates the not modifier is being used
                if (input.ToUpper().StartsWith("NOT")) {
                    NOT_MODIFIER = true;
                    input = input.Substring(3).Trim();
                }

                CONDITION = new ScriptElement(MyParent);
                return CONDITION.Parse(input, false);
            } catch {
                return new ParseResult("Exception in If Condition");
            }
        }

        public override string ToString() {
            return "IF CONDITION (" + CONDITION.ToString() + ")";
        }
    }

    internal class ScriptLoop : ScriptLineElement {
        internal string VAR_NAME { get; set; } // This is the name of the variable
        internal uint START_IND { get; set; } = 0U;
        internal uint END_IND { get; set; } = 0U;
        internal uint STEP_VAL { get; set; } = 1U;

        public ScriptLineElement[] LOOP_MAIN;
        private ScriptElementOperand LOOPSTART_OPER; // The argument for the first part (pre TO)
        private ScriptElementOperand LOOPSTOP_OPER; // Argument for the stop part (post TO)

        public ScriptLoop(Processor oParent) : base(oParent) {
            ElementType = ScriptFileElementType.FOR_LOOP;
        }

        internal ParseResult Parse(string next_part) {
            try {
                if (next_part.ToUpper().StartsWith("FOR "))
                    next_part = next_part.Substring(4).Trim();
                if (next_part.StartsWith("(") && next_part.EndsWith(")")) {
                    next_part = next_part.Substring(1, next_part.Length - 2);
                    VAR_NAME = Tools.FeedWord(ref next_part, new[] { "=" });
                    if (string.IsNullOrEmpty(VAR_NAME))
                        return new ParseResult("For Loop syntax is invalid");
                    next_part = next_part.Substring(1).Trim();
                    string first_part = Tools.FeedElement(ref next_part);
                    next_part = next_part.Trim();
                    if (string.IsNullOrEmpty(next_part))
                        return new ParseResult("For Loop syntax is invalid");
                    string to_part = Tools.FeedElement(ref next_part);
                    next_part = next_part.Trim();
                    if (string.IsNullOrEmpty(next_part))
                        return new ParseResult("For Loop syntax is invalid");
                    if (!(to_part.ToUpper() == "TO"))
                        return new ParseResult("For Loop syntax is invalid");
                    LOOPSTART_OPER = new ScriptElementOperand(MyParent);
                    LOOPSTOP_OPER = new ScriptElementOperand(MyParent);
                    var p1 = LOOPSTART_OPER.Parse(first_part);
                    var p2 = LOOPSTOP_OPER.Parse(next_part);
                    if (p1.IsError)
                        return p1;
                    if (p2.IsError)
                        return p2;
                    return new ParseResult(); // Should we return an error?
                } else {
                    return new ParseResult("For Loop syntax is invalid");
                }
            } catch {
                return new ParseResult("Exception in For Loop");
            }
        }
        // Compiles the FROM TO variables
        internal bool Evaluate() {
            try {
                ExitMode argexit_task = default;
                var sv1 = LOOPSTART_OPER.CompileToVariable(ref argexit_task);
                ExitMode argexit_task1 = default;
                var sv2 = LOOPSTOP_OPER.CompileToVariable(ref argexit_task1);
                if (sv1 is null)
                    return false;
                if (sv2 is null)
                    return false;
                if (!(sv1.Data.VarType == DataType.UInteger))
                    return false;
                if (!(sv2.Data.VarType == DataType.UInteger))
                    return false;
                START_IND = Conversions.ToUInteger(sv1.Value);
                END_IND = Conversions.ToUInteger(sv2.Value);
                return true;
            } catch {
                return false;
            }
        }

        public override string ToString() {
            return "FOR LOOP (" + VAR_NAME + " = " + START_IND + " to " + END_IND + ") STEP " + STEP_VAL;
        }
    }

    internal class ScriptLabel : ScriptLineElement {
        public ScriptLabel(Processor oParent) : base(oParent) {
            ElementType = ScriptFileElementType.LABEL;
        }

        internal string NAME { get; set; }

        public override string ToString() {
            return "LABEL: " + NAME;
        }
    }

    internal class ScriptGoto : ScriptLineElement {
        internal string TO_LABEL { get; set; }

        public ScriptGoto(Processor oParent) : base(oParent) {
            ElementType = ScriptFileElementType.GOTO;
        }

        public override string ToString() {
            return "GOTO: " + TO_LABEL;
        }
    }

    internal class ScriptExit : ScriptLineElement {
        internal ExitMode MODE { get; set; }

        public ScriptExit(Processor oParent) : base(oParent) {
            ElementType = ScriptFileElementType.EXIT;
        }

        public override string ToString() {
            switch (MODE) {
                case ExitMode.KeepRunning: {
                        return "KEEP-ALIVE";
                    }

                case ExitMode.Leave: {
                        return "EXIT (LEAVE)";
                    }

                case ExitMode.LeaveEvent: {
                        return "EXIT (EVENT)";
                    }

                case ExitMode.LeaveScript: {
                        return "EXIT (SCRIPT)";
                    }

                default: {
                        return "";
                    }
            }
        }
    }

    internal class ScriptReturn : ScriptLineElement {
        public bool HAS_ERROR {
            get {
                if (ERROR_MSG.Equals(""))
                    return false;
                else
                    return true;
            }
        }

        public string ERROR_MSG { get; set; } = "";

        private ScriptElementOperand OPERLIST;

        public ScriptReturn(Processor oParent) : base(oParent) {
            ElementType = ScriptFileElementType.RETURN;
        }

        internal ParseResult Parse(string to_parse) {
            to_parse = to_parse.Trim();
            OPERLIST = new ScriptElementOperand(MyParent);
            return OPERLIST.Parse(to_parse);
        }

        internal ScriptVariable Compile(ref ExitMode exit_task) {
            return OPERLIST.CompileToVariable(ref exit_task);
        }

        public override string ToString() {
            return "RETURN " + OPERLIST.ToString();
        }
    }

    internal class ScriptEvent : ScriptLineElement {
        public ScriptEvent(Processor oParent) : base(oParent) {
            ElementType = ScriptFileElementType.EVENT;
        }

        internal string EVENT_NAME { get; set; }

        internal ScriptLineElement[] Elements;

        public override string ToString() {
            return "SCRIPT EVENT: " + EVENT_NAME;
        }
    }

    internal static class Tools {
        internal static ScriptVariable CreateError(object error_msg) {
            return new ScriptVariable("ERROR", DataType.FncError, error_msg);
        }

        internal static string GetDataTypeString(DataType input) {
            switch (input) {
                case DataType.Integer: {
                        return "Integer";
                    }

                case DataType.UInteger: {
                        return "UInteger";
                    }

                case DataType.String: {
                        return "String";
                    }

                case DataType.Data: {
                        return "Data";
                    }

                case DataType.Bool: {
                        return "Bool";
                    }

                default: {
                        return "NotDefined";
                    }
            }
        }

        internal static bool IsDataArrayType(string input) {
            try {
                if (input.IndexOf(";") == -1)
                    return false;
                if (input.EndsWith(";"))
                    input = input.Substring(0, input.Length - 1);
                var t = input.Split(';');
                foreach (var item in t) {
                    if (!Utilities.IsDataType.Hex(item))
                        return false;
                    uint t2 = Utilities.HexToUInt(item);
                    if (t2 > 255L)
                        return false;
                }

                return true;
            } catch {
            }

            return false;
        }

        internal static byte[] DataArrayTypeToBytes(string input) {
            if (!IsDataArrayType(input))
                return null;
            try {
                if (input.EndsWith(";"))
                    input = input.Substring(0, input.Length - 1);
                var d = new List<byte>();
                var t = input.Split(';');
                foreach (var item in t) {
                    uint t2 = Utilities.HexToUInt(item);
                    d.Add((byte)((long)Utilities.HexToUInt(item) & 255L));
                }

                return d.ToArray();
            } catch {
            }

            return null;
        }

        internal static string FeedParameter(ref string input) {
            if (!input.StartsWith("("))
                return "";
            string strout = "";
            int counter = 0;
            int level = 0;
            bool is_in_string = false;
            for (int i = 0, loopTo = input.Length - 1; i <= loopTo; i++) {
                counter += 1;
                char c = Conversions.ToChar(Strings.Mid(input, i + 1, 1));
                strout += Conversions.ToString(c);
                if (is_in_string && Conversions.ToString(c) == "\"") {
                    is_in_string = false;
                } else if (Conversions.ToString(c) == "\"") {
                    is_in_string = true;
                } else {
                    if (Conversions.ToString(c) == "(")
                        level = level + 1;
                    if (Conversions.ToString(c) == ")")
                        level = level - 1;
                    if (Conversions.ToString(c) == ")" & level == 0)
                        break;
                }
            }

            input = Strings.Mid(input, counter + 1).TrimStart();
            return Strings.Mid(strout, 2, strout.Length - 2).Trim();
        }
        // This feeds the input string up to a char specified in the stop_char array
        internal static string FeedWord(ref string input, string[] stop_chars) {
            int first_index = input.Length;
            foreach (string c in stop_chars) {
                int i = input.IndexOf(c);
                if (i > -1)
                    first_index = Math.Min(first_index, i);
            }

            string output = input.Substring(0, first_index).Trim();
            input = input.Substring(first_index).Trim();
            return output;
        }

        internal static string FeedString(ref string objline) {
            if (!objline.StartsWith("\""))
                return "";
            int counter = 0;
            foreach (char c in objline) {
                if (c == '"' & !(counter == 0))
                    break;
                counter += 1;
            }

            string InsideParam = objline.Substring(1, counter - 1);
            objline = objline.Substring(counter + 1).TrimStart();
            return InsideParam;
        }
        // Returns the first word,function, etc. and mutates the input object
        internal static string FeedElement(ref string objline) {
            int PARAM_LEVEL = 0;
            bool IN_STRING = false;
            int x = 0;
            while (x != objline.Length) {
                char pull = objline[x];
                if (IN_STRING) {
                    if (pull == '"')
                        IN_STRING = false;
                } else if (pull == '"') {
                    IN_STRING = true;
                } else if (pull == '(') {
                    PARAM_LEVEL += 1;
                } else if (pull == ')') {
                    PARAM_LEVEL -= 1;
                    if (PARAM_LEVEL == -1)
                        return ""; // ERROR
                    if (PARAM_LEVEL == 0 & (x + 1 == objline.Length || !objline[x + 1].Equals('.'))) {
                        x += 1;
                        break;
                    }
                } else if (PARAM_LEVEL == 0) {
                    if (pull == '=')
                        break;
                    if (pull == '+')
                        break;
                    if (pull == '-') {
                        if (!(x == 0 && objline.Length > 1 && Information.IsNumeric(objline[x + 1]))) {
                            break;
                        }
                    }

                    if (pull == '*')
                        break;
                    if (pull == '/')
                        break;
                    if (pull == '<')
                        break;
                    if (pull == '>')
                        break;
                    if (pull == '&')
                        break;
                    if (pull == '|')
                        break;
                    if (pull == ' ')
                        break;
                }

                x += 1;
            }

            if (x == 0)
                return "";
            string NewElement = objline.Substring(0, x);
            objline = objline.Substring(x).TrimStart();
            return NewElement;
        }

        internal static bool FeedOperator(ref string text_input, ref OperandOper sel_operator) {
            if (text_input.StartsWith("+")) { // Valid for string, data, and int
                sel_operator = OperandOper.ADD;
                text_input = text_input.Substring(1).TrimStart();
            } else if (text_input.StartsWith("-")) {
                sel_operator = OperandOper.SUB;
                text_input = text_input.Substring(1).TrimStart();
            } else if (text_input.StartsWith("/")) {
                sel_operator = OperandOper.DIV;
                text_input = text_input.Substring(1).TrimStart();
            } else if (text_input.StartsWith("*")) {
                sel_operator = OperandOper.MULT;
                text_input = text_input.Substring(1).TrimStart();
            } else if (text_input.StartsWith("&")) {
                sel_operator = OperandOper.AND;
                text_input = text_input.Substring(1).TrimStart();
            } else if (text_input.StartsWith("|")) {
                sel_operator = OperandOper.OR;
                text_input = text_input.Substring(1).TrimStart();
            } else if (text_input.StartsWith("<<")) {
                sel_operator = OperandOper.S_LEFT;
                text_input = text_input.Substring(2).TrimStart();
            } else if (text_input.StartsWith(">>")) {
                sel_operator = OperandOper.S_RIGHT;
                text_input = text_input.Substring(2).TrimStart();
            } else if (text_input.StartsWith("==")) {
                sel_operator = OperandOper.IS;
                text_input = text_input.Substring(2).TrimStart();
            } else if (text_input.StartsWith("<")) {
                sel_operator = OperandOper.LESS_THAN;
                text_input = text_input.Substring(1).TrimStart();
            } else if (text_input.StartsWith(">")) {
                sel_operator = OperandOper.GRT_THAN;
                text_input = text_input.Substring(1).TrimStart();
            } else {
                return false;
            }

            return true;
        }
        // Compiles two variables, returns a string if there is an error
        internal static ScriptVariable CompileSVars(ScriptVariable var1, ScriptVariable var2, OperandOper oper) {
            try {
                if (oper == OperandOper.AND | oper == OperandOper.OR) {
                    if (!(var1.Data.VarType == DataType.Bool)) {
                        return CreateError("OR / AND bitwise operators only valid for Bool data types");
                    }
                }

                switch (oper) {
                    case OperandOper.ADD: {
                            switch (var1.Data.VarType) {
                                case DataType.Integer: {
                                        if (var2.Data.VarType == DataType.Integer) {
                                            return new ScriptVariable("RESULT", DataType.Integer, Conversions.ToInteger(var1.Value) + Conversions.ToInteger(var2.Value));
                                        } else if (var2.Data.VarType == DataType.UInteger) {
                                            return new ScriptVariable("RESULT", DataType.Integer, Conversions.ToInteger(var1.Value) + Conversions.ToUInteger(var2.Value));
                                        } else {
                                            return CreateError("Operand data type mismatch");
                                        }
                                    }

                                case DataType.UInteger: {
                                        if (var2.Data.VarType == DataType.UInteger) {
                                            return new ScriptVariable("RESULT", DataType.UInteger, Conversions.ToUInteger(var1.Value) + Conversions.ToUInteger(var2.Value));
                                        } else if (var2.Data.VarType == DataType.UInteger) {
                                            return new ScriptVariable("RESULT", DataType.UInteger, Conversions.ToUInteger(var1.Value) + Conversions.ToInteger(var2.Value));
                                        } else {
                                            return CreateError("Operand data type mismatch");
                                        }
                                    }

                                case DataType.String: {
                                        var new_result = new ScriptVariable("RESULT", DataType.String);
                                        if (var2.Data.VarType == DataType.UInteger) {
                                            new_result.Value = Conversions.ToString(var1.Value) + Conversions.ToUInteger(var2.Value).ToString();
                                        } else if (var2.Data.VarType == DataType.String) {
                                            new_result.Value = Conversions.ToString(var1.Value) + Conversions.ToString(var2.Value);
                                        } else {
                                            return CreateError("Operand data type mismatch");
                                        }
                                        return new_result;
                                    }

                                case DataType.Data: {
                                        var new_result = new ScriptVariable("RESULT", DataType.Data);
                                        if (!(var1.Data.VarType == var2.Data.VarType)) {
                                            return CreateError("Operand data type mismatch");
                                        }

                                        byte[] data1 = (byte[])var1.Value;
                                        byte[] data2 = (byte[])var2.Value;
                                        int new_size = data1.Length + data2.Length;
                                        var new_data = new byte[new_size + 1];
                                        Array.Copy(data1, 0, new_data, 0, data1.Length);
                                        Array.Copy(data2, 0, new_data, data1.Length, data2.Length);
                                        new_result.Value = new_data;
                                        return new_result;
                                    }

                                case DataType.Bool: {
                                        return CreateError("Add operand not valid for Bool data type");
                                    }
                            }

                            break;
                        }

                    case OperandOper.SUB: {
                            switch (var1.Data.VarType) {
                                case DataType.Integer: {
                                        if (var2.Data.VarType == DataType.Integer) {
                                            return new ScriptVariable("RESULT", DataType.Integer, Conversions.ToInteger(var1.Value) - Conversions.ToInteger(var2.Value));
                                        } else if (var2.Data.VarType == DataType.UInteger) {
                                            return new ScriptVariable("RESULT", DataType.Integer, Conversions.ToInteger(var1.Value) - Conversions.ToUInteger(var2.Value));
                                        } else {
                                            return CreateError("Operand data type mismatch");
                                        }
                                    }

                                case DataType.UInteger: {
                                        if (var2.Data.VarType == DataType.UInteger) {
                                            return new ScriptVariable("RESULT", DataType.UInteger, Conversions.ToUInteger(var1.Value) - Conversions.ToUInteger(var2.Value));
                                        } else if (var2.Data.VarType == DataType.UInteger) {
                                            return new ScriptVariable("RESULT", DataType.UInteger, Conversions.ToUInteger(var1.Value) - Conversions.ToInteger(var2.Value));
                                        } else {
                                            return CreateError("Operand data type mismatch");
                                        }
                                    }

                                default: {
                                        var new_result = new ScriptVariable("RESULT", DataType.UInteger);
                                        if (!(var1.Data.VarType == var2.Data.VarType))
                                            return CreateError("Operand data type mismatch");
                                        if (!(var1.Data.VarType == DataType.UInteger))
                                            return CreateError("Subtract operand only valid for Integer data type");
                                        new_result.Value = Conversions.ToUInteger(var1.Value) - Conversions.ToUInteger(var2.Value);
                                        return new_result;
                                    }
                            }
                        }

                    case OperandOper.DIV: {
                            if (!(var1.Data.VarType == var2.Data.VarType))
                                return CreateError("Operand data type mismatch");
                            if (var1.Data.VarType == DataType.Integer) {
                                var new_result = new ScriptVariable("RESULT", DataType.Integer);
                                new_result.Value = Conversions.ToInteger(var1.Value) / (double)Conversions.ToInteger(var2.Value);
                                return new_result;
                            } else if (var2.Data.VarType == DataType.UInteger) {
                                var new_result = new ScriptVariable("RESULT", DataType.UInteger);
                                new_result.Value = Conversions.ToUInteger(var1.Value) / (double)Conversions.ToUInteger(var2.Value);
                                return new_result;
                            } else {
                                return CreateError("Division operand only valid for Integer or UInteger data type");
                            }
                        }

                    case OperandOper.MULT: {
                            if (!(var1.Data.VarType == var2.Data.VarType))
                                return CreateError("Operand data type mismatch");
                            if (var1.Data.VarType == DataType.Integer) {
                                var new_result = new ScriptVariable("RESULT", DataType.Integer);
                                new_result.Value = Conversions.ToInteger(var1.Value) * Conversions.ToInteger(var2.Value);
                                return new_result;
                            } else if (var2.Data.VarType == DataType.UInteger) {
                                var new_result = new ScriptVariable("RESULT", DataType.UInteger);
                                new_result.Value = Conversions.ToUInteger(var1.Value) * Conversions.ToUInteger(var2.Value);
                                return new_result;
                            } else {
                                return CreateError("Mulitple operand only valid for Integer or UInteger data type");
                            }
                        }

                    case OperandOper.S_LEFT: {
                            var new_result = new ScriptVariable("RESULT", DataType.Integer);
                            if (!(var1.Data.VarType == var2.Data.VarType))
                                return CreateError("Operand data type mismatch");
                            if (!(var1.Data.VarType == DataType.Integer))
                                return CreateError("Shift-left operand only valid for Integer data type");
                            int shift_value = Conversions.ToInteger(var2.Value);
                            if (shift_value > 31)
                                return CreateError("Shift-left value is greater than 31-bits");
                            new_result.Value = Conversions.ToUInteger(var1.Value) << shift_value;
                            return new_result;
                        }

                    case OperandOper.S_RIGHT: {
                            var new_result = new ScriptVariable("RESULT", DataType.Integer);
                            if (!(var1.Data.VarType == var2.Data.VarType))
                                return CreateError("Operand data type mismatch");
                            if (!(var1.Data.VarType == DataType.Integer))
                                return CreateError("Shift-right operand only valid for Integer data type");
                            int shift_value = Conversions.ToInteger(var2.Value);
                            if (shift_value > 31)
                                return CreateError("Shift-right value is greater than 31-bits");
                            new_result.Value = Conversions.ToUInteger(var1.Value) >> shift_value;
                            return new_result;
                        }

                    case OperandOper.AND: { // We already checked to make sure these are BOOL
                            var new_result = new ScriptVariable("RESULT", DataType.Bool);
                            if (!(var1.Data.VarType == var2.Data.VarType))
                                return CreateError("Operand data type mismatch");
                            new_result.Value = Conversions.ToBoolean(var1.Value) & Conversions.ToBoolean(var2.Value);
                            return new_result;
                        }

                    case OperandOper.OR: { // We already checked to make sure these are BOOL
                            var new_result = new ScriptVariable("RESULT", DataType.Bool);
                            if (!(var1.Data.VarType == var2.Data.VarType))
                                return CreateError("Operand data type mismatch");
                            new_result.Value = Conversions.ToBoolean(Operators.OrObject(var1.Value, var2.Value));
                            return new_result;
                        }

                    case OperandOper.IS: { // Boolean compare operators
                            var new_result = new ScriptVariable("RESULT", DataType.Bool);
                            bool result = false;
                            if (var1 is object && var1.Data.VarType == DataType.NULL) {
                                if (var2 is null) {
                                    result = true;
                                } else if (var2.Value is null) {
                                    result = true;
                                }
                            } else if (var2 is object && var2.Data.VarType == DataType.NULL) {
                                if (var1 is null) {
                                    result = true;
                                } else if (var1.Value is null) {
                                    result = true;
                                }
                            } else {
                                if (!(var1.Data.VarType == var2.Data.VarType))
                                    return CreateError("Operand data type mismatch");
                                if (var1.Data.VarType == DataType.String) {
                                    string s1 = Conversions.ToString(var1.Value);
                                    string s2 = Conversions.ToString(var2.Value);
                                    if (s1.Length == s2.Length) {
                                        for (int i = 0, loopTo = s1.Length - 1; i <= loopTo; i++) {
                                            if (!((s1.Substring(i, 1) ?? "") == (s2.Substring(i, 1) ?? ""))) {
                                                result = false;
                                                break;
                                            }
                                        }
                                    }
                                } else if (var1.Data.VarType == DataType.Integer) {
                                    result = Conversions.ToInteger(var1.Value) == Conversions.ToInteger(var2.Value);
                                } else if (var1.Data.VarType == DataType.UInteger) {
                                    result = Conversions.ToUInteger(var1.Value) == Conversions.ToUInteger(var2.Value);
                                } else if (var1.Data.VarType == DataType.Data) {
                                    byte[] d1 = (byte[])var1.Value;
                                    byte[] d2 = (byte[])var2.Value;
                                    if (d1.Length == d2.Length) {
                                        result = true;
                                        for (int i = 0, loopTo1 = d1.Length - 1; i <= loopTo1; i++) {
                                            if (!(d1[i] == d2[i])) {
                                                result = false;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            new_result.Value = result;
                            return new_result;
                        }

                    case OperandOper.LESS_THAN: { // Boolean compare operators 
                            var new_result = new ScriptVariable("RESULT", DataType.Bool);
                            if (!(var1.Data.VarType == var2.Data.VarType))
                                return CreateError("Operand data type mismatch");
                            if (var1.Data.VarType == DataType.Integer) {
                                new_result.Value = Conversions.ToInteger(var1.Value) < Conversions.ToInteger(var2.Value);
                            } else if (var1.Data.VarType == DataType.UInteger) {
                                new_result.Value = Conversions.ToUInteger(var1.Value) < Conversions.ToUInteger(var2.Value);
                            } else {
                                return CreateError("Less-than compare operand only valid for Integer/UInteger data type");
                            }

                            return new_result;
                        }

                    case OperandOper.GRT_THAN: { // Boolean compare operators
                            var new_result = new ScriptVariable("RESULT", DataType.Bool);
                            if (!(var1.Data.VarType == var2.Data.VarType))
                                return CreateError("Operand data type mismatch");
                            if (var1.Data.VarType == DataType.Integer) {
                                new_result.Value = Conversions.ToInteger(var1.Value) > Conversions.ToInteger(var2.Value);
                            } else if (var1.Data.VarType == DataType.UInteger) {
                                new_result.Value = Conversions.ToUInteger(var1.Value) > Conversions.ToUInteger(var2.Value);
                            } else {
                                return CreateError("Greater-than compare operand only valid for Integer/UInteger data type");
                            }

                            return new_result;
                        }
                }
            } catch {
                return CreateError("Error compiling operands");
            }

            return null;
        }

        public static void ParseToFunctionAndSub(string to_parse, ref string main_fnc, ref string sub_fnc, ref string ind_fnc, ref string arguments) {
            try {
                ind_fnc = "0";
                main_fnc = FeedWord(ref to_parse, new[] { "(", "." });
                if (string.IsNullOrEmpty(to_parse)) { // element is only one item
                    sub_fnc = "";
                    arguments = "";
                } else if (to_parse.StartsWith("()")) {
                    to_parse = to_parse.Substring(2).Trim();
                    if (string.IsNullOrEmpty(to_parse) || !to_parse.StartsWith(".")) {
                        sub_fnc = "";
                        arguments = "";
                        return;
                    }
                    sub_fnc = FeedWord(ref to_parse, new[] { "(" });
                    arguments = FeedParameter(ref to_parse);
                    return;
                } else if (to_parse.StartsWith("(")) {
                    string section = FeedParameter(ref to_parse);
                    if (to_parse.StartsWith(".")) {
                        ind_fnc = section;
                        to_parse = to_parse.Substring(1).Trim();
                        sub_fnc = FeedWord(ref to_parse, new[] { "(" });
                        if (!string.IsNullOrEmpty(to_parse) && to_parse.StartsWith("(")) {
                            arguments = FeedParameter(ref to_parse);
                        }
                    } else {
                        sub_fnc = "";
                        arguments = section;
                    }
                } else if (to_parse.StartsWith(".")) {
                    to_parse = to_parse.Substring(1).Trim();
                    sub_fnc = FeedWord(ref to_parse, new[] { "(" });
                    if (!string.IsNullOrEmpty(to_parse) && to_parse.StartsWith("(")) {
                        arguments = FeedParameter(ref to_parse);
                    }
                }
            } catch {
                main_fnc = "";
                sub_fnc = "";
            }
        }
    }
}