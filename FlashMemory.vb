Namespace FlashMemory

    Public Module Constants
        Public Const Kb032 As UInt32 = 4096
        Public Const Kb064 As UInt32 = 8192
        Public Const Kb128 As UInt32 = 16384
        Public Const Kb256 As UInt32 = 32768
        Public Const Kb512 As UInt32 = 65536
        Public Const Mb001 As UInt32 = 131072
        Public Const Mb002 As UInt32 = 262144
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
    End Module

    Public Enum MemoryType
        UNSPECIFIED
        PARALLEL_NOR 'CFI devices
        SERIAL_NOR 'SPI devices
        SLC_NAND 'NAND devices
        JTAG 'CFI devices accessed via the JTAG chain
        VOLATILE 'RAM
        'MLC_NAND
        'TLC_NAND
        'SERIAL_NAND
    End Enum

    Public Enum FlashArea As Byte
        Main = 0
        OOB = 1
        All = 2
    End Enum

    Public Enum MFP_ProgramMode
        Standard 'Use the standard sequence that chip id detected
        PageMode 'Writes an entire page of data (128 bytes etc.)
        BypassMode 'Writes 64 bytes using ByPass sequence
        IntelSharp 'SA=0x40;SA=DATA;SR.7
        Buffer 'Use Write-To-Buffer mode (x16 only)
    End Enum

    Public Enum MFP_SectorEraseMode
        Standard 'Use the standard sequence that chip id detected
        IntelSharp 'SA=0x50;SA=0x60;SA=0xD0,SR.7,SA=0x20;SA=0xD0,SR.7
    End Enum

    Public Enum EraseMethod
        Standard 'Chip-Erase, then Blank check
        BySector 'Erase each sector (some chips lack Erase All)
    End Enum

    Public Enum DelayMethod
        LoopCycles
        StatusRegister
    End Enum

    Public Enum BadBlockMarker
        noneyet 'Default
    End Enum
    'Contains SPI definition command op codes (usually industry standard)
    Public Class SPI_Command_DEF
        Friend Shared RDID As Byte = &H9F 'Read Identification
        Friend Shared REMS As Byte = &H90 'Read Electronic Manufacturer Signature 
        Friend Shared RES As Byte = &HAB 'Read Electronic Signature
        Friend Shared RSFDP As Byte = &H5A 'Read Serial Flash Discoverable Parameters
        Public WRSR As Byte = &H1 'Write Status Register
        Public PROG As Byte = &H2 'Page Program or word program (AAI) command
        Public READ As Byte = &H3 'Read-data
        Public WRDI As Byte = &H4 'Write-Disable
        Public RDSR As Byte = &H5 'Read Status Register
        Public WREN As Byte = &H6 'Write-Enable
        Public EWSR As Byte = &H50 'Enable Write Status Register (used by SST/PCT chips) or (Clear Flag Status Register)
        Public RDFR As Byte = &H70 'Read Flag Status Register
        Public WRTB As Byte = &H84 'Command to write data into SRAM buffer 1 (used by Atmel)
        Public WRFB As Byte = &H88 'Command to write data from SRAM buffer 1 into page (used by Atmel)
        Public BE As Byte = &HC7 'Bulk Erase (or chip erase) Sometimes 0x60
        Public SE As Byte = &HD8 'Erases one sector (or one block)
        Public AAI_WORD As Byte = &HAD 'Used for PROG when in AAI Word Mode
        Public AAI_BYTE As Byte = &HAF 'Used for PROG when in AAI Byte Mode
        Public EN4B As Byte = &HB7 'Enter 4-byte address mode (only used for certain 32-bit SPI devices)
        Public EX4B As Byte = &HE9 'Exit 4-byte address mode (only used for certain 32-bit SPI devices)
        Public ULBPR As Byte = &H98 'Global Block Protection Unlock
    End Class

    Public Enum SPI_QUADMODE
        NotSupported = 0
        SST_Micro = 1
        Winbond = 2
    End Enum

    Public Enum SPI_ProgramMode
        PageMode = 0
        AAI_Byte = 1
        AAI_Word = 2
        Atmel45Series = 3
        Nordic = 4
        SPI_EEPROM_8BIT = 5 'For 8bit ST/Atmel EEPROMS
        SPI_EEPROM = 6
    End Enum

    Public Interface Device
        ReadOnly Property NAME As String 'Manufacturer and part number
        ReadOnly Property FLASH_TYPE As MemoryType
        ReadOnly Property FLASH_SIZE As Long 'Size of this flash device (without spare area)
        ReadOnly Property MFG_CODE As Byte 'The manufaturer byte ID
        ReadOnly Property PART_CODE As UInt32 'The part ID (and up to 3 additional bytes)
        ReadOnly Property PAGE_SIZE As UInt32 'Size of the pages
        ReadOnly Property BLOCK_COUNT As UInt32 'Total number of blocks or sectors this flash device has
        Property ERASE_REQUIRED As Boolean 'Indicates that the sector/block must be erased prior to writing

    End Interface

    Public Class MFP_Flash
        Implements Device
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property PART_CODE As UInteger Implements Device.PART_CODE
        Public ReadOnly Property FLASH_TYPE As MemoryType Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public Property PAGE_SIZE As UInt32 Implements Device.PAGE_SIZE 'Only used for WRITE_PAGE mode of certain flash devices
        Public Property ERASE_REQUIRED As Boolean Implements Device.ERASE_REQUIRED
        Public Property RESET_REQUIRED As Boolean = False 'Set to TRUE to do a RESET after SE, CE, WR
        Public Property WriteMode As MFP_ProgramMode = MFP_ProgramMode.Standard 'This indicates the perfered programing method
        Public Property SectorEraseMode As MFP_SectorEraseMode = MFP_SectorEraseMode.Standard
        Public Property ChipEraseMode As EraseMethod = EraseMethod.Standard
        Public Property WRITE_DELAY_CYCLES As UInt16 = 10 'Number of clock cycles between write (mult by 2)

        Sub New(f_name As String, MFG As Byte, PART As UInt32, f_size As Long)
            FLASH_TYPE = MemoryType.PARALLEL_NOR
            Me.NAME = f_name
            Me.FLASH_SIZE = f_size
            Me.MFG_CODE = MFG
            Me.PART_CODE = PART
            Me.ERASE_REQUIRED = True
        End Sub

        Sub New(f_name As String, MFG As Byte, PART As UInt32, f_size As Long, uniform_block As UInt32)
            FLASH_TYPE = MemoryType.PARALLEL_NOR
            Me.NAME = f_name
            Me.FLASH_SIZE = f_size
            Me.MFG_CODE = MFG
            Me.PART_CODE = PART
            Dim TotalSectors As UInt32 = Me.FLASH_SIZE / uniform_block
            For i As UInt32 = 1 To TotalSectors
                SectorList.Add(uniform_block)
            Next
            Me.ERASE_REQUIRED = True
        End Sub

        Private SectorList As New List(Of UInt32)

        Public Sub AddSector(ByVal SectorSize As UInt32)
            SectorList.Add(SectorSize)
        End Sub

        Public Sub AddSector(ByVal SectorSize As UInt32, ByVal Count As Integer)
            For i = 1 To Count
                SectorList.Add(SectorSize)
            Next
        End Sub

        Public Function GetSectorSize(ByVal SectorIndex As Integer) As Integer
            Try
                Return SectorList(SectorIndex)
            Catch ex As Exception
            End Try
            Return -1
        End Function

        Public ReadOnly Property BLOCK_COUNT As UInt32 Implements Device.BLOCK_COUNT
            Get
                Return SectorList.Count
            End Get
        End Property

    End Class

    Public Class CFI_Flash
        Implements Device
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property PART_CODE As UInteger Implements Device.PART_CODE
        Public ReadOnly Property FLASH_TYPE As MemoryType Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public ReadOnly Property PAGE_SIZE As UInt32 Implements Device.PAGE_SIZE
        Public Property ERASE_REQUIRED As Boolean Implements Device.ERASE_REQUIRED

        Sub New(f_name As String, MFG As Byte, PART As UInt32, f_size As Long)
            Me.FLASH_TYPE = MemoryType.JTAG
            Me.FLASH_SIZE = f_size
            Me.NAME = f_name
            Me.MFG_CODE = MFG
            Me.PART_CODE = PART
            Me.ERASE_REQUIRED = True
        End Sub

        Public ReadOnly Property BLOCK_COUNT As UInt32 Implements Device.BLOCK_COUNT
            Get
                Return 0 'Not needed. JTAG code auto-configures for this.
            End Get
        End Property

    End Class

    Public Class SPI_FLASH
        Implements Device
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property PART_CODE As UInteger Implements Device.PART_CODE
        Public ReadOnly Property FLASH_TYPE As MemoryType Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public Property ERASE_REQUIRED As Boolean Implements Device.ERASE_REQUIRED
        'These are properties unique to SPI devices
        Public Property ADDRESSBITS As UInt32 = 24 'Number of bits the address space takes up (16/24/32)
        Public ReadOnly Property PAGE_COUNT As Integer 'The total number of pages this flash contains
        Public ReadOnly Property PAGE_SIZE_BASE As UInt32 'Number of bytes per page
        Public ReadOnly Property PAGE_SIZE_EXTENDED As UInt32
        Public Property PAGE_EXTENDED As Boolean = False 'Indicates this SPI device has an extended page area enabled
        Public Property QUAD As SPI_QUADMODE = SPI_QUADMODE.NotSupported 'SQI mode
        Public Property ProgramMode As SPI_ProgramMode
        Public Property SEND_4BYTE As Boolean = False 'Set to True to send the EN4B
        Public Property SEND_RDFS As Boolean = False 'Set to True to read the flag status register

        Private ERASE_SIZE_BASE As UInt32 = &H10000 'Number of bytes per page that are erased(typically 64KB)

        Public ReadOnly Property PAGE_SIZE As UInt32 Implements Device.PAGE_SIZE  'Number of bytes per page
            Get
                If PAGE_EXTENDED Then
                    Return PAGE_SIZE_EXTENDED
                Else
                    Return PAGE_SIZE_BASE
                End If
            End Get
        End Property
        Public Property ERASE_SIZE As UInt32
            Get
                If (Not PAGE_EXTENDED) Then
                    Return ERASE_SIZE_BASE
                Else
                    Dim NumberOfPages As Integer = ERASE_SIZE_BASE / PAGE_SIZE_BASE
                    Dim EXT_ERASE As UInt32 = NumberOfPages * PAGE_SIZE_EXTENDED
                    Return EXT_ERASE
                End If
            End Get
            Set(value As UInt32)
                ERASE_SIZE_BASE = value
            End Set
        End Property

        Public OP_COMMANDS As New SPI_Command_DEF 'Contains a list of op-codes used to read/write/erase

        'FlashName, Size, and Page Size
        Sub New(ByVal f_name As String, ByVal f_size As UInt32, ByVal page_size As UInt32)
            Me.NAME = f_name
            Me.FLASH_SIZE = f_size
            Me.PAGE_SIZE_BASE = page_size
            Me.PAGE_SIZE_EXTENDED = page_size + (page_size / 32)
            Me.PAGE_COUNT = f_size / page_size
            Me.MFG_CODE = 0
            Me.PART_CODE = 0
            Me.ERASE_REQUIRED = True
            Me.FLASH_TYPE = MemoryType.SERIAL_NOR
        End Sub

        Sub New(ByVal f_name As String, ByVal f_size As UInt32, ByVal ID As Byte, ByVal PART As UInt32, Optional PAGESIZE As UInt32 = 256)
            NAME = f_name
            Me.FLASH_SIZE = f_size
            PAGE_SIZE_BASE = PAGESIZE
            PAGE_SIZE_EXTENDED = PAGE_SIZE_BASE + (PAGE_SIZE_BASE / 32)
            PAGE_COUNT = f_size / PAGE_SIZE
            Me.MFG_CODE = ID
            Me.PART_CODE = PART
            If (f_size > Mb128) Then Me.ADDRESSBITS = 32
            ERASE_REQUIRED = True
            FLASH_TYPE = MemoryType.SERIAL_NOR
        End Sub

        Sub New(ByVal f_name As String, ByVal f_size As UInt32, ByVal ID As Byte, ByVal PART As UInt32, ByVal ERASECMD As Byte, ByVal ERASESIZE As UInt32, Optional PAGESIZE As UInt32 = 256)
            Me.NAME = f_name
            Me.FLASH_SIZE = f_size
            Me.PAGE_SIZE_BASE = PAGESIZE
            Me.PAGE_SIZE_EXTENDED = PAGE_SIZE_BASE + (PAGE_SIZE_BASE / 32)
            Me.PAGE_COUNT = f_size / PAGE_SIZE
            Me.MFG_CODE = ID
            Me.PART_CODE = PART
            OP_COMMANDS.SE = ERASECMD 'Sometimes 0xD8 or 0x20
            Me.ERASE_SIZE = ERASESIZE
            If (f_size > Mb128) Then Me.ADDRESSBITS = 32
            Me.ERASE_REQUIRED = True
            Me.FLASH_TYPE = MemoryType.SERIAL_NOR
        End Sub

        Sub New(ByVal f_name As String, ByVal f_size As UInt32, ByVal ID As Byte, ByVal PART As UInt32, ByVal ERASECMD As Byte, ByVal ERASESIZE As UInt32, ByVal READCMD As Byte, ByVal WRITECMD As Byte)
            Me.NAME = f_name
            Me.FLASH_SIZE = f_size
            Me.PAGE_SIZE_BASE = 256
            Me.PAGE_SIZE_EXTENDED = PAGE_SIZE_BASE + (PAGE_SIZE_BASE / 32)
            Me.PAGE_COUNT = f_size / PAGE_SIZE
            Me.MFG_CODE = ID
            Me.PART_CODE = PART
            OP_COMMANDS.SE = ERASECMD 'Sometimes 0xD8 or 0x20
            OP_COMMANDS.READ = READCMD
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

        Public ReadOnly Property BLOCK_COUNT As UInt32 Implements Device.BLOCK_COUNT
            Get
                If PAGE_SIZE_BASE = 0 Then Return 1
                Return FLASH_SIZE / PAGE_SIZE_BASE
            End Get
        End Property

    End Class

    Public Class NAND_Flash
        Implements Device
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property PART_CODE As UInteger Implements Device.PART_CODE
        Public ReadOnly Property FLASH_TYPE As MemoryType Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public ReadOnly Property PAGE_SIZE As UInt32 Implements Device.PAGE_SIZE 'Total number of bytes per page (not including OOB)
        Public Property ERASE_REQUIRED As Boolean Implements Device.ERASE_REQUIRED
        'These are properties unique to NAND memory:
        Public Property PAGE_SIZE_EXTENDED As UInt16 'Total number of bytes per page (with spare/extended area)
        Public Property BLOCK_SIZE As UInt32 'Number of bytes per block (not including extended pages)
        'Public Property ADDR_MODE As AddressingMode 'This indicates how to write the address table

        Sub New(FlashName As String, MFG As Byte, PART As UInt32, m_size As Long, PageSize As UInt16, SpareSize As UInt16, BlockSize As UInt32, Optional BADBLOCK As BadBlockMarker = BadBlockMarker.noneyet)
            Me.NAME = FlashName
            Me.FLASH_TYPE = MemoryType.SLC_NAND
            Me.PAGE_SIZE = PageSize 'Does not include extended / spare pages
            Me.PAGE_SIZE_EXTENDED = (PageSize + SpareSize) 'This is the entire size of the page
            Me.MFG_CODE = MFG
            Me.PART_CODE = PART
            Me.FLASH_SIZE = m_size 'Does not include extended /spare areas
            Me.BLOCK_SIZE = BlockSize
            Me.ERASE_REQUIRED = True
        End Sub

        Public ReadOnly Property BLOCK_COUNT As UInt32 Implements Device.BLOCK_COUNT
            Get
                Return (FLASH_SIZE / BLOCK_SIZE)
            End Get
        End Property

    End Class

    Public Class FlashDatabase
        Public FlashDB As New List(Of Device)

        Sub New()
            SPI_Database() 'Adds all of the SPI and QSPI devices
            MFP_Database() 'Adds all of the TSOP/PLCC etc. devices
            CFI_Database() 'Adds all of the devices that are used by the JTAG CFI engine
            NAND_Database() 'Adds all of the NAND compatible devices
        End Sub

        'Helper function to create the proper definition for Atmel/Adesto Series 45 SPI devices
        Private Function CreateSeries45(ByVal atName As String, ByVal mbitsize As UInt32, ByVal manu16 As UInt16, ByVal edi() As Byte, ByVal page_size As UInt32) As SPI_FLASH
            Dim atmel_spi As New SPI_FLASH(atName, mbitsize, &H1F, manu16, &H50, page_size * 8, page_size)
            atmel_spi.ProgramMode = SPI_ProgramMode.Atmel45Series  'Atmel Series 45
            atmel_spi.OP_COMMANDS.RDSR = &HD7
            atmel_spi.OP_COMMANDS.READ = &HE8
            atmel_spi.OP_COMMANDS.PROG = &H12
            Return atmel_spi
        End Function

        Private Sub CFI_Database()
            FlashDB.Add(New CFI_Flash("Spansion S29GL256M", &H1, &H7E1201, Mb256))
            FlashDB.Add(New CFI_Flash("Spansion S29GL128M", &H1, &H7E1200, Mb128))
            FlashDB.Add(New CFI_Flash("Spansion S29GL064M", &H1, &H7E1300, Mb064))
            FlashDB.Add(New CFI_Flash("Spansion S29GL064M", &H1, &H7E1301, Mb064))
            FlashDB.Add(New CFI_Flash("Spansion S29GL032M", &H1, &H7E1C00, Mb032))
            FlashDB.Add(New CFI_Flash("Spansion S29GL032M", &H1, &H7E1A00, Mb032)) 'Bottom boot
            FlashDB.Add(New CFI_Flash("Spansion S29GL032M", &H1, &H7E1A01, Mb032)) 'Top Boot
            FlashDB.Add(New CFI_Flash("Spansion S70GL02G", &H1, &H7E4801, Gb002))
            FlashDB.Add(New CFI_Flash("Spansion S29GL01G", &H1, &H7E2801, Gb001))
            FlashDB.Add(New CFI_Flash("Spansion S29GL512", &H1, &H7E2301, Mb512))
            FlashDB.Add(New CFI_Flash("Spansion S29GL256", &H1, &H7E2201, Mb256))
            FlashDB.Add(New CFI_Flash("Spansion S29GL128", &H1, &H7E2101, Mb128))
            FlashDB.Add(New CFI_Flash("AMD 28F400BT", &H1, &H2223, Mb004))
            FlashDB.Add(New CFI_Flash("AMD 29DL322GB", &H1, &H2256, Mb032))
            FlashDB.Add(New CFI_Flash("AMD 29DL322GT", &H1, &H2255, Mb032))
            FlashDB.Add(New CFI_Flash("AMD 29DL323GB", &H1, &H2253, Mb032))
            FlashDB.Add(New CFI_Flash("AMD 29DL323GT", &H1, &H2250, Mb032))
            FlashDB.Add(New CFI_Flash("AMD 29DL324GB", &H1, &H225F, Mb032))
            FlashDB.Add(New CFI_Flash("AMD 29DL324GT", &H1, &H225C, Mb032))
            FlashDB.Add(New CFI_Flash("AMD 29LV160DB", &H1, &H2249, Mb016))
            FlashDB.Add(New CFI_Flash("AMD 29LV160DT", &H1, &H22C4, Mb016))
            FlashDB.Add(New CFI_Flash("AMD 29LV320DB", &H1, &H22F9, Mb032))
            FlashDB.Add(New CFI_Flash("AMD 29LV320DT", &H1, &H22F6, Mb032))
            FlashDB.Add(New CFI_Flash("AMD 29LV320MB", &H1, &H2200, Mb032))
            FlashDB.Add(New CFI_Flash("AMD 29LV320MT", &H1, &H2201, Mb032))
            FlashDB.Add(New CFI_Flash("AMD 29LV400BB", &H1, &H22BA, Mb004))
            FlashDB.Add(New CFI_Flash("AMD 29LV800BB", &H1, &H225B, Mb008))
            FlashDB.Add(New CFI_Flash("ATMEL AT49BV/LV16X", &H1F, &HC0, Mb016))
            FlashDB.Add(New CFI_Flash("ATMEL AT49BV/LV16XT", &H1F, &HC2, Mb016))
            FlashDB.Add(New CFI_Flash("HYHYNIX HY29F400TT", &HAD, &H2223, Mb004))
            FlashDB.Add(New CFI_Flash("HYHYNIX HY29LV1600T", &HAD, &H22C4, Mb016))
            FlashDB.Add(New CFI_Flash("Intel 28F160B3", &H89, &H8891, Mb016))
            FlashDB.Add(New CFI_Flash("Intel 28F160B3", &H89, &H8890, Mb016))
            FlashDB.Add(New CFI_Flash("Intel 28F800B3", &H89, &H8893, Mb008))
            FlashDB.Add(New CFI_Flash("Intel 28F320B3", &H89, &H8896, Mb032))
            FlashDB.Add(New CFI_Flash("Intel 28F320B3", &H89, &H8897, Mb032))
            FlashDB.Add(New CFI_Flash("Intel 28F640B3", &H89, &H8898, Mb064))
            FlashDB.Add(New CFI_Flash("Intel 28F640B3", &H89, &H8899, Mb064))
            FlashDB.Add(New CFI_Flash("Intel TE28F800C3T", &H89, &H88C0, Mb008))
            FlashDB.Add(New CFI_Flash("Intel TE28F800C3B", &H89, &H88C1, Mb008))
            FlashDB.Add(New CFI_Flash("Intel TE28F160C3T", &H89, &H88C2, Mb016))
            FlashDB.Add(New CFI_Flash("Intel TE28F160C3B", &H89, &H88C3, Mb016))
            FlashDB.Add(New CFI_Flash("Intel TE28F320C3T", &H89, &H88C4, Mb032))
            FlashDB.Add(New CFI_Flash("Intel TE28F320C3B", &H89, &H88C5, Mb032))
            FlashDB.Add(New CFI_Flash("Intel TE28F640C3T", &H89, &H88CC, Mb064))
            FlashDB.Add(New CFI_Flash("Intel TE28F640C3B", &H89, &H88CD, Mb064))
            FlashDB.Add(New CFI_Flash("Intel 28F320J5", &H89, &H14, Mb032))
            FlashDB.Add(New CFI_Flash("Intel 28F640J5", &H89, &H15, Mb064))
            FlashDB.Add(New CFI_Flash("Intel 28F320J3", &H89, &H16, Mb032))
            FlashDB.Add(New CFI_Flash("Intel 28F640J3", &H89, &H17, Mb064))
            FlashDB.Add(New CFI_Flash("Intel 28F128J3", &H89, &H18, Mb128))
            FlashDB.Add(New CFI_Flash("Samsung K8D1716UB", &HEC, &H2277, Mb016))
            FlashDB.Add(New CFI_Flash("Samsung K8D1716UT", &HEC, &H2275, Mb016))
            FlashDB.Add(New CFI_Flash("Samsung K8D3216UB", &HEC, &H22A2, Mb016))
            FlashDB.Add(New CFI_Flash("Samsung K8D3216UT", &HEC, &H22A0, Mb016))
            FlashDB.Add(New CFI_Flash("ST M28W160CB", &H20, &H88CF, Mb016))
            FlashDB.Add(New CFI_Flash("ST M29D323DB", &H20, &H225F, Mb032))
            FlashDB.Add(New CFI_Flash("FUJITSU 29DL323GB", &H4, &H2253, Mb032))
            FlashDB.Add(New CFI_Flash("FUJITSU 29DL323TE", &H4, &H225C, Mb032))
            FlashDB.Add(New CFI_Flash("FUJITSU 29LV160B", &H4, &H2249, Mb016))
            FlashDB.Add(New CFI_Flash("FUJITSU 29LV160T", &H4, &H22C4, Mb016))
            FlashDB.Add(New CFI_Flash("FUJITSU 29LV320BE", &H4, &H22F9, Mb032))
            FlashDB.Add(New CFI_Flash("FUJITSU 29LV320TE", &H4, &H22F6, Mb032))
            FlashDB.Add(New CFI_Flash("FUJITSU 29LV800B", &H4, &H225B, Mb008))
            FlashDB.Add(New CFI_Flash("Micron 28F160C34B", &H2C, &H4493, Mb016))
            FlashDB.Add(New CFI_Flash("Micron 28F160C34T", &H2C, &H4492, Mb016))
            FlashDB.Add(New CFI_Flash("Micron 28F322P3", &H2C, &H4495, Mb032))
            FlashDB.Add(New CFI_Flash("MXIC 25FL0165A", &HC2, &H20, Mb016))
            FlashDB.Add(New CFI_Flash("MXIC 29LV800T", &HC2, &H22DA, Mb008)) 'NON-CFI
            FlashDB.Add(New CFI_Flash("MXIC 29LV800B", &HC2, &H225B, Mb008)) 'NON-CFI
            FlashDB.Add(New CFI_Flash("MXIC 29LV161T", &HC2, &H22C4, Mb016)) 'NON-CFI
            FlashDB.Add(New CFI_Flash("MXIC 29LV161B", &HC2, &H2249, Mb016)) 'NON-CFI
            FlashDB.Add(New CFI_Flash("MXIC 29LV320B", &HC2, &HA8, Mb032))
            FlashDB.Add(New CFI_Flash("MXIC 29LV320B", &HC2, &H22A8, Mb032))
            FlashDB.Add(New CFI_Flash("MXIC 29LV320T", &HC2, &H22A7, Mb032))
            FlashDB.Add(New CFI_Flash("MXIC 29LV800BMC", &HC2, &H225B, Mb008))
            FlashDB.Add(New CFI_Flash("SHARP 28F320BJE", &HB0, &HE3, Mb032))
            FlashDB.Add(New CFI_Flash("SHARP LH28F160BJHG", &HB0, &HE9, Mb016))
            FlashDB.Add(New CFI_Flash("SHARP 28F160S3", &HB0, &HD0, Mb016))
            FlashDB.Add(New CFI_Flash("SHARP 28F320S3", &HB0, &HD4, Mb032))
            FlashDB.Add(New CFI_Flash("SST 39VF1600", &HBF, &H2782, Mb016))
            FlashDB.Add(New CFI_Flash("SST 39VF1601", &HBF, &H234B, Mb016))
            FlashDB.Add(New CFI_Flash("SST 39VF3201", &HBF, &H235B, Mb032))
            FlashDB.Add(New CFI_Flash("SST 39VF800", &HBF, &H2781, Mb008))
            FlashDB.Add(New CFI_Flash("ST MT28W320", &H20, &H88BB, Mb032))
            FlashDB.Add(New CFI_Flash("ST MT28W320", &H20, &H88BC, Mb032))
            FlashDB.Add(New CFI_Flash("ST 29W320DB", &H20, &H22CB, Mb032))
            FlashDB.Add(New CFI_Flash("ST 29W320DT", &H20, &H22CA, Mb032))
            FlashDB.Add(New CFI_Flash("ST M29W160EB", &H20, &H2249, Mb016))
            FlashDB.Add(New CFI_Flash("ST M29W160ET", &H20, &H22C4, Mb016))
            FlashDB.Add(New CFI_Flash("ST M58LW064D", &H20, &H17, Mb064))
            FlashDB.Add(New CFI_Flash("ST M29W800AB", &H20, &H5B, Mb008))
            FlashDB.Add(New CFI_Flash("TOSHIBA TC58FVB160", &H98, &H43, Mb016))
            FlashDB.Add(New CFI_Flash("TOSHIBA TC58FVB321", &H98, &H9C, Mb016))
            FlashDB.Add(New CFI_Flash("TOSHIBA TC58FVT160", &H98, &HC2, Mb016))
            FlashDB.Add(New CFI_Flash("TOSHIBA TC58FVT160B", &H98, &H43, Mb016))
            FlashDB.Add(New CFI_Flash("TOSHIBA TC58FVT321", &H98, &H9A, Mb032))
        End Sub

        Private Sub SPI_Database()
            'FlashDB.Add(New SPI_Flash("Generic 1Mbit", MB001, &H10, &H1010)) 'ST M25P10-A
            'Atmel AT25 Series
            FlashDB.Add(New SPI_FLASH("Atmel AT25DF641", Mb064, &H1F, &H4800)) 'Confirmed (build 350)
            FlashDB.Add(New SPI_FLASH("Atmel AT25DF321S", Mb032, &H1F, &H4701))
            FlashDB.Add(New SPI_FLASH("Atmel AT25DF321", Mb032, &H1F, &H4700))
            FlashDB.Add(New SPI_FLASH("Atmel AT25DF161", Mb016, &H1F, &H4602))
            FlashDB.Add(New SPI_FLASH("Atmel AT25DF081", Mb008, &H1F, &H4502))
            FlashDB.Add(New SPI_FLASH("Atmel AT25DF021", Mb002, &H1F, &H4300))
            'Atmel AT26 Series
            FlashDB.Add(New SPI_FLASH("Atmel AT26DF321", Mb032, &H1F, &H4700))
            FlashDB.Add(New SPI_FLASH("Atmel AT26DF161", Mb016, &H1F, &H4600))
            FlashDB.Add(New SPI_FLASH("Atmel AT26DF161A", Mb016, &H1F, &H4601))
            FlashDB.Add(New SPI_FLASH("Atmel AT26DF081A", Mb008, &H1F, &H4501))
            'Atmel AT45 Series (updated on July 2016) - Licensed to Adesto
            FlashDB.Add(CreateSeries45("Adesto AT45DB641E", Mb064, &H2800, {&H1, &H0}, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB642D", Mb064, &H2800, {&H0}, 1024))
            FlashDB.Add(CreateSeries45("Adesto AT45DB321E", Mb032, &H2701, {&H1, &H0}, 512)) 'CONFIRMED July 2016
            FlashDB.Add(CreateSeries45("Adesto AT45DB321D", Mb032, &H2701, {&H0}, 512))
            FlashDB.Add(CreateSeries45("Adesto AT45DB161E", Mb016, &H2600, {&H1, &H0}, 512))
            FlashDB.Add(CreateSeries45("Adesto AT45DB161D", Mb016, &H2600, {&H0}, 512))
            FlashDB.Add(CreateSeries45("Adesto AT45DB081E", Mb008, &H2500, {&H1, &H0}, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB081D", Mb008, &H2500, {&H0}, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB041E", Mb004, &H2400, {&H1, &H0}, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB041D", Mb004, &H2400, {&H0}, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB021E", Mb002, &H2300, {&H1, &H0}, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB021D", Mb002, &H2300, {&H0}, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB011D", Mb001, &H2200, {&H0}, 256))
            'Adesto AT25 series
            FlashDB.Add(New SPI_FLASH("Adesto AT25SF161", Mb016, &H1F, &H8601))
            FlashDB.Add(New SPI_FLASH("Adesto AT25SF081", Mb008, &H1F, &H8501))
            FlashDB.Add(New SPI_FLASH("Adesto AT25SF041", Mb004, &H1F, &H8401)) 'TESTING
            FlashDB.Add(New SPI_FLASH("Adesto AT25XV041", Mb004, &H1F, &H4402))
            FlashDB.Add(New SPI_FLASH("Adesto AT25XV021", Mb002, &H1F, &H4301))
            'Cypress 25FL Series (formely Spansion)
            FlashDB.Add(New SPI_FLASH("Cypress S70FL01GS", Gb001, &H1, &H221, &HDC, &H40000, &H13, &H12))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL512S", Mb512, &H1, &H220, &HDC, &H40000, &H13, &H12))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL256S", Mb256, &H1, &H2194D00, &HDC, &H40000, &H13, &H12)) 'Confirmed (build 371)
            FlashDB.Add(New SPI_FLASH("Cypress S25FL256S", Mb256, &H1, &H2194D01, &HDC, &H10000, &H13, &H12))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL128S", Mb128, &H1, &H20184D00, &HD8, &H40000, &H3, &H2))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL128S", Mb128, &H1, &H20184D01, &HD8, &H10000, &H3, &H2))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL129P", Mb128, &H1, &H20184D00, &HD8, &H40000, &H3, &H2))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL129P", Mb128, &H1, &H20184D01, &HD8, &H10000, &H3, &H2))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL128P", Mb128, &H1, &H20180300, &HD8, &H40000, &H3, &H2))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL128P", Mb128, &H1, &H20180301, &HD8, &H10000, &H3, &H2))
            FlashDB.Add(New SPI_FLASH("Cypress S70FL256P", Mb256, &H1, &H20184D00, &HDC, &H40000, &H13, &H12)) '2x S25FL128S DIES (pin 6 is second CS)
            FlashDB.Add(New SPI_FLASH("Cypress S70FL256P", Mb256, &H1, &H20184D01, &HDC, &H10000, &H13, &H12)) '2x S25FL128S DIES (pin 6 is second CS)
            FlashDB.Add(New SPI_FLASH("Cypress S25FL064", Mb064, &H1, &H216))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL032", Mb032, &H1, &H215))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL016A", Mb016, &H1, &H214))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL008A", Mb008, &H1, &H213))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL040A", Mb004, &H1, &H212))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL164K", Mb064, &H1, &H4017))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL132K", Mb032, &H1, &H4016))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL116K", Mb016, &H1, &H4015))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL216K", Mb016, &H1, &H4015)) 'Uses the same ID as S25FL116K (might support 3 byte ID)
            FlashDB.Add(New SPI_FLASH("Cypress S25FL208K", Mb008, &H1, &H4014))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL204K", Mb004, &H1, &H4013))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL004A", Mb004, &H1, &H212))
            'Micron (ST)
            FlashDB.Add(New SPI_FLASH("Micron N25Q00A", Gb001, &H20, &HBA21) With {.SEND_4BYTE = True, .SEND_RDFS = True})
            FlashDB.Add(New SPI_FLASH("Micron N25Q512", Mb512, &H20, &HBA20) With {.SEND_4BYTE = True, .SEND_RDFS = True}) 'Verified (build 390)
            FlashDB.Add(New SPI_FLASH("Micron N25Q256", Mb256, &H20, &HBA19) With {.SEND_4BYTE = True}) 'Confirmed (build 350)
            FlashDB.Add(New SPI_FLASH("Micron NP5Q128A", Mb128, &H20, &HDA18, 64) With {.ERASE_SIZE = &H20000}) 'NEW! PageSize is 64 bytes
            FlashDB.Add(New SPI_FLASH("Micron N25Q128", Mb128, &H20, &HBA18))
            FlashDB.Add(New SPI_FLASH("Micron N25Q064A", Mb064, &H20, &HBB17))
            FlashDB.Add(New SPI_FLASH("Micron N25Q064", Mb064, &H20, &HBA17))
            FlashDB.Add(New SPI_FLASH("Micron N25Q032", Mb032, &H20, &HBA16))
            FlashDB.Add(New SPI_FLASH("Micron N25Q016", Mb016, &H20, &HBB15))
            FlashDB.Add(New SPI_FLASH("Micron N25Q008", Mb008, &H20, &HBB14))
            FlashDB.Add(New SPI_FLASH("Micron M25P128", Mb128, &H20, &H2018) With {.ERASE_SIZE = Mb002}) 'Confirmed (build 350)
            FlashDB.Add(New SPI_FLASH("Micron M25P64", Mb064, &H20, &H2017)) 'Confirmed (Build 350)
            FlashDB.Add(New SPI_FLASH("Micron M25PX32", Mb032, &H20, &H7116))
            FlashDB.Add(New SPI_FLASH("Micron M25P32", Mb032, &H20, &H2016))
            FlashDB.Add(New SPI_FLASH("Micron M25PX16", Mb016, &H20, &H7115))
            FlashDB.Add(New SPI_FLASH("Micron M25P16", Mb016, &H20, &H2015))
            FlashDB.Add(New SPI_FLASH("Micron M25P80", Mb008, &H20, &H2014))
            FlashDB.Add(New SPI_FLASH("Micron M25PX80", Mb008, &H20, &H7114)) 'Build 370
            FlashDB.Add(New SPI_FLASH("Micron M25P40", Mb004, &H20, &H2013))
            FlashDB.Add(New SPI_FLASH("Micron M25P20", Mb002, &H20, &H2012))
            FlashDB.Add(New SPI_FLASH("Micron M25P10", Mb001, &H20, &H2011))
            FlashDB.Add(New SPI_FLASH("Micron M25P05", Kb512, &H20, &H2010))
            FlashDB.Add(New SPI_FLASH("Micron M25PX64", Mb064, &H20, &H7117))
            FlashDB.Add(New SPI_FLASH("Micron M25PX32", Mb032, &H20, &H7116))
            FlashDB.Add(New SPI_FLASH("Micron M25PX16", Mb016, &H20, &H7115))
            FlashDB.Add(New SPI_FLASH("Micron M25PE16", Mb016, &H20, &H8015))
            FlashDB.Add(New SPI_FLASH("Micron M25PE80", Mb008, &H20, &H8014))
            FlashDB.Add(New SPI_FLASH("Micron M25PE40", Mb004, &H20, &H8013))
            FlashDB.Add(New SPI_FLASH("Micron M25PE20", Mb002, &H20, &H8012))
            FlashDB.Add(New SPI_FLASH("Micron M25PE10", Mb001, &H20, &H8011))
            FlashDB.Add(New SPI_FLASH("Micron M45PE16", Mb016, &H20, &H4015))
            FlashDB.Add(New SPI_FLASH("Micron M45PE80", Mb008, &H20, &H4014))
            FlashDB.Add(New SPI_FLASH("Micron M45PE40", Mb004, &H20, &H4013))
            FlashDB.Add(New SPI_FLASH("Micron M45PE20", Mb002, &H20, &H4012))
            FlashDB.Add(New SPI_FLASH("Micron M45PE10", Mb001, &H20, &H4011))
            'Windbond
            'http://www.nexflash.com/hq/enu/ProductAndSales/ProductLines/FlashMemory/SerialFlash/
            FlashDB.Add(New SPI_FLASH("Winbond W25M512", Mb512, &HEF, &H4020) With {.SEND_4BYTE = True})
            FlashDB.Add(New SPI_FLASH("Winbond W25Q256", Mb256, &HEF, &H4019) With {.SEND_4BYTE = True})
            FlashDB.Add(New SPI_FLASH("Winbond W25Q128", Mb128, &HEF, &H4018))
            FlashDB.Add(New SPI_FLASH("Winbond W25Q64", Mb064, &HEF, &H4017))
            FlashDB.Add(New SPI_FLASH("Winbond W25Q32", Mb032, &HEF, &H4016))
            FlashDB.Add(New SPI_FLASH("Winbond W25Q16", Mb016, &HEF, &H4015)) 'Confirmed (Build 350)
            FlashDB.Add(New SPI_FLASH("Winbond W25Q80", Mb008, &HEF, &H4014)) 'Confirmed (Build 350)
            FlashDB.Add(New SPI_FLASH("Winbond W25Q80BW", Mb008, &HEF, &H5014)) 'Added in build 350
            FlashDB.Add(New SPI_FLASH("Winbond W25Q40", Mb004, &HEF, &H4013))
            FlashDB.Add(New SPI_FLASH("Winbond W25Q128FW", Mb128, &HEF, &H6018))
            FlashDB.Add(New SPI_FLASH("Winbond W25Q64FW", Mb064, &HEF, &H6017))
            FlashDB.Add(New SPI_FLASH("Winbond W25Q32FW", Mb032, &HEF, &H6016))
            FlashDB.Add(New SPI_FLASH("Winbond W25Q16FW", Mb016, &HEF, &H6015))
            FlashDB.Add(New SPI_FLASH("Winbond W25X64", Mb064, &HEF, &H3017))
            FlashDB.Add(New SPI_FLASH("Winbond W25X64", Mb064, &HEF, &H3017))
            FlashDB.Add(New SPI_FLASH("Winbond W25X32", Mb032, &HEF, &H3016))
            FlashDB.Add(New SPI_FLASH("Winbond W25X16", Mb016, &HEF, &H3015))
            FlashDB.Add(New SPI_FLASH("Winbond W25X80", Mb008, &HEF, &H3014))
            FlashDB.Add(New SPI_FLASH("Winbond W25X40", Mb004, &HEF, &H3013))
            FlashDB.Add(New SPI_FLASH("Winbond W25X20", Mb002, &HEF, &H3012))
            FlashDB.Add(New SPI_FLASH("Winbond W25X10", Mb002, &HEF, &H3011))
            FlashDB.Add(New SPI_FLASH("Winbond W25X05", Mb001, &HEF, &H3010))
            'MXIC
            FlashDB.Add(New SPI_FLASH("MXIC MX25L51245G", Mb512, &HC2, &H201A) With {.SEND_4BYTE = True}) 'Added (Build 372)
            FlashDB.Add(New SPI_FLASH("MXIC MX25L25655E", Mb256, &HC2, &H2619) With {.SEND_4BYTE = True}) 'Added (Build 371)
            FlashDB.Add(New SPI_FLASH("MXIC MX25L256", Mb256, &HC2, &H2019) With {.SEND_4BYTE = True}) 'Added (Build 350)
            FlashDB.Add(New SPI_FLASH("MXIC MX25L12855E", Mb128, &HC2, &H2618)) 'Added Build 372
            FlashDB.Add(New SPI_FLASH("MXIC MX25L128", Mb128, &HC2, &H2018))
            FlashDB.Add(New SPI_FLASH("MXIC MX25L6455E", Mb064, &HC2, &H2617)) 'Added Build 372
            FlashDB.Add(New SPI_FLASH("MXIC MX25L640", Mb064, &HC2, &H2017))
            FlashDB.Add(New SPI_FLASH("MXIC MX25L320", Mb032, &HC2, &H2016)) 'Confirmed Build 350
            FlashDB.Add(New SPI_FLASH("MXIC MX25L3205D", Mb032, &HC2, &H20FF))
            FlashDB.Add(New SPI_FLASH("MXIC MX25L323", Mb032, &HC2, &H5E16))
            FlashDB.Add(New SPI_FLASH("MXIC MX25L160", Mb016, &HC2, &H2015))
            FlashDB.Add(New SPI_FLASH("MXIC MX25L80", Mb008, &HC2, &H2014))
            FlashDB.Add(New SPI_FLASH("MXIC MX25L40", Mb004, &HC2, &H2013))
            FlashDB.Add(New SPI_FLASH("MXIC MX25L20", Mb002, &HC2, &H2012)) 'MX25L2005 MX25L2006E MX25L2026E
            FlashDB.Add(New SPI_FLASH("MXIC MX25L10", Mb001, &HC2, &H2011))
            FlashDB.Add(New SPI_FLASH("MXIC MX25U643", Mb064, &HC2, &H2537))
            FlashDB.Add(New SPI_FLASH("MXIC MX25U323", Mb032, &HC2, &H2536))
            FlashDB.Add(New SPI_FLASH("MXIC MX25U163", Mb016, &HC2, &H2535))
            FlashDB.Add(New SPI_FLASH("MXIC MX25L512", Kb512, &HC2, &H2010)) 'Added (Build 371)
            FlashDB.Add(New SPI_FLASH("MXIC MX25L1021E", Mb001, &HC2, &H2211)) 'Added (Build 371)
            FlashDB.Add(New SPI_FLASH("MXIC MX25L5121E", Kb512, &HC2, &H2210)) 'Added (Build 371)
            FlashDB.Add(New SPI_FLASH("MXIC MX66L51235F", Mb512, &HC2, &H201A) With {.SEND_4BYTE = True}) 'Uses MX25L51245G (Build 405)
            FlashDB.Add(New SPI_FLASH("MXIC MX25V8035", Mb008, &HC2, &H2554))
            FlashDB.Add(New SPI_FLASH("MXIC MX25V4035", Mb004, &HC2, &H2553))
            FlashDB.Add(New SPI_FLASH("MXIC MX25V8035F", Mb008, &HC2, &H2314))
            'EON
            FlashDB.Add(New SPI_FLASH("EON EN25Q128", Mb128, &H1C, &H3018)) 'Fixed
            FlashDB.Add(New SPI_FLASH("EON EN25Q32", Mb032, &H1C, &H3016))
            FlashDB.Add(New SPI_FLASH("EON EN25Q16", Mb016, &H1C, &H3015))
            FlashDB.Add(New SPI_FLASH("EON EN25Q80", Mb008, &H1C, &H3014))
            FlashDB.Add(New SPI_FLASH("EON EN25Q40", Mb004, &H1C, &H3013))
            FlashDB.Add(New SPI_FLASH("EON EN25QH128", Mb128, &H1C, &H7018)) 'Added build 402
            FlashDB.Add(New SPI_FLASH("EON EN25QH64", Mb064, &H1C, &H7017))
            FlashDB.Add(New SPI_FLASH("EON EN25QH32", Mb032, &H1C, &H7016)) 'Added build 350
            FlashDB.Add(New SPI_FLASH("EON EN25QH16", Mb016, &H1C, &H7015)) 'Added build 402
            FlashDB.Add(New SPI_FLASH("EON EN25QH80", Mb008, &H1C, &H7014)) 'Added build 402
            FlashDB.Add(New SPI_FLASH("EON EN25P64", Mb064, &H1C, &H2017))
            FlashDB.Add(New SPI_FLASH("EON EN25P32", Mb032, &H1C, &H2016))
            FlashDB.Add(New SPI_FLASH("EON EN25P16", Mb016, &H1C, &H2015))
            FlashDB.Add(New SPI_FLASH("EON EN25F32", Mb032, &H1C, &H3116)) 'Added build 372
            FlashDB.Add(New SPI_FLASH("EON EN25F16", Mb016, &H1C, &H3115)) 'Added build 372
            FlashDB.Add(New SPI_FLASH("EON EN25F80", Mb008, &H1C, &H3114))
            FlashDB.Add(New SPI_FLASH("EON EN25F40", Mb004, &H1C, &H3113))
            FlashDB.Add(New SPI_FLASH("EON EN25F20", Mb002, &H1C, &H3112))
            FlashDB.Add(New SPI_FLASH("EON EN25T32", Mb032, &H1C, &H5116))
            FlashDB.Add(New SPI_FLASH("EON EN25T16", Mb016, &H1C, &H5115))
            FlashDB.Add(New SPI_FLASH("EON EN25T80", Mb008, &H1C, &H5114))
            FlashDB.Add(New SPI_FLASH("EON EN25T40", Mb004, &H1C, &H5113))
            FlashDB.Add(New SPI_FLASH("EON EN25T20", Mb002, &H1C, &H5112))
            'Microchip / Silicon Storage Technology (SST) / PCT Group (Rebranded)
            'http://www.microchip.com/pagehandler/en-us/products/memory/flashmemory/sfhome.html
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF128B", Mb128, &HBF, &H2544)) 'Might use AAI
            FlashDB.Add(New SPI_FLASH("Microchip SST26VF064B", Mb064, &HBF, &H2643)) 'SST26VF064BA
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF064C", Mb064, &HBF, &H254B)) 'PCT25VF064C
            FlashDB.Add(New SPI_FLASH("Microchip SST26VF064", Mb064, &HBF, &H2603))
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF032B", Mb032, &HBF, &H254A) With {.ProgramMode = SPI_ProgramMode.AAI_Word}) 'PCT25VF032B
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF032", Mb032, &HBF, &H2542) With {.ProgramMode = SPI_ProgramMode.AAI_Word})
            FlashDB.Add(New SPI_FLASH("Microchip SST26VF032", Mb032, &HBF, &H2602)) 'PCT26VF032
            FlashDB.Add(New SPI_FLASH("Microchip SST26VF032B", Mb032, &HBF, &H2642)) 'SST26VF032BA
            FlashDB.Add(New SPI_FLASH("Microchip SST26WF032", Mb032, &HBF, &H2622)) 'PCT26WF032
            FlashDB.Add(New SPI_FLASH("Microchip SST26VF016", Mb016, &HBF, &H2601, &H20, &H1000) With {.QUAD = SPI_QUADMODE.SST_Micro}) 'Testing (Block-Protection)
            FlashDB.Add(New SPI_FLASH("Microchip SST26VF032", Mb032, &HBF, &H2602, &H20, &H1000) With {.QUAD = SPI_QUADMODE.SST_Micro}) 'Testing (Block-Protection)
            FlashDB.Add(New SPI_FLASH("Microchip SST26VF016B", Mb016, &HBF, &H2641, &H20, &H1000)) 'SST26VF016BA
            FlashDB.Add(New SPI_FLASH("Microchip SST26VF016", Mb016, &HBF, &H16BF, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte})
            FlashDB.Add(New SPI_FLASH("Microchip SST26WF016B", Mb016, &HBF, &H2651)) 'SST26WF016BA
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF016B", Mb016, &HBF, &H2541) With {.ProgramMode = SPI_ProgramMode.AAI_Word}) 'Confirmed (Build 350)
            FlashDB.Add(New SPI_FLASH("Microchip SST26WF080B", Mb008, &HBF, &H2658, &H20, &H1000))
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF080B", Mb008, &HBF, &H258E, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word}) 'PCT25VF080B - Confirmed (Build 350)
            FlashDB.Add(New SPI_FLASH("Microchip SST25WF080B", Mb008, &H62, &H1614, &H20, &H1000)) 'Working HERE
            FlashDB.Add(New SPI_FLASH("Microchip SST26WF040B", Mb004, &HBF, &H2654, &H20, &H1000))
            FlashDB.Add(New SPI_FLASH("Microchip SST25WF040B", Mb004, &H62, &H1613, &H20, &H1000))
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF080", Mb008, &HBF, &H80BF, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte})
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF040B", Mb004, &HBF, &H258D, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word}) 'PCT25VF040B
            FlashDB.Add(New SPI_FLASH("Microchip SST25WF040", Mb004, &HBF, &H2504, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word})
            FlashDB.Add(New SPI_FLASH("Microchip SST25WF020A", Mb002, &H62, &H1612, &H20, &H1000))
            FlashDB.Add(New SPI_FLASH("Microchip SST25LF020A", Mb002, &HBF, &H43BF, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte})
            FlashDB.Add(New SPI_FLASH("Microchip SST25WF020", Mb002, &HBF, &H2503, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word})
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF020", Mb002, &HBF, &H258C, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word}) 'SST25VF020B SST25PF020B PCT25VF020B
            FlashDB.Add(New SPI_FLASH("Microchip SST25WF010", Mb001, &HBF, &H2502, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word})
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF010", Mb001, &HBF, &H49BF, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte}) 'SST25VF010A PCT25VF010A
            FlashDB.Add(New SPI_FLASH("Microchip SST25WF512", Kb512, &HBF, &H2501, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word})
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF512", Kb512, &HBF, &H48BF, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte}) 'SST25VF512A PCT25VF512A
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF020A", Mb002, &HBF, &H43, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte}) 'Confirmed (Build 350)
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF010A", Mb001, &HBF, &H49, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte}) 'Confirmed (Build 350)
            'PMC
            FlashDB.Add(New SPI_FLASH("PMC PM25LV016B", Mb016, &H7F, &H9D14))
            FlashDB.Add(New SPI_FLASH("PMC PM25LV080B", Mb008, &H7F, &H9D13))
            FlashDB.Add(New SPI_FLASH("PMC PM25LV040", Mb004, &H9D, &H7E7F))
            FlashDB.Add(New SPI_FLASH("PMC PM25LV020", Mb002, &H9D, &H7D7F))
            FlashDB.Add(New SPI_FLASH("PMC PM25LD020", Mb002, &H7F, &H9D22)) 'Added in 366
            FlashDB.Add(New SPI_FLASH("PMC PM25LV010", Mb001, &H9D, &H7C7F))
            FlashDB.Add(New SPI_FLASH("PMC PM25LV512", Kb512, &H9D, &H7B7F))
            'AMIC
            'http://www.amictechnology.com/english/flash_spi_flash.html
            FlashDB.Add(New SPI_FLASH("AMIC A25LQ64", Mb064, &H37, &H4017)) 'A25LMQ64
            FlashDB.Add(New SPI_FLASH("AMIC A25LQ32A", Mb032, &H37, &H4016))
            FlashDB.Add(New SPI_FLASH("AMIC A25L032", Mb032, &H37, &H3016))
            FlashDB.Add(New SPI_FLASH("AMIC A25L016", Mb016, &H37, &H3015))
            FlashDB.Add(New SPI_FLASH("AMIC A25LQ16", Mb016, &H37, &H4015))
            FlashDB.Add(New SPI_FLASH("AMIC A25L080", Mb008, &H37, &H3014))
            FlashDB.Add(New SPI_FLASH("AMIC A25L040", Mb004, &H37, &H3013)) 'A25L040A A25P040
            FlashDB.Add(New SPI_FLASH("AMIC A25L020", Mb002, &H37, &H3012)) 'A25L020C A25P020
            FlashDB.Add(New SPI_FLASH("AMIC A25L010", Mb001, &H37, &H3011)) 'A25L010A A25P010
            FlashDB.Add(New SPI_FLASH("AMIC A25L512", Kb512, &H37, &H3010)) 'A25L512A A25P512
            FlashDB.Add(New SPI_FLASH("AMIC A25LS512A", Kb512, &HC2, &H2010))
            'Fidelix
            'http://www.fidelix.co.kr/semiconductor/eng/product/serialflash.jsp
            FlashDB.Add(New SPI_FLASH("Fidelix FM25Q16A", Mb016, &HF8, &H3215)) 'FM25Q16B
            FlashDB.Add(New SPI_FLASH("Fidelix FM25Q32A", Mb032, &HF8, &H3216))
            FlashDB.Add(New SPI_FLASH("Fidelix FM25M04A", Mb004, &HF8, &H4213))
            FlashDB.Add(New SPI_FLASH("Fidelix FM25M08A", Mb008, &HF8, &H4214))
            FlashDB.Add(New SPI_FLASH("Fidelix FM25M16A", Mb016, &HF8, &H4215))
            FlashDB.Add(New SPI_FLASH("Fidelix FM25M32A", Mb032, &HF8, &H4216))
            FlashDB.Add(New SPI_FLASH("Fidelix FM25M64A", Mb064, &HF8, &H4217))
            FlashDB.Add(New SPI_FLASH("Fidelix FM25M4AA", Mb004, &HF8, &H4212))
            'Gigadevice
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25Q128", Mb128, &HC8, &H4018))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25Q64", Mb064, &HC8, &H4017))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25Q32", Mb032, &HC8, &H4016))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25Q16", Mb016, &HC8, &H4015))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25Q80", Mb008, &HC8, &H4014))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25Q40", Mb004, &HC8, &H4013))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25Q20", Mb002, &HC8, &H4012))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25Q10", Mb001, &HC8, &H4011))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25Q512", Kb512, &HC8, &H4010))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25VQ16C", Mb016, &HC8, &H4215))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25VQ80C", Mb008, &HC8, &H4214))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25VQ41B", Mb004, &HC8, &H4213))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25VQ21B", Mb002, &HC8, &H4212))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25LQ128", Mb128, &HC8, &H6018))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25LQ64", Mb064, &HC8, &H6017))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25LQ32", Mb032, &HC8, &H6016))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25LQ16", Mb016, &HC8, &H6015))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25LQ80", Mb008, &HC8, &H6014))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25LQ40", Mb004, &HC8, &H6013))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25LQ20", Mb002, &HC8, &H6012))
            FlashDB.Add(New SPI_FLASH("Gigadevice GD25LQ10", Mb001, &HC8, &H6011))
            'ISSI
            FlashDB.Add(New SPI_FLASH("ISSI IS25LD040", Mb004, &H7F, &H9D7E))
            FlashDB.Add(New SPI_FLASH("ISSI IS25WQ040", Mb004, &H9D, &H1253))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LQ010", Mb001, &H9D, &H4011))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LQ020", Mb002, &H9D, &H4012))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LQ040", Mb004, &H9D, &H4013))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LQ080", Mb008, &H9D, &H4014))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LQ016", Mb016, &H9D, &H4015))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LQ032", Mb032, &H9D, &H4016))
            'Others
            FlashDB.Add(New SPI_FLASH("ESMT F25L04", Mb004, &H8C, &H12) With {.ProgramMode = SPI_ProgramMode.AAI_Word}) 'REMS only
            FlashDB.Add(New SPI_FLASH("ESMT F25L04", Mb004, &H8C, &H2013) With {.ProgramMode = SPI_ProgramMode.AAI_Word})
            FlashDB.Add(New SPI_FLASH("ESMT F25L08", Mb008, &H8C, &H13) With {.ProgramMode = SPI_ProgramMode.AAI_Word}) 'REMS only
            FlashDB.Add(New SPI_FLASH("ESMT F25L08", Mb008, &H8C, &H2014) With {.ProgramMode = SPI_ProgramMode.AAI_Word})
            FlashDB.Add(New SPI_FLASH("ESMT F25L32QA", Mb032, &H8C, &H4116))
            FlashDB.Add(New SPI_FLASH("Sanyo LE25FU406B", Mb004, &H62, &H1E62))
            FlashDB.Add(New SPI_FLASH("Berg Micro BG25Q32A", Mb032, &HE0, &H4016))
            'SUPPORTED EEPROM SPI DEVICES:
            FlashDB.Add(New SPI_FLASH("Atmel AT25128B", 16384, 64))
            FlashDB.Add(New SPI_FLASH("Atmel AT25256B", 32768, 64))
            FlashDB.Add(New SPI_FLASH("Atmel AT25512", 65536, 128))
            FlashDB.Add(New SPI_FLASH("ST M95010", 128, 16))
            FlashDB.Add(New SPI_FLASH("ST M95020", 256, 16))
            FlashDB.Add(New SPI_FLASH("ST M95040", 512, 16))
            FlashDB.Add(New SPI_FLASH("ST M95080", 1024, 32))
            FlashDB.Add(New SPI_FLASH("ST M95160", 2048, 32))
            FlashDB.Add(New SPI_FLASH("ST M95320", 4096, 32))
            FlashDB.Add(New SPI_FLASH("ST M95640", 8192, 32))
            FlashDB.Add(New SPI_FLASH("ST M95128", 16384, 64))
            FlashDB.Add(New SPI_FLASH("ST M95256", 32768, 64))
            FlashDB.Add(New SPI_FLASH("ST M95512", 65536, 128))
            FlashDB.Add(New SPI_FLASH("ST M95M01", 131072, 256))
            FlashDB.Add(New SPI_FLASH("ST M95M02", 262144, 256))
            FlashDB.Add(New SPI_FLASH("Atmel AT25010A", 128, 8))
            FlashDB.Add(New SPI_FLASH("Atmel AT25020A", 256, 8))
            FlashDB.Add(New SPI_FLASH("Atmel AT25040A", 512, 8))
            FlashDB.Add(New SPI_FLASH("Atmel AT25080", 1024, 32))
            FlashDB.Add(New SPI_FLASH("Atmel AT25160", 2048, 32))
            FlashDB.Add(New SPI_FLASH("Atmel AT25320", 4096, 32))
            FlashDB.Add(New SPI_FLASH("Atmel AT25640", 8192, 32))
            FlashDB.Add(New SPI_FLASH("Microchip 25AA160A", 2048, 16))
            FlashDB.Add(New SPI_FLASH("Microchip 25AA160B", 2048, 32))
        End Sub

        Private Sub MFP_Database()
            'https://github.com/jhcloos/flashrom/blob/master/flashchips.h
            'Intel
            Dim TE28F800_T As New MFP_Flash("Intel 28F800C3", &H89, &HC0, Mb008) 'Top-boot
            TE28F800_T.WriteMode = MFP_ProgramMode.IntelSharp
            TE28F800_T.SectorEraseMode = MFP_SectorEraseMode.IntelSharp
            TE28F800_T.ChipEraseMode = EraseMethod.BySector
            TE28F800_T.AddSector(Kb064, 8)
            TE28F800_T.AddSector(Kb512, 15)
            FlashDB.Add(TE28F800_T)
            Dim TE28F800_B As New MFP_Flash("Intel 28F800C3", &H89, &HC1, Mb008) 'Bottom-boot
            TE28F800_B.WriteMode = MFP_ProgramMode.IntelSharp
            TE28F800_B.SectorEraseMode = MFP_SectorEraseMode.IntelSharp
            TE28F800_B.ChipEraseMode = EraseMethod.BySector
            TE28F800_B.AddSector(Kb512, 15)
            TE28F800_B.AddSector(Kb064, 8)
            FlashDB.Add(TE28F800_B)
            Dim TE28F160_T As New MFP_Flash("Intel 28F160C3", &H89, &HC2, Mb016) 'Top
            TE28F160_T.WriteMode = MFP_ProgramMode.IntelSharp
            TE28F160_T.SectorEraseMode = MFP_SectorEraseMode.IntelSharp
            TE28F160_T.ChipEraseMode = EraseMethod.BySector
            TE28F160_T.AddSector(Kb064, 8) '8x 8Kbyte
            TE28F160_T.AddSector(Kb512, 31) '63x 64Kbyte
            FlashDB.Add(TE28F160_T)
            Dim TE28F160_B As New MFP_Flash("Intel 28F160C3", &H89, &HC3, Mb016) 'Bottom
            TE28F160_B.WriteMode = MFP_ProgramMode.IntelSharp
            TE28F160_B.SectorEraseMode = MFP_SectorEraseMode.IntelSharp
            TE28F160_B.ChipEraseMode = EraseMethod.BySector
            TE28F160_B.AddSector(Kb512, 31) '63x 64Kbyte
            TE28F160_B.AddSector(Kb064, 8) '8x 8Kbyte
            FlashDB.Add(TE28F160_B)
            Dim TE28F320_T As New MFP_Flash("Intel 28F320C3", &H89, &HC4, Mb032) 'Top
            TE28F320_T.WriteMode = MFP_ProgramMode.IntelSharp
            TE28F320_T.SectorEraseMode = MFP_SectorEraseMode.IntelSharp
            TE28F320_T.ChipEraseMode = EraseMethod.BySector
            TE28F320_T.AddSector(Kb064, 8)
            TE28F320_T.AddSector(Kb512, 63)
            FlashDB.Add(TE28F320_T)
            Dim TE28F320_B As New MFP_Flash("Intel 28F320C3", &H89, &HC5, Mb032) 'Bottom
            TE28F320_B.WriteMode = MFP_ProgramMode.IntelSharp
            TE28F320_B.SectorEraseMode = MFP_SectorEraseMode.IntelSharp
            TE28F320_B.ChipEraseMode = EraseMethod.BySector
            TE28F320_B.AddSector(Kb512, 63)
            TE28F320_B.AddSector(Kb064, 8)
            FlashDB.Add(TE28F320_B)
            'TSOP-56 (Type-B for 28F series)
            FlashDB.Add(New MFP_Flash("Intel 28F320J3", &H89, &H16, Mb032, Mb001) With {.WriteMode = MFP_ProgramMode.Buffer, .SectorEraseMode = MFP_SectorEraseMode.IntelSharp, .ChipEraseMode = EraseMethod.BySector})
            FlashDB.Add(New MFP_Flash("Intel 28F640J3", &H89, &H17, Mb064, Mb001) With {.WriteMode = MFP_ProgramMode.Buffer, .SectorEraseMode = MFP_SectorEraseMode.IntelSharp, .ChipEraseMode = EraseMethod.BySector})
            FlashDB.Add(New MFP_Flash("Intel 28F128J3", &H89, &H18, Mb128, Mb001) With {.WriteMode = MFP_ProgramMode.Buffer, .SectorEraseMode = MFP_SectorEraseMode.IntelSharp, .ChipEraseMode = EraseMethod.BySector})
            FlashDB.Add(New MFP_Flash("Intel 28F256J3", &H89, &H1D, Mb256, Mb001) With {.WriteMode = MFP_ProgramMode.Buffer, .SectorEraseMode = MFP_SectorEraseMode.IntelSharp, .ChipEraseMode = EraseMethod.BySector})
            FlashDB.Add(New MFP_Flash("Intel 28F320J5", &H89, &H14, Mb032, Mb001) With {.WriteMode = MFP_ProgramMode.Buffer, .SectorEraseMode = MFP_SectorEraseMode.IntelSharp, .ChipEraseMode = EraseMethod.BySector})
            FlashDB.Add(New MFP_Flash("Intel 28F640J5", &H89, &H15, Mb064, Mb001) With {.WriteMode = MFP_ProgramMode.Buffer, .SectorEraseMode = MFP_SectorEraseMode.IntelSharp, .ChipEraseMode = EraseMethod.BySector})

            Dim AM29F800B As New MFP_Flash("AMD AM29F800B", &H1, &H58, Mb008) 'Tested with SO-44 socket (x16 mode, 5v)
            'AM29F800B.EraseMode = EraseMethod.BySector
            AM29F800B.AddSector(Kb512, 15)
            AM29F800B.AddSector(Kb256, 1)
            AM29F800B.AddSector(Kb064, 2)
            AM29F800B.AddSector(Kb128, 1)
            FlashDB.Add(AM29F800B)
            FlashDB.Add(New MFP_Flash("AMD AM29F040B", &H20, &HE2, Mb004, Kb512) With {.WRITE_DELAY_CYCLES = 30}) 'Verified 380 (5v) Why is this not: 01 A4? (PLCC32 and DIP32 tested)
            FlashDB.Add(New MFP_Flash("AMD AM29F010B", &H1, &H20, Mb001, Kb128)) 'Verified 380 (5v)
            FlashDB.Add(New MFP_Flash("MXIC MX29LV040C", &HC2, &H4F, Mb004, Kb512)) 'Verified 380 (3.3v)
            Dim W_W49F002U As New MFP_Flash("Winbond W49F002U", &HDA, &HB, Mb002) With {.WRITE_DELAY_CYCLES = 8} 'Verified 372
            W_W49F002U.AddSector(Mb001) 'Main Block
            W_W49F002U.AddSector(98304) 'Main Block
            W_W49F002U.AddSector(Kb064) 'Parameter Block
            W_W49F002U.AddSector(Kb064) 'Parameter Block
            W_W49F002U.AddSector(Kb128) 'Boot Block
            FlashDB.Add(W_W49F002U)
            Dim W29EE512 As New MFP_Flash("Winbond W29EE512", &HDA, &HC8, Kb512, Kb256) 'Verified 372
            W29EE512.ERASE_REQUIRED = False 'Each page is automatically erased
            W29EE512.PAGE_SIZE = 128
            W29EE512.WriteMode = MFP_ProgramMode.PageMode 'Write in 128 byte pages
            W29EE512.WRITE_DELAY_CYCLES = 2
            FlashDB.Add(W29EE512)
            Dim W29C010 As New MFP_Flash("Winbond W29C010", &HDA, &HC1, Mb001, Kb256)
            W29C010.ERASE_REQUIRED = False 'Each page is automatically erased
            W29C010.PAGE_SIZE = 128
            W29C010.WriteMode = MFP_ProgramMode.PageMode 'Write in 128 byte pages
            W29C010.WRITE_DELAY_CYCLES = 2
            FlashDB.Add(W29C010)
            Dim W29C020 As New MFP_Flash("Winbond W29C020", &HDA, &H45, Mb002, Kb256)
            W29C020.ERASE_REQUIRED = False 'Each page is automatically erased
            W29C020.PAGE_SIZE = 128
            W29C020.WriteMode = MFP_ProgramMode.PageMode 'Write in 128 byte pages
            W29C020.WRITE_DELAY_CYCLES = 2
            FlashDB.Add(W29C020)
            Dim W29C040 As New MFP_Flash("Winbond W29C040", &HDA, &H46, Mb004, Kb256)
            W29C040.ERASE_REQUIRED = False 'Each page is automatically erased
            W29C040.PAGE_SIZE = 256
            W29C040.WriteMode = MFP_ProgramMode.PageMode 'Write in 256 byte pages
            W29C040.WRITE_DELAY_CYCLES = 2
            FlashDB.Add(W29C040)
            'SST39SF512 / SST39SF010 / SST39SF020
            FlashDB.Add(New MFP_Flash("SST 39SF512", &HBF, &HB4, Kb512, Kb032)) '5v
            FlashDB.Add(New MFP_Flash("SST 39SF010", &HBF, &HB5, Mb001, Kb032)) '5v
            FlashDB.Add(New MFP_Flash("SST 39SF020", &HBF, &HB6, Mb002, Kb032)) '5v 'Verified 372
            FlashDB.Add(New MFP_Flash("SST 39LF010", &HBF, &HD5, Mb001, Kb032)) '3.3v
            FlashDB.Add(New MFP_Flash("SST 39LF020", &HBF, &HD6, Mb002, Kb032)) '3.3v
            FlashDB.Add(New MFP_Flash("SST 39LF040", &HBF, &HD7, Mb004, Kb032) With {.WRITE_DELAY_CYCLES = 0}) 'Verified Build 406 (3.3v)
            FlashDB.Add(New MFP_Flash("SST 39VF1681", &HBF, &HC8, Mb016, Kb512) With {.WRITE_DELAY_CYCLES = 0}) 'Verified Build 406 (3.3v / 8x ONLY)
            FlashDB.Add(New MFP_Flash("SST 39VF1682", &HBF, &HC9, Mb016, Kb512) With {.WRITE_DELAY_CYCLES = 0}) 'Verified Build 406 (3.3v / 8x ONLY)
            FlashDB.Add(New MFP_Flash("Atmel AT49F512", &H1F, &H3, Kb512, 65536)) 'Verified 372
            FlashDB.Add(New MFP_Flash("Atmel AT49F010", &H1F, &H17, Mb001, Mb001))
            FlashDB.Add(New MFP_Flash("Atmel AT49F020", &H1F, &HB, Mb002, Mb002))
            FlashDB.Add(New MFP_Flash("Atmel AT49F040", &H1F, &H13, Mb004, Mb004))
            FlashDB.Add(New MFP_Flash("Atmel AT49F040T", &H1F, &H12, Mb004, Mb004))

            Dim W29GL032CT As New MFP_Flash("Winbond W29GL032CT", &H1, &H7E1A01, Mb032) 'Verified 406
            W29GL032CT.WRITE_DELAY_CYCLES = 0
            W29GL032CT.AddSector(Kb512, 63)
            W29GL032CT.AddSector(Kb064, 8)
            FlashDB.Add(W29GL032CT)

            Dim W29GL032CB As New MFP_Flash("Winbond W29GL032CB", &H1, &H7E1A00, Mb032)
            W29GL032CB.WRITE_DELAY_CYCLES = 0
            W29GL032CB.AddSector(Kb064, 8)
            W29GL032CB.AddSector(Kb512, 63)
            FlashDB.Add(W29GL032CB)

            FlashDB.Add(New MFP_Flash("MXIC MX29LV160DT", &HC2, &HC4, Mb016, Kb512) With {.WRITE_DELAY_CYCLES = 0}) 'Verified 406
            FlashDB.Add(New MFP_Flash("MXIC MX29LV160DB", &HC2, &H49, Mb016, Kb512) With {.WRITE_DELAY_CYCLES = 0}) 'Slow read?

            'Cypress / Spansion
            'http://www.cypress.com/file/177976/download   S29GLxxxP (TSOP56)
            'http://www.cypress.com/file/219926/download   S29GLxxxS (TSOP56 only)
            FlashDB.Add(New MFP_Flash("Cypress S29GL032", &H1, &H7E1D00, Mb032, Kb512)) 'Bottom boot
            FlashDB.Add(New MFP_Flash("Cypress S29GL032", &H1, &H7E1D01, Mb032, Kb512)) 'Top-boot
            FlashDB.Add(New MFP_Flash("Cypress S29GL064", &H1, &H7E0C00, Mb064, Kb512)) 'Bottom boot
            FlashDB.Add(New MFP_Flash("Cypress S29GL064", &H1, &H7E0C01, Mb064, Kb512)) 'Top-boot
            FlashDB.Add(New MFP_Flash("Cypress S29GL128", &H1, &H7E2101, Mb128, Mb001))
            FlashDB.Add(New MFP_Flash("Cypress S29GL256", &H1, &H7E2201, Mb256, Mb001))
            FlashDB.Add(New MFP_Flash("Cypress S29GL512", &H1, &H7E2301, Mb512, Mb001))
            FlashDB.Add(New MFP_Flash("Cypress S29GL01G", &H1, &H7E2801, Gb001, Mb001))

            'Micron / ST Microelectronics
            Dim M29W640GT As New MFP_Flash("Micron M29W640GT", &H20, &H7E1001, Mb064) 'Top boot
            M29W640GT.WriteMode = MFP_ProgramMode.BypassMode
            M29W640GT.AddSector(Kb512, 127) '64K Sectors
            M29W640GT.AddSector(Kb064, 8)
            FlashDB.Add(M29W640GT)

            Dim M29W640GB As New MFP_Flash("Micron M29W640GB", &H20, &H7E1000, Mb064, Kb512) 'Bottom boot (verified 382)
            M29W640GB.WriteMode = MFP_ProgramMode.BypassMode
            FlashDB.Add(M29W640GB)

            Dim M29W256 As New MFP_Flash("Micron M29W256G", &H20, &H7E2201, Mb256, Mb001) ' TSOP56
            M29W256.WriteMode = MFP_ProgramMode.BypassMode
            FlashDB.Add(M29W256)

            Dim M29W160EB As New MFP_Flash("Micron M29W160EB", &H20, &H49, Mb016)
            M29W160EB.WriteMode = MFP_ProgramMode.BypassMode
            M29W160EB.AddSector(Kb128, 1) '16KByte
            M29W160EB.AddSector(Kb064, 2) '8KByte
            M29W160EB.AddSector(Kb256, 1) '32KByte
            M29W160EB.AddSector(Kb512, 31) '64KByte
            FlashDB.Add(M29W160EB)

            Dim M29W160ET As New MFP_Flash("Micron M29W160ET", &H20, &HC4, Mb016)
            M29W160ET.WriteMode = MFP_ProgramMode.BypassMode
            M29W160ET.AddSector(Kb512, 31) '64KByte
            M29W160ET.AddSector(Kb256, 1) '32KByte
            M29W160ET.AddSector(Kb064, 2) '8KByte
            M29W160ET.AddSector(Kb128, 1) '16KByte
            FlashDB.Add(M29W160ET)

            Dim LHF00L15 As New MFP_Flash("Sharp LHF00L15", &HB0, &HA1, Mb032) 'Verified 382
            LHF00L15.WriteMode = MFP_ProgramMode.IntelSharp
            LHF00L15.SectorEraseMode = MFP_SectorEraseMode.IntelSharp
            LHF00L15.WRITE_DELAY_CYCLES = 0 'Not needed
            LHF00L15.ChipEraseMode = EraseMethod.BySector 'Chip Erase does not work?
            LHF00L15.AddSector(Kb064, 8)
            LHF00L15.AddSector(Kb512, 1)
            LHF00L15.AddSector(Mb001, 31)
            FlashDB.Add(LHF00L15)

            'Also supports Cypress S29AL016J
            Dim AM29LV160BB As New MFP_Flash("AMD AM29LV160BB", &H1, &H49, Mb016) 'Verified build 406
            AM29LV160BB.WriteMode = MFP_ProgramMode.BypassMode
            AM29LV160BB.WRITE_DELAY_CYCLES = 25
            AM29LV160BB.AddSector(Kb128, 1) '16KByte
            AM29LV160BB.AddSector(Kb064, 2) '8KByte
            AM29LV160BB.AddSector(Kb256, 1) '32KByte
            AM29LV160BB.AddSector(Kb512, 31) '64KByte
            FlashDB.Add(AM29LV160BB)

            Dim AM29LV160TB As New MFP_Flash("AMD AM29LV160TB", &H1, &HC4, Mb016)
            AM29LV160TB.WriteMode = MFP_ProgramMode.BypassMode
            AM29LV160TB.WRITE_DELAY_CYCLES = 25
            AM29LV160TB.AddSector(Kb512, 31) '64KByte
            AM29LV160TB.AddSector(Kb256, 1) '32KByte
            AM29LV160TB.AddSector(Kb064, 2) '8KByte
            AM29LV160TB.AddSector(Kb128, 1) '16KByte
            FlashDB.Add(AM29LV160TB)

            Dim MX29L3211 As New MFP_Flash("MXIC MX29L3211", &HC2, &HF9, Mb032, Mb001) '32-mbit (tested with SO-44)
            MX29L3211.PAGE_SIZE = 256
            MX29L3211.RESET_REQUIRED = True
            MX29L3211.WriteMode = MFP_ProgramMode.PageMode
            FlashDB.Add(MX29L3211)

        End Sub

        Private Sub NAND_Database()
            'Good ID list at: http://www.usbdev.ru/databases/flashlist/flcbm93e98s98p98e/
            'And : http://www.linux-mtd.infradead.org/nand-data/nanddata.html
            'And: http://aitendo2.sakura.ne.jp/aitendo_data/product_img2/product_img/aitendo-kit/USB-MEM/MW8209/Flash_suport_091120.pdf

            'Micron SLC 8x NAND devices
            FlashDB.Add(New NAND_Flash("Micron NAND128W3A", &H20, &H732073, Mb128, 512, 16, Kb128))
            FlashDB.Add(New NAND_Flash("Micron NAND256R3A", &H20, &H352035, Mb256, 512, 16, Kb128))
            FlashDB.Add(New NAND_Flash("Micron NAND256W3A", &H20, &H752075, Mb256, 512, 16, Kb128))
            FlashDB.Add(New NAND_Flash("Micron NAND512R3A", &H20, &H362036, Mb512, 512, 16, Kb128))
            FlashDB.Add(New NAND_Flash("Micron NAND512W3A", &H20, &H762076, Mb512, 512, 16, Kb128))
            FlashDB.Add(New NAND_Flash("Micron NAND01GR3A", &H20, &H392039, Gb001, 512, 16, Kb128))
            FlashDB.Add(New NAND_Flash("Micron NAND01GW3A", &H20, &H792079, Gb001, 512, 16, Kb128))

            FlashDB.Add(New NAND_Flash("Micron MT29F2G08AAB", &H2C, &HDA0015, Gb002, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Micron MT29F4G08BAB", &H2C, &HDC0015, Gb004, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Micron MT29F1G08ABAEA", &H2C, &HF1809504UI, Gb001, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Micron MT29F1G08ABBEA", &H2C, &HA1801504UI, Gb001, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Micron MT29F2G08ABBFA", &H2C, &HAA901504UI, Gb002, 2048, 224, Mb001))
            FlashDB.Add(New NAND_Flash("Micron MT29F2G08ABAFA", &H2C, &HDA909504UI, Gb002, 2048, 224, Mb001))
            FlashDB.Add(New NAND_Flash("Micron MT29F4G08AAA", &H2C, &HDC909554UI, Gb004, 2048, 64, Mb001)) '3v
            FlashDB.Add(New NAND_Flash("Micron MT29F8G08BAA", &H2C, &HD3D19558UI, Gb008, 2048, 64, Mb001)) '3v
            'Toshiba SLC 8x NAND devices
            FlashDB.Add(New NAND_Flash("Toshiba TC58NVG0S3HTA00", &H98, &HF1801572UI, Gb001, 2048, 128, Mb001))
            FlashDB.Add(New NAND_Flash("Toshiba TC58NVG0S3HTAI0", &H98, &HF1801572UI, Gb001, 2048, 128, Mb001))
            FlashDB.Add(New NAND_Flash("Toshiba TC58NVG1S3HTA00", &H98, &HDA901576UI, Gb002, 2048, 128, Mb001))
            FlashDB.Add(New NAND_Flash("Toshiba TC58NVG1S3HTAI0", &H98, &HDA901576UI, Gb002, 2048, 128, Mb001))
            FlashDB.Add(New NAND_Flash("Toshiba TC58NVG2S0HTA00", &H98, &HDC902676UI, Gb004, 4096, 256, Mb001)) 'CHECK
            FlashDB.Add(New NAND_Flash("Toshiba TC58NVG2S0HTAI0", &H98, &HDC902676UI, Gb004, 4096, 256, Mb001))
            FlashDB.Add(New NAND_Flash("Toshiba TH58NVG3S0HTA00", &H98, &HD3912676UI, Gb008, 4096, 256, Mb001))
            FlashDB.Add(New NAND_Flash("Toshiba TH58NVG3S0HTAI0", &H98, &HD3912676UI, Gb008, 2048, 128, Mb001))
            'Winbond SLC 8x NAND devices
            FlashDB.Add(New NAND_Flash("Winbond W29N01GV", &HEF, &HF1809500UI, Gb001, 2048, 64, Mb001)) '3v
            FlashDB.Add(New NAND_Flash("Winbond W29N02GV", &HEF, &HDA909504UI, Gb002, 2048, 64, Mb001)) '3v <-- verified
            'Macronix SLC 8x NAND devices
            FlashDB.Add(New NAND_Flash("MXIC MX30LF1208AA", &HC2, &HF0801D, Mb512, 2048, 64, Mb001)) '3v
            FlashDB.Add(New NAND_Flash("MXIC MX30LF1GE8AB", &HC2, &HF1809582UI, Gb001, 2048, 64, Mb001)) '3v
            FlashDB.Add(New NAND_Flash("MXIC MX30UF1G18AC", &HC2, &HA1801502UI, Gb001, 2048, 64, Mb001)) '1.8v
            FlashDB.Add(New NAND_Flash("MXIC MX30LF1G18AC", &HC2, &HF1809502UI, Gb001, 2048, 64, Mb001)) '3v
            FlashDB.Add(New NAND_Flash("MXIC MX30LF1G08AA", &HC2, &HF1801D, Gb001, 2048, 64, Mb001)) '3v Verified
            FlashDB.Add(New NAND_Flash("MXIC MX30LF2G18AC", &HC2, &HDA909506UI, Gb002, 2048, 64, Mb001)) '3v
            FlashDB.Add(New NAND_Flash("MXIC MX30UF2G18AC", &HC2, &HAA901506UI, Gb002, 2048, 64, Mb001)) '1.8v
            FlashDB.Add(New NAND_Flash("MXIC MX30LF2G28AB", &HC2, &HDA909507UI, Gb002, 2048, 64, Mb001)) '3v
            FlashDB.Add(New NAND_Flash("MXIC MX30LF2GE8AB", &HC2, &HDA909586UI, Gb002, 2048, 64, Mb001)) '3v
            FlashDB.Add(New NAND_Flash("MXIC MX30UF2G18AB", &HC2, &HBA905506UI, Gb002, 2048, 64, Mb001)) '1.8v
            FlashDB.Add(New NAND_Flash("MXIC MX30UF2G28AB", &HC2, &HAA901507UI, Gb002, 2048, 64, Mb001)) '1.8v
            FlashDB.Add(New NAND_Flash("MXIC MX30LF4G18AC", &HC2, &HDC909556UI, Gb004, 2048, 64, Mb001)) '3v
            FlashDB.Add(New NAND_Flash("MXIC MX30UF4G18AB", &HC2, &HAC901556UI, Gb004, 2048, 64, Mb001)) '1.8v
            FlashDB.Add(New NAND_Flash("MXIC MX30LF4G28AB", &HC2, &HDC909507UI, Gb004, 2048, 64, Mb001)) '3v
            FlashDB.Add(New NAND_Flash("MXIC MX30LF4GE8AB", &HC2, &HDC9095D6UI, Gb004, 2048, 64, Mb001)) '3v
            FlashDB.Add(New NAND_Flash("MXIC MX30UF4G28AB", &HC2, &HAC901557UI, Gb004, 2048, 64, Mb001)) '1.8v
            FlashDB.Add(New NAND_Flash("MXIC MX60LF8G18AC", &HC2, &HD3D1955AUI, Gb008, 2048, 64, Mb001)) '3v
            FlashDB.Add(New NAND_Flash("MXIC MX60LF8G28AB", &HC2, &HD3D1955BUI, Gb008, 2048, 64, Mb001))
            'Samsung SLC x8 NAND devices
            FlashDB.Add(New NAND_Flash("Samsung K9F1G08U0D", &HEC, &HF1001540UI, Gb001, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Samsung K9F1G08U0B", &HEC, &HF1009540UI, Gb001, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Samsung K9F1G08X0", &HEC, &HF1009540UI, Gb001, 2048, 64, Mb001)) 'K9F1G08U0C K9F1G08B0C K9F1G08U0B
            FlashDB.Add(New NAND_Flash("Samsung K9F2G08X0", &HEC, &HDA101544UI, Gb002, 2048, 64, Mb001)) 'K9F2G08B0B K9F2G08U0B K9F2G08U0A K9F2G08U0C
            FlashDB.Add(New NAND_Flash("Samsung K9F2G08U0C", &HEC, &HDA109544UI, Gb002, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Samsung K9F2G08U0M", &HEC, &HDA8015UI, Gb004, 2048, 64, Mb001)) 'K9K4G08U1M = 2X DIE
            'Hynix SLC x8 devices
            FlashDB.Add(New NAND_Flash("Hynix HY27SS08561A", &HAD, &H35UI, Mb256, 512, 16, Mb001))
            FlashDB.Add(New NAND_Flash("Hynix HY27US08561A", &HAD, &H75UI, Mb256, 512, 16, Mb001))
            FlashDB.Add(New NAND_Flash("Hynix HY27US0812(1/2)B", &HAD, &H76UI, Mb512, 512, 16, Mb001))
            FlashDB.Add(New NAND_Flash("Hynix H27U1G8F2B", &HAD, &HF1001D, Gb001, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Hynix HY27UF081G2M", &HAD, &HADF10015UI, Gb001, 2048, 64, Mb001)) '0xADF1XX15
            FlashDB.Add(New NAND_Flash("Hynix HY27US081G1M", &HAD, &H79A500UI, Gb001, 512, 16, Mb001))
            FlashDB.Add(New NAND_Flash("Hynix HY27SF081G2M", &HAD, &HA10015UI, Gb001, 2048, 64, Mb001)) 'ADA1XX15
            FlashDB.Add(New NAND_Flash("Hynix HY27UF082G2B", &HAD, &HDA109544UI, Gb002, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Hynix HY27UF082G2A", &HAD, &HDA801D00UI, Gb002, 2048, 64, Mb001))
            'Spansion SLC 34 series
            FlashDB.Add(New NAND_Flash("Cypress S34ML01G1", &H1, &HF1001DUI, Gb001, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Cypress S34ML02G1", &H1, &HDA9095UI, Gb002, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Cypress S34ML04G1", &H1, &HDC9095UI, Gb004, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Cypress S34ML01G2", &H1, &HF1801DUI, Gb001, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Cypress S34ML02G2", &H1, &HDA9095UI, Gb002, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Cypress S34ML04G2", &H1, &HDC9095UI, Gb004, 2048, 64, Mb001))

            'MLC and 8LC devices:
            'Database.Add(New MPFlash("SanDisk SDTNPNAHEM-008G", &H98, &H809272UI, MB8Gb * 8, 8192, 1024, MB032))
            'FlashDB.Add(New NAND_Flash("Toshiba TC58NVG3D4CTGI0", &H98, &H8095D6, Gb008, 4096, 256, Mb001)) 'We have this one
            'Samsung K9WG08U1M and this one

            '0x98D385A5
            '2kB / 256kB

            'Dim test As New NAND_Flash("Toshiba TC58NVG3D4CTGI0", &H98, &H8095D6, Gb008, 2048, 64, Mb002)
            'FlashDB.Add(test)

        End Sub

        Public Function FindDevice(ByVal MFG As Byte, ByVal PART As UInt32, Optional DEVICE As MemoryType = MemoryType.UNSPECIFIED) As Device
            If (DEVICE = MemoryType.UNSPECIFIED) Then 'Search all devices
                For Each flash As Device In FlashDB
                    If DatabaseMatch(flash, MFG, PART) Then Return flash
                Next
            ElseIf DEVICE = MemoryType.PARALLEL_NOR Then
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                        If DatabaseMatch(flash, MFG, PART) Then Return flash
                    End If
                Next
            ElseIf DEVICE = MemoryType.SERIAL_NOR Then
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.SERIAL_NOR Then
                        If DatabaseMatch(flash, MFG, PART) Then Return flash
                    End If
                Next
            ElseIf DEVICE = MemoryType.SLC_NAND Then
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.SLC_NAND Then
                        If DatabaseMatch(flash, MFG, PART) Then Return flash
                    End If
                Next
            ElseIf DEVICE = MemoryType.JTAG Then
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.JTAG Then
                        If DatabaseMatch(flash, MFG, PART) Then Return flash
                    End If
                Next
            End If
            Return Nothing 'Not found
        End Function
        'Searches the CFI database to match devices running in x8 mode
        Public Function FindDevice_CFI_x8(ByVal MFG As Byte, ByVal PART As UInt32) As Device
            For Each flash In FlashDB
                If flash.GetType Is GetType(MFP_Flash) Then
                    If (flash.MFG_CODE = MFG) Then
                        If (PART >> 24) = (flash.PART_CODE And 255) Then Return flash
                    End If
                End If
            Next
            Return Nothing
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
            End If
            Return Count
        End Function

        Private Function DatabaseMatch(ByVal flash As Device, ByVal MFG As Byte, ByVal PART As UInt32) As Boolean
            If (flash.MFG_CODE = MFG) Then
                If flash.PART_CODE = PART Then Return True 'An excact match
                Dim PART_TOFIND As UInt32 = flash.PART_CODE
                If PART_TOFIND = 0 Or PART = 0 Then Return False
                Dim BytesToMatch As Integer = 4
                Do Until (Not (PART_TOFIND And &HFF000000) = 0)
                    PART_TOFIND = (PART_TOFIND << 8)
                    BytesToMatch -= 1
                Loop
                Do Until (Not (PART And &HFF000000) = 0)
                    PART = (PART << 8)
                Loop
                Dim b1() As Byte = Utilities.Bytes.FromUInt32(PART_TOFIND, False)
                Dim b2() As Byte = Utilities.Bytes.FromUInt32(PART, False)
                For i = 1 To BytesToMatch
                    If (Not b1(i - 1) = b2(i - 1)) Then Return False
                Next
                Return True 'Match
            End If
            Return False
        End Function

        Public Sub WriteDatabaseToFile()
            Dim f As New List(Of String)
            For Each s As SPI_FLASH In FlashDB
                f.Add(s.NAME & " (" & (s.FLASH_SIZE / 131072) & "Mbit)")
            Next
            Utilities.FileIO.WriteFile(f.ToArray, "d:\spi_flash_list.txt")
        End Sub

    End Class


End Namespace