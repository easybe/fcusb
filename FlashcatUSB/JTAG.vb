'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2018 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'ACKNOWLEDGEMENT: USB driver functionality provided by LibUsbDotNet (sourceforge.net/projects/libusbdotnet)
'USB AVR stack provided by LUFA (fourwalledcubicle.com/LUFA.php), Thanks for your help Dean!

Imports LibUsbDotNet.Main
Imports System.Threading
Imports FlashcatUSB.USB.HostClient

Public Class JTAG_IF
    Public FCUSB As FCUSB_DEVICE
    Public TargetDevice As New JTAG_DEVICE 'Loaded after Init() has been called and returns true
    Public Property IR_LEN As Integer 'Number of bits of the instruction register
    Public TAP As JTAG_STATE_CONTROLLER 'JTAG state machine
    Public WithEvents JSP As New JTAG.SVF_Player(Me) 'SVF / XSVF Parser and player

    Sub New(ByVal parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
        TAP = Nothing
    End Sub
    'Connects to the target device

    Public Function Init() As Boolean
        Try
            TargetDevice = New JTAG_DEVICE 'Re-init
            Dim ChipIrLen As Integer = TAP_Detect()
            If ChipIrLen = 0 Then Return False
            If Not SetIRLen(ChipIrLen) Then Return False 'Makes us select this chip for communication
            WriteConsole(String.Format("JTAG: IR length set to {0}", ChipIrLen.ToString))
            TAP = New JTAG_STATE_CONTROLLER
            AddHandler TAP.ShiftBits, AddressOf Tap_ShiftBits
            TAP.Reset()
            Dim IMPCODE As UInt32 = ReadWriteData(EJTAG_OPCODE.IMPCODE)
            Dim id_readback As UInt32 = ReadWriteData(1)
            If (id_readback <> 0) AndAlso (id_readback <> &HFFFFFFFFUI) Then 'IDCODE used by most devices
                TargetDevice.LoadDevice(id_readback, ChipIrLen, IMPCODE)
            Else
                id_readback = ReadWriteData(6) 'IDCODE used by Altera
                If (id_readback <> 0) AndAlso (id_readback <> &HFFFFFFFFUI) Then TargetDevice.LoadDevice(id_readback, ChipIrLen, IMPCODE)
            End If
            If (TargetDevice.CONTROLLER = JTAG_CONTROLLER.Broadcom) Then
                EJTAG_LoadCapabilities(IMPCODE) 'Only supported by MIPS/EJTAG devices
            End If
            If Me.DMA_SUPPORTED Then
                Dim r As UInteger = DMA_ReadData(&HFF300000UI, DataWidth.Word) 'Returns 2000001E 
                r = r And &HFFFFFFFBUI '2000001A
                DMA_WriteData(&HFF300000UI, r, DataWidth.Word)
            End If
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Sub HandleJtagPrintRequest(ByVal msg As String) Handles JSP.Printf
        WriteConsole("SVF Player: " & msg)
    End Sub

#Region "MIPS SPECIFIC"
    'MIPS SPECIFIC:
    Public IMPCODE As UInt32
    Public IMPVER As String 'converted to text readable
    Public RK4_ENV As Boolean 'Indicates host is a R4000
    Public RK3_ENV As Boolean 'Indicates host is a R3000
    Public DINT_SUPPORT As Boolean 'Probe can use DINT signal to debug int on this cpu
    Public ASID_SIZE As Short '0=no ASIS,1=6bit,2=8bit
    Public MIPS16e As Boolean 'Indicates MIPS16e ASE support
    Public DMA_SUPPORTED As Boolean 'Indicates no DMA support and must use PrAcc mode
    Public MIPS32 As Boolean
    Public MIPS64 As Boolean

    Public Enum EJTAG_CTRL As UInt32
        '/* EJTAG 3.1 Control Register Bits */
        VPED = (1 <<23)       '/* R    */
        '/* EJTAG 2.6 Control Register Bits */
                           Rocc = (1UI << 31)     '/* R/W0 */
        Psz1 = (1 << 30)       '/* R    */
        Psz0 = (1 << 29)       '/* R    */
        Doze = (1 << 22)       '/* R    */
        ProbTrap = (1 << 14)   '/* R/W  */
        DebugMode = (1 << 3)   '/* R    */
        '/* EJTAG 1.5.3 Control Register Bits */
        Dnm = (1 << 28)        '/* */
        Sync = (1 << 23)       '/* R/W  */
        Run = (1 << 21)        '/* R    */
        PerRst = (1 << 20)     '/* R/W  */
        PRnW = (1 << 19)       '/* R    0 = Read, 1 = Write */
        PrAcc = (1 << 18)      '/* R/W0 */
        DmaAcc = (1 << 17)     '/* R/W  */
        PrRst = (1 << 16)      '/* R/W  */
        ProbEn = (1 << 15)     '/* R/W  */
        SetDev = (1 << 14)     '/* R    */
        JtagBrk = (1 << 12)    '/* R/W1 */
        DStrt = (1 << 11)      '/* R/W1 */
        DeRR = (1 << 10)       '/* R    */
        DrWn = (1 << 9)        '/* R/W  */
        Dsz1 = (1 << 8)        '/* R/W  */
        Dsz0 = (1 << 7)        '/* R/W  */
        DLock = (1 << 5)       '/* R/W  */
        BrkSt = (1 << 3)       '/* R    */
        TIF = (1 << 2)         '/* W0/R */
        TOF = (1 << 1)         '/* W0/R */
        ClkEn = (1 << 0)       '/* R/W  */
    End Enum
    'Loads specific features this device using EJTAG IMP OPCODE
    Private Sub EJTAG_LoadCapabilities(ByVal features As UInt32)
        Dim e_ver As UInt32 = ((features >> 29) And 7)
        Dim e_nodma As UInt32 = (features And (1 << 14))
        Dim e_priv As UInt32 = (features And (1 << 28))
        Dim e_dint As UInt32 = (features And (1 << 24))
        Dim e_mips As UInt32 = (features And 1)
        Select Case e_ver
            Case 0
                IMPVER = "1 and 2.0"
            Case 1
                IMPVER = "2.5"
            Case 2
                IMPVER = "2.6"
            Case 3
                IMPVER = "3.1"
        End Select
        If (e_nodma = 0) Then
            DMA_SUPPORTED = True
        Else
            DMA_SUPPORTED = False
        End If
        If (e_priv = 0) Then
            RK3_ENV = False
            RK4_ENV = True
        Else
            RK3_ENV = True
            RK4_ENV = False
        End If
        If (e_dint = 0) Then
            DINT_SUPPORT = False
        Else
            DINT_SUPPORT = True
        End If
        If (e_mips = 0) Then
            MIPS32 = True
            MIPS64 = False
        Else
            MIPS32 = False
            MIPS64 = True
        End If
    End Sub
    'Resets the processor (EJTAG ONLY)
    Public Sub EJTAG_Reset()
        ReadWriteData(EJTAG_OPCODE.CONTROL_IR, (EJTAG_CTRL.PrRst Or EJTAG_CTRL.PerRst))
    End Sub

    Public Function EJTAG_Debug_Enable() As Boolean
        Try
            Dim debug_flag As UInt32 = EJTAG_CTRL.PrAcc Or EJTAG_CTRL.ProbEn Or EJTAG_CTRL.SetDev Or EJTAG_CTRL.JtagBrk
            Dim ctrl_reg As UInt32 = ReadWriteData(EJTAG_OPCODE.CONTROL_IR, debug_flag)
            If (ReadWriteData(EJTAG_OPCODE.CONTROL_IR, EJTAG_CTRL.PrAcc Or EJTAG_CTRL.ProbEn Or EJTAG_CTRL.SetDev) And EJTAG_CTRL.BrkSt) Then
                Return True
            Else
                Return False
            End If
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Sub EJTAG_Debug_Disable()
        Try
            Dim flag As UInt32 = (EJTAG_CTRL.ProbEn Or EJTAG_CTRL.SetDev) 'This clears the JTAGBRK bit
            ReadWriteData(EJTAG_OPCODE.CONTROL_IR, flag)
        Catch ex As Exception
        End Try
    End Sub

#End Region

#Region "CFI over JTAG"
    Private CFI_IF As New CFI_FLASH_INTERFACE(Me) 'Handles all of the CFI flash protocol

    Public Enum cfi_mode As Byte
        Intel_16 = USB.USBREQ.JTAG_FLASHWRITE_I16
        AMD_16 = USB.USBREQ.JTAG_FLASHWRITE_A16
        SST = USB.USBREQ.JTAG_FLASHWRITE_SST
        NoBypass = USB.USBREQ.JTAG_FLASHWRITE_AMDNB
    End Enum

    Public Function CFI_Detect(ByVal DMA_ADDR As UInt32) As Boolean
        Return CFI_IF.DetectFlash(DMA_ADDR)
    End Function

    Public Sub CFI_ReadMode()
        CFI_IF.Read_Mode()
    End Sub

    Public Sub CFI_EraseDevice()
        CFI_IF.EraseBulk()
    End Sub

    Public Sub CFI_WaitUntilReady()
        CFI_IF.WaitUntilReady()
    End Sub

    Public Function CFI_Sector_Count() As UInt32
        Return CFI_IF.GetFlashSectors
    End Function

    Public Function CFI_GetSectorSize(ByVal sector_ind As UInt32) As UInt32
        Return CFI_IF.GetSectorSize(sector_ind)
    End Function

    Public Function CFI_FindSectorBase(ByVal sector_ind As UInt32) As UInt32
        Return CFI_IF.FindSectorBase(sector_ind)
    End Function

    Public Function CFI_Sector_Erase(ByVal sector_ind As UInt32) As Boolean
        CFI_IF.Sector_Erase(sector_ind)
        Return True
    End Function

    Public Function CFI_ReadFlash(ByVal Address As UInt32, ByVal count As UInt32) As Byte()
        Return CFI_IF.ReadData(Address, count)
    End Function

    Public Function CFI_Sector_Write(ByVal Address As UInt32, ByVal data_out() As Byte) As Boolean
        CFI_IF.WriteSector(Address, data_out)
        Return True
    End Function

    Public Function CFI_GetFlashName() As String
        Return CFI_IF.FlashName
    End Function

    Public Function CFI_GetFlashSize() As UInt32
        Return CFI_IF.FlashSize
    End Function

    Public Sub CFI_GetFlashPart(ByRef MFG As Byte, ByRef CHIPID As UInt32)
        MFG = CFI_IF.MyDeviceID.MFG
        CHIPID = (CFI_IF.MyDeviceID.ID1 << 16) Or CFI_IF.MyDeviceID.ID2
    End Sub


#End Region

#Region "SPI over JTAG"
    Friend SPI_Part As FlashMemory.SPI_NOR_FLASH 'Contains the SPI Flash definition
    Friend SPI_JTAG_IF As SPI_API
    'Returns TRUE if the JTAG can detect a connected flash to the SPI port
    Public Function SPI_Detect() As Boolean
        If (Not TargetDevice.SPI_SUPPORTED) Then
            WriteConsole("JTAG: target device does not support SPI direct-access")
            Return False
        End If
        Dim reg As UInt32 = 0
        SPI_JTAG_IF = New SPI_API(TargetDevice.CONTROLLER)
        reg = SPI_SendCommand(SPI_JTAG_IF.READ_ID, 1, 3)
        WriteConsole(String.Format("JTAG: SPI register returned {0}", "0x" & Hex(reg)))
        If reg = 0 OrElse reg = &HFFFFFFFFUI Then
            Return False
        Else
            Dim MFG_BYTE As Byte = CByte((reg And &HFF0000) >> 16)
            Dim PART_ID As UInt16 = CUShort(reg And &HFFFF)
            SPI_Part = FlashDatabase.FindDevice(MFG_BYTE, PART_ID, 0, FlashMemory.MemoryType.SERIAL_NOR)
            If SPI_Part IsNot Nothing Then
                WriteConsole(String.Format("JTAG: SPI flash detected ({0})", SPI_Part.NAME))
                If TargetDevice.CONTROLLER = JTAG_CONTROLLER.Broadcom Then
                    Memory_Write_W(SPI_JTAG_IF.REG_CNTR, 0)
                    Dim data() As Byte = SPI_ReadFlash(0, 4) 'Why?
                End If
                Return True
            Else
                WriteConsole("JTAG: SPI flash not found in database")
            End If
        End If
        Return False
    End Function
    'Includes chip-specific API for connecting JTAG->SPI
    Public Structure SPI_API
        Public Property BASE As UInt32
        'Register addrs
        Public Property REG_CNTR As UInt32
        Public Property REG_DATA As UInt32
        Public Property REG_OPCODE As UInt32
        'Control CODES
        Public Property CTL_Start As UInt32
        Public Property CTL_Busy As UInt32
        'OP CODES
        Public Property READ_ID As UShort '16 bits
        Public Property WREN As UShort
        Public Property SECTORERASE As UShort
        Public Property RD_STATUS As UShort
        Public Property PAGEPRG As UShort

        Sub New(ByVal jtag_dev As JTAG_CONTROLLER)
            Me.BASE = &H1FC00000UI
            Select Case jtag_dev
                Case JTAG_CONTROLLER.Broadcom
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
                Case JTAG_CONTROLLER.Atheros
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

    End Structure

    Public Function SPI_EraseBulk() As Boolean
        Try
            WriteConsole("Erasing entire SPI flash (this could take a moment)")
            Memory_Write_W(SPI_JTAG_IF.REG_CNTR, 0) 'Might need to be for Ath too
            SPI_SendCommand(SPI_JTAG_IF.WREN, 1, 0)
            Memory_Write_W(SPI_JTAG_IF.REG_OPCODE, SPI_JTAG_IF.BASE)
            Memory_Write_W(SPI_JTAG_IF.REG_CNTR, &H800000C7UI) 'Might need to be for Ath too
            SPI_WaitUntilReady()
            WriteConsole("Erase operation complete!")
            Return True
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Function SPI_SectorErase(ByVal secotr_ind As UInt32) As Boolean
        Try
            Dim Addr24 As UInteger = SPI_FindSectorBase(secotr_ind)
            Dim reg As UInt32 = SPI_GetControlReg()
            If TargetDevice.CONTROLLER = JTAG_CONTROLLER.Broadcom Then
                Memory_Write_W(SPI_JTAG_IF.REG_CNTR, 0)
                SPI_SendCommand(SPI_JTAG_IF.WREN, 1, 0)
                Memory_Write_W(SPI_JTAG_IF.REG_OPCODE, Addr24 Or SPI_JTAG_IF.SECTORERASE)
                reg = (reg And &HFFFFFF00) Or SPI_JTAG_IF.SECTORERASE Or SPI_JTAG_IF.CTL_Start
                Memory_Write_W(SPI_JTAG_IF.REG_CNTR, reg)
                Memory_Write_W(SPI_JTAG_IF.REG_CNTR, 0)
            ElseIf TargetDevice.CONTROLLER = JTAG_CONTROLLER.Atheros Then
                SPI_SendCommand(SPI_JTAG_IF.WREN, 1, 0)
                Memory_Write_W(SPI_JTAG_IF.REG_OPCODE, (Addr24 << 8) Or SPI_JTAG_IF.SECTORERASE)
                reg = (reg And &HFFFFFF00) Or &H4 Or SPI_JTAG_IF.CTL_Start
                Memory_Write_W(SPI_JTAG_IF.REG_CNTR, reg)
            End If
            SPI_WaitUntilReady()
            SPI_GetControlReg()
        Catch ex As Exception
        End Try
        Return True
    End Function

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
                If BytesRemaining > MAX_USB_BUFFER_SIZE Then
                    ReDim BlockToWrite(MAX_USB_BUFFER_SIZE - 1)
                Else
                    ReDim BlockToWrite(BytesRemaining - 1)
                End If
                Array.Copy(DataToWrite, BytesWritten, BlockToWrite, 0, BlockToWrite.Length)
                SPI_WriteFlash(Address, BlockToWrite)
                BytesWritten += BlockToWrite.Length
                BytesRemaining -= BlockToWrite.Length
                Address += CUInt(BlockToWrite.Length)
            End While
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function SPI_SendCommand(ByVal SPI_OPCODE As UInt16, ByVal BytesToWrite As UInt32, ByVal BytesToRead As UInt32) As UInt32
        Memory_Write_W(&HBF000000UI, 0)
        If TargetDevice.CONTROLLER = JTAG_CONTROLLER.Broadcom Then
            Memory_Write_W(SPI_JTAG_IF.REG_CNTR, 0)
        End If
        Dim reg As UInt32 = SPI_GetControlReg() 'Zero
        Select Case TargetDevice.CONTROLLER
            Case JTAG_CONTROLLER.Broadcom
                reg = (reg And &HFFFFFF00UI) Or SPI_OPCODE Or SPI_JTAG_IF.CTL_Start
            Case JTAG_CONTROLLER.Atheros
                reg = (reg And &HFFFFFF00UI) Or BytesToWrite Or (BytesToRead << 4) Or SPI_JTAG_IF.CTL_Start
        End Select
        Memory_Write_W(SPI_JTAG_IF.REG_OPCODE, SPI_OPCODE)
        Memory_Write_W(SPI_JTAG_IF.REG_CNTR, reg)
        reg = SPI_GetControlReg()
        If (BytesToRead > 0) Then
            reg = Memory_Read_W(SPI_JTAG_IF.REG_DATA)
            Select Case BytesToRead
                Case 1
                    reg = (reg And 255)
                Case 2
                    reg = ((reg And 255) << 8) Or ((reg And &HFF00) >> 8)
                Case 3
                    reg = ((reg And 255) << 16) Or (reg And &HFF00) Or ((reg And &HFF0000) >> 16)
                Case 4
                    reg = ((reg And 255) << 24) Or ((reg And &HFF00) << 8) Or ((reg And &HFF0000) >> 8) Or ((reg And &HFF000000) >> 24)
            End Select
            Return reg
        End If
        Return 0
    End Function
    'Returns the total number of sectors
    Public Function SPI_Sector_Count() As UInt32
        Dim secSize As UInt32 = &H10000 '64KB
        Dim totalsize As UInt32 = SPI_Part.FLASH_SIZE
        Return CUInt(totalsize / secSize)
    End Function

    Public Function SPI_WriteSector(ByVal sector_index As UInt32, ByVal data() As Byte) As Boolean
        Dim Addr As UInteger = SPI_FindSectorBase(sector_index)
        SPI_WriteData(Addr, data)
        Return True
    End Function

    Public Function SPI_FindSectorBase(ByVal sectorInt As Integer) As UInteger
        Return CUInt(SPI_GetSectorSize(0) * sectorInt)
    End Function

    Public Function SPI_GetSectorSize(ByVal sector_ind As UInt32) As UInteger
        Return &H10000
    End Function

    Public Function SPI_GetControlReg() As UInt32
        Dim i As Integer = 0
        Dim reg As UInt32
        Do
            If Not i = 0 Then Thread.Sleep(25)
            reg = Memory_Read_W(SPI_JTAG_IF.REG_CNTR)
            i = i + 1
            If i = 20 Then Return 0
        Loop While ((reg And SPI_JTAG_IF.CTL_Busy) > 0)
        Return reg
    End Function

    Public Sub SPI_WaitUntilReady()
        Dim reg As UInt32
        Do
            reg = SPI_SendCommand(SPI_JTAG_IF.RD_STATUS, 1, 4)
        Loop While ((reg And 1) > 0)
    End Sub

    Public Function SPI_ReadFlash(ByVal addr32 As UInt32, ByVal count As UInt32) As Byte()
        Return Memory_Read_Bulk(SPI_JTAG_IF.BASE + addr32, count)
    End Function

    Private Function SPI_WriteFlash(ByVal addr32 As UInt32, ByVal data_out() As Byte) As Boolean
        Dim OpCode As Byte
        Select Case TargetDevice.CONTROLLER
            Case JTAG_CONTROLLER.Broadcom
                OpCode = USB.USBREQ.JTAG_FLASHSPI_BRCM
            Case JTAG_CONTROLLER.Atheros
                OpCode = USB.USBREQ.JTAG_FLASHSPI_ATH
        End Select
        Dim setup_data(7) As Byte
        Dim data_len As UInt32 = data_out.Length
        setup_data(0) = CByte(addr32 And 255)
        setup_data(1) = CByte((addr32 >> 8) And 255)
        setup_data(2) = CByte((addr32 >> 16) And 255)
        setup_data(3) = CByte((addr32 >> 24) And 255)
        setup_data(4) = CByte(data_len And 255)
        setup_data(5) = CByte((data_len >> 8) And 255)
        setup_data(6) = CByte((data_len >> 16) And 255)
        setup_data(7) = CByte((data_len >> 24) And 255)
        If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data, setup_data.Length) Then Return Nothing
        If Not FCUSB.USB_CONTROL_MSG_OUT(OpCode) Then Return Nothing 'Sends setup command and data
        Return FCUSB.USB_BULK_OUT(data_out)
    End Function

#End Region

#Region "DMA"
    Private Const MAX_USB_BUFFER_SIZE As UShort = 2048 '4096 'Max number of bytes we should send via USB bulk endpoints

    'Target device needs to support DMA and FCUSB needs to include flashmode
    Public Sub DMA_WriteFlash(ByVal dma_addr As UInt32, ByVal data_to_write() As Byte, ByVal prog_mode As cfi_mode)
        Dim data_blank As Boolean = True
        For i = 0 To data_to_write.Length - 1
            If Not data_to_write(i) = 255 Then
                data_blank = False
                Exit For
            End If
        Next
        If data_blank Then Exit Sub 'Sector is already blank, no need to write data
        Dim BytesWritten As UInt32 = 0
        Try
            Dim BufferIndex As UInt32 = 0
            Dim BytesLeft As UInt32 = data_to_write.Length
            Dim Counter As UInt32 = 0
            Do Until BytesLeft = 0
                If BytesLeft > MAX_USB_BUFFER_SIZE Then
                    Dim Packet(MAX_USB_BUFFER_SIZE - 1) As Byte
                    Array.Copy(data_to_write, BufferIndex, Packet, 0, Packet.Length)
                    DMA_WriteFlash_Block(dma_addr, Packet, prog_mode)
                    dma_addr = dma_addr + MAX_USB_BUFFER_SIZE
                    BufferIndex = BufferIndex + MAX_USB_BUFFER_SIZE
                    BytesLeft = BytesLeft - MAX_USB_BUFFER_SIZE
                    BytesWritten += MAX_USB_BUFFER_SIZE
                Else
                    Dim Packet(BytesLeft - 1) As Byte
                    Array.Copy(data_to_write, BufferIndex, Packet, 0, Packet.Length)
                    DMA_WriteFlash_Block(dma_addr, Packet, prog_mode)
                    BytesLeft = 0
                End If
                Counter += 1
            Loop
        Catch
        End Try
    End Sub

    Private Function DMA_WriteFlash_Block(ByVal dma_addr As UInt32, ByVal data() As Byte, ByVal sub_cmd As cfi_mode) As Boolean
        Dim setup_data(7) As Byte
        Dim data_count As UInt32 = data.Length
        setup_data(0) = CByte(dma_addr And 255)
        setup_data(1) = CByte((dma_addr >> 8) And 255)
        setup_data(2) = CByte((dma_addr >> 16) And 255)
        setup_data(3) = CByte((dma_addr >> 24) And 255)
        setup_data(4) = CByte(data_count And 255)
        setup_data(5) = CByte((data_count >> 8) And 255)
        setup_data(6) = CByte((data_count >> 16) And 255)
        setup_data(7) = CByte((data_count >> 24) And 255)
        If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data, setup_data.Length) Then Return Nothing
        If Not FCUSB.USB_CONTROL_MSG_OUT(sub_cmd) Then Return Nothing
        Dim write_result As Boolean = FCUSB.USB_BULK_OUT(data)
        If write_result Then FCUSB.USB_WaitForComplete()
        Return write_result
    End Function

    Private Function DMA_ReadData(ByVal addr As UInteger, ByVal type As DataWidth) As UInteger
        Try
            Dim setup_data As UInt32 = ((addr And &HFFFF) << 16) Or ((addr >> 16) And &HFFFF)
            Dim ReadBack As Byte() = New Byte(3) {}
            Select Case type
                Case DataWidth.Word
                    FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_DMAREAD_W, ReadBack, setup_data)
                    Array.Reverse(ReadBack)
                    Return Utilities.Bytes.ToUInteger(ReadBack)
                Case DataWidth.HalfWord
                    FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_DMAREAD_H, ReadBack, setup_data)
                    Array.Reverse(ReadBack)
                    Return (CUInt(ReadBack(0)) << 8) + ReadBack(1)
                Case DataWidth.Byte
                    FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_DMAREAD_B, ReadBack, setup_data)
                    Array.Reverse(ReadBack)
                    Return CUInt(ReadBack(0))
            End Select
        Catch ex As Exception
        End Try
        Return 0
    End Function

    Private Function DMA_ReadBulk(ByVal dma_addr As UInt32, ByVal count As UInt32) As Byte()
        Dim setup_data(7) As Byte
        setup_data(0) = CByte(dma_addr And 255)
        setup_data(1) = CByte((dma_addr >> 8) And 255)
        setup_data(2) = CByte((dma_addr >> 16) And 255)
        setup_data(3) = CByte((dma_addr >> 24) And 255)
        setup_data(4) = CByte(count And 255)
        setup_data(5) = CByte((count >> 8) And 255)
        setup_data(6) = CByte((count >> 16) And 255)
        setup_data(7) = CByte((count >> 24) And 255)
        Dim data_back(count - 1) As Byte
        If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data, setup_data.Length) Then Return Nothing
        If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_DMAREADBULK) Then Return Nothing 'Sends setup command and data
        If Not FCUSB.USB_BULK_IN(data_back) Then Return Nothing
        Return data_back
    End Function

    Private Sub DMA_WriteData(ByVal dma_addr As UInt32, ByVal data As UInt32, ByVal type As DataWidth)
        Dim setup_data(7) As Byte
        setup_data(0) = CByte(dma_addr And 255)
        setup_data(1) = CByte((dma_addr >> 8) And 255)
        setup_data(2) = CByte((dma_addr >> 16) And 255)
        setup_data(3) = CByte((dma_addr >> 24) And 255)
        setup_data(4) = CByte(data And 255)
        setup_data(5) = CByte((data >> 8) And 255)
        setup_data(6) = CByte((data >> 16) And 255)
        setup_data(7) = CByte((data >> 24) And 255)
        If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data, setup_data.Length) Then Exit Sub
        Select Case type
            Case DataWidth.Word
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_DMAWRITE_W)
            Case DataWidth.HalfWord
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_DMAWRITE_H)
            Case DataWidth.Byte
                FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_DMAWRITE_B)
        End Select
    End Sub

    Private Function DMA_WriteBulk(ByVal dma_addr As UInteger, ByVal data_out() As Byte) As Boolean
        Dim setup_data(7) As Byte
        Dim data_len As UInt32 = data_out.Length
        setup_data(0) = CByte(dma_addr And 255)
        setup_data(1) = CByte((dma_addr >> 8) And 255)
        setup_data(2) = CByte((dma_addr >> 16) And 255)
        setup_data(3) = CByte((dma_addr >> 24) And 255)
        setup_data(4) = CByte(data_len And 255)
        setup_data(5) = CByte((data_len >> 8) And 255)
        setup_data(6) = CByte((data_len >> 16) And 255)
        setup_data(7) = CByte((data_len >> 24) And 255)
        If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data, setup_data.Length) Then Return Nothing
        If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_DMAWRITEBULK) Then Return Nothing 'Sends setup command and data
        Return FCUSB.USB_BULK_OUT(data_out)
    End Function

#End Region

#Region "Reading/Writing to Memory"
    Public Property IntelFlashDelay As UInt32 = 250 'Delay in clock cycles (16MHz total)
    Public Property AmdFlashDelay As UInt32 = 250 'Delay in clock cycles (16MHz total)
    Public Property DMA_Delay As UInt32 = 250

    'Sets parameters in device
    Public Sub SetParameter()
        Dim w_Data As UInt32 = (Me.IntelFlashDelay << 16) Or 1
        FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SETPARAM, Nothing, w_Data)
        w_Data = (Me.AmdFlashDelay << 16) + 2
        FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SETPARAM, Nothing, w_Data)
        w_Data = (Me.DMA_Delay << 16) + 3
        FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SETPARAM, Nothing, w_Data)
    End Sub

    Public Function ReadMemory(ByVal Address As UInteger, ByVal count As Integer) As Byte()
        Return Memory_Read_Bulk(Address, count)
    End Function

    Public Function WriteMemory(ByVal Address As UInt32, ByVal data() As Byte)
        Return Memory_Write_Bulk(Address, data)
    End Function

    Public Function Memory_Read_B(ByVal addr As UInt32) As Byte
        If TargetDevice.DMA_SUPPORTED Then
            If (addr Mod 2) = 0 Then
                Return CByte(DMA_ReadData(addr, DataWidth.Byte) And &HFF)
            Else
                Return CByte(DMA_ReadData(addr, DataWidth.HalfWord) And &HFF)
            End If
        End If
        Return 0
    End Function

    Public Function Memory_Read_H(ByVal addr As UInt32) As UInt16
        If TargetDevice.DMA_SUPPORTED Then
            Return CUShort(DMA_ReadData(addr, DataWidth.HalfWord) And &HFFFF)
        End If
        Return 0
    End Function

    Public Function Memory_Read_W(ByVal addr As UInt32) As UInteger
        If TargetDevice.DMA_SUPPORTED Then
            Return DMA_ReadData(addr, DataWidth.Word)
        End If
        Return 0
    End Function
    'Reads data from DRAM (optomized for speed)
    Public Function Memory_Read_Bulk(ByVal Address As UInt32, ByVal count As UInt32) As Byte()
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
            If BytesToRead > MAX_USB_BUFFER_SIZE Then BytesToRead = MAX_USB_BUFFER_SIZE
            Dim Offset As UInt32 = LargeCount - BytesLeft
            Dim TempBuffer() As Byte = Nothing
            If TargetDevice.DMA_SUPPORTED Then
                TempBuffer = DMA_ReadBulk(Address + Offset, BytesToRead)
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

    Public Sub Memory_Write_B(ByVal addr As UInteger, ByVal data As Byte)
        If TargetDevice.DMA_SUPPORTED Then
            Dim pattern As UInt32 = data
            pattern = (pattern << 24) Or (pattern << 16) Or (pattern << 8) Or data
            DMA_WriteData(addr, pattern, DataWidth.Byte)
        End If
    End Sub

    Public Sub Memory_Write_H(ByVal addr As UInteger, ByVal data As UInt16)
        If TargetDevice.DMA_SUPPORTED Then
            Dim pattern As UInt32 = (data << 16) Or data
            DMA_WriteData(addr, pattern, DataWidth.HalfWord)
        End If
    End Sub

    Public Sub Memory_Write_W(ByVal addr As UInteger, ByVal data As UInt32)
        If TargetDevice.DMA_SUPPORTED Then
            DMA_WriteData(addr, data, DataWidth.Word)
        End If
    End Sub
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
                If BytesRemaining > MAX_USB_BUFFER_SIZE Then
                    ReDim BlockToWrite(MAX_USB_BUFFER_SIZE - 1)
                Else
                    ReDim BlockToWrite(BytesRemaining - 1)
                End If
                Array.Copy(DataToWrite, BytesWritten, BlockToWrite, 0, BlockToWrite.Length)
                If TargetDevice.DMA_SUPPORTED Then
                    DMA_WriteBulk(Address, BlockToWrite)
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

#End Region

    Public Class JTAG_DEVICE
        Property IDCODE As UInt32 = 0
        Property PARTNU As UShort = 0
        Property MANUID As UShort = 0
        Property VERSION As Short = 0
        Property CONTROLLER As JTAG_CONTROLLER
        Property IR_SAMPLE As UInt32 = 0
        Property IR_BYPASS As UInt32 = 0
        Property IR_EXTTEST As UInt32 = 0
        Property DMA_SUPPORTED As Boolean = False
        Property SPI_SUPPORTED As Boolean = False 'Supports SPI-DMA

        Friend Sub LoadDevice(ByVal ID As UInt32, IR As Integer, IMPCODE As UInt32)
            Me.DMA_SUPPORTED = False
            Me.SPI_SUPPORTED = False
            Me.IDCODE = ID
            Me.VERSION = CShort((ID And &HF0000000L) >> 28)
            Me.PARTNU = CUShort((ID And &HFFFF000) >> 12)
            Me.MANUID = CUShort((ID And &HFFE) >> 1)
            If Me.MANUID = &HBF Then
                Me.CONTROLLER = JTAG_CONTROLLER.Broadcom
                Me.DMA_SUPPORTED = True
                Me.SPI_SUPPORTED = True
                Me.IR_SAMPLE = EJTAG_OPCODE.SAMPLE
                Me.IR_BYPASS = EJTAG_OPCODE.BYPASS
                Me.IR_EXTTEST = EJTAG_OPCODE.EXTEST
            ElseIf Me.MANUID = &H70 Then
                Me.CONTROLLER = JTAG_CONTROLLER.Atheros
                Me.DMA_SUPPORTED = True
                Me.SPI_SUPPORTED = True
                Me.IR_SAMPLE = EJTAG_OPCODE.SAMPLE
                Me.IR_BYPASS = EJTAG_OPCODE.BYPASS
                Me.IR_EXTTEST = EJTAG_OPCODE.EXTEST
            ElseIf Me.MANUID = &H6E Then
                Me.CONTROLLER = JTAG_CONTROLLER.Altera
                Me.IR_SAMPLE = ALTERA_OPCODE.SAMPLE
                Me.IR_BYPASS = ALTERA_OPCODE.BYPASS
                Me.IR_EXTTEST = ALTERA_OPCODE.EXTEST
            ElseIf Me.MANUID = &H49 Then
                Me.CONTROLLER = JTAG_CONTROLLER.Xilinx
                Me.IR_SAMPLE = XILINX_OPCODE.SAMPLE
                Me.IR_BYPASS = XILINX_OPCODE.BYPASS
                Me.IR_EXTTEST = XILINX_OPCODE.EXTEST
            ElseIf ID = 1 AndAlso IR = 5 AndAlso IMPCODE = &H60414000UI Then 'Atheros_AR9331
                Me.CONTROLLER = JTAG_CONTROLLER.Atheros
                Me.DMA_SUPPORTED = True
                Me.SPI_SUPPORTED = True
                Me.IR_SAMPLE = EJTAG_OPCODE.SAMPLE
                Me.IR_BYPASS = EJTAG_OPCODE.BYPASS
                Me.IR_EXTTEST = EJTAG_OPCODE.EXTEST
            Else
                Me.CONTROLLER = JTAG_CONTROLLER.Unknown
            End If
        End Sub

    End Class

    Public Enum JTAG_CONTROLLER
        Unknown
        Atheros
        Altera
        Broadcom
        TexasInstruments
        Xilinx
    End Enum

    Public Enum EJTAG_OPCODE As UInt32 'IR LEN 5
        EXTEST = &H0
        IDCODE = &H1 'Selects Device Identiﬁcation (ID) register
        SAMPLE = &H2 'Free for other use, such as JTAG boundary scan
        IMPCODE = &H3 'Selects Implementation register
        ADDRESS_IR = &H8 'Selects Address register
        DATA_IR = &H9 'Selects Data register
        CONTROL_IR = &HA 'Selects EJTAG Control register
        IR_ALL = &HB 'Selects the Address, Data and EJTAG Control registers
        EJTAGBOOT = &HC 'Makes the processor take a debug exception after rese
        NORMALBOOT = &HD 'Makes the processor execute the reset handler after rese
        FASTDATA = &HE 'Selects the Data and Fastdata registers
        EJWATCH = &H1C
        BYPASS = &H1F 'Select Bypass register
    End Enum

    Public Enum XILINX_OPCODE As UInt32 'IR LEN 8
        INTEST = &H2
        BYPASS = &HFF 'Typically all 1s for the size of the register
        SAMPLE = &H3
        EXTEST = &H0
        IDCODE = &H1
        USERCODE = &HFD
        HIGHZ = &HFC
        ISC_ENABLE_CLAMP = &HE9
        ISC_ENABLE_OTF = &HE4
        ISC_ENABLE = &HE8
        ISC_SRAM_READ = &HE7
        ISC_WRITE = &HE6
        ISC_ERASE = &HED
        ISC_PROGRAM = &HEA
        ISC_READ = &HEE
        ISC_INIT = &HF0
        ISC_DISABLE = &HC0
        TEST_ENABLE = &H11
        BULKPROG = &H12
        ERASE_ALL = &H14
        MVERIFY = &H13
        TEST_DISABLE = &H15
        ISC_NOOP = &HE0
    End Enum
    'IR instructions common on CPLD (such as MAX V) 
    Public Enum ALTERA_OPCODE As UInt32 'IR LEN 10
        EXTEST = &HF
        IDCODE = &H6
        SAMPLE = &H5
        BYPASS = &H3FF
        USERCODE = &H7
        HIGHZ = &HB
        CLAMP = &HA
        USER0 = &HC
        USER1 = &HE
    End Enum

    Private Enum DataWidth As Byte
        [Byte]
        HalfWord
        Word
    End Enum
    'Writes to the instruction-register and then shifts out 32-bits from the DR
    Public Function ReadWriteData(ByVal ir_reg As UInt32, Optional value_in As UInt32 = 0) As UInt32
        Dim data_in() As Byte = Utilities.Bytes.FromUInt32(value_in)
        Dim data_out() As Byte = Nothing
        Dim byte_count As Integer = Math.Ceiling(IR_LEN / 8)
        If (byte_count = 1) Then
            TAP.ShiftIR({(ir_reg And 255)}, Nothing, IR_LEN)
        ElseIf (byte_count = 2) Then
            TAP.ShiftIR({((ir_reg >> 8) And 255), (ir_reg And 255)}, Nothing, IR_LEN)
        End If
        TAP.ShiftDR(data_in, data_out, 32)
        TAP.GotoState(JTAG_STATE_CONTROLLER.MachineState.Select_DR) 'Default parking
        Dim value_out As UInt32 = Utilities.Bytes.ToUInteger(data_out)
        Return value_out
    End Function

    Public Sub ResetTAP()
        Try
            If TAP IsNot Nothing Then TAP.Reset()
        Catch ex As Exception
        End Try
    End Sub
    'Attempts to auto-detect a JTAG device on the TAP, returns the IR Length of the device
    Private Function TAP_Detect() As UInt32
        Dim chipiddata(3) As Byte
        If Not FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_DETECT, chipiddata) Then Return 0 'Error
        Dim dint As UInteger = BitConverter.ToUInt32(chipiddata, 0)
        If (Utilities.HWeight32(dint) <> 1) Then Return 0
        Dim ir As UInt32 = 0
        While (dint <> 0)
            dint >>= 1
            ir += 1
        End While
        Return ir
    End Function
    'Sets the IR len for embedded functions
    Public Function SetIRLen(ByVal IRLen As UInteger) As Boolean
        Try
            Me.IR_LEN = IRLen
            Dim setup_data As UInt32 = (IRLen << 16)
            Return FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SETIRLEN, Nothing, setup_data)
        Catch ex As Exception
            Return False
        End Try
    End Function
    'Byte from tdi(0) is shifted in first (LSB) then tdi(1) etc.
    Private Function ShiftData(ByVal bit_count As UInt32, ByVal tdi() As Byte, ByVal tms() As Byte) As Byte()
        Dim tdobytecollector As New ArrayList
        Dim flag1 As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim Ret As Integer = 0
        Dim pointer As Integer = 0
        Dim BytesLeft As Integer = tdi.Length
        Dim BitsLeft As UInt32 = bit_count
        ReDim Preserve tms(tdi.Length - 1)
        Do Until BytesLeft = 0
            Dim packet_size As UInt32 = BytesLeft
            Dim packet_bits As UInt32 = BitsLeft 'Each packet holds up to 32-bytes
            If (packet_size > 32) Then
                packet_size = 32
                packet_bits = 256
            End If
            Dim setup_data((packet_size * 2) - 1) As Byte
            Array.Copy(tdi, pointer, setup_data, 0, packet_size)
            Array.Copy(tms, pointer, setup_data, packet_size, packet_size)
            Dim tdo(packet_size - 1) As Byte
            Dim ctrl_data As UInt32 = (CUInt(packet_bits) << 16) 'Sets the bit_count
            ctrl_data = ctrl_data Or (packet_size And 255) 'We want to write this number of bytes
            If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, setup_data, setup_data.Length) Then Return Nothing 'Preloads our TDI/TMS data
            If Not FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_SHIFTDATA, tdo, ctrl_data) Then Return Nothing 'Shifts data and reads result
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

    Public Function ShiftData32(ByVal bit_count As UInt16, ByVal tdi32 As UInt32, ByVal tms32 As UInt32) As UInt32
        Try
            Dim tdi(3) As Byte
            Dim tms(3) As Byte
            tdi(0) = CByte(tdi32 And 255)
            tdi(1) = CByte((tdi32 >> 8) And 255)
            tdi(2) = CByte((tdi32 >> 16) And 255)
            tdi(3) = CByte((tdi32 >> 24) And 255)
            tms(0) = CByte(tms32 And 255)
            tms(1) = CByte((tms32 >> 8) And 255)
            tms(2) = CByte((tms32 >> 16) And 255)
            tms(3) = CByte((tms32 >> 24) And 255)
            Dim tdo() As Byte = ShiftData(bit_count, tdi, tms)
            Return Utilities.Bytes.ToUInteger(tdo)
        Catch ex As Exception
        End Try
        Return 0
    End Function

    Private Sub Tap_ShiftBits(ByVal BitCount As UInt32, ByVal tdi_bits() As Byte, ByVal tms_bits() As Byte, ByRef tdo_bits() As Byte)
        Try
            Array.Reverse(tdi_bits)
            Array.Reverse(tms_bits)
            tdo_bits = ShiftData(BitCount, tdi_bits, tms_bits)
            If tdo_bits IsNot Nothing Then Array.Reverse(tdo_bits)
        Catch ex As Exception
        End Try
    End Sub
    'Selects a ir-reg, then selects the data-reg and shifts data to and from it
    Public Function ScanRegister(ByVal IR As UInt32, ByVal dr_bit_count As UInt16, Optional ByVal preload() As Byte = Nothing) As Byte()
        Dim byte_count As UInt16 = Math.Ceiling(dr_bit_count / 8)
        Dim packet(5 + byte_count) As Byte 'This must be 64 bytes or less
        If preload IsNot Nothing Then
            Dim copy_size As Integer = preload.Length
            If copy_size > byte_count Then copy_size = byte_count
            Array.Copy(preload, 0, packet, 6, copy_size)
        End If
        packet(1) = CByte(byte_count And 255)
        packet(2) = CByte((byte_count << 8) And 255)
        packet(3) = CByte((byte_count << 16) And 255)
        packet(4) = CByte((byte_count << 24) And 255)
        packet(4) = CByte(dr_bit_count And 255)
        packet(5) = CByte((dr_bit_count << 8) And 255)
        If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.LOAD_PAYLOAD, packet, packet.Length) Then Return Nothing
        If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SCAN) Then Return Nothing 'This sets the IR and then scans the DR register
        If Not FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.READ_PAYLOAD, packet, packet.Length) Then Return Nothing
        Dim data_out(byte_count - 1) As Byte
        Array.Copy(packet, 6, data_out, 0, data_out.Length)
        Return data_out
    End Function


End Class
