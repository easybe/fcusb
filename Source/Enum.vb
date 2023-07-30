
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
    EMMC = 13
    FWH = 14
    DFU = 15
    P_EEPROM = 16
    Unspecified = 20
End Enum
' Protocols for parallel NOR/NAND devices
Public Enum MEM_PROTOCOL As Byte
    SETUP = 0
    NOR_X8 = 1
    NOR_X8_DQ15 = 2     'X8 device that uses DQ15 for A0
    NOR_X16_BYTE = 3    'X16 device in byte mode (uses DQ15 for LSB)
    NOR_X16_WORD = 4
    NOR_X16_LEGACY = 5
    NAND_X8_ASYNC = 6
    NAND_X16_ASYNC = 7
    FWH = 8
    HYPERFLASH = 9
    EPROM_X8 = 10
    EPROM_X16 = 11
    AT90C = 12
End Enum

Public Enum ConsoleTask As Byte
    NoTask
    Help
    Check
    Detect
    ReadMemory
    WriteMemory
    EraseMemory
    Compare
    ExecuteScript
    License
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

Public Enum BadBlockMarker As Integer
    Disabled = 0
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