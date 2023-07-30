Namespace SREC

    Public Enum FIELD_TYPE As Byte
        Header = 0
        Data16 = 1
        Data24 = 2
        Data32 = 3
        Reserved = 4
        Count16 = 5
        Count24 = 6
        StartAddr32 = 7
        StartAddr24 = 8
        StartAddr16 = 9
    End Enum

    Public Enum RECORD_DATAWIDTH
        [BYTE] = 0
        [WORD] = 1
    End Enum

    Public Class SRECORD
        Public ReadOnly Property IsValid As Boolean = False
        Public ReadOnly Property RecordField As FIELD_TYPE
        Public ReadOnly Property Address As UInt32 'Line address, Count or Start Address

        Public Data() As Byte = Nothing

        Sub New(line As String)
            line = line.ToUpper
            If Not line.Substring(0, 1).StartsWith("S") Then Exit Sub
            If Not IsNumeric(line.Substring(1, 1)) Then Exit Sub
            Me.RecordField = CInt(line.Substring(1, 1))
            Dim crc As Byte = Convert.ToByte(line.Substring(line.Length - 2), 16)
            Dim line_length As Byte = Convert.ToByte(line.Substring(2, 2), 16)
            If Not ((line.Length / 2) - 2) = line_length Then Exit Sub
            Select Case Me.RecordField
                Case FIELD_TYPE.Header
                    Dim header_line_body As String = line.Substring(8, line.Length - 10)
                    While header_line_body.StartsWith("00")
                        header_line_body = header_line_body.Substring(2)
                    End While
                    Me.Data = Utilities.Bytes.FromHexString(header_line_body)
                Case FIELD_TYPE.Data16
                    Dim addr() As Byte = Utilities.Bytes.FromHexString(line.Substring(4, 4))
                    Me.Address = Utilities.Bytes.ToUInt32(addr)
                    Me.Data = Utilities.Bytes.FromHexString(line.Substring(8, line.Length - 10))
                Case FIELD_TYPE.Data24
                    Dim addr() As Byte = Utilities.Bytes.FromHexString(line.Substring(4, 6))
                    Me.Address = Utilities.Bytes.ToUInt32(addr)
                    Me.Data = Utilities.Bytes.FromHexString(line.Substring(10, line.Length - 12))
                Case FIELD_TYPE.Data32
                    Dim addr() As Byte = Utilities.Bytes.FromHexString(line.Substring(4, 8))
                    Me.Address = Utilities.Bytes.ToUInt32(addr)
                    Me.Data = Utilities.Bytes.FromHexString(line.Substring(12, line.Length - 14))
                Case FIELD_TYPE.Count16
                    Dim addr() As Byte = Utilities.Bytes.FromHexString(line.Substring(4, 4))
                    Me.Address = Utilities.Bytes.ToUInt32(addr)
                Case FIELD_TYPE.Count24
                    Dim addr() As Byte = Utilities.Bytes.FromHexString(line.Substring(4, 6))
                    Me.Address = Utilities.Bytes.ToUInt32(addr)
                Case FIELD_TYPE.StartAddr32
                    Dim addr() As Byte = Utilities.Bytes.FromHexString(line.Substring(4, 8))
                    Me.Address = Utilities.Bytes.ToUInt32(addr)
                Case FIELD_TYPE.StartAddr24
                    Dim addr() As Byte = Utilities.Bytes.FromHexString(line.Substring(4, 6))
                    Me.Address = Utilities.Bytes.ToUInt32(addr)
                Case FIELD_TYPE.StartAddr16
                    Dim addr() As Byte = Utilities.Bytes.FromHexString(line.Substring(4, 4))
                    Me.Address = Utilities.Bytes.ToUInt32(addr)
                Case Else
                    Exit Sub
            End Select
            Dim my_crc_calc As UInt32 = line_length
            my_crc_calc += CByte((Me.Address >> 24) And 255)
            my_crc_calc += CByte((Me.Address >> 16) And 255)
            my_crc_calc += CByte((Me.Address >> 8) And 255)
            my_crc_calc += CByte(Me.Address And 255)
            If Me.Data IsNot Nothing Then
                For i = 0 To Me.Data.Length - 1
                    my_crc_calc += Me.Data(i)
                Next
            End If
            Dim my_crc As Byte = ((my_crc_calc And 255) Xor 255)
            If Not my_crc = crc Then Exit Sub 'CRC FAIL!
            Me.IsValid = True
        End Sub

        Sub New(record As FIELD_TYPE, addr As UInt32, data() As Byte)
            Me.RecordField = record
            Me.Address = addr
            Me.Data = data
            Me.IsValid = True
        End Sub

        Public Overrides Function ToString() As String
            Dim AddrSize As Byte = 0 'Number of bytes for bytes field 
            Dim line_out As New Text.StringBuilder(80)
            If Not Me.IsValid Then Return ""
            Dim line_length As Byte = 0
            Dim DataFieldSize As Byte = 0 'Number of bytes for data field
            If Data IsNot Nothing Then DataFieldSize = Data.Length
            Select Case Me.RecordField
                Case FIELD_TYPE.Header
                    AddrSize = 2
                    line_length = AddrSize + DataFieldSize + 1
                    line_out.Insert(0, "S0")
                    line_out.Insert(2, Hex(line_length).PadLeft(2, "0"))
                    line_out.Insert(4, "0000")
                Case FIELD_TYPE.Data16
                    AddrSize = 2
                    line_length = AddrSize + DataFieldSize + 1
                    line_out.Insert(0, "S1")
                    line_out.Insert(2, Hex(line_length).PadLeft(2, "0"))
                    line_out.Insert(4, Hex(Address).PadLeft(4, "0"))
                Case FIELD_TYPE.Data24
                    AddrSize = 3
                    line_length = AddrSize + DataFieldSize + 1
                    line_out.Insert(0, "S2")
                    line_out.Insert(2, Hex(line_length).PadLeft(2, "0"))
                    line_out.Insert(4, Hex(Address).PadLeft(6, "0"))
                Case FIELD_TYPE.Data32
                    AddrSize = 4
                    line_length = AddrSize + DataFieldSize + 1
                    line_out.Insert(0, "S3")
                    line_out.Insert(2, Hex(line_length).PadLeft(2, "0"))
                    line_out.Insert(4, Hex(Address).PadLeft(8, "0"))
                Case FIELD_TYPE.Count16
                    AddrSize = 2
                    line_length = AddrSize + DataFieldSize + 1
                    line_out.Insert(0, "S5")
                    line_out.Insert(2, Hex(line_length).PadLeft(2, "0"))
                    line_out.Insert(4, Hex(Address).PadLeft(4, "0"))
                Case FIELD_TYPE.Count24
                    AddrSize = 3
                    line_length = AddrSize + DataFieldSize + 1
                    line_out.Insert(0, "S6")
                    line_out.Insert(2, Hex(line_length).PadLeft(2, "0"))
                    line_out.Insert(4, Hex(Address).PadLeft(6, "0"))
                Case FIELD_TYPE.StartAddr32
                    AddrSize = 4
                    line_length = AddrSize + DataFieldSize + 1
                    line_out.Insert(0, "S7")
                    line_out.Insert(2, Hex(line_length).PadLeft(2, "0"))
                    line_out.Insert(4, Hex(Address).PadLeft(8, "0"))
                Case FIELD_TYPE.StartAddr24
                    AddrSize = 3
                    line_length = AddrSize + DataFieldSize + 1
                    line_out.Insert(0, "S8")
                    line_out.Insert(2, Hex(line_length).PadLeft(2, "0"))
                    line_out.Insert(4, Hex(Address).PadLeft(6, "0"))
                Case FIELD_TYPE.StartAddr16
                    AddrSize = 2
                    line_length = AddrSize + DataFieldSize + 1
                    line_out.Insert(0, "S9")
                    line_out.Insert(2, Hex(line_length).PadLeft(2, "0"))
                    line_out.Insert(4, Hex(Address).PadLeft(4, "0"))
                Case Else
                    Return ""
            End Select
            Dim my_crc_calc As UInt32 = line_length + ((Me.Address >> 24) And 255) + ((Me.Address >> 16) And 255) + ((Me.Address >> 8) And 255) + (Me.Address And 255)
            If Me.Data IsNot Nothing Then
                For i = 0 To Me.Data.Length - 1
                    my_crc_calc += Me.Data(i)
                Next
            End If
            Dim my_crc As Byte = ((my_crc_calc And 255) Xor 255)
            If DataFieldSize > 0 Then
                line_out.Insert((AddrSize * 2) + 4, Utilities.Bytes.ToHexString(Data))
            End If
            Dim x As Byte = (AddrSize * 2) + (DataFieldSize * 2)
            line_out.Insert(x + 4, Hex(my_crc).PadLeft(2, "0"))
            Return line_out.ToString()
        End Function

    End Class
    'Reads a file from stream and converts it to binary output stream
    Public Class StreamReader
        Inherits IO.Stream
        Public ReadOnly Property IsValid As Boolean
        Public ReadOnly Property Header As String

        Private m_local_steam As IO.StreamReader
        Private m_data_size As Integer

        Private BUFFER() As Byte
        Private BUFFER_PTR As Integer = 0
        Private STREAM_ADDR As UInt32 = 0 'The last address of data from the stream
        Private STREAM_POS As Integer = 0 'Number of bytes read from this stream
        Private MyDataWidth As RECORD_DATAWIDTH

        Private Const BUFF_MIN As Integer = 65536

        Sub New(file_stream As IO.StreamReader, Optional data_width As RECORD_DATAWIDTH = RECORD_DATAWIDTH.BYTE)
            Me.m_local_steam = file_stream
            Me.MyDataWidth = data_width
            Dim HeaderRecord As New SRECORD(Me.m_local_steam.ReadLine())
            If Not HeaderRecord.IsValid Then Me.IsValid = False : Exit Sub
            If Not HeaderRecord.RecordField = FIELD_TYPE.Header Then Me.IsValid = False : Exit Sub
            Dim LastRecord As SRECORD = GetLastDataRecord()
            Me.m_local_steam.BaseStream.Seek(0, IO.SeekOrigin.Begin)
            Me.m_local_steam.ReadLine()
            If LastRecord Is Nothing Then Exit Sub
            Dim LastAddr As UInt32 = LastRecord.Address
            If MyDataWidth = RECORD_DATAWIDTH.WORD Then LastAddr = (LastAddr * 2)
            Me.m_data_size = LastAddr + LastRecord.Data.Length
            ProcessBuffer()
            Me.IsValid = True
        End Sub

        Private Sub ProcessBuffer()
            Static Dim counter As Long = 0
            counter += 1
            BUFFER = Nothing
            Dim DataLine As SRECORD
            Dim io_buffer As New IO.MemoryStream
            Dim BytesProcessed As Integer = 0
            Do While (BytesProcessed < BUFF_MIN)
                If Me.m_local_steam.Peek = -1 Then Exit Do
                DataLine = New SRECORD(Me.m_local_steam.ReadLine())
                If IsDataField(DataLine.RecordField) Then
                    Dim CurrentAddr As UInt32 = DataLine.Address
                    If MyDataWidth = RECORD_DATAWIDTH.WORD Then CurrentAddr = (CurrentAddr * 2)
                    Dim offset As Integer = (CurrentAddr - Me.STREAM_ADDR)
                    If (offset > 0) Then
                        Dim blank_data() As Byte = Enumerable.Repeat(CByte(255), offset).ToArray
                        io_buffer.Write(blank_data, 0, blank_data.Length)
                        BytesProcessed += offset
                        Me.STREAM_ADDR += offset
                    End If
                    io_buffer.Write(DataLine.Data, 0, DataLine.Data.Length)
                    BytesProcessed += DataLine.Data.Length
                    Me.STREAM_ADDR += DataLine.Data.Length
                End If
            Loop
            io_buffer.Flush()
            BUFFER = io_buffer.GetBuffer()
            ReDim Preserve BUFFER(BytesProcessed - 1)
            Me.BUFFER_PTR = 0
        End Sub

        Private Function GetLastDataRecord() As SRECORD
            Me.m_local_steam.BaseStream.Seek(-100, IO.SeekOrigin.End)
            Dim records As New List(Of SRECORD)
            Do Until Me.m_local_steam.Peek = -1
                Dim line As String = Me.m_local_steam.ReadLine
                Dim s As New SRECORD(line)
                If s.IsValid Then
                    If s.RecordField = FIELD_TYPE.Data16 Or s.RecordField = FIELD_TYPE.Data24 Or s.RecordField = FIELD_TYPE.Data32 Then
                        records.Add(s)
                    End If
                End If
            Loop
            If records.Count > 0 Then Return records(records.Count - 1)
            Return Nothing
        End Function

        Private Function IsDataField(field As FIELD_TYPE) As Boolean
            If field = FIELD_TYPE.Data16 Then
                Return True
            ElseIf field = FIELD_TYPE.Data24 Then
                Return True
            ElseIf field = FIELD_TYPE.Data32 Then
                Return True
            Else
                Return False
            End If
        End Function

        Public Overrides ReadOnly Property CanRead As Boolean
            Get
                Return Me.IsValid
            End Get
        End Property

        Public Overrides ReadOnly Property CanSeek As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property CanWrite As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property Length As Long
            Get
                Return Me.m_data_size
            End Get
        End Property

        Public Overrides Property Position As Long
            Get
                Return Me.STREAM_POS
            End Get
            Set(value As Long)

            End Set
        End Property

        Public Overrides Sub Flush()

        End Sub

        Public Overrides Function Seek(offset As Long, origin As IO.SeekOrigin) As Long
            Throw New NotImplementedException()
        End Function

        Public Overrides Sub SetLength(value As Long)
            Throw New NotImplementedException()
        End Sub

        Public Overrides Function Read(buffer_out() As Byte, offset As Integer, count As Integer) As Integer
            Dim BytesLeft As Integer = count
            Dim ptr As Integer = 0
            While (BytesLeft > 0)
                Dim buffer_size As Integer = 0
                Dim BytesInBuffer As Integer = (BUFFER.Length - Me.BUFFER_PTR)
                If (BytesInBuffer = 0) Then
                    ProcessBuffer()
                    If BUFFER Is Nothing OrElse BUFFER.Length = 0 Then Throw New Exception("Stream has reached the end")
                    BytesInBuffer = BUFFER.Length
                End If
                If BytesLeft < BytesInBuffer Then BytesInBuffer = BytesLeft
                Array.Copy(Me.BUFFER, Me.BUFFER_PTR, buffer_out, ptr + offset, BytesInBuffer)
                ptr += BytesInBuffer
                Me.BUFFER_PTR += BytesInBuffer
                BytesLeft -= BytesInBuffer
            End While
            Me.STREAM_POS += count
            Return count
        End Function

        Public Overrides Sub Write(buffer() As Byte, offset As Integer, count As Integer)
            Throw New NotImplementedException()
        End Sub

        Public Overrides Sub Close()
            m_local_steam.Close()
        End Sub

    End Class

    Public Class StreamWriter
        Inherits IO.Stream

        Public Property BytesPerLine As Integer = 32

        Private output_stream As IO.StreamWriter
        Private DataAddress As UInt32 = 0
        Private DataRecordSize As FIELD_TYPE
        Private StartAddrSize As FIELD_TYPE
        Private RecordCount As Integer = 0
        Private MyDataWidth As RECORD_DATAWIDTH

        Sub New(file_stream As IO.StreamWriter, addr_size As Integer, Optional data_width As RECORD_DATAWIDTH = RECORD_DATAWIDTH.BYTE)
            Me.output_stream = file_stream
            Me.MyDataWidth = data_width
            Dim header_data() As Byte = Utilities.Bytes.FromChrString("EC_SREC_V1.0")
            Me.output_stream.WriteLine((New SRECORD(FIELD_TYPE.Header, 0, header_data)).ToString())
            Select Case addr_size
                Case 2
                    DataRecordSize = FIELD_TYPE.Data16
                    StartAddrSize = FIELD_TYPE.StartAddr16
                Case 3
                    DataRecordSize = FIELD_TYPE.Data24
                    StartAddrSize = FIELD_TYPE.StartAddr24
                Case 4
                    DataRecordSize = FIELD_TYPE.Data32
                    StartAddrSize = FIELD_TYPE.StartAddr32
            End Select
        End Sub

        Public Overrides ReadOnly Property CanRead As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property CanSeek As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property CanWrite As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property Length As Long
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Overrides Property Position As Long
            Get
                Throw New NotImplementedException()
            End Get
            Set(value As Long)
                Throw New NotImplementedException()
            End Set
        End Property

        Public Overrides Sub Flush()
            output_stream.Flush()
        End Sub

        Public Overrides Sub SetLength(value As Long)
            Throw New NotImplementedException()
        End Sub

        Public Overrides Sub Write(buffer() As Byte, offset As Integer, count As Integer)
            Dim buffer_ptr As Integer = 0
            Dim bytes_left As Integer = count
            Do While (bytes_left > 0)
                Dim packet_size As Integer = IIf(bytes_left > BytesPerLine, BytesPerLine, bytes_left)
                Dim data(packet_size - 1) As Byte
                Array.Copy(buffer, buffer_ptr, data, 0, packet_size)
                Dim DataAddr32 As UInt32 = Me.DataAddress
                If Me.MyDataWidth = RECORD_DATAWIDTH.WORD Then MyDataWidth = (MyDataWidth * 2)
                Me.output_stream.WriteLine((New SRECORD(Me.DataRecordSize, DataAddr32, data)).ToString())
                Me.RecordCount += 1
                buffer_ptr += packet_size
                If Me.MyDataWidth = RECORD_DATAWIDTH.WORD Then
                    Me.DataAddress += (packet_size / 2)
                Else
                    Me.DataAddress += packet_size
                End If
                bytes_left -= packet_size
            Loop
        End Sub

        Public Overrides Function Seek(offset As Long, origin As IO.SeekOrigin) As Long
            Throw New NotImplementedException()
        End Function

        Public Overrides Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer
            Throw New NotImplementedException()
        End Function

        Public Overrides Sub Close()
            Dim RecordCountType As FIELD_TYPE
            If Me.RecordCount > &HFFFF Then
                RecordCountType = FIELD_TYPE.Count24
            Else
                RecordCountType = FIELD_TYPE.Count16
            End If
            Me.output_stream.WriteLine((New SRECORD(RecordCountType, Me.RecordCount, Nothing)).ToString())
            Me.output_stream.WriteLine((New SRECORD(Me.StartAddrSize, 0, Nothing)).ToString())
            Me.Flush()
            output_stream.Close()
        End Sub

    End Class


End Namespace