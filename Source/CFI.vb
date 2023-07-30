'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2021 - ALL RIGHTS RESERVED
'CONTACT EMAIL: support@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: This class interfaces the CFI flashes (over JTAG) via FlashcatUSB hardware/firmware
Namespace CFI

    Public Class FLASH_INTERFACE
        Public Property FlashSize As UInt32 'Size in number of bytes
        Public Property FlashName As String 'Contains the ascii name of the flash IC
        Public Property USE_BULK As Boolean = True  'Set to false to manually read each word

#Region "Sector / Addresses"
        Private Flash_BlockCount As UShort 'Number of blocks
        Private Flash_EraseBlock() As Int32 'Number of erase sectors per block
        Private Flash_EraseSize() As UInt32 'Size of sectors per block
        Private Flash_Address() As UInt32 'Addresses of all sectors
        Private Flash_Supported As Boolean 'Indicates that we support the device for writing

        Private Sub InitSectorAddresses()
            Dim i As Integer
            Dim AllSects() As Integer = GetAllSectors()
            Dim SectorInt As Integer = AllSects.Length
            Dim SecAdd As UInt32 = 0
            ReDim Flash_Address(SectorInt - 1)
            For i = 0 To SectorInt - 1
                Flash_Address(i) = SecAdd
                SecAdd += CUInt(AllSects(i))
            Next
        End Sub
        'Returns the base address given the sector
        Public Function FindSectorBase(sector As Integer) As UInt32
            Try
                Return CUInt(Flash_Address(sector))
            Catch ex As Exception
                Return 0
            End Try
        End Function
        'Returns the sector that contains the offset address (verified)
        Public Function FindSectorOffset(Offset As UInt32) As Integer
            Dim allSectors() As Integer = GetAllSectors()
            Dim i As Integer
            Dim MinAddress As UInt32 = 0
            Dim MaxAddress As UInt32
            For i = 0 To allSectors.Length - 1
                MaxAddress += CUInt(allSectors(i)) - 1UI
                If Offset >= MinAddress And Offset <= MaxAddress Then Return i 'Found it
                MinAddress = MaxAddress + 1UI
            Next
            Return -1 'Did not find it
        End Function
        'Returns the size (in bytes) of a sector
        Public Function GetSectorSize(Sector As Integer) As Integer
            Dim sectors() As Integer = GetAllSectors()
            If Sector > sectors.Length Then Return 0
            Return sectors(Sector)
        End Function
        'Returns all of the sectors (as their byte sizes)
        Private Function GetAllSectors() As Integer()
            Dim list As New List(Of Integer)
            Dim numSectors As Integer
            For i = 0 To Flash_BlockCount - 1
                numSectors = Flash_EraseBlock(i)
                For x = 0 To numSectors - 1
                    list.Add(CInt(Flash_EraseSize(i)))
                Next
            Next
            Return list.ToArray
        End Function
        'Returns the total number of sectors
        Public Function GetFlashSectors() As Integer
            Dim TotalSectors As Integer = 0
            For i As Integer = 0 To Flash_BlockCount - 1
                TotalSectors += Flash_EraseBlock(i)
            Next
            Return TotalSectors
        End Function
        'Erases a sector on the flash device (byte mode only)
        Public Sub Sector_Erase(Sector As Integer)
            Try
                MyDeviceBus = DeviceBus.X16
                Dim SA As UInt32 = FindSectorBase(Sector) 'Sector Address
                If MyDeviceMode = DeviceAlgorithm.Intel Or MyDeviceMode = DeviceAlgorithm.Intel_Sharp Then
                    write_command(SA, &H50) 'clear register
                    write_command(SA, &H60) 'Unlock block (just in case)
                    write_command(SA, &HD0) 'Confirm Command
                    Utilities.Sleep(50)
                    write_command(SA, &H20)
                    write_command(SA, &HD0)
                    WaitUntilReady()
                ElseIf MyDeviceMode = DeviceAlgorithm.AMD_Fujitsu Or MyDeviceMode = DeviceAlgorithm.AMD_Fujitsu_Extended Then
                    write_command(&HAAA, &HAA) 'AAA = 0xAA
                    write_command(&H555, &H55) '555 = 0x55
                    write_command(&HAAA, &H80) 'AAA = 0x80
                    write_command(&HAAA, &HAA) 'AAA = 0xAA
                    write_command(&H555, &H55) '555 = 0x55
                    write_command(SA, &H30) 'SA  = 0x30
                    write_command(0, &HF0) 'amd reset cmd
                    amd_erasewait(SA)
                ElseIf MyDeviceMode = DeviceAlgorithm.SST Then
                    write_command(&HAAAA, &HAA)
                    write_command(&H5554, &H55)
                    write_command(&HAAAA, &H80)
                    write_command(&HAAAA, &HAA)
                    write_command(&H5554, &H55)
                    write_command(SA, &H30) 'SA  = 0x30
                    amd_erasewait(SA)
                ElseIf MyDeviceMode = DeviceAlgorithm.AMD_NoBypass Then
                    write_command(&HAAA, &HAA) 'AAA = 0xAA
                    write_command(&H555, &H55) '555 = 0x55
                    write_command(&HAAA, &H80) 'AAA = 0x80
                    write_command(&HAAA, &HAA) 'AAA = 0xAA
                    write_command(&H555, &H55) '555 = 0x55
                    write_command(SA, &H30) 'SA  = 0x30
                    write_command(0, &HF0) 'amd reset cmd
                    amd_erasewait(SA)
                End If
            Catch
            End Try
        End Sub
        'Writes data to a given sector and also swaps bytes (endian for words/halfwords)
        Public Sub WriteSector(Sector As Integer, data() As Byte)
            Dim Addr32 As UInteger = FindSectorBase(Sector)
            WriteData(Addr32, data)
        End Sub
        'Waits until a sector is blank (using the AMD read sector method)
        Private Sub amd_erasewait(SectorOffset As UInt32, Optional AllowTimeout As Boolean = True)
            Try
                Utilities.Sleep(500) 'Incase the data is already blank
                Dim Counter As UInt32 = 0
                Dim mydata As UInt32 = 0
                Do Until mydata = &HFFFFFFFFL
                    Utilities.Sleep(100)
                    mydata = ReadWord(CUInt(MyDeviceBase + SectorOffset))
                    If AllowTimeout Then
                        Counter += 1UI
                        If Counter = 20 Then Exit Do
                    End If
                Loop
            Catch ex As Exception
            End Try
        End Sub

#End Region

        Public Event Memory_Write_B(addr As UInt32, data As Byte)
        Public Event Memory_Write_H(addr As UInt32, data As UInt16)
        Public Event Memory_Write_W(addr As UInt32, data As UInt32)
        Public Event Memory_Read_B(addr As UInt32, ByRef data As Byte)
        Public Event Memory_Read_H(addr As UInt32, ByRef data As UInt16)
        Public Event Memory_Read_W(addr As UInt32, ByRef data As UInt32)
        Public Event SetBaseAddress(addr As UInt32)
        Public Event ReadFlash(mem_addr As UInt32, ByRef data() As Byte)
        Public Event WriteFlash(mem_addr As UInt32, data_to_write() As Byte, prog_mode As CFI_FLASH_MODE)
        Public Event WriteConsole(message As String)

        Public MyDeviceID As ChipID
        Private MyDeviceMode As DeviceAlgorithm
        Private MyDeviceBus As DeviceBus
        Private MyDeviceInterface As DeviceInterface 'Loaded via CFI
        Private MyDeviceBase As UInt32 'Address of the device

        Sub New()

        End Sub
        'Returns true if the flash device is detected
        Public Function DetectFlash(BaseAddress As UInt32) As Boolean
            Flash_Supported = False
            Me.MyDeviceBase = BaseAddress
            Read_Mode()
            If Enable_CFI_Mode(DeviceBus.X16) OrElse Enable_CFI_Mode(DeviceBus.X8) OrElse Enable_CFI_Mode(DeviceBus.X32) Then
                Load_CFI_Data()
            ElseIf Enable_CFI_Mode_ForSST() Then
                Load_CFI_Data()
            End If
            Read_Mode() 'Puts the flash back into read mode
            If Enable_JEDEC_Mode() Then
                MyDeviceID = New ChipID With {.MFG = 0, .ID1 = 0}
                If MyDeviceMode = DeviceAlgorithm.NotDefined Then 'Possible non-cfi device
                    Dim FirstWord As UInt32 = Readword(MyDeviceBase)
                    FirstWord = Readword(MyDeviceBase) 'Read this twice for some unknown reason
                    MyDeviceID.MFG = CByte(FirstWord And &HFF)
                    MyDeviceID.ID1 = CUShort(FirstWord >> 16)
                    If Not Detect_NonCFI_Device(MyDeviceID.MFG, MyDeviceID.ID1) Then Return False
                    RaiseEvent SetBaseAddress(BaseAddress)
                Else
                    MyDeviceID.MFG = CByte(ReadHalfword(MyDeviceBase) And &HFF) '0x00F0 0x00C2
                    If MyDeviceMode = DeviceAlgorithm.Intel Or MyDeviceMode = DeviceAlgorithm.Intel_Sharp Then
                        MyDeviceID.ID1 = ReadHalfword(CUInt(MyDeviceBase + &H2))
                    ElseIf MyDeviceMode = DeviceAlgorithm.AMD_Fujitsu Or MyDeviceMode = DeviceAlgorithm.AMD_Fujitsu_Extended Then
                        MyDeviceID.ID1 = ReadHalfword(CUInt(MyDeviceBase + &H22)) '0x00E8 should be 0x2249
                    ElseIf MyDeviceMode = DeviceAlgorithm.SST Then
                        MyDeviceID.ID1 = ReadHalfword(CUInt(MyDeviceBase + &H2))
                        RaiseEvent SetBaseAddress(BaseAddress)
                    End If
                    If MyDeviceID.MFG = 1 And MyDeviceID.ID1 = &H227E Then 'Updates the full PartNumber for SPANSION devices
                        Dim cycle_two As Byte = CByte(&HFF And ReadHalfword(CUInt(MyDeviceBase + +&H1C)))
                        Dim cycle_thr As Byte = CByte(&HFF And ReadHalfword(CUInt(MyDeviceBase + +&H1E)))
                        MyDeviceID.ID2 = (CUShort(cycle_two) << 8) + CUShort(cycle_thr)
                    End If
                    Read_Mode() 'Puts the flash back into read mode
                    Dim BaseStr As String = Hex(BaseAddress).PadLeft(8, "0"c)
                    RaiseEvent WriteConsole(String.Format(RM.GetString("cfi_flash_detected"), BaseStr))
                End If
                LoadFlashName()
                RaiseEvent WriteConsole(String.Format(RM.GetString("ext_connected_chipid"), MyDeviceID.ToString))
                RaiseEvent WriteConsole(String.Format(RM.GetString("cfi_flash_base"), "0x" & Hex(MyDeviceBase).PadLeft(8, "0"c)))
                RaiseEvent WriteConsole(String.Format(RM.GetString("cfi_flash_info"), Me.FlashName, Format(Me.FlashSize, "#,###")))
                PrintProgrammingMode() '"Programming mode: etc"
            Else
                RaiseEvent WriteConsole(RM.GetString("cfi_flash_failed"))
                Return False
            End If
            Return True
        End Function

        Private Function Detect_NonCFI_Device(ManufactureID As Byte, PartNumber As UShort) As Boolean
            If ManufactureID = &HC2 And PartNumber = &H22C4 Then 'MX29LV161T
                Me.FlashSize = &H200000
                Flash_BlockCount = 4
                ReDim Flash_EraseBlock(Flash_BlockCount - 1)
                ReDim Flash_EraseSize(Flash_BlockCount - 1)
                Flash_EraseSize(0) = &H10000 '64KB
                Flash_EraseBlock(0) = 31
                Flash_EraseSize(1) = &H8000 '32KB
                Flash_EraseBlock(1) = 1
                Flash_EraseSize(2) = &H2000 '8KB
                Flash_EraseBlock(2) = 2
                Flash_EraseSize(3) = &H4000 '16KB
                Flash_EraseBlock(3) = 1
                InitSectorAddresses()
                MyDeviceMode = DeviceAlgorithm.AMD_NoBypass
            ElseIf ManufactureID = &HC2 And PartNumber = &H2249 Then 'MX29LV161B
                Me.FlashSize = &H200000
                Flash_BlockCount = 4
                ReDim Flash_EraseBlock(Flash_BlockCount - 1)
                ReDim Flash_EraseSize(Flash_BlockCount - 1)
                Flash_EraseSize(0) = &H4000 '16KB
                Flash_EraseBlock(0) = 1
                Flash_EraseSize(1) = &H2000 '8KB
                Flash_EraseBlock(1) = 2
                Flash_EraseSize(2) = &H8000 '32KB
                Flash_EraseBlock(2) = 1
                Flash_EraseSize(3) = &H10000 '64KB
                Flash_EraseBlock(3) = 31
                InitSectorAddresses()
                MyDeviceMode = DeviceAlgorithm.AMD_NoBypass
            ElseIf ManufactureID = &HC2 And PartNumber = &H22DA Then 'MX29LV800T
                Me.FlashSize = &H100000
                Flash_BlockCount = 4
                ReDim Flash_EraseBlock(Flash_BlockCount - 1)
                ReDim Flash_EraseSize(Flash_BlockCount - 1)
                Flash_EraseSize(0) = &H10000 '64KB
                Flash_EraseBlock(0) = 15
                Flash_EraseSize(1) = &H8000 '32KB
                Flash_EraseBlock(1) = 1
                Flash_EraseSize(2) = &H2000 '8KB
                Flash_EraseBlock(2) = 2
                Flash_EraseSize(3) = &H4000 '16KB
                Flash_EraseBlock(3) = 1
                InitSectorAddresses()
                MyDeviceMode = DeviceAlgorithm.AMD_NoBypass
            ElseIf ManufactureID = &HC2 And PartNumber = &H22DA Then 'MX29LV800B
                Me.FlashSize = &H100000
                Flash_BlockCount = 4
                ReDim Flash_EraseBlock(Flash_BlockCount - 1)
                ReDim Flash_EraseSize(Flash_BlockCount - 1)
                Flash_EraseSize(0) = &H4000 '16KB
                Flash_EraseBlock(0) = 1
                Flash_EraseSize(1) = &H2000 '8KB
                Flash_EraseBlock(1) = 2
                Flash_EraseSize(2) = &H8000 '32KB
                Flash_EraseBlock(2) = 1
                Flash_EraseSize(3) = &H10000 '64KB
                Flash_EraseBlock(3) = 15
                InitSectorAddresses()
                MyDeviceMode = DeviceAlgorithm.AMD_NoBypass
            Else
                Read_Mode()
                Return False
            End If
            Read_Mode()
            Return True
        End Function
        'If our device can be programmed by this code
        Public ReadOnly Property WriteAllowed() As Boolean
            Get
                Return Flash_Supported
            End Get
        End Property
        'Loads the device name (if we have it in our database)
        Private Sub LoadFlashName()
            Try
                Dim flash As FlashMemory.Device = FlashDatabase.FindDevice(MyDeviceID.MFG, MyDeviceID.ID1, MyDeviceID.ID2, FlashMemory.MemoryType.PARALLEL_NOR)
                If flash IsNot Nothing Then
                    Me.FlashName = flash.NAME
                Else
                    Me.FlashName = "(Unknown Name)"
                End If
            Catch ex As Exception
                Me.FlashName = "Erorr loading Flash name"
            End Try
        End Sub

        Private Sub PrintProgrammingMode()
            Dim BusWidthString As String = ""
            Dim AlgStr As String = ""
            Dim InterfaceStr As String = ""
            Select Case MyDeviceBus
                Case DeviceBus.X8
                    BusWidthString = String.Format("({0} bit bus)", 8)
                Case DeviceBus.X16
                    BusWidthString = String.Format("({0} bit bus)", 16)
                Case DeviceBus.X32
                    BusWidthString = String.Format("({0} bit bus)", 32)
                Case Else
                    Exit Sub
            End Select
            Select Case MyDeviceMode
                Case DeviceAlgorithm.AMD_Fujitsu
                    AlgStr = "AMD/Fujitsu"
                Case DeviceAlgorithm.AMD_Fujitsu_Extended
                    AlgStr = "AMD/Fujitsu (extended)"
                Case DeviceAlgorithm.Intel
                    AlgStr = "Intel"
                Case DeviceAlgorithm.Intel_Sharp
                    AlgStr = "Intel/Sharp"
                Case DeviceAlgorithm.SST
                    AlgStr = "SST"
                Case Else
                    Exit Sub
            End Select
            Select Case MyDeviceInterface
                Case DeviceInterface.x8_only
                    InterfaceStr = "x8 interface"
                Case DeviceInterface.x8_and_x16
                    InterfaceStr = "x8/x16 interface"
                Case DeviceInterface.x16_only
                    InterfaceStr = "x16 interface"
                Case DeviceInterface.x32
                    InterfaceStr = "x32 interface"
            End Select
            RaiseEvent WriteConsole("Programming mode: " & AlgStr & " " & InterfaceStr & " " & BusWidthString)
        End Sub

        Private Sub write_command(offset As UInt32, data As UInt32)
            Select Case MyDeviceBus
                Case DeviceBus.X8
                    RaiseEvent Memory_Write_B(CUInt(MyDeviceBase + offset), CByte(data And &HFFUI))
                Case DeviceBus.X16
                    RaiseEvent Memory_Write_H(CUInt(MyDeviceBase + offset), CUShort(data And &HFFFFUI))
                Case DeviceBus.X32
                    RaiseEvent Memory_Write_W(CUInt(MyDeviceBase + offset), data)
            End Select
        End Sub
        'Attempts to put the device into CFI mode
        Private Function Enable_CFI_Mode(BusMode As DeviceBus) As Boolean
            Select Case BusMode
                Case DeviceBus.X8
                    RaiseEvent Memory_Write_B(CUInt(MyDeviceBase + &HAA), &H98) 'CFI Mode Command
                Case DeviceBus.X16
                    RaiseEvent Memory_Write_H(CUInt(MyDeviceBase + &HAA), &H98) 'CFI Mode Command
                Case DeviceBus.X32
                    RaiseEvent Memory_Write_W(CUInt(MyDeviceBase + &HAA), &H98) 'CFI Mode Command 
            End Select
            Utilities.Sleep(50) 'If the command succeded, we need to wait for the device to switch modes
            Dim ReadBack1 As UInt16 = ReadHalfword(MyDeviceBase + &H20UI)
            Dim ReadBack2 As UInt16 = ReadHalfword(MyDeviceBase + &H22UI)
            Dim ReadBack3 As UInt16 = ReadHalfword(MyDeviceBase + &H24UI)
            Dim QRY As UInt32 = (CUInt(ReadBack1 And 255) << 16) Or (CUInt(ReadBack2 And 255) << 8) Or CUInt(ReadBack3 And 255)
            If (QRY = &H515259) Then
                MyDeviceBus = BusMode 'Flash Device Interface description (refer to CFI publication 100)
                Return True
            End If
            Read_Mode()
            Return False
        End Function
        'Attempts to put the device into JEDEC mode
        Private Function Enable_JEDEC_Mode() As Boolean
            If MyDeviceMode = DeviceAlgorithm.NotDefined Then
                write_command(&HAAA, &HAA)
                write_command(&H555, &H55)
                write_command(&HAAA, &H90)
            ElseIf MyDeviceMode = DeviceAlgorithm.Intel Or MyDeviceMode = DeviceAlgorithm.Intel_Sharp Then
                write_command(0, &H90)
            ElseIf MyDeviceMode = DeviceAlgorithm.AMD_Fujitsu Or MyDeviceMode = DeviceAlgorithm.AMD_Fujitsu_Extended Then
                write_command(&HAAA, &HAA) 'HERE
                write_command(&H555, &H55)
                write_command(&HAAA, &H90)
            ElseIf MyDeviceMode = DeviceAlgorithm.AMD_NoBypass Then
                write_command(&HAAA, &HAA)
                write_command(&H555, &H55)
                write_command(&HAAA, &H90)
            ElseIf MyDeviceMode = DeviceAlgorithm.SST Then
                write_command(&HAAAA, &HAA)
                write_command(&H5554, &H55)
                write_command(&HAAAA, &H90)
            Else
                Return False
            End If
            Utilities.Sleep(50)
            Return True
        End Function
        'Puts the device back into READ mode
        Private Function Enable_CFI_Mode_ForSST() As Boolean
            RaiseEvent Memory_Write_B(MyDeviceBase + &HAAAAUI, &HAA)
            RaiseEvent Memory_Write_B(MyDeviceBase + &H5554UI, &H55)
            RaiseEvent Memory_Write_B(MyDeviceBase + &HAAAAUI, &H98)
            Utilities.Sleep(50) 'If the command succeeded, we need to wait for the device to switch modes
            Dim ReadBack As UInt32 = CUInt(ReadHalfword(MyDeviceBase + &H20UI))
            ReadBack = CUInt((ReadBack << 8) + ReadHalfword(MyDeviceBase + &H22UI))
            ReadBack = CUInt((ReadBack << 8) + ReadHalfword(MyDeviceBase + &H24UI))
            If ReadBack = &H515259 Then '"QRY"
                MyDeviceBus = DeviceBus.X8 'Flash Device Interface description (refer to CFI publication 100)
                Return True
            End If
            Read_Mode()
            Return False
        End Function

        Private Function Load_CFI_Data() As Boolean
            Try
                Me.FlashSize = CUInt((2UI ^ ReadHalfword(MyDeviceBase + &H4EUI))) '&H00200000
                Dim DeviceCommandSet As UShort = CUShort(&HFF And ReadHalfword(CUInt(MyDeviceBase + &H26))) << 8 '&H0200
                DeviceCommandSet += (ReadHalfword(CUInt(MyDeviceBase + &H28) And &HFFUS))
                MyDeviceMode = CType(DeviceCommandSet, DeviceAlgorithm)
                MyDeviceInterface = CType(ReadHalfword(MyDeviceBase + &H50UI), DeviceInterface) '0x02
                Flash_BlockCount = ReadHalfword(CUInt(MyDeviceBase + &H58)) '0x04
                Dim BootFlag As UInt32 = ReadHalfword(CUInt(MyDeviceBase + &H9E)) '&H0000FFFF
                ReDim Flash_EraseBlock(Flash_BlockCount - 1)
                ReDim Flash_EraseSize(Flash_BlockCount - 1)
                Dim BlockAddress As UInt32 = &H5A 'Start address of block 1 information
                For i = 1 To Flash_BlockCount
                    Flash_EraseBlock(i - 1) = (ReadHalfword(CUInt(MyDeviceBase + BlockAddress + 2)) << 8) + ReadHalfword(CUInt(MyDeviceBase + BlockAddress)) + 1
                    Flash_EraseSize(i - 1) = (ReadHalfword(CUInt(MyDeviceBase + BlockAddress + 6)) << 8) + ReadHalfword(CUInt(MyDeviceBase + BlockAddress + 4)) * 256UI
                    BlockAddress += 8UI 'Increase address by 8
                Next
                If BootFlag = 3 Then 'warning: might only be designed for TC58FVT160
                    Array.Reverse(Flash_EraseBlock)
                    Array.Reverse(Flash_EraseSize)
                End If
                InitSectorAddresses() 'Creates the map of the addresses of all sectors
                Return True
            Catch ex As Exception
            End Try
            Return False
        End Function

        Public Sub Read_Mode()
            If MyDeviceMode = DeviceAlgorithm.NotDefined Then
                RaiseEvent Memory_Write_B(MyDeviceBase, &HFF) 'For Intel / Sharp
                RaiseEvent Memory_Write_B(MyDeviceBase, &H50)
                RaiseEvent Memory_Write_B(MyDeviceBase + &HAAAUI, &HAA) 'For AMD
                RaiseEvent Memory_Write_B(MyDeviceBase + &H555UI, &H55)
                RaiseEvent Memory_Write_B(MyDeviceBase + &HAAAAUI, &HAA) 'For SST
                RaiseEvent Memory_Write_B(MyDeviceBase + &H5554UI, &H55)
                RaiseEvent Memory_Write_B(MyDeviceBase + &HAAAAUI, &HF0)
                RaiseEvent Memory_Write_B(MyDeviceBase, &HF0) 'For LEGACY
            ElseIf MyDeviceMode = DeviceAlgorithm.Intel Or MyDeviceMode = DeviceAlgorithm.Intel_Sharp Then
                write_command(0, &HFF)
                write_command(0, &H50)
            ElseIf MyDeviceMode = DeviceAlgorithm.AMD_Fujitsu Or MyDeviceMode = DeviceAlgorithm.AMD_Fujitsu_Extended Then
                write_command(&HAAA, &HAA) 'second time here
                write_command(&H555, &H55)
                write_command(0, &HF0)
            ElseIf MyDeviceMode = DeviceAlgorithm.AMD_NoBypass Then
                write_command(0, &HF0)
            ElseIf MyDeviceMode = DeviceAlgorithm.SST Then
                write_command(&HAAAA, &HAA)
                write_command(&H5554, &H55)
                write_command(&HAAAA, &HF0)
            End If
            Utilities.Sleep(50)
        End Sub

        Public Sub WaitUntilReady()
            Dim counter As Integer = 0
            Dim sr As UShort
            If MyDeviceMode = DeviceAlgorithm.Intel Or MyDeviceMode = DeviceAlgorithm.Intel_Sharp Then
                Do
                    If counter = 100 Then Exit Sub
                    counter += 1
                    Utilities.Sleep(25)
                    write_command(0, &H70) 'READ SW
                    sr = ReadHalfword(MyDeviceBase)
                    If AppIsClosing Then Exit Sub
                Loop While (Not ((sr >> 7) = 1))
                Read_Mode()
            ElseIf MyDeviceMode = DeviceAlgorithm.AMD_Fujitsu Or MyDeviceMode = DeviceAlgorithm.AMD_Fujitsu_Extended Then
                Utilities.Sleep(100)
            ElseIf MyDeviceMode = DeviceAlgorithm.SST Then
                Utilities.Sleep(100)
            End If
        End Sub
        'Erases all blocks on the CFI device
        Public Function EraseBulk() As Boolean
            RaiseEvent WriteConsole(RM.GetString("mem_erase_device"))
            If MyDeviceMode = DeviceAlgorithm.Intel Or MyDeviceMode = DeviceAlgorithm.Intel_Sharp Then
                Dim secCount As Integer = GetFlashSectors()
                For i = 0 To secCount - 1
                    Sector_Erase(i)
                Next
            ElseIf MyDeviceMode = DeviceAlgorithm.AMD_Fujitsu Or MyDeviceMode = DeviceAlgorithm.AMD_Fujitsu_Extended Then
                write_command(&HAAA, &HAA) 'AAA = 0xAA
                write_command(&H555, &H55) '555 = 0x55
                write_command(&HAAA, &H80) 'AAA = 0x80
                write_command(&HAAA, &HAA) 'AAA = 0xAA
                write_command(&H555, &H55) '555 = 0x55
                write_command(&HAAA, &H10) 'AAA = 0x10
                amd_erasewait(0, False) 'We may want to wait for a very long time (up to a minute)
            ElseIf MyDeviceMode = DeviceAlgorithm.SST Then
                write_command(&HAAAA, &HAA)
                write_command(&H5554, &H55)
                write_command(&HAAAA, &H80)
                write_command(&HAAAA, &HAA)
                write_command(&H5554, &H55)
                write_command(&HAAAA, &H10)
                amd_erasewait(0, False) 'We may want to wait for a very long time (up to a minute)
            ElseIf MyDeviceMode = DeviceAlgorithm.AMD_NoBypass Then
                write_command(&HAAA, &HAA) 'AAA = 0xAA
                write_command(&H555, &H55) '555 = 0x55
                write_command(&HAAA, &H80) 'AAA = 0x80
                write_command(&HAAA, &HAA) 'AAA = 0xAA
                write_command(&H555, &H55) '555 = 0x55
                write_command(&HAAA, &H10) 'AAA = 0x10
                amd_erasewait(0, False) 'We may want to wait for a very long time (up to a minute)
            End If
            Read_Mode()
            RaiseEvent WriteConsole(RM.GetString("mem_erase_device_success"))
            Return True
        End Function

        Public Function ReadData(Offset As UInt32, count As Integer) As Byte()
            Dim DataOut() As Byte = Nothing
            Try
                If USE_BULK Then 'This is significantly faster
                    Dim data(count - 1) As Byte
                    RaiseEvent ReadFlash(MyDeviceBase + Offset, data)
                    Return data
                Else
                    Dim c As Integer = 0
                    ReDim DataOut(count - 1)
                    For i = 0 To (count / 4) - 1
                        Dim word As UInt32 = ReadWord(MyDeviceBase + Offset + CUInt(i * 4))
                        DataOut(c + 3) = CByte((word >> 24) And &HFFUI)
                        DataOut(c + 2) = CByte((word >> 16) And &HFFUI)
                        DataOut(c + 1) = CByte((word >> 8) And &HFFUI)
                        DataOut(c + 0) = CByte((word >> 0) And &HFFUI)
                        c = c + 4
                    Next
                End If
            Catch
            End Try
            Return DataOut
        End Function
        'Sector must be erased prior to writing data
        Private Sub WriteData(Offset As UInt32, data_to_write() As Byte)
            Try
                Utilities.ChangeEndian32_LSB16(data_to_write) 'Might be DeviceAlgorithm specific
                If MyDeviceMode = DeviceAlgorithm.Intel Or MyDeviceMode = DeviceAlgorithm.Intel_Sharp Then
                    If Me.USE_BULK Then
                        RaiseEvent WriteFlash(MyDeviceBase + Offset, data_to_write, CFI_FLASH_MODE.Intel_16)
                    Else
                        For i As Int32 = 0 To (data_to_write.Length - 1) Step 4 'We will write data 4 bytes at a time
                            RaiseEvent Memory_Write_H(MyDeviceBase + Offset + CUInt(i), &H40)
                            RaiseEvent Memory_Write_H(MyDeviceBase + Offset + CUInt(i), (CUShort(data_to_write(i + 1)) << 8) + data_to_write(i + 0))
                            RaiseEvent Memory_Write_H(MyDeviceBase + Offset + CUInt(i) + 2UI, &H40)
                            RaiseEvent Memory_Write_H(MyDeviceBase + Offset + CUInt(i) + 2UI, (CUShort(data_to_write(i + 3)) << 8) + data_to_write(i + 2))
                        Next
                    End If
                    Read_Mode()
                ElseIf MyDeviceMode = DeviceAlgorithm.AMD_Fujitsu Or MyDeviceMode = DeviceAlgorithm.AMD_Fujitsu_Extended Then
                    write_command(&HAAA, &HAA)
                    write_command(&H555, &H55)
                    write_command(&HAAA, &H20)
                    If (Me.USE_BULK) Then 'Our fast method only works for DMA enabled targets
                        RaiseEvent WriteFlash(MyDeviceBase + Offset, data_to_write, CFI_FLASH_MODE.AMD_16)
                    Else
                        For i As Int32 = 0 To (data_to_write.Length - 1) Step 4 'We will write data 4 bytes at a time
                            RaiseEvent Memory_Write_H(MyDeviceBase, &HA0)
                            RaiseEvent Memory_Write_H(MyDeviceBase + Offset + CUInt(i), (CUShort(data_to_write(i + 1)) << 8) + data_to_write(i + 0))
                            RaiseEvent Memory_Write_H(MyDeviceBase, &HA0)
                            RaiseEvent Memory_Write_H(MyDeviceBase + Offset + CUInt(i) + 2UI, (CUShort(data_to_write(i + 3)) << 8) + data_to_write(i + 2))
                        Next
                    End If
                    write_command(0, &H90)
                    write_command(0, &H0)
                ElseIf MyDeviceMode = DeviceAlgorithm.AMD_NoBypass Then
                    If (Me.USE_BULK) Then 'Our fast method only works for DMA enabled targets
                        RaiseEvent WriteFlash(MyDeviceBase + Offset, data_to_write, CFI_FLASH_MODE.NoBypass)
                    Else
                        For i As Int32 = 0 To (data_to_write.Length - 1) Step 4 'We will write data 4 bytes at a time
                            write_command(&HAAA, &HAA)
                            write_command(&H555, &H55)
                            write_command(&HAAA, &HA0)
                            RaiseEvent Memory_Write_H(MyDeviceBase + Offset + CUInt(i), (CUShort(data_to_write(i + 1)) << 8) + data_to_write(i + 0))
                            write_command(&HAAA, &HAA)
                            write_command(&H555, &H55)
                            write_command(&HAAA, &HA0)
                            RaiseEvent Memory_Write_H(MyDeviceBase + Offset + CUInt(i) + 2UI, (CUShort(data_to_write(i + 3)) << 8) + data_to_write(i + 2))
                        Next
                    End If
                ElseIf MyDeviceMode = DeviceAlgorithm.SST Then
                    If (Me.USE_BULK) Then
                        RaiseEvent WriteFlash(MyDeviceBase + Offset, data_to_write, CFI_FLASH_MODE.SST)
                    Else
                        For i As Int32 = 0 To (data_to_write.Length - 1) Step 4 'We will write data 4 bytes at a time
                            write_command(&HAAAA, &HAA)
                            write_command(&H5554, &H55)
                            write_command(&HAAAA, &HA0)
                            RaiseEvent Memory_Write_H(MyDeviceBase + Offset + CUInt(i), (CUShort(data_to_write(i + 1)) << 8) + data_to_write(i + 0))
                            write_command(&HAAAA, &HAA)
                            write_command(&H5554, &H55)
                            write_command(&HAAAA, &HA0)
                            RaiseEvent Memory_Write_H(MyDeviceBase + Offset + CUInt(i) + 2UI, (CUShort(data_to_write(i + 3)) << 8) + data_to_write(i + 2))
                        Next
                    End If
                End If
            Catch
            End Try
        End Sub

        Private Function ReadByte(offset As UInt32) As Byte
            Dim data As Byte
            RaiseEvent Memory_Read_B(offset, data)
            Return data
        End Function

        Private Function ReadWord(offset As UInt32) As UInt32
            Dim data As UInt32
            RaiseEvent Memory_Read_W(offset, data)
            Return data
        End Function

        Private Function ReadHalfword(offset As UInt32) As UInt16
            Dim data As UInt16
            RaiseEvent Memory_Read_H(offset, data)
            Return data
        End Function

    End Class

    Public Enum CFI_FLASH_MODE As Byte
        Intel_16 = USB.USBREQ.JTAG_FLASHWRITE_I16
        AMD_16 = USB.USBREQ.JTAG_FLASHWRITE_A16
        SST = USB.USBREQ.JTAG_FLASHWRITE_SST
        NoBypass = USB.USBREQ.JTAG_FLASHWRITE_AMDNB
    End Enum

    Public Structure ChipID
        Dim MFG As Byte
        Dim ID1 As UInt16 'Contains the most commonly used id
        Dim ID2 As UInt16 'Some chips have a secondary id

        Public Function IsValid() As Boolean
            If MFG = 0 Then Return False
            If MFG = 255 Then Return False
            If ID1 = 0 Then Return False
            If ID1 = &HFFFF Then Return False
            Return True
        End Function

        Public Overrides Function ToString() As String
            Return Hex(MFG).PadLeft(2, CChar("0")) & " " & Hex(ID1).PadLeft(4, CChar("0"))
        End Function

    End Structure
    'The device bus width used to accept commands
    Public Enum DeviceBus
        X8 = 0
        X16 = 1
        X32 = 2
    End Enum
    'The device specific programming / algorithm (set by CFI, 0x26+0x28)
    Public Enum DeviceAlgorithm As UInt16
        NotDefined = 0
        Intel_Sharp = &H100
        SST = &H107
        AMD_Fujitsu = &H200
        Intel = &H300
        AMD_Fujitsu_Extended = &H400
        AMD_NoBypass = &H1001 'We created/specified this mode type
    End Enum

    Public Enum DeviceInterface
        x8_only = 0
        x16_only = 1
        x8_and_x16 = 2 'via BYTE#
        x32 = 3
    End Enum

End Namespace

