Public Module ECC

    'Public Function Denary2BaseX(ByVal i As Integer, ByVal b As Integer) As String
    '    Dim r As Integer = 0, n As String = "", c As String = "0123456789ABCDEF", d As Boolean = False
    '    Do Until i = 0
    '        r = i Mod b
    '        i = i \ b
    '        n = c.Substring(r, 1) & n
    '    Loop
    '    Return n
    'End Function

    'Public Function GetHammingCode(ByVal data() As Byte) As String
    '    Dim hDistance As Integer = 0
    '    Dim ex1 As Integer = 0
    '    Dim hCalculated As Boolean = False
    '    Dim hammingCode() As Byte

    '    'Calculate hammering distance.
    '    Do While hCalculated = False
    '        'If 2 to the power of "a" is less than the length of the binary array (since we are using zero-based arrays), hamming length could be this value.
    '        If (2 ^ ex1) <= data.Count Then
    '            hDistance = (2 ^ ex1)
    '            ex1 += 1
    '        ElseIf (2 ^ ex1) > data.Count Then
    '            'Otherwise, if a supposed hamming length puts us equal to the length of the binary array, or over, we've already found the hamming length.
    '            hCalculated = True
    '        End If
    '    Loop

    '    ex1 -= 1
    '    'Set the expected length of the binary word.
    '    ReDim hammingCode(ex1 + data.Count)
    '    Dim ex2 As Integer = 0
    '    Dim binaryPos As Integer = 0
    '    Dim hCodePos As Integer = 0

    '    'Build the template hamming code.
    '    Do Until ex2 = (ex1 + 1)
    '        hammingCode((2 ^ ex2) - 1) = "_"
    '        ex2 += 1
    '    Loop

    '    Do Until hCodePos = hammingCode.Count
    '        If hammingCode(hCodePos) <> "_" Then
    '            hammingCode(hCodePos) = data(binaryPos)
    '            binaryPos += 1
    '        End If
    '        hCodePos += 1
    '    Loop


    '    Dim ex3 As Integer = 0
    '    Dim skipCount As Integer = (2 ^ ex3)
    '    Dim readTo As Integer = 0
    '    Dim readPos As Integer = 0

    '    'Find the value of each parity bit.

    '    Do Until ex3 = (ex1 + 1)

    '        skipCount = (2 ^ ex3)
    '        readPos = (2 ^ ex3) - 1

    '        Dim numberOfOnes As Integer = 0
    '        Dim numberOfZeroes As Integer = 0

    '        Do Until readPos >= hammingCode.Count
    '            Dim clusterBits((2 ^ ex3) - 1) As Char
    '            Dim clusterBit As Integer = 0

    '            Do Until clusterBit = clusterBits.Count
    '                If readPos < hammingCode.Count Then
    '                    clusterBits(clusterBit) = hammingCode(readPos)
    '                ElseIf readPos >= hammingCode.Count Then
    '                    For Each bit As Char In clusterBits
    '                        If bit = Nothing Then
    '                            bit = "N" 'Use this to fill blank space.
    '                        End If
    '                    Next
    '                End If
    '                clusterBit += 1
    '                readPos += 1
    '            Loop

    '            'Find the number of zeroes and ones in this bunch.
    '            For Each bit As Char In clusterBits
    '                If bit = "_" Or bit = "0" Then
    '                    numberOfZeroes += 1
    '                ElseIf bit = "1" Then
    '                    numberOfOnes += 1
    '                ElseIf bit = "N" Then
    '                    'Just do nothing here.
    '                End If
    '            Next

    '            readPos += skipCount
    '        Loop

    '        'Determine the value of the parity bit.
    '        If IsEven(numberOfOnes) = True Then
    '            hammingCode((2 ^ ex3) - 1) = "0"

    '        Else
    '            hammingCode((2 ^ ex3) - 1) = "1"
    '        End If

    '        ex3 += 1
    '    Loop

    '    Return hammingCode
    'End Function

    'Public Function IsEven(ByVal Number As Long) As Boolean
    '    IsEven = (Number Mod 2 = 0)
    'End Function


End Module
