using System;
using System.Collections.Generic;
using FlashMemory;
using Microsoft.VisualBasic;

public class NAND_BLOCK_IF
{
    public event PrintConsoleEventHandler PrintConsole;

    public delegate void PrintConsoleEventHandler(string msg);

    public event SetProgressEventHandler SetProgress;

    public delegate void SetProgressEventHandler(int percent);

    public event ReadPagesEventHandler ReadPages;

    public delegate void ReadPagesEventHandler(uint page_addr, ushort page_offset, uint count, FlashArea area, ref byte[] data);

    public event WritePagesEventHandler WritePages;

    public delegate void WritePagesEventHandler(uint page_addr, byte[] main, byte[] oob, FlashArea area, ref bool write_result);

    public event EraseSectorEventHandler EraseSector;

    public delegate void EraseSectorEventHandler(uint page_addr, ref bool erase_result);

    public event ReadyEventHandler Ready;

    public delegate void ReadyEventHandler(); // Checks the RD/BSY pin or register (WAS WaitForReady)

    public List<MAPPING> MAP = new List<MAPPING>();

    public uint MAPPED_PAGES { get; set; } // Number of pages available in the current map
    private long NAND_SIZE { get; set; } // Typical size of the nand (does not include extra area)
    private uint PAGE_MAIN { get; set; } // Size of the main page
    private uint PAGE_OOB { get; set; } // Size of the ext page
    private ushort PAGE_COUNT { get; set; } // Number of pages in the block
    private ushort BLOCK_COUNT { get; set; } // Number of blocks


    // Private Property BLOCK_SIZE As UInt32 'size of the block (minus oob)

    private byte[] MEMORY_AREA_ERASED; // If CopyExtendedArea is true, this will contain the other area that was erased

    public NAND_BLOCK_IF()
    {
    }

    public enum BLOCK_STATUS
    {
        Valid, // Marked valid by the manager or by the user
        Bad_Manager, // This block was marked bad because of the manager setting
        Bad_Marked, // This block was marked bad because of the user
        Bad_ByError, // This block was marked bad because of programming
        Unknown
    }

    public class MAPPING
    {
        public BLOCK_STATUS Status;
        public uint BlockIndex; // Index of the block
        public long PageAddress;  // The physical address of the first page of this block
        public long LogicalPage; // The mapped address of the first page of this block
    }

    // Called once on device init to create a map of all blocks
    public void CreateMap(long mem_size, uint main_page, uint oob_page, ushort page_count, uint block_count)
    {
        NAND_SIZE = mem_size;
        PAGE_MAIN = main_page;
        PAGE_OOB = oob_page;
        PAGE_COUNT = page_count;
        BLOCK_COUNT = (ushort)block_count;
        MAP.Clear();
        for (uint i = 0U, loopTo = (uint)(BLOCK_COUNT - 1); i <= loopTo; i++)
        {
            var block_info = new MAPPING();
            block_info.Status = BLOCK_STATUS.Valid;
            block_info.BlockIndex = i;
            block_info.PageAddress = PAGE_COUNT * (long)i;
            block_info.LogicalPage = PAGE_COUNT * (long)i;
            MAP.Add(block_info);
        }
    }

    public bool EnableBlockManager()
    {
        try
        {
            if (MainApp.MySettings.NAND_BadBlockManager == BadBlockMode.Disabled | MainApp.MySettings.NAND_Layout == NandMemLayout.Combined)
            {
                PrintConsole?.Invoke("NAND block manager disabled");
                return true;
            }
            else
            {
                PrintConsole?.Invoke("NAND memory device detected, loading valid memory map");
            }

            uint page_addr = 0U;
            for (uint i = 0U, loopTo = (uint)(BLOCK_COUNT - 1); i <= loopTo; i++)
            {
                uint LastPageAddr = (uint)(page_addr + PAGE_COUNT - 1L); // The last page of this block
                byte[] page_one = null;
                byte[] page_two = null;
                byte[] page_last = null;
                bool valid_block = true;
                int markers = (int)MainApp.MySettings.NAND_BadBlockMarkers;
                if ((markers & (int)BadBlockMarker._1stByte_FirstPage) > 0)
                {
                    if (page_one is null)
                        ReadPages?.Invoke(page_addr, (ushort)0, 6U, FlashArea.OOB, ref page_one);
                    if (!(page_one[0] == 255))
                        valid_block = false;
                }
                if ((markers & (int)BadBlockMarker._1stByte_SecondPage) > 0)
                {
                    if (page_two is null)
                        ReadPages?.Invoke((uint)((long)page_addr + 1L), (ushort)0, 6U, FlashArea.OOB, ref page_two);
                    if (!(page_two[0] == 255))
                        valid_block = false;
                }
                if ((markers & (int)BadBlockMarker._1stByte_LastPage) > 0)
                {
                    if (page_last is null)
                        ReadPages?.Invoke(LastPageAddr, (ushort)0, 6U, FlashArea.OOB, ref page_last);
                    if (!(page_last[0] == 255))
                        valid_block = false;
                }
                if ((markers & (int)BadBlockMarker._6thByte_FirstPage) > 0)
                {
                    if (page_one is null)
                        ReadPages?.Invoke(page_addr, (ushort)0, 6U, FlashArea.OOB, ref page_one);
                    if (!(page_one[5] == 255))
                        valid_block = false;
                }
                if ((markers & (int)BadBlockMarker._6thByte_SecondPage) > 0)
                {
                    if (page_two is null)
                        ReadPages?.Invoke((uint)((long)page_addr + 1L), (ushort)0, 6U, FlashArea.OOB, ref page_two);
                    if (!(page_two[5] == 255))
                        valid_block = false;
                }
                if (!valid_block)
                {
                    PrintConsole?.Invoke(string.Format("BAD NAND BLOCK at page index: 0x{0} (block index: {1})", Conversion.Hex(page_addr).PadLeft(6, '0'), i.ToString()));
                    MAP[(int)i].Status = BLOCK_STATUS.Bad_Manager;
                }
                else
                {
                    MAP[(int)i].Status = BLOCK_STATUS.Valid;
                }

                page_addr += PAGE_COUNT;
            }

            return true;
        }
        catch
        {
        }

        return false;
    }

    public void ProcessMap()
    {
        MAPPED_PAGES = 0U;
        long Logical_Page_Pointer = 0L;
        for (uint i = 0U, loopTo = (uint)(MAP.Count - 1); i <= loopTo; i++)
        {
            if (MAP[(int)i].Status == BLOCK_STATUS.Valid)
            {
                MAP[(int)i].LogicalPage = Logical_Page_Pointer;
                Logical_Page_Pointer += PAGE_COUNT;
                MAPPED_PAGES += PAGE_COUNT;
            }
            else
            {
                MAP[(int)i].LogicalPage = 0L;
            }
        }

        PrintConsole?.Invoke(string.Format("NAND memory map complete: {0} pages available for access", Strings.Format((object)MAPPED_PAGES, "#,###")));
    }
    // Returns the physical page address from the logical page address
    public uint GetPageMapping(uint page_index)
    {
        for (uint i = 0U, loopTo = (uint)(MAP.Count - 1); i <= loopTo; i++)
        {
            if (MAP[(int)i].Status == BLOCK_STATUS.Valid)
            {
                uint page_start = (uint)MAP[(int)i].LogicalPage;
                uint page_end = (uint)(page_start + PAGE_COUNT - 1L);
                if (page_index >= page_start && page_index <= page_end)
                {
                    return (uint)(MAP[(int)i].PageAddress + (page_index - page_start));
                }
            }
        }

        return 0U; // NOT FOUND
    }
    // Returns the total space of the extra data
    public uint Extra_GetSize()
    {
        uint PageCount = (uint)(NAND_SIZE / (double)PAGE_MAIN); // Total number of pages in this device
        uint PageExtraSize = PageCount * PAGE_OOB;
        return PageExtraSize;
    }

    public bool ERASEBLOCK(long page_address, FlashArea Memory_area, bool CopyOtherArea)
    {
        MEMORY_AREA_ERASED = null;
        if (CopyOtherArea && !(Memory_area == FlashArea.All))
        {
            if (Memory_area == FlashArea.Main)
            {
                ReadPages?.Invoke((uint)page_address, (ushort)0, (uint)PAGE_COUNT * PAGE_OOB, FlashArea.OOB, ref MEMORY_AREA_ERASED);
            }
            else if (Memory_area == FlashArea.OOB)
            {
                ReadPages?.Invoke((uint)page_address, (ushort)0, (uint)PAGE_COUNT * PAGE_MAIN, FlashArea.Main, ref MEMORY_AREA_ERASED);
            }
        }

        try // START BLOCK ERASE
        {
            var result = default(bool);
            EraseSector?.Invoke((uint)page_address, ref result);
            if (!result)
                return false;
            Ready?.Invoke();
        }
        catch
        {
        }  // BLOCK ERASE COMPLETE

        return true;
    }
    // This writes the data to a page
    public bool WRITEPAGE(long page_address, byte[] data_to_write, FlashArea memory_area)
    {
        try
        {
            byte[] main_data = null;
            byte[] oob_data = null;
            var WriteResult = default(bool);
            if (memory_area == FlashArea.Main)
            {
                main_data = data_to_write;
                oob_data = MEMORY_AREA_ERASED;
            }
            else if (memory_area == FlashArea.OOB)
            {
                main_data = MEMORY_AREA_ERASED;
                oob_data = data_to_write;
            }
            else if (memory_area == FlashArea.All)
            {
                main_data = data_to_write;
            }

            MEMORY_AREA_ERASED = null;
            WritePages?.Invoke((uint)page_address, main_data, oob_data, memory_area, ref WriteResult);
            if (!WriteResult)
                return false;
            if (PAGE_MAIN == 512L) // LEGACY NAND DEVICE
            {
                Utilities.Sleep(100);
            }
            else
            {
                Utilities.Sleep(10);
            }

            Ready?.Invoke();
            return WriteResult;
        }
        catch
        {
            return false;
        }
    }

    public byte[] READPAGE(long page_addr, ushort page_offset, uint count, FlashArea memory_area)
    {
        byte[] data_out = null;
        ReadPages?.Invoke((uint)page_addr, page_offset, count, memory_area, ref data_out);
        return data_out;
    }

    public bool EraseChip()
    {
        long PageAddr = 0L;
        for (int i = 0, loopTo = BLOCK_COUNT - 1; i <= loopTo; i++)
        {
            bool preserve = MainApp.MySettings.NAND_Preserve;
            if (MainApp.NAND_LayoutTool.Layout == NandMemLayout.Combined)
                preserve = false;
            bool Result = this.ERASEBLOCK(PageAddr, FlashArea.Main, preserve);
            if (!Result)
                return false;
            if (preserve && MEMORY_AREA_ERASED is object) // We need to write back the OOB
            {
                WritePages?.Invoke((uint)PageAddr, null, MEMORY_AREA_ERASED, FlashArea.OOB, ref Result);
                MEMORY_AREA_ERASED = null;
            }

            PageAddr += PAGE_COUNT;
            if (i % 10 == 0)
            {
                int Percent = (int)Math.Round(i / (double)BLOCK_COUNT * 100d);
                SetProgress?.Invoke(Percent);
            }
        }
        SetProgress?.Invoke(0);
        return true;
    }
}