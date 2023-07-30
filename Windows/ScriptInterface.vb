Imports System.Threading
Imports FlashcatUSB.EC_ScriptEngine
Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.JTAG
Imports FlashcatUSB.MemoryInterface

Public Class ScriptInterface

    Public Event PrintConsole(msg As String)
    Public Event SetStatus(msg As String)

    Public Property CURRENT_DEVICE_MODE As DeviceMode

    Public WithEvents ScriptProcessor As New EC_ScriptEngine.Processor

    Delegate Function ScriptFunction(arguments() As ScriptVariable, Index As Int32) As ScriptVariable

    Public Sub PrintInformation()
        RaiseEvent PrintConsole("FlashcatUSB Script Engine build: " & EC_ScriptEngine.Processor.Build)
    End Sub

    Sub New()
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
        ScriptProcessor.AddScriptNest(MEM_CMD)
        Dim TAB_CMD As New ScriptCmd("TAB")
        TAB_CMD.Add("create", {CmdPrm.String}, New ScriptFunction(AddressOf c_tab_create))
        TAB_CMD.Add("addgroup", {CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addgroup))
        TAB_CMD.Add("addbox", {CmdPrm.String, CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addbox))
        TAB_CMD.Add("addtext", {CmdPrm.String, CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addtext))
        TAB_CMD.Add("addimage", {CmdPrm.String, CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addimage))
        TAB_CMD.Add("addbutton", {CmdPrm.String, CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addbutton))
        TAB_CMD.Add("addprogress", {CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer}, New ScriptFunction(AddressOf c_tab_addprogress))
        TAB_CMD.Add("remove", {CmdPrm.String}, New ScriptFunction(AddressOf c_tab_remove))
        TAB_CMD.Add("settext", {CmdPrm.String, CmdPrm.String}, New ScriptFunction(AddressOf c_tab_settext))
        TAB_CMD.Add("gettext", {CmdPrm.String}, New ScriptFunction(AddressOf c_tab_gettext))
        TAB_CMD.Add("buttondisable", {CmdPrm.String_Optional}, New ScriptFunction(AddressOf c_tab_buttondisable))
        TAB_CMD.Add("buttonenable", {CmdPrm.String_Optional}, New ScriptFunction(AddressOf c_tab_buttonenable))
        ScriptProcessor.AddScriptNest(TAB_CMD)
        Dim SPI_CMD As New ScriptCmd("SPI")
        SPI_CMD.Add("clock", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_spi_clock))
        SPI_CMD.Add("mode", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_spi_mode))
        SPI_CMD.Add("database", {CmdPrm.Bool_Optional}, New ScriptFunction(AddressOf c_spi_database))
        SPI_CMD.Add("getsr", {CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_spi_getsr))
        SPI_CMD.Add("setsr", {CmdPrm.Data}, New ScriptFunction(AddressOf c_spi_setsr))
        SPI_CMD.Add("writeread", {CmdPrm.Data, CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_spi_writeread))
        SPI_CMD.Add("prog", {CmdPrm.Integer}, New ScriptFunction(AddressOf c_spi_prog)) 'Undocumented
        ScriptProcessor.AddScriptNest(SPI_CMD)
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
        ScriptProcessor.AddScriptNest(JTAG_CMD)
        Dim BSDL_CMD As New ScriptCmd("BSDL")
        BSDL_CMD.Add("new", {CmdPrm.String}, New ScriptFunction(AddressOf c_bsdl_new))
        BSDL_CMD.Add("find", {CmdPrm.String}, New ScriptFunction(AddressOf c_bsdl_find))
        BSDL_CMD.Add("parameter", {CmdPrm.String, CmdPrm.UInteger}, New ScriptFunction(AddressOf c_bsdl_param))
        ScriptProcessor.AddScriptNest(BSDL_CMD)
        Dim JTAG_BSP As New ScriptCmd("BoundaryScan")
        JTAG_BSP.Add("setup", Nothing, New ScriptFunction(AddressOf c_bsp_setup))
        JTAG_BSP.Add("init", {CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_bsp_init))
        JTAG_BSP.Add("addpin", {CmdPrm.String, CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Integer_Optional}, New ScriptFunction(AddressOf c_bsp_addpin))
        JTAG_BSP.Add("setbsr", {CmdPrm.Integer, CmdPrm.Integer, CmdPrm.Bool}, New ScriptFunction(AddressOf c_bsp_setbsr))
        JTAG_BSP.Add("writebsr", Nothing, New ScriptFunction(AddressOf c_bsp_writebsr))
        JTAG_BSP.Add("detect", Nothing, New ScriptFunction(AddressOf c_bsp_detect))
        ScriptProcessor.AddScriptNest(JTAG_BSP)
        Dim PAR_CMD As New ScriptCmd("parallel")
        PAR_CMD.Add("test", Nothing, New ScriptFunction(AddressOf c_parallel_test))
        PAR_CMD.Add("command", {CmdPrm.UInteger, CmdPrm.UInteger}, New ScriptFunction(AddressOf c_parallel_command))
        PAR_CMD.Add("write", {CmdPrm.UInteger, CmdPrm.UInteger}, New ScriptFunction(AddressOf c_parallel_write))
        PAR_CMD.Add("read", {CmdPrm.UInteger}, New ScriptFunction(AddressOf c_parallel_read))
        ScriptProcessor.AddScriptNest(PAR_CMD)
        Dim LOADOPT As New ScriptCmd("load") 'Undocumented
        LOADOPT.Add("firmware", Nothing, New ScriptFunction(AddressOf c_load_firmware))
        LOADOPT.Add("logic", Nothing, New ScriptFunction(AddressOf c_load_logic))
        LOADOPT.Add("erase", Nothing, New ScriptFunction(AddressOf c_load_erase))
        LOADOPT.Add("bootloader", {CmdPrm.Data}, New ScriptFunction(AddressOf c_load_bootloader))
        ScriptProcessor.AddScriptNest(LOADOPT)
        Dim FLSHOPT As New ScriptCmd("flash") 'Undocumented
        Dim del_add As New ScriptFunction(AddressOf c_flash_add)
        FLSHOPT.Add("add", {CmdPrm.String, CmdPrm.UInteger, CmdPrm.UInteger, CmdPrm.UInteger, CmdPrm.UInteger, CmdPrm.UInteger, CmdPrm.UInteger, CmdPrm.UInteger}, del_add)
        ScriptProcessor.AddScriptNest(FLSHOPT)

        ScriptProcessor.AddScriptCommand("endian", {CmdPrm.String}, New ScriptFunction(AddressOf c_endian))
        ScriptProcessor.AddScriptCommand("verify", {CmdPrm.Bool}, New ScriptFunction(AddressOf c_verify))
        ScriptProcessor.AddScriptCommand("mode", Nothing, New ScriptFunction(AddressOf c_mode))
        ScriptProcessor.AddScriptCommand("refresh", Nothing, New ScriptFunction(AddressOf c_refresh))

    End Sub

    Public Sub Unload()
        For i = 0 To OurFlashDevices.Count - 1
            Dim this_devie As MemoryDeviceInstance = OurFlashDevices(i)
            If GUI IsNot Nothing Then GUI.RemoveTab(this_devie)
            MEM_IF.Remove(this_devie)
            Application.DoEvents()
        Next
        If GUI IsNot Nothing Then GUI.RemoveUserTabs()
        OurFlashDevices.Clear()
        UserTabCount = 0
    End Sub

    Private Function CreateVarName() As String
        Return ScriptProcessor.CurrentVars.GetNewName()
    End Function

    Private Sub ScriptProcessor_PrintConsole(msg As String) Handles ScriptProcessor.PrintConsole
        RaiseEvent PrintConsole(msg)
    End Sub

    Private Sub ScriptProcessor_SetStatus(msg As String) Handles ScriptProcessor.SetStatus
        RaiseEvent SetStatus(msg)
    End Sub


#Region "Memory commands"

    Private Function c_mem_name(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim name_out As String = MEM_IF.GetDevice(CInt(Index)).Name
        Dim sv As New ScriptVariable(CreateVarName(), DataType.String)
        sv.Value = name_out
        Return sv
    End Function

    Private Function c_mem_size(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If (MEM_IF.GetDevice(CInt(Index)).Size > CLng(Int32.MaxValue)) Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device is larger than 2GB"}
        End If
        Dim size_value As Int32 = CInt(MEM_IF.GetDevice(CInt(Index)).Size)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Integer)
        sv.Value = size_value
        Return sv
    End Function

    Private Function c_mem_write(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim data_to_write() As Byte = CType(arguments(0).Value, Byte())
        Dim offset As UInt32 = CUInt(arguments(1).Value)
        Dim data_len As Int32 = data_to_write.Length
        If (arguments.Length > 2) Then data_len = CInt(arguments(2).Value)
        ReDim Preserve data_to_write(data_len - 1)
        ProgressBar_Percent(0)
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device not connected"}
        End If
        Dim cb As New MemoryDeviceInstance.StatusCallback
        cb.UpdatePercent = New UpdateFunction_Progress(AddressOf MainApp.ProgressBar_Percent)
        cb.UpdateTask = New UpdateFunction_Status(AddressOf MainApp.SetStatus)
        MEM_IF.GetDevice(CInt(Index)).DisableGuiControls()
        MEM_IF.GetDevice(CInt(Index)).FCUSB.USB_LEDBlink()
        Try
            Dim mem_dev As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
            Dim write_result As Boolean = mem_dev.WriteBytes(offset, data_to_write, MySettings.VERIFY_WRITE, cb)
            If write_result Then
                RaiseEvent PrintConsole("Sucessfully programmed " & data_len.ToString("N0") & " bytes")
            Else
                RaiseEvent PrintConsole("Canceled memory write operation")
            End If
        Catch ex As Exception
        Finally
            MEM_IF.GetDevice(CInt(Index)).EnableGuiControls()
            MEM_IF.GetDevice(CInt(Index)).FCUSB.USB_LEDOn()
            ProgressBar_Percent(0)
        End Try
        Return Nothing
    End Function

    Private Function c_mem_read(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device not connected"}
        End If
        Dim offset As UInt32 = CUInt(arguments(0).Value)
        Dim count As Int32 = CInt(arguments(1).Value)
        Dim display As Boolean = True
        If (arguments.Length > 2) Then display = CBool(arguments(2).Value)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Data)
        Dim cb As New MemoryDeviceInstance.StatusCallback
        If display Then
            cb.UpdatePercent = New UpdateFunction_Progress(AddressOf MainApp.ProgressBar_Percent)
            cb.UpdateTask = New UpdateFunction_Status(AddressOf MainApp.SetStatus)
        End If
        mem_device.DisableGuiControls()
        Try
            ProgressBar_Percent(0)
            Dim data_read() As Byte = Nothing
            data_read = mem_device.ReadBytes(offset, count, cb)
            sv.Value = data_read
        Catch ex As Exception
        Finally
            mem_device.EnableGuiControls()
        End Try
        ProgressBar_Percent(0)
        Return sv
    End Function

    Private Function c_mem_wait(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device not connected"}
        End If
        mem_device.WaitUntilReady()
        Return Nothing
    End Function

    Private Function c_mem_readstring(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device not connected"}
        End If
        Dim offset As UInt32 = CUInt(arguments(0).Value)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.String)
        Dim FlashSize As UInt32 = CUInt(mem_device.Size)
        If offset + 1 > FlashSize Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Offset is greater than flash size"}
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
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device not connected"}
        End If
        Dim FlashAddress As UInt32 = CUInt(arguments(0).Value)
        Dim FlashLen As Int32 = CInt(arguments(1).Value)
        Dim data() As Byte = Nothing
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Data)
        ProgressBar_Percent(0)
        mem_device.DisableGuiControls()
        Try
            data = ReadMemoryVerify(FlashAddress, CUInt(FlashLen), CType(Index, FlashArea))
        Catch ex As Exception
        Finally
            mem_device.EnableGuiControls()
            ProgressBar_Percent(0)
        End Try
        If data Is Nothing Then
            RaiseEvent PrintConsole("Read operation failed")
            Return Nothing
        End If
        sv.Value = data
        Return sv
    End Function

    Private Function c_mem_sectorcount(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device not connected"}
        End If
        Dim sector_count As Int32 = CInt(mem_device.GetSectorCount)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Integer)
        sv.Value = sector_count
        Return sv
    End Function

    Private Function c_mem_sectorsize(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device not connected"}
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
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device not connected"}
        End If
        Dim mem_sector As Int32 = CInt(arguments(0).Value)
        mem_device.EraseSector(mem_sector)
        Dim target_addr As Long = mem_device.GetSectorBaseAddress(mem_sector)
        Dim target_area As String = "0x" & Hex(target_addr).PadLeft(8, "0"c) & " to 0x" & Hex(target_addr + mem_device.GetSectorSize(mem_sector) - 1).PadLeft(8, "0"c)
        If mem_device.NoErrors Then
            RaiseEvent PrintConsole("Successfully erased sector index: " & mem_sector & " (" & target_area & ")")
        Else
            RaiseEvent PrintConsole("Failed to erase sector index: " & mem_sector & " (" & target_area & ")")
        End If
        mem_device.GuiControl.RefreshView()
        mem_device.ReadMode()
        Return Nothing
    End Function

    Private Function c_mem_erasebulk(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(CInt(Index))
        If mem_device Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Memory device not connected"}
        End If
        Try
            MEM_IF.GetDevice(CInt(Index)).DisableGuiControls()
            mem_device.EraseFlash()
        Catch ex As Exception
        Finally
            MEM_IF.GetDevice(CInt(Index)).EnableGuiControls()
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

#Region "TAB commands"

    Private Function c_tab_create(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim tab_name As String = CStr(arguments(0).Value)
        GUI.CreateFormTab(UserTabCount, " " & tab_name & " ") 'Thread-Safe
        UserTabCount = UserTabCount + 1
        Dim sv As New ScriptVariable(CreateVarName(), DataType.UInteger)
        sv.Value = UserTabCount - 1
        Return sv
    End Function

    Private Function c_tab_addgroup(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim NewGroup As New GroupBox
        NewGroup.Name = CStr(arguments(0).Value)
        NewGroup.Text = CStr(arguments(0).Value)
        NewGroup.Left = CInt(arguments(1).Value)
        NewGroup.Top = CInt(arguments(2).Value)
        NewGroup.Width = CInt(arguments(3).Value)
        NewGroup.Height = CInt(arguments(4).Value)
        GUI.AddControlToTable(CInt(Index), NewGroup)
        Return Nothing
    End Function

    Private Function c_tab_addbox(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim NewTextBox As New TextBox
        NewTextBox.Name = CStr(arguments(0).Value)
        NewTextBox.Text = CStr(arguments(1).Value)
        NewTextBox.Width = (NewTextBox.Text.Length * 8) + 2
        NewTextBox.TextAlign = HorizontalAlignment.Center
        NewTextBox.Left = CInt(arguments(2).Value)
        NewTextBox.Top = CInt(arguments(3).Value)
        GUI.AddControlToTable(CInt(Index), NewTextBox)
        Return Nothing
    End Function

    Private Function c_tab_addtext(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim NewTextLabel As New Label
        NewTextLabel.AutoSize = True
        NewTextLabel.Name = CStr(arguments(0).Value)
        NewTextLabel.Text = CStr(arguments(1).Value)
        NewTextLabel.Width = (NewTextLabel.Text.Length * 7)
        NewTextLabel.Left = CInt(arguments(2).Value)
        NewTextLabel.Top = CInt(arguments(3).Value)
        NewTextLabel.BringToFront()
        GUI.AddControlToTable(CInt(Index), NewTextLabel)
        Return Nothing
    End Function

    Private Function c_tab_addimage(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim filen As String = CStr(arguments(1).Value)
        Dim finfo As New IO.FileInfo(ScriptPath & filen)
        If Not finfo.Exists Then RaiseEvent PrintConsole("Tab.AddImage, specified image not found: " & filen) : Return Nothing
        Dim newImage As Image = Image.FromFile(finfo.FullName)
        Dim NewPB As New PictureBox
        NewPB.Name = CStr(arguments(0).Value)
        NewPB.Image = newImage
        NewPB.Left = CInt(arguments(2).Value)
        NewPB.Top = CInt(arguments(3).Value)
        NewPB.Width = newImage.Width + 5
        NewPB.Height = newImage.Height + 5
        NewPB.BringToFront() 'does not work
        GUI.AddControlToTable(CInt(Index), NewPB)
        Return Nothing
    End Function

    Private Function c_tab_addbutton(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim NewButton As New Button
        NewButton.AutoSize = True
        NewButton.Name = CStr(arguments(0).Value)
        NewButton.Text = CStr(arguments(1).Value)
        AddHandler NewButton.Click, AddressOf ButtonHandler
        NewButton.Left = CInt(arguments(2).Value)
        NewButton.Top = CInt(arguments(3).Value)
        NewButton.BringToFront() 'does not work
        GUI.AddControlToTable(CInt(Index), NewButton)
        Return Nothing
    End Function

    Private Function c_tab_addprogress(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim bar_left As Integer = CInt(arguments(0).Value)
        Dim bar_top As Integer = CInt(arguments(1).Value)
        Dim bar_width As Integer = CInt(arguments(2).Value)
        MainApp.ProgressBar_Add(CInt(Index), bar_left, bar_top, bar_width)
        Return Nothing
    End Function

    Private Function c_tab_remove(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim item_name As String = CStr(arguments(0).Value)
        RemoveUserControl(item_name)
        Return Nothing
    End Function

    Private Function c_tab_settext(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim ctrl_name As String = CStr(arguments(0).Value)
        Dim new_text As String = CStr(arguments(1).Value)
        GUI.SetControlText(CInt(Index), ctrl_name, new_text)
        Return Nothing
    End Function

    Private Function c_tab_gettext(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim ctrl_name As String = CStr(arguments(0).Value)
        Dim result_str As String = GUI.GetControlText(CInt(Index), ctrl_name)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.String)
        sv.Value = result_str
        Return sv
        Return Nothing
    End Function

    Private Function c_tab_buttondisable(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim specific_button As String = ""
        If specific_button.Length = 1 Then
            specific_button = CStr(arguments(0).Value)
        End If
        GUI.HandleButtons(CInt(Index), False, specific_button)
        Return Nothing
    End Function

    Private Function c_tab_buttonenable(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim specific_button As String = ""
        If specific_button.Length = 1 Then
            specific_button = CStr(arguments(0).Value)
        End If
        GUI.HandleButtons(CInt(Index), True, specific_button)
        Return Nothing
    End Function

#End Region

#Region "SPI commands"

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
        RaiseEvent PrintConsole("The internal Flash database consists of " & FlashDatabase.FlashDB.Count & " devices")
        For Each device In FlashDatabase.FlashDB
            If device.FLASH_TYPE = FlashMemory.MemoryType.SERIAL_NOR Then
                Dim size_str As String = ""
                Dim size_int As UInt32 = CUInt(device.FLASH_SIZE)
                If (size_int < 128) Then
                    size_str = (size_int / 8) & "bits"
                ElseIf (size_int < 131072) Then
                    size_str = (size_int / 128) & "Kbits"
                Else
                    size_str = (size_int / 131072) & "Mbits"
                End If
                If DisplayJedecID Then
                    Dim jedec_str As String = Hex(device.MFG_CODE).PadLeft(2, "0"c) & Hex(device.ID1).PadLeft(4, "0"c)
                    If (jedec_str = "000000") Then
                        RaiseEvent PrintConsole(device.NAME & " (" & size_str & ") EEPROM")
                    Else
                        RaiseEvent PrintConsole(device.NAME & " (" & size_str & ") JEDEC: 0x" & jedec_str)
                    End If
                Else
                    RaiseEvent PrintConsole(device.NAME & " (" & size_str & ")")
                End If
            End If
        Next
        RaiseEvent PrintConsole("SPI Flash database list complete")
        Return Nothing
    End Function

    Private Function c_spi_getsr(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If Me.CURRENT_DEVICE_MODE = DeviceMode.SPI Then
        ElseIf Me.CURRENT_DEVICE_MODE = DeviceMode.SQI Then
        Else
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Device is not in SPI/QUAD operation mode"}
        End If
        If Not MAIN_FCUSB.IS_CONNECTED Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "FlashcatUSB device is not connected"}
        End If
        Dim bytes_to_read As Int32 = 1
        If arguments.Length > 0 Then bytes_to_read = CInt(arguments(0).Value)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Data)
        If Me.CURRENT_DEVICE_MODE = DeviceMode.SPI Then
            sv.Value = MAIN_FCUSB.SPI_NOR_IF.ReadStatusRegister(bytes_to_read)
        ElseIf Me.CURRENT_DEVICE_MODE = DeviceMode.SQI Then
            sv.Value = MAIN_FCUSB.SQI_NOR_IF.ReadStatusRegister(bytes_to_read)
        End If
        Return sv
    End Function

    Private Function c_spi_setsr(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If Me.CURRENT_DEVICE_MODE = DeviceMode.SPI Then
        ElseIf Me.CURRENT_DEVICE_MODE = DeviceMode.SQI Then
        Else
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Device is not in SPI/QUAD operation mode"}
        End If
        If Not MAIN_FCUSB.IS_CONNECTED Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "FlashcatUSB device is not connected"}
        End If
        Dim data_out() As Byte = CType(arguments(0).Value, Byte())
        If Me.CURRENT_DEVICE_MODE = DeviceMode.SPI Then
            MAIN_FCUSB.SPI_NOR_IF.WriteStatusRegister(data_out)
        ElseIf Me.CURRENT_DEVICE_MODE = DeviceMode.SQI Then
            MAIN_FCUSB.SQI_NOR_IF.WriteStatusRegister(data_out)
        End If
        Return Nothing
    End Function

    Private Function c_spi_writeread(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If Not Me.CURRENT_DEVICE_MODE = DeviceMode.SPI Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Device is not in SPI operation mode"}
        End If
        If Not MAIN_FCUSB.IS_CONNECTED Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "FlashcatUSB device is not connected"}
        End If
        Dim DataToWrite() As Byte = CType(arguments(0).Value, Byte())
        Dim ReadBack As Int32 = 0
        If arguments.Length = 2 Then ReadBack = CInt(arguments(1).Value)
        If ReadBack = 0 Then
            MAIN_FCUSB.SPI_NOR_IF.SPIBUS_WriteRead(DataToWrite)
            Return Nothing
        Else
            Dim return_data(ReadBack - 1) As Byte
            MAIN_FCUSB.SPI_NOR_IF.SPIBUS_WriteRead(DataToWrite, return_data)
            Dim sv As New ScriptVariable(CreateVarName(), DataType.Data)
            sv.Value = return_data
            Return sv
        End If
    End Function

    Private Function c_spi_prog(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim spi_port As SPI.SPI_Programmer = MAIN_FCUSB.SPI_NOR_IF
        Dim state As Integer = CInt(arguments(0).Value)
        If state = 1 Then 'Set the PROGPIN to HIGH
            spi_port.SetProgPin(True)
        Else 'Set the PROGPIN to LOW
            spi_port.SetProgPin(False)
        End If
        Return Nothing
    End Function

#End Region

#Region "JTAG"

    Private Function c_jtag_idcode(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim sv As New ScriptVariable(CreateVarName(), DataType.UInteger)
        Dim current_index As Integer = MAIN_FCUSB.JTAG_IF.Chain_SelectedIndex
        sv.Value = MAIN_FCUSB.JTAG_IF.Devices(current_index).IDCODE
        Return sv
    End Function

    Private Function c_jtag_config(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If arguments.Length = 1 Then
            Select Case CStr(arguments(0).Value).ToUpper
                Case "MIPS"
                    MAIN_FCUSB.JTAG_IF.Configure(JTAG.PROCESSOR.MIPS)
                Case "ARM"
                    MAIN_FCUSB.JTAG_IF.Configure(JTAG.PROCESSOR.ARM)
                Case Else
                    Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Unknown mode: " & CStr(arguments(0).Value)}
            End Select
        Else
            MAIN_FCUSB.JTAG_IF.Configure(JTAG.PROCESSOR.NONE)
        End If
        Return Nothing
    End Function

    Private Function c_jtag_select(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not currently valid"}
        End If
        Dim select_index As Integer = CInt(arguments(0).Value)
        If MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
            MAIN_FCUSB.JTAG_IF.Chain_Select(CInt(Index))
            Return New ScriptVariable(CreateVarName(), DataType.Bool, True)
        Else
            RaiseEvent PrintConsole("JTAG chain is not valid, not all devices have BSDL loaded")
        End If
        Return New ScriptVariable(CreateVarName(), DataType.Bool, False)
    End Function

    Private Function c_jtag_print(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        MAIN_FCUSB.JTAG_IF.Chain_Print()
        Return Nothing
    End Function

    Private Function c_jtag_clear(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        MAIN_FCUSB.JTAG_IF.Chain_Clear()
        Return Nothing
    End Function

    Private Function c_jtag_set(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim jtag_device_index As Int32 = CInt(arguments(0).Value)
        Dim bsdl_name As String = CStr(arguments(1).Value)
        If MAIN_FCUSB.JTAG_IF.Chain_Set(jtag_device_index, bsdl_name) Then
            RaiseEvent PrintConsole("Successful set chain index " & jtag_device_index.ToString & " to " & bsdl_name)
        Else
            RaiseEvent PrintConsole("Error: unable to find internal BSDL device with name " & bsdl_name)
        End If
        Return Nothing
    End Function

    Private Function c_jtag_add(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim bsdl_lib As String = CStr(arguments(0).Value)
        If MAIN_FCUSB.JTAG_IF.Chain_Add(bsdl_lib) Then
            RaiseEvent PrintConsole("Successful added BSDL to JTAG chain")
        Else
            RaiseEvent PrintConsole("Error: BSDL library " & bsdl_lib & " not found")
        End If
        Return Nothing
    End Function

    Private Function c_jtag_validate(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If MAIN_FCUSB.JTAG_IF.Chain_Validate() Then
            RaiseEvent PrintConsole("JTAG chain is valid")
        Else
            RaiseEvent PrintConsole("JTAG chain is invalid")
        End If
        Return Nothing
    End Function

    Private Function c_jtag_control(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim control_value As UInt32 = CUInt(arguments(0).Value)
        Dim j As BSDL_DEF = MAIN_FCUSB.JTAG_IF.Chain_Get(MAIN_FCUSB.JTAG_IF.Chain_SelectedIndex)
        If j IsNot Nothing Then
            Dim result As UInt32 = MAIN_FCUSB.JTAG_IF.AccessDataRegister32(j.MIPS_CONTROL, control_value)
            RaiseEvent PrintConsole("JTAT CONTROL command issued: 0x" & Hex(control_value) & " result: 0x" & Hex(result))
            Dim sv As New ScriptVariable(CreateVarName(), DataType.UInteger)
            sv.Value = result
            Return sv
        End If
        Return Nothing
    End Function

    Private Function c_jtag_memoryinit(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim flash_type As String = CStr(arguments(0).Value)
        Dim new_dev As MemoryDeviceInstance = Nothing
        Select Case flash_type.ToUpper
            Case "CFI"
                Dim base_address As UInt32 = CUInt(arguments(1).Value)
                RaiseEvent PrintConsole(String.Format(RM.GetString("jtag_cfi_attempt_detect"), Hex(base_address).PadLeft(8, "0"c)))
                If MAIN_FCUSB.JTAG_IF.CFI_Detect(base_address) Then
                    new_dev = Connected_Event(MAIN_FCUSB, 16384)
                Else
                    RaiseEvent PrintConsole(RM.GetString("jtag_cfi_no_detect"))
                End If
            Case "SPI"
                RaiseEvent PrintConsole(RM.GetString("jtag_spi_attempt_detect"))
                If MAIN_FCUSB.JTAG_IF.SPI_Detect(CType(CUInt(arguments(1).Value), JTAG_IF.JTAG_SPI_Type)) Then
                    new_dev = Connected_Event(MAIN_FCUSB, 16384)
                Else
                    RaiseEvent PrintConsole(RM.GetString("jtag_spi_no_detect")) '"Error: unable to detect SPI flash device over JTAG"
                End If
            Case Else
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Error in JTAG.MemoryInit: device type not specified"}
        End Select
        If new_dev IsNot Nothing Then
            OurFlashDevices.Add(new_dev)
            Return New ScriptVariable(CreateVarName(), DataType.UInteger) With {.Value = (OurFlashDevices.Count - 1)}
        Else
            RaiseEvent PrintConsole("JTAG.MemoryInit: failed to create new memory device interface")
        End If
        Return Nothing
    End Function

    Private Function c_jtag_debug(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim enable As Boolean = CBool(arguments(0).Value)
        If enable Then
            MAIN_FCUSB.JTAG_IF.EJTAG_Debug_Enable()
        Else
            MAIN_FCUSB.JTAG_IF.EJTAG_Debug_Disable()
        End If
        Return Nothing
    End Function

    Private Function c_jtag_cpureset(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        MAIN_FCUSB.JTAG_IF.EJTAG_Reset()
        Return Nothing
    End Function

    Private Function c_jtag_runsvf(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not currently valid"}
        End If
        ProgressBar_Percent(0)
        RemoveHandler MAIN_FCUSB.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
        AddHandler MAIN_FCUSB.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
        RaiseEvent PrintConsole("Running SVF file in internal JTAG SVF player")
        Dim DataBytes() As Byte = CType(arguments(0).Value, Byte())
        Dim FileStr() As String = Utilities.Bytes.ToCharStringArray(DataBytes)
        Dim result As Boolean = MAIN_FCUSB.JTAG_IF.JSP.RunFile_SVF(FileStr)
        If result Then
            RaiseEvent PrintConsole("SVF file successfully played")
        Else
            RaiseEvent PrintConsole("Error playing the SVF file")
        End If
        ProgressBar_Percent(0)
        RemoveHandler MAIN_FCUSB.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Bool)
        sv.Value = result
        Return sv
    End Function

    Private Function c_jtag_runxsvf(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not currently valid"}
        End If
        ProgressBar_Percent(0)
        RemoveHandler MAIN_FCUSB.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
        AddHandler MAIN_FCUSB.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
        RaiseEvent PrintConsole("Running XSVF file in internal JTAG XSVF player")
        Dim DataBytes() As Byte = CType(arguments(0).Value, Byte())
        Dim result As Boolean = MAIN_FCUSB.JTAG_IF.JSP.RunFile_XSVF(DataBytes)
        If result Then
            RaiseEvent PrintConsole("XSVF file successfully played")
        Else
            RaiseEvent PrintConsole("Error playing the XSVF file")
        End If
        ProgressBar_Percent(0)
        RemoveHandler MAIN_FCUSB.JTAG_IF.JSP.Progress, AddressOf ProgressBar_Percent
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Bool)
        sv.Value = result
        Return sv
    End Function

    Private Function c_jtag_shiftdr(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not currently valid"}
        End If
        Dim exit_mode As Boolean = True
        Dim data_in() As Byte = CType(arguments(0).Value, Byte())
        Dim bit_count As Integer = CInt(arguments(1).Value)
        Dim data_out() As Byte = Nothing
        If arguments.Length = 3 Then exit_mode = CBool(arguments(1).Value)
        MAIN_FCUSB.JTAG_IF.JSP_ShiftDR(data_in, data_out, CUShort(bit_count), exit_mode)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Data)
        sv.Value = data_out
        Return sv
    End Function

    Private Function c_jtag_shiftir(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not currently valid"}
        End If
        Dim exit_mode As Boolean = True
        Dim data_in() As Byte = CType(arguments(0).Value, Byte())
        If arguments.Length = 2 Then exit_mode = CBool(arguments(1).Value)
        Dim ir_size As Int16 = CShort(MAIN_FCUSB.JTAG_IF.GetSelected_IRLength())
        MAIN_FCUSB.JTAG_IF.JSP_ShiftIR(data_in, Nothing, ir_size, exit_mode)
        Return Nothing
    End Function

    Private Function c_jtag_shiftout(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not currently valid"}
        End If
        Dim tdi_data() As Byte = CType(arguments(0).Value, Byte())
        Dim bit_count As Integer = CInt(arguments(1).Value)
        Dim exit_tms As Boolean = True
        If arguments.Length = 3 Then exit_tms = CBool(arguments(2).Value)
        Dim tdo_data() As Byte = Nothing
        MAIN_FCUSB.JTAG_IF.ShiftTDI(CUInt(bit_count), tdi_data, tdo_data, exit_tms)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Data)
        sv.Value = tdo_data
        Return sv
    End Function

    Private Function c_jtag_tapreset(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not currently valid"}
        End If
        MAIN_FCUSB.JTAG_IF.Reset_StateMachine()
        Return Nothing
    End Function

    Private Function c_jtag_write32(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not currently valid"}
        End If
        Dim addr32 As UInt32 = CUInt(arguments(0).Value)
        Dim data As UInt32 = CUInt(arguments(1).Value)
        MAIN_FCUSB.JTAG_IF.WriteMemory(addr32, data, DATA_WIDTH.Word)
        Return Nothing
    End Function

    Private Function c_jtag_read32(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not currently valid"}
        End If
        Dim addr32 As UInt32 = CUInt(arguments(0).Value)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.UInteger)
        sv.Value = MAIN_FCUSB.JTAG_IF.ReadMemory(addr32, DATA_WIDTH.Word)
        Return sv
    End Function

    Private Function c_jtag_state(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If MAIN_FCUSB Is Nothing OrElse Not MAIN_FCUSB.JTAG_IF.Chain_IsValid Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG operations are not currently valid"}
        End If
        Dim state_str As String = CStr(arguments(0).Value)
        Select Case state_str.ToUpper
            Case "RunTestIdle".ToUpper
                MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.RunTestIdle)
            Case "Select_DR".ToUpper
                MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Select_DR)
            Case "Capture_DR".ToUpper
                MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Capture_DR)
            Case "Shift_DR".ToUpper
                MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Shift_DR)
            Case "Exit1_DR".ToUpper
                MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Exit1_DR)
            Case "Pause_DR".ToUpper
                MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Pause_DR)
            Case "Exit2_DR".ToUpper
                MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Exit2_DR)
            Case "Update_DR".ToUpper
                MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Update_DR)
            Case "Select_IR".ToUpper
                MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Select_IR)
            Case "Capture_IR".ToUpper
                MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Capture_IR)
            Case "Shift_IR".ToUpper
                MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Shift_IR)
            Case "Exit1_IR".ToUpper
                MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Exit1_IR)
            Case "Pause_IR".ToUpper
                MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Pause_IR)
            Case "Exit2_IR".ToUpper
                MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Exit2_IR)
            Case "Update_IR".ToUpper
                MAIN_FCUSB.JTAG_IF.TAP_GotoState(JTAG_MACHINE_STATE.Update_IR)
            Case Else
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "JTAG.State: unknown state: " & state_str}
        End Select
        Return Nothing
    End Function

    Private Function c_jtag_graycode(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim use_reserve As Boolean = False
        Dim table_ind As Integer = CInt(arguments(0).Value)
        If arguments.Length = 2 Then use_reserve = CBool(arguments(1).Value)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.UInteger)
        If use_reserve Then
            sv.Value = gray_code_table_reverse(table_ind)
        Else
            sv.Value = gray_code_table(table_ind)
        End If
        Return sv
    End Function
    'Undocumented. This is for setting delays on FCUSB Classic EJTAG firmware
    Private Function c_jtag_setdelay(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim dev_ind As Int32 = CInt(arguments(0).Value)
        Dim delay_val As UInt32 = CUInt(CInt(arguments(1).Value))
        Select Case dev_ind
            Case 1 'Intel
                MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SET_OPTION, Nothing, (delay_val << 16) + 1UI)
            Case 2 'AMD
                MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SET_OPTION, Nothing, (delay_val << 16) + 2UI)
            Case 3 'DMA
                MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SET_OPTION, Nothing, (delay_val << 16) + 3UI)
        End Select
        Return Nothing
    End Function

    Private Function c_jtag_exitstate(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim exit_state As Boolean = CBool(arguments(0).Value)
        MAIN_FCUSB.JTAG_IF.JSP.ExitStateMachine = exit_state
        If exit_state Then
            RaiseEvent PrintConsole("SVF exit to test-logic-reset enabled")
        Else
            RaiseEvent PrintConsole("SVF exit to test-logic-reset disabled")
        End If
        Return Nothing
    End Function

    Private Function c_jtag_epc2_read(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        ProgressBar_Percent(0)
        Dim cbProgress As New JTAG_IF.EPC2_ProgressCallback(AddressOf ProgressBar_Percent)
        Dim e_data() As Byte = MAIN_FCUSB.JTAG_IF.EPC2_ReadBinary(cbProgress)
        ProgressBar_Percent(0)
        If e_data IsNot Nothing Then
            Dim sv As New ScriptVariable(CreateVarName(), DataType.Data)
            sv.Value = e_data
            Return sv
        End If
        Return Nothing
    End Function

    Private Function c_jtag_epc2_write(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim BootData() As Byte = CType(arguments(0).Value, Byte())
        Dim CfgData() As Byte = CType(arguments(1).Value, Byte())
        ProgressBar_Percent(0)
        Dim cbProgress As New JTAG_IF.EPC2_ProgressCallback(AddressOf ProgressBar_Percent)
        Dim result As Boolean = MAIN_FCUSB.JTAG_IF.EPC2_WriteBinary(BootData, CfgData, cbProgress)
        ProgressBar_Percent(0)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Bool)
        sv.Value = result
        Return sv
    End Function

    Private Function c_jtag_epc2_erase(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim e_result As Boolean = MAIN_FCUSB.JTAG_IF.EPC2_Erase()
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Bool)
        sv.Value = e_result
        Return sv
    End Function

#End Region

#Region "BSDL"

    Private Function c_bsdl_new(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim n As New BSDL_DEF
        n.PART_NAME = CStr(arguments(0).Value)
        Dim index_created As Integer = MAIN_FCUSB.JTAG_IF.BSDL_Add(n)
        Return New ScriptVariable(CreateVarName(), DataType.Integer, index_created)
    End Function

    Private Function c_bsdl_find(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim param_name As String = CStr(arguments(0).Value)
        Dim lib_ind As Integer = MAIN_FCUSB.JTAG_IF.BSDL_Find(param_name)
        Return New ScriptVariable(CreateVarName(), DataType.Integer, lib_ind)
    End Function

    Private Function c_bsdl_param(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim param_name As String = CStr(arguments(0).Value)
        Dim param_value As UInt32 = CUInt(arguments(1).Value)
        Dim result As Boolean = MAIN_FCUSB.JTAG_IF.BSDL_SetParamater(Index, param_name, param_value)
        Return New ScriptVariable(CreateVarName(), DataType.Bool, result)
    End Function

#End Region

#Region "Boundary Scan Programmer"

    Private Function c_bsp_setup(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        If MAIN_FCUSB Is Nothing Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "FlashcatUSB Professional must be connected via USB"}
        End If
        If Not MAIN_FCUSB.HWBOARD = USB.FCUSB_BOARD.Professional_PCB5 Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "This command is only supported for FlashcatUSB Professional"}
        End If
        If Not MySettings.OPERATION_MODE = DeviceMode.JTAG Then
            Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "This command is only supported when in JTAG mode"}
        End If
        MAIN_FCUSB.JTAG_IF.BoundaryScan_Setup()
        Return Nothing
    End Function

    Private Function c_bsp_init(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim flash_mode As Integer = 0 '0=Automatic, 1=X8_OVER_X16
        If arguments.Length = 1 Then flash_mode = CInt(arguments(0).Value)
        Dim result As Boolean = MAIN_FCUSB.JTAG_IF.BoundaryScan_Init(CBool(flash_mode))
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Bool)
        sv.Value = result
        Return sv
    End Function

    Private Function c_bsp_addpin(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim pin_name As String = CStr(arguments(0).Value)
        Dim pin_output As Int16 = CShort(CInt(arguments(1).Value)) 'cell associated with the bidir or output cell
        Dim pin_control As Int16 = CShort(CInt(arguments(2).Value))  'cell associated with the control register bit
        Dim pin_input As Int16 = -1US 'cell associated with the input cell when output cell is not bidir
        If arguments.Length = 4 Then
            pin_input = CShort(CInt(arguments(3).Value))
        End If
        MAIN_FCUSB.JTAG_IF.BoundaryScan_AddPin(pin_name, pin_output, pin_control, pin_input)
        Return Nothing
    End Function

    Private Function c_bsp_setbsr(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim pin_output As Int16 = CShort(CInt(arguments(0).Value))
        Dim pin_control As Int16 = CShort(CInt(arguments(1).Value))
        Dim pin_level As Boolean = CBool(arguments(2).Value)
        MAIN_FCUSB.JTAG_IF.BoundaryScan_SetBSR(pin_output, pin_control, pin_level)
        Return Nothing
    End Function

    Private Function c_bsp_writebsr(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        MAIN_FCUSB.JTAG_IF.BoundaryScan_WriteBSR()
        Return Nothing
    End Function

    Private Function c_bsp_detect(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim result As Boolean = MAIN_FCUSB.JTAG_IF.BoundaryScan_Detect()
        Dim sv As New ScriptVariable(CreateVarName(), DataType.Bool)
        sv.Value = result
        Return sv
    End Function

#End Region

#Region "LOAD"

    Friend Function c_load_firmware(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Select Case MAIN_FCUSB.HWBOARD
            Case USB.FCUSB_BOARD.Professional_PCB5
            Case USB.FCUSB_BOARD.Mach1
            Case Else
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Only available for PRO or MACH1"}
        End Select
        MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.FW_REBOOT, Nothing, &HFFFFFFFFUI)
        Return Nothing
    End Function

    Friend Function c_load_logic(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Select Case MAIN_FCUSB.HWBOARD
            Case USB.FCUSB_BOARD.Professional_PCB5
                MAIN_FCUSB.LOGIC_SetVersion(&HFFFFFFFFUI)
                FCUSBPRO_PCB5_Init(MAIN_FCUSB, MySettings.OPERATION_MODE)
            Case USB.FCUSB_BOARD.Mach1
                MAIN_FCUSB.LOGIC_SetVersion(&HFFFFFFFFUI)
                FCUSBMACH1_Init(MAIN_FCUSB, MySettings.OPERATION_MODE)
            Case Else
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Only available for PRO or MACH1"}
        End Select
        Return Nothing
    End Function
    'Performs an erase of the logic
    Friend Function c_load_erase(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Select Case MAIN_FCUSB.HWBOARD
            Case USB.FCUSB_BOARD.Mach1
                MACH1_FPGA_ERASE(MAIN_FCUSB)
        End Select
        Return Nothing
    End Function

    Friend Function c_load_bootloader(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Select Case MAIN_FCUSB.HWBOARD
            Case USB.FCUSB_BOARD.Professional_PCB5
            Case USB.FCUSB_BOARD.Mach1
            Case Else
                Return New ScriptVariable("ERROR", DataType.FncError) With {.Value = "Only available for PRO or MACH1"}
        End Select
        Dim bl_data() As Byte = CType(arguments(0).Value, Byte())
        If bl_data IsNot Nothing AndAlso bl_data.Length > 0 Then
            If MAIN_FCUSB.BootloaderUpdate(bl_data) Then
                RaiseEvent PrintConsole("Bootloader successfully updated")
                MAIN_FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.FW_REBOOT, Nothing, &HFFFFFFFFUI)
            Else
                RaiseEvent PrintConsole("Bootloader update was not successful")
            End If
        Else
            RaiseEvent PrintConsole("Error: Load.Bootloader requires data variable with valid data")
        End If
        Return Nothing
    End Function

#End Region

#Region "FLASH commands"

    Friend Function c_flash_add(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim flash_name As String = CStr(arguments(0).Value)
        Dim ID_MFG As Byte = CByte(CInt(arguments(1).Value))
        Dim ID_PART As UInt16 = CUShort(CInt(arguments(2).Value))
        Dim flash_size As UInt32 = CUInt(CInt(arguments(3).Value)) 'Upgrade this to LONG in the future
        Dim flash_if As VCC_IF = CType(CInt(arguments(4).Value), VCC_IF)
        Dim block_layout As BLKLYT = CType(CInt(arguments(5).Value), BLKLYT)
        Dim prog_mode As MFP_PRG = CType(CInt(arguments(6).Value), MFP_PRG)
        Dim delay_mode As MFP_DELAY = CType(CInt(arguments(7).Value), MFP_DELAY)
        Dim new_mem_part As New FlashMemory.P_NOR(flash_name, ID_MFG, ID_PART, flash_size, flash_if, block_layout, prog_mode, delay_mode)
        FlashDatabase.FlashDB.Add(new_mem_part)
        Return Nothing
    End Function

#End Region

#Region "Parallel"

    Friend Function c_parallel_test(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim td As New Threading.Thread(AddressOf MAIN_FCUSB.PARALLEL_NOR_IF.PARALLEL_PORT_TEST)
        td.Start()
        Return Nothing
    End Function

    Friend Function c_parallel_command(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim cmd_addr As UInt32 = CUInt(arguments(0).Value)
        Dim cmd_data As UInt16 = CUShort(CUInt(arguments(1).Value) And &HFFFF)
        MAIN_FCUSB.PARALLEL_NOR_IF.WriteCommandData(cmd_addr, cmd_data)
        Return Nothing
    End Function

    Friend Function c_parallel_write(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim cmd_addr As UInt32 = CUInt(arguments(0).Value)
        Dim cmd_data As UInt16 = CUShort(CUInt(arguments(1).Value) And &HFFFF)
        MAIN_FCUSB.PARALLEL_NOR_IF.WriteMemoryAddress(cmd_addr, cmd_data)
        Return Nothing
    End Function

    Friend Function c_parallel_read(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim cmd_addr As UInt32 = CUInt(arguments(0).Value)
        Dim sv As New ScriptVariable(CreateVarName(), DataType.UInteger)
        sv.Value = MAIN_FCUSB.PARALLEL_NOR_IF.ReadMemoryAddress(cmd_addr)
        Return sv
    End Function

#End Region

#Region "User control handlers"
    Private Delegate Sub UpdateFunction_Progress(percent As Integer)
    Private Delegate Sub UpdateFunction_Status(msg As String)
    Private UserTabCount As Integer
    Private OurFlashDevices As New List(Of MemoryDeviceInstance)

    'Reads the data from flash and verifies it (returns nothing on error)
    Private Function ReadMemoryVerify(address As UInt32, data_len As UInt32, index As FlashMemory.FlashArea) As Byte()
        Dim cb As New MemoryDeviceInstance.StatusCallback
        cb.UpdatePercent = New UpdateFunction_Progress(AddressOf MainApp.ProgressBar_Percent)
        cb.UpdateTask = New UpdateFunction_Status(AddressOf MainApp.SetStatus)
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
    'Removes a user control from NAME
    Private Sub RemoveUserControl(ctr_name As String)
        If GUI Is Nothing Then Exit Sub
        If UserTabCount = 0 Then Exit Sub
        For i As Integer = 0 To CInt(UserTabCount - 1)
            Dim uTab As TabPage = GUI.GetUserTab(i)
            For Each user_control As Control In uTab.Controls
                If user_control.Name.ToUpper.Equals(ctr_name.ToUpper) Then
                    uTab.Controls.Remove(user_control)
                    Exit Sub
                End If
            Next
        Next
    End Sub
    'Handles when the user clicks a button
    Private Sub ButtonHandler(sender As Object, e As EventArgs)
        Dim MyButton As Button = CType(sender, Button)
        Dim EventToCall As String = MyButton.Name
        Dim EventThread As New Thread(AddressOf ScriptProcessor.CallEvent)
        EventThread.Name = "Event:" & EventToCall
        EventThread.SetApartmentState(ApartmentState.STA)
        EventThread.Start(EventToCall)
        MyButton.Select()
    End Sub

#End Region

    Private Function c_endian(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim endian_mode As String = arguments(0).Value.ToString.ToUpper
        Select Case endian_mode
            Case "MSB"
                MySettings.BIT_ENDIAN = BitEndianMode.BigEndian32
            Case "LSB"
                MySettings.BIT_ENDIAN = BitEndianMode.LittleEndian32_16bit
            Case "LSB16"
                MySettings.BIT_ENDIAN = BitEndianMode.LittleEndian32_16bit
            Case "LSB8"
                MySettings.BIT_ENDIAN = BitEndianMode.LittleEndian32_8bit
            Case Else
                MySettings.BIT_ENDIAN = BitEndianMode.BigEndian32
        End Select
        Return Nothing
    End Function

    Private Function c_verify(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim verify_bool As Boolean = CBool(arguments(0).Value)
        MySettings.VERIFY_WRITE = verify_bool
        Return Nothing
    End Function

    Private Function c_mode(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim rv As New ScriptVariable(CreateVarName(), DataType.String)
        Select Case Me.CURRENT_DEVICE_MODE
            Case DeviceMode.SPI
                rv.Value = "SPI"
            Case DeviceMode.SPI_EEPROM
                rv.Value = "SPI (EEPROM)"
            Case DeviceMode.JTAG
                rv.Value = "JTAG"
            Case DeviceMode.I2C_EEPROM
                rv.Value = "I2C"
            Case DeviceMode.PNOR
                rv.Value = "Parallel NOR"
            Case DeviceMode.PNAND
                rv.Value = "Parallel NAND"
            Case DeviceMode.ONE_WIRE
                rv.Value = "1-WIRE"
            Case DeviceMode.SPI_NAND
                rv.Value = "SPI-NAND"
            Case DeviceMode.EPROM
                rv.Value = "EPROM/OTP"
            Case DeviceMode.HyperFlash
                rv.Value = "HyperFlash"
            Case DeviceMode.Microwire
                rv.Value = "Microwire"
            Case DeviceMode.SQI
                rv.Value = "QUAD SPI"
            Case DeviceMode.FWH
                rv.Value = "Firmware HUB"
            Case Else
                rv.Value = "Other"
        End Select
        Return rv
    End Function

    Private Function c_refresh(arguments() As ScriptVariable, Index As Int32) As ScriptVariable
        Dim count As Integer = MEM_IF.DeviceCount
        For i = 0 To count - 1
            Dim mem_device As MemoryDeviceInstance = MEM_IF.GetDevice(i)
            mem_device.RefreshControls()
        Next
        Return Nothing
    End Function

End Class
