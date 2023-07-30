Imports System.Runtime.Serialization

Namespace FlashMemory

    Public Module Constants
        Public Const Kb016 As UInt32 = 2048
        Public Const Kb032 As UInt32 = 4096
        Public Const Kb064 As UInt32 = 8192
        Public Const Kb128 As UInt32 = 16384
        Public Const Kb256 As UInt32 = 32768
        Public Const Kb512 As UInt32 = 65536
        Public Const Mb001 As UInt32 = 131072
        Public Const Mb002 As UInt32 = 262144
        Public Const Mb003 As UInt32 = 393216
        Public Const Mb004 As UInt32 = 524288
        Public Const Mb008 As UInt32 = 1048576
        Public Const Mb016 As UInt32 = 2097152
        Public Const Mb032 As UInt32 = 4194304
        Public Const Mb064 As UInt32 = 8388608
        Public Const Mb128 As UInt32 = 16777216
        Public Const Mb256 As UInt32 = 33554432
        Public Const Mb512 As UInt32 = 67108864
        Public Const Gb001 As UInt32 = 134217728
        Public Const Gb002 As UInt32 = 268435456
        Public Const Gb004 As UInt32 = 536870912
        Public Const Gb008 As UInt32 = 1073741824
        Public Const Gb016 As UInt32 = &H80000000UI
        Public Const Gb032 As UInt64 = &H100000000L
    End Module

    Public Enum MemoryType
        UNSPECIFIED
        PARALLEL_NOR
        OTP_EPROM
        SERIAL_NOR 'SPI devices
        SERIAL_I2C 'I2C EEPROMs
        SERIAL_MICROWIRE
        SERIAL_NAND 'SPI NAND devices
        SLC_NAND 'NAND devices
        JTAG_DMA_RAM 'Vol memory attached to a MCU with DMA access
        JTAG_DMA_CFI 'Non-Vol memory attached to a MCU with DMA access
        JTAG_SPI    'SPI devices connected to an MCU with a SPI access register
        FWH_NOR 'Firmware hub memories
        'HYPERFLASH
        'MLC_NAND
        'TLC_NAND
        DFU_MODE
    End Enum

    Public Enum FlashArea As Byte
        Main = 0 'Data area (read main page, skip to next page)
        OOB = 1 'Extended area (skip main page, read oob page)
        All = 2 'All data (read main page, then oob page, repeat)
        NotSpecified = 255
    End Enum

    Public Enum MFP_PROG
        Standard 'Use the standard sequence that chip id detected
        PageMode 'Writes an entire page of data (128 bytes etc.)
        BypassMode 'Writes 64 bytes using ByPass sequence; 0x555=0xAA;0x2AA=55;0x555=20;(0x00=0xA0;SA=DATA;...)0x00=0x90;0x00=0x00
        IntelSharp 'Writes data (SA=0x40;SA=DATA;SR.7), erases sectors (SA=0x50;SA=0x60;SA=0xD0,SR.7,SA=0x20;SA=0xD0,SR.7)
        Buffer1 'Use Write-To-Buffer mode (x16 only), used by Intel
        Buffer2 'Use Write-To-Buffer mode (x16 only), Used by Spansion/Winbond
    End Enum

    Public Enum MFP_DELAY As UInt16
        None = 0
        uS = 1 'Wait for uS delay cycles (set HARDWARE_DELAY to specify cycles)
        mS = 2 'Wait for mS delay cycles (set HARDWARE_DELAY to specify cycles)
        SR1 = 3 'Wait for Status-Register (0x555=0x70,[sr>>7],EXIT), used by Spansion
        SR2 = 4 'Wait for Status-Register (0x5555=0xAA,0x2AAA=0x55,0x5555=0x70,[sr>>7])
        DQ7 = 5 'Wait for DQ7 to equal last byte written (lower byte for X16)
    End Enum

    Public Enum MFP_IF
        UNKNOWN
        X8_3V
        X8_5V
        X8_5V_12V 'Requires 12V ERASE/PROGRAM
        X16_3V
        X16_3V_12V 'Requires 12V ERASE/PROGRAM
        X16_5V
        X16_5V_12V 'Requires 12V ERASE/PROGRAM
    End Enum

    Public Enum ND_IF
        UNKNOWN
        X8_3V
        X8_1V8
        X16_3V
        X16_1V8
        X8_X16_3V
    End Enum

    Public Enum MFP_BLKLAYOUT
        Four_Top
        Two_Top
        Four_Btm
        Two_Btm
        Dual 'Contans top and bottom boot
        'Uniform block sizes
        Kb016_Uni '2KByte
        Kb032_Uni '4KByte
        Kb064_Uni '8KByte
        Kb128_Uni '16KByte
        Kb256_Uni '32KByte
        Kb512_Uni '64KByte
        Mb001_Uni '128KByte
        'Non-Uniform
        Mb002_NonUni
        Mb032_NonUni
        Mb016_Samsung
        Mb032_Samsung
        Mb064_Samsung
        Mb128_Samsung 'Mb64_Samsung x 2
        Mb256_Samsung
        EntireDevice
    End Enum

    Public Enum EraseMethod
        Standard 'Chip-Erase, then Blank check
        BySector 'Erase each sector (some chips lack Erase All)
        DieErase 'Do a DIE erase for each 32MB die
        Micron 'Some Micron devices need either DieErase or Standard
    End Enum

    Public Enum BadBlockMarker
        noneyet 'Default
    End Enum
    'Contains SPI definition command op codes (usually industry standard)
    Public Class SPI_Command_DEF
        Public Shared RDID As Byte = &H9F 'Read Identification
        Public Shared REMS As Byte = &H90 'Read Electronic Manufacturer Signature 
        Public Shared RES As Byte = &HAB 'Read Electronic Signature
        Public RSFDP As Byte = &H5A 'Read Serial Flash Discoverable Parameters
        Public WRSR As Byte = &H1 'Write Status Register
        Public PROG As Byte = &H2 'Page Program or word program (AAI) command
        Public READ As Byte = &H3 'Read-data
        Public WRDI As Byte = &H4 'Write-Disable
        Public RDSR As Byte = &H5 'Read Status Register
        Public WREN As Byte = &H6 'Write-Enable
        Public FAST_READ As Byte = &HB 'Fast Read
        Public EWSR As Byte = &H50 'Enable Write Status Register (used by SST/PCT chips) or (Clear Flag Status Register)
        Public RDFR As Byte = &H70 'Read Flag Status Register
        Public WRTB As Byte = &H84 'Command to write data into SRAM buffer 1 (used by Atmel)
        Public WRFB As Byte = &H88 'Command to write data from SRAM buffer 1 into page (used by Atmel)
        Public DE As Byte = &HC4 'Die Erase
        Public BE As Byte = &HC7 'Bulk Erase (or chip erase) Sometimes 0x60
        Public SE As Byte = &HD8 'Erases one sector (or one block)
        Public AAI_WORD As Byte = &HAD 'Used for PROG when in AAI Word Mode
        Public AAI_BYTE As Byte = &HAF 'Used for PROG when in AAI Byte Mode
        Public EN4B As Byte = &HB7 'Enter 4-byte address mode (only used for certain 32-bit SPI devices)
        Public EX4B As Byte = &HE9 'Exit 4-byte address mode (only used for certain 32-bit SPI devices)
        Public ULBPR As Byte = &H98 'Global Block Protection Unlock
        Public DIESEL As Byte = &HC2 'Die-Select (used by flashes with multiple die)
    End Class

    Public Enum SPI_QUADMODE
        NotSupported = 0
        all_quadio = 1 'commands are sent using quad i/o
        spisetup_quadio = 2 'commands are sent using SPI, then switches to quad i/o for reading/writing
    End Enum

    Public Enum VENDOR_FEATURE As Integer
        NotSupported = -1
        Micron = 1
        Spansion = 2
        ISSI = 3
    End Enum

    Public Enum SPI_ProgramMode As Byte
        PageMode = 0
        AAI_Byte = 1
        AAI_Word = 2
        Atmel45Series = 3
        SPI_EEPROM = 4
        Nordic = 5
    End Enum

    Public Interface Device
        ReadOnly Property NAME As String 'Manufacturer and part number
        ReadOnly Property FLASH_TYPE As MemoryType
        ReadOnly Property FLASH_SIZE As Long 'Size of this flash device (without spare area)
        ReadOnly Property MFG_CODE As Byte 'The manufaturer byte ID
        ReadOnly Property ID1 As UInt16
        Property ID2 As UInt16
        ReadOnly Property PAGE_SIZE As UInt32 'Size of the pages
        ReadOnly Property Sector_Count As UInt32 'Total number of blocks or sectors this flash device has
        Property ERASE_REQUIRED As Boolean 'Indicates that the sector/block must be erased prior to writing

    End Interface

    Public Class OTP_EPROM
        Implements Device

        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property ID1 As UInt16 Implements Device.ID1
        Public Property ID2 As UInt16 = 0 Implements Device.ID2 'Not used
        Public ReadOnly Property FLASH_TYPE As MemoryType = MemoryType.OTP_EPROM Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public ReadOnly Property Sector_Count As UInt32 = 1 Implements Device.Sector_Count 'We will have to write the entire array
        Public Property PAGE_SIZE As UInt32 = 0 Implements Device.PAGE_SIZE 'Not used
        Public Property ERASE_REQUIRED As Boolean = False Implements Device.ERASE_REQUIRED
        Public Property IS_BLANK As Boolean = False 'On init, do blank check
        Public Property HARDWARE_DELAY As UInt16 = 100 'uS wait after each word program
        Public Property IFACE As MFP_IF = MFP_IF.UNKNOWN

        Sub New(f_name As String, MFG As Byte, ID1 As UInt16, f_size As UInt32, f_if As MFP_IF)
            Me.NAME = f_name
            Me.MFG_CODE = MFG
            Me.ID1 = ID1
            Me.FLASH_SIZE = f_size
            Me.IFACE = f_if
        End Sub

    End Class

    Public Class MFP_Flash
        Implements Device
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property ID1 As UInt16 Implements Device.ID1
        Public Property ID2 As UInt16 Implements Device.ID2
        Public ReadOnly Property FLASH_TYPE As MemoryType = MemoryType.PARALLEL_NOR Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Property AVAILABLE_SIZE As Long 'Number of bytes we have available (less for A25, more for stacked, etc.)
        Public Property PAGE_SIZE As UInt32 = 32 Implements Device.PAGE_SIZE 'Only used for WRITE_PAGE mode of certain flash devices
        Public Property ERASE_REQUIRED As Boolean = True Implements Device.ERASE_REQUIRED
        Public Property WriteMode As MFP_PROG = MFP_PROG.Standard 'This indicates the perfered programing method
        Public Property RESET_ENABLED As Boolean = True 'Indicates if we will call reset/read mode op code
        Public Property HARDWARE_DELAY As UInt16 = 10 'Number of hardware uS to wait between write operations
        Public Property SOFTWARE_DELAY As UInt16 = 100 'Number of software ms to wait between write operations
        Public Property ERASE_DELAY As UInt16 = 250 'Number of ms to wait after an erase operation
        Public Property DELAY_MODE As MFP_DELAY = MFP_DELAY.uS
        Public Property IFACE As MFP_IF = MFP_IF.UNKNOWN

        Sub New(f_name As String, MFG As Byte, ID1 As UInt16, f_size As UInt32, f_if As MFP_IF, block_layout As MFP_BLKLAYOUT, write_mode As MFP_PROG, delay_mode As MFP_DELAY, Optional ID2 As UInt16 = 0)
            Me.NAME = f_name
            Me.MFG_CODE = MFG
            Me.ID1 = ID1
            Me.ID2 = ID2
            Me.FLASH_SIZE = f_size
            Me.AVAILABLE_SIZE = f_size
            Me.IFACE = f_if
            Me.WriteMode = write_mode
            Me.DELAY_MODE = delay_mode
            Dim blocks As UInt32 = (f_size / Kb512)
            Select Case block_layout
                Case MFP_BLKLAYOUT.Four_Top
                    AddSector(Kb512, blocks - 1)
                    AddSector(Kb256, 1)
                    AddSector(Kb064, 2)
                    AddSector(Kb128, 1)
                Case MFP_BLKLAYOUT.Two_Top
                    AddSector(Kb512, blocks - 1)
                    AddSector(Kb064, 8)
                Case MFP_BLKLAYOUT.Four_Btm
                    AddSector(Kb128, 1)
                    AddSector(Kb064, 2)
                    AddSector(Kb256, 1)
                    AddSector(Kb512, blocks - 1)
                Case MFP_BLKLAYOUT.Two_Btm
                    AddSector(Kb064, 8)
                    AddSector(Kb512, blocks - 1)
                Case MFP_BLKLAYOUT.Dual 'this device has small boot blocks on the top and bottom of the device
                    AddSector(Kb064, 8) 'bottom block
                    AddSector(Kb512, blocks - 2)
                    AddSector(Kb064, 8) 'top block
                Case MFP_BLKLAYOUT.Kb016_Uni
                    AddUniformSector(Kb016)
                Case MFP_BLKLAYOUT.Kb032_Uni
                    AddUniformSector(Kb032)
                Case MFP_BLKLAYOUT.Kb064_Uni
                    AddUniformSector(Kb064)
                Case MFP_BLKLAYOUT.Kb128_Uni
                    AddUniformSector(Kb128)
                Case MFP_BLKLAYOUT.Kb256_Uni
                    AddUniformSector(Kb256)
                Case MFP_BLKLAYOUT.Kb512_Uni
                    AddUniformSector(Kb512)
                Case MFP_BLKLAYOUT.Mb001_Uni
                    AddUniformSector(Mb001)
                Case MFP_BLKLAYOUT.Mb002_NonUni
                    AddSector(Mb001) 'Main Block
                    AddSector(98304) 'Main Block
                    AddSector(Kb064) 'Parameter Block
                    AddSector(Kb064) 'Parameter Block
                    AddSector(Kb128) 'Boot Block
                Case MFP_BLKLAYOUT.Mb032_NonUni
                    AddSector(Kb064, 8)
                    AddSector(Kb512, 1)
                    AddSector(Mb001, 31)
                Case MFP_BLKLAYOUT.Mb016_Samsung
                    AddSector(Kb064, 8) '8192    65536
                    AddSector(Kb512, 3) '65536   196608
                    AddSector(Mb002, 6) '262144  7864320
                    AddSector(Kb512, 3) '65536   196608
                    AddSector(Kb064, 8) '8192    65536
                Case MFP_BLKLAYOUT.Mb032_Samsung
                    AddSector(Kb064, 8)  '8192    65536
                    AddSector(Kb512, 3)  '65536   196608
                    AddSector(Mb002, 14) '262144 
                    AddSector(Kb512, 3)  '65536   196608
                    AddSector(Kb064, 8) '8192    65536
                Case MFP_BLKLAYOUT.Mb064_Samsung
                    AddSector(Kb064, 8)  '8192    65536
                    AddSector(Kb512, 3)  '65536   196608
                    AddSector(Mb002, 30) '262144  7864320
                    AddSector(Kb512, 3)  '65536   196608
                    AddSector(Kb064, 8) '8192    65536
                Case MFP_BLKLAYOUT.Mb128_Samsung
                    AddSector(Kb064, 8)  '8192    65536
                    AddSector(Kb512, 3)  '65536   196608
                    AddSector(Mb002, 30) '262144  7864320
                    AddSector(Kb512, 3)  '65536   196608
                    AddSector(Kb064, 8) '8192    65536
                    AddSector(Kb064, 8)  '8192    65536
                    AddSector(Kb512, 3)  '65536   196608
                    AddSector(Mb002, 30) '262144  7864320
                    AddSector(Kb512, 3)  '65536   196608
                    AddSector(Kb064, 8) '8192    65536
                Case MFP_BLKLAYOUT.Mb256_Samsung
                    AddSector(Kb512, 4)   '65536    262144
                    AddSector(Mb002, 126) '262144   33030144
                    AddSector(Kb512, 4)   '65536    262144
                Case MFP_BLKLAYOUT.EntireDevice
                    AddSector(f_size)
            End Select
        End Sub

#Region "Sectors"
        Private SectorList As New List(Of UInt32)

        Public Sub AddSector(ByVal SectorSize As UInt32)
            SectorList.Add(SectorSize)
        End Sub

        Public Sub AddSector(ByVal SectorSize As UInt32, ByVal Count As Integer)
            For i = 1 To Count
                SectorList.Add(SectorSize)
            Next
        End Sub

        Public Sub AddUniformSector(ByVal uniform_block As UInt32)
            Dim TotalSectors As UInt32 = Me.FLASH_SIZE / uniform_block
            For i As UInt32 = 1 To TotalSectors
                SectorList.Add(uniform_block)
            Next
        End Sub

        Public Function GetSectorSize(ByVal SectorIndex As Integer) As Integer
            Try
                Return SectorList(SectorIndex)
            Catch ex As Exception
            End Try
            Return -1
        End Function

        Public ReadOnly Property Sector_Count As UInt32 Implements Device.Sector_Count
            Get
                Return SectorList.Count
            End Get
        End Property

#End Region

    End Class

    <Serializable()> Public Class SPI_NOR_FLASH
        Implements ISerializable
        Implements Device

        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property ID1 As UInt16 Implements Device.ID1
        Public Property ID2 As UInt16 Implements Device.ID2
        Public Property FAMILY As Byte 'SPI Extended byte
        Public ReadOnly Property FLASH_TYPE As MemoryType Implements Device.FLASH_TYPE
        Public Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public Property ERASE_REQUIRED As Boolean Implements Device.ERASE_REQUIRED
        'These are properties unique to SPI devices
        Public Property ADDRESSBITS As UInt32 = 24 'Number of bits the address space takes up (16/24/32)
        Public Property PAGE_COUNT As UInt32 'The total number of pages this flash contains
        Public Property QUAD As SPI_QUADMODE = SPI_QUADMODE.NotSupported 'SQI mode
        Public Property ProgramMode As SPI_ProgramMode
        Public Property STACKED_DIES As UInt32 = 1 'If device has more than one die, set this value
        Public Property SEND_EN4B As Boolean = False 'Set to True to send the EN4BSP
        Public Property SEND_RDFS As Boolean = False 'Set to True to read the flag status register
        Public Property ERASE_SIZE As UInt32 = &H10000 'Number of bytes per page that are erased(typically 64KB)
        Public Property VENDOR_SPECIFIC As VENDOR_FEATURE = VENDOR_FEATURE.NotSupported 'Indicates we can load a unique vendor specific tab
        Public Property SPI_DUMMY As Integer = 8 'Number of cycles after read in SPI operation
        Public Property SQI_DUMMY As Integer = 2 'Number of cycles after read in QUAD operation
        Public Property CHIP_ERASE As EraseMethod = EraseMethod.Standard 'How to erase the entire device
        Public Property SEND_EWSR As Boolean = False 'Set to TRUE to write the enable write status-register
        Public Property PAGE_SIZE As UInt32 Implements Device.PAGE_SIZE  'Number of bytes per page
        Public Property PAGE_SIZE_EXTENDED As UInt32 'Number of bytes in the extended page
        Public Property EXTENDED_MODE As Boolean = False 'True if this device has extended bytes (used by AT45 devices)
        Public Property EEPROM As Byte = 0 'Enumerator used for the specific SPI EEPROM (0=Normal SPI flash)

        Public OP_COMMANDS As New SPI_Command_DEF 'Contains a list of op-codes used to read/write/erase

        Protected Sub New(info As SerializationInfo, context As StreamingContext)
            Me.NAME = info.GetString("m_name")
            OP_COMMANDS.READ = info.GetByte("m_op_rd")
            OP_COMMANDS.PROG = info.GetByte("m_op_wr")
            OP_COMMANDS.SE = info.GetByte("m_op_se")
            OP_COMMANDS.WREN = info.GetByte("m_op_we")
            OP_COMMANDS.BE = info.GetByte("m_op_be")
            OP_COMMANDS.RDSR = info.GetByte("m_op_rdsr")
            OP_COMMANDS.WRSR = info.GetByte("m_op_wrsr")
            OP_COMMANDS.EWSR = info.GetByte("m_op_ewsr")
            Me.FLASH_SIZE = info.GetUInt32("m_flash_size")
            Me.ERASE_SIZE = info.GetUInt32("m_erase_size")
            Me.PAGE_SIZE = info.GetUInt32("m_page_size")
            Me.ProgramMode = info.GetByte("m_prog_mode")
            Me.SEND_EWSR = info.GetBoolean("m_send_ewsr")
            Me.SEND_EN4B = info.GetBoolean("m_send_4byte")
            Me.PAGE_SIZE_EXTENDED = info.GetUInt32("m_page_ext")
            Me.ERASE_REQUIRED = info.GetBoolean("m_erase_req")
            Me.ADDRESSBITS = info.GetUInt32("m_addr_size")
        End Sub

        Public Sub GetObjectData(info As SerializationInfo, context As StreamingContext) Implements ISerializable.GetObjectData
            info.AddValue("m_name", Me.NAME, GetType(String))
            info.AddValue("m_op_rd", OP_COMMANDS.READ, GetType(Byte))
            info.AddValue("m_op_wr", OP_COMMANDS.PROG, GetType(Byte))
            info.AddValue("m_op_se", OP_COMMANDS.SE, GetType(Byte))
            info.AddValue("m_op_we", OP_COMMANDS.WREN, GetType(Byte))
            info.AddValue("m_op_be", OP_COMMANDS.BE, GetType(Byte))
            info.AddValue("m_op_rdsr", OP_COMMANDS.RDSR, GetType(Byte))
            info.AddValue("m_op_wrsr", OP_COMMANDS.WRSR, GetType(Byte))
            info.AddValue("m_op_ewsr", OP_COMMANDS.EWSR, GetType(Byte))
            info.AddValue("m_flash_size", Me.FLASH_SIZE, GetType(UInt32))
            info.AddValue("m_erase_size", Me.ERASE_SIZE, GetType(UInt32))
            info.AddValue("m_page_size", Me.PAGE_SIZE, GetType(UInt32))
            info.AddValue("m_prog_mode", Me.ProgramMode, GetType(Byte))
            info.AddValue("m_send_ewsr", Me.SEND_EWSR, GetType(Boolean))
            info.AddValue("m_send_4byte", Me.SEND_EN4B, GetType(Boolean))
            info.AddValue("m_page_ext", Me.PAGE_SIZE_EXTENDED, GetType(UInt32))
            info.AddValue("m_erase_req", Me.ERASE_REQUIRED, GetType(Boolean))
            info.AddValue("m_addr_size", Me.ADDRESSBITS, GetType(UInt32))
        End Sub

        Sub New(ByVal f_name As String, ByVal f_size As UInt32, ByVal page_size As UInt32)
            Me.NAME = f_name
            Me.FLASH_SIZE = f_size
            Me.PAGE_SIZE = page_size
            If Not (f_size = 0 Or page_size = 0) Then
                Me.PAGE_COUNT = (Me.FLASH_SIZE / page_size)
            End If
            Me.MFG_CODE = 0
            Me.ID1 = 0
            Me.ID2 = 0
            Me.ERASE_REQUIRED = True
            Me.FLASH_TYPE = MemoryType.SERIAL_NOR
        End Sub

        Sub New(ByVal f_name As String, ByVal f_size As UInt32, ByVal MFG As Byte, ByVal ID1 As UInt16, Optional PAGESIZE As UInt32 = 256)
            NAME = f_name
            Me.FLASH_SIZE = f_size
            PAGE_SIZE = PAGESIZE
            If Not (f_size = 0 Or PAGE_SIZE = 0) Then
                Me.PAGE_COUNT = (Me.FLASH_SIZE / PAGE_SIZE)
            End If
            Me.MFG_CODE = MFG
            Me.ID1 = ID1
            If (f_size > Mb128) Then Me.ADDRESSBITS = 32
            ERASE_REQUIRED = True
            FLASH_TYPE = MemoryType.SERIAL_NOR
        End Sub

        Sub New(ByVal f_name As String, ByVal f_size As UInt32, ByVal MFG As Byte, ByVal ID1 As UInt16, ByVal ERASECMD As Byte, ByVal ERASESIZE As UInt32, Optional PAGESIZE As UInt32 = 256)
            Me.NAME = f_name
            Me.FLASH_SIZE = f_size
            Me.PAGE_SIZE = PAGESIZE
            If Not (f_size = 0 Or PAGE_SIZE = 0) Then
                Me.PAGE_COUNT = (Me.FLASH_SIZE / PAGE_SIZE)
            End If
            Me.MFG_CODE = MFG
            Me.ID1 = ID1
            OP_COMMANDS.SE = ERASECMD 'Sometimes 0xD8 or 0x20
            Me.ERASE_SIZE = ERASESIZE
            If (f_size > Mb128) Then Me.ADDRESSBITS = 32
            Me.ERASE_REQUIRED = True
            Me.FLASH_TYPE = MemoryType.SERIAL_NOR
        End Sub
        '32-bit setup command
        Sub New(ByVal f_name As String, ByVal f_size As UInt32, ByVal MFG As Byte, ByVal ID1 As UInt16, ByVal ERASECMD As Byte, ByVal ERASESIZE As UInt32, ByVal READCMD As Byte, ByVal FASTCMD As Byte, ByVal WRITECMD As Byte)
            Me.NAME = f_name
            Me.FLASH_SIZE = f_size
            Me.PAGE_SIZE = 256
            If Not (f_size = 0 Or PAGE_SIZE = 0) Then
                Me.PAGE_COUNT = (Me.FLASH_SIZE / PAGE_SIZE)
            End If
            Me.MFG_CODE = MFG
            Me.ID1 = ID1
            OP_COMMANDS.SE = ERASECMD 'Sometimes 0xD8 or 0x20
            OP_COMMANDS.READ = READCMD
            OP_COMMANDS.FAST_READ = FASTCMD
            OP_COMMANDS.PROG = WRITECMD
            Me.ERASE_SIZE = ERASESIZE
            If (f_size > Mb128) Then Me.ADDRESSBITS = 32
            Me.ERASE_REQUIRED = True
            Me.FLASH_TYPE = MemoryType.SERIAL_NOR
        End Sub

        'Returns the amounts of bytes needed to indicate device address (usually 3 or 4 bytes)
        Public ReadOnly Property AddressBytes() As Integer
            Get
                Return CInt(Math.Ceiling(ADDRESSBITS / 8))
            End Get
        End Property

        Public ReadOnly Property Sector_Count As UInt32 Implements Device.Sector_Count
            Get
                If Me.ERASE_REQUIRED Then
                    Return (FLASH_SIZE / ERASE_SIZE)
                Else
                    Return 1 'EEPROM do not have sectors
                End If
            End Get
        End Property

    End Class

    Public Class SPI_NAND_Flash
        Implements Device

        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property ID1 As UShort Implements Device.ID1
        Public Property ID2 As UShort Implements Device.ID2
        Public Property ERASE_REQUIRED As Boolean Implements Device.ERASE_REQUIRED
        Public ReadOnly Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public ReadOnly Property FLASH_TYPE As MemoryType Implements Device.FLASH_TYPE
        Public ReadOnly Property PAGE_SIZE As UInteger Implements Device.PAGE_SIZE 'The number of bytes in the main area
        Public ReadOnly Property EXT_PAGE_SIZE As UInt16 'The number of bytes in the extended page area
        Public ReadOnly Property BLOCK_SIZE As UInt32 'Number of bytes per block (not including extended pages)
        Public ReadOnly Property PLANE_SELECT As Boolean 'Indicates that this device needs to select a plane when accessing pages
        Public Property STACKED_DIES As UInt32 = 1 'If device has more than one die, set this value
        Public ReadOnly Property Sector_Count As UInt32 Implements Device.Sector_Count
            Get
                Return (FLASH_SIZE / BLOCK_SIZE)
            End Get
        End Property

        Sub New(FlashName As String, MFG As Byte, ID As UInt32, m_size As UInt32, PageSize As UInt16, SpareSize As UInt16, BlockSize As UInt32, ByVal plane_select As Boolean)
            Me.NAME = FlashName
            Me.FLASH_TYPE = MemoryType.SERIAL_NAND
            Me.PAGE_SIZE = PageSize 'Does not include extended / spare pages
            Me.EXT_PAGE_SIZE = SpareSize
            Me.MFG_CODE = MFG
            Me.ID1 = CUShort(ID And &HFFFF)
            Me.ID2 = 0
            Me.FLASH_SIZE = m_size 'Does not include extended /spare areas
            Me.BLOCK_SIZE = BlockSize
            Me.PLANE_SELECT = plane_select
            Me.ERASE_REQUIRED = True
        End Sub

    End Class

    Public Class SLC_NAND_Flash
        Implements Device
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property ID1 As UInt16 Implements Device.ID1
        Public Property ID2 As UInt16 Implements Device.ID2
        Public ReadOnly Property FLASH_TYPE As MemoryType Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public ReadOnly Property PAGE_SIZE As UInt32 Implements Device.PAGE_SIZE 'Total number of bytes per page (not including OOB)
        Public Property STACKED_DIES As UInt32 = 1 'If device has more than one die, set this value
        Public Property ERASE_REQUIRED As Boolean Implements Device.ERASE_REQUIRED
        Public Property EXT_PAGE_SIZE As UInt32 'The number of bytes in the spare area
        Public Property BLOCK_SIZE As UInt32 'Number of bytes per block (not including extended pages)
        Public Property IFACE As ND_IF = ND_IF.UNKNOWN

        Public ReadOnly Property Sector_Count As UInt32 Implements Device.Sector_Count
            Get
                Return (FLASH_SIZE / BLOCK_SIZE)
            End Get
        End Property

        Sub New(FlashName As String, MFG As Byte, ID As UInt32, m_size As Long, PageSize As UInt16, SpareSize As UInt16, BlockSize As UInt32, lv As ND_IF)
            Me.NAME = FlashName
            Me.FLASH_TYPE = MemoryType.SLC_NAND
            Me.PAGE_SIZE = PageSize 'Does not include extended / spare pages
            Me.EXT_PAGE_SIZE = SpareSize
            Me.MFG_CODE = MFG
            Me.IFACE = lv
            If Not ID = 0 Then
                While ((ID And &HFF000000UI) = 0)
                    ID = (ID << 8)
                End While
                Me.ID1 = (ID >> 16)
                Me.ID2 = (ID And &HFFFF)
            End If
            Me.FLASH_SIZE = m_size 'Does not include extended /spare areas
            Me.BLOCK_SIZE = BlockSize
            Me.ERASE_REQUIRED = True
        End Sub

    End Class

    Public Class FWH_Flash
        Implements Device
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property ID1 As UInt16 Implements Device.ID1
        Public Property ID2 As UInt16 Implements Device.ID2
        Public ReadOnly Property FLASH_TYPE As MemoryType = MemoryType.FWH_NOR Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public Property PAGE_SIZE As UInt32 = 32 Implements Device.PAGE_SIZE 'Only used for WRITE_PAGE mode of certain flash devices
        Public Property ERASE_REQUIRED As Boolean = True Implements Device.ERASE_REQUIRED
        Public ReadOnly Property SECTOR_SIZE As UInt32
        Public ReadOnly Property SECTOR_COUNT As UInt32 Implements Device.Sector_Count

        Public ReadOnly ERASE_CMD As Byte

        Sub New(f_name As String, MFG As Byte, ID1 As UInt16, f_size As UInt32, sector_size As UInt32, sector_erase As Byte)
            Me.NAME = f_name
            Me.MFG_CODE = MFG
            Me.ID1 = ID1
            Me.ID2 = ID2
            Me.FLASH_SIZE = f_size
            Me.SECTOR_SIZE = sector_size
            Me.SECTOR_COUNT = (f_size / sector_size)
            Me.ERASE_CMD = sector_erase
        End Sub

    End Class

    Public Class FlashDatabase
        Public FlashDB As New List(Of Device)

        Sub New()
            SPINOR_Database() 'Adds all of the SPI and QSPI devices
            SPINAND_Database() 'Adds all of the SPI NAND devices
            MFP_Database() 'Adds all of the TSOP/PLCC etc. devices
            NAND_Database() 'Adds all of the SLC NAND (x8) compatible devices
            OTP_Database() 'Adds all of the OTP EPROM devices
            FWH_Database() 'Adds all of the firmware hub devices
            'Add device specific features
            Dim MT25QL02GC As SPI_NOR_FLASH = FindDevice(&H20, &HBA22, 0, MemoryType.SERIAL_NOR)
            MT25QL02GC.QUAD = SPI_QUADMODE.all_quadio
            MT25QL02GC.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron 'Adds the non-vol tab to the GUI
            MT25QL02GC.SEND_RDFS = True 'Will read the flag-status register after a erase/programer opertion
            MT25QL02GC.CHIP_ERASE = EraseMethod.Micron   'Will erase all of the sectors instead

            Dim N25Q00AA_3V As SPI_NOR_FLASH = FindDevice(&H20, &HBA21, 0, MemoryType.SERIAL_NOR)
            N25Q00AA_3V.QUAD = SPI_QUADMODE.all_quadio
            N25Q00AA_3V.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron 'Adds the non-vol tab to the GUI
            N25Q00AA_3V.SEND_RDFS = True 'Will read the flag-status register after a erase/programer opertion
            N25Q00AA_3V.CHIP_ERASE = EraseMethod.Micron  'Will erase all of the sectors instead
            N25Q00AA_3V.STACKED_DIES = 4

            Dim N25Q00AA_1V8 As SPI_NOR_FLASH = FindDevice(&H20, &HBB21, 0, MemoryType.SERIAL_NOR) 'CV
            N25Q00AA_1V8.QUAD = SPI_QUADMODE.all_quadio
            N25Q00AA_1V8.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron 'Adds the non-vol tab to the GUI
            N25Q00AA_1V8.SEND_RDFS = True 'Will read the flag-status register after a erase/programer opertion
            N25Q00AA_1V8.CHIP_ERASE = EraseMethod.Micron  'Will erase all of the sectors instead
            N25Q00AA_1V8.STACKED_DIES = 4

            Dim N25Q512 As SPI_NOR_FLASH = FindDevice(&H20, &HBA20, 0, MemoryType.SERIAL_NOR)
            N25Q512.QUAD = SPI_QUADMODE.all_quadio
            N25Q512.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron 'Adds the non-vol tab to the GUI
            N25Q512.SEND_RDFS = True 'Will read the flag-status register after a erase/programer opertion
            N25Q512.CHIP_ERASE = EraseMethod.Micron 'Will erase all of the sectors instead

            Dim N25Q256 As SPI_NOR_FLASH = FindDevice(&H20, &HBA19, 0, MemoryType.SERIAL_NOR)
            N25Q256.QUAD = SPI_QUADMODE.all_quadio
            N25Q256.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron 'Adds the non-vol tab to the GUI
            N25Q256.SEND_RDFS = True 'Will read the flag-status register after a erase/programer opertion

            Dim S25FL116K As SPI_NOR_FLASH = FindDevice(&H1, &H4015, 0, MemoryType.SERIAL_NOR)
            S25FL116K.QUAD = SPI_QUADMODE.all_quadio
            S25FL116K.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion

            Dim S25FL132K As SPI_NOR_FLASH = FindDevice(&H1, &H4016, 0, MemoryType.SERIAL_NOR)
            S25FL132K.QUAD = SPI_QUADMODE.all_quadio
            S25FL132K.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion

            Dim S25FL164K As SPI_NOR_FLASH = FindDevice(&H1, &H4017, 0, MemoryType.SERIAL_NOR)
            S25FL164K.QUAD = SPI_QUADMODE.all_quadio
            S25FL164K.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion

            Dim IS25LQ032 As SPI_NOR_FLASH = FindDevice(&H9D, &H4016, 0, MemoryType.SERIAL_NOR)
            IS25LQ032.QUAD = SPI_QUADMODE.all_quadio
            IS25LQ032.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI
            Dim IS25LQ016 As SPI_NOR_FLASH = FindDevice(&H9D, &H4015, 0, MemoryType.SERIAL_NOR)
            IS25LQ016.QUAD = SPI_QUADMODE.all_quadio
            IS25LQ016.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI
            Dim IS25LP080D As SPI_NOR_FLASH = FindDevice(&H9D, &H6014, 0, MemoryType.SERIAL_NOR)
            IS25LP080D.QUAD = SPI_QUADMODE.all_quadio
            IS25LP080D.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI
            Dim IS25WP080D As SPI_NOR_FLASH = FindDevice(&H9D, &H7014, 0, MemoryType.SERIAL_NOR)
            IS25WP080D.QUAD = SPI_QUADMODE.all_quadio
            IS25WP080D.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI
            Dim IS25WP040D As SPI_NOR_FLASH = FindDevice(&H9D, &H7013, 0, MemoryType.SERIAL_NOR)
            IS25WP040D.QUAD = SPI_QUADMODE.all_quadio
            IS25WP040D.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI
            Dim IS25WP020D As SPI_NOR_FLASH = FindDevice(&H9D, &H7012, 0, MemoryType.SERIAL_NOR)
            IS25WP020D.QUAD = SPI_QUADMODE.all_quadio
            IS25WP020D.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI
            Dim IS25LP256 As SPI_NOR_FLASH = FindDevice(&H9D, &H6019, 0, MemoryType.SERIAL_NOR)
            IS25LP256.QUAD = SPI_QUADMODE.all_quadio
            IS25LP256.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI
            Dim IS25WP256 As SPI_NOR_FLASH = FindDevice(&H9D, &H7019, 0, MemoryType.SERIAL_NOR)
            IS25WP256.QUAD = SPI_QUADMODE.all_quadio
            IS25WP256.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI
            Dim IS25LP128 As SPI_NOR_FLASH = FindDevice(&H9D, &H6018, 0, MemoryType.SERIAL_NOR)
            IS25LP128.QUAD = SPI_QUADMODE.all_quadio
            IS25LP128.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI

            'CreateHtmlCatalog(MemoryType.SERIAL_NOR, 3, "d: \spi_database.html")
            'CreateHtmlCatalog(MemoryType.SERIAL_NAND, 3, "d:\spinand_database.html")
            'CreateHtmlCatalog(MemoryType.PARALLEL_NOR, 3, "d:\mpf_database.html")
            'CreateHtmlCatalog(MemoryType.SLC_NAND, 3, "d:\nand_database.html")

        End Sub

        Private Sub SPINOR_Database()
            'Adesto 25/25 Series (formely Atmel)
            FlashDB.Add(CreateSeries45("Adesto AT45DB641E", Mb064, &H2800, &H100, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB642D", Mb064, &H2800, 0, 1024))
            FlashDB.Add(CreateSeries45("Adesto AT45DB321E", Mb032, &H2701, &H100, 512))
            FlashDB.Add(CreateSeries45("Adesto AT45DB321D", Mb032, &H2701, 0, 512))
            FlashDB.Add(CreateSeries45("Adesto AT45DB161E", Mb016, &H2600, &H100, 512)) 'Testing?
            FlashDB.Add(CreateSeries45("Adesto AT45DB161D", Mb016, &H2600, 0, 512))
            FlashDB.Add(CreateSeries45("Adesto AT45DB081E", Mb008, &H2500, &H100, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB081D", Mb008, &H2500, 0, 256)) '<--
            FlashDB.Add(CreateSeries45("Adesto AT45DB041E", Mb004, &H2400, &H100, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB041D", Mb004, &H2400, 0, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB021E", Mb002, &H2300, &H100, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB021D", Mb002, &H2300, 0, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB011D", Mb001, &H2200, 0, 256))
            FlashDB.Add(New SPI_NOR_FLASH("Adesto AT25DF641", Mb064, &H1F, &H4800)) 'Confirmed (build 350)
            FlashDB.Add(New SPI_NOR_FLASH("Adesto AT25DF321S", Mb032, &H1F, &H4701))
            FlashDB.Add(New SPI_NOR_FLASH("Adesto AT25DF321", Mb032, &H1F, &H4700))
            FlashDB.Add(New SPI_NOR_FLASH("Adesto AT25DF161", Mb016, &H1F, &H4602))
            FlashDB.Add(New SPI_NOR_FLASH("Adesto AT25DF081", Mb008, &H1F, &H4502))
            FlashDB.Add(New SPI_NOR_FLASH("Adesto AT25DF021", Mb002, &H1F, &H4300))
            FlashDB.Add(New SPI_NOR_FLASH("Adesto AT26DF321", Mb032, &H1F, &H4700))
            FlashDB.Add(New SPI_NOR_FLASH("Adesto AT26DF161", Mb016, &H1F, &H4600))
            FlashDB.Add(New SPI_NOR_FLASH("Adesto AT26DF161A", Mb016, &H1F, &H4601))
            FlashDB.Add(New SPI_NOR_FLASH("Adesto AT26DF081A", Mb008, &H1F, &H4501))
            FlashDB.Add(New SPI_NOR_FLASH("Adesto AT25SL321", Mb032, &H1F, &H4216)) '1.8v
            FlashDB.Add(New SPI_NOR_FLASH("Adesto AT25SF321", Mb032, &H1F, &H8701))
            FlashDB.Add(New SPI_NOR_FLASH("Adesto AT25SF161", Mb016, &H1F, &H8601))
            FlashDB.Add(New SPI_NOR_FLASH("Adesto AT25SF081", Mb008, &H1F, &H8501))
            FlashDB.Add(New SPI_NOR_FLASH("Adesto AT25SF041", Mb004, &H1F, &H8401))
            FlashDB.Add(New SPI_NOR_FLASH("Adesto AT25XV041", Mb004, &H1F, &H4402))
            FlashDB.Add(New SPI_NOR_FLASH("Adesto AT25XV021", Mb002, &H1F, &H4301))
            'Cypress 25FL Series (formely Spansion)
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S70FL01GS", Gb001, &H1, &H221, &HDC, &H40000, &H13, &HC, &H12))
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL512S", Mb512, &H1, &H220, &HDC, &H40000, &H13, &HC, &H12))
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S70FL256P", Mb256, 0)) 'Placeholder (uses two S25FL128S, PIN6 is CS2)
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL256S", Mb256, &H1, &H219, &HDC, &H40000, &H13, &HC, &H12) With {.ID2 = &H4D00}) 'Confirmed (build 371)
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL256S", Mb256, &H1, &H219, &HDC, &H10000, &H13, &HC, &H12) With {.ID2 = &H4D01})
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL128P", Mb128, &H1, &H2018) With {.ERASE_SIZE = Kb512, .ID2 = &H301}) '0301h X
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL128P", Mb128, &H1, &H2018) With {.ERASE_SIZE = Mb002, .ID2 = &H300}) '0300h X
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL129P", Mb128, &H1, &H2018) With {.ERASE_SIZE = Kb512, .ID2 = &H4D01}) '4D01h X
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL129P", Mb128, &H1, &H2018) With {.ERASE_SIZE = Mb002, .ID2 = &H4D00}) '4D00h X
            FlashDB.Add(New SPI_NOR_FLASH("Cypress FL127S/FL128S", Mb128, &H1, &H2018) With {.ERASE_SIZE = Kb512, .ID2 = &H4D01, .FAMILY = &H80})
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL128S", Mb128, &H1, &H2018) With {.ERASE_SIZE = Mb002, .ID2 = &H4D00, .FAMILY = &H80})
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL127S", Mb128, 0)) 'Placeholder for database files
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL128L", Mb128, &H1, &H6018))
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL064L", Mb064, &H1, &H6017))
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL064", Mb064, &H1, &H216))
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL032", Mb032, &H1, &H215))
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL016A", Mb016, &H1, &H214))
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL008A", Mb008, &H1, &H213))
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL040A", Mb004, &H1, &H212))
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL164K", Mb064, &H1, &H4017))
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL132K", Mb032, &H1, &H4016))
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL216K", Mb016, &H1, &H4015)) 'Uses the same ID as S25FL116K (might support 3 byte ID)
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL116K", Mb016, &H1, &H4015))
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL208K", Mb008, &H1, &H4014))
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL204K", Mb004, &H1, &H4013))
            FlashDB.Add(New SPI_NOR_FLASH("Cypress S25FL004A", Mb004, &H1, &H212))
            'Micron (ST)
            FlashDB.Add(New SPI_NOR_FLASH("Micron MT25QL02GC", Gb002, &H20, &HBA22) With {.SEND_EN4B = True})
            FlashDB.Add(New SPI_NOR_FLASH("Micron N25Q00AA", Gb001, &H20, &HBA21) With {.SEND_EN4B = True}) '3V
            FlashDB.Add(New SPI_NOR_FLASH("Micron N25Q00AA", Gb001, &H20, &HBB21) With {.SEND_EN4B = True}) '1.8V CV
            FlashDB.Add(New SPI_NOR_FLASH("Micron N25Q512A", Mb512, &H20, &HBA20) With {.SEND_EN4B = True})
            FlashDB.Add(New SPI_NOR_FLASH("Micron N25Q512A", Mb512, &H20, &HBB20) With {.SEND_EN4B = True}) '1.8v 
            FlashDB.Add(New SPI_NOR_FLASH("Micron N25Q256A", Mb256, &H20, &HBA19) With {.SEND_EN4B = True}) '3.3v version
            FlashDB.Add(New SPI_NOR_FLASH("Micron N25Q256A", Mb256, &H20, &HBB19) With {.SEND_EN4B = True}) '1.8v version
            FlashDB.Add(New SPI_NOR_FLASH("Micron NP5Q128A", Mb128, &H20, &HDA18, 64) With {.ERASE_SIZE = &H20000}) 'NEW! PageSize is 64 bytes
            FlashDB.Add(New SPI_NOR_FLASH("Micron N25Q128", Mb128, &H20, &HBA18))
            FlashDB.Add(New SPI_NOR_FLASH("Micron N25Q064A", Mb064, &H20, &HBB17))
            FlashDB.Add(New SPI_NOR_FLASH("Micron N25Q064", Mb064, &H20, &HBA17))
            FlashDB.Add(New SPI_NOR_FLASH("Micron N25Q032", Mb032, &H20, &HBA16))
            FlashDB.Add(New SPI_NOR_FLASH("Micron N25Q016", Mb016, &H20, &HBB15))
            FlashDB.Add(New SPI_NOR_FLASH("Micron N25Q008", Mb008, &H20, &HBB14))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25P128", Mb128, &H20, &H2018) With {.ERASE_SIZE = Mb002}) 'Confirmed (build 350)
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25P64", Mb064, &H20, &H2017)) 'Confirmed (Build 350)
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25PX32", Mb032, &H20, &H7116))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25P32", Mb032, &H20, &H2016))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25PX16", Mb016, &H20, &H7115))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25PX16", Mb016, &H20, &H7315)) 'Older version?
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25P16", Mb016, &H20, &H2015))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25P80", Mb008, &H20, &H2014))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25PX80", Mb008, &H20, &H7114)) 'Build 370
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25P40", Mb004, &H20, &H2013))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25P20", Mb002, &H20, &H2012))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25P10", Mb001, &H20, &H2011))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25P05", Kb512, &H20, &H2010))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25PX64", Mb064, &H20, &H7117))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25PX32", Mb032, &H20, &H7116))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25PX16", Mb016, &H20, &H7115))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25PE16", Mb016, &H20, &H8015))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25PE80", Mb008, &H20, &H8014))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25PE40", Mb004, &H20, &H8013))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25PE20", Mb002, &H20, &H8012))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M25PE10", Mb001, &H20, &H8011))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M45PE16", Mb016, &H20, &H4015))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M45PE80", Mb008, &H20, &H4014))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M45PE40", Mb004, &H20, &H4013))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M45PE20", Mb002, &H20, &H4012))
            FlashDB.Add(New SPI_NOR_FLASH("Micron M45PE10", Mb001, &H20, &H4011))
            'Windbond
            'http://www.nexflash.com/hq/enu/ProductAndSales/ProductLines/FlashMemory/SerialFlash/
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25M512JV", Mb512, &HEF, &H7119) With {.SEND_EN4B = True, .STACKED_DIES = 2}) 'Confirmed working (7/13/17)
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q512", Mb512, &HEF, &H4020) With {.SEND_EN4B = True})
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q256JV", Mb256, &HEF, &H7019) With {.SEND_EN4B = True})
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q256", Mb256, &HEF, &H4019) With {.SEND_EN4B = True})
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q128JV", Mb128, &HEF, &H7018))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q128", Mb128, &HEF, &H4018)) 'CV
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q64", Mb064, &HEF, &H4017))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q32", Mb032, &HEF, &H4016))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q16", Mb016, &HEF, &H4015)) 'Confirmed (Build 350)
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q80", Mb008, &HEF, &H4014)) 'Confirmed (Build 350)
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q80BW", Mb008, &HEF, &H5014)) 'Added in build 350
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q40", Mb004, &HEF, &H4013))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q128FW", Mb128, &HEF, &H6018))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q64FW", Mb064, &HEF, &H6017))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q64JV", Mb064, &HEF, &H7017))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q32FW", Mb032, &HEF, &H6016))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q16FW", Mb016, &HEF, &H6015))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q08EW", Mb008, &HEF, &H6014))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25Q40BW", Mb004, &HEF, &H5013))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25X64", Mb064, &HEF, &H3017))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25X64", Mb064, &HEF, &H3017))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25X32", Mb032, &HEF, &H3016))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25X16", Mb016, &HEF, &H3015))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25X80", Mb008, &HEF, &H3014))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25X40", Mb004, &HEF, &H3013))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25X20", Mb002, &HEF, &H3012))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25X10", Mb002, &HEF, &H3011))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25X05", Mb001, &HEF, &H3010))
            FlashDB.Add(New SPI_NOR_FLASH("Winbond W25M121AV", 0, 0)) 'Contains a NOR die and NAND die
            'MXIC
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L51245G", Mb512, &HC2, &H201A) With {.SEND_EN4B = True}) 'Added (Build 372)
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L25655E", Mb256, &HC2, &H2619) With {.SEND_EN4B = True}) 'Added (Build 371)
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L256", Mb256, &HC2, &H2019) With {.SEND_EN4B = True}) 'Added (Build 350)
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L12855E", Mb128, &HC2, &H2618)) 'Added Build 372
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L128", Mb128, &HC2, &H2018))
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25U12873F", Mb128, &HC2, &H2538)) '1.8V SQI
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25R6435", Mb064, &HC2, &H2817))
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L6455E", Mb064, &HC2, &H2617)) 'Added Build 372
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L640", Mb064, &HC2, &H2017))
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L320", Mb032, &HC2, &H2016)) 'Confirmed Build 350
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L3205D", Mb032, &HC2, &H20FF))
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L323", Mb032, &HC2, &H5E16))
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L3255E", Mb032, &HC2, &H9E16))
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25U3235F", Mb032, &HC2, &H2536)) '1.8v only
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25R3235F", Mb032, &HC2, &H2816)) '1.8-3.3v compatible
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L1633E", Mb016, &HC2, &H2415))
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L160", Mb016, &HC2, &H2015))
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L80", Mb008, &HC2, &H2014))
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L40", Mb004, &HC2, &H2013))
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L20", Mb002, &HC2, &H2012)) 'MX25L2005 MX25L2006E MX25L2026E
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L10", Mb001, &HC2, &H2011))
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25U643", Mb064, &HC2, &H2537))
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25U323", Mb032, &HC2, &H2536))
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25U163", Mb016, &HC2, &H2535))
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25U803", Mb008, &HC2, &H2534))
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L512", Kb512, &HC2, &H2010)) 'Added (Build 371)
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L1021E", Mb001, &HC2, &H2211)) 'Added (Build 371)
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25L5121E", Kb512, &HC2, &H2210)) 'Added (Build 371)
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX66L51235F", Mb512, &HC2, &H201A) With {.SEND_EN4B = True}) 'Uses MX25L51245G (Build 405)
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25V8035", Mb008, &HC2, &H2554))
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25V4035", Mb004, &HC2, &H2553))
            FlashDB.Add(New SPI_NOR_FLASH("MXIC MX25V8035F", Mb008, &HC2, &H2314))
            'EON
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25Q128", Mb128, &H1C, &H3018)) 'Fixed
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25Q64", Mb064, &H1C, &H3017))
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25Q32", Mb032, &H1C, &H3016))
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25Q16", Mb016, &H1C, &H3015))
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25Q80", Mb008, &H1C, &H3014))
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25Q40", Mb004, &H1C, &H3013))
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25QH128", Mb128, &H1C, &H7018)) 'Added build 402
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25QH64", Mb064, &H1C, &H7017))
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25QH32", Mb032, &H1C, &H7016)) 'Added build 350
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25QH16", Mb016, &H1C, &H7015)) 'Added build 402
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25QH80", Mb008, &H1C, &H7014)) 'Added build 402
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25P64", Mb064, &H1C, &H2017))
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25P32", Mb032, &H1C, &H2016))
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25P16", Mb016, &H1C, &H2015))
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25F32", Mb032, &H1C, &H3116)) 'Added build 372
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25F16", Mb016, &H1C, &H3115)) 'Added build 372
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25F80", Mb008, &H1C, &H3114))
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25F40", Mb004, &H1C, &H3113))
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25F20", Mb002, &H1C, &H3112))
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25T32", Mb032, &H1C, &H5116))
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25T16", Mb016, &H1C, &H5115))
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25T80", Mb008, &H1C, &H5114))
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25T40", Mb004, &H1C, &H5113))
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25T20", Mb002, &H1C, &H5112))
            FlashDB.Add(New SPI_NOR_FLASH("EON EN25F10", Mb001, &H1C, &H3111))
            'Microchip / Silicon Storage Technology (SST) / PCT Group (Rebranded)
            'http://www.microchip.com/pagehandler/en-us/products/memory/flashmemory/sfhome.html
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST26VF064B", Mb064, &HBF, &H2643)) 'SST26VF064BA
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST26VF064", Mb064, &HBF, &H2603))
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST26VF032", Mb032, &HBF, &H2602)) 'PCT26VF032
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST26VF032B", Mb032, &HBF, &H2642, &H20, &H1000)) 'SST26VF032BA
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST26WF032", Mb032, &HBF, &H2622)) 'PCT26WF032
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST26VF016", Mb016, &HBF, &H2601, &H20, &H1000) With {.QUAD = SPI_QUADMODE.all_quadio})
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST26VF032", Mb032, &HBF, &H2602, &H20, &H1000) With {.QUAD = SPI_QUADMODE.all_quadio})
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST26VF016B", Mb016, &HBF, &H2641, &H20, &H1000)) 'SST26VF016BA
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST26VF016", Mb016, &HBF, &H16BF, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte})
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST26WF016B", Mb016, &HBF, &H2651)) 'SST26WF016BA
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST26WF080B", Mb008, &HBF, &H2658, &H20, &H1000))
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST26WF040B", Mb004, &HBF, &H2654, &H20, &H1000))
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25VF128B", Mb128, &HBF, &H2544) With {.SEND_EWSR = True}) 'Might use AAI
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25VF064C", Mb064, &HBF, &H254B) With {.SEND_EWSR = True}) 'PCT25VF064C
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25VF032B", Mb032, &HBF, &H254A) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True}) 'PCT25VF032B
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25VF032", Mb032, &HBF, &H2542) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25VF016B", Mb016, &HBF, &H2541) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True}) 'Confirmed (Build 350)
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25VF080B", Mb008, &HBF, &H258E, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True}) 'PCT25VF080B - Confirmed (Build 350)
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25VF080", Mb008, &HBF, &H80BF, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte, .SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25WF080B", Mb008, &H62, &H1614, &H20, &H1000) With {.SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25PF040C", Mb004, &H62, &H613))
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25WF040B", Mb004, &H62, &H1613, &H20, &H1000) With {.SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25VF040B", Mb004, &HBF, &H258D, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True}) 'PCT25VF040B <--testing
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25WF040", Mb004, &HBF, &H2504, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25WF020A", Mb002, &H62, &H1612, &H20, &H1000) With {.SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25LF020A", Mb002, &HBF, &H43BF, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte, .SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25WF020", Mb002, &HBF, &H2503, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25VF020", Mb002, &HBF, &H258C, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True}) 'SST25VF020B SST25PF020B PCT25VF020B
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25WF010", Mb001, &HBF, &H2502, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25VF010", Mb001, &HBF, &H49BF, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte, .SEND_EWSR = True}) 'SST25VF010A PCT25VF010A
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25WF512", Kb512, &HBF, &H2501, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25VF512", Kb512, &HBF, &H48, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte, .SEND_EWSR = True}) 'SST25VF512A PCT25VF512A REMS ONLY
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25VF020A", Mb002, &HBF, &H43, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte, .SEND_EWSR = True}) 'Confirmed (Build 350)
            FlashDB.Add(New SPI_NOR_FLASH("Microchip SST25VF010A", Mb001, &HBF, &H49, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte, .SEND_EWSR = True}) 'Confirmed (Build 350)
            'PMC
            FlashDB.Add(New SPI_NOR_FLASH("PMC PM25LV016B", Mb016, &H7F, &H9D14))
            FlashDB.Add(New SPI_NOR_FLASH("PMC PM25LV080B", Mb008, &H7F, &H9D13))
            FlashDB.Add(New SPI_NOR_FLASH("PMC PM25LV040", Mb004, &H9D, &H7E7F))
            FlashDB.Add(New SPI_NOR_FLASH("PMC PM25LV020", Mb002, &H9D, &H7D7F))
            FlashDB.Add(New SPI_NOR_FLASH("PMC PM25LV010", Mb001, &H9D, &H7C7F))
            FlashDB.Add(New SPI_NOR_FLASH("PMC PM25LV512", Kb512, &H9D, &H7B7F))
            FlashDB.Add(New SPI_NOR_FLASH("PMC PM25LD020", Mb002, &H7F, &H9D22))
            FlashDB.Add(New SPI_NOR_FLASH("PMC Pm25LD010", Mb001, &H7F, &H9D21))
            FlashDB.Add(New SPI_NOR_FLASH("PMC Pm25LD512", Kb512, &H7F, &H9D20))
            'AMIC
            'http://www.amictechnology.com/english/flash_spi_flash.html
            FlashDB.Add(New SPI_NOR_FLASH("AMIC A25LQ64", Mb064, &H37, &H4017)) 'A25LMQ64
            FlashDB.Add(New SPI_NOR_FLASH("AMIC A25LQ32A", Mb032, &H37, &H4016))
            FlashDB.Add(New SPI_NOR_FLASH("AMIC A25L032", Mb032, &H37, &H3016))
            FlashDB.Add(New SPI_NOR_FLASH("AMIC A25L016", Mb016, &H37, &H3015))
            FlashDB.Add(New SPI_NOR_FLASH("AMIC A25LQ16", Mb016, &H37, &H4015))
            FlashDB.Add(New SPI_NOR_FLASH("AMIC A25L080", Mb008, &H37, &H3014))
            FlashDB.Add(New SPI_NOR_FLASH("AMIC A25L040", Mb004, &H37, &H3013)) 'A25L040A A25P040
            FlashDB.Add(New SPI_NOR_FLASH("AMIC A25L020", Mb002, &H37, &H3012)) 'A25L020C A25P020
            FlashDB.Add(New SPI_NOR_FLASH("AMIC A25L010", Mb001, &H37, &H3011)) 'A25L010A A25P010
            FlashDB.Add(New SPI_NOR_FLASH("AMIC A25L512", Kb512, &H37, &H3010)) 'A25L512A A25P512
            FlashDB.Add(New SPI_NOR_FLASH("AMIC A25LS512A", Kb512, &HC2, &H2010))
            'Fidelix
            'http://www.fidelix.co.kr/semiconductor/eng/product/serialflash.jsp
            FlashDB.Add(New SPI_NOR_FLASH("Fidelix FM25Q16A", Mb016, &HF8, &H3215)) 'FM25Q16B
            FlashDB.Add(New SPI_NOR_FLASH("Fidelix FM25Q32A", Mb032, &HF8, &H3216))
            FlashDB.Add(New SPI_NOR_FLASH("Fidelix FM25M04A", Mb004, &HF8, &H4213))
            FlashDB.Add(New SPI_NOR_FLASH("Fidelix FM25M08A", Mb008, &HF8, &H4214))
            FlashDB.Add(New SPI_NOR_FLASH("Fidelix FM25M16A", Mb016, &HF8, &H4215))
            FlashDB.Add(New SPI_NOR_FLASH("Fidelix FM25M32A", Mb032, &HF8, &H4216))
            FlashDB.Add(New SPI_NOR_FLASH("Fidelix FM25M64A", Mb064, &HF8, &H4217))
            FlashDB.Add(New SPI_NOR_FLASH("Fidelix FM25M4AA", Mb004, &HF8, &H4212))
            'Gigadevice
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25Q128", Mb128, &HC8, &H4018))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25Q64", Mb064, &HC8, &H4017))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25Q32", Mb032, &HC8, &H4016))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25Q16", Mb016, &HC8, &H4015))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25Q80", Mb008, &HC8, &H4014))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25Q40", Mb004, &HC8, &H4013))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25Q20", Mb002, &HC8, &H4012))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25Q10", Mb001, &HC8, &H4011))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25Q512", Kb512, &HC8, &H4010))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25VQ16C", Mb016, &HC8, &H4215))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25VQ80C", Mb008, &HC8, &H4214))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25VQ41B", Mb004, &HC8, &H4213))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25VQ21B", Mb002, &HC8, &H4212))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25LQ128", Mb128, &HC8, &H6018))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25LQ64", Mb064, &HC8, &H6017))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25LQ32", Mb032, &HC8, &H6016))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25LQ16", Mb016, &HC8, &H6015))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25LQ80", Mb008, &HC8, &H6014))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25LQ40", Mb004, &HC8, &H6013))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25LQ20", Mb002, &HC8, &H6012))
            FlashDB.Add(New SPI_NOR_FLASH("Gigadevice GD25LQ10", Mb001, &HC8, &H6011))
            'ISSI
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25CD020", Mb002, &H9D, &H1122))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25CD010", Mb001, &H9D, &H1021))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25CD512", Kb512, &H9D, &H520))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25CD025", Kb256, &H7F, &H9D2F))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25CQ032", Mb032, &H7F, &H9D46))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25LP256", Mb256, &H9D, &H6019))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25LP128", Mb128, &H9D, &H6018))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25LP064", Mb064, &H9D, &H6017))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25LP032", Mb032, &H9D, &H6016))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25LP016", Mb016, &H9D, &H6015))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25LP080", Mb008, &H9D, &H6014))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25LQ032", Mb032, &H9D, &H4016))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25LQ016", Mb016, &H9D, &H4015))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25LQ080", Mb008, &H9D, &H1344))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25LQ040", Mb004, &H7F, &H9D43))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25LQ020", Mb002, &H7F, &H9D42)) 'Verified
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25LQ010", Mb001, &H9D, &H4011))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25LQ512", Kb512, &H9D, &H4010))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25LQ025", Kb256, &H9D, &H4009))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25LD040", Mb004, &H7F, &H9D7E))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25WD040", Mb004, &H7F, &H9D33))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25WD020", Mb002, &H7F, &H9D32))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25WP256", Mb256, &H9D, &H7019))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25WP128", Mb128, &H9D, &H7018))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25WP064", Mb064, &H9D, &H7017))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25WP032", Mb032, &H9D, &H7016))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25WP016", Mb016, &H9D, &H7015))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25WP080", Mb008, &H9D, &H7014))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25WP040", Mb004, &H9D, &H7013))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25WP020", Mb002, &H9D, &H7012))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25WQ040", Mb004, &H9D, &H1253))
            FlashDB.Add(New SPI_NOR_FLASH("ISSI IS25WQ020", Mb002, &H9D, &H1152))
            'Others
            FlashDB.Add(New SPI_NOR_FLASH("ESMT F25L04", Mb004, &H8C, &H12) With {.ProgramMode = SPI_ProgramMode.AAI_Word}) 'REMS only
            FlashDB.Add(New SPI_NOR_FLASH("ESMT F25L04", Mb004, &H8C, &H2013) With {.ProgramMode = SPI_ProgramMode.AAI_Word})
            FlashDB.Add(New SPI_NOR_FLASH("ESMT F25L08", Mb008, &H8C, &H13) With {.ProgramMode = SPI_ProgramMode.AAI_Word}) 'REMS only
            FlashDB.Add(New SPI_NOR_FLASH("ESMT F25L08", Mb008, &H8C, &H2014) With {.ProgramMode = SPI_ProgramMode.AAI_Word})
            FlashDB.Add(New SPI_NOR_FLASH("ESMT F25L32QA", Mb032, &H8C, &H4116))
            FlashDB.Add(New SPI_NOR_FLASH("Sanyo LE25FU406B", Mb004, &H62, &H1E62))
            FlashDB.Add(New SPI_NOR_FLASH("Berg_Micro BG25Q32A", Mb032, &HE0, &H4016))

            'ST 25PX16 VZM6P


            'SUPPORTED EEPROM SPI DEVICES:
            FlashDB.Add(New SPI_NOR_FLASH("Atmel AT25128B", 16384, 64))
            FlashDB.Add(New SPI_NOR_FLASH("Atmel AT25256B", 32768, 64))
            FlashDB.Add(New SPI_NOR_FLASH("Atmel AT25512", 65536, 128))
            FlashDB.Add(New SPI_NOR_FLASH("ST M95010", 128, 16))
            FlashDB.Add(New SPI_NOR_FLASH("ST M95020", 256, 16))
            FlashDB.Add(New SPI_NOR_FLASH("ST M95040", 512, 16))
            FlashDB.Add(New SPI_NOR_FLASH("ST M95080", 1024, 32))
            FlashDB.Add(New SPI_NOR_FLASH("ST M95160", 2048, 32))
            FlashDB.Add(New SPI_NOR_FLASH("ST M95320", 4096, 32))
            FlashDB.Add(New SPI_NOR_FLASH("ST M95640", 8192, 32))
            FlashDB.Add(New SPI_NOR_FLASH("ST M95128", 16384, 64))
            FlashDB.Add(New SPI_NOR_FLASH("ST M95256", 32768, 64))
            FlashDB.Add(New SPI_NOR_FLASH("ST M95512", 65536, 128))
            FlashDB.Add(New SPI_NOR_FLASH("ST M95M01", 131072, 256))
            FlashDB.Add(New SPI_NOR_FLASH("ST M95M02", 262144, 256))
            FlashDB.Add(New SPI_NOR_FLASH("Atmel AT25010A", 128, 8))
            FlashDB.Add(New SPI_NOR_FLASH("Atmel AT25020A", 256, 8))
            FlashDB.Add(New SPI_NOR_FLASH("Atmel AT25040A", 512, 8))
            FlashDB.Add(New SPI_NOR_FLASH("Atmel AT25080", 1024, 32))
            FlashDB.Add(New SPI_NOR_FLASH("Atmel AT25160", 2048, 32))
            FlashDB.Add(New SPI_NOR_FLASH("Atmel AT25320", 4096, 32))
            FlashDB.Add(New SPI_NOR_FLASH("Atmel AT25640", 8192, 32))
            FlashDB.Add(New SPI_NOR_FLASH("Microchip 25AA160A", 2048, 16))
            FlashDB.Add(New SPI_NOR_FLASH("Microchip 25AA160B", 2048, 32))
        End Sub

        Private Sub SPINAND_Database()
            FlashDB.Add(New SPI_NAND_Flash("Micron MT29F1G01ABA", &H2C, &H14, Gb001, 2048, 128, Mb001, False)) '3.3v (chip-vault)
            FlashDB.Add(New SPI_NAND_Flash("Micron MT29F1G01ABB", &H2C, &H15, Gb001, 2048, 128, Mb001, False)) '1.8v
            FlashDB.Add(New SPI_NAND_Flash("Micron MT29F2G01AAA", &H2C, &H22, Gb002, 2048, 128, Mb001, True))
            FlashDB.Add(New SPI_NAND_Flash("Micron MT29F2G01ABA", &H2C, &H24, Gb002, 2048, 128, Mb001, True)) '3.3v
            FlashDB.Add(New SPI_NAND_Flash("Micron MT29F2G01ABB", &H2C, &H25, Gb002, 2048, 128, Mb001, True)) '1.8v
            FlashDB.Add(New SPI_NAND_Flash("Micron MT29F4G01ADA", &H2C, &H36, Gb004, 2048, 128, Mb001, True))
            FlashDB.Add(New SPI_NAND_Flash("Micron MT29F4G01AAA", &H2C, &H32, Gb004, 2048, 128, Mb001, True))

            FlashDB.Add(New SPI_NAND_Flash("GigaDevice GD5F1GQ4UB", &HC8, &HD1, Gb001, 2048, 128, Mb001, False)) '3.3v
            FlashDB.Add(New SPI_NAND_Flash("GigaDevice GD5F1GQ4RB", &HC8, &HC1, Gb001, 2048, 128, Mb001, False)) '1.8v
            FlashDB.Add(New SPI_NAND_Flash("GigaDevice GD5F1GQ4UC", &HC8, &HB148, Gb001, 2048, 128, Mb001, False)) '3.3v
            FlashDB.Add(New SPI_NAND_Flash("GigaDevice GD5F1GQ4RC", &HC8, &HA148, Gb001, 2048, 128, Mb001, False)) '1.8v
            FlashDB.Add(New SPI_NAND_Flash("GigaDevice GD5F2GQ4UB", &HC8, &HD2, Gb002, 2048, 128, Mb001, False)) '3.3v
            FlashDB.Add(New SPI_NAND_Flash("GigaDevice GD5F2GQ4RB", &HC8, &HC2, Gb002, 2048, 128, Mb001, False)) '1.8v
            FlashDB.Add(New SPI_NAND_Flash("GigaDevice GD5F2GQ4UC", &HC8, &HB248, Gb002, 2048, 128, Mb001, False)) '3.3v
            FlashDB.Add(New SPI_NAND_Flash("GigaDevice GD5F2GQ4RC", &HC8, &HA248, Gb002, 2048, 128, Mb001, False)) '1.8v
            FlashDB.Add(New SPI_NAND_Flash("GigaDevice GD5F4GQ4UA", &HC8, &HF4, Gb004, 2048, 64, Mb001, False)) '3.3v
            FlashDB.Add(New SPI_NAND_Flash("GigaDevice GD5F4GQ4UB", &HC8, &HD4, Gb004, 4096, 256, Mb002, False)) '3.3v
            FlashDB.Add(New SPI_NAND_Flash("GigaDevice GD5F4GQ4RB", &HC8, &HC4, Gb004, 4096, 256, Mb002, False)) '1.8v
            FlashDB.Add(New SPI_NAND_Flash("GigaDevice GD5F4GQ4UC", &HC8, &HB468, Gb004, 4096, 256, Mb002, False)) '3.3v
            FlashDB.Add(New SPI_NAND_Flash("GigaDevice GD5F4GQ4RC", &HC8, &HA468, Gb004, 4096, 256, Mb002, False)) '1.8v

            FlashDB.Add(New SPI_NAND_Flash("Winbond W25M02GV", &HEF, &HAB21, Gb002, 2048, 64, Mb001, False) With {.STACKED_DIES = 2}) '3.3v
            FlashDB.Add(New SPI_NAND_Flash("Winbond W25M02GW", &HEF, &HBB21, Gb002, 2048, 64, Mb001, False) With {.STACKED_DIES = 2}) '1.8v
            FlashDB.Add(New SPI_NAND_Flash("Winbond W25N01GV", &HEF, &HAA21, Gb001, 2048, 64, Mb001, False)) '3.3v
            FlashDB.Add(New SPI_NAND_Flash("Winbond W25N01GW", &HEF, &HBA21, Gb001, 2048, 64, Mb001, False)) '1.8v
            FlashDB.Add(New SPI_NAND_Flash("Winbond W25N512GV", &HEF, &HAA20, Mb512, 2048, 64, Mb001, False)) '3.3v
            FlashDB.Add(New SPI_NAND_Flash("Winbond W25N512GW", &HEF, &HBA20, Mb512, 2048, 64, Mb001, False)) '1.8v

            FlashDB.Add(New SPI_NAND_Flash("ISSI IS37/38SML01G1", &HC8, &H21, Gb001, 2048, 64, Mb001, False)) '3.3v

            ''TC58CVG2S0HxAIx
            'FlashDB.Add(New SPI_NAND_Flash("Toshiba TC58CVG0S3", &H98, &H0, Gb001, 4096, 128, Mb001, False))
            'FlashDB.Add(New SPI_NAND_Flash("Toshiba TC58CVG1S3", &H98, &H0, Gb002, 4096, 128, Mb001, False))
            'FlashDB.Add(New SPI_NAND_Flash("Toshiba TC58CVG2S0", &H98, &HCD, Gb004, 4096, 128, Mb001, False))
            ''Get Samples of TC58CVG2S0HQAIE, TC58CVG1S3HQAIE, TC58CVG0S3HQAIE

        End Sub

        Private Sub MFP_Database()
            'https://github.com/jhcloos/flashrom/blob/master/flashchips.h
            'Intel

            FlashDB.Add(New MFP_Flash("Intel A28F512", &H89, &HB8, Kb512, MFP_IF.X8_5V_12V, MFP_BLKLAYOUT.EntireDevice, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Intel 28F320J3", &H89, &H16, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer1, MFP_DELAY.SR1)) '32 byte buffers
            FlashDB.Add(New MFP_Flash("Intel 28F640J3", &H89, &H17, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer1, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F128J3", &H89, &H18, Mb128, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer1, MFP_DELAY.SR1)) 'TESTING
            FlashDB.Add(New MFP_Flash("Intel 28F256J3", &H89, &H1D, Mb256, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer1, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F320J5", &H89, &H14, Mb032, MFP_IF.X16_5V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer1, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F640J5", &H89, &H15, Mb064, MFP_IF.X16_5V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer1, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F800C3(T)", &H89, &H88C0, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F800C3(B)", &H89, &H88C1, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F160C3(T)", &H89, &H88C2, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F160C3(B)", &H89, &H88C3, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F320C3(T)", &H89, &H88C4, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F320C3(B)", &H89, &H88C5, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F640C3(T)", &H89, &H88CC, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F640C3(B)", &H89, &H88CD, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F008SA", &H89, &HA2, Mb008, MFP_IF.X8_5V_12V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F400B3(T)", &H89, &H8894, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F400B3(B)", &H89, &H8895, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F800B3(T)", &H89, &H8892, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F800B3(B)", &H89, &H8893, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F160B3(T)", &H89, &H8890, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F160B3(B)", &H89, &H8891, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F320B3(T)", &H89, &H8896, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F320B3(B)", &H89, &H8897, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F640B3(T)", &H89, &H8898, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Intel 28F640B3(B)", &H89, &H8899, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            'AMD
            FlashDB.Add(New MFP_Flash("AMD AM29F010B", &H1, &H20, Mb001, MFP_IF.X8_5V, MFP_BLKLAYOUT.Kb128_Uni, MFP_PROG.Standard, MFP_DELAY.uS) With {.ERASE_DELAY = 500, .RESET_ENABLED = False})
            FlashDB.Add(New MFP_Flash("AMD AM29F040B", &H20, &HE2, Mb004, MFP_IF.X8_5V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.Standard, MFP_DELAY.uS) With {.ERASE_DELAY = 500, .RESET_ENABLED = False}) 'Why is this not: 01 A4? (PLCC32 and DIP32 tested)
            FlashDB.Add(New MFP_Flash("AMD AM29F080B", &H1, &HD5, Mb008, MFP_IF.X8_5V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.Standard, MFP_DELAY.uS) With {.ERASE_DELAY = 500, .RESET_ENABLED = False}) 'TSOP40
            FlashDB.Add(New MFP_Flash("AMD AM29F016D", &H1, &HAD, Mb016, MFP_IF.X8_5V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.BypassMode, MFP_DELAY.uS)) 'TSOP40 CV
            FlashDB.Add(New MFP_Flash("AMD AM29F032B", &H4, &HD4, Mb032, MFP_IF.X8_5V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.Standard, MFP_DELAY.uS)) 'TSOP40 CV (wrong MFG ID?)
            FlashDB.Add(New MFP_Flash("AMD AM29LV200(T)", &H1, &H223B, Mb002, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29LV200(B)", &H1, &H22BF, Mb002, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29F200(T)", &H1, &H2251, Mb002, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29F200(B)", &H1, &H2257, Mb002, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29LV400(T)", &H1, &H22B9, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29LV400(B)", &H1, &H22BA, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29F400(T)", &H1, &H2223, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29F400(B)", &H1, &H22AB, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS)) '<-- please verify
            FlashDB.Add(New MFP_Flash("AMD AM29LV800(T)", &H1, &H22DA, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29LV800(B)", &H1, &H225B, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29F800(T)", &H1, &H22D6, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29F800(B)", &H1, &H2258, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29LV160B(T)", &H1, &H22C4, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.BypassMode, MFP_DELAY.uS)) 'Set HWDELAY to 25 (CV)
            FlashDB.Add(New MFP_Flash("AMD AM29LV160B(B)", &H1, &H2249, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS)) 'Set HWDELAY to 25
            FlashDB.Add(New MFP_Flash("AMD AM29DL322G(T)", &H1, &H2255, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29DL322G(B)", &H1, &H2256, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29DL323G(T)", &H1, &H2250, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29DL323G(B)", &H1, &H2253, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29DL324G(T)", &H1, &H225C, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29DL324G(B)", &H1, &H225F, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29LV320D(T)", &H1, &H22F6, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29LV320D(B)", &H1, &H22F9, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29LV320M(T)", &H1, &H2201, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("AMD AM29LV320M(B)", &H1, &H2200, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            'Winbond
            FlashDB.Add(New MFP_Flash("Winbond W49F020", &HDA, &H8C, Mb002, MFP_IF.X8_5V, MFP_BLKLAYOUT.EntireDevice, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Winbond W49F002U", &HDA, &HB, Mb002, MFP_IF.X8_5V, MFP_BLKLAYOUT.Mb002_NonUni, MFP_PROG.Standard, MFP_DELAY.uS) With {.PAGE_SIZE = 128, .HARDWARE_DELAY = 18})
            FlashDB.Add(New MFP_Flash("Winbond W29EE512", &HDA, &HC8, Kb512, MFP_IF.X8_5V, MFP_BLKLAYOUT.Kb256_Uni, MFP_PROG.PageMode, MFP_DELAY.DQ7) With {.PAGE_SIZE = 128, .ERASE_REQUIRED = False, .RESET_ENABLED = False})
            FlashDB.Add(New MFP_Flash("Winbond W29C010", &HDA, &HC1, Mb001, MFP_IF.X8_5V, MFP_BLKLAYOUT.Kb256_Uni, MFP_PROG.PageMode, MFP_DELAY.mS) With {.PAGE_SIZE = 128, .ERASE_REQUIRED = False})
            FlashDB.Add(New MFP_Flash("Winbond W29C020", &HDA, &H45, Mb002, MFP_IF.X8_5V, MFP_BLKLAYOUT.Kb256_Uni, MFP_PROG.PageMode, MFP_DELAY.mS) With {.PAGE_SIZE = 128, .ERASE_REQUIRED = False})
            FlashDB.Add(New MFP_Flash("Winbond W29C040", &HDA, &H46, Mb004, MFP_IF.X8_5V, MFP_BLKLAYOUT.Kb256_Uni, MFP_PROG.PageMode, MFP_DELAY.mS) With {.PAGE_SIZE = 256, .ERASE_REQUIRED = False})
            FlashDB.Add(New MFP_Flash("Winbond W29GL032CT", &H1, &H227E, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H1A01)) 'DQ.7 polling now added!
            FlashDB.Add(New MFP_Flash("Winbond W29GL032CB", &H1, &H227E, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H1A00))
            'SST
            FlashDB.Add(New MFP_Flash("SST 39VF401C/39LF401C", &HBF, &H2321, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("SST 39VF402C/39LF402C", &HBF, &H2322, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("SST 39SF512", &HBF, &HB4, Kb512, MFP_IF.X8_5V, MFP_BLKLAYOUT.Kb032_Uni, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("SST 39SF010", &HBF, &HB5, Mb001, MFP_IF.X8_5V, MFP_BLKLAYOUT.Kb032_Uni, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("SST 39SF020", &HBF, &HB6, Mb002, MFP_IF.X8_5V, MFP_BLKLAYOUT.Kb032_Uni, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("SST 39LF010", &HBF, &HD5, Mb001, MFP_IF.X8_5V, MFP_BLKLAYOUT.Kb032_Uni, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("SST 39LF020", &HBF, &HD6, Mb002, MFP_IF.X8_5V, MFP_BLKLAYOUT.Kb032_Uni, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("SST 39LF040", &HBF, &HD7, Mb004, MFP_IF.X8_5V, MFP_BLKLAYOUT.Kb032_Uni, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("SST 39VF800", &HBF, &H2781, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb032_Uni, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("SST 39VF160", &HBF, &H2782, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb032_Uni, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("SST 39VF1681", &HBF, &HC8, Mb016, MFP_IF.X8_3V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.Standard, MFP_DELAY.uS)) 'Verified 520
            FlashDB.Add(New MFP_Flash("SST 39VF1682", &HBF, &HC9, Mb016, MFP_IF.X8_3V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("SST 39VF1601", &HBF, &H234B, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb032_Uni, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("SST 39VF1602", &HBF, &H234A, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb032_Uni, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("SST 39VF1602C", &HBF, &H234E, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb032_Uni, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("SST 39VF3201", &HBF, &H235B, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb032_Uni, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("SST 39VF3202", &HBF, &H235A, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb032_Uni, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("SST 39VF6401", &HBF, &H236B, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb032_Uni, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("SST 39VF6402", &HBF, &H236A, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb032_Uni, MFP_PROG.Standard, MFP_DELAY.uS))
            'Atmel
            FlashDB.Add(New MFP_Flash("Atmel AT29C010A", &H1F, &HD5, Mb001, MFP_IF.X8_5V, MFP_BLKLAYOUT.Kb256_Uni, MFP_PROG.PageMode, MFP_DELAY.DQ7) With {.ERASE_REQUIRED = False, .PAGE_SIZE = 128, .RESET_ENABLED = False})
            FlashDB.Add(New MFP_Flash("Atmel AT49F512", &H1F, &H3, Kb512, MFP_IF.X8_5V, MFP_BLKLAYOUT.EntireDevice, MFP_PROG.Standard, MFP_DELAY.uS)) 'No SE, only BE
            FlashDB.Add(New MFP_Flash("Atmel AT49F010", &H1F, &H17, Mb001, MFP_IF.X8_5V, MFP_BLKLAYOUT.EntireDevice, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Atmel AT49F020", &H1F, &HB, Mb002, MFP_IF.X8_5V, MFP_BLKLAYOUT.EntireDevice, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Atmel AT49F040", &H1F, &H13, Mb004, MFP_IF.X8_5V, MFP_BLKLAYOUT.EntireDevice, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Atmel AT49F040T", &H1F, &H12, Mb004, MFP_IF.X8_5V, MFP_BLKLAYOUT.EntireDevice, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Atmel AT49BV/LV16X", &H1F, &HC0, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.Standard, MFP_DELAY.uS)) 'Supports Single Pulse Byte/ Word Program
            FlashDB.Add(New MFP_Flash("Atmel AT49BV/LV16XT", &H1F, &HC2, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            'MXIC
            FlashDB.Add(New MFP_Flash("MXIC MX29L3211", &HC2, &HF9, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.PageMode, MFP_DELAY.SR2) With {.PAGE_SIZE = 64}) 'Actualy supports up to 256 bytes
            FlashDB.Add(New MFP_Flash("MXIC MX29LV040", &HC2, &H4F, Mb004, MFP_IF.X8_3V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("MXIC MX29LV400T", &HC2, &H22B9, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("MXIC MX29LV400B", &HC2, &H22BA, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("MXIC MX29LV800T", &HC2, &H22DA, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("MXIC MX29LV800B", &HC2, &H225B, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("MXIC MX29LV160DT", &HC2, &H22C4, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS) With {.HARDWARE_DELAY = 6}) 'Required! SO-44 in CV
            FlashDB.Add(New MFP_Flash("MXIC MX29LV160DB", &HC2, &H2249, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS) With {.HARDWARE_DELAY = 6})
            FlashDB.Add(New MFP_Flash("MXIC MX29LV320T", &HC2, &H22A7, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS) With {.HARDWARE_DELAY = 0})
            FlashDB.Add(New MFP_Flash("MXIC MX29LV320B", &HC2, &H22A8, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS) With {.HARDWARE_DELAY = 0})
            FlashDB.Add(New MFP_Flash("MXIC MX29LV640ET", &HC2, &H22C9, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS) With {.HARDWARE_DELAY = 0})
            FlashDB.Add(New MFP_Flash("MXIC MX29LV640EB", &HC2, &H22CB, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS) With {.HARDWARE_DELAY = 0})
            FlashDB.Add(New MFP_Flash("MXIC MX29GL128F", &HC2, &H227E, Mb128, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Standard, MFP_DELAY.uS, &H2101) With {.HARDWARE_DELAY = 0})
            'Cypress / Spansion
            'http://www.cypress.com/file/177976/download   S29GLxxxS
            'http://www.cypress.com/file/219926/download   S29GLxxxP
            FlashDB.Add(New MFP_Flash("Cypress S29AL004D(B)", &HC2, &H22BA, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Cypress S29AL004D(T)", &HC2, &H22B9, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Cypress S29AL008J(B)", &HC2, &H225B, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Cypress S29AL008J(T)", &HC2, &H22DA, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Cypress S29AL016D(B)", &HC2, &H2249, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Cypress S29AL016D(T)", &HC2, &H22C4, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Cypress S29AL032D", &HC2, &HA3, Mb032, MFP_IF.X8_3V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.BypassMode, MFP_DELAY.uS)) 'Available in TSOP-40
            FlashDB.Add(New MFP_Flash("Cypress S29AL032D(B)", &HC2, &H22F9, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Cypress S29AL032D(T)", &HC2, &H22F6, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Cypress S29GL128", &H1, &H227E, Mb128, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.uS, &H2100) With {.PAGE_SIZE = 64}) 'We need to test this device
            FlashDB.Add(New MFP_Flash("Cypress S29GL256", &H1, &H227E, Mb256, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.uS, &H2200) With {.PAGE_SIZE = 64})
            FlashDB.Add(New MFP_Flash("Cypress S29GL512", &H1, &H227E, Mb512, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.uS, &H2300) With {.PAGE_SIZE = 64})
            FlashDB.Add(New MFP_Flash("Cypress S29GL01G", &H1, &H227E, Gb001, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.uS, &H2800) With {.PAGE_SIZE = 64})
            FlashDB.Add(New MFP_Flash("Cypress S29JL064J", &H1, &H227E, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Dual, MFP_PROG.Standard, MFP_DELAY.SR1, &H201)) 'Dual-boot
            FlashDB.Add(New MFP_Flash("Cypress S29GL032M", &H1, &H227E, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.Standard, MFP_DELAY.SR1, &H1C00)) 'Model R0
            FlashDB.Add(New MFP_Flash("Cypress S29GL032M", &H1, &H227E, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.Standard, MFP_DELAY.SR1, &H1D00))
            FlashDB.Add(New MFP_Flash("Cypress S29GL032M(B)", &H1, &H227E, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.Standard, MFP_DELAY.SR1, &H1A00)) 'Bottom-Boot
            FlashDB.Add(New MFP_Flash("Cypress S29GL032M(T)", &H1, &H227E, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.Standard, MFP_DELAY.SR1, &H1A01)) 'Top-Boot
            FlashDB.Add(New MFP_Flash("Cypress S29JL032J(B)", &H1, &H225F, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.BypassMode, MFP_DELAY.DQ7)) 'Bottom-Boot
            FlashDB.Add(New MFP_Flash("Cypress S29JL032J(T)", &H1, &H225C, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.BypassMode, MFP_DELAY.DQ7)) 'Top-Boot
            FlashDB.Add(New MFP_Flash("Cypress S29JL032J(B)", &H1, &H2253, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.BypassMode, MFP_DELAY.DQ7)) 'Bottom-Boot
            FlashDB.Add(New MFP_Flash("Cypress S29JL032J(T)", &H1, &H2250, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.BypassMode, MFP_DELAY.DQ7)) 'Top-Boot
            FlashDB.Add(New MFP_Flash("Cypress S29JL032J(B)", &H1, &H2256, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.BypassMode, MFP_DELAY.DQ7)) 'Bottom-Boot
            FlashDB.Add(New MFP_Flash("Cypress S29JL032J(T)", &H1, &H2255, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.BypassMode, MFP_DELAY.DQ7)) 'Top-Boot
            FlashDB.Add(New MFP_Flash("Cypress S29GL064M", &H1, &H227E, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.Standard, MFP_DELAY.SR1, &H1300)) 'Model R0
            FlashDB.Add(New MFP_Flash("Cypress S29GL064M", &H1, &H227E, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.Standard, MFP_DELAY.SR1, &HC01))
            FlashDB.Add(New MFP_Flash("Cypress S29GL064M(T)", &H1, &H227E, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.Standard, MFP_DELAY.SR1, &H1001)) 'Top-Boot
            FlashDB.Add(New MFP_Flash("Cypress S29GL064M(B)", &H1, &H227E, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.Standard, MFP_DELAY.SR1, &H1000)) 'Bottom-Boot
            FlashDB.Add(New MFP_Flash("Cypress S29GL064M", &H1, &H227E, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.Standard, MFP_DELAY.SR1, &H1301))
            FlashDB.Add(New MFP_Flash("Cypress S29GL128M", &H1, &H227E, Mb128, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Standard, MFP_DELAY.SR1, &H1200))
            FlashDB.Add(New MFP_Flash("Cypress S29GL256M", &H1, &H227E, Mb256, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Standard, MFP_DELAY.SR1, &H1201))
            FlashDB.Add(New MFP_Flash("Cypress S29GL128N", &H1, &H227E, Mb128, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H2101) With {.PAGE_SIZE = 32})
            FlashDB.Add(New MFP_Flash("Cypress S29GL256N", &H1, &H227E, Mb256, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H2201) With {.PAGE_SIZE = 32}) '(CHIP-VAULT)
            FlashDB.Add(New MFP_Flash("Cypress S29GL512N", &H1, &H227E, Mb512, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H2301) With {.PAGE_SIZE = 32})
            FlashDB.Add(New MFP_Flash("Cypress S29GL128P", &H1, &H227E, Mb128, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H2101) With {.PAGE_SIZE = 64}) '(CHIP-VAULT)
            FlashDB.Add(New MFP_Flash("Cypress S29GL256P", &H1, &H227E, Mb256, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H2201) With {.PAGE_SIZE = 64})
            FlashDB.Add(New MFP_Flash("Cypress S29GL512P", &H1, &H227E, Mb512, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H2301) With {.PAGE_SIZE = 64})
            FlashDB.Add(New MFP_Flash("Cypress S29GL01GP", &H1, &H227E, Gb001, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H2801) With {.PAGE_SIZE = 64})
            FlashDB.Add(New MFP_Flash("Cypress S29GL128S", &H1, &H227E, Mb128, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H2201) With {.PAGE_SIZE = 512})
            FlashDB.Add(New MFP_Flash("Cypress S29GL256S", &H1, &H227E, Mb256, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H2201) With {.PAGE_SIZE = 512})
            FlashDB.Add(New MFP_Flash("Cypress S29GL512S", &H1, &H227E, Mb512, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H2201) With {.PAGE_SIZE = 512})
            FlashDB.Add(New MFP_Flash("Cypress S29GL01GS", &H1, &H227E, Gb001, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H2201) With {.PAGE_SIZE = 512})
            FlashDB.Add(New MFP_Flash("Cypress S29GL512T", &H1, &H227E, Mb512, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.SR1, &H2301) With {.PAGE_SIZE = 512})
            FlashDB.Add(New MFP_Flash("Cypress S29GL01GT", &H1, &H227E, Gb001, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.SR1, &H2801) With {.PAGE_SIZE = 512}) '(CHIP-VAULT)
            FlashDB.Add(New MFP_Flash("Cypress S70GL02G", &H1, &H227E, Gb002, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.SR1, &H4801) With {.PAGE_SIZE = 512})
            'ST Microelectronics (now numonyx)
            FlashDB.Add(New MFP_Flash("ST M29F200T", &H20, &HD3, Mb002, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS)) 'Available in TSOP48/SO44
            FlashDB.Add(New MFP_Flash("ST M29F200B", &H20, &HD4, Mb002, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS)) 'Available in TSOP48/SO44
            FlashDB.Add(New MFP_Flash("ST M29F400T", &H20, &HD5, Mb004, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS)) 'Available in TSOP48/SO44
            FlashDB.Add(New MFP_Flash("ST M29F400B", &H20, &HD6, Mb004, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS)) 'Available in TSOP48/SO44
            FlashDB.Add(New MFP_Flash("ST M29F800T", &H20, &HEC, Mb008, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS)) 'Available in TSOP48/SO44
            FlashDB.Add(New MFP_Flash("ST M29F800B", &H20, &H58, Mb008, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS)) 'Available in TSOP48/SO44
            FlashDB.Add(New MFP_Flash("ST M29W800AT", &H20, &HD7, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("ST M29W800AB", &H20, &H5B, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("ST M29W800DT", &H20, &H22D7, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("ST M29W800DB", &H20, &H225B, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("ST M29W160ET", &H20, &H22C4, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("ST M29W160EB", &H20, &H2249, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS)) 'CV
            FlashDB.Add(New MFP_Flash("ST M29D323DT", &H20, &H225E, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("ST M29D323DB", &H20, &H225F, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("ST M29W320DT", &H20, &H22CA, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("ST M29W320DB", &H20, &H22CB, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("ST M29W320ET", &H20, &H2256, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("ST M29W320EB", &H20, &H2257, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            'ST M28
            FlashDB.Add(New MFP_Flash("ST M28W160CT", &H20, &H88CE, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("ST M28W160CB", &H20, &H88CF, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("ST M28W320FCT", &H20, &H88BA, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("ST M28W320FCB", &H20, &H88BB, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("ST M28W320BT", &H20, &H88BC, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("ST M28W320BB", &H20, &H88BD, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("ST M28W640ECT", &H20, &H8848, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("ST M28W640ECB", &H20, &H8849, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("ST M58LW064D", &H20, &H17, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            'Micron
            FlashDB.Add(New MFP_Flash("Micron M29F200FT", &HC2, &H2251, Mb002, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Micron M29F200FB", &HC2, &H2257, Mb002, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Micron M29F400FT", &HC2, &H2223, Mb004, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Micron M29F400FB", &HC2, &H22AB, Mb004, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Micron M29F800FT", &H1, &H22D6, Mb008, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Micron M29F800FB", &H1, &H2258, Mb008, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Micron M29F160FT", &H1, &H22D2, Mb016, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Micron M29F160FB", &H1, &H22D8, Mb016, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Micron M29W160ET", &H20, &H22C4, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Micron M29W160EB", &H20, &H2249, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Micron M29W320DT", &H20, &H22CA, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Micron M29W320DB", &H20, &H22CB, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Micron M29W640GH", &H20, &H227E, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.BypassMode, MFP_DELAY.uS, &HC01))
            FlashDB.Add(New MFP_Flash("Micron M29W640GL", &H20, &H227E, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.BypassMode, MFP_DELAY.uS, &HC00))
            FlashDB.Add(New MFP_Flash("Micron M29W640GT", &H20, &H227E, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.BypassMode, MFP_DELAY.uS, &H1001))
            FlashDB.Add(New MFP_Flash("Micron M29W640GB", &H20, &H227E, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS, &H1000))
            FlashDB.Add(New MFP_Flash("Micron M29W128GH", &H20, &H227E, Mb128, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H2201)) '(CHIP-VAULT)
            FlashDB.Add(New MFP_Flash("Micron M29W128GL", &H20, &H227E, Mb128, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H2200))
            FlashDB.Add(New MFP_Flash("Micron M29W256GH", &H20, &H227E, Mb256, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H2201))
            FlashDB.Add(New MFP_Flash("Micron M29W256GL", &H20, &H227E, Mb256, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H2200))
            FlashDB.Add(New MFP_Flash("Micron M29W512G", &H20, &H227E, Mb512, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.Buffer2, MFP_DELAY.DQ7, &H2301))
            'Sharp
            FlashDB.Add(New MFP_Flash("Sharp LHF00L15", &HB0, &HA1, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb032_NonUni, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Sharp LH28F160S3", &HB0, &HD0, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Sharp LH28F320S3", &HB0, &HD4, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Sharp LH28F160BJE", &HB0, &HE9, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New MFP_Flash("Sharp LH28F320BJE", &HB0, &HE3, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.IntelSharp, MFP_DELAY.SR1))
            'Toshiba
            FlashDB.Add(New MFP_Flash("Toshiba TC58FVT800", &H98, &H4F, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Toshiba TC58FVB800", &H98, &HCE, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Toshiba TC58FVT160", &H98, &HC2, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Toshiba TC58FVB160", &H98, &H43, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Toshiba TC58FVT321", &H98, &H9C, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Toshiba TC58FVB321", &H98, &H9A, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            'Samsung
            FlashDB.Add(New MFP_Flash("Samsung K8P1615UQB", &HEC, &H257E, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb016_Samsung, MFP_PROG.BypassMode, MFP_DELAY.uS, &H1))
            FlashDB.Add(New MFP_Flash("Samsung K8D1716UT", &HEC, &H2275, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Samsung K8D1716UB", &HEC, &H2277, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Samsung K8D3216UT", &HEC, &H22A0, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Samsung K8D3216UB", &HEC, &H22A2, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Samsung K8P3215UQB", &HEC, &H257E, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb032_Samsung, MFP_PROG.BypassMode, MFP_DELAY.uS, &H301))
            FlashDB.Add(New MFP_Flash("Samsung K8D6316UT", &HEC, &H22E0, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Samsung K8D6316UB", &HEC, &H22E2, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Samsung K8P6415UQB", &HEC, &H257E, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb064_Samsung, MFP_PROG.BypassMode, MFP_DELAY.uS, &H601))
            FlashDB.Add(New MFP_Flash("Samsung K8P2716UZC", &HEC, &H227E, Mb128, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.BypassMode, MFP_DELAY.uS, &H6660))
            FlashDB.Add(New MFP_Flash("Samsung K8Q2815UQB", &HEC, &H257E, Mb128, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb128_Samsung, MFP_PROG.BypassMode, MFP_DELAY.uS, &H601)) 'TSOP56 Type-A
            FlashDB.Add(New MFP_Flash("Samsung K8P5516UZB", &HEC, &H227E, Mb256, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb001_Uni, MFP_PROG.BypassMode, MFP_DELAY.uS, &H6460))
            FlashDB.Add(New MFP_Flash("Samsung K8P5615UQA", &HEC, &H227E, Mb256, MFP_IF.X16_3V, MFP_BLKLAYOUT.Mb256_Samsung, MFP_PROG.BypassMode, MFP_DELAY.uS, &H6360))
            'Hynix
            FlashDB.Add(New MFP_Flash("Hynix HY29F400T", &HAD, &H2223, Mb004, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Hynix HY29F400B", &HAD, &H22AB, Mb004, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Hynix HY29F800T", &HAD, &H22D6, Mb008, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Hynix HY29F800B", &HAD, &H2258, Mb008, MFP_IF.X16_5V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Hynix HY29LV400T", &HAD, &H22B9, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Hynix HY29LV400B", &HAD, &H22BA, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Hynix HY29LV800T", &HAD, &H22DA, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Hynix HY29LV800B", &HAD, &H225B, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Hynix HY29LV160T", &HAD, &H22C4, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Hynix HY29LV160B", &HAD, &H2249, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Hynix HY29LV320T", &HAD, &H227E, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Hynix HY29LV320B", &HAD, &H227D, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            'Fujitsu
            FlashDB.Add(New MFP_Flash("Fujitsu MBM29LV200TC", &H4, &H223B, Mb002, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Fujitsu MBM29LV200BC", &H4, &H22BF, Mb002, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Fujitsu MBM29LV400TC", &H4, &H22B9, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Fujitsu MBM29LV400BC", &H4, &H22BA, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Fujitsu MBM29LV800TA", &H4, &H22DA, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Fujitsu MBM29LV800BA", &H4, &H225B, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Fujitsu MBM29LV160T", &H4, &H22C4, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Fujitsu MBM29LV160B", &H4, &H2249, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New MFP_Flash("Fujitsu MBM29LV320TE", &H4, &H22F6, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.Standard, MFP_DELAY.uS)) 'Supports FAST programming (ADR=0xA0,PA=PD)
            FlashDB.Add(New MFP_Flash("Fujitsu MBM29LV320BE", &H4, &H22F9, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.Standard, MFP_DELAY.uS)) 'Supports FAST programming (ADR=0xA0,PA=PD)
            FlashDB.Add(New MFP_Flash("Fujitsu MBM29DL32XTD", &H4, &H2259, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.Standard, MFP_DELAY.uS)) 'Supports FAST programming (ADR=0xA0,PA=PD)
            FlashDB.Add(New MFP_Flash("Fujitsu MBM29DL32XBD", &H4, &H225A, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.Standard, MFP_DELAY.uS)) 'Supports FAST programming (ADR=0xA0,PA=PD)
            'EON (MFG is 7F 1C)
            FlashDB.Add(New MFP_Flash("EON EN29LV400AT", &H7F, &H22B9, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New MFP_Flash("EON EN29LV400AB", &H7F, &H22BA, Mb004, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New MFP_Flash("EON EN29LV800AT", &H7F, &H22DA, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New MFP_Flash("EON EN29LV800AB", &H7F, &H225B, Mb008, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New MFP_Flash("EON EN29LV160AT", &H7F, &H22C4, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Top, MFP_PROG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New MFP_Flash("EON EN29LV160AB", &H7F, &H2249, Mb016, MFP_IF.X16_3V, MFP_BLKLAYOUT.Four_Btm, MFP_PROG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New MFP_Flash("EON EN29LV320AT", &H7F, &H22F6, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Top, MFP_PROG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New MFP_Flash("EON EN29LV320AB", &H7F, &H22F9, Mb032, MFP_IF.X16_3V, MFP_BLKLAYOUT.Two_Btm, MFP_PROG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New MFP_Flash("EON EN29LV640", &H7F, &H227E, Mb064, MFP_IF.X16_3V, MFP_BLKLAYOUT.Kb512_Uni, MFP_PROG.BypassMode, MFP_DELAY.DQ7))
        End Sub

        Private Sub NAND_Database()
            'Good ID list at: http://www.usbdev.ru/databases/flashlist/flcbm93e98s98p98e/
            'And : http://www.linux-mtd.infradead.org/nand-data/nanddata.html
            'And: http://aitendo2.sakura.ne.jp/aitendo_data/product_img2/product_img/aitendo-kit/USB-MEM/MW8209/Flash_suport_091120.pdf

            'Micron SLC 8x NAND devices
            FlashDB.Add(New SLC_NAND_Flash("Micron NAND128W3A", &H20, &H732073, Mb128, 512, 16, Kb128, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron NAND256R3A", &H20, &H352035, Mb256, 512, 16, Kb128, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron NAND256W3A", &H20, &H752075, Mb256, 512, 16, Kb128, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron NAND512R3A", &H20, &H362036, Mb512, 512, 16, Kb128, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron NAND512W3A", &H20, &H762076, Mb512, 512, 16, Kb128, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron NAND01GR3A", &H20, &H392039, Gb001, 512, 16, Kb128, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron NAND01GW3A", &H20, &H792079, Gb001, 512, 16, Kb128, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron NAND04GW3B", &H20, &HDC1095, Gb004, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron NAND04GW3B", &H20, &HDC1095, Gb004, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron NAND04GW3B", &H20, &HDC1095, Gb004, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron NAND04GW3B", &H20, &HDC1095, Gb004, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron NAND04GW3B", &H20, &HDC1095, Gb004, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron NAND04GW3B", &H20, &HDC1095, Gb004, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron MT29F2G08AAB", &H2C, &HDA0015, Gb002, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron MT29F1G08ABAEA", &H2C, &HF1809504UI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron MT29F1G08ABBEA", &H2C, &HA1801504UI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron MT29F1G08ABADAWP", &H2C, &HF1809502UI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V)) 'Updated ID
            FlashDB.Add(New SLC_NAND_Flash("Micron MT29F2G08ABBFA", &H2C, &HAA901504UI, Gb002, 2048, 224, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron MT29F2G08ABAFA", &H2C, &HDA909504UI, Gb002, 2048, 224, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron MT29F2G08ABBEA", &H2C, &HAA901560UI, Gb002, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron MT29F2G08ABAEA", &H2C, &HDA909506UI, Gb002, 2048, 64, Mb001, ND_IF.X8_3V)) 'Fixed
            FlashDB.Add(New SLC_NAND_Flash("Micron MT29F4G08BAB", &H2C, &HDC0015, Gb004, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron MT29F4G08AAA", &H2C, &HDC909554UI, Gb004, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron MT29F4G08ABA", &H2C, &HDC909556UI, Gb004, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron MT29F4G08ABADAWP", &H2C, &H90A0B0CUI, Gb002, 2048, 64, Mb001, ND_IF.X8_3V)) 'Fixed/CV
            FlashDB.Add(New SLC_NAND_Flash("Micron MT29F4G08ABAHC", &H2C, &HDC90A654UI, Gb004, 2048, 64, Mb001, ND_IF.X8_3V)) 'Just added
            FlashDB.Add(New SLC_NAND_Flash("Micron MT29F8G08BAA", &H2C, &HD3D19558UI, Gb008, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Micron MT29F8G08ABABA", &H2C, &H38002685UI, Gb008, 4096, 224, Mb004, ND_IF.X8_3V))
            'Toshiba SLC 8x NAND devices
            FlashDB.Add(New SLC_NAND_Flash("Toshiba TC58DVM92A5TA10", &H98, &H76A5C029UI, Mb512, 512, 16, Kb128, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Toshiba TC58BVG0S3HTA00", &H98, &HF08014F2UI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Toshiba TC58NVG0S3HTA00", &H98, &HF1801572UI, Gb001, 2048, 128, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Toshiba TC58NVG0S3HTAI0", &H98, &HF1801572UI, Gb001, 2048, 128, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Toshiba TC58NVG1S3HTA00", &H98, &HDA901576UI, Gb002, 2048, 128, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Toshiba TC58NVG1S3HTAI0", &H98, &HDA901576UI, Gb002, 2048, 128, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Toshiba TC58NVG2S0HTA00", &H98, &HDC902676UI, Gb004, 4096, 256, Mb001, ND_IF.X8_3V)) 'CHECK
            FlashDB.Add(New SLC_NAND_Flash("Toshiba TC58NVG2S0HTAI0", &H98, &HDC902676UI, Gb004, 4096, 256, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Toshiba TC58BVG2S0HTAI0", &H98, &HDC9026F6UI, Gb004, 4096, 128, Mb002, ND_IF.X8_3V)) 'CV (ECC INTERNAL)
            FlashDB.Add(New SLC_NAND_Flash("Toshiba TH58NVG3S0HTA00", &H98, &HD3912676UI, Gb008, 4096, 256, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Toshiba TH58NVG3S0HTAI0", &H98, &HD3912676UI, Gb008, 2048, 128, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Toshiba TC58NVG3S0FTA00", &H98, &HD3902676UI, Gb008, 4096, 232, Mb002, ND_IF.X8_3V))
            'Winbond SLC 8x NAND devices
            FlashDB.Add(New SLC_NAND_Flash("Winbond W29N01GV", &HEF, &HF1809500UI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Winbond W29N02GV", &HEF, &HDA909504UI, Gb002, 2048, 64, Mb001, ND_IF.X8_3V))
            'Macronix SLC 8x NAND devices
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX30LF1208AA", &HC2, &HF0801D, Mb512, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX30LF1GE8AB", &HC2, &HF1809582UI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX30UF1G18AC", &HC2, &HA1801502UI, Gb001, 2048, 64, Mb001, ND_IF.X8_1V8))
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX30LF1G18AC", &HC2, &HF1809502UI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX30LF1G08AA", &HC2, &HF1801D, Gb001, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX30LF2G18AC", &HC2, &HDA909506UI, Gb002, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX30UF2G18AC", &HC2, &HAA901506UI, Gb002, 2048, 64, Mb001, ND_IF.X8_1V8))
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX30LF2G28AB", &HC2, &HDA909507UI, Gb002, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX30LF2GE8AB", &HC2, &HDA909586UI, Gb002, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX30UF2G18AB", &HC2, &HBA905506UI, Gb002, 2048, 64, Mb001, ND_IF.X8_1V8))
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX30UF2G28AB", &HC2, &HAA901507UI, Gb002, 2048, 112, Mb001, ND_IF.X8_1V8))
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX30LF4G18AC", &HC2, &HDC909556UI, Gb004, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX30UF4G18AB", &HC2, &HAC901556UI, Gb004, 2048, 64, Mb001, ND_IF.X8_1V8))
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX30LF4G28AB", &HC2, &HDC909507UI, Gb004, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX30LF4GE8AB", &HC2, &HDC9095D6UI, Gb004, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX30UF4G28AB", &HC2, &HAC901557UI, Gb004, 2048, 112, Mb001, ND_IF.X8_1V8))
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX60LF8G18AC", &HC2, &HD3D1955AUI, Gb008, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("MXIC MX60LF8G28AB", &HC2, &HD3D1955BUI, Gb008, 2048, 64, Mb001, ND_IF.X8_3V))
            'Samsung SLC x8 NAND devices
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9K1606UOM", &HEC, &H79A5C0ECUI, Gb001, 512, 16, Kb128, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9F5608U0D", &HEC, &H75A5BDECUI, Mb256, 512, 16, Kb128, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9F1208U0C", &HEC, &H765A3F74UI, Mb512, 512, 16, Kb128, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9F1G08U0A", &HEC, &HF1801540UI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9F1G08U0D", &HEC, &HF1001540UI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9F1G08U0B", &HEC, &HF1009540UI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9F1G08X0", &HEC, &HF1009540UI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V)) 'K9F1G08U0C K9F1G08B0C K9F1G08U0B
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9F1G08U0E", &HEC, &HF1009541UI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V)) 'Added in 434
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9F2G08X0", &HEC, &HDA101544UI, Gb002, 2048, 64, Mb001, ND_IF.X8_3V)) 'K9F2G08B0B K9F2G08U0B K9F2G08U0A K9F2G08U0C
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9F2G08U0C", &HEC, &HDA109544UI, Gb002, 2048, 64, Mb001, ND_IF.X8_3V)) 'CV
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9F2G08U0M", &HEC, &HDA8015UI, Gb004, 2048, 64, Mb001, ND_IF.X8_3V)) 'K9K4G08U1M = 2X DIE
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9G8G08U0B", &HEC, &HD314A564UI, Gb001, 2048, 64, Mb002, ND_IF.X8_3V)) '2-bit/cell
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9W8G08U1M", &HEC, &HDCC11554UI, Gb004, 2048, 64, Mb001, ND_IF.X8_3V)) 'CV
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9F4G08U0B", &HEC, &HDC109554UI, Gb004, 2048, 64, Mb001, ND_IF.X8_3V)) 'CV
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9GAG08U0E", &HEC, &HD5847250UI, Gb016, 8192, 436, Mb008, ND_IF.X8_3V)) 'MLC 2-bit (CV)
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9GAG08U0M", &HEC, &HD514B674UI, Gb016, 4096, 128, Mb004, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9K8G08U0A", &HEC, &HD3519558UI, Gb008, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9WAG08U1A", &HEC, &HD3519558UI, Gb016, 2048, 64, Mb001, ND_IF.X8_3V) With {.STACKED_DIES = 2}) 'Dual die (CE1#/CE2#)
            FlashDB.Add(New SLC_NAND_Flash("Samsung K9NBG08U5A", &HEC, &HD3519558UI, Gb032, 2048, 64, Mb001, ND_IF.X8_3V) With {.STACKED_DIES = 4}) 'Quad die (CE1#/CE2#/CE3#/CE4#)
            'Hynix SLC x8 devices
            FlashDB.Add(New SLC_NAND_Flash("Hynix HY27US08281A", &HAD, &H73AD73ADUI, Mb128, 512, 16, Kb128, ND_IF.X8_X16_3V))
            FlashDB.Add(New SLC_NAND_Flash("Hynix HY27US08121B", &HAD, &H76AD76ADUI, Mb512, 512, 16, Kb128, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Hynix HY27US08561A", &HAD, &H75AD75ADUI, Mb256, 512, 16, Kb128, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Hynix HY27SS08561A", &HAD, &H35AD35ADUI, Mb256, 512, 16, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Hynix HY27US0812(1/2)B", &HAD, &H76UI, Mb512, 512, 16, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Hynix H27U1G8F2B", &HAD, &HF1001D, Gb001, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Hynix H27U1G8F2CTR", &HAD, &HF1801DADUI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Hynix HY27UF081G2M", &HAD, &HF10015ADUI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V)) '0xADF1XX15
            FlashDB.Add(New SLC_NAND_Flash("Hynix HY27US081G1M", &HAD, &H79A500UI, Gb001, 512, 16, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Hynix HY27SF081G2M", &HAD, &HA10015UI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V)) 'ADA1XX15
            FlashDB.Add(New SLC_NAND_Flash("Hynix HY27UF082G2B", &HAD, &HDA109544UI, Gb002, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Hynix HY27UF082G2A", &HAD, &HDA801D00UI, Gb002, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Hynix H27U2G8F2C", &HAD, &HDA909546UI, Gb002, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Hynix H27U2G6F2C", &HAD, &HCA90D544UI, Gb002, 2048, 64, Mb001, ND_IF.X16_3V))
            FlashDB.Add(New SLC_NAND_Flash("Hynix H27S2G8F2C", &HAD, &HAA901544UI, Gb002, 2048, 64, Mb001, ND_IF.X8_1V8))
            FlashDB.Add(New SLC_NAND_Flash("Hynix H27S2G6F2C", &HAD, &HBA905544UI, Gb002, 2048, 64, Mb001, ND_IF.X16_1V8))
            'Spansion SLC 34 series
            FlashDB.Add(New SLC_NAND_Flash("Cypress S34ML01G1", &H1, &HF1001DUI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Cypress S34ML02G1", &H1, &HDA9095UI, Gb002, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Cypress S34ML04G1", &H1, &HDC9095UI, Gb004, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Cypress S34ML01G2", &H1, &HF1801DUI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Cypress S34ML02G2", &H1, &HD89097UI, Gb002, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Cypress S34ML04G2", &H1, &HDC9095UI, Gb004, 2048, 64, Mb001, ND_IF.X8_3V))
            FlashDB.Add(New SLC_NAND_Flash("Cypress S34MS01G200", &H1, &HA18015UI, Gb004, 2048, 64, Mb001, ND_IF.X8_1V8))
            FlashDB.Add(New SLC_NAND_Flash("Cypress S34MS02G200", &H1, &HAA901546UI, Gb004, 2048, 64, Mb001, ND_IF.X8_1V8))
            FlashDB.Add(New SLC_NAND_Flash("Cypress S34MS04G200", &H1, &HAC901556UI, Gb004, 2048, 64, Mb001, ND_IF.X8_1V8))
            FlashDB.Add(New SLC_NAND_Flash("Cypress S34MS01G204", &H1, &HB18055UI, Gb004, 2048, 64, Mb001, ND_IF.X16_1V8))
            FlashDB.Add(New SLC_NAND_Flash("Cypress S34MS02G204", &H1, &HBA905546UI, Gb004, 2048, 64, Mb001, ND_IF.X16_1V8))
            FlashDB.Add(New SLC_NAND_Flash("Cypress S34MS04G204", &H1, &HBC905556UI, Gb004, 2048, 64, Mb001, ND_IF.X16_1V8))

            'Others
            FlashDB.Add(New SLC_NAND_Flash("Zentel A5U1GA31ATS", &H92, &HF1809540UI, Gb001, 2048, 64, Mb001, ND_IF.X8_3V))

            'MLC and 8LC devices:
            'Database.Add(New MPFlash("SanDisk SDTNPNAHEM-008G", &H98, &H809272UI, MB8Gb * 8, 8192, 1024, MB032))
            'FlashDB.Add(New NAND_Flash("Toshiba TC58NVG3D4CTGI0", &H98, &H8095D6, Gb008, 4096, 256, Mb001)) 'We have this one
            'Samsung K9WG08U1M and this one

            '0x98D385A5
            '2kB / 256kB

            'Dim test As New NAND_Flash("Toshiba TC58NVG3D4CTGI0", &H98, &H8095D6, Gb008, 2048, 64, Mb002)
            'FlashDB.Add(test)

        End Sub

        Private Sub OTP_Database()
            FlashDB.Add(New OTP_EPROM("ST M27C1024", &H20, &H8C, Mb001, MFP_IF.X16_5V_12V))
            FlashDB.Add(New OTP_EPROM("ST M27C256B", &H20, &H8D, Kb256, MFP_IF.X8_5V_12V))
            FlashDB.Add(New OTP_EPROM("ST M27C512", &H20, &H3D, Kb512, MFP_IF.X8_5V_12V))
            FlashDB.Add(New OTP_EPROM("ST M27C1001", &H20, &H5, Mb001, MFP_IF.X8_5V_12V))
            FlashDB.Add(New OTP_EPROM("ST M27C2001", &H20, &H61, Mb002, MFP_IF.X8_5V_12V))
            FlashDB.Add(New OTP_EPROM("ST M27C4001", &H20, &H41, Mb004, MFP_IF.X8_5V_12V))
            FlashDB.Add(New OTP_EPROM("ST M27C801", &H20, &H42, Mb008, MFP_IF.X8_5V_12V))
            FlashDB.Add(New OTP_EPROM("ST M27C160", &H20, &HB1, Mb016, MFP_IF.X16_5V_12V)) 'DIP42,SO44,PLCC44
            FlashDB.Add(New OTP_EPROM("ST M27C322", &H20, &H34, Mb032, MFP_IF.X16_5V_12V)) 'DIP42
            FlashDB.Add(New OTP_EPROM("ATMEL AT27C010", &H1E, &H5, Mb001, MFP_IF.X8_5V_12V))
            FlashDB.Add(New OTP_EPROM("ATMEL AT27C020", &H1E, &H86, Mb002, MFP_IF.X8_5V_12V))
            FlashDB.Add(New OTP_EPROM("ATMEL AT27C040", &H1E, &HB, Mb004, MFP_IF.X8_5V_12V))
            FlashDB.Add(New OTP_EPROM("ATMEL AT27C516", &H1E, &HF2, Kb512, MFP_IF.X16_5V_12V))
            FlashDB.Add(New OTP_EPROM("ATMEL AT27C1024", &H1E, &HF1, Mb001, MFP_IF.X16_5V_12V))
            FlashDB.Add(New OTP_EPROM("ATMEL AT27C2048", &H1E, &HF7, Mb002, MFP_IF.X16_5V_12V))
            FlashDB.Add(New OTP_EPROM("ATMEL AT27C4096", &H1E, &HF4, Mb004, MFP_IF.X16_5V_12V))
        End Sub

        Private Sub FWH_Database()
            FlashDB.Add(New FWH_Flash("Atmel AT49LH004", &H1F, &HEE, Mb002, Kb512, &H20))
            FlashDB.Add(New FWH_Flash("Winbond W39V040FA", &HDA, &H34, Mb004, Kb032, &H50))
            FlashDB.Add(New FWH_Flash("SST 49LF002A", &HBF, &H57, Mb002, Kb032, &H30))
            FlashDB.Add(New FWH_Flash("SST 49LF003A", &HBF, &H1B, Mb003, Kb032, &H30))
            FlashDB.Add(New FWH_Flash("SST 49LF004A", &HBF, &H60, Mb004, Kb032, &H30))
            FlashDB.Add(New FWH_Flash("SST 49LF008A", &HBF, &H5A, Mb008, Kb032, &H30))
        End Sub

        'Helper function to create the proper definition for Atmel/Adesto Series 45 SPI devices
        Private Function CreateSeries45(atName As String, mbitsize As UInt32, id1 As UInt16, id2 As UInt16, page_size As UInt32) As SPI_NOR_FLASH
            Dim atmel_spi As New SPI_NOR_FLASH(atName, mbitsize, &H1F, id1, &H50, page_size * 8, page_size)
            atmel_spi.ID2 = id2
            atmel_spi.PAGE_SIZE_EXTENDED = page_size + (page_size / 32) 'Additional bytes available per page
            atmel_spi.ProgramMode = SPI_ProgramMode.Atmel45Series  'Atmel Series 45
            atmel_spi.OP_COMMANDS.RDSR = &HD7
            atmel_spi.OP_COMMANDS.READ = &HE8
            atmel_spi.OP_COMMANDS.PROG = &H12
            Return atmel_spi
        End Function

        Public Function FindDevice(MFG As Byte, ID1 As UInt16, ID2 As UInt16, DEVICE As MemoryType, Optional FM As Byte = 0) As Device
            Select Case DEVICE
                Case MemoryType.SLC_NAND
                    For Each flash In FlashDB
                        If flash.FLASH_TYPE = MemoryType.SLC_NAND Then
                            If flash.MFG_CODE = MFG Then
                                If (flash.ID1 = ID1) Then
                                    If flash.ID2 = 0 Then Return flash 'ID2 is not used
                                    If (ID2 >> 8) = (flash.ID2 >> 8) Then 'First byte matches
                                        If (flash.ID2 And 255) = 0 Then Return flash 'second byte not needed
                                        If (ID2 And 255) = (flash.ID2 And 255) Then Return flash
                                    End If
                                End If
                            End If
                        End If
                    Next
                Case MemoryType.SERIAL_NOR
                    Dim list As New List(Of SPI_NOR_FLASH)
                    For Each flash In FlashDB
                        If flash.FLASH_TYPE = MemoryType.SERIAL_NOR Then
                            If flash.MFG_CODE = MFG Then
                                If (flash.ID1 = ID1) Then
                                    list.Add(DirectCast(flash, SPI_NOR_FLASH))
                                End If
                            End If
                        End If
                    Next
                    If list.Count = 1 Then Return list(0)
                    If (list.Count > 1) Then 'Find the best match
                        For Each flash In list
                            If flash.ID2 = ID2 AndAlso flash.FAMILY = FM Then Return flash
                        Next
                        For Each flash In list
                            If flash.ID2 = ID2 Then Return flash
                        Next
                        Return list(0)
                    End If
                Case MemoryType.PARALLEL_NOR
                    For Each flash In FlashDB
                        If flash.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                            If flash.MFG_CODE = MFG Then
                                If ID2 = 0 Then 'Only checks the LSB of ID1 (and ignore ID2)
                                    If (flash.ID1 = ID1) Then Return flash
                                    If ((ID1 >> 8) = 0) OrElse ((ID1 >> 8) = 255) Then
                                        If (ID1 And 255) = (flash.ID1 And 255) Then Return flash
                                    End If
                                Else
                                    If (flash.ID1 = ID1) Then
                                        If flash.ID2 = 0 OrElse flash.ID2 = ID2 Then Return flash
                                    End If
                                End If
                            End If
                        End If
                    Next
                Case MemoryType.OTP_EPROM
                    For Each flash In FlashDB
                        If flash.FLASH_TYPE = MemoryType.OTP_EPROM Then
                            If flash.MFG_CODE = MFG Then
                                If ID2 = 0 Then 'Only checks the LSB of ID1 (and ignore ID2)
                                    If (flash.ID1 = ID1) Then Return flash
                                    If ((ID1 >> 8) = 0) OrElse ((ID1 >> 8) = 255) Then
                                        If (ID1 And 255) = (flash.ID1 And 255) Then Return flash
                                    End If
                                Else
                                    If (flash.ID1 = ID1) Then
                                        If flash.ID2 = 0 OrElse flash.ID2 = ID2 Then Return flash
                                    End If
                                End If
                            End If
                        End If
                    Next
                Case MemoryType.SERIAL_NAND
                    For Each flash In FlashDB
                        If flash.FLASH_TYPE = MemoryType.SERIAL_NAND Then
                            If flash.MFG_CODE = MFG Then
                                If flash.ID1 = ID1 Then Return flash
                            End If
                        End If
                    Next
                Case MemoryType.FWH_NOR
                    For Each flash In FlashDB
                        If flash.FLASH_TYPE = MemoryType.FWH_NOR Then
                            If flash.MFG_CODE = MFG Then
                                If flash.ID1 = ID1 Then Return flash
                            End If
                        End If
                    Next
            End Select
            Return Nothing 'Not found
        End Function

        Public Function FindDevices(MFG As Byte, ID1 As UInt16, ID2 As UInt16, DEVICE As MemoryType) As Device()
            Dim devices As New List(Of Device)
            For Each flash In FlashDB
                If flash.FLASH_TYPE = DEVICE Then
                    If flash.MFG_CODE = MFG Then
                        If (flash.ID1 = ID1) Then
                            If flash.ID2 = 0 OrElse flash.ID2 = ID2 Then
                                devices.Add(flash)
                            ElseIf flash.FLASH_TYPE = MemoryType.SLC_NAND Then 'SLC NAND we may only want to check byte #3
                                If (flash.ID2 And &HFF) = 0 Then
                                    If (ID2 >> 8) = (flash.ID2 >> 8) Then devices.Add(flash)
                                End If
                            End If
                        End If
                    End If
                End If
            Next
            Return devices.ToArray
        End Function

        Public Function PartCount(Optional DEVICE As MemoryType = MemoryType.UNSPECIFIED) As UInt32 'Returns the total number of devices
            Dim Count As UInt32 = 0
            If DEVICE = MemoryType.UNSPECIFIED Then 'Search all devices
                Return FlashDB.Count
            ElseIf DEVICE = MemoryType.PARALLEL_NOR Then 'Search only CFI devices
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                        Count += 1
                    End If
                Next
            ElseIf DEVICE = MemoryType.SERIAL_NAND Then
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.SERIAL_NAND Then
                        Count += 1
                    End If
                Next
            ElseIf DEVICE = MemoryType.SERIAL_NOR Then
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.SERIAL_NOR Then
                        Count += 1
                    End If
                Next
            ElseIf DEVICE = MemoryType.SLC_NAND Then
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.SLC_NAND Then
                        Count += 1
                    End If
                Next
            ElseIf DEVICE = MemoryType.FWH_NOR Then
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.FWH_NOR Then
                        Count += 1
                    End If
                Next
            End If
            Return Count
        End Function

#Region "Catalog / Data file"
        Private Sub CreateHtmlCatalog(ByVal FlashType As MemoryType, ByVal ColumnCount As UInt32, ByVal file_name As String)
            Dim TotalParts() As Device = GetFlashDevices(FlashType)
            Dim FlashDevices() As DeviceCollection = SortFlashDevices(TotalParts)
            Dim RowCount As Integer = Math.Ceiling(FlashDevices.Length / ColumnCount)
            Dim ColumnPercent As Integer = 245 '225
            Dim cell_contents(FlashDevices.Length - 1) As String
            Dim part_prefixes As New List(Of String)
            Dim prefix As String = ""
            Select Case FlashType
                Case MemoryType.SERIAL_NOR
                    prefix = "spi_"
                Case MemoryType.SERIAL_NAND
                    prefix = "spinand_"
                Case MemoryType.PARALLEL_NOR
                    prefix = "nor_"
                Case MemoryType.SLC_NAND
                    prefix = "nand_"
            End Select
            'sort all of the devices into cell_contents
            For cell_index = 0 To cell_contents.Length - 1
                Dim PartNumbers() As String = Nothing
                GeneratePartNames(FlashDevices(cell_index), PartNumbers)
                Dim part_pre As String = prefix & FlashDevices(cell_index).Name.Replace(" ", "").Replace("/", "").ToLower
                part_prefixes.Add(part_pre)
                cell_contents(cell_index) = CreatePartTable(FlashDevices(cell_index).Name, PartNumbers, part_pre, ColumnPercent)
            Next
            Dim x As Integer = 0
            Dim html_body As New List(Of String)
            html_body.Add("<table style=""width: 100%; margin :0; border-collapse: collapse; word-spacing:0;"">")
            For row_ind = 1 To RowCount
                html_body.Add("   <tr>")
                For i As UInt32 = 1 To ColumnCount
                    html_body.Add("      <td valign=""top"">")
                    Dim s2() As String = cell_contents(x).Split(vbCrLf)
                    For Each line In s2
                        html_body.Add("         " & line.Replace(vbLf, ""))
                    Next
                    html_body.Add("      </td>")
                    x += 1
                    If x = FlashDevices.Length Then Exit For
                Next
                html_body.Add("   </tr>")
            Next
            html_body.Add("</table>")

            'Create script
            Dim script As New List(Of String)
            script.Add("<script type=""text/javascript"">")
            script.Add("function toggle_visibility(tbid,lnkid)")
            script.Add("{")
            script.Add("  if(document.all){document.getElementById(tbid).style.display = document.getElementById(tbid).style.display == ""block"" ? ""none"" : ""block"";}")
            script.Add("  else{document.getElementById(tbid).style.display = document.getElementById(tbid).style.display == ""table"" ? ""none"" : ""table"";}")
            script.Add("  document.getElementById(lnkid).value = document.getElementById(lnkid).value == ""[-] Collapse"" ? ""[+] Expand"" : ""[-] Collapse"";")
            script.Add("}")
            script.Add("</script>")

            Dim style As New List(Of String)
            Dim table_str As String = ""
            Dim link_str As String = ""
            style.Add("<style type=""text/css"">")
            For Each line In part_prefixes.ToArray
                table_str &= "#" & line & "_table,"
                link_str &= "#" & line & "_lnk,"
            Next
            table_str = table_str.Substring(0, table_str.Length - 1)
            link_str = link_str.Substring(0, link_str.Length - 1)
            style.Add("   " & table_str & " {display:none;}")
            style.Add("   " & link_str & " {border:none;background:none;width:85px;}")
            style.Add("</style>")
            style.Add("")

            Dim file_out As New List(Of String)
            file_out.AddRange(script.ToArray)
            file_out.AddRange(style.ToArray)
            file_out.AddRange(html_body.ToArray)
            Utilities.FileIO.WriteFile(file_out.ToArray, file_name)
        End Sub
        'Creates the part table/cell
        Private Function CreatePartTable(ByVal title As String, ByVal part_str() As String, ByVal part_prefix As String, ByVal column_size As Integer) As String
            Dim table_name As String = part_prefix & "_table"
            Dim link_name As String = part_prefix & "_lnk"
            Dim str_out As String = ""
            Dim title_str As String = title
            Select Case title.ToLower
                Case "adesto"
                    title_str = "Atmel / Adesto"
                Case "cypress"
                    title_str = "Spansion / Cypress"
            End Select
            title_str = title_str.Replace("_", " ")
            str_out = "<table style = ""width: " & column_size.ToString & "px"" align=""center"">"
            str_out &= "<tr><td valign=""top"" style=""text-align:right"">"
            'This is the 2 sub tables
            str_out &= "   <table style=""width: 100%; border-collapse :collapse; border: 1px solid #000000"">" & vbCrLf
            str_out &= "      <tr><td style=""width: 135px; height: 24px;"">" & title_str & "</td>" & vbCrLf
            str_out &= "      <td style=""height: 24px""><input id=""" & link_name & """ type=""button"" value=""[+] Expand"" onclick=""toggle_visibility('" & table_name & "','" & link_name & "');""></td></tr>" & vbCrLf
            str_out &= "   </table>" & vbCrLf
            str_out &= "   <table width=""100%"" border=""0"" cellpadding=""4"" cellspacing=""0"" id=""" & table_name & """ name=""" & table_name & """>" & vbCrLf
            str_out &= "      <tr><td>" & vbCrLf
            For i = 0 To part_str.Length - 1
                If i = part_str.Length - 1 Then
                    str_out &= "      " & part_str(i) & vbCrLf
                Else 'Not last item, add br
                    str_out &= "      " & part_str(i) & "<br>" & vbCrLf
                End If
            Next
            str_out &= "   </td></tr>" & vbCrLf
            str_out &= "   </table>"
            str_out &= "</td></tr></table>"
            Return str_out
        End Function
        'Returns all of the devices that match the device type
        Public Function GetFlashDevices(ByVal type As MemoryType) As Device()
            Dim dev As New List(Of Device)
            If type = MemoryType.PARALLEL_NOR Then 'Search only CFI devices
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                        dev.Add(flash)
                    End If
                Next
            ElseIf type = MemoryType.OTP_EPROM Then
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.OTP_EPROM Then
                        dev.Add(flash)
                    End If
                Next
            ElseIf type = MemoryType.SERIAL_NOR Then
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.SERIAL_NOR Then
                        dev.Add(flash)
                    End If
                Next
            ElseIf type = MemoryType.SERIAL_NAND Then
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.SERIAL_NAND Then
                        dev.Add(flash)
                    End If
                Next
            ElseIf type = MemoryType.SLC_NAND Then
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.SLC_NAND Then
                        dev.Add(flash)
                    End If
                Next
            End If
            Return dev.ToArray
        End Function
        'Sorts a collection into a group of the same manufacturer name
        Private Function SortFlashDevices(ByVal devices() As Device) As DeviceCollection()
            Dim GrowingCollection As New List(Of DeviceCollection)
            For Each dev In devices
                Dim SkipAdd As Boolean = False
                If dev.FLASH_TYPE = MemoryType.SERIAL_NOR Then
                    If DirectCast(dev, SPI_NOR_FLASH).EEPROM Then SkipAdd = True
                End If
                If Not SkipAdd Then
                    Dim Manu As String = dev.NAME.Substring(0, dev.NAME.IndexOf(" "))
                    Dim Part As String = dev.NAME.Substring(Manu.Length + 1)
                    Dim s As DeviceCollection = DevColIndexOf(GrowingCollection, Manu)
                    If (s Is Nothing) Then
                        Dim new_item As New DeviceCollection
                        new_item.Name = Manu
                        new_item.Parts = {dev}
                        GrowingCollection.Add(new_item)
                    Else 'Add to existing collection
                        If (s.Parts Is Nothing) Then
                            ReDim s.Parts(0)
                            s.Parts(0) = dev
                        Else
                            ReDim Preserve s.Parts(s.Parts.Length)
                            s.Parts(s.Parts.Length - 1) = dev
                        End If
                    End If
                End If
            Next
            Return GrowingCollection.ToArray()
        End Function

        Private Function DevColIndexOf(ByRef Collection As List(Of DeviceCollection), ByVal ManuName As String) As DeviceCollection
            For i = 0 To Collection.Count - 1
                If Collection(i).Name = ManuName Then
                    Return Collection(i)
                End If
            Next
            Return Nothing
        End Function

        Private Class DeviceCollection
            Friend Name As String
            Friend Parts() As Device
        End Class

        Private Sub GeneratePartNames(ByVal input As DeviceCollection, ByRef part_numbers() As String)
            ReDim part_numbers(input.Parts.Length - 1)
            For i = 0 To part_numbers.Length - 1
                Dim part_name As String = input.Parts(i).NAME
                part_name = part_name.Substring(input.Name.Length + 1)
                If part_name = "W25M121AV" Then
                    part_numbers(i) = part_name & " (128Mbit/1Gbit)"
                Else
                    Dim size_str As String = ""
                    If (input.Parts(i).FLASH_SIZE < Mb001) Then
                        size_str = (input.Parts(i).FLASH_SIZE / 128).ToString & "Kbit"
                    ElseIf (input.Parts(i).FLASH_SIZE < Gb001) Then
                        size_str = (input.Parts(i).FLASH_SIZE / Mb001).ToString & "Mbit"
                    Else
                        size_str = (input.Parts(i).FLASH_SIZE / Gb001).ToString & "Gbit"
                    End If
                    part_numbers(i) = part_name & " (" & size_str & ")"
                End If
            Next
        End Sub

        Public Sub WriteDatabaseToFile()
            Dim f As New List(Of String)
            For Each s As SPI_NOR_FLASH In FlashDB
                f.Add(s.NAME & " (" & (s.FLASH_SIZE / Mb001) & "Mbit)")
            Next
            Utilities.FileIO.WriteFile(f.ToArray, "d:\spi_flash_list.txt")
        End Sub

#End Region

    End Class

    Public Module NAND_LAYOUT_TOOL

        Public Structure NANDLAYOUT_STRUCTURE
            Dim Layout_Main As UInt16
            Dim Layout_Spare As UInt16
        End Structure

        Public Function NANDLAYOUT_Get(ByVal nand_dev As Device) As NANDLAYOUT_STRUCTURE
            Dim current_value As NANDLAYOUT_STRUCTURE
            Dim nand_page_size As UInt32
            Dim nand_ext_size As UInt32
            If nand_dev.GetType Is GetType(SPI_NAND_Flash) Then
                nand_page_size = DirectCast(nand_dev, SPI_NAND_Flash).PAGE_SIZE
                nand_ext_size = DirectCast(nand_dev, SPI_NAND_Flash).EXT_PAGE_SIZE
            ElseIf nand_dev.GetType Is GetType(SLC_NAND_Flash) Then
                nand_page_size = DirectCast(nand_dev, SLC_NAND_Flash).PAGE_SIZE
                nand_ext_size = DirectCast(nand_dev, SLC_NAND_Flash).EXT_PAGE_SIZE
            End If
            If MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Separated Then
                current_value.Layout_Main = nand_page_size
                current_value.Layout_Spare = nand_ext_size
            ElseIf MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Segmented Then
                Select Case nand_page_size
                    Case 2048
                        current_value.Layout_Main = (nand_page_size / 4)
                        current_value.Layout_Spare = (nand_ext_size / 4)
                    Case Else
                        current_value.Layout_Main = nand_page_size
                        current_value.Layout_Spare = nand_ext_size
                End Select
            ElseIf MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Combined Then
            End If
            Return current_value
        End Function

        Public Sub NANDLAYOUT_FILL_MAIN(ByVal nand_dev As Device, ByVal cache_data() As Byte, main_data() As Byte, ByRef data_ptr As UInt32, ByRef bytes_left As UInt32)
            Dim ext_page_size As UInt32
            If nand_dev.GetType Is GetType(SPI_NAND_Flash) Then
                ext_page_size = DirectCast(nand_dev, SPI_NAND_Flash).EXT_PAGE_SIZE
            ElseIf nand_dev.GetType Is GetType(SLC_NAND_Flash) Then
                ext_page_size = DirectCast(nand_dev, SLC_NAND_Flash).EXT_PAGE_SIZE
            End If
            Dim nand_layout As NANDLAYOUT_STRUCTURE = NANDLAYOUT_Get(nand_dev)
            Dim page_size_tot As UInt16 = (nand_dev.PAGE_SIZE + ext_page_size)
            Dim logical_block As UInt16 = (nand_layout.Layout_Main + nand_layout.Layout_Spare)
            Dim sub_index As UInt16 = 1
            Dim adj_offset As UInt16 = 0
            Do While Not (adj_offset = page_size_tot)
                Dim sub_left As UInt16 = nand_layout.Layout_Main
                If sub_left > bytes_left Then sub_left = bytes_left
                Array.Copy(main_data, data_ptr, cache_data, adj_offset, sub_left)
                data_ptr += sub_left
                bytes_left -= sub_left
                If (bytes_left = 0) Then Exit Do
                adj_offset = (sub_index * logical_block)
                sub_index += 1
            Loop
        End Sub

        Public Sub NANDLAYOUT_FILL_SPARE(ByVal nand_dev As Device, ByVal cache_data() As Byte, oob_data() As Byte, ByRef oob_ptr As UInt32, ByRef bytes_left As UInt32)
            Dim page_size_ext As UInt32
            If nand_dev.GetType Is GetType(SPI_NAND_Flash) Then
                page_size_ext = DirectCast(nand_dev, SPI_NAND_Flash).EXT_PAGE_SIZE
            ElseIf nand_dev.GetType Is GetType(SLC_NAND_Flash) Then
                page_size_ext = DirectCast(nand_dev, SLC_NAND_Flash).EXT_PAGE_SIZE
            End If
            Dim nand_layout As NANDLAYOUT_STRUCTURE = NANDLAYOUT_Get(nand_dev)
            Dim page_size_tot As UInt16 = (nand_dev.PAGE_SIZE + page_size_ext)
            Dim logical_block As UInt16 = (nand_layout.Layout_Main + nand_layout.Layout_Spare)
            Dim sub_index As UInt16 = 2
            Dim adj_offset As UInt16 = (logical_block - nand_layout.Layout_Spare)
            Do While Not ((adj_offset - nand_layout.Layout_Main) = page_size_tot)
                Dim sub_left As UInt16 = nand_layout.Layout_Spare
                If sub_left > bytes_left Then sub_left = bytes_left
                Array.Copy(oob_data, oob_ptr, cache_data, adj_offset, sub_left)
                oob_ptr += sub_left
                bytes_left -= sub_left
                If (bytes_left = 0) Then Exit Do
                adj_offset = (sub_index * logical_block) - nand_layout.Layout_Spare
                sub_index += 1
            Loop
        End Sub

        Public Function CreatePageAligned(ByVal nand_dev As Device, main_data() As Byte, oob_data() As Byte) As Byte()
            Dim page_size_ext As UInt32
            If nand_dev.GetType Is GetType(SPI_NAND_Flash) Then
                page_size_ext = DirectCast(nand_dev, SPI_NAND_Flash).EXT_PAGE_SIZE
            ElseIf nand_dev.GetType Is GetType(SLC_NAND_Flash) Then
                page_size_ext = DirectCast(nand_dev, SLC_NAND_Flash).EXT_PAGE_SIZE
            End If
            Dim page_size_tot As UInt16 = (nand_dev.PAGE_SIZE + page_size_ext)
            Dim total_pages As UInt32 = 0
            Dim data_ptr As UInt32 = 0
            Dim oob_ptr As UInt32 = 0
            Dim page_aligned() As Byte = Nothing
            If main_data Is Nothing Then
                total_pages = (oob_data.Length / page_size_ext)
                ReDim main_data((total_pages * nand_dev.PAGE_SIZE) - 1)
                Utilities.FillByteArray(main_data, 255)
            ElseIf oob_data Is Nothing Then
                total_pages = (main_data.Length / nand_dev.PAGE_SIZE)
                ReDim oob_data((total_pages * page_size_ext) - 1)
                Utilities.FillByteArray(oob_data, 255)
            Else
                total_pages = (main_data.Length / nand_dev.PAGE_SIZE)
            End If
            ReDim page_aligned((total_pages * page_size_tot) - 1)
            Dim bytes_left As UInt32 = page_aligned.Length
            For i = 0 To total_pages - 1
                Dim cache_data(page_size_tot - 1) As Byte
                If main_data IsNot Nothing Then NANDLAYOUT_FILL_MAIN(nand_dev, cache_data, main_data, data_ptr, bytes_left)
                If oob_data IsNot Nothing Then NANDLAYOUT_FILL_SPARE(nand_dev, cache_data, oob_data, oob_ptr, bytes_left)
                Array.Copy(cache_data, 0, page_aligned, (i * page_size_tot), cache_data.Length)
            Next
            Return page_aligned
        End Function

        Public Function GetNandPageAddress(ByVal nand_dev As Device, ByVal gui_addr As UInt32, ByVal memory_area As FlashArea) As UInt32
            Dim nand_page_size As UInt32 '0x800 (2048)
            Dim nand_ext_size As UInt32 '0x40 (64)
            If nand_dev.GetType Is GetType(SPI_NAND_Flash) Then
                nand_page_size = DirectCast(nand_dev, SPI_NAND_Flash).PAGE_SIZE
                nand_ext_size = DirectCast(nand_dev, SPI_NAND_Flash).EXT_PAGE_SIZE
            ElseIf nand_dev.GetType Is GetType(SLC_NAND_Flash) Then
                nand_page_size = DirectCast(nand_dev, SLC_NAND_Flash).PAGE_SIZE
                nand_ext_size = DirectCast(nand_dev, SLC_NAND_Flash).EXT_PAGE_SIZE
            End If
            Dim page_addr As UInt32 'This is the page address
            If (memory_area = FlashArea.Main) Then
                page_addr = (gui_addr / nand_page_size)
            ElseIf (memory_area = FlashArea.OOB) Then
                page_addr = Math.Floor(gui_addr / nand_ext_size)
            ElseIf (memory_area = FlashArea.All) Then   'we need to adjust large address to logical address
                page_addr = Math.Floor(gui_addr / (nand_page_size + nand_ext_size))
            End If
            Return page_addr
        End Function

    End Module


End Namespace