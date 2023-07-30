'COPYRIGHT EMBEDDED COMPUTERS LLC 2019 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB PRODUCTS
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: This is the main module that is loaded first.

Imports FlashcatUSB.ECC_LIB
Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB
Imports FlashcatUSB.USB.HostClient

Public Class PARALLEL_NOR_NAND : Implements MemoryDeviceUSB
    Private FCUSB As FCUSB_DEVICE
    Public Property MyFlashDevice As Device
    Public Property MyFlashStatus As DeviceStatus = DeviceStatus.NotDetected
    Public Property ONFI As NAND_ONFI 'Contains ONFI table information (NAND)
    Public Property CFI As NOR_CFI 'Contains CFI table information (NOR)

    Public Property MyAdapter As MEM_PROTOCOL 'This is the kind of socket adapter connected and the mode it is in

    Public FLASH_IDENT As FlashDetectResult

    Public Event PrintConsole(ByVal msg As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(ByVal percent As Integer) Implements MemoryDeviceUSB.SetProgress

    Public Property MULTI_DIE As Boolean = False
    Public Property DIE_SELECTED As Integer = 0
    Public Property ECC_LAST_RESULT As ECC_DECODE_RESULT = ECC_DECODE_RESULT.NoErrors

    Sub New(parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        MyFlashDevice = Nothing
        Me.ONFI = Nothing
        Me.CFI = Nothing
        If Not EXPIO_SETUP_USB(MEM_PROTOCOL.SETUP) Then
            RaiseEvent PrintConsole("Parallel I/O failed to initialize")
            MyFlashStatus = DeviceStatus.ExtIoNotConnected
            Return False
        Else
            RaiseEvent PrintConsole(RM.GetString("io_mode_initalized"))
        End If
        If DetectFlashDevice() Then
            Dim chip_id_str As String = Hex(FLASH_IDENT.MFG).PadLeft(2, "0") & Hex(FLASH_IDENT.PART).PadLeft(8, "0")
            RaiseEvent PrintConsole(String.Format(RM.GetString("ext_connected_chipid"), chip_id_str))
            If (FCUSB.HWBOARD = FCUSB_BOARD.XPORT_PCB1) Or (FCUSB.HWBOARD = FCUSB_BOARD.XPORT_PCB2) Then
                If (FLASH_IDENT.ID1 >> 8 = 255) Then FLASH_IDENT.ID1 = (FLASH_IDENT.ID1 And 255) 'XPORT IO is a little different than the EXTIO for X8 devices
            End If
            Dim device_matches() As Device
            If (MyAdapter = MEM_PROTOCOL.NAND_X8_ASYNC) Then
                device_matches = FlashDatabase.FindDevices(FLASH_IDENT.MFG, FLASH_IDENT.ID1, FLASH_IDENT.ID2, MemoryType.NAND)
            Else
                If MyAdapter = MEM_PROTOCOL.NOR_X8 Then
                    device_matches = FlashDatabase.FindDevices(FLASH_IDENT.MFG, FLASH_IDENT.ID1, 0, MemoryType.PARALLEL_NOR)
                ElseIf MyAdapter = MEM_PROTOCOL.FWH Then
                    device_matches = FlashDatabase.FindDevices(FLASH_IDENT.MFG, FLASH_IDENT.ID1, 0, MemoryType.FWH_NOR)
                Else
                    device_matches = FlashDatabase.FindDevices(FLASH_IDENT.MFG, FLASH_IDENT.ID1, FLASH_IDENT.ID2, MemoryType.PARALLEL_NOR)
                End If
            End If
            If ((MyAdapter = MEM_PROTOCOL.NAND_X8_ASYNC) Or (MyAdapter = MEM_PROTOCOL.NAND_X16_ASYNC)) Then
                Me.ONFI = New NAND_ONFI(NAND_GetONFITable())
                If Me.ONFI Is Nothing Then
                    RaiseEvent PrintConsole("NAND device failed to load ONFI table")
                End If
            Else
                Me.CFI = New NOR_CFI(EXPIO_NOR_GetCFI())
            End If
            If (device_matches IsNot Nothing AndAlso device_matches.Count > 0) Then
                MyFlashDevice_SelectBest(device_matches)
                RaiseEvent PrintConsole(String.Format(RM.GetString("flash_detected"), MyFlashDevice.NAME, Format(MyFlashDevice.FLASH_SIZE, "#,###")))
                RaiseEvent PrintConsole(RM.GetString("ext_prog_mode"))
                PrintDeviceInterface()
                If (MyFlashDevice.FLASH_TYPE = MemoryType.PARALLEL_NOR) Then
                    Dim NOR_FLASH As P_NOR = DirectCast(MyFlashDevice, P_NOR)
                    If MySettings.MUTLI_NOR Then
                        RaiseEvent PrintConsole("Multi-chip select feature is enabled")
                        NOR_FLASH.AVAILABLE_SIZE = (NOR_FLASH.FLASH_SIZE * 2)
                        Me.MULTI_DIE = True
                        Me.DIE_SELECTED = 0
                    Else
                        NOR_FLASH.AVAILABLE_SIZE = NOR_FLASH.FLASH_SIZE
                        Me.MULTI_DIE = False
                    End If
                    If NOR_FLASH.RESET_ENABLED Then Me.ResetDevice() 'This is needed for some devices
                    EXPIO_SETUP_WRITEDELAY(NOR_FLASH.HARDWARE_DELAY)
                    EXPIO_SETUP_DELAY(NOR_FLASH.DELAY_MODE)
                    Select Case NOR_FLASH.WriteMode
                        Case MFP_PRG.Standard
                            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Standard)
                        Case MFP_PRG.IntelSharp
                            Me.CURRENT_SECTOR_ERASE = E_EXPIO_SECTOR.Intel
                            Me.CURRENT_CHIP_ERASE = E_EXPIO_CHIPERASE.Intel
                            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Intel)
                        Case MFP_PRG.BypassMode 'Writes 64 bytes using ByPass sequence
                            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Bypass)
                        Case MFP_PRG.PageMode 'Writes an entire page of data (128 bytes etc.)
                            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Page)
                        Case MFP_PRG.Buffer1 'Writes to a buffer that is than auto-programmed
                            Me.CURRENT_SECTOR_ERASE = E_EXPIO_SECTOR.Intel
                            Me.CURRENT_CHIP_ERASE = E_EXPIO_CHIPERASE.Intel
                            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Buffer_1)
                        Case MFP_PRG.Buffer2
                            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Buffer_2)
                    End Select
                    If (NOR_FLASH.AVAILABLE_SIZE >= Mb512) AndAlso Me.CURRENT_BUS_WIDTH = E_BUS_WIDTH.X16 Then
                        EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.Parallel_X16_26BIT) 'Ignored by PCB 2.x
                    End If
                    WaitForReady()
                ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.NAND Then
                    Dim page_info As String = String.Format(RM.GetString("ext_page_size"), Strings.Format(MyFlashDevice.PAGE_SIZE, "#,###"), DirectCast(MyFlashDevice, P_NAND).EXT_PAGE_SIZE)
                    RaiseEvent PrintConsole(page_info)
                    RaiseEvent PrintConsole("Block size: " & Strings.Format((DirectCast(MyFlashDevice, P_NAND).BLOCK_SIZE), "#,###") & " bytes")
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
                    If ((FCUSB.HWBOARD = FCUSB_BOARD.XPORT_PCB1) Or (FCUSB.HWBOARD = FCUSB_BOARD.XPORT_PCB2)) Then
                        If (Not nand_mem.IFACE = VCC_IF.X8_3V) Then
                            RaiseEvent PrintConsole("This NAND device is not compatible with this programmer")
                            MyFlashStatus = DeviceStatus.NotCompatible
                            Return False
                        End If
                    End If
                    If nand_mem.IFACE = VCC_IF.X16_3V Or nand_mem.IFACE = VCC_IF.X16_1V8 Then
                        RaiseEvent PrintConsole("NAND interface changed to X16")
                        EXPIO_SETUP_USB(MEM_PROTOCOL.NAND_X16_ASYNC)
                        Me.MyAdapter = MEM_PROTOCOL.NAND_X16_ASYNC
                    End If
                    If (nand_mem.FLASH_SIZE > Gb004) Then 'Remove this check if you wish
                        If (FCUSB.HWBOARD = FCUSB_BOARD.XPORT_PCB1) Or (FCUSB.HWBOARD = FCUSB_BOARD.XPORT_PCB2) Then
                            RaiseEvent PrintConsole("XPORT is not compatible with NAND Flash larger than 4Gbit")
                            RaiseEvent PrintConsole("Please upgrade to Mach1")
                            MyFlashStatus = DeviceStatus.NotCompatible
                            Return False
                        End If
                    End If
                    NAND_SetupHandlers()
                    FCUSB.NAND_IF.CreateMap(nand_mem.FLASH_SIZE, nand_mem.PAGE_SIZE, nand_mem.EXT_PAGE_SIZE, nand_mem.BLOCK_SIZE)
                    FCUSB.NAND_IF.EnableBlockManager() 'If enabled 
                    FCUSB.NAND_IF.ProcessMap()
                ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.FWH_NOR Then
                    Dim FWH_FLASH As FWH = DirectCast(MyFlashDevice, FWH)
                    FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_MODE_WRITE, Nothing, FWH_FLASH.ERASE_CMD)
                End If
                EXPIO_PrintCurrentWriteMode()
                Utilities.Sleep(10) 'We need to wait here (device is being configured)
                MyFlashStatus = DeviceStatus.Supported
                Return True
            Else
                RaiseEvent PrintConsole(RM.GetString("unknown_device_email"))
                MyFlashDevice = Nothing
                MyFlashStatus = DeviceStatus.NotSupported
            End If
        Else
            GUI.PrintConsole(RM.GetString("ext_not_detected"))
            MyFlashStatus = DeviceStatus.NotDetected
        End If
        Return False
    End Function

    Private Sub MyFlashDevice_SelectBest(device_matches() As Device)
        If device_matches.Length = 1 Then
            MyFlashDevice = device_matches(0)
            Exit Sub
        End If
        If ((MyAdapter = MEM_PROTOCOL.NAND_X8_ASYNC) Or (MyAdapter = MEM_PROTOCOL.NAND_X16_ASYNC)) Then
            If Me.ONFI.IS_VALID Then
                For i = 0 To device_matches.Count - 1
                    Dim PART As String = device_matches(i).NAME.ToUpper
                    If PART.IndexOf(" ") > 0 Then PART = PART.Substring(PART.IndexOf(" ") + 1)
                    If PART.Equals(Me.ONFI.DEVICE_MODEL) Then
                        MyFlashDevice = device_matches(i) : Exit Sub
                    End If
                Next
            End If
        Else 'NOR device 
            If (device_matches(0).MFG_CODE = &H1 AndAlso device_matches(0).ID1 = &HAD) Then 'AM29F016x (we need to figure out which one)
                If Not CFI.IS_VALID Then
                    MyFlashDevice = device_matches(0) : Exit Sub 'AM29F016B (Uses Legacy programming)
                Else
                    MyFlashDevice = device_matches(1) : Exit Sub 'AM29F016D (Uses Bypass programming)
                End If
            Else
                If Me.CFI.IS_VALID Then
                    Dim flash_dev As New List(Of Device)
                    For i = 0 To device_matches.Count - 1
                        If CUInt(device_matches(i).FLASH_SIZE) = Me.CFI.DEVICE_SIZE Then
                            flash_dev.Add(device_matches(i))
                        End If
                    Next
                    If flash_dev.Count = 1 Then
                        MyFlashDevice = flash_dev(0) : Exit Sub
                    Else
                        For i = 0 To flash_dev.Count - 1
                            If flash_dev(i).PAGE_SIZE = Me.CFI.WRITE_BUFFER_SIZE Then
                                MyFlashDevice = flash_dev(i) : Exit Sub
                            End If
                        Next
                    End If
                End If
            End If
        End If
        If MyFlashDevice Is Nothing Then MyFlashDevice = device_matches(0)
    End Sub

#Region "Public Interface"

    Friend ReadOnly Property DeviceName() As String Implements MemoryDeviceUSB.DeviceName
        Get
            Select Case MyFlashStatus
                Case DeviceStatus.Supported
                    Return MyFlashDevice.NAME
                Case DeviceStatus.NotSupported
                    Return Hex(MyFlashDevice.MFG_CODE).PadLeft(2, CChar("0")) & " " & Hex(MyFlashDevice.ID1).PadLeft(4, CChar("0"))
                Case Else
                    Return RM.GetString("no_flash_detected")
            End Select
        End Get
    End Property

    Friend ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
        Get
            If MyFlashDevice.FLASH_TYPE = MemoryType.NAND Then
                Dim d As P_NAND = DirectCast(MyFlashDevice, P_NAND)
                Dim available_pages As Long = FCUSB.NAND_IF.MAPPED_PAGES
                If MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Combined Then
                    Return (available_pages * (d.PAGE_SIZE + d.EXT_PAGE_SIZE))
                Else
                    Return (available_pages * d.PAGE_SIZE)
                End If
            ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                Dim NOR_FLASH As P_NOR = DirectCast(MyFlashDevice, P_NOR)
                Return NOR_FLASH.AVAILABLE_SIZE
            ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.OTP_EPROM Then
                Dim NOR_FLASH As OTP_EPROM = DirectCast(MyFlashDevice, OTP_EPROM)
                Return NOR_FLASH.FLASH_SIZE
            ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.FWH_NOR Then
                Dim fwh_device As FWH = DirectCast(MyFlashDevice, FWH)
                Return fwh_device.FLASH_SIZE
            Else
                Return Me.MyFlashDevice.FLASH_SIZE
            End If
        End Get
    End Property

    Public Function ReadData(logical_address As Long, data_count As UInt32, Optional memory_area As FlashArea = FlashArea.Main) As Byte() Implements MemoryDeviceUSB.ReadData
        If MyFlashDevice.FLASH_TYPE = MemoryType.NAND Then
            Dim nand_dev As P_NAND = DirectCast(MyFlashDevice, P_NAND)
            Dim page_addr As Long  'This is the page address
            Dim page_offset As UInt16 'this is the start offset within the page
            Dim page_size As UInt32
            If (memory_area = FlashArea.Main) Then
                page_addr = Math.Floor(logical_address / CLng(MyFlashDevice.PAGE_SIZE))
                page_size = nand_dev.PAGE_SIZE
                page_offset = logical_address - (page_addr * CLng(MyFlashDevice.PAGE_SIZE))
            ElseIf (memory_area = FlashArea.OOB) Then
                page_addr = Math.Floor(logical_address / nand_dev.EXT_PAGE_SIZE)
                page_offset = logical_address - (page_addr * nand_dev.EXT_PAGE_SIZE)
                page_size = nand_dev.EXT_PAGE_SIZE
            ElseIf (memory_area = FlashArea.All) Then   'we need to adjust large address to logical address
                Dim full_page_size As Long = (MyFlashDevice.PAGE_SIZE + nand_dev.EXT_PAGE_SIZE)
                page_addr = Math.Floor(logical_address / full_page_size)
                page_offset = logical_address - (page_addr * full_page_size)
                page_size = nand_dev.PAGE_SIZE + nand_dev.EXT_PAGE_SIZE
            End If
            'The following code is so we can read past invalid blocks
            Dim pages_per_block As UInt32 = (nand_dev.BLOCK_SIZE / nand_dev.PAGE_SIZE)
            Dim data_out(data_count - 1) As Byte
            Dim data_ptr As Integer = 0
            Do While (data_count > 0)
                Dim pages_left As UInt32 = (pages_per_block - (page_addr Mod pages_per_block))
                Dim bytes_left_in_block As UInt32 = (pages_left * page_size) - page_offset
                Dim packet_size As UInt32 = Math.Min(bytes_left_in_block, data_count)
                page_addr = FCUSB.NAND_IF.GetPageMapping(page_addr)
                Dim data() As Byte = ReadBulk_NAND(page_addr, page_offset, packet_size, memory_area)
                If data Is Nothing Then Return Nothing
                Array.Copy(data, 0, data_out, data_ptr, data.Length)
                data_ptr += packet_size
                data_count -= packet_size
                page_addr += Math.Ceiling(bytes_left_in_block / page_size)
                page_offset = 0
            Loop
            Return data_out
        ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.FWH_NOR Then
            Return ReadData_FWH(logical_address, data_count)
        ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.OTP_EPROM Then
            Return EPROM_ReadData(logical_address, data_count)
        Else 'NOR memory
            If Me.MULTI_DIE Then
                Dim data_to_read(data_count - 1) As Byte
                Dim buffer_size As UInt32 = 0
                Dim array_ptr As UInt32 = 0
                Do Until data_count = 0
                    Dim die_address As UInt32 = GetAddressForMultiDie(logical_address, data_count, buffer_size)
                    Dim die_data() As Byte = ReadBulk_NOR(die_address, buffer_size)
                    Array.Copy(die_data, 0, data_to_read, array_ptr, die_data.Length) : array_ptr += buffer_size
                Loop
                Return data_to_read
            Else
                Return ReadBulk_NOR(CUInt(logical_address), data_count)
            End If
        End If
    End Function
    'Returns the die address from the flash_offset (and increases by the buffersize) and also selects the correct die
    Private Function GetAddressForMultiDie(ByRef flash_offset As UInt32, ByRef count As UInt32, ByRef buffer_size As UInt32) As UInt32
        Dim die_count As Integer = 2 'Multi die only supports 2 (for now)
        Dim die_size As UInt32 = MyFlashDevice.FLASH_SIZE
        Dim die_id As Byte = CByte(Math.Floor(flash_offset / die_size))
        Dim die_addr As UInt32 = (flash_offset Mod die_size)
        buffer_size = Math.Min(count, (die_size - die_addr))
        If (die_id <> Me.DIE_SELECTED) Then
            Dim w_data As UInt32 = 2
            If (die_id > 0) Then w_data = ((MySettings.MULTI_CE + 17) << 16) + 2
            FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_MODE_ADDRESS, Nothing, w_data)
            Utilities.Sleep(10)
            Me.DIE_SELECTED = die_id
        End If
        count -= buffer_size
        flash_offset += buffer_size
        Return die_addr
    End Function

    Public Function SectorErase(sector_index As UInt32, Optional memory_area As FlashArea = FlashArea.Main) As Boolean Implements MemoryDeviceUSB.SectorErase
        If Not MyFlashDevice.ERASE_REQUIRED Then Return True
        If MyFlashDevice.FLASH_TYPE = MemoryType.NAND Then
            Dim pages_per_block As UInt32 = (DirectCast(MyFlashDevice, P_NAND).BLOCK_SIZE / MyFlashDevice.PAGE_SIZE)
            Dim page_addr As UInt32 = (pages_per_block * sector_index)
            Dim local_page_addr As UInt32 = FCUSB.NAND_IF.GetPageMapping(page_addr)
            Return FCUSB.NAND_IF.ERASEBLOCK(local_page_addr, memory_area, MySettings.NAND_Preserve)
        ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.FWH_NOR Then
            Dim fwh_device As FWH = DirectCast(MyFlashDevice, FWH)
            Dim Result As Boolean = False
            Dim Logical_Address As UInt32 = 0
            If (sector_index > 0) Then
                For i As UInt32 = 0 To sector_index - 1
                    Dim s_size As UInt32 = SectorSize(i)
                    Logical_Address += s_size
                Next
            End If
            Result = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_SECTORERASE, Nothing, Logical_Address)
            If Not Result Then Return False
            Utilities.Sleep(50)
            Dim blank_result As Boolean = BlankCheck(Logical_Address)
            Return blank_result
        Else
            Dim nor_device As P_NOR = DirectCast(MyFlashDevice, P_NOR)
            Try
                If sector_index = 0 AndAlso SectorSize(0) = MyFlashDevice.FLASH_SIZE Then
                    Return EraseDevice() 'Single sector, must do a full chip erase instead
                Else
                    Dim Logical_Address As UInt32 = 0
                    If (sector_index > 0) Then
                        For i As UInt32 = 0 To sector_index - 1
                            Dim s_size As UInt32 = SectorSize(i)
                            Logical_Address += s_size
                        Next
                    End If
                    EXPIO_VPP_ENABLE() 'Enables +12V for supported devices
                    Dim sector_start_addr As UInt32 = Logical_Address
                    If Me.MULTI_DIE Then sector_start_addr = GetAddressForMultiDie(Logical_Address, 0, 0)
                    EXPIO_EraseSector(sector_start_addr)
                    EXPIO_VPP_DISABLE()
                    If nor_device.DELAY_MODE = MFP_DELAY.SR1 Or nor_device.DELAY_MODE = MFP_DELAY.SR2 Then
                        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WAIT) 'Calls the assigned WAIT function (uS, mS, SR, DQ7)
                        FCUSB.USB_WaitForComplete() 'Checks for WAIT flag to clear
                    Else
                        Utilities.Sleep(nor_device.ERASE_DELAY) 'Some flashes (like MX29LV040C) need more than 100ms delay
                    End If
                    Dim blank_result As Boolean = False
                    Dim timeout As UInt32 = 0
                    Do Until blank_result
                        If nor_device.RESET_ENABLED Then ResetDevice()
                        blank_result = BlankCheck(Logical_Address)
                        timeout += 1
                        If (timeout = 10) Then Return False
                        If Not blank_result Then Utilities.Sleep(100)
                    Loop
                    Return True
                End If
            Catch ex As Exception
                Return False
            End Try
        End If
    End Function

    Public Function WriteData(logical_address As Long, data_to_write() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        If (MyFlashDevice.FLASH_TYPE = MemoryType.NAND) Then
            Dim page_addr As UInt32 = GetNandPageAddress(MyFlashDevice, logical_address, Params.Memory_Area)
            page_addr = FCUSB.NAND_IF.GetPageMapping(page_addr) 'Adjusts the page to point to a valid page
            Dim result As Boolean = FCUSB.NAND_IF.WRITEPAGE(page_addr, data_to_write, Params.Memory_Area) 'We will write the whole block instead
            FCUSB.USB_WaitForComplete()
            Return result
        ElseIf (MyFlashDevice.FLASH_TYPE = MemoryType.OTP_EPROM) Then
            Return EPROM_WriteData(logical_address, data_to_write, Params)
        ElseIf (MyFlashDevice.FLASH_TYPE = MemoryType.FWH_NOR) Then
            Return WriteData_FWH(logical_address, data_to_write, Params)
        Else
            Dim nor_device As P_NOR = DirectCast(MyFlashDevice, P_NOR)
            Try
                EXPIO_VPP_ENABLE()
                Dim ReturnValue As Boolean
                Dim DataToWrite As UInt32 = data_to_write.Length
                Dim PacketSize As UInt32 = 8192 'Possibly /2 for IsFlashX8Mode
                Dim Loops As Integer = CInt(Math.Ceiling(DataToWrite / PacketSize)) 'Calcuates iterations
                For i As Integer = 0 To Loops - 1
                    Dim BufferSize As Integer = DataToWrite
                    If (BufferSize > PacketSize) Then BufferSize = PacketSize
                    Dim data(BufferSize - 1) As Byte
                    Array.Copy(data_to_write, (i * PacketSize), data, 0, data.Length)
                    If Me.MULTI_DIE Then
                        Dim die_address As UInt32 = GetAddressForMultiDie(logical_address, 0, 0)
                        ReturnValue = WriteBulk_NOR(die_address, data)
                    Else
                        ReturnValue = WriteBulk_NOR(logical_address, data)
                    End If
                    If (Not ReturnValue) Then Return False
                    If FCUSB.HWBOARD = FCUSB_BOARD.Mach1 AndAlso nor_device.WriteMode = MFP_PRG.BypassMode Then
                        Utilities.Sleep(300) 'Board is too fast! We need a delay between writes (i.e. AM29LV160B)
                    End If
                    FCUSB.USB_WaitForComplete()
                    logical_address += data.Length
                    DataToWrite -= data.Length
                Next
                If nor_device.DELAY_MODE = MFP_DELAY.DQ7 Or nor_device.DELAY_MODE = MFP_DELAY.SR1 Or nor_device.DELAY_MODE = MFP_DELAY.SR2 Then
                    EXPIO_WAIT()
                Else
                    Utilities.Sleep(DirectCast(MyFlashDevice, P_NOR).SOFTWARE_DELAY)
                End If
            Catch ex As Exception
            Finally
                EXPIO_VPP_DISABLE()
                If nor_device.RESET_ENABLED Then ResetDevice()
            End Try
            Return True
        End If
        Return False
    End Function

    Private Function WriteData_FWH(ByVal logical_address As UInt32, ByVal data_to_write() As Byte, ByRef Params As WriteParameters) As Boolean
        Try
            Dim PacketSize As UInt32 = 4096
            Dim BytesWritten As UInt32 = 0
            Dim DataToWrite As UInt32 = data_to_write.Length
            Dim Loops As Integer = CInt(Math.Ceiling(DataToWrite / PacketSize)) 'Calcuates iterations
            For i As Integer = 0 To Loops - 1
                If Params IsNot Nothing Then If Params.AbortOperation Then Return False
                Dim BufferSize As Integer = DataToWrite
                If (BufferSize > PacketSize) Then BufferSize = PacketSize
                Dim data(BufferSize - 1) As Byte
                Array.Copy(data_to_write, (i * PacketSize), data, 0, data.Length)
                Dim ReturnValue As Boolean = WriteBulk_NOR(logical_address, data)
                If (Not ReturnValue) Then Return False
                FCUSB.USB_WaitForComplete()
                logical_address += data.Length
                DataToWrite -= data.Length
                BytesWritten += data.Length
                If Params IsNot Nothing AndAlso (Loops > 1) Then
                    Dim UpdatedTotal As UInt32 = Params.BytesWritten + BytesWritten
                    Dim percent As Single = CSng(CSng((UpdatedTotal) / CSng(Params.BytesTotal)) * 100)
                    If Params.Status.UpdateSpeed IsNot Nothing Then
                        Dim speed_str As String = Format(Math.Round(UpdatedTotal / (Params.Timer.ElapsedMilliseconds / 1000)), "#,###") & " B/s"
                        Params.Status.UpdateSpeed.DynamicInvoke(speed_str)
                    End If
                    If Params.Status.UpdatePercent IsNot Nothing Then Params.Status.UpdatePercent.DynamicInvoke(CInt(percent))
                End If
            Next
        Catch ex As Exception
        Finally
            'If Params.Status.UpdateSpeed IsNot Nothing Then Params.Status.UpdateSpeed.DynamicInvoke("")
            'If Params.Status.UpdatePercent IsNot Nothing Then Params.Status.UpdatePercent.DynamicInvoke(0)
        End Try
        Return True
    End Function

    Private Function ReadData_FWH(ByVal logical_address As UInt32, ByVal data_count As UInt32) As Byte()
        Dim data_out(data_count - 1) As Byte
        Dim ptr As Integer = 0
        Dim bytes_left As Integer = data_count
        Dim PacketSize As UInt32 = 2048
        While (bytes_left > 0)
            Dim BufferSize As Integer = bytes_left
            If (BufferSize > PacketSize) Then BufferSize = PacketSize
            Dim data() As Byte = ReadBulk_NOR(logical_address, BufferSize)
            If data Is Nothing Then Return Nothing
            Array.Copy(data, 0, data_out, ptr, BufferSize)
            logical_address += data.Length
            bytes_left -= data.Length
            ptr += data.Length
        End While
        Return data_out
    End Function

    Public Sub WaitForReady() Implements MemoryDeviceUSB.WaitUntilReady
        If MyFlashDevice.FLASH_TYPE = MemoryType.NAND Then
            FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WAIT)
            FCUSB.USB_WaitForComplete() 'Checks for WAIT flag to clear
        ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
            Utilities.Sleep(100) 'Some flash devices have registers, some rely on delays
        ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.FWH_NOR Then
            Utilities.Sleep(100)
        ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.OTP_EPROM Then
            Utilities.Sleep(100)
        End If
    End Sub

    Public Function SectorFind(ByVal sector_index As UInt32, Optional ByVal memory_area As FlashArea = FlashArea.Main) As Long Implements MemoryDeviceUSB.SectorFind
        Dim base_addr As UInt32 = 0
        If sector_index > 0 Then
            For i As UInt32 = 0 To sector_index - 1
                base_addr += Me.SectorSize(i, memory_area)
            Next
        End If
        Return base_addr
    End Function

    Public Function SectorWrite(ByVal sector_index As UInt32, ByVal data() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.SectorWrite
        Dim Addr32 As UInteger = Me.SectorFind(sector_index, Params.Memory_Area)
        Return WriteData(Addr32, data, Params)
    End Function

    Public Function SectorCount() As UInt32 Implements MemoryDeviceUSB.SectorCount
        If MySettings.MUTLI_NOR Then
            Return (MyFlashDevice.Sector_Count * 2)
        Else
            Return MyFlashDevice.Sector_Count
        End If
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        Try
            If MyFlashDevice.FLASH_TYPE = MemoryType.NAND Then
                Dim Result As Boolean = FCUSB.NAND_IF.EraseChip()
                If Result Then
                    RaiseEvent PrintConsole(RM.GetString("nand_erase_successful"))
                Else
                    RaiseEvent PrintConsole(RM.GetString("nand_erase_failed"))
                End If
            ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.OTP_EPROM Then
                RaiseEvent PrintConsole("EPROM devices are not able to be erased")
            ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.FWH_NOR Then
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CHIPERASE)
                Utilities.Sleep(200) 'Perform blank check
                For i = 0 To 179 '3 minutes
                    If BlankCheck(0) Then Return True
                    Utilities.Sleep(900)
                Next
                Return False 'Timeout (device erase failed)
            Else
                Try
                    EXPIO_VPP_ENABLE()
                    Dim wm As MFP_PRG = DirectCast(MyFlashDevice, P_NOR).WriteMode
                    If (wm = MFP_PRG.IntelSharp Or wm = MFP_PRG.Buffer1) Then
                        Dim BlockCount As Integer = DirectCast(MyFlashDevice, P_NOR).Sector_Count
                        RaiseEvent SetProgress(0)
                        For i = 0 To BlockCount - 1
                            If (Not SectorErase(i, 0)) Then
                                RaiseEvent SetProgress(0)
                                Return False 'Error erasing sector
                            Else
                                Dim percent As Single = (i / BlockCount) * 100
                                RaiseEvent SetProgress(Math.Floor(percent))
                            End If
                        Next
                        RaiseEvent SetProgress(0)
                        Return True 'Device successfully erased
                    Else
                        EXPIO_EraseChip()
                        Utilities.Sleep(200) 'Perform blank check
                        For i = 0 To 179 '3 minutes
                            If BlankCheck(0) Then Return True
                            Utilities.Sleep(900)
                        Next
                        Return False 'Timeout (device erase failed)
                    End If
                Catch ex As Exception
                Finally
                    EXPIO_VPP_DISABLE()
                End Try
            End If
        Catch ex As Exception
        Finally
            If MyFlashDevice.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                Dim nor_device As P_NOR = DirectCast(MyFlashDevice, P_NOR)
                If nor_device.RESET_ENABLED Then ResetDevice() 'Lets do a chip reset too
            End If
        End Try
        Return False
    End Function

    Friend ReadOnly Property SectorSize(ByVal sector As UInt32, Optional ByVal memory_area As FlashArea = FlashArea.Main) As UInt32 Implements MemoryDeviceUSB.SectorSize
        Get
            If Not MyFlashStatus = USB.DeviceStatus.Supported Then Return 0
            If FCUSB.EXT_IF.MyFlashDevice.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                If MySettings.MUTLI_NOR Then sector = ((MyFlashDevice.Sector_Count - 1) And sector)
                Return DirectCast(MyFlashDevice, P_NOR).GetSectorSize(sector)
            ElseIf FCUSB.EXT_IF.MyFlashDevice.FLASH_TYPE = MemoryType.OTP_EPROM Then
                Return 8192 'Program 8KB at a time
            ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.FWH_NOR Then
                Return DirectCast(MyFlashDevice, FWH).SECTOR_SIZE
            Else
                Dim nand_dev As P_NAND = DirectCast(MyFlashDevice, P_NAND)
                Dim page_count As UInt32 = (nand_dev.BLOCK_SIZE / nand_dev.PAGE_SIZE)
                Select Case memory_area
                    Case FlashArea.Main
                        Return (page_count * nand_dev.PAGE_SIZE)
                    Case FlashArea.OOB
                        Return (page_count * nand_dev.EXT_PAGE_SIZE)
                    Case FlashArea.All
                        Return (page_count * (nand_dev.PAGE_SIZE + nand_dev.EXT_PAGE_SIZE))
                End Select
                Return 0
            End If
        End Get
    End Property

#End Region

#Region "NAND IF"

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

    Private Sub NAND_PrintConsole(ByVal msg As String)
        RaiseEvent PrintConsole(msg)
    End Sub

    Private Sub NAND_SetProgress(ByVal percent As Integer)
        RaiseEvent SetProgress(percent)
    End Sub

    Public Sub NAND_ReadPages(page_addr As UInt32, page_offset As UInt16, data_count As UInt32, memory_area As FlashArea, ByRef data() As Byte)
        data = ReadBulk_NAND(page_addr, page_offset, data_count, memory_area)
    End Sub

    Private Sub NAND_WritePages(page_addr As UInt32, main() As Byte, oob() As Byte, memory_area As FlashArea, ByRef write_result As Boolean)
        write_result = WriteBulk_NAND(page_addr, main, oob, memory_area)
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

    Private Function NAND_GetSR() As Byte
        Dim result_data(0) As Byte
        Dim result As Boolean = FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_NAND_SR, result_data)
        Return result_data(0) 'E0 11100000
    End Function

    Private Function NAND_GetONFITable() As Byte()
        Dim onfi(255) As Byte
        If FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_NAND_ONFI, Nothing, onfi, 0) Then
            Return onfi
        End If
        Return Nothing
    End Function

#End Region

#Region "EPROM / OTP"

    Public Function EPROM_Init() As Boolean
        MyFlashDevice = Nothing
        If EPROM_Detect() Then
            Dim o As OTP_EPROM = MyFlashDevice
            If o.IFACE = VCC_IF.X16_5V_12VPP Then
                EXPIO_SETUP_USB(MEM_PROTOCOL.EPROM_X16)
                EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.EPROM_X16)
                Me.MyAdapter = MEM_PROTOCOL.EPROM_X16
                RaiseEvent PrintConsole("Device mode set to EPROM (16-bit)")
            ElseIf o.IFACE = VCC_IF.X8_5V_12VPP Then
                EXPIO_SETUP_USB(MEM_PROTOCOL.EPROM_X8)
                Me.MyAdapter = MEM_PROTOCOL.EPROM_X8
                EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.EPROM_X8)
                Me.MyAdapter = MEM_PROTOCOL.EPROM_X8
                RaiseEvent PrintConsole("Device mode set to EPROM (8-bit)")
            End If
        Else
            RaiseEvent PrintConsole("Unable to automatically detect EPROM/OTP device")
            Return False
        End If
        RaiseEvent PrintConsole("EPROM successfully detected!")
        RaiseEvent PrintConsole("EPROM device: " & MyFlashDevice.NAME & ", size: " & Format(MyFlashDevice.FLASH_SIZE, "#,###") & " bytes")
        EXPIO_SETUP_DELAY(MFP_DELAY.uS)
        EXPIO_SETUP_WRITEDELAY(DirectCast(MyFlashDevice, OTP_EPROM).HARDWARE_DELAY)
        DirectCast(MyFlashDevice, OTP_EPROM).IS_BLANK = True
        'DirectCast(MyFlashDevice, OTP_EPROM).IS_BLANK = EPROM_BlankCheck()
        SetStatus("EPROM mode ready for operation")
        MyFlashStatus = DeviceStatus.Supported
        Return True
    End Function

    Public Function EPROM_Detect() As Boolean
        EXPIO_SETUP_USB(MEM_PROTOCOL.EPROM_X8)
        Utilities.Sleep(200)
        Dim IDENT_DATA() As Byte
        IDENT_DATA = EPROM_ReadEletronicID_1()
        MyFlashDevice = FlashDatabase.FindDevice(IDENT_DATA(0), IDENT_DATA(1), 0, MemoryType.OTP_EPROM)
        If MyFlashDevice Is Nothing Then IDENT_DATA = EPROM_ReadEletronicID_2()
        MyFlashDevice = FlashDatabase.FindDevice(IDENT_DATA(0), IDENT_DATA(1), 0, MemoryType.OTP_EPROM)
        If MyFlashDevice IsNot Nothing Then Return True 'Detected!
        Return False
    End Function

    Private Function EPROM_ReadEletronicID_1() As Byte()
        Dim IDENT_DATA(1) As Byte
        HardwareControl(EXPIO_CTRL.VPP_ENABLE)
        HardwareControl(EXPIO_CTRL.OE_LOW)
        HardwareControl(EXPIO_CTRL.WE_LOW)
        HardwareControl(EXPIO_CTRL.VPP_12V)
        HardwareControl(EXPIO_CTRL.RELAY_ON)
        Utilities.Sleep(150)
        FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_READDATA, GetSetupPacket_NOR(0, 2, 0), IDENT_DATA, 0)
        HardwareControl(EXPIO_CTRL.RELAY_OFF)
        HardwareControl(EXPIO_CTRL.VPP_0V)
        HardwareControl(EXPIO_CTRL.WE_HIGH)
        HardwareControl(EXPIO_CTRL.VPP_DISABLE)
        If IDENT_DATA(0) = 0 Or IDENT_DATA(1) = 0 Then
        ElseIf IDENT_DATA(0) = 255 Or IDENT_DATA(1) = 255 Then
        Else
            RaiseEvent PrintConsole("EPROM IDENT CODE returned MFG: 0x" & Hex(IDENT_DATA(0)) & " and PART 0x" & Hex(IDENT_DATA(1)))
        End If
        Return IDENT_DATA
    End Function

    Private Function EPROM_ReadEletronicID_2() As Byte()
        Dim IDENT_DATA(1) As Byte
        HardwareControl(EXPIO_CTRL.VPP_DISABLE)
        HardwareControl(EXPIO_CTRL.OE_LOW)
        HardwareControl(EXPIO_CTRL.WE_LOW)
        HardwareControl(EXPIO_CTRL.VPP_12V)
        HardwareControl(EXPIO_CTRL.RELAY_ON)
        Utilities.Sleep(150)
        FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_READDATA, GetSetupPacket_NOR(0, 2, 0), IDENT_DATA, 0)
        HardwareControl(EXPIO_CTRL.RELAY_OFF)
        HardwareControl(EXPIO_CTRL.VPP_0V)
        HardwareControl(EXPIO_CTRL.WE_HIGH)
        If IDENT_DATA(0) = 0 Or IDENT_DATA(1) = 0 Then
        ElseIf IDENT_DATA(0) = 255 Or IDENT_DATA(1) = 255 Then
        Else
            RaiseEvent PrintConsole("EPROM IDENT CODE returned MFG: 0x" & Hex(IDENT_DATA(0)) & " and PART 0x" & Hex(IDENT_DATA(1)))
        End If
        Return IDENT_DATA
    End Function

    Public Function EPROM_BlankCheck() As Boolean
        SetStatus("Performing EPROM blank check")
        RaiseEvent SetProgress(0)
        Dim entire_data(MyFlashDevice.FLASH_SIZE - 1) As Byte
        Dim BlockCount As Integer = (entire_data.Length / 8192)
        For i = 0 To BlockCount - 1
            If AppIsClosing Then Return False
            Dim block() As Byte = ReadBulk_NOR(i * 8191, 8191)
            Array.Copy(block, 0, entire_data, i * 8191, 8191)
            Dim percent As Single = (i / BlockCount) * 100
            RaiseEvent SetProgress(Math.Floor(percent))
        Next
        If Utilities.IsByteArrayFilled(entire_data, 255) Then
            RaiseEvent PrintConsole("EPROM device is blank and can be programmed")
            DirectCast(MyFlashDevice, OTP_EPROM).IS_BLANK = True
            Return True
        Else
            RaiseEvent PrintConsole("EPROM device is not blank")
            DirectCast(MyFlashDevice, OTP_EPROM).IS_BLANK = False
            Return False
        End If
    End Function

    Public Function EPROM_ReadData(ByVal logical_address As UInt32, ByVal data_count As UInt32) As Byte()
        Dim M27C160 As OTP_EPROM = FlashDatabase.FindDevice(&H20, &HB1, 0, MemoryType.OTP_EPROM)
        Dim M27C801 As OTP_EPROM = FlashDatabase.FindDevice(&H20, &H42, 0, MemoryType.OTP_EPROM)
        Dim M27C1001 As OTP_EPROM = FlashDatabase.FindDevice(&H20, &H5, 0, MemoryType.OTP_EPROM)
        If MyFlashDevice Is M27C160 Then
            HardwareControl(EXPIO_CTRL.VPP_5V)
            HardwareControl(EXPIO_CTRL.OE_LOW)
        ElseIf MyFlashDevice Is M27C1001 Then
            HardwareControl(EXPIO_CTRL.VPP_0V)
            HardwareControl(EXPIO_CTRL.OE_LOW)
        Else
            HardwareControl(EXPIO_CTRL.VPP_0V)
        End If
        HardwareControl(EXPIO_CTRL.WE_LOW)
        Utilities.Sleep(100)
        Dim data_out() As Byte = ReadBulk_NOR(logical_address, data_count)
        HardwareControl(EXPIO_CTRL.WE_HIGH)
        HardwareControl(EXPIO_CTRL.VPP_0V)
        Return data_out
    End Function

    Private Function EPROM_WriteData(logical_address As UInt32, data_to_write() As Byte, Params As WriteParameters) As Boolean
        Try
            Dim PacketSize As UInt32 = 2048
            Dim M27C160 As OTP_EPROM = FlashDatabase.FindDevice(&H20, &HB1, 0, MemoryType.OTP_EPROM)
            Dim M27C801 As OTP_EPROM = FlashDatabase.FindDevice(&H20, &H42, 0, MemoryType.OTP_EPROM)
            Dim M27C1001 As OTP_EPROM = FlashDatabase.FindDevice(&H20, &H5, 0, MemoryType.OTP_EPROM)
            Dim BytesWritten As UInt32 = 0
            Dim DataToWrite As UInt32 = data_to_write.Length
            Dim Loops As Integer = CInt(Math.Ceiling(DataToWrite / PacketSize)) 'Calcuates iterations
            HardwareControl(EXPIO_CTRL.WE_HIGH)
            HardwareControl(EXPIO_CTRL.VPP_12V)
            HardwareControl(EXPIO_CTRL.VPP_ENABLE)
            If (MyFlashDevice Is M27C160) Then HardwareControl(EXPIO_CTRL.OE_HIGH)
            If (MyFlashDevice Is M27C1001) Then HardwareControl(EXPIO_CTRL.OE_HIGH)
            Utilities.Sleep(200)
            For i As Integer = 0 To Loops - 1
                If Params.AbortOperation Then Return False
                Dim BufferSize As Integer = DataToWrite
                If (BufferSize > PacketSize) Then BufferSize = PacketSize
                Dim data_packet(BufferSize - 1) As Byte
                Array.Copy(data_to_write, (i * PacketSize), data_packet, 0, data_packet.Length)
                Dim result As Boolean = WriteBulk_NOR(logical_address, data_packet)
                If (Not result) Then Return False
                logical_address += data_packet.Length
                DataToWrite -= data_packet.Length
                BytesWritten += data_packet.Length
            Next
            HardwareControl(EXPIO_CTRL.VPP_0V)
            If (MyFlashDevice Is M27C160) Then HardwareControl(EXPIO_CTRL.OE_LOW)
            HardwareControl(EXPIO_CTRL.VPP_DISABLE)
        Catch ex As Exception
        End Try
        Return True
    End Function

    Private Function ByteToWordArray(byte_arr() As Byte) As UInt16()
        Dim out(byte_arr.Length / 2 - 1) As UInt16
        Dim ptr As Integer = 0
        For i = 0 To out.Length - 1
            out(i) = CUShort(byte_arr(ptr + 1)) << 8 Or byte_arr(ptr)
            ptr += 2
        Next
        Return out
    End Function

#End Region

#Region "EXPIO SETUP"

    Private Enum E_EXPIO_WRADDR As UInt16
        Parallel = 0 'Normal addressing
        Parallel_X16_26BIT = 1 '26-bit mode for XPORT PCB 1.x (Legacy)
        Parallel_CE = 2 'Set CHIP-SELECT for address
    End Enum

    Private Enum E_BUS_WIDTH 'Number of bits transfered per operation
        X0 = 0 'Default
        X8 = 8
        X16 = 16
    End Enum

    Private Enum E_EXPIO_SECTOR As UInt16
        Standard = 1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0x80;0x5555=0xAA;0x2AAA=0x55;SA=0x30
        Intel = 2 'SA=0x50;SA=0x60;SA=0xD0,SR.7,SA=0x20;SA=0xD0,SR.7 (used by Intel/Sharp devices)
    End Enum

    Private Enum E_EXPIO_CHIPERASE As UInt16
        Standard = 1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0x80;0x5555=0xAA;0x2AAA=0x55;0x5555=0x10
        Intel = 2 '0x00=0x30;0x00=0xD0; (used by Intel/Sharp devices)
    End Enum

    Private Enum E_EXPIO_WRITEDATA As UInt16
        Standard = 1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0xA0;SA=DATA;DELAY
        Intel = 2 'SA=0x40;SA=DATA;SR.7
        Bypass = 3 '0x555=0xAA;0x2AA=55;0x555=20;(0x00=0xA0;SA=DATA;...)0x00=0x90;0x00=0x00
        Page = 4 '0x5555,0x2AAA,0x5555;(BA/DATA)
        Buffer_1 = 5 '0xE8...0xD0 (Used by Intel/Sharp)
        Buffer_2 = 6 '0x555=0xAA,0x2AA=0x55,SA=0x25,SA=(WC-1).. (Used by Spanion/Cypress)
        EPROM_X8 = 7 '8-BIT EPROM DEVICE
        EPROM_X16 = 8 '16-BIT EPROM DEVICE
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
                    Case MEM_PROTOCOL.NOR_X8
                        Me.CURRENT_BUS_WIDTH = E_BUS_WIDTH.X8
                    Case MEM_PROTOCOL.NOR_X16
                        Me.CURRENT_BUS_WIDTH = E_BUS_WIDTH.X16
                    Case MEM_PROTOCOL.NOR_X16_X8
                        Me.CURRENT_BUS_WIDTH = E_BUS_WIDTH.X16
                    Case MEM_PROTOCOL.NAND_X8_ASYNC
                        Me.CURRENT_BUS_WIDTH = E_BUS_WIDTH.X8
                    Case MEM_PROTOCOL.NAND_X16_ASYNC
                        Me.CURRENT_BUS_WIDTH = E_BUS_WIDTH.X16
                    Case MEM_PROTOCOL.FWH
                        Me.CURRENT_BUS_WIDTH = E_BUS_WIDTH.X8
                    Case MEM_PROTOCOL.HYPERFLASH
                        Me.CURRENT_BUS_WIDTH = E_BUS_WIDTH.X16
                    Case MEM_PROTOCOL.EPROM_X8
                        Me.CURRENT_BUS_WIDTH = E_BUS_WIDTH.X8
                    Case MEM_PROTOCOL.EPROM_X16
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

    Private Function EXPIO_SETUP_WRITEADDRESS(mode As E_EXPIO_WRADDR) As Boolean
        Try
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_MODE_ADDRESS, Nothing, mode)
            Utilities.Sleep(10)
            Me.CURRENT_ADDR_MODE = mode
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_WRITEDATA(mode As E_EXPIO_WRITEDATA) As Boolean
        Try
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_MODE_WRITE, Nothing, mode)
            Utilities.Sleep(10)
            Me.CURRENT_WRITE_MODE = mode
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_DELAY(delay_mode As MFP_DELAY) As Boolean
        Try
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.EXPIO_MODE_DELAY, Nothing, delay_mode)
            Threading.Thread.Sleep(25)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_WRITEDELAY(delay_cycles As UInt16) As Boolean
        Try
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_DELAY, Nothing, delay_cycles)
            Threading.Thread.Sleep(25)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function
    'We should only allow this for devices that have a 12V option/chip
    Private Sub EXPIO_VPP_ENABLE()
        Dim VPP_FEAT_EN As Boolean = False
        If MyFlashDevice.GetType Is GetType(P_NOR) Then
            Dim if_type As VCC_IF = DirectCast(MyFlashDevice, P_NOR).IFACE
            If if_type = VCC_IF.X16_5V_12VPP Then
                VPP_FEAT_EN = True
            ElseIf if_type = VCC_IF.X16_3V_12VPP Then
                VPP_FEAT_EN = True
            ElseIf if_type = VCC_IF.X8_5V_12VPP Then
                VPP_FEAT_EN = True
            End If
            If VPP_FEAT_EN Then
                HardwareControl(EXPIO_CTRL.VPP_12V)
                Utilities.Sleep(100) 'We need to wait
            End If
        End If
    End Sub
    'We should only allow this for devices that have a 12V option/chip
    Private Sub EXPIO_VPP_DISABLE()
        Dim VPP_FEAT_EN As Boolean = False
        If MyFlashDevice.GetType Is GetType(P_NOR) Then
            Dim if_type As VCC_IF = DirectCast(MyFlashDevice, P_NOR).IFACE
            If if_type = VCC_IF.X16_5V_12VPP Then
                VPP_FEAT_EN = True
            ElseIf if_type = VCC_IF.X16_3V_12VPP Then
                VPP_FEAT_EN = True
            ElseIf if_type = VCC_IF.X8_5V_12VPP Then
                VPP_FEAT_EN = True
            End If
            If VPP_FEAT_EN Then
                HardwareControl(EXPIO_CTRL.VPP_5V)
                Utilities.Sleep(100) 'We need to wait
            End If
        End If
    End Sub

    Private Sub EXPIO_PrintCurrentWriteMode()
        If (MyFlashDevice.FLASH_TYPE = MemoryType.NAND) Then
        Else
            Select Case CURRENT_WRITE_MODE
                Case E_EXPIO_WRITEDATA.Standard  '0x5555=0xAA;0x2AAA=0x55;0x5555=0xA0;SA=DATA;DELAY
                    RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Standard")
                Case E_EXPIO_WRITEDATA.Intel 'SA=0x40;SA=DATA;SR.7
                    RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Auto-Word Program")
                Case E_EXPIO_WRITEDATA.Bypass '0x555=0xAA;0x2AA=55;0x555=20;(0x00=0xA0;SA=DATA;...)0x00=0x90;0x00=0x00
                    RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Bypass Mode")
                Case E_EXPIO_WRITEDATA.Page  '0x5555,0x2AAA,0x5555;(BA/DATA)
                    RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Page Write")
                Case E_EXPIO_WRITEDATA.Buffer_1  '0xE8...0xD0
                    RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Buffer (Intel)")
                Case E_EXPIO_WRITEDATA.Buffer_2 '0x555=0xAA,0x2AA=0x55,SA=0x25,SA=(WC-1)..
                    RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Buffer (Cypress)")
                Case E_EXPIO_WRITEDATA.EPROM_X8
                    RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": EPROM (8-bit)")
                Case E_EXPIO_WRITEDATA.EPROM_X16
                    RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": EPROM (16-bit)")
            End Select
        End If
    End Sub

#End Region

#Region "PARALLEL NOR"
    Private Property CURRENT_WRITE_MODE As E_EXPIO_WRITEDATA
    Private Property CURRENT_SECTOR_ERASE As E_EXPIO_SECTOR
    Private Property CURRENT_CHIP_ERASE As E_EXPIO_CHIPERASE
    Private Property CURRENT_ADDR_MODE As E_EXPIO_WRADDR

    Private Delegate Sub cfi_cmd_sub()
    Private cfi_data() As Byte

    '0xAAA=0xAA;0x555=0x55;0xAAA=0x90; (X8/X16 DEVICES)
    Private Function EXPIO_ReadIdent(X16_MODE As Boolean) As Byte()
        Dim ident(7) As Byte
        Dim SHIFT As UInt32 = 0
        If X16_MODE Then SHIFT = 1
        EXPIO_ResetDevice()
        Utilities.Sleep(10)
        WriteCommandData(&H5555, &HAA)
        WriteCommandData(&H2AAA, &H55)
        WriteCommandData(&H5555, &H90)
        Utilities.Sleep(10)
        ident(0) = CByte(ReadMemoryAddress(0) And &HFF)             'MFG
        Dim ID1 As UInt16 = ReadMemoryAddress(1 << SHIFT)
        If Not X16_MODE Then ID1 = (ID1 And &HFF)                   'X8 ID1
        ident(1) = CByte((ID1 >> 8) And &HFF)                       'ID1(UPPER)
        ident(2) = CByte(ID1 And &HFF)                              'ID1(LOWER)
        ident(3) = CByte(ReadMemoryAddress(&HE << SHIFT) And &HFF)  'ID2
        ident(4) = CByte(ReadMemoryAddress(&HF << SHIFT) And &HFF)  'ID3
        EXPIO_ResetDevice()
        Utilities.Sleep(1)
        Me.CURRENT_SECTOR_ERASE = E_EXPIO_SECTOR.Standard
        Me.CURRENT_CHIP_ERASE = E_EXPIO_CHIPERASE.Standard
        Me.CURRENT_WRITE_MODE = E_EXPIO_WRITEDATA.Standard
        Return ident
    End Function
    'Sets access and write pulse timings for MACH1 using NOR PARALLEL mode
    Public Sub EXPIO_SetTiming(read_access As Integer, we_pulse As Integer)
        If FCUSB.HWBOARD = FCUSB_BOARD.Mach1 Then
            FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_TIMING, Nothing, (read_access << 8 Or we_pulse))
        End If
    End Sub

    Private Sub EXPIO_WAIT()
        Utilities.Sleep(10)
        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WAIT)
        FCUSB.USB_WaitForComplete() 'Checks for WAIT flag to clear
    End Sub

    '(X8/X16 DEVICES)
    Private Sub EXPIO_EraseSector_Standard(addr As UInt32)
        'Write Unlock Cycles
        WriteCommandData(&H5555, &HAA)
        WriteCommandData(&H2AAA, &H55)
        'Write Sector Erase Cycles
        WriteCommandData(&H5555, &H80)
        WriteCommandData(&H5555, &HAA)
        WriteCommandData(&H2AAA, &H55)
        WriteMemoryAddress(addr, &H30)
    End Sub

    Private Sub EXPIO_EraseSector_Intel(addr As UInt32)
        WriteMemoryAddress(addr, &H50) 'clear register
        WriteMemoryAddress(addr, &H60) 'Unlock block (just in case)
        WriteMemoryAddress(addr, &HD0) 'Confirm Command
        EXPIO_WAIT()
        WriteMemoryAddress(addr, &H20)
        WriteMemoryAddress(addr, &HD0)
        EXPIO_WAIT()
        WriteMemoryAddress(0, &HFF) 'Puts the device back into READ mode
        WriteMemoryAddress(0, &HF0)
    End Sub

    Private Sub EXPIO_EraseChip_Standard()
        WriteCommandData(&H5555, &HAA)
        WriteCommandData(&H2AAA, &H55)
        WriteCommandData(&H5555, &H80)
        WriteCommandData(&H5555, &HAA)
        WriteCommandData(&H2AAA, &H55)
        WriteCommandData(&H5555, &H10)
    End Sub

    Private Sub EXPIO_EraseChip_Intel()
        WriteMemoryAddress(&H0, &H30)
        WriteMemoryAddress(&H0, &HD0)
    End Sub

    Private Sub EXPIO_ResetDevice()
        WriteCommandData(&H5555, &HAA) 'Standard
        WriteCommandData(&H2AAA, &H55)
        WriteCommandData(&H5555, &HF0)
        WriteCommandData(0, &HFF)
        WriteCommandData(0, &HF0) 'Intel
    End Sub

    Private Sub EXPIO_EraseChip()
        Select Case CURRENT_CHIP_ERASE
            Case E_EXPIO_CHIPERASE.Standard
                EXPIO_EraseChip_Standard()
            Case E_EXPIO_CHIPERASE.Intel
                EXPIO_EraseChip_Intel()
        End Select
    End Sub

    Private Sub EXPIO_EraseSector(sector_addr As UInt32)
        Select Case CURRENT_SECTOR_ERASE
            Case E_EXPIO_SECTOR.Standard
                EXPIO_EraseSector_Standard(sector_addr)
            Case E_EXPIO_SECTOR.Intel
                EXPIO_EraseSector_Intel(sector_addr)
        End Select
    End Sub

    Private Function EXPIO_NOR_GetCFI() As Byte()
        Try
            cfi_data = Nothing
            If CFI_ExecuteCommand(Sub() WriteCommandData(&H55, &H98)) Then 'Issue Enter CFI command
                WriteConsole("Common Flash Interface information present")
                Return cfi_data
            ElseIf CFI_ExecuteCommand(Sub()
                                          WriteCommandData(&H5555, &HAA)
                                          WriteCommandData(&H2AAA, &H55)
                                          WriteCommandData(&H5555, &H98)
                                      End Sub) Then
                WriteConsole("Common Flash Interface information present")
                Return cfi_data
            Else
                cfi_data = Nothing
                WriteConsole("Common Flash Interface information not present")
            End If
        Catch ex As Exception
        Finally
            EXPIO_ResetDevice()
            Utilities.Sleep(50)
        End Try
        Return Nothing
    End Function

    Private Function CFI_ExecuteCommand(cfi_cmd As cfi_cmd_sub) As Boolean
        cfi_cmd.Invoke()
        ReDim cfi_data(31)
        Dim SHIFT As UInt32 = 0
        If Me.CURRENT_BUS_WIDTH = E_BUS_WIDTH.X16 Then SHIFT = 1
        ReDim cfi_data(31)
        For i = 0 To cfi_data.Length - 1
            cfi_data(i) = CByte(ReadMemoryAddress((&H10 + i) << SHIFT) And 255)
        Next
        If cfi_data(0) = &H51 And cfi_data(1) = &H52 And cfi_data(2) = &H59 Then 'QRY
            Return True
        End If
        Return False
    End Function
    'This is used to write data (8/16 bit) to the EXTIO IO (parallel NOR) port. CMD ADDRESS
    Public Function WriteCommandData(cmd_addr As UInt32, cmd_data As UInt16) As Boolean
        Dim addr_data(5) As Byte
        addr_data(0) = CByte((cmd_addr >> 24) And 255)
        addr_data(1) = CByte((cmd_addr >> 16) And 255)
        addr_data(2) = CByte((cmd_addr >> 8) And 255)
        addr_data(3) = CByte(cmd_addr And 255)
        addr_data(4) = CByte((cmd_data >> 8) And 255)
        addr_data(5) = CByte(cmd_data And 255)
        If FCUSB.HWBOARD = FCUSB_BOARD.Mach1 Then
            FCUSB.USB_CONTROL_MSG_OUT(USBREQ.LOAD_PAYLOAD, addr_data)
            Return FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WRCMDDATA)
        Else
            Return FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WRCMDDATA, addr_data)
        End If
    End Function

    Public Function WriteMemoryAddress(mem_addr As UInt32, mem_data As UInt16) As UInt16
        Dim addr_data(5) As Byte
        addr_data(0) = CByte((mem_addr >> 24) And 255)
        addr_data(1) = CByte((mem_addr >> 16) And 255)
        addr_data(2) = CByte((mem_addr >> 8) And 255)
        addr_data(3) = CByte(mem_addr And 255)
        addr_data(4) = CByte((mem_data >> 8) And 255)
        addr_data(5) = CByte(mem_data And 255)
        If FCUSB.HWBOARD = FCUSB_BOARD.Mach1 Then
            FCUSB.USB_CONTROL_MSG_OUT(USBREQ.LOAD_PAYLOAD, addr_data)
            Return FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WRMEMDATA)
        Else
            Return FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WRMEMDATA, addr_data)
        End If
    End Function

    Public Function ReadMemoryAddress(mem_addr As UInt32) As UInt16
        Dim data_out(1) As Byte
        FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_RDMEMDATA, data_out, mem_addr)
        Return (CUShort(data_out(1)) << 8) Or data_out(0)
    End Function

#End Region

#Region "Detect Flash Device"

    Private Function DetectFlashDevice() As Boolean
        RaiseEvent PrintConsole(RM.GetString("ext_detecting_device")) 'Attempting to automatically detect Flash device
        Dim LAST_DETECT As FlashDetectResult = Nothing
        LAST_DETECT.MFG = 0
        Me.FLASH_IDENT = DetectFlash(MEM_PROTOCOL.NAND_X8_ASYNC) 'X16 and X8 are detected with X8
        If Me.FLASH_IDENT.Successful Then
            Dim d() As Device = FlashDatabase.FindDevices(Me.FLASH_IDENT.MFG, Me.FLASH_IDENT.ID1, Me.FLASH_IDENT.ID2, MemoryType.NAND)
            If (d.Count > 0) Then
                RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "NAND"))
                MyAdapter = MEM_PROTOCOL.NAND_X8_ASYNC
                Return True
            Else
                LAST_DETECT = Me.FLASH_IDENT
            End If
        End If
        Me.FLASH_IDENT = DetectFlash(MEM_PROTOCOL.NOR_X16)
        If Me.FLASH_IDENT.Successful Then
            Dim d() As Device = FlashDatabase.FindDevices(Me.FLASH_IDENT.MFG, Me.FLASH_IDENT.ID1, Me.FLASH_IDENT.ID2, MemoryType.PARALLEL_NOR)
            If (d.Count > 0) AndAlso IsIFACE16X(DirectCast(d(0), P_NOR).IFACE) Then
                RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "NOR X16 (Word addressing)"))
                MyAdapter = MEM_PROTOCOL.NOR_X16
                Return True
            Else
                LAST_DETECT = Me.FLASH_IDENT
            End If
        End If
        If Not FCUSB.HWBOARD = FCUSB_BOARD.XPORT_PCB1 Then
            Me.FLASH_IDENT = DetectFlash(MEM_PROTOCOL.NOR_X16_X8)
            If Me.FLASH_IDENT.Successful Then
                Dim d() As Device = FlashDatabase.FindDevices(Me.FLASH_IDENT.MFG, Me.FLASH_IDENT.ID1, 0, MemoryType.PARALLEL_NOR)
                If (d.Count > 0) AndAlso IsIFACE16X(DirectCast(d(0), P_NOR).IFACE) Then
                    RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "NOR X16 (Byte addressing)"))
                    MyAdapter = MEM_PROTOCOL.NOR_X16
                    Return True
                Else
                    LAST_DETECT = Me.FLASH_IDENT
                End If
            End If
        End If
        Me.FLASH_IDENT = DetectFlash(MEM_PROTOCOL.NOR_X8)
        If Me.FLASH_IDENT.Successful Then
            Dim d() As Device = FlashDatabase.FindDevices(Me.FLASH_IDENT.MFG, Me.FLASH_IDENT.ID1, 0, MemoryType.PARALLEL_NOR)
            If (d.Count > 0) AndAlso IsIFACE8X(DirectCast(d(0), P_NOR).IFACE) Then
                RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "NOR X8"))
                MyAdapter = MEM_PROTOCOL.NOR_X8
                Return True
            Else
                LAST_DETECT = Me.FLASH_IDENT
            End If
        End If
        If Not FCUSB.HWBOARD = FCUSB_BOARD.Mach1 Then
            Me.FLASH_IDENT = DetectFlash(MEM_PROTOCOL.FWH)
            If Me.FLASH_IDENT.Successful Then
                Dim d() As Device = FlashDatabase.FindDevices(Me.FLASH_IDENT.MFG, Me.FLASH_IDENT.ID1, 0, MemoryType.FWH_NOR)
                If (d.Count > 0) Then
                    RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "FWH"))
                    MyAdapter = MEM_PROTOCOL.FWH
                    Return True
                Else
                    LAST_DETECT = Me.FLASH_IDENT
                End If
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
            Case MEM_PROTOCOL.NOR_X16
                mode_name = "NOR X16 (Word addressing)"
                result = EXPIO_DetectX16()
            Case MEM_PROTOCOL.NOR_X16_X8
                mode_name = "NOR X16 (Byte addressing)"
                result = EXPIO_DetectX16_X8()
            Case MEM_PROTOCOL.NOR_X8
                mode_name = "NOR X8"
                result = EXPIO_DetectX8()
            Case MEM_PROTOCOL.FWH
                mode_name = "FWH"
                result = EXPIO_DetectFWH()
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

    Private Function EXPIO_DetectX16() As FlashDetectResult
        Dim ident_data() As Byte = Nothing
        Dim devices() As Device = Nothing
        EXPIO_SETUP_USB(MEM_PROTOCOL.NOR_X16)
        ident_data = EXPIO_ReadIdent(True)
        Return GetFlashResult(ident_data)
    End Function

    Private Function EXPIO_DetectX16_X8() As FlashDetectResult
        Dim ident_data() As Byte = Nothing
        Dim devices() As Device = Nothing
        EXPIO_SETUP_USB(MEM_PROTOCOL.NOR_X16_X8)
        ident_data = EXPIO_ReadIdent(True)
        Return GetFlashResult(ident_data)
    End Function

    Private Function EXPIO_DetectX8() As FlashDetectResult
        Dim ident_data() As Byte = Nothing
        Dim devices() As Device = Nothing
        EXPIO_SETUP_USB(MEM_PROTOCOL.NOR_X8)
        ident_data = EXPIO_ReadIdent(False)
        Return GetFlashResult(ident_data)
    End Function

    Private Function EXPIO_DetectFWH() As FlashDetectResult
        Dim ident_data(7) As Byte
        EXPIO_SETUP_USB(MEM_PROTOCOL.FWH)
        FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_RDID, ident_data)
        Return GetFlashResult(ident_data)
    End Function

    Private Function EXPIO_DetectNAND() As FlashDetectResult
        Dim ident_data(7) As Byte
        FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_RDID, ident_data)
        Return GetFlashResult(ident_data)
    End Function
    'contains AutoSelect Device ID and some CFI-ID space
    Public Structure FlashDetectResult
        Public Property Successful As Boolean
        Public Property MFG As Byte
        Public Property ID1 As UInt16
        Public Property ID2 As UInt16
        Public ReadOnly Property PART As UInt32
            Get
                Return (CUInt(Me.ID1) << 16) Or (Me.ID2)
            End Get
        End Property

    End Structure

    Public Shared Function GetFlashResult(ident_data() As Byte) As FlashDetectResult
        Dim result As New FlashDetectResult
        result.Successful = False
        If ident_data Is Nothing Then Return result
        If ident_data(0) = 0 AndAlso ident_data(2) = 0 Then Return result '0x0000
        If ident_data(0) = &H90 AndAlso ident_data(2) = &H90 Then Return result '0x9090 
        If ident_data(0) = &H90 AndAlso ident_data(2) = 0 Then Return result '0x9000 
        If ident_data(0) = &HFF AndAlso ident_data(2) = &HFF Then Return result '0xFFFF 
        If ident_data(0) = &HFF AndAlso ident_data(2) = 0 Then Return result '0xFF00
        If ident_data(0) = &H1 AndAlso ident_data(1) = 0 AndAlso ident_data(2) = &H1 AndAlso ident_data(3) = 0 Then Return result '0x01000100
        If Array.TrueForAll(ident_data, Function(a) a.Equals(ident_data(0))) Then Return result 'If all bytes are the same
        result.MFG = ident_data(0)
        result.ID1 = (CUInt(ident_data(1)) << 8) Or CUInt(ident_data(2))
        result.ID2 = (CUInt(ident_data(3)) << 8) Or CUInt(ident_data(4))
        If result.ID1 = 0 AndAlso result.ID2 = 0 Then Return result
        result.Successful = True
        Return result
    End Function

#End Region

    Private Sub PrintDeviceInterface()
        If (MyFlashDevice.FLASH_TYPE = MemoryType.PARALLEL_NOR) Or (MyFlashDevice.FLASH_TYPE = MemoryType.OTP_EPROM) Then
            Select Case DirectCast(MyFlashDevice, P_NOR).IFACE
                Case VCC_IF.X8_3V
                    RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X8 (3V)")
                Case VCC_IF.X8_5V
                    RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X8 (5V)")
                Case VCC_IF.X8_5V_12VPP
                    RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (5V/12V VPP)")
                Case VCC_IF.X16_3V
                    RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (3V)")
                Case VCC_IF.X16_5V
                    RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (5V)")
                Case VCC_IF.X16_5V_12VPP
                    RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (5V/12V VPP)")
            End Select
        ElseIf (MyFlashDevice.FLASH_TYPE = MemoryType.FWH_NOR) Then
            RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR (FWH)")
        End If
    End Sub

    Private Function IsIFACE8X(input As VCC_IF) As Boolean
        Select Case input
            Case VCC_IF.X8_3V
                Return True
            Case VCC_IF.X8_5V
                Return True
            Case VCC_IF.X8_5V_12VPP
                Return True
            Case Else
                Return False
        End Select
    End Function

    Private Function IsIFACE16X(input As VCC_IF) As Boolean
        Select Case input
            Case VCC_IF.X16_3V
                Return True
            Case VCC_IF.X16_5V
                Return True
            Case VCC_IF.X16_5V_12VPP
                Return True
            Case Else
                Return False
        End Select
    End Function

    Public Function ResetDevice() As Boolean
        Try
            If MyFlashDevice.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                EXPIO_ResetDevice()
            ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.OTP_EPROM Then
                EXPIO_ResetDevice()
            End If
        Catch ex As Exception
            Return False
        Finally
            Utilities.Sleep(50)
        End Try
        Return True
    End Function

    Private Function GetSetupPacket_NAND(page_addr As UInt32, page_offset As UInt16, transfer_size As UInt32, area As FlashArea) As Byte()
        Dim NAND_DEV As P_NAND = DirectCast(MyFlashDevice, P_NAND)
        Dim nand_layout As NANDLAYOUT_STRUCTURE = NANDLAYOUT_Get(NAND_DEV)
        If MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Combined Then area = FlashArea.All
        Dim TX_NAND_ADDRSIZE As Byte 'Number of bytes the address command table uses
        If (NAND_DEV.PAGE_SIZE = 512) Then 'Small page
            If (MyFlashDevice.FLASH_SIZE > Mb256) Then
                TX_NAND_ADDRSIZE = 4
            Else
                TX_NAND_ADDRSIZE = 3
            End If
        Else
            If NAND_DEV.FLASH_SIZE < Gb002 Then
                TX_NAND_ADDRSIZE = 4 '<=1Gbit
            Else
                TX_NAND_ADDRSIZE = 5 '2Gbit+
            End If
        End If
        Dim setup_data(19) As Byte '18 bytes total
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
        setup_data(12) = CByte(NAND_DEV.EXT_PAGE_SIZE And 255)
        setup_data(13) = CByte((NAND_DEV.EXT_PAGE_SIZE >> 8) And 255)
        setup_data(14) = CByte(nand_layout.Layout_Main And 255)
        setup_data(15) = CByte((nand_layout.Layout_Main >> 8) And 255)
        setup_data(16) = CByte(nand_layout.Layout_Spare And 255)
        setup_data(17) = CByte((nand_layout.Layout_Spare >> 8) And 255)
        setup_data(18) = TX_NAND_ADDRSIZE
        setup_data(19) = area 'Area (0=main,1=spare,2=all), note: all ignores layout settings
        Return setup_data
    End Function

    Private Function GetSetupPacket_NOR(Address As UInt32, Count As UInt32, PageSize As UInt16) As Byte()
        Dim addr_bytes As Byte = 0
        Dim data_in(19) As Byte '18 bytes total
        data_in(0) = CByte(Address And 255)
        data_in(1) = CByte((Address >> 8) And 255)
        data_in(2) = CByte((Address >> 16) And 255)
        data_in(3) = CByte((Address >> 24) And 255)
        data_in(4) = CByte(Count And 255)
        data_in(5) = CByte((Count >> 8) And 255)
        data_in(6) = CByte((Count >> 16) And 255)
        data_in(7) = CByte((Count >> 24) And 255)
        data_in(8) = CByte(PageSize And 255) 'This is how many bytes to increment between operations
        data_in(9) = CByte((PageSize >> 8) And 255)
        Return data_in
    End Function

    Public Function BlankCheck(base_addr As UInt32) As Boolean
        Try
            Dim IsBlank As Boolean = False
            Dim Counter As Integer = 0
            Do Until IsBlank
                Utilities.Sleep(10)
                Dim w() As Byte = ReadData(base_addr, 4, FlashArea.Main)
                If w Is Nothing Then Return False
                If w(0) = 255 AndAlso w(1) = 255 AndAlso w(2) = 255 AndAlso w(3) = 255 Then IsBlank = True
                Counter += 1
                If Counter = 50 Then Return False 'Timeout (500 ms)
            Loop
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function ReadBulk_NOR(address As UInt32, count As UInt32) As Byte()
        Try
            Dim read_count As UInt32 = count
            Dim addr_offset As Boolean = False
            Dim count_offset As Boolean = False
            If Not (MyAdapter = MEM_PROTOCOL.NOR_X8 OrElse MyAdapter = MEM_PROTOCOL.FWH) Then
                If (address Mod 2 = 1) Then
                    addr_offset = True
                    address = (address - 1)
                    read_count += 1
                End If
                If (read_count Mod 2 = 1) Then
                    count_offset = True
                    read_count += 1
                End If
            End If
            Dim data_out(read_count - 1) As Byte 'Bytes we want to read
            Dim page_size As Integer = 512
            If MyFlashDevice IsNot Nothing Then page_size = MyFlashDevice.PAGE_SIZE
            Dim setup_data() As Byte = GetSetupPacket_NOR(address, read_count, page_size)
            Dim result As Boolean = FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_READDATA, setup_data, data_out, 0)
            If Not result Then Return Nothing
            If addr_offset Then
                Dim new_data(count - 1) As Byte
                Array.Copy(data_out, 1, new_data, 0, new_data.Length)
                data_out = new_data
            Else
                ReDim Preserve data_out(count - 1)
            End If
            Return data_out
        Catch ex As Exception
        End Try
        Return Nothing
    End Function

    Private Function WriteBulk_NOR(address As UInt32, data_out() As Byte) As Boolean
        Try
            Dim setup_data() As Byte = GetSetupPacket_NOR(address, data_out.Length, MyFlashDevice.PAGE_SIZE)
            Dim result As Boolean = FCUSB.USB_SETUP_BULKOUT(USBREQ.EXPIO_WRITEDATA, setup_data, data_out, 0)
            Return result
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Function ReadBulk_NAND(page_addr As UInt32, page_offset As UInt16, count As UInt32, memory_area As FlashArea) As Byte()
        Try
            Dim result As Boolean
            If MySettings.ECC_READ_ENABLED AndAlso (memory_area = FlashArea.Main) Then 'We need to auto-correct data uisng ECC
                Dim NAND_DEV As P_NAND = DirectCast(MyFlashDevice, P_NAND)
                Dim page_count As UInt32 = Math.Ceiling((count + page_offset) / NAND_DEV.PAGE_SIZE) 'Number of complete pages and OOB to read and correct
                Dim total_main_bytes As UInt32 = (page_count * NAND_DEV.PAGE_SIZE)
                Dim total_oob_bytes As UInt32 = (page_count * NAND_DEV.EXT_PAGE_SIZE)
                Dim main_area_data(total_main_bytes - 1) As Byte 'Data from the main page
                Dim setup_data() As Byte = GetSetupPacket_NAND(page_addr, 0, main_area_data.Length, FlashArea.Main)
                result = FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_READDATA, setup_data, main_area_data, 1)
                If Not result Then Return Nothing
                Dim oob_area_data(total_oob_bytes - 1) As Byte 'Data from the spare page, containing flags, metadata and ecc data
                setup_data = GetSetupPacket_NAND(page_addr, 0, oob_area_data.Length, FlashArea.OOB)
                result = FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_READDATA, setup_data, oob_area_data, 1)
                If Not result Then Return Nothing
                Dim ecc_data() As Byte = NAND_ECC_ENG.GetEccFromSpare(oob_area_data, NAND_DEV.PAGE_SIZE, NAND_DEV.EXT_PAGE_SIZE) 'This strips out the ecc data from the spare area
                ECC_LAST_RESULT = NAND_ECC_ENG.ReadData(main_area_data, ecc_data) 'This processes the flash data (512 bytes at a time) and corrects for any errors using the ECC
                If ECC_LAST_RESULT = ECC_DECODE_RESULT.Uncorractable Then
                    Dim logical_addr As Long = (page_addr * CLng(MyFlashDevice.PAGE_SIZE)) + page_offset
                    RaiseEvent PrintConsole("ECC failed at: 0x" & Hex(logical_addr).PadLeft(8, "0"))
                End If
                Dim data_out(count - 1) As Byte 'This is the data the user requested
                Array.Copy(main_area_data, page_offset, data_out, 0, data_out.Length)
                Return data_out
            Else 'Normal read from device
                Dim data_out(count - 1) As Byte 'Bytes we want to read
                Dim setup_data() As Byte = GetSetupPacket_NAND(page_addr, page_offset, count, memory_area)
                result = FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_READDATA, setup_data, data_out, 1)
                If Not result Then Return Nothing
                Return data_out
            End If
        Catch ex As Exception
        End Try
        Return Nothing
    End Function

    Private Function WriteBulk_NAND(page_addr As UInt32, main_data() As Byte, oob_data() As Byte, memory_area As FlashArea) As Boolean
        Try
            If main_data Is Nothing And oob_data Is Nothing Then Return False
            Dim NAND_DEV As P_NAND = DirectCast(MyFlashDevice, P_NAND)
            Dim page_size_tot As UInt16 = (MyFlashDevice.PAGE_SIZE + NAND_DEV.EXT_PAGE_SIZE)
            Dim page_aligned() As Byte = Nothing
            If memory_area = FlashArea.All Then 'Ignore OOB/SPARE
                oob_data = Nothing
                Dim total_pages As UInt32 = Math.Ceiling(main_data.Length / page_size_tot)
                ReDim page_aligned((total_pages * page_size_tot) - 1)
                For i = 0 To page_aligned.Length - 1 : page_aligned(i) = 255 : Next
                Array.Copy(main_data, 0, page_aligned, 0, main_data.Length)
            ElseIf memory_area = FlashArea.Main Then
                If MySettings.ECC_WRITE_ENABLED Then
                    If oob_data Is Nothing Then
                        ReDim oob_data(((main_data.Length / NAND_DEV.PAGE_SIZE) * NAND_DEV.EXT_PAGE_SIZE) - 1)
                        Utilities.FillByteArray(oob_data, 255)
                    End If
                    Dim ecc_data() As Byte = Nothing
                    NAND_ECC_ENG.WriteData(main_data, ecc_data)
                    NAND_ECC_ENG.SetEccToSpare(oob_data, ecc_data, NAND_DEV.PAGE_SIZE, NAND_DEV.EXT_PAGE_SIZE)
                End If
                page_aligned = CreatePageAligned(MyFlashDevice, main_data, oob_data)
            ElseIf memory_area = FlashArea.OOB Then
                page_aligned = CreatePageAligned(MyFlashDevice, main_data, oob_data)
            End If
            Dim pages_to_write As UInt32 = (page_aligned.Length / page_size_tot)
            Dim array_ptr As UInt32 = 0
            Do Until pages_to_write = 0
                Dim page_count_max As Integer = 0 'Number of total pages to write per operation
                If NAND_DEV.PAGE_SIZE = 512 Then
                    page_count_max = 8
                ElseIf NAND_DEV.PAGE_SIZE = 2048 Then
                    page_count_max = 4
                ElseIf NAND_DEV.PAGE_SIZE = 4096 Then
                    page_count_max = 2
                ElseIf NAND_DEV.PAGE_SIZE = 8192 Then
                    page_count_max = 1
                End If
                Dim count As UInt32 = Math.Min(page_count_max, pages_to_write) 'Write up to 4 pages (fcusb pro buffer has 12KB total)
                Dim packet((count * page_size_tot) - 1) As Byte
                Array.Copy(page_aligned, array_ptr, packet, 0, packet.Length)
                array_ptr += packet.Length
                Dim setup() As Byte = GetSetupPacket_NAND(page_addr, 0, packet.Length, FlashArea.All) 'We will write the entire page
                Dim result As Boolean = FCUSB.USB_SETUP_BULKOUT(USBREQ.EXPIO_WRITEDATA, setup, packet, 1)
                If Not result Then Return Nothing
                FCUSB.USB_WaitForComplete()
                page_addr += count
                pages_to_write -= count
            Loop
            Return True
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Sub HardwareControl(cmd As EXPIO_CTRL)
        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, cmd)
        Utilities.Sleep(10)
    End Sub

    Public Sub PARALLEL_PORT_TEST()
        SetStatus("Performing parallel I/O output test")
        EXPIO_SETUP_USB(MEM_PROTOCOL.NOR_X16)
        WriteCommandData(&HFFFFFFFFUI, &HFFFF)
        HardwareControl(EXPIO_CTRL.OE_HIGH)
        HardwareControl(EXPIO_CTRL.CE_HIGH)
        HardwareControl(EXPIO_CTRL.WE_HIGH)
        HardwareControl(EXPIO_CTRL.RELAY_OFF)
        HardwareControl(EXPIO_CTRL.VPP_DISABLE) 'Turns off adapter
        Utilities.Sleep(500)
        WriteCommandData(0, 0)
        Utilities.Sleep(500)
        HardwareControl(EXPIO_CTRL.VPP_5V)
        Utilities.Sleep(300)
        HardwareControl(EXPIO_CTRL.VPP_0V)
        For i = 0 To 15
            WriteCommandData(0, 1 << i)
            Utilities.Sleep(300)
        Next
        WriteCommandData(0, 0)
        HardwareControl(EXPIO_CTRL.OE_LOW)
        HardwareControl(EXPIO_CTRL.CE_LOW)
        HardwareControl(EXPIO_CTRL.WE_LOW)
        Utilities.Sleep(300)
        HardwareControl(EXPIO_CTRL.OE_HIGH)
        Utilities.Sleep(300)
        HardwareControl(EXPIO_CTRL.OE_LOW)
        HardwareControl(EXPIO_CTRL.WE_HIGH)
        Utilities.Sleep(300)
        HardwareControl(EXPIO_CTRL.WE_LOW)
        HardwareControl(EXPIO_CTRL.CE_HIGH)
        Utilities.Sleep(300)
        HardwareControl(EXPIO_CTRL.CE_LOW)
        Utilities.Sleep(300)
        For i = 0 To 27
            WriteCommandData(1 << i, 0)
            Utilities.Sleep(300)
        Next
        WriteCommandData(0, 0)
        SetStatus("Parallel I/O output test complete")
    End Sub

End Class
