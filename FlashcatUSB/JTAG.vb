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
    Public Property IR_LEN As Integer 'Number of bits of the instruction register
    Public Property AmdFlashDelay As UInt16 = 250 'Delay in clock cycles (16MHz total)
    Public Property IntelFlashDelay As UInt16 = 250 'Delay in clock cycles (16MHz total)
    Public Property DMA_Delay As UInt16 = 250

    Public TargetDevice As JTAGHOST 'Loaded after Init() has been called and returns true

    Public TAP As JTAG_STATE_CONTROLLER 'JTAG state machine
    Public WithEvents JSP As New JTAG.SVF_Player(Me) 'SVF / XSVF Parser and player

    Sub New(ByVal parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
        TAP = Nothing
    End Sub
    'Connects to the target device
    Public Function Init() As Boolean
        Try
            TargetDevice.IDCODE = 0 'Reset host information
            SPI_API_LOADED = False
            Dim ChipIrLen As Integer = TAP_Detect() 'Turns 4 for 3348?
            If ChipIrLen = 0 Then Return False
            If Not SetIRLen(ChipIrLen) Then Return False 'Makes us select this chip for communication
            WriteConsole(String.Format("JTAG: IR length set to {0}", ChipIrLen.ToString))
            TAP = New JTAG_STATE_CONTROLLER
            AddHandler TAP.ShiftBits, AddressOf Tap_ShiftBits
            TAP.Reset()
            TargetDevice.IDCODE = ReadWriteData(JTAG_OPCODE.IDCODE)
            TargetDevice.VERSION = CShort((TargetDevice.IDCODE And &HF0000000L) >> 28)
            TargetDevice.PARTNU = CUShort((TargetDevice.IDCODE And &HFFFF000) >> 12)
            TargetDevice.MANUID = CUShort((TargetDevice.IDCODE And &HFFE) >> 1)
            TargetDevice.IMPCODE = ReadWriteData(JTAG_OPCODE.IMPCODE) 'This might return 0 for non EJTAG devices
            Load_EJAG_Capabilities(TargetDevice.IMPCODE) 'Only supported by MIPS/EJTAG devices
            If TargetDevice.MANUID = &HBF Then
                SelectedAPI = BrcmAPI
                SPI_API_LOADED = True
            ElseIf TargetDevice.MANUID = &H70 Then
                SelectedAPI = AtherosAPI
                SPI_API_LOADED = True
            End If
            If TargetDevice.DMA_SUPPORTED Then 'Clears protection bit
                Dim r As UInteger = DMA_ReadData(&HFF300000UI, DataWidth.Word) 'Returns 2000001E 
                r = r And &HFFFFFFFBUI '2000001A
                DMA_WriteData(&HFF300000UI, r, DataWidth.Word)
            Else
                'ProcessorReset()
                'DebugMode_Enable()
                WriteConsole("JTAG device does not support DMA Access")
                'Return False
            End If
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    'Loads specific features this device using EJTAG IMP OPCODE
    Private Sub Load_EJAG_Capabilities(IMP As UInt32)
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
        If b = 1 Then TargetDevice.DMA_SUPPORTED = False Else TargetDevice.DMA_SUPPORTED = True
        b = (IMP And &H1)
        If b = 1 Then TargetDevice.MIPS32 = False : TargetDevice.MIPS64 = True Else TargetDevice.MIPS32 = True : TargetDevice.MIPS64 = False
    End Sub

    Private Sub Tap_ShiftBits(ByVal BitCount As UInt32, ByVal tdi_bits() As Byte, ByVal tms_bits() As Byte, ByRef tdo_bits() As Byte)
        Array.Reverse(tdi_bits)
        Array.Reverse(tms_bits)
        tdo_bits = ShiftBulk(BitCount, tdi_bits, tms_bits)
        If tdo_bits IsNot Nothing Then Array.Reverse(tdo_bits)
    End Sub

    Private Sub HandleJtagPrintRequest(ByVal msg As String) Handles JSP.Printf
        WriteConsole("SVF Player: " & msg)
    End Sub

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

    Public Function CFI_ReadFlash(ByVal Address As UInteger, ByVal count As Integer) As Byte()
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
    Friend BrcmAPI As New SPI_API(TargetType.Broadcom)
    Friend AtherosAPI As New SPI_API(TargetType.Atheros)
    Friend SelectedAPI As SPI_API 'Contains one of the above APIs to use
    Friend SPI_API_LOADED As Boolean = False 'Indicates the processor supports SPI access
    Friend SPI_Part As FlashMemory.SPI_NOR_FLASH
    'Returns TRUE if the JTAG can detect a connected flash to the SPI port
    Public Function SPI_Detect() As Boolean
        Dim reg As UInt32 = SPI_SendCommand(SelectedAPI.READ_ID, 1, 3)
        WriteConsole(String.Format("JTAG: SPI register returned {0}", "0x" & Hex(reg)))
        Dim ReadBack() As Byte = Utilities.Bytes.FromUInt32(reg, False) 'Returns 4 bytes
        Dim PartNum As UInt16 = (CUInt(ReadBack(1)) << 8) + ReadBack(2)
        SPI_Part = FlashDatabase.FindDevice(ReadBack(0), PartNum, 0, False, FlashMemory.MemoryType.PARALLEL_NOR)
        If SPI_Part IsNot Nothing Then
            WriteConsole(String.Format("JTAG: SPI flash detected ({0})", SPI_Part.NAME))
            If SelectedAPI.DTYPE = TargetType.Broadcom Then
                Memory_Write_W(SelectedAPI.REG_CNTR, 0)
                Dim data() As Byte = SPI_ReadFlash(0, 4) 'Why?
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

    Public Sub SPI_EraseBulk()
        WriteConsole("Erasing entire SPI flash (this could take a moment)")
        Memory_Write_W(SelectedAPI.REG_CNTR, 0) 'Might need to be for Ath too
        SPI_SendCommand(SelectedAPI.WREN, 1, 0)
        Memory_Write_W(SelectedAPI.REG_OPCODE, SelectedAPI.BASE)
        Memory_Write_W(SelectedAPI.REG_CNTR, &H800000C7UI) 'Might need to be for Ath too
        SPI_WaitUntilReady()
        WriteConsole("Erase operation complete!")
    End Sub

    Public Function SPI_SectorErase(ByVal secotr_ind As UInt32) As Boolean
        Try
            Dim Addr24 As UInteger = SPI_FindSectorBase(secotr_ind)
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
                spiWrite(Address, BlockToWrite, SelectedAPI.DTYPE)
                BytesWritten += BlockToWrite.Length
                BytesRemaining -= BlockToWrite.Length
                Address += CUInt(BlockToWrite.Length)
            End While
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

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

    Public Function SPI_ReadFlash(ByVal Address As UInteger, ByVal count As Integer) As Byte()
        Return Memory_Read_Bulk(SelectedAPI.BASE + Address, count)
    End Function

    Private Sub spiWrite(ByVal Offset As UInteger, ByVal Data() As Byte, ByVal JtagDev As TargetType)
        Dim count As Integer = Data.Length
        If count < 1 Or count > MAX_USB_BUFFER_SIZE Then Exit Sub
        Dim BytesWritten As Integer = 0
        Dim OpCode As Byte
        Select Case JtagDev
            Case TargetType.Broadcom
                OpCode = USB.USBREQ.JTAG_FLASHSPI_BRCM
            Case TargetType.Atheros
                OpCode = USB.USBREQ.JTAG_FLASHSPI_ATH
            Case Else
                Exit Sub
        End Select
        Try
            Dim SizeArray() As Byte = {CByte(count And &HFF), CByte((count And &HFF00) >> 8)}
            Dim setup_data As UInt32 = ((Offset And &HFFFF) << 16) Or ((Offset >> 16) And &HFFFF)
            FCUSB.USB_CONTROL_MSG_OUT(OpCode, SizeArray, setup_data)
            FCUSB.USB_BULK_OUT(Data)
        Catch
        End Try
    End Sub



#End Region

#Region "DMA"
    'Target device needs to support DMA and FCUSB needs to include flashmode
    Public Sub DMA_WriteFlash(ByVal Address As UInt32, ByVal DataToWrite() As Byte, ByVal flashmode As cfi_mode)
        Dim BlankCheck As Boolean = True
        Dim BytesWritten As Integer = 0
        For i = 0 To DataToWrite.Length - 1
            If DataToWrite(i) = &HFF Then BlankCheck = False : Exit For
        Next
        If BlankCheck Then Exit Sub 'No need to write blank data
        Try
            Dim BufferIndex As Integer = 0
            Dim BytesLeft As Integer = DataToWrite.Length
            Dim Counter As Integer = 0
            Do Until BytesLeft = 0
                If BytesLeft > MAX_USB_BUFFER_SIZE Then
                    Dim Packet(MAX_USB_BUFFER_SIZE - 1) As Byte
                    Array.Copy(DataToWrite, BufferIndex, Packet, 0, Packet.Length)
                    DMA_WriteFlash_Block(Address, Packet, flashmode)
                    Address = Address + MAX_USB_BUFFER_SIZE
                    BufferIndex = BufferIndex + MAX_USB_BUFFER_SIZE
                    BytesLeft = BytesLeft - MAX_USB_BUFFER_SIZE
                    BytesWritten += MAX_USB_BUFFER_SIZE
                Else
                    Dim Packet(BytesLeft - 1) As Byte
                    Array.Copy(DataToWrite, BufferIndex, Packet, 0, Packet.Length)
                    DMA_WriteFlash_Block(Address, Packet, flashmode)
                    BytesLeft = 0
                End If
                Counter += 1
            Loop
        Catch
        End Try
    End Sub

    Private Function DMA_WriteFlash_Block(ByVal FlashAddr As UInt32, ByVal data() As Byte, ByVal sub_cmd As cfi_mode) As Boolean
        Dim DataCount As Integer = data.Length
        If DataCount > MAX_USB_BUFFER_SIZE Then Return False
        Dim SizeArray() As Byte = {CByte(DataCount And &HFF), CByte((DataCount And &HFF00) >> 8)}
        Dim setup_data As UInt32 = (&HFFFF And FlashAddr) << 16 Or CUShort(FlashAddr >> 16)
        If Not FCUSB.USB_CONTROL_MSG_OUT(sub_cmd, SizeArray, setup_data) Then Return False
        Return FCUSB.USB_BULK_OUT(data)
    End Function

    Private Function DMA_ReadData(ByVal addr As UInteger, ByVal type As DataWidth) As UInteger
        Try
            Dim ReadBack As Byte() = New Byte(3) {}
            Dim cmd As Byte = 0
            Select Case type
                Case DataWidth.Word
                    cmd = USB.USBREQ.JTAG_DMAREAD_W
                Case DataWidth.HalfWord
                    cmd = USB.USBREQ.JTAG_DMAREAD_H
                Case DataWidth.Byte
                    cmd = USB.USBREQ.JTAG_DMAREAD_B
            End Select
            Dim setup_data As UInt32 = ((addr And &HFFFF) << 16) Or ((addr >> 16) And &HFFFF)
            FCUSB.USB_CONTROL_MSG_IN(cmd, ReadBack, setup_data)
            Array.Reverse(ReadBack)
            Select Case type
                Case DataWidth.Word
                    Return Utilities.Bytes.ToUInteger(ReadBack)
                Case DataWidth.HalfWord
                    Return (CUInt(ReadBack(0)) << 8) + ReadBack(1)
                Case DataWidth.Byte
                    Return CUInt(ReadBack(0))
                Case Else
                    Return 0
            End Select
        Catch ex As Exception
        End Try
        Return 0
    End Function

    Private Function DMA_ReadBulk(ByVal Address As UInteger, ByVal count As Integer) As Byte()
        If count < 1 Or count > MAX_USB_BUFFER_SIZE Then Return Nothing
        Dim dataout(count - 1) As Byte
        Try
            Dim SizeArray() As Byte = {CByte(count And &HFF), CByte((count And &HFF00) >> 8)}
            Dim setup_data As UInt32 = ((Address And &HFFFF) << 16) Or ((Address >> 16) And &HFFFF)
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_DMAREADBULK, SizeArray, setup_data)
            If Not FCUSB.USB_BULK_IN(dataout) Then Return Nothing
        Catch
        End Try
        Return dataout
    End Function

    Private Sub DMA_WriteData(ByVal Address As UInteger, ByVal data As UInteger, ByVal type As DataWidth)
        Try
            Dim ret As Integer = 0
            Dim cmd As Byte = 0
            Select Case type
                Case DataWidth.Word
                    cmd = USB.USBREQ.JTAG_DMAWRITE_W
                Case DataWidth.HalfWord
                    cmd = USB.USBREQ.JTAG_DMAWRITE_H
                Case DataWidth.Byte
                    cmd = USB.USBREQ.JTAG_DMAWRITE_B
            End Select
            Dim setup_data As UInt32 = ((Address And &HFFFF) << 16) Or ((Address >> 16) And &HFFFF)
            Dim dd() As Byte = BitConverter.GetBytes(data)
            FCUSB.USB_CONTROL_MSG_OUT(cmd, dd, setup_data)
        Catch ex As Exception
        End Try
    End Sub

    Private Function DMA_WriteBulk(ByVal Address As UInteger, ByVal Data() As Byte) As Boolean
        Dim count As Integer = Data.Length
        Try
            Dim SizeArray() As Byte = {CByte(count And &HFF), CByte((count And &HFF00) >> 8)}
            Dim setup_data As UInt32 = ((Address And &HFFFF) << 16) Or ((Address >> 16) And &HFFFF)
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_DMAWRITEBULK, SizeArray, setup_data)
            If Not FCUSB.USB_BULK_OUT(Data) Then Return False
        Catch
        End Try
        Return True
    End Function

#End Region

#Region "PrAcc"

#Region "Code modules"

    Private pracc_readword_code_module() As UInt32 = {
        &H3C01FF20UI, 'lui		$1,0xFF20
        &H34211000UI, 'ori		$1,0x1000
        &H8C220000UI, 'lw		$2,0($1)
        &H8C430000UI, 'lw		$3,0($2)
        &HAC230004UI, 'sw		$3,4($1)
        &H1000FFFAUI, 'beq		$0,$0, start (go back to the top)
        0} 'nop

    Private pracc_readhalf_code_module() As UInt32 = {
        &H3C01FF20UI, 'lui $1,  0xFF20
        &H34211000UI, 'ori $1,  0x1000
        &H8C220000UI, 'lw $2,  ($1)  #loads r2 with address for read
        &H94430000UI, 'lhu $3, 0($2) #Load r3 with H at r2
        &HAC230004UI, 'sw $3, 4($1)  #Store the value into the pseudo-data register
        &H1000FFFAUI, 'beq $0, $0, start
        0} 'nop

    Private pracc_writeword_code_module() As UInt32 = {
        &H3C01FF20UI, 'lui $1,  0xFF20 # Load R1 with the address of the pseudo-address register
        &H34211000UI, 'ori $1,  0x1000
        &H8C220000UI, 'lw $2,  ($1) # Load R2 with the address for the write
        &H8C230004UI, 'lw $3, 4($1) # Load R3 with the data from pseudo-data register
        &H1000FFFBUI, 'beq $0, $0, start
        &HAC430000UI} 'sw $3,  ($2) # Store the word at @R2 (the address)

    Private pracc_writehalf_code_module() As UInt32 = {
        &H3C01FF20UI, 'lui $1,  0xFF20		#Load R1 with the address of the pseudo-address register
        &H34211000UI, 'ori $1,  0x1000
        &H8C220000UI, 'lw $2,  ($1)		#Load R2 with the address for the write
        &H8C230004UI, 'lw $3, 4($1)		#Load R3 with the data from pseudo-data register
        &H1000FFFBUI, 'beq $0, $0, start
        &HA4430000UI} 'sh $3,  ($2) #Store the half word at @R2 (the address)

    Private pracc_writebyte_code_module() As UInt32 = {
        &H3C01FF20UI, 'lui $1,0xFF20 # Load R1 with the address of the pseudo-address register
        &H34211000UI, 'ori $1,0x1000
        &H8C220000UI, 'lw $2,0($1) # Load R2 with the address for the write
        &H8C230004UI, 'lw $3,4($1) # Load R3 with the data from pseudo-data register
        &H1000FFFBUI, 'beq $0, $0, start
        &HA0430000UI} 'sb $3,0($2) # Store the byte at @R2 (the address)

    Private pracc_readbulk_code_module() As UInt32 = {
        &H3C09FF20UI, 'la    $t1,0xFF201000
        &H35291000UI,
        &H252C1000UI, 'addiu  $t4,$t1,0x1000
        &H8D2A0000UI, 'lw    $t2,0($t1) (start address)
        &H8D2B0004UI, 'lw    $t3,4($t1) (words to read)
        &H1160FFFAUI, 'beqz  $t3,-6		#Quit when we have read all of the words
        &H216BFFFFUI, 'addi  $t3,$t3,-1
        &H8D490000UI, 'lw    $t1,0($t2)  Reads the data from EJTAG address
        &HAD890000UI, 'sw    $t1,0($t4)  Stores the data to 0xFF202000+offset
        &H214A0004UI, 'addi  $t2,$t2,4
        &H1000FFFAUI, 'b     -6
        &H218C0004UI} 'addi  $t4,$t4,4  #increase address by 4

    Private pracc_writebulk_code_module() As UInt32 = {
        &H3C09FF20UI, 'la    $t1,0xFF201000
        &H35291000UI,
        &H252C1000UI, 'addiu  $t4,$t1,0x1000
        &H8D2A0000UI, 'lw    $t2,0($t1) (start address)
        &H8D2B0004UI, 'lw    $t3,4($t1) (words to write)
        &H1160FFFAUI, 'beqz  $t3,-6		#Quit when we have read all of the words
        &H216BFFFFUI, 'addi  $t3,$t3,-1
        &H8D890000UI, 'lw    $t1,0($t4)
        &HAD490000UI, 'sw    $t1,0($t2)
        &H214A0004UI, 'addi  $t2,$t2,4
        &H1000FFFAUI, 'b     -6
        &H218C0004UI} 'addi  $t4,$t4,4  #increase address by 4

#End Region

    Private Enum pracc_modules
        none
        readword
        readhalf
        writeword
        writehalf
        writebyte
        readbulk
        writebulk
    End Enum

    Private pracc_selected_module As pracc_modules = pracc_modules.none

    Private Sub PRACC_LoadModule(sel_mod As pracc_modules)
        If sel_mod = pracc_selected_module Then Exit Sub
        Dim module_data() As Byte = Nothing
        Select Case sel_mod
            Case pracc_modules.readword
                module_data = Utilities.Bytes.FromUint32Array(pracc_readword_code_module)
            Case pracc_modules.readhalf
                module_data = Utilities.Bytes.FromUint32Array(pracc_readhalf_code_module)
            Case pracc_modules.writeword
                module_data = Utilities.Bytes.FromUint32Array(pracc_writeword_code_module)
            Case pracc_modules.writehalf
                module_data = Utilities.Bytes.FromUint32Array(pracc_writehalf_code_module)
            Case pracc_modules.writebyte
                module_data = Utilities.Bytes.FromUint32Array(pracc_writebyte_code_module)
            Case pracc_modules.readbulk
                module_data = Utilities.Bytes.FromUint32Array(pracc_readbulk_code_module)
            Case pracc_modules.writebulk
                module_data = Utilities.Bytes.FromUint32Array(pracc_writebulk_code_module)
        End Select
        Utilities.ChangeEndian32_LSB8(module_data) 'The Stream bytes will align this data incorrectly
        Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_PRACC_LOAD, module_data) 'Writes the module into the MCU RAM (Supports up to 16 instructions)
        pracc_selected_module = sel_mod
    End Sub

    Private Sub PRACC_WriteData(ByVal Address As UInt32, ByVal data As UInteger, ByVal mode As DataWidth)
        TAP.GotoState(JTAG_STATE_CONTROLLER.MachineState.Select_DR) 'ExecuteDebugModule() starts at Select-DR
        Select Case mode
            Case DataWidth.Word
                PRACC_LoadModule(pracc_modules.writeword)
            Case DataWidth.HalfWord
                PRACC_LoadModule(pracc_modules.writehalf)
            Case DataWidth.Byte
                PRACC_LoadModule(pracc_modules.writebyte)
        End Select
        Dim setup_data() As Byte = Utilities.Bytes.FromUInt32(data, False)
        Dim index As UInt32 = (Address << 16) Or (Address >> 16)
        Dim result As Boolean = FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_PRACC_WR, setup_data, index)
    End Sub

    Private Function PRACC_ReadData(ByVal Address As UInteger, ByVal mode As DataWidth) As UInt32
        TAP.GotoState(JTAG_STATE_CONTROLLER.MachineState.Select_DR) 'ExecuteDebugModule() starts at Select-DR
        Select Case mode
            Case DataWidth.Word
                PRACC_LoadModule(pracc_modules.readword)
            Case DataWidth.HalfWord
                PRACC_LoadModule(pracc_modules.readhalf)
            Case DataWidth.Byte
                PRACC_LoadModule(pracc_modules.readhalf)
        End Select
        Dim index As UInt32 = (Address << 16) Or (Address >> 16)
        Dim data_back(3) As Byte
        Dim result As Boolean = FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_PRACC_RD, data_back, index)
        Return BitConverter.ToUInt32(data_back, 0)
    End Function

    Private Function PRACC_ReadBulk(ByVal Address As UInteger, ByVal count As Integer) As Byte()
        TAP.GotoState(JTAG_STATE_CONTROLLER.MachineState.Select_DR) 'ExecuteDebugModule() starts at Select-DR
        PRACC_LoadModule(pracc_modules.readbulk)
        Dim bytes_left As UInt32 = count
        Dim pointer As UInt32 = 0
        Dim data_out(count - 1) As Byte
        Do Until bytes_left = 0
            Dim word_count As Integer = Math.Ceiling(Math.Min(64, bytes_left) / 4)
            Dim setup_data() As Byte = Utilities.Bytes.FromUInt32(word_count, False)
            Dim index As UInt32 = (Address << 16) Or (Address >> 16)
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_PRACC_WR, setup_data, index) 'MEMORY->TX_BUFFER
            Dim packet_size((word_count * 4) - 1) As Byte
            FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_PRACC_BUFFER, packet_size) 'TX_BUFFER->USB
            If (word_count * 4) > bytes_left Then
                Array.Copy(packet_size, 0, data_out, pointer, bytes_left)
                bytes_left = 0
            Else
                Array.Copy(packet_size, 0, data_out, pointer, packet_size.Length)
                pointer += packet_size.Length
                Address += packet_size.Length
                bytes_left -= packet_size.Length
            End If
        Loop
        Return data_out
    End Function

    Private Function PRACC_WriteBulk(ByVal Address As UInteger, ByVal Data() As Byte) As Boolean
        TAP.GotoState(JTAG_STATE_CONTROLLER.MachineState.Select_DR) 'ExecuteDebugModule() starts at Select-DR
        'Not implementeded
        Return False
    End Function

#End Region

#Region "Reading/Writing to Memory"
    'Sets parameters in device
    Public Sub SetParameter()
        FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SETPARAM, Nothing, (Me.IntelFlashDelay << 16) + 1)
        FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SETPARAM, Nothing, (Me.AmdFlashDelay << 16) + 2)
        FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SETPARAM, Nothing, (Me.DMA_Delay << 16) + 3)
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
        Else
            Return CByte(PRACC_ReadData(addr, DataWidth.HalfWord) And &HFF)
        End If
    End Function

    Public Function Memory_Read_H(ByVal addr As UInt32) As UShort
        If TargetDevice.DMA_SUPPORTED Then
            Return CUShort(DMA_ReadData(addr, DataWidth.HalfWord) And &HFFFF)
        Else
            Return CUShort(PRACC_ReadData(addr, DataWidth.HalfWord) And &HFFFF)
        End If
    End Function

    Public Function Memory_Read_W(ByVal addr As UInt32) As UInteger
        If TargetDevice.DMA_SUPPORTED Then
            Return DMA_ReadData(addr, DataWidth.Word)
        Else
            Return PRACC_ReadData(addr, DataWidth.Word)
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
            If BytesToRead > MAX_USB_BUFFER_SIZE Then BytesToRead = MAX_USB_BUFFER_SIZE
            Dim Offset As UInt32 = LargeCount - BytesLeft
            Dim TempBuffer() As Byte = Nothing
            If TargetDevice.DMA_SUPPORTED Then
                TempBuffer = DMA_ReadBulk(Address + Offset, BytesToRead)
            Else
                TempBuffer = PRACC_ReadBulk(Address + Offset, BytesToRead)
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
        Else
            PRACC_WriteData(addr, CByte(data And &HFF), DataWidth.Byte)
        End If
    End Sub

    Public Sub Memory_Write_H(ByVal addr As UInteger, ByVal data As UInt16)
        If TargetDevice.DMA_SUPPORTED Then
            Dim pattern As UInt32 = (data << 16) Or data
            DMA_WriteData(addr, pattern, DataWidth.HalfWord)
        Else
            PRACC_WriteData(addr, data, DataWidth.HalfWord)
        End If
    End Sub

    Public Sub Memory_Write_W(ByVal addr As UInteger, ByVal data As UInt32)
        If TargetDevice.DMA_SUPPORTED Then
            DMA_WriteData(addr, data, DataWidth.Word)
        Else
            PRACC_WriteData(addr, data, DataWidth.Word)
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
                Else
                    PRACC_WriteBulk(Address, BlockToWrite)
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

    Public Structure JTAGHOST
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
        Public DMA_SUPPORTED As Boolean 'Indicates no DMA support and must use PrAcc mode
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

    Private Enum DataWidth As Byte
        [Byte]
        HalfWord
        Word
    End Enum
    'Attempts to auto-detect a JTAG device on the TAP, returns the IR Length of the device
    Public Function TAP_Detect() As UInt32
        Dim chipiddata(3) As Byte
        If Not FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_DETECT, chipiddata) Then Return 0 'Error
        Dim dint As UInteger = BitConverter.ToUInt32(chipiddata, 0)
        If Utilities.HWeight32(dint) <> 1 Then Return 0
        Dim ir As UInt32 = 0
        While dint <> 0
            dint >>= 1
            ir += 1
        End While
        ir -= 1
        Return ir
    End Function

    Public Function Extest(ByVal bsrlen As UShort, ByVal DataToLoad() As Byte) As Byte()
        Dim xfer As Integer = 0 'Number of bytes transfered back
        If bsrlen > 1024 Then Return Nothing 'Over 1024 bits and we will need to do some memory management
        Dim ByteCount As UShort = Math.Ceiling(bsrlen / 8)
        Dim setup_data As UInt32 = (bsrlen << 16) Or ByteCount
        If Not FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_EXTEST, DataToLoad, setup_data) Then Return Nothing
        Dim DataBack(ByteCount - 1) As Byte
        If Not FCUSB.USB_BULK_IN(DataBack) Then Return Nothing
        Return DataBack
    End Function

    Public Function Sample(ByVal bsrlen As UShort) As Byte()
        If bsrlen > 1024 Then Return Nothing 'Over 1024 bits and we will need to do some memory management
        Dim ByteCount As UShort = Math.Ceiling(bsrlen / 8)
        Dim buffer(ByteCount - 1) As Byte
        Dim setup_data As UInt32 = (bsrlen << 16) Or ByteCount
        If Not FCUSB.USB_CONTROL_MSG_IN(USB.USBREQ.JTAG_SAMPLE, buffer, setup_data) Then Return Nothing
        Return buffer
    End Function

    Public Function Preload(ByVal bsrlen As UShort, ByVal DataToLoad() As Byte) As Boolean
        Dim xfer As Integer = 0 'Number of bytes transfered back
        If bsrlen > 1024 Then Return Nothing 'Over 1024 bits and we will need to do some memory management
        Dim ByteCount As UShort = Math.Ceiling(bsrlen / 8)
        Dim setup_data As UInt32 = (bsrlen << 16) Or ByteCount
        Return FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_PRELOAD, DataToLoad, setup_data)
    End Function
    'Resets the processor (EJTAG ONLY)
    Public Sub ProcessorReset()
        ReadWriteData(JTAG_OPCODE.CONTROL_IR, (EJTAG_CTRL.PrRst Or EJTAG_CTRL.PerRst))
    End Sub

    'Writes to the instruction-register and then shifts out 32-bits from the DR
    Public Function ReadWriteData(ByVal ir_reg As JTAG_OPCODE, Optional value_in As UInt32 = 0) As UInt32
        Dim data_in() As Byte = Utilities.Bytes.FromUInt32(value_in)
        Dim data_out() As Byte = Nothing
        TAP.ShiftIR({ir_reg}, Nothing, IR_LEN)
        TAP.ShiftDR(data_in, data_out, 32)
        TAP.GotoState(JTAG_STATE_CONTROLLER.MachineState.Select_DR) 'Default parking
        Dim value_out As UInt32 = Utilities.Bytes.ToUInteger(data_out)
        Return value_out
    End Function

    Public Function DebugMode_Enable() As Boolean
        Try
            Dim debug_flag As UInt32 = EJTAG_CTRL.PrAcc Or EJTAG_CTRL.ProbEn Or EJTAG_CTRL.SetDev Or EJTAG_CTRL.JtagBrk
            Dim ctrl_reg As UInt32 = ReadWriteData(JTAG_OPCODE.CONTROL_IR, debug_flag)
            If (ReadWriteData(JTAG_OPCODE.CONTROL_IR, EJTAG_CTRL.PrAcc Or EJTAG_CTRL.ProbEn Or EJTAG_CTRL.SetDev) And EJTAG_CTRL.BrkSt) Then
                Return True
            Else
                Return False
            End If
        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Sub DebugMode_Disable()
        Try
            Dim flag As UInt32 = (EJTAG_CTRL.ProbEn Or EJTAG_CTRL.SetDev) 'This clears the JTAGBRK bit
            ReadWriteData(JTAG_OPCODE.CONTROL_IR, flag)
        Catch ex As Exception
        End Try
    End Sub

    Private Const MAX_USB_BUFFER_SIZE As UShort = 4096 'Max number of bytes we should send via USB bulk endpoints
    Private Const MIPS_DEBUG_VECTOR_ADDRESS As UInt32 = &HFF200200UI
    Private Const MIPS_VIRTUAL_ADDRESS_ACCESS As UInt32 = &HFF200000UI
    Private Const MIPS_VIRTUAL_DATA_ACCESS As UInt32 = &HFF200004UI
    Private Const LATTICE_PART As Byte = &HF
    Private Const LATTICE_IDCODE As Byte = &H16

    Public Enum JTAG_OPCODE As Byte
        EXTEST = &H0
        IDCODE = &H1 'Selects Device Identiﬁcation (ID) register
        IR_SAMPLE = &H2 'Free for other use, such as JTAG boundary scan
        IMPCODE = &H3 'Selects Implementation register
        ADDRESS_IR = &H8 'Selects Address register
        DATA_IR = &H9 'Selects Data register
        CONTROL_IR = &HA 'Selects EJTAG Control registe
        IR_ALL = &HB 'Selects the Address, Data and EJTAG Control registers
        IR_EJTAGBOOT = &HC 'Makes the processor take a debug exception after rese
        IR_NORMALBOOT = &HD 'Makes the processor execute the reset handler after rese
        IR_FASTDATA = &HE 'Selects the Data and Fastdata registers
        IR_TABCTRA = &H10 'Selects the control register TCBTraceControl in the Trace Control Bloc
        IR_TABCTRB = &H11 'Selects another trace control block register
        IR_TABCTRDATA = &H12 'Used to access the registers speciﬁed by the TCBCONTROLBREG ﬁeld and transfers data between the TAP and the TCB control register
        IR_TABCTRC = &H13 'Selects another trace control block register
        IR_EJWATCH = &H1C
        IR_BYPASS = &H1F 'Select Bypass register
        ISC_ENABLE_OTF = &HE4
        ISC_ENABLE = &HE8
        ISC_SRAM_READ = &HE7
        ISC_WRITE = &HE6
        ISC_ERASE = &HED
        ISC_PROGRAM = &HEA
        ISC_READ = &HEE
        ISC_INIT = &HF0
        ISC_DISABLE = &HC0
        USERCODE = &HFD
        BYPASS = &HFF
    End Enum

    Public Enum EJTAG_CTRL As UInt32
        '/* EJTAG 3.1 Control Register Bits */
        VPED = (1 << 23)       '/* R    */
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

    Public Sub ResetTAP()
        Try
            If TAP IsNot Nothing Then
                TAP.Reset()
            End If
        Catch ex As Exception
        End Try
    End Sub

    Public Sub Shiftout(bit_count As Byte, tdi As UInt32, tms As UInt32)
        Dim tdiarray() As Byte = BitConverter.GetBytes(tdi)
        Dim tmsarray() As Byte = BitConverter.GetBytes(tms)
        Dim TotalPacket(11) As Byte
        TotalPacket(0) = bit_count
        TotalPacket(4) = tdiarray(0)
        TotalPacket(5) = tdiarray(1)
        TotalPacket(6) = tdiarray(2)
        TotalPacket(7) = tdiarray(3)
        TotalPacket(8) = tmsarray(0)
        TotalPacket(9) = tmsarray(1)
        TotalPacket(10) = tmsarray(2)
        TotalPacket(11) = tmsarray(3)
        FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SHIFTOUT, TotalPacket)
    End Sub

    Public Sub Shiftout32(ByVal tdi As UInt32)
        Dim TotalPacket() As Byte = BitConverter.GetBytes(tdi)
        FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SHIFTOUT32, TotalPacket)
    End Sub

    Public Function ShiftIn(ByVal bit_count As UInt16, ByVal tdi32 As UInt32, ByVal tms32 As UInt32) As UInt32
        Dim tdi() As Byte = BitConverter.GetBytes(tdi32)
        Dim tms() As Byte = BitConverter.GetBytes(tms32)
        Dim tdo() As Byte = ShiftBulk(bit_count, tdi, tms)
        Return BitConverter.ToUInt32(tdo, 0)
    End Function
    'Byte from tdi(0) is shifted in first (LSB) then tdi(1) etc.
    Public Function ShiftBulk(ByVal bit_count As UInt32, ByVal tdi() As Byte, ByVal tms() As Byte) As Byte()
        Dim tdobytecollector As New ArrayList
        Dim flag1 As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
        Dim Ret As Integer = 0
        Dim pointer As Integer = 0
        Dim BytesLeft As Integer = tdi.Length
        Dim BitsLeft As UInt32 = bit_count
        ReDim Preserve tms(tdi.Length - 1)
        Do Until BytesLeft = 0
            Dim packet_size As UInt32 = BytesLeft
            Dim packet_bits As UInt32 = BitsLeft
            If (packet_size > 32) Then
                packet_size = 32
                packet_bits = 256
            End If
            Dim data_out((packet_size * 2) - 1) As Byte
            Array.Copy(tdi, pointer, data_out, 0, packet_size)
            Array.Copy(tms, pointer, data_out, packet_size, packet_size)
            Dim tdo(packet_size - 1) As Byte
            Dim setup_data As UInt32 = (CUInt(packet_bits) << 16) Or (packet_size And &HFFFF)
            FCUSB.USB_CONTROL_MSG_OUT(USB.USBREQ.JTAG_SHIFTIN, data_out, setup_data)
            If Not FCUSB.USB_BULK_IN(tdo) Then Return Nothing
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

End Class
