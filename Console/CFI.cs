// COPYRIGHT EMBEDDEDCOMPUTERS.NET 2020 - ALL RIGHTS RESERVED
// CONTACT EMAIL: support@embeddedcomputers.net
// ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
// INFO: This class interfaces the CFI flashes (over JTAG) via FlashcatUSB hardware/firmware
using System;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using USB;

namespace CFI
{
    public class FLASH_INTERFACE
    {
        public uint FlashSize { get; set; } // Size in number of bytes
        public string FlashName { get; set; } // Contains the ascii name of the flash IC
        public bool USE_BULK { get; set; } = true;  // Set to false to manually read each word

        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        private ushort Flash_BlockCount; // Number of blocks
        private int[] Flash_EraseBlock; // Number of erase sectors per block
        private int[] Flash_EraseSize; // Size of sectors per block
        private int[] Flash_Address; // Addresses of all sectors
        private bool Flash_Supported; // Indicates that we support the device for writing

        private void InitSectorAddresses()
        {
            int i;
            var AllSects = GetAllSectors();
            int SectorInt = AllSects.Length;
            int SecAdd = 0;
            Flash_Address = new int[SectorInt];
            var loopTo = SectorInt - 1;
            for (i = 0; i <= loopTo; i++)
            {
                Flash_Address[i] = SecAdd;
                SecAdd += AllSects[i];
            }
        }
        // Returns the base address given the sector
        public uint FindSectorBase(uint sector)
        {
            try
            {
                return (uint)Flash_Address[(int)sector];
            }
            catch
            {
                return 0U;
            }
        }
        // Returns the sector that contains the offset address (verified)
        public int FindSectorOffset(int Offset)
        {
            var allSectors = GetAllSectors();
            int i;
            int MinAddress = 0;
            var MaxAddress = default(int);
            var loopTo = allSectors.Length - 1;
            for (i = 0; i <= loopTo; i++)
            {
                MaxAddress += allSectors[i] - 1;
                if (Offset >= MinAddress & Offset <= MaxAddress)
                    return i; // Found it
                MinAddress = MaxAddress + 1;
            }

            return -1; // Did not find it
        }
        // Returns the size (in bytes) of a sector
        public int GetSectorSize(int Sector)
        {
            var sectors = GetAllSectors();
            if (Sector > sectors.Length)
                return 0;
            return sectors[Sector];
        }
        // Returns all of the sectors (as their byte sizes)
        private int[] GetAllSectors()
        {
            var list = new List<int>();
            int numSectors;
            for (int i = 0, loopTo = Flash_BlockCount - 1; i <= loopTo; i++)
            {
                numSectors = Flash_EraseBlock[i];
                for (int x = 0, loopTo1 = numSectors - 1; x <= loopTo1; x++)
                    list.Add(Flash_EraseSize[i]);
            }

            return list.ToArray();
        }
        // Returns the total number of sectors
        public int GetFlashSectors()
        {
            int i;
            int TotalSectors = 0;
            var loopTo = Flash_BlockCount - 1;
            for (i = 0; i <= loopTo; i++)
                TotalSectors += Flash_EraseBlock[i];
            return TotalSectors;
        }
        // Erases a sector on the flash device (byte mode only)
        public void Sector_Erase(int Sector)
        {
            try
            {
                MyDeviceBus = DeviceBus.X16;
                uint SA = FindSectorBase((uint)Sector); // Sector Address
                if (MyDeviceMode == DeviceAlgorithm.Intel | MyDeviceMode == DeviceAlgorithm.Intel_Sharp)
                {
                    write_command(SA, 0x50U); // clear register
                    write_command(SA, 0x60U); // Unlock block (just in case)
                    write_command(SA, 0xD0U); // Confirm Command
                    Utilities.Sleep(50);
                    write_command(SA, 0x20U);
                    write_command(SA, 0xD0U);
                    WaitUntilReady();
                }
                else if (MyDeviceMode == DeviceAlgorithm.AMD_Fujitsu | MyDeviceMode == DeviceAlgorithm.AMD_Fujitsu_Extended)
                {
                    write_command(0xAAAU, 0xAAU); // AAA = 0xAA
                    write_command(0x555U, 0x55U); // 555 = 0x55
                    write_command(0xAAAU, 0x80U); // AAA = 0x80
                    write_command(0xAAAU, 0xAAU); // AAA = 0xAA
                    write_command(0x555U, 0x55U); // 555 = 0x55
                    write_command(SA, 0x30U); // SA  = 0x30
                    write_command(0U, 0xF0U); // amd reset cmd
                    amd_erasewait((int)SA);
                }
                else if (MyDeviceMode == DeviceAlgorithm.SST)
                {
                    write_command(0xAAAAU, 0xAAU);
                    write_command(0x5554U, 0x55U);
                    write_command(0xAAAAU, 0x80U);
                    write_command(0xAAAAU, 0xAAU);
                    write_command(0x5554U, 0x55U);
                    write_command(SA, 0x30U); // SA  = 0x30
                    amd_erasewait((int)SA);
                }
                else if (MyDeviceMode == DeviceAlgorithm.AMD_NoBypass)
                {
                    write_command(0xAAAU, 0xAAU); // AAA = 0xAA
                    write_command(0x555U, 0x55U); // 555 = 0x55
                    write_command(0xAAAU, 0x80U); // AAA = 0x80
                    write_command(0xAAAU, 0xAAU); // AAA = 0xAA
                    write_command(0x555U, 0x55U); // 555 = 0x55
                    write_command(SA, 0x30U); // SA  = 0x30
                    write_command(0U, 0xF0U); // amd reset cmd
                    amd_erasewait((int)SA);
                }
            }
            catch
            {
            }
        }
        // Writes data to a given sector and also swaps bytes (endian for words/halfwords)
        public void WriteSector(int Sector, byte[] data)
        {
            uint Addr32 = FindSectorBase((uint)Sector);
            WriteData(Addr32, data);
        }
        // Waits until a sector is blank (using the AMD read sector method)
        private void amd_erasewait(int SectorOffset, bool AllowTimeout = true)
        {
            try
            {
                Utilities.Sleep(500); // Incase the data is already blank
                uint Counter = 0U;
                uint mydata = 0U;
                while (mydata != 0xFFFFFFFFL)
                {
                    Utilities.Sleep(100);
                    mydata = ReadWord((uint)(MyDeviceBase + SectorOffset));
                    if (AllowTimeout)
                    {
                        Counter = (uint)(Counter + 1L);
                        if (Counter == 20L)
                            break;
                    }
                }
            }
            catch
            {
            }
        }

        public event Memory_Write_BEventHandler Memory_Write_B;

        public delegate void Memory_Write_BEventHandler(uint addr, byte data);

        public event Memory_Write_HEventHandler Memory_Write_H;

        public delegate void Memory_Write_HEventHandler(uint addr, ushort data);

        public event Memory_Write_WEventHandler Memory_Write_W;

        public delegate void Memory_Write_WEventHandler(uint addr, uint data);

        public event Memory_Read_BEventHandler Memory_Read_B;

        public delegate void Memory_Read_BEventHandler(uint addr, ref byte data);

        public event Memory_Read_HEventHandler Memory_Read_H;

        public delegate void Memory_Read_HEventHandler(uint addr, ref ushort data);

        public event Memory_Read_WEventHandler Memory_Read_W;

        public delegate void Memory_Read_WEventHandler(uint addr, ref uint data);

        public event SetBaseAddressEventHandler SetBaseAddress;

        public delegate void SetBaseAddressEventHandler(uint addr);

        public event ReadFlashEventHandler ReadFlash;

        public delegate void ReadFlashEventHandler(uint mem_addr, ref byte[] data);

        public event WriteFlashEventHandler WriteFlash;

        public delegate void WriteFlashEventHandler(uint mem_addr, byte[] data_to_write, CFI_FLASH_MODE prog_mode);

        public event WriteConsoleEventHandler WriteConsole;

        public delegate void WriteConsoleEventHandler(string message);

        public ChipID MyDeviceID;
        private DeviceAlgorithm MyDeviceMode;
        private DeviceBus MyDeviceBus;
        private DeviceInterface MyDeviceInterface; // Loaded via CFI
        private uint MyDeviceBase; // Address of the device

        public FLASH_INTERFACE()
        {
        }
        // Returns true if the flash device is detected
        public bool DetectFlash(uint BaseAddress)
        {
            Flash_Supported = false;
            MyDeviceBase = BaseAddress;
            Read_Mode();
            if (Enable_CFI_Mode(DeviceBus.X16) || Enable_CFI_Mode(DeviceBus.X8) || Enable_CFI_Mode(DeviceBus.X32))
            {
                Load_CFI_Data();
            }
            else if (Conversions.ToBoolean(Enable_CFI_Mode_ForSST()))
            {
                Load_CFI_Data();
            }

            Read_Mode(); // Puts the flash back into read mode
            if (Enable_JEDEC_Mode())
            {
                MyDeviceID = new ChipID() { MFG = 0, ID1 = 0 };
                if (MyDeviceMode == DeviceAlgorithm.NotDefined) // Possible non-cfi device
                {
                    uint FirstWord = ReadWord(MyDeviceBase);
                    FirstWord = ReadWord(MyDeviceBase); // Read this twice for some unknown reason
                    MyDeviceID.MFG = (byte)(FirstWord & 0xFFL);
                    MyDeviceID.ID1 = (ushort)((FirstWord & 0xFFFF0000L) >> 16);
                    if (!Detect_NonCFI_Device(MyDeviceID.MFG, MyDeviceID.ID1))
                        return false;
                    SetBaseAddress?.Invoke(BaseAddress);
                }
                else
                {
                    MyDeviceID.MFG = (byte)(ReadHalfword(MyDeviceBase) & 0xFF); // 0x00F0 0x00C2
                    if (MyDeviceMode == DeviceAlgorithm.Intel | MyDeviceMode == DeviceAlgorithm.Intel_Sharp)
                    {
                        MyDeviceID.ID1 = ReadHalfword((uint)(MyDeviceBase + 0x2L));
                    }
                    else if (MyDeviceMode == DeviceAlgorithm.AMD_Fujitsu | MyDeviceMode == DeviceAlgorithm.AMD_Fujitsu_Extended)
                    {
                        MyDeviceID.ID1 = ReadHalfword((uint)(MyDeviceBase + 0x22L)); // 0x00E8 should be 0x2249
                    }
                    else if (MyDeviceMode == DeviceAlgorithm.SST)
                    {
                        MyDeviceID.ID1 = ReadHalfword((uint)(MyDeviceBase + 0x2L));
                        SetBaseAddress?.Invoke(BaseAddress);
                    }

                    if (MyDeviceID.MFG == 1 & MyDeviceID.ID1 == 0x227E) // Updates the full PartNumber for SPANSION devices
                    {
                        byte cycle_two = (byte)(0xFF & ReadHalfword((uint)(MyDeviceBase + (long)+0x1C)));
                        byte cycle_thr = (byte)(0xFF & ReadHalfword((uint)(MyDeviceBase + (long)+0x1E)));
                        MyDeviceID.ID2 = (ushort)(((uint)cycle_two << 8) + cycle_thr);
                    }

                    Read_Mode(); // Puts the flash back into read mode
                    string BaseStr = Conversion.Hex(BaseAddress).PadLeft(8, '0');
                    WriteConsole?.Invoke(string.Format("CFI compatible Flash detected at 0x{0}", BaseStr));
                }

                LoadFlashName();
                WriteConsole?.Invoke(string.Format("Connected to Flash (CHIP ID: 0x{0})", MyDeviceID.ToString()));
                WriteConsole?.Invoke(string.Format("CFI Flash base address: 0x{0}", "0x" + Conversion.Hex(MyDeviceBase).PadLeft(8, '0')));
                WriteConsole?.Invoke(string.Format("CFI Flash description: {0} ({1} bytes)", FlashName, Strings.Format((object)FlashSize, "#,###")));
                PrintProgrammingMode(); // "Programming mode: etc"
            }
            else
            {
                WriteConsole?.Invoke("Failed to detect CFI compatible Flash");
                return false;
            }

            return true;
        }

        private bool Detect_NonCFI_Device(byte ManufactureID, ushort PartNumber)
        {
            if (ManufactureID == 0xC2 & PartNumber == 0x22C4) // MX29LV161T
            {
                FlashSize = 0x200000U;
                Flash_BlockCount = 4;
                Flash_EraseBlock = new int[Flash_BlockCount];
                Flash_EraseSize = new int[Flash_BlockCount];
                Flash_EraseSize[0] = 0x10000; // 64KB
                Flash_EraseBlock[0] = 31;
                Flash_EraseSize[1] = 0x8000; // 32KB
                Flash_EraseBlock[1] = 1;
                Flash_EraseSize[2] = 0x2000; // 8KB
                Flash_EraseBlock[2] = 2;
                Flash_EraseSize[3] = 0x4000; // 16KB
                Flash_EraseBlock[3] = 1;
                InitSectorAddresses();
                MyDeviceMode = DeviceAlgorithm.AMD_NoBypass;
            }
            else if (ManufactureID == 0xC2 & PartNumber == 0x2249) // MX29LV161B
            {
                FlashSize = 0x200000U;
                Flash_BlockCount = 4;
                Flash_EraseBlock = new int[Flash_BlockCount];
                Flash_EraseSize = new int[Flash_BlockCount];
                Flash_EraseSize[0] = 0x4000; // 16KB
                Flash_EraseBlock[0] = 1;
                Flash_EraseSize[1] = 0x2000; // 8KB
                Flash_EraseBlock[1] = 2;
                Flash_EraseSize[2] = 0x8000; // 32KB
                Flash_EraseBlock[2] = 1;
                Flash_EraseSize[3] = 0x10000; // 64KB
                Flash_EraseBlock[3] = 31;
                InitSectorAddresses();
                MyDeviceMode = DeviceAlgorithm.AMD_NoBypass;
            }
            else if (ManufactureID == 0xC2 & PartNumber == 0x22DA) // MX29LV800T
            {
                FlashSize = 0x100000U;
                Flash_BlockCount = 4;
                Flash_EraseBlock = new int[Flash_BlockCount];
                Flash_EraseSize = new int[Flash_BlockCount];
                Flash_EraseSize[0] = 0x10000; // 64KB
                Flash_EraseBlock[0] = 15;
                Flash_EraseSize[1] = 0x8000; // 32KB
                Flash_EraseBlock[1] = 1;
                Flash_EraseSize[2] = 0x2000; // 8KB
                Flash_EraseBlock[2] = 2;
                Flash_EraseSize[3] = 0x4000; // 16KB
                Flash_EraseBlock[3] = 1;
                InitSectorAddresses();
                MyDeviceMode = DeviceAlgorithm.AMD_NoBypass;
            }
            else if (ManufactureID == 0xC2 & PartNumber == 0x22DA) // MX29LV800B
            {
                FlashSize = 0x100000U;
                Flash_BlockCount = 4;
                Flash_EraseBlock = new int[Flash_BlockCount];
                Flash_EraseSize = new int[Flash_BlockCount];
                Flash_EraseSize[0] = 0x4000; // 16KB
                Flash_EraseBlock[0] = 1;
                Flash_EraseSize[1] = 0x2000; // 8KB
                Flash_EraseBlock[1] = 2;
                Flash_EraseSize[2] = 0x8000; // 32KB
                Flash_EraseBlock[2] = 1;
                Flash_EraseSize[3] = 0x10000; // 64KB
                Flash_EraseBlock[3] = 15;
                InitSectorAddresses();
                MyDeviceMode = DeviceAlgorithm.AMD_NoBypass;
            }
            else
            {
                Read_Mode();
                return false;
            }

            Read_Mode();
            return true;
        }
        // If our device can be programmed by this code
        public bool WriteAllowed
        {
            get
            {
                return Flash_Supported;
            }
        }
        // Loads the device name (if we have it in our database)
        private void LoadFlashName()
        {
            try
            {
                var flash = MainApp.FlashDatabase.FindDevice(MyDeviceID.MFG, MyDeviceID.ID1, MyDeviceID.ID2, FlashMemory.MemoryType.PARALLEL_NOR);
                if (flash is object)
                {
                    FlashName = flash.NAME;
                }
                else
                {
                    FlashName = "(Unknown Name)";
                }
            }
            catch
            {
                FlashName = "Erorr loading Flash name";
            }
        }

        private void PrintProgrammingMode()
        {
            string BusWidthString = "";
            string AlgStr = "";
            string InterfaceStr = "";
            switch (MyDeviceBus)
            {
                case DeviceBus.X8:
                    {
                        BusWidthString = string.Format("({0} bit bus)", 8);
                        break;
                    }

                case DeviceBus.X16:
                    {
                        BusWidthString = string.Format("({0} bit bus)", 16);
                        break;
                    }

                case DeviceBus.X32:
                    {
                        BusWidthString = string.Format("({0} bit bus)", 32);
                        break;
                    }

                default:
                    {
                        return;
                    }
            }

            switch (MyDeviceMode)
            {
                case DeviceAlgorithm.AMD_Fujitsu:
                    {
                        AlgStr = "AMD/Fujitsu";
                        break;
                    }

                case DeviceAlgorithm.AMD_Fujitsu_Extended:
                    {
                        AlgStr = "AMD/Fujitsu (extended)";
                        break;
                    }

                case DeviceAlgorithm.Intel:
                    {
                        AlgStr = "Intel";
                        break;
                    }

                case DeviceAlgorithm.Intel_Sharp:
                    {
                        AlgStr = "Intel/Sharp";
                        break;
                    }

                case DeviceAlgorithm.SST:
                    {
                        AlgStr = "SST";
                        break;
                    }

                default:
                    {
                        return;
                    }
            }

            switch (MyDeviceInterface)
            {
                case DeviceInterface.x8_only:
                    {
                        InterfaceStr = "x8 interface";
                        break;
                    }

                case DeviceInterface.x8_and_x16:
                    {
                        InterfaceStr = "x8/x16 interface";
                        break;
                    }

                case DeviceInterface.x16_only:
                    {
                        InterfaceStr = "x16 interface";
                        break;
                    }

                case DeviceInterface.x32:
                    {
                        InterfaceStr = "x32 interface";
                        break;
                    }
            }

            WriteConsole?.Invoke("Programming mode: " + AlgStr + " " + InterfaceStr + " " + BusWidthString);
        }

        private void write_command(uint offset, uint data)
        {
            switch (MyDeviceBus)
            {
                case DeviceBus.X8:
                    {
                        Memory_Write_B?.Invoke(MyDeviceBase + offset, (byte)data);
                        break;
                    }

                case DeviceBus.X16:
                    {
                        Memory_Write_H?.Invoke(MyDeviceBase + offset, (ushort)data);
                        break;
                    }

                case DeviceBus.X32:
                    {
                        Memory_Write_W?.Invoke(MyDeviceBase + offset, data);
                        break;
                    }
            }
        }
        // Attempts to put the device into CFI mode
        private bool Enable_CFI_Mode(DeviceBus BusMode)
        {
            switch (BusMode)
            {
                case DeviceBus.X8:
                    {
                        Memory_Write_B?.Invoke((uint)(MyDeviceBase + 0xAAL), 0x98); // CFI Mode Command
                        break;
                    }

                case DeviceBus.X16:
                    {
                        Memory_Write_H?.Invoke((uint)(MyDeviceBase + 0xAAL), 0x98); // CFI Mode Command
                        break;
                    }

                case DeviceBus.X32:
                    {
                        Memory_Write_W?.Invoke((uint)(MyDeviceBase + 0xAAL), 0x98U); // CFI Mode Command 
                        break;
                    }
            }

            Utilities.Sleep(50); // If the command succeded, we need to wait for the device to switch modes
            ushort ReadBack1 = ReadHalfword(MyDeviceBase + 0x20U);
            ushort ReadBack2 = ReadHalfword(MyDeviceBase + 0x22U);
            ushort ReadBack3 = ReadHalfword(MyDeviceBase + 0x24U);
            uint QRY = (uint)((ReadBack1 & 255) << 16 | (ReadBack2 & 255) << 8 | ReadBack3 & 255);
            if (QRY == 0x515259L)
            {
                MyDeviceBus = BusMode; // Flash Device Interface description (refer to CFI publication 100)
                return true;
            }

            Read_Mode();
            return false;
        }
        // Attempts to put the device into JEDEC mode
        private bool Enable_JEDEC_Mode()
        {
            if (MyDeviceMode == DeviceAlgorithm.NotDefined)
            {
                write_command(0xAAAU, 0xAAU);
                write_command(0x555U, 0x55U);
                write_command(0xAAAU, 0x90U);
            }
            else if (MyDeviceMode == DeviceAlgorithm.Intel | MyDeviceMode == DeviceAlgorithm.Intel_Sharp)
            {
                write_command(0U, 0x90U);
            }
            else if (MyDeviceMode == DeviceAlgorithm.AMD_Fujitsu | MyDeviceMode == DeviceAlgorithm.AMD_Fujitsu_Extended)
            {
                write_command(0xAAAU, 0xAAU); // HERE
                write_command(0x555U, 0x55U);
                write_command(0xAAAU, 0x90U);
            }
            else if (MyDeviceMode == DeviceAlgorithm.AMD_NoBypass)
            {
                write_command(0xAAAU, 0xAAU);
                write_command(0x555U, 0x55U);
                write_command(0xAAAU, 0x90U);
            }
            else if (MyDeviceMode == DeviceAlgorithm.SST)
            {
                write_command(0xAAAAU, 0xAAU);
                write_command(0x5554U, 0x55U);
                write_command(0xAAAAU, 0x90U);
            }
            else
            {
                return false;
            }

            Utilities.Sleep(50);
            return true;
        }
        // Puts the device back into READ mode
        private object Enable_CFI_Mode_ForSST()
        {
            Memory_Write_B?.Invoke((uint)(MyDeviceBase + 0xAAAAL), 0xAA);
            Memory_Write_B?.Invoke((uint)(MyDeviceBase + 0x5554L), 0x55);
            Memory_Write_B?.Invoke((uint)(MyDeviceBase + 0xAAAAL), 0x98);
            Utilities.Sleep(50); // If the command succeeded, we need to wait for the device to switch modes
            uint ReadBack = ReadHalfword(MyDeviceBase + 0x20U);
            ReadBack = (ReadBack << 8) + ReadHalfword(MyDeviceBase + 0x22U);
            ReadBack = (ReadBack << 8) + ReadHalfword(MyDeviceBase + 0x24U);
            if (ReadBack == 0x515259L) // "QRY"
            {
                MyDeviceBus = DeviceBus.X8; // Flash Device Interface description (refer to CFI publication 100)
                return true;
            }

            Read_Mode();
            return false;
        }

        private object Load_CFI_Data()
        {
            try
            {
                FlashSize = (uint)(int)Math.Pow(2d, ReadHalfword((uint)(MyDeviceBase + 0x4EL))); // &H00200000
                ushort DeviceCommandSet = (ushort)((ushort)(0xFF & ReadHalfword((uint)(MyDeviceBase + 0x26L))) << 8); // &H0200
                DeviceCommandSet += (byte)(0xFF & ReadHalfword((uint)(MyDeviceBase + 0x28L)));
                MyDeviceMode = (DeviceAlgorithm)DeviceCommandSet;
                MyDeviceInterface = (DeviceInterface)ReadHalfword((uint)(MyDeviceBase + 0x50L)); // 0x02
                Flash_BlockCount = ReadHalfword((uint)(MyDeviceBase + 0x58L)); // 0x04
                uint BootFlag = ReadHalfword((uint)(MyDeviceBase + 0x9EL)); // &H0000FFFF
                Flash_EraseBlock = new int[Flash_BlockCount];
                Flash_EraseSize = new int[Flash_BlockCount];
                uint BlockAddress = 0x5AU; // Start address of block 1 information
                for (int i = 1, loopTo = Flash_BlockCount; i <= loopTo; i++)
                {
                    Flash_EraseBlock[i - 1] = (ReadHalfword((uint)(MyDeviceBase + BlockAddress + 2L)) << 8) + ReadHalfword(MyDeviceBase + BlockAddress) + 1;
                    Flash_EraseSize[i - 1] = ((ReadHalfword((uint)(MyDeviceBase + BlockAddress + 6L)) << 8) + ReadHalfword((uint)(MyDeviceBase + BlockAddress + 4L))) * 256;
                    BlockAddress = (uint)(BlockAddress + 8L); // Increase address by 8
                }

                if (BootFlag == 3L) // warning: might only be designed for TC58FVT160
                {
                    Array.Reverse(Flash_EraseBlock);
                    Array.Reverse(Flash_EraseSize);
                }

                InitSectorAddresses(); // Creates the map of the addresses of all sectors
                return true;
            }
            catch
            {
            }

            return false;
        }

        public void Read_Mode()
        {
            if (MyDeviceMode == DeviceAlgorithm.NotDefined)
            {
                Memory_Write_B?.Invoke(MyDeviceBase, 0xFF); // For Intel / Sharp
                Memory_Write_B?.Invoke(MyDeviceBase, 0x50);
                Memory_Write_B?.Invoke((uint)(MyDeviceBase + 0xAAAL), 0xAA); // For AMD
                Memory_Write_B?.Invoke((uint)(MyDeviceBase + 0x555L), 0x55);
                Memory_Write_B?.Invoke((uint)(MyDeviceBase + 0xAAAAL), 0xAA); // For SST
                Memory_Write_B?.Invoke((uint)(MyDeviceBase + 0x5554L), 0x55);
                Memory_Write_B?.Invoke((uint)(MyDeviceBase + 0xAAAAL), 0xF0);
                Memory_Write_B?.Invoke(MyDeviceBase, 0xF0); // For LEGACY
            }
            else if (MyDeviceMode == DeviceAlgorithm.Intel | MyDeviceMode == DeviceAlgorithm.Intel_Sharp)
            {
                write_command(0U, 0xFFU);
                write_command(0U, 0x50U);
            }
            else if (MyDeviceMode == DeviceAlgorithm.AMD_Fujitsu | MyDeviceMode == DeviceAlgorithm.AMD_Fujitsu_Extended)
            {
                write_command(0xAAAU, 0xAAU); // second time here
                write_command(0x555U, 0x55U);
                write_command(0U, 0xF0U);
            }
            else if (MyDeviceMode == DeviceAlgorithm.AMD_NoBypass)
            {
                write_command(0U, 0xF0U);
            }
            else if (MyDeviceMode == DeviceAlgorithm.SST)
            {
                write_command(0xAAAAU, 0xAAU);
                write_command(0x5554U, 0x55U);
                write_command(0xAAAAU, 0xF0U);
            }

            Utilities.Sleep(50);
        }

        public void WaitUntilReady()
        {
            int counter = 0;
            ushort sr;
            if (MyDeviceMode == DeviceAlgorithm.Intel | MyDeviceMode == DeviceAlgorithm.Intel_Sharp)
            {
                do
                {
                    if (counter == 100)
                        return;
                    counter += 1;
                    Utilities.Sleep(25);
                    write_command(0U, 0x70U); // READ SW
                    sr = ReadHalfword(MyDeviceBase);
                    if (MainApp.AppIsClosing)
                        return;
                }
                while (!(sr >> 7 == 1));
                Read_Mode();
            }
            else if (MyDeviceMode == DeviceAlgorithm.AMD_Fujitsu | MyDeviceMode == DeviceAlgorithm.AMD_Fujitsu_Extended)
            {
                Utilities.Sleep(100);
            }
            else if (MyDeviceMode == DeviceAlgorithm.SST)
            {
                Utilities.Sleep(100);
            }
        }
        // Erases all blocks on the CFI device
        public bool EraseBulk()
        {
            WriteConsole?.Invoke("Performing a full chip erase");
            if (MyDeviceMode == DeviceAlgorithm.Intel | MyDeviceMode == DeviceAlgorithm.Intel_Sharp)
            {
                int secCount = GetFlashSectors();
                for (int i = 0, loopTo = secCount - 1; i <= loopTo; i++)
                    Sector_Erase(i);
            }
            else if (MyDeviceMode == DeviceAlgorithm.AMD_Fujitsu | MyDeviceMode == DeviceAlgorithm.AMD_Fujitsu_Extended)
            {
                write_command(0xAAAU, 0xAAU); // AAA = 0xAA
                write_command(0x555U, 0x55U); // 555 = 0x55
                write_command(0xAAAU, 0x80U); // AAA = 0x80
                write_command(0xAAAU, 0xAAU); // AAA = 0xAA
                write_command(0x555U, 0x55U); // 555 = 0x55
                write_command(0xAAAU, 0x10U); // AAA = 0x10
                amd_erasewait(0, false); // We may want to wait for a very long time (up to a minute)
            }
            else if (MyDeviceMode == DeviceAlgorithm.SST)
            {
                write_command(0xAAAAU, 0xAAU);
                write_command(0x5554U, 0x55U);
                write_command(0xAAAAU, 0x80U);
                write_command(0xAAAAU, 0xAAU);
                write_command(0x5554U, 0x55U);
                write_command(0xAAAAU, 0x10U);
                amd_erasewait(0, false); // We may want to wait for a very long time (up to a minute)
            }
            else if (MyDeviceMode == DeviceAlgorithm.AMD_NoBypass)
            {
                write_command(0xAAAU, 0xAAU); // AAA = 0xAA
                write_command(0x555U, 0x55U); // 555 = 0x55
                write_command(0xAAAU, 0x80U); // AAA = 0x80
                write_command(0xAAAU, 0xAAU); // AAA = 0xAA
                write_command(0x555U, 0x55U); // 555 = 0x55
                write_command(0xAAAU, 0x10U); // AAA = 0x10
                amd_erasewait(0, false); // We may want to wait for a very long time (up to a minute)
            }
            Read_Mode();
            WriteConsole?.Invoke("Memory device erased successfully");
            return true;
        }

        public byte[] ReadData(uint Offset, uint count)
        {
            byte[] DataOut = null;
            try
            {
                if (USE_BULK) // This is significantly faster
                {
                    var data = new byte[(int)(count - 1L + 1)];
                    ReadFlash?.Invoke(MyDeviceBase + Offset, ref data);
                    return data;
                }
                else
                {
                    int c = 0;
                    DataOut = new byte[(int)(count - 1L + 1)];
                    for (double i = 0d, loopTo = count / 4d - 1d; i <= loopTo; i++)
                    {
                        uint word = ReadWord((uint)(MyDeviceBase + Offset + i * 4d));
                        DataOut[c + 3] = (byte)((word & 0xFF000000L) >> 24);
                        DataOut[c + 2] = (byte)((word & 0xFF0000L) >> 16);
                        DataOut[c + 1] = (byte)((word & 0xFF00L) >> 8);
                        DataOut[c + 0] = (byte)(word & 0xFFL);
                        c = c + 4;
                    }
                }
            }
            catch
            {
            }

            return DataOut;
        }
        // Sector must be erased prior to writing data
        private void WriteData(uint Offset, byte[] data_to_write)
        {
            try
            {
                Utilities.ChangeEndian32_LSB16(ref data_to_write); // Might be DeviceAlgorithm specific
                if (MyDeviceMode == DeviceAlgorithm.Intel | MyDeviceMode == DeviceAlgorithm.Intel_Sharp)
                {
                    if (USE_BULK)
                    {
                        WriteFlash?.Invoke(MyDeviceBase + Offset, data_to_write, CFI_FLASH_MODE.Intel_16);
                    }
                    else
                    {
                        for (int i = 0, loopTo = data_to_write.Length - 1; i <= loopTo; i += 4) // We will write data 4 bytes at a time
                        {
                            Memory_Write_H?.Invoke((uint)(MyDeviceBase + Offset + i), 0x40);
                            Memory_Write_H?.Invoke((uint)(MyDeviceBase + Offset + i), (ushort)((data_to_write[i + 1] << 8) + data_to_write[i + 0]));
                            Memory_Write_H?.Invoke((uint)(MyDeviceBase + Offset + i + 2L), 0x40);
                            Memory_Write_H?.Invoke((uint)(MyDeviceBase + Offset + i + 2L), (ushort)((data_to_write[i + 3] << 8) + data_to_write[i + 2]));
                        }
                    }

                    Read_Mode();
                }
                else if (MyDeviceMode == DeviceAlgorithm.AMD_Fujitsu | MyDeviceMode == DeviceAlgorithm.AMD_Fujitsu_Extended)
                {
                    write_command(0xAAAU, 0xAAU);
                    write_command(0x555U, 0x55U);
                    write_command(0xAAAU, 0x20U);
                    if (USE_BULK) // Our fast method only works for DMA enabled targets
                    {
                        WriteFlash?.Invoke(MyDeviceBase + Offset, data_to_write, CFI_FLASH_MODE.AMD_16);
                    }
                    else
                    {
                        for (int i = 0, loopTo1 = data_to_write.Length - 1; i <= loopTo1; i += 4) // We will write data 4 bytes at a time
                        {
                            Memory_Write_H?.Invoke(MyDeviceBase, 0xA0);
                            Memory_Write_H?.Invoke((uint)(MyDeviceBase + Offset + i), (ushort)((data_to_write[i + 1] << 8) + data_to_write[i + 0]));
                            Memory_Write_H?.Invoke(MyDeviceBase, 0xA0);
                            Memory_Write_H?.Invoke((uint)(MyDeviceBase + Offset + i + 2L), (ushort)((data_to_write[i + 3] << 8) + data_to_write[i + 2]));
                        }
                    }

                    write_command(0U, 0x90U);
                    write_command(0U, 0x0U);
                }
                else if (MyDeviceMode == DeviceAlgorithm.AMD_NoBypass)
                {
                    if (USE_BULK) // Our fast method only works for DMA enabled targets
                    {
                        WriteFlash?.Invoke(MyDeviceBase + Offset, data_to_write, CFI_FLASH_MODE.NoBypass);
                    }
                    else
                    {
                        for (int i = 0, loopTo2 = data_to_write.Length - 1; i <= loopTo2; i += 4) // We will write data 4 bytes at a time
                        {
                            write_command(0xAAAU, 0xAAU);
                            write_command(0x555U, 0x55U);
                            write_command(0xAAAU, 0xA0U);
                            Memory_Write_H?.Invoke((uint)(MyDeviceBase + Offset + i), (ushort)((data_to_write[i + 1] << 8) + data_to_write[i + 0]));
                            write_command(0xAAAU, 0xAAU);
                            write_command(0x555U, 0x55U);
                            write_command(0xAAAU, 0xA0U);
                            Memory_Write_H?.Invoke((uint)(MyDeviceBase + Offset + i + 2L), (ushort)((data_to_write[i + 3] << 8) + data_to_write[i + 2]));
                        }
                    }
                }
                else if (MyDeviceMode == DeviceAlgorithm.SST)
                {
                    if (USE_BULK)
                    {
                        WriteFlash?.Invoke(MyDeviceBase + Offset, data_to_write, CFI_FLASH_MODE.SST);
                    }
                    else
                    {
                        for (int i = 0, loopTo3 = data_to_write.Length - 1; i <= loopTo3; i += 4) // We will write data 4 bytes at a time
                        {
                            write_command(0xAAAAU, 0xAAU);
                            write_command(0x5554U, 0x55U);
                            write_command(0xAAAAU, 0xA0U);
                            Memory_Write_H?.Invoke((uint)(MyDeviceBase + Offset + i), (ushort)((data_to_write[i + 1] << 8) + data_to_write[i + 0]));
                            write_command(0xAAAAU, 0xAAU);
                            write_command(0x5554U, 0x55U);
                            write_command(0xAAAAU, 0xA0U);
                            Memory_Write_H?.Invoke((uint)(MyDeviceBase + Offset + i + 2L), (ushort)((data_to_write[i + 3] << 8) + data_to_write[i + 2]));
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private byte ReadByte(uint offset)
        {
            var data = default(byte);
            Memory_Read_B?.Invoke(offset, ref data);
            return data;
        }

        private uint ReadWord(uint offset)
        {
            var data = default(uint);
            Memory_Read_W?.Invoke(offset, ref data);
            return data;
        }

        private ushort ReadHalfword(uint offset)
        {
            var data = default(ushort);
            Memory_Read_H?.Invoke(offset, ref data);
            return data;
        }
    }

    public enum CFI_FLASH_MODE : byte
    {
        Intel_16 = USBREQ.JTAG_FLASHWRITE_I16,
        AMD_16 = USBREQ.JTAG_FLASHWRITE_A16,
        SST = USBREQ.JTAG_FLASHWRITE_SST,
        NoBypass = USBREQ.JTAG_FLASHWRITE_AMDNB
    }

    public struct ChipID
    {
        public byte MFG;
        public ushort ID1; // Contains the most commonly used id
        public ushort ID2; // Some chips have a secondary id

        public bool IsValid()
        {
            if (MFG == 0)
                return false;
            if (MFG == 255)
                return false;
            if (ID1 == 0)
                return false;
            if (ID1 == 0xFFFF)
                return false;
            return true;
        }

        public override string ToString()
        {
            return Conversion.Hex(MFG).PadLeft(2, '0') + " " + Conversion.Hex(ID1).PadLeft(4, '0');
        }
    }
    // The device bus width used to accept commands
    public enum DeviceBus
    {
        X8 = 0,
        X16 = 1,
        X32 = 2
    }
    // The device specific programming / algorithm (set by CFI, 0x26+0x28)
    public enum DeviceAlgorithm : ushort
    {
        NotDefined = 0,
        Intel_Sharp = 0x100,
        SST = 0x107,
        AMD_Fujitsu = 0x200,
        Intel = 0x300,
        AMD_Fujitsu_Extended = 0x400,
        AMD_NoBypass = 0x1001 // We created/specified this mode type
    }

    public enum DeviceInterface
    {
        x8_only = 0,
        x16_only = 1,
        x8_and_x16 = 2, // via BYTE#
        x32 = 3
    }
}