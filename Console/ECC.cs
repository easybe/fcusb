using System;
using Microsoft.VisualBasic.CompilerServices;

namespace ECC_LIB {

    // 512 byte processing (24-bit ECC) 1-bit correctable/2-bit detection
    public class Hamming {
        public Hamming() {
        }


        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        private byte[] byte_parity_table = new byte[] { 0xFF, 0xD4, 0xD2, 0xF9, 0xCC, 0xE7, 0xE1, 0xCA, 0xCA, 0xE1, 0xE7, 0xCC, 0xF9, 0xD2, 0xD4, 0xFF, 0xB4, 0x9F, 0x99, 0xB2, 0x87, 0xAC, 0xAA, 0x81, 0x81, 0xAA, 0xAC, 0x87, 0xB2, 0x99, 0x9F, 0xB4, 0xB2, 0x99, 0x9F, 0xB4, 0x81, 0xAA, 0xAC, 0x87, 0x87, 0xAC, 0xAA, 0x81, 0xB4, 0x9F, 0x99, 0xB2, 0xF9, 0xD2, 0xD4, 0xFF, 0xCA, 0xE1, 0xE7, 0xCC, 0xCC, 0xE7, 0xE1, 0xCA, 0xFF, 0xD4, 0xD2, 0xF9, 0xAC, 0x87, 0x81, 0xAA, 0x9F, 0xB4, 0xB2, 0x99, 0x99, 0xB2, 0xB4, 0x9F, 0xAA, 0x81, 0x87, 0xAC, 0xE7, 0xCC, 0xCA, 0xE1, 0xD4, 0xFF, 0xF9, 0xD2, 0xD2, 0xF9, 0xFF, 0xD4, 0xE1, 0xCA, 0xCC, 0xE7, 0xE1, 0xCA, 0xCC, 0xE7, 0xD2, 0xF9, 0xFF, 0xD4, 0xD4, 0xFF, 0xF9, 0xD2, 0xE7, 0xCC, 0xCA, 0xE1, 0xAA, 0x81, 0x87, 0xAC, 0x99, 0xB2, 0xB4, 0x9F, 0x9F, 0xB4, 0xB2, 0x99, 0xAC, 0x87, 0x81, 0xAA, 0xAA, 0x81, 0x87, 0xAC, 0x99, 0xB2, 0xB4, 0x9F, 0x9F, 0xB4, 0xB2, 0x99, 0xAC, 0x87, 0x81, 0xAA, 0xE1, 0xCA, 0xCC, 0xE7, 0xD2, 0xF9, 0xFF, 0xD4, 0xD4, 0xFF, 0xF9, 0xD2, 0xE7, 0xCC, 0xCA, 0xE1, 0xE7, 0xCC, 0xCA, 0xE1, 0xD4, 0xFF, 0xF9, 0xD2, 0xD2, 0xF9, 0xFF, 0xD4, 0xE1, 0xCA, 0xCC, 0xE7, 0xAC, 0x87, 0x81, 0xAA, 0x9F, 0xB4, 0xB2, 0x99, 0x99, 0xB2, 0xB4, 0x9F, 0xAA, 0x81, 0x87, 0xAC, 0xF9, 0xD2, 0xD4, 0xFF, 0xCA, 0xE1, 0xE7, 0xCC, 0xCC, 0xE7, 0xE1, 0xCA, 0xFF, 0xD4, 0xD2, 0xF9, 0xB2, 0x99, 0x9F, 0xB4, 0x81, 0xAA, 0xAC, 0x87, 0x87, 0xAC, 0xAA, 0x81, 0xB4, 0x9F, 0x99, 0xB2, 0xB4, 0x9F, 0x99, 0xB2, 0x87, 0xAC, 0xAA, 0x81, 0x81, 0xAA, 0xAC, 0x87, 0xB2, 0x99, 0x9F, 0xB4, 0xFF, 0xD4, 0xD2, 0xF9, 0xCC, 0xE7, 0xE1, 0xCA, 0xCA, 0xE1, 0xE7, 0xCC, 0xF9, 0xD2, 0xD4, 0xFF };
        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        // Creates ECC (24-bit) for 512 bytes - 1 bit correctable, 2 bit detectable
        public byte[] GenerateECC(byte[] block) {
            var ecc_code = new byte[3];
            ushort word_reg;
            uint LP0, LP1, LP2, LP3, LP4 = default, LP5 = default, LP6 = default, LP7 = default, LP8 = default, LP9 = default, LP10 = default, LP11 = default, LP12 = default, LP13 = default, LP14 = default, LP15 = default, LP16 = default, LP17 = default;
            var uddata = Utilities.Bytes.ToUIntArray(block);
            for (int j = 0; j <= 127; j++) {
                uint temp = uddata[j];
                if (Conversions.ToBoolean(j & 0x1))
                    LP5 = LP5 ^ temp;
                else
                    LP4 = LP4 ^ temp;
                if (Conversions.ToBoolean(j & 0x2))
                    LP7 = LP7 ^ temp;
                else
                    LP6 = LP6 ^ temp;
                if (Conversions.ToBoolean(j & 0x4))
                    LP9 = LP9 ^ temp;
                else
                    LP8 = LP8 ^ temp;
                if (Conversions.ToBoolean(j & 0x8))
                    LP11 = LP11 ^ temp;
                else
                    LP10 = LP10 ^ temp;
                if (Conversions.ToBoolean(j & 0x10))
                    LP13 = LP13 ^ temp;
                else
                    LP12 = LP12 ^ temp;
                if (Conversions.ToBoolean(j & 0x20))
                    LP15 = LP15 ^ temp;
                else
                    LP14 = LP14 ^ temp;
                if (Conversions.ToBoolean(j & 0x40))
                    LP17 = LP17 ^ temp;
                else
                    LP16 = LP16 ^ temp;
            }

            uint reg32 = LP15 ^ LP14;
            byte byte_reg = (byte)(reg32 & 0xFFL);
            byte_reg = (byte)(byte_reg ^ (byte)(reg32 >> 8 & 0xFFL));
            byte_reg = (byte)(byte_reg ^ (byte)(reg32 >> 16 & 0xFFL));
            byte_reg = (byte)(byte_reg ^ (byte)(reg32 >> 24 & 0xFFL));
            byte_reg = byte_parity_table[byte_reg];
            word_reg = (ushort)((LP16 >> 16) ^ (ushort)(LP16 & 0xFFFFL));
            LP16 = (uint)((byte)(word_reg & 0xFF) ^ (byte)(word_reg >> 8));
            word_reg = (ushort)((LP17 >> 16) ^ (ushort)(LP17 & 0xFFFFL));
            LP17 = (uint)((byte)(word_reg & 0xFF) ^ (byte)(word_reg >> 8));
            ecc_code[2] = (byte)((byte)(byte_reg & 0xFE) << 1 | byte_parity_table[(byte)(LP16 & 255L)] & 0x1 | (byte_parity_table[(byte)(LP17 & 255L)] & 0x1) << 1);
            LP0 = (byte)((reg32 ^ reg32 >> 16) & 255L);
            LP1 = (byte)((reg32 >> 8 ^ reg32 >> 24) & 255L);
            LP2 = (byte)((reg32 ^ reg32 >> 8) & 255L);
            LP3 = (byte)((reg32 >> 16 ^ reg32 >> 24) & 255L);
            word_reg = (ushort)((LP4 >> 16) ^ (ushort)(LP4 & 0xFFFFL));
            LP4 = (uint)((byte)(word_reg & 255) ^ (byte)(word_reg >> 8));
            word_reg = (ushort)((LP5 >> 16) ^ (ushort)(LP5 & 0xFFFFL));
            LP5 = (uint)((byte)(word_reg & 255) ^ (byte)(word_reg >> 8));
            word_reg = (ushort)((LP6 >> 16) ^ (ushort)(LP6 & 0xFFFFL));
            LP6 = (uint)((byte)(word_reg & 255) ^ (byte)(word_reg >> 8));
            word_reg = (ushort)((LP7 >> 16) ^ (ushort)(LP7 & 0xFFFFL));
            LP7 = (uint)((byte)(word_reg & 255) ^ (byte)(word_reg >> 8));
            word_reg = (ushort)((LP8 >> 16) ^ (ushort)(LP8 & 0xFFFFL));
            LP8 = (uint)((byte)(word_reg & 255) ^ (byte)(word_reg >> 8));
            word_reg = (ushort)((LP9 >> 16) ^ (ushort)(LP9 & 0xFFFFL));
            LP9 = (uint)((byte)(word_reg & 255) ^ (byte)(word_reg >> 8));
            word_reg = (ushort)((LP10 >> 16) ^ (ushort)(LP10 & 0xFFFFL));
            LP10 = (uint)((byte)(word_reg & 255) ^ (byte)(word_reg >> 8));
            word_reg = (ushort)((LP11 >> 16) ^ (ushort)(LP11 & 0xFFFFL));
            LP11 = (uint)((byte)(word_reg & 255) ^ (byte)(word_reg >> 8));
            word_reg = (ushort)((LP12 >> 16) ^ (ushort)(LP12 & 0xFFFFL));
            LP12 = (uint)((byte)(word_reg & 255) ^ (byte)(word_reg >> 8));
            word_reg = (ushort)((LP13 >> 16) ^ (ushort)(LP13 & 0xFFFFL));
            LP13 = (uint)((byte)(word_reg & 255) ^ (byte)(word_reg >> 8));
            word_reg = (ushort)((LP14 >> 16) ^ (ushort)(LP14 & 0xFFFFL));
            LP14 = (uint)((byte)(word_reg & 255) ^ (byte)(word_reg >> 8));
            word_reg = (ushort)((LP15 >> 16) ^ (ushort)(LP15 & 0xFFFFL));
            LP15 = (uint)((byte)(word_reg & 255) ^ (byte)(word_reg >> 8));
            ecc_code[0] = (byte)(byte_parity_table[(byte)(LP0 & 255L)] & 0x1 | (byte_parity_table[(byte)(LP1 & 255L)] & 0x1) << 1 | (byte_parity_table[(byte)(LP2 & 255L)] & 0x1) << 2 | (byte_parity_table[(byte)(LP3 & 255L)] & 0x1) << 3 | (byte_parity_table[(byte)(LP4 & 255L)] & 0x1) << 4 | (byte_parity_table[(byte)(LP5 & 255L)] & 0x1) << 5 | (byte_parity_table[(byte)(LP6 & 255L)] & 0x1) << 6 | (byte_parity_table[(byte)(LP7 & 255L)] & 0x1) << 7);
            ecc_code[1] = (byte)(byte_parity_table[(byte)(LP8 & 255L)] & 0x1 | (byte_parity_table[(byte)(LP9 & 255L)] & 0x1) << 1 | (byte_parity_table[(byte)(LP10 & 255L)] & 0x1) << 2 | (byte_parity_table[(byte)(LP11 & 255L)] & 0x1) << 3 | (byte_parity_table[(byte)(LP12 & 255L)] & 0x1) << 4 | (byte_parity_table[(byte)(LP13 & 255L)] & 0x1) << 5 | (byte_parity_table[(byte)(LP14 & 255L)] & 0x1) << 6 | (byte_parity_table[(byte)(LP15 & 255L)] & 0x1) << 7);
            return ecc_code;
        }
        // Processed a block of 512 bytes with 24-bit ecc code word data, corrects if needed
        public ECC_DECODE_RESULT ProcessECC(byte[] block, ref byte[] stored_ecc) {
            var new_ecc = GenerateECC(block);
            var ecc_xor = new byte[3];
            ecc_xor[0] = (byte)(new_ecc[0] ^ stored_ecc[0]);
            ecc_xor[1] = (byte)(new_ecc[1] ^ stored_ecc[1]);
            ecc_xor[2] = (byte)(new_ecc[2] ^ stored_ecc[2]);
            if ((ecc_xor[0] | ecc_xor[1] | ecc_xor[2]) == 0) {
                return ECC_DECODE_RESULT.NoErrors;
            } else {
                int bit_count = BitCount(ecc_xor); // Counts the bit number
                if (bit_count == 12) {
                    byte bit_addr = (byte)(ecc_xor[2] >> 3 & 1 | ecc_xor[2] >> 4 & 2 | ecc_xor[2] >> 5 & 4);
                    uint byte_addr = (uint)(ecc_xor[0] >> 1 & 0x1 | ecc_xor[0] >> 2 & 0x2 | ecc_xor[0] >> 3 & 0x4 | ecc_xor[0] >> 4 & 0x8 | ecc_xor[1] << 3 & 0x10 | ecc_xor[1] << 2 & 0x20 | ecc_xor[1] << 1 & 0x40 | ecc_xor[1] & 0x80 | ecc_xor[2] << 7 & 0x100);
                    block[(int)(byte_addr - 1L)] = (byte)(block[(int)(byte_addr - 1L)] ^ 1 << bit_addr);
                    return ECC_DECODE_RESULT.Correctable;
                } else if (bit_count == 1) {
                    stored_ecc = new_ecc;
                    return ECC_DECODE_RESULT.EccError;
                } else {
                    return ECC_DECODE_RESULT.Uncorractable;
                }
            }
        }

        private int BitCount(byte[] data) {
            var counter = default(int);
            for (int i = 0, loopTo = data.Length - 1; i <= loopTo; i++) {
                byte temp = data[i];
                while (temp > 0) {
                    if ((temp & 1) == 1)
                        counter += 1;
                    temp = (byte)(temp >> 1);
                }
            }

            return counter;
        }
    }

    public class RS_ECC {
        public int PARITY_BITS { get; set; } // RS_CONST_T (Number of symbols to correct)
        public int SYM_WIDTH { get; set; } // RS_SYM_W (Number of bits per symbol)

        public RS_ECC() {
        }

        public int GetEccSize() {
            if (SYM_WIDTH == 9) {
                switch (PARITY_BITS) {
                    case 1: {
                            return 3;
                        }

                    case 2: {
                            return 5;
                        }

                    case 4: {
                            return 9;
                        }

                    case 8: {
                            return 18;
                        }

                    case 10: {
                            return 23;
                        }

                    case 14: {
                            return 32;
                        }
                }
            } else if (SYM_WIDTH == 10) {
                switch (PARITY_BITS) {
                    case 1: {
                            return 3;
                        }

                    case 2: {
                            return 5;
                        }

                    case 4: {
                            return 10;
                        }

                    case 8: {
                            return 20;
                        }

                    case 10: {
                            return 25;
                        }

                    case 14: {
                            return 35;
                        }
                }
            }

            return -1;
        }

        public byte[] GenerateECC(byte[] block) {
            //var sym_data = bytes_to_symbols(block, SYM_WIDTH);
            //using (var RS = new ECC.ReedSolomon(SYM_WIDTH, GetPolynomial(SYM_WIDTH), 0, 2, PARITY_BITS * 2)) {
            //    var Symbols = bytes_to_symbols(block, SYM_WIDTH);
            //    var result = RS.Encode(Symbols);
            //    var ecc_data = symbols_to_bytes(result, SYM_WIDTH);
            //    return ecc_data;
            //}
            return null;
        }

        public ECC_DECODE_RESULT ProcessECC(ref byte[] block, ref byte[] stored_ecc) {
            //var sym_data = bytes_to_symbols(block, SYM_WIDTH);
            //using (var RS = new ECC.ReedSolomon(SYM_WIDTH, GetPolynomial(SYM_WIDTH), 0, 2, PARITY_BITS * 2)) {
            //    var Symbols = bytes_to_symbols(block, SYM_WIDTH);
            //    var EccData = bytes_to_symbols(stored_ecc, SYM_WIDTH);
            //    var cmp_result = RS.Decode(Symbols, EccData);
            //    switch (cmp_result) {
            //        case ECC.CompareResult.NoError: {
            //                return ECC_DECODE_RESULT.NoErrors;
            //            }

            //        case ECC.CompareResult.EccError: {
            //                int org_size = stored_ecc.Length;
            //                stored_ecc = symbols_to_bytes(EccData, SYM_WIDTH);
            //                Array.Resize(ref stored_ecc, org_size);
            //                return ECC_DECODE_RESULT.EccError;
            //            }

            //        case ECC.CompareResult.Correctable: {
            //                int org_size = block.Length;
            //                block = symbols_to_bytes(Symbols, SYM_WIDTH);
            //                Array.Resize(ref block, org_size);
            //                return ECC_DECODE_RESULT.Correctable;
            //            }

            //        default: {
            //                return ECC_DECODE_RESULT.Uncorractable;
            //            }
            //    }
            //}
            return ECC_DECODE_RESULT.NoErrors;
        }

        private int GetPolynomial(int sym_size) {
            switch (sym_size) {
                case 0: { // dont care
                        break;
                    }

                case 1: { // dont care
                        break;
                    }

                case 2: { // 2-nd: poly = x^2 + x + 1
                        return 0x7;
                    }

                case 3: { // 3-rd: poly = x^3 + x + 1
                        return 0xB;
                    }

                case 4: {  // 4-th: poly = x^4 + x + 1
                        return 0x13;
                    }

                case 5: { // 5-th: poly = x^5 + x^2 + 1
                        return 0x25;
                    }

                case 6: { // 6-th: poly = x^6 + x + 1
                        return 0x43;
                    }

                case 7: { // 7-th: poly = x^7 + x^3 + 1
                        return 0x89;
                    }

                case 8: { // 8-th: poly = x^8 + x^4 + x^3 + x^2 + 1
                        return 0x11D;
                    }

                case 9: { // 9-th: poly = x^9 + x^4 + 1 
                        return 0x211;
                    }

                case 10: {  // 10-th: poly = x^10 + x^3 + 1
                        return 0x409;
                    }

                case 11: { // 11-th: poly = x^11 + x^2 + 1
                        return 0x805;
                    }

                case 12: { // 12-th: poly = x^12 + x^6 + x^4 + x + 1
                        return 0x1053;
                    }

                case 13: { // 13-th: poly = x^13 + x^4 + x^3 + x + 1
                        return 0x201B;
                    }

                case 14: { // 14-th: poly = x^14 + x^10 + x^6 + x + 1
                        return 0x4443;
                    }

                case 15: { // 15-th: poly = x^15 + x + 1
                        return 0x8003;
                    }

                case 16: { // 16-th: poly = x^16 + x^12 + x^3 + x + 1
                        return 0x1100B;
                    }
            }

            return -1;
        }
        // Converts data stored in 32-bit to byte by the specified symbol width
        private byte[] symbols_to_bytes(int[] data_in, int sym_width) {
            int ecc_size = (int)Math.Ceiling(data_in.Length * sym_width / 8d);
            var data_out = new byte[ecc_size]; // This needs to be 0x00
            int counter = 0;
            byte spare_data = 0;
            int bits_left = 0;
            foreach (int item in data_in) {
                do {
                    int next_bits = 8 - bits_left;
                    bits_left = sym_width - next_bits;
                    byte byte_to_add = (byte)(spare_data << next_bits | item >> bits_left & (1 << next_bits) - 1);
                    data_out[counter] = byte_to_add;
                    counter += 1;
                    int x = Math.Min(bits_left, 8);
                    int offset = bits_left - x;
                    spare_data = (byte)((item & (1 << x) - 1 << offset) >> offset);
                    if (x == 8) {
                        data_out[counter] = spare_data;
                        counter += 1;
                        bits_left -= 8;
                        spare_data = (byte)(item & (1 << offset) - 1);
                    }
                }
                while (bits_left > 8);
            }

            if (bits_left > 0) {
                int next_bits = 8 - bits_left;
                byte byte_to_add = (byte)(spare_data << next_bits);
                data_out[counter] = byte_to_add;  // Last byte
            }

            return data_out;
        }

        private int[] bytes_to_symbols(byte[] data_in, int sym_width) {
            int sym_count = (int)Math.Ceiling(data_in.Length * 8 / (double)sym_width);
            var data_out = new int[sym_count];
            int counter = 0;
            var int_data = default(int);
            int bit_offset = 0;
            for (int i = 0, loopTo = data_in.Length - 1; i <= loopTo; i++) {
                int data_in_bitcount = 8;
                do {
                    int bits_left = sym_width - bit_offset; // number of bits our int_data needed
                    int sel_count = Math.Min(bits_left, data_in_bitcount); // number of bits we can pull from the current byte
                    int target_offset = sym_width - (bit_offset + sel_count);
                    data_in_bitcount -= sel_count;
                    int src_offset = data_in_bitcount;
                    byte bit_mask = (byte)((1 << sel_count) - 1 << src_offset);
                    int data_selected = (data_in[i] & bit_mask) >> src_offset;
                    int_data = int_data | data_selected << target_offset;
                    bit_offset += sel_count;
                    if (bit_offset == sym_width) {
                        data_out[counter] = int_data;
                        counter += 1;
                        bit_offset = 0;
                        int_data = 0;
                    }
                }
                while (data_in_bitcount > 0);
            }

            if (bit_offset > 0) {
                data_out[counter] = int_data;
            }

            return data_out;
        }
    }

    public class BinaryBHC {
        private int BCH_CONST_M = 13; // 512 bytes, 14=1024
        private int BCH_CONST_T; // 1,2,4,8,10,14

        public int PARITY_BITS {
            get {
                return BCH_CONST_T;
            }

            set {
                BCH_CONST_T = value;
            }
        }

        public BinaryBHC() {
        }

        public int GetEccSize() {
            switch (PARITY_BITS) {
                case 1: {
                        return 2;
                    }

                case 2: {
                        return 4;
                    }

                case 4: {
                        return 7;
                    }

                case 8: {
                        return 13;
                    }

                case 10: {
                        return 17;
                    }

                case 14: {
                        return 23;
                    }

                default: {
                        return -1;
                    }
            }
        }

        public byte[] GenerateECC(byte[] block) {
            //using (var BchControl = new ECC.BCH(BCH_CONST_M, BCH_CONST_T)) {
            //    var data = BchControl.Encode(block);
            //    return data;
            //}
            return null;
        }

        public ECC_DECODE_RESULT ProcessECC(ref byte[] block, ref byte[] stored_ecc) {
            //using (var BchControl = new ECC.BCH(BCH_CONST_M, BCH_CONST_T)) {
            //    var cmp_result = BchControl.Decode(block, stored_ecc);
            //    switch (cmp_result) {
            //        case ECC.CompareResult.NoError: {
            //                return ECC_DECODE_RESULT.NoErrors;
            //            }

            //        case ECC.CompareResult.EccError: {
            //                return ECC_DECODE_RESULT.EccError;
            //            }

            //        case ECC.CompareResult.Correctable: {
            //                return ECC_DECODE_RESULT.Correctable;
            //            }

            //        default: {
            //                return ECC_DECODE_RESULT.Uncorractable;
            //            }
            //    }
            //}
            return ECC_DECODE_RESULT.NoErrors;
        }
    }

    public class Engine {
        private ecc_algorithum ecc_mode;
        private Hamming ecc_hamming = new Hamming();
        private RS_ECC ecc_reedsolomon = new RS_ECC();
        private BinaryBHC ecc_bhc = new BinaryBHC();
        public ushort[] ECC_DATA_LOCATION;

        public bool REVERSE_ARRAY { get; set; } = false; // RS option allows to reverse the input byte array
        public int SYM_WIDTH { get; private set; } = 9; // Used by RS

        public Engine(ecc_algorithum mode, int parity_level = 1, int symbole_width = 0) {
            ecc_mode = mode;
            SYM_WIDTH = symbole_width;
            switch (ecc_mode) {
                case ecc_algorithum.hamming: { // Hamming only supports 1-bit ECC correction
                        break;
                    }

                case ecc_algorithum.reedsolomon: {
                        ecc_reedsolomon.SYM_WIDTH = symbole_width;
                        ecc_reedsolomon.PARITY_BITS = parity_level;
                        break;
                    }

                case ecc_algorithum.bhc: {
                        ecc_bhc.PARITY_BITS = parity_level;
                        break;
                    }
            }
        }

        public byte[] GenerateECC(byte[] data_in) {
            if (REVERSE_ARRAY)
                Array.Reverse(data_in);
            switch (ecc_mode) {
                case ecc_algorithum.hamming: { // Hamming only supports 1-bit ECC correction
                        return ecc_hamming.GenerateECC(data_in);
                    }

                case ecc_algorithum.reedsolomon: {
                        return ecc_reedsolomon.GenerateECC(data_in);
                    }

                case ecc_algorithum.bhc: {
                        return ecc_bhc.GenerateECC(data_in);
                    }
            }

            return null;
        }
        // Processes blocks of 512 bytes and returns the last decoded result
        public ECC_DECODE_RESULT ReadData(byte[] data_in, byte[] ecc) {
            var result = ECC_DECODE_RESULT.NoErrors;
            try {
                if (Utilities.IsByteArrayFilled(ref ecc, 255))
                    return ECC_DECODE_RESULT.NoErrors; // ECC area does not contain ECC data
                if (!(data_in.Length % 512 == 0))
                    return ECC_DECODE_RESULT.InputError;
                if (REVERSE_ARRAY)
                    Array.Reverse(data_in);
                int ecc_byte_size = GetEccByteSize();
                int ecc_ptr = 0;
                for (int i = 1, loopTo = data_in.Length; i <= loopTo; i += 512) {
                    var block = new byte[512];
                    Array.Copy(data_in, i - 1, block, 0, 512);
                    var ecc_data = new byte[ecc_byte_size];
                    Array.Copy(ecc, ecc_ptr, ecc_data, 0, ecc_data.Length);
                    switch (ecc_mode) {
                        case ecc_algorithum.hamming: {
                                result = ecc_hamming.ProcessECC(block, ref ecc_data);
                                break;
                            }

                        case ecc_algorithum.reedsolomon: {
                                result = ecc_reedsolomon.ProcessECC(ref block, ref ecc_data);
                                break;
                            }

                        case ecc_algorithum.bhc: {
                                result = ecc_bhc.ProcessECC(ref block, ref ecc_data);
                                break;
                            }
                    }

                    if (result == ECC_DECODE_RESULT.Uncorractable) {
                        return ECC_DECODE_RESULT.Uncorractable;
                    } else if (result == ECC_DECODE_RESULT.Correctable) {
                        Array.Copy(block, 0, data_in, i - 1, 512); // Copies the correct block into the current page data
                        result = ECC_DECODE_RESULT.NoErrors;
                    }

                    ecc_ptr += ecc_byte_size;
                }
            } catch (Exception ex) {
            }

            return result;
        }

        public void WriteData(byte[] data_in, ref byte[] ecc) {
            try {
                if (!(data_in.Length % 512 == 0))
                    return;
                if (REVERSE_ARRAY)
                    Array.Reverse(data_in);
                int ecc_byte_size = GetEccByteSize();
                int blocks = (int)(data_in.Length / 512d);
                ecc = new byte[(blocks * ecc_byte_size)];
                Utilities.FillByteArray(ref ecc, 255);
                int ecc_ptr = 0;
                for (int i = 1, loopTo = data_in.Length; i <= loopTo; i += 512) {
                    var block = new byte[512];
                    Array.Copy(data_in, i - 1, block, 0, 512);
                    byte[] ecc_data = null;
                    switch (ecc_mode) {
                        case ecc_algorithum.hamming: {
                                ecc_data = ecc_hamming.GenerateECC(block);
                                break;
                            }

                        case ecc_algorithum.reedsolomon: {
                                ecc_data = ecc_reedsolomon.GenerateECC(block);
                                break;
                            }

                        case ecc_algorithum.bhc: {
                                ecc_data = ecc_bhc.GenerateECC(block);
                                break;
                            }
                    }

                    Array.Copy(ecc_data, 0, ecc, ecc_ptr, ecc_data.Length);
                    ecc_ptr += ecc_byte_size;
                }
            } catch (Exception ex) {
            }
        }

        public int GetEccByteSize() {
            switch (ecc_mode) {
                case ecc_algorithum.hamming: {
                        return 3;
                    }

                case ecc_algorithum.reedsolomon: {
                        return ecc_reedsolomon.GetEccSize();
                    }

                case ecc_algorithum.bhc: {
                        return ecc_bhc.GetEccSize();
                    }
            }

            return -1;
        }

        public byte[] GetEccFromSpare(byte[] spare, ushort page_size, ushort oob_size) {
            int bytes_per_ecc = GetEccByteSize(); // Number of ECC byters per sector
            int obb_count = (int)(spare.Length / (double)oob_size); // Number of OOB areas to process
            int sector_count = (int)(page_size / 512d); // Each OOB contains this many sectors
            var ecc_data = new byte[(obb_count * sector_count * bytes_per_ecc)];
            int ecc_data_ptr = 0;
            int spare_ptr = 0;
            for (int x = 0, loopTo = obb_count - 1; x <= loopTo; x++) {
                for (int y = 0, loopTo1 = sector_count - 1; y <= loopTo1; y++) {
                    ushort ecc_offset = ECC_DATA_LOCATION[y];
                    Array.Copy(spare, spare_ptr + ecc_offset, ecc_data, ecc_data_ptr, bytes_per_ecc);
                    ecc_data_ptr += bytes_per_ecc;
                }

                spare_ptr += oob_size;
            }

            return ecc_data;
        }
        // Writes the ECC bytes into the spare area
        public void SetEccToSpare(byte[] spare, byte[] ecc_data, ushort page_size, ushort oob_size) {
            int bytes_per_ecc = GetEccByteSize(); // Number of ECC byters per sector
            int obb_count = (int)(spare.Length / (double)oob_size); // Number of OOB areas to process
            int sector_count = (int)(page_size / 512d); // Each OOB contains this many sectors
            int ecc_data_ptr = 0;
            int spare_ptr = 0;
            for (int x = 0, loopTo = obb_count - 1; x <= loopTo; x++) {
                for (int y = 0, loopTo1 = sector_count - 1; y <= loopTo1; y++) {
                    ushort ecc_offset = ECC_DATA_LOCATION[y];
                    Array.Copy(ecc_data, ecc_data_ptr, spare, ecc_offset + spare_ptr, bytes_per_ecc);
                    ecc_data_ptr += bytes_per_ecc;
                }

                spare_ptr += oob_size;
            }
        }
    }

    public enum ECC_DECODE_RESULT {
        NoErrors, // all bits and parity match
        Correctable, // one or more bits dont match but was corrected
        EccError, // the error is in the ecc
        Uncorractable, // more errors than are correctable
        InputError // User sent data that was not in 512 byte segments
    }

    public enum ecc_algorithum : int {
        hamming = 0,
        reedsolomon = 1,
        bhc = 2
    }

    public static class Common {
        public static string ECC_GetErrorMessage(ECC_DECODE_RESULT err) {
            switch (err) {
                case ECC_DECODE_RESULT.NoErrors: {
                        return "No Errors found";
                    }

                case ECC_DECODE_RESULT.Correctable: {
                        return "Data Corrected";
                    }

                case ECC_DECODE_RESULT.EccError: {
                        return "ECC data error";
                    }

                case ECC_DECODE_RESULT.Uncorractable: {
                        return "Too many errors";
                    }

                case ECC_DECODE_RESULT.InputError: {
                        return "Block data was invalid";
                    }

                default: {
                        return "";
                    }
            }
        }

        public static byte GetEccDataSize(ECC_Configuration_Entry item) {
            var ecc_eng_example = new Engine(item.Algorithm, item.BitError, item.SymSize);
            return (byte)ecc_eng_example.GetEccByteSize();
        }
    }

    public class ECC_Configuration_Entry {
        public ushort PageSize { get; set; }
        public ushort SpareSize { get; set; }
        public ecc_algorithum Algorithm { get; set; }
        public byte BitError { get; set; } // Number of bits that can be corrected
        public byte SymSize { get; set; } // Number of bits per symbol (RS only)
        public bool ReverseData { get; set; } // Reverses ECC byte order

        public ushort[] EccRegion;

        public ECC_Configuration_Entry() {
        }

        public bool IsValid() {
            if (PageSize == 0 || !(PageSize % 512 == 0))
                return false;
            int sector_count = (int)(PageSize / 512d);
            int ecc_data_size = Common.GetEccDataSize(this);
            if (EccRegion is null)
                return false;
            if (!(EccRegion.Length == sector_count))
                return false;
            for (int i = 0, loopTo = EccRegion.Length - 1; i <= loopTo; i++) {
                ushort start_addr = EccRegion[i];
                ushort end_addr = (ushort)(start_addr + ecc_data_size - 1);
                if (end_addr >= SpareSize)
                    return false;
                for (int x = 0, loopTo1 = EccRegion.Length - 1; x <= loopTo1; x++) {
                    if (i != x) {
                        ushort target_addr = EccRegion[x];
                        ushort target_end = (ushort)(target_addr + ecc_data_size - 1);
                        if (start_addr >= target_addr & start_addr <= target_end) {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public void AddRegion(ushort offset) {
            if (EccRegion is null) {
                EccRegion = new ushort[1];
                EccRegion[0] = offset;
            } else {
                Array.Resize(ref EccRegion, EccRegion.Length + 1);
                EccRegion[EccRegion.Length - 1] = offset;
            }
        }
    }
}