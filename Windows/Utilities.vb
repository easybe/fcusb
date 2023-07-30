Option Strict On

Imports System.Text

Namespace Utilities

    Namespace Bytes
        'This module contains common converting methods for bytes
        Friend Module Bytes

#Region "Bytes From"
            'Converts a Single byte into bytes
            Public Function FromUInt16(int_in As UInt16) As Byte()
                Dim byteArray As Byte() = BitConverter.GetBytes(int_in)
                Array.Reverse(byteArray) 'Need to reverse the result
                Return byteArray
            End Function
            'Converts a integer byte into three bytes
            Public Function FromUInt24(int_in As UInt32) As Byte()
                Dim output(2) As Byte
                output(0) = CByte((int_in >> 16) And &HFF)
                output(1) = CByte((int_in >> 8) And &HFF)
                output(2) = CByte(int_in And &HFF)
                Return output
            End Function
            'Converts a uinteger into bytes
            Public Function FromUInt32(int_in As UInt32) As Byte()
                Dim byteArray As Byte() = BitConverter.GetBytes(int_in)
                Array.Reverse(byteArray) 'Need to reverse the result
                Return byteArray
            End Function

            Public Function FromUInt64(int_in As UInt64) As Byte()
                Dim byteArray As Byte() = BitConverter.GetBytes(int_in)
                Array.Reverse(byteArray) 'Need to reverse the result
                Return byteArray
            End Function
            'Converts a integer into bytes
            Public Function FromInt32(int_in As Int32) As Byte()
                Dim byteArray As Byte() = BitConverter.GetBytes(int_in)
                Array.Reverse(byteArray) 'Need to reverse the result
                Return byteArray
            End Function
            'Converts a string into a byte array (does not add string terminator)
            Public Function FromChrString(str_in As String) As Byte()
                If str_in Is Nothing OrElse str_in = "" Then Return Nothing
                Dim ret(str_in.Length - 1) As Byte
                Dim i As Integer
                For i = 0 To ret.Length - 1
                    Dim c As Char = CChar(str_in.Substring(i, 1))
                    ret(i) = CByte(AscW(c))
                Next
                Return ret
            End Function

            Public Function FromUint32Array(words() As UInt32) As Byte()
                Dim ret((words.Length * 4) - 1) As Byte
                Dim i As Integer
                Dim counter As Integer = 0
                Dim q() As Byte
                For i = 0 To words.Length - 1
                    q = FromUInt32(words(i))
                    ret(counter) = q(0)
                    ret(counter + 1) = q(1)
                    ret(counter + 2) = q(2)
                    ret(counter + 3) = q(3)
                    counter += 4
                Next
                Return ret
            End Function
            'Converts a string into a byte array (adds string terminator)
            Public Function FromStringZero(str_in As String) As Byte()
                Dim ret(str_in.Length) As Byte
                For i As Integer = 0 To ret.Length - 2
                    ret(i) = CByte(AscW(str_in.Substring(i, 1)))
                Next
                ret(str_in.Length) = 0
                Return ret
            End Function

            Public Function FromLong(lng_in As Long) As Byte()
                Dim d() As Byte = BitConverter.GetBytes(lng_in)
                Array.Reverse(d)
                Return d
            End Function

            Public Function FromBool(str_in As String) As Byte()
                If str_in.ToUpper = "TRUE" Then Return New Byte() {1}
                Return New Byte() {0}
            End Function

            Public Function FromAnyIpAddress(Address As String) As Byte()
                Try
                    Dim addr As Net.IPAddress = Nothing
                    If Net.IPAddress.TryParse(Address, addr) Then
                        Return addr.GetAddressBytes
                    Else
                        Return Nothing 'Not a valid IP
                    End If
                Catch ex As Exception
                    Return Nothing 'Not a valid IP
                End Try
            End Function

            Public Function FromHexString(hex_string As String) As Byte()
                If hex_string Is Nothing OrElse hex_string.Trim.Equals("") Then Return Nothing
                hex_string = hex_string.Replace(" ", "").Trim.ToUpper 'Remove padding
                If hex_string.StartsWith("0X") Then hex_string = hex_string.Substring(2)
                If hex_string.EndsWith("H") Then hex_string = hex_string.Substring(0, hex_string.Length - 1)
                If Not hex_string.Length Mod 2 = 0 Then hex_string = "0" & hex_string
                Dim data_out((hex_string.Length \ 2) - 1) As Byte
                For i = 0 To data_out.Length - 1
                    data_out(i) = Convert.ToByte(hex_string.Substring(i * 2, 2), 16)
                Next
                Return data_out
            End Function

            Public Function FromBitstring(bits_in As String) As Byte()
                Do Until bits_in.Length Mod 8 = 0
                    bits_in &= "0"
                Loop
                Dim numbytes As Integer = CInt(Math.Ceiling(bits_in.Length / 8))
                Dim bytesout(numbytes - 1) As Byte
                Dim counter As Integer = 0
                For i = 0 To bits_in.Length - 1 Step 8
                    bytesout(counter) = Convert.ToByte(bits_in.Substring(i, 8), 2)
                    counter += 1
                Next
                Return bytesout
            End Function

            Public Function FromIpAddress(ipv4_str As String) As Byte()
                Dim p As Net.IPAddress = Nothing
                If Not Net.IPAddress.TryParse(ipv4_str, p) Then Return Nothing
                If Not p.AddressFamily = Net.Sockets.AddressFamily.InterNetwork Then Return Nothing
                Return p.GetAddressBytes()
            End Function
            'Converts a ipv4/6 address and port into bytes. I.e. "192.168.100.1:80"
            Public Function FromTransport(input As String) As Byte()
                Try
                    Dim part1 As String = input.Substring(0, input.IndexOf(":"))
                    Dim part2 As String = input.Substring(input.IndexOf(":") + 1)
                    Dim port() As Byte = FromUInt16(CUShort(part2))
                    Dim ip() As Byte = FromAnyIpAddress(part1)
                    ReDim Preserve ip(ip.Length + 1)
                    ip(ip.Length - 2) = port(0)
                    ip(ip.Length - 1) = port(1)
                    Return ip
                Catch ex As Exception
                    Return Nothing
                End Try
            End Function

            Public Function FromCharStringArray(str_in() As String) As Byte()
                Try
                    Dim total_size As Integer = 0
                    For Each line In str_in
                        total_size += line.Length + 2
                    Next
                    total_size = total_size - 2 'Removes the last CrLf
                    Dim bytes_out(total_size - 1) As Byte
                    Dim ptr As Integer = 0
                    For i = 0 To str_in.Length - 1
                        Dim x() As Byte = Nothing
                        If i = str_in.Length - 1 Then 'last entry
                            x = FromChrString(str_in(i))
                        Else
                            x = FromChrString(str_in(i) & vbCrLf)
                        End If
                        Array.Copy(x, 0, bytes_out, ptr, x.Length)
                        ptr += x.Length
                    Next
                    Return bytes_out
                Catch ex As Exception
                    Return Nothing
                End Try
            End Function

#End Region

#Region "Bytes To"
            'Converts up to 4 bytes to a unsigned integer
            Public Function ToUInt32(data() As Byte) As UInt32
                Try
                    Dim Result As UInt32 = 0
                    Dim TotalBytes As Integer = 4
                    If data.Length < TotalBytes Then TotalBytes = data.Length
                    Do Until TotalBytes = 0
                        Result = (Result << 8)
                        Result += data(data.Length - TotalBytes)
                        TotalBytes -= 1
                    Loop
                    Return Result
                Catch ex As Exception
                    Return 0
                End Try
            End Function
            'Converts up to 4 bytes to a unsigned integer
            Public Function ToUInt64(data() As Byte) As UInt64
                Try
                    Dim Result As UInt64 = 0
                    Dim TotalBytes As Integer = 8
                    If data.Length < TotalBytes Then TotalBytes = data.Length
                    Do Until TotalBytes = 0
                        Result = (Result << 8)
                        Result += data(data.Length - TotalBytes)
                        TotalBytes -= 1
                    Loop
                    Return Result
                Catch ex As Exception
                    Return 0
                End Try
            End Function

            Public Function ToUInt16(data() As Byte) As UInt16
                If data Is Nothing OrElse data.Length > 2 Then Return 0
                If data.Length = 1 Then Return CUShort(data(0))
                Return (CUShort(data(0)) * 256US) + CUShort(data(1))
            End Function
            'Converts up to 4 bytes to a signed integer
            Public Function ToInt32(input() As Byte) As Int32
                If input Is Nothing Then Return 0
                If input.Length = 0 Then Return 0
                Dim IsNegative As Boolean = False
                If (input(0) >> 7) = 1 Then 'Negative value
                    If input.Length = 1 Then
                        Return CInt(&HFFFFFF00 + CInt(input(0)))
                    ElseIf input.Length = 2 Then
                        Return CInt(&HFFFF0000 + (CInt(input(0)) << 8)) + CInt(input(1))
                    ElseIf input.Length = 3 Then
                        Return CInt(&HFF000000 + (CInt(input(0)) << 16)) + (CInt(input(1)) << 8) + CInt(input(2))
                    ElseIf input.Length = 4 Then
                        Return CInt(CInt(input(0)) << 24) + (CInt(input(1)) << 16) + (CInt(input(2)) << 8) + CInt(input(3))
                    End If
                Else
                    If input.Length = 1 Then
                        Return CInt(input(0))
                    ElseIf input.Length = 2 Then
                        Return (CInt(input(0)) << 8) + input(1)
                    ElseIf input.Length = 3 Then
                        Return (CInt(input(0)) << 16) + (CInt(input(1)) << 8) + input(2)
                    ElseIf input.Length = 4 Then
                        Return (CInt(input(0)) << 24) + (CInt(input(1)) << 16) + (CInt(input(2)) << 8) + input(3)
                    End If
                End If
                Return 0
            End Function

            Public Function ToLong(data() As Byte) As Long
                Array.Reverse(data)
                Return BitConverter.ToInt64(data, 0)
            End Function

            Public Function ToChrString(data() As Byte) As String
                If data Is Nothing Then Return ""
                Dim c(data.Length - 1) As Char
                For i = 0 To data.Length - 1
                    If data(i) = 0 Then
                        ReDim Preserve c(i - 1)
                        Return New String(c)
                    End If
                    c(i) = ChrW(data(i))
                Next
                Return New String(c)
            End Function
            'Similar to above, but only converts bytes from the ASCII table
            Public Function ToChrString_OnlyAscii(data() As Byte) As String
                Dim s As String = ""
                For Each b In data
                    If b >= 32 And b <= 126 Then
                        s &= ChrW(b)
                    End If
                Next
                Return s
            End Function

            Public Function ToUTF8(data() As Byte) As String
                Dim utf8 As New System.Text.UTF8Encoding
                Dim decodedString As String = utf8.GetString(data)
                Return decodedString
            End Function

            Public Function ToChrString(data() As Byte, StartIndex As Integer) As String
                If StartIndex > data.Length - 1 Then Return ""
                Dim StrOut As String = ""
                For i = StartIndex To data.Length - 1
                    If data(i) = 0 Then Return StrOut
                    StrOut &= ChrW(data(i))
                Next
                Return StrOut
            End Function

            Public Function ToBoolean(data() As Byte) As String
                If data IsNot Nothing OrElse Not data.Length = 1 Then
                    If data(0) = 1 Then Return "True"
                End If
                Return "False"
            End Function

            Public Function ToNumericValue(data() As Byte) As UInt64
                If data Is Nothing Then Return 0
                Dim largNumber As UInt64
                For i = 0 To data.Length - 1
                    Dim b As Byte = data(data.Length - (1 + i))
                    largNumber += CULng(b) << (i * 8)
                Next
                Return largNumber
            End Function
            'Converts a data array {00,01,02} to its hexstring "000102"
            Public Function ToHexString(bytes_Input() As Byte) As String
                Dim strTemp As New Text.StringBuilder(bytes_Input.Length * 2)
                For Each b As Byte In bytes_Input
                    strTemp.Append(b.ToString("X").PadLeft(2, "0"c))
                Next
                Return strTemp.ToString()
            End Function
            'Converts a data array {00,01,02} to its padded hexstring "00 01 02"
            Public Function ToPaddedHexString(data() As Byte) As String
                If data Is Nothing OrElse data.Length = 0 Then Return ""
                Dim c((data.Length * 2) + (data.Length - 1) - 1) As Char
                Dim counter As Integer = 0
                For i = 0 To data.Length - 2
                    Dim b As Byte = data(i)
                    c(counter) = GetByteChar(b >> 4)
                    c(counter + 1) = GetByteChar(b)
                    c(counter + 2) = " "c
                    counter += 3
                Next
                Dim last_byte As Byte = data(data.Length - 1)
                c(counter) = GetByteChar(last_byte >> 4)
                c(counter + 1) = GetByteChar(last_byte)
                Return New String(c)
            End Function

            Public Function ToCharStringArray(data() As Byte) As String()
                Dim file_out As New List(Of String)
                Using mem_reader As New IO.MemoryStream(data)
                    Using str_reader As New IO.StreamReader(mem_reader)
                        Do Until str_reader.Peek = -1
                            file_out.Add(str_reader.ReadLine)
                        Loop
                    End Using
                End Using
                Return file_out.ToArray
            End Function
            'Converts a byte() into uint() padds the last element with 00s
            Public Function ToUIntArray(data() As Byte) As UInt32()
                Dim i As Integer
                Do Until (data.Length Mod 4) = 0
                    ReDim Preserve data(data.Length)
                Loop
                Dim NumOfWords As Integer = CInt(data.Length / 4)
                Dim ret(NumOfWords - 1) As UInt32
                Dim sVal As UInt32
                Dim ival As UInt32
                For i = 0 To NumOfWords - 1
                    Dim s As Integer = i * 4
                    sVal = CUInt(data(s)) << 24
                    ival = data(s + 1)
                    sVal += (ival << 16)
                    ival = data(s + 2)
                    sVal += (ival << 8)
                    sVal += data(s + 3)
                    ret(i) = sVal
                Next
                Return ret
            End Function

            Private Function GetByteChar(n As Byte) As Char
                n = CByte((n And &HF)) 'We are only converting first 4 bits
                If n < 10 Then
                    Return ChrW(48 + n) '0-9
                Else
                    Return ChrW(55 + n) 'A-F
                End If
            End Function

#End Region

        End Module

    End Namespace

    Namespace IsDataType

        Friend Module IsDataType
            Public Function Bool(Input As String) As Boolean
                If Input.ToUpper.Equals("TRUE") OrElse Input.ToUpper.Equals("FALSE") Then Return True
                Return False
            End Function

            Public Function [Integer](Input As String) As Boolean
                Return Int32.TryParse(Input, Nothing)
            End Function

            Public Function [Uinteger](Input As String) As Boolean
                Return UInt32.TryParse(Input, Nothing)
            End Function

            Public Function [UInteger64](Input As String) As Boolean
                Return UInt64.TryParse(Input, Nothing)
            End Function

            Public Function IPv4(ipv4_str As String) As Boolean
                Dim p As Net.IPAddress = Nothing
                If Not Net.IPAddress.TryParse(ipv4_str, p) Then Return False
                If Not p.AddressFamily = Net.Sockets.AddressFamily.InterNetwork Then Return False
                Return True
            End Function

            Public Function IPv6(ipv6_str As String) As Boolean
                Dim p As Net.IPAddress = Nothing
                If Not Net.IPAddress.TryParse(ipv6_str, p) Then Return False
                If Not p.AddressFamily = Net.Sockets.AddressFamily.InterNetworkV6 Then Return False
                Return True
            End Function

            Public Function [String](input As String) As Boolean
                If input Is Nothing Then Return False
                If input = "" Then Return False
                If input.StartsWith("""") And input.EndsWith("""") Then Return True
                Return False
            End Function

            Public Function [IpAddress](input As String) As Boolean
                Dim addr As Net.IPAddress = Nothing
                If Net.IPAddress.TryParse(input, addr) Then
                    Return True
                Else
                    Return False 'Not a valid IP
                End If
            End Function

            Public Function HexString(inputhex As String) As Boolean
                Try
                    inputhex = inputhex.Replace(" ", "")
                    If inputhex.ToUpper.StartsWith("0X") Then
                        inputhex = inputhex.Substring(2)
                    ElseIf inputhex.ToUpper.EndsWith("H") Then
                        inputhex = inputhex.Substring(0, inputhex.Length - 1)
                    End If
                    If inputhex = "" Then Return False
                    Return [Hex](inputhex)
                Catch ex As Exception
                    Return False
                End Try
            End Function

            Public Function [Hex](input As String) As Boolean
                If input.ToUpper().StartsWith("0X") Then input = input.Substring(2)
                Dim i As Integer
                For i = 0 To input.Length - 1
                    Dim c As Char = CChar(input.Substring(i, 1).ToUpper())
                    If Not IsNumeric(c) Then
                        Select Case c
                            Case "A"c
                            Case "B"c
                            Case "C"c
                            Case "D"c
                            Case "E"c
                            Case "F"c
                            Case Else
                                Return False
                        End Select
                    End If
                Next
                Return True
            End Function

        End Module

    End Namespace

    Namespace FileIO
        Friend Module ReadWriteFunctions
            Public Function ReadFile(fileName As String) As String()
                Try
                    Dim local_file As New IO.FileInfo(fileName)
                    If Not local_file.Exists Then Return Nothing
                    Dim file_out As New List(Of String)
                    Using file_reader As IO.StreamReader = local_file.OpenText
                        Do Until file_reader.Peek = -1
                            file_out.Add(file_reader.ReadLine)
                        Loop
                        file_reader.Close()
                    End Using
                    If file_out.Count = 0 Then Return Nothing
                    Return file_out.ToArray
                Catch ex As Exception
                    Return Nothing
                End Try
            End Function

            Public Function WriteFile(FileOut() As String, FileName As String) As Boolean
                Try
                    Dim local_file As New IO.FileInfo(FileName)
                    If local_file.Exists Then local_file.Delete()
                    Dim local_dir As New IO.DirectoryInfo(local_file.DirectoryName)
                    If (Not local_dir.Exists) Then local_dir.Create()
                    Using file_writer As New IO.StreamWriter(local_file.FullName, True, Text.Encoding.ASCII, 2048)
                        For Each Line As String In FileOut
                            If Line.Length = 0 OrElse Line = "" Then
                                file_writer.WriteLine()
                            Else
                                file_writer.WriteLine(Line)
                            End If
                        Next
                        file_writer.Flush()
                    End Using
                    Return True
                Catch ex As Exception
                    Return False
                End Try
            End Function

            Public Function AppendFile(FileOut() As String, FileName As String) As Boolean
                Try
                    Dim local_file As New IO.FileInfo(FileName)
                    Dim local_dir As New IO.DirectoryInfo(local_file.DirectoryName)
                    If Not local_dir.Exists Then local_dir.Create()
                    If FileOut Is Nothing Then Return True
                    Using file_writer As IO.StreamWriter = local_file.AppendText
                        For Each Line In FileOut
                            If Line.Length = 0 Then file_writer.WriteLine() Else file_writer.WriteLine(Line)
                        Next
                        file_writer.Close()
                    End Using
                    Return True
                Catch ex As Exception
                    Return False
                End Try
            End Function

            Public Function AppendBytes(DataOut() As Byte, FileName As String) As Boolean
                Try
                    Dim local_file As New IO.FileInfo(FileName)
                    Dim local_dir As New IO.DirectoryInfo(local_file.DirectoryName)
                    If (Not local_dir.Exists) Then local_dir.Create()
                    If DataOut Is Nothing OrElse DataOut.Length = 0 Then Return True
                    Using file_writer As IO.FileStream = local_file.OpenWrite()
                        file_writer.Position = local_file.Length
                        file_writer.Write(DataOut, 0, DataOut.Length)
                        file_writer.Flush()
                    End Using
                    Return True
                Catch ex As Exception
                    Return False
                End Try
            End Function

            Public Function ReadBytes(FileName As String, Optional MaximumSize As Integer = 0) As Byte()
                Try
                    Dim local_file As New IO.FileInfo(FileName)
                    If (Not local_file.Exists) Or (local_file.Length = 0) Then Return Nothing
                    Dim BytesOut() As Byte
                    If MaximumSize > 0 Then
                        ReDim BytesOut(MaximumSize - 1)
                    Else
                        ReDim BytesOut(CInt(local_file.Length) - 1)
                    End If
                    Using file_reader As New IO.BinaryReader(local_file.OpenRead)
                        For i As Integer = 0 To BytesOut.Length - 1
                            BytesOut(i) = file_reader.ReadByte
                        Next
                        file_reader.Close()
                    End Using
                    Return BytesOut
                Catch ex As Exception
                    Return Nothing
                End Try
            End Function

            Public Function WriteBytes(DataOut() As Byte, FileName As String) As Boolean
                Try
                    Dim local_file As New IO.FileInfo(FileName)
                    If local_file.Exists Then local_file.Delete()
                    Dim local_dir As New IO.DirectoryInfo(local_file.DirectoryName)
                    If Not local_dir.Exists Then local_dir.Create()
                    Using file_writer As IO.FileStream = local_file.OpenWrite()
                        file_writer.Write(DataOut, 0, DataOut.Length)
                        file_writer.Flush()
                    End Using
                    Return True
                Catch ex As Exception
                    Return False
                End Try
            End Function

        End Module

    End Namespace

    Friend Module Main

        Public Sub FillByteArray(ByRef data() As Byte, value As Byte)
            If data Is Nothing Then Exit Sub
            For i = 0 To data.Length - 1
                data(i) = value
            Next
        End Sub

        Public Function IsByteArrayFilled(ByRef data() As Byte, value As Byte) As Boolean
            If data Is Nothing Then Return False
            For Each d In data
                If (Not d = value) Then Return False
            Next
            Return True
        End Function

        Public Function RemoveQuotes(input As String) As String
            If input.StartsWith("""") AndAlso input.EndsWith("""") Then
                Return input.Substring(1, input.Length - 2)
            End If
            Return input
        End Function

        Public Function HasQuotes(input As String) As Boolean
            If input.StartsWith("""") AndAlso input.EndsWith("""") Then Return True
            Return False
        End Function

        Public Function AddQuotes(input As String) As String
            Return """" & input & """"
        End Function

        Public Function Parse_FromSpecifiedData(user_input As String) As Byte()
            If (user_input.StartsWith("'") And user_input.ToUpper.EndsWith("'H")) AndAlso IsDataType.HexString(user_input.Substring(1, user_input.Length - 4)) Then
                Return Utilities.Bytes.FromHexString(user_input.Substring(1, user_input.Length - 3))
            ElseIf (user_input.StartsWith("'") And user_input.ToUpper.EndsWith("'B")) AndAlso IsDataType.HexString(user_input.Substring(1, user_input.Length - 4)) Then
                Dim bitstr As String = user_input.Substring(1, user_input.Length - 3)
                Return Utilities.Bytes.FromBitstring(bitstr)
            End If
            Return Nothing
        End Function

        Public Function DisplayHint_ParseFromInteger(pattern As String, data() As Byte) As String
            Try
                Dim pattern_chars() As Char = pattern.ToLower.ToCharArray
                If pattern_chars.Length > 3 Then Return "" 'can only be max len of 3
                If data.Length > 4 Then Return "" 'can only be 4 bytes or less
                Select Case pattern_chars(0)
                    Case "x"c
                        If data Is Nothing Then data = {0}
                        Dim output As Integer = Bytes.ToInt32(data)
                        Return Pad(Hex(output))
                    Case "d"c
                        If data Is Nothing Then data = {0}
                        Dim decimal_point As Integer = 0
                        If pattern_chars.Length > 2 AndAlso pattern_chars(1) = "-" Then
                            decimal_point = CInt(CStr(pattern_chars(2)))
                        End If
                        Dim output As String = (Bytes.ToInt32(data)).ToString
                        If decimal_point > 0 Then
                            Do Until decimal_point < output.Length
                                output = "0" & output
                            Loop
                            output = StrReverse(output)
                            output = output.Insert(decimal_point, ".")
                            output = StrReverse(output)
                        End If
                        Return output
                    Case "o"c
                        If data Is Nothing Then data = {0}
                        Dim output As UInteger = Bytes.ToUInt32(data)
                        Return Convert.ToString(output, 8)
                    Case "b"c
                        If data Is Nothing Then data = {0}
                        Dim output As UInteger = Bytes.ToUInt32(data)
                        Return Convert.ToString(output, 2)
                End Select
            Catch ex As Exception
            End Try
            Return ""
        End Function

        Public Function DisplayHint_ParseToInteger(pattern As String, input As String) As Byte()
            Dim pattern_chars() As Char = pattern.ToLower.ToCharArray
            If pattern_chars.Length > 3 Then Return Nothing 'can only be max len of 3
            Select Case pattern_chars(0)
                Case "x"c
                    If IsDataType.Hex(input) Then
                        Return Bytes.FromHexString(input)
                    Else
                        Return Nothing
                    End If
                Case "d"c
                    Dim decimal_point As Integer = 0
                    If pattern_chars.Length > 2 AndAlso pattern_chars(1) = "-" Then
                        decimal_point = CInt(CStr(pattern_chars(2)))
                    End If
                    If Not IsNumeric(input) Then Return Nothing 'We can only parse an numeric value
                    If decimal_point > 0 Then
                        input = StrReverse(input)
                        Dim offset As Integer = input.IndexOf("."c)
                        If offset = -1 Then 'value contains no decimal
                            input = input.Substring(0, decimal_point) & input.Substring(decimal_point + 1)
                        Else
                            If offset > decimal_point Then Return Nothing
                            Do Until offset = decimal_point
                                input = "0" & input
                                offset = input.IndexOf("."c)
                            Loop
                        End If
                        input = StrReverse(input)
                    End If
                    input = input.Replace(".", "")
                    Return Bytes.FromInt32(Integer.Parse(input)) 'Allow smaller integer? sure, for now
                Case "o"c
                    If Not IsNumeric(input) Then Return Nothing 'We can only parse an numeric value
                    Dim output As Int32 = Convert.ToInt32(input, 8)
                    Return Bytes.FromInt32(output)
                Case "b"c
                    If Not IsBinary(input) Then Return Nothing
                    Dim output As UInt32 = Convert.ToUInt32(input, 2)
                    Return Bytes.FromUInt32(output)
            End Select
            Return Nothing
        End Function

        Public Function DisplayHint_ParseToOctetString(pattern As String, input As String) As Byte()
            Try
                Dim data_out As New List(Of Byte)
                Dim pattern_repeat As Integer = 1
                Dim repeat_term As Boolean = False
                If pattern.StartsWith("*") Then
                    data_out.Add(CByte(0)) 'Holder for repeat holder
                    pattern = pattern.Substring(1) 'Removes the *
                    repeat_term = True
                End If
                Dim pattern_pointer As Integer = 0 ' to the end of pattern
                Do Until input = ""
                    Dim octetsize_str As String = ""
                    Do Until Not IsNumeric(pattern.Substring(pattern_pointer, 1))
                        octetsize_str &= pattern.Substring(pattern_pointer, 1)
                        pattern_pointer += 1
                    Loop
                    If octetsize_str.Equals("") Then octetsize_str = "0"
                    Dim pattern_cmd As String = pattern.Substring(pattern_pointer, 1)
                    pattern_pointer += 1 : If pattern_pointer > pattern.Length Then pattern_pointer = 1
                    Dim requested_size As Integer = CInt(octetsize_str)
                    If requested_size > 0 Then
                        Select Case pattern_cmd
                            Case "x" 'hexdecimal
                                '1080:0:0:0:8:800:200C:417A
                                Dim HexStr As String = ""
                                For i = 1 To (requested_size * 2)
                                    HexStr &= input.Substring(0, 1)
                                    input = input.Substring(1)
                                    If input.Equals("") Then Exit Do
                                    If Not IsDataType.Hex(HexStr & input.Substring(0, 1)) Then Exit For
                                Next
                                Dim d() As Byte = Bytes.FromHexString(HexStr)
                                If Not d.Length = requested_size Then
                                    Dim c(requested_size - 1) As Byte
                                    For i = 0 To d.Length - 1
                                        c(c.Length - d.Length + i) = d(i)
                                    Next
                                    d = c
                                End If
                                data_out.AddRange(d)
                            Case "d"  'decimal
                                Dim d_value As String = ""
                                Do Until (input.Equals("")) OrElse (Not IsNumeric(input.Substring(0, 1)))
                                    d_value &= input.Substring(0, 1)
                                    input = input.Substring(1)
                                Loop
                                If String.IsNullOrEmpty(d_value) Then Return Nothing
                                Dim data() As Byte = Nothing
                                Select Case requested_size
                                    Case 1
                                        data = {Byte.Parse(d_value)}
                                    Case 2
                                        data = Bytes.FromUInt16(CUShort(d_value))
                                    Case 3
                                        data = Bytes.FromUInt24(CUInt(d_value))
                                    Case 4
                                        data = Bytes.FromUInt32(CUInt(d_value))
                                    Case Else
                                        Return Nothing 'To large!
                                End Select
                                For Each b In data
                                    data_out.Add(b)
                                Next
                            Case "o" 'octet
                                Dim d_value As String = ""
                                Do Until (input.Equals("")) OrElse (Not IsNumeric(input.Substring(0, 1)))
                                    d_value &= input.Substring(0, 1)
                                    input = input.Substring(1)
                                Loop
                                If d_value = "" Then Return Nothing
                                Dim data() As Byte = Nothing
                                Dim output As UInt32 = Convert.ToUInt32(d_value, 8)
                                Select Case requested_size
                                    Case 1
                                        data = {Byte.Parse(d_value)}
                                    Case 2
                                        data = Bytes.FromUInt16(CUShort(output))
                                    Case 3
                                        data = Bytes.FromUInt24(output)
                                    Case 4
                                        data = Bytes.FromUInt32(output)
                                    Case Else
                                        Return Nothing 'To large!
                                End Select
                                For Each b In data
                                    data_out.Add(b)
                                Next
                            Case "a" 'ascii
                                For i = 1 To requested_size
                                    Dim c As Char = CChar(input.Substring(0, 1))
                                    input = input.Substring(1)
                                    data_out.Add(CByte(AscW(c)))
                                    If input = "" Then Exit For
                                Next
                            Case "t" 'UTF-8
                                For i = 1 To requested_size
                                    Dim c As Char = CChar(input.Substring(0, 1))
                                    input = input.Substring(1)
                                    data_out.Add(CByte(AscW(c)))
                                    If input = "" Then Exit For
                                Next
                        End Select
                    End If
                    If (Not IsNumeric(pattern.Substring(pattern_pointer, 1))) And (Not input.Equals("")) Then 'display separator character (optional)
                        Dim sep_char As String = pattern.Substring(pattern_pointer, 1)
                        pattern_pointer += 1 : If (pattern_pointer > pattern.Length) Then pattern_pointer = 1
                        If pattern_pointer > 1 And (Not IsNumeric(pattern.Substring(pattern_pointer, 1))) Then
                            Dim term_char As String = pattern.Substring(pattern_pointer, 1)
                            pattern_pointer += 1 : If (pattern_pointer > pattern.Length) Then pattern_pointer = 1
                            If sep_char = input.Substring(0, 1) Then
                                pattern_repeat = data_out.Count - 1
                                input = input.Substring(1) 'Removes the seperation character
                            ElseIf term_char = input.Substring(0, 1) Then
                                input = input.Substring(1) 'Removes the seperation character
                            Else
                                Return Nothing 'error
                            End If
                        Else
                            If Not sep_char = input.Substring(0, 1) Then Return Nothing
                            input = input.Substring(1) 'Removes the seperation character 
                        End If
                    End If
                Loop
                If repeat_term Then data_out.Item(0) = CByte(pattern_repeat And 255)
                Return data_out.ToArray
            Catch ex As Exception
            End Try
            Return Nothing
        End Function

        Public Function IsBinary(bin_str As String) As Boolean
            For Each c As Char In bin_str
                If c.Equals("1"c) Then
                ElseIf c.Equals("0"c) Then
                Else
                    Return False
                End If
            Next
            Return True
        End Function

        Public Function DisplayHint_ParseFromOctetString(pattern As String, data() As Byte, ByRef patterns As List(Of Char)) As String
            'http://www.freesoft.org/CIE/RFC/1903/5.htm
            Try
                patterns = New List(Of Char)
                Dim str_out As String = ""
                Dim pattern_repeat As Integer = 1
                Dim data_pointer As Integer = 0
                Dim repeat_term As Boolean = False
                If pattern.StartsWith("*") Then
                    pattern_repeat = data(data_pointer)
                    pattern = pattern.Substring(1) 'Removes the *
                    data_pointer += 1
                    repeat_term = True
                End If
                Dim pattern_pointer As Integer = 0 ' to the end of pattern
                Do
                    Dim octetsize_str As String = ""
                    Do Until Not IsNumeric(pattern.Substring(pattern_pointer, 1))
                        octetsize_str &= pattern.Substring(pattern_pointer, 1)
                        pattern_pointer += 1
                    Loop
                    If octetsize_str = "" Then octetsize_str = "0"
                    Dim pattern_cmd As Char = CChar(pattern.Substring(pattern_pointer, 1))
                    patterns.Add(pattern_cmd)
                    pattern_pointer += 1 : If pattern_pointer > pattern.Length Then pattern_pointer = 1 : pattern_repeat -= 1
                    Dim requested_size As Integer = CInt(octetsize_str)
                    If requested_size > 0 Then
                        If requested_size + data_pointer > data.Length Then
                            requested_size = data.Length - data_pointer
                        End If
                        Dim data_to_parse(requested_size - 1) As Byte
                        Array.Copy(data, data_pointer, data_to_parse, 0, data_to_parse.Length)
                        data_pointer += data_to_parse.Length
                        Select Case pattern_cmd
                            Case "x"c 'hexdecimal
                                str_out &= Bytes.ToHexString(data_to_parse)
                            Case "d"c 'decimal
                                str_out &= Bytes.ToUInt32(data_to_parse)
                            Case "o"c 'octet
                                Dim output As UInteger = Bytes.ToUInt32(data_to_parse)
                                str_out &= Convert.ToString(output, 8)
                            Case "a"c 'ascii
                                str_out &= Bytes.ToChrString(data_to_parse)
                            Case "t"c 'UTF-8
                                str_out &= Bytes.ToUTF8(data_to_parse)
                        End Select
                    End If
                    If (Not IsNumeric(pattern.Substring(pattern_pointer, 1))) Then 'display separator character (optional)
                        Dim sep_char As String = pattern.Substring(pattern_pointer, 1)
                        pattern_pointer += 1 : If (pattern_pointer > pattern.Length) Then pattern_pointer = 1 : pattern_repeat -= 1
                        If pattern_pointer > 1 And (Not IsNumeric(pattern.Substring(pattern_pointer, 1))) Then
                            Dim term_char As String = pattern.Substring(pattern_pointer, 1)
                            pattern_pointer += 1 : If (pattern_pointer > pattern.Length) Then pattern_pointer = 1 : pattern_repeat -= 1
                            If repeat_term And pattern_repeat = 0 Then
                                str_out &= term_char
                            Else
                                If Not data_pointer = data.Length Then str_out &= sep_char
                            End If
                        Else
                            If Not data_pointer = data.Length Then str_out &= sep_char
                        End If
                    End If
                Loop Until data_pointer = data.Length 'Or pattern_repeat = 0
                Return str_out
            Catch ex As Exception
            End Try
            Return ""
        End Function

        Public Function DecompressGzip(CompressedData() As Byte) As Byte()
            Try
                Using stream_out As IO.MemoryStream = New IO.MemoryStream
                    Using memory As IO.MemoryStream = New IO.MemoryStream(CompressedData)
                        Using gzip As IO.Compression.GZipStream = New IO.Compression.GZipStream(memory, IO.Compression.CompressionMode.Decompress, True)
                            Dim buffer() As Byte = New Byte(4095) {}
                            While True
                                Dim size = gzip.Read(buffer, 0, buffer.Length)
                                If size > 0 Then
                                    stream_out.Write(buffer, 0, size)
                                Else
                                    Exit While
                                End If
                            End While
                        End Using
                    End Using
                    Return stream_out.ToArray()
                End Using
            Catch ex As Exception
                Return Nothing
            End Try
        End Function

        Public Function CompressGzip(UncompressedData() As Byte) As Byte()
            Using memory As IO.MemoryStream = New IO.MemoryStream()
                Using gzip As IO.Compression.GZipStream = New IO.Compression.GZipStream(memory, IO.Compression.CompressionMode.Compress, True)
                    gzip.Write(UncompressedData, 0, UncompressedData.Length)
                End Using
                Return memory.ToArray()
            End Using
        End Function

        Public Class CRC32
            Shared table As UInteger()

            Shared Sub New()
                Dim poly As UInteger = &HEDB88320UI
                table = New UInteger(255) {}
                Dim temp As UInt32 = 0
                For i As Integer = 0 To table.Length - 1
                    temp = CUInt(i)
                    For j As Integer = 8 To 1 Step -1
                        If (temp And 1) = 1 Then
                            temp = CUInt((temp >> 1) Xor poly)
                        Else
                            temp >>= 1
                        End If
                    Next
                    table(i) = temp
                Next
            End Sub

            Public Shared Function ComputeChecksum(bytes As Byte()) As UInteger
                Dim crc As UInteger = &HFFFFFFFFUI
                For i As Integer = 0 To bytes.Length - 1
                    Dim index As Byte = CByte(((crc) And &HFF) Xor bytes(i))
                    crc = CUInt((crc >> 8) Xor table(index))
                Next
                Return Not crc
            End Function

        End Class

        Public Class CRC16

            Shared table As UShort()

            Shared Sub New()
                Dim poly As UShort = &HA001US 'calculates CRC-16 using A001 polynomial (modbus)
                table = New UShort(255) {}
                Dim temp As Integer = 0
                For i As Integer = 0 To table.Length - 1
                    temp = i
                    For j As Integer = 8 To 1 Step -1
                        If (temp And 1) = 1 Then
                            temp = CUShort((temp >> 1) Xor poly)
                        Else
                            temp >>= 1
                        End If
                    Next
                    table(i) = CUShort(temp)
                Next
            End Sub

            Public Shared Function ComputeChecksum(bytes As Byte()) As UShort
                Dim crc As UShort = &H0US ' The calculation start with 0x00
                For i As Integer = 0 To bytes.Length - 1
                    Dim index As Byte = CByte(((crc) And &HFF) Xor bytes(i))
                    crc = CUShort((crc >> 8) Xor table(index))
                Next
                Return Not crc
            End Function

        End Class


#Region "MAC Tools"

        Public Function IsMacMatch(ByRef MacOne() As Byte, ByRef MacTwo() As Byte) As Boolean
            If MacOne Is Nothing Then Return False
            If MacTwo Is Nothing Then Return False
            Try
                For i = 0 To 5
                    If Not MacOne(i) = MacTwo(i) Then Return False
                Next
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function
        'Returns true if Mac is 00:01:02:03:04:05
        Public Function IsMacInStandardFormat(MacInput As String) As Boolean
            If Not MacInput.Contains(":") Then Return False
            Dim MacParts() As String = MacInput.Split(":"c)
            If Not MacParts.Length = 6 Then Return False
            For i = 0 To 5
                If Not MacParts(i).Length = 2 Then Return False
                If Not IsDataType.Hex(MacParts(i)) Then Return False
            Next
            Return True
        End Function
        'Returns true if Mac is 0001.0203.0405
        Public Function IsMacInCiscoFormat(MacInput As String) As Boolean
            If MacInput.Contains(":") Then Return False
            If Not MacInput.Length = 14 Then Return False
            Dim Part1 As String = MacInput.Substring(0, 4)
            Dim Part2 As String = MacInput.Substring(5, 4)
            Dim Part3 As String = MacInput.Substring(10, 4)
            If Not IsDataType.Hex(Part1) Then Return False
            If Not IsDataType.Hex(Part2) Then Return False
            If Not IsDataType.Hex(Part3) Then Return False
            If Not MacInput.Substring(4, 1).Equals(".") Then Return False
            If Not MacInput.Substring(9, 1).Equals(".") Then Return False
            Return True
        End Function
        'Returns true if Mac is 000102030405
        Public Function IsMacInOtherFormat(MacInput As String) As Boolean
            If MacInput.Contains(":") Then Return False
            If MacInput.Length = 12 And IsDataType.Hex(MacInput) Then Return True
            If MacInput.Length = 12 And IsDataType.Hex(MacInput) Then Return True
            Return False
        End Function

        'Returns MAC input as 00:01:02:03:04:05
        Public Function MacToStandardFormat(MacInput As String) As String
            Dim raw_mac As String = MacInput.Replace(":", "").Replace(".", "")
            Dim c() As Char = raw_mac.ToCharArray
            Return c(0) & c(1) & ":" & c(2) & c(3) & ":" & c(4) & c(5) & ":" & c(6) & c(7) & ":" & c(8) & c(9) & ":" & c(10) & c(11)
        End Function
        'Returns MAC input as 0001.0203.0405
        Public Function MacToCiscoFormat(NormalMAC As String) As String
            Dim raw_mac As String = NormalMAC.Replace(":", "").Replace(".", "")
            Return raw_mac.Substring(0, 4) & "." & raw_mac.Substring(4, 4) & "." & raw_mac.Substring(8, 4)
        End Function
        'Returns it as 000102030405 
        Public Function MacToPlainFormat(MacString As String) As String
            Dim ret As String = MacString.Replace(":", "")
            Return ret.Replace(".", "")
        End Function
        'Returns 0001.0203.0405
        Public Function MacToCiscoFormat(Mac() As Byte) As String
            If Mac Is Nothing OrElse Mac.Length <> 6 Then Return "0000.0000.0000"
            Dim RetStr As String
            RetStr = Hex(Mac(0)).PadLeft(2, "0"c) & Hex(Mac(1)).PadLeft(2, "0"c) & "."
            RetStr &= Hex(Mac(2)).PadLeft(2, "0"c) & Hex(Mac(3)).PadLeft(2, "0"c) & "."
            RetStr &= Hex(Mac(4)).PadLeft(2, "0"c) & Hex(Mac(5)).PadLeft(2, "0"c)
            Return RetStr
        End Function
        'Returns 00:01:02:03:04:05
        Public Function MacToBasicFormat(Mac() As Byte) As String
            If Mac Is Nothing OrElse Mac.Length <> 6 Then Return "00:00:00:00:00:00"
            Dim s As String = ""
            For i = 0 To 5
                s &= Hex(Mac(i)).PadLeft(2, "0"c) & ":"
            Next
            Return s.Substring(0, s.Length - 1).ToUpper()
        End Function
        'Returns 000102030405
        Public Function MacToPlainFormat(mac() As Byte) As String
            If mac Is Nothing OrElse mac.Length <> 6 Then Return "000000000000"
            Dim s As String = ""
            For i = 0 To 5
                s &= CStr(Hex(mac(i))).PadLeft(2, "0"c)
            Next
            Return s
        End Function
        'Takes an input (00:01:02:03:04:05) or (0001.0203.0405) or (000102030405) to data 
        Public Function MacStringToBytes(MacInput As String) As Byte()
            Dim s As String = MacInput.Replace(":", "").Replace(".", "")
            If Not s.Length = 12 Then Return Nothing 'Not valid input
            Dim b(5) As Byte
            b(0) = CByte(HexToInt(s.Substring(0, 2)))
            b(1) = CByte(HexToInt(s.Substring(2, 2)))
            b(2) = CByte(HexToInt(s.Substring(4, 2)))
            b(3) = CByte(HexToInt(s.Substring(6, 2)))
            b(4) = CByte(HexToInt(s.Substring(8, 2)))
            b(5) = CByte(HexToInt(s.Substring(10, 2)))
            Return b
        End Function

#End Region

#Region "ArrayHelpers"

        'Copies on array to the end of the destination array (for byte arrays)
        Public Function ArrayCopy(ByRef DestinationArray() As Byte, ToCopyArray() As Byte) As Byte()
            If DestinationArray Is Nothing Then
                DestinationArray = CType(ToCopyArray.Clone, Byte())
            Else
                Dim nArray(DestinationArray.Length + ToCopyArray.Length - 1) As Byte
                DestinationArray.CopyTo(nArray, 0)
                ToCopyArray.CopyTo(nArray, DestinationArray.Length)
                DestinationArray = nArray
            End If
            Return DestinationArray
        End Function
        'Copies on array to the end of the destination array (for io.filename arrays)
        Public Function ArrayCopy(ByRef DestinationArray() As IO.FileInfo, ToCopyArray() As IO.FileInfo) As IO.FileInfo()
            If DestinationArray Is Nothing Then
                DestinationArray = CType(ToCopyArray.Clone, IO.FileInfo())
            Else
                Dim nArray(DestinationArray.Length + ToCopyArray.Length - 1) As IO.FileInfo
                DestinationArray.CopyTo(nArray, 0)
                ToCopyArray.CopyTo(nArray, DestinationArray.Length)
                DestinationArray = nArray
            End If
            Return DestinationArray
        End Function
        'Copies on array to the end of the destination array (for int arrays)
        Public Function ArrayCopy(ByRef DestinationArray() As Integer, ToCopyArray() As Integer) As Integer()
            If DestinationArray Is Nothing Then
                DestinationArray = CType(ToCopyArray.Clone, Integer())
            Else
                Dim nArray(DestinationArray.Length + ToCopyArray.Length - 1) As Integer
                DestinationArray.CopyTo(nArray, 0)
                ToCopyArray.CopyTo(nArray, DestinationArray.Length)
                DestinationArray = nArray
            End If
            Return DestinationArray
        End Function
        'Copies on array to the end of the destination array (for str arrays)
        Public Function ArrayCopy(ByRef DestinationArray() As String, ToCopyArray() As String) As String()
            If DestinationArray Is Nothing Then
                DestinationArray = CType(ToCopyArray.Clone, String())
            Else
                Dim nArray(DestinationArray.Length + ToCopyArray.Length - 1) As String
                DestinationArray.CopyTo(nArray, 0)
                ToCopyArray.CopyTo(nArray, DestinationArray.Length)
                DestinationArray = nArray
            End If
            Return DestinationArray
        End Function

        Public Sub ArrayAdd(ByRef DestinationArray() As UInt32, NewItem As UInt32)
            ReDim Preserve DestinationArray(DestinationArray.Length)
            DestinationArray(DestinationArray.Length - 1) = NewItem
        End Sub

        Public Sub ArrayAdd(ByRef ToAdd() As String, NewItem As String)
            ReDim Preserve ToAdd(ToAdd.Length)
            ToAdd(ToAdd.Length - 1) = NewItem
        End Sub

        Public Function ArraySection(ArrIn() As Byte, Start As Integer, Count As Integer) As Byte()
            Dim out(Count - 1) As Byte
            For i = 0 To Count - 1
                out(i) = ArrIn(i + (Start - 1))
            Next
            Return out
        End Function

        Public Function ArraysMatch(data1() As Byte, data2() As Byte) As Boolean
            If data1 Is Nothing AndAlso data2 Is Nothing Then Return True
            If Not data1.Length = data2.Length Then Return False
            For i As Integer = 0 To data1.Length - 1
                If Not data1(i) = data2(i) Then Return False
            Next
            Return True
        End Function

#End Region

#Region "Type conversions"

        Public Function HexToInt(value As String) As Int32
            Try
                If value.ToUpper.StartsWith("0X") Then value = value.Substring(2)
                If value = "" Then Return 0
                Return Convert.ToInt32(value, 16)
            Catch
                Return 0
            End Try
        End Function

        Public Function HexToUInt(value As String) As UInt32
            Try
                If value.ToUpper.StartsWith("0X") Then value = value.Substring(2)
                If value = "" Then Return 0
                Return Convert.ToUInt32(value, 16)
            Catch
                Return 0
            End Try
        End Function

        Public Function HexToLng(value As String) As Long
            Try
                If value.ToUpper().StartsWith("0X") Then value = value.Substring(2)
                If value = "" Then Return 0
                Return Convert.ToInt64(value, 16)
            Catch
                Return 0
            End Try
        End Function

        Public Function BoolToInt(en As Boolean) As Int32
            If en Then
                Return 1
            Else
                Return 0
            End If
        End Function

        Public Function BitStringToInteger(input As String) As Integer
            input = input.ToUpper().Trim()
            Dim firstpart As String
            If input.EndsWith("MBPS") Then
                firstpart = input.Substring(0, input.IndexOf("MBPS") - 1).Trim()
                If Not IsDataType.Integer(firstpart) Then Return 0
                Return Integer.Parse(firstpart) * 1000000
            ElseIf input.EndsWith("KBPS") Then
                firstpart = input.Substring(0, input.IndexOf("KBPS") - 1).Trim()
                If Not IsDataType.Integer(firstpart) Then Return 0
                Return Integer.Parse(firstpart) * 1000
            ElseIf input.EndsWith("M") Then
                firstpart = input.Substring(0, input.IndexOf("M") - 1).Trim()
                If Not IsDataType.Integer(firstpart) Then Return 0
                Return Integer.Parse(firstpart) * 1000000
            ElseIf input.EndsWith("K") Then
                firstpart = input.Substring(0, input.IndexOf("K") - 1).Trim()
                If Not IsDataType.Integer(firstpart) Then Return 0
                Return Integer.Parse(firstpart) * 1000
            ElseIf input.EndsWith("MB") Then
                firstpart = input.Substring(0, input.IndexOf("MB") - 1).Trim()
                If Not IsDataType.Integer(firstpart) Then Return 0
                Return Integer.Parse(firstpart) * 1000000
            ElseIf input.EndsWith("KB") Then
                firstpart = input.Substring(0, input.IndexOf("KB") - 1).Trim()
                If Not IsDataType.Integer(firstpart) Then Return 0
                Return Integer.Parse(firstpart) * 1000
            Else
                If IsDataType.Integer(input) Then Return Integer.Parse(input)
                Return 0
            End If
        End Function
        'Converts a string that contains a number to single (all region compatible)
        Public Function StringToSingle(input As String) As Single
            Return Convert.ToSingle(input, New Globalization.CultureInfo("en-US"))
        End Function

#End Region

#Region "Endian/Byte/Bit Changing"
        '0x01020304 = 0x03040102
        Public Sub ChangeEndian32_LSB16(ByRef Buffer() As Byte)
            Dim step_value As Integer = 4
            Dim last_index As Integer = Buffer.Length - (Buffer.Length Mod step_value)
            For i As Integer = 0 To last_index - 1 Step step_value
                Dim B1 As Byte = Buffer(i + 3)
                Dim B2 As Byte = Buffer(i + 2)
                Dim B3 As Byte = Buffer(i + 1)
                Dim B4 As Byte = Buffer(i + 0)
                Buffer(i + 3) = B3
                Buffer(i + 2) = B4
                Buffer(i + 1) = B1
                Buffer(i + 0) = B2
            Next
        End Sub
        '0x01020304 = 0x04030201
        Public Sub ChangeEndian32_LSB8(ByRef Buffer() As Byte)
            Dim step_value As Integer = 4
            Dim last_index As Integer = Buffer.Length - (Buffer.Length Mod step_value)
            For i As Integer = 0 To last_index - 1 Step step_value
                Dim B1 As Byte = Buffer(i + 3)
                Dim B2 As Byte = Buffer(i + 2)
                Dim B3 As Byte = Buffer(i + 1)
                Dim B4 As Byte = Buffer(i + 0)
                Buffer(i + 3) = B4
                Buffer(i + 2) = B3
                Buffer(i + 1) = B2
                Buffer(i + 0) = B1
            Next
        End Sub
        '0x01020304 = 0x02010403
        Public Sub ChangeEndian16_MSB(ByRef Buffer() As Byte)
            Dim step_value As Integer = 2
            Dim last_index As Integer = Buffer.Length - (Buffer.Length Mod step_value)
            For i As Integer = 0 To last_index - 1 Step step_value
                Dim b_high As Byte = Buffer(i)
                Dim b_low As Byte = Buffer(i + 1)
                Buffer(i) = b_low
                Buffer(i + 1) = b_high
            Next
        End Sub
        '0b11110000 = 0b00001111
        Public Sub ChangeEndian_Nibble(ByRef Buffer() As Byte)
            For i As Integer = 0 To Buffer.Length - 1
                Dim b As Byte = Buffer(i)
                Buffer(i) = (b << 4) Or (b >> 4)
            Next
        End Sub
        '0b00000001 = 0b10000000 (reversed bit order for 8-bit)
        Public Sub ReverseBits_Byte(ByRef Buffer() As Byte)
            For i As Integer = 0 To Buffer.Length - 1 Step 1
                Dim b As Byte = Buffer(i)
                Dim bo() As Boolean = ByteToBooleanArray({b})
                Array.Reverse(bo)
                Dim out() As Byte = BoolToByteArray(bo)
                Buffer(i) = out(0)
            Next
        End Sub
        '0b0000000100000010 = 0b0100000010000000 (reversed bit order for 16-bit)
        Public Sub ReverseBits_HalfWord(ByRef Buffer() As Byte)
            Dim step_value As Integer = 2
            Dim last_index As Integer = Buffer.Length - (Buffer.Length Mod step_value)
            For i As Integer = 0 To last_index - 1 Step step_value
                Dim high_b As Byte = Buffer(i)
                Dim low_b As Byte = Buffer(i + 1)
                Dim bo() As Boolean = ByteToBooleanArray({high_b, low_b})
                Array.Reverse(bo)
                Dim out() As Byte = BoolToByteArray(bo)
                Buffer(i) = out(0)
                Buffer(i + 1) = out(1)
            Next
        End Sub
        '0b00000001000000100000001100000100 = 0b00100000110000000100000010000000 (reversed bit order for 32-bit)
        Public Sub ReverseBits_Word(ByRef Buffer() As Byte)
            Dim step_value As Integer = 4
            Dim last_index As Integer = Buffer.Length - (Buffer.Length Mod step_value)
            For i As Integer = 0 To last_index - 1 Step step_value
                Dim b1 As Byte = Buffer(i)
                Dim b2 As Byte = Buffer(i + 1)
                Dim b3 As Byte = Buffer(i + 2)
                Dim b4 As Byte = Buffer(i + 3)
                Dim bo() As Boolean = ByteToBooleanArray({b1, b2, b3, b4})
                Array.Reverse(bo)
                Dim out() As Byte = BoolToByteArray(bo)
                Buffer(i) = out(0)
                Buffer(i + 1) = out(1)
                Buffer(i + 2) = out(2)
                Buffer(i + 3) = out(3)
            Next
        End Sub

        Public Sub ReverseBits(ByRef x As UInt32, Optional count As Integer = 32)
            Dim y As UInt32 = 0
            For i = 0 To count - 1
                y <<= 1
                y = y Or (x And 1UI)
                x >>= 1
            Next
            x = y
        End Sub

        Public Sub ReverseBits(ByRef x As UInt64, Optional count As Integer = 64)
            Dim y As UInt64 = 0
            For i = 0 To count - 1
                y <<= 1
                y = y Or (x And 1UI)
                x >>= 1
            Next
            x = y
        End Sub

        Public Sub ReverseBits_ByteEndian(ByRef x As UInt32)
            Dim y As UInt32
            Dim offset As Integer = 7
            For i = 0 To 31
                If ((x And 1UI) = 1) Then
                    y = y Or (1UI << (offset + ((i \ 8) * 8)))
                End If
                offset -= 1
                If offset = -1 Then offset = 7
                x >>= 1
            Next
            x = y
        End Sub

#End Region

        Public Function GetResourceAsBytes(ResourceName As String) As Byte()
            Dim asm_names() As String = Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames()
            Dim ns_embedded_name As String = Nothing     'Namespace.ResourceName
            For Each emb_name As String In asm_names
                If emb_name.ToUpper().EndsWith("." & ResourceName.ToUpper()) Then
                    ns_embedded_name = emb_name
                    Exit For
                End If
            Next
            If String.IsNullOrEmpty(ns_embedded_name) Then Return Nothing
            Dim CurrentAssembly As Reflection.Assembly = Reflection.Assembly.GetCallingAssembly
            Dim resStream As IO.Stream = CurrentAssembly.GetManifestResourceStream(ns_embedded_name)
            If resStream Is Nothing Then Return Nothing
            Dim SizeOfFile As Integer = CInt(resStream.Length)
            Dim data As Byte() = New Byte(SizeOfFile - 1) {}
            resStream.Read(data, 0, data.Length)
            Return data
        End Function
        'Removes a comment from a command line
        Public Function RemoveComment(input As String, Optional comment_char As String = "#") As String
            Try
                Dim ProcessingQuote As Boolean = False
                For i = 0 To input.Length - comment_char.Length
                    If ProcessingQuote Then
                        If input.Substring(i, 1).Equals("""") Then ProcessingQuote = False
                    ElseIf input.Substring(i, 1).Equals("""") Then
                        ProcessingQuote = True
                    ElseIf input.Substring(i, comment_char.Length).Equals(comment_char) Then
                        Return input.Substring(0, i).Trim
                    End If
                Next
            Catch ex As Exception
            End Try
            Return input
        End Function
        'Splits a command line into command parts
        Public Function SplitCmd(cmdStr As String) As String()
            If cmdStr = "" Then Return Nothing
            Dim i As Integer
            Dim partCounter As Integer = -1
            Dim parts(31) As String 'Splits up to 32 parts
            Dim InStr As Boolean = False 'If we are in string 
            Dim InParam As Boolean = False 'If we are in parameter
            Dim MakeString As String = ""
            Dim sChr As Char
            For i = 0 To cmdStr.Length - 1
                sChr = CChar(cmdStr.Substring(i, 1))
                If InStr Then
                    If sChr = """" Then
                        MakeString &= """"
                        InStr = False
                    Else
                        MakeString &= sChr
                    End If
                ElseIf InParam Then
                    If sChr = ")" Then
                        MakeString &= ")"
                        InParam = False
                    Else
                        MakeString &= sChr
                    End If
                Else
                    If sChr = """" Then
                        MakeString &= """"
                        InStr = True
                    ElseIf sChr = "(" Then
                        MakeString &= "("
                        InParam = True
                    ElseIf sChr = " " Then
                        If Not MakeString = "" Then
                            partCounter = partCounter + 1
                            parts(partCounter) = MakeString
                            MakeString = ""
                        End If
                    Else
                        MakeString &= sChr
                    End If
                End If
                If i = cmdStr.Length - 1 Then 'last char
                    If Not MakeString = "" Then
                        partCounter = partCounter + 1
                        parts(partCounter) = MakeString
                        MakeString = ""
                    End If
                End If
            Next
            ReDim Preserve parts(partCounter)
            Return parts
        End Function
        'Gets only the unique elements in the array (this one for Integer)
        Public Function Unique(IntArray() As Integer) As Integer()
            Array.Sort(IntArray)
            Dim counter As Integer = 0
            Dim ret(IntArray.Length - 1) As Integer
            Dim i As Integer
            Dim LastVal As Integer = -1
            For i = 0 To IntArray.Length - 1
                If Not IntArray(i) = LastVal Then
                    ret(counter) = IntArray(i)
                    LastVal = ret(counter)
                    counter = counter + 1
                End If
            Next
            ReDim Preserve ret(counter - 1)
            Return ret
        End Function

        Public Function CharCount(input As String, ChrToCount As Char) As Integer
            Dim Count As Integer = 0
            For Each c As Char In input
                If c = ChrToCount Then Count += 1
            Next
            Return Count
        End Function

        Public Function RandomNumber(MinValue As Integer, Optional MaxValue As Integer = 0) As Integer
            Randomize()
            Static r As New Random()
            Dim outnum As Integer = r.Next(MinValue, MaxValue + 1)
            Return outnum
        End Function

        Public Sub Sleep(miliseconds As Integer)
            Threading.Thread.Sleep(miliseconds)
        End Sub
        '"A" returns "0A"
        Public Function Pad(input As String) As String
            If (Not input.Length Mod 2 = 0) Then
                Return "0" & input
            Else
                Return input
            End If
        End Function

        Public Function ByteToBooleanArray(anyByteArray() As Byte) As Boolean()
            Dim returnedArray() As Boolean
            Dim truthList As New List(Of Boolean)
            If Not anyByteArray Is Nothing Then
                For index As Integer = 0 To anyByteArray.Length - 1
                    truthList.Add(Convert.ToBoolean(anyByteArray(index) And 128))
                    truthList.Add(Convert.ToBoolean(anyByteArray(index) And 64))
                    truthList.Add(Convert.ToBoolean(anyByteArray(index) And 32))
                    truthList.Add(Convert.ToBoolean(anyByteArray(index) And 16))
                    truthList.Add(Convert.ToBoolean(anyByteArray(index) And 8))
                    truthList.Add(Convert.ToBoolean(anyByteArray(index) And 4))
                    truthList.Add(Convert.ToBoolean(anyByteArray(index) And 2))
                    truthList.Add(Convert.ToBoolean(anyByteArray(index) And 1))
                Next
            End If
            returnedArray = truthList.ToArray
            Return returnedArray
        End Function

        Public Function BoolToByteArray(bools() As Boolean) As Byte()
            Dim carry As Integer = bools.Length Mod 8
            If Not carry = 0 Then ReDim Preserve bools(bools.Length + carry - 1) 'Ensures the array.len is even
            Dim data_out As New List(Of Byte)
            For i = 0 To bools.Length - 1 Step 8
                Dim res As Byte = 0
                If bools(i + 0) Then res = CByte(128)
                If bools(i + 1) Then res = CByte(res + 64)
                If bools(i + 2) Then res = CByte(res + 32)
                If bools(i + 3) Then res = CByte(res + 16)
                If bools(i + 4) Then res = CByte(res + 8)
                If bools(i + 5) Then res = CByte(res + 4)
                If bools(i + 6) Then res = CByte(res + 2)
                If bools(i + 7) Then res = CByte(res + 1)
                data_out.Add(res)
            Next
            Return data_out.ToArray
        End Function

        Public Function HWeight32(w As UInteger) As UInteger
            Dim res As UInteger = CUInt((w And &H55555555) + ((w >> 1) And &H55555555))
            res = CUInt((res And &H33333333) + ((res >> 2) And &H33333333))
            res = CUInt((res And &HF0F0F0F) + ((res >> 4) And &HF0F0F0F))
            res = CUInt((res And &HFF00FF) + ((res >> 8) And &HFF00FF))
            Return CUInt((res And &HFFFF) + ((res >> 16) And &HFFFF))
        End Function
        'Adds two U32 and overflows
        Public Sub AddUInt32(ByRef Dest As UInt32, ToAdd As UInt32)
            Dim ultra As UInt64 = Dest 'Larger than 32 bits to hold the overflow
            ultra = ultra + ToAdd 'This overflows the value
            Dest = CUInt((CLng(ultra) And &HFFFFFFFF))
        End Sub

        Public Function ReverseSplit(input() As String, Delimeter As Char) As String
            If input Is Nothing Then Return ""
            If input.Length = 1 Then Return input(0)
            Dim strbuild As String = ""
            For Each item In input
                strbuild &= item & Delimeter
            Next
            Return strbuild.Substring(0, strbuild.Length - 1) 'Removes the last delimeter
        End Function

        Public Function DownloadFile(URL As String) As Byte()
            Try
                Dim myWebClient As New Net.WebClient()
                Dim b() As Byte = myWebClient.DownloadData(URL)
                Return b
            Catch ex As Exception
                Return Nothing
            End Try
        End Function

        Public Function FormatToDataSize(bytes As UInt64) As String
            Dim MB01 As UInt64 = 1048576UL
            Dim GB01 As UInt64 = 1073741824UL
            Dim TB01 As UInt64 = 1099511627776UL
            If (bytes < MB01) Then
                Return FormatToMegabytes(bytes, 2)
            ElseIf (bytes >= MB01) AndAlso (bytes < GB01) Then
                Return FormatToMegabytes(bytes, 2)
            ElseIf (bytes >= GB01) AndAlso (bytes < TB01) Then
                Return FormatToGigabytes(bytes, 2)
            Else
                Return FormatToTerabytes(bytes, 2)
            End If
        End Function

        Public Function FormatToMegabytes(bytes As UInt64, Optional LeadingZeros As Integer = 0) As String
            Dim MB01 As Double = 1048576
            Dim d As Double = (CDbl(bytes) / MB01)
            Return d.ToString("F" & LeadingZeros.ToString, Globalization.CultureInfo.InvariantCulture) & " MB"
        End Function

        Public Function FormatToMegabits(bytes As UInt64, Optional LeadingZeros As Integer = 0) As String
            Dim MB01 As Double = 1048576
            Dim d As Double = (CDbl(bytes) / MB01)
            Dim Mbits As UInt32 = CUInt(d * 8)
            Return Mbits.ToString("F" & LeadingZeros.ToString, Globalization.CultureInfo.InvariantCulture) & " Mbit"
        End Function

        Public Function FormatToKilobytes(bytes As UInt64, Optional LeadingZeros As Integer = 0) As String
            Dim KB01 As Double = 1024
            Dim d As Double = (CDbl(bytes) / KB01)
            Return d.ToString("F" & LeadingZeros.ToString, Globalization.CultureInfo.InvariantCulture) & " KB"
        End Function

        Public Function FormatToGigabytes(bytes As UInt64, Optional LeadingZeros As Integer = 0) As String
            Dim GB01 As Double = 1073741824
            Dim d As Double = (CDbl(bytes) / GB01)
            Return d.ToString("F" & LeadingZeros.ToString, Globalization.CultureInfo.InvariantCulture) & " GBs"
        End Function

        Public Function FormatToTerabytes(bytes As UInt64, Optional LeadingZeros As Integer = 0) As String
            Dim TB01 As Double = 1099511627776
            Dim d As Double = (CDbl(bytes) / TB01)
            Return d.ToString("F" & LeadingZeros.ToString, Globalization.CultureInfo.InvariantCulture) & " TBs"
        End Function
        'Returns the number of bits the input value requires
        Public Function BitSize(input As Long) As Integer
            Dim Counter As Integer = 0
            Do Until input = 0
                input = (input >> 1)
                Counter += 1
            Loop
            Return Counter
        End Function

    End Module

    Namespace Encryption

        Friend Module Bytes

            Public Function AES_Encrypt(input As String, password As String) As String
                Dim AES As New System.Security.Cryptography.RijndaelManaged
                Dim Hash_AES As New System.Security.Cryptography.MD5CryptoServiceProvider
                Dim encrypted As String = ""
                Try
                    Dim hash(31) As Byte
                    Dim temp As Byte() = Hash_AES.ComputeHash(ASCIIEncoding.ASCII.GetBytes(password))
                    Array.Copy(temp, 0, hash, 0, 16)
                    Array.Copy(temp, 0, hash, 15, 16)
                    AES.Key = hash
                    AES.Mode = Security.Cryptography.CipherMode.ECB
                    Dim DESEncrypter As System.Security.Cryptography.ICryptoTransform = AES.CreateEncryptor
                    Dim Buffer As Byte() = ASCIIEncoding.ASCII.GetBytes(input)
                    encrypted = Convert.ToBase64String(DESEncrypter.TransformFinalBlock(Buffer, 0, Buffer.Length))
                    Return encrypted
                Catch ex As Exception
                    Return Nothing
                End Try
            End Function

            Public Function AES_Encrypt(input() As Byte, password As String) As Byte()
                Dim AES As New Security.Cryptography.RijndaelManaged
                Dim Hash_AES As New Security.Cryptography.MD5CryptoServiceProvider
                Try
                    Dim hash(31) As Byte
                    Dim temp As Byte() = Hash_AES.ComputeHash(ASCIIEncoding.ASCII.GetBytes(password))
                    Array.Copy(temp, 0, hash, 0, 16)
                    Array.Copy(temp, 0, hash, 15, 16)
                    AES.Key = hash
                    AES.Mode = Security.Cryptography.CipherMode.ECB
                    Dim DESEncrypter As Security.Cryptography.ICryptoTransform = AES.CreateEncryptor
                    Dim encrypted() As Byte = DESEncrypter.TransformFinalBlock(input, 0, input.Length)
                    Return encrypted
                Catch ex As Exception
                    Return Nothing
                End Try
            End Function

            Public Function AES_Decrypt(input As String, password As String) As String
                Dim AES As New System.Security.Cryptography.RijndaelManaged
                Dim Hash_AES As New System.Security.Cryptography.MD5CryptoServiceProvider
                Try
                    Dim hash(31) As Byte
                    Dim temp As Byte() = Hash_AES.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(password))
                    Array.Copy(temp, 0, hash, 0, 16)
                    Array.Copy(temp, 0, hash, 15, 16)
                    AES.Key = hash
                    AES.Mode = Security.Cryptography.CipherMode.ECB
                    Dim DESDecrypter As System.Security.Cryptography.ICryptoTransform = AES.CreateDecryptor
                    Dim Buffer As Byte() = Convert.FromBase64String(input)
                    Return ASCIIEncoding.ASCII.GetString(DESDecrypter.TransformFinalBlock(Buffer, 0, Buffer.Length))
                Catch ex As Exception
                    Return Nothing
                End Try
            End Function

            Public Function AES_Decrypt(input() As Byte, password As String) As Byte()
                Dim AES As New System.Security.Cryptography.RijndaelManaged
                Dim Hash_AES As New System.Security.Cryptography.MD5CryptoServiceProvider
                Try
                    Dim hash(31) As Byte
                    Dim temp As Byte() = Hash_AES.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(password))
                    Array.Copy(temp, 0, hash, 0, 16)
                    Array.Copy(temp, 0, hash, 15, 16)
                    AES.Key = hash
                    AES.Mode = Security.Cryptography.CipherMode.ECB
                    Dim DESDecrypter As System.Security.Cryptography.ICryptoTransform = AES.CreateDecryptor
                    Dim decrypted() As Byte = DESDecrypter.TransformFinalBlock(input, 0, input.Length)
                    Return decrypted
                Catch ex As Exception
                    Return Nothing
                End Try
            End Function


        End Module


    End Namespace

    Public Module IpAddressTools

        Public Function GetLocalIpAddress() As Net.IPAddress
            Dim addresses() As System.Net.IPAddress
            Dim strHostName As String = System.Net.Dns.GetHostName()
            addresses = System.Net.Dns.GetHostAddresses(strHostName)
            ' Find an IpV4 address
            For Each address As System.Net.IPAddress In addresses
                ' Return the first IpV4 IP Address we find in the list.
                If address.AddressFamily = Net.Sockets.AddressFamily.InterNetwork Then
                    Return address
                End If
            Next
            ' No IpV4 address? Return the loopback address.
            Return System.Net.IPAddress.Loopback
        End Function

        Public Function GetExternalIpAddress() As Net.IPAddress
            Try
                Using wc As New Net.WebClient
                    Dim b() As Byte = wc.DownloadData("http://tools.feron.it/php/ip.php")
                    Dim s As String = Utilities.Bytes.ToChrString_OnlyAscii(b)
                    Return Net.IPAddress.Parse(s)
                End Using
            Catch ex As Exception
                Return Nothing
            End Try
        End Function

        Public Function GetTimeBytes(Seconds As UInt32) As Byte()
            Dim b(3) As Byte
            b(0) = CByte((Seconds And &HFF000000) >> 24)
            b(1) = CByte((Seconds And &HFF0000) >> 16)
            b(2) = CByte((Seconds And &HFF00) >> 8)
            b(3) = CByte(Seconds And &HFF)
            Return b
        End Function

        Public Function IsIP(Address As String) As Boolean
            Dim ipv4 As Net.IPAddress = Nothing
            If Net.IPAddress.TryParse(Address, ipv4) Then
                If ipv4.AddressFamily = Net.Sockets.AddressFamily.InterNetwork Then Return True
            End If
            Return False
        End Function

        Public Function IsIpV6(v6Ip As String) As Boolean
            Dim ipv6 As Net.IPAddress = Net.IPAddress.Parse(v6Ip)
            If ipv6.AddressFamily = Net.Sockets.AddressFamily.InterNetworkV6 Then Return True
            Return False
        End Function
        'Returns true if address provided is a valid subnet
        Public Function IsSubnet(Address As String) As Boolean
            If Not IsIP(Address) Then Return False
            If GetHostsForSubnet(Net.IPAddress.Parse(Address)) = -1 Then Return False
            Return True
        End Function
        'Returns the number of hosts 
        Public Function GetHostsForSubnet(subnet As Net.IPAddress) As Integer
            Dim ipstr As String = subnet.ToString
            Select Case ipstr
                Case "255.0.0.0"
                    Return 16777216
                Case "255.128.0.0"
                    Return 8388608
                Case "255.192.0.0"
                    Return 4194304
                Case "255.224.0.0"
                    Return 2097152
                Case "255.240.0.0"
                    Return 1048576
                Case "255.248.0.0"
                    Return 524288
                Case "255.252.0.0"
                    Return 262144
                Case "255.254.0.0"
                    Return 131072
                Case "255.255.0.0"
                    Return 65536
                Case "255.255.128.0"
                    Return 32768
                Case "255.255.192.0"
                    Return 16384
                Case "255.255.224.0"
                    Return 8192
                Case "255.255.240.0"
                    Return 4096
                Case "255.255.248.0"
                    Return 2048
                Case "255.255.252.0"
                    Return 1024
                Case "255.255.254.0"
                    Return 512
                Case "255.255.255.0"
                    Return 256
                Case "255.255.255.128"
                    Return 128
                Case "255.255.255.192"
                    Return 64
                Case "255.255.255.224"
                    Return 32
                Case "255.255.255.240"
                    Return 16
                Case "255.255.255.248"
                    Return 8
                Case "255.255.255.252"
                    Return 4
                Case "255.255.255.254"
                    Return 2
                Case "255.255.255.255"
                    Return 1
                Case Else
                    Return -1 'Not valid subnet
            End Select
            Return 0
        End Function
        'Gets number of bits usable for the network prefix
        Public Function GetNetworkPrefix(Subnet As Net.IPAddress) As Integer
            Dim b() As Boolean = ByteToBooleanArray(Subnet.GetAddressBytes)
            Dim i As Integer
            Dim c As Integer = 0
            For i = 0 To b.Length - 1
                If b(i) Then c = c + 1
            Next
            Return c
        End Function
        'Add 1 for usable start address
        Public Function GetStartingAddress(IP As Net.IPAddress, Subnet As Net.IPAddress) As Net.IPAddress
            Try
                Dim PrefixNet As Integer = GetNetworkPrefix(Subnet)
                Dim IpS As UInt32 = IpToUint32(IP)
                Dim BaseAdr As UInt32 = CUInt(IpS And IpToUint32(Subnet))
                Return Uint32ToIP(BaseAdr + 1UI)
            Catch ex As Exception
                Return Nothing
            End Try
        End Function
        'Remove 1 for usuable end address
        Public Function GetEndingAddress(IP As Net.IPAddress, Subnet As Net.IPAddress) As Net.IPAddress
            Try
                Dim StartIP As Net.IPAddress = GetStartingAddress(IP, Subnet)
                If StartIP Is Nothing Then Return Nothing
                Dim Hosts As Integer = GetHostsForSubnet(Subnet)
                Dim IpInt As UInt32 = IpToUint32(StartIP) - 1UI
                Dim EndIp As UInt32 = CUInt(IpInt + Hosts) - 2UI
                Return Uint32ToIP(EndIp)
            Catch ex As Exception
                Return Nothing
            End Try
        End Function

        Public Function IpToUint32(ip As Net.IPAddress) As UInt32
            Dim AddressByte() As Byte = ip.GetAddressBytes
            Array.Reverse(AddressByte)
            Dim u As UInt32 = BitConverter.ToUInt32(AddressByte, 0)
            Return u
        End Function

        Public Function Uint32ToIP(IpInt As UInt32) As Net.IPAddress
            Dim IpBytesOut() As Byte = BitConverter.GetBytes(IpInt)
            Array.Reverse(IpBytesOut)
            Return New Net.IPAddress(IpBytesOut)
        End Function

        Public Function GetDnsIpBytes(DNS1 As Net.IPAddress, DNS2 As Net.IPAddress) As Byte()
            If DNS1 Is Nothing And DNS2 Is Nothing Then
                Return New Byte(3) {0, 0, 0, 0}
            End If
            If Not DNS1 Is Nothing And DNS2 Is Nothing Then
                Return DNS1.GetAddressBytes
            End If
            If Not DNS1 Is Nothing And Not DNS2 Is Nothing Then
                Dim ip1() As Byte = DNS1.GetAddressBytes
                Dim ip2() As Byte = DNS2.GetAddressBytes
                Return New Byte(7) {ip1(0), ip1(1), ip1(2), ip1(3), ip2(0), ip2(1), ip2(2), ip2(3)}
            End If
            Return Nothing
        End Function


    End Module

    Public Class TicketQueue
        Private TicketLock As Object = New Object
        Private Tickets As New List(Of TicketClass)

        Public Function AddResponseTicket(TicketID As UInt32) As Boolean
            Debug.WriteLine("Adding ticket for " & TicketID.ToString)
            Try : Threading.Monitor.Enter(TicketLock)
                Tickets.Add(New TicketClass(TicketID))
            Finally
                Threading.Monitor.Exit(TicketLock)
            End Try
            Return True 'Ticket added
        End Function

        Public Function HasResponseTicket(TicketID As UInt32, Ticket_Data() As Byte) As Boolean
            Try : Threading.Monitor.Enter(TicketLock)
                For i = 0 To Tickets.Count - 1
                    If Tickets(i).TicketNumber = TicketID Then
                        Debug.WriteLine("Incoming data for ticket: " & TicketID.ToString)
                        Tickets(i).Resonse = Ticket_Data
                        Tickets(i).ResponseReceived = True
                        Return True
                    End If
                Next
            Finally
                Threading.Monitor.Exit(TicketLock)
            End Try
            Return False 'We are not waiting for this packet
        End Function

        Public Function GetResponseTicket(TicketID As UInt32, ByRef Ticket_Data() As Byte) As Boolean
            Try : Threading.Monitor.Enter(TicketLock)
                For i = 0 To Tickets.Count - 1
                    If (Tickets(i).TicketNumber = TicketID) Then
                        If Tickets(i).ResponseReceived Then
                            Debug.WriteLine("Found ticket for " & TicketID.ToString)
                            Ticket_Data = Tickets(i).Resonse
                            Tickets.RemoveAt(i)
                            Return True
                        Else
                            Return False
                        End If
                    End If
                Next
            Finally
                Threading.Monitor.Exit(TicketLock)
            End Try
            Return False 'No response yet
        End Function

        Public Sub RemoveTicket(TicketID As UInt32)
            Try : Threading.Monitor.Enter(TicketLock)
                For i = 0 To Tickets.Count - 1
                    If (Tickets(i).TicketNumber = TicketID) Then
                        Debug.WriteLine("Removed ticket: " & TicketID.ToString)
                        Tickets.RemoveAt(i)
                        Exit Sub
                    End If
                Next
            Finally
                Threading.Monitor.Exit(TicketLock)
            End Try
            Debug.WriteLine("Unable to find and remove: " & TicketID.ToString)
        End Sub

        Private Class TicketClass
            Public TicketNumber As UInt32
            Public ResponseReceived As Boolean
            Public Resonse() As Byte

            Sub New(ID As UInt32)
                TicketNumber = ID
                ResponseReceived = False
                Resonse = Nothing
            End Sub

        End Class


    End Class

    Public Class BackwardReader
        Implements IDisposable

        Private MyBaseStream As IO.Stream
        Private IsDisposed As Boolean = False

        Public Sub New(path As String)
            Me.MyBaseStream = New IO.FileStream(path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite)
            Me.MyBaseStream.Seek(0, IO.SeekOrigin.[End])
        End Sub

        Public Sub New(file_stream As IO.Stream)
            Me.MyBaseStream = file_stream
            Me.MyBaseStream.Seek(0, IO.SeekOrigin.[End])
        End Sub

        Public Function ReadLine() As String
            Me.MyBaseStream.Seek(0, IO.SeekOrigin.Current)
            Dim count As Integer = 0
            Dim SeekPosition As Integer = CInt(Me.MyBaseStream.Position - Me.MyBaseStream.Length) + 1
            If SeekPosition = 1 Then SeekPosition = -1
            While (Not Math.Abs(SeekPosition) = Me.MyBaseStream.Length + 1)
                Dim sel_bytes(0) As Byte
                Me.MyBaseStream.Seek(SeekPosition, IO.SeekOrigin.End)
                Me.MyBaseStream.Read(sel_bytes, 0, 1)
                SeekPosition -= 1
                If (sel_bytes(0) = 10) Then 'Cr
                    If (Not count = 0) Then Exit While
                ElseIf (sel_bytes(0) = 13) Then 'Lf
                Else
                    count += 1
                End If
            End While
            Dim line(count - 1) As Byte
            If (Math.Abs(SeekPosition) = Me.MyBaseStream.Length + 1) Then
                Me.MyBaseStream.Seek(-1, IO.SeekOrigin.Current)
                Me.MyBaseStream.Read(line, 0, count)
                Me.MyBaseStream.Seek(0, IO.SeekOrigin.Begin) 'We are now at the beginning of the file
            Else
                Me.MyBaseStream.Read(line, 0, count)
                Me.MyBaseStream.Seek(SeekPosition - 1, IO.SeekOrigin.End)
            End If
            Return ASCIIEncoding.ASCII.GetString(line)
        End Function

        Public Sub Close()
            MyBaseStream.Close() : MyBaseStream.Dispose()
        End Sub
        'Indicates if we are at the start of the file
        Public ReadOnly Property SOF() As Boolean
            Get
                Return (MyBaseStream.Position = 0)
            End Get
        End Property

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not Me.IsDisposed Then
                If disposing Then
                    Me.Close()
                End If
            End If
            Me.IsDisposed = True
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

    End Class

End Namespace