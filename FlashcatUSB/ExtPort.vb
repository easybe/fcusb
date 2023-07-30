Imports FlashcatUSB.ECC_LIB
Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB
Imports FlashcatUSB.USB.HostClient

Public Class ExtPort : Implements MemoryDeviceUSB
    Private FCUSB As FCUSB_DEVICE
    Public Property MyFlashDevice As Device  'Contains the definition for the EXT I/O device that is connected
    Public Property MyFlashStatus As DeviceStatus = DeviceStatus.NotDetected
    Public MyAdapter As AdatperType 'This is the kind of socket adapter connected and the mode it is in
    Private CURRENT_WRITE_MODE As E_EXPIO_WRITEDATA
    Private CURRENT_ADDR_MODE As E_EXPIO_WRADDR

    Public CHIPID_MFG As Byte = 0
    Public CHIPID_PART As UInt32 = 0
    Public CHIPID_DETECT As FlashDetectResult

    Public Event PrintConsole(ByVal msg As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(ByVal percent As Integer) Implements MemoryDeviceUSB.SetProgress

    Public Property FWH_SUPPORTED As Boolean = False 'XPORT ONLY
    Public Property MULTI_DIE As Boolean = False
    Public Property DIE_SELECTED As Integer = 0
    Public Property ECC_READ_ENABLED As Boolean = False
    Public Property ECC_WRITE_ENABLED As Boolean = False
    Public Property ECC_LAST_RESULT As decode_result = decode_result.NoErrors

    Sub New(ByVal parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        MyFlashDevice = Nothing
        If Not EXPIO_SETUP_USB(EXPIO_Mode.Setup) Then
            RaiseEvent PrintConsole(RM.GetString("ext_unable_to_connect_to_board"))
            MyFlashStatus = DeviceStatus.ExtIoNotConnected
            Return False
        Else
            RaiseEvent PrintConsole(RM.GetString("ext_board_initalized"))
        End If
        If DetectFlashDevice() Then
            Dim chip_id_str As String = Hex(CHIPID_MFG).PadLeft(2, "0") & Hex(CHIPID_PART).PadLeft(8, "0")
            RaiseEvent PrintConsole(String.Format(RM.GetString("ext_connected_chipid"), chip_id_str))
            Dim ID1 As UInt16 = (CHIPID_PART >> 16)
            Dim ID2 As UInt16 = (CHIPID_PART And &HFFFF)
            If FCUSB.HWBOARD = FCUSB_BOARD.Classic_XPORT AndAlso (ID1 >> 8 = 255) Then
                ID1 = (ID1 And 255) 'XPORT IO is a little different than the EXTIO for X8 devices
            End If
            Dim device_matches() As Device
            If (MyAdapter = AdatperType.NAND) Then
                device_matches = FlashDatabase.FindDevices(CHIPID_MFG, ID1, ID2, MemoryType.SLC_NAND)
            Else
                If MyAdapter = AdatperType.X8_Type1 Or MyAdapter = AdatperType.X8_Type2 Then
                    device_matches = FlashDatabase.FindDevices(CHIPID_MFG, ID1, 0, MemoryType.PARALLEL_NOR)
                ElseIf MyAdapter = AdatperType.FWH Then
                    device_matches = FlashDatabase.FindDevices(CHIPID_MFG, ID1, 0, MemoryType.FWH_NOR)
                Else
                    device_matches = FlashDatabase.FindDevices(CHIPID_MFG, ID1, ID2, MemoryType.PARALLEL_NOR)
                End If
            End If
            If (device_matches IsNot Nothing AndAlso device_matches.Count > 0) Then
                If (device_matches.Count > 1) Then
                    Dim cfi_page_size As UInt32 = (2 ^ CHIPID_DETECT.CFI_MULTI)
                    For i = 0 To device_matches.Count - 1
                        If device_matches(i).PAGE_SIZE = cfi_page_size Then
                            MyFlashDevice = device_matches(i) : Exit For
                        End If
                    Next
                End If
                If MyFlashDevice Is Nothing Then MyFlashDevice = device_matches(0)
                RaiseEvent PrintConsole(String.Format(RM.GetString("flash_detected"), MyFlashDevice.NAME, Format(MyFlashDevice.FLASH_SIZE, "#,###")))
                RaiseEvent PrintConsole(RM.GetString("ext_prog_mode"))
                SetupFlashDevice()
                If (MyFlashDevice.FLASH_TYPE = MemoryType.PARALLEL_NOR) Then
                    Dim NOR_FLASH As MFP_Flash = DirectCast(MyFlashDevice, MFP_Flash)
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
                    If (MyAdapter = AdatperType.X8_Type1 Or MyAdapter = AdatperType.X16_Type1) Then
                        EXPIO_SETUP_CHIPERASE(E_EXPIO_CHIPERASE.Type1) '0x5555=0xAA;0x2AAA=0x55;0x5555=0x80;0x5555=0xAA;0x2AAA=0x55;0x5555=0x10
                        EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.Type1) '0x5555=0xAA;0x2AAA=0x55;0x5555=0x80;0x5555=0xAA;0x2AAA=0x55;SA=0x30
                        EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type1) '0x5555=0xAA;0x2AAA=0x55;0x5555=0xA0;SA=DATA;DELAY
                    ElseIf (MyAdapter = AdatperType.X16_Type2) Then
                        EXPIO_SETUP_CHIPERASE(E_EXPIO_CHIPERASE.Type3) '0x555=0xAA;0x2AA=0x55;0x555=0x80;0x555=0xAA;0x2AA=0x55;0x555=0x10
                        EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.Type3) '0x555=0xAA,0x2AA=0x55,0x555=0x80,0x555=0xAA,0x2AA=0x55;SA=0x30
                        EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type3) '0x555=0xAA;0x2AA=0x55;0x555=0xA0,SA=DATA;DELAY
                    ElseIf MyAdapter = AdatperType.X8_Type2 Then
                        EXPIO_SETUP_CHIPERASE(E_EXPIO_CHIPERASE.Type2) '0xAAA=0xAA;0x555=0x55;0xAAA=0x80;0xAAA=0xAA;0x555=0x55;0xAAA=0x10
                        EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.Type2) '0xAAA=0xAA;0x555=0x55;0xAAA=0x80;0xAAA=0xAA;0x555=0x55;SA=0x30
                        EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type2) '0xAAA=0xAA;0x555=0x55;0xAAA=0xA0;SA=DATA;DELAY 
                    End If
                    If (NOR_FLASH.AVAILABLE_SIZE = Mb512) Then 'Device is a Mb512 device (must be x16 mode) 
                        EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.Parallel_25bit) 'Uses CE line for A24 (X16 only) 
                    ElseIf (NOR_FLASH.AVAILABLE_SIZE > Mb512) Then '1Gbit or 2Gbit
                        If Not FCUSB.HWBOARD = FCUSB_BOARD.Classic_XPORT Then
                            Dim MbitStr As String = Utilities.FormatToMegabits(NOR_FLASH.FLASH_SIZE).Replace(" ", "")
                            RaiseEvent PrintConsole(String.Format(RM.GetString("ext_large_flash_detected"), MbitStr))
                            EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.Parallel_25bit) 'Extension port can only read/write up to 512Mbit (in x18 mode)
                            NOR_FLASH.AVAILABLE_SIZE = Mb512
                        Else
                            EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.Parallel_27bit) 'xPort board supports A25,A26 lines
                        End If
                    End If
                    Select Case NOR_FLASH.WriteMode
                        Case MFP_PROG.IntelSharp
                            EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.Type4) 'SA=0x50;SA=0x60;SA=0xD0(SR.7)SA=0x20;SA=0xD0(SR.7)
                            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type4) 'SA=0x40;SA=DATA;SR.7
                        Case MFP_PROG.BypassMode 'Writes 64 bytes using ByPass sequence
                            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type5) '(Bypass) 0x555=0xAA;0x2AA=55;0x555=20;(0x00=0xA0;SA=DATA;...)0x00=0x90;0x00=0x00
                        Case MFP_PROG.PageMode 'Writes an entire page of data (128 bytes etc.)
                            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type6)
                        Case MFP_PROG.Buffer1 'Writes to a buffer that is than auto-programmed
                            EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.Type4) 'SA=0x50;SA=0x60;SA=0xD0,SR.7,SA=0x20;SA=0xD0,SR.7
                            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type7)
                        Case MFP_PROG.Buffer2
                            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type8)
                    End Select
                    WaitForReady()
                ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
                    RaiseEvent PrintConsole(String.Format(RM.GetString("ext_page_size"), MyFlashDevice.PAGE_SIZE, DirectCast(MyFlashDevice, SLC_NAND_Flash).EXT_PAGE_SIZE))
                    Dim nand_mem As SLC_NAND_Flash = DirectCast(MyFlashDevice, SLC_NAND_Flash)
                    If nand_mem.IFACE = ND_IF.X8_3V Then
                        RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NAND (X8 3.3V)")
                    ElseIf nand_mem.IFACE = ND_IF.X8_1V8 Then
                        RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NAND (X8 1.8V)")
                    ElseIf nand_mem.IFACE = ND_IF.X16_3V Then
                        RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NAND (X16 3.3V)")
                        RaiseEvent PrintConsole("This NAND device uses X16 IO and is not compatible with this programmer")
                        MyFlashStatus = DeviceStatus.NotCompatible
                        Return False
                    ElseIf nand_mem.IFACE = ND_IF.X16_1V8 Then
                        RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NAND (X16 1.8V)")
                        RaiseEvent PrintConsole("This NAND device uses X16 IO and is not compatible with this programmer")
                        MyFlashStatus = DeviceStatus.NotCompatible
                        Return False
                    End If
                    EXPIO_SETUP_ERASESECTOR(E_EXPIO_SECTOR.NAND)
                    NAND_SetupHandlers()
                    FCUSB.NAND_IF.CreateMap(nand_mem.FLASH_SIZE, nand_mem.PAGE_SIZE, nand_mem.EXT_PAGE_SIZE, nand_mem.BLOCK_SIZE)
                    FCUSB.NAND_IF.EnableBlockManager() 'If enabled
                    FCUSB.NAND_IF.ProcessMap()
                ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.FWH_NOR Then
                    'Nothing to setup
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

    Public Function EPROM_Init() As Boolean
        MyFlashDevice = Nothing
        If Not EXPIO_SETUP_USB(EXPIO_Mode.Setup) Then
            RaiseEvent PrintConsole(RM.GetString("ext_unable_to_connect_to_board"))
            MyFlashStatus = DeviceStatus.ExtIoNotConnected
            SetStatus(RM.GetString("ext_not_detected"))
            Return False
        Else
            RaiseEvent PrintConsole(RM.GetString("ext_board_initalized"))
        End If
        MyFlashDevice = FlashDatabase.FindDevice(MySettings.OTP_MFG, MySettings.OTP_ID, 0, MemoryType.OTP_EPROM)
        Dim s27_eprom As OTP_EPROM = DirectCast(MyFlashDevice, OTP_EPROM)
        EXPIO_SETUP_DELAY(MFP_DELAY.uS)
        EXPIO_SETUP_WRITEDELAY(s27_eprom.HARDWARE_DELAY)
        If IsIFACE8X(s27_eprom.IFACE) Then
            MyAdapter = AdatperType.X8_Type1
            EXPIO_SETUP_USB(EXPIO_Mode.NOR_x8)
            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type9)
            RaiseEvent PrintConsole("Device mode set to EPROM (8-bit)")
        Else
            MyAdapter = AdatperType.X16_Type1
            EXPIO_SETUP_USB(EXPIO_Mode.NOR_x16)
            EXPIO_SETUP_WRITEDATA(E_EXPIO_WRITEDATA.Type10)
            RaiseEvent PrintConsole("Device mode set to EPROM (16-bit)")
        End If
        RaiseEvent PrintConsole("Memory name: " & MyFlashDevice.NAME & ", size: " & Format(MyFlashDevice.FLASH_SIZE, "#,###") & " bytes")
        DirectCast(MyFlashDevice, OTP_EPROM).IS_BLANK = True
        'DirectCast(MyFlashDevice, OTP_EPROM).IS_BLANK = EPROM_BlankCheck()
        SetStatus("EPROM mode ready for operation")
        MyFlashStatus = DeviceStatus.Supported
        Return True
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
            If MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
                Dim d As SLC_NAND_Flash = DirectCast(MyFlashDevice, SLC_NAND_Flash)
                Dim available_pages As Long = FCUSB.NAND_IF.MAPPED_PAGES
                If MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Combined Then
                    Return (available_pages * (d.PAGE_SIZE + d.EXT_PAGE_SIZE))
                Else
                    Return (available_pages * d.PAGE_SIZE)
                End If
            ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                Dim NOR_FLASH As MFP_Flash = DirectCast(MyFlashDevice, MFP_Flash)
                Return NOR_FLASH.AVAILABLE_SIZE
            ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.OTP_EPROM Then
                Dim NOR_FLASH As OTP_EPROM = DirectCast(MyFlashDevice, OTP_EPROM)
                Return NOR_FLASH.FLASH_SIZE
            ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.FWH_NOR Then
                Dim fwh_device As FWH_Flash = DirectCast(MyFlashDevice, FWH_Flash)
                Return fwh_device.FLASH_SIZE
            Else
                Return Me.MyFlashDevice.FLASH_SIZE
            End If
        End Get
    End Property

    Public Function ReadData(ByVal logical_address As Long, ByVal data_count As UInt32, Optional ByVal memory_area As FlashArea = FlashArea.Main) As Byte() Implements MemoryDeviceUSB.ReadData
        If MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
            Dim nand_dev As SLC_NAND_Flash = DirectCast(MyFlashDevice, SLC_NAND_Flash)
            Dim page_addr As UInt32 'This is the page address
            Dim page_offset As UInt16 'this is the start offset within the page
            Dim page_size As UInt32
            If (memory_area = FlashArea.Main) Then
                page_addr = Math.Floor(logical_address / MyFlashDevice.PAGE_SIZE)
                page_size = nand_dev.PAGE_SIZE
                page_offset = logical_address - (page_addr * MyFlashDevice.PAGE_SIZE)
            ElseIf (memory_area = FlashArea.OOB) Then
                page_addr = Math.Floor(logical_address / nand_dev.EXT_PAGE_SIZE)
                page_offset = logical_address - (page_addr * nand_dev.EXT_PAGE_SIZE)
                page_size = nand_dev.EXT_PAGE_SIZE
            ElseIf (memory_area = FlashArea.All) Then   'we need to adjust large address to logical address
                Dim full_page_size As UInt32 = (MyFlashDevice.PAGE_SIZE + nand_dev.EXT_PAGE_SIZE)
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
                Array.Copy(data, 0, data_out, data_ptr, data.Length)
                data_ptr += packet_size
                data_count -= packet_size
                page_addr += Math.Ceiling(bytes_left_in_block / page_size)
                page_offset = 0
            Loop
            Return data_out
        ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.FWH_NOR Then
            Return ReadData_FWH(logical_address, data_count)
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
                Return ReadBulk_NOR(logical_address, data_count)
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
            Dim w_data As UInt32 = 0
            If die_id = 0 Then
                w_data = CURRENT_ADDR_MODE
            Else
                w_data = ExtPort.E_EXPIO_WRADDR.Parallel_CE2 Or ((MySettings.MULTI_CE + 17) << 16)
            End If
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.EXPIO_MODE_ADDRESS, Nothing, w_data)
            Utilities.Sleep(10)
            Me.DIE_SELECTED = die_id
        End If
        count -= buffer_size
        flash_offset += buffer_size
        Return die_addr
    End Function

    Public Function Sector_Erase(ByVal sector_index As UInt32, Optional ByVal memory_area As FlashArea = FlashArea.Main) As Boolean Implements MemoryDeviceUSB.Sector_Erase
        If Not MyFlashDevice.ERASE_REQUIRED Then Return True
        If MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
            Dim pages_per_block As UInt32 = (DirectCast(MyFlashDevice, SLC_NAND_Flash).BLOCK_SIZE / MyFlashDevice.PAGE_SIZE)
            Dim page_addr As UInt32 = (pages_per_block * sector_index)
            Dim local_page_addr As UInt32 = FCUSB.NAND_IF.GetPageMapping(page_addr)
            Return FCUSB.NAND_IF.ERASEBLOCK(local_page_addr, memory_area, MySettings.NAND_Preserve)
        ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.FWH_NOR Then
            Dim fwh_device As FWH_Flash = DirectCast(MyFlashDevice, FWH_Flash)
            Dim Result As Boolean = False
            Dim Logical_Address As UInt32 = 0
            If (sector_index > 0) Then
                For i As UInt32 = 0 To sector_index - 1
                    Dim s_size As UInt32 = SectorSize(i)
                    Logical_Address += s_size
                Next
            End If
            Dim erase_cmd As UInt32 = fwh_device.ERASE_CMD
            Dim setup_data() As Byte = GetSetupPacket_NOR(Logical_Address, 0, 0)
            Result = FCUSB.USB_SETUP_BULKOUT(USBREQ.EXPIO_SECTORERASE, setup_data, Nothing, (erase_cmd << 16))
            If Not Result Then Return False
            Utilities.Sleep(50)
            Dim blank_result As Boolean = BlankCheck(Logical_Address)
            Return blank_result
        Else
            Dim nor_device As MFP_Flash = DirectCast(MyFlashDevice, MFP_Flash)
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
                    EXPIO_VPP_START() 'Enables +12V for supported devices
                    Dim Result As Boolean = False
                    Try
                        Dim sector_start_addr As UInt32 = 0
                        If Me.MULTI_DIE Then
                            sector_start_addr = GetAddressForMultiDie(Logical_Address, 0, 0)
                        Else
                            sector_start_addr = Logical_Address
                        End If
                        Dim setup_data() As Byte = GetSetupPacket_NOR(sector_start_addr, 0, 0)
                        Result = FCUSB.USB_SETUP_BULKOUT(USBREQ.EXPIO_SECTORERASE, setup_data, Nothing, 0)
                    Catch ex As Exception
                    End Try
                    EXPIO_VPP_STOP()
                    If Not Result Then Return False
                    If nor_device.DELAY_MODE = MFP_DELAY.DQ7 Or nor_device.DELAY_MODE = MFP_DELAY.SR1 Or nor_device.DELAY_MODE = MFP_DELAY.SR2 Then
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

    Public Function WriteData(ByVal logical_address As Long, ByVal data_to_write() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        If (MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND) Then
            Dim page_addr As UInt32 = GetNandPageAddress(MyFlashDevice, logical_address, Params.Memory_Area)
            page_addr = FCUSB.NAND_IF.GetPageMapping(page_addr) 'Adjusts the page to point to a valid page
            Dim result As Boolean = FCUSB.NAND_IF.WRITEPAGE(page_addr, data_to_write, Params.Memory_Area) 'We will write the whole block instead
            FCUSB.USB_WaitForComplete()
            Return result
        ElseIf (MyFlashDevice.FLASH_TYPE = MemoryType.OTP_EPROM) Then
            Return WriteData_EPROM(logical_address, data_to_write, Params)
        ElseIf (MyFlashDevice.FLASH_TYPE = MemoryType.FWH_NOR) Then
            Return WriteData_FWH(logical_address, data_to_write, Params)
        Else
            Dim nor_device As MFP_Flash = DirectCast(MyFlashDevice, MFP_Flash)
            Try
                EXPIO_VPP_START()
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
                    logical_address += data.Length
                    DataToWrite -= data.Length
                    FCUSB.USB_WaitForComplete()
                Next
                If nor_device.DELAY_MODE = MFP_DELAY.DQ7 Or nor_device.DELAY_MODE = MFP_DELAY.SR1 Or nor_device.DELAY_MODE = MFP_DELAY.SR2 Then
                    FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WAIT) 'Calls the assigned WAIT function (uS, mS, SR, DQ7)
                    FCUSB.USB_WaitForComplete() 'Checks for WAIT flag to clear
                Else
                    Utilities.Sleep(DirectCast(MyFlashDevice, MFP_Flash).SOFTWARE_DELAY)
                End If
            Catch ex As Exception
            Finally
                EXPIO_VPP_STOP()
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

    Private Function WriteData_EPROM(ByVal logical_address As UInt32, ByVal data_to_write() As Byte, ByRef Params As WriteParameters) As Boolean
        Dim eprom_device As OTP_EPROM = DirectCast(MyFlashDevice, OTP_EPROM)
        Try
            EXPIO_VPP_START()
            Utilities.Sleep(250)
            Dim PacketSize As UInt32 = 1024
            Dim BytesWritten As UInt32 = 0
            Dim DataToWrite As UInt32 = data_to_write.Length
            Dim Loops As Integer = CInt(Math.Ceiling(DataToWrite / PacketSize)) 'Calcuates iterations
            For i As Integer = 0 To Loops - 1
                If Params.AbortOperation Then Return False
                Dim BufferSize As Integer = DataToWrite
                If (BufferSize > PacketSize) Then BufferSize = PacketSize
                Dim data(BufferSize - 1) As Byte
                Array.Copy(data_to_write, (i * PacketSize), data, 0, data.Length)
                Dim ReturnValue As Boolean = WriteBulk_NOR(logical_address, data)
                If (Not ReturnValue) Then Return False
                Utilities.Sleep(10)
                logical_address += data.Length
                DataToWrite -= data.Length
                BytesWritten += data.Length
            Next
        Catch ex As Exception
        Finally
            EXPIO_VPP_STOP()
        End Try
        Return True
    End Function

    Public Sub WaitForReady() Implements MemoryDeviceUSB.WaitUntilReady
        If MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
            Utilities.Sleep(10) 'Checks READ/BUSY# pin
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

    Public Function Sector_Write(ByVal sector_index As UInt32, ByVal data() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.Sector_Write
        Dim Addr32 As UInteger = Me.SectorFind(sector_index, Params.Memory_Area)
        Return WriteData(Addr32, data, Params)
    End Function

    Public Function Sector_Count() As UInt32 Implements MemoryDeviceUSB.Sector_Count
        If MySettings.MUTLI_NOR Then
            Return (MyFlashDevice.Sector_Count * 2)
        Else
            Return MyFlashDevice.Sector_Count
        End If
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        Try
            If MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND Then
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
                    EXPIO_VPP_START()
                    Dim wm As MFP_PROG = DirectCast(MyFlashDevice, MFP_Flash).WriteMode
                    If (wm = MFP_PROG.IntelSharp Or wm = MFP_PROG.Buffer1) Then
                        Dim BlockCount As Integer = DirectCast(MyFlashDevice, MFP_Flash).Sector_Count
                        RaiseEvent SetProgress(0)
                        For i = 0 To BlockCount - 1
                            If (Not Sector_Erase(i, 0)) Then
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
                        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CHIPERASE)
                        Utilities.Sleep(200) 'Perform blank check
                        For i = 0 To 179 '3 minutes
                            If BlankCheck(0) Then Return True
                            Utilities.Sleep(900)
                        Next
                        Return False 'Timeout (device erase failed)
                    End If
                Catch ex As Exception
                Finally
                    EXPIO_VPP_STOP()
                End Try
            End If
        Catch ex As Exception
        Finally
            If MyFlashDevice.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                Dim nor_device As MFP_Flash = DirectCast(MyFlashDevice, MFP_Flash)
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
                Return DirectCast(MyFlashDevice, MFP_Flash).GetSectorSize(sector)
            ElseIf FCUSB.EXT_IF.MyFlashDevice.FLASH_TYPE = MemoryType.OTP_EPROM Then
                Return 8192 'Program 8KB at a time
            ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.FWH_NOR Then
                Return DirectCast(MyFlashDevice, FWH_Flash).SECTOR_SIZE
            Else
                Dim nand_dev As SLC_NAND_Flash = DirectCast(MyFlashDevice, SLC_NAND_Flash)
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

    Public Sub NAND_ReadPages(ByVal page_addr As UInt32, ByVal page_offset As UInt16, ByVal data_count As UInt32, ByVal memory_area As FlashArea, ByRef data() As Byte)
        data = ReadBulk_NAND(page_addr, page_offset, data_count, memory_area)
    End Sub

    Private Sub NAND_WritePages(ByVal page_addr As UInt32, ByVal main() As Byte, ByVal oob() As Byte, ByVal memory_area As FlashArea, ByRef write_result As Boolean)
        write_result = WriteBulk_NAND(page_addr, main, oob, memory_area)
    End Sub

    Private Sub NAND_EraseSector(ByVal page_addr As UInt32, ByRef erase_result As Boolean)
        Dim setup_data() As Byte = GetSetupPacket_NAND(page_addr, 0, 0, FlashArea.All)
        erase_result = FCUSB.USB_SETUP_BULKOUT(USBREQ.EXPIO_SECTORERASE, setup_data, Nothing, 1)
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
        Dim result As Boolean = FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.EXPIO_NAND_SR, result_data)
        Return result_data(0) 'E0 11100000
    End Function

#End Region

#Region "EXPIO SETUP"

    Public Enum E_EXPIO_WRADDR As UInt16
        Parallel_24bit = 1 'Standard 24-bit address (up to 128Mbit devices)
        Parallel_DQ15 = 2 'TSOP48 X8 - DQ15 is used for A-1
        Parallel_25bit = 3 'TSOP-56 256Mbit devices (uses A0 to A24) CE=A24
        Parallel_27bit = 4 'TSOP-56 1Gbit-2Gbit devices (adds A25,A26) - Only xPort compatible
        Parallel_CE2 = 5 '64mbit samsung dual-die device
    End Enum

    Public Enum E_EXPIO_IDENT As UInt16
        Type1 = 1 '(0x5555=0xAA;0x2AAA=0x55;0x5555=0x90) READ 0x00,0x01,0x02 (0x5555=0xAA;0x2AAA=0x55;0x5555=0xF0)
        Type2 = 2 '(0x555=0xAA;2AA=0x55;0x555=0x90) READ 0x00,0x01,0x0E,0x0F,0x03 (0x00=0x00F0), x16 (typically)
        Type3 = 3 '(0xAAA=0xAA;0x555=0x55;0xAAA=0x90)
        NAND = 4
        HF = 5
    End Enum

    Public Enum E_EXPIO_SECTOR As UInt16
        Type1 = 1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0x80;0x5555=0xAA;0x2AAA=0x55;SA=0x30 (used by older devices)
        Type2 = 2 '0xAAA=0xAA;0x555=0x55;0xAAA=0x80;0xAAA=0xAA;0x555=0x55;SA=0x30 (used by x8 devices)
        Type3 = 3 '0x555=0xAA,0x2AA=0x55,0x555=0x80,0x555=0xAA,0x2AA=0x55;SA=0x30 (used by x16 devices)
        Type4 = 4 'SA=0x50;SA=0x60;SA=0xD0,SR.7,SA=0x20;SA=0xD0,SR.7 (used by Intel/Sharp devices)
        NAND = 5
        HF = 6
    End Enum

    Public Enum E_EXPIO_CHIPERASE As UInt16
        Type1 = 1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0x80;0x5555=0xAA;0x2AAA=0x55;0x5555=0x10 (used by older devices)
        Type2 = 2 '0xAAA=0xAA;0x555=0x55;0xAAA=0x80;0xAAA=0xAA;0x555=0x55;0xAAA=0x10 (used by x8 devices)
        Type3 = 3 '0x555=0xAA;0x2AA=0x55;0x555=0x80;0x555=0xAA;0x2AA=0x55;0x555=0x10 (used by x16 devices)
        Type4 = 4 '0x00=0x30;0x00=0xD0;
        HF = 5
    End Enum

    Private Enum E_EXPIO_WRITEDATA As UInt16
        Type1 = 1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0xA0;SA=DATA;DELAY
        Type2 = 2 '0xAAA=0xAA;0x555=0x55;0xAAA=0xA0;SA=DATA;DELAY
        Type3 = 3 '0x555=0xAA;0x2AA=0x55;0x555=0xA0,SA=DATA;DELAY
        Type4 = 4 'SA=0x40;SA=DATA;SR.7
        Type5 = 5 '(Bypass) 0x555=0xAA;0x2AA=55;0x555=20;(0x00=0xA0;SA=DATA;...)0x00=0x90;0x00=0x00
        Type6 = 6 '(Page)0x5555,0x2AAA,0x5555;(BA/DATA)
        Type7 = 7 '(Buffer)0xE8...0xD0
        Type8 = 8 '(buffer)0x555=0xAA,0x2AA=0x55,SA=0x25,SA=(WC-1)..
        Type9 = 9 '(VPP=HIGH) ADDR/DATA (EPROM_8BIT)
        Type10 = 10 '(VPP=HIGH) ADDR/DATA (EPROM_16BIT)
    End Enum

    Private Function EXPIO_SETUP_USB(ByVal mode As EXPIO_Mode) As Boolean
        Try
            Dim result_data(0) As Byte
            Dim setup_data As UInt32 = mode Or (10 << 16)
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_INIT, result_data, setup_data)
            If Not result Then Return False
            If (result_data(0) = &H17) Then 'Extension port returns 0x17 if it can communicate with the MCP23S17
                Threading.Thread.Sleep(50) 'Give the USB time to change modes
                Return True 'Communication successful
            Else
                Return False
            End If
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_READIDENT(ByVal mode As E_EXPIO_IDENT) As Boolean
        Try
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.EXPIO_MODE_IDENT, Nothing, mode)
            Threading.Thread.Sleep(25)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function EXPIO_SETUP_WRITEADDRESS(ByVal mode As E_EXPIO_WRADDR) As Boolean
        Try
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.EXPIO_MODE_ADDRESS, Nothing, mode)
            CURRENT_ADDR_MODE = mode
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_ERASESECTOR(ByVal mode As E_EXPIO_SECTOR) As Boolean
        Try
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.EXPIO_MODE_ERSCR, Nothing, mode)
            Threading.Thread.Sleep(25)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_CHIPERASE(ByVal mode As E_EXPIO_CHIPERASE) As Boolean
        Try
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.EXPIO_MODE_ERCHP, Nothing, mode)
            Threading.Thread.Sleep(25)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_WRITEDATA(ByVal mode As E_EXPIO_WRITEDATA) As Boolean
        Try
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_MODE_WRITE, Nothing, mode)
            CURRENT_WRITE_MODE = mode
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_DELAY(ByVal delay_mode As MFP_DELAY) As Boolean
        Try
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.EXPIO_MODE_DELAY, Nothing, delay_mode)
            Threading.Thread.Sleep(25)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_WRITEDELAY(ByVal delay_cycles As UInt16) As Boolean
        Try
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.EXPIO_DELAY, Nothing, delay_cycles)
            Threading.Thread.Sleep(25)
            Return result
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Sub EXPIO_VPP_START()
        'We should only allow this for devices that have a 12V option/chip
        Dim VPP_FEAT_EN As Boolean = False
        Dim if_type As MFP_IF
        If MyFlashDevice.GetType Is GetType(MFP_Flash) Then
            if_type = DirectCast(MyFlashDevice, MFP_Flash).IFACE
        ElseIf MyFlashDevice.GetType Is GetType(OTP_EPROM) Then
            if_type = DirectCast(MyFlashDevice, OTP_EPROM).IFACE
        Else
            Exit Sub
        End If
        If if_type = MFP_IF.X16_5V_12V Then
            VPP_FEAT_EN = True
        ElseIf if_type = MFP_IF.X16_3V_12V Then
            VPP_FEAT_EN = True
        ElseIf if_type = MFP_IF.X8_5V_12V Then
            VPP_FEAT_EN = True
        End If
        If VPP_FEAT_EN Then
            EXPIO_CHIPENABLE_LOW() 'VPP=12V
            Utilities.Sleep(100) 'We need to wait
        End If
    End Sub

    Private Sub EXPIO_VPP_STOP()
        'We should only allow this for devices that have a 12V option/chip
        Dim VPP_FEAT_EN As Boolean = False
        Dim if_type As MFP_IF
        If MyFlashDevice.GetType Is GetType(MFP_Flash) Then
            if_type = DirectCast(MyFlashDevice, MFP_Flash).IFACE
        ElseIf MyFlashDevice.GetType Is GetType(OTP_EPROM) Then
            if_type = DirectCast(MyFlashDevice, OTP_EPROM).IFACE
        Else
            Exit Sub
        End If
        If if_type = MFP_IF.X16_5V_12V Then
            VPP_FEAT_EN = True
        ElseIf if_type = MFP_IF.X16_3V_12V Then
            VPP_FEAT_EN = True
        ElseIf if_type = MFP_IF.X8_5V_12V Then
            VPP_FEAT_EN = True
        End If
        If VPP_FEAT_EN Then
            Utilities.Sleep(100) 'We need to wait
            EXPIO_CHIPENABLE_HIGH() 'VPP=12V
        End If
    End Sub

    Private Sub EXPIO_CHIPENABLE_HIGH()
        Try
            FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CE_HIGH)
        Catch ex As Exception
        End Try
    End Sub

    Private Sub EXPIO_CHIPENABLE_LOW()
        Try
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.EXPIO_CE_LOW)
        Catch ex As Exception
        End Try
    End Sub

    Private Sub EXPIO_PrintCurrentWriteMode()
        If (MyFlashDevice.FLASH_TYPE = MemoryType.SLC_NAND) Then
        Else
            Select Case CURRENT_WRITE_MODE
                Case E_EXPIO_WRITEDATA.Type1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0xA0;SA=DATA;DELAY
                    RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Type-1 (Byte Program)")
                Case E_EXPIO_WRITEDATA.Type2 '0xAAA=0xAA;0x555=0x55;0xAAA=0xA0;SA=DATA;DELAY
                    RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Type-2 (Word Program)")
                Case E_EXPIO_WRITEDATA.Type3 '0x555=0xAA;0x2AA=0x55;0x555=0xA0,SA=DATA;DELAY
                    RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Type-3 (Word Program)")
                Case E_EXPIO_WRITEDATA.Type4 'SA=0x40;SA=DATA;SR.7
                    RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Type-4 (Auto-Word Program)")
                Case E_EXPIO_WRITEDATA.Type5 '0x555=0xAA;0x2AA=55;0x555=20;(0x00=0xA0;SA=DATA;...)0x00=0x90;0x00=0x00
                    RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Type-5 (Bypass Mode)")
                Case E_EXPIO_WRITEDATA.Type6 '0x5555,0x2AAA,0x5555;(BA/DATA)
                    RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Type-6 (Page Write)")
                Case E_EXPIO_WRITEDATA.Type7 '0xE8...0xD0
                    RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Type-7 (Write-to-Buffer)")
                Case E_EXPIO_WRITEDATA.Type8 '0x555=0xAA,0x2AA=0x55,SA=0x25,SA=(WC-1)..
                    RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Type-8 (Write-to-Buffer)")
            End Select
        End If
    End Sub

#End Region

    Public Enum AdatperType As Byte
        X8_Type1 = 10 'PLCC32/DIP32
        X8_Type2 = 11 'TSOP48 X8 only device (i.e. SST39VF1681)
        X16_Type1 = 12 'SO-44 X16 devices (legacy)
        X16_Type2 = 13 'TSOP48 X16
        NAND = 14 'SLC X8 NAND
        FWH = 15 'Firmware Hub
    End Enum

    Public Enum EXPIO_Mode
        Setup = 0
        NOR_x8 = 1
        NOR_x16 = 2
        NAND_x8 = 3
        FWH = 4
        HYPERFLASH = 5
    End Enum

    Private Function DetectFlashDevice() As Boolean
        RaiseEvent PrintConsole(RM.GetString("ext_detecting_device"))
        CHIPID_MFG = 0
        CHIPID_PART = 0
        Dim LAST_DETECT As FlashDetectResult = Nothing
        Dim NAND_DETECT As FlashDetectResult = DetectFlashByMode(AdatperType.NAND)
        Me.CHIPID_DETECT = NAND_DETECT
        If Me.CHIPID_DETECT.Successful Then
            Dim d() As Device = FlashDatabase.FindDevices(Me.CHIPID_DETECT.MFG, Me.CHIPID_DETECT.ID1, Me.CHIPID_DETECT.ID2, MemoryType.SLC_NAND)
            If (d.Count > 0) Then
                RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "SLC NAND"))
                CHIPID_MFG = Me.CHIPID_DETECT.MFG
                CHIPID_PART = (CUInt(Me.CHIPID_DETECT.ID1) << 16) Or (Me.CHIPID_DETECT.ID2)
                MyAdapter = AdatperType.NAND
                Return True
            Else
                LAST_DETECT = Me.CHIPID_DETECT
            End If
        End If
        If FCUSB.HWBOARD = FCUSB_BOARD.Mach1 Then Return False 'Mach1 can only detect NAND devices
        Dim NORX16T2 As FlashDetectResult = DetectFlashByMode(AdatperType.X16_Type2) 'TSOP-48/56 devices
        Me.CHIPID_DETECT = NORX16T2
        If Me.CHIPID_DETECT.Successful Then
            Dim d() As Device = FlashDatabase.FindDevices(Me.CHIPID_DETECT.MFG, Me.CHIPID_DETECT.ID1, Me.CHIPID_DETECT.ID2, MemoryType.PARALLEL_NOR)
            If (d.Count > 0) AndAlso IsIFACE16X(DirectCast(d(0), MFP_Flash).IFACE) Then
                RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "NOR X16 (Type-2)"))
                CHIPID_MFG = Me.CHIPID_DETECT.MFG
                CHIPID_PART = (CUInt(Me.CHIPID_DETECT.ID1) << 16) Or (Me.CHIPID_DETECT.ID2)
                MyAdapter = AdatperType.X16_Type2
                Return True
            Else
                LAST_DETECT = Me.CHIPID_DETECT
            End If
        End If
        Dim NORX16T1 As FlashDetectResult = DetectFlashByMode(AdatperType.X16_Type1) 'Legacy SO-44 devices
        Me.CHIPID_DETECT = NORX16T1
        If Me.CHIPID_DETECT.Successful Then
            Dim d() As Device = FlashDatabase.FindDevices(Me.CHIPID_DETECT.MFG, Me.CHIPID_DETECT.ID1, 0, MemoryType.PARALLEL_NOR)
            If (d.Count > 0) AndAlso IsIFACE16X(DirectCast(d(0), MFP_Flash).IFACE) Then
                RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "NOR X16 (Type-1)"))
                CHIPID_MFG = Me.CHIPID_DETECT.MFG
                CHIPID_PART = (CUInt(Me.CHIPID_DETECT.ID1) << 16)
                MyAdapter = AdatperType.X16_Type1
                Return True
            Else
                LAST_DETECT = Me.CHIPID_DETECT
            End If
        End If
        Dim NORX8T1 As FlashDetectResult = DetectFlashByMode(AdatperType.X8_Type1) 'PLCC32/DIP32
        Me.CHIPID_DETECT = NORX8T1
        If Me.CHIPID_DETECT.Successful Then
            Dim d() As Device = FlashDatabase.FindDevices(Me.CHIPID_DETECT.MFG, Me.CHIPID_DETECT.ID1, 0, MemoryType.PARALLEL_NOR)
            If (d.Count > 0) AndAlso IsIFACE8X(DirectCast(d(0), MFP_Flash).IFACE) Then
                RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "NOR X8 (Type-1)"))
                CHIPID_MFG = Me.CHIPID_DETECT.MFG
                CHIPID_PART = (CUInt(Me.CHIPID_DETECT.ID1) << 16)
                MyAdapter = AdatperType.X8_Type1
                Return True
            Else
                LAST_DETECT = Me.CHIPID_DETECT
            End If
        End If
        Dim NORX8T2 As FlashDetectResult = DetectFlashByMode(AdatperType.X8_Type2) 'TSOP48 (X8)
        Me.CHIPID_DETECT = NORX8T2
        If Me.CHIPID_DETECT.Successful Then
            Dim d() As Device = FlashDatabase.FindDevices(Me.CHIPID_DETECT.MFG, Me.CHIPID_DETECT.ID1, 0, MemoryType.PARALLEL_NOR)
            If (d.Count > 0) AndAlso IsIFACE8X(DirectCast(d(0), MFP_Flash).IFACE) Then
                RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "NOR X8 (Type-2)"))
                CHIPID_MFG = Me.CHIPID_DETECT.MFG
                CHIPID_PART = (CUInt(Me.CHIPID_DETECT.ID1) << 16)
                MyAdapter = AdatperType.X8_Type2
                Return True
            Else
                LAST_DETECT = Me.CHIPID_DETECT
            End If
        End If
        If Me.FWH_SUPPORTED Then
            Dim FWH_MEM As FlashDetectResult = DetectFlashByMode(AdatperType.FWH)
            Me.CHIPID_DETECT = FWH_MEM
            If Me.CHIPID_DETECT.Successful Then
                Dim d() As Device = FlashDatabase.FindDevices(Me.CHIPID_DETECT.MFG, Me.CHIPID_DETECT.ID1, 0, MemoryType.FWH_NOR)
                If (d.Count > 0) Then
                    RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "FWH"))
                    CHIPID_MFG = Me.CHIPID_DETECT.MFG
                    CHIPID_PART = (CUInt(Me.CHIPID_DETECT.ID1) << 16)
                    MyAdapter = AdatperType.FWH
                    Return True
                Else
                    LAST_DETECT = Me.CHIPID_DETECT
                End If
            End If
        End If
        If (LAST_DETECT IsNot Nothing) Then
            Me.CHIPID_DETECT = LAST_DETECT
            CHIPID_MFG = LAST_DETECT.MFG
            CHIPID_PART = (CUInt(LAST_DETECT.ID1) << 16) Or (LAST_DETECT.ID2)
            Return True 'Found, but not in library
        End If
        Return False 'No devices detected
    End Function

    Private Function DetectFlashByMode(ByVal mode As AdatperType) As FlashDetectResult
        Select Case mode
            Case AdatperType.X8_Type1
                EXPIO_SETUP_USB(EXPIO_Mode.NOR_x8)
                EXPIO_SETUP_READIDENT(E_EXPIO_IDENT.Type1) '(0x5555=0xAA;0x2AAA=0x55;0x5555=0x90)
                CURRENT_ADDR_MODE = E_EXPIO_WRADDR.Parallel_24bit
            Case AdatperType.X8_Type2 'TSOP48 (X8)
                EXPIO_SETUP_USB(EXPIO_Mode.NOR_x8)
                EXPIO_SETUP_READIDENT(E_EXPIO_IDENT.Type3) '(0x555=0xAA;2AA=0x55;0x555=0x90)
                EXPIO_SETUP_WRITEADDRESS(E_EXPIO_WRADDR.Parallel_DQ15) 'DQ15 is used for A-1
                CURRENT_ADDR_MODE = E_EXPIO_WRADDR.Parallel_DQ15
            Case AdatperType.X16_Type1
                EXPIO_SETUP_USB(EXPIO_Mode.NOR_x16)
                EXPIO_SETUP_READIDENT(E_EXPIO_IDENT.Type1) '(0x5555=0xAA;0x2AAA=0x55;0x5555=0x90)
                CURRENT_ADDR_MODE = E_EXPIO_WRADDR.Parallel_24bit
            Case AdatperType.X16_Type2
                EXPIO_SETUP_USB(EXPIO_Mode.NOR_x16)
                EXPIO_SETUP_READIDENT(E_EXPIO_IDENT.Type2) '(0x555=0xAA;2AA=0x55;0x555=0x90)
                CURRENT_ADDR_MODE = E_EXPIO_WRADDR.Parallel_24bit
            Case AdatperType.NAND
                EXPIO_SETUP_USB(EXPIO_Mode.NAND_x8)
            Case AdatperType.FWH
                EXPIO_SETUP_USB(EXPIO_Mode.FWH)
        End Select
        Threading.Thread.Sleep(20) 'Delay
        Return DetectFlash()
    End Function
    'contains AutoSelect Device ID and some CFI-ID space
    Public Class FlashDetectResult
        Public Successful As Boolean
        Public MFG As Byte   '0
        Public ID1 As UInt16 '1 2
        Public ID2 As UInt16 '3 4
        Public BOOT_LOCK As Byte  '5
        Public SUPPORTED As Byte '6
        Public CFI_MULTI As Byte '7 'Max. number of bytes supported per write operation
    End Class

    Private Sub SetupFlashDevice()
        If (MyFlashDevice.FLASH_TYPE = MemoryType.PARALLEL_NOR) Or (MyFlashDevice.FLASH_TYPE = MemoryType.OTP_EPROM) Then
            Select Case DirectCast(MyFlashDevice, MFP_Flash).IFACE
                Case MFP_IF.X8_3V
                    RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X8 (3V)")
                    If MyAdapter = AdatperType.X16_Type1 Or MyAdapter = AdatperType.X16_Type2 Then
                        DetectFlashByMode(AdatperType.X8_Type2)
                        MyAdapter = AdatperType.X8_Type2
                    End If
                Case MFP_IF.X8_5V
                    RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X8 (5V)")
                    If MyAdapter = AdatperType.X16_Type1 Or MyAdapter = AdatperType.X16_Type2 Then
                        DetectFlashByMode(AdatperType.X8_Type2)
                        MyAdapter = AdatperType.X8_Type2
                    End If
                Case MFP_IF.X8_5V_12V
                    RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (5V/12V VPP)")
                Case MFP_IF.X16_3V
                    RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (3V)")
                Case MFP_IF.X16_3V_12V
                    RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (3V/12V VPP)")
                Case MFP_IF.X16_5V
                    RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (5V)")
                Case MFP_IF.X16_5V_12V
                    RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (5V/12V VPP)")
            End Select
        ElseIf (MyFlashDevice.FLASH_TYPE = MemoryType.FWH_NOR) Then
            RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR (FWH)")
        End If
    End Sub

    Private Function DetectFlash() As FlashDetectResult
        Dim result As New FlashDetectResult
        result.Successful = False
        Try
            Dim ident_data(7) As Byte 'Contains 8 bytes
            If Not FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_RDID, ident_data) Then Return result
            If ident_data(0) = 0 AndAlso ident_data(2) = 0 Then Return result '0x0000
            If ident_data(0) = &H90 AndAlso ident_data(2) = &H90 Then Return result '0x9090 
            If ident_data(0) = &H90 AndAlso ident_data(2) = 0 Then Return result '0x9000 
            If ident_data(0) = &HFF AndAlso ident_data(2) = &HFF Then Return result '0xFFFF 
            If ident_data(0) = &HFF AndAlso ident_data(2) = 0 Then Return result '0xFF00
            If ident_data(0) = &H1 AndAlso ident_data(1) = 0 AndAlso ident_data(2) = &H1 AndAlso ident_data(3) = 0 Then Return result '0x01000100
            result.MFG = ident_data(0)
            result.ID1 = (CUInt(ident_data(1)) << 8) Or CUInt(ident_data(2))
            result.ID2 = (CUInt(ident_data(3)) << 8) Or CUInt(ident_data(4))
            If result.ID1 = 0 AndAlso result.ID2 = 0 Then Return result
            result.BOOT_LOCK = ident_data(5)
            result.SUPPORTED = ident_data(6)
            result.CFI_MULTI = ident_data(7)
            result.Successful = True
        Catch ex As Exception
        End Try
        Return result
    End Function

    Private Function IsIFACE8X(input As MFP_IF) As Boolean
        Select Case input
            Case MFP_IF.X8_3V
                Return True
            Case MFP_IF.X8_5V
                Return True
            Case MFP_IF.X8_5V_12V
                Return True
            Case Else
                Return False
        End Select
    End Function

    Private Function IsIFACE16X(input As MFP_IF) As Boolean
        Select Case input
            Case MFP_IF.X16_3V
                Return True
            Case MFP_IF.X16_5V
                Return True
            Case MFP_IF.X16_5V_12V
                Return True
            Case Else
                Return False
        End Select
    End Function

    Private Function IsFlashX8Mode() As Boolean
        Select Case MyAdapter
            Case AdatperType.X8_Type1
                Return True
            Case AdatperType.X8_Type2
                Return True
            Case AdatperType.FWH
                Return True
            Case Else
                Return False
        End Select
    End Function

    Public Function ResetDevice() As Boolean
        Try
            If MyFlashDevice.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_RESET)
                Utilities.Sleep(50)
                Return result
            ElseIf MyFlashDevice.FLASH_TYPE = MemoryType.OTP_EPROM Then
                Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_RESET)
                Utilities.Sleep(50)
                Return result
            Else
                Return True 'Device does not have RESET mode
            End If
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function GetSetupPacket_NAND(page_addr As UInt32, page_offset As UInt16, transfer_size As UInt32, area As FlashArea) As Byte()
        Dim NAND_DEV As SLC_NAND_Flash = DirectCast(MyFlashDevice, SLC_NAND_Flash)
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

    Private Function GetSetupPacket_NAND_debug(page_addr As UInt32, page_offset As UInt16, transfer_size As UInt32, area As FlashArea) As Byte()
        Dim TX_NAND_ADDRSIZE As Byte = 4
        Dim TX_OOB_SIZE As UInt16 = 64
        Dim TX_MAIN_SIZE As UInt16 = 2048
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
        setup_data(10) = CByte(TX_MAIN_SIZE And 255)
        setup_data(11) = CByte((TX_MAIN_SIZE >> 8) And 255)
        setup_data(12) = CByte(TX_OOB_SIZE And 255)
        setup_data(13) = CByte((TX_OOB_SIZE >> 8) And 255)
        setup_data(14) = CByte(TX_MAIN_SIZE And 255)
        setup_data(15) = CByte((TX_MAIN_SIZE >> 8) And 255)
        setup_data(16) = CByte(TX_OOB_SIZE And 255)
        setup_data(17) = CByte((TX_OOB_SIZE >> 8) And 255)
        setup_data(18) = TX_NAND_ADDRSIZE
        setup_data(19) = area 'Area (0=main,1=spare,2=all), note: all ignores layout settings
        Return setup_data
    End Function

    Private Function GetSetupPacket_NOR(ByVal Address As UInt32, ByVal Count As UInt32, ByVal PageSize As UInt16) As Byte()
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

    Private Function BlankCheck(ByVal base_addr As UInt32) As Boolean
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

    Private Function ReadBulk_NOR(ByVal address As UInt32, ByVal count As UInt32) As Byte()
        Try
            Dim read_count As UInt32 = count
            Dim addr_offset As Boolean = False
            Dim count_offset As Boolean = False
            If (Not IsFlashX8Mode()) Then 'We are using x16 mode, we need to read from low addresses only
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
            Dim setup_data() As Byte = GetSetupPacket_NOR(address, read_count, MyFlashDevice.PAGE_SIZE)
            Dim data_out(read_count - 1) As Byte 'Bytes we want to read
            If MyFlashDevice.FLASH_TYPE = MemoryType.OTP_EPROM Then FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WE_LOW) : Utilities.Sleep(10)
            Dim result As Boolean = FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_READDATA, setup_data, data_out, 0)
            If MyFlashDevice.FLASH_TYPE = MemoryType.OTP_EPROM Then FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WE_HIGH)
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

    Private Function WriteBulk_NOR(ByVal address As UInt32, ByVal data_out() As Byte) As Boolean
        Try
            Dim setup_data() As Byte = GetSetupPacket_NOR(address, data_out.Length, MyFlashDevice.PAGE_SIZE)
            Dim result As Boolean = FCUSB.USB_SETUP_BULKOUT(USBREQ.EXPIO_WRITEDATA, setup_data, data_out, 0)
            Return result
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Function ReadBulk_NAND(ByVal page_addr As UInt32, ByVal page_offset As UInt16, ByVal count As UInt32, ByVal memory_area As FlashArea) As Byte()
        Try
            Dim result As Boolean
            If Me.ECC_READ_ENABLED AndAlso (memory_area = FlashArea.Main) Then 'We need to auto-correct data uisng ECC
                Dim NAND_DEV As SLC_NAND_Flash = DirectCast(MyFlashDevice, SLC_NAND_Flash)
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

    Private Function WriteBulk_NAND(ByVal page_addr As UInt32, main_data() As Byte, oob_data() As Byte, ByVal memory_area As FlashArea) As Boolean
        Try
            If main_data Is Nothing And oob_data Is Nothing Then Return False
            Dim NAND_DEV As SLC_NAND_Flash = DirectCast(MyFlashDevice, SLC_NAND_Flash)
            Dim page_size_tot As UInt16 = (MyFlashDevice.PAGE_SIZE + NAND_DEV.EXT_PAGE_SIZE)
            Dim page_aligned() As Byte = Nothing
            If memory_area = FlashArea.All Then 'Ignore OOB/SPARE
                oob_data = Nothing
                Dim total_pages As UInt32 = Math.Ceiling(main_data.Length / page_size_tot)
                ReDim page_aligned((total_pages * page_size_tot) - 1)
                For i = 0 To page_aligned.Length - 1 : page_aligned(i) = 255 : Next
                Array.Copy(main_data, 0, page_aligned, 0, main_data.Length)
            ElseIf memory_area = FlashArea.Main Then
                If Me.ECC_WRITE_ENABLED Then
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
            Dim pages_to_write As UInt32 = page_aligned.Length / page_size_tot
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
    'This is used to write data (8/16 bit) to the EXTIO IO (parallel NOR) port.
    Public Function WriteAddressData(ByVal addr As UInt32, ByVal data As UInt16)
        Dim addr_data(5) As Byte
        addr_data(0) = CByte((addr >> 24) And 255)
        addr_data(1) = CByte((addr >> 16) And 255)
        addr_data(2) = CByte((addr >> 8) And 255)
        addr_data(3) = CByte(addr And 255)
        addr_data(4) = CByte((data >> 8) And 255)
        addr_data(5) = CByte(data And 255)
        Return FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WRADRDATA, addr_data)
    End Function

End Class
