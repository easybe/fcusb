Imports FlashcatUSB.FlashMemory

'Implements a NAND Flash block manager
Public Class NAND_BLOCK_IF
    Public Event PrintConsole(ByVal msg As String)
    Public Event SetProgress(ByVal percent As Integer)
    Public Event ReadPages(ByVal page_addr As UInt32, ByVal page_offset As UInt16, ByVal count As UInt32, ByVal area As FlashArea, ByRef data() As Byte)
    Public Event WritePages(ByVal page_addr As UInt32, ByVal main() As Byte, ByVal oob() As Byte, ByVal area As FlashArea, ByRef write_result As Boolean)
    Public Event EraseSector(ByVal page_addr As UInt32, ByRef erase_result As Boolean)
    Public Event Ready() 'Checks the RD/BSY pin or register (WAS WaitForReady)
    Public MAP As New List(Of MAPPING)
    Public Property MAPPED_PAGES As UInt32 'Number of pages available in the current map
    Private Property NAND_SIZE As UInt32 'Typical size of the nand (does not include extra area)
    Private Property PAGE_MAIN As UInt32 'Size of the main page
    Private Property PAGE_OOB As UInt32 'Size of the ext page
    Private Property BLOCK_SIZE As UInt32 'size of the block (minus oob)

    Private MEMORY_AREA_ERASED() As Byte 'If CopyExtendedArea is true, this will contain the other area that was erased


    Sub New()

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
        Public BlockIndex As UInt32 'Index of the block
        Public PageAddress As UInt32 'The physical address of the first page of this block
        Public LogicalPage As UInt32 'The mapped address of the first page of this block
    End Class

    'Called once on device init to create a map of all blocks
    Public Sub CreateMap(mem_size As UInt32, main_page As UInt32, oob_page As UInt32, flash_block As UInt32)
        Me.NAND_SIZE = mem_size
        Me.PAGE_MAIN = main_page
        Me.PAGE_OOB = oob_page
        Me.BLOCK_SIZE = flash_block
        MAP.Clear()
        Dim BaseAddress As UInt32 = 0
        Dim BlockCount As UInt32 = NAND_SIZE / BLOCK_SIZE
        Dim PagesPerBlock As UInt32 = BLOCK_SIZE / PAGE_MAIN
        For i As UInt32 = 0 To BlockCount - 1
            Dim block_info As New MAPPING
            block_info.Status = BLOCK_STATUS.Valid
            block_info.BlockIndex = i
            block_info.PageAddress = (PagesPerBlock * i)
            block_info.LogicalPage = (PagesPerBlock * i)
            MAP.Add(block_info)
            BaseAddress += BLOCK_SIZE
        Next
    End Sub

    Public Sub EnableBlockManager()
        If MySettings.NAND_BadBlockManager = FlashcatSettings.BadBlockMode.Disabled Or MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Combined Then
            RaiseEvent PrintConsole(RM.GetString("nand_block_manager_disabled"))
            Exit Sub
        Else
            SetStatus(RM.GetString("nand_mem_device_detected"))
            RaiseEvent PrintConsole(RM.GetString("nand_mem_map_loading"))
        End If
        Dim BlockCount As Integer = NAND_SIZE / BLOCK_SIZE
        Dim PagesPerBlock As UInt32 = BLOCK_SIZE / PAGE_MAIN
        Dim page_addr As UInt32 = 0
        For i As UInt32 = 0 To BlockCount - 1
            Dim LastPageAddr As UInt32 = (page_addr + PagesPerBlock - 1) 'The last page of this block
            Dim page_one() As Byte = Nothing
            Dim page_two() As Byte = Nothing
            Dim page_last() As Byte = Nothing
            Dim valid_block As Boolean = True
            Dim markers As Integer = MySettings.NAND_BadBlockMarkers
            If (markers And FlashcatSettings.BadBlockMarker._1stByte_FirstPage) > 0 Then
                If page_one Is Nothing Then RaiseEvent ReadPages(page_addr, 0, 6, FlashArea.OOB, page_one)
                If Not ((page_one(0)) = 255) Then valid_block = False
            End If
            If (markers And FlashcatSettings.BadBlockMarker._1stByte_SecondPage) > 0 Then
                If page_two Is Nothing Then RaiseEvent ReadPages(page_addr + 1, 0, 6, FlashArea.OOB, page_two)
                If Not ((page_two(0)) = 255) Then valid_block = False
            End If
            If (markers And FlashcatSettings.BadBlockMarker._1stByte_LastPage) > 0 Then
                If page_last Is Nothing Then RaiseEvent ReadPages(LastPageAddr, 0, 6, FlashArea.OOB, page_last)
                If Not ((page_last(0)) = 255) Then valid_block = False
            End If
            If (markers And FlashcatSettings.BadBlockMarker._6thByte_FirstPage) > 0 Then
                If page_one Is Nothing Then RaiseEvent ReadPages(page_addr, 0, 6, FlashArea.OOB, page_one)
                If Not ((page_one(5)) = 255) Then valid_block = False
            End If
            If (markers And FlashcatSettings.BadBlockMarker._6thByte_SecondPage) > 0 Then
                If page_two Is Nothing Then RaiseEvent ReadPages(page_addr + 1, 0, 6, FlashArea.OOB, page_two)
                If Not ((page_two(5)) = 255) Then valid_block = False
            End If
            If (Not valid_block) Then
                Application.DoEvents()
                RaiseEvent PrintConsole(String.Format(RM.GetString("mem_bad_nand_block"), Hex(page_addr).PadLeft(6, "0"), i.ToString))
                MAP(i).Status = BLOCK_STATUS.Bad_Manager
            Else
                MAP(i).Status = BLOCK_STATUS.Valid
            End If
            page_addr += PagesPerBlock
        Next
    End Sub

    Public Sub ProcessMap()
        Me.MAPPED_PAGES = 0
        Dim Logical_Page_Pointer As UInt32 = 0
        Dim PagesPerBlock As UInt32 = BLOCK_SIZE / PAGE_MAIN
        For i As UInt32 = 0 To MAP.Count - 1
            If MAP(i).Status = BLOCK_STATUS.Valid Then
                MAP(i).LogicalPage = Logical_Page_Pointer
                Logical_Page_Pointer += PagesPerBlock
                Me.MAPPED_PAGES += PagesPerBlock
            Else
                MAP(i).LogicalPage = 0
            End If
        Next
        RaiseEvent PrintConsole(String.Format(RM.GetString("nand_mem_map_complete"), Format(MAPPED_PAGES, "#,###")))
    End Sub
    'Returns the physical page address from the logical page address
    Public Function GetPageMapping(ByVal page_index As UInt32) As UInt32
        Dim PagesPerBlock As UInt32 = BLOCK_SIZE / PAGE_MAIN
        For i As UInt32 = 0 To MAP.Count - 1
            If (MAP(i).Status = BLOCK_STATUS.Valid) Then
                Dim page_start As UInt32 = MAP(i).LogicalPage
                Dim page_end As UInt32 = (page_start + PagesPerBlock) - 1
                If page_index >= page_start AndAlso page_index <= page_end Then
                    Return MAP(i).PageAddress + (page_index - page_start)
                End If
            End If
        Next
        Return 0 'NOT FOUND
    End Function
    'Returns the total space of the extra data
    Public Function Extra_GetSize() As UInt32
        Dim PageCount As UInt32 = NAND_SIZE / PAGE_MAIN 'Total number of pages in this device
        Dim PageExtraSize As UInt32 = (PageCount * PAGE_OOB)
        Return PageExtraSize
    End Function

    Public Function ERASEBLOCK(ByVal page_address As UInt32, ByVal Memory_area As FlashArea, ByVal CopyOtherArea As Boolean) As Boolean
        MEMORY_AREA_ERASED = Nothing
        If CopyOtherArea AndAlso Not Memory_area = FlashArea.All Then
            Dim page_count As Integer = (BLOCK_SIZE / PAGE_MAIN) 'number of pages per block
            If Memory_area = FlashArea.Main Then
                RaiseEvent ReadPages(page_address, 0, (page_count * PAGE_OOB), FlashArea.OOB, MEMORY_AREA_ERASED)
            ElseIf Memory_area = FlashArea.OOB Then
                RaiseEvent ReadPages(page_address, 0, (page_count * PAGE_MAIN), FlashArea.Main, MEMORY_AREA_ERASED)
            End If
        End If
        Try 'START BLOCK ERASE
            Dim result As Boolean
            RaiseEvent EraseSector(page_address, result)
            If Not result Then Return False
            If PAGE_MAIN = 512 Then 'LEGACY NAND DEVICE
                Utilities.Sleep(250) 'Micron NAND legacy delay (was 200)
            Else
                Utilities.Sleep(50) 'Normal delay
            End If
            RaiseEvent Ready()
        Catch ex As Exception
        End Try  'BLOCK ERASE COMPLETE
        Return True
    End Function
    'This writes the data to a page
    Public Function WRITEPAGE(ByVal page_address As UInt32, ByVal data_to_write() As Byte, ByVal memory_area As FlashArea) As Boolean
        Try
            Dim main_data() As Byte = Nothing
            Dim oob_data() As Byte = Nothing
            Dim WriteResult As Boolean
            If memory_area = FlashArea.Main Then
                main_data = data_to_write
                oob_data = MEMORY_AREA_ERASED
            ElseIf memory_area = FlashArea.OOB Then
                main_data = MEMORY_AREA_ERASED
                oob_data = data_to_write
            ElseIf memory_area = FlashArea.All Then
                main_data = data_to_write
            End If
            MEMORY_AREA_ERASED = Nothing
            RaiseEvent WritePages(page_address, main_data, oob_data, memory_area, WriteResult)
            If Not WriteResult Then Return False
            If (PAGE_MAIN = 512) Then 'LEGACY NAND DEVICE
                Utilities.Sleep(100)
            Else
                Utilities.Sleep(10)
            End If
            RaiseEvent Ready()
            Return WriteResult
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function READPAGE(ByVal page_addr As UInt32, ByVal page_offset As UInt16, ByVal count As UInt32, ByVal memory_area As FlashArea) As Byte()
        Dim data_out() As Byte = Nothing
        RaiseEvent ReadPages(page_addr, page_offset, count, memory_area, data_out)
        Return data_out
    End Function

    Public Function EraseChip() As Boolean
        Dim PagesPerBlock As UInt32 = BLOCK_SIZE / PAGE_MAIN
        Dim BlockCount As Integer = (NAND_SIZE / BLOCK_SIZE)
        Dim PageAddr As UInt32 = 0
        For i = 0 To BlockCount - 1
            Dim Result As Boolean = ERASEBLOCK(PageAddr, FlashArea.Main, MySettings.NAND_Preserve)
            If Not Result Then Return False
            PageAddr += PagesPerBlock
            If i Mod 10 = 0 Then
                Dim Percent As Integer = Math.Round((i / BlockCount) * 100)
                RaiseEvent SetProgress(Percent)
            End If
        Next
        RaiseEvent SetProgress(0)
        Return True
    End Function

End Class
