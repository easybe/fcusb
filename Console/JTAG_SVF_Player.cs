// COPYRIGHT EMBEDDEDCOMPUTERS.NET 2020 - ALL RIGHTS RESERVED
// CONTACT EMAIL: support@embeddedcomputers.net
// ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
// INFO: This class implements the SVF / XSVF file format developed by Texas Instroments
// 12/29/18: Added Lattice LOOP/LOOPEND commands

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace JTAG
{
    public class SVF_Player
    {
        public bool ExitStateMachine { get; set; } = true;
        public bool IgnoreErrors { get; set; } = false; // If set to true, this player will not stop executing on a readback error

        public event ProgressEventHandler Progress;

        public delegate void ProgressEventHandler(int percent);

        public event SetFrequencyEventHandler SetFrequency;

        public delegate void SetFrequencyEventHandler(uint Hz);

        public event SetTRSTEventHandler SetTRST;

        public delegate void SetTRSTEventHandler(bool Enabled);

        public event WriteconsoleEventHandler Writeconsole;

        public delegate void WriteconsoleEventHandler(string msg);

        public event ResetTapEventHandler ResetTap;

        public delegate void ResetTapEventHandler();

        public event GotoStateEventHandler GotoState;

        public delegate void GotoStateEventHandler(JTAG_MACHINE_STATE dst_state);

        public event ShiftIREventHandler ShiftIR;

        public delegate void ShiftIREventHandler(byte[] tdi_bits, ref byte[] tdo_bits, short bit_count, bool exit_tms);

        public event ShiftDREventHandler ShiftDR;

        public delegate void ShiftDREventHandler(byte[] tdi_bits, ref byte[] tdo_bits, ushort bit_count, bool exit_tms);

        public event ToggleClockEventHandler ToggleClock;

        public delegate bool ToggleClockEventHandler(uint clk_tck, bool exit_tms = false);

        public SVF_Player()
        {
        }

        public JTAG_MACHINE_STATE ENDIR { get; set; }
        public JTAG_MACHINE_STATE ENDDR { get; set; }
        public svf_param IR_TAIL { get; set; }
        public svf_param IR_HEAD { get; set; }
        public svf_param DR_TAIL { get; set; }
        public svf_param DR_HEAD { get; set; }
        public int SIR_LAST_LEN { get; set; } // Number of bits the last mask length was
        public int SDR_LAST_LEN { get; set; } // Number of bits the last mask length was

        public byte[] SIR_LAST_MASK;
        public byte[] SDR_LAST_MASK;

        private void Setup()
        {
            ENDIR = JTAG_MACHINE_STATE.RunTestIdle;
            ENDDR = JTAG_MACHINE_STATE.RunTestIdle;
            IR_TAIL = new svf_param();
            IR_HEAD = new svf_param();
            DR_TAIL = new svf_param();
            DR_HEAD = new svf_param();
            SIR_LAST_MASK = null;
            SIR_LAST_LEN = -1;
            SDR_LAST_MASK = null;
            SDR_LAST_LEN = -1;
        }

        public bool RunFile_XSVF(byte[] user_file)
        {
            Setup();
            var xsvf_file = ConvertDataToProperFormat(user_file);
            svf_data TDO_MASK = default;
            uint TDO_REPEAT = 16U; // Number of times to retry
            uint XRUNTEST = 0U;
            uint XSDRSIZE = 0U;
            int line_counter = 0;
            GotoState?.Invoke(JTAG.JTAG_MACHINE_STATE.RunTestIdle);
            foreach (var line in xsvf_file)
            {
                line_counter += 1;
                Progress?.Invoke((int)(line_counter / (double)xsvf_file.Length * 100d));
                switch (line.instruction)
                {
                    case xsvf_instruction.XTDOMASK:
                        {
                            TDO_MASK = line.value_data;
                            break;
                        }

                    case xsvf_instruction.XREPEAT:
                        {
                            TDO_REPEAT = (uint)line.value_uint;
                            break;
                        }

                    case xsvf_instruction.XRUNTEST:
                        {
                            XRUNTEST = (uint)line.value_uint;
                            break;
                        }

                    case xsvf_instruction.XSIR:
                        {
                            byte[] argtdo_bits = null;
                            ShiftIR?.Invoke(line.value_data.data, ref argtdo_bits, (Int16)line.value_data.bits, true);
                            if (!(XRUNTEST == 0L))
                            {
                                GotoState?.Invoke(JTAG.JTAG_MACHINE_STATE.RunTestIdle);
                                DoXRunTest(XRUNTEST); // wait for the last specified XRUNTEST time. 
                            }
                            else
                            {
                                GotoState?.Invoke(ENDIR);
                            }  // Otherwise, go to the last specified XENDIR state.

                            break;
                        }

                    case xsvf_instruction.XSDR:
                        {
                            uint Counter = TDO_REPEAT;
                            bool Result = false;
                            do
                            {
                                byte[] TDO = null;
                                ShiftDR?.Invoke(line.value_data.data, ref TDO, (UInt16)line.value_data.bits, true);
                                Result = CompareResult(TDO, line.value_expected.data, TDO_MASK.data); // compare TDO with line.value_expected (use TDOMask from last XTDOMASK)
                                if (!Result)
                                {
                                    PrintCompareError(line_counter, "XSDR", TDO, TDO_MASK.data, line.value_expected.data);
                                    if (Counter == 0L)
                                    {
                                        if (IgnoreErrors)
                                            break;
                                        else
                                            return false;
                                    }
                                }

                                if (Counter > 0L)
                                    Counter = (uint)(Counter - 1L);
                            }
                            while (!Result);
                            if (!(XRUNTEST == 0L))
                            {
                                GotoState?.Invoke(JTAG.JTAG_MACHINE_STATE.RunTestIdle);
                                DoXRunTest(XRUNTEST); // wait for the last specified XRUNTEST time. 
                            }
                            else
                            {
                                GotoState?.Invoke(ENDDR);
                            }  // Otherwise, go to the last specified XENDDR state.

                            break;
                        }

                    case xsvf_instruction.XSDRSIZE:
                        {
                            XSDRSIZE = (uint)line.value_uint;    // Specifies the length of all XSDR/XSDRTDO records that follow.
                            break;
                        }

                    case xsvf_instruction.XSDRTDO:
                        {
                            uint Counter = TDO_REPEAT;
                            bool Result = false;
                            do
                            {
                                if (Counter > 0L)
                                    Counter = (uint)(Counter - 1L);
                                byte[] TDO = null;
                                ShiftDR?.Invoke(line.value_data.data, ref TDO, (UInt16)line.value_data.bits, true);
                                Result = CompareResult(TDO, line.value_expected.data, TDO_MASK.data); // compare TDO with line.value_expected (use TDOMask from last XTDOMASK)
                                if (!Result)
                                {
                                    PrintCompareError(line_counter, "XSDRTDO", TDO, TDO_MASK.data, line.value_expected.data);
                                    if (Counter == 0L)
                                    {
                                        if (IgnoreErrors)
                                            break;
                                        else
                                            return false;
                                    }
                                }
                            }
                            while (!Result);
                            if (!(XRUNTEST == 0L))
                            {
                                GotoState?.Invoke(JTAG.JTAG_MACHINE_STATE.RunTestIdle);
                                DoXRunTest(XRUNTEST); // wait for the last specified XRUNTEST time. 
                            }
                            else
                            {
                                GotoState?.Invoke(ENDDR);
                            }  // Otherwise, go to the last specified XENDDR state.

                            break;
                        }

                    case xsvf_instruction.XSDRB:
                        {
                            byte[] argtdo_bits1 = null;
                            ShiftDR?.Invoke(line.value_data.data, ref argtdo_bits1, (UInt16)line.value_data.bits, false);// Stay in DR
                            break;
                        }

                    case xsvf_instruction.XSDRC:
                        {
                            byte[] argtdo_bits2 = null;
                            ShiftDR?.Invoke(line.value_data.data, ref argtdo_bits2, (UInt16)line.value_data.bits, false); // Stay in DR
                            break;
                        }

                    case xsvf_instruction.XSDRE:
                        {
                            byte[] argtdo_bits3 = null;
                            ShiftDR?.Invoke(line.value_data.data, ref argtdo_bits3, (UInt16)line.value_data.bits, false);
                            GotoState?.Invoke(ENDDR);
                            break;
                        }

                    case xsvf_instruction.XSDRTDOB:
                        {
                            uint Counter = TDO_REPEAT;
                            bool Result = false;
                            do
                            {
                                if (Counter > 0L)
                                    Counter = (uint)(Counter - 1L);
                                byte[] TDO = null;
                                ShiftDR?.Invoke(line.value_data.data, ref TDO, (UInt16)line.value_data.bits, false);
                                Result = CompareResult(TDO, line.value_expected.data, TDO_MASK.data); // compare TDO with line.value_expected (use TDOMask from last XTDOMASK)
                                if (!Result)
                                {
                                    PrintCompareError(line_counter, "XSDRTDOB", TDO, TDO_MASK.data, line.value_expected.data);
                                    if (Counter == 0L)
                                    {
                                        if (IgnoreErrors)
                                            break;
                                        else
                                            return false;
                                    }
                                }

                                if (Counter > 0L)
                                    Counter = (uint)(Counter - 1L);
                            }
                            while (!Result);
                            break;
                        }

                    case xsvf_instruction.XSDRTDOC:
                        {
                            uint Counter = TDO_REPEAT;
                            bool Result = false;
                            do
                            {
                                byte[] TDO = null;
                                ShiftDR?.Invoke(line.value_data.data, ref TDO, (UInt16)line.value_data.bits, false);
                                Result = CompareResult(TDO, line.value_expected.data, TDO_MASK.data); // compare TDO with line.value_expected (use TDOMask from last XTDOMASK)
                                if (IgnoreErrors)
                                    break;
                                if (!Result)
                                {
                                    PrintCompareError(line_counter, "XSDRTDOC", TDO, TDO_MASK.data, line.value_expected.data);
                                    if (Counter == 0L)
                                    {
                                        if (IgnoreErrors)
                                            break;
                                        else
                                            return false;
                                    }
                                }
                            }
                            while (!Result);
                            break;
                        }

                    case xsvf_instruction.XSDRTDOE:
                        {
                            uint Counter = TDO_REPEAT;
                            bool Result = false;
                            do
                            {
                                if (Counter > 0L)
                                    Counter = (uint)(Counter - 1L);
                                byte[] TDO = null;
                                ShiftDR?.Invoke(line.value_data.data, ref TDO, (UInt16)line.value_data.bits, false);
                                Result = CompareResult(TDO, line.value_expected.data, TDO_MASK.data);
                                if (!Result)
                                {
                                    PrintCompareError(line_counter, "XSDRTDOE", TDO, TDO_MASK.data, line.value_expected.data);
                                    if (Counter == 0L)
                                    {
                                        if (IgnoreErrors)
                                            break;
                                        else
                                            return false;
                                    }
                                }
                            }
                            while (!Result);
                            GotoState?.Invoke(ENDDR);
                            break;
                        }

                    case xsvf_instruction.XSETSDRMASKS: // Obsolete
                        {
                            break;
                        }

                    case xsvf_instruction.XSDRINC: // Obsolete
                        {
                            break;
                        }

                    case xsvf_instruction.XCOMPLETE:
                        {
                            break;
                        }

                    case xsvf_instruction.XSTATE:
                        {
                            if (line.state == JTAG.JTAG_MACHINE_STATE.TestLogicReset)
                            {
                                ResetTap?.Invoke();
                            }
                            else
                            {
                                GotoState?.Invoke(line.state);
                            }

                            break;
                        }

                    case xsvf_instruction.XENDIR:
                        {
                            ENDIR = line.state;
                            break;
                        }

                    case xsvf_instruction.XENDDR:
                        {
                            ENDDR = line.state;
                            break;
                        }

                    case xsvf_instruction.XSIR2: // Same as XSIR (since we should all support 255 bit lengths or more)
                        {
                            byte[] argtdo_bits4 = null;
                            ShiftIR?.Invoke(line.value_data.data, ref argtdo_bits4, (Int16)line.value_data.bits, true);
                            if (!(XRUNTEST == 0L))
                            {
                                GotoState?.Invoke(JTAG.JTAG_MACHINE_STATE.RunTestIdle);
                                DoXRunTest((uint)line.value_uint); // wait for the last specified XRUNTEST time. 
                            }
                            else
                            {
                                GotoState?.Invoke(ENDIR);
                            }  // Otherwise, go to the last specified XENDIR state.

                            break;
                        }

                    case xsvf_instruction.XCOMMENT: // No need to display this
                        {
                            break;
                        }

                    case xsvf_instruction.XWAIT:
                        {
                            GotoState?.Invoke(line.state);
                            double Sleep = line.value_uint / 1000d;
                            System.Threading.Thread.Sleep((int)Sleep);
                            GotoState?.Invoke(line.state_end);
                            break;
                        }

                    default:
                        {
                            break;
                        }
                }
            }

            if (ExitStateMachine)
                GotoState?.Invoke(JTAG.JTAG_MACHINE_STATE.TestLogicReset);
            return true;
        }

        public bool RunFile_SVF(string[] user_file)
        {
            Setup();
            ResetTap?.Invoke();
            string[] svf_file = null;
            int[] svf_index = null;
            ConvertFileToProperFormat(user_file, ref svf_file, ref svf_index);
            GotoState?.Invoke(JTAG.JTAG_MACHINE_STATE.RunTestIdle);
            int LOOP_COUNTER = 0;
            var LOOP_CACHE = new List<string>();
            try
            {
                for (int x = 0, loopTo = svf_file.Count() - 1; x <= loopTo; x++)
                {
                    string line = svf_file[x];
                    if (x % 10 == 0)
                    {
                        Progress?.Invoke((int)((x + 1) / (double)svf_file.Length * 100d));
                    }

                    if (LOOP_COUNTER == 0)
                    {
                        if (line.ToUpper().StartsWith("LOOP ")) // Lattice's Extended SVF command
                        {
                            string loop_count_str = line.Substring(5).Trim();
                            LOOP_COUNTER = Conversions.ToInteger(loop_count_str);
                            LOOP_CACHE.Clear();
                        }
                        else if (!RunFile_Execute(line, svf_index[x], false))
                            return false;
                    }
                    else if (line.ToUpper().StartsWith("ENDLOOP"))
                    {
                        string end_loop_extra = line.Substring(7).Trim();
                        for (int i = 1, loopTo1 = LOOP_COUNTER; i <= loopTo1; i++)
                        {
                            var Loop_commands = LOOP_CACHE.ToArray();
                            bool result = true;
                            for (int sub_i = 0, loopTo2 = Loop_commands.Length - 1; sub_i <= loopTo2; sub_i++)
                                result = result & RunFile_Execute(Loop_commands[sub_i], svf_index[x] - Loop_commands.Length + sub_i, true);
                            if (result)
                                break;
                        }

                        LOOP_COUNTER = 0;
                    }
                    else // We are collecting for LOOP
                    {
                        LOOP_CACHE.Add(line);
                    }
                }

                if (ExitStateMachine)
                    GotoState?.Invoke(JTAG.JTAG_MACHINE_STATE.TestLogicReset);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool RunFile_Execute(string line, int line_index, bool lattice_loop)
        {
            if (line.ToUpper().StartsWith("SIR "))
            {
                var line_svf = new svf_param(line);
                byte[] TDO = null;
                if (IR_HEAD.LEN > 0)
                {
                    byte[] argtdo_bits = null;
                    ShiftIR?.Invoke(IR_HEAD.TDI, ref argtdo_bits, (Int16)IR_HEAD.LEN, false);
                }

                if (IR_TAIL.LEN > 0)
                {
                    ShiftIR?.Invoke(line_svf.TDI, ref TDO, (Int16)line_svf.LEN, false);
                    byte[] argtdo_bits1 = null;
                    ShiftIR?.Invoke(IR_TAIL.TDI, ref argtdo_bits1, (Int16)IR_TAIL.LEN, true);
                }
                else
                {
                    ShiftIR?.Invoke(line_svf.TDI, ref TDO, (Int16)line_svf.LEN, true);
                }

                var MASK_TO_COMPARE = line_svf.MASK;
                if (MASK_TO_COMPARE is null)
                {
                    if (line_svf.LEN == SIR_LAST_LEN && SIR_LAST_MASK is object)
                    {
                        MASK_TO_COMPARE = SIR_LAST_MASK;
                    }
                    else
                    {
                        SIR_LAST_LEN = -1;
                    }
                }
                else
                {
                    SIR_LAST_MASK = line_svf.MASK;
                    SIR_LAST_LEN = line_svf.LEN;
                }

                bool Result = CompareResult(TDO, line_svf.TDO, MASK_TO_COMPARE);
                if (!Result && !lattice_loop)
                {
                    PrintCompareError(line_index, "SIR", TDO, line_svf.MASK, line_svf.TDO);
                }

                GotoState?.Invoke(ENDIR);
                if (!Result && !IgnoreErrors)
                    return false;
            }
            else if (line.ToUpper().StartsWith("SDR "))
            {
                var line_svf = new svf_param(line);
                byte[] TDO = null;
                if (DR_HEAD.LEN > 0)
                {
                    byte[] argtdo_bits2 = null;
                    ShiftDR?.Invoke(DR_HEAD.TDI, ref argtdo_bits2, (UInt16)DR_HEAD.LEN, false);
                }

                if (DR_TAIL.LEN > 0)
                {
                    ShiftDR?.Invoke(line_svf.TDI, ref TDO, (UInt16)line_svf.LEN, false);
                    byte[] argtdo_bits3 = null;
                    ShiftDR?.Invoke(DR_TAIL.TDI, ref argtdo_bits3, (UInt16)DR_TAIL.LEN, true);
                }
                else
                {
                    ShiftDR?.Invoke(line_svf.TDI, ref TDO, (UInt16)line_svf.LEN, true);
                }

                var MASK_TO_COMPARE = line_svf.MASK;
                if (MASK_TO_COMPARE is null)
                {
                    if (line_svf.LEN == SDR_LAST_LEN && SDR_LAST_MASK is object)
                    {
                        MASK_TO_COMPARE = SDR_LAST_MASK;
                    }
                    else
                    {
                        SDR_LAST_LEN = -1;
                    }
                }
                else
                {
                    SDR_LAST_MASK = line_svf.MASK;
                    SDR_LAST_LEN = line_svf.LEN;
                }

                bool Result = CompareResult(TDO, line_svf.TDO, MASK_TO_COMPARE);
                if (!Result && !lattice_loop)
                {
                    PrintCompareError(line_index, "SDR", TDO, line_svf.MASK, line_svf.TDO);
                }

                GotoState?.Invoke(ENDDR);
                if (!Result && !IgnoreErrors)
                    return false;
            }
            else if (line.ToUpper().StartsWith("ENDIR "))
            {
                ENDIR = GetStateFromInput(Strings.Mid(line, 7).Trim());
            }
            else if (line.ToUpper().StartsWith("ENDDR "))
            {
                ENDDR = GetStateFromInput(Strings.Mid(line, 7).Trim());
            }
            else if (line.ToUpper().StartsWith("TRST ")) // Disable Test Reset line
            {
                string s = Strings.Mid(line, 6).Trim().ToUpper();
                bool EnableTrst = false;
                if (s.Equals("ON") || s.Equals("YES") || s.Equals("TRUE"))
                    EnableTrst = true;
                SetTRST?.Invoke(EnableTrst);
            }
            else if (line.ToUpper().StartsWith("FREQUENCY ")) // Sets the max freq of the device
            {
                try
                {
                    string s = Strings.Mid(line, 11).Trim();
                    if (s.ToUpper().EndsWith("HZ"))
                        s = Strings.Mid(s, 1, s.Length - 2).Trim();
                    uint FREQ32 = (uint)decimal.Parse(s, System.Globalization.NumberStyles.Float);
                    SetFrequency?.Invoke(FREQ32);
                }
                catch
                {
                    SetFrequency?.Invoke(1000000U);
                }
            }
            else if (line.ToUpper().StartsWith("RUNTEST "))
            {
                DoRuntest(Strings.Mid(line, 9).Trim());
            }
            else if (line.ToUpper().StartsWith("STATE "))
            {
                string Desired_State = Strings.Mid(line, 7).Trim().ToUpper(); // Possibly a list?
                GotoState?.Invoke(GetStateFromInput(Desired_State));
            }
            else if (line.ToUpper().StartsWith("TIR "))
            {
                IR_TAIL.LoadParams(line);
            }
            else if (line.ToUpper().StartsWith("HIR "))
            {
                IR_HEAD.LoadParams(line);
            }
            else if (line.ToUpper().StartsWith("TDR "))
            {
                DR_TAIL.LoadParams(line);
            }
            else if (line.ToUpper().StartsWith("HDR "))
            {
                DR_HEAD.LoadParams(line);
            }
            else
            {
                Writeconsole?.Invoke("Unknown SVF command at line " + line_index + " : " + line);
                return false;
            }

            return true;
        }

        private bool CompareResult(byte[] TDO, byte[] Expected, byte[] MASK)
        {
            try
            {
                if (MASK is object && Expected is object)
                {
                    if (TDO is null)
                        return false;
                    for (int i = 0, loopTo = TDO.Length - 1; i <= loopTo; i++)
                    {
                        byte masked_tdo = (byte)(TDO[i] & MASK[i]);
                        byte masked_exp = (byte)(Expected[i] & MASK[i]);
                        if (!(masked_tdo == masked_exp))
                            return false;
                    }
                }
                else if (Expected is object) // No MASK, use ALL CARE bits
                {
                    if (TDO is null)
                        return false;
                    for (int i = 0, loopTo1 = TDO.Length - 1; i <= loopTo1; i++)
                    {
                        if (!(TDO[i] == Expected[i]))
                            return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void PrintCompareError(int LineIndex, string command, byte[] TDO, byte[] TDO_MASK, byte[] EXPECTED)
        {
            Writeconsole?.Invoke("Failed sending " + command + " command (line index: " + LineIndex.ToString() + ")");
            Writeconsole?.Invoke("TDO: 0x" + Utilities.Bytes.ToHexString(TDO));
            Writeconsole?.Invoke("Expected: 0x" + Utilities.Bytes.ToHexString(EXPECTED));
            if (TDO_MASK is object)
            {
                Writeconsole?.Invoke("Mask: 0x" + Utilities.Bytes.ToHexString(TDO_MASK));
            }
        }

        private void DoXRunTest(uint wait_amount)
        {
            int s = (int)(wait_amount / 1000d);
            if (s < 30)
                s = 30;
            System.Threading.Thread.Sleep(s);
        }

        private bool IsValidRunState(string input)
        {
            switch (input.Trim().ToUpper() ?? "")
            {
                case "IRPAUSE":
                    {
                        break;
                    }

                case "DRPAUSE":
                    {
                        break;
                    }

                case "RESET":
                    {
                        break;
                    }

                case "IDLE":
                    {
                        break;
                    }

                default:
                    {
                        return false;
                    }
            }

            return true;
        }

        private void DoRuntest(string line)
        {
            try
            {
                var start_state = JTAG.JTAG_MACHINE_STATE.RunTestIdle; // Default
                var Params = line.Split(' ');
                if (Params is null)
                    return;
                int Counter = 0;
                if (IsValidRunState(Params[Counter]))
                {
                    start_state = GetStateFromInput(Params[Counter]);
                    Counter += 1;
                }

                GotoState?.Invoke(start_state);
                if (!Information.IsNumeric(Params[Counter]))
                    return;
                decimal wait_time = decimal.Parse(Params[Counter], System.Globalization.NumberStyles.Float);
                switch (Params[Counter + 1].Trim().ToUpper() ?? "")
                {
                    case "TCK": // Toggle test-clock
                        {
                            ToggleClock?.Invoke((uint)wait_time);
                            break;
                        }

                    case "SCK": // Toggle system-clock
                        {
                            System.Threading.Thread.Sleep((int)wait_time);
                            break;
                        }

                    case "SEC":
                        {
                            int sleep_int = (int)(wait_time * 1000m);
                            if (sleep_int < 1)
                                sleep_int = 20;
                            System.Threading.Thread.Sleep(sleep_int);
                            break;
                        }

                    default:
                        {
                            return;
                        }
                }

                Counter += 2;
                if (Counter == Params.Length)
                    return; // The rest are optional
                if (Params[Counter + 1].Trim().ToUpper() == "SEC")
                {
                    decimal min_time = decimal.Parse(Params[Counter], System.Globalization.NumberStyles.Float);
                    int sleep_int = (int)(min_time * 1000m);
                    if (sleep_int < 1)
                        sleep_int = 20;
                    System.Threading.Thread.Sleep(sleep_int);
                    Counter += 2;
                }

                if (Counter == Params.Length)
                    return; // The rest are optional
                if (Params[Counter].Trim().ToUpper() == "MAXIMUM")
                {
                    decimal max_time = decimal.Parse(Params[Counter + 1], System.Globalization.NumberStyles.Float);
                    Counter += 3; // THIRD ARG MUST BE SEC
                }

                if (Counter == Params.Length)
                    return; // The rest are optional
                if (Params[Counter].Trim().ToUpper() == "ENDSTATE" && IsValidRunState(Params[Counter + 1]))
                {
                    var end_state = GetStateFromInput(Params[Counter]);
                    GotoState?.Invoke(end_state);
                }
            }
            catch
            {
                System.Threading.Thread.Sleep(50);
            }
        }

        private JTAG_MACHINE_STATE GetStateFromInput(string input)
        {
            input = Utilities.RemoveComment(input, "!");
            input = Utilities.RemoveComment(input, "//");
            if (input.EndsWith(";"))
                input = Strings.Mid(input, 1, input.Length - 1).Trim();
            switch (input.ToUpper() ?? "")
            {
                case "IRPAUSE":
                    {
                        return JTAG.JTAG_MACHINE_STATE.Pause_IR;
                    }

                case "DRPAUSE":
                    {
                        return JTAG.JTAG_MACHINE_STATE.Pause_DR;
                    }

                case "RESET":
                    {
                        return JTAG.JTAG_MACHINE_STATE.TestLogicReset;
                    }

                case "IDLE":
                    {
                        return JTAG.JTAG_MACHINE_STATE.RunTestIdle;
                    }

                default:
                    {
                        return JTAG.JTAG_MACHINE_STATE.RunTestIdle;
                    }
            }
        }

        private void ConvertFileToProperFormat(string[] input, ref string[] output, ref int[] line_numbers)
        {
            var FormatedListOut = new List<string>();
            var LineNumberList = new List<int>();
            int line_counter = 1;
            foreach (var line in input)
            {
                var line2 = line.Replace(Constants.vbTab, " ").Trim();
                line2 = Utilities.RemoveComment(line2, "!");
                line2 = Utilities.RemoveComment(line2, "//");
                if (!line.Equals("")) {
                    FormatedListOut.Add(line2);
                    LineNumberList.Add(line_counter);
                }

                line_counter += 1;
            }

            var FormatedFileTwo = new List<string>();
            var LineNumberListTwo = new List<int>();
            string WorkInProgress = "";
            line_counter = -1;
            int index = 0;
            foreach (var line in FormatedListOut)
            {
                WorkInProgress += line.ToString().TrimStart();
                if (line_counter == -1)
                    line_counter = LineNumberList[index];
                if (WorkInProgress.EndsWith(";"))
                {
                    WorkInProgress = Strings.Mid(WorkInProgress, 1, WorkInProgress.Length - 1).TrimEnd();
                    FormatedFileTwo.Add(WorkInProgress);
                    LineNumberListTwo.Add(line_counter);
                    WorkInProgress = "";
                    line_counter = -1;
                }
                else
                {
                    WorkInProgress += " ";
                }

                index += 1;
            }

            output = FormatedFileTwo.ToArray();
            line_numbers = LineNumberListTwo.ToArray();
        }

        private xsvf_param[] ConvertDataToProperFormat(byte[] data)
        {
            int pointer = 0;
            var x = new List<xsvf_param>();
            uint XSDRSIZE = 8U; // number of bits
            while (pointer != data.Length)
            {
                var n = new xsvf_param(data[pointer]);
                switch (n.instruction)
                {
                    case xsvf_instruction.XTDOMASK:
                        {
                            Load_TDI(ref data, ref pointer, ref n, (int)XSDRSIZE);
                            break;
                        }

                    case xsvf_instruction.XREPEAT:
                        {
                            n.value_uint = data[pointer + 1];
                            pointer += 2;
                            break;
                        }

                    case xsvf_instruction.XRUNTEST:
                        {
                            n.value_uint = Load_Uint32_Value(ref data, ref pointer);
                            break;
                        }

                    case xsvf_instruction.XSIR:
                        {
                            int num_bytes = (int)Math.Ceiling(data[pointer + 1] / 8d);
                            var new_Data = new byte[num_bytes];
                            Array.Copy(data, pointer + 2, new_Data, 0, num_bytes);
                            n.value_data = new svf_data() { data = new_Data, bits = data[pointer + 1] };
                            pointer += num_bytes + 2;
                            break;
                        }

                    case xsvf_instruction.XSDR:
                        {
                            Load_TDI(ref data, ref pointer, ref n, (int)XSDRSIZE);
                            break;
                        }

                    case xsvf_instruction.XSDRSIZE:
                        {
                            XSDRSIZE = Load_Uint32_Value(ref data, ref pointer);
                            n.value_uint = XSDRSIZE;
                            break;
                        }

                    case xsvf_instruction.XSDRTDO:
                        {
                            int num_bytes = (int)Math.Ceiling(XSDRSIZE / 8d);
                            var data1 = new byte[num_bytes];
                            var data2 = new byte[num_bytes];
                            Array.Copy(data, pointer + 1, data1, 0, num_bytes);
                            Array.Copy(data, pointer + 1 + num_bytes, data2, 0, num_bytes);
                            n.value_data = new svf_data() { data = data1, bits = (int)XSDRSIZE };
                            n.value_expected = new svf_data() { data = data2, bits = (int)XSDRSIZE };
                            pointer += num_bytes + num_bytes + 1;
                            break;
                        }

                    case xsvf_instruction.XSDRB:
                        {
                            Load_TDI(ref data, ref pointer, ref n, (int)XSDRSIZE);
                            break;
                        }

                    case xsvf_instruction.XSDRC:
                        {
                            Load_TDI(ref data, ref pointer, ref n, (int)XSDRSIZE);
                            break;
                        }

                    case xsvf_instruction.XSDRE:
                        {
                            Load_TDI(ref data, ref pointer, ref n, (int)XSDRSIZE);
                            break;
                        }

                    case xsvf_instruction.XSDRTDOB:
                        {
                            Load_TDI_Expected(ref data, ref pointer, ref n, (int)XSDRSIZE);
                            break;
                        }

                    case xsvf_instruction.XSDRTDOC:
                        {
                            Load_TDI_Expected(ref data, ref pointer, ref n, (int)XSDRSIZE);
                            break;
                        }

                    case xsvf_instruction.XSDRTDOE:
                        {
                            Load_TDI_Expected(ref data, ref pointer, ref n, (int)XSDRSIZE);
                            break;
                        }

                    case xsvf_instruction.XSETSDRMASKS: // Obsolete
                        {
                            break;
                        }

                    case xsvf_instruction.XSDRINC: // Obsolete
                        {
                            break;
                        }

                    case xsvf_instruction.XCOMPLETE:
                        {
                            return x.ToArray();
                        }

                    case xsvf_instruction.XSTATE:
                        {
                            n.value_uint = data[pointer + 1];
                            n.state = GetStateFromInput(data[pointer + 1].ToString());
                            pointer += 2;
                            break;
                        }

                    case xsvf_instruction.XENDIR:
                        {
                            n.value_uint = data[pointer + 1];
                            switch (n.value_uint)
                            {
                                case 0UL:
                                    {
                                        n.state = JTAG.JTAG_MACHINE_STATE.RunTestIdle;
                                        break;
                                    }

                                case 1UL:
                                    {
                                        n.state = JTAG.JTAG_MACHINE_STATE.Pause_IR;
                                        break;
                                    }
                            }

                            pointer += 2;
                            break;
                        }

                    case xsvf_instruction.XENDDR:
                        {
                            n.value_uint = data[pointer + 1];
                            switch (n.value_uint)
                            {
                                case 0UL:
                                    {
                                        n.state = JTAG.JTAG_MACHINE_STATE.RunTestIdle;
                                        break;
                                    }

                                case 1UL:
                                    {
                                        n.state = JTAG.JTAG_MACHINE_STATE.Pause_DR;
                                        break;
                                    }
                            }

                            pointer += 2;
                            break;
                        }

                    case xsvf_instruction.XSIR2: // Same as XSIR (since we should all support 255 bit lengths or more)
                        {
                            int num_bytes = (int)Math.Ceiling(data[pointer + 1] / 8d);
                            var new_Data = new byte[num_bytes];
                            Array.Copy(data, pointer + 2, new_Data, 0, num_bytes);
                            n.value_data = new svf_data() { data = new_Data, bits = (int)XSDRSIZE };
                            pointer += num_bytes + 2;
                            break;
                        }

                    case xsvf_instruction.XCOMMENT:
                        {
                            do
                            {
                                pointer += 1;
                                if (pointer == data.Length)
                                    break;
                            }
                            while (data[pointer] != 0);
                            pointer += 1;
                            break;
                        }

                    case xsvf_instruction.XWAIT:
                        {
                            n.state = GetStateFromInput(data[pointer + 1].ToString());
                            n.state_end = GetStateFromInput(data[pointer + 2].ToString());
                            n.value_uint = (uint)data[pointer + 3] << 24;
                            n.value_uint += (uint)data[pointer + 4] << 16;
                            n.value_uint += (uint)data[pointer + 5] << 8;
                            n.value_uint += data[pointer + 6];
                            pointer += 7;
                            break;
                        }
                }

                x.Add(n);
            }

            return x.ToArray();
        }

        private void Load_TDI(ref byte[] data, ref int pointer, ref xsvf_param line, int XSDRSIZE)
        {
            int num_bytes = (int)Math.Ceiling(XSDRSIZE / 8d);
            var new_Data = new byte[num_bytes];
            Array.Copy(data, pointer + 1, new_Data, 0, num_bytes);
            line.value_data = new svf_data() { data = new_Data, bits = XSDRSIZE };
            pointer += num_bytes + 1;
        }

        private void Load_TDI_Expected(ref byte[] data, ref int pointer, ref xsvf_param line, int XSDRSIZE)
        {
            int num_bytes = (int)(XSDRSIZE / 8d); // Possible problem
            var data1 = new byte[num_bytes];
            var data2 = new byte[num_bytes];
            Array.Copy(data, pointer + 1, data1, 0, num_bytes);
            Array.Copy(data, pointer + 5, data2, 0, num_bytes);
            line.value_data = new svf_data() { data = data1, bits = XSDRSIZE };
            line.value_expected = new svf_data() { data = data2, bits = XSDRSIZE };
            pointer += 9;
        }

        private uint Load_Uint32_Value(ref byte[] data, ref int pointer)
        {
            uint ret;
            ret = (uint)data[pointer + 1] << 24;
            ret += (uint)data[pointer + 2] << 16;
            ret += (uint)data[pointer + 3] << 8;
            ret += data[pointer + 4];
            pointer += 5;
            return ret;
        }

        private byte[] GetBytes_FromUint(uint input, int MinBits)
        {
            var current = new byte[4];
            current[0] = (byte)((input & 0xFF000000L) >> 24);
            current[1] = (byte)((input & 0xFF0000L) >> 16);
            current[2] = (byte)((input & 0xFF00L) >> 8);
            current[3] = (byte)(input & 0xFFL);
            int MaxSize = (int)Math.Ceiling(MinBits / 8d);
            var @out = new byte[MaxSize];
            for (int i = 0, loopTo = MaxSize - 1; i <= loopTo; i++)
                @out[@out.Length - (1 + i)] = current[current.Length - (1 + i)];
            return @out;
        }
    }

    public struct svf_data
    {
        public byte[] data;
        public int bits;
    }

    public class svf_param
    {
        public int LEN = 0;
        public byte[] TDI;
        public byte[] SMASK;
        public byte[] TDO;
        public byte[] MASK;

        public svf_param()
        {
        }

        public svf_param(string input)
        {
            LoadParams(input);
        }

        public void LoadParams(string input)
        {
            input = Strings.Mid(input, Strings.InStr(input, " ") + 1); // We remove the first part (TIR, etc)
            if (!input.Contains(" "))
            {
                LEN = Conversions.ToInteger(input);
            }
            else
            {
                LEN = Conversions.ToInteger(Strings.Mid(input, 1, Strings.InStr(input, " ") - 1));
                input = Strings.Mid(input, Strings.InStr(input, " ") + 1);
                while (!input.Equals(""))
                {
                    if (input.ToUpper().StartsWith("TDI"))
                    {
                        input = Strings.Mid(input, 4).Trim();
                        TDI = GetBytes_Param(ref input);
                    }
                    else if (input.ToUpper().StartsWith("TDO"))
                    {
                        input = Strings.Mid(input, 4).Trim();
                        TDO = GetBytes_Param(ref input);
                    }
                    else if (input.ToUpper().StartsWith("SMASK"))
                    {
                        input = Strings.Mid(input, 6).Trim();
                        SMASK = GetBytes_Param(ref input);
                    }
                    else if (input.ToUpper().StartsWith("MASK"))
                    {
                        input = Strings.Mid(input, 5).Trim();
                        MASK = GetBytes_Param(ref input);
                    }
                }
            }
        }

        private byte[] GetBytes_Param(ref string line)
        {
            int x1 = Strings.InStr(line, "(");
            int x2 = Strings.InStr(line, ")");
            string HexStr = line.Substring(x1, x2 - 2).Replace(" ", "");
            line = Strings.Mid(line, x2 + 1).Trim();
            var data = Utilities.Bytes.FromHexString(HexStr);
            return data;
        }
    }

    public class xsvf_param
    {
        public xsvf_instruction instruction;
        public JTAG_MACHINE_STATE state;
        public JTAG_MACHINE_STATE state_end;
        public ulong value_uint;
        public svf_data value_data;
        public svf_data value_expected;

        public xsvf_param(byte code)
        {
            instruction = (xsvf_instruction)code;
        }

        public override string ToString()
        {
            return instruction.ToString();
        }
    }

    public enum xsvf_instruction
    {
        XCOMPLETE = 0x0,
        XTDOMASK = 0x1,
        XSIR = 0x2,
        XSDR = 0x3,
        XRUNTEST = 0x4,
        XREPEAT = 0x7,
        XSDRSIZE = 0x8,
        XSDRTDO = 0x9,
        XSETSDRMASKS = 0xA,
        XSDRINC = 0xB,
        XSDRB = 0xC,
        XSDRC = 0xD,
        XSDRE = 0xE,
        XSDRTDOB = 0xF,
        XSDRTDOC = 0x10,
        XSDRTDOE = 0x11,
        XSTATE = 0x12,
        XENDIR = 0x13,
        XENDDR = 0x14,
        XSIR2 = 0x15,
        XCOMMENT = 0x16,
        XWAIT = 0x17
    }

}