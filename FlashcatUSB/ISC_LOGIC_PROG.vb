'This provides in-circuit programming for MACHXO2 FPGA via SLAVE SPI
Imports FlashcatUSB.USB
Imports FlashcatUSB.USB.HostClient

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

    Private Const MACHXO2_PAGE_SIZE As UInt32 = 16

    Public Sub New(usb_dev As FCUSB_DEVICE)
        Me.sel_usb_dev = usb_dev
    End Sub

    Public Sub SSPI_Init()
        Dim speed As UInt32 = 24 'In MHZ
        Dim spi_select As UInt32 = 1 '0=CS0; 1=CS1 (1=SLAVE SPI PORT)
        sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.SPI_INIT, Nothing, (spi_select << 16) Or speed)
    End Sub

    Private Sub SSPI_SS(enabled As Boolean)
        If enabled Then
            sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.SPI_SS_ENABLE) 'SS=LOW
        Else
            sel_usb_dev.USB_CONTROL_MSG_OUT(USBREQ.SPI_SS_DISABLE) 'SS=HIGH
        End If
        Utilities.Sleep(2)
    End Sub

    Private Function SSPI_WriteData(data() As Byte)
        sel_usb_dev.USB_SETUP_BULKOUT(USBREQ.SPI_WR_DATA, Nothing, data, data.Length)
        Utilities.Sleep(2)
        Return True
    End Function

    Private Function SSPI_ReadData(ByRef Data_In() As Byte) As Boolean
        Dim Success As Boolean = False
        Try
            Success = sel_usb_dev.USB_SETUP_BULKIN(USBREQ.SPI_RD_DATA, Nothing, Data_In, Data_In.Length)
        Catch ex As Exception
        End Try
        Return Success
    End Function

    Private Function SSPI_WriteRead(WriteBuffer() As Byte, Optional ByRef ReadBuffer() As Byte = Nothing) As UInt32
        If WriteBuffer Is Nothing And ReadBuffer Is Nothing Then Return 0
        Dim TotalBytesTransfered As UInt32 = 0
        SSPI_SS(True)
        If (WriteBuffer IsNot Nothing) Then
            Dim BytesWritten As Integer = 0
            Dim Result As Boolean = SSPI_WriteData(WriteBuffer)
            Utilities.Sleep(2)
            If Result Then TotalBytesTransfered += WriteBuffer.Length
        End If
        If (ReadBuffer IsNot Nothing) Then
            Dim BytesRead As Integer = 0
            Dim Result As Boolean = SSPI_ReadData(ReadBuffer)
            If Result Then TotalBytesTransfered += ReadBuffer.Length
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
            Me.DONE_FLAG = ((STATUS32 >> 8) And 1)
            Me.CFG_IF_ENABLED = ((STATUS32 >> 9) And 1)
            Me.BUSY_FLAG = ((STATUS32 >> 12) And 1)
            Me.FAIL_FLAG = ((STATUS32 >> 13) And 1)
            Me.CHECK_STATUS = ((STATUS32 >> 23) And 7)
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

    Public Function SSPI_ReadStatus() As SSPI_Status
        Try
            Dim STATUS(3) As Byte
            SSPI_WriteRead(LSC_READ_STATUS, STATUS)
            Return New SSPI_Status(STATUS)
        Catch ex As Exception
        End Try
        Return Nothing
    End Function

    Public Function SSPI_ProgramBitstream(logic() As Byte) As Boolean
        Dim bytes_left As Integer = logic.Length
        WriteConsole("Programming FPGA with bitstream (" & String.Format(bytes_left, "#,###") & " bytes)")
        RaiseEvent SetProgress(0)
        Dim status As SSPI_Status = SSPI_ReadStatus()
        If status.BUSY_FLAG Then
            WriteConsole("Error: FPGA device is busy") : Return False
        End If
        SSPI_WriteRead(ISC_ENABLE)
        status = SSPI_ReadStatus()
        If (status.FAIL_FLAG) Or (Not status.CFG_IF_ENABLED) Then
            WriteConsole("Error: unable to enabling configuration interface") : Return False
        End If
        SSPI_WriteRead(ISC_ERASE)
        SSPI_Wait()
        status = SSPI_ReadStatus()
        If (status.FAIL_FLAG Or (Not status.CHECK_STATUS = SSPI_Status.CFG_CHECK_STATUS.No_error)) Then
            WriteConsole("Error: logic erase command failed") : Return False
        End If
        SSPI_WriteRead(LSC_INITADDRESS)
        Dim ptr As Integer = 0
        Dim counter As Integer = 0
        While (bytes_left > 0)
            Dim page(MACHXO2_PAGE_SIZE + LSC_PROGINCRNV.Length - 1) As Byte
            Array.Copy(LSC_PROGINCRNV, 0, page, 0, LSC_PROGINCRNV.Length)
            Dim data_count As Integer = Math.Min(bytes_left, 16)
            Array.Copy(logic, ptr, page, LSC_PROGINCRNV.Length, data_count)
            bytes_left -= data_count
            ptr += data_count
            SSPI_WriteRead(page)
            counter += 1
            If (counter Mod 10 = 0) Then
                Dim percent_done As Integer = ((logic.Length - bytes_left) / logic.Length) * 100
                If (percent_done > 100) Then percent_done = 100
                RaiseEvent SetProgress(percent_done)
            End If
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


End Class
