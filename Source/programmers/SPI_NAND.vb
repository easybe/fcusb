'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2021 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
'CONTACT EMAIL: support@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'ACKNOWLEDGEMENT: USB driver functionality provided by LibUsbDotNet (sourceforge.net/projects/libusbdotnet) 

Imports FlashcatUSB.ECC_LIB
Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB

Public Class SPINAND_Programmer : Implements MemoryDeviceUSB
    Private FCUSB As FCUSB_DEVICE
    Public Event PrintConsole(message As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress
    Public Property MyFlashDevice As SPI_NAND
    Public Property MyFlashStatus As DeviceStatus = DeviceStatus.NotDetected
    Public Property DIE_SELECTED As Integer = 0
    Public Property MemoryArea As FlashArea = FlashArea.All

    Private Delegate Function USB_Readages(page_addr As Integer, page_offset As UInt16, data_count As Integer, memory_area As FlashArea) As Byte()

    Public WithEvents BlockManager As NAND_BLOCK_IF

    Sub New(parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        Dim rdid(3) As Byte
        Dim sr(0) As Byte
        Dim MFG As Byte
        Dim PART As UInt16
        Dim clk_speed As UInt32 = CUInt(GetMaxSpiClock(FCUSB.HWBOARD, MySettings.SPI_CLOCK_MAX))
        Me.FCUSB.USB_SPI_INIT(CUInt(MySettings.SPI_MODE), clk_speed)
        SPIBUS_WriteRead({SPI_OPCODES.RDID}, rdid) 'NAND devives use 1 dummy byte, then MFG and ID1 (and sometimes, ID2)
        W25M121AV(rdid) 'Check to see if device is W25M121AV
        If (rdid(0) = &HC8 AndAlso rdid(3) = &HC8) Then 'GigaDevice device
            MFG = rdid(0)
            PART = (CUShort(rdid(1)) << 8) + rdid(2)
        Else 'Other Manufacturers use this
            If Not (rdid(0) = 0 Or rdid(0) = 255) Then Return False
            If rdid(1) = 0 OrElse rdid(1) = 255 Then Return False
            If rdid(2) = 0 OrElse rdid(2) = 255 Then Return False
            If rdid(1) = rdid(2) Then Return False
            MFG = rdid(1)
            PART = rdid(2)
            If Not rdid(3) = MFG Then PART = (CUShort(rdid(2)) << 8) + rdid(3)
        End If
        Dim RDID_STR As String = "0x" & MFG.ToString("X").PadLeft(2, "0"c) & PART.ToString("X").PadLeft(4, "0"c)
        RaiseEvent PrintConsole(RM.GetString("spinand_opened_device"))
        RaiseEvent PrintConsole(String.Format(RM.GetString("spinand_connected"), RDID_STR))
        If (PART And &HFF00) = 0 Then PART = (PART << 8) 'ID CODES are LEFT ALIGNED
        MyFlashDevice = CType(FlashDatabase.FindDevice(MFG, PART, 0, MemoryType.SERIAL_NAND), SPI_NAND)
        If MyFlashDevice IsNot Nothing Then
            MyFlashStatus = DeviceStatus.Supported
            RaiseEvent PrintConsole(String.Format(RM.GetString("flash_detected"), MyFlashDevice.NAME, Format(MyFlashDevice.FLASH_SIZE, "#,###")))
            RaiseEvent PrintConsole(String.Format(RM.GetString("ext_page_size"), MyFlashDevice.PAGE_SIZE, MyFlashDevice.PAGE_EXT))
            Me.ECC_ENABLED = Not MySettings.SPI_NAND_DISABLE_ECC
            If MFG = &HEF AndAlso PART = &HAA21 Then 'W25M01GV/W25M121AV
                SPIBUS_WriteRead({OPCMD_GETFEAT, &HB0}, sr)
                SPIBUS_WriteRead({OPCMD_SETFEAT, &HB0, CByte(sr(0) Or 8)}) 'Set bit 3 to ON (BUF=1)
            ElseIf MFG = &HEF AndAlso PART = &HAB21 Then 'W25M02GV
                SPIBUS_WriteRead({OPCMD_GETFEAT, &HB0}, sr)
                SPIBUS_WriteRead({OPCMD_SETFEAT, &HB0, CByte(sr(0) Or 8)}) 'Set bit 3 to ON (BUF=1)
            Else
                SPIBUS_WriteRead({OPCMD_RESET}) 'Dont reset W25M121AV
                Utilities.Sleep(1)
            End If
            If (MyFlashDevice.STACKED_DIES > 1) Then 'Multi-die support
                For i = 0 To MyFlashDevice.STACKED_DIES - 1
                    SPIBUS_WriteRead({OPCMD_DIESELECT, CByte(i)}) : WaitUntilReady()
                    SPIBUS_WriteEnable()
                    SPIBUS_WriteRead({OPCMD_SETFEAT, &HA0, 0}) 'Remove block protection
                Next
                SPIBUS_WriteRead({OPCMD_DIESELECT, 0}) : WaitUntilReady()
                Me.DIE_SELECTED = 0
            Else
                SPIBUS_WriteEnable()
                SPIBUS_WriteRead({OPCMD_SETFEAT, &HA0, 0}) 'Remove block protection
            End If
            BlockManager = New NAND_BLOCK_IF(MyFlashDevice)
            If (MySettings.NAND_BadBlockManager = BadBlockMode.Disabled) Then
                RaiseEvent PrintConsole(RM.GetString("nand_block_manager_disabled"))
            Else
                SetStatus(RM.GetString("nand_mem_device_detected"))
                RaiseEvent PrintConsole(RM.GetString("nand_mem_map_loading"))
                BlockManager.EnableBlockManager(MySettings.NAND_BadBlockMarkers)
                Dim TotalBadBlocks As Integer = (MyFlashDevice.BLOCK_COUNT - BlockManager.VALID_BLOCKS)
                RaiseEvent PrintConsole(String.Format("Total bad blocks: {0}", TotalBadBlocks))
                RaiseEvent PrintConsole(String.Format(RM.GetString("nand_mem_map_complete"), Format(BlockManager.MAPPED_PAGES, "#,###")))
            End If
            ECC_LoadConfiguration(MyFlashDevice.PAGE_SIZE, MyFlashDevice.PAGE_EXT)
            Return True
        Else
            MyFlashStatus = DeviceStatus.NotSupported
            Return False
        End If
    End Function
    'Indicates if the device has its internal ECC engine enabled
    Public Property ECC_ENABLED As Boolean
        Get
            Dim sr(0) As Byte
            SPIBUS_WriteRead({OPCMD_GETFEAT, &HB0}, sr)
            Dim config_reg As Byte = sr(0)
            If (((config_reg >> 4) And 1) = 1) Then Return True
            Return False
        End Get
        Set(value As Boolean)
            Dim sr(0) As Byte
            SPIBUS_WriteRead({OPCMD_GETFEAT, &HB0}, sr)
            Dim config_reg As Byte = sr(0)
            If value Then
                config_reg = CByte(config_reg Or CByte(&H10)) 'Set bit 4 to ON
            Else
                config_reg = CByte(config_reg And CByte(&HEF)) 'Set bit 4 to OFF
            End If
            SPIBUS_WriteEnable()
            SPIBUS_WriteRead({OPCMD_SETFEAT, &HB0, config_reg})
        End Set
    End Property

    Public Sub NAND_ReadPages(page_addr As Integer, page_offset As UInt16, data_count As Integer, memory_area As FlashArea, ByRef data() As Byte) Handles BlockManager.ReadPages
        data = PageRead_Physical(page_addr, page_offset, data_count, memory_area)
    End Sub

    Private Sub W25M121AV(rdid() As Byte)
        If (rdid(0) = &HEF AndAlso rdid(1) = &H40 AndAlso rdid(2) = &H18) Then 'Possibly W25M121AV
            SPIBUS_WriteRead({&HC2, 1}) : WaitUntilReady() 'Switch to W25N01GV die
            SPIBUS_WriteRead({SPI_OPCODES.RDID}, rdid)
            If rdid(1) = &HEF AndAlso rdid(2) = &HAB AndAlso rdid(3) = &H21 Then 'ID changed, W25M121AV confirmed
                rdid(2) = &HAA 'We need to change ID to 1Gbit version
            End If
        End If
    End Sub

#Region "Public Interface"
    Private MEMORY_AREA_ERASED() As Byte 'Preserved memory area

    Public ReadOnly Property GetDevice As Device Implements MemoryDeviceUSB.GetDevice
        Get
            Return Me.MyFlashDevice
        End Get
    End Property

    Public ReadOnly Property DeviceName As String Implements MemoryDeviceUSB.DeviceName
        Get
            Select Case MyFlashStatus
                Case DeviceStatus.Supported
                    Return MyFlashDevice.NAME
                Case DeviceStatus.NotSupported
                    Return (MyFlashDevice.MFG_CODE).ToString("X").PadLeft(2, "0"c) & " " & (MyFlashDevice.ID1).ToString("X").PadLeft(2, "0"c)
                Case Else
                    Return RM.GetString("no_flash_detected")
            End Select
        End Get
    End Property

    Public ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
        Get
            Dim available_pages As Long = CLng(Me.BlockManager.MAPPED_PAGES)
            If Me.MemoryArea = FlashArea.Main Then
                Return (available_pages * MyFlashDevice.PAGE_SIZE)
            ElseIf Me.MemoryArea = FlashArea.OOB Then
                Return (available_pages * MyFlashDevice.PAGE_EXT)
            ElseIf Me.MemoryArea = FlashArea.All Then
                Return (available_pages * (MyFlashDevice.PAGE_SIZE + MyFlashDevice.PAGE_EXT))
            Else
                Return 0
            End If
        End Get
    End Property

    Public Function ReadData(logical_address As Long, data_count As Integer) As Byte() Implements MemoryDeviceUSB.ReadData
        Dim page_addr As Integer 'This is the page address
        Dim page_offset As UInt16 'this is the start offset within the page
        Dim page_size As UInt16
        If (Me.MemoryArea = FlashArea.Main) Then
            page_size = MyFlashDevice.PAGE_SIZE
            page_addr = CInt(logical_address \ MyFlashDevice.PAGE_SIZE)
            page_offset = CUShort(logical_address - (page_addr * MyFlashDevice.PAGE_SIZE))
        ElseIf (Me.MemoryArea = FlashArea.OOB) Then
            page_size = MyFlashDevice.PAGE_EXT
            page_addr = CInt(logical_address \ MyFlashDevice.PAGE_EXT)
            page_offset = CUShort(logical_address - (page_addr * MyFlashDevice.PAGE_EXT))
        ElseIf (Me.MemoryArea = FlashArea.All) Then   'we need to adjust large address to logical address
            page_size = MyFlashDevice.PAGE_SIZE + MyFlashDevice.PAGE_EXT
            Dim full_page_size As Integer = (MyFlashDevice.PAGE_SIZE + MyFlashDevice.PAGE_EXT)
            page_addr = CInt(logical_address \ full_page_size)
            page_offset = CUShort(logical_address - (page_addr * full_page_size))
        End If
        Dim pages_per_block As UInt16 = MyFlashDevice.PAGE_COUNT
        Dim data_out(data_count - 1) As Byte
        Dim data_ptr As Integer = 0
        Do While (data_count > 0)
            Dim pages_left As Integer = (pages_per_block - (page_addr Mod pages_per_block))
            Dim bytes_left_in_block As Integer = (pages_left * page_size) - page_offset
            Dim packet_size As Integer = Math.Min(bytes_left_in_block, data_count)
            Dim page_addr_phy As Integer = Me.BlockManager.GetPhysical(page_addr)
            Dim data() As Byte = ReadBulk(page_addr, page_offset, packet_size, Me.MemoryArea)
            If data Is Nothing Then Return Nothing
            Array.Copy(data, 0, data_out, data_ptr, data.Length)
            data_ptr += packet_size
            data_count -= packet_size
            page_addr += CInt(Math.Ceiling(bytes_left_in_block / page_size))
            page_offset = 0
        Loop
        Return data_out
    End Function

    Public Function SectorErase(sector_index As Integer) As Boolean Implements MemoryDeviceUSB.SectorErase
        If Not MyFlashDevice.ERASE_REQUIRED Then Return True
        Dim page_logical As Integer = CInt(MyFlashDevice.PAGE_COUNT) * sector_index
        Dim page_addr_phy As Integer = Me.BlockManager.GetPhysical(page_logical)
        MEMORY_AREA_ERASED = Nothing
        If MySettings.NAND_Preserve AndAlso Not Me.MemoryArea = FlashArea.All Then
            If Me.MemoryArea = FlashArea.Main Then
                Dim mem_size As Integer = CInt(MyFlashDevice.PAGE_COUNT) * MyFlashDevice.PAGE_EXT
                MEMORY_AREA_ERASED = PageRead_Physical(page_addr_phy, 0, mem_size, FlashArea.OOB)
            ElseIf Me.MemoryArea = FlashArea.OOB Then
                Dim mem_size As Integer = CInt(MyFlashDevice.PAGE_COUNT) * MyFlashDevice.PAGE_SIZE
                MEMORY_AREA_ERASED = PageRead_Physical(page_addr_phy, 0, mem_size, FlashArea.Main)
            End If
        End If
        If Not SectorErase_Physical(page_addr_phy) Then Return False
        WaitUntilReady()
        Return True
    End Function

    Public Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
        Try
            Dim sr() As Byte
            Do
                sr = ReadStatusRegister()
                If AppIsClosing Then Exit Sub
                If sr(0) = 255 Then Exit Do
            Loop While ((sr(0) And 1) = 1)
        Catch ex As Exception
        End Try
    End Sub

    Public Function SectorFind(sector_index As Integer) As Long Implements MemoryDeviceUSB.SectorFind
        Dim base_addr As Long = 0
        If sector_index > 0 Then
            For i As Integer = 0 To sector_index - 1
                base_addr += CLng(Me.SectorSize(i))
            Next
        End If
        Return base_addr
    End Function

    Public Function SectorWrite(sector_index As Integer, data() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.SectorWrite
        Dim logical_address As Long = Me.SectorFind(sector_index)
        Return WriteData(logical_address, data, Params)
    End Function

    Public Function WriteData(logical_address As Long, data_to_write() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        Dim page_addr As Integer = NAND_LayoutTool.GetNandPageAddress(MyFlashDevice, logical_address, Me.MemoryArea)
        page_addr = Me.BlockManager.GetPhysical(page_addr) 'Adjusts the page to point to a valid page
        Dim result As Boolean = WritePage_Physical(page_addr, data_to_write, Me.MemoryArea)
        FCUSB.USB_WaitForComplete()
        Return result
    End Function

    Public Function SectorCount() As Integer Implements MemoryDeviceUSB.SectorCount
        Return MyFlashDevice.BLOCK_COUNT
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        Dim logical_index As Integer = 0
        For i As Integer = 0 To Me.BlockManager.VALID_BLOCKS - 1
            If (Not SectorErase(logical_index)) Then
                RaiseEvent PrintConsole(RM.GetString("nand_erase_failed"))
                Return False
            End If
            If (i Mod 10 = 0) Then 'Every 10 blocks, update progress bar
                Dim Percent As Integer = CInt(Math.Round((i / MyFlashDevice.BLOCK_COUNT) * 100))
                RaiseEvent SetProgress(Percent)
            End If
            logical_index += Me.MyFlashDevice.PAGE_COUNT
        Next
        RaiseEvent SetProgress(0)
        RaiseEvent PrintConsole(RM.GetString("nand_erase_successful"))
        Return True
    End Function

    Public Function SectorSize(sector As Integer) As Integer Implements MemoryDeviceUSB.SectorSize
        If Not MyFlashStatus = USB.DeviceStatus.Supported Then Return 0
        Select Case Me.MemoryArea
            Case FlashArea.Main
                Return (CInt(MyFlashDevice.PAGE_COUNT) * MyFlashDevice.PAGE_SIZE)
            Case FlashArea.OOB
                Return (CInt(MyFlashDevice.PAGE_COUNT) * MyFlashDevice.PAGE_EXT)
            Case FlashArea.All
                Return (CInt(MyFlashDevice.PAGE_COUNT) * (MyFlashDevice.PAGE_SIZE + MyFlashDevice.PAGE_EXT))
        End Select
        Return 0
    End Function

#End Region

#Region "SPIBUS"
    Private Const OPCMD_WREN As Byte = &H6
    Private Const OPCMD_GETFEAT As Byte = &HF
    Private Const OPCMD_SETFEAT As Byte = &H1F
    Private Const OPCMD_SR As Byte = &HC0
    Private Const OPCMD_PAGEREAD As Byte = &H13
    Private Const OPCMD_PAGELOAD As Byte = &H2 'Program Load / &H84
    Private Const OPCMD_READCACHE As Byte = &HB
    Private Const OPCMD_PROGCACHE As Byte = &H10 'Program execute
    Private Const OPCMD_BLOCKERASE As Byte = &HD8
    Private Const OPCMD_DIESELECT As Byte = &HC2
    Private Const OPCMD_RESET As Byte = &HFF

    Public Function SPIBUS_WriteRead(WriteBuffer() As Byte, Optional ByRef ReadBuffer() As Byte = Nothing) As UInt32
        If WriteBuffer Is Nothing And ReadBuffer Is Nothing Then Return 0
        Dim TotalBytesTransfered As UInt32 = 0
        SPIBUS_SlaveSelect_Enable()
        If (WriteBuffer IsNot Nothing) Then
            Dim BytesWritten As Integer = 0
            Dim Result As Boolean = SPIBUS_WriteData(WriteBuffer)
            If WriteBuffer.Length > 2048 Then Utilities.Sleep(2)
            If Result Then TotalBytesTransfered += CUInt(WriteBuffer.Length)
        End If
        If (ReadBuffer IsNot Nothing) Then
            Dim BytesRead As Integer = 0
            Dim Result As Boolean = SPIBUS_ReadData(ReadBuffer)
            If Result Then TotalBytesTransfered += CUInt(ReadBuffer.Length)
        End If
        SPIBUS_SlaveSelect_Disable()
        Return TotalBytesTransfered
    End Function
    'Makes the CS/SS pin go low
    Private Sub SPIBUS_SlaveSelect_Enable()
        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.SPI_SS_ENABLE)
    End Sub
    'Releases the CS/SS pin
    Private Sub SPIBUS_SlaveSelect_Disable()
        Try
            FCUSB.USB_CONTROL_MSG_OUT(USBREQ.SPI_SS_DISABLE)
        Catch ex As Exception
        End Try
    End Sub

    Private Function SPIBUS_WriteData(DataOut() As Byte) As Boolean
        Dim Success As Boolean = False
        Try
            Success = FCUSB.USB_SETUP_BULKOUT(USBREQ.SPI_WR_DATA, Nothing, DataOut, CUInt(DataOut.Length))
        Catch ex As Exception
            Return False
        End Try
        If Not Success Then RaiseEvent PrintConsole(RM.GetString("spi_error_writing"))
        Return True
    End Function

    Private Function SPIBUS_ReadData(ByRef Data_In() As Byte) As Boolean
        Dim Success As Boolean = False
        Try
            Success = FCUSB.USB_SETUP_BULKIN(USBREQ.SPI_RD_DATA, Nothing, Data_In, CUInt(Data_In.Length))
        Catch ex As Exception
            Success = False
        End Try
        If Not Success Then RaiseEvent PrintConsole(RM.GetString("spi_error_reading"))
        Return Success
    End Function

    Private Function SPIBUS_WriteEnable() As Boolean
        If SPIBUS_WriteRead({OPCMD_WREN}, Nothing) = 1 Then
            Return True
        Else
            Return False
        End If
    End Function

#End Region

    Public Function WritePage_Physical(phy_page_index As Integer, data_to_write() As Byte, memory_area As FlashArea) As Boolean
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
        WriteResult = WriteBulk(phy_page_index, main_data, oob_data, memory_area)
        If Not WriteResult Then Return False
        Utilities.Sleep(10)
        WaitUntilReady()
        Return WriteResult
    End Function

    Public Function SectorErase_Physical(phy_page_index As Integer) As Boolean
        If (MyFlashDevice.STACKED_DIES > 1) Then 'Multi-die support
            phy_page_index = GetPageForMultiDie(phy_page_index, 0, 0, 0, FlashArea.Main)
        End If
        Dim block(2) As Byte
        block(2) = CByte((phy_page_index >> 16) And 255)
        block(1) = CByte((phy_page_index >> 8) And 255)
        block(0) = CByte(phy_page_index And 255)
        SPIBUS_WriteEnable()
        SPIBUS_WriteRead({OPCMD_BLOCKERASE, block(2), block(1), block(0)})
        WaitUntilReady()
        Return True
    End Function

    Public Function PageRead_Physical(phy_page_index As Integer, page_offset As UInt16, count As Integer, memory_area As FlashArea) As Byte()
        Return ReadBulk(phy_page_index, page_offset, count, memory_area)
    End Function

    Private Function ReadBulk(page_addr As Integer, page_offset As UInt16, data_count As Integer, memory_area As FlashArea) As Byte()
        If (MyFlashDevice.STACKED_DIES > 1) Then
            Dim data_to_read(data_count - 1) As Byte
            Dim array_ptr As Integer = 0
            Dim bytes_left As Integer = data_count
            Do Until bytes_left = 0
                Dim buffer_size As Integer = 0
                Dim die_page_addr As Integer = GetPageForMultiDie(page_addr, page_offset, bytes_left, buffer_size, memory_area)
                Dim die_data() As Byte = ReadPages(die_page_addr, page_offset, buffer_size, memory_area)
                Array.Copy(die_data, 0, data_to_read, array_ptr, die_data.Length) : array_ptr += buffer_size
            Loop
            Return data_to_read
        Else
            Return ReadPages(page_addr, page_offset, data_count, memory_area)
        End If
    End Function

    Private Function WriteBulk(page_addr As Integer, main_data() As Byte, oob_data() As Byte, memory_area As FlashArea) As Boolean
        Try
            If main_data Is Nothing And oob_data Is Nothing Then Return False
            Dim NAND_DEV As SPI_NAND = DirectCast(MyFlashDevice, SPI_NAND)
            Dim page_size_tot As UInt16 = (MyFlashDevice.PAGE_SIZE + NAND_DEV.PAGE_EXT)
            Dim page_aligned() As Byte = Nothing
            If memory_area = FlashArea.All Then 'Ignore OOB/SPARE
                oob_data = Nothing
                Dim total_pages As Integer = CInt(Math.Ceiling(main_data.Length / page_size_tot))
                ReDim page_aligned((total_pages * page_size_tot) - 1)
                For i = 0 To page_aligned.Length - 1 : page_aligned(i) = 255 : Next
                Array.Copy(main_data, 0, page_aligned, 0, main_data.Length)
            ElseIf memory_area = FlashArea.Main Then
                If NAND_ECC IsNot Nothing Then
                    If oob_data Is Nothing Then
                        Dim o_size As Integer = ((main_data.Length \ NAND_DEV.PAGE_SIZE) * NAND_DEV.PAGE_EXT)
                        ReDim oob_data(o_size - 1)
                        Utilities.FillByteArray(oob_data, 255)
                    End If
                    Dim ecc_data() As Byte = Nothing
                    NAND_ECC.WriteData(main_data, ecc_data)
                    NAND_ECC.SetEccToSpare(oob_data, ecc_data, NAND_DEV.PAGE_SIZE, NAND_DEV.PAGE_EXT)
                End If
                page_aligned = NAND_LayoutTool.CreatePageAligned(MyFlashDevice, main_data, oob_data)
            ElseIf memory_area = FlashArea.OOB Then
                page_aligned = NAND_LayoutTool.CreatePageAligned(MyFlashDevice, main_data, oob_data)
            End If
            Return USB_WritePageAlignedData(page_addr, page_aligned)
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Function ReadStatusRegister() As Byte()
        Try
            Dim sr(0) As Byte
            SPIBUS_WriteRead({OPCMD_GETFEAT, OPCMD_SR}, sr)
            Return sr
        Catch ex As Exception
            Return Nothing 'Erorr
        End Try
    End Function

    Public Function ReadPages(page_addr As Integer, page_offset As UInt16, count As Integer, memory_area As FlashArea) As Byte()
        Dim read_fnc As USB_Readages
        Try
            If (FCUSB.HasLogic()) Then 'Hardware-enabled routine
                read_fnc = New USB_Readages(AddressOf ReadPages_Hardware)
            Else 'Software mode only
                read_fnc = New USB_Readages(AddressOf ReadPages_Software)
            End If
            If NAND_ECC IsNot Nothing AndAlso (memory_area = FlashArea.Main) Then
                Dim NAND_DEV As SPI_NAND = DirectCast(MyFlashDevice, SPI_NAND)
                Dim page_count As Integer = CInt(Math.Ceiling((count + page_offset) / NAND_DEV.PAGE_SIZE)) 'Number of complete pages and OOB to read and correct
                Dim total_main_bytes As Integer = (page_count * NAND_DEV.PAGE_SIZE)
                Dim total_oob_bytes As Integer = (page_count * NAND_DEV.PAGE_EXT)
                Dim main_area_data() As Byte = read_fnc(page_addr, 0, total_main_bytes, FlashArea.Main)
                Dim oob_area_data() As Byte = read_fnc(page_addr, 0, total_oob_bytes, FlashArea.OOB)
                Dim ecc_data() As Byte = NAND_ECC.GetEccFromSpare(oob_area_data, NAND_DEV.PAGE_SIZE, NAND_DEV.PAGE_EXT) 'This strips out the ecc data from the spare area
                ECC_LAST_RESULT = NAND_ECC.ReadData(main_area_data, ecc_data) 'This processes the flash data (512 bytes at a time) and corrects for any errors using the ECC
                If ECC_LAST_RESULT = ECC_DECODE_RESULT.Uncorractable Then
                    Dim logical_addr As Long = (page_addr * CLng(MyFlashDevice.PAGE_SIZE)) + page_offset
                    RaiseEvent PrintConsole("ECC failed at: 0x" & Hex(logical_addr).PadLeft(8, "0"c))
                End If
                Dim data_out(count - 1) As Byte 'This is the data the user requested
                Array.Copy(main_area_data, page_offset, data_out, 0, data_out.Length)
                Return data_out
            Else
                Return read_fnc(page_addr, page_offset, count, memory_area)
            End If
        Catch ex As Exception
        End Try
        Return Nothing
    End Function

    Private Function ReadPages_Hardware(page_addr As Integer, page_offset As UInt16, data_count As Integer, memory_area As FlashArea) As Byte()
        Dim setup() As Byte = SetupPacket_NAND(page_addr, page_offset, data_count, memory_area)
        Dim param As UInt32 = 0
        If MyFlashDevice.PLANE_SELECT Then param = 1
        If MyFlashDevice.READ_CMD_DUMMY Then param = (param Or (1UI << 1))
        Dim data_out(data_count - 1) As Byte
        If Not FCUSB.USB_SETUP_BULKIN(USBREQ.SPINAND_READFLASH, setup, data_out, param) Then Return Nothing
        Return data_out
    End Function
    'SPI NAND software mode
    Private Function ReadPages_Software(page_addr As Integer, page_offset As UInt16, count As Integer, memory_area As FlashArea) As Byte()
        Dim data_out(count - 1) As Byte
        Dim bytes_left As Integer = count
        Dim page_size_tot As UInt16 = (MyFlashDevice.PAGE_SIZE + MyFlashDevice.PAGE_EXT)
        Dim nand_layout As NAND_LAYOUT_TOOL.NANDLAYOUT_STRUCTURE = NAND_LayoutTool.GetStructure(MyFlashDevice)
        Dim data_ptr As UInt32 = 0
        Dim logical_block As UInt16 = (nand_layout.Layout_Main + nand_layout.Layout_Spare)
        Do While (bytes_left > 0)
            Dim read_offset As UInt16 = 0 'We always want to read the entire page
            If MyFlashDevice.PLANE_SELECT Then read_offset = (read_offset Or &H1000US) 'Sets plane select to HIGH
            Dim op_setup() As Byte = GetReadCacheCommand(read_offset)
            Dim read_packet(page_size_tot - 1) As Byte
            SPIBUS_WriteRead({OPCMD_PAGEREAD, CByte((page_addr >> 16) And 255), CByte((page_addr >> 8) And 255), CByte(page_addr And 255)})
            SPIBUS_WriteRead(op_setup, read_packet)
            Select Case memory_area
                Case FlashArea.Main
                    Dim sub_index As UInt16 = (page_offset \ nand_layout.Layout_Main) 'the sub block we are in			
                    Dim adj_offset As UInt16 = (sub_index * logical_block) + (page_offset Mod nand_layout.Layout_Main)
                    sub_index += 1US
                    Do While Not (adj_offset = page_size_tot)
                        Dim sub_left As UInt16 = nand_layout.Layout_Main - (page_offset Mod nand_layout.Layout_Main)
                        If sub_left > bytes_left Then sub_left = CUShort(bytes_left)
                        Array.Copy(read_packet, adj_offset, data_out, data_ptr, sub_left)
                        data_ptr += sub_left
                        page_offset += sub_left
                        bytes_left -= sub_left
                        If (bytes_left = 0) Then Exit Do
                        adj_offset = (sub_index * logical_block)
                        sub_index += 1US
                    Loop
                    If (page_offset = MyFlashDevice.PAGE_SIZE) Then
                        page_offset = 0
                        page_addr += 1
                    End If
                Case FlashArea.OOB
                    Dim sub_index As UInt16 = (page_offset \ nand_layout.Layout_Spare) + 1US 'the sub block we are in			
                    Dim adj_offset As UInt16 = ((sub_index * logical_block) - nand_layout.Layout_Spare) + (page_offset Mod nand_layout.Layout_Spare)
                    sub_index += 1US
                    Do While Not ((adj_offset - nand_layout.Layout_Main) = page_size_tot)
                        Dim sub_left As UInt16 = nand_layout.Layout_Spare - (page_offset Mod nand_layout.Layout_Spare)
                        If sub_left > bytes_left Then sub_left = CUShort(bytes_left)
                        Array.Copy(read_packet, adj_offset, data_out, data_ptr, sub_left)
                        data_ptr += sub_left
                        page_offset += sub_left
                        bytes_left -= sub_left
                        If (bytes_left = 0) Then Exit Do
                        adj_offset = ((sub_index * logical_block) - nand_layout.Layout_Spare)
                        sub_index += 1US
                    Loop
                    If (page_offset = MyFlashDevice.PAGE_EXT) Then
                        page_offset = 0
                        page_addr += 1
                    End If
                Case FlashArea.All
                    Dim sub_left As UInt16 = page_size_tot - page_offset
                    Dim transfer_count As UInt16 = sub_left
                    If (transfer_count > bytes_left) Then transfer_count = CUShort(bytes_left)
                    Array.Copy(read_packet, page_offset, data_out, data_ptr, transfer_count)
                    data_ptr += transfer_count
                    bytes_left -= transfer_count
                    sub_left -= transfer_count
                    If (sub_left = 0) Then
                        page_addr += 1
                        page_offset = 0
                    Else
                        page_offset = (page_size_tot - sub_left)
                    End If
            End Select
        Loop
        Return data_out
    End Function

    Private Function GetReadCacheCommand(read_offset As UInt16) As Byte()
        Dim rd_cmd() As Byte
        If MyFlashDevice.READ_CMD_DUMMY Then
            ReDim rd_cmd(4)
            rd_cmd(0) = OPCMD_READCACHE
            rd_cmd(2) = CByte((read_offset >> 8) And 255) 'Column address (upper)
            rd_cmd(3) = CByte(read_offset And 255) 'Lower
        Else
            ReDim rd_cmd(3)
            rd_cmd(0) = OPCMD_READCACHE
            rd_cmd(1) = CByte((read_offset >> 8) And 255) 'Column address (upper)
            rd_cmd(2) = CByte(read_offset And 255) 'Lower
        End If
        Return rd_cmd
    End Function

    Private Function USB_WritePageAlignedData(ByRef page_addr As Integer, page_aligned() As Byte) As Boolean
        Dim page_size_tot As UInt16 = (MyFlashDevice.PAGE_SIZE + MyFlashDevice.PAGE_EXT)
        Dim pages_to_write As Integer = (page_aligned.Length \ page_size_tot)
        If (FCUSB.HasLogic()) Then 'Hardware-enabled routine
            Dim setup() As Byte = SetupPacket_NAND(page_addr, 0, page_aligned.Length, FlashArea.All) 'We will write the entire page
            Dim param As Integer = Utilities.BoolToInt(MyFlashDevice.PLANE_SELECT)
            Dim result As Boolean = FCUSB.USB_SETUP_BULKOUT(USBREQ.SPINAND_WRITEFLASH, setup, page_aligned, CUInt(param))
            If Not result Then Return False
            FCUSB.USB_WaitForComplete()
        Else
            For i = 0 To pages_to_write - 1
                Dim cache_data(page_size_tot - 1) As Byte
                Array.Copy(page_aligned, (i * page_size_tot), cache_data, 0, cache_data.Length)
                SPIBUS_WriteEnable()
                Dim cache_offset As UInt16 = 0
                If MyFlashDevice.PLANE_SELECT Then
                    If (((page_addr \ 64) And 1) = 1) Then
                        cache_offset = (cache_offset Or &H1000US) 'Sets plane select to HIGH
                    End If
                End If
                LoadPageCache(0, cache_data)
                ProgramPageCache(page_addr)
                page_addr += 1
                WaitUntilReady()
            Next
        End If
        Return True
    End Function

    Private Sub LoadPageCache(page_offset As UInt16, data_to_load() As Byte)
        Dim setup_packet(data_to_load.Length + 2) As Byte
        setup_packet(0) = OPCMD_PAGELOAD
        setup_packet(1) = CByte((page_offset >> 8) And 255) 'Column address (upper)
        setup_packet(2) = CByte(page_offset And 255) 'Lower
        Array.Copy(data_to_load, 0, setup_packet, 3, data_to_load.Length)
        SPIBUS_WriteRead(setup_packet)
    End Sub

    Private Sub ProgramPageCache(page_addr As Integer)
        Dim exe_packet(3) As Byte
        exe_packet(0) = OPCMD_PROGCACHE
        exe_packet(1) = CByte((page_addr >> 16) And 255)
        exe_packet(2) = CByte((page_addr >> 8) And 255)
        exe_packet(3) = CByte(page_addr And 255)
        SPIBUS_WriteRead(exe_packet)
    End Sub

    Private Function SetupPacket_NAND(page_addr As Integer, page_offset As UInt16, Count As Integer, area As FlashArea) As Byte()
        Dim nand_layout As NAND_LAYOUT_TOOL.NANDLAYOUT_STRUCTURE = NAND_LayoutTool.GetStructure(MyFlashDevice)
        Dim spare_size As UInt16 = MyFlashDevice.PAGE_EXT
        Dim setup_data(19) As Byte
        setup_data(0) = CByte(page_addr And 255)
        setup_data(1) = CByte((page_addr >> 8) And 255)
        setup_data(2) = CByte((page_addr >> 16) And 255)
        setup_data(3) = CByte((page_addr >> 24) And 255)
        setup_data(4) = CByte(Count And 255)
        setup_data(5) = CByte((Count >> 8) And 255)
        setup_data(6) = CByte((Count >> 16) And 255)
        setup_data(7) = CByte((Count >> 24) And 255)
        setup_data(8) = CByte(page_offset And 255)
        setup_data(9) = CByte((page_offset >> 8) And 255)
        setup_data(10) = CByte(MyFlashDevice.PAGE_SIZE And 255)
        setup_data(11) = CByte((MyFlashDevice.PAGE_SIZE >> 8) And 255)
        setup_data(12) = CByte(spare_size And 255)
        setup_data(13) = CByte((spare_size >> 8) And 255)
        setup_data(14) = CByte(nand_layout.Layout_Main And 255)
        setup_data(15) = CByte((nand_layout.Layout_Main >> 8) And 255)
        setup_data(16) = CByte(nand_layout.Layout_Spare And 255)
        setup_data(17) = CByte((nand_layout.Layout_Spare >> 8) And 255)
        setup_data(18) = 0 '(Addresssize) Not needed for SPI-NAND
        setup_data(19) = area 'Area (0=main,1=spare,2=all), note: all ignores layout settings
        Return setup_data
    End Function

    Private Function GetPageForMultiDie(ByRef page_addr As Integer, page_offset As UInt16, ByRef count As Integer, ByRef buffer_size As Integer, area As FlashArea) As Integer
        Dim total_pages As Integer = CInt(MyFlashDevice.FLASH_SIZE \ MyFlashDevice.PAGE_SIZE)
        Dim pages_per_die As Integer = total_pages \ MyFlashDevice.STACKED_DIES
        Dim die_id As Byte = CByte((page_addr \ pages_per_die) And 255)
        If (die_id <> Me.DIE_SELECTED) Then
            SPIBUS_WriteRead({OPCMD_DIESELECT, die_id})
            WaitUntilReady()
            Me.DIE_SELECTED = die_id
        End If
        Dim pages_left As Integer = (pages_per_die - (page_addr Mod pages_per_die))
        Dim bytes_left As Integer 'Total number of bytes left in this die (for the selected area)
        Dim page_area_size As UInt16 'Number of bytes we are accessing
        Select Case area
            Case FlashArea.Main
                page_area_size = MyFlashDevice.PAGE_SIZE
            Case FlashArea.OOB
                page_area_size = MyFlashDevice.PAGE_EXT
            Case FlashArea.All
                page_area_size = MyFlashDevice.PAGE_SIZE + MyFlashDevice.PAGE_EXT
        End Select
        bytes_left = (page_area_size - page_offset) + (page_area_size * (pages_left - 1))
        buffer_size = Math.Min(count, bytes_left)
        count -= buffer_size
        Return (page_addr Mod pages_per_die)
    End Function

End Class
