Namespace IHEX

    Public Enum FIELD_TYPE As Byte
        Data = 0
        EndOfFile = 1
        ExtSegmentAddr = 2
        StartSegmentAddr = 3
        ExtLinearAddr = 4
        StartLinearAddr = 5
    End Enum

    Public Class IRECORD
        Public ReadOnly Property IsValid As Boolean
        Public ReadOnly Property RecordField As FIELD_TYPE
        Public ReadOnly Property Address As UInt16

        Public Data() As Byte = Nothing

        Sub New(line As String)
            If Not line.StartsWith(":") Then Exit Sub
            Dim line_data() As Byte = Utilities.Bytes.FromHexString(line.Substring(1))
            Dim byte_count As Integer = line_data(0)
            Me.Address = (CUShort(line_data(1)) << 8) Or CUShort(line_data(2))
            Me.RecordField = CType(line_data(3), FIELD_TYPE)
            If (byte_count > 0) Then
                ReDim Data(byte_count - 1)
                Array.Copy(line_data, 4, Me.Data, 0, byte_count)
            End If
            Dim check_sum As Byte = line_data(4 + byte_count)
            Dim new_crc As UInt32
            For i = 0 To line_data.Length - 2
                new_crc += line_data(i)
            Next
            If check_sum = CByte(((new_crc Xor 255) + 1) And 255) Then
                Me.IsValid = True
            End If
        End Sub

        Sub New(record As FIELD_TYPE, addr As UInt16, data() As Byte)
            Me.RecordField = record
            Me.Address = addr
            Me.Data = data
            Me.IsValid = True
        End Sub

        Public Overrides Function ToString() As String
            Dim line_out As New Text.StringBuilder(":", 80)
            Dim data_len As Integer = 0
            If Data IsNot Nothing Then data_len = Data.Length
            Dim crc As UInt32 = CUInt(Me.RecordField) + CUInt(data_len) + CUInt(Address >> 8) + CUInt(Address And 255)
            If Data IsNot Nothing Then
                For i = 0 To Data.Length - 1
                    crc += Data(i)
                Next
            End If
            crc = ((crc Xor 255UI) + 1UI)
            line_out.Append(Hex(data_len And 255).PadLeft(2, "0"c))
            line_out.Append(Hex(Address).PadLeft(4, "0"c))
            line_out.Append(Hex(Me.RecordField).PadLeft(2, "0"c))
            If Data IsNot Nothing Then
                Dim s As String = Utilities.Bytes.ToHexString(Data)
                line_out.Append(s)
            End If
            line_out.Append(Hex(crc And 255).PadLeft(2, "0"c))
            Return line_out.ToString()
        End Function

    End Class

    Public Class StreamReader
        Inherits IO.Stream
        Public ReadOnly Property IsValid As Boolean

        Private m_local_steam As IO.StreamReader
        Private m_data_size As Integer

        Private BUFFER() As Byte
        Private BUFFER_PTR As Integer = 0
        Private STREAM_ADDR As Integer = 0 'The last address of data from the stream
        Private STREAM_POS As Integer = 0 'Number of bytes read from this stream

        Private Const BUFF_MIN As Integer = 65536

        Private EXT_ADDR32 As UInt32 'The extended address that ExtLinearAddr updates

        Sub New(file_stream As IO.StreamReader)
            Me.m_local_steam = file_stream
            If Not LoadFileSize() Then Exit Sub
            Me.m_local_steam.BaseStream.Seek(0, IO.SeekOrigin.Begin)
            ProcessBuffer()
            Me.IsValid = True
        End Sub

        Private Function LoadFileSize() As Boolean
            Dim RecordAddr As Integer = -1
            Dim bfr As New Utilities.BackwardReader(Me.m_local_steam.BaseStream)
            Do Until bfr.SOF
                Dim n As New IRECORD(bfr.ReadLine())
                If Not n.IsValid Then Return False
                If n.RecordField = FIELD_TYPE.Data AndAlso RecordAddr = -1 Then
                    If n.Data Is Nothing Then Return False
                    RecordAddr = n.Address + n.Data.Length
                ElseIf n.RecordField = FIELD_TYPE.ExtLinearAddr Then
                    Me.m_data_size = (CInt(n.Data(0)) << 24) Or (CInt(n.Data(1)) << 16) Or RecordAddr
                    Return True
                End If
            Loop
            If RecordAddr = -1 Then Return False
            Me.m_data_size = RecordAddr
            Return True
        End Function

        Private Sub ProcessBuffer()
            Static Dim counter As Long = 0
            counter += 1
            BUFFER = Nothing
            Dim DataLine As IRECORD
            Dim io_buffer As New IO.MemoryStream
            Dim BytesProcessed As Integer = 0
            Do While (BytesProcessed < BUFF_MIN)
                If Me.m_local_steam.Peek = -1 Then Exit Do
                DataLine = New IRECORD(Me.m_local_steam.ReadLine())
                If DataLine.RecordField = FIELD_TYPE.Data Then
                    Dim offset As Integer = (CInt(DataLine.Address + EXT_ADDR32) - Me.STREAM_ADDR)
                    If (offset > 0) Then
                        Dim blank_data() As Byte = Enumerable.Repeat(CByte(255), offset).ToArray
                        io_buffer.Write(blank_data, 0, blank_data.Length)
                        BytesProcessed += offset
                        Me.STREAM_ADDR += offset
                    End If
                    io_buffer.Write(DataLine.Data, 0, DataLine.Data.Length)
                    BytesProcessed += DataLine.Data.Length
                    Me.STREAM_ADDR += DataLine.Data.Length
                ElseIf DataLine.RecordField = FIELD_TYPE.ExtLinearAddr Then
                    Me.EXT_ADDR32 = (CUInt(DataLine.Data(0)) << 24) Or (CUInt(DataLine.Data(1)) << 16)
                End If
            Loop
            io_buffer.Flush()
            BUFFER = io_buffer.GetBuffer()
            ReDim Preserve BUFFER(BytesProcessed - 1)
            Me.BUFFER_PTR = 0
        End Sub

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

        Sub New(file_stream As IO.StreamWriter)
            Me.output_stream = file_stream
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
                Dim packet_size As Integer = CInt(IIf(bytes_left > BytesPerLine, BytesPerLine, bytes_left))
                Dim data(packet_size - 1) As Byte
                Array.Copy(buffer, buffer_ptr, data, 0, packet_size)
                Me.output_stream.WriteLine((New IRECORD(FIELD_TYPE.Data, CUShort(Me.DataAddress And &HFFFF), data)).ToString())
                buffer_ptr += packet_size
                Dim upper16 As UInt16 = CUShort(Me.DataAddress >> 16)
                Me.DataAddress += CUInt(packet_size)
                Dim current16 As UInt16 = CUShort(Me.DataAddress >> 16)
                bytes_left -= packet_size
                If (Not current16 = upper16) Then
                    Me.output_stream.WriteLine((New IRECORD(FIELD_TYPE.ExtLinearAddr, current16, Nothing)).ToString())
                End If
            Loop
        End Sub

        Public Overrides Function Seek(offset As Long, origin As IO.SeekOrigin) As Long
            Throw New NotImplementedException()
        End Function

        Public Overrides Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer
            Throw New NotImplementedException()
        End Function

        Public Overrides Sub Close()
            Me.output_stream.WriteLine((New IRECORD(FIELD_TYPE.EndOfFile, 0, Nothing)).ToString())
            Me.Flush()
            output_stream.Close()
        End Sub

    End Class

End Namespace
