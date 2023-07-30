Imports System.Runtime.Serialization

Namespace FlashMemory

    Public Module Constants
        Public Const Kb016 As UInt32 = 2048UI
        Public Const Kb032 As UInt32 = 4096UI
        Public Const Kb064 As UInt32 = 8192UI
        Public Const Kb128 As UInt32 = 16384UI
        Public Const Kb256 As UInt32 = 32768UI
        Public Const Kb512 As UInt32 = 65536UI
        Public Const Mb001 As UInt32 = 131072UI
        Public Const Mb002 As UInt32 = 262144UI
        Public Const Mb003 As UInt32 = 393216UI
        Public Const Mb004 As UInt32 = 524288UI
        Public Const Mb008 As UInt32 = 1048576UI
        Public Const Mb016 As UInt32 = 2097152UI
        Public Const Mb032 As UInt32 = 4194304UI
        Public Const Mb064 As UInt32 = 8388608UI
        Public Const Mb128 As UInt32 = 16777216UI
        Public Const Mb256 As UInt32 = 33554432UI
        Public Const Mb512 As UInt32 = 67108864UI
        Public Const Gb001 As UInt32 = 134217728UI
        Public Const Gb002 As UInt32 = 268435456UI
        Public Const Gb004 As UInt32 = 536870912UI
        Public Const Gb008 As UInt32 = 1073741824UI
        Public Const Gb016 As UInt32 = 2147483648UI
        Public Const Gb032 As UInt64 = 4294967296UL
        Public Const Gb064 As UInt64 = 8589934592UL
        Public Const Gb128 As UInt64 = 17179869184UL
        Public Const Gb256 As UInt64 = 34359738368UL

        Public Function GetDataSizeString(size As Int64) As String
            Dim size_str As String = ""
            If (size < Mb001) Then
                size_str = (size / 128).ToString & "Kbit"
            ElseIf (size < Gb001) Then
                size_str = (size / Mb001).ToString & "Mbit"
            Else
                size_str = (size / Gb001).ToString & "Gbit"
            End If
            Return size_str
        End Function

    End Module

    Public Structure FlashDetectResult
        Public Property Successful As Boolean
        Public Property MFG As Byte
        Public Property ID1 As UInt16
        Public Property ID2 As UInt16
        Public ReadOnly Property PART As UInt32
            Get
                Return (CUInt(Me.ID1) << 16) Or (Me.ID2)
            End Get
        End Property

    End Structure

    Public Enum MemoryType As Integer
        UNSPECIFIED = 0
        PARALLEL_NOR = 1
        OTP_EPROM = 2
        SERIAL_NOR = 3 'SPI devices
        SERIAL_QUAD = 4 'SQI devices
        SERIAL_I2C = 5 'I2C EEPROMs
        SERIAL_MICROWIRE = 6
        SERIAL_NAND = 7 'SPI NAND devices
        SERIAL_SWI = 8 'Atmel single-wire
        PARALLEL_NAND = 9 'NAND X8 devices
        JTAG_CFI = 10 'Non-Vol memory attached to a MCU with DMA access
        JTAG_SPI = 11 'SPI devices connected to an MCU with a SPI access register
        FWH_NOR = 12 'Firmware hub memories
        HYPERFLASH = 13
        DFU_MODE = 14
    End Enum

    Public Enum FlashAccess
        Read 'We can read but can not write or erase
        ReadWriteErase 'We can read, write, and perform a full erase
        ReadWriteOnce 'We can read but can only write once
        ReadWrite 'We can read and write but not erase
    End Enum


    Public Enum FlashArea As Byte
        Main = 0 'Data area (read main page, skip to next page)
        OOB = 1 'Extended area (skip main page, read oob page)
        All = 2 'All data (read main page, then oob page, repeat)
        NotSpecified = 255
    End Enum

    Public Enum MFP_PRG As Integer
        Standard = 0 'Use the standard sequence that chip id detected
        PageMode = 1 'Writes an entire page of data (128 bytes etc.)
        BypassMode = 2 'Writes 0,64,128 bytes using ByPass sequence; 0x555=0xAA;0x2AA=55;0x555=20;(0x00=0xA0;SA=DATA;...)0x00=0x90;0x00=0x00
        IntelSharp = 3 'Writes data (SA=0x40;SA=DATA;SR.7), erases sectors (SA=0x50;SA=0x60;SA=0xD0,SR.7,SA=0x20;SA=0xD0,SR.7)
        Buffer1 = 4 'Use Write-To-Buffer mode (x16 only), used mostly by Intel (SA=0xE8;...;SA=0xD0)
        Buffer2 = 5 'Use Write-To-Buffer mode (x16 only), Used by Spansion/Winbond (0x555=0xAA;0x2AA=0x55,SA=0x25;SA=WC;...;SA=0x29;DELAY)
        EEPROM = 6 'Use RAM like mode
    End Enum

    Public Enum MFP_DELAY As Integer
        None = 0
        uS = 1 'Wait for uS delay cycles (set HARDWARE_DELAY to specify cycles)
        mS = 2 'Wait for mS delay cycles (set HARDWARE_DELAY to specify cycles)
        SR1 = 3 'Wait for Status-Register (0x555=0x70,[sr>>7],EXIT), used by Spansion/Cypress
        SR2 = 4 'Wait for Status-Register (0x5555=0xAA,0x2AAA=0x55,0x5555=0x70,[sr>>7])
        SR3 = 5 'Wait for Status-Register (PA=0x70, Sharp dual plane)
        DQ7 = 6 'Wait for DQ7 to equal last byte written (lower byte for X16)
        NAND = 7 'Used by NAND mode
        HF = 8 'Used by HYPERFLASH
        RYRB = 9 'Wait for RY/BY pin to be HIGH
        NAND_SR = 10 'Wait for NAND using command instead of RB_x pin
        NAND_RBx = 11
    End Enum

    Public Enum VCC_IF As Integer
        UNKNOWN
        SERIAL_1V8
        SERIAL_2V5
        SERIAL_3V
        SERIAL_5V
        SERIAL_1V8_5V  '1.8-5.5V
        SERIAL_2V7_5V  '2.5-5.5V
        X8_1V8  'DQ[0..7]; VCC=1.8V; VIO=1.8V
        X8_3V 'DQ[0..7]; VCC=3V; VIO=3V
        X8_5V   'DQ[0..7]; VCC=5V
        X8_5V_VPP  'DQ[0..7]; VCC=5V; 12V ERASE/PRG
        X16_1V8  'DQ[0..15]; VCC=1.8V; VIO=1.8V
        X16_3V   'DQ[0..15]; VCC=3V; VIO=3V
        X16_5V   'DQ[0..15]; VCC=5V; VIO=5V
        X16_5V_VPP 'DQ[0..7]; VCC=5V; 12V ERASE/PRG
        X16_X8_3V   'Uses BYTE# to select X16 (Word) or X8 (Byte)
    End Enum

    Public Enum BLKLYT As Integer
        None = 0
        Four_Top = 1
        Two_Top = 2
        Four_Btm = 3
        Two_Btm = 4
        Dual = 5 'Contans top and bottom boot
        'Uniform block sizes
        Kb016_Uni = 6 '2KByte
        Kb032_Uni = 7 '4KByte
        Kb064_Uni = 8 '8KByte
        Kb128_Uni = 9 '16KByte
        Kb256_Uni = 10 '32KByte
        Kb512_Uni = 11 '64KByte
        Mb001_Uni = 12 '128KByte
        'Non-Uniform
        Mb002_NonUni = 13
        Mb032_NonUni = 14
        Mb016_Samsung = 15
        Mb032_Samsung = 16
        Mb064_Samsung = 17
        Mb128_Samsung = 18 'Mb64_Samsung x 2
        Mb256_Samsung = 19
        EntireDevice = 20
        'Intel P30
        P30_Top = 21 '4x32KB; ?x128KB
        P30_Btm = 22 '?x128KB; 4x32KB
        'Intel B5
        Four_Top_B5
        Four_Btm_B5
        'Sharp
        Sharp_Top
        Sharp_Btm
    End Enum

    Public Enum EraseMethod
        Standard 'Chip-Erase, then Blank check
        BySector 'Erase each sector (some chips lack Erase All)
        DieErase 'Do a DIE erase for each 32MB die
        Micron 'Some Micron devices need either DieErase or Standard
    End Enum

    Public Enum SPI_QUAD_SUPPORT
        NO_QUAD = 0 'Only SPI (not multi-io capability)
        QUAD = 1 'All commands are data are received in 1/2/4
        SPI_QUAD = 2 'Commands are sent in single, but data is sent/received in multi-io
    End Enum

    Public Enum VENDOR_FEATURE As Integer
        NotSupported = -1
        Micron = 1
        Spansion_FL = 2
        ISSI = 3
        Winbond = 4
        Intel_01
    End Enum

    Public Enum SPI_PROG As Byte
        PageMode = 0
        AAI_Byte = 1
        AAI_Word = 2
        Atmel45Series = 3
        SPI_EEPROM = 4
        Nordic = 5
    End Enum

    Public Class SPI_OPCODES
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
        Public FAST_READ As Byte = &HB 'FAST READ
        Public DUAL_READ As Byte = &H3B 'DUAL OUTPUT FAST READ
        Public QUAD_READ As Byte = &H6B 'QUAD OUTPUT FAST READ
        Public QUAD_PROG As Byte = &H32 'QUAD INPUT PROGRAM
        Public DUAL_PROG As Byte = &HA2 'DUAL INPUT PROGRAM
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

    Public Interface Device
        ReadOnly Property NAME As String 'Manufacturer and part number
        Property FLASH_TYPE As MemoryType
        ReadOnly Property IFACE As VCC_IF 'The type of VCC and Interface
        ReadOnly Property FLASH_SIZE As Long 'Size of this flash device (without spare area)
        ReadOnly Property MFG_CODE As Byte 'The manufaturer byte ID
        ReadOnly Property ID1 As UInt16
        Property ID2 As UInt16
        ReadOnly Property PAGE_SIZE As UInt16 'Size of the pages
        ReadOnly Property Sector_Count As Integer  'Total number of blocks or sectors this flash device has
        Property ERASE_REQUIRED As Boolean 'Indicates that the sector/block must be erased prior to writing
    End Interface

    Public Class OTP_EPROM : Implements Device
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public Property IFACE As VCC_IF Implements Device.IFACE
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property ID1 As UInt16 Implements Device.ID1
        Public Property ID2 As UInt16 = 0 Implements Device.ID2 'Not used
        Public Property FLASH_TYPE As MemoryType = MemoryType.OTP_EPROM Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public ReadOnly Property Sector_Count As Integer = 1 Implements Device.Sector_Count 'We will have to write the entire array
        Public Property PAGE_SIZE As UInt16 = 0 Implements Device.PAGE_SIZE 'Not used
        Public Property ERASE_REQUIRED As Boolean = False Implements Device.ERASE_REQUIRED
        Public Property IS_BLANK As Boolean = False 'On init, do blank check
        Public Property HARDWARE_DELAY As UInt16 = 50 'uS wait after each word program

        Public Property WR_OE_HIGH As Boolean = True 'Most devices use OE HIGH during write operations


        Sub New(f_name As String, vcc As VCC_IF, MFG As Byte, ID1 As UInt16, f_size As UInt32)
            Me.NAME = f_name
            Me.IFACE = vcc
            Me.MFG_CODE = MFG
            Me.ID1 = ID1
            Me.FLASH_SIZE = f_size
        End Sub

    End Class
    'Parallel NOR / Multi-purpose Flash
    Public Class P_NOR : Implements Device
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property IFACE As VCC_IF Implements Device.IFACE
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property ID1 As UInt16 Implements Device.ID1
        Public Property ID2 As UInt16 Implements Device.ID2
        Public Property FLASH_TYPE As MemoryType = MemoryType.PARALLEL_NOR Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public Property PAGE_SIZE As UInt16 = 32 Implements Device.PAGE_SIZE 'Only used for WRITE_PAGE mode of certain flash devices
        Public Property ERASE_REQUIRED As Boolean = True Implements Device.ERASE_REQUIRED
        Public Property VENDOR_SPECIFIC As VENDOR_FEATURE = VENDOR_FEATURE.NotSupported 'Indicates we can load a unique vendor specific tab
        Public Property WriteMode As MFP_PRG = MFP_PRG.Standard 'This indicates the perfered programing method
        Public Property RESET_ENABLED As Boolean = True 'Indicates if we will call reset/read mode op code
        Public Property HARDWARE_DELAY As UInt16 = 10 'Number of hardware uS/mS to wait between write operations
        Public Property SOFTWARE_DELAY As UInt16 = 100 'Number of software ms to wait between write operations
        Public Property ERASE_DELAY As UInt16 = 0 'Number of ms to wait after an erase operation
        Public Property DELAY_MODE As MFP_DELAY = MFP_DELAY.uS
        Public Property DUAL_DIE As Boolean = False 'Package contains two memory die
        Public Property CE2 As Integer 'The Axx pin to use for the second CE control

        Sub New(FlashName As String, MFG As Byte, ID1 As UInt16, Size As Long, f_if As VCC_IF, block_layout As BLKLYT, write_mode As MFP_PRG, delay_mode As MFP_DELAY, Optional ID2 As UInt16 = 0)
            Me.NAME = FlashName
            Me.MFG_CODE = MFG
            Me.ID1 = ID1
            Me.ID2 = ID2
            Me.FLASH_SIZE = Size
            Me.IFACE = f_if
            Me.WriteMode = write_mode
            Me.DELAY_MODE = delay_mode
            Dim blocks As Int32 = CInt(Size / Kb512)
            Select Case block_layout
                Case BLKLYT.Four_Top
                    AddSector(Kb512, CInt(blocks - 1))
                    AddSector(Kb256, 1)
                    AddSector(Kb064, 2)
                    AddSector(Kb128, 1)
                Case BLKLYT.Two_Top
                    AddSector(Kb512, blocks - 1)
                    AddSector(Kb064, 8)
                Case BLKLYT.Four_Btm
                    AddSector(Kb128, 1) '16KB
                    AddSector(Kb064, 2) '8KB
                    AddSector(Kb256, 1) '32KB
                    AddSector(Kb512, blocks - 1) '64KB
                Case BLKLYT.Two_Btm
                    AddSector(Kb064, 8)
                    AddSector(Kb512, blocks - 1)
                Case BLKLYT.P30_Top
                    AddSector(Mb001, CInt(Size \ Mb001) - 1)
                    AddSector(Kb256, 4)
                Case BLKLYT.P30_Btm
                    AddSector(Kb256, 4)
                    AddSector(Mb001, CInt(Size \ Mb001) - 1)
                Case BLKLYT.Dual 'this device has small boot blocks on the top and bottom of the device
                    AddSector(Kb064, 8) 'bottom block
                    AddSector(Kb512, blocks - 2)
                    AddSector(Kb064, 8) 'top block
                Case BLKLYT.Kb016_Uni
                    AddUniformSector(Kb016)
                Case BLKLYT.Kb032_Uni
                    AddUniformSector(Kb032)
                Case BLKLYT.Kb064_Uni
                    AddUniformSector(Kb064)
                Case BLKLYT.Kb128_Uni
                    AddUniformSector(Kb128)
                Case BLKLYT.Kb256_Uni
                    AddUniformSector(Kb256)
                Case BLKLYT.Kb512_Uni
                    AddUniformSector(Kb512)
                Case BLKLYT.Mb001_Uni
                    AddUniformSector(Mb001)
                Case BLKLYT.Mb002_NonUni
                    AddSector(Mb001) 'Main Block
                    AddSector(98304) 'Main Block
                    AddSector(Kb064) 'Parameter Block
                    AddSector(Kb064) 'Parameter Block
                    AddSector(Kb128) 'Boot Block
                Case BLKLYT.Mb032_NonUni
                    AddSector(Kb064, 8)
                    AddSector(Kb512, 1)
                    AddSector(Mb001, 31)
                Case BLKLYT.Mb016_Samsung
                    AddSector(Kb064, 8) '8192    65536
                    AddSector(Kb512, 3) '65536   196608
                    AddSector(Mb002, 6) '262144  7864320
                    AddSector(Kb512, 3) '65536   196608
                    AddSector(Kb064, 8) '8192    65536
                Case BLKLYT.Mb032_Samsung
                    AddSector(Kb064, 8)  '8192    65536
                    AddSector(Kb512, 3)  '65536   196608
                    AddSector(Mb002, 14) '262144 
                    AddSector(Kb512, 3)  '65536   196608
                    AddSector(Kb064, 8) '8192    65536
                Case BLKLYT.Mb064_Samsung
                    AddSector(Kb064, 8)  '8192    65536
                    AddSector(Kb512, 3)  '65536   196608
                    AddSector(Mb002, 30) '262144  7864320
                    AddSector(Kb512, 3)  '65536   196608
                    AddSector(Kb064, 8) '8192    65536
                Case BLKLYT.Mb128_Samsung
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
                Case BLKLYT.Mb256_Samsung
                    AddSector(Kb512, 4)   '65536    262144
                    AddSector(Mb002, 126) '262144   33030144
                    AddSector(Kb512, 4)   '65536    262144
                Case BLKLYT.Four_Top_B5
                    blocks = CInt(CUInt(Size) / Mb001)
                    AddSector(Kb128, 1) '16KB
                    AddSector(Kb064, 2) '8KB
                    AddSector(Kb512 + Kb256, 1) '32KB
                    AddSector(Mb001, blocks - 1) '64KB
                Case BLKLYT.Four_Btm_B5
                    blocks = CInt(CUInt(Size) / Mb001)
                    AddSector(Kb128, 1) '16KB
                    AddSector(Kb064, 2) '8KB
                    AddSector(Kb512 + Kb256, 1) '32KB
                    AddSector(Mb001, blocks - 1) '128KB
                Case BLKLYT.Sharp_Top
                    AddSector(Kb512, blocks - 1) '64KB
                    AddSector(Kb064, 8) '8KB
                Case BLKLYT.Sharp_Btm
                    AddSector(Kb064, 8) '8KB
                    AddSector(Kb512, blocks - 1) '64KB
                Case BLKLYT.EntireDevice
                    AddSector(CInt(Size))
            End Select
        End Sub

#Region "Sectors"
        Private SectorList As New List(Of Integer)

        Public Sub AddSector(SectorSize As Integer)
            SectorList.Add(SectorSize)
        End Sub

        Public Sub AddSector(SectorSize As Integer, Count As Integer)
            For i As Integer = 1 To Count
                SectorList.Add(SectorSize)
            Next
        End Sub

        Public Sub AddUniformSector(uniform_block As Integer)
            Dim TotalSectors As Integer = CInt(Me.FLASH_SIZE \ uniform_block)
            For i As Integer = 1 To TotalSectors
                SectorList.Add(uniform_block)
            Next
        End Sub

        Public Function GetSectorSize(SectorIndex As Integer) As Integer
            Try
                Return SectorList(SectorIndex)
            Catch ex As Exception
            End Try
            Return 0UI
        End Function

        Public ReadOnly Property Sector_Count As Integer Implements Device.Sector_Count
            Get
                Return SectorList.Count
            End Get
        End Property

#End Region

        Public Overrides Function ToString() As String
            Return Me.NAME
        End Function

    End Class

    Public Class SPI_NOR : Implements ISerializable : Implements Device
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property IFACE As VCC_IF Implements Device.IFACE
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property ID1 As UInt16 Implements Device.ID1
        Public Property ID2 As UInt16 Implements Device.ID2
        Public Property FAMILY As Byte 'SPI Extended byte
        Public Property FLASH_TYPE As MemoryType Implements Device.FLASH_TYPE
        Public Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public Property ERASE_REQUIRED As Boolean Implements Device.ERASE_REQUIRED
        'These are properties unique to SPI devices
        Public Property ADDRESSBITS As UInt32 = 24 'Number of bits the address space takes up (16/24/32)
        Public Property SQI_MODE As SPI_QUAD_SUPPORT = SPI_QUAD_SUPPORT.NO_QUAD
        Public Property ProgramMode As SPI_PROG
        Public Property STACKED_DIES As UInt32 = 1 'If device has more than one die, set this value
        Public Property SEND_EN4B As Boolean = False 'Set to True to send the EN4BSP
        Public Property SEND_RDFS As Boolean = False 'Set to True to read the flag status register
        Public Property ERASE_SIZE As UInt32 = &H10000 'Number of bytes per page that are erased(typically 64KB)
        Public Property VENDOR_SPECIFIC As VENDOR_FEATURE = VENDOR_FEATURE.NotSupported 'Indicates we can load a unique vendor specific tab
        Public Property SPI_DUMMY As Byte = 8 'Number of cycles after read in SPI operation
        Public Property SQI_DUMMY As Byte = 10 'Number of cycles after read in QUAD SPI operation
        Public Property CHIP_ERASE As EraseMethod = EraseMethod.Standard 'How to erase the entire device
        Public Property SEND_EWSR As Boolean = False 'Set to TRUE to write the enable write status-register
        Public Property PAGE_SIZE As UInt16 = 256 Implements Device.PAGE_SIZE 'Number of bytes per page
        Public Property PAGE_SIZE_EXTENDED As UInt16 'Number of bytes in the extended page

        Public OP_COMMANDS As New SPI_OPCODES 'Contains a list of op-codes used to read/write/erase

        Public ReadOnly Property PAGE_COUNT As UInt32 'The total number of pages this flash contains
            Get
                If Me.FLASH_SIZE = 0UI Or Me.PAGE_SIZE = 0UI Then Return 0UI
                Return CUInt(Me.FLASH_SIZE \ Me.PAGE_SIZE)
            End Get
        End Property

        Protected Sub New(info As SerializationInfo, context As StreamingContext)
            Me.NAME = info.GetString("m_name")
            Me.IFACE = CType(info.GetValue("m_iface", GetType(VCC_IF)), VCC_IF)
            Me.FLASH_SIZE = info.GetUInt32("m_flash_size")
            Me.ERASE_SIZE = info.GetUInt32("m_erase_size")
            Me.PAGE_SIZE = info.GetUInt16("m_page_size")
            Me.ProgramMode = CType(info.GetByte("m_prog_mode"), SPI_PROG)
            Me.SEND_EWSR = info.GetBoolean("m_send_ewsr")
            Me.SEND_EN4B = info.GetBoolean("m_send_4byte")
            Me.PAGE_SIZE_EXTENDED = info.GetUInt16("m_page_ext")
            Me.ERASE_REQUIRED = info.GetBoolean("m_erase_req")
            Me.ADDRESSBITS = info.GetUInt32("m_addr_size")
            Me.OP_COMMANDS.READ = info.GetByte("m_op_rd")
            Me.OP_COMMANDS.PROG = info.GetByte("m_op_wr")
            Me.OP_COMMANDS.SE = info.GetByte("m_op_se")
            Me.OP_COMMANDS.WREN = info.GetByte("m_op_we")
            Me.OP_COMMANDS.BE = info.GetByte("m_op_be")
            Me.OP_COMMANDS.RDSR = info.GetByte("m_op_rdsr")
            Me.OP_COMMANDS.WRSR = info.GetByte("m_op_wrsr")
            Me.OP_COMMANDS.EWSR = info.GetByte("m_op_ewsr")
        End Sub

        Public Sub GetObjectData(info As SerializationInfo, context As StreamingContext) Implements ISerializable.GetObjectData
            info.AddValue("m_name", Me.NAME, GetType(String))
            info.AddValue("m_iface", Me.IFACE, GetType(VCC_IF))
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

        Sub New(name As String, vcc As VCC_IF, size As UInt32, MFG As Byte, ID1 As UInt16)
            Me.NAME = name
            Me.IFACE = vcc
            Me.FLASH_SIZE = CLng(size)
            Me.MFG_CODE = MFG
            Me.ID1 = ID1
            If (size > Mb128) Then Me.ADDRESSBITS = 32
            Me.ERASE_REQUIRED = True
            Me.FLASH_TYPE = MemoryType.SERIAL_NOR
        End Sub

        Sub New(name As String, vcc As VCC_IF, size As UInt32, MFG As Byte, ID1 As UInt16, ERASECMD As Byte, ERASESIZE As UInt32)
            Me.NAME = name
            Me.IFACE = vcc
            Me.FLASH_SIZE = size
            Me.MFG_CODE = MFG
            Me.ID1 = ID1
            Me.OP_COMMANDS.SE = ERASECMD 'Sometimes 0xD8 or 0x20
            Me.ERASE_SIZE = ERASESIZE
            If (size > Mb128) Then Me.ADDRESSBITS = 32
            Me.ERASE_REQUIRED = True
            Me.FLASH_TYPE = MemoryType.SERIAL_NOR
        End Sub
        '32-bit setup command
        Sub New(name As String, f_if As VCC_IF, size As UInt32, MFG As Byte, ID1 As UInt16, ERASECMD As Byte, ERASESIZE As UInt32, READCMD As Byte, FASTCMD As Byte, WRITECMD As Byte)
            Me.NAME = name
            Me.IFACE = f_if
            Me.FLASH_SIZE = size
            Me.PAGE_SIZE = 256
            Me.MFG_CODE = MFG
            Me.ID1 = ID1
            Me.OP_COMMANDS.SE = ERASECMD 'Sometimes 0xD8 or 0x20
            Me.OP_COMMANDS.READ = READCMD
            Me.OP_COMMANDS.FAST_READ = FASTCMD
            Me.OP_COMMANDS.PROG = WRITECMD
            Me.ERASE_SIZE = ERASESIZE
            If (size > Mb128) Then Me.ADDRESSBITS = 32
            Me.ERASE_REQUIRED = True
            Me.FLASH_TYPE = MemoryType.SERIAL_NOR
        End Sub

        Sub New(name As String, size As Integer, page_size As UInt16, addr_bits As UInt32, PROG As SPI_PROG)
            If PROG = SPI_PROG.Nordic Then
                Me.ERASE_REQUIRED = True
                Me.OP_COMMANDS.SE = &H52
                Me.ERASE_SIZE = 512
            Else
                Me.ERASE_REQUIRED = False
            End If
            Me.NAME = name
            Me.IFACE = VCC_IF.SERIAL_3V
            Me.FLASH_SIZE = CLng(size)
            Me.PAGE_SIZE = page_size
            Me.ADDRESSBITS = addr_bits
            Me.ProgramMode = PROG
            Me.FLASH_TYPE = MemoryType.SERIAL_NOR
        End Sub

        'Returns the amounts of bytes needed to indicate device address (usually 3 or 4 bytes)
        Public ReadOnly Property AddressBytes() As Integer
            Get
                Return CInt(Math.Ceiling(ADDRESSBITS / 8))
            End Get
        End Property

        Public ReadOnly Property Sector_Count As Integer Implements Device.Sector_Count
            Get
                If Me.ERASE_REQUIRED Then
                    Return CInt(FLASH_SIZE \ ERASE_SIZE)
                Else
                    Return 1 'EEPROM do not have sectors
                End If
            End Get
        End Property

    End Class
    'Generic NAND
    Public Class G_NAND : Implements Device
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property IFACE As VCC_IF Implements Device.IFACE
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property ID1 As UInt16 Implements Device.ID1
        Public Property ID2 As UInt16 Implements Device.ID2
        Public Property FLASH_TYPE As MemoryType Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public ReadOnly Property PAGE_SIZE As UInt16 Implements Device.PAGE_SIZE 'Number of bytes in a normal page area
        Public Property ERASE_REQUIRED As Boolean Implements Device.ERASE_REQUIRED
        Public ReadOnly Property PAGE_EXT As UInt16 'Extended number of bytes
        Public ReadOnly Property PAGE_COUNT As UInt16 'Number of pages in a block
        Public ReadOnly Property BLOCK_COUNT As Integer Implements Device.Sector_Count 'Number of blocks in this device

        Public ReadOnly Property Block_Size() As Integer  'Returns the total size of the block (including all spare area)
            Get
                Return (CInt(PAGE_COUNT) * CInt(PAGE_SIZE + PAGE_EXT))
            End Get
        End Property

        Sub New(FlashName As String, MFG As Byte, ID As UInt32, PageSize As UInt16, SpareSize As UInt16, PageCount As UInt16, BlockCount As UInt16, vcc As VCC_IF)
            While ((ID And &HFF000000UI) = 0)
                ID = (ID << 8)
            End While
            Me.NAME = FlashName
            Me.MFG_CODE = MFG
            Me.ID1 = CUShort(ID >> 16)
            Me.ID2 = CUShort(ID And &HFFFF)
            Me.FLASH_TYPE = MemoryType.PARALLEL_NAND
            Me.PAGE_SIZE = PageSize 'Does not include extended / spare pages
            Me.PAGE_EXT = SpareSize
            Me.PAGE_COUNT = PageCount
            Me.BLOCK_COUNT = BlockCount
            Me.FLASH_SIZE = (CLng(PageSize) * CLng(PageCount) * CLng(BlockCount)) 'Does not include extended /spare areas
            Me.ERASE_REQUIRED = True
            Me.IFACE = vcc
        End Sub

        Public Overrides Function ToString() As String
            Return Me.NAME & " (" & GetDataSizeString(Me.FLASH_SIZE) & ")"
        End Function

    End Class
    'Serial NAND
    Public Class SPI_NAND : Inherits G_NAND
        Public ReadOnly Property PLANE_SELECT As Boolean 'Indicates that this device needs to select a plane when accessing pages
        Public Property READ_CMD_DUMMY As Boolean = False 'Write a dummy byte after read command
        Public Property STACKED_DIES As Integer = 1 'If device has more than one die, set this value

        Sub New(FlashName As String, MFG As Byte, ID As UInt32, PageSize As UInt16, SpareSize As UInt16, PageCount As UInt16, BlockCount As UInt16, plane_select As Boolean, vcc As VCC_IF)
            MyBase.New(FlashName, MFG, ID, PageSize, SpareSize, PageCount, BlockCount, vcc)
            Me.PLANE_SELECT = plane_select
            Me.FLASH_TYPE = MemoryType.SERIAL_NAND
        End Sub

    End Class
    'Parallel NAND
    Public Class P_NAND : Inherits G_NAND
        Public Property ADDRESS_SCHEME As PAGE_TYPE
        Public Property DIE_COUNT As Integer = 1

        Sub New(FlashName As String, MFG As Byte, ID As UInt32, PageSize As UInt16, SpareSize As UInt16, PageCount As UInt16, BlockCount As UInt16, vcc As VCC_IF)
            MyBase.New(FlashName.TrimEnd, MFG, ID, PageSize, SpareSize, PageCount, BlockCount, vcc)
            If PageSize = 512 Then
                Me.ADDRESS_SCHEME = PAGE_TYPE.SMALL
            Else
                Me.ADDRESS_SCHEME = PAGE_TYPE.LARGE
            End If
            Me.FLASH_TYPE = MemoryType.PARALLEL_NAND
        End Sub

        Public Enum PAGE_TYPE As Integer
            SMALL = 1
            LARGE = 2
            SANDISK = 3
        End Enum

    End Class

    Public Class FWH : Implements Device
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property IFACE As VCC_IF Implements Device.IFACE
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property ID1 As UInt16 Implements Device.ID1
        Public Property ID2 As UInt16 Implements Device.ID2
        Public Property FLASH_TYPE As MemoryType = MemoryType.FWH_NOR Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public Property PAGE_SIZE As UInt16 = 32 Implements Device.PAGE_SIZE 'Only used for WRITE_PAGE mode of certain flash devices
        Public Property ERASE_REQUIRED As Boolean = True Implements Device.ERASE_REQUIRED
        Public ReadOnly Property SECTOR_SIZE As Integer
        Public ReadOnly Property Sector_Count As Integer Implements Device.Sector_Count

        Public ReadOnly ERASE_CMD As Byte

        Sub New(f_name As String, MFG As Byte, ID1 As UInt16, f_size As UInt32, sector_size As Integer, sector_erase As Byte)
            Me.NAME = f_name
            Me.MFG_CODE = MFG
            Me.ID1 = ID1
            Me.ID2 = ID2
            Me.FLASH_SIZE = f_size
            Me.SECTOR_SIZE = sector_size
            Me.Sector_Count = CInt(f_size \ sector_size)
            Me.ERASE_CMD = sector_erase
        End Sub

    End Class

    Public Class SWI : Implements Device
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property IFACE As VCC_IF Implements Device.IFACE
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property ID1 As UInt16 Implements Device.ID1
        Public Property ID2 As UInt16 Implements Device.ID2
        Public Property FLASH_TYPE As MemoryType = MemoryType.SERIAL_SWI Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public Property PAGE_SIZE As UInt16 Implements Device.PAGE_SIZE 'Only used for WRITE_PAGE mode of certain flash devices
        Public Property ERASE_REQUIRED As Boolean = False Implements Device.ERASE_REQUIRED
        Public ReadOnly Property Sector_Count As Integer Implements Device.Sector_Count

        Sub New(device_name As String, MFG As Byte, ID1 As UInt16, mem_size As Integer, page_size As UInt16)
            Me.NAME = device_name
            Me.MFG_CODE = MFG
            Me.ID1 = ID1
            Me.FLASH_SIZE = mem_size
            Me.PAGE_SIZE = page_size
            Me.ERASE_REQUIRED = False
            Me.Sector_Count = 0
            Me.IFACE = VCC_IF.SERIAL_2V7_5V
        End Sub

    End Class

    Public Class I2C_DEVICE : Implements Device
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property IFACE As VCC_IF Implements Device.IFACE
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property ID1 As UInt16 Implements Device.ID1
        Public Property ID2 As UInt16 Implements Device.ID2
        Public Property FLASH_TYPE As MemoryType = MemoryType.SERIAL_I2C Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public Property PAGE_SIZE As UInt16 Implements Device.PAGE_SIZE 'Number of bytes written per write operation (8 to 256 bytes)
        Public Property ERASE_REQUIRED As Boolean = False Implements Device.ERASE_REQUIRED
        Public ReadOnly Property Sector_Count As Integer Implements Device.Sector_Count
        Public Property Delay As Integer = 5 'In Miliseconds

        Public ReadOnly Property AddressSize As Byte

        Sub New(DisplayName As String, SizeInBytes As UInt32, EEAddrSize As Byte, EEPageSize As UInt16)
            Me.NAME = DisplayName
            Me.FLASH_SIZE = SizeInBytes
            Me.AddressSize = EEAddrSize 'Number of bytes that are used to store the address
            Me.PAGE_SIZE = EEPageSize
        End Sub

    End Class

    Public Class HYPERFLASH : Implements Device
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property IFACE As VCC_IF Implements Device.IFACE
        Public ReadOnly Property MFG_CODE As Byte Implements Device.MFG_CODE
        Public ReadOnly Property ID1 As UInt16 Implements Device.ID1
        Public Property ID2 As UInt16 Implements Device.ID2 'NOT USED
        Public Property FLASH_TYPE As MemoryType = MemoryType.HYPERFLASH Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public Property PAGE_SIZE As UInt16 = 512 Implements Device.PAGE_SIZE 'Only used for WRITE_PAGE mode of certain flash devices
        Public ReadOnly Property SECTOR_SIZE As Integer
        Public ReadOnly Property Sector_Count As Integer Implements Device.Sector_Count
        Public Property ERASE_REQUIRED As Boolean = True Implements Device.ERASE_REQUIRED

        Sub New(F_NAME As String, MFG As Byte, ID1 As UInt16, f_size As UInt32)
            Me.NAME = F_NAME
            Me.MFG_CODE = MFG
            Me.ID1 = ID1
            Me.ID2 = ID2
            Me.FLASH_SIZE = f_size
            Me.SECTOR_SIZE = Mb002
            Me.Sector_Count = CInt(f_size \ Me.SECTOR_SIZE)
        End Sub

    End Class

    Public Class MICROWIRE : Implements Device
        Public ReadOnly Property NAME As String Implements Device.NAME
        Public ReadOnly Property IFACE As VCC_IF Implements Device.IFACE
        Public ReadOnly Property MFG_CODE As Byte = 0 Implements Device.MFG_CODE 'NOT USED
        Public ReadOnly Property ID1 As UInt16 = 0 Implements Device.ID1 'NOT USED 
        Public Property ID2 As UInt16 Implements Device.ID2 'NOT USED
        Public Property FLASH_TYPE As MemoryType = MemoryType.SERIAL_MICROWIRE Implements Device.FLASH_TYPE
        Public ReadOnly Property FLASH_SIZE As Long Implements Device.FLASH_SIZE
        Public Property PAGE_SIZE As UInt16 = 0 Implements Device.PAGE_SIZE
        Public ReadOnly Property SECTOR_SIZE As UInt32 = 0
        Public ReadOnly Property Sector_Count As Integer = 0 Implements Device.Sector_Count
        Public Property ERASE_REQUIRED As Boolean = False Implements Device.ERASE_REQUIRED
        'Microwire specific options
        Public ReadOnly Property X8_ADDRSIZE As Byte = 0 '0=Means not supported
        Public ReadOnly Property X16_ADDRSIZE As Byte

        Sub New(F_NAME As String, F_SIZE As UInt32, X8_ADDRS As Byte, X16_ADDRS As Byte)
            Me.NAME = F_NAME
            Me.FLASH_SIZE = F_SIZE
            Me.X8_ADDRSIZE = X8_ADDRS
            Me.X16_ADDRSIZE = X16_ADDRS
        End Sub

        Public Overrides Function ToString() As String
            Return Me.NAME
        End Function

    End Class

    Public Class NAND_ONFI
        Public ReadOnly Property IS_VALID As Boolean = False
        Public ReadOnly Property DEVICE_MFG As String
        Public ReadOnly Property DEVICE_MODEL As String
        Public ReadOnly Property PAGE_SIZE As UInt32
        Public ReadOnly Property SPARE_SIZE As UInt16
        Public ReadOnly Property PAGES_PER_BLOCK As UInt32
        Public ReadOnly Property BLOCKS_PER_LUN As UInt32
        Public ReadOnly Property LUN_COUNT As UInt32 'CE_x
        Public ReadOnly Property BITS_PER_CELL As Integer

        Sub New(onfi_table() As Byte)
            Try
                If onfi_table Is Nothing OrElse Not onfi_table.Length = 256 Then Exit Sub
                If Utilities.Bytes.ToChrString(onfi_table.Slice(0, 4)).Equals("ONFI") Then
                    Me.DEVICE_MFG = Utilities.Bytes.ToChrString(onfi_table.Slice(32, 12)).Trim
                    Me.DEVICE_MODEL = Utilities.Bytes.ToChrString(onfi_table.Slice(44, 20)).Trim
                    Me.PAGE_SIZE = Utilities.Bytes.ToUInt32(onfi_table.Slice(80, 4).Reverse())
                    Me.SPARE_SIZE = Utilities.Bytes.ToUInt16(onfi_table.Slice(84, 2).Reverse())
                    Me.PAGES_PER_BLOCK = Utilities.Bytes.ToUInt32(onfi_table.Slice(92, 4).Reverse())
                    Me.BLOCKS_PER_LUN = Utilities.Bytes.ToUInt32(onfi_table.Slice(96, 4).Reverse())
                    Me.LUN_COUNT = onfi_table(100) 'Indicates how many CE this device has
                    Me.BITS_PER_CELL = onfi_table(102)
                    Me.IS_VALID = True
                End If
            Catch ex As Exception
            End Try
        End Sub

    End Class

    Public Class NOR_CFI
        Public ReadOnly Property IS_VALID As Boolean = False

        Public ReadOnly Property VCC_MIN_PROGERASE As Single
        Public ReadOnly Property VCC_MAX_PROGERASE As Single
        Public ReadOnly Property VPP_MIN_PROGERASE As Single
        Public ReadOnly Property VPP_MAX_PROGERASE As Single
        Public ReadOnly Property WORD_WRITE_TIMEOUT As Integer 'Typical, in uS
        Public ReadOnly Property BUFFER_WRITE_TIMEOUT As Integer 'Typical, in uS
        Public ReadOnly Property BLOCK_ERASE_TIMEOUT As Integer 'Typical, in ms
        Public ReadOnly Property ERASE_TIMEOUT As Integer 'Typical, in ms
        Public ReadOnly Property WORD_WRITE_MAX_TIMEOUT As Integer
        Public ReadOnly Property BUFFER_WRITE_MAX_TIMEOUT As Integer
        Public ReadOnly Property BLOCK_ERASE_MAX_TIMEOUT As Integer 'in seconds
        Public ReadOnly Property ERASE_MAX_TIMEOUT As Integer 'in seconds
        Public ReadOnly Property DEVICE_SIZE As UInt32
        Public ReadOnly Property DESCRIPTION As String
        Public ReadOnly Property WRITE_BUFFER_SIZE As Integer

        Sub New(cfi_table() As Byte)
            Try
                If cfi_table Is Nothing OrElse Not cfi_table.Length = 32 Then Exit Sub
                If Utilities.Bytes.ToChrString(cfi_table.Slice(0, 3)).Equals("QRY") Then
                    Me.VCC_MIN_PROGERASE = CSng((cfi_table(11) >> 4).ToString & "." & (cfi_table(11) And 15).ToString)
                    Me.VCC_MAX_PROGERASE = CSng((cfi_table(12) >> 4).ToString & "." & (cfi_table(12) And 15).ToString)
                    Me.VPP_MIN_PROGERASE = CSng((cfi_table(13) >> 4).ToString & "." & (cfi_table(13) And 15).ToString)
                    Me.VPP_MAX_PROGERASE = CSng((cfi_table(14) >> 4).ToString & "." & (cfi_table(14) And 15).ToString)
                    Me.WORD_WRITE_TIMEOUT = CInt(2 ^ cfi_table(15)) '0x1F
                    Me.BUFFER_WRITE_TIMEOUT = CInt(2 ^ cfi_table(16)) '0x20
                    Me.BLOCK_ERASE_TIMEOUT = CInt(2 ^ cfi_table(17)) '0x21
                    Me.ERASE_TIMEOUT = CInt(2 ^ cfi_table(18)) '0x22
                    Me.WORD_WRITE_MAX_TIMEOUT = CInt(2 ^ cfi_table(15)) * CInt(2 ^ cfi_table(19)) '0x23
                    Me.BUFFER_WRITE_MAX_TIMEOUT = CInt(2 ^ cfi_table(16)) * CInt(2 ^ cfi_table(20)) '0x24
                    Me.BLOCK_ERASE_MAX_TIMEOUT = CInt(2 ^ cfi_table(21)) '0x25
                    Me.ERASE_MAX_TIMEOUT = CInt(2 ^ cfi_table(22)) '0x26
                    Me.DEVICE_SIZE = CUInt(2 ^ CUInt(cfi_table(23))) '0x27
                    Me.DESCRIPTION = CStr((New String() {"X8 ONLY", "X16 ONLY", "X8/X16", "X32", "SPI"}).GetValue(cfi_table(24))) '0x28
                    Me.WRITE_BUFFER_SIZE = CInt(2 ^ cfi_table(26)) '0x2A
                    Me.IS_VALID = True
                End If
            Catch ex As Exception
                Me.IS_VALID = False
            End Try
        End Sub

    End Class

    Public Class FlashDatabase
        Public FlashDB As New List(Of Device)

        Private Const SPI_1V8 As VCC_IF = VCC_IF.SERIAL_1V8
        Private Const SPI_2V5 As VCC_IF = VCC_IF.SERIAL_2V5
        Private Const SPI_3V As VCC_IF = VCC_IF.SERIAL_3V
        Private Const QUAD As SPI_QUAD_SUPPORT = SPI_QUAD_SUPPORT.QUAD
        Private Const SPI_QUAD As SPI_QUAD_SUPPORT = SPI_QUAD_SUPPORT.SPI_QUAD
        Private Const AAI_Word As SPI_PROG = SPI_PROG.AAI_Word
        Private Const AAI_Byte As SPI_PROG = SPI_PROG.AAI_Byte

        Sub New()
            SPINOR_Database() 'Adds all of the SPI and QSPI devices
            SPINAND_Database() 'Adds all of the SPI NAND devices
            MFP_Database() 'Adds all of the TSOP/PLCC etc. devices
            SPI_EEPROM_Database() 'Adds serial / parallel EEPROM devices
            NAND_Database() 'Adds all of the parallel NAND compatible devices
            OTP_Database() 'Adds all of the OTP EPROM devices
            FWH_Database() 'Adds all of the firmware hub devices
            MICROWIRE_Database() 'Adds all of the micriwire devices

            VENDOR_SPECIFIC() 'Add device specific features

            'Add HyperFlash devices
            FlashDB.Add(New HYPERFLASH("Cypress S26KS128S", &H1, &H7E74, Mb128)) '1.8V
            FlashDB.Add(New HYPERFLASH("Cypress S26KS256S", &H1, &H7E72, Mb256)) '1.8V
            FlashDB.Add(New HYPERFLASH("Cypress S26KS512S", &H1, &H7E70, Mb512)) '1.8V
            FlashDB.Add(New HYPERFLASH("Cypress S26KL128S", &H1, &H7E73, Mb128)) '3.3V
            FlashDB.Add(New HYPERFLASH("Cypress S26KL256S", &H1, &H7E71, Mb256)) '3.3V
            FlashDB.Add(New HYPERFLASH("Cypress S26KL512S", &H1, &H7E6F, Mb512)) '3.3V

        End Sub

        Private Sub VENDOR_SPECIFIC()
            Dim MT25QL02GC As SPI_NOR = CType(FindDevice(&H20, &HBA22, 0, MemoryType.SERIAL_NOR), SPI_NOR)
            MT25QL02GC.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron 'Adds the non-vol tab to the GUI
            MT25QL02GC.SEND_RDFS = True 'Will read the flag-status register after a erase/programer opertion
            MT25QL02GC.CHIP_ERASE = EraseMethod.Micron   'Will erase all of the sectors instead
            Dim N25Q00AA_3V As SPI_NOR = CType(FindDevice(&H20, &HBA21, 0, MemoryType.SERIAL_NOR), SPI_NOR)
            N25Q00AA_3V.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron 'Adds the non-vol tab to the GUI
            N25Q00AA_3V.SEND_RDFS = True 'Will read the flag-status register after a erase/programer opertion
            N25Q00AA_3V.CHIP_ERASE = EraseMethod.Micron  'Will erase all of the sectors instead
            N25Q00AA_3V.STACKED_DIES = 4
            Dim N25Q00AA_1V8 As SPI_NOR = CType(FindDevice(&H20, &HBB21, 0, MemoryType.SERIAL_NOR), SPI_NOR) 'CV
            N25Q00AA_1V8.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron 'Adds the non-vol tab to the GUI
            N25Q00AA_1V8.SEND_RDFS = True 'Will read the flag-status register after a erase/programer opertion
            N25Q00AA_1V8.CHIP_ERASE = EraseMethod.Micron  'Will erase all of the sectors instead
            N25Q00AA_1V8.STACKED_DIES = 4
            Dim N25Q512 As SPI_NOR = CType(FindDevice(&H20, &HBA20, 0, MemoryType.SERIAL_NOR), SPI_NOR)
            N25Q512.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron 'Adds the non-vol tab to the GUI
            N25Q512.SEND_RDFS = True 'Will read the flag-status register after a erase/programer opertion
            N25Q512.CHIP_ERASE = EraseMethod.Micron 'Will erase all of the sectors instead
            Dim N25Q256 As SPI_NOR = CType(FindDevice(&H20, &HBA19, 0, MemoryType.SERIAL_NOR), SPI_NOR)
            N25Q256.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron 'Adds the non-vol tab to the GUI
            N25Q256.SEND_RDFS = True 'Will read the flag-status register after a erase/programer opertion
            Dim N25Q256A As SPI_NOR = CType(FindDevice(&H20, &HBB19, 0, MemoryType.SERIAL_NOR), SPI_NOR) '1.8V version
            N25Q256A.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron 'Adds the non-vol tab to the GUI
            N25Q256A.SEND_RDFS = True 'Will read the flag-status register after a erase/programer opertion
            N25Q256A.OP_COMMANDS.QUAD_PROG = &H12

            Dim S25FL128S As SPI_NOR = CType(FindDevice(&H1, &H2018, 0, MemoryType.SERIAL_NOR), SPI_NOR)
            S25FL128S.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL
            S25FL128S.SQI_MODE = QUAD
            Dim S25FL129P As SPI_NOR = CType(FindDevice(&H1, &H2018, &H4D01, MemoryType.SERIAL_NOR), SPI_NOR)
            S25FL129P.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL
            S25FL129P.SQI_MODE = QUAD
            S25FL129P = CType(FindDevice(&H1, &H2018, &H4D00, MemoryType.SERIAL_NOR), SPI_NOR)
            S25FL129P.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL
            S25FL129P.SQI_MODE = QUAD
            Dim S25FL116K As SPI_NOR = CType(FindDevice(&H1, &H4015, 0, MemoryType.SERIAL_NOR), SPI_NOR)
            S25FL116K.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL
            S25FL116K.SQI_MODE = QUAD
            Dim S25FL132K As SPI_NOR = CType(FindDevice(&H1, &H4016, 0, MemoryType.SERIAL_NOR), SPI_NOR)
            S25FL132K.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL
            S25FL132K.SQI_MODE = QUAD
            Dim S25FL164K As SPI_NOR = CType(FindDevice(&H1, &H4017, 0, MemoryType.SERIAL_NOR), SPI_NOR)
            S25FL164K.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL
            S25FL164K.SQI_MODE = QUAD
            Dim S25FL512S As SPI_NOR = CType(FindDevice(&H1, &H220, 0, MemoryType.SERIAL_NOR), SPI_NOR)
            S25FL512S.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL
            S25FL512S.SQI_MODE = QUAD
            S25FL512S.OP_COMMANDS.QUAD_READ = &H6C '4QOR
            S25FL512S.OP_COMMANDS.QUAD_PROG = &H34 '4QPP
            Dim S25FL256S_256KB As SPI_NOR = CType(FindDevice(&H1, &H219, &H4D00, MemoryType.SERIAL_NOR), SPI_NOR)
            S25FL256S_256KB.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL
            S25FL256S_256KB.SQI_MODE = QUAD
            S25FL256S_256KB.OP_COMMANDS.QUAD_READ = &H6C '4QOR
            S25FL256S_256KB.OP_COMMANDS.QUAD_PROG = &H34 '4QPP
            Dim S25FL256S_64KB As SPI_NOR = CType(FindDevice(&H1, &H219, &H4D01, MemoryType.SERIAL_NOR), SPI_NOR)
            S25FL256S_64KB.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL
            S25FL256S_64KB.SQI_MODE = QUAD
            S25FL256S_64KB.OP_COMMANDS.QUAD_READ = &H6C '4QOR
            S25FL256S_64KB.OP_COMMANDS.QUAD_PROG = &H34 '4QPP

            CType(FindDevice(&H9D, &H4016, 0, MemoryType.SERIAL_NOR), SPI_NOR).VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI 'IS25LQ032
            CType(FindDevice(&H9D, &H4015, 0, MemoryType.SERIAL_NOR), SPI_NOR).VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI 'IS25LQ016
            CType(FindDevice(&H9D, &H6014, 0, MemoryType.SERIAL_NOR), SPI_NOR).VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI 'IS25LP080
            CType(FindDevice(&H9D, &H7014, 0, MemoryType.SERIAL_NOR), SPI_NOR).VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI 'IS25WP080
            CType(FindDevice(&H9D, &H7013, 0, MemoryType.SERIAL_NOR), SPI_NOR).VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI 'IS25WP040
            CType(FindDevice(&H9D, &H7012, 0, MemoryType.SERIAL_NOR), SPI_NOR).VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI 'IS25WP020
            CType(FindDevice(&H9D, &H6019, 0, MemoryType.SERIAL_NOR), SPI_NOR).VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI 'IS25LP256
            CType(FindDevice(&H9D, &H7019, 0, MemoryType.SERIAL_NOR), SPI_NOR).VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI 'IS25WP256
            CType(FindDevice(&H9D, &H6018, 0, MemoryType.SERIAL_NOR), SPI_NOR).VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI 'IS25LP128

            'Winbond
            For Each flash In MemDeviceSelect(MemoryType.SERIAL_NOR)
                Dim spi_flash = DirectCast(flash, SPI_NOR)
                If spi_flash.MFG_CODE = &HEF AndAlso spi_flash.SQI_MODE = SPI_QUAD Then
                    spi_flash.VENDOR_SPECIFIC = VENDOR_FEATURE.Winbond 'W25Q32JW
                End If
            Next

            'Intel
            Dim I28F128J3 As P_NOR = CType(FindDevice(&H89, &H18, 0, MemoryType.PARALLEL_NOR), P_NOR)
            I28F128J3.VENDOR_SPECIFIC = VENDOR_FEATURE.Intel_01
            Dim I28F256J3 As P_NOR = CType(FindDevice(&H89, &H1D, 0, MemoryType.PARALLEL_NOR), P_NOR)
            I28F256J3.VENDOR_SPECIFIC = VENDOR_FEATURE.Intel_01
            Dim I28F640J3 As P_NOR = CType(FindDevice(&H89, &H17, 0, MemoryType.PARALLEL_NOR), P_NOR)
            I28F640J3.VENDOR_SPECIFIC = VENDOR_FEATURE.Intel_01
            Dim I28F320J3 As P_NOR = CType(FindDevice(&H89, &H16, 0, MemoryType.PARALLEL_NOR), P_NOR)
            I28F320J3.VENDOR_SPECIFIC = VENDOR_FEATURE.Intel_01

        End Sub

        Private Sub SPINOR_Database()
            'Adesto (formely Atmel)
            FlashDB.Add(CreateSeries45("Adesto AT45DB641E", Mb064, &H2800, &H100, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB642D", Mb064, &H2800, 0, 1024))
            FlashDB.Add(CreateSeries45("Adesto AT45DB321E", Mb032, &H2701, &H100, 512))
            FlashDB.Add(CreateSeries45("Adesto AT45DB321D", Mb032, &H2701, 0, 512))
            FlashDB.Add(CreateSeries45("Adesto AT45DB161E", Mb016, &H2600, &H100, 512))
            FlashDB.Add(CreateSeries45("Adesto AT45DB161D", Mb016, &H2600, 0, 512))
            FlashDB.Add(CreateSeries45("Adesto AT45DB081E", Mb008, &H2500, &H100, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB081D", Mb008, &H2500, 0, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB041E", Mb004, &H2400, &H100, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB041D", Mb004, &H2400, 0, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB021E", Mb002, &H2300, &H100, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB021D", Mb002, &H2300, 0, 256))
            FlashDB.Add(CreateSeries45("Adesto AT45DB011D", Mb001, &H2200, 0, 256))

            FlashDB.Add(New SPI_NOR("Adesto AT25DF641", SPI_3V, Mb064, &H1F, &H4800)) 'Confirmed (build 350)
            FlashDB.Add(New SPI_NOR("Adesto AT25DF321S", SPI_3V, Mb032, &H1F, &H4701))
            FlashDB.Add(New SPI_NOR("Adesto AT25DF321", SPI_3V, Mb032, &H1F, &H4700))
            FlashDB.Add(New SPI_NOR("Adesto AT25DF161", SPI_3V, Mb016, &H1F, &H4602))
            FlashDB.Add(New SPI_NOR("Adesto AT25DF081", SPI_3V, Mb008, &H1F, &H4502))
            FlashDB.Add(New SPI_NOR("Adesto AT25FF041A ", SPI_3V, Mb004, &H1F, &H4408)) '1.8-3V
            FlashDB.Add(New SPI_NOR("Adesto AT25DF041", SPI_3V, Mb004, &H1F, &H4402))
            FlashDB.Add(New SPI_NOR("Adesto AT25DF021", SPI_3V, Mb002, &H1F, &H4300))
            FlashDB.Add(New SPI_NOR("Adesto AT26DF321", SPI_3V, Mb032, &H1F, &H4700))
            FlashDB.Add(New SPI_NOR("Adesto AT26DF161", SPI_3V, Mb016, &H1F, &H4600))
            FlashDB.Add(New SPI_NOR("Adesto AT26DF161A", SPI_3V, Mb016, &H1F, &H4601))
            FlashDB.Add(New SPI_NOR("Adesto AT26DF081A", SPI_3V, Mb008, &H1F, &H4501))
            FlashDB.Add(New SPI_NOR("Adesto AT25SF321", SPI_3V, Mb032, &H1F, &H8701))
            FlashDB.Add(New SPI_NOR("Adesto AT25SF161", SPI_3V, Mb016, &H1F, &H8601))
            FlashDB.Add(New SPI_NOR("Adesto AT25SF081", SPI_3V, Mb008, &H1F, &H8501))
            FlashDB.Add(New SPI_NOR("Adesto AT25SF041", SPI_3V, Mb004, &H1F, &H8401))
            FlashDB.Add(New SPI_NOR("Adesto AT25XV041", SPI_3V, Mb004, &H1F, &H4401))
            FlashDB.Add(New SPI_NOR("Adesto AT25XV021", SPI_3V, Mb002, &H1F, &H4301))
            FlashDB.Add(New SPI_NOR("Adesto AT25DN011", SPI_3V, Mb001, &H1F, &H4200))
            FlashDB.Add(New SPI_NOR("Adesto AT25DN512C", SPI_3V, Kb512, &H1F, &H6501))
            FlashDB.Add(New SPI_NOR("Adesto AT25DN256", SPI_3V, Kb256, &H1F, &H4000))
            'Adesto (1.8V memories)
            FlashDB.Add(New SPI_NOR("Adesto AT25SL128A", SPI_1V8, Mb128, &H1F, &H4218))
            FlashDB.Add(New SPI_NOR("Adesto AT25SL641", SPI_1V8, Mb064, &H1F, &H4217))
            FlashDB.Add(New SPI_NOR("Adesto AT25SL321", SPI_1V8, Mb032, &H1F, &H4216))
            'Cypress 25FL Series (formely Spansion)
            FlashDB.Add(New SPI_NOR("Cypress S70FL01GS", SPI_3V, Gb001, &H1, &H221, &HDC, &H40000, &H13, &HC, &H12) With {.ID2 = &H4D00, .FAMILY = &H80})
            FlashDB.Add(New SPI_NOR("Cypress S70FS01GS", SPI_1V8, Gb001, &H1, &H221, &HDC, &H40000, &H13, &HC, &H12) With {.ID2 = &H4D00, .FAMILY = &H81})
            FlashDB.Add(New SPI_NOR("Cypress S25FL512S", SPI_3V, Mb512, &H1, &H220, &HDC, &H40000, &H13, &HC, &H12) With {.ID2 = &H4D00, .FAMILY = &H80})
            FlashDB.Add(New SPI_NOR("Cypress S25FL512S", SPI_3V, Mb512, &H1, &H220, &HDC, &H10000, &H13, &HC, &H12) With {.ID2 = &H4D01, .FAMILY = &H80})
            FlashDB.Add(New SPI_NOR("Cypress S25FS512S", SPI_1V8, Mb512, &H1, &H220, &HDC, &H40000, &H13, &HC, &H12) With {.ID2 = &H4D00, .FAMILY = &H81})
            FlashDB.Add(New SPI_NOR("Cypress S25FS512S", SPI_1V8, Mb512, &H1, &H220, &HDC, &H10000, &H13, &HC, &H12) With {.ID2 = &H4D01, .FAMILY = &H81})
            FlashDB.Add(New SPI_NOR("Cypress S25FL256S", SPI_3V, Mb256, &H1, &H219, &HDC, &H40000, &H13, &HC, &H12) With {.ID2 = &H4D00, .FAMILY = &H80})
            FlashDB.Add(New SPI_NOR("Cypress S25FL256S", SPI_3V, Mb256, &H1, &H219, &HDC, &H10000, &H13, &HC, &H12) With {.ID2 = &H4D01, .FAMILY = &H80})
            FlashDB.Add(New SPI_NOR("Cypress S25FS256S", SPI_1V8, Mb256, &H1, &H219, &HDC, &H40000, &H13, &HC, &H12) With {.ID2 = &H4D00, .FAMILY = &H81})
            FlashDB.Add(New SPI_NOR("Cypress S25FS256S", SPI_1V8, Mb256, &H1, &H219, &HDC, &H10000, &H13, &HC, &H12) With {.ID2 = &H4D01, .FAMILY = &H81})
            FlashDB.Add(New SPI_NOR("Cypress FL127S/FL128S", SPI_3V, Mb128, &H1, &H2018) With {.ERASE_SIZE = Kb512, .ID2 = &H4D01, .FAMILY = &H80})
            FlashDB.Add(New SPI_NOR("Cypress S25FS128S", SPI_1V8, Mb128, &H1, &H2018) With {.ID2 = &H4D00, .FAMILY = &H81, .ERASE_SIZE = Mb002})
            FlashDB.Add(New SPI_NOR("Cypress S25FL128S", SPI_3V, Mb128, &H1, &H2018) With {.ID2 = &H4D00, .FAMILY = &H80, .ERASE_SIZE = Mb002})
            FlashDB.Add(New SPI_NOR("Cypress S25FL127S", SPI_3V, Mb128, 0, 0)) 'Placeholder for database files
            FlashDB.Add(New SPI_NOR("Cypress S25FS064S", SPI_1V8, Mb064, &H1, &H217) With {.ID2 = &H4D00, .FAMILY = &H81, .ERASE_SIZE = Mb002})
            FlashDB.Add(New SPI_NOR("Cypress S25FS128S", SPI_1V8, Mb128, &H1, &H2018) With {.ID2 = &H4D01, .FAMILY = &H81})
            FlashDB.Add(New SPI_NOR("Cypress S25FS064S", SPI_1V8, Mb064, &H1, &H217) With {.ID2 = &H4D01, .FAMILY = &H81})
            FlashDB.Add(New SPI_NOR("Cypress S25FL256L", SPI_3V, Mb256, &H1, &H6019, &HDC, &H10000, &H13, &HC, &H12))
            FlashDB.Add(New SPI_NOR("Cypress S25FL128L", SPI_3V, Mb128, &H1, &H6018, &HDC, &H10000, &H13, &HC, &H12) With {.ADDRESSBITS = 32})
            FlashDB.Add(New SPI_NOR("Cypress S25FL064L", SPI_3V, Mb064, &H1, &H6017, &HDC, &H10000, &H13, &HC, &H12) With {.ADDRESSBITS = 32})
            FlashDB.Add(New SPI_NOR("Cypress S70FL256P", SPI_3V, Mb256, 0, 0)) 'Placeholder (uses two S25FL128S, PIN6 is CS2)
            FlashDB.Add(New SPI_NOR("Cypress S25FL128P", SPI_3V, Mb128, &H1, &H2018) With {.ERASE_SIZE = Kb512, .ID2 = &H301}) '0301h X
            FlashDB.Add(New SPI_NOR("Cypress S25FL128P", SPI_3V, Mb128, &H1, &H2018) With {.ERASE_SIZE = Mb002, .ID2 = &H300}) '0300h X
            FlashDB.Add(New SPI_NOR("Cypress S25FL129P", SPI_3V, Mb128, &H1, &H2018) With {.ERASE_SIZE = Kb512, .ID2 = &H4D01}) '4D01h X
            FlashDB.Add(New SPI_NOR("Cypress S25FL129P", SPI_3V, Mb128, &H1, &H2018) With {.ERASE_SIZE = Mb002, .ID2 = &H4D00}) '4D00h X
            FlashDB.Add(New SPI_NOR("Cypress S25FL064", SPI_3V, Mb064, &H1, &H216))
            FlashDB.Add(New SPI_NOR("Cypress S25FL032", SPI_3V, Mb032, &H1, &H215))
            FlashDB.Add(New SPI_NOR("Cypress S25FL016A", SPI_3V, Mb016, &H1, &H214))
            FlashDB.Add(New SPI_NOR("Cypress S25FL008A", SPI_3V, Mb008, &H1, &H213))
            FlashDB.Add(New SPI_NOR("Cypress S25FL004A", SPI_3V, Mb004, &H1, &H212))
            FlashDB.Add(New SPI_NOR("Cypress S25FL040A", SPI_3V, Mb004, &H1, &H212))
            FlashDB.Add(New SPI_NOR("Cypress S25FL164K", SPI_3V, Mb064, &H1, &H4017))
            FlashDB.Add(New SPI_NOR("Cypress S25FL132K", SPI_3V, Mb032, &H1, &H4016))
            FlashDB.Add(New SPI_NOR("Cypress S25FL216K", SPI_3V, Mb016, &H1, &H4015)) 'Uses the same ID as S25FL116K (might support 3 byte ID)
            FlashDB.Add(New SPI_NOR("Cypress S25FL116K", SPI_3V, Mb016, &H1, &H4015))
            FlashDB.Add(New SPI_NOR("Cypress S25FL208K", SPI_3V, Mb008, &H1, &H4014))
            FlashDB.Add(New SPI_NOR("Cypress S25FL204K", SPI_3V, Mb004, &H1, &H4013))
            'Semper Flash (SPI compatible)
            FlashDB.Add(New SPI_NOR("Cypress S25HS256T", SPI_1V8, Mb256, &H34, &H2B19, &HDC, Mb002) With {.SEND_EWSR = True, .SEND_EN4B = True, .PAGE_SIZE = 512})
            FlashDB.Add(New SPI_NOR("Cypress S25HS512T", SPI_1V8, Mb512, &H34, &H2B1A, &HDC, Mb002) With {.SEND_EWSR = True, .SEND_EN4B = True, .PAGE_SIZE = 512})
            FlashDB.Add(New SPI_NOR("Cypress S25HS01GT", SPI_1V8, Gb001, &H34, &H2B1B, &HDC, Mb002) With {.SEND_EWSR = True, .SEND_EN4B = True, .PAGE_SIZE = 512})
            FlashDB.Add(New SPI_NOR("Cypress S25HL256T", SPI_3V, Mb256, &H34, &H2A19, &HDC, Mb002) With {.SEND_EWSR = True, .SEND_EN4B = True, .PAGE_SIZE = 512})
            FlashDB.Add(New SPI_NOR("Cypress S25HL512T", SPI_3V, Mb512, &H34, &H2A1A, &HDC, Mb002) With {.SEND_EWSR = True, .SEND_EN4B = True, .PAGE_SIZE = 512})
            FlashDB.Add(New SPI_NOR("Cypress S25HL01GT", SPI_3V, Gb001, &H34, &H2A1B, &HDC, Mb002) With {.SEND_EWSR = True, .SEND_EN4B = True, .PAGE_SIZE = 512})
            'Semper Flash (SPI/HF compatible)
            FlashDB.Add(New SPI_NOR("Cypress S26HS256T", SPI_1V8, Mb256, &H34, &H6B, &HDC, Mb002, &H3, &H13, &H12) With {.ID2 = &H19, .PAGE_SIZE = 512})
            FlashDB.Add(New SPI_NOR("Cypress S26HS512T", SPI_1V8, Mb512, &H34, &H6B, &HDC, Mb002, &H3, &H13, &H12) With {.ID2 = &H1A, .PAGE_SIZE = 512})
            FlashDB.Add(New SPI_NOR("Cypress S26HS01GT", SPI_1V8, Gb001, &H34, &H6B, &HDC, Mb002, &H3, &H13, &H12) With {.ID2 = &H1B, .PAGE_SIZE = 512})
            FlashDB.Add(New SPI_NOR("Cypress S26HL256T", SPI_3V, Mb256, &H34, &H6A, &HDC, Mb002, &H3, &H13, &H12) With {.ID2 = &H19, .PAGE_SIZE = 512})
            FlashDB.Add(New SPI_NOR("Cypress S26HL512T", SPI_3V, Mb512, &H34, &H6A, &HDC, Mb002, &H3, &H13, &H12) With {.ID2 = &H1A, .PAGE_SIZE = 512})
            FlashDB.Add(New SPI_NOR("Cypress S26HL01GT", SPI_3V, Gb001, &H34, &H6A, &HDC, Mb002, &H3, &H13, &H12) With {.ID2 = &H1B, .PAGE_SIZE = 512})
            'Micron (ST)
            FlashDB.Add(New SPI_NOR("Micron MT25QL02GC", SPI_3V, Gb002, &H20, &HBA22) With {.SEND_EN4B = True, .SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Micron N25Q00AA", SPI_3V, Gb001, &H20, &HBA21) With {.SEND_EN4B = True, .SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Micron N25Q512A", SPI_3V, Mb512, &H20, &HBA20) With {.SEND_EN4B = True, .SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Micron N25Q256A", SPI_3V, Mb256, &H20, &HBA19) With {.SEND_EN4B = True, .SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Micron N25Q128A", SPI_3V, Mb128, &H20, &HDA18) With {.ERASE_SIZE = &H20000, .PAGE_SIZE = 64, .SQI_MODE = QUAD}) 'NEW! PageSize is 64 bytes
            FlashDB.Add(New SPI_NOR("Micron N25Q128", SPI_3V, Mb128, &H20, &HBA18) With {.SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Micron N25Q064", SPI_3V, Mb064, &H20, &HBA17) With {.SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Micron N25Q032", SPI_3V, Mb032, &H20, &HBA16) With {.SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Micron N25Q016", SPI_3V, Mb016, &H20, &HBA15) With {.SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Micron N25Q008", SPI_3V, Mb008, &H20, &HBA14) With {.SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Micron N25Q00AA", SPI_1V8, Gb001, &H20, &HBB21) With {.SEND_EN4B = True, .SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Micron N25Q512A", SPI_1V8, Mb512, &H20, &HBB20) With {.SEND_EN4B = True, .SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Micron N25Q256A", SPI_1V8, Mb256, &H20, &HBB19) With {.SEND_EN4B = True, .SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Micron N25Q128A", SPI_1V8, Mb128, &H20, &HBB18) With {.SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Micron N25Q064A", SPI_1V8, Mb064, &H20, &HBB17) With {.SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Micron N25Q032", SPI_1V8, Mb016, &H20, &HBB15) With {.SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Micron N25Q016", SPI_1V8, Mb016, &H20, &HBB15) With {.SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Micron N25Q008", SPI_1V8, Mb008, &H20, &HBB14) With {.SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Micron M25P128", SPI_3V, Mb128, &H20, &H2018) With {.ERASE_SIZE = Mb002})
            FlashDB.Add(New SPI_NOR("Micron M25P64", SPI_3V, Mb064, &H20, &H2017))
            FlashDB.Add(New SPI_NOR("Micron M25PX32", SPI_3V, Mb032, &H20, &H7116))
            FlashDB.Add(New SPI_NOR("Micron M25P32", SPI_3V, Mb032, &H20, &H2016))
            FlashDB.Add(New SPI_NOR("Micron M25PX16", SPI_3V, Mb016, &H20, &H7115))
            FlashDB.Add(New SPI_NOR("Micron M25PX16", SPI_3V, Mb016, &H20, &H7315))
            FlashDB.Add(New SPI_NOR("Micron M25P16", SPI_3V, Mb016, &H20, &H2015))
            FlashDB.Add(New SPI_NOR("Micron M25P80", SPI_3V, Mb008, &H20, &H2014))
            FlashDB.Add(New SPI_NOR("Micron M25PX80", SPI_3V, Mb008, &H20, &H7114))
            FlashDB.Add(New SPI_NOR("Micron M25P40", SPI_3V, Mb004, &H20, &H2013))
            FlashDB.Add(New SPI_NOR("Micron M25P20", SPI_3V, Mb002, &H20, &H2012))
            FlashDB.Add(New SPI_NOR("Micron M25P10", SPI_3V, Mb001, &H20, &H2011))
            FlashDB.Add(New SPI_NOR("Micron M25P05", SPI_3V, Kb512, &H20, &H2010))
            FlashDB.Add(New SPI_NOR("Micron M25PX64", SPI_3V, Mb064, &H20, &H7117))
            FlashDB.Add(New SPI_NOR("Micron M25PX32", SPI_3V, Mb032, &H20, &H7116))
            FlashDB.Add(New SPI_NOR("Micron M25PX16", SPI_3V, Mb016, &H20, &H7115))
            FlashDB.Add(New SPI_NOR("Micron M25PE16", SPI_3V, Mb016, &H20, &H8015))
            FlashDB.Add(New SPI_NOR("Micron M25PE80", SPI_3V, Mb008, &H20, &H8014))
            FlashDB.Add(New SPI_NOR("Micron M25PE40", SPI_3V, Mb004, &H20, &H8013))
            FlashDB.Add(New SPI_NOR("Micron M25PE20", SPI_3V, Mb002, &H20, &H8012))
            FlashDB.Add(New SPI_NOR("Micron M25PE10", SPI_3V, Mb001, &H20, &H8011))
            FlashDB.Add(New SPI_NOR("Micron M45PE16", SPI_3V, Mb016, &H20, &H4015))
            FlashDB.Add(New SPI_NOR("Micron M45PE80", SPI_3V, Mb008, &H20, &H4014))
            FlashDB.Add(New SPI_NOR("Micron M45PE40", SPI_3V, Mb004, &H20, &H4013))
            FlashDB.Add(New SPI_NOR("Micron M45PE20", SPI_3V, Mb002, &H20, &H4012))
            FlashDB.Add(New SPI_NOR("Micron M45PE10", SPI_3V, Mb001, &H20, &H4011))
            'Windbond
            FlashDB.Add(New SPI_NOR("Winbond W25Q256JW", SPI_1V8, Mb256, &HEF, &H8019) With {.SEND_EN4B = True, .SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q128JW", SPI_1V8, Mb128, &HEF, &H8018) With {.SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q64JW", SPI_1V8, Mb064, &HEF, &H8017) With {.SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q32JW", SPI_1V8, Mb032, &HEF, &H8016) With {.SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25M512JV", SPI_3V, Mb512, &HEF, &H7119) With {.SEND_EN4B = True, .STACKED_DIES = 2})
            FlashDB.Add(New SPI_NOR("Winbond W25M512JW", SPI_1V8, Mb512, &HEF, &H6119) With {.SEND_EN4B = True, .STACKED_DIES = 2})
            FlashDB.Add(New SPI_NOR("Winbond W25H02NW", SPI_1V8, Gb002, &HEF, &HA022) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25H01NW", SPI_1V8, Gb001, &HEF, &HA021) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25H512NW", SPI_1V8, Mb512, &HEF, &HA020) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25H02JV", SPI_3V, Gb002, &HEF, &H9022) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25H01JV", SPI_3V, Gb001, &HEF, &H9021) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25H512JV", SPI_3V, Mb512, &HEF, &H9020) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q02NW", SPI_1V8, Gb002, &HEF, &H8022) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q01NW", SPI_1V8, Gb001, &HEF, &H8021) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q512NW", SPI_1V8, Mb512, &HEF, &H8020) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q01NW", SPI_1V8, Gb001, &HEF, &H6021) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q512NW", SPI_1V8, Mb512, &HEF, &H6020) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q256FV", SPI_3V, Mb256, &HEF, &H6019) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q02JV", SPI_3V, Gb002, &HEF, &H7022) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q01JV", SPI_3V, Gb001, &HEF, &H7021) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q512JV", SPI_3V, Mb512, &HEF, &H7020) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q256JV", SPI_3V, Mb256, &HEF, &H7019) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q128JV", SPI_3V, Mb128, &HEF, &H7018) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q64JV", SPI_3V, Mb064, &HEF, &H7017) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q32JV", SPI_3V, Mb032, &HEF, &H7016) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q01", SPI_3V, Mb256, &HEF, &H4021) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q512", SPI_3V, Mb512, &HEF, &H4020) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q256", SPI_3V, Mb256, &HEF, &H4019) With {.SEND_EN4B = True, .SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q128", SPI_3V, Mb128, &HEF, &H4018) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q64", SPI_3V, Mb064, &HEF, &H4017) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q32", SPI_3V, Mb032, &HEF, &H4016) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q16", SPI_3V, Mb016, &HEF, &H4015) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q16JV-DTR", SPI_3V, Mb016, &HEF, &H7015) With {.SQI_MODE = SPI_QUAD}) 'CV (3x2mm)
            FlashDB.Add(New SPI_NOR("Winbond W25Q16JW", SPI_1V8, Mb016, &HEF, &H6015) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q16JW", SPI_1V8, Mb016, &HEF, &H8015) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q80", SPI_3V, Mb008, &HEF, &H4014) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q40", SPI_3V, Mb004, &HEF, &H4013) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25X64", SPI_3V, Mb064, &HEF, &H3017))
            FlashDB.Add(New SPI_NOR("Winbond W25X32", SPI_3V, Mb032, &HEF, &H3016))
            FlashDB.Add(New SPI_NOR("Winbond W25X16", SPI_3V, Mb016, &HEF, &H3015))
            FlashDB.Add(New SPI_NOR("Winbond W25X80", SPI_3V, Mb008, &HEF, &H3014))
            FlashDB.Add(New SPI_NOR("Winbond W25X40", SPI_3V, Mb004, &HEF, &H3013))
            FlashDB.Add(New SPI_NOR("Winbond W25X20", SPI_3V, Mb002, &HEF, &H3012))
            FlashDB.Add(New SPI_NOR("Winbond W25X10", SPI_3V, Mb002, &HEF, &H3011))
            FlashDB.Add(New SPI_NOR("Winbond W25X05", SPI_3V, Kb512, &HEF, &H3010))
            FlashDB.Add(New SPI_NOR("Winbond W25M121AV", SPI_3V, 0UI, CByte(0), CUShort(0))) 'Contains a NOR die and NAND die
            FlashDB.Add(New SPI_NOR("Winbond W25Q256FW", SPI_1V8, Mb256, &HEF, &H6019) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q128FW", SPI_1V8, Mb128, &HEF, &H6018) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q64FW", SPI_1V8, Mb064, &HEF, &H6017) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q32FW", SPI_1V8, Mb032, &HEF, &H6016) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q16FW", SPI_1V8, Mb016, &HEF, &H6015) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q80EW", SPI_1V8, Mb008, &HEF, &H6014) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q40EW", SPI_1V8, Mb004, &HEF, &H6013) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q20EW", SPI_1V8, Mb002, &HEF, &H6012) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q80BW", SPI_1V8, Mb008, &HEF, &H5014) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q40BW", SPI_1V8, Mb004, &HEF, &H5013) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Winbond W25Q20BW", SPI_1V8, Mb002, &HEF, &H5012) With {.SQI_MODE = SPI_QUAD})
            'MXIC
            FlashDB.Add(New SPI_NOR("MXIC MX66L1G45G", SPI_3V, Gb001, &HC2, &H201B) With {.SEND_EN4B = True})
            FlashDB.Add(New SPI_NOR("MXIC MX25LM51245G", SPI_3V, Mb512, &HC2, &H853A) With {.SEND_EN4B = True})
            FlashDB.Add(New SPI_NOR("MXIC MX25L51245G", SPI_3V, Mb512, &HC2, &H201A) With {.SEND_EN4B = True})
            FlashDB.Add(New SPI_NOR("MXIC MX25L25655E", SPI_3V, Mb256, &HC2, &H2619) With {.SEND_EN4B = True})
            FlashDB.Add(New SPI_NOR("MXIC MX25L256", SPI_3V, Mb256, &HC2, &H2019) With {.SEND_EN4B = True})
            FlashDB.Add(New SPI_NOR("MXIC MX25L12855E", SPI_3V, Mb128, &HC2, &H2618))
            FlashDB.Add(New SPI_NOR("MXIC MX25L128", SPI_3V, Mb128, &HC2, &H2018))
            FlashDB.Add(New SPI_NOR("MXIC MX25L6455E", SPI_3V, Mb064, &HC2, &H2617))
            FlashDB.Add(New SPI_NOR("MXIC MX25L640", SPI_3V, Mb064, &HC2, &H2017))
            FlashDB.Add(New SPI_NOR("MXIC MX25L320", SPI_3V, Mb032, &HC2, &H2016)) '
            FlashDB.Add(New SPI_NOR("MXIC MX25L3205D", SPI_3V, Mb032, &HC2, &H20FF))
            FlashDB.Add(New SPI_NOR("MXIC MX25L323", SPI_3V, Mb032, &HC2, &H5E16))
            FlashDB.Add(New SPI_NOR("MXIC MX25L3255E", SPI_3V, Mb032, &HC2, &H9E16))
            FlashDB.Add(New SPI_NOR("MXIC MX25L1633E", SPI_3V, Mb016, &HC2, &H2415))
            FlashDB.Add(New SPI_NOR("MXIC MX25L160", SPI_3V, Mb016, &HC2, &H2015))
            FlashDB.Add(New SPI_NOR("MXIC MX25L80", SPI_3V, Mb008, &HC2, &H2014))
            FlashDB.Add(New SPI_NOR("MXIC MX25L40", SPI_3V, Mb004, &HC2, &H2013))
            FlashDB.Add(New SPI_NOR("MXIC MX25L20", SPI_3V, Mb002, &HC2, &H2012))
            FlashDB.Add(New SPI_NOR("MXIC MX25L10", SPI_3V, Mb001, &HC2, &H2011))
            FlashDB.Add(New SPI_NOR("MXIC MX25L512", SPI_3V, Kb512, &HC2, &H2010))
            FlashDB.Add(New SPI_NOR("MXIC MX25L1021E", SPI_3V, Mb001, &HC2, &H2211))
            FlashDB.Add(New SPI_NOR("MXIC MX25L5121E", SPI_3V, Kb512, &HC2, &H2210))
            FlashDB.Add(New SPI_NOR("MXIC MX66L51235F", SPI_3V, Mb512, &HC2, &H201A) With {.SEND_EN4B = True})
            FlashDB.Add(New SPI_NOR("MXIC MX25V8035", SPI_2V5, Mb008, &HC2, &H2554))
            FlashDB.Add(New SPI_NOR("MXIC MX25V4035", SPI_2V5, Mb004, &HC2, &H2553))
            FlashDB.Add(New SPI_NOR("MXIC MX25V1635F", SPI_3V, Mb016, &HC2, &H2315)) 'Range 2.3V-3.6V
            FlashDB.Add(New SPI_NOR("MXIC MX25V8035F", SPI_3V, Mb008, &HC2, &H2314)) 'Range 2.3V-3.6V
            FlashDB.Add(New SPI_NOR("MXIC MX25R6435", SPI_3V, Mb064, &HC2, &H2817)) 'Wide range: 1.65 to 3.5V
            FlashDB.Add(New SPI_NOR("MXIC MX25R3235F", SPI_3V, Mb032, &HC2, &H2816)) 'Wide range: 1.65 to 3.5V
            FlashDB.Add(New SPI_NOR("MXIC MX25R1635F", SPI_3V, Mb016, &HC2, &H2815)) 'Wide range: 1.65 to 3.5V
            FlashDB.Add(New SPI_NOR("MXIC MX25R8035F", SPI_3V, Mb008, &HC2, &H2814)) 'Wide range: 1.65 to 3.5V
            FlashDB.Add(New SPI_NOR("MXIC MX25L3235E", SPI_3V, Mb032, 0, 0)) 'Place holder
            FlashDB.Add(New SPI_NOR("MXIC MX25L2005", SPI_3V, Mb032, 0, 0)) 'Place holder
            FlashDB.Add(New SPI_NOR("MXIC MX25L2006E", SPI_3V, Mb032, 0, 0)) 'Place holder
            FlashDB.Add(New SPI_NOR("MXIC MX25L2026E", SPI_3V, Mb032, 0, 0)) 'Place holder
            FlashDB.Add(New SPI_NOR("MXIC MX25L51245G", SPI_3V, Mb032, 0, 0)) 'Place holder
            'MXIC (1.8V)
            FlashDB.Add(New SPI_NOR("MXIC MX25UM51345G", SPI_1V8, Mb512, &HC2, &H813A) With {.SEND_EN4B = True})
            FlashDB.Add(New SPI_NOR("MXIC MX25U25645G", SPI_1V8, Mb256, &HC2, &H2539) With {.SEND_EN4B = True})
            FlashDB.Add(New SPI_NOR("MXIC MX25U12873F", SPI_1V8, Mb128, &HC2, &H2538))
            FlashDB.Add(New SPI_NOR("MXIC MX25U643", SPI_1V8, Mb064, &HC2, &H2537))
            FlashDB.Add(New SPI_NOR("MXIC MX25U323", SPI_1V8, Mb032, &HC2, &H2536))
            FlashDB.Add(New SPI_NOR("MXIC MX25U3235F", SPI_1V8, Mb032, &HC2, &H2536))
            FlashDB.Add(New SPI_NOR("MXIC MX25U1635E", SPI_1V8, Mb016, &HC2, &H2535))
            FlashDB.Add(New SPI_NOR("MXIC MX25U803", SPI_1V8, Mb008, &HC2, &H2534))
            'EON
            FlashDB.Add(New SPI_NOR("EON EN25Q128", SPI_3V, Mb128, &H1C, &H3018) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("EON EN25Q64", SPI_3V, Mb064, &H1C, &H3017) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("EON EN25Q32", SPI_3V, Mb032, &H1C, &H3016) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("EON EN25Q16", SPI_3V, Mb016, &H1C, &H3015) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("EON EN25Q80", SPI_3V, Mb008, &H1C, &H3014) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("EON EN25Q40", SPI_3V, Mb004, &H1C, &H3013) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("EON EN25QH128", SPI_3V, Mb128, &H1C, &H7018) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("EON EN25QH64", SPI_3V, Mb064, &H1C, &H7017) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("EON EN25QH32", SPI_3V, Mb032, &H1C, &H7016) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("EON EN25QH16", SPI_3V, Mb016, &H1C, &H7015) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("EON EN25QH80", SPI_3V, Mb008, &H1C, &H7014) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("EON EN25P64", SPI_3V, Mb064, &H1C, &H2017))
            FlashDB.Add(New SPI_NOR("EON EN25P32", SPI_3V, Mb032, &H1C, &H2016))
            FlashDB.Add(New SPI_NOR("EON EN25P16", SPI_3V, Mb016, &H1C, &H2015))
            FlashDB.Add(New SPI_NOR("EON EN25F32", SPI_3V, Mb032, &H1C, &H3116))
            FlashDB.Add(New SPI_NOR("EON EN25F16", SPI_3V, Mb016, &H1C, &H3115))
            FlashDB.Add(New SPI_NOR("EON EN25F80", SPI_3V, Mb008, &H1C, &H3114))
            FlashDB.Add(New SPI_NOR("EON EN25F40", SPI_3V, Mb004, &H1C, &H3113))
            FlashDB.Add(New SPI_NOR("EON EN25F20", SPI_3V, Mb002, &H1C, &H3112))
            FlashDB.Add(New SPI_NOR("EON EN25T32", SPI_3V, Mb032, &H1C, &H5116))
            FlashDB.Add(New SPI_NOR("EON EN25T16", SPI_3V, Mb016, &H1C, &H5115))
            FlashDB.Add(New SPI_NOR("EON EN25T80", SPI_3V, Mb008, &H1C, &H5114))
            FlashDB.Add(New SPI_NOR("EON EN25T40", SPI_3V, Mb004, &H1C, &H5113))
            FlashDB.Add(New SPI_NOR("EON EN25T20", SPI_3V, Mb002, &H1C, &H5112))
            FlashDB.Add(New SPI_NOR("EON EN25F10", SPI_3V, Mb001, &H1C, &H3111))
            FlashDB.Add(New SPI_NOR("EON EN25S64", SPI_1V8, Mb064, &H1C, &H3817))
            FlashDB.Add(New SPI_NOR("EON EN25S32", SPI_1V8, Mb032, &H1C, &H3816))
            FlashDB.Add(New SPI_NOR("EON EN25S16", SPI_1V8, Mb016, &H1C, &H3815))
            FlashDB.Add(New SPI_NOR("EON EN25S80", SPI_1V8, Mb008, &H1C, &H3814))
            FlashDB.Add(New SPI_NOR("EON EN25S40", SPI_1V8, Mb004, &H1C, &H3813))
            FlashDB.Add(New SPI_NOR("EON EN25S20", SPI_1V8, Mb002, &H1C, &H3812))
            FlashDB.Add(New SPI_NOR("EON EN25S10", SPI_1V8, Mb001, &H1C, &H3811))
            'Microchip / Silicon Storage Technology (SST) / PCT Group (Rebranded)
            FlashDB.Add(New SPI_NOR("Microchip SST26VF064", SPI_3V, Mb064, &HBF, &H2603))
            FlashDB.Add(New SPI_NOR("Microchip SST26VF064B", SPI_3V, Mb064, &HBF, &H2643)) 'SST26VF064BA
            FlashDB.Add(New SPI_NOR("Microchip SST26VF032", SPI_3V, Mb032, &HBF, &H2602)) 'PCT26VF032
            FlashDB.Add(New SPI_NOR("Microchip SST26VF032", SPI_3V, Mb032, &HBF, &H2602, &H52, &H8000) With {.SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Microchip SST26VF032B", SPI_3V, Mb032, &HBF, &H2642, &H52, &H8000)) 'SST26VF032BA
            FlashDB.Add(New SPI_NOR("Microchip SST26VF016", SPI_3V, Mb016, &HBF, &H2601, &H52, &H8000) With {.SQI_MODE = QUAD})
            FlashDB.Add(New SPI_NOR("Microchip SST26VF016", SPI_3V, Mb016, &HBF, &H16BF, &H52, &H8000) With {.ProgramMode = AAI_Byte})
            FlashDB.Add(New SPI_NOR("Microchip SST26VF016B", SPI_3V, Mb016, &HBF, &H2641, &H52, &H8000)) 'SST26VF016BA
            FlashDB.Add(New SPI_NOR("Microchip SST26VF080A", SPI_3V, Mb008, &HBF, &H2618, &H52, &H8000))
            FlashDB.Add(New SPI_NOR("Microchip SST26VF040A", SPI_3V, Mb004, &HBF, &H2614, &H52, &H8000))
            FlashDB.Add(New SPI_NOR("Microchip SST25VF128B", SPI_3V, Mb128, &HBF, &H2544) With {.SEND_EWSR = True}) 'Might use AAI
            FlashDB.Add(New SPI_NOR("Microchip SST25VF064C", SPI_3V, Mb064, &HBF, &H254B) With {.SEND_EWSR = True}) 'PCT25VF064C
            FlashDB.Add(New SPI_NOR("Microchip SST25VF032", SPI_3V, Mb032, &HBF, &H2542) With {.ProgramMode = AAI_Word, .SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR("Microchip SST25VF032B", SPI_3V, Mb032, &HBF, &H254A) With {.ProgramMode = AAI_Word, .SEND_EWSR = True}) 'PCT25VF032B
            FlashDB.Add(New SPI_NOR("Microchip SST25VF016B", SPI_3V, Mb016, &HBF, &H2541) With {.ProgramMode = AAI_Word, .SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR("Microchip SST25VF080", SPI_3V, Mb008, &HBF, &H80BF, &H20, &H1000) With {.ProgramMode = AAI_Byte, .SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR("Microchip SST25VF080B", SPI_3V, Mb008, &HBF, &H258E, &H20, &H1000) With {.ProgramMode = AAI_Word, .SEND_EWSR = True}) 'PCT25VF080B - Confirmed (Build 350)
            FlashDB.Add(New SPI_NOR("Microchip SST25VF040B", SPI_3V, Mb004, &HBF, &H258D, &H20, &H1000) With {.ProgramMode = AAI_Word, .SEND_EWSR = True}) 'PCT25VF040B <--testing
            FlashDB.Add(New SPI_NOR("Microchip SST25VF020", SPI_3V, Mb002, &HBF, &H258C, &H20, &H1000) With {.ProgramMode = AAI_Word, .SEND_EWSR = True}) 'SST25VF020B SST25PF020B PCT25VF020B
            FlashDB.Add(New SPI_NOR("Microchip SST25VF020A", SPI_3V, Mb002, &HBF, &H43, &H20, &H1000) With {.ProgramMode = AAI_Byte, .SEND_EWSR = True}) 'Confirmed (Build 350)
            FlashDB.Add(New SPI_NOR("Microchip SST25VF010", SPI_3V, Mb001, &HBF, &H49BF, &H20, &H1000) With {.ProgramMode = AAI_Byte, .SEND_EWSR = True}) 'SST25VF010A PCT25VF010A
            FlashDB.Add(New SPI_NOR("Microchip SST25VF010A", SPI_3V, Mb001, &HBF, &H49, &H20, &H1000) With {.ProgramMode = AAI_Byte, .SEND_EWSR = True}) 'Confirmed (Build 350)
            FlashDB.Add(New SPI_NOR("Microchip SST25VF512", SPI_3V, Kb512, &HBF, &H48, &H20, &H1000) With {.ProgramMode = AAI_Byte, .SEND_EWSR = True}) 'SST25VF512A PCT25VF512A REMS ONLY
            FlashDB.Add(New SPI_NOR("Microchip SST25PF040C", SPI_3V, Mb004, &H62, &H613))
            FlashDB.Add(New SPI_NOR("Microchip SST25LF020A", SPI_3V, Mb002, &HBF, &H43BF, &H20, &H1000) With {.ProgramMode = AAI_Byte, .SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR("Microchip SST26WF064", SPI_1V8, Mb064, &HBF, &H2643))
            FlashDB.Add(New SPI_NOR("Microchip SST26WF032", SPI_1V8, Mb032, &HBF, &H2622)) 'PCT26WF032
            FlashDB.Add(New SPI_NOR("Microchip SST26WF016", SPI_1V8, Mb016, &HBF, &H2651)) 'SST26WF016
            FlashDB.Add(New SPI_NOR("Microchip SST26WF080", SPI_1V8, Mb008, &HBF, &H2658, &H20, &H1000))
            FlashDB.Add(New SPI_NOR("Microchip SST26WF040", SPI_1V8, Mb004, &HBF, &H2654, &H20, &H1000))
            FlashDB.Add(New SPI_NOR("Microchip SST25WF080B", SPI_1V8, Mb008, &H62, &H1614, &H20, &H1000) With {.SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR("Microchip SST25WF040", SPI_1V8, Mb004, &HBF, &H2504, &H20, &H1000) With {.ProgramMode = AAI_Word, .SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR("Microchip SST25WF020A", SPI_1V8, Mb002, &H62, &H1612, &H20, &H1000) With {.SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR("Microchip SST25WF040B", SPI_1V8, Mb004, &H62, &H1613, &H20, &H1000) With {.SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR("Microchip SST25WF020", SPI_1V8, Mb002, &HBF, &H2503, &H20, &H1000) With {.ProgramMode = AAI_Word, .SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR("Microchip SST25WF010", SPI_1V8, Mb001, &HBF, &H2502, &H20, &H1000) With {.ProgramMode = AAI_Word, .SEND_EWSR = True})
            FlashDB.Add(New SPI_NOR("Microchip SST25WF512", SPI_1V8, Kb512, &HBF, &H2501, &H20, &H1000) With {.ProgramMode = AAI_Word, .SEND_EWSR = True})
            'PMC
            FlashDB.Add(New SPI_NOR("PMC PM25LV016B", SPI_3V, Mb016, &H7F, &H9D14))
            FlashDB.Add(New SPI_NOR("PMC PM25LV080B", SPI_3V, Mb008, &H7F, &H9D13))
            FlashDB.Add(New SPI_NOR("PMC PM25LV040", SPI_3V, Mb004, &H9D, &H7E7F))
            FlashDB.Add(New SPI_NOR("PMC PM25LV020", SPI_3V, Mb002, &H9D, &H7D7F))
            FlashDB.Add(New SPI_NOR("PMC PM25LV010", SPI_3V, Mb001, &H9D, &H7C7F))
            FlashDB.Add(New SPI_NOR("PMC PM25LV512", SPI_3V, Kb512, &H9D, &H7B7F))
            FlashDB.Add(New SPI_NOR("PMC PM25LD020", SPI_3V, Mb002, &H7F, &H9D22))
            FlashDB.Add(New SPI_NOR("PMC PM25LD010", SPI_3V, Mb001, &H7F, &H9D21))
            FlashDB.Add(New SPI_NOR("PMC PM25LD512", SPI_3V, Kb512, &H7F, &H9D20))
            'AMIC
            FlashDB.Add(New SPI_NOR("AMIC A25LQ64", SPI_3V, Mb064, &H37, &H4017) With {.SQI_MODE = SPI_QUAD}) 'A25LMQ64
            FlashDB.Add(New SPI_NOR("AMIC A25LQ32A", SPI_3V, Mb032, &H37, &H4016) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("AMIC A25L032", SPI_3V, Mb032, &H37, &H3016))
            FlashDB.Add(New SPI_NOR("AMIC A25L016", SPI_3V, Mb016, &H37, &H3015))
            FlashDB.Add(New SPI_NOR("AMIC A25LQ16", SPI_3V, Mb016, &H37, &H4015) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("AMIC A25L080", SPI_3V, Mb008, &H37, &H3014))
            FlashDB.Add(New SPI_NOR("AMIC A25L040", SPI_3V, Mb004, &H37, &H3013)) 'A25L040A A25P040
            FlashDB.Add(New SPI_NOR("AMIC A25L020", SPI_3V, Mb002, &H37, &H3012)) 'A25L020C A25P020
            FlashDB.Add(New SPI_NOR("AMIC A25L010", SPI_3V, Mb001, &H37, &H3011)) 'A25L010A A25P010
            FlashDB.Add(New SPI_NOR("AMIC A25L512", SPI_3V, Kb512, &H37, &H3010)) 'A25L512A A25P512
            FlashDB.Add(New SPI_NOR("AMIC A25LS512A", SPI_3V, Kb512, &HC2, &H2010))
            'Dosilicon (Formaly Fidelix)
            FlashDB.Add(New SPI_NOR("Dosilicon FM25Q128", SPI_3V, Mb032, &HA1, &H4018) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Dosilicon FM25Q64A", SPI_3V, Mb032, &HF8, &H3217) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Dosilicon FM25Q32A", SPI_3V, Mb032, &HF8, &H3216) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Dosilicon FM25Q16A", SPI_3V, Mb016, &HF8, &H3215) With {.SQI_MODE = SPI_QUAD}) 'FM25Q16B
            FlashDB.Add(New SPI_NOR("Dosilicon FM25Q08", SPI_3V, Mb008, &HF8, &H3214) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Dosilicon FM25Q08", SPI_3V, Mb008, &HA1, &H4014) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Dosilicon FM25Q04", SPI_3V, Mb004, &HA1, &H4013) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Dosilicon FM25Q02", SPI_3V, Mb002, &HA1, &H4012) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("Dosilicon FM25M04A", SPI_3V, Mb004, &HF8, &H4213))
            FlashDB.Add(New SPI_NOR("Dosilicon FM25M08A", SPI_3V, Mb008, &HF8, &H4214))
            FlashDB.Add(New SPI_NOR("Dosilicon FM25M16A", SPI_3V, Mb016, &HF8, &H4215))
            FlashDB.Add(New SPI_NOR("Dosilicon FM25M32A", SPI_3V, Mb032, &HF8, &H4216))
            FlashDB.Add(New SPI_NOR("Dosilicon FM25M64A", SPI_3V, Mb064, &HF8, &H4217))
            FlashDB.Add(New SPI_NOR("Dosilicon FM25M4AA", SPI_3V, Mb004, &HF8, &H4212))
            FlashDB.Add(New SPI_NOR("Dosilicon DS25M4BA", SPI_1V8, Mb004, &HE5, &H4212))
            'Gigadevice
            FlashDB.Add(New SPI_NOR("GigaDevice GD25Q256", SPI_3V, Mb256, &HC8, &H4019) With {.SQI_MODE = SPI_QUAD, .SEND_EN4B = True})
            FlashDB.Add(New SPI_NOR("GigaDevice GD25Q128", SPI_3V, Mb128, &HC8, &H4018) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("GigaDevice GD25Q64", SPI_3V, Mb064, &HC8, &H4017) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("GigaDevice GD25Q32", SPI_3V, Mb032, &HC8, &H4016) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("GigaDevice GD25Q16", SPI_3V, Mb016, &HC8, &H4015) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("GigaDevice GD25Q80", SPI_3V, Mb008, &HC8, &H4014) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("GigaDevice GD25Q40", SPI_3V, Mb004, &HC8, &H4013) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("GigaDevice GD25Q20", SPI_3V, Mb002, &HC8, &H4012) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("GigaDevice GD25Q10", SPI_3V, Mb001, &HC8, &H4011) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("GigaDevice GD25Q512", SPI_3V, Kb512, &HC8, &H4010) With {.SQI_MODE = SPI_QUAD})
            FlashDB.Add(New SPI_NOR("GigaDevice GD25VQ16C", SPI_3V, Mb016, &HC8, &H4215))
            FlashDB.Add(New SPI_NOR("GigaDevice GD25VQ80C", SPI_3V, Mb008, &HC8, &H4214))
            FlashDB.Add(New SPI_NOR("GigaDevice GD25VQ41B", SPI_3V, Mb004, &HC8, &H4213))
            FlashDB.Add(New SPI_NOR("GigaDevice GD25VQ21B", SPI_3V, Mb002, &HC8, &H4212))
            FlashDB.Add(New SPI_NOR("GigaDevice MD25D16SIG", SPI_3V, Mb016, &H51, &H4015))
            FlashDB.Add(New SPI_NOR("GigaDevice GD25LQ128", SPI_1V8, Mb128, &HC8, &H6018))
            FlashDB.Add(New SPI_NOR("GigaDevice GD25LQ64", SPI_1V8, Mb064, &HC8, &H6017))
            FlashDB.Add(New SPI_NOR("GigaDevice GD25LQ32", SPI_1V8, Mb032, &HC8, &H6016))
            FlashDB.Add(New SPI_NOR("GigaDevice GD25LQ16", SPI_1V8, Mb016, &HC8, &H6015))
            FlashDB.Add(New SPI_NOR("GigaDevice GD25LQ80", SPI_1V8, Mb008, &HC8, &H6014))
            FlashDB.Add(New SPI_NOR("GigaDevice GD25LQ40", SPI_1V8, Mb004, &HC8, &H6013))
            FlashDB.Add(New SPI_NOR("GigaDevice GD25LQ20", SPI_1V8, Mb002, &HC8, &H6012))
            FlashDB.Add(New SPI_NOR("GigaDevice GD25LQ10", SPI_1V8, Mb001, &HC8, &H6011))
            'ISSI
            FlashDB.Add(New SPI_NOR("ISSI IS25LP512", SPI_3V, Mb512, &H9D, &H601A) With {.SEND_EN4B = True})
            FlashDB.Add(New SPI_NOR("ISSI IS25LP256", SPI_3V, Mb256, &H9D, &H6019) With {.SEND_EN4B = True}) 'CV
            FlashDB.Add(New SPI_NOR("ISSI IS25LP128", SPI_3V, Mb128, &H9D, &H6018))
            FlashDB.Add(New SPI_NOR("ISSI IS25LP064", SPI_3V, Mb064, &H9D, &H6017))
            FlashDB.Add(New SPI_NOR("ISSI IS25LP032", SPI_3V, Mb032, &H9D, &H6016))
            FlashDB.Add(New SPI_NOR("ISSI IS25LP016", SPI_3V, Mb016, &H9D, &H6015))
            FlashDB.Add(New SPI_NOR("ISSI IS25LP080", SPI_3V, Mb008, &H9D, &H6014))
            FlashDB.Add(New SPI_NOR("ISSI IS25CD020", SPI_3V, Mb002, &H9D, &H1122))
            FlashDB.Add(New SPI_NOR("ISSI IS25CD010", SPI_3V, Mb001, &H9D, &H1021))
            FlashDB.Add(New SPI_NOR("ISSI IS25CD512", SPI_3V, Kb512, &H9D, &H520))
            FlashDB.Add(New SPI_NOR("ISSI IS25CD025", SPI_3V, Kb256, &H7F, &H9D2F))
            FlashDB.Add(New SPI_NOR("ISSI IS25CQ032", SPI_3V, Mb032, &H7F, &H9D46))
            FlashDB.Add(New SPI_NOR("ISSI IS25LQ032", SPI_3V, Mb032, &H9D, &H4016))
            FlashDB.Add(New SPI_NOR("ISSI IS25LQ016", SPI_3V, Mb016, &H9D, &H4015))
            FlashDB.Add(New SPI_NOR("ISSI IS25LQ080", SPI_3V, Mb008, &H9D, &H1344))
            FlashDB.Add(New SPI_NOR("ISSI IS25LQ040B", SPI_3V, Mb004, &H9D, &H4013))
            FlashDB.Add(New SPI_NOR("ISSI IS25LQ040", SPI_3V, Mb004, &H7F, &H9D43))
            FlashDB.Add(New SPI_NOR("ISSI IS25LQ020", SPI_3V, Mb002, &H7F, &H9D42))
            FlashDB.Add(New SPI_NOR("ISSI IS25LQ020", SPI_3V, Mb002, &H9D, &H4012))
            FlashDB.Add(New SPI_NOR("ISSI IS25LQ010", SPI_3V, Mb001, &H9D, &H4011))
            FlashDB.Add(New SPI_NOR("ISSI IS25LQ512", SPI_3V, Kb512, &H9D, &H4010))
            FlashDB.Add(New SPI_NOR("ISSI IS25LQ025", SPI_3V, Kb256, &H9D, &H4009))
            FlashDB.Add(New SPI_NOR("ISSI IS25LD040", SPI_3V, Mb004, &H7F, &H9D7E))
            FlashDB.Add(New SPI_NOR("ISSI IS25WP256", SPI_1V8, Mb256, &H9D, &H7019))
            FlashDB.Add(New SPI_NOR("ISSI IS25WP128", SPI_1V8, Mb128, &H9D, &H7018))
            FlashDB.Add(New SPI_NOR("ISSI IS25WP064", SPI_1V8, Mb064, &H9D, &H7017))
            FlashDB.Add(New SPI_NOR("ISSI IS25WP032", SPI_1V8, Mb032, &H9D, &H7016))
            FlashDB.Add(New SPI_NOR("ISSI IS25WP016", SPI_1V8, Mb016, &H9D, &H7015))
            FlashDB.Add(New SPI_NOR("ISSI IS25WP080", SPI_1V8, Mb008, &H9D, &H7014))
            FlashDB.Add(New SPI_NOR("ISSI IS25WP040", SPI_1V8, Mb004, &H9D, &H7013))
            FlashDB.Add(New SPI_NOR("ISSI IS25WP020", SPI_1V8, Mb002, &H9D, &H7012))
            FlashDB.Add(New SPI_NOR("ISSI IS25WQ040", SPI_1V8, Mb004, &H9D, &H1253))
            FlashDB.Add(New SPI_NOR("ISSI IS25WQ020", SPI_1V8, Mb002, &H9D, &H1152))
            FlashDB.Add(New SPI_NOR("ISSI IS25WD040", SPI_1V8, Mb004, &H7F, &H9D33))
            FlashDB.Add(New SPI_NOR("ISSI IS25WD020", SPI_1V8, Mb002, &H7F, &H9D32))
            'ESMT
            FlashDB.Add(New SPI_NOR("ESMT F25L64QA", SPI_3V, Mb032, &H8C, &H4117))
            FlashDB.Add(New SPI_NOR("ESMT F25L32QA", SPI_3V, Mb032, &H8C, &H4116))
            FlashDB.Add(New SPI_NOR("ESMT F25L16QA", SPI_3V, Mb032, &H8C, &H4115))
            FlashDB.Add(New SPI_NOR("ESMT F25L14QA", SPI_3V, Mb032, &H8C, &H4114))
            FlashDB.Add(New SPI_NOR("ESMT F25L08", SPI_3V, Mb008, &H8C, &H2014) With {.ProgramMode = AAI_Word})
            FlashDB.Add(New SPI_NOR("ESMT F25L08", SPI_3V, Mb008, &H8C, &H13) With {.ProgramMode = AAI_Word}) 'REMS only
            FlashDB.Add(New SPI_NOR("ESMT F25L04", SPI_3V, Mb004, &H8C, &H2013) With {.ProgramMode = AAI_Word})
            FlashDB.Add(New SPI_NOR("ESMT F25L04", SPI_3V, Mb004, &H8C, &H12) With {.ProgramMode = AAI_Word}) 'REMS only
            FlashDB.Add(New SPI_NOR("ESMT F25L64PA", SPI_3V, Mb016, &H8C, &H2017))
            FlashDB.Add(New SPI_NOR("ESMT F25L32PA", SPI_3V, Mb016, &H8C, &H2016))
            FlashDB.Add(New SPI_NOR("ESMT F25L16PA", SPI_3V, Mb016, &H8C, &H2015))
            FlashDB.Add(New SPI_NOR("ESMT F25L08PA", SPI_3V, Mb008, &H8C, &H3014))
            FlashDB.Add(New SPI_NOR("ESMT F25L04PA", SPI_3V, Mb004, &H8C, &H3013))
            FlashDB.Add(New SPI_NOR("ESMT F25L02PA", SPI_3V, Mb002, &H8C, &H3012))
            'Others
            FlashDB.Add(New SPI_NOR("Sanyo LE25FU406B", SPI_3V, Mb004, &H62, &H1E62))
            FlashDB.Add(New SPI_NOR("Sanyo LE25FW406A", SPI_3V, Mb004, &H62, &H1A62))
            FlashDB.Add(New SPI_NOR("Berg_Micro BG25Q32A", SPI_3V, Mb032, &HE0, &H4016))
            FlashDB.Add(New SPI_NOR("XMC XM25QH32B", SPI_3V, Mb032, &H20, &H4016)) 'Rebranded-micron
            FlashDB.Add(New SPI_NOR("XMC XM25QH64A", SPI_3V, Mb064, &H20, &H7017)) 'Rebranded-micron
            FlashDB.Add(New SPI_NOR("XMC XM25QH128A", SPI_3V, Mb128, &H20, &H7018))
            FlashDB.Add(New SPI_NOR("XMC XM25QH128C", SPI_3V, Mb128, &H20, &H2018))
            FlashDB.Add(New SPI_NOR("BOYAMICRO BY25D16", SPI_3V, Mb016, &H68, &H4015))
            FlashDB.Add(New SPI_NOR("BOYAMICRO BY25Q32", SPI_3V, Mb032, &H68, &H4016))
            FlashDB.Add(New SPI_NOR("BOYAMICRO BY25Q64", SPI_3V, Mb064, &H68, &H4017))
            FlashDB.Add(New SPI_NOR("BOYAMICRO BY25Q128A", SPI_3V, Mb128, &H68, &H4018))
            FlashDB.Add(New SPI_NOR("PUYA P25Q32H", SPI_3V, Mb032, &H85, &H6016))
            FlashDB.Add(New SPI_NOR("PUYA P25Q16H", SPI_3V, Mb016, &H85, &H6015))
            FlashDB.Add(New SPI_NOR("PUYA P25Q80H", SPI_3V, Mb008, &H85, &H6014))
            FlashDB.Add(New SPI_NOR("PUYA P25D16H", SPI_3V, Mb016, &H85, &H6015))
            FlashDB.Add(New SPI_NOR("PUYA P25D80H", SPI_3V, Mb008, &H85, &H6014))
            FlashDB.Add(New SPI_NOR("PUYA P25D40H", SPI_3V, Mb004, &H85, &H6013))
            FlashDB.Add(New SPI_NOR("PUYA P25D20H", SPI_3V, Mb002, &H85, &H6012))
            FlashDB.Add(New SPI_NOR("PUYA P25D10H", SPI_3V, Mb001, &H85, &H6011))
            FlashDB.Add(New SPI_NOR("PUYA P25D05H", SPI_3V, Kb512, &H85, &H6010))
            FlashDB.Add(New SPI_NOR("FMD FT25L04", SPI_1V8, Mb004, &HE, &H6013))
            FlashDB.Add(New SPI_NOR("FMD FT25L02", SPI_1V8, Mb001, &HE, &H6012))
            FlashDB.Add(New SPI_NOR("FMD FT25H16", SPI_3V, Mb016, &HE, &H4015))
            FlashDB.Add(New SPI_NOR("FMD FT25H08", SPI_3V, Mb008, &HE, &H4014))
            FlashDB.Add(New SPI_NOR("FMD FT25H04", SPI_3V, Mb004, &HE, &H4013))
            FlashDB.Add(New SPI_NOR("FMD FT25H02", SPI_3V, Mb002, &HE, &H4012))
            FlashDB.Add(New SPI_NOR("XTX XT25F256B", SPI_3V, Mb256, &HB, &H4019) With {.SEND_EN4B = True})
            FlashDB.Add(New SPI_NOR("XTX XT25F128B", SPI_3V, Mb128, &HB, &H4018))
            FlashDB.Add(New SPI_NOR("XTX XT25F64B", SPI_3V, Mb064, &HB, &H4017))
            FlashDB.Add(New SPI_NOR("Zbit_Semi ZB25VQ128", SPI_3V, Mb128, &H5E, &H6018)) 'QUAD mode
            FlashDB.Add(New SPI_NOR("Zbit_Semi ZB25VQ64", SPI_3V, Mb064, &H5E, &H6017)) 'QUAD mode
            FlashDB.Add(New SPI_NOR("Zbit_Semi ZB25VQ32", SPI_3V, Mb032, &H5E, &H6016)) 'QUAD mode
            FlashDB.Add(New SPI_NOR("Zbit_Semi ZB25VQ16", SPI_3V, Mb016, &H5E, &H6015)) 'QUAD mode
            FlashDB.Add(New SPI_NOR("Zbit_Semi ZB25VQ80", SPI_3V, Mb008, &H5E, &H6014)) 'QUAD mode
            FlashDB.Add(New SPI_NOR("Zbit_Semi ZB25VQ40", SPI_3V, Mb004, &H5E, &H6013)) 'QUAD mode
            FlashDB.Add(New SPI_NOR("Zbit_Semi ZB25VQ20", SPI_3V, Mb002, &H5E, &H6012)) 'QUAD mode
            FlashDB.Add(New SPI_NOR("Zbit_Semi ZB25VQ128", SPI_3V, Mb128, &H5E, &H4018)) 'SPI mode
            FlashDB.Add(New SPI_NOR("Zbit_Semi ZB25VQ64", SPI_3V, Mb064, &H5E, &H4017)) 'SPI mode
            FlashDB.Add(New SPI_NOR("Zbit_Semi ZB25VQ32", SPI_3V, Mb032, &H5E, &H4016)) 'SPI mode
            FlashDB.Add(New SPI_NOR("Zbit_Semi ZB25VQ16", SPI_3V, Mb016, &H5E, &H4015)) 'SPI mode
            FlashDB.Add(New SPI_NOR("Zbit_Semi ZB25VQ80", SPI_3V, Mb008, &H5E, &H4014)) 'SPI mode
            FlashDB.Add(New SPI_NOR("Zbit_Semi ZB25VQ40", SPI_3V, Mb004, &H5E, &H4013)) 'SPI mode
            FlashDB.Add(New SPI_NOR("Zbit_Semi ZB25VQ20", SPI_3V, Mb002, &H5E, &H4012)) 'SPI mode
            FlashDB.Add(New SPI_NOR("Zbit_Semi ZB25D40", SPI_3V, Mb004, &H5E, &H3213))
        End Sub

        Private Sub SPINAND_Database()
            FlashDB.Add(New SPI_NAND("Micron MT29F1G01ABA", &H2C, &H14, 2048, 128, 64, 1024, False, SPI_3V)) '1Gb
            FlashDB.Add(New SPI_NAND("Micron MT29F1G01ABB", &H2C, &H15, 2048, 128, 64, 1024, False, SPI_1V8)) '1Gb
            FlashDB.Add(New SPI_NAND("Micron MT29F2G01AAA", &H2C, &H22, 2048, 128, 64, 2048, True, SPI_3V)) '2Gb
            FlashDB.Add(New SPI_NAND("Micron MT29F2G01ABA", &H2C, &H24, 2048, 128, 64, 2048, True, SPI_3V)) '2Gb
            FlashDB.Add(New SPI_NAND("Micron MT29F2G01ABB", &H2C, &H25, 2048, 128, 64, 2048, True, SPI_1V8)) '2Gb
            FlashDB.Add(New SPI_NAND("Micron MT29F4G01ADA", &H2C, &H36, 2048, 128, 64, 4096, True, SPI_3V)) '4Gb
            FlashDB.Add(New SPI_NAND("Micron MT29F4G01AAA", &H2C, &H32, 2048, 128, 64, 4096, True, SPI_3V)) '4Gb
            'GigaDevice
            FlashDB.Add(New SPI_NAND("GigaDevice GD5F1GQ4UB", &HC8, &HD1, 2048, 128, 64, 1024, False, SPI_3V)) '1Gb
            FlashDB.Add(New SPI_NAND("GigaDevice GD5F1GQ4RB", &HC8, &HC1, 2048, 128, 64, 1024, False, SPI_1V8)) '1Gb
            FlashDB.Add(New SPI_NAND("GigaDevice GD5F1GQ4UE", &HC8, &HD9, 2048, 64, 64, 1024, False, SPI_3V)) '1Gb
            FlashDB.Add(New SPI_NAND("GigaDevice GD5F1GQ4RE", &HC8, &HC9, 2048, 64, 64, 1024, False, SPI_1V8)) '1Gb
            FlashDB.Add(New SPI_NAND("GigaDevice GD5F2GQ4UB", &HC8, &HD2, 2048, 128, 64, 2048, False, SPI_3V)) '2Gb
            FlashDB.Add(New SPI_NAND("GigaDevice GD5F2GQ4RB", &HC8, &HC2, 2048, 128, 64, 2048, False, SPI_1V8)) '2Gb
            FlashDB.Add(New SPI_NAND("GigaDevice GD5F4GQ4UA", &HC8, &HF4, 2048, 64, 64, 4096, False, SPI_3V)) '4Gb
            FlashDB.Add(New SPI_NAND("GigaDevice GD5F4GQ4UB", &HC8, &HD4, 4096, 256, 64, 2048, False, SPI_3V)) '4Gb
            FlashDB.Add(New SPI_NAND("GigaDevice GD5F4GQ4RB", &HC8, &HC4, 4096, 256, 64, 2048, False, SPI_1V8)) '4Gb
            FlashDB.Add(New SPI_NAND("GigaDevice GD5F1GQ4UC", &HC8, &HB148, 2048, 128, 64, 1024, False, SPI_3V) With {.READ_CMD_DUMMY = True}) '1Gb
            FlashDB.Add(New SPI_NAND("GigaDevice GD5F1GQ4RC", &HC8, &HA148, 2048, 128, 64, 1024, False, SPI_1V8) With {.READ_CMD_DUMMY = True}) '1Gb
            FlashDB.Add(New SPI_NAND("GigaDevice GD5F2GQ4UC", &HC8, &HB248, 2048, 128, 64, 2048, False, SPI_3V) With {.READ_CMD_DUMMY = True}) '2Gb
            FlashDB.Add(New SPI_NAND("GigaDevice GD5F2GQ4RC", &HC8, &HA248, 2048, 128, 64, 2048, False, SPI_1V8) With {.READ_CMD_DUMMY = True}) '2Gb
            FlashDB.Add(New SPI_NAND("GigaDevice GD5F4GQ4UC", &HC8, &HB468, 4096, 256, 64, 2048, False, SPI_3V) With {.READ_CMD_DUMMY = True}) '4Gb
            FlashDB.Add(New SPI_NAND("GigaDevice GD5F4GQ4RC", &HC8, &HA468, 4096, 256, 64, 2048, False, SPI_1V8) With {.READ_CMD_DUMMY = True}) '4Gb
            FlashDB.Add(New SPI_NAND("GigaDevice GD5F1GQ5RE", &HC8, &H41, 2048, 128, 64, 1024, False, SPI_3V) With {.READ_CMD_DUMMY = True}) '1Gb
            FlashDB.Add(New SPI_NAND("GigaDevice GD5F1GQ5UE", &HC8, &H51, 2048, 128, 64, 1024, False, SPI_1V8) With {.READ_CMD_DUMMY = False}) '1Gb
            'Winbond SPI-NAND 3V
            FlashDB.Add(New SPI_NAND("Winbond W25N512GV", &HEF, &HAA20, 2048, 64, 64, 512, False, SPI_3V)) '512Mb
            FlashDB.Add(New SPI_NAND("Winbond W25N01GV", &HEF, &HAA21, 2048, 64, 64, 1024, False, SPI_3V)) '1Gb
            FlashDB.Add(New SPI_NAND("Winbond W25M02GV", &HEF, &HAB21, 2048, 64, 64, 2048, False, SPI_3V) With {.STACKED_DIES = 2}) '2Gb
            FlashDB.Add(New SPI_NAND("Winbond W25N02KV", &HEF, &HAA22, 2048, 128, 64, 2048, False, SPI_3V)) '2Gb
            'Winbond SPI-NAND 1.8V
            FlashDB.Add(New SPI_NAND("Winbond W25N512GW", &HEF, &HBA20, 2048, 64, 64, 512, False, SPI_1V8)) '512Mb
            FlashDB.Add(New SPI_NAND("Winbond W25N01GW", &HEF, &HBA21, 2048, 64, 64, 1024, False, SPI_1V8)) '1Gb
            FlashDB.Add(New SPI_NAND("Winbond W25M02GW", &HEF, &HBB21, 2048, 64, 64, 2048, False, SPI_1V8) With {.STACKED_DIES = 2}) '2Gb
            'Kioxia (Subsiduary of Toshiba) - 2nd gen. SPI-NAND
            FlashDB.Add(New SPI_NAND("Kioxia TC58CVG0S3", &H98, &HC2, 2048, 64, 64, 1024, False, SPI_3V)) '1Gb (TC58CVG0S3HRAIG)
            FlashDB.Add(New SPI_NAND("Kioxia TC58CVG1S3", &H98, &HCB, 2048, 64, 64, 2048, False, SPI_3V)) '2Gb (TC58CVG1S3HRAIG)
            FlashDB.Add(New SPI_NAND("Kioxia TC58CVG2S0", &H98, &HCD, 4096, 128, 64, 2048, False, SPI_3V)) '4Gb
            FlashDB.Add(New SPI_NAND("Kioxia TC58CYG0S3", &H98, &HB2, 2048, 128, 64, 1024, False, SPI_1V8)) '1Gb
            FlashDB.Add(New SPI_NAND("Kioxia TC58CYG1S3", &H98, &HBB, 2048, 128, 64, 2048, False, SPI_1V8)) '2Gb
            FlashDB.Add(New SPI_NAND("Kioxia TC58CYG2S0", &H98, &HBD, 4096, 256, 64, 2048, False, SPI_1V8)) '4Gb
            FlashDB.Add(New SPI_NAND("Kioxia TC58CVG0S3HRAIJ", &H98, &HE240, 2048, 128, 64, 1024, False, SPI_3V)) '1Gb
            FlashDB.Add(New SPI_NAND("Kioxia TC58CVG1S3HRAIJ", &H98, &HEB40, 2048, 128, 64, 2048, False, SPI_3V)) '2Gb
            FlashDB.Add(New SPI_NAND("Kioxia TC58CVG2S0HRAIJ", &H98, &HED51, 4096, 256, 64, 2048, False, SPI_3V)) '4Gb
            FlashDB.Add(New SPI_NAND("Kioxia TH58CVG3S0HRAIJ", &H98, &HE451, 4096, 256, 64, 4096, False, SPI_3V)) '8Gb
            FlashDB.Add(New SPI_NAND("Kioxia TC58CYG0S3HRAIJ", &H98, &HD240, 2048, 128, 64, 1024, False, SPI_1V8)) '1Gb
            FlashDB.Add(New SPI_NAND("Kioxia TC58CYG1S3HRAIJ", &H98, &HDB40, 2048, 128, 64, 2048, False, SPI_1V8)) '2Gb
            FlashDB.Add(New SPI_NAND("Kioxia TC58CYG2S0HRAIJ", &H98, &HDD51, 4096, 256, 64, 2048, False, SPI_1V8)) '4Gb
            FlashDB.Add(New SPI_NAND("Kioxia TH58CYG3S0HRAIJ", &H98, &HD451, 4096, 256, 64, 4096, False, SPI_1V8)) '8Gb
            'XTX
            FlashDB.Add(New SPI_NAND("XTX PN26Q01A", &HA1, &HC1, 2048, 128, 64, 1024, False, SPI_1V8)) '1Gb
            FlashDB.Add(New SPI_NAND("XTX PN26Q02A", &HA1, &HC2, 2048, 128, 64, 2048, False, SPI_1V8)) '2Gb
            FlashDB.Add(New SPI_NAND("XTX PN26G01A", &HA1, &HE1, 2048, 128, 64, 1024, False, SPI_3V)) '1Gb
            FlashDB.Add(New SPI_NAND("XTX XT26G01A", &HB, &HE1, 2048, 64, 64, 1024, False, SPI_3V)) '1Gb
            FlashDB.Add(New SPI_NAND("XTX XT26G02A", &HB, &HE2, 2048, 64, 64, 2048, False, SPI_3V)) '2Gb
            FlashDB.Add(New SPI_NAND("XTX XT26G01B", &HB, &HF1, 2048, 64, 64, 1024, False, SPI_3V)) '1Gb
            FlashDB.Add(New SPI_NAND("XTX XT26G02B", &HB, &HF2, 2048, 64, 64, 2048, False, SPI_3V)) '2Gb
            'Dosilicon
            FlashDB.Add(New SPI_NAND("Dosilicon DS35Q1GX", &HE5, &H71, 2048, 64, 64, 1024, False, SPI_3V)) '1Gb
            FlashDB.Add(New SPI_NAND("Dosilicon DS35M1GX", &HE5, &H21, 2048, 64, 64, 1024, False, SPI_1V8)) '1Gb
            FlashDB.Add(New SPI_NAND("Dosilicon DS35Q2GX", &HE5, &H72, 2048, 64, 64, 2048, False, SPI_3V)) '2Gb
            FlashDB.Add(New SPI_NAND("Dosilicon DS35M2GX", &HE5, &H22, 2048, 64, 64, 2048, False, SPI_1V8)) '2Gb
            'Others
            FlashDB.Add(New SPI_NAND("MXIC MX35LF1GE4AB", &HC2, &H12, 2048, 64, 64, 1024, False, SPI_3V)) '1Gb
            FlashDB.Add(New SPI_NAND("MXIC MX35LF2GE4AB", &HC2, &H22, 2048, 64, 64, 2048, True, SPI_3V)) '2Gb
            FlashDB.Add(New SPI_NAND("MXIC MX35LF1G24AD", &HC2, &H1403, 2048, 128, 64, 1024, False, SPI_3V)) '1Gb
            FlashDB.Add(New SPI_NAND("MXIC MX35LF2G24AD", &HC2, &H2403, 2048, 128, 64, 2048, False, SPI_3V)) '2Gb
            FlashDB.Add(New SPI_NAND("MXIC MX35LF4G24AD", &HC2, &H3503, 4096, 256, 64, 2048, False, SPI_3V)) '4Gb
            FlashDB.Add(New SPI_NAND("ISSI IS37/38SML01G1", &HC8, &H21, 2048, 64, 64, 1024, False, SPI_3V)) '1Gb
            FlashDB.Add(New SPI_NAND("ESMT F50L1G41A", &HC8, &H217F, 2048, 64, 64, 1024, False, SPI_3V)) '1Gb
            FlashDB.Add(New SPI_NAND("ESMT F50L1G41LB", &HC8, &H17F, 2048, 64, 64, 1024, False, SPI_3V)) '1Gb
            FlashDB.Add(New SPI_NAND("FMSH FM25G01", &HA1, &HF1, 2048, 64, 64, 1024, False, SPI_3V)) '1Gb Shanghai Fudan Microelectronics
            FlashDB.Add(New SPI_NAND("FMSH FM25G02", &HA1, &HF2, 2048, 64, 64, 2048, False, SPI_3V)) '2Gb Shanghai Fudan Microelectronics
        End Sub

        Private Sub MFP_Database()
            'Intel
            FlashDB.Add(New P_NOR("Intel 28F064P30(T)", &H89, &H8817, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Buffer1, MFP_DELAY.SR1) With {.PAGE_SIZE = 64})
            FlashDB.Add(New P_NOR("Intel 28F064P30(B)", &H89, &H881B, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Buffer1, MFP_DELAY.SR1) With {.PAGE_SIZE = 64})
            FlashDB.Add(New P_NOR("Intel 28F128P30(T)", &H89, &H8818, Mb128, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Buffer1, MFP_DELAY.SR1) With {.PAGE_SIZE = 64})
            FlashDB.Add(New P_NOR("Intel 28F128P30(B)", &H89, &H881B, Mb128, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Buffer1, MFP_DELAY.SR1) With {.PAGE_SIZE = 64})
            FlashDB.Add(New P_NOR("Intel 28F256P30(T)", &H89, &H8919, Mb256, VCC_IF.X16_3V, BLKLYT.P30_Top, MFP_PRG.Buffer1, MFP_DELAY.SR1) With {.PAGE_SIZE = 64})
            FlashDB.Add(New P_NOR("Intel 28F256P30(B)", &H89, &H891C, Mb256, VCC_IF.X16_3V, BLKLYT.P30_Btm, MFP_PRG.Buffer1, MFP_DELAY.SR1) With {.PAGE_SIZE = 64})
            FlashDB.Add(New P_NOR("Intel 28F640P33(T)", &H89, &H881D, Mb064, VCC_IF.X16_3V, BLKLYT.P30_Top, MFP_PRG.Buffer1, MFP_DELAY.SR1) With {.PAGE_SIZE = 64})
            FlashDB.Add(New P_NOR("Intel 28F640P33(B)", &H89, &H8820, Mb064, VCC_IF.X16_3V, BLKLYT.P30_Btm, MFP_PRG.Buffer1, MFP_DELAY.SR1) With {.PAGE_SIZE = 64})
            FlashDB.Add(New P_NOR("Intel 28F128P33(T)", &H89, &H881E, Mb128, VCC_IF.X16_3V, BLKLYT.P30_Top, MFP_PRG.Buffer1, MFP_DELAY.SR1) With {.PAGE_SIZE = 64})
            FlashDB.Add(New P_NOR("Intel 28F128P33(B)", &H89, &H8821, Mb128, VCC_IF.X16_3V, BLKLYT.P30_Btm, MFP_PRG.Buffer1, MFP_DELAY.SR1) With {.PAGE_SIZE = 64})
            FlashDB.Add(New P_NOR("Intel A28F512", &H89, &HB8, Kb512, VCC_IF.X8_5V_VPP, BLKLYT.EntireDevice, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Intel 28F320J5", &H89, &H14, Mb032, VCC_IF.X16_5V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer1, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F640J5", &H89, &H15, Mb064, VCC_IF.X16_5V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer1, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F320J3", &H89, &H16, Mb032, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer1, MFP_DELAY.SR1)) '32 byte buffers
            FlashDB.Add(New P_NOR("Intel 28F640J3", &H89, &H17, Mb064, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer1, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F128J3", &H89, &H18, Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer1, MFP_DELAY.SR1)) '(CV)
            FlashDB.Add(New P_NOR("Intel 28F256J3", &H89, &H1D, Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer1, MFP_DELAY.SR1))
            'Intel C3
            FlashDB.Add(New P_NOR("Intel 28F800C3(T)", &H89, &H88C0, Mb008, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F800C3(B)", &H89, &H88C1, Mb008, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F160C3(T)", &H89, &H88C2, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F160C3(B)", &H89, &H88C3, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F320C3(T)", &H89, &H88C4, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F320C3(B)", &H89, &H88C5, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F640C3(T)", &H89, &H88CC, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F640C3(B)", &H89, &H88CD, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            'Intel B3
            FlashDB.Add(New P_NOR("Intel 28F400B3(T)", &H89, &H8894, Mb004, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F400B3(B)", &H89, &H8895, Mb004, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F800B3(T)", &H89, &H8892, Mb008, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F800B3(B)", &H89, &H8893, Mb008, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F160B3(T)", &H89, &H8890, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F160B3(B)", &H89, &H8891, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F320B3(T)", &H89, &H8896, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F320B3(B)", &H89, &H8897, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F640B3(T)", &H89, &H8898, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F640B3(B)", &H89, &H8899, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            'Intel SA
            FlashDB.Add(New P_NOR("Intel 28F008SA", &H89, &HA2, Mb008, VCC_IF.X8_5V_VPP, BLKLYT.Kb512_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F016SA", &H89, &H66A0, Mb032, VCC_IF.X16_5V, BLKLYT.Kb512_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F032SA", 0, 0, Mb032, VCC_IF.X16_5V, BLKLYT.Kb512_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1)) 'DD28F032SA uses two 28F016SA, A21 for multi-ce 
            'Intel B5
            FlashDB.Add(New P_NOR("Intel 28F200B5(T)", &H89, &H2274, Mb002, VCC_IF.X16_5V_VPP, BLKLYT.Four_Top_B5, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F200B5(B)", &H89, &H2275, Mb002, VCC_IF.X16_5V_VPP, BLKLYT.Four_Btm_B5, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F400B5(T)", &H89, &H4470, Mb004, VCC_IF.X16_5V_VPP, BLKLYT.Four_Top_B5, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F400B5(B)", &H89, &H4471, Mb004, VCC_IF.X16_5V_VPP, BLKLYT.Four_Btm_B5, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F800B5(T)", &H89, &H889C, Mb008, VCC_IF.X16_5V_VPP, BLKLYT.Four_Top_B5, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F800B5(B)", &H89, &H889D, Mb008, VCC_IF.X16_5V_VPP, BLKLYT.Four_Btm_B5, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F004B5(T)", &H89, &H78, Mb004, VCC_IF.X8_5V_VPP, BLKLYT.Four_Top_B5, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Intel 28F004B5(B)", &H89, &H79, Mb004, VCC_IF.X8_5V_VPP, BLKLYT.Four_Btm_B5, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            'AMD
            FlashDB.Add(New P_NOR("AMD AM29F200(T)", &H1, &H2251, Mb002, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29F200(B)", &H1, &H2252, Mb002, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29LV002B(T)", &H1, &H40, Mb002, VCC_IF.X8_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS)) 'TSOP40 (TYPE-B) CV
            FlashDB.Add(New P_NOR("AMD AM29LV002B(B)", &H1, &HC2, Mb002, VCC_IF.X8_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29LV065D", &H1, &H93, Mb064, VCC_IF.X8_3V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29F040", &H1, &HA4, Mb004, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7) With {.ERASE_DELAY = 500, .RESET_ENABLED = False})
            FlashDB.Add(New P_NOR("AMD AM29F010B", &H1, &H20, Mb001, VCC_IF.X8_5V, BLKLYT.Kb128_Uni, MFP_PRG.Standard, MFP_DELAY.uS) With {.ERASE_DELAY = 500, .RESET_ENABLED = False})
            FlashDB.Add(New P_NOR("AMD AM29F040B", &H20, &HE2, Mb004, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS) With {.ERASE_DELAY = 500, .RESET_ENABLED = False}) 'Why is this not: 01 A4? (PLCC32 and DIP32 tested)
            FlashDB.Add(New P_NOR("AMD AM29F080B", &H1, &HD5, Mb008, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS) With {.ERASE_DELAY = 500, .RESET_ENABLED = False}) 'TSOP40
            FlashDB.Add(New P_NOR("AMD AM29F160DT", 0, 0, Mb016, VCC_IF.X16_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS)) 'Uses Micron M29F160FT
            FlashDB.Add(New P_NOR("AMD AM29F160DB", 0, 0, Mb016, VCC_IF.X16_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS)) 'Uses Micron M29F160FB
            FlashDB.Add(New P_NOR("AMD AM29F016B", &H1, &HAD, Mb016, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS)) '(CV)
            FlashDB.Add(New P_NOR("AMD AM29F016D", &H1, &HAD, Mb016, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS)) 'TSOP40 CV
            FlashDB.Add(New P_NOR("AMD AM29F032B", &H1, &H41, Mb032, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS)) 'TSOP40
            FlashDB.Add(New P_NOR("AMD AM29LV200(T)", &H1, &H223B, Mb002, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29LV200(B)", &H1, &H22BF, Mb002, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29F200(T)", &H1, &H2251, Mb002, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29F200(B)", &H1, &H2257, Mb002, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29LV400(T)", &H1, &H22B9, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29LV400(B)", &H1, &H22BA, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29F400(T)", &H1, &H2223, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29F400(B)", &H1, &H22AB, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS)) '<-- please verify
            FlashDB.Add(New P_NOR("AMD AM29LV800(T)", &H1, &H22DA, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29LV800(B)", &H1, &H225B, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS)) '(CV)
            FlashDB.Add(New P_NOR("AMD AM29F800(T)", &H1, &H22D6, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29F800(B)", &H1, &H2258, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29LV160B(T)", &H1, &H22C4, Mb016, VCC_IF.X16_X8_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS)) '(CV)
            FlashDB.Add(New P_NOR("AMD AM29LV160B(B)", &H1, &H2249, Mb016, VCC_IF.X16_X8_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29DL161D(T)", &H1, &H2236, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29DL162D(T)", &H1, &H222D, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29DL163D(T)", &H1, &H2228, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29DL164D(T)", &H1, &H2233, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29DL161D(B)", &H1, &H2239, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29DL162D(B)", &H1, &H222E, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29DL163D(B)", &H1, &H222B, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29DL164D(B)", &H1, &H2235, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29DL322G(T)", &H1, &H2255, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29DL322G(B)", &H1, &H2256, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29DL323G(T)", &H1, &H2250, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29DL323G(B)", &H1, &H2253, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29DL324G(T)", &H1, &H225C, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29DL324G(B)", &H1, &H225F, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29LV320D(T)", &H1, &H22F6, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29LV320D(B)", &H1, &H22F9, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29LV320M(T)", &H1, &H2201, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29LV320M(B)", &H1, &H2200, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("AMD AM29LV640ML", 0, 0, Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS)) 'Uses S29GL064M
            'Sharp
            FlashDB.Add(New P_NOR("Sharp LHF00L15", &HB0, &HA1, Mb032, VCC_IF.X16_3V, BLKLYT.Mb032_NonUni, MFP_PRG.IntelSharp, MFP_DELAY.SR1)) '(CV)
            FlashDB.Add(New P_NOR("Sharp LH28F160S3", &HB0, &HD0, Mb016, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Sharp LH28F320S3", &HB0, &HD4, Mb032, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Sharp LH28F160BJE", &HB0, &HE9, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Sharp LH28F320BJE", &HB0, &HE3, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("Sharp LH28F008SCT", &H89, &HA6, Mb008, VCC_IF.X8_3V, BLKLYT.Kb512_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1)) 'TSOP40
            FlashDB.Add(New P_NOR("Sharp LH28F016SCT", &H89, &HAA, Mb016, VCC_IF.X8_3V, BLKLYT.Kb512_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1)) 'TSOP40
            FlashDB.Add(New P_NOR("Sharp LH28F160BJHE", &HB0, &HE8, Mb016, VCC_IF.X16_3V, BLKLYT.Sharp_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Sharp LH28F160BG(T)", &HB0, &H68, Mb016, VCC_IF.X16_3V, BLKLYT.Sharp_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Sharp LH28F160BG(B)", &HB0, &H69, Mb016, VCC_IF.X16_3V, BLKLYT.Sharp_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Sharp LH28F320BFHE", &HB0, &HB4, Mb032, VCC_IF.X16_3V, BLKLYT.Sharp_Top, MFP_PRG.Buffer1, MFP_DELAY.SR3)) '(CV)
            FlashDB.Add(New P_NOR("Sharp LH28F320BJHE", &HB0, &HE3, Mb032, VCC_IF.X16_3V, BLKLYT.Sharp_Top, MFP_PRG.Buffer1, MFP_DELAY.SR3))
            FlashDB.Add(New P_NOR("Sharp LH28F640BFHE", &HB0, &HB3, Mb064, VCC_IF.X16_3V, BLKLYT.Sharp_Btm, MFP_PRG.Buffer1, MFP_DELAY.SR3))
            FlashDB.Add(New P_NOR("Sharp LH28F640BFHG", &HB0, &HB1, Mb064, VCC_IF.X16_3V, BLKLYT.Sharp_Top, MFP_PRG.Buffer1, MFP_DELAY.SR3))
            FlashDB.Add(New P_NOR("Sharp LH28F640SPHT", &HB0, &H17, Mb064, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer1, MFP_DELAY.SR3))
            FlashDB.Add(New P_NOR("Sharp LH28F128BFHT", &HB0, &H10, Mb128, VCC_IF.X16_3V, BLKLYT.Sharp_Top, MFP_PRG.Buffer1, MFP_DELAY.SR3)) '(CV)
            FlashDB.Add(New P_NOR("Sharp LH28F128SPHTD", &HB0, &H18, Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer1, MFP_DELAY.SR3))
            'FlashDB.Add(New P_NOR("Sharp LH28F016SUT", &HB0, &HB0, Mb128, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Buffer1, MFP_DELAY.SR3) With {.PAGE_SIZE = 256})
            'Winbond
            FlashDB.Add(New P_NOR("Winbond W49F020", &HDA, &H8C, Mb002, VCC_IF.X8_5V, BLKLYT.EntireDevice, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Winbond W49F002U", &HDA, &HB, Mb002, VCC_IF.X8_5V, BLKLYT.Mb002_NonUni, MFP_PRG.Standard, MFP_DELAY.uS) With {.PAGE_SIZE = 128, .HARDWARE_DELAY = 18})
            FlashDB.Add(New P_NOR("Winbond W29EE512", &HDA, &HC8, Kb512, VCC_IF.X8_5V, BLKLYT.Kb256_Uni, MFP_PRG.PageMode, MFP_DELAY.DQ7) With {.PAGE_SIZE = 128, .ERASE_REQUIRED = False, .RESET_ENABLED = False})
            FlashDB.Add(New P_NOR("Winbond W29C010", &HDA, &HC1, Mb001, VCC_IF.X8_5V, BLKLYT.Kb256_Uni, MFP_PRG.PageMode, MFP_DELAY.mS) With {.PAGE_SIZE = 128, .ERASE_REQUIRED = False})
            FlashDB.Add(New P_NOR("Winbond W29C020", &HDA, &H45, Mb002, VCC_IF.X8_5V, BLKLYT.Kb256_Uni, MFP_PRG.PageMode, MFP_DELAY.mS) With {.PAGE_SIZE = 128, .ERASE_REQUIRED = False})
            FlashDB.Add(New P_NOR("Winbond W29C040", &HDA, &H46, Mb004, VCC_IF.X8_5V, BLKLYT.Kb256_Uni, MFP_PRG.PageMode, MFP_DELAY.mS) With {.PAGE_SIZE = 256, .ERASE_REQUIRED = False})
            FlashDB.Add(New P_NOR("Winbond W29GL256S", &HEF, &H227E, Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2201) With {.PAGE_SIZE = 512})
            FlashDB.Add(New P_NOR("Winbond W29GL256P", &HEF, &H227E, Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2201) With {.PAGE_SIZE = 64})
            FlashDB.Add(New P_NOR("Winbond W29GL032C", 0, 0, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Standard, MFP_DELAY.uS)) 'Rebranded S29GL032M
            FlashDB.Add(New P_NOR("Winbond W29GL064C", 0, 0, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Standard, MFP_DELAY.uS)) 'Rebranded S29GL064M
            FlashDB.Add(New P_NOR("Winbond W29GL128C", 0, 0, Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Standard, MFP_DELAY.uS)) 'Rebranded S29G128M
            'SST
            FlashDB.Add(New P_NOR("SST 29EE010", &HBF, &H7, Mb001, VCC_IF.X8_5V, BLKLYT.Kb256_Uni, MFP_PRG.PageMode, MFP_DELAY.DQ7) With {.PAGE_SIZE = 128, .ERASE_REQUIRED = False, .RESET_ENABLED = False})
            FlashDB.Add(New P_NOR("SST 29LE010/29VE010", &HBF, &H8, Mb001, VCC_IF.X8_3V, BLKLYT.Kb256_Uni, MFP_PRG.PageMode, MFP_DELAY.DQ7) With {.PAGE_SIZE = 128, .ERASE_REQUIRED = False, .RESET_ENABLED = False})
            FlashDB.Add(New P_NOR("SST 39VF401C/39LF401C", &HBF, &H2321, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("SST 39VF402C/39LF402C", &HBF, &H2322, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("SST 39SF512", &HBF, &HB4, Kb512, VCC_IF.X8_5V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("SST 39SF010", &HBF, &HB5, Mb001, VCC_IF.X8_5V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("SST 39SF020", &HBF, &HB6, Mb002, VCC_IF.X8_5V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("SST 39SF040", &HBF, &HB7, Mb004, VCC_IF.X8_5V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("SST 39LF010", &HBF, &HD5, Mb001, VCC_IF.X8_5V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("SST 39LF020", &HBF, &HD6, Mb002, VCC_IF.X8_5V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("SST 39LF040", &HBF, &HD7, Mb004, VCC_IF.X8_5V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("SST 39LF200/39VF200", &HBF, &H2789, Mb002, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("SST 39LF400/39VF200", &HBF, &H2780, Mb004, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("SST 39LF800/39VF800", &HBF, &H2781, Mb008, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("SST 39VF160", &HBF, &H2782, Mb016, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("SST 39VF1681", &HBF, &HC8, Mb016, VCC_IF.X8_3V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS)) '(CV)
            FlashDB.Add(New P_NOR("SST 39VF1682", &HBF, &HC9, Mb016, VCC_IF.X8_3V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("SST 39VF1601", &HBF, &H234B, Mb016, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("SST 39VF1601C", &HBF, &H234F, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("SST 39VF1602", &HBF, &H234A, Mb016, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("SST 39VF1602C", &HBF, &H234E, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("SST 39VF3201", &HBF, &H235B, Mb032, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("SST 39VF3201B", &HBF, &H235D, Mb032, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("SST 39VF3202", &HBF, &H235A, Mb032, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("SST 39VF3202B", &HBF, &H235C, Mb032, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("SST 39VF6401", &HBF, &H236B, Mb064, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("SST 39VF6402", &HBF, &H236A, Mb064, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("SST 39VF6401B", &HBF, &H236D, Mb064, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("SST 39VF6402B", &HBF, &H236C, Mb064, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7))
            'Atmel
            FlashDB.Add(New P_NOR("Atmel AT29C010", &H1F, &HD5, Mb001, VCC_IF.X8_5V, BLKLYT.Kb256_Uni, MFP_PRG.PageMode, MFP_DELAY.DQ7) With {.ERASE_REQUIRED = False, .PAGE_SIZE = 128, .RESET_ENABLED = False})
            FlashDB.Add(New P_NOR("Atmel AT29C020", &H1F, &HDA, Mb002, VCC_IF.X8_5V, BLKLYT.Kb256_Uni, MFP_PRG.PageMode, MFP_DELAY.DQ7) With {.ERASE_REQUIRED = False, .PAGE_SIZE = 256, .RESET_ENABLED = False})
            FlashDB.Add(New P_NOR("Atmel AT29C040", &H1F, &HA4, Mb004, VCC_IF.X8_5V, BLKLYT.Kb256_Uni, MFP_PRG.PageMode, MFP_DELAY.DQ7) With {.ERASE_REQUIRED = False, .PAGE_SIZE = 256, .RESET_ENABLED = False})
            FlashDB.Add(New P_NOR("Atmel AT49F512", &H1F, &H3, Kb512, VCC_IF.X8_5V, BLKLYT.EntireDevice, MFP_PRG.Standard, MFP_DELAY.uS)) 'No SE, only BE
            FlashDB.Add(New P_NOR("Atmel AT49F010", &H1F, &H17, Mb001, VCC_IF.X8_5V, BLKLYT.EntireDevice, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Atmel AT49F020", &H1F, &HB, Mb002, VCC_IF.X8_5V, BLKLYT.EntireDevice, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Atmel AT49F040", &H1F, &H13, Mb004, VCC_IF.X8_5V, BLKLYT.EntireDevice, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Atmel AT49F040T", &H1F, &H12, Mb004, VCC_IF.X8_5V, BLKLYT.EntireDevice, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Atmel AT49BV/LV16X", &H1F, &HC0, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Standard, MFP_DELAY.uS)) 'Supports Single Pulse Byte/ Word Program
            FlashDB.Add(New P_NOR("Atmel AT49BV/LV16XT", &H1F, &HC2, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Atmel AT49BV322D(B)", &H1F, &H_01C8, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Atmel AT49BV322D(T)", &H1F, &H_01C9, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            'MXIC
            FlashDB.Add(New P_NOR("MXIC MX29F040", &HC2, &HA4, Mb004, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("MXIC MX29F080", &HC2, &HD5, Mb008, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("MXIC MX29F016", &HC2, &HAD, Mb016, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("MXIC MX29F800T", &HC2, &H22D6, Mb008, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS)) 'SO44 CV
            FlashDB.Add(New P_NOR("MXIC MX29F800B", &HC2, &H2258, Mb008, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("MXIC MX29F1610", &HC2, &HF1, Mb016, VCC_IF.X16_5V, BLKLYT.Mb001_Uni, MFP_PRG.PageMode, MFP_DELAY.mS) With {.PAGE_SIZE = 64, .HARDWARE_DELAY = 8})
            FlashDB.Add(New P_NOR("MXIC MX29F1610MC", &HC2, &HF7, Mb016, VCC_IF.X16_5V, BLKLYT.Mb001_Uni, MFP_PRG.PageMode, MFP_DELAY.mS) With {.PAGE_SIZE = 64, .HARDWARE_DELAY = 8})
            FlashDB.Add(New P_NOR("MXIC MX29F1610MC", &HC2, &HF8, Mb016, VCC_IF.X16_5V, BLKLYT.Mb001_Uni, MFP_PRG.PageMode, MFP_DELAY.mS) With {.PAGE_SIZE = 64, .HARDWARE_DELAY = 12})
            FlashDB.Add(New P_NOR("MXIC MX29F1610A", &HC2, &HFA, Mb016, VCC_IF.X16_5V, BLKLYT.Mb001_Uni, MFP_PRG.PageMode, MFP_DELAY.mS) With {.PAGE_SIZE = 64, .HARDWARE_DELAY = 8})
            FlashDB.Add(New P_NOR("MXIC MX29F1610B", &HC2, &HFB, Mb016, VCC_IF.X16_5V, BLKLYT.Mb001_Uni, MFP_PRG.PageMode, MFP_DELAY.mS) With {.PAGE_SIZE = 64, .HARDWARE_DELAY = 8})
            FlashDB.Add(New P_NOR("MXIC MX29F1615", &HC2, &H6B, Mb016, VCC_IF.X16_5V, BLKLYT.EntireDevice, MFP_PRG.PageMode, MFP_DELAY.mS) With {.PAGE_SIZE = 64, .HARDWARE_DELAY = 8}) 'Needs to be tested
            FlashDB.Add(New P_NOR("MXIC MX29SL800CT", &HC2, &H22EA, Mb008, VCC_IF.X16_1V8, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.DQ7)) 'Untested
            FlashDB.Add(New P_NOR("MXIC MX29SL800CB", &HC2, &H226B, Mb008, VCC_IF.X16_1V8, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.DQ7)) 'Untested
            FlashDB.Add(New P_NOR("MXIC MX29L3211", &HC2, &HF9, Mb032, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.PageMode, MFP_DELAY.SR2) With {.PAGE_SIZE = 64}) 'Actualy supports up to 256 bytes (tested build 595)
            FlashDB.Add(New P_NOR("MXIC MX29LV040", &HC2, &H4F, Mb004, VCC_IF.X8_3V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS)) '(CV)
            FlashDB.Add(New P_NOR("MXIC MX29LV400T", &HC2, &H22B9, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("MXIC MX29LV400B", &HC2, &H22BA, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("MXIC MX29LV800T", &HC2, &H22DA, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("MXIC MX29LV800B", &HC2, &H225B, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("MXIC MX29LV160DT", &HC2, &H22C4, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS) With {.HARDWARE_DELAY = 6}) 'Required! SO-44 in CV
            FlashDB.Add(New P_NOR("MXIC MX29LV160DB", &HC2, &H2249, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS) With {.HARDWARE_DELAY = 6})
            FlashDB.Add(New P_NOR("MXIC MX29LV320T", &HC2, &H22A7, Mb032, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS) With {.HARDWARE_DELAY = 0})
            FlashDB.Add(New P_NOR("MXIC MX29LV320B", &HC2, &H22A8, Mb032, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS) With {.HARDWARE_DELAY = 0})
            FlashDB.Add(New P_NOR("MXIC MX29LV640ET", &HC2, &H22C9, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("MXIC MX29LV640EB", &HC2, &H22CB, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("MXIC MX29GL640ET", &HC2, &H227E, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H1001) With {.HARDWARE_DELAY = 6})
            FlashDB.Add(New P_NOR("MXIC MX29GL640EB", &HC2, &H227E, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H1000) With {.HARDWARE_DELAY = 6})
            FlashDB.Add(New P_NOR("MXIC MX29GL640E(H/L)", &HC2, &H227E, Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &HC01) With {.HARDWARE_DELAY = 6})
            FlashDB.Add(New P_NOR("MXIC MX29GL128F", &HC2, &H227E, Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2101) With {.HARDWARE_DELAY = 6}) '(CV)
            FlashDB.Add(New P_NOR("MXIC MX29GL256F", &HC2, &H227E, Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2201) With {.HARDWARE_DELAY = 6})
            FlashDB.Add(New P_NOR("MXIC MX29LV128DT", &HC2, &H227E, Mb128, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("MXIC MX29LV128DB", &HC2, &H227A, Mb128, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Standard, MFP_DELAY.DQ7))
            'Cypress / Spansion
            FlashDB.Add(New P_NOR("Cypress S29AL004D(B)", &H1, &H22BA, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Cypress S29AL004D(T)", &H1, &H22B9, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Cypress S29AL008J(B)", &H1, &H225B, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS)) 'Am29LV800B
            FlashDB.Add(New P_NOR("Cypress S29AL008J(T)", &H1, &H22DA, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS)) 'Am29LV800T
            FlashDB.Add(New P_NOR("Cypress S29AL008D(B)", 0, 0, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS)) 'Am29LV800B
            FlashDB.Add(New P_NOR("Cypress S29AL008D(T)", 0, 0, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS)) 'Am29LV800T
            FlashDB.Add(New P_NOR("Cypress S29AL016M(B)", &H1, &H2249, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Cypress S29AL016M(T)", &H1, &H22C4, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Cypress S29AL016D(B)", &H1, &H2249, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Cypress S29AL016D(T)", &H1, &H22C4, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Cypress S29AL016J(T)", &H1, &H22C4, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Cypress S29AL016J(B)", &H1, &H2249, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Cypress S29AL032D", &H1, &HA3, Mb032, VCC_IF.X8_3V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS)) 'Available in TSOP-40
            FlashDB.Add(New P_NOR("Cypress S29AL032D(B)", &H1, &H22F9, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Cypress S29AL032D(T)", &H1, &H22F6, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Cypress S29GL128", &H1, &H227E, Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.uS, &H2100) With {.PAGE_SIZE = 64}) 'We need to test this device
            FlashDB.Add(New P_NOR("Cypress S29GL256", &H1, &H227E, Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.uS, &H2200) With {.PAGE_SIZE = 64})
            FlashDB.Add(New P_NOR("Cypress S29GL512", &H1, &H227E, Mb512, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.uS, &H2300) With {.PAGE_SIZE = 64})
            FlashDB.Add(New P_NOR("Cypress S29GL01G", &H1, &H227E, Gb001, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.uS, &H2800) With {.PAGE_SIZE = 64})
            FlashDB.Add(New P_NOR("Cypress S29JL032J(T)", &H1, &H227E, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7, &HA01))
            FlashDB.Add(New P_NOR("Cypress S29JL032J(B)", &H1, &H227E, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7, &HA00))
            FlashDB.Add(New P_NOR("Cypress S29JL064J", &H1, &H227E, Mb064, VCC_IF.X16_3V, BLKLYT.Dual, MFP_PRG.BypassMode, MFP_DELAY.DQ7, &H201)) 'Top and bottom boot blocks (CV)
            FlashDB.Add(New P_NOR("Cypress S29GL032M", &H1, &H7E, Mb032, VCC_IF.X8_3V, BLKLYT.Kb512_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H1C00) With {.PAGE_SIZE = 32}) 'R0
            FlashDB.Add(New P_NOR("Cypress S29GL032M", &H1, &H227E, Mb032, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H1D00) With {.PAGE_SIZE = 32}) 'R1,R2,R8,R9
            FlashDB.Add(New P_NOR("Cypress S29GL032M(B)", &H1, &H227E, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H1A00) With {.PAGE_SIZE = 32}) 'R4,R6
            FlashDB.Add(New P_NOR("Cypress S29GL032M(T)", &H1, &H227E, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H1A01) With {.PAGE_SIZE = 32}) 'R3,R5 (CV) - Labeled as Winbond
            FlashDB.Add(New P_NOR("Cypress S29JL032J(B)", &H1, &H225F, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7)) 'Bottom-Boot
            FlashDB.Add(New P_NOR("Cypress S29JL032J(T)", &H1, &H225C, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7)) 'Top-Boot
            FlashDB.Add(New P_NOR("Cypress S29JL032J(B)", &H1, &H2253, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7)) 'Bottom-Boot
            FlashDB.Add(New P_NOR("Cypress S29JL032J(T)", &H1, &H2250, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7)) 'Top-Boot
            FlashDB.Add(New P_NOR("Cypress S29JL032J(B)", &H1, &H2256, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7)) 'Bottom-Boot
            FlashDB.Add(New P_NOR("Cypress S29JL032J(T)", &H1, &H2255, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7)) 'Top-Boot
            FlashDB.Add(New P_NOR("Cypress S29GL064M", &H1, &H227E, Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H1300) With {.PAGE_SIZE = 32}) 'Model R0
            FlashDB.Add(New P_NOR("Cypress S29GL064M", &H1, &H227E, Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &HC01) With {.PAGE_SIZE = 32}) 'CV (as AM29LV640ML)
            FlashDB.Add(New P_NOR("Cypress S29GL064M(T)", &H1, &H227E, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H1001) With {.PAGE_SIZE = 32}) 'Top-Boot
            FlashDB.Add(New P_NOR("Cypress S29GL064M(B)", &H1, &H227E, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H1000) With {.PAGE_SIZE = 32}) 'Bottom-Boot
            FlashDB.Add(New P_NOR("Cypress S29GL064M", &H1, &H227E, Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H1301) With {.PAGE_SIZE = 32})
            FlashDB.Add(New P_NOR("Cypress S29GL128M", &H1, &H227E, Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H1200) With {.PAGE_SIZE = 32})
            FlashDB.Add(New P_NOR("Cypress S29GL256M", &H1, &H227E, Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H1201) With {.PAGE_SIZE = 32})
            FlashDB.Add(New P_NOR("Cypress S29GL064N", &H1, &H227E, Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &HC01) With {.PAGE_SIZE = 32})
            FlashDB.Add(New P_NOR("Cypress S29GL128N", &H1, &H227E, Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2101) With {.PAGE_SIZE = 32})
            FlashDB.Add(New P_NOR("Cypress S29GL256N", &H1, &H227E, Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2201) With {.PAGE_SIZE = 32}) '(CV)
            FlashDB.Add(New P_NOR("Cypress S29GL512N", &H1, &H227E, Mb512, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2301) With {.PAGE_SIZE = 32})
            FlashDB.Add(New P_NOR("Cypress S29GL128S", &H1, &H227E, Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2101) With {.PAGE_SIZE = 512}) '(CV) BGA-64
            FlashDB.Add(New P_NOR("Cypress S29GL256S", &H1, &H227E, Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2201) With {.PAGE_SIZE = 512})
            FlashDB.Add(New P_NOR("Cypress S29GL512S", &H1, &H227E, Mb512, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.SR1, &H2301) With {.PAGE_SIZE = 512}) '(CV)
            FlashDB.Add(New P_NOR("Cypress S29GL01GP", &H1, &H227E, Gb001, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2801) With {.PAGE_SIZE = 64})
            FlashDB.Add(New P_NOR("Cypress S29GL512P", &H1, &H227E, Mb512, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2301) With {.PAGE_SIZE = 64})
            FlashDB.Add(New P_NOR("Cypress S29GL256P", &H1, &H227E, Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2201) With {.PAGE_SIZE = 64})
            FlashDB.Add(New P_NOR("Cypress S29GL128P", &H1, &H227E, Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2101) With {.PAGE_SIZE = 64}) '(CV)
            FlashDB.Add(New P_NOR("Cypress S29GL512T", &H1, &H227E, Mb512, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.SR1, &H2301) With {.PAGE_SIZE = 512})
            FlashDB.Add(New P_NOR("Cypress S29GL01GS", &H1, &H227E, Gb001, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.SR1, &H2401) With {.PAGE_SIZE = 512})
            FlashDB.Add(New P_NOR("Cypress S29GL01GT", &H1, &H227E, Gb001, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.SR1, &H2801) With {.PAGE_SIZE = 512}) '(CV)
            FlashDB.Add(New P_NOR("Cypress S70GL02G", &H1, &H227E, Gb001, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.SR1, &H4801) With {.PAGE_SIZE = 512, .CE2 = 26, .DUAL_DIE = True}) '(CV)
            FlashDB.Add(New P_NOR("Cypress S29PL032J", &H1, &H227E, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.RYRB, &HA01))
            FlashDB.Add(New P_NOR("Cypress S29PL064J", &H1, &H227E, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.RYRB, &H201))
            FlashDB.Add(New P_NOR("Cypress S29PL127J", &H1, &H227E, Mb128, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.RYRB, &H2000)) '(CV)
            FlashDB.Add(New P_NOR("Cypress S29WS64J", &H1, &H227E, Mb064, VCC_IF.X16_1V8, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.RYRB, &H11E)) 'BGA80
            FlashDB.Add(New P_NOR("Cypress S29WS128J", &H1, &H227E, Mb128, VCC_IF.X16_1V8, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.RYRB, &H1800)) 'BGA84
            'ST Microelectronics (now numonyx)
            FlashDB.Add(New P_NOR("ST M29F200T", &H20, &HD3, Mb002, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS)) 'Available in TSOP48/SO44
            FlashDB.Add(New P_NOR("ST M29F200B", &H20, &HD4, Mb002, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS)) 'Available in TSOP48/SO44
            FlashDB.Add(New P_NOR("ST M29F400T", &H20, &HD5, Mb004, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS)) 'Available in TSOP48/SO44
            FlashDB.Add(New P_NOR("ST M29F400B", &H20, &HD6, Mb004, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS)) 'Available in TSOP48/SO44
            FlashDB.Add(New P_NOR("ST M29F080A", &H20, &HF1, Mb008, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("ST M29F800T", &H20, &HEC, Mb008, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS)) 'Available in TSOP48/SO44
            FlashDB.Add(New P_NOR("ST M29F800B", &H20, &H58, Mb008, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS)) 'Available in TSOP48/SO44
            'ST 3V NOR
            FlashDB.Add(New P_NOR("ST M29F032D", &H20, &HAC, Mb032, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS)) 'TSOP40
            FlashDB.Add(New P_NOR("ST M29W400DT", &H20, &H_00EE, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("ST M29W400DB", &H20, &H_00EF, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("ST M29W800AT", &H20, &H_00D7, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("ST M29W800AB", &H20, &H_005B, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("ST M29W800DT", &H20, &H_22D7, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("ST M29W800DB", &H20, &H_225B, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("ST M29W160ET", &H20, &H_22C4, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("ST M29W160EB", &H20, &H_2249, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS)) '(CV)
            FlashDB.Add(New P_NOR("ST M29D323DT", &H20, &H_225E, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("ST M29D323DB", &H20, &H_225F, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("ST M29W320ET", &H20, &H_2256, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("ST M29W320EB", &H20, &H_2257, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("ST M29W640GH", &H20, &H_227E, Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &HC01))
            FlashDB.Add(New P_NOR("ST M29W640GL", &H20, &H_227E, Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &HC00))
            FlashDB.Add(New P_NOR("ST M29W640GT", &H20, &H_227E, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H1001))
            FlashDB.Add(New P_NOR("ST M29W640GB", &H20, &H_227E, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H1000)) '(CV)
            FlashDB.Add(New P_NOR("ST M29W640FT", &H20, &H_22ED, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("ST M29W640FB", &H20, &H_22FD, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("ST M29W128GH", &H20, &H_227E, Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2101))
            FlashDB.Add(New P_NOR("ST M29W128GL", &H20, &H_227E, Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2100))
            'ST M28
            FlashDB.Add(New P_NOR("ST M28W160CT", &H20, &H88CE, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("ST M28W160CB", &H20, &H88CF, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("ST M28W320FCT", &H20, &H88BA, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("ST M28W320FCB", &H20, &H88BB, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("ST M28W320BT", &H20, &H88BC, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("ST M28W320BB", &H20, &H88BD, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("ST M28W640ECT", &H20, &H8848, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("ST M28W640ECB", &H20, &H8849, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("ST M28W320FSU", &H20, &H880C, Mb032, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1)) '(CV) (EasyBGA-64)
            FlashDB.Add(New P_NOR("ST M28W640FSU", &H20, &H8857, Mb064, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            FlashDB.Add(New P_NOR("ST M58LW064D", &H20, &H17, Mb064, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1))
            'Micron
            FlashDB.Add(New P_NOR("Micron M29F200FT", &HC2, &H2251, Mb002, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Micron M29F200FB", &HC2, &H2257, Mb002, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Micron M29F400FT", &HC2, &H2223, Mb004, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Micron M29F400FB", &HC2, &H22AB, Mb004, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Micron M29F800FT", &H1, &H22D6, Mb008, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Micron M29F800FB", &H1, &H2258, Mb008, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Micron M29F160FT", &H1, &H22D2, Mb016, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Micron M29F160FB", &H1, &H22D8, Mb016, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Micron M29W160ET", &H20, &H22C4, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Micron M29W160EB", &H20, &H2249, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Micron M29W320DT", &H20, &H22CA, Mb032, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Micron M29W320DB", &H20, &H22CB, Mb032, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Micron M29W640GH", &H20, &H227E, Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS, &HC01))
            FlashDB.Add(New P_NOR("Micron M29W640GL", &H20, &H227E, Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS, &HC00))
            FlashDB.Add(New P_NOR("Micron M29W640GT", &H20, &H227E, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS, &H1001))
            FlashDB.Add(New P_NOR("Micron M29W640GB", &H20, &H227E, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS, &H1000))
            FlashDB.Add(New P_NOR("Micron M29W128GH", &H20, &H227E, Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2201)) '(CV)
            FlashDB.Add(New P_NOR("Micron M29W128GL", &H20, &H227E, Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2200))
            FlashDB.Add(New P_NOR("Micron M29W256GH", &H20, &H227E, Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2201))
            FlashDB.Add(New P_NOR("Micron M29W256GL", &H20, &H227E, Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2200))
            FlashDB.Add(New P_NOR("Micron M29W512G", &H20, &H227E, Mb512, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2301))
            FlashDB.Add(New P_NOR("Micron MT28EW128", &H89, &H227E, Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2101) With {.PAGE_SIZE = 256}) 'May support up to 1024 bytes
            FlashDB.Add(New P_NOR("Micron MT28EW256", &H89, &H227E, Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2201) With {.PAGE_SIZE = 256})
            FlashDB.Add(New P_NOR("Micron MT28EW512", &H89, &H227E, Mb512, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2301) With {.PAGE_SIZE = 256})
            FlashDB.Add(New P_NOR("Micron MT28EW01G", &H89, &H227E, Gb001, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H2801) With {.PAGE_SIZE = 256})
            FlashDB.Add(New P_NOR("Micron MT28FW02G", &H89, &H227E, Gb002, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, &H4801) With {.PAGE_SIZE = 256}) 'Stacked die / BGA-64 (11x13mm)
            'Toshiba
            FlashDB.Add(New P_NOR("Toshiba TC58FVT800", &H98, &H4F, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Toshiba TC58FVB800", &H98, &HCE, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Toshiba TC58FVT160", &H98, &HC2, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Toshiba TC58FVB160", &H98, &H43, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Toshiba TC58FVT321", &H98, &H9C, Mb032, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Toshiba TC58FVB321", &H98, &H9A, Mb032, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Toshiba TC58FVM5T2A", &H98, &HC5, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FVM5B2A", &H98, &H55, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FVM5T3A", &H98, &HC6, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FVM5B3A", &H98, &H56, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FYM5T2A", &H98, &H59, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FYM5B2A", &H98, &H69, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FYM5T3A", &H98, &H5A, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FYM5B3A", &H98, &H6A, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FVM6T2A", &H98, &H57, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FVM6B2A", &H98, &H58, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FVM6T5B", &H98, &H2D, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FVM6B5B", &H98, &H2E, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FYM6T2A", &H98, &H7A, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FYM6B2A", &H98, &H7B, Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FVM7T2A", &H98, &H7C, Mb128, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FVM7B2A", &H98, &H82, Mb128, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FYM7T2A", &H98, &HD8, Mb128, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FYM7B2A", &H98, &HB2, Mb128, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FVM7T5B", &H98, &H1B, Mb128, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Toshiba TC58FVM7B5B", &H98, &H1D, Mb128, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            'Samsung
            FlashDB.Add(New P_NOR("Samsung K8P1615UQB", &HEC, &H257E, Mb016, VCC_IF.X16_3V, BLKLYT.Mb016_Samsung, MFP_PRG.BypassMode, MFP_DELAY.uS, &H1))
            FlashDB.Add(New P_NOR("Samsung K8D1716UT", &HEC, &H2275, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Samsung K8D1716UB", &HEC, &H2277, Mb016, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Samsung K8D3216UT", &HEC, &H22A0, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Samsung K8D3216UB", &HEC, &H22A2, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Samsung K8P3215UQB", &HEC, &H257E, Mb032, VCC_IF.X16_3V, BLKLYT.Mb032_Samsung, MFP_PRG.BypassMode, MFP_DELAY.uS, &H301))
            FlashDB.Add(New P_NOR("Samsung K8D6316UT", &HEC, &H22E0, Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Samsung K8D6316UB", &HEC, &H22E2, Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Samsung K8P6415UQB", &HEC, &H257E, Mb064, VCC_IF.X16_3V, BLKLYT.Mb064_Samsung, MFP_PRG.BypassMode, MFP_DELAY.uS, &H601)) '(CV)
            FlashDB.Add(New P_NOR("Samsung K8P2716UZC", &HEC, &H227E, Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS, &H6660))
            FlashDB.Add(New P_NOR("Samsung K8Q2815UQB", &HEC, &H257E, Mb128, VCC_IF.X16_3V, BLKLYT.Mb128_Samsung, MFP_PRG.BypassMode, MFP_DELAY.uS, &H601)) 'TSOP56 Type-A
            FlashDB.Add(New P_NOR("Samsung K8P5516UZB", &HEC, &H227E, Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS, &H6460))
            FlashDB.Add(New P_NOR("Samsung K8P5615UQA", &HEC, &H227E, Mb256, VCC_IF.X16_3V, BLKLYT.Mb256_Samsung, MFP_PRG.BypassMode, MFP_DELAY.uS, &H6360))
            'Hynix
            FlashDB.Add(New P_NOR("Hynix HY29F040", &HAD, &HA4, Mb004, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("Hynix HY29F080", &HAD, &HD5, Mb008, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7)) 'TSOP40-A
            FlashDB.Add(New P_NOR("Hynix HY29F400T", &HAD, &H2223, Mb004, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Hynix HY29F400B", &HAD, &H22AB, Mb004, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Hynix HY29F800T", &HAD, &H22D6, Mb008, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Hynix HY29F800B", &HAD, &H2258, Mb008, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Hynix HY29LV400T", &HAD, &H22B9, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Hynix HY29LV400B", &HAD, &H22BA, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Hynix HY29LV800T", &HAD, &H22DA, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Hynix HY29LV800B", &HAD, &H225B, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Hynix HY29LV160T", &HAD, &H22C4, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Hynix HY29LV160B", &HAD, &H2249, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Hynix HY29LV320T", &HAD, &H227E, Mb032, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Hynix HY29LV320B", &HAD, &H227D, Mb032, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            'Fujitsu
            FlashDB.Add(New P_NOR("Fujitsu MBM29F040C", &H4, &HA4, Mb004, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Fujitsu MBM29F400TA", &H4, &H2223, Mb004, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Fujitsu MBM29F400BA", &H4, &H22AB, Mb004, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Fujitsu MBM29F800TA", &H4, &H22D6, Mb008, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Fujitsu MBM29F800BA", &H4, &H2258, Mb008, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Fujitsu MBM29LV200TC", &H4, &H223B, Mb002, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Fujitsu MBM29LV200BC", &H4, &H22BF, Mb002, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Fujitsu MBM29LV400TC", &H4, &H22B9, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Fujitsu MBM29LV400BC", &H4, &H22BA, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Fujitsu MBM29LV800TA", &H4, &H22DA, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Fujitsu MBM29LV800BA", &H4, &H225B, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Fujitsu MBM29LV160T", &H4, &H22C4, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Fujitsu MBM29LV160B", &H4, &H2249, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Fujitsu MBM29F033C", &H4, &HD4, Mb032, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS)) 'TSOP40 CV (AMD branded)
            FlashDB.Add(New P_NOR("Fujitsu MBM29LV320TE", &H4, &H22F6, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Standard, MFP_DELAY.uS)) 'Supports FAST programming (ADR=0xA0,PA=PD)
            FlashDB.Add(New P_NOR("Fujitsu MBM29LV320BE", &H4, &H22F9, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Standard, MFP_DELAY.uS)) 'Supports FAST programming (ADR=0xA0,PA=PD)
            FlashDB.Add(New P_NOR("Fujitsu MBM29DL32XTD", &H4, &H2259, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Standard, MFP_DELAY.uS)) 'Supports FAST programming (ADR=0xA0,PA=PD)
            FlashDB.Add(New P_NOR("Fujitsu MBM29DL32XBD", &H4, &H225A, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Standard, MFP_DELAY.uS)) 'Supports FAST programming (ADR=0xA0,PA=PD)
            FlashDB.Add(New P_NOR("Fujitsu MBM29LV650UE", &H4, &H22D7, Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS, &H2201))
            FlashDB.Add(New P_NOR("Fujitsu MBM29LV651UE", &H4, &H22D7, Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS, &H2200))
            'EON (MFG is 7F 1C)
            FlashDB.Add(New P_NOR("EON EN29LV400AT", &H7F, &H22B9, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("EON EN29LV400AB", &H7F, &H22BA, Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("EON EN29LV800AT", &H7F, &H22DA, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("EON EN29LV800AB", &H7F, &H225B, Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7)) '(CV)
            FlashDB.Add(New P_NOR("EON EN29LV160AT", &H7F, &H22C4, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("EON EN29LV160AB", &H7F, &H2249, Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("EON EN29LV320AT", &H7F, &H22F6, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("EON EN29LV320AB", &H7F, &H22F9, Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
            FlashDB.Add(New P_NOR("EON EN29LV640", &H7F, &H227E, Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.DQ7))
        End Sub

        Private Sub SPI_EEPROM_Database()
            'Serial devices:
            FlashDB.Add(New SPI_NOR("Nordic nRF24LE1", 16384, 512US, 16UI, SPI_PROG.Nordic))
            FlashDB.Add(New SPI_NOR("Nordic nRF24LU1+ (16KB)", 16384, 256US, 16UI, SPI_PROG.Nordic))
            FlashDB.Add(New SPI_NOR("Nordic nRF24LU1+ (32KB)", 32768, 256US, 16UI, SPI_PROG.Nordic))
            FlashDB.Add(New SPI_NOR("Altera EPCS1", CInt(Mb001), 256US, 24UI, SPI_PROG.SPI_EEPROM)) 'RES: 0x10
            FlashDB.Add(New SPI_NOR("Altera EPCS4", CInt(Mb004), 256US, 24UI, SPI_PROG.SPI_EEPROM)) 'RES: 0x12
            FlashDB.Add(New SPI_NOR("Altera EPCS16", CInt(Mb016), 256US, 24UI, SPI_PROG.SPI_EEPROM)) 'RES: 0x14
            FlashDB.Add(New SPI_NOR("Altera EPCS64", CInt(Mb064), 256US, 24UI, SPI_PROG.SPI_EEPROM)) 'RES: 0x16
            FlashDB.Add(New SPI_NOR("Altera EPCS128", CInt(Mb128), 256US, 24UI, SPI_PROG.SPI_EEPROM)) 'RES: 0x18, RDID: 0x18
            FlashDB.Add(New SPI_NOR("Altera EPCQ4A", CInt(Mb001), 256US, 24UI, SPI_PROG.SPI_EEPROM)) 'RDID: 0x13
            FlashDB.Add(New SPI_NOR("Altera EPCQ16A", CInt(Mb016), 256US, 24UI, SPI_PROG.SPI_EEPROM)) 'RDID: 0x15
            FlashDB.Add(New SPI_NOR("Altera EPCQ32A", CInt(Mb032), 256US, 24UI, SPI_PROG.SPI_EEPROM)) 'RDID: 0x16
            FlashDB.Add(New SPI_NOR("Altera EPCQ64A", CInt(Mb064), 256US, 24UI, SPI_PROG.SPI_EEPROM)) 'RDID: 0x17
            FlashDB.Add(New SPI_NOR("Altera EPCQ128A", CInt(Mb128), 256US, 24UI, SPI_PROG.SPI_EEPROM)) 'RDID: 0x18
            FlashDB.Add(New SPI_NOR("Atmel AT25010A", 128, 8US, 8UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Atmel AT25020A", 256, 8US, 8UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Atmel AT25040A", 512, 8US, 8UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Atmel AT25080", 1024, 32US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Atmel AT25160", 2048, 32US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Atmel AT25320", 4096, 32US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Atmel AT25640", 8192, 32US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Atmel AT25128", 16384, 64US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Atmel AT25256", 32768, 64US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Atmel AT25512", 65536, 128US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Atmel AT25M01", CInt(Mb001), 256US, 24UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Atmel AT25M02", CInt(Mb002), 256US, 24UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("ST 95P08", 1024, 16US, 8UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("ST M95010", 128, 16US, 8UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("ST M95020", 256, 16US, 8UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("ST M95040", 512, 16US, 8UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("ST M95080", 1024, 32US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("ST M95160", 2048, 32US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("ST M95320", 4096, 32US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("ST M95640", 8192, 32US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("ST M95128", 16384, 64US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("ST M95256", 32768, 64US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("ST M95512", 65536, 128US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("ST M95M01", CInt(Mb001), 256US, 24UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("ST M95M02", CInt(Mb002), 256US, 24UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Microchip 25AA160A", 2048, 16US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Microchip 25AA160B", 2048, 32US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Microchip 25XX320", 4096, 32US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Microchip 25XX640", 8192, 32US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Microchip 25XX128", 16384, 64US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Microchip 25XX256", 32768, 64US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Microchip 25XX512", 65536, 128US, 16UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Microchip 25XX1024", CInt(Mb001), 256US, 24UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("Renesas X5083", 1048, 16US, 8UI, SPI_PROG.SPI_EEPROM))
            FlashDB.Add(New SPI_NOR("XICOR X25650", 8192, 32US, 16UI, SPI_PROG.SPI_EEPROM))
            'Parallel Devices:
            FlashDB.Add(New P_NOR("Atmel AT28C16", 0, 0, Kb016, VCC_IF.X8_5V, BLKLYT.Kb016_Uni, MFP_PRG.EEPROM, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Atmel AT28C32", 0, 0, Kb032, VCC_IF.X8_5V, BLKLYT.Kb016_Uni, MFP_PRG.EEPROM, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Atmel AT28C64", 0, 0, Kb064, VCC_IF.X8_5V, BLKLYT.Kb016_Uni, MFP_PRG.EEPROM, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Atmel AT28C128", 0, 0, Kb128, VCC_IF.X8_5V, BLKLYT.Kb016_Uni, MFP_PRG.EEPROM, MFP_DELAY.uS))
            FlashDB.Add(New P_NOR("Atmel AT28C256", 0, 0, Kb256, VCC_IF.X8_5V, BLKLYT.Kb016_Uni, MFP_PRG.EEPROM, MFP_DELAY.uS))

        End Sub

        Private Sub NAND_Database()
            'Intel MLC NAND
            FlashDB.Add(New P_NAND("Intel JS29F64G08AAME1", &H89, &H_88244BA9UI, 8192, 448, 256, 2048, VCC_IF.X8_3V)) '64Gb
            FlashDB.Add(New P_NAND("Intel PF29F64G08LCMFS", &H89, &H_64643CA1UI, 16384, 1216, 512, 1024, VCC_IF.X8_3V)) '64Gb
            FlashDB.Add(New P_NAND("Intel PF29F16B08MCMFS", &H89, &H_64643CA1UI, 512, 16, 32, 1024, VCC_IF.X8_3V) With {.DIE_COUNT = 2}) '128Gb (dual x8)
            FlashDB.Add(New P_NAND("Intel PF29F32B08NCMFS", &H89, &H_64643CA1UI, 512, 16, 32, 1024, VCC_IF.X8_3V) With {.DIE_COUNT = 4}) '256Gb (dual x8)
            '00 = Does not matter
            'Micron SLC 8x NAND devices
            FlashDB.Add(New P_NAND("ST NAND128W3A", &H20, &H_00732073UI, 512, 16, 32, 1024, VCC_IF.X8_3V)) '128Mb
            FlashDB.Add(New P_NAND("ST NAND256R3A", &H20, &H_00352035UI, 512, 16, 32, 2048, VCC_IF.X8_1V8)) '256Mb
            FlashDB.Add(New P_NAND("ST NAND256W3A", &H20, &H_00752075UI, 512, 16, 32, 2048, VCC_IF.X8_3V)) '256Mb
            FlashDB.Add(New P_NAND("ST NAND256R4A", &H20, &H_00452045UI, 512, 16, 32, 2048, VCC_IF.X16_1V8)) '256Mb
            FlashDB.Add(New P_NAND("ST NAND256W4A", &H20, &H_00552055UI, 512, 16, 32, 2048, VCC_IF.X16_3V)) '256Mb
            FlashDB.Add(New P_NAND("ST NAND512R3A", &H20, &H_00362036UI, 512, 16, 32, 4096, VCC_IF.X8_1V8)) '512Mb
            FlashDB.Add(New P_NAND("ST NAND512W3A", &H20, &H_00762076UI, 512, 16, 32, 4096, VCC_IF.X8_3V)) '512Mb
            FlashDB.Add(New P_NAND("ST NAND512R4A", &H20, &H_00462046UI, 512, 16, 32, 4096, VCC_IF.X16_1V8)) '512Mb
            FlashDB.Add(New P_NAND("ST NAND512W4A", &H20, &H_00562056UI, 512, 16, 32, 4096, VCC_IF.X16_3V)) '512Mb
            FlashDB.Add(New P_NAND("ST NAND01GR3A", &H20, &H_00392039UI, 512, 16, 32, 8192, VCC_IF.X8_1V8)) '1Gb
            FlashDB.Add(New P_NAND("ST NAND01GW3A", &H20, &H_00792079UI, 512, 16, 32, 8192, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("ST NAND01GR4A", &H20, &H_00492049UI, 512, 16, 32, 8192, VCC_IF.X16_1V8)) '1Gb
            FlashDB.Add(New P_NAND("ST NAND01GW4A", &H20, &H_00592059UI, 512, 16, 32, 8192, VCC_IF.X16_3V)) '1Gb
            'Type-2 AddressWrite
            FlashDB.Add(New P_NAND("ST NAND01GR3B", &H20, &H_000000A1UI, 2048, 64, 64, 1024, VCC_IF.X8_1V8)) '1Gb
            FlashDB.Add(New P_NAND("ST NAND01GW3B", &H20, &H_00F1001DUI, 2048, 64, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("ST NAND01GR4B", &H20, &H_000000B1UI, 2048, 64, 64, 1024, VCC_IF.X16_1V8)) '1Gb
            FlashDB.Add(New P_NAND("ST NAND01GW4B", &H20, &H_000000C1UI, 2048, 64, 64, 1024, VCC_IF.X16_3V)) '1Gb
            FlashDB.Add(New P_NAND("ST NAND02GR3B", &H20, &H_000000AAUI, 2048, 64, 64, 2048, VCC_IF.X8_1V8)) '2Gb
            FlashDB.Add(New P_NAND("ST NAND02GW3B", &H20, &H_00DA8015UI, 2048, 64, 64, 2048, VCC_IF.X8_3V)) '2Gb
            FlashDB.Add(New P_NAND("ST NAND02GR4B", &H20, &H_000000BAUI, 2048, 64, 64, 2048, VCC_IF.X16_1V8)) '2Gb
            FlashDB.Add(New P_NAND("ST NAND02GW4B", &H20, &H_000000CAUI, 2048, 64, 64, 2048, VCC_IF.X16_3V)) '2Gb
            FlashDB.Add(New P_NAND("ST NAND04GW3B", &H20, &H_00DC8095UI, 2048, 64, 64, 4096, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("ST NAND04GW3B", &H20, &H_00DC1095UI, 2048, 64, 64, 4096, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("ST NAND08GW3B", &H20, &H_00D3C195UI, 2048, 64, 64, 8192, VCC_IF.X8_3V)) '8Gb
            'Micron devices
            FlashDB.Add(New P_NAND("Micron MT29F1G08ABAEA    ", &H2C, &H_F1809504UI, 2048, 64, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("Micron MT29F1G08ABBEA    ", &H2C, &H_A1801504UI, 2048, 64, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("Micron MT29F1G08ABADAWP  ", &H2C, &H_F1809502UI, 2048, 64, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("Micron MT29F2G08ABAEA    ", &H2C, &H_DA909506UI, 2048, 64, 64, 2048, VCC_IF.X8_3V)) '2Gb
            FlashDB.Add(New P_NAND("Micron MT29F2G08ABAFA    ", &H2C, &H_DA909504UI, 2048, 224, 64, 2048, VCC_IF.X8_3V)) '2Gb
            FlashDB.Add(New P_NAND("Micron MT29F2G08ABAGA    ", &H2C, &H_DA909586UI, 2048, 128, 64, 2048, VCC_IF.X8_3V)) '2Gb
            FlashDB.Add(New P_NAND("Micron MT29F2G08AAB      ", &H2C, &H_0000DA0015, 2048, 64, 64, 2048, VCC_IF.X8_3V)) '2Gb
            FlashDB.Add(New P_NAND("Micron MT29F2G08ABBFA    ", &H2C, &H_AA901504UI, 2048, 224, 64, 2048, VCC_IF.X8_3V)) '2Gb
            FlashDB.Add(New P_NAND("Micron MT29F2G08ABBEA    ", &H2C, &H_AA901506UI, 2048, 64, 64, 2048, VCC_IF.X8_1V8)) '2Gb
            FlashDB.Add(New P_NAND("Micron MT29F2G16ABBEA    ", &H2C, &H_BA905506UI, 2048, 64, 64, 2048, VCC_IF.X16_1V8)) '2Gb
            FlashDB.Add(New P_NAND("Micron MT29F2G08ABBGA    ", &H2C, &H_AA901506UI, 2048, 128, 64, 2048, VCC_IF.X8_1V8)) '2Gb
            FlashDB.Add(New P_NAND("Micron MT29F2G16ABAEA    ", &H2C, &H_CA90D506UI, 2048, 64, 64, 2048, VCC_IF.X16_3V)) '2Gb
            FlashDB.Add(New P_NAND("Micron MT29F2G08ABBEAH4  ", &H2C, &H_00AA9015UI, 2048, 64, 64, 2048, VCC_IF.X8_1V8)) '2Gb
            FlashDB.Add(New P_NAND("Micron MT29F2G16ABBEAH4  ", &H2C, &H_00BA9055UI, 2048, 64, 64, 2048, VCC_IF.X16_1V8)) '2Gb
            FlashDB.Add(New P_NAND("Micron MT29F2G16ABAEAH4  ", &H2C, &H_00CA90D5UI, 2048, 64, 64, 2048, VCC_IF.X16_3V)) '2Gb
            FlashDB.Add(New P_NAND("Micron MT29F4G08BAB      ", &H2C, &H_0000DC0015, 2048, 64, 64, 4096, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("Micron MT29F4G08AAA      ", &H2C, &H_DC909554UI, 2048, 64, 64, 4096, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("Micron MT29F4G08BABWP    ", &H2C, &H_DC801550UI, 2048, 64, 64, 4096, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("Micron MT29F4G08ABADA    ", &H2C, &H_DC909556UI, 2048, 64, 64, 4096, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("Micron MT29F4G08ABAEA    ", &H2C, &H_DC90A654UI, 4096, 224, 64, 2048, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("Micron MT29F4G08ABAEA    ", &H2C, &H_DC90A654UI, 4096, 224, 64, 2048, VCC_IF.X8_3V)) '4Gb (1 page concurrent programming)
            FlashDB.Add(New P_NAND("Micron MT29F4G08ABAEA    ", &H2C, &H_DC80A662UI, 4096, 224, 64, 2048, VCC_IF.X8_3V)) '4Gb (2 page concurrent programming)
            FlashDB.Add(New P_NAND("Micron MT29F4G16ABADA    ", &H2C, &H_CC90D556UI, 2048, 64, 64, 4096, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("Micron MT29F4G08ABBDA    ", &H2C, &H_AC901556UI, 2048, 64, 64, 4096, VCC_IF.X8_1V8)) '4Gb
            FlashDB.Add(New P_NAND("Micron MT29F8G08DAA      ", &H2C, &H_D3909554UI, 2048, 64, 64, 4096, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("Micron MT29F8G08BAA      ", &H2C, &H_D3D19558UI, 2048, 64, 64, 8192, VCC_IF.X8_3V)) '8Gb
            FlashDB.Add(New P_NAND("Micron MT29F8G08ABACA    ", &H2C, &H_D390A664UI, 4096, 224, 64, 4096, VCC_IF.X8_3V)) '8Gb
            FlashDB.Add(New P_NAND("Micron MT29F8G08ABABA    ", &H2C, &H_38002685UI, 4096, 224, 128, 2048, VCC_IF.X8_3V)) '8Gb
            FlashDB.Add(New P_NAND("Micron MT29F16G08CBACA   ", &H2C, &H_48044AA5UI, 4096, 224, 256, 2048, VCC_IF.X8_3V)) '16Gb
            FlashDB.Add(New P_NAND("Micron MT29F16G08ABABA   ", &H2C, &H_48002689UI, 4096, 224, 128, 4096, VCC_IF.X8_3V)) '16Gb
            FlashDB.Add(New P_NAND("Micron MT29F16G08ABCBB   ", &H2C, &H_48002689UI, 4096, 224, 128, 4096, VCC_IF.X8_3V)) '16Gb
            FlashDB.Add(New P_NAND("Micron MT29F32G08AFABA   ", &H2C, &H_48002689UI, 4096, 224, 128, 8192, VCC_IF.X8_3V)) '32Gb
            FlashDB.Add(New P_NAND("Micron MT29F32G08AECBB   ", &H2C, &H_48002689UI, 4096, 224, 128, 8192, VCC_IF.X8_3V)) '32Gb
            FlashDB.Add(New P_NAND("Micron MT29F64G08AJABA   ", &H2C, &H_6801A689UI, 4096, 224, 128, 16384, VCC_IF.X8_3V)) '64Gb (2 LUN)
            FlashDB.Add(New P_NAND("Micron MT29F64G08AKABA   ", &H2C, &H_6801A689UI, 4096, 224, 128, 16384, VCC_IF.X8_3V)) '64Gb (2 LUN)
            FlashDB.Add(New P_NAND("Micron MT29F64G08AKCBB   ", &H2C, &H_6801A689UI, 4096, 224, 128, 16384, VCC_IF.X8_3V)) '64Gb (2 LUN)
            FlashDB.Add(New P_NAND("Micron MT29F64G08AMABA   ", &H2C, &H_48002689UI, 4096, 224, 128, 16384, VCC_IF.X8_3V)) '64Gb
            FlashDB.Add(New P_NAND("Micron MT29F64G08AMCBB   ", &H2C, &H_48002689UI, 4096, 224, 128, 16384, VCC_IF.X8_3V)) '64Gb
            FlashDB.Add(New P_NAND("Micron MT29F128G08AUABA   ", &H2C, &H_6801A689UI, 4096, 224, 128, 32768, VCC_IF.X8_3V)) '128Gb (2 LUN)
            FlashDB.Add(New P_NAND("Micron MT29F128G08AUCBB   ", &H2C, &H_6801A689UI, 4096, 224, 128, 32768, VCC_IF.X8_3V)) '128Gb (2 LUN)
            FlashDB.Add(New P_NAND("Micron MT29F32G08AFACA   ", &H2C, &H_480026A9UI, 4096, 224, 256, 4096, VCC_IF.X8_3V)) '32Gb
            FlashDB.Add(New P_NAND("Micron MT29F32G08CBAAA   ", &H2C, &H_D7943E84UI, 4096, 218, 128, 8192, VCC_IF.X8_3V)) '32Gb (2 planes)
            FlashDB.Add(New P_NAND("Micron MT29F32G08CBACAWP ", &H2C, &H_64444BA9UI, 4096, 224, 256, 4096, VCC_IF.X8_3V)) '32Gb
            FlashDB.Add(New P_NAND("Micron MT29F32G08CBACAWP ", &H2C, &H_68044AA9UI, 4096, 224, 256, 4096, VCC_IF.X8_3V)) '32Gb
            FlashDB.Add(New P_NAND("Micron MT29F64G08CFACAWP ", &H2C, &H_68044AA9UI, 4096, 224, 256, 8192, VCC_IF.X8_3V)) '64Gb
            FlashDB.Add(New P_NAND("Micron MT29F64G08CEACAD1 ", &H2C, &H_68044AA9UI, 4096, 224, 256, 8192, VCC_IF.X8_3V)) '64Gb
            FlashDB.Add(New P_NAND("Micron MT29F128G08CXACAD1", &H2C, &H_68044AA9UI, 4096, 224, 256, 8192, VCC_IF.X8_3V)) '64Gb
            FlashDB.Add(New P_NAND("Micron MT29F64G08CECCBH1 ", &H2C, &H_68044AA9UI, 4096, 224, 256, 8192, VCC_IF.X8_3V)) '64Gb
            FlashDB.Add(New P_NAND("Micron MT29F64G08CBABA   ", &H2C, &H_64444BA9UI, 8192, 744, 256, 4096, VCC_IF.X8_3V)) '64Gb
            FlashDB.Add(New P_NAND("Micron MT29F64G08CBAAA   ", &H2C, &H_88044BA9UI, 8192, 448, 256, 4096, VCC_IF.X8_3V)) '64Gb MLC
            FlashDB.Add(New P_NAND("Micron MT29F64G08CBABB   ", &H2C, &H_64444BA9UI, 8192, 744, 256, 4096, VCC_IF.X8_3V)) '64Gb
            FlashDB.Add(New P_NAND("Micron MT29F64G08CBCBB   ", &H2C, &H_64444BA9UI, 8192, 744, 256, 4096, VCC_IF.X8_3V)) '64Gb
            FlashDB.Add(New P_NAND("Micron MT29F64G08CEACA   ", &H2C, &H_64444BA9UI, 4096, 224, 256, 8192, VCC_IF.X8_3V)) '64Gb
            FlashDB.Add(New P_NAND("Micron MT29F64G08CECCB   ", &H2C, &H_64444BA9UI, 4096, 224, 256, 8192, VCC_IF.X8_3V)) '64Gb
            FlashDB.Add(New P_NAND("Micron MT29F64G08CFACA   ", &H2C, &H_64444BA9UI, 4096, 224, 256, 8192, VCC_IF.X8_3V)) '64Gb
            FlashDB.Add(New P_NAND("Micron MT29F64G08CFACBWP ", &H2C, &H_8805CAA9UI, 4096, 224, 256, 16384, VCC_IF.X8_3V)) '128Gb
            FlashDB.Add(New P_NAND("Micron MT29F128G08CKCCBH2", &H2C, &H_68044AA9UI, 4096, 224, 256, 16384, VCC_IF.X8_3V)) '128Gb
            FlashDB.Add(New P_NAND("Micron MT29F256G08CUCCBH3", &H2C, &H_8805CAA9UI, 4096, 224, 256, 16384, VCC_IF.X8_3V)) '128Gb
            FlashDB.Add(New P_NAND("Micron MT29F128G08CECBB  ", &H2C, &H_64444BA9UI, 8192, 744, 256, 8192, VCC_IF.X8_3V)) '128Gb
            FlashDB.Add(New P_NAND("Micron MT29F128G08CFABA  ", &H2C, &H_64444BA9UI, 8192, 744, 256, 8192, VCC_IF.X8_3V)) '128Gb
            FlashDB.Add(New P_NAND("Micron MT29F128G08CFABB  ", &H2C, &H_64444BA9UI, 8192, 744, 256, 8192, VCC_IF.X8_3V)) '128Gb
            FlashDB.Add(New P_NAND("Micron MT29F128G08CXACA  ", &H2C, &H_64444BA9UI, 4096, 224, 256, 16384, VCC_IF.X8_3V)) '128Gb
            FlashDB.Add(New P_NAND("Micron MT29F256G08CJABA  ", &H2C, &H_84C54BA9UI, 8192, 744, 256, 16384, VCC_IF.X8_3V)) '256Gb
            FlashDB.Add(New P_NAND("Micron MT29F256G08CJABB  ", &H2C, &H_84C54BA9UI, 8192, 744, 256, 16384, VCC_IF.X8_3V)) '256Gb
            FlashDB.Add(New P_NAND("Micron MT29F256G08CKCBB  ", &H2C, &H_84C54BA9UI, 8192, 744, 256, 16384, VCC_IF.X8_3V)) '256Gb
            FlashDB.Add(New P_NAND("Micron MT29F256G08CMCBB  ", &H2C, &H_64444BA9UI, 8192, 744, 256, 16384, VCC_IF.X8_3V)) '256Gb
            'Toshiba SLC 8x NAND devices
            FlashDB.Add(New P_NAND("Toshiba TC58DVM92A5TAI0", &H98, &H_76A5C029UI, 512, 16, 32, 4096, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Toshiba TC58DVM92A5TAI0", &H98, &H_76A5C021UI, 512, 16, 32, 4096, VCC_IF.X8_3V)) '<---- different revision
            FlashDB.Add(New P_NAND("Toshiba TC58NVG3D4CTGI0", &H98, &H_D384A5E6UI, 2048, 128, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("Toshiba TC58BVG0S3HTA00", &H98, &H_F08014F2UI, 2048, 64, 64, 1024, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Toshiba TC58DVG02D5TA00", &H98, &H_F1001572UI, 2048, 64, 64, 1024, VCC_IF.X8_3V)) 'TC58NVG0S3HTA00 and TC58NVG0S3HTAI0 share same ID
            FlashDB.Add(New P_NAND("Toshiba TC58NVG0S3HTA00", &H98, &H_F1801572UI, 2048, 128, 64, 1024, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Toshiba TC58NVG0S3HTAI0", &H98, &H_F1801572UI, 2048, 128, 64, 1024, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Toshiba TC58NVG0S3ETA00", &H98, &H_D1901576UI, 2048, 64, 64, 1024, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Toshiba TC58BVG0S3HTAI0", &H98, &H_F18015F2UI, 2048, 64, 64, 1024, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Toshiba TC58NVG1S3HTA00", &H98, &H_DA901576UI, 2048, 128, 64, 2048, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Toshiba TC58BVG1S3HTA00", &H98, &H_DA9015F6UI, 2048, 64, 64, 2048, VCC_IF.X8_3V)) '2Gb
            FlashDB.Add(New P_NAND("Toshiba TC58NVG1S3HTAI0", &H98, &H_DA901576UI, 2048, 128, 64, 2048, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Toshiba TC58NVG2S0HTA00", &H98, &H_DC902676UI, 4096, 256, 64, 2048, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Toshiba TC58NVG2S3ETA00", &H98, &H_DC901576UI, 2048, 64, 64, 4096, VCC_IF.X8_3V)) 'CV
            FlashDB.Add(New P_NAND("Toshiba TC58NVG2S0HTAI0", &H98, &H_DC902676UI, 4096, 256, 64, 2048, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Toshiba TH58NVG2S3HTA00", &H98, &H_DC911576UI, 2048, 128, 64, 4096, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Toshiba TC58BVG2S0HTAI0", &H98, &H_DC9026F6UI, 4096, 128, 64, 2048, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Toshiba TH58NVG3S0HTA00", &H98, &H_D3912676UI, 4096, 256, 64, 4096, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Toshiba TH58NVG3S0HTAI0", &H98, &H_D3912676UI, 4096, 256, 64, 4096, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Toshiba TC58NVG3S0FTA00", &H98, &H_D3902676UI, 4096, 232, 64, 4096, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Toshiba TC58NVG3S0FTA00", &H98, &H_00902676UI, 4096, 232, 64, 4096, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Toshiba TC58NVG6D2HTA00", &H98, &H_DE948276UI, 8192, 640, 256, 4096, VCC_IF.X8_3V)) '64Gb
            FlashDB.Add(New P_NAND("Toshiba TC58NVG2S0HBAI4", &H98, &H_D8902070UI, 4096, 256, 64, 2048, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("Toshiba TC58NVG4D2ETA00", &H98, &H_00D59432UI, 8192, 376, 128, 2048, VCC_IF.X8_3V)) '16Gbit
            'Winbond SLC 8x NAND devices
            FlashDB.Add(New P_NAND("Winbond W29N01GW", &HEF, &H_B1805500UI, 2048, 64, 64, 1024, VCC_IF.X8_1V8)) '1Gb
            FlashDB.Add(New P_NAND("Winbond W29N01GV", &HEF, &H_F1809500UI, 2048, 64, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("Winbond W29N01HV", &HEF, &H_F1009500UI, 2048, 64, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("Winbond W29N02GV", &HEF, &H_DA909504UI, 2048, 64, 64, 2048, VCC_IF.X8_3V)) '2Gb
            FlashDB.Add(New P_NAND("Winbond W29N04GV", &HEF, &H_DC909554UI, 2048, 64, 64, 4096, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("Winbond W29N08GV", &HEF, &H_D3919558UI, 2048, 64, 64, 8192, VCC_IF.X8_3V)) '8Gb
            FlashDB.Add(New P_NAND("Winbond W29N08GV", &HEF, &H_DC909554UI, 2048, 64, 64, 8192, VCC_IF.X8_3V)) '8Gb
            FlashDB.Add(New P_NAND("Winbond W29N04GZ", &HEF, &H_AC901554UI, 2048, 64, 64, 4096, VCC_IF.X8_1V8)) '4Gb
            FlashDB.Add(New P_NAND("Winbond W29N04GW", &HEF, &H_BC905554UI, 2048, 64, 64, 4096, VCC_IF.X16_1V8)) '4Gb
            'Macronix SLC 8x NAND devices
            FlashDB.Add(New P_NAND("MXIC MX30LF1208AA", &HC2, &H_00F0801DUI, 2048, 64, 64, 512, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("MXIC MX30LF1GE8AB", &HC2, &H_F1809582UI, 2048, 64, 64, 1024, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("MXIC MX30UF1G18AC", &HC2, &H_A1801502UI, 2048, 64, 64, 1024, VCC_IF.X8_1V8))
            FlashDB.Add(New P_NAND("MXIC MX30LF1G18AC", &HC2, &H_F1809502UI, 2048, 64, 64, 1024, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("MXIC MX30LF1G08AA", &HC2, &H_0000F1801D, 2048, 64, 64, 1024, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("MXIC MX30LF1G28AD", &HC2, &H_F1809103UI, 2048, 128, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("MXIC MX30LF2G28AD", &HC2, &H_DA909107UI, 2048, 128, 64, 2048, VCC_IF.X8_3V)) '2Gb
            FlashDB.Add(New P_NAND("MXIC MX30LF4G28AD", &HC2, &H_DC90A257UI, 4096, 256, 64, 2048, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("MXIC MX30LF2G18AC", &HC2, &H_DA909506UI, 2048, 64, 64, 2048, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("MXIC MX30UF2G18AC", &HC2, &H_AA901506UI, 2048, 64, 64, 2048, VCC_IF.X8_1V8))
            FlashDB.Add(New P_NAND("MXIC MX30LF2G28AB", &HC2, &H_DA909507UI, 2048, 64, 64, 2048, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("MXIC MX30LF2GE8AB", &HC2, &H_DA909586UI, 2048, 64, 64, 2048, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("MXIC MX30UF2G18AB", &HC2, &H_BA905506UI, 2048, 64, 64, 2048, VCC_IF.X8_1V8))
            FlashDB.Add(New P_NAND("MXIC MX30UF2G28AB", &HC2, &H_AA901507UI, 2048, 112, 64, 1024, VCC_IF.X8_1V8))
            FlashDB.Add(New P_NAND("MXIC MX30LF4G18AC", &HC2, &H_DC909556UI, 2048, 64, 64, 4096, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("MXIC MX30UF4G18AB", &HC2, &H_AC901556UI, 2048, 64, 64, 4096, VCC_IF.X8_1V8))
            FlashDB.Add(New P_NAND("MXIC MX30LF2G28AB", &HC2, &H_DC909507UI, 2048, 112, 64, 2048, VCC_IF.X8_3V)) '2-plane (8-bit ECC)
            FlashDB.Add(New P_NAND("MXIC MX30LF4G28AB", &HC2, &H_DC909557UI, 2048, 112, 64, 4096, VCC_IF.X8_3V)) '2-plane (8-bit ECC)
            FlashDB.Add(New P_NAND("MXIC MX30LF4GE8AB", &HC2, &H_DC9095D6UI, 2048, 64, 64, 4096, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("MXIC MX30UF4G28AB", &HC2, &H_AC901557UI, 2048, 112, 64, 2048, VCC_IF.X8_1V8))
            FlashDB.Add(New P_NAND("MXIC MX60LF8G18AC", &HC2, &H_D3D1955AUI, 2048, 64, 64, 8192, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("MXIC MX60LF8G28AB", &HC2, &H_D3D1955BUI, 2048, 64, 64, 8192, VCC_IF.X8_3V))
            'Samsung SLC x8 NAND devices
            FlashDB.Add(New P_NAND("Samsung K9F2808U0C", &HEC, &H_0073A533UI, 512, 16, 32, 1024, VCC_IF.X8_3V)) '128Mb
            FlashDB.Add(New P_NAND("Samsung K9K2G08U0M", &HEC, &H_DAC11544UI, 2048, 64, 64, 2048, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Samsung K9K1G08U0M", &HEC, &H_79A5C0ECUI, 512, 16, 32, 8192, VCC_IF.X8_3V)) 'Gb001
            FlashDB.Add(New P_NAND("Samsung K9F1208U0C", &HEC, &H_765A3F74UI, 512, 16, 32, 4096, VCC_IF.X8_3V)) 'Mb512
            FlashDB.Add(New P_NAND("Samsung K9F5608U0D", &HEC, &H_75A5BDECUI, 512, 16, 32, 2048, VCC_IF.X8_3V)) 'Mb256
            FlashDB.Add(New P_NAND("Samsung K9F1G08U0A", &HEC, &H_F1801540UI, 2048, 64, 64, 1024, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Samsung K9F1G08U0B", &HEC, &H_F1009540UI, 2048, 64, 64, 1024, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Samsung K9F1G08U0C", &HEC, &H_F1009540UI, 2048, 64, 64, 1024, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Samsung K9F1G08U0D", &HEC, &H_F1001540UI, 2048, 64, 64, 1024, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Samsung K9F1G08U0C", &HEC, &H_F1009540UI, 2048, 64, 64, 1024, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Samsung K9F1G08B0C", &HEC, &H_F1009540UI, 2048, 64, 64, 1024, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Samsung K9F1G08U0E", &HEC, &H_F1009541UI, 2048, 64, 64, 1024, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Samsung K9F2G08X0 ", &HEC, &H_DA101544UI, 2048, 64, 64, 2048, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Samsung K9F2G08U0C", &HEC, &H_DA109544UI, 2048, 64, 64, 2048, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Samsung K9F2G08U0M", &HEC, &H_00DA8015UI, 2048, 64, 64, 4096, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Samsung K9G4G08U0B", &HEC, &H_DC14A554UI, 2048, 64, 64, 4096, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("Samsung K9G8G08U0B", &HEC, &H_D314A564UI, 2048, 64, 128, 4096, VCC_IF.X8_3V)) '8Gb
            FlashDB.Add(New P_NAND("Samsung K9GAG08U0E", &HEC, &H_D5847250UI, 8192, 436, 128, 2076, VCC_IF.X8_3V)) '16Gb
            FlashDB.Add(New P_NAND("Samsung K9GAG08U0M", &HEC, &H_D514B674UI, 4096, 128, 128, 4096, VCC_IF.X8_3V)) '16Gb
            FlashDB.Add(New P_NAND("Samsung K9W8G08U1M", &HEC, &H_DCC11554UI, 2048, 64, 64, 4096, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Samsung K9F4G08U0A", &HEC, &H_DC109554UI, 2048, 64, 64, 4096, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Samsung K9F4G08U0B", &HEC, &H_DC109554UI, 2048, 64, 64, 4096, VCC_IF.X8_3V)) '(CV)
            FlashDB.Add(New P_NAND("Samsung K9F4G08U0B", &HEC, &H_DC109555UI, 2048, 64, 64, 4096, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Samsung K9K8G08U0A", &HEC, &H_D3519558UI, 2048, 64, 64, 8192, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Samsung K9KAG08U0M", &HEC, &H_D551A668UI, 4096, 128, 64, 8192, VCC_IF.X8_3V)) '8Gb
            FlashDB.Add(New P_NAND("Samsung K9WAG08U1A", &HEC, &H_D3519558UI, 2048, 64, 64, 8192, VCC_IF.X8_3V)) '8Gb
            FlashDB.Add(New P_NAND("Samsung K9K8G08U0A", &HEC, &H_D3519558UI, 2048, 64, 64, 8192, VCC_IF.X8_3V)) '16Gb Dual die (CE1#/CE2#)
            FlashDB.Add(New P_NAND("Samsung K9NBG08U5A", &HEC, &H_D3519558UI, 2048, 64, 64, 8192, VCC_IF.X8_3V)) '32Gb Quad die (CE1#/CE2#/CE3#/CE4#)
            'Hynix SLC x8 devices
            FlashDB.Add(New P_NAND("Hynix HY27US08281A", &HAD, &H_73AD73ADUI, 512, 16, 32, 1024, VCC_IF.X16_3V)) '128Mb
            FlashDB.Add(New P_NAND("Hynix HY27US08561A", &HAD, &H_75AD75ADUI, 512, 16, 32, 2048, VCC_IF.X8_3V)) '256Mb
            FlashDB.Add(New P_NAND("Hynix HY27US16561A", &HAD, &H_55AD55ADUI, 512, 16, 32, 2048, VCC_IF.X16_3V)) '256Mb
            FlashDB.Add(New P_NAND("Hynix HY27SS08561A", &HAD, &H_35AD35ADUI, 512, 16, 32, 2048, VCC_IF.X8_1V8)) '256Mb
            FlashDB.Add(New P_NAND("Hynix HY27SS16561A", &HAD, &H_45AD45ADUI, 512, 16, 32, 2048, VCC_IF.X16_1V8)) '256Mb
            FlashDB.Add(New P_NAND("Hynix HY27US08121B", &HAD, &H_76AD76ADUI, 512, 16, 32, 4096, VCC_IF.X8_3V)) '512Mb
            FlashDB.Add(New P_NAND("Hynix HY27UF081G2A", &HAD, &H_F1805DADUI, 2048, 64, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("Hynix HY27UF161G2A", &HAD, &H_F1805DADUI, 2048, 64, 64, 1024, VCC_IF.X16_3V)) '1Gb
            FlashDB.Add(New P_NAND("Hynix H27U1G8F2B  ", &HAD, &H_0000F1001D, 2048, 64, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("Hynix H27U1G8F2CTR", &HAD, &H_F1801DADUI, 2048, 64, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("Hynix HY27UF081G2M", &HAD, &H_F10015ADUI, 2048, 64, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("Hynix HY27US081G1M", &HAD, &H_0079A500UI, 512, 16, 32, 8192, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Hynix HY27SF081G2M", &HAD, &H_00A10015UI, 2048, 64, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("Hynix HY27UF082G2B", &HAD, &H_DA109544UI, 2048, 64, 64, 2048, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Hynix HY27UF082G2A", &HAD, &H_DA801D00UI, 2048, 64, 64, 2048, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Hynix H27UAG8T2M  ", &HAD, &H_D514B644UI, 4096, 128, 128, 4096, VCC_IF.X8_3V)) '16Gb
            FlashDB.Add(New P_NAND("Hynix H27UAG8T2B  ", &HAD, &H_D5949A74UI, 8192, 448, 256, 512, VCC_IF.X8_3V)) '16Gb
            FlashDB.Add(New P_NAND("Hynix H27U2G8F2C  ", &HAD, &H_DA909546UI, 2048, 64, 64, 2048, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Hynix H27U2G8F2C  ", &HAD, &H_DA909544UI, 2048, 64, 64, 2048, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Hynix H27U2G6F2C  ", &HAD, &H_CA90D544UI, 2048, 64, 64, 2048, VCC_IF.X16_3V))
            FlashDB.Add(New P_NAND("Hynix H27S2G8F2C  ", &HAD, &H_AA901544UI, 2048, 64, 64, 2048, VCC_IF.X8_1V8))
            FlashDB.Add(New P_NAND("Hynix H27S2G6F2C  ", &HAD, &H_BA905544UI, 2048, 64, 64, 2048, VCC_IF.X16_1V8))
            FlashDB.Add(New P_NAND("Hynix H27UBG8T2B  ", &HAD, &H_D794DA74UI, 8192, 640, 256, 2048, VCC_IF.X8_3V)) '32Gb 16Mbit blocks
            FlashDB.Add(New P_NAND("Hynix H27UBG8T2C  ", &HAD, &H_D7949160UI, 8192, 640, 256, 2048, VCC_IF.X8_3V)) '32Gb
            FlashDB.Add(New P_NAND("Hynix H27U4G8F2D  ", &HAD, &H_DC909554UI, 2048, 64, 64, 4096, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Hynix H27U4G6F2D  ", &HAD, &H_CC90D554UI, 2048, 64, 64, 4096, VCC_IF.X16_3V))
            FlashDB.Add(New P_NAND("Hynix H27S4G8F2D  ", &HAD, &H_AC901554UI, 2048, 64, 64, 4096, VCC_IF.X8_1V8))
            FlashDB.Add(New P_NAND("Hynix H27S4G6F2D  ", &HAD, &H_BC905554UI, 2048, 64, 64, 4096, VCC_IF.X16_1V8))
            FlashDB.Add(New P_NAND("Hynix H27UCG8T2FTR", &HAD, &H_DE14AB42UI, 8192, 640, 256, 4180, VCC_IF.X8_3V)) '64Gb
            FlashDB.Add(New P_NAND("Hynix HY27UG084G2M", &HAD, &H_00DC0015UI, 2048, 64, 64, 4096, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("Hynix HY27UG084GDM", &HAD, &H_00DA0015UI, 2048, 64, 64, 4096, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("Hynix HY27UG164G2M", &HAD, &H_00DC0055UI, 2048, 64, 64, 4096, VCC_IF.X16_3V)) '4Gb
            FlashDB.Add(New P_NAND("Hynix HY27UF084G2M", &HAD, &H_00DC8095UI, 2048, 64, 64, 4096, VCC_IF.X8_3V)) '4Gb
            'Spansion SLC 34 series
            FlashDB.Add(New P_NAND("Cypress S34ML01G1  ", &H1, &H_00F1001DUI, 2048, 64, 64, 1024, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Cypress S34ML02G1  ", &H1, &H_DA909544UI, 2048, 64, 64, 2048, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Cypress S34ML04G1  ", &H1, &H_DC909554UI, 2048, 64, 64, 4096, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Cypress S34ML01G1  ", &H1, &H_00C1005DUI, 2048, 64, 64, 1024, VCC_IF.X16_3V))
            FlashDB.Add(New P_NAND("Cypress S34ML02G1  ", &H1, &H_CA90D544UI, 2048, 64, 64, 2048, VCC_IF.X16_3V))
            FlashDB.Add(New P_NAND("Cypress S34ML04G1  ", &H1, &H_CC90D554UI, 2048, 64, 64, 4096, VCC_IF.X16_3V))
            FlashDB.Add(New P_NAND("Cypress S34ML01G2  ", &H1, &H_00F1801DUI, 2048, 64, 64, 1024, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Cypress S34ML02G2  ", &H1, &H_DA909546UI, 2048, 128, 64, 2048, VCC_IF.X8_3V)) '2Gb
            FlashDB.Add(New P_NAND("Cypress S34ML04G2  ", &H1, &H_DC909556UI, 2048, 128, 64, 4096, VCC_IF.X8_3V))
            FlashDB.Add(New P_NAND("Cypress S34ML01G2  ", &H1, &H_00C1805DUI, 2048, 64, 64, 1024, VCC_IF.X16_3V)) 'X16 version
            FlashDB.Add(New P_NAND("Cypress S34ML02G2  ", &H1, &H_CA90D546UI, 2048, 128, 64, 2048, VCC_IF.X16_3V))
            FlashDB.Add(New P_NAND("Cypress S34ML04G2  ", &H1, &H_CC90D556UI, 2048, 128, 64, 4096, VCC_IF.X16_3V))
            FlashDB.Add(New P_NAND("Cypress S34ML04G3  ", &H1, &H_DC000504UI, 2048, 128, 64, 4096, VCC_IF.X16_3V))
            FlashDB.Add(New P_NAND("Cypress S34MS01G200", &H1, &H_00A18015UI, 2048, 64, 64, 4096, VCC_IF.X8_1V8))
            FlashDB.Add(New P_NAND("Cypress S34MS02G200", &H1, &H_AA901546UI, 2048, 64, 64, 4096, VCC_IF.X8_1V8))
            FlashDB.Add(New P_NAND("Cypress S34MS04G200", &H1, &H_AC901556UI, 2048, 64, 64, 4096, VCC_IF.X8_1V8))
            FlashDB.Add(New P_NAND("Cypress S34MS01G204", &H1, &H_00B18055UI, 2048, 64, 64, 4096, VCC_IF.X16_1V8))
            FlashDB.Add(New P_NAND("Cypress S34MS02G204", &H1, &H_BA905546UI, 2048, 128, 64, 4096, VCC_IF.X16_1V8))
            FlashDB.Add(New P_NAND("Cypress S34MS04G204", &H1, &H_BC905556UI, 2048, 128, 64, 4096, VCC_IF.X16_1V8))
            'Dosilicon
            FlashDB.Add(New P_NAND("Dosilicon FMND1G08U3D", &HF8, &H_00F18095UI, 2048, 64, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND1G16U3D", &HF8, &H_00C180D5UI, 2048, 64, 64, 1024, VCC_IF.X16_3V)) '1Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND1G08S3D", &HF8, &H_00A18015UI, 2048, 64, 64, 1024, VCC_IF.X8_1V8)) '1Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND1G16S3D", &HF8, &H_00B18055UI, 2048, 64, 64, 1024, VCC_IF.X16_1V8)) '1Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND2G08U3D", &HF8, &H_DA909546UI, 2048, 64, 64, 2048, VCC_IF.X8_3V)) '2Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND2G16U3D", &HF8, &H_CA90D546UI, 2048, 64, 64, 2048, VCC_IF.X16_3V)) '2Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND2G08S3D", &HF8, &H_AA901546UI, 2048, 64, 64, 2048, VCC_IF.X8_1V8)) '2Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND2G16S3D", &HF8, &H_BA905546UI, 2048, 64, 64, 2048, VCC_IF.X16_1V8)) '2Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND4G08U3B", &HF8, &H_DC909546UI, 2048, 64, 64, 4096, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND4G16U3B", &HF8, &H_CC90D546UI, 2048, 64, 64, 4096, VCC_IF.X16_3V)) '4Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND4G08S3B", &HF8, &H_AC901546UI, 2048, 64, 64, 4096, VCC_IF.X8_1V8)) '4Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND4G16S3B", &HF8, &H_BC905546UI, 2048, 64, 64, 4096, VCC_IF.X16_1V8)) '4Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND4G08U3C", &HF8, &H_DC909546UI, 2048, 128, 64, 4096, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND4G16U3C", &HF8, &H_CC90D546UI, 2048, 128, 64, 4096, VCC_IF.X16_3V)) '4Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND4G08S3C", &HF8, &H_AC901546UI, 2048, 128, 64, 4096, VCC_IF.X8_1V8)) '4Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND4G16S3C", &HF8, &H_BC905546UI, 2048, 128, 64, 4096, VCC_IF.X16_1V8)) '4Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND4G08U3F", &HF8, &H_DC80A662UI, 4096, 256, 64, 2048, VCC_IF.X8_3V)) '4Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND4G16U3F", &HF8, &H_CC80E662UI, 4096, 256, 64, 2048, VCC_IF.X16_3V)) '4Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND4G08S3F", &HF8, &H_AC802662UI, 4096, 256, 64, 2048, VCC_IF.X8_1V8)) '4Gb
            FlashDB.Add(New P_NAND("Dosilicon FMND4G16S3F", &HF8, &H_BC806662UI, 4096, 256, 64, 2048, VCC_IF.X16_1V8)) '4Gb
            FlashDB.Add(New P_NAND("Dosilicon DSND8G08U3N", &HE5, &H_D3C1A666UI, 4096, 256, 64, 2048 * 2, VCC_IF.X8_3V)) '8Gb (dual die)
            FlashDB.Add(New P_NAND("Dosilicon DSND8G16U3N", &HE5, &H_C3C1E666UI, 4096, 256, 64, 2048 * 2, VCC_IF.X16_3V)) '8Gb (dual die)
            FlashDB.Add(New P_NAND("Dosilicon DSND8G08S3N", &HE5, &H_A3C12666UI, 4096, 256, 64, 2048 * 2, VCC_IF.X8_1V8)) '8Gb (dual die)
            FlashDB.Add(New P_NAND("Dosilicon DSND8G16S3N", &HE5, &H_B3C16666UI, 4096, 256, 64, 2048 * 2, VCC_IF.X16_1V8)) '8Gb (dual die)
            FlashDB.Add(New P_NAND("Dosilicon DSND8G08U3M", &HE5, &H_D3D1955AUI, 2048, 64, 64, 4096 * 2, VCC_IF.X8_3V)) '8Gb (dual die)
            FlashDB.Add(New P_NAND("Dosilicon DSND8G16U3M", &HE5, &H_C3D1D55AUI, 2048, 64, 64, 4096 * 2, VCC_IF.X16_3V)) '8Gb (dual die)
            FlashDB.Add(New P_NAND("Dosilicon DSND8G08S3M", &HE5, &H_A3D1155AUI, 2048, 64, 64, 4096 * 2, VCC_IF.X8_1V8)) '8Gb (dual die)
            FlashDB.Add(New P_NAND("Dosilicon DSND8G16S3M", &HE5, &H_B3D1555AUI, 2048, 64, 64, 4096 * 2, VCC_IF.X16_1V8)) '8Gb (dual die)
            'Others
            FlashDB.Add(New P_NAND("FORESEE FS33ND01GS1   ", &HEC, &H_F1009542UI, 2048, 64, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("Zentel A5U1GA31ATS    ", &H92, &H_F1809540UI, 2048, 64, 64, 1024, VCC_IF.X8_3V)) '1Gb (ESMT branded)
            FlashDB.Add(New P_NAND("ESMT F59L1G81MA       ", &HC8, &H_D1809540UI, 2048, 64, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("ESMT F59L1G81MB       ", &HC8, &H_D1809540UI, 2048, 64, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("ESMT F59L1G81LA       ", &HC8, &H_D1809542UI, 2048, 64, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("ESMT F59L2G81A        ", &HC8, &H_DA909544UI, 2048, 64, 64, 2048, VCC_IF.X8_3V)) '2Gb
            FlashDB.Add(New P_NAND("GigaDevice GD9FU1G8F2A", &HC8, &H_F1801D42UI, 2048, 128, 64, 1024, VCC_IF.X8_3V)) '1Gb
            FlashDB.Add(New P_NAND("GigaDevice GD9FU1G6F2A", &HC8, &H_C1805D42UI, 2048, 128, 64, 1024, VCC_IF.X16_3V)) '1Gb
            FlashDB.Add(New P_NAND("GigaDevice GD9FS1G8F2A", &HC8, &H_A1801D42UI, 2048, 128, 64, 1024, VCC_IF.X8_1V8)) '1Gb
            FlashDB.Add(New P_NAND("GigaDevice GD9FS1G6F2A", &HC8, &H_B1805542UI, 2048, 128, 64, 1024, VCC_IF.X16_1V8)) '1Gb

            'FlashDB.Add(New P_NAND("SanDisk SDTNPNAHEM-008G", &H45, &HDE989272UI, 8192, 1024, 516, 2084, VCC_IF.X8_3V) With {.ADDRESS_SCHEME = 3}) '64Gb

        End Sub

        Private Sub OTP_Database()
            FlashDB.Add(New OTP_EPROM("AMD AM27C64", VCC_IF.X8_5V_VPP, &H1, &H15, Kb064))
            FlashDB.Add(New OTP_EPROM("AMD AM27C128", VCC_IF.X8_5V_VPP, &H1, &H16, Kb128))
            FlashDB.Add(New OTP_EPROM("AMD AM27C256", VCC_IF.X8_5V_VPP, &H1, &H10, Kb256))
            FlashDB.Add(New OTP_EPROM("AMD AM27C512", VCC_IF.X8_5V_VPP, &H1, &H91, Kb512))
            FlashDB.Add(New OTP_EPROM("AMD AM27C010", VCC_IF.X8_5V_VPP, &H1, &HE, Mb001))
            FlashDB.Add(New OTP_EPROM("AMD AM27C020", VCC_IF.X8_5V_VPP, &H1, &H97, Mb002))
            FlashDB.Add(New OTP_EPROM("AMD AM27C040", VCC_IF.X8_5V_VPP, &H1, &H9B, Mb004))
            FlashDB.Add(New OTP_EPROM("AMD AM27C080", VCC_IF.X8_5V_VPP, &H1, &H1C, Mb008))
            FlashDB.Add(New OTP_EPROM("AMD AM27C400", VCC_IF.X16_5V_VPP, &H1, &H9D, Mb004))
            FlashDB.Add(New OTP_EPROM("AMD AM27C800", VCC_IF.X16_5V_VPP, &H1, &H1A, Mb008)) 'DIP42/PLCC44
            FlashDB.Add(New OTP_EPROM("ATMEL AT27C010", VCC_IF.X8_5V_VPP, &H1E, &H5, Mb001))
            FlashDB.Add(New OTP_EPROM("ATMEL AT27C020", VCC_IF.X8_5V_VPP, &H1E, &H86, Mb002))
            FlashDB.Add(New OTP_EPROM("ATMEL AT27C040", VCC_IF.X8_5V_VPP, &H1E, &HB, Mb004))
            FlashDB.Add(New OTP_EPROM("ATMEL AT27C080", VCC_IF.X8_5V_VPP, &H1E, &H8A, Mb008))
            FlashDB.Add(New OTP_EPROM("ATMEL AT27C512", VCC_IF.X16_5V_VPP, &H1E, &HF2, Kb512))
            FlashDB.Add(New OTP_EPROM("ATMEL AT27C1024", VCC_IF.X16_5V_VPP, &H1E, &HF1, Mb001))
            FlashDB.Add(New OTP_EPROM("ATMEL AT27C2048", VCC_IF.X16_5V_VPP, &H1E, &HF7, Mb002))
            FlashDB.Add(New OTP_EPROM("ATMEL AT27C4096", VCC_IF.X16_5V_VPP, &H1E, &HF4, Mb004))
            FlashDB.Add(New OTP_EPROM("Intel M27C64", VCC_IF.X8_5V_VPP, &H89, &H7, Kb064))
            FlashDB.Add(New OTP_EPROM("Intel M27128A", VCC_IF.X8_5V_VPP, &H89, &H89, Kb064))
            FlashDB.Add(New OTP_EPROM("MX 27C1000", VCC_IF.X8_5V_VPP, &HC2, &HE, Mb001))
            FlashDB.Add(New OTP_EPROM("MX 27C1001", VCC_IF.X8_5V_VPP, &HC2, &HF, Mb001))
            FlashDB.Add(New OTP_EPROM("MX 27C2000", VCC_IF.X8_5V_VPP, &HC2, &H20, Mb002))
            FlashDB.Add(New OTP_EPROM("MX 27C4000", VCC_IF.X8_5V_VPP, &HC2, &H40, Mb004))
            FlashDB.Add(New OTP_EPROM("ST M27C64A", VCC_IF.X8_5V_VPP, &H9B, &H8, Kb064))
            FlashDB.Add(New OTP_EPROM("ST M27C256B", VCC_IF.X8_5V_VPP, &H20, &H8D, Kb256))
            FlashDB.Add(New OTP_EPROM("ST M27C512", VCC_IF.X8_5V_VPP, &H20, &H3D, Kb512))
            FlashDB.Add(New OTP_EPROM("ST M27C1024", VCC_IF.X16_5V_VPP, &H20, &H8C, Mb001))
            FlashDB.Add(New OTP_EPROM("ST M27C1001", VCC_IF.X8_5V_VPP, &H20, &H5, Mb001))
            FlashDB.Add(New OTP_EPROM("ST M27C2001", VCC_IF.X8_5V_VPP, &H20, &H61, Mb002))
            FlashDB.Add(New OTP_EPROM("ST M27C4001", VCC_IF.X8_5V_VPP, &H20, &H41, Mb004))
            FlashDB.Add(New OTP_EPROM("ST M27C4002", VCC_IF.X16_5V_VPP, &H20, &H44, Mb004)) 'DIP40/PLCC44
            FlashDB.Add(New OTP_EPROM("ST M27C800", VCC_IF.X8_5V_VPP, &H20, &HB2, Mb008))
            FlashDB.Add(New OTP_EPROM("ST M27C801", VCC_IF.X8_5V_VPP, &H20, &H42, Mb008))
            FlashDB.Add(New OTP_EPROM("ST M27C160", VCC_IF.X16_5V_VPP, &H20, &HB1, Mb016))
            FlashDB.Add(New OTP_EPROM("ST M27C300", VCC_IF.X8_5V_VPP, &H20, &H32, Mb032))
            FlashDB.Add(New OTP_EPROM("ST M27C322", VCC_IF.X16_5V_VPP, &H20, &H34, Mb032))
            FlashDB.Add(New OTP_EPROM("TI TMS27C32", VCC_IF.X8_5V_VPP, &H97, &H8, Kb032))
            FlashDB.Add(New OTP_EPROM("TI TMS27C64", VCC_IF.X8_5V_VPP, &H97, &H7, Kb064))
            FlashDB.Add(New OTP_EPROM("TI TMS27C128", VCC_IF.X8_5V_VPP, &H97, &H83, Kb128))
            FlashDB.Add(New OTP_EPROM("TI TMS27C256", VCC_IF.X8_5V_VPP, &H97, &H4, Kb256))
            FlashDB.Add(New OTP_EPROM("TI TMS27C512", VCC_IF.X8_5V_VPP, &H97, &H85, Kb512))
            FlashDB.Add(New OTP_EPROM("TI TMS27C010", VCC_IF.X8_5V_VPP, &H97, &HD6, Mb001))
            FlashDB.Add(New OTP_EPROM("TI TMS27C020", VCC_IF.X8_5V_VPP, &H97, &H32, Mb002))
            FlashDB.Add(New OTP_EPROM("TI TMS27C040", VCC_IF.X8_5V_VPP, &H97, &H50, Mb004))
            FlashDB.Add(New OTP_EPROM("Mitsubishi M5M27C402K", VCC_IF.X16_5V_VPP, &H1C, &H8F, Mb004)) 'DIP40
        End Sub

        Private Sub FWH_Database()
            FlashDB.Add(New FWH("Atmel AT49LH002", &H1F, &HE9, Mb002, Kb512, &H20))
            FlashDB.Add(New FWH("Atmel AT49LH004", &H1F, &HEE, Mb004, Kb512, &H20))
            FlashDB.Add(New FWH("Winbond W39V040FA", &HDA, &H34, Mb004, Kb032, &H50)) 'CV
            FlashDB.Add(New FWH("Winbond W39V080FA", &HDA, &HD3, Mb008, Kb032, &H50))
            FlashDB.Add(New FWH("Microchip SST49LF002A", &HBF, &H57, Mb002, Kb032, &H30))
            FlashDB.Add(New FWH("Microchip SST49LF003A", &HBF, &H1B, Mb003, Kb032, &H30))
            FlashDB.Add(New FWH("Microchip SST49LF004A", &HBF, &H60, Mb004, Kb032, &H30))
            FlashDB.Add(New FWH("Microchip SST49LF008A", &HBF, &H5A, Mb008, Kb032, &H30))
            FlashDB.Add(New FWH("Microchip SST49LF080A", &HBF, &H5B, Mb008, Kb032, &H30)) 'CV
            FlashDB.Add(New FWH("Microchip SST49LF016C", &HBF, &H5C, Mb016, Kb032, &H30))
            FlashDB.Add(New FWH("ISSI PM49FL002", &H9D, &H6D, Mb002, Kb032, &H30))
            FlashDB.Add(New FWH("ISSI PM49FL004", &H9D, &H6E, Mb004, Kb032, &H30))
            FlashDB.Add(New FWH("ISSI PM49FL008", &H9D, &H6A, Mb008, Kb032, &H30))
        End Sub

        Private Sub MICROWIRE_Database()
            FlashDB.Add(New MICROWIRE("Generic 93xx46A", 128, 7, 0)) 'X8 ONLY
            FlashDB.Add(New MICROWIRE("Generic 93xx46B", 128, 0, 6)) 'X16 ONLY
            FlashDB.Add(New MICROWIRE("Generic 93xx46C", 128, 7, 6)) 'X8/X16
            FlashDB.Add(New MICROWIRE("Generic 93xx56A", 256, 9, 0)) 'X8 ONLY
            FlashDB.Add(New MICROWIRE("Generic 93xx56B", 256, 0, 8)) 'X16 ONLY
            FlashDB.Add(New MICROWIRE("Generic 93xx56C", 256, 9, 8)) 'X8/X16
            FlashDB.Add(New MICROWIRE("Generic 93xx66A", 512, 9, 0)) 'X8 ONLY
            FlashDB.Add(New MICROWIRE("Generic 93xx66B", 512, 0, 8)) 'X16 ONLY
            FlashDB.Add(New MICROWIRE("Generic 93xx66C", 512, 9, 8)) 'X8/X16
            FlashDB.Add(New MICROWIRE("Generic 93xx76A", 1024, 10, 0)) 'X8 ONLY
            FlashDB.Add(New MICROWIRE("Generic 93xx76B", 1024, 0, 9)) 'X16 ONLY
            FlashDB.Add(New MICROWIRE("Generic 93xx76C", 1024, 10, 9)) 'X8/X16
            FlashDB.Add(New MICROWIRE("Generic 93xx86A", 2048, 11, 0)) 'X8 ONLY
            FlashDB.Add(New MICROWIRE("Generic 93xx86B", 2048, 0, 10)) 'X16 ONLY
            FlashDB.Add(New MICROWIRE("Generic 93xx86C", 2048, 11, 10)) 'X8/X16
        End Sub
        'Helper function to create the proper definition for Atmel/Adesto Series 45 SPI devices
        Private Function CreateSeries45(atName As String, mbitsize As UInt32, id1 As UInt16, id2 As UInt16, page_size As UInt16) As SPI_NOR
            Dim atmel_spi As New SPI_NOR(atName, SPI_3V, mbitsize, &H1F, id1, &H50, (page_size * 8US))
            atmel_spi.ID2 = id2
            atmel_spi.PAGE_SIZE = page_size
            atmel_spi.PAGE_SIZE_EXTENDED = page_size + (page_size \ 32US) 'Additional bytes available per page
            atmel_spi.ProgramMode = SPI_PROG.Atmel45Series  'Atmel Series 45
            atmel_spi.OP_COMMANDS.RDSR = &HD7
            atmel_spi.OP_COMMANDS.READ = &HE8
            atmel_spi.OP_COMMANDS.PROG = &H12
            Return atmel_spi
        End Function

        Public Function FindDevice(MFG As Byte, ID1 As UInt16, ID2 As UInt16, DEVICE As MemoryType, Optional FM As Byte = 0) As Device
            Select Case DEVICE
                Case MemoryType.PARALLEL_NAND
                    For Each flash In MemDeviceSelect(MemoryType.PARALLEL_NAND)
                        If flash.MFG_CODE = MFG Then
                            If NAND_DeviceMatches(ID1, ID2, flash.ID1, flash.ID2) Then
                                Return flash
                            End If
                        End If
                    Next
                Case MemoryType.SERIAL_NAND
                    For Each flash In MemDeviceSelect(MemoryType.SERIAL_NAND)
                        If SNAND_DeviceMatches(MFG, ID1, flash.MFG_CODE, flash.ID1) Then
                            Return flash
                        End If
                    Next
                Case MemoryType.SERIAL_NOR
                    Dim list As New List(Of SPI_NOR)
                    For Each flash In MemDeviceSelect(MemoryType.SERIAL_NOR)
                        If flash.MFG_CODE = MFG Then
                            If (flash.ID1 = ID1) Then
                                list.Add(DirectCast(flash, SPI_NOR))
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
                Case MemoryType.PARALLEL_NOR Or MemoryType.OTP_EPROM
                    For Each flash In MemDeviceSelect(DEVICE)
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
                    Next
                Case Else
                    For Each flash In MemDeviceSelect(DEVICE)
                        If flash.MFG_CODE = MFG AndAlso flash.ID1 = ID1 Then Return flash
                    Next
            End Select
            Return Nothing 'Not found
        End Function

        Public Function FindDevices(MFG As Byte, ID1 As UInt16, ID2 As UInt16, DEVICE As MemoryType) As Device()
            Dim devices As New List(Of Device)
            For Each flash In FlashDB
                If flash.FLASH_TYPE = DEVICE Then
                    If flash.MFG_CODE = MFG Then
                        If flash.FLASH_TYPE = MemoryType.PARALLEL_NAND Then
                            If NAND_DeviceMatches(ID1, ID2, flash.ID1, flash.ID2) Then
                                devices.Add(flash)
                            End If
                        Else
                            If (flash.ID1 = ID1) Then
                                If (flash.ID2 = 0) OrElse (flash.ID2 = ID2) Then
                                    devices.Add(flash)
                                End If
                            End If
                        End If
                    End If
                End If
            Next
            Return devices.ToArray
        End Function

        Public Function FindDevice_NORX8(MFG As Byte, ID1 As Byte, ID2 As Byte) As Device()
            If ID2 = &HFF Then ID2 = 0
            Dim devices As New List(Of Device)
            For Each flash In FlashDB
                If flash.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                    Dim nor_flash As P_NOR = DirectCast(flash, P_NOR)
                    If (NOR_IsType8X(nor_flash.IFACE)) Then
                        Dim FLASH_ID1 As Byte = CByte(nor_flash.ID1 And 255)
                        If MFG = nor_flash.MFG_CODE AndAlso ID1 = FLASH_ID1 AndAlso ID2 = nor_flash.ID2 Then
                            devices.Add(flash)
                        End If
                    End If
                End If
            Next
            Return devices.ToArray
        End Function

        Public Function FindDevice_NORX16(MFG As Byte, ID1 As UInt16, ID2 As UInt16) As Device()
            Dim devices As New List(Of Device)
            For Each flash In FlashDB
                If flash.FLASH_TYPE = MemoryType.PARALLEL_NOR Then
                    Dim nor_flash As P_NOR = DirectCast(flash, P_NOR)
                    If (NOR_IsType16X(nor_flash.IFACE)) Then
                        If MFG = nor_flash.MFG_CODE AndAlso ID1 = nor_flash.ID1 Then
                            If (flash.ID2 = 0) OrElse (flash.ID2 = ID2) Then
                                devices.Add(flash)
                            End If
                        End If
                    End If
                End If
            Next
            Return devices.ToArray
        End Function

        Private Function NAND_DeviceMatches(TARGET_ID1 As UInt16, TARGET_ID2 As UInt16, LIST_ID1 As UInt16, LIST_ID2 As UInt16) As Boolean
            Dim NAND_ID As UInt32 = (CUInt(TARGET_ID1) << 16) Or CUInt(TARGET_ID2)
            Dim LIST_ID As UInt32 = (CUInt(LIST_ID1) << 16) Or CUInt(LIST_ID2)
            Dim Matched As Boolean = True
            For i = 0 To 3
                Dim B1 As Byte = CByte(NAND_ID >> ((3 - i) * 8) And 255)
                Dim B2 As Byte = CByte(LIST_ID >> ((3 - i) * 8) And 255)
                If B2 = 0 Then 'If this is 0, we do not check
                ElseIf (B1 = B2) Then
                Else
                    Matched = False
                    Exit For 'Not a match
                End If
            Next
            Return Matched
        End Function

        Private Function SNAND_DeviceMatches(TARGET_MFG As Byte, TARGET_ID1 As UInt16, LIST_MFG As Byte, LIST_ID1 As UInt16) As Boolean
            If TARGET_MFG = LIST_MFG Then
                If TARGET_ID1 = LIST_ID1 Then
                    Return True
                End If
            ElseIf TARGET_MFG = LIST_ID1 Then
                If LIST_MFG = TARGET_ID1 Then
                    Return True
                End If
            End If
            Return False
        End Function
        'Returns the total number of devices for a specific flash technology
        Public Function PartCount(Optional filter_device As MemoryType = MemoryType.UNSPECIFIED) As UInt32
            Dim Count As UInt32 = 0
            For Each flash In MemDeviceSelect(filter_device)
                Count += 1UI
            Next
            Return Count
        End Function

        Private Iterator Function MemDeviceSelect(m_type As MemoryType) As IEnumerable(Of Device)
            For Each flash In FlashDB
                If m_type = MemoryType.UNSPECIFIED Then
                    Yield flash
                ElseIf m_type = flash.FLASH_TYPE Then
                    Yield flash
                End If
            Next
        End Function


#Region "Catalog / Data file"
        Public Sub CreateHtmlCatalog(FlashType As MemoryType, ColumnCount As UInt32, file_name As String, Optional size_limit As Int64 = 0)
            Dim TotalParts() As Device = GetFlashDevices(FlashType)
            Dim FilteredParts As New List(Of Device)
            If size_limit = 0 Then
                FilteredParts.AddRange(TotalParts)
            Else
                For Each part_number In TotalParts
                    If part_number.FLASH_SIZE <= size_limit Then FilteredParts.Add(part_number)
                Next
            End If
            Dim FlashDevices() As DeviceCollection = SortFlashDevices(FilteredParts.ToArray)
            Dim RowCount As Integer = CInt(Math.Ceiling(FlashDevices.Length / ColumnCount))
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
                Case MemoryType.PARALLEL_NAND
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
                    Dim s2() As String = cell_contents(x).Split(CChar(vbCrLf))
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
        Private Function CreatePartTable(title As String, part_str() As String, part_prefix As String, column_size As Integer) As String
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
        Public Function GetFlashDevices(type As MemoryType) As Device()
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
            ElseIf type = MemoryType.PARALLEL_NAND Then
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.PARALLEL_NAND Then
                        dev.Add(flash)
                    End If
                Next
            ElseIf type = MemoryType.FWH_NOR Then
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.FWH_NOR Then
                        dev.Add(flash)
                    End If
                Next
            ElseIf type = MemoryType.HYPERFLASH Then
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.HYPERFLASH Then
                        dev.Add(flash)
                    End If
                Next
            ElseIf type = MemoryType.SERIAL_MICROWIRE Then
                For Each flash In FlashDB
                    If flash.FLASH_TYPE = MemoryType.SERIAL_MICROWIRE Then
                        dev.Add(flash)
                    End If
                Next
            End If
            Return dev.ToArray
        End Function
        'Sorts a collection into a group of the same manufacturer name
        Private Function SortFlashDevices(devices() As Device) As DeviceCollection()
            Dim GrowingCollection As New List(Of DeviceCollection)
            For Each dev In devices
                Dim SkipAdd As Boolean = False
                If dev.FLASH_TYPE = MemoryType.SERIAL_NOR Then
                    If DirectCast(dev, SPI_NOR).ProgramMode = SPI_PROG.SPI_EEPROM Then
                        SkipAdd = True
                    ElseIf DirectCast(dev, SPI_NOR).ProgramMode = SPI_PROG.Nordic Then
                        SkipAdd = True
                    End If
                End If
                If (Not SkipAdd) Then
                    Dim Manu As String = dev.NAME
                    If Manu.Contains(" ") Then Manu = Manu.Substring(0, Manu.IndexOf(" "))
                    'Dim Part As String = dev.NAME.Substring(Manu.Length + 1)
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

        Private Function DevColIndexOf(ByRef Collection As List(Of DeviceCollection), ManuName As String) As DeviceCollection
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

        Private Sub GeneratePartNames(input As DeviceCollection, ByRef part_numbers() As String)
            ReDim part_numbers(input.Parts.Length - 1)
            For i = 0 To part_numbers.Length - 1
                Dim part_name As String = input.Parts(i).NAME
                If part_name.Contains(" ") Then part_name = part_name.Substring(input.Name.Length + 1)
                If part_name.Equals("W25M121AV") Then
                    part_numbers(i) = part_name & " (128Mbit/1Gbit)"
                Else
                    part_numbers(i) = part_name & " (" & GetDataSizeString(input.Parts(i).FLASH_SIZE) & ")"
                End If
            Next
        End Sub

#End Region

    End Class

    Public Class NAND_LAYOUT_TOOL

        Public ReadOnly Property Layout As NandMemLayout

        Sub New(defined_layout As NandMemLayout)
            Me.Layout = defined_layout
        End Sub

        Public Structure NANDLAYOUT_STRUCTURE
            Dim Layout_Main As UInt16
            Dim Layout_Spare As UInt16
        End Structure

        Public Function GetStructure(nand_dev As Device) As NANDLAYOUT_STRUCTURE
            Dim current_value As NANDLAYOUT_STRUCTURE
            Dim nand_page_size As UInt32
            Dim nand_ext_size As UInt32
            If nand_dev.GetType Is GetType(SPI_NAND) Then
                nand_page_size = DirectCast(nand_dev, SPI_NAND).PAGE_SIZE
                nand_ext_size = DirectCast(nand_dev, SPI_NAND).PAGE_EXT
            ElseIf nand_dev.GetType Is GetType(P_NAND) Then
                nand_page_size = DirectCast(nand_dev, P_NAND).PAGE_SIZE
                nand_ext_size = DirectCast(nand_dev, P_NAND).PAGE_EXT
            End If
            If Me.Layout = NandMemLayout.Separated Then
                current_value.Layout_Main = CUShort(nand_page_size)
                current_value.Layout_Spare = CUShort(nand_ext_size)
            ElseIf Me.Layout = NandMemLayout.Segmented Then
                Select Case nand_page_size
                    Case 2048
                        current_value.Layout_Main = CUShort(nand_page_size \ 4)
                        current_value.Layout_Spare = CUShort(nand_ext_size \ 4)
                    Case Else
                        current_value.Layout_Main = CUShort(nand_page_size)
                        current_value.Layout_Spare = CUShort(nand_ext_size)
                End Select
            End If
            Return current_value
        End Function

        Private Sub FillMain(nand_dev As Device, ByRef cache_data() As Byte, main_data() As Byte, ByRef data_ptr As UInt32, ByRef bytes_left As UInt32)
            Dim ext_page_size As UInt16
            If nand_dev.GetType Is GetType(SPI_NAND) Then
                ext_page_size = DirectCast(nand_dev, SPI_NAND).PAGE_EXT
            ElseIf nand_dev.GetType Is GetType(P_NAND) Then
                ext_page_size = DirectCast(nand_dev, P_NAND).PAGE_EXT
            End If
            Dim nand_layout As NANDLAYOUT_STRUCTURE = GetStructure(nand_dev)
            Dim page_size_tot As UInt16 = (nand_dev.PAGE_SIZE + ext_page_size)
            Dim logical_block As UInt16 = (nand_layout.Layout_Main + nand_layout.Layout_Spare)
            Dim sub_index As UInt16 = 1
            Dim adj_offset As UInt16 = 0
            Do While Not (adj_offset = page_size_tot)
                Dim sub_left As UInt16 = nand_layout.Layout_Main
                If (sub_left > bytes_left) Then sub_left = CUShort(bytes_left)
                Array.Copy(main_data, data_ptr, cache_data, adj_offset, sub_left)
                data_ptr += sub_left
                bytes_left -= sub_left
                If (bytes_left = 0) Then Exit Do
                adj_offset = (sub_index * logical_block)
                sub_index += 1US
            Loop
        End Sub

        Private Sub FillSpare(nand_dev As Device, ByRef cache_data() As Byte, oob_data() As Byte, ByRef oob_ptr As UInt32, ByRef bytes_left As UInt32)
            Dim page_size_ext As UInt16
            If nand_dev.GetType Is GetType(SPI_NAND) Then
                page_size_ext = DirectCast(nand_dev, SPI_NAND).PAGE_EXT
            ElseIf nand_dev.GetType Is GetType(P_NAND) Then
                page_size_ext = DirectCast(nand_dev, P_NAND).PAGE_EXT
            End If
            Dim nand_layout As NANDLAYOUT_STRUCTURE = GetStructure(nand_dev)
            Dim page_size_tot As UInt16 = (nand_dev.PAGE_SIZE + page_size_ext)
            Dim logical_block As UInt16 = (nand_layout.Layout_Main + nand_layout.Layout_Spare)
            Dim sub_index As UInt16 = 2
            Dim adj_offset As UInt16 = (logical_block - nand_layout.Layout_Spare)
            Do While Not ((adj_offset - nand_layout.Layout_Main) = page_size_tot)
                Dim sub_left As UInt16 = nand_layout.Layout_Spare
                If sub_left > bytes_left Then sub_left = CUShort(bytes_left)
                Array.Copy(oob_data, oob_ptr, cache_data, adj_offset, sub_left)
                oob_ptr += sub_left
                bytes_left -= sub_left
                If (bytes_left = 0) Then Exit Do
                adj_offset = (sub_index * logical_block) - nand_layout.Layout_Spare
                sub_index += 1US
            Loop
        End Sub

        Public Function CreatePageAligned(nand_dev As Device, main_data() As Byte, oob_data() As Byte) As Byte()
            Dim page_size_ext As UInt16
            If nand_dev.GetType Is GetType(SPI_NAND) Then
                page_size_ext = DirectCast(nand_dev, SPI_NAND).PAGE_EXT
            ElseIf nand_dev.GetType Is GetType(P_NAND) Then
                page_size_ext = DirectCast(nand_dev, P_NAND).PAGE_EXT
            End If
            Dim page_size_tot As UInt16 = CUShort(nand_dev.PAGE_SIZE + page_size_ext)
            Dim total_pages As UInt32 = 0
            Dim data_ptr As UInt32 = 0
            Dim oob_ptr As UInt32 = 0
            Dim page_aligned() As Byte = Nothing
            If main_data Is Nothing Then
                total_pages = CUInt(oob_data.Length \ page_size_ext)
                ReDim main_data(CInt(total_pages * CUInt(nand_dev.PAGE_SIZE)) - 1)
                Utilities.FillByteArray(main_data, 255)
            ElseIf oob_data Is Nothing Then
                total_pages = CUInt(main_data.Length \ nand_dev.PAGE_SIZE)
                ReDim oob_data(CInt(total_pages * CUInt(page_size_ext)) - 1)
                Utilities.FillByteArray(oob_data, 255)
            Else
                total_pages = CUInt(main_data.Length \ nand_dev.PAGE_SIZE)
            End If
            ReDim page_aligned(CInt(total_pages * CUInt(page_size_tot)) - 1)
            Dim bytes_left As UInt32 = CUInt(page_aligned.Length)
            For i = 0 To total_pages - 1
                Dim cache_data(page_size_tot - 1) As Byte
                If main_data IsNot Nothing Then FillMain(nand_dev, cache_data, main_data, data_ptr, bytes_left)
                If oob_data IsNot Nothing Then FillSpare(nand_dev, cache_data, oob_data, oob_ptr, bytes_left)
                Array.Copy(cache_data, 0, page_aligned, (i * page_size_tot), cache_data.Length)
            Next
            Return page_aligned
        End Function
        'Returns the page index from a given address
        Public Function GetNandPageAddress(nand_dev As Device, nand_addr As Long, memory_area As FlashArea) As Integer
            Dim nand_page_size As UInt16 '0x800 (2048)
            Dim nand_ext_size As UInt16 '0x40 (64)
            If nand_dev.GetType Is GetType(SPI_NAND) Then
                nand_page_size = DirectCast(nand_dev, SPI_NAND).PAGE_SIZE
                nand_ext_size = DirectCast(nand_dev, SPI_NAND).PAGE_EXT
            ElseIf nand_dev.GetType Is GetType(P_NAND) Then
                nand_page_size = DirectCast(nand_dev, P_NAND).PAGE_SIZE
                nand_ext_size = DirectCast(nand_dev, P_NAND).PAGE_EXT
            End If
            If (memory_area = FlashArea.Main) Then
                Return CInt(nand_addr \ nand_page_size)
            ElseIf (memory_area = FlashArea.OOB) Then
                Return CInt(nand_addr \ nand_ext_size)
            ElseIf (memory_area = FlashArea.All) Then   'we need to adjust large address to logical address
                Return CInt(nand_addr \ (nand_page_size + nand_ext_size))
            Else
                Return 0
            End If
        End Function

    End Class

    Public Module Tools

        Public Function GetFlashResult(ident_data() As Byte) As FlashDetectResult
            Dim result As New FlashDetectResult
            result.Successful = False
            If ident_data Is Nothing Then Return result
            If ident_data(0) = 0 AndAlso ident_data(2) = 0 Then Return result '0x0000
            If ident_data(0) = &H90 AndAlso ident_data(2) = &H90 Then Return result '0x9090 
            If ident_data(0) = &H90 AndAlso ident_data(2) = 0 Then Return result '0x9000 
            If ident_data(0) = &HFF AndAlso ident_data(2) = &HFF Then Return result '0xFFFF 
            If ident_data(0) = &HFF AndAlso ident_data(2) = 0 Then Return result '0xFF00
            If ident_data(0) = &H1 AndAlso ident_data(1) = 0 AndAlso ident_data(2) = &H1 AndAlso ident_data(3) = 0 Then Return result '0x01000100
            If Array.TrueForAll(ident_data, Function(a) a.Equals(ident_data(0))) Then Return result 'If all bytes are the same
            result.MFG = ident_data(0)
            result.ID1 = (CUShort(ident_data(1)) << 8) Or CUShort(ident_data(2))
            result.ID2 = (CUShort(ident_data(3)) << 8) Or CUShort(ident_data(4))
            If result.ID1 = 0 AndAlso result.ID2 = 0 Then Return result
            result.Successful = True
            Return result
        End Function

        Public Function GetTypeString(MemType As MemoryType) As String
            Select Case MemType
                Case MemoryType.PARALLEL_NOR 'CFI devices
                    Return "Parallel NOR"
                Case MemoryType.SERIAL_NOR 'SPI devices
                    Return "SPI NOR Flash"
                Case MemoryType.SERIAL_QUAD
                    Return "SPI QUAD NOR Flash"
                Case MemoryType.SERIAL_NAND
                    Return "SPI NAND Flash"
                Case MemoryType.SERIAL_I2C
                    Return "I2C EEPROM"
                Case MemoryType.SERIAL_MICROWIRE
                    Return "Microwire EEPROM"
                Case MemoryType.PARALLEL_NAND
                    Return "NAND Flash"
                Case MemoryType.JTAG_CFI
                    Return "CFI Flash"
                Case MemoryType.JTAG_SPI
                    Return "SPI Flash"
                Case MemoryType.FWH_NOR
                    Return "Firmware Hub Flash"
                Case MemoryType.DFU_MODE
                    Return "AVR Firmware"
                Case MemoryType.OTP_EPROM
                    Return "EPROM"
                Case MemoryType.HYPERFLASH
                    Return "HyperFlash"
                Case MemoryType.SERIAL_SWI
                    Return "SWI EEPROM"
                Case Else
                    Return String.Empty
            End Select
        End Function

        Public Function NOR_IsType8X(input As VCC_IF) As Boolean
            Select Case input
                Case VCC_IF.X8_3V
                    Return True
                Case VCC_IF.X8_1V8
                    Return True
                Case VCC_IF.X8_5V
                    Return True
                Case VCC_IF.X8_5V_VPP
                    Return True
                Case VCC_IF.X16_X8_3V
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Public Function NOR_IsType16X(input As VCC_IF) As Boolean
            Select Case input
                Case VCC_IF.X16_3V
                    Return True
                Case VCC_IF.X16_1V8
                    Return True
                Case VCC_IF.X16_5V
                    Return True
                Case VCC_IF.X16_5V_VPP
                    Return True
                Case VCC_IF.X16_X8_3V
                    Return True
                Case Else
                    Return False
            End Select
        End Function

    End Module


End Namespace
