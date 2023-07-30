using FlashMemory;
using SPI;
using System;
using System.Collections.Generic;
using System.Threading;
using USB;
using static MemoryInterface;

public static class MainApp {
    public static string MyLocation = System.Reflection.Assembly.GetEntryAssembly().Location;
    public static FlashDatabase FlashDatabase = new FlashDatabase();
    public static FlashcatSettings MySettings = new FlashcatSettings();
    public static double FC_BUILD = 1.03;
    private const float PRO_PCB5_FW = 1.09f;
    private const float MACH1_PCB2_FW = 2.22f;
    private const float XPORT_PCB2_FW = 5.2f;
    private const float CLASSIC_FW = 4.52f;
    private const uint MACH1_FGPA_3V3 = 0xAF330006U;
    private const uint MACH1_FGPA_1V8 = 0xAF180006U;
    private const uint MACH1_SPI_3V3 = 0xAF330101U; 
    private const uint MACH1_SPI_1V8 = 0xAF180102U; 
    public static bool AppIsClosing = false;
    public static FlashcatScript.Processor ScriptEngine = new FlashcatScript.Processor();
    public static HostClient USBCLIENT = new HostClient();
    public static string ScriptPath = MyLocation + @"\Scripts\";
    public static SPI_NOR CUSTOM_SPI_DEV;
    public static MemoryInterface MEM_IF = new MemoryInterface();
    public static FCUSB_DEVICE MAIN_FCUSB = null;
    public static ConsoleMode MyConsole = null;
    public static NAND_LAYOUT_TOOL NAND_LayoutTool;

    public static object GUI = null;    //In future, we can place a GUI form obj here

    static void Main(string[] args) {

        //args = new[] { "-READ", "-SPI", "-MHZ", "24", "-FILE", "Flash.bin", "-LENGTH", "1000000" };
        //args = new[] { "-CHECK" };
        //args = new[] { "-READ", "-PNAND", "-File", "nand_dump.bin" };

        LicenseSystem_Init();
        CUSTOM_SPI_DEV = new SPI_NOR("User-defined", VCC_IF.SERIAL_3V, 1048576, 0, 0);
        CreateGrayCodeTable();
        if (NAND_ECC_CFG is null) { NAND_ECC_CFG = GenerateLocalEccConfigurations(); }       
        USBCLIENT.DeviceConnected += OnUsbDevice_Connected;
        USBCLIENT.DeviceDisconnected += OnUsbDevice_Disconnected;
        USBCLIENT.StartService();
        
        MyConsole = new ConsoleMode();
        MyConsole.Start(args);

        MAIN_FCUSB.UpdateProgress += OnDeviceUpdateProgress;
    }

    public static void PrintConsole(string msg, bool set_status = false) {
        if (AppIsClosing) { return; }
        MyConsole.PrintConsole(msg);
    }

    public static void PrintConsole(string msg) {
        PrintConsole(msg, false);
    }

    public static void SetStatus(string msg) {
        PrintConsole(msg, true);
    }

    #region License System

    public static LicenseStatusEnum LicenseStatus { get; set; } = LicenseStatusEnum.NotLicensed;

    public static void LicenseSystem_Init() {
        try {
            if (MySettings.LICENSED_TO.Equals("")) {
                LicenseStatus = LicenseStatusEnum.NotLicensed;
            } else if (MySettings.LICENSE_EXP.Date.Year == 1) {
                LicenseStatus = LicenseStatusEnum.LicensedValid;
            } else if (DateTime.Compare(DateTime.Now, MySettings.LICENSE_EXP.Date) < 0) {
                LicenseStatus = LicenseStatusEnum.LicensedValid;
            } else {
                LicenseStatus = LicenseStatusEnum.LicenseExpired;
            }
        } catch {
        }
        if (LicenseStatus == LicenseStatusEnum.LicenseExpired) {
            if (License_LoadKey(MySettings.LICENSE_KEY)) // This will update existing license if its been renewed
            {
                if (DateTime.Compare(DateTime.Now, MySettings.LICENSE_EXP) < 0) {
                    LicenseStatus = LicenseStatusEnum.LicensedValid;
                }
            }
            if (LicenseStatus == LicenseStatusEnum.LicenseExpired) {
                string msg = "The license for this software has expired, please consider ";
                msg += "renewing your license. If you need assistance, email license@embeddedcomputers.net\n\r";
                msg += "Thank you";
                PrintConsole(msg);
            }
        }
        if (!(LicenseStatus == LicenseStatusEnum.LicensedValid)) {
            MySettings.ECC_FEATURE_ENABLED = false;
        }
    }

    public static bool License_LoadKey(string key) {
        byte[] w = Utilities.DownloadFile("https://www.embeddedcomputers.net/licensing/index.php?key=" + key);
        if (w is object && w.Length > 0) {
            string response = Utilities.Bytes.ToChrString(w).Replace("\r", "").Replace("\n", "");
            if (response.Equals("ERROR"))
                return false;
            var result = response.Split("\t");
            string data_str = result[1];
            var LicensedDate = new DateTime();
            if (!data_str.Equals("01/01/0001")) {
                LicensedDate = DateTime.Parse(data_str);
            }
            if (LicensedDate.Date.Year == 1) {
                LicenseStatus = LicenseStatusEnum.LicensedValid;
            } else if (DateTime.Compare(DateTime.Now, LicensedDate) < 0) {
                LicenseStatus = LicenseStatusEnum.LicensedValid;
            } else {
                return false;
            }
            MySettings.LICENSED_TO = result[0];
            MySettings.LICENSE_KEY = key;
            MySettings.LICENSE_EXP = LicensedDate;
            return true;
        }
        return false;
    }

    #endregion

    #region Error correcting code
    public static ECC_LIB.ECC_Configuration_Entry[] NAND_ECC_CFG;
    public static ECC_LIB.Engine NAND_ECC;

    public static ECC_LIB.ECC_DECODE_RESULT ECC_LAST_RESULT { get; set; } = ECC_LIB.ECC_DECODE_RESULT.NoErrors;

    public static void ECC_LoadConfiguration(int page_size, int spare_size) {
        NAND_ECC = null;
        if (NAND_ECC_CFG is null || !MySettings.ECC_FEATURE_ENABLED)
            return;
        PrintConsole("ECC Feature is enabled");
        for (int i = 0, loopTo = NAND_ECC_CFG.Length - 1; i <= loopTo; i++) {
            if (NAND_ECC_CFG[i].IsValid()) {
                if (NAND_ECC_CFG[i].PageSize == page_size && NAND_ECC_CFG[i].SpareSize == spare_size) {
                    NAND_ECC = new ECC_LIB.Engine(NAND_ECC_CFG[i].Algorithm, NAND_ECC_CFG[i].BitError, NAND_ECC_CFG[i].SymSize);
                    NAND_ECC.REVERSE_ARRAY = NAND_ECC_CFG[i].ReverseData;
                    NAND_ECC.ECC_DATA_LOCATION = NAND_ECC_CFG[i].EccRegion;
                    PrintConsole("Compatible profile found at index " + (i + 1).ToString());
                    return;
                }
            }
        }
        PrintConsole("No compatible profile found");
    }

    public static ECC_LIB.ECC_Configuration_Entry[] GenerateLocalEccConfigurations() {
        var cfg_list = new List<ECC_LIB.ECC_Configuration_Entry>();
        var n1 = new ECC_LIB.ECC_Configuration_Entry();
        n1.PageSize = 512;
        n1.SpareSize = 16;
        n1.Algorithm = ECC_LIB.ecc_algorithum.hamming;
        n1.BitError = 1;
        n1.SymSize = 0;
        n1.ReverseData = false;
        n1.AddRegion(0xD);
        var n2 = new ECC_LIB.ECC_Configuration_Entry();
        n2.PageSize = 2048;
        n2.SpareSize = 64;
        n2.Algorithm = ECC_LIB.ecc_algorithum.reedsolomon;
        n2.BitError = 4;
        n2.SymSize = 9;
        n2.ReverseData = false;
        n2.AddRegion(0x7);
        n2.AddRegion(0x17);
        n2.AddRegion(0x27);
        n2.AddRegion(0x37);
        var n3 = new ECC_LIB.ECC_Configuration_Entry();
        n3.PageSize = 2048;
        n3.SpareSize = 128;
        n3.Algorithm = ECC_LIB.ecc_algorithum.bhc;
        n3.BitError = 8;
        n3.SymSize = 0;
        n3.ReverseData = false;
        n3.AddRegion(0x13);
        n3.AddRegion(0x33);
        n3.AddRegion(0x53);
        n3.AddRegion(0x73);
        cfg_list.Add(n1);
        cfg_list.Add(n2);
        cfg_list.Add(n3);
        return cfg_list.ToArray();
    }

    #endregion

    #region Bit Swapping / Endian Feature

    public static void BitSwap_Forward(ref byte[] data)
    {
        switch (MySettings.BIT_ENDIAN)
        {
            case BitEndianMode.BigEndian16:
                {
                    Utilities.ChangeEndian16_MSB(ref data);
                    break;
                }

            case BitEndianMode.LittleEndian32_8bit:
                {
                    Utilities.ChangeEndian32_LSB8(ref data);
                    break;
                }

            case BitEndianMode.LittleEndian32_16bit:
                {
                    Utilities.ChangeEndian32_LSB16(ref data);
                    break;
                }
        }

        switch (MySettings.BIT_SWAP)
        {
            case BitSwapMode.Bits_8:
                {
                    Utilities.ReverseBits_Byte(ref data);
                    break;
                }

            case BitSwapMode.Bits_16:
                {
                    Utilities.ReverseBits_HalfWord(ref data);
                    break;
                }

            case BitSwapMode.Bits_32:
                {
                    Utilities.ReverseBits_Word(ref data);
                    break;
                }
        }
    }

    public static void BitSwap_Reverse(ref byte[] data)
    {
        switch (MySettings.BIT_SWAP)
        {
            case BitSwapMode.Bits_8:
                {
                    Utilities.ReverseBits_Byte(ref data);
                    break;
                }

            case BitSwapMode.Bits_16:
                {
                    Utilities.ReverseBits_HalfWord(ref data);
                    break;
                }

            case BitSwapMode.Bits_32:
                {
                    Utilities.ReverseBits_Word(ref data);
                    break;
                }
        }

        switch (MySettings.BIT_ENDIAN)
        {
            case BitEndianMode.BigEndian16:
                {
                    Utilities.ChangeEndian16_MSB(ref data);
                    break;
                }

            case BitEndianMode.LittleEndian32_8bit:
                {
                    Utilities.ChangeEndian32_LSB8(ref data);
                    break;
                }

            case BitEndianMode.LittleEndian32_16bit:
                {
                    Utilities.ChangeEndian32_LSB16(ref data);
                    break;
                }
        }
    }

    public static int BitSwap_Offset()
    {
        int bits_needed = 0;
        switch (MySettings.BIT_SWAP)
        {
            case BitSwapMode.Bits_16:
                {
                    bits_needed = 2;
                    break;
                }

            case BitSwapMode.Bits_32:
                {
                    bits_needed = 4;
                    break;
                }
        }

        switch (MySettings.BIT_ENDIAN)
        {
            case BitEndianMode.BigEndian16:
                {
                    bits_needed = 4;
                    break;
                }

            case BitEndianMode.LittleEndian32_16bit:
                {
                    bits_needed = 4;
                    break;
                }

            case BitEndianMode.LittleEndian32_8bit:
                {
                    bits_needed = 4;
                    break;
                }
        }

        return bits_needed;
    }

    public static byte[] gray_code_table_reverse = new byte[256];
    public static byte[] gray_code_table = new byte[256];

    public static void CreateGrayCodeTable() {
        for (int i = 0; i <= 255; i++) {
            var data_in = new byte[] { (byte)(i >> 1 ^ i) };
            gray_code_table[i] = data_in[0];
            Utilities.ReverseBits_Byte(ref data_in);
            gray_code_table_reverse[i] = data_in[0];
        }
    }

    #endregion

    #region SPI Settings

    public static string GetSpiClockString(FCUSB_DEVICE usb_dev, SPI_SPEED desired_speed) {
        uint current_speed = (uint)GetMaxSpiClock(usb_dev.HWBOARD, desired_speed);
        return (current_speed / 1000000d).ToString() + " Mhz";
    }

    public static SPI_SPEED GetMaxSpiClock(FCUSB_BOARD brd, SPI_SPEED desired_speed) {
        if (brd == FCUSB_BOARD.Classic || brd == FCUSB_BOARD.XPORT_PCB2) {
            if (desired_speed >= SPI_SPEED.MHZ_8) { desired_speed = SPI_SPEED.MHZ_8; }
        } else if (brd == FCUSB_BOARD.Professional_PCB5 || brd == FCUSB_BOARD.Mach1) {
            if (desired_speed >= SPI_SPEED.MHZ_32) { desired_speed = SPI_SPEED.MHZ_32; }
        }
        if (desired_speed <= SPI_SPEED.MHZ_1) { desired_speed = SPI_SPEED.MHZ_1; }
        return desired_speed;
    }

    public static SQI_SPEED GetMaxSqiClock(FCUSB_BOARD brd, uint desired_speed) {
        switch (brd) {
            case var @case when @case == FCUSB_BOARD.Mach1:
                {
                    if (desired_speed > (uint)SQI_SPEED.MHZ_40)
                        return SQI_SPEED.MHZ_40;
                    break;
                }
            case var case1 when case1 == FCUSB_BOARD.Professional_PCB5:
                {
                    if (desired_speed > (uint)SQI_SPEED.MHZ_40)
                        return SQI_SPEED.MHZ_40;
                    break;
                }
        }
        return (SQI_SPEED)desired_speed;
    }

    public static SPI_NOR[] GetDevices_SPI_EEPROM() {
        var spi_eeprom = new List<SPI_NOR>();
        Device[] d = FlashDatabase.GetFlashDevices(MemoryType.SERIAL_NOR);
        foreach (SPI_NOR dev in d)
        {
            if (dev.ProgramMode == SPI_ProgramMode.SPI_EEPROM)
            {
                spi_eeprom.Add(dev);
            }
            else if (dev.ProgramMode == SPI_ProgramMode.Nordic)
            {
                spi_eeprom.Add(dev);
            }
        }
        return spi_eeprom.ToArray();
    }

    public static bool SPIEEPROM_Configure(string eeprom_name) {
        var all_eeprom_devices = GetDevices_SPI_EEPROM();
        SPI_NOR eeprom = default;
        foreach (var ee_dev in all_eeprom_devices) {
            if (ee_dev.NAME.Equals(eeprom_name)) {
                eeprom = ee_dev;
            }
        }
        if (eeprom is null)
            return false;
        bool nRF24_mode = false;
        MAIN_FCUSB.SPI_NOR_IF.SPIBUS_Setup(GetMaxSpiClock(MAIN_FCUSB.HWBOARD, SPI_SPEED.MHZ_1));
        switch (eeprom.NAME) {
            case "Nordic nRF24LE1":
                {
                    MAIN_FCUSB.USB_VCC_ON();
                    nRF24_mode = true;
                    break;
                }
            case "Nordic nRF24LU1+ (16KB)":
                {
                    MAIN_FCUSB.USB_VCC_ON();
                    nRF24_mode = true;
                    break;
                }
            case "Nordic nRF24LU1+ (32KB)":
                {
                    MAIN_FCUSB.USB_VCC_ON();
                    nRF24_mode = true;
                    break;
                }
        }
        if (nRF24_mode) {
            Thread.Sleep(100);
            MAIN_FCUSB.SPI_NOR_IF.SetProgPin(true); // Sets PROG.PIN to HIGH
            MAIN_FCUSB.SPI_NOR_IF.SetProgPin(false); // Sets PROG.PIN to LOW
            MAIN_FCUSB.SPI_NOR_IF.SetProgPin(true); // Sets PROG.PIN to HIGH
            Thread.Sleep(10);
            if (MAIN_FCUSB.HWBOARD == FCUSB_BOARD.Professional_PCB5) {
                MAIN_FCUSB.SPI_NOR_IF.SPIBUS_Setup(GetMaxSpiClock(MAIN_FCUSB.HWBOARD, SPI_SPEED.MHZ_8));
            } else if (MAIN_FCUSB.HWBOARD == FCUSB_BOARD.XPORT_PCB2) {
                MAIN_FCUSB.SPI_NOR_IF.SPIBUS_Setup(GetMaxSpiClock(MAIN_FCUSB.HWBOARD, SPI_SPEED.MHZ_8));
            } else {
                MAIN_FCUSB.SPI_NOR_IF.SPIBUS_Setup(GetMaxSpiClock(MAIN_FCUSB.HWBOARD, SPI_SPEED.MHZ_8));
            }
        }
        MAIN_FCUSB.SPI_NOR_IF.MyFlashDevice = eeprom;
        MAIN_FCUSB.SPI_NOR_IF.MyFlashStatus = DeviceStatus.Supported;
        if (eeprom.NAME.StartsWith("ST ")) {
            MAIN_FCUSB.SPI_NOR_IF.WriteStatusRegister(new byte[] { 0 }); // Disable BP0/BP1
        }
        return true;
    }

    #endregion

    #region USB CONNECTED EVENTS

    private static void OnUsbDevice_Connected(FCUSB_DEVICE usb_dev) {
        if (MAIN_FCUSB==null) {
            MAIN_FCUSB = usb_dev;
        } else {
            return;
        }
        NAND_ECC = null;
        MEM_IF.Clear();
        PrintConsole("Successfully connected to FlashcatUSB over USB");
        string fw_str = usb_dev.FW_VERSION;
        if (usb_dev.HWBOARD == FCUSB_BOARD.ATMEL_DFU) {
            PrintConsole("Connected to FlashcatUSB in bootloader mode", true);
            return; // No need to detect any device
        } else if (usb_dev.HWBOARD == FCUSB_BOARD.Classic) {
            if (!FirmwareCheck(fw_str, CLASSIC_FW)) { return; }
            PrintConsole(String.Format("Connected to {0}, firmware version: {1}", new[] { "FlashcatUSB Classic", fw_str}));
        } else if (usb_dev.HWBOARD == FCUSB_BOARD.XPORT_PCB2) {
            if (!FirmwareCheck(fw_str, XPORT_PCB2_FW)) { return; }
            PrintConsole(String.Format("Connected to {0}, firmware version: {1}", new[] { "FlashcatUSB XPORT", fw_str }));
        } else if (usb_dev.HWBOARD == FCUSB_BOARD.Professional_PCB5) {
            if (usb_dev.BOOTLOADER) {
                FCUSBPRO_Bootloader(usb_dev, "PCB5_Source.bin"); //Current PCB5 firmware
                return;
            }
            PrintConsole(String.Format("Connected to {0}, firmware version: {1}", new[] { "FlashcatUSB Pro (PCB 5.x)", fw_str }));
            float AvrVerSng = Utilities.StringToSingle(fw_str);
            if (AvrVerSng != PRO_PCB5_FW) {
                FCUSBPRO_RebootToBootloader(usb_dev);
                return;
            }
        } else if (usb_dev.HWBOARD == FCUSB_BOARD.Mach1) {
            if (usb_dev.BOOTLOADER) {
                FCUSBPRO_Bootloader(usb_dev, "Mach1_v2_Source.bin"); //Current PCB5 firmware
                return;
            }
            PrintConsole(String.Format("Connected to {0}, firmware version: {1}", new[] { "FlashcatUSB Mach¹", fw_str }));
            float AvrVerSng = Utilities.StringToSingle(fw_str);
            if (AvrVerSng != MACH1_PCB2_FW) {
                FCUSBPRO_RebootToBootloader(usb_dev);
                return;
            }
        } else if (usb_dev.HWBOARD == FCUSB_BOARD.NotSupported) {
            PrintConsole("Hardware version is no longer supported", true);
            return;
        }
        if (usb_dev.HWBOARD == FCUSB_BOARD.Professional_PCB5) {
            if (!FCUSBPRO_PCB5_Init(usb_dev, MyConsole.MyOperation.Mode)) {
                PrintConsole("Error: unable to load FPGA bitstream", true);
                return;
            }
        } else if (usb_dev.HWBOARD == FCUSB_BOARD.Mach1) {
            if (!FCUSBMACH1_Init(usb_dev, MyConsole.MyOperation.Mode)) { return; }
        }
        MyConsole.device_connected = true;      //We can now procede
    }
    
    private static void OnUsbDevice_Disconnected(FCUSB_DEVICE usb_dev) {
        if (MAIN_FCUSB != usb_dev)
            return;
        NAND_ECC = null;
        MEM_IF.Clear(); // Remove all devices that are on this usb port
        string msg_out;
        if ((usb_dev.HWBOARD == FCUSB_BOARD.Professional_PCB5))
            msg_out = string.Format("Disconnected from {0} device", "FlashcatUSB Pro");
        else if (usb_dev.HWBOARD == FCUSB_BOARD.Mach1)
            msg_out = string.Format("Disconnected from {0} device", "FlashcatUSB Mach¹");
        else if (usb_dev.HWBOARD == FCUSB_BOARD.XPORT_PCB2)
            msg_out = string.Format("Disconnected from {0} device", "FlashcatUSB XPORT");
        else
            msg_out = string.Format("Disconnected from {0} device", "FlashcatUSB Classic");
        PrintConsole(msg_out);
        MAIN_FCUSB = null;
    }

    public partial struct DetectParams {
        public DeviceMode OPER_MODE;
        public bool SPI_AUTO;
        public SPI_SPEED SPI_CLOCK;
        public SQI_SPEED SQI_CLOCK;
        public string SPI_EEPROM;
        public int I2C_INDEX;
        public I2C_SPEED_MODE I2C_SPEED;
        public byte I2C_ADDRESS;
        public int NOR_READ_ACCESS;
        public int NOR_WE_PULSE;
        public NandMemLayout NAND_Layout;
    }

    public static bool DetectDevice(FCUSB_DEVICE usb_dev, DetectParams Params) {
        //if (usb_dev.HWBOARD == FCUSB_BOARD.ATMEL_DFU) { return DetectDevice_DFU(usb_dev, Params); }
        usb_dev.SelectProgrammer(Params.OPER_MODE);
        ScriptEngine.CURRENT_DEVICE_MODE = Params.OPER_MODE;
        NAND_LayoutTool = new NAND_LAYOUT_TOOL(Params.NAND_Layout);
        MainApp.PrintConsole("Detecting connected Flash device...",true);
        Utilities.Sleep(100); // Allow time for USB to power up devices
        if (Params.OPER_MODE == DeviceMode.SPI) {
            return DetectDevice_SPI(usb_dev, Params);
        } else if (Params.OPER_MODE == DeviceMode.SQI) {
            return DetectDevice_SQI(usb_dev, Params);
        } else if (Params.OPER_MODE == DeviceMode.SPI_NAND) {
            return DetectDevice_SPI_NAND(usb_dev, Params);
        } else if (Params.OPER_MODE == DeviceMode.SPI_EEPROM) {
            return DetectDevice_SPI_EEPROM(usb_dev, Params);
        } else if (Params.OPER_MODE == DeviceMode.I2C_EEPROM) {
            return DetectDevice_I2C_EEPROM(usb_dev, Params);
        } else if (Params.OPER_MODE == DeviceMode.ONE_WIRE) {
            return DetectDevice_ONE_WIRE(usb_dev, Params);
        } else if (Params.OPER_MODE == DeviceMode.Microwire) {
            return DetectDevice_Microwire(usb_dev, Params);
        } else if (Params.OPER_MODE == DeviceMode.PNOR) {
            return DetectDevice_PNOR(usb_dev, Params);
        } else if (Params.OPER_MODE == DeviceMode.PNAND) {
            return DetectDevice_PNAND(usb_dev, Params);
        } else if (Params.OPER_MODE == DeviceMode.FWH) {
            return DetectDevice_FWH(usb_dev, Params);
        } else if (Params.OPER_MODE == DeviceMode.HyperFlash) {
            return DetectDevice_HyperFlash(usb_dev, Params);
        } else if (Params.OPER_MODE == DeviceMode.EPROM) {
            return DetectDevice_EPROM(usb_dev, Params);
        } else {
        } // (OTHER MODES)
        return false;
    }

    public static bool DetectDevice_SPI(FCUSB_DEVICE usb_dev, DetectParams Params) {
        if (Params.SPI_AUTO) {
            MainApp.PrintConsole("Attempting to detect SPI device (auto-detect mode)");
            usb_dev.SPI_NOR_IF.SPIBUS_Setup(GetMaxSpiClock(usb_dev.HWBOARD, SPI_SPEED.MHZ_8));
            if (usb_dev.SPI_NOR_IF.DeviceInit()) {
                usb_dev.SPI_NOR_IF.SPIBUS_Setup(GetMaxSpiClock(usb_dev.HWBOARD, Params.SPI_CLOCK));
                uint block_size = 65536U;
                if (usb_dev.HasLogic)
                    block_size = 262144U;
                Connected_Event(usb_dev, block_size);
                MainApp.PrintConsole("Detected SPI Flash on high-speed SPI port"); // "Detected SPI Flash on high-speed SPI port" 
                MainApp.PrintConsole(string.Format("Setting SPI clock to: {0}", GetSpiClockString(usb_dev, Params.SPI_CLOCK)));
                if (usb_dev.SPI_NOR_IF.W25M121AV_Mode) {
                    PrintConsole("Winbond W25M121AV Flash device detected");
                    usb_dev.SPI_NAND_IF.DeviceInit();
                    Connected_Event(usb_dev, 65536);
                }
                return true;
            } else {
                switch (usb_dev.SPI_NOR_IF.MyFlashStatus) {
                    case var @case when @case == DeviceStatus.NotDetected: {
                            MainApp.PrintConsole("Flash memory not detected on SPI NOR mode"); // "Flash memory not detected on SPI NOR mode"
                            break;
                        }
                    case var case1 when case1 == DeviceStatus.NotSupported: {
                            MainApp.PrintConsole("Flash memory detected but not found in Flash library"); // "Flash memory detected but not found in Flash library"
                            break;
                        }
                }
            }
        } else {
            usb_dev.SPI_NOR_IF.MyFlashStatus = DeviceStatus.Supported;
            usb_dev.SPI_NOR_IF.MyFlashDevice = CUSTOM_SPI_DEV;
            Connected_Event(usb_dev, 65536);
            PrintConsole(string.Format("Setting SPI clock to: {0}", GetSpiClockString(usb_dev, Params.SPI_CLOCK)));
            return true;
        }
        return false;
    }

    public static bool DetectDevice_SQI(FCUSB_DEVICE usb_dev, DetectParams Params) {
        PrintConsole("Attempting to detect SPI device in SPI extended mode");
        usb_dev.SQI_NOR_IF.SQIBUS_Setup(GetMaxSqiClock(usb_dev.HWBOARD, (uint)SQI_SPEED.MHZ_10));
        if (usb_dev.SQI_NOR_IF.DeviceInit()) {
            usb_dev.SQI_NOR_IF.SQIBUS_Setup(GetMaxSqiClock(usb_dev.HWBOARD, (uint)Params.SQI_CLOCK));
            if (usb_dev.HasLogic) {
                Connected_Event(usb_dev, 131072);
            } else {
                Connected_Event(usb_dev, 16384);
            }
            MainApp.PrintConsole("Detected SPI Flash on SQI port");
            return true;
        } else {
            if (usb_dev.SQI_NOR_IF.MyFlashStatus == DeviceStatus.NotDetected) {
                MainApp.PrintConsole("Flash memory not detected on SPI NOR mode"); // "Flash memory not detected on SPI NOR mode"
            } else if (usb_dev.SQI_NOR_IF.MyFlashStatus == DeviceStatus.NotDetected) {
                MainApp.PrintConsole("Flash memory detected but not found in Flash library");
            }
        }
        return false;
    }

    public static bool DetectDevice_SPI_NAND(FCUSB_DEVICE usb_dev, DetectParams Params) {
        MainApp.PrintConsole("Attempting to detect SPI NAND device");
        usb_dev.SPI_NOR_IF.SPIBUS_Setup(GetMaxSpiClock(usb_dev.HWBOARD, Params.SPI_CLOCK));
        if (usb_dev.SPI_NAND_IF.DeviceInit()) {
            Connected_Event(usb_dev, 65536);
            MainApp.PrintConsole("Successfully detected SPI NAND Flash device");
            PrintConsole(string.Format("Setting SPI clock to: {0}", GetSpiClockString(usb_dev, Params.SPI_CLOCK)));
            return true;
        } else {
            if (usb_dev.SPI_NAND_IF.MyFlashStatus == DeviceStatus.NotDetected) {
                MainApp.PrintConsole("Flash memory not detected on SPI NAND mode"); // "Flash memory not detected on SPI NOR mode"
            } else if (usb_dev.SQI_NOR_IF.MyFlashStatus == DeviceStatus.NotDetected) {
                MainApp.PrintConsole("Flash memory detected but not found in Flash library");
            }
        }
        return false;
    }

    public static bool DetectDevice_SPI_EEPROM(FCUSB_DEVICE usb_dev, DetectParams Params) {
        if (SPIEEPROM_Configure(Params.SPI_EEPROM))
            return false;
        Connected_Event(usb_dev, 1024);
        Utilities.Sleep(100); // Wait for device to be configured
        MainApp.PrintConsole("Configured to use SPI EEPROM device");
        return true;
    }

    public static bool DetectDevice_I2C_EEPROM(FCUSB_DEVICE usb_dev, DetectParams Params) {
        usb_dev.I2C_IF.SelectDeviceIndex(Params.I2C_INDEX);
        if (!usb_dev.I2C_IF.DeviceInit())
            return false;
        MainApp.PrintConsole("Attempting to detect I2C EEPROM device");
        MainApp.PrintConsole(string.Format("I2C Address byte: 0x{0}", Params.I2C_ADDRESS.ToString("X")));
        MainApp.PrintConsole(string.Format("I2C EEPROM device size: {0} bytes", usb_dev.I2C_IF.DeviceSize.ToString("N0")));
        switch (Params.I2C_SPEED) {
            case var case24 when case24 == I2C_SPEED_MODE._100kHz: {
                    MainApp.PrintConsole("I2C protocol speed" + ": 100kHz");
                    break;
                }
            case var case25 when case25 == I2C_SPEED_MODE._400kHz: {
                    MainApp.PrintConsole("I2C protocol speed" + ": 400kHz");
                    break;
                }
            case var case26 when case26 == I2C_SPEED_MODE._1MHz: {
                    MainApp.PrintConsole("I2C protocol speed" + ": 1MHz (Fm+)");
                    break;
                }
        }
        if (usb_dev.I2C_IF.IsConnected()) {
            Connected_Event(usb_dev, 512);
            PrintConsole("I2C EEPROM detected and ready for operation", true); // "I2C EEPROM detected and ready for operation"
            return true;
        } else {
            PrintConsole("I2C EEPROM not detected", true);
        }
        return false;
    }

    public static bool DetectDevice_ONE_WIRE(FCUSB_DEVICE usb_dev, DetectParams Params) {
        MainApp.PrintConsole("Connecting to Single-Wire EEPROM device");
        if (usb_dev.SWI_IF.DeviceInit()) {
            Connected_Event(usb_dev, 128);
            return true;
        } else {
            PrintConsole("1-wire device not detected", true);
        }
        return false;
    }

    public static bool DetectDevice_Microwire(FCUSB_DEVICE usb_dev, DetectParams Params) {
        MainApp.PrintConsole("Connecting to Microwire EEPROM device");
        if (usb_dev.MW_IF.DeviceInit()) {
            Connected_Event(usb_dev, 256);
            return true;
        } else {
            PrintConsole("Microwire device not detected", true);
        }
        return false;
    }

    public static bool DetectDevice_PNOR(FCUSB_DEVICE usb_dev, DetectParams Params) {
        MainApp.PrintConsole("Initializing Parallel mode"); // Initializing parallel mode hardware board
        Utilities.Sleep(150); // Wait for IO board vcc to charge
        usb_dev.PARALLEL_NOR_IF.DeviceInit();
        if (usb_dev.PARALLEL_NOR_IF.MyFlashStatus == DeviceStatus.Supported) {
            MainApp.PrintConsole("Flash device successfully detected and ready for operation"); // "Flash device successfully detected and ready for operation"
            if (usb_dev.HWBOARD == FCUSB_BOARD.Mach1) {
                Connected_Event(usb_dev, 262144);
                usb_dev.PARALLEL_NOR_IF.EXPIO_SetTiming(Params.NOR_READ_ACCESS, Params.NOR_WE_PULSE);
            } else {
                Connected_Event(usb_dev, 16384);
            }
            return true;
        } else if (usb_dev.PARALLEL_NOR_IF.MyFlashStatus == DeviceStatus.NotSupported) {
            MainApp.PrintConsole("Flash memory detected but not found in Flash library"); // "Flash memory detected but not found in Flash library"
        } else if (usb_dev.PARALLEL_NOR_IF.MyFlashStatus == DeviceStatus.NotDetected) {
            MainApp.PrintConsole("Flash device not detected in Parallel I/O mode");
        } else if (usb_dev.PARALLEL_NOR_IF.MyFlashStatus == DeviceStatus.ExtIoNotConnected) {
            MainApp.PrintConsole("Unable to connect to the Parallel I/O board");
        } else if (usb_dev.PARALLEL_NOR_IF.MyFlashStatus == DeviceStatus.NotCompatible) {
            MainApp.PrintConsole("Flash memory is not compatible with this FlashcatUSB programmer model");
        }
        return false;
    }

    public static bool DetectDevice_PNAND(FCUSB_DEVICE usb_dev, DetectParams Params) {
        MainApp.PrintConsole("Initializing Parallel mode"); // Initializing parallel mode hardware board
        Utilities.Sleep(150); // Wait for IO board vcc to charge
        usb_dev.PARALLEL_NAND_IF.DeviceInit();
        if (usb_dev.PARALLEL_NAND_IF.MyFlashStatus == DeviceStatus.Supported) {
            MainApp.PrintConsole("Flash device successfully detected and ready for operation"); // "Flash device successfully detected and ready for operation"
            if (usb_dev.PARALLEL_NAND_IF.MyAdapter == MEM_PROTOCOL.NAND_X16_ASYNC) {
                if (usb_dev.HWBOARD == FCUSB_BOARD.Mach1) {
                    Connected_Event(usb_dev, 524288);
                } else {
                    Connected_Event(usb_dev, 65536);
                }
            } else if (usb_dev.PARALLEL_NAND_IF.MyAdapter == MEM_PROTOCOL.NAND_X8_ASYNC) {
                if (usb_dev.HWBOARD == FCUSB_BOARD.Mach1) {
                    Connected_Event(usb_dev, 524288);
                } else {
                    Connected_Event(usb_dev, 65536);
                }
            }
            return true;
        } else if (usb_dev.PARALLEL_NAND_IF.MyFlashStatus == DeviceStatus.NotSupported) {
            MainApp.PrintConsole("Flash memory detected but not found in Flash library");
        } else if (usb_dev.PARALLEL_NAND_IF.MyFlashStatus == DeviceStatus.NotDetected) {
            MainApp.PrintConsole("Flash device not detected in Parallel I/O mode");
        } else if (usb_dev.PARALLEL_NAND_IF.MyFlashStatus == DeviceStatus.ExtIoNotConnected) {
            MainApp.PrintConsole("Unable to connect to the Parallel I/O board");
        } else if (usb_dev.PARALLEL_NAND_IF.MyFlashStatus == DeviceStatus.NotCompatible) {
            MainApp.PrintConsole("Flash memory is not compatible with this FlashcatUSB programmer model");
        }
        return false;
    }

    public static bool DetectDevice_FWH(FCUSB_DEVICE usb_dev, DetectParams Params) {
        PrintConsole("Initializing FWH device mode");
        usb_dev.FWH_IF.DeviceInit();
        if (usb_dev.FWH_IF.MyFlashStatus == DeviceStatus.Supported) {
            Connected_Event(usb_dev, 4096);
            return true;
        } else if (usb_dev.FWH_IF.MyFlashStatus == DeviceStatus.NotSupported) {
            MainApp.PrintConsole("Flash memory detected but not found in Flash library");
        } else if (usb_dev.FWH_IF.MyFlashStatus == DeviceStatus.NotDetected) {
            MainApp.PrintConsole("FWH device not detected", true);
        }
        return false;
    }

    public static bool DetectDevice_HyperFlash(FCUSB_DEVICE usb_dev, DetectParams Params) {
        PrintConsole("Initializing HyperFlash device mode");
        Utilities.Sleep(250); // Wait for IO board vcc to charge
        usb_dev.HF_IF.DeviceInit();
        if (usb_dev.HF_IF.MyFlashStatus == DeviceStatus.Supported) {
            Connected_Event(usb_dev, 262144);
            return true;
        } else if (usb_dev.HF_IF.MyFlashStatus == DeviceStatus.NotSupported) {
            MainApp.PrintConsole("Flash memory detected but not found in Flash library");
        } else if (usb_dev.HF_IF.MyFlashStatus == DeviceStatus.NotDetected) {
            MainApp.PrintConsole("HyperFlash device not detected", true);
        }
        return false;
    }

    public static bool DetectDevice_EPROM(FCUSB_DEVICE usb_dev, DetectParams Params) {
        MainApp.PrintConsole("Initializing Parallel mode");
        if (usb_dev.EPROM_IF.DeviceInit()) {
            Connected_Event(usb_dev, 16384);
            return true;
        } else {
            PrintConsole("EPROM device not detected", true);
        }
        return false;
    }
    // Called whent the device is closing
    public static void AppClosing() {
        MEM_IF.AbortOperations();
        USBCLIENT.DisconnectAll();
        USBCLIENT.CloseService = true;
        AppIsClosing = true;
        Utilities.Sleep(200);
        MySettings.Save();
    }

    public static void JTAG_Init(FCUSB_DEVICE usb_dev) {
        ScriptEngine.CURRENT_DEVICE_MODE = MyConsole.MyOperation.Mode;
        if (usb_dev.HasLogic) {
            usb_dev.JTAG_IF.TCK_SPEED = MySettings.JTAG_SPEED;
        } else {
            usb_dev.JTAG_IF.TCK_SPEED = JTAG.JTAG_SPEED._1MHZ;
        }
        if (usb_dev.JTAG_IF.Init()) {
            MainApp.PrintConsole("TAG engine setup successfully");
        } else {
            MainApp.PrintConsole("Failed to connect to target board using JTAG");
            return;
        }
        Select_JTAG_Device(0);
    }

    public static void Select_JTAG_Device(int index) {
        ScriptEngine.Unload();
        if (MAIN_FCUSB.JTAG_IF.Devices.Count > 0 && index < MAIN_FCUSB.JTAG_IF.Devices.Count) {
            if (MAIN_FCUSB.JTAG_IF.Chain_IsValid) {
                MAIN_FCUSB.JTAG_IF.Chain_Select(index);
            } else {
                PrintConsole("JTAG chain is not valid, not all devices have BSDL loaded");
            }
        }
    }

    public static MemoryDeviceInstance Connected_Event(FCUSB_DEVICE usb_dev, uint block_size) {
        try {
            Utilities.Sleep(150); // Some devices (such as Spansion 128mbit devices) need a delay here
            MemoryDeviceInstance dev_inst = MEM_IF.Add(usb_dev, usb_dev.PROGRAMMER.GetDevice);
            dev_inst.PrintConsole += MainApp.PrintConsole;
            dev_inst.SetStatus += MainApp.SetStatus;
            dev_inst.PreferredBlockSize = block_size;
            return dev_inst;
        } catch {
        }
        return null;
    }

    public static void OnDeviceUpdateProgress(int percent, FCUSB_DEVICE device) {


    }
    // Makes sure the current firmware is installed
    private static bool FirmwareCheck(string fw_str, float current_version) {
        float AvrVerSng = Utilities.StringToSingle(fw_str);
        if (!(AvrVerSng == current_version)) {
            MainApp.PrintConsole(string.Format("Software requires firmware version {0}", current_version.ToString()));
            MainApp.PrintConsole("FlashcatUSB firmware is out of date, please update");
            return false;
        }
        return true;
    }

    #endregion

    #region FlashcatUSB Pro and Mach1

    private static void FCUSBPRO_Bootloader(FCUSB_DEVICE usb_dev, string board_firmware) {
        float fw_ver = 0f;
        if (usb_dev.HWBOARD == FCUSB_BOARD.Mach1) {
            fw_ver = MACH1_PCB2_FW;
        } else if (usb_dev.HWBOARD == FCUSB_BOARD.Professional_PCB5) {
            fw_ver = PRO_PCB5_FW;
        }
        PrintConsole("Connected to FlashcatUSB in bootloader mode");
        PrintConsole("Performing firmware unit update", true); // Performing firmware unit update       
        Utilities.Sleep(500);
        byte[] Current_fw = Utilities.GetResourceAsBytes(board_firmware);
        PrintConsole(string.Format("Firmware update starting (sending {0} bytes)", Current_fw.Length.ToString("N0")), true);
        bool result = usb_dev.FirmwareUpdate(Current_fw, fw_ver);
        if (result) {
            PrintConsole("Firmware update was a success!", true);
        } else {
            PrintConsole("Error: failed to start firmware update", true);
        }
    }

    private static void FCUSBPRO_SetDeviceVoltage(FCUSB_DEVICE usb_dev, bool silent = false) {
        string console_message;
        if (MySettings.VOLT_SELECT == Voltage.V1_8) {
            console_message = string.Format("Voltage set to: {0}", "1.8V");
            usb_dev.USB_VCC_ON(Voltage.V1_8);
        } else {
            MySettings.VOLT_SELECT = Voltage.V3_3;
            console_message = string.Format("Voltage set to: {0}", "3.3V");
            usb_dev.USB_VCC_ON(Voltage.V3_3);
        }
        if (!silent)
            PrintConsole(console_message);
        Utilities.Sleep(200);
    }

    private static void FCUSBPRO_RebootToBootloader(FCUSB_DEVICE usb_dev) {
        PrintConsole("Firmware update available, performing automatic update", true);
        Utilities.Sleep(2000);
        if (usb_dev.HasLogic) {
            usb_dev.USB_CONTROL_MSG_OUT(USBREQ.FW_REBOOT, null, 0xFFFFFFFFU); // Removes firmware version
        }
        usb_dev.Disconnect();
        Utilities.Sleep(300);
    }

    public static void FCUSBPRO_Update_Logic() {
        try {
            if (MAIN_FCUSB.IS_CONNECTED) {
                if (MAIN_FCUSB.HWBOARD == FCUSB_BOARD.Professional_PCB5) {
                    MainApp.FCUSBPRO_PCB5_Init(MAIN_FCUSB, MyConsole.MyOperation.Mode);
                } else if (MAIN_FCUSB.HWBOARD == FCUSB_BOARD.Mach1) {
                    PrintConsole("Updating all FPGA logic", true);
                    MainApp.FCUSBMACH1_Init(MAIN_FCUSB, MyConsole.MyOperation.Mode);
                    PrintConsole("FPGA logic successfully updated", true);
                }
            }
        } catch {
        }
    }

    public static bool FCUSBPRO_PCB5_Init(FCUSB_DEVICE usb_dev, DeviceMode CurrentMode) {
        usb_dev.USB_VCC_OFF();
        Utilities.Sleep(100);
        if (!(usb_dev.HWBOARD == FCUSB_BOARD.Professional_PCB5))
            return false;
        byte[] bit_data = null;
        if (Utilities.StringToSingle(usb_dev.FW_VERSION) == PRO_PCB5_FW) {
            if (MySettings.VOLT_SELECT == Voltage.V1_8) {
                bit_data = Utilities.GetResourceAsBytes("PRO5_1V8.bit");
                usb_dev.USB_VCC_ON(Voltage.V1_8);
            } else if (MySettings.VOLT_SELECT == Voltage.V3_3) {
                bit_data = Utilities.GetResourceAsBytes("PRO5_3V.bit");
                usb_dev.USB_VCC_ON(Voltage.V3_3);
            }
        }
        var SPI_CFG_IF = new ISC_LOGIC_PROG(MAIN_FCUSB);
        return SPI_CFG_IF.SSPI_ProgramICE(bit_data);
    }

    public static void OnUpdateProgress(int percent) {
        MyConsole.Progress_Set(percent);
    }

    public static void MACH1_FPGA_ERASE(FCUSB_DEVICE usb_dev) {
        PrintConsole("Erasing FPGA device", true);
        MEM_IF.Clear(); // Remove all devices that are on this usb port
        var svf_data = Utilities.GetResourceAsBytes("MACH1_ERASE.svf");
        bool jtag_successful = usb_dev.JTAG_IF.Init();
        if (!jtag_successful) {
            PrintConsole("Error: failed to connect to FPGA via JTAG");
            return;
        }
        var svf_file = Utilities.Bytes.ToCharStringArray(svf_data);
        usb_dev.LOGIC_SetVersion(0xFFFFFFFFU);
        bool result = usb_dev.JTAG_IF.JSP.RunFile_SVF(svf_file);
        if (!result) {
            PrintConsole("FPGA erase failed", true);
            return;
        } else {
            PrintConsole("FPGA erased successfully", true);
            usb_dev.USB_VCC_OFF();
            FCUSBPRO_SetDeviceVoltage(usb_dev);
        }
    }

    public static bool FCUSBMACH1_Init(FCUSB_DEVICE usb_dev, DeviceMode CurrentMode) {
        if (!(usb_dev.HWBOARD == FCUSB_BOARD.Mach1))
            return false;
        FCUSBPRO_SetDeviceVoltage(usb_dev); // Power on CPLD
        uint cpld32 = usb_dev.LOGIC_GetVersion();
        byte[] bit_data = null;
        uint svf_code = 0U;
        if (CurrentMode == DeviceMode.SPI | CurrentMode == DeviceMode.SPI_EEPROM | CurrentMode == DeviceMode.SPI_NAND) {
            if (MySettings.VOLT_SELECT == Voltage.V1_8 & !(cpld32 == MACH1_SPI_1V8)) {
                bit_data = Utilities.GetResourceAsBytes("MACH1_SPI_1V8.bit");
                svf_code = MACH1_SPI_1V8;
            } else if (MySettings.VOLT_SELECT == Voltage.V3_3 & !(cpld32 == MACH1_SPI_3V3)) {
                bit_data = Utilities.GetResourceAsBytes("MACH1_SPI_3V.bit");
                svf_code = MACH1_SPI_3V3;
            }
        } else if (MySettings.VOLT_SELECT == Voltage.V1_8 & !(cpld32 == MACH1_FGPA_1V8)) {
            bit_data = Utilities.GetResourceAsBytes("MACH1_1V8.bit");
            svf_code = MACH1_FGPA_1V8;
        } else if (MySettings.VOLT_SELECT == Voltage.V3_3 & !(cpld32 == MACH1_FGPA_3V3)) {
            bit_data = Utilities.GetResourceAsBytes("MACH1_3V3.bit");
            svf_code = MACH1_FGPA_3V3;
        }
        if (bit_data is object) {
            return MACH_ProgramLogic(usb_dev, bit_data, svf_code);
        }
        return true;
    }

    private static bool MACH_ProgramLogic(FCUSB_DEVICE usb_dev, byte[] bit_data, uint bit_code) {
        try {
            var SPI_CFG_IF = new ISC_LOGIC_PROG(usb_dev);
            SPI_CFG_IF.PrintConsole += PrintConsole;
            SPI_CFG_IF.SetProgress += OnUpdateProgress;
            bool SPI_INIT_RES = SPI_CFG_IF.SSPI_Init(0U, 1U, 24U); //CS_1
            uint SPI_ID = SPI_CFG_IF.SSPI_ReadIdent();
            if (!(SPI_ID == 0x12BC043L)) {
                MACH1_FPGA_ERASE(usb_dev);
                SPI_CFG_IF.SSPI_Init(0U, 1U, 24U);
                SPI_ID = SPI_CFG_IF.SSPI_ReadIdent();
            }
            if (!(SPI_ID == 0x12BC043L)) {
                PrintConsole("FPGA error: unable to communicate via SPI", true);
                return false;
            }
            PrintConsole("Programming on board FPGA with new logic", true);
            if (SPI_CFG_IF.SSPI_ProgramMACHXO(bit_data)) {
                PrintConsole("FPGA device successfully programmed", true);
                usb_dev.LOGIC_SetVersion(bit_code);
                return true;
            } else {
                PrintConsole("FPGA device programming failed", true);
                return false;
            }
        } catch {
        }
        return true;
    }

    private static void ProgramSVF(FCUSB_DEVICE usb_dev, byte[] svf_data, uint svf_code) {
        try {
            PrintConsole("Programming on board FPGA with new logic", true);
            usb_dev.USB_VCC_OFF();
            Utilities.Sleep(1000);
            if (!usb_dev.JTAG_IF.Init()) {
                PrintConsole("Error: unable to connect to on board FPGA via JTAG", true);
                return;
            }
            usb_dev.USB_VCC_ON(MySettings.VOLT_SELECT);
            var svf_file = Utilities.Bytes.ToCharStringArray(svf_data);
            if (GUI is object) {
                usb_dev.JTAG_IF.JSP.Progress -= OnUpdateProgress;
                usb_dev.JTAG_IF.JSP.Progress += OnUpdateProgress;
            } else {
                usb_dev.JTAG_IF.JSP.Progress += MyConsole.Progress_Set;
            }
            PrintConsole("Programming SVF data into Logic device");
            usb_dev.LOGIC_SetVersion(0xFFFFFFFFU);
            bool result = usb_dev.JTAG_IF.JSP.RunFile_SVF(svf_file);
            OnUpdateProgress(100);
            if (result) {
                PrintConsole("FPGA successfully programmed!", true);
                usb_dev.LOGIC_SetVersion(svf_code);
            } else {
                 PrintConsole("Error, unable to program in-circuit FPGA", true);
                return;
            }
            Utilities.Sleep(250);
            usb_dev.USB_CONTROL_MSG_OUT(USBREQ.FW_REBOOT, null, 0U); // We need to reboot to clean up USB memory
            Utilities.Sleep(250);
            if (GUI is null)
                MyConsole.PrintConsole("Rebooting board...");
        } catch {
            PrintConsole("Exception in programming FPGA", true);
        }
    }

    #endregion

    #region Shell User Interface

    public static string PromptUser_OpenFile(string title = "Choose file to open", string filter = "All files (*.*)|*.*", string opt_path = @"\") {
        var response = MyConsole.Console_Ask(title);
        return response;
    }

    public static string PromptUser_SaveFile(string title = "Choose location to save", string filter = "All files (*.*)|*.*", string default_file = "") {
        var response = MyConsole.Console_Ask(title);
        return response;
    }

    public static bool PromptUser_Ask(string the_question) {
        var response = MyConsole.Console_Ask(the_question);
        if (response.ToUpper().Equals("YES")) {
            return true;
        } else {
            return false;
        }
    }

    public static void PromptUser_Msg(string message_txt) {
        MainApp.PrintConsole(message_txt);
    }

    public static void ProgressBar_Add(int index, int bar_left, int bar_top, int bar_width) {
        MyConsole.Progress_Create();
    }

    public static void ProgressBar_Percent(int percent) {
        MyConsole.Progress_Set(percent);
    }

    public static void ProgressBar_Dispose() {
        MyConsole.Progress_Remove();
    }

    #endregion

    public static string GetOsBitsString() {
        if (Environment.Is64BitOperatingSystem) {
            return "64 bit";
        } else {
            return "32 bit";
        }
    }

    public static DeviceMode[] GetSupportedModes(FCUSB_DEVICE usb_dev) {
        List<DeviceMode> modes = new List<DeviceMode>();
        if (usb_dev.HWBOARD== FCUSB_BOARD.Classic) {
            modes.Add(DeviceMode.SPI);
            modes.Add(DeviceMode.SQI);
            modes.Add(DeviceMode.SPI_NAND);
            modes.Add(DeviceMode.I2C_EEPROM);
            modes.Add(DeviceMode.SPI_EEPROM);
            modes.Add(DeviceMode.Microwire);
            modes.Add(DeviceMode.ONE_WIRE);
            modes.Add(DeviceMode.JTAG);
        } else if (usb_dev.HWBOARD == FCUSB_BOARD.XPORT_PCB2) {
            modes.Add(DeviceMode.PNOR);
            modes.Add(DeviceMode.PNAND);
            modes.Add(DeviceMode.FWH);
            modes.Add(DeviceMode.SPI);
            modes.Add(DeviceMode.SQI);
            modes.Add(DeviceMode.SPI_NAND);
            modes.Add(DeviceMode.I2C_EEPROM);
            modes.Add(DeviceMode.SPI_EEPROM);
            modes.Add(DeviceMode.SPI_NAND);
            modes.Add(DeviceMode.EPROM);
            modes.Add(DeviceMode.JTAG);
        } else if (usb_dev.HWBOARD == FCUSB_BOARD.Professional_PCB5) {
            modes.Add(DeviceMode.SPI);
            modes.Add(DeviceMode.I2C_EEPROM);
            modes.Add(DeviceMode.SPI_EEPROM);
            modes.Add(DeviceMode.SPI_NAND);
            modes.Add(DeviceMode.Microwire);
            modes.Add(DeviceMode.SQI);
            modes.Add(DeviceMode.JTAG);
        } else if (usb_dev.HWBOARD == FCUSB_BOARD.Mach1) {
            modes.Add(DeviceMode.SPI);
            modes.Add(DeviceMode.SPI_NAND);
            modes.Add(DeviceMode.SQI);
            modes.Add(DeviceMode.PNOR);
            modes.Add(DeviceMode.PNAND);
            modes.Add(DeviceMode.HyperFlash);
        }
        return modes.ToArray();
    }

}
