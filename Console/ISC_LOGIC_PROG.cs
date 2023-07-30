// This provides in-circuit programming for MACHXO2 FPGA via SLAVE SPI
using System;
using System.Runtime.InteropServices;
using USB;

public class ISC_LOGIC_PROG {
    private FCUSB_DEVICE sel_usb_dev;
    private byte[] IDCODE_PUB = new byte[] { 0xE0, 0, 0, 0 };
    private byte[] ISC_ENABLE = new byte[] { 0xC6, 0x8, 0, 0 };
    private byte[] ISC_ERASE = new byte[] { 0xE, 0x4, 0, 0 };
    private byte[] ISC_PROGRAMDONE = new byte[] { 0x5E, 0, 0, 0 };
    private byte[] LSC_INITADDRESS = new byte[] { 0x46, 0, 0, 0 };
    private byte[] LSC_PROGINCRNV = new byte[] { 0x70, 0, 0, 1 };
    private byte[] LSC_READ_STATUS = new byte[] { 0x3C, 0, 0, 0 };
    private byte[] LSC_REFRESH = new byte[] { 0x79, 0, 0, 0 };

    public event PrintConsoleEventHandler PrintConsole;

    public delegate void PrintConsoleEventHandler(string msg);

    public event SetProgressEventHandler SetProgress;

    public delegate void SetProgressEventHandler(int value);

    private const uint MACHXO2_PAGE_SIZE = 16U;

    public ISC_LOGIC_PROG(FCUSB_DEVICE usb_dev) {
        sel_usb_dev = usb_dev;
    }

    public bool SSPI_Init(uint spi_mode, uint spi_select, uint speed_mhz) {
        try {
            uint w32 = spi_select << 24 | spi_mode << 16 | speed_mhz;
            return sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.SPI_INIT, null, w32);
        } catch {
            PrintConsole?.Invoke("SPI Init Failed");
            return false;
        }
    }

    public void SSPI_SS(bool enabled) {
        if (enabled) {
            sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.SPI_SS_ENABLE); // SS=LOW
        } else {
            sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.SPI_SS_DISABLE);
        } // SS=HIGH
    }

    public bool SSPI_WriteData(byte[] data) {
        bool result = sel_usb_dev.USB_SETUP_BULKOUT(USBREQ.SPI_WR_DATA, null, data, (uint)data.Length);
        Utilities.Sleep(2);
        return result;
    }

    public bool SSPI_ReadData(ref byte[] Data_In) {
        bool Success = false;
        try {
            Success = sel_usb_dev.USB_SETUP_BULKIN(USBREQ.SPI_RD_DATA, null, ref Data_In, (uint)Data_In.Length);
        } catch {
        }
        return Success;
    }

    public uint SSPI_WriteRead(byte[] WriteBuffer, [Optional, DefaultParameterValue(null)] ref byte[] ReadBuffer) {
        if (WriteBuffer is null & ReadBuffer is null)
            return 0U;
        uint TotalBytesTransfered = 0U;
        SSPI_SS(true);
        if (WriteBuffer is object) {
            bool Result = SSPI_WriteData(WriteBuffer);
            if (Result)
                TotalBytesTransfered = (uint)(TotalBytesTransfered + WriteBuffer.Length);
        }
        if (ReadBuffer is object) {
            bool Result = SSPI_ReadData(ref ReadBuffer);
            if (Result)
                TotalBytesTransfered = (uint)(TotalBytesTransfered + ReadBuffer.Length);
        }
        SSPI_SS(false);
        return TotalBytesTransfered;
    }

    public class SSPI_Status {
        public uint STATUS32;

        public bool DONE_FLAG { get; private set; }
        public CFG_CHECK_STATUS CHECK_STATUS { get; private set; }
        public bool CFG_IF_ENABLED { get; private set; }
        public bool BUSY_FLAG { get; private set; }
        public bool FAIL_FLAG { get; private set; }

        public SSPI_Status(byte[] cfg_register) {
            STATUS32 = Utilities.Bytes.ToUInt32(cfg_register);
            DONE_FLAG = ((STATUS32 >> 8 & 1) == 1);
            CFG_IF_ENABLED = ((STATUS32 >> 9 & 1) == 1);
            BUSY_FLAG = ((STATUS32 >> 12 & 1) == 1);
            FAIL_FLAG = ((STATUS32 >> 13 & 1) == 1);
            CHECK_STATUS = (CFG_CHECK_STATUS)(STATUS32 >> 23 & 7);
        }

        public enum CFG_CHECK_STATUS : byte {
            No_error = 0,
            ID_error = 1,
            CMD_error = 2,
            CRC_error = 3,
            Preamble_error = 4,
            Abort_error = 5,
            Overflow_error = 6,
            SDM_EOF = 7
        }
    }

    private bool SSPI_Wait() {
        int MACHXO2_MAX_BUSY_LOOP = 128;
        int counter = 0;
        SSPI_Status current_status = null;
        do {
            current_status = SSPI_ReadStatus();
            Utilities.Sleep(10);
            counter += 1;
            if (counter == MACHXO2_MAX_BUSY_LOOP)
                return false; // TIMEOUT
            if (current_status is null)
                return false; // ERROR
        }
        while (current_status.BUSY_FLAG);
        return true;
    }
    // If SPI Slave port is enabled, this will return 0x012BC043
    public uint SSPI_ReadIdent() {
        try {
            var ID = new byte[4];
            SSPI_WriteRead(IDCODE_PUB, ref ID);
            return Utilities.Bytes.ToUInt32(ID);
        } catch {
        }
        return 0U;
    }

    public SSPI_Status SSPI_ReadStatus() {
        try {
            var STATUS = new byte[4];
            SSPI_WriteRead(LSC_READ_STATUS, ref STATUS);
            return new SSPI_Status(STATUS);
        } catch {
        }
        return null;
    }
    // Fast! This will load an entire bitstream into the FPGA in 3 seconds.
    public bool SSPI_ProgramMACHXO(byte[] logic) {
        int bytes_left = logic.Length;
        MainApp.PrintConsole("Programming FPGA with bitstream (" + string.Format(bytes_left.ToString(), "#,###") + " bytes)");
        SetProgress?.Invoke(0);
        var status = SSPI_ReadStatus();
        if (status.BUSY_FLAG) {
            MainApp.PrintConsole("Error: FPGA device is busy");
            return false;
        }
        byte[] argReadBuffer = null;
        SSPI_WriteRead(ISC_ENABLE, ReadBuffer: ref argReadBuffer);
        status = SSPI_ReadStatus();
        if (status.FAIL_FLAG | !status.CFG_IF_ENABLED) {
            MainApp.PrintConsole("Error: unable to enabling configuration interface");
            return false;
        }

        int erase_attemps = 0;
        bool erase_failed;
        do {
            erase_failed = false;
            byte[] argReadBuffer1 = null;
            SSPI_WriteRead(ISC_ERASE, ReadBuffer: ref argReadBuffer1);
            SSPI_Wait();
            status = SSPI_ReadStatus();
            if (status.FAIL_FLAG | !(status.CHECK_STATUS == SSPI_Status.CFG_CHECK_STATUS.No_error)) {
                Utilities.Sleep(200);
                erase_failed = true;
                erase_attemps += 1;
                if (erase_attemps == 3) {
                    MainApp.PrintConsole("Error: logic erase command failed");
                    return false;
                }
            }
        }
        while (erase_failed);
        byte[] argReadBuffer2 = null;
        SSPI_WriteRead(LSC_INITADDRESS, ReadBuffer: ref argReadBuffer2);
        int spi_size = (int)(Math.Ceiling(logic.Length / (double)MACHXO2_PAGE_SIZE) * 4d + logic.Length);
        var spi_buffer = new byte[spi_size];
        int buffer_ptr = 0;
        int logic_ptr = 0;
        while (bytes_left > 0) {
            int data_count = (int)Math.Min(bytes_left, MACHXO2_PAGE_SIZE);
            Array.Copy(LSC_PROGINCRNV, 0, spi_buffer, buffer_ptr, LSC_PROGINCRNV.Length);
            buffer_ptr += LSC_PROGINCRNV.Length;
            Array.Copy(logic, logic_ptr, spi_buffer, buffer_ptr, data_count);
            buffer_ptr += data_count;
            logic_ptr += data_count;
            bytes_left -= data_count;
        }

        bytes_left = spi_buffer.Length;
        buffer_ptr = 0;
        while (bytes_left > 0) {
            int data_count = (int)Math.Min(bytes_left, (MACHXO2_PAGE_SIZE + LSC_PROGINCRNV.Length) * 128L); // 128 pages
            var buffer_out = new byte[data_count];
            Array.Copy(spi_buffer, buffer_ptr, buffer_out, 0, buffer_out.Length);
            uint setup = (uint)(MACHXO2_PAGE_SIZE + LSC_PROGINCRNV.Length << 16 | (long)data_count);
            bool result = sel_usb_dev.USB_SETUP_BULKOUT(USBREQ.SPI_REPEAT, null, buffer_out, setup);
            if (!result)
                return false;
            sel_usb_dev.USB_WaitForComplete();
            bytes_left -= data_count;
            buffer_ptr += data_count;
            int percent_done = (int)((spi_buffer.Length - bytes_left) / (double)spi_buffer.Length * 100d);
            if (percent_done > 100)
                percent_done = 100;
            SetProgress?.Invoke(percent_done);
        }

        byte[] argReadBuffer3 = null;
        SSPI_WriteRead(ISC_PROGRAMDONE, ReadBuffer: ref argReadBuffer3);
        SSPI_Wait();
        SetProgress?.Invoke(100);
        status = SSPI_ReadStatus();
        if (!status.DONE_FLAG)
            return false;
        byte[] argReadBuffer4 = null;
        SSPI_WriteRead(LSC_REFRESH, ReadBuffer: ref argReadBuffer4);
        status = SSPI_ReadStatus();
        if (!status.BUSY_FLAG && status.CHECK_STATUS == SSPI_Status.CFG_CHECK_STATUS.No_error) {
            return true;
        } else {
            return false;
        }
    }
    // For programming FlashcatUSB Professional PCB 5.0
    public bool SSPI_ProgramICE(byte[] logic) {
        try {
            SSPI_Init(3U, 1U, 24U); // MODE_3, CS_0, 24MHZ; PIN_FPGA_RESET=HIGH
            SSPI_SS(true); // SS_LOW
            sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.PULSE_RESET); // PCRESET_B=LOW; delay_us(200); PCRESET_B=HIGH
            SSPI_SS(false); // SS_HIGH
            this.SSPI_WriteData(new byte[] { 0 }); // 8 dummy clocks
            SSPI_SS(true); // SS_LOW
            SSPI_WriteData(logic);
            SSPI_SS(false); // SS_HIGH
            this.SSPI_WriteData(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            bool Success = SSPI_ICE_GetCDONE();
            if (Success) {
                sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.LOGIC_START); // SMC_Init(); PIN_FPGA_RESET=LOW
                }
            return Success;
        } catch {
        }
        return false;
    }

    public bool SSPI_ICE_GetCDONE() {
        var s = new byte[4];
        sel_usb_dev.USB_CONTROL_MSG_IN(USBREQ.LOGIC_STATUS, ref s);
        if (s[0] == 0)
            return false;
        return true;
    }

}
