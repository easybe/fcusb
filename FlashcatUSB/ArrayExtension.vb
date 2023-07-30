Imports System.Runtime.CompilerServices

Module ArrayExtensions
    <Extension()>
    Public Sub RemoveAt(Of T)(ByRef a() As T, ByVal index As Integer)
        Array.Copy(a, index + 1, a, index, UBound(a) - index)
        ReDim Preserve a(UBound(a) - 1) 'Remove the element
    End Sub

    <Extension()>
    Public Function Slice(Of T)(data() As T, ByVal start_index As Integer, ByVal length As Integer) As Byte()
        Dim d(length - 1) As Byte
        Array.Copy(data, start_index, d, 0, length)
        Return d
    End Function

    <Extension()>
    Public Function Reverse(Of T)(data() As T) As Byte()
        Dim d(data.Length - 1) As Byte
        Array.Copy(data, 0, d, 0, data.Length)
        Array.Reverse(d)
        Return d
    End Function


End Module