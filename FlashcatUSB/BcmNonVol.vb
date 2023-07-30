'COPYRIGHT EMBEDDEDCOMPUTERS.NET 2012 - ALL RIGHTS RESERVED
'CONTACT EMAIL: contact@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: This class is used for handeling data structors on bcm nonvol devices 

Imports FlashcatUSB.MemoryInterface

Public Class BcmNonVol
    Public Event WriteConsole(ByVal Msg As String)

    Private HasRead As Boolean = False
    Private NonVolCfg As New Bcm3348NonVolCfg
    Private mem_dev As MemoryDeviceInstance 'This is the hook that allows us to read/write etc.

    Sub New(ByVal flash As MemoryDeviceInstance)
        mem_dev = flash
    End Sub

    Public Property MacAddress() As String
        Get
            If Not HasRead Then ReadConfig()
            If Not HasRead Then Return "00:00:00:00:00:00" 'Could not open nonvol
            Return Utilities.FormatMacStr(NonVolCfg.HFC_MAC)
        End Get
        Set(ByVal value As String)
            If Not HasRead Then ReadConfig()
            NonVolCfg.HFC_MAC = value
        End Set
    End Property

    Public Property Serial() As String
        Get
            If Not HasRead Then ReadConfig()
            Return NonVolCfg.Serial
        End Get
        Set(ByVal value As String)
            NonVolCfg.Serial = value
        End Set
    End Property

    Public Function ReadConfig() As Boolean
        RaiseEvent WriteConsole("Reading Non-Vol configuration data")
        Dim CfgArea As UInt32 = GetCfgArea()
        If CfgArea = 0 Then
            RaiseEvent WriteConsole("Error: No CFG area Found!") : Return False
        End If
        Dim b() As Byte = GetCfg(CfgArea)
        If b Is Nothing Then Return False
        NonVolCfg.LoadConfig(b)
        If Not NonVolCfg.isLoaded Then '"NonVol Config Error"
            RaiseEvent WriteConsole("Error loading Non-Vol data") : Return False
        End If
        HasRead = True
        RaiseEvent WriteConsole("Data successfully read and parsed")
        Return True
    End Function

    Public Sub WriteConfig()
        Try
            RaiseEvent WriteConsole("Writing Non-Vol configuration to Flash")
            mem_dev.WriteBytes(GetCfgArea, NonVolCfg.GetConfig, FlashMemory.FlashArea.Main)
            RaiseEvent WriteConsole("Done. Non-Vol config updated")
        Catch ex As Exception
        End Try
    End Sub

    Public Function GetFirmwareStart() As UInt32
        Return GetEndAdr() + 1
    End Function

    Public Function GetFirmwareName() As String
        Try
            Dim HeaderLocation As UInt32 = CUInt(GetEndAdr() + 1)
            Dim header() As Byte = mem_dev.ReadBytes(HeaderLocation, 92, FlashMemory.FlashArea.Main)
            Dim HeaderCls As New BcmHeaderClass(header)
            Return HeaderCls.FwName
        Catch ex As Exception
            Return "(Not found)"
        End Try
    End Function

    Public Function GetFirmwareLen() As UInt32
        Dim header() As Byte = mem_dev.ReadBytes(CUInt(GetEndAdr() + 1), 92, FlashMemory.FlashArea.Main)
        Dim HeaderCls As New BcmHeaderClass(header)
        Return HeaderCls.FwLen
    End Function

    Private Function GetCfg(ByVal Area As UInt32) As Byte()
        Try
            Dim Word() As Byte = mem_dev.ReadBytes(Area, 4, FlashMemory.FlashArea.Main)
            Dim LenStr As String = Utilities.Bytes.ToHexString(Word)
            Dim LenInt As UInt32 = Utilities.HexToInt(LenStr)
            If LenInt < 1 Or LenInt > 16384 Then Return Nothing
            Dim data_out() As Byte = mem_dev.ReadBytes(Area, 92, FlashMemory.FlashArea.Main)
            Return data_out
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Private Function GetCfgArea() As UInt32
        Dim CfgAdr As UInt32
        Dim i As UInt32 = GetEndAdr()
        If (i > 0) Then
            Dim sB() As Byte = mem_dev.ReadBytes(i, 1, FlashMemory.FlashArea.Main)
            Select Case sB(0)
                Case 255 '"FF"
                    CfgAdr = CUInt(i - Utilities.HexToInt("7F35"))  '80CA
                Case 254 '"FE"
                    CfgAdr = CUInt(i - Utilities.HexToInt("7F35"))
                Case 252 '"FC"
                    CfgAdr = CUInt(i - Utilities.HexToInt("3F35")) 'C0CA
                Case Else
                    CfgAdr = CUInt(i - Utilities.HexToInt("7F35"))
            End Select
        Else
            Return 0
        End If
        Return CfgAdr
    End Function
    '"FFFFFFFFFFFFFFFF00400000FCFFFFFF"
    Private Function GetEndAdr() As UInt32
        Dim HexAdr As UInt32 = Utilities.HexToUInt("FFF0")
        Dim flash_data() As Byte = mem_dev.ReadBytes(HexAdr, 16, FlashMemory.FlashArea.Main)
        Dim ByteLine As String = Utilities.Bytes.ToHexString(flash_data)
        Dim i As Integer = InStr(ByteLine, "00004000")
        If i = 0 Then Return 0
        i = CInt((i + 1) / 2)
        Return HexAdr + i + 6
    End Function
    'Returns TRUE if the checksum read is the one in the config
    Private Function CheckConfigChecksum() As Boolean
        Dim CRC As String = NonVolCfg.MyChecksum
        Dim CfgCopy() As Byte = NonVolCfg.ConfigClone
        If CfgCopy Is Nothing Then Return False
        Dim c() As Byte = CfgCopy
        c(4) = 0
        c(5) = 0
        c(6) = 0
        c(7) = 0
        Dim NowCRC As String = Hex(SectionChecksum(c)).PadLeft(8, CChar("0"))
        If CRC = NowCRC Then Return True
        Return False
    End Function

    Private Shared Function SectionChecksum(ByVal section() As Byte) As UInt32
        Dim i As Integer
        Dim sum As UInt32 = 0L
        Dim s As UInt32
        Dim WORDTOT As Integer = CInt((Math.Floor(section.Length / 4)))
        For i = 0 To WORDTOT - 1
            s = CUInt((section(i * 4) * 16777216L))
            s = CUInt(s + (section((i * 4) + 1) * 65536))
            s = CUInt(s + (section((i * 4) + 2) * 256))
            s = s + (section((i * 4) + 3))
            Utilities.AddUInt32(sum, s)
        Next
        Dim TOP As Integer = (WORDTOT * 4)
        Dim LEFT As Integer = section.Length - TOP 'Number of bytes left
        Dim TWOBYTE As UInt32 = 0
        Dim ONEBYTE As UInt32 = 0
        If LEFT = 3 Then
            TWOBYTE = CUInt((section(TOP) * 256) + (section(TOP + 1)))
            ONEBYTE = (section(TOP + 2))
        ElseIf LEFT = 2 Then
            TWOBYTE = CUInt((section(TOP) * 256) + (section(TOP + 1)))
        ElseIf LEFT = 1 Then
            ONEBYTE = section(TOP)
        End If
        TWOBYTE = (TWOBYTE << 8)
        TWOBYTE = (TWOBYTE Or ONEBYTE)
        TWOBYTE = (TWOBYTE << 8)
        Utilities.AddUInt32(sum, TWOBYTE)
        Return CUInt((sum Xor &HFFFFFFFFL))
    End Function

    Public Class Bcm3348NonVolCfg
        Public ConfigClone() As Byte
        Public FatalError As Boolean
        Public MyChecksum As String 'in hex 
        Private RecordNames As ArrayList
        Private RecordData As ArrayList
        Private intRecordCount As Integer
        Public isLoaded As Boolean
        Public Event OnError(ByVal ErrStr As String)
        Public Event Opened()

        Public ReadOnly Property Count() As Integer
            Get
                Return intRecordCount
            End Get
        End Property

        Public Sub Clear()
            FatalError = False
            RecordNames = New ArrayList
            RecordData = New ArrayList
            intRecordCount = 0
            isLoaded = False
        End Sub

        Public Sub LoadConfig(ByVal cfg() As Byte)
            Clear()
            ParseCfg(cfg)
            ConfigClone = CType(cfg.Clone, Byte())
            RaiseEvent Opened()
        End Sub

        Private Sub ParseCfg(ByVal MyCfg() As Byte)
            MyChecksum = Hex(MyCfg(4)).PadLeft(2, CChar("0")) & Hex(MyCfg(5)).PadLeft(2, CChar("0")) &
                Hex(MyCfg(6)).PadLeft(2, CChar("0")) & Hex(MyCfg(7)).PadLeft(2, CChar("0"))
            Dim i As Integer = 8
            Dim LenInt As Integer
            Dim done As Boolean = False
            Dim p() As Byte
            Dim bName() As Byte
            Dim iRecord() As Byte

            Do Until done
                If MyCfg.Length < (i + 1) Then
                    FatalError = True
                    Exit Sub
                End If
                LenInt = ((MyCfg(i) * 256)) + MyCfg(i + 1)
                ReDim p(LenInt - 3)
                If MyCfg.Length < (i + LenInt) Then
                    FatalError = True
                    Exit Sub
                End If
                Array.Copy(MyCfg, i + 2, p, 0, LenInt - 2)
                ReDim bName(3)
                Array.Copy(p, bName, 4)
                RecordNames.Add(GetRecordName(bName))
                ReDim iRecord(p.Length - 5)
                Array.Copy(p, 4, iRecord, 0, iRecord.Length)
                RecordData.Add(iRecord)
                intRecordCount = intRecordCount + 1
                i = i + LenInt
                If i = MyCfg.Length Then done = True
            Loop
            isLoaded = True
        End Sub

        Private Function DeparseCfg() As Byte()
            Dim NewCfg() As Byte = Nothing
            Dim iStr As String
            Dim Fourbytes() As Byte
            Dim i As Integer = 0
            Dim D() As Byte
            For Each iStr In RecordNames
                D = CType(RecordData.Item(i), Byte())
                If iStr.Length = 8 Then
                    Fourbytes = Utilities.Bytes.FromHexString(iStr)
                Else
                    Fourbytes = Utilities.Bytes.FromString(iStr)
                End If
                If Not Fourbytes.Length = 4 Then
                    FatalError = True
                    RaiseEvent OnError("Deparsing Error: Value - " & iStr)
                    Return Nothing
                End If
                Dim d_size As Int16 = D.Length + 6 'Has to fit into 2 bytes
                Dim b1 As Byte = CByte((d_size >> 8) And 255)
                Dim b2 As Byte = CByte(d_size And 255)
                Utilities.ArrayCopy(NewCfg, New Byte() {b1, b2})
                Utilities.ArrayCopy(NewCfg, Fourbytes)
                Utilities.ArrayCopy(NewCfg, D)
                i = i + 1
            Next
            Dim Tlen As UInteger = CUInt(NewCfg.Length + 8)
            Dim MyCfgMem(CInt(Tlen - 1)) As Byte
            Fourbytes = Utilities.Bytes.FromUInt32(Tlen, False)
            Array.Copy(Fourbytes, 0, MyCfgMem, 0, 4)
            Array.Copy(NewCfg, 0, MyCfgMem, 8, NewCfg.Length)
            Fourbytes = Utilities.Bytes.FromHexString(Hex(SectionChecksum(MyCfgMem)).PadLeft(8, CChar("0")))
            Array.Copy(Fourbytes, 0, MyCfgMem, 4, 4)
            Return MyCfgMem
        End Function

        Private Function GetRecordName(ByVal b() As Byte) As String
            Dim retStr As String = ""
            Dim i As Integer
            Dim v As Integer

            For i = 1 To b.Length
                v = b(i - 1)
                If v > 47 And v < 126 Then
                    retStr = retStr & Chr(v)
                ElseIf v = 32 Then
                    retStr = retStr & " "
                Else
                    retStr = retStr & Utilities.Pad(Hex((v)))
                End If
            Next

            Return retStr
        End Function

        Private Function GetRecordData(ByVal RcName As String) As Byte()
            Dim i As Integer
            i = RecordNames.IndexOf(RcName)

            If i > -1 Then
                Return CType(RecordData.Item(i), Byte())
            Else
                RaiseEvent OnError("Get Record Failed: " & RcName)
            End If
            Return Nothing
        End Function

        Private Sub SetRecordData(ByVal RcName As String, ByVal NewBytes() As Byte)

            Dim i As Integer
            i = RecordNames.IndexOf(RcName)

            If i > -1 Then
                RecordData.Item(i) = NewBytes
            Else
                RaiseEvent OnError("Set Record Failed: " & RcName)
            End If

        End Sub

        Private Function ReadHFCMac() As String
            Dim b() As Byte = GetRecordData("F2A1F61F")
            Dim i As Integer
            Dim MacOut As String = ""
            For i = 6 To 11
                MacOut = MacOut & Utilities.Pad(Hex((b(i))))
            Next
            Return MacOut
        End Function

        Private Function ReadETHMac() As String
            Dim b() As Byte = GetRecordData("F2A1F61F")
            Dim i As Integer
            Dim MacOut As String = ""
            For i = 12 To 17
                MacOut = MacOut & Utilities.Pad(Hex((b(i))))
            Next
            Return MacOut
        End Function

        Private Function ReadSerial() As String
            Dim b() As Byte = GetRecordData("snmp")
            Dim i As Integer
            Dim One As Int32
            Dim outStr As String = ""
            For i = 1 To 127
                One = b(1059 + i)
                If IsNumeric(Chr(One)) Then
                    outStr = outStr & Chr(One)
                Else
                    Exit For
                End If
            Next
            Return outStr
        End Function

        Private Function GetFactoryMIB() As Boolean
            Dim b() As Byte = GetRecordData("snmp")
            If b(2) = 0 Then
                Return False
            ElseIf b(2) = 1 Then
                Return True
            Else
                RaiseEvent OnError("Factory Get Failed, Value: " & b(2))
            End If
            Return False
        End Function

        Private Sub SetFactoryMIB(ByVal V As Boolean)
            Dim b() As Byte = GetRecordData("snmp")
            If V Then
                b(2) = 1
            Else
                b(2) = 0
            End If
            SetRecordData("snmp", b)
        End Sub

        Private Sub SetSerial(ByVal Ser As String)
            Dim r() As Byte = GetRecordData("snmp")
            If Ser.Length < 128 Then
                Dim i As Integer
                For i = 1 To Ser.Length
                    r(1059 + i) = CByte(Asc(Mid(Ser, i, 1)))
                Next
                r(1061 + Ser.Length) = 0 'Termination String
            Else
                RaiseEvent OnError("Serial Address is too long")
            End If

            SetRecordData("snmp", r)
        End Sub

        Private Sub SetHFCMac(ByVal NewMac As String)
            Dim b() As Byte = Utilities.Bytes.FromHexString(NewMac.Replace(":", ""))
            Dim r() As Byte = GetRecordData("F2A1F61F")
            Dim i As Integer
            If b.Length = 6 Then
                For i = 6 To 11
                    r(i) = b(i - 6)
                Next
                SetRecordData("F2A1F61F", r)
            Else
                RaiseEvent OnError("HFC MAC ADDRESS IS INVALID")
            End If
        End Sub

        Private Sub SetEthMac(ByVal NewMac As String)
            Dim b() As Byte = Utilities.Bytes.FromHexString(NewMac.Replace(":", ""))
            Dim r() As Byte = GetRecordData("F2A1F61F")
            Dim i As Integer
            If b.Length = 6 Then
                For i = 12 To 17
                    r(i) = b(i - 12)
                Next
                SetRecordData("F2A1F61F", r)
            Else
                RaiseEvent OnError("ETHERNET MAC ADDRESS IS INVALID")
            End If
        End Sub

        Private Function Compare(ByVal a() As Byte, ByVal b() As Byte) As Boolean
            If Not a.Length = a.Length Then Return False
            Dim i As Int16
            For i = 0 To CShort(a.Length - 1)
                If Not a(i) = b(i) Then Return False
            Next
            Return True
        End Function

        Public Function GetConfig() As Byte()
            If isLoaded Then
                Return DeparseCfg()
            End If
            Return Nothing
        End Function

        Public Property HFC_MAC() As String
            Get
                Return ReadHFCMac()
            End Get
            Set(ByVal NewMac As String)
                SetHFCMac(NewMac)
            End Set
        End Property

        Public Property ETH_MAC() As String
            Get
                Return ReadETHMac()
            End Get
            Set(ByVal NewMac As String)
                SetEthMac(NewMac)
            End Set
        End Property

        Public Property Serial() As String
            Get
                Return ReadSerial()
            End Get
            Set(ByVal Value As String)
                SetSerial(Value)
            End Set
        End Property

        Public Property FactoryMIB() As Boolean
            Get
                Return GetFactoryMIB()
            End Get
            Set(ByVal Value As Boolean)
                SetFactoryMIB(Value)
            End Set
        End Property

    End Class

    Public Class BcmHeaderClass

        Private MyHeader() As Byte

        Public Sub New(ByRef b() As Byte)
            MyHeader = b
            If b.Length = 92 Then Init()
        End Sub

        Private bSig(1) As Byte
        Private bCntr(1) As Byte
        Private majRev(1) As Byte
        Private minRev(1) As Byte
        Private BldTime(3) As Byte
        Private Fsize(3) As Byte
        Private La(3) As Byte
        Private FName(63) As Byte
        Private fHcs(3) As Byte
        Private fCrc(3) As Byte

        Public Sub Init()
            Array.Copy(MyHeader, 0, bSig, 0, 2) '3348
            Array.Copy(MyHeader, 2, bCntr, 0, 2) '0004
            Array.Copy(MyHeader, 4, majRev, 0, 2) '0002
            Array.Copy(MyHeader, 6, minRev, 0, 2) '0000
            Array.Copy(MyHeader, 8, BldTime, 0, 4) '3DB84E64
            Array.Copy(MyHeader, 12, Fsize, 0, 4) '000E9847
            Array.Copy(MyHeader, 16, La, 0, 4) '80010000
            Array.Copy(MyHeader, 20, FName, 0, 64)
            Array.Copy(MyHeader, 84, fHcs, 0, 4)
            Array.Copy(MyHeader, 88, fHcs, 0, 4)
        End Sub

        Public ReadOnly Property FwLen() As Integer
            Get
                Return Utilities.HexToInt(Utilities.Bytes.ToHexString(Fsize))
            End Get
        End Property

        Public ReadOnly Property FwName() As String
            Get
                Return Utilities.Bytes.ToChrString(FName)
            End Get
        End Property

        Public ReadOnly Property FwCRC() As String
            Get
                Return Utilities.Bytes.ToHexString(fHcs)
            End Get
        End Property

    End Class

End Class
