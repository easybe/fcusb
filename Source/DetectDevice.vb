Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB
Imports FlashcatUSB.MemoryInterface
Imports FlashcatUSB.SPI

Public Module DetectDevice

    Public Structure DetectParams
        Public OPER_MODE As DeviceMode
        Public SPI_AUTO As Boolean
        Public SPI_CLOCK As SPI_SPEED
        Public SQI_CLOCK As SQI_SPEED
        Public SPI_EEPROM As String
        Public PARALLEL_EEPROM As String
        Public I2C_INDEX As Integer
        Public I2C_SPEED As I2C_SPEED_MODE
        Public I2C_ADDRESS As Byte
        Public NOR_READ_ACCESS As Integer
        Public NOR_WE_PULSE As Integer
        Public NAND_Layout As NandMemLayout
    End Structure

    Public Function Device(usb_dev As FCUSB_DEVICE, Params As DetectParams) As Boolean
        Dim m As MemoryDeviceUSB = CreateProgrammer(usb_dev, Params.OPER_MODE)
        If m Is Nothing Then Return False
        CURRENT_DEVICE_MODE = Params.OPER_MODE
        NAND_LayoutTool = New NAND_LAYOUT_TOOL(Params.NAND_Layout)
        PrintConsole(RM.GetString("detecting_device"), True)
        Utilities.Sleep(100) 'Allow time for USB to power up devices
        If Params.OPER_MODE = DeviceMode.SPI Then
            Return DetectDevice_SPI(m, Params)
        ElseIf Params.OPER_MODE = DeviceMode.SQI Then
            Return DetectDevice_SQI(m, Params)
        ElseIf Params.OPER_MODE = DeviceMode.SPI_NAND Then
            Return DetectDevice_SPI_NAND(m, Params)
        ElseIf Params.OPER_MODE = DeviceMode.SPI_EEPROM Then
            Return DetectDevice_SPI_EEPROM(m, Params)
        ElseIf Params.OPER_MODE = DeviceMode.I2C_EEPROM Then
            Return DetectDevice_I2C_EEPROM(m, Params)
        ElseIf Params.OPER_MODE = DeviceMode.ONE_WIRE Then
            Return DetectDevice_ONE_WIRE(m, Params)
        ElseIf Params.OPER_MODE = DeviceMode.Microwire Then
            Return DetectDevice_Microwire(m, Params)
        ElseIf Params.OPER_MODE = DeviceMode.PNOR Then
            Return DetectDevice_PNOR(m, Params)
        ElseIf Params.OPER_MODE = DeviceMode.PNAND Then
            Return DetectDevice_PNAND(m, Params)
        ElseIf Params.OPER_MODE = DeviceMode.FWH Then
            Return DetectDevice_FWH(m, Params)
        ElseIf Params.OPER_MODE = DeviceMode.HyperFlash Then
            Return DetectDevice_HyperFlash(m, Params)
        ElseIf Params.OPER_MODE = DeviceMode.EPROM Then
            Return DetectDevice_EPROM(m, Params)
        ElseIf Params.OPER_MODE = DeviceMode.P_EEPROM Then
            Return DetectDevice_PEEPROM(m, Params)
        Else '(OTHER MODES)
        End If
        Return False
    End Function

    Private Function DetectDevice_SPI(prg_if As MemoryDeviceUSB, Params As DetectParams) As Boolean
        Dim SPI_IF As SPI_Programmer = CType(prg_if, SPI_Programmer)
        Dim FCUSB = SPI_IF.GetUsbDevice()
        PrintConsole("Initializing SPI device mode")
        If Params.SPI_AUTO OrElse CUSTOM_SPI_DEV Is Nothing Then
            PrintConsole(RM.GetString("spi_attempting_detect"))
            SPI_IF.SPIBUS_Setup(GetMaxSpiClock(FCUSB.HWBOARD, SPI_SPEED.MHZ_8), MySettings.SPI_MODE)
            If SPI_IF.DeviceInit() Then
                SPI_IF.SPIBUS_Setup(GetMaxSpiClock(FCUSB.HWBOARD, Params.SPI_CLOCK), MySettings.SPI_MODE)
                SPI_IF.SPI_FASTREAD = MySettings.SPI_FASTREAD
                Dim block_size As Integer = 65536
                If FCUSB.HasLogic() Then block_size = 262144
                Connected_Event(prg_if, block_size)
                PrintConsole(RM.GetString("spi_detected_spi")) '"Detected SPI Flash on high-speed SPI port" 
                PrintConsole(String.Format(RM.GetString("spi_set_clock"), GetSpiClockString(FCUSB, Params.SPI_CLOCK)))
                Return True
            Else
                Select Case SPI_IF.MyFlashStatus
                    Case DeviceStatus.NotDetected
                        PrintConsole(RM.GetString("spi_not_detected"), True) '"Unable to detect to SPI NOR Flash device"
                    Case DeviceStatus.NotSupported
                        PrintConsole(RM.GetString("mem_not_supported"), True) '"Flash memory detected but not found in Flash library"
                End Select
            End If
        Else 'We are using a specified device
            SPI_IF.MyFlashStatus = DeviceStatus.Supported
            SPI_IF.MyFlashDevice = CUSTOM_SPI_DEV
            Connected_Event(prg_if, 65536)
            PrintConsole(String.Format(RM.GetString("spi_set_clock"), GetSpiClockString(FCUSB, Params.SPI_CLOCK)))
            Return True
        End If
        Return False
    End Function

    Private Function DetectDevice_SQI(prg_if As MemoryDeviceUSB, Params As DetectParams) As Boolean
        Dim SQI_IF As SQI_Programmer = CType(prg_if, SQI_Programmer)
        Dim FCUSB = SQI_IF.GetUsbDevice()
        PrintConsole("Initializing QUAD SPI device mode")
        SQI_IF.SQIBUS_Setup(GetMaxSqiClock(FCUSB.HWBOARD, SQI_SPEED.MHZ_10))
        If SQI_IF.DeviceInit() Then
            SQI_IF.SQIBUS_Setup(GetMaxSqiClock(FCUSB.HWBOARD, Params.SQI_CLOCK))
            Dim packet_size As Integer = 16384
            If FCUSB.HasLogic() AndAlso (Params.SQI_CLOCK > SQI_SPEED.MHZ_10) Then
                If SQI_IF.SQI_DEVICE_MODE = SQI_IO_MODE.QUAD_ONLY Then
                    packet_size = 524288 'I feel the need... the need for speed
                ElseIf SQI_IF.SQI_DEVICE_MODE = SQI_IO_MODE.SPI_QUAD Then
                    packet_size = 524288
                ElseIf SQI_IF.SQI_DEVICE_MODE = SQI_IO_MODE.DUAL_ONLY Then
                    packet_size = 262144
                ElseIf SQI_IF.SQI_DEVICE_MODE = SQI_IO_MODE.SPI_DUAL Then
                    packet_size = 262144
                Else
                    packet_size = 131072
                End If
            End If
            Connected_Event(prg_if, packet_size)
            PrintConsole(RM.GetString("spi_detected_sqi"))
            Return True
        Else
            Select Case SQI_IF.MyFlashStatus
                Case DeviceStatus.NotDetected
                    PrintConsole(RM.GetString("spi_not_detected"), True) '"Unable to detect to SPI NOR Flash device"
                Case DeviceStatus.NotSupported
                    PrintConsole(RM.GetString("mem_not_supported"), True) '"Flash memory detected but not found in Flash library"
            End Select
        End If
        Return False
    End Function

    Private Function DetectDevice_SPI_NAND(prg_if As MemoryDeviceUSB, Params As DetectParams) As Boolean
        Dim SNAND_IF As SPINAND_Programmer = CType(prg_if, SPINAND_Programmer)
        Dim FCUSB = SNAND_IF.GetUsbDevice()
        PrintConsole("Initializing SPI NAND device mode")

        SNAND_IF.MAX_CLOCK = MySettings.SPI_CLOCK_MAX
        SNAND_IF.BadBlockMode = MySettings.NAND_BadBlockMode
        SNAND_IF.DisableECC = MySettings.SPI_NAND_DISABLE_ECC
        SNAND_IF.NAND_Preserve = MySettings.NAND_Preserve

        If SNAND_IF.DeviceInit() Then
            MainApp.ECC_Init(SNAND_IF)
            Connected_Event(prg_if, 65536)
            PrintConsole(RM.GetString("spi_nand_detected"))
            PrintConsole(String.Format(RM.GetString("spi_set_clock"), GetSpiClockString(FCUSB, Params.SPI_CLOCK)))
            Return True
        Else
            Select Case SNAND_IF.MyFlashStatus
                Case DeviceStatus.NotDetected
                    PrintConsole(RM.GetString("spi_nand_unable_to_detect"), True)
                Case DeviceStatus.NotSupported
                    PrintConsole(RM.GetString("mem_not_supported"), True)
            End Select
        End If
        Return False
    End Function

    Private Function DetectDevice_SPI_EEPROM(prg_if As MemoryDeviceUSB, Params As DetectParams) As Boolean
        Dim SPI_IF As SPI_Programmer = CType(prg_if, SPI_Programmer)
        PrintConsole("Initializing SPI EEPROM device mode")
        If Not SPIEEPROM_Configure(SPI_IF, Params.SPI_EEPROM) Then
            PrintConsole("ERROR: SPI EEPROM not configured correctly")
            Return False
        End If
        Dim default_access As FlashAccess = FlashAccess.ReadWriteErase
        If (Not SPI_IF.MyFlashDevice.ERASE_REQUIRED) Then default_access = FlashAccess.ReadWrite
        Connected_Event(prg_if, 1024, default_access)
        Utilities.Sleep(100) 'Wait for device to be configured
        PrintConsole(RM.GetString("spi_eeprom_cfg"))
        Return True
    End Function

    Private Function DetectDevice_I2C_EEPROM(prg_if As MemoryDeviceUSB, Params As DetectParams) As Boolean
        Dim I2C_IF As I2C_Programmer = CType(prg_if, I2C_Programmer)
        PrintConsole("Initializing I2C EEPROM device mode")
        I2C_IF.SelectDeviceIndex(Params.I2C_INDEX)
        I2C_IF.SPEED = MySettings.I2C_SPEED
        I2C_IF.ADDRESS = MySettings.I2C_ADDRESS
        If Not I2C_IF.DeviceInit() Then Return False
        MainApp.PrintConsole(RM.GetString("i2c_attempt_detect"))
        MainApp.PrintConsole(String.Format(RM.GetString("i2c_addr_byte"), Hex(Params.I2C_ADDRESS)))
        MainApp.PrintConsole(String.Format(RM.GetString("i2c_eeprom_size"), Format(I2C_IF.DeviceSize, "#,###")))
        Select Case Params.I2C_SPEED
            Case I2C_SPEED_MODE._100kHz
                MainApp.PrintConsole(RM.GetString("i2c_protocol_speed") & ": 100kHz")
            Case I2C_SPEED_MODE._400kHz
                MainApp.PrintConsole(RM.GetString("i2c_protocol_speed") & ": 400kHz")
            Case I2C_SPEED_MODE._1MHz
                MainApp.PrintConsole(RM.GetString("i2c_protocol_speed") & ": 1MHz (Fm+)")
        End Select
        If I2C_IF.IsConnected() Then
            Connected_Event(prg_if, 512)
            PrintConsole(RM.GetString("i2c_detected"), True) '"I2C EEPROM detected and ready for operation"
            Return True
        Else
            PrintConsole(RM.GetString("i2c_not_detected"), True)
        End If
        Return False
    End Function

    Private Function DetectDevice_ONE_WIRE(prg_if As MemoryDeviceUSB, Params As DetectParams) As Boolean
        PrintConsole("Initializing 1-Wire device mode")
        Dim SWI_PROG As SWI_Programmer = CType(prg_if, SWI_Programmer)
        Dim FCUSB = SWI_PROG.GetUsbDevice()
        SWI_PROG.ADDRESS = MySettings.SWI_ADDRESS
        If SWI_PROG.DeviceInit() Then
            Dim mi As MemoryDeviceInstance = Connected_Event(prg_if, 128, FlashAccess.ReadWrite)
            mi.VendorMenu = New vendor_microchip_at21(FCUSB)
            Return True
        Else
            PrintConsole("1-wire device not detected", True)
        End If
        Return False
    End Function

    Private Function DetectDevice_Microwire(prg_if As MemoryDeviceUSB, Params As DetectParams) As Boolean
        PrintConsole("Initializing Microwire device mode")
        Dim MICRO_PROG As Microwire_Programmer = CType(prg_if, Microwire_Programmer)
        MICRO_PROG.ORGANIZATION = MySettings.S93_DEVICE_ORG
        MICRO_PROG.DEVICE_SELECT = MySettings.S93_DEVICE
        If MICRO_PROG.DeviceInit() Then
            MainApp.Connected_Event(prg_if, 256)
            Return True
        Else
            PrintConsole("Microwire device not detected", True)
        End If
        Return False
    End Function

    Private Function DetectDevice_PNOR(prg_if As MemoryDeviceUSB, Params As DetectParams) As Boolean
        Dim PNOR_IF As PARALLEL_NOR = CType(prg_if, PARALLEL_NOR)
        Dim FCUSB = PNOR_IF.GetUsbDevice()
        PrintConsole("Initializing Parallel NOR device mode")
        Utilities.Sleep(150) 'Wait for IO board vcc to charge
        PNOR_IF.MULTI_CE = MySettings.MULTI_CE
        PNOR_IF.DeviceInit()
        Select Case PNOR_IF.MyFlashStatus
            Case DeviceStatus.Supported
                PrintConsole(RM.GetString("mem_flash_supported"), True) '"Flash device successfully detected and ready for operation"
                Dim default_access As FlashAccess = FlashAccess.ReadWriteErase
                If Not PNOR_IF.ERASE_ALLOWED Then default_access = FlashAccess.ReadWrite
                If FCUSB.HWBOARD = FCUSB_BOARD.Mach1 Then
                    Connected_Event(prg_if, 262144, default_access)
                    PNOR_IF.EXPIO_SETTIMING(Params.NOR_READ_ACCESS, Params.NOR_WE_PULSE)
                Else
                    Connected_Event(prg_if, 16384, default_access)
                End If
                Return True
            Case DeviceStatus.NotSupported
                PrintConsole(RM.GetString("mem_not_supported"), True) '"Flash memory detected but not found in Flash library"
            Case DeviceStatus.NotDetected
                PrintConsole(RM.GetString("ext_not_detected"), True) '"Flash device not detected in Parallel I/O mode"
            Case DeviceStatus.ExtIoNotConnected
                PrintConsole(RM.GetString("ext_board_not_detected"), True) '"Unable to connect to the Parallel I/O board"
            Case DeviceStatus.NotCompatible
                PrintConsole("Flash memory is not compatible with this FlashcatUSB programmer model", True)
        End Select
        Return False
    End Function

    Private Function DetectDevice_PNAND(prg_if As MemoryDeviceUSB, Params As DetectParams) As Boolean
        Dim PNAND_IF As PNAND_Programmer = CType(prg_if, PNAND_Programmer)
        Dim FCUSB = PNAND_IF.GetUsbDevice()
        PrintConsole("Initializing Parallel NAND device mode")
        Utilities.Sleep(150) 'Wait for IO board vcc to charge
        PNAND_IF.BadBlockMode = MySettings.NAND_BadBlockMode
        PNAND_IF.UseRBx = MySettings.NAND_UseRBx
        PNAND_IF.Clock = MySettings.NAND_Speed
        PNAND_IF.PreserveAreas = MySettings.NAND_Preserve
        PNAND_IF.DeviceInit()
        Select Case PNAND_IF.MyFlashStatus
            Case DeviceStatus.Supported
                PrintConsole(RM.GetString("mem_flash_supported"), True) '"Flash device successfully detected and ready for operation"


                MainApp.ECC_Init(PNAND_IF)

                Dim mem_instance As MemoryDeviceInstance = Nothing
                If (PNAND_IF.MyAdapter = MEM_PROTOCOL.NAND_X16_ASYNC) Then
                    If FCUSB.HWBOARD = FCUSB_BOARD.Mach1 Then
                        mem_instance = Connected_Event(prg_if, 524288)
                    Else
                        mem_instance = Connected_Event(prg_if, 65536)
                    End If
                ElseIf (PNAND_IF.MyAdapter = MEM_PROTOCOL.NAND_X8_ASYNC) Then
                    If FCUSB.HWBOARD = FCUSB_BOARD.Mach1 Then
                        mem_instance = Connected_Event(prg_if, 524288)
                    Else
                        mem_instance = Connected_Event(prg_if, 65536)
                    End If
                End If




                Return True
            Case DeviceStatus.NotSupported
                PrintConsole(RM.GetString("mem_not_supported"), True) '"Flash memory detected but not found in Flash library"
            Case DeviceStatus.NotDetected
                PrintConsole(RM.GetString("ext_not_detected"), True) '"Flash device not detected in Parallel I/O mode"
            Case DeviceStatus.ExtIoNotConnected
                PrintConsole(RM.GetString("ext_board_not_detected"), True) '"Unable to connect to the Parallel I/O board"
            Case DeviceStatus.NotCompatible
                PrintConsole("Flash memory is not compatible with this FlashcatUSB programmer model", True)
        End Select
        Return False
    End Function

    Private Function DetectDevice_PEEPROM(prg_if As MemoryDeviceUSB, Params As DetectParams) As Boolean
        Dim PNOR_IF As PARALLEL_NOR = CType(prg_if, PARALLEL_NOR)
        PrintConsole("Initializing Parallel EEPROM device mode")
        Utilities.Sleep(150) 'Wait for IO board vcc to charge
        Dim all_eeprom_devices() As P_NOR = FlashDatabase.GetDevices_PARALLEL_EEPROM()
        Dim eeprom As P_NOR = Nothing
        For Each ee_dev In all_eeprom_devices
            If ee_dev.NAME.Equals(Params.PARALLEL_EEPROM) Then
                eeprom = ee_dev
            End If
        Next
        If eeprom Is Nothing Then Return False
        If PNOR_IF.EEPROM_Init(eeprom) Then
            Connected_Event(prg_if, 4096)
            PrintConsole("Configured to use PARALLEL EEPROM device", True)
            Return True
        Else
            Return False
        End If
    End Function

    Private Function DetectDevice_FWH(prg_if As MemoryDeviceUSB, Params As DetectParams) As Boolean
        Dim FWH_IF As FWH_Programmer = CType(prg_if, FWH_Programmer)
        PrintConsole("Initializing FWH device mode")
        FWH_IF.DeviceInit()
        Select Case FWH_IF.MyFlashStatus
            Case DeviceStatus.Supported
                Connected_Event(prg_if, 4096)
                Return True
            Case DeviceStatus.NotSupported
                PrintConsole(RM.GetString("mem_not_supported"), True) '"Flash memory detected but not found in Flash library"
            Case DeviceStatus.NotDetected
                PrintConsole("FWH device not detected", True)
        End Select
        Return False
    End Function

    Private Function DetectDevice_HyperFlash(prg_if As MemoryDeviceUSB, Params As DetectParams) As Boolean
        Dim HF_IF As HF_Programmer = CType(prg_if, HF_Programmer)
        PrintConsole("Initializing HyperFlash device mode")
        Utilities.Sleep(250) 'Wait for IO board vcc to charge
        HF_IF.DeviceInit()
        Select Case HF_IF.MyFlashStatus
            Case DeviceStatus.Supported
                Connected_Event(prg_if, 262144)
                Return True
            Case DeviceStatus.NotSupported
                PrintConsole(RM.GetString("mem_not_supported"), True) '"Flash memory detected but not found in Flash library"
            Case DeviceStatus.NotDetected
                PrintConsole("HyperFlash device not detected", True)
        End Select
        Return False
    End Function

    Private Function DetectDevice_EPROM(prg_if As MemoryDeviceUSB, Params As DetectParams) As Boolean
        PrintConsole("Initializing EPROM/OTP device mode")
        If prg_if.DeviceInit() Then
            Connected_Event(prg_if, 16384, FlashAccess.ReadWriteOnce)
            Return True
        Else
            PrintConsole("EPROM device not detected", True)
        End If
        Return False
    End Function

End Module
