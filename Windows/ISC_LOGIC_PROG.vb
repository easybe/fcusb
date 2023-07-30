﻿'This provides in-circuit programming for MACHXO2 FPGA via SLAVE SPI
Imports FlashcatUSB.USB

Public Class ISC_LOGIC_PROG
    Private sel_usb_dev As FCUSB_DEVICE

    Private IDCODE_PUB() As Byte = {&HE0, 0, 0, 0}
    Private ISC_ENABLE() As Byte = {&HC6, &H8, 0, 0}
    Private ISC_ERASE() As Byte = {&HE, &H4, 0, 0}
    Private ISC_PROGRAMDONE() As Byte = {&H5E, 0, 0, 0}
    Private LSC_INITADDRESS() As Byte = {&H46, 0, 0, 0}
    Private LSC_PROGINCRNV() As Byte = {&H70, 0, 0, 1}
    Private LSC_READ_STATUS() As Byte = {&H3C, 0, 0, 0}
    Private LSC_REFRESH() As Byte = {&H79, 0, 0, 0}

    Public Event PrintConsole(msg As String)
    Public Event SetProgress(value As Integer)

    Private Const MACHXO2_PAGE_SIZE As Integer = 16

    Public Sub New(usb_dev As FCUSB_DEVICE)
        Me.sel_usb_dev = usb_dev
    End Sub

    Public Function SSPI_Init(spi_mode As UInt32, spi_select As UInt32, speed_mhz As UInt32) As Boolean
        Try
            Dim w32 As UInt32 = (spi_select << 24) Or (spi_mode << 16) Or speed_mhz
            Return sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.SPI_INIT, Nothing, w32)
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Sub SSPI_SS(enabled As Boolean)
        If enabled Then
            sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.SPI_SS_ENABLE) 'SS=LOW
        Else
            sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.SPI_SS_DISABLE) 'SS=HIGH
        End If
    End Sub

    Public Function SSPI_WriteData(data() As Byte) As Boolean
        Dim result As Boolean = sel_usb_dev.USB_SETUP_BULKOUT(USBREQ.SPI_WR_DATA, Nothing, data, CUInt(data.Length))
        Utilities.Sleep(2)
        Return result
    End Function

    Public Function SSPI_ReadData(ByRef Data_In() As Byte) As Boolean
        Dim Success As Boolean = False
        Try
            Success = sel_usb_dev.USB_SETUP_BULKIN(USBREQ.SPI_RD_DATA, Nothing, Data_In, CUInt(Data_In.Length))
        Catch ex As Exception
        End Try
        Return Success
    End Function

    Public Function SSPI_WriteRead(WriteBuffer() As Byte, Optional ByRef ReadBuffer() As Byte = Nothing) As UInt32
        If WriteBuffer Is Nothing And ReadBuffer Is Nothing Then Return 0
        Dim TotalBytesTransfered As UInt32 = 0
        SSPI_SS(True)
        If (WriteBuffer IsNot Nothing) Then
            Dim BytesWritten As Integer = 0
            Dim Result As Boolean = SSPI_WriteData(WriteBuffer)
            If Result Then TotalBytesTransfered += CUInt(WriteBuffer.Length)
        End If
        If (ReadBuffer IsNot Nothing) Then
            Dim BytesRead As Integer = 0
            Dim Result As Boolean = SSPI_ReadData(ReadBuffer)
            If Result Then TotalBytesTransfered += CUInt(ReadBuffer.Length)
        End If
        SSPI_SS(False)
        Return TotalBytesTransfered
    End Function

    Public Class SSPI_Status
        Public STATUS32 As UInt32
        Public ReadOnly Property DONE_FLAG As Boolean
        Public ReadOnly Property CHECK_STATUS As CFG_CHECK_STATUS
        Public ReadOnly Property CFG_IF_ENABLED As Boolean
        Public ReadOnly Property BUSY_FLAG As Boolean
        Public ReadOnly Property FAIL_FLAG As Boolean

        Sub New(cfg_register() As Byte)
            STATUS32 = Utilities.Bytes.ToUInt32(cfg_register)
            Me.DONE_FLAG = CBool((STATUS32 >> 8) And 1)
            Me.CFG_IF_ENABLED = CBool((STATUS32 >> 9) And 1)
            Me.BUSY_FLAG = CBool((STATUS32 >> 12) And 1)
            Me.FAIL_FLAG = CBool((STATUS32 >> 13) And 1)
            Me.CHECK_STATUS = CType(((STATUS32 >> 23) And 7), CFG_CHECK_STATUS)
        End Sub

        Public Enum CFG_CHECK_STATUS As Byte
            No_error = 0
            ID_error = 1
            CMD_error = 2
            CRC_error = 3
            Preamble_error = 4
            Abort_error = 5
            Overflow_error = 6
            SDM_EOF = 7
        End Enum

    End Class

    Private Function SSPI_Wait() As Boolean
        Dim MACHXO2_MAX_BUSY_LOOP As Integer = 128
        Dim counter As Integer = 0
        Dim current_status As SSPI_Status = Nothing
        Do
            current_status = SSPI_ReadStatus()
            Utilities.Sleep(10)
            counter += 1
            If counter = MACHXO2_MAX_BUSY_LOOP Then Return False 'TIMEOUT
            If current_status Is Nothing Then Return False 'ERROR
        Loop While current_status.BUSY_FLAG
        Return True
    End Function
    'If SPI Slave port is enabled, this will return 0x012BC043
    Public Function SSPI_ReadIdent() As UInt32
        Try
            Dim ID(3) As Byte
            SSPI_WriteRead(IDCODE_PUB, ID)
            Return Utilities.Bytes.ToUInt32(ID)
        Catch ex As Exception
        End Try
        Return 0
    End Function

    Private Function SSPI_ReadStatus() As SSPI_Status
        Try
            Dim STATUS(3) As Byte
            SSPI_WriteRead(LSC_READ_STATUS, STATUS)
            Return New SSPI_Status(STATUS)
        Catch ex As Exception
        End Try
        Return Nothing
    End Function
    'Fast! This will load an entire bitstream into the FPGA in 3 seconds.
    Public Function SSPI_ProgramMACHXO(logic() As Byte) As Boolean
        Dim bytes_left As Integer = logic.Length
        MainApp.PrintConsole("Programming FPGA with bitstream (" & bytes_left.ToString("#,###") & " bytes)")
        RaiseEvent SetProgress(0)
        Dim status As SSPI_Status = SSPI_ReadStatus()
        If status.BUSY_FLAG Then
            MainApp.PrintConsole("Error: FPGA device is busy") : Return False
        End If
        SSPI_WriteRead(ISC_ENABLE)
        status = SSPI_ReadStatus()
        If (status.FAIL_FLAG) Or (Not status.CFG_IF_ENABLED) Then
            MainApp.PrintConsole("Error: unable to enabling configuration interface") : Return False
        End If
        Dim erase_attemps As Integer = 0
        Dim erase_failed As Boolean
        Do
            erase_failed = False
            SSPI_WriteRead(ISC_ERASE)
            SSPI_Wait()
            status = SSPI_ReadStatus()
            If (status.FAIL_FLAG Or (Not status.CHECK_STATUS = SSPI_Status.CFG_CHECK_STATUS.No_error)) Then
                Utilities.Sleep(200)
                erase_failed = True
                erase_attemps += 1
                If erase_attemps = 3 Then
                    MainApp.PrintConsole("Error: logic erase command failed") : Return False
                End If
            End If
        Loop While erase_failed
        SSPI_WriteRead(LSC_INITADDRESS)
        Dim spi_size As Integer = CInt(Math.Ceiling(logic.Length / MACHXO2_PAGE_SIZE) * 4) + logic.Length
        Dim spi_buffer(spi_size - 1) As Byte
        Dim buffer_ptr As Integer = 0
        Dim logic_ptr As Integer = 0
        While (bytes_left > 0)
            Dim data_count As Integer = Math.Min(bytes_left, MACHXO2_PAGE_SIZE)
            Array.Copy(LSC_PROGINCRNV, 0, spi_buffer, buffer_ptr, LSC_PROGINCRNV.Length)
            buffer_ptr += LSC_PROGINCRNV.Length
            Array.Copy(logic, logic_ptr, spi_buffer, buffer_ptr, data_count)
            buffer_ptr += data_count
            logic_ptr += data_count
            bytes_left -= data_count
        End While
        bytes_left = spi_buffer.Length
        buffer_ptr = 0
        While (bytes_left > 0)
            Dim data_count As Integer = Math.Min(bytes_left, (MACHXO2_PAGE_SIZE + LSC_PROGINCRNV.Length) * 128) '128 pages
            Dim buffer_out(data_count - 1) As Byte
            Array.Copy(spi_buffer, buffer_ptr, buffer_out, 0, buffer_out.Length)
            Dim setup As UInt32 = CUInt(((MACHXO2_PAGE_SIZE + LSC_PROGINCRNV.Length) << 16) Or data_count)
            Dim result As Boolean = sel_usb_dev.USB_SETUP_BULKOUT(USBREQ.SPI_REPEAT, Nothing, buffer_out, setup)
            If Not result Then Return False
            sel_usb_dev.USB_WaitForComplete()
            bytes_left -= data_count
            buffer_ptr += data_count
            Dim percent_done As Integer = CInt(((spi_buffer.Length - bytes_left) / spi_buffer.Length) * 100)
            If (percent_done > 100) Then percent_done = 100
            RaiseEvent SetProgress(percent_done)
        End While
        SSPI_WriteRead(ISC_PROGRAMDONE)
        SSPI_Wait()
        RaiseEvent SetProgress(100)
        status = SSPI_ReadStatus()
        If Not status.DONE_FLAG Then Return False
        SSPI_WriteRead(LSC_REFRESH)
        status = SSPI_ReadStatus()
        If (Not status.BUSY_FLAG) AndAlso status.CHECK_STATUS = SSPI_Status.CFG_CHECK_STATUS.No_error Then
            Return True
        Else
            Return False
        End If
    End Function
    'For programming FlashcatUSB Professional PCB 5.0
    Public Function SSPI_ProgramICE(logic() As Byte) As Boolean
        Try
            SSPI_Init(3, 1, 24) 'MODE_3, CS_0, 24MHZ; PIN_FPGA_RESET=HIGH
            SSPI_SS(True) 'SS_LOW
            sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.PULSE_RESET) 'PCRESET_B=LOW; delay_us(200); PCRESET_B=HIGH
            SSPI_SS(False) 'SS_HIGH
            SSPI_WriteData({0}) '8 dummy clocks
            SSPI_SS(True) 'SS_LOW
            SSPI_WriteData(logic)
            SSPI_SS(False) 'SS_HIGH
            SSPI_WriteData({0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0})
            Dim Success As Boolean = SSPI_ICE_GetCDONE()
            If Success Then
                sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.LOGIC_START) 'SMC_Init(); PIN_FPGA_RESET=LOW
            End If
            Return Success
        Catch ex As Exception
        End Try
        Return False
    End Function

    Private Function SSPI_ICE_GetCDONE() As Boolean
        Dim s(3) As Byte
        sel_usb_dev.USB_CONTROL_MSG_IN(USBREQ.LOGIC_STATUS, s)
        If (s(0) = 0) Then Return False
        Return True
    End Function

End Class