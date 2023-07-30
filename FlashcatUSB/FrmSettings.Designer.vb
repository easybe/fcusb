<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
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
        Me.RadioUseSpiSettings = New System.Windows.Forms.RadioButton()
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
        Me.RadioUseSpiAuto = New System.Windows.Forms.RadioButton()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.cb_sqi_clock = New System.Windows.Forms.ComboBox()
        Me.Label21 = New System.Windows.Forms.Label()
        Me.cb_spi_eeprom = New System.Windows.Forms.ComboBox()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.lbl_read_cmd = New System.Windows.Forms.Label()
        Me.cb_spi_clock = New System.Windows.Forms.ComboBox()
        Me.rb_fastread_op = New System.Windows.Forms.RadioButton()
        Me.rb_read_op = New System.Windows.Forms.RadioButton()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.TP_I2C = New System.Windows.Forms.TabPage()
        Me.GroupBox8 = New System.Windows.Forms.GroupBox()
        Me.cb_swi_a0 = New System.Windows.Forms.CheckBox()
        Me.cb_swi_a1 = New System.Windows.Forms.CheckBox()
        Me.cb_swi_a2 = New System.Windows.Forms.CheckBox()
        Me.GroupBox6 = New System.Windows.Forms.GroupBox()
        Me.cb_i2c_device = New System.Windows.Forms.ComboBox()
        Me.GroupBox5 = New System.Windows.Forms.GroupBox()
        Me.cb_i2c_a0 = New System.Windows.Forms.CheckBox()
        Me.cb_i2c_a1 = New System.Windows.Forms.CheckBox()
        Me.cb_i2c_a2 = New System.Windows.Forms.CheckBox()
        Me.GroupBox4 = New System.Windows.Forms.GroupBox()
        Me.rb_speed_1mhz = New System.Windows.Forms.RadioButton()
        Me.rb_speed_400khz = New System.Windows.Forms.RadioButton()
        Me.rb_speed_100khz = New System.Windows.Forms.RadioButton()
        Me.TP_NAND1 = New System.Windows.Forms.TabPage()
        Me.gb_block_layout = New System.Windows.Forms.GroupBox()
        Me.nand_box = New System.Windows.Forms.PictureBox()
        Me.rb_mainspare_all = New System.Windows.Forms.RadioButton()
        Me.rb_mainspare_segmented = New System.Windows.Forms.RadioButton()
        Me.rb_mainspare_default = New System.Windows.Forms.RadioButton()
        Me.gb_block_manager = New System.Windows.Forms.GroupBox()
        Me.cb_badblock_enabled = New System.Windows.Forms.RadioButton()
        Me.cb_badmarker_6th_page2 = New System.Windows.Forms.CheckBox()
        Me.cb_badmarker_6th_page1 = New System.Windows.Forms.CheckBox()
        Me.cb_badmarker_1st_lastpage = New System.Windows.Forms.CheckBox()
        Me.cb_badmarker_1st_page2 = New System.Windows.Forms.CheckBox()
        Me.cb_badmarker_1st_page1 = New System.Windows.Forms.CheckBox()
        Me.lbl_6th_byte = New System.Windows.Forms.Label()
        Me.lbl_1st_byte = New System.Windows.Forms.Label()
        Me.cb_badblock_disabled = New System.Windows.Forms.RadioButton()
        Me.gb_nand_general = New System.Windows.Forms.GroupBox()
        Me.cbNAND_Speed = New System.Windows.Forms.ComboBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.cb_nand_image_readverify = New System.Windows.Forms.CheckBox()
        Me.cb_spinand_disable_ecc = New System.Windows.Forms.CheckBox()
        Me.cb_mismatch = New System.Windows.Forms.CheckBox()
        Me.cb_preserve = New System.Windows.Forms.CheckBox()
        Me.TP_NAND2 = New System.Windows.Forms.TabPage()
        Me.gb_nandecc_title = New System.Windows.Forms.GroupBox()
        Me.txt_ecc_location = New System.Windows.Forms.TextBox()
        Me.cb_ecc_seperate = New System.Windows.Forms.CheckBox()
        Me.lbl_ECC_size = New System.Windows.Forms.Label()
        Me.rb_ECC_BHC = New System.Windows.Forms.RadioButton()
        Me.rb_ECC_ReedSolomon = New System.Windows.Forms.RadioButton()
        Me.rb_ECC_Hamming = New System.Windows.Forms.RadioButton()
        Me.lbl_nandecc_algorithm = New System.Windows.Forms.Label()
        Me.lbl_nandecc_changes = New System.Windows.Forms.Label()
        Me.cb_rs_reverse_data = New System.Windows.Forms.CheckBox()
        Me.cb_sym_width = New System.Windows.Forms.ComboBox()
        Me.lbl_sym_width = New System.Windows.Forms.Label()
        Me.lbl_nandecc_location = New System.Windows.Forms.Label()
        Me.cb_ECC_WriteEnable = New System.Windows.Forms.CheckBox()
        Me.cb_ECC_ReadEnable = New System.Windows.Forms.CheckBox()
        Me.lbl_nandecc_enabled = New System.Windows.Forms.Label()
        Me.cb_ECC_BITERR = New System.Windows.Forms.ComboBox()
        Me.lbl_nandecc_biterror = New System.Windows.Forms.Label()
        Me.TP_GEN = New System.Windows.Forms.TabPage()
        Me.GroupBox9 = New System.Windows.Forms.GroupBox()
        Me.cb_nor_we_pulse = New System.Windows.Forms.ComboBox()
        Me.cb_nor_read_access = New System.Windows.Forms.ComboBox()
        Me.Label24 = New System.Windows.Forms.Label()
        Me.Label23 = New System.Windows.Forms.Label()
        Me.GroupBox7 = New System.Windows.Forms.GroupBox()
        Me.lbl_s93_size = New System.Windows.Forms.Label()
        Me.cb_s93_devices = New System.Windows.Forms.ComboBox()
        Me.Label18 = New System.Windows.Forms.Label()
        Me.cb_s93_org = New System.Windows.Forms.ComboBox()
        Me.Label19 = New System.Windows.Forms.Label()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.cb_jtag_tck_speed = New System.Windows.Forms.ComboBox()
        Me.Label22 = New System.Windows.Forms.Label()
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.Label16 = New System.Windows.Forms.Label()
        Me.cbSrec = New System.Windows.Forms.ComboBox()
        Me.cb_ce_select = New System.Windows.Forms.ComboBox()
        Me.cb_retry_write = New System.Windows.Forms.ComboBox()
        Me.Label17 = New System.Windows.Forms.Label()
        Me.Label20 = New System.Windows.Forms.Label()
        Me.cb_multi_ce = New System.Windows.Forms.ComboBox()
        Me.Label25 = New System.Windows.Forms.Label()
        Me.MyTabs.SuspendLayout()
        Me.TP_SPI.SuspendLayout()
        Me.group_custom.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.TP_I2C.SuspendLayout()
        Me.GroupBox8.SuspendLayout()
        Me.GroupBox6.SuspendLayout()
        Me.GroupBox5.SuspendLayout()
        Me.GroupBox4.SuspendLayout()
        Me.TP_NAND1.SuspendLayout()
        Me.gb_block_layout.SuspendLayout()
        CType(Me.nand_box, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.gb_block_manager.SuspendLayout()
        Me.gb_nand_general.SuspendLayout()
        Me.TP_NAND2.SuspendLayout()
        Me.gb_nandecc_title.SuspendLayout()
        Me.TP_GEN.SuspendLayout()
        Me.GroupBox9.SuspendLayout()
        Me.GroupBox7.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.GroupBox3.SuspendLayout()
        Me.SuspendLayout()
        '
        'MyTabs
        '
        Me.MyTabs.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.MyTabs.Controls.Add(Me.TP_SPI)
        Me.MyTabs.Controls.Add(Me.TP_I2C)
        Me.MyTabs.Controls.Add(Me.TP_NAND1)
        Me.MyTabs.Controls.Add(Me.TP_NAND2)
        Me.MyTabs.Controls.Add(Me.TP_GEN)
        Me.MyTabs.Location = New System.Drawing.Point(6, 6)
        Me.MyTabs.Name = "MyTabs"
        Me.MyTabs.SelectedIndex = 0
        Me.MyTabs.Size = New System.Drawing.Size(532, 354)
        Me.MyTabs.TabIndex = 0
        '
        'TP_SPI
        '
        Me.TP_SPI.BackColor = System.Drawing.SystemColors.Control
        Me.TP_SPI.Controls.Add(Me.RadioUseSpiSettings)
        Me.TP_SPI.Controls.Add(Me.group_custom)
        Me.TP_SPI.Controls.Add(Me.RadioUseSpiAuto)
        Me.TP_SPI.Controls.Add(Me.GroupBox1)
        Me.TP_SPI.Location = New System.Drawing.Point(4, 22)
        Me.TP_SPI.Name = "TP_SPI"
        Me.TP_SPI.Padding = New System.Windows.Forms.Padding(3)
        Me.TP_SPI.Size = New System.Drawing.Size(524, 328)
        Me.TP_SPI.TabIndex = 0
        Me.TP_SPI.Text = " SPI "
        '
        'RadioUseSpiSettings
        '
        Me.RadioUseSpiSettings.AutoSize = True
        Me.RadioUseSpiSettings.Location = New System.Drawing.Point(268, 117)
        Me.RadioUseSpiSettings.Name = "RadioUseSpiSettings"
        Me.RadioUseSpiSettings.Size = New System.Drawing.Size(112, 17)
        Me.RadioUseSpiSettings.TabIndex = 27
        Me.RadioUseSpiSettings.Text = "Use these settings"
        Me.RadioUseSpiSettings.UseVisualStyleBackColor = True
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
        Me.group_custom.Size = New System.Drawing.Size(512, 176)
        Me.group_custom.TabIndex = 28
        Me.group_custom.TabStop = False
        Me.group_custom.Text = "Operation commands"
        '
        'Label13
        '
        Me.Label13.AutoSize = True
        Me.Label13.Location = New System.Drawing.Point(48, 151)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(108, 13)
        Me.Label13.TabIndex = 63
        Me.Label13.Text = "Use operation codes:"
        '
        'cbEN4B
        '
        Me.cbEN4B.AutoSize = True
        Me.cbEN4B.Location = New System.Drawing.Point(242, 149)
        Me.cbEN4B.Name = "cbEN4B"
        Me.cbEN4B.Size = New System.Drawing.Size(54, 17)
        Me.cbEN4B.TabIndex = 62
        Me.cbEN4B.Text = "EN4B"
        Me.cbEN4B.UseVisualStyleBackColor = True
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(294, 23)
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
        Me.cb_addr_size.Location = New System.Drawing.Point(297, 41)
        Me.cb_addr_size.Name = "cb_addr_size"
        Me.cb_addr_size.Size = New System.Drawing.Size(65, 21)
        Me.cb_addr_size.TabIndex = 61
        '
        'cbENWS
        '
        Me.cbENWS.AutoSize = True
        Me.cbENWS.Location = New System.Drawing.Point(162, 149)
        Me.cbENWS.Name = "cbENWS"
        Me.cbENWS.Size = New System.Drawing.Size(59, 17)
        Me.cbENWS.TabIndex = 13
        Me.cbENWS.Text = "EWSR"
        Me.cbENWS.UseVisualStyleBackColor = True
        '
        'Label14
        '
        Me.Label14.AutoSize = True
        Me.Label14.Location = New System.Drawing.Point(320, 98)
        Me.Label14.Name = "Label14"
        Me.Label14.Size = New System.Drawing.Size(40, 13)
        Me.Label14.TabIndex = 59
        Me.Label14.Text = "EWRS"
        '
        'Label15
        '
        Me.Label15.AutoSize = True
        Me.Label15.Location = New System.Drawing.Point(320, 74)
        Me.Label15.Name = "Label15"
        Me.Label15.Size = New System.Drawing.Size(65, 13)
        Me.Label15.TabIndex = 58
        Me.Label15.Text = "Write Status"
        '
        'Label12
        '
        Me.Label12.AutoSize = True
        Me.Label12.Location = New System.Drawing.Point(186, 125)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(66, 13)
        Me.Label12.TabIndex = 53
        Me.Label12.Text = "Read Status"
        '
        'op_ewsr
        '
        Me.op_ewsr.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.op_ewsr.FormattingEnabled = True
        Me.op_ewsr.Location = New System.Drawing.Point(412, 95)
        Me.op_ewsr.Name = "op_ewsr"
        Me.op_ewsr.Size = New System.Drawing.Size(51, 21)
        Me.op_ewsr.TabIndex = 56
        '
        'op_ws
        '
        Me.op_ws.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.op_ws.FormattingEnabled = True
        Me.op_ws.Location = New System.Drawing.Point(412, 68)
        Me.op_ws.Name = "op_ws"
        Me.op_ws.Size = New System.Drawing.Size(51, 21)
        Me.op_ws.TabIndex = 55
        '
        'op_rs
        '
        Me.op_rs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.op_rs.FormattingEnabled = True
        Me.op_rs.Location = New System.Drawing.Point(263, 122)
        Me.op_rs.Name = "op_rs"
        Me.op_rs.Size = New System.Drawing.Size(51, 21)
        Me.op_rs.TabIndex = 54
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.Location = New System.Drawing.Point(186, 98)
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
        Me.cb_spare.Location = New System.Drawing.Point(403, 122)
        Me.cb_spare.Name = "cb_spare"
        Me.cb_spare.Size = New System.Drawing.Size(60, 21)
        Me.cb_spare.TabIndex = 40
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(320, 125)
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
        Me.cb_prog_mode.Location = New System.Drawing.Point(373, 41)
        Me.cb_prog_mode.Name = "cb_prog_mode"
        Me.cb_prog_mode.Size = New System.Drawing.Size(90, 21)
        Me.cb_prog_mode.TabIndex = 34
        '
        'Label10
        '
        Me.Label10.AutoSize = True
        Me.Label10.Location = New System.Drawing.Point(186, 74)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(68, 13)
        Me.Label10.TabIndex = 49
        Me.Label10.Text = "Write Enable"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(209, 23)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(55, 13)
        Me.Label4.TabIndex = 34
        Me.Label4.Text = "Page Size"
        '
        'op_ce
        '
        Me.op_ce.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.op_ce.FormattingEnabled = True
        Me.op_ce.Location = New System.Drawing.Point(263, 95)
        Me.op_ce.Name = "op_ce"
        Me.op_ce.Size = New System.Drawing.Size(51, 21)
        Me.op_ce.TabIndex = 52
        '
        'cb_page_size
        '
        Me.cb_page_size.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_page_size.FormattingEnabled = True
        Me.cb_page_size.Items.AddRange(New Object() {"8 bytes", "16 bytes", "32 bytes", "64 bytes", "128 bytes", "256 bytes", "512 bytes", "1024 bytes"})
        Me.cb_page_size.Location = New System.Drawing.Point(212, 41)
        Me.cb_page_size.Name = "cb_page_size"
        Me.cb_page_size.Size = New System.Drawing.Size(77, 21)
        Me.cb_page_size.TabIndex = 38
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(48, 125)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(68, 13)
        Me.Label9.TabIndex = 47
        Me.Label9.Text = "Sector Erase"
        '
        'lblSpiProgMode
        '
        Me.lblSpiProgMode.AutoSize = True
        Me.lblSpiProgMode.Location = New System.Drawing.Point(370, 23)
        Me.lblSpiProgMode.Name = "lblSpiProgMode"
        Me.lblSpiProgMode.Size = New System.Drawing.Size(76, 13)
        Me.lblSpiProgMode.TabIndex = 27
        Me.lblSpiProgMode.Text = "Program Mode"
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(48, 98)
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
        Me.cb_erase_size.Location = New System.Drawing.Point(129, 41)
        Me.cb_erase_size.Name = "cb_erase_size"
        Me.cb_erase_size.Size = New System.Drawing.Size(77, 21)
        Me.cb_erase_size.TabIndex = 36
        '
        'cb_chip_size
        '
        Me.cb_chip_size.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_chip_size.FormattingEnabled = True
        Me.cb_chip_size.Items.AddRange(New Object() {"1 Mbit", "2 Mbit", "4 Mbit", "8 Mbit", "16 Mbit", "32 Mbit", "64 Mbit", "128 Mbit", "256 Mbit", "512 Mbit", "1 Gbit", "2 Gbit"})
        Me.cb_chip_size.Location = New System.Drawing.Point(46, 41)
        Me.cb_chip_size.Name = "cb_chip_size"
        Me.cb_chip_size.Size = New System.Drawing.Size(77, 21)
        Me.cb_chip_size.TabIndex = 34
        '
        'op_we
        '
        Me.op_we.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.op_we.FormattingEnabled = True
        Me.op_we.Location = New System.Drawing.Point(263, 68)
        Me.op_we.Name = "op_we"
        Me.op_we.Size = New System.Drawing.Size(51, 21)
        Me.op_we.TabIndex = 50
        '
        'lblSpiChipSize
        '
        Me.lblSpiChipSize.AutoSize = True
        Me.lblSpiChipSize.Location = New System.Drawing.Point(43, 23)
        Me.lblSpiChipSize.Name = "lblSpiChipSize"
        Me.lblSpiChipSize.Size = New System.Drawing.Size(51, 13)
        Me.lblSpiChipSize.TabIndex = 7
        Me.lblSpiChipSize.Text = "Chip Size"
        '
        'lblSpiEraseSize
        '
        Me.lblSpiEraseSize.AutoSize = True
        Me.lblSpiEraseSize.Location = New System.Drawing.Point(126, 23)
        Me.lblSpiEraseSize.Name = "lblSpiEraseSize"
        Me.lblSpiEraseSize.Size = New System.Drawing.Size(57, 13)
        Me.lblSpiEraseSize.TabIndex = 5
        Me.lblSpiEraseSize.Text = "Erase Size"
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(48, 74)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(57, 13)
        Me.Label7.TabIndex = 43
        Me.Label7.Text = "Read data"
        '
        'op_read
        '
        Me.op_read.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.op_read.FormattingEnabled = True
        Me.op_read.Location = New System.Drawing.Point(129, 68)
        Me.op_read.Name = "op_read"
        Me.op_read.Size = New System.Drawing.Size(51, 21)
        Me.op_read.TabIndex = 44
        '
        'op_sectorerase
        '
        Me.op_sectorerase.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.op_sectorerase.FormattingEnabled = True
        Me.op_sectorerase.Location = New System.Drawing.Point(129, 122)
        Me.op_sectorerase.Name = "op_sectorerase"
        Me.op_sectorerase.Size = New System.Drawing.Size(51, 21)
        Me.op_sectorerase.TabIndex = 48
        '
        'op_prog
        '
        Me.op_prog.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.op_prog.FormattingEnabled = True
        Me.op_prog.Location = New System.Drawing.Point(129, 95)
        Me.op_prog.Name = "op_prog"
        Me.op_prog.Size = New System.Drawing.Size(51, 21)
        Me.op_prog.TabIndex = 46
        '
        'RadioUseSpiAuto
        '
        Me.RadioUseSpiAuto.AutoSize = True
        Me.RadioUseSpiAuto.Checked = True
        Me.RadioUseSpiAuto.Location = New System.Drawing.Point(9, 117)
        Me.RadioUseSpiAuto.Name = "RadioUseSpiAuto"
        Me.RadioUseSpiAuto.Size = New System.Drawing.Size(132, 17)
        Me.RadioUseSpiAuto.TabIndex = 26
        Me.RadioUseSpiAuto.TabStop = True
        Me.RadioUseSpiAuto.Text = "Use automatic settings"
        Me.RadioUseSpiAuto.UseVisualStyleBackColor = True
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.cb_sqi_clock)
        Me.GroupBox1.Controls.Add(Me.Label21)
        Me.GroupBox1.Controls.Add(Me.cb_spi_eeprom)
        Me.GroupBox1.Controls.Add(Me.Label3)
        Me.GroupBox1.Controls.Add(Me.lbl_read_cmd)
        Me.GroupBox1.Controls.Add(Me.cb_spi_clock)
        Me.GroupBox1.Controls.Add(Me.rb_fastread_op)
        Me.GroupBox1.Controls.Add(Me.rb_read_op)
        Me.GroupBox1.Controls.Add(Me.Label1)
        Me.GroupBox1.Location = New System.Drawing.Point(6, 6)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(512, 97)
        Me.GroupBox1.TabIndex = 2
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "General"
        '
        'cb_sqi_clock
        '
        Me.cb_sqi_clock.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_sqi_clock.FormattingEnabled = True
        Me.cb_sqi_clock.Items.AddRange(New Object() {"40MHz", "20MHz", "10MHz", "5MHz", "2MHz", "1MHz"})
        Me.cb_sqi_clock.Location = New System.Drawing.Point(154, 34)
        Me.cb_sqi_clock.Name = "cb_sqi_clock"
        Me.cb_sqi_clock.Size = New System.Drawing.Size(90, 21)
        Me.cb_sqi_clock.TabIndex = 6
        '
        'Label21
        '
        Me.Label21.AutoSize = True
        Me.Label21.Location = New System.Drawing.Point(153, 18)
        Me.Label21.Name = "Label21"
        Me.Label21.Size = New System.Drawing.Size(103, 13)
        Me.Label21.TabIndex = 7
        Me.Label21.Text = "SQI maximum speed"
        '
        'cb_spi_eeprom
        '
        Me.cb_spi_eeprom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_spi_eeprom.FormattingEnabled = True
        Me.cb_spi_eeprom.Location = New System.Drawing.Point(298, 34)
        Me.cb_spi_eeprom.Name = "cb_spi_eeprom"
        Me.cb_spi_eeprom.Size = New System.Drawing.Size(156, 21)
        Me.cb_spi_eeprom.TabIndex = 4
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(297, 18)
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
        Me.lbl_read_cmd.Size = New System.Drawing.Size(85, 13)
        Me.lbl_read_cmd.TabIndex = 2
        Me.lbl_read_cmd.Text = "Read command:"
        '
        'cb_spi_clock
        '
        Me.cb_spi_clock.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_spi_clock.FormattingEnabled = True
        Me.cb_spi_clock.Items.AddRange(New Object() {"32MHz", "24Mhz", "16Mhz", "12Mhz", "8Mhz", "4Mhz", "2Mhz", "1Mhz", "", ""})
        Me.cb_spi_clock.Location = New System.Drawing.Point(15, 34)
        Me.cb_spi_clock.Name = "cb_spi_clock"
        Me.cb_spi_clock.Size = New System.Drawing.Size(90, 21)
        Me.cb_spi_clock.TabIndex = 0
        '
        'rb_fastread_op
        '
        Me.rb_fastread_op.AutoSize = True
        Me.rb_fastread_op.Location = New System.Drawing.Point(298, 63)
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
        Me.rb_read_op.Location = New System.Drawing.Point(157, 65)
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
        Me.Label1.Location = New System.Drawing.Point(13, 18)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(102, 13)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "SPI maximum speed"
        '
        'TP_I2C
        '
        Me.TP_I2C.BackColor = System.Drawing.SystemColors.Control
        Me.TP_I2C.Controls.Add(Me.GroupBox8)
        Me.TP_I2C.Controls.Add(Me.GroupBox6)
        Me.TP_I2C.Controls.Add(Me.GroupBox5)
        Me.TP_I2C.Controls.Add(Me.GroupBox4)
        Me.TP_I2C.Location = New System.Drawing.Point(4, 22)
        Me.TP_I2C.Name = "TP_I2C"
        Me.TP_I2C.Padding = New System.Windows.Forms.Padding(3)
        Me.TP_I2C.Size = New System.Drawing.Size(524, 328)
        Me.TP_I2C.TabIndex = 2
        Me.TP_I2C.Text = " I ² C  / SWI "
        '
        'GroupBox8
        '
        Me.GroupBox8.Controls.Add(Me.cb_swi_a0)
        Me.GroupBox8.Controls.Add(Me.cb_swi_a1)
        Me.GroupBox8.Controls.Add(Me.cb_swi_a2)
        Me.GroupBox8.Location = New System.Drawing.Point(6, 172)
        Me.GroupBox8.Name = "GroupBox8"
        Me.GroupBox8.Size = New System.Drawing.Size(512, 52)
        Me.GroupBox8.TabIndex = 35
        Me.GroupBox8.TabStop = False
        Me.GroupBox8.Text = "SWI Address"
        '
        'cb_swi_a0
        '
        Me.cb_swi_a0.AutoSize = True
        Me.cb_swi_a0.Location = New System.Drawing.Point(274, 19)
        Me.cb_swi_a0.Name = "cb_swi_a0"
        Me.cb_swi_a0.Size = New System.Drawing.Size(39, 17)
        Me.cb_swi_a0.TabIndex = 28
        Me.cb_swi_a0.Text = "A0"
        Me.cb_swi_a0.UseVisualStyleBackColor = True
        '
        'cb_swi_a1
        '
        Me.cb_swi_a1.AutoSize = True
        Me.cb_swi_a1.Location = New System.Drawing.Point(219, 19)
        Me.cb_swi_a1.Name = "cb_swi_a1"
        Me.cb_swi_a1.Size = New System.Drawing.Size(39, 17)
        Me.cb_swi_a1.TabIndex = 29
        Me.cb_swi_a1.Text = "A1"
        Me.cb_swi_a1.UseVisualStyleBackColor = True
        '
        'cb_swi_a2
        '
        Me.cb_swi_a2.AutoSize = True
        Me.cb_swi_a2.Location = New System.Drawing.Point(164, 19)
        Me.cb_swi_a2.Name = "cb_swi_a2"
        Me.cb_swi_a2.Size = New System.Drawing.Size(39, 17)
        Me.cb_swi_a2.TabIndex = 30
        Me.cb_swi_a2.Text = "A2"
        Me.cb_swi_a2.UseVisualStyleBackColor = True
        '
        'GroupBox6
        '
        Me.GroupBox6.Controls.Add(Me.cb_i2c_device)
        Me.GroupBox6.Location = New System.Drawing.Point(6, 57)
        Me.GroupBox6.Name = "GroupBox6"
        Me.GroupBox6.Size = New System.Drawing.Size(512, 51)
        Me.GroupBox6.TabIndex = 35
        Me.GroupBox6.TabStop = False
        Me.GroupBox6.Text = "I2C Device"
        '
        'cb_i2c_device
        '
        Me.cb_i2c_device.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_i2c_device.Font = New System.Drawing.Font("Consolas", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.cb_i2c_device.FormattingEnabled = True
        Me.cb_i2c_device.Location = New System.Drawing.Point(137, 19)
        Me.cb_i2c_device.Name = "cb_i2c_device"
        Me.cb_i2c_device.Size = New System.Drawing.Size(187, 22)
        Me.cb_i2c_device.TabIndex = 36
        '
        'GroupBox5
        '
        Me.GroupBox5.Controls.Add(Me.cb_i2c_a0)
        Me.GroupBox5.Controls.Add(Me.cb_i2c_a1)
        Me.GroupBox5.Controls.Add(Me.cb_i2c_a2)
        Me.GroupBox5.Location = New System.Drawing.Point(6, 114)
        Me.GroupBox5.Name = "GroupBox5"
        Me.GroupBox5.Size = New System.Drawing.Size(512, 52)
        Me.GroupBox5.TabIndex = 34
        Me.GroupBox5.TabStop = False
        Me.GroupBox5.Text = "I2C Address"
        '
        'cb_i2c_a0
        '
        Me.cb_i2c_a0.AutoSize = True
        Me.cb_i2c_a0.Location = New System.Drawing.Point(274, 19)
        Me.cb_i2c_a0.Name = "cb_i2c_a0"
        Me.cb_i2c_a0.Size = New System.Drawing.Size(39, 17)
        Me.cb_i2c_a0.TabIndex = 28
        Me.cb_i2c_a0.Text = "A0"
        Me.cb_i2c_a0.UseVisualStyleBackColor = True
        '
        'cb_i2c_a1
        '
        Me.cb_i2c_a1.AutoSize = True
        Me.cb_i2c_a1.Location = New System.Drawing.Point(219, 19)
        Me.cb_i2c_a1.Name = "cb_i2c_a1"
        Me.cb_i2c_a1.Size = New System.Drawing.Size(39, 17)
        Me.cb_i2c_a1.TabIndex = 29
        Me.cb_i2c_a1.Text = "A1"
        Me.cb_i2c_a1.UseVisualStyleBackColor = True
        '
        'cb_i2c_a2
        '
        Me.cb_i2c_a2.AutoSize = True
        Me.cb_i2c_a2.Location = New System.Drawing.Point(164, 19)
        Me.cb_i2c_a2.Name = "cb_i2c_a2"
        Me.cb_i2c_a2.Size = New System.Drawing.Size(39, 17)
        Me.cb_i2c_a2.TabIndex = 30
        Me.cb_i2c_a2.Text = "A2"
        Me.cb_i2c_a2.UseVisualStyleBackColor = True
        '
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.rb_speed_1mhz)
        Me.GroupBox4.Controls.Add(Me.rb_speed_400khz)
        Me.GroupBox4.Controls.Add(Me.rb_speed_100khz)
        Me.GroupBox4.Location = New System.Drawing.Point(6, 6)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(512, 45)
        Me.GroupBox4.TabIndex = 33
        Me.GroupBox4.TabStop = False
        Me.GroupBox4.Text = "I2C SCL Speed"
        '
        'rb_speed_1mhz
        '
        Me.rb_speed_1mhz.AutoSize = True
        Me.rb_speed_1mhz.Location = New System.Drawing.Point(323, 19)
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
        Me.rb_speed_400khz.Location = New System.Drawing.Point(172, 19)
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
        Me.rb_speed_100khz.Location = New System.Drawing.Point(76, 19)
        Me.rb_speed_100khz.Name = "rb_speed_100khz"
        Me.rb_speed_100khz.Size = New System.Drawing.Size(65, 17)
        Me.rb_speed_100khz.TabIndex = 34
        Me.rb_speed_100khz.TabStop = True
        Me.rb_speed_100khz.Text = "100 kHz"
        Me.rb_speed_100khz.UseVisualStyleBackColor = True
        '
        'TP_NAND1
        '
        Me.TP_NAND1.BackColor = System.Drawing.SystemColors.Control
        Me.TP_NAND1.Controls.Add(Me.gb_block_layout)
        Me.TP_NAND1.Controls.Add(Me.gb_block_manager)
        Me.TP_NAND1.Controls.Add(Me.gb_nand_general)
        Me.TP_NAND1.Location = New System.Drawing.Point(4, 22)
        Me.TP_NAND1.Name = "TP_NAND1"
        Me.TP_NAND1.Padding = New System.Windows.Forms.Padding(3)
        Me.TP_NAND1.Size = New System.Drawing.Size(524, 328)
        Me.TP_NAND1.TabIndex = 3
        Me.TP_NAND1.Text = " NAND"
        '
        'gb_block_layout
        '
        Me.gb_block_layout.Controls.Add(Me.nand_box)
        Me.gb_block_layout.Controls.Add(Me.rb_mainspare_all)
        Me.gb_block_layout.Controls.Add(Me.rb_mainspare_segmented)
        Me.gb_block_layout.Controls.Add(Me.rb_mainspare_default)
        Me.gb_block_layout.Location = New System.Drawing.Point(6, 245)
        Me.gb_block_layout.Name = "gb_block_layout"
        Me.gb_block_layout.Size = New System.Drawing.Size(512, 77)
        Me.gb_block_layout.TabIndex = 2
        Me.gb_block_layout.TabStop = False
        Me.gb_block_layout.Text = "Page layout"
        '
        'nand_box
        '
        Me.nand_box.Location = New System.Drawing.Point(69, 19)
        Me.nand_box.Name = "nand_box"
        Me.nand_box.Size = New System.Drawing.Size(356, 20)
        Me.nand_box.TabIndex = 4
        Me.nand_box.TabStop = False
        '
        'rb_mainspare_all
        '
        Me.rb_mainspare_all.AutoSize = True
        Me.rb_mainspare_all.Location = New System.Drawing.Point(322, 51)
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
        Me.rb_mainspare_segmented.Location = New System.Drawing.Point(194, 51)
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
        Me.rb_mainspare_default.Location = New System.Drawing.Point(86, 51)
        Me.rb_mainspare_default.Name = "rb_mainspare_default"
        Me.rb_mainspare_default.Size = New System.Drawing.Size(68, 17)
        Me.rb_mainspare_default.TabIndex = 1
        Me.rb_mainspare_default.TabStop = True
        Me.rb_mainspare_default.Text = "Separate"
        Me.rb_mainspare_default.UseVisualStyleBackColor = True
        '
        'gb_block_manager
        '
        Me.gb_block_manager.Controls.Add(Me.cb_badblock_enabled)
        Me.gb_block_manager.Controls.Add(Me.cb_badmarker_6th_page2)
        Me.gb_block_manager.Controls.Add(Me.cb_badmarker_6th_page1)
        Me.gb_block_manager.Controls.Add(Me.cb_badmarker_1st_lastpage)
        Me.gb_block_manager.Controls.Add(Me.cb_badmarker_1st_page2)
        Me.gb_block_manager.Controls.Add(Me.cb_badmarker_1st_page1)
        Me.gb_block_manager.Controls.Add(Me.lbl_6th_byte)
        Me.gb_block_manager.Controls.Add(Me.lbl_1st_byte)
        Me.gb_block_manager.Controls.Add(Me.cb_badblock_disabled)
        Me.gb_block_manager.Location = New System.Drawing.Point(6, 124)
        Me.gb_block_manager.Name = "gb_block_manager"
        Me.gb_block_manager.Size = New System.Drawing.Size(512, 115)
        Me.gb_block_manager.TabIndex = 0
        Me.gb_block_manager.TabStop = False
        Me.gb_block_manager.Text = "Bad block manager"
        '
        'cb_badblock_enabled
        '
        Me.cb_badblock_enabled.AutoSize = True
        Me.cb_badblock_enabled.Location = New System.Drawing.Point(143, 19)
        Me.cb_badblock_enabled.Name = "cb_badblock_enabled"
        Me.cb_badblock_enabled.Size = New System.Drawing.Size(208, 17)
        Me.cb_badblock_enabled.TabIndex = 1
        Me.cb_badblock_enabled.TabStop = True
        Me.cb_badblock_enabled.Text = "Enabled (check for bad block markers)"
        Me.cb_badblock_enabled.UseVisualStyleBackColor = True
        '
        'cb_badmarker_6th_page2
        '
        Me.cb_badmarker_6th_page2.AutoSize = True
        Me.cb_badmarker_6th_page2.Location = New System.Drawing.Point(208, 66)
        Me.cb_badmarker_6th_page2.Name = "cb_badmarker_6th_page2"
        Me.cb_badmarker_6th_page2.Size = New System.Drawing.Size(119, 17)
        Me.cb_badmarker_6th_page2.TabIndex = 8
        Me.cb_badmarker_6th_page2.Text = "Second spare page"
        Me.cb_badmarker_6th_page2.UseVisualStyleBackColor = True
        '
        'cb_badmarker_6th_page1
        '
        Me.cb_badmarker_6th_page1.AutoSize = True
        Me.cb_badmarker_6th_page1.Location = New System.Drawing.Point(78, 66)
        Me.cb_badmarker_6th_page1.Name = "cb_badmarker_6th_page1"
        Me.cb_badmarker_6th_page1.Size = New System.Drawing.Size(101, 17)
        Me.cb_badmarker_6th_page1.TabIndex = 7
        Me.cb_badmarker_6th_page1.Text = "First spare page"
        Me.cb_badmarker_6th_page1.UseVisualStyleBackColor = True
        '
        'cb_badmarker_1st_lastpage
        '
        Me.cb_badmarker_1st_lastpage.AutoSize = True
        Me.cb_badmarker_1st_lastpage.Location = New System.Drawing.Point(356, 45)
        Me.cb_badmarker_1st_lastpage.Name = "cb_badmarker_1st_lastpage"
        Me.cb_badmarker_1st_lastpage.Size = New System.Drawing.Size(102, 17)
        Me.cb_badmarker_1st_lastpage.TabIndex = 4
        Me.cb_badmarker_1st_lastpage.Text = "Last spare page"
        Me.cb_badmarker_1st_lastpage.UseVisualStyleBackColor = True
        '
        'cb_badmarker_1st_page2
        '
        Me.cb_badmarker_1st_page2.AutoSize = True
        Me.cb_badmarker_1st_page2.Location = New System.Drawing.Point(208, 45)
        Me.cb_badmarker_1st_page2.Name = "cb_badmarker_1st_page2"
        Me.cb_badmarker_1st_page2.Size = New System.Drawing.Size(119, 17)
        Me.cb_badmarker_1st_page2.TabIndex = 3
        Me.cb_badmarker_1st_page2.Text = "Second spare page"
        Me.cb_badmarker_1st_page2.UseVisualStyleBackColor = True
        '
        'cb_badmarker_1st_page1
        '
        Me.cb_badmarker_1st_page1.AutoSize = True
        Me.cb_badmarker_1st_page1.Location = New System.Drawing.Point(78, 45)
        Me.cb_badmarker_1st_page1.Name = "cb_badmarker_1st_page1"
        Me.cb_badmarker_1st_page1.Size = New System.Drawing.Size(101, 17)
        Me.cb_badmarker_1st_page1.TabIndex = 2
        Me.cb_badmarker_1st_page1.Text = "First spare page"
        Me.cb_badmarker_1st_page1.UseVisualStyleBackColor = True
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
        Me.gb_nand_general.Controls.Add(Me.cbNAND_Speed)
        Me.gb_nand_general.Controls.Add(Me.Label2)
        Me.gb_nand_general.Controls.Add(Me.cb_nand_image_readverify)
        Me.gb_nand_general.Controls.Add(Me.cb_spinand_disable_ecc)
        Me.gb_nand_general.Controls.Add(Me.cb_mismatch)
        Me.gb_nand_general.Controls.Add(Me.cb_preserve)
        Me.gb_nand_general.Location = New System.Drawing.Point(6, 6)
        Me.gb_nand_general.Name = "gb_nand_general"
        Me.gb_nand_general.Size = New System.Drawing.Size(512, 112)
        Me.gb_nand_general.TabIndex = 1
        Me.gb_nand_general.TabStop = False
        Me.gb_nand_general.Text = "General"
        '
        'cbNAND_Speed
        '
        Me.cbNAND_Speed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbNAND_Speed.FormattingEnabled = True
        Me.cbNAND_Speed.Items.AddRange(New Object() {"20 MHz", "10 MHz", "5 MHz", "1 MHz"})
        Me.cbNAND_Speed.Location = New System.Drawing.Point(385, 43)
        Me.cbNAND_Speed.Name = "cbNAND_Speed"
        Me.cbNAND_Speed.Size = New System.Drawing.Size(103, 21)
        Me.cbNAND_Speed.TabIndex = 4
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(385, 24)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(103, 13)
        Me.Label2.TabIndex = 5
        Me.Label2.Text = "Mach¹ NAND speed"
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
        Me.cb_preserve.Size = New System.Drawing.Size(136, 17)
        Me.cb_preserve.TabIndex = 0
        Me.cb_preserve.Text = "Preserve memory areas"
        Me.cb_preserve.UseVisualStyleBackColor = True
        '
        'TP_NAND2
        '
        Me.TP_NAND2.BackColor = System.Drawing.SystemColors.Control
        Me.TP_NAND2.Controls.Add(Me.Label25)
        Me.TP_NAND2.Controls.Add(Me.gb_nandecc_title)
        Me.TP_NAND2.Location = New System.Drawing.Point(4, 22)
        Me.TP_NAND2.Name = "TP_NAND2"
        Me.TP_NAND2.Padding = New System.Windows.Forms.Padding(3)
        Me.TP_NAND2.Size = New System.Drawing.Size(524, 328)
        Me.TP_NAND2.TabIndex = 4
        Me.TP_NAND2.Text = "  ECC  "
        '
        'gb_nandecc_title
        '
        Me.gb_nandecc_title.Controls.Add(Me.txt_ecc_location)
        Me.gb_nandecc_title.Controls.Add(Me.cb_ecc_seperate)
        Me.gb_nandecc_title.Controls.Add(Me.lbl_ECC_size)
        Me.gb_nandecc_title.Controls.Add(Me.rb_ECC_BHC)
        Me.gb_nandecc_title.Controls.Add(Me.rb_ECC_ReedSolomon)
        Me.gb_nandecc_title.Controls.Add(Me.rb_ECC_Hamming)
        Me.gb_nandecc_title.Controls.Add(Me.lbl_nandecc_algorithm)
        Me.gb_nandecc_title.Controls.Add(Me.lbl_nandecc_changes)
        Me.gb_nandecc_title.Controls.Add(Me.cb_rs_reverse_data)
        Me.gb_nandecc_title.Controls.Add(Me.cb_sym_width)
        Me.gb_nandecc_title.Controls.Add(Me.lbl_sym_width)
        Me.gb_nandecc_title.Controls.Add(Me.lbl_nandecc_location)
        Me.gb_nandecc_title.Controls.Add(Me.cb_ECC_WriteEnable)
        Me.gb_nandecc_title.Controls.Add(Me.cb_ECC_ReadEnable)
        Me.gb_nandecc_title.Controls.Add(Me.lbl_nandecc_enabled)
        Me.gb_nandecc_title.Controls.Add(Me.cb_ECC_BITERR)
        Me.gb_nandecc_title.Controls.Add(Me.lbl_nandecc_biterror)
        Me.gb_nandecc_title.Location = New System.Drawing.Point(6, 6)
        Me.gb_nandecc_title.Name = "gb_nandecc_title"
        Me.gb_nandecc_title.Size = New System.Drawing.Size(512, 237)
        Me.gb_nandecc_title.TabIndex = 1
        Me.gb_nandecc_title.TabStop = False
        Me.gb_nandecc_title.Text = "Software ECC Feature"
        '
        'txt_ecc_location
        '
        Me.txt_ecc_location.Location = New System.Drawing.Point(366, 137)
        Me.txt_ecc_location.Name = "txt_ecc_location"
        Me.txt_ecc_location.Size = New System.Drawing.Size(64, 20)
        Me.txt_ecc_location.TabIndex = 33
        Me.txt_ecc_location.Text = "0x00"
        '
        'cb_ecc_seperate
        '
        Me.cb_ecc_seperate.AutoSize = True
        Me.cb_ecc_seperate.Location = New System.Drawing.Point(9, 139)
        Me.cb_ecc_seperate.Name = "cb_ecc_seperate"
        Me.cb_ecc_seperate.Size = New System.Drawing.Size(237, 17)
        Me.cb_ecc_seperate.TabIndex = 32
        Me.cb_ecc_seperate.Text = "Separate OOB area for each 512 byte sector"
        Me.cb_ecc_seperate.UseVisualStyleBackColor = True
        '
        'lbl_ECC_size
        '
        Me.lbl_ECC_size.AutoSize = True
        Me.lbl_ECC_size.Location = New System.Drawing.Point(257, 210)
        Me.lbl_ECC_size.Name = "lbl_ECC_size"
        Me.lbl_ECC_size.Size = New System.Drawing.Size(186, 13)
        Me.lbl_ECC_size.TabIndex = 31
        Me.lbl_ECC_size.Text = "ECC data per 512 byte sector: 0 bytes"
        '
        'rb_ECC_BHC
        '
        Me.rb_ECC_BHC.AutoSize = True
        Me.rb_ECC_BHC.Location = New System.Drawing.Point(250, 81)
        Me.rb_ECC_BHC.Name = "rb_ECC_BHC"
        Me.rb_ECC_BHC.Size = New System.Drawing.Size(79, 17)
        Me.rb_ECC_BHC.TabIndex = 4
        Me.rb_ECC_BHC.TabStop = True
        Me.rb_ECC_BHC.Text = "Binary BHC"
        Me.rb_ECC_BHC.UseVisualStyleBackColor = True
        '
        'rb_ECC_ReedSolomon
        '
        Me.rb_ECC_ReedSolomon.AutoSize = True
        Me.rb_ECC_ReedSolomon.Location = New System.Drawing.Point(250, 58)
        Me.rb_ECC_ReedSolomon.Name = "rb_ECC_ReedSolomon"
        Me.rb_ECC_ReedSolomon.Size = New System.Drawing.Size(95, 17)
        Me.rb_ECC_ReedSolomon.TabIndex = 3
        Me.rb_ECC_ReedSolomon.TabStop = True
        Me.rb_ECC_ReedSolomon.Text = "Reed-Solomon"
        Me.rb_ECC_ReedSolomon.UseVisualStyleBackColor = True
        '
        'rb_ECC_Hamming
        '
        Me.rb_ECC_Hamming.AutoSize = True
        Me.rb_ECC_Hamming.Location = New System.Drawing.Point(250, 35)
        Me.rb_ECC_Hamming.Name = "rb_ECC_Hamming"
        Me.rb_ECC_Hamming.Size = New System.Drawing.Size(69, 17)
        Me.rb_ECC_Hamming.TabIndex = 2
        Me.rb_ECC_Hamming.TabStop = True
        Me.rb_ECC_Hamming.Text = "Hamming"
        Me.rb_ECC_Hamming.UseVisualStyleBackColor = True
        '
        'lbl_nandecc_algorithm
        '
        Me.lbl_nandecc_algorithm.AutoSize = True
        Me.lbl_nandecc_algorithm.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbl_nandecc_algorithm.Location = New System.Drawing.Point(247, 19)
        Me.lbl_nandecc_algorithm.Name = "lbl_nandecc_algorithm"
        Me.lbl_nandecc_algorithm.Size = New System.Drawing.Size(59, 13)
        Me.lbl_nandecc_algorithm.TabIndex = 1
        Me.lbl_nandecc_algorithm.Text = "Algorithm"
        '
        'lbl_nandecc_changes
        '
        Me.lbl_nandecc_changes.AutoSize = True
        Me.lbl_nandecc_changes.Location = New System.Drawing.Point(6, 210)
        Me.lbl_nandecc_changes.Name = "lbl_nandecc_changes"
        Me.lbl_nandecc_changes.Size = New System.Drawing.Size(223, 13)
        Me.lbl_nandecc_changes.TabIndex = 11
        Me.lbl_nandecc_changes.Text = "* Changes take effect on device detect event"
        '
        'cb_rs_reverse_data
        '
        Me.cb_rs_reverse_data.AutoSize = True
        Me.cb_rs_reverse_data.Location = New System.Drawing.Point(9, 116)
        Me.cb_rs_reverse_data.Name = "cb_rs_reverse_data"
        Me.cb_rs_reverse_data.Size = New System.Drawing.Size(116, 17)
        Me.cb_rs_reverse_data.TabIndex = 30
        Me.cb_rs_reverse_data.Text = "Reverse byte order"
        Me.cb_rs_reverse_data.UseVisualStyleBackColor = True
        '
        'cb_sym_width
        '
        Me.cb_sym_width.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_sym_width.FormattingEnabled = True
        Me.cb_sym_width.Items.AddRange(New Object() {"9-bit", "10-bit"})
        Me.cb_sym_width.Location = New System.Drawing.Point(366, 85)
        Me.cb_sym_width.Name = "cb_sym_width"
        Me.cb_sym_width.Size = New System.Drawing.Size(80, 21)
        Me.cb_sym_width.TabIndex = 28
        '
        'lbl_sym_width
        '
        Me.lbl_sym_width.AutoSize = True
        Me.lbl_sym_width.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbl_sym_width.Location = New System.Drawing.Point(363, 68)
        Me.lbl_sym_width.Name = "lbl_sym_width"
        Me.lbl_sym_width.Size = New System.Drawing.Size(81, 13)
        Me.lbl_sym_width.TabIndex = 29
        Me.lbl_sym_width.Text = "Symbol width"
        '
        'lbl_nandecc_location
        '
        Me.lbl_nandecc_location.AutoSize = True
        Me.lbl_nandecc_location.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbl_nandecc_location.Location = New System.Drawing.Point(363, 118)
        Me.lbl_nandecc_location.Name = "lbl_nandecc_location"
        Me.lbl_nandecc_location.Size = New System.Drawing.Size(80, 13)
        Me.lbl_nandecc_location.TabIndex = 10
        Me.lbl_nandecc_location.Text = "ECC location"
        '
        'cb_ECC_WriteEnable
        '
        Me.cb_ECC_WriteEnable.AutoSize = True
        Me.cb_ECC_WriteEnable.Location = New System.Drawing.Point(9, 67)
        Me.cb_ECC_WriteEnable.Name = "cb_ECC_WriteEnable"
        Me.cb_ECC_WriteEnable.Size = New System.Drawing.Size(98, 17)
        Me.cb_ECC_WriteEnable.TabIndex = 6
        Me.cb_ECC_WriteEnable.Text = "Write operation"
        Me.cb_ECC_WriteEnable.UseVisualStyleBackColor = True
        '
        'cb_ECC_ReadEnable
        '
        Me.cb_ECC_ReadEnable.AutoSize = True
        Me.cb_ECC_ReadEnable.Location = New System.Drawing.Point(9, 44)
        Me.cb_ECC_ReadEnable.Name = "cb_ECC_ReadEnable"
        Me.cb_ECC_ReadEnable.Size = New System.Drawing.Size(99, 17)
        Me.cb_ECC_ReadEnable.TabIndex = 0
        Me.cb_ECC_ReadEnable.Text = "Read operation"
        Me.cb_ECC_ReadEnable.UseVisualStyleBackColor = True
        '
        'lbl_nandecc_enabled
        '
        Me.lbl_nandecc_enabled.AutoSize = True
        Me.lbl_nandecc_enabled.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbl_nandecc_enabled.Location = New System.Drawing.Point(6, 27)
        Me.lbl_nandecc_enabled.Name = "lbl_nandecc_enabled"
        Me.lbl_nandecc_enabled.Size = New System.Drawing.Size(53, 13)
        Me.lbl_nandecc_enabled.TabIndex = 8
        Me.lbl_nandecc_enabled.Text = "Enabled"
        '
        'cb_ECC_BITERR
        '
        Me.cb_ECC_BITERR.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_ECC_BITERR.FormattingEnabled = True
        Me.cb_ECC_BITERR.Items.AddRange(New Object() {"1-bit", "2-bit", "4-bit", "8-bit", "10-bit", "14-bit"})
        Me.cb_ECC_BITERR.Location = New System.Drawing.Point(366, 36)
        Me.cb_ECC_BITERR.Name = "cb_ECC_BITERR"
        Me.cb_ECC_BITERR.Size = New System.Drawing.Size(80, 21)
        Me.cb_ECC_BITERR.TabIndex = 5
        '
        'lbl_nandecc_biterror
        '
        Me.lbl_nandecc_biterror.AutoSize = True
        Me.lbl_nandecc_biterror.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbl_nandecc_biterror.Location = New System.Drawing.Point(363, 19)
        Me.lbl_nandecc_biterror.Name = "lbl_nandecc_biterror"
        Me.lbl_nandecc_biterror.Size = New System.Drawing.Size(52, 13)
        Me.lbl_nandecc_biterror.TabIndex = 7
        Me.lbl_nandecc_biterror.Text = "Bit-error"
        '
        'TP_GEN
        '
        Me.TP_GEN.BackColor = System.Drawing.SystemColors.Control
        Me.TP_GEN.Controls.Add(Me.GroupBox9)
        Me.TP_GEN.Controls.Add(Me.GroupBox7)
        Me.TP_GEN.Controls.Add(Me.GroupBox2)
        Me.TP_GEN.Controls.Add(Me.GroupBox3)
        Me.TP_GEN.Location = New System.Drawing.Point(4, 22)
        Me.TP_GEN.Name = "TP_GEN"
        Me.TP_GEN.Padding = New System.Windows.Forms.Padding(3)
        Me.TP_GEN.Size = New System.Drawing.Size(524, 328)
        Me.TP_GEN.TabIndex = 5
        Me.TP_GEN.Text = "  General  "
        '
        'GroupBox9
        '
        Me.GroupBox9.Controls.Add(Me.cb_nor_we_pulse)
        Me.GroupBox9.Controls.Add(Me.cb_nor_read_access)
        Me.GroupBox9.Controls.Add(Me.Label24)
        Me.GroupBox9.Controls.Add(Me.Label23)
        Me.GroupBox9.Location = New System.Drawing.Point(256, 123)
        Me.GroupBox9.Name = "GroupBox9"
        Me.GroupBox9.Size = New System.Drawing.Size(262, 151)
        Me.GroupBox9.TabIndex = 54
        Me.GroupBox9.TabStop = False
        Me.GroupBox9.Text = "Parallel NOR timings (Mach¹)"
        '
        'cb_nor_we_pulse
        '
        Me.cb_nor_we_pulse.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_nor_we_pulse.FormattingEnabled = True
        Me.cb_nor_we_pulse.Items.AddRange(New Object() {"50 ns", "75 ns", "100 ns", "125 ns", "150 ns", "175 ns", "200 ns", "250 ns"})
        Me.cb_nor_we_pulse.Location = New System.Drawing.Point(11, 94)
        Me.cb_nor_we_pulse.Name = "cb_nor_we_pulse"
        Me.cb_nor_we_pulse.Size = New System.Drawing.Size(109, 21)
        Me.cb_nor_we_pulse.TabIndex = 47
        '
        'cb_nor_read_access
        '
        Me.cb_nor_read_access.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_nor_read_access.FormattingEnabled = True
        Me.cb_nor_read_access.Items.AddRange(New Object() {"50 ns", "75 ns", "100 ns", "125 ns", "150 ns", "175 ns", "200 ns", "250 ns"})
        Me.cb_nor_read_access.Location = New System.Drawing.Point(11, 40)
        Me.cb_nor_read_access.Name = "cb_nor_read_access"
        Me.cb_nor_read_access.Size = New System.Drawing.Size(109, 21)
        Me.cb_nor_read_access.TabIndex = 46
        '
        'Label24
        '
        Me.Label24.AutoSize = True
        Me.Label24.Location = New System.Drawing.Point(12, 76)
        Me.Label24.Name = "Label24"
        Me.Label24.Size = New System.Drawing.Size(60, 13)
        Me.Label24.TabIndex = 56
        Me.Label24.Text = "Write-pulse"
        '
        'Label23
        '
        Me.Label23.AutoSize = True
        Me.Label23.Location = New System.Drawing.Point(12, 22)
        Me.Label23.Name = "Label23"
        Me.Label23.Size = New System.Drawing.Size(70, 13)
        Me.Label23.TabIndex = 55
        Me.Label23.Text = "Read-access"
        '
        'GroupBox7
        '
        Me.GroupBox7.Controls.Add(Me.lbl_s93_size)
        Me.GroupBox7.Controls.Add(Me.cb_s93_devices)
        Me.GroupBox7.Controls.Add(Me.Label18)
        Me.GroupBox7.Controls.Add(Me.cb_s93_org)
        Me.GroupBox7.Controls.Add(Me.Label19)
        Me.GroupBox7.Location = New System.Drawing.Point(256, 6)
        Me.GroupBox7.Name = "GroupBox7"
        Me.GroupBox7.Size = New System.Drawing.Size(262, 111)
        Me.GroupBox7.TabIndex = 53
        Me.GroupBox7.TabStop = False
        Me.GroupBox7.Text = "Microwire"
        '
        'lbl_s93_size
        '
        Me.lbl_s93_size.AutoSize = True
        Me.lbl_s93_size.Location = New System.Drawing.Point(12, 76)
        Me.lbl_s93_size.Name = "lbl_s93_size"
        Me.lbl_s93_size.Size = New System.Drawing.Size(84, 13)
        Me.lbl_s93_size.TabIndex = 54
        Me.lbl_s93_size.Text = "Device Size: XX"
        '
        'cb_s93_devices
        '
        Me.cb_s93_devices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_s93_devices.FormattingEnabled = True
        Me.cb_s93_devices.Location = New System.Drawing.Point(11, 40)
        Me.cb_s93_devices.Name = "cb_s93_devices"
        Me.cb_s93_devices.Size = New System.Drawing.Size(171, 21)
        Me.cb_s93_devices.TabIndex = 41
        '
        'Label18
        '
        Me.Label18.AutoSize = True
        Me.Label18.Location = New System.Drawing.Point(11, 22)
        Me.Label18.Name = "Label18"
        Me.Label18.Size = New System.Drawing.Size(90, 13)
        Me.Label18.TabIndex = 42
        Me.Label18.Text = "EEPROM Device"
        '
        'cb_s93_org
        '
        Me.cb_s93_org.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_s93_org.FormattingEnabled = True
        Me.cb_s93_org.Items.AddRange(New Object() {"8-bit", "16-bit"})
        Me.cb_s93_org.Location = New System.Drawing.Point(188, 40)
        Me.cb_s93_org.Name = "cb_s93_org"
        Me.cb_s93_org.Size = New System.Drawing.Size(63, 21)
        Me.cb_s93_org.TabIndex = 43
        '
        'Label19
        '
        Me.Label19.AutoSize = True
        Me.Label19.Location = New System.Drawing.Point(185, 22)
        Me.Label19.Name = "Label19"
        Me.Label19.Size = New System.Drawing.Size(66, 13)
        Me.Label19.TabIndex = 44
        Me.Label19.Text = "Organization"
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.cb_jtag_tck_speed)
        Me.GroupBox2.Controls.Add(Me.Label22)
        Me.GroupBox2.Location = New System.Drawing.Point(6, 190)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(244, 84)
        Me.GroupBox2.TabIndex = 52
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "JTAG (Professional)"
        '
        'cb_jtag_tck_speed
        '
        Me.cb_jtag_tck_speed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_jtag_tck_speed.FormattingEnabled = True
        Me.cb_jtag_tck_speed.Items.AddRange(New Object() {"10 MHz", "20 MHz", "40 MHz"})
        Me.cb_jtag_tck_speed.Location = New System.Drawing.Point(9, 45)
        Me.cb_jtag_tck_speed.Name = "cb_jtag_tck_speed"
        Me.cb_jtag_tck_speed.Size = New System.Drawing.Size(117, 21)
        Me.cb_jtag_tck_speed.TabIndex = 53
        '
        'Label22
        '
        Me.Label22.AutoSize = True
        Me.Label22.Location = New System.Drawing.Point(9, 27)
        Me.Label22.Name = "Label22"
        Me.Label22.Size = New System.Drawing.Size(62, 13)
        Me.Label22.TabIndex = 54
        Me.Label22.Text = "TCK Speed"
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.Label16)
        Me.GroupBox3.Controls.Add(Me.cbSrec)
        Me.GroupBox3.Controls.Add(Me.cb_ce_select)
        Me.GroupBox3.Controls.Add(Me.cb_retry_write)
        Me.GroupBox3.Controls.Add(Me.Label17)
        Me.GroupBox3.Controls.Add(Me.Label20)
        Me.GroupBox3.Controls.Add(Me.cb_multi_ce)
        Me.GroupBox3.Location = New System.Drawing.Point(6, 6)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(244, 178)
        Me.GroupBox3.TabIndex = 1
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "General"
        '
        'Label16
        '
        Me.Label16.AutoSize = True
        Me.Label16.Location = New System.Drawing.Point(9, 126)
        Me.Label16.Name = "Label16"
        Me.Label16.Size = New System.Drawing.Size(104, 13)
        Me.Label16.TabIndex = 49
        Me.Label16.Text = "S-Record data width"
        '
        'cbSrec
        '
        Me.cbSrec.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbSrec.FormattingEnabled = True
        Me.cbSrec.Items.AddRange(New Object() {"8-bit (byte)", "16-bit (word)"})
        Me.cbSrec.Location = New System.Drawing.Point(9, 144)
        Me.cbSrec.Name = "cbSrec"
        Me.cbSrec.Size = New System.Drawing.Size(104, 21)
        Me.cbSrec.TabIndex = 48
        '
        'cb_ce_select
        '
        Me.cb_ce_select.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_ce_select.FormattingEnabled = True
        Me.cb_ce_select.Items.AddRange(New Object() {"A18", "A19", "A20", "A21", "A22", "A23", "A24"})
        Me.cb_ce_select.Location = New System.Drawing.Point(98, 90)
        Me.cb_ce_select.Name = "cb_ce_select"
        Me.cb_ce_select.Size = New System.Drawing.Size(63, 21)
        Me.cb_ce_select.TabIndex = 47
        '
        'cb_retry_write
        '
        Me.cb_retry_write.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_retry_write.FormattingEnabled = True
        Me.cb_retry_write.Items.AddRange(New Object() {"One time", "Two times", "Three times", "Four times", "Five times"})
        Me.cb_retry_write.Location = New System.Drawing.Point(9, 39)
        Me.cb_retry_write.Name = "cb_retry_write"
        Me.cb_retry_write.Size = New System.Drawing.Size(117, 21)
        Me.cb_retry_write.TabIndex = 39
        '
        'Label17
        '
        Me.Label17.AutoSize = True
        Me.Label17.Location = New System.Drawing.Point(9, 21)
        Me.Label17.Name = "Label17"
        Me.Label17.Size = New System.Drawing.Size(112, 13)
        Me.Label17.TabIndex = 40
        Me.Label17.Text = "Re-attempt write verify"
        '
        'Label20
        '
        Me.Label20.AutoSize = True
        Me.Label20.Location = New System.Drawing.Point(9, 72)
        Me.Label20.Name = "Label20"
        Me.Label20.Size = New System.Drawing.Size(153, 13)
        Me.Label20.TabIndex = 46
        Me.Label20.Text = "Multi-chip select (Parallel NOR)"
        '
        'cb_multi_ce
        '
        Me.cb_multi_ce.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_multi_ce.FormattingEnabled = True
        Me.cb_multi_ce.Items.AddRange(New Object() {"Disabled", "Enabled"})
        Me.cb_multi_ce.Location = New System.Drawing.Point(9, 90)
        Me.cb_multi_ce.Name = "cb_multi_ce"
        Me.cb_multi_ce.Size = New System.Drawing.Size(83, 21)
        Me.cb_multi_ce.TabIndex = 45
        '
        'Label25
        '
        Me.Label25.AutoSize = True
        Me.Label25.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label25.Location = New System.Drawing.Point(95, 246)
        Me.Label25.Name = "Label25"
        Me.Label25.Size = New System.Drawing.Size(341, 13)
        Me.Label25.TabIndex = 12
        Me.Label25.Text = "This feature is only available to commercial license holders"
        '
        'FrmSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(543, 363)
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
        Me.GroupBox8.ResumeLayout(False)
        Me.GroupBox8.PerformLayout()
        Me.GroupBox6.ResumeLayout(False)
        Me.GroupBox5.ResumeLayout(False)
        Me.GroupBox5.PerformLayout()
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox4.PerformLayout()
        Me.TP_NAND1.ResumeLayout(False)
        Me.gb_block_layout.ResumeLayout(False)
        Me.gb_block_layout.PerformLayout()
        CType(Me.nand_box, System.ComponentModel.ISupportInitialize).EndInit()
        Me.gb_block_manager.ResumeLayout(False)
        Me.gb_block_manager.PerformLayout()
        Me.gb_nand_general.ResumeLayout(False)
        Me.gb_nand_general.PerformLayout()
        Me.TP_NAND2.ResumeLayout(False)
        Me.TP_NAND2.PerformLayout()
        Me.gb_nandecc_title.ResumeLayout(False)
        Me.gb_nandecc_title.PerformLayout()
        Me.TP_GEN.ResumeLayout(False)
        Me.GroupBox9.ResumeLayout(False)
        Me.GroupBox9.PerformLayout()
        Me.GroupBox7.ResumeLayout(False)
        Me.GroupBox7.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents MyTabs As TabControl
    Friend WithEvents TP_SPI As TabPage
    Friend WithEvents TP_I2C As TabPage
    Friend WithEvents TP_NAND1 As TabPage
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
    Friend WithEvents cb_i2c_a2 As CheckBox
    Friend WithEvents cb_i2c_a1 As CheckBox
    Friend WithEvents cb_i2c_a0 As CheckBox
    Friend WithEvents GroupBox4 As GroupBox
    Friend WithEvents rb_speed_1mhz As RadioButton
    Friend WithEvents rb_speed_400khz As RadioButton
    Friend WithEvents rb_speed_100khz As RadioButton
    Friend WithEvents GroupBox5 As GroupBox
    Friend WithEvents GroupBox6 As GroupBox
    Friend WithEvents cb_i2c_device As ComboBox
    Friend WithEvents cb_spi_eeprom As ComboBox
    Friend WithEvents Label3 As Label
    Friend WithEvents Label6 As Label
    Friend WithEvents cb_addr_size As ComboBox
    Friend WithEvents cbEN4B As CheckBox
    Friend WithEvents Label13 As Label
    Friend WithEvents gb_block_layout As GroupBox
    Friend WithEvents rb_mainspare_segmented As RadioButton
    Friend WithEvents rb_mainspare_default As RadioButton
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
    Friend WithEvents TP_NAND2 As TabPage
    Friend WithEvents cb_ECC_ReadEnable As CheckBox
    Friend WithEvents gb_nandecc_title As GroupBox
    Friend WithEvents lbl_nandecc_algorithm As Label
    Friend WithEvents lbl_nandecc_biterror As Label
    Friend WithEvents cb_ECC_WriteEnable As CheckBox
    Friend WithEvents cb_ECC_BITERR As ComboBox
    Friend WithEvents rb_ECC_BHC As RadioButton
    Friend WithEvents rb_ECC_ReedSolomon As RadioButton
    Friend WithEvents rb_ECC_Hamming As RadioButton
    Friend WithEvents lbl_nandecc_enabled As Label
    Friend WithEvents lbl_nandecc_location As Label
    Friend WithEvents lbl_nandecc_changes As Label
    Friend WithEvents cb_rs_reverse_data As CheckBox
    Friend WithEvents cb_sym_width As ComboBox
    Friend WithEvents lbl_sym_width As Label
    Friend WithEvents TP_GEN As TabPage
    Friend WithEvents lbl_ECC_size As Label
    Friend WithEvents cb_ecc_seperate As CheckBox
    Friend WithEvents txt_ecc_location As TextBox
    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents Label17 As Label
    Friend WithEvents cb_retry_write As ComboBox
    Friend WithEvents Label18 As Label
    Friend WithEvents cb_s93_devices As ComboBox
    Friend WithEvents Label19 As Label
    Friend WithEvents cb_s93_org As ComboBox
    Friend WithEvents Label20 As Label
    Friend WithEvents cb_multi_ce As ComboBox
    Friend WithEvents cb_ce_select As ComboBox
    Friend WithEvents Label16 As Label
    Friend WithEvents cbSrec As ComboBox
    Friend WithEvents GroupBox7 As GroupBox
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents cb_jtag_tck_speed As ComboBox
    Friend WithEvents Label22 As Label
    Friend WithEvents cb_sqi_clock As ComboBox
    Friend WithEvents Label21 As Label
    Friend WithEvents GroupBox8 As GroupBox
    Friend WithEvents cb_swi_a0 As CheckBox
    Friend WithEvents cb_swi_a1 As CheckBox
    Friend WithEvents cb_swi_a2 As CheckBox
    Friend WithEvents lbl_s93_size As Label
    Friend WithEvents cbNAND_Speed As ComboBox
    Friend WithEvents Label2 As Label
    Friend WithEvents GroupBox9 As GroupBox
    Friend WithEvents cb_nor_we_pulse As ComboBox
    Friend WithEvents cb_nor_read_access As ComboBox
    Friend WithEvents Label24 As Label
    Friend WithEvents Label23 As Label
    Friend WithEvents Label25 As Label
End Class
