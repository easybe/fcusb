﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FrmSettings
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
        Me.MyTabs = New System.Windows.Forms.TabControl()
        Me.TP_SPI = New System.Windows.Forms.TabPage()
        Me.group_custom = New System.Windows.Forms.GroupBox()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.cbEN4B = New System.Windows.Forms.CheckBox()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.cb_addr_size = New System.Windows.Forms.ComboBox()
        Me.cbENWS = New System.Windows.Forms.CheckBox()
        Me.Label14 = New System.Windows.Forms.Label()
        Me.Label15 = New System.Windows.Forms.Label()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.op_ewsr = New System.Windows.Forms.ComboBox()
        Me.op_ws = New System.Windows.Forms.ComboBox()
        Me.op_rs = New System.Windows.Forms.ComboBox()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.cb_spare = New System.Windows.Forms.ComboBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.cb_prog_mode = New System.Windows.Forms.ComboBox()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.op_ce = New System.Windows.Forms.ComboBox()
        Me.cb_page_size = New System.Windows.Forms.ComboBox()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.lblSpiProgMode = New System.Windows.Forms.Label()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.cb_erase_size = New System.Windows.Forms.ComboBox()
        Me.cb_chip_size = New System.Windows.Forms.ComboBox()
        Me.op_we = New System.Windows.Forms.ComboBox()
        Me.lblSpiChipSize = New System.Windows.Forms.Label()
        Me.lblSpiEraseSize = New System.Windows.Forms.Label()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.op_read = New System.Windows.Forms.ComboBox()
        Me.op_sectorerase = New System.Windows.Forms.ComboBox()
        Me.op_prog = New System.Windows.Forms.ComboBox()
        Me.RadioUseSpiSettings = New System.Windows.Forms.RadioButton()
        Me.RadioUseSpiAuto = New System.Windows.Forms.RadioButton()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.cb_spi_quad = New System.Windows.Forms.CheckBox()
        Me.cb_spi_pro_clock = New System.Windows.Forms.ComboBox()
        Me.Label16 = New System.Windows.Forms.Label()
        Me.cb_spi_eeprom = New System.Windows.Forms.ComboBox()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.lbl_read_cmd = New System.Windows.Forms.Label()
        Me.cb_spi_clock = New System.Windows.Forms.ComboBox()
        Me.rb_fastread_op = New System.Windows.Forms.RadioButton()
        Me.rb_read_op = New System.Windows.Forms.RadioButton()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.TP_JTAG = New System.Windows.Forms.TabPage()
        Me.TP_I2C = New System.Windows.Forms.TabPage()
        Me.GroupBox6 = New System.Windows.Forms.GroupBox()
        Me.cbi2cDensity = New System.Windows.Forms.ComboBox()
        Me.GroupBox5 = New System.Windows.Forms.GroupBox()
        Me.cbI2C_A0 = New System.Windows.Forms.CheckBox()
        Me.cbI2C_A1 = New System.Windows.Forms.CheckBox()
        Me.cbI2C_A2 = New System.Windows.Forms.CheckBox()
        Me.GroupBox4 = New System.Windows.Forms.GroupBox()
        Me.rb_speed_1mhz = New System.Windows.Forms.RadioButton()
        Me.rb_speed_400khz = New System.Windows.Forms.RadioButton()
        Me.rb_speed_100khz = New System.Windows.Forms.RadioButton()
        Me.TP_NAND = New System.Windows.Forms.TabPage()
        Me.gb_block_layout = New System.Windows.Forms.GroupBox()
        Me.nand_box = New System.Windows.Forms.PictureBox()
        Me.rb_mainspare_all = New System.Windows.Forms.RadioButton()
        Me.rb_mainspare_segmented = New System.Windows.Forms.RadioButton()
        Me.rb_mainspare_default = New System.Windows.Forms.RadioButton()
        Me.gb_block_manager = New System.Windows.Forms.GroupBox()
        Me.cb_badmarker_6th_page2 = New System.Windows.Forms.CheckBox()
        Me.cb_badmarker_6th_page1 = New System.Windows.Forms.CheckBox()
        Me.lbl_6th_byte = New System.Windows.Forms.Label()
        Me.lbl_1st_byte = New System.Windows.Forms.Label()
        Me.cb_badmarker_1st_lastpage = New System.Windows.Forms.CheckBox()
        Me.cb_badmarker_1st_page2 = New System.Windows.Forms.CheckBox()
        Me.cb_badmarker_1st_page1 = New System.Windows.Forms.CheckBox()
        Me.cb_badblock_enabled = New System.Windows.Forms.RadioButton()
        Me.cb_badblock_disabled = New System.Windows.Forms.RadioButton()
        Me.gb_nand_general = New System.Windows.Forms.GroupBox()
        Me.cb_nand_image_readverify = New System.Windows.Forms.CheckBox()
        Me.cb_spinand_disable_ecc = New System.Windows.Forms.CheckBox()
        Me.cb_mismatch = New System.Windows.Forms.CheckBox()
        Me.cb_preserve = New System.Windows.Forms.CheckBox()
        Me.MyTabs.SuspendLayout()
        Me.TP_SPI.SuspendLayout()
        Me.group_custom.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.TP_I2C.SuspendLayout()
        Me.GroupBox6.SuspendLayout()
        Me.GroupBox5.SuspendLayout()
        Me.GroupBox4.SuspendLayout()
        Me.TP_NAND.SuspendLayout()
        Me.gb_block_layout.SuspendLayout()
        CType(Me.nand_box, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.gb_block_manager.SuspendLayout()
        Me.gb_nand_general.SuspendLayout()
        Me.SuspendLayout()
        '
        'MyTabs
        '
        Me.MyTabs.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.MyTabs.Controls.Add(Me.TP_SPI)
        Me.MyTabs.Controls.Add(Me.TP_JTAG)
        Me.MyTabs.Controls.Add(Me.TP_I2C)
        Me.MyTabs.Controls.Add(Me.TP_NAND)
        Me.MyTabs.Location = New System.Drawing.Point(6, 6)
        Me.MyTabs.Name = "MyTabs"
        Me.MyTabs.SelectedIndex = 0
        Me.MyTabs.Size = New System.Drawing.Size(459, 354)
        Me.MyTabs.TabIndex = 0
        '
        'TP_SPI
        '
        Me.TP_SPI.BackColor = System.Drawing.SystemColors.Control
        Me.TP_SPI.Controls.Add(Me.group_custom)
        Me.TP_SPI.Controls.Add(Me.RadioUseSpiSettings)
        Me.TP_SPI.Controls.Add(Me.RadioUseSpiAuto)
        Me.TP_SPI.Controls.Add(Me.GroupBox1)
        Me.TP_SPI.Location = New System.Drawing.Point(4, 22)
        Me.TP_SPI.Name = "TP_SPI"
        Me.TP_SPI.Padding = New System.Windows.Forms.Padding(3)
        Me.TP_SPI.Size = New System.Drawing.Size(451, 328)
        Me.TP_SPI.TabIndex = 0
        Me.TP_SPI.Text = " SPI "
        '
        'group_custom
        '
        Me.group_custom.Controls.Add(Me.Label13)
        Me.group_custom.Controls.Add(Me.cbEN4B)
        Me.group_custom.Controls.Add(Me.Label6)
        Me.group_custom.Controls.Add(Me.cb_addr_size)
        Me.group_custom.Controls.Add(Me.cbENWS)
        Me.group_custom.Controls.Add(Me.Label14)
        Me.group_custom.Controls.Add(Me.Label15)
        Me.group_custom.Controls.Add(Me.Label12)
        Me.group_custom.Controls.Add(Me.op_ewsr)
        Me.group_custom.Controls.Add(Me.op_ws)
        Me.group_custom.Controls.Add(Me.op_rs)
        Me.group_custom.Controls.Add(Me.Label11)
        Me.group_custom.Controls.Add(Me.cb_spare)
        Me.group_custom.Controls.Add(Me.Label5)
        Me.group_custom.Controls.Add(Me.cb_prog_mode)
        Me.group_custom.Controls.Add(Me.Label10)
        Me.group_custom.Controls.Add(Me.Label4)
        Me.group_custom.Controls.Add(Me.op_ce)
        Me.group_custom.Controls.Add(Me.cb_page_size)
        Me.group_custom.Controls.Add(Me.Label9)
        Me.group_custom.Controls.Add(Me.lblSpiProgMode)
        Me.group_custom.Controls.Add(Me.Label8)
        Me.group_custom.Controls.Add(Me.cb_erase_size)
        Me.group_custom.Controls.Add(Me.cb_chip_size)
        Me.group_custom.Controls.Add(Me.op_we)
        Me.group_custom.Controls.Add(Me.lblSpiChipSize)
        Me.group_custom.Controls.Add(Me.lblSpiEraseSize)
        Me.group_custom.Controls.Add(Me.Label7)
        Me.group_custom.Controls.Add(Me.op_read)
        Me.group_custom.Controls.Add(Me.op_sectorerase)
        Me.group_custom.Controls.Add(Me.op_prog)
        Me.group_custom.Location = New System.Drawing.Point(6, 146)
        Me.group_custom.Name = "group_custom"
        Me.group_custom.Size = New System.Drawing.Size(439, 176)
        Me.group_custom.TabIndex = 28
        Me.group_custom.TabStop = False
        Me.group_custom.Text = "Operation commands"
        '
        'Label13
        '
        Me.Label13.AutoSize = True
        Me.Label13.Location = New System.Drawing.Point(8, 153)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(108, 13)
        Me.Label13.TabIndex = 63
        Me.Label13.Text = "Use operation codes:"
        '
        'cbEN4B
        '
        Me.cbEN4B.AutoSize = True
        Me.cbEN4B.Location = New System.Drawing.Point(202, 151)
        Me.cbEN4B.Name = "cbEN4B"
        Me.cbEN4B.Size = New System.Drawing.Size(54, 17)
        Me.cbEN4B.TabIndex = 62
        Me.cbEN4B.Text = "EN4B"
        Me.cbEN4B.UseVisualStyleBackColor = True
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(254, 25)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(66, 13)
        Me.Label6.TabIndex = 60
        Me.Label6.Text = "Address size"
        '
        'cb_addr_size
        '
        Me.cb_addr_size.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_addr_size.FormattingEnabled = True
        Me.cb_addr_size.Items.AddRange(New Object() {"16-bit", "24-bit", "32-bit"})
        Me.cb_addr_size.Location = New System.Drawing.Point(257, 43)
        Me.cb_addr_size.Name = "cb_addr_size"
        Me.cb_addr_size.Size = New System.Drawing.Size(65, 21)
        Me.cb_addr_size.TabIndex = 61
        '
        'cbENWS
        '
        Me.cbENWS.AutoSize = True
        Me.cbENWS.Location = New System.Drawing.Point(122, 151)
        Me.cbENWS.Name = "cbENWS"
        Me.cbENWS.Size = New System.Drawing.Size(59, 17)
        Me.cbENWS.TabIndex = 13
        Me.cbENWS.Text = "EWSR"
        Me.cbENWS.UseVisualStyleBackColor = True
        '
        'Label14
        '
        Me.Label14.AutoSize = True
        Me.Label14.Location = New System.Drawing.Point(280, 100)
        Me.Label14.Name = "Label14"
        Me.Label14.Size = New System.Drawing.Size(40, 13)
        Me.Label14.TabIndex = 59
        Me.Label14.Text = "EWRS"
        '
        'Label15
        '
        Me.Label15.AutoSize = True
        Me.Label15.Location = New System.Drawing.Point(280, 76)
        Me.Label15.Name = "Label15"
        Me.Label15.Size = New System.Drawing.Size(65, 13)
        Me.Label15.TabIndex = 58
        Me.Label15.Text = "Write Status"
        '
        'Label12
        '
        Me.Label12.AutoSize = True
        Me.Label12.Location = New System.Drawing.Point(146, 127)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(66, 13)
        Me.Label12.TabIndex = 53
        Me.Label12.Text = "Read Status"
        '
        'op_ewsr
        '
        Me.op_ewsr.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.op_ewsr.FormattingEnabled = True
        Me.op_ewsr.Location = New System.Drawing.Point(372, 97)
        Me.op_ewsr.Name = "op_ewsr"
        Me.op_ewsr.Size = New System.Drawing.Size(51, 21)
        Me.op_ewsr.TabIndex = 56
        '
        'op_ws
        '
        Me.op_ws.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.op_ws.FormattingEnabled = True
        Me.op_ws.Location = New System.Drawing.Point(372, 70)
        Me.op_ws.Name = "op_ws"
        Me.op_ws.Size = New System.Drawing.Size(51, 21)
        Me.op_ws.TabIndex = 55
        '
        'op_rs
        '
        Me.op_rs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.op_rs.FormattingEnabled = True
        Me.op_rs.Location = New System.Drawing.Point(223, 124)
        Me.op_rs.Name = "op_rs"
        Me.op_rs.Size = New System.Drawing.Size(51, 21)
        Me.op_rs.TabIndex = 54
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.Location = New System.Drawing.Point(146, 100)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(58, 13)
        Me.Label11.TabIndex = 51
        Me.Label11.Text = "Chip Erase"
        '
        'cb_spare
        '
        Me.cb_spare.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_spare.FormattingEnabled = True
        Me.cb_spare.Items.AddRange(New Object() {"0 bits", "8 bits", "16 bits", "32 bits", "64 bits", "128 bits"})
        Me.cb_spare.Location = New System.Drawing.Point(363, 124)
        Me.cb_spare.Name = "cb_spare"
        Me.cb_spare.Size = New System.Drawing.Size(60, 21)
        Me.cb_spare.TabIndex = 40
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(280, 127)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(79, 13)
        Me.Label5.TabIndex = 39
        Me.Label5.Text = "Extended page"
        '
        'cb_prog_mode
        '
        Me.cb_prog_mode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_prog_mode.FormattingEnabled = True
        Me.cb_prog_mode.Items.AddRange(New Object() {"Page", "AAI (BYTE)", "AAI (WORD)", "AT45 Series"})
        Me.cb_prog_mode.Location = New System.Drawing.Point(333, 43)
        Me.cb_prog_mode.Name = "cb_prog_mode"
        Me.cb_prog_mode.Size = New System.Drawing.Size(90, 21)
        Me.cb_prog_mode.TabIndex = 34
        '
        'Label10
        '
        Me.Label10.AutoSize = True
        Me.Label10.Location = New System.Drawing.Point(146, 76)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(68, 13)
        Me.Label10.TabIndex = 49
        Me.Label10.Text = "Write Enable"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(169, 25)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(55, 13)
        Me.Label4.TabIndex = 34
        Me.Label4.Text = "Page Size"
        '
        'op_ce
        '
        Me.op_ce.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.op_ce.FormattingEnabled = True
        Me.op_ce.Location = New System.Drawing.Point(223, 97)
        Me.op_ce.Name = "op_ce"
        Me.op_ce.Size = New System.Drawing.Size(51, 21)
        Me.op_ce.TabIndex = 52
        '
        'cb_page_size
        '
        Me.cb_page_size.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_page_size.FormattingEnabled = True
        Me.cb_page_size.Items.AddRange(New Object() {"8 bytes", "16 bytes", "32 bytes", "64 bytes", "128 bytes", "256 bytes", "512 bytes", "1024 bytes"})
        Me.cb_page_size.Location = New System.Drawing.Point(172, 43)
        Me.cb_page_size.Name = "cb_page_size"
        Me.cb_page_size.Size = New System.Drawing.Size(77, 21)
        Me.cb_page_size.TabIndex = 38
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(8, 127)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(68, 13)
        Me.Label9.TabIndex = 47
        Me.Label9.Text = "Sector Erase"
        '
        'lblSpiProgMode
        '
        Me.lblSpiProgMode.AutoSize = True
        Me.lblSpiProgMode.Location = New System.Drawing.Point(330, 25)
        Me.lblSpiProgMode.Name = "lblSpiProgMode"
        Me.lblSpiProgMode.Size = New System.Drawing.Size(76, 13)
        Me.lblSpiProgMode.TabIndex = 27
        Me.lblSpiProgMode.Text = "Program Mode"
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(8, 100)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(46, 13)
        Me.Label8.TabIndex = 45
        Me.Label8.Text = "Program"
        '
        'cb_erase_size
        '
        Me.cb_erase_size.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_erase_size.FormattingEnabled = True
        Me.cb_erase_size.Items.AddRange(New Object() {"(Disabled)", "4 KB", "8 KB", "16 KB", "32 KB", "64 KB", "128 KB", "256 KB"})
        Me.cb_erase_size.Location = New System.Drawing.Point(89, 43)
        Me.cb_erase_size.Name = "cb_erase_size"
        Me.cb_erase_size.Size = New System.Drawing.Size(77, 21)
        Me.cb_erase_size.TabIndex = 36
        '
        'cb_chip_size
        '
        Me.cb_chip_size.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_chip_size.FormattingEnabled = True
        Me.cb_chip_size.Items.AddRange(New Object() {"1 Mbit", "2 Mbit", "4 Mbit", "8 Mbit", "16 Mbit", "32 Mbit", "64 Mbit", "128 Mbit", "256 Mbit", "512 Mbit", "1 Gbit", "2 Gbit"})
        Me.cb_chip_size.Location = New System.Drawing.Point(6, 43)
        Me.cb_chip_size.Name = "cb_chip_size"
        Me.cb_chip_size.Size = New System.Drawing.Size(77, 21)
        Me.cb_chip_size.TabIndex = 34
        '
        'op_we
        '
        Me.op_we.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.op_we.FormattingEnabled = True
        Me.op_we.Location = New System.Drawing.Point(223, 70)
        Me.op_we.Name = "op_we"
        Me.op_we.Size = New System.Drawing.Size(51, 21)
        Me.op_we.TabIndex = 50
        '
        'lblSpiChipSize
        '
        Me.lblSpiChipSize.AutoSize = True
        Me.lblSpiChipSize.Location = New System.Drawing.Point(3, 25)
        Me.lblSpiChipSize.Name = "lblSpiChipSize"
        Me.lblSpiChipSize.Size = New System.Drawing.Size(51, 13)
        Me.lblSpiChipSize.TabIndex = 7
        Me.lblSpiChipSize.Text = "Chip Size"
        '
        'lblSpiEraseSize
        '
        Me.lblSpiEraseSize.AutoSize = True
        Me.lblSpiEraseSize.Location = New System.Drawing.Point(86, 25)
        Me.lblSpiEraseSize.Name = "lblSpiEraseSize"
        Me.lblSpiEraseSize.Size = New System.Drawing.Size(57, 13)
        Me.lblSpiEraseSize.TabIndex = 5
        Me.lblSpiEraseSize.Text = "Erase Size"
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(8, 76)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(57, 13)
        Me.Label7.TabIndex = 43
        Me.Label7.Text = "Read data"
        '
        'op_read
        '
        Me.op_read.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.op_read.FormattingEnabled = True
        Me.op_read.Location = New System.Drawing.Point(89, 70)
        Me.op_read.Name = "op_read"
        Me.op_read.Size = New System.Drawing.Size(51, 21)
        Me.op_read.TabIndex = 44
        '
        'op_sectorerase
        '
        Me.op_sectorerase.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.op_sectorerase.FormattingEnabled = True
        Me.op_sectorerase.Location = New System.Drawing.Point(89, 124)
        Me.op_sectorerase.Name = "op_sectorerase"
        Me.op_sectorerase.Size = New System.Drawing.Size(51, 21)
        Me.op_sectorerase.TabIndex = 48
        '
        'op_prog
        '
        Me.op_prog.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.op_prog.FormattingEnabled = True
        Me.op_prog.Location = New System.Drawing.Point(89, 97)
        Me.op_prog.Name = "op_prog"
        Me.op_prog.Size = New System.Drawing.Size(51, 21)
        Me.op_prog.TabIndex = 46
        '
        'RadioUseSpiSettings
        '
        Me.RadioUseSpiSettings.AutoSize = True
        Me.RadioUseSpiSettings.Location = New System.Drawing.Point(253, 123)
        Me.RadioUseSpiSettings.Name = "RadioUseSpiSettings"
        Me.RadioUseSpiSettings.Size = New System.Drawing.Size(112, 17)
        Me.RadioUseSpiSettings.TabIndex = 27
        Me.RadioUseSpiSettings.Text = "Use these settings"
        Me.RadioUseSpiSettings.UseVisualStyleBackColor = True
        '
        'RadioUseSpiAuto
        '
        Me.RadioUseSpiAuto.AutoSize = True
        Me.RadioUseSpiAuto.Checked = True
        Me.RadioUseSpiAuto.Location = New System.Drawing.Point(41, 123)
        Me.RadioUseSpiAuto.Name = "RadioUseSpiAuto"
        Me.RadioUseSpiAuto.Size = New System.Drawing.Size(132, 17)
        Me.RadioUseSpiAuto.TabIndex = 26
        Me.RadioUseSpiAuto.TabStop = True
        Me.RadioUseSpiAuto.Text = "Use automatic settings"
        Me.RadioUseSpiAuto.UseVisualStyleBackColor = True
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.cb_spi_quad)
        Me.GroupBox1.Controls.Add(Me.cb_spi_pro_clock)
        Me.GroupBox1.Controls.Add(Me.Label16)
        Me.GroupBox1.Controls.Add(Me.cb_spi_eeprom)
        Me.GroupBox1.Controls.Add(Me.Label3)
        Me.GroupBox1.Controls.Add(Me.lbl_read_cmd)
        Me.GroupBox1.Controls.Add(Me.cb_spi_clock)
        Me.GroupBox1.Controls.Add(Me.rb_fastread_op)
        Me.GroupBox1.Controls.Add(Me.rb_read_op)
        Me.GroupBox1.Controls.Add(Me.Label1)
        Me.GroupBox1.Location = New System.Drawing.Point(6, 6)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(439, 111)
        Me.GroupBox1.TabIndex = 2
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "General"
        '
        'cb_spi_quad
        '
        Me.cb_spi_quad.AutoSize = True
        Me.cb_spi_quad.Location = New System.Drawing.Point(15, 85)
        Me.cb_spi_quad.Name = "cb_spi_quad"
        Me.cb_spi_quad.Size = New System.Drawing.Size(169, 17)
        Me.cb_spi_quad.TabIndex = 8
        Me.cb_spi_quad.Text = "Enable SQI mode (QUAD-SPI)"
        Me.cb_spi_quad.UseVisualStyleBackColor = True
        '
        'cb_spi_pro_clock
        '
        Me.cb_spi_pro_clock.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_spi_pro_clock.FormattingEnabled = True
        Me.cb_spi_pro_clock.Location = New System.Drawing.Point(122, 34)
        Me.cb_spi_pro_clock.Name = "cb_spi_pro_clock"
        Me.cb_spi_pro_clock.Size = New System.Drawing.Size(77, 21)
        Me.cb_spi_pro_clock.TabIndex = 6
        '
        'Label16
        '
        Me.Label16.AutoSize = True
        Me.Label16.Location = New System.Drawing.Point(122, 18)
        Me.Label16.Name = "Label16"
        Me.Label16.Size = New System.Drawing.Size(78, 13)
        Me.Label16.TabIndex = 7
        Me.Label16.Text = "SPI clock (Pro)"
        '
        'cb_spi_eeprom
        '
        Me.cb_spi_eeprom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_spi_eeprom.FormattingEnabled = True
        Me.cb_spi_eeprom.Location = New System.Drawing.Point(250, 34)
        Me.cb_spi_eeprom.Name = "cb_spi_eeprom"
        Me.cb_spi_eeprom.Size = New System.Drawing.Size(156, 21)
        Me.cb_spi_eeprom.TabIndex = 4
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(249, 18)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(108, 13)
        Me.Label3.TabIndex = 5
        Me.Label3.Text = "SPI EEPROM device"
        '
        'lbl_read_cmd
        '
        Me.lbl_read_cmd.AutoSize = True
        Me.lbl_read_cmd.Location = New System.Drawing.Point(13, 65)
        Me.lbl_read_cmd.Name = "lbl_read_cmd"
        Me.lbl_read_cmd.Size = New System.Drawing.Size(82, 13)
        Me.lbl_read_cmd.TabIndex = 2
        Me.lbl_read_cmd.Text = "Read command"
        '
        'cb_spi_clock
        '
        Me.cb_spi_clock.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_spi_clock.FormattingEnabled = True
        Me.cb_spi_clock.Location = New System.Drawing.Point(15, 34)
        Me.cb_spi_clock.Name = "cb_spi_clock"
        Me.cb_spi_clock.Size = New System.Drawing.Size(77, 21)
        Me.cb_spi_clock.TabIndex = 0
        '
        'rb_fastread_op
        '
        Me.rb_fastread_op.AutoSize = True
        Me.rb_fastread_op.Location = New System.Drawing.Point(194, 63)
        Me.rb_fastread_op.Name = "rb_fastread_op"
        Me.rb_fastread_op.Size = New System.Drawing.Size(118, 17)
        Me.rb_fastread_op.TabIndex = 3
        Me.rb_fastread_op.TabStop = True
        Me.rb_fastread_op.Text = "FAST READ (0x0B)"
        Me.rb_fastread_op.UseVisualStyleBackColor = True
        '
        'rb_read_op
        '
        Me.rb_read_op.AutoSize = True
        Me.rb_read_op.Location = New System.Drawing.Point(101, 63)
        Me.rb_read_op.Name = "rb_read_op"
        Me.rb_read_op.Size = New System.Drawing.Size(87, 17)
        Me.rb_read_op.TabIndex = 2
        Me.rb_read_op.TabStop = True
        Me.rb_read_op.Text = "READ (0x03)"
        Me.rb_read_op.UseVisualStyleBackColor = True
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(15, 18)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(53, 13)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "SPI clock"
        '
        'TP_JTAG
        '
        Me.TP_JTAG.BackColor = System.Drawing.SystemColors.Control
        Me.TP_JTAG.Location = New System.Drawing.Point(4, 22)
        Me.TP_JTAG.Name = "TP_JTAG"
        Me.TP_JTAG.Padding = New System.Windows.Forms.Padding(3)
        Me.TP_JTAG.Size = New System.Drawing.Size(451, 328)
        Me.TP_JTAG.TabIndex = 1
        Me.TP_JTAG.Text = " JTAG "
        '
        'TP_I2C
        '
        Me.TP_I2C.BackColor = System.Drawing.SystemColors.Control
        Me.TP_I2C.Controls.Add(Me.GroupBox6)
        Me.TP_I2C.Controls.Add(Me.GroupBox5)
        Me.TP_I2C.Controls.Add(Me.GroupBox4)
        Me.TP_I2C.Location = New System.Drawing.Point(4, 22)
        Me.TP_I2C.Name = "TP_I2C"
        Me.TP_I2C.Padding = New System.Windows.Forms.Padding(3)
        Me.TP_I2C.Size = New System.Drawing.Size(451, 328)
        Me.TP_I2C.TabIndex = 2
        Me.TP_I2C.Text = " I ² C "
        '
        'GroupBox6
        '
        Me.GroupBox6.Controls.Add(Me.cbi2cDensity)
        Me.GroupBox6.Location = New System.Drawing.Point(6, 57)
        Me.GroupBox6.Name = "GroupBox6"
        Me.GroupBox6.Size = New System.Drawing.Size(433, 51)
        Me.GroupBox6.TabIndex = 35
        Me.GroupBox6.TabStop = False
        Me.GroupBox6.Text = "Density"
        '
        'cbi2cDensity
        '
        Me.cbi2cDensity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbi2cDensity.FormattingEnabled = True
        Me.cbi2cDensity.Items.AddRange(New Object() {"(Not Selected)", "256 Bytes (256 x 8) - 24XX02", "512 Bytes (512 x 8) - 24XX04", "1024 Bytes (1K x 8) - 24XX08", "2048 Bytes (2K x 8) - 24XX16", "4096 Bytes (4K x 8) - 24XX32", "8192 Bytes (8K x 8) - 24XX64", "16K Bytes (16K x 8) - 24XX128", "32K Bytes (32K x 8) - 24XX256", "64K Bytes (64K x 8) - 24XX512", "128K Bytes (128K x 8) - 24XXM01", "256K Bytes (256K x 8) - 24XXM02"})
        Me.cbi2cDensity.Location = New System.Drawing.Point(9, 22)
        Me.cbi2cDensity.Name = "cbi2cDensity"
        Me.cbi2cDensity.Size = New System.Drawing.Size(187, 21)
        Me.cbi2cDensity.TabIndex = 36
        '
        'GroupBox5
        '
        Me.GroupBox5.Controls.Add(Me.cbI2C_A0)
        Me.GroupBox5.Controls.Add(Me.cbI2C_A1)
        Me.GroupBox5.Controls.Add(Me.cbI2C_A2)
        Me.GroupBox5.Location = New System.Drawing.Point(3, 114)
        Me.GroupBox5.Name = "GroupBox5"
        Me.GroupBox5.Size = New System.Drawing.Size(433, 52)
        Me.GroupBox5.TabIndex = 34
        Me.GroupBox5.TabStop = False
        Me.GroupBox5.Text = "Addressing mode"
        '
        'cbI2C_A0
        '
        Me.cbI2C_A0.AutoSize = True
        Me.cbI2C_A0.Location = New System.Drawing.Point(97, 24)
        Me.cbI2C_A0.Name = "cbI2C_A0"
        Me.cbI2C_A0.Size = New System.Drawing.Size(39, 17)
        Me.cbI2C_A0.TabIndex = 28
        Me.cbI2C_A0.Text = "A0"
        Me.cbI2C_A0.UseVisualStyleBackColor = True
        '
        'cbI2C_A1
        '
        Me.cbI2C_A1.AutoSize = True
        Me.cbI2C_A1.Location = New System.Drawing.Point(52, 24)
        Me.cbI2C_A1.Name = "cbI2C_A1"
        Me.cbI2C_A1.Size = New System.Drawing.Size(39, 17)
        Me.cbI2C_A1.TabIndex = 29
        Me.cbI2C_A1.Text = "A1"
        Me.cbI2C_A1.UseVisualStyleBackColor = True
        '
        'cbI2C_A2
        '
        Me.cbI2C_A2.AutoSize = True
        Me.cbI2C_A2.Location = New System.Drawing.Point(9, 24)
        Me.cbI2C_A2.Name = "cbI2C_A2"
        Me.cbI2C_A2.Size = New System.Drawing.Size(39, 17)
        Me.cbI2C_A2.TabIndex = 30
        Me.cbI2C_A2.Text = "A2"
        Me.cbI2C_A2.UseVisualStyleBackColor = True
        '
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.rb_speed_1mhz)
        Me.GroupBox4.Controls.Add(Me.rb_speed_400khz)
        Me.GroupBox4.Controls.Add(Me.rb_speed_100khz)
        Me.GroupBox4.Location = New System.Drawing.Point(6, 6)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(433, 45)
        Me.GroupBox4.TabIndex = 33
        Me.GroupBox4.TabStop = False
        Me.GroupBox4.Text = "Speed mode"
        '
        'rb_speed_1mhz
        '
        Me.rb_speed_1mhz.AutoSize = True
        Me.rb_speed_1mhz.Location = New System.Drawing.Point(253, 19)
        Me.rb_speed_1mhz.Name = "rb_speed_1mhz"
        Me.rb_speed_1mhz.Size = New System.Drawing.Size(82, 17)
        Me.rb_speed_1mhz.TabIndex = 36
        Me.rb_speed_1mhz.TabStop = True
        Me.rb_speed_1mhz.Text = "1MHz (Fm+)"
        Me.rb_speed_1mhz.UseVisualStyleBackColor = True
        '
        'rb_speed_400khz
        '
        Me.rb_speed_400khz.AutoSize = True
        Me.rb_speed_400khz.Location = New System.Drawing.Point(102, 19)
        Me.rb_speed_400khz.Name = "rb_speed_400khz"
        Me.rb_speed_400khz.Size = New System.Drawing.Size(123, 17)
        Me.rb_speed_400khz.TabIndex = 35
        Me.rb_speed_400khz.TabStop = True
        Me.rb_speed_400khz.Text = "400 kHz (Fast-mode)"
        Me.rb_speed_400khz.UseVisualStyleBackColor = True
        '
        'rb_speed_100khz
        '
        Me.rb_speed_100khz.AutoSize = True
        Me.rb_speed_100khz.Location = New System.Drawing.Point(6, 19)
        Me.rb_speed_100khz.Name = "rb_speed_100khz"
        Me.rb_speed_100khz.Size = New System.Drawing.Size(65, 17)
        Me.rb_speed_100khz.TabIndex = 34
        Me.rb_speed_100khz.TabStop = True
        Me.rb_speed_100khz.Text = "100 kHz"
        Me.rb_speed_100khz.UseVisualStyleBackColor = True
        '
        'TP_NAND
        '
        Me.TP_NAND.BackColor = System.Drawing.SystemColors.Control
        Me.TP_NAND.Controls.Add(Me.gb_block_layout)
        Me.TP_NAND.Controls.Add(Me.gb_block_manager)
        Me.TP_NAND.Controls.Add(Me.gb_nand_general)
        Me.TP_NAND.Location = New System.Drawing.Point(4, 22)
        Me.TP_NAND.Name = "TP_NAND"
        Me.TP_NAND.Padding = New System.Windows.Forms.Padding(3)
        Me.TP_NAND.Size = New System.Drawing.Size(451, 328)
        Me.TP_NAND.TabIndex = 3
        Me.TP_NAND.Text = " NAND "
        '
        'gb_block_layout
        '
        Me.gb_block_layout.Controls.Add(Me.nand_box)
        Me.gb_block_layout.Controls.Add(Me.rb_mainspare_all)
        Me.gb_block_layout.Controls.Add(Me.rb_mainspare_segmented)
        Me.gb_block_layout.Controls.Add(Me.rb_mainspare_default)
        Me.gb_block_layout.Location = New System.Drawing.Point(6, 245)
        Me.gb_block_layout.Name = "gb_block_layout"
        Me.gb_block_layout.Size = New System.Drawing.Size(433, 77)
        Me.gb_block_layout.TabIndex = 2
        Me.gb_block_layout.TabStop = False
        Me.gb_block_layout.Text = "Page layout"
        '
        'nand_box
        '
        Me.nand_box.Location = New System.Drawing.Point(44, 19)
        Me.nand_box.Name = "nand_box"
        Me.nand_box.Size = New System.Drawing.Size(356, 20)
        Me.nand_box.TabIndex = 4
        Me.nand_box.TabStop = False
        '
        'rb_mainspare_all
        '
        Me.rb_mainspare_all.AutoSize = True
        Me.rb_mainspare_all.Location = New System.Drawing.Point(305, 52)
        Me.rb_mainspare_all.Name = "rb_mainspare_all"
        Me.rb_mainspare_all.Size = New System.Drawing.Size(72, 17)
        Me.rb_mainspare_all.TabIndex = 3
        Me.rb_mainspare_all.TabStop = True
        Me.rb_mainspare_all.Text = "Combined"
        Me.rb_mainspare_all.UseVisualStyleBackColor = True
        '
        'rb_mainspare_segmented
        '
        Me.rb_mainspare_segmented.AutoSize = True
        Me.rb_mainspare_segmented.Location = New System.Drawing.Point(177, 52)
        Me.rb_mainspare_segmented.Name = "rb_mainspare_segmented"
        Me.rb_mainspare_segmented.Size = New System.Drawing.Size(79, 17)
        Me.rb_mainspare_segmented.TabIndex = 2
        Me.rb_mainspare_segmented.TabStop = True
        Me.rb_mainspare_segmented.Text = "Segmented"
        Me.rb_mainspare_segmented.UseVisualStyleBackColor = True
        '
        'rb_mainspare_default
        '
        Me.rb_mainspare_default.AutoSize = True
        Me.rb_mainspare_default.Location = New System.Drawing.Point(69, 52)
        Me.rb_mainspare_default.Name = "rb_mainspare_default"
        Me.rb_mainspare_default.Size = New System.Drawing.Size(68, 17)
        Me.rb_mainspare_default.TabIndex = 1
        Me.rb_mainspare_default.TabStop = True
        Me.rb_mainspare_default.Text = "Seperate"
        Me.rb_mainspare_default.UseVisualStyleBackColor = True
        '
        'gb_block_manager
        '
        Me.gb_block_manager.Controls.Add(Me.cb_badmarker_6th_page2)
        Me.gb_block_manager.Controls.Add(Me.cb_badmarker_6th_page1)
        Me.gb_block_manager.Controls.Add(Me.lbl_6th_byte)
        Me.gb_block_manager.Controls.Add(Me.lbl_1st_byte)
        Me.gb_block_manager.Controls.Add(Me.cb_badmarker_1st_lastpage)
        Me.gb_block_manager.Controls.Add(Me.cb_badmarker_1st_page2)
        Me.gb_block_manager.Controls.Add(Me.cb_badmarker_1st_page1)
        Me.gb_block_manager.Controls.Add(Me.cb_badblock_enabled)
        Me.gb_block_manager.Controls.Add(Me.cb_badblock_disabled)
        Me.gb_block_manager.Location = New System.Drawing.Point(6, 124)
        Me.gb_block_manager.Name = "gb_block_manager"
        Me.gb_block_manager.Size = New System.Drawing.Size(433, 115)
        Me.gb_block_manager.TabIndex = 0
        Me.gb_block_manager.TabStop = False
        Me.gb_block_manager.Text = "Bad block manager"
        '
        'cb_badmarker_6th_page2
        '
        Me.cb_badmarker_6th_page2.AutoSize = True
        Me.cb_badmarker_6th_page2.Location = New System.Drawing.Point(176, 66)
        Me.cb_badmarker_6th_page2.Name = "cb_badmarker_6th_page2"
        Me.cb_badmarker_6th_page2.Size = New System.Drawing.Size(119, 17)
        Me.cb_badmarker_6th_page2.TabIndex = 8
        Me.cb_badmarker_6th_page2.Text = "Second spare page"
        Me.cb_badmarker_6th_page2.UseVisualStyleBackColor = True
        '
        'cb_badmarker_6th_page1
        '
        Me.cb_badmarker_6th_page1.AutoSize = True
        Me.cb_badmarker_6th_page1.Location = New System.Drawing.Point(69, 66)
        Me.cb_badmarker_6th_page1.Name = "cb_badmarker_6th_page1"
        Me.cb_badmarker_6th_page1.Size = New System.Drawing.Size(101, 17)
        Me.cb_badmarker_6th_page1.TabIndex = 7
        Me.cb_badmarker_6th_page1.Text = "First spare page"
        Me.cb_badmarker_6th_page1.UseVisualStyleBackColor = True
        '
        'lbl_6th_byte
        '
        Me.lbl_6th_byte.AutoSize = True
        Me.lbl_6th_byte.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbl_6th_byte.Location = New System.Drawing.Point(3, 67)
        Me.lbl_6th_byte.Name = "lbl_6th_byte"
        Me.lbl_6th_byte.Size = New System.Drawing.Size(57, 13)
        Me.lbl_6th_byte.TabIndex = 6
        Me.lbl_6th_byte.Text = "6th byte:"
        '
        'lbl_1st_byte
        '
        Me.lbl_1st_byte.AutoSize = True
        Me.lbl_1st_byte.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbl_1st_byte.Location = New System.Drawing.Point(3, 46)
        Me.lbl_1st_byte.Name = "lbl_1st_byte"
        Me.lbl_1st_byte.Size = New System.Drawing.Size(56, 13)
        Me.lbl_1st_byte.TabIndex = 5
        Me.lbl_1st_byte.Text = "1st byte:"
        '
        'cb_badmarker_1st_lastpage
        '
        Me.cb_badmarker_1st_lastpage.AutoSize = True
        Me.cb_badmarker_1st_lastpage.Location = New System.Drawing.Point(301, 45)
        Me.cb_badmarker_1st_lastpage.Name = "cb_badmarker_1st_lastpage"
        Me.cb_badmarker_1st_lastpage.Size = New System.Drawing.Size(102, 17)
        Me.cb_badmarker_1st_lastpage.TabIndex = 4
        Me.cb_badmarker_1st_lastpage.Text = "Last spare page"
        Me.cb_badmarker_1st_lastpage.UseVisualStyleBackColor = True
        '
        'cb_badmarker_1st_page2
        '
        Me.cb_badmarker_1st_page2.AutoSize = True
        Me.cb_badmarker_1st_page2.Location = New System.Drawing.Point(176, 45)
        Me.cb_badmarker_1st_page2.Name = "cb_badmarker_1st_page2"
        Me.cb_badmarker_1st_page2.Size = New System.Drawing.Size(119, 17)
        Me.cb_badmarker_1st_page2.TabIndex = 3
        Me.cb_badmarker_1st_page2.Text = "Second spare page"
        Me.cb_badmarker_1st_page2.UseVisualStyleBackColor = True
        '
        'cb_badmarker_1st_page1
        '
        Me.cb_badmarker_1st_page1.AutoSize = True
        Me.cb_badmarker_1st_page1.Location = New System.Drawing.Point(69, 45)
        Me.cb_badmarker_1st_page1.Name = "cb_badmarker_1st_page1"
        Me.cb_badmarker_1st_page1.Size = New System.Drawing.Size(101, 17)
        Me.cb_badmarker_1st_page1.TabIndex = 2
        Me.cb_badmarker_1st_page1.Text = "First spare page"
        Me.cb_badmarker_1st_page1.UseVisualStyleBackColor = True
        '
        'cb_badblock_enabled
        '
        Me.cb_badblock_enabled.AutoSize = True
        Me.cb_badblock_enabled.Location = New System.Drawing.Point(78, 19)
        Me.cb_badblock_enabled.Name = "cb_badblock_enabled"
        Me.cb_badblock_enabled.Size = New System.Drawing.Size(208, 17)
        Me.cb_badblock_enabled.TabIndex = 1
        Me.cb_badblock_enabled.TabStop = True
        Me.cb_badblock_enabled.Text = "Enabled (check for bad block markers)"
        Me.cb_badblock_enabled.UseVisualStyleBackColor = True
        '
        'cb_badblock_disabled
        '
        Me.cb_badblock_disabled.AutoSize = True
        Me.cb_badblock_disabled.Location = New System.Drawing.Point(6, 19)
        Me.cb_badblock_disabled.Name = "cb_badblock_disabled"
        Me.cb_badblock_disabled.Size = New System.Drawing.Size(66, 17)
        Me.cb_badblock_disabled.TabIndex = 0
        Me.cb_badblock_disabled.TabStop = True
        Me.cb_badblock_disabled.Text = "Disabled"
        Me.cb_badblock_disabled.UseVisualStyleBackColor = True
        '
        'gb_nand_general
        '
        Me.gb_nand_general.Controls.Add(Me.cb_nand_image_readverify)
        Me.gb_nand_general.Controls.Add(Me.cb_spinand_disable_ecc)
        Me.gb_nand_general.Controls.Add(Me.cb_mismatch)
        Me.gb_nand_general.Controls.Add(Me.cb_preserve)
        Me.gb_nand_general.Location = New System.Drawing.Point(6, 6)
        Me.gb_nand_general.Name = "gb_nand_general"
        Me.gb_nand_general.Size = New System.Drawing.Size(433, 112)
        Me.gb_nand_general.TabIndex = 1
        Me.gb_nand_general.TabStop = False
        Me.gb_nand_general.Text = "General"
        '
        'cb_nand_image_readverify
        '
        Me.cb_nand_image_readverify.AutoSize = True
        Me.cb_nand_image_readverify.Location = New System.Drawing.Point(6, 86)
        Me.cb_nand_image_readverify.Name = "cb_nand_image_readverify"
        Me.cb_nand_image_readverify.Size = New System.Drawing.Size(188, 17)
        Me.cb_nand_image_readverify.TabIndex = 3
        Me.cb_nand_image_readverify.Text = "Use Read-Verify on 'Create Image'"
        Me.cb_nand_image_readverify.UseVisualStyleBackColor = True
        '
        'cb_spinand_disable_ecc
        '
        Me.cb_spinand_disable_ecc.AutoSize = True
        Me.cb_spinand_disable_ecc.Location = New System.Drawing.Point(6, 65)
        Me.cb_spinand_disable_ecc.Name = "cb_spinand_disable_ecc"
        Me.cb_spinand_disable_ecc.Size = New System.Drawing.Size(187, 17)
        Me.cb_spinand_disable_ecc.TabIndex = 2
        Me.cb_spinand_disable_ecc.Text = "Disable SPI-NAND ECC generator"
        Me.cb_spinand_disable_ecc.UseVisualStyleBackColor = True
        '
        'cb_mismatch
        '
        Me.cb_mismatch.AutoSize = True
        Me.cb_mismatch.Location = New System.Drawing.Point(6, 42)
        Me.cb_mismatch.Name = "cb_mismatch"
        Me.cb_mismatch.Size = New System.Drawing.Size(228, 17)
        Me.cb_mismatch.TabIndex = 1
        Me.cb_mismatch.Text = "On write mismatch, write data to next block"
        Me.cb_mismatch.UseVisualStyleBackColor = True
        '
        'cb_preserve
        '
        Me.cb_preserve.AutoSize = True
        Me.cb_preserve.Location = New System.Drawing.Point(6, 19)
        Me.cb_preserve.Name = "cb_preserve"
        Me.cb_preserve.Size = New System.Drawing.Size(394, 17)
        Me.cb_preserve.TabIndex = 0
        Me.cb_preserve.Text = "Preserve memory areas (i.e. copy spare area prior to main area write operation)"
        Me.cb_preserve.UseVisualStyleBackColor = True
        '
        'FrmSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(470, 363)
        Me.Controls.Add(Me.MyTabs)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FrmSettings"
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "FlashcatUSB Protocol Settings"
        Me.MyTabs.ResumeLayout(False)
        Me.TP_SPI.ResumeLayout(False)
        Me.TP_SPI.PerformLayout()
        Me.group_custom.ResumeLayout(False)
        Me.group_custom.PerformLayout()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.TP_I2C.ResumeLayout(False)
        Me.GroupBox6.ResumeLayout(False)
        Me.GroupBox5.ResumeLayout(False)
        Me.GroupBox5.PerformLayout()
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox4.PerformLayout()
        Me.TP_NAND.ResumeLayout(False)
        Me.gb_block_layout.ResumeLayout(False)
        Me.gb_block_layout.PerformLayout()
        CType(Me.nand_box, System.ComponentModel.ISupportInitialize).EndInit()
        Me.gb_block_manager.ResumeLayout(False)
        Me.gb_block_manager.PerformLayout()
        Me.gb_nand_general.ResumeLayout(False)
        Me.gb_nand_general.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents MyTabs As TabControl
    Friend WithEvents TP_SPI As TabPage
    Friend WithEvents TP_JTAG As TabPage
    Friend WithEvents TP_I2C As TabPage
    Friend WithEvents TP_NAND As TabPage
    Friend WithEvents Label1 As Label
    Friend WithEvents cb_spi_clock As ComboBox
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents lbl_read_cmd As Label
    Friend WithEvents rb_fastread_op As RadioButton
    Friend WithEvents rb_read_op As RadioButton
    Friend WithEvents group_custom As GroupBox
    Friend WithEvents cb_prog_mode As ComboBox
    Friend WithEvents Label4 As Label
    Friend WithEvents cb_page_size As ComboBox
    Friend WithEvents lblSpiProgMode As Label
    Friend WithEvents cb_erase_size As ComboBox
    Friend WithEvents cb_chip_size As ComboBox
    Friend WithEvents lblSpiChipSize As Label
    Friend WithEvents lblSpiEraseSize As Label
    Friend WithEvents RadioUseSpiSettings As RadioButton
    Friend WithEvents RadioUseSpiAuto As RadioButton
    Friend WithEvents cbENWS As CheckBox
    Friend WithEvents cb_spare As ComboBox
    Friend WithEvents Label5 As Label
    Friend WithEvents Label14 As Label
    Friend WithEvents Label15 As Label
    Friend WithEvents Label12 As Label
    Friend WithEvents op_ewsr As ComboBox
    Friend WithEvents op_ws As ComboBox
    Friend WithEvents op_rs As ComboBox
    Friend WithEvents Label11 As Label
    Friend WithEvents Label10 As Label
    Friend WithEvents op_ce As ComboBox
    Friend WithEvents Label9 As Label
    Friend WithEvents Label8 As Label
    Friend WithEvents op_we As ComboBox
    Friend WithEvents Label7 As Label
    Friend WithEvents op_read As ComboBox
    Friend WithEvents op_sectorerase As ComboBox
    Friend WithEvents op_prog As ComboBox
    Friend WithEvents gb_block_manager As GroupBox
    Friend WithEvents cb_badblock_enabled As RadioButton
    Friend WithEvents cb_badblock_disabled As RadioButton
    Friend WithEvents gb_nand_general As GroupBox
    Friend WithEvents cb_mismatch As CheckBox
    Friend WithEvents cb_preserve As CheckBox
    Friend WithEvents cbI2C_A2 As CheckBox
    Friend WithEvents cbI2C_A1 As CheckBox
    Friend WithEvents cbI2C_A0 As CheckBox
    Friend WithEvents GroupBox4 As GroupBox
    Friend WithEvents rb_speed_1mhz As RadioButton
    Friend WithEvents rb_speed_400khz As RadioButton
    Friend WithEvents rb_speed_100khz As RadioButton
    Friend WithEvents GroupBox5 As GroupBox
    Friend WithEvents GroupBox6 As GroupBox
    Friend WithEvents cbi2cDensity As ComboBox
    Friend WithEvents cb_spi_eeprom As ComboBox
    Friend WithEvents Label3 As Label
    Friend WithEvents Label6 As Label
    Friend WithEvents cb_addr_size As ComboBox
    Friend WithEvents cbEN4B As CheckBox
    Friend WithEvents Label13 As Label
    Friend WithEvents cb_spi_pro_clock As ComboBox
    Friend WithEvents Label16 As Label
    Friend WithEvents gb_block_layout As GroupBox
    Friend WithEvents rb_mainspare_segmented As RadioButton
    Friend WithEvents rb_mainspare_default As RadioButton
    Friend WithEvents cb_spi_quad As CheckBox
    Friend WithEvents rb_mainspare_all As RadioButton
    Friend WithEvents cb_spinand_disable_ecc As CheckBox
    Friend WithEvents cb_nand_image_readverify As CheckBox
    Friend WithEvents nand_box As PictureBox
    Friend WithEvents lbl_1st_byte As Label
    Friend WithEvents cb_badmarker_1st_lastpage As CheckBox
    Friend WithEvents cb_badmarker_1st_page2 As CheckBox
    Friend WithEvents cb_badmarker_1st_page1 As CheckBox
    Friend WithEvents cb_badmarker_6th_page2 As CheckBox
    Friend WithEvents cb_badmarker_6th_page1 As CheckBox
    Friend WithEvents lbl_6th_byte As Label
End Class