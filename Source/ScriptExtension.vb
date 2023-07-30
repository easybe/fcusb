Imports System.Threading
Imports FlashcatUSB.EC_ScriptEngine
Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.JTAG
Imports FlashcatUSB.MemoryInterface
Imports FlashcatUSB.USB

Public Class ScriptExtension
    Private Delegate Function ScriptFunction(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
    Private MyProcessor As EC_ScriptEngine.Processor

    Public Event GetSelectedDevce(byref usb_dev As FCUSB_DEVICE)

    Sub New(processor As EC_ScriptEngine.Processor)
        Me.MyProcessor = processor
        AddInternalMethods()
    End Sub

    Private Sub AddInternalMethods()
        Dim MEM_CMD As New ScriptCmd("MEMORY")
        MEM_CMD.Add("name", Nothing, New ScriptFunction(AddressOf c_mem_name))
        MEM_CMD.Add("size", Nothing, New ScriptFunction(AddressOf c_mem_size))
        MEM_CMD.Add("write", {CmdPrm.Data, CmdPrm.UInteger, CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_mem_write))
        MEM_CMD.Add("read", {CmdPrm.UInteger, CmdPrm.Integer, CmdPrm.Bool_Optional}, New ScriptFunction(AddressOf c_mem_read))
        MEM_CMD.Add("wait", Nothing, New ScriptFunction(AddressOf c_mem_wait))
        MEM_CMD.Add("readstring", {CmdPrm.UInteger}, New ScriptFunction(AddressOf c_mem_readstring))
        MEM_CMD.Add("readverify", {CmdPrm.UInteger, CmdPrm.Integer}, New ScriptFunction(AddressOf c_mem_readverify))
        MEM_CMD.Add("sectorcount", Nothing, New ScriptFunction(AddressOf c_mem_sectorcount))
        MEM_CMD.Add("sectorsize", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_mem_sectorsize))
        MEM_CMD.Add("erasesector", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_mem_erasesector))
        MEM_CMD.Add("erasebulk", Nothing, New ScriptFunction(AddressOf c_mem_erasebulk))
        MEM_CMD.Add("exist", Nothing, New ScriptFunction(AddressOf c_mem_exist))
        Me.MyProcessor.AddScriptNest(MEM_CMD)
        Dim NAND_CMD As New ScriptCmd("NAND")
        NAND_CMD.Add("writepage", {CmdPrm.Data, CmdPrm.Integer, CmdPrm.Integer_Optional, CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_nand_writepage))
        Me.MyProcessor.AddScriptNest(NAND_CMD)
        Dim SPI_CMD As New ScriptCmd("SPI")
        SPI_CMD.Add("writeenable", Nothing, New ScriptFunction(AddressOf c_spi_writeenable)) 'Undocumented
        SPI_CMD.Add("clock", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_spi_clock))
        SPI_CMD.Add("mode", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_spi_mode))
        SPI_CMD.Add("database", {CmdPrm.Bool_Optional}, New ScriptFunction(AddressOf c_spi_database))
        SPI_CMD.Add("getsr", {CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_spi_getsr))
        SPI_CMD.Add("setsr", {CmdPrm.Data}, New ScriptFunction(AddressOf c_spi_setsr))
        SPI_CMD.Add("writeread", {CmdPrm.Data, CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_spi_writeread))
        SPI_CMD.Add("raw", {CmdPrm.Data, CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_spi_raw)) 'Undocumented
        SPI_CMD.Add("prog", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_spi_prog)) 'Undocumented
        Me.MyProcessor.AddScriptNest(SPI_CMD)
        Dim JTAG_CMD As New ScriptCmd("JTAG")
        JTAG_CMD.Add("idcode", Nothing, New ScriptFunction(AddressOf c_jtag_idcode))
        JTAG_CMD.Add("config", {CmdPrm.String_Optional}, New ScriptFunction(AddressOf c_jtag_config))
        JTAG_CMD.Add("select", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_jtag_select))
        JTAG_CMD.Add("print", Nothing, New ScriptFunction(AddressOf c_jtag_print))
        JTAG_CMD.Add("clear", Nothing, New ScriptFunction(AddressOf c_jtag_clear))
        JTAG_CMD.Add("set", {CmdPrm.Integer, CmdPrm.String}, New ScriptFunction(AddressOf c_jtag_set))
        JTAG_CMD.Add("add", {CmdPrm.String}, New ScriptFunction(AddressOf c_jtag_add))
        JTAG_CMD.Add("validate", Nothing, New ScriptFunction(AddressOf c_jtag_validate))
        JTAG_CMD.Add("writeword", {CmdPrm.UInteger, CmdPrm.UInteger}, New ScriptFunction(AddressOf c_jtag_write32))
        JTAG_CMD.Add("readword", {CmdPrm.UInteger}, New ScriptFunction(AddressOf c_jtag_read32))
        JTAG_CMD.Add("control", {CmdPrm.UInteger}, New ScriptFunction(AddressOf c_jtag_control))
        JTAG_CMD.Add("memoryinit", {CmdPrm.String, CmdPrm.UInteger_Optional, CmdPrm.UInteger_Optional}, New ScriptFunction(AddressOf c_jtag_memoryinit))
        JTAG_CMD.Add("debug", {CmdPrm.Bool}, New ScriptFunction(AddressOf c_jtag_debug))
        JTAG_CMD.Add("cpureset", Nothing, New ScriptFunction(AddressOf c_jtag_cpureset))
        JTAG_CMD.Add("runsvf", {CmdPrm.Data}, New ScriptFunction(AddressOf c_jtag_runsvf))
        JTAG_CMD.Add("runxsvf", {CmdPrm.Data}, New ScriptFunction(AddressOf c_jtag_runxsvf))
        JTAG_CMD.Add("shiftdr", {CmdPrm.Data, CmdPrm.Integer, CmdPrm.Bool_Optional}, New ScriptFunction(AddressOf c_jtag_shiftdr))
        JTAG_CMD.Add("shiftir", {CmdPrm.Data, CmdPrm.Bool_Optional}, New ScriptFunction(AddressOf c_jtag_shiftir))
        JTAG_CMD.Add("shiftout", {CmdPrm.Data, CmdPrm.Integer, CmdPrm.Bool_Optional}, New ScriptFunction(AddressOf c_jtag_shiftout))
        JTAG_CMD.Add("tapreset", Nothing, New ScriptFunction(AddressOf c_jtag_tapreset))
        JTAG_CMD.Add("state", {CmdPrm.String}, New ScriptFunction(AddressOf c_jtag_state))
        JTAG_CMD.Add("graycode", {CmdPrm.Integer, CmdPrm.Bool_Optional}, New ScriptFunction(AddressOf c_jtag_graycode))
        JTAG_CMD.Add("setdelay", {CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_jtag_setdelay)) 'Legacy support
        JTAG_CMD.Add("exitstate", {CmdPrm.Bool}, New ScriptFunction(AddressOf c_jtag_exitstate)) 'SVF player option
        JTAG_CMD.Add("epc2_read", Nothing, New ScriptFunction(AddressOf c_jtag_epc2_read))
        JTAG_CMD.Add("epc2_write", {CmdPrm.Data, CmdPrm.Data}, New ScriptFunction(AddressOf c_jtag_epc2_write))
        JTAG_CMD.Add("epc2_erase", Nothing, New ScriptFunction(AddressOf c_jtag_epc2_erase))
        Me.MyProcessor.AddScriptNest(JTAG_CMD)
        Dim BSDL_CMD As New ScriptCmd("BSDL")
        BSDL_CMD.Add("new", {CmdPrm.String, CmdPrm.String, CmdPrm.String_Optional}, New ScriptFunction(AddressOf c_bsdl_new))
        BSDL_CMD.Add("find", {CmdPrm.String, CmdPrm.String_Optional}, New ScriptFunction(AddressOf c_bsdl_find))
        BSDL_CMD.Add("parameter", {CmdPrm.String, CmdPrm.UInteger}, New ScriptFunction(AddressOf c_bsdl_param))
        Me.MyProcessor.AddScriptNest(BSDL_CMD)
        Dim JTAG_BSP As New ScriptCmd("BoundaryScan")
        JTAG_BSP.Add("setup", Nothing, New ScriptFunction(AddressOf c_bsp_setup))
        JTAG_BSP.Add("init", {CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_bsp_init))
        JTAG_BSP.Add("addpin", {CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_bsp_addpin))
        JTAG_BSP.Add("setbsr", {CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Bool}, New ScriptFunction(AddressOf c_bsp_setbsr))
        JTAG_BSP.Add("writebsr", Nothing, New ScriptFunction(AddressOf c_bsp_writebsr))
        JTAG_BSP.Add("detect", Nothing, New ScriptFunction(AddressOf c_bsp_detect))
        Me.MyProcessor.AddScriptNest(JTAG_BSP)
        Dim PAR_CMD As New ScriptCmd("parallel")
        PAR_CMD.Add("test", Nothing, New ScriptFunction(AddressOf c_parallel_test))
        PAR_CMD.Add("command", {CmdPrm.UInteger, CmdPrm.UInteger}, New ScriptFunction(AddressOf c_parallel_command))
        PAR_CMD.Add("write", {CmdPrm.UInteger, CmdPrm.UInteger}, New ScriptFunction(AddressOf c_parallel_write))
        PAR_CMD.Add("read", {CmdPrm.UInteger}, New ScriptFunction(AddressOf c_parallel_read))
        Me.MyProcessor.AddScriptNest(PAR_CMD)
        Dim LOADOPT As New ScriptCmd("load") 'Undocumented
        LOADOPT.Add("firmware", Nothing, New ScriptFunction(AddressOf c_load_firmware))
        LOADOPT.Add("logic", Nothing, New ScriptFunction(AddressOf c_load_logic))
        LOADOPT.Add("erase", Nothing, New ScriptFunction(AddressOf c_load_erase))
        LOADOPT.Add("bootloader", {CmdPrm.Data}, New ScriptFunction(AddressOf c_load_bootloader))
        LOADOPT.Add("eeprom", {CmdPrm.Data}, New ScriptFunction(AddressOf c_load_eeprom))
        Me.MyProcessor.AddScriptNest(LOADOPT)
        Dim READOPT As New ScriptCmd("read") 'Undocumented
        READOPT.Add("eeprom", {CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_read_eeprom))
        Me.MyProcessor.AddScriptNest(READOPT)
        Dim FLSHOPT As New ScriptCmd("nor_flash") 'Undocumented
        Dim cb_flash_parallel_add As New ScriptFunction(AddressOf c_norflash_parallel_add)
        FLSHOPT.Add("add", {CmdPrm.String, CmdPrm.UInteger, CmdPrm.UInteger, CmdPrm.UInteger, CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer}, cb_flash_parallel_add)
        Me.MyProcessor.AddScriptNest(FLSHOPT)
        FLSHOPT.Add("addsector", {CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_norflash_sector_add))
        Me.MyProcessor.AddScriptCommand("endian", {CmdPrm.String}, New ScriptFunction(AddressOf c_endian))
        Me.MyProcessor.AddScriptCommand("verify", {CmdPrm.Bool}, New ScriptFunction(AddressOf c_verify))
        Me.MyProcessor.AddScriptCommand("mode", Nothing, New ScriptFunction(AddressOf c_mode))
        Me.MyProcessor.AddScriptCommand("refresh", Nothing, New ScriptFunction(AddressOf c_refresh))
    End Sub

    Private Function GetDevice() As FCUSB_DEVICE
        Dim FCUSB As FCUSB_DEVICE = Nothing
        RaiseEvent GetSelectedDevce(FCUSB)
        Return FCUSB
    End Function

#Region "Memory commands"

    Private Function c_mem_name(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Memory device not connected"}
        End If
        Dim sv As New ScriptVariable(CreateVarName(), DataType.String)
        sv.Value = mem_device.Name
        Return sv
    End Function

    Private Function c_mem_size(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If (MEM_IF.GetDevice(CInt(Index)).Size > CLng(Int32.MaxValue)) Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Memory device is larger than 2GB"}
        End If
        Dim size_value As Int32 = CInt(MEM_IF.GetDevice(CInt(Index)).Size)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Integer)
        sv.Value = size_value
        Return sv
    End Function

    Private Function c_mem_write(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim write_result As Boolean
        Dim data_to_write() As Byte = CType(arguments(0).Value, Byte())
        Dim offset As UInt32 = CUInt(arguments(1).Value)
        Dim data_len As Int32 = data_to_write.Length
        If (arguments.Length > 2) Then data_len = CInt(arguments(2).Value)
        ReDim Preserve data_to_write(data_len - 1)
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Memory device not connected"}
        End If
        Dim cb As New MemoryDeviceInstance.StatusCallback
        cb.UpdatePercent = New UpdateFunction_Progress(AddressOf Callback_UpdatePercent)
        cb.UpdateTask = New UpdateFunction_Status(AddressOf Callback_UpdateStatus)
        mem_device.Controls_Disable()
        mem_device.FCUSB.USB_LEDBlink()
        ProgressBar_SetDevice(mem_device)
        ProgressBar_Percent(0)
        Try
            write_result = mem_device.WriteBytes(offset, data_to_write, MySettings.VERIFY_WRITE, cb)
            If write_result Then
                PrintConsole("Sucessfully programmed " & data_len.ToString("N0") & " bytes")
            Else
                PrintConsole("Memory write operation did not complete")
            End If
        Catch ex As Exception
        Finally
            mem_device.Controls_Enable()
            mem_device.FCUSB.USB_LEDOn()
            ProgressBar_Dispose()
        End Try
        Return New ScriptVariable(CreateVarName(), DataType.Bool, write_result)
    End Function

    Private Function c_mem_read(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Memory device not connected"}
        End If
        Dim offset As UInt32 = CUInt(arguments(0).Value)
        Dim count As Int32 = CInt(arguments(1).Value)
        Dim display As Boolean = True
        If (arguments.Length > 2) Then display = CBool(arguments(2).Value)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Data)
        Dim cb As New MemoryDeviceInstance.StatusCallback
        If display Then
            cb.UpdatePercent = New UpdateFunction_Progress(AddressOf Callback_UpdatePercent)
            cb.UpdateTask = New UpdateFunction_Status(AddressOf Callback_UpdateStatus)
        End If
        mem_device.Controls_Disable()
        ProgressBar_SetDevice(mem_device)
        ProgressBar_Percent(0)
        Try
            Dim data_read() As Byte = Nothing
            data_read = mem_device.ReadBytes(offset, count, cb)
            If data_read Is Nothing Then Return New ScriptVariable(CreateVarName(), DataType.Error, "Error reading from memory device")
            sv.Value = data_read
            MainApp.SetStatus(String.Format("{0} bytes read from memory device: {1}", {data_read.Length.ToString, mem_device.Name}))
        Catch ex As Exception
        Finally
            mem_device.Controls_Enable()
        End Try
        ProgressBar_Percent(0)
        Return sv
    End Function

    Private Function c_mem_wait(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Memory device not connected"}
        End If
        mem_device.WaitUntilReady()
        Return Nothing
    End Function

    Private Function c_mem_readstring(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Memory device not connected"}
        End If
        Dim offset As UInt32 = CUInt(arguments(0).Value)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.String)
        Dim FlashSize As UInt32 = CUInt(mem_device.Size)
        If offset + 1 > FlashSize Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Offset is greater than flash size"}
        End If
        Dim strBuilder As String = ""
        For i = offset To FlashSize - 1
            Dim flash_data() As Byte = mem_device.ReadBytes(CLng(i), 1)
            Dim b As Byte = flash_data(0)
            If b > 31 And b < 127 Then
                strBuilder &= Chr(b)
            ElseIf b = 0 Then
                Exit For
            Else
                Return Nothing 'Error
            End If
        Next
        sv.Value = strBuilder
        Return sv
    End Function

    Private Function c_mem_readverify(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Memory device not connected"}
        End If
        Dim FlashAddress As UInt32 = CUInt(arguments(0).Value)
        Dim FlashLen As Int32 = CInt(arguments(1).Value)
        Dim data() As Byte = Nothing
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Data)
        ProgressBar_Percent(0)
        mem_device.Controls_Disable()
        Try
            data = ReadMemoryVerify(FlashAddress, CUInt(FlashLen), CType(Index, FlashArea))
        Catch ex As Exception
        Finally
            mem_device.Controls_Enable()
            ProgressBar_Percent(0)
        End Try
        If data Is Nothing Then
            PrintConsole("Read operation failed")
            Return Nothing
        End If
        sv.Value = data
        Return sv
    End Function

    Private Function c_mem_sectorcount(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Memory device not connected"}
        End If
        Dim sector_count As Int32 = CInt(mem_device.GetSectorCount)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Integer)
        sv.Value = sector_count
        Return sv
    End Function

    Private Function c_mem_sectorsize(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Memory device not connected"}
        End If
        Dim sector_int As Int32 = CInt(arguments(0).Value)
        Dim sector_size As Integer = mem_device.GetSectorSize(sector_int)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Integer)
        sv.Value = sector_size
        Return sv
    End Function

    Private Function c_mem_erasesector(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Memory device not connected"}
        End If
        Dim mem_sector As Int32 = CInt(arguments(0).Value)
        mem_device.EraseSector(mem_sector)
        Dim target_addr As Long = mem_device.GetSectorAddress(mem_sector)
        Dim target_area As String = "0x" & Hex(target_addr).PadLeft(8, "0"c) & " to 0x" & Hex(target_addr + mem_device.GetSectorSize(mem_sector) - 1).PadLeft(8, "0"c)
        If mem_device.NoErrors Then
            PrintConsole("Successfully erased sector index: " & mem_sector & " (" & target_area & ")")
        Else
            PrintConsole("Failed to erase sector index: " & mem_sector & " (" & target_area & ")")
        End If
        mem_device.Controls_Refresh()
        mem_device.ReadMode()
        Return Nothing
    End Function

    Private Function c_mem_erasebulk(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Memory device not connected"}
        End If
        Try
            MEM_IF.GetDevice(CInt(Index)).Controls_Disable()
            mem_device.EraseFlash()
        Catch ex As Exception
        Finally
            MEM_IF.GetDevice(CInt(Index)).Controls_Enable()
        End Try
        Return Nothing
    End Function

    Private Function c_mem_exist(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Bool)
        If mem_device Is Nothing Then
            sv.Value = False
        Else
            sv.Value = True
        End If
        Return sv
        Return Nothing
    End Function

#End Region

#Region "NAND commands"

    Private Function c_nand_writepage(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If (Not CURRENT_DEVICE_MODE = DeviceMode.PNAND) Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Device is not in NAND or SPI NAND mode"}
        End If
        If (Not MyDevice.IS_CONNECTED) Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "FlashcatUSB device is not connected"}
        End If
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "NAND memory device not connected"}
        End If
        Dim NAND_Device As G_NAND = CType(mem_device.MEM_IF, PNAND_Programmer).MyFlashDevice
        Dim input_data() As Byte = CType(arguments(0).Value, Byte())
        Dim page_index As Int32 = CInt(arguments(1).Value)
        Dim area As FlashArea = FlashArea.Main
        If (arguments.Length > 2) Then
            Dim area_int As Int32 = CInt(arguments(2).Value)
            Select Case area_int
                Case 0
                    area = FlashArea.Main
                Case 1
                    area = FlashArea.OOB
                Case 2
                    area = FlashArea.All
                Case Else
                    Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "NAND area value is not valid"}
            End Select
        End If
        Dim page_offset As Int32 = 0
        If (arguments.Length > 3) Then
            page_offset = CInt(arguments(3).Value)
        End If
        Dim NAND_IF As PNAND_Programmer = CType(mem_device.MEM_IF, PNAND_Programmer)
        Dim operation_result As Boolean = NAND_IF.WritePage(page_index, input_data, area, page_offset)
        If operation_result Then Return Nothing 'No Error
        Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "NAND WritePage operation was not successful"}
    End Function

#End Region

#Region "SPI commands"

    Private Function c_spi_writeenable(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim current_if() As MemoryDeviceInstance = MEM_IF.GetDevices()
        If current_if Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "A SPI/QSPI memory device must be connected"}
        End If
        If CURRENT_DEVICE_MODE = DeviceMode.SPI Then
            Dim SPI_IF As SPI.SPI_Programmer = CType(current_if(0).MEM_IF, SPI.SPI_Programmer)
            SPI_IF.SPIBUS_WriteEnable()
        ElseIf CURRENT_DEVICE_MODE = DeviceMode.SQI Then
            Dim SQI_IF As SPI.SQI_Programmer = CType(current_if(0).MEM_IF, SPI.SQI_Programmer)
            SQI_IF.SQIBUS_WriteEnable()
        End If
        Return Nothing
    End Function

    Private Function c_spi_clock(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim clock_int As Int32 = CInt(arguments(0).Value)
        MySettings.SPI_CLOCK_MAX = CType(clock_int, SPI.SPI_SPEED)
        If (MySettings.SPI_CLOCK_MAX < SPI.SPI_SPEED.MHZ_1) Then MySettings.SPI_CLOCK_MAX = SPI.SPI_SPEED.MHZ_1
        Return Nothing
    End Function

    Private Function c_spi_mode(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim mode_int As Int32 = CInt(arguments(0).Value)
        Select Case mode_int
            Case 0
                MySettings.SPI_MODE = SPI.SPI_CLOCK_POLARITY.SPI_MODE_0
            Case 1
                MySettings.SPI_MODE = SPI.SPI_CLOCK_POLARITY.SPI_MODE_1
            Case 2
                MySettings.SPI_MODE = SPI.SPI_CLOCK_POLARITY.SPI_MODE_2
            Case 3
                MySettings.SPI_MODE = SPI.SPI_CLOCK_POLARITY.SPI_MODE_3
        End Select
        Return Nothing
    End Function

    Private Function c_spi_database(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim DisplayJedecID As Boolean = False
        If arguments.Length = 1 Then
            DisplayJedecID = CBool(arguments(0).Value)
        End If
        PrintConsole("The internal Flash database consists of " & FlashDatabase.FlashDB.Count & " devices")
        For Each mem_entry In FlashDatabase.FlashDB
            If mem_entry.FLASH_TYPE = FlashMemory.MemoryType.SERIAL_NOR Then
                Dim size_str As String = ""
                Dim size_int As UInt32 = CUInt(mem_entry.FLASH_SIZE)
                If (size_int < 128) Then
                    size_str = (size_int / 8) & "bits"
                ElseIf (size_int < 131072) Then
                    size_str = (size_int / 128) & "Kbits"
                Else
                    size_str = (size_int / 131072) & "Mbits"
                End If
                If DisplayJedecID Then
                    Dim jedec_str As String = Hex(mem_entry.MFG_CODE).PadLeft(2, "0"c) & Hex(mem_entry.ID1).PadLeft(4, "0"c)
                    If (jedec_str = "000000") Then
                        PrintConsole(mem_entry.NAME & " (" & size_str & ") EEPROM")
                    Else
                        PrintConsole(mem_entry.NAME & " (" & size_str & ") JEDEC: 0x" & jedec_str)
                    End If
                Else
                    PrintConsole(mem_entry.NAME & " (" & size_str & ")")
                End If
            End If
        Next
        PrintConsole("SPI Flash database list complete")
        Return Nothing
    End Function

    Private Function c_spi_getsr(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If CURRENT_DEVICE_MODE = DeviceMode.SPI Then
        ElseIf CURRENT_DEVICE_MODE = DeviceMode.SQI Then
        Else
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Device is not in SPI/QUAD operation mode"}
        End If
        If Not MyDevice.IS_CONNECTED Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "FlashcatUSB device is not connected"}
        End If
        Dim bytes_to_read As Int32 = 1
        If arguments.Length > 0 Then bytes_to_read = CInt(arguments(0).Value)
        Dim current_if() As MemoryDeviceInstance = MEM_IF.GetDevices()
        If current_if Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "A SPI/QSPI memory device must be connected"}
        End If
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Data)
        If CURRENT_DEVICE_MODE = DeviceMode.SPI Then
            Dim SPI_IF As SPI.SPI_Programmer = CType(current_if(0).MEM_IF, SPI.SPI_Programmer)
            sv.Value = SPI_IF.ReadStatusRegister(bytes_to_read)
        ElseIf CURRENT_DEVICE_MODE = DeviceMode.SQI Then
            Dim SQI_IF As SPI.SQI_Programmer = CType(current_if(0).MEM_IF, SPI.SQI_Programmer)
            sv.Value = SQI_IF.ReadStatusRegister(bytes_to_read)
        End If
        Return sv
    End Function

    Private Function c_spi_setsr(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If CURRENT_DEVICE_MODE = DeviceMode.SPI Then
        ElseIf CURRENT_DEVICE_MODE = DeviceMode.SQI Then
        Else
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Device is not in SPI/QUAD operation mode"}
        End If
        If Not MyDevice.IS_CONNECTED Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "FlashcatUSB device is not connected"}
        End If
        Dim current_if() As MemoryDeviceInstance = MEM_IF.GetDevices()
        If current_if Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "A SPI/QSPI memory device must be connected"}
        End If
        Dim data_out() As Byte = CType(arguments(0).Value, Byte())
        If CURRENT_DEVICE_MODE = DeviceMode.SPI Then
            Dim SPI_IF As SPI.SPI_Programmer = CType(current_if(0).MEM_IF, SPI.SPI_Programmer)
            SPI_IF.WriteStatusRegister(data_out)
        ElseIf CURRENT_DEVICE_MODE = DeviceMode.SQI Then
            Dim SQI_IF As SPI.SQI_Programmer = CType(current_if(0).MEM_IF, SPI.SQI_Programmer)
            SQI_IF.WriteStatusRegister(data_out)
        End If
        Return Nothing
    End Function

    Private Function c_spi_writeread(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If Not MyDevice.IS_CONNECTED Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "FlashcatUSB device is not connected"}
        End If
        If CURRENT_DEVICE_MODE = DeviceMode.SPI Then
        ElseIf CURRENT_DEVICE_MODE = DeviceMode.SQI Then
        Else
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Device is not in SPI/QUAD operation mode"}
        End If
        Dim current_if() As MemoryDeviceInstance = MEM_IF.GetDevices()
        If current_if Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "A SPI/QSPI memory device must be connected"}
        End If
        Dim DataToWrite() As Byte = CType(arguments(0).Value, Byte())
        Dim ReadBack As Int32 = 0
        If arguments.Length = 2 Then ReadBack = CInt(arguments(1).Value)
        If CURRENT_DEVICE_MODE = DeviceMode.SPI Then
            Dim SPI_IF As SPI.SPI_Programmer = CType(current_if(0).MEM_IF, SPI.SPI_Programmer)
            If ReadBack = 0 Then
                Dim bytes_count As Integer = SPI_IF.SPIBUS_WriteRead(DataToWrite)
                If (bytes_count = -1) Then Return New ScriptVariable(CreateVarName(), DataType.Error, "SPI Communication Error")
            Else
                Dim return_data(ReadBack - 1) As Byte
                Dim bytes_count As Integer = SPI_IF.SPIBUS_WriteRead(DataToWrite, return_data)
                If (bytes_count = -1) Then Return New ScriptVariable(CreateVarName(), DataType.Error, "SPI Communication Error")
                Dim sv As New ScriptVariable(CreateVarName(), DataType.Data)
                sv.Value = return_data
                Return sv
            End If
        ElseIf CURRENT_DEVICE_MODE = DeviceMode.SQI Then
            Dim SQI_IF As SPI.SQI_Programmer = CType(current_if(0).MEM_IF, SPI.SQI_Programmer)
            If ReadBack = 0 Then
                SQI_IF.SQIBUS_WriteRead(DataToWrite)
            Else
                Dim return_data(ReadBack - 1) As Byte
                SQI_IF.SQIBUS_WriteRead(DataToWrite, return_data)
                Dim sv As New ScriptVariable(CreateVarName(), DataType.Data)
                sv.Value = return_data
                Return sv
            End If
        End If
        Return Nothing
    End Function

    Private Function c_spi_raw(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If Not MyDevice.IS_CONNECTED Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "FlashcatUSB device is not connected"}
        End If
        If CURRENT_DEVICE_MODE = DeviceMode.SPI Then
        Else
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Device is not in SPI NOR FLASH mode"}
        End If
        Dim DataToWrite() As Byte = CType(arguments(0).Value, Byte())
        Dim BytesToWrite As Integer = DataToWrite.Length
        Dim BytesToRead As Integer = 0
        If arguments.Length = 2 Then BytesToRead = CInt(arguments(1).Value)
        Dim SPI_IF As New SPI.SPI_Programmer(MyDevice)
        SPI_IF.SPIBUS_Setup(GetMaxSpiClock(MyDevice.HWBOARD, MySettings.SPI_CLOCK_MAX), MySettings.SPI_MODE)
        If BytesToRead = 0 Then
            Dim bytes_count As Integer = SPI_IF.SPIBUS_WriteRead(DataToWrite)
            If (bytes_count = -1) Then Return New ScriptVariable(CreateVarName(), DataType.Error, "SPI Communication Error")
        Else
            Dim return_data(BytesToRead - 1) As Byte
            Dim bytes_count As Integer = SPI_IF.SPIBUS_WriteRead(DataToWrite, return_data)
            If (bytes_count = -1) Then Return New ScriptVariable(CreateVarName(), DataType.Error, "SPI Communication Error")
            Dim sv As New ScriptVariable(CreateVarName(), DataType.Data)
            sv.Value = return_data
            Return sv
        End If
        Return Nothing
    End Function

    Private Function c_spi_prog(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If Not CURRENT_DEVICE_MODE = DeviceMode.SPI Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Device is not in SPI operation mode"}
        ElseIf Not MyDevice.IS_CONNECTED Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "FlashcatUSB device is not connected"}
        End If
        Dim SPI_IF As New SPI.SPI_Programmer(MyDevice)
        Dim state As Integer = CInt(arguments(0).Value)
        If (state = 1) Then 'Set the PROGPIN to HIGH
            SPI_IF.SetProgPin(True)
        Else 'Set the PROGPIN to LOW
            SPI_IF.SetProgPin(False)
        End If
        Return Nothing
    End Function

#End Region

#Region "JTAG"

    Private Function c_jtag_idcode(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim sv As New ScriptVariable(CreateVarName(), DataType.UInteger)
        If MyDevice Is Nothing Then
            PrintConsole("Error: FlashcatUSB not connected")
        ElseIf MyDevice.JTAG_IF.Devices.Count = 0 Then
            PrintConsole("Error: no JTAG devices detected")
        Else
            Dim current_index As Integer = MyDevice.JTAG_IF.Chain_SelectedIndex
            sv.Value = MyDevice.JTAG_IF.Devices(current_index).IDCODE
        End If
        Return sv
    End Function

    Private Function c_jtag_config(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If arguments.Length = 1 Then
            Select Case CStr(arguments(0).Value).ToUpper
                Case "MIPS"
                    MyDevice.JTAG_IF.Configure(JTAG.PROCESSOR.MIPS)
                Case "ARM"
                    MyDevice.JTAG_IF.Configure(JTAG.PROCESSOR.ARM)
                Case Else
                    Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Unknown mode: " & CStr(arguments(0).Value)}
            End Select
        Else
            MyDevice.JTAG_IF.Configure(JTAG.PROCESSOR.NONE)
        End If
        Return Nothing
    End Function

    Private Function c_jtag_select(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If MyDevice Is Nothing OrElse Not MyDevice.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "JTAG operations are not currently valid"}
        End If
        Dim select_index As Integer = CInt(arguments(0).Value)
        If MyDevice.JTAG_IF.Chain_IsValid Then
            MyDevice.JTAG_IF.Chain_Select(CInt(Index))
            Return New ScriptVariable(CreateVarName(), DataType.Bool, True)
        Else
            PrintConsole("JTAG chain is not valid, not all devices have BSDL loaded")
        End If
        Return New ScriptVariable(CreateVarName(), DataType.Bool, False)
    End Function

    Private Function c_jtag_print(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        MyDevice.JTAG_IF.Chain_Print()
        Return Nothing
    End Function

    Private Function c_jtag_clear(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        MyDevice.JTAG_IF.Chain_Clear()
        Return Nothing
    End Function

    Private Function c_jtag_set(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim jtag_device_index As Int32 = CInt(arguments(0).Value)
        Dim bsdl_name As String = CStr(arguments(1).Value)
        If MyDevice.JTAG_IF.Chain_Set(jtag_device_index, bsdl_name) Then
            PrintConsole("Successful set chain index " & jtag_device_index.ToString & " to " & bsdl_name)
        Else
            PrintConsole("Error: unable to find internal BSDL device with name " & bsdl_name)
        End If
        Return Nothing
    End Function

    Private Function c_jtag_add(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim bsdl_lib As String = CStr(arguments(0).Value)
        If MyDevice.JTAG_IF.Chain_Add(bsdl_lib) Then
            PrintConsole("Successful added BSDL to JTAG chain")
        Else
            PrintConsole("Error: BSDL library " & bsdl_lib & " not found")
        End If
        Return Nothing
    End Function

    Private Function c_jtag_validate(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If MyDevice.JTAG_IF.Chain_Validate() Then
            PrintConsole("JTAG chain is valid")
        Else
            PrintConsole("JTAG chain is invalid")
        End If
        Return Nothing
    End Function

    Private Function c_jtag_control(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim control_value As UInt32 = CUInt(arguments(0).Value)
        Dim j As BSDL_DEF = MyDevice.JTAG_IF.Chain_Get(MyDevice.JTAG_IF.Chain_SelectedIndex)
        If j IsNot Nothing Then
            Dim result As UInt32 = MyDevice.JTAG_IF.AccessDataRegister32(j.MIPS_CONTROL, control_value)
            PrintConsole("JTAT CONTROL command issued: 0x" & Hex(control_value) & " result: 0x" & Hex(result))
            Dim sv As New ScriptVariable(CreateVarName(), DataType.UInteger)
            sv.Value = result
            Return sv
        End If
        Return Nothing
    End Function

    Private Function c_jtag_memoryinit(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim flash_type As String = CStr(arguments(0).Value)
        Dim new_dev As MemoryDeviceInstance = Nothing
        Select Case flash_type.ToUpper
            Case "CFI"
                Dim base_address As UInt32 = CUInt(arguments(1).Value)
                PrintConsole(String.Format(RM.GetString("jtag_cfi_attempt_detect"), Hex(base_address).PadLeft(8, "0"c)))
                If MyDevice.JTAG_IF.CFI_Detect(base_address) Then
                    'new_dev = Connected_Event(MyDevice, 16384)
                Else
                    PrintConsole(RM.GetString("jtag_cfi_no_detect"))
                End If
            Case "SPI"
                PrintConsole(RM.GetString("jtag_spi_attempt_detect"))
                If MyDevice.JTAG_IF.SPI_Detect(CType(CUInt(arguments(1).Value), JTAG_IF.JTAG_SPI_Type)) Then
                    'new_dev = Connected_Event(MyDevice, 16384)
                Else
                    PrintConsole(RM.GetString("jtag_spi_no_detect")) '"Error: unable to detect SPI flash device over JTAG"
                End If
            Case Else
                Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Error in JTAG.MemoryInit: device type not specified"}
        End Select
        If new_dev IsNot Nothing Then
            Return New ScriptVariable(CreateVarName(), DataType.UInteger) With {.Value = (MEM_IF.DeviceCount - 1)}
        Else
            PrintConsole("JTAG.MemoryInit: failed to create new memory device interface")
        End If
        Return Nothing
    End Function

    Private Function c_jtag_debug(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim enable As Boolean = CBool(arguments(0).Value)
        If enable Then
            MyDevice.JTAG_IF.EJTAG_Debug_Enable()
        Else
            MyDevice.JTAG_IF.EJTAG_Debug_Disable()
        End If
        Return Nothing
    End Function

    Private Function c_jtag_cpureset(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        MyDevice.JTAG_IF.EJTAG_Reset()
        Return Nothing
    End Function

    Private Function c_jtag_runsvf(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If MyDevice Is Nothing OrElse Not MyDevice.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "JTAG operations are not currently valid"}
        End If
        ProgressBar_Percent(0)
        RemoveHandler MyDevice.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
        AddHandler MyDevice.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
        PrintConsole("Running SVF file in internal JTAG SVF player")
        Dim DataBytes() As Byte = CType(arguments(0).Value, Byte())
        Dim FileStr() As String = Utilities.Bytes.ToCharStringArray(DataBytes)
        Dim result As Boolean = MyDevice.JTAG_IF.JSP.RunFile_SVF(FileStr)
        If result Then
            PrintConsole("SVF file successfully played")
        Else
            PrintConsole("Error playing the SVF file")
        End If
        ProgressBar_Percent(0)
        RemoveHandler MyDevice.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Bool)
        sv.Value = result
        Return sv
    End Function

    Private Function c_jtag_runxsvf(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If MyDevice Is Nothing OrElse Not MyDevice.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "JTAG operations are not currently valid"}
        End If
        ProgressBar_Percent(0)
        RemoveHandler MyDevice.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
        AddHandler MyDevice.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
        PrintConsole("Running XSVF file in internal JTAG XSVF player")
        Dim DataBytes() As Byte = CType(arguments(0).Value, Byte())
        Dim result As Boolean = MyDevice.JTAG_IF.JSP.RunFile_XSVF(DataBytes)
        If result Then
            PrintConsole("XSVF file successfully played")
        Else
            PrintConsole("Error playing the XSVF file")
        End If
        ProgressBar_Percent(0)
        RemoveHandler MyDevice.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Bool)
        sv.Value = result
        Return sv
    End Function

    Private Function c_jtag_shiftdr(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If MyDevice Is Nothing OrElse Not MyDevice.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "JTAG operations are not currently valid"}
        End If
        Dim exit_mode As Boolean = True
        Dim data_in() As Byte = CType(arguments(0).Value, Byte())
        Dim bit_count As Integer = CInt(arguments(1).Value)
        Dim data_out() As Byte = Nothing
        If arguments.Length = 3 Then exit_mode = CBool(arguments(1).Value)
        MyDevice.JTAG_IF.JSP_ShiftDR(data_in, data_out, CUShort(bit_count), exit_mode)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Data)
        sv.Value = data_out
        Return sv
    End Function

    Private Function c_jtag_shiftir(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If MyDevice Is Nothing OrElse Not MyDevice.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "JTAG operations are not currently valid"}
        End If
        Dim exit_mode As Boolean = True
        Dim data_in() As Byte = CType(arguments(0).Value, Byte())
        If arguments.Length = 2 Then exit_mode = CBool(arguments(1).Value)
        Dim ir_size As Int16 = CShort(MyDevice.JTAG_IF.GetSelected_IRLength())
        MyDevice.JTAG_IF.JSP_ShiftIR(data_in, Nothing, ir_size, exit_mode)
        Return Nothing
    End Function

    Private Function c_jtag_shiftout(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If MyDevice Is Nothing OrElse Not MyDevice.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "JTAG operations are not currently valid"}
        End If
        Dim tdi_data() As Byte = CType(arguments(0).Value, Byte())
        Dim bit_count As Integer = CInt(arguments(1).Value)
        Dim exit_tms As Boolean = True
        If arguments.Length = 3 Then exit_tms = CBool(arguments(2).Value)
        Dim tdo_data() As Byte = Nothing
        MyDevice.JTAG_IF.ShiftTDI(CUInt(bit_count), tdi_data, tdo_data, exit_tms)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Data)
        sv.Value = tdo_data
        Return sv
    End Function

    Private Function c_jtag_tapreset(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If MyDevice Is Nothing OrElse Not MyDevice.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "JTAG operations are not currently valid"}
        End If
        MyDevice.JTAG_IF.Reset_StateMachine()
        Return Nothing
    End Function

    Private Function c_jtag_write32(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If MyDevice Is Nothing OrElse Not MyDevice.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "JTAG operations are not currently valid"}
        End If
        Dim addr32 As UInt32 = CUInt(arguments(0).Value)
        Dim data As UInt32 = CUInt(arguments(1).Value)
        MyDevice.JTAG_IF.WriteMemory(addr32, data, DATA_WIDTH.Word)
        Return Nothing
    End Function

    Private Function c_jtag_read32(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If MyDevice Is Nothing OrElse Not MyDevice.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "JTAG operations are not currently valid"}
        End If
        Dim addr32 As UInt32 = CUInt(arguments(0).Value)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.UInteger)
        sv.Value = MyDevice.JTAG_IF.ReadMemory(addr32, DATA_WIDTH.Word)
        Return sv
    End Function

    Private Function c_jtag_state(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If MyDevice Is Nothing OrElse Not MyDevice.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "JTAG operations are not currently valid"}
        End If
        Dim state_str As String = CStr(arguments(0).Value)
        Select Case state_str.ToUpper
            Case "RunTestIdle".ToUpper
                MyDevice.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            Case "Select_DR".ToUpper
                MyDevice.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Select_DR)
            Case "Capture_DR".ToUpper
                MyDevice.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Capture_DR)
            Case "Shift_DR".ToUpper
                MyDevice.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            Case "Exit1_DR".ToUpper
                MyDevice.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Exit1_DR)
            Case "Pause_DR".ToUpper
                MyDevice.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Pause_DR)
            Case "Exit2_DR".ToUpper
                MyDevice.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Exit2_DR)
            Case "Update_DR".ToUpper
                MyDevice.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Update_DR)
            Case "Select_IR".ToUpper
                MyDevice.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Select_IR)
            Case "Capture_IR".ToUpper
                MyDevice.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Capture_IR)
            Case "Shift_IR".ToUpper
                MyDevice.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR)
            Case "Exit1_IR".ToUpper
                MyDevice.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Exit1_IR)
            Case "Pause_IR".ToUpper
                MyDevice.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            Case "Exit2_IR".ToUpper
                MyDevice.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Exit2_IR)
            Case "Update_IR".ToUpper
                MyDevice.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Update_IR)
            Case Else
                Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "JTAG.State: unknown state: " & state_str}
        End Select
        Return Nothing
    End Function

    Private Function c_jtag_graycode(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim use_reserve As Boolean = False
        Dim table_ind As Integer = CInt(arguments(0).Value)
        If arguments.Length = 2 Then use_reserve = CBool(arguments(1).Value)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.UInteger)
        Utilities.CreateGrayCodeTable()
        If use_reserve Then
            sv.Value = Utilities.gray_code_table_reverse(table_ind)
        Else
            sv.Value = Utilities.gray_code_table(table_ind)
        End If
        Return sv
    End Function
    'Undocumented. This is for setting delays on FCUSB Classic EJTAG firmware
    Private Function c_jtag_setdelay(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim dev_ind As Int32 = CInt(arguments(0).Value)
        Dim delay_val As UInt32 = CUInt(CInt(arguments(1).Value))
        Select Case dev_ind
            Case 1 'Intel
                MyDevice.USB_CONTROL_MSG_OUT(USBREQ.JTAG_SET_OPTION, Nothing, (delay_val << 16) + 1UI)
            Case 2 'AMD
                MyDevice.USB_CONTROL_MSG_OUT(USBREQ.JTAG_SET_OPTION, Nothing, (delay_val << 16) + 2UI)
            Case 3 'DMA
                MyDevice.USB_CONTROL_MSG_OUT(USBREQ.JTAG_SET_OPTION, Nothing, (delay_val << 16) + 3UI)
        End Select
        Return Nothing
    End Function

    Private Function c_jtag_exitstate(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim exit_state As Boolean = CBool(arguments(0).Value)
        MyDevice.JTAG_IF.JSP.ExitStateMachine = exit_state
        If exit_state Then
            PrintConsole("SVF exit to test-logic-reset enabled")
        Else
            PrintConsole("SVF exit to test-logic-reset disabled")
        End If
        Return Nothing
    End Function

    Private Function c_jtag_epc2_read(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        ProgressBar_Percent(0)
        Dim cbProgress As New JTAG_IF.EPC2_ProgressCallback(AddressOf ProgressBar_Percent)
        Dim e_data() As Byte = MyDevice.JTAG_IF.EPC2_ReadBinary(cbProgress)
        ProgressBar_Percent(0)
        If e_data IsNot Nothing Then
            Dim sv As New ScriptVariable(CreateVarName(), DataType.Data)
            sv.Value = e_data
            Return sv
        End If
        Return Nothing
    End Function

    Private Function c_jtag_epc2_write(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim BootData() As Byte = CType(arguments(0).Value, Byte())
        Dim CfgData() As Byte = CType(arguments(1).Value, Byte())
        ProgressBar_Percent(0)
        Dim cbProgress As New JTAG_IF.EPC2_ProgressCallback(AddressOf ProgressBar_Percent)
        Dim result As Boolean = MyDevice.JTAG_IF.EPC2_WriteBinary(BootData, CfgData, cbProgress)
        ProgressBar_Percent(0)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Bool)
        sv.Value = result
        Return sv
    End Function

    Private Function c_jtag_epc2_erase(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim e_result As Boolean = MyDevice.JTAG_IF.EPC2_Erase()
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Bool)
        sv.Value = e_result
        Return sv
    End Function

#End Region

#Region "BSDL"

    Private Function c_bsdl_new(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Try
            Dim MyDevice = GetDevice()
            Dim mfg_name As String = CStr(arguments(0).Value)
            Dim part_name As String = CStr(arguments(1).Value)
            Dim package As String = ""
            If arguments.Length = 3 Then package = CStr(arguments(2).Value)
            Dim index_created As Integer = MyDevice.JTAG_IF.BSDL_Add(mfg_name, part_name, package)
            Return New ScriptVariable(CreateVarName(), DataType.Integer, index_created)
        Catch ex As Exception
        End Try
        Return Nothing
    End Function

    Private Function c_bsdl_find(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim part_name As String = CStr(arguments(0).Value)
        Dim package As String = ""
        If arguments.Length = 2 Then package = CStr(arguments(1).Value)
        Dim lib_ind As Integer = MyDevice.JTAG_IF.BSDL_Find(part_name, package)
        Return New ScriptVariable(CreateVarName(), DataType.Integer, lib_ind)
    End Function

    Private Function c_bsdl_param(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim param_name As String = CStr(arguments(0).Value)
        Dim param_value As UInt32 = CUInt(arguments(1).Value)
        Dim result As Boolean = MyDevice.JTAG_IF.BSDL_SetParamater(Index, param_name, param_value)
        Return New ScriptVariable(CreateVarName(), DataType.Bool, result)
    End Function

#End Region

#Region "Boundary Scan Programmer"

    Private Function c_bsp_setup(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If MyDevice Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "FlashcatUSB Professional must be connected via USB"}
        End If
        If Not MyDevice.HWBOARD = USB.FCUSB_BOARD.Professional_PCB5 Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "This command is only supported for FlashcatUSB Professional"}
        End If
        If Not MySettings.OPERATION_MODE = DeviceMode.JTAG Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "This command is only supported when in JTAG mode"}
        End If
        MyDevice.JTAG_IF.BoundaryScan_Setup()
        Return Nothing
    End Function

    Private Function c_bsp_init(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim result As Boolean = MyDevice.JTAG_IF.BoundaryScan_Init()
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Bool)
        sv.Value = result
        Return sv
    End Function

    Private Function c_bsp_addpin(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim pin_name As String = CStr(arguments(0).Value)
        Dim pin_output As Int16 = CShort(CInt(arguments(1).Value)) 'cell associated with the bidir or output cell
        Dim pin_control As Int16 = CShort(CInt(arguments(2).Value))  'cell associated with the control register bit
        Dim pin_input As Int16 = -1US 'cell associated with the input cell when output cell is not bidir
        If arguments.Length = 4 Then
            pin_input = CShort(CInt(arguments(3).Value))
        End If
        MyDevice.JTAG_IF.BoundaryScan_AddPin(pin_name, pin_output, pin_control, pin_input)
        Return Nothing
    End Function

    Private Function c_bsp_setbsr(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim pin_output As Int16 = CShort(CInt(arguments(0).Value))
        Dim pin_control As Int16 = CShort(CInt(arguments(1).Value))
        Dim pin_level As Boolean = CBool(arguments(2).Value)
        MyDevice.JTAG_IF.BoundaryScan_SetBSR(pin_output, pin_control, pin_level)
        Return Nothing
    End Function

    Private Function c_bsp_writebsr(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        MyDevice.JTAG_IF.BoundaryScan_WriteBSR()
        Return Nothing
    End Function

    Private Function c_bsp_detect(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim result As Boolean = MyDevice.JTAG_IF.BoundaryScan_Detect()
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Bool)
        sv.Value = result
        Return sv
    End Function

#End Region

#Region "LOAD"

    Friend Function c_load_firmware(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Select Case MyDevice.HWBOARD
            Case USB.FCUSB_BOARD.Professional_PCB5
            Case USB.FCUSB_BOARD.Mach1
            Case Else
                Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Only available for PRO or MACH1"}
        End Select
        MyDevice.USB_CONTROL_MSG_OUT(USBREQ.FW_REBOOT, Nothing, &HFFFFFFFFUI)
        Return Nothing
    End Function

    Friend Function c_load_logic(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Select Case MyDevice.HWBOARD
            Case USB.FCUSB_BOARD.Professional_PCB5
                MyDevice.LOGIC_SetVersion(&HFFFFFFFFUI)
                Logic.FCUSBPRO_LoadBitstream(MyDevice, MySettings.OPERATION_MODE, MySettings.VOLT_SELECT)
            Case USB.FCUSB_BOARD.Mach1
                MyDevice.LOGIC_SetVersion(&HFFFFFFFFUI)
                Logic.MACH1_Init(MyDevice, MySettings.OPERATION_MODE, MySettings.VOLT_SELECT)
            Case Else
                Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Only available for PRO or MACH1"}
        End Select
        Return Nothing
    End Function
    'Performs an erase of the logic
    Friend Function c_load_erase(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Select Case MyDevice.HWBOARD
            Case USB.FCUSB_BOARD.Mach1
                Logic.MACH1_EraseLogic(MyDevice)
        End Select
        Return Nothing
    End Function

    Friend Function c_load_bootloader(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim bl_data() As Byte = CType(arguments(0).Value, Byte())
        If bl_data IsNot Nothing AndAlso bl_data.Length > 0 Then
            If MyDevice.BootloaderUpdate(bl_data) Then
                PrintConsole("Bootloader successfully updated")
                MyDevice.USB_CONTROL_MSG_OUT(USB.USBREQ.FW_REBOOT, Nothing, &HFFFFFFFFUI)
            Else
                PrintConsole("Bootloader update was not successful")
            End If
        Else
            PrintConsole("Error: Load.Bootloader requires data variable with valid data")
        End If
        Return Nothing
    End Function
    'This allows you to write up to 64 bytes into the EEPROM (ATMEGA32u2/u4)
    Friend Function c_load_eeprom(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Dim result As Boolean = False
        Select Case MyDevice.HWBOARD
            Case USB.FCUSB_BOARD.Classic
            Case USB.FCUSB_BOARD.XPORT_PCB2
            Case Else
                Return New ScriptVariable("ERROR", DataType.Error, "Only available for Classic")
        End Select
        Dim bl_data() As Byte = CType(arguments(0).Value, Byte())
        Dim eeprom_addr As UInt32 = 0
        If bl_data IsNot Nothing AndAlso bl_data.Length > 0 AndAlso bl_data.Length <= 64 Then
            'If Not bl_data.Length = 64 Then ReDim Preserve bl_data(63)
            result = MyDevice.USB_CONTROL_MSG_OUT(USBREQ.EEPROM_WR, bl_data, eeprom_addr)
        Else
            PrintConsole("Error: Load.EEPROM requires data variable data (1-64 bytes)")
        End If
        Return New ScriptVariable(CreateVarName(), DataType.Bool, result)
    End Function

#End Region

#Region "READ"
    'This allows you to read up to 64 bytes from the EEPROM (ATMEGA32u2/u4)
    Friend Function c_read_eeprom(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        Select Case MyDevice.HWBOARD
            Case USB.FCUSB_BOARD.Classic
            Case USB.FCUSB_BOARD.XPORT_PCB2
            Case Else
                Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Only available for Classic"}
        End Select
        Dim size As Integer = 64
        Dim eeprom_addr As UInt32 = 0
        If arguments.Length > 0 Then size = CType(arguments(0).Value, Integer)
        Dim eeprom(size - 1) As Byte
        Dim result As Boolean = MyDevice.USB_CONTROL_MSG_IN(USBREQ.EEPROM_RD, eeprom, (CUInt(size) << 16) Or eeprom_addr)
        If Not result Then Return New ScriptVariable("ERROR", DataType.Error, "Unable to read EEPROM")
        Return New ScriptVariable(CreateVarName(), DataType.Data, eeprom)
    End Function

#End Region

#Region "FLASH commands"

    Friend Function c_norflash_parallel_add(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim flash_name As String = CStr(arguments(0).Value)
        Dim ID_MFG As Byte = CByte(CUInt(arguments(1).Value))
        Dim ID_PART As UInt16 = CUShort(CUInt(arguments(2).Value) And &HFFFFUI)
        Dim flash_size As UInt32 = CUInt(arguments(3).Value)
        Dim flash_if As VCC_IF = CType(CInt(arguments(4).Value), VCC_IF)
        Dim block_layout As BLKLYT = CType(CInt(arguments(5).Value), BLKLYT)
        Dim prog_mode As MFP_PRG = CType(CInt(arguments(6).Value), MFP_PRG)
        Dim delay_mode As MFP_DELAY = CType(CInt(arguments(7).Value), MFP_DELAY)
        Dim new_mem_part As New P_NOR(flash_name, ID_MFG, CUShort(ID_PART And &HFFFFUI), flash_size, flash_if, block_layout, prog_mode, delay_mode)
        'Delete existing entry
        Dim exist_device() As Device = FlashDatabase.FindDevices(ID_MFG, ID_PART, 0, MemoryType.PARALLEL_NOR)
        If exist_device IsNot Nothing Then
            For Each item In exist_device
                FlashDatabase.FlashDB.Remove(item)
            Next
        End If
        FlashDatabase.FlashDB.Add(new_mem_part)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Integer)
        sv.Value = FlashDatabase.FlashDB.Count - 1
        Return sv
    End Function

    Friend Function c_norflash_sector_add(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim device_index As Int32 = CInt(arguments(0).Value)
        Dim size As Int32 = CInt(arguments(1).Value)
        Dim d As Device = FlashDatabase.FlashDB(device_index)
        If d.GetType() Is GetType(P_NOR) Then
            Dim nor_flash As P_NOR = DirectCast(d, P_NOR)
            nor_flash.AddSector(size)
        End If
        Return Nothing
    End Function


#End Region

#Region "Parallel"

    Friend Function c_parallel_test(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If Not CURRENT_DEVICE_MODE = DeviceMode.PNOR Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Device is not in parallel operation mode"}
        ElseIf Not MyDevice.IS_CONNECTED Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "FlashcatUSB device is not connected"}
        End If
        Dim PNOR_IF As New PARALLEL_NOR(MyDevice)
        Dim td As New Thread(AddressOf PNOR_IF.PARALLEL_PORT_TEST)
        td.Start()
        Return Nothing
    End Function

    Friend Function c_parallel_command(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If Not CURRENT_DEVICE_MODE = DeviceMode.PNOR Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Device is not in parallel operation mode"}
        ElseIf Not MyDevice.IS_CONNECTED Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "FlashcatUSB device is not connected"}
        End If
        Dim PNOR_IF As New PARALLEL_NOR(MyDevice)
        Dim cmd_addr As UInt32 = CUInt(arguments(0).Value)
        Dim cmd_data As UInt16 = CUShort(CUInt(arguments(1).Value) And &HFFFF)
        PNOR_IF.WriteCommandData(cmd_addr, cmd_data)
        Return Nothing
    End Function

    Friend Function c_parallel_write(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If Not CURRENT_DEVICE_MODE = DeviceMode.PNOR Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Device is not in parallel operation mode"}
        ElseIf Not MyDevice.IS_CONNECTED Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "FlashcatUSB device is not connected"}
        End If
        Dim PNOR_IF As New PARALLEL_NOR(MyDevice)
        Dim cmd_addr As UInt32 = CUInt(arguments(0).Value)
        Dim cmd_data As UInt16 = CUShort(CUInt(arguments(1).Value) And &HFFFF)
        PNOR_IF.WriteMemoryAddress(cmd_addr, cmd_data)
        Return Nothing
    End Function

    Friend Function c_parallel_read(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim MyDevice = GetDevice()
        If Not CURRENT_DEVICE_MODE = DeviceMode.PNOR Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "Device is not in parallel operation mode"}
        ElseIf Not MyDevice.IS_CONNECTED Then
            Return New ScriptVariable("ERROR", DataType.Error) With {.Value = "FlashcatUSB device is not connected"}
        End If
        Dim PNOR_IF As New PARALLEL_NOR(MyDevice)
        Dim cmd_addr As UInt32 = CUInt(arguments(0).Value)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.UInteger)
        sv.Value = PNOR_IF.ReadMemoryAddress(cmd_addr)
        Return sv
    End Function

#End Region

    Private Delegate Sub UpdateFunction_Progress(index As Integer, percent As Integer)
    Private Delegate Sub UpdateFunction_Status(index As Integer, msg As String)

    Private Sub Callback_UpdatePercent(index As Integer, percent As Integer)
        MainApp.ProgressBar_Percent(percent)
    End Sub

    Private Sub Callback_UpdateStatus(index As Integer, task As String)
        MainApp.SetStatus(task)
    End Sub

    Private Function c_endian(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim endian_mode As String = arguments(0).Value.ToString.ToUpper
        Select Case endian_mode
            Case "MSB"
                MySettings.BIT_ENDIAN = Utilities.BitEndianMode.BigEndian32
            Case "LSB"
                MySettings.BIT_ENDIAN = Utilities.BitEndianMode.LittleEndian32_16bit
            Case "LSB16"
                MySettings.BIT_ENDIAN = Utilities.BitEndianMode.LittleEndian32_16bit
            Case "LSB8"
                MySettings.BIT_ENDIAN = Utilities.BitEndianMode.LittleEndian32_8bit
            Case Else
                MySettings.BIT_ENDIAN = Utilities.BitEndianMode.BigEndian32
        End Select
        Return Nothing
    End Function

    Private Function c_verify(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim verify_bool As Boolean = CBool(arguments(0).Value)
        MySettings.VERIFY_WRITE = verify_bool
        Return Nothing
    End Function

    Private Function c_mode(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Return New ScriptVariable(CreateVarName(), DataType.String, FlashcatSettings.DeviceModetoString(CURRENT_DEVICE_MODE))
    End Function

    Private Function c_refresh(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim count As Integer = MEM_IF.DeviceCount
        For i = 0 To count - 1
            Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(i)
            mem_device.Controls_Refresh()
        Next
        Return Nothing
    End Function

    'Reads the data from flash and verifies it (returns nothing on error)
    Private Function ReadMemoryVerify(address As UInt32, data_len As UInt32, index As FlashArea) As Byte()
        Dim cb As New MemoryDeviceInstance.StatusCallback
        cb.UpdatePercent = New UpdateFunction_Progress(AddressOf Callback_UpdatePercent)
        cb.UpdateTask = New UpdateFunction_Status(AddressOf Callback_UpdateStatus)
        Dim memDev As MemoryDeviceInstance = MEM_IF.GetDevice(index)
        Dim FlashData1() As Byte = memDev.ReadBytes(address, data_len, cb)
        If FlashData1 Is Nothing Then Return Nothing
        Dim FlashData2() As Byte = memDev.ReadBytes(address, data_len, cb)
        If FlashData2 Is Nothing Then Return Nothing
        If Not FlashData1.Length = FlashData2.Length Then Return Nothing 'Error already?
        If FlashData1.Length = 0 Then Return Nothing
        If FlashData2.Length = 0 Then Return Nothing
        Dim DataWords1() As UInt32 = Utilities.Bytes.ToUIntArray(FlashData1) 'This is the one corrected
        Dim DataWords2() As UInt32 = Utilities.Bytes.ToUIntArray(FlashData2)
        Dim Counter As Integer
        Dim CheckAddr, CheckValue, CheckArray() As UInt32
        Dim Data() As Byte
        Dim ErrCount As Integer = 0
        For Counter = 0 To DataWords1.Length - 1
            If Not DataWords1(Counter) = DataWords2(Counter) Then
                If ErrCount = 100 Then Return Nothing 'Too many errors
                ErrCount = ErrCount + 1
                CheckAddr = CUInt(address + (Counter * 4)) 'Address to verify
                Data = memDev.ReadBytes(CheckAddr, 4)
                CheckArray = Utilities.Bytes.ToUIntArray(Data) 'Will only read one element
                CheckValue = CheckArray(0)
                If DataWords1(Counter) = CheckValue Then 'Our original data matched
                ElseIf DataWords2(Counter) = CheckValue Then 'Our original was incorrect
                    DataWords1(Counter) = DataWords2(Counter)
                Else
                    Return Nothing '3 reads of the same data did not match, return error!
                End If
            End If
        Next
        Dim DataOut() As Byte = Utilities.Bytes.FromUint32Array(DataWords1)
        ReDim Preserve DataOut(FlashData1.Length - 1)
        Return DataOut 'Checked ok!
    End Function

    Private Function CreateVarName() As String
        Return ScriptProcessor.CurrentVars.GetNewName()
    End Function

End Class
