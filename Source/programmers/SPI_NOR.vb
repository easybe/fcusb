'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2021 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
'CONTACT EMAIL: support@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'ACKNOWLEDGEMENT: USB driver functionality provided by LibUsbDotNet (sourceforge.net/projects/libusbdotnet) 

Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB

Namespace SPI

    Public Class SPI_Programmer : Implements MemoryDeviceUSB
        Private FCUSB As FCUSB_DEVICE
        Public Event PrintConsole(msg As String) Implements MemoryDeviceUSB.PrintConsole
        Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress
        Public Property MyFlashDevice As SPI_NOR 'Contains the definition of the Flash device that is connected
        Public Property MyFlashStatus As DeviceStatus = DeviceStatus.NotDetected
        Public Property DIE_SELECTED As Integer = 0
        Public Property ExtendedPage As Boolean = False 'Indicates we should use extended pages
        Public Property SPI_FASTREAD As Boolean = False 'Uses 0xB and dummy cycle

        Sub New(parent_if As FCUSB_DEVICE)
            Me.FCUSB = parent_if
        End Sub

        Friend Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
            Me.ExtendedPage = False
            MyFlashStatus = DeviceStatus.NotDetected
            SPIBUS_WriteRead({&HC2, 0}) 'always select die 0
            Dim ReadSuccess As Boolean = False
            Dim MFG As Byte
            Dim ID1 As UInt16
            Dim ID2 As UInt16
            Dim DEVICEID As SPI_IDENT = ReadDeviceID() 'Sends RDID/REMS/RES command and reads back
            If (DEVICEID.MANU = &HFF Or DEVICEID.MANU = 0) Then 'Check REMS
                If Not ((DEVICEID.REMS = &HFFFF) Or (DEVICEID.REMS = &H0)) Then
                    MFG = CByte(DEVICEID.REMS >> 8)
                    ID1 = CByte(DEVICEID.REMS And 255)
                    ReadSuccess = True 'RDID did not return anything, but REMS did
                End If
            ElseIf (DEVICEID.RDID = &HFFFFFFFFUI) Or (DEVICEID.RDID = 0) Then
            Else
                MFG = DEVICEID.MANU
                ID1 = CUShort(DEVICEID.RDID >> 16)
                ID2 = CUShort(DEVICEID.RDID And &HFFFF)
                ReadSuccess = True
            End If
            If ReadSuccess Then
                RaiseEvent PrintConsole(RM.GetString("spi_device_opened"))
                Dim RDID_Str As String = "0x" & Hex(DEVICEID.MANU).PadLeft(2, "0"c) & Hex((DEVICEID.RDID And &HFFFF0000UL) >> 16).PadLeft(4, "0"c)
                Dim RDID2_Str As String = Hex(DEVICEID.RDID And &HFFFF).PadLeft(4, "0"c)
                Dim REMS_Str As String = "0x" & Hex(DEVICEID.REMS).PadLeft(4, "0"c)
                RaiseEvent PrintConsole(String.Format(RM.GetString("spi_connected_to_flash_spi"), RDID_Str, REMS_Str))
                MyFlashDevice = CType(FlashDatabase.FindDevice(MFG, ID1, ID2, MemoryType.SERIAL_NOR, DEVICEID.FMY), SPI_NOR)
                If MyFlashDevice IsNot Nothing Then
                    MyFlashStatus = DeviceStatus.Supported
                    ResetDevice()
                    LoadDeviceConfigurations() 'Does device settings (4BYTE mode, unlock global block)
                    LoadVendorSpecificConfigurations() 'Some devices may need additional configurations
                    RaiseEvent PrintConsole(String.Format(RM.GetString("flash_detected"), Me.DeviceName, Format(Me.DeviceSize, "#,###")))
                    RaiseEvent PrintConsole(String.Format(RM.GetString("spi_flash_page_size"), Format(Me.SectorSize(0), "#,###")))
                    Utilities.Sleep(50)
                    Return True
                Else
                    MyFlashDevice = New SPI_NOR("Unknown", VCC_IF.SERIAL_3V, 0UI, DEVICEID.MANU, ID1)
                    MyFlashStatus = DeviceStatus.NotSupported
                    Return False
                End If
            Else
                MyFlashStatus = DeviceStatus.NotDetected
                RaiseEvent PrintConsole(RM.GetString("spi_flash_not_detected"))
                Return False
            End If
        End Function

#Region "Public Interface"

        Public ReadOnly Property GetDevice As Device Implements MemoryDeviceUSB.GetDevice
            Get
                Return Me.MyFlashDevice
            End Get
        End Property

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
                If Not MyFlashStatus = USB.DeviceStatus.Supported Then Return 0
                If Me.ExtendedPage Then
                    Return (MyFlashDevice.PAGE_COUNT * MyFlashDevice.PAGE_SIZE_EXTENDED)
                Else
                    Return MyFlashDevice.FLASH_SIZE
                End If
            End Get
        End Property

        Friend Function SectorSize(sector As Integer) As Integer Implements MemoryDeviceUSB.SectorSize
            If Not MyFlashStatus = USB.DeviceStatus.Supported Then Return 0
            If MyFlashDevice.ERASE_REQUIRED Then
                If Me.ExtendedPage Then
                    Dim page_count As Integer = CInt(MyFlashDevice.ERASE_SIZE \ MyFlashDevice.PAGE_SIZE)
                    Return (MyFlashDevice.PAGE_SIZE_EXTENDED * page_count)
                Else
                    Return CInt(MyFlashDevice.ERASE_SIZE)
                End If
            Else
                Return CInt(MyFlashDevice.FLASH_SIZE)
            End If
        End Function

        Friend Function SectorFind(sector_index As Integer) As Long Implements MemoryDeviceUSB.SectorFind
            If sector_index = 0 Then Return 0 'Addresses start at the base address 
            Return Me.SectorSize(0) * sector_index
        End Function

        Friend Function SectorCount() As Integer Implements MemoryDeviceUSB.SectorCount
            If MyFlashStatus = USB.DeviceStatus.Supported Then
                Dim EraseSize As UInt32 = MyFlashDevice.ERASE_SIZE
                If EraseSize = 0 Then Return 1
                Dim FlashSize As Long = Me.DeviceSize()
                If FlashSize < EraseSize Then Return 1
                Return CInt(FlashSize / EraseSize)
            End If
            Return 0
        End Function

        Friend Function ReadData(flash_offset As Long, data_count As Integer) As Byte() Implements MemoryDeviceUSB.ReadData
            Dim flash_offset32 As UInt32 = CUInt(flash_offset)
            'If W25M121AV_DETECTED Then SPIBUS_WriteRead({&HC2, 0}) : WaitUntilReady()
            Dim data_to_read(data_count - 1) As Byte
            Dim bytes_left As Integer = data_count
            Dim buffer_size As Integer = 0
            Dim array_ptr As Integer = 0
            Dim read_cmd As Byte = MyFlashDevice.OP_COMMANDS.READ
            Dim dummy_clocks As Byte = 0
            If Me.SPI_FASTREAD AndAlso FCUSB.HasLogic() Then
                read_cmd = MyFlashDevice.OP_COMMANDS.FAST_READ
            End If
            If (Me.MyFlashDevice.ProgramMode = FlashMemory.SPI_PROG.Atmel45Series) Then
                Return AT45_ReadData(flash_offset32, data_count)
            ElseIf Me.MyFlashDevice.ProgramMode = FlashMemory.SPI_PROG.SPI_EEPROM Then
                If MyFlashDevice.ADDRESSBITS = 8 Then 'Used on ST M95010 - M95040 (8bit) and ATMEL devices (AT25010A - AT25040A)
                    read_cmd = read_cmd Or CByte(flash_offset32 >> 5)
                    flash_offset32 = (flash_offset32 And 255UI)
                End If
                Dim setup_class As New ReadSetupPacket(read_cmd, flash_offset32, data_to_read.Length, MyFlashDevice.AddressBytes) With {.DUMMY = dummy_clocks}
                FCUSB.USB_SETUP_BULKIN(USBREQ.SPI_READFLASH, setup_class.ToBytes, data_to_read, 0)
            Else 'Normal SPI READ
                If Me.SPI_FASTREAD AndAlso (FCUSB.HasLogic()) Then
                    dummy_clocks = MyFlashDevice.SPI_DUMMY
                End If
                If (MyFlashDevice.STACKED_DIES > 1) Then
                    Do Until bytes_left = 0
                        Dim die_address As UInt32 = GetAddressForMultiDie(flash_offset32, bytes_left, buffer_size)
                        Dim die_data(buffer_size - 1) As Byte
                        Dim setup_class As New ReadSetupPacket(read_cmd, die_address, die_data.Length, MyFlashDevice.AddressBytes) With {.DUMMY = dummy_clocks}
                        FCUSB.USB_SETUP_BULKIN(USBREQ.SPI_READFLASH, setup_class.ToBytes, die_data, 0)
                        Array.Copy(die_data, 0, data_to_read, array_ptr, die_data.Length) : array_ptr += buffer_size
                    Loop
                Else
                    Dim setup_class As New ReadSetupPacket(read_cmd, flash_offset32, data_to_read.Length, MyFlashDevice.AddressBytes) With {.DUMMY = dummy_clocks}
                    FCUSB.USB_SETUP_BULKIN(USBREQ.SPI_READFLASH, setup_class.ToBytes, data_to_read, 0)
                End If
            End If
            Return data_to_read
        End Function

        Friend Function WriteData(flash_offset As Long, data_to_write() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
            Dim flash_offset32 As UInt32 = CUInt(flash_offset)
            'If W25M121AV_DETECTED Then SPIBUS_WriteRead({&HC2, 0}) : WaitUntilReady()
            Dim bytes_left As Integer = data_to_write.Length
            Dim buffer_size As Integer = 0
            Dim array_ptr As Integer = 0
            Select Case MyFlashDevice.ProgramMode
                Case SPI_PROG.PageMode
                    If (MyFlashDevice.STACKED_DIES > 1) Then 'Multi-die support
                        Dim write_result As Boolean
                        Do Until bytes_left = 0
                            Dim die_address As UInt32 = GetAddressForMultiDie(flash_offset32, bytes_left, buffer_size)
                            Dim die_data(buffer_size - 1) As Byte
                            Array.Copy(data_to_write, array_ptr, die_data, 0, die_data.Length) : array_ptr += buffer_size
                            Dim setup_packet As New WriteSetupPacket(MyFlashDevice, die_address, die_data.Length)
                            write_result = WriteData_Flash(setup_packet, die_data)
                            If Not write_result Then Return False
                        Loop
                        Return write_result
                    Else
                        Dim setup_packet As New WriteSetupPacket(MyFlashDevice, flash_offset32, data_to_write.Length)
                        Return WriteData_Flash(setup_packet, data_to_write)
                    End If
                Case SPI_PROG.SPI_EEPROM 'Used on most ST M95080 and above
                    Return WriteData_SPI_EEPROM(flash_offset32, data_to_write)
                Case SPI_PROG.AAI_Byte
                    Return WriteData_AAI(flash_offset32, data_to_write, False)
                Case SPI_PROG.AAI_Word
                    Return WriteData_AAI(flash_offset32, data_to_write, True)
                Case SPI_PROG.Atmel45Series
                    Return AT45_WriteData(flash_offset32, data_to_write)
                Case SPI_PROG.Nordic
                    Dim data_left As Integer = data_to_write.Length
                    Dim ptr As Integer = 0
                    Do While (data_left > 0)
                        SPIBUS_WriteEnable()
                        Dim packet_data(MyFlashDevice.PAGE_SIZE - 1) As Byte
                        Array.Copy(data_to_write, ptr, packet_data, 0, packet_data.Length)
                        Dim setup_packet As New WriteSetupPacket(MyFlashDevice, CUInt(flash_offset32 + ptr), packet_data.Length)
                        Dim write_result As Boolean = WriteData_Flash(setup_packet, packet_data)
                        If Not write_result Then Return False
                        WaitUntilReady()
                        ptr += packet_data.Length
                        data_left -= packet_data.Length
                    Loop
                    Return True
            End Select
            Return False
        End Function

        Friend Function SectorErase(sector_index As Integer) As Boolean Implements MemoryDeviceUSB.SectorErase
            'If W25M121AV_DETECTED Then SPIBUS_WriteRead({&HC2, 0}) : WaitUntilReady()
            If (Not MyFlashDevice.ERASE_REQUIRED) Then Return True 'Erase not needed
            Dim flash_offset As UInt32 = CUInt(Me.SectorFind(sector_index))
            If MyFlashDevice.ProgramMode = SPI_PROG.Atmel45Series Then
                AT45_EraseSector(flash_offset)
            ElseIf MyFlashDevice.ProgramMode = SPI_PROG.Nordic Then
                SPIBUS_WriteEnable()
                Dim PageNum As Byte = CByte(flash_offset \ MyFlashDevice.ERASE_SIZE)
                SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.SE, PageNum}, Nothing)
            Else
                SPIBUS_WriteEnable()
                If (MyFlashDevice.STACKED_DIES > 1) Then 'Multi-die support
                    flash_offset = GetAddressForMultiDie(flash_offset, 0, 0)
                End If
                Dim DataToWrite() As Byte = GetArrayWithCmdAndAddr(MyFlashDevice.OP_COMMANDS.SE, flash_offset)
                SPIBUS_WriteRead(DataToWrite, Nothing)
                If MyFlashDevice.SEND_RDFS Then
                    ReadFlagStatusRegister()
                Else
                    Utilities.Sleep(10)
                End If
            End If
            WaitUntilReady()
            Return True
        End Function

        Friend Function SectorWrite(sector_index As Integer, data() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.SectorWrite
            'If W25M121AV_DETECTED Then SPIBUS_WriteRead({&HC2, 0}) : WaitUntilReady()
            Dim Addr32 As Long = Me.SectorFind(sector_index)
            Return WriteData(Addr32, data, Params)
        End Function

        Friend Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
            RaiseEvent PrintConsole(String.Format(RM.GetString("spi_erasing_flash_device"), Format(Me.DeviceSize, "#,###")))
            Dim erase_timer As New Stopwatch : erase_timer.Start()
            'If W25M121AV_DETECTED Then SPIBUS_WriteRead({&HC2, 0}) : WaitUntilReady()
            If MyFlashDevice.ProgramMode = FlashMemory.SPI_PROG.Atmel45Series Then
                SPIBUS_WriteRead({&HC7, &H94, &H80, &H9A}, Nothing) 'Chip erase command
                Utilities.Sleep(100)
                WaitUntilReady()
            ElseIf MyFlashDevice.ProgramMode = SPI_PROG.SPI_EEPROM Then
                Dim eeprom_size As Integer = CInt(MyFlashDevice.FLASH_SIZE)
                Dim data(eeprom_size - 1) As Byte
                Utilities.FillByteArray(data, 255)
                WriteData(0, data)
            ElseIf MyFlashDevice.ProgramMode = SPI_PROG.Nordic Then
                'This device does support chip-erase, but it will also erase the InfoPage content
                Dim nord_timer As New Stopwatch : nord_timer.Start()
                RaiseEvent PrintConsole(String.Format(RM.GetString("spi_erasing_flash_device"), Format(Me.DeviceSize, "#,###")))
                Dim TotalPages As Integer = CInt(MyFlashDevice.FLASH_SIZE \ MyFlashDevice.PAGE_SIZE)
                For i = 0 To TotalPages - 1
                    SPIBUS_WriteEnable() : Utilities.Sleep(50)
                    SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.SE, CByte(i)}, Nothing)
                    WaitUntilReady()
                Next
            Else
                Select Case MyFlashDevice.CHIP_ERASE
                    Case EraseMethod.Standard
                        SPIBUS_WriteEnable()
                        SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.BE}, Nothing) '&HC7
                        If MyFlashDevice.SEND_RDFS Then ReadFlagStatusRegister()
                        WaitUntilReady()
                    Case EraseMethod.BySector
                        Dim SectorCount As Integer = MyFlashDevice.Sector_Count
                        RaiseEvent SetProgress(0)
                        For i As Integer = 0 To SectorCount - 1
                            If Not SectorErase(i) Then
                                RaiseEvent SetProgress(0) : Return False 'Error erasing sector
                            Else
                                Dim progress As Single = CSng((i / SectorCount) * 100)
                                RaiseEvent SetProgress(CInt(Math.Floor(progress)))
                            End If
                        Next
                        RaiseEvent SetProgress(0) 'Device successfully erased
                    Case EraseMethod.DieErase
                        EraseDie()
                    Case EraseMethod.Micron
                        Dim internal_timer As New Stopwatch
                        internal_timer.Start()
                        SPIBUS_WriteEnable() 'Try Chip Erase first
                        SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.BE}, Nothing)
                        If MyFlashDevice.SEND_RDFS Then ReadFlagStatusRegister()
                        WaitUntilReady()
                        internal_timer.Stop()
                        If (internal_timer.ElapsedMilliseconds < 1000) Then 'Command not supported, use DIE ERASE instead
                            EraseDie()
                        End If
                End Select
            End If
            RaiseEvent PrintConsole(String.Format(RM.GetString("spi_erase_complete"), Format(erase_timer.ElapsedMilliseconds / 1000, "#.##")))
            Return True
        End Function
        'Reads the SPI status register and waits for the device to complete its current operation
        Friend Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
            Try
                Dim status As Byte
                If MyFlashDevice.ProgramMode = SPI_PROG.Atmel45Series Then
                    Do
                        Dim sr() As Byte = ReadStatusRegister() 'Check Bit 7 (RDY/BUSY#)
                        status = sr(0)
                        If Not ((status And &H80) > 0) Then Utilities.Sleep(10)
                    Loop While Not ((status And &H80) > 0)
                Else
                    Do
                        Dim sr() As Byte = ReadStatusRegister()
                        status = sr(0)
                        If AppIsClosing Then Exit Sub
                        If status = 255 Then Exit Do
                        If ((status And CByte(1)) = CByte(1)) Then Utilities.Sleep(5)
                    Loop While ((status And CByte(1)) = 1)
                    If MyFlashDevice IsNot Nothing AndAlso MyFlashDevice.ProgramMode = SPI_PROG.Nordic Then
                        Utilities.Sleep(50)
                    End If
                End If
            Catch ex As Exception
            End Try
        End Sub

#End Region

        Private Sub LoadDeviceConfigurations()
            If MyFlashDevice.VENDOR_SPECIFIC = VENDOR_FEATURE.NotSupported Then 'We don't want to do this for vendor enabled devices
                WriteStatusRegister({0})
                Utilities.Sleep(100) 'Needed, some devices will lock up if else.
            End If
            If (MyFlashDevice.STACKED_DIES > 1) Then 'Multi-die support
                For i As Byte = 0 To CByte(MyFlashDevice.STACKED_DIES - 1)
                    SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.DIESEL, i}) : WaitUntilReady() 'We need to make sure DIE 0 is selected
                    If MyFlashDevice.SEND_EN4B Then
                        SPIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.EN4B) 'Set options for each DIE
                    End If
                    SPIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.ULBPR) '0x98 (global block unprotect)
                Next
                SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.DIESEL, 0}) : WaitUntilReady() 'We need to make sure DIE 0 is selected
                Me.DIE_SELECTED = 0
            ElseIf MyFlashDevice.ProgramMode = SPI_PROG.Atmel45Series Then
                SPIBUS_WriteRead({&H3D, &H2A, &H7F, &H9A}, Nothing) 'Disable sector protection
            Else
                If MyFlashDevice.SEND_EN4B Then SPIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.EN4B) '0xB7
                SPIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.ULBPR) '0x98 (global block unprotect)
            End If
        End Sub

        Private Sub LoadVendorSpecificConfigurations()
            If (MyFlashDevice.ProgramMode = SPI_PROG.Atmel45Series) Then 'May need to load the current page mode
                Dim sr() As Byte = ReadStatusRegister() 'Some devices have 2 SR
                Me.ExtendedPage = ((sr(0) And 1) = 0)
                Dim page_size As UInt16 = CUShort(IIf(Me.ExtendedPage, MyFlashDevice.PAGE_SIZE_EXTENDED, MyFlashDevice.PAGE_SIZE))
                RaiseEvent PrintConsole("Device configured to page size: " & page_size & " bytes")
                Exit Sub
            End If
            If (MyFlashDevice.MFG_CODE = &HBF) Then 'SST26VF016/SST26VF032 requires block protection to be removed in SQI only
                If MyFlashDevice.ID1 = &H2601 Or MyFlashDevice.ID1 = &H2602 Then
                    RaiseEvent PrintConsole("SQI mode must be used to remove block protection")
                End If
                Exit Sub
            End If
            If (MyFlashDevice.MFG_CODE = &H9D) Then 'ISSI
                WriteStatusRegister({0}) 'Erase protection bits
                Exit Sub
            End If
            'If (MyFlashDevice.MFG_CODE = &HEF) AndAlso (MyFlashDevice.ID1 = &H4018) Then
            '    SPIBUS_WriteRead({&HC2, 1}) : WaitUntilReady() 'Check to see if this device has two dies
            '    Dim id As SPI_IDENT = ReadDeviceID()
            '    If (id.RDID = &HEFAB2100UI) Then W25M121AV_DETECTED = True
            'End If
            If (MyFlashDevice.MFG_CODE = &H34) Then 'Cypress MFG ID
                Dim SEMPER_SPI As Boolean = False
                If MyFlashDevice.ID1 = &H2A19 Then SEMPER_SPI = True
                If MyFlashDevice.ID1 = &H2A1A Then SEMPER_SPI = True
                If MyFlashDevice.ID1 = &H2A1B Then SEMPER_SPI = True
                If MyFlashDevice.ID1 = &H2B19 Then SEMPER_SPI = True
                If MyFlashDevice.ID1 = &H2B1A Then SEMPER_SPI = True
                If MyFlashDevice.ID1 = &H2B1B Then SEMPER_SPI = True
                If SEMPER_SPI Then
                    SPIBUS_WriteEnable() 'WRENB_0_0
                    SPIBUS_WriteRead({&H71, 0, 0, 0, &H4, &H18}) 'Enables 512-byte buffer / 256KB sectors
                End If
                Dim SEMPER_SPI_HF As Boolean = False 'Semper HF/SPI version
                If (MyFlashDevice.MFG_CODE = &H34) Then
                    If MyFlashDevice.ID1 = &H6B AndAlso MyFlashDevice.ID2 = &H19 Then SEMPER_SPI_HF = True
                    If MyFlashDevice.ID1 = &H6B AndAlso MyFlashDevice.ID2 = &H1A Then SEMPER_SPI_HF = True
                    If MyFlashDevice.ID1 = &H6B AndAlso MyFlashDevice.ID2 = &H1B Then SEMPER_SPI_HF = True
                    If MyFlashDevice.ID1 = &H6A AndAlso MyFlashDevice.ID2 = &H19 Then SEMPER_SPI_HF = True
                    If MyFlashDevice.ID1 = &H6A AndAlso MyFlashDevice.ID2 = &H1A Then SEMPER_SPI_HF = True
                    If MyFlashDevice.ID1 = &H6A AndAlso MyFlashDevice.ID2 = &H1B Then SEMPER_SPI_HF = True
                End If
                If SEMPER_SPI_HF Then
                    SPIBUS_WriteEnable()
                    SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.EWSR}) 'WRENV
                    SPIBUS_WriteRead({&H71, 0, &H80, 0, 4, &H18}) 'Enables 512-byte buffer
                End If
                Exit Sub
            End If

            If (MyFlashDevice.MFG_CODE = &H1 AndAlso MyFlashDevice.ID1 = &H2018) Then
                If (MyFlashDevice.ID2 = &H4D01 AndAlso MyFlashDevice.FAMILY = &H81) Then 'S25FS-S Family
                    'Some devices has 8 blocks of 4KB at the top or bottom and you must use the 4K Erase command
                End If
            End If

        End Sub

        Private Function ReadDeviceID() As SPI_IDENT
            Dim DEVICEID As New SPI_IDENT
            Dim rdid(5) As Byte
            SPIBUS_WriteRead({SPI_OPCODES.RDID}, rdid) 'This reads SPI CHIP ID
            Dim rems(1) As Byte
            SPIBUS_WriteRead({SPI_OPCODES.REMS, 0, 0, 0}, rems) 'Some devices (such as SST25VF512) only support REMS
            Dim res(0) As Byte
            SPIBUS_WriteRead({SPI_OPCODES.RES, 0, 0, 0}, res)
            DEVICEID.MANU = rdid(0)
            DEVICEID.RDID = (CUInt(rdid(1)) << 24) Or (CUInt(rdid(2)) << 16) Or (CUInt(rdid(3)) << 8) Or CUInt(rdid(4))
            DEVICEID.FMY = rdid(5)
            DEVICEID.REMS = (CUShort(rems(0)) << 8) Or rems(1)
            DEVICEID.RES = res(0)
            Return DEVICEID
        End Function
        'This writes to the SR (multi-bytes can be input to write as well)
        Public Function WriteStatusRegister(NewValues() As Byte) As Boolean
            Try
                If NewValues Is Nothing Then Return False
                SPIBUS_WriteEnable() 'Some devices such as AT25DF641 require the WREN and the status reg cleared before we can write data
                If MyFlashDevice.SEND_EWSR Then
                    SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.EWSR}, Nothing) 'Send the command that we are going to enable-write to register
                    Threading.Thread.Sleep(20) 'Wait a brief moment
                End If
                Dim cmd(NewValues.Length) As Byte
                cmd(0) = MyFlashDevice.OP_COMMANDS.WRSR
                Array.Copy(NewValues, 0, cmd, 1, NewValues.Length)
                If Not SPIBUS_WriteRead(cmd, Nothing) = cmd.Length Then Return False
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function ReadStatusRegister(Optional Count As Integer = 1) As Byte()
            Try
                Dim Output(Count - 1) As Byte
                SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.RDSR}, Output) '0x05
                Return Output
            Catch ex As Exception
                Return Nothing 'Erorr
            End Try
        End Function

        Private Function GetArrayWithCmdAndAddr(cmd As Byte, addr_offset As UInt32) As Byte()
            Dim addr_data() As Byte = BitConverter.GetBytes(addr_offset)
            ReDim Preserve addr_data(MyFlashDevice.AddressBytes - 1)
            Array.Reverse(addr_data)
            Dim data_out(MyFlashDevice.AddressBytes) As Byte
            data_out(0) = cmd
            For i = 1 To data_out.Length - 1
                data_out(i) = addr_data(i - 1)
            Next
            Return data_out
        End Function

        Private Sub ReadFlagStatusRegister()
            Utilities.Sleep(10)
            Dim flag() As Byte = {0}
            Do
                SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.RDFR}, flag)
            Loop Until (((flag(0) >> 7) And 1) = 1)
        End Sub

        Private Sub EraseDie()
            Dim die_size As UInt32 = &H2000000UI
            Dim die_count As Integer = CInt(MyFlashDevice.FLASH_SIZE / die_size)
            For x As Integer = 0 To die_count - 1
                RaiseEvent PrintConsole(String.Format(RM.GetString("spi_erasing_die"), x.ToString, Format(die_size, "#,###")))
                Dim die_addr() As Byte = Utilities.Bytes.FromUInt32(CUInt(x) * die_size)
                SPIBUS_WriteEnable()
                SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.DE, die_addr(0), die_addr(1), die_addr(1), die_addr(1)}, Nothing) '&HC4
                Utilities.Sleep(1000)
                If MyFlashDevice.SEND_RDFS Then ReadFlagStatusRegister()
                WaitUntilReady()
            Next
        End Sub

        Public Sub ResetDevice()
            SPIBUS_WriteRead({&HF0}) 'SPI RESET COMMAND
            'Other commands: 0x66 and 0x99
            Utilities.Sleep(10)
        End Sub

#Region "AT45 DataFlash"
        'Changes the Page Size configuration bit in nonvol
        Public Function AT45_SetPageConfiguration(EnableExtPage As Boolean) As Boolean
            If (EnableExtPage) Then
                SPIBUS_WriteRead({&H3D, &H2A, &H80, &HA7}, Nothing) '528 bytes per page
            Else '512 bytes per page
                SPIBUS_WriteRead({&H3D, &H2A, &H80, &HA6}, Nothing) 'One-time programmable on some devices?
            End If
            Utilities.Sleep(20)
            WaitUntilReady()
            Dim sr() As Byte = ReadStatusRegister() 'Some devices have 2 SR
            If EnableExtPage = ((sr(0) And 1) = 0) Then
                Me.ExtendedPage = EnableExtPage
                Return True
            Else
                Return False
            End If
        End Function

        Private Function AT45_ReadData(flash_offset As UInt32, data_count As Integer) As Byte()
            Dim page_size As UInt16 = CUShort(IIf(Me.ExtendedPage, MyFlashDevice.PAGE_SIZE_EXTENDED, MyFlashDevice.PAGE_SIZE))
            Dim data_out(data_count - 1) As Byte
            Dim AddrOffset As Integer = CInt(Math.Ceiling(Math.Log(page_size, 2))) 'Number of bits the address is offset
            Dim PageAddr As UInt32 = CUShort(flash_offset \ page_size)
            Dim PageOffset As UInt32 = flash_offset - (PageAddr * page_size)
            Dim addr_bytes() As Byte = Utilities.Bytes.FromUInt24((PageAddr << AddrOffset) + PageOffset)
            Dim at45_addr As UInt32 = (PageAddr << AddrOffset) + PageOffset
            Dim dummy_clocks As Integer = (4 * 8) '(4 extra bytes)
            Dim setup_class As New ReadSetupPacket(MyFlashDevice.OP_COMMANDS.READ, at45_addr, data_out.Length, MyFlashDevice.AddressBytes)
            setup_class.DUMMY = dummy_clocks
            Dim result As Boolean = FCUSB.USB_SETUP_BULKIN(USBREQ.SPI_READFLASH, setup_class.ToBytes, data_out, 0)
            If Not result Then Return Nothing
            Return data_out
        End Function

        Private Function AT45_EraseSector(flash_offset As UInt32) As Boolean
            Dim page_size As UInt16 = CUShort(IIf(Me.ExtendedPage, MyFlashDevice.PAGE_SIZE_EXTENDED, MyFlashDevice.PAGE_SIZE))
            Dim EraseSize As Integer = Me.SectorSize(0)
            Dim AddrOffset As Integer = CInt(Math.Ceiling(Math.Log(page_size, 2))) 'Number of bits the address is offset
            Dim blocknum As Integer = CInt(flash_offset \ EraseSize)
            Dim addrbytes() As Byte = Utilities.Bytes.FromUInt24(CUInt(blocknum << CInt(AddrOffset + 3)))
            SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.SE, addrbytes(0), addrbytes(1), addrbytes(2)}, Nothing)
            Return True
        End Function
        'Uses an internal sram buffer to transfer data from the board to the flash (used by Atmel AT45DBxxx)
        Private Function AT45_WriteData(offset As UInt32, DataOut() As Byte) As Boolean
            Try
                Dim page_size As UInt16 = CUShort(IIf(Me.ExtendedPage, MyFlashDevice.PAGE_SIZE_EXTENDED, MyFlashDevice.PAGE_SIZE))
                Dim AddrOffset As Integer = CInt(Math.Ceiling(Math.Log(page_size, 2))) 'Number of bits the address is offset
                Dim BytesLeft As Integer = DataOut.Length
                Do Until BytesLeft = 0
                    Dim BytesToWrite As Integer = BytesLeft
                    If BytesToWrite > page_size Then BytesToWrite = page_size
                    Dim DataToBuffer(BytesToWrite + 3) As Byte
                    DataToBuffer(0) = MyFlashDevice.OP_COMMANDS.WRTB '0x84
                    Dim src_ind As Integer = DataOut.Length - BytesLeft
                    Array.Copy(DataOut, src_ind, DataToBuffer, 4, BytesToWrite)
                    SPIBUS_SlaveSelect_Enable()
                    Utilities.Sleep(5)   'We need this here for Pro
                    If Not SPIBUS_WriteData(DataToBuffer) Then Return False
                    Utilities.Sleep(5)
                    SPIBUS_SlaveSelect_Disable()
                    WaitUntilReady()
                    Dim PageAddr As UInt32 = (offset \ page_size)
                    Dim PageCmd() As Byte = Utilities.Bytes.FromUInt24(PageAddr << AddrOffset)
                    Dim Cmd2() As Byte = {MyFlashDevice.OP_COMMANDS.WRFB, PageCmd(0), PageCmd(1), PageCmd(2)} '0x88
                    SPIBUS_WriteRead(Cmd2)
                    WaitUntilReady()
                    offset += CUInt(BytesToWrite)
                    BytesLeft -= BytesToWrite
                Loop
                Return True
            Catch ex As Exception
            End Try
            Return False
        End Function

#End Region

#Region "Programming Algorithums"
        'Returns the die address from the flash_offset (and increases by the buffersize) and also selects the correct die
        Private Function GetAddressForMultiDie(ByRef flash_offset As UInt32, ByRef count As Integer, ByRef buffer_size As Integer) As UInt32
            Dim die_size As UInt32 = CUInt(MyFlashDevice.FLASH_SIZE \ MyFlashDevice.STACKED_DIES)
            Dim die_id As Byte = CByte(Math.Floor(flash_offset / die_size))
            Dim die_addr As UInt32 = (flash_offset Mod die_size)
            If (MyFlashDevice.MFG_CODE = &H20) Then 'Micron uses a different die system
                buffer_size = CInt(Math.Min(count, ((MyFlashDevice.FLASH_SIZE \ MyFlashDevice.STACKED_DIES) - die_addr)))
                count -= buffer_size
                die_addr = flash_offset
                flash_offset += CUInt(buffer_size)
            Else
                buffer_size = CInt(Math.Min(count, ((MyFlashDevice.FLASH_SIZE \ MyFlashDevice.STACKED_DIES) - die_addr)))
                If (die_id <> Me.DIE_SELECTED) Then
                    SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.DIESEL, die_id})
                    WaitUntilReady()
                    Me.DIE_SELECTED = die_id
                End If
                count -= buffer_size
                flash_offset += CUInt(buffer_size)
            End If
            Return die_addr
        End Function

        Private Function WriteData_Flash(setup_packet As WriteSetupPacket, data_out() As Byte) As Boolean
            Dim result As Boolean
            Dim setup_data() As Byte = setup_packet.ToBytes()
            result = FCUSB.USB_SETUP_BULKOUT(USBREQ.SPI_WRITEFLASH, setup_data, data_out, 0)
            Utilities.Sleep(6) 'Needed
            Return result
        End Function
        'Designed for SPI EEPROMS where each page needs to wait until ready
        Private Function WriteData_SPI_EEPROM(offset As UInt32, data_to_write() As Byte) As Boolean
            Dim PageSize As UInt16 = MyFlashDevice.PAGE_SIZE
            Dim DataToWrite As Integer = data_to_write.Length
            For i As Integer = 0 To CInt(Math.Ceiling(DataToWrite / PageSize)) - 1
                Dim BufferSize As Integer = DataToWrite
                If (BufferSize > PageSize) Then BufferSize = PageSize
                Dim data(BufferSize - 1) As Byte
                Array.Copy(data_to_write, (i * PageSize), data, 0, data.Length)
                Dim addr_size As Integer = (CInt(MyFlashDevice.ADDRESSBITS) \ 8)
                Dim packet(data.Length + addr_size) As Byte 'OPCMD,ADDR,DATA
                packet(0) = MyFlashDevice.OP_COMMANDS.PROG 'First byte is the write command
                If (addr_size = 1) Then
                    packet(0) = packet(0) Or CByte(offset >> 5) 'Sets bits 3/4 with A8/A9
                    packet(1) = CByte(offset And 255)
                    Array.Copy(data, 0, packet, 2, data.Length)
                ElseIf (addr_size = 2) Then
                    packet(1) = CByte((offset >> 8) And 255)
                    packet(2) = CByte(offset And 255)
                    Array.Copy(data, 0, packet, 3, data.Length)
                ElseIf (addr_size = 3) Then
                    packet(1) = CByte((offset >> 16) And 255)
                    packet(2) = CByte((offset >> 8) And 255)
                    packet(3) = CByte(offset And 255)
                    Array.Copy(data, 0, packet, 4, data.Length)
                End If
                SPIBUS_WriteEnable()
                SPIBUS_SlaveSelect_Enable()
                SPIBUS_WriteData(packet)
                SPIBUS_SlaveSelect_Disable()
                Utilities.Sleep(10)
                WaitUntilReady()
                offset += CUInt(data.Length)
                DataToWrite -= data.Length
            Next
            Return True
        End Function

        Private Function WriteData_AAI(flash_offset As UInteger, data() As Byte, word_mode As Boolean) As Boolean
            If word_mode AndAlso (Not data.Length Mod 2 = 0) Then 'We must write a even number of bytes
                ReDim Preserve data(data.Length)
                data(data.Length - 1) = 255 'Fill the last byte with 0xFF
            End If
            SPIBUS_WriteEnable()
            MyFlashDevice.PAGE_SIZE = 1024 'No page size needed when doing AAI
            Dim setup_packet As New WriteSetupPacket(MyFlashDevice, flash_offset, data.Length)
            If word_mode Then
                setup_packet.CMD_PROG = MyFlashDevice.OP_COMMANDS.AAI_WORD
            Else
                setup_packet.CMD_PROG = MyFlashDevice.OP_COMMANDS.AAI_BYTE
            End If
            Dim ctrl As UInt32 = CUInt(Utilities.BoolToInt(word_mode) + 1)
            Dim Result As Boolean = FCUSB.USB_SETUP_BULKOUT(USBREQ.SPI_WRITEDATA_AAI, setup_packet.ToBytes, data, ctrl)
            If Not Result Then Return False
            Utilities.Sleep(6) 'Needed for some reason
            FCUSB.USB_WaitForComplete()
            SPIBUS_WriteDisable()
            Return True
        End Function

#End Region

#Region "SPIBUS"

        Public Sub SPIBUS_Setup(bus_speed As SPI_SPEED, spi_mode As SPI_CLOCK_POLARITY)
            Dim clock_speed As UInt32 = GetMaxSpiClock(FCUSB.HWBOARD, bus_speed)
            Me.FCUSB.USB_SPI_INIT(CUInt(spi_mode), clock_speed)
            Utilities.Sleep(50) 'Allow time for device to change IO
        End Sub

        Public Function SPIBUS_WriteEnable() As Boolean
            If SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.WREN}, Nothing) = 1 Then
                Return True
            Else
                Return False
            End If
        End Function

        Public Function SPIBUS_WriteDisable() As Boolean
            If SPIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.WRDI}, Nothing) = 1 Then
                Return True
            Else
                Return False
            End If
        End Function

        Public Function SPIBUS_SendCommand(spi_cmd As Byte) As Boolean
            Dim we As Boolean = SPIBUS_WriteEnable()
            If Not we Then Return False
            If SPIBUS_WriteRead({spi_cmd}, Nothing) = 1 Then Return True
            Return False
        End Function

        Public Function SPIBUS_WriteRead(WriteBuffer() As Byte, Optional ByRef ReadBuffer() As Byte = Nothing) As Integer
            Dim Result As Boolean = True
            If WriteBuffer Is Nothing And ReadBuffer Is Nothing Then Return 0
            Dim TotalBytesTransfered As Integer = 0
            SPIBUS_SlaveSelect_Enable()
            If (WriteBuffer IsNot Nothing) Then
                Result = SPIBUS_WriteData(WriteBuffer)
                If Result Then TotalBytesTransfered += WriteBuffer.Length
            End If
            If (ReadBuffer IsNot Nothing) AndAlso Result Then
                Result = SPIBUS_ReadData(ReadBuffer)
                If Result Then TotalBytesTransfered += ReadBuffer.Length
            End If
            SPIBUS_SlaveSelect_Disable()
            If Not Result Then Return -1
            Return TotalBytesTransfered
        End Function
        'Makes the CS/SS pin go low
        Private Sub SPIBUS_SlaveSelect_Enable()
            Try
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.SPI_SS_ENABLE)
                Utilities.Sleep(2)
            Catch ex As Exception
            End Try
        End Sub
        'Releases the CS/SS pin
        Private Sub SPIBUS_SlaveSelect_Disable()
            Try
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.SPI_SS_DISABLE)
                Utilities.Sleep(2)
            Catch ex As Exception
            End Try
        End Sub

        Private Function SPIBUS_WriteData(DataOut() As Byte) As Boolean
            Dim Success As Boolean
            Try
                Success = FCUSB.USB_SETUP_BULKOUT(USBREQ.SPI_WR_DATA, Nothing, DataOut, CUInt(DataOut.Length))
            Catch ex As Exception
                Return False
            End Try
            If Not Success Then RaiseEvent PrintConsole(RM.GetString("spi_error_writing"))
            Return Success
        End Function

        Private Function SPIBUS_ReadData(ByRef Data_In() As Byte) As Boolean
            Dim Success As Boolean = False
            Try
                Success = FCUSB.USB_SETUP_BULKIN(USBREQ.SPI_RD_DATA, Nothing, Data_In, CUInt(Data_In.Length))
            Catch ex As Exception
            End Try
            If Not Success Then RaiseEvent PrintConsole(RM.GetString("spi_error_reading"))
            Return Success
        End Function

        Public Sub SetProgPin(enabled As Boolean)
            Try
                Dim value As UInt32 = 0 'Set to LOW
                If enabled Then value = 1 'Set to HIGH
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.SPI_PROG, Nothing, value)
            Catch ex As Exception
            End Try
        End Sub

#End Region

        Public Function GetUsbDevice() As FCUSB_DEVICE Implements MemoryDeviceUSB.GetUsbDevice
            Return Me.FCUSB
        End Function

    End Class

    Public Enum SPI_ORDER As Integer
        SPI_ORDER_MSB_FIRST = 0
        SPI_ORDER_LSB_FIRST = 1
    End Enum

    Public Enum SPI_CLOCK_POLARITY As Integer
        SPI_MODE_0 = 0 'CPOL(0),CPHA(0),CKE(1)
        SPI_MODE_1 = 1 'CPOL(0),CPHA(1),CKE(0)
        SPI_MODE_2 = 2 'CPOL(1),CPHA(0),CKE(1)
        SPI_MODE_3 = 3 'CPOL(1),CPHA(1),CKE(0)
    End Enum

    Public Enum SPI_SPEED As UInt32
        MHZ_32 = 32000000
        MHZ_24 = 24000000
        MHZ_16 = 16000000
        MHZ_12 = 12000000
        MHZ_8 = 8000000
        MHZ_4 = 4000000
        MHZ_2 = 2000000
        MHZ_1 = 1000000
    End Enum

    Public Enum SQI_SPEED As UInt32
        MHZ_40 = 40000000
        MHZ_20 = 20000000
        MHZ_10 = 10000000
        MHZ_5 = 5000000
        MHZ_2 = 2000000
        MHZ_1 = 1000000
    End Enum

    Friend Class WriteSetupPacket
        Public Property CMD_PROG As Byte
        Public Property CMD_WREN As Byte
        Public Property CMD_RDSR As Byte
        Public Property CMD_RDFR As Byte
        Public Property CMD_WR As Byte
        Public Property PAGE_SIZE As UInt16
        Public Property SEND_RDFS As Boolean = False
        Property DATA_OFFSET As UInt32
        Property WR_COUNT As Integer
        Property ADDR_BYTES As Byte 'Number of bytes used for the read command 
        Property SPI_MODE As SPI_QUAD_SUPPORT = SPI_QUAD_SUPPORT.NO_QUAD

        Sub New(spi_dev As SPI_NOR, offset As UInt32, d_count As Integer)
            Me.CMD_PROG = spi_dev.OP_COMMANDS.PROG
            Me.CMD_WREN = spi_dev.OP_COMMANDS.WREN
            Me.CMD_RDSR = spi_dev.OP_COMMANDS.RDSR
            Me.CMD_RDFR = spi_dev.OP_COMMANDS.RDFR 'Flag Status Register
            Me.ADDR_BYTES = CByte(spi_dev.AddressBytes)
            Me.DATA_OFFSET = offset
            Me.WR_COUNT = d_count
            Me.SEND_RDFS = spi_dev.SEND_RDFS
            Me.PAGE_SIZE = spi_dev.PAGE_SIZE
        End Sub

        Public Function ToBytes() As Byte()
            Dim setup_data(14) As Byte
            setup_data(0) = Me.CMD_PROG
            setup_data(1) = Me.CMD_WREN
            setup_data(2) = Me.CMD_RDSR
            setup_data(3) = Me.CMD_RDFR
            setup_data(4) = CByte(Me.ADDR_BYTES) 'Number of bytes to write
            setup_data(5) = CByte((Me.PAGE_SIZE And &HFF00) >> 8)
            setup_data(6) = CByte(Me.PAGE_SIZE And &HFF)
            setup_data(7) = CByte((Me.DATA_OFFSET And &HFF000000) >> 24)
            setup_data(8) = CByte((Me.DATA_OFFSET And &HFF0000) >> 16)
            setup_data(9) = CByte((Me.DATA_OFFSET And &HFF00) >> 8)
            setup_data(10) = CByte(Me.DATA_OFFSET And &HFF)
            setup_data(11) = CByte((Me.WR_COUNT And &HFF0000) >> 16)
            setup_data(12) = CByte((Me.WR_COUNT And &HFF00) >> 8)
            setup_data(13) = CByte(Me.WR_COUNT And &HFF)
            setup_data(14) = CByte(Me.SPI_MODE)
            If (Not Me.SEND_RDFS) Then setup_data(3) = 0 'Only use flag-reg if required
            Return setup_data
        End Function

    End Class

    Friend Class ReadSetupPacket
        Property READ_CMD As Byte
        Property DATA_OFFSET As UInt32
        Property COUNT As Integer
        Property ADDR_BYTES As Byte 'Number of bytes used for the read command (MyFlashDevice.AddressBytes)
        Property DUMMY As Integer = 0 'Number of clock toggles before reading data
        Property SPI_MODE As SQI_IO_MODE = SQI_IO_MODE.SPI_ONLY

        Sub New(cmd As Byte, offset As UInt32, d_count As Integer, addr_size As Integer)
            Me.READ_CMD = cmd
            Me.DATA_OFFSET = offset
            Me.COUNT = d_count
            Me.ADDR_BYTES = CByte(addr_size And 255)
        End Sub

        Public Function ToBytes() As Byte()
            Dim setup_data(10) As Byte '12 bytes
            setup_data(0) = READ_CMD 'READ/FAST_READ/ETC.
            setup_data(1) = CByte(Me.ADDR_BYTES)
            setup_data(2) = CByte((Me.DATA_OFFSET And &HFF000000) >> 24)
            setup_data(3) = CByte((Me.DATA_OFFSET And &HFF0000) >> 16)
            setup_data(4) = CByte((Me.DATA_OFFSET And &HFF00) >> 8)
            setup_data(5) = CByte(Me.DATA_OFFSET And &HFF)
            setup_data(6) = CByte((COUNT And &HFF0000) >> 16)
            setup_data(7) = CByte((COUNT And &HFF00) >> 8)
            setup_data(8) = CByte(COUNT And &HFF)
            setup_data(9) = CByte(Me.DUMMY) 'Number of dummy bytes
            setup_data(10) = Me.SPI_MODE
            Return setup_data
        End Function

    End Class

    Friend Class SPI_IDENT
        Public MANU As Byte '(MFG)
        Public RDID As UInt32 '(ID1,ID2,ID3,ID4)
        Public FMY As Byte '(ID5)
        Public REMS As UInt16 '(MFG)(ID1)
        Public RES As Byte '(MFG)

        Sub New()

        End Sub

        Public ReadOnly Property DETECTED As Boolean
            Get
                If Me.MANU = 0 OrElse Me.MANU = &HFF Then Return False
                If Me.RDID = 0 OrElse Me.RDID = &HFFFF Then Return False
                If Me.MANU = CByte(Me.RDID And 255) And Me.MANU = CByte((Me.RDID >> 8) And 255) Then Return False
                Return True
            End Get
        End Property

    End Class

    Public Enum SPIBUS_MODE As Byte
        SPI_MODE_0 = 1
        SPI_MODE_1 = 2
        SPI_MODE_2 = 3
        SPI_MODE_3 = 4
        SPI_UNSPECIFIED
    End Enum

End Namespace
