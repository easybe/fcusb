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
        SERIAL_I2C 'I2C EEPROMs
        SLC_NAND 'NAND devices
        VOLATILE 'RAM
        'MLC_NAND
        'TLC_NAND
        'SERIAL_NAND
    End Enum

    Public Enum FlashArea As Byte
        Main = 0 'Data area
        OOB = 1 'Extended area
        All = 2 'All data
        NotSpecified = 255
    End Enum

    Public Enum MFP_PROG
        Standard 'Use the standard sequence that chip id detected
        PageMode 'Writes an entire page of data (128 bytes etc.)
        BypassMode 'Writes 64 bytes using ByPass sequence
        IntelSharp 'Writes data (SA=0x40;SA=DATA;SR.7), erases sectors (SA=0x50;SA=0x60;SA=0xD0,SR.7,SA=0x20;SA=0xD0,SR.7)
        IntelBuffer 'Use Write-To-Buffer mode (x16 only)
    End Enum

    Public Enum MFP_BLKLAYOUT
        Four_Top
        Two_Top
        Four_Btm
        Two_Btm
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
    End Enum

    Public Enum SPI_ProgramMode
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
        ReadOnly Property FLASH_SIZE As UInt32 'Size of this flash device (without spare area)
        ReadOnly Property MFG_CODE As Byte 'The manufaturer byte ID
        ReadOnly Property ID1 As UInt16
        Property ID2 As UInt16
        ReadOnly Property PAGE_SIZE As UInt32 'Size of the pages
        ReadOnly Property SECTOR_COUNT As UInt32 'Total number of blocks or sectors this flash device has
        Property ERASE_REQUIRED As Boolean 'Indicates that the sector/block must be erased prior to writing

    End Interface

    Public Class MFP_Flash
        Implements Device
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property ID1 As UInt16 Implements Device.ID1
        Public Property ID2 As UInt16 Implements Device.ID2
        Public ReadOnly Property FLASH_TYPE As MemoryType Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As UInt32 Implements Device.FLASH_SIZE
        Public Property PAGE_SIZE As UInt32 Implements Device.PAGE_SIZE 'Only used for WRITE_PAGE mode of certain flash devices
        Public Property ERASE_REQUIRED As Boolean Implements Device.ERASE_REQUIRED
        Public Property RESET_REQUIRED As Boolean = False 'Set to TRUE to do a RESET after SE, CE, WR
        Public Property WriteMode As MFP_PROG = MFP_PROG.Standard 'This indicates the perfered programing method
        Public Property WRITE_HARDWARE_DELAY As UInt16 = 10 'Number of hardware uS to wait between write operations
        Public Property WRITE_SOFTWARE_DELAY As UInt16 = 100 'Number of software ms to wait between write operations

        Sub New(f_name As String, MFG As Byte, ID1 As UInt16, f_size As UInt32)
            FLASH_TYPE = MemoryType.PARALLEL_NOR
            Me.NAME = f_name
            Me.FLASH_SIZE = f_size
            Me.MFG_CODE = MFG
            Me.ID1 = ID1
            Me.ERASE_REQUIRED = True
        End Sub

        Sub New(f_name As String, MFG As Byte, ID1 As UInt16, f_size As UInt32, uniform_block As UInt32)
            FLASH_TYPE = MemoryType.PARALLEL_NOR
            Me.NAME = f_name
            Me.FLASH_SIZE = f_size
            Me.MFG_CODE = MFG
            Me.ID1 = ID1
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

        Public ReadOnly Property SECTOR_COUNT As UInt32 Implements Device.SECTOR_COUNT
            Get
                Return SectorList.Count
            End Get
        End Property

    End Class

    Public Class SPI_FLASH
        Implements Device
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property ID1 As UInt16 Implements Device.ID1
        Public Property ID2 As UInt16 Implements Device.ID2
        Public ReadOnly Property FLASH_TYPE As MemoryType Implements Device.FLASH_TYPE
        Public Property FLASH_SIZE As UInt32 Implements Device.FLASH_SIZE
        Public Property ERASE_REQUIRED As Boolean Implements Device.ERASE_REQUIRED
        'These are properties unique to SPI devices
        Public Property ADDRESSBITS As UInt32 = 24 'Number of bits the address space takes up (16/24/32)
        Public Property PAGE_COUNT As UInt32 'The total number of pages this flash contains
        Public Property QUAD As SPI_QUADMODE = SPI_QUADMODE.NotSupported 'SQI mode
        Public Property ProgramMode As SPI_ProgramMode
        Public Property STACKED_DIES As UInt32 = 1 'If device has more than one die, set this value
        Public Property SEND_4BYTE As Boolean = False 'Set to True to send the EN4B
        Public Property SEND_RDFS As Boolean = False 'Set to True to read the flag status register
        Public Property ERASE_SIZE As UInt32 = &H10000 'Number of bytes per page that are erased(typically 64KB)
        Public Property VENDOR_SPECIFIC As VENDOR_FEATURE = VENDOR_FEATURE.NotSupported 'Indicates we can load a unique vendor specific tab
        Public Property DUMMY_CLOCK_CYCLES As Integer = 8 'Number of dummy bytes to use when using FAST READ (0x0B OP COMMAND)
        Public Property CHIP_ERASE As EraseMethod = EraseMethod.Standard 'How to erase the entire device
        Public Property SEND_EWSR As Boolean = False 'Set to TRUE to write the enable write status-register
        Public Property PAGE_SIZE As UInt32 Implements Device.PAGE_SIZE  'Number of bytes per page
        Public Property PAGE_SIZE_EXTENDED As UInt32 'Number of bytes in the extended page
        Public Property EXTENDED_MODE As Boolean = False 'True if this device has extended bytes (used by AT45 devices)
        Public Property EEPROM As Boolean = False 'For SPI EEPROM devices

        Public OP_COMMANDS As New SPI_Command_DEF 'Contains a list of op-codes used to read/write/erase

        'FlashName, Size, and Page Size
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

        Sub New(ByVal f_name As String, ByVal f_size As UInt32, ByVal MFG As Byte, ByVal ID1 As UInt16, ByVal ERASECMD As Byte, ByVal ERASESIZE As UInt32, ByVal READCMD As Byte, ByVal WRITECMD As Byte)
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

        Public ReadOnly Property SECTOR_COUNT As UInt32 Implements Device.SECTOR_COUNT
            Get
                If Me.ERASE_REQUIRED Then
                    Return (FLASH_SIZE / ERASE_SIZE)
                Else
                    Return 1 'EEPROM do not have sectors
                End If
            End Get
        End Property

    End Class

    Public Class NAND_Flash
        Implements Device
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property ID1 As UInt16 Implements Device.ID1
        Public Property ID2 As UInt16 Implements Device.ID2
        Public ReadOnly Property FLASH_TYPE As MemoryType Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As UInt32 Implements Device.FLASH_SIZE
        Public ReadOnly Property PAGE_SIZE As UInt32 Implements Device.PAGE_SIZE 'Total number of bytes per page (not including OOB)
        Public Property ERASE_REQUIRED As Boolean Implements Device.ERASE_REQUIRED
        'These are properties unique to NAND memory:
        Public Property PAGE_SIZE_EXTENDED As UInt16 'Total number of bytes per page (with spare/extended area)
        Public Property BLOCK_SIZE As UInt32 'Number of bytes per block (not including extended pages)
        'Public Property ADDR_MODE As AddressingMode 'This indicates how to write the address table

        Sub New(FlashName As String, MFG As Byte, ID As UInt32, m_size As UInt32, PageSize As UInt16, SpareSize As UInt16, BlockSize As UInt32, Optional BADBLOCK As BadBlockMarker = BadBlockMarker.noneyet)
            Me.NAME = FlashName
            Me.FLASH_TYPE = MemoryType.SLC_NAND
            Me.PAGE_SIZE = PageSize 'Does not include extended / spare pages
            Me.PAGE_SIZE_EXTENDED = (PageSize + SpareSize) 'This is the entire size of the page
            Me.MFG_CODE = MFG
            While ((ID And &HFF000000UI) = 0)
                ID = (ID << 8)
            End While
            Me.ID1 = (ID >> 16)
            Me.ID2 = (ID And &HFFFF)
            Me.FLASH_SIZE = m_size 'Does not include extended /spare areas
            Me.BLOCK_SIZE = BlockSize
            Me.ERASE_REQUIRED = True
        End Sub

        Public ReadOnly Property SECTOR_COUNT As UInt32 Implements Device.SECTOR_COUNT
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
            NAND_Database() 'Adds all of the NAND compatible devices

            'Add device specific features
            Dim MT25QL02GC As SPI_FLASH = FindDevice(&H20, &HBA22, 0, False, MemoryType.SERIAL_NOR)
            MT25QL02GC.QUAD = SPI_QUADMODE.all_quadio
            MT25QL02GC.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron 'Adds the non-vol tab to the GUI
            MT25QL02GC.SEND_RDFS = True 'Will read the flag-status register after a erase/programer opertion
            MT25QL02GC.CHIP_ERASE = EraseMethod.Micron   'Will erase all of the sectors instead
            Dim N25Q00A As SPI_FLASH = FindDevice(&H20, &HBA21, 0, False, MemoryType.SERIAL_NOR)
            N25Q00A.QUAD = SPI_QUADMODE.all_quadio
            N25Q00A.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron 'Adds the non-vol tab to the GUI
            N25Q00A.SEND_RDFS = True 'Will read the flag-status register after a erase/programer opertion
            N25Q00A.CHIP_ERASE = EraseMethod.Micron  'Will erase all of the sectors instead
            Dim N25Q512 As SPI_FLASH = FindDevice(&H20, &HBA20, 0, False, MemoryType.SERIAL_NOR)
            N25Q512.QUAD = SPI_QUADMODE.all_quadio
            N25Q512.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron 'Adds the non-vol tab to the GUI
            N25Q512.SEND_RDFS = True 'Will read the flag-status register after a erase/programer opertion
            N25Q512.CHIP_ERASE = EraseMethod.Micron 'Will erase all of the sectors instead
            Dim N25Q256 As SPI_FLASH = FindDevice(&H20, &HBA19, 0, False, MemoryType.SERIAL_NOR)
            N25Q256.QUAD = SPI_QUADMODE.all_quadio
            N25Q256.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron 'Adds the non-vol tab to the GUI
            N25Q256.SEND_RDFS = True 'Will read the flag-status register after a erase/programer opertion

            Dim S25FL116K As SPI_FLASH = FindDevice(&H1, &H4015, 0, False, MemoryType.SERIAL_NOR)
            S25FL116K.QUAD = SPI_QUADMODE.all_quadio
            S25FL116K.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion

            Dim S25FL132K As SPI_FLASH = FindDevice(&H1, &H4016, 0, False, MemoryType.SERIAL_NOR)
            S25FL132K.QUAD = SPI_QUADMODE.all_quadio
            S25FL132K.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion

            Dim S25FL164K As SPI_FLASH = FindDevice(&H1, &H4017, 0, False, MemoryType.SERIAL_NOR)
            S25FL164K.QUAD = SPI_QUADMODE.all_quadio
            S25FL164K.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion

            Dim MX29L3211 As MFP_Flash = FindDevice(&HC2, &HF9, 0, False, MemoryType.PARALLEL_NOR)
            MX29L3211.WRITE_HARDWARE_DELAY = 100 'Wait 100us between page writes
            MX29L3211.WRITE_SOFTWARE_DELAY = 70 'Wait 70ns between page writes


            Dim M29W256G As MFP_Flash = FindDevice(&H20, &H227E, &H2201, False, MemoryType.PARALLEL_NOR)
            M29W256G.WRITE_HARDWARE_DELAY = 8 'Verified
            M29W256G.WRITE_SOFTWARE_DELAY = 0

            Dim AM29LV160BT As MFP_Flash = FindDevice(&H1, &H22C4, 0, False, MemoryType.PARALLEL_NOR)
            AM29LV160BT.WRITE_HARDWARE_DELAY = 5
            AM29LV160BT.WRITE_SOFTWARE_DELAY = 0

            Dim MX29LV160DT As MFP_Flash = FindDevice(&HC2, &H22C4, 0, False, MemoryType.PARALLEL_NOR)
            MX29LV160DT.WRITE_HARDWARE_DELAY = 5
            MX29LV160DT.WRITE_SOFTWARE_DELAY = 0


            'CreateHtmlCatalog(MemoryType.SERIAL_NOR, 3, "d: \spi_database.html")
            'CreateHtmlCatalog(MemoryType.PARALLEL_NOR, 3, "d:\mpf_database.html")
            'CreateHtmlCatalog(MemoryType.SLC_NAND, 3, "d:\nand_database.html")
        End Sub

        Private Sub SPI_Database()
            'FlashDB.Add(New SPI_Flash("Generic 1Mbit", MB001, &H10, &H1010)) 'ST M25P10-A
            'Adesto AT25 Series (formely Atmel)
            FlashDB.Add(New SPI_FLASH("Adesto AT25DF641", Mb064, &H1F, &H4800)) 'Confirmed (build 350)
            FlashDB.Add(New SPI_FLASH("Adesto AT25DF321S", Mb032, &H1F, &H4701))
            FlashDB.Add(New SPI_FLASH("Adesto AT25DF321", Mb032, &H1F, &H4700))
            FlashDB.Add(New SPI_FLASH("Adesto AT25DF161", Mb016, &H1F, &H4602))
            FlashDB.Add(New SPI_FLASH("Adesto AT25DF081", Mb008, &H1F, &H4502))
            FlashDB.Add(New SPI_FLASH("Adesto AT25DF021", Mb002, &H1F, &H4300))
            'Adesto AT26 Series (formely Atmel)
            FlashDB.Add(New SPI_FLASH("Adesto AT26DF321", Mb032, &H1F, &H4700))
            FlashDB.Add(New SPI_FLASH("Adesto AT26DF161", Mb016, &H1F, &H4600))
            FlashDB.Add(New SPI_FLASH("Adesto AT26DF161A", Mb016, &H1F, &H4601))
            FlashDB.Add(New SPI_FLASH("Adesto AT26DF081A", Mb008, &H1F, &H4501))
            'Adesto AT45 Series (formely Atmel)
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
            'Adesto AT25 series (formely Atmel)
            FlashDB.Add(New SPI_FLASH("Adesto AT25SF161", Mb016, &H1F, &H8601))
            FlashDB.Add(New SPI_FLASH("Adesto AT25SF081", Mb008, &H1F, &H8501))
            FlashDB.Add(New SPI_FLASH("Adesto AT25SF041", Mb004, &H1F, &H8401))
            FlashDB.Add(New SPI_FLASH("Adesto AT25XV041", Mb004, &H1F, &H4402))
            FlashDB.Add(New SPI_FLASH("Adesto AT25XV021", Mb002, &H1F, &H4301))
            'Cypress 25FL Series (formely Spansion)
            FlashDB.Add(New SPI_FLASH("Cypress S70FL01GS", Gb001, &H1, &H221, &HDC, &H40000, &H13, &H12))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL512S", Mb512, &H1, &H220, &HDC, &H40000, &H13, &H12))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL256S", Mb256, &H1, &H219, &HDC, &H40000, &H13, &H12) With {.ID2 = &H4D00}) 'Confirmed (build 371)
            FlashDB.Add(New SPI_FLASH("Cypress S25FL256S", Mb256, &H1, &H219, &HDC, &H10000, &H13, &H12) With {.ID2 = &H4D01})
            FlashDB.Add(New SPI_FLASH("Cypress S70FL256P", Mb256, &H1, &H2018, &HDC, &H40000, &H13, &H12) With {.ID2 = &H4D00}) '2x S25FL128S DIES (pin 6 is second CS)
            FlashDB.Add(New SPI_FLASH("Cypress S70FL256P", Mb256, &H1, &H2018, &HDC, &H10000, &H13, &H12) With {.ID2 = &H4D01}) '2x S25FL128S DIES (pin 6 is second CS)
            FlashDB.Add(New SPI_FLASH("Cypress S25FL128S", Mb128, &H1, &H2018, &HD8, &H40000, &H3, &H2) With {.ID2 = &H4D00})
            FlashDB.Add(New SPI_FLASH("Cypress S25FL128S", Mb128, &H1, &H2018, &HD8, &H10000, &H3, &H2) With {.ID2 = &H4D01}) 'Chip Vault
            FlashDB.Add(New SPI_FLASH("Cypress S25FL129P", Mb128, &H1, &H2018, &HD8, &H40000, &H3, &H2) With {.ID2 = &H4D00})
            FlashDB.Add(New SPI_FLASH("Cypress S25FL129P", Mb128, &H1, &H2018, &HD8, &H10000, &H3, &H2) With {.ID2 = &H4D01})
            FlashDB.Add(New SPI_FLASH("Cypress S25FL128P", Mb128, &H1, &H2018, &HD8, &H40000, &H3, &H2) With {.ID2 = &H300})
            FlashDB.Add(New SPI_FLASH("Cypress S25FL128P", Mb128, &H1, &H2018, &HD8, &H10000, &H3, &H2) With {.ID2 = &H301})
            FlashDB.Add(New SPI_FLASH("Cypress S25FL064L", Mb064, &H1, &H6017)) 'Added build 427
            FlashDB.Add(New SPI_FLASH("Cypress S25FL064", Mb064, &H1, &H216))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL032", Mb032, &H1, &H215))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL016A", Mb016, &H1, &H214))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL008A", Mb008, &H1, &H213))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL040A", Mb004, &H1, &H212))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL164K", Mb064, &H1, &H4017))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL132K", Mb032, &H1, &H4016))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL216K", Mb016, &H1, &H4015)) 'Uses the same ID as S25FL116K (might support 3 byte ID)
            FlashDB.Add(New SPI_FLASH("Cypress S25FL116K", Mb016, &H1, &H4015))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL208K", Mb008, &H1, &H4014))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL204K", Mb004, &H1, &H4013))
            FlashDB.Add(New SPI_FLASH("Cypress S25FL004A", Mb004, &H1, &H212))
            'Micron (ST)
            FlashDB.Add(New SPI_FLASH("Micron MT25QL02GC", Gb002, &H20, &HBA22) With {.SEND_4BYTE = True})
            FlashDB.Add(New SPI_FLASH("Micron N25Q00A", Gb001, &H20, &HBA21) With {.SEND_4BYTE = True})
            FlashDB.Add(New SPI_FLASH("Micron N25Q512A", Mb512, &H20, &HBA20) With {.SEND_4BYTE = True})
            FlashDB.Add(New SPI_FLASH("Micron N25Q256", Mb256, &H20, &HBA19) With {.SEND_4BYTE = True})
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
            FlashDB.Add(New SPI_FLASH("Winbond W25M512JV", Mb512, &HEF, &H7119) With {.SEND_4BYTE = True, .STACKED_DIES = 2}) 'Confirmed working (7/13/17)
            FlashDB.Add(New SPI_FLASH("Winbond W25Q512", Mb512, &HEF, &H4020) With {.SEND_4BYTE = True})
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
            FlashDB.Add(New SPI_FLASH("MXIC MX25U12873F", Mb128, &HC2, &H2538)) '1.8V SQI
            FlashDB.Add(New SPI_FLASH("MXIC MX25L6455E", Mb064, &HC2, &H2617)) 'Added Build 372
            FlashDB.Add(New SPI_FLASH("MXIC MX25L640", Mb064, &HC2, &H2017))
            FlashDB.Add(New SPI_FLASH("MXIC MX25L320", Mb032, &HC2, &H2016)) 'Confirmed Build 350
            FlashDB.Add(New SPI_FLASH("MXIC MX25L3205D", Mb032, &HC2, &H20FF))
            FlashDB.Add(New SPI_FLASH("MXIC MX25L323", Mb032, &HC2, &H5E16))
            FlashDB.Add(New SPI_FLASH("MXIC MX25U3235F", Mb032, &HC2, &H2536)) '1.8v only
            FlashDB.Add(New SPI_FLASH("MXIC MX25L1633E", Mb016, &HC2, &H2415))
            FlashDB.Add(New SPI_FLASH("MXIC MX25L160", Mb016, &HC2, &H2015))
            FlashDB.Add(New SPI_FLASH("MXIC MX25L80", Mb008, &HC2, &H2014))
            FlashDB.Add(New SPI_FLASH("MXIC MX25L40", Mb004, &HC2, &H2013))
            FlashDB.Add(New SPI_FLASH("MXIC MX25L20", Mb002, &HC2, &H2012)) 'MX25L2005 MX25L2006E MX25L2026E
            FlashDB.Add(New SPI_FLASH("MXIC MX25L10", Mb001, &HC2, &H2011))
            FlashDB.Add(New SPI_FLASH("MXIC MX25U643", Mb064, &HC2, &H2537))
            FlashDB.Add(New SPI_FLASH("MXIC MX25U323", Mb032, &HC2, &H2536))
            FlashDB.Add(New SPI_FLASH("MXIC MX25U163", Mb016, &HC2, &H2535))
            FlashDB.Add(New SPI_FLASH("MXIC MX25U803", Mb008, &HC2, &H2534))
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
            FlashDB.Add(New SPI_FLASH("Microchip SST26VF064B", Mb064, &HBF, &H2643)) 'SST26VF064BA
            FlashDB.Add(New SPI_FLASH("Microchip SST26VF064", Mb064, &HBF, &H2603))
            FlashDB.Add(New SPI_FLASH("Microchip SST26VF032", Mb032, &HBF, &H2602)) 'PCT26VF032
            FlashDB.Add(New SPI_FLASH("Microchip SST26VF032B", Mb032, &HBF, &H2642, &H20, &H1000)) 'SST26VF032BA
            FlashDB.Add(New SPI_FLASH("Microchip SST26WF032", Mb032, &HBF, &H2622)) 'PCT26WF032
            FlashDB.Add(New SPI_FLASH("Microchip SST26VF016", Mb016, &HBF, &H2601, &H20, &H1000) With {.QUAD = SPI_QUADMODE.all_quadio})
            FlashDB.Add(New SPI_FLASH("Microchip SST26VF032", Mb032, &HBF, &H2602, &H20, &H1000) With {.QUAD = SPI_QUADMODE.all_quadio})
            FlashDB.Add(New SPI_FLASH("Microchip SST26VF016B", Mb016, &HBF, &H2641, &H20, &H1000)) 'SST26VF016BA
            FlashDB.Add(New SPI_FLASH("Microchip SST26VF016", Mb016, &HBF, &H16BF, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte})
            FlashDB.Add(New SPI_FLASH("Microchip SST26WF016B", Mb016, &HBF, &H2651)) 'SST26WF016BA
            FlashDB.Add(New SPI_FLASH("Microchip SST26WF080B", Mb008, &HBF, &H2658, &H20, &H1000))
            FlashDB.Add(New SPI_FLASH("Microchip SST26WF040B", Mb004, &HBF, &H2654, &H20, &H1000))
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF128B", Mb128, &HBF, &H2544) With {.SEND_EWSR = True}) 'Might use AAI
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF064C", Mb064, &HBF, &H254B) With {.SEND_EWSR = True}) 'PCT25VF064C
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF032B", Mb032, &HBF, &H254A) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True}) 'PCT25VF032B
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF032", Mb032, &HBF, &H2542) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True})
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF016B", Mb016, &HBF, &H2541) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True}) 'Confirmed (Build 350)
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF080B", Mb008, &HBF, &H258E, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True}) 'PCT25VF080B - Confirmed (Build 350)
            FlashDB.Add(New SPI_FLASH("Microchip SST25WF080B", Mb008, &H62, &H1614, &H20, &H1000) With {.SEND_EWSR = True})
            FlashDB.Add(New SPI_FLASH("Microchip SST25WF040B", Mb004, &H62, &H1613, &H20, &H1000) With {.SEND_EWSR = True})
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF080", Mb008, &HBF, &H80BF, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte, .SEND_EWSR = True})
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF040B", Mb004, &HBF, &H258D, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True}) 'PCT25VF040B
            FlashDB.Add(New SPI_FLASH("Microchip SST25WF040", Mb004, &HBF, &H2504, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True})
            FlashDB.Add(New SPI_FLASH("Microchip SST25WF020A", Mb002, &H62, &H1612, &H20, &H1000) With {.SEND_EWSR = True})
            FlashDB.Add(New SPI_FLASH("Microchip SST25LF020A", Mb002, &HBF, &H43BF, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte, .SEND_EWSR = True})
            FlashDB.Add(New SPI_FLASH("Microchip SST25WF020", Mb002, &HBF, &H2503, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True})
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF020", Mb002, &HBF, &H258C, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True}) 'SST25VF020B SST25PF020B PCT25VF020B
            FlashDB.Add(New SPI_FLASH("Microchip SST25WF010", Mb001, &HBF, &H2502, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True})
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF010", Mb001, &HBF, &H49BF, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte, .SEND_EWSR = True}) 'SST25VF010A PCT25VF010A
            FlashDB.Add(New SPI_FLASH("Microchip SST25WF512", Kb512, &HBF, &H2501, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Word, .SEND_EWSR = True})
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF512", Kb512, &HBF, &H48BF, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte, .SEND_EWSR = True}) 'SST25VF512A PCT25VF512A
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF020A", Mb002, &HBF, &H43, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte, .SEND_EWSR = True}) 'Confirmed (Build 350)
            FlashDB.Add(New SPI_FLASH("Microchip SST25VF010A", Mb001, &HBF, &H49, &H20, &H1000) With {.ProgramMode = SPI_ProgramMode.AAI_Byte, .SEND_EWSR = True}) 'Confirmed (Build 350)
            'PMC
            FlashDB.Add(New SPI_FLASH("PMC PM25LV016B", Mb016, &H7F, &H9D14))
            FlashDB.Add(New SPI_FLASH("PMC PM25LV080B", Mb008, &H7F, &H9D13))
            FlashDB.Add(New SPI_FLASH("PMC PM25LV040", Mb004, &H9D, &H7E7F))
            FlashDB.Add(New SPI_FLASH("PMC PM25LV020", Mb002, &H9D, &H7D7F))
            FlashDB.Add(New SPI_FLASH("PMC PM25LV010", Mb001, &H9D, &H7C7F))
            FlashDB.Add(New SPI_FLASH("PMC PM25LV512", Kb512, &H9D, &H7B7F))
            FlashDB.Add(New SPI_FLASH("PMC PM25LD020", Mb002, &H7F, &H9D22))
            FlashDB.Add(New SPI_FLASH("PMC Pm25LD010", Mb001, &H7F, &H9D21))
            FlashDB.Add(New SPI_FLASH("PMC Pm25LD512", Kb512, &H7F, &H9D20))
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
            FlashDB.Add(New SPI_FLASH("ISSI IS25CD020", Mb002, &H9D, &H1122))
            FlashDB.Add(New SPI_FLASH("ISSI IS25CD010", Mb001, &H9D, &H1021))
            FlashDB.Add(New SPI_FLASH("ISSI IS25CD512", Kb512, &H9D, &H520))
            FlashDB.Add(New SPI_FLASH("ISSI IS25CD025", Kb256, &H7F, &H9D2F))
            FlashDB.Add(New SPI_FLASH("ISSI IS25CQ032", Mb032, &H7F, &H9D46))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LP256", Mb256, &H9D, &H6019))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LP128", Mb128, &H9D, &H6018))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LP064", Mb064, &H9D, &H6017))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LP032", Mb032, &H9D, &H6016))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LP016", Mb016, &H9D, &H6015))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LP080", Mb008, &H9D, &H6014))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LQ032", Mb032, &H9D, &H4016))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LQ016", Mb016, &H9D, &H4015))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LQ080", Mb008, &H9D, &H4014))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LQ040", Mb004, &H9D, &H4013))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LQ020", Mb002, &H9D, &H4012))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LQ010", Mb001, &H9D, &H4011))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LQ512", Kb512, &H9D, &H4010))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LQ025", Kb256, &H9D, &H4009))
            FlashDB.Add(New SPI_FLASH("ISSI IS25LD040", Mb004, &H7F, &H9D7E))
            FlashDB.Add(New SPI_FLASH("ISSI IS25WD040", Mb004, &H7F, &H9D33))
            FlashDB.Add(New SPI_FLASH("ISSI IS25WD020", Mb002, &H7F, &H9D32))
            FlashDB.Add(New SPI_FLASH("ISSI IS25WP256", Mb256, &H9D, &H7019))
            FlashDB.Add(New SPI_FLASH("ISSI IS25WP128", Mb128, &H9D, &H7018))
            FlashDB.Add(New SPI_FLASH("ISSI IS25WP064", Mb064, &H9D, &H7017))
            FlashDB.Add(New SPI_FLASH("ISSI IS25WP032", Mb032, &H9D, &H7016))
            FlashDB.Add(New SPI_FLASH("ISSI IS25WP016", Mb016, &H9D, &H7015))
            FlashDB.Add(New SPI_FLASH("ISSI IS25WP080", Mb008, &H9D, &H7014))
            FlashDB.Add(New SPI_FLASH("ISSI IS25WP040", Mb004, &H9D, &H7013))
            FlashDB.Add(New SPI_FLASH("ISSI IS25WP020", Mb002, &H9D, &H7012))
            FlashDB.Add(New SPI_FLASH("ISSI IS25WQ040", Mb004, &H9D, &H1253))
            FlashDB.Add(New SPI_FLASH("ISSI IS25WQ020", Mb002, &H9D, &H1152))
            'Others
            FlashDB.Add(New SPI_FLASH("ESMT F25L04", Mb004, &H8C, &H12) With {.ProgramMode = SPI_ProgramMode.AAI_Word}) 'REMS only
            FlashDB.Add(New SPI_FLASH("ESMT F25L04", Mb004, &H8C, &H2013) With {.ProgramMode = SPI_ProgramMode.AAI_Word})
            FlashDB.Add(New SPI_FLASH("ESMT F25L08", Mb008, &H8C, &H13) With {.ProgramMode = SPI_ProgramMode.AAI_Word}) 'REMS only
            FlashDB.Add(New SPI_FLASH("ESMT F25L08", Mb008, &H8C, &H2014) With {.ProgramMode = SPI_ProgramMode.AAI_Word})
            FlashDB.Add(New SPI_FLASH("ESMT F25L32QA", Mb032, &H8C, &H4116))
            FlashDB.Add(New SPI_FLASH("Sanyo LE25FU406B", Mb004, &H62, &H1E62))
            FlashDB.Add(New SPI_FLASH("Berg_Micro BG25Q32A", Mb032, &HE0, &H4016))
            'SUPPORTED EEPROM SPI DEVICES:
            FlashDB.Add(New SPI_FLASH("Atmel AT25128B", 16384, 64) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("Atmel AT25256B", 32768, 64) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("Atmel AT25512", 65536, 128) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("ST M95010", 128, 16) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("ST M95020", 256, 16) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("ST M95040", 512, 16) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("ST M95080", 1024, 32) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("ST M95160", 2048, 32) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("ST M95320", 4096, 32) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("ST M95640", 8192, 32) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("ST M95128", 16384, 64) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("ST M95256", 32768, 64) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("ST M95512", 65536, 128) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("ST M95M01", 131072, 256) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("ST M95M02", 262144, 256) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("Atmel AT25010A", 128, 8) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("Atmel AT25020A", 256, 8) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("Atmel AT25040A", 512, 8) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("Atmel AT25080", 1024, 32) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("Atmel AT25160", 2048, 32) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("Atmel AT25320", 4096, 32) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("Atmel AT25640", 8192, 32) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("Microchip 25AA160A", 2048, 16) With {.EEPROM = True})
            FlashDB.Add(New SPI_FLASH("Microchip 25AA160B", 2048, 32) With {.EEPROM = True})
        End Sub

        Private Sub MFP_Database()
            'https://github.com/jhcloos/flashrom/blob/master/flashchips.h
            'Intel
            FlashDB.Add(New MFP_Flash("Intel 28F320J3", &H89, &H16, Mb032, Mb001) With {.WriteMode = MFP_PROG.IntelBuffer, .PAGE_SIZE = 32})
            FlashDB.Add(New MFP_Flash("Intel 28F640J3", &H89, &H17, Mb064, Mb001) With {.WriteMode = MFP_PROG.IntelBuffer, .PAGE_SIZE = 32})
            FlashDB.Add(New MFP_Flash("Intel 28F128J3", &H89, &H18, Mb128, Mb001) With {.WriteMode = MFP_PROG.IntelBuffer, .PAGE_SIZE = 32})
            FlashDB.Add(New MFP_Flash("Intel 28F256J3", &H89, &H1D, Mb256, Mb001) With {.WriteMode = MFP_PROG.IntelBuffer, .PAGE_SIZE = 32})
            FlashDB.Add(New MFP_Flash("Intel 28F320J5", &H89, &H14, Mb032, Mb001) With {.WriteMode = MFP_PROG.IntelBuffer, .PAGE_SIZE = 32})
            FlashDB.Add(New MFP_Flash("Intel 28F640J5", &H89, &H15, Mb064, Mb001) With {.WriteMode = MFP_PROG.IntelBuffer, .PAGE_SIZE = 32})
            FlashDB.Add(CreateMFP("Intel 28F800C3(T)", &H89, &H88C0, Mb008, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("Intel 28F800C3(B)", &H89, &H88C1, Mb008, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("Intel 28F160C3(T)", &H89, &H88C2, Mb016, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("Intel 28F160C3(B)", &H89, &H88C3, Mb016, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("Intel 28F320C3(T)", &H89, &H88C4, Mb032, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("Intel 28F320C3(B)", &H89, &H88C5, Mb032, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("Intel 28F640C3(T)", &H89, &H88CC, Mb064, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("Intel 28F640C3(B)", &H89, &H88CD, Mb064, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("Intel 28F400B3(T)", &H89, &H8894, Mb004, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("Intel 28F400B3(B)", &H89, &H8895, Mb004, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("Intel 28F800B3(T)", &H89, &H8892, Mb008, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("Intel 28F800B3(B)", &H89, &H8893, Mb008, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("Intel 28F160B3(T)", &H89, &H8890, Mb016, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("Intel 28F160B3(B)", &H89, &H8891, Mb016, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("Intel 28F320B3(T)", &H89, &H8896, Mb032, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("Intel 28F320B3(B)", &H89, &H8897, Mb032, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("Intel 28F640B3(T)", &H89, &H8898, Mb064, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("Intel 28F640B3(B)", &H89, &H8899, Mb064, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Btm))
            'AMD
            FlashDB.Add(New MFP_Flash("AMD AM29F010B", &H1, &H20, Mb001, Kb128)) 'Verified 380 (5v)
            FlashDB.Add(New MFP_Flash("AMD AM29F040B", &H20, &HE2, Mb004, Kb512) With {.WRITE_HARDWARE_DELAY = 30}) 'Verified 380 (5v) Why is this not: 01 A4? (PLCC32 and DIP32 tested)
            FlashDB.Add(CreateMFP("AMD AM29LV200(T)", &H1, &H223B, Mb002, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("AMD AM29LV200(B)", &H1, &H22BF, Mb002, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("AMD AM29F200(T)", &H1, &H2251, Mb002, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("AMD AM29F200(B)", &H1, &H2257, Mb002, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("AMD AM29LV400(T)", &H1, &H22B9, Mb004, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("AMD AM29LV400(B)", &H1, &H22BA, Mb004, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("AMD AM29F400(T)", &H1, &H2223, Mb004, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("AMD AM29F400(B)", &H1, &H22AB, Mb004, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("AMD AM29LV800(T)", &H1, &H22DA, Mb008, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("AMD AM29LV800(B)", &H1, &H225B, Mb008, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("AMD AM29F800(T)", &H1, &H22D6, Mb008, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("AMD AM29F800(B)", &H1, &H2258, Mb008, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("AMD AM29LV160B(T)", &H1, &H22C4, Mb016, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Four_Top)) 'Set HWDELAY to 25 (CV)
            FlashDB.Add(CreateMFP("AMD AM29LV160B(B)", &H1, &H2249, Mb016, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Four_Btm)) 'Set HWDELAY to 25
            FlashDB.Add(CreateMFP("AMD AM29DL322G(T)", &H1, &H2255, Mb032, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("AMD AM29DL322G(B)", &H1, &H2256, Mb032, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("AMD AM29DL323G(T)", &H1, &H2250, Mb032, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("AMD AM29DL323G(B)", &H1, &H2253, Mb032, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("AMD AM29DL324G(T)", &H1, &H225C, Mb032, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("AMD AM29DL324G(B)", &H1, &H225F, Mb032, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("AMD AM29LV320D(T)", &H1, &H22F6, Mb032, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("AMD AM29LV320D(B)", &H1, &H22F9, Mb032, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("AMD AM29LV320M(T)", &H1, &H2201, Mb032, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("AMD AM29LV320M(B)", &H1, &H2200, Mb032, MFP_PROG.BypassMode, EraseMethod.Standard, MFP_BLKLAYOUT.Two_Btm))
            'Winbond
            Dim W_W49F002U As New MFP_Flash("Winbond W49F002U", &HDA, &HB, Mb002) With {.WRITE_HARDWARE_DELAY = 18} 'Needs additional hardware delay
            W_W49F002U.AddSector(Mb001) 'Main Block
            W_W49F002U.AddSector(98304) 'Main Block
            W_W49F002U.AddSector(Kb064) 'Parameter Block
            W_W49F002U.AddSector(Kb064) 'Parameter Block
            W_W49F002U.AddSector(Kb128) 'Boot Block
            FlashDB.Add(W_W49F002U)
            Dim W29EE512 As New MFP_Flash("Winbond W29EE512", &HDA, &HC8, Kb512, Kb256) 'Verified 372
            W29EE512.ERASE_REQUIRED = False 'Each page is automatically erased
            W29EE512.PAGE_SIZE = 128
            W29EE512.WriteMode = MFP_PROG.PageMode 'Write in 128 byte pages
            W29EE512.WRITE_HARDWARE_DELAY = 2
            FlashDB.Add(W29EE512)
            Dim W29C010 As New MFP_Flash("Winbond W29C010", &HDA, &HC1, Mb001, Kb256)
            W29C010.ERASE_REQUIRED = False 'Each page is automatically erased
            W29C010.PAGE_SIZE = 128
            W29C010.WriteMode = MFP_PROG.PageMode 'Write in 128 byte pages
            W29C010.WRITE_HARDWARE_DELAY = 2
            FlashDB.Add(W29C010)
            Dim W29C020 As New MFP_Flash("Winbond W29C020", &HDA, &H45, Mb002, Kb256)
            W29C020.ERASE_REQUIRED = False 'Each page is automatically erased
            W29C020.PAGE_SIZE = 128
            W29C020.WriteMode = MFP_PROG.PageMode 'Write in 128 byte pages
            W29C020.WRITE_HARDWARE_DELAY = 2
            FlashDB.Add(W29C020)
            Dim W29C040 As New MFP_Flash("Winbond W29C040", &HDA, &H46, Mb004, Kb256)
            W29C040.ERASE_REQUIRED = False 'Each page is automatically erased
            W29C040.PAGE_SIZE = 256
            W29C040.WriteMode = MFP_PROG.PageMode 'Write in 256 byte pages
            W29C040.WRITE_HARDWARE_DELAY = 2
            FlashDB.Add(W29C040)
            FlashDB.Add(CreateMFP("Winbond W29GL032CT", &H1, &H227E, Mb032, MFP_PROG.Standard, MFP_BLKLAYOUT.Two_Top, &H1A01))
            FlashDB.Add(CreateMFP("Winbond W29GL032CB", &H1, &H227E, Mb032, MFP_PROG.Standard, MFP_BLKLAYOUT.Two_Btm, &H1A00))
            'SST
            FlashDB.Add(New MFP_Flash("SST 39SF512", &HBF, &HB4, Kb512, Kb032)) '5v
            FlashDB.Add(New MFP_Flash("SST 39SF010", &HBF, &HB5, Mb001, Kb032)) '5v
            FlashDB.Add(New MFP_Flash("SST 39SF020", &HBF, &HB6, Mb002, Kb032) With {.WRITE_HARDWARE_DELAY = 20}) '5v 'Verified 372
            FlashDB.Add(New MFP_Flash("SST 39LF010", &HBF, &HD5, Mb001, Kb032)) '3.3v
            FlashDB.Add(New MFP_Flash("SST 39LF020", &HBF, &HD6, Mb002, Kb032)) '3.3v
            FlashDB.Add(New MFP_Flash("SST 39LF040", &HBF, &HD7, Mb004, Kb032) With {.WRITE_HARDWARE_DELAY = 0}) 'Verified Build 406 (3.3v)
            FlashDB.Add(New MFP_Flash("SST 39VF800", &HBF, &H2781, Mb008, Kb032))
            FlashDB.Add(New MFP_Flash("SST 39VF160", &HBF, &H2782, Mb016, Kb032))
            FlashDB.Add(New MFP_Flash("SST 39VF1681", &HBF, &HC8, Mb016, Kb512) With {.WRITE_HARDWARE_DELAY = 0}) 'Verified Build 406 (3.3v / 8x ONLY)
            FlashDB.Add(New MFP_Flash("SST 39VF1682", &HBF, &HC9, Mb016, Kb512) With {.WRITE_HARDWARE_DELAY = 0}) 'Verified Build 406 (3.3v / 8x ONLY)
            FlashDB.Add(New MFP_Flash("SST 39VF1601", &HBF, &H234B, Mb016, Kb032)) 'SE (0x30) is used. Otherwise BE (0x50) erases 64K (32Kword)
            FlashDB.Add(New MFP_Flash("SST 39VF1602", &HBF, &H234A, Mb016, Kb032))
            FlashDB.Add(New MFP_Flash("SST 39VF1602", &HBF, &H235B, Mb032, Kb032))
            FlashDB.Add(New MFP_Flash("SST 39VF3202", &HBF, &H235A, Mb032, Kb032))
            FlashDB.Add(New MFP_Flash("SST 39VF6401", &HBF, &H236B, Mb064, Kb032))
            FlashDB.Add(New MFP_Flash("SST 39VF6402", &HBF, &H236A, Mb064, Kb032))
            'Atmel
            Dim AT29C010A As New MFP_Flash("Atmel AT29C010A", &H1F, &HD5, Mb001, Kb256)
            AT29C010A.ERASE_REQUIRED = False 'Each page is automatically erased
            AT29C010A.PAGE_SIZE = 128
            AT29C010A.WriteMode = MFP_PROG.PageMode 'Write in 128 byte pages
            FlashDB.Add(AT29C010A)
            FlashDB.Add(New MFP_Flash("Atmel AT49F512", &H1F, &H3, Kb512, 65536)) 'Verified 372
            FlashDB.Add(New MFP_Flash("Atmel AT49F010", &H1F, &H17, Mb001, Mb001))
            FlashDB.Add(New MFP_Flash("Atmel AT49F020", &H1F, &HB, Mb002, Mb002))
            FlashDB.Add(New MFP_Flash("Atmel AT49F040", &H1F, &H13, Mb004, Mb004))
            FlashDB.Add(New MFP_Flash("Atmel AT49F040T", &H1F, &H12, Mb004, Mb004))
            FlashDB.Add(CreateMFP("Atmel AT49BV/LV16X", &H1F, &HC0, Mb016, MFP_PROG.Standard, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("Atmel AT49BV/LV16XT", &H1F, &HC2, Mb016, MFP_PROG.Standard, MFP_BLKLAYOUT.Two_Btm))
            'MXIC
            FlashDB.Add(New MFP_Flash("MXIC MX29L3211", &HC2, &HF9, Mb032, Mb001) With {.WriteMode = MFP_PROG.PageMode, .PAGE_SIZE = 256, .RESET_REQUIRED = True}) '32-mbit (tested with SO-44)
            FlashDB.Add(New MFP_Flash("MXIC MX29LV040C", &HC2, &H4F, Mb004, Kb512) With {.WRITE_HARDWARE_DELAY = 5, .WRITE_SOFTWARE_DELAY = 50}) 'Verified 380 (3.3v)
            FlashDB.Add(CreateMFP("MXIC MX29LV400T", &HC2, &H22B9, Mb004, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("MXIC MX29LV400B", &HC2, &H22BA, Mb004, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("MXIC MX29LV800T", &HC2, &H22DA, Mb008, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("MXIC MX29LV800B", &HC2, &H225B, Mb008, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("MXIC MX29LV160DT", &HC2, &H22C4, Mb016, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("MXIC MX29LV160DB", &HC2, &H2249, Mb016, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("MXIC MX29LV320T", &HC2, &H22A7, Mb032, MFP_PROG.Standard, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("MXIC MX29LV320B", &HC2, &H22A8, Mb032, MFP_PROG.Standard, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("MXIC MX29LV640ET", &HC2, &H22C9, Mb064, MFP_PROG.Standard, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("MXIC MX29LV640EB", &HC2, &H22CB, Mb064, MFP_PROG.Standard, MFP_BLKLAYOUT.Two_Btm))
            'Cypress / Spansion
            'http://www.cypress.com/file/177976/download   S29GLxxxP (TSOP56)
            'http://www.cypress.com/file/219926/download   S29GLxxxS (TSOP56 only)
            FlashDB.Add(New MFP_Flash("Cypress S29GL032", &H1, &H227E, Mb032, Kb512) With {.ID2 = &H1D00}) 'Bottom boot
            FlashDB.Add(New MFP_Flash("Cypress S29GL032", &H1, &H227E, Mb032, Kb512) With {.ID2 = &H1D01}) 'Top-boot
            FlashDB.Add(New MFP_Flash("Cypress S29GL032M", &H1, &H227E, Mb032, Mb001) With {.ID2 = &H1C00})
            FlashDB.Add(New MFP_Flash("Cypress S29GL032M", &H1, &H227E, Mb032, Mb001) With {.ID2 = &H1A00}) 'Bottom boot
            FlashDB.Add(New MFP_Flash("Cypress S29GL032M", &H1, &H227E, Mb032, Mb001) With {.ID2 = &H1A01}) 'Top Boot
            FlashDB.Add(New MFP_Flash("Cypress S29GL064", &H1, &H227E, Mb064, Kb512) With {.ID2 = &HC00}) 'Bottom boot
            FlashDB.Add(New MFP_Flash("Cypress S29GL064", &H1, &H227E, Mb064, Kb512) With {.ID2 = &HC01}) 'Top-boot
            FlashDB.Add(New MFP_Flash("Cypress S29GL064M", &H1, &H227E, Mb064, Mb001) With {.ID2 = &H1300})
            FlashDB.Add(New MFP_Flash("Cypress S29GL064M", &H1, &H227E, Mb064, Mb001) With {.ID2 = &H1301})
            FlashDB.Add(New MFP_Flash("Cypress S29GL128", &H1, &H227E, Mb128, Mb001) With {.ID2 = &H2101})
            FlashDB.Add(New MFP_Flash("Cypress S29GL128M", &H1, &H227E, Mb128, Mb001) With {.ID2 = &H1200})
            FlashDB.Add(New MFP_Flash("Cypress S29GL256", &H1, &H227E, Mb256, Mb001) With {.ID2 = &H2201})
            FlashDB.Add(New MFP_Flash("Cypress S29GL256M", &H1, &H227E, Mb256, Mb001) With {.ID2 = &H1201})
            FlashDB.Add(New MFP_Flash("Cypress S29GL512", &H1, &H227E, Mb512, Mb001) With {.ID2 = &H2301})
            FlashDB.Add(New MFP_Flash("Cypress S29GL01G", &H1, &H227E, Gb001, Mb001) With {.ID2 = &H2801})
            FlashDB.Add(New MFP_Flash("Cypress S70GL02G", &H1, &H227E, Gb002, Mb001) With {.ID2 = &H4801})
            'ST Microelectronics (now numonyx)
            FlashDB.Add(CreateMFP("ST M29W800AT", &H20, &HD7, Mb008, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("ST M29W800AB", &H20, &H5B, Mb008, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("ST M28W160CT", &H20, &H88CE, Mb016, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("ST M28W160CB", &H20, &H88CF, Mb016, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("ST M29W160ET", &H20, &H22C4, Mb016, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("ST M29W160EB", &H20, &H2249, Mb016, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("ST M29D323DT", &H20, &H225E, Mb032, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("ST M29D323DB", &H20, &H225F, Mb032, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("ST M28W320FCT", &H20, &H88BA, Mb032, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("ST M28W320FCB", &H20, &H88BB, Mb032, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("ST M28W320BT", &H20, &H88BC, Mb032, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("ST M28W320BB", &H20, &H88BD, Mb032, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("ST 29W320DT", &H20, &H22CA, Mb032, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("ST 29W320DB", &H20, &H22CB, Mb032, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("ST M28W640ECT", &H20, &H8848, Mb064, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("ST M28W640ECB", &H20, &H8849, Mb064, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(New MFP_Flash("ST M58LW064D", &H20, &H17, Mb064, Mb001) With {.WriteMode = MFP_PROG.IntelSharp})
            'Micron
            FlashDB.Add(CreateMFP("Micron M29F200FT", &HC2, &H2251, Mb002, MFP_PROG.BypassMode, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("Micron M29F200FB", &HC2, &H2257, Mb002, MFP_PROG.BypassMode, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("Micron M29F400FT", &HC2, &H2223, Mb004, MFP_PROG.BypassMode, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("Micron M29F400FB", &HC2, &H22AB, Mb004, MFP_PROG.BypassMode, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("Micron M29F800FT", &H1, &H22D6, Mb008, MFP_PROG.BypassMode, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("Micron M29F800FB", &H1, &H2258, Mb008, MFP_PROG.BypassMode, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("Micron M29F160FT", &H1, &H22D2, Mb016, MFP_PROG.BypassMode, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("Micron M29F160FB", &H1, &H22D8, Mb016, MFP_PROG.BypassMode, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("Micron M29W160ET", &H20, &H22C4, Mb016, MFP_PROG.BypassMode, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("Micron M29W160EB", &H20, &H2249, Mb016, MFP_PROG.BypassMode, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("Micron M29W640GT", &H20, &H227E, Mb064, MFP_PROG.BypassMode, MFP_BLKLAYOUT.Two_Top, &H1001))
            FlashDB.Add(CreateMFP("Micron M29W640GB", &H20, &H227E, Mb064, MFP_PROG.BypassMode, MFP_BLKLAYOUT.Two_Btm, &H1000))
            FlashDB.Add(New MFP_Flash("Micron M29W256G", &H20, &H227E, Mb256, Mb001) With {.ID2 = &H2201, .WriteMode = MFP_PROG.BypassMode}) '(VC) TSOP56
            'Sharp
            Dim LHF00L15 As New MFP_Flash("Sharp LHF00L15", &HB0, &HA1, Mb032) 'Verified 382
            LHF00L15.WriteMode = MFP_PROG.IntelSharp
            LHF00L15.WRITE_HARDWARE_DELAY = 0 'Not needed
            LHF00L15.AddSector(Kb064, 8)
            LHF00L15.AddSector(Kb512, 1)
            LHF00L15.AddSector(Mb001, 31)
            FlashDB.Add(LHF00L15)
            FlashDB.Add(New MFP_Flash("Sharp LH28F160S3", &HB0, &HD0, Mb016, Kb512) With {.WriteMode = MFP_PROG.IntelSharp})
            FlashDB.Add(New MFP_Flash("Sharp LH28F320S3", &HB0, &HD4, Mb032, Kb512) With {.WriteMode = MFP_PROG.IntelSharp})
            FlashDB.Add(CreateMFP("Sharp LH28F160BJE", &HB0, &HE9, Mb016, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("Sharp LH28F320BJE", &HB0, &HE3, Mb032, MFP_PROG.IntelSharp, MFP_BLKLAYOUT.Two_Btm))
            'Others
            FlashDB.Add(CreateMFP("Toshiba TC58FVT800", &H98, &H4F, Mb016, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("Toshiba TC58FVB800", &H98, &HCE, Mb016, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("Toshiba TC58FVT160", &H98, &HC2, Mb016, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("Toshiba TC58FVB160", &H98, &H43, Mb016, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("Toshiba TC58FVT321", &H98, &H9C, Mb032, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("Toshiba TC58FVB321", &H98, &H9A, Mb032, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("Samsung K8D1716UT", &HEC, &H2275, Mb016, MFP_PROG.BypassMode, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("Samsung K8D1716UB", &HEC, &H2277, Mb016, MFP_PROG.BypassMode, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("Samsung K8D3216UT", &HEC, &H22A0, Mb032, MFP_PROG.BypassMode, MFP_BLKLAYOUT.Two_Top))
            FlashDB.Add(CreateMFP("Samsung K8D3216UB", &HEC, &H22A2, Mb032, MFP_PROG.BypassMode, MFP_BLKLAYOUT.Two_Btm))
            FlashDB.Add(CreateMFP("Hynix HY29F400T", &HAD, &H2223, Mb004, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("Hynix HY29F400B", &HAD, &H22AB, Mb004, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("Hynix HY29F800T", &HAD, &H22D6, Mb008, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("Hynix HY29F800B", &HAD, &H2258, Mb008, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("Hynix HY29LV160T", &HAD, &H22C4, Mb016, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("Hynix HY29LV160B", &HAD, &H2249, Mb016, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("Fujitsu MBM29LV400TC", &H4, &H22B9, Mb004, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("Fujitsu MBM29LV400BC", &H4, &H22BA, Mb004, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("Fujitsu MBM29LV800TA", &H4, &H22DA, Mb008, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("Fujitsu MBM29LV800BA", &H4, &H225B, Mb008, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("Fujitsu MBM29LV160T", &H4, &H22C4, Mb016, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Top))
            FlashDB.Add(CreateMFP("Fujitsu MBM29LV160B", &H4, &H2249, Mb016, MFP_PROG.Standard, MFP_BLKLAYOUT.Four_Btm))
            FlashDB.Add(CreateMFP("Fujitsu MBM29LV320TE", &H4, &H22F6, Mb032, MFP_PROG.Standard, MFP_BLKLAYOUT.Two_Top)) 'Supports FAST programming (ADR=0xA0,PA=PD)
            FlashDB.Add(CreateMFP("Fujitsu MBM29LV320BE", &H4, &H22F9, Mb032, MFP_PROG.Standard, MFP_BLKLAYOUT.Two_Btm)) 'Supports FAST programming (ADR=0xA0,PA=PD)
            FlashDB.Add(CreateMFP("Fujitsu MBM29DL32XTD", &H4, &H2259, Mb032, MFP_PROG.Standard, MFP_BLKLAYOUT.Two_Top)) 'Supports FAST programming (ADR=0xA0,PA=PD)
            FlashDB.Add(CreateMFP("Fujitsu MBM29DL32XBD", &H4, &H225A, Mb032, MFP_PROG.Standard, MFP_BLKLAYOUT.Two_Btm)) 'Supports FAST programming (ADR=0xA0,PA=PD)
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
            FlashDB.Add(New NAND_Flash("Micron NAND04GW3B", &H20, &HDC1095, Gb004, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Micron MT29F2G08AAB", &H2C, &HDA0015, Gb002, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Micron MT29F4G08BAB", &H2C, &HDC0015, Gb004, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Micron MT29F1G08ABAEA", &H2C, &HF1809504UI, Gb001, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Micron MT29F1G08ABBEA", &H2C, &HA1801504UI, Gb001, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Micron MT29F1G08ABADAWP", &H2C, &HF1009582UI, Gb001, 2048, 64, Mb001)) 'Verify here
            FlashDB.Add(New NAND_Flash("Micron MT29F2G08ABBFA", &H2C, &HAA901504UI, Gb002, 2048, 224, Mb001))
            FlashDB.Add(New NAND_Flash("Micron MT29F2G08ABAFA", &H2C, &HDA909504UI, Gb002, 2048, 224, Mb001))
            FlashDB.Add(New NAND_Flash("Micron MT29F4G08AAA", &H2C, &HDC909554UI, Gb004, 2048, 64, Mb001)) '3v
            FlashDB.Add(New NAND_Flash("Micron MT29F8G08BAA", &H2C, &HD3D19558UI, Gb008, 2048, 64, Mb001)) '3v

            'Toshiba SLC 8x NAND devices
            FlashDB.Add(New NAND_Flash("Toshiba TC58DVM92A5TA10", &H98, &H76A5C029UI, Mb512, 512, 16, Kb128))
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
            FlashDB.Add(New NAND_Flash("Samsung K9F1G08U0E", &HEC, &HF1009541UI, Gb001, 2048, 64, Mb001)) 'Added in 434
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
            FlashDB.Add(New NAND_Flash("Cypress S34ML02G2", &H1, &HD89097UI, Gb002, 2048, 64, Mb001))
            FlashDB.Add(New NAND_Flash("Cypress S34ML04G2", &H1, &HDC9095UI, Gb004, 2048, 64, Mb001))

            'Others
            FlashDB.Add(New NAND_Flash("Zentel A5U1GA31ATS", &H92, &HF1809540UI, Gb001, 2048, 64, Mb001))

            'MLC and 8LC devices:
            'Database.Add(New MPFlash("SanDisk SDTNPNAHEM-008G", &H98, &H809272UI, MB8Gb * 8, 8192, 1024, MB032))
            'FlashDB.Add(New NAND_Flash("Toshiba TC58NVG3D4CTGI0", &H98, &H8095D6, Gb008, 4096, 256, Mb001)) 'We have this one
            'Samsung K9WG08U1M and this one

            '0x98D385A5
            '2kB / 256kB

            'Dim test As New NAND_Flash("Toshiba TC58NVG3D4CTGI0", &H98, &H8095D6, Gb008, 2048, 64, Mb002)
            'FlashDB.Add(test)

        End Sub

        'Helper function to create the proper definition for Atmel/Adesto Series 45 SPI devices
        Private Function CreateSeries45(atName As String, mbitsize As UInt32, id1 As UInt16, id2 As UInt16, page_size As UInt32) As SPI_FLASH
            Dim atmel_spi As New SPI_FLASH(atName, mbitsize, &H1F, id1, &H50, page_size * 8, page_size)
            atmel_spi.ID2 = id2
            atmel_spi.PAGE_SIZE_EXTENDED = page_size + (page_size / 32) 'Additional bytes available per page
            atmel_spi.ProgramMode = SPI_ProgramMode.Atmel45Series  'Atmel Series 45
            atmel_spi.OP_COMMANDS.RDSR = &HD7
            atmel_spi.OP_COMMANDS.READ = &HE8
            atmel_spi.OP_COMMANDS.PROG = &H12
            Return atmel_spi
        End Function
        'Creates a parallel device with the standard block layouts
        Private Function CreateMFP(partname As String, mfg As Byte, id1 As UInt16, size As UInt32, write_mode As MFP_PROG, layout As MFP_BLKLAYOUT, Optional ID2 As UInt16 = 0) As MFP_Flash
            Dim new_device As New MFP_Flash(partname, mfg, id1, size)
            new_device.WriteMode = write_mode
            new_device.ID2 = ID2
            Dim blocks As UInt32 = (size / Kb512)
            If layout = MFP_BLKLAYOUT.Four_Top Then
                new_device.AddSector(Kb512, blocks - 1)
                new_device.AddSector(Kb256, 1)
                new_device.AddSector(Kb064, 2)
                new_device.AddSector(Kb128, 1)
            ElseIf layout = MFP_BLKLAYOUT.Two_Top Then
                new_device.AddSector(Kb512, blocks - 1)
                new_device.AddSector(Kb064, 8)
            ElseIf layout = MFP_BLKLAYOUT.Four_Btm Then
                new_device.AddSector(Kb128, 1)
                new_device.AddSector(Kb064, 2)
                new_device.AddSector(Kb256, 1)
                new_device.AddSector(Kb512, blocks - 1)
            ElseIf layout = MFP_BLKLAYOUT.Two_Btm Then
                new_device.AddSector(Kb064, 8)
                new_device.AddSector(Kb512, blocks - 1)
            End If
            Return new_device
        End Function

        Public Function FindDevice(MFG As Byte, ID1 As UInt16, ID2 As UInt16, X8_MODE As Boolean, DEVICE As MemoryType) As Device
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
                    For Each flash In FlashDB
                        If flash.FLASH_TYPE = MemoryType.SERIAL_NOR Then
                            If flash.MFG_CODE = MFG Then
                                If (flash.ID1 = ID1) Then
                                    If flash.ID2 = 0 OrElse flash.ID2 = ID2 Then Return flash
                                End If
                            End If
                        End If
                    Next
                Case MemoryType.PARALLEL_NOR
                    For Each flash In FlashDB
                        If flash.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                            If flash.MFG_CODE = MFG Then
                                If X8_MODE Then 'Only checks the LSB of ID1 (and ignore ID2)
                                    If ID1 = (flash.ID1 And 255) Then Return flash
                                Else
                                    If (flash.ID1 = ID1) Then
                                        If flash.ID2 = 0 OrElse flash.ID2 = ID2 Then Return flash
                                    End If
                                End If
                            End If
                        End If
                    Next
            End Select
            Return Nothing 'Not found
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

#Region "Catalog / Data file"
        Private Sub CreateHtmlCatalog(ByVal FlashType As MemoryType, ByVal ColumnCount As Integer, ByVal file_name As String)
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
                For i = 1 To ColumnCount
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
        Private Function GetFlashDevices(ByVal type As MemoryType) As Device()
            Dim dev As New List(Of Device)
            If type = MemoryType.PARALLEL_NOR Then 'Search only CFI devices
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                        dev.Add(flash)
                    End If
                Next
            ElseIf type = MemoryType.SERIAL_NOR Then
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.SERIAL_NOR Then
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
                    If DirectCast(dev, SPI_FLASH).EEPROM Then SkipAdd = True
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
                Dim size_str As String = ""
                If (input.Parts(i).FLASH_SIZE < Mb001) Then
                    size_str = (input.Parts(i).FLASH_SIZE / 1024).ToString & "Kbit"
                ElseIf (input.Parts(i).FLASH_SIZE < Gb001) Then
                    size_str = (input.Parts(i).FLASH_SIZE / Mb001).ToString & "Mbit"
                Else
                    size_str = (input.Parts(i).FLASH_SIZE / Gb001).ToString & "Gbit"
                End If
                part_numbers(i) = part_name & " (" & size_str & ")"
            Next
        End Sub

        Public Sub WriteDatabaseToFile()
            Dim f As New List(Of String)
            For Each s As SPI_FLASH In FlashDB
                f.Add(s.NAME & " (" & (s.FLASH_SIZE / Mb001) & "Mbit)")
            Next
            Utilities.FileIO.WriteFile(f.ToArray, "d:\spi_flash_list.txt")
        End Sub

#End Region

    End Class


End Namespace