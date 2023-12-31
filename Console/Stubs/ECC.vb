﻿Namespace ECC_LIB
    '512 byte processing (24-bit ECC) 1-bit correctable/2-bit detection
    Public Class Hamming

        Sub New()

        End Sub

#Region "Parity Table"
        Private byte_parity_table() As Byte = {
                &HFF, &HD4, &HD2, &HF9, &HCC, &HE7, &HE1, &HCA,
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
            Dim uddata() As UInt32 = Utilities.Bytes.ToUIntArray(block)
            For j As Integer = 0 To 127
                Dim temp As UInt32 = uddata(j)
                If CBool(j And &H1) Then LP5 = LP5 Xor temp Else LP4 = LP4 Xor temp
                If CBool(j And &H2) Then LP7 = LP7 Xor temp Else LP6 = LP6 Xor temp
                If CBool(j And &H4) Then LP9 = LP9 Xor temp Else LP8 = LP8 Xor temp
                If CBool(j And &H8) Then LP11 = LP11 Xor temp Else LP10 = LP10 Xor temp
                If CBool(j And &H10) Then LP13 = LP13 Xor temp Else LP12 = LP12 Xor temp
                If CBool(j And &H20) Then LP15 = LP15 Xor temp Else LP14 = LP14 Xor temp
                If CBool(j And &H40) Then LP17 = LP17 Xor temp Else LP16 = LP16 Xor temp
            Next
            Dim reg32 As UInt32 = (LP15 Xor LP14)
            Dim byte_reg As Byte = CByte((reg32) And &HFF)
            byte_reg = byte_reg Xor CByte((reg32 >> 8) And &HFF)
            byte_reg = byte_reg Xor CByte((reg32 >> 16) And &HFF)
            byte_reg = byte_reg Xor CByte((reg32 >> 24) And &HFF)
            byte_reg = byte_parity_table(byte_reg)
            word_reg = (CUShort(LP16 >> 16)) Xor (CUShort(LP16 And &HFFFF))
            LP16 = CByte(CByte(word_reg And &HFF) Xor CByte(word_reg >> 8))
            word_reg = CUShort(LP17 >> 16) Xor CUShort(LP17 And &HFFFF)
            LP17 = CByte(CByte(word_reg And &HFF) Xor CByte(word_reg >> 8))
            ecc_code(2) = ((byte_reg And CByte(&HFE)) << 1) Or byte_parity_table(CByte(LP16 And &HFF) And CByte(&H1)) Or (byte_parity_table(CByte(LP17 And &HFF) And CByte(&H1)) << 1)
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

            ecc_code(0) = (byte_parity_table(CByte(LP0 And 255)) And CByte(&H1)) Or
                ((byte_parity_table(CByte(LP1 And 255)) And CByte(&H1)) << 1) Or
                ((byte_parity_table(CByte(LP2 And 255)) And CByte(&H1)) << 2) Or
                ((byte_parity_table(CByte(LP3 And 255)) And CByte(&H1)) << 3) Or
                ((byte_parity_table(CByte(LP4 And 255)) And CByte(&H1)) << 4) Or
                ((byte_parity_table(CByte(LP5 And 255)) And CByte(&H1)) << 5) Or
                ((byte_parity_table(CByte(LP6 And 255)) And CByte(&H1)) << 6) Or
                ((byte_parity_table(CByte(LP7 And 255)) And CByte(&H1)) << 7)

            ecc_code(1) = ((byte_parity_table(CByte(LP8 And 255)) And CByte(&H1))) Or
                (byte_parity_table(CByte(LP9 And 255)) And CByte(&H1)) << 1 Or
                (byte_parity_table(CByte(LP10 And 255)) And CByte(&H1)) << 2 Or
                (byte_parity_table(CByte(LP11 And 255)) And CByte(&H1)) << 3 Or
                (byte_parity_table(CByte(LP12 And 255)) And CByte(&H1)) << 4 Or
                (byte_parity_table(CByte(LP13 And 255)) And CByte(&H1)) << 5 Or
                (byte_parity_table(CByte(LP14 And 255)) And CByte(&H1)) << 6 Or
                (byte_parity_table(CByte(LP15 And 255)) And CByte(&H1)) << 7

            Return ecc_code
        End Function
        'Processed a block of 512 bytes with 24-bit ecc code word data, corrects if needed
        Public Function ProcessECC(block() As Byte, ByRef stored_ecc() As Byte) As ECC_DECODE_RESULT
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
                    Dim bit_addr As Byte = ((ecc_xor(2) >> 3) And CByte(1)) Or ((ecc_xor(2) >> 4) And CByte(2)) Or ((ecc_xor(2) >> 5) And CByte(4))
                    Dim byte_addr As UInt16 =
                    (ecc_xor(0) >> 1) And &H1US Or
                    (ecc_xor(0) >> 2) And &H2US Or
                    (ecc_xor(0) >> 3) And &H4US Or
                    (ecc_xor(0) >> 4) And &H8US Or
                    (ecc_xor(1) << 3) And &H10US Or
                    (ecc_xor(1) << 2) And &H20US Or
                    (ecc_xor(1) << 1) And &H40US Or
                    (ecc_xor(1) << 0) And &H80US Or
                    (ecc_xor(2) << 7) And &H100US
                    block(byte_addr - 1) = block(byte_addr - 1) Xor (CByte(1) << bit_addr)
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

        Public Function GetEccSize() As UInt16
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
            Return 0
        End Function

        Public Function GenerateECC(block() As Byte) As Byte()
            Return Nothing
        End Function

        Public Function ProcessECC(ByRef block() As Byte, ByRef stored_ecc() As Byte) As ECC_DECODE_RESULT
            Return Nothing
        End Function

        Private Function GetPolynomial(sym_size As Integer) As Integer
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
        Private Function symbols_to_bytes(data_in() As Integer, sym_width As Integer) As Byte()
            Dim ecc_size As Integer = CInt(Math.Ceiling((data_in.Length * sym_width) / 8))
            Dim data_out(ecc_size - 1) As Byte 'This needs to be 0x00
            Dim counter As Integer = 0
            Dim spare_data As Byte = 0
            Dim bits_left As Integer = 0
            For Each item As Integer In data_in
                Do
                    Dim next_bits As Integer = (8 - bits_left)
                    bits_left = (sym_width - next_bits)
                    Dim byte_to_add As Byte = CByte((spare_data << next_bits) Or ((item >> bits_left) And ((1 << next_bits) - 1)))
                    data_out(counter) = byte_to_add : counter += 1
                    Dim x As Integer = Math.Min(bits_left, 8)
                    Dim offset As Integer = (bits_left - x)
                    spare_data = CByte((item And ((1 << x) - 1) << offset) >> offset)
                    If x = 8 Then
                        data_out(counter) = spare_data : counter += 1
                        bits_left -= 8
                        spare_data = CByte(item And ((1 << offset) - 1))
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

        Private Function bytes_to_symbols(data_in() As Byte, sym_width As Integer) As Integer()
            Dim sym_count As Integer = CInt(Math.Ceiling((data_in.Length * 8) / sym_width))
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
                    Dim bit_mask As Byte = CByte(((1 << sel_count) - 1) << src_offset)
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

        Public Function GetEccSize() As UInt16
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
                    Return 0
            End Select
        End Function

        Public Function GenerateECC(block() As Byte) As Byte()
            Return Nothing
        End Function

        Public Function ProcessECC(ByRef block() As Byte, ByRef stored_ecc() As Byte) As ECC_DECODE_RESULT
            Return Nothing
        End Function

    End Class


    Public Class Engine
        Private ecc_mode As ecc_algorithum
        Private ecc_hamming As New Hamming
        Private ecc_reedsolomon As New RS_ECC
        Private ecc_bhc As New BinaryBHC

        Public ECC_DATA_LOCATION() As UInt16
        Public Property REVERSE_ARRAY As Boolean = False 'RS option allows to reverse the input byte array

        Public ReadOnly Property SYM_WIDTH As Integer = 9 'Used by RS

        Sub New(mode As ecc_algorithum, Optional parity_level As Integer = 1, Optional symbole_width As Integer = 0)
            Me.ecc_mode = mode
            Me.SYM_WIDTH = symbole_width
            Select Case ecc_mode
                Case ecc_algorithum.hamming 'Hamming only supports 1-bit ECC correction
                Case ecc_algorithum.reedsolomon
                    ecc_reedsolomon.SYM_WIDTH = symbole_width
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
                        Array.Copy(block, 0, data_in, i - 1, 512) 'Copies the correct block into the current page data
                        result = ECC_DECODE_RESULT.NoErrors
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
                Dim blocks As Integer = (data_in.Length \ 512)
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

        Public Function GetEccByteSize() As UInt16
            Select Case ecc_mode
                Case ecc_algorithum.hamming
                    Return 3
                Case ecc_algorithum.reedsolomon
                    Return ecc_reedsolomon.GetEccSize()
                Case ecc_algorithum.bhc
                    Return ecc_bhc.GetEccSize()
            End Select
            Return 0
        End Function

        Public Function GetEccFromSpare(spare() As Byte, page_size As UInt16, oob_size As UInt16) As Byte()
            Dim bytes_per_ecc As Integer = Me.GetEccByteSize() 'Number of ECC byters per sector
            Dim obb_count As Integer = (spare.Length \ oob_size) 'Number of OOB areas to process
            Dim sector_count As Integer = (page_size \ 512) 'Each OOB contains this many sectors
            Dim ecc_data((obb_count * (sector_count * bytes_per_ecc)) - 1) As Byte
            Dim ecc_data_ptr As Integer = 0
            Dim spare_ptr As Integer = 0
            For x = 0 To obb_count - 1
                For y = 0 To sector_count - 1
                    Dim ecc_offset As UInt16 = Me.ECC_DATA_LOCATION(y)
                    Array.Copy(spare, spare_ptr + ecc_offset, ecc_data, ecc_data_ptr, bytes_per_ecc)
                    ecc_data_ptr += bytes_per_ecc
                Next
                spare_ptr += oob_size
            Next
            Return ecc_data
        End Function
        'Writes the ECC bytes into the spare area
        Public Sub SetEccToSpare(spare() As Byte, ecc_data() As Byte, page_size As UInt16, oob_size As UInt16)
            Dim bytes_per_ecc As Integer = Me.GetEccByteSize() 'Number of ECC byters per sector
            Dim obb_count As Integer = (spare.Length \ oob_size) 'Number of OOB areas to process
            Dim sector_count As Integer = (page_size \ 512) 'Each OOB contains this many sectors
            Dim ecc_data_ptr As Integer = 0
            Dim spare_ptr As Integer = 0
            For x = 0 To obb_count - 1
                For y = 0 To sector_count - 1
                    Dim ecc_offset As UInt16 = Me.ECC_DATA_LOCATION(y)
                    Array.Copy(ecc_data, ecc_data_ptr, spare, ecc_offset + spare_ptr, bytes_per_ecc)
                    ecc_data_ptr += bytes_per_ecc
                Next
                spare_ptr += oob_size
            Next
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

        Public Function GetEccDataSize(item As ECC_Configuration_Entry) As UInt16
            Dim ecc_eng_example As New Engine(item.Algorithm, item.BitError, item.SymSize)
            Return ecc_eng_example.GetEccByteSize()
        End Function

    End Module

    Public Class ECC_Configuration_Entry
        Public Property PageSize As UInt16
        Public Property SpareSize As UInt16
        Public Property Algorithm As ecc_algorithum
        Public Property BitError As Byte 'Number of bits that can be corrected
        Public Property SymSize As Byte 'Number of bits per symbol (RS only)
        Public Property ReverseData As Boolean 'Reverses ECC byte order

        Public EccRegion() As UInt16

        Sub New()

        End Sub

        Public Function IsValid() As Boolean
            If Me.PageSize = 0 OrElse Not Me.PageSize Mod 512 = 0 Then Return False
            Dim sector_count As Integer = (Me.PageSize \ 512)
            Dim ecc_data_size As UInt16 = GetEccDataSize(Me)
            If EccRegion Is Nothing Then Return False
            If Not EccRegion.Length = sector_count Then Return False
            For i = 0 To EccRegion.Length - 1
                Dim start_addr As UInt16 = EccRegion(i)
                Dim end_addr As UInt16 = start_addr + ecc_data_size - 1US
                If end_addr >= Me.SpareSize Then Return False
                For x = 0 To EccRegion.Length - 1
                    If (i <> x) Then
                        Dim target_addr As UInt16 = EccRegion(x)
                        Dim target_end As UInt16 = target_addr + ecc_data_size - 1US
                        If start_addr >= target_addr And start_addr <= target_end Then
                            Return False
                        End If
                    End If
                Next
            Next
            Return True
        End Function


        Public Sub AddRegion(offset As UInt16)
            If EccRegion Is Nothing Then
                ReDim EccRegion(0)
                EccRegion(0) = offset
            Else
                ReDim Preserve EccRegion(EccRegion.Length)
                EccRegion(EccRegion.Length - 1) = offset
            End If
        End Sub

    End Class



End Namespace