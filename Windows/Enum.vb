﻿
Public Enum DeviceMode As Byte
    SPI = 1
    JTAG = 2
    I2C_EEPROM = 3
    SPI_EEPROM = 4
    PNOR = 5
    PNAND = 6
    ONE_WIRE = 7
    SPI_NAND = 8
    EPROM = 9
    HyperFlash = 10
    Microwire = 11
    SQI = 12
    SD_MMC_EMMC = 13
    FWH = 14
    DFU = 15
    Unspecified = 20
End Enum

Public Enum MEM_PROTOCOL As Byte
    SETUP = 0
    NOR_X8 = 1
    NOR_X16 = 2
    NOR_X16_X8 = 3
    NAND_X8_ASYNC = 4
    NAND_X16_ASYNC = 5
    FWH = 6
    HYPERFLASH = 7
    EPROM_X8 = 8
    EPROM_X16 = 9
End Enum

Public Enum ConsoleTask As Byte
    NoTask
    Help
    ReadMemory
    WriteMemory
    EraseMemory
    ExecuteScript
End Enum

Public Enum LicenseStatusEnum
    NotLicensed
    LicensedValid
    LicenseExpired
End Enum

Public Enum Voltage As Integer
    OFF = 0
    V1_8 = 1 'Low (300ma max)
    V3_3 = 2 'Default
End Enum

Public Enum BadBlockMode As Integer
    Disabled = 1
    Enabled = 2
End Enum

Public Enum BadBlockMarker As Integer
    _1stByte_FirstPage = (1 << 1)
    _1stByte_SecondPage = (1 << 2)
    _1stByte_LastPage = (1 << 3)
    _6thByte_FirstPage = (1 << 4)
    _6thByte_SecondPage = (1 << 5)
End Enum

Public Enum NandMemLayout As Integer
    Separated = 1 'Main is the beginning of the page
    Segmented = 2 'Main is segmented over the entire page
End Enum

Public Enum NandMemSpeed As Integer
    _20MHz = 0
    _10MHz = 1
    _5MHz = 2
    _1MHz = 3
End Enum

Public Enum BitSwapMode
    None = 0
    Bits_8 = 1 '0x01 = 0x80
    Bits_16 = 2 '0x0102 = 0x4080
    Bits_32 = 3 '0x00010203 = 0x20C04080
End Enum

Public Enum BitEndianMode
    BigEndian32 = 0 '0x01020304 = 0x01020304 (default)
    BigEndian16 = 1 '0x01020304 = 0x02010403
    LittleEndian32_8bit = 2 '0x01020304 = 0x03040102
    LittleEndian32_16bit = 3 '0x01020304 = 0x02010403
End Enum

Public Enum I2C_SPEED_MODE As Integer
    _100kHz = 1
    _400kHz = 2
    _1MHz = 3
End Enum

Public Enum LOGIC_MODE
    NotSelected 'Default
    SPI_3V 'Standard GPIO/SPI @ 3.3V
    SPI_1V8 'Standard GPIO/SPI @ 1.8V
    QSPI_3V
    QSPI_1V8
    I2C 'I2C only mode @ 3.3V
    JTAG 'JTAG mode @ 3.3V
    NAND_1V8 'NAND mode @ 1.8V
    NAND_3V3 'NAND mode @ 3.3V
    HF_1V8 'HyperFlash @ 1.8V
    HF_3V3 'HyperFlash @ 3.3V
End Enum