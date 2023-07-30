Imports System.IO

Public Class ConsoleMode
    Private ConsoleLog As List(Of String) = New List(Of String)()
    Public MyOperation As ConsoleOperation = New ConsoleOperation()
    Private Delegate Sub UpdateFunction_Progress(ByVal percent As Integer)
    Private Delegate Sub UpdateFunction_SpeedLabel(ByVal speed_str As String)
    Public Property device_connected As Boolean
    Public Property console_result As ExitValue = ExitValue.NoError

    Public Enum ExitValue As Integer
        [Error] = -1
        NoError = 0
    End Enum

    Partial Public Class ConsoleOperation
        Public Property CurrentTask As ConsoleTask = ConsoleTask.NoTask
        Public Property Mode As DeviceMode = DeviceMode.Unspecified
        Public Property VERIFY As Boolean = True
        Public Property FILENAME As String ' The filename to write to or read from
        Public Property FILE_IO As FileInfo
        Public Property DATA_LENGTH As Long = 0 ' Optional file length argument
        Public Property FLASH_OFFSET As Long = 0UI
        Public Property FLASH_AREA As FlashMemory.FlashArea = FlashMemory.FlashArea.Main
        Public Property SPI_EEPROM As String
        Public Property PARALLEL_EEPROM As String
        Public Property SPI_FREQ As SPI.SPI_SPEED = SPI.SPI_SPEED.MHZ_8
        Public Property I2C_EEPROM As Integer
        Public Property LogOutput As Boolean = False
        Public Property LogAppendFile As Boolean = False
        Public Property LogFilename As String = "FlashcatUSB_Console.txt"
        Public Property ExitConsole As Boolean = False ' Closes the console window when complete
    End Class

    Public Sub Start(ParamArray Args As String())
        Try
            Dim os_ver As String = Utilities.PlatformIDToStr(Environment.OSVersion.Platform)
            PrintConsole(RM.GetString("welcome_to_flashcatusb") & ", build: " & FC_BUILD)
            PrintConsole("Copyright " & Date.Now.Year & " - Embedded Computers LLC")
            PrintConsole("Running on: " & os_ver & " (" & Utilities.GetOsBitsString() & ")")
            PrintConsole("FlashcatUSB Script Engine build: " & EC_ScriptEngine.Processor.Build)
            If MainApp.LicenseStatus = LicenseStatusEnum.LicensedValid Then
                Me.PrintConsole("License status: valid (expires " & MainApp.MySettings.LICENSE_EXP.ToShortDateString() & ")")
                Me.PrintConsole("License to: " & MainApp.MySettings.LICENSED_TO)
            Else
                PrintConsole("License status: non-commercial use only")
            End If
            Environment.ExitCode = 0
            If Args.Length = 0 Then
                Console_DisplayHelp()
            Else
                MyOperation = ConsoleMode_ParseSwitches(Args)
                If MyOperation Is Nothing Then Exit Sub
                If MyOperation.Mode = DeviceMode.Unspecified Then
                    MyOperation.Mode = MySettings.OPERATION_MODE
                    PrintConsole("Device mode not specified, using settings file")
                    PrintConsole("Mode set to: " & FlashcatSettings.DeviceModetoString(MySettings.OPERATION_MODE))
                Else
                    MySettings.OPERATION_MODE = MyOperation.Mode
                End If
                If MyOperation Is Nothing Then Return
                If Not ConsoleMode_SetupOperation() Then Exit Sub
                Dim counter As Integer = 0
                While Not device_connected
                    If MainApp.AppIsClosing Then Exit Sub
                    Utilities.Sleep(50)
                    counter += 1
                    If counter = 20 Then 'Wait up to 1 second to see if the device is connected
                        PrintConsole("FlashcatUSB is not connected")
                        Exit Sub
                    End If
                End While
                ConsoleMode_RunTask()
            End If
        Catch
        Finally
            MainApp.AppClosing()
            Console_Exit()
        End Try
    End Sub

    Public Function Console_Ask(question As String) As String
        If ProgressBarEnabled Then
            Console.SetCursorPosition(0, Console.CursorTop - 1)
            Console.WriteLine(question.PadRight(80))
            Console.WriteLine("")
            Progress_Set(ProgressBarLast)

            If Not String.IsNullOrEmpty(ProgressBarLastSpeed) Then
                Progress_UpdateSpeed(ProgressBarLastSpeed)
            End If

            Console.SetCursorPosition(question.Length + 1, Console.CursorTop - 2)
            Dim result As String = Console.ReadLine()
            Console.SetCursorPosition(0, Console.CursorTop + 1)
            Return result
        Else
            Console.Write(question & " ")
            Return Console.ReadLine()
        End If
    End Function

    Private Function ConsoleMode_ParseSwitches(args As String()) As ConsoleOperation
        Dim arg_index As Integer = 0
        Dim MyConsoleParams = New ConsoleOperation()
        Select Case If(args(arg_index).ToUpper(), "")
            Case "-H", "-?", "-HELP"
                MyConsoleParams.CurrentTask = ConsoleTask.Help
                Exit Select
            Case "-CHECK"
                MyConsoleParams.CurrentTask = ConsoleTask.Check
                Exit Select
            Case "-DETECT"
                MyConsoleParams.CurrentTask = ConsoleTask.Detect
                Exit Select
            Case "-READ"
                MyConsoleParams.CurrentTask = ConsoleTask.ReadMemory
                Exit Select
            Case "-WRITE"
                MyConsoleParams.CurrentTask = ConsoleTask.WriteMemory
                Exit Select
            Case "-ERASE"
                MyConsoleParams.CurrentTask = ConsoleTask.EraseMemory
                Exit Select
            Case "-SCRIPT"
                MyConsoleParams.CurrentTask = ConsoleTask.ExecuteScript
                Exit Select
            Case "-COMPARE"
                MyConsoleParams.CurrentTask = ConsoleTask.Compare
                Exit Select
            Case "-KEY"
                MyConsoleParams.CurrentTask = ConsoleTask.License
                Exit Select
            Case Else
                PrintConsole("OPERATION mode not specified")
                Return Nothing
        End Select
        arg_index += 1
        If (args.Length > 1) Then
            If MyConsoleParams.CurrentTask = ConsoleTask.License Then
                If License_LoadKey(args(1).ToUpper()) Then
                    PrintConsole("Software license successfully applied")
                    MySettings.Save()
                Else
                    PrintConsole("Software license is not valid")
                End If
                Return Nothing
            ElseIf Equals(args(1).ToUpper(), "-SPI") Then
                MyConsoleParams.Mode = DeviceMode.SPI : arg_index += 1
            ElseIf Equals(args(1).ToUpper(), "-SQI") Then
                MyConsoleParams.Mode = DeviceMode.SQI : arg_index += 1
            ElseIf Equals(args(1).ToUpper(), "-SPINAND") Then
                MyConsoleParams.Mode = DeviceMode.SPI_NAND : arg_index += 1
            ElseIf Equals(args(1).ToUpper(), "-SPIEEPROM") Then
                MyConsoleParams.Mode = DeviceMode.SPI_EEPROM : arg_index += 1
            ElseIf Equals(args(1).ToUpper(), "-I2C") Then
                MyConsoleParams.Mode = DeviceMode.I2C_EEPROM : arg_index += 1
            ElseIf Equals(args(1).ToUpper(), "-SWI") Then
                MyConsoleParams.Mode = DeviceMode.ONE_WIRE : arg_index += 1
            ElseIf Equals(args(1).ToUpper(), "-PNOR") Then
                MyConsoleParams.Mode = DeviceMode.PNOR : arg_index += 1
            ElseIf Equals(args(1).ToUpper(), "-PNAND") Then
                MyConsoleParams.Mode = DeviceMode.PNAND : arg_index += 1
            ElseIf Equals(args(1).ToUpper(), "-PEEPROM") Then
                MyConsoleParams.Mode = DeviceMode.P_EEPROM : arg_index += 1
            ElseIf Equals(args(1).ToUpper(), "-MICROWIRE") Then
                MyConsoleParams.Mode = DeviceMode.Microwire : arg_index += 1
            ElseIf Equals(args(1).ToUpper(), "-FWH") Then
                MyConsoleParams.Mode = DeviceMode.FWH : arg_index += 1
            ElseIf Equals(args(1).ToUpper(), "-HYPERFLASH") Then
                MyConsoleParams.Mode = DeviceMode.HyperFlash : arg_index += 1
            ElseIf Equals(args(1).ToUpper(), "-EPROM") Then
                MyConsoleParams.Mode = DeviceMode.EPROM : arg_index += 1
            ElseIf Equals(args(1).ToUpper(), "-JTAG") Then
                MyConsoleParams.Mode = DeviceMode.JTAG : arg_index += 1
            Else
                PrintConsole("MODE not specified (using OPERATION setting instead)")
            End If
        End If

        While arg_index <= args.Length - 1
            If Not String.IsNullOrEmpty(args(arg_index)) Then
                Dim last_option As Boolean = (arg_index = args.Length - 1)
                Select Case args(arg_index).ToUpper()
                    Case "-FILE"
                        If last_option OrElse args(arg_index + 1).StartsWith("-") Then
                            PrintConsole(String.Format("You must specify a value following {0}", args(arg_index)))
                            Return Nothing
                        End If
                        MyConsoleParams.FILENAME = args(arg_index + 1)
                        arg_index += 1
                        Exit Select
                    Case "-LOG"
                        If last_option OrElse args(arg_index + 1).StartsWith("-") Then
                            PrintConsole(String.Format("You must specify a value following {0}", args(arg_index)))
                            Return Nothing
                        End If
                        MyConsoleParams.LogFilename = args(arg_index + 1)
                        arg_index += 1
                        MyConsoleParams.LogOutput = True
                        Exit Select
                    Case "-MHZ"
                        If last_option OrElse args(arg_index + 1).StartsWith("-") Then
                            PrintConsole(String.Format("You must specify a value following {0}", args(arg_index)))
                            Return Nothing
                        End If
                        Dim speed As String = args(arg_index + 1)
                        arg_index += 1
                        If Utilities.IsNumeric(speed) AndAlso (Integer.Parse(speed) >= 1 AndAlso Integer.Parse(speed) <= 48) Then
                        Else
                            PrintConsole("MHZ value must be between 1 and 48")
                            Return Nothing
                        End If
                        MyConsoleParams.SPI_FREQ = CType(Integer.Parse(speed) * 1000000, SPI.SPI_SPEED)
                        Exit Select
                    Case "-AREA"
                        If last_option OrElse args(arg_index + 1).StartsWith("-") Then
                            PrintConsole(String.Format("You must specify a value following {0}", args(arg_index)))
                            Return Nothing
                        End If
                        Dim area_option As String = args(arg_index + 1)
                        arg_index += 1
                        If area_option.ToUpper().Equals("MAIN") Then
                            MyConsoleParams.FLASH_AREA = FlashMemory.FlashArea.Main
                        ElseIf area_option.ToUpper().Equals("SPARE") Then
                            MyConsoleParams.FLASH_AREA = FlashMemory.FlashArea.OOB
                        ElseIf area_option.ToUpper().Equals("ALL") Then
                            MyConsoleParams.FLASH_AREA = FlashMemory.FlashArea.All
                        Else
                            PrintConsole("-AREA option not valid; must specify: main, spare or all")
                            Return Nothing
                        End If
                        Exit Select
                    Case "-LOGAPPEND"
                        MyConsoleParams.LogAppendFile = True
                        Exit Select
                    Case "-OFFSET"
                        If last_option OrElse args(arg_index + 1).StartsWith("-") Then
                            PrintConsole(String.Format("You must specify a value following {0}", args(arg_index)))
                            Return Nothing
                        End If
                        Dim offset_value As String = args(arg_index + 1)
                        arg_index += 1
                        If Not Utilities.IsDataType.HexString(offset_value) AndAlso Not Utilities.IsNumeric(offset_value) Then
                            PrintConsole(String.Format("{0} value must be numeric or hexadecimal", args(arg_index)))
                            Return Nothing
                        End If
                        If Utilities.IsNumeric(offset_value) Then
                            MyConsoleParams.FLASH_OFFSET = UInteger.Parse(offset_value)
                        ElseIf Utilities.IsDataType.HexString(offset_value) Then
                            MyConsoleParams.FLASH_OFFSET = Utilities.HexToLng(offset_value)
                        End If
                        Exit Select
                    Case "-LENGTH"
                        If last_option OrElse args(arg_index + 1).StartsWith("-") Then
                            PrintConsole(String.Format("You must specify a value following {0}", args(arg_index)))
                            Return Nothing
                        End If
                        Dim offset_value As String = args(arg_index + 1)
                        arg_index += 1
                        If Not Utilities.IsDataType.HexString(offset_value) AndAlso Not Utilities.IsNumeric(offset_value) Then
                            PrintConsole(String.Format("{0} value must be numeric or hexadecimal", args(arg_index)))
                            Return Nothing
                        End If
                        If Utilities.IsNumeric(offset_value) Then
                            MyConsoleParams.DATA_LENGTH = Integer.Parse(offset_value)
                        ElseIf Utilities.IsDataType.HexString(offset_value) Then
                            MyConsoleParams.DATA_LENGTH = Utilities.HexToLng(offset_value)
                        End If
                        Exit Select
                    Case "-VERIFY_OFF"
                        MyConsoleParams.VERIFY = False
                        Exit Select
                    Case "-EXIT"
                        MyConsoleParams.ExitConsole = True
                        Exit Select
                    Case "-ADDRESS"
                        If last_option OrElse args(arg_index + 1).StartsWith("-") Then
                            PrintConsole(String.Format("You must specify a value following {0}", args(arg_index)))
                            Return Nothing
                        End If
                        Dim offset_value = args(arg_index + 1)
                        arg_index += 1
                        If Not Utilities.IsDataType.HexString(offset_value) AndAlso Not Utilities.IsNumeric(offset_value) Then
                            PrintConsole(String.Format("{0} value must be numeric or hexadecimal", args(arg_index)))
                            Return Nothing
                        End If
                        If Utilities.IsNumeric(offset_value) Then
                            MainApp.MySettings.I2C_ADDRESS = UInteger.Parse(offset_value) And 255L
                        ElseIf Utilities.IsDataType.HexString(offset_value) Then
                            MainApp.MySettings.I2C_ADDRESS = UInteger.Parse(offset_value) And 255L
                        End If
                        Exit Select
                    Case "-EEPROM"
                        If last_option OrElse args(arg_index + 1).StartsWith("-") Then
                            PrintConsole(String.Format("You must specify a value following {0}", args(arg_index)))
                            Return Nothing
                        End If
                        Dim eeprom_str As String = args(arg_index + 1)
                        arg_index += 1
                        Dim Device_Found = False
                        Dim I2C_IF = New I2C_Programmer(Nothing)
                        Dim index = 0
                        For Each i2c_device In I2C_IF.I2C_EEPROM_LIST
                            If eeprom_str.ToUpper().Equals(i2c_device.NAME.ToUpper()) Then
                                MyConsoleParams.I2C_EEPROM = index
                                Device_Found = True
                                Exit For
                            End If
                            index += 1
                        Next
                        If Not Device_Found Then
                            Dim SPI_EEPROM_LIST = MainApp.GetDevices_SERIAL_EEPROM()
                            For Each dev In SPI_EEPROM_LIST
                                Dim spi_part As String = dev.NAME.Substring(dev.NAME.IndexOf(" ") + 1)
                                If dev.NAME.ToUpper().Equals(eeprom_str.ToUpper()) OrElse spi_part.ToUpper().Equals(eeprom_str.ToUpper()) Then
                                    MyConsoleParams.SPI_EEPROM = dev.NAME
                                    Device_Found = True
                                    Exit For
                                End If
                            Next
                        End If
                        If Not Device_Found Then
                            Dim SPI_EEPROM_LIST = MainApp.GetDevices_PARALLEL_EEPROM()
                            For Each dev In SPI_EEPROM_LIST
                                Dim spi_part As String = dev.NAME.Substring(dev.NAME.IndexOf(" ") + 1)
                                If dev.NAME.ToUpper().Equals(eeprom_str.ToUpper()) OrElse spi_part.ToUpper().Equals(eeprom_str.ToUpper()) Then
                                    MyConsoleParams.PARALLEL_EEPROM = dev.NAME
                                    Device_Found = True
                                    Exit For
                                End If
                            Next
                        End If
                        If Not Device_Found Then
                            PrintConsole("The EEPROM device you specified was not found")
                            Console_PrintEEPROMList()
                            Return Nothing
                        End If
                        Exit Select
                    Case Else
                        PrintConsole(String.Format("Option not recognized: {0}", args(arg_index)))
                        Exit Select
                End Select
            End If
            arg_index += 1 ' Load other options
        End While
        Return MyConsoleParams
    End Function

    Private Function ConsoleMode_SetupOperation() As Boolean
        Select Case MyOperation.CurrentTask
            Case ConsoleTask.Help
                Console_DisplayHelp()
                Return False
            Case ConsoleTask.Check
                ConsoleMode_Check()
                Return False
            Case ConsoleTask.Detect
                PrintConsole("Performing memory Auto-Detect")
            Case ConsoleTask.ReadMemory
                If String.IsNullOrEmpty(MyOperation.FILENAME) Then
                    PrintConsole("Operation ReadMemory requires option -FILE to specify where to save to")
                    Return False
                End If
                MyOperation.FILE_IO = New FileInfo(MyOperation.FILENAME)
            Case ConsoleTask.WriteMemory
                If String.IsNullOrEmpty(MyOperation.FILENAME) Then
                    PrintConsole("Operation WriteMemory requires option -FILE to specify where to save to")
                    Return False
                End If
                MyOperation.FILE_IO = New FileInfo(MyOperation.FILENAME)
                If Not MyOperation.FILE_IO.Exists Then
                    PrintConsole("Error: file not found" & ": " & MyOperation.FILENAME)
                    Return False
                End If
            Case ConsoleTask.Compare
                If String.IsNullOrEmpty(MyOperation.FILENAME) Then
                    PrintConsole("Operation Compare requires option -FILE to specify which file to compare to")
                    Return False
                End If
                MyOperation.FILE_IO = New FileInfo(MyOperation.FILENAME)
                If Not MyOperation.FILE_IO.Exists Then
                    PrintConsole("Error: file not found" & ": " & MyOperation.FILENAME)
                    Return False
                End If
            Case ConsoleTask.ExecuteScript
                If String.IsNullOrEmpty(MyOperation.FILENAME) Then
                    PrintConsole("Operation ExecuteScript requires option -FILE to specify which script to run")
                    Return False
                End If
                MyOperation.FILE_IO = New FileInfo((New IO.FileInfo(MyLocation)).DirectoryName & "\Scripts\" & MyOperation.FILENAME)
                If Not MyOperation.FILE_IO.Exists Then
                    MyOperation.FILE_IO = New FileInfo(MyOperation.FILENAME)
                    If Not MyOperation.FILE_IO.Exists Then
                        PrintConsole("Error: file not found" & ": " & MyOperation.FILE_IO.FullName)
                        Return False
                    End If
                End If
        End Select
        Return True
    End Function

    Private Sub ConsoleMode_RunTask()
        Dim operation_success = False
        If MainApp.MAIN_FCUSB Is Nothing Then
            console_result = ExitValue.Error
            PrintConsole("Error: Unable to connect to FlashcatUSB")
            Return
        End If
        Dim supported_modes = GetSupportedModes(MainApp.MAIN_FCUSB)
        If Array.IndexOf(supported_modes, MyOperation.Mode) = -1 Then
            console_result = ExitValue.Error
            PrintConsole("Hardware does not support the selected MODE")
            Return
        End If
        If Not DetectDevice.Device(MainApp.MAIN_FCUSB, GetDeviceParams()) Then
            console_result = ExitValue.Error
            Return
        End If




        Dim mem_dev As MemoryInterface.MemoryDeviceInstance = MainApp.MEM_IF.GetDevice(0)
        If mem_dev.DEV_MODE = DeviceMode.PNAND Then
            DirectCast(mem_dev.MEM_IF, PARALLEL_NAND).MemoryArea = MyOperation.FLASH_AREA
            'mem_dev.Size = mem_dev.MEM_IF.DeviceSize
        ElseIf mem_dev.DEV_MODE = DeviceMode.SPI_NAND Then
            DirectCast(mem_dev.MEM_IF, SPINAND_Programmer).MemoryArea = MyOperation.FLASH_AREA
            'mem_dev.Size = mem_dev.MEM_IF.DeviceSize
        End If



        Select Case MyOperation.CurrentTask
            Case ConsoleTask.ReadMemory
                operation_success = Me.ConsoleMode_RunTask_ReadMemory(mem_dev)
            Case ConsoleTask.WriteMemory
                operation_success = Me.ConsoleMode_RunTask_WriteMemory(mem_dev)
            Case ConsoleTask.EraseMemory
                operation_success = Me.ConsoleMode_RunTask_EraseMemory(mem_dev)
            Case ConsoleTask.Compare
                operation_success = Me.ConsoleMode_RunTask_CompareMemory(mem_dev)
            Case ConsoleTask.ExecuteScript
                operation_success = Me.ConsoleMode_RunTask_ExecuteScript(mem_dev)
            Case ConsoleTask.Detect
                operation_success = Me.ConsoleMode_RunTask_Detect(mem_dev)
        End Select
        PrintConsole("----------------------------------------------")
        PrintConsole("Application completed")
        If MyOperation.LogOutput Then
            If MyOperation.LogAppendFile Then
                Utilities.FileIO.AppendFile(ConsoleLog.ToArray(), MyOperation.LogFilename)
            Else
                Utilities.FileIO.WriteFile(ConsoleLog.ToArray(), MyOperation.LogFilename)
            End If
        End If
        If operation_success Then
            console_result = ExitValue.NoError
        Else
            console_result = ExitValue.Error
        End If
    End Sub

    Private Function ConsoleMode_RunTask_ReadMemory(mem_dev As MemoryInterface.MemoryDeviceInstance) As Boolean
        Try
            PrintConsole("Beginning Read Memory Operation")
            Progress_Create()

            Dim available_size As Long = mem_dev.MEM_IF.DeviceSize

            If MyOperation.FLASH_OFFSET > available_size Then
                MyOperation.FLASH_OFFSET = 0UI ' Out of bounds
            End If
            If MyOperation.DATA_LENGTH = 0 Or MyOperation.FLASH_OFFSET + MyOperation.DATA_LENGTH > available_size Then
                MyOperation.DATA_LENGTH = available_size - MyOperation.FLASH_OFFSET
            End If

            Dim cb = New MemoryInterface.MemoryDeviceInstance.StatusCallback()
            cb.UpdatePercent = New UpdateFunction_Progress(AddressOf Progress_Set)
            cb.UpdateSpeed = New UpdateFunction_SpeedLabel(AddressOf Progress_UpdateSpeed)

            Dim f_params = New ReadParameters()

            Using ms = MyOperation.FILE_IO.OpenWrite()
                f_params.Address = MyOperation.FLASH_OFFSET
                f_params.Count = MyOperation.DATA_LENGTH
                f_params.Status = cb
                mem_dev.ReadStream(ms, f_params)
            End Using

            PrintConsole(String.Format("Saved data to: {0}", MyOperation.FILE_IO.FullName))
            Return True
        Catch e As Exception
            Return False
        Finally
            Progress_Remove()
        End Try
    End Function

    Private Function ConsoleMode_RunTask_WriteMemory(mem_dev As MemoryInterface.MemoryDeviceInstance) As Boolean
        Try
            Progress_Create()
            Dim available_size As Long = mem_dev.MEM_IF.DeviceSize
            If MyOperation.FLASH_OFFSET > available_size Then MyOperation.FLASH_OFFSET = 0UI ' Out of bounds
            Dim max_write_count As UInt32 = CUInt(Math.Min(available_size, MyOperation.FILE_IO.Length))
            If MyOperation.DATA_LENGTH = 0 Then
                MyOperation.DATA_LENGTH = max_write_count
            ElseIf MyOperation.DATA_LENGTH > max_write_count Then
                MyOperation.DATA_LENGTH = max_write_count
            End If
            Dim data_out() As Byte = Utilities.FileIO.ReadBytes(MyOperation.FILE_IO.FullName, MyOperation.DATA_LENGTH)
            If data_out Is Nothing OrElse data_out.Length = 0 Then
                PrintConsole("Error: Write was not successful because there is no data to write")
                Return False
            End If
            Dim verify_str = "enabled"
            If Not MyOperation.VERIFY Then verify_str = "disabled"
            PrintConsole("Performing WRITE of " & MyOperation.DATA_LENGTH.ToString("#,###") & " bytes at offset 0x" & MyOperation.FLASH_OFFSET.ToString("X") & " with verify " & verify_str)
            Array.Resize(data_out, CInt(MyOperation.DATA_LENGTH))
            Dim cb = New MemoryInterface.MemoryDeviceInstance.StatusCallback()
            cb.UpdatePercent = New UpdateFunction_Progress(AddressOf Progress_Set)
            cb.UpdateSpeed = New UpdateFunction_SpeedLabel(AddressOf Progress_UpdateSpeed)
            Dim write_result As Boolean = mem_dev.WriteBytes(MyOperation.FLASH_OFFSET, data_out, MyOperation.VERIFY, cb)
            If write_result Then
                PrintConsole("Write operation was successful")
                Return True
            Else
                PrintConsole("Error, write operation was not successful")
                Return False
            End If
        Catch
            Return False
        Finally
            Progress_Remove()
        End Try
    End Function

    Private Function ConsoleMode_RunTask_EraseMemory(mem_dev As MemoryInterface.MemoryDeviceInstance) As Boolean
        Try
            Progress_Create()
            If mem_dev.EraseFlash() Then
                PrintConsole("Memory device erased successfully")
                Return True
            Else
                PrintConsole("Error: erasing device failed")
                Return False
            End If
        Catch
            Return False
        Finally
            Progress_Remove()
        End Try
    End Function

    Private Function ConsoleMode_RunTask_CompareMemory(mem_dev As MemoryInterface.MemoryDeviceInstance) As Boolean
        Try
            Progress_Create()
            Dim available_size As Long = mem_dev.MEM_IF.DeviceSize
            If MyOperation.FLASH_OFFSET > available_size Then
                MyOperation.FLASH_OFFSET = 0UI ' Out of bounds
            End If
            If MyOperation.DATA_LENGTH = 0 OrElse MyOperation.FLASH_OFFSET + MyOperation.DATA_LENGTH > available_size Then
                MyOperation.DATA_LENGTH = available_size - MyOperation.FLASH_OFFSET
            End If
            If MyOperation.DATA_LENGTH > MyOperation.FILE_IO.Length Then MyOperation.DATA_LENGTH = MyOperation.FILE_IO.Length
            Dim cmp_data() As Byte = Utilities.FileIO.ReadBytes(MyOperation.FILE_IO.FullName, MyOperation.DATA_LENGTH)
            PrintConsole(String.Format("Performing memory compare operation of {0} bytes", cmp_data.Length.ToString("#,###")))
            Dim err_counter As Integer = 0
            Dim bytes_left As Integer = cmp_data.Length
            Dim bytes_read As Integer = 0
            Dim addr As Long = MyOperation.FLASH_OFFSET
            Progress_Set(0)
            While (bytes_left > 0)
                Progress_UpdateSpeed(String.Format("{0} of {1} bytes processed", bytes_read.ToString("#,###"), cmp_data.Length.ToString("#,###")))
                Dim count As Integer = Math.Min(bytes_left, 131072) 'Read up to 128KB per check
                Dim block() As Byte = mem_dev.ReadFlash(addr, count)
                If block Is Nothing Then PrintConsole("Error: while reading memory from device") : Return False
                For i = 0 To block.Length - 1
                    If Not block(i) = cmp_data(bytes_read + i) Then err_counter += 1
                Next
                bytes_left -= count : addr += count : bytes_read += count
                Progress_Set(bytes_read / cmp_data.Length * 100)
            End While
            Dim valid_bytes As Integer = cmp_data.Length - err_counter
            Dim mismatch As Integer = CInt(CSng(CSng(valid_bytes) / CSng(cmp_data.Length)) * 100)
            PrintConsole(String.Format(RM.GetString("mc_compare_mismatch"), err_counter.ToString("#,##0"), mismatch))
            Utilities.Sleep(100)
            Return True
        Catch
            Return False
        Finally
            Progress_Remove()
        End Try
    End Function

    Private Function ConsoleMode_RunTask_ExecuteScript(mem_dev As MemoryInterface.MemoryDeviceInstance) As Boolean
        MainApp.CURRENT_DEVICE_MODE = MyOperation.Mode
        Dim script_text() As String = Utilities.FileIO.ReadFile(MyOperation.FILE_IO.FullName)
        If MainApp.ScriptProcessor.RunScript(script_text) Then
            Dim o As Object = MainApp.ScriptProcessor.CurrentVars.GetValue("ERROR")
            If o IsNot Nothing Then
                If o.GetType Is GetType(Boolean) Then
                    Return (Not DirectCast(o, Boolean))
                End If
            End If
            Return True
        Else
            Return False
        End If
    End Function

    Private Function ConsoleMode_RunTask_Detect(mem_dev As MemoryInterface.MemoryDeviceInstance) As Boolean
        Return True
    End Function

    ' Frees up memory and exits the application and console io calls
    Private Sub Console_Exit()
        If MyOperation?.LogOutput Then
            If MyOperation.LogAppendFile Then
                Utilities.FileIO.AppendFile(ConsoleLog.ToArray(), MyOperation.LogFilename)
            Else
                Utilities.FileIO.WriteFile(ConsoleLog.ToArray(), MyOperation.LogFilename)
            End If
        End If
        If Not MyOperation?.ExitConsole Then
            PrintConsole("----------------------------------------------")
            If Not Console.IsInputRedirected Then
                PrintConsole("Press any key to close")
                Console.ReadKey()
            End If
        End If
        If TypeOf MainApp.MAIN_FCUSB Is Object Then
            MainApp.MAIN_FCUSB.USB_LEDOff()
            MainApp.MAIN_FCUSB.Disconnect()
        End If
        Environment.Exit(Me.console_result)
    End Sub

    Private Sub ConsoleMode_Check()
        Dim counter = 0
        While MainApp.MAIN_FCUSB Is Nothing
            If MainApp.AppIsClosing Then
                Return
            End If
            Utilities.Sleep(100)
            counter += 1
            If counter = 100 Then
                PrintConsole("Waiting for connected device timedout")
                Return
            End If
        End While
        Utilities.Sleep(500)
        Return
    End Sub

    Private Sub Console_DisplayHelp()
        PrintConsole("--------------------------------------------")
        PrintConsole("Syntax: exe [OPERATION] [MODE] (options) ...")
        PrintConsole("")
        PrintConsole("Operations:")
        PrintConsole("-detect           " & "Use this to detect a connected memory device")
        PrintConsole("-read             " & "Will perform a flash memory read operation")
        PrintConsole("-write            " & "Will perform a flash memory write operation")
        PrintConsole("-erase            " & "Erases the entire memory device")
        PrintConsole("-script           " & "Allows you to execute a FlashcatUSB script file (*.fcs)")
        PrintConsole("-check            " & "Display all connected FlashcatUSB devices")
        PrintConsole("-key              " & "Register your software")
        PrintConsole("-compare          " & "Compares a local file with data from a memory device")
        PrintConsole("-help             " & "Shows this dialog")
        PrintConsole("")
        PrintConsole("Supported modes:")
        PrintConsole("-SPI -SQI -SPINAND -SPIEEPROM -I2C -SWI -PNOR -PNAND")
        PrintConsole("-MICROWIRE -FWH -HYPERFLASH -EPROM -JTAG -PEEPROM")
        PrintConsole("")
        PrintConsole("Options:")
        PrintConsole("-File (filename)  " & "Specifies the file to use for read/write/execute")
        PrintConsole("-Length (value)   " & "Specifies the number of bytes to read/write")
        PrintConsole("-MHZ (value)      " & "Specifies the MHz speed for SPI operation")
        PrintConsole("-Area (value)     " & "Specifies the region of NAND memory: main/spare/all")
        PrintConsole("-Offset (value)   " & "Specifies the offset to write the file to flash")
        PrintConsole("-EEPROM (part)    " & "Specifies a SPI/I2C EEPROM (i.e. M95080 or 24XX64)")
        PrintConsole("-Address (hex)    " & "Specifies the I2C slave address (i.e. 0xA0)")
        PrintConsole("-Verify_Off       " & "Turns off data verification for flash write operations")
        PrintConsole("-Exit             " & "Automatically close window when completed")
        PrintConsole("-Log (filename)   " & "Save the output from the console to a file")
        PrintConsole("-LogAppend        " & "Append the console text to an existing file")
    End Sub
    ' Prints the list of valid options that can be used for the -EEPROM option
    Private Sub Console_PrintEEPROMList()
        PrintConsole("Valid EEPROM options are:")
        PrintConsole("[I2C EEPROM DEVICES]")
        Dim n As New I2C_Programmer(Nothing)
        For Each dev In n.I2C_EEPROM_LIST
            PrintConsole(dev.NAME)
        Next
        PrintConsole("[SERIAL EEPROM DEVICES]")
        Dim SPI_EEPROM_LIST = MainApp.GetDevices_SERIAL_EEPROM()
        For Each dev In SPI_EEPROM_LIST
            Dim spi_part As String = dev.NAME.Substring(dev.NAME.IndexOf(" ") + 1)
            PrintConsole(spi_part)
        Next
        PrintConsole("[PARALLEL EEPROM DEVICES]")
        Dim PAR_EEPROM_LIST = MainApp.GetDevices_PARALLEL_EEPROM()
        For Each dev In PAR_EEPROM_LIST
            Dim par_part As String = dev.NAME.Substring(dev.NAME.IndexOf(" ") + 1)
            PrintConsole(par_part)
        Next
    End Sub

    Public Sub PrintConsole(Line As String)
        Try
            If ProgressBarEnabled Then
                Console.SetCursorPosition(0, Console.CursorTop - 1)
                Console.WriteLine(Line.PadRight(80))
                Console.WriteLine("")
                Progress_Set(ProgressBarLast)
                If Not String.IsNullOrEmpty(ProgressBarLastSpeed) Then
                    Progress_UpdateSpeed(ProgressBarLastSpeed)
                End If
            Else
                Console.WriteLine(Line)
            End If
            ConsoleLog.Add(Line)
        Catch
        End Try
    End Sub

#Region "Progress Bar"
    Private ProgressBarEnabled As Boolean = False
    Private ProgressBarLast As Integer = 0
    Private ProgressBarLastSpeed As String = ""

    Public Sub Progress_Create()
        PrintConsole("")
        ProgressBarEnabled = True
        Progress_Set(0)
    End Sub

    Public Sub Progress_Remove()
        ProgressBarEnabled = False
        Console.SetCursorPosition(0, Console.CursorTop - 1)
        Console.WriteLine("".PadRight(80))
        Console.SetCursorPosition(0, Console.CursorTop - 1)
    End Sub

    Public Sub Progress_Set(percent As Integer)
        If Not ProgressBarEnabled Then Return
        If (percent > 100) Then percent = 100
        Threading.Thread.CurrentThread.Join(10) ' Pump a message
        Dim line_out As String = String.Format("[{0}% complete]", percent.ToString().PadLeft(3, " "c))
        Console.SetCursorPosition(0, Console.CursorTop - 1)
        Console.Write(line_out)
        Console.SetCursorPosition(0, Console.CursorTop + 1)
        ProgressBarLast = percent
    End Sub

    Public Sub Progress_UpdateSpeed(speed_str As String)
        If Not ProgressBarEnabled Then
            Return
        End If
        Try
            ProgressBarLastSpeed = speed_str
            Dim line_out = " [" & speed_str & "]          "
            Console.SetCursorPosition(15, Console.CursorTop - 1)
            Console.Write(line_out)
            Console.SetCursorPosition(0, Console.CursorTop + 1)
        Catch
        End Try
    End Sub

#End Region

End Class
