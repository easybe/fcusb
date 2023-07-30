'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2020 - ALL RIGHTS RESERVED
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


    Private Delegate Function USB_Readages(page_addr As UInt32, page_offset As UInt16, data_count As UInt32, memory_area As FlashArea) As Byte()

    Sub New(parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        Dim rdid(3) As Byte
        Dim sr(0) As Byte
        Dim MFG As Byte
        Dim PART As UInt16
        If FCUSB.SPI_NOR_IF.W25M121AV_Mode Then
            SPIBUS_WriteRead({&HC2, 1}) : WaitUntilReady()
            MFG = &HEF
            PART = &HAA21 'We need to override AB21 to indicate 1Gbit NAND die
        Else
            Dim clk_speed As Integer = GetMaxSpiClock(FCUSB.HWBOARD, MySettings.SPI_CLOCK_MAX)
            Me.FCUSB.USB_SPI_INIT(MySettings.SPI_MODE, clk_speed)
            SPIBUS_WriteRead({SPI_CmdDef.RDID}, rdid) 'NAND devives use 1 dummy byte, then MFG and ID1 (and sometimes, ID2)
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
        End If
        Dim RDID_Str As String = "0x" & Hex(MFG).PadLeft(2, "0") & Hex(PART).PadLeft(4, "0")
        RaiseEvent PrintConsole(RM.GetString("spinand_opened_device"))
        RaiseEvent PrintConsole(String.Format(RM.GetString("spinand_connected"), RDID_Str))
        MyFlashDevice = FlashDatabase.FindDevice(MFG, PART, 0, MemoryType.SERIAL_NAND)
        If MyFlashDevice IsNot Nothing Then
            MyFlashStatus = DeviceStatus.Supported
            RaiseEvent PrintConsole(String.Format(RM.GetString("flash_detected"), MyFlashDevice.NAME, MyFlashDevice.FLASH_SIZE))
            RaiseEvent PrintConsole(String.Format(RM.GetString("ext_page_size"), MyFlashDevice.PAGE_SIZE, MyFlashDevice.PAGE_EXT))
            NANDHELPER_SetupHandlers()
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
            FCUSB.NAND_IF.CreateMap(MyFlashDevice.FLASH_SIZE, MyFlashDevice.PAGE_SIZE, MyFlashDevice.PAGE_EXT, MyFlashDevice.PAGE_COUNT, MyFlashDevice.BLOCK_COUNT)
            FCUSB.NAND_IF.EnableBlockManager() 'If enabled
            FCUSB.NAND_IF.ProcessMap()
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
            If ((config_reg >> 4) And 1 = 1) Then Return True
            Return False
        End Get
        Set(value As Boolean)
            Dim sr(0) As Byte
            SPIBUS_WriteRead({OPCMD_GETFEAT, &HB0}, sr)
            Dim config_reg As Byte = sr(0)
            If value Then
                config_reg = config_reg Or &H10 'Set bit 4 to ON
            Else
                config_reg = config_reg And &HEF 'Set bit 4 to OFF
            End If
            SPIBUS_WriteEnable()
            SPIBUS_WriteRead({OPCMD_SETFEAT, &HB0, config_reg})
        End Set
    End Property

#Region "Public Interface"

    Public ReadOnly Property GetDevice As Device Implements MemoryDeviceUSB.GetDevice
        Get
            Return Me.MyFlashDevice
        End Get
    End Property

    Friend ReadOnly Property DeviceName As String Implements MemoryDeviceUSB.DeviceName
        Get
            Select Case MyFlashStatus
                Case DeviceStatus.Supported
                    Return MyFlashDevice.NAME
                Case DeviceStatus.NotSupported
                    Return Hex(MyFlashDevice.MFG_CODE).PadLeft(2, CChar("0")) & " " & Hex(MyFlashDevice.ID1).PadLeft(2, CChar("0"))
                Case Else
                    Return RM.GetString("no_flash_detected")
            End Select
        End Get
    End Property

    Friend ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
        Get
            Dim available_pages As Long = FCUSB.NAND_IF.MAPPED_PAGES
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

    Friend Function SectorSize(sector As UInt32) As UInt32 Implements MemoryDeviceUSB.SectorSize
        If Not MyFlashStatus = USB.DeviceStatus.Supported Then Return 0
        Select Case Me.MemoryArea
            Case FlashArea.Main
                Return (MyFlashDevice.PAGE_COUNT * MyFlashDevice.PAGE_SIZE)
            Case FlashArea.OOB
                Return (MyFlashDevice.PAGE_COUNT * MyFlashDevice.PAGE_EXT)
            Case FlashArea.All
                Return (MyFlashDevice.PAGE_COUNT * (MyFlashDevice.PAGE_SIZE + MyFlashDevice.PAGE_EXT))
        End Select
        Return 0
    End Function

    Friend Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
        Try
            Dim sr() As Byte
            Do
                sr = ReadStatusRegister()
                If AppIsClosing Then Exit Sub
                If sr(0) = 255 Then Exit Do
            Loop While (sr(0) And 1)
        Catch ex As Exception
        End Try
    End Sub

    Friend Function ReadData(logical_address As Long, data_count As Long) As Byte() Implements MemoryDeviceUSB.ReadData
        If FCUSB.SPI_NOR_IF.W25M121AV_Mode Then SPIBUS_WriteRead({&HC2, 1}) : WaitUntilReady()
        Dim page_addr As UInt32 'This is the page address
        Dim page_offset As UInt16 'this is the start offset within the page
        Dim page_size As UInt16
        If (Me.MemoryArea = FlashArea.Main) Then
            page_size = MyFlashDevice.PAGE_SIZE
            page_addr = Math.Floor(logical_address / MyFlashDevice.PAGE_SIZE)
            page_offset = logical_address - (page_addr * MyFlashDevice.PAGE_SIZE)
        ElseIf (Me.MemoryArea = FlashArea.OOB) Then
            page_size = MyFlashDevice.PAGE_EXT
            page_addr = Math.Floor(logical_address / MyFlashDevice.PAGE_EXT)
            page_offset = logical_address - (page_addr * MyFlashDevice.PAGE_EXT)
        ElseIf (Me.MemoryArea = FlashArea.All) Then   'we need to adjust large address to logical address
            page_size = MyFlashDevice.PAGE_SIZE + MyFlashDevice.PAGE_EXT
            Dim full_page_size As UInt32 = (MyFlashDevice.PAGE_SIZE + MyFlashDevice.PAGE_EXT)
            page_addr = Math.Floor(logical_address / full_page_size)
            page_offset = logical_address - (page_addr * full_page_size)
        End If
        Dim pages_per_block As UInt32 = (MyFlashDevice.Block_Size / MyFlashDevice.PAGE_SIZE)
        Dim data_out(data_count - 1) As Byte
        Dim data_ptr As Integer = 0
        Do While (data_count > 0)
            Dim pages_left As UInt32 = (pages_per_block - (page_addr Mod pages_per_block))
            Dim bytes_left_in_block As UInt32 = (pages_left * page_size) - page_offset
            Dim packet_size As UInt32 = Math.Min(bytes_left_in_block, data_count)
            page_addr = FCUSB.NAND_IF.GetPageMapping(page_addr)
            Dim data() As Byte = ReadBulk_NAND(page_addr, page_offset, packet_size, Me.MemoryArea)
            If data Is Nothing Then Return Nothing
            Array.Copy(data, 0, data_out, data_ptr, data.Length)
            data_ptr += packet_size
            data_count -= packet_size
            page_addr += Math.Ceiling(bytes_left_in_block / page_size)
            page_offset = 0
        Loop
        Return data_out
    End Function

    Private Function ReadBulk_NAND(page_addr As UInt32, page_offset As UInt16, data_count As UInt32, memory_area As FlashArea) As Byte()
        If (MyFlashDevice.STACKED_DIES > 1) Then
            Dim data_to_read(data_count - 1) As Byte
            Dim array_ptr As UInt32 = 0
            Dim bytes_left As UInt32 = data_count
            Do Until bytes_left = 0
                Dim buffer_size As UInt32 = 0
                Dim die_page_addr As UInt32 = GetPageForMultiDie(page_addr, page_offset, bytes_left, buffer_size, memory_area)
                Dim die_data() As Byte = ReadPages(die_page_addr, page_offset, buffer_size, memory_area)
                Array.Copy(die_data, 0, data_to_read, array_ptr, die_data.Length) : array_ptr += buffer_size
            Loop
            Return data_to_read
        Else
            Return ReadPages(page_addr, page_offset, data_count, memory_area)
        End If
    End Function

    Friend Function SectorCount() As UInteger Implements MemoryDeviceUSB.SectorCount
        Return MyFlashDevice.BLOCK_COUNT
    End Function

    Friend Function SectorFind(sector_index As UInteger) As Long Implements MemoryDeviceUSB.SectorFind
        Dim base_addr As UInt32 = 0
        If sector_index > 0 Then
            For i As UInt32 = 0 To sector_index - 1
                base_addr += Me.SectorSize(i)
            Next
        End If
        Return base_addr
    End Function

    Friend Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        If FCUSB.SPI_NOR_IF.W25M121AV_Mode Then SPIBUS_WriteRead({&HC2, 1}) : WaitUntilReady()
        Dim Result As Boolean = FCUSB.NAND_IF.EraseChip()
        If Result Then
            RaiseEvent PrintConsole("Successfully erased NAND Flash device")
        Else
            RaiseEvent PrintConsole("Error while erasing NAND flash device")
        End If
        Return Result
    End Function

    Friend Function SectorErase(sector_index As UInt32) As Boolean Implements MemoryDeviceUSB.SectorErase
        If Not MyFlashDevice.ERASE_REQUIRED Then Return True
        If FCUSB.SPI_NOR_IF.W25M121AV_Mode Then SPIBUS_WriteRead({&HC2, 1}) : WaitUntilReady()
        Dim page_addr As UInt32 = (MyFlashDevice.PAGE_COUNT * sector_index)
        Dim local_page_addr As UInt32 = FCUSB.NAND_IF.GetPageMapping(page_addr)
        Return FCUSB.NAND_IF.ERASEBLOCK(local_page_addr, Me.MemoryArea, MySettings.NAND_Preserve)
    End Function

    Friend Function SectorWrite(sector_index As UInt32, data() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.SectorWrite
        If FCUSB.SPI_NOR_IF.W25M121AV_Mode Then SPIBUS_WriteRead({&HC2, 1}) : WaitUntilReady()
        Dim Addr32 As UInt32 = Me.SectorFind(sector_index)
        Return WriteData(Addr32, data, Params)
    End Function

    Friend Function WriteData(logical_address As Long, data_to_write() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        If FCUSB.SPI_NOR_IF.W25M121AV_Mode Then SPIBUS_WriteRead({&HC2, 1}) : WaitUntilReady()
        Dim page_addr As UInt32 'This is the page address
        Dim page_offset As UInt16 'this is the start offset within the page (obsolete for now)
        If (Me.MemoryArea = FlashArea.Main) Then
            page_addr = (logical_address / MyFlashDevice.PAGE_SIZE)
            page_offset = logical_address - (page_addr * MyFlashDevice.PAGE_SIZE)
        ElseIf (Me.MemoryArea = FlashArea.OOB) Then
            page_addr = Math.Floor(logical_address / MyFlashDevice.PAGE_EXT)
            page_offset = logical_address - (page_addr * (MyFlashDevice.PAGE_EXT))
        ElseIf (Me.MemoryArea = FlashArea.All) Then   'we need to adjust large address to logical address
            page_addr = (logical_address / MyFlashDevice.PAGE_EXT)
            page_offset = logical_address - (page_addr * (MyFlashDevice.PAGE_SIZE + MyFlashDevice.PAGE_EXT))
        End If
        page_addr = FCUSB.NAND_IF.GetPageMapping(page_addr) 'Adjusts the page to point to a valid page
        Dim result As Boolean = FCUSB.NAND_IF.WRITEPAGE(page_addr, data_to_write, Me.MemoryArea) 'We will write the whole block instead
        FCUSB.USB_WaitForComplete()
        Return result
    End Function

#End Region

#Region "NAND IF"

    Private Sub NANDHELPER_SetupHandlers()
        RemoveHandler FCUSB.NAND_IF.PrintConsole, AddressOf NAND_PrintConsole
        RemoveHandler FCUSB.NAND_IF.SetProgress, AddressOf NAND_SetProgress
        RemoveHandler FCUSB.NAND_IF.ReadPages, AddressOf NAND_ReadPages
        RemoveHandler FCUSB.NAND_IF.WritePages, AddressOf NAND_WritePages
        RemoveHandler FCUSB.NAND_IF.EraseSector, AddressOf NAND_EraseSector
        RemoveHandler FCUSB.NAND_IF.Ready, AddressOf WaitUntilReady
        AddHandler FCUSB.NAND_IF.PrintConsole, AddressOf NAND_PrintConsole
        AddHandler FCUSB.NAND_IF.SetProgress, AddressOf NAND_SetProgress
        AddHandler FCUSB.NAND_IF.ReadPages, AddressOf NAND_ReadPages
        AddHandler FCUSB.NAND_IF.WritePages, AddressOf NAND_WritePages
        AddHandler FCUSB.NAND_IF.EraseSector, AddressOf NAND_EraseSector
        AddHandler FCUSB.NAND_IF.Ready, AddressOf WaitUntilReady
    End Sub

    Private Sub NAND_PrintConsole(msg As String)
        RaiseEvent PrintConsole(msg)
    End Sub

    Private Sub NAND_SetProgress(percent As Integer)
        RaiseEvent SetProgress(percent)
    End Sub

    Public Sub NAND_ReadPages(page_addr As UInt32, page_offset As UInt16, data_count As UInt32, memory_area As FlashArea, ByRef data() As Byte)
        If (MyFlashDevice.STACKED_DIES > 1) Then
            ReDim data(data_count - 1)
            Dim array_ptr As UInt32 = 0
            Dim bytes_left As UInt32 = data_count
            Do Until bytes_left = 0
                Dim buffer_size As UInt32 = 0
                Dim die_page_addr As UInt32 = GetPageForMultiDie(page_addr, page_offset, bytes_left, buffer_size, memory_area)
                Dim die_data() As Byte = ReadPages(die_page_addr, page_offset, buffer_size, memory_area)
                Array.Copy(die_data, 0, data, array_ptr, die_data.Length) : array_ptr += buffer_size
            Loop
        Else
            data = ReadPages(page_addr, page_offset, data_count, memory_area)
        End If
    End Sub

    Private Sub NAND_WritePages(page_addr As UInt32, main() As Byte, oob() As Byte, memory_area As FlashArea, ByRef write_result As Boolean)
        If (MyFlashDevice.STACKED_DIES > 1) Then
            Dim main_ptr As UInt32 = 0
            Dim oob_ptr As UInt32 = 0
            If (memory_area = FlashArea.All) Then 'ignore oob()
                If main Is Nothing Then write_result = False : Exit Sub
                Dim bytes_left As UInt32 = main.Length
                Do Until (bytes_left = 0)
                    Dim main_buffer_size As UInt32 = 0
                    Dim die_page_addr As UInt32 = GetPageForMultiDie(page_addr, 0, bytes_left, main_buffer_size, memory_area)
                    Dim die_data(main_buffer_size - 1) As Byte
                    Array.Copy(main, main_ptr, die_data, 0, die_data.Length) : main_ptr += main_buffer_size
                    write_result = WriteBulk_SNAND(die_page_addr, die_data, Nothing, FlashArea.All)
                    If Not write_result Then Exit Sub
                Loop
            Else
                If main IsNot Nothing Then
                    Dim bytes_left As UInt32 = main.Length
                    Do Until bytes_left = 0
                        Dim main_buffer_size As UInt32 = 0
                        Dim die_page_addr As UInt32 = GetPageForMultiDie(page_addr, 0, bytes_left, main_buffer_size, memory_area)
                        Dim die_data(main_buffer_size - 1) As Byte
                        Dim die_oob() As Byte = Nothing
                        Array.Copy(main, main_ptr, die_data, 0, die_data.Length) : main_ptr += main_buffer_size
                        If oob IsNot Nothing Then
                            Dim main_pages As UInt32 = main_buffer_size / MyFlashDevice.PAGE_SIZE
                            Dim oob_buffer_size As UInt32 = main_pages * MyFlashDevice.PAGE_EXT
                            ReDim die_oob(oob_buffer_size - 1)
                            Array.Copy(oob, oob_ptr, die_oob, 0, die_oob.Length) : oob_ptr += oob_buffer_size
                        End If
                        write_result = WriteBulk_SNAND(die_page_addr, die_data, die_oob, FlashArea.Main)
                        If Not write_result Then Exit Sub
                    Loop
                ElseIf oob IsNot Nothing Then
                    Dim bytes_left As UInt32 = oob.Length
                    Do Until bytes_left = 0
                        Dim oob_buffer_size As UInt32 = 0
                        Dim die_page_addr As UInt32 = GetPageForMultiDie(page_addr, 0, bytes_left, oob_buffer_size, memory_area)
                        Dim die_oob(oob_buffer_size - 1) As Byte
                        Array.Copy(oob, oob_ptr, die_oob, 0, die_oob.Length) : oob_ptr += oob_buffer_size
                        write_result = WriteBulk_SNAND(die_page_addr, Nothing, die_oob, FlashArea.OOB)
                        If Not write_result Then Exit Sub
                    Loop
                Else
                    write_result = False
                End If
            End If
        Else
            write_result = WriteBulk_SNAND(page_addr, main, oob, memory_area)
        End If
    End Sub

    Private Sub NAND_EraseSector(page_addr As UInt32, ByRef erase_result As Boolean)
        If (MyFlashDevice.STACKED_DIES > 1) Then 'Multi-die support
            page_addr = GetPageForMultiDie(page_addr, 0, 0, 0, FlashArea.Main)
        End If
        Dim block(2) As Byte
        block(2) = (page_addr >> 16) And 255
        block(1) = (page_addr >> 8) And 255
        block(0) = page_addr And 255
        SPIBUS_WriteEnable()
        SPIBUS_WriteRead({OPCMD_BLOCKERASE, block(2), block(1), block(0)})
        WaitUntilReady()
        erase_result = True
    End Sub

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
            If Result Then TotalBytesTransfered += WriteBuffer.Length
        End If
        If (ReadBuffer IsNot Nothing) Then
            Dim BytesRead As Integer = 0
            Dim Result As Boolean = SPIBUS_ReadData(ReadBuffer)
            If Result Then TotalBytesTransfered += ReadBuffer.Length
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
            Success = FCUSB.USB_SETUP_BULKOUT(USBREQ.SPI_WR_DATA, Nothing, DataOut, DataOut.Length)
        Catch ex As Exception
            Return False
        End Try
        If Not Success Then RaiseEvent PrintConsole(RM.GetString("spi_error_writing"))
        Return True
    End Function

    Private Function SPIBUS_ReadData(ByRef Data_In() As Byte) As Boolean
        Dim Success As Boolean = False
        Try
            Success = FCUSB.USB_SETUP_BULKIN(USBREQ.SPI_RD_DATA, Nothing, Data_In, Data_In.Length)
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

    Public Function ReadStatusRegister() As Byte()
        Try
            Dim sr(0) As Byte
            SPIBUS_WriteRead({OPCMD_GETFEAT, OPCMD_SR}, sr)
            Return sr
        Catch ex As Exception
            Return Nothing 'Erorr
        End Try
    End Function

    Public Function ReadPages(page_addr As UInt32, page_offset As UInt16, count As UInt32, memory_area As FlashArea) As Byte()
        Dim read_fnc As USB_Readages
        Try
            If (FCUSB.HasLogic()) Then 'Hardware-enabled routine
                read_fnc = New USB_Readages(AddressOf ReadPages_Hardware)
            Else 'Software mode only
                read_fnc = New USB_Readages(AddressOf ReadPages_Software)
            End If
            If NAND_ECC IsNot Nothing AndAlso (memory_area = FlashArea.Main) Then
                Dim NAND_DEV As SPI_NAND = DirectCast(MyFlashDevice, SPI_NAND)
                Dim page_count As UInt32 = Math.Ceiling((count + page_offset) / NAND_DEV.PAGE_SIZE) 'Number of complete pages and OOB to read and correct
                Dim total_main_bytes As UInt32 = (page_count * NAND_DEV.PAGE_SIZE)
                Dim total_oob_bytes As UInt32 = (page_count * NAND_DEV.PAGE_EXT)
                Dim main_area_data() As Byte = read_fnc(page_addr, 0, total_main_bytes, FlashArea.Main)
                Dim oob_area_data() As Byte = read_fnc(page_addr, 0, total_oob_bytes, FlashArea.OOB)
                Dim ecc_data() As Byte = NAND_ECC.GetEccFromSpare(oob_area_data, NAND_DEV.PAGE_SIZE, NAND_DEV.PAGE_EXT) 'This strips out the ecc data from the spare area
                ECC_LAST_RESULT = NAND_ECC.ReadData(main_area_data, ecc_data) 'This processes the flash data (512 bytes at a time) and corrects for any errors using the ECC
                If ECC_LAST_RESULT = ECC_DECODE_RESULT.Uncorractable Then
                    Dim logical_addr As Long = (page_addr * CLng(MyFlashDevice.PAGE_SIZE)) + page_offset
                    RaiseEvent PrintConsole("ECC failed at: 0x" & Hex(logical_addr).PadLeft(8, "0"))
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

    Private Function ReadPages_Hardware(page_addr As UInt32, page_offset As UInt16, data_count As UInt32, memory_area As FlashArea) As Byte()
        Dim setup() As Byte = SetupPacket_NAND(page_addr, page_offset, data_count, memory_area)
        Dim param As UInt32 = 0
        If MyFlashDevice.PLANE_SELECT Then param = (param Or 1)
        If MyFlashDevice.READ_CMD_DUMMY Then param = (param Or (1 << 1))
        Dim data_out(data_count - 1) As Byte
        If Not FCUSB.USB_SETUP_BULKIN(USBREQ.SPINAND_READFLASH, setup, data_out, param) Then Return Nothing
        Return data_out
    End Function
    'SPI NAND software mode
    Private Function ReadPages_Software(page_addr As UInt32, page_offset As UInt16, count As UInt32, memory_area As FlashArea) As Byte()
        Dim data_out(count - 1) As Byte
        Dim bytes_left As UInt32 = count
        Dim page_size_tot As UInt16 = (MyFlashDevice.PAGE_SIZE + MyFlashDevice.PAGE_EXT)
        Dim nand_layout As NAND_LAYOUT_TOOL.NANDLAYOUT_STRUCTURE = NAND_LayoutTool.GetStructure(MyFlashDevice)
        Dim data_ptr As UInt32 = 0
        Dim logical_block As UInt16 = (nand_layout.Layout_Main + nand_layout.Layout_Spare)
        Do While (bytes_left > 0)
            Dim read_offset As UInt16 = 0 'We always want to read the entire page
            If MyFlashDevice.PLANE_SELECT Then read_offset = read_offset Or &H1000 'Sets plane select to HIGH
            Dim op_setup() As Byte = GetReadCacheCommand(read_offset)
            Dim read_packet(page_size_tot - 1) As Byte
            SPIBUS_WriteRead({OPCMD_PAGEREAD, ((page_addr >> 16) And 255), ((page_addr >> 8) And 255), (page_addr And 255)})
            SPIBUS_WriteRead(op_setup, read_packet)
            Select Case memory_area
                Case FlashArea.Main
                    Dim sub_index As UInt16 = Math.Floor(page_offset / nand_layout.Layout_Main) 'the sub block we are in			
                    Dim adj_offset As UInt16 = (sub_index * logical_block) + (page_offset Mod nand_layout.Layout_Main)
                    sub_index += 1
                    Do While Not (adj_offset = page_size_tot)
                        Dim sub_left As UInt16 = nand_layout.Layout_Main - (page_offset Mod nand_layout.Layout_Main)
                        If sub_left > bytes_left Then sub_left = bytes_left
                        Array.Copy(read_packet, adj_offset, data_out, data_ptr, sub_left)
                        data_ptr += sub_left
                        page_offset += sub_left
                        bytes_left -= sub_left
                        If (bytes_left = 0) Then Exit Do
                        adj_offset = (sub_index * logical_block)
                        sub_index += 1
                    Loop
                    If (page_offset = MyFlashDevice.PAGE_SIZE) Then
                        page_offset = 0
                        page_addr += 1
                    End If
                Case FlashArea.OOB
                    Dim sub_index As UInt16 = Math.Floor(page_offset / nand_layout.Layout_Spare) + 1 'the sub block we are in			
                    Dim adj_offset As UInt16 = ((sub_index * logical_block) - nand_layout.Layout_Spare) + (page_offset Mod nand_layout.Layout_Spare)
                    sub_index += 1
                    Do While Not ((adj_offset - nand_layout.Layout_Main) = page_size_tot)
                        Dim sub_left As UInt16 = nand_layout.Layout_Spare - (page_offset Mod nand_layout.Layout_Spare)
                        If sub_left > bytes_left Then sub_left = bytes_left
                        Array.Copy(read_packet, adj_offset, data_out, data_ptr, sub_left)
                        data_ptr += sub_left
                        page_offset += sub_left
                        bytes_left -= sub_left
                        If (bytes_left = 0) Then Exit Do
                        adj_offset = ((sub_index * logical_block) - nand_layout.Layout_Spare)
                        sub_index += 1
                    Loop
                    If (page_offset = MyFlashDevice.PAGE_EXT) Then
                        page_offset = 0
                        page_addr += 1
                    End If
                Case FlashArea.All
                    Dim sub_left As UInt16 = page_size_tot - page_offset
                    Dim transfer_count As UInt16 = sub_left
                    If (transfer_count > bytes_left) Then transfer_count = bytes_left
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
            rd_cmd(2) = ((read_offset >> 8) And 255) 'Column address (upper)
            rd_cmd(3) = (read_offset And 255) 'Lower
        Else
            ReDim rd_cmd(3)
            rd_cmd(0) = OPCMD_READCACHE
            rd_cmd(1) = ((read_offset >> 8) And 255) 'Column address (upper)
            rd_cmd(2) = (read_offset And 255) 'Lower
        End If
        Return rd_cmd
    End Function

    Private Sub SNAND_Wait()
        SPIBUS_SlaveSelect_Enable()
        SPIBUS_WriteData({&HF, &HC0})
        Dim sr(0) As Byte
        Do
            SPIBUS_ReadData(sr)
            Utilities.Sleep(5)
        Loop While (sr(0) And 1 = 1)
        SPIBUS_SlaveSelect_Disable()
    End Sub

    Private Function WriteBulk_SNAND(page_addr As UInt32, main_data() As Byte, oob_data() As Byte, memory_area As FlashArea) As Boolean
        Try
            If main_data Is Nothing And oob_data Is Nothing Then Return False
            Dim NAND_DEV As SPI_NAND = DirectCast(MyFlashDevice, SPI_NAND)
            Dim page_size_tot As UInt16 = (MyFlashDevice.PAGE_SIZE + NAND_DEV.PAGE_EXT)
            Dim page_aligned() As Byte = Nothing
            If memory_area = FlashArea.All Then 'Ignore OOB/SPARE
                oob_data = Nothing
                Dim total_pages As UInt32 = Math.Ceiling(main_data.Length / page_size_tot)
                ReDim page_aligned((total_pages * page_size_tot) - 1)
                For i = 0 To page_aligned.Length - 1 : page_aligned(i) = 255 : Next
                Array.Copy(main_data, 0, page_aligned, 0, main_data.Length)
            ElseIf memory_area = FlashArea.Main Then
                If NAND_ECC IsNot Nothing Then
                    If oob_data Is Nothing Then
                        ReDim oob_data(((main_data.Length / NAND_DEV.PAGE_SIZE) * NAND_DEV.PAGE_EXT) - 1)
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

    Private Function USB_WritePageAlignedData(ByRef page_addr As UInt32, page_aligned() As Byte) As Boolean
        Dim page_size_tot As UInt16 = (MyFlashDevice.PAGE_SIZE + MyFlashDevice.PAGE_EXT)
        Dim pages_to_write As UInt32 = (page_aligned.Length / page_size_tot)
        If (FCUSB.HasLogic()) Then 'Hardware-enabled routine
            Dim array_ptr As UInt32 = 0
            Do Until pages_to_write = 0
                Dim max_page_count As Integer = 8192 / MyFlashDevice.PAGE_SIZE
                Dim count As UInt32 = Math.Min(max_page_count, pages_to_write) 'Write up to 4 pages (fcusb pro buffer has 12KB total)
                Dim packet((count * page_size_tot) - 1) As Byte
                Array.Copy(page_aligned, array_ptr, packet, 0, packet.Length)
                array_ptr += packet.Length
                Dim setup() As Byte = SetupPacket_NAND(page_addr, 0, packet.Length, FlashArea.All) 'We will write the entire page
                Dim param As UInt32 = Utilities.BoolToInt(MyFlashDevice.PLANE_SELECT)
                Dim result As Boolean = FCUSB.USB_SETUP_BULKOUT(USBREQ.SPINAND_WRITEFLASH, setup, packet, param)
                If Not result Then Return False
                FCUSB.USB_WaitForComplete()
                page_addr += count
                pages_to_write -= count
            Loop
        Else
            For i = 0 To pages_to_write - 1
                Dim cache_data(page_size_tot - 1) As Byte
                Array.Copy(page_aligned, (i * page_size_tot), cache_data, 0, cache_data.Length)
                SPIBUS_WriteEnable()
                Dim cache_offset As UInt16 = 0
                If MyFlashDevice.PLANE_SELECT Then
                    If (Math.Floor(page_addr / 64) And 1 = 1) Then cache_offset = cache_offset Or &H1000 'Sets plane select to HIGH
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
        setup_packet(1) = ((page_offset >> 8) And 255) 'Column address (upper)
        setup_packet(2) = (page_offset And 255) 'Lower
        Array.Copy(data_to_load, 0, setup_packet, 3, data_to_load.Length)
        SPIBUS_WriteRead(setup_packet)
    End Sub

    Private Sub ProgramPageCache(page_addr As UInt32)
        Dim exe_packet(3) As Byte
        exe_packet(0) = OPCMD_PROGCACHE
        exe_packet(1) = ((page_addr >> 16) And 255)
        exe_packet(2) = ((page_addr >> 8) And 255)
        exe_packet(3) = (page_addr And 255)
        SPIBUS_WriteRead(exe_packet)
    End Sub

    Private Function SetupPacket_NAND(page_addr As UInt32, page_offset As UInt16, Count As UInt32, area As FlashArea) As Byte()
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

    Private Function GetPageForMultiDie(ByRef page_addr As UInt32, page_offset As UInt16, ByRef count As UInt32, ByRef buffer_size As UInt32, area As FlashArea) As UInt32
        Dim total_pages As UInt32 = (MyFlashDevice.FLASH_SIZE / MyFlashDevice.PAGE_SIZE)
        Dim pages_per_die As UInt32 = total_pages / MyFlashDevice.STACKED_DIES
        Dim die_id As Byte = Math.Floor(page_addr / pages_per_die)
        If (die_id <> Me.DIE_SELECTED) Then
            SPIBUS_WriteRead({OPCMD_DIESELECT, die_id})
            WaitUntilReady()
            Me.DIE_SELECTED = die_id
        End If
        Dim pages_left As UInt32 = (pages_per_die - (page_addr Mod pages_per_die))
        Dim bytes_left As UInt32 'Total number of bytes left in this die (for the selected area)
        Dim page_area_size As UInt32 'Number of bytes we are accessing
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
        Dim die_page_addr As UInt32 = (page_addr Mod pages_per_die)
        Return die_page_addr
    End Function

End Class
