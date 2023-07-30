'COPYRIGHT EMBEDDED COMPUTERS LLC 2020 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB PRODUCTS
'CONTACT EMAIL: support@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: This is the main module that is loaded first.

Imports FlashcatUSB.ECC_LIB
Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB

Public Class PARALLEL_NOR : Implements MemoryDeviceUSB
    Private FCUSB As FCUSB_DEVICE
    Public Property MyFlashDevice As P_NOR
    Public Property MyFlashStatus As DeviceStatus = DeviceStatus.NotDetected
    Public Property CFI As NOR_CFI 'Contains CFI table information (NOR)
    Public Property MyAdapter As MEM_PROTOCOL 'This is the kind of socket adapter connected and the mode it is in

    Private FLASH_IDENT As FlashDetectResult

    Public Event PrintConsole(msg As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress

    Public Property DUALDIE_EN As Boolean = False 'Indicates two DIE are connected and using a CE
    Public Property DUALDIE_CE2 As Integer 'The Address pin that goes to the second chip-enable
    Public Property DIE_SELECTED As Integer = 0

    Sub New(parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        Me.DIE_SELECTED = 0
        Me.DUALDIE_EN = False
        MyFlashDevice = Nothing
        Me.CFI = Nothing
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
            Dim device_matches() As Device
            If MyAdapter = MEM_PROTOCOL.NOR_X8 Then
                device_matches = FlashDatabase.FindDevices(FLASH_IDENT.MFG, FLASH_IDENT.ID1, 0, MemoryType.PARALLEL_NOR)
            Else
                device_matches = FlashDatabase.FindDevices(FLASH_IDENT.MFG, FLASH_IDENT.ID1, FLASH_IDENT.ID2, MemoryType.PARALLEL_NOR)
            End If
            Me.CFI = New NOR_CFI(EXPIO_NOR_GetCFI())
            If (device_matches IsNot Nothing AndAlso device_matches.Count > 0) Then
                MyFlashDevice_SelectBest(device_matches)
                RaiseEvent PrintConsole(String.Format(RM.GetString("flash_detected"), MyFlashDevice.NAME, Format(MyFlashDevice.FLASH_SIZE, "#,###")))
                RaiseEvent PrintConsole(RM.GetString("ext_prog_mode"))
                PrintDeviceInterface()
                If (MySettings.MULTI_CE > 0) Then
                    RaiseEvent PrintConsole("Multi-chip select feature is enabled")
                    Me.DUALDIE_EN = True
                    Me.DUALDIE_CE2 = MySettings.MULTI_CE
                ElseIf MyFlashDevice.DUAL_DIE Then 'This IC package has two-die with CE access
                    Me.DUALDIE_EN = True
                    Me.DUALDIE_CE2 = MyFlashDevice.CE2
                End If
                If MyFlashDevice.RESET_ENABLED Then Me.ResetDevice() 'This is needed for some devices
                EXPIO_SETUP_WRITEDELAY(MyFlashDevice.HARDWARE_DELAY)
                EXPIO_SETUP_DELAY(MyFlashDevice.DELAY_MODE)
                Select Case MyFlashDevice.WriteMode
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
                WaitForReady()
                EXPIO_PrintCurrentWriteMode()
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
        If MyFlashDevice Is Nothing Then MyFlashDevice = device_matches(0)
    End Sub

#Region "Public Interface"

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

    Public Function ReadData(logical_address As Long, data_count As UInt32, Optional memory_area As FlashArea = FlashArea.Main) As Byte() Implements MemoryDeviceUSB.ReadData
        If Me.DUALDIE_EN Then
            Dim data_to_read(data_count - 1) As Byte
            Dim buffer_size As UInt32 = 0
            Dim array_ptr As UInt32 = 0
            Do Until data_count = 0
                Dim die_address As UInt32 = GetAddressForMultiDie(logical_address, data_count, buffer_size)
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
    Private Function GetAddressForMultiDie(ByRef flash_offset As UInt32, ByRef count As UInt32, ByRef buffer_size As UInt32) As UInt32
        Dim die_count As Integer = 2 'Multi die only supports 2 (for now)
        Dim die_size As UInt32 = MyFlashDevice.FLASH_SIZE
        Dim die_id As Byte = CByte(Math.Floor(flash_offset / die_size))
        Dim die_addr As UInt32 = (flash_offset Mod die_size)
        buffer_size = Math.Min(count, (die_size - die_addr))
        If (die_id <> Me.DIE_SELECTED) Then
            If (die_id = 0) Then
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_ADDRESS_CE, Nothing, 0)
            Else
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_ADDRESS_CE, Nothing, Me.DUALDIE_CE2)
            End If
            Me.DIE_SELECTED = die_id
        End If
        count -= buffer_size
        flash_offset += buffer_size
        Return die_addr
    End Function

    Public Function SectorErase(sector_index As UInt32, Optional memory_area As FlashArea = FlashArea.Main) As Boolean Implements MemoryDeviceUSB.SectorErase
        If Not MyFlashDevice.ERASE_REQUIRED Then Return True
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
                If Me.DUALDIE_EN Then sector_start_addr = GetAddressForMultiDie(Logical_Address, 0, 0)
                EXPIO_EraseSector(sector_start_addr)
                EXPIO_VPP_DISABLE()
                If MyFlashDevice.DELAY_MODE = MFP_DELAY.SR1 Or MyFlashDevice.DELAY_MODE = MFP_DELAY.SR2 Or MyFlashDevice.DELAY_MODE = MFP_DELAY.RYRB Then
                    FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WAIT) 'Calls the assigned WAIT function (uS, mS, SR, DQ7)
                    FCUSB.USB_WaitForComplete() 'Checks for WAIT flag to clear
                Else
                    Utilities.Sleep(MyFlashDevice.ERASE_DELAY) 'Some flashes (like MX29LV040C) need more than 100ms delay
                End If
                Dim blank_result As Boolean = False
                Dim timeout As UInt32 = 0
                Do Until blank_result
                    If MyFlashDevice.RESET_ENABLED Then ResetDevice()
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
    End Function

    Public Function WriteData(logical_address As Long, data_to_write() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
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
                If Me.DUALDIE_EN Then
                    Dim die_address As UInt32 = GetAddressForMultiDie(logical_address, 0, 0)
                    ReturnValue = WriteBulk(die_address, data)
                Else
                    ReturnValue = WriteBulk(logical_address, data)
                End If
                If (Not ReturnValue) Then Return False
                If FCUSB.HWBOARD = FCUSB_BOARD.Mach1 AndAlso MyFlashDevice.WriteMode = MFP_PRG.BypassMode Then
                    Utilities.Sleep(300) 'Board is too fast! We need a delay between writes (i.e. AM29LV160B)
                End If
                FCUSB.USB_WaitForComplete()
                logical_address += data.Length
                DataToWrite -= data.Length
            Next
            If MyFlashDevice.DELAY_MODE = MFP_DELAY.DQ7 Or MyFlashDevice.DELAY_MODE = MFP_DELAY.SR1 Or MyFlashDevice.DELAY_MODE = MFP_DELAY.SR2 Then
                EXPIO_WAIT()
            Else
                Utilities.Sleep(MyFlashDevice.SOFTWARE_DELAY)
            End If
        Catch ex As Exception
        Finally
            EXPIO_VPP_DISABLE()
            If MyFlashDevice.RESET_ENABLED Then ResetDevice()
        End Try
        Return True
        Return False
    End Function

    Public Sub WaitForReady() Implements MemoryDeviceUSB.WaitUntilReady
        'FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_WAIT)
        'FCUSB.USB_WaitForComplete() 'Checks for WAIT flag to clear
        Utilities.Sleep(100) 'Some flash devices have registers, some rely on delays
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
        If (Me.DUALDIE_EN) Then
            Return (MyFlashDevice.Sector_Count * 2)
        Else
            Return MyFlashDevice.Sector_Count
        End If
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        Try
            Try
                EXPIO_VPP_ENABLE()
                Dim wm As MFP_PRG = MyFlashDevice.WriteMode
                If (wm = MFP_PRG.IntelSharp Or wm = MFP_PRG.Buffer1) Then
                    Dim BlockCount As Integer = MyFlashDevice.Sector_Count
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
        Catch ex As Exception
        Finally
            If MyFlashDevice.RESET_ENABLED Then ResetDevice() 'Lets do a chip reset too
        End Try
        Return False
    End Function

    Friend ReadOnly Property SectorSize(sector As UInt32, Optional memory_area As FlashArea = FlashArea.Main) As UInt32 Implements MemoryDeviceUSB.SectorSize
        Get
            If Not MyFlashStatus = USB.DeviceStatus.Supported Then Return 0
            If (Me.DUALDIE_EN) Then sector = ((MyFlashDevice.Sector_Count - 1) And sector)
            Return MyFlashDevice.GetSectorSize(sector)
        End Get
    End Property

#End Region

#Region "EXPIO SETUP"

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
                End Select
                Return True 'Communication successful
            Else
                Return False
            End If
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EXPIO_SETUP_WRITEDATA(mode As E_EXPIO_WRITEDATA) As Boolean
        Try
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_MODE_WRITE, Nothing, mode)
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
        Dim if_type As VCC_IF = MyFlashDevice.IFACE
        If if_type = VCC_IF.X16_5V_12VPP Then
            VPP_FEAT_EN = True
        ElseIf if_type = VCC_IF.X16_3V_12VPP Then
            VPP_FEAT_EN = True
        ElseIf if_type = VCC_IF.X8_5V_12VPP Then
            VPP_FEAT_EN = True
        End If
        If VPP_FEAT_EN Then
            HardwareControl(FCUSB_HW_CTRL.VPP_12V)
            Utilities.Sleep(100) 'We need to wait
        End If
    End Sub
    'We should only allow this for devices that have a 12V option/chip
    Private Sub EXPIO_VPP_DISABLE()
        Dim VPP_FEAT_EN As Boolean = False
        Dim if_type As VCC_IF = MyFlashDevice.IFACE
        If if_type = VCC_IF.X16_5V_12VPP Then
            VPP_FEAT_EN = True
        ElseIf if_type = VCC_IF.X16_3V_12VPP Then
            VPP_FEAT_EN = True
        ElseIf if_type = VCC_IF.X8_5V_12VPP Then
            VPP_FEAT_EN = True
        End If
        If VPP_FEAT_EN Then
            HardwareControl(FCUSB_HW_CTRL.VPP_5V)
            Utilities.Sleep(100) 'We need to wait
        End If
    End Sub

    Private Sub EXPIO_PrintCurrentWriteMode()
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
        End Select
    End Sub

#End Region

#Region "PARALLEL NOR"
    Private Property CURRENT_WRITE_MODE As E_EXPIO_WRITEDATA
    Private Property CURRENT_SECTOR_ERASE As E_EXPIO_SECTOR
    Private Property CURRENT_CHIP_ERASE As E_EXPIO_CHIPERASE

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
        Me.FLASH_IDENT = DetectFlash(MEM_PROTOCOL.NOR_X16)
        If Me.FLASH_IDENT.Successful Then
            Dim d() As Device = FlashDatabase.FindDevices(Me.FLASH_IDENT.MFG, Me.FLASH_IDENT.ID1, Me.FLASH_IDENT.ID2, MemoryType.PARALLEL_NOR)
            If (d.Count > 0) AndAlso IsIFACE16X(DirectCast(d(0), P_NOR).IFACE) Then
                RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "NOR X16 (Word addressing)"))
                Me.MyAdapter = MEM_PROTOCOL.NOR_X16
                Return True
            Else
                LAST_DETECT = Me.FLASH_IDENT
            End If
        End If
        Me.FLASH_IDENT = DetectFlash(MEM_PROTOCOL.NOR_X16_X8)
        If Me.FLASH_IDENT.Successful Then
            Dim d() As Device = FlashDatabase.FindDevices(Me.FLASH_IDENT.MFG, Me.FLASH_IDENT.ID1, 0, MemoryType.PARALLEL_NOR)
            If (d.Count > 0) AndAlso IsIFACE16X(DirectCast(d(0), P_NOR).IFACE) Then
                RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "NOR X16 (Byte addressing)"))
                Me.MyAdapter = MEM_PROTOCOL.NOR_X16_X8
                Return True
            Else
                LAST_DETECT = Me.FLASH_IDENT
            End If
        End If
        Me.FLASH_IDENT = DetectFlash(MEM_PROTOCOL.NOR_X8)
        If Me.FLASH_IDENT.Successful Then
            Dim d() As Device = FlashDatabase.FindDevices(Me.FLASH_IDENT.MFG, Me.FLASH_IDENT.ID1, 0, MemoryType.PARALLEL_NOR)
            If (d.Count > 0) AndAlso IsIFACE8X(DirectCast(d(0), P_NOR).IFACE) Then
                RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "NOR X8"))
                Me.MyAdapter = MEM_PROTOCOL.NOR_X8
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
            Case MEM_PROTOCOL.NOR_X16
                mode_name = "NOR X16 (Word addressing)"
                result = EXPIO_DetectX16()
            Case MEM_PROTOCOL.NOR_X16_X8
                mode_name = "NOR X16 (Byte addressing)"
                result = EXPIO_DetectX16_X8()
            Case MEM_PROTOCOL.NOR_X8
                mode_name = "NOR X8"
                result = EXPIO_DetectX8()
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

#End Region

    Private Sub PrintDeviceInterface()
        Select Case MyFlashDevice.IFACE
            Case VCC_IF.X8_3V
                RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X8 (3V)")
            Case VCC_IF.X8_5V
                RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X8 (5V)")
            Case VCC_IF.X8_5V_12VPP
                RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (5V/12V VPP)")
            Case VCC_IF.X16_1V8
                RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (1.8V)")
            Case VCC_IF.X16_3V
                RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (3V)")
            Case VCC_IF.X16_5V
                RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (5V)")
            Case VCC_IF.X16_5V_12VPP
                RaiseEvent PrintConsole(RM.GetString("ext_device_interface") & ": NOR X16 (5V/12V VPP)")
        End Select
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
            Case VCC_IF.X16_1V8
                Return True
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
            End If
        Catch ex As Exception
            Return False
        Finally
            Utilities.Sleep(50)
        End Try
        Return True
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

    Private Function BlankCheck(base_addr As UInt32) As Boolean
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

    Private Function ReadBulk(address As UInt32, count As UInt32) As Byte()
        Try
            Dim read_count As UInt32 = count
            Dim addr_offset As Boolean = False
            Dim count_offset As Boolean = False
            If Not (MyAdapter = MEM_PROTOCOL.NOR_X8) Then
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

    Private Function WriteBulk(address As UInt32, data_out() As Byte) As Boolean
        Try
            Dim setup_data() As Byte = GetSetupPacket_NOR(address, data_out.Length, MyFlashDevice.PAGE_SIZE)
            Return FCUSB.USB_SETUP_BULKOUT(USBREQ.EXPIO_WRITEDATA, setup_data, data_out, 0)
        Catch ex As Exception
        End Try
        Return False
    End Function

    Private Sub HardwareControl(cmd As FCUSB_HW_CTRL)
        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, cmd)
        Utilities.Sleep(10)
    End Sub

    Public Sub PARALLEL_PORT_TEST()
        SetStatus("Performing parallel I/O output test")
        EXPIO_SETUP_USB(MEM_PROTOCOL.NOR_X16)
        WriteCommandData(&HFFFFFFFFUI, &HFFFF)
        HardwareControl(FCUSB_HW_CTRL.BYTE_HIGH)
        HardwareControl(FCUSB_HW_CTRL.RB0_HIGH)
        HardwareControl(FCUSB_HW_CTRL.OE_HIGH)
        HardwareControl(FCUSB_HW_CTRL.CE_HIGH)
        HardwareControl(FCUSB_HW_CTRL.WE_HIGH)
        HardwareControl(FCUSB_HW_CTRL.CLE_HIGH)
        HardwareControl(FCUSB_HW_CTRL.ALE_HIGH)
        HardwareControl(FCUSB_HW_CTRL.RELAY_OFF)
        HardwareControl(FCUSB_HW_CTRL.VPP_5V)
        Utilities.Sleep(500)
        WriteCommandData(0, 0)
        HardwareControl(FCUSB_HW_CTRL.BYTE_LOW)
        HardwareControl(FCUSB_HW_CTRL.OE_LOW)
        HardwareControl(FCUSB_HW_CTRL.CE_LOW)
        HardwareControl(FCUSB_HW_CTRL.WE_LOW)
        HardwareControl(FCUSB_HW_CTRL.CLE_LOW)
        HardwareControl(FCUSB_HW_CTRL.ALE_LOW)
        HardwareControl(FCUSB_HW_CTRL.RB0_LOW)
        Utilities.Sleep(500)
        HardwareControl(FCUSB_HW_CTRL.VPP_5V)
        Utilities.Sleep(300)
        HardwareControl(FCUSB_HW_CTRL.VPP_0V)
        HardwareControl(FCUSB_HW_CTRL.CLE_HIGH)
        Utilities.Sleep(300)
        HardwareControl(FCUSB_HW_CTRL.CLE_LOW)
        HardwareControl(FCUSB_HW_CTRL.ALE_HIGH)
        Utilities.Sleep(300)
        HardwareControl(FCUSB_HW_CTRL.ALE_LOW)
        HardwareControl(FCUSB_HW_CTRL.BYTE_HIGH)
        Utilities.Sleep(300)
        HardwareControl(FCUSB_HW_CTRL.BYTE_LOW)
        HardwareControl(FCUSB_HW_CTRL.RB0_HIGH)
        Utilities.Sleep(300)
        HardwareControl(FCUSB_HW_CTRL.RB0_LOW)
        For i = 0 To 7
            WriteCommandData(0, 1 << i)
            Utilities.Sleep(300)
        Next
        For i = 15 To 8 Step -1
            WriteCommandData(0, 1 << i)
            Utilities.Sleep(300)
        Next
        WriteCommandData(0, 0)
        HardwareControl(FCUSB_HW_CTRL.WE_LOW)
        HardwareControl(FCUSB_HW_CTRL.CE_HIGH)
        Utilities.Sleep(300)
        HardwareControl(FCUSB_HW_CTRL.CE_LOW)
        HardwareControl(FCUSB_HW_CTRL.WE_HIGH)
        Utilities.Sleep(300)
        HardwareControl(FCUSB_HW_CTRL.WE_LOW)
        HardwareControl(FCUSB_HW_CTRL.OE_HIGH)
        Utilities.Sleep(300)
        HardwareControl(FCUSB_HW_CTRL.OE_LOW)
        For i = 0 To 27
            WriteCommandData(1 << i, 0)
            Utilities.Sleep(300)
        Next
        WriteCommandData(0, 0)
        SetStatus("Parallel I/O output test complete")
    End Sub

End Class
