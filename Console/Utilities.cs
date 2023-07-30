using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

public static class Utilities {

    public static class Bytes {

        #region "Bytes From"
        // Converts a Single byte into bytes
        public static byte[] FromUInt16(ushort int_in) {
            var byteArray = BitConverter.GetBytes(int_in);
            Array.Reverse(byteArray); // Need to reverse the result
            return byteArray;
        }
        // Converts a integer byte into three bytes
        public static byte[] FromUInt24(UInt32 int_in) {
            byte[] output = new byte[3];
            output[0] = System.Convert.ToByte((int_in >> 16) & 0xFF);
            output[1] = System.Convert.ToByte((int_in >> 8) & 0xFF);
            output[2] = System.Convert.ToByte(int_in & 0xFF);
            return output;
        }
        // Converts a uinteger into bytes
        public static byte[] FromUInt32(uint int_in) {
            var byteArray = BitConverter.GetBytes(int_in);
            Array.Reverse(byteArray); // Need to reverse the result
            return byteArray;
        }

        public static byte[] FromUInt64(ulong int_in) {
            var byteArray = BitConverter.GetBytes(int_in);
            Array.Reverse(byteArray); // Need to reverse the result
            return byteArray;
        }

        public static byte[] FromInt32(int int_in) {
            var byteArray = BitConverter.GetBytes(int_in);
            Array.Reverse(byteArray); // Need to reverse the result
            return byteArray;
        }
        // Converts a string into a byte array (does not add string terminator)
        public static byte[] FromChrString(string str_in) {
            if (str_in is null || string.IsNullOrEmpty(str_in))
                return null;
            var ret = new byte[str_in.Length];
            int i;
            var loopTo = ret.Length - 1;
            for (i = 0; i <= loopTo; i++) {
                char c = char.Parse(str_in.Substring(i, 1));
                ret[i] = (byte)c;
            }
            return ret;
        }

        public static byte[] FromUint32Array(uint[] words) {
            var ret = new byte[(words.Length * 4)];
            int i;
            int counter = 0;
            byte[] q;
            var loopTo = words.Length - 1;
            for (i = 0; i <= loopTo; i++) {
                q = FromUInt32(words[i]);
                ret[counter] = q[0];
                ret[counter + 1] = q[1];
                ret[counter + 2] = q[2];
                ret[counter + 3] = q[3];
                counter += 4;
            }
            return ret;
        }

        public static byte[] FromHexString(string hex_string)
        {
            if (hex_string is null || hex_string.Trim().Equals(""))
                return null;
            hex_string = hex_string.Replace(" ", "").Trim().ToUpper(); // Remove padding
            if (hex_string.EndsWith("H"))
                hex_string = hex_string.Substring(0, hex_string.Length - 1);
            if (hex_string.StartsWith("0X"))
                hex_string = hex_string.Substring(2);
            if (!(hex_string.Length % 2 == 0))
                hex_string = "0" + hex_string;
            var data_out = new byte[(int)(hex_string.Length / 2d - 1d + 1)];
            for (int i = 0, loopTo = data_out.Length - 1; i <= loopTo; i++)
                data_out[i] = Convert.ToByte(hex_string.Substring(i * 2, 2), 16);
            return data_out;
        }

        #endregion

        #region "Bytes To"

        public static ushort ToUInt16(byte[] data)
        {
            if (data is null || data.Length > 2)
                return 0;
            if (data.Length == 1)
                return (ushort)(uint)data[0];
            return (ushort)(data[0] * 256 + data[1]);
        }

        public static UInt32 ToUInt32(byte[] data)
        {
            try
            {
                UInt32 Result = 0;
                int TotalBytes = 4;
                if (data.Length < TotalBytes)
                    TotalBytes = data.Length;
                while (TotalBytes != 0)
                {
                    Result = (Result << 8);
                    Result += data[data.Length - TotalBytes];
                    TotalBytes -= 1;
                }
                return Result;
            }
            catch
            {
                return 0;
            }
        }

        public static ulong ToUInt64(byte[] data)
        {
            try
            {
                ulong Result = 0UL;
                int TotalBytes = 8;
                if (data.Length < TotalBytes)
                    TotalBytes = data.Length;
                while (TotalBytes != 0)
                {
                    Result = Result << 8;
                    Result += data[data.Length - TotalBytes];
                    TotalBytes -= 1;
                }

                return Result;
            }
            catch
            {
                return 0UL;
            }
        }

        public static int ToInt32(byte[] input)
        {
            return BitConverter.ToInt32(input, 0);
        }

        public static string ToChrString(byte[] data)
        {
            return Encoding.UTF8.GetString(data, 0, data.Length);
        }

        public static string ToHexString(byte[] bytes_Input)
        {
            var strTemp = new StringBuilder(bytes_Input.Length * 2);
            foreach (byte b in bytes_Input)
                strTemp.Append(b.ToString("X").PadLeft(2,'0'));
            return strTemp.ToString();
        }
        // Converts a data array {00,01,02} to its padded hexstring "00 01 02"
        public static string ToPaddedHexString(byte[] data) {
            if (data is null || data.Length == 0)
                return "";
            var c = new char[(data.Length * 2 + (data.Length - 1))];
            int counter = 0;
            for (int i = 0, loopTo = data.Length - 2; i <= loopTo; i++) {
                byte b = data[i];
                c[counter] = GetByteChar((byte)(b >> 4));
                c[counter + 1] = GetByteChar(b);
                c[counter + 2] = ' ';
                counter += 3;
            }
            byte last_byte = data[data.Length - 1];
            c[counter] = GetByteChar((byte)(last_byte >> 4));
            c[counter + 1] = GetByteChar(last_byte);
            return new string(c);
        }

        public static string[] ToCharStringArray(byte[] data) {
            var file_out = new List<string>();
            using (var mem_reader = new MemoryStream(data)) {
                using (var str_reader = new StreamReader(mem_reader)) {
                    while (str_reader.Peek() != -1)
                        file_out.Add(str_reader.ReadLine());
                }
            }
            return file_out.ToArray();
        }
        // Converts a byte() into uint() padds the last element with 00s
        public static uint[] ToUIntArray(byte[] data) {
            int i;
            while (data.Length % 4 != 0)
                Array.Resize(ref data, data.Length + 1);
            int NumOfWords = (int)(data.Length / 4d);
            var ret = new uint[NumOfWords];
            uint sVal;
            uint ival;
            var loopTo = NumOfWords - 1;
            for (i = 0; i <= loopTo; i++) {
                int s = i * 4;
                sVal = (uint)data[s] << 24;
                ival = data[s + 1];
                sVal += ival << 16;
                ival = data[s + 2];
                sVal += ival << 8;
                sVal += data[s + 3];
                ret[i] = sVal;
            }
            return ret;
        }

        private static char GetByteChar(byte n) {
            n = (byte)(n & 0xF); // We are only converting first 4 bits
            if (n < 10) {
                return (char)(48 + n); // 0-9
            } else {
                return (char)(55 + n);
            } // A-F
        }

        #endregion

    }

    public static class IsDataType {

        public static bool Bool(string Input) {
            if (Input.ToUpper().Equals("TRUE") || Input.ToUpper().Equals("FALSE"))
                return true;
            return false;
        }

        public static bool Integer(string Input) {
            int argresult = default;
            return int.TryParse(Input, out argresult);
        }

        public static bool Uinteger(string Input) {
            uint argresult = default;
            return uint.TryParse(Input, out argresult);
        }

        public static bool UInteger64(string Input) {
            ulong argresult = default;
            return ulong.TryParse(Input, out argresult);
        }

        public static bool IPv4(string ipv4_str) {
            System.Net.IPAddress p = null;
            if (!System.Net.IPAddress.TryParse(ipv4_str, out p))
                return false;
            if (!(p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
                return false;
            return true;
        }

        public static bool IPv6(string ipv6_str) {
            System.Net.IPAddress p = null;
            if (!System.Net.IPAddress.TryParse(ipv6_str, out p))
                return false;
            if (!(p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6))
                return false;
            return true;
        }

        public static bool String(string input) {
            if (input is null)
                return false;
            if (string.IsNullOrEmpty(input))
                return false;
            if (input.StartsWith("\"") & input.EndsWith("\""))
                return true;
            return false;
        }

        public static bool IpAddress(string input) {
            System.Net.IPAddress addr = null;
            if (System.Net.IPAddress.TryParse(input, out addr)) {
                return true;
            } else {
                return false;
            } // Not a valid IP
        }

        public static bool HexString(string inputhex) {
            try {
                inputhex = inputhex.Replace(" ", "");
                if (inputhex.ToUpper().StartsWith("0X")) {
                    inputhex = inputhex.Substring(2);
                } else if (inputhex.ToUpper().EndsWith("H")) {
                    inputhex = inputhex.Substring(0, inputhex.Length - 1);
                }
                if (string.IsNullOrEmpty(inputhex))
                    return false;
                return Hex(inputhex);
            } catch {
                return false;
            }
        }

        public static bool Hex(string input) {
            if (input.ToUpper().StartsWith("0X"))
                input = input.Substring(2);
            int i;
            var loopTo = input.Length - 1;
            for (i = 0; i <= loopTo; i++) {
                char c = char.Parse(input.Substring(i, 1).ToUpper());
                if (!Char.IsDigit(c)) {
                    switch (c) {
                        case 'A': {
                                break;
                            }
                        case 'B': {
                                break;
                            }
                        case 'C': {
                                break;
                            }
                        case 'D': {
                                break;
                            }
                        case 'E': {
                                break;
                            }
                        case 'F': {
                                break;
                            }
                        default: {
                                return false;
                            }
                    }
                }
            }
            return true;
        }

    }

    public static class FileIO {

        public static string[] ReadFile(string fileName) {
            try {
                var local_file = new FileInfo(fileName);
                if (!local_file.Exists)
                    return null;
                var file_out = new List<string>();
                using (var file_reader = local_file.OpenText()) {
                    while (file_reader.Peek() != -1)
                        file_out.Add(file_reader.ReadLine());
                    file_reader.Close();
                }

                if (file_out.Count == 0)
                    return null;
                return file_out.ToArray();
            } catch {
                return null;
            }
        }

        public static bool WriteFile(string[] FileOut, string FileName) {
            try {
                var local_file = new FileInfo(FileName);
                if (local_file.Exists)
                    local_file.Delete();
                var local_dir = new DirectoryInfo(local_file.DirectoryName);
                if (!local_dir.Exists)
                    local_dir.Create();
                using (var file_writer = new StreamWriter(local_file.FullName, true, Encoding.ASCII, 2048)) {
                    foreach (string Line in FileOut) {
                        if (Line.Length == 0 || string.IsNullOrEmpty(Line)) {
                            file_writer.WriteLine();
                        } else {
                            file_writer.WriteLine(Line);
                        }
                    }

                    file_writer.Flush();
                }

                return true;
            } catch {
                return false;
            }
        }

        public static bool AppendFile(string[] FileOut, string FileName) {
            try {
                var local_file = new FileInfo(FileName);
                var local_dir = new DirectoryInfo(local_file.DirectoryName);
                if (!local_dir.Exists)
                    local_dir.Create();
                if (FileOut is null)
                    return true;
                using (var file_writer = local_file.AppendText()) {
                    foreach (var Line in FileOut) {
                        if (Line.Length == 0)
                            file_writer.WriteLine();
                        else
                            file_writer.WriteLine(Line);
                    }

                    file_writer.Close();
                }

                return true;
            } catch {
                return false;
            }
        }

        public static bool AppendBytes(byte[] DataOut, string FileName) {
            try {
                var local_file = new FileInfo(FileName);
                var local_dir = new DirectoryInfo(local_file.DirectoryName);
                if (!local_dir.Exists)
                    local_dir.Create();
                if (DataOut is null || DataOut.Length == 0)
                    return true;
                using (var file_writer = local_file.OpenWrite()) {
                    file_writer.Position = local_file.Length;
                    file_writer.Write(DataOut, 0, DataOut.Length);
                    file_writer.Flush();
                }
                return true;
            } catch {
                return false;
            }
        }

        public static byte[] ReadBytes(string FileName, long MaximumSize = 0) {
            try {
                var local_file = new FileInfo(FileName);
                if (!local_file.Exists | local_file.Length == 0L)
                    return null;
                byte[] BytesOut;
                if (MaximumSize > 0) {
                    BytesOut = new byte[MaximumSize];
                } else {
                    BytesOut = new byte[(int)(local_file.Length - 1L + 1)];
                }
                using (var file_reader = new BinaryReader(local_file.OpenRead())) {
                    for (uint i = 0U, loopTo = (uint)(BytesOut.Length - 1); i <= loopTo; i++)
                        BytesOut[(int)i] = file_reader.ReadByte();
                    file_reader.Close();
                }
                return BytesOut;
            } catch {
                return null;
            }
        }

        public static bool WriteBytes(byte[] DataOut, string FileName) {
            try {
                var local_file = new FileInfo(FileName);
                if (local_file.Exists)
                    local_file.Delete();
                var local_dir = new DirectoryInfo(local_file.DirectoryName);
                if (!local_dir.Exists)
                    local_dir.Create();
                using (var file_writer = local_file.OpenWrite()) {
                    file_writer.Write(DataOut, 0, DataOut.Length);
                    file_writer.Flush();
                }
                return true;
            } catch {
                return false;
            }
        }

    }

    public class CRC32 {
        private static uint[] table;

        static CRC32()
        {
            uint poly = 0xEDB88320U;
            table = new uint[256];
            uint temp = 0U;
            for (uint i = 0U, loopTo = (uint)(table.Length - 1); i <= loopTo; i++)
            {
                temp = i;
                for (int j = 8; j >= 1; j -= 1)
                {
                    if ((temp & 1L) == 1L)
                    {
                        temp = temp >> 1 ^ poly;
                    }
                    else
                    {
                        temp = temp >> 1;
                    }
                }

                table[(int)i] = temp;
            }
        }

        public static uint ComputeChecksum(byte[] bytes)
        {
            uint crc = 0xFFFFFFFFU;
            for (int i = 0, loopTo = bytes.Length - 1; i <= loopTo; i++)
            {
                byte index = (byte)(crc & 0xFFL ^ bytes[i]);
                crc = crc >> 8 ^ table[index];
            }

            return ~crc;
        }
    }

    public class CRC16 {
        private static ushort[] table;

        static CRC16()
        {
            ushort poly = 0xA001; // calculates CRC-16 using A001 polynomial (modbus)
            table = new ushort[256];
            ushort temp = 0;
            for (ushort i = 0, loopTo = (ushort)(table.Length - 1); i <= loopTo; i++)
            {
                temp = i;
                for (int j = 8; j >= 1; j -= 1)
                {
                    if ((temp & 1) == 1)
                    {
                        temp = (ushort)((temp >> 1) ^ poly);
                    }
                    else
                    {
                        temp = (ushort)(temp >> 1);
                    }
                }

                table[i] = temp;
            }
        }

        public static ushort ComputeChecksum(byte[] bytes)
        {
            ushort crc = 0x0; // The calculation start with 0x00
            for (int i = 0, loopTo = bytes.Length - 1; i <= loopTo; i++)
            {
                byte index = (byte)(crc & 0xFF ^ bytes[i]);
                crc = (ushort)((crc >> 8) ^ table[index]);
            }

            return (ushort)~crc;
        }
    }

    public static void ChangeEndian32_LSB16(ref byte[] Buffer)
    {
        uint step_value = 4U;
        uint last_index = (uint)(Buffer.Length - Buffer.Length % step_value);
        for (long i = 0L, loopTo = last_index - 1L; (long)step_value >= 0 ? i <= loopTo : i >= loopTo; i += step_value)
        {
            byte B1 = Buffer[(int)(i + 3L)];
            byte B2 = Buffer[(int)(i + 2L)];
            byte B3 = Buffer[(int)(i + 1L)];
            byte B4 = Buffer[(int)(i + 0L)];
            Buffer[(int)(i + 3L)] = B3;
            Buffer[(int)(i + 2L)] = B4;
            Buffer[(int)(i + 1L)] = B1;
            Buffer[(int)(i + 0L)] = B2;
        }
    }
    // 0x01020304 = 0x04030201
    public static void ChangeEndian32_LSB8(ref byte[] Buffer)
    {
        uint step_value = 4U;
        uint last_index = (uint)(Buffer.Length - Buffer.Length % step_value);
        for (long i = 0L, loopTo = last_index - 1L; (long)step_value >= 0 ? i <= loopTo : i >= loopTo; i += step_value)
        {
            byte B1 = Buffer[(int)(i + 3L)];
            byte B2 = Buffer[(int)(i + 2L)];
            byte B3 = Buffer[(int)(i + 1L)];
            byte B4 = Buffer[(int)(i + 0L)];
            Buffer[(int)(i + 3L)] = B4;
            Buffer[(int)(i + 2L)] = B3;
            Buffer[(int)(i + 1L)] = B2;
            Buffer[(int)(i + 0L)] = B1;
        }
    }
    // 0x01020304 = 0x02010403
    public static void ChangeEndian16_MSB(ref byte[] Buffer)
    {
        uint step_value = 2U;
        uint last_index = (uint)(Buffer.Length - Buffer.Length % step_value);
        for (long i = 0L, loopTo = last_index - 1L; (long)step_value >= 0 ? i <= loopTo : i >= loopTo; i += step_value)
        {
            byte b_high = Buffer[(int)i];
            byte b_low = Buffer[(int)(i + 1L)];
            Buffer[(int)i] = b_low;
            Buffer[(int)(i + 1L)] = b_high;
        }
    }
    // 0b11110000 = 0b00001111
    public static void ChangeEndian_Nibble(ref byte[] Buffer)
    {
        for (int i = 0, loopTo = Buffer.Length - 1; i <= loopTo; i++)
        {
            byte b = Buffer[i];
            Buffer[i] = (byte)(b << 4 | b >> 4);
        }
    }
    // 0b00000001 = 0b10000000 (reversed bit order for 8-bit)
    public static void ReverseBits_Byte(ref byte[] Buffer)
    {
        for (int i = 0, loopTo = Buffer.Length - 1; i <= loopTo; i += 1)
        {
            byte b = Buffer[i];
            var bo = ByteToBooleanArray(new[] { b });
            Array.Reverse(bo);
            var @out = BoolToByteArray(bo);
            Buffer[i] = @out[0];
        }
    }
    // 0b0000000100000010 = 0b0100000010000000 (reversed bit order for 16-bit)
    public static void ReverseBits_HalfWord(ref byte[] Buffer)
    {
        uint step_value = 2U;
        uint last_index = (uint)(Buffer.Length - Buffer.Length % step_value);
        for (long i = 0L, loopTo = last_index - 1L; (long)step_value >= 0 ? i <= loopTo : i >= loopTo; i += step_value)
        {
            byte high_b = Buffer[(int)i];
            byte low_b = Buffer[(int)(i + 1L)];
            var bo = ByteToBooleanArray(new[] { high_b, low_b });
            Array.Reverse(bo);
            var @out = BoolToByteArray(bo);
            Buffer[(int)i] = @out[0];
            Buffer[(int)(i + 1L)] = @out[1];
        }
    }
    // 0b00000001000000100000001100000100 = 0b00100000110000000100000010000000 (reversed bit order for 32-bit)
    public static void ReverseBits_Word(ref byte[] Buffer)
    {
        uint step_value = 4U;
        uint last_index = (uint)(Buffer.Length - Buffer.Length % step_value);
        for (long i = 0L, loopTo = last_index - 1L; (long)step_value >= 0 ? i <= loopTo : i >= loopTo; i += step_value)
        {
            byte b1 = Buffer[(int)i];
            byte b2 = Buffer[(int)(i + 1L)];
            byte b3 = Buffer[(int)(i + 2L)];
            byte b4 = Buffer[(int)(i + 3L)];
            var bo = ByteToBooleanArray(new[] { b1, b2, b3, b4 });
            Array.Reverse(bo);
            var @out = BoolToByteArray(bo);
            Buffer[(int)i] = @out[0];
            Buffer[(int)(i + 1L)] = @out[1];
            Buffer[(int)(i + 2L)] = @out[2];
            Buffer[(int)(i + 3L)] = @out[3];
        }
    }

    public static void ReverseBits(ref uint x, int count = 32)
    {
        uint y = 0U;
        for (int i = 0, loopTo = count - 1; i <= loopTo; i++)
        {
            y = y << 1;
            y = (uint)(y | x & 1L);
            x = x >> 1;
        }

        x = y;
    }

    public static void ReverseBits(ref ulong x, int count = 64)
    {
        ulong y = 0UL;
        for (int i = 0, loopTo = count - 1; i <= loopTo; i++)
        {
            y = y << 1;
            y = (ulong)((long)y | (long)x & 1L);
            x = x >> 1;
        }

        x = y;
    }

    public static void ReverseBits_ByteEndian(ref uint x)
    {
        var y = default(uint);
        int offset = 7;
        for (int i = 0; i <= 31; i++)
        {
            if ((x & 1) == 1)
            {
                y = (uint)(y | (long)(1 << offset + i / 8 * 8));
            }
            offset -= 1;
            if (offset == -1)
                offset = 7;
            x = x >> 1;
        }

        x = y;
    }

    public static bool[] ByteToBooleanArray(byte[] anyByteArray)
    {
        bool[] returnedArray;
        var truthList = new List<bool>();
        if (anyByteArray is object)
        {
            for (int index = 0, loopTo = anyByteArray.Length - 1; index <= loopTo; index++)
            {
                truthList.Add(Convert.ToBoolean(anyByteArray[index] & 128));
                truthList.Add(Convert.ToBoolean(anyByteArray[index] & 64));
                truthList.Add(Convert.ToBoolean(anyByteArray[index] & 32));
                truthList.Add(Convert.ToBoolean(anyByteArray[index] & 16));
                truthList.Add(Convert.ToBoolean(anyByteArray[index] & 8));
                truthList.Add(Convert.ToBoolean(anyByteArray[index] & 4));
                truthList.Add(Convert.ToBoolean(anyByteArray[index] & 2));
                truthList.Add(Convert.ToBoolean(anyByteArray[index] & 1));
            }
        }

        returnedArray = truthList.ToArray();
        return returnedArray;
    }

    public static byte[] BoolToByteArray(bool[] bools)
    {
        int carry = bools.Length % 8;
        if (!(carry == 0))
            Array.Resize(ref bools, bools.Length + carry); // Ensures the array.len is even
        var data_out = new List<byte>();
        for (int i = 0, loopTo = bools.Length - 1; i <= loopTo; i += 8)
        {
            byte res = 0;
            if (bools[i + 0])
                res = 128;
            if (bools[i + 1])
                res = (byte)(res + 64);
            if (bools[i + 2])
                res = (byte)(res + 32);
            if (bools[i + 3])
                res = (byte)(res + 16);
            if (bools[i + 4])
                res = (byte)(res + 8);
            if (bools[i + 5])
                res = (byte)(res + 4);
            if (bools[i + 6])
                res = (byte)(res + 2);
            if (bools[i + 7])
                res = (byte)(res + 1);
            data_out.Add(res);
        }

        return data_out.ToArray();
    }
    // Removes a comment from a command line
    public static string RemoveComment(string input, string comment_char = "#")
    {
        try
        {
            bool ProcessingQuote = false;
            for (int i = 0, loopTo = input.Length - comment_char.Length; i <= loopTo; i++)
            {
                if (ProcessingQuote)
                {
                    if (input.Substring(i, 1).Equals("\""))
                        ProcessingQuote = false;
                }
                else if (input.Substring(i, 1).Equals("\""))
                {
                    ProcessingQuote = true;
                }
                else if (input.Substring(i, comment_char.Length).Equals(comment_char))
                {
                    return input.Substring(0, i).Trim();
                }
            }
        }
        catch
        {
        }
        return input;
    }

    public static void FillByteArray(ref byte[] data, byte value) {
        if (data is null)
            return;
        for (int i = 0, loopTo = data.Length - 1; i <= loopTo; i++)
            data[i] = value;
    }

    public static bool IsByteArrayFilled(ref byte[] data, byte value) {
        if (data is null)
            return false;
        long counter = 0L;
        foreach (var d in data) {
            if (!(d == value)) {
                return false;
            }

            counter += 1L;
        }

        return true;
    }

    public static string RemoveQuotes(string input) {
        if (input.StartsWith("\"") && input.EndsWith("\"")) {
            return input.Substring(1, input.Length - 2);
        }
        return input;
    }

    public static bool HasQuotes(string input) {
        if (input.StartsWith("\"") && input.EndsWith("\""))
            return true;
        return false;
    }

    public static string AddQuotes(string input) {
        return "\"" + input + "\"";
    }

    public static void Sleep(int delay)
    {
        System.Threading.Thread.Sleep(delay);
    }

   public static int HexToInt(string value) {
        try {
            if (value.ToUpper().StartsWith("0X"))
                value = value.Substring(2);
            if (string.IsNullOrEmpty(value))
                return 0;
            return Convert.ToInt32(value, 16);
        } catch {
            return 0;
        }
    }

    public static uint HexToUInt(string value) {
        try {
            if (value.ToUpper().StartsWith("0X"))
                value = value.Substring(2);
            if (string.IsNullOrEmpty(value))
                return 0U;
            return Convert.ToUInt32(value, 16);
        } catch {
            return 0U;
        }
    }

    public static long HexToLng(string value) {
        try {
            if (value.ToUpper().StartsWith("0X"))
                value = value.Substring(2);
            if (string.IsNullOrEmpty(value))
                return 0L;
            return Convert.ToInt64(value, 16);
        } catch {
            return 0L;
        }
    }

    public static int BoolToInt(bool en) {
        if (en) {
            return 1;
        } else {
            return 0;
        }
    }

    public static float StringToSingle(string input) {
        return Convert.ToSingle(input, new System.Globalization.CultureInfo("en-US"));
    }

    public static byte[] DownloadFile(string URL) {
        try {
            var myWebClient = new System.Net.WebClient();
            byte[] b = myWebClient.DownloadData(URL);
            return b;
        } catch {
            return null;
        }
    }

    public static byte[] GetResourceAsBytes(string ResourceName) {
        var asm_names = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames();
        string ns_embedded_name = null;     //Namespace.ResourceName
        foreach (var emb_name in asm_names) {
            if (emb_name.ToUpper().EndsWith("." + ResourceName.ToUpper())) {
                ns_embedded_name = emb_name;
                break;
            }
        }
        if (String.IsNullOrEmpty(ns_embedded_name)) { return null; }
        System.Reflection.Assembly CurrentAssembly = System.Reflection.Assembly.GetCallingAssembly();
        Stream resStream = CurrentAssembly.GetManifestResourceStream(ns_embedded_name);
        if (resStream == null) { return null; }
        int SizeOfFile = (int)resStream.Length;
        byte[] data = new byte[SizeOfFile];
        resStream.Read(data, 0, data.Length);
        return data;
    }

    public static byte[] DecompressGzip(byte[] CompressedData) {
        try {
            using (var stream_out = new MemoryStream()) {
                using (var memory = new MemoryStream(CompressedData)) {
                    using (var gzip = new System.IO.Compression.GZipStream(memory, System.IO.Compression.CompressionMode.Decompress, true)) {
                        var buffer = new byte[4096];
                        while (true) {
                            int size = gzip.Read(buffer, 0, buffer.Length);
                            if (size > 0) {
                                stream_out.Write(buffer, 0, size);
                            } else {
                                break;
                            }
                        }
                    }
                }
                return stream_out.ToArray();
            }
        } catch {
            return null;
        }
    }

    public static byte[] CompressGzip(byte[] UncompressedData) {
        using (var memory = new MemoryStream()) {
            using (var gzip = new System.IO.Compression.GZipStream(memory, System.IO.Compression.CompressionMode.Compress, true)) {
                gzip.Write(UncompressedData, 0, UncompressedData.Length);
            }
            return memory.ToArray();
        }
    }

    public static Boolean IsNumeric(String s) {
        Boolean value = true;
        if (s == String.Empty || s == null) {
            value = false;
        } else {
            foreach (Char c in s.ToCharArray()) {
                value = value && Char.IsDigit(c);
            }
        }
        return value;
    }

}