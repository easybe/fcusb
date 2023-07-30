
Namespace ECC_LIB

    Public Interface ECC_Engine
        Function GetEccByteSize() As UInt16
        Function GetEccFromSpare(spare() As Byte, page_size As UInt16, oob_size As UInt16) As Byte()
        Sub SetEccToSpare(spare() As Byte, ecc_data() As Byte, page_size As UInt16, oob_size As UInt16)
        Sub ReadData(data_in() As Byte, ecc() As Byte)
        Sub WriteData(data_in() As Byte, ByRef ecc() As Byte)
        Function GetLastResult() As ECC_DECODE_RESULT
    End Interface

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

    Public Module Common
        Public NAND_ECC_CFG() As ECC_Configuration_Entry

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
                    Return String.Empty
            End Select
        End Function

        Public Function GetEccDataSize(item As ECC_Configuration_Entry) As UInt16
            Select Case item.Algorithm
                Case ecc_algorithum.hamming
                    Return 3
                Case ecc_algorithum.reedsolomon
                    If item.SymSize = 9 Then
                        Select Case item.BitError
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
                    ElseIf item.SymSize = 10 Then
                        Select Case item.BitError
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
                Case ecc_algorithum.bhc
                    Select Case item.BitError
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
            End Select
            Return 0
        End Function

        Public Sub ECC_Init()
            NAND_ECC_CFG = GenerateLocalEccConfigurations()
        End Sub

        Private Function GenerateLocalEccConfigurations() As ECC_Configuration_Entry()
            Dim cfg_list As New List(Of ECC_Configuration_Entry)

            Dim n1 As New ECC_Configuration_Entry()
            n1.PageSize = 512
            n1.SpareSize = 16
            n1.Algorithm = ecc_algorithum.hamming
            n1.BitError = 1
            n1.SymSize = 0
            n1.ReverseData = False
            n1.AddRegion(&HD)

            Dim n2 As New ECC_Configuration_Entry()
            n2.PageSize = 2048
            n2.SpareSize = 64
            n2.Algorithm = ecc_algorithum.reedsolomon
            n2.BitError = 4
            n2.SymSize = 9
            n2.ReverseData = False
            n2.AddRegion(&H7)
            n2.AddRegion(&H17)
            n2.AddRegion(&H27)
            n2.AddRegion(&H37)

            Dim n3 As New ECC_Configuration_Entry()
            n3.PageSize = 2048
            n3.SpareSize = 128
            n3.Algorithm = ecc_algorithum.bhc
            n3.BitError = 8
            n3.SymSize = 0
            n3.ReverseData = False
            n3.AddRegion(&H13)
            n3.AddRegion(&H33)
            n3.AddRegion(&H53)
            n3.AddRegion(&H73)

            cfg_list.Add(n1)
            cfg_list.Add(n2)
            cfg_list.Add(n3)

            Return cfg_list.ToArray
        End Function

    End Module


End Namespace






