using FlashcatUSB;
using System;
using System.Collections.Generic;

namespace FlashMemory
{
    public static class Constants
    {
        public const uint Kb016 = 2048U;
        public const uint Kb032 = 4096U;
        public const uint Kb064 = 8192U;
        public const uint Kb128 = 16384U;
        public const uint Kb256 = 32768U;
        public const uint Kb512 = 65536U;
        public const uint Mb001 = 131072U;
        public const uint Mb002 = 262144U;
        public const uint Mb003 = 393216U;
        public const uint Mb004 = 524288U;
        public const uint Mb008 = 1048576U;
        public const uint Mb016 = 2097152U;
        public const uint Mb032 = 4194304U;
        public const uint Mb064 = 8388608U;
        public const uint Mb128 = 16777216U;
        public const uint Mb256 = 33554432U;
        public const uint Mb512 = 67108864U;
        public const uint Gb001 = 134217728U;
        public const uint Gb002 = 268435456U;
        public const uint Gb004 = 536870912U;
        public const uint Gb008 = 1073741824U;
        public const uint Gb016 = 2147483648U;
        public const ulong Gb032 = 4294967296UL;
        public const ulong Gb064 = 8589934592UL;
        public const ulong Gb128 = 17179869184UL;
        public const ulong Gb256 = 34359738368UL;

        public static string GetDataSizeString(long size)
        {
            string size_str = "";
            if (size < Mb001)
            {
                size_str = (size / 128d).ToString() + "Kbit";
            }
            else if (size < Gb001)
            {
                size_str = (size / (double)Mb001).ToString() + "Mbit";
            }
            else
            {
                size_str = (size / (double)Gb001).ToString() + "Gbit";
            }

            return size_str;
        }
    }

    public struct FlashDetectResult
    {
        public bool Successful { get; set; }
        public byte MFG { get; set; }
        public ushort ID1 { get; set; }
        public ushort ID2 { get; set; }

        public uint PART
        {
            get
            {
                return (uint)ID1 << 16 | ID2;
            }
        }
    }

    public enum MemoryType {
        UNSPECIFIED,
        PARALLEL_NOR,
        OTP_EPROM,
        SERIAL_NOR, // SPI devices
        SERIAL_QUAD, // SQI devices
        SERIAL_I2C, // I2C EEPROMs
        SERIAL_MICROWIRE,
        SERIAL_NAND, // SPI NAND devices
        SERIAL_SWI, // Atmel single-wire
        PARALLEL_NAND, // NAND X8 devices
        JTAG_CFI, // Non-Vol memory attached to a MCU with DMA access
        JTAG_SPI, // SPI devices connected to an MCU with a SPI access register
        JTAG_BSDL, // CFI Flash via Boundary Scan
        FWH_NOR, // Firmware hub memories
        HYPERFLASH,
        DFU_MODE
    }

    public enum FlashArea : byte
    {
        Main = 0, // Data area (read main page, skip to next page)
        OOB = 1, // Extended area (skip main page, read oob page)
        All = 2, // All data (read main page, then oob page, repeat)
        NotSpecified = 255
    }

    public enum MFP_PRG : ushort
    {
        Standard = 0, // Use the standard sequence that chip id detected
        PageMode = 1, // Writes an entire page of data (128 bytes etc.)
        BypassMode = 2, // Writes 0,64,128 bytes using ByPass sequence; 0x555=0xAA;0x2AA=55;0x555=20;(0x00=0xA0;SA=DATA;...)0x00=0x90;0x00=0x00
        IntelSharp = 3, // Writes data (SA=0x40;SA=DATA;SR.7), erases sectors (SA=0x50;SA=0x60;SA=0xD0,SR.7,SA=0x20;SA=0xD0,SR.7)
        Buffer1 = 4, // Use Write-To-Buffer mode (x16 only), used mostly by Intel (SA=0xE8;...;SA=0xD0)
        Buffer2 = 5 // Use Write-To-Buffer mode (x16 only), Used by Spansion/Winbond (0x555=0xAA;0x2AA=0x55,SA=0x25;SA=WC;...;SA=0x29;DELAY)
    }

    public enum MFP_DELAY : ushort {
        None = 0,
        uS = 1, // Wait for uS delay cycles (set HARDWARE_DELAY to specify cycles)
        mS = 2, // Wait for mS delay cycles (set HARDWARE_DELAY to specify cycles)
        SR1 = 3, // Wait for Status-Register (0x555=0x70,[sr>>7],EXIT), used by Spansion/Cypress
        SR2 = 4, // Wait for Status-Register (0x5555=0xAA,0x2AAA=0x55,0x5555=0x70,[sr>>7])
        DQ7 = 5, // Wait for DQ7 to equal last byte written (lower byte for X16)
        NAND = 6, // Used by NAND mode
        HF = 7, // Used by HYPERFLASH
        RYRB = 8, // Wait for RY/BY pin to be HIGH
        NAND_SR = 9 //Wait for NAND using command instead of RB_x pin
    }

    public enum VCC_IF : ushort
    {
        UNKNOWN = 0,
        SERIAL_1V8 = 1,
        SERIAL_2V5 = 2,
        SERIAL_3V = 3,
        SERIAL_5V = 4,
        SERIAL_1V8_5V = 6, // 1.8-5.5V
        SERIAL_2V7_5V = 7, // 2.5-5.5V
        X8_3V = 8, // DQ[0..7]; VCC=3V; VIO=3V
        X8_1V8 = 9, // DQ[0..7]; VCC=1.8V; VIO=1.8V
        X8_3V_1V8 = 10, // DQ[0..7]; VCC=3V; VIO=1.8V
        X8_5V = 11, // DQ[0..7]; VCC=5V
        X16_3V = 12, // DQ[0..15]; VCC=3V; VIO=3V
        X16_1V8 = 13, // DQ[0..15]; VCC=1.8V; VIO=1.8V
        X16_3V_1V8 = 14, // DQ[0..15]; VCC=3V; VIO=1.8V
        X16_5V = 17, // DQ[0..15]; VCC=5V; VIO=5V
        X8_5V_12VPP = 18, // DQ[0..7]; VCC=5V; 12V ERASE/PRG
        X16_3V_12VPP = 19, // Supported in PCB 2.0
        X16_5V_12VPP = 20 // DQ[0..7]; VCC=5V; 12V ERASE/PRG
    }

    public enum BLKLYT : ushort
    {
        Four_Top = 0,
        Two_Top = 1,
        Four_Btm = 2,
        Two_Btm = 3,
        Dual = 4, // Contans top and bottom boot
        // Uniform block sizes
        Kb016_Uni = 5, // 2KByte
        Kb032_Uni = 6, // 4KByte
        Kb064_Uni = 7, // 8KByte
        Kb128_Uni = 8, // 16KByte
        Kb256_Uni = 9, // 32KByte
        Kb512_Uni = 10, // 64KByte
        Mb001_Uni = 11, // 128KByte
        // Non-Uniform
        Mb002_NonUni = 12,
        Mb032_NonUni = 13,
        Mb016_Samsung = 14,
        Mb032_Samsung = 15,
        Mb064_Samsung = 16,
        Mb128_Samsung = 17, // Mb64_Samsung x 2
        Mb256_Samsung = 18,
        EntireDevice = 19,
        // Intel P30
        P30_Top = 20, // 4x32KB; ?x128KB
        P30_Btm = 21 // ?x128KB; 4x32KB
    }

    public enum EraseMethod
    {
        Standard, // Chip-Erase, then Blank check
        BySector, // Erase each sector (some chips lack Erase All)
        DieErase, // Do a DIE erase for each 32MB die
        Micron // Some Micron devices need either DieErase or Standard
    }

    public enum SPI_QUAD_SUPPORT
    {
        NO_QUAD = 0, // Only SPI (not multi-io capability)
        QUAD = 1, // All commands are data are received in 1/2/4
        SPI_QUAD = 2 // Commands are sent in single, but data is sent/received in multi-io
    }

    public enum VENDOR_FEATURE : int
    {
        NotSupported = -1,
        Micron = 1,
        Spansion_FL = 2,
        ISSI = 3
    }

    public enum SPI_ProgramMode : byte
    {
        PageMode = 0,
        AAI_Byte = 1,
        AAI_Word = 2,
        Atmel45Series = 3,
        SPI_EEPROM = 4,
        Nordic = 5
    }

    public class SPI_CmdDef
    {
        public static byte RDID = 0x9F; // Read Identification
        public static byte REMS = 0x90; // Read Electronic Manufacturer Signature 
        public static byte RES = 0xAB; // Read Electronic Signature
        public byte RSFDP = 0x5A; // Read Serial Flash Discoverable Parameters
        public byte WRSR = 0x1; // Write Status Register
        public byte PROG = 0x2; // Page Program or word program (AAI) command
        public byte READ = 0x3; // Read-data
        public byte WRDI = 0x4; // Write-Disable
        public byte RDSR = 0x5; // Read Status Register
        public byte WREN = 0x6; // Write-Enable
        public byte FAST_READ = 0xB; // FAST READ
        public byte DUAL_READ = 0x3B; // DUAL OUTPUT FAST READ
        public byte QUAD_READ = 0x6B; // QUAD OUTPUT FAST READ
        public byte QUAD_PROG = 0x32; // QUAD INPUT PROGRAM
        public byte DUAL_PROG = 0xA2; // DUAL INPUT PROGRAM
        public byte EWSR = 0x50; // Enable Write Status Register (used by SST/PCT chips) or (Clear Flag Status Register)
        public byte RDFR = 0x70; // Read Flag Status Register
        public byte WRTB = 0x84; // Command to write data into SRAM buffer 1 (used by Atmel)
        public byte WRFB = 0x88; // Command to write data from SRAM buffer 1 into page (used by Atmel)
        public byte DE = 0xC4; // Die Erase
        public byte BE = 0xC7; // Bulk Erase (or chip erase) Sometimes 0x60
        public byte SE = 0xD8; // Erases one sector (or one block)
        public byte AAI_WORD = 0xAD; // Used for PROG when in AAI Word Mode
        public byte AAI_BYTE = 0xAF; // Used for PROG when in AAI Byte Mode
        public byte EN4B = 0xB7; // Enter 4-byte address mode (only used for certain 32-bit SPI devices)
        public byte EX4B = 0xE9; // Exit 4-byte address mode (only used for certain 32-bit SPI devices)
        public byte ULBPR = 0x98; // Global Block Protection Unlock
        public byte DIESEL = 0xC2; // Die-Select (used by flashes with multiple die)
    }

    public interface Device
    {
        string NAME { get; } // Manufacturer and part number
        MemoryType FLASH_TYPE { get; }
        VCC_IF IFACE { get; } // The type of VCC and Interface
        long FLASH_SIZE { get; } // Size of this flash device (without spare area)
        byte MFG_CODE { get; } // The manufaturer byte ID
        ushort ID1 { get; }
        ushort ID2 { get; set; }
        uint PAGE_SIZE { get; } // Size of the pages
        uint Sector_Count { get; } // Total number of blocks or sectors this flash device has
        bool ERASE_REQUIRED { get; set; } // Indicates that the sector/block must be erased prior to writing
    }

    public class OTP_EPROM : Device
    {
        public string NAME { get; private set; }
        public VCC_IF IFACE { get; set; }
        public byte MFG_CODE { get; private set; }
        public ushort ID1 { get; private set; }
        public ushort ID2 { get; set; } = 0; // Not used
        public MemoryType FLASH_TYPE { get; private set; } = MemoryType.OTP_EPROM;
        public long FLASH_SIZE { get; private set; }
        public uint Sector_Count { get; private set; } = 1U; // We will have to write the entire array
        public uint PAGE_SIZE { get; set; } = 0U; // Not used
        public bool ERASE_REQUIRED { get; set; } = false;
        public bool IS_BLANK { get; set; } = false; // On init, do blank check
        public ushort HARDWARE_DELAY { get; set; } = 50; // uS wait after each word program

        public OTP_EPROM(string f_name, VCC_IF vcc, byte MFG, ushort ID1, uint f_size, ushort word_write)
        {
            NAME = f_name;
            IFACE = vcc;
            MFG_CODE = MFG;
            this.ID1 = ID1;
            FLASH_SIZE = f_size;
        }
    }

    public class P_NOR : Device
    {
        public string NAME { get; private set; }
        public VCC_IF IFACE { get; private set; }
        public byte MFG_CODE { get; private set; }
        public ushort ID1 { get; private set; }
        public ushort ID2 { get; set; }
        public MemoryType FLASH_TYPE { get; private set; } = MemoryType.PARALLEL_NOR;
        public long FLASH_SIZE { get; private set; }
        public uint PAGE_SIZE { get; set; } = 32U; // Only used for WRITE_PAGE mode of certain flash devices
        public bool ERASE_REQUIRED { get; set; } = true;
        public MFP_PRG WriteMode { get; set; } = MFP_PRG.Standard; // This indicates the perfered programing method
        public bool RESET_ENABLED { get; set; } = true; // Indicates if we will call reset/read mode op code
        public ushort HARDWARE_DELAY { get; set; } = 10; // Number of hardware uS/mS to wait between write operations
        public ushort SOFTWARE_DELAY { get; set; } = 100; // Number of software ms to wait between write operations
        public ushort ERASE_DELAY { get; set; } = 250; // Number of ms to wait after an erase operation
        public MFP_DELAY DELAY_MODE { get; set; } = MFP_DELAY.uS;
        public bool DUAL_DIE { get; set; } = false; // Package contains two memory die
        public int CE2 { get; set; } // The Axx pin to use for the second CE control

        public P_NOR(string FlashName, byte MFG, ushort ID1, uint Size, VCC_IF f_if, BLKLYT block_layout, MFP_PRG write_mode, MFP_DELAY delay_mode, ushort ID2 = 0)
        {
            NAME = FlashName;
            MFG_CODE = MFG;
            this.ID1 = ID1;
            this.ID2 = ID2;
            FLASH_SIZE = Size;
            IFACE = f_if;
            WriteMode = write_mode;
            DELAY_MODE = delay_mode;
            uint blocks = (uint)(Size / (double)Constants.Kb512);
            switch (block_layout)
            {
                case BLKLYT.Four_Top:
                    {
                        AddSector(Constants.Kb512, (int)(blocks - 1L));
                        AddSector(Constants.Kb256, 1);
                        AddSector(Constants.Kb064, 2);
                        AddSector(Constants.Kb128, 1);
                        break;
                    }

                case BLKLYT.Two_Top:
                    {
                        AddSector(Constants.Kb512, (int)(blocks - 1L));
                        AddSector(Constants.Kb064, 8);
                        break;
                    }

                case BLKLYT.Four_Btm:
                    {
                        AddSector(Constants.Kb128, 1);
                        AddSector(Constants.Kb064, 2);
                        AddSector(Constants.Kb256, 1);
                        AddSector(Constants.Kb512, (int)(blocks - 1L));
                        break;
                    }

                case BLKLYT.Two_Btm:
                    {
                        AddSector(Constants.Kb064, 8);
                        AddSector(Constants.Kb512, (int)(blocks - 1L));
                        break;
                    }

                case BLKLYT.P30_Top:
                    {
                        AddSector(Constants.Mb001, (int)(Size / (double)Constants.Mb001 - 1d));
                        AddSector(Constants.Kb256, 4);
                        break;
                    }

                case BLKLYT.P30_Btm:
                    {
                        AddSector(Constants.Kb256, 4);
                        AddSector(Constants.Mb001, (int)(Size / (double)Constants.Mb001 - 1d));
                        break;
                    }

                case BLKLYT.Dual: // this device has small boot blocks on the top and bottom of the device
                    {
                        AddSector(Constants.Kb064, 8); // bottom block
                        AddSector(Constants.Kb512, (int)(blocks - 2L));
                        AddSector(Constants.Kb064, 8); // top block
                        break;
                    }

                case BLKLYT.Kb016_Uni:
                    {
                        AddUniformSector(Constants.Kb016);
                        break;
                    }

                case BLKLYT.Kb032_Uni:
                    {
                        AddUniformSector(Constants.Kb032);
                        break;
                    }

                case BLKLYT.Kb064_Uni:
                    {
                        AddUniformSector(Constants.Kb064);
                        break;
                    }

                case BLKLYT.Kb128_Uni:
                    {
                        AddUniformSector(Constants.Kb128);
                        break;
                    }

                case BLKLYT.Kb256_Uni:
                    {
                        AddUniformSector(Constants.Kb256);
                        break;
                    }

                case BLKLYT.Kb512_Uni:
                    {
                        AddUniformSector(Constants.Kb512);
                        break;
                    }

                case BLKLYT.Mb001_Uni:
                    {
                        AddUniformSector(Constants.Mb001);
                        break;
                    }

                case BLKLYT.Mb002_NonUni:
                    {
                        AddSector(Constants.Mb001); // Main Block
                        AddSector(98304U); // Main Block
                        AddSector(Constants.Kb064); // Parameter Block
                        AddSector(Constants.Kb064); // Parameter Block
                        AddSector(Constants.Kb128); // Boot Block
                        break;
                    }

                case BLKLYT.Mb032_NonUni:
                    {
                        AddSector(Constants.Kb064, 8);
                        AddSector(Constants.Kb512, 1);
                        AddSector(Constants.Mb001, 31);
                        break;
                    }

                case BLKLYT.Mb016_Samsung:
                    {
                        AddSector(Constants.Kb064, 8); // 8192    65536
                        AddSector(Constants.Kb512, 3); // 65536   196608
                        AddSector(Constants.Mb002, 6); // 262144  7864320
                        AddSector(Constants.Kb512, 3); // 65536   196608
                        AddSector(Constants.Kb064, 8); // 8192    65536
                        break;
                    }

                case BLKLYT.Mb032_Samsung:
                    {
                        AddSector(Constants.Kb064, 8);  // 8192    65536
                        AddSector(Constants.Kb512, 3);  // 65536   196608
                        AddSector(Constants.Mb002, 14); // 262144 
                        AddSector(Constants.Kb512, 3);  // 65536   196608
                        AddSector(Constants.Kb064, 8); // 8192    65536
                        break;
                    }

                case BLKLYT.Mb064_Samsung:
                    {
                        AddSector(Constants.Kb064, 8);  // 8192    65536
                        AddSector(Constants.Kb512, 3);  // 65536   196608
                        AddSector(Constants.Mb002, 30); // 262144  7864320
                        AddSector(Constants.Kb512, 3);  // 65536   196608
                        AddSector(Constants.Kb064, 8); // 8192    65536
                        break;
                    }

                case BLKLYT.Mb128_Samsung:
                    {
                        AddSector(Constants.Kb064, 8);  // 8192    65536
                        AddSector(Constants.Kb512, 3);  // 65536   196608
                        AddSector(Constants.Mb002, 30); // 262144  7864320
                        AddSector(Constants.Kb512, 3);  // 65536   196608
                        AddSector(Constants.Kb064, 8); // 8192    65536
                        AddSector(Constants.Kb064, 8);  // 8192    65536
                        AddSector(Constants.Kb512, 3);  // 65536   196608
                        AddSector(Constants.Mb002, 30); // 262144  7864320
                        AddSector(Constants.Kb512, 3);  // 65536   196608
                        AddSector(Constants.Kb064, 8); // 8192    65536
                        break;
                    }

                case BLKLYT.Mb256_Samsung:
                    {
                        AddSector(Constants.Kb512, 4);   // 65536    262144
                        AddSector(Constants.Mb002, 126); // 262144   33030144
                        AddSector(Constants.Kb512, 4);   // 65536    262144
                        break;
                    }

                case BLKLYT.EntireDevice:
                    {
                        AddSector(Size);
                        break;
                    }
            }
        }

        private List<uint> SectorList = new List<uint>();

        public void AddSector(uint SectorSize)
        {
            SectorList.Add(SectorSize);
        }

        public void AddSector(uint SectorSize, int Count)
        {
            for (int i = 1, loopTo = Count; i <= loopTo; i++)
                SectorList.Add(SectorSize);
        }

        public void AddUniformSector(uint uniform_block)
        {
            uint TotalSectors = (uint)(FLASH_SIZE / (double)uniform_block);
            for (uint i = 1U, loopTo = TotalSectors; i <= loopTo; i++)
                SectorList.Add(uniform_block);
        }

        public int GetSectorSize(int SectorIndex)
        {
            try
            {
                return (int)SectorList[SectorIndex];
            }
            catch
            {
            }

            return -1;
        }

        public uint Sector_Count
        {
            get
            {
                return (uint)SectorList.Count;
            }
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        public override string ToString()
        {
            return NAME;
        }
    }

    public class SPI_NOR : Device
    {
        public string NAME { get; private set; }
        public VCC_IF IFACE { get; private set; }
        public byte MFG_CODE { get; private set; }
        public ushort ID1 { get; private set; }
        public ushort ID2 { get; set; }
        public byte FAMILY { get; set; } // SPI Extended byte
        public MemoryType FLASH_TYPE { get; private set; }
        public long FLASH_SIZE { get; set; }
        public bool ERASE_REQUIRED { get; set; }
        // These are properties unique to SPI devices
        public uint ADDRESSBITS { get; set; } = 24U; // Number of bits the address space takes up (16/24/32)
        public SPI_QUAD_SUPPORT SQI_MODE { get; set; } = SPI_QUAD_SUPPORT.NO_QUAD;
        public SPI_ProgramMode ProgramMode { get; set; }
        public uint STACKED_DIES { get; set; } = 1U; // If device has more than one die, set this value
        public bool SEND_EN4B { get; set; } = false; // Set to True to send the EN4BSP
        public bool SEND_RDFS { get; set; } = false; // Set to True to read the flag status register
        public uint ERASE_SIZE { get; set; } = 0x10000U; // Number of bytes per page that are erased(typically 64KB)
        public VENDOR_FEATURE VENDOR_SPECIFIC { get; set; } = VENDOR_FEATURE.NotSupported; // Indicates we can load a unique vendor specific tab
        public byte SPI_DUMMY { get; set; } = 8; // Number of cycles after read in SPI operation
        public byte SQI_DUMMY { get; set; } = 10; // Number of cycles after read in QUAD SPI operation
        public EraseMethod CHIP_ERASE { get; set; } = EraseMethod.Standard; // How to erase the entire device
        public bool SEND_EWSR { get; set; } = false; // Set to TRUE to write the enable write status-register
        public uint PAGE_SIZE { get; set; } = 256U; // Number of bytes per page
        public uint PAGE_SIZE_EXTENDED { get; set; } // Number of bytes in the extended page

        public SPI_CmdDef OP_COMMANDS = new SPI_CmdDef(); // Contains a list of op-codes used to read/write/erase

        public uint PAGE_COUNT // The total number of pages this flash contains
        {
            get
            {
                if (FLASH_SIZE == 0L | PAGE_SIZE == 0L)
                    return 0U;
                return (uint)(FLASH_SIZE / (double)PAGE_SIZE);
            }
        }

        public SPI_NOR(string name, VCC_IF vcc, uint size, byte MFG, ushort ID1)
        {
            NAME = name;
            IFACE = vcc;
            FLASH_SIZE = size;
            MFG_CODE = MFG;
            this.ID1 = ID1;
            if (size > Constants.Mb128)
                ADDRESSBITS = 32U;
            ERASE_REQUIRED = true;
            FLASH_TYPE = MemoryType.SERIAL_NOR;
        }

        public SPI_NOR(string name, VCC_IF vcc, uint size, byte MFG, ushort ID1, byte ERASECMD, uint ERASESIZE)
        {
            NAME = name;
            IFACE = vcc;
            FLASH_SIZE = size;
            MFG_CODE = MFG;
            this.ID1 = ID1;
            OP_COMMANDS.SE = ERASECMD; // Sometimes 0xD8 or 0x20
            ERASE_SIZE = ERASESIZE;
            if (size > Constants.Mb128)
                ADDRESSBITS = 32U;
            ERASE_REQUIRED = true;
            FLASH_TYPE = MemoryType.SERIAL_NOR;
        }
        // 32-bit setup command
        public SPI_NOR(string name, VCC_IF f_if, uint size, byte MFG, ushort ID1, byte ERASECMD, uint ERASESIZE, byte READCMD, byte FASTCMD, byte WRITECMD)
        {
            NAME = name;
            IFACE = f_if;
            FLASH_SIZE = size;
            PAGE_SIZE = 256U;
            MFG_CODE = MFG;
            this.ID1 = ID1;
            OP_COMMANDS.SE = ERASECMD; // Sometimes 0xD8 or 0x20
            OP_COMMANDS.READ = READCMD;
            OP_COMMANDS.FAST_READ = FASTCMD;
            OP_COMMANDS.PROG = WRITECMD;
            ERASE_SIZE = ERASESIZE;
            if (size > Constants.Mb128)
                ADDRESSBITS = 32U;
            ERASE_REQUIRED = true;
            FLASH_TYPE = MemoryType.SERIAL_NOR;
        }
        // Returns the amounts of bytes needed to indicate device address (usually 3 or 4 bytes)
        public int AddressBytes
        {
            get
            {
                return (int)Math.Ceiling(ADDRESSBITS / 8d);
            }
        }

        public uint Sector_Count
        {
            get
            {
                if (ERASE_REQUIRED)
                {
                    return (uint)(FLASH_SIZE / (double)ERASE_SIZE);
                }
                else
                {
                    return 1U;
                } // EEPROM do not have sectors
            }
        }

        public void Enable_EEPROM_Mode(ushort page_size, uint addr_bits, bool erase_page, SPI_ProgramMode eeprom_mode = SPI_ProgramMode.SPI_EEPROM)
        {
            PAGE_SIZE = page_size;
            ADDRESSBITS = addr_bits;
            ERASE_REQUIRED = erase_page;
            ProgramMode = eeprom_mode;
        }
    }

    public class SPI_NAND : Device
    {
        public string NAME { get; private set; }
        public VCC_IF IFACE { get; private set; }
        public byte MFG_CODE { get; private set; }
        public ushort ID1 { get; private set; }
        public ushort ID2 { get; set; }
        public bool ERASE_REQUIRED { get; set; }
        public long FLASH_SIZE { get; private set; }
        public MemoryType FLASH_TYPE { get; private set; }
        public uint PAGE_SIZE { get; private set; } // The number of bytes in the main area
        public bool PLANE_SELECT { get; private set; } // Indicates that this device needs to select a plane when accessing pages
        public uint STACKED_DIES { get; set; } = 1U; // If device has more than one die, set this value
        public ushort PAGE_EXT { get; private set; } // Extended number of bytes
        public ushort PAGE_COUNT { get; private set; } // Number of pages in a block
        public uint Sector_Count { get; private set; } // Number of blocks in this device

        public uint Block_Size // Returns the total size of the block (including all spare area)
        {
            get
            {
                return PAGE_COUNT * (PAGE_SIZE + PAGE_EXT);
            }
        }

        public bool READ_CMD_DUMMY { get; set; } = false; // Write a dummy byte after read command

        public SPI_NAND(string FlashName, byte MFG, uint ID, uint PageSize, ushort SpareSize, ushort PageCount, ushort BlockCount, bool plane_select, VCC_IF vcc)
        {
            NAME = FlashName;
            MFG_CODE = MFG;
            ID1 = (ushort)(ID & 0xFFFFL);
            ID2 = 0;
            FLASH_TYPE = MemoryType.SERIAL_NAND;
            PAGE_SIZE = PageSize; // Does not include extended / spare pages
            PAGE_EXT = SpareSize;
            PAGE_COUNT = PageCount;
            Sector_Count = BlockCount;
            FLASH_SIZE = PageSize * (long)PageCount * BlockCount; // Does not include extended /spare areas
            PLANE_SELECT = plane_select;
            ERASE_REQUIRED = true;
            IFACE = vcc;
        }
    }

    public class P_NAND : Device
    {
        public string NAME { get; private set; }
        public VCC_IF IFACE { get; private set; }
        public byte MFG_CODE { get; private set; }
        public ushort ID1 { get; private set; }
        public ushort ID2 { get; set; }
        public MemoryType FLASH_TYPE { get; private set; }
        public long FLASH_SIZE { get; private set; }
        public uint PAGE_SIZE { get; private set; } // Number of bytes in a normal page area
        public bool ERASE_REQUIRED { get; set; }
        public uint STACKED_DIES { get; set; } = 1U; // If device has more than one die, set this value
        public ushort PAGE_EXT { get; private set; } // Extended number of bytes
        public ushort PAGE_COUNT { get; private set; } // Number of pages in a block
        public uint Sector_Count { get; private set; } // Number of blocks in this device
        public int ADDRESS_SCHEME { get; set; } = 2; // 1=small page,2=large page,3=SanDisk

        public uint Block_Size // Returns the total size of the block (including all spare area)
        {
            get
            {
                return PAGE_COUNT * (PAGE_SIZE + PAGE_EXT);
            }
        }

        public P_NAND(string FlashName, byte MFG, uint ID, uint PageSize, ushort SpareSize, ushort PageCount, ushort BlockCount, VCC_IF vcc)
        {
            NAME = FlashName;
            MFG_CODE = MFG;
            while ((ID & 0xFF000000U) == 0L)
                ID = ID << 8;
            ID1 = (ushort)(ID >> 16);
            ID2 = (ushort)(ID & 0xFFFFL);
            FLASH_TYPE = MemoryType.PARALLEL_NAND;
            PAGE_SIZE = PageSize; // Does not include extended / spare pages
            PAGE_EXT = SpareSize;
            PAGE_COUNT = PageCount;
            Sector_Count = BlockCount;
            FLASH_SIZE = PageSize * (long)PageCount * BlockCount; // Does not include extended /spare areas
            ERASE_REQUIRED = true;
            IFACE = vcc;
            if (PageSize == 512L)
                ADDRESS_SCHEME = 1;
        }

        public override string ToString()
        {
            return NAME + " (" + Constants.GetDataSizeString(FLASH_SIZE) + ")";
        }
    }

    public class FWH : Device {
        public string NAME { get; private set; }
        public VCC_IF IFACE { get; private set; }
        public byte MFG_CODE { get; private set; }
        public ushort ID1 { get; private set; }
        public ushort ID2 { get; set; }
        public MemoryType FLASH_TYPE { get; private set; } = MemoryType.FWH_NOR;
        public long FLASH_SIZE { get; private set; }
        public uint PAGE_SIZE { get; set; } = 32U; // Only used for WRITE_PAGE mode of certain flash devices
        public bool ERASE_REQUIRED { get; set; } = true;
        public uint SECTOR_SIZE { get; private set; }
        public uint Sector_Count { get; private set; }

        public readonly byte ERASE_CMD;

        public FWH(string f_name, byte MFG, ushort ID1, uint f_size, uint sector_size, byte sector_erase)
        {
            NAME = f_name;
            MFG_CODE = MFG;
            this.ID1 = ID1;
            ID2 = ID2;
            FLASH_SIZE = f_size;
            SECTOR_SIZE = sector_size;
            Sector_Count = (uint)(f_size / (double)sector_size);
            ERASE_CMD = sector_erase;
        }
    }

    public class SWI : Device {
        public string NAME { get; private set; }
        public VCC_IF IFACE { get; private set; }
        public byte MFG_CODE { get; private set; }
        public ushort ID1 { get; private set; }
        public ushort ID2 { get; set; }
        public MemoryType FLASH_TYPE { get; private set; } = MemoryType.SERIAL_SWI;
        public long FLASH_SIZE { get; private set; }
        public uint PAGE_SIZE { get; set; }
        public bool ERASE_REQUIRED { get; set; } = true;
        public uint SECTOR_SIZE { get; private set; }
        public uint Sector_Count { get; private set; }

        public SWI(string device_name, byte MFG, UInt16 ID1, int mem_size, UInt32 page_size) {
            this.NAME = device_name;
            this.MFG_CODE = MFG;
            this.ID1 = ID1;
            this.FLASH_SIZE = mem_size;
            this.PAGE_SIZE = page_size;
            this.ERASE_REQUIRED = false;
            this.Sector_Count = 0;
            this.IFACE = VCC_IF.SERIAL_2V7_5V;
        }

    }

    public class I2C_DEVICE : Device {
        public string NAME { get; private set; }
        public VCC_IF IFACE { get; private set; }
        public byte MFG_CODE { get; private set; }
        public ushort ID1 { get; private set; }
        public ushort ID2 { get; set; }
        public MemoryType FLASH_TYPE { get; private set; } = MemoryType.SERIAL_I2C;
        public long FLASH_SIZE { get; private set; }
        public uint PAGE_SIZE { get; set; }
        public bool ERASE_REQUIRED { get; set; } = true;
        public uint SECTOR_SIZE { get; private set; }
        public uint Sector_Count { get; private set; }

        public int AddressSize { get; }

        public I2C_DEVICE(string DisplayName, uint SizeInBytes, int EEAddrSize, uint EEPageSize) {
            this.NAME = DisplayName;
            this.FLASH_SIZE = SizeInBytes;
            this.AddressSize = EEAddrSize; // Number of bytes that are used to store the address
            this.PAGE_SIZE = EEPageSize;
        }

    }

    public class HYPERFLASH : Device
    {
        public string NAME { get; private set; }
        public VCC_IF IFACE { get; private set; }
        public byte MFG_CODE { get; private set; }
        public ushort ID1 { get; private set; }
        public ushort ID2 { get; set; } // NOT USED
        public MemoryType FLASH_TYPE { get; private set; } = MemoryType.HYPERFLASH;
        public long FLASH_SIZE { get; private set; }
        public uint PAGE_SIZE { get; set; } = 512U; // Only used for WRITE_PAGE mode of certain flash devices
        public uint SECTOR_SIZE { get; private set; }
        public uint Sector_Count { get; private set; }
        public bool ERASE_REQUIRED { get; set; } = true;

        public HYPERFLASH(string F_NAME, byte MFG, ushort ID1, uint f_size)
        {
            NAME = F_NAME;
            MFG_CODE = MFG;
            this.ID1 = ID1;
            ID2 = ID2;
            FLASH_SIZE = f_size;
            SECTOR_SIZE = Constants.Mb002;
            Sector_Count = (uint)(f_size / (double)SECTOR_SIZE);
        }
    }

    public class MICROWIRE : Device
    {
        public string NAME { get; private set; }
        public VCC_IF IFACE { get; private set; }
        public byte MFG_CODE { get; private set; } = 0; // NOT USED
        public ushort ID1 { get; private set; } = 0; // NOT USED 
        public ushort ID2 { get; set; } // NOT USED
        public MemoryType FLASH_TYPE { get; private set; } = MemoryType.SERIAL_MICROWIRE;
        public long FLASH_SIZE { get; private set; }
        public uint PAGE_SIZE { get; set; } = 0U;
        public uint SECTOR_SIZE { get; private set; } = 0U;
        public uint Sector_Count { get; private set; } = 0U;
        public bool ERASE_REQUIRED { get; set; } = false;
        // Microwire specific options
        public byte X8_ADDRSIZE { get; private set; } = 0; // 0=Means not supported
        public byte X16_ADDRSIZE { get; private set; }

        public MICROWIRE(string F_NAME, uint F_SIZE, byte X8_ADDRS, byte X16_ADDRS)
        {
            NAME = F_NAME;
            FLASH_SIZE = F_SIZE;
            X8_ADDRSIZE = X8_ADDRS;
            X16_ADDRSIZE = X16_ADDRS;
        }

        public override string ToString()
        {
            return NAME;
        }
    }

    public class NAND_ONFI
    {
        public bool IS_VALID { get; private set; } = false;
        public string DEVICE_MFG { get; private set; }
        public string DEVICE_MODEL { get; private set; }
        public uint PAGE_SIZE { get; private set; }
        public ushort SPARE_SIZE { get; private set; }
        public uint PAGES_PER_BLOCK { get; private set; }
        public uint BLOCKS_PER_LUN { get; private set; }
        public uint LUN_COUNT { get; private set; } // CE_x
        public int BITS_PER_CELL { get; private set; }

        public NAND_ONFI(byte[] onfi_table)
        {
            try
            {
                if (onfi_table is null || !(onfi_table.Length == 256))
                    return;

                if ((onfi_table[0] == 'O') || (onfi_table[1] == 'N') || (onfi_table[2] == 'F') || (onfi_table[3] == 'I'))
                {
                    DEVICE_MFG = Utilities.Bytes.ToChrString(onfi_table.Slice(32, 12)).Trim();
                    DEVICE_MODEL = Utilities.Bytes.ToChrString(onfi_table.Slice(44, 20)).Trim();
                    PAGE_SIZE = Utilities.Bytes.ToUInt32(onfi_table.Slice(80, 4).Reverse());
                    SPARE_SIZE = Utilities.Bytes.ToUInt16(onfi_table.Slice(84, 2).Reverse());
                    PAGES_PER_BLOCK = Utilities.Bytes.ToUInt32(onfi_table.Slice(92, 4).Reverse());
                    BLOCKS_PER_LUN = Utilities.Bytes.ToUInt32(onfi_table.Slice(96, 4).Reverse());
                    LUN_COUNT = onfi_table[100]; // Indicates how many CE this device has
                    BITS_PER_CELL = onfi_table[102];
                    IS_VALID = true;
                }
            }
            catch
            {
            }
        }
    }

    public class NOR_CFI
    {
        public bool IS_VALID { get; private set; } = false;
        public float VCC_MIN_PROGERASE { get; private set; }
        public float VCC_MAX_PROGERASE { get; private set; }
        public float VPP_MIN_PROGERASE { get; private set; }
        public float VPP_MAX_PROGERASE { get; private set; }
        public int WORD_WRITE_TIMEOUT { get; private set; } // Typical, in uS
        public int BUFFER_WRITE_TIMEOUT { get; private set; } // Typical, in uS
        public int BLOCK_ERASE_TIMEOUT { get; private set; } // Typical, in ms
        public int ERASE_TIMEOUT { get; private set; } // Typical, in ms
        public int WORD_WRITE_MAX_TIMEOUT { get; private set; }
        public int BUFFER_WRITE_MAX_TIMEOUT { get; private set; }
        public int BLOCK_ERASE_MAX_TIMEOUT { get; private set; } // in seconds
        public int ERASE_MAX_TIMEOUT { get; private set; } // in seconds
        public uint DEVICE_SIZE { get; private set; }
        public string DESCRIPTION { get; private set; }
        public int WRITE_BUFFER_SIZE { get; private set; }

        public NOR_CFI(byte[] cfi_table)
        {
            try
            {
                if (cfi_table is null || !(cfi_table.Length == 32))
                    return;
                if ((cfi_table[0]=='Q') || (cfi_table[1] == 'R') || (cfi_table[2] == 'Y')) {
                    VCC_MIN_PROGERASE = Convert.ToSingle((cfi_table[11] >> 4).ToString() + "." + (cfi_table[11] & 15).ToString());
                    VCC_MAX_PROGERASE = Convert.ToSingle((cfi_table[12] >> 4).ToString() + "." + (cfi_table[12] & 15).ToString());
                    VPP_MIN_PROGERASE = Convert.ToSingle((cfi_table[13] >> 4).ToString() + "." + (cfi_table[13] & 15).ToString());
                    VPP_MAX_PROGERASE = Convert.ToSingle((cfi_table[14] >> 4).ToString() + "." + (cfi_table[14] & 15).ToString());
                    WORD_WRITE_TIMEOUT = (int)Math.Pow(2d, cfi_table[15]); // 0x1F
                    BUFFER_WRITE_TIMEOUT = (int)Math.Pow(2d, cfi_table[16]); // 0x20
                    BLOCK_ERASE_TIMEOUT = (int)Math.Pow(2d, cfi_table[17]); // 0x21
                    ERASE_TIMEOUT = (int)Math.Pow(2d, cfi_table[18]); // 0x22
                    WORD_WRITE_MAX_TIMEOUT = (int)(Math.Pow(2d, cfi_table[15]) * Math.Pow(2d, cfi_table[19])); // 0x23
                    BUFFER_WRITE_MAX_TIMEOUT = (int)(Math.Pow(2d, cfi_table[16]) * Math.Pow(2d, cfi_table[20])); // 0x24
                    BLOCK_ERASE_MAX_TIMEOUT = (int)Math.Pow(2d, cfi_table[21]); // 0x25
                    ERASE_MAX_TIMEOUT = (int)Math.Pow(2d, cfi_table[22]); // 0x26
                    DEVICE_SIZE = (uint)Math.Pow(2d, cfi_table[23]); // 0x27
                    var str_opt = new string[] { "X8 ONLY", "X16 ONLY", "X8/X16", "X32", "SPI" };
                    DESCRIPTION = str_opt[cfi_table[24]];
                    WRITE_BUFFER_SIZE = (int)Math.Pow(2d, cfi_table[26]); // 0x2A
                    IS_VALID = true;
                }
            }
            catch
            {
                IS_VALID = false;
            }
        }
    }

    public class FlashDatabase
    {
        public List<Device> FlashDB = new List<Device>();
        private const VCC_IF SPI_1V8 = VCC_IF.SERIAL_1V8;
        private const VCC_IF SPI_2V5 = VCC_IF.SERIAL_2V5;
        private const VCC_IF SPI_3V = VCC_IF.SERIAL_3V;
        private const int QUAD = (int)SPI_QUAD_SUPPORT.QUAD;
        private const int SPI_QUAD = (int)SPI_QUAD_SUPPORT.SPI_QUAD;
        private const byte AAI_Word = (byte)SPI_ProgramMode.AAI_Word;
        private const byte AAI_Byte = (byte)SPI_ProgramMode.AAI_Byte;

        public FlashDatabase()
        {
            SPINOR_Database(); // Adds all of the SPI and QSPI devices
            SPIEEPROM_Database(); // Adds all SPI EEPROMs
            SPINAND_Database(); // Adds all of the SPI NAND devices
            MFP_Database(); // Adds all of the TSOP/PLCC etc. devices
            NAND_Database(); // Adds all of the SLC NAND (x8) compatible devices
            OTP_Database(); // Adds all of the OTP EPROM devices
            FWH_Database(); // Adds all of the firmware hub devices
            MICROWIRE_Database(); // Adds all of the micriwire devices

            // Add device specific features
            SPI_NOR MT25QL02GC = (SPI_NOR)FindDevice(0x20, 0xBA22, 0, MemoryType.SERIAL_NOR);
            MT25QL02GC.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron; // Adds the non-vol tab to the GUI
            MT25QL02GC.SEND_RDFS = true; // Will read the flag-status register after a erase/programer opertion
            MT25QL02GC.CHIP_ERASE = EraseMethod.Micron;   // Will erase all of the sectors instead
            SPI_NOR N25Q00AA_3V = (SPI_NOR)FindDevice(0x20, 0xBA21, 0, MemoryType.SERIAL_NOR);
            N25Q00AA_3V.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron; // Adds the non-vol tab to the GUI
            N25Q00AA_3V.SEND_RDFS = true; // Will read the flag-status register after a erase/programer opertion
            N25Q00AA_3V.CHIP_ERASE = EraseMethod.Micron;  // Will erase all of the sectors instead
            N25Q00AA_3V.STACKED_DIES = 4U;
            SPI_NOR N25Q00AA_1V8 = (SPI_NOR)FindDevice(0x20, 0xBB21, 0, MemoryType.SERIAL_NOR); // CV
            N25Q00AA_1V8.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron; // Adds the non-vol tab to the GUI
            N25Q00AA_1V8.SEND_RDFS = true; // Will read the flag-status register after a erase/programer opertion
            N25Q00AA_1V8.CHIP_ERASE = EraseMethod.Micron;  // Will erase all of the sectors instead
            N25Q00AA_1V8.STACKED_DIES = 4U;
            SPI_NOR N25Q512 = (SPI_NOR)FindDevice(0x20, 0xBA20, 0, MemoryType.SERIAL_NOR);
            N25Q512.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron; // Adds the non-vol tab to the GUI
            N25Q512.SEND_RDFS = true; // Will read the flag-status register after a erase/programer opertion
            N25Q512.CHIP_ERASE = EraseMethod.Micron; // Will erase all of the sectors instead
            SPI_NOR N25Q256 = (SPI_NOR)FindDevice(0x20, 0xBA19, 0, MemoryType.SERIAL_NOR);
            N25Q256.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron; // Adds the non-vol tab to the GUI
            N25Q256.SEND_RDFS = true; // Will read the flag-status register after a erase/programer opertion
            SPI_NOR N25Q256A = (SPI_NOR)FindDevice(0x20, 0xBB19, 0, MemoryType.SERIAL_NOR); // 1.8V version
            N25Q256A.VENDOR_SPECIFIC = VENDOR_FEATURE.Micron; // Adds the non-vol tab to the GUI
            N25Q256A.SEND_RDFS = true; // Will read the flag-status register after a erase/programer opertion
            N25Q256A.OP_COMMANDS.QUAD_PROG = 0x12;
            SPI_NOR S25FL128S = (SPI_NOR)FindDevice(0x1, 0x2018, 0, MemoryType.SERIAL_NOR);
            S25FL128S.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL;
            S25FL128S.SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD;
            SPI_NOR S25FL129P = (SPI_NOR)FindDevice(0x1, 0x2018, 0x4D01, MemoryType.SERIAL_NOR);
            S25FL129P.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL;
            S25FL129P.SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD;
            S25FL129P = (SPI_NOR)FindDevice(0x1, 0x2018, 0x4D00, MemoryType.SERIAL_NOR);
            S25FL129P.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL;
            S25FL129P.SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD;
            SPI_NOR S25FL116K = (SPI_NOR)FindDevice(0x1, 0x4015, 0, MemoryType.SERIAL_NOR);
            S25FL116K.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL;
            S25FL116K.SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD;
            SPI_NOR S25FL132K = (SPI_NOR)FindDevice(0x1, 0x4016, 0, MemoryType.SERIAL_NOR);
            S25FL132K.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL;
            S25FL132K.SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD;
            SPI_NOR S25FL164K = (SPI_NOR)FindDevice(0x1, 0x4017, 0, MemoryType.SERIAL_NOR);
            S25FL164K.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL;
            S25FL164K.SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD;
            SPI_NOR S25FL512S = (SPI_NOR)FindDevice(0x1, 0x220, 0, MemoryType.SERIAL_NOR);
            S25FL512S.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL;
            S25FL512S.SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD;
            S25FL512S.OP_COMMANDS.QUAD_READ = 0x6C; // 4QOR
            S25FL512S.OP_COMMANDS.QUAD_PROG = 0x34; // 4QPP
            SPI_NOR S25FL256S_256KB = (SPI_NOR)FindDevice(0x1, 0x219, 0x4D00, MemoryType.SERIAL_NOR);
            S25FL256S_256KB.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL;
            S25FL256S_256KB.SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD;
            S25FL256S_256KB.OP_COMMANDS.QUAD_READ = 0x6C; // 4QOR
            S25FL256S_256KB.OP_COMMANDS.QUAD_PROG = 0x34; // 4QPP
            SPI_NOR S25FL256S_64KB = (SPI_NOR)FindDevice(0x1, 0x219, 0x4D01, MemoryType.SERIAL_NOR);
            S25FL256S_64KB.VENDOR_SPECIFIC = VENDOR_FEATURE.Spansion_FL;
            S25FL256S_64KB.SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD;
            S25FL256S_64KB.OP_COMMANDS.QUAD_READ = 0x6C; // 4QOR
            S25FL256S_64KB.OP_COMMANDS.QUAD_PROG = 0x34; // 4QPP
            SPI_NOR IS25LQ032 = (SPI_NOR)FindDevice(0x9D, 0x4016, 0, MemoryType.SERIAL_NOR);
            IS25LQ032.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI;
            SPI_NOR IS25LQ016 = (SPI_NOR)FindDevice(0x9D, 0x4015, 0, MemoryType.SERIAL_NOR);
            IS25LQ016.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI;
            SPI_NOR IS25LP080D = (SPI_NOR)FindDevice(0x9D, 0x6014, 0, MemoryType.SERIAL_NOR);
            IS25LP080D.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI;
            SPI_NOR IS25WP080D = (SPI_NOR)FindDevice(0x9D, 0x7014, 0, MemoryType.SERIAL_NOR);
            IS25WP080D.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI;
            SPI_NOR IS25WP040D = (SPI_NOR)FindDevice(0x9D, 0x7013, 0, MemoryType.SERIAL_NOR);
            IS25WP040D.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI;
            SPI_NOR IS25WP020D = (SPI_NOR)FindDevice(0x9D, 0x7012, 0, MemoryType.SERIAL_NOR);
            IS25WP020D.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI;
            SPI_NOR IS25LP256 = (SPI_NOR)FindDevice(0x9D, 0x6019, 0, MemoryType.SERIAL_NOR);
            IS25LP256.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI;
            SPI_NOR IS25WP256 = (SPI_NOR)FindDevice(0x9D, 0x7019, 0, MemoryType.SERIAL_NOR);
            IS25WP256.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI;
            SPI_NOR IS25LP128 = (SPI_NOR)FindDevice(0x9D, 0x6018, 0, MemoryType.SERIAL_NOR);
            IS25LP128.VENDOR_SPECIFIC = VENDOR_FEATURE.ISSI;

            // Add HyperFlash devices
            FlashDB.Add(new HYPERFLASH("Cypress S26KS128S", 0x1, 0x7E74, Constants.Mb128)); // 1.8V
            FlashDB.Add(new HYPERFLASH("Cypress S26KS256S", 0x1, 0x7E72, Constants.Mb256)); // 1.8V
            FlashDB.Add(new HYPERFLASH("Cypress S26KS512S", 0x1, 0x7E70, Constants.Mb512)); // 1.8V
            FlashDB.Add(new HYPERFLASH("Cypress S26KL128S", 0x1, 0x7E73, Constants.Mb128)); // 3.3V
            FlashDB.Add(new HYPERFLASH("Cypress S26KL256S", 0x1, 0x7E71, Constants.Mb256)); // 3.3V
            FlashDB.Add(new HYPERFLASH("Cypress S26KL512S", 0x1, 0x7E6F, Constants.Mb512)); // 3.3V
        }

        private void SPINOR_Database() {
            // Adesto 25/25 Series (formely Atmel)
            FlashDB.Add(CreateSeries45("Adesto AT45DB641E", Constants.Mb064, 0x2800, 0x100, 256U));
            FlashDB.Add(CreateSeries45("Adesto AT45DB642D", Constants.Mb064, 0x2800, 0, 1024U));
            FlashDB.Add(CreateSeries45("Adesto AT45DB321E", Constants.Mb032, 0x2701, 0x100, 512U));
            FlashDB.Add(CreateSeries45("Adesto AT45DB321D", Constants.Mb032, 0x2701, 0, 512U));
            FlashDB.Add(CreateSeries45("Adesto AT45DB161E", Constants.Mb016, 0x2600, 0x100, 512U));
            FlashDB.Add(CreateSeries45("Adesto AT45DB161D", Constants.Mb016, 0x2600, 0, 512U));
            FlashDB.Add(CreateSeries45("Adesto AT45DB081E", Constants.Mb008, 0x2500, 0x100, 256U));
            FlashDB.Add(CreateSeries45("Adesto AT45DB081D", Constants.Mb008, 0x2500, 0, 256U));
            FlashDB.Add(CreateSeries45("Adesto AT45DB041E", Constants.Mb004, 0x2400, 0x100, 256U));
            FlashDB.Add(CreateSeries45("Adesto AT45DB041D", Constants.Mb004, 0x2400, 0, 256U));
            FlashDB.Add(CreateSeries45("Adesto AT45DB021E", Constants.Mb002, 0x2300, 0x100, 256U));
            FlashDB.Add(CreateSeries45("Adesto AT45DB021D", Constants.Mb002, 0x2300, 0, 256U));
            FlashDB.Add(CreateSeries45("Adesto AT45DB011D", Constants.Mb001, 0x2200, 0, 256U));
            FlashDB.Add(new SPI_NOR("Adesto AT25DF641", SPI_3V, Constants.Mb064, 0x1F, 0x4800)); // Confirmed (build 350)
            FlashDB.Add(new SPI_NOR("Adesto AT25DF321S", SPI_3V, Constants.Mb032, 0x1F, 0x4701));
            FlashDB.Add(new SPI_NOR("Adesto AT25DF321", SPI_3V, Constants.Mb032, 0x1F, 0x4700));
            FlashDB.Add(new SPI_NOR("Adesto AT25DF161", SPI_3V, Constants.Mb016, 0x1F, 0x4602));
            FlashDB.Add(new SPI_NOR("Adesto AT25DF081", SPI_3V, Constants.Mb008, 0x1F, 0x4502));
            FlashDB.Add(new SPI_NOR("Adesto AT25DF041", SPI_3V, Constants.Mb004, 0x1F, 0x4402));
            FlashDB.Add(new SPI_NOR("Adesto AT25DF021", SPI_3V, Constants.Mb002, 0x1F, 0x4300));
            FlashDB.Add(new SPI_NOR("Adesto AT26DF321", SPI_3V, Constants.Mb032, 0x1F, 0x4700));
            FlashDB.Add(new SPI_NOR("Adesto AT26DF161", SPI_3V, Constants.Mb016, 0x1F, 0x4600));
            FlashDB.Add(new SPI_NOR("Adesto AT26DF161A", SPI_3V, Constants.Mb016, 0x1F, 0x4601));
            FlashDB.Add(new SPI_NOR("Adesto AT26DF081A", SPI_3V, Constants.Mb008, 0x1F, 0x4501));
            FlashDB.Add(new SPI_NOR("Adesto AT25SF321", SPI_3V, Constants.Mb032, 0x1F, 0x8701));
            FlashDB.Add(new SPI_NOR("Adesto AT25SF161", SPI_3V, Constants.Mb016, 0x1F, 0x8601));
            FlashDB.Add(new SPI_NOR("Adesto AT25SF081", SPI_3V, Constants.Mb008, 0x1F, 0x8501));
            FlashDB.Add(new SPI_NOR("Adesto AT25SF041", SPI_3V, Constants.Mb004, 0x1F, 0x8401));
            FlashDB.Add(new SPI_NOR("Adesto AT25XV041", SPI_3V, Constants.Mb004, 0x1F, 0x4401));
            FlashDB.Add(new SPI_NOR("Adesto AT25XV021", SPI_3V, Constants.Mb002, 0x1F, 0x4301));
            // Adesto (1.8V memories)
            FlashDB.Add(new SPI_NOR("Adesto AT25SL128A", SPI_1V8, Constants.Mb128, 0x1F, 0x4218));
            FlashDB.Add(new SPI_NOR("Adesto AT25SL641", SPI_1V8, Constants.Mb064, 0x1F, 0x4217));
            FlashDB.Add(new SPI_NOR("Adesto AT25SL321", SPI_1V8, Constants.Mb032, 0x1F, 0x4216));
            // Cypress 25FL Series (formely Spansion)
            FlashDB.Add(new SPI_NOR("Cypress S70FL01GS", SPI_3V, Constants.Gb001, 0x1, 0x221, 0xDC, 0x40000U, 0x13, 0xC, 0x12));
            FlashDB.Add(new SPI_NOR("Cypress S25FL512S", SPI_3V, Constants.Mb512, 0x1, 0x220, 0xDC, 0x40000U, 0x13, 0xC, 0x12));
            FlashDB.Add(new SPI_NOR("Cypress S25FL256S", SPI_3V, Constants.Mb256, 0x1, 0x219, 0xDC, 0x40000U, 0x13, 0xC, 0x12) { ID2 = 0x4D00 });
            FlashDB.Add(new SPI_NOR("Cypress S25FL256S", SPI_3V, Constants.Mb256, 0x1, 0x219, 0xDC, 0x10000U, 0x13, 0xC, 0x12) { ID2 = 0x4D01 });
            FlashDB.Add(new SPI_NOR("Cypress FL127S/FL128S", SPI_3V, Constants.Mb128, 0x1, 0x2018) { ERASE_SIZE = Constants.Kb512, ID2 = 0x4D01, FAMILY = 0x80 });
            FlashDB.Add(new SPI_NOR("Cypress S25FL128S", SPI_3V, Constants.Mb128, 0x1, 0x2018) { ID2 = 0x4D00, FAMILY = 0x80, ERASE_SIZE = Constants.Mb002 });
            FlashDB.Add(new SPI_NOR("Cypress S25FL127S", SPI_3V, Constants.Mb128, 0, 0)); // Placeholder for database files
            FlashDB.Add(new SPI_NOR("Cypress S25FS256S", SPI_1V8, Constants.Mb256, 0x1, 0x219) { ID2 = 0x4D00, FAMILY = 0x81, ERASE_SIZE = Constants.Mb002 });
            FlashDB.Add(new SPI_NOR("Cypress S25FS128S", SPI_1V8, Constants.Mb128, 0x1, 0x2018) { ID2 = 0x4D00, FAMILY = 0x81, ERASE_SIZE = Constants.Mb002 });
            FlashDB.Add(new SPI_NOR("Cypress S25FS064S", SPI_1V8, Constants.Mb064, 0x1, 0x217) { ID2 = 0x4D00, FAMILY = 0x81, ERASE_SIZE = Constants.Mb002 });
            FlashDB.Add(new SPI_NOR("Cypress S25FS256S", SPI_1V8, Constants.Mb256, 0x1, 0x219) { ID2 = 0x4D01, FAMILY = 0x81 });
            FlashDB.Add(new SPI_NOR("Cypress S25FS128S", SPI_1V8, Constants.Mb128, 0x1, 0x2018) { ID2 = 0x4D01, FAMILY = 0x81 });
            FlashDB.Add(new SPI_NOR("Cypress S25FS064S", SPI_1V8, Constants.Mb064, 0x1, 0x217) { ID2 = 0x4D01, FAMILY = 0x81 });
            FlashDB.Add(new SPI_NOR("Cypress S25FL256L", SPI_3V, Constants.Mb256, 0x1, 0x6019, 0xDC, 0x10000U, 0x13, 0xC, 0x12));
            FlashDB.Add(new SPI_NOR("Cypress S25FL128L", SPI_3V, Constants.Mb128, 0x1, 0x6018, 0xDC, 0x10000U, 0x13, 0xC, 0x12) { ADDRESSBITS = 32U });
            FlashDB.Add(new SPI_NOR("Cypress S25FL064L", SPI_3V, Constants.Mb064, 0x1, 0x6017, 0xDC, 0x10000U, 0x13, 0xC, 0x12) { ADDRESSBITS = 32U });
            FlashDB.Add(new SPI_NOR("Cypress S70FL256P", SPI_3V, Constants.Mb256, 0, 0)); // Placeholder (uses two S25FL128S, PIN6 is CS2)
            FlashDB.Add(new SPI_NOR("Cypress S25FL128P", SPI_3V, Constants.Mb128, 0x1, 0x2018) { ERASE_SIZE = Constants.Kb512, ID2 = 0x301 }); // 0301h X
            FlashDB.Add(new SPI_NOR("Cypress S25FL128P", SPI_3V, Constants.Mb128, 0x1, 0x2018) { ERASE_SIZE = Constants.Mb002, ID2 = 0x300 }); // 0300h X
            FlashDB.Add(new SPI_NOR("Cypress S25FL129P", SPI_3V, Constants.Mb128, 0x1, 0x2018) { ERASE_SIZE = Constants.Kb512, ID2 = 0x4D01 }); // 4D01h X
            FlashDB.Add(new SPI_NOR("Cypress S25FL129P", SPI_3V, Constants.Mb128, 0x1, 0x2018) { ERASE_SIZE = Constants.Mb002, ID2 = 0x4D00 }); // 4D00h X
            FlashDB.Add(new SPI_NOR("Cypress S25FL064", SPI_3V, Constants.Mb064, 0x1, 0x216));
            FlashDB.Add(new SPI_NOR("Cypress S25FL032", SPI_3V, Constants.Mb032, 0x1, 0x215));
            FlashDB.Add(new SPI_NOR("Cypress S25FL016A", SPI_3V, Constants.Mb016, 0x1, 0x214));
            FlashDB.Add(new SPI_NOR("Cypress S25FL008A", SPI_3V, Constants.Mb008, 0x1, 0x213));
            FlashDB.Add(new SPI_NOR("Cypress S25FL004A", SPI_3V, Constants.Mb004, 0x1, 0x212));
            FlashDB.Add(new SPI_NOR("Cypress S25FL040A", SPI_3V, Constants.Mb004, 0x1, 0x212));
            FlashDB.Add(new SPI_NOR("Cypress S25FL164K", SPI_3V, Constants.Mb064, 0x1, 0x4017));
            FlashDB.Add(new SPI_NOR("Cypress S25FL132K", SPI_3V, Constants.Mb032, 0x1, 0x4016));
            FlashDB.Add(new SPI_NOR("Cypress S25FL216K", SPI_3V, Constants.Mb016, 0x1, 0x4015)); // Uses the same ID as S25FL116K (might support 3 byte ID)
            FlashDB.Add(new SPI_NOR("Cypress S25FL116K", SPI_3V, Constants.Mb016, 0x1, 0x4015));
            FlashDB.Add(new SPI_NOR("Cypress S25FL208K", SPI_3V, Constants.Mb008, 0x1, 0x4014));
            FlashDB.Add(new SPI_NOR("Cypress S25FL204K", SPI_3V, Constants.Mb004, 0x1, 0x4013));
            // Semper Flash (SPI compatible)
            FlashDB.Add(new SPI_NOR("Cypress S25HS256T", SPI_1V8, Constants.Mb256, 0x34, 0x2B19) { SEND_EN4B = true, PAGE_SIZE = 512U, SEND_EWSR = true, ERASE_SIZE = Constants.Mb002 });
            FlashDB.Add(new SPI_NOR("Cypress S25HS512T", SPI_1V8, Constants.Mb512, 0x34, 0x2B1A) { SEND_EN4B = true, PAGE_SIZE = 512U, SEND_EWSR = true, ERASE_SIZE = Constants.Mb002 });
            FlashDB.Add(new SPI_NOR("Cypress S25HS01GT", SPI_1V8, Constants.Gb001, 0x34, 0x2B1B) { SEND_EN4B = true, PAGE_SIZE = 512U, SEND_EWSR = true, ERASE_SIZE = Constants.Mb002 });
            FlashDB.Add(new SPI_NOR("Cypress S25HL256T", SPI_3V, Constants.Mb256, 0x34, 0x2A19) { SEND_EN4B = true, PAGE_SIZE = 512U, SEND_EWSR = true, ERASE_SIZE = Constants.Mb002 });
            FlashDB.Add(new SPI_NOR("Cypress S25HL512T", SPI_3V, Constants.Mb512, 0x34, 0x2A1A) { SEND_EN4B = true, PAGE_SIZE = 512U, SEND_EWSR = true, ERASE_SIZE = Constants.Mb002 });
            FlashDB.Add(new SPI_NOR("Cypress S25HL01GT", SPI_3V, Constants.Gb001, 0x34, 0x2A1B) { SEND_EN4B = true, PAGE_SIZE = 512U, SEND_EWSR = true, ERASE_SIZE = Constants.Mb002 });
            // Semper Flash (SPI/HF compatible)
            FlashDB.Add(new SPI_NOR("Cypress S26HS256T", SPI_1V8, Constants.Mb256, 0x34, 0x6B, 0xDC, Constants.Mb002, 0x3, 0x13, 0x12) { ID2 = 0x19, PAGE_SIZE = 512U });
            FlashDB.Add(new SPI_NOR("Cypress S26HS512T", SPI_1V8, Constants.Mb512, 0x34, 0x6B, 0xDC, Constants.Mb002, 0x3, 0x13, 0x12) { ID2 = 0x1A, PAGE_SIZE = 512U });
            FlashDB.Add(new SPI_NOR("Cypress S26HS01GT", SPI_1V8, Constants.Gb001, 0x34, 0x6B, 0xDC, Constants.Mb002, 0x3, 0x13, 0x12) { ID2 = 0x1B, PAGE_SIZE = 512U });
            FlashDB.Add(new SPI_NOR("Cypress S26HL256T", SPI_3V, Constants.Mb256, 0x34, 0x6A, 0xDC, Constants.Mb002, 0x3, 0x13, 0x12) { ID2 = 0x19, PAGE_SIZE = 512U });
            FlashDB.Add(new SPI_NOR("Cypress S26HL512T", SPI_3V, Constants.Mb512, 0x34, 0x6A, 0xDC, Constants.Mb002, 0x3, 0x13, 0x12) { ID2 = 0x1A, PAGE_SIZE = 512U });
            FlashDB.Add(new SPI_NOR("Cypress S26HL01GT", SPI_3V, Constants.Gb001, 0x34, 0x6A, 0xDC, Constants.Mb002, 0x3, 0x13, 0x12) { ID2 = 0x1B, PAGE_SIZE = 512U });
            // Micron (ST)
            FlashDB.Add(new SPI_NOR("Micron MT25QL02GC", SPI_3V, Constants.Gb002, 0x20, 0xBA22) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Micron N25Q00AA", SPI_3V, Constants.Gb001, 0x20, 0xBA21) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Micron N25Q512A", SPI_3V, Constants.Mb512, 0x20, 0xBA20) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Micron N25Q256A", SPI_3V, Constants.Mb256, 0x20, 0xBA19) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Micron NP5Q128A", SPI_3V, Constants.Mb128, 0x20, 0xDA18) { ERASE_SIZE = 0x20000U, PAGE_SIZE = 64U, SQI_MODE = (SPI_QUAD_SUPPORT)QUAD }); // NEW! PageSize is 64 bytes
            FlashDB.Add(new SPI_NOR("Micron N25Q128", SPI_3V, Constants.Mb128, 0x20, 0xBA18) { SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Micron N25Q064", SPI_3V, Constants.Mb064, 0x20, 0xBA17) { SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Micron N25Q032", SPI_3V, Constants.Mb032, 0x20, 0xBA16) { SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Micron N25Q016", SPI_3V, Constants.Mb016, 0x20, 0xBA15) { SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Micron N25Q008", SPI_3V, Constants.Mb008, 0x20, 0xBA14) { SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Micron N25Q00AA", SPI_1V8, Constants.Gb001, 0x20, 0xBB21) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Micron N25Q512A", SPI_1V8, Constants.Mb512, 0x20, 0xBB20) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Micron N25Q256A", SPI_1V8, Constants.Mb256, 0x20, 0xBB19) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Micron N25Q128A", SPI_1V8, Constants.Mb128, 0x20, 0xBB18) { SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Micron N25Q064A", SPI_1V8, Constants.Mb064, 0x20, 0xBB17) { SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Micron N25Q032", SPI_1V8, Constants.Mb016, 0x20, 0xBB15) { SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Micron N25Q016", SPI_1V8, Constants.Mb016, 0x20, 0xBB15) { SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Micron N25Q008", SPI_1V8, Constants.Mb008, 0x20, 0xBB14) { SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Micron M25P128", SPI_3V, Constants.Mb128, 0x20, 0x2018) { ERASE_SIZE = Constants.Mb002 });
            FlashDB.Add(new SPI_NOR("Micron M25P64", SPI_3V, Constants.Mb064, 0x20, 0x2017));
            FlashDB.Add(new SPI_NOR("Micron M25PX32", SPI_3V, Constants.Mb032, 0x20, 0x7116));
            FlashDB.Add(new SPI_NOR("Micron M25P32", SPI_3V, Constants.Mb032, 0x20, 0x2016));
            FlashDB.Add(new SPI_NOR("Micron M25PX16", SPI_3V, Constants.Mb016, 0x20, 0x7115));
            FlashDB.Add(new SPI_NOR("Micron M25PX16", SPI_3V, Constants.Mb016, 0x20, 0x7315));
            FlashDB.Add(new SPI_NOR("Micron M25P16", SPI_3V, Constants.Mb016, 0x20, 0x2015));
            FlashDB.Add(new SPI_NOR("Micron M25P80", SPI_3V, Constants.Mb008, 0x20, 0x2014));
            FlashDB.Add(new SPI_NOR("Micron M25PX80", SPI_3V, Constants.Mb008, 0x20, 0x7114));
            FlashDB.Add(new SPI_NOR("Micron M25P40", SPI_3V, Constants.Mb004, 0x20, 0x2013));
            FlashDB.Add(new SPI_NOR("Micron M25P20", SPI_3V, Constants.Mb002, 0x20, 0x2012));
            FlashDB.Add(new SPI_NOR("Micron M25P10", SPI_3V, Constants.Mb001, 0x20, 0x2011));
            FlashDB.Add(new SPI_NOR("Micron M25P05", SPI_3V, Constants.Kb512, 0x20, 0x2010));
            FlashDB.Add(new SPI_NOR("Micron M25PX64", SPI_3V, Constants.Mb064, 0x20, 0x7117));
            FlashDB.Add(new SPI_NOR("Micron M25PX32", SPI_3V, Constants.Mb032, 0x20, 0x7116));
            FlashDB.Add(new SPI_NOR("Micron M25PX16", SPI_3V, Constants.Mb016, 0x20, 0x7115));
            FlashDB.Add(new SPI_NOR("Micron M25PE16", SPI_3V, Constants.Mb016, 0x20, 0x8015));
            FlashDB.Add(new SPI_NOR("Micron M25PE80", SPI_3V, Constants.Mb008, 0x20, 0x8014));
            FlashDB.Add(new SPI_NOR("Micron M25PE40", SPI_3V, Constants.Mb004, 0x20, 0x8013));
            FlashDB.Add(new SPI_NOR("Micron M25PE20", SPI_3V, Constants.Mb002, 0x20, 0x8012));
            FlashDB.Add(new SPI_NOR("Micron M25PE10", SPI_3V, Constants.Mb001, 0x20, 0x8011));
            FlashDB.Add(new SPI_NOR("Micron M45PE16", SPI_3V, Constants.Mb016, 0x20, 0x4015));
            FlashDB.Add(new SPI_NOR("Micron M45PE80", SPI_3V, Constants.Mb008, 0x20, 0x4014));
            FlashDB.Add(new SPI_NOR("Micron M45PE40", SPI_3V, Constants.Mb004, 0x20, 0x4013));
            FlashDB.Add(new SPI_NOR("Micron M45PE20", SPI_3V, Constants.Mb002, 0x20, 0x4012));
            FlashDB.Add(new SPI_NOR("Micron M45PE10", SPI_3V, Constants.Mb001, 0x20, 0x4011));
            // Windbond
            FlashDB.Add(new SPI_NOR("Winbond W25M512JV", SPI_3V, Constants.Mb512, 0xEF, 0x7119) { SEND_EN4B = true, STACKED_DIES = 2U });
            FlashDB.Add(new SPI_NOR("Winbond W25M512JW", SPI_1V8, Constants.Mb512, 0xEF, 0x6119) { SEND_EN4B = true, STACKED_DIES = 2U });
            FlashDB.Add(new SPI_NOR("Winbond W25H02NW", SPI_1V8, Constants.Gb002, 0xEF, 0xA022) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25H01NW", SPI_1V8, Constants.Gb001, 0xEF, 0xA021) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25H512NW", SPI_1V8, Constants.Mb512, 0xEF, 0xA020) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25H02JV", SPI_3V, Constants.Gb002, 0xEF, 0x9022) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25H01JV", SPI_3V, Constants.Gb001, 0xEF, 0x9021) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25H512JV", SPI_3V, Constants.Mb512, 0xEF, 0x9020) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q02NW", SPI_1V8, Constants.Gb002, 0xEF, 0x8022) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q01NW", SPI_1V8, Constants.Gb001, 0xEF, 0x8021) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q512NW", SPI_1V8, Constants.Mb512, 0xEF, 0x8020) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q01NW", SPI_1V8, Constants.Gb001, 0xEF, 0x6021) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q512NW", SPI_1V8, Constants.Mb512, 0xEF, 0x6020) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q256FV", SPI_3V, Constants.Mb256, 0xEF, 0x6019) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q02JV", SPI_3V, Constants.Gb002, 0xEF, 0x7022) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q01JV", SPI_3V, Constants.Gb001, 0xEF, 0x7021) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q512JV", SPI_3V, Constants.Mb512, 0xEF, 0x7020) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q256JV", SPI_3V, Constants.Mb256, 0xEF, 0x7019) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q128JV", SPI_3V, Constants.Mb128, 0xEF, 0x7018) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q64JV", SPI_3V, Constants.Mb064, 0xEF, 0x7017) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q32JV", SPI_3V, Constants.Mb032, 0xEF, 0x7016) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q01", SPI_3V, Constants.Mb256, 0xEF, 0x4021) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q512", SPI_3V, Constants.Mb256, 0xEF, 0x4020) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q256", SPI_3V, Constants.Mb256, 0xEF, 0x4019) { SEND_EN4B = true, SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q128", SPI_3V, Constants.Mb128, 0xEF, 0x4018) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q64", SPI_3V, Constants.Mb064, 0xEF, 0x4017) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q32", SPI_3V, Constants.Mb032, 0xEF, 0x4016) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q16", SPI_3V, Constants.Mb016, 0xEF, 0x4015) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q16JV-DTR", SPI_3V, Constants.Mb016, 0xEF, 0x7015) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD }); // CV (3x2mm)
            FlashDB.Add(new SPI_NOR("Winbond W25Q80", SPI_3V, Constants.Mb008, 0xEF, 0x4014) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q40", SPI_3V, Constants.Mb004, 0xEF, 0x4013) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25X64", SPI_3V, Constants.Mb064, 0xEF, 0x3017));
            FlashDB.Add(new SPI_NOR("Winbond W25X32", SPI_3V, Constants.Mb032, 0xEF, 0x3016));
            FlashDB.Add(new SPI_NOR("Winbond W25X16", SPI_3V, Constants.Mb016, 0xEF, 0x3015));
            FlashDB.Add(new SPI_NOR("Winbond W25X80", SPI_3V, Constants.Mb008, 0xEF, 0x3014));
            FlashDB.Add(new SPI_NOR("Winbond W25X40", SPI_3V, Constants.Mb004, 0xEF, 0x3013));
            FlashDB.Add(new SPI_NOR("Winbond W25X20", SPI_3V, Constants.Mb002, 0xEF, 0x3012));
            FlashDB.Add(new SPI_NOR("Winbond W25X10", SPI_3V, Constants.Mb002, 0xEF, 0x3011));
            FlashDB.Add(new SPI_NOR("Winbond W25X05", SPI_3V, Constants.Kb512, 0xEF, 0x3010));
            FlashDB.Add(new SPI_NOR("Winbond W25M121AV", SPI_3V, 0U, 0, 0)); // Contains a NOR die and NAND die
            FlashDB.Add(new SPI_NOR("Winbond W25Q256FW", SPI_1V8, Constants.Mb256, 0xEF, 0x6019) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q128FW", SPI_1V8, Constants.Mb128, 0xEF, 0x6018) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q64FW", SPI_1V8, Constants.Mb064, 0xEF, 0x6017) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q32FW", SPI_1V8, Constants.Mb032, 0xEF, 0x6016) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q16FW", SPI_1V8, Constants.Mb016, 0xEF, 0x6015) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q80EW", SPI_1V8, Constants.Mb008, 0xEF, 0x6014) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q40EW", SPI_1V8, Constants.Mb004, 0xEF, 0x6013) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q20EW", SPI_1V8, Constants.Mb002, 0xEF, 0x6012) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q80BW", SPI_1V8, Constants.Mb008, 0xEF, 0x5014) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q40BW", SPI_1V8, Constants.Mb004, 0xEF, 0x5013) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Winbond W25Q20BW", SPI_1V8, Constants.Mb002, 0xEF, 0x5012) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            // MXIC
            FlashDB.Add(new SPI_NOR("MXIC MX66L1G45G", SPI_3V, Constants.Gb001, 0xC2, 0x201B) { SEND_EN4B = true });
            FlashDB.Add(new SPI_NOR("MXIC MX25L51245G", SPI_3V, Constants.Mb512, 0xC2, 0x201A) { SEND_EN4B = true });
            FlashDB.Add(new SPI_NOR("MXIC MX25L25655E", SPI_3V, Constants.Mb256, 0xC2, 0x2619) { SEND_EN4B = true });
            FlashDB.Add(new SPI_NOR("MXIC MX25L256", SPI_3V, Constants.Mb256, 0xC2, 0x2019) { SEND_EN4B = true });
            FlashDB.Add(new SPI_NOR("MXIC MX25L12855E", SPI_3V, Constants.Mb128, 0xC2, 0x2618));
            FlashDB.Add(new SPI_NOR("MXIC MX25L128", SPI_3V, Constants.Mb128, 0xC2, 0x2018));
            FlashDB.Add(new SPI_NOR("MXIC MX25L6455E", SPI_3V, Constants.Mb064, 0xC2, 0x2617));
            FlashDB.Add(new SPI_NOR("MXIC MX25L640", SPI_3V, Constants.Mb064, 0xC2, 0x2017));
            FlashDB.Add(new SPI_NOR("MXIC MX25L320", SPI_3V, Constants.Mb032, 0xC2, 0x2016)); 
            FlashDB.Add(new SPI_NOR("MXIC MX25L3205D", SPI_3V, Constants.Mb032, 0xC2, 0x20FF));
            FlashDB.Add(new SPI_NOR("MXIC MX25L323", SPI_3V, Constants.Mb032, 0xC2, 0x5E16));
            FlashDB.Add(new SPI_NOR("MXIC MX25L3255E", SPI_3V, Constants.Mb032, 0xC2, 0x9E16));
            FlashDB.Add(new SPI_NOR("MXIC MX25L1633E", SPI_3V, Constants.Mb016, 0xC2, 0x2415));
            FlashDB.Add(new SPI_NOR("MXIC MX25L160", SPI_3V, Constants.Mb016, 0xC2, 0x2015));
            FlashDB.Add(new SPI_NOR("MXIC MX25L80", SPI_3V, Constants.Mb008, 0xC2, 0x2014));
            FlashDB.Add(new SPI_NOR("MXIC MX25L40", SPI_3V, Constants.Mb004, 0xC2, 0x2013));
            FlashDB.Add(new SPI_NOR("MXIC MX25L20", SPI_3V, Constants.Mb002, 0xC2, 0x2012));
            FlashDB.Add(new SPI_NOR("MXIC MX25L10", SPI_3V, Constants.Mb001, 0xC2, 0x2011));
            FlashDB.Add(new SPI_NOR("MXIC MX25L512", SPI_3V, Constants.Kb512, 0xC2, 0x2010));
            FlashDB.Add(new SPI_NOR("MXIC MX25L1021E", SPI_3V, Constants.Mb001, 0xC2, 0x2211));
            FlashDB.Add(new SPI_NOR("MXIC MX25L5121E", SPI_3V, Constants.Kb512, 0xC2, 0x2210));
            FlashDB.Add(new SPI_NOR("MXIC MX66L51235F", SPI_3V, Constants.Mb512, 0xC2, 0x201A) { SEND_EN4B = true });
            FlashDB.Add(new SPI_NOR("MXIC MX25V8035", SPI_2V5, Constants.Mb008, 0xC2, 0x2554));
            FlashDB.Add(new SPI_NOR("MXIC MX25V4035", SPI_2V5, Constants.Mb004, 0xC2, 0x2553));
            FlashDB.Add(new SPI_NOR("MXIC MX25V8035F", SPI_2V5, Constants.Mb008, 0xC2, 0x2314));
            FlashDB.Add(new SPI_NOR("MXIC MX25R6435", SPI_3V, Constants.Mb064, 0xC2, 0x2817)); // Wide range: 1.65 to 3.5V
            FlashDB.Add(new SPI_NOR("MXIC MX25R3235F", SPI_3V, Constants.Mb032, 0xC2, 0x2816)); // Wide range: 1.65 to 3.5V
            FlashDB.Add(new SPI_NOR("MXIC MX25R8035F", SPI_3V, Constants.Mb008, 0xC2, 0x2814)); // Wide range: 1.65 to 3.5V
            FlashDB.Add(new SPI_NOR("MXIC MX25L3235E", SPI_3V, Constants.Mb032, 0, 0)); // Place holder
            FlashDB.Add(new SPI_NOR("MXIC MX25L2005", SPI_3V, Constants.Mb032, 0, 0)); // Place holder
            FlashDB.Add(new SPI_NOR("MXIC MX25L2006E", SPI_3V, Constants.Mb032, 0, 0)); // Place holder
            FlashDB.Add(new SPI_NOR("MXIC MX25L2026E", SPI_3V, Constants.Mb032, 0, 0)); // Place holder
            FlashDB.Add(new SPI_NOR("MXIC MX25L51245G", SPI_3V, Constants.Mb032, 0, 0)); // Place holder
            // MXIC (1.8V)
            FlashDB.Add(new SPI_NOR("MXIC MX25UM51345G", SPI_1V8, Constants.Mb512, 0xC2, 0x813A) { SEND_EN4B = true });
            FlashDB.Add(new SPI_NOR("MXIC MX25U25645G", SPI_1V8, Constants.Mb256, 0xC2, 0x2539) { SEND_EN4B = true });
            FlashDB.Add(new SPI_NOR("MXIC MX25U12873F", SPI_1V8, Constants.Mb128, 0xC2, 0x2538));
            FlashDB.Add(new SPI_NOR("MXIC MX25U643", SPI_1V8, Constants.Mb064, 0xC2, 0x2537));
            FlashDB.Add(new SPI_NOR("MXIC MX25U323", SPI_1V8, Constants.Mb032, 0xC2, 0x2536));
            FlashDB.Add(new SPI_NOR("MXIC MX25U3235F", SPI_1V8, Constants.Mb032, 0xC2, 0x2536));
            FlashDB.Add(new SPI_NOR("MXIC MX25U1635E", SPI_1V8, Constants.Mb016, 0xC2, 0x2535));
            FlashDB.Add(new SPI_NOR("MXIC MX25U803", SPI_1V8, Constants.Mb008, 0xC2, 0x2534));
            // EON
            FlashDB.Add(new SPI_NOR("EON EN25Q128", SPI_3V, Constants.Mb128, 0x1C, 0x3018) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("EON EN25Q64", SPI_3V, Constants.Mb064, 0x1C, 0x3017) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("EON EN25Q32", SPI_3V, Constants.Mb032, 0x1C, 0x3016) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("EON EN25Q16", SPI_3V, Constants.Mb016, 0x1C, 0x3015) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("EON EN25Q80", SPI_3V, Constants.Mb008, 0x1C, 0x3014) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("EON EN25Q40", SPI_3V, Constants.Mb004, 0x1C, 0x3013) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("EON EN25QH128", SPI_3V, Constants.Mb128, 0x1C, 0x7018) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("EON EN25QH64", SPI_3V, Constants.Mb064, 0x1C, 0x7017) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("EON EN25QH32", SPI_3V, Constants.Mb032, 0x1C, 0x7016) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("EON EN25QH16", SPI_3V, Constants.Mb016, 0x1C, 0x7015) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("EON EN25QH80", SPI_3V, Constants.Mb008, 0x1C, 0x7014) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("EON EN25P64", SPI_3V, Constants.Mb064, 0x1C, 0x2017));
            FlashDB.Add(new SPI_NOR("EON EN25P32", SPI_3V, Constants.Mb032, 0x1C, 0x2016));
            FlashDB.Add(new SPI_NOR("EON EN25P16", SPI_3V, Constants.Mb016, 0x1C, 0x2015));
            FlashDB.Add(new SPI_NOR("EON EN25F32", SPI_3V, Constants.Mb032, 0x1C, 0x3116));
            FlashDB.Add(new SPI_NOR("EON EN25F16", SPI_3V, Constants.Mb016, 0x1C, 0x3115));
            FlashDB.Add(new SPI_NOR("EON EN25F80", SPI_3V, Constants.Mb008, 0x1C, 0x3114));
            FlashDB.Add(new SPI_NOR("EON EN25F40", SPI_3V, Constants.Mb004, 0x1C, 0x3113));
            FlashDB.Add(new SPI_NOR("EON EN25F20", SPI_3V, Constants.Mb002, 0x1C, 0x3112));
            FlashDB.Add(new SPI_NOR("EON EN25T32", SPI_3V, Constants.Mb032, 0x1C, 0x5116));
            FlashDB.Add(new SPI_NOR("EON EN25T16", SPI_3V, Constants.Mb016, 0x1C, 0x5115));
            FlashDB.Add(new SPI_NOR("EON EN25T80", SPI_3V, Constants.Mb008, 0x1C, 0x5114));
            FlashDB.Add(new SPI_NOR("EON EN25T40", SPI_3V, Constants.Mb004, 0x1C, 0x5113));
            FlashDB.Add(new SPI_NOR("EON EN25T20", SPI_3V, Constants.Mb002, 0x1C, 0x5112));
            FlashDB.Add(new SPI_NOR("EON EN25F10", SPI_3V, Constants.Mb001, 0x1C, 0x3111));
            FlashDB.Add(new SPI_NOR("EON EN25S64", SPI_1V8, Constants.Mb064, 0x1C, 0x3817));
            FlashDB.Add(new SPI_NOR("EON EN25S32", SPI_1V8, Constants.Mb032, 0x1C, 0x3816));
            FlashDB.Add(new SPI_NOR("EON EN25S16", SPI_1V8, Constants.Mb016, 0x1C, 0x3815));
            FlashDB.Add(new SPI_NOR("EON EN25S80", SPI_1V8, Constants.Mb008, 0x1C, 0x3814));
            FlashDB.Add(new SPI_NOR("EON EN25S40", SPI_1V8, Constants.Mb004, 0x1C, 0x3813));
            FlashDB.Add(new SPI_NOR("EON EN25S20", SPI_1V8, Constants.Mb002, 0x1C, 0x3812));
            FlashDB.Add(new SPI_NOR("EON EN25S10", SPI_1V8, Constants.Mb001, 0x1C, 0x3811));
            // Microchip / Silicon Storage Technology (SST) / PCT Group (Rebranded)
            FlashDB.Add(new SPI_NOR("Microchip SST26VF064", SPI_3V, Constants.Mb064, 0xBF, 0x2603));
            FlashDB.Add(new SPI_NOR("Microchip SST26VF064B", SPI_3V, Constants.Mb064, 0xBF, 0x2643)); // SST26VF064BA
            FlashDB.Add(new SPI_NOR("Microchip SST26VF032", SPI_3V, Constants.Mb032, 0xBF, 0x2602)); // PCT26VF032
            FlashDB.Add(new SPI_NOR("Microchip SST26VF032", SPI_3V, Constants.Mb032, 0xBF, 0x2602, 0x20, 0x1000U) { SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Microchip SST26VF032B", SPI_3V, Constants.Mb032, 0xBF, 0x2642, 0x20, 0x1000U)); // SST26VF032BA
            FlashDB.Add(new SPI_NOR("Microchip SST26VF016", SPI_3V, Constants.Mb016, 0xBF, 0x2601, 0x20, 0x1000U) { SQI_MODE = (SPI_QUAD_SUPPORT)QUAD });
            FlashDB.Add(new SPI_NOR("Microchip SST26VF016", SPI_3V, Constants.Mb016, 0xBF, 0x16BF, 0x20, 0x1000U) { ProgramMode = (SPI_ProgramMode)AAI_Byte });
            FlashDB.Add(new SPI_NOR("Microchip SST26VF016B", SPI_3V, Constants.Mb016, 0xBF, 0x2641, 0x20, 0x1000U)); // SST26VF016BA
            FlashDB.Add(new SPI_NOR("Microchip SST25VF128B", SPI_3V, Constants.Mb128, 0xBF, 0x2544) { SEND_EWSR = true }); // Might use AAI
            FlashDB.Add(new SPI_NOR("Microchip SST25VF064C", SPI_3V, Constants.Mb064, 0xBF, 0x254B) { SEND_EWSR = true }); // PCT25VF064C
            FlashDB.Add(new SPI_NOR("Microchip SST25VF032", SPI_3V, Constants.Mb032, 0xBF, 0x2542) { ProgramMode = (SPI_ProgramMode)AAI_Word, SEND_EWSR = true });
            FlashDB.Add(new SPI_NOR("Microchip SST25VF032B", SPI_3V, Constants.Mb032, 0xBF, 0x254A) { ProgramMode = (SPI_ProgramMode)AAI_Word, SEND_EWSR = true }); // PCT25VF032B
            FlashDB.Add(new SPI_NOR("Microchip SST25VF016B", SPI_3V, Constants.Mb016, 0xBF, 0x2541) { ProgramMode = (SPI_ProgramMode)AAI_Word, SEND_EWSR = true });
            FlashDB.Add(new SPI_NOR("Microchip SST25VF080", SPI_3V, Constants.Mb008, 0xBF, 0x80BF, 0x20, 0x1000U) { ProgramMode = (SPI_ProgramMode)AAI_Byte, SEND_EWSR = true });
            FlashDB.Add(new SPI_NOR("Microchip SST25VF080B", SPI_3V, Constants.Mb008, 0xBF, 0x258E, 0x20, 0x1000U) { ProgramMode = (SPI_ProgramMode)AAI_Word, SEND_EWSR = true }); // PCT25VF080B - Confirmed (Build 350)
            FlashDB.Add(new SPI_NOR("Microchip SST25VF040B", SPI_3V, Constants.Mb004, 0xBF, 0x258D, 0x20, 0x1000U) { ProgramMode = (SPI_ProgramMode)AAI_Word, SEND_EWSR = true }); // PCT25VF040B <--testing
            FlashDB.Add(new SPI_NOR("Microchip SST25VF020", SPI_3V, Constants.Mb002, 0xBF, 0x258C, 0x20, 0x1000U) { ProgramMode = (SPI_ProgramMode)AAI_Word, SEND_EWSR = true }); // SST25VF020B SST25PF020B PCT25VF020B
            FlashDB.Add(new SPI_NOR("Microchip SST25VF020A", SPI_3V, Constants.Mb002, 0xBF, 0x43, 0x20, 0x1000U) { ProgramMode = (SPI_ProgramMode)AAI_Byte, SEND_EWSR = true }); // Confirmed (Build 350)
            FlashDB.Add(new SPI_NOR("Microchip SST25VF010", SPI_3V, Constants.Mb001, 0xBF, 0x49BF, 0x20, 0x1000U) { ProgramMode = (SPI_ProgramMode)AAI_Byte, SEND_EWSR = true }); // SST25VF010A PCT25VF010A
            FlashDB.Add(new SPI_NOR("Microchip SST25VF010A", SPI_3V, Constants.Mb001, 0xBF, 0x49, 0x20, 0x1000U) { ProgramMode = (SPI_ProgramMode)AAI_Byte, SEND_EWSR = true }); // Confirmed (Build 350)
            FlashDB.Add(new SPI_NOR("Microchip SST25VF512", SPI_3V, Constants.Kb512, 0xBF, 0x48, 0x20, 0x1000U) { ProgramMode = (SPI_ProgramMode)AAI_Byte, SEND_EWSR = true }); // SST25VF512A PCT25VF512A REMS ONLY
            FlashDB.Add(new SPI_NOR("Microchip SST25PF040C", SPI_3V, Constants.Mb004, 0x62, 0x613));
            FlashDB.Add(new SPI_NOR("Microchip SST25LF020A", SPI_3V, Constants.Mb002, 0xBF, 0x43BF, 0x20, 0x1000U) { ProgramMode = (SPI_ProgramMode)AAI_Byte, SEND_EWSR = true });
            FlashDB.Add(new SPI_NOR("Microchip SST26WF064", SPI_1V8, Constants.Mb064, 0xBF, 0x2643));
            FlashDB.Add(new SPI_NOR("Microchip SST26WF032", SPI_1V8, Constants.Mb032, 0xBF, 0x2622)); // PCT26WF032
            FlashDB.Add(new SPI_NOR("Microchip SST26WF016", SPI_1V8, Constants.Mb016, 0xBF, 0x2651)); // SST26WF016
            FlashDB.Add(new SPI_NOR("Microchip SST26WF080", SPI_1V8, Constants.Mb008, 0xBF, 0x2658, 0x20, 0x1000U));
            FlashDB.Add(new SPI_NOR("Microchip SST26WF040", SPI_1V8, Constants.Mb004, 0xBF, 0x2654, 0x20, 0x1000U));
            FlashDB.Add(new SPI_NOR("Microchip SST25WF080B", SPI_1V8, Constants.Mb008, 0x62, 0x1614, 0x20, 0x1000U) { SEND_EWSR = true });
            FlashDB.Add(new SPI_NOR("Microchip SST25WF040", SPI_1V8, Constants.Mb004, 0xBF, 0x2504, 0x20, 0x1000U) { ProgramMode = (SPI_ProgramMode)AAI_Word, SEND_EWSR = true });
            FlashDB.Add(new SPI_NOR("Microchip SST25WF020A", SPI_1V8, Constants.Mb002, 0x62, 0x1612, 0x20, 0x1000U) { SEND_EWSR = true });
            FlashDB.Add(new SPI_NOR("Microchip SST25WF040B", SPI_1V8, Constants.Mb004, 0x62, 0x1613, 0x20, 0x1000U) { SEND_EWSR = true });
            FlashDB.Add(new SPI_NOR("Microchip SST25WF020", SPI_1V8, Constants.Mb002, 0xBF, 0x2503, 0x20, 0x1000U) { ProgramMode = (SPI_ProgramMode)AAI_Word, SEND_EWSR = true });
            FlashDB.Add(new SPI_NOR("Microchip SST25WF010", SPI_1V8, Constants.Mb001, 0xBF, 0x2502, 0x20, 0x1000U) { ProgramMode = (SPI_ProgramMode)AAI_Word, SEND_EWSR = true });
            FlashDB.Add(new SPI_NOR("Microchip SST25WF512", SPI_1V8, Constants.Kb512, 0xBF, 0x2501, 0x20, 0x1000U) { ProgramMode = (SPI_ProgramMode)AAI_Word, SEND_EWSR = true });
            // PMC
            FlashDB.Add(new SPI_NOR("PMC PM25LV016B", SPI_3V, Constants.Mb016, 0x7F, 0x9D14));
            FlashDB.Add(new SPI_NOR("PMC PM25LV080B", SPI_3V, Constants.Mb008, 0x7F, 0x9D13));
            FlashDB.Add(new SPI_NOR("PMC PM25LV040", SPI_3V, Constants.Mb004, 0x9D, 0x7E7F));
            FlashDB.Add(new SPI_NOR("PMC PM25LV020", SPI_3V, Constants.Mb002, 0x9D, 0x7D7F));
            FlashDB.Add(new SPI_NOR("PMC PM25LV010", SPI_3V, Constants.Mb001, 0x9D, 0x7C7F));
            FlashDB.Add(new SPI_NOR("PMC PM25LV512", SPI_3V, Constants.Kb512, 0x9D, 0x7B7F));
            FlashDB.Add(new SPI_NOR("PMC PM25LD020", SPI_3V, Constants.Mb002, 0x7F, 0x9D22));
            FlashDB.Add(new SPI_NOR("PMC PM25LD010", SPI_3V, Constants.Mb001, 0x7F, 0x9D21));
            FlashDB.Add(new SPI_NOR("PMC PM25LD512", SPI_3V, Constants.Kb512, 0x7F, 0x9D20));
            // AMIC
            FlashDB.Add(new SPI_NOR("AMIC A25LQ64", SPI_3V, Constants.Mb064, 0x37, 0x4017) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD }); // A25LMQ64
            FlashDB.Add(new SPI_NOR("AMIC A25LQ32A", SPI_3V, Constants.Mb032, 0x37, 0x4016) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("AMIC A25L032", SPI_3V, Constants.Mb032, 0x37, 0x3016));
            FlashDB.Add(new SPI_NOR("AMIC A25L016", SPI_3V, Constants.Mb016, 0x37, 0x3015));
            FlashDB.Add(new SPI_NOR("AMIC A25LQ16", SPI_3V, Constants.Mb016, 0x37, 0x4015) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("AMIC A25L080", SPI_3V, Constants.Mb008, 0x37, 0x3014));
            FlashDB.Add(new SPI_NOR("AMIC A25L040", SPI_3V, Constants.Mb004, 0x37, 0x3013)); // A25L040A A25P040
            FlashDB.Add(new SPI_NOR("AMIC A25L020", SPI_3V, Constants.Mb002, 0x37, 0x3012)); // A25L020C A25P020
            FlashDB.Add(new SPI_NOR("AMIC A25L010", SPI_3V, Constants.Mb001, 0x37, 0x3011)); // A25L010A A25P010
            FlashDB.Add(new SPI_NOR("AMIC A25L512", SPI_3V, Constants.Kb512, 0x37, 0x3010)); // A25L512A A25P512
            FlashDB.Add(new SPI_NOR("AMIC A25LS512A", SPI_3V, Constants.Kb512, 0xC2, 0x2010));
            // Dosilicon (Formaly Fidelix)
            FlashDB.Add(new SPI_NOR("Dosilicon FM25Q128", SPI_3V, Constants.Mb032, 0xA1, 0x4018) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Dosilicon FM25Q64A", SPI_3V, Constants.Mb032, 0xF8, 0x3217) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Dosilicon FM25Q32A", SPI_3V, Constants.Mb032, 0xF8, 0x3216) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("Dosilicon FM25Q16A", SPI_3V, Constants.Mb016, 0xF8, 0x3215) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD }); // FM25Q16B
            FlashDB.Add(new SPI_NOR("Dosilicon FM25Q08", SPI_3V, Constants.Mb008, 0xF8, 0x3214));
            FlashDB.Add(new SPI_NOR("Dosilicon FM25Q04", SPI_3V, Constants.Mb004, 0xA1, 0x4013));
            FlashDB.Add(new SPI_NOR("Dosilicon FM25Q02", SPI_3V, Constants.Mb002, 0xA1, 0x4012));
            FlashDB.Add(new SPI_NOR("Dosilicon FM25M04A", SPI_3V, Constants.Mb004, 0xF8, 0x4213));
            FlashDB.Add(new SPI_NOR("Dosilicon FM25M08A", SPI_3V, Constants.Mb008, 0xF8, 0x4214));
            FlashDB.Add(new SPI_NOR("Dosilicon FM25M16A", SPI_3V, Constants.Mb016, 0xF8, 0x4215));
            FlashDB.Add(new SPI_NOR("Dosilicon FM25M32A", SPI_3V, Constants.Mb032, 0xF8, 0x4216));
            FlashDB.Add(new SPI_NOR("Dosilicon FM25M64A", SPI_3V, Constants.Mb064, 0xF8, 0x4217));
            FlashDB.Add(new SPI_NOR("Dosilicon FM25M4AA", SPI_3V, Constants.Mb004, 0xF8, 0x4212));
            FlashDB.Add(new SPI_NOR("Dosilicon DS25M4BA", SPI_1V8, Constants.Mb004, 0xE5, 0x4212));
            // Gigadevice
            FlashDB.Add(new SPI_NOR("GigaDevice GD25Q256", SPI_3V, Constants.Mb256, 0xC8, 0x4019) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD, SEND_EN4B = true });
            FlashDB.Add(new SPI_NOR("GigaDevice GD25Q128", SPI_3V, Constants.Mb128, 0xC8, 0x4018) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("GigaDevice GD25Q64", SPI_3V, Constants.Mb064, 0xC8, 0x4017) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("GigaDevice GD25Q32", SPI_3V, Constants.Mb032, 0xC8, 0x4016) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("GigaDevice GD25Q16", SPI_3V, Constants.Mb016, 0xC8, 0x4015) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("GigaDevice GD25Q80", SPI_3V, Constants.Mb008, 0xC8, 0x4014) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("GigaDevice GD25Q40", SPI_3V, Constants.Mb004, 0xC8, 0x4013) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("GigaDevice GD25Q20", SPI_3V, Constants.Mb002, 0xC8, 0x4012) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("GigaDevice GD25Q10", SPI_3V, Constants.Mb001, 0xC8, 0x4011) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("GigaDevice GD25Q512", SPI_3V, Constants.Kb512, 0xC8, 0x4010) { SQI_MODE = (SPI_QUAD_SUPPORT)SPI_QUAD });
            FlashDB.Add(new SPI_NOR("GigaDevice GD25VQ16C", SPI_3V, Constants.Mb016, 0xC8, 0x4215));
            FlashDB.Add(new SPI_NOR("GigaDevice GD25VQ80C", SPI_3V, Constants.Mb008, 0xC8, 0x4214));
            FlashDB.Add(new SPI_NOR("GigaDevice GD25VQ41B", SPI_3V, Constants.Mb004, 0xC8, 0x4213));
            FlashDB.Add(new SPI_NOR("GigaDevice GD25VQ21B", SPI_3V, Constants.Mb002, 0xC8, 0x4212));
            FlashDB.Add(new SPI_NOR("GigaDevice MD25D16SIG", SPI_3V, Constants.Mb016, 0x51, 0x4015));
            FlashDB.Add(new SPI_NOR("GigaDevice GD25LQ128", SPI_1V8, Constants.Mb128, 0xC8, 0x6018));
            FlashDB.Add(new SPI_NOR("GigaDevice GD25LQ64", SPI_1V8, Constants.Mb064, 0xC8, 0x6017));
            FlashDB.Add(new SPI_NOR("GigaDevice GD25LQ32", SPI_1V8, Constants.Mb032, 0xC8, 0x6016));
            FlashDB.Add(new SPI_NOR("GigaDevice GD25LQ16", SPI_1V8, Constants.Mb016, 0xC8, 0x6015));
            FlashDB.Add(new SPI_NOR("GigaDevice GD25LQ80", SPI_1V8, Constants.Mb008, 0xC8, 0x6014));
            FlashDB.Add(new SPI_NOR("GigaDevice GD25LQ40", SPI_1V8, Constants.Mb004, 0xC8, 0x6013));
            FlashDB.Add(new SPI_NOR("GigaDevice GD25LQ20", SPI_1V8, Constants.Mb002, 0xC8, 0x6012));
            FlashDB.Add(new SPI_NOR("GigaDevice GD25LQ10", SPI_1V8, Constants.Mb001, 0xC8, 0x6011));
            // ISSI
            FlashDB.Add(new SPI_NOR("ISSI IS25LP512", SPI_3V, Constants.Mb512, 0x9D, 0x601A) { SEND_EN4B = true });
            FlashDB.Add(new SPI_NOR("ISSI IS25LP256", SPI_3V, Constants.Mb256, 0x9D, 0x6019) { SEND_EN4B = true }); // CV
            FlashDB.Add(new SPI_NOR("ISSI IS25LP128", SPI_3V, Constants.Mb128, 0x9D, 0x6018));
            FlashDB.Add(new SPI_NOR("ISSI IS25LP064", SPI_3V, Constants.Mb064, 0x9D, 0x6017));
            FlashDB.Add(new SPI_NOR("ISSI IS25LP032", SPI_3V, Constants.Mb032, 0x9D, 0x6016));
            FlashDB.Add(new SPI_NOR("ISSI IS25LP016", SPI_3V, Constants.Mb016, 0x9D, 0x6015));
            FlashDB.Add(new SPI_NOR("ISSI IS25LP080", SPI_3V, Constants.Mb008, 0x9D, 0x6014));
            FlashDB.Add(new SPI_NOR("ISSI IS25CD020", SPI_3V, Constants.Mb002, 0x9D, 0x1122));
            FlashDB.Add(new SPI_NOR("ISSI IS25CD010", SPI_3V, Constants.Mb001, 0x9D, 0x1021));
            FlashDB.Add(new SPI_NOR("ISSI IS25CD512", SPI_3V, Constants.Kb512, 0x9D, 0x520));
            FlashDB.Add(new SPI_NOR("ISSI IS25CD025", SPI_3V, Constants.Kb256, 0x7F, 0x9D2F));
            FlashDB.Add(new SPI_NOR("ISSI IS25CQ032", SPI_3V, Constants.Mb032, 0x7F, 0x9D46));
            FlashDB.Add(new SPI_NOR("ISSI IS25LQ032", SPI_3V, Constants.Mb032, 0x9D, 0x4016));
            FlashDB.Add(new SPI_NOR("ISSI IS25LQ016", SPI_3V, Constants.Mb016, 0x9D, 0x4015));
            FlashDB.Add(new SPI_NOR("ISSI IS25LQ080", SPI_3V, Constants.Mb008, 0x9D, 0x1344));
            FlashDB.Add(new SPI_NOR("ISSI IS25LQ040B", SPI_3V, Constants.Mb004, 0x9D, 0x4013));
            FlashDB.Add(new SPI_NOR("ISSI IS25LQ040", SPI_3V, Constants.Mb004, 0x7F, 0x9D43));
            FlashDB.Add(new SPI_NOR("ISSI IS25LQ020", SPI_3V, Constants.Mb002, 0x7F, 0x9D42));
            FlashDB.Add(new SPI_NOR("ISSI IS25LQ020", SPI_3V, Constants.Mb002, 0x9D, 0x4012));
            FlashDB.Add(new SPI_NOR("ISSI IS25LQ010", SPI_3V, Constants.Mb001, 0x9D, 0x4011));
            FlashDB.Add(new SPI_NOR("ISSI IS25LQ512", SPI_3V, Constants.Kb512, 0x9D, 0x4010));
            FlashDB.Add(new SPI_NOR("ISSI IS25LQ025", SPI_3V, Constants.Kb256, 0x9D, 0x4009));
            FlashDB.Add(new SPI_NOR("ISSI IS25LD040", SPI_3V, Constants.Mb004, 0x7F, 0x9D7E));
            FlashDB.Add(new SPI_NOR("ISSI IS25WP256", SPI_1V8, Constants.Mb256, 0x9D, 0x7019));
            FlashDB.Add(new SPI_NOR("ISSI IS25WP128", SPI_1V8, Constants.Mb128, 0x9D, 0x7018));
            FlashDB.Add(new SPI_NOR("ISSI IS25WP064", SPI_1V8, Constants.Mb064, 0x9D, 0x7017));
            FlashDB.Add(new SPI_NOR("ISSI IS25WP032", SPI_1V8, Constants.Mb032, 0x9D, 0x7016));
            FlashDB.Add(new SPI_NOR("ISSI IS25WP016", SPI_1V8, Constants.Mb016, 0x9D, 0x7015));
            FlashDB.Add(new SPI_NOR("ISSI IS25WP080", SPI_1V8, Constants.Mb008, 0x9D, 0x7014));
            FlashDB.Add(new SPI_NOR("ISSI IS25WP040", SPI_1V8, Constants.Mb004, 0x9D, 0x7013));
            FlashDB.Add(new SPI_NOR("ISSI IS25WP020", SPI_1V8, Constants.Mb002, 0x9D, 0x7012));
            FlashDB.Add(new SPI_NOR("ISSI IS25WQ040", SPI_1V8, Constants.Mb004, 0x9D, 0x1253));
            FlashDB.Add(new SPI_NOR("ISSI IS25WQ020", SPI_1V8, Constants.Mb002, 0x9D, 0x1152));
            FlashDB.Add(new SPI_NOR("ISSI IS25WD040", SPI_1V8, Constants.Mb004, 0x7F, 0x9D33));
            FlashDB.Add(new SPI_NOR("ISSI IS25WD020", SPI_1V8, Constants.Mb002, 0x7F, 0x9D32));
            // ESMT
            FlashDB.Add(new SPI_NOR("ESMT F25L64QA", SPI_3V, Constants.Mb032, 0x8C, 0x4117));
            FlashDB.Add(new SPI_NOR("ESMT F25L32QA", SPI_3V, Constants.Mb032, 0x8C, 0x4116));
            FlashDB.Add(new SPI_NOR("ESMT F25L16QA", SPI_3V, Constants.Mb032, 0x8C, 0x4115));
            FlashDB.Add(new SPI_NOR("ESMT F25L14QA", SPI_3V, Constants.Mb032, 0x8C, 0x4114));
            FlashDB.Add(new SPI_NOR("ESMT F25L08", SPI_3V, Constants.Mb008, 0x8C, 0x2014) { ProgramMode = (SPI_ProgramMode)AAI_Word });
            FlashDB.Add(new SPI_NOR("ESMT F25L08", SPI_3V, Constants.Mb008, 0x8C, 0x13) { ProgramMode = (SPI_ProgramMode)AAI_Word }); // REMS only
            FlashDB.Add(new SPI_NOR("ESMT F25L04", SPI_3V, Constants.Mb004, 0x8C, 0x2013) { ProgramMode = (SPI_ProgramMode)AAI_Word });
            FlashDB.Add(new SPI_NOR("ESMT F25L04", SPI_3V, Constants.Mb004, 0x8C, 0x12) { ProgramMode = (SPI_ProgramMode)AAI_Word }); // REMS only
            FlashDB.Add(new SPI_NOR("ESMT F25L64PA", SPI_3V, Constants.Mb016, 0x8C, 0x2017));
            FlashDB.Add(new SPI_NOR("ESMT F25L32PA", SPI_3V, Constants.Mb016, 0x8C, 0x2016));
            FlashDB.Add(new SPI_NOR("ESMT F25L16PA", SPI_3V, Constants.Mb016, 0x8C, 0x2015));
            FlashDB.Add(new SPI_NOR("ESMT F25L08PA", SPI_3V, Constants.Mb008, 0x8C, 0x3014));
            FlashDB.Add(new SPI_NOR("ESMT F25L04PA", SPI_3V, Constants.Mb004, 0x8C, 0x3013));
            FlashDB.Add(new SPI_NOR("ESMT F25L02PA", SPI_3V, Constants.Mb002, 0x8C, 0x3012));
            // Others
            FlashDB.Add(new SPI_NOR("Sanyo LE25FU406B", SPI_3V, Constants.Mb004, 0x62, 0x1E62));
            FlashDB.Add(new SPI_NOR("Sanyo LE25FW406A", SPI_3V, Constants.Mb004, 0x62, 0x1A62));
            FlashDB.Add(new SPI_NOR("Berg_Micro BG25Q32A", SPI_3V, Constants.Mb032, 0xE0, 0x4016));
            FlashDB.Add(new SPI_NOR("XMC XM25QH32B", SPI_3V, Constants.Mb032, 0x20, 0x4016)); // Rebranded-micron
            FlashDB.Add(new SPI_NOR("XMC XM25QH64A", SPI_3V, Constants.Mb064, 0x20, 0x7017)); // Rebranded-micron
            FlashDB.Add(new SPI_NOR("XMC XM25QH128A", SPI_3V, Constants.Mb128, 0x20, 0x7018));
            FlashDB.Add(new SPI_NOR("BOYAMICRO BY25D16", SPI_3V, Constants.Mb016, 0x68, 0x4015));
            FlashDB.Add(new SPI_NOR("BOYAMICRO BY25Q32", SPI_3V, Constants.Mb032, 0x68, 0x4016));
            FlashDB.Add(new SPI_NOR("BOYAMICRO BY25Q64", SPI_3V, Constants.Mb064, 0x68, 0x4017));
            FlashDB.Add(new SPI_NOR("BOYAMICRO BY25Q128A", SPI_3V, Constants.Mb128, 0x68, 0x4018));
            FlashDB.Add(new SPI_NOR("PUYA P25Q32H", SPI_3V, Constants.Mb032, 0x85, 0x6016));
            FlashDB.Add(new SPI_NOR("PUYA P25Q16H", SPI_3V, Constants.Mb016, 0x85, 0x6015));
            FlashDB.Add(new SPI_NOR("PUYA P25Q80H", SPI_3V, Constants.Mb008, 0x85, 0x6014));
            FlashDB.Add(new SPI_NOR("PUYA P25D16H", SPI_3V, Constants.Mb016, 0x85, 0x6015));
            FlashDB.Add(new SPI_NOR("PUYA P25D80H", SPI_3V, Constants.Mb008, 0x85, 0x6014));
            FlashDB.Add(new SPI_NOR("PUYA P25D40H", SPI_3V, Constants.Mb004, 0x85, 0x6013));
            FlashDB.Add(new SPI_NOR("PUYA P25D20H", SPI_3V, Constants.Mb002, 0x85, 0x6012));
            FlashDB.Add(new SPI_NOR("PUYA P25D10H", SPI_3V, Constants.Mb001, 0x85, 0x6011));
            FlashDB.Add(new SPI_NOR("PUYA P25D05H", SPI_3V, Constants.Kb512, 0x85, 0x6010));
            // SUPPORTED EEPROM SPI DEVICES:
            FlashDB.Add(new SPI_NOR("Atmel AT25128B", SPI_3V, 16384U, 0, 0) { PAGE_SIZE = 64U }); // Same as AT25128A
            FlashDB.Add(new SPI_NOR("Atmel AT25256B", SPI_3V, 32768U, 0, 0) { PAGE_SIZE = 64U }); // Same as AT25256A
            FlashDB.Add(new SPI_NOR("Atmel AT25512", SPI_3V, 65536U, 0, 0) { PAGE_SIZE = 128U });
            FlashDB.Add(new SPI_NOR("ST M95010", SPI_3V, 128U, 0, 0) { PAGE_SIZE = 16U });
            FlashDB.Add(new SPI_NOR("ST M95020", SPI_3V, 256U, 0, 0) { PAGE_SIZE = 16U });
            FlashDB.Add(new SPI_NOR("ST M95040", SPI_3V, 512U, 0, 0) { PAGE_SIZE = 16U });
            FlashDB.Add(new SPI_NOR("ST M95080", SPI_3V, 1024U, 0, 0) { PAGE_SIZE = 32U });
            FlashDB.Add(new SPI_NOR("ST M95160", SPI_3V, 2048U, 0, 0) { PAGE_SIZE = 32U });
            FlashDB.Add(new SPI_NOR("ST M95320", SPI_3V, 4096U, 0, 0) { PAGE_SIZE = 32U });
            FlashDB.Add(new SPI_NOR("ST M95640", SPI_3V, 8192U, 0, 0) { PAGE_SIZE = 32U });
            FlashDB.Add(new SPI_NOR("ST M95128", SPI_3V, 16384U, 0, 0) { PAGE_SIZE = 64U });
            FlashDB.Add(new SPI_NOR("ST M95256", SPI_3V, 32768U, 0, 0) { PAGE_SIZE = 64U });
            FlashDB.Add(new SPI_NOR("ST M95512", SPI_3V, 65536U, 0, 0) { PAGE_SIZE = 128U });
            FlashDB.Add(new SPI_NOR("ST M95M01", SPI_3V, 131072U, 0, 0) { PAGE_SIZE = 256U });
            FlashDB.Add(new SPI_NOR("ST M95M02", SPI_3V, 262144U, 0, 0) { PAGE_SIZE = 256U });
            FlashDB.Add(new SPI_NOR("Atmel AT25010A", SPI_3V, 128U, 0, 0) { PAGE_SIZE = 8U });
            FlashDB.Add(new SPI_NOR("Atmel AT25020A", SPI_3V, 256U, 0, 0) { PAGE_SIZE = 8U });
            FlashDB.Add(new SPI_NOR("Atmel AT25040A", SPI_3V, 512U, 0, 0) { PAGE_SIZE = 8U });
            FlashDB.Add(new SPI_NOR("Atmel AT25080", SPI_3V, 1024U, 0, 0) { PAGE_SIZE = 32U });
            FlashDB.Add(new SPI_NOR("Atmel AT25160", SPI_3V, 2048U, 0, 0) { PAGE_SIZE = 32U });
            FlashDB.Add(new SPI_NOR("Atmel AT25320", SPI_3V, 4096U, 0, 0) { PAGE_SIZE = 32U });
            FlashDB.Add(new SPI_NOR("Atmel AT25640", SPI_3V, 8192U, 0, 0) { PAGE_SIZE = 32U });
            FlashDB.Add(new SPI_NOR("Microchip 25AA160A", SPI_3V, 2048U, 0, 0) { PAGE_SIZE = 16U });
            FlashDB.Add(new SPI_NOR("Microchip 25AA160B", SPI_3V, 2048U, 0, 0) { PAGE_SIZE = 32U });
            FlashDB.Add(new SPI_NOR("Microchip 25LC1024", SPI_3V, 131072U, 0, 0) { PAGE_SIZE = 256U, ERASE_SIZE = 0x8000U }); // ID 0x29
            FlashDB.Add(new SPI_NOR("XICOR X25650", SPI_3V, 8192U, 0, 0) { PAGE_SIZE = 32U });
        }

        private void SPINAND_Database() {
            FlashDB.Add(new SPI_NAND("Micron MT29F1G01ABA", 0x2C, 0x14U, 2048U, 128, 64, 1024, false, SPI_3V)); // 1Gb
            FlashDB.Add(new SPI_NAND("Micron MT29F1G01ABB", 0x2C, 0x15U, 2048U, 128, 64, 1024, false, SPI_1V8)); // 1Gb
            FlashDB.Add(new SPI_NAND("Micron MT29F2G01AAA", 0x2C, 0x22U, 2048U, 128, 64, 2048, true, SPI_3V)); // 2Gb
            FlashDB.Add(new SPI_NAND("Micron MT29F2G01ABA", 0x2C, 0x24U, 2048U, 128, 64, 2048, true, SPI_3V)); // 2Gb
            FlashDB.Add(new SPI_NAND("Micron MT29F2G01ABB", 0x2C, 0x25U, 2048U, 128, 64, 2048, true, SPI_1V8)); // 2Gb
            FlashDB.Add(new SPI_NAND("Micron MT29F4G01ADA", 0x2C, 0x36U, 2048U, 128, 64, 4096, true, SPI_3V)); // 4Gb
            FlashDB.Add(new SPI_NAND("Micron MT29F4G01AAA", 0x2C, 0x32U, 2048U, 128, 64, 4096, true, SPI_3V)); // 4Gb
            // GigaDevice
            FlashDB.Add(new SPI_NAND("GigaDevice GD5F1GQ4UB", 0xC8, 0xD1U, 2048U, 128, 64, 1024, false, SPI_3V)); // 1Gb
            FlashDB.Add(new SPI_NAND("GigaDevice GD5F1GQ4RB", 0xC8, 0xC1U, 2048U, 128, 64, 1024, false, SPI_1V8)); // 1Gb
            FlashDB.Add(new SPI_NAND("GigaDevice GD5F2GQ4UB", 0xC8, 0xD2U, 2048U, 128, 64, 2048, false, SPI_3V)); // 2Gb
            FlashDB.Add(new SPI_NAND("GigaDevice GD5F2GQ4RB", 0xC8, 0xC2U, 2048U, 128, 64, 2048, false, SPI_1V8)); // 2Gb
            FlashDB.Add(new SPI_NAND("GigaDevice GD5F4GQ4UA", 0xC8, 0xF4U, 2048U, 64, 64, 4096, false, SPI_3V)); // 4Gb
            FlashDB.Add(new SPI_NAND("GigaDevice GD5F4GQ4UB", 0xC8, 0xD4U, 4096U, 256, 64, 2048, false, SPI_3V)); // 4Gb
            FlashDB.Add(new SPI_NAND("GigaDevice GD5F4GQ4RB", 0xC8, 0xC4U, 4096U, 256, 64, 2048, false, SPI_1V8)); // 4Gb
            FlashDB.Add(new SPI_NAND("GigaDevice GD5F1GQ4UC", 0xC8, 0xB148U, 2048U, 128, 64, 1024, false, SPI_3V) { READ_CMD_DUMMY = true }); // 1Gb
            FlashDB.Add(new SPI_NAND("GigaDevice GD5F1GQ4RC", 0xC8, 0xA148U, 2048U, 128, 64, 1024, false, SPI_1V8) { READ_CMD_DUMMY = true }); // 1Gb
            FlashDB.Add(new SPI_NAND("GigaDevice GD5F2GQ4UC", 0xC8, 0xB248U, 2048U, 128, 64, 2048, false, SPI_3V) { READ_CMD_DUMMY = true }); // 2Gb
            FlashDB.Add(new SPI_NAND("GigaDevice GD5F2GQ4RC", 0xC8, 0xA248U, 2048U, 128, 64, 2048, false, SPI_1V8) { READ_CMD_DUMMY = true }); // 2Gb
            FlashDB.Add(new SPI_NAND("GigaDevice GD5F4GQ4UC", 0xC8, 0xB468U, 4096U, 256, 64, 2048, false, SPI_3V) { READ_CMD_DUMMY = true }); // 4Gb
            FlashDB.Add(new SPI_NAND("GigaDevice GD5F4GQ4RC", 0xC8, 0xA468U, 4096U, 256, 64, 2048, false, SPI_1V8) { READ_CMD_DUMMY = true }); // 4Gb
            // Winbond SPI-NAND 3V
            FlashDB.Add(new SPI_NAND("Winbond W25N512GV", 0xEF, 0xAA20U, 2048U, 64, 64, 512, false, SPI_3V)); // 512Mb
            FlashDB.Add(new SPI_NAND("Winbond W25N01GV", 0xEF, 0xAA21U, 2048U, 64, 64, 1024, false, SPI_3V)); // 1Gb
            FlashDB.Add(new SPI_NAND("Winbond W25M02GV", 0xEF, 0xAB21U, 2048U, 64, 64, 2048, false, SPI_3V) { STACKED_DIES = 2U }); // 2Gb
            FlashDB.Add(new SPI_NAND("Winbond W25N02KV", 0xEF, 0xAA22, 2048, 128, 64, 2048, false, SPI_3V)); //2Gb

            // Winbond SPI-NAND 1.8V
            FlashDB.Add(new SPI_NAND("Winbond W25N512GW", 0xEF, 0xBA20U, 2048U, 64, 64, 512, false, SPI_1V8)); // 512Mb
            FlashDB.Add(new SPI_NAND("Winbond W25N01GW", 0xEF, 0xBA21U, 2048U, 64, 64, 1024, false, SPI_1V8)); // 1Gb
            FlashDB.Add(new SPI_NAND("Winbond W25M02GW", 0xEF, 0xBB21U, 2048U, 64, 64, 2048, false, SPI_1V8) { STACKED_DIES = 2U }); // 2Gb
            // Toshiba SPI-NAND 3V
            FlashDB.Add(new SPI_NAND("Toshiba TC58CVG0S3", 0x98, 0xC2U, 2048U, 128, 64, 1024, false, SPI_3V)); // 1Gb
            FlashDB.Add(new SPI_NAND("Toshiba TC58CVG1S3", 0x98, 0xCBU, 2048U, 128, 64, 2048, false, SPI_3V)); // 2Gb
            FlashDB.Add(new SPI_NAND("Toshiba TC58CVG2S0", 0x98, 0xCDU, 4096U, 256, 64, 2048, false, SPI_3V)); // 4Gb
            // Toshiba SPI-NAND 1.8V
            FlashDB.Add(new SPI_NAND("Toshiba TC58CYG0S3", 0x98, 0xB2U, 2048U, 128, 64, 1024, false, SPI_1V8)); // 1Gb
            FlashDB.Add(new SPI_NAND("Toshiba TC58CYG1S3", 0x98, 0xBBU, 2048U, 128, 64, 2048, false, SPI_1V8)); // 2Gb
            FlashDB.Add(new SPI_NAND("Toshiba TC58CYG2S0", 0x98, 0xBDU, 4096U, 256, 64, 2048, false, SPI_1V8)); // 4Gb
            // Kioxia (Subsiduary of Toshiba) - 2nd gen. SPI-NAND
            FlashDB.Add(new SPI_NAND("Kioxia TC58CVG0S3HRAIJ", 0x98, 0xE240U, 2048U, 128, 64, 1024, false, SPI_3V)); // 1Gb
            FlashDB.Add(new SPI_NAND("Kioxia TC58CVG1S3HRAIJ", 0x98, 0xEB40U, 2048U, 128, 64, 2048, false, SPI_3V)); // 2Gb
            FlashDB.Add(new SPI_NAND("Kioxia TC58CVG2S0HRAIJ", 0x98, 0xED51U, 4096U, 256, 64, 2048, false, SPI_3V)); // 4Gb
            FlashDB.Add(new SPI_NAND("Kioxia TH58CVG3S0HRAIJ", 0x98, 0xE451U, 4096U, 256, 64, 4096, false, SPI_3V)); // 8Gb
            FlashDB.Add(new SPI_NAND("Kioxia TC58CYG0S3HRAIJ", 0x98, 0xD240U, 2048U, 128, 64, 1024, false, SPI_1V8)); // 1Gb
            FlashDB.Add(new SPI_NAND("Kioxia TC58CYG1S3HRAIJ", 0x98, 0xDB40U, 2048U, 128, 64, 2048, false, SPI_1V8)); // 2Gb
            FlashDB.Add(new SPI_NAND("Kioxia TC58CYG2S0HRAIJ", 0x98, 0xDD51U, 4096U, 256, 64, 2048, false, SPI_1V8)); // 4Gb
            FlashDB.Add(new SPI_NAND("Kioxia TH58CYG3S0HRAIJ", 0x98, 0xD451U, 4096U, 256, 64, 4096, false, SPI_1V8)); // 8Gb
            // XTX
            FlashDB.Add(new SPI_NAND("XTX PN26Q01AWSIUG", 0xA1, 0xC1U, 2048U, 128, 64, 1024, false, SPI_1V8)); // 1Gb
            FlashDB.Add(new SPI_NAND("XTX PN26Q02AWSIUG", 0xA1, 0xC2U, 2048U, 128, 64, 2048, false, SPI_1V8)); // 2Gb
            FlashDB.Add(new SPI_NAND("XTX PN26G01AWSIUG", 0xA1, 0xE1U, 2048U, 128, 64, 1024, false, SPI_3V)); // 1Gb
            FlashDB.Add(new SPI_NAND("XTX XT26G01AWSEGA", 0xB, 0xE1U, 2048U, 128, 64, 1024, false, SPI_3V)); // 1Gb
            FlashDB.Add(new SPI_NAND("XTX XT26G02AWSEGA", 0xB, 0xE2U, 2048U, 128, 64, 2048, false, SPI_3V)); // 2Gb
            FlashDB.Add(new SPI_NAND("XTX XT26G01BWSEGA", 0xB, 0xF1U, 2048U, 128, 64, 1024, false, SPI_3V)); // 1Gb
            FlashDB.Add(new SPI_NAND("XTX XT26G02BWSIGA", 0xB, 0xF2U, 2048U, 128, 64, 2048, false, SPI_3V)); // 2Gb
            // Others
            FlashDB.Add(new SPI_NAND("MXIC MX35LF1GE4AB", 0xC2, 0x12U, 2048U, 64, 64, 1024, false, SPI_3V)); // 1Gb
            FlashDB.Add(new SPI_NAND("MXIC MX35LF2GE4AB", 0xC2, 0x22U, 2048U, 64, 64, 2048, true, SPI_3V)); // 2Gb
            FlashDB.Add(new SPI_NAND("ISSI IS37/38SML01G1", 0xC8, 0x21U, 2048U, 64, 64, 1024, false, SPI_3V)); // 1Gb
            FlashDB.Add(new SPI_NAND("ESMT F50L1G41A", 0xC8, 0x217FU, 2048U, 64, 64, 1024, false, SPI_3V)); // 1Gb
            FlashDB.Add(new SPI_NAND("FMSH FM25G01", 0xA1, 0xF1U, 2048U, 64, 64, 1024, false, SPI_3V)); // 1Gb Shanghai Fudan Microelectronics
            FlashDB.Add(new SPI_NAND("FMSH FM25G02", 0xA1, 0xF2U, 2048U, 64, 64, 2048, false, SPI_3V)); // 2Gb Shanghai Fudan Microelectronics
        }

        private void MFP_Database()
        {
            // Intel
            FlashDB.Add(new P_NOR("Intel 28F064P30(T)", 0x89, 0x8817, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Buffer1, MFP_DELAY.SR1) { PAGE_SIZE = 64U });
            FlashDB.Add(new P_NOR("Intel 28F064P30(B)", 0x89, 0x881B, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Buffer1, MFP_DELAY.SR1) { PAGE_SIZE = 64U });
            FlashDB.Add(new P_NOR("Intel 28F128P30(T)", 0x89, 0x8818, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Buffer1, MFP_DELAY.SR1) { PAGE_SIZE = 64U });
            FlashDB.Add(new P_NOR("Intel 28F128P30(B)", 0x89, 0x881B, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Buffer1, MFP_DELAY.SR1) { PAGE_SIZE = 64U });
            FlashDB.Add(new P_NOR("Intel 28F256P30(T)", 0x89, 0x8919, Constants.Mb256, VCC_IF.X16_3V, BLKLYT.P30_Top, MFP_PRG.Buffer1, MFP_DELAY.SR1) { PAGE_SIZE = 64U });
            FlashDB.Add(new P_NOR("Intel 28F256P30(B)", 0x89, 0x891C, Constants.Mb256, VCC_IF.X16_3V, BLKLYT.P30_Btm, MFP_PRG.Buffer1, MFP_DELAY.SR1) { PAGE_SIZE = 64U });
            FlashDB.Add(new P_NOR("Intel A28F512", 0x89, 0xB8, Constants.Kb512, VCC_IF.X8_5V_12VPP, BLKLYT.EntireDevice, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Intel 28F320J5", 0x89, 0x14, Constants.Mb032, VCC_IF.X16_5V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer1, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F640J5", 0x89, 0x15, Constants.Mb064, VCC_IF.X16_5V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer1, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F320J3", 0x89, 0x16, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer1, MFP_DELAY.SR1)); // 32 byte buffers
            FlashDB.Add(new P_NOR("Intel 28F640J3", 0x89, 0x17, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer1, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F128J3", 0x89, 0x18, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer1, MFP_DELAY.SR1)); // CV
            FlashDB.Add(new P_NOR("Intel 28F256J3", 0x89, 0x1D, Constants.Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer1, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F800C3(T)", 0x89, 0x88C0, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F800C3(B)", 0x89, 0x88C1, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F160C3(T)", 0x89, 0x88C2, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F160C3(B)", 0x89, 0x88C3, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F320C3(T)", 0x89, 0x88C4, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F320C3(B)", 0x89, 0x88C5, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F640C3(T)", 0x89, 0x88CC, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F640C3(B)", 0x89, 0x88CD, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F008SA", 0x89, 0xA2, Constants.Mb008, VCC_IF.X8_5V_12VPP, BLKLYT.Kb512_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F400B3(T)", 0x89, 0x8894, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F400B3(B)", 0x89, 0x8895, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F800B3(T)", 0x89, 0x8892, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F800B3(B)", 0x89, 0x8893, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F160B3(T)", 0x89, 0x8890, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F160B3(B)", 0x89, 0x8891, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F320B3(T)", 0x89, 0x8896, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F320B3(B)", 0x89, 0x8897, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F640B3(T)", 0x89, 0x8898, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F640B3(B)", 0x89, 0x8899, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F004B5(T)", 0x89, 0x78, Constants.Mb004, VCC_IF.X8_5V, BLKLYT.Four_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F200B5(T)", 0x89, 0x2274, Constants.Mb002, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F400B5(T)", 0x89, 0x4470, Constants.Mb004, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F800B5(T)", 0x89, 0x889C, Constants.Mb008, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F004B5(B)", 0x89, 0x79, Constants.Mb004, VCC_IF.X8_5V, BLKLYT.Four_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F200B5(B)", 0x89, 0x2275, Constants.Mb002, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F400B5(B)", 0x89, 0x4471, Constants.Mb004, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Intel 28F800B5(B)", 0x89, 0x889D, Constants.Mb008, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            // AMD
            FlashDB.Add(new P_NOR("AMD AM29F200(T)", 0x1, 0x2251, Constants.Mb002, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29F200(B)", 0x1, 0x2252, Constants.Mb002, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29LV002B(T)", 0x1, 0x40, Constants.Mb002, VCC_IF.X8_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS)); // TSOP40 (TYPE-B) CV
            FlashDB.Add(new P_NOR("AMD AM29LV002B(B)", 0x1, 0xC2, Constants.Mb002, VCC_IF.X8_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29LV065D", 0x1, 0x93, Constants.Mb064, VCC_IF.X8_3V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29F040", 0x1, 0xA4, Constants.Mb004, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7) { ERASE_DELAY = 500, RESET_ENABLED = false });
            FlashDB.Add(new P_NOR("AMD AM29F010B", 0x1, 0x20, Constants.Mb001, VCC_IF.X8_5V, BLKLYT.Kb128_Uni, MFP_PRG.Standard, MFP_DELAY.uS) { ERASE_DELAY = 500, RESET_ENABLED = false });
            FlashDB.Add(new P_NOR("AMD AM29F040B", 0x20, 0xE2, Constants.Mb004, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS) { ERASE_DELAY = 500, RESET_ENABLED = false }); // Why is this not: 01 A4? (PLCC32 and DIP32 tested)
            FlashDB.Add(new P_NOR("AMD AM29F080B", 0x1, 0xD5, Constants.Mb008, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS) { ERASE_DELAY = 500, RESET_ENABLED = false }); // TSOP40
            FlashDB.Add(new P_NOR("AMD AM29F016B", 0x1, 0xAD, Constants.Mb016, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS)); // NO CFI
            FlashDB.Add(new P_NOR("AMD AM29F016D", 0x1, 0xAD, Constants.Mb016, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS)); // TSOP40 CV
            FlashDB.Add(new P_NOR("AMD AM29F032B", 0x4, 0xD4, Constants.Mb032, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS)); // TSOP40 CV (die with wrong MFG ID)
            FlashDB.Add(new P_NOR("AMD AM29F032B", 0x1, 0x41, Constants.Mb032, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS)); // TSOP40 (correct die)
            FlashDB.Add(new P_NOR("AMD AM29LV200(T)", 0x1, 0x223B, Constants.Mb002, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29LV200(B)", 0x1, 0x22BF, Constants.Mb002, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29F200(T)", 0x1, 0x2251, Constants.Mb002, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29F200(B)", 0x1, 0x2257, Constants.Mb002, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29LV400(T)", 0x1, 0x22B9, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29LV400(B)", 0x1, 0x22BA, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29F400(T)", 0x1, 0x2223, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29F400(B)", 0x1, 0x22AB, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS)); // <-- please verify
            FlashDB.Add(new P_NOR("AMD AM29LV800(T)", 0x1, 0x22DA, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29LV800(B)", 0x1, 0x225B, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29F800(T)", 0x1, 0x22D6, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29F800(B)", 0x1, 0x2258, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29LV160B(T)", 0x1, 0x22C4, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS)); // (CV)
            FlashDB.Add(new P_NOR("AMD AM29LV160B(B)", 0x1, 0x2249, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29DL322G(T)", 0x1, 0x2255, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29DL322G(B)", 0x1, 0x2256, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29DL323G(T)", 0x1, 0x2250, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29DL323G(B)", 0x1, 0x2253, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29DL324G(T)", 0x1, 0x225C, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29DL324G(B)", 0x1, 0x225F, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29LV320D(T)", 0x1, 0x22F6, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29LV320D(B)", 0x1, 0x22F9, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29LV320M(T)", 0x1, 0x2201, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("AMD AM29LV320M(B)", 0x1, 0x2200, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            // Winbond
            FlashDB.Add(new P_NOR("Winbond W49F020", 0xDA, 0x8C, Constants.Mb002, VCC_IF.X8_5V, BLKLYT.EntireDevice, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Winbond W49F002U", 0xDA, 0xB, Constants.Mb002, VCC_IF.X8_5V, BLKLYT.Mb002_NonUni, MFP_PRG.Standard, MFP_DELAY.uS) { PAGE_SIZE = 128U, HARDWARE_DELAY = 18 });
            FlashDB.Add(new P_NOR("Winbond W29EE512", 0xDA, 0xC8, Constants.Kb512, VCC_IF.X8_5V, BLKLYT.Kb256_Uni, MFP_PRG.PageMode, MFP_DELAY.DQ7) { PAGE_SIZE = 128U, ERASE_REQUIRED = false, RESET_ENABLED = false });
            FlashDB.Add(new P_NOR("Winbond W29C010", 0xDA, 0xC1, Constants.Mb001, VCC_IF.X8_5V, BLKLYT.Kb256_Uni, MFP_PRG.PageMode, MFP_DELAY.mS) { PAGE_SIZE = 128U, ERASE_REQUIRED = false });
            FlashDB.Add(new P_NOR("Winbond W29C020", 0xDA, 0x45, Constants.Mb002, VCC_IF.X8_5V, BLKLYT.Kb256_Uni, MFP_PRG.PageMode, MFP_DELAY.mS) { PAGE_SIZE = 128U, ERASE_REQUIRED = false });
            FlashDB.Add(new P_NOR("Winbond W29C040", 0xDA, 0x46, Constants.Mb004, VCC_IF.X8_5V, BLKLYT.Kb256_Uni, MFP_PRG.PageMode, MFP_DELAY.mS) { PAGE_SIZE = 256U, ERASE_REQUIRED = false });
            FlashDB.Add(new P_NOR("Winbond W29GL256S", 0xEF, 0x227E, Constants.Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2201) { PAGE_SIZE = 512U });
            FlashDB.Add(new P_NOR("Winbond W29GL256P", 0xEF, 0x227E, Constants.Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2201) { PAGE_SIZE = 64U });
            FlashDB.Add(new P_NOR("Winbond W29GL128C", 0x1, 0x227E, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2101));
            FlashDB.Add(new P_NOR("Winbond W29GL064CT", 0x1, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x101));
            FlashDB.Add(new P_NOR("Winbond W29GL064CB", 0x1, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x100));
            FlashDB.Add(new P_NOR("Winbond W29GL032CT", 0x1, 0x227E, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x1A01));
            FlashDB.Add(new P_NOR("Winbond W29GL032CB", 0x1, 0x227E, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x1A00));
            // SST
            FlashDB.Add(new P_NOR("SST 39VF401C/39LF401C", 0xBF, 0x2321, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("SST 39VF402C/39LF402C", 0xBF, 0x2322, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("SST 39SF512", 0xBF, 0xB4, Constants.Kb512, VCC_IF.X8_5V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("SST 39SF010", 0xBF, 0xB5, Constants.Mb001, VCC_IF.X8_5V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("SST 39SF020", 0xBF, 0xB6, Constants.Mb002, VCC_IF.X8_5V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("SST 39SF040", 0xBF, 0xB7, Constants.Mb004, VCC_IF.X8_5V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("SST 39LF010", 0xBF, 0xD5, Constants.Mb001, VCC_IF.X8_5V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("SST 39LF020", 0xBF, 0xD6, Constants.Mb002, VCC_IF.X8_5V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("SST 39LF040", 0xBF, 0xD7, Constants.Mb004, VCC_IF.X8_5V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("SST 39VF200", 0xBF, 0x2789, Constants.Mb002, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("SST 39VF400", 0xBF, 0x2780, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("SST 39VF800", 0xBF, 0x2781, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("SST 39VF160", 0xBF, 0x2782, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("SST 39VF1681", 0xBF, 0xC8, Constants.Mb016, VCC_IF.X8_3V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS)); // Verified 520
            FlashDB.Add(new P_NOR("SST 39VF1682", 0xBF, 0xC9, Constants.Mb016, VCC_IF.X8_3V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("SST 39VF1601", 0xBF, 0x234B, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("SST 39VF1602", 0xBF, 0x234A, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("SST 39VF3201", 0xBF, 0x235B, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("SST 39VF3202", 0xBF, 0x235A, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("SST 39VF6401", 0xBF, 0x236B, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("SST 39VF6402", 0xBF, 0x236A, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("SST 39VF6401B", 0xBF, 0x236D, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("SST 39VF6402B", 0xBF, 0x236C, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Kb032_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7));
            // Atmel
            FlashDB.Add(new P_NOR("Atmel AT29C010A", 0x1F, 0xD5, Constants.Mb001, VCC_IF.X8_5V, BLKLYT.Kb256_Uni, MFP_PRG.PageMode, MFP_DELAY.DQ7) { ERASE_REQUIRED = false, PAGE_SIZE = 128U, RESET_ENABLED = false });
            FlashDB.Add(new P_NOR("Atmel AT49F512", 0x1F, 0x3, Constants.Kb512, VCC_IF.X8_5V, BLKLYT.EntireDevice, MFP_PRG.Standard, MFP_DELAY.uS)); // No SE, only BE
            FlashDB.Add(new P_NOR("Atmel AT49F010", 0x1F, 0x17, Constants.Mb001, VCC_IF.X8_5V, BLKLYT.EntireDevice, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Atmel AT49F020", 0x1F, 0xB, Constants.Mb002, VCC_IF.X8_5V, BLKLYT.EntireDevice, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Atmel AT49F040", 0x1F, 0x13, Constants.Mb004, VCC_IF.X8_5V, BLKLYT.EntireDevice, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Atmel AT49F040T", 0x1F, 0x12, Constants.Mb004, VCC_IF.X8_5V, BLKLYT.EntireDevice, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Atmel AT49BV/LV16X", 0x1F, 0xC0, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Standard, MFP_DELAY.uS)); // Supports Single Pulse Byte/ Word Program
            FlashDB.Add(new P_NOR("Atmel AT49BV/LV16XT", 0x1F, 0xC2, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            // MXIC
            FlashDB.Add(new P_NOR("MXIC MX29F040", 0xC2, 0xA4, Constants.Mb004, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("MXIC MX29F080", 0xC2, 0xD5, Constants.Mb008, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("MXIC MX29F016", 0xC2, 0xAD, Constants.Mb016, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("MXIC MX29F800T", 0xC2, 0x22D6, Constants.Mb008, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS)); // SO44 CV
            FlashDB.Add(new P_NOR("MXIC MX29F800B", 0xC2, 0x2258, Constants.Mb008, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("MXIC MX29F1610", 0xC2, 0xF1, Constants.Mb016, VCC_IF.X16_5V, BLKLYT.Mb001_Uni, MFP_PRG.PageMode, MFP_DELAY.mS) { PAGE_SIZE = 64U, HARDWARE_DELAY = 8 });
            FlashDB.Add(new P_NOR("MXIC MX29F1610MC", 0xC2, 0xF7, Constants.Mb016, VCC_IF.X16_5V, BLKLYT.Mb001_Uni, MFP_PRG.PageMode, MFP_DELAY.mS) { PAGE_SIZE = 64U, HARDWARE_DELAY = 8 });
            FlashDB.Add(new P_NOR("MXIC MX29F1610MC", 0xC2, 0xF8, Constants.Mb016, VCC_IF.X16_5V, BLKLYT.Mb001_Uni, MFP_PRG.PageMode, MFP_DELAY.mS) { PAGE_SIZE = 64U, HARDWARE_DELAY = 12 });
            FlashDB.Add(new P_NOR("MXIC MX29F1610A", 0xC2, 0xFA, Constants.Mb016, VCC_IF.X16_5V, BLKLYT.Mb001_Uni, MFP_PRG.PageMode, MFP_DELAY.mS) { PAGE_SIZE = 64U, HARDWARE_DELAY = 8 });
            FlashDB.Add(new P_NOR("MXIC MX29F1610B", 0xC2, 0xFA, Constants.Mb016, VCC_IF.X16_5V, BLKLYT.Mb001_Uni, MFP_PRG.PageMode, MFP_DELAY.mS) { PAGE_SIZE = 64U, HARDWARE_DELAY = 8 });
            FlashDB.Add(new P_NOR("MXIC MX29SL800CT", 0xC2, 0x22EA, Constants.Mb008, VCC_IF.X16_1V8, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.DQ7)); // Untested
            FlashDB.Add(new P_NOR("MXIC MX29SL800CB", 0xC2, 0x226B, Constants.Mb008, VCC_IF.X16_1V8, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.DQ7)); // Untested
            FlashDB.Add(new P_NOR("MXIC MX29L3211", 0xC2, 0xF9, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.PageMode, MFP_DELAY.SR2) { PAGE_SIZE = 64U }); // Actualy supports up to 256 bytes (tested build 595)
            FlashDB.Add(new P_NOR("MXIC MX29LV040", 0xC2, 0x4F, Constants.Mb004, VCC_IF.X8_3V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("MXIC MX29LV400T", 0xC2, 0x22B9, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("MXIC MX29LV400B", 0xC2, 0x22BA, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("MXIC MX29LV800T", 0xC2, 0x22DA, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("MXIC MX29LV800B", 0xC2, 0x225B, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("MXIC MX29LV160DT", 0xC2, 0x22C4, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS) { HARDWARE_DELAY = 6 }); // Required! SO-44 in CV
            FlashDB.Add(new P_NOR("MXIC MX29LV160DB", 0xC2, 0x2249, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS) { HARDWARE_DELAY = 6 });
            FlashDB.Add(new P_NOR("MXIC MX29LV320T", 0xC2, 0x22A7, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS) { HARDWARE_DELAY = 0 });
            FlashDB.Add(new P_NOR("MXIC MX29LV320B", 0xC2, 0x22A8, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS) { HARDWARE_DELAY = 0 });
            FlashDB.Add(new P_NOR("MXIC MX29LV640ET", 0xC2, 0x22C9, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Standard, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("MXIC MX29LV640EB", 0xC2, 0x22CB, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Standard, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("MXIC MX29GL128F", 0xC2, 0x227E, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2101) { HARDWARE_DELAY = 6 });
            FlashDB.Add(new P_NOR("MXIC MX29GL256F", 0xC2, 0x227E, Constants.Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2201) { HARDWARE_DELAY = 6 });
            FlashDB.Add(new P_NOR("MXIC MX29LV128DT", 0xC2, 0x227E, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Standard, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("MXIC MX29LV128DB", 0xC2, 0x227A, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Standard, MFP_DELAY.DQ7));
            // Cypress / Spansion
            FlashDB.Add(new P_NOR("Cypress S29AL004D(B)", 0x1, 0x22BA, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Cypress S29AL004D(T)", 0x1, 0x22B9, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Cypress S29AL008J(B)", 0x1, 0x225B, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Cypress S29AL008J(T)", 0x1, 0x22DA, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Cypress S29AL016M(B)", 0x1, 0x2249, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Cypress S29AL016M(T)", 0x1, 0x22C4, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Cypress S29AL016D(B)", 0x1, 0x2249, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Cypress S29AL016D(T)", 0x1, 0x22C4, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Cypress S29AL016J(T)", 0x1, 0x22C4, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Cypress S29AL016J(B)", 0x1, 0x2249, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Cypress S29AL032D", 0x1, 0xA3, Constants.Mb032, VCC_IF.X8_3V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS)); // Available in TSOP-40
            FlashDB.Add(new P_NOR("Cypress S29AL032D(B)", 0x1, 0x22F9, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Cypress S29AL032D(T)", 0x1, 0x22F6, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Cypress S29GL128", 0x1, 0x227E, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.uS, 0x2100) { PAGE_SIZE = 64U }); // We need to test this device
            FlashDB.Add(new P_NOR("Cypress S29GL256", 0x1, 0x227E, Constants.Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.uS, 0x2200) { PAGE_SIZE = 64U });
            FlashDB.Add(new P_NOR("Cypress S29GL512", 0x1, 0x227E, Constants.Mb512, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.uS, 0x2300) { PAGE_SIZE = 64U });
            FlashDB.Add(new P_NOR("Cypress S29GL01G", 0x1, 0x227E, Constants.Gb001, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.uS, 0x2800) { PAGE_SIZE = 64U });
            FlashDB.Add(new P_NOR("Cypress S29JL032J(T)", 0x1, 0x227E, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7, 0xA01));
            FlashDB.Add(new P_NOR("Cypress S29JL032J(B)", 0x1, 0x227E, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7, 0xA00));
            FlashDB.Add(new P_NOR("Cypress S29JL064J", 0x1, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Dual, MFP_PRG.BypassMode, MFP_DELAY.DQ7, 0x201)); // Top and bottom boot blocks (CV)
            FlashDB.Add(new P_NOR("Cypress S29GL032M", 0x1, 0x227E, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.SR1, 0x1C00)); // Model R0
            FlashDB.Add(new P_NOR("Cypress S29GL032M(B)", 0x1, 0x227E, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Standard, MFP_DELAY.SR1, 0x1A00)); // Bottom-Boot
            FlashDB.Add(new P_NOR("Cypress S29GL032M(T)", 0x1, 0x227E, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Standard, MFP_DELAY.SR1, 0x1A01)); // Top-Boot
            FlashDB.Add(new P_NOR("Cypress S29JL032J(B)", 0x1, 0x225F, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7)); // Bottom-Boot
            FlashDB.Add(new P_NOR("Cypress S29JL032J(T)", 0x1, 0x225C, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7)); // Top-Boot
            FlashDB.Add(new P_NOR("Cypress S29JL032J(B)", 0x1, 0x2253, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7)); // Bottom-Boot
            FlashDB.Add(new P_NOR("Cypress S29JL032J(T)", 0x1, 0x2250, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7)); // Top-Boot
            FlashDB.Add(new P_NOR("Cypress S29JL032J(B)", 0x1, 0x2256, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7)); // Bottom-Boot
            FlashDB.Add(new P_NOR("Cypress S29JL032J(T)", 0x1, 0x2255, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7)); // Top-Boot
            FlashDB.Add(new P_NOR("Cypress S29GL064M", 0x1, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.SR1, 0x1300)); // Model R0
            FlashDB.Add(new P_NOR("Cypress S29GL064M", 0x1, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.SR1, 0xC01));
            FlashDB.Add(new P_NOR("Cypress S29GL064M(T)", 0x1, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Standard, MFP_DELAY.SR1, 0x1001)); // Top-Boot
            FlashDB.Add(new P_NOR("Cypress S29GL064M(B)", 0x1, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Standard, MFP_DELAY.SR1, 0x1000)); // Bottom-Boot
            FlashDB.Add(new P_NOR("Cypress S29GL064M", 0x1, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.SR1, 0x1301));
            FlashDB.Add(new P_NOR("Cypress S29GL128M", 0x1, 0x227E, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Standard, MFP_DELAY.SR1, 0x1200));
            FlashDB.Add(new P_NOR("Cypress S29GL256M", 0x1, 0x227E, Constants.Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Standard, MFP_DELAY.SR1, 0x1201));
            FlashDB.Add(new P_NOR("Cypress S29GL032N", 0x1, 0x227E, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x1D00) { PAGE_SIZE = 32U });
            FlashDB.Add(new P_NOR("Cypress S29GL064N", 0x1, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0xC01) { PAGE_SIZE = 32U });
            FlashDB.Add(new P_NOR("Cypress S29GL128N", 0x1, 0x227E, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2101) { PAGE_SIZE = 32U });
            FlashDB.Add(new P_NOR("Cypress S29GL256N", 0x1, 0x227E, Constants.Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2201) { PAGE_SIZE = 32U }); // (CHIP-VAULT)
            FlashDB.Add(new P_NOR("Cypress S29GL512N", 0x1, 0x227E, Constants.Mb512, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2301) { PAGE_SIZE = 32U });
            FlashDB.Add(new P_NOR("Cypress S29GL128S", 0x1, 0x227E, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.SR1, 0x2101) { PAGE_SIZE = 512U }); // (CHIP-VAULT) BGA-64
            FlashDB.Add(new P_NOR("Cypress S29GL256S", 0x1, 0x227E, Constants.Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.SR1, 0x2201) { PAGE_SIZE = 512U });
            FlashDB.Add(new P_NOR("Cypress S29GL512S", 0x1, 0x227E, Constants.Mb512, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.SR1, 0x2301) { PAGE_SIZE = 512U });
            FlashDB.Add(new P_NOR("Cypress S29GL128P", 0x1, 0x227E, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2101) { PAGE_SIZE = 64U }); // (CHIP-VAULT)
            FlashDB.Add(new P_NOR("Cypress S29GL256P", 0x1, 0x227E, Constants.Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2201) { PAGE_SIZE = 64U });
            FlashDB.Add(new P_NOR("Cypress S29GL512P", 0x1, 0x227E, Constants.Mb512, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2301) { PAGE_SIZE = 64U });
            FlashDB.Add(new P_NOR("Cypress S29GL01GP", 0x1, 0x227E, Constants.Gb001, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2801) { PAGE_SIZE = 64U });
            FlashDB.Add(new P_NOR("Cypress S29GL512T", 0x1, 0x227E, Constants.Mb512, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.SR1, 0x2301) { PAGE_SIZE = 512U });
            FlashDB.Add(new P_NOR("Cypress S29GL01GS", 0x1, 0x227E, Constants.Gb001, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.SR1, 0x2401) { PAGE_SIZE = 512U });
            FlashDB.Add(new P_NOR("Cypress S29GL01GT", 0x1, 0x227E, Constants.Gb001, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.SR1, 0x2801) { PAGE_SIZE = 512U }); // (CHIP-VAULT)
            FlashDB.Add(new P_NOR("Cypress S70GL02G", 0x1, 0x227E, Constants.Gb001, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.SR1, 0x4801) { PAGE_SIZE = 512U, CE2 = 26, DUAL_DIE = true }); // (CHIP-VAULT)
            FlashDB.Add(new P_NOR("Cypress S29PL032J", 0x1, 0x227E, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.RYRB, 0xA01));
            FlashDB.Add(new P_NOR("Cypress S29PL064J", 0x1, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.RYRB, 0x201));
            FlashDB.Add(new P_NOR("Cypress S29PL127J", 0x1, 0x227E, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.RYRB, 0x2000)); // CV
            // ST Microelectronics (now numonyx)
            FlashDB.Add(new P_NOR("ST M29F200T", 0x20, 0xD3, Constants.Mb002, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS)); // Available in TSOP48/SO44
            FlashDB.Add(new P_NOR("ST M29F200B", 0x20, 0xD4, Constants.Mb002, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS)); // Available in TSOP48/SO44
            FlashDB.Add(new P_NOR("ST M29F400T", 0x20, 0xD5, Constants.Mb004, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS)); // Available in TSOP48/SO44
            FlashDB.Add(new P_NOR("ST M29F400B", 0x20, 0xD6, Constants.Mb004, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS)); // Available in TSOP48/SO44
            FlashDB.Add(new P_NOR("ST M29F080A", 0x20, 0xF1, Constants.Mb008, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("ST M29F800T", 0x20, 0xEC, Constants.Mb008, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS)); // Available in TSOP48/SO44
            FlashDB.Add(new P_NOR("ST M29F800B", 0x20, 0x58, Constants.Mb008, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS)); // Available in TSOP48/SO44
            // ST 3V NOR
            FlashDB.Add(new P_NOR("ST M29W400DT", 0x20, 0xEE, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("ST M29W400DB", 0x20, 0xEF, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("ST M29W800AT", 0x20, 0xD7, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("ST M29W800AB", 0x20, 0x5B, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("ST M29W800DT", 0x20, 0x22D7, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("ST M29W800DB", 0x20, 0x225B, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("ST M29W160ET", 0x20, 0x22C4, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("ST M29W160EB", 0x20, 0x2249, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS)); // CV
            FlashDB.Add(new P_NOR("ST M29D323DT", 0x20, 0x225E, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("ST M29D323DB", 0x20, 0x225F, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("ST M29W320DT", 0x20, 0x22CA, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("ST M29W320DB", 0x20, 0x22CB, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("ST M29W320ET", 0x20, 0x2256, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("ST M29W320EB", 0x20, 0x2257, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("ST M29W640GH", 0x20, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0xC01));
            FlashDB.Add(new P_NOR("ST M29W640GL", 0x20, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0xC00));
            FlashDB.Add(new P_NOR("ST M29W640GT", 0x20, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x1001));
            FlashDB.Add(new P_NOR("ST M29W640GB", 0x20, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x1000));
            FlashDB.Add(new P_NOR("ST M29W128GH", 0x20, 0x227E, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2101));
            FlashDB.Add(new P_NOR("ST M29W128GL", 0x20, 0x227E, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2100));
            // ST M28
            FlashDB.Add(new P_NOR("ST M28W160CT", 0x20, 0x88CE, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("ST M28W160CB", 0x20, 0x88CF, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("ST M28W320FCT", 0x20, 0x88BA, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("ST M28W320FCB", 0x20, 0x88BB, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("ST M28W320BT", 0x20, 0x88BC, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("ST M28W320BB", 0x20, 0x88BD, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("ST M28W640ECT", 0x20, 0x8848, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("ST M28W640ECB", 0x20, 0x8849, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("ST M28W320FSU", 0x20, 0x880C, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1)); // CV (EasyBGA-64)
            FlashDB.Add(new P_NOR("ST M28W640FSU", 0x20, 0x8857, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("ST M58LW064D", 0x20, 0x17, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            // Micron
            FlashDB.Add(new P_NOR("Micron M29F200FT", 0xC2, 0x2251, Constants.Mb002, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Micron M29F200FB", 0xC2, 0x2257, Constants.Mb002, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Micron M29F400FT", 0xC2, 0x2223, Constants.Mb004, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Micron M29F400FB", 0xC2, 0x22AB, Constants.Mb004, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Micron M29F800FT", 0x1, 0x22D6, Constants.Mb008, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Micron M29F800FB", 0x1, 0x2258, Constants.Mb008, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Micron M29F160FT", 0x1, 0x22D2, Constants.Mb016, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Micron M29F160FB", 0x1, 0x22D8, Constants.Mb016, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Micron M29W160ET", 0x20, 0x22C4, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Micron M29W160EB", 0x20, 0x2249, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Micron M29W320DT", 0x20, 0x22CA, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Micron M29W320DB", 0x20, 0x22CB, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Micron M29W640GH", 0x20, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS, 0xC01));
            FlashDB.Add(new P_NOR("Micron M29W640GL", 0x20, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS, 0xC00));
            FlashDB.Add(new P_NOR("Micron M29W640GT", 0x20, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS, 0x1001));
            FlashDB.Add(new P_NOR("Micron M29W640GB", 0x20, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS, 0x1000));
            FlashDB.Add(new P_NOR("Micron M29W128GH", 0x20, 0x227E, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2201)); // (CHIP-VAULT)
            FlashDB.Add(new P_NOR("Micron M29W128GL", 0x20, 0x227E, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2200));
            FlashDB.Add(new P_NOR("Micron M29W256GH", 0x20, 0x227E, Constants.Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2201));
            FlashDB.Add(new P_NOR("Micron M29W256GL", 0x20, 0x227E, Constants.Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2200));
            FlashDB.Add(new P_NOR("Micron M29W512G", 0x20, 0x227E, Constants.Mb512, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2301));
            FlashDB.Add(new P_NOR("Micron MT28EW128", 0x89, 0x227E, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2101) { PAGE_SIZE = 256U }); // May support up to 1024 bytes
            FlashDB.Add(new P_NOR("Micron MT28EW256", 0x89, 0x227E, Constants.Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2201) { PAGE_SIZE = 256U });
            FlashDB.Add(new P_NOR("Micron MT28EW512", 0x89, 0x227E, Constants.Mb512, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2301) { PAGE_SIZE = 256U });
            FlashDB.Add(new P_NOR("Micron MT28EW01G", 0x89, 0x227E, Constants.Gb001, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x2801) { PAGE_SIZE = 256U });
            FlashDB.Add(new P_NOR("Micron MT28FW02G", 0x89, 0x227E, Constants.Gb002, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.Buffer2, MFP_DELAY.DQ7, 0x4801) { PAGE_SIZE = 256U }); // Stacked die / BGA-64 (11x13mm)
            // Sharp
            FlashDB.Add(new P_NOR("Sharp LHF00L15", 0xB0, 0xA1, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Mb032_NonUni, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Sharp LH28F160S3", 0xB0, 0xD0, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Sharp LH28F320S3", 0xB0, 0xD4, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Sharp LH28F160BJE", 0xB0, 0xE9, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Sharp LH28F320BJE", 0xB0, 0xE3, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.IntelSharp, MFP_DELAY.SR1));
            FlashDB.Add(new P_NOR("Sharp LH28F008SCT", 0x89, 0xA6, Constants.Mb008, VCC_IF.X8_3V, BLKLYT.Kb512_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1)); // TSOP40
            FlashDB.Add(new P_NOR("Sharp LH28F016SCT", 0x89, 0xAA, Constants.Mb016, VCC_IF.X8_3V, BLKLYT.Kb512_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1)); // TSOP40
            // FlashDB.Add(New P_NOR("Sharp LH28F016SU", &HB0, &HB0, Mb016, VCC_IF.X16_5V, BLKLYT.Kb512_Uni, MFP_PRG.IntelSharp, MFP_DELAY.SR1)) 'TSOP56-B
            // Toshiba
            FlashDB.Add(new P_NOR("Toshiba TC58FVT800", 0x98, 0x4F, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Toshiba TC58FVB800", 0x98, 0xCE, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Toshiba TC58FVT160", 0x98, 0xC2, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Toshiba TC58FVB160", 0x98, 0x43, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Toshiba TC58FVT321", 0x98, 0x9C, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Toshiba TC58FVB321", 0x98, 0x9A, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Toshiba TC58FVM5T2A", 0x98, 0xC5, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Toshiba TC58FVM5B2A", 0x98, 0x55, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Toshiba TC58FVM5T3A", 0x98, 0xC6, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Toshiba TC58FVM5B3A", 0x98, 0x56, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Toshiba TC58FYM5T2A", 0x98, 0x59, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Toshiba TC58FYM5B2A", 0x98, 0x69, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Toshiba TC58FYM5T3A", 0x98, 0x5A, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Toshiba TC58FYM5B3A", 0x98, 0x6A, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Toshiba TC58FVM6T2A", 0x98, 0x57, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Toshiba TC58FVM6B2A", 0x98, 0x58, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Toshiba TC58FVM6T5B", 0x98, 0x2D, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Toshiba TC58FVM6B5B", 0x98, 0x2E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Toshiba TC58FYM6T2A", 0x98, 0x7A, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Toshiba TC58FYM6B2A", 0x98, 0x7B, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Toshiba TC58FVM7T2A", 0x98, 0x7C, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Toshiba TC58FVM7B2A", 0x98, 0x82, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Toshiba TC58FYM7T2A", 0x98, 0xD8, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Toshiba TC58FYM7B2A", 0x98, 0xB2, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            // Samsung
            FlashDB.Add(new P_NOR("Samsung K8P1615UQB", 0xEC, 0x257E, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Mb016_Samsung, MFP_PRG.BypassMode, MFP_DELAY.uS, 0x1));
            FlashDB.Add(new P_NOR("Samsung K8D1716UT", 0xEC, 0x2275, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Samsung K8D1716UB", 0xEC, 0x2277, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Samsung K8D3216UT", 0xEC, 0x22A0, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Samsung K8D3216UB", 0xEC, 0x22A2, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Samsung K8P3215UQB", 0xEC, 0x257E, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Mb032_Samsung, MFP_PRG.BypassMode, MFP_DELAY.uS, 0x301));
            FlashDB.Add(new P_NOR("Samsung K8D6316UT", 0xEC, 0x22E0, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Samsung K8D6316UB", 0xEC, 0x22E2, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Samsung K8P6415UQB", 0xEC, 0x257E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Mb064_Samsung, MFP_PRG.BypassMode, MFP_DELAY.uS, 0x601));
            FlashDB.Add(new P_NOR("Samsung K8P2716UZC", 0xEC, 0x227E, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS, 0x6660));
            FlashDB.Add(new P_NOR("Samsung K8Q2815UQB", 0xEC, 0x257E, Constants.Mb128, VCC_IF.X16_3V, BLKLYT.Mb128_Samsung, MFP_PRG.BypassMode, MFP_DELAY.uS, 0x601)); // TSOP56 Type-A
            FlashDB.Add(new P_NOR("Samsung K8P5516UZB", 0xEC, 0x227E, Constants.Mb256, VCC_IF.X16_3V, BLKLYT.Mb001_Uni, MFP_PRG.BypassMode, MFP_DELAY.uS, 0x6460));
            FlashDB.Add(new P_NOR("Samsung K8P5615UQA", 0xEC, 0x227E, Constants.Mb256, VCC_IF.X16_3V, BLKLYT.Mb256_Samsung, MFP_PRG.BypassMode, MFP_DELAY.uS, 0x6360));
            // Hynix
            FlashDB.Add(new P_NOR("Hynix HY29F040", 0xAD, 0xA4, Constants.Mb004, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("Hynix HY29F080", 0xAD, 0xD5, Constants.Mb008, VCC_IF.X8_5V, BLKLYT.Kb512_Uni, MFP_PRG.Standard, MFP_DELAY.DQ7)); // TSOP40-A
            FlashDB.Add(new P_NOR("Hynix HY29F400T", 0xAD, 0x2223, Constants.Mb004, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Hynix HY29F400B", 0xAD, 0x22AB, Constants.Mb004, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Hynix HY29F800T", 0xAD, 0x22D6, Constants.Mb008, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Hynix HY29F800B", 0xAD, 0x2258, Constants.Mb008, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Hynix HY29LV400T", 0xAD, 0x22B9, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Hynix HY29LV400B", 0xAD, 0x22BA, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Hynix HY29LV800T", 0xAD, 0x22DA, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Hynix HY29LV800B", 0xAD, 0x225B, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Hynix HY29LV160T", 0xAD, 0x22C4, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Hynix HY29LV160B", 0xAD, 0x2249, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Hynix HY29LV320T", 0xAD, 0x227E, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Hynix HY29LV320B", 0xAD, 0x227D, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            // Fujitsu
            FlashDB.Add(new P_NOR("Fujitsu MBM29F400TA", 0x4, 0x2223, Constants.Mb004, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Fujitsu MBM29F400BA", 0x4, 0x22AB, Constants.Mb004, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Fujitsu MBM29F800TA", 0x4, 0x22D6, Constants.Mb008, VCC_IF.X16_5V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Fujitsu MBM29F800BA", 0x4, 0x2258, Constants.Mb008, VCC_IF.X16_5V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Fujitsu MBM29LV200TC", 0x4, 0x223B, Constants.Mb002, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Fujitsu MBM29LV200BC", 0x4, 0x22BF, Constants.Mb002, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Fujitsu MBM29LV400TC", 0x4, 0x22B9, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Fujitsu MBM29LV400BC", 0x4, 0x22BA, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Fujitsu MBM29LV800TA", 0x4, 0x22DA, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Fujitsu MBM29LV800BA", 0x4, 0x225B, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Fujitsu MBM29LV160T", 0x4, 0x22C4, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Fujitsu MBM29LV160B", 0x4, 0x2249, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.Standard, MFP_DELAY.uS));
            FlashDB.Add(new P_NOR("Fujitsu MBM29LV320TE", 0x4, 0x22F6, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Standard, MFP_DELAY.uS)); // Supports FAST programming (ADR=0xA0,PA=PD)
            FlashDB.Add(new P_NOR("Fujitsu MBM29LV320BE", 0x4, 0x22F9, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Standard, MFP_DELAY.uS)); // Supports FAST programming (ADR=0xA0,PA=PD)
            FlashDB.Add(new P_NOR("Fujitsu MBM29DL32XTD", 0x4, 0x2259, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.Standard, MFP_DELAY.uS)); // Supports FAST programming (ADR=0xA0,PA=PD)
            FlashDB.Add(new P_NOR("Fujitsu MBM29DL32XBD", 0x4, 0x225A, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.Standard, MFP_DELAY.uS)); // Supports FAST programming (ADR=0xA0,PA=PD)
            // EON (MFG is 7F 1C)
            FlashDB.Add(new P_NOR("EON EN29LV400AT", 0x7F, 0x22B9, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("EON EN29LV400AB", 0x7F, 0x22BA, Constants.Mb004, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("EON EN29LV800AT", 0x7F, 0x22DA, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("EON EN29LV800AB", 0x7F, 0x225B, Constants.Mb008, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("EON EN29LV160AT", 0x7F, 0x22C4, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("EON EN29LV160AB", 0x7F, 0x2249, Constants.Mb016, VCC_IF.X16_3V, BLKLYT.Four_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("EON EN29LV320AT", 0x7F, 0x22F6, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Top, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("EON EN29LV320AB", 0x7F, 0x22F9, Constants.Mb032, VCC_IF.X16_3V, BLKLYT.Two_Btm, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
            FlashDB.Add(new P_NOR("EON EN29LV640", 0x7F, 0x227E, Constants.Mb064, VCC_IF.X16_3V, BLKLYT.Kb512_Uni, MFP_PRG.BypassMode, MFP_DELAY.DQ7));
        }

        private void NAND_Database() {
            // 00 = Does not matter
            // Micron SLC 8x NAND devices
            FlashDB.Add(new P_NAND("ST NAND128W3A", 0x20, 0x732073U, 512U, 16, 32, 1024, VCC_IF.X8_3V)); // 128Mb
            FlashDB.Add(new P_NAND("ST NAND256R3A", 0x20, 0x352035U, 512U, 16, 32, 2048, VCC_IF.X8_1V8)); // 256Mb
            FlashDB.Add(new P_NAND("ST NAND256W3A", 0x20, 0x752075U, 512U, 16, 32, 2048, VCC_IF.X8_3V)); // 256Mb
            FlashDB.Add(new P_NAND("ST NAND256R4A", 0x20, 0x452045U, 512U, 16, 32, 2048, VCC_IF.X16_1V8)); // 256Mb
            FlashDB.Add(new P_NAND("ST NAND256W4A", 0x20, 0x552055U, 512U, 16, 32, 2048, VCC_IF.X16_3V)); // 256Mb
            FlashDB.Add(new P_NAND("ST NAND512R3A", 0x20, 0x362036U, 512U, 16, 32, 4096, VCC_IF.X8_1V8)); // 512Mb
            FlashDB.Add(new P_NAND("ST NAND512W3A", 0x20, 0x762076U, 512U, 16, 32, 4096, VCC_IF.X8_3V)); // 512Mb
            FlashDB.Add(new P_NAND("ST NAND512R4A", 0x20, 0x462046U, 512U, 16, 32, 4096, VCC_IF.X16_1V8)); // 512Mb
            FlashDB.Add(new P_NAND("ST NAND512W4A", 0x20, 0x562056U, 512U, 16, 32, 4096, VCC_IF.X16_3V)); // 512Mb
            FlashDB.Add(new P_NAND("ST NAND01GR3A", 0x20, 0x392039U, 512U, 16, 32, 8192, VCC_IF.X8_1V8)); // 1Gb
            FlashDB.Add(new P_NAND("ST NAND01GW3A", 0x20, 0x792079U, 512U, 16, 32, 8192, VCC_IF.X8_3V)); // 1Gb
            FlashDB.Add(new P_NAND("ST NAND01GR4A", 0x20, 0x492049U, 512U, 16, 32, 8192, VCC_IF.X16_1V8)); // 1Gb
            FlashDB.Add(new P_NAND("ST NAND01GW4A", 0x20, 0x592059U, 512U, 16, 32, 8192, VCC_IF.X16_3V)); // 1Gb
            // Type-2 AddressWrite
            FlashDB.Add(new P_NAND("ST NAND01GR3B", 0x20, 0xA1U, 2048U, 64, 64, 1024, VCC_IF.X8_1V8)); // 1Gb
            FlashDB.Add(new P_NAND("ST NAND01GW3B", 0x20, 0xF1001DU, 2048U, 64, 64, 1024, VCC_IF.X8_3V)); // 1Gb
            FlashDB.Add(new P_NAND("ST NAND01GR4B", 0x20, 0xB1U, 2048U, 64, 64, 1024, VCC_IF.X16_1V8)); // 1Gb
            FlashDB.Add(new P_NAND("ST NAND01GW4B", 0x20, 0xC1U, 2048U, 64, 64, 1024, VCC_IF.X16_3V)); // 1Gb
            FlashDB.Add(new P_NAND("ST NAND02GR3B", 0x20, 0xAAU, 2048U, 64, 64, 2048, VCC_IF.X8_1V8)); // 2Gb
            FlashDB.Add(new P_NAND("ST NAND02GW3B", 0x20, 0xDA801520U, 2048U, 64, 64, 2048, VCC_IF.X8_3V)); // 2Gb
            FlashDB.Add(new P_NAND("ST NAND02GR4B", 0x20, 0xBAU, 2048U, 64, 64, 2048, VCC_IF.X16_1V8)); // 2Gb
            FlashDB.Add(new P_NAND("ST NAND02GW4B", 0x20, 0xCAU, 2048U, 64, 64, 2048, VCC_IF.X16_3V)); // 2Gb
            FlashDB.Add(new P_NAND("ST NAND04GW3B", 0x20, 0xDC109520U, 2048U, 64, 64, 4096, VCC_IF.X8_3V)); // 4Gb
            FlashDB.Add(new P_NAND("ST NAND04GW3B", 0x20, 0xDC809520U, 2048U, 64, 64, 4096, VCC_IF.X8_3V)); // 4Gb
            FlashDB.Add(new P_NAND("ST NAND08GW3B", 0x20, 0xD3C19520U, 2048U, 64, 64, 8192, VCC_IF.X8_3V)); // 4Gb
            // Micron devices
            FlashDB.Add(new P_NAND("Micron MT29F1G08ABAEA", 0x2C, 0xF1809504U, 2048U, 64, 64, 1024, VCC_IF.X8_3V)); // 1Gb
            FlashDB.Add(new P_NAND("Micron MT29F1G08ABBEA", 0x2C, 0xA1801504U, 2048U, 64, 64, 1024, VCC_IF.X8_3V)); // 1Gb
            FlashDB.Add(new P_NAND("Micron MT29F1G08ABADAWP", 0x2C, 0xF1809502U, 2048U, 64, 64, 1024, VCC_IF.X8_3V)); // 1Gb
            FlashDB.Add(new P_NAND("Micron MT29F2G08AAB", 0x2C, 0xDA0015U, 2048U, 64, 64, 2048, VCC_IF.X8_3V)); // 2Gb
            FlashDB.Add(new P_NAND("Micron MT29F2G08ABBFA", 0x2C, 0xAA901504U, 2048U, 224, 64, 2048, VCC_IF.X8_3V)); // 2Gb
            FlashDB.Add(new P_NAND("Micron MT29F2G08ABAFA", 0x2C, 0xDA909504U, 2048U, 224, 64, 2048, VCC_IF.X8_3V)); // 2Gb
            FlashDB.Add(new P_NAND("Micron MT29F2G08ABBEA", 0x2C, 0xAA901506U, 2048U, 64, 64, 2048, VCC_IF.X8_1V8)); // 2Gb
            FlashDB.Add(new P_NAND("Micron MT29F2G16ABBEA", 0x2C, 0xBA905506U, 2048U, 64, 64, 2048, VCC_IF.X16_1V8)); // 2Gb
            FlashDB.Add(new P_NAND("Micron MT29F2G08ABAEA", 0x2C, 0xDA909506U, 2048U, 64, 64, 2048, VCC_IF.X8_3V)); // 2Gb
            FlashDB.Add(new P_NAND("Micron MT29F2G16ABAEA", 0x2C, 0xCA90D506U, 2048U, 64, 64, 2048, VCC_IF.X16_3V)); // 2Gb
            FlashDB.Add(new P_NAND("Micron MT29F2G08ABBEAH4", 0x2C, 0xAA9015U, 2048U, 64, 64, 2048, VCC_IF.X8_1V8)); // 2Gb
            FlashDB.Add(new P_NAND("Micron MT29F2G16ABBEAH4", 0x2C, 0xBA9055U, 2048U, 64, 64, 2048, VCC_IF.X16_1V8)); // 2Gb
            FlashDB.Add(new P_NAND("Micron MT29F2G08ABAEAH4", 0x2C, 0xDA9095U, 2048U, 64, 64, 2048, VCC_IF.X8_3V)); // 2Gb
            FlashDB.Add(new P_NAND("Micron MT29F2G16ABAEAH4", 0x2C, 0xCA90D5U, 2048U, 64, 64, 2048, VCC_IF.X16_3V)); // 2Gb
            FlashDB.Add(new P_NAND("Micron MT29F4G08BAB", 0x2C, 0xDC0015U, 2048U, 64, 64, 4096, VCC_IF.X8_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Micron MT29F4G08AAA", 0x2C, 0xDC909554U, 2048U, 64, 64, 4096, VCC_IF.X8_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Micron MT29F4G08BABWP ", 0x2C, 0xDC801550U, 2048U, 64, 64, 4096, VCC_IF.X8_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Micron MT29F4G08ABA", 0x2C, 0xDC909556U, 2048U, 64, 64, 4096, VCC_IF.X8_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Micron MT29F4G08ABADAWP", 0x2C, 0x90A0B0CU, 2048U, 64, 64, 4096, VCC_IF.X8_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Micron MT29F4G08ABAEA", 0x2C, 0xDC90A654U, 4096U, 224, 64, 2048, VCC_IF.X8_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Micron MT29F4G08ABAEA", 0x2C, 0xDC90A654U, 4096U, 224, 64, 2048, VCC_IF.X8_3V)); // 4Gb (1 page concurrent programming)
            FlashDB.Add(new P_NAND("Micron MT29F4G08ABAEA", 0x2C, 0xDC80A662U, 4096U, 224, 64, 2048, VCC_IF.X8_3V)); // 4Gb (2 page concurrent programming)
            FlashDB.Add(new P_NAND("Micron MT29F4G16ABADA", 0x2C, 0xCC90D556U, 2048U, 64, 64, 4096, VCC_IF.X8_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Micron MT29F8G08DAA", 0x2C, 0xD3909554U, 2048U, 64, 64, 4096, VCC_IF.X8_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Micron MT29F8G08BAA", 0x2C, 0xD3D19558U, 2048U, 64, 64, 8192, VCC_IF.X8_3V)); // 8Gb
            FlashDB.Add(new P_NAND("Micron MT29F8G08ABACA", 0x2C, 0xD390A664U, 4096U, 224, 64, 4096, VCC_IF.X8_3V)); // 8Gb
            FlashDB.Add(new P_NAND("Micron MT29F8G08ABABA", 0x2C, 0x38002685U, 4096U, 224, 128, 2048, VCC_IF.X8_3V)); // 8Gb
            FlashDB.Add(new P_NAND("Micron MT29F16G08CBACA", 0x2C, 0x48044AA5U, 4096U, 224, 256, 2048, VCC_IF.X8_3V)); // 16Gb
            FlashDB.Add(new P_NAND("Micron MT29F32G08CBACAWP", 0x2C, 0x64444BA9U, 4096U, 224, 256, 4096, VCC_IF.X8_3V)); // 32Gb
            FlashDB.Add(new P_NAND("Micron MT29F32G08CBACAWP", 0x2C, 0x68044AA9U, 4096U, 224, 256, 4096, VCC_IF.X8_3V)); // 32Gb
            FlashDB.Add(new P_NAND("Micron MT29F64G08CFACAWP", 0x2C, 0x68044AA9U, 4096U, 224, 256, 8192, VCC_IF.X8_3V)); // 64Gb
            FlashDB.Add(new P_NAND("Micron MT29F64G08CEACAD1", 0x2C, 0x68044AA9U, 4096U, 224, 256, 8192, VCC_IF.X8_3V)); // 64Gb
            FlashDB.Add(new P_NAND("Micron MT29F128G08CXACAD1", 0x2C, 0x68044AA9U, 4096U, 224, 256, 8192, VCC_IF.X8_3V)); // 64Gb
            FlashDB.Add(new P_NAND("Micron MT29F64G08CECCBH1", 0x2C, 0x68044AA9U, 4096U, 224, 256, 8192, VCC_IF.X8_3V)); // 64Gb
            FlashDB.Add(new P_NAND("Micron MT29F64G08CBABA", 0x2C, 0x64444BA9U, 8192U, 744, 256, 4096, VCC_IF.X8_3V)); // 64Gb
            FlashDB.Add(new P_NAND("Micron MT29F64G08CBABB", 0x2C, 0x64444BA9U, 8192U, 744, 256, 4096, VCC_IF.X8_3V)); // 64Gb
            FlashDB.Add(new P_NAND("Micron MT29F64G08CBCBB", 0x2C, 0x64444BA9U, 8192U, 744, 256, 4096, VCC_IF.X8_3V)); // 64Gb
            FlashDB.Add(new P_NAND("Micron MT29F64G08CEACA", 0x2C, 0x64444BA9U, 4096U, 224, 256, 8192, VCC_IF.X8_3V)); // 64Gb
            FlashDB.Add(new P_NAND("Micron MT29F64G08CECCB", 0x2C, 0x64444BA9U, 4096U, 224, 256, 8192, VCC_IF.X8_3V)); // 64Gb
            FlashDB.Add(new P_NAND("Micron MT29F64G08CFACA", 0x2C, 0x64444BA9U, 4096U, 224, 256, 8192, VCC_IF.X8_3V)); // 64Gb
            FlashDB.Add(new P_NAND("Micron MT29F64G08CFACBWP", 0x2C, 0x8805CAA9U, 4096U, 224, 256, 16384, VCC_IF.X8_3V)); // 128Gb
            FlashDB.Add(new P_NAND("Micron MT29F128G08CKCCBH2", 0x2C, 0x68044AA9U, 4096U, 224, 256, 16384, VCC_IF.X8_3V)); // 128Gb
            FlashDB.Add(new P_NAND("Micron MT29F256G08CUCCBH3", 0x2C, 0x8805CAA9U, 4096U, 224, 256, 16384, VCC_IF.X8_3V)); // 128Gb
            FlashDB.Add(new P_NAND("Micron MT29F128G08CECBB", 0x2C, 0x64444BA9U, 8192U, 744, 256, 8192, VCC_IF.X8_3V)); // 128Gb
            FlashDB.Add(new P_NAND("Micron MT29F128G08CFABA", 0x2C, 0x64444BA9U, 8192U, 744, 256, 8192, VCC_IF.X8_3V)); // 128Gb
            FlashDB.Add(new P_NAND("Micron MT29F128G08CFABB", 0x2C, 0x64444BA9U, 8192U, 744, 256, 8192, VCC_IF.X8_3V)); // 128Gb
            FlashDB.Add(new P_NAND("Micron MT29F128G08CXACA", 0x2C, 0x64444BA9U, 4096U, 224, 256, 16384, VCC_IF.X8_3V)); // 128Gb
            FlashDB.Add(new P_NAND("Micron MT29F256G08CJABA", 0x2C, 0x84C54BA9U, 8192U, 744, 256, 16384, VCC_IF.X8_3V)); // 256Gb
            FlashDB.Add(new P_NAND("Micron MT29F256G08CJABB", 0x2C, 0x84C54BA9U, 8192U, 744, 256, 16384, VCC_IF.X8_3V)); // 256Gb
            FlashDB.Add(new P_NAND("Micron MT29F256G08CKCBB", 0x2C, 0x84C54BA9U, 8192U, 744, 256, 16384, VCC_IF.X8_3V)); // 256Gb
            FlashDB.Add(new P_NAND("Micron MT29F256G08CMCBB", 0x2C, 0x64444BA9U, 8192U, 744, 256, 16384, VCC_IF.X8_3V)); // 256Gb
            // Toshiba SLC 8x NAND devices
            FlashDB.Add(new P_NAND("Toshiba TC58DVM92A5TAI0", 0x98, 0x76A5C029U, 512U, 16, 32, 4096, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Toshiba TC58DVM92A5TAI0", 0x98, 0x76A5C021U, 512U, 16, 32, 4096, VCC_IF.X8_3V)); // <---- different revision
            FlashDB.Add(new P_NAND("Toshiba TC58NVG3D4CTGI0", 0x98, 0xD384A5E6U, 2048U, 128, 64, 1024, VCC_IF.X8_3V)); // 1Gb
            FlashDB.Add(new P_NAND("Toshiba TC58BVG0S3HTA00", 0x98, 0xF08014F2U, 2048U, 64, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Toshiba TC58DVG02D5TA00", 0x98, 0xF1001572U, 2048U, 64, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Toshiba TC58NVG0S3ETA00", 0x98, 0xD1901576U, 2048U, 64, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Toshiba TC58NVG0S3HTA00", 0x98, 0xF1801572U, 2048U, 128, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Toshiba TC58NVG0S3HTAI0", 0x98, 0xF1801572U, 2048U, 128, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Toshiba TC58BVG0S3HTAI0", 0x98, 0xF18015F2U, 2048U, 64, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Toshiba TC58NVG1S3HTA00", 0x98, 0xDA901576U, 2048U, 128, 64, 2048, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Toshiba TC58BVG1S3HTA00", 0x98, 0xDA9015F6U, 2048U, 64, 64, 2048, VCC_IF.X8_3V)); // 2Gb
            FlashDB.Add(new P_NAND("Toshiba TC58NVG1S3HTAI0", 0x98, 0xDA901576U, 2048U, 128, 64, 2048, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Toshiba TC58NVG2S0HTA00", 0x98, 0xDC902676U, 4096U, 256, 64, 2048, VCC_IF.X8_3V)); // VERIFY
            FlashDB.Add(new P_NAND("Toshiba TC58NVG2S3ETA00", 0x98, 0xDC901576U, 2048U, 64, 64, 4096, VCC_IF.X8_3V)); // CV
            FlashDB.Add(new P_NAND("Toshiba TC58NVG2S0HTAI0", 0x98, 0xDC902676U, 4096U, 256, 64, 2048, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Toshiba TH58NVG2S3HTA00", 0x98, 0xDC911576U, 2048U, 128, 64, 4096, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Toshiba TC58BVG2S0HTAI0", 0x98, 0xDC9026F6U, 4096U, 128, 64, 2048, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Toshiba TH58NVG3S0HTA00", 0x98, 0xD3912676U, 4096U, 256, 64, 4096, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Toshiba TH58NVG3S0HTAI0", 0x98, 0xD3912676U, 4096U, 256, 64, 4096, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Toshiba TC58NVG3S0FTA00", 0x98, 0xD3902676U, 4096U, 232, 64, 4096, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Toshiba TC58NVG3S0FTA00", 0x98, 0x902676U, 4096U, 232, 64, 4096, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Toshiba TC58NVG6D2HTA00", 0x98, 0xDE948276U, 8192U, 640, 256, 4096, VCC_IF.X8_3V)); // 64Gb
            FlashDB.Add(new P_NAND("Toshiba TC58NVG2S0HBAI4", 0x98, 0xD8902070U, 4096U, 256, 64, 2048, VCC_IF.X8_3V)); //4Gb
            // Winbond SLC 8x NAND devices
            FlashDB.Add(new P_NAND("Winbond W29N01GW", 0xEF, 0xB1805500U, 2048U, 64, 64, 1024, VCC_IF.X8_1V8)); // 1Gb
            FlashDB.Add(new P_NAND("Winbond W29N01GV", 0xEF, 0xF1809500U, 2048U, 64, 64, 1024, VCC_IF.X8_3V)); // 1Gb
            FlashDB.Add(new P_NAND("Winbond W29N01HV", 0xEF, 0xF1009500U, 2048U, 64, 64, 1024, VCC_IF.X8_3V)); // 1Gb
            FlashDB.Add(new P_NAND("Winbond W29N02GV", 0xEF, 0xDA909504U, 2048U, 64, 64, 2048, VCC_IF.X8_3V)); // 2Gb
            FlashDB.Add(new P_NAND("Winbond W29N04GV", 0xEF, 0xDC909554U, 2048U, 64, 64, 4096, VCC_IF.X8_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Winbond W29N08GV", 0xEF, 0xD3919558U, 2048U, 64, 64, 8192, VCC_IF.X8_3V)); // 8Gb
            FlashDB.Add(new P_NAND("Winbond W29N08GV", 0xEF, 0xDC909554U, 2048U, 64, 64, 8192, VCC_IF.X8_3V)); // 8Gb
            FlashDB.Add(new P_NAND("Winbond W29N04GZ", 0xEF, 0xAC901554U, 2048U, 64, 64, 4096, VCC_IF.X8_1V8)); // 4Gb
            FlashDB.Add(new P_NAND("Winbond W29N04GW", 0xEF, 0xBC905554U, 2048U, 64, 64, 4096, VCC_IF.X16_1V8)); // 4Gb
            // Macronix SLC 8x NAND devices
            FlashDB.Add(new P_NAND("MXIC MX30LF1208AA", 0xC2, 0xF0801DU, 2048U, 64, 64, 512, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("MXIC MX30LF1GE8AB", 0xC2, 0xF1809582U, 2048U, 64, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("MXIC MX30UF1G18AC", 0xC2, 0xA1801502U, 2048U, 64, 64, 1024, VCC_IF.X8_1V8));
            FlashDB.Add(new P_NAND("MXIC MX30LF1G18AC", 0xC2, 0xF1809502U, 2048U, 64, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("MXIC MX30LF1G08AA", 0xC2, 0xF1801DU, 2048U, 64, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("MXIC MX30LF2G18AC", 0xC2, 0xDA909506U, 2048U, 64, 64, 2048, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("MXIC MX30UF2G18AC", 0xC2, 0xAA901506U, 2048U, 64, 64, 2048, VCC_IF.X8_1V8));
            FlashDB.Add(new P_NAND("MXIC MX30LF2G28AB", 0xC2, 0xDA909507U, 2048U, 64, 64, 2048, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("MXIC MX30LF2GE8AB", 0xC2, 0xDA909586U, 2048U, 64, 64, 2048, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("MXIC MX30UF2G18AB", 0xC2, 0xBA905506U, 2048U, 64, 64, 2048, VCC_IF.X8_1V8));
            FlashDB.Add(new P_NAND("MXIC MX30UF2G28AB", 0xC2, 0xAA901507U, 2048U, 112, 64, 1024, VCC_IF.X8_1V8));
            FlashDB.Add(new P_NAND("MXIC MX30LF4G18AC", 0xC2, 0xDC909556U, 2048U, 64, 64, 4096, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("MXIC MX30UF4G18AB", 0xC2, 0xAC901556U, 2048U, 64, 64, 4096, VCC_IF.X8_1V8));
            FlashDB.Add(new P_NAND("MXIC MX30LF4G28AB", 0xC2, 0xDC909507U, 2048U, 64, 64, 4096, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("MXIC MX30LF4GE8AB", 0xC2, 0xDC9095D6U, 2048U, 64, 64, 4096, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("MXIC MX30UF4G28AB", 0xC2, 0xAC901557U, 2048U, 112, 64, 2048, VCC_IF.X8_1V8));
            FlashDB.Add(new P_NAND("MXIC MX60LF8G18AC", 0xC2, 0xD3D1955AU, 2048U, 64, 64, 8192, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("MXIC MX60LF8G28AB", 0xC2, 0xD3D1955BU, 2048U, 64, 64, 8192, VCC_IF.X8_3V));
            // Samsung SLC x8 NAND devices
            FlashDB.Add(new P_NAND("Samsung K9F2808U0C", 0xEC, 0x73U, 512U, 16, 32, 1024, VCC_IF.X8_3V)); // 128Mb
            FlashDB.Add(new P_NAND("Samsung K9K2G08U0M", 0xEC, 0xDAC11544U, 2048U, 64, 64, 2048, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Samsung K9K1G08U0M", 0xEC, 0x79A5C0ECU, 512U, 16, 32, 8192, VCC_IF.X8_3V)); // Gb001
            FlashDB.Add(new P_NAND("Samsung K9F1208U0C", 0xEC, 0x765A3F74U, 512U, 16, 32, 4096, VCC_IF.X8_3V)); // Mb512
            FlashDB.Add(new P_NAND("Samsung K9F5608U0D", 0xEC, 0x75A5BDECU, 512U, 16, 32, 2048, VCC_IF.X8_3V)); // Mb256
            FlashDB.Add(new P_NAND("Samsung K9F1G08U0A", 0xEC, 0xF1801540U, 2048U, 64, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Samsung K9F1G08U0B", 0xEC, 0xF1009540U, 2048U, 64, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Samsung K9F1G08U0C", 0xEC, 0xF1009540U, 2048U, 64, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Samsung K9F1G08U0D", 0xEC, 0xF1001540U, 2048U, 64, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Samsung K9F1G08U0C", 0xEC, 0xF1009540U, 2048U, 64, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Samsung K9F1G08B0C", 0xEC, 0xF1009540U, 2048U, 64, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Samsung K9F1G08U0E", 0xEC, 0xF1009541U, 2048U, 64, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Samsung K9F2G08X0", 0xEC, 0xDA101544U, 2048U, 64, 64, 2048, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Samsung K9F2G08U0C", 0xEC, 0xDA109544U, 2048U, 64, 64, 2048, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Samsung K9F2G08U0M", 0xEC, 0xDA8015U, 2048U, 64, 64, 4096, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Samsung K9G8G08U0B", 0xEC, 0xD314A564U, 2048U, 64, 128, 4096, VCC_IF.X8_3V)); // 8Gb
            FlashDB.Add(new P_NAND("Samsung K9W8G08U1M", 0xEC, 0xDCC11554U, 2048U, 64, 64, 4096, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Samsung K9F4G08U0A", 0xEC, 0xDC109554U, 2048U, 64, 64, 4096, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Samsung K9F4G08U0B", 0xEC, 0xDC109554U, 2048U, 64, 64, 4096, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Samsung K9GAG08U0E", 0xEC, 0xD5847250U, 8192U, 436, 128, 2076, VCC_IF.X8_3V)); // 16Gb
            FlashDB.Add(new P_NAND("Samsung K9GAG08U0M", 0xEC, 0xD514B674U, 4096U, 128, 128, 4096, VCC_IF.X8_3V)); // 16Gb
            FlashDB.Add(new P_NAND("Samsung K9K8G08U0A", 0xEC, 0xD3519558U, 2048U, 64, 64, 8192, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Samsung K9KAG08U0M", 0xEC, 0xD551A668U, 4096U, 128, 64, 8192, VCC_IF.X8_3V)); // 8Gb
            FlashDB.Add(new P_NAND("Samsung K9WAG08U1A", 0xEC, 0xD3519558U, 2048U, 64, 64, 8192, VCC_IF.X8_3V)); // 8Gb
            FlashDB.Add(new P_NAND("Samsung K9K8G08U0A", 0xEC, 0xD3519558U, 2048U, 64, 64, 8192, VCC_IF.X8_3V)); // 16Gb Dual die (CE1#/CE2#)
            FlashDB.Add(new P_NAND("Samsung K9NBG08U5A", 0xEC, 0xD3519558U, 2048U, 64, 64, 8192, VCC_IF.X8_3V)); // 32Gb Quad die (CE1#/CE2#/CE3#/CE4#)
            // Hynix SLC x8 devices
            FlashDB.Add(new P_NAND("Hynix HY27US08281A", 0xAD, 0x73AD73ADU, 512U, 16, 32, 1024, VCC_IF.X16_3V)); // 128Mb
            FlashDB.Add(new P_NAND("Hynix HY27US08561A", 0xAD, 0x75AD75ADU, 512U, 16, 32, 2048, VCC_IF.X8_3V)); // 256Mb
            FlashDB.Add(new P_NAND("Hynix HY27US16561A", 0xAD, 0x55AD55ADU, 512U, 16, 32, 2048, VCC_IF.X16_3V)); // 256Mb
            FlashDB.Add(new P_NAND("Hynix HY27SS08561A", 0xAD, 0x35AD35ADU, 512U, 16, 32, 2048, VCC_IF.X8_1V8)); // 256Mb
            FlashDB.Add(new P_NAND("Hynix HY27SS16561A", 0xAD, 0x45AD45ADU, 512U, 16, 32, 2048, VCC_IF.X16_1V8)); // 256Mb
            FlashDB.Add(new P_NAND("Hynix HY27US08121B", 0xAD, 0x76AD76ADU, 512U, 16, 32, 4096, VCC_IF.X8_3V)); // 512Mb
            FlashDB.Add(new P_NAND("Hynix HY27UF081G2A", 0xAD, 0xF1805DADU, 2048U, 64, 64, 1024, VCC_IF.X8_3V)); // 1Gb
            FlashDB.Add(new P_NAND("Hynix HY27UF161G2A", 0xAD, 0xF1805DADU, 2048U, 64, 64, 1024, VCC_IF.X16_3V)); // 1Gb
            FlashDB.Add(new P_NAND("Hynix H27U1G8F2B", 0xAD, 0xF1001DU, 2048U, 64, 64, 1024, VCC_IF.X8_3V)); // 1Gb
            FlashDB.Add(new P_NAND("Hynix H27U1G8F2CTR", 0xAD, 0xF1801DADU, 2048U, 64, 64, 1024, VCC_IF.X8_3V)); // 1Gb
            FlashDB.Add(new P_NAND("Hynix HY27UF081G2M", 0xAD, 0xF10015ADU, 2048U, 64, 64, 1024, VCC_IF.X8_3V)); // 1Gb
            FlashDB.Add(new P_NAND("Hynix HY27US081G1M", 0xAD, 0x79A500U, 512U, 16, 32, 8192, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Hynix HY27SF081G2M", 0xAD, 0xA10015U, 2048U, 64, 64, 1024, VCC_IF.X8_3V)); // 1Gb
            FlashDB.Add(new P_NAND("Hynix HY27UF082G2B", 0xAD, 0xDA109544U, 2048U, 64, 64, 2048, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Hynix HY27UF082G2A", 0xAD, 0xDA801D00U, 2048U, 64, 64, 2048, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Hynix H27UAG8T2M", 0xAD, 0xD514B644U, 4096U, 128, 128, 4096, VCC_IF.X8_3V)); // 16Gb
            FlashDB.Add(new P_NAND("Hynix H27UAG8T2B", 0xAD, 0xD5949A74U, 8192U, 448, 256, 512, VCC_IF.X8_3V)); // 16Gb
            FlashDB.Add(new P_NAND("Hynix H27U2G8F2C", 0xAD, 0xDA909546U, 2048U, 64, 64, 2048, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Hynix H27U2G8F2C", 0xAD, 0xDA909544U, 2048U, 64, 64, 2048, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Hynix H27U2G6F2C", 0xAD, 0xCA90D544U, 2048U, 64, 64, 2048, VCC_IF.X16_3V));
            FlashDB.Add(new P_NAND("Hynix H27S2G8F2C", 0xAD, 0xAA901544U, 2048U, 64, 64, 2048, VCC_IF.X8_1V8));
            FlashDB.Add(new P_NAND("Hynix H27S2G6F2C", 0xAD, 0xBA905544U, 2048U, 64, 64, 2048, VCC_IF.X16_1V8));
            FlashDB.Add(new P_NAND("Hynix H27UBG8T2B", 0xAD, 0xD794DA74U, 8192U, 640, 256, 2048, VCC_IF.X8_3V)); // 32Gb 16Mbit blocks
            FlashDB.Add(new P_NAND("Hynix H27UBG8T2C", 0xAD, 0xD7949160U, 8192U, 640, 256, 2048, VCC_IF.X8_3V)); // 32Gb
            FlashDB.Add(new P_NAND("Hynix H27U4G8F2D", 0xAD, 0xDC909554U, 2048U, 64, 64, 4096, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Hynix H27U4G6F2D", 0xAD, 0xCC90D554U, 2048U, 64, 64, 4096, VCC_IF.X16_3V));
            FlashDB.Add(new P_NAND("Hynix H27S4G8F2D", 0xAD, 0xAC901554U, 2048U, 64, 64, 4096, VCC_IF.X8_1V8));
            FlashDB.Add(new P_NAND("Hynix H27S4G6F2D", 0xAD, 0xBC905554U, 2048U, 64, 64, 4096, VCC_IF.X16_1V8));
            FlashDB.Add(new P_NAND("Hynix H27UCG8T2FTR", 0xAD, 0xDE14AB42U, 8192U, 640, 256, 4180, VCC_IF.X8_3V)); // 64Gb
            FlashDB.Add(new P_NAND("Hynix HY27UG084G2M", 0xAD, 0xDC0015U, 2048U, 64, 64, 4096, VCC_IF.X8_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Hynix HY27UG084GDM", 0xAD, 0xDA0015U, 2048U, 64, 64, 4096, VCC_IF.X8_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Hynix HY27UG164G2M", 0xAD, 0xDC0055U, 2048U, 64, 64, 4096, VCC_IF.X16_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Hynix HY27UF084G2M", 0xAD, 0xDC8095U, 2048U, 64, 64, 4096, VCC_IF.X8_3V)); // 4Gb
            // Spansion SLC 34 series
            FlashDB.Add(new P_NAND("Cypress S34ML01G1", 0x1, 0xF1001DU, 2048U, 64, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Cypress S34ML02G1", 0x1, 0xDA9095U, 2048U, 64, 64, 2048, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Cypress S34ML04G1", 0x1, 0xDC9095U, 2048U, 64, 64, 4096, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Cypress S34ML01G2", 0x1, 0xF1801DU, 2048U, 64, 64, 1024, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Cypress S34ML02G2", 0x1, 0xDA909546U, 2048U, 128, 64, 2048, VCC_IF.X8_3V)); // 2Gb
            FlashDB.Add(new P_NAND("Cypress S34ML04G2", 0x1, 0xDC909556U, 2048U, 128, 64, 4096, VCC_IF.X8_3V));
            FlashDB.Add(new P_NAND("Cypress S34ML01G2", 0x1, 0xC1805DU, 2048U, 64, 64, 1024, VCC_IF.X16_3V));
            FlashDB.Add(new P_NAND("Cypress S34ML02G2", 0x1, 0xCA90D546U, 2048U, 128, 64, 2048, VCC_IF.X16_3V));
            FlashDB.Add(new P_NAND("Cypress S34ML04G2", 0x1, 0xCC90D556U, 2048U, 128, 64, 4096, VCC_IF.X16_3V));
            FlashDB.Add(new P_NAND("Cypress S34ML04G3", 0x1, 0xDC000504U, 2048, 128, 64, 4096, VCC_IF.X16_3V));
            FlashDB.Add(new P_NAND("Cypress S34MS01G200", 0x1, 0xA18015U, 2048U, 64, 64, 4096, VCC_IF.X8_1V8));
            FlashDB.Add(new P_NAND("Cypress S34MS02G200", 0x1, 0xAA901546U, 2048U, 64, 64, 4096, VCC_IF.X8_1V8));
            FlashDB.Add(new P_NAND("Cypress S34MS04G200", 0x1, 0xAC901556U, 2048U, 64, 64, 4096, VCC_IF.X8_1V8));
            FlashDB.Add(new P_NAND("Cypress S34MS01G204", 0x1, 0xB18055U, 2048U, 64, 64, 4096, VCC_IF.X16_1V8));
            FlashDB.Add(new P_NAND("Cypress S34MS02G204", 0x1, 0xBA905546U, 2048U, 128, 64, 4096, VCC_IF.X16_1V8));
            FlashDB.Add(new P_NAND("Cypress S34MS04G204", 0x1, 0xBC905556U, 2048U, 128, 64, 4096, VCC_IF.X16_1V8));
            // Dosilicon
            FlashDB.Add(new P_NAND("Dosilicon FMND1G08U3D", 0xF8, 0xF18095U, 2048U, 64, 64, 1024, VCC_IF.X8_3V)); // 1Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND1G16U3D", 0xF8, 0xC180D5U, 2048U, 64, 64, 1024, VCC_IF.X16_3V)); // 1Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND1G08S3D", 0xF8, 0xA18015U, 2048U, 64, 64, 1024, VCC_IF.X8_1V8)); // 1Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND1G16S3D", 0xF8, 0xB18055U, 2048U, 64, 64, 1024, VCC_IF.X16_1V8)); // 1Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND2G08U3D", 0xF8, 0xDA909546U, 2048U, 64, 64, 2048, VCC_IF.X8_3V)); // 2Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND2G16U3D", 0xF8, 0xCA90D546U, 2048U, 64, 64, 2048, VCC_IF.X16_3V)); // 2Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND2G08S3D", 0xF8, 0xAA901546U, 2048U, 64, 64, 2048, VCC_IF.X8_1V8)); // 2Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND2G16S3D", 0xF8, 0xBA905546U, 2048U, 64, 64, 2048, VCC_IF.X16_1V8)); // 2Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND4G08U3B", 0xF8, 0xDC909546U, 2048U, 64, 64, 4096, VCC_IF.X8_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND4G16U3B", 0xF8, 0xCC90D546U, 2048U, 64, 64, 4096, VCC_IF.X16_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND4G08S3B", 0xF8, 0xAC901546U, 2048U, 64, 64, 4096, VCC_IF.X8_1V8)); // 4Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND4G16S3B", 0xF8, 0xBC905546U, 2048U, 64, 64, 4096, VCC_IF.X16_1V8)); // 4Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND4G08U3C", 0xF8, 0xDC909546U, 2048U, 128, 64, 4096, VCC_IF.X8_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND4G16U3C", 0xF8, 0xCC90D546U, 2048U, 128, 64, 4096, VCC_IF.X16_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND4G08S3C", 0xF8, 0xAC901546U, 2048U, 128, 64, 4096, VCC_IF.X8_1V8)); // 4Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND4G16S3C", 0xF8, 0xBC905546U, 2048U, 128, 64, 4096, VCC_IF.X16_1V8)); // 4Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND4G08U3F", 0xF8, 0xDC80A662U, 4096U, 256, 64, 2048, VCC_IF.X8_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND4G16U3F", 0xF8, 0xCC80E662U, 4096U, 256, 64, 2048, VCC_IF.X16_3V)); // 4Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND4G08S3F", 0xF8, 0xAC802662U, 4096U, 256, 64, 2048, VCC_IF.X8_1V8)); // 4Gb
            FlashDB.Add(new P_NAND("Dosilicon FMND4G16S3F", 0xF8, 0xBC806662U, 4096U, 256, 64, 2048, VCC_IF.X16_1V8)); // 4Gb
            FlashDB.Add(new P_NAND("Dosilicon DSND8G08U3N", 0xE5, 0xD3C1A666U, 4096U, 256, 64, 2048 * 2, VCC_IF.X8_3V)); // 8Gb (dual die)
            FlashDB.Add(new P_NAND("Dosilicon DSND8G16U3N", 0xE5, 0xC3C1E666U, 4096U, 256, 64, 2048 * 2, VCC_IF.X16_3V)); // 8Gb (dual die)
            FlashDB.Add(new P_NAND("Dosilicon DSND8G08S3N", 0xE5, 0xA3C12666U, 4096U, 256, 64, 2048 * 2, VCC_IF.X8_1V8)); // 8Gb (dual die)
            FlashDB.Add(new P_NAND("Dosilicon DSND8G16S3N", 0xE5, 0xB3C16666U, 4096U, 256, 64, 2048 * 2, VCC_IF.X16_1V8)); // 8Gb (dual die)
            FlashDB.Add(new P_NAND("Dosilicon DSND8G08U3M", 0xE5, 0xD3D1955AU, 2048U, 64, 64, 4096 * 2, VCC_IF.X8_3V)); // 8Gb (dual die)
            FlashDB.Add(new P_NAND("Dosilicon DSND8G16U3M", 0xE5, 0xC3D1D55AU, 2048U, 64, 64, 4096 * 2, VCC_IF.X16_3V)); // 8Gb (dual die)
            FlashDB.Add(new P_NAND("Dosilicon DSND8G08S3M", 0xE5, 0xA3D1155AU, 2048U, 64, 64, 4096 * 2, VCC_IF.X8_1V8)); // 8Gb (dual die)
            FlashDB.Add(new P_NAND("Dosilicon DSND8G16S3M", 0xE5, 0xB3D1555AU, 2048U, 64, 64, 4096 * 2, VCC_IF.X16_1V8)); // 8Gb (dual die)
            // Others
            FlashDB.Add(new P_NAND("FORESEE FS33ND01GS1", 0xEC, 0xF1009542U, 2048U, 64, 64, 1024, VCC_IF.X8_3V)); // 1Gb
            FlashDB.Add(new P_NAND("Zentel A5U1GA31ATS", 0x92, 0xF1809540U, 2048U, 64, 64, 1024, VCC_IF.X8_3V)); // 1Gb (ESMT branded)
            FlashDB.Add(new P_NAND("ESMT F59L1G81MA", 0xC8, 0xD1809540U, 2048U, 64, 64, 1024, VCC_IF.X8_3V)); // 1Gb
            FlashDB.Add(new P_NAND("ESMT F59L1G81LA", 0xC8, 0xD1809542U, 2048U, 64, 64, 1024, VCC_IF.X8_3V)); // 1Gb
            FlashDB.Add(new P_NAND("ESMT F59L2G81A", 0xC8, 0xDA909544U, 2048U, 64, 64, 2048, VCC_IF.X8_3V)); // 2Gb

            // FlashDB.Add(New P_NAND("SanDisk SDTNPNAHEM-008G", &H45, &HDE989272UI, 8192, 1024, 516, 2084, VCC_IF.X8_3V) With {.ADDRESS_SCHEME = 3}) '64Gb

        }

        private void OTP_Database()
        {
            FlashDB.Add(new OTP_EPROM("ATMEL AT27C010", VCC_IF.X8_5V_12VPP, 0x1E, 0x5, Constants.Mb001, 60));
            FlashDB.Add(new OTP_EPROM("ATMEL AT27C020", VCC_IF.X8_5V_12VPP, 0x1E, 0x86, Constants.Mb002, 60));
            FlashDB.Add(new OTP_EPROM("ATMEL AT27C040", VCC_IF.X8_5V_12VPP, 0x1E, 0xB, Constants.Mb004, 60));
            FlashDB.Add(new OTP_EPROM("ATMEL AT27C516", VCC_IF.X16_5V_12VPP, 0x1E, 0xF2, Constants.Kb512, 60));
            FlashDB.Add(new OTP_EPROM("ATMEL AT27C1024", VCC_IF.X16_5V_12VPP, 0x1E, 0xF1, Constants.Mb001, 60));
            FlashDB.Add(new OTP_EPROM("ATMEL AT27C2048", VCC_IF.X16_5V_12VPP, 0x1E, 0xF7, Constants.Mb002, 60));
            FlashDB.Add(new OTP_EPROM("ATMEL AT27C4096", VCC_IF.X16_5V_12VPP, 0x1E, 0xF4, Constants.Mb004, 60));
            FlashDB.Add(new OTP_EPROM("ST M27C1024", VCC_IF.X16_5V_12VPP, 0x20, 0x8C, Constants.Mb001, 60));
            FlashDB.Add(new OTP_EPROM("ST M27C256B", VCC_IF.X8_5V_12VPP, 0x20, 0x8D, Constants.Kb256, 60));
            FlashDB.Add(new OTP_EPROM("ST M27C512", VCC_IF.X8_5V_12VPP, 0x20, 0x3D, Constants.Kb512, 60));
            FlashDB.Add(new OTP_EPROM("ST M27C1001", VCC_IF.X8_5V_12VPP, 0x20, 0x5, Constants.Mb001, 60));
            FlashDB.Add(new OTP_EPROM("ST M27C2001", VCC_IF.X8_5V_12VPP, 0x20, 0x61, Constants.Mb002, 60));
            FlashDB.Add(new OTP_EPROM("ST M27C4001", VCC_IF.X8_5V_12VPP, 0x20, 0x41, Constants.Mb004, 60));
            FlashDB.Add(new OTP_EPROM("ST M27C801", VCC_IF.X8_5V_12VPP, 0x20, 0x42, Constants.Mb008, 60));
            FlashDB.Add(new OTP_EPROM("ST M27C160", VCC_IF.X16_5V_12VPP, 0x20, 0xB1, Constants.Mb016, 60)); // DIP42,SO44,PLCC44
            FlashDB.Add(new OTP_EPROM("ST M27C322", VCC_IF.X16_5V_12VPP, 0x20, 0x34, Constants.Mb032, 60)); // DIP42
        }

        private void FWH_Database()
        {
            FlashDB.Add(new FWH("Atmel AT49LH002", 0x1F, 0xE9, Constants.Mb002, Constants.Kb512, 0x20));
            FlashDB.Add(new FWH("Atmel AT49LH004", 0x1F, 0xEE, Constants.Mb004, Constants.Kb512, 0x20));
            FlashDB.Add(new FWH("Winbond W39V040FA", 0xDA, 0x34, Constants.Mb004, Constants.Kb032, 0x50)); // CV
            FlashDB.Add(new FWH("Winbond W39V080FA", 0xDA, 0xD3, Constants.Mb008, Constants.Kb032, 0x50));
            FlashDB.Add(new FWH("Microchip SST49LF002A", 0xBF, 0x57, Constants.Mb002, Constants.Kb032, 0x30));
            FlashDB.Add(new FWH("Microchip SST49LF003A", 0xBF, 0x1B, Constants.Mb003, Constants.Kb032, 0x30));
            FlashDB.Add(new FWH("Microchip SST49LF004A", 0xBF, 0x60, Constants.Mb004, Constants.Kb032, 0x30));
            FlashDB.Add(new FWH("Microchip SST49LF008A", 0xBF, 0x5A, Constants.Mb008, Constants.Kb032, 0x30));
            FlashDB.Add(new FWH("Microchip SST49LF080A", 0xBF, 0x5B, Constants.Mb008, Constants.Kb032, 0x30)); // CV
            FlashDB.Add(new FWH("Microchip SST49LF016C", 0xBF, 0x5C, Constants.Mb016, Constants.Kb032, 0x30));
            FlashDB.Add(new FWH("ISSI PM49FL002", 0x9D, 0x6D, Constants.Mb002, Constants.Kb032, 0x30));
            FlashDB.Add(new FWH("ISSI PM49FL004", 0x9D, 0x6E, Constants.Mb004, Constants.Kb032, 0x30));
            FlashDB.Add(new FWH("ISSI PM49FL008", 0x9D, 0x6A, Constants.Mb008, Constants.Kb032, 0x30));
        }

        private void SPIEEPROM_Database() {
            SPI_NOR spi_dev;

            spi_dev = new SPI_NOR("Renesas X5083", VCC_IF.SERIAL_3V, 1048, 0, 0); //Available in 3V and 5V parts
            spi_dev.Enable_EEPROM_Mode(16, 8U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Nordic nRF24LE1", VCC_IF.SERIAL_3V, 16384U, 0, 0, 0x52, 512U);
            spi_dev.Enable_EEPROM_Mode(512, 16U, true, SPI_ProgramMode.Nordic);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Nordic nRF24LU1+ (16KB)", VCC_IF.SERIAL_3V, 16384U, 0, 0, 0x52, 512U);
            spi_dev.Enable_EEPROM_Mode(256, 16U, true, SPI_ProgramMode.Nordic);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Nordic nRF24LU1+ (32KB)", VCC_IF.SERIAL_3V, 32768U, 0, 0, 0x52, 512U);
            spi_dev.Enable_EEPROM_Mode(256, 16U, true, SPI_ProgramMode.Nordic);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Atmel AT25010A", VCC_IF.SERIAL_3V, 128U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(8, 8U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Atmel AT25020A", VCC_IF.SERIAL_3V, 256U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(8, 8U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Atmel AT25040A", VCC_IF.SERIAL_3V, 512U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(8, 8U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Atmel AT25080", VCC_IF.SERIAL_3V, 1024U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(8, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Atmel AT25160", VCC_IF.SERIAL_3V, 2048U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(32, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Atmel AT25320", VCC_IF.SERIAL_3V, 4096U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(32, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Atmel AT25640", VCC_IF.SERIAL_3V, 8192U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(32, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Atmel AT25128B", VCC_IF.SERIAL_3V, 16384U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(64, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Atmel AT25256B", VCC_IF.SERIAL_3V, 32768U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(64, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Atmel AT25512", VCC_IF.SERIAL_3V, 65536U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(128, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("ST M95010", VCC_IF.SERIAL_3V, 128U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(16, 8U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("ST M95020", VCC_IF.SERIAL_3V, 256U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(16, 8U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("ST M95040", VCC_IF.SERIAL_3V, 512U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(16, 8U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("ST M95080", VCC_IF.SERIAL_3V, 1024U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(32, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("ST M95160", VCC_IF.SERIAL_3V, 2048U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(32, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("ST M95320", VCC_IF.SERIAL_3V, 4096U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(32, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("ST M95640", VCC_IF.SERIAL_3V, 8192U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(32, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("ST M95128", VCC_IF.SERIAL_3V, 16384U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(64, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("ST M95256", VCC_IF.SERIAL_3V, 32768U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(64, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("ST M95512", VCC_IF.SERIAL_3V, 65536U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(128, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("ST M95M01", VCC_IF.SERIAL_3V, 131072U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(256, 24U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("ST M95M02", VCC_IF.SERIAL_3V, 262144U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(256, 24U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Microchip 25AA160A", VCC_IF.SERIAL_3V, 2048U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(16, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Microchip 25AA160B", VCC_IF.SERIAL_3V, 2048U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(32, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Microchip 25XX320", VCC_IF.SERIAL_3V, 4096U, 0, 0); // 25AA320/25LC320/25C320
            spi_dev.Enable_EEPROM_Mode(32, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Microchip 25XX640", VCC_IF.SERIAL_3V, 8192U, 0, 0); // 25AA640/25LC640
            spi_dev.Enable_EEPROM_Mode(32, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Microchip 25XX512", VCC_IF.SERIAL_3V, 65536U, 0, 0); // 25AA512
            spi_dev.Enable_EEPROM_Mode(128, 16U, false);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("Microchip 25XX1024", VCC_IF.SERIAL_3V, 131072U, 0, 0, 0xD8, 0x8000U); // 25AA1024/25LC1024
            spi_dev.Enable_EEPROM_Mode(256, 24U, true, SPI_ProgramMode.PageMode);
            FlashDB.Add(spi_dev);
            spi_dev = new SPI_NOR("XICOR X25650", VCC_IF.SERIAL_3V, 8192U, 0, 0);
            spi_dev.Enable_EEPROM_Mode(32, 16U, false);
            FlashDB.Add(spi_dev);
        }

        private void MICROWIRE_Database()
        {
            FlashDB.Add(new MICROWIRE("Generic 93xx46A", 128U, 7, 0)); // X8 ONLY
            FlashDB.Add(new MICROWIRE("Generic 93xx46B", 128U, 0, 6)); // X16 ONLY
            FlashDB.Add(new MICROWIRE("Generic 93xx46C", 128U, 7, 6)); // X8/X16
            FlashDB.Add(new MICROWIRE("Generic 93xx56A", 256U, 9, 0)); // X8 ONLY
            FlashDB.Add(new MICROWIRE("Generic 93xx56B", 256U, 0, 8)); // X16 ONLY
            FlashDB.Add(new MICROWIRE("Generic 93xx56C", 256U, 9, 8)); // X8/X16
            FlashDB.Add(new MICROWIRE("Generic 93xx66A", 512U, 9, 0)); // X8 ONLY
            FlashDB.Add(new MICROWIRE("Generic 93xx66B", 512U, 0, 8)); // X16 ONLY
            FlashDB.Add(new MICROWIRE("Generic 93xx66C", 512U, 9, 8)); // X8/X16
            FlashDB.Add(new MICROWIRE("Generic 93xx76A", 1024U, 10, 0)); // X8 ONLY
            FlashDB.Add(new MICROWIRE("Generic 93xx76B", 1024U, 0, 9)); // X16 ONLY
            FlashDB.Add(new MICROWIRE("Generic 93xx76C", 1024U, 10, 9)); // X8/X16
            FlashDB.Add(new MICROWIRE("Generic 93xx86A", 2048U, 11, 0)); // X8 ONLY
            FlashDB.Add(new MICROWIRE("Generic 93xx86B", 2048U, 0, 10)); // X16 ONLY
            FlashDB.Add(new MICROWIRE("Generic 93xx86C", 2048U, 11, 10)); // X8/X16
        }
        // Helper function to create the proper definition for Atmel/Adesto Series 45 SPI devices
        private SPI_NOR CreateSeries45(string atName, uint mbitsize, ushort id1, ushort id2, uint page_size)
        {
            var atmel_spi = new SPI_NOR(atName, SPI_3V, mbitsize, 0x1F, id1, 0x50, (uint)(page_size * 8L));
            atmel_spi.ID2 = id2;
            atmel_spi.PAGE_SIZE = page_size;
            atmel_spi.PAGE_SIZE_EXTENDED = (uint)(page_size + page_size / 32d); // Additional bytes available per page
            atmel_spi.ProgramMode = SPI_ProgramMode.Atmel45Series;  // Atmel Series 45
            atmel_spi.OP_COMMANDS.RDSR = 0xD7;
            atmel_spi.OP_COMMANDS.READ = 0xE8;
            atmel_spi.OP_COMMANDS.PROG = 0x12;
            return atmel_spi;
        }

        public Device FindDevice(byte MFG, ushort ID1, ushort ID2, MemoryType DEVICE, byte FM = 0)
        {
            switch (DEVICE)
            {
                case MemoryType.PARALLEL_NAND:
                    {
                        foreach (var flash in MemDeviceSelect(MemoryType.PARALLEL_NAND))
                        {
                            if (flash.MFG_CODE == MFG)
                            {
                                if (NAND_DeviceMatches(ID1, ID2, flash.ID1, flash.ID2))
                                {
                                    return flash;
                                }
                            }
                        }

                        break;
                    }

                case MemoryType.SERIAL_NAND:
                    {
                        foreach (var flash in MemDeviceSelect(MemoryType.SERIAL_NAND))
                        {
                            if (SNAND_DeviceMatches(MFG, ID1, flash.MFG_CODE, flash.ID1))
                            {
                                return flash;
                            }
                        }

                        break;
                    }

                case MemoryType.SERIAL_NOR:
                    {
                        var list = new List<SPI_NOR>();
                        foreach (var flash in MemDeviceSelect(MemoryType.SERIAL_NOR))
                        {
                            if (flash.MFG_CODE == MFG)
                            {
                                if (flash.ID1 == ID1)
                                {
                                    list.Add((SPI_NOR)flash);
                                }
                            }
                        }

                        if (list.Count == 1)
                            return list[0];
                        if (list.Count > 1) // Find the best match
                        {
                            foreach (var flash in list)
                            {
                                if (flash.ID2 == ID2 && flash.FAMILY == FM)
                                    return flash;
                            }

                            foreach (var flash in list)
                            {
                                if (flash.ID2 == ID2)
                                    return flash;
                            }

                            return list[0];
                        }

                        break;
                    }

                case var @case when @case == (MemoryType.PARALLEL_NOR | MemoryType.OTP_EPROM):
                    {
                        foreach (var flash in MemDeviceSelect(DEVICE))
                        {
                            if (flash.MFG_CODE == MFG)
                            {
                                if (ID2 == 0) // Only checks the LSB of ID1 (and ignore ID2)
                                {
                                    if (flash.ID1 == ID1)
                                        return flash;
                                    if (ID1 >> 8 == 0 || ID1 >> 8 == 255)
                                    {
                                        if ((ID1 & 255) == (flash.ID1 & 255))
                                            return flash;
                                    }
                                }
                                else if (flash.ID1 == ID1)
                                {
                                    if (flash.ID2 == 0 || flash.ID2 == ID2)
                                        return flash;
                                }
                            }
                        }

                        break;
                    }

                default:
                    {
                        foreach (var flash in MemDeviceSelect(DEVICE))
                        {
                            if (flash.MFG_CODE == MFG && flash.ID1 == ID1)
                                return flash;
                        }

                        break;
                    }
            }

            return null; // Not found
        }

        public Device[] FindDevices(byte MFG, ushort ID1, ushort ID2, MemoryType DEVICE)
        {
            var devices = new List<Device>();
            foreach (var flash in FlashDB)
            {
                if (flash.FLASH_TYPE == DEVICE)
                {
                    if (flash.MFG_CODE == MFG)
                    {
                        if (flash.FLASH_TYPE == MemoryType.PARALLEL_NAND)
                        {
                            if (NAND_DeviceMatches(ID1, ID2, flash.ID1, flash.ID2))
                            {
                                devices.Add(flash);
                            }
                        }
                        else if (flash.ID1 == ID1)
                        {
                            if (flash.ID2 == 0 || flash.ID2 == ID2)
                            {
                                devices.Add(flash);
                            }
                        }
                    }
                }
            }

            return devices.ToArray();
        }

        private bool NAND_DeviceMatches(ushort TARGET_ID1, ushort TARGET_ID2, ushort LIST_ID1, ushort LIST_ID2)
        {
            uint NAND_ID = (uint)TARGET_ID1 << 16 | TARGET_ID2;
            uint LIST_ID = (uint)LIST_ID1 << 16 | LIST_ID2;
            bool Matched = true;
            for (int i = 0; i <= 3; i++)
            {
                byte B1 = (byte)(NAND_ID >> (3 - i) * 8 & 255L);
                byte B2 = (byte)(LIST_ID >> (3 - i) * 8 & 255L);
                if (B2 == 0) // If this is 0, we do not check
                {
                }
                else if (B1 == B2)
                {
                }
                else
                {
                    Matched = false;
                    break;
                } // Not a match
            }

            return Matched;
        }

        private bool SNAND_DeviceMatches(byte TARGET_MFG, ushort TARGET_ID1, byte LIST_MFG, ushort LIST_ID1)
        {
            if (TARGET_MFG == LIST_MFG)
            {
                if (TARGET_ID1 == LIST_ID1)
                {
                    return true;
                }
            }
            else if (TARGET_MFG == LIST_ID1)
            {
                if (LIST_MFG == TARGET_ID1)
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerable<Device> MemDeviceSelect(MemoryType m_type)
        {
            foreach (var flash in FlashDB)
            {
                if (m_type == MemoryType.UNSPECIFIED)
                {
                    yield return flash;
                }
                else if (m_type == flash.FLASH_TYPE)
                {
                    yield return flash;
                }
            }
        }
        // Returns all of the devices that match the device type
        public Device[] GetFlashDevices(MemoryType type)
        {
            var dev = new List<Device>();
            if (type == MemoryType.PARALLEL_NOR) // Search only CFI devices
            {
                foreach (var flash in FlashDB)
                {
                    if (flash.FLASH_TYPE == MemoryType.PARALLEL_NOR)
                    {
                        dev.Add(flash);
                    }
                }
            }
            else if (type == MemoryType.OTP_EPROM)
            {
                foreach (var flash in FlashDB)
                {
                    if (flash.FLASH_TYPE == MemoryType.OTP_EPROM)
                    {
                        dev.Add(flash);
                    }
                }
            }
            else if (type == MemoryType.SERIAL_NOR)
            {
                foreach (var flash in FlashDB)
                {
                    if (flash.FLASH_TYPE == MemoryType.SERIAL_NOR)
                    {
                        dev.Add(flash);
                    }
                }
            }
            else if (type == MemoryType.SERIAL_NAND)
            {
                foreach (var flash in FlashDB)
                {
                    if (flash.FLASH_TYPE == MemoryType.SERIAL_NAND)
                    {
                        dev.Add(flash);
                    }
                }
            }
            else if (type == MemoryType.PARALLEL_NAND)
            {
                foreach (var flash in FlashDB)
                {
                    if (flash.FLASH_TYPE == MemoryType.PARALLEL_NAND)
                    {
                        dev.Add(flash);
                    }
                }
            }
            else if (type == MemoryType.FWH_NOR)
            {
                foreach (var flash in FlashDB)
                {
                    if (flash.FLASH_TYPE == MemoryType.FWH_NOR)
                    {
                        dev.Add(flash);
                    }
                }
            }
            else if (type == MemoryType.HYPERFLASH)
            {
                foreach (var flash in FlashDB)
                {
                    if (flash.FLASH_TYPE == MemoryType.HYPERFLASH)
                    {
                        dev.Add(flash);
                    }
                }
            }
            else if (type == MemoryType.SERIAL_MICROWIRE)
            {
                foreach (var flash in FlashDB)
                {
                    if (flash.FLASH_TYPE == MemoryType.SERIAL_MICROWIRE)
                    {
                        dev.Add(flash);
                    }
                }
            }

            return dev.ToArray();
        }

        private DeviceCollection[] SortFlashDevices(Device[] devices)
        {
            var GrowingCollection = new List<DeviceCollection>();
            foreach (var dev in devices)
            {
                bool SkipAdd = false;
                if (dev.FLASH_TYPE == MemoryType.SERIAL_NOR)
                {
                    if (((SPI_NOR)dev).ProgramMode == SPI_ProgramMode.SPI_EEPROM)
                    {
                        SkipAdd = true;
                    }
                    else if (((SPI_NOR)dev).ProgramMode == SPI_ProgramMode.Nordic)
                    {
                        SkipAdd = true;
                    }
                }

                if (!SkipAdd)
                {
                    string Manu = dev.NAME;
                    if (Manu.Contains(" "))
                        Manu = Manu.Substring(0, Manu.IndexOf(" "));
                    // Dim Part As String = dev.NAME.Substring(Manu.Length + 1)
                    var s = DevColIndexOf(ref GrowingCollection, Manu);
                    if (s is null)
                    {
                        var new_item = new DeviceCollection();
                        new_item.Name = Manu;
                        new_item.Parts = new[] { dev };
                        GrowingCollection.Add(new_item);
                    }
                    else if (s.Parts is null) // Add to existing collection
                    {
                        s.Parts = new Device[1];
                        s.Parts[0] = dev;
                    }
                    else
                    {
                        Array.Resize(ref s.Parts, s.Parts.Length + 1);
                        s.Parts[s.Parts.Length - 1] = dev;
                    }
                }
            }

            return GrowingCollection.ToArray();
        }

        private DeviceCollection DevColIndexOf(ref List<DeviceCollection> Collection, string ManuName)
        {
            for (int i = 0, loopTo = Collection.Count - 1; i <= loopTo; i++)
            {
                if ((Collection[i].Name ?? "") == (ManuName ?? ""))
                {
                    return Collection[i];
                }
            }

            return null;
        }

        private class DeviceCollection
        {
            internal string Name;
            internal Device[] Parts;
        }

        private void GeneratePartNames(DeviceCollection input, ref string[] part_numbers)
        {
            part_numbers = new string[input.Parts.Length];
            for (int i = 0, loopTo = part_numbers.Length - 1; i <= loopTo; i++)
            {
                string part_name = input.Parts[i].NAME;
                if (part_name.Contains(" "))
                    part_name = part_name.Substring(input.Name.Length + 1);
                if (part_name.Equals("W25M121AV"))
                {
                    part_numbers[i] = part_name + " (128Mbit/1Gbit)";
                }
                else
                {
                    part_numbers[i] = part_name + " (" + Constants.GetDataSizeString(input.Parts[i].FLASH_SIZE) + ")";
                }
            }
        }

    }

    public class NAND_LAYOUT_TOOL
    {
        public NandMemLayout Layout { get; private set; }

        public NAND_LAYOUT_TOOL(NandMemLayout defined_layout)
        {
            Layout = defined_layout;
        }

        public struct NANDLAYOUT_STRUCTURE
        {
            public ushort Layout_Main;
            public ushort Layout_Spare;
        }

        public NANDLAYOUT_STRUCTURE GetStructure(Device nand_dev)
        {
            var current_value = default(NANDLAYOUT_STRUCTURE);
            var nand_page_size = default(uint);
            var nand_ext_size = default(uint);
            if (ReferenceEquals(nand_dev.GetType(), typeof(SPI_NAND)))
            {
                nand_page_size = ((SPI_NAND)nand_dev).PAGE_SIZE;
                nand_ext_size = ((SPI_NAND)nand_dev).PAGE_EXT;
            }
            else if (ReferenceEquals(nand_dev.GetType(), typeof(P_NAND)))
            {
                nand_page_size = ((P_NAND)nand_dev).PAGE_SIZE;
                nand_ext_size = ((P_NAND)nand_dev).PAGE_EXT;
            }

            if (Layout == NandMemLayout.Separated)
            {
                current_value.Layout_Main = (ushort)nand_page_size;
                current_value.Layout_Spare = (ushort)nand_ext_size;
            }
            else if (Layout == NandMemLayout.Segmented)
            {
                switch (nand_page_size)
                {
                    case 2048U:
                        {
                            current_value.Layout_Main = (ushort)(nand_page_size / 4d);
                            current_value.Layout_Spare = (ushort)(nand_ext_size / 4d);
                            break;
                        }

                    default:
                        {
                            current_value.Layout_Main = (ushort)nand_page_size;
                            current_value.Layout_Spare = (ushort)nand_ext_size;
                            break;
                        }
                }
            }
            else if (Layout == NandMemLayout.Combined)
            {
                current_value.Layout_Main = (ushort)(nand_page_size + nand_ext_size);
                current_value.Layout_Spare = 0;
            }

            return current_value;
        }

        private void FillMain(Device nand_dev, ref byte[] cache_data, byte[] main_data, ref uint data_ptr, ref uint bytes_left)
        {
            var ext_page_size = default(uint);
            if (ReferenceEquals(nand_dev.GetType(), typeof(SPI_NAND)))
            {
                ext_page_size = ((SPI_NAND)nand_dev).PAGE_EXT;
            }
            else if (ReferenceEquals(nand_dev.GetType(), typeof(P_NAND)))
            {
                ext_page_size = ((P_NAND)nand_dev).PAGE_EXT;
            }

            var nand_layout = GetStructure(nand_dev);
            ushort page_size_tot = (ushort)(nand_dev.PAGE_SIZE + ext_page_size);
            ushort logical_block = (ushort)(nand_layout.Layout_Main + nand_layout.Layout_Spare);
            ushort sub_index = 1;
            ushort adj_offset = 0;
            while (!(adj_offset == page_size_tot))
            {
                ushort sub_left = nand_layout.Layout_Main;
                if (sub_left > bytes_left)
                    sub_left = (ushort)bytes_left;
                Array.Copy(main_data, data_ptr, cache_data, adj_offset, sub_left);
                data_ptr += sub_left;
                bytes_left -= sub_left;
                if (bytes_left == 0L)
                    break;
                adj_offset = (ushort)(sub_index * logical_block);
                sub_index = (ushort)(sub_index + 1);
            }
        }

        private void FillSpare(Device nand_dev, ref byte[] cache_data, byte[] oob_data, ref uint oob_ptr, ref uint bytes_left)
        {
            var page_size_ext = default(uint);
            if (ReferenceEquals(nand_dev.GetType(), typeof(SPI_NAND)))
            {
                page_size_ext = ((SPI_NAND)nand_dev).PAGE_EXT;
            }
            else if (ReferenceEquals(nand_dev.GetType(), typeof(P_NAND)))
            {
                page_size_ext = ((P_NAND)nand_dev).PAGE_EXT;
            }

            var nand_layout = GetStructure(nand_dev);
            ushort page_size_tot = (ushort)(nand_dev.PAGE_SIZE + page_size_ext);
            ushort logical_block = (ushort)(nand_layout.Layout_Main + nand_layout.Layout_Spare);
            ushort sub_index = 2;
            ushort adj_offset = (ushort)(logical_block - nand_layout.Layout_Spare);
            while (!((ushort)(adj_offset - nand_layout.Layout_Main) == page_size_tot))
            {
                ushort sub_left = nand_layout.Layout_Spare;
                if (sub_left > bytes_left)
                    sub_left = (ushort)bytes_left;
                Array.Copy(oob_data, oob_ptr, cache_data, adj_offset, sub_left);
                oob_ptr += sub_left;
                bytes_left -= sub_left;
                if (bytes_left == 0L)
                    break;
                adj_offset = (ushort)((ushort)(sub_index * logical_block) - nand_layout.Layout_Spare);
                sub_index = (ushort)(sub_index + 1);
            }
        }

        public byte[] CreatePageAligned(Device nand_dev, byte[] main_data, byte[] oob_data)
        {
            var page_size_ext = default(uint);
            if (ReferenceEquals(nand_dev.GetType(), typeof(SPI_NAND)))
            {
                page_size_ext = ((SPI_NAND)nand_dev).PAGE_EXT;
            }
            else if (ReferenceEquals(nand_dev.GetType(), typeof(P_NAND)))
            {
                page_size_ext = ((P_NAND)nand_dev).PAGE_EXT;
            }

            ushort page_size_tot = (ushort)(nand_dev.PAGE_SIZE + page_size_ext);
            uint total_pages = 0U;
            uint data_ptr = 0U;
            uint oob_ptr = 0U;
            byte[] page_aligned = null;
            if (main_data is null)
            {
                total_pages = (uint)(oob_data.Length / (double)page_size_ext);
                main_data = new byte[(int)(total_pages * nand_dev.PAGE_SIZE - 1L + 1)];
                Utilities.FillByteArray(ref main_data, 255);
            }
            else if (oob_data is null)
            {
                total_pages = (uint)(main_data.Length / (double)nand_dev.PAGE_SIZE);
                oob_data = new byte[(int)(total_pages * page_size_ext - 1L + 1)];
                Utilities.FillByteArray(ref oob_data, 255);
            }
            else
            {
                total_pages = (uint)(main_data.Length / (double)nand_dev.PAGE_SIZE);
            }

            page_aligned = new byte[(int)(total_pages * page_size_tot - 1L + 1)];
            uint bytes_left = (uint)page_aligned.Length;
            for (long i = 0L, loopTo = total_pages - 1L; i <= loopTo; i++)
            {
                var cache_data = new byte[page_size_tot];
                if (main_data is object)
                    FillMain(nand_dev, ref cache_data, main_data, ref data_ptr, ref bytes_left);
                if (oob_data is object)
                    FillSpare(nand_dev, ref cache_data, oob_data, ref oob_ptr, ref bytes_left);
                Array.Copy(cache_data, 0L, page_aligned, i * page_size_tot, cache_data.Length);
            }

            return page_aligned;
        }

        public uint GetNandPageAddress(Device nand_dev, long nand_addr, FlashArea memory_area)
        {
            var nand_page_size = default(uint); // 0x800 (2048)
            var nand_ext_size = default(uint); // 0x40 (64)
            if (ReferenceEquals(nand_dev.GetType(), typeof(SPI_NAND)))
            {
                nand_page_size = ((SPI_NAND)nand_dev).PAGE_SIZE;
                nand_ext_size = ((SPI_NAND)nand_dev).PAGE_EXT;
            }
            else if (ReferenceEquals(nand_dev.GetType(), typeof(P_NAND)))
            {
                nand_page_size = ((P_NAND)nand_dev).PAGE_SIZE;
                nand_ext_size = ((P_NAND)nand_dev).PAGE_EXT;
            }

            var page_addr = default(uint); // This is the page address
            if (memory_area == FlashArea.Main)
            {
                page_addr = (uint)(nand_addr / (double)nand_page_size);
            }
            else if (memory_area == FlashArea.OOB)
            {
                page_addr = (uint)Math.Floor(nand_addr / (double)nand_ext_size);
            }
            else if (memory_area == FlashArea.All)   // we need to adjust large address to logical address
            {
                page_addr = (uint)Math.Floor(nand_addr / (double)(nand_page_size + nand_ext_size));
            }

            return page_addr;
        }
    }

    public static class Tools
    {
        public static FlashDetectResult GetFlashResult(byte[] ident_data)
        {
            var result = new FlashDetectResult();
            result.Successful = false;
            if (ident_data is null)
                return result;
            if (ident_data[0] == 0 && ident_data[2] == 0)
                return result; // 0x0000
            if (ident_data[0] == 0x90 && ident_data[2] == 0x90)
                return result; // 0x9090 
            if (ident_data[0] == 0x90 && ident_data[2] == 0)
                return result; // 0x9000 
            if (ident_data[0] == 0xFF && ident_data[2] == 0xFF)
                return result; // 0xFFFF 
            if (ident_data[0] == 0xFF && ident_data[2] == 0)
                return result; // 0xFF00
            if (ident_data[0] == 0x1 && ident_data[1] == 0 && ident_data[2] == 0x1 && ident_data[3] == 0)
                return result; // 0x01000100
            if (Array.TrueForAll(ident_data, a => a.Equals(ident_data[0])))
                return result; // If all bytes are the same
            result.MFG = ident_data[0];
            result.ID1 = (ushort)((uint)ident_data[1] << 8 | ident_data[2]);
            result.ID2 = (ushort)((uint)ident_data[3] << 8 | ident_data[4]);
            if (result.ID1 == 0 && result.ID2 == 0)
                return result;
            result.Successful = true;
            return result;
        }

        public static string GetTypeString(MemoryType FlashType) {
            if (FlashType == MemoryType.PARALLEL_NOR) {
                return "Parallel NOR";
            } else if (FlashType == MemoryType.SERIAL_NOR) {
                return "SPI NOR Flash";
            } else if (FlashType == MemoryType.SERIAL_QUAD) {
                return "SPI QUAD NOR Flash";
            } else if (FlashType == MemoryType.SERIAL_NAND) {
                return "SPI NAND Flash";
            } else if (FlashType == MemoryType.SERIAL_I2C) {
                return "I2C EEPROM";
            } else if (FlashType == MemoryType.SERIAL_MICROWIRE) {
                return "Microwire EEPROM";
            } else if (FlashType == MemoryType.PARALLEL_NAND) {
                return "NAND Flash";
            } else if (FlashType == MemoryType.JTAG_CFI) {
                return "CFI Flash";
            } else if (FlashType == MemoryType.JTAG_SPI) {
                return "SPI Flash";
            } else if (FlashType == MemoryType.JTAG_BSDL) {
                return "CFI Flash";
            } else if (FlashType == MemoryType.FWH_NOR) {
                return "Firmware Hub Flash";
            } else if (FlashType == MemoryType.DFU_MODE) {
                return "AVR Firmware";
            } else if (FlashType == MemoryType.OTP_EPROM) {
                return "EPROM";
            } else if (FlashType == MemoryType.HYPERFLASH) {
                return "HyperFlash";
            } else if (FlashType == MemoryType.SERIAL_SWI) {
                return "SWI EEPROM";
            } else {
                return "";
            }
        }

    }

}
