Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB

Public Class BSR_Programmer : Implements MemoryDeviceUSB
    Private FCUSB As FCUSB_DEVICE
    Public Property MyFlashDevice As P_NOR
    Public Property MyFlashStatus As DeviceStatus = DeviceStatus.NotDetected
    Public Property CFI As NOR_CFI 'Contains CFI table information (NOR)

    Private FLASH_IDENT As FlashDetectResult
    Private ModeSelect As MEM_PROTOCOL

    Public Event PrintConsole(message As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress

    Sub New(parent_if As USB.FCUSB_DEVICE)
        Me.FCUSB = parent_if
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        Me.MyFlashDevice = Nothing
        Me.CFI = Nothing
        If DetectFlashDevice() Then
            Dim chip_id_str As String = Hex(FLASH_IDENT.MFG).PadLeft(2, "0"c) & Hex(FLASH_IDENT.PART).PadLeft(8, "0"c)
            RaiseEvent PrintConsole(String.Format(RM.GetString("ext_connected_chipid"), chip_id_str))
            Dim device_matches() As Device = Nothing
            If Me.CURRENT_BUS_WIDTH = E_BSR_BUS_WIDTH.X8 Then
                device_matches = FlashDatabase.FindDevice_NORX8(FLASH_IDENT.MFG, CByte(FLASH_IDENT.ID1 And 255), CByte(FLASH_IDENT.ID2 And 255))
            Else
                device_matches = FlashDatabase.FindDevice_NORX16(FLASH_IDENT.MFG, FLASH_IDENT.ID1, FLASH_IDENT.ID2)
            End If
            Me.CFI = New NOR_CFI(BSR_GetCFI())
            If (device_matches IsNot Nothing AndAlso device_matches.Count > 0) Then
                If (device_matches.Count > 1) AndAlso Me.CFI.IS_VALID Then
                    For i = 0 To device_matches.Count - 1
                        If device_matches(i).PAGE_SIZE = Me.CFI.WRITE_BUFFER_SIZE Then
                            Me.MyFlashDevice = CType(device_matches(i), P_NOR) : Exit For
                        End If
                    Next
                End If
                If Me.MyFlashDevice Is Nothing Then Me.MyFlashDevice = CType(device_matches(0), P_NOR)
                RaiseEvent PrintConsole(String.Format(RM.GetString("flash_detected"), Me.MyFlashDevice.NAME, Format(Me.MyFlashDevice.FLASH_SIZE, "#,###")))
                If MyFlashDevice.RESET_ENABLED Then ResetDevice() 'This is needed for some devices
                Select Case MyFlashDevice.WriteMode
                    Case MFP_PRG.Standard
                        Me.CURRENT_WRITE_MODE = E_BSR_WRITEDATA.Standard
                    Case MFP_PRG.IntelSharp
                        Me.CURRENT_SECTOR_ERASE = E_BSR_SECTOR.Intel
                        Me.CURRENT_CHIP_ERASE = E_BSR_CHIPERASE.Intel
                        Me.CURRENT_WRITE_MODE = E_BSR_WRITEDATA.Intel
                    Case MFP_PRG.BypassMode 'Writes 64 bytes using ByPass sequence
                        Me.CURRENT_WRITE_MODE = E_BSR_WRITEDATA.Bypass
                    Case MFP_PRG.PageMode 'Writes an entire page of data (128 bytes etc.)
                        Me.CURRENT_WRITE_MODE = E_BSR_WRITEDATA.Page
                    Case MFP_PRG.Buffer1 'Writes to a buffer that is than auto-programmed
                        Me.CURRENT_SECTOR_ERASE = E_BSR_SECTOR.Intel
                        Me.CURRENT_CHIP_ERASE = E_BSR_CHIPERASE.Intel
                        Me.CURRENT_WRITE_MODE = E_BSR_WRITEDATA.Buffer_1
                    Case MFP_PRG.Buffer2
                        Me.CURRENT_WRITE_MODE = E_BSR_WRITEDATA.Buffer_2
                End Select
                WaitUntilReady()
                Utilities.Sleep(10) 'We need to wait here (device is being configured)
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

    Private Function DetectFlashDevice() As Boolean
        RaiseEvent PrintConsole(RM.GetString("ext_detecting_device")) 'Attempting to automatically detect Flash device
        If Me.CURRENT_BUS_WIDTH = E_BSR_BUS_WIDTH.X8 Then
            Me.FLASH_IDENT = DetectFlash(MEM_PROTOCOL.NOR_X8)
            If Me.FLASH_IDENT.Successful Then
                Dim d() As Device = FlashDatabase.FindDevices(Me.FLASH_IDENT.MFG, Me.FLASH_IDENT.ID1, 0, MemoryType.PARALLEL_NOR)
                RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "NOR X8"))
                Return True
            End If
            Me.FLASH_IDENT = DetectFlash(MEM_PROTOCOL.NOR_X16_BYTE) 'We might have a X16 Flash in X8 mode
            If Me.FLASH_IDENT.Successful Then
                Dim d() As Device = FlashDatabase.FindDevices(Me.FLASH_IDENT.MFG, Me.FLASH_IDENT.ID1, 0, MemoryType.PARALLEL_NOR)
                RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "NOR X16 (Byte mode)"))
                Return True
            End If
        ElseIf Me.CURRENT_BUS_WIDTH = E_BSR_BUS_WIDTH.X16 Then
            Me.FLASH_IDENT = DetectFlash(MEM_PROTOCOL.NOR_X16_WORD)
            If Me.FLASH_IDENT.Successful Then
                Dim d() As Device = FlashDatabase.FindDevices(Me.FLASH_IDENT.MFG, Me.FLASH_IDENT.ID1, Me.FLASH_IDENT.ID2, MemoryType.PARALLEL_NOR)
                RaiseEvent PrintConsole(String.Format(RM.GetString("ext_device_detected"), "NOR X16 (Word mode)"))
                Return True
            End If
        End If
        Return False 'No devices detected
    End Function

    Private Function DetectFlash(mode As MEM_PROTOCOL) As FlashDetectResult
        Dim mode_name As String = ""
        Dim result As FlashDetectResult
        Dim dw_protocol As UInt32 = 0
        ModeSelect = mode
        Select Case ModeSelect
            Case MEM_PROTOCOL.NOR_X16_WORD
                mode_name = "NOR X16 (Word mode)"
                dw_protocol = CUInt(E_BSR_IF.IFTYPE_X16_WORD)
            Case MEM_PROTOCOL.NOR_X16_BYTE
                mode_name = "NOR X16 (Byte mode)"
                dw_protocol = CUInt(E_BSR_IF.IFTYPE_X16_BYTE)
            Case MEM_PROTOCOL.NOR_X8
                mode_name = "NOR X8"
                dw_protocol = CUInt(E_BSR_IF.IFTYPE_X8)
        End Select
        If (Not FCUSB.USB_CONTROL_MSG_OUT(USBREQ.JTAG_BDR_INIT, Nothing, dw_protocol)) Then
            RaiseEvent PrintConsole("Error: Boundary Scan init failed") : Return Nothing
        End If
        result = GetFlashResult(BSR_ReadIdent())
        If result.Successful Then
            Dim part As UInt32 = (CUInt(result.ID1) << 16) Or (result.ID2)
            Dim chip_id_str As String = Hex(result.MFG).PadLeft(2, "0"c) & Hex(part).PadLeft(8, "0"c)
            RaiseEvent PrintConsole("Mode " & mode_name & " returned ident code: 0x" & chip_id_str)
        End If
        Return result
    End Function

#Region "Properties"
    Public Property CURRENT_BUS_WIDTH As E_BSR_BUS_WIDTH
    Private Property CURRENT_WRITE_MODE As E_BSR_WRITEDATA
    Private Property CURRENT_SECTOR_ERASE As E_BSR_SECTOR
    Private Property CURRENT_CHIP_ERASE As E_BSR_CHIPERASE

    Public Enum E_BSR_BUS_WIDTH As UInt32
        X8 = 8
        X16 = 16
    End Enum

    Private Enum E_BSR_SECTOR As UInt16
        Standard = 1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0x80;0x5555=0xAA;0x2AAA=0x55;SA=0x30
        Intel = 2 'SA=0x50;SA=0x60;SA=0xD0,SR.7,SA=0x20;SA=0xD0,SR.7 (used by Intel/Sharp devices)
    End Enum

    Private Enum E_BSR_CHIPERASE As UInt16
        Standard = 1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0x80;0x5555=0xAA;0x2AAA=0x55;0x5555=0x10
        Intel = 2 '0x00=0x30;0x00=0xD0; (used by Intel/Sharp devices)
    End Enum

    Private Enum E_BSR_WRITEDATA As UInt16
        Standard = 1 '0x5555=0xAA;0x2AAA=0x55;0x5555=0xA0;SA=DATA;DELAY
        Intel = 2 'SA=0x40;SA=DATA;SR.7
        Bypass = 3 '0x555=0xAA;0x2AA=55;0x555=20;(0x00=0xA0;SA=DATA;...)0x00=0x90;0x00=0x00
        Page = 4 '0x5555,0x2AAA,0x5555;(BA/DATA)
        Buffer_1 = 5 '0xE8...0xD0 (Used by Intel/Sharp)
        Buffer_2 = 6 '0x555=0xAA,0x2AA=0x55,SA=0x25,SA=(WC-1).. (Used by Spanion/Cypress)
        EPROM_X8 = 7 '8-BIT EPROM DEVICE
        EPROM_X16 = 8 '16-BIT EPROM DEVICE
    End Enum

    Private Enum E_BSR_IF
        IFTYPE_X8 = 1
        IFTYPE_X16_WORD = 2
        IFTYPE_X16_BYTE = 3
    End Enum

#End Region

#Region "Public Interface"

    Public ReadOnly Property GetDevice As Device Implements MemoryDeviceUSB.GetDevice
        Get
            Return Me.MyFlashDevice
        End Get
    End Property

    Public ReadOnly Property DeviceName As String Implements MemoryDeviceUSB.DeviceName
        Get
            Return Me.MyFlashDevice.NAME
        End Get
    End Property

    Public ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
        Get
            Return Me.MyFlashDevice.FLASH_SIZE
        End Get
    End Property

    Public Function SectorSize(sector As Integer) As Integer Implements MemoryDeviceUSB.SectorSize
        Return CInt(MyFlashDevice.GetSectorSize(sector))
    End Function

    Public Function ReadData(base_addr As Long, read_count As Integer) As Byte() Implements MemoryDeviceUSB.ReadData
        Dim byte_count As Integer = read_count
        Dim data_out(byte_count - 1) As Byte 'Bytes we want to read
        Dim data_left As Integer = byte_count
        Dim ptr As Integer = 0
        While data_left > 0
            Dim packet_size As Integer = Math.Min(8192, data_left)
            Dim packet_data(packet_size - 1) As Byte
            Dim setup_data() As Byte = GetSetupPacket_NOR(base_addr, packet_size, 0)
            Dim result As Boolean = FCUSB.USB_SETUP_BULKIN(USBREQ.JTAG_BDR_RDFLASH, setup_data, packet_data, 0)
            If Not result Then Return Nothing
            Array.Copy(packet_data, 0, data_out, ptr, packet_size)
            ptr += packet_size
            base_addr += CUInt(packet_size)
            data_left -= packet_size
        End While
        Return data_out
    End Function

    Public Function WriteData(base_addr As Long, data_to_write() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        Dim DataToWrite As Integer = data_to_write.Length
        Dim PacketSize As Integer = 8192
        Dim Loops As Integer = CInt(Math.Ceiling(DataToWrite / PacketSize)) 'Calcuates iterations
        For i As Integer = 0 To Loops - 1
            Dim BufferSize As Integer = DataToWrite
            If (BufferSize > PacketSize) Then BufferSize = PacketSize
            Dim data(BufferSize - 1) As Byte
            Array.Copy(data_to_write, (i * PacketSize), data, 0, data.Length)
            Dim setup_data() As Byte = GetSetupPacket_NOR(base_addr, data.Length, MyFlashDevice.PAGE_SIZE)
            Dim BSDL_PROG_CMD As UInt32 = CUInt(MyFlashDevice.WriteMode)
            If Not FCUSB.USB_SETUP_BULKOUT(USBREQ.JTAG_BDR_WRFLASH, setup_data, data, BSDL_PROG_CMD) Then Return False
            Utilities.Sleep(200) 'ORG 350
            base_addr += CUInt(data.Length)
            If Not FCUSB.USB_WaitForComplete() Then Return False
        Next
        If MyFlashDevice.RESET_ENABLED Then ResetDevice()
        Return True
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        If MyFlashDevice.WriteMode = MFP_PRG.IntelSharp Or MyFlashDevice.WriteMode = MFP_PRG.Buffer1 Then
            BSR_WriteMemAddress(&H0, &H30)
            BSR_WriteMemAddress(&H0, &HD0)
        Else
            BSR_WriteCmdData(&H5555, &HAA)
            BSR_WriteCmdData(&H2AAA, &H55)
            BSR_WriteCmdData(&H5555, &H80)
            BSR_WriteCmdData(&H5555, &HAA)
            BSR_WriteCmdData(&H2AAA, &H55)
            BSR_WriteCmdData(&H5555, &H10)
        End If
        Utilities.Sleep(500)
        Dim counter As Integer = 0
        Dim dw As UInt16
        Do Until dw = &HFFFF
            Utilities.Sleep(100)
            dw = BSR_ReadWord(0)
            counter += 1
            If counter = 250 Then Return False
        Loop
        Return True
    End Function

    Public Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
        Utilities.Sleep(100) 'Some flash devices have registers, some rely on delays
    End Sub

    Public Function SectorFind(sector_index As Integer) As Long Implements MemoryDeviceUSB.SectorFind
        Dim base_addr As UInt32 = 0
        If (sector_index > 0) Then
            For i As Integer = 0 To sector_index - 1
                base_addr += CUInt(SectorSize(i))
            Next
        End If
        Return base_addr
    End Function

    Public Function SectorErase(sector_index As Integer) As Boolean Implements MemoryDeviceUSB.SectorErase
        Dim sector_addr As UInt32 = CUInt(SectorFind(sector_index))
        BSR_EraseSector(sector_addr)
        Utilities.Sleep(100)
        Dim counter As Integer = 0
        Dim dw As UInt16 = 0
        Dim erased_value As UInt16 = &HFF
        If CURRENT_BUS_WIDTH = E_BSR_BUS_WIDTH.X16 Then erased_value = &HFFFF
        Do Until dw = erased_value
            Utilities.Sleep(20)
            dw = BSR_ReadWord(sector_addr)
            counter += 1
            If counter = 100 Then Return False
        Loop
        Return True
    End Function

    Public Function SectorCount() As Integer Implements MemoryDeviceUSB.SectorCount
        Return MyFlashDevice.Sector_Count
    End Function

    Public Function SectorWrite(sector_index As Integer, data() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.SectorWrite
        Dim Addr32 As UInteger = CUInt(SectorFind(sector_index))
        Return WriteData(Addr32, data)
    End Function

#End Region

#Region "BSR Functions"

    Private Function BSR_GetCFI() As Byte()
        Dim cfi_data(31) As Byte
        Dim SHIFT As Integer = 0
        If Me.CURRENT_BUS_WIDTH = E_BSR_BUS_WIDTH.X16 Then SHIFT = 1
        Try
            BSR_WriteCmdData(&H55, &H98)
            For i = 0 To cfi_data.Length - 1
                cfi_data(i) = CByte(BSR_ReadWord(CUInt((&H10 + i) << SHIFT)) And 255)
            Next
            If cfi_data(0) = &H51 And cfi_data(1) = &H52 And cfi_data(2) = &H59 Then Return cfi_data
            BSR_WriteCmdData(&H5555, &HAA)
            BSR_WriteCmdData(&H2AAA, &H55)
            BSR_WriteCmdData(&H5555, &H98)
            For i As Integer = 0 To cfi_data.Length - 1
                cfi_data(i) = CByte(BSR_ReadWord(CUInt((&H10 + i) << SHIFT)) And 255)
            Next
            If cfi_data(0) = &H51 And cfi_data(1) = &H52 And cfi_data(2) = &H59 Then Return cfi_data
        Catch ex As Exception
        Finally
            ResetDevice()
        End Try
        Return Nothing
    End Function

    Private Function BSR_ReadIdent() As Byte()
        Dim ident(7) As Byte
        Dim SHIFT As Integer = 0
        If Me.CURRENT_BUS_WIDTH = E_BSR_BUS_WIDTH.X16 Then SHIFT = 1
        ResetDevice()
        Utilities.Sleep(1)
        BSR_WriteCmdData(&H5555, &HAA)
        BSR_WriteCmdData(&H2AAA, &H55)
        BSR_WriteCmdData(&H5555, &H90)
        Utilities.Sleep(10)
        ident(0) = CByte(BSR_ReadWord(0) And &HFF)                'MFG
        Dim ID1 As UInt16 = BSR_ReadWord(1UI << SHIFT)
        If (ModeSelect = MEM_PROTOCOL.NOR_X8) Then ID1 = (ID1 And &HFFUS) 'X8 ID1
        ident(1) = CByte((ID1 >> 8) And &HFF)                     'ID1(UPPER)
        ident(2) = CByte(ID1 And &HFF)                            'ID1(LOWER)
        ident(3) = CByte(BSR_ReadWord(&HEUI << SHIFT) And &HFF)   'ID2
        ident(4) = CByte(BSR_ReadWord(&HFUI << SHIFT) And &HFF)   'ID3
        ResetDevice()
        Utilities.Sleep(1)
        Me.CURRENT_SECTOR_ERASE = E_BSR_SECTOR.Standard
        Me.CURRENT_CHIP_ERASE = E_BSR_CHIPERASE.Standard
        Me.CURRENT_WRITE_MODE = E_BSR_WRITEDATA.Standard
        Return ident
    End Function

    Private Function BSR_ReadWord(base_addr As UInt32) As UInt16
        Dim dt(3) As Byte
        Dim result As Boolean = FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_BDR_RDMEM, dt, base_addr)
        Dim DQ16 As UInt16 = (CUShort(dt(1)) << 8) Or CUShort(dt(0))
        If Me.CURRENT_BUS_WIDTH = E_BSR_BUS_WIDTH.X16 Then
            Return DQ16
        ElseIf Me.CURRENT_BUS_WIDTH = E_BSR_BUS_WIDTH.X8 Then
            Return CUShort(DQ16 And 255)
        End If
        Return 0
    End Function

    Private Sub BSR_WriteCmdData(base_addr As UInt32, data16 As UInt16)
        Dim dt_out(5) As Byte
        dt_out(0) = CByte(base_addr And 255)
        dt_out(1) = CByte((base_addr >> 8) And 255)
        dt_out(2) = CByte((base_addr >> 16) And 255)
        dt_out(3) = CByte((base_addr >> 24) And 255)
        dt_out(4) = CByte(data16 And 255)
        dt_out(5) = CByte((data16 >> 8) And 255)
        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.JTAG_BDR_WRCMD, dt_out)
    End Sub

    Private Sub BSR_WriteMemAddress(base_addr As UInt32, data16 As UInt16)
        Dim dt_out(5) As Byte
        dt_out(0) = CByte(base_addr And 255)
        dt_out(1) = CByte((base_addr >> 8) And 255)
        dt_out(2) = CByte((base_addr >> 16) And 255)
        dt_out(3) = CByte((base_addr >> 24) And 255)
        dt_out(4) = CByte(data16 And 255)
        dt_out(5) = CByte((data16 >> 8) And 255)
        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.JTAG_BDR_WRMEM, dt_out)
    End Sub

    Private Sub BSR_EraseSector(sector_addr As UInt32)
        Select Case CURRENT_SECTOR_ERASE
            Case E_BSR_SECTOR.Standard
                BSR_EraseSector_Standard(sector_addr)
            Case E_BSR_SECTOR.Intel
                BSR_EraseSector_Intel(sector_addr)
        End Select
    End Sub

    Private Sub BSR_EraseSector_Standard(sector_addr As UInt32)
        'Write Unlock Cycles
        BSR_WriteCmdData(&H5555, &HAA)
        BSR_WriteCmdData(&H2AAA, &H55)
        'Write Sector Erase Cycles
        BSR_WriteCmdData(&H5555, &H80)
        BSR_WriteCmdData(&H5555, &HAA)
        BSR_WriteCmdData(&H2AAA, &H55)
        BSR_WriteMemAddress(sector_addr, &H30)
    End Sub

    Private Sub BSR_EraseSector_Intel(sector_addr As UInt32)
        BSR_WriteMemAddress(sector_addr, &H50) 'clear register
        BSR_WriteMemAddress(sector_addr, &H60) 'Unlock block (just in case)
        BSR_WriteMemAddress(sector_addr, &HD0) 'Confirm Command
        Utilities.Sleep(20)
        BSR_WriteMemAddress(sector_addr, &H20)
        BSR_WriteMemAddress(sector_addr, &HD0)
        Utilities.Sleep(20)
        BSR_WriteMemAddress(0, &HFF) 'Puts the device back into READ mode
        BSR_WriteMemAddress(0, &HF0)
    End Sub

    Public Sub ResetDevice()
        BSR_WriteCmdData(&H5555, &HAA) 'Standard
        BSR_WriteCmdData(&H2AAA, &H55)
        BSR_WriteCmdData(&H5555, &HF0)
        BSR_WriteCmdData(0, &HFF)
        BSR_WriteCmdData(0, &HF0) 'Intel
    End Sub

#End Region

    'This should mirror the same function in PARALLEL_NOR.vb
    Private Function GetSetupPacket_NOR(Address As Long, Count As Integer, PageSize As UInt16) As Byte()
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

End Class
