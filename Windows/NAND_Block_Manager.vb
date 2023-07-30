
Imports FlashcatUSB.FlashMemory

Public Class NAND_BLOCK_IF
    Public Event ReadPages(page_addr As Integer, page_offset As UInt16, count As Integer, area As FlashArea, ByRef data() As Byte)

    Public Property MAPPED_PAGES As Integer  'Number of pages available in the current map
    Public Property VALID_BLOCKS As Integer 'Number of valid blocks we can access

    Public MAP As New List(Of MAPPING) 'Map of all blocks with page addressing for logicial->physical

    Private Property PAGE_COUNT As UInt16 'Number of pages in the block
    Private Property BLOCK_COUNT As Integer  'Number of blocks

    Sub New(nand_device As G_NAND)
        Me.PAGE_COUNT = nand_device.PAGE_COUNT
        Me.BLOCK_COUNT = nand_device.BLOCK_COUNT
        MAP.Clear()
        For block_index As Integer = 0 To Me.BLOCK_COUNT - 1
            Dim block_info As New MAPPING
            block_info.Status = BLOCK_STATUS.Valid
            block_info.BlockIndex = block_index
            block_info.PagePhysical = CInt(Me.PAGE_COUNT) * block_index
            block_info.PageLogical = CInt(Me.PAGE_COUNT) * block_index
            MAP.Add(block_info)
        Next
        ProcessMap()
    End Sub

    Public Enum BLOCK_STATUS
        Valid 'Marked valid by the manager or by the user
        Bad_Manager 'This block was marked bad because of the manager setting
        Bad_Marked 'This block was marked bad because of the user
        Bad_ByError 'This block was marked bad because of programming
        Unknown
    End Enum

    Public Class MAPPING
        Public Status As BLOCK_STATUS
        Public BlockIndex As Integer  'Index of the block
        Public PagePhysical As Integer 'The physical address of the first page of this block
        Public PageLogical As Integer 'The mapped address of the first page of this block
    End Class

    Public Function EnableBlockManager(markers As BadBlockMarker) As Boolean
        Try
            Dim page_addr As Integer = 0
            For i As Integer = 0 To Me.BLOCK_COUNT - 1
                Dim LastPageAddr As Integer = (page_addr + PAGE_COUNT - 1) 'The last page of this block
                Dim page_one() As Byte = Nothing
                Dim page_two() As Byte = Nothing
                Dim page_last() As Byte = Nothing
                Dim valid_block As Boolean = True
                If (markers And BadBlockMarker._1stByte_FirstPage) > 0 Then
                    If page_one Is Nothing Then RaiseEvent ReadPages(page_addr, 0, 6, FlashArea.OOB, page_one)
                    If Not ((page_one(0)) = 255) Then valid_block = False
                End If
                If (markers And BadBlockMarker._1stByte_SecondPage) > 0 Then
                    If page_two Is Nothing Then RaiseEvent ReadPages(page_addr + 1, 0, 6, FlashArea.OOB, page_two)
                    If Not ((page_two(0)) = 255) Then valid_block = False
                End If
                If (markers And BadBlockMarker._1stByte_LastPage) > 0 Then
                    If page_last Is Nothing Then RaiseEvent ReadPages(LastPageAddr, 0, 6, FlashArea.OOB, page_last)
                    If Not ((page_last(0)) = 255) Then valid_block = False
                End If
                If (markers And BadBlockMarker._6thByte_FirstPage) > 0 Then
                    If page_one Is Nothing Then RaiseEvent ReadPages(page_addr, 0, 6, FlashArea.OOB, page_one)
                    If Not ((page_one(5)) = 255) Then valid_block = False
                End If
                If (markers And BadBlockMarker._6thByte_SecondPage) > 0 Then
                    If page_two Is Nothing Then RaiseEvent ReadPages(page_addr + 1, 0, 6, FlashArea.OOB, page_two)
                    If Not ((page_two(5)) = 255) Then valid_block = False
                End If
                If (Not valid_block) Then
                    MAP(i).Status = BLOCK_STATUS.Bad_Manager
                Else
                    MAP(i).Status = BLOCK_STATUS.Valid
                End If
                page_addr += PAGE_COUNT
            Next
            ProcessMap()
            Return True
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Sub ProcessMap()
        Me.MAPPED_PAGES = 0
        Me.VALID_BLOCKS = 0
        Dim Logical_Page_Pointer As Integer = 0
        For i As Integer = 0 To MAP.Count - 1
            If MAP(i).Status = BLOCK_STATUS.Valid Then
                MAP(i).PageLogical = Logical_Page_Pointer
                Logical_Page_Pointer += Me.PAGE_COUNT
                Me.MAPPED_PAGES += Me.PAGE_COUNT
                Me.VALID_BLOCKS += 1
            Else
                MAP(i).PageLogical = 0
            End If
        Next
    End Sub
    'Returns the physical page address from the logical page address
    Public Function GetPhysical(logical_page_index As Integer) As Integer
        For i As Integer = 0 To MAP.Count - 1
            If (MAP(i).Status = BLOCK_STATUS.Valid) Then
                Dim page_start As Integer = MAP(i).PageLogical
                Dim page_end As Integer = page_start + Me.PAGE_COUNT - 1
                If logical_page_index >= page_start AndAlso logical_page_index <= page_end Then
                    Return MAP(i).PagePhysical + (logical_page_index - page_start)
                End If
            End If
        Next
        Return 0 'NOT FOUND
    End Function

End Class