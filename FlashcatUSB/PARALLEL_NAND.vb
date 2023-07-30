'COPYRIGHT EMBEDDED COMPUTERS LLC 2020 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB PRODUCTS
'CONTACT EMAIL: support@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: This is the main module that is loaded first.

Imports FlashcatUSB.ECC_LIB
Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB

Public Class PARALLEL_NAND : Implements MemoryDeviceUSB
    Private FCUSB As FCUSB_DEVICE
    Public Property MyFlashDevice As P_NAND
    Public Property MyFlashStatus As DeviceStatus = DeviceStatus.NotDetected
    Public Property ONFI As NAND_ONFI
    Public Property MyAdapter As MEM_PROTOCOL 'This is the kind of socket adapter connected and the mode it is in

    Private FLASH_IDENT As FlashDetectResult

    Public Event PrintConsole(msg As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress

    Sub New(parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        MyFlashDevice = Nothing
        Me.ONFI = Nothing
        If Not EXPIO_SETUP_USB(MEM_PROTOCOL.SETUP) Then
            RaiseEvent PrintConsole("Parallel I/O failed to initialize")
            Me.MyFlashStatus = DeviceStatus.ExtIoNotConnected
            Return False
        Else
            RaiseEvent PrintConsole(RM.GetString("io_mode_initalized"))
        End If
        If DetectFlashDevice() Then
            Dim chip_id_str As String = Hex(FLASH_IDENT.MFG).PadLeft(2, "0") & Hex(FLASH_IDENT.PART).PadLeft(8, "0")
            RaiseEvent PrintConsole(String.Format(RM.GetString("ext_connected_chipid"), chip_id_str))
            If (FCUSB.HWBOARD = FCUSB_BOARD.XPORT_PCB2) Then
                If (FLASH_IDENT.ID1 >> 8 = 255) Then FLASH_IDENT.ID1 = (FLASH_IDENT.ID1 And 255)
            End If
            Dim device_matches() As Device = FlashDatabase.FindDevices(FLASH_IDENT.MFG, FLASH_IDENT.ID1, FLASH_IDENT.ID2, MemoryType.PARALLEL_NAND)
            Me.ONFI = New NAND_ONFI(NAND_GetONFITable())
            If Me.ONFI Is Nothing OrElse (Not Me.ONFI.IS_VALID) Then
                RaiseEvent PrintConsole("NAND device failed to load ONFI table")
            End If
            If (device_matches IsNot Nothing AndAlso device_matches.Count > 0) Then
                MyFlashDevice_SelectBest(device_matches)
                RaiseEvent PrintConsole(String.Format(RM.GetString("flash_detected"), MyFlashDevice.NAME, Format(MyFlashDevice.FLASH_SIZE, "#,###")))
                RaiseEvent PrintConsole(RM.GetString("ext_prog_mode"))
                Dim page_info As String = String.Format(RM.GetString("ext_page_size"), Strings.Format(MyFlashDevice.PAGE_SIZE, "#,###"), DirectCast(MyFlashDevice, P_NAND).PAGE_EXT)
                RaiseEvent PrintConsole(page_info)
                RaiseEvent PrintConsole("Block size: " & Strings.Format((DirectCast(MyFlashDevice, P_NAND).Block_Size), "#,###") & " bytes")
                Dim nand_mem As P_NAND = DirectCast(MyFlashDevice, P_NAND)
                If nand_mem.IFACE = VCC_IF.X8_3V Then
                    RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NAND (X8 3.3V)")
                ElseIf nand_mem.IFACE = VCC_IF.X8_1V8 Then
                    RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NAND (X8 1.8V)")
                ElseIf nand_mem.IFACE = VCC_IF.X16_3V Then
                    RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NAND (X16 3.3V)")
                ElseIf nand_mem.IFACE = VCC_IF.X16_1V8 Then
                    RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NAND (X16 1.8V)")
                End If
                If (FCUSB.HWBOARD = FCUSB_BOARD.XPORT_PCB2) Then
                    If (Not nand_mem.IFACE = VCC_IF.X8_3V) Then
                        RaiseEvent PrintConsole("This NAND device is not compatible with this programmer")
                        Me.MyFlashStatus = DeviceStatus.NotCompatible
                        Return False
                    End If
                End If
                If nand_mem.IFACE = VCC_IF.X16_3V Or nand_mem.IFACE = VCC_IF.X16_1V8 Then
                    RaiseEvent PrintConsole("NAND interface changed to X16")
                    EXPIO_SETUP_USB(MEM_PROTOCOL.NAND_X16_ASYNC)
                    Me.MyAdapter = MEM_PROTOCOL.NAND_X16_ASYNC
                End If
                If (nand_mem.FLASH_SIZE > Gb004) Then 'Remove this check if you wish
                    If (FCUSB.HWBOARD = FCUSB_BOARD.XPORT_PCB2) Then
                        RaiseEvent PrintConsole("XPORT is not compatible with NAND Flash larger than 4Gbit")
                        RaiseEvent PrintConsole("Please upgrade to Mach1")
                        Me.MyFlashStatus = DeviceStatus.NotCompatible
                        Return False
                    End If
                End If
                NAND_SetupHandlers()
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.NAND_SETTYPE, Nothing, nand_mem.ADDRESS_SCHEME)
                FCUSB.NAND_IF.CreateMap(nand_mem.FLASH_SIZE, nand_mem.PAGE_SIZE, nand_mem.PAGE_EXT, nand_mem.PAGE_COUNT, nand_mem.BLOCK_COUNT)
                FCUSB.NAND_IF.EnableBlockManager() 'If enabled 
                FCUSB.NAND_IF.ProcessMap()
                ECC_LoadConfiguration(nand_mem.PAGE_SIZE, nand_mem.PAGE_EXT)
                Utilities.Sleep(10) 'We need to wait here (device is being configured)
                Me.MyFlashStatus = DeviceStatus.Supported
                Return True
            Else
                RaiseEvent PrintConsole(RM.GetString("unknown_device_email"))
                MyFlashDevice = Nothing
                Me.MyFlashStatus = DeviceStatus.NotSupported
            End If
        Else
            GUI.PrintConsole(RM.GetString("ext_not_detected"))
            Me.MyFlashStatus = DeviceStatus.NotDetected
        End If
        Return False
    End Function

    Private Sub MyFlashDevice_SelectBest(device_matches() As Device)
        If device_matches.Length = 1 Then
            MyFlashDevice = device_matches(0)
            Exit Sub
        End If
        If Me.ONFI.IS_VALID Then
            For i = 0 To device_matches.Count - 1
                Dim PART As String = device_matches(i).NAME.ToUpper
                If PART.IndexOf(" ") > 0 Then PART = PART.Substring(PART.IndexOf(" ") + 1)
                If PART.Equals(Me.ONFI.DEVICE_MODEL) Then
                    MyFlashDevice = device_matches(i) : Exit Sub
                End If
            Next
        End If
        If MyFlashDevice Is Nothing Then MyFlashDevice = device_matches(0)
    End Sub

    Private Function NAND_GetSR() As Byte
        Dim result_data(0) As Byte
        Dim result As Boolean = FCUSB.USB_CONTROL_MSG_IN(USBREQ.NAND_SR, result_data)
        Return result_data(0) 'E0 11100000
    End Function

    Private Function NAND_GetONFITable() As Byte()
        Dim onfi(255) As Byte
        If FCUSB.USB_SETUP_BULKIN(USBREQ.NAND_ONFI, Nothing, onfi, 0) Then
            Return onfi
        End If
        Return Nothing
    End Function

    Private Function DetectFlashDevice() As Boolean
        RaiseEvent PrintConsole(RM.GetString("ext_detecting_device")) 'Attempting to automatically detect Flash device
        Dim LAST_DETECT As FlashDetectResult = Nothing
        LAST_DETECT.MFG = 0
        Me.FLASH_IDENT = DetectFlash(MEM_PROTOCOL.NAND_X8_ASYNC) 'X16 and X8 are detected with X8
        If Me.FLASH_IDENT.Successful Then
            Dim d() As Device = FlashDatabase.FindDevices(Me.FLASH_IDENT.MFG, Me.FLASH_IDENT.ID1, Me.FLASH_IDENT.ID2, MemoryType.PARALLEL_NAND)
            If (d.Count > 0) Then
                RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "NAND"))
                MyAdapter = MEM_PROTOCOL.NAND_X8_ASYNC
                Return True
            Else
                LAST_DETECT = Me.FLASH_IDENT
            End If
        End If
        If (Not LAST_DETECT.MFG = 0) Then
            Me.FLASH_IDENT = LAST_DETECT
            Return True 'Found, but not in library
        End If
        Return False 'No devices detected
    End Function

    Private Function DetectFlash(mode As MEM_PROTOCOL) As FlashDetectResult
        Dim mode_name As String = ""
        Dim ident_data(7) As Byte '8 bytes total
        Dim result As FlashDetectResult
        Select Case mode
            Case MEM_PROTOCOL.NAND_X8_ASYNC
                mode_name = "NAND X8 ASYNC"
                EXPIO_SETUP_USB(MEM_PROTOCOL.NAND_X8_ASYNC)
                result = EXPIO_DetectNAND()
        End Select
        If result.Successful Then
            Dim part As UInt32 = (CUInt(result.ID1) << 16) Or (result.ID2)
            Dim chip_id_str As String = Hex(result.MFG).PadLeft(2, "0") & Hex(part).PadLeft(8, "0")
            RaiseEvent PrintConsole("Mode " & mode_name & " returned ident code: 0x" & chip_id_str)
        End If
        Return result
    End Function

    Private Function EXPIO_DetectNAND() As FlashDetectResult
        Dim ident_data(7) As Byte
        If FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_RDID, ident_data) Then
            Return GetFlashResult(ident_data)
        Else
            RaiseEvent PrintConsole("Error detecting NAND device")
            Return Nothing
        End If
    End Function


#Region "NAND Block Manager"

    Private Sub NAND_SetupHandlers()
        RemoveHandler FCUSB.NAND_IF.PrintConsole, AddressOf NAND_PrintConsole
        RemoveHandler FCUSB.NAND_IF.SetProgress, AddressOf NAND_SetProgress
        RemoveHandler FCUSB.NAND_IF.ReadPages, AddressOf NAND_ReadPages
        RemoveHandler FCUSB.NAND_IF.WritePages, AddressOf NAND_WritePages
        RemoveHandler FCUSB.NAND_IF.EraseSector, AddressOf NAND_EraseSector
        RemoveHandler FCUSB.NAND_IF.Ready, AddressOf WaitForReady
        AddHandler FCUSB.NAND_IF.PrintConsole, AddressOf NAND_PrintConsole
        AddHandler FCUSB.NAND_IF.SetProgress, AddressOf NAND_SetProgress
        AddHandler FCUSB.NAND_IF.ReadPages, AddressOf NAND_ReadPages
        AddHandler FCUSB.NAND_IF.WritePages, AddressOf NAND_WritePages
        AddHandler FCUSB.NAND_IF.EraseSector, AddressOf NAND_EraseSector
        AddHandler FCUSB.NAND_IF.Ready, AddressOf WaitForReady
    End Sub

    Private Sub NAND_PrintConsole(msg As String)
        RaiseEvent PrintConsole(msg)
    End Sub

    Private Sub NAND_SetProgress(percent As Integer)
        RaiseEvent SetProgress(percent)
    End Sub

    Public Sub NAND_ReadPages(page_addr As UInt32, page_offset As UInt16, data_count As UInt32, memory_area As FlashArea, ByRef data() As Byte)
        data = ReadBulk(page_addr, page_offset, data_count, memory_area)
    End Sub

    Private Sub NAND_WritePages(page_addr As UInt32, main() As Byte, oob() As Byte, memory_area As FlashArea, ByRef write_result As Boolean)
        write_result = WriteBulk(page_addr, main, oob, memory_area)
    End Sub

    Private Sub NAND_EraseSector(page_addr As UInt32, ByRef erase_result As Boolean)
        erase_result = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_SECTORERASE, Nothing, page_addr)
        If (MyFlashDevice.PAGE_SIZE = 512) Then 'LEGACY NAND DEVICE
            Utilities.Sleep(250) 'Micron NAND legacy delay (was 200), always wait! Just to be sure.
        Else
            If (Not FCUSB.HWBOARD = FCUSB_BOARD.Mach1) Then 'Mach1 uses HW to get correct wait
                Utilities.Sleep(50) 'Normal delay
            End If
        End If
    End Sub

#End Region

#Region "EXPIO SETUP"

    Private Enum E_BUS_WIDTH 'Number of bits transfered per operation
        X0 = 0 'Default
        X8 = 8
        X16 = 16
    End Enum

    Private Property CURRENT_BUS_WIDTH As E_BUS_WIDTH = E_BUS_WIDTH.X0

    Private Function EXPIO_SETUP_USB(mode As MEM_PROTOCOL) As Boolean
        Try
            Dim result_data(0) As Byte
            Dim setup_data As UInt32 = mode
            If FCUSB.HWBOARD = FCUSB_BOARD.Mach1 Then
                If mode = MEM_PROTOCOL.NAND_X16_ASYNC Then
                    setup_data = setup_data Or (MySettings.NAND_Speed << 16)
                    RaiseEvent PrintConsole("NAND clock speed set to: " & FlashcatSettings.NandMemSpeedToString(MySettings.NAND_Speed))
                ElseIf mode = MEM_PROTOCOL.NAND_X8_ASYNC Then
                    setup_data = setup_data Or (MySettings.NAND_Speed << 16)
                    RaiseEvent PrintConsole("NAND clock speed set to: " & FlashcatSettings.NandMemSpeedToString(MySettings.NAND_Speed))
                End If
            End If
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_INIT, result_data, setup_data)
            If Not result Then Return False
            If (result_data(0) = &H17) Then 'Extension port returns 0x17 if it can communicate with the MCP23S17
                Threading.Thread.Sleep(50) 'Give the USB time to change modes
                Select Case mode
                    Case MEM_PROTOCOL.NAND_X8_ASYNC
                        Me.CURRENT_BUS_WIDTH = E_BUS_WIDTH.X8
                    Case MEM_PROTOCOL.NAND_X16_ASYNC
                        Me.CURRENT_BUS_WIDTH = E_BUS_WIDTH.X16
                End Select
                Return True 'Communication successful
            Else
                Return False
            End If
        Catch ex As Exception
            Return False
        End Try
    End Function

#End Region

#Region "Public Interface"

    Public ReadOnly Property DeviceName() As String Implements MemoryDeviceUSB.DeviceName
        Get
            Select Case Me.MyFlashStatus
                Case DeviceStatus.Supported
                    Return MyFlashDevice.NAME
                Case DeviceStatus.NotSupported
                    Return Hex(MyFlashDevice.MFG_CODE).PadLeft(2, CChar("0")) & " " & Hex(MyFlashDevice.ID1).PadLeft(4, CChar("0"))
                Case Else
                    Return RM.GetString("no_flash_detected")
            End Select
        End Get
    End Property

    Public ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
        Get
            Dim d As P_NAND = DirectCast(MyFlashDevice, P_NAND)
            Dim available_pages As Long = FCUSB.NAND_IF.MAPPED_PAGES
            If MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Combined Then
                Return (available_pages * (d.PAGE_SIZE + d.PAGE_EXT))
            Else
                Return (available_pages * d.PAGE_SIZE)
            End If
        End Get
    End Property

    Public Function ReadData(logical_address As Long, data_count As UInt32, Optional memory_area As FlashArea = FlashArea.Main) As Byte() Implements MemoryDeviceUSB.ReadData
        Dim nand_dev As P_NAND = DirectCast(MyFlashDevice, P_NAND)
        Dim page_addr As Long  'This is the page address
        Dim page_offset As UInt16 'this is the start offset within the page
        Dim page_size As UInt32
        If (memory_area = FlashArea.Main) Then
            page_addr = Math.Floor(logical_address / CLng(MyFlashDevice.PAGE_SIZE))
            page_size = nand_dev.PAGE_SIZE
            page_offset = logical_address - (page_addr * CLng(MyFlashDevice.PAGE_SIZE))
        ElseIf (memory_area = FlashArea.OOB) Then
            page_addr = Math.Floor(logical_address / nand_dev.PAGE_EXT)
            page_offset = logical_address - (page_addr * nand_dev.PAGE_EXT)
            page_size = nand_dev.PAGE_EXT
        ElseIf (memory_area = FlashArea.All) Then   'we need to adjust large address to logical address
            Dim full_page_size As Long = (MyFlashDevice.PAGE_SIZE + nand_dev.PAGE_EXT)
            page_addr = Math.Floor(logical_address / full_page_size)
            page_offset = logical_address - (page_addr * full_page_size)
            page_size = nand_dev.PAGE_SIZE + nand_dev.PAGE_EXT
        End If
        'The following code is so we can read past invalid blocks
        Dim pages_per_block As UInt32 = nand_dev.PAGE_COUNT
        Dim data_out(data_count - 1) As Byte
        Dim data_ptr As Integer = 0
        Do While (data_count > 0)
            Dim pages_left As UInt32 = (pages_per_block - (page_addr Mod pages_per_block))
            Dim bytes_left_in_block As UInt32 = (pages_left * page_size) - page_offset
            Dim packet_size As UInt32 = Math.Min(bytes_left_in_block, data_count)
            page_addr = FCUSB.NAND_IF.GetPageMapping(page_addr)
            Dim data() As Byte = ReadBulk(page_addr, page_offset, packet_size, memory_area)
            If data Is Nothing Then Return Nothing
            Array.Copy(data, 0, data_out, data_ptr, data.Length)
            data_ptr += packet_size
            data_count -= packet_size
            page_addr += Math.Ceiling(bytes_left_in_block / page_size)
            page_offset = 0
        Loop
        Return data_out
    End Function

    Public Function SectorErase(sector_index As UInt32, Optional memory_area As FlashArea = FlashArea.Main) As Boolean Implements MemoryDeviceUSB.SectorErase
        If Not MyFlashDevice.ERASE_REQUIRED Then Return True
        Dim page_addr As UInt32 = (MyFlashDevice.PAGE_COUNT * sector_index)
        Dim local_page_addr As UInt32 = FCUSB.NAND_IF.GetPageMapping(page_addr)
        Return FCUSB.NAND_IF.ERASEBLOCK(local_page_addr, memory_area, MySettings.NAND_Preserve)
    End Function

    Public Function WriteData(logical_address As Long, data_to_write() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        Dim page_addr As UInt32 = GetNandPageAddress(MyFlashDevice, logical_address, Params.Memory_Area)
        page_addr = FCUSB.NAND_IF.GetPageMapping(page_addr) 'Adjusts the page to point to a valid page
        Dim result As Boolean = FCUSB.NAND_IF.WRITEPAGE(page_addr, data_to_write, Params.Memory_Area) 'We will write the whole block instead
        FCUSB.USB_WaitForComplete()
        Return result
        Return False
    End Function

    Public Sub WaitForReady() Implements MemoryDeviceUSB.WaitUntilReady
        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WAIT)
        FCUSB.USB_WaitForComplete() 'Checks for WAIT flag to clear
    End Sub

    Public Function SectorFind(sector_index As UInt32, Optional memory_area As FlashArea = FlashArea.Main) As Long Implements MemoryDeviceUSB.SectorFind
        Dim base_addr As UInt32 = 0
        If sector_index > 0 Then
            For i As UInt32 = 0 To sector_index - 1
                base_addr += Me.SectorSize(i, memory_area)
            Next
        End If
        Return base_addr
    End Function

    Public Function SectorWrite(sector_index As UInt32, data() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.SectorWrite
        Dim Addr32 As UInteger = Me.SectorFind(sector_index, Params.Memory_Area)
        Return WriteData(Addr32, data, Params)
    End Function

    Public Function SectorCount() As UInt32 Implements MemoryDeviceUSB.SectorCount
        Return MyFlashDevice.BLOCK_COUNT
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        Try
            Dim Result As Boolean = FCUSB.NAND_IF.EraseChip()
            If Result Then
                RaiseEvent PrintConsole(RM.GetString("nand_erase_successful"))
            Else
                RaiseEvent PrintConsole(RM.GetString("nand_erase_failed"))
            End If
        Catch ex As Exception
        End Try
        Return False
    End Function

    Friend ReadOnly Property SectorSize(sector As UInt32, Optional memory_area As FlashArea = FlashArea.Main) As UInt32 Implements MemoryDeviceUSB.SectorSize
        Get
            If Not MyFlashStatus = USB.DeviceStatus.Supported Then Return 0
            Select Case memory_area
                Case FlashArea.Main
                    Return (MyFlashDevice.PAGE_COUNT * MyFlashDevice.PAGE_SIZE)
                Case FlashArea.OOB
                    Return (MyFlashDevice.PAGE_COUNT * MyFlashDevice.PAGE_EXT)
                Case FlashArea.All
                    Return (MyFlashDevice.PAGE_COUNT * (MyFlashDevice.PAGE_SIZE + MyFlashDevice.PAGE_EXT))
            End Select
            Return 0
        End Get
    End Property

#End Region

    Public Function ReadBulk(page_addr As UInt32, page_offset As UInt16, count As UInt32, memory_area As FlashArea) As Byte()
        Try
            Dim result As Boolean
            If NAND_ECC IsNot Nothing AndAlso (memory_area = FlashArea.Main) Then 'We need to auto-correct data uisng ECC
                Dim page_count As UInt32 = Math.Ceiling((count + page_offset) / MyFlashDevice.PAGE_SIZE) 'Number of complete pages and OOB to read and correct
                Dim total_main_bytes As UInt32 = (page_count * MyFlashDevice.PAGE_SIZE)
                Dim total_oob_bytes As UInt32 = (page_count * MyFlashDevice.PAGE_EXT)
                Dim main_area_data(total_main_bytes - 1) As Byte 'Data from the main page
                Dim setup_data() As Byte = GetSetupPacket(page_addr, 0, main_area_data.Length, FlashArea.Main)
                result = FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_READDATA, setup_data, main_area_data, 1)
                If Not result Then Return Nothing
                Dim oob_area_data(total_oob_bytes - 1) As Byte 'Data from the spare page, containing flags, metadata and ecc data
                setup_data = GetSetupPacket(page_addr, 0, oob_area_data.Length, FlashArea.OOB)
                result = FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_READDATA, setup_data, oob_area_data, 1)
                If Not result Then Return Nothing
                Dim ecc_data() As Byte = NAND_ECC.GetEccFromSpare(oob_area_data, MyFlashDevice.PAGE_SIZE, MyFlashDevice.PAGE_EXT) 'This strips out the ecc data from the spare area
                ECC_LAST_RESULT = NAND_ECC.ReadData(main_area_data, ecc_data) 'This processes the flash data (512 bytes at a time) and corrects for any errors using the ECC
                If ECC_LAST_RESULT = ECC_DECODE_RESULT.Uncorractable Then
                    Dim logical_addr As Long = (page_addr * CLng(MyFlashDevice.PAGE_SIZE)) + page_offset
                    RaiseEvent PrintConsole("ECC failed at: 0x" & Hex(logical_addr).PadLeft(8, "0"))
                End If
                Dim data_out(count - 1) As Byte 'This is the data the user requested
                Array.Copy(main_area_data, page_offset, data_out, 0, data_out.Length)
                Return data_out
            Else 'Normal read from device
                Dim data_out(count - 1) As Byte 'Bytes we want to read
                Dim setup_data() As Byte = GetSetupPacket(page_addr, page_offset, count, memory_area)
                result = FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_READDATA, setup_data, data_out, 1)
                If Not result Then Return Nothing
                Return data_out
            End If
        Catch ex As Exception
        End Try
        Return Nothing
    End Function

    Private Function WriteBulk(page_addr As UInt32, main_data() As Byte, oob_data() As Byte, memory_area As FlashArea) As Boolean
        Try
            If main_data Is Nothing And oob_data Is Nothing Then Return False
            Dim page_size_tot As UInt16 = (MyFlashDevice.PAGE_SIZE + MyFlashDevice.PAGE_EXT)
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
                        ReDim oob_data(((main_data.Length / MyFlashDevice.PAGE_SIZE) * MyFlashDevice.PAGE_EXT) - 1)
                        Utilities.FillByteArray(oob_data, 255)
                    End If
                    Dim ecc_data() As Byte = Nothing
                    NAND_ECC.WriteData(main_data, ecc_data)
                    NAND_ECC.SetEccToSpare(oob_data, ecc_data, MyFlashDevice.PAGE_SIZE, MyFlashDevice.PAGE_EXT)
                End If
                page_aligned = CreatePageAligned(MyFlashDevice, main_data, oob_data)
            ElseIf memory_area = FlashArea.OOB Then
                page_aligned = CreatePageAligned(MyFlashDevice, main_data, oob_data)
            End If
            Dim pages_to_write As UInt32 = (page_aligned.Length / page_size_tot)
            Dim array_ptr As UInt32 = 0
            Do Until pages_to_write = 0
                Dim page_count_max As Integer = GetMaxPacketCount(MyFlashDevice.PAGE_SIZE)
                Dim page_count As UInt32 = Math.Min(page_count_max, pages_to_write)
                Dim packet((page_count * page_size_tot) - 1) As Byte
                Array.Copy(page_aligned, array_ptr, packet, 0, packet.Length)
                array_ptr += packet.Length
                Dim setup() As Byte = GetSetupPacket(page_addr, 0, packet.Length, FlashArea.All) 'We will write the entire page
                Dim result As Boolean = FCUSB.USB_SETUP_BULKOUT(USBREQ.EXPIO_WRITEDATA, setup, packet, 1)
                If Not result Then Return Nothing
                FCUSB.USB_WaitForComplete()
                page_addr += page_count
                pages_to_write -= page_count
            Loop
            Return True
        Catch ex As Exception
        End Try
        Return False
    End Function
    'Returns the max number of pages the hardware can support via bulk write
    Private Function GetMaxPacketCount(PageSize As UInt32) As Integer
        Dim page_count_max As Integer = 0 'Number of total pages to write per operation
        If PageSize = 512 Then
            page_count_max = 8
        ElseIf PageSize = 2048 Then
            page_count_max = 4
        ElseIf PageSize = 4096 Then
            page_count_max = 2
        ElseIf PageSize = 8192 Then
            page_count_max = 1
        End If
        Return page_count_max
    End Function

    Private Function GetSetupPacket(page_addr As UInt32, page_offset As UInt16, transfer_size As UInt32, memory_area As FlashArea) As Byte()
        Dim nand_layout As NANDLAYOUT_STRUCTURE = NANDLAYOUT_Get(MyFlashDevice)
        If MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Combined Then memory_area = FlashArea.All
        Dim TX_NAND_ADDRSIZE As Byte 'Number of bytes the address command table uses
        If (MyFlashDevice.PAGE_SIZE = 512) Then 'Small page
            If (MyFlashDevice.FLASH_SIZE > Mb256) Then
                TX_NAND_ADDRSIZE = 4
            Else
                TX_NAND_ADDRSIZE = 3
            End If
        Else
            If MyFlashDevice.FLASH_SIZE < Gb002 Then
                TX_NAND_ADDRSIZE = 4 '<=1Gbit
            Else
                TX_NAND_ADDRSIZE = 5 '2Gbit+
            End If
        End If
        Dim setup_data(21) As Byte '18 bytes total
        setup_data(0) = CByte(page_addr And 255)
        setup_data(1) = CByte((page_addr >> 8) And 255)
        setup_data(2) = CByte((page_addr >> 16) And 255)
        setup_data(3) = CByte((page_addr >> 24) And 255)
        setup_data(4) = CByte(transfer_size And 255)
        setup_data(5) = CByte((transfer_size >> 8) And 255)
        setup_data(6) = CByte((transfer_size >> 16) And 255)
        setup_data(7) = CByte((transfer_size >> 24) And 255)
        setup_data(8) = CByte(page_offset And 255)
        setup_data(9) = CByte((page_offset >> 8) And 255)
        setup_data(10) = CByte(MyFlashDevice.PAGE_SIZE And 255)
        setup_data(11) = CByte((MyFlashDevice.PAGE_SIZE >> 8) And 255)
        setup_data(12) = CByte(MyFlashDevice.PAGE_EXT And 255)
        setup_data(13) = CByte((MyFlashDevice.PAGE_EXT >> 8) And 255)
        setup_data(14) = CByte(nand_layout.Layout_Main And 255)
        setup_data(15) = CByte((nand_layout.Layout_Main >> 8) And 255)
        setup_data(16) = CByte(nand_layout.Layout_Spare And 255)
        setup_data(17) = CByte((nand_layout.Layout_Spare >> 8) And 255)
        setup_data(18) = CByte(MyFlashDevice.PAGE_COUNT And 255)
        setup_data(19) = CByte((MyFlashDevice.PAGE_COUNT >> 8) And 255)
        setup_data(20) = TX_NAND_ADDRSIZE
        setup_data(21) = memory_area 'Area (0=main,1=spare,2=all), note: all ignores layout settings
        Return setup_data
    End Function



End Class
