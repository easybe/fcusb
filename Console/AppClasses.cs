using FlashMemory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static MemoryInterface;

public class WriteParameters
{
    public long Address = 0; // flash address to write to
    public long BytesLeft = 0; // Number of bytes to write from this stream
    public long BytesWritten = 0; // Number of bytes we have written
    public long BytesTotal = 0; // The total number of bytes to write
    public MemoryDeviceInstance.StatusCallback Status = new MemoryDeviceInstance.StatusCallback(); // Contains all the delegates (if connected)
    public Stopwatch Timer; // To monitor the transfer speed
    // Write Specific Parameters:
    public bool EraseSector = true;  // True if we want to erase each sector prior to write
    public bool Verify = true; // True if we want to read back the data
    public bool AbortOperation = false;
}

public class ReadParameters {
    public long Address = 0;
    public long Count = 0;
    public MemoryDeviceInstance.StatusCallback Status = new MemoryDeviceInstance.StatusCallback(); // Contains all the delegates (if connected)
    public Stopwatch Timer; // To monitor the transfer speed
    public bool AbortOperation = false;
}

public class FlashcatSettings {
    public string LanguageName { get; set; }
    public Voltage VOLT_SELECT { get; set; } // Selects output voltage and level
    //public DeviceMode OPERATION_MODE { get; set; } = DeviceMode.SPI;
    public bool VERIFY_WRITE { get; set; } // Read back written data to compare write was successful
    public int RETRY_WRITE_ATTEMPTS { get; set; } // Number of times to retry a write operation
    public BitEndianMode BIT_ENDIAN { get; set; } = BitEndianMode.BigEndian32; // Mirrors bits (not saved)
    public BitSwapMode BIT_SWAP { get; set; } = BitSwapMode.None; // Swaps nibbles/bytes/words (not saved)
    public int MULTI_CE { get; set; } // 0 (do not use), else A=1<<CE_VALUE
    public SPI.SPI_SPEED SPI_CLOCK_MAX { get; set; }
    public SPI.SQI_SPEED SQI_CLOCK_MAX { get; set; }
    public SPI.SPI_CLOCK_POLARITY SPI_MODE { get; set; } // MODE=0 
    public string SPI_EEPROM { get; set; } // Name of the EEPROM
    public bool SPI_FASTREAD { get; set; }
    public bool SPI_AUTO { get; set; } // Indicates if the software will use common op commands
    public bool SPI_NAND_DISABLE_ECC { get; set; }
    public byte I2C_ADDRESS { get; set; }
    public I2C_SPEED_MODE I2C_SPEED { get; set; }
    public int I2C_INDEX { get; set; } // The device selected index
    public byte SWI_ADDRESS { get; set; } // Slave Address
    public bool NAND_Preserve { get; set; } = true; // We want to copy SPARE data before erase
    public bool NAND_Verify { get; set; } = false;
    public BadBlockMode NAND_BadBlockManager { get; set; } // Indicates how BAD BLOCKS are detected
    public BadBlockMarker NAND_BadBlockMarkers { get; set; }
    public bool NAND_SkipBadBlock { get; set; } = true; // If a block fails to program, skip block and write data to the next block
    public NandMemLayout NAND_Layout { get; set; } = NandMemLayout.Separated;
    public NandMemSpeed NAND_Speed { get; set; } = NandMemSpeed._20MHz;
    public bool ECC_FEATURE_ENABLED { get; set; }
    public int NOR_READ_ACCESS { get; set; }
    public int NOR_WE_PULSE { get; set; }
    public string S93_DEVICE { get; set; } // Name of the part number
    public int S93_DEVICE_ORG { get; set; } // 0=8-bit,1=16-bit
    public int SREC_DATAMODE { get; set; } // 0=8-bit,1=16-bit
                                           // JTAG
    public JTAG.JTAG_SPEED JTAG_SPEED { get; set; }
    // License
    public string LICENSE_KEY { get; set; }
    public string LICENSED_TO { get; set; }
    public DateTime LICENSE_EXP { get; set; }

    private SettingsIO SettingsFile = new SettingsIO_INI();

    public FlashcatSettings() {
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");
        LoadLanguageSettings(SettingsFile.GetValue("Language", "English"));
        LICENSE_KEY = SettingsFile.GetValue("LICENSE_KEY", "");
        string date_str = SettingsFile.GetValue("LICENSE_DATE", "01/01/0001");
        LICENSED_TO = SettingsFile.GetValue("LICENSE_NAME", "");
        if (date_str.Equals("01/01/0001") || date_str.Equals("1/1/0001")) {
            LICENSE_EXP = new DateTime();
        } else {
            LICENSE_EXP = DateTime.Parse(date_str);
        }
        MULTI_CE = SettingsFile.GetValue("MULTI_CE", 5);
        VOLT_SELECT = (Voltage)SettingsFile.GetValue("VOLTAGE", (int)Voltage.V3_3);
        //OPERATION_MODE = (DeviceMode)SettingsFile.GetValue("OPERATION", 1); // Default is normal
        VERIFY_WRITE = SettingsFile.GetValue("VERIFY", true);
        RETRY_WRITE_ATTEMPTS = SettingsFile.GetValue("VERIFY_COUNT", 2);
        BIT_ENDIAN = BitEndianMode.BigEndian32;
        BIT_SWAP = BitSwapMode.None;
        SPI_CLOCK_MAX = (SPI.SPI_SPEED)SettingsFile.GetValue("SPI_CLOCK_MAX", (int)SPI.SPI_SPEED.MHZ_8);
        SQI_CLOCK_MAX = (SPI.SQI_SPEED)SettingsFile.GetValue("SPI_QUAD_SPEED", (int)SPI.SQI_SPEED.MHZ_10);
        SPI_FASTREAD = SettingsFile.GetValue("SPI_FASTREAD", false);
        SPI_MODE = (SPI.SPI_CLOCK_POLARITY)SettingsFile.GetValue("SPI_MODE", (int)SPI.SPI_CLOCK_POLARITY.SPI_MODE_0);
        SPI_EEPROM = SettingsFile.GetValue("SPI_EEPROM", "");
        SPI_AUTO = SettingsFile.GetValue("SPI_AUTO", true);
        SPI_NAND_DISABLE_ECC = SettingsFile.GetValue("SPI_NAND_ECC", false);
        I2C_ADDRESS = (byte)SettingsFile.GetValue("I2C_ADDR", 0xA0);
        I2C_SPEED = (I2C_SPEED_MODE)SettingsFile.GetValue("I2C_SPEED", (int)I2C_SPEED_MODE._400kHz);
        I2C_INDEX = SettingsFile.GetValue("I2C_INDEX", -1);
        SWI_ADDRESS = (byte)SettingsFile.GetValue("SWI_ADDR", 0x0);
        NAND_Preserve = SettingsFile.GetValue("NAND_Preserve", true);
        NAND_Verify = SettingsFile.GetValue("NAND_Verify", false);
        NAND_BadBlockManager = (BadBlockMode)SettingsFile.GetValue("NAND_BadBlockMode", (int)BadBlockMode.Disabled);
        NAND_BadBlockMarkers = (BadBlockMarker)SettingsFile.GetValue("NAND_BadBlockMarker", (int)(BadBlockMarker._1stByte_FirstPage | BadBlockMarker._1stByte_SecondPage | BadBlockMarker._1stByte_LastPage));
        NAND_SkipBadBlock = SettingsFile.GetValue("NAND_Mismatch", true);
        NAND_Layout = (NandMemLayout)SettingsFile.GetValue("NAND_Layout", (int)NandMemLayout.Separated);
        NAND_Speed = (NandMemSpeed)SettingsFile.GetValue("NAND_Speed", (int)NandMemSpeed._20MHz);
        ECC_FEATURE_ENABLED = SettingsFile.GetValue("ECC_ENABLED", false);
        S93_DEVICE = SettingsFile.GetValue("S93_DEVICE_NAME", "");
        S93_DEVICE_ORG = SettingsFile.GetValue("S93_ORG", 0);
        SREC_DATAMODE = SettingsFile.GetValue("SREC_ORG", 0);
        JTAG_SPEED = (JTAG.JTAG_SPEED)SettingsFile.GetValue("JTAG_FREQ", (int)JTAG.JTAG_SPEED._10MHZ);
        NOR_READ_ACCESS = SettingsFile.GetValue("NOR_READ_ACCESS", 200);
        NOR_WE_PULSE = SettingsFile.GetValue("NOR_WE_PULSE", 125);
        ValidateEEPROM();
        SettingsFile.ECC_Load();
    }

    public void Save() {
        SettingsFile.SetValue("LICENSE_KEY", LICENSE_KEY);
        SettingsFile.SetValue("LICENSE_NAME", LICENSED_TO);
        SettingsFile.SetValue("LICENSE_DATE", LICENSE_EXP.ToShortDateString());
        SettingsFile.SetValue("MULTI_CE", MULTI_CE);
        SettingsFile.SetValue("VOLTAGE", (int)VOLT_SELECT);
        //SettingsFile.SetValue("OPERATION", (int)OPERATION_MODE);
        SettingsFile.SetValue("VERIFY", VERIFY_WRITE);
        SettingsFile.SetValue("VERIFY_COUNT", RETRY_WRITE_ATTEMPTS);
        SettingsFile.SetValue("ENDIAN", (int)BIT_ENDIAN);
        SettingsFile.SetValue("BITSWAP", (int)BIT_SWAP);
        SettingsFile.SetValue("SPI_CLOCK_MAX", (int)SPI_CLOCK_MAX);
        SettingsFile.SetValue("SPI_MODE", (int)SPI_MODE);
        SettingsFile.SetValue("SPI_EEPROM", SPI_EEPROM);
        SettingsFile.SetValue("SPI_FASTREAD", SPI_FASTREAD);
        SettingsFile.SetValue("SPI_AUTO", SPI_AUTO);
        SettingsFile.SetValue("SPI_NAND_ECC", SPI_NAND_DISABLE_ECC);
        SettingsFile.SetValue("SPI_QUAD_SPEED", (int)SQI_CLOCK_MAX);
        SettingsFile.SetValue("I2C_ADDR", I2C_ADDRESS);
        SettingsFile.SetValue("I2C_SPEED", (int)I2C_SPEED);
        SettingsFile.SetValue("I2C_INDEX", I2C_INDEX);
        SettingsFile.SetValue("SWI_ADDR", SWI_ADDRESS);
        SettingsFile.SetValue("NAND_Preserve", NAND_Preserve);
        SettingsFile.SetValue("NAND_Verify", NAND_Verify);
        SettingsFile.SetValue("NAND_BadBlockMode", (int)NAND_BadBlockManager);
        SettingsFile.SetValue("NAND_BadBlockMarker", (int)NAND_BadBlockMarkers);
        SettingsFile.SetValue("NAND_Mismatch", NAND_SkipBadBlock);
        SettingsFile.SetValue("NAND_Layout", (int)NAND_Layout);
        SettingsFile.SetValue("NAND_Speed", (int)NAND_Speed);
        SettingsFile.SetValue("Language", LanguageName);
        SettingsFile.SetValue("ECC_ENABLED", ECC_FEATURE_ENABLED);
        SettingsFile.SetValue("S93_DEVICE_NAME", S93_DEVICE);
        SettingsFile.SetValue("S93_ORG", S93_DEVICE_ORG);
        SettingsFile.SetValue("SREC_ORG", SREC_DATAMODE);
        SettingsFile.SetValue("JTAG_FREQ", (int)JTAG_SPEED);
        SettingsFile.SetValue("NOR_READ_ACCESS", NOR_READ_ACCESS);
        SettingsFile.SetValue("NOR_WE_PULSE", NOR_WE_PULSE);
        SettingsFile.ECC_Save();
    }

    private void ValidateEEPROM() {
        try {
            bool EEPROM_FOUND = false;
            if (!SPI_EEPROM.Equals("")) {
                var d = MainApp.GetDevices_SPI_EEPROM();
                foreach (var dev in d) {
                    if (dev.NAME.Equals(SPI_EEPROM)) {
                        EEPROM_FOUND = true;
                        break;
                    }
                }
            }

            if (!EEPROM_FOUND)
                SPI_EEPROM = "";
        } catch {
            SPI_EEPROM = "";
        }
    }

    public static string NandMemSpeedToString(NandMemSpeed speed) {
        switch (speed) {
            case NandMemSpeed._20MHz: {
                    return "20MHz";
                }

            case NandMemSpeed._10MHz: {
                    return "10MHz";
                }

            case NandMemSpeed._5MHz: {
                    return "5MHz";
                }

            case NandMemSpeed._1MHz: {
                    return "1MHz";
                }

            default: {
                    return "ERROR";
                }
        }
    }

    private void LoadLanguageSettings(string language_name) {
        LanguageName = language_name;
    }

    public void SetPrefferedScript(string name, uint id) {
        SettingsFile.SetValue("SCRIPT_" + id.ToString(), name);
    }

    public string GetPrefferedScript(uint id) {
        return SettingsFile.GetValue("SCRIPT_" + id.ToString(), "");
    }
}

public interface SettingsIO {
    void ECC_Load();
    bool ECC_Save();
    string GetValue(string Name, string DefaultValue);
    bool GetValue(string Name, bool DefaultValue);
    int GetValue(string Name, int DefaultValue);
    byte[] GetData(string Name);
    bool SetValue(string Name, string Value);
    bool SetValue(string Name, bool Value);
    bool SetValue(string Name, int Value);
    bool SetData(string Name, byte[] data);
}

public class SettingsIO_INI : SettingsIO {
    private Dictionary<string, string> ini_dictionary = new Dictionary<string, string>();
    private System.IO.FileInfo app_ini_file = new System.IO.FileInfo("app_settings.ini");
    private System.IO.FileInfo ecc_ini_file = new System.IO.FileInfo("ecc_settings.ini");

    public SettingsIO_INI() {
        if (app_ini_file.Exists) {
            using (var ini_reader = app_ini_file.OpenText()) {
                while (!(ini_reader.Peek() == -1)) {
                    string cfg_line = ini_reader.ReadLine().Trim();
                    if (!(string.IsNullOrEmpty(cfg_line) || cfg_line.StartsWith("#"))) {
                        int tab_ind = cfg_line.IndexOf('\t'); // Split by tab char
                        if (tab_ind > 0) {
                            string key_name = cfg_line.Substring(0, tab_ind);
                            string key_value = cfg_line.Substring(tab_ind + 1);
                            ini_dictionary.Add(key_name, key_value);
                        }
                    }
                }
            }
        }
    }

    private bool Save() {
        try {
            if (app_ini_file.Exists) { app_ini_file.Delete(); }
            using (var ini_writer = app_ini_file.CreateText()) {
                ini_writer.WriteLine("#Application settings file for FlashcatUSB");
                ini_writer.WriteLine("#Format: SETTING_NAME<TAB>SETTING_VALUE");
                foreach (var name in ini_dictionary.Keys)
                    ini_writer.WriteLine(name + '\t' + ini_dictionary[name]);
            }
            return true;
        } catch {
            return false;
        }
    }

    public void ECC_Load() {
        if (ecc_ini_file.Exists) {
            var ecc_settings = new List<ECC_LIB.ECC_Configuration_Entry>();
            ECC_LIB.ECC_Configuration_Entry ecc_entry = null;
            using (var ini_reader = ecc_ini_file.OpenText()) {
                while (!(ini_reader.Peek() == -1)) {
                    string cfg_line = ini_reader.ReadLine().Trim();
                    if (!(string.IsNullOrEmpty(cfg_line) || cfg_line.StartsWith("#"))) {
                        if (cfg_line.ToUpper().Equals("START_CONFIGURATION")) {
                            ecc_entry = new ECC_LIB.ECC_Configuration_Entry();
                        } else if (cfg_line.ToUpper().Equals("END_CONFIGURATION")) {
                            ecc_settings.Add(ecc_entry);
                        } else {
                            int tab_ind = cfg_line.IndexOf('\t'); // Split by tab char
                            if (tab_ind > 0) {
                                string key_name = cfg_line.Substring(0, tab_ind);
                                string key_value = cfg_line.Substring(tab_ind + 1);
                                if (key_name.ToUpper().Equals("PageSize".ToUpper())) {
                                    ecc_entry.PageSize = UInt16.Parse(key_value);
                                } else if (key_name.ToUpper().Equals("SpareSize".ToUpper())) {
                                    ecc_entry.SpareSize = UInt16.Parse(key_value);
                                } else if (key_name.ToUpper().Equals("Algorithm".ToUpper())) {
                                    if (Microsoft.VisualBasic.Information.IsNumeric(key_value)) {
                                        ecc_entry.Algorithm = (ECC_LIB.ecc_algorithum)Int32.Parse(key_value);
                                    } else if (key_value.ToUpper().Equals("HAMMING")) {
                                        ecc_entry.Algorithm = (ECC_LIB.ecc_algorithum)0;
                                    } else if (key_value.ToUpper().Equals("REEDSOLOMON")) {
                                        ecc_entry.Algorithm = (ECC_LIB.ecc_algorithum)1;
                                    } else if (key_value.ToUpper().Equals("BHC")) {
                                        ecc_entry.Algorithm = (ECC_LIB.ecc_algorithum)2;
                                    }
                                } else if (key_name.ToUpper().Equals("BitError".ToUpper())) {
                                    ecc_entry.BitError = Byte.Parse(key_value);
                                } else if (key_name.ToUpper().Equals("SymbolSize".ToUpper())) {
                                    ecc_entry.SymSize = Byte.Parse(key_value);
                                } else if (key_name.ToUpper().Equals("ReverseData".ToUpper())) {
                                    ecc_entry.ReverseData = Boolean.Parse(key_value);
                                } else if (key_name.ToUpper().StartsWith("Region_".ToUpper())) {
                                    ecc_entry.AddRegion(UInt16.Parse(key_value));
                                }
                            }
                        }
                    }
                }
                MainApp.NAND_ECC_CFG = ecc_settings.ToArray();
            }
        }
    }

    public bool ECC_Save() {
        try {
            if (ecc_ini_file.Exists) { ecc_ini_file.Delete(); }
            if (MainApp.NAND_ECC_CFG != null && MainApp.NAND_ECC_CFG.Length > 0) {
                using (var ini_writer = ecc_ini_file.CreateText()) {
                    ini_writer.WriteLine("#ECC settings file for FlashcatUSB");
                    for (int i = 0, loopTo = MainApp.NAND_ECC_CFG.Length - 1; i <= loopTo; i++) {
                        ini_writer.WriteLine("START_CONFIGURATION");
                        ini_writer.WriteLine("PageSize" + '\t' + MainApp.NAND_ECC_CFG[i].PageSize.ToString());
                        ini_writer.WriteLine("SpareSize" + '\t' + MainApp.NAND_ECC_CFG[i].SpareSize.ToString());
                        ini_writer.WriteLine("Algorithm" + '\t' + MainApp.NAND_ECC_CFG[i].Algorithm.ToString());
                        ini_writer.WriteLine("BitError" + '\t' + MainApp.NAND_ECC_CFG[i].BitError.ToString());
                        ini_writer.WriteLine("SymbolSize" + '\t' + MainApp.NAND_ECC_CFG[i].SymSize.ToString());
                        ini_writer.WriteLine("ReverseData" + '\t' + MainApp.NAND_ECC_CFG[i].ReverseData.ToString());
                        if (MainApp.NAND_ECC_CFG[i].EccRegion is object && MainApp.NAND_ECC_CFG[i].EccRegion.Length > 0) {
                            for (int x = 1, loopTo1 = MainApp.NAND_ECC_CFG[i].EccRegion.Length - 1; x <= loopTo1; x++)
                                ini_writer.WriteLine("Region_" + x.ToString() + '\t' + ((int)MainApp.NAND_ECC_CFG[i].EccRegion[x]).ToString());
                        }
                        ini_writer.WriteLine("END_CONFIGURATION");
                    }
                }
            }
            return true;
        } catch {
            return false;
        }
    }

    public string GetValue(string Name, string DefaultValue) {
        if (!ini_dictionary.ContainsKey(Name))
            return DefaultValue;
        return ini_dictionary[Name];
    }

    public bool GetValue(string Name, bool DefaultValue) {
        if (!ini_dictionary.ContainsKey(Name))
            return DefaultValue;
        return bool.Parse(ini_dictionary[Name]);
    }

    public int GetValue(string Name, int DefaultValue) {
        if (!ini_dictionary.ContainsKey(Name))
            return DefaultValue;
        return int.Parse(ini_dictionary[Name]);
    }

    public byte[] GetData(string Name) {
        if (!ini_dictionary.ContainsKey(Name))
            return null;
        string hex_str = ini_dictionary[Name];
        return Utilities.Bytes.FromHexString(hex_str);
    }

    public bool SetValue(string Name, string Value) {
        ini_dictionary[Name] = Value;
        return Save();
    }

    public bool SetValue(string Name, bool Value) {
        ini_dictionary[Name] = Value.ToString();
        return Save();
    }

    public bool SetValue(string Name, int Value) {
        ini_dictionary[Name] = Value.ToString();
        return Save();
    }

    public bool SetData(string Name, byte[] data) {
        string hex_str = Utilities.Bytes.ToHexString(data);
        ini_dictionary[Name] = hex_str;
        return Save();
    }

}
