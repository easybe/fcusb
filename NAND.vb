'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2015 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'ACKNOWLEDGEMENT: USB driver functionality provided by LibUsbDotNet (sourceforge.net/projects/libusbdotnet) 

Imports LibUsbDotNet
Imports LibUsbDotNet.Info
Imports LibUsbDotNet.Main

Public Class NAND_API : Implements MemoryDeviceUSB
    Event PrintConsole(ByVal msg As String) Implements MemoryDeviceUSB.PrintConsole
    Private ConfigFlashID As UInt32
    Private NandSectorCount As Integer
    Private LibusbDeviceFinder As UsbDeviceFinder = New UsbDeviceFinder(&H16C0, &H5DF)
    Private FCUSB As UsbDevice

    Sub New()

    End Sub

#Region "USB Hardware Flags"

    Private Const NANDREQ_LEDON As Byte = &H10
    Private Const NANDREQ_LEDOFF As Byte = &H11
    Private Const NANDREQ_LEDBLINK As Byte = &H12
    Private Const NANDREQ_ECHO As Byte = &H13
    Private Const NANDREQ_VERSION As Byte = &H14
    Private Const NANDREQ_DATAREAD As Byte = &H20
    Private Const NANDREQ_DATAWRITE As Byte = &H21
    Private Const NANDREQ_DATAINIT As Byte = &H22
    Private Const NANDREQ_DATADEINIT As Byte = &H23
    Private Const NANDREQ_DATASTATUS As Byte = &H24
    Private Const NANDREQ_DATAERASE As Byte = &H25
    Private Const NANDREQ_POWERUP As Byte = &H30
    Private Const NANDREQ_SHUTDOWN As Byte = &H31

#End Region

#Region "USB Hardware Calls"

    Friend Sub LEDOn() Implements MemoryDeviceUSB.LEDOn
        Try
            If FCUSB Is Nothing Then Exit Sub
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, NANDREQ_LEDON, 0, 0, 0)
            FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
        Catch ex As Exception
        End Try
    End Sub

    Friend Sub LEDOff() Implements MemoryDeviceUSB.LedOff
        Try
            If FCUSB Is Nothing Then Exit Sub
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, NANDREQ_LEDOFF, 0, 0, 0)
            FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
        Catch ex As Exception
        End Try
    End Sub

    Friend Sub LEDBlink() Implements MemoryDeviceUSB.LEDBlink
        Try
            If FCUSB Is Nothing Then Exit Sub
            Dim ret As Integer = 0
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.RequestType_Vendor Or UsbCtrlFlags.Recipient_Device)
            Dim usbPacket2 As New UsbSetupPacket(usbflag, NANDREQ_LEDBLINK, 0, 0, 0)
            FCUSB.ControlTransfer(usbPacket2, Nothing, 0, ret)
        Catch ex As Exception
        End Try
    End Sub

    Private Function EchoTest() As Boolean
        Try
            Dim buff As Byte() = New Byte(7) {}
            Dim DirFlag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor)
            Dim usbSetupPacket As New UsbSetupPacket(DirFlag, CByte(NANDREQ_ECHO), &H1234, &H5678, 8)
            Dim ret As Integer
            If FCUSB.ControlTransfer(usbSetupPacket, buff, 8, ret) Then
                If buff(0) <> CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor) Then
                    Return False
                End If
                If buff(1) <> CByte(NANDREQ_ECHO) Then Return False
                If buff(2) <> &H34 Then Return False
                If buff(3) <> &H12 Then Return False
                If buff(4) <> &H78 Then Return False
                If buff(5) <> &H56 Then Return False
                If buff(6) <> &H8 Then Return False
                If buff(7) <> &H0 Then Return False
                Return True
            End If
        Catch ex As Exception
        End Try
        Return False
    End Function
    'Returns the version of our firmware
    Public Function GetAvrVersion() As String
        Try
            Dim usbflag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor)
            Dim usbSetupPacket As New UsbSetupPacket(usbflag, NANDREQ_VERSION, 0, 0, 4)
            Dim ret As Integer
            Dim buff(4) As Byte
            If FCUSB.ControlTransfer(usbSetupPacket, buff, 4, ret) Then
                buff(4) = buff(3)
                buff(3) = buff(2)
                buff(2) = Asc(".")
            End If
            Dim fwstr As String = Utilities.Bytes.ToChrString(buff)
            Return Utilities.StringToSingle(fwstr).ToString
        Catch ex As Exception
            Return ""
        End Try
    End Function

    Friend Function IsConnected() As Boolean Implements MemoryDeviceUSB.IsConnected
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
    Friend Function Connect() As Boolean Implements MemoryDeviceUSB.Connect
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
        If FCUSB Is Nothing Then Return False
        FCUSB.Open()
        Dim wholeUsbDevice As IUsbDevice = TryCast(FCUSB, IUsbDevice)
        If wholeUsbDevice IsNot Nothing Then 'Libusb only
            wholeUsbDevice.SetConfiguration(1)
            wholeUsbDevice.ClaimInterface(0)
        End If
        Return EchoTest()
    End Function
    'If we are already connected, this will disconnect
    Friend Function Disconnect() As Boolean Implements MemoryDeviceUSB.Disconnect
        Try
            If FCUSB IsNot Nothing Then FCUSB.Close()
            MyUSBDeviceID = ""
            FCUSB = Nothing
        Catch ex As Exception
        End Try
        Return True
    End Function


#End Region

    Public Sub WaitUntilReady() Implements MemoryDeviceUSB.WaitUntilReady
        Utilities.Sleep(100)
    End Sub

    Public Function FindPageBase(ByVal PageIndex As Long) As Long Implements MemoryDeviceUSB.Sector_Find
        Return CUInt(GetPageSize() * PageIndex)
    End Function

    Public Function WriteSector(ByVal SectorInd As Integer, ByVal data() As Byte) As Boolean Implements MemoryDeviceUSB.Sector_Write
        Try
            Dim PageIndex As Integer = (SectorInd * 32)
            Dim Addr As UInteger = FindPageBase(PageIndex)
            WriteData(Addr, data)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function EraseSector(ByVal SectorInd As Long) As Boolean Implements MemoryDeviceUSB.Sector_Erase
        Try
            Dim RetCount As Integer = 0
            Dim SectAddr() As Byte = Utilities.Bytes.FromUInt32(SectorInd * 16896, False)
            Array.Reverse(SectAddr)
            Dim DirectionFlag As Byte = CByte(UsbCtrlFlags.RequestType_Vendor)
            Dim usbSetupPacket As New UsbSetupPacket(DirectionFlag, CByte(NANDREQ_DATAERASE), 0, 0, 0)
            FCUSB.ControlTransfer(usbSetupPacket, SectAddr, 0, RetCount)
            GetStatus()
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function GetSectorCount() As Long Implements MemoryDeviceUSB.Sectors_Count
        Return NandSectorCount
    End Function

    Public ReadOnly Property SectorSize(ByVal sector As Integer) As Long Implements MemoryDeviceUSB.SectorSize
        Get
            Return (GetPageSize() * 32) 'Size of each block in bytes
        End Get
    End Property

    Public Function ChipErase() As Boolean Implements MemoryDeviceUSB.ChipErase
        Dim count As Integer = GetPageCount()
        Dim blockcount As Integer = count
        For x = 0 To blockcount - 1
            EraseSector(x)
        Next
        Return True
    End Function

    Public Function DeviceInit() As Boolean Implements MemoryDeviceUSB.DeviceInit
        DataDeinit()
        ConfigFlashID = 0
        Dim s As UInt32 = GetStatus()
        ConfigFlashID = DataInit()
        If ConfigFlashID = 0 Then Return False
        Select Case ConfigFlashID
            Case &H1198010 'Org 16MB
                NandSectorCount = 1024
            Case &H23010 'Jasper 16MB
                NandSectorCount = 1024
            Case &HA3020 'Jasper 256MB
                NandSectorCount = 16384
            Case &HA8A3020 'Jasper 256MB (just added)
                NandSectorCount = 16384
            Case &HAA3020 ' Jasper 512MB
                NandSectorCount = 32768
            Case Else
                WriteConsole("Detect returned: 0x" & Hex(ConfigFlashID).PadLeft(8, "0"))
                Return False
        End Select
        WriteConsole("NAND Device detected, config: 0x" & Hex(ConfigFlashID).PadLeft(8, "0"))
        LEDOn()
        Return True
    End Function

    Public ReadOnly Property DeviceName As String Implements MemoryDeviceUSB.DeviceName
        Get
            Return "NAND Flash (0x" & Hex(ConfigFlashID).PadLeft(8, "0") & ")"
        End Get
    End Property

    Public ReadOnly Property DeviceSize As Long Implements MemoryDeviceUSB.DeviceSize
        Get
            Return (GetPageCount() * GetPageSize())
        End Get
    End Property

    Public Function ReadData(ByVal flash_offset As Long, ByVal data_count As Long) As Byte() Implements MemoryDeviceUSB.ReadData
        Dim SectorSize As UInteger = GetPageSize()
        If data_count <= SectorSize Then
            Return ReadFlash_ByPage(flash_offset, data_count)
        Else
            Return ReadFlash_ByBlock(flash_offset, data_count)
        End If
    End Function

    Public Function WriteData(ByVal flash_offset As Long, ByVal data_to_write() As Byte) As Boolean Implements MemoryDeviceUSB.WriteData
        Dim SectorSize As UInteger = GetPageSize()
        Dim BlockSize As UInteger = SectorSize
        Dim StartSectorAddress As UInteger = Math.Floor(flash_offset \ SectorSize)
        Dim BytesLeft As UInteger = data_to_write.Length
        Dim calc As Single = CSng(BytesLeft) / CSng(BlockSize)
        Dim BlocksToWrite As UInteger = Math.Ceiling(calc)
        For i = 0 To BlocksToWrite - 1
            Dim BytesWritting As UInteger
            If BytesLeft > BlockSize Then BytesWritting = BlockSize Else BytesWritting = BytesLeft
            Dim DataOut(BytesWritting - 1) As Byte
            Array.Copy(data_to_write, i * BlockSize, DataOut, 0, DataOut.Length)
            WriteFlashSector(StartSectorAddress + (i * 32), DataOut)
            BytesLeft -= BytesWritting
        Next
        Return True
    End Function

    Private Sub WritePage(ByVal PageIndex As UInt32, ByVal data() As Byte)
        Dim Addr As UInteger = FindPageBase(PageIndex)
        WriteData(Addr, data)
    End Sub

    Public Function FindSectorBase(ByVal BlockNum As Integer) As UInteger
        Return CUInt(GetPageSize() * (BlockNum * 32))
    End Function

    Public Sub ErasePage(ByVal PageInd As Integer)
        EraseSector(PageInd \ 32)
    End Sub

    Private Function GetPageCount() As UInteger
        Return GetSectorCount() * 32 'Total number of pages
    End Function

    Private Function GetPageSize() As UInteger
        Return 528 'number of bytes in each page
    End Function

    Private Function ReadFlash_ByPage(ByVal Address As Integer, ByVal Count As Integer) As Byte()
        Dim SectorSize As Integer = GetPageSize()
        Dim SectorStart As Integer = Math.Floor(Address \ SectorSize)
        Dim SectorEnd As Integer = Math.Floor((Address + (Count - 1)) \ SectorSize)
        Dim Offset As Integer = (Address - (SectorStart * SectorSize))
        Dim SectorsToRead As Integer = (SectorEnd - SectorStart) + 1
        Dim TotalReadBuff((SectorsToRead * SectorSize) - 1) As Byte
        For i = 0 To SectorsToRead - 1
            Dim b() As Byte = FlashReadSector(SectorStart + i, SectorSize)
            If b Is Nothing Then Return Nothing 'Error
            Array.Copy(b, 0, TotalReadBuff, i * SectorSize, SectorSize)
        Next
        Dim RetOut(Count - 1) As Byte
        Array.Copy(TotalReadBuff, Offset, RetOut, 0, RetOut.Length)
        Return RetOut
    End Function

    Private Function ReadFlash_ByBlock(ByVal Address As Integer, ByVal Count As Integer) As Byte()
        Dim BlockSize As UInteger = SectorSize(0)
        Dim BlockStart As UInteger = Math.Floor(Address \ BlockSize)
        Dim BlockEnd As UInteger = Math.Floor((Address + (Count - 1)) \ BlockSize)
        Dim Offset As UInteger = (Address - (BlockStart * BlockSize))
        Dim BlocksToRead As UInteger = (BlockEnd - BlockStart) + 1
        Dim TotalReadBuff((BlocksToRead * BlockSize) - 1) As Byte
        For i = 0 To BlocksToRead - 1
            Dim b() As Byte = FlashReadSector((BlockStart + i) * 32, BlockSize)
            If b Is Nothing Then Return Nothing 'Error
            Array.Copy(b, 0, TotalReadBuff, i * BlockSize, BlockSize)
        Next
        Dim RetOut(Count - 1) As Byte
        Array.Copy(TotalReadBuff, Offset, RetOut, 0, RetOut.Length)
        Return RetOut
    End Function

    Private Function FlashReadSector(ByVal Sector As UInteger, ByVal Count As UShort) As Byte()
        Dim DirectionFlag As Byte = CByte(UsbCtrlFlags.RequestType_Vendor)
        Dim wIndex As UShort = 0
        Dim usbSetupPacket As New UsbSetupPacket(DirectionFlag, CByte(NANDREQ_DATAREAD), Count, wIndex, CShort(4))
        Dim RetCount As Integer = 0
        Dim SectAddr() As Byte = Utilities.Bytes.FromUInt32(Sector, False)
        Array.Reverse(SectAddr)
        If FCUSB.ControlTransfer(usbSetupPacket, SectAddr, 4, RetCount) Then
            Dim Data() As Byte = ReadEndPoint(ReadEndpointID.Ep02, Count) 'ep2 = 130 0x82
            Dim res As UInt32 = GetStatus()
            Return Data
        End If
        Return Nothing
    End Function

    Private Function WriteFlashSector(ByVal SectorNum As UInteger, ByVal Data() As Byte) As Boolean
        Dim DirectionFlag As Byte = CByte(UsbCtrlFlags.Direction_Out Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor)
        Dim Count As UShort = Data.Length
        Dim wIndex As UShort = 0
        Dim usbSetupPacket As New UsbSetupPacket(DirectionFlag, CByte(NANDREQ_DATAWRITE), Count, wIndex, CShort(4))
        Dim RetCount As Integer = 0
        Dim SectAddr() As Byte = Utilities.Bytes.FromUInt32(SectorNum, False)
        Array.Reverse(SectAddr)
        If FCUSB.ControlTransfer(usbSetupPacket, SectAddr, 4, RetCount) Then
            Dim w As UsbEndpointWriter = FCUSB.OpenEndpointWriter(WriteEndpointID.Ep01, EndpointType.Bulk)
            Dim tlen As Integer = 0
            Dim ec As ErrorCode = w.Write(Data, 0, Count, 1000, tlen)
            If ec <> ErrorCode.None Then
                Return False
            Else
                Return True
            End If
        Else
            Return False
        End If
    End Function

    Private Sub DataDeinit()
        Try
            Dim DirectionFlag As Byte = CByte(UsbCtrlFlags.RequestType_Vendor)
            Dim usbSetupPacket As New UsbSetupPacket(DirectionFlag, CByte(NANDREQ_DATADEINIT), 0, 0, 0)
            Dim ret As Integer = 0
            FCUSB.ControlTransfer(usbSetupPacket, 0, 0, ret)
        Catch ex As Exception
        End Try
    End Sub

    Private Function DataInit() As UInt32
        Dim DirectionFlag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor)
        Dim usbSetupPacket As New UsbSetupPacket(DirectionFlag, CByte(NANDREQ_DATAINIT), 0, 0, 4)
        Dim ret As Integer = 0
        Dim buff(3) As Byte
        If FCUSB.ControlTransfer(usbSetupPacket, buff, 4, ret) Then
            Array.Reverse(buff)
            Return Utilities.Bytes.ToUInteger(buff)
        End If
        Return 0
    End Function

    Private Function GetStatus() As UInt16
        Dim DirectionFlag As Byte = CByte(UsbCtrlFlags.Direction_In Or UsbCtrlFlags.Recipient_Device Or UsbCtrlFlags.RequestType_Vendor)
        Dim usbSetupPacket As New UsbSetupPacket(DirectionFlag, CByte(NANDREQ_DATASTATUS), 0, 0, 2)
        Dim ret As Integer = 0
        Dim buff(1) As Byte
        If FCUSB.ControlTransfer(usbSetupPacket, buff, 2, ret) Then
            Return (CUShort(buff(0)) << 8) + buff(1)
        Else
        End If
        Return &HFFFF
    End Function

    Private Function ReadStatus() As UInt32
        Dim DirectionFlag As Byte = CByte(UsbCtrlFlags.RequestType_Vendor)
        Dim usbSetupPacket As New UsbSetupPacket(DirectionFlag, CByte(NANDREQ_DATASTATUS), 0, 0, 0)
        Dim ret As Integer = 0
        If FCUSB.ControlTransfer(usbSetupPacket, 0, 0, ret) Then
            Dim Status() As Byte = ReadEndPoint(ReadEndpointID.Ep02, 4)
            If Status IsNot Nothing AndAlso Status.Length = 4 Then Return Utilities.Bytes.ToUInteger(Status)
        Else
        End If
        Return 0
    End Function

    Private Function ReadEndPoint(ByVal EPNUM As ReadEndpointID, ByVal ByteCount As Integer) As Byte()
        Dim buffer(ByteCount - 1) As Byte
        Dim TranCount As Integer = 0
        'Dim reader As UsbEndpointReader = fcusb.OpenEndpointReader(EPNUM, ByteCount, EndpointType.Bulk)
        Dim reader As UsbEndpointReader = FCUSB.OpenEndpointReader(EPNUM)
        reader.Read(buffer, 0, ByteCount, 1000, TranCount)
        Return buffer
    End Function

    Private Function WriteEndPoint(ByVal EPNUM As WriteEndpointID, ByVal Data() As Byte) As Boolean
        Dim xferlen As Integer = 0
        Dim writer As UsbEndpointWriter = FCUSB.OpenEndpointWriter(EPNUM)
        Dim ec As ErrorCode = writer.Write(Data, 0, Data.Length, 2000, xferlen)
        If ec <> ErrorCode.None Then Return False
        Return True
    End Function

    Private Function TargetPowerOn() As Boolean
        Dim DirectionFlag As Byte = CByte(UsbCtrlFlags.RequestType_Vendor)
        Dim usbSetupPacket As New UsbSetupPacket(DirectionFlag, CByte(NANDREQ_POWERUP), 0, 0, 0)
        Dim ret As Integer = 0
        If FCUSB.ControlTransfer(usbSetupPacket, 0, 0, ret) Then
            Return True
        Else
            Return False
        End If
    End Function

    Private Function TargetPowerOff() As Boolean
        Dim DirectionFlag As Byte = CByte(UsbCtrlFlags.RequestType_Vendor)
        Dim usbSetupPacket As New UsbSetupPacket(DirectionFlag, CByte(NANDREQ_SHUTDOWN), 0, 0, 0)
        Dim ret As Integer = 0
        If FCUSB.ControlTransfer(usbSetupPacket, 0, 0, ret) Then
            Return True
        Else
            Return False
        End If
    End Function


End Class
