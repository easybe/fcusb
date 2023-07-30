<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MainForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MainForm))
        Me.ContextMenuStrip1 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.MainToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.miDetectDevice = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator1 = New System.Windows.Forms.ToolStripSeparator()
        Me.miRepeatWrite = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_RefreshFlash = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator4 = New System.Windows.Forms.ToolStripSeparator()
        Me.ExitToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.SettingsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.VerifyMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem4 = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_bitswap_none = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_bitswap_4bit = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_bitswap_8bit = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_bitswap_16bit = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem2 = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_bitendian_big = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_bitendian_little_8 = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_bitendian_little_16 = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_bitendian_little_32 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator2 = New System.Windows.Forms.ToolStripSeparator()
        Me.ToolStripMenuItem3 = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_NandBlockManager = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_NandPreserve = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem5 = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_so44_normal = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_so44_12v_write = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator7 = New System.Windows.Forms.ToolStripSeparator()
        Me.miSPIMode = New System.Windows.Forms.ToolStripMenuItem()
        Me.miSQIMode = New System.Windows.Forms.ToolStripMenuItem()
        Me.miI2CMode = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
        Me.miSPINRF24LE1 = New System.Windows.Forms.ToolStripMenuItem()
        Me.minRF24LUIP = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator3 = New System.Windows.Forms.ToolStripSeparator()
        Me.miAT25010A = New System.Windows.Forms.ToolStripMenuItem()
        Me.miAT25020A = New System.Windows.Forms.ToolStripMenuItem()
        Me.miAT25040A = New System.Windows.Forms.ToolStripMenuItem()
        Me.miAT25080 = New System.Windows.Forms.ToolStripMenuItem()
        Me.miAT25160 = New System.Windows.Forms.ToolStripMenuItem()
        Me.miAT25320 = New System.Windows.Forms.ToolStripMenuItem()
        Me.miAT25640 = New System.Windows.Forms.ToolStripMenuItem()
        Me.miAT25128BT = New System.Windows.Forms.ToolStripMenuItem()
        Me.miAT25256B = New System.Windows.Forms.ToolStripMenuItem()
        Me.miAT25512 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator5 = New System.Windows.Forms.ToolStripSeparator()
        Me.miSTM95010 = New System.Windows.Forms.ToolStripMenuItem()
        Me.miSTM95020 = New System.Windows.Forms.ToolStripMenuItem()
        Me.miSTM95040 = New System.Windows.Forms.ToolStripMenuItem()
        Me.miSTM95080 = New System.Windows.Forms.ToolStripMenuItem()
        Me.miSTM95160 = New System.Windows.Forms.ToolStripMenuItem()
        Me.miSTM95320 = New System.Windows.Forms.ToolStripMenuItem()
        Me.miSTM95640 = New System.Windows.Forms.ToolStripMenuItem()
        Me.miSTM95128 = New System.Windows.Forms.ToolStripMenuItem()
        Me.miSTM95256 = New System.Windows.Forms.ToolStripMenuItem()
        Me.miSTM95512 = New System.Windows.Forms.ToolStripMenuItem()
        Me.miSTM95M01 = New System.Windows.Forms.ToolStripMenuItem()
        Me.miSTM95M02 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator6 = New System.Windows.Forms.ToolStripSeparator()
        Me.miM25AA160A = New System.Windows.Forms.ToolStripMenuItem()
        Me.miM25AA160B = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_ext_mode = New System.Windows.Forms.ToolStripMenuItem()
        Me.ScriptToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.CurrentScript_MI = New System.Windows.Forms.ToolStripMenuItem()
        Me.LoadScript_MI = New System.Windows.Forms.ToolStripMenuItem()
        Me.UnloadScript_MI = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_tools_menu = New System.Windows.Forms.ToolStripMenuItem()
        Me.EraseChipToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.miFlashSeperator1 = New System.Windows.Forms.ToolStripSeparator()
        Me.miBytesPerPage_Normal = New System.Windows.Forms.ToolStripMenuItem()
        Me.miBytesPerPage_Extended = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_tools_seperate2 = New System.Windows.Forms.ToolStripSeparator()
        Me.CreateNANDImageToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.WriteNANDImageToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.LanguageToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.EnglishToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ChineseToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.FrenchToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.GermanToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.PortugueseToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.SpanishToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.MyTabs = New System.Windows.Forms.TabControl()
        Me.TabStatus = New System.Windows.Forms.TabPage()
        Me.pb_logo = New System.Windows.Forms.PictureBox()
        Me.TableLayoutPanel2 = New System.Windows.Forms.TableLayoutPanel()
        Me.sm7 = New System.Windows.Forms.Label()
        Me.sm6 = New System.Windows.Forms.Label()
        Me.sm5 = New System.Windows.Forms.Label()
        Me.sm4 = New System.Windows.Forms.Label()
        Me.sm3 = New System.Windows.Forms.Label()
        Me.sm2 = New System.Windows.Forms.Label()
        Me.lblStatus = New System.Windows.Forms.Label()
        Me.sm1 = New System.Windows.Forms.Label()
        Me.TabConsole = New System.Windows.Forms.TabPage()
        Me.cmdSaveLog = New System.Windows.Forms.Button()
        Me.txtInput = New System.Windows.Forms.TextBox()
        Me.ConsoleBox = New System.Windows.Forms.ListBox()
        Me.AvrTab = New System.Windows.Forms.TabPage()
        Me.AvrEditor = New FlashcatUSB.HexEditor()
        Me.lblAvrCrc = New System.Windows.Forms.Label()
        Me.lblAvrRange = New System.Windows.Forms.Label()
        Me.lblAvrFn = New System.Windows.Forms.Label()
        Me.cmdAvrProg = New System.Windows.Forms.Button()
        Me.cmdAvrStart = New System.Windows.Forms.Button()
        Me.cmdAvrLoad = New System.Windows.Forms.Button()
        Me.DfuPbBar = New System.Windows.Forms.ProgressBar()
        Me.SpiTab = New System.Windows.Forms.TabPage()
        Me.cbSPI = New System.Windows.Forms.GroupBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.txtPageSize = New System.Windows.Forms.TextBox()
        Me.cbSpiProgMode = New System.Windows.Forms.ComboBox()
        Me.cbUseEnWS = New System.Windows.Forms.CheckBox()
        Me.txtEnWS = New System.Windows.Forms.TextBox()
        Me.lblSpiProgMode = New System.Windows.Forms.Label()
        Me.lblSpiInfo = New System.Windows.Forms.Label()
        Me.txtChipSize = New System.Windows.Forms.TextBox()
        Me.lblSpiChipSize = New System.Windows.Forms.Label()
        Me.txtChipErase = New System.Windows.Forms.TextBox()
        Me.lblSpiRead = New System.Windows.Forms.Label()
        Me.txtRead = New System.Windows.Forms.TextBox()
        Me.txtWriteStatus = New System.Windows.Forms.TextBox()
        Me.lblSpiSectorErase = New System.Windows.Forms.Label()
        Me.lblSpiWriteStatus = New System.Windows.Forms.Label()
        Me.txtSectorErase = New System.Windows.Forms.TextBox()
        Me.txtReadStatus = New System.Windows.Forms.TextBox()
        Me.lblSpiEraseSize = New System.Windows.Forms.Label()
        Me.lblSpiReadStatus = New System.Windows.Forms.Label()
        Me.txtEraseSize = New System.Windows.Forms.TextBox()
        Me.txtWriteEnable = New System.Windows.Forms.TextBox()
        Me.lblSpiWriteEn = New System.Windows.Forms.Label()
        Me.txtPageProgram = New System.Windows.Forms.TextBox()
        Me.lblSpiChipErase = New System.Windows.Forms.Label()
        Me.lblSpiPageProgram = New System.Windows.Forms.Label()
        Me.RadioUseSpiSettings = New System.Windows.Forms.RadioButton()
        Me.RadioUseSpiAuto = New System.Windows.Forms.RadioButton()
        Me.I2CTab = New System.Windows.Forms.TabPage()
        Me.pbI2C = New System.Windows.Forms.ProgressBar()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.cbI2C_A2 = New System.Windows.Forms.CheckBox()
        Me.cbI2C_A1 = New System.Windows.Forms.CheckBox()
        Me.cbI2C_A0 = New System.Windows.Forms.CheckBox()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.cmdI2cRead = New System.Windows.Forms.Button()
        Me.cmdI2cWrite = New System.Windows.Forms.Button()
        Me.cmdI2cConnect = New System.Windows.Forms.Button()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.cbEepromDevices = New FlashcatUSB.CustomComboPlus.CustomComboPlus()
        Me.I2CEditor = New FlashcatUSB.HexEditor()
        Me.StatusStrip1 = New System.Windows.Forms.StatusStrip()
        Me.Status = New System.Windows.Forms.ToolStripStatusLabel()
        Me.MenuStrip1.SuspendLayout()
        Me.MyTabs.SuspendLayout()
        Me.TabStatus.SuspendLayout()
        CType(Me.pb_logo, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.TableLayoutPanel2.SuspendLayout()
        Me.TabConsole.SuspendLayout()
        Me.AvrTab.SuspendLayout()
        Me.SpiTab.SuspendLayout()
        Me.cbSPI.SuspendLayout()
        Me.I2CTab.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.StatusStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'ContextMenuStrip1
        '
        Me.ContextMenuStrip1.Name = "ContextMenuStrip1"
        Me.ContextMenuStrip1.Size = New System.Drawing.Size(61, 4)
        '
        'MenuStrip1
        '
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.MainToolStripMenuItem, Me.SettingsToolStripMenuItem, Me.ScriptToolStripMenuItem, Me.mi_tools_menu, Me.LanguageToolStripMenuItem})
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Size = New System.Drawing.Size(452, 24)
        Me.MenuStrip1.TabIndex = 1
        Me.MenuStrip1.Text = "MenuStrip1"
        '
        'MainToolStripMenuItem
        '
        Me.MainToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.miDetectDevice, Me.ToolStripSeparator1, Me.miRepeatWrite, Me.mi_RefreshFlash, Me.ToolStripSeparator4, Me.ExitToolStripMenuItem})
        Me.MainToolStripMenuItem.Name = "MainToolStripMenuItem"
        Me.MainToolStripMenuItem.Size = New System.Drawing.Size(46, 20)
        Me.MainToolStripMenuItem.Text = "Main"
        '
        'miDetectDevice
        '
        Me.miDetectDevice.Image = Global.FlashcatUSB.My.Resources.Resources.upgrade
        Me.miDetectDevice.Name = "miDetectDevice"
        Me.miDetectDevice.ShortcutKeys = System.Windows.Forms.Keys.F1
        Me.miDetectDevice.Size = New System.Drawing.Size(212, 22)
        Me.miDetectDevice.Text = "Detect device"
        '
        'ToolStripSeparator1
        '
        Me.ToolStripSeparator1.Name = "ToolStripSeparator1"
        Me.ToolStripSeparator1.Size = New System.Drawing.Size(209, 6)
        '
        'miRepeatWrite
        '
        Me.miRepeatWrite.Image = Global.FlashcatUSB.My.Resources.Resources.repeat
        Me.miRepeatWrite.Name = "miRepeatWrite"
        Me.miRepeatWrite.ShortcutKeys = System.Windows.Forms.Keys.F2
        Me.miRepeatWrite.Size = New System.Drawing.Size(212, 22)
        Me.miRepeatWrite.Text = "Repeat write operation"
        '
        'mi_RefreshFlash
        '
        Me.mi_RefreshFlash.Image = Global.FlashcatUSB.My.Resources.Resources.refresh
        Me.mi_RefreshFlash.Name = "mi_RefreshFlash"
        Me.mi_RefreshFlash.ShortcutKeys = System.Windows.Forms.Keys.F5
        Me.mi_RefreshFlash.Size = New System.Drawing.Size(212, 22)
        Me.mi_RefreshFlash.Text = "Refresh Flash"
        '
        'ToolStripSeparator4
        '
        Me.ToolStripSeparator4.Name = "ToolStripSeparator4"
        Me.ToolStripSeparator4.Size = New System.Drawing.Size(209, 6)
        '
        'ExitToolStripMenuItem
        '
        Me.ExitToolStripMenuItem.Image = Global.FlashcatUSB.My.Resources.Resources.ico_exit
        Me.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem"
        Me.ExitToolStripMenuItem.Size = New System.Drawing.Size(212, 22)
        Me.ExitToolStripMenuItem.Text = "Exit"
        '
        'SettingsToolStripMenuItem
        '
        Me.SettingsToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.VerifyMenuItem, Me.ToolStripMenuItem4, Me.ToolStripMenuItem2, Me.ToolStripSeparator2, Me.ToolStripMenuItem3, Me.ToolStripMenuItem5, Me.ToolStripSeparator7, Me.miSPIMode, Me.miSQIMode, Me.miI2CMode, Me.ToolStripMenuItem1, Me.mi_ext_mode})
        Me.SettingsToolStripMenuItem.Name = "SettingsToolStripMenuItem"
        Me.SettingsToolStripMenuItem.Size = New System.Drawing.Size(61, 20)
        Me.SettingsToolStripMenuItem.Text = "Settings"
        '
        'VerifyMenuItem
        '
        Me.VerifyMenuItem.Name = "VerifyMenuItem"
        Me.VerifyMenuItem.Size = New System.Drawing.Size(183, 22)
        Me.VerifyMenuItem.Text = "Verify programming"
        '
        'ToolStripMenuItem4
        '
        Me.ToolStripMenuItem4.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mi_bitswap_none, Me.mi_bitswap_4bit, Me.mi_bitswap_8bit, Me.mi_bitswap_16bit})
        Me.ToolStripMenuItem4.Image = Global.FlashcatUSB.My.Resources.Resources.binary
        Me.ToolStripMenuItem4.Name = "ToolStripMenuItem4"
        Me.ToolStripMenuItem4.Size = New System.Drawing.Size(183, 22)
        Me.ToolStripMenuItem4.Text = "Bit Swapping"
        '
        'mi_bitswap_none
        '
        Me.mi_bitswap_none.Name = "mi_bitswap_none"
        Me.mi_bitswap_none.Size = New System.Drawing.Size(145, 22)
        Me.mi_bitswap_none.Text = "None"
        '
        'mi_bitswap_4bit
        '
        Me.mi_bitswap_4bit.Name = "mi_bitswap_4bit"
        Me.mi_bitswap_4bit.Size = New System.Drawing.Size(145, 22)
        Me.mi_bitswap_4bit.Text = "4-bit (Nibble)"
        '
        'mi_bitswap_8bit
        '
        Me.mi_bitswap_8bit.Name = "mi_bitswap_8bit"
        Me.mi_bitswap_8bit.Size = New System.Drawing.Size(145, 22)
        Me.mi_bitswap_8bit.Text = "8-bit (Byte)"
        '
        'mi_bitswap_16bit
        '
        Me.mi_bitswap_16bit.Name = "mi_bitswap_16bit"
        Me.mi_bitswap_16bit.Size = New System.Drawing.Size(145, 22)
        Me.mi_bitswap_16bit.Text = "16-bit (Word)"
        '
        'ToolStripMenuItem2
        '
        Me.ToolStripMenuItem2.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mi_bitendian_big, Me.mi_bitendian_little_8, Me.mi_bitendian_little_16, Me.mi_bitendian_little_32})
        Me.ToolStripMenuItem2.Image = Global.FlashcatUSB.My.Resources.Resources.binary
        Me.ToolStripMenuItem2.Name = "ToolStripMenuItem2"
        Me.ToolStripMenuItem2.Size = New System.Drawing.Size(183, 22)
        Me.ToolStripMenuItem2.Text = "Endian Mode"
        '
        'mi_bitendian_big
        '
        Me.mi_bitendian_big.Name = "mi_bitendian_big"
        Me.mi_bitendian_big.Size = New System.Drawing.Size(186, 22)
        Me.mi_bitendian_big.Text = "Big Endian"
        '
        'mi_bitendian_little_8
        '
        Me.mi_bitendian_little_8.Name = "mi_bitendian_little_8"
        Me.mi_bitendian_little_8.Size = New System.Drawing.Size(186, 22)
        Me.mi_bitendian_little_8.Text = "Little Endian (8-bits)"
        '
        'mi_bitendian_little_16
        '
        Me.mi_bitendian_little_16.Name = "mi_bitendian_little_16"
        Me.mi_bitendian_little_16.Size = New System.Drawing.Size(186, 22)
        Me.mi_bitendian_little_16.Text = "Little Endian (16-bits)"
        '
        'mi_bitendian_little_32
        '
        Me.mi_bitendian_little_32.Name = "mi_bitendian_little_32"
        Me.mi_bitendian_little_32.Size = New System.Drawing.Size(186, 22)
        Me.mi_bitendian_little_32.Text = "Little Endian (32-bits)"
        '
        'ToolStripSeparator2
        '
        Me.ToolStripSeparator2.Name = "ToolStripSeparator2"
        Me.ToolStripSeparator2.Size = New System.Drawing.Size(180, 6)
        '
        'ToolStripMenuItem3
        '
        Me.ToolStripMenuItem3.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mi_NandBlockManager, Me.mi_NandPreserve})
        Me.ToolStripMenuItem3.Name = "ToolStripMenuItem3"
        Me.ToolStripMenuItem3.Size = New System.Drawing.Size(183, 22)
        Me.ToolStripMenuItem3.Text = "NAND Flash Settings"
        '
        'mi_NandBlockManager
        '
        Me.mi_NandBlockManager.Name = "mi_NandBlockManager"
        Me.mi_NandBlockManager.Size = New System.Drawing.Size(196, 22)
        Me.mi_NandBlockManager.Text = "Enable Block Manager"
        '
        'mi_NandPreserve
        '
        Me.mi_NandPreserve.Name = "mi_NandPreserve"
        Me.mi_NandPreserve.Size = New System.Drawing.Size(196, 22)
        Me.mi_NandPreserve.Text = "Preserve memory areas"
        '
        'ToolStripMenuItem5
        '
        Me.ToolStripMenuItem5.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mi_so44_normal, Me.mi_so44_12v_write})
        Me.ToolStripMenuItem5.Name = "ToolStripMenuItem5"
        Me.ToolStripMenuItem5.Size = New System.Drawing.Size(183, 22)
        Me.ToolStripMenuItem5.Text = "SO-44 VPP Setting"
        '
        'mi_so44_normal
        '
        Me.mi_so44_normal.Name = "mi_so44_normal"
        Me.mi_so44_normal.Size = New System.Drawing.Size(155, 22)
        Me.mi_so44_normal.Text = "Disabled"
        '
        'mi_so44_12v_write
        '
        Me.mi_so44_12v_write.Name = "mi_so44_12v_write"
        Me.mi_so44_12v_write.Size = New System.Drawing.Size(155, 22)
        Me.mi_so44_12v_write.Text = "12v Erase/Write"
        '
        'ToolStripSeparator7
        '
        Me.ToolStripSeparator7.Name = "ToolStripSeparator7"
        Me.ToolStripSeparator7.Size = New System.Drawing.Size(180, 6)
        '
        'miSPIMode
        '
        Me.miSPIMode.Name = "miSPIMode"
        Me.miSPIMode.Size = New System.Drawing.Size(183, 22)
        Me.miSPIMode.Text = "SPI mode (normal)"
        '
        'miSQIMode
        '
        Me.miSQIMode.Name = "miSQIMode"
        Me.miSQIMode.Size = New System.Drawing.Size(183, 22)
        Me.miSQIMode.Text = "Quad SPI mode"
        Me.miSQIMode.Visible = False
        '
        'miI2CMode
        '
        Me.miI2CMode.Name = "miI2CMode"
        Me.miI2CMode.Size = New System.Drawing.Size(183, 22)
        Me.miI2CMode.Text = "I2C EEPROM mode"
        '
        'ToolStripMenuItem1
        '
        Me.ToolStripMenuItem1.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.miSPINRF24LE1, Me.minRF24LUIP, Me.ToolStripSeparator3, Me.miAT25010A, Me.miAT25020A, Me.miAT25040A, Me.miAT25080, Me.miAT25160, Me.miAT25320, Me.miAT25640, Me.miAT25128BT, Me.miAT25256B, Me.miAT25512, Me.ToolStripSeparator5, Me.miSTM95010, Me.miSTM95020, Me.miSTM95040, Me.miSTM95080, Me.miSTM95160, Me.miSTM95320, Me.miSTM95640, Me.miSTM95128, Me.miSTM95256, Me.miSTM95512, Me.miSTM95M01, Me.miSTM95M02, Me.ToolStripSeparator6, Me.miM25AA160A, Me.miM25AA160B})
        Me.ToolStripMenuItem1.Name = "ToolStripMenuItem1"
        Me.ToolStripMenuItem1.Size = New System.Drawing.Size(183, 22)
        Me.ToolStripMenuItem1.Text = "SPI EEPROM mode"
        '
        'miSPINRF24LE1
        '
        Me.miSPINRF24LE1.Name = "miSPINRF24LE1"
        Me.miSPINRF24LE1.Size = New System.Drawing.Size(267, 22)
        Me.miSPINRF24LE1.Text = "Nordic nRF24LE1"
        '
        'minRF24LUIP
        '
        Me.minRF24LUIP.Name = "minRF24LUIP"
        Me.minRF24LUIP.Size = New System.Drawing.Size(267, 22)
        Me.minRF24LUIP.Text = "Nordic nRF24LUI+"
        '
        'ToolStripSeparator3
        '
        Me.ToolStripSeparator3.Name = "ToolStripSeparator3"
        Me.ToolStripSeparator3.Size = New System.Drawing.Size(264, 6)
        '
        'miAT25010A
        '
        Me.miAT25010A.Name = "miAT25010A"
        Me.miAT25010A.Size = New System.Drawing.Size(267, 22)
        Me.miAT25010A.Text = "Atmel AT25010A"
        '
        'miAT25020A
        '
        Me.miAT25020A.Name = "miAT25020A"
        Me.miAT25020A.Size = New System.Drawing.Size(267, 22)
        Me.miAT25020A.Text = "Atmel AT25020A"
        '
        'miAT25040A
        '
        Me.miAT25040A.Name = "miAT25040A"
        Me.miAT25040A.Size = New System.Drawing.Size(267, 22)
        Me.miAT25040A.Text = "Atmel AT25040A"
        '
        'miAT25080
        '
        Me.miAT25080.Name = "miAT25080"
        Me.miAT25080.Size = New System.Drawing.Size(267, 22)
        Me.miAT25080.Text = "Atmel AT25080"
        '
        'miAT25160
        '
        Me.miAT25160.Name = "miAT25160"
        Me.miAT25160.Size = New System.Drawing.Size(267, 22)
        Me.miAT25160.Text = "Atmel AT25160"
        '
        'miAT25320
        '
        Me.miAT25320.Name = "miAT25320"
        Me.miAT25320.Size = New System.Drawing.Size(267, 22)
        Me.miAT25320.Text = "Atmel AT25320"
        '
        'miAT25640
        '
        Me.miAT25640.Name = "miAT25640"
        Me.miAT25640.Size = New System.Drawing.Size(267, 22)
        Me.miAT25640.Text = "Atmel AT25640"
        '
        'miAT25128BT
        '
        Me.miAT25128BT.Name = "miAT25128BT"
        Me.miAT25128BT.Size = New System.Drawing.Size(267, 22)
        Me.miAT25128BT.Text = "Atmel AT25128B"
        '
        'miAT25256B
        '
        Me.miAT25256B.Name = "miAT25256B"
        Me.miAT25256B.Size = New System.Drawing.Size(267, 22)
        Me.miAT25256B.Text = "Atmel AT25256B"
        '
        'miAT25512
        '
        Me.miAT25512.Name = "miAT25512"
        Me.miAT25512.Size = New System.Drawing.Size(267, 22)
        Me.miAT25512.Text = "Atmel AT25512 / Microchip 25AA512"
        '
        'ToolStripSeparator5
        '
        Me.ToolStripSeparator5.Name = "ToolStripSeparator5"
        Me.ToolStripSeparator5.Size = New System.Drawing.Size(264, 6)
        '
        'miSTM95010
        '
        Me.miSTM95010.Name = "miSTM95010"
        Me.miSTM95010.Size = New System.Drawing.Size(267, 22)
        Me.miSTM95010.Text = "ST M95010"
        '
        'miSTM95020
        '
        Me.miSTM95020.Name = "miSTM95020"
        Me.miSTM95020.Size = New System.Drawing.Size(267, 22)
        Me.miSTM95020.Text = "ST M95020"
        '
        'miSTM95040
        '
        Me.miSTM95040.Name = "miSTM95040"
        Me.miSTM95040.Size = New System.Drawing.Size(267, 22)
        Me.miSTM95040.Text = "ST M95040"
        '
        'miSTM95080
        '
        Me.miSTM95080.Name = "miSTM95080"
        Me.miSTM95080.Size = New System.Drawing.Size(267, 22)
        Me.miSTM95080.Text = "ST M95080"
        '
        'miSTM95160
        '
        Me.miSTM95160.Name = "miSTM95160"
        Me.miSTM95160.Size = New System.Drawing.Size(267, 22)
        Me.miSTM95160.Text = "ST M95160"
        '
        'miSTM95320
        '
        Me.miSTM95320.Name = "miSTM95320"
        Me.miSTM95320.Size = New System.Drawing.Size(267, 22)
        Me.miSTM95320.Text = "ST M95320"
        '
        'miSTM95640
        '
        Me.miSTM95640.Name = "miSTM95640"
        Me.miSTM95640.Size = New System.Drawing.Size(267, 22)
        Me.miSTM95640.Text = "ST M95640"
        '
        'miSTM95128
        '
        Me.miSTM95128.Name = "miSTM95128"
        Me.miSTM95128.Size = New System.Drawing.Size(267, 22)
        Me.miSTM95128.Text = "ST M95128"
        '
        'miSTM95256
        '
        Me.miSTM95256.Name = "miSTM95256"
        Me.miSTM95256.Size = New System.Drawing.Size(267, 22)
        Me.miSTM95256.Text = "ST M95256"
        '
        'miSTM95512
        '
        Me.miSTM95512.Name = "miSTM95512"
        Me.miSTM95512.Size = New System.Drawing.Size(267, 22)
        Me.miSTM95512.Text = "ST M95512"
        '
        'miSTM95M01
        '
        Me.miSTM95M01.Name = "miSTM95M01"
        Me.miSTM95M01.Size = New System.Drawing.Size(267, 22)
        Me.miSTM95M01.Text = "ST M95M01"
        '
        'miSTM95M02
        '
        Me.miSTM95M02.Name = "miSTM95M02"
        Me.miSTM95M02.Size = New System.Drawing.Size(267, 22)
        Me.miSTM95M02.Text = "ST M95M02"
        '
        'ToolStripSeparator6
        '
        Me.ToolStripSeparator6.Name = "ToolStripSeparator6"
        Me.ToolStripSeparator6.Size = New System.Drawing.Size(264, 6)
        '
        'miM25AA160A
        '
        Me.miM25AA160A.Name = "miM25AA160A"
        Me.miM25AA160A.Size = New System.Drawing.Size(267, 22)
        Me.miM25AA160A.Text = "Microchip M25AA160A"
        '
        'miM25AA160B
        '
        Me.miM25AA160B.Name = "miM25AA160B"
        Me.miM25AA160B.Size = New System.Drawing.Size(267, 22)
        Me.miM25AA160B.Text = "Microchip M25AA160B"
        '
        'mi_ext_mode
        '
        Me.mi_ext_mode.Name = "mi_ext_mode"
        Me.mi_ext_mode.Size = New System.Drawing.Size(183, 22)
        Me.mi_ext_mode.Text = "Extension I/O mode"
        '
        'ScriptToolStripMenuItem
        '
        Me.ScriptToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.CurrentScript_MI, Me.LoadScript_MI, Me.UnloadScript_MI})
        Me.ScriptToolStripMenuItem.Name = "ScriptToolStripMenuItem"
        Me.ScriptToolStripMenuItem.Size = New System.Drawing.Size(49, 20)
        Me.ScriptToolStripMenuItem.Text = "Script"
        '
        'CurrentScript_MI
        '
        Me.CurrentScript_MI.Image = Global.FlashcatUSB.My.Resources.Resources.config
        Me.CurrentScript_MI.Name = "CurrentScript_MI"
        Me.CurrentScript_MI.Size = New System.Drawing.Size(146, 22)
        Me.CurrentScript_MI.Text = "Current script"
        '
        'LoadScript_MI
        '
        Me.LoadScript_MI.Image = Global.FlashcatUSB.My.Resources.Resources.openfile
        Me.LoadScript_MI.Name = "LoadScript_MI"
        Me.LoadScript_MI.Size = New System.Drawing.Size(146, 22)
        Me.LoadScript_MI.Text = "Load script"
        '
        'UnloadScript_MI
        '
        Me.UnloadScript_MI.Image = Global.FlashcatUSB.My.Resources.Resources.clear_x
        Me.UnloadScript_MI.Name = "UnloadScript_MI"
        Me.UnloadScript_MI.Size = New System.Drawing.Size(146, 22)
        Me.UnloadScript_MI.Text = "Unload script"
        '
        'mi_tools_menu
        '
        Me.mi_tools_menu.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.EraseChipToolStripMenuItem, Me.miFlashSeperator1, Me.miBytesPerPage_Normal, Me.miBytesPerPage_Extended, Me.mi_tools_seperate2, Me.CreateNANDImageToolStripMenuItem, Me.WriteNANDImageToolStripMenuItem})
        Me.mi_tools_menu.Name = "mi_tools_menu"
        Me.mi_tools_menu.Size = New System.Drawing.Size(47, 20)
        Me.mi_tools_menu.Text = "Tools"
        '
        'EraseChipToolStripMenuItem
        '
        Me.EraseChipToolStripMenuItem.Image = Global.FlashcatUSB.My.Resources.Resources._erase
        Me.EraseChipToolStripMenuItem.Name = "EraseChipToolStripMenuItem"
        Me.EraseChipToolStripMenuItem.Size = New System.Drawing.Size(181, 22)
        Me.EraseChipToolStripMenuItem.Text = "Erase chip"
        '
        'miFlashSeperator1
        '
        Me.miFlashSeperator1.Name = "miFlashSeperator1"
        Me.miFlashSeperator1.Size = New System.Drawing.Size(178, 6)
        '
        'miBytesPerPage_Normal
        '
        Me.miBytesPerPage_Normal.Name = "miBytesPerPage_Normal"
        Me.miBytesPerPage_Normal.Size = New System.Drawing.Size(181, 22)
        Me.miBytesPerPage_Normal.Text = "x bytes per page"
        '
        'miBytesPerPage_Extended
        '
        Me.miBytesPerPage_Extended.Name = "miBytesPerPage_Extended"
        Me.miBytesPerPage_Extended.Size = New System.Drawing.Size(181, 22)
        Me.miBytesPerPage_Extended.Text = "x bytes per page"
        '
        'mi_tools_seperate2
        '
        Me.mi_tools_seperate2.Name = "mi_tools_seperate2"
        Me.mi_tools_seperate2.Size = New System.Drawing.Size(178, 6)
        '
        'CreateNANDImageToolStripMenuItem
        '
        Me.CreateNANDImageToolStripMenuItem.Image = Global.FlashcatUSB.My.Resources.Resources.download
        Me.CreateNANDImageToolStripMenuItem.Name = "CreateNANDImageToolStripMenuItem"
        Me.CreateNANDImageToolStripMenuItem.Size = New System.Drawing.Size(181, 22)
        Me.CreateNANDImageToolStripMenuItem.Text = "Create NAND image"
        '
        'WriteNANDImageToolStripMenuItem
        '
        Me.WriteNANDImageToolStripMenuItem.Image = Global.FlashcatUSB.My.Resources.Resources.upload
        Me.WriteNANDImageToolStripMenuItem.Name = "WriteNANDImageToolStripMenuItem"
        Me.WriteNANDImageToolStripMenuItem.Size = New System.Drawing.Size(181, 22)
        Me.WriteNANDImageToolStripMenuItem.Text = "Write NAND image"
        '
        'LanguageToolStripMenuItem
        '
        Me.LanguageToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.EnglishToolStripMenuItem, Me.ChineseToolStripMenuItem, Me.FrenchToolStripMenuItem, Me.GermanToolStripMenuItem, Me.PortugueseToolStripMenuItem, Me.SpanishToolStripMenuItem})
        Me.LanguageToolStripMenuItem.Name = "LanguageToolStripMenuItem"
        Me.LanguageToolStripMenuItem.Size = New System.Drawing.Size(71, 20)
        Me.LanguageToolStripMenuItem.Text = "Language"
        '
        'EnglishToolStripMenuItem
        '
        Me.EnglishToolStripMenuItem.Image = Global.FlashcatUSB.My.Resources.Resources.English
        Me.EnglishToolStripMenuItem.Name = "EnglishToolStripMenuItem"
        Me.EnglishToolStripMenuItem.Size = New System.Drawing.Size(134, 22)
        Me.EnglishToolStripMenuItem.Text = "English"
        '
        'ChineseToolStripMenuItem
        '
        Me.ChineseToolStripMenuItem.Image = Global.FlashcatUSB.My.Resources.Resources.china
        Me.ChineseToolStripMenuItem.Name = "ChineseToolStripMenuItem"
        Me.ChineseToolStripMenuItem.Size = New System.Drawing.Size(134, 22)
        Me.ChineseToolStripMenuItem.Text = "Chinese"
        Me.ChineseToolStripMenuItem.Visible = False
        '
        'FrenchToolStripMenuItem
        '
        Me.FrenchToolStripMenuItem.Image = Global.FlashcatUSB.My.Resources.Resources.france
        Me.FrenchToolStripMenuItem.Name = "FrenchToolStripMenuItem"
        Me.FrenchToolStripMenuItem.Size = New System.Drawing.Size(134, 22)
        Me.FrenchToolStripMenuItem.Text = "French"
        '
        'GermanToolStripMenuItem
        '
        Me.GermanToolStripMenuItem.Image = Global.FlashcatUSB.My.Resources.Resources.german
        Me.GermanToolStripMenuItem.Name = "GermanToolStripMenuItem"
        Me.GermanToolStripMenuItem.Size = New System.Drawing.Size(134, 22)
        Me.GermanToolStripMenuItem.Text = "German"
        '
        'PortugueseToolStripMenuItem
        '
        Me.PortugueseToolStripMenuItem.Image = Global.FlashcatUSB.My.Resources.Resources.portugal
        Me.PortugueseToolStripMenuItem.Name = "PortugueseToolStripMenuItem"
        Me.PortugueseToolStripMenuItem.Size = New System.Drawing.Size(134, 22)
        Me.PortugueseToolStripMenuItem.Text = "Portuguese"
        '
        'SpanishToolStripMenuItem
        '
        Me.SpanishToolStripMenuItem.Image = Global.FlashcatUSB.My.Resources.Resources.spain
        Me.SpanishToolStripMenuItem.Name = "SpanishToolStripMenuItem"
        Me.SpanishToolStripMenuItem.Size = New System.Drawing.Size(134, 22)
        Me.SpanishToolStripMenuItem.Text = "Spanish"
        '
        'MyTabs
        '
        Me.MyTabs.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.MyTabs.Controls.Add(Me.TabStatus)
        Me.MyTabs.Controls.Add(Me.TabConsole)
        Me.MyTabs.Controls.Add(Me.AvrTab)
        Me.MyTabs.Controls.Add(Me.SpiTab)
        Me.MyTabs.Controls.Add(Me.I2CTab)
        Me.MyTabs.Location = New System.Drawing.Point(0, 25)
        Me.MyTabs.Name = "MyTabs"
        Me.MyTabs.SelectedIndex = 0
        Me.MyTabs.Size = New System.Drawing.Size(452, 315)
        Me.MyTabs.TabIndex = 2
        '
        'TabStatus
        '
        Me.TabStatus.Controls.Add(Me.pb_logo)
        Me.TabStatus.Controls.Add(Me.TableLayoutPanel2)
        Me.TabStatus.Location = New System.Drawing.Point(4, 22)
        Me.TabStatus.Name = "TabStatus"
        Me.TabStatus.Padding = New System.Windows.Forms.Padding(3)
        Me.TabStatus.Size = New System.Drawing.Size(444, 289)
        Me.TabStatus.TabIndex = 0
        Me.TabStatus.Text = "Status"
        Me.TabStatus.UseVisualStyleBackColor = True
        '
        'pb_logo
        '
        Me.pb_logo.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pb_logo.BackgroundImage = Global.FlashcatUSB.My.Resources.Resources.logo_ec
        Me.pb_logo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center
        Me.pb_logo.Location = New System.Drawing.Point(3, 183)
        Me.pb_logo.Name = "pb_logo"
        Me.pb_logo.Size = New System.Drawing.Size(438, 100)
        Me.pb_logo.TabIndex = 6
        Me.pb_logo.TabStop = False
        '
        'TableLayoutPanel2
        '
        Me.TableLayoutPanel2.ColumnCount = 1
        Me.TableLayoutPanel2.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
        Me.TableLayoutPanel2.Controls.Add(Me.sm7, 0, 7)
        Me.TableLayoutPanel2.Controls.Add(Me.sm6, 0, 6)
        Me.TableLayoutPanel2.Controls.Add(Me.sm5, 0, 5)
        Me.TableLayoutPanel2.Controls.Add(Me.sm4, 0, 4)
        Me.TableLayoutPanel2.Controls.Add(Me.sm3, 0, 3)
        Me.TableLayoutPanel2.Controls.Add(Me.sm2, 0, 2)
        Me.TableLayoutPanel2.Controls.Add(Me.lblStatus, 0, 0)
        Me.TableLayoutPanel2.Controls.Add(Me.sm1, 0, 1)
        Me.TableLayoutPanel2.Location = New System.Drawing.Point(3, 12)
        Me.TableLayoutPanel2.Name = "TableLayoutPanel2"
        Me.TableLayoutPanel2.RowCount = 9
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.TableLayoutPanel2.Size = New System.Drawing.Size(438, 165)
        Me.TableLayoutPanel2.TabIndex = 4
        '
        'sm7
        '
        Me.sm7.AutoSize = True
        Me.sm7.Location = New System.Drawing.Point(3, 140)
        Me.sm7.Name = "sm7"
        Me.sm7.Size = New System.Drawing.Size(0, 13)
        Me.sm7.TabIndex = 10
        '
        'sm6
        '
        Me.sm6.AutoSize = True
        Me.sm6.Location = New System.Drawing.Point(3, 120)
        Me.sm6.Name = "sm6"
        Me.sm6.Size = New System.Drawing.Size(0, 13)
        Me.sm6.TabIndex = 9
        '
        'sm5
        '
        Me.sm5.AutoSize = True
        Me.sm5.Location = New System.Drawing.Point(3, 100)
        Me.sm5.Name = "sm5"
        Me.sm5.Size = New System.Drawing.Size(0, 13)
        Me.sm5.TabIndex = 8
        '
        'sm4
        '
        Me.sm4.AutoSize = True
        Me.sm4.Location = New System.Drawing.Point(3, 80)
        Me.sm4.Name = "sm4"
        Me.sm4.Size = New System.Drawing.Size(0, 13)
        Me.sm4.TabIndex = 7
        '
        'sm3
        '
        Me.sm3.AutoSize = True
        Me.sm3.Location = New System.Drawing.Point(3, 60)
        Me.sm3.Name = "sm3"
        Me.sm3.Size = New System.Drawing.Size(0, 13)
        Me.sm3.TabIndex = 6
        '
        'sm2
        '
        Me.sm2.AutoSize = True
        Me.sm2.Location = New System.Drawing.Point(3, 40)
        Me.sm2.Name = "sm2"
        Me.sm2.Size = New System.Drawing.Size(0, 13)
        Me.sm2.TabIndex = 5
        '
        'lblStatus
        '
        Me.lblStatus.AutoSize = True
        Me.lblStatus.Location = New System.Drawing.Point(3, 0)
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Size = New System.Drawing.Size(177, 13)
        Me.lblStatus.TabIndex = 3
        Me.lblStatus.Text = "FlashcatUSB status: Not connected"
        '
        'sm1
        '
        Me.sm1.AutoSize = True
        Me.sm1.Location = New System.Drawing.Point(3, 20)
        Me.sm1.Name = "sm1"
        Me.sm1.Size = New System.Drawing.Size(0, 13)
        Me.sm1.TabIndex = 4
        '
        'TabConsole
        '
        Me.TabConsole.Controls.Add(Me.cmdSaveLog)
        Me.TabConsole.Controls.Add(Me.txtInput)
        Me.TabConsole.Controls.Add(Me.ConsoleBox)
        Me.TabConsole.Location = New System.Drawing.Point(4, 22)
        Me.TabConsole.Name = "TabConsole"
        Me.TabConsole.Padding = New System.Windows.Forms.Padding(3)
        Me.TabConsole.Size = New System.Drawing.Size(444, 289)
        Me.TabConsole.TabIndex = 1
        Me.TabConsole.Text = "Console"
        Me.TabConsole.UseVisualStyleBackColor = True
        '
        'cmdSaveLog
        '
        Me.cmdSaveLog.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdSaveLog.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.cmdSaveLog.Image = CType(resources.GetObject("cmdSaveLog.Image"), System.Drawing.Image)
        Me.cmdSaveLog.Location = New System.Drawing.Point(416, 260)
        Me.cmdSaveLog.Name = "cmdSaveLog"
        Me.cmdSaveLog.Size = New System.Drawing.Size(22, 22)
        Me.cmdSaveLog.TabIndex = 5
        Me.cmdSaveLog.UseVisualStyleBackColor = True
        '
        'txtInput
        '
        Me.txtInput.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtInput.Location = New System.Drawing.Point(3, 260)
        Me.txtInput.Name = "txtInput"
        Me.txtInput.Size = New System.Drawing.Size(410, 20)
        Me.txtInput.TabIndex = 4
        '
        'ConsoleBox
        '
        Me.ConsoleBox.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ConsoleBox.FormattingEnabled = True
        Me.ConsoleBox.Location = New System.Drawing.Point(3, 3)
        Me.ConsoleBox.Name = "ConsoleBox"
        Me.ConsoleBox.Size = New System.Drawing.Size(438, 251)
        Me.ConsoleBox.TabIndex = 0
        '
        'AvrTab
        '
        Me.AvrTab.Controls.Add(Me.AvrEditor)
        Me.AvrTab.Controls.Add(Me.lblAvrCrc)
        Me.AvrTab.Controls.Add(Me.lblAvrRange)
        Me.AvrTab.Controls.Add(Me.lblAvrFn)
        Me.AvrTab.Controls.Add(Me.cmdAvrProg)
        Me.AvrTab.Controls.Add(Me.cmdAvrStart)
        Me.AvrTab.Controls.Add(Me.cmdAvrLoad)
        Me.AvrTab.Controls.Add(Me.DfuPbBar)
        Me.AvrTab.Location = New System.Drawing.Point(4, 22)
        Me.AvrTab.Name = "AvrTab"
        Me.AvrTab.Padding = New System.Windows.Forms.Padding(3)
        Me.AvrTab.Size = New System.Drawing.Size(444, 289)
        Me.AvrTab.TabIndex = 2
        Me.AvrTab.Text = "AVR Firmware"
        Me.AvrTab.UseVisualStyleBackColor = True
        '
        'AvrEditor
        '
        Me.AvrEditor.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.AvrEditor.Location = New System.Drawing.Point(6, 50)
        Me.AvrEditor.Name = "AvrEditor"
        Me.AvrEditor.Size = New System.Drawing.Size(435, 217)
        Me.AvrEditor.TabIndex = 15
        '
        'lblAvrCrc
        '
        Me.lblAvrCrc.AutoSize = True
        Me.lblAvrCrc.ForeColor = System.Drawing.Color.Gray
        Me.lblAvrCrc.Location = New System.Drawing.Point(354, 6)
        Me.lblAvrCrc.Name = "lblAvrCrc"
        Me.lblAvrCrc.Size = New System.Drawing.Size(82, 13)
        Me.lblAvrCrc.TabIndex = 14
        Me.lblAvrCrc.Text = "CRC: 0x000000"
        '
        'lblAvrRange
        '
        Me.lblAvrRange.AutoSize = True
        Me.lblAvrRange.ForeColor = System.Drawing.Color.Gray
        Me.lblAvrRange.Location = New System.Drawing.Point(224, 6)
        Me.lblAvrRange.Name = "lblAvrRange"
        Me.lblAvrRange.Size = New System.Drawing.Size(124, 13)
        Me.lblAvrRange.TabIndex = 13
        Me.lblAvrRange.Text = "Range: 0x0000 - 0x0000"
        '
        'lblAvrFn
        '
        Me.lblAvrFn.AutoSize = True
        Me.lblAvrFn.Location = New System.Drawing.Point(8, 6)
        Me.lblAvrFn.Name = "lblAvrFn"
        Me.lblAvrFn.Size = New System.Drawing.Size(135, 13)
        Me.lblAvrFn.TabIndex = 12
        Me.lblAvrFn.Text = "File: no file currently loaded"
        '
        'cmdAvrProg
        '
        Me.cmdAvrProg.Location = New System.Drawing.Point(156, 24)
        Me.cmdAvrProg.Name = "cmdAvrProg"
        Me.cmdAvrProg.Size = New System.Drawing.Size(112, 22)
        Me.cmdAvrProg.TabIndex = 9
        Me.cmdAvrProg.Text = "Program"
        Me.cmdAvrProg.UseVisualStyleBackColor = True
        '
        'cmdAvrStart
        '
        Me.cmdAvrStart.Location = New System.Drawing.Point(300, 25)
        Me.cmdAvrStart.Name = "cmdAvrStart"
        Me.cmdAvrStart.Size = New System.Drawing.Size(136, 22)
        Me.cmdAvrStart.TabIndex = 11
        Me.cmdAvrStart.Text = "Start Application"
        Me.cmdAvrStart.UseVisualStyleBackColor = True
        '
        'cmdAvrLoad
        '
        Me.cmdAvrLoad.Location = New System.Drawing.Point(11, 25)
        Me.cmdAvrLoad.Name = "cmdAvrLoad"
        Me.cmdAvrLoad.Size = New System.Drawing.Size(113, 22)
        Me.cmdAvrLoad.TabIndex = 10
        Me.cmdAvrLoad.Text = "Load File"
        Me.cmdAvrLoad.UseVisualStyleBackColor = True
        '
        'DfuPbBar
        '
        Me.DfuPbBar.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DfuPbBar.Location = New System.Drawing.Point(8, 269)
        Me.DfuPbBar.Name = "DfuPbBar"
        Me.DfuPbBar.Size = New System.Drawing.Size(430, 12)
        Me.DfuPbBar.TabIndex = 8
        '
        'SpiTab
        '
        Me.SpiTab.Controls.Add(Me.cbSPI)
        Me.SpiTab.Controls.Add(Me.RadioUseSpiSettings)
        Me.SpiTab.Controls.Add(Me.RadioUseSpiAuto)
        Me.SpiTab.Location = New System.Drawing.Point(4, 22)
        Me.SpiTab.Name = "SpiTab"
        Me.SpiTab.Padding = New System.Windows.Forms.Padding(3)
        Me.SpiTab.Size = New System.Drawing.Size(444, 289)
        Me.SpiTab.TabIndex = 3
        Me.SpiTab.Text = "SPI Settings"
        Me.SpiTab.UseVisualStyleBackColor = True
        '
        'cbSPI
        '
        Me.cbSPI.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cbSPI.Controls.Add(Me.Label1)
        Me.cbSPI.Controls.Add(Me.txtPageSize)
        Me.cbSPI.Controls.Add(Me.cbSpiProgMode)
        Me.cbSPI.Controls.Add(Me.cbUseEnWS)
        Me.cbSPI.Controls.Add(Me.txtEnWS)
        Me.cbSPI.Controls.Add(Me.lblSpiProgMode)
        Me.cbSPI.Controls.Add(Me.lblSpiInfo)
        Me.cbSPI.Controls.Add(Me.txtChipSize)
        Me.cbSPI.Controls.Add(Me.lblSpiChipSize)
        Me.cbSPI.Controls.Add(Me.txtChipErase)
        Me.cbSPI.Controls.Add(Me.lblSpiRead)
        Me.cbSPI.Controls.Add(Me.txtRead)
        Me.cbSPI.Controls.Add(Me.txtWriteStatus)
        Me.cbSPI.Controls.Add(Me.lblSpiSectorErase)
        Me.cbSPI.Controls.Add(Me.lblSpiWriteStatus)
        Me.cbSPI.Controls.Add(Me.txtSectorErase)
        Me.cbSPI.Controls.Add(Me.txtReadStatus)
        Me.cbSPI.Controls.Add(Me.lblSpiEraseSize)
        Me.cbSPI.Controls.Add(Me.lblSpiReadStatus)
        Me.cbSPI.Controls.Add(Me.txtEraseSize)
        Me.cbSPI.Controls.Add(Me.txtWriteEnable)
        Me.cbSPI.Controls.Add(Me.lblSpiWriteEn)
        Me.cbSPI.Controls.Add(Me.txtPageProgram)
        Me.cbSPI.Controls.Add(Me.lblSpiChipErase)
        Me.cbSPI.Controls.Add(Me.lblSpiPageProgram)
        Me.cbSPI.Location = New System.Drawing.Point(8, 30)
        Me.cbSPI.Name = "cbSPI"
        Me.cbSPI.Size = New System.Drawing.Size(428, 249)
        Me.cbSPI.TabIndex = 24
        Me.cbSPI.TabStop = False
        Me.cbSPI.Text = "SPI device commands"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(9, 116)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(55, 13)
        Me.Label1.TabIndex = 34
        Me.Label1.Text = "Page Size"
        '
        'txtPageSize
        '
        Me.txtPageSize.Location = New System.Drawing.Point(8, 134)
        Me.txtPageSize.Name = "txtPageSize"
        Me.txtPageSize.Size = New System.Drawing.Size(66, 20)
        Me.txtPageSize.TabIndex = 12
        Me.txtPageSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'cbSpiProgMode
        '
        Me.cbSpiProgMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbSpiProgMode.FormattingEnabled = True
        Me.cbSpiProgMode.Location = New System.Drawing.Point(303, 38)
        Me.cbSpiProgMode.Name = "cbSpiProgMode"
        Me.cbSpiProgMode.Size = New System.Drawing.Size(90, 21)
        Me.cbSpiProgMode.TabIndex = 33
        '
        'cbUseEnWS
        '
        Me.cbUseEnWS.AutoSize = True
        Me.cbUseEnWS.Location = New System.Drawing.Point(100, 164)
        Me.cbUseEnWS.Name = "cbUseEnWS"
        Me.cbUseEnWS.Size = New System.Drawing.Size(59, 17)
        Me.cbUseEnWS.TabIndex = 13
        Me.cbUseEnWS.Text = "ENWS"
        Me.cbUseEnWS.UseVisualStyleBackColor = True
        '
        'txtEnWS
        '
        Me.txtEnWS.Enabled = False
        Me.txtEnWS.Location = New System.Drawing.Point(111, 187)
        Me.txtEnWS.Name = "txtEnWS"
        Me.txtEnWS.Size = New System.Drawing.Size(36, 20)
        Me.txtEnWS.TabIndex = 14
        Me.txtEnWS.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'lblSpiProgMode
        '
        Me.lblSpiProgMode.AutoSize = True
        Me.lblSpiProgMode.Location = New System.Drawing.Point(299, 15)
        Me.lblSpiProgMode.Name = "lblSpiProgMode"
        Me.lblSpiProgMode.Size = New System.Drawing.Size(76, 13)
        Me.lblSpiProgMode.TabIndex = 27
        Me.lblSpiProgMode.Text = "Program Mode"
        '
        'lblSpiInfo
        '
        Me.lblSpiInfo.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lblSpiInfo.AutoSize = True
        Me.lblSpiInfo.Location = New System.Drawing.Point(4, 224)
        Me.lblSpiInfo.Name = "lblSpiInfo"
        Me.lblSpiInfo.Size = New System.Drawing.Size(279, 13)
        Me.lblSpiInfo.TabIndex = 25
        Me.lblSpiInfo.Text = "Use the values commonly found in the device's datasheet"
        '
        'txtChipSize
        '
        Me.txtChipSize.Location = New System.Drawing.Point(8, 38)
        Me.txtChipSize.Name = "txtChipSize"
        Me.txtChipSize.Size = New System.Drawing.Size(66, 20)
        Me.txtChipSize.TabIndex = 10
        Me.txtChipSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'lblSpiChipSize
        '
        Me.lblSpiChipSize.AutoSize = True
        Me.lblSpiChipSize.Location = New System.Drawing.Point(8, 20)
        Me.lblSpiChipSize.Name = "lblSpiChipSize"
        Me.lblSpiChipSize.Size = New System.Drawing.Size(51, 13)
        Me.lblSpiChipSize.TabIndex = 7
        Me.lblSpiChipSize.Text = "Chip Size"
        '
        'txtChipErase
        '
        Me.txtChipErase.Location = New System.Drawing.Point(209, 134)
        Me.txtChipErase.Name = "txtChipErase"
        Me.txtChipErase.Size = New System.Drawing.Size(44, 20)
        Me.txtChipErase.TabIndex = 22
        Me.txtChipErase.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'lblSpiRead
        '
        Me.lblSpiRead.AutoSize = True
        Me.lblSpiRead.Location = New System.Drawing.Point(217, 20)
        Me.lblSpiRead.Name = "lblSpiRead"
        Me.lblSpiRead.Size = New System.Drawing.Size(33, 13)
        Me.lblSpiRead.TabIndex = 1
        Me.lblSpiRead.Text = "Read"
        '
        'txtRead
        '
        Me.txtRead.Location = New System.Drawing.Point(209, 38)
        Me.txtRead.Name = "txtRead"
        Me.txtRead.Size = New System.Drawing.Size(44, 20)
        Me.txtRead.TabIndex = 20
        Me.txtRead.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'txtWriteStatus
        '
        Me.txtWriteStatus.Location = New System.Drawing.Point(11, 188)
        Me.txtWriteStatus.Name = "txtWriteStatus"
        Me.txtWriteStatus.Size = New System.Drawing.Size(44, 20)
        Me.txtWriteStatus.TabIndex = 13
        Me.txtWriteStatus.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'lblSpiSectorErase
        '
        Me.lblSpiSectorErase.AutoSize = True
        Me.lblSpiSectorErase.Location = New System.Drawing.Point(205, 68)
        Me.lblSpiSectorErase.Name = "lblSpiSectorErase"
        Me.lblSpiSectorErase.Size = New System.Drawing.Size(68, 13)
        Me.lblSpiSectorErase.TabIndex = 3
        Me.lblSpiSectorErase.Text = "Sector Erase"
        '
        'lblSpiWriteStatus
        '
        Me.lblSpiWriteStatus.AutoSize = True
        Me.lblSpiWriteStatus.Location = New System.Drawing.Point(9, 168)
        Me.lblSpiWriteStatus.Name = "lblSpiWriteStatus"
        Me.lblSpiWriteStatus.Size = New System.Drawing.Size(65, 13)
        Me.lblSpiWriteStatus.TabIndex = 17
        Me.lblSpiWriteStatus.Text = "Write Status"
        '
        'txtSectorErase
        '
        Me.txtSectorErase.Location = New System.Drawing.Point(208, 86)
        Me.txtSectorErase.Name = "txtSectorErase"
        Me.txtSectorErase.Size = New System.Drawing.Size(44, 20)
        Me.txtSectorErase.TabIndex = 21
        Me.txtSectorErase.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'txtReadStatus
        '
        Me.txtReadStatus.Location = New System.Drawing.Point(112, 134)
        Me.txtReadStatus.Name = "txtReadStatus"
        Me.txtReadStatus.Size = New System.Drawing.Size(48, 20)
        Me.txtReadStatus.TabIndex = 17
        Me.txtReadStatus.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'lblSpiEraseSize
        '
        Me.lblSpiEraseSize.AutoSize = True
        Me.lblSpiEraseSize.Location = New System.Drawing.Point(8, 68)
        Me.lblSpiEraseSize.Name = "lblSpiEraseSize"
        Me.lblSpiEraseSize.Size = New System.Drawing.Size(57, 13)
        Me.lblSpiEraseSize.TabIndex = 5
        Me.lblSpiEraseSize.Text = "Erase Size"
        '
        'lblSpiReadStatus
        '
        Me.lblSpiReadStatus.AutoSize = True
        Me.lblSpiReadStatus.Location = New System.Drawing.Point(100, 116)
        Me.lblSpiReadStatus.Name = "lblSpiReadStatus"
        Me.lblSpiReadStatus.Size = New System.Drawing.Size(66, 13)
        Me.lblSpiReadStatus.TabIndex = 15
        Me.lblSpiReadStatus.Text = "Read Status"
        '
        'txtEraseSize
        '
        Me.txtEraseSize.Location = New System.Drawing.Point(7, 86)
        Me.txtEraseSize.Name = "txtEraseSize"
        Me.txtEraseSize.Size = New System.Drawing.Size(66, 20)
        Me.txtEraseSize.TabIndex = 11
        Me.txtEraseSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'txtWriteEnable
        '
        Me.txtWriteEnable.Location = New System.Drawing.Point(111, 86)
        Me.txtWriteEnable.Name = "txtWriteEnable"
        Me.txtWriteEnable.Size = New System.Drawing.Size(48, 20)
        Me.txtWriteEnable.TabIndex = 16
        Me.txtWriteEnable.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'lblSpiWriteEn
        '
        Me.lblSpiWriteEn.AutoSize = True
        Me.lblSpiWriteEn.Location = New System.Drawing.Point(100, 68)
        Me.lblSpiWriteEn.Name = "lblSpiWriteEn"
        Me.lblSpiWriteEn.Size = New System.Drawing.Size(68, 13)
        Me.lblSpiWriteEn.TabIndex = 13
        Me.lblSpiWriteEn.Text = "Write Enable"
        '
        'txtPageProgram
        '
        Me.txtPageProgram.Location = New System.Drawing.Point(112, 38)
        Me.txtPageProgram.Name = "txtPageProgram"
        Me.txtPageProgram.Size = New System.Drawing.Size(48, 20)
        Me.txtPageProgram.TabIndex = 15
        Me.txtPageProgram.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'lblSpiChipErase
        '
        Me.lblSpiChipErase.AutoSize = True
        Me.lblSpiChipErase.Location = New System.Drawing.Point(209, 116)
        Me.lblSpiChipErase.Name = "lblSpiChipErase"
        Me.lblSpiChipErase.Size = New System.Drawing.Size(58, 13)
        Me.lblSpiChipErase.TabIndex = 9
        Me.lblSpiChipErase.Text = "Chip Erase"
        '
        'lblSpiPageProgram
        '
        Me.lblSpiPageProgram.AutoSize = True
        Me.lblSpiPageProgram.Location = New System.Drawing.Point(100, 20)
        Me.lblSpiPageProgram.Name = "lblSpiPageProgram"
        Me.lblSpiPageProgram.Size = New System.Drawing.Size(74, 13)
        Me.lblSpiPageProgram.TabIndex = 11
        Me.lblSpiPageProgram.Text = "Page Program"
        '
        'RadioUseSpiSettings
        '
        Me.RadioUseSpiSettings.AutoSize = True
        Me.RadioUseSpiSettings.Location = New System.Drawing.Point(260, 8)
        Me.RadioUseSpiSettings.Name = "RadioUseSpiSettings"
        Me.RadioUseSpiSettings.Size = New System.Drawing.Size(112, 17)
        Me.RadioUseSpiSettings.TabIndex = 2
        Me.RadioUseSpiSettings.Text = "Use these settings"
        Me.RadioUseSpiSettings.UseVisualStyleBackColor = True
        '
        'RadioUseSpiAuto
        '
        Me.RadioUseSpiAuto.AutoSize = True
        Me.RadioUseSpiAuto.Checked = True
        Me.RadioUseSpiAuto.Location = New System.Drawing.Point(48, 8)
        Me.RadioUseSpiAuto.Name = "RadioUseSpiAuto"
        Me.RadioUseSpiAuto.Size = New System.Drawing.Size(132, 17)
        Me.RadioUseSpiAuto.TabIndex = 1
        Me.RadioUseSpiAuto.TabStop = True
        Me.RadioUseSpiAuto.Text = "Use automatic settings"
        Me.RadioUseSpiAuto.UseVisualStyleBackColor = True
        '
        'I2CTab
        '
        Me.I2CTab.Controls.Add(Me.pbI2C)
        Me.I2CTab.Controls.Add(Me.Label3)
        Me.I2CTab.Controls.Add(Me.cbI2C_A2)
        Me.I2CTab.Controls.Add(Me.cbI2C_A1)
        Me.I2CTab.Controls.Add(Me.cbI2C_A0)
        Me.I2CTab.Controls.Add(Me.GroupBox1)
        Me.I2CTab.Controls.Add(Me.cmdI2cConnect)
        Me.I2CTab.Controls.Add(Me.Label2)
        Me.I2CTab.Controls.Add(Me.cbEepromDevices)
        Me.I2CTab.Controls.Add(Me.I2CEditor)
        Me.I2CTab.Location = New System.Drawing.Point(4, 22)
        Me.I2CTab.Name = "I2CTab"
        Me.I2CTab.Padding = New System.Windows.Forms.Padding(3)
        Me.I2CTab.Size = New System.Drawing.Size(444, 289)
        Me.I2CTab.TabIndex = 4
        Me.I2CTab.Text = "I2C EEPROM"
        Me.I2CTab.UseVisualStyleBackColor = True
        '
        'pbI2C
        '
        Me.pbI2C.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pbI2C.Location = New System.Drawing.Point(9, 274)
        Me.pbI2C.Name = "pbI2C"
        Me.pbI2C.Size = New System.Drawing.Size(430, 12)
        Me.pbI2C.TabIndex = 28
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(6, 32)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(44, 13)
        Me.Label3.TabIndex = 27
        Me.Label3.Text = "Device:"
        '
        'cbI2C_A2
        '
        Me.cbI2C_A2.AutoSize = True
        Me.cbI2C_A2.Location = New System.Drawing.Point(108, 6)
        Me.cbI2C_A2.Name = "cbI2C_A2"
        Me.cbI2C_A2.Size = New System.Drawing.Size(39, 17)
        Me.cbI2C_A2.TabIndex = 26
        Me.cbI2C_A2.Text = "A2"
        Me.cbI2C_A2.UseVisualStyleBackColor = True
        '
        'cbI2C_A1
        '
        Me.cbI2C_A1.AutoSize = True
        Me.cbI2C_A1.Location = New System.Drawing.Point(151, 6)
        Me.cbI2C_A1.Name = "cbI2C_A1"
        Me.cbI2C_A1.Size = New System.Drawing.Size(39, 17)
        Me.cbI2C_A1.TabIndex = 25
        Me.cbI2C_A1.Text = "A1"
        Me.cbI2C_A1.UseVisualStyleBackColor = True
        '
        'cbI2C_A0
        '
        Me.cbI2C_A0.AutoSize = True
        Me.cbI2C_A0.Location = New System.Drawing.Point(196, 6)
        Me.cbI2C_A0.Name = "cbI2C_A0"
        Me.cbI2C_A0.Size = New System.Drawing.Size(39, 17)
        Me.cbI2C_A0.TabIndex = 24
        Me.cbI2C_A0.Text = "A0"
        Me.cbI2C_A0.UseVisualStyleBackColor = True
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.cmdI2cRead)
        Me.GroupBox1.Controls.Add(Me.cmdI2cWrite)
        Me.GroupBox1.Location = New System.Drawing.Point(268, 6)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(168, 50)
        Me.GroupBox1.TabIndex = 23
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "File I/O"
        '
        'cmdI2cRead
        '
        Me.cmdI2cRead.Location = New System.Drawing.Point(6, 19)
        Me.cmdI2cRead.Name = "cmdI2cRead"
        Me.cmdI2cRead.Size = New System.Drawing.Size(75, 23)
        Me.cmdI2cRead.TabIndex = 17
        Me.cmdI2cRead.Text = "Read"
        Me.cmdI2cRead.UseVisualStyleBackColor = True
        '
        'cmdI2cWrite
        '
        Me.cmdI2cWrite.Location = New System.Drawing.Point(87, 19)
        Me.cmdI2cWrite.Name = "cmdI2cWrite"
        Me.cmdI2cWrite.Size = New System.Drawing.Size(75, 23)
        Me.cmdI2cWrite.TabIndex = 18
        Me.cmdI2cWrite.Text = "Write"
        Me.cmdI2cWrite.UseVisualStyleBackColor = True
        '
        'cmdI2cConnect
        '
        Me.cmdI2cConnect.Location = New System.Drawing.Point(187, 27)
        Me.cmdI2cConnect.Name = "cmdI2cConnect"
        Me.cmdI2cConnect.Size = New System.Drawing.Size(75, 23)
        Me.cmdI2cConnect.TabIndex = 21
        Me.cmdI2cConnect.Text = "Connect"
        Me.cmdI2cConnect.UseVisualStyleBackColor = True
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(5, 7)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(97, 13)
        Me.Label2.TabIndex = 20
        Me.Label2.Text = "EEPROM Address:"
        '
        'cbEepromDevices
        '
        Me.cbEepromDevices.AllowResizeDropDown = True
        Me.cbEepromDevices.ControlSize = New System.Drawing.Size(1, 1)
        Me.cbEepromDevices.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed
        Me.cbEepromDevices.DropDownControl = Nothing
        Me.cbEepromDevices.DropSize = New System.Drawing.Size(121, 106)
        Me.cbEepromDevices.Location = New System.Drawing.Point(53, 27)
        Me.cbEepromDevices.Name = "cbEepromDevices"
        Me.cbEepromDevices.Size = New System.Drawing.Size(127, 21)
        Me.cbEepromDevices.TabIndex = 22
        '
        'I2CEditor
        '
        Me.I2CEditor.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.I2CEditor.Location = New System.Drawing.Point(6, 60)
        Me.I2CEditor.Name = "I2CEditor"
        Me.I2CEditor.Size = New System.Drawing.Size(435, 209)
        Me.I2CEditor.TabIndex = 16
        '
        'StatusStrip1
        '
        Me.StatusStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.Status})
        Me.StatusStrip1.Location = New System.Drawing.Point(0, 341)
        Me.StatusStrip1.Name = "StatusStrip1"
        Me.StatusStrip1.Size = New System.Drawing.Size(452, 22)
        Me.StatusStrip1.TabIndex = 3
        Me.StatusStrip1.Text = "StatusStrip1"
        '
        'Status
        '
        Me.Status.Name = "Status"
        Me.Status.Size = New System.Drawing.Size(141, 17)
        Me.Status.Text = "Welcome to FlashcatUSB!"
        '
        'MainForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(452, 363)
        Me.Controls.Add(Me.StatusStrip1)
        Me.Controls.Add(Me.MyTabs)
        Me.Controls.Add(Me.MenuStrip1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MainMenuStrip = Me.MenuStrip1
        Me.Name = "MainForm"
        Me.Text = "FlashcatUSB"
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        Me.MyTabs.ResumeLayout(False)
        Me.TabStatus.ResumeLayout(False)
        CType(Me.pb_logo, System.ComponentModel.ISupportInitialize).EndInit()
        Me.TableLayoutPanel2.ResumeLayout(False)
        Me.TableLayoutPanel2.PerformLayout()
        Me.TabConsole.ResumeLayout(False)
        Me.TabConsole.PerformLayout()
        Me.AvrTab.ResumeLayout(False)
        Me.AvrTab.PerformLayout()
        Me.SpiTab.ResumeLayout(False)
        Me.SpiTab.PerformLayout()
        Me.cbSPI.ResumeLayout(False)
        Me.cbSPI.PerformLayout()
        Me.I2CTab.ResumeLayout(False)
        Me.I2CTab.PerformLayout()
        Me.GroupBox1.ResumeLayout(False)
        Me.StatusStrip1.ResumeLayout(False)
        Me.StatusStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents ContextMenuStrip1 As System.Windows.Forms.ContextMenuStrip
    Friend WithEvents MenuStrip1 As System.Windows.Forms.MenuStrip
    Friend WithEvents MainToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents MyTabs As System.Windows.Forms.TabControl
    Friend WithEvents TabStatus As System.Windows.Forms.TabPage
    Friend WithEvents TabConsole As System.Windows.Forms.TabPage
    Friend WithEvents StatusStrip1 As System.Windows.Forms.StatusStrip
    Friend WithEvents Status As System.Windows.Forms.ToolStripStatusLabel
    Friend WithEvents SettingsToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents miDetectDevice As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents VerifyMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ScriptToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents CurrentScript_MI As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents LoadScript_MI As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents UnloadScript_MI As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripSeparator1 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents ExitToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents lblStatus As System.Windows.Forms.Label
    Friend WithEvents TableLayoutPanel2 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents sm7 As System.Windows.Forms.Label
    Friend WithEvents sm6 As System.Windows.Forms.Label
    Friend WithEvents sm5 As System.Windows.Forms.Label
    Friend WithEvents sm4 As System.Windows.Forms.Label
    Friend WithEvents sm3 As System.Windows.Forms.Label
    Friend WithEvents sm2 As System.Windows.Forms.Label
    Friend WithEvents sm1 As System.Windows.Forms.Label
    Friend WithEvents AvrTab As System.Windows.Forms.TabPage
    Friend WithEvents DfuPbBar As System.Windows.Forms.ProgressBar
    Friend WithEvents SpiTab As System.Windows.Forms.TabPage
    Friend WithEvents ConsoleBox As System.Windows.Forms.ListBox
    Friend WithEvents cmdSaveLog As System.Windows.Forms.Button
    Friend WithEvents txtInput As System.Windows.Forms.TextBox
    Friend WithEvents lblAvrCrc As System.Windows.Forms.Label
    Friend WithEvents lblAvrRange As System.Windows.Forms.Label
    Friend WithEvents lblAvrFn As System.Windows.Forms.Label
    Friend WithEvents cmdAvrProg As System.Windows.Forms.Button
    Friend WithEvents cmdAvrStart As System.Windows.Forms.Button
    Friend WithEvents cmdAvrLoad As System.Windows.Forms.Button
    Friend WithEvents cbSPI As System.Windows.Forms.GroupBox
    Friend WithEvents cbSpiProgMode As System.Windows.Forms.ComboBox
    Friend WithEvents cbUseEnWS As System.Windows.Forms.CheckBox
    Friend WithEvents txtEnWS As System.Windows.Forms.TextBox
    Friend WithEvents lblSpiProgMode As System.Windows.Forms.Label
    Friend WithEvents lblSpiInfo As System.Windows.Forms.Label
    Friend WithEvents txtChipSize As System.Windows.Forms.TextBox
    Friend WithEvents lblSpiChipSize As System.Windows.Forms.Label
    Friend WithEvents txtChipErase As System.Windows.Forms.TextBox
    Friend WithEvents lblSpiRead As System.Windows.Forms.Label
    Friend WithEvents txtRead As System.Windows.Forms.TextBox
    Friend WithEvents txtWriteStatus As System.Windows.Forms.TextBox
    Friend WithEvents lblSpiSectorErase As System.Windows.Forms.Label
    Friend WithEvents lblSpiWriteStatus As System.Windows.Forms.Label
    Friend WithEvents txtSectorErase As System.Windows.Forms.TextBox
    Friend WithEvents txtReadStatus As System.Windows.Forms.TextBox
    Friend WithEvents lblSpiEraseSize As System.Windows.Forms.Label
    Friend WithEvents lblSpiReadStatus As System.Windows.Forms.Label
    Friend WithEvents txtEraseSize As System.Windows.Forms.TextBox
    Friend WithEvents txtWriteEnable As System.Windows.Forms.TextBox
    Friend WithEvents lblSpiWriteEn As System.Windows.Forms.Label
    Friend WithEvents txtPageProgram As System.Windows.Forms.TextBox
    Friend WithEvents lblSpiChipErase As System.Windows.Forms.Label
    Friend WithEvents lblSpiPageProgram As System.Windows.Forms.Label
    Friend WithEvents RadioUseSpiSettings As System.Windows.Forms.RadioButton
    Friend WithEvents RadioUseSpiAuto As System.Windows.Forms.RadioButton
    Friend WithEvents AvrEditor As FlashcatUSB.HexEditor
    Friend WithEvents LanguageToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents EnglishToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ChineseToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents FrenchToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents GermanToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents PortugueseToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents SpanishToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents pb_logo As System.Windows.Forms.PictureBox
    Friend WithEvents mi_tools_menu As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents EraseChipToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripSeparator2 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents miSPIMode As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents txtPageSize As System.Windows.Forms.TextBox
    Friend WithEvents miI2CMode As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents I2CTab As System.Windows.Forms.TabPage
    Friend WithEvents I2CEditor As FlashcatUSB.HexEditor
    Friend WithEvents cmdI2cWrite As System.Windows.Forms.Button
    Friend WithEvents cmdI2cRead As System.Windows.Forms.Button
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents cmdI2cConnect As System.Windows.Forms.Button
    Friend WithEvents cbEepromDevices As FlashcatUSB.CustomComboPlus.CustomComboPlus
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents cbI2C_A2 As System.Windows.Forms.CheckBox
    Friend WithEvents cbI2C_A1 As System.Windows.Forms.CheckBox
    Friend WithEvents cbI2C_A0 As System.Windows.Forms.CheckBox
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents pbI2C As System.Windows.Forms.ProgressBar
    Friend WithEvents miRepeatWrite As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripSeparator4 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents ToolStripMenuItem1 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents miSPINRF24LE1 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents miAT25128BT As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents miAT25256B As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents miAT25512 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents miSTM95010 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents miSTM95020 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents miSTM95040 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents miSTM95080 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripSeparator3 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents ToolStripSeparator5 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents miSTM95160 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents miSTM95320 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents miSTM95640 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents miSTM95128 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents miSTM95256 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents miSTM95512 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents miSTM95M01 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents miSTM95M02 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents miAT25010A As ToolStripMenuItem
    Friend WithEvents miAT25020A As ToolStripMenuItem
    Friend WithEvents miAT25040A As ToolStripMenuItem
    Friend WithEvents miAT25080 As ToolStripMenuItem
    Friend WithEvents miAT25160 As ToolStripMenuItem
    Friend WithEvents miAT25320 As ToolStripMenuItem
    Friend WithEvents miAT25640 As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator6 As ToolStripSeparator
    Friend WithEvents miM25AA160A As ToolStripMenuItem
    Friend WithEvents miM25AA160B As ToolStripMenuItem
    Friend WithEvents miSQIMode As ToolStripMenuItem
    Friend WithEvents miBytesPerPage_Normal As ToolStripMenuItem
    Friend WithEvents miBytesPerPage_Extended As ToolStripMenuItem
    Friend WithEvents mi_RefreshFlash As ToolStripMenuItem
    Friend WithEvents CreateNANDImageToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents WriteNANDImageToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents minRF24LUIP As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator7 As ToolStripSeparator
    Friend WithEvents mi_tools_seperate2 As ToolStripSeparator
    Friend WithEvents miFlashSeperator1 As ToolStripSeparator
    Friend WithEvents ToolStripMenuItem4 As ToolStripMenuItem
    Friend WithEvents mi_bitswap_none As ToolStripMenuItem
    Friend WithEvents mi_bitswap_4bit As ToolStripMenuItem
    Friend WithEvents mi_bitswap_8bit As ToolStripMenuItem
    Friend WithEvents mi_bitswap_16bit As ToolStripMenuItem
    Friend WithEvents ToolStripMenuItem2 As ToolStripMenuItem
    Friend WithEvents mi_bitendian_big As ToolStripMenuItem
    Friend WithEvents mi_bitendian_little_8 As ToolStripMenuItem
    Friend WithEvents mi_bitendian_little_16 As ToolStripMenuItem
    Friend WithEvents mi_bitendian_little_32 As ToolStripMenuItem
    Friend WithEvents ToolStripMenuItem3 As ToolStripMenuItem
    Friend WithEvents mi_NandBlockManager As ToolStripMenuItem
    Friend WithEvents mi_NandPreserve As ToolStripMenuItem
    Friend WithEvents mi_ext_mode As ToolStripMenuItem
    Friend WithEvents ToolStripMenuItem5 As ToolStripMenuItem
    Friend WithEvents mi_so44_normal As ToolStripMenuItem
    Friend WithEvents mi_so44_12v_write As ToolStripMenuItem
End Class
