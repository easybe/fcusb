'COPYRIGHT EMBEDDED COMPUTERS LLC 2018 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'ACKNOWLEDGEMENT: USB driver functionality provided by LibUsbDotNet (sourceforge.net/projects/libusbdotnet) 

Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB
Imports FlashcatUSB.USB.HostClient

Namespace SPI
    'This class is used for devices with DUAL and QUAD I/O modes
    Public Class SQI_Programmer : Implements MemoryDeviceUSB
        Private FCUSB As FCUSB_DEVICE
        Public Event PrintConsole(ByVal msg As String) Implements MemoryDeviceUSB.PrintConsole
        Public Event SetProgress(ByVal percent As Integer) Implements MemoryDeviceUSB.SetProgress
        Public Property MyFlashDevice As SPI_NOR 'Contains the definition of the Flash device that is connected
        Public Property MyFlashStatus As DeviceStatus = DeviceStatus.NotDetected
        Public Property DIE_SELECTED As Integer = 0
        Private Property SQI_IO_MODE As MULTI_IO_MODE 'IO=1/2/4 bits per clock cycle
        Private Property SQI_DEVICE_MODE As SQI_IO_MODE '0=SPI_ONLY,1=QUAD_ONLY,2=DUAL_ONLY,3=SPI_QUAD,4=SPI_DUAL

        Sub New(ByVal parent_if As FCUSB_DEVICE)
            FCUSB = parent_if
        End Sub

        Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
            MyFlashStatus = DeviceStatus.NotDetected
            FCUSB.USB_VCC_OFF()
            SQIBUS_Setup()
            If FCUSB.HWBOARD = FCUSB_BOARD.Professional OrElse FCUSB.HWBOARD = FCUSB_BOARD.Mach1 Then
                If MySettings.VOLT_SELECT = Voltage.V1_8 Then
                    FCUSB.USB_VCC_1V8()
                Else
                    FCUSB.USB_VCC_3V()
                End If
            End If
            Utilities.Sleep(200)
            Dim FLASH_IDENT As SPI_IDENT = ReadDeviceID(MULTI_IO_MODE.Quad)
            If Not FLASH_IDENT.DETECTED Then FLASH_IDENT = ReadDeviceID(MULTI_IO_MODE.Dual)
            If Not FLASH_IDENT.DETECTED Then FLASH_IDENT = ReadDeviceID(MULTI_IO_MODE.Single)
            If FLASH_IDENT.DETECTED Then
                RaiseEvent PrintConsole(RM.GetString("spi_successfully_opened_sqi"))
                Dim RDID_Str As String = "0x" & Hex(FLASH_IDENT.MANU).PadLeft(2, "0") & Hex(FLASH_IDENT.RDID).PadLeft(4, "0")
                RaiseEvent PrintConsole(String.Format(RM.GetString("spi_connected_to_flash_sqi"), RDID_Str))
                Dim ID1 As UInt16 = (FLASH_IDENT.RDID And &HFFFF)
                Dim ID2 As UInt16 = (FLASH_IDENT.RDID >> 16)
                MyFlashDevice = FlashDatabase.FindDevice(FLASH_IDENT.MANU, ID1, ID2, MemoryType.SERIAL_NOR)
                If MyFlashDevice IsNot Nothing Then
                    MyFlashStatus = DeviceStatus.Supported
                    RaiseEvent PrintConsole(String.Format(RM.GetString("flash_detected"), Me.DeviceName, Format(Me.DeviceSize, "#,###")))
                    RaiseEvent PrintConsole(RM.GetString("spi_mode_sqi"))
                    If MyFlashDevice.SQI_MODE = SPI_QUAD_SUPPORT.QUAD Then
                        If SQI_IO_MODE = MULTI_IO_MODE.Quad Then
                            RaiseEvent PrintConsole("Detected Flash in QUAD-SPI mode (4-bit)")
                            Me.SQI_DEVICE_MODE = SPI.SQI_IO_MODE.QUAD_ONLY
                        ElseIf SQI_IO_MODE = MULTI_IO_MODE.Dual Then
                            RaiseEvent PrintConsole("Detected Flash in DUAL-SPI mode (2-bit)")
                            Me.SQI_DEVICE_MODE = SPI.SQI_IO_MODE.DUAL_ONLY
                        ElseIf SQI_IO_MODE = MULTI_IO_MODE.Single Then
                            RaiseEvent PrintConsole("Detected Flash in SPI mode (1-bit)")
                            Me.SQI_DEVICE_MODE = SPI.SQI_IO_MODE.SPI_ONLY
                        End If
                    ElseIf MyFlashDevice.SQI_MODE = SPI_QUAD_SUPPORT.SPI_QUAD Then
                        RaiseEvent PrintConsole("Detected Flash in SPI mode (1-bit)")
                        SQIBUS_WriteRead({&HF0}) 'SPI RESET COMMAND
                        Utilities.Sleep(20)
                        If (FLASH_IDENT.MANU = &HEF) Then 'Winbond
                            RaiseEvent PrintConsole("Entering QPI mode for Winbond device")
                            SQIBUS_WriteRead({&H50}) 'WREN VOLATILE
                            SQIBUS_WriteRead({&H1, 0, 2}) 'WRSR(0,2) - Sets QE bit
                            Dim sr(0) As Byte
                            SQIBUS_WriteRead({&H35}, sr) 'Read SR-2
                            If ((sr(0) And 2) >> 1) Then
                                RaiseEvent PrintConsole("QE bit set in Status Register-2")
                                SQIBUS_WriteRead({&H38})
                                Utilities.Sleep(20)
                                Me.SQI_DEVICE_MODE = SPI.SQI_IO_MODE.SPI_QUAD
                                RaiseEvent PrintConsole("IO mode switched to SPI/QUAD (1-bit/4-bit)")
                            Else
                                RaiseEvent PrintConsole("Error: failed to set the QE bit")
                            End If
                        ElseIf FLASH_IDENT.MANU = 1 Then 'Cypress/Spansion
                            Dim sr2(0) As Byte
                            SQIBUS_WriteRead({&H35}, sr2)
                            If ((sr2(0) >> 1) And 1) Then
                                RaiseEvent PrintConsole("IO mode switched to SPI/QUAD (1-bit/4-bit)")
                                Me.SQI_DEVICE_MODE = SPI.SQI_IO_MODE.SPI_QUAD
                            Else
                                RaiseEvent PrintConsole("QUAD mode not enabled, using SPI (1-bit)")
                                Me.SQI_DEVICE_MODE = SPI.SQI_IO_MODE.SPI_ONLY
                            End If
                        End If
                    Else
                        RaiseEvent PrintConsole("Detected Flash in SPI mode (1-bit)")
                    End If
                    SQIBUS_SendCommand(&HF0) 'SPI RESET COMMAND
                    If MyFlashDevice.SEND_EN4B Then SQIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.EN4B) '0xB7
                    SQIBUS_SendCommand(MyFlashDevice.OP_COMMANDS.ULBPR) '0x98 (global block unprotect)
                    LoadVendorSpecificConfigurations() 'Some devices may need additional configurations
                    Return True
                Else
                    RaiseEvent PrintConsole(RM.GetString("unknown_device_email"))
                    MyFlashDevice = New SPI_NOR("Unknown", VCC_IF.SPI_3V, 0, FLASH_IDENT.MANU, FLASH_IDENT.RDID)
                    MyFlashStatus = DeviceStatus.NotSupported
                    Return False
                End If
            Else
                MyFlashStatus = DeviceStatus.NotDetected
                RaiseEvent PrintConsole(RM.GetString("spi_flash_not_detected"))
                Return False
            End If
        End Function

        Private Function ReadDeviceID(mode As MULTI_IO_MODE) As SPI_IDENT
            Dim id As New SPI_IDENT
            id.MANU = 0
            id.RDID = 0
            Dim out_buffer(3) As Byte
            Dim id_code As Byte = 0
            If mode = MULTI_IO_MODE.Single Then
                id_code = &H9F
            ElseIf mode = MULTI_IO_MODE.Dual Then
                id_code = &HAF
            ElseIf mode = MULTI_IO_MODE.Quad Then
                id_code = &HAF
            End If
            Me.SQI_IO_MODE = mode
            If SQIBUS_WriteRead({id_code}, out_buffer) = 5 Then 'MULTIPLE I/O READ ID
                id.MANU = out_buffer(0)
                id.RDID = Utilities.Bytes.ToUInt32({0, 0, out_buffer(1), out_buffer(2)})
            End If
            Return id
        End Function

        Private Sub LoadVendorSpecificConfigurations()
            If (MyFlashDevice.MFG_CODE = &HBF) Then 'SST26VF016/SST26VF032 requires block protection to be removed in SQI only
                If MyFlashDevice.ID1 = &H2601 Then 'SST26VF016
                    SQIBUS_WriteEnable()
                    SQIBUS_WriteRead({&H42, 0, 0, 0, 0, 0, 0}) '6 blank bytes
                    Utilities.Sleep(200)
                ElseIf MyFlashDevice.ID1 = &H2602 Then 'SST26VF032
                    SQIBUS_WriteEnable()
                    SQIBUS_WriteRead({&H42, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}) '10 blank bytes
                    Utilities.Sleep(200)
                End If
            ElseIf (MyFlashDevice.MFG_CODE = &H9D) Then 'ISSI
                WriteStatusRegister({0}) 'Erase protection bits
            End If
            If (MyFlashDevice.MFG_CODE = &HEF) AndAlso (MyFlashDevice.ID1 = &H4018) Then
                SQIBUS_WriteRead({&HC2, 1}) : WaitUntilReady() 'Check to see if this device has two dies
            End If
        End Sub

        Private Function EnableWinbondSQIMode() As Boolean
            Try
                SQIBUS_WriteEnable()
                SQIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.WRSR, 0, 2}, Nothing) '0x01 00 02
                Dim status_reg(0) As Byte
                WaitUntilReady()
                SQIBUS_WriteRead({&H35}, status_reg) '0x5
                If status_reg(0) And 2 = 2 Then Return True 'QE bit is set
            Catch ex As Exception
            End Try
            Return False 'Quad mode is not enabled or supported
        End Function

        Private Function DisableWinbondSQIMode() As Boolean
            Try
                SQIBUS_WriteEnable()
                SQIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.WRSR, 0, 0}, Nothing) '0x01 00 02
                Dim status_reg(0) As Byte
                WaitUntilReady()
                SQIBUS_WriteRead({&H35}, status_reg)
                If status_reg(0) And 2 = 0 Then Return True 'QE bit is unset
            Catch ex As Exception
            End Try
            Return False
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
                If Not MyFlashStatus = USB.DeviceStatus.Supported Then Return 0
                Return MyFlashDevice.FLASH_SIZE
            End Get
        End Property

        Friend ReadOnly Property SectorSize(ByVal sector As UInt32, Optional ByVal memory_area As FlashArea = FlashArea.Main) As UInt32 Implements MemoryDeviceUSB.SectorSize
            Get
                If Not MyFlashStatus = USB.DeviceStatus.Supported Then Return 0
                If MyFlashDevice.ERASE_REQUIRED Then
                    Return MyFlashDevice.ERASE_SIZE
                Else
                    Return MyFlashDevice.FLASH_SIZE
                End If
            End Get
        End Property

        Friend Function SectorFind(ByVal sector_index As UInt32, Optional ByVal memory_area As FlashArea = FlashArea.Main) As Long Implements MemoryDeviceUSB.SectorFind
            If sector_index = 0 Then Return 0 'Addresses start at the base address 
            Return Me.SectorSize(0, memory_area) * sector_index
        End Function

        Friend Function SectorCount() As UInt32 Implements MemoryDeviceUSB.SectorCount
            If MyFlashStatus = USB.DeviceStatus.Supported Then
                Dim EraseSize As UInt32 = MyFlashDevice.ERASE_SIZE
                If EraseSize = 0 Then Return 1
                Dim FlashSize As UInt32 = Me.DeviceSize()
                If FlashSize < EraseSize Then Return 1
                Return CInt(FlashSize / EraseSize)
            End If
            Return 0
        End Function

        Friend Function ReadData(flash_offset As Long, data_count As UInt32, Optional ByVal memory_area As FlashArea = FlashArea.Main) As Byte() Implements MemoryDeviceUSB.ReadData
            Dim data_to_read(data_count - 1) As Byte
            Dim READ_CMD As Byte
            Dim DUMMY As Byte = 0 'Number of dummy clock cycles
            If Me.SQI_DEVICE_MODE = SPI.SQI_IO_MODE.SPI_ONLY Then 'DUAL/QUAD require dummy bits and different read command
                READ_CMD = MyFlashDevice.OP_COMMANDS.READ
            ElseIf Me.SQI_DEVICE_MODE = SPI.SQI_IO_MODE.DUAL_ONLY Then
                READ_CMD = MyFlashDevice.OP_COMMANDS.DUAL_READ
                DUMMY = MyFlashDevice.SQI_DUMMY
            ElseIf Me.SQI_DEVICE_MODE = SPI.SQI_IO_MODE.SPI_QUAD Then
                READ_CMD = MyFlashDevice.OP_COMMANDS.QUAD_READ
                DUMMY = MyFlashDevice.SPI_DUMMY 'This needs to be SPI, since dummy bits are shifted before IO is QUAD
            Else 'We are in quad mode
                READ_CMD = MyFlashDevice.OP_COMMANDS.QUAD_READ
                DUMMY = MyFlashDevice.SQI_DUMMY
            End If
            If FCUSB.HWBOARD = FCUSB_BOARD.Professional OrElse FCUSB.HWBOARD = FCUSB_BOARD.Mach1 Then
                Dim setup_class As New ReadSetupPacket(READ_CMD, flash_offset, data_to_read.Length, MyFlashDevice.AddressBytes)
                setup_class.SPI_MODE = Me.SQI_DEVICE_MODE
                setup_class.DUMMY = DUMMY
                Dim result As Boolean = FCUSB.USB_SETUP_BULKIN(USBREQ.SQI_RD_FLASH, setup_class.ToBytes, data_to_read, 0)
                If Not result Then Return Nothing
            Else
                SQIBUS_SlaveSelect_Enable()
                Dim rd_cmd() As Byte = SQI_GetSetup(READ_CMD, CUInt(flash_offset))
                If Me.SQI_DEVICE_MODE = SPI.SQI_IO_MODE.QUAD_ONLY Then
                    SQIBUS_WriteData(rd_cmd, MULTI_IO_MODE.Quad)
                Else
                    SQIBUS_WriteData(rd_cmd, MULTI_IO_MODE.Single)
                End If
                If DUMMY Then
                    Dim dummmy_data((DUMMY / 8) - 1) As Byte
                    SQIBUS_ReadData(dummmy_data, MULTI_IO_MODE.Single)
                End If
                If Me.SQI_DEVICE_MODE = SPI.SQI_IO_MODE.QUAD_ONLY OrElse Me.SQI_DEVICE_MODE = SPI.SQI_IO_MODE.SPI_QUAD Then
                    SQIBUS_ReadData(data_to_read, MULTI_IO_MODE.Quad)
                Else
                    SQIBUS_ReadData(data_to_read, MULTI_IO_MODE.Single)
                End If
                SQIBUS_SlaveSelect_Disable()
            End If
            Return data_to_read
        End Function

        Friend Function WriteData(flash_offset As Long, data_out() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
            Dim DataToWrite As UInt32 = data_out.Length
            Dim PacketSize As UInt32 = 8192
            Dim Loops As Integer = CInt(Math.Ceiling(DataToWrite / PacketSize)) 'Calcuates iterations
            Dim PROG_CMD As Byte
            If Me.SQI_DEVICE_MODE = SPI.SQI_IO_MODE.SPI_ONLY Then
                PROG_CMD = MyFlashDevice.OP_COMMANDS.PROG
            ElseIf Me.SQI_DEVICE_MODE = SPI.SQI_IO_MODE.DUAL_ONLY Then
                PROG_CMD = MyFlashDevice.OP_COMMANDS.DUAL_PROG
            ElseIf Me.SQI_DEVICE_MODE = SPI.SQI_IO_MODE.SPI_QUAD Then
                PROG_CMD = MyFlashDevice.OP_COMMANDS.QUAD_PROG
            Else 'We are in quad mode
                PROG_CMD = MyFlashDevice.OP_COMMANDS.QUAD_PROG
            End If
            For i As Integer = 0 To Loops - 1
                Dim BufferSize As Integer = DataToWrite
                If (BufferSize > PacketSize) Then BufferSize = PacketSize
                Dim sector_data(BufferSize - 1) As Byte
                Array.Copy(data_out, (i * PacketSize), sector_data, 0, sector_data.Length)
                Dim setup_class As New WriteSetupPacket(MyFlashDevice, flash_offset, BufferSize)
                setup_class.SPI_MODE = Me.SQI_DEVICE_MODE
                setup_class.CMD_PROG = PROG_CMD
                Dim result As Boolean = FCUSB.USB_SETUP_BULKOUT(USBREQ.SQI_WR_FLASH, setup_class.ToBytes, sector_data, 0)
                If Not result Then Return False
                Utilities.Sleep(10)
                FCUSB.USB_WaitForComplete()
                flash_offset += sector_data.Length
                DataToWrite -= sector_data.Length
            Next
            Return True
        End Function

        Private Function SQI_GetSetup(cmd As Byte, flash_offset As UInt32) As Byte()
            Dim payload(MyFlashDevice.AddressBytes) As Byte
            payload(0) = cmd
            If MyFlashDevice.AddressBytes = 4 Then
                payload(1) = CByte((flash_offset And &HFF000000) >> 24)
                payload(2) = CByte((flash_offset And &HFF0000) >> 16)
                payload(3) = CByte((flash_offset And &HFF00) >> 8)
                payload(4) = CByte(flash_offset And &HFF)
            ElseIf MyFlashDevice.AddressBytes = 3 Then
                payload(1) = CByte((flash_offset And &HFF0000) >> 16)
                payload(2) = CByte((flash_offset And &HFF00) >> 8)
                payload(3) = CByte(flash_offset And &HFF)
            ElseIf MyFlashDevice.AddressBytes = 2 Then
                payload(1) = CByte((flash_offset And &HFF00) >> 8)
                payload(2) = CByte(flash_offset And &HFF)
            End If
            Return payload
        End Function

        Friend Function SectorErase(ByVal sector_index As UInt32, Optional ByVal memory_area As FlashArea = FlashArea.Main) As Boolean Implements MemoryDeviceUSB.SectorErase
            If (Not MyFlashDevice.ERASE_REQUIRED) Then Return True 'Erase not needed
            Dim flash_offset As UInt32 = Me.SectorFind(sector_index, memory_area)
            SQIBUS_WriteEnable()
            Dim DataToWrite() As Byte = GetArrayWithCmdAndAddr(MyFlashDevice.OP_COMMANDS.SE, flash_offset) '0xD8
            SQIBUS_WriteRead(DataToWrite, Nothing)
            WaitUntilReady()
            Return True
        End Function

        Friend Function SectorWrite(ByVal sector_index As UInt32, ByVal data() As Byte, Optional ByRef Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.SectorWrite
            Dim Addr32 As UInteger = Me.SectorFind(sector_index, Params.Memory_Area)
            Return WriteData(Addr32, data, Params)
        End Function

        Friend Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
            If MyFlashDevice.ProgramMode = FlashMemory.SPI_ProgramMode.Atmel45Series Then
                SQIBUS_WriteRead({&HC7, &H94, &H80, &H9A}, Nothing)
            ElseIf MyFlashDevice.ProgramMode = SPI_ProgramMode.SPI_EEPROM Then
            ElseIf MyFlashDevice.ProgramMode = SPI_ProgramMode.Nordic Then
            Else
                RaiseEvent PrintConsole(String.Format(RM.GetString("spi_erasing_flash_device"), Format(Me.DeviceSize, "#,###")))
                Dim erase_timer As New Stopwatch : erase_timer.Start()
                Select Case MyFlashDevice.CHIP_ERASE
                    Case EraseMethod.Standard
                        SQIBUS_WriteEnable()
                        SQIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.BE}, Nothing) '&HC7
                        If MyFlashDevice.SEND_RDFS Then ReadFlagStatusRegister()
                        WaitUntilReady()
                    Case EraseMethod.BySector
                        Dim SectorCount As UInt32 = MyFlashDevice.Sector_Count
                        RaiseEvent SetProgress(0)
                        For i As UInt32 = 0 To SectorCount - 1
                            If (Not SectorErase(i, FlashArea.NotSpecified)) Then
                                RaiseEvent SetProgress(0) : Return False 'Error erasing sector
                            Else
                                Dim progress As Single = CSng((i / SectorCount) * 100)
                                RaiseEvent SetProgress(Math.Floor(progress))
                            End If
                        Next
                        RaiseEvent SetProgress(0) 'Device successfully erased
                    Case EraseMethod.DieErase
                        EraseDie()
                    Case EraseMethod.Micron
                        Dim internal_timer As New Stopwatch
                        internal_timer.Start()
                        SQIBUS_WriteEnable() 'Try Chip Erase first
                        SQIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.BE}, Nothing)
                        If MyFlashDevice.SEND_RDFS Then ReadFlagStatusRegister()
                        WaitUntilReady()
                        internal_timer.Stop()
                        If (internal_timer.ElapsedMilliseconds < 1000) Then 'Command not supported, use DIE ERASE instead
                            EraseDie()
                        End If
                End Select
                RaiseEvent PrintConsole(String.Format(RM.GetString("spi_erase_complete"), Format(erase_timer.ElapsedMilliseconds / 1000, "#.##")))
            End If
            Return True
        End Function
        'Reads the SPI status register and waits for the device to complete its current operation
        Friend Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
            Try
                Dim IO As MULTI_IO_MODE = MULTI_IO_MODE.Single
                Select Case SQI_DEVICE_MODE
                    Case SPI.SQI_IO_MODE.QUAD_ONLY
                        IO = MULTI_IO_MODE.Quad
                    Case SPI.SQI_IO_MODE.DUAL_ONLY
                        IO = MULTI_IO_MODE.Dual
                End Select
                Dim sr(0) As Byte
                If MyFlashDevice.SEND_RDFS Then
                    SQIBUS_SlaveSelect_Enable()
                    SQIBUS_WriteData({MyFlashDevice.OP_COMMANDS.RDFR}, IO)
                    Do
                        SQIBUS_ReadData(sr, IO)
                    Loop While (((sr(0) >> 7) And 1) = 0)
                    SQIBUS_SlaveSelect_Disable()
                End If
                SQIBUS_SlaveSelect_Enable()
                SQIBUS_WriteData({MyFlashDevice.OP_COMMANDS.RDSR}, IO)
                Do
                    SQIBUS_ReadData(sr, IO)
                Loop While ((sr(0) And 1) = 1)
                SQIBUS_SlaveSelect_Disable()
            Catch ex As Exception
            End Try
        End Sub

#End Region

#Region "SPIBUS"

        Public Sub SQIBUS_Setup()
            If FCUSB.HWBOARD = FCUSB_BOARD.Professional Then
                If MySettings.SPI_QUAD_SPEED = SQI_SPEED.MHZ_20 Then
                    GUI.PrintConsole(String.Format("SQI clock set to: {0}", "20 MHz"))
                    FCUSB.USB_CONTROL_MSG_OUT(USBREQ.SQI_SETUP, Nothing, 0)
                Else
                    GUI.PrintConsole(String.Format("SQI clock set to: {0}", "10 MHz"))
                    FCUSB.USB_CONTROL_MSG_OUT(USBREQ.SQI_SETUP, Nothing, 1)
                End If
            ElseIf FCUSB.HWBOARD = FCUSB_BOARD.Mach1 Then
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.SQI_SETUP, Nothing, MySettings.SPI_QUAD_SPEED)
                If MySettings.SPI_QUAD_SPEED = SQI_SPEED.MHZ_20 Then
                    GUI.PrintConsole(String.Format("SQI clock set to: {0}", "20 MHz"))
                ElseIf MySettings.SPI_QUAD_SPEED = SQI_SPEED.MHZ_10 Then
                    GUI.PrintConsole(String.Format("SQI clock set to: {0}", "10 MHz"))
                ElseIf MySettings.SPI_QUAD_SPEED = SQI_SPEED.MHZ_5 Then
                    GUI.PrintConsole(String.Format("SQI clock set to: {0}", "5 MHz"))
                ElseIf MySettings.SPI_QUAD_SPEED = SQI_SPEED.MHZ_2 Then
                    GUI.PrintConsole(String.Format("SQI clock set to: {0}", "2 MHz"))
                End If
            Else
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.SQI_SETUP, Nothing, 0)
                GUI.PrintConsole(String.Format("SQI clock set to: {0}", "1 MHz"))
            End If
            Utilities.Sleep(50) 'Allow time for device to change IO
        End Sub

        Public Function SQIBUS_WriteEnable() As Boolean
            If SQIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.WREN}, Nothing) = 1 Then
                Return True
            Else
                Return False
            End If
        End Function

        Public Function SQIBUS_SendCommand(ByVal spi_cmd As Byte) As Boolean
            Dim we As Boolean = SQIBUS_WriteEnable()
            If Not we Then Return False
            Return SQIBUS_WriteRead({spi_cmd}, Nothing)
        End Function

        Public Function SQIBUS_WriteRead(ByVal WriteBuffer() As Byte, Optional ByRef ReadBuffer() As Byte = Nothing) As UInt32
            If WriteBuffer Is Nothing And ReadBuffer Is Nothing Then Return 0
            Dim TotalBytesTransfered As UInt32 = 0
            SQIBUS_SlaveSelect_Enable()
            If (WriteBuffer IsNot Nothing) Then
                Dim BytesWritten As Integer = 0
                Dim Result As Boolean = SQIBUS_WriteData(WriteBuffer, Me.SQI_IO_MODE)
                If WriteBuffer.Length > 2048 Then Utilities.Sleep(2)
                If Result Then TotalBytesTransfered += WriteBuffer.Length
            End If
            If (ReadBuffer IsNot Nothing) Then
                Dim BytesRead As Integer = 0
                Dim Result As Boolean = SQIBUS_ReadData(ReadBuffer, Me.SQI_IO_MODE)
                If Result Then TotalBytesTransfered += ReadBuffer.Length
            End If
            SQIBUS_SlaveSelect_Disable()
            Return TotalBytesTransfered
        End Function
        'Makes the CS/SS pin go low
        Private Sub SQIBUS_SlaveSelect_Enable()
            Try
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.SQI_SS_ENABLE)
                Utilities.Sleep(1)
            Catch ex As Exception
            End Try
        End Sub
        'Releases the CS/SS pin
        Private Sub SQIBUS_SlaveSelect_Disable()
            Try
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.SQI_SS_DISABLE)
                Utilities.Sleep(2)
            Catch ex As Exception
            End Try
        End Sub

        Private Function SQIBUS_WriteData(ByVal DataOut() As Byte, ByVal io_mode As MULTI_IO_MODE) As Boolean
            Dim value_index As UInt32 = (CUInt(io_mode) << 24) Or (DataOut.Length And &HFFFFFF)
            Dim Success As Boolean = FCUSB.USB_SETUP_BULKOUT(USBREQ.SQI_WR_DATA, Nothing, DataOut, value_index)
            Utilities.Sleep(2)
            Return Success
        End Function

        Private Function SQIBUS_ReadData(ByRef Data_In() As Byte, ByVal io_mode As MULTI_IO_MODE) As Boolean
            Dim value_index As UInt32 = (CUInt(io_mode) << 24) Or (Data_In.Length And &HFFFFFF)
            Dim Success As Boolean = FCUSB.USB_SETUP_BULKIN(USBREQ.SQI_RD_DATA, Nothing, Data_In, value_index)
            Return Success
        End Function

#End Region

        Private Function GetArrayWithCmdAndAddr(ByVal cmd As Byte, ByVal addr_offset As UInt32) As Byte()
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
        'This writes to the SR (multi-bytes can be input to write as well)
        Public Function WriteStatusRegister(ByVal NewValues() As Byte) As Boolean
            Try
                If NewValues Is Nothing Then Return False
                SQIBUS_WriteEnable() 'Some devices such as AT25DF641 require the WREN and the status reg cleared before we can write data
                If MyFlashDevice.SEND_EWSR Then
                    SQIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.EWSR}, Nothing) 'Send the command that we are going to enable-write to register
                    Threading.Thread.Sleep(20) 'Wait a brief moment
                End If
                Dim cmd(NewValues.Length) As Byte
                cmd(0) = MyFlashDevice.OP_COMMANDS.WRSR
                Array.Copy(NewValues, 0, cmd, 1, NewValues.Length)
                If Not SQIBUS_WriteRead(cmd, Nothing) = cmd.Length Then Return False
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function ReadStatusRegister(Optional Count As Integer = 1) As Byte()
            Try
                Dim Output(Count - 1) As Byte
                SQIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.RDSR}, Output)
                Return Output
            Catch ex As Exception
                Return Nothing 'Erorr
            End Try
        End Function

        Private Sub ReadFlagStatusRegister()
            Utilities.Sleep(10)
            Dim flag() As Byte = {0}
            Do
                SQIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.RDFR}, flag)
            Loop Until ((flag(0) >> 7) And 1)
        End Sub

        Private Sub EraseDie()
            Dim die_size As UInt32 = &H2000000
            Dim die_count As UInt32 = MyFlashDevice.FLASH_SIZE / die_size
            For x As UInt32 = 1 To die_count
                RaiseEvent PrintConsole(String.Format(RM.GetString("spi_erasing_die"), x.ToString, Format(die_size, "#,###")))
                Dim die_addr() As Byte = Utilities.Bytes.FromUInt32((x - 1) * die_size, False)
                SQIBUS_WriteEnable()
                SQIBUS_WriteRead({MyFlashDevice.OP_COMMANDS.DE, die_addr(0), die_addr(1), die_addr(1), die_addr(1)}, Nothing) '&HC4
                Utilities.Sleep(1000)
                If MyFlashDevice.SEND_RDFS Then ReadFlagStatusRegister()
                WaitUntilReady()
            Next
        End Sub

        Public Sub ResetDevice()
            SQIBUS_WriteRead({&HF0}) 'SPI RESET COMMAND
            'Other commands: 0x66 and 0x99
            Utilities.Sleep(10)
        End Sub

    End Class

    Friend Enum MULTI_IO_MODE As Byte
        [Single] = 1
        Dual = 2
        Quad = 4
    End Enum

    Friend Enum SQI_IO_MODE As Byte
        SPI_ONLY = 0 'SETUP=SPI;DATA=SPI
        QUAD_ONLY = 1 'SETUP=QUAD;DATA=SPI
        DUAL_ONLY = 2 'SET=DUAL;DATA=DUAL
        SPI_QUAD = 3 'SETUP=SPI,DATA=QUAD
        SPI_DUAL = 4 'SETUP=DUAL;DATA=DUAL
    End Enum

End Namespace