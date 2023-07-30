'This library includes the common error-correction used in NAND memory.

Namespace ECC_LIB

    '512 byte processing (24-bit ECC) 1-bit correctable/2-bit detection
    Public Class Hamming

        Sub New()

        End Sub


#Region "Parity Table"
        Private byte_parity_table() As Byte = {&HFF, &HD4, &HD2, &HF9, &HCC, &HE7, &HE1, &HCA,
                &HCA, &HE1, &HE7, &HCC, &HF9, &HD2, &HD4, &HFF,
                &HB4, &H9F, &H99, &HB2, &H87, &HAC, &HAA, &H81,
                &H81, &HAA, &HAC, &H87, &HB2, &H99, &H9F, &HB4,
                &HB2, &H99, &H9F, &HB4, &H81, &HAA, &HAC, &H87,
                &H87, &HAC, &HAA, &H81, &HB4, &H9F, &H99, &HB2,
                &HF9, &HD2, &HD4, &HFF, &HCA, &HE1, &HE7, &HCC,
                &HCC, &HE7, &HE1, &HCA, &HFF, &HD4, &HD2, &HF9,
                &HAC, &H87, &H81, &HAA, &H9F, &HB4, &HB2, &H99,
                &H99, &HB2, &HB4, &H9F, &HAA, &H81, &H87, &HAC,
                &HE7, &HCC, &HCA, &HE1, &HD4, &HFF, &HF9, &HD2,
                &HD2, &HF9, &HFF, &HD4, &HE1, &HCA, &HCC, &HE7,
                &HE1, &HCA, &HCC, &HE7, &HD2, &HF9, &HFF, &HD4,
                &HD4, &HFF, &HF9, &HD2, &HE7, &HCC, &HCA, &HE1,
                &HAA, &H81, &H87, &HAC, &H99, &HB2, &HB4, &H9F,
                &H9F, &HB4, &HB2, &H99, &HAC, &H87, &H81, &HAA,
                &HAA, &H81, &H87, &HAC, &H99, &HB2, &HB4, &H9F,
                &H9F, &HB4, &HB2, &H99, &HAC, &H87, &H81, &HAA,
                &HE1, &HCA, &HCC, &HE7, &HD2, &HF9, &HFF, &HD4,
                &HD4, &HFF, &HF9, &HD2, &HE7, &HCC, &HCA, &HE1,
                &HE7, &HCC, &HCA, &HE1, &HD4, &HFF, &HF9, &HD2,
                &HD2, &HF9, &HFF, &HD4, &HE1, &HCA, &HCC, &HE7,
                &HAC, &H87, &H81, &HAA, &H9F, &HB4, &HB2, &H99,
                &H99, &HB2, &HB4, &H9F, &HAA, &H81, &H87, &HAC,
                &HF9, &HD2, &HD4, &HFF, &HCA, &HE1, &HE7, &HCC,
                &HCC, &HE7, &HE1, &HCA, &HFF, &HD4, &HD2, &HF9,
                &HB2, &H99, &H9F, &HB4, &H81, &HAA, &HAC, &H87,
                &H87, &HAC, &HAA, &H81, &HB4, &H9F, &H99, &HB2,
                &HB4, &H9F, &H99, &HB2, &H87, &HAC, &HAA, &H81,
                &H81, &HAA, &HAC, &H87, &HB2, &H99, &H9F, &HB4,
                &HFF, &HD4, &HD2, &HF9, &HCC, &HE7, &HE1, &HCA,
                &HCA, &HE1, &HE7, &HCC, &HF9, &HD2, &HD4, &HFF}
#End Region

        'Creates ECC (24-bit) for 512 bytes - 1 bit correctable, 2 bit detectable
        Public Function GenerateECC(block() As Byte) As Byte()
            Dim ecc_code(2) As Byte
            Dim word_reg As UInt16
            Dim LP0, LP1, LP2, LP3, LP4, LP5, LP6, LP7, LP8, LP9, LP10, LP11, LP12, LP13, LP14, LP15, LP16, LP17 As UInt32
            Dim uddata() As UInt32 = Utilities.Bytes.ToUintArray(block)
            For j = 0 To 127
                Dim temp As UInt32 = uddata(j)
                If j And &H1 Then LP5 = LP5 Xor temp Else LP4 = LP4 Xor temp
                If j And &H2 Then LP7 = LP7 Xor temp Else LP6 = LP6 Xor temp
                If j And &H4 Then LP9 = LP9 Xor temp Else LP8 = LP8 Xor temp
                If j And &H8 Then LP11 = LP11 Xor temp Else LP10 = LP10 Xor temp
                If j And &H10 Then LP13 = LP13 Xor temp Else LP12 = LP12 Xor temp
                If j And &H20 Then LP15 = LP15 Xor temp Else LP14 = LP14 Xor temp
                If j And &H40 Then LP17 = LP17 Xor temp Else LP16 = LP16 Xor temp
            Next
            Dim reg32 As UInt32 = (LP15 Xor LP14)
            Dim byte_reg As Byte = byte_reg Xor CByte((reg32) And &HFF)
            byte_reg = byte_reg Xor CByte((reg32 >> 8) And &HFF)
            byte_reg = byte_reg Xor CByte((reg32 >> 16) And &HFF)
            byte_reg = byte_reg Xor CByte((reg32 >> 24) And &HFF)
            byte_reg = byte_parity_table(byte_reg)
            word_reg = (CUShort(LP16 >> 16)) Xor (CUShort(LP16 And &HFFFF))
            LP16 = CByte(CByte(word_reg And &HFF) Xor CByte(word_reg >> 8))
            word_reg = CUShort(LP17 >> 16) Xor CUShort(LP17 And &HFFFF)
            LP17 = CByte(CByte(word_reg And &HFF) Xor CByte(word_reg >> 8))
            ecc_code(2) = CByte(CByte(byte_reg And &HFE) << 1) Or (byte_parity_table(CByte(LP16 And 255)) And &H1) Or ((byte_parity_table(CByte(LP17 And 255)) And &H1) << 1)
            LP0 = CByte((reg32 Xor (reg32 >> 16)) And 255)
            LP1 = CByte(((reg32 >> 8) Xor (reg32 >> 24)) And 255)
            LP2 = CByte((reg32 Xor (reg32 >> 8)) And 255)
            LP3 = CByte(((reg32 >> 16) Xor (reg32 >> 24)) And 255)
            word_reg = CUShort(LP4 >> 16) Xor CUShort(LP4 And &HFFFF)
            LP4 = CByte(CByte(word_reg And 255) Xor (CByte(word_reg >> 8)))
            word_reg = CUShort(LP5 >> 16) Xor CUShort(LP5 And &HFFFF)
            LP5 = CByte(CByte(word_reg And 255) Xor (CByte(word_reg >> 8)))
            word_reg = CUShort(LP6 >> 16) Xor CUShort(LP6 And &HFFFF)
            LP6 = CByte(CByte(word_reg And 255) Xor (CByte(word_reg >> 8)))
            word_reg = CUShort(LP7 >> 16) Xor CUShort(LP7 And &HFFFF)
            LP7 = CByte(CByte(word_reg And 255) Xor (CByte(word_reg >> 8)))
            word_reg = CUShort(LP8 >> 16) Xor CUShort(LP8 And &HFFFF)
            LP8 = CByte(CByte(word_reg And 255) Xor (CByte(word_reg >> 8)))
            word_reg = CUShort(LP9 >> 16) Xor CUShort(LP9 And &HFFFF)
            LP9 = CByte(CByte(word_reg And 255) Xor (CByte(word_reg >> 8)))
            word_reg = CUShort(LP10 >> 16) Xor CUShort(LP10 And &HFFFF)
            LP10 = CByte(CByte(word_reg And 255) Xor (CByte(word_reg >> 8)))
            word_reg = CUShort(LP11 >> 16) Xor CUShort(LP11 And &HFFFF)
            LP11 = CByte(CByte(word_reg And 255) Xor (CByte(word_reg >> 8)))
            word_reg = CUShort(LP12 >> 16) Xor CUShort(LP12 And &HFFFF)
            LP12 = CByte(CByte(word_reg And 255) Xor (CByte(word_reg >> 8)))
            word_reg = CUShort(LP13 >> 16) Xor CUShort(LP13 And &HFFFF)
            LP13 = CByte(CByte(word_reg And 255) Xor (CByte(word_reg >> 8)))
            word_reg = CUShort(LP14 >> 16) Xor CUShort(LP14 And &HFFFF)
            LP14 = CByte(CByte(word_reg And 255) Xor (CByte(word_reg >> 8)))
            word_reg = CUShort(LP15 >> 16) Xor CUShort(LP15 And &HFFFF)
            LP15 = CByte(CByte(word_reg And 255) Xor (CByte(word_reg >> 8)))

            ecc_code(0) = (byte_parity_table(CByte(LP0 And 255)) And &H1) Or
                ((byte_parity_table(CByte(LP1 And 255)) And &H1) << 1) Or
                ((byte_parity_table(CByte(LP2 And 255)) And &H1) << 2) Or
                ((byte_parity_table(CByte(LP3 And 255)) And &H1) << 3) Or
                ((byte_parity_table(CByte(LP4 And 255)) And &H1) << 4) Or
                ((byte_parity_table(CByte(LP5 And 255)) And &H1) << 5) Or
                ((byte_parity_table(CByte(LP6 And 255)) And &H1) << 6) Or
                ((byte_parity_table(CByte(LP7 And 255)) And &H1) << 7)

            ecc_code(1) = ((byte_parity_table(CByte(LP8 And 255)) And &H1)) Or
                (byte_parity_table(CByte(LP9 And 255)) And &H1) << 1 Or
                (byte_parity_table(CByte(LP10 And 255)) And &H1) << 2 Or
                (byte_parity_table(CByte(LP11 And 255)) And &H1) << 3 Or
                (byte_parity_table(CByte(LP12 And 255)) And &H1) << 4 Or
                (byte_parity_table(CByte(LP13 And 255)) And &H1) << 5 Or
                (byte_parity_table(CByte(LP14 And 255)) And &H1) << 6 Or
                (byte_parity_table(CByte(LP15 And 255)) And &H1) << 7

            Return ecc_code
        End Function
        'Processed a block of 512 bytes with 24-bit ecc code word data, corrects if needed
        Public Function ProcessECC(block() As Byte, stored_ecc() As Byte) As ECC_DECODE_RESULT
            Dim new_ecc() As Byte = GenerateECC(block)
            Dim ecc_xor(2) As Byte
            ecc_xor(0) = new_ecc(0) Xor stored_ecc(0)
            ecc_xor(1) = new_ecc(1) Xor stored_ecc(1)
            ecc_xor(2) = new_ecc(2) Xor stored_ecc(2)
            If ((ecc_xor(0) Or ecc_xor(1) Or ecc_xor(2)) = 0) Then
                Return ECC_DECODE_RESULT.NoErrors
            Else
                Dim bit_count As Integer = BitCount(ecc_xor) 'Counts the bit number
                If (bit_count = 12) Then
                    Dim bit_addr As Byte = (ecc_xor(2) >> 3) And 1 Or (ecc_xor(2) >> 4) And 2 Or (ecc_xor(2) >> 5) And 4
                    Dim byte_addr As UInt32 = (ecc_xor(0) >> 1) And &H1 Or (ecc_xor(0) >> 2) And &H2 Or (ecc_xor(0) >> 3) And &H4 Or
                           (ecc_xor(0) >> 4) And &H8 Or
                           (ecc_xor(1) << 3) And &H10 Or
                           (ecc_xor(1) << 2) And &H20 Or
                           (ecc_xor(1) << 1) And &H40 Or
                           (ecc_xor(1) And &H80) Or ((CUShort(ecc_xor(2)) << 7) And &H100)

                    block(byte_addr - 1) = block(byte_addr - 1) Xor (1 << bit_addr)
                    Return ECC_DECODE_RESULT.Correctable
                ElseIf (bit_count = 1) Then
                    stored_ecc = new_ecc
                    Return ECC_DECODE_RESULT.EccError
                Else
                    Return ECC_DECODE_RESULT.Uncorractable
                End If
            End If
        End Function

        Private Function BitCount(data() As Byte) As Integer
            Dim counter As Integer
            For i = 0 To data.Length - 1
                Dim temp As Byte = data(i)
                Do While (temp > 0)
                    If (temp And 1) = 1 Then counter += 1
                    temp = (temp >> 1)
                Loop
            Next
            Return counter
        End Function

    End Class

    Public Class RS_ECC
        Public Property PARITY_BITS As Integer 'RS_CONST_T (Number of symbols to correct)
        Public Property SYM_WIDTH As Integer 'RS_SYM_W (Number of bits per symbol)

        Sub New()

        End Sub

        Public Function GetEccSize() As Integer
            If Me.SYM_WIDTH = 9 Then
                Select Case PARITY_BITS
                    Case 1
                        Return 3
                    Case 2
                        Return 5
                    Case 4
                        Return 9
                    Case 8
                        Return 18
                    Case 10
                        Return 23
                    Case 14
                        Return 32
                End Select
            ElseIf Me.SYM_WIDTH = 10 Then
                Select Case PARITY_BITS
                    Case 1
                        Return 3
                    Case 2
                        Return 5
                    Case 4
                        Return 10
                    Case 8
                        Return 20
                    Case 10
                        Return 25
                    Case 14
                        Return 35
                End Select
            End If
            Return -1
        End Function

        Public Function GenerateECC(block() As Byte) As Byte()
            Dim sym_data() As Integer = bytes_to_symbols(block, Me.SYM_WIDTH)
            Using RS As New ECC.ReedSolomon(Me.SYM_WIDTH, GetPolynomial(Me.SYM_WIDTH), 0, 2, Me.PARITY_BITS * 2)
                Dim Symbols() As Integer = bytes_to_symbols(block, Me.SYM_WIDTH)
                Dim result() As Integer = RS.Encode(Symbols)
                Dim ecc_data() As Byte = symbols_to_bytes(result, Me.SYM_WIDTH)
                Return ecc_data
            End Using
        End Function

        Public Function ProcessECC(ByRef block() As Byte, ByRef stored_ecc() As Byte) As ECC_DECODE_RESULT
            Dim sym_data() As Integer = bytes_to_symbols(block, Me.SYM_WIDTH)
            Using RS As New ECC.ReedSolomon(Me.SYM_WIDTH, GetPolynomial(Me.SYM_WIDTH), 0, 2, Me.PARITY_BITS * 2)
                Dim Symbols() As Integer = bytes_to_symbols(block, Me.SYM_WIDTH)
                Dim EccData() As Integer = bytes_to_symbols(stored_ecc, Me.SYM_WIDTH)
                Dim cmp_result As ECC.CompareResult = RS.Decode(Symbols, EccData)
                Select Case cmp_result
                    Case ECC.CompareResult.NoError
                        Return ECC_DECODE_RESULT.NoErrors
                    Case ECC.CompareResult.EccError
                        Dim org_size As Integer = stored_ecc.Length
                        stored_ecc = symbols_to_bytes(EccData, Me.SYM_WIDTH)
                        ReDim Preserve stored_ecc(org_size - 1)
                        Return ECC_DECODE_RESULT.EccError
                    Case ECC.CompareResult.Correctable
                        Dim org_size As Integer = block.Length
                        block = symbols_to_bytes(Symbols, Me.SYM_WIDTH)
                        ReDim Preserve block(org_size - 1)
                        Return ECC_DECODE_RESULT.Correctable
                    Case Else
                        Return ECC_DECODE_RESULT.Uncorractable
                End Select
            End Using
        End Function

        Private Function GetPolynomial(ByVal sym_size As Integer) As Integer
            Select Case sym_size
                Case 0 'dont care
                Case 1 'dont care
                Case 2 '2-nd: poly = x^2 + x + 1
                    Return &H7
                Case 3 '3-rd: poly = x^3 + x + 1
                    Return &HB
                Case 4  '4-th: poly = x^4 + x + 1
                    Return &H13
                Case 5 '5-th: poly = x^5 + x^2 + 1
                    Return &H25
                Case 6 '6-th: poly = x^6 + x + 1
                    Return &H43
                Case 7 '7-th: poly = x^7 + x^3 + 1
                    Return &H89
                Case 8 '8-th: poly = x^8 + x^4 + x^3 + x^2 + 1
                    Return &H11D
                Case 9 '9-th: poly = x^9 + x^4 + 1 
                    Return &H211
                Case 10  '10-th: poly = x^10 + x^3 + 1
                    Return &H409
                Case 11 '11-th: poly = x^11 + x^2 + 1
                    Return &H805
                Case 12 '12-th: poly = x^12 + x^6 + x^4 + x + 1
                    Return &H1053
                Case 13 '13-th: poly = x^13 + x^4 + x^3 + x + 1
                    Return &H201B
                Case 14 '14-th: poly = x^14 + x^10 + x^6 + x + 1
                    Return &H4443
                Case 15 '15-th: poly = x^15 + x + 1
                    Return &H8003
                Case 16 '16-th: poly = x^16 + x^12 + x^3 + x + 1
                    Return &H1100B
            End Select
            Return -1
        End Function
        'Converts data stored in 32-bit to byte by the specified symbol width
        Private Function symbols_to_bytes(ByVal data_in() As Integer, ByVal sym_width As Integer) As Byte()
            Dim ecc_size As Integer = Math.Ceiling((data_in.Length * sym_width) / 8)
            Dim data_out(ecc_size - 1) As Byte 'This needs to be 0x00
            Dim counter As Integer = 0
            Dim spare_data As Byte = 0
            Dim bits_left As Integer = 0
            For Each item As Integer In data_in
                Do
                    Dim next_bits As Integer = (8 - bits_left)
                    bits_left = (sym_width - next_bits)
                    Dim byte_to_add As Byte = (spare_data << next_bits) Or ((item >> bits_left) And ((1 << next_bits) - 1))
                    data_out(counter) = byte_to_add : counter += 1
                    Dim x As Integer = Math.Min(bits_left, 8)
                    Dim offset As Integer = (bits_left - x)
                    spare_data = ((item And ((1 << x) - 1) << offset) >> offset)
                    If x = 8 Then
                        data_out(counter) = spare_data : counter += 1
                        bits_left -= 8
                        spare_data = (item And ((1 << offset) - 1))
                    End If
                Loop While bits_left > 8
            Next
            If (bits_left > 0) Then
                Dim next_bits As Integer = (8 - bits_left)
                Dim byte_to_add As Byte = (spare_data << next_bits)
                data_out(counter) = byte_to_add  'Last byte
            End If
            Return data_out
        End Function

        Private Function bytes_to_symbols(ByVal data_in() As Byte, ByVal sym_width As Integer) As Integer()
            Dim sym_count As Integer = Math.Ceiling((data_in.Length * 8) / sym_width)
            Dim data_out(sym_count - 1) As Integer
            Dim counter As Integer = 0
            Dim int_data As Integer
            Dim bit_offset As Integer = 0
            For i = 0 To data_in.Length - 1
                Dim data_in_bitcount As Integer = 8
                Do
                    Dim bits_left As Integer = (sym_width - bit_offset) 'number of bits our int_data needed
                    Dim sel_count As Integer = Math.Min(bits_left, data_in_bitcount) 'number of bits we can pull from the current byte
                    Dim target_offset As Integer = sym_width - (bit_offset + sel_count)
                    data_in_bitcount -= sel_count
                    Dim src_offset As Integer = data_in_bitcount
                    Dim bit_mask As Byte = ((1 << sel_count) - 1) << src_offset
                    Dim data_selected As Integer = (data_in(i) And bit_mask) >> src_offset
                    int_data = int_data Or (data_selected << target_offset)
                    bit_offset += sel_count
                    If bit_offset = sym_width Then
                        data_out(counter) = int_data : counter += 1
                        bit_offset = 0
                        int_data = 0
                    End If
                Loop While data_in_bitcount > 0
            Next
            If (bit_offset > 0) Then
                data_out(counter) = int_data
            End If
            Return data_out
        End Function

    End Class

    Public Class BinaryBHC
        Private BCH_CONST_M As Integer = 13 '512 bytes, 14=1024
        Private BCH_CONST_T As Integer '1,2,4,8,10,14

        Public Property PARITY_BITS As Integer
            Get
                Return BCH_CONST_T
            End Get
            Set(value As Integer)
                BCH_CONST_T = value
            End Set
        End Property

        Sub New()

        End Sub

        Public Function GetEccSize() As Integer
            Select Case PARITY_BITS
                Case 1
                    Return 2
                Case 2
                    Return 4
                Case 4
                    Return 7
                Case 8
                    Return 13
                Case 10
                    Return 17
                Case 14
                    Return 23
                Case Else
                    Return -1
            End Select
        End Function

        Public Function GenerateECC(ByVal block() As Byte) As Byte()
            Using BchControl As New ECC.BCH(BCH_CONST_M, BCH_CONST_T)
                Dim data() As Byte = BchControl.Encode(block)
                Return data
            End Using
        End Function

        Public Function ProcessECC(ByRef block() As Byte, ByRef stored_ecc() As Byte) As ECC_DECODE_RESULT
            Using BchControl As New ECC.BCH(BCH_CONST_M, BCH_CONST_T)
                Dim cmp_result As ECC.CompareResult = BchControl.Decode(block, stored_ecc)
                Select Case cmp_result
                    Case ECC.CompareResult.NoError
                        Return ECC_DECODE_RESULT.NoErrors
                    Case ECC.CompareResult.EccError
                        Return ECC_DECODE_RESULT.EccError
                    Case ECC.CompareResult.Correctable
                        Return ECC_DECODE_RESULT.Correctable
                    Case Else
                        Return ECC_DECODE_RESULT.Uncorractable
                End Select
            End Using
        End Function

    End Class

    Public Class Engine
        Private ecc_mode As ecc_algorithum
        Private ecc_hamming As New Hamming
        Private ecc_reedsolomon As New RS_ECC
        Private ecc_bhc As New BinaryBHC

        Public Property ECC_DATA_LOCATION As Byte '= ecc_location_opt.half_of_oob_page
        Public Property ECC_SEPERATE As Boolean 'We need to seperate each spare into sections
        Public Property REVERSE_ARRAY As Boolean = False 'RS option allows to reverse the input byte array

        Sub New(mode As ecc_algorithum, parity_level As Integer)
            Me.ecc_mode = mode
            Select Case ecc_mode
                Case ecc_algorithum.hamming 'Hamming only supports 1-bit ECC correction
                Case ecc_algorithum.reedsolomon
                    ecc_reedsolomon.PARITY_BITS = parity_level
                Case ecc_algorithum.bhc
                    ecc_bhc.PARITY_BITS = parity_level
            End Select
        End Sub

        Public Function GenerateECC(data_in() As Byte) As Byte()
            If Me.REVERSE_ARRAY Then Array.Reverse(data_in)
            Select Case Me.ecc_mode
                Case ecc_algorithum.hamming 'Hamming only supports 1-bit ECC correction
                    Return ecc_hamming.GenerateECC(data_in)
                Case ecc_algorithum.reedsolomon
                    Return ecc_reedsolomon.GenerateECC(data_in)
                Case ecc_algorithum.bhc
                    Return ecc_bhc.GenerateECC(data_in)
            End Select
            Return Nothing
        End Function
        'Processes blocks of 512 bytes and returns the last decoded result
        Public Function ReadData(data_in() As Byte, ecc() As Byte) As ECC_DECODE_RESULT
            Dim result As ECC_DECODE_RESULT = ECC_DECODE_RESULT.NoErrors
            Try
                If Utilities.IsByteArrayFilled(ecc, 255) Then Return ECC_DECODE_RESULT.NoErrors 'ECC area does not contain ECC data
                If Not (data_in.Length Mod 512 = 0) Then Return ECC_DECODE_RESULT.InputError
                If Me.REVERSE_ARRAY Then Array.Reverse(data_in)
                Dim ecc_byte_size As Integer = GetEccByteSize()
                Dim ecc_ptr As Integer = 0
                For i = 1 To data_in.Length Step 512
                    Dim block(511) As Byte
                    Array.Copy(data_in, i - 1, block, 0, 512)
                    Dim ecc_data(ecc_byte_size - 1) As Byte
                    Array.Copy(ecc, ecc_ptr, ecc_data, 0, ecc_data.Length)
                    Select Case ecc_mode
                        Case ecc_algorithum.hamming
                            result = ecc_hamming.ProcessECC(block, ecc_data)
                        Case ecc_algorithum.reedsolomon
                            result = ecc_reedsolomon.ProcessECC(block, ecc_data)
                        Case ecc_algorithum.bhc
                            result = ecc_bhc.ProcessECC(block, ecc_data)
                    End Select
                    If result = ECC_DECODE_RESULT.Uncorractable Then
                        Return ECC_DECODE_RESULT.Uncorractable
                    ElseIf result = ECC_DECODE_RESULT.Correctable Then
                        Array.Copy(block, 0, data_in, i - 1, 512)
                    End If
                    ecc_ptr += ecc_byte_size
                Next
            Catch ex As Exception
            End Try
            Return result
        End Function

        Public Sub WriteData(data_in() As Byte, ByRef ecc() As Byte)
            Try
                If Not (data_in.Length Mod 512 = 0) Then Exit Sub
                If Me.REVERSE_ARRAY Then Array.Reverse(data_in)
                Dim ecc_byte_size As Integer = GetEccByteSize()
                Dim blocks As Integer = (data_in.Length / 512)
                ReDim ecc((blocks * ecc_byte_size) - 1)
                Utilities.FillByteArray(ecc, 255)
                Dim ecc_ptr As Integer = 0
                For i = 1 To data_in.Length Step 512
                    Dim block(511) As Byte
                    Array.Copy(data_in, i - 1, block, 0, 512)
                    Dim ecc_data() As Byte = Nothing
                    Select Case ecc_mode
                        Case ecc_algorithum.hamming
                            ecc_data = ecc_hamming.GenerateECC(block)
                        Case ecc_algorithum.reedsolomon
                            ecc_data = ecc_reedsolomon.GenerateECC(block)
                        Case ecc_algorithum.bhc
                            ecc_data = ecc_bhc.GenerateECC(block)
                    End Select
                    Array.Copy(ecc_data, 0, ecc, ecc_ptr, ecc_data.Length)
                    ecc_ptr += ecc_byte_size
                Next
            Catch ex As Exception
            End Try
        End Sub

        Public Function GetEccByteSize() As Integer
            Select Case ecc_mode
                Case ecc_algorithum.hamming
                    Return 3
                Case ecc_algorithum.reedsolomon
                    Return ecc_reedsolomon.GetEccSize
                Case ecc_algorithum.bhc
                    Return ecc_bhc.GetEccSize
            End Select
            Return -1
        End Function

        Public Function GetEccFromSpare(spare() As Byte, page_size As UInt16, oob_size As UInt16) As Byte()
            Dim bytes_per_ecc As Integer = Me.GetEccByteSize
            Dim sub_pages As Integer = (page_size / 512)
            Dim page_count As Integer = (spare.Length / oob_size)
            Dim seperate_ecc As Boolean = False
            If MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Separated Then
                seperate_ecc = Me.ECC_SEPERATE
            End If
            Dim ecc_data(page_count * (sub_pages * bytes_per_ecc) - 1) As Byte
            Utilities.FillByteArray(ecc_data, 255)
            Dim ptr As Integer = 0
            For x = 0 To page_count - 1
                Dim page_offset As Integer = (x * oob_size)
                Dim page_ecc_size As Integer = (bytes_per_ecc * sub_pages)
                Dim sub_page_size As Integer = (oob_size / sub_pages)
                If seperate_ecc AndAlso (sub_pages > 1) Then
                    If (ECC_DATA_LOCATION + bytes_per_ecc) > sub_page_size Then Return ecc_data
                    For i = 0 To sub_pages - 1
                        Dim base_ptr As Integer = (i * sub_page_size) + Me.ECC_DATA_LOCATION
                        Array.Copy(spare, page_offset + base_ptr, ecc_data, ptr, bytes_per_ecc)
                        ptr += bytes_per_ecc
                    Next
                Else
                    If ((Me.ECC_DATA_LOCATION + page_ecc_size) > oob_size) Then Return ecc_data
                    Array.Copy(spare, page_offset + Me.ECC_DATA_LOCATION, ecc_data, ptr, page_ecc_size)
                    ptr += page_ecc_size
                End If
            Next
            Return ecc_data
        End Function
        'Writes the ECC bytes into the spare area
        Public Sub SetEccToSpare(spare() As Byte, ecc_data() As Byte, page_size As UInt16, oob_size As UInt16)
            Dim bytes_per_ecc As Integer = Me.GetEccByteSize
            Dim sub_pages As Integer = (page_size / 512)
            Dim page_count As Integer = (spare.Length / oob_size)
            Dim seperate_ecc As Boolean = False
            If MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Separated Then
                seperate_ecc = Me.ECC_SEPERATE
            End If
            Dim ptr As Integer = 0
            For x = 0 To page_count - 1
                Dim page_offset As Integer = (x * oob_size)
                Dim page_ecc(bytes_per_ecc * sub_pages - 1) As Byte
                Dim sub_page_size As Integer = (oob_size / sub_pages)
                Array.Copy(ecc_data, ptr, page_ecc, 0, page_ecc.Length)
                ptr += page_ecc.Length
                If seperate_ecc AndAlso (sub_pages > 1) Then
                    If (ECC_DATA_LOCATION + bytes_per_ecc) > sub_page_size Then Exit Sub
                    For i = 0 To sub_pages - 1
                        Dim base_ptr As Integer = (i * sub_page_size)
                        Dim ecc_ptr As Integer = (i * bytes_per_ecc)
                        Array.Copy(page_ecc, ecc_ptr, spare, page_offset + base_ptr + Me.ECC_DATA_LOCATION, bytes_per_ecc)
                    Next
                Else
                    If (page_ecc.Length + ECC_DATA_LOCATION > oob_size) Then Exit Sub
                    Array.Copy(page_ecc, 0, spare, page_offset + Me.ECC_DATA_LOCATION, page_ecc.Length)
                End If
            Next
        End Sub

        Public Sub SetSymbolWidth(bit_width As Integer)
            If Me.ecc_mode = ecc_algorithum.reedsolomon Then
                ecc_reedsolomon.SYM_WIDTH = bit_width
            End If
        End Sub

    End Class

    Public Enum ECC_DECODE_RESULT
        NoErrors 'all bits and parity match
        Correctable 'one or more bits dont match but was corrected
        EccError 'the error is in the ecc
        Uncorractable 'more errors than are correctable
        InputError 'User sent data that was not in 512 byte segments
    End Enum

    Public Enum ecc_algorithum As Integer
        hamming = 0
        reedsolomon = 1
        bhc = 2
    End Enum

    Public Module Common

        Public Function ECC_GetErrorMessage(err As ECC_DECODE_RESULT) As String
            Select Case err
                Case ECC_DECODE_RESULT.NoErrors
                    Return "No Errors found"
                Case ECC_DECODE_RESULT.Correctable
                    Return "Data Corrected"
                Case ECC_DECODE_RESULT.EccError
                    Return "ECC data error"
                Case ECC_DECODE_RESULT.Uncorractable
                    Return "Too many errors"
                Case ECC_DECODE_RESULT.InputError
                    Return "Block data was invalid"
                Case Else
                    Return ""
            End Select
        End Function


    End Module

End Namespace