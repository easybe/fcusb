﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MainForm))
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.mi_main_menu = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_detect = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator1 = New System.Windows.Forms.ToolStripSeparator()
        Me.mi_repeat = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_refresh = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator2 = New System.Windows.Forms.ToolStripSeparator()
        Me.mi_exit = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_mode_menu = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_mode_settings = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator3 = New System.Windows.Forms.ToolStripSeparator()
        Me.mi_verify = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_bit_swapping = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_bitswap_none = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_bitswap_8bit = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_bitswap_16bit = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_bitswap_32bit = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_endian = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_bitendian_big = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_bitendian_little_16 = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_bitendian_little_8 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator9 = New System.Windows.Forms.ToolStripSeparator()
        Me.mi_vpp = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_so44_normal = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_so44_12v_write = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator8 = New System.Windows.Forms.ToolStripSeparator()
        Me.mi_1V8 = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_3V3 = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_5V0 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator7 = New System.Windows.Forms.ToolStripSeparator()
        Me.mi_mode_spi = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_mode_spi_nand = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_mode_spieeprom = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_mode_i2c = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_mode_1wire = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_mode_extio = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_mode_jtag = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_script_menu = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_script_selected = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_script_load = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_script_unload = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_tools_menu = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_erase_tool = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator4 = New System.Windows.Forms.ToolStripSeparator()
        Me.mi_create_img = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_write_img = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator10 = New System.Windows.Forms.ToolStripSeparator()
        Me.mi_nand_map = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator5 = New System.Windows.Forms.ToolStripSeparator()
        Me.mi_device_features = New System.Windows.Forms.ToolStripMenuItem()
        Me.mi_Language = New System.Windows.Forms.ToolStripMenuItem()
        Me.FlashStatus = New System.Windows.Forms.StatusStrip()
        Me.FlashStatusLabel = New System.Windows.Forms.ToolStripStatusLabel()
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
        Me.sm1 = New System.Windows.Forms.Label()
        Me.lblStatus = New System.Windows.Forms.Label()
        Me.TabConsole = New System.Windows.Forms.TabPage()
        Me.cmd_console_copy = New System.Windows.Forms.Button()
        Me.cmd_console_clear = New System.Windows.Forms.Button()
        Me.cmdSaveLog = New System.Windows.Forms.Button()
        Me.txtInput = New System.Windows.Forms.TextBox()
        Me.ConsoleBox = New System.Windows.Forms.ListBox()
        Me.TabMultiDevice = New System.Windows.Forms.TabPage()
        Me.lbl_gang_info = New System.Windows.Forms.Label()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.pb_gang5 = New System.Windows.Forms.ProgressBar()
        Me.lbl_gang5 = New System.Windows.Forms.Label()
        Me.pb_gang4 = New System.Windows.Forms.ProgressBar()
        Me.lbl_gang4 = New System.Windows.Forms.Label()
        Me.pb_gang3 = New System.Windows.Forms.ProgressBar()
        Me.lbl_gang3 = New System.Windows.Forms.Label()
        Me.pb_gang2 = New System.Windows.Forms.ProgressBar()
        Me.lbl_gang2 = New System.Windows.Forms.Label()
        Me.pb_gang1 = New System.Windows.Forms.ProgressBar()
        Me.lbl_gang1 = New System.Windows.Forms.Label()
        Me.cmd_gang_erase = New System.Windows.Forms.Button()
        Me.cmd_gang_write = New System.Windows.Forms.Button()
        Me.MenuStrip1.SuspendLayout()
        Me.FlashStatus.SuspendLayout()
        Me.MyTabs.SuspendLayout()
        Me.TabStatus.SuspendLayout()
        CType(Me.pb_logo, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.TableLayoutPanel2.SuspendLayout()
        Me.TabConsole.SuspendLayout()
        Me.TabMultiDevice.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.SuspendLayout()
        '
        'MenuStrip1
        '
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mi_main_menu, Me.mi_mode_menu, Me.mi_script_menu, Me.mi_tools_menu, Me.mi_Language})
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Size = New System.Drawing.Size(469, 24)
        Me.MenuStrip1.TabIndex = 0
        Me.MenuStrip1.Text = "MenuStrip1"
        '
        'mi_main_menu
        '
        Me.mi_main_menu.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mi_detect, Me.ToolStripSeparator1, Me.mi_repeat, Me.mi_refresh, Me.ToolStripSeparator2, Me.mi_exit})
        Me.mi_main_menu.Name = "mi_main_menu"
        Me.mi_main_menu.Size = New System.Drawing.Size(46, 20)
        Me.mi_main_menu.Text = "Main"
        '
        'mi_detect
        '
        Me.mi_detect.Image = Global.FlashcatUSB.My.Resources.Resources.detect
        Me.mi_detect.Name = "mi_detect"
        Me.mi_detect.ShortcutKeys = System.Windows.Forms.Keys.F1
        Me.mi_detect.Size = New System.Drawing.Size(212, 22)
        Me.mi_detect.Text = "Detect (re-initialize)"
        '
        'ToolStripSeparator1
        '
        Me.ToolStripSeparator1.Name = "ToolStripSeparator1"
        Me.ToolStripSeparator1.Size = New System.Drawing.Size(209, 6)
        '
        'mi_repeat
        '
        Me.mi_repeat.Image = Global.FlashcatUSB.My.Resources.Resources.repeat
        Me.mi_repeat.Name = "mi_repeat"
        Me.mi_repeat.ShortcutKeys = System.Windows.Forms.Keys.F2
        Me.mi_repeat.Size = New System.Drawing.Size(212, 22)
        Me.mi_repeat.Text = "Repeat write operation"
        '
        'mi_refresh
        '
        Me.mi_refresh.Image = Global.FlashcatUSB.My.Resources.Resources.refresh
        Me.mi_refresh.Name = "mi_refresh"
        Me.mi_refresh.ShortcutKeys = System.Windows.Forms.Keys.F5
        Me.mi_refresh.Size = New System.Drawing.Size(212, 22)
        Me.mi_refresh.Text = "Refresh flash device"
        '
        'ToolStripSeparator2
        '
        Me.ToolStripSeparator2.Name = "ToolStripSeparator2"
        Me.ToolStripSeparator2.Size = New System.Drawing.Size(209, 6)
        '
        'mi_exit
        '
        Me.mi_exit.Image = Global.FlashcatUSB.My.Resources.Resources.ico_exit
        Me.mi_exit.Name = "mi_exit"
        Me.mi_exit.Size = New System.Drawing.Size(212, 22)
        Me.mi_exit.Text = "Exit"
        '
        'mi_mode_menu
        '
        Me.mi_mode_menu.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mi_mode_settings, Me.ToolStripSeparator3, Me.mi_verify, Me.mi_bit_swapping, Me.mi_endian, Me.ToolStripSeparator9, Me.mi_vpp, Me.ToolStripSeparator8, Me.mi_1V8, Me.mi_3V3, Me.mi_5V0, Me.ToolStripSeparator7, Me.mi_mode_spi, Me.mi_mode_spi_nand, Me.mi_mode_spieeprom, Me.mi_mode_i2c, Me.mi_mode_1wire, Me.mi_mode_extio, Me.mi_mode_jtag})
        Me.mi_mode_menu.Name = "mi_mode_menu"
        Me.mi_mode_menu.Size = New System.Drawing.Size(50, 20)
        Me.mi_mode_menu.Text = "Mode"
        '
        'mi_mode_settings
        '
        Me.mi_mode_settings.Image = Global.FlashcatUSB.My.Resources.Resources.config
        Me.mi_mode_settings.Name = "mi_mode_settings"
        Me.mi_mode_settings.Size = New System.Drawing.Size(180, 22)
        Me.mi_mode_settings.Text = "Protocol settings"
        '
        'ToolStripSeparator3
        '
        Me.ToolStripSeparator3.Name = "ToolStripSeparator3"
        Me.ToolStripSeparator3.Size = New System.Drawing.Size(177, 6)
        '
        'mi_verify
        '
        Me.mi_verify.Name = "mi_verify"
        Me.mi_verify.Size = New System.Drawing.Size(180, 22)
        Me.mi_verify.Text = "Verify programming"
        '
        'mi_bit_swapping
        '
        Me.mi_bit_swapping.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mi_bitswap_none, Me.mi_bitswap_8bit, Me.mi_bitswap_16bit, Me.mi_bitswap_32bit})
        Me.mi_bit_swapping.Image = Global.FlashcatUSB.My.Resources.Resources.binary
        Me.mi_bit_swapping.Name = "mi_bit_swapping"
        Me.mi_bit_swapping.Size = New System.Drawing.Size(180, 22)
        Me.mi_bit_swapping.Text = "Bit Swapping"
        '
        'mi_bitswap_none
        '
        Me.mi_bitswap_none.Name = "mi_bitswap_none"
        Me.mi_bitswap_none.Size = New System.Drawing.Size(170, 22)
        Me.mi_bitswap_none.Text = "None"
        '
        'mi_bitswap_8bit
        '
        Me.mi_bitswap_8bit.Name = "mi_bitswap_8bit"
        Me.mi_bitswap_8bit.Size = New System.Drawing.Size(170, 22)
        Me.mi_bitswap_8bit.Text = "8-bit (Byte)"
        '
        'mi_bitswap_16bit
        '
        Me.mi_bitswap_16bit.Name = "mi_bitswap_16bit"
        Me.mi_bitswap_16bit.Size = New System.Drawing.Size(170, 22)
        Me.mi_bitswap_16bit.Text = "16-bit (Half-word)"
        '
        'mi_bitswap_32bit
        '
        Me.mi_bitswap_32bit.Name = "mi_bitswap_32bit"
        Me.mi_bitswap_32bit.Size = New System.Drawing.Size(170, 22)
        Me.mi_bitswap_32bit.Text = "32-bit (Word)"
        '
        'mi_endian
        '
        Me.mi_endian.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mi_bitendian_big, Me.mi_bitendian_little_16, Me.mi_bitendian_little_8})
        Me.mi_endian.Image = Global.FlashcatUSB.My.Resources.Resources.binary
        Me.mi_endian.Name = "mi_endian"
        Me.mi_endian.Size = New System.Drawing.Size(180, 22)
        Me.mi_endian.Text = "Endian mode"
        '
        'mi_bitendian_big
        '
        Me.mi_bitendian_big.Name = "mi_bitendian_big"
        Me.mi_bitendian_big.Size = New System.Drawing.Size(186, 22)
        Me.mi_bitendian_big.Text = "Big Endian"
        '
        'mi_bitendian_little_16
        '
        Me.mi_bitendian_little_16.Name = "mi_bitendian_little_16"
        Me.mi_bitendian_little_16.Size = New System.Drawing.Size(186, 22)
        Me.mi_bitendian_little_16.Text = "Little Endian (16-bits)"
        '
        'mi_bitendian_little_8
        '
        Me.mi_bitendian_little_8.Name = "mi_bitendian_little_8"
        Me.mi_bitendian_little_8.Size = New System.Drawing.Size(186, 22)
        Me.mi_bitendian_little_8.Text = "Little Endian (8-bits)"
        '
        'ToolStripSeparator9
        '
        Me.ToolStripSeparator9.Name = "ToolStripSeparator9"
        Me.ToolStripSeparator9.Size = New System.Drawing.Size(177, 6)
        '
        'mi_vpp
        '
        Me.mi_vpp.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mi_so44_normal, Me.mi_so44_12v_write})
        Me.mi_vpp.Name = "mi_vpp"
        Me.mi_vpp.Size = New System.Drawing.Size(180, 22)
        Me.mi_vpp.Text = "VPP Setting"
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
        'ToolStripSeparator8
        '
        Me.ToolStripSeparator8.Name = "ToolStripSeparator8"
        Me.ToolStripSeparator8.Size = New System.Drawing.Size(177, 6)
        '
        'mi_1V8
        '
        Me.mi_1V8.Name = "mi_1V8"
        Me.mi_1V8.Size = New System.Drawing.Size(180, 22)
        Me.mi_1V8.Text = "Voltage (1.8v)"
        '
        'mi_3V3
        '
        Me.mi_3V3.Name = "mi_3V3"
        Me.mi_3V3.Size = New System.Drawing.Size(180, 22)
        Me.mi_3V3.Text = "Voltage (3.3v)"
        '
        'mi_5V0
        '
        Me.mi_5V0.Name = "mi_5V0"
        Me.mi_5V0.Size = New System.Drawing.Size(180, 22)
        Me.mi_5V0.Text = "Voltage (5.0v)"
        '
        'ToolStripSeparator7
        '
        Me.ToolStripSeparator7.Name = "ToolStripSeparator7"
        Me.ToolStripSeparator7.Size = New System.Drawing.Size(177, 6)
        '
        'mi_mode_spi
        '
        Me.mi_mode_spi.Name = "mi_mode_spi"
        Me.mi_mode_spi.Size = New System.Drawing.Size(180, 22)
        Me.mi_mode_spi.Text = "SPI NOR FLASH"
        '
        'mi_mode_spi_nand
        '
        Me.mi_mode_spi_nand.Name = "mi_mode_spi_nand"
        Me.mi_mode_spi_nand.Size = New System.Drawing.Size(180, 22)
        Me.mi_mode_spi_nand.Text = "SPI NAND FLASH"
        '
        'mi_mode_spieeprom
        '
        Me.mi_mode_spieeprom.Name = "mi_mode_spieeprom"
        Me.mi_mode_spieeprom.Size = New System.Drawing.Size(180, 22)
        Me.mi_mode_spieeprom.Text = "SPI EEPROM"
        '
        'mi_mode_i2c
        '
        Me.mi_mode_i2c.Name = "mi_mode_i2c"
        Me.mi_mode_i2c.Size = New System.Drawing.Size(180, 22)
        Me.mi_mode_i2c.Text = "I2C EEPROM"
        '
        'mi_mode_1wire
        '
        Me.mi_mode_1wire.Name = "mi_mode_1wire"
        Me.mi_mode_1wire.Size = New System.Drawing.Size(180, 22)
        Me.mi_mode_1wire.Text = "1-Wire EEPROM"
        '
        'mi_mode_extio
        '
        Me.mi_mode_extio.Name = "mi_mode_extio"
        Me.mi_mode_extio.Size = New System.Drawing.Size(180, 22)
        Me.mi_mode_extio.Text = "EXTENSION I/O"
        '
        'mi_mode_jtag
        '
        Me.mi_mode_jtag.Name = "mi_mode_jtag"
        Me.mi_mode_jtag.Size = New System.Drawing.Size(180, 22)
        Me.mi_mode_jtag.Text = "JTAG Mode"
        '
        'mi_script_menu
        '
        Me.mi_script_menu.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mi_script_selected, Me.mi_script_load, Me.mi_script_unload})
        Me.mi_script_menu.Name = "mi_script_menu"
        Me.mi_script_menu.Size = New System.Drawing.Size(49, 20)
        Me.mi_script_menu.Text = "Script"
        '
        'mi_script_selected
        '
        Me.mi_script_selected.Image = Global.FlashcatUSB.My.Resources.Resources.config
        Me.mi_script_selected.Name = "mi_script_selected"
        Me.mi_script_selected.Size = New System.Drawing.Size(152, 22)
        Me.mi_script_selected.Text = "Select script"
        '
        'mi_script_load
        '
        Me.mi_script_load.Image = Global.FlashcatUSB.My.Resources.Resources.openfile
        Me.mi_script_load.Name = "mi_script_load"
        Me.mi_script_load.Size = New System.Drawing.Size(152, 22)
        Me.mi_script_load.Text = "Load script"
        '
        'mi_script_unload
        '
        Me.mi_script_unload.Image = Global.FlashcatUSB.My.Resources.Resources.clear_x
        Me.mi_script_unload.Name = "mi_script_unload"
        Me.mi_script_unload.Size = New System.Drawing.Size(152, 22)
        Me.mi_script_unload.Text = "Unload script"
        '
        'mi_tools_menu
        '
        Me.mi_tools_menu.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mi_erase_tool, Me.ToolStripSeparator4, Me.mi_create_img, Me.mi_write_img, Me.ToolStripSeparator10, Me.mi_nand_map, Me.ToolStripSeparator5, Me.mi_device_features})
        Me.mi_tools_menu.Name = "mi_tools_menu"
        Me.mi_tools_menu.Size = New System.Drawing.Size(47, 20)
        Me.mi_tools_menu.Text = "Tools"
        '
        'mi_erase_tool
        '
        Me.mi_erase_tool.Image = Global.FlashcatUSB.My.Resources.Resources.erase_ico
        Me.mi_erase_tool.Name = "mi_erase_tool"
        Me.mi_erase_tool.Size = New System.Drawing.Size(183, 22)
        Me.mi_erase_tool.Text = "Erase chip"
        '
        'ToolStripSeparator4
        '
        Me.ToolStripSeparator4.Name = "ToolStripSeparator4"
        Me.ToolStripSeparator4.Size = New System.Drawing.Size(180, 6)
        '
        'mi_create_img
        '
        Me.mi_create_img.Image = Global.FlashcatUSB.My.Resources.Resources.download
        Me.mi_create_img.Name = "mi_create_img"
        Me.mi_create_img.Size = New System.Drawing.Size(183, 22)
        Me.mi_create_img.Text = "Create image"
        '
        'mi_write_img
        '
        Me.mi_write_img.Image = Global.FlashcatUSB.My.Resources.Resources.upload
        Me.mi_write_img.Name = "mi_write_img"
        Me.mi_write_img.Size = New System.Drawing.Size(183, 22)
        Me.mi_write_img.Text = "Write image"
        '
        'ToolStripSeparator10
        '
        Me.ToolStripSeparator10.Name = "ToolStripSeparator10"
        Me.ToolStripSeparator10.Size = New System.Drawing.Size(180, 6)
        '
        'mi_nand_map
        '
        Me.mi_nand_map.Image = Global.FlashcatUSB.My.Resources.Resources.globe
        Me.mi_nand_map.Name = "mi_nand_map"
        Me.mi_nand_map.Size = New System.Drawing.Size(183, 22)
        Me.mi_nand_map.Text = "NAND memory map"
        '
        'ToolStripSeparator5
        '
        Me.ToolStripSeparator5.Name = "ToolStripSeparator5"
        Me.ToolStripSeparator5.Size = New System.Drawing.Size(180, 6)
        '
        'mi_device_features
        '
        Me.mi_device_features.Image = Global.FlashcatUSB.My.Resources.Resources.config
        Me.mi_device_features.Name = "mi_device_features"
        Me.mi_device_features.Size = New System.Drawing.Size(183, 22)
        Me.mi_device_features.Text = "Vendor Features"
        '
        'mi_Language
        '
        Me.mi_Language.Name = "mi_Language"
        Me.mi_Language.Size = New System.Drawing.Size(71, 20)
        Me.mi_Language.Text = "Language"
        '
        'FlashStatus
        '
        Me.FlashStatus.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.FlashStatusLabel})
        Me.FlashStatus.Location = New System.Drawing.Point(0, 349)
        Me.FlashStatus.Name = "FlashStatus"
        Me.FlashStatus.Size = New System.Drawing.Size(469, 22)
        Me.FlashStatus.TabIndex = 1
        Me.FlashStatus.Text = "StatusStrip1"
        '
        'FlashStatusLabel
        '
        Me.FlashStatusLabel.Name = "FlashStatusLabel"
        Me.FlashStatusLabel.Size = New System.Drawing.Size(415, 17)
        Me.FlashStatusLabel.Text = "Welcome to FlashcatUSB Professional (SPI / I2C / JTAG Programing Software)!"
        '
        'MyTabs
        '
        Me.MyTabs.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.MyTabs.Controls.Add(Me.TabStatus)
        Me.MyTabs.Controls.Add(Me.TabConsole)
        Me.MyTabs.Controls.Add(Me.TabMultiDevice)
        Me.MyTabs.Location = New System.Drawing.Point(2, 24)
        Me.MyTabs.Name = "MyTabs"
        Me.MyTabs.SelectedIndex = 0
        Me.MyTabs.Size = New System.Drawing.Size(467, 322)
        Me.MyTabs.TabIndex = 2
        '
        'TabStatus
        '
        Me.TabStatus.BackColor = System.Drawing.SystemColors.Control
        Me.TabStatus.Controls.Add(Me.pb_logo)
        Me.TabStatus.Controls.Add(Me.TableLayoutPanel2)
        Me.TabStatus.Location = New System.Drawing.Point(4, 22)
        Me.TabStatus.Name = "TabStatus"
        Me.TabStatus.Padding = New System.Windows.Forms.Padding(3)
        Me.TabStatus.Size = New System.Drawing.Size(459, 296)
        Me.TabStatus.TabIndex = 0
        Me.TabStatus.Text = "  Status  "
        '
        'pb_logo
        '
        Me.pb_logo.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pb_logo.BackgroundImage = Global.FlashcatUSB.My.Resources.Resources.logo_ec
        Me.pb_logo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center
        Me.pb_logo.Location = New System.Drawing.Point(3, 190)
        Me.pb_logo.Name = "pb_logo"
        Me.pb_logo.Size = New System.Drawing.Size(450, 100)
        Me.pb_logo.TabIndex = 7
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
        Me.TableLayoutPanel2.Controls.Add(Me.sm1, 0, 1)
        Me.TableLayoutPanel2.Controls.Add(Me.lblStatus, 0, 0)
        Me.TableLayoutPanel2.Location = New System.Drawing.Point(2, 10)
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
        Me.TableLayoutPanel2.Size = New System.Drawing.Size(450, 165)
        Me.TableLayoutPanel2.TabIndex = 5
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
        'sm1
        '
        Me.sm1.AutoSize = True
        Me.sm1.Location = New System.Drawing.Point(3, 20)
        Me.sm1.Name = "sm1"
        Me.sm1.Size = New System.Drawing.Size(0, 13)
        Me.sm1.TabIndex = 4
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
        'TabConsole
        '
        Me.TabConsole.BackColor = System.Drawing.SystemColors.Control
        Me.TabConsole.Controls.Add(Me.cmd_console_copy)
        Me.TabConsole.Controls.Add(Me.cmd_console_clear)
        Me.TabConsole.Controls.Add(Me.cmdSaveLog)
        Me.TabConsole.Controls.Add(Me.txtInput)
        Me.TabConsole.Controls.Add(Me.ConsoleBox)
        Me.TabConsole.Location = New System.Drawing.Point(4, 22)
        Me.TabConsole.Name = "TabConsole"
        Me.TabConsole.Padding = New System.Windows.Forms.Padding(3)
        Me.TabConsole.Size = New System.Drawing.Size(459, 296)
        Me.TabConsole.TabIndex = 1
        Me.TabConsole.Text = "  Console  "
        '
        'cmd_console_copy
        '
        Me.cmd_console_copy.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmd_console_copy.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.cmd_console_copy.Image = Global.FlashcatUSB.My.Resources.Resources.clipboard_ico
        Me.cmd_console_copy.Location = New System.Drawing.Point(408, 271)
        Me.cmd_console_copy.Name = "cmd_console_copy"
        Me.cmd_console_copy.Size = New System.Drawing.Size(22, 22)
        Me.cmd_console_copy.TabIndex = 9
        Me.cmd_console_copy.UseVisualStyleBackColor = True
        '
        'cmd_console_clear
        '
        Me.cmd_console_clear.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmd_console_clear.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.cmd_console_clear.Image = Global.FlashcatUSB.My.Resources.Resources.clear_x
        Me.cmd_console_clear.Location = New System.Drawing.Point(382, 271)
        Me.cmd_console_clear.Name = "cmd_console_clear"
        Me.cmd_console_clear.Size = New System.Drawing.Size(22, 22)
        Me.cmd_console_clear.TabIndex = 8
        Me.cmd_console_clear.UseVisualStyleBackColor = True
        '
        'cmdSaveLog
        '
        Me.cmdSaveLog.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdSaveLog.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.cmdSaveLog.Image = CType(resources.GetObject("cmdSaveLog.Image"), System.Drawing.Image)
        Me.cmdSaveLog.Location = New System.Drawing.Point(434, 271)
        Me.cmdSaveLog.Name = "cmdSaveLog"
        Me.cmdSaveLog.Size = New System.Drawing.Size(22, 22)
        Me.cmdSaveLog.TabIndex = 7
        Me.cmdSaveLog.UseVisualStyleBackColor = True
        '
        'txtInput
        '
        Me.txtInput.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtInput.Location = New System.Drawing.Point(3, 273)
        Me.txtInput.Name = "txtInput"
        Me.txtInput.Size = New System.Drawing.Size(373, 20)
        Me.txtInput.TabIndex = 6
        '
        'ConsoleBox
        '
        Me.ConsoleBox.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ConsoleBox.FormattingEnabled = True
        Me.ConsoleBox.Location = New System.Drawing.Point(3, 3)
        Me.ConsoleBox.Name = "ConsoleBox"
        Me.ConsoleBox.Size = New System.Drawing.Size(453, 264)
        Me.ConsoleBox.TabIndex = 1
        '
        'TabMultiDevice
        '
        Me.TabMultiDevice.BackColor = System.Drawing.SystemColors.Control
        Me.TabMultiDevice.Controls.Add(Me.lbl_gang_info)
        Me.TabMultiDevice.Controls.Add(Me.GroupBox1)
        Me.TabMultiDevice.Controls.Add(Me.cmd_gang_erase)
        Me.TabMultiDevice.Controls.Add(Me.cmd_gang_write)
        Me.TabMultiDevice.Location = New System.Drawing.Point(4, 22)
        Me.TabMultiDevice.Name = "TabMultiDevice"
        Me.TabMultiDevice.Padding = New System.Windows.Forms.Padding(3)
        Me.TabMultiDevice.Size = New System.Drawing.Size(459, 296)
        Me.TabMultiDevice.TabIndex = 2
        Me.TabMultiDevice.Text = "  Multi-device  "
        '
        'lbl_gang_info
        '
        Me.lbl_gang_info.AutoSize = True
        Me.lbl_gang_info.Location = New System.Drawing.Point(104, 13)
        Me.lbl_gang_info.Name = "lbl_gang_info"
        Me.lbl_gang_info.Size = New System.Drawing.Size(239, 13)
        Me.lbl_gang_info.TabIndex = 5
        Me.lbl_gang_info.Text = "This is the multi-device programmer interface tool."
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.pb_gang5)
        Me.GroupBox1.Controls.Add(Me.lbl_gang5)
        Me.GroupBox1.Controls.Add(Me.pb_gang4)
        Me.GroupBox1.Controls.Add(Me.lbl_gang4)
        Me.GroupBox1.Controls.Add(Me.pb_gang3)
        Me.GroupBox1.Controls.Add(Me.lbl_gang3)
        Me.GroupBox1.Controls.Add(Me.pb_gang2)
        Me.GroupBox1.Controls.Add(Me.lbl_gang2)
        Me.GroupBox1.Controls.Add(Me.pb_gang1)
        Me.GroupBox1.Controls.Add(Me.lbl_gang1)
        Me.GroupBox1.Location = New System.Drawing.Point(8, 82)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(445, 134)
        Me.GroupBox1.TabIndex = 4
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Status"
        '
        'pb_gang5
        '
        Me.pb_gang5.Location = New System.Drawing.Point(286, 103)
        Me.pb_gang5.Name = "pb_gang5"
        Me.pb_gang5.Size = New System.Drawing.Size(153, 15)
        Me.pb_gang5.TabIndex = 10
        '
        'lbl_gang5
        '
        Me.lbl_gang5.AutoSize = True
        Me.lbl_gang5.Location = New System.Drawing.Point(6, 105)
        Me.lbl_gang5.Name = "lbl_gang5"
        Me.lbl_gang5.Size = New System.Drawing.Size(127, 13)
        Me.lbl_gang5.TabIndex = 11
        Me.lbl_gang5.Text = "Device 5: Not connected"
        '
        'pb_gang4
        '
        Me.pb_gang4.Location = New System.Drawing.Point(286, 82)
        Me.pb_gang4.Name = "pb_gang4"
        Me.pb_gang4.Size = New System.Drawing.Size(153, 15)
        Me.pb_gang4.TabIndex = 8
        '
        'lbl_gang4
        '
        Me.lbl_gang4.AutoSize = True
        Me.lbl_gang4.Location = New System.Drawing.Point(6, 84)
        Me.lbl_gang4.Name = "lbl_gang4"
        Me.lbl_gang4.Size = New System.Drawing.Size(127, 13)
        Me.lbl_gang4.TabIndex = 9
        Me.lbl_gang4.Text = "Device 4: Not connected"
        '
        'pb_gang3
        '
        Me.pb_gang3.Location = New System.Drawing.Point(286, 61)
        Me.pb_gang3.Name = "pb_gang3"
        Me.pb_gang3.Size = New System.Drawing.Size(153, 15)
        Me.pb_gang3.TabIndex = 6
        '
        'lbl_gang3
        '
        Me.lbl_gang3.AutoSize = True
        Me.lbl_gang3.Location = New System.Drawing.Point(6, 63)
        Me.lbl_gang3.Name = "lbl_gang3"
        Me.lbl_gang3.Size = New System.Drawing.Size(127, 13)
        Me.lbl_gang3.TabIndex = 7
        Me.lbl_gang3.Text = "Device 3: Not connected"
        '
        'pb_gang2
        '
        Me.pb_gang2.Location = New System.Drawing.Point(286, 40)
        Me.pb_gang2.Name = "pb_gang2"
        Me.pb_gang2.Size = New System.Drawing.Size(153, 15)
        Me.pb_gang2.TabIndex = 4
        '
        'lbl_gang2
        '
        Me.lbl_gang2.AutoSize = True
        Me.lbl_gang2.Location = New System.Drawing.Point(6, 42)
        Me.lbl_gang2.Name = "lbl_gang2"
        Me.lbl_gang2.Size = New System.Drawing.Size(127, 13)
        Me.lbl_gang2.TabIndex = 5
        Me.lbl_gang2.Text = "Device 2: Not connected"
        '
        'pb_gang1
        '
        Me.pb_gang1.Location = New System.Drawing.Point(286, 19)
        Me.pb_gang1.Name = "pb_gang1"
        Me.pb_gang1.Size = New System.Drawing.Size(153, 15)
        Me.pb_gang1.TabIndex = 2
        '
        'lbl_gang1
        '
        Me.lbl_gang1.AutoSize = True
        Me.lbl_gang1.Location = New System.Drawing.Point(6, 21)
        Me.lbl_gang1.Name = "lbl_gang1"
        Me.lbl_gang1.Size = New System.Drawing.Size(127, 13)
        Me.lbl_gang1.TabIndex = 3
        Me.lbl_gang1.Text = "Device 1: Not connected"
        '
        'cmd_gang_erase
        '
        Me.cmd_gang_erase.Location = New System.Drawing.Point(139, 42)
        Me.cmd_gang_erase.Name = "cmd_gang_erase"
        Me.cmd_gang_erase.Size = New System.Drawing.Size(75, 23)
        Me.cmd_gang_erase.TabIndex = 1
        Me.cmd_gang_erase.Text = "Erase"
        Me.cmd_gang_erase.UseVisualStyleBackColor = True
        '
        'cmd_gang_write
        '
        Me.cmd_gang_write.Location = New System.Drawing.Point(220, 42)
        Me.cmd_gang_write.Name = "cmd_gang_write"
        Me.cmd_gang_write.Size = New System.Drawing.Size(75, 23)
        Me.cmd_gang_write.TabIndex = 0
        Me.cmd_gang_write.Text = "Write"
        Me.cmd_gang_write.UseVisualStyleBackColor = True
        '
        'MainForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(469, 371)
        Me.Controls.Add(Me.MyTabs)
        Me.Controls.Add(Me.FlashStatus)
        Me.Controls.Add(Me.MenuStrip1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MainMenuStrip = Me.MenuStrip1
        Me.Name = "MainForm"
        Me.Text = "FlashcatUSB"
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        Me.FlashStatus.ResumeLayout(False)
        Me.FlashStatus.PerformLayout()
        Me.MyTabs.ResumeLayout(False)
        Me.TabStatus.ResumeLayout(False)
        CType(Me.pb_logo, System.ComponentModel.ISupportInitialize).EndInit()
        Me.TableLayoutPanel2.ResumeLayout(False)
        Me.TableLayoutPanel2.PerformLayout()
        Me.TabConsole.ResumeLayout(False)
        Me.TabConsole.PerformLayout()
        Me.TabMultiDevice.ResumeLayout(False)
        Me.TabMultiDevice.PerformLayout()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents MenuStrip1 As MenuStrip
    Friend WithEvents mi_main_menu As ToolStripMenuItem
    Friend WithEvents FlashStatus As StatusStrip
    Friend WithEvents FlashStatusLabel As ToolStripStatusLabel
    Friend WithEvents MyTabs As TabControl
    Friend WithEvents TabStatus As TabPage
    Friend WithEvents pb_logo As PictureBox
    Friend WithEvents TableLayoutPanel2 As TableLayoutPanel
    Friend WithEvents sm7 As Label
    Friend WithEvents sm6 As Label
    Friend WithEvents sm5 As Label
    Friend WithEvents sm4 As Label
    Friend WithEvents sm3 As Label
    Friend WithEvents sm2 As Label
    Friend WithEvents lblStatus As Label
    Friend WithEvents sm1 As Label
    Friend WithEvents TabConsole As TabPage
    Friend WithEvents cmdSaveLog As Button
    Friend WithEvents txtInput As TextBox
    Friend WithEvents ConsoleBox As ListBox
    Friend WithEvents mi_detect As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator1 As ToolStripSeparator
    Friend WithEvents mi_repeat As ToolStripMenuItem
    Friend WithEvents mi_refresh As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator2 As ToolStripSeparator
    Friend WithEvents mi_exit As ToolStripMenuItem
    Friend WithEvents mi_mode_menu As ToolStripMenuItem
    Friend WithEvents mi_script_menu As ToolStripMenuItem
    Friend WithEvents mi_Language As ToolStripMenuItem
    Friend WithEvents mi_mode_settings As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator3 As ToolStripSeparator
    Friend WithEvents mi_mode_spi As ToolStripMenuItem
    Friend WithEvents mi_mode_jtag As ToolStripMenuItem
    Friend WithEvents mi_mode_i2c As ToolStripMenuItem
    Friend WithEvents mi_mode_spieeprom As ToolStripMenuItem
    Friend WithEvents mi_mode_extio As ToolStripMenuItem
    Friend WithEvents mi_script_selected As ToolStripMenuItem
    Friend WithEvents mi_script_load As ToolStripMenuItem
    Friend WithEvents mi_script_unload As ToolStripMenuItem
    Friend WithEvents mi_1V8 As ToolStripMenuItem
    Friend WithEvents mi_3V3 As ToolStripMenuItem
    Friend WithEvents mi_5V0 As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator7 As ToolStripSeparator
    Friend WithEvents cmd_console_clear As Button
    Friend WithEvents mi_verify As ToolStripMenuItem
    Friend WithEvents mi_bit_swapping As ToolStripMenuItem
    Friend WithEvents mi_bitswap_none As ToolStripMenuItem
    Friend WithEvents mi_bitswap_8bit As ToolStripMenuItem
    Friend WithEvents mi_bitswap_16bit As ToolStripMenuItem
    Friend WithEvents mi_bitswap_32bit As ToolStripMenuItem
    Friend WithEvents mi_endian As ToolStripMenuItem
    Friend WithEvents mi_bitendian_big As ToolStripMenuItem
    Friend WithEvents mi_bitendian_little_8 As ToolStripMenuItem
    Friend WithEvents mi_bitendian_little_16 As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator8 As ToolStripSeparator
    Friend WithEvents ToolStripSeparator9 As ToolStripSeparator
    Friend WithEvents mi_vpp As ToolStripMenuItem
    Friend WithEvents mi_so44_normal As ToolStripMenuItem
    Friend WithEvents mi_so44_12v_write As ToolStripMenuItem
    Friend WithEvents mi_mode_1wire As ToolStripMenuItem
    Friend WithEvents mi_tools_menu As ToolStripMenuItem
    Friend WithEvents mi_create_img As ToolStripMenuItem
    Friend WithEvents mi_write_img As ToolStripMenuItem
    Friend WithEvents cmd_console_copy As Button
    Friend WithEvents ToolStripSeparator10 As ToolStripSeparator
    Friend WithEvents mi_nand_map As ToolStripMenuItem
    Friend WithEvents mi_erase_tool As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator4 As ToolStripSeparator
    Friend WithEvents ToolStripSeparator5 As ToolStripSeparator
    Friend WithEvents mi_device_features As ToolStripMenuItem
    Friend WithEvents TabMultiDevice As TabPage
    Friend WithEvents lbl_gang_info As Label
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents pb_gang5 As ProgressBar
    Friend WithEvents lbl_gang5 As Label
    Friend WithEvents pb_gang4 As ProgressBar
    Friend WithEvents lbl_gang4 As Label
    Friend WithEvents pb_gang3 As ProgressBar
    Friend WithEvents lbl_gang3 As Label
    Friend WithEvents pb_gang2 As ProgressBar
    Friend WithEvents lbl_gang2 As Label
    Friend WithEvents pb_gang1 As ProgressBar
    Friend WithEvents lbl_gang1 As Label
    Friend WithEvents cmd_gang_erase As Button
    Friend WithEvents cmd_gang_write As Button
    Friend WithEvents mi_mode_spi_nand As ToolStripMenuItem
End Class