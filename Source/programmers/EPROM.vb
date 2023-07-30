'COPYRIGHT EMBEDDED COMPUTERS LLC 2023 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB PRODUCTS
'CONTACT EMAIL: support@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: This is the main module that is loaded first.

Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.USB
Imports FlashcatUSB.USB.HostClient

Public Class EPROM_Programmer : Implements MemoryDeviceUSB

    Private FCUSB As FCUSB_DEVICE

    Public Property MyFlashDevice As OTP_EPROM
    Public Property MyFlashStatus As DeviceStatus = DeviceStatus.NotDetected
    Public Property MyAdapter As MEM_PROTOCOL 'This is the kind of socket adapter connected and the mode it is in

    Public Event PrintConsole(message As String) Implements MemoryDeviceUSB.PrintConsole
    Public Event SetProgress(percent As Integer) Implements MemoryDeviceUSB.SetProgress

    Sub New(parent_if As FCUSB_DEVICE)
        FCUSB = parent_if
    End Sub

    Public ReadOnly Property GetDevice As Device Implements MemoryDeviceUSB.GetDevice
        Get
            Return Me.MyFlashDevice
        End Get
    End Property

    Public ReadOnly Property DeviceName As String Implements MemoryDeviceUSB.DeviceName
        Get
            Select Case MyFlashStatus
                Case DeviceStatus.Supported
                    Return MyFlashDevice.NAME
                Case DeviceStatus.NotSupported
                    Return Hex(MyFlashDevice.MFG_CODE).PadLeft(2, CChar("0")) & " " & Hex(MyFlashDevice.ID1).PadLeft(4, CChar("0"))
                Case Else
                    Return RM.GetString("no_flash_detected")
            End Select
        End Get
    End Property

    Public ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
        Get
            Dim NOR_FLASH As OTP_EPROM = DirectCast(MyFlashDevice, OTP_EPROM)
            Return NOR_FLASH.FLASH_SIZE
        End Get
    End Property

    Public Function SectorSize(sector As Integer) As Integer Implements MemoryDeviceUSB.SectorSize
        Return 8192 'Program 8KB at a time
    End Function

    Public Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
        Utilities.Sleep(100)
    End Sub

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        MyFlashDevice = Nothing
        If EPROM_Detect() Then
            If MyFlashDevice.IFACE = VCC_IF.X16_5V_VPP Then
                EXPIO_SETUP_USB(MEM_PROTOCOL.EPROM_X16)
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_MODE_WRITE, Nothing, E_DEV_MODE.EPROM_X16)
                Me.MyAdapter = MEM_PROTOCOL.EPROM_X16
                RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": EPROM (16-bit)")
            ElseIf MyFlashDevice.IFACE = VCC_IF.X8_5V_VPP Then
                EXPIO_SETUP_USB(MEM_PROTOCOL.EPROM_X8)
                Me.MyAdapter = MEM_PROTOCOL.EPROM_X8
                FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_MODE_WRITE, Nothing, E_DEV_MODE.EPROM_X8)
                Me.MyAdapter = MEM_PROTOCOL.EPROM_X8
                RaiseEvent PrintConsole(RM.GetString("ext_write_mode_supported") & ": EPROM (8-bit)")
            End If
        Else
            RaiseEvent PrintConsole("Unable to automatically detect EPROM/OTP device")
            Return False
        End If
        RaiseEvent PrintConsole("EPROM successfully detected!")
        RaiseEvent PrintConsole("EPROM device: " & MyFlashDevice.NAME & ", size: " & Format(MyFlashDevice.FLASH_SIZE, "#,###") & " bytes")
        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_DELAY, Nothing, DirectCast(MyFlashDevice, OTP_EPROM).HARDWARE_DELAY)
        DirectCast(MyFlashDevice, OTP_EPROM).IS_BLANK = True
        'DirectCast(MyFlashDevice, OTP_EPROM).IS_BLANK = EPROM_BlankCheck()
        RaiseEvent PrintConsole("EPROM mode ready for operation")
        MyFlashStatus = DeviceStatus.Supported
        Return True
    End Function

    Public Function ReadData(flash_offset As Long, data_count As Integer) As Byte() Implements MemoryDeviceUSB.ReadData
        Dim M27C160 As OTP_EPROM = CType(FlashDatabase.FindDevice(&H20, &HB1, 0, MemoryType.OTP_EPROM), OTP_EPROM)
        Dim M27C801 As OTP_EPROM = CType(FlashDatabase.FindDevice(&H20, &H42, 0, MemoryType.OTP_EPROM), OTP_EPROM)
        Dim M27C1001 As OTP_EPROM = CType(FlashDatabase.FindDevice(&H20, &H5, 0, MemoryType.OTP_EPROM), OTP_EPROM)
        If MyFlashDevice Is M27C160 Then
            HardwareControl(FCUSB_HW_CTRL.VPP_5V)
            HardwareControl(FCUSB_HW_CTRL.VPP_ENABLE) 'Must enable VPP for BYTEvpp=HIGH(5V)
        ElseIf MyFlashDevice Is M27C1001 Then
            HardwareControl(FCUSB_HW_CTRL.VPP_DISABLE)
            HardwareControl(FCUSB_HW_CTRL.VPP_0V)
        Else
            HardwareControl(FCUSB_HW_CTRL.VPP_DISABLE)
            HardwareControl(FCUSB_HW_CTRL.VPP_0V)
        End If
        Utilities.Sleep(100)
        HardwareControl(FCUSB_HW_CTRL.WE_LOW)
        Utilities.Sleep(100)
        Dim data_out() As Byte = ReadBulk(CUInt(flash_offset), CInt(data_count))
        HardwareControl(FCUSB_HW_CTRL.WE_HIGH)
        HardwareControl(FCUSB_HW_CTRL.VPP_0V)
        HardwareControl(FCUSB_HW_CTRL.VPP_DISABLE)
        Return data_out
    End Function

    Public Function WriteData(flash_offset As Long, data_to_write() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.WriteData
        Try
            Dim EPROM_DEV As OTP_EPROM = DirectCast(MyFlashDevice, OTP_EPROM)
            Dim DataToWrite As Integer = data_to_write.Length
            Dim PacketSize As Integer = 2048
            Dim Loops As Integer = CInt(Math.Ceiling(DataToWrite / PacketSize)) 'Calcuates iterations
            HardwareControl(FCUSB_HW_CTRL.WE_HIGH)
            HardwareControl(FCUSB_HW_CTRL.VPP_12V)
            HardwareControl(FCUSB_HW_CTRL.VPP_ENABLE)
            If EPROM_DEV.WR_OE_HIGH Then HardwareControl(FCUSB_HW_CTRL.OE_HIGH)
            Utilities.Sleep(200)
            For i As Integer = 0 To Loops - 1
                If Params.AbortOperation Then Return False
                Dim BufferSize As Integer = DataToWrite
                If (BufferSize > PacketSize) Then BufferSize = PacketSize
                Dim data_packet(BufferSize - 1) As Byte
                Array.Copy(data_to_write, (i * PacketSize), data_packet, 0, data_packet.Length)
                Dim result As Boolean = WriteBulk(CUInt(flash_offset), data_packet)
                If (Not result) Then Return False
                FCUSB.USB_WaitForComplete() 'Wait for TASK=APP_IDLE
                flash_offset += data_packet.Length
                DataToWrite -= data_packet.Length
            Next
            HardwareControl(FCUSB_HW_CTRL.VPP_DISABLE)
            HardwareControl(FCUSB_HW_CTRL.VPP_0V)
            HardwareControl(FCUSB_HW_CTRL.OE_LOW)
        Catch ex As Exception
        End Try
        Return True
    End Function

    Public Function SectorFind(sector_index As Integer) As Long Implements MemoryDeviceUSB.SectorFind
        Dim base_addr As UInt32 = 0
        If sector_index > 0 Then
            For i As Integer = 0 To sector_index - 1
                base_addr += CUInt(Me.SectorSize(i))
            Next
        End If
        Return base_addr
    End Function

    Public Function SectorCount() As Integer Implements MemoryDeviceUSB.SectorCount
        Return MyFlashDevice.Sector_Count
    End Function

    Public Function SectorWrite(sector_index As Integer, data() As Byte, Optional Params As WriteParameters = Nothing) As Boolean Implements MemoryDeviceUSB.SectorWrite
        Dim Addr32 As UInteger = CUInt(Me.SectorFind(sector_index))
        Return WriteData(Addr32, data, Params)
    End Function

    Public Function EraseDevice() As Boolean Implements MemoryDeviceUSB.EraseDevice
        Throw New NotSupportedException()
    End Function

    Public Function SectorErase(SectorIndex As Integer) As Boolean Implements MemoryDeviceUSB.SectorErase
        Throw New NotSupportedException()
    End Function

    Public Function EPROM_Detect() As Boolean
        If Not EXPIO_SETUP_USB(MEM_PROTOCOL.EPROM_X8) Then Return False
        Utilities.Sleep(200)
        Dim IDENT_DATA() As Byte
        IDENT_DATA = EPROM_ReadEletronicID_1()
        MyFlashDevice = CType(FlashDatabase.FindDevice(IDENT_DATA(0), IDENT_DATA(1), 0, MemoryType.OTP_EPROM), OTP_EPROM)
        If MyFlashDevice IsNot Nothing Then Return True 'Detected!
        IDENT_DATA = EPROM_ReadEletronicID_2()
        MyFlashDevice = CType(FlashDatabase.FindDevice(IDENT_DATA(0), IDENT_DATA(1), 0, MemoryType.OTP_EPROM), OTP_EPROM)
        If MyFlashDevice IsNot Nothing Then Return True 'Detected!
        Return False
    End Function

    Private Function EXPIO_SETUP_USB(mode As MEM_PROTOCOL) As Boolean
        Try
            Dim result_data(0) As Byte
            Dim setup_data As UInt32 = mode
            Dim result As Boolean = FCUSB.USB_CONTROL_MSG_IN(USBREQ.EXPIO_INIT, result_data, setup_data)
            If Not result Then Return False
            If (result_data(0) = &H17) Then
                Threading.Thread.Sleep(50) 'Give the USB time to change modes
                Return True 'Communication successful
            Else
                Return False
            End If
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EPROM_ReadEletronicID_1() As Byte()
        Dim IDENT_DATA(1) As Byte
        HardwareControl(FCUSB_HW_CTRL.OE_LOW)
        HardwareControl(FCUSB_HW_CTRL.WE_LOW)
        HardwareControl(FCUSB_HW_CTRL.VPP_12V)
        HardwareControl(FCUSB_HW_CTRL.RELAY_ON)   'A9=12V and VPP=12V
        HardwareControl(FCUSB_HW_CTRL.VPP_ENABLE) 'Enables VPP on adapter (CLE=HIGH)
        Utilities.Sleep(300) 'Need this to be somewhat high to allow ID CODE to load
        FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_READDATA, GetSetupPacket(0, 2, 0), IDENT_DATA, 0)
        HardwareControl(FCUSB_HW_CTRL.VPP_DISABLE)
        HardwareControl(FCUSB_HW_CTRL.RELAY_OFF)
        HardwareControl(FCUSB_HW_CTRL.VPP_0V)
        HardwareControl(FCUSB_HW_CTRL.WE_HIGH)

        If IDENT_DATA(0) = 0 Or IDENT_DATA(1) = 0 Then
        ElseIf IDENT_DATA(0) = 255 Or IDENT_DATA(1) = 255 Then
        Else
            RaiseEvent PrintConsole("EPROM IDENT CODE 1 returned MFG: 0x" & Hex(IDENT_DATA(0)) & " and PART 0x" & Hex(IDENT_DATA(1)))
        End If
        Return IDENT_DATA
    End Function

    Private Function EPROM_ReadEletronicID_2() As Byte()
        Dim IDENT_DATA(1) As Byte
        HardwareControl(FCUSB_HW_CTRL.VPP_DISABLE) 'Disables VPP Pin on Adapter (CLE=LOW)
        HardwareControl(FCUSB_HW_CTRL.OE_LOW)
        HardwareControl(FCUSB_HW_CTRL.WE_LOW)
        HardwareControl(FCUSB_HW_CTRL.VPP_12V)
        HardwareControl(FCUSB_HW_CTRL.RELAY_ON) 'A9=12V and VPP=0V
        Utilities.Sleep(300) 'Need this to be somewhat high to allow ID CODE to load
        FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_READDATA, GetSetupPacket(0, 2, 0), IDENT_DATA, 0)
        HardwareControl(FCUSB_HW_CTRL.RELAY_OFF)
        HardwareControl(FCUSB_HW_CTRL.VPP_0V)
        HardwareControl(FCUSB_HW_CTRL.WE_HIGH)
        If IDENT_DATA(0) = 0 Or IDENT_DATA(1) = 0 Then
        ElseIf IDENT_DATA(0) = 255 Or IDENT_DATA(1) = 255 Then
        Else
            RaiseEvent PrintConsole("EPROM IDENT CODE 2 returned MFG: 0x" & Hex(IDENT_DATA(0)) & " and PART 0x" & Hex(IDENT_DATA(1)))
        End If
        Return IDENT_DATA
    End Function

    Public Function EPROM_BlankCheck() As Boolean
        RaiseEvent PrintConsole("Performing EPROM blank check")
        RaiseEvent SetProgress(0)
        Dim entire_data(CInt(MyFlashDevice.FLASH_SIZE) - 1) As Byte
        Dim BlockCount As Integer = (entire_data.Length \ 8192)
        For i As Integer = 0 To BlockCount - 1
            If AppIsClosing Then Return False
            Dim block() As Byte = ReadBulk(CUInt(i * 8191), 8191)
            Array.Copy(block, 0, entire_data, i * 8191, 8191)
            Dim percent As Single = (i \ BlockCount) * 100
            RaiseEvent SetProgress(CInt(Math.Floor(percent)))
        Next
        If Utilities.IsByteArrayFilled(entire_data, 255) Then
            RaiseEvent PrintConsole("EPROM device is blank and can be programmed")
            DirectCast(MyFlashDevice, OTP_EPROM).IS_BLANK = True
            Return True
        Else
            RaiseEvent PrintConsole("EPROM device is not blank")
            DirectCast(MyFlashDevice, OTP_EPROM).IS_BLANK = False
            Return False
        End If
    End Function

    Private Sub HardwareControl(cmd As FCUSB_HW_CTRL)
        FCUSB.USB_CONTROL_MSG_OUT(USBREQ.EXPIO_CTRL, Nothing, cmd)
    End Sub

    Private Function GetSetupPacket(Address As UInt32, Count As UInt32, PageSize As UInt16) As Byte()
        Dim data_in(19) As Byte '18 bytes total
        data_in(0) = CByte(Address And 255)
        data_in(1) = CByte((Address >> 8) And 255)
        data_in(2) = CByte((Address >> 16) And 255)
        data_in(3) = CByte((Address >> 24) And 255)
        data_in(4) = CByte(Count And 255)
        data_in(5) = CByte((Count >> 8) And 255)
        data_in(6) = CByte((Count >> 16) And 255)
        data_in(7) = CByte((Count >> 24) And 255)
        data_in(8) = CByte(PageSize And 255) 'This is how many bytes to increment between operations
        data_in(9) = CByte((PageSize >> 8) And 255)
        Return data_in
    End Function

    Private Function ReadBulk(address As UInt32, count As Integer) As Byte()
        Try
            Dim read_count As Integer = count
            Dim addr_offset As Boolean = False
            If (MyAdapter = MEM_PROTOCOL.EPROM_X16) Then
                If (address Mod 2 = 1) Then
                    addr_offset = True
                    address = (address - 1UI)
                    read_count += 1
                End If
                If (read_count Mod 2 = 1) Then
                    read_count += 1
                End If
            End If
            Dim data_out(read_count - 1) As Byte 'Bytes we want to read
            Dim page_size As UInt16 = 512
            If MyFlashDevice IsNot Nothing Then page_size = MyFlashDevice.PAGE_SIZE
            Dim setup_data() As Byte = GetSetupPacket(address, CUInt(read_count), page_size)
            Dim result As Boolean = FCUSB.USB_SETUP_BULKIN(USBREQ.EXPIO_READDATA, setup_data, data_out, 0)
            If Not result Then Return Nothing
            If addr_offset Then
                Dim new_data(count - 1) As Byte
                Array.Copy(data_out, 1, new_data, 0, new_data.Length)
                data_out = new_data
            Else
                ReDim Preserve data_out(count - 1)
            End If
            Return data_out
        Catch ex As Exception
        End Try
        Return Nothing
    End Function

    Private Function WriteBulk(address As UInt32, data_out() As Byte) As Boolean
        Try
            Dim setup_data() As Byte = GetSetupPacket(address, CUInt(data_out.Length), MyFlashDevice.PAGE_SIZE)
            Dim result As Boolean = FCUSB.USB_SETUP_BULKOUT(USBREQ.EXPIO_WRITEDATA, setup_data, data_out, 0)
            Return result
        Catch ex As Exception
        End Try
        Return False
    End Function

    Private Enum E_DEV_MODE As UInt16
        EPROM_X8 = 7 '8-BIT EPROM DEVICE
        EPROM_X16 = 8 '16-BIT EPROM DEVICE
    End Enum

    Public Function GetUsbDevice() As USB.FCUSB_DEVICE Implements MemoryDeviceUSB.GetUsbDevice
        Return Me.FCUSB
    End Function

End Class

