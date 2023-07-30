'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2015 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'ACKNOWLEDGEMENT: USB driver functionality provided by LibUsbDotNet (sourceforge.net/projects/libusbdotnet)
'USB AVR stack provided by LUFA (fourwalledcubicle.com/LUFA.php), Thanks for your help Dean!

Imports LibUsbDotNet
Imports LibUsbDotNet.Info
Imports LibUsbDotNet.Main
Imports System.Threading

Public Class EJTAG

    Private LibusbDeviceFinder As UsbDeviceFinder = New UsbDeviceFinder(&H16C0, &H5DD)
    Private FCUSB As UsbDevice

    Public Structure JtagHost
        Public IDCODE As UInt32
        Public VERSION As Short
        Public PARTNU As UShort
        Public MANUID As UShort
        Public IMPCODE As UInt32
        Public IMPVER As String 'converted to text readable
        Public RK4_ENV As Boolean 'Indicates host is a R4000
        Public RK3_ENV As Boolean 'Indicates host is a R3000
        Public DINT_SUPPORT As Boolean 'Probe can use DINT signal to debug int on this cpu
        Public ASID_SIZE As Short '0=no ASIS,1=6bit,2=8bit
        Public MIPS16e As Boolean 'Indicates MIPS16e ASE support
        Public NoDMA As Boolean 'Indicates no DMA support and must use PrAcc mode
        Public MIPS32 As Boolean
        Public MIPS64 As Boolean
        Public LatticeCpld As Boolean 'Indicates this is a Lattice CPLD device
    End Structure
    'Type of the actual JTAG target device
    Public Enum TargetType
        Broadcom = 1
        Atheros = 2
        TexasInstruments = 3
    End Enum

    Public Enum cfi_mode As Byte
        Intel_16 = EJREQ_FLASHWRITE_I16
        AMD_16 = EJREQ_FLASHWRITE_A16
        SST = EJREQ_FLASHWRITE_SST
        NoBypass = EJREQ_FLASHWRITE_AMDNB
    End Enum

    Public WithEvents JSP As New JTAG.SVF_Player

    Public SUPPORT_DMA As Boolean = False
    Public SUPPORT_PRACC As Boolean = False 'Indicates FCUSB AVR includes PrAcc modes
    Public SUPPORT_INTELFLASH As Boolean = False 'Indicates FCUSB AVR can program Intel flashes directly
    Public SUPPORT_AMDFLASH As Boolean = False 'Indicates FCUSB AVR can program AMD flashes directly
    Public SUPPORT_LEGACYFLASH As Boolean = False 'Indicates FCUSB AVR can program SST flashes directly
    Public SUPPORT_JTAGSPI As Boolean = False 'Indicates FCUSB AVR can program SPI over JTAG

    Public BoardRev As Single = 0.0 'Set after you call GetFirmwareVersion, this is the FlashcatUSB firmware version
    Public AmdFlashDelay As Short = -1 'Delay in clock cycles (16MHz total), use -1 to define default setting of 250
    Public IntelFlashDelay As Short = -1 'Delay in clock cycles (16MHz total), use -1 to define default setting of 250
    Public TargetDevice As JtagHost 'Loaded after Init() has been called and returns true
    Public ClientLock As Object = New Object() 'Used to prevent cross-threading issues with the JTAG device
    Private IR_LEN As Integer 'Number of bits for the instruction register

    Private flash_td As Threading.Thread

    Private Sub UpdateSvfProgress(ByVal percent As Integer) Handles JSP.Progress


    End Sub

    Public Function EjtagCmd(ByVal Cmd32 As UInt32) As UInteger
        Return ejctrl(Cmd32)
    End Function
    'Compares two byte arrays (uses byref to decrease execution time) 0= SAME, otherwise is point that is different
    Private Function CompareByteArrays(ByRef b1() As Byte, ByRef b2() As Byte) As Integer
        If b1.Length = b2.Length Then
            Dim i As Integer = 0
            For i = 0 To b1.Length - 1
                If Not b1(i) = b2(i) Then Return (i + 1)
            Next
            Return 0
        Else
            Return -1 'Not same
        End If
        Return 0
    End Function
    'Returns name of AVR firmware (called from startup init)
    Public Function GetAvrVersion() As String
        Return GetFirmwareRev()
    End Function
    'Sets parameters in device
    Public Sub SetParameter(ByVal paramSelector As UInt32, ByVal paramValue As UInt32)
        setParam(paramSelector, paramValue)
    End Sub
    'Searches the dram memory for a CFI compatible flash device
    Public Sub FlashFind()
        flash_td = New Threading.Thread(AddressOf FlashFind_Thread)
        flash_td.IsBackground = True
        flash_td.Start()
    End Sub
    'Searches for a compatible CFI flash
    Private Sub FlashFind_Thread()
        Dim CfiDev As New CFI
        Dim FLASH_BASE As UInt32 = 0
        Try
            WriteConsole(RM.GetString("fcusb_jtag_searchcfi"))
            Dim i As UInteger
            For i = 0 To 4095
                FLASH_BASE = CUInt(i * &H100000) 'Skip addresses
                'Application.DoEvents()
                If i Mod 10 = 0 Then
                    Dim sstatus As String = "0x" & Hex(FLASH_BASE).PadLeft(8, CChar("0"))
                    SetStatus(String.Format(RM.GetString("fcusb_jtag_searchcfiat"), sstatus))
                End If
                If CfiDev.DetectFlash(FLASH_BASE) Then
                    Dim sstatus As String = "0x" & Hex(FLASH_BASE).PadLeft(8, CChar("0"))
                    SetStatus(String.Format(RM.GetString("fcusb_jtag_foundcfi"), sstatus))
                    WriteConsole(String.Format(RM.GetString("fcusb_jtag_foundat"), sstatus))
                    AddMemoryDevice(DeviceTypes.CFI, FLASH_BASE, 0, Nothing, "")
                    Exit Sub
                End If
            Next
        Catch ex As Exception
        End Try
        SetStatus(RM.GetString("fcusb_jtag_notfound"))
        WriteConsole(RM.GetString("fcusb_jtag_notfound"))
    End Sub
    'Attempts to auto-detect a JTAG device on the TAP, returns the IR Length of the device
    Public Function Chip_Detect() As UInt32
        Dim ret As Integer = 0
        Dim d As Byte() = New Byte(3) {}
        Dim dflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim usbPacket1 As New UsbSetupPacket(dflag, EJREQ_CHIPDETECT, 0, 0, 4)
        Dim ReadJtagIDResult As Boolean = FCUSB.ControlTransfer(usbPacket1, d, 4, ret)
        If Not ReadJtagIDResult Then Return 0 'Not detected
        If ret <> 4 Then Return 0
        Dim dint As UInteger = BitConverter.ToUInt32(d, 0)
        If Utilities.HWeight32(dint) <> 1 Then Return 0
        Dim ir As UInt32 = 0
        While dint <> 0
            dint >>= 1
            ir += 1
        End While
        ir -= 1
        Return ir
    End Function
    'Loads specific features this device/CPU supports
    Public Sub Chip_LoadFeatures(IMP As UInt32)
        Dim b As Long = (IMP And &HE0000000L) >> 29
        If b = 0 Then
            TargetDevice.IMPVER = "2.0" 'Also 1.0
        ElseIf b = 1 Then
            TargetDevice.IMPVER = "2.5"
        ElseIf b = 2 Then
            TargetDevice.IMPVER = "2.6"
        ElseIf b = 3 Then
            TargetDevice.IMPVER = "3.1"
        End If
        b = (IMP And &H10000000) >> 28 '28 bit (0 based)
        If b = 1 Then
            TargetDevice.RK4_ENV = False
            TargetDevice.RK3_ENV = True
        Else
            TargetDevice.RK4_ENV = True
            TargetDevice.RK3_ENV = False
        End If
        b = (IMP And &H1000000) >> 24 '24 bit (0 based)
        If b = 1 Then TargetDevice.DINT_SUPPORT = True Else TargetDevice.DINT_SUPPORT = False
        b = (IMP And &H600000) >> 21
        TargetDevice.ASID_SIZE = CShort(b)
        b = (IMP And &H10000) >> 16
        If b = 1 Then TargetDevice.MIPS16e = True Else TargetDevice.MIPS16e = False
        b = (IMP And &H4000) >> 14
        If b = 1 Then TargetDevice.NoDMA = True Else TargetDevice.NoDMA = False
        b = (IMP And &H1)
        If b = 1 Then TargetDevice.MIPS32 = False : TargetDevice.MIPS64 = True Else TargetDevice.MIPS32 = True : TargetDevice.MIPS64 = False
    End Sub
    'Connects to the target device
    Public Function Init() As Boolean
        LoadCapabilities()
        SPI_API_LOADED = False
        Dim ChipIrLen As Integer = Chip_Detect() 'Turns 4 for 3348?
        If ChipIrLen = 0 Then Return False
        If Not SetIRLen(ChipIrLen) Then Return False 'Makes us select this chip for communication
        WriteConsole(String.Format(RM.GetString("fcusb_jtag_irlen"), ChipIrLen.ToString))
        TargetDevice.IDCODE = GetIDCODE()
        If TargetDevice.IDCODE = 0 Then
            'ElseIf (TargetDevice.IDCODE = LATTICE_LC4032V) Then 'LATTICE DEVICE
            '    WriteConsole("Lattice CPLD device detected")
            '    SetIR(LATTICE_PART)
            '    WriteConsole("Part number: 0x" & Hex(ReadWriteDR(0)))
            '    TargetDevice.PARTNU = &H4032
            '    TargetDevice.LatticeCpld = True
            'ElseIf (TargetDevice.IDCODE = LATTICE_LC4064V) Then
            '    WriteConsole("Lattice CPLD device detected")
            '    SetIR(LATTICE_PART)
            '    WriteConsole("Part number: 0x" & Hex(ReadWriteDR(0)))
            '    TargetDevice.PARTNU = &H4064
            '    TargetDevice.LatticeCpld = True
        Else
            TargetDevice.VERSION = CShort((TargetDevice.IDCODE And &HF0000000L) >> 28)
            TargetDevice.PARTNU = CUShort((TargetDevice.IDCODE And &HFFFF000) >> 12)
            TargetDevice.MANUID = CUShort((TargetDevice.IDCODE And &HFFE) >> 1)
            TargetDevice.IMPCODE = GetImpCode()
            Chip_LoadFeatures(TargetDevice.IMPCODE)
            If TargetDevice.NoDMA Then 'Issues a processor reset
                ProcessReset()
            End If
            If (Not TargetDevice.NoDMA) Then 'We support DMA (enable memory write)
                Dim r As UInteger = dmaRead(&HFF300000UI, 0) 'Returns 2000001E 
                r = r And &HFFFFFFFBUI '2000001A
                dmaWrite(&HFF300000UI, r, 0)
            End If
            If TargetDevice.NoDMA And Not SUPPORT_PRACC Then
                WriteConsole(RM.GetString("fcusb_jtag_praccmode"))
            End If
            If TargetDevice.MANUID = &HBF Then
                SelectedAPI = BrcmAPI
                SPI_API_LOADED = True
            ElseIf TargetDevice.MANUID = &H70 Then
                SelectedAPI = AtherosAPI
                SPI_API_LOADED = True
            End If
        End If
        Return True
    End Function

    Private Sub HandleJtagPrintRequest(ByVal msg As String) Handles JSP.Printf
        WriteConsole("SVF Player: " & msg)
    End Sub

    Private Sub HandleJtagShiftRequest(ByVal BitCount As Integer, ByVal tdi_bits() As Byte, ByVal tms_bits() As Byte, ByRef tdo_bits() As Byte) Handles JSP.ShiftBits
        Array.Reverse(tdi_bits)
        Array.Reverse(tms_bits)
        tdo_bits = ShiftBulk(BitCount, tdi_bits, tms_bits)
        If tdo_bits IsNot Nothing Then Array.Reverse(tdo_bits)
    End Sub

#Region "SPI over JTAG"
    Friend BrcmAPI As New SPI_API(TargetType.Broadcom)
    Friend AtherosAPI As New SPI_API(TargetType.Atheros)
    Friend SelectedAPI As SPI_API 'Contains one of the above APIs to use
    Friend SPI_API_LOADED As Boolean = False
    Friend SPI_Part As FlashMemory.SPI_FLASH
    'Returns TRUE if the JTAG can detect a connected flash to the SPI port
    Public Function SPI_Detect() As Boolean
        Dim reg As UInt32 = SPI_SendCommand(SelectedAPI.READ_ID, 1, 3)
        WriteConsole(String.Format(RM.GetString("fcusb_jtag_spiregret"), "0x" & Hex(reg)))
        Dim ReadBack() As Byte = Utilities.Bytes.FromUInt32(reg, False) 'Returns 4 bytes
        If BIT_ENDIAN = BitEndianMode.LittleEndian_32bit Then
            Utilities.ReverseByteEndian_32bit(ReadBack)
        ElseIf BIT_ENDIAN = BitEndianMode.LittleEndian_16bit Then
            Utilities.ReverseByteEndian_16bit(ReadBack)
        End If
        Dim PartNum As Integer = (CInt(ReadBack(1)) << 8) + ReadBack(2)
        SPI_Part = FlashDatabase.FindDevice(ReadBack(0), PartNum, FlashMemory.MemoryType.JTAG)
        If SPI_Part IsNot Nothing Then
            WriteConsole(String.Format(RM.GetString("fcusb_jtag_spidetected"), SPI_Part.NAME))
            If SelectedAPI.DTYPE = TargetType.Broadcom Then
                Memory_Write_W(SelectedAPI.REG_CNTR, 0)
                SPI_ReadBulk(0, 4)
            End If
            Return True
        End If
        Return False
    End Function
    'Includes chip-specific API for connecting JTAG->SPI
    Public Structure SPI_API
        Sub New(ByVal Dev As TargetType)
            DTYPE = Dev
            BASE = &H1FC00000
            Select Case DTYPE
                Case TargetType.Broadcom
                    REG_CNTR = &H18000040
                    REG_OPCODE = &H18000044
                    REG_DATA = &H18000048
                    CTL_Start = &H80000000UI
                    CTL_Busy = &H80000000UI
                    READ_ID = &H49F
                    WREN = &H6
                    SECTORERASE = &H2D8
                    RD_STATUS = &H105
                    PAGEPRG = &H402
                Case TargetType.Atheros
                    REG_CNTR = &H11300000
                    REG_OPCODE = &H11300004
                    REG_DATA = &H11300008
                    CTL_Start = &H11300100
                    CTL_Busy = &H11310000
                    READ_ID = &H9F
                    WREN = &H6
                    SECTORERASE = &HD8
                    RD_STATUS = &H5
                    PAGEPRG = &H2
            End Select
        End Sub
        Public DTYPE As TargetType
        'Register addrs
        Public REG_CNTR As UInt32
        Public REG_DATA As UInt32
        Public REG_OPCODE As UInt32
        'Control CODES
        Public CTL_Start As UInt32
        Public CTL_Busy As UInt32
        'OP CODES
        Public READ_ID As UShort '16 bits
        Public WREN As UShort
        Public SECTORERASE As UShort
        Public RD_STATUS As UShort
        Public PAGEPRG As UShort

        Public BASE As UInt32
    End Structure

    Public Function SPI_ReadBulk(ByVal Address As UInteger, ByVal count As Integer) As Byte()
        Return Memory_Read_Bulk(SelectedAPI.BASE + Address, count)
    End Function

    Public Sub SPI_EraseBulk()
        WriteConsole(RM.GetString("fcusb_jtag_spierasing"))
        Memory_Write_W(SelectedAPI.REG_CNTR, 0) 'Might need to be for Ath too
        SPI_SendCommand(SelectedAPI.WREN, 1, 0)
        Memory_Write_W(SelectedAPI.REG_OPCODE, SelectedAPI.BASE)
        Memory_Write_W(SelectedAPI.REG_CNTR, &H800000C7UI) 'Might need to be for Ath too
        SPI_WaitUntilReady()
        WriteConsole(RM.GetString("fcusb_jtag_erasecomplete"))
    End Sub

    Public Sub SPI_SectorErase(ByVal Addr24 As UInt32)
        If SelectedAPI.DTYPE = TargetType.Broadcom Then Memory_Write_W(SelectedAPI.REG_CNTR, 0)
        SPI_SendCommand(SelectedAPI.WREN, 1, 0)
        Dim reg As UInt32 = SPI_GetControlReg()
        If SelectedAPI.DTYPE = TargetType.Broadcom Then
            Memory_Write_W(SelectedAPI.REG_OPCODE, Addr24 Or SelectedAPI.SECTORERASE)
            reg = (reg And &HFFFFFF00) Or SelectedAPI.SECTORERASE Or SelectedAPI.CTL_Start
            Memory_Write_W(SelectedAPI.REG_CNTR, reg)
            Memory_Write_W(SelectedAPI.REG_CNTR, 0)
        ElseIf SelectedAPI.DTYPE = TargetType.Atheros Then
            Memory_Write_W(SelectedAPI.REG_OPCODE, (Addr24 << 8) Or SelectedAPI.SECTORERASE)
            reg = (reg And &HFFFFFF00) Or &H4 Or SelectedAPI.CTL_Start
            Memory_Write_W(SelectedAPI.REG_CNTR, reg)
        End If
        SPI_WaitUntilReady()
    End Sub

    Public Function SPI_WriteData(ByVal Address As UInt32, ByVal data() As Byte) As Boolean
        Try
            Do Until data.Length Mod 4 = 0
                ReDim Preserve data(data.Length)
                data(data.Length - 1) = 255
            Loop
            Dim TotalBytes As Integer = data.Length
            Dim WordBytes As Integer = CInt(Math.Floor(TotalBytes / 4) * 4)
            Dim DataToWrite(WordBytes - 1) As Byte 'Word aligned
            Array.Copy(data, DataToWrite, WordBytes)
            Dim BytesRemaining As Integer = DataToWrite.Length
            Dim BytesWritten As Integer = 0
            Dim BlockToWrite() As Byte
            While BytesRemaining > 0
                If BytesRemaining > FCUSB2_MAX_USB_BUFFER_SIZE Then
                    ReDim BlockToWrite(FCUSB2_MAX_USB_BUFFER_SIZE - 1)
                Else
                    ReDim BlockToWrite(BytesRemaining - 1)
                End If
                Array.Copy(DataToWrite, BytesWritten, BlockToWrite, 0, BlockToWrite.Length)
                If SUPPORT_JTAGSPI And (Not TargetDevice.NoDMA) Then
                    spiWrite(Address, BlockToWrite, SelectedAPI.DTYPE)
                Else 'Very slow
                    SPI_WriteData_Slow(Address, BlockToWrite)
                End If
                BytesWritten += BlockToWrite.Length
                BytesRemaining -= BlockToWrite.Length
                Address += CUInt(BlockToWrite.Length)
            End While
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Sub SPI_WriteData_Slow(ByVal Addr24 As UInteger, ByVal data() As Byte)
        For i = 0 To (data.Length - 1) Step 4
            SPI_SendCommand(SelectedAPI.WREN, 1, 0)
            Dim data_out As UInt32 = CUInt(data(i)) << 24
            data_out += CUInt(data(i + 1)) << 16
            data_out += CUInt(data(i + 2)) << 8
            data_out += CUInt(data(i + 3))
            Memory_Write_W(SelectedAPI.REG_DATA, data_out)
            If SelectedAPI.DTYPE = TargetType.Broadcom Then
                Memory_Write_W(SelectedAPI.REG_OPCODE, Addr24)
                Memory_Write_W(SelectedAPI.REG_CNTR, &H80000402UI)
            ElseIf SelectedAPI.DTYPE = TargetType.Atheros Then
                Dim reg As UInt32 = SPI_GetControlReg()
                Memory_Write_W(SelectedAPI.PAGEPRG, (SelectedAPI.PAGEPRG Or (Addr24 << 8)))
                reg = (reg And &HFFFFFF00) Or 8 Or SelectedAPI.CTL_Start
                Memory_Write_W(SelectedAPI.REG_CNTR, reg)
            End If
            SPI_WaitUntilReady()
            Addr24 += 4
        Next
    End Sub

    Public Function SPI_SendCommand(ByVal SPI_OPCODE As UShort, ByVal BytesToWrite As UInt32, ByVal BytesToRead As UInt32) As UInt32
        If SelectedAPI.DTYPE = TargetType.Broadcom Then Memory_Write_W(SelectedAPI.REG_CNTR, 0)
        Dim reg As UInt32 = SPI_GetControlReg() 'Zero
        If SelectedAPI.DTYPE = TargetType.Broadcom Then
            reg = (reg And &HFFFFFF00) Or SPI_OPCODE Or SelectedAPI.CTL_Start
        ElseIf SelectedAPI.DTYPE = TargetType.Atheros Then
            reg = (reg And &HFFFFFF00) Or BytesToWrite Or (BytesToRead << 4) Or SelectedAPI.CTL_Start
        End If
        Memory_Write_W(SelectedAPI.REG_OPCODE, SPI_OPCODE)
        Memory_Write_W(SelectedAPI.REG_CNTR, reg)
        SPI_GetControlReg()
        If BytesToRead > 0 Then
            reg = Memory_Read_W(SelectedAPI.REG_DATA)
            Select Case BytesToRead
                Case 1
                    reg = (reg And &HFF)
                Case 2
                    reg = (reg And &HFFFF)
                Case 3
                    reg = (reg And &HFFFFFF)
            End Select
            Return reg 'CMD = 0
        End If
        Return 0
    End Function

    Public Sub SPI_EraseSector(ByVal secNum As Integer)
        Try
            Dim Addr As UInteger = SPI_FindSectorBase(secNum)
            SPI_SectorErase(Addr) 'Sends offset, does not send with BASE address
            SPI_GetControlReg()
        Catch ex As Exception
        End Try
    End Sub
    'Returns the total number of sectors
    Public Function SPI_GetFlashSectors() As Integer
        Dim secSize As Integer = &H10000 '64KB
        Dim totalsize As Integer = SPI_Part.FLASH_SIZE
        Return CInt(totalsize / secSize)
    End Function

    Public Sub SPI_WriteSector(ByVal secNum As Integer, ByVal data() As Byte)
        Dim Addr As UInteger = SPI_FindSectorBase(secNum)
        SPI_WriteData(Addr, data)
    End Sub

    Public Function SPI_FindSectorBase(ByVal sectorInt As Integer) As UInteger
        Return CUInt(SPI_GetSectorSize() * sectorInt)
    End Function

    Public Function SPI_GetSectorSize() As Integer
        Return &H10000
    End Function

    Public Function SPI_GetControlReg() As UInt32
        Dim i As Integer
        Dim reg As UInt32
        Do
            If Not i = 0 Then Thread.Sleep(50)
            reg = Memory_Read_W(SelectedAPI.REG_CNTR)
            i = i + 1
            If i = 10 Then Return 0
        Loop While ((reg And SelectedAPI.CTL_Busy) > 0)
        Return reg
    End Function

    Public Sub SPI_WaitUntilReady()
        Dim reg As UInt32
        Do
            reg = SPI_SendCommand(SelectedAPI.RD_STATUS, 1, 4)
        Loop While ((reg And 1) > 0)
    End Sub

#End Region

    Public Function Extest(ByVal bsrlen As UShort, ByVal DataToLoad() As Byte) As Byte()
        Dim xfer As Integer = 0 'Number of bytes transfered back
        If bsrlen > 1024 Then Return Nothing 'Over 1024 bits and we will need to do some memory management
        Dim ByteCount As UShort = Math.Ceiling(bsrlen / 8)
        Dim control As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim setup As New UsbSetupPacket(control, EJREQ_EXTEST, bsrlen, ByteCount, CShort(ByteCount))
        Dim res As Boolean = fcusb.ControlTransfer(setup, DataToLoad, CShort(ByteCount), xfer)
        If Not res Then Return Nothing
        Dim DataBack(ByteCount - 1) As Byte
        Dim reader As UsbEndpointReader = fcusb.OpenEndpointReader(ReadEndpointID.Ep01)
        Dim ec As ErrorCode = reader.Read(DataBack, 0, ByteCount, 5000, xfer)
        If ec = ErrorCode.None Then Return DataBack
        Return Nothing
    End Function

    Public Function Sample(ByVal bsrlen As UShort) As Byte()
        If bsrlen > 1024 Then Return Nothing 'Over 1024 bits and we will need to do some memory management
        Dim ByteCount As UShort = Math.Ceiling(bsrlen / 8)
        Dim buffer(ByteCount - 1) As Byte
        Dim dflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim usbPacket2 As New UsbSetupPacket(dflag, EJREQ_SAMPLE, bsrlen, ByteCount, CShort(ByteCount))
        Dim xfer As Integer = 0
        Dim res As Boolean = fcusb.ControlTransfer(usbPacket2, buffer, ByteCount, xfer)
        If Not res Then Return Nothing
        Return buffer
    End Function

    Public Function Preload(ByVal bsrlen As UShort, ByVal DataToLoad() As Byte) As Boolean
        Dim xfer As Integer = 0 'Number of bytes transfered back
        If bsrlen > 1024 Then Return Nothing 'Over 1024 bits and we will need to do some memory management
        Dim ByteCount As UShort = Math.Ceiling(bsrlen / 8)
        Dim control As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim setup As New UsbSetupPacket(control, EJREQ_PRELOAD, bsrlen, ByteCount, CShort(ByteCount))
        Dim res As Boolean = fcusb.ControlTransfer(setup, DataToLoad, CShort(ByteCount), xfer)
        Return res
    End Function

    Public Sub ProcessReset()
        set_instr(EJTAG_CONTROL_IR)
        Dim ctrl_reg As UInt32 = ReadWriteData(PRRST Or PERRST)
        ctrl_reg = ctrl_reg And (Not (PRRST Or PERRST))
        ctrl_reg = ReadWriteData(ctrl_reg)
        WriteConsole(RM.GetString("fcusb_jtag_cpureset"))
    End Sub

    Public Sub DebugMode(ByVal Enabled As Boolean)
        set_instr(EJTAG_CONTROL_IR)
        If Enabled Then
            ReadWriteData(PRACC Or PROBEN Or SETDEV Or JTAGBRK)
            If CBool((ReadWriteData(PRACC Or PROBEN Or SETDEV) And BRKST)) Then
                WriteConsole(RM.GetString("fcusb_jtag_debugon"))
            End If
        Else
            ReadWriteData(PROBEN Or SETDEV) 'This clears the JTAGBRK bit
        End If
    End Sub
    'Target device needs to support DMA and FCUSB needs to include flashmode
    Public Sub dmaWriteFlash(ByVal Address As UInt32, ByVal DataToWrite() As Byte, ByVal flashmode As cfi_mode)
        Dim BlankCheck As Boolean = True
        Dim BytesWritten As Integer = 0
        For i = 0 To DataToWrite.Length - 1
            If DataToWrite(i) = &HFF Then BlankCheck = False : Exit For
        Next
        If BlankCheck Then Exit Sub 'No need to write blank data
        Monitor.Enter(ClientLock)
        Try
            Dim BufferIndex As Integer = 0
            Dim BytesLeft As Integer = DataToWrite.Length
            Dim Counter As Integer = 0
            Do Until BytesLeft = 0
                If BytesLeft > FCUSB2_MAX_USB_BUFFER_SIZE Then
                    Dim Packet(FCUSB2_MAX_USB_BUFFER_SIZE - 1) As Byte
                    Array.Copy(DataToWrite, BufferIndex, Packet, 0, Packet.Length)
                    dmaWriteFlash_writeblock(Address, Packet, flashmode)
                    Address = Address + FCUSB2_MAX_USB_BUFFER_SIZE
                    BufferIndex = BufferIndex + FCUSB2_MAX_USB_BUFFER_SIZE
                    BytesLeft = BytesLeft - FCUSB2_MAX_USB_BUFFER_SIZE
                    BytesWritten += FCUSB2_MAX_USB_BUFFER_SIZE
                Else
                    Dim Packet(BytesLeft - 1) As Byte
                    Array.Copy(DataToWrite, BufferIndex, Packet, 0, Packet.Length)
                    dmaWriteFlash_writeblock(Address, Packet, flashmode)
                    BytesLeft = 0
                End If
                Counter += 1
            Loop
        Finally
            Monitor.Exit(ClientLock)
        End Try
    End Sub

    Public Function Memory_Read_B(ByVal addr As UInteger) As Byte
        If TargetDevice.NoDMA Then
            Dim data As UInteger = praccRead(addr, 1)
            Return CByte(&HFF And data)
        Else
            Dim Offset As UInt32 = (addr Mod 2)
            If Offset = 0 Then
                Dim data As UInteger = dmaRead(addr, 2)
                Return CByte(data)
            Else
                Dim data As UInteger = dmaRead(addr, 1)
                Return CByte(&HFF And data)
            End If
        End If
    End Function

    Public Function Memory_Read_H(ByVal addr As UInteger) As UShort
        If TargetDevice.NoDMA Then
            Return praccRead(addr, 1)
        Else
            Dim data As UInteger = dmaRead(addr, 1)
            Return CUShort(data)
        End If
    End Function

    Public Function Memory_Read_W(ByVal addr As UInteger) As UInteger
        If TargetDevice.NoDMA Then
            Return praccRead(addr, 0)
        Else
            Return dmaRead(addr, 0)
        End If
    End Function
    'Reads data from DRAM (optomized for speed)
    Public Function Memory_Read_Bulk(ByVal Address As UInteger, ByVal count As Integer) As Byte()
        Dim DramStart As UInt32 = Address
        Dim LargeCount As Integer = count 'The total amount of data we need to read in
        Do Until Address Mod 4 = 0 Or Address = 0
            Address = CUInt(Address - 1)
            LargeCount = LargeCount + 1
        Loop
        Do Until LargeCount Mod 4 = 0
            LargeCount = LargeCount + 1
        Loop 'Now StartAdd2 and ByteLen2 are on bounds of 4
        Dim TotalBuffer(LargeCount - 1) As Byte
        Dim BytesLeft As Integer = LargeCount
        While BytesLeft > 0
            Dim BytesToRead As Integer = BytesLeft
            If BytesToRead > FCUSB2_MAX_USB_BUFFER_SIZE Then BytesToRead = FCUSB2_MAX_USB_BUFFER_SIZE
            Dim Offset As UInt32 = LargeCount - BytesLeft
            Dim TempBuffer() As Byte = Nothing
            If TargetDevice.NoDMA Then
                TempBuffer = praccRead_Bulk(Address + Offset, BytesToRead)
            Else
                TempBuffer = dmaRead_bulk(Address + Offset, BytesToRead)
            End If
            If TempBuffer Is Nothing OrElse Not TempBuffer.Length = BytesToRead Then
                ReDim Preserve TempBuffer(BytesToRead - 1) 'Fill buffer with blank data
            End If
            Array.Copy(TempBuffer, 0, TotalBuffer, Offset, TempBuffer.Length)
            BytesLeft -= BytesToRead
        End While
        Dim OutByte(count - 1) As Byte
        Array.Copy(TotalBuffer, LargeCount - count, OutByte, 0, count)
        Return OutByte
    End Function

    Public Function Memory_Write_B(ByVal addr As UInteger, ByVal data As UInteger) As Boolean
        If TargetDevice.NoDMA Then
            praccWrite(addr, CByte(data And &HFF), 2)
            Return True
        Else
            data = CUInt(data And &HFF)
            data = (data << 24) Or (data << 16) Or (data << 8) Or data
            dmaWrite(addr, data, 2)
            Return True
        End If
    End Function

    Public Function Memory_Write_H(ByVal addr As UInteger, ByVal data As UInteger) As Boolean
        If TargetDevice.NoDMA Then
            praccWrite(addr, CUShort(data And &HFFFF), 1)
            Return True
        Else
            data = CUInt((data And &HFFFF) Or (data << 16))
            dmaWrite(addr, data, 1)
            Return True
        End If
    End Function

    Public Function Memory_Write_W(ByVal addr As UInteger, ByVal data As UInteger) As Boolean
        If TargetDevice.NoDMA Then
            praccWrite(addr, data, 0)
            Return True
        Else
            dmaWrite(addr, data, 0)
            Return True
        End If
    End Function
    'Writes an unspecified amount of b() into memory (usually DRAM)
    Public Function Memory_Write_Bulk(ByVal Address As UInt32, ByVal data() As Byte) As Boolean
        Try
            Dim TotalBytes As Integer = data.Length
            Dim WordBytes As Integer = CInt(Math.Floor(TotalBytes / 4) * 4)
            Dim DataToWrite(WordBytes - 1) As Byte 'Word aligned
            Array.Copy(data, DataToWrite, WordBytes)
            Dim BytesRemaining As Integer = DataToWrite.Length
            Dim BytesWritten As Integer = 0
            Dim BlockToWrite() As Byte
            While BytesRemaining > 0
                If BytesRemaining > FCUSB2_MAX_USB_BUFFER_SIZE Then
                    ReDim BlockToWrite(FCUSB2_MAX_USB_BUFFER_SIZE - 1)
                Else
                    ReDim BlockToWrite(BytesRemaining - 1)
                End If
                Array.Copy(DataToWrite, BytesWritten, BlockToWrite, 0, BlockToWrite.Length)
                If TargetDevice.NoDMA Then
                    praccWrite_Bulk(Address, BlockToWrite)
                Else
                    dmaWrite_bulk(Address, BlockToWrite)
                End If
                BytesWritten += BlockToWrite.Length
                BytesRemaining -= BlockToWrite.Length
                Address += CUInt(BlockToWrite.Length)
            End While
            If Not TotalBytes = WordBytes Then 'Writes the bytes left over is less than 4
                For i = 0 To (TotalBytes - WordBytes) - 1
                    Memory_Write_B(CUInt(Address + WordBytes + i), data(WordBytes + i))
                Next
            End If
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function
    ''Needs to be optomized (currently reads at around 930 bytes)
    Private Function praccRead_Bulk_slow(ByVal addr As UInt32, ByVal count As Integer) As Byte()
        Dim BufferOut(count - 1) As Byte
        For i = 0 To count - 1 Step 4
            Dim w As UInt32 = praccRead(addr + i, 0)
            Dim b() As Byte = BitConverter.GetBytes(w)
            BufferOut(i + 0) = b(0)
            BufferOut(i + 1) = b(1)
            BufferOut(i + 2) = b(2)
            BufferOut(i + 3) = b(3)
        Next
        Return BufferOut
    End Function

    Private Function praccRead_Bulk(ByVal Address As UInteger, ByVal count As Integer) As Byte()
        If count < 1 Or count > FCUSB2_MAX_USB_BUFFER_SIZE Then Return Nothing
        Dim dataout() As Byte
        Monitor.Enter(ClientLock)
        Try
            Dim ret As Integer
            Dim SizeArray() As Byte = {CByte(count And &HFF), CByte((count And &HFF00) >> 8)}
            Dim lowaddr As UShort = CUShort(&HFFFF And Address)
            Dim highaddr As UShort = CUShort(Address >> 16)
            Dim rtype As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim setuppacket As New UsbSetupPacket(rtype, EJREQ_PRACCREAD_BULK, lowaddr, highaddr, CShort(2))
            Dim res As Boolean = fcusb.ControlTransfer(setuppacket, SizeArray, 2, ret)
            If Not res Then Return Nothing
            Dim reader As UsbEndpointReader = fcusb.OpenEndpointReader(ReadEndpointID.Ep01, count, EndpointType.Bulk)
            ReDim dataout(count - 1)
            Dim ec As ErrorCode = reader.Read(dataout, 0, count, 5000, ret)
            If Not ec = ErrorCode.None Or Not ret = count Then dataout = Nothing
        Finally
            Monitor.Exit(ClientLock)
        End Try
        Return dataout
    End Function
    'Needs to be optomized
    Private Function praccWrite_Bulk_slow(ByVal addr As UInt32, ByVal data() As Byte) As Integer
        If fcusb Is Nothing Then Return Nothing
        Dim isopen As Boolean = fcusb.IsOpen
        Dim count As Integer = data.Length
        If count < 1 Or count > FCUSB2_MAX_USB_BUFFER_SIZE Then Return Nothing
        Dim BytesWritten As Integer = 0
        Monitor.Enter(ClientLock)
        Try
            Dim SizeArray() As Byte = {CByte(count And &HFF), CByte((count And &HFF00) >> 8)}
            Dim lowaddr As UShort = CUShort(&HFFFF And addr)
            Dim highaddr As UShort = CUShort(addr >> 16)
            For i = 0 To (data.Length - 1) Step 4
                Dim wout As UInt32 = (CUInt(data(i)) << 24) + (CUInt(data(i + 1)) << 16) + (CUInt(data(i + 2)) << 8) + (CUInt(data(i + 3)))
                praccWrite(addr, wout, 0)
                addr = addr + 1
            Next
        Finally
            Monitor.Exit(ClientLock)
        End Try
        Return BytesWritten
    End Function

    Private Function praccWrite_Bulk(ByVal Address As UInteger, ByVal Data() As Byte) As Integer
        If fcusb Is Nothing Then Return Nothing
        Dim isopen As Boolean = fcusb.IsOpen
        Dim count As Integer = Data.Length
        If count < 1 Or count > FCUSB2_MAX_USB_BUFFER_SIZE Then Return Nothing
        Dim BytesWritten As Integer = 0
        Monitor.Enter(ClientLock)
        Try
            Dim ret As Integer
            Dim SizeArray() As Byte = {CByte(count And &HFF), CByte((count And &HFF00) >> 8)}
            Dim lowaddr As UShort = CUShort(&HFFFF And Address)
            Dim highaddr As UShort = CUShort(Address >> 16)
            Dim rtype As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim setuppacket As New UsbSetupPacket(rtype, EJREQ_PRACCWRITE_BULK, lowaddr, highaddr, CShort(2))
            Dim res As Boolean = fcusb.ControlTransfer(setuppacket, SizeArray, 2, ret)
            If Not res Then Return -1
            Dim writer As UsbEndpointWriter = fcusb.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
            Dim ec As ErrorCode = writer.Write(Data, 5000, BytesWritten)
            If Not ec = ErrorCode.None Or Not BytesWritten = count Then BytesWritten = -1
        Finally
            Monitor.Exit(ClientLock)
        End Try
        Return BytesWritten
    End Function

#Region "Manual PrAcc Module"
    Private Sub ExecuteDebugModule(ByVal pmodule() As UInteger)
        Dim Accessed As Boolean = False 'True once the vector address has been read
        Dim ctrl_reg As UInteger
        Dim address As UInteger
        Dim data As UInteger = 0
        Dim offset As UInteger = 0
        Do
            Do
                set_instr(EJTAG_CONTROL_IR)
                ctrl_reg = ReadWriteData(PRACC Or PROBEN Or SETDEV)
                If ctrl_reg And PRACC <> 0 Then Exit Do
            Loop
            set_instr(EJTAG_ADDRESS_IR)
            address = ReadWriteData(0)
            If CBool(ctrl_reg And PRNW) Then ' Bit set for a WRITE
                ' Read the data out
                set_instr(EJTAG_DATA_IR)
                data = ReadWriteData(0)
                ' Clear the access pending bit (let the processor eat!)
                set_instr(EJTAG_CONTROL_IR)
                ctrl_reg = ReadWriteData(PROBEN Or SETDEV)
                ' If processor is writing to one of our psuedo virtual registers then save off data
                If address = MIPS_VIRTUAL_ADDRESS_ACCESS Then
                    PRACC_ADDR_REG = data
                End If
                If address = MIPS_VIRTUAL_DATA_ACCESS Then
                    PRACC_DATA_REG = data
                End If
            Else
                If address = MIPS_DEBUG_VECTOR_ADDRESS Then
                    If Accessed Then Exit Sub 'Module complete
                    Accessed = True
                End If
                If address >= MIPS_DEBUG_VECTOR_ADDRESS Then
                    offset = (address - MIPS_DEBUG_VECTOR_ADDRESS) / 4
                    If offset < pmodule.Length Then
                        data = pmodule(offset)
                    Else
                        data = 0 'Send NOP
                    End If
                Else
                    If address = MIPS_VIRTUAL_ADDRESS_ACCESS Then
                        data = PRACC_ADDR_REG
                    End If
                    If address = MIPS_VIRTUAL_DATA_ACCESS Then
                        data = PRACC_DATA_REG
                    End If
                End If
                set_instr(EJTAG_DATA_IR)
                data = ReadWriteData(data)
                set_instr(EJTAG_CONTROL_IR)
                ctrl_reg = ReadWriteData(PROBEN Or SETDEV)
            End If
        Loop
    End Sub
    'Sets the instruction register
    Private Sub set_instr(ByVal instr As Integer)
        'Assume we are on Select-DR-SCAN
        Shiftout(5 + IR_LEN, instr << 3, 1 Or (7 << (IR_LEN + 2)))
    End Sub

    Private Function ReadWriteData(ByVal data As UInt32) As UInt32
        Shiftout(2, 0, 0) 'capture-dr -> shift-dr
        Dim res As UInt32 = ShiftIn(32, data, &H80000000UI) 'Shifts out 32-bits and goes into Exit-DR
        Shiftout(2, 0, 3) 'Goes to Update-DR -> Select-DR-Scan
        Return res
    End Function

    Dim pracc_readbyte_code_module() As UInteger = {&H3C01FF20, &H34210000, &H8C220000UI, &H90430000UI, &HAC230008UI, &H0, &H1000FFF9, &H0, &H0}
    Dim pracc_writebyte_code_module() As UInteger = {&H3C01FF20, &H34210000, &H8C220000UI, &H8C230004UI, &HA0430000UI, &H0, &H1000FFF9, &H0, &H0}
    Dim pracc_readword_code_module() As UInteger = {&H3C01FF20, &H34210000, &H8C220000UI, &H8C430000UI, &HAC230004UI, &H0, &H1000FFF9, &H0, &H0}
    Dim pracc_writeword_code_module() As UInteger = {&H3C01FF20, &H34210000, &H8C220000UI, &H8C230004UI, &HAC430000UI, &H0, &H1000FFF9, &H0, &H0}
    Dim pracc_readhalf_code_module() As UInteger = {&H3C01FF20, &H34210000, &H8C220000UI, &H94430000UI, &HAC230004UI, &H0, &H1000FFF9, &H0, &H0}
    Dim pracc_writehalf_code_module() As UInteger = {&H3C01FF20, &H34210000, &H8C220000UI, &H8C230004UI, &HA4430000UI, &H0, &H1000FFF9, &H0, &H0}
    Dim pracc_return_from_debug() As UInteger = {&H0, &H0, &H0, &H0, &H4200001F, &H0, &H1000FFF9, &H0}
    Dim pracc_read_depc() As UInteger = {&H3C01FF20, &H34210000, &H4002C000, &H0, &HAC220004UI, &H0, &H1000FFF9, &H0}
    Dim pracc_write_depc() As UInteger = {&H3C01FF20, &H34210000, &H8C220004UI, &H4082C000, &H0, &H0, &H1000FFF9, &H0}
    Dim pracc_init_dreg() As UInteger = {&H3C01FF20, &H0, &H1000FFFD, &H0}
    Dim pracc_readword_new() As UInteger = {&H3C021F01, &H8C438100UI, &HAC230004UI, &H0, &H1000FFFB, &H0}
    Dim pracc_readhalf_new() As UInteger = {&H3C021F01, &H94438100UI, &HAC230004UI, &H0, &H1000FFFB, &H0}
    Dim pracc_writehalf_new() As UInteger = {&H3C021F01, &H34030010, &HA4438100UI, &H1000FFFC, &H0}
    Dim pracc_writeword_new() As UInteger = {&H3C021F01, &H3C030001, &H34631000, &HAC438100UI, &H1000FFFB, &H0}
    Dim init_5352() As UInteger = {&H0, &HE021, &H3C09FF40, &H3529000C, &H40826000, &H0, &H1000FFF9, &H0}
    Dim cpu_ejtag() As UInteger = {&H81C0818, &H4200001F, &H0}
#End Region

#Region "Constants"

    Const MIPS_DEBUG_VECTOR_ADDRESS As UInt32 = &HFF200200UI
    Const MIPS_VIRTUAL_ADDRESS_ACCESS As UInt32 = &HFF200000UI
    Const MIPS_VIRTUAL_DATA_ACCESS As UInt32 = &HFF200004UI

    Private Const FCUSB2_MAX_USB_BUFFER_SIZE As UShort = 4096 'Max number of bytes we should send via USB bulk endpoints

    'LATTICE
    Private Const LATTICE_PART As Byte = &HF
    Private Const LATTICE_IDCODE As Byte = &H16
    'Private Const LATTICE_LC4064V As UInt32 = &H01809043UI
    'Private Const LATTICE_LC4032V As UInt32 = &H01805043UI

    'EJTAG INSTRUCTION OP-CODES
    Private Const JTAG_EXTEST As Byte = &H0 'Free for other use, such as JTAG boundary scan
    Private Const EJTAG_IDCODE As Byte = &H1 'Selects Device Identiﬁcation (ID) register
    Private Const EJTAG_IR_SAMPLE As Byte = &H2 'Free for other use, such as JTAG boundary scan
    Private Const EJTAG_IMPCODE As Byte = &H3 'Selects Implementation register
    Private Const EJTAG_ADDRESS_IR As Byte = &H8 'Selects Address register
    Private Const EJTAG_DATA_IR As Byte = &H9 'Selects Data register
    Private Const EJTAG_CONTROL_IR As Byte = &HA 'Selects EJTAG Control registe
    Private Const EJTAG_IR_ALL As Byte = &HB 'Selects the Address, Data and EJTAG Control registers
    Private Const EJTAG_IR_EJTAGBOOT As Byte = &HC 'Makes the processor take a debug exception after rese
    Private Const EJTAG_IR_NORMALBOOT As Byte = &HD 'Makes the processor execute the reset handler after rese
    Private Const EJTAG_IR_FASTDATA As Byte = &HE 'Selects the Data and Fastdata registers
    Private Const EJTAG_IR_TABCTRA As Byte = &H10 'Selects the control register TCBTraceControl in the Trace Control Bloc
    Private Const EJTAG_IR_TABCTRB As Byte = &H11 'Selects another trace control block register
    Private Const EJTAG_IR_TABCTRDATA As Byte = &H12 'Used to access the registers speciﬁed by the TCBCONTROLBREG ﬁeld and transfers data between the TAP and the TCB control register
    Private Const EJTAG_IR_TABCTRC As Byte = &H13 'Selects another trace control block register
    Private Const EJTAG_IR_EJWATCH As Byte = &H1C
    Private Const EJTAG_IR_BYPASS As Byte = &H1F 'Select Bypass register

    'TARGET CONTROL REGISTER BITS
    Private Const TOF As Integer = &H2
    Private Const TIF As Integer = &H4
    Private Const BRKST As Integer = &H8
    Private Const DINC As Integer = &H10
    Private Const DLOCK As Integer = &H20
    Private Const JTS_BYTE As Integer = &H0
    Private Const JTS_HALFWORD As Integer = &H80
    Private Const JTS_WORD As Integer = &H100
    Private Const JTS_TRIPLEBYTE As Integer = &H180
    Private Const DRWN As UInteger = &H200
    Private Const DERR As UInteger = &H400
    Private Const DSTRT As UInteger = &H800
    Private Const JTAGBRK As UInteger = &H1000
    Private Const SETDEV As UInteger = &H4000
    Private Const PROBEN As UInteger = &H8000
    Private Const PRRST As UInteger = &H10000
    Private Const DMAACC As UInteger = &H20000
    Private Const PRACC As UInteger = &H40000
    Private Const PRNW As UInteger = &H80000
    Private Const PERRST As UInteger = &H100000
    Private Const RUN As UInteger = &H200000
    Private Const DOZE As UInteger = &H400000
    Private Const SYNC As UInteger = &H800000
    Private Const PCLEN As UInteger = &H1000000
    'USB CONTROL COMMANDS
    Private Const EJREQ_ECHO As Byte = &H10
    Private Const EJREQ_CHIPDETECT As Byte = &H11
    Private Const EJREQ_FIRMWARE As Byte = &H12
    Private Const EJREQ_BOOTLOADER As Byte = &H13
    Private Const EJREQ_LEDON As Byte = &H20
    Private Const EJREQ_LEDOFF As Byte = &H21
    Private Const EJREQ_LEDBLINK As Byte = &H22
    Private Const EJREQ_RESETTAP As Byte = &H30
    Private Const EJREQ_SETIR As Byte = &H33
    Private Const EJREQ_SETIRLEN As Byte = &H34
    Private Const EJREQ_RWDATA As Byte = &H35
    Private Const EJREQ_DMAREAD_B As Byte = &H36
    Private Const EJREQ_DMAREAD_H As Byte = &H37
    Private Const EJREQ_DMAREAD_W As Byte = &H38
    Private Const EJREQ_DMAWRITE_B As Byte = &H39
    Private Const EJREQ_DMAWRITE_H As Byte = &H3A
    Private Const EJREQ_DMAWRITE_W As Byte = &H3B
    Private Const EJREQ_DMAREADBULK As Byte = &H3C
    Private Const EJREQ_DMAWRITEBULK As Byte = &H3D
    Private Const EJREQ_PRACCREAD_W As Byte = &H40
    Private Const EJREQ_PRACCREAD_H As Byte = &H41
    Private Const EJREQ_PRACCREAD_BULK As Byte = &H42
    Private Const EJREQ_PRACCWRITE_W As Byte = &H43
    Private Const EJREQ_PRACCWRITE_H As Byte = &H44
    Private Const EJREQ_PRACCWRITE_B As Byte = &H45
    Private Const EJREQ_PRACCWRITE_BULK As Byte = &H46

    Private Const EJREQ_CAPABILITIES As Byte = &H50
    Private Const EJREQ_ENABLEVCC As Byte = &H51
    Private Const EJREQ_DISABLEVCC As Byte = &H52

    Private Const EJREQ_FLASHSPI_BRCM As Byte = &H62
    Private Const EJREQ_FLASHSPI_ATH As Byte = &H63

    Private Const EJREQ_FLASHWRITE_I16 As Byte = &H66
    Private Const EJREQ_FLASHWRITE_A16 As Byte = &H67
    Private Const EJREQ_FLASHWRITE_SST As Byte = &H68
    Private Const EJREQ_FLASHWRITE_AMDNB As Byte = &H69

    Private Const EJREQ_SAMPLE As Byte = &H70
    Private Const EJREQ_PRELOAD As Byte = &H71
    Private Const EJREQ_EXTEST As Byte = &H72

    Private Const EJREQ_RDATA As Byte = &H90
    Private Const EJREQ_SETPARAM As Byte = &H91
    Private Const EJREQ_SHIFTIN As Byte = &H95
    Private Const EJREQ_SHIFTOUT As Byte = &H96
    Private Const EJREQ_SHIFTOUT32 As Byte = &H97
    Private Const EJREQ_SHIFTINREAD As Byte = &H98

    Private PRACC_DATA_REG As UInt32
    Private PRACC_ADDR_REG As UInt32

#End Region

#Region "USB Hardware Calls"

    Friend Sub LEDOn()
        If FCUSB Is Nothing Then Exit Sub
        Try
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EJREQ_LEDON, 0, 0, 0)
            FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
        Catch ex As Exception
        End Try
    End Sub

    Friend Sub LEDOff()
        If FCUSB Is Nothing Then Exit Sub
        Try
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EJREQ_LEDOFF, 0, 0, 0)
            FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
        Catch ex As Exception
        End Try
    End Sub

    Friend Sub LEDBlink()
        If FCUSB Is Nothing Then Exit Sub
        Try
            Dim BlinkRate As Integer = 200
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, EJREQ_LEDBLINK, CShort(BlinkRate Mod 256), 0, 0)
            FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
        Catch ex As Exception
        End Try
    End Sub

    Friend Function IsConnected()
        If FCUSB Is Nothing Then
            Dim fcusb_list As UsbRegDeviceList = UsbDevice.AllDevices.FindAll(LibusbDeviceFinder)
            If fcusb_list IsNot Nothing AndAlso fcusb_list.Count > 0 Then
                Dim InUse() As String = GetFlashcatInUse()
                For i = 0 To fcusb_list.Count - 1
                    Dim u As WinUsb.WinUsbRegistry = fcusb_list(i)
                    Dim o As String = u.DeviceProperties("DeviceID")
                    If MyUSBDeviceID = o Then
                        Return True
                    ElseIf MyUSBDeviceID = "" Then
                        'check other fcusb apps
                        If InUse Is Nothing Then 'No other instances, we can use this one
                            Return True
                        Else
                            Dim FoundAvailableFcusb As Boolean = True
                            For Each item In InUse
                                If item = o Then FoundAvailableFcusb = False : Exit For
                            Next
                            If FoundAvailableFcusb Then Return True 'This ID is not being used
                        End If
                    End If
                Next
            End If
            Return False
        Else
            Return FCUSB.UsbRegistryInfo.IsAlive
        End If
    End Function
    'Connects this interface to an available fcusb
    Friend Function Connect() As Boolean
        'Disconnect()
        Dim fcusb_list As UsbRegDeviceList = UsbDevice.AllDevices.FindAll(LibusbDeviceFinder)
        If fcusb_list IsNot Nothing AndAlso fcusb_list.Count > 0 Then
            Dim InUse() As String = GetFlashcatInUse()
            For i = 0 To fcusb_list.Count - 1
                Dim u As WinUsb.WinUsbRegistry = fcusb_list(i)
                Dim o As String = u.DeviceProperties("DeviceID")
                If MyUSBDeviceID = o Then
                    MyUSBDeviceID = o
                    FCUSB = u.Device
                    Return OpenUsbDevice()
                ElseIf MyUSBDeviceID = "" Then
                    If InUse Is Nothing Then 'No other instances, we can use this one
                        MyUSBDeviceID = o
                        FCUSB = u.Device
                        Return OpenUsbDevice()
                    Else
                        Dim FoundAvailableFcusb As Boolean = True
                        For Each item In InUse
                            If item = o Then FoundAvailableFcusb = False : Exit For
                        Next
                        If FoundAvailableFcusb Then
                            MyUSBDeviceID = o
                            FCUSB = u.Device
                            Return OpenUsbDevice()
                        End If
                    End If
                End If
            Next
        End If
        Return False 'No FCUSB found
    End Function

    Private Function OpenUsbDevice() As Boolean
        Try
            If FCUSB Is Nothing Then Return False
            FCUSB.Open()
            Dim wholeUsbDevice As IUsbDevice = TryCast(FCUSB, IUsbDevice)
            If wholeUsbDevice IsNot Nothing Then 'Libusb only
                wholeUsbDevice.SetConfiguration(1)
                wholeUsbDevice.ClaimInterface(0)
            End If
            Return EchoTest()
        Catch ex As Exception
        End Try
        Return False
    End Function

    'If we are already connected, this will disconnect
    Friend Function Disconnect()
        Try
            If FCUSB IsNot Nothing Then FCUSB.Close()
            MyUSBDeviceID = ""
            FCUSB = Nothing
        Catch ex As Exception
        End Try
        Return True
    End Function

    Private Function EchoTest() As Boolean

        Dim value As Short = &H1234
        Dim index As Short = &H5678
        Dim readback As Byte() = New Byte(9) {}
        Dim ret As Integer = 0
        Dim usbSetupPacket As New UsbSetupPacket(CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device), EJREQ_ECHO, value, index, 8)
        If FCUSB.ControlTransfer(usbSetupPacket, readback, 8, ret) Then
            If ret <> 8 Then Return False
            If (readback(3) = &H12) And (readback(2) = &H34) And (readback(5) = &H56) And (readback(4) = &H78) Then
                Return True
            End If
        End If
        MyUSBDeviceID = ""
        FCUSB = Nothing
        Return False
    End Function

#End Region

    Public Function GetFirmwareRev() As String
        Dim readback As Byte() = New Byte(3) {}
        Dim ret As Integer = 0
        Dim usbPacket2 As New UsbSetupPacket(CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device), EJREQ_FIRMWARE, 0, 0, 4)
        If Not fcusb.ControlTransfer(usbPacket2, readback, 4, ret) Then
            Return Nothing
        End If
        If ret <> 4 Then Return Nothing
        Dim b As Byte() = New Byte(4) {}
        b(0) = readback(0)
        b(1) = readback(1)
        b(2) = Asc(".")
        b(3) = readback(2)
        b(4) = readback(3)
        Dim fwstr As String = System.Text.ASCIIEncoding.ASCII.GetString(b)
        If fwstr.StartsWith("0") Then fwstr = Mid(fwstr, 2)
        Return fwstr
    End Function

    Public Sub ResetTAP()
        Dim ret As Integer = 0
        Dim dflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim usbPacket2 As New UsbSetupPacket(dflag, EJREQ_RESETTAP, 0, 0, 0)
        Dim res As Boolean = fcusb.ControlTransfer(usbPacket2, Nothing, 0, ret)
    End Sub

    Public Sub Shiftout(num As Byte, tdi As UInt32, tms As UInt32)
        Dim tdiarray() As Byte = BitConverter.GetBytes(tdi)
        Dim tmsarray() As Byte = BitConverter.GetBytes(tms)
        Dim TotalPacket(11) As Byte
        TotalPacket(0) = num
        TotalPacket(4) = tdiarray(0)
        TotalPacket(5) = tdiarray(1)
        TotalPacket(6) = tdiarray(2)
        TotalPacket(7) = tdiarray(3)
        TotalPacket(8) = tmsarray(0)
        TotalPacket(9) = tmsarray(1)
        TotalPacket(10) = tmsarray(2)
        TotalPacket(11) = tmsarray(3)
        Dim dflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim setup As New UsbSetupPacket(dflag, EJREQ_SHIFTOUT, 0, 0, CShort(TotalPacket.Length))
        Dim Ret As Integer = 0
        Dim res As Boolean = fcusb.ControlTransfer(setup, TotalPacket, TotalPacket.Length, Ret)
    End Sub

    Public Sub Shiftout32(ByVal tdi As UInt32)
        Dim TotalPacket() As Byte = BitConverter.GetBytes(tdi)
        Dim dflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim setup As New UsbSetupPacket(dflag, EJREQ_SHIFTOUT32, 0, 0, CShort(TotalPacket.Length))
        Dim Ret As Integer = 0
        Dim res As Boolean = fcusb.ControlTransfer(setup, TotalPacket, TotalPacket.Length, Ret)
    End Sub

    Public Function ShiftIn(ByVal bit_count As UInt16, ByVal tdi32 As UInt32, ByVal tms32 As UInt32) As UInt32
        Dim tdi() As Byte = BitConverter.GetBytes(tdi32)
        Dim tms() As Byte = BitConverter.GetBytes(tms32)
        Dim tdo() As Byte = ShiftBulk(bit_count, tdi, tms)
        Return BitConverter.ToUInt32(tdo, 0)
    End Function


    'Byte from tdi(0) is shifted in first (LSB) then tdi(1) etc.
    Public Function ShiftBulk(ByVal bit_count As UInt16, ByVal tdi() As Byte, ByVal tms() As Byte) As Byte()
        Dim tdobytecollector As New ArrayList
        Dim flag1 As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim flag2 As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim Ret As Integer = 0
        Dim pointer As Integer = 0
        Dim BytesLeft As Integer = tdi.Length
        Dim BitsLeft As UInt16 = bit_count
        ReDim Preserve tms(tdi.Length - 1)
        Do Until BytesLeft = 0
            Dim packet_size As UShort = BytesLeft
            Dim packet_bits As UShort = BitsLeft
            If (packet_size > 32) Then
                packet_size = 32
                packet_bits = 256
            End If
            Dim data_out((packet_size * 2) - 1) As Byte
            Array.Copy(tdi, pointer, data_out, 0, packet_size)
            Array.Copy(tms, pointer, data_out, packet_size, packet_size)
            Dim usb_setup As New UsbSetupPacket(flag1, EJREQ_SHIFTIN, packet_bits, packet_size, CShort(data_out.Length))
            Dim res As Boolean = FCUSB.ControlTransfer(usb_setup, data_out, data_out.Length, Ret)
            Dim tdo(packet_size - 1) As Byte
            Dim reader As UsbEndpointReader = FCUSB.OpenEndpointReader(ReadEndpointID.Ep01, packet_size, EndpointType.Bulk)
            Dim ec As ErrorCode = reader.Read(tdo, 0, tdo.Length, 50000, Ret)
            If Not ec = ErrorCode.None Then Return Nothing
            tdobytecollector.Add(tdo)
            pointer += packet_size
            BytesLeft -= packet_size
            BitsLeft -= packet_bits
        Loop
        Dim total_tdo(tdi.Length - 1) As Byte
        Dim b() As Byte
        pointer = 0
        For Each b In tdobytecollector
            Array.Copy(b, 0, total_tdo, pointer, b.Length)
            pointer += b.Length
        Next
        Return total_tdo
    End Function
    'Causes the device to reset and enter DFU mode (bootloader)
    Public Sub StartBootloader()
        Dim ret As Integer = 0
        Dim dflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim usbPacket2 As New UsbSetupPacket(dflag, EJREQ_BOOTLOADER, 0, 0, 0)
        Dim res As Boolean = fcusb.ControlTransfer(usbPacket2, Nothing, 0, ret)
    End Sub

    Private Sub SetIR(ByVal IR As UInteger)
        Dim ret As Integer = 0
        Dim dflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim usbPacket2 As New UsbSetupPacket(dflag, EJREQ_SETIR, CUShort(IR), CUShort(0), CShort(0))
        Dim res As Boolean = fcusb.ControlTransfer(usbPacket2, Nothing, 0, ret)
    End Sub

    Private Function SetIRLen(ByVal IRLen As UInteger) As Boolean
        IR_LEN = IRLen
        Dim ret As Integer = 0
        Dim dflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim usbPacket2 As New UsbSetupPacket(dflag, EJREQ_SETIRLEN, CShort(IRLen), 0, 4)
        Dim shift_val As UInt32 = (1UI << IRLen)
        Dim irdata() As Byte = BitConverter.GetBytes(shift_val)
        Dim Success As Boolean = FCUSB.ControlTransfer(usbPacket2, irdata, 4, ret)
        Return Success
    End Function

    Private Function ReadWriteDR(ByVal d As UInteger) As UInteger
        Dim readback As Byte() = New Byte(3) {}
        Dim ret As Integer = 0
        Dim toppart As UInteger = CUInt(&HFFFF And d)
        Dim param1 As UShort = CUShort(toppart)
        toppart = CUInt((d >> 16) And &HFFFF)
        Dim param2 As UShort = CUShort(toppart)
        Dim dd As Byte() = New Byte(3) {}
        Dim dflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim usbPacket2 As New UsbSetupPacket(dflag, EJREQ_RWDATA, param1, param2, CShort(4))
        Dim res As Boolean = fcusb.ControlTransfer(usbPacket2, dd, 4, ret)
        d = BitConverter.ToUInt32(dd, 0) 'Returns 3FFF FFFF
        Return d
    End Function

    Private Function dmaWriteFlash_writeblock(ByVal FlashAddr As UInt32, ByVal data() As Byte, ByVal sub_cmd As Byte) As Boolean
        Dim DataCount As Integer = data.Length
        If DataCount > FCUSB2_MAX_USB_BUFFER_SIZE Then Return False
        Dim SizeArray() As Byte = {CByte(DataCount And &HFF), CByte((DataCount And &HFF00) >> 8)}
        Dim lowaddr As UShort = CUShort(&HFFFF And FlashAddr)
        Dim highaddr As UShort = CUShort(FlashAddr >> 16)
        Dim rtype As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim setuppacket As New UsbSetupPacket(rtype, sub_cmd, lowaddr, highaddr, CShort(2))
        Dim ret As Integer = 0
        Dim Result As Boolean = fcusb.ControlTransfer(setuppacket, SizeArray, 2, ret)
        If Not Result Then Return False
        Dim writer As UsbEndpointWriter = fcusb.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
        Dim ByteReturn As Integer = 0
        Dim ec As ErrorCode = writer.Write(data, 5000, ByteReturn)
        If Not ec = ErrorCode.None Or Not ByteReturn = data.Length Then Return False
        Return True
    End Function

    Private Function dmaRead(ByVal addr As UInteger, ByVal type As Integer) As UInteger
        Try
            Dim dread As UInteger = 0
            Dim ReadBack As Byte() = New Byte(3) {}
            Dim ret As Integer = 0
            Dim cmd As Byte = 0
            If type = 0 Then cmd = EJREQ_DMAREAD_W
            If type = 1 Then cmd = EJREQ_DMAREAD_H
            If type = 2 Then cmd = EJREQ_DMAREAD_B
            Dim lowaddr As UInt16 = CUShort(&HFFFF And addr)
            Dim highaddr As UInt16 = CUShort((addr >> 16))
            Dim req As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(req, cmd, lowaddr, highaddr, CShort(4))
            Dim res As Boolean = FCUSB.ControlTransfer(usbPacket2, ReadBack, 4, ret)
            Array.Reverse(ReadBack)
            Select Case type
                Case 0 'word (32-bit)
                    Return Utilities.Bytes.ToUInteger(ReadBack)
                Case 1 'halfword (16-bits)
                    Return (CUInt(ReadBack(0)) << 8) + ReadBack(1)
                Case 2 'byte (8-bits)
                    Return ReadBack(0)
                Case Else
                    Return 0
            End Select
        Catch ex As Exception
        End Try
        Return 0
    End Function

    Private Function dmaRead_bulk(ByVal Address As UInteger, ByVal count As Integer) As Byte()
        If count < 1 Or count > FCUSB2_MAX_USB_BUFFER_SIZE Then Return Nothing
        Dim dataout() As Byte
        Monitor.Enter(ClientLock)
        Try
            Dim ret As Integer
            Dim SizeArray() As Byte = {CByte(count And &HFF), CByte((count And &HFF00) >> 8)}
            Dim lowaddr As UShort = CUShort(&HFFFF And Address)
            Dim highaddr As UShort = CUShort(Address >> 16)
            Dim cmd As Byte = EJREQ_DMAREADBULK
            Dim rtype As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim setuppacket As New UsbSetupPacket(rtype, EJREQ_DMAREADBULK, lowaddr, highaddr, CShort(2))
            Dim res As Boolean = fcusb.ControlTransfer(setuppacket, SizeArray, 2, ret)
            If Not res Then Return Nothing
            Dim reader As UsbEndpointReader = fcusb.OpenEndpointReader(ReadEndpointID.Ep01, count, EndpointType.Bulk)
            ReDim dataout(count - 1)
            Dim ec As ErrorCode = reader.Read(dataout, 0, count, 5000, ret)
            If Not ec = ErrorCode.None Or Not ret = count Then dataout = Nothing
        Finally
            Monitor.Exit(ClientLock)
        End Try
        Return dataout
    End Function

    Private Sub dmaWrite(ByVal addr As UInteger, ByVal d As UInteger, ByVal type As Integer)
        Try
            Dim ret As Integer = 0
            Dim cmd As Byte = 0
            Dim dd As Byte() = New Byte(3) {}
            If type = 0 Then cmd = EJREQ_DMAWRITE_W
            If type = 1 Then cmd = EJREQ_DMAWRITE_H
            If type = 2 Then cmd = EJREQ_DMAWRITE_B
            Dim lowaddr As UInt16 = CUShort(&HFFFF And addr)
            Dim highaddr As UInt16 = CUShort((addr >> 16))
            dd = BitConverter.GetBytes(d)
            Dim control As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim setup As New UsbSetupPacket(control, cmd, lowaddr, highaddr, CShort(4))
            Dim res As Boolean = FCUSB.ControlTransfer(setup, dd, 4, ret)
        Catch ex As Exception
        End Try
    End Sub

    Private Function dmaWrite_bulk(ByVal Address As UInteger, ByVal Data() As Byte) As Integer
        If fcusb Is Nothing Then Return Nothing
        Dim isopen As Boolean = fcusb.IsOpen
        Dim count As Integer = Data.Length
        If count < 1 Or count > FCUSB2_MAX_USB_BUFFER_SIZE Then Return Nothing
        Dim BytesWritten As Integer = 0
        Monitor.Enter(ClientLock)
        Try
            Dim ret As Integer
            Dim SizeArray() As Byte = {CByte(count And &HFF), CByte((count And &HFF00) >> 8)}
            Dim lowaddr As UShort = CUShort(&HFFFF And Address)
            Dim highaddr As UShort = CUShort(Address >> 16)
            Dim rtype As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim setuppacket As New UsbSetupPacket(rtype, EJREQ_DMAWRITEBULK, lowaddr, highaddr, CShort(2))
            Dim res As Boolean = fcusb.ControlTransfer(setuppacket, SizeArray, 2, ret)
            If Not res Then Return -1
            Dim writer As UsbEndpointWriter = fcusb.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
            Dim ec As ErrorCode = writer.Write(Data, 5000, BytesWritten)
            If Not ec = ErrorCode.None Or Not BytesWritten = count Then BytesWritten = -1
        Finally
            Monitor.Exit(ClientLock)
        End Try
        Return BytesWritten
    End Function

    Private Sub spiWrite(ByVal Offset As UInteger, ByVal Data() As Byte, ByVal JtagDev As TargetType)
        If fcusb Is Nothing Then Exit Sub
        Dim isopen As Boolean = fcusb.IsOpen
        Dim count As Integer = Data.Length
        If count < 1 Or count > FCUSB2_MAX_USB_BUFFER_SIZE Then Exit Sub
        Dim BytesWritten As Integer = 0
        Dim OpCode As Byte
        Select Case JtagDev
            Case TargetType.Broadcom
                OpCode = EJREQ_FLASHSPI_BRCM
            Case TargetType.Atheros
                OpCode = EJREQ_FLASHSPI_ATH
            Case Else
                Exit Sub
        End Select
        Monitor.Enter(ClientLock)
        Try
            Dim ret As Integer
            Dim SizeArray() As Byte = {CByte(count And &HFF), CByte((count And &HFF00) >> 8)}
            Dim lowaddr As UShort = CUShort(&HFFFF And Offset)
            Dim highaddr As UShort = CUShort(Offset >> 16)
            Dim rtype As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim setuppacket As New UsbSetupPacket(rtype, OpCode, lowaddr, highaddr, CShort(2))
            Dim res As Boolean = fcusb.ControlTransfer(setuppacket, SizeArray, 2, ret)
            If Not res Then Exit Sub
            Dim writer As UsbEndpointWriter = fcusb.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk)
            Dim ec As ErrorCode = writer.Write(Data, 5000, BytesWritten)
            If Not ec = ErrorCode.None Or Not BytesWritten = count Then BytesWritten = -1
        Finally
            Monitor.Exit(ClientLock)
        End Try
    End Sub

    Private Sub praccWrite(ByVal addr As UInteger, ByVal data As UInteger, ByVal mode As Integer)
        Dim cmd As Byte
        If mode = 0 Then cmd = EJREQ_PRACCWRITE_W
        If mode = 1 Then cmd = EJREQ_PRACCWRITE_H
        If mode = 2 Then cmd = EJREQ_PRACCWRITE_B
        Dim ret As Integer = 0
        Dim lowaddr As UInt16 = CUShort(&HFFFF And addr)
        Dim highaddr As UInt16 = CUShort((addr >> 16))
        Dim control As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim setup As New UsbSetupPacket(control, cmd, lowaddr, highaddr, CShort(4))
        Dim res As Boolean = fcusb.ControlTransfer(setup, BitConverter.GetBytes(data), 4, ret)
    End Sub

    Private Function praccRead(ByVal addr As UInteger, ByVal mode As Byte) As UInt32
        Dim cmd As Byte = 0
        If mode = 0 Then cmd = EJREQ_PRACCREAD_W
        If mode = 1 Then cmd = EJREQ_PRACCREAD_H
        Dim ReadBack As UInteger = 0
        Dim dd As Byte() = New Byte(3) {}
        Dim ret As Integer = 0
        Dim lowaddr As UInt16 = CUShort(&HFFFF And addr)
        Dim highaddr As UInt16 = CUShort((addr >> 16))
        Dim req As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim usbPacket2 As New UsbSetupPacket(req, cmd, lowaddr, highaddr, CShort(4))
        Dim res As Boolean = fcusb.ControlTransfer(usbPacket2, dd, 4, ret)
        ReadBack = BitConverter.ToUInt32(dd, 0)
        Return ReadBack
    End Function

    Private Sub setParam(ByVal paramSelector As UInteger, ByVal paramValue As UInteger)
        Dim dd As Byte() = New Byte(3) {}
        Dim ret As Integer = 0
        Dim lowaddr As Short = CShort(&HFFFF And paramSelector)
        Dim highaddr As Short = CShort(paramSelector >> 16)
        dd = BitConverter.GetBytes(paramValue)
        Dim usbPacket2 As New UsbSetupPacket(CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device), EJREQ_SETPARAM, lowaddr, highaddr, 4)
        fcusb.ControlTransfer(usbPacket2, dd, 4, ret)
    End Sub

    Public Function ejctrl(ByVal ctrl As UInteger) As UInteger
        SetIR(EJTAG_CONTROL_IR)
        Return ReadWriteDR(ctrl)
    End Function
    'scan out the version register 
    Private Function GetIDCODE() As UInt32
        SetIR(EJTAG_IDCODE)
        Dim retCode As UInt32 = ReadWriteDR(0)
        If (retCode = 0) Then 'Might need other register
            SetIR(LATTICE_IDCODE)
            retCode = ReadWriteDR(0)
            'SetIR(&H0F) = &H4064BFF1
            'SetIR(&H1616) = &H01809043
        End If
        Return retCode
    End Function
    'scan out the implementation register
    Private Function GetImpCode() As UInt32
        SetIR(EJTAG_IMPCODE)
        Dim retCode As UInt32 = ReadWriteDR(0)
        Return retCode 'Should be &H21404000 for TI
    End Function
    'New in 7.x - Loads data that identifies the features in the AVR code
    Private Sub LoadCapabilities()
        Dim capBytes As Byte() = New Byte(3) {}
        Dim ret As Integer = 0
        Dim req As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim usbPacket2 As New UsbSetupPacket(req, EJREQ_CAPABILITIES, 0, 0, CShort(4))
        Dim res As Boolean = fcusb.ControlTransfer(usbPacket2, capBytes, 4, ret)
        If Not res Then Exit Sub
        Dim result As UInt32 = BitConverter.ToUInt32(capBytes, 0)
        SUPPORT_DMA = (result And &H80000000)
        SUPPORT_PRACC = (result And &H40000000)
        SUPPORT_INTELFLASH = (result And &H20000000)
        SUPPORT_AMDFLASH = (result And &H10000000)
        SUPPORT_LEGACYFLASH = (result And &H8000000)
        SUPPORT_JTAGSPI = (result And &H4000000)
    End Sub

End Class
