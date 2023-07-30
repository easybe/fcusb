// COPYRIGHT EMBEDDEDCOMPUTERS.NET 2020 - ALL RIGHTS RESERVED
// THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
// CONTACT EMAIL: support@embeddedcomputers.net
// ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
// ACKNOWLEDGEMENT: USB driver functionality provided by LibUsbDotNet (sourceforge.net/projects/libusbdotnet)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using CFI;
using FlashMemory;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace JTAG {
    public class JTAG_IF {
        public JTAG_IF(USB.FCUSB_DEVICE parent_if) {
            FCUSB = parent_if;
            Setup_SVF_Player();
            CFI_IF = new FLASH_INTERFACE();      
            BSDL_Init();
        }

        public USB.FCUSB_DEVICE FCUSB;
        public List<JTAG_DEVICE> Devices = new List<JTAG_DEVICE>();

        public bool Chain_IsValid { get; set; } = false; // Indicates if the chain has all BSDL loaded
        public byte Chain_BitLength { get; set; } // Number of total bits in the TDI-TDO through IR REG
        public int Chain_SelectedIndex { get; set; } // Selected device in the chain
        public byte IR_LENGTH { get; set; } // Number of bits of the IR register for the selected device
        public byte IR_LEADING { get; set; } // Number of bits after the IR register
        public byte IR_TRAILING { get; set; } // Number of bits before the IR register
        public byte DR_LEADING { get; set; } // Number of bits after the DR register
        public byte DR_TRAILING { get; set; } // Number of bits before the DR register
        public JTAG_SPEED TCK_SPEED { get; set; } = JTAG_SPEED._10MHZ;
        // Connects to the target device
        public bool Init()
        {
            try
            {
                Chain_BitLength = 0;
                Chain_SelectedIndex = 0;
                Chain_IsValid = false;
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_INIT, null, (uint)TCK_SPEED);
                JTAG_PrintClockSpeed();
                Thread.Sleep(200); // We need to wait
                if (!TAP_Detect())
                    return false;
                ValidateAndPrintChain();
                return true;
            }
            catch
            {
            }
            return false;
        }

#region "EPC2 Support"

        public delegate void EPC2_ProgressCallback(int i);

        public byte[] EPC2_ReadBinary(EPC2_ProgressCallback progress_update = null)
        {
            Reset_StateMachine();
            if (!EPC2_Check_JedecID())
            {
                return null;
            }

            byte[] argtdo_bits = null;
            this.JSP_ShiftIR(new byte[] { 0, 0x44 }, ref argtdo_bits, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            JSP_ToggleClock(10000U);
            if (!EPC2_Check_SiliconID())
            {
                return null;
            }

            byte[] argtdo_bits1 = null;
            this.JSP_ShiftIR(new byte[] { 0x1, 0xA }, ref argtdo_bits1, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            JSP_ToggleClock(50U);
            byte[] argtdo_bits2 = null;
            this.JSP_ShiftIR(new byte[] { 0x1, 0x22 }, ref argtdo_bits2, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            JSP_ToggleClock(200U);
            var tdi_high_64 = new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 };
            var epc2_data = new byte[211960];
            int loops = (int)(epc2_data.Length / 8d);
            for (int i = 0, loopTo = loops - 1; i <= loopTo; i++)
            {
                var tdo_out = new byte[8]; // 64 bits / 8 bytes at a time
                JSP_ShiftDR(tdi_high_64, ref tdo_out, 64, true);
                JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
                Array.Reverse(tdo_out);
                Array.Copy(tdo_out, 0, epc2_data, i * 8, 8);
                if (progress_update is object)
                {
                    progress_update.DynamicInvoke((object)(int)((i + 1) / (double)loops * 100d));
                }
            }

            byte[] argtdo_bits3 = null;
            this.JSP_ShiftIR(new byte[] { 0x0, 0x3E }, ref argtdo_bits3, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            JSP_ToggleClock(200U);
            bool success = EPC2_GetStatus();
            return epc2_data;
        }

        private bool EPC2_Check_JedecID()
        {
            var id_code = new byte[4];
            byte[] argtdo_bits = null;
            this.JSP_ShiftIR(new byte[] { 0, 0x59 }, ref argtdo_bits, -1, true);
            this.JSP_ShiftDR(new byte[] { 255, 255, 255, 255 }, ref id_code, (ushort)32, true);
            if (!(id_code[0] == 1 & id_code[1] == 0 & id_code[2] == 0x20 & id_code[3] == 0xDD)) // Check ID CODE
            {
                MainApp.PrintConsole("JTAG EPC2 error: device not detected");
                return false;
            }

            return true;
        }

        private bool EPC2_Check_SiliconID()
        {
            var id_code = new byte[4];
            byte[] argtdo_bits = null;
            this.JSP_ShiftIR(new byte[] { 0, 0x42 }, ref argtdo_bits, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            JSP_ToggleClock(50U);
            this.JSP_ShiftDR(new byte[] { 255, 255, 255, 255 }, ref id_code, (ushort)32, true);
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            if (!(id_code[0] == 0x41 & id_code[1] == 0x39 & id_code[2] == 0x38)) // Check ID CODE
            {
                MainApp.PrintConsole("JTAG EPC2 error: silicon ID failed");
                return false;
            }

            return true;
        }

        public bool EPC2_Erase()
        {
            Reset_StateMachine();
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            byte[] argtdo_bits = null;
            this.JSP_ShiftIR(new byte[] { 0, 0x44 }, ref argtdo_bits, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            JSP_ToggleClock(10000U);
            if (!EPC2_Check_SiliconID())
            {
                MainApp.PrintConsole("EPC2 Error: silicon ID check failed");
                return false;
            }

            MainApp.PrintConsole("Performing erase opreation on EPC2 device");
            byte[] argtdo_bits1 = null;
            this.JSP_ShiftIR(new byte[] { 0x1, 0x92 }, ref argtdo_bits1, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            JSP_ToggleClock(440000000U);
            return EPC2_GetStatus();
        }

        private bool EPC2_GetStatus()
        {
            var status = new byte[1];
            byte[] argtdo_bits = null;
            this.JSP_ShiftIR(new byte[] { 0x0, 0x3E }, ref argtdo_bits, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_ToggleClock(200U);
            this.JSP_ShiftDR(new byte[] { 7 }, ref status, (ushort)3, true);
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            return status[0] == 0;
        }

        public bool EPC2_WriteBinary(byte[] rbf, byte[] ctr_reg, EPC2_ProgressCallback progress_update = null)
        {
            MainApp.PrintConsole("Programming EPC2 device with raw binary file (RBF)");
            if (rbf.Length > 211960)
            {
                MainApp.PrintConsole("JTAG EPC2 error: raw binary file is too large to fit into memory");
                return default;
            }

            byte[] epc2_data;
            int extra = 8 - rbf.Length % 8;
            if (extra > 0)
            {
                epc2_data = new byte[(rbf.Length + extra)];
                Array.Copy(rbf, 0, epc2_data, 0, rbf.Length);
                for (int i = 0, loopTo = extra - 1; i <= loopTo; i++)
                    epc2_data[rbf.Length + i] = 255;
            }
            else
            {
                epc2_data = new byte[rbf.Length];
                Array.Copy(rbf, 0, epc2_data, 0, rbf.Length);
            }

            Array.Resize(ref ctr_reg, 8);
            ctr_reg[7] = 0x20;
            ctr_reg[6] = 0xBD;
            ctr_reg[5] = 0xCA;
            ctr_reg[4] = 0xFF; // or &HBF
            Reset_StateMachine();
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            byte[] argtdo_bits = null;
            this.JSP_ShiftIR(new byte[] { 0, 0x44 }, ref argtdo_bits, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            JSP_ToggleClock(10000U);
            if (!EPC2_Check_SiliconID())
            {
                MainApp.PrintConsole("EPC2 Error: silicon ID check failed");
                return false;
            }
            // ERASE
            MainApp.PrintConsole("Performing erase opreation on EPC2 device");
            byte[] argtdo_bits1 = null;
            this.JSP_ShiftIR(new byte[] { 0x1, 0x92 }, ref argtdo_bits1, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            JSP_ToggleClock(440000000U);
            if (!EPC2_GetStatus())
            {
                MainApp.PrintConsole("EPC2 Error: unable to erase device");
                return false;
            }
            // PROGRAM
            MainApp.PrintConsole("Programming EPC2 device");
            byte[] argtdo_bits2 = null;
            this.JSP_ShiftIR(new byte[] { 0x0, 0x6 }, ref argtdo_bits2, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_ToggleClock(50U);
            byte[] argtdo_bits3 = null;
            JSP_ShiftDR((byte[])ctr_reg.Clone(), ref argtdo_bits3, 64, true);
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            byte[] argtdo_bits4 = null;
            this.JSP_ShiftIR(new byte[] { 0x1, 0xA }, ref argtdo_bits4, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_ToggleClock(50U);
            byte[] argtdo_bits5 = null;
            this.JSP_ShiftIR(new byte[] { 0x1, 0x96 }, ref argtdo_bits5, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_ToggleClock(9000U);
            int loops = (int)(epc2_data.Length / 8d);
            for (int i = 0, loopTo1 = loops - 1; i <= loopTo1; i++)
            {
                var tdi_in = new byte[8];
                Array.Copy(epc2_data, i * 8, tdi_in, 0, 8);
                Array.Reverse(tdi_in);
                byte[] argtdo_bits6 = null;
                JSP_ShiftDR(tdi_in, ref argtdo_bits6, 64, true);
                JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
                JSP_ToggleClock(9000U);
                if (progress_update is object)
                {
                    int progress = (int)((i + 1) / (double)loops * 100d);
                    progress_update.DynamicInvoke((object)progress);
                }
            }

            bool was_sucessful = EPC2_GetStatus();
            if (!was_sucessful)
            {
                MainApp.PrintConsole("EPC2 Error: Programming failed");
                return false;
            }
            // PROGRAMMING DONE BIT
            byte[] argtdo_bits7 = null;
            this.JSP_ShiftIR(new byte[] { 0x0, 0x6 }, ref argtdo_bits7, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_ToggleClock(50U);
            ctr_reg[4] = (byte)(ctr_reg[4] & 0xBF); // Set BIT 7 to LOW
            byte[] argtdo_bits8 = null;
            JSP_ShiftDR(ctr_reg, ref argtdo_bits8, 64, true);
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            byte[] argtdo_bits9 = null;
            this.JSP_ShiftIR(new byte[] { 0x1, 0xA }, ref argtdo_bits9, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_ToggleClock(50U);
            byte[] argtdo_bits10 = null;
            this.JSP_ShiftIR(new byte[] { 0x1, 0x96 }, ref argtdo_bits10, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_ToggleClock(9000U);
            was_sucessful = EPC2_GetStatus();
            byte[] argtdo_bits11 = null;
            this.JSP_ShiftIR(new byte[] { 0x0, 0x4A }, ref argtdo_bits11, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_ToggleClock(10000U);
            byte[] argtdo_bits12 = null;
            this.JSP_ShiftIR(new byte[] { 0x3, 0xFF }, ref argtdo_bits12, -1, true);
            JSP_GotoState(JTAG_MACHINE_STATE.Pause_IR);
            JSP_ToggleClock(10000U);
            JSP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            MainApp.PrintConsole("EPC2 device programming successfully completed");
            return true;
        }

        #endregion

#region "ARM DAP"

        // IR=0xA
        private void ARM_DP_Decode(ulong data_in, ref uint data_out, ref ARM_DP_REG dp_reg, ref ARM_RnW r_w)
        {
            r_w = (ARM_RnW)((long)data_in & 1L);
            byte b = (byte)((long)(data_in >> 1) & 3L);
            dp_reg = (ARM_DP_REG)(b << 2);
            data_out = (uint)(data_in >> 3);
            // data_out = REVERSE32(data_out)
        }
        // IR=0xB
        private void ARM_AP_Decode(ulong data_in, ref uint data_out, ref ARM_AP_REG ap_reg, ref ARM_RnW r_w)
        {
            r_w = (ARM_RnW)((long)data_in & 1L);
            byte b = (byte)((long)(data_in >> 1) & 3L);
            ap_reg = (ARM_AP_REG)(b << 2);
            data_out = (uint)(data_in >> 3);
            // data_out = REVERSE32(data_out)
        }
        // Accesses CTRL/STAT, SELECT and RDBUFF
        private uint ARM_DPACC(uint reg32, ARM_DP_REG dp_reg, ARM_RnW read_write, bool goto_state = true)
        {
            var tdo = new byte[4];
            if (goto_state)
            {
                TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR);
                byte[] argtdo_bits = null;
                this.ShiftTDI((uint)Devices[0].BSDL.IR_LEN, new byte[] { Devices[0].BSDL.ARM_DPACC }, ref argtdo_bits, true);
            }

            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR);
            byte b5 = (byte)((reg32 & 0x1FL) << 3 | (long)(((int)dp_reg & 0xF) >> 1) | (long)read_write);
            reg32 = reg32 >> 5;
            byte b4 = (byte)(reg32 & 0xFFL);
            reg32 = reg32 >> 8;
            byte b3 = (byte)(reg32 & 0xFFL);
            reg32 = reg32 >> 8;
            byte b2 = (byte)(reg32 & 0xFFL);
            reg32 = reg32 >> 8;
            byte b1 = (byte)(reg32 & 0x7L);
            ShiftTDI(35U, new[] { b5, b4, b3, b2, b1 }, ref tdo, true);
            TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            byte status = (byte)(tdo[0] & 3);
            uint reg32_out = (uint)((tdo[1] & 7) << 5 | tdo[0] >> 3);
            reg32_out = reg32_out | (uint)((tdo[2] & 7) << 5 | tdo[1] >> 3) << 8;
            reg32_out = reg32_out | (uint)((tdo[3] & 7) << 5 | tdo[2] >> 3) << 16;
            reg32_out = reg32_out | (uint)((tdo[4] & 7) << 5 | tdo[3] >> 3) << 24;
            return reg32_out;
        }
        // Accesses port registers (AHB-AP)
        private uint ARM_APACC(uint reg32, ARM_AP_REG ap_reg, ARM_RnW read_write, bool goto_state = true)
        {
            if (!((byte)ap_reg >> 4 == ARM_REG_ADDR >> 4))
            {
                ARM_DPACC((uint)((int)ap_reg & 0xF0), ARM_DP_REG.ADDR, ARM_RnW.WR);
            }

            ARM_REG_ADDR = (byte)ap_reg;
            var tdo = new byte[4];
            if (goto_state)
            {
                TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR);
                byte[] argtdo_bits = null;
                this.ShiftTDI((uint)Devices[0].BSDL.IR_LEN, new byte[] { Devices[0].BSDL.ARM_APACC }, ref argtdo_bits, true);
            }

            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR);
            byte b5 = (byte)((reg32 & 0x1FL) << 3 | (long)(((int)ap_reg & 0xF) >> 1) | (long)read_write);
            reg32 = reg32 >> 5;
            byte b4 = (byte)(reg32 & 0xFFL);
            reg32 = reg32 >> 8;
            byte b3 = (byte)(reg32 & 0xFFL);
            reg32 = reg32 >> 8;
            byte b2 = (byte)(reg32 & 0xFFL);
            reg32 = reg32 >> 8;
            byte b1 = (byte)(reg32 & 0x7L);
            ShiftTDI(35U, new[] { b5, b4, b3, b2, b1 }, ref tdo, true);
            TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            byte status = (byte)(tdo[0] & 3);
            uint reg32_out = (uint)((tdo[1] & 7) << 5 | tdo[0] >> 3);
            reg32_out = reg32_out | (uint)((tdo[2] & 7) << 5 | tdo[1] >> 3) << 8;
            reg32_out = reg32_out | (uint)((tdo[3] & 7) << 5 | tdo[2] >> 3) << 16;
            reg32_out = reg32_out | (uint)((tdo[4] & 7) << 5 | tdo[3] >> 3) << 24;
            return reg32_out;
        }

        private byte ARM_REG_ADDR;

        private void ARM_MDAP_INIT()
        {
            ARM_DPACC(0x0U, ARM_DP_REG.ADDR, ARM_RnW.WR);
            ARM_REG_ADDR = 0; // A[7:2] A1 and A0 are ignored
        }

        public enum ARM_RnW : byte
        {
            WR = 0, // Write operation
            RD = 1 // Read operation
        }

        private enum ARM_DP_REG : byte
        {
            None = 0, // 0b00xx
            CTRL_STAT = 0x4, // 0b01xx
            ADDR = 0x8, // 0b10xx
            RDBUFF = 0xC // 0b11xx
        }

        private enum ARM_AP_REG : byte
        {
            CSW = 0x0,   // 0b00xx Transfer direction
            TAR = 0x4,   // 0b01xx Transfer address
            DRW = 0xC,   // 0b11xx Data read/Write
            CFG = 0xF4,  // 0b01xx
            BASE = 0xF8, // 0b10xx DEBUG AHB ROM
            IDR = 0xFC  // 0b11xx
        }

        private enum CTRLSTAT : uint
        {
            CSYSPWRUPACK = 1U << 31,
            CSYSPWRUPREQ = 1U << 30,
            CDBGPWRUPACK = 1U << 29,
            CDBGPWRUPREQ = 1U << 28,
            CDBGRSTACK = 1U << 27,
            CDBRSTREQ = 1U << 26,
            WDATAERR = 1U << 7,
            READOK = 1U << 6,
            STICKYERR = 1U << 5,
            STICKYCMP = 1U << 4,
            STICKYORUN = 1U << 1,
            ORUNDETECT = 1U << 0
        }

        private void ValidateAndPrintChain()
        {
            Chain_IsValid = true;
            Chain_Print();
            for (int i = 0, loopTo = Devices.Count - 1; i <= loopTo; i++)
            {
                if (Devices[i].BSDL is null)
                    Chain_IsValid = false;
            }
        }

        private void JTAG_PrintClockSpeed()
        {
            switch (TCK_SPEED)
            {
                case JTAG_SPEED._40MHZ:
                    {
                        MainApp.PrintConsole("JTAG TCK speed: 40 MHz");
                        break;
                    }

                case JTAG_SPEED._20MHZ:
                    {
                        MainApp.PrintConsole("JTAG TCK speed: 20 MHz");
                        break;
                    }

                case JTAG_SPEED._10MHZ:
                    {
                        MainApp.PrintConsole("JTAG TCK speed: 10 MHz");
                        break;
                    }

                case JTAG_SPEED._1MHZ:
                    {
                        MainApp.PrintConsole("JTAG TCK speed: 1 MHz");
                        break;
                    }
            }
        }

        private uint JTAG_GetTckInHerz()
        {
            switch (TCK_SPEED)
            {
                case JTAG_SPEED._1MHZ:
                    {
                        return 1000000U;
                    }

                case JTAG_SPEED._10MHZ:
                    {
                        return 10000000U;
                    }

                case JTAG_SPEED._20MHZ:
                    {
                        return 20000000U;
                    }

                case JTAG_SPEED._40MHZ:
                    {
                        return 40000000U;
                    }

                default:
                    {
                        return 0U;
                    }
            }
        }
        // Attempts to auto-detect a JTAG device on the TAP, returns the IR Length of the device
        private bool TAP_Detect()
        {
            Devices.Clear();
            var r_data = new byte[64];
            bool result = FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_DETECT, ref r_data);
            if (!result)
                return false;
            int dev_count = r_data[0];
            Chain_BitLength = r_data[1];
            if (dev_count == 0)
                return false;
            int ptr = 2;
            uint ID32 = 0U;
            for (int i = 0, loopTo = dev_count - 1; i <= loopTo; i++)
            {
                ID32 = (uint)r_data[ptr] << 24;
                ID32 = ID32 | (uint)r_data[ptr + 1] << 16;
                ID32 = ID32 | (uint)r_data[ptr + 2] << 8;
                ID32 = ID32 | (uint)r_data[ptr + 3] << 0;
                ptr += 4;
                if (!(ID32 == 0L | ID32 == 0xFFFFFFFFU))
                {
                    var n = new JTAG_DEVICE();
                    n.IDCODE = ID32;
                    n.VERSION = (short)((ID32 & 0xF0000000L) >> 28);
                    n.PARTNU = (ushort)((ID32 & 0xFFFF000L) >> 12);
                    n.MANUID = (ushort)((ID32 & 0xFFEL) >> 1);
                    n.BSDL = BSDL_GetDefinition(ID32);
                    if (n.BSDL is object)
                        n.IR_LENGTH = n.BSDL.IR_LEN;
                    Devices.Add(n);
                }
            }

            if (Devices.Count == 0)
                return false;
            if (Devices.Count == 1 && Devices[0].BSDL is null)
            {
                Devices[0].IR_LENGTH = r_data[1];
            }

            Devices.Reverse(); // Put devices in order closest to HOST
            return true;
        }

        public void Configure(PROCESSOR proc_type) {
            Devices[Chain_SelectedIndex].ACCESS = JTAG_MEM_ACCESS.NONE;
            if (proc_type == PROCESSOR.MIPS)
            {
                MainApp.PrintConsole("Configure JTAG engine for MIPS processor");
                uint IMPCODE = AccessDataRegister32(Devices[Chain_SelectedIndex].BSDL.MIPS_IMPCODE);
                EJTAG_LoadCapabilities(IMPCODE); // Only supported by MIPS/EJTAG devices
                if (DMA_SUPPORTED)
                {
                    uint r = ReadMemory(0xFF300000U, DATA_WIDTH.Word); // Returns 2000001E 
                    r = r & 0xFFFFFFFBU; // 2000001A
                    WriteMemory(0xFF300000U, r, DATA_WIDTH.Word);
                    Devices[Chain_SelectedIndex].ACCESS = JTAG_MEM_ACCESS.EJTAG_DMA;
                    MainApp.PrintConsole("Target device supports DMA mode");
                } else {
                    Devices[Chain_SelectedIndex].ACCESS = JTAG_MEM_ACCESS.EJTAG_PRACC;
                    MainApp.PrintConsole("Target device does not support DMA mode");
                }
            }
            else if (proc_type == PROCESSOR.ARM)
            {
                MainApp.PrintConsole("Configure JTAG engine for ARM processor");
                Devices[Chain_SelectedIndex].ACCESS = JTAG_MEM_ACCESS.ARM;
                // This does processor disable_cache, disable_mmu, and halt
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_ARM_INIT, null);
            }
        }

#endregion

#region "Boundary Scan Programmer"
        public NOR_CFI CFI { get; set; } // Contains CFI table information (NOR)

        private const bool IO_INPUT = false; // 0
        private const bool IO_OUTPUT = true; // 1
        private const bool LOGIC_LOW = false; // 0
        private const bool LOGIC_HIGH = true; // 1
        private List<BoundaryCell> BoundaryMap = new List<BoundaryCell>();
        private Device BSDL_FLASH_DEVICE;
        private MemoryInterface.MemoryDeviceInstance BSDL_IF = null;
        private bool SetupReady = false;

        private BSDL_DQ_IO BSDL_IO { get; set; }
        private int BDR_DQ_SIZE { get; set; } = 0;
        private int BDR_ADDR_SIZE { get; set; } = 0;

        private enum BSDL_DQ_IO : uint
        {
            X16 = 1U,
            X8 = 2U,
            X8_OVER_X16 = 3U
        }

        public void BoundaryScan_Setup()
        {
            SetupReady = false;
            if (MainApp.MySettings.LICENSED_TO.Equals(""))
            {
                MainApp.PrintConsole("Boundary Scan Library only available with commercial license");
                return;
            }

            if (Devices[Chain_SelectedIndex].BSDL.BS_LEN == 0)
            {
                MainApp.PrintConsole("Boundary Scan error: BSDL scan length not not specified");
                return;
            }

            BoundaryMap.Clear();
            BDR_DQ_SIZE = 0;
            BDR_ADDR_SIZE = 0;
            BSDL_FLASH_DEVICE = null;
            var setup_data = new byte[11];
            setup_data[0] = (byte)(Devices[Chain_SelectedIndex].BSDL.EXTEST & 255L);
            setup_data[1] = (byte)(Devices[Chain_SelectedIndex].BSDL.EXTEST >> 8 & 255L);
            setup_data[2] = (byte)(Devices[Chain_SelectedIndex].BSDL.EXTEST >> 16 & 255L);
            setup_data[3] = (byte)(Devices[Chain_SelectedIndex].BSDL.EXTEST >> 24 & 255L);
            setup_data[4] = (byte)(Devices[Chain_SelectedIndex].BSDL.SAMPLE & 255L);
            setup_data[5] = (byte)(Devices[Chain_SelectedIndex].BSDL.SAMPLE >> 8 & 255L);
            setup_data[6] = (byte)(Devices[Chain_SelectedIndex].BSDL.SAMPLE >> 16 & 255L);
            setup_data[7] = (byte)(Devices[Chain_SelectedIndex].BSDL.SAMPLE >> 24 & 255L);
            setup_data[8] = (byte)(Devices[Chain_SelectedIndex].BSDL.BS_LEN & 255);
            setup_data[9] = (byte)(Devices[Chain_SelectedIndex].BSDL.BS_LEN >> 8 & 255);
            setup_data[10] = Conversions.ToByte(Devices[Chain_SelectedIndex].BSDL.DISVAL);
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data);
            SetupReady = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_BDR_SETUP);
        }

        public bool BoundaryScan_Init(bool EnableX8Mode)
        {
            if (!SetupReady)
                return false;
            MainApp.PrintConsole("JTAG Boundary Scan Programmer");
            if (!FCUSB.HasLogic)
            {
                MainApp.PrintConsole("This feature is only available using FlashcatUSB Professional");
                return false;
            }

            if (!BSDL_Is_Configured())
                return false;
            foreach (var item in BoundaryMap)
            {
                var pin_data = new byte[8];
                pin_data[0] = (byte)item.TYPE; // AD,DQ,WE,OE,CE,WP,RST,BYTE
                pin_data[1] = item.pin_index; // ADx, DQx
                pin_data[2] = (byte)(item.OUTPUT & 255);
                pin_data[3] = (byte)(item.OUTPUT >> 8);
                pin_data[4] = (byte)(item.CONTROL & 255);
                pin_data[5] = (byte)(item.CONTROL >> 8);
                pin_data[6] = (byte)(item.INPUT & 255);
                pin_data[7] = (byte)(item.INPUT >> 8);
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, pin_data);
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_BDR_ADDPIN);
            }

            if (EnableX8Mode)
            {
                BSDL_IO = BSDL_DQ_IO.X8_OVER_X16;
            }
            else if (BDR_DQ_SIZE == 8)
            {
                BSDL_IO = BSDL_DQ_IO.X8;
            }
            else if (BDR_DQ_SIZE == 16)
            {
                BSDL_IO = BSDL_DQ_IO.X16;
            }

            uint dw = 0U; // X8
            if (BSDL_IO == BSDL_DQ_IO.X16 | BSDL_IO == BSDL_DQ_IO.X8_OVER_X16)
                dw = 1U;
            if (!FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_BDR_INIT, null, dw))
            {
                MainApp.PrintConsole("Error: Boundary Scan init failed");
                return false;
            }

            return true;
        }

        public bool BoundaryScan_Detect() {
            CFI = null;
            FlashDetectResult FLASH_RESULT;
            if (BSDL_IO == BSDL_DQ_IO.X8)
            {
                FLASH_RESULT = Tools.GetFlashResult(BSDL_ReadIdent(false)); // X8 Device
            }
            else
            {
                FLASH_RESULT = Tools.GetFlashResult(BSDL_ReadIdent(true));
            } // X16 Device

            if (FLASH_RESULT.Successful)
            {
                CFI = new NOR_CFI(BSDL_GetCFI());
                Device[] device_matches;
                string chip_id_str = Conversion.Hex(FLASH_RESULT.MFG).PadLeft(2, '0') + Conversion.Hex((uint)FLASH_RESULT.ID1 << 16 | (uint)FLASH_RESULT.ID2).PadLeft(8, '0');
                MainApp.PrintConsole("Flash detected: DEVICE ID: 0x" + chip_id_str);
                device_matches = MainApp.FlashDatabase.FindDevices(FLASH_RESULT.MFG, FLASH_RESULT.ID1, FLASH_RESULT.ID2, MemoryType.PARALLEL_NOR);
                if (device_matches is object && device_matches.Count() > 0)
                {
                    if (device_matches.Count() > 1 && CFI.IS_VALID)
                    {
                        for (int i = 0, loopTo = device_matches.Count() - 1; i <= loopTo; i++)
                        {
                            if ((long)device_matches[i].PAGE_SIZE == (long)CFI.WRITE_BUFFER_SIZE)
                            {
                                BSDL_FLASH_DEVICE = device_matches[i];
                                break;
                            }
                        }
                    }
                    if (BSDL_FLASH_DEVICE is null) { BSDL_FLASH_DEVICE = device_matches[0]; }
                    MainApp.PrintConsole(string.Format("Flash detected: {0} ({1} bytes)", BSDL_FLASH_DEVICE.NAME, Strings.Format((object)BSDL_FLASH_DEVICE.FLASH_SIZE, "#,###")));
                    BSDL_IF = MainApp.Connected_Event(FCUSB, 4096);
                }
                else
                {
                    MainApp.PrintConsole("CFI Flash device not found in library");
                }
            }
            else
            {
                MainApp.PrintConsole("No CFI Flash (X8/X16) device detected");
            }

            return false;
        }

        private bool BSDL_Is_Configured()
        {
            BDR_DQ_SIZE = 0;
            BDR_ADDR_SIZE = 0;
            foreach (var item in BoundaryMap)
            {
                if (item.pin_name.ToUpper().StartsWith("AD"))
                    BDR_ADDR_SIZE += 1;
            }

            foreach (var item in BoundaryMap)
            {
                if (item.pin_name.ToUpper().StartsWith("DQ"))
                    BDR_DQ_SIZE += 1;
            }

            if (BDR_ADDR_SIZE < 16)
            {
                MainApp.PrintConsole("Error, AQ pins need to be have at least 16 bits");
                return false;
            }

            if (!(BDR_DQ_SIZE == 8 | BDR_DQ_SIZE == 16))
            {
                MainApp.PrintConsole("Error, DQ pins need to be assigned either 8 or 16 bits");
                return false;
            }

            for (int i = 0, loopTo = BDR_ADDR_SIZE - 1; i <= loopTo; i++)
            {
                if (BoundaryScan_GetPinIndex("AD" + i.ToString()) == -1)
                {
                    MainApp.PrintConsole("Error, missing address pin: AD" + i.ToString());
                    return false;
                }
            }

            for (int i = 0, loopTo1 = BDR_DQ_SIZE - 1; i <= loopTo1; i++)
            {
                if (BoundaryScan_GetPinIndex("DQ" + i.ToString()) == -1)
                {
                    MainApp.PrintConsole("Error, missing address pin: DQ" + i.ToString());
                    return false;
                }
            }

            if (BoundaryScan_GetPinIndex("WE#") == -1)
            {
                MainApp.PrintConsole("Error, missing address pin: WE#");
                return false;
            }

            if (BoundaryScan_GetPinIndex("OE#") == -1)
            {
                MainApp.PrintConsole("Error, missing address pin: OE#");
                return false;
            }

            MainApp.PrintConsole("Interface configured: X" + BDR_DQ_SIZE.ToString() + " (" + BDR_ADDR_SIZE + "-bit address)");
            return true;
        }

        private byte[] BSDL_ReadIdent(bool X16_MODE)
        {
            var ident = new byte[8];
            uint SHIFT = 0U;
            if (X16_MODE)
                SHIFT = 1U;
            BSDL_ResetDevice();
            Thread.Sleep(1);
            BSDL_WriteCmdData(0x5555U, 0xAA);
            BSDL_WriteCmdData(0x2AAAU, 0x55);
            BSDL_WriteCmdData(0x5555U, 0x90);
            Thread.Sleep(10);
            ident[0] = (byte)(BSDL_ReadWord(0U) & 0xFF);             // MFG
            ushort ID1 = BSDL_ReadWord((uint)(1 << (int)SHIFT));
            if (!X16_MODE)
                ID1 = (ushort)(ID1 & 0xFF);               // X8 ID1
            ident[1] = (byte)(ID1 >> 8 & 0xFF);                   // ID1(UPPER)
            ident[2] = (byte)(ID1 & 0xFF);                          // ID1(LOWER)
            ident[3] = (byte)(BSDL_ReadWord((uint)(0xE << (int)SHIFT)) & 0xFF);  // ID2
            ident[4] = (byte)(BSDL_ReadWord((uint)(0xF << (int)SHIFT)) & 0xFF);  // ID3
            BSDL_ResetDevice();
            Thread.Sleep(1);
            return ident;
        }

        private void BSDL_ResetDevice()
        {
            BSDL_WriteCmdData(0x5555U, 0xAA); // Standard
            BSDL_WriteCmdData(0x2AAAU, 0x55);
            BSDL_WriteCmdData(0x5555U, 0xF0);
            BSDL_WriteCmdData(0U, 0xFF);
            BSDL_WriteCmdData(0U, 0xF0); // Intel
        }

        private byte[] BSDL_GetCFI()
        {
            var cfi_data = new byte[32];
            uint SHIFT = 0U;
            if (BSDL_IO == BSDL_DQ_IO.X16 | BSDL_IO == BSDL_DQ_IO.X8_OVER_X16)
                SHIFT = 1U;
            try
            {
                BSDL_WriteCmdData(0x55U, 0x98);
                for (int i = 0, loopTo = cfi_data.Length - 1; i <= loopTo; i++)
                    cfi_data[i] = (byte)(BSDL_ReadWord((uint)(0x10 + i << (int)SHIFT)) & 255);
                if (cfi_data[0] == 0x51 & cfi_data[1] == 0x52 & cfi_data[2] == 0x59)
                    return cfi_data;
                BSDL_WriteCmdData(0x5555U, 0xAA);
                BSDL_WriteCmdData(0x2AAAU, 0x55);
                BSDL_WriteCmdData(0x5555U, 0x98);
                for (int i = 0, loopTo1 = cfi_data.Length - 1; i <= loopTo1; i++)
                    cfi_data[i] = (byte)(BSDL_ReadWord((uint)(0x10 + i << (int)SHIFT)) & 255);
                if (cfi_data[0] == 0x51 & cfi_data[1] == 0x52 & cfi_data[2] == 0x59)
                    return cfi_data;
            }
            catch
            {
            }
            finally
            {
                BSDL_ResetDevice();
            }

            return null;
        }
        // Defines a pin. Output cell can be output/bidir, control_cell can be -1 if it is output_cell+1, and input_cell is used when not bidir
        public void BoundaryScan_AddPin(string signal_name, int output_cell, int control_cell, int input_cell)
        {
            var pin_desc = new BoundaryCell();
            pin_desc.pin_name = signal_name.ToUpper();
            pin_desc.pin_index = 0;
            if (pin_desc.pin_name.StartsWith("AD"))
            {
                pin_desc.TYPE = BoundaryScan_PinType.AD;
                pin_desc.pin_index = Conversions.ToByte(pin_desc.pin_name.Substring(2));
                BDR_ADDR_SIZE += 1;
            }
            else if (pin_desc.pin_name.StartsWith("DQ"))
            {
                pin_desc.TYPE = BoundaryScan_PinType.DQ;
                pin_desc.pin_index = Conversions.ToByte(pin_desc.pin_name.Substring(2));
                BDR_DQ_SIZE += 1;
            }
            else if (pin_desc.pin_name.Equals("WE#"))
            {
                pin_desc.TYPE = BoundaryScan_PinType.WE;
            }
            else if (pin_desc.pin_name.Equals("OE#"))
            {
                pin_desc.TYPE = BoundaryScan_PinType.OE;
            }
            else if (pin_desc.pin_name.Equals("CE#"))
            {
                pin_desc.TYPE = BoundaryScan_PinType.CE;
            }
            else if (pin_desc.pin_name.Equals("WP#")) // (optional)
            {
                pin_desc.TYPE = BoundaryScan_PinType.WP;
            }
            else if (pin_desc.pin_name.Equals("RESET#")) // (optional)
            {
                pin_desc.TYPE = BoundaryScan_PinType.RESET;
            }
            else if (pin_desc.pin_name.Equals("BYTE#")) // (optional)
            {
                pin_desc.TYPE = BoundaryScan_PinType.BYTE_MODE;
            }
            else
            {
                MainApp.PrintConsole("Boundary Scan Programmer: Pin name not reconized: " + pin_desc.pin_name);
                return;
            } // ERROR

            pin_desc.OUTPUT = (ushort)output_cell;
            pin_desc.CONTROL = (ushort)control_cell;
            if (input_cell == -1)
            {
                pin_desc.INPUT = (ushort)output_cell;
            }
            else
            {
                pin_desc.INPUT = (ushort)input_cell;
            }

            BoundaryMap.Add(pin_desc);
        }

        public void BoundaryScan_SetBSR(int output_cell, int control_cell, bool level)
        {
            uint dw = (uint)(control_cell << 16 | output_cell);
            if (level)
                dw = (uint)(dw | 1UL << 31);
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_BDR_SETBSR, null, dw);
        }

        public void BoundaryScan_WriteBSR()
        {
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_BDR_WRITEBSR);
        }

        public int BoundaryScan_GetPinIndex(string signal_name)
        {
            int index = 0;
            foreach (var item in BoundaryMap)
            {
                if (item.pin_name.ToUpper().Equals(signal_name.ToUpper()))
                    return index;
                index += 1;
            }

            return -1;
        }

        private BoundaryCell GetBoundaryCell(string signal_name)
        {
            foreach (var item in BoundaryMap)
            {
                if (item.pin_name.ToUpper().Equals(signal_name.ToUpper()))
                    return item;
            }

            return default;
        }

        private struct BoundaryCell
        {
            public string pin_name;
            public byte pin_index; // This is the DQx or ADx value
            public ushort OUTPUT;
            public ushort INPUT; // Some devices use bidir cells (this is for seperate o/i cells)
            public ushort CONTROL;
            public BoundaryScan_PinType TYPE;
        }

        private enum BoundaryScan_PinType : byte
        {
            AD = 1,
            DQ = 2,
            WE = 3,
            OE = 4,
            CE = 5,
            WP = 6,
            RESET = 7,
            BYTE_MODE = 8,
            CNT_LOW = 9, // Contant HIGH output
            CNT_HIGH = 10 // Constant LOW output
        }

        public ushort BSDL_ReadWord(uint base_addr)
        {
            var dt = new byte[4];
            bool result = FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_BDR_RDMEM, ref dt, base_addr);
            ushort DQ16 = (ushort)(dt[2] << 8 | dt[3]);
            if (BSDL_IO == BSDL_DQ_IO.X16)
            {
                return DQ16;
            }
            else if (BSDL_IO == BSDL_DQ_IO.X8)
            {
                return (ushort)(DQ16 & 255);
            }
            else if (BSDL_IO == BSDL_DQ_IO.X8_OVER_X16)
            {
                return (ushort)(DQ16 & 255);
            }

            return 0;
        }

        public void BSDL_WriteCmdData(uint base_addr, ushort data16)
        {
            var dt_out = new byte[6];
            dt_out[0] = (byte)(base_addr & 255L);
            dt_out[1] = (byte)(base_addr >> 8 & 255L);
            dt_out[2] = (byte)(base_addr >> 16 & 255L);
            dt_out[3] = (byte)(base_addr >> 24 & 255L);
            dt_out[4] = (byte)(data16 & 255);
            dt_out[5] = (byte)(data16 >> 8 & 255);
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, dt_out);
            bool result = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_BDR_WRCMD);
        }

        public void BSDL_WriteMemAddress(uint base_addr, ushort data16)
        {
            var dt_out = new byte[6];
            dt_out[0] = (byte)(base_addr & 255L);
            dt_out[1] = (byte)(base_addr >> 8 & 255L);
            dt_out[2] = (byte)(base_addr >> 16 & 255L);
            dt_out[3] = (byte)(base_addr >> 24 & 255L);
            dt_out[4] = (byte)(data16 & 255);
            dt_out[5] = (byte)(data16 >> 8 & 255);
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, dt_out);
            bool result = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_BDR_WRMEM);
        }

        private byte[] BoundaryScan_GetSetupPacket(uint Address, uint Count, ushort PageSize)
        {
            var data_in = new byte[20]; // 18 bytes total
            data_in[0] = (byte)(Address & 255L);
            data_in[1] = (byte)(Address >> 8 & 255L);
            data_in[2] = (byte)(Address >> 16 & 255L);
            data_in[3] = (byte)(Address >> 24 & 255L);
            data_in[4] = (byte)(Count & 255L);
            data_in[5] = (byte)(Count >> 8 & 255L);
            data_in[6] = (byte)(Count >> 16 & 255L);
            data_in[7] = (byte)(Count >> 24 & 255L);
            data_in[8] = (byte)(PageSize & 255); // This is how many bytes to increment between operations
            data_in[9] = (byte)(PageSize >> 8 & 255);
            return data_in;
        }

        public string BoundaryScan_DeviceName
        {
            get
            {
                P_NOR NOR_FLASH = (P_NOR)BSDL_FLASH_DEVICE;
                return NOR_FLASH.NAME;
            }
        }

        public long BoundaryScan_DeviceSize
        {
            get
            {
                P_NOR NOR_FLASH = (P_NOR)BSDL_FLASH_DEVICE;
                return NOR_FLASH.FLASH_SIZE;
            }
        }

        public byte[] BoundaryScan_ReadFlash(uint base_addr, uint read_count)
        {
            int byte_count = (int)read_count;
            if (BSDL_IO == BSDL_DQ_IO.X8_OVER_X16)
            {
                byte_count = byte_count * 2;
                base_addr = base_addr << 1;
            }

            var data_out = new byte[byte_count]; // Bytes we want to read
            uint data_left = (uint)byte_count;
            int ptr = 0;
            while (data_left > 0L)
            {
                uint packet_size = (uint)Math.Min(8192L, data_left);
                var packet_data = new byte[(int)(packet_size - 1L + 1)];
                var setup_data = BoundaryScan_GetSetupPacket(base_addr, packet_size, 0);
                bool result = FCUSB.USB_SETUP_BULKIN(USB.USBREQ.JTAG_BDR_RDFLASH, setup_data, ref packet_data, 0U);
                if (!result)
                    return null;
                Array.Copy(packet_data, 0L, data_out, ptr, packet_size);
                ptr = (int)(ptr + packet_size);
                base_addr += packet_size;
                data_left -= packet_size;
            }

            if (BSDL_IO == BSDL_DQ_IO.X8_OVER_X16)
            {
                var new_data_out = new byte[(int)(read_count - 1L + 1)];
                for (int i = 0, loopTo = byte_count - 1; i <= loopTo; i += 2)
                    new_data_out[i >> 1] = data_out[i];
                return new_data_out;
            }
            else
            {
                return data_out;
            }
        }

        public void BoundaryScan_WaitForReady()
        {
            Thread.Sleep(100);
        }

        public long BoundaryScan_SectorFind(uint sector_index)
        {
            uint base_addr = 0U;
            if (sector_index > 0L)
            {
                for (uint i = 0U, loopTo = (uint)(sector_index - 1L); i <= loopTo; i++)
                    base_addr += BoundaryScan_GetSectorSize(i);
            }

            return base_addr;
        }

        public bool BoundaryScan_SectorWrite(uint sector_index, byte[] data)
        {
            uint Addr32 = (uint)BoundaryScan_SectorFind(sector_index);
            return BoundaryScan_WriteFlash(Addr32, data);
        }

        public uint BoundaryScan_SectorCount()
        {
            P_NOR NOR_FLASH = (P_NOR)BSDL_FLASH_DEVICE;
            return NOR_FLASH.Sector_Count;
        }

        public bool BoundaryScan_WriteFlash(uint base_addr, byte[] data_to_write)
        {
            P_NOR NOR_FLASH = (P_NOR)BSDL_FLASH_DEVICE;
            uint DataToWrite = (uint)data_to_write.Length;
            uint PacketSize = 8192U;
            if (BSDL_IO == BSDL_DQ_IO.X8_OVER_X16)
            {
                base_addr = base_addr << 1;
                var data_bloated = new byte[(int)(DataToWrite * 2L - 1L + 1)];
                for (long i = 0L, loopTo = DataToWrite - 1L; i <= loopTo; i++)
                    data_bloated[(int)(i * 2L)] = data_to_write[(int)i];
                data_to_write = data_bloated;
                DataToWrite = (uint)data_to_write.Length;
            }

            int Loops = (int)Math.Ceiling(DataToWrite / (double)PacketSize); // Calcuates iterations
            for (int i = 0, loopTo1 = Loops - 1; i <= loopTo1; i++)
            {
                int BufferSize = (int)DataToWrite;
                if (BufferSize > PacketSize)
                    BufferSize = (int)PacketSize;
                var data = new byte[BufferSize];
                Array.Copy(data_to_write, i * PacketSize, data, 0L, data.Length);
                var setup_data = BoundaryScan_GetSetupPacket(base_addr, (uint)data.Length, (ushort)NOR_FLASH.PAGE_SIZE);
                uint BSDL_PROG_CMD = (uint)NOR_FLASH.WriteMode;
                bool result = FCUSB.USB_SETUP_BULKOUT(USB.USBREQ.JTAG_BDR_WRFLASH, setup_data, data, BSDL_PROG_CMD);
                if (!result)
                    return false;
                Thread.Sleep(350); // We need a long pause here after each program <-- critical that this is 350
                base_addr = (uint)(base_addr + data.Length);
                bool Success = FCUSB.USB_WaitForComplete();
            }

            return true;
        }

        public bool BoundaryScan_SectorErase(uint sector_index)
        {
            P_NOR NOR_FLASH = (P_NOR)BSDL_FLASH_DEVICE;
            uint sector_addr = (uint)BoundaryScan_SectorFind(sector_index);
            if (BSDL_IO == BSDL_DQ_IO.X8_OVER_X16)
            {
                sector_addr = sector_addr << 1;
            }

            if (NOR_FLASH.WriteMode == MFP_PRG.IntelSharp | NOR_FLASH.WriteMode == MFP_PRG.Buffer1)
            {
                BSDL_WriteMemAddress(sector_addr, 0x50); // clear register
                BSDL_WriteMemAddress(sector_addr, 0x60); // Unlock block (just in case)
                BSDL_WriteMemAddress(sector_addr, 0xD0); // Confirm Command
                // EXPIO_WAIT()
                BSDL_WriteMemAddress(sector_addr, 0x20);
                BSDL_WriteMemAddress(sector_addr, 0xD0);
                // EXPIO_WAIT()
                BSDL_WriteMemAddress(0U, 0xFF); // Puts the device back into READ mode
                BSDL_WriteMemAddress(0U, 0xF0);
            }
            else
            {
                // Write Unlock Cycles
                BSDL_WriteCmdData(0x5555U, 0xAA);
                BSDL_WriteCmdData(0x2AAAU, 0x55);
                // Write Sector Erase Cycles
                BSDL_WriteCmdData(0x5555U, 0x80);
                BSDL_WriteCmdData(0x5555U, 0xAA);
                BSDL_WriteCmdData(0x2AAAU, 0x55);
                BSDL_WriteMemAddress(sector_addr, 0x30);
            }

            Thread.Sleep(100);
            int counter = 0;
            ushort dw = 0;
            ushort erased_value = 0xFF;
            if (BSDL_IO == BSDL_DQ_IO.X16)
                erased_value = 0xFFFF;
            while (dw != erased_value)
            {
                Thread.Sleep(20);
                dw = BSDL_ReadWord(sector_addr);
                counter += 1;
                if (counter == 100)
                    return false;
            }

            return true;
        }

        public bool BoundaryScan_EraseDevice()
        {
            P_NOR NOR_FLASH = (P_NOR)BSDL_FLASH_DEVICE;
            if (NOR_FLASH.WriteMode == MFP_PRG.IntelSharp | NOR_FLASH.WriteMode == MFP_PRG.Buffer1)
            {
                BSDL_WriteMemAddress(0x0U, 0x30);
                BSDL_WriteMemAddress(0x0U, 0xD0);
            }
            else
            {
                BSDL_WriteCmdData(0x5555U, 0xAA);
                BSDL_WriteCmdData(0x2AAAU, 0x55);
                BSDL_WriteCmdData(0x5555U, 0x80);
                BSDL_WriteCmdData(0x5555U, 0xAA);
                BSDL_WriteCmdData(0x2AAAU, 0x55);
                BSDL_WriteCmdData(0x5555U, 0x10);
            }

            Thread.Sleep(500);
            int counter = 0;
            var dw = default(ushort);
            while (dw != 0xFFFF)
            {
                Thread.Sleep(100);
                dw = BSDL_ReadWord(0U);
                counter += 1;
                if (counter == 100)
                    return false;
            }

            return true;
        }

        public uint BoundaryScan_GetSectorSize(uint sector_index)
        {
            P_NOR NOR_FLASH = (P_NOR)BSDL_FLASH_DEVICE;
            if (BSDL_IO == BSDL_DQ_IO.X8_OVER_X16)
            {
                return (uint)((double)NOR_FLASH.GetSectorSize((int)sector_index) / 2d);
            }
            else
            {
                return (uint)NOR_FLASH.GetSectorSize((int)sector_index);
            }
        }

        #endregion

#region "ARM Support"

        private const int NO_SYSSPEED = 0;
        private const int YES_SYSSPEED = 1;
        private const uint ARM_NOOP = 0xE1A00000U;

        public uint ARM_ReadMemory32(uint addr)
        {
            ARM_WriteRegister_r0(addr);
            ARM_PushInstruction(Conversions.ToBoolean(NO_SYSSPEED), 0xE5901000U, 0);   // "LDR r1, [r0]"
            ARM_PushInstruction(Conversions.ToBoolean(YES_SYSSPEED), 0xE1A00000U, 0);  // "NOP"
            ARM_IR((byte)Devices[0].BSDL.RESTART);
            TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            return ARM_ReadRegister_r1();
        }

        public void ARM_WriteMemory32(uint addr, uint data)
        {
            ARM_WriteRegister_r0r1(addr, data);
            ARM_PushInstruction(Conversions.ToBoolean(NO_SYSSPEED), 0xE5801000U, 0);   // "STR r1, [r0]"
            ARM_PushInstruction(Conversions.ToBoolean(YES_SYSSPEED), 0xE1A00000U, 0);  // "NOP"
            ARM_IR((byte)Devices[0].BSDL.RESTART);
            TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
        }

        public void ARM_WriteRegister_r0(uint r0)
        {
            uint r0_rev = REVERSE32(r0);
            ARM_SelectChain1();
            ARM_PushInstruction(Conversions.ToBoolean(NO_SYSSPEED), 0xE59F0000U, 0);   // "LDR r0, [pc]"
            ARM_PushInstruction(Conversions.ToBoolean(NO_SYSSPEED), ARM_NOOP, 1);       // "NOP"
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR);
            var tdi_data = Utilities.Bytes.FromUInt32(r0_rev);
            Array.Resize(ref tdi_data, 5); // Add on one extra byte
            byte[] argtdo_bits = null;
            this.ShiftTDI(34U, tdi_data, ref argtdo_bits, true);                 // shits an extra 0b00 at the End
            ARM_PushInstruction(Conversions.ToBoolean(NO_SYSSPEED), 0xE1A00000U, 1);
        }

        public void ARM_WriteRegister_r0r1(uint r0, uint r1)
        {
            uint r0_rev = REVERSE32(r0);
            uint r1_rev = REVERSE32(r1);
            ARM_SelectChain1();
            ARM_PushInstruction(Conversions.ToBoolean(NO_SYSSPEED), 0xE89F0003U, 0);   // "LDMIA pc, {r0-r1}"
            ARM_PushInstruction(Conversions.ToBoolean(NO_SYSSPEED), ARM_NOOP, 1);       // "NOP"
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR);
            byte[] argtdo_bits = null;
            this.ShiftTDI(32U, Utilities.Bytes.FromUInt32(r0_rev), ref argtdo_bits, true);
            ARM_PushInstruction(Conversions.ToBoolean(NO_SYSSPEED), 0xE1A00000U, 0);
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR);
            byte[] argtdo_bits1 = null;
            this.ShiftTDI(32U, Utilities.Bytes.FromUInt32(r1_rev), ref argtdo_bits1, true);
            ARM_PushInstruction(Conversions.ToBoolean(NO_SYSSPEED), ARM_NOOP, 1);
        }

        public uint ARM_ReadRegister_r0(uint r0, uint r1)
        {
            ARM_SelectChain1();
            ARM_PushInstruction(Conversions.ToBoolean(NO_SYSSPEED), 0xE58F0000U, 0);  // "STR r0, [pc]"
            ARM_PushInstruction(Conversions.ToBoolean(NO_SYSSPEED), ARM_NOOP, 1);      // "NOP"
            var tdo = new byte[32];
            ShiftTDI(32U, null, ref tdo, true);
            TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            uint reg_out = this.REVERSE32(Utilities.Bytes.ToUInt32(tdo));
            return reg_out;
        }

        public uint ARM_ReadRegister_r1()
        {
            ARM_SelectChain1();
            ARM_PushInstruction(Conversions.ToBoolean(NO_SYSSPEED), 0xE58F1000U, 0); // "STR r1, [pc]"
            ARM_PushInstruction(Conversions.ToBoolean(NO_SYSSPEED), ARM_NOOP, 1); // "NOP"
            var tdo = new byte[32];
            ShiftTDI(32U, null, ref tdo, true);
            TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            uint reg_out = this.REVERSE32(Utilities.Bytes.ToUInt32(tdo));
            return reg_out;
        }

        public void ARM_SelectChain1()
        {
            ARM_IR((byte)Devices[0].BSDL.SCAN_N);
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR);
            byte[] argtdo_bits = null;
            this.ShiftTDI(5U, new byte[] { 1 }, ref argtdo_bits, true); // shift: 0x10 (correct to 0b00001)
            ARM_IR((byte)Devices[0].BSDL.INTEST);
        }

        public void ARM_SelectChain2()
        {
            ARM_IR((byte)Devices[0].BSDL.SCAN_N);
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR);
            byte[] argtdo_bits = null;
            this.ShiftTDI(5U, new byte[] { 2 }, ref argtdo_bits, true); // shift: 0x08 (reversed to 0b00010)
            ARM_IR((byte)Devices[0].BSDL.INTEST);
        }

        public void ARM_PushInstruction(bool SYSSPEED, uint op_cmd, byte rti_cycles)
        {
            uint op_rev = REVERSE32(op_cmd); // So we shift MSB first
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR);
            ulong tdi64;
            if (SYSSPEED)
            {
                tdi64 = (ulong)((long)((ulong)op_rev << 1) | 1L);
            }
            else
            {
                tdi64 = (ulong)op_rev << 1;
            }
            var tdi = Utilities.Bytes.FromUInt64(tdi64);
            tdi = new byte[5]; // 5 bytes only
            byte[] argtdo_bits = null;
            this.ShiftTDI(33U, tdi, ref argtdo_bits, false);
            TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle);
            if (rti_cycles > 0)
                Tap_Toggle(rti_cycles, false);
        }

        public void ARM_IR(byte ir_data)
        {
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR);
            byte[] argtdo_bits = null;
            ShiftTDI(4U, new[] { ir_data }, ref argtdo_bits, true);
        }

        public uint REVERSE32(uint u32)
        {
            uint out32 = 0U;
            for (int i = 0; i <= 31; i++)
            {
                if ((u32 & 1L) == 1L)
                    out32 = (uint)(out32 | 1L);
                out32 = out32 << 1;
                u32 = u32 >> 1;
            }

            return out32;
        }

        #endregion

#region "SVF Player"

        public SVF_Player JSP;
        private uint CurrentHz = 1000000U;

        private void Setup_SVF_Player() {
            JSP = new SVF_Player();
            JSP.SetFrequency += JSP_SetFrequency;
            JSP.ResetTap += JSP_ResetTap;
            JSP.GotoState += JSP_GotoState;
            JSP.ToggleClock += JSP_ToggleClock;
            JSP.ShiftIR += JSP_ShiftIR;
            JSP.ShiftDR += JSP_ShiftDR;
            JSP.Writeconsole += JSP_Writeconsole;
        }

        private void JSP_SetFrequency(uint Hz)
        {
            CurrentHz = Hz;
        }

        private void JSP_ResetTap()
        {
            Reset_StateMachine();
        }

        private void JSP_GotoState(JTAG_MACHINE_STATE dst_state)
        {
            TAP_GotoState(dst_state);
        }

        private bool JSP_ToggleClock(uint ticks, bool exit_tms = false)
        {
            uint hz = JTAG_GetTckInHerz();
            int mult = (int)(hz / (float)CurrentHz);
            return Tap_Toggle((uint)(ticks * mult), exit_tms);
        }

        public void JSP_ShiftIR(byte[] tdi_bits, ref byte[] tdo_bits, short bit_count, bool exit_tms)
        {
            ShiftIR(tdi_bits, ref tdo_bits, bit_count, exit_tms);
        }

        public void JSP_ShiftDR(byte[] tdi_bits, ref byte[] tdo_bits, ushort bit_count, bool exit_tms)
        {
            ShiftDR(tdi_bits, ref tdo_bits, bit_count, exit_tms);
        }

        private void JSP_Writeconsole(string msg)
        {
            MainApp.PrintConsole("SVF Player: " + msg);
        }

        #endregion

#region "EJTAG Extension"

        public string IMPVER; // converted to text readable
        public bool RK4_ENV; // Indicates host is a R4000
        public bool RK3_ENV; // Indicates host is a R3000
        public bool DINT_SUPPORT; // Probe can use DINT signal to debug int on this cpu
        public short ASID_SIZE; // 0=no ASIS,1=6bit,2=8bit
        public bool MIPS16e; // Indicates MIPS16e ASE support
        public bool DMA_SUPPORTED; // Indicates no DMA support and must use PrAcc mode
        public bool MIPS32;
        public bool MIPS64;

        public enum EJTAG_CTRL : uint
        {
            // /* EJTAG 3.1 Control Register Bits */
            VPED = 1 << 23,       // /* R    */
            // /* EJTAG 2.6 Control Register Bits */
            Rocc = 1U << 31,     // /* R/W0 */
            Psz1 = 1 << 30,       // /* R    */
            Psz0 = 1 << 29,       // /* R    */
            Doze = 1 << 22,       // /* R    */
            ProbTrap = 1 << 14,   // /* R/W  */
            DebugMode = 1 << 3,   // /* R    */
            // /* EJTAG 1.5.3 Control Register Bits */
            Dnm = 1 << 28,        // /* */
            Sync = 1 << 23,       // /* R/W  */
            Run = 1 << 21,        // /* R    */
            PerRst = 1 << 20,     // /* R/W  */
            PRnW = 1 << 19,       // /* R    0 = Read, 1 = Write */
            PrAcc = 1 << 18,      // /* R/W0 */
            DmaAcc = 1 << 17,     // /* R/W  */
            PrRst = 1 << 16,      // /* R/W  */
            ProbEn = 1 << 15,     // /* R/W  */
            SetDev = 1 << 14,     // /* R    */
            JtagBrk = 1 << 12,    // /* R/W1 */
            DStrt = 1 << 11,      // /* R/W1 */
            DeRR = 1 << 10,       // /* R    */
            DrWn = 1 << 9,        // /* R/W  */
            Dsz1 = 1 << 8,        // /* R/W  */
            Dsz0 = 1 << 7,        // /* R/W  */
            DLock = 1 << 5,       // /* R/W  */
            BrkSt = 1 << 3,       // /* R    */
            TIF = 1 << 2,         // /* W0/R */
            TOF = 1 << 1,         // /* W0/R */
            ClkEn = 1 << 0       // /* R/W  */
        }
        // Loads specific features this device using EJTAG IMP OPCODE
        private void EJTAG_LoadCapabilities(uint features)
        {
            uint e_ver = (uint)(features >> 29 & 7L);
            uint e_nodma = (uint)(features & (long)(1 << 14));
            uint e_priv = (uint)(features & (long)(1 << 28));
            uint e_dint = (uint)(features & (long)(1 << 24));
            uint e_mips = (uint)(features & 1L);
            switch (e_ver)
            {
                case 0U:
                    {
                        IMPVER = "1 and 2.0";
                        break;
                    }

                case 1U:
                    {
                        IMPVER = "2.5";
                        break;
                    }

                case 2U:
                    {
                        IMPVER = "2.6";
                        break;
                    }

                case 3U:
                    {
                        IMPVER = "3.1";
                        break;
                    }
            }

            if (e_nodma == 0L)
            {
                DMA_SUPPORTED = true;
            }
            else
            {
                DMA_SUPPORTED = false;
            }

            if (e_priv == 0L)
            {
                RK3_ENV = false;
                RK4_ENV = true;
            }
            else
            {
                RK3_ENV = true;
                RK4_ENV = false;
            }

            if (e_dint == 0L)
            {
                DINT_SUPPORT = false;
            }
            else
            {
                DINT_SUPPORT = true;
            }

            if (e_mips == 0L)
            {
                MIPS32 = true;
                MIPS64 = false;
            }
            else
            {
                MIPS32 = false;
                MIPS64 = true;
            }
        }
        // Resets the processor (EJTAG ONLY)
        public void EJTAG_Reset()
        {
            AccessDataRegister32(Devices[Chain_SelectedIndex].BSDL.MIPS_CONTROL, (uint)(EJTAG_CTRL.PrRst | EJTAG_CTRL.PerRst));
        }

        public bool EJTAG_Debug_Enable()
        {
            try
            {
                uint debug_flag = (uint)(EJTAG_CTRL.PrAcc | EJTAG_CTRL.ProbEn | EJTAG_CTRL.SetDev | EJTAG_CTRL.JtagBrk);
                uint ctrl_reg = AccessDataRegister32(Devices[Chain_SelectedIndex].BSDL.MIPS_CONTROL, debug_flag);
                if (Conversions.ToBoolean(AccessDataRegister32(Devices[Chain_SelectedIndex].BSDL.MIPS_CONTROL, (uint)(EJTAG_CTRL.PrAcc | EJTAG_CTRL.ProbEn | EJTAG_CTRL.SetDev)) & (uint)EJTAG_CTRL.BrkSt))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
            }

            return false;
        }

        public void EJTAG_Debug_Disable()
        {
            try
            {
                uint flag = (uint)(EJTAG_CTRL.ProbEn | EJTAG_CTRL.SetDev); // This clears the JTAGBRK bit
                AccessDataRegister32(Devices[Chain_SelectedIndex].BSDL.MIPS_CONTROL, flag);
            }
            catch
            {
            }
        }

        private const ushort MAX_USB_BUFFER_SIZE = 2048; // Max number of bytes we should send via USB bulk endpoints
        // Target device needs to support DMA and FCUSB needs to include flashmode
        public void DMA_WriteFlash(uint dma_addr, byte[] data_to_write, CFI.CFI_FLASH_MODE prog_mode)
        {
            bool data_blank = true;
            for (int i = 0, loopTo = data_to_write.Length - 1; i <= loopTo; i++)
            {
                if (!(data_to_write[i] == 255))
                {
                    data_blank = false;
                    break;
                }
            }

            if (data_blank)
                return; // Sector is already blank, no need to write data
            uint BytesWritten = 0U;
            try
            {
                uint BufferIndex = 0U;
                uint BytesLeft = (uint)data_to_write.Length;
                uint Counter = 0U;
                while (BytesLeft != 0L)
                {
                    if (BytesLeft > MAX_USB_BUFFER_SIZE)
                    {
                        var Packet = new byte[2048];
                        Array.Copy(data_to_write, BufferIndex, Packet, 0L, Packet.Length);
                        DMA_WriteFlash_Block(dma_addr, Packet, prog_mode);
                        dma_addr = dma_addr + MAX_USB_BUFFER_SIZE;
                        BufferIndex = BufferIndex + MAX_USB_BUFFER_SIZE;
                        BytesLeft = BytesLeft - MAX_USB_BUFFER_SIZE;
                        BytesWritten += MAX_USB_BUFFER_SIZE;
                    }
                    else
                    {
                        var Packet = new byte[(int)(BytesLeft - 1L + 1)];
                        Array.Copy(data_to_write, BufferIndex, Packet, 0L, Packet.Length);
                        DMA_WriteFlash_Block(dma_addr, Packet, prog_mode);
                        BytesLeft = 0U;
                    }

                    Counter = (uint)(Counter + 1L);
                }
            }
            catch
            {
            }
        }

        private bool DMA_WriteFlash_Block(uint dma_addr, byte[] data, CFI.CFI_FLASH_MODE sub_cmd)
        {
            var setup_data = new byte[8];
            uint data_count = (uint)data.Length;
            setup_data[0] = (byte)(dma_addr & 255L);
            setup_data[1] = (byte)(dma_addr >> 8 & 255L);
            setup_data[2] = (byte)(dma_addr >> 16 & 255L);
            setup_data[3] = (byte)(dma_addr >> 24 & 255L);
            setup_data[4] = (byte)(data_count & 255L);
            setup_data[5] = (byte)(data_count >> 8 & 255L);
            setup_data[6] = (byte)(data_count >> 16 & 255L);
            setup_data[7] = (byte)(data_count >> 24 & 255L);
            if (!FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data, (uint)setup_data.Length))
                return default;
            if (!FCUSB.USB_CONTROL_MSG_OUT((USB.USBREQ)sub_cmd))
                return default;
            bool write_result = FCUSB.USB_BULK_OUT(data);
            if (write_result)
                FCUSB.USB_WaitForComplete();
            return write_result;
        }

        public uint ReadMemory(uint addr, DATA_WIDTH width)
        {
            var ReadBack = new byte[4];
            switch (Devices[Chain_SelectedIndex].ACCESS)
            {
                case JTAG_MEM_ACCESS.EJTAG_DMA:
                    {
                        switch (width)
                        {
                            case DATA_WIDTH.Word:
                                {
                                    FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_READ_W, ref ReadBack, addr);
                                    Array.Reverse(ReadBack);
                                    return Utilities.Bytes.ToUInt32(ReadBack);
                                }

                            case DATA_WIDTH.HalfWord:
                                {
                                    FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_READ_H, ref ReadBack, addr);
                                    Array.Reverse(ReadBack);
                                    return (uint)((ReadBack[0] << 8) + ReadBack[1]);
                                }

                            case DATA_WIDTH.Byte:
                                {
                                    if (addr % 2L == 0L)
                                    {
                                        FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_READ_B, ref ReadBack, addr);
                                    }
                                    else
                                    {
                                        FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_READ_H, ref ReadBack, addr);
                                    }

                                    Array.Reverse(ReadBack);
                                    return (byte)(ReadBack[0] & 0xFF);
                                }
                        }

                        break;
                    }

                case JTAG_MEM_ACCESS.EJTAG_PRACC:
                    {
                        break;
                    }

                case JTAG_MEM_ACCESS.ARM:
                    {
                        FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_READ_W, ref ReadBack, addr);
                        Array.Reverse(ReadBack);
                        return Utilities.Bytes.ToUInt32(ReadBack);
                    }
            }

            return 0U;
        }

        public void WriteMemory(uint addr, uint data, DATA_WIDTH width)
        {
            ushort bits = 32;
            if (width == DATA_WIDTH.Byte)
            {
                data = data << 24 | data << 16 | data << 8 | data;
                bits = 8;
            }
            else if (width == DATA_WIDTH.HalfWord)
            {
                data = data << 16 | data;
                bits = 16;
            }

            var setup_data = new byte[9];
            setup_data[0] = (byte)(addr & 255L);
            setup_data[1] = (byte)(addr >> 8 & 255L);
            setup_data[2] = (byte)(addr >> 16 & 255L);
            setup_data[3] = (byte)(addr >> 24 & 255L);
            setup_data[4] = (byte)(data & 255L);
            setup_data[5] = (byte)(data >> 8 & 255L);
            setup_data[6] = (byte)(data >> 16 & 255L);
            setup_data[7] = (byte)(data >> 24 & 255L);
            setup_data[8] = (byte)Devices[Chain_SelectedIndex].ACCESS;
            if (!FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data, (uint)setup_data.Length))
                return;
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_WRITE, null, bits);
        }
        // Reads data from DRAM (optomized for speed)
        public byte[] ReadMemory(uint Address, uint count)
        {
            uint DramStart = Address;
            int LargeCount = (int)count; // The total amount of data we need to read in
            while (!(Address % 4L == 0L | Address == 0L))
            {
                Address = (uint)(Address - 1L);
                LargeCount = LargeCount + 1;
            }

            while (LargeCount % 4 != 0)
                LargeCount = LargeCount + 1; // Now StartAdd2 and ByteLen2 are on bounds of 4
            var TotalBuffer = new byte[LargeCount];
            int BytesLeft = LargeCount;
            while (BytesLeft > 0)
            {
                int BytesToRead = BytesLeft;
                if (BytesToRead > MAX_USB_BUFFER_SIZE)
                    BytesToRead = MAX_USB_BUFFER_SIZE;
                uint Offset = (uint)(LargeCount - BytesLeft);
                byte[] TempBuffer = null;
                switch (Devices[Chain_SelectedIndex].ACCESS)
                {
                    case JTAG_MEM_ACCESS.EJTAG_DMA:
                        {
                            TempBuffer = JTAG_ReadMemory(Address + Offset, (uint)BytesToRead);
                            break;
                        }

                    case JTAG_MEM_ACCESS.EJTAG_PRACC:
                        {
                            break;
                        }

                    case JTAG_MEM_ACCESS.ARM:
                        {
                            break;
                        }
                }

                if (TempBuffer is null || !(TempBuffer.Length == BytesToRead))
                {
                    Array.Resize(ref TempBuffer, BytesToRead); // Fill buffer with blank data
                }

                Array.Copy(TempBuffer, 0L, TotalBuffer, Offset, TempBuffer.Length);
                BytesLeft -= BytesToRead;
            }

            var OutByte = new byte[(int)(count - 1L + 1)];
            Array.Copy(TotalBuffer, LargeCount - count, OutByte, 0L, count);
            return OutByte;
        }
        // Writes an unspecified amount of b() into memory (usually DRAM)
        public bool WriteMemory(uint Address, byte[] data)
        {
            try
            {
                int TotalBytes = data.Length;
                int WordBytes = (int)(Math.Floor(TotalBytes / 4d) * 4d);
                var DataToWrite = new byte[WordBytes]; // Word aligned
                Array.Copy(data, DataToWrite, WordBytes);
                int BytesRemaining = DataToWrite.Length;
                int BytesWritten = 0;
                byte[] BlockToWrite;
                while (BytesRemaining > 0)
                {
                    if (BytesRemaining > MAX_USB_BUFFER_SIZE)
                    {
                        BlockToWrite = new byte[2048];
                    }
                    else
                    {
                        BlockToWrite = new byte[BytesRemaining];
                    }

                    Array.Copy(DataToWrite, BytesWritten, BlockToWrite, 0, BlockToWrite.Length);
                    switch (Devices[Chain_SelectedIndex].ACCESS)
                    {
                        case JTAG_MEM_ACCESS.EJTAG_DMA:
                            {
                                JTAG_WriteMemory(Address, BlockToWrite);
                                break;
                            }

                        case JTAG_MEM_ACCESS.EJTAG_PRACC:
                            {
                                break;
                            }

                        case JTAG_MEM_ACCESS.ARM:
                            {
                                break;
                            }
                    }

                    BytesWritten += BlockToWrite.Length;
                    BytesRemaining -= BlockToWrite.Length;
                    Address += (uint)BlockToWrite.Length;
                }

                if (!(TotalBytes == WordBytes)) // Writes the bytes left over is less than 4
                {
                    for (int i = 0, loopTo = TotalBytes - WordBytes - 1; i <= loopTo; i++)
                        WriteMemory((uint)(Address + WordBytes + i), data[WordBytes + i], DATA_WIDTH.Byte);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool JTAG_WriteMemory(uint dma_addr, byte[] data_out)
        {
            var setup_data = new byte[9];
            uint data_len = (uint)data_out.Length;
            setup_data[0] = (byte)(dma_addr & 255L);
            setup_data[1] = (byte)(dma_addr >> 8 & 255L);
            setup_data[2] = (byte)(dma_addr >> 16 & 255L);
            setup_data[3] = (byte)(dma_addr >> 24 & 255L);
            setup_data[4] = (byte)(data_len & 255L);
            setup_data[5] = (byte)(data_len >> 8 & 255L);
            setup_data[6] = (byte)(data_len >> 16 & 255L);
            setup_data[7] = (byte)(data_len >> 24 & 255L);
            setup_data[8] = (byte)Devices[Chain_SelectedIndex].ACCESS;
            if (!FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data, (uint)setup_data.Length))
                return default;
            if (!FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_WRITEMEM))
                return default; // Sends setup command and data
            return FCUSB.USB_BULK_OUT(data_out);
        }

        private byte[] JTAG_ReadMemory(uint dma_addr, uint count)
        {
            var setup_data = new byte[9];
            setup_data[0] = (byte)(dma_addr & 255L);
            setup_data[1] = (byte)(dma_addr >> 8 & 255L);
            setup_data[2] = (byte)(dma_addr >> 16 & 255L);
            setup_data[3] = (byte)(dma_addr >> 24 & 255L);
            setup_data[4] = (byte)(count & 255L);
            setup_data[5] = (byte)(count >> 8 & 255L);
            setup_data[6] = (byte)(count >> 16 & 255L);
            setup_data[7] = (byte)(count >> 24 & 255L);
            setup_data[8] = (byte)Devices[Chain_SelectedIndex].ACCESS;
            var data_back = new byte[(int)(count - 1L + 1)];
            if (!FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data, (uint)setup_data.Length))
                return null;
            if (!FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_READMEM))
                return null; // Sends setup command and data
            if (!FCUSB.USB_BULK_IN(ref data_back))
                return null;
            return data_back;
        }

        #endregion

#region "CFI Plugin"

        private FLASH_INTERFACE _CFI_IF; // Handles all of the CFI flash protocol

        private FLASH_INTERFACE CFI_IF
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _CFI_IF;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_CFI_IF != null)
                {
                    _CFI_IF.Memory_Write_B -= CFIEVENT_WriteB;
                    _CFI_IF.Memory_Write_H -= CFIEVENT_WriteH;
                    _CFI_IF.Memory_Write_W -= CFIEVENT_WriteW;
                    _CFI_IF.Memory_Read_B -= CFIEVENT_ReadB;
                    _CFI_IF.Memory_Read_H -= CFIEVENT_ReadH;
                    _CFI_IF.Memory_Read_W -= CFIEVENT_ReadW;
                    _CFI_IF.SetBaseAddress -= CFIEVENT_SetBase;
                    _CFI_IF.ReadFlash -= CFIEVENT_ReadFlash;
                    _CFI_IF.WriteFlash -= CFIEVENT_WriteFlash;
                    _CFI_IF.WriteConsole -= CFIEVENT_WriteConsole;
                }
                _CFI_IF = value;
                if (_CFI_IF != null)
                {
                    _CFI_IF.Memory_Write_B += CFIEVENT_WriteB;
                    _CFI_IF.Memory_Write_H += CFIEVENT_WriteH;
                    _CFI_IF.Memory_Write_W += CFIEVENT_WriteW;
                    _CFI_IF.Memory_Read_B += CFIEVENT_ReadB;
                    _CFI_IF.Memory_Read_H += CFIEVENT_ReadH;
                    _CFI_IF.Memory_Read_W += CFIEVENT_ReadW;
                    _CFI_IF.SetBaseAddress += CFIEVENT_SetBase;
                    _CFI_IF.ReadFlash += CFIEVENT_ReadFlash;
                    _CFI_IF.WriteFlash += CFIEVENT_WriteFlash;
                    _CFI_IF.WriteConsole += CFIEVENT_WriteConsole;
                }
            }
        }

        public bool CFI_Detect(uint DMA_ADDR)
        {
            return CFI_IF.DetectFlash(DMA_ADDR);
        }

        public void CFI_ReadMode()
        {
            CFI_IF.Read_Mode();
        }

        public void CFI_EraseDevice()
        {
            CFI_IF.EraseBulk();
        }

        public void CFI_WaitUntilReady()
        {
            CFI_IF.WaitUntilReady();
        }

        public uint CFI_SectorCount()
        {
            return (uint)CFI_IF.GetFlashSectors();
        }

        public uint CFI_GetSectorSize(uint sector_ind)
        {
            return (uint)CFI_IF.GetSectorSize((int)sector_ind);
        }

        public uint CFI_FindSectorBase(uint sector_ind)
        {
            return CFI_IF.FindSectorBase(sector_ind);
        }

        public bool CFI_Sector_Erase(uint sector_ind)
        {
            CFI_IF.Sector_Erase((int)sector_ind);
            return true;
        }

        public byte[] CFI_ReadFlash(uint Address, uint count)
        {
            return CFI_IF.ReadData(Address, count);
        }

        public bool CFI_SectorWrite(uint Address, byte[] data_out)
        {
            CFI_IF.WriteSector((int)Address, data_out);
            return true;
        }

        public string CFI_GetFlashName()
        {
            return CFI_IF.FlashName;
        }

        public uint CFI_GetFlashSize()
        {
            return CFI_IF.FlashSize;
        }

        public void CFI_GetFlashPart(ref byte MFG, ref uint CHIPID)
        {
            MFG = CFI_IF.MyDeviceID.MFG;
            CHIPID = (uint)(CFI_IF.MyDeviceID.ID1 << 16 | CFI_IF.MyDeviceID.ID2);
        }

        private void CFIEVENT_WriteB(uint addr, byte data)
        {
            WriteMemory(addr, data, DATA_WIDTH.Byte);
        }

        private void CFIEVENT_WriteH(uint addr, ushort data)
        {
            WriteMemory(addr, data, DATA_WIDTH.HalfWord);
        }

        private void CFIEVENT_WriteW(uint addr, uint data)
        {
            WriteMemory(addr, data, DATA_WIDTH.Word);
        }

        private void CFIEVENT_ReadB(uint addr, ref byte data)
        {
            data = (byte)ReadMemory(addr, DATA_WIDTH.Byte);
        }

        private void CFIEVENT_ReadH(uint addr, ref ushort data)
        {
            data = (ushort)ReadMemory(addr, DATA_WIDTH.HalfWord);
        }

        private void CFIEVENT_ReadW(uint addr, ref uint data)
        {
            data = ReadMemory(addr, DATA_WIDTH.Word);
        }

        private void CFIEVENT_SetBase(uint @base)
        {
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SET_OPTION, BitConverter.GetBytes(@base), 5U);
        }

        private void CFIEVENT_ReadFlash(uint dma_addr, ref byte[] data)
        {
            data = JTAG_ReadMemory(dma_addr, (uint)data.Length);
        }

        private void CFIEVENT_WriteFlash(uint dma_addr, byte[] data_to_write, CFI.CFI_FLASH_MODE prog_mode)
        {
            DMA_WriteFlash(dma_addr, data_to_write, prog_mode);
        }

        private void CFIEVENT_WriteConsole(string message)
        {
            MainApp.PrintConsole(message);
        }

        #endregion

#region "SPI over JTAG"

        public SPI_NOR SPI_Part; // Contains the SPI Flash definition
        private JTAG_SPI_Type SPI_JTAG_PROTOCOL;
        private MIPS_SPI_API SPI_MIPS_JTAG_IF;
        // Returns TRUE if the JTAG can detect a connected flash to the SPI port
        public bool SPI_Detect(JTAG_SPI_Type spi_if)
        {
            SPI_JTAG_PROTOCOL = spi_if;
            if (spi_if == JTAG_SPI_Type.ATH_MIPS | spi_if == JTAG_SPI_Type.BCM_MIPS)
            {
                SPI_MIPS_JTAG_IF = new MIPS_SPI_API(SPI_JTAG_PROTOCOL);
                uint reg = SPI_SendCommand(SPI_MIPS_JTAG_IF.READ_ID, 1U, 3U);
                MainApp.PrintConsole(string.Format("JTAG: SPI register returned {0}", "0x" + Conversion.Hex(reg)));
                if (reg == 0L || reg == 0xFFFFFFFFU)
                {
                    return false;
                }
                else
                {
                    byte MFG_BYTE = (byte)((reg & 0xFF0000L) >> 16);
                    ushort PART_ID = (ushort)(reg & 0xFFFFL);
                    SPI_Part = (SPI_NOR)MainApp.FlashDatabase.FindDevice(MFG_BYTE, PART_ID, 0, MemoryType.SERIAL_NOR);
                    if (SPI_Part is object)
                    {
                        MainApp.PrintConsole(string.Format("JTAG: SPI flash detected ({0})", SPI_Part.NAME));
                        if (SPI_JTAG_PROTOCOL == JTAG_SPI_Type.BCM_MIPS)
                        {
                            WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, 0U, DATA_WIDTH.Word);
                            var data = SPI_ReadFlash(0U, 4U);
                        }

                        return true;
                    }
                    else
                    {
                        MainApp.PrintConsole("JTAG: SPI flash not found in database");
                    }
                }
            }
            else if (spi_if == JTAG_SPI_Type.BCM_ARM)
            {
                if (!(FCUSB.HWBOARD == USB.FCUSB_BOARD.Professional_PCB5))
                {
                    MainApp.PrintConsole("JTAG: Error, ARM extension is only supported on FlashcatUSB Professional");
                    return false;
                }
            }
            return false;
        }
        // Includes chip-specific API for connecting JTAG->SPI
        private class MIPS_SPI_API
        {
            public uint BASE { get; set; }
            // Register addrs
            public uint REG_CNTR { get; set; }
            public uint REG_DATA { get; set; }
            public uint REG_OPCODE { get; set; }
            // Control CODES
            public uint CTL_Start { get; set; }
            public uint CTL_Busy { get; set; }
            // OP CODES
            public ushort READ_ID { get; set; } // 16 bits
            public ushort WREN { get; set; }
            public ushort SECTORERASE { get; set; }
            public ushort RD_STATUS { get; set; }
            public ushort PAGEPRG { get; set; }

            public MIPS_SPI_API(JTAG_SPI_Type spi_type)
            {
                BASE = 0x1FC00000U;
                switch (spi_type)
                {
                    case JTAG_SPI_Type.BCM_MIPS:
                        {
                            REG_CNTR = 0x18000040U;
                            REG_OPCODE = 0x18000044U;
                            REG_DATA = 0x18000048U;
                            CTL_Start = 0x80000000U;
                            CTL_Busy = 0x80000000U;
                            READ_ID = 0x49F;
                            WREN = 0x6;
                            SECTORERASE = 0x2D8;
                            RD_STATUS = 0x105;
                            PAGEPRG = 0x402;
                            break;
                        }

                    case JTAG_SPI_Type.BCM_ARM:
                        {
                            REG_CNTR = 0x11300000U;
                            REG_OPCODE = 0x11300004U;
                            REG_DATA = 0x11300008U;
                            CTL_Start = 0x11300100U;
                            CTL_Busy = 0x11310000U;
                            READ_ID = 0x9F;
                            WREN = 0x6;
                            SECTORERASE = 0xD8;
                            RD_STATUS = 0x5;
                            PAGEPRG = 0x2;
                            break;
                        }
                }
            }
        }

        public bool SPI_EraseBulk()
        {
            try
            {
                MainApp.PrintConsole("Erasing entire SPI flash (this could take a moment)");
                WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, 0U, DATA_WIDTH.Word); // Might need to be for Ath too
                SPI_SendCommand(SPI_MIPS_JTAG_IF.WREN, 1U, 0U);
                WriteMemory(SPI_MIPS_JTAG_IF.REG_OPCODE, SPI_MIPS_JTAG_IF.BASE, DATA_WIDTH.Word);
                WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, 0x800000C7U, DATA_WIDTH.Word); // Might need to be for Ath too
                SPI_WaitUntilReady();
                MainApp.PrintConsole("Erase operation complete!");
                return true;
            }
            catch
            {
            }

            return false;
        }

        public bool SPI_SectorErase(uint secotr_ind)
        {
            try
            {
                uint Addr24 = SPI_FindSectorBase((int)secotr_ind);
                uint reg = SPI_GetControlReg();
                if (SPI_JTAG_PROTOCOL == JTAG_SPI_Type.BCM_MIPS)
                {
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, 0U, DATA_WIDTH.Word);
                    SPI_SendCommand(SPI_MIPS_JTAG_IF.WREN, 1U, 0U);
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_OPCODE, Addr24 | SPI_MIPS_JTAG_IF.SECTORERASE, DATA_WIDTH.Word);
                    reg = (uint)(reg & 0xFFFFFF00L | SPI_MIPS_JTAG_IF.SECTORERASE | SPI_MIPS_JTAG_IF.CTL_Start);
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, reg, DATA_WIDTH.Word);
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, 0U, DATA_WIDTH.Word);
                }
                else if (SPI_JTAG_PROTOCOL == JTAG_SPI_Type.ATH_MIPS)
                {
                    SPI_SendCommand(SPI_MIPS_JTAG_IF.WREN, 1U, 0U);
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_OPCODE, Addr24 << 8 | SPI_MIPS_JTAG_IF.SECTORERASE, DATA_WIDTH.Word);
                    reg = (uint)(reg & 0xFFFFFF00L | 0x4L | SPI_MIPS_JTAG_IF.CTL_Start);
                    WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, reg, DATA_WIDTH.Word);
                }

                SPI_WaitUntilReady();
                SPI_GetControlReg();
            }
            catch
            {
            }

            return true;
        }

        public bool SPI_WriteData(uint Address, byte[] data)
        {
            try
            {
                while (data.Length % 4 != 0)
                {
                    Array.Resize(ref data, data.Length + 1);
                    data[data.Length - 1] = 255;
                }

                int TotalBytes = data.Length;
                int WordBytes = (int)(Math.Floor(TotalBytes / 4d) * 4d);
                var DataToWrite = new byte[WordBytes]; // Word aligned
                Array.Copy(data, DataToWrite, WordBytes);
                int BytesRemaining = DataToWrite.Length;
                int BytesWritten = 0;
                byte[] BlockToWrite;
                while (BytesRemaining > 0)
                {
                    if (BytesRemaining > MAX_USB_BUFFER_SIZE)
                    {
                        BlockToWrite = new byte[2048];
                    }
                    else
                    {
                        BlockToWrite = new byte[BytesRemaining];
                    }

                    Array.Copy(DataToWrite, BytesWritten, BlockToWrite, 0, BlockToWrite.Length);
                    SPI_WriteFlash(Address, BlockToWrite);
                    BytesWritten += BlockToWrite.Length;
                    BytesRemaining -= BlockToWrite.Length;
                    Address += (uint)BlockToWrite.Length;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public uint SPI_SendCommand(ushort SPI_OPCODE, uint BytesToWrite, uint BytesToRead)
        {
            WriteMemory(0xBF000000U, 0U, DATA_WIDTH.Word);
            if (SPI_JTAG_PROTOCOL == JTAG_SPI_Type.BCM_MIPS)
            {
                WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, 0U, DATA_WIDTH.Word);
            }

            uint reg = SPI_GetControlReg(); // Zero
            switch (SPI_JTAG_PROTOCOL)
            {
                case JTAG_SPI_Type.BCM_MIPS:
                    {
                        reg = reg & 0xFFFFFF00U | SPI_OPCODE | SPI_MIPS_JTAG_IF.CTL_Start;
                        break;
                    }

                case JTAG_SPI_Type.ATH_MIPS:
                    {
                        reg = reg & 0xFFFFFF00U | BytesToWrite | BytesToRead << 4 | SPI_MIPS_JTAG_IF.CTL_Start;
                        break;
                    }
            }
            WriteMemory(SPI_MIPS_JTAG_IF.REG_OPCODE, SPI_OPCODE, DATA_WIDTH.Word);
            WriteMemory(SPI_MIPS_JTAG_IF.REG_CNTR, reg, DATA_WIDTH.Word);
            reg = SPI_GetControlReg();
            if (BytesToRead > 0L)
            {
                reg = ReadMemory(SPI_MIPS_JTAG_IF.REG_DATA, DATA_WIDTH.Word);
                switch (BytesToRead)
                {
                    case 1U:
                        {
                            reg = (uint)(reg & 255L);
                            break;
                        }

                    case 2U:
                        {
                            reg = (uint)((reg & 255L) << 8 | (reg & 0xFF00L) >> 8);
                            break;
                        }

                    case 3U:
                        {
                            reg = (uint)((reg & 255L) << 16 | reg & 0xFF00L | (reg & 0xFF0000L) >> 16);
                            break;
                        }

                    case 4U:
                        {
                            reg = (uint)((reg & 255L) << 24 | (reg & 0xFF00L) << 8 | (reg & 0xFF0000L) >> 8 | (reg & 0xFF000000L) >> 24);
                            break;
                        }
                }

                return reg;
            }

            return 0U;
        }
        // Returns the total number of sectors
        public uint SPI_SectorCount()
        {
            uint secSize = 0x10000U; // 64KB
            uint totalsize = (uint)SPI_Part.FLASH_SIZE;
            return (uint)(totalsize / (double)secSize);
        }

        public bool SPI_SectorWrite(uint sector_index, byte[] data)
        {
            uint Addr = SPI_FindSectorBase((int)sector_index);
            SPI_WriteData(Addr, data);
            return true;
        }

        public uint SPI_FindSectorBase(int sectorInt)
        {
            return (uint)(SPI_GetSectorSize(0U) * sectorInt);
        }

        public uint SPI_GetSectorSize(uint sector_ind)
        {
            return 0x10000U;
        }

        public uint SPI_GetControlReg()
        {
            int i = 0;
            uint reg;
            do
            {
                if (!(i == 0))
                    Thread.Sleep(25);
                reg = ReadMemory(SPI_MIPS_JTAG_IF.REG_CNTR, DATA_WIDTH.Word);
                i = i + 1;
                if (i == 20)
                    return 0U;
            }
            while ((reg & SPI_MIPS_JTAG_IF.CTL_Busy) > 0L);
            return reg;
        }

        public void SPI_WaitUntilReady()
        {
            uint reg;
            do
                reg = SPI_SendCommand(SPI_MIPS_JTAG_IF.RD_STATUS, 1U, 4U);
            while ((reg & 1L) > 0L);
        }

        public byte[] SPI_ReadFlash(uint addr32, uint count)
        {
            return ReadMemory(SPI_MIPS_JTAG_IF.BASE + addr32, count);
        }

        private bool SPI_WriteFlash(uint addr32, byte[] data_out)
        {
            var OpCode = default(byte);
            switch (SPI_JTAG_PROTOCOL)
            {
                case JTAG_SPI_Type.BCM_MIPS:
                    {
                        OpCode = (byte)USB.USBREQ.JTAG_FLASHSPI_BRCM;
                        break;
                    }

                case JTAG_SPI_Type.ATH_MIPS:
                    {
                        OpCode = (byte)USB.USBREQ.JTAG_FLASHSPI_ATH;
                        break;
                    }
            }

            var setup_data = new byte[8];
            uint data_len = (uint)data_out.Length;
            setup_data[0] = (byte)(addr32 & 255L);
            setup_data[1] = (byte)(addr32 >> 8 & 255L);
            setup_data[2] = (byte)(addr32 >> 16 & 255L);
            setup_data[3] = (byte)(addr32 >> 24 & 255L);
            setup_data[4] = (byte)(data_len & 255L);
            setup_data[5] = (byte)(data_len >> 8 & 255L);
            setup_data[6] = (byte)(data_len >> 16 & 255L);
            setup_data[7] = (byte)(data_len >> 24 & 255L);
            if (!FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data, (uint)setup_data.Length))
                return default;
            if (!FCUSB.USB_CONTROL_MSG_OUT((USB.USBREQ)OpCode))
                return default; // Sends setup command and data
            return FCUSB.USB_BULK_OUT(data_out);
        }

        public enum JTAG_SPI_Type : int
        {
            BCM_MIPS = 1,
            ATH_MIPS = 2,
            BCM_ARM = 3
        }

        #endregion

#region "Chain Support"

        public void Chain_Select(int device_index)
        {
            byte ACCESS_TYPE = 0;
            if (!this.Chain_IsValid)
            {
                MainApp.PrintConsole("JTAG chain is not valid, unable to select device");
                return;
            }

            if (device_index < this.Devices.Count)
            {
                JTAG_DEVICE J = this.Devices[device_index];
                this.IR_LEADING = 0;
                this.IR_TRAILING = 0;
                this.DR_LEADING = 0;
                this.DR_TRAILING = 0;
                this.IR_LENGTH = J.BSDL.IR_LEN;
                ACCESS_TYPE = (byte)J.ACCESS;
                this.Chain_SelectedIndex = device_index;
                if (J.BSDL is object)
                {
                    for (int i = 0, loopTo = device_index - 1; i <= loopTo; i++)
                    {
                        this.IR_TRAILING += this.Devices[i].BSDL.IR_LEN;
                        this.DR_TRAILING += 1; // BYPASS REG is always one bit wide
                    }

                    for (int i = device_index + 1, loopTo1 = this.Devices.Count - 1; i <= loopTo1; i++)
                    {
                        this.IR_LEADING += J.BSDL.IR_LEN;
                        this.DR_LEADING += 1; // BYPASS REG is always one bit wide
                    }

                    string jtag_dev_name = GetJedecDescription(Devices[device_index].IDCODE);
                    MainApp.PrintConsole(string.Format("JTAG index {0} selected: {1} (IR length {2})", device_index, jtag_dev_name, J.BSDL.IR_LEN.ToString()));
                }
            }
            else if (device_index == this.Devices.Count) // Select all
            {
                this.Chain_SelectedIndex = device_index;
                this.IR_LEADING = 0;
                this.IR_TRAILING = 0;
                this.DR_LEADING = 0;
                this.DR_TRAILING = 0;
                this.IR_LENGTH = this.Chain_BitLength;
                MainApp.PrintConsole("JTAG selected all devices");
            }
            else
            {
                return;
            }

            var select_data = new byte[8];
            select_data[0] = this.IR_LENGTH;
            select_data[1] = this.IR_LEADING;
            select_data[2] = this.IR_TRAILING;
            select_data[3] = this.DR_LEADING;
            select_data[4] = this.DR_TRAILING;
            select_data[5] = ACCESS_TYPE;
            select_data[6] = 0; // RFU1
            select_data[7] = 0; // RFU2
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, select_data);
            bool result = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SELECT);
        }

        public void Chain_Print()
        {
            MainApp.PrintConsole("JTAG chain detected: " + this.Devices.Count + " devices");
            MainApp.PrintConsole("JTAG TDI IR chain size: " + this.Chain_BitLength + " bits");
            for (int i = 0, loopTo = this.Devices.Count - 1; i <= loopTo; i++)
            {
                string ID = "0x" + Devices[i].IDCODE.ToString("X").PadLeft(8, '0');
                if (Devices[i].BSDL is object)
                {
                    MainApp.PrintConsole("Index " + i.ToString() + ": JEDEC ID " + ID + " (" + Devices[i].BSDL.PART_NAME + ")");
                }
                else
                {
                    MainApp.PrintConsole("Index " + i.ToString() + ": JEDEC ID " + ID + " - BSDL definition not found");
                }
            }

            if (this.Devices.Count > 1)
            {
                MainApp.PrintConsole("Index " + this.Devices.Count.ToString() + ": [select all devices]");
            }
        }

        public void Chain_Clear()
        {
            this.Devices.Clear();
            MainApp.PrintConsole("JTAG chain cleared");
        }
        // Sets the BSDL definition for a given device in a JTAG chain
        public bool Chain_Set(int index, string bsdl_name)
        {
            if (index > this.Devices.Count - 1)
                return false;
            foreach (var selected_device in this.BSDL_DATABASE)
            {
                if (selected_device.PART_NAME.ToUpper().Equals(bsdl_name.ToUpper()))
                {
                    this.Devices[index].BSDL = selected_device;
                    this.Devices[index].IR_LENGTH = selected_device.IR_LEN;
                    return true;
                }
            }
            return false;
        }

        public bool Chain_Add(string bsdl_name)
        {
            foreach (var selected_device in this.BSDL_DATABASE)
            {
                if (selected_device.PART_NAME.ToUpper().Equals(bsdl_name.ToUpper()))
                {
                    var j = new JTAG_DEVICE();
                    j.IDCODE = selected_device.IDCODE;
                    j.BSDL = selected_device;
                    j.IR_LENGTH = selected_device.IR_LEN;
                    this.Devices.Add(j);
                    return true;
                }
            }

            return false;
        }
        // Returns the BSDL definition
        public BSDL_DEF Chain_Get(int index)
        {
            return this.Devices[index].BSDL;
        }

        public bool Chain_Validate()
        {
            bool ChainIsValid = true;
            int TotalSize = 0;
            for (int i = 0, loopTo = this.Devices.Count - 1; i <= loopTo; i++)
            {
                if (Devices[i].BSDL is null)
                {
                    ChainIsValid = false;
                }
                else
                {
                    TotalSize += this.Devices[i].IR_LENGTH;
                }
            }

            if (ChainIsValid && TotalSize == this.Chain_BitLength)
            {
                this.Chain_IsValid = true;
                return true;
            }
            else
            {
                this.Chain_IsValid = false;
                return false;
            }
        }

#endregion

#region "Public function"

        public void Reset_StateMachine()
        {
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_RESET);
        }

        public void TAP_GotoState(JTAG_MACHINE_STATE J_STATE)
        {
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_GOTO_STATE, null, (uint)J_STATE);
        }

        public bool Tap_Toggle(uint count, bool exit_tms)
        {
            try
            {
                uint ticks_left = count;
                while (ticks_left > 0L)
                {
                    uint toggle_count = Conversions.ToUInteger(Interaction.IIf(ticks_left <= 10000000U, ticks_left, 10000000U)); // MAX 10 million cycles
                    uint toggle_cmd = toggle_count;
                    if (exit_tms)
                        toggle_cmd = toggle_cmd | 1U << 31; // The MSB indicates STAY/LEAVE for toggles
                    bool result = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_TOGGLE, null, toggle_cmd);
                    if (!result)
                        return false;
                    uint current_hz = JTAG_GetTckInHerz();
                    int delay_ms = (int)(toggle_count / (double)current_hz * 1000d);
                    Thread.Sleep(delay_ms + 5);
                    if (FCUSB.HasLogic)
                    {
                        Thread.Sleep(100);
                        result = FCUSB.USB_WaitForComplete();
                        if (!result)
                            return false;
                    }

                    ticks_left -= toggle_count;
                    if (ticks_left > 0L)
                    {
                        Thread.CurrentThread.Join(10); // Pump a message
                    }
                }

                return true;
            }
            catch
            {
            }

            return false;
        }
        // Selects the IR and shifts data into them and exits
        public void ShiftIR(byte[] tdi_bits, ref byte[] tdo_bits, Int16 bit_count, bool exit_tms)
        {
            if (bit_count == -1)
                bit_count = IR_LENGTH;
            tdo_bits = new byte[tdi_bits.Length];
            uint cmd = Conversions.ToUInteger(Interaction.IIf(exit_tms, (1U << 16) | (UInt16)bit_count, (uint)bit_count));
            Array.Reverse(tdi_bits);
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, tdi_bits, (uint)tdi_bits.Length); // Preloads the TDI data
            FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_SHIFT_IR, ref tdo_bits, cmd);
            Array.Reverse(tdo_bits);
        }

        public void ShiftDR(byte[] tdi_bits, ref byte[] tdo_bits, ushort bit_count, bool exit_tms)
        {
            tdo_bits = new byte[tdi_bits.Length];
            uint cmd = Conversions.ToUInteger(Interaction.IIf(exit_tms, 1U << 16 | bit_count, (uint)bit_count));
            Array.Reverse(tdi_bits);
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, tdi_bits, (uint)tdi_bits.Length); // Preloads the TDI data
            FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_SHIFT_DR, ref tdo_bits, cmd);
            Array.Reverse(tdo_bits);
        }

        public void ShiftTDI(uint BitCount, byte[] tdi_bits, ref byte[] tdo_bits, bool exit_tms)
        {
            int byte_count = (int)Math.Ceiling(BitCount / 8d);
            if (tdo_bits is null)
                tdo_bits = new byte[byte_count];
            if (tdi_bits is null)
                tdi_bits = new byte[byte_count];
            int BytesLeft = byte_count;
            uint BitsLeft = BitCount;
            int ptr = 0;
            while (BytesLeft != 0)
            {
                uint packet_size = (uint)Math.Min(64, BytesLeft);
                var packet_data = new byte[(int)(packet_size - 1L + 1)];
                Array.Copy(tdi_bits, ptr, packet_data, 0, packet_data.Length);
                BytesLeft = (int)(BytesLeft - packet_size);
                uint bits_size = (uint)Math.Min(512L, BitsLeft);
                BitsLeft -= bits_size;
                if (BytesLeft == 0 && exit_tms)
                    bits_size = (uint)(bits_size | (long)(1 << 16));
                var tdo_data = new byte[(int)(packet_size - 1L + 1)];
                if (!FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, packet_data, (uint)packet_data.Length))
                    return; // Preloads the TDI/TMS data
                if (!FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_SHIFT_DATA, ref tdo_data, bits_size))
                    return;  // Shifts data
                Array.Copy(tdo_data, 0, tdo_bits, ptr, tdo_data.Length);
                ptr += packet_data.Length;
            }
        }

        public uint AccessDataRegister32(uint ir_value, uint dr_value = 0U) {
            var dr_data = Utilities.Bytes.FromUInt32(dr_value);
            Array.Reverse(dr_data);
            var tdo = new byte[4];
            TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR);
            if (IR_LEADING > 0)
                Tap_Toggle(IR_LEADING, false);
            if (IR_TRAILING > 0)
            {
                byte[] argtdo_bits = null;
                this.ShiftTDI((uint)Devices[Chain_SelectedIndex].IR_LENGTH, new[] { (byte)ir_value }, ref argtdo_bits, false);
                Tap_Toggle(IR_TRAILING, true);
            }
            else
            {
                byte[] argtdo_bits1 = null;
                this.ShiftTDI((uint)Devices[Chain_SelectedIndex].IR_LENGTH, new[] { (byte)ir_value }, ref argtdo_bits1, true);
            }

            TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR);
            if (DR_LEADING > 0)
                Tap_Toggle(DR_LEADING, false);
            if (DR_TRAILING > 0)
            {
                this.ShiftTDI(32U, dr_data, ref tdo, false);
                Tap_Toggle(DR_TRAILING, true);
            }
            else
            {
                this.ShiftTDI(32U, dr_data, ref tdo, true);
            }

            TAP_GotoState(JTAG_MACHINE_STATE.Update_DR);
            Array.Reverse(tdo);
            return Utilities.Bytes.ToUInt32(tdo);
        }

        public int GetSelected_IRLength()
        {
            return Devices[Chain_SelectedIndex].IR_LENGTH;
        }

        #endregion

#region "BSDL"

        private List<BSDL_DEF> BSDL_DATABASE = new List<BSDL_DEF>();

        private void BSDL_Init()
        {
            BSDL_DATABASE.Clear();
            BSDL_DATABASE.Add(ARM_CORTEXM7());
            BSDL_DATABASE.Add(Microsemi_A3P250_FG144());
            BSDL_DATABASE.Add(Xilinx_XC2C64A());
            BSDL_DATABASE.Add(Xilinx_XC9572XL());
            BSDL_DATABASE.Add(Xilinx_XC95144_TQ100());
            BSDL_DATABASE.Add(Xilinx_XC4013XLA_PQ160());
            BSDL_DATABASE.Add(Altera_5M160ZE64());
            BSDL_DATABASE.Add(Altera_5M570ZT144());
            BSDL_DATABASE.Add(Altera_EPM7032ST44());
            BSDL_DATABASE.Add(Altera_EPC2L20());
            BSDL_DATABASE.Add(Lattice_LCMXO2_4000HC_XFTG256());
            BSDL_DATABASE.Add(Lattice_LCMXO2_7000HC_XXTG144());
            BSDL_DATABASE.Add(Lattice_LC4032V_TQFP44());
            BSDL_DATABASE.Add(Lattice_LC4064V_TQFP44());
            BSDL_DATABASE.Add(Broadcom_BCM3348());
            BSDL_DATABASE.Add(Motorola_MC68340());
        }

        public int BSDL_Add(BSDL_DEF bsdl_obj)
        {
            BSDL_DATABASE.Add(bsdl_obj);
            int created_index = BSDL_DATABASE.Count - 1;
            MainApp.PrintConsole("BSDL databased added device '" + bsdl_obj.PART_NAME + "' to index: " + created_index.ToString());
            return created_index;
        }

        public int BSDL_Find(string bsdl_name)
        {
            for (int i = 0, loopTo = BSDL_DATABASE.Count - 1; i <= loopTo; i++)
            {
                if (BSDL_DATABASE[i].PART_NAME.Equals(bsdl_name.ToUpper()))
                    return i;
            }

            return -1;
        }

        public bool BSDL_SetParamater(int library_index, string param_name, uint param_value)
        {
            if (library_index > BSDL_DATABASE.Count - 1)
                return false;
            var dsdl = BSDL_DATABASE[library_index];
            switch (param_name.ToUpper() ?? "")
            {
                case "ID_JEDEC":
                    {
                        dsdl.ID_JEDEC = param_value;
                        break;
                    }

                case "ID_MASK":
                    {
                        dsdl.ID_MASK = param_value;
                        break;
                    }

                case "IR_LEN":
                    {
                        dsdl.IR_LEN = (byte)(param_value & 0xFFL);
                        break;
                    }

                case "BS_LEN":
                    {
                        dsdl.BS_LEN = (ushort)(param_value & 0xFFFFL);
                        break;
                    }

                case "IDCODE":
                    {
                        dsdl.IDCODE = param_value;
                        break;
                    }

                case "BYPASS":
                    {
                        dsdl.BYPASS = param_value;
                        break;
                    }

                case "INTEST":
                    {
                        dsdl.INTEST = param_value;
                        break;
                    }

                case "EXTEST":
                    {
                        dsdl.EXTEST = param_value;
                        break;
                    }

                case "SAMPLE":
                    {
                        dsdl.SAMPLE = param_value;
                        break;
                    }

                case "CLAMP":
                    {
                        dsdl.CLAMP = param_value;
                        break;
                    }

                case "HIGHZ":
                    {
                        dsdl.HIGHZ = param_value;
                        break;
                    }

                case "PRELOAD":
                    {
                        dsdl.PRELOAD = param_value;
                        break;
                    }

                case "USERCODE":
                    {
                        dsdl.USERCODE = param_value;
                        break;
                    }

                case "DISVAL":
                    {
                        dsdl.DISVAL = Conversions.ToBoolean(param_value);
                        break;
                    }

                default:
                    {
                        return false;
                    }
            }

            return true;
        }

        public BSDL_DEF BSDL_GetDefinition(uint jedec_id)
        {
            foreach (var jtag_def in BSDL_DATABASE)
            {
                if (jtag_def.ID_JEDEC == (jedec_id & jtag_def.ID_MASK))
                    return jtag_def;
            }

            return null;
        }

        public string GetJedecDescription(uint JEDECID)
        {
            ushort PARTNU = (ushort)((JEDECID & 0xFFFF000L) >> 12);
            ushort MANUID = (ushort)((JEDECID & 0xFFEL) >> 1);
            string mfg_name = "0x" + Conversion.Hex(MANUID);
            string part_name = "0x" + Conversion.Hex(PARTNU).PadLeft(4, '0');
            switch (MANUID)
            {
                case 1:
                    {
                        mfg_name = "Spansion";
                        break;
                    }

                case 4:
                    {
                        mfg_name = "Fujitsu";
                        break;
                    }

                case 7:
                    {
                        mfg_name = "Hitachi";
                        break;
                    }

                case 9:
                    {
                        mfg_name = "Intel";
                        break;
                    }

                case 21:
                    {
                        mfg_name = "Philips";
                        break;
                    }

                case 31:
                    {
                        mfg_name = "Atmel";
                        break;
                    }

                case 32:
                    {
                        mfg_name = "ST";
                        break;
                    }

                case 33:
                    {
                        mfg_name = "Lattice";
                        break;
                    }

                case 52:
                    {
                        mfg_name = "Cypress";
                        break;
                    }

                case 53:
                    {
                        mfg_name = "DEC";
                        break;
                    }

                case 73:
                    {
                        mfg_name = "Xilinx";
                        break;
                    }

                case 110:
                    {
                        mfg_name = "Altera";
                        break;
                    }

                case 112: // 0x70
                    {
                        mfg_name = "Qualcomm";
                        break;
                    }

                case 191: // 0xBF
                    {
                        mfg_name = "Broadcom";
                        break;
                    }

                case 194:
                    {
                        mfg_name = "MXIC";
                        break;
                    }

                case 231:
                    {
                        mfg_name = "Microsemi";
                        break;
                    }

                case 239:
                    {
                        mfg_name = "Winbond";
                        break;
                    }

                case 336:
                    {
                        mfg_name = "Signetics";
                        break;
                    }

                case 571: // 0x23B
                    {
                        mfg_name = "ARM"; // ARM Ltd.
                        break;
                    }
            }

            foreach (var jtag_def in BSDL_DATABASE)
            {
                if (jtag_def.ID_JEDEC == JEDECID)
                {
                    part_name = jtag_def.PART_NAME;
                    break;
                }
            }

            return mfg_name + " " + part_name;
        }

        private BSDL_DEF ARM_CORTEXM7()
        {
            var J_DEVICE = new BSDL_DEF();
            J_DEVICE.PART_NAME = "ARM-CORTEX-M7";
            J_DEVICE.ID_JEDEC = 0xBA02477U;
            J_DEVICE.IR_LEN = 4;
            J_DEVICE.BS_LEN = 0;
            J_DEVICE.IDCODE = 0xEU; // 0b1110 - JTAG Device ID Code Register (DR width: 32)
            J_DEVICE.BYPASS = 0xFU; // 0b1111 - JTAG Bypass Register (DR width: 1)
            J_DEVICE.RESTART = 0x4; // 0b0100
            J_DEVICE.SCAN_N = 0x2; // 0b0010
            J_DEVICE.ARM_ABORT = 0x8; // b1000 - JTAG-DP Abort Register (DR width: 35)
            J_DEVICE.ARM_DPACC = 0xA; // b1010 - JTAG DP Access Register (DR width: 35)
            J_DEVICE.ARM_APACC = 0xB; // b1011 - JTAG AP Access Register (DR width: 35)
            return J_DEVICE;
        }

        private BSDL_DEF Microsemi_A3P250_FG144()
        {
            var J_DEVICE = new BSDL_DEF();
            J_DEVICE.PART_NAME = "A3P250";
            J_DEVICE.ID_JEDEC = 0x2A141CFU;
            J_DEVICE.ID_MASK = 0x6FFFFFFU;
            J_DEVICE.IR_LEN = 8;
            J_DEVICE.BS_LEN = 708;
            J_DEVICE.BYPASS = 0xFFU;
            J_DEVICE.IDCODE = 0xFU;
            J_DEVICE.EXTEST = 0U;
            J_DEVICE.SAMPLE = 1U;
            J_DEVICE.HIGHZ = 0x7U;
            J_DEVICE.CLAMP = 0x5U;
            J_DEVICE.INTEST = 0x6U;
            J_DEVICE.USERCODE = 0xEU;
            J_DEVICE.DISVAL = false;
            return J_DEVICE;
        }

        private BSDL_DEF Xilinx_XC9572XL()
        {
            var J_DEVICE = new BSDL_DEF();
            J_DEVICE.PART_NAME = "XC9572XL";
            J_DEVICE.ID_JEDEC = 0x59604093U;
            J_DEVICE.IR_LEN = 8;
            J_DEVICE.BS_LEN = 216;
            J_DEVICE.IDCODE = 0xFEU;
            J_DEVICE.BYPASS = 0xFFU;
            J_DEVICE.INTEST = 2U;
            J_DEVICE.EXTEST = 0U;
            J_DEVICE.SAMPLE = 1U;
            J_DEVICE.CLAMP = 0xFAU;
            J_DEVICE.HIGHZ = 0xFCU;
            J_DEVICE.USERCODE = 0xFDU;
            J_DEVICE.DISVAL = false;
            // "ISPEX ( 11110000)," &
            // "FBULK ( 11101101),"&
            // "FBLANK ( 11100101),"&
            // "FERASE ( 11101100),"&
            // "FPGM ( 11101010)," &
            // "FPGMI ( 11101011)," &
            // "FVFY ( 11101110)," &
            // "FVFYI ( 11101111)," &
            // "ISPEN ( 11101000)," &
            // "ISPENC ( 11101001)," &
            return J_DEVICE;
        }

        private BSDL_DEF Xilinx_XC2C64A()
        {
            var J_DEVICE = new BSDL_DEF();
            J_DEVICE.PART_NAME = "XC2C64A";
            J_DEVICE.ID_JEDEC = 0x6E58093U;
            J_DEVICE.IR_LEN = 8;
            J_DEVICE.BS_LEN = 192;
            J_DEVICE.IDCODE = 1U;
            J_DEVICE.BYPASS = 0xFFU;
            J_DEVICE.INTEST = 2U;
            J_DEVICE.EXTEST = 0U;
            J_DEVICE.SAMPLE = 3U;
            J_DEVICE.HIGHZ = 0xFCU;
            J_DEVICE.USERCODE = 0xFDU;
            J_DEVICE.DISVAL = false;
            // "ISC_ENABLE_CLAMP (11101001)," &
            // "ISC_ENABLEOTF  (11100100)," &
            // "ISC_ENABLE     (11101000)," &
            // "ISC_SRAM_READ  (11100111)," &
            // "ISC_SRAM_WRITE (11100110)," &
            // "ISC_ERASE      (11101101)," &
            // "ISC_PROGRAM    (11101010)," &
            // "ISC_READ       (11101110)," &
            // "ISC_INIT       (11110000)," &
            // "ISC_DISABLE    (11000000)," &
            // "TEST_ENABLE    (00010001)," &
            // "BULKPROG       (00010010)," &
            // "ERASE_ALL      (00010100)," &
            // "MVERIFY        (00010011)," &
            // "TEST_DISABLE   (00010101)," &
            // "ISC_NOOP       (11100000)";
            return J_DEVICE;
        }
        // Xilinx XC95144 (100-pin TQFP)
        private BSDL_DEF Xilinx_XC95144_TQ100()
        {
            var J_DEVICE = new BSDL_DEF();
            J_DEVICE.PART_NAME = "XC95144";
            J_DEVICE.ID_JEDEC = 0x9608093U;
            J_DEVICE.ID_MASK = 0xFFFFFFFU;
            J_DEVICE.IR_LEN = 8;
            J_DEVICE.BS_LEN = 432;
            J_DEVICE.BYPASS = 0xFFU;
            J_DEVICE.IDCODE = 0xFEU;
            J_DEVICE.EXTEST = 0U;
            J_DEVICE.INTEST = 2U;
            J_DEVICE.HIGHZ = 0xFCU;
            J_DEVICE.SAMPLE = 1U;
            J_DEVICE.USERCODE = 0xFDU;
            // J_DEVICE.ISPEX = &HF0
            // J_DEVICE.FERASE = &HEC
            // J_DEVICE.FBULK = &HED
            // J_DEVICE.FPGM = &HEA
            // J_DEVICE.FPGMI = &HEB
            // J_DEVICE.FVFY = &HEE
            // J_DEVICE.FVFYI = &HEF
            // J_DEVICE.ISPEN = &HE8
            J_DEVICE.DISVAL = false;
            return J_DEVICE;
        }

        private BSDL_DEF Xilinx_XC4013XLA_PQ160()
        {
            var J_DEVICE = new BSDL_DEF();
            J_DEVICE.PART_NAME = "XC4013XLA";
            J_DEVICE.ID_JEDEC = 0x218093U;
            J_DEVICE.ID_MASK = 0xFFFFFFFU;
            J_DEVICE.IR_LEN = 3;
            J_DEVICE.BS_LEN = 584;
            J_DEVICE.BYPASS = 0x7U;
            J_DEVICE.IDCODE = 0x6U;
            J_DEVICE.EXTEST = 0x0U;
            J_DEVICE.SAMPLE = 0x1U;
            // J_DEVICE.READBACK = &H4
            // J_DEVICE.CONFIGURE = &H5
            // J_DEVICE.USER2 = &H3
            // J_DEVICE.USER1 = &H2
            J_DEVICE.DISVAL = false;
            return J_DEVICE;
        }

        private BSDL_DEF Broadcom_BCM3348()
        {
            var J_DEVICE = new BSDL_DEF();
            J_DEVICE.PART_NAME = "BCM3348";
            J_DEVICE.ID_JEDEC = 0x334817FU;
            J_DEVICE.IR_LEN = 5;
            J_DEVICE.BS_LEN = 0;
            J_DEVICE.IDCODE = 0x1U; // Selects Device Identiﬁcation (ID) register
            J_DEVICE.BYPASS = 0x1FU; // Select Bypass register
            J_DEVICE.SAMPLE = 2U;
            J_DEVICE.MIPS_IMPCODE = 0x3U; // Selects Implementation register
            J_DEVICE.MIPS_ADDRESS = 0x8U; // Selects Address register
            J_DEVICE.MIPS_CONTROL = 0xAU; // Selects EJTAG Control register
            // DATA_IR = &H9 'Selects Data register
            // IR_ALL = &HB 'Selects the Address, Data and EJTAG Control registers
            // EJTAGBOOT = &HC 'Makes the processor take a debug exception after rese
            // NORMALBOOT = &HD 'Makes the processor execute the reset handler after rese
            // FASTDATA = &HE 'Selects the Data and Fastdata registers
            // EJWATCH = &H1C
            return J_DEVICE;
        }
        // 44-pin TQFP
        private BSDL_DEF Altera_5M570ZT144()
        {
            var J_DEVICE = new BSDL_DEF();
            J_DEVICE.PART_NAME = "5M570ZT144";
            J_DEVICE.ID_JEDEC = 0x20A60DDU;
            J_DEVICE.IR_LEN = 10;
            J_DEVICE.BS_LEN = 480;
            J_DEVICE.IDCODE = 6U;
            J_DEVICE.EXTEST = 0xFU;
            J_DEVICE.SAMPLE = 5U;
            J_DEVICE.BYPASS = 0x3FFU;
            J_DEVICE.CLAMP = 0xAU;
            J_DEVICE.HIGHZ = 0xBU;
            J_DEVICE.USERCODE = 0x7U;
            J_DEVICE.DISVAL = true;
            return J_DEVICE;
        }
        // 64-pin EQFP
        private BSDL_DEF Altera_5M160ZE64()
        {
            var J_DEVICE = new BSDL_DEF();
            J_DEVICE.PART_NAME = "5M160ZE64";
            J_DEVICE.ID_JEDEC = 0x20A50DDU;
            J_DEVICE.IR_LEN = 10;
            J_DEVICE.BS_LEN = 240;
            J_DEVICE.IDCODE = 6U;
            J_DEVICE.EXTEST = 0xFU;
            J_DEVICE.SAMPLE = 5U;
            J_DEVICE.BYPASS = 0x3FFU;
            J_DEVICE.CLAMP = 0xAU;
            J_DEVICE.HIGHZ = 0xBU;
            J_DEVICE.USERCODE = 7U;
            J_DEVICE.DISVAL = true;
            return J_DEVICE;
        }
        // 44-pin TQFP
        private BSDL_DEF Altera_EPM7032ST44()
        {
            var J_DEVICE = new BSDL_DEF();
            J_DEVICE.PART_NAME = "EPM7032";
            J_DEVICE.ID_JEDEC = 0x70320DDU;
            J_DEVICE.IR_LEN = 10;
            J_DEVICE.BS_LEN = 1;
            J_DEVICE.IDCODE = 0x59U;
            J_DEVICE.EXTEST = 0x3U;
            J_DEVICE.SAMPLE = 0x57U;
            J_DEVICE.BYPASS = 0x3FFU;
            return J_DEVICE;
        }
        // PLCC20
        private BSDL_DEF Altera_EPC2L20()
        {
            var J_DEVICE = new BSDL_DEF();
            J_DEVICE.PART_NAME = "EPC2L20";
            J_DEVICE.ID_JEDEC = 0x10020DDU;
            J_DEVICE.IR_LEN = 10;
            J_DEVICE.BS_LEN = 24;
            J_DEVICE.BYPASS = 0x3FFU;
            J_DEVICE.EXTEST = 0U;
            J_DEVICE.SAMPLE = 0x55U;
            J_DEVICE.IDCODE = 0x59U;
            J_DEVICE.USERCODE = 0x79U;
            J_DEVICE.DISVAL = true;
            return J_DEVICE;
        }

        private BSDL_DEF Lattice_LCMXO2_4000HC_XFTG256()
        {
            var J_DEVICE = new BSDL_DEF();
            J_DEVICE.PART_NAME = "LCMXO2_4000HC";
            J_DEVICE.ID_JEDEC = 0x12BC043U;
            J_DEVICE.IR_LEN = 8;
            J_DEVICE.BS_LEN = 552;
            J_DEVICE.IDCODE = 0xE0U; // 0b11100000
            J_DEVICE.BYPASS = 0xFFU; // 0b11111111
            J_DEVICE.CLAMP = 0x78U; // 01111000
            J_DEVICE.PRELOAD = 0x1CU; // 00011100
            J_DEVICE.SAMPLE = 0x1CU; // 00011100
            J_DEVICE.HIGHZ = 0x18U; // 00011000
            J_DEVICE.EXTEST = 0x15U; // 00010101
            J_DEVICE.USERCODE = 0U; // (11000000)
            J_DEVICE.DISVAL = true;
            // "          ISC_ENABLE		(11000110)," &
            // "    ISC_PROGRAM_DONE		(01011110)," &
            // " LSC_PROGRAM_SECPLUS		(11001111)," &
            // "ISC_PROGRAM_USERCODE		(11000010)," &
            // "ISC_PROGRAM_SECURITY		(11001110)," &
            // "         ISC_PROGRAM		(01100111)," &
            // "        LSC_ENABLE_X		(01110100)," &
            // "      ISC_DATA_SHIFT		(00001010)," &
            // "       ISC_DISCHARGE		(00010100)," &
            // "      ISC_ERASE_DONE		(00100100)," &
            // "   ISC_ADDRESS_SHIFT		(01000010)," &
            // "            ISC_READ		(10000000)," &
            // "         ISC_DISABLE		(00100110)," &
            // "           ISC_ERASE		(00001110)," &
            // "            ISC_NOOP		(00110000)," &
            return J_DEVICE;
        }
        // TQFP-144
        private BSDL_DEF Lattice_LCMXO2_7000HC_XXTG144()
        {
            var J_DEVICE = new BSDL_DEF();
            J_DEVICE.PART_NAME = "LCMXO2-7000HC";
            J_DEVICE.ID_JEDEC = 0x12BD043U;
            J_DEVICE.IR_LEN = 8;
            J_DEVICE.BS_LEN = 664;
            J_DEVICE.IDCODE = 0xE0U; // 0b11100000
            J_DEVICE.BYPASS = 0xFFU; // 0b11111111
            J_DEVICE.CLAMP = 0x78U; // 01111000
            J_DEVICE.PRELOAD = 0x1CU; // 00011100
            J_DEVICE.SAMPLE = 0x1CU; // 00011100
            J_DEVICE.HIGHZ = 0x18U; // 00011000
            J_DEVICE.EXTEST = 0x15U; // 00010101
            J_DEVICE.USERCODE = 0U; // (11000000)
            J_DEVICE.DISVAL = true;
            // "          ISC_ENABLE		(11000110)," &
            // "    ISC_PROGRAM_DONE		(01011110)," &
            // " LSC_PROGRAM_SECPLUS		(11001111)," &
            // "ISC_PROGRAM_USERCODE		(11000010)," &
            // "ISC_PROGRAM_SECURITY		(11001110)," &
            // "         ISC_PROGRAM		(01100111)," &
            // "        LSC_ENABLE_X		(01110100)," &
            // "      ISC_DATA_SHIFT		(00001010)," &
            // "       ISC_DISCHARGE		(00010100)," &
            // "      ISC_ERASE_DONE		(00100100)," &
            // "   ISC_ADDRESS_SHIFT		(01000010)," &
            // "            ISC_READ		(10000000)," &
            // "         ISC_DISABLE		(00100110)," &
            // "           ISC_ERASE		(00001110)," &
            // "            ISC_NOOP		(00110000)," &
            return J_DEVICE;
        }

        private BSDL_DEF Lattice_LC4032V_TQFP44()
        {
            var J_DEVICE = new BSDL_DEF();
            J_DEVICE.PART_NAME = "LC4032V";
            J_DEVICE.ID_JEDEC = 0x1805043U;
            J_DEVICE.IR_LEN = 8;
            J_DEVICE.BS_LEN = 68;
            J_DEVICE.IDCODE = 0x16U; // 00010110
            J_DEVICE.BYPASS = 0xFFU; // 11111111
            J_DEVICE.CLAMP = 0x20U; // 00100000
            J_DEVICE.PRELOAD = 0x1CU; // 00011100
            J_DEVICE.SAMPLE = 0x1CU; // 00011100
            J_DEVICE.HIGHZ = 0x18U; // 00011000
            J_DEVICE.EXTEST = 0U; // 00000000
            J_DEVICE.USERCODE = 0x17U; // 00010111
            J_DEVICE.DISVAL = false;
            // -- ISC instructions
            // "ISC_ENABLE                        (00010101),"&
            // "ISC_DISABLE                       (00011110),"&
            // "ISC_NOOP                          (00110000),"&
            // "ISC_ADDRESS_SHIFT                 (00000001),"&
            // "ISC_DATA_SHIFT                    (00000010),"&
            // "ISC_ERASE                         (00000011),"&
            // "ISC_DISCHARGE                     (00010100),"&
            // "ISC_PROGRAM_INCR                  (00100111),"&
            // "ISC_READ_INCR                     (00101010),"&
            // "ISC_PROGRAM_SECURITY              (00001001),"&
            // "ISC_PROGRAM_DONE                  (00101111),"&
            // "ISC_ERASE_DONE                    (00100100),"&
            // "ISC_PROGRAM_USERCODE              (00011010),"&
            // "LSC_ADDRESS_INIT                  (00100001)";
            return J_DEVICE;
        }

        private BSDL_DEF Lattice_LC4064V_TQFP44()
        {
            var J_DEVICE = new BSDL_DEF();
            J_DEVICE.PART_NAME = "LC4064V";
            J_DEVICE.ID_JEDEC = 0x1809043U;
            J_DEVICE.IR_LEN = 8;
            J_DEVICE.BS_LEN = 68;
            J_DEVICE.IDCODE = 0x16U; // 00010110
            J_DEVICE.BYPASS = 0xFFU; // 11111111
            J_DEVICE.CLAMP = 0x20U; // 00100000
            J_DEVICE.PRELOAD = 0x1CU; // 00011100
            J_DEVICE.SAMPLE = 0x1CU; // 00011100
            J_DEVICE.HIGHZ = 0x18U; // 00011000
            J_DEVICE.EXTEST = 0U; // 00000000
            J_DEVICE.USERCODE = 0x17U; // 00010111
            J_DEVICE.DISVAL = false;
            // -- ISC instructions
            // "ISC_ENABLE                        (00010101),"&
            // "ISC_DISABLE                       (00011110),"&
            // "ISC_NOOP                          (00110000),"&
            // "ISC_ADDRESS_SHIFT                 (00000001),"&
            // "ISC_DATA_SHIFT                    (00000010),"&
            // "ISC_ERASE                         (00000011),"&
            // "ISC_DISCHARGE                     (00010100),"&
            // "ISC_PROGRAM_INCR                  (00100111),"&
            // "ISC_READ_INCR                     (00101010),"&
            // "ISC_PROGRAM_SECURITY              (00001001),"&
            // "ISC_PROGRAM_DONE                  (00101111),"&
            // "ISC_ERASE_DONE                    (00100100),"&
            // "ISC_PROGRAM_USERCODE              (00011010),"&
            // "LSC_ADDRESS_INIT                  (00100001)";
            return J_DEVICE;
        }

        private BSDL_DEF Motorola_MC68340()
        {
            var J_DEVICE = new BSDL_DEF();
            J_DEVICE.PART_NAME = "MC68340";
            J_DEVICE.ID_JEDEC = 0U; // DEVICE DOES NOT SUPPORT IDCODE!
            J_DEVICE.ID_MASK = 0U;
            J_DEVICE.IR_LEN = 3;
            J_DEVICE.BS_LEN = 132;
            J_DEVICE.EXTEST = 0x0U;
            J_DEVICE.SAMPLE = 0x1U;
            J_DEVICE.HIGHZ = 0x4U;
            J_DEVICE.BYPASS = 0x7U;
            J_DEVICE.DISVAL = false;
            return J_DEVICE;
        }

#endregion

    }

    public enum JTAG_SPEED : int {
        _40MHZ = 0,  // Not supported on PCB 4.x
        _20MHZ = 1,
        _10MHZ = 2,
        _1MHZ = 3
    }

    public enum JTAG_MACHINE_STATE : byte {
        TestLogicReset = 0,
        RunTestIdle = 1,
        Select_DR = 2,
        Capture_DR = 3,
        Shift_DR = 4,
        Exit1_DR = 5,
        Pause_DR = 6,
        Exit2_DR = 7,
        Update_DR = 8,
        Select_IR = 9,
        Capture_IR = 10,
        Shift_IR = 11,
        Exit1_IR = 12,
        Pause_IR = 13,
        Exit2_IR = 14,
        Update_IR = 15
    }

    public enum PROCESSOR {
        MIPS = 1,
        ARM = 2,
        NONE = 3
    }

    public class JTAG_DEVICE {
        public uint IDCODE { get; set; } // Contains ID_CODEs
        public ushort MANUID { get; set; }
        public ushort PARTNU { get; set; }
        public short VERSION { get; set; } = 0;
        public JTAG_MEM_ACCESS ACCESS { get; set; }
        public int IR_LENGTH { get; set; } // Loaded from BSDL/single device chain/console command
        public BSDL_DEF BSDL { get; set; }
    }

    public class BSDL_DEF {
        public uint ID_JEDEC { get; set; }
        public uint ID_MASK { get; set; } = 0xFFFFFFFFU;
        public string PART_NAME { get; set; }
        public byte IR_LEN { get; set; }  // Number of bits the IR uses
        public ushort BS_LEN { get; set; } // Number of bits for PRELOAD/EXTEST
        public uint IDCODE { get; set; }
        public uint BYPASS { get; set; }
        public uint INTEST { get; set; }
        public uint EXTEST { get; set; }
        public uint SAMPLE { get; set; }
        public uint CLAMP { get; set; }
        public uint HIGHZ { get; set; }
        public uint PRELOAD { get; set; }
        public uint USERCODE { get; set; }
        public bool DISVAL { get; set; } // This is the value that will disable the control cell (false=0, true=1)
        // ARM SPECIFIC REGISTERS
        public byte ARM_ABORT { get; set; }
        public byte ARM_DPACC { get; set; }
        public byte ARM_APACC { get; set; }
        public byte SCAN_N { get; set; }
        public byte RESTART { get; set; }
        // MIPS SPECIFIC
        public uint MIPS_IMPCODE { get; set; }
        public uint MIPS_ADDRESS { get; set; }
        public uint MIPS_CONTROL { get; set; }
    }

    public enum DATA_WIDTH : byte {
        Byte = 8,
        HalfWord = 16,
        Word = 32
    }

    public enum JTAG_MEM_ACCESS : byte {
        NONE = 0,
        EJTAG_DMA = 1,
        EJTAG_PRACC = 2,
        ARM = 3
    }

}