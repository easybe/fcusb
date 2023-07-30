public enum DeviceMode : byte {
    SPI = 1,
    JTAG = 2,
    I2C_EEPROM = 3,
    SPI_EEPROM = 4,
    PNOR = 5,
    PNAND = 6,
    ONE_WIRE = 7,
    SPI_NAND = 8,
    EPROM = 9,
    HyperFlash = 10,
    Microwire = 11,
    SQI = 12,
    SD_MMC_EMMC = 13,
    FWH = 14,
    Unspecified = 20
}

public enum MEM_PROTOCOL : byte {
    SETUP = 0,
    NOR_X8 = 1,
    NOR_X16 = 2,
    NOR_X16_X8 = 3,
    NAND_X8_ASYNC = 4,
    NAND_X16_ASYNC = 5,
    FWH = 6,
    HYPERFLASH = 7,
    EPROM_X8 = 8,
    EPROM_X16 = 9
}

public enum ConsoleTask : byte {
    NoTask,
    Help,
    Check,
    ReadMemory,
    WriteMemory,
    EraseMemory,
    ExecuteScript
}

public enum LicenseStatusEnum {
    NotLicensed,
    LicensedValid,
    LicenseExpired
}

public enum Voltage : int {
    OFF = 0,
    V1_8 = 1,
    V3_3 = 2
}

public enum BadBlockMode : int {
    Disabled = 1,
    Enabled = 2
}

public enum BadBlockMarker : int {
    _1stByte_FirstPage = (1 << 1),
    _1stByte_SecondPage = (1 << 2),
    _1stByte_LastPage = (1 << 3),
    _6thByte_FirstPage = (1 << 4),
    _6thByte_SecondPage = (1 << 5)
}
public enum NandMemLayout : int {
    Separated = 1,      //We want to see Main or Spare data
    Combined = 2,       //We want to see all data 
    Segmented = 3       //Main is spread across the entire page with spare area after each 512 byte chunks
}

public enum NandMemSpeed : int {
    _20MHz = 0,
    _10MHz = 1,
    _5MHz = 2,
    _1MHz = 3
}

public enum BitSwapMode {
    None = 0,
    Bits_8 = 1, // 0x01 = 0x80
    Bits_16 = 2, // 0x0102 = 0x4080
    Bits_32 = 3 // 0x00010203 = 0x20C04080
}

public enum BitEndianMode {
    BigEndian32 = 0, // 0x01020304 = 0x01020304 (default)
    BigEndian16 = 1, // 0x01020304 = 0x02010403
    LittleEndian32_8bit = 2, // 0x01020304 = 0x03040102
    LittleEndian32_16bit = 3 // 0x01020304 = 0x02010403
}

public enum I2C_SPEED_MODE : int {
    _100kHz = 1,
    _400kHz = 2,
    _1MHz = 3
}

public enum LOGIC_MODE {
    NotSelected, // Default
    SPI_3V, // Standard GPIO/SPI @ 3.3V
    SPI_1V8, // Standard GPIO/SPI @ 1.8V
    QSPI_3V,
    QSPI_1V8,
    I2C, // I2C only mode @ 3.3V
    JTAG, // JTAG mode @ 3.3V
    NAND_1V8, // NAND mode @ 1.8V
    NAND_3V3, // NAND mode @ 3.3V
    HF_1V8, // HyperFlash @ 1.8V
    HF_3V3 // HyperFlash @ 3.3V
}