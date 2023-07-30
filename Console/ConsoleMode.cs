using SPI;
using FlashMemory;
using System;
using System.IO;
using System.Collections.Generic;
using static MainApp;

public class ConsoleMode {

    private List<string> ConsoleLog = new List<string>();
    public ConsoleOperation MyOperation = new ConsoleOperation();

    private delegate void UpdateFunction_Progress(int percent);

    private delegate void UpdateFunction_SpeedLabel(string speed_str);

    public bool device_connected { get; set; }

    public partial class ConsoleOperation {
        public ConsoleTask CurrentTask { get; set; } = ConsoleTask.NoTask;
        public DeviceMode Mode { get; set; } = DeviceMode.Unspecified;
        public bool VERIFY { get; set; } = true;
        public string FILENAME { get; set; } // The filename to write to or read from
        public FileInfo FILE_IO { get; set; }
        public long FILE_LENGTH { get; set; } = 0; // Optional file length argument
        public long FLASH_OFFSET { get; set; } = 0U;
        public FlashArea FLASH_AREA { get; set; } = FlashArea.Main;
        public string SPI_EEPROM { get; set; }
        public SPI_SPEED SPI_FREQ { get; set; } = SPI_SPEED.MHZ_8;
        public int I2C_EEPROM { get; set; }
        public bool LogOutput { get; set; } = false;
        public bool LogAppendFile { get; set; } = false;
        public string LogFilename { get; set; } = "FlashcatUSB_Console.txt";
        public bool ExitConsole { get; set; } = false; // Closes the console window when complete
    }

    public void Start(params string[] Args) {
        try {
            PrintConsole("Welcome to FlashcatUSB, build: " + MainApp.FC_BUILD.ToString());
            PrintConsole("Copyright " + DateTime.Now.Year + " - Embedded Computers LLC");
            PrintConsole("Running on: " + Environment.OSVersion.Platform + " (" + MainApp.GetOsBitsString() + ")");
            if (MainApp.LicenseStatus ==  LicenseStatusEnum.LicensedValid ) {
                PrintConsole("License status: valid (expires " + MySettings.LICENSE_EXP.ToShortDateString() + ")");
                PrintConsole("License to: " + MySettings.LICENSED_TO);
            } else {
                PrintConsole("License status: non-commercial use only");
            }
            Environment.ExitCode = 0;
            if (Args.Length == 0) {
                Console_DisplayHelp();
            } else {
                MyOperation = ConsoleMode_ParseSwitches(Args);
                if (MyOperation is null) { return; }
                if (!ConsoleMode_SetupOperation()) { return; }
                PrintConsole("Waiting to connect to FlashcatUSB");
                while (!device_connected) {
                    if (MainApp.AppIsClosing) { return; }
                    Utilities.Sleep(100);
                }
                ConsoleMode_RunTask();
            }
        } catch {
        } finally {
            MainApp.AppClosing();
            Console_Exit();
        }
    }

    public string Console_Ask(string question) {
        if (ProgressBarEnabled) {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.WriteLine(question.PadRight(80));
            Console.WriteLine("");
            Progress_Set(ProgressBarLast);
            if (!String.IsNullOrEmpty(ProgressBarLastSpeed)) {
                Progress_UpdateSpeed(ProgressBarLastSpeed);
            }
            Console.SetCursorPosition(question.Length + 1, Console.CursorTop - 2);
            string result = Console.ReadLine();
            Console.SetCursorPosition(0, Console.CursorTop + 1);
            return result;
        } else {
            Console.Write(question + " ");
            return Console.ReadLine();
        }
    }

    private ConsoleOperation ConsoleMode_ParseSwitches(string[] args) {
        var MyConsoleParams = new ConsoleOperation();
        switch (args[0].ToUpper() ?? "") {
            case "-H":
            case "-?":
            case "-HELP": {
                    MyConsoleParams.CurrentTask = ConsoleTask.Help;
                    break;
                }
            case "-CHECK": {
                    MyConsoleParams.CurrentTask = ConsoleTask.Check;
                    break;
                }
            case "-READ": {
                    MyConsoleParams.CurrentTask = ConsoleTask.ReadMemory;
                    break;
                }
            case "-WRITE": {
                    MyConsoleParams.CurrentTask = ConsoleTask.WriteMemory;
                    break;
                }
            case "-ERASE": {
                    MyConsoleParams.CurrentTask = ConsoleTask.EraseMemory;
                    break;
                }
            case "-EXECUTE": {
                    MyConsoleParams.CurrentTask = ConsoleTask.ExecuteScript;
                    break;
                }
            default: {
                    this.PrintConsole("OPERATION not specified (i.e. -READ or -WRITE)");
                    return null;
                }
        }
        if (args.Length > 1) {
            if (args[1].ToUpper() == "-SPI") {
                MyConsoleParams.Mode = DeviceMode.SPI;
            } else if (args[1].ToUpper() == "-SQI") {
                MyConsoleParams.Mode = DeviceMode.SQI;
            } else if (args[1].ToUpper() == "-SPINAND") {
                MyConsoleParams.Mode = DeviceMode.SPI_NAND;
            } else if (args[1].ToUpper() == "-SPIEEPROM") {
                MyConsoleParams.Mode = DeviceMode.SPI_EEPROM;
            } else if (args[1].ToUpper() == "-I2C") {
                MyConsoleParams.Mode = DeviceMode.I2C_EEPROM;
            } else if (args[1].ToUpper() == "-SWI") {
                MyConsoleParams.Mode = DeviceMode.ONE_WIRE;
            } else if (args[1].ToUpper() == "-PNOR") {
                MyConsoleParams.Mode = DeviceMode.PNOR;
            } else if (args[1].ToUpper() == "-PNAND") {
                MyConsoleParams.Mode = DeviceMode.PNAND;
            } else if (args[1].ToUpper() == "-MICROWIRE") {
                MyConsoleParams.Mode = DeviceMode.Microwire;
            } else if (args[1].ToUpper() == "-FWH") {
                MyConsoleParams.Mode = DeviceMode.FWH;
            } else if (args[1].ToUpper() == "-HYPERFLASH") {
                MyConsoleParams.Mode = DeviceMode.HyperFlash;
            } else if (args[1].ToUpper() == "-EPROM") {
                MyConsoleParams.Mode = DeviceMode.EPROM;
            } else if (args[1].ToUpper() == "-JTAG") {
                MyConsoleParams.Mode = DeviceMode.JTAG;
            } else {
                Environment.ExitCode = -1;
                this.PrintConsole("MODE not specified (i.e. -SPI or -I2C)");
                return null;
            }
        }
        for (int i = 2, loopTo = args.Length - 1; i <= loopTo; i++) { // Load other options
            bool last_option = i == args.Length - 1;
            switch (args[i].ToUpper() ?? "") {
                case "-FILE": {
                        if (last_option || args[i + 1].StartsWith("-")) {
                            PrintConsole(string.Format("You must specify a value following {0}", args[i]));
                            return null;
                        }
                        MyConsoleParams.FILENAME = args[i + 1];
                        i += 1;
                        break;
                    }
                case "-LOG": {
                        if (last_option || args[i + 1].StartsWith("-")) {
                            PrintConsole(string.Format("You must specify a value following {0}", args[i]));
                            return null;
                        }
                        MyConsoleParams.LogFilename = args[i + 1];
                        i += 1;
                        MyConsoleParams.LogOutput = true;
                        break;
                    }
                case "-MHZ": {
                        if (last_option || args[i + 1].StartsWith("-")) {
                            PrintConsole(string.Format("You must specify a value following {0}", args[i]));
                            return null;
                        }
                        string speed = args[i + 1];
                        i += 1;
                        if (Utilities.IsNumeric(speed) && (Int32.Parse(speed) >= 1 && Int32.Parse(speed) <= 48)) {
                        } else {
                            PrintConsole("MHZ value must be between 1 and 48");
                            return null;
                        }
                        MyConsoleParams.SPI_FREQ = (SPI_SPEED)(Int32.Parse(speed) * 1000000);
                        break;
                    }
                case "-AREA": {
                        if (last_option || args[i + 1].StartsWith("-")) {
                            PrintConsole(string.Format("You must specify a value following {0}", args[i]));
                            return null;
                        }
                        string area_option = args[i + 1];
                        i += 1;
                        if (area_option.ToUpper().Equals("MAIN")) {
                            MyConsoleParams.FLASH_AREA = FlashArea.Main;
                        } else if (area_option.ToUpper().Equals("SPARE")) {
                            MyConsoleParams.FLASH_AREA = FlashArea.OOB;
                        } else if (area_option.ToUpper().Equals("ALL")) {
                            MyConsoleParams.FLASH_AREA = FlashArea.All;
                        } else {
                            PrintConsole("-AREA option not valid; must specify: main, spare or all");
                            return null;
                        }
                        break;
                    }
                case "-LOGAPPEND": {
                        MyConsoleParams.LogAppendFile = true;
                        break;
                    }
                case "-OFFSET": {
                        if (last_option || args[i + 1].StartsWith("-")) {
                            PrintConsole(string.Format("You must specify a value following {0}", args[i]));
                            return null;
                        }
                        string offset_value = args[i + 1];
                        i += 1;
                        if (!Utilities.IsDataType.HexString(offset_value) && !Utilities.IsNumeric(offset_value)) {
                            PrintConsole(string.Format("{0} value must be numeric or hexadecimal", args[i]));
                            return null;
                        }
                        if (Utilities.IsNumeric(offset_value)) {
                            MyConsoleParams.FLASH_OFFSET = UInt32.Parse(offset_value);
                        } else if (Utilities.IsDataType.HexString(offset_value)) {
                            MyConsoleParams.FLASH_OFFSET = UInt32.Parse(offset_value);
                        }

                        break;
                    }
                case "-LENGTH": {
                        if (last_option || args[i + 1].StartsWith("-")) {
                            PrintConsole(string.Format("You must specify a value following {0}", args[i]));
                            return null;
                        }
                        string offset_value = args[i + 1];
                        i += 1;
                        if (!Utilities.IsDataType.HexString(offset_value) && !Utilities.IsNumeric(offset_value)) {
                            PrintConsole(string.Format("{0} value must be numeric or hexadecimal", args[i]));
                            return null;
                        }
                        if (Utilities.IsNumeric(offset_value)) {
                            MyConsoleParams.FILE_LENGTH = Int32.Parse(offset_value);
                        } else if (Utilities.IsDataType.HexString(offset_value)) {
                            MyConsoleParams.FILE_LENGTH = Int32.Parse(offset_value);
                        }
                        break;
                    }
                case "-VERIFY_OFF": {
                        MyConsoleParams.VERIFY = false;
                        break;
                    }
                case "-EXIT": {
                        MyConsoleParams.ExitConsole = true;
                        break;
                    }
                case "-ADDRESS": {
                        if (last_option || args[i + 1].StartsWith("-")) {
                            PrintConsole(string.Format("You must specify a value following {0}", args[i]));
                            return null;
                        }
                        string offset_value = args[i + 1];
                        i += 1;
                        if (!Utilities.IsDataType.HexString(offset_value) && !Utilities.IsNumeric(offset_value)) {
                            PrintConsole(string.Format("{0} value must be numeric or hexadecimal", args[i]));
                            return null;
                        }
                        if (Utilities.IsNumeric(offset_value)) {
                            MySettings.I2C_ADDRESS = (byte)(UInt32.Parse(offset_value) & 255L);
                        } else if (Utilities.IsDataType.HexString(offset_value)) {
                            MySettings.I2C_ADDRESS = (byte)(UInt32.Parse(offset_value) & 255L);
                        }
                        break;
                    }
                case "-EEPROM": {
                        if (last_option || args[i + 1].StartsWith("-")) {
                            PrintConsole(string.Format("You must specify a value following {0}", args[i]));
                            return null;
                        }
                        string eeprom_str = args[i + 1];
                        i += 1;
                        bool Device_Found = false;
                        var I2C_IF = new I2C_Programmer(null);
                        int index = 0;
                        foreach (var i2c_device in I2C_IF.I2C_EEPROM_LIST) {
                            if (eeprom_str.ToUpper().Equals(i2c_device.NAME.ToUpper())) {
                                MyConsoleParams.I2C_EEPROM = index;
                                Device_Found = true;
                                break;
                            }
                            index += 1;
                        }
                        if (!Device_Found) {
                            var SPI_EEPROM_LIST = GetDevices_SPI_EEPROM();
                            foreach (var dev in SPI_EEPROM_LIST) {
                                string spi_part = dev.NAME.Substring(dev.NAME.IndexOf(" ") + 1);
                                if (dev.NAME.ToUpper().Equals(eeprom_str.ToUpper()) || spi_part.ToUpper().Equals(eeprom_str.ToUpper())) {
                                    MyConsoleParams.SPI_EEPROM = dev.NAME;
                                    Device_Found = true;
                                    break;
                                }
                            }
                        }
                        if (!Device_Found) {
                            this.PrintConsole("The EEPROM device you specified was not found");
                            Console_PrintEEPROMList();
                            return null;
                        }
                        break;
                    }
                default: {
                        PrintConsole(string.Format("Option not recognized: {0}", args[i]));
                        break;
                    }
            }
        }

        return MyConsoleParams;
    }

    private bool ConsoleMode_SetupOperation() {
        switch (MyOperation.CurrentTask) {
            case ConsoleTask.Help: {
                    Console_DisplayHelp();
                    return false;
                }
            case ConsoleTask.Check: {
                    ConsoleMode_Check();
                    return false;
                }
            case ConsoleTask.ReadMemory: {
                    if (string.IsNullOrEmpty(MyOperation.FILENAME)) {
                        Environment.ExitCode = -1;
                        var msg = "Operation ReadMemory requires option -FILE to specify where to save to";
                        this.PrintConsole(msg);
                        return false;
                    }
                    MyOperation.FILE_IO = new FileInfo(MyOperation.FILENAME);
                    break;
                }
            case ConsoleTask.WriteMemory: {
                    if (string.IsNullOrEmpty(MyOperation.FILENAME)) {
                        Environment.ExitCode = -1;
                        var msg = "Operation WriteMemory requires option -FILE to specify where to save to";
                        this.PrintConsole(msg);
                        return false;
                    }
                    MyOperation.FILE_IO = new FileInfo(MyOperation.FILENAME);
                    if (!MyOperation.FILE_IO.Exists) {
                        Environment.ExitCode = -1;
                        this.PrintConsole("Error: file not found" + ": " + MyOperation.FILENAME);
                        return false;
                    }
                    break;
                }
            case ConsoleTask.ExecuteScript: {
                    if (string.IsNullOrEmpty(MyOperation.FILENAME)) {
                        Environment.ExitCode = -1;
                        var msg = "Operation ExecuteScript requires option -FILE to specify which script to run";
                        this.PrintConsole(msg);
                        return false;
                    }
                    var running_exe = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    MyOperation.FILE_IO = new FileInfo(running_exe.DirectoryName + @"\Scripts\" + MyOperation.FILENAME);
                    if (!MyOperation.FILE_IO.Exists) {
                        MyOperation.FILE_IO = new FileInfo(MyOperation.FILENAME);
                        if (!MyOperation.FILE_IO.Exists) {
                            Environment.ExitCode = -1;
                            this.PrintConsole("Error: file not found" + ": " + MyOperation.FILE_IO.FullName);
                            return false;
                        }
                    }
                    ScriptEngine.PrintConsole += this.PrintConsole;
                    break;
                }
        }
        return true;
    }

    private DetectParams GetDeviceParams() {
        DetectParams my_params;
        my_params.OPER_MODE = MyOperation.Mode;
        my_params.SPI_AUTO = true;
        my_params.SPI_CLOCK = MyOperation.SPI_FREQ;
        my_params.SQI_CLOCK = SQI_SPEED.MHZ_10;
        my_params.SPI_EEPROM = MyOperation.SPI_EEPROM;
        my_params.I2C_INDEX = MyOperation.I2C_EEPROM;
        my_params.I2C_SPEED = I2C_SPEED_MODE._400kHz;
        my_params.I2C_ADDRESS = 0xA0;
        my_params.NOR_READ_ACCESS = MainApp.MySettings.NOR_READ_ACCESS;
        my_params.NOR_WE_PULSE = MainApp.MySettings.NOR_WE_PULSE;
        my_params.NAND_Layout = MainApp.MySettings.NAND_Layout;
        return my_params;
    }

    private void ConsoleMode_RunTask() {
        bool operation_error = false;
        if (MAIN_FCUSB is null) {
            Environment.ExitCode = -1;
            this.PrintConsole("Error: Unable to connect to FlashcatUSB");
            return;
        }
        var supported_modes = MainApp.GetSupportedModes(MAIN_FCUSB);
        if (Array.IndexOf(supported_modes, MyOperation.Mode)==-1) {
            Environment.ExitCode = -1;
            this.PrintConsole("Hardware does not support the selected MODE");
            return;
        }
        if (!DetectDevice(MAIN_FCUSB, GetDeviceParams())) {
            Environment.ExitCode = -1;
            this.PrintConsole("Unable to perform any actions because there are no detected memory devices");
            return;
        }
        var mem_dev = MEM_IF.GetDevice(0U); // Possibly catch error here
        mem_dev.FCUSB.PARALLEL_NAND_IF.MemoryArea = MyOperation.FLASH_AREA;
        mem_dev.FCUSB.SPI_NAND_IF.MemoryArea = MyOperation.FLASH_AREA;
        mem_dev.Size = MAIN_FCUSB.PROGRAMMER.DeviceSize;
        switch (MyOperation.CurrentTask) {
            case ConsoleTask.ReadMemory: {
                    operation_error = this.ConsoleMode_RunTask_ReadMemory(mem_dev);
                    break;
                }
            case ConsoleTask.WriteMemory: {
                    operation_error = this.ConsoleMode_RunTask_WriteMemory(mem_dev);
                    break;
                }
            case ConsoleTask.EraseMemory: {
                    operation_error = this.ConsoleMode_RunTask_EraseMemory(mem_dev);
                    break;
                }
            case ConsoleTask.ExecuteScript: {
                    operation_error = this.ConsoleMode_RunTask_ExecuteScript(mem_dev);
                    break;
                }
        }
        PrintConsole("----------------------------------------------");
        this.PrintConsole("Application completed");
        if (MyOperation.LogOutput) {
            if (MyOperation.LogAppendFile) {
                Utilities.FileIO.AppendFile(ConsoleLog.ToArray(), MyOperation.LogFilename);
            } else {
                Utilities.FileIO.WriteFile(ConsoleLog.ToArray(), MyOperation.LogFilename);
            }
        }
        if (operation_error) {
            MyOperation.ExitConsole = false;
        }
    }

    private bool ConsoleMode_RunTask_ReadMemory(MemoryInterface.MemoryDeviceInstance mem_dev) {
        try {
            Progress_Create();
            if (MyOperation.FLASH_OFFSET > mem_dev.Size) {
                MyOperation.FLASH_OFFSET = 0U; // Out of bounds
            }
            if (MyOperation.FILE_LENGTH == 0 | MyOperation.FLASH_OFFSET + MyOperation.FILE_LENGTH > mem_dev.Size) {
                MyOperation.FILE_LENGTH = (mem_dev.Size - MyOperation.FLASH_OFFSET);
            }
            var cb = new MemoryInterface.MemoryDeviceInstance.StatusCallback();
            cb.UpdatePercent = new UpdateFunction_Progress(Progress_Set);
            cb.UpdateSpeed = new UpdateFunction_SpeedLabel(Progress_UpdateSpeed);
            var f_params = new ReadParameters();
            using (var ms = MyOperation.FILE_IO.OpenWrite()) {
                f_params.Address = MyOperation.FLASH_OFFSET;
                f_params.Count = MyOperation.FILE_LENGTH;
                f_params.Status = cb;
                mem_dev.ReadStream(ms, f_params);
            }
            PrintConsole(string.Format("Saved data to: {0}", MyOperation.FILE_IO.FullName));
            return true;
        } catch {
            return false;
        } finally {
            Progress_Remove();
        }
    }

    private bool ConsoleMode_RunTask_WriteMemory(MemoryInterface.MemoryDeviceInstance mem_dev) {
        try {
            Progress_Create();
            if (MyOperation.FLASH_OFFSET > mem_dev.Size)
            MyOperation.FLASH_OFFSET = 0U; // Out of bounds
            uint max_write_count = (uint)Math.Min(mem_dev.Size, MyOperation.FILE_IO.Length);
            if (MyOperation.FILE_LENGTH == 0) {
                MyOperation.FILE_LENGTH = (int)max_write_count;
            } else if (MyOperation.FILE_LENGTH > max_write_count) {
                MyOperation.FILE_LENGTH = (int)max_write_count;
            }
            var data_out = Utilities.FileIO.ReadBytes(MyOperation.FILE_IO.FullName, MyOperation.FILE_LENGTH);
            if (data_out is null || data_out.Length == 0) {
                Environment.ExitCode = -1;
                this.PrintConsole("Error: Write was not successful because there is no data to write");
                return false;
            }
            string verify_str = "enabled";
            if (!MyOperation.VERIFY)
                verify_str = "disabled";
            PrintConsole("Performing WRITE of " + MyOperation.FILE_LENGTH + " bytes at offset 0x" + MyOperation.FLASH_OFFSET.ToString("X") + " with verify " + verify_str);
            Array.Resize(ref data_out, (int)MyOperation.FILE_LENGTH);
            var cb = new MemoryInterface.MemoryDeviceInstance.StatusCallback();
            cb.UpdatePercent = new UpdateFunction_Progress(Progress_Set);
            cb.UpdateSpeed = new UpdateFunction_SpeedLabel(Progress_UpdateSpeed);
            bool write_result = mem_dev.WriteBytes(MyOperation.FLASH_OFFSET, data_out, MyOperation.VERIFY, cb);
            if (write_result) {
                this.PrintConsole("Write operation was successful");
                return true;
            } else {
                this.PrintConsole("Error, write operation was not successful");
            }
            return false;
        } catch {
            return false;
        } finally {
            Progress_Remove();
        }
    }

    private bool ConsoleMode_RunTask_EraseMemory(MemoryInterface.MemoryDeviceInstance mem_dev) {
        try {
            Progress_Create();
            if (mem_dev.EraseFlash()) {
                this.PrintConsole("Memory device erased successfully");
                return true;
            } else {
                this.PrintConsole("Error: erasing device failed");
                return false;
            }
        } catch {
            return false;
        } finally {
            Progress_Remove();
        }
    }

    private bool ConsoleMode_RunTask_ExecuteScript(MemoryInterface.MemoryDeviceInstance mem_dev) {
        ScriptEngine.CURRENT_DEVICE_MODE = MyOperation.Mode;
        if (!ScriptEngine.LoadFile(MyOperation.FILE_IO)) {
            Environment.ExitCode = -1;
            return false;
        } else {
            while (ScriptEngine.IsRunning) {
                Utilities.Sleep(20);
            }
        }
        return true;
    }
    // Frees up memory and exits the application and console io calls
    private void Console_Exit() {
        if (MyOperation is Object) {
            if (!MyOperation.ExitConsole) {
                PrintConsole("----------------------------------------------");
                PrintConsole("Press any key to close");
                Console.ReadKey();
                if (MyOperation.LogOutput) {
                    if (MyOperation.LogAppendFile) {
                        Utilities.FileIO.AppendFile(ConsoleLog.ToArray(), MyOperation.LogFilename);
                    } else {
                        Utilities.FileIO.WriteFile(ConsoleLog.ToArray(), MyOperation.LogFilename);
                    }
                }
            }
        }
        if (MAIN_FCUSB is Object) {
            MAIN_FCUSB.USB_LEDOff();
            MAIN_FCUSB.Disconnect();
        }
        Environment.Exit(-1);
    }

    private void ConsoleMode_Check() {
        int counter = 0;
        while (MAIN_FCUSB == null) {
            if (MainApp.AppIsClosing) { return; }
            Utilities.Sleep(100);
            counter++;
            if (counter == 100) {
                this.PrintConsole("Waiting for connected device timedout");
                return;
            }
        }
        Utilities.Sleep(500);
        return;
    }

    private void Console_DisplayHelp() {
        PrintConsole("--------------------------------------------");
        PrintConsole("Syntax: exe [OPERATION] [MODE] (options) ...");
        PrintConsole("");
        PrintConsole("Operations:");
        PrintConsole("-read             " + "Will perform a flash memory read operation");
        PrintConsole("-write            " + "Will perform a flash memory write operation");
        PrintConsole("-erase            " + "Erases the entire memory device");
        PrintConsole("-execute          " + "Allows you to execute a FlashcatUSB script file (*.fcs)");
        PrintConsole("-check            " + "Display all connected FlashcatUSB devices");
        PrintConsole("-help             " + "Shows this dialog");
        PrintConsole("");
        PrintConsole("Supported modes:");
        PrintConsole("-SPI -SQI -SPINAND -SPIEEPROM -I2C -SWI -PNOR -PNAND");
        PrintConsole("-MICROWIRE -FWH -HYPERFLASH -EPROM -JTAG");
        PrintConsole("");
        PrintConsole("Options:");
        PrintConsole("-File (filename)  " + "Specifies the file to use for read/write/execute");
        PrintConsole("-Length (value)   " + "Specifies the number of bytes to read/write");
        PrintConsole("-MHZ (value)      " + "Specifies the MHz speed for SPI operation");
        PrintConsole("-Area (value)     " + "Specifies the region of NAND memory: main/spare/all");
        PrintConsole("-Offset (value)   " + "Specifies the offset to write the file to flash");
        PrintConsole("-EEPROM (part)    " + "Specifies a SPI/I2C EEPROM (i.e. M95080 or 24XX64)");
        PrintConsole("-Address (hex)    " + "Specifies the I2C slave address (i.e. 0xA0)");
        PrintConsole("-Verify_Off       " + "Turns off data verification for flash write operations");
        PrintConsole("-Exit             " + "Automatically close window when completed");
        PrintConsole("-Log (filename)   " + "Save the output from the console to a file");
        PrintConsole("-LogAppend        " + "Append the console text to an existing file");
    }
    // Prints the list of valid options that can be used for the -EEPROM option
    private void Console_PrintEEPROMList() {
        PrintConsole("I2C/SPI EEPROM valid options are:");
        PrintConsole("[I2C EEPROM DEVICES]");
        foreach (var dev in MAIN_FCUSB.I2C_IF.I2C_EEPROM_LIST)
            this.PrintConsole(dev.NAME);
        PrintConsole("[SPI EEPROM DEVICES]");
        var SPI_EEPROM_LIST = GetDevices_SPI_EEPROM();
        foreach (var dev in SPI_EEPROM_LIST) {
            string spi_part = dev.NAME.Substring(dev.NAME.IndexOf(" ") + 1);
            PrintConsole(spi_part);
        }
    }

    public void PrintConsole(string Line) {
        try {
            if (ProgressBarEnabled) {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine(Line.PadRight(80));
                ConsoleLog.Add(Line);
                Console.WriteLine("");
                Progress_Set(ProgressBarLast);
                if (!String.IsNullOrEmpty(ProgressBarLastSpeed)) {
                    Progress_UpdateSpeed(ProgressBarLastSpeed);
                }
            } else {
                Console.WriteLine(Line);
                ConsoleLog.Add(Line);
            }
        } catch {
        }
    }

    #region Progress Bar

    private bool ProgressBarEnabled = false;
    private int ProgressBarLast = 0;
    private string ProgressBarLastSpeed = "";
        
    public void Progress_Create() {
        PrintConsole("");
        ProgressBarEnabled = true;
        Progress_Set(0);
    }

    public void Progress_Remove() {
        ProgressBarEnabled = false;
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        Console.WriteLine("".PadRight(80));
        Console.SetCursorPosition(0, Console.CursorTop - 1);
    }

    public void Progress_Set(int percent) {
        if (!ProgressBarEnabled) { return; }
        if (percent > 100) { percent = 100; }
        System.Threading.Thread.CurrentThread.Join(10); // Pump a message
        var line_out = string.Format("[{0}% complete]", percent.ToString().PadLeft(3, ' '));
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        Console.Write(line_out);
        Console.SetCursorPosition(0, Console.CursorTop + 1);
        ProgressBarLast = percent;
    }

    public void Progress_UpdateSpeed(string speed_str) {
        if (!ProgressBarEnabled) { return; }
        try {
            ProgressBarLastSpeed = speed_str;
            var line_out = " [" + speed_str + "]          ";
            Console.SetCursorPosition(15, Console.CursorTop - 1);
            Console.Write(line_out);
            Console.SetCursorPosition(0, Console.CursorTop + 1);
        } catch {
        }
    }

    #endregion

}