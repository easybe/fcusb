Imports System.ComponentModel
Imports FlashcatUSB.ECC_LIB
Imports FlashcatUSB.USB

Public Class FrmSettings
    Private otp_devices As New List(Of FlashMemory.OTP_EPROM)
    Private one_mhz As UInt32 = 1000000

    Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
    End Sub

    Private Sub FrmSettings_Load(sender As Object, e As EventArgs) Handles Me.Load
        Language_setup()
        Microwire_Init()
        Me.MyTabs.DrawMode = TabDrawMode.OwnerDrawFixed
        If (MySettings.MULTI_CE = 0) Then
            cb_ce_select.SelectedIndex = 0
        ElseIf (MySettings.MULTI_CE < 18) Then
            cb_ce_select.SelectedIndex = 0
        Else
            cb_ce_select.SelectedIndex = (MySettings.MULTI_CE - 17)
        End If
        SPI_SetMaximumClockSettings()
        If MySettings.SPI_FASTREAD Then
            rb_fastread_op.Checked = True
        Else
            rb_read_op.Checked = True
        End If
        Dim all_bytes(254) As String
        For i = 1 To 255
            all_bytes(i - 1) = "0x" & Hex(i).PadLeft(2, "0"c)
        Next
        op_read.Items.AddRange(all_bytes)
        op_prog.Items.AddRange(all_bytes)
        op_sectorerase.Items.AddRange(all_bytes)
        op_we.Items.AddRange(all_bytes)
        op_ce.Items.AddRange(all_bytes)
        op_rs.Items.AddRange(all_bytes)
        op_ws.Items.AddRange(all_bytes)
        op_ewsr.Items.AddRange(all_bytes)
        CustomDevice_LoadSettings()
        cb_preserve.Checked = MySettings.NAND_Preserve
        cb_mismatch.Checked = MySettings.NAND_SkipBadBlock
        Dim markers As Integer = MySettings.NAND_BadBlockMarkers
        If (markers And BadBlockMarker._1stByte_FirstPage) > 0 Then
            cb_badmarker_1st_page1.Checked = True
        End If
        If (markers And BadBlockMarker._1stByte_SecondPage) > 0 Then
            cb_badmarker_1st_page2.Checked = True
        End If
        If (markers And BadBlockMarker._1stByte_LastPage) > 0 Then
            cb_badmarker_1st_lastpage.Checked = True
        End If
        If (markers And BadBlockMarker._6thByte_FirstPage) > 0 Then
            cb_badmarker_6th_page1.Checked = True
        End If
        If (markers And BadBlockMarker._6thByte_SecondPage) > 0 Then
            cb_badmarker_6th_page2.Checked = True
        End If
        Select Case MySettings.NAND_BadBlockManager
            Case BadBlockMode.Disabled
                cb_badblock_disabled.Checked = True
            Case BadBlockMode.Enabled
                cb_badblock_enabled.Checked = True
        End Select
        Select Case MySettings.NAND_Layout
            Case NandMemLayout.Separated
                rb_mainspare_default.Checked = True
            Case NandMemLayout.Segmented
                rb_mainspare_segmented.Checked = True
        End Select
        cbNAND_Speed.SelectedIndex = MySettings.NAND_Speed
        SetupSpiEeprom()
        Setup_I2C_SWI_tab()

        If MAIN_FCUSB Is Nothing Then
            rb_speed_100khz.Enabled = False
            rb_speed_1mhz.Enabled = False
            rb_speed_400khz.Checked = True
            rb_read_op.Enabled = False
            rb_fastread_op.Enabled = False
            rb_read_op.Checked = True
        Else
            If Not MAIN_FCUSB.IS_CONNECTED Then
            ElseIf MAIN_FCUSB.HWBOARD = FCUSB_BOARD.Professional_PCB5 Then
            ElseIf MAIN_FCUSB.HWBOARD = FCUSB_BOARD.Mach1 Then
            Else
                rb_speed_100khz.Enabled = False
                rb_speed_1mhz.Enabled = False
                rb_speed_400khz.Checked = True
                rb_read_op.Enabled = False
                rb_fastread_op.Enabled = False
                rb_read_op.Checked = True
            End If
        End If
        cb_spinand_disable_ecc.Checked = MySettings.SPI_NAND_DISABLE_ECC
        cb_nand_image_readverify.Checked = MySettings.NAND_Verify
        ECC_Feature_Load()
        cb_retry_write.SelectedIndex = (MySettings.RETRY_WRITE_ATTEMPTS - 1)
        cbSrec.SelectedIndex = MySettings.SREC_DATAMODE
        Select Case MySettings.JTAG_SPEED
            Case JTAG.JTAG_SPEED._10MHZ
                cb_jtag_tck_speed.SelectedIndex = 0
            Case JTAG.JTAG_SPEED._20MHZ
                cb_jtag_tck_speed.SelectedIndex = 1
            Case JTAG.JTAG_SPEED._40MHZ
                cb_jtag_tck_speed.SelectedIndex = 2
        End Select
        For Each item In cb_nor_read_access.Items
            If item.ToString.StartsWith(CStr(MySettings.NOR_READ_ACCESS)) Then
                cb_nor_read_access.SelectedItem = item
                Exit For
            End If
        Next
        For Each item In cb_nor_we_pulse.Items
            If item.ToString.StartsWith(CStr(MySettings.NOR_WE_PULSE)) Then
                cb_nor_we_pulse.SelectedItem = item
                Exit For
            End If
        Next
        If LicenseStatus = LicenseStatusEnum.LicensedValid Then
            gb_nandecc_title.Enabled = True
        Else
            gb_nandecc_title.Enabled = False
        End If
    End Sub

    Private Sub SPI_SetMaximumClockSettings()
        Select Case MySettings.SPI_CLOCK_MAX
            Case SPI.SPI_SPEED.MHZ_32
                cb_spi_clock.SelectedIndex = 0
            Case SPI.SPI_SPEED.MHZ_24
                cb_spi_clock.SelectedIndex = 1
            Case SPI.SPI_SPEED.MHZ_16
                cb_spi_clock.SelectedIndex = 2
            Case SPI.SPI_SPEED.MHZ_12
                cb_spi_clock.SelectedIndex = 3
            Case SPI.SPI_SPEED.MHZ_8
                cb_spi_clock.SelectedIndex = 4
            Case SPI.SPI_SPEED.MHZ_4
                cb_spi_clock.SelectedIndex = 5
            Case SPI.SPI_SPEED.MHZ_2
                cb_spi_clock.SelectedIndex = 6
            Case SPI.SPI_SPEED.MHZ_1
                cb_spi_clock.SelectedIndex = 7
            Case Else
                cb_spi_clock.SelectedIndex = 4 'SPI_SPEED.MHZ_8
        End Select
        Select Case MySettings.SQI_CLOCK_MAX
            Case SPI.SQI_SPEED.MHZ_40
                cb_sqi_clock.SelectedIndex = 0
            Case SPI.SQI_SPEED.MHZ_20
                cb_sqi_clock.SelectedIndex = 1
            Case SPI.SQI_SPEED.MHZ_10
                cb_sqi_clock.SelectedIndex = 2
            Case SPI.SQI_SPEED.MHZ_5
                cb_sqi_clock.SelectedIndex = 3
            Case SPI.SQI_SPEED.MHZ_2
                cb_sqi_clock.SelectedIndex = 4
            Case SPI.SQI_SPEED.MHZ_1
                cb_sqi_clock.SelectedIndex = 5
            Case Else
                cb_sqi_clock.SelectedIndex = 0
        End Select
    End Sub

    Private Sub SPI_SaveMaximumClockSettings()
        Select Case cb_spi_clock.SelectedIndex
            Case 0
                MySettings.SPI_CLOCK_MAX = SPI.SPI_SPEED.MHZ_32
            Case 1
                MySettings.SPI_CLOCK_MAX = SPI.SPI_SPEED.MHZ_24
            Case 2
                MySettings.SPI_CLOCK_MAX = SPI.SPI_SPEED.MHZ_16
            Case 3
                MySettings.SPI_CLOCK_MAX = SPI.SPI_SPEED.MHZ_12
            Case 4
                MySettings.SPI_CLOCK_MAX = SPI.SPI_SPEED.MHZ_8
            Case 5
                MySettings.SPI_CLOCK_MAX = SPI.SPI_SPEED.MHZ_4
            Case 6
                MySettings.SPI_CLOCK_MAX = SPI.SPI_SPEED.MHZ_2
            Case 7
                MySettings.SPI_CLOCK_MAX = SPI.SPI_SPEED.MHZ_1
            Case Else
                MySettings.SPI_CLOCK_MAX = SPI.SPI_SPEED.MHZ_32
        End Select
        Select Case cb_sqi_clock.SelectedIndex
            Case 0
                MySettings.SQI_CLOCK_MAX = SPI.SQI_SPEED.MHZ_40
            Case 1
                MySettings.SQI_CLOCK_MAX = SPI.SQI_SPEED.MHZ_20
            Case 2
                MySettings.SQI_CLOCK_MAX = SPI.SQI_SPEED.MHZ_10
            Case 3
                MySettings.SQI_CLOCK_MAX = SPI.SQI_SPEED.MHZ_5
            Case 4
                MySettings.SQI_CLOCK_MAX = SPI.SQI_SPEED.MHZ_2
            Case 5
                MySettings.SQI_CLOCK_MAX = SPI.SQI_SPEED.MHZ_1
            Case Else
                MySettings.SQI_CLOCK_MAX = SPI.SQI_SPEED.MHZ_40
        End Select
    End Sub

    Private Sub Language_setup()
        lbl_read_cmd.Text = RM.GetString("settings_read_cmd")   '"Read command"
        RadioUseSpiAuto.Text = RM.GetString("settings_auto")       '"Use automatic settings"
        RadioUseSpiSettings.Text = RM.GetString("settings_specify")    '"Use these settings"
        gb_nand_general.Text = RM.GetString("settings_box_general")    '"General"
        gb_block_manager.Text = RM.GetString("settings_box_block")  '"Bad block manager"
        gb_block_layout.Text = RM.GetString("settings_box_layout") '"Main/spare area layout"
        cb_preserve.Text = RM.GetString("settings_preserve_mem")   '"Preserve memory areas (i.e. copy spare area prior to main area write operation)"
        cb_mismatch.Text = RM.GetString("settings_mismatch") '"On write mismatch, write data to next block"
        cb_badblock_disabled.Text = RM.GetString("settings_blk_disabled") '"Disabled"
        cb_badblock_enabled.Text = RM.GetString("settings_blk_enabled") '"Enabled (check for bad block markers)"
        lbl_1st_byte.Text = RM.GetString("settings_blk_1stbyte") '"1st byte:"
        lbl_6th_byte.Text = RM.GetString("settings_blk_6thbyte") '"6th byte:"
        cb_badmarker_1st_page1.Text = RM.GetString("settings_blk_1stpage") '"First spare page"
        cb_badmarker_1st_page2.Text = RM.GetString("settings_blk_2ndpage") '"Second spare page"
        cb_badmarker_1st_lastpage.Text = RM.GetString("settings_blk_lastpage") '"Last spare page"
        cb_badmarker_6th_page1.Text = RM.GetString("settings_blk_1stpage") '"First spare page"
        cb_badmarker_6th_page2.Text = RM.GetString("settings_blk_2ndpage") '"Second spare page"
        cb_spinand_disable_ecc.Text = RM.GetString("settings_disable_ecc") '"Disable SPI-NAND ECC generator"
        cb_nand_image_readverify.Text = RM.GetString("settings_nand_readverify") 'Use Read-Verify on 'Create Image'
        rb_mainspare_default.Text = RM.GetString("settings_seperate") '"Separate"
        rb_mainspare_segmented.Text = RM.GetString("settings_segmented") '"Segmented"
        lbl_nandecc_changes.Text = RM.GetString("nandecc_changes") '"* Changes take effect on device detect event"
        gb_nandecc_title.Text = RM.GetString("nandecc_groupbox") '"Software ECC Feature"
        If MySettings.LanguageName = "Spanish" Then
            cb_badmarker_1st_page1.Location = New Point(64, 45)
            cb_badmarker_6th_page1.Location = New Point(64, 66)
            cb_badmarker_1st_lastpage.Location = New Point(362, 45)
        End If
    End Sub

    Private Sub FrmSettings_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        If cb_ce_select.SelectedIndex = 0 Then
            MySettings.MULTI_CE = 0 'Disable
        Else
            MySettings.MULTI_CE = 17 + cb_ce_select.SelectedIndex
        End If
        SPI_SaveMaximumClockSettings()
        MySettings.SPI_FASTREAD = rb_fastread_op.Checked
        CustomDevice_SaveSettings()
        MySettings.NAND_Preserve = cb_preserve.Checked
        MySettings.NAND_SkipBadBlock = cb_mismatch.Checked
        If cb_badblock_disabled.Checked Then
            MySettings.NAND_BadBlockManager = BadBlockMode.Disabled
        End If
        If cb_badblock_enabled.Checked Then
            MySettings.NAND_BadBlockManager = BadBlockMode.Enabled
        End If
        MySettings.NAND_BadBlockMarkers = 0
        If cb_badmarker_1st_page1.Checked Then
            MySettings.NAND_BadBlockMarkers = MySettings.NAND_BadBlockMarkers Or (BadBlockMarker._1stByte_FirstPage)
        End If
        If cb_badmarker_1st_page2.Checked Then
            MySettings.NAND_BadBlockMarkers = MySettings.NAND_BadBlockMarkers Or (BadBlockMarker._1stByte_SecondPage)
        End If
        If cb_badmarker_1st_lastpage.Checked Then
            MySettings.NAND_BadBlockMarkers = MySettings.NAND_BadBlockMarkers Or (BadBlockMarker._1stByte_LastPage)
        End If
        If cb_badmarker_6th_page1.Checked Then
            MySettings.NAND_BadBlockMarkers = MySettings.NAND_BadBlockMarkers Or (BadBlockMarker._6thByte_FirstPage)
        End If
        If cb_badmarker_6th_page2.Checked Then
            MySettings.NAND_BadBlockMarkers = MySettings.NAND_BadBlockMarkers Or (BadBlockMarker._6thByte_SecondPage)
        End If
        If rb_mainspare_default.Checked Then
            MySettings.NAND_Layout = NandMemLayout.Separated
        ElseIf rb_mainspare_segmented.Checked Then
            MySettings.NAND_Layout = NandMemLayout.Segmented
        End If
        'i2c tab
        Dim i2c_address As Byte = &HA0 'Initial address
        If cb_i2c_a2.Checked Then i2c_address = CByte(i2c_address Or (1 << 3))
        If cb_i2c_a1.Checked Then i2c_address = CByte(i2c_address Or (1 << 2))
        If cb_i2c_a0.Checked Then i2c_address = CByte(i2c_address Or (1 << 1))
        MySettings.I2C_ADDRESS = i2c_address
        'swi
        Dim swi_address As Byte = 0
        If cb_swi_a2.Checked Then swi_address = CByte(swi_address Or (1 << 3))
        If cb_swi_a1.Checked Then swi_address = CByte(swi_address Or (1 << 2))
        If cb_swi_a0.Checked Then swi_address = CByte(swi_address Or (1 << 1))
        MySettings.SWI_ADDRESS = swi_address

        If rb_speed_100khz.Checked Then
            MySettings.I2C_SPEED = I2C_SPEED_MODE._100kHz
        ElseIf rb_speed_400khz.Checked Then
            MySettings.I2C_SPEED = I2C_SPEED_MODE._400kHz
        ElseIf rb_speed_1mhz.Checked Then
            MySettings.I2C_SPEED = I2C_SPEED_MODE._1MHz
        End If
        MySettings.I2C_INDEX = (cb_i2c_device.SelectedIndex - 1)
        MySettings.SPI_EEPROM = cb_spi_eeprom.SelectedItem.ToString
        MySettings.SPI_NAND_DISABLE_ECC = cb_spinand_disable_ecc.Checked
        MySettings.NAND_Verify = cb_nand_image_readverify.Checked
        MySettings.NAND_Speed = CType(cbNAND_Speed.SelectedIndex, NandMemSpeed)
        MySettings.RETRY_WRITE_ATTEMPTS = cb_retry_write.SelectedIndex + 1
        MySettings.SREC_DATAMODE = CType(cbSrec.SelectedIndex, SREC.RECORD_DATAWIDTH)
        Microwire_Save()
        Select Case cb_jtag_tck_speed.SelectedIndex
            Case 0
                MySettings.JTAG_SPEED = JTAG.JTAG_SPEED._10MHZ
            Case 1
                MySettings.JTAG_SPEED = JTAG.JTAG_SPEED._20MHZ
            Case 2
                MySettings.JTAG_SPEED = JTAG.JTAG_SPEED._40MHZ
        End Select
        Dim s As String = cb_nor_read_access.SelectedItem.ToString
        MySettings.NOR_READ_ACCESS = CInt(s.Substring(0, s.IndexOf(" ")))
        s = cb_nor_we_pulse.SelectedItem.ToString
        MySettings.NOR_WE_PULSE = CInt(s.Substring(0, s.IndexOf(" ")))
    End Sub

    Private Sub CustomDevice_LoadSettings()
        If MySettings.SPI_AUTO Then
            RadioUseSpiAuto.Checked = True
            group_custom.Enabled = False
        Else
            RadioUseSpiSettings.Checked = True
            group_custom.Enabled = True
        End If
        op_read.SelectedIndex = (CUSTOM_SPI_DEV.OP_COMMANDS.READ - 1)
        op_prog.SelectedIndex = (CUSTOM_SPI_DEV.OP_COMMANDS.PROG - 1)
        op_sectorerase.SelectedIndex = (CUSTOM_SPI_DEV.OP_COMMANDS.SE - 1)
        op_we.SelectedIndex = (CUSTOM_SPI_DEV.OP_COMMANDS.WREN - 1)
        op_ce.SelectedIndex = (CUSTOM_SPI_DEV.OP_COMMANDS.BE - 1)
        op_rs.SelectedIndex = (CUSTOM_SPI_DEV.OP_COMMANDS.RDSR - 1)
        op_ws.SelectedIndex = (CUSTOM_SPI_DEV.OP_COMMANDS.WRSR - 1)
        op_ewsr.SelectedIndex = (CUSTOM_SPI_DEV.OP_COMMANDS.EWSR - 1)
        SPICUSTOM_SetDensity(CUInt(CUSTOM_SPI_DEV.FLASH_SIZE))
        Select Case CUSTOM_SPI_DEV.ADDRESSBITS
            Case 16
                cb_addr_size.SelectedIndex = 0
            Case 24
                cb_addr_size.SelectedIndex = 1
            Case 32
                cb_addr_size.SelectedIndex = 2
        End Select
        Select Case CUSTOM_SPI_DEV.ERASE_SIZE
            Case 0 'Erase not required
                cb_erase_size.SelectedIndex = 0
            Case FlashMemory.Kb016
                cb_erase_size.SelectedIndex = 1
            Case FlashMemory.Kb064
                cb_erase_size.SelectedIndex = 2
            Case FlashMemory.Kb128
                cb_erase_size.SelectedIndex = 3
            Case FlashMemory.Kb256
                cb_erase_size.SelectedIndex = 4
            Case FlashMemory.Kb512
                cb_erase_size.SelectedIndex = 5
            Case FlashMemory.Mb001
                cb_erase_size.SelectedIndex = 6
            Case FlashMemory.Mb002
                cb_erase_size.SelectedIndex = 7
        End Select
        Select Case CUSTOM_SPI_DEV.PAGE_SIZE
            Case 8
                cb_page_size.SelectedIndex = 0
            Case 16
                cb_page_size.SelectedIndex = 1
            Case 32
                cb_page_size.SelectedIndex = 2
            Case 64
                cb_page_size.SelectedIndex = 3
            Case 128
                cb_page_size.SelectedIndex = 4
            Case 256
                cb_page_size.SelectedIndex = 5
            Case 512
                cb_page_size.SelectedIndex = 6
            Case 1024
                cb_page_size.SelectedIndex = 7
        End Select
        Select Case CUSTOM_SPI_DEV.ProgramMode
            Case FlashMemory.SPI_ProgramMode.PageMode
                cb_prog_mode.SelectedIndex = 0
                cb_spare.Enabled = False
            Case FlashMemory.SPI_ProgramMode.AAI_Byte
                cb_prog_mode.SelectedIndex = 1
                cb_spare.Enabled = False
            Case FlashMemory.SPI_ProgramMode.AAI_Word
                cb_prog_mode.SelectedIndex = 2
                cb_spare.Enabled = False
            Case FlashMemory.SPI_ProgramMode.Atmel45Series
                cb_prog_mode.SelectedIndex = 3
                cb_spare.Enabled = True
        End Select
        cbENWS.Checked = CUSTOM_SPI_DEV.SEND_EWSR
        cbEN4B.Checked = CUSTOM_SPI_DEV.SEND_EN4B
        Dim spare_bytes As Integer = 0
        If CUSTOM_SPI_DEV.PAGE_SIZE_EXTENDED > 0 Then
            spare_bytes = CUSTOM_SPI_DEV.PAGE_SIZE_EXTENDED - CUSTOM_SPI_DEV.PAGE_SIZE
        End If
        Select Case spare_bytes
            Case 0
                cb_spare.SelectedIndex = 0
            Case 8
                cb_spare.SelectedIndex = 1
            Case 16
                cb_spare.SelectedIndex = 2
            Case 32
                cb_spare.SelectedIndex = 3
            Case 64
                cb_spare.SelectedIndex = 4
            Case 128
                cb_spare.SelectedIndex = 5
        End Select
    End Sub

    Private Sub CustomDevice_SaveSettings()
        MySettings.SPI_AUTO = RadioUseSpiAuto.Checked
        CUSTOM_SPI_DEV.OP_COMMANDS.READ = CByte(op_read.SelectedIndex + 1)
        CUSTOM_SPI_DEV.OP_COMMANDS.PROG = CByte(op_prog.SelectedIndex + 1)
        CUSTOM_SPI_DEV.OP_COMMANDS.SE = CByte(op_sectorerase.SelectedIndex + 1)
        CUSTOM_SPI_DEV.OP_COMMANDS.WREN = CByte(op_we.SelectedIndex + 1)
        CUSTOM_SPI_DEV.OP_COMMANDS.BE = CByte(op_ce.SelectedIndex + 1)
        CUSTOM_SPI_DEV.OP_COMMANDS.RDSR = CByte(op_rs.SelectedIndex + 1)
        CUSTOM_SPI_DEV.OP_COMMANDS.WRSR = CByte(op_ws.SelectedIndex + 1)
        CUSTOM_SPI_DEV.OP_COMMANDS.EWSR = CByte(op_ewsr.SelectedIndex + 1)
        CUSTOM_SPI_DEV.FLASH_SIZE = SPICUSTOM_GetDensity()
        Select Case cb_addr_size.SelectedIndex
            Case 0
                CUSTOM_SPI_DEV.ADDRESSBITS = 16
            Case 1
                CUSTOM_SPI_DEV.ADDRESSBITS = 24
            Case 2
                CUSTOM_SPI_DEV.ADDRESSBITS = 32
        End Select
        Select Case cb_erase_size.SelectedIndex
            Case 0
                CUSTOM_SPI_DEV.ERASE_SIZE = 0
                CUSTOM_SPI_DEV.ERASE_REQUIRED = False 'Not required
            Case 1
                CUSTOM_SPI_DEV.ERASE_SIZE = FlashMemory.Kb032
                CUSTOM_SPI_DEV.ERASE_REQUIRED = True
            Case 2
                CUSTOM_SPI_DEV.ERASE_SIZE = FlashMemory.Kb064
                CUSTOM_SPI_DEV.ERASE_REQUIRED = True
            Case 3
                CUSTOM_SPI_DEV.ERASE_SIZE = FlashMemory.Kb128
                CUSTOM_SPI_DEV.ERASE_REQUIRED = True
            Case 4
                CUSTOM_SPI_DEV.ERASE_SIZE = FlashMemory.Kb256
                CUSTOM_SPI_DEV.ERASE_REQUIRED = True
            Case 5
                CUSTOM_SPI_DEV.ERASE_SIZE = FlashMemory.Kb512
                CUSTOM_SPI_DEV.ERASE_REQUIRED = True
            Case 6
                CUSTOM_SPI_DEV.ERASE_SIZE = FlashMemory.Mb001
                CUSTOM_SPI_DEV.ERASE_REQUIRED = True
            Case 7
                CUSTOM_SPI_DEV.ERASE_SIZE = FlashMemory.Mb002
                CUSTOM_SPI_DEV.ERASE_REQUIRED = True
        End Select
        Select Case cb_page_size.SelectedIndex
            Case 0
                CUSTOM_SPI_DEV.PAGE_SIZE = 8
            Case 1
                CUSTOM_SPI_DEV.PAGE_SIZE = 16
            Case 2
                CUSTOM_SPI_DEV.PAGE_SIZE = 32
            Case 3
                CUSTOM_SPI_DEV.PAGE_SIZE = 64
            Case 4
                CUSTOM_SPI_DEV.PAGE_SIZE = 128
            Case 5
                CUSTOM_SPI_DEV.PAGE_SIZE = 256
            Case 6
                CUSTOM_SPI_DEV.PAGE_SIZE = 512
            Case 7
                CUSTOM_SPI_DEV.PAGE_SIZE = 1024
        End Select
        Select Case cb_prog_mode.SelectedIndex
            Case 0
                CUSTOM_SPI_DEV.ProgramMode = FlashMemory.SPI_ProgramMode.PageMode
            Case 1
                CUSTOM_SPI_DEV.ProgramMode = FlashMemory.SPI_ProgramMode.AAI_Byte
            Case 2
                CUSTOM_SPI_DEV.ProgramMode = FlashMemory.SPI_ProgramMode.AAI_Word
            Case 3
                CUSTOM_SPI_DEV.ProgramMode = FlashMemory.SPI_ProgramMode.Atmel45Series
        End Select
        CUSTOM_SPI_DEV.SEND_EWSR = cbENWS.Checked
        CUSTOM_SPI_DEV.SEND_EN4B = cbEN4B.Checked
        Select Case cb_spare.SelectedIndex
            Case 0
                CUSTOM_SPI_DEV.PAGE_SIZE_EXTENDED = 0
            Case 1
                CUSTOM_SPI_DEV.PAGE_SIZE_EXTENDED = CUSTOM_SPI_DEV.PAGE_SIZE + 8US
            Case 2
                CUSTOM_SPI_DEV.PAGE_SIZE_EXTENDED = CUSTOM_SPI_DEV.PAGE_SIZE + 16US
            Case 3
                CUSTOM_SPI_DEV.PAGE_SIZE_EXTENDED = CUSTOM_SPI_DEV.PAGE_SIZE + 32US
            Case 4
                CUSTOM_SPI_DEV.PAGE_SIZE_EXTENDED = CUSTOM_SPI_DEV.PAGE_SIZE + 64US
            Case 5
                CUSTOM_SPI_DEV.PAGE_SIZE_EXTENDED = CUSTOM_SPI_DEV.PAGE_SIZE + 128US
        End Select
    End Sub

    Private Sub SPICUSTOM_SetDensity(size As UInt32)
        Select Case size
            Case FlashMemory.Mb001
                cb_chip_size.SelectedIndex = 0
            Case FlashMemory.Mb002
                cb_chip_size.SelectedIndex = 1
            Case FlashMemory.Mb004
                cb_chip_size.SelectedIndex = 2
            Case FlashMemory.Mb008
                cb_chip_size.SelectedIndex = 3
            Case FlashMemory.Mb016
                cb_chip_size.SelectedIndex = 4
            Case FlashMemory.Mb032
                cb_chip_size.SelectedIndex = 5
            Case FlashMemory.Mb064
                cb_chip_size.SelectedIndex = 6
            Case FlashMemory.Mb128
                cb_chip_size.SelectedIndex = 7
            Case FlashMemory.Mb256
                cb_chip_size.SelectedIndex = 8
            Case FlashMemory.Mb256
                cb_chip_size.SelectedIndex = 9
            Case FlashMemory.Gb001
                cb_chip_size.SelectedIndex = 10
            Case FlashMemory.Gb002
                cb_chip_size.SelectedIndex = 11
        End Select
    End Sub

    Private Function SPICUSTOM_GetDensity() As UInt32
        Select Case cb_chip_size.SelectedIndex
            Case 0 '1 Mbit
                Return FlashMemory.Mb001
            Case 1 '2 Mbit
                Return FlashMemory.Mb002
            Case 2 '4 Mbit
                Return FlashMemory.Mb004
            Case 3 '8 Mbit
                Return FlashMemory.Mb008
            Case 4 '16 Mbit
                Return FlashMemory.Mb016
            Case 5 '32 Mbit
                Return FlashMemory.Mb032
            Case 6 '64 Mbit
                Return FlashMemory.Mb064
            Case 7 '128 Mbit
                Return FlashMemory.Mb128
            Case 8 '256 Mbit
                Return FlashMemory.Mb256
            Case 9 '512 Mbit
                Return FlashMemory.Mb512
            Case 10 '1 Gbit
                Return FlashMemory.Gb001
            Case 11 '2 Gbit
                Return FlashMemory.Gb002
        End Select
        Return FlashMemory.Mb001
    End Function

    Private Sub MyTabs_DrawItem(sender As Object, e As System.Windows.Forms.DrawItemEventArgs) Handles MyTabs.DrawItem
        Dim SelectedTab As TabPage = MyTabs.TabPages(e.Index) 'Select the active tab
        Dim HeaderRect As Rectangle = MyTabs.GetTabRect(e.Index) 'Get the area of the header of this TabPage
        Dim TextBrush As New SolidBrush(Color.Black) 'Create a Brush to paint the Text
        'Set the Alignment of the Text
        Dim sf As New StringFormat(StringFormatFlags.NoWrap)
        sf.Alignment = StringAlignment.Center
        sf.LineAlignment = StringAlignment.Center
        'Paint the Text using the appropriate Bold setting 
        If Convert.ToBoolean(e.State And DrawItemState.Selected) Then
            Dim BoldFont As New Font(MyTabs.Font.Name, MyTabs.Font.Size, FontStyle.Bold)
            e.Graphics.DrawString(SelectedTab.Text.Trim, BoldFont, TextBrush, HeaderRect, sf)
            Dim LineY As Integer = HeaderRect.Y + HeaderRect.Height 'This draws the line between the tab and the tab form
            e.Graphics.DrawLine(New Pen(Control.DefaultBackColor), HeaderRect.X, LineY, HeaderRect.X + HeaderRect.Width, LineY)
        Else
            e.Graphics.DrawString(SelectedTab.Text.Trim, e.Font, TextBrush, HeaderRect, sf)
        End If
        TextBrush.Dispose() 'Dispose of the Brush
    End Sub

    Private Sub RadioUseSpiAuto_CheckedChanged(sender As Object, e As EventArgs) Handles RadioUseSpiAuto.CheckedChanged
        If RadioUseSpiAuto.Checked Then
            group_custom.Enabled = False
        Else
            group_custom.Enabled = True
        End If
    End Sub

    Private Sub RadioUseSpiSettings_CheckedChanged(sender As Object, e As EventArgs) Handles RadioUseSpiSettings.CheckedChanged
        If RadioUseSpiAuto.Checked Then
            group_custom.Enabled = False
        Else
            group_custom.Enabled = True
        End If
    End Sub

    Private Sub cb_prog_mode_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cb_prog_mode.SelectedIndexChanged
        If cb_prog_mode.SelectedIndex = 3 Then
            cb_spare.Enabled = True
        Else
            cb_spare.Enabled = False
        End If
    End Sub

    Private Sub Setup_I2C_SWI_tab()
        cb_i2c_a2.Checked = False
        cb_i2c_a1.Checked = False
        cb_i2c_a0.Checked = False
        If ((MySettings.I2C_ADDRESS And (1 << 3)) > 0) Then
            cb_i2c_a2.Checked = True
        End If
        If ((MySettings.I2C_ADDRESS And (1 << 2)) > 0) Then
            cb_i2c_a1.Checked = True
        End If
        If ((MySettings.I2C_ADDRESS And (1 << 1)) > 0) Then
            cb_i2c_a0.Checked = True
        End If
        cb_swi_a2.Checked = False
        cb_swi_a1.Checked = False
        cb_swi_a0.Checked = False
        If ((MySettings.SWI_ADDRESS And (1 << 3)) > 0) Then
            cb_swi_a2.Checked = True
        End If
        If ((MySettings.SWI_ADDRESS And (1 << 2)) > 0) Then
            cb_swi_a1.Checked = True
        End If
        If ((MySettings.SWI_ADDRESS And (1 << 1)) > 0) Then
            cb_swi_a0.Checked = True
        End If
        Dim i2c_if As New I2C_Programmer(Nothing)
        cb_i2c_device.Items.Add("(Not selected)")
        For Each i2c_eeprom In i2c_if.I2C_EEPROM_LIST
            Dim dev_size_str As String
            If i2c_eeprom.FLASH_SIZE >= 1024 Then
                dev_size_str = (i2c_eeprom.FLASH_SIZE / 1024).ToString & "K"
            Else
                dev_size_str = i2c_eeprom.FLASH_SIZE.ToString
            End If
            Dim dev_name As String = (dev_size_str & " bytes ").PadRight(12, " "c)
            cb_i2c_device.Items.Add(dev_name & " (" & i2c_eeprom.Name & ")")
        Next
        cb_i2c_device.SelectedIndex = (MySettings.I2C_INDEX + 1)
        Select Case MySettings.I2C_SPEED
            Case I2C_SPEED_MODE._100kHz
                rb_speed_100khz.Checked = True
            Case I2C_SPEED_MODE._400kHz
                rb_speed_400khz.Checked = True
            Case I2C_SPEED_MODE._1MHz
                rb_speed_1mhz.Checked = True
        End Select
    End Sub

    Private Sub cbi2cDensity_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cb_i2c_device.SelectedIndexChanged

    End Sub

    Private Sub SetupSpiEeprom()
        cb_spi_eeprom.Items.Add("(Not selected)") 'Index 0
        Dim SPI_EEPROM_LIST() As FlashMemory.SPI_NOR = GetDevices_SPI_EEPROM()
        Dim selected_index As Integer = 0
        Dim counter As Integer = 0
        For Each item In SPI_EEPROM_LIST
            counter += 1
            If item.NAME.Equals(MySettings.SPI_EEPROM) Then
                selected_index = counter
            End If
            cb_spi_eeprom.Items.Add(item.NAME)
        Next
        cb_spi_eeprom.SelectedIndex = selected_index
    End Sub

    Private Sub rb_mainspare_default_CheckedChanged(sender As Object, e As EventArgs) Handles rb_mainspare_default.CheckedChanged
        If rb_mainspare_default.Checked Then
            nand_box.Image = My.Resources.nand_page_seperate
        End If
    End Sub

    Private Sub rb_mainspare_segmented_CheckedChanged(sender As Object, e As EventArgs) Handles rb_mainspare_segmented.CheckedChanged
        If rb_mainspare_segmented.Checked Then
            nand_box.Image = My.Resources.nand_page_segmented
        End If
    End Sub

    Private Sub cb_badblock_disabled_CheckedChanged(sender As Object, e As EventArgs) Handles cb_badblock_disabled.CheckedChanged
        If cb_badblock_disabled.Checked Then
            cb_badmarker_1st_page1.Enabled = False
            cb_badmarker_1st_page2.Enabled = False
            cb_badmarker_1st_lastpage.Enabled = False
            cb_badmarker_6th_page1.Enabled = False
            cb_badmarker_6th_page2.Enabled = False
        End If
    End Sub

    Private Sub cb_badblock_enabled_CheckedChanged(sender As Object, e As EventArgs) Handles cb_badblock_enabled.CheckedChanged
        If cb_badblock_enabled.Checked Then
            cb_badmarker_1st_page1.Enabled = True
            cb_badmarker_1st_page2.Enabled = True
            cb_badmarker_1st_lastpage.Enabled = True
            cb_badmarker_6th_page1.Enabled = True
            cb_badmarker_6th_page2.Enabled = True
        End If
    End Sub

    Private Sub Microwire_Init()
        cb_s93_org.SelectedIndex = MySettings.S93_DEVICE_ORG
        Dim s93_devices() As FlashMemory.Device = FlashDatabase.GetFlashDevices(FlashMemory.MemoryType.SERIAL_MICROWIRE)
        Me.cb_s93_devices.Items.Add("(Not Selected)")
        Me.cb_s93_devices.Items.AddRange(s93_devices)
        If Not MySettings.S93_DEVICE.Equals("") Then
            cb_s93_org.Enabled = True
            Dim i As Integer = 0
            For Each item In Me.cb_s93_devices.Items
                If item.ToString.Equals(MySettings.S93_DEVICE) Then
                    cb_s93_devices.SelectedIndex = i
                    Exit Sub
                End If
                i += 1
            Next
        End If
        cb_s93_org.Enabled = False
        cb_s93_devices.SelectedIndex = 0
    End Sub

    Private Sub Microwire_Save()
        If (cb_s93_devices.SelectedIndex > 0) Then
            Dim s93 As FlashMemory.MICROWIRE = CType(cb_s93_devices.SelectedItem, FlashMemory.MICROWIRE)
            MySettings.S93_DEVICE = s93.NAME
        Else
            MySettings.S93_DEVICE = ""
        End If
        MySettings.S93_DEVICE_ORG = cb_s93_org.SelectedIndex
    End Sub

    Private Sub Cb_s93_devices_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cb_s93_devices.SelectedIndexChanged
        If (cb_s93_devices.SelectedIndex > 0) Then
            Dim s93 As FlashMemory.MICROWIRE = CType(cb_s93_devices.SelectedItem, FlashMemory.MICROWIRE)
            lbl_s93_size.Text = "Device Size: " & s93.FLASH_SIZE.ToString & " bytes"
            lbl_s93_size.Visible = True
            If s93.X8_ADDRSIZE = 0 And (Not s93.X16_ADDRSIZE = 0) Then 'X16 only
                cb_s93_org.Enabled = False
                cb_s93_org.SelectedIndex = 1
            ElseIf (Not s93.X8_ADDRSIZE = 0) And s93.X16_ADDRSIZE = 0 Then 'X8 only
                cb_s93_org.Enabled = False
                cb_s93_org.SelectedIndex = 0
            Else 'X8 or X16
                cb_s93_org.Enabled = True
                If MySettings.S93_DEVICE_ORG = 0 Then
                    cb_s93_org.SelectedIndex = 0
                Else
                    cb_s93_org.SelectedIndex = 1
                End If
            End If
        Else
            cb_s93_org.Enabled = False
            lbl_s93_size.Visible = False
        End If
    End Sub

#Region "ECC Feature"

    Private Sub ECC_Feature_Load()
        lv_nand_type.Items.Clear()
        If NAND_ECC_CFG IsNot Nothing AndAlso NAND_ECC_CFG.Length > 0 Then
            For i = 1 To NAND_ECC_CFG.Length
                Dim lv As ListViewItem = ECC_CreateLV(NAND_ECC_CFG(i - 1))
                lv.Text = i.ToString
                lv_nand_type.Items.Add(lv)
            Next
        End If
        cb_ecc_feature.Checked = MySettings.ECC_FEATURE_ENABLED
        ECC_Features_EnableEvent()
    End Sub

    Private Function ECC_CreateLV(ecc_entry As ECC_Configuration_Entry) As ListViewItem
        Dim AlgorithmStr As String = ""
        Select Case ecc_entry.Algorithm
            Case ecc_algorithum.hamming
                AlgorithmStr = "Hamming"
            Case ecc_algorithum.reedsolomon
                AlgorithmStr = "Reed-Solomon"
            Case ecc_algorithum.bhc
                AlgorithmStr = "Binary BHC"
        End Select
        Dim lv As New ListViewItem
        lv.SubItems.Add(ecc_entry.PageSize.ToString)
        lv.SubItems.Add(ecc_entry.SpareSize.ToString)
        lv.SubItems.Add(AlgorithmStr)
        lv.SubItems.Add(CStr(ecc_entry.BitError))
        If ecc_entry.Algorithm = ecc_algorithum.reedsolomon Then
            lv.SubItems.Add(ecc_entry.SymSize.ToString)
        Else
            lv.SubItems.Add("")
        End If
        If ecc_entry.ReverseData Then
            lv.SubItems.Add("True")
        Else
            lv.SubItems.Add("False")
        End If
        lv.Tag = ecc_entry
        Return lv
    End Function

    Private Sub lv_nand_type_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lv_nand_type.SelectedIndexChanged
        If lv_nand_type.SelectedItems IsNot Nothing AndAlso lv_nand_type.SelectedItems.Count > 0 Then
            Dim lv As ListViewItem = lv_nand_type.SelectedItems(0)
            Dim nand_ecc_cfg As ECC_Configuration_Entry = CType(lv.Tag, ECC_Configuration_Entry)
            lv_nand_region.Items.Clear()
            If nand_ecc_cfg.EccRegion Is Nothing Then Exit Sub
            Dim ecc_size As Integer = GetEccDataSize(nand_ecc_cfg)
            lbl_ECC_size.Text = "ECC data per 512 byte sector: " & ecc_size.ToString & " bytes"
            For i = 0 To nand_ecc_cfg.EccRegion.Length - 1
                Dim n As New ListViewItem
                n.Text = (i + 1).ToString
                Dim start_addr As UInt16 = nand_ecc_cfg.EccRegion(i)
                n.SubItems.Add("0x" & Hex(start_addr))
                n.SubItems.Add("0x" & Hex(start_addr + ecc_size - 1))
                lv_nand_region.Items.Add(n)
            Next
            cmdEccRemove.Enabled = True
        Else
            lv_nand_region.Items.Clear()
            cmdEccRemove.Enabled = False
        End If
    End Sub

    Private Sub cb_ecc_feature_CheckedChanged(sender As Object, e As EventArgs) Handles cb_ecc_feature.CheckedChanged
        ECC_Features_EnableEvent()
    End Sub

    Private Sub ECC_Features_EnableEvent()
        If cb_ecc_feature.Checked Then
            MySettings.ECC_FEATURE_ENABLED = True
            lv_nand_type.Enabled = True
            lv_nand_region.Enabled = True
            cmdEccAdd.Enabled = True
            cmdEccRemove.Enabled = True
        Else
            MySettings.ECC_FEATURE_ENABLED = False
            lv_nand_type.Enabled = False
            lv_nand_region.Enabled = False
            lv_nand_type.SelectedItems.Clear()
            lv_nand_region.SelectedItems.Clear()
            cmdEccAdd.Enabled = False
            cmdEccRemove.Enabled = False
        End If
    End Sub

    Private Sub cmdEccAdd_Click(sender As Object, e As EventArgs) Handles cmdEccAdd.Click
        Dim f As New FrmECC
        If (f.ShowDialog = DialogResult.OK) Then
            Dim WasAdded As Boolean = False
            If NAND_ECC_CFG IsNot Nothing AndAlso NAND_ECC_CFG.Length > 0 Then
                For i = 0 To NAND_ECC_CFG.Length - 1
                    If NAND_ECC_CFG(i).PageSize = 0 AndAlso NAND_ECC_CFG(i).SpareSize = 0 Then
                        NAND_ECC_CFG(i) = f.MyConfiguration
                        WasAdded = True
                        Exit For
                    End If
                Next
            End If
            If Not WasAdded Then
                If NAND_ECC_CFG Is Nothing Then
                    ReDim NAND_ECC_CFG(0)
                    NAND_ECC_CFG(0) = f.MyConfiguration
                Else
                    ReDim Preserve NAND_ECC_CFG(NAND_ECC_CFG.Length)
                    NAND_ECC_CFG(NAND_ECC_CFG.Length - 1) = f.MyConfiguration
                End If
            End If
            ECC_Feature_Load()
        End If
    End Sub

    Private Sub cmdEccRemove_Click(sender As Object, e As EventArgs) Handles cmdEccRemove.Click
        If lv_nand_type.SelectedItems IsNot Nothing AndAlso lv_nand_type.SelectedItems.Count > 0 Then
            Dim entry_index As Integer = lv_nand_type.SelectedIndices(0)
            NAND_ECC_CFG.RemoveAt(entry_index)
            lv_nand_region.Items.Clear()
            ECC_Feature_Load()
        End If
    End Sub

    Private Sub lv_nand_type_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles lv_nand_type.MouseDoubleClick
        If lv_nand_type.SelectedItems IsNot Nothing AndAlso lv_nand_type.SelectedItems.Count > 0 Then
            Dim lv As ListViewItem = lv_nand_type.SelectedItems(0)
            Dim entry_index As Integer = lv_nand_type.SelectedIndices(0)
            Dim cfg As ECC_Configuration_Entry = CType(lv.Tag, ECC_Configuration_Entry)
            Dim f As New FrmECC(cfg)
            If (f.ShowDialog = DialogResult.OK) Then
                NAND_ECC_CFG(entry_index) = f.MyConfiguration
                ECC_Feature_Load()
            End If
        End If
    End Sub

    Private Sub lv_nand_region_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles lv_nand_region.MouseDoubleClick
        If lv_nand_type.SelectedItems IsNot Nothing AndAlso (lv_nand_type.SelectedItems.Count > 0) Then
            Dim lv As ListViewItem = lv_nand_type.SelectedItems(0)
            Dim entry_index As Integer = lv_nand_type.SelectedIndices(0)
            Dim sector_index As Integer = lv_nand_region.SelectedIndices(0)
            Dim InputSelectionForm As New Form
            InputSelectionForm.FormBorderStyle = FormBorderStyle.FixedToolWindow
            InputSelectionForm.ShowInTaskbar = False
            InputSelectionForm.ShowIcon = False
            InputSelectionForm.ControlBox = False
            InputSelectionForm.StartPosition = FormStartPosition.CenterParent
            Dim BtnOK As New Button With {.Text = RM.GetString("mc_button_ok"), .Width = 40, .Height = 20, .Left = 4, .Top = 40}
            Dim inputbox As New TextBox
            inputbox.Width = 40
            inputbox.TextAlign = HorizontalAlignment.Center
            inputbox.Text = "0x" & Hex(NAND_ECC_CFG(entry_index).EccRegion(sector_index))
            inputbox.Location = New Point(5, 16)
            Dim Lbl1 As New Label With {.Text = "Offset", .Location = New Point(6, 2)}
            InputSelectionForm.Controls.Add(BtnOK)
            InputSelectionForm.Controls.Add(inputbox)
            InputSelectionForm.Controls.Add(Lbl1)
            AddHandler BtnOK.Click, AddressOf Dyn_OkClick
            AddHandler inputbox.KeyPress, AddressOf Dyn_KeyPress
            inputbox.Select()
            InputSelectionForm.Width = 52
            InputSelectionForm.Height = 67
            InputSelectionForm.ShowDialog()
            Dim new_value As String = inputbox.Text
            Dim start_addr As UInt16
            If new_value.Equals("") Then
                MsgBox("No input was entered.", MsgBoxStyle.Critical, "Error with input")
                Exit Sub
            ElseIf IsNumeric(new_value) Then
                start_addr = CUShort(new_value)
            ElseIf Utilities.IsDataType.Hex(new_value) Then
                start_addr = CUShort(Utilities.HexToInt(new_value))
            Else
                MsgBox("Value must be numeric or hexadecimal.", MsgBoxStyle.Critical, "Error with input")
                Exit Sub
            End If
            Dim ecc_size As Integer = GetEccDataSize(NAND_ECC_CFG(entry_index))
            If ((start_addr + ecc_size - 1) < NAND_ECC_CFG(entry_index).SpareSize) Then
                NAND_ECC_CFG(entry_index).EccRegion(sector_index) = start_addr
                lv = lv_nand_region.SelectedItems(0)
                lv.SubItems(1).Text = "0x" & Hex(start_addr)
                lv.SubItems(2).Text = "0x" & Hex(start_addr + ecc_size - 1)
            Else
                MsgBox("Offset must be less than the size of the spare area with ECC data.", MsgBoxStyle.Critical, "Error with input")
            End If
        End If
    End Sub

    Private Sub Dyn_OkClick(sender As Object, e As EventArgs)
        Dim SendFrm As Form = DirectCast(sender, Control).FindForm
        SendFrm.DialogResult = DialogResult.OK
    End Sub

    Private Sub Dyn_KeyPress(sender As Object, e As KeyPressEventArgs)
        If e.KeyChar = vbCr Then
            Dyn_OkClick(sender, Nothing)
            e.Handled = True
        End If
    End Sub


#End Region

End Class