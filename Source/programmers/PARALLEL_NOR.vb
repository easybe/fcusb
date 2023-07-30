'COPYRIGHT EMBEDDED COMPUTERS LLC 2023 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB PRODUCTS
'CONTACT EMAIL: support@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: This is the main module that is loaded first.

Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB

Public Class PARALLEL_NOR : Implements MemoryDeviceUSB
    Private FCUSB As FCUSB_DEVICE
    Public Property MyFlashDevice As P_NOR
    Public Property MyFlashStatus As DeviceStatus = DeviceStatus.NotDetected
    Public Property CFI As NOR_CFI 'Contains CFI table information (NOR)
    Public Property MyAdapter As MEM_PROTOCOL 'This is the kind of socket adapter connected and the mode it is in
    Public Property DUALDIE_EN As Boolean = False 'Indicates two DIE are connected and using a CE
    Public Property DUALDIE_CE2 As Integer 'The Address pin that goes to the second chip-enable
    Public Property DIE_SELECTED As Integer = 0
    Private Property MAX_PACKET_SIZE As Integer 'Set by DeviceInit/EEPROM_Init
    Public Property ERASE_ALLOWED As Boolean = True
    Public Property MULTI_CE As Integer = 0
    Public Property X8_MODE_ONLY As Boolean = False

    Private FLASH_IDENT As FlashDetectResult

    Public Event PrintConsole(msg As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress

    Sub New(parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        Me.DIE_SELECTED = 0
        Me.DUALDIE_EN = False
        Me.MAX_PACKET_SIZE = 8192
        Me.MyFlashDevice = Nothing
        Me.CFI = Nothing
        Me.CURRENT_SECTOR_ERASE = E_PARALLEL_SECTOR.Standard
        Me.CURRENT_CHIP_ERASE = E_PARALLEL_CHIPERASE.Standard
        Me.CURRENT_WRITE_MODE = E_PARALLEL_WRITEDATA.Standard
        If Not ConfigureParallelBus(MEM_PROTOCOL.SETUP) Then
            RaiseEvent PrintConsole("Parallel NOR mode failed to initialize")
            Me.MyFlashStatus = DeviceStatus.ExtIoNotConnected
            Return False
        Else
            RaiseEvent PrintConsole(RM.GetString("io_mode_initalized"))
        End If
        Utilities.Sleep(100) 'Wait a little bit for VCC to charge up for 29F devices
        Dim device_matches() As Device = Nothing
        If DetectFlashDevice(device_matches) Then
            Dim chip_id_str As String = Hex(FLASH_IDENT.MFG).PadLeft(2, "0"c) & Hex(FLASH_IDENT.PART).PadLeft(8, "0"c)
            RaiseEvent PrintConsole(String.Format(RM.GetString("ext_connected_chipid"), chip_id_str))
            If (FCUSB.HWBOARD = FCUSB_BOARD.XPORT_PCB2) Then
                If (FLASH_IDENT.ID1 >> 8 = 255) Then FLASH_IDENT.ID1 = (FLASH_IDENT.ID1 And 255US)
            End If
            Me.CFI = New NOR_CFI(EXPIO_NOR_GetCFI())
            If (device_matches IsNot Nothing AndAlso device_matches.Length > 0) Then
                MyFlashDevice_SelectBest(device_matches)
                RaiseEvent PrintConsole(String.Format(RM.GetString("flash_detected"), MyFlashDevice.NAME, Format(MyFlashDevice.FLASH_SIZE, "#,###")))
                RaiseEvent PrintConsole(RM.GetString("ext_prog_mode"))
                If (CURRENT_BUS_WIDTH = E_BUS_WIDTH.X8 AndAlso MyFlashDevice.IFACE = VCC_IF.X16_X8_3V) Then
                    ConfigureParallelBus(MEM_PROTOCOL.NOR_X16_BYTE) 'Change mode to X16 BYTE mode
                End If
                PrintDeviceInterface()
                If (Me.MULTI_CE > 0) Then
                    RaiseEvent PrintConsole("Multi-chip select feature is enabled")
                    Me.DUALDIE_EN = True
                    Me.DUALDIE_CE2 = Me.MULTI_CE
                ElseIf MyFlashDevice.DUAL_DIE Then 'This IC package has two-die with CE access
                    Me.DUALDIE_EN = True
                    Me.DUALDIE_CE2 = MyFlashDevice.CE2
                End If
                If MyFlashDevice.RESET_ENABLED Then Me.ResetDevice(0) 'This is needed for some devices
                EXPIO_SETUP_WRITEDELAY(MyFlashDevice.HARDWARE_DELAY)
                EXPIO_SETUP_DELAY(MyFlashDevice.DELAY_MODE)
                Select Case MyFlashDevice.WriteMode
                    Case MFP_PRG.Standard
                        EXPIO_SETUP_WRITEDATA(E_PARALLEL_WRITEDATA.Standard)
                    Case MFP_PRG.IntelSharp
                        Me.CURRENT_SECTOR_ERASE = E_PARALLEL_SECTOR.Intel
                        Me.CURRENT_CHIP_ERASE = E_PARALLEL_CHIPERASE.Intel
                        EXPIO_SETUP_WRITEDATA(E_PARALLEL_WRITEDATA.Intel)
                    Case MFP_PRG.BypassMode 'Writes 64 bytes using ByPass sequence
                        EXPIO_SETUP_WRITEDATA(E_PARALLEL_WRITEDATA.Bypass)
                    Case MFP_PRG.PageMode 'Writes an entire page of data (128 bytes etc.)
                        EXPIO_SETUP_WRITEDATA(E_PARALLEL_WRITEDATA.Page)
                    Case MFP_PRG.Buffer1 'Writes to a buffer that is than auto-programmed
                        Me.CURRENT_SECTOR_ERASE = E_PARALLEL_SECTOR.Intel
                        Me.CURRENT_CHIP_ERASE = E_PARALLEL_CHIPERASE.Intel
                        EXPIO_SETUP_WRITEDATA(E_PARALLEL_WRITEDATA.Buffer_1)
                    Case MFP_PRG.Buffer2
                        EXPIO_SETUP_WRITEDATA(E_PARALLEL_WRITEDATA.Buffer_2)
                End Select
                WaitUntilReady()
                PrintCurrentWriteMode()
                Utilities.Sleep(10) 'We need to wait here (device is being configured)
                Me.ERASE_ALLOWED = MyFlashDevice.ERASE_REQUIRED
                Me.MyFlashStatus = DeviceStatus.Supported
                Return True
            Else
                MyFlashDevice = Nothing
                Me.MyFlashStatus = DeviceStatus.NotSupported
            End If
        Else
            Me.MyFlashStatus = DeviceStatus.NotDetected
        End If
        Return False
    End Function

    Public Function EEPROM_Init(selected_eeprom As P_NOR) As Boolean
        Me.DIE_SELECTED = 0
        Me.DUALDIE_EN = False
        Me.MyFlashDevice = Nothing
        Me.MAX_PACKET_SIZE = 256
        Me.CFI = Nothing 'EEPROM does not support CFI
        If Not ConfigureParallelBus(MEM_PROTOCOL.AT90C) Then
            RaiseEvent PrintConsole("Parallel I/O failed to initialize")
            Me.MyFlashStatus = DeviceStatus.ExtIoNotConnected
            Return False
        Else
            RaiseEvent PrintConsole(RM.GetString("io_mode_initalized"))
        End If
        Me.ERASE_ALLOWED = False
        Me.MyFlashDevice = selected_eeprom
        Me.MyFlashDevice.RESET_ENABLED = False
        Me.MyFlashStatus = DeviceStatus.Supported
        Return True
    End Function

    Private Sub MyFlashDevice_SelectBest(device_matches() As Device)
        If device_matches.Length = 1 Then
            MyFlashDevice = CType(device_matches(0), P_NOR)
            Exit Sub
        End If
        If (device_matches(0).MFG_CODE = &H1 AndAlso device_matches(0).ID1 = &HAD) Then 'AM29F016x (we need to figure out which one)
            If Not CFI.IS_VALID Then
                MyFlashDevice = CType(device_matches(0), P_NOR) : Exit Sub 'AM29F016B (Uses Legacy programming)
            Else
                MyFlashDevice = CType(device_matches(1), P_NOR) : Exit Sub 'AM29F016D (Uses Bypass programming)
            End If
        Else
            If Me.CFI.IS_VALID Then
                Dim flash_dev As New List(Of Device)
                For i = 0 To device_matches.Length - 1
                    If CUInt(device_matches(i).FLASH_SIZE) = Me.CFI.DEVICE_SIZE Then
                        flash_dev.Add(device_matches(i))
                    End If
                Next
                If flash_dev.Count = 1 Then
                    MyFlashDevice = CType(flash_dev(0), P_NOR) : Exit Sub
                Else
                    For i = 0 To flash_dev.Count - 1
                        If flash_dev(i).PAGE_SIZE = Me.CFI.WRITE_BUFFER_SIZE Then
                            MyFlashDevice = CType(flash_dev(i), P_NOR) : Exit Sub
                        End If
                    Next
                End If
            End If
        End If
        If MyFlashDevice Is Nothing Then MyFlashDevice = CType(device_matches(0), P_NOR)
    End Sub

#Region "Public Interface"

    Public ReadOnly Property GetDevice As Device Implements MemoryDeviceUSB.GetDevice
        Get
            Return Me.MyFlashDevice
        End Get
    End Property

    Friend ReadOnly Property DeviceName() As String Implements MemoryDeviceUSB.DeviceName
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

    Friend ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
        Get
            Dim FLASH_SIZE As Long = MyFlashDevice.FLASH_SIZE
            If Me.DUALDIE_EN Then Return (FLASH_SIZE * 2)
            Return FLASH_SIZE
        End Get
    End Property

    Public Function ReadData(logical_address As Long, data_count As Integer) As Byte() Implements MemoryDeviceUSB.ReadData
        If Me.DUALDIE_EN Then
            Dim data_to_read(data_count - 1) As Byte
            Dim buffer_size As Integer = 0
            Dim array_ptr As Integer = 0
            Do Until data_count = 0
                Dim die_address As UInt32 = GetAddressForMultiDie(CUInt(logical_address), data_count, buffer_size)
                Dim die_data() As Byte = ReadBulk(die_address, buffer_size)
                If die_data Is Nothing Then Return Nothing
                Array.Copy(die_data, 0, data_to_read, array_ptr, die_data.Length) : array_ptr += buffer_size
            Loop
            Return data_to_read
        Else
            Return ReadBulk(CUInt(logical_address), data_count)
        End If
    End Function
    'Returns the die address from the flash_offset (and increases by the buffersize) and also selects the correct die
    Private Function GetAddressForMultiDie(ByRef flash_offset As UInt32, ByRef count As Integer, ByRef buffer_size As Integer) As UInt32
        Dim die_size As UInt32 = CUInt(MyFlashDevice.FLASH_SIZE)
        Dim die_id As Byte = CByte(Math.Floor(flash_offset / die_size))
        Dim die_addr As UInt32 = (flash_offset Mod die_size)
        buffer_size = CInt(Math.Min(count, (die_size - die_addr)))
        If (die_id <> Me.DIE_SELECTED) Then
            If (die_id = 0) Then
                EXPIO_SETADDRCE(0)
            Else
                EXPIO_SETADDRCE(Me.DUALDIE_CE2)
            End If
            Me.DIE_SELECTED = die_id
        End If
        count -= buffer_size
        flash_offset += CUInt(buffer_size)
        Return die_addr
    End Function

    Public Function SectorErase(sector_index As Integer) As Boolean Implements MemoryDeviceUSB.SectorErase
        If Not Me.ERASE_ALLOWED Then Return True
        Try
            If sector_index = 0 AndAlso SectorSize(0) = MyFlashDevice.FLASH_SIZE Then
                Return EraseDevice() 'Single sector, must do a full chip erase instead
            Else
                Dim Logical_Address As UInt32 = 0
                If (sector_index > 0) Then
                    For i As Integer = 0 To sector_index - 1
                        Dim s_size As Integer = SectorSize(i)
                        Logical_Address += CUInt(s_size)
                    Next
                End If
                VPP_ENABLE() 'Enables +12V for supported devices
                Dim sector_start_addr As UInt32 = Logical_Address
                If Me.DUALDIE_EN Then sector_start_addr = GetAddressForMultiDie(Logical_Address, 0, 0)
                EXPIO_EraseSector(sector_start_addr)
                VPP_DISABLE()
                If (MyFlashDevice.ERASE_DELAY > 0) Then Utilities.Sleep(MyFlashDevice.ERASE_DELAY)
                Dim blank_result As Boolean = False
                Dim timeout As UInt32 = 0
                Do Until blank_result
                    If MyFlashDevice.RESET_ENABLED Then ResetDevice(Logical_Address)
                    blank_result = BlankCheck(Logical_Address)
                    timeout += 1UI
                    If (timeout = 10) Then Return False
                    If Not blank_result Then Utilities.Sleep(100)
                Loop
                Return True
            End If
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function WriteData(logical_address As Long, data_to_write() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        Try
            Dim flash_addr32 As UInt32 = CUInt(logical_address)
            VPP_ENABLE()
            Dim ReturnValue As Boolean
            Dim DataToWrite As Integer = data_to_write.Length
            Dim Loops As Integer = CInt(Math.Ceiling(DataToWrite / Me.MAX_PACKET_SIZE)) 'Calcuates iterations
            For i As Integer = 0 To Loops - 1
                Dim BufferSize As Integer = DataToWrite
                If (BufferSize > Me.MAX_PACKET_SIZE) Then BufferSize = Me.MAX_PACKET_SIZE
                Dim data(BufferSize - 1) As Byte
                Array.Copy(data_to_write, (i * Me.MAX_PACKET_SIZE), data, 0, data.Length)
                If Me.DUALDIE_EN Then
                    Dim die_address As UInt32 = GetAddressForMultiDie(flash_addr32, 0, 0)
                    ReturnValue = WriteBulk(die_address, data)
                Else
                    ReturnValue = WriteBulk(flash_addr32, data)
                End If
                If (Not ReturnValue) Then Return False
                If FCUSB.HWBOARD = FCUSB_BOARD.Mach1 AndAlso MyFlashDevice.WriteMode = MFP_PRG.BypassMode Then
                    Utilities.Sleep(300) 'Board is too fast! We need a delay between writes (i.e. AM29LV160B)
                End If
                FCUSB.USB_WaitForComplete()
                flash_addr32 += CUInt(data.Length)
                DataToWrite -= data.Length
            Next
        Catch ex As Exception
        Finally
            VPP_DISABLE()
            If MyFlashDevice.RESET_ENABLED Then ResetDevice(logical_address)
        End Try
        Return True
    End Function

    Public Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
        Utilities.Sleep(100) 'Some flash devices have registers, some rely on delays
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
        Dim Addr32 As Long = Me.SectorFind(sector_index)
        Return WriteData(Addr32, data, Params)
    End Function

    Public Function SectorCount() As Integer Implements MemoryDeviceUSB.SectorCount
        If (Me.DUALDIE_EN) Then
            Return (MyFlashDevice.Sector_Count * 2)
        Else
            Return MyFlashDevice.Sector_Count
        End If
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        Try
            Try
                VPP_ENABLE()
                Dim wm As MFP_PRG = MyFlashDevice.WriteMode
                If (wm = MFP_PRG.IntelSharp Or wm = MFP_PRG.Buffer1) Then
                    Dim BlockCount As Integer = MyFlashDevice.Sector_Count
                    RaiseEvent SetProgress(0)
                    For i = 0 To BlockCount - 1
                        If Not SectorErase(i) Then
                            RaiseEvent SetProgress(0)
                            Return False 'Error erasing sector
                        Else
                            Dim percent As Integer = CInt(Math.Floor((i / (BlockCount - 1)) * 100))
                            RaiseEvent SetProgress(percent)
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
                VPP_DISABLE()
            End Try
        Catch ex As Exception
        Finally
            If MyFlashDevice.RESET_ENABLED Then ResetDevice(0) 'Lets do a chip reset too
        End Try
        Return False
    End Function

    Public Function SectorSize(sector As Integer) As Integer Implements MemoryDeviceUSB.SectorSize
        If Not MyFlashStatus = USB.DeviceStatus.Supported Then Return 0
        If (Me.DUALDIE_EN) Then sector = ((MyFlashDevice.Sector_Count - 1) And sector)
        Return MyFlashDevice.GetSectorSize(sector)
    End Function

#End Region

#Region "Parallel Bus Setup"
    Private Property CURRENT_BUS_WIDTH As E_BUS_WIDTH = E_BUS_WIDTH.X0

    Private Enum E_BUS_WIDTH 'Number of bits transfered per operation
        X0 = 0 'Default
        X8 = 8
        X16 = 16
    End Enum

    Private Enum E_PARALLEL_SECTOR As UInt16
        Standard = 1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0x80;0x5555=0xAA;0x2AAA=0x55;SA=0x30
        Intel = 2 'SA=0x50;SA=0x60;SA=0xD0,SR.7,SA=0x20;SA=0xD0,SR.7 (used by Intel/Sharp devices)
    End Enum

    Private Enum E_PARALLEL_CHIPERASE As UInt16
        Standard = 1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0x80;0x5555=0xAA;0x2AAA=0x55;0x5555=0x10
        Intel = 2 '0x00=0x30;0x00=0xD0; (used by Intel/Sharp devices)
    End Enum

    Private Enum E_PARALLEL_WRITEDATA As UInt16
        Standard = 1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0xA0;SA=DATA;DELAY
        Intel = 2 'SA=0x40;SA=DATA;SR.7
        Bypass = 3 '0x555=0xAA;0x2AA=55;0x555=20;(0x00=0xA0;SA=DATA;...)0x00=0x90;0x00=0x00
        Page = 4 '0x5555,0x2AAA,0x5555;(BA/DATA)
        Buffer_1 = 5 '0xE8...0xD0 (Used by Intel/Sharp)
        Buffer_2 = 6 '0x555=0xAA,0x2AA=0x55,SA=0x25,SA=(WC-1).. (Used by Spanion/Cypress)
        EPROM_X8 = 7 '8-BIT EPROM DEVICE
        EPROM_X16 = 8 '16-BIT EPROM DEVICE
        AT90C = 9 '8-bit ATMEL AT90C EEPROM device
    End Enum

    Private Function ConfigureParallelBus(mode As MEM_PROTOCOL) As Boolean
        Try
            If EXPIO_INIT(mode) Then
                Me.MyAdapter = mode
                Threading.Thread.Sleep(50) 'Give the USB time to change modes
                Select Case mode
                    Case MEM_PROTOCOL.NOR_X8
                        Me.CURRENT_BUS_WIDTH = E_BUS_WIDTH.X8
                    Case MEM_PROTOCOL.NOR_X8_DQ15
                        Me.CURRENT_BUS_WIDTH = E_BUS_WIDTH.X8
                    Case MEM_PROTOCOL.NOR_X16_BYTE
                        Me.CURRENT_BUS_WIDTH = E_BUS_WIDTH.X8
                    Case MEM_PROTOCOL.NOR_X16_WORD
                        Me.CURRENT_BUS_WIDTH = E_BUS_WIDTH.X16
                    Case MEM_PROTOCOL.NOR_X16_LEGACY
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
    'We should only allow this for devices that have a 12V option/chip
    Private Sub VPP_ENABLE()
        Dim VPP_FEAT_EN As Boolean = False
        Dim if_type As VCC_IF = MyFlashDevice.IFACE
        If if_type = VCC_IF.X16_5V_VPP Then
            VPP_FEAT_EN = True
        ElseIf if_type = VCC_IF.X8_5V_VPP Then
            VPP_FEAT_EN = True
        End If
        If VPP_FEAT_EN Then
            EXPIO_HWCONTROL(FCUSB_HW_CTRL.VPP_12V)
            Utilities.Sleep(100) 'We need to wait
        End If
    End Sub
    'We should only allow this for devices that have a 12V option/chip
    Private Sub VPP_DISABLE()
        Dim VPP_FEAT_EN As Boolean = False
        Dim if_type As VCC_IF = MyFlashDevice.IFACE
        If if_type = VCC_IF.X16_5V_VPP Then
            VPP_FEAT_EN = True
        ElseIf if_type = VCC_IF.X8_5V_VPP Then
            VPP_FEAT_EN = True
        End If
        If VPP_FEAT_EN Then
            EXPIO_HWCONTROL(FCUSB_HW_CTRL.VPP_5V)
            Utilities.Sleep(100) 'We need to wait
        End If
    End Sub

    Private Sub PrintCurrentWriteMode()
        Select Case CURRENT_WRITE_MODE
            Case E_PARALLEL_WRITEDATA.Standard  '0x5555=0xAA;0x2AAA=0x55;0x5555=0xA0;SA=DATA;DELAY
                RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Standard")
            Case E_PARALLEL_WRITEDATA.Intel 'SA=0x40;SA=DATA;SR.7
                RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Auto-Word Program")
            Case E_PARALLEL_WRITEDATA.Bypass '0x555=0xAA;0x2AA=55;0x555=20;(0x00=0xA0;SA=DATA;...)0x00=0x90;0x00=0x00
                RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Bypass Mode")
            Case E_PARALLEL_WRITEDATA.Page  '0x5555,0x2AAA,0x5555;(BA/DATA)
                RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Page Write")
            Case E_PARALLEL_WRITEDATA.Buffer_1  '0xE8...0xD0
                RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Buffer (Intel)")
            Case E_PARALLEL_WRITEDATA.Buffer_2 '0x555=0xAA,0x2AA=0x55,SA=0x25,SA=(WC-1)..
                RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": Buffer (Cypress)")
        End Select
    End Sub

#End Region

#Region "PARALLEL NOR"
    Private Property CURRENT_WRITE_MODE As E_PARALLEL_WRITEDATA
    Private Property CURRENT_SECTOR_ERASE As E_PARALLEL_SECTOR
    Private Property CURRENT_CHIP_ERASE As E_PARALLEL_CHIPERASE

    Private Delegate Sub cfi_cmd_sub()
    Private cfi_data() As Byte

    '0xAAA=0xAA;0x555=0x55;0xAAA=0x90; (X8/X16 DEVICES)
    Private Function EXPIO_ReadIdent() As Byte()
        Dim ident(7) As Byte
        Dim ADDR_SHIFT As Integer = 0
        If (Me.MyAdapter = MEM_PROTOCOL.NOR_X16_WORD OrElse Me.MyAdapter = MEM_PROTOCOL.NOR_X16_LEGACY) Then ADDR_SHIFT = 1
        EXPIO_ResetDevice()
        Utilities.Sleep(10)
        WriteCommandData(&H5555, &HAA)
        WriteCommandData(&H2AAA, &H55)
        WriteCommandData(&H5555, &H90)
        Utilities.Sleep(10)
        ident(0) = CByte(ReadMemoryAddress(0) And &HFF)                    'MFG
        Dim ID1 As UInt16 = ReadMemoryAddress(1UI << ADDR_SHIFT)
        If (ADDR_SHIFT = 0) Then ID1 = (ID1 And &HFFUS)                    'X8 ID1
        ident(1) = CByte((ID1 >> 8) And &HFF)                              'ID1(UPPER)
        ident(2) = CByte(ID1 And &HFF)                                     'ID1(LOWER)
        ident(3) = CByte(ReadMemoryAddress(&HEUI << ADDR_SHIFT) And &HFF)  'ID2
        ident(4) = CByte(ReadMemoryAddress(&HFUI << ADDR_SHIFT) And &HFF)  'ID3
        EXPIO_ResetDevice()
        Utilities.Sleep(1)
        Return ident
    End Function

    Private Sub EXPIO_EraseSector_Standard(addr As UInt32)
        'Write Unlock Cycles
        WriteCommandData(&H5555, &HAA)
        WriteCommandData(&H2AAA, &H55)
        'Write Sector Erase Cycles
        WriteCommandData(&H5555, &H80)
        WriteCommandData(&H5555, &HAA)
        WriteCommandData(&H2AAA, &H55)
        WriteMemoryAddress(addr, &H30)
        Utilities.Sleep(10)
        EXPIO_WAIT(addr, &HFF)
        'Puts the device back into READ mode
        WriteMemoryAddress(addr, &HFF)
        WriteMemoryAddress(addr, &HF0)
    End Sub

    Private Sub EXPIO_EraseSector_Intel(addr As UInt32)
        'Clear status register
        WriteMemoryAddress(addr, &H50)
        'Unlock block
        WriteMemoryAddress(addr, &H60)
        WriteMemoryAddress(addr, &HD0)
        Utilities.Sleep(10)
        EXPIO_WAIT(addr)
        'Erase block/sector
        WriteMemoryAddress(addr, &H20)
        WriteMemoryAddress(addr, &HD0)
        Utilities.Sleep(10)
        EXPIO_WAIT(addr)
        'Puts the device back into READ mode
        WriteMemoryAddress(addr, &HFF)
        WriteMemoryAddress(addr, &HF0)
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

    Private Sub EXPIO_ResetDevice(Optional addr As UInt32 = 0)
        WriteCommandData(&H5555, &HAA) 'Standard
        WriteCommandData(&H2AAA, &H55)
        WriteCommandData(&H5555, &HF0)
        WriteMemoryAddress(addr, &HF0) '(MX29LV040C etc.)
        WriteMemoryAddress(addr, &HFF) 'Intel
    End Sub

    Private Sub EXPIO_EraseChip()
        Select Case CURRENT_CHIP_ERASE
            Case E_PARALLEL_CHIPERASE.Standard
                EXPIO_EraseChip_Standard()
            Case E_PARALLEL_CHIPERASE.Intel
                EXPIO_EraseChip_Intel()
        End Select
    End Sub

    Private Sub EXPIO_EraseSector(sector_addr As UInt32)
        Select Case Me.CURRENT_SECTOR_ERASE
            Case E_PARALLEL_SECTOR.Standard
                EXPIO_EraseSector_Standard(sector_addr)
            Case E_PARALLEL_SECTOR.Intel
                EXPIO_EraseSector_Intel(sector_addr)
        End Select
    End Sub

    Private Function EXPIO_NOR_GetCFI() As Byte()
        Try
            cfi_data = Nothing
            If CFI_ExecuteCommand(Sub() WriteCommandData(&H55, &H98)) Then 'Issue Enter CFI command
                MainApp.PrintConsole("Common Flash Interface information present")
                Return cfi_data
            ElseIf CFI_ExecuteCommand(Sub()
                                          WriteCommandData(&H5555, &HAA)
                                          WriteCommandData(&H2AAA, &H55)
                                          WriteCommandData(&H5555, &H98)
                                      End Sub) Then
                MainApp.PrintConsole("Common Flash Interface information present")
                Return cfi_data
            Else
                cfi_data = Nothing
                MainApp.PrintConsole("Common Flash Interface information not present")
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
        Dim SHIFT As Integer = 0
        If Me.CURRENT_BUS_WIDTH = E_BUS_WIDTH.X16 Then SHIFT = 1
        ReDim cfi_data(31)
        For i = 0 To cfi_data.Length - 1
            cfi_data(i) = CByte(ReadMemoryAddress((&H10UI + CUInt(i)) << SHIFT) And 255)
        Next
        If cfi_data(0) = &H51 And cfi_data(1) = &H52 And cfi_data(2) = &H59 Then 'QRY
            Return True
        End If
        Return False
    End Function

    Public Function WriteCommandData(cmd_addr As UInt32, cmd_data As UInt16) As Boolean
        If (Me.MyAdapter = MEM_PROTOCOL.NOR_X16_WORD) Then
            cmd_addr = (cmd_addr << 1) 'The WriteMemoryAddress on MCU will shift back for X16
        ElseIf (Me.MyAdapter = MEM_PROTOCOL.NOR_X8_DQ15) Then
            cmd_addr = (cmd_addr >> 1)
        ElseIf (Me.MyAdapter = MEM_PROTOCOL.NOR_X16_BYTE) Then
            cmd_addr = (cmd_addr << 1)
        End If
        Return WriteMemoryAddress(cmd_addr, cmd_data)
    End Function

#End Region

#Region "Detect Flash Device"

    Private Function DetectFlashDevice(ByRef device_matches() As Device) As Boolean
        RaiseEvent PrintConsole(RM.GetString("ext_detecting_device")) 'Attempting to automatically detect Flash device
        Dim LAST_DETECT As FlashDetectResult = Nothing
        LAST_DETECT.MFG = 0
        If Not X8_MODE_ONLY Then
            Me.FLASH_IDENT = DetectFlash(MEM_PROTOCOL.NOR_X16_WORD)
            If Me.FLASH_IDENT.Successful Then
                device_matches = FlashDatabase.FindDevice_NORX16(Me.FLASH_IDENT.MFG, Me.FLASH_IDENT.ID1, Me.FLASH_IDENT.ID2)
                If (device_matches.Length > 0) AndAlso NOR_IsType16X(DirectCast(device_matches(0), P_NOR).IFACE) Then
                    RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), MemProtoToStr(MEM_PROTOCOL.NOR_X16_WORD)))
                    Return True
                Else
                    LAST_DETECT = Me.FLASH_IDENT
                End If
            End If
        End If
        If Not X8_MODE_ONLY Then
            Me.FLASH_IDENT = DetectFlash(MEM_PROTOCOL.NOR_X16_LEGACY)
            If Me.FLASH_IDENT.Successful Then
                device_matches = FlashDatabase.FindDevice_NORX16(Me.FLASH_IDENT.MFG, Me.FLASH_IDENT.ID1, 0)
                If (device_matches.Length > 0) AndAlso NOR_IsType16X(DirectCast(device_matches(0), P_NOR).IFACE) Then
                    RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), MemProtoToStr(MEM_PROTOCOL.NOR_X16_LEGACY)))
                    Return True
                Else
                    LAST_DETECT = Me.FLASH_IDENT
                End If
            End If
        End If
        Me.FLASH_IDENT = DetectFlash(MEM_PROTOCOL.NOR_X8) 'NOR_X16_DQ15 devices will detect here
        If Me.FLASH_IDENT.Successful Then
            device_matches = FlashDatabase.FindDevice_NORX8(Me.FLASH_IDENT.MFG, CByte(Me.FLASH_IDENT.ID1 And 255), CByte(Me.FLASH_IDENT.ID2 And 255))
            If (device_matches.Length > 0) Then
                RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), MemProtoToStr(MEM_PROTOCOL.NOR_X8)))
                Return True
            Else
                LAST_DETECT = Me.FLASH_IDENT
            End If
        End If
        Me.FLASH_IDENT = DetectFlash(MEM_PROTOCOL.NOR_X8_DQ15)
        If Me.FLASH_IDENT.Successful Then
            device_matches = FlashDatabase.FindDevices(Me.FLASH_IDENT.MFG, Me.FLASH_IDENT.ID1, 0, MemoryType.PARALLEL_NOR)
            If (device_matches.Length > 0) Then
                RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), MemProtoToStr(MEM_PROTOCOL.NOR_X8_DQ15)))
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
        Dim mode_name As String = MemProtoToStr(mode)
        ConfigureParallelBus(mode)
        Dim result As FlashDetectResult = GetFlashResult(EXPIO_ReadIdent())
        If result.Successful Then
            Dim part As UInt32 = (CUInt(result.ID1) << 16) Or (result.ID2)
            Dim chip_id_str As String = Hex(result.MFG).PadLeft(2, "0"c) & Hex(part).PadLeft(8, "0"c)
            RaiseEvent PrintConsole("Mode " & mode_name & " returned ident code: 0x" & chip_id_str)
        End If
        Return result
    End Function

    Private Function MemProtoToStr(mode As MEM_PROTOCOL) As String
        Select Case mode
            Case MEM_PROTOCOL.NOR_X16_WORD
                Return "NOR X16 (Word addressing)"
            Case MEM_PROTOCOL.NOR_X16_LEGACY
                Return "NOR X16 (Byte addressing)"
            Case MEM_PROTOCOL.NOR_X8
                Return "NOR X8 (A0 addressing)"
            Case MEM_PROTOCOL.NOR_X8_DQ15
                Return "NOR X8 (A-1 addressing)"
            Case MEM_PROTOCOL.NOR_X16_BYTE
                Return "NOR X8 (X16/X8 in Byte mode)"
            Case Else
                Return "(Mode not valid)"
        End Select
    End Function

#End Region

    Private Sub PrintDeviceInterface()
        Select Case MyFlashDevice.IFACE
            Case VCC_IF.X8_3V
                RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X8 (3V)")
            Case VCC_IF.X8_5V
                RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X8 (5V)")
            Case VCC_IF.X8_5V_VPP
                RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (5V/12V VPP)")
            Case VCC_IF.X16_1V8
                RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (1.8V)")
            Case VCC_IF.X16_3V
                RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (3V)")
            Case VCC_IF.X16_5V
                RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (5V)")
            Case VCC_IF.X16_5V_VPP
                RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (5V/12V VPP)")
        End Select
    End Sub

    Public Function ResetDevice(addr As Long) As Boolean
        Try
            If MyFlashDevice.FLASH_TYPE = MemoryType.PARALLEL_NOR AndAlso MyFlashDevice.RESET_ENABLED Then
                EXPIO_ResetDevice(CUInt(addr))
            End If
        Catch ex As Exception
            Return False
        Finally
            Utilities.Sleep(50)
        End Try
        Return True
    End Function

    Private Function GetSetupPacket_NOR(Address As UInt32, Count As Integer, PageSize As UInt16) As Byte()
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

    Private Function BlankCheck(base_addr As UInt32) As Boolean
        Try
            Dim IsBlank As Boolean = False
            Dim Counter As Integer = 0
            Do Until IsBlank
                Utilities.Sleep(10)
                Dim w() As Byte = ReadData(base_addr, 4)
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

    Private Function ReadBulk(address As UInt32, count As Integer) As Byte()
        Try
            Dim read_count As Integer = count
            Dim addr_offset As Boolean = False
            If Not (MyAdapter_IsX8()) Then
                If (address Mod 2 = 1) Then
                    addr_offset = True
                    address = (address - 1UI)
                    read_count += 1
                End If
                If (read_count Mod 2 = 1) Then
                    read_count += 1
                End If
            End If
            Dim data_out(read_count - 1) As Byte 'Bytes we want to read
            Dim page_size As UInt16 = 512
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

    Private Function MyAdapter_IsX8() As Boolean
        If MyAdapter = MEM_PROTOCOL.NOR_X8 Then Return True
        If MyAdapter = MEM_PROTOCOL.NOR_X8_DQ15 Then Return True
        If MyAdapter = MEM_PROTOCOL.NOR_X16_BYTE Then Return True
        Return False
    End Function

    Private Function WriteBulk(address As UInt32, data_out() As Byte) As Boolean
        Try
            Dim setup_data() As Byte = GetSetupPacket_NOR(address, data_out.Length, MyFlashDevice.PAGE_SIZE)
            Return FCUSB.USB_SETUP_BULKOUT(USBREQ.EXPIO_WRITEDATA, setup_data, data_out, 0)
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Sub PARALLEL_PORT_TEST()
        SetStatus("Performing parallel I/O output test")
        ConfigureParallelBus(MEM_PROTOCOL.NOR_X16_WORD)
        WriteCommandData(&HFFFFFFFFUI, &HFFFF)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.BYTE_HIGH)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.RB0_HIGH)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.OE_HIGH)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.CE_HIGH)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.WE_HIGH)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.CLE_HIGH)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.ALE_HIGH)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.RELAY_OFF)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.VPP_5V)
        Utilities.Sleep(1000)
        If (Not FCUSB.CheckConnection) Then Exit Sub
        'Test VPP
        'EXPIO_HWCONTROL(FCUSB_HW_CTRL.VPP_12V)
        'EXPIO_HWCONTROL(FCUSB_HW_CTRL.VPP_5V)
        'EXPIO_HWCONTROL(FCUSB_HW_CTRL.VPP_0V)
        WriteCommandData(0, 0)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.BYTE_LOW)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.OE_LOW)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.CE_LOW)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.WE_LOW)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.CLE_LOW)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.ALE_LOW)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.RB0_LOW)
        Utilities.Sleep(500)
        If (Not FCUSB.CheckConnection) Then Exit Sub
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.VPP_5V)
        Utilities.Sleep(300)
        If (Not FCUSB.CheckConnection) Then Exit Sub
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.VPP_0V)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.CLE_HIGH)
        Utilities.Sleep(300)
        If (Not FCUSB.CheckConnection) Then Exit Sub
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.CLE_LOW)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.ALE_HIGH)
        Utilities.Sleep(300)
        If (Not FCUSB.CheckConnection) Then Exit Sub
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.ALE_LOW)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.BYTE_HIGH)
        Utilities.Sleep(300)
        If (Not FCUSB.CheckConnection) Then Exit Sub
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.BYTE_LOW)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.RB0_HIGH)
        Utilities.Sleep(300)
        If (Not FCUSB.CheckConnection) Then Exit Sub
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.RB0_LOW)
        For i As Integer = 0 To 7
            WriteCommandData(0, 1US << i)
            Utilities.Sleep(300)
            If (Not FCUSB.CheckConnection) Then Exit Sub
        Next
        For i As Integer = 15 To 8 Step -1
            WriteCommandData(0, 1US << i)
            Utilities.Sleep(300)
            If (Not FCUSB.CheckConnection) Then Exit Sub
        Next
        WriteCommandData(0, 0)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.WE_LOW)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.CE_HIGH)
        Utilities.Sleep(300)
        If (Not FCUSB.CheckConnection) Then Exit Sub
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.CE_LOW)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.WE_HIGH)
        Utilities.Sleep(300)
        If (Not FCUSB.CheckConnection) Then Exit Sub
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.WE_LOW)
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.OE_HIGH)
        Utilities.Sleep(300)
        If (Not FCUSB.CheckConnection) Then Exit Sub
        EXPIO_HWCONTROL(FCUSB_HW_CTRL.OE_LOW)
        For i As Integer = 0 To 27
            WriteCommandData(1UI << i, 0)
            Utilities.Sleep(300)
            If (Not FCUSB.CheckConnection) Then Exit Sub
        Next
        WriteCommandData(0, 0)
        SetStatus("Parallel I/O output test complete")
    End Sub

#Region "USB Calls"

    Private Sub EXPIO_HWCONTROL(cmd As FCUSB_HW_CTRL)
        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, cmd)
        Utilities.Sleep(10)
    End Sub

    Private Sub EXPIO_SETADDRCE(ce_value As Integer)
        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_ADDRESS_CE, Nothing, CUInt(ce_value))
    End Sub

    Private Function EXPIO_INIT(mode As MEM_PROTOCOL) As Boolean
        Dim result_data(0) As Byte
        If Not FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_INIT, result_data, CUInt(mode)) Then Return False
        If (result_data(0) = &H17) Then Return True
        Return False
    End Function

    Private Function EXPIO_SETUP_WRITEDATA(mode As E_PARALLEL_WRITEDATA) As Boolean
        Try
            If Not FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_MODE_WRITE, Nothing, mode) Then Return False
            Me.CURRENT_WRITE_MODE = mode
        Catch ex As Exception
            Return False
        End Try
        Return True
    End Function

    Private Function EXPIO_SETUP_DELAY(delay_mode As MFP_DELAY) As Boolean
        Try
            If Not FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_MODE_DELAY, Nothing, CUInt(delay_mode)) Then Return False
            Threading.Thread.Sleep(25)
        Catch ex As Exception
            Return False
        End Try
        Return True
    End Function

    Private Function EXPIO_SETUP_WRITEDELAY(delay_cycles As UInt16) As Boolean
        Try
            If Not FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_DELAY, Nothing, delay_cycles) Then Return False
            Threading.Thread.Sleep(25)
        Catch ex As Exception
            Return False
        End Try
        Return True
    End Function
    'Sets access and write pulse timings for MACH1 using NOR PARALLEL mode
    Public Sub EXPIO_SETTIMING(read_access As Integer, we_pulse As Integer)
        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_TIMING, Nothing, CUInt(read_access << 8 Or we_pulse))
    End Sub

    Private Sub EXPIO_WAIT(addr32 As UInt32, Optional last_byte As Byte = 255)
        Dim data_out(4) As Byte
        data_out(0) = CByte((addr32 >> 24) And 255)
        data_out(1) = CByte((addr32 >> 16) And 255)
        data_out(2) = CByte((addr32 >> 8) And 255)
        data_out(3) = CByte(addr32 And 255)
        data_out(4) = last_byte
        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WAIT, data_out) 'LastAddress and LastByte
        FCUSB.USB_WaitForComplete() 'Checks for WAIT flag to clear
    End Sub

    Public Function WriteMemoryAddress(mem_addr As UInt32, mem_data As UInt16) As Boolean
        Dim addr_data(5) As Byte
        addr_data(0) = CByte((mem_addr >> 24) And 255)
        addr_data(1) = CByte((mem_addr >> 16) And 255)
        addr_data(2) = CByte((mem_addr >> 8) And 255)
        addr_data(3) = CByte(mem_addr And 255)
        addr_data(4) = CByte((mem_data >> 8) And 255)
        addr_data(5) = CByte(mem_data And 255)
        Return FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WRMEMDATA, addr_data)
    End Function

    Public Function ReadMemoryAddress(mem_addr As UInt32) As UInt16
        Dim data_out(1) As Byte
        FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_RDMEMDATA, data_out, mem_addr)
        Return (CUShort(data_out(1)) << 8) Or data_out(0)
    End Function

#End Region

    Public Function GetUsbDevice() As FCUSB_DEVICE Implements MemoryDeviceUSB.GetUsbDevice
        Return Me.FCUSB
    End Function

End Class
