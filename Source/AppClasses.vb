'COPYRIGHT EMBEDDED COMPUTERS LLC 2023 - ALL RIGHTS RESERVED
'THIS SOFTWARE IS ONLY FOR USE WITH GENUINE FLASHCATUSB PRODUCTS
'CONTACT EMAIL: support@embeddedcomputers.net
'ANY USE OF THIS CODE MUST ADHERE TO THE LICENSE FILE INCLUDED WITH THIS SDK
'INFO: This is the main module that is loaded first.

Imports FlashcatUSB.FlashMemory
Imports FlashcatUSB.MemoryInterface
Imports FlashcatUSB.ECC_LIB

Public Class WriteParameters
    Public Index As Integer = 0 'Used for multiple write operations
    Public Address As Long = 0 'flash address to write to
    Public BytesLeft As Long = 0 'Number of bytes to write from this stream
    Public BytesWritten As Long = 0 'Number of bytes we have written
    Public BytesTotal As Long = 0 'The total number of bytes to write
    Public Status As New MemoryDeviceInstance.StatusCallback 'Contains all the delegates (if connected)
    Public Timer As Stopwatch 'To monitor the transfer speed
    'Write Specific Parameters:
    Public EraseSector As Boolean = True  'True if we want to erase each sector prior to write
    Public Verify As Boolean = True 'True if we want to read back the data
    Public AbortOperation As Boolean = False
End Class

Public Class ReadParameters
    Public Index As Integer = 0 'Used for multiple read operations
    Public Address As Long = 0
    Public Count As Long = 0
    Public Status As New MemoryDeviceInstance.StatusCallback 'Contains all the delegates (if connected)
    Public Timer As Stopwatch 'To monitor the transfer speed
    Public AbortOperation As Boolean = False
End Class

Public Enum SettingsMode
    FromRegistry
    FromIniFile
End Enum

Public Class FlashcatSettings
    Public Property LanguageName As String
    Public Property VOLT_SELECT As Voltage 'Selects output voltage and level
    Public Property OPERATION_MODE As DeviceMode = DeviceMode.SPI
    Public Property VERIFY_WRITE As Boolean 'Read back written data to compare write was successful
    Public Property RETRY_WRITE_ATTEMPTS As Integer 'Number of times to retry a write operation
    Public Property BIT_ENDIAN As Utilities.BitEndianMode = Utilities.BitEndianMode.BigEndian32 'Mirrors bits (not saved)
    Public Property BIT_SWAP As BitSwapMode = BitSwapMode.None 'Swaps nibbles/bytes/words (not saved)
    Public Property MULTI_CE As Integer '0 (do not use), else A=1<<CE_VALUE
    'SPI Settings
    Public Property SPI_CLOCK_MAX As SPI.SPI_SPEED
    Public Property SQI_CLOCK_MAX As SPI.SQI_SPEED
    Public Property SPI_MODE As SPI.SPI_CLOCK_POLARITY 'MODE=0 
    Public Property SPI_EEPROM As String 'Name of the SERIAL EEPROM
    Public Property SPI_FASTREAD As Boolean
    Public Property SPI_AUTO As Boolean 'Indicates if the software will use common op commands
    Public Property SPI_NAND_DISABLE_ECC As Boolean
    'I2C Settings
    Public Property I2C_ADDRESS As Byte
    Public Property I2C_SPEED As I2C_SPEED_MODE
    Public Property I2C_INDEX As Int32 'The device selected index
    'SWI Settings
    Public Property SWI_ADDRESS As Byte 'Slave Address
    'NAND Settings
    Public Property NAND_Preserve As Boolean = True 'We want to copy SPARE data before erase
    Public Property NAND_Verify As Boolean = False
    Public Property NAND_BadBlockMode As BadBlockMarker 'Indicates how BAD BLOCKS are detected
    Public Property NAND_SkipBadBlock As Boolean = True 'If a block fails to program, skip block and write data to the next block
    Public Property NAND_Layout As NandMemLayout = NandMemLayout.Separated
    Public Property NAND_Speed As NandMemSpeed = NandMemSpeed._20MHz
    Public Property NAND_UseRBx As Boolean = True
    Public Property NAND_CE_SELECT As Integer = 0

    'NAND ECC Settings
    Public Property ECC_FEATURE_ENABLED As Boolean
    'NOR Flash
    Public Property NOR_READ_ACCESS As Integer
    Public Property NOR_WE_PULSE As Integer
    'GENERAL
    Public Property S93_DEVICE As String 'Name of the part number
    Public Property S93_DEVICE_ORG As Integer '0=8-bit,1=16-bit
    Public Property SREC_DATAMODE As Integer  '0=8-bit,1=16-bit
    Public Property PARALLEL_EEPROM As String 'Name of the Parallel EEPROM
    'JTAG
    Public Property JTAG_SPEED As Integer 'JTAG.JTAG_SPEED
    'License
    Public Property LICENSE_KEY As String
    Public Property LICENSED_TO As String
    Public Property LICENSE_EXP As DateTime
    Public Property PLUGIN_DEFAULT_DIR As String

    Private SettingsFile As SettingsIO

    Sub New(load_settings_from As SettingsMode)
        If load_settings_from = SettingsMode.FromRegistry Then
            SettingsFile = New SettingsIO_Registry()
        ElseIf load_settings_from = SettingsMode.FromIniFile Then
            SettingsFile = New SettingsIO_INI()
        End If
        Threading.Thread.CurrentThread.CurrentCulture = Globalization.CultureInfo.CreateSpecificCulture("en-US")
        LoadLanguageSettings(SettingsFile.GetValue("Language", "English"))
        Me.LICENSE_KEY = SettingsFile.GetValue("LICENSE_KEY", "")
        Dim date_str As String = SettingsFile.GetValue("LICENSE_DATE", "01/01/0001")
        Me.LICENSED_TO = SettingsFile.GetValue("LICENSE_NAME", "")
        If date_str.Equals("01/01/0001") OrElse date_str.Equals("1/1/0001") Then
            Me.LICENSE_EXP = New DateTime
        Else
            Me.LICENSE_EXP = DateTime.Parse(date_str)
        End If
        Me.MULTI_CE = SettingsFile.GetValue("MULTI_CE", 5)
        Me.VOLT_SELECT = CType(SettingsFile.GetValue("VOLTAGE", Voltage.V3_3), Voltage)
        Me.OPERATION_MODE = CType(SettingsFile.GetValue("OPERATION", 1), DeviceMode) 'Default is normal
        Me.VERIFY_WRITE = SettingsFile.GetValue("VERIFY", True)
        Me.RETRY_WRITE_ATTEMPTS = SettingsFile.GetValue("VERIFY_COUNT", 2)
        Me.BIT_ENDIAN = Utilities.BitEndianMode.BigEndian32
        Me.BIT_SWAP = BitSwapMode.None
        Me.SPI_CLOCK_MAX = CType(SettingsFile.GetValue("SPI_CLOCK_MAX", CInt(SPI.SPI_SPEED.MHZ_8)), SPI.SPI_SPEED)
        Me.SQI_CLOCK_MAX = CType(SettingsFile.GetValue("SPI_QUAD_SPEED", CInt(SPI.SQI_SPEED.MHZ_10)), SPI.SQI_SPEED)
        Me.SPI_FASTREAD = SettingsFile.GetValue("SPI_FASTREAD", False)
        Me.SPI_MODE = CType(SettingsFile.GetValue("SPI_MODE", SPI.SPI_CLOCK_POLARITY.SPI_MODE_0), SPI.SPI_CLOCK_POLARITY)
        Me.SPI_EEPROM = SettingsFile.GetValue("SPI_EEPROM", "")
        Me.SPI_AUTO = SettingsFile.GetValue("SPI_AUTO", True)
        Me.SPI_NAND_DISABLE_ECC = SettingsFile.GetValue("SPI_NAND_ECC", False)
        Me.I2C_ADDRESS = CByte(SettingsFile.GetValue("I2C_ADDR", CInt(&HA0)))
        Me.I2C_SPEED = CType(SettingsFile.GetValue("I2C_SPEED", I2C_SPEED_MODE._400kHz), I2C_SPEED_MODE)
        Me.I2C_INDEX = SettingsFile.GetValue("I2C_INDEX", -1)
        Me.SWI_ADDRESS = CByte(SettingsFile.GetValue("SWI_ADDR", CInt(&H0)))
        Me.NAND_Preserve = SettingsFile.GetValue("NAND_Preserve", True)
        Me.NAND_Verify = SettingsFile.GetValue("NAND_Verify", False)
        Me.NAND_BadBlockMode = CType(SettingsFile.GetValue("NAND_BadBlockFeature", BadBlockMarker.Disabled), BadBlockMarker)
        Me.NAND_SkipBadBlock = SettingsFile.GetValue("NAND_Mismatch", True)
        Me.NAND_Layout = CType(SettingsFile.GetValue("NAND_Layout", NandMemLayout.Separated), NandMemLayout)
        Me.NAND_Speed = CType(SettingsFile.GetValue("NAND_Speed", NandMemSpeed._20MHz), NandMemSpeed)
        Me.NAND_UseRBx = SettingsFile.GetValue("NAND_UseRBx", True)
        Me.NAND_CE_SELECT = SettingsFile.GetValue("NAND_CE_SELECT", 0)
        Me.ECC_FEATURE_ENABLED = SettingsFile.GetValue("ECC_ENABLED", False)
        Me.S93_DEVICE = SettingsFile.GetValue("S93_DEVICE_NAME", "")
        Me.S93_DEVICE_ORG = SettingsFile.GetValue("S93_ORG", 0)
        Me.SREC_DATAMODE = SettingsFile.GetValue("SREC_ORG", 0)
        Me.PARALLEL_EEPROM = SettingsFile.GetValue("PARALLEL_EEPROM", "")
        Me.JTAG_SPEED = SettingsFile.GetValue("JTAG_FREQ", 2) 'JTAG_SPEED._10MHZ
        Me.NOR_READ_ACCESS = SettingsFile.GetValue("NOR_READ_ACCESS", 200)
        Me.NOR_WE_PULSE = SettingsFile.GetValue("NOR_WE_PULSE", 125)
        Me.PLUGIN_DEFAULT_DIR = SettingsFile.GetValue("PLUGIN_DIR", "C:\")
        Validate_SerialEEPROM()
        Validate_ParallelEEPROM()
        SettingsFile.ECC_Load()
    End Sub

    Public Sub Save()
        SettingsFile.SetValue("LICENSE_KEY", Me.LICENSE_KEY)
        SettingsFile.SetValue("LICENSE_NAME", Me.LICENSED_TO)
        SettingsFile.SetValue("LICENSE_DATE", Me.LICENSE_EXP.ToShortDateString)
        SettingsFile.SetValue("MULTI_CE", Me.MULTI_CE)
        SettingsFile.SetValue("VOLTAGE", Me.VOLT_SELECT)
        SettingsFile.SetValue("OPERATION", Me.OPERATION_MODE)
        SettingsFile.SetValue("VERIFY", Me.VERIFY_WRITE)
        SettingsFile.SetValue("VERIFY_COUNT", Me.RETRY_WRITE_ATTEMPTS)
        SettingsFile.SetValue("ENDIAN", Me.BIT_ENDIAN)
        SettingsFile.SetValue("BITSWAP", Me.BIT_SWAP)
        SettingsFile.SetValue("SPI_CLOCK_MAX", CInt(Me.SPI_CLOCK_MAX))
        SettingsFile.SetValue("SPI_MODE", Me.SPI_MODE)
        SettingsFile.SetValue("SPI_EEPROM", Me.SPI_EEPROM)
        SettingsFile.SetValue("SPI_FASTREAD", Me.SPI_FASTREAD)
        SettingsFile.SetValue("SPI_AUTO", Me.SPI_AUTO)
        SettingsFile.SetValue("SPI_NAND_ECC", Me.SPI_NAND_DISABLE_ECC)
        SettingsFile.SetValue("SPI_QUAD_SPEED", CInt(Me.SQI_CLOCK_MAX))
        SettingsFile.SetValue("I2C_ADDR", CInt(I2C_ADDRESS))
        SettingsFile.SetValue("I2C_SPEED", CInt(I2C_SPEED))
        SettingsFile.SetValue("I2C_INDEX", CInt(I2C_INDEX))
        SettingsFile.SetValue("SWI_ADDR", CInt(SWI_ADDRESS))
        SettingsFile.SetValue("NAND_Preserve", Me.NAND_Preserve)
        SettingsFile.SetValue("NAND_Verify", Me.NAND_Verify)
        SettingsFile.SetValue("NAND_BadBlockFeature", Me.NAND_BadBlockMode)
        SettingsFile.SetValue("NAND_Mismatch", Me.NAND_SkipBadBlock)
        SettingsFile.SetValue("NAND_Layout", Me.NAND_Layout)
        SettingsFile.SetValue("NAND_Speed", Me.NAND_Speed)
        SettingsFile.SetValue("NAND_UseRBx", Me.NAND_UseRBx)
        SettingsFile.SetValue("NAND_CE_SELECT", Me.NAND_CE_SELECT)
        SettingsFile.SetValue("Language", LanguageName)
        SettingsFile.SetValue("ECC_ENABLED", Me.ECC_FEATURE_ENABLED)
        SettingsFile.SetValue("S93_DEVICE_NAME", Me.S93_DEVICE)
        SettingsFile.SetValue("S93_ORG", Me.S93_DEVICE_ORG)
        SettingsFile.SetValue("SREC_ORG", Me.SREC_DATAMODE)
        SettingsFile.SetValue("PARALLEL_EEPROM", Me.PARALLEL_EEPROM)
        SettingsFile.SetValue("JTAG_FREQ", Me.JTAG_SPEED)
        SettingsFile.SetValue("NOR_READ_ACCESS", Me.NOR_READ_ACCESS)
        SettingsFile.SetValue("NOR_WE_PULSE", Me.NOR_WE_PULSE)
        SettingsFile.SetValue("PLUGIN_DIR", Me.PLUGIN_DEFAULT_DIR)
        SettingsFile.ECC_Save()
    End Sub

    Private Sub Validate_SerialEEPROM()
        Try
            Dim EEPROM_FOUND As Boolean = False
            If Not Me.SPI_EEPROM.Equals("") Then
                Dim d() As SPI_NOR = FlashDatabase.GetDevices_SERIAL_EEPROM()
                For Each dev In d
                    If dev.NAME.Equals(Me.SPI_EEPROM) Then
                        EEPROM_FOUND = True
                        Exit For
                    End If
                Next
            End If
            If Not EEPROM_FOUND Then Me.SPI_EEPROM = ""
        Catch ex As Exception
            Me.SPI_EEPROM = ""
        End Try
    End Sub

    Private Sub Validate_ParallelEEPROM()
        Try
            Dim EEPROM_FOUND As Boolean = False
            If Not Me.PARALLEL_EEPROM.Equals("") Then
                Dim d() As P_NOR = FlashDatabase.GetDevices_PARALLEL_EEPROM()
                For Each dev In d
                    If dev.NAME.Equals(Me.PARALLEL_EEPROM) Then
                        EEPROM_FOUND = True
                        Exit For
                    End If
                Next
            End If
            If Not EEPROM_FOUND Then Me.PARALLEL_EEPROM = ""
        Catch ex As Exception
            Me.PARALLEL_EEPROM = ""
        End Try
    End Sub

    Public Shared Function NandMemSpeedToString(speed As NandMemSpeed) As String
        Select Case speed
            Case NandMemSpeed._20MHz
                Return "20MHz"
            Case NandMemSpeed._10MHz
                Return "10MHz"
            Case NandMemSpeed._5MHz
                Return "5MHz"
            Case NandMemSpeed._1MHz
                Return "1MHz"
            Case Else
                Return "ERROR"
        End Select
    End Function

    Private Sub LoadLanguageSettings(language_name As String)
        Me.LanguageName = language_name
        Select Case Me.LanguageName.ToUpper
            Case "ENGLISH"
                RM = My.Resources.english.ResourceManager : LanguageName = "English"
            Case "SPANISH"
                RM = My.Resources.spanish.ResourceManager : LanguageName = "Spanish"
            Case "FRENCH"
                RM = My.Resources.french.ResourceManager : LanguageName = "French"
            Case "PORTUGUESE"
                RM = My.Resources.portuguese.ResourceManager : LanguageName = "Portuguese"
            Case "RUSSIAN"
                RM = My.Resources.russian.ResourceManager : LanguageName = "Russian"
            Case "CHINESE"
                RM = My.Resources.chinese.ResourceManager : LanguageName = "Chinese"
            Case "ITALIAN"
                RM = My.Resources.italian.ResourceManager : LanguageName = "Italian"
            Case "GERMAN"
                RM = My.Resources.german.ResourceManager : LanguageName = "German"
            Case Else
                RM = My.Resources.english.ResourceManager : LanguageName = "English"
        End Select
    End Sub

    Public Shared Function DeviceModetoString(mode As DeviceMode) As String
        Select Case mode
            Case DeviceMode.SPI
                Return "SPI"
            Case DeviceMode.JTAG
                Return "JTAG"
            Case DeviceMode.I2C_EEPROM
                Return "I2C EEPROM"
            Case DeviceMode.SPI_EEPROM
                Return "SPI EEPROM"
            Case DeviceMode.PNOR
                Return "Parallel NOR"
            Case DeviceMode.PNAND
                Return "Parallel NAND"
            Case DeviceMode.ONE_WIRE
                Return "One-Wire"
            Case DeviceMode.SPI_NAND
                Return "SPI NAND"
            Case DeviceMode.EPROM
                Return "EPROM"
            Case DeviceMode.HyperFlash
                Return "HyperFlash"
            Case DeviceMode.Microwire
                Return "Microwire"
            Case DeviceMode.SQI
                Return "QUAD SPI"
            Case DeviceMode.EMMC
                Return "EMMC"
            Case DeviceMode.FWH
                Return "FWH"
            Case DeviceMode.DFU
                Return "DFU"
            Case DeviceMode.P_EEPROM
                Return "Parallel EEPROM"
            Case DeviceMode.Unspecified
                Return "Unspecified"
            Case Else
                Return "Unknown"
        End Select
    End Function

    Public Sub SetPrefferedScript(name As String, id As UInt32)
        SettingsFile.SetValue("SCRIPT_" & id.ToString, name)
    End Sub

    Public Function GetPrefferedScript(id As UInt32) As String
        Return SettingsFile.GetValue("SCRIPT_" & id.ToString, "")
    End Function

End Class

Public Interface SettingsIO
    Sub ECC_Load()
    Function ECC_Save() As Boolean
    Function GetValue(Name As String, DefaultValue As String) As String
    Function GetValue(Name As String, DefaultValue As Boolean) As Boolean
    Function GetValue(Name As String, DefaultValue As Integer) As Integer
    Function GetData(Name As String) As Byte()
    Function SetValue(Name As String, Value As String) As Boolean
    Function SetValue(Name As String, Value As Boolean) As Boolean
    Function SetValue(Name As String, Value As Integer) As Boolean
    Function SetData(Name As String, data() As Byte) As Boolean

End Interface

Public Class SettingsIO_INI
    Implements SettingsIO

    Private ini_dictionary As New Dictionary(Of String, String)
    Private app_ini_file As New IO.FileInfo("app_settings.ini")
    Private ecc_ini_file As New IO.FileInfo("ecc_settings.ini")

    Sub New()
        If app_ini_file.Exists Then
            Using ini_reader As IO.StreamReader = app_ini_file.OpenText
                While Not ini_reader.Peek = -1
                    Dim cfg_line As String = ini_reader.ReadLine().Trim()
                    If Not (cfg_line = "" OrElse cfg_line.StartsWith("#")) Then
                        Dim tab_ind As Integer = cfg_line.IndexOf(ChrW(9)) 'Split by tab char
                        If tab_ind > 0 Then
                            Dim key_name As String = cfg_line.Substring(0, tab_ind)
                            Dim key_value As String = cfg_line.Substring(tab_ind + 1)
                            ini_dictionary.Add(key_name, key_value)
                        End If
                    End If
                End While
            End Using
        End If
    End Sub

    Private Function Save() As Boolean
        Try
            app_ini_file.Delete()
            Using ini_writer As IO.StreamWriter = app_ini_file.CreateText()
                ini_writer.WriteLine("#Application settings file for FlashcatUSB")
                ini_writer.WriteLine("#Format: SETTING_NAME<TAB>SETTING_VALUE")
                For Each name In ini_dictionary.Keys
                    ini_writer.WriteLine(name & ChrW(9) & ini_dictionary(name))
                Next
            End Using
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Sub ECC_Load() Implements SettingsIO.ECC_Load
        If ecc_ini_file.Exists Then
            Dim ecc_settings As New List(Of ECC_LIB.ECC_Configuration_Entry)
            Dim ecc_entry As ECC_LIB.ECC_Configuration_Entry = Nothing
            Using ini_reader As IO.StreamReader = ecc_ini_file.OpenText
                While Not ini_reader.Peek = -1
                    Dim cfg_line As String = ini_reader.ReadLine().Trim()
                    If Not (cfg_line = "" OrElse cfg_line.StartsWith("#")) Then
                        If cfg_line.ToUpper.Equals("START_CONFIGURATION") Then
                            ecc_entry = New ECC_LIB.ECC_Configuration_Entry
                        ElseIf cfg_line.ToUpper.Equals("END_CONFIGURATION") Then
                            ecc_settings.Add(ecc_entry)
                        Else
                            Dim tab_ind As Integer = cfg_line.IndexOf(ChrW(9)) 'Split by tab char
                            If tab_ind > 0 Then
                                Dim key_name As String = cfg_line.Substring(0, tab_ind)
                                Dim key_value As String = cfg_line.Substring(tab_ind + 1)
                                If key_name.ToUpper.Equals("PageSize".ToUpper()) Then
                                    ecc_entry.PageSize = CUShort(key_value)
                                ElseIf key_name.ToUpper.Equals("SpareSize".ToUpper()) Then
                                    ecc_entry.SpareSize = CUShort(key_value)
                                ElseIf key_name.ToUpper.Equals("Algorithm".ToUpper()) Then
                                    If IsNumeric(key_value) Then
                                        ecc_entry.Algorithm = CType(CInt(key_value), ECC_LIB.ecc_algorithum)
                                    Else
                                        If key_value.ToUpper.Equals("HAMMING") Then
                                            ecc_entry.Algorithm = ECC_LIB.ecc_algorithum.hamming
                                        ElseIf key_value.ToUpper.Equals("REEDSOLOMON") Then
                                            ecc_entry.Algorithm = ECC_LIB.ecc_algorithum.reedsolomon
                                        ElseIf key_value.ToUpper.Equals("BHC") Then
                                            ecc_entry.Algorithm = ECC_LIB.ecc_algorithum.bhc
                                        End If
                                    End If
                                ElseIf key_name.ToUpper.Equals("BitError".ToUpper()) Then
                                    ecc_entry.BitError = CByte(key_value)
                                ElseIf key_name.ToUpper.Equals("SymbolSize".ToUpper()) Then
                                    ecc_entry.SymSize = CByte(key_value)
                                ElseIf key_name.ToUpper.Equals("ReverseData".ToUpper()) Then
                                    ecc_entry.ReverseData = CBool(key_value)
                                ElseIf key_name.ToUpper.StartsWith("Region_".ToUpper()) Then
                                    ecc_entry.AddRegion(CUShort(key_value))
                                End If
                            End If
                        End If
                    End If
                End While
                NAND_ECC_CFG = ecc_settings.ToArray()
            End Using
        End If
    End Sub

    Public Function ECC_Save() As Boolean Implements SettingsIO.ECC_Save
        Try
            If ecc_ini_file.Exists Then ecc_ini_file.Delete()
            If NAND_ECC_CFG IsNot Nothing AndAlso NAND_ECC_CFG.Length > 0 Then
                Using ini_writer As IO.StreamWriter = ecc_ini_file.CreateText()
                    ini_writer.WriteLine("#ECC settings file for FlashcatUSB")
                    For i = 0 To NAND_ECC_CFG.Length - 1
                        ini_writer.WriteLine("START_CONFIGURATION")
                        ini_writer.WriteLine("PageSize" & ChrW(9) & NAND_ECC_CFG(i).PageSize.ToString)
                        ini_writer.WriteLine("SpareSize" & ChrW(9) & NAND_ECC_CFG(i).SpareSize.ToString)
                        ini_writer.WriteLine("Algorithm" & ChrW(9) & NAND_ECC_CFG(i).Algorithm.ToString)
                        ini_writer.WriteLine("BitError" & ChrW(9) & NAND_ECC_CFG(i).BitError.ToString)
                        ini_writer.WriteLine("SymbolSize" & ChrW(9) & NAND_ECC_CFG(i).SymSize.ToString)
                        ini_writer.WriteLine("ReverseData" & ChrW(9) & NAND_ECC_CFG(i).ReverseData.ToString)
                        If NAND_ECC_CFG(i).EccRegion IsNot Nothing AndAlso NAND_ECC_CFG(i).EccRegion.Length > 0 Then
                            For x = 1 To NAND_ECC_CFG(i).EccRegion.Length - 1
                                ini_writer.WriteLine("Region_" & x.ToString & ChrW(9) & CInt(NAND_ECC_CFG(i).EccRegion(x)).ToString)
                            Next
                        End If
                        ini_writer.WriteLine("END_CONFIGURATION")
                    Next
                End Using
            End If
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function GetValue(Name As String, DefaultValue As String) As String Implements SettingsIO.GetValue
        If Not ini_dictionary.ContainsKey(Name) Then Return DefaultValue
        Return ini_dictionary(Name)
    End Function

    Public Function GetValue(Name As String, DefaultValue As Boolean) As Boolean Implements SettingsIO.GetValue
        If Not ini_dictionary.ContainsKey(Name) Then Return DefaultValue
        Return Boolean.Parse(ini_dictionary(Name))
    End Function

    Public Function GetValue(Name As String, DefaultValue As Integer) As Integer Implements SettingsIO.GetValue
        If Not ini_dictionary.ContainsKey(Name) Then Return DefaultValue
        Return Int32.Parse(ini_dictionary(Name))
    End Function

    Public Function GetData(Name As String) As Byte() Implements SettingsIO.GetData
        If Not ini_dictionary.ContainsKey(Name) Then Return Nothing
        Dim hex_str As String = ini_dictionary(Name)
        Return Utilities.Bytes.FromHexString(hex_str)
    End Function

    Public Function SetValue(Name As String, Value As String) As Boolean Implements SettingsIO.SetValue
        ini_dictionary(Name) = Value
        Return Save()
    End Function

    Public Function SetValue(Name As String, Value As Boolean) As Boolean Implements SettingsIO.SetValue
        ini_dictionary(Name) = Value.ToString()
        Return Save()
    End Function

    Public Function SetValue(Name As String, Value As Integer) As Boolean Implements SettingsIO.SetValue
        ini_dictionary(Name) = Value.ToString()
        Return Save()
    End Function

    Public Function SetData(Name As String, data() As Byte) As Boolean Implements SettingsIO.SetData
        Dim hex_str As String = Utilities.Bytes.ToHexString(data)
        ini_dictionary(Name) = hex_str
        Return Save()
    End Function

End Class

#If NET6_0 Then
Public Class SettingsIO_Registry
    Implements SettingsIO

    Public Sub ECC_Load() Implements SettingsIO.ECC_Load
        Throw New NotImplementedException()
    End Sub

    Public Function ECC_Save() As Boolean Implements SettingsIO.ECC_Save
        Throw New NotImplementedException()
    End Function

    Public Function GetValue(Name As String, DefaultValue As String) As String Implements SettingsIO.GetValue
        Throw New NotImplementedException()
    End Function

    Public Function GetValue(Name As String, DefaultValue As Boolean) As Boolean Implements SettingsIO.GetValue
        Throw New NotImplementedException()
    End Function

    Public Function GetValue(Name As String, DefaultValue As Integer) As Integer Implements SettingsIO.GetValue
        Throw New NotImplementedException()
    End Function

    Public Function GetData(Name As String) As Byte() Implements SettingsIO.GetData
        Throw New NotImplementedException()
    End Function

    Public Function SetValue(Name As String, Value As String) As Boolean Implements SettingsIO.SetValue
        Throw New NotImplementedException()
    End Function

    Public Function SetValue(Name As String, Value As Boolean) As Boolean Implements SettingsIO.SetValue
        Throw New NotImplementedException()
    End Function

    Public Function SetValue(Name As String, Value As Integer) As Boolean Implements SettingsIO.SetValue
        Throw New NotImplementedException()
    End Function

    Public Function SetData(Name As String, data() As Byte) As Boolean Implements SettingsIO.SetData
        Throw New NotImplementedException()
    End Function
End Class
#Else

Public Class SettingsIO_Registry
    Implements SettingsIO

    Private Const REGKEY As String = "Software\EmbComputers\FlashcatUSB\"

    Sub New()
        Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY, Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree)
        If key Is Nothing Then Microsoft.Win32.Registry.CurrentUser.CreateSubKey(REGKEY)
    End Sub

    Public Sub ECC_Load() Implements SettingsIO.ECC_Load
        Dim root_key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY, Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree)
        If root_key.GetSubKeyNames().Contains("ECC") Then
            Dim ecc_key As Microsoft.Win32.RegistryKey = root_key.OpenSubKey("ECC")
            Dim CFGS() As String = ecc_key.GetSubKeyNames()
            If CFGS IsNot Nothing AndAlso CFGS.Length > 0 Then
                ReDim NAND_ECC_CFG(CFGS.Length - 1)
                For i = 0 To NAND_ECC_CFG.Length - 1
                    Dim sub_key As Microsoft.Win32.RegistryKey = ecc_key.OpenSubKey(CFGS(i))
                    NAND_ECC_CFG(i) = New ECC_LIB.ECC_Configuration_Entry
                    NAND_ECC_CFG(i).PageSize = CUShort(sub_key.GetValue("PageSize"))
                    NAND_ECC_CFG(i).SpareSize = CUShort(sub_key.GetValue("SpareSize"))
                    NAND_ECC_CFG(i).Algorithm = CType(CInt(sub_key.GetValue("Algorithm")), ECC_LIB.ecc_algorithum)
                    NAND_ECC_CFG(i).BitError = CByte(sub_key.GetValue("BitError"))
                    NAND_ECC_CFG(i).SymSize = CByte(sub_key.GetValue("SymSize"))
                    NAND_ECC_CFG(i).ReverseData = CBool(sub_key.GetValue("ReverseData"))
                    Dim n() As String = sub_key.GetValueNames()
                    For Each item In n
                        If item.StartsWith("Region_") Then
                            NAND_ECC_CFG(i).AddRegion(CUShort(sub_key.GetValue(item)))
                        End If
                    Next
                Next
            End If
        End If
    End Sub

    Public Function ECC_Save() As Boolean Implements SettingsIO.ECC_Save
        Try
            Dim root_key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY, Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree)
            If root_key.GetSubKeyNames().Contains("ECC") Then
                root_key.DeleteSubKeyTree("ECC")
            End If
            If NAND_ECC_CFG Is Nothing Then Return False
            Dim ecc_key As Microsoft.Win32.RegistryKey = root_key.CreateSubKey("ECC")
            For i = 1 To NAND_ECC_CFG.Length
                Dim sub_key As Microsoft.Win32.RegistryKey = ecc_key.CreateSubKey("Profile_" & i.ToString)
                sub_key.SetValue("PageSize", CInt(NAND_ECC_CFG(i - 1).PageSize))
                sub_key.SetValue("SpareSize", CInt(NAND_ECC_CFG(i - 1).SpareSize))
                sub_key.SetValue("Algorithm", CInt(NAND_ECC_CFG(i - 1).Algorithm))
                sub_key.SetValue("BitError", CInt(NAND_ECC_CFG(i - 1).BitError))
                sub_key.SetValue("SymSize", CInt(NAND_ECC_CFG(i - 1).SymSize))
                sub_key.SetValue("ReverseData", NAND_ECC_CFG(i - 1).ReverseData.ToString)
                If NAND_ECC_CFG(i - 1).EccRegion IsNot Nothing AndAlso NAND_ECC_CFG(i - 1).EccRegion.Length > 0 Then
                    For x = 1 To NAND_ECC_CFG(i - 1).EccRegion.Length
                        sub_key.SetValue("Region_" & x.ToString, CInt(NAND_ECC_CFG(i - 1).EccRegion(x - 1)))
                    Next
                End If
            Next
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function GetValue(Name As String, DefaultValue As String) As String Implements SettingsIO.GetValue
        Try
            Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY)
            If key Is Nothing Then Return DefaultValue
            Dim o As Object = key.GetValue(Name)
            If o Is Nothing Then Return DefaultValue
            Return CStr(o)
        Catch ex As Exception
            Return DefaultValue
        End Try
    End Function

    Public Function GetValue(Name As String, DefaultValue As Boolean) As Boolean Implements SettingsIO.GetValue
        Try
            Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY)
            If key Is Nothing Then Return DefaultValue
            Dim o As Object = key.GetValue(Name)
            If o Is Nothing Then Return DefaultValue
            Return CBool(o)
        Catch ex As Exception
            Return DefaultValue
        End Try
    End Function

    Public Function GetValue(Name As String, DefaultValue As Integer) As Integer Implements SettingsIO.GetValue
        Try
            Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY)
            If key Is Nothing Then Return DefaultValue
            Dim o As Object = key.GetValue(Name)
            If o Is Nothing Then Return DefaultValue
            Return CInt(o)
        Catch ex As Exception
            Return DefaultValue
        End Try
    End Function

    Public Function GetData(Name As String) As Byte() Implements SettingsIO.GetData
        Try
            Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY)
            If key Is Nothing Then Return Nothing
            Dim o As Object = key.GetValue(Name)
            If o Is Nothing Then Return Nothing
            Return CType(o, Byte())
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Function SetValue(Name As String, Value As String) As Boolean Implements SettingsIO.SetValue
        Try
            Dim permission = Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree
            Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY, permission)
            key.SetValue(Name, Value)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function SetValue(Name As String, Value As Boolean) As Boolean Implements SettingsIO.SetValue
        Try
            Dim permission = Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree
            Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY, permission)
            If key Is Nothing Then
                Microsoft.Win32.Registry.CurrentUser.CreateSubKey(REGKEY)
                key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY, True)
            End If
            key.SetValue(Name, Value)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function SetValue(Name As String, Value As Integer) As Boolean Implements SettingsIO.SetValue
        Try
            Dim permission = Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree
            Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY, permission)
            key.SetValue(Name, Value)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function SetData(Name As String, data() As Byte) As Boolean Implements SettingsIO.SetData
        Try
            Dim permission = Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree
            Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGKEY, permission)
            key.SetValue(Name, data)
            Return True
        Catch ex As Exception
            Return False
        End Try

    End Function

End Class
#End If


