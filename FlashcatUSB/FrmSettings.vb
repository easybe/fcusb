Imports System.ComponentModel
Imports FlashcatUSB.ECC_LIB

Public Class FrmSettings
    Private otp_devices As New List(Of FlashMemory.OTP_EPROM)

    Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
    End Sub

    Private Sub FrmSettings_Load(sender As Object, e As EventArgs) Handles Me.Load
        Language_setup()
        Me.MyTabs.DrawMode = TabDrawMode.OwnerDrawFixed
        cb_sym_width.Enabled = False
        cb_spi_pro_clock.Items.Clear()
        cb_spi_pro_clock.Items.Add("5MHz")
        cb_spi_pro_clock.Items.Add("8MHz")
        cb_spi_pro_clock.Items.Add("10MHz")
        cb_spi_pro_clock.Items.Add("12MHz")
        cb_spi_pro_clock.Items.Add("15MHz")
        cb_spi_pro_clock.Items.Add("20MHz")
        cb_spi_pro_clock.Items.Add("24MHz")
        cb_spi_pro_clock.Items.Add("30MHz")
        Select Case MySettings.SPI_CLOCK_PRO
            Case SPI.SPI_CLOCK_SPEED.MHZ_5
                cb_spi_pro_clock.SelectedIndex = 0
            Case SPI.SPI_CLOCK_SPEED.MHZ_8
                cb_spi_pro_clock.SelectedIndex = 1
            Case SPI.SPI_CLOCK_SPEED.MHZ_10
                cb_spi_pro_clock.SelectedIndex = 2
            Case SPI.SPI_CLOCK_SPEED.MHZ_12
                cb_spi_pro_clock.SelectedIndex = 3
            Case SPI.SPI_CLOCK_SPEED.MHZ_15
                cb_spi_pro_clock.SelectedIndex = 4
            Case SPI.SPI_CLOCK_SPEED.MHZ_20
                cb_spi_pro_clock.SelectedIndex = 5
            Case SPI.SPI_CLOCK_SPEED.MHZ_24
                cb_spi_pro_clock.SelectedIndex = 6
            Case SPI.SPI_CLOCK_SPEED.MHZ_30
                cb_spi_pro_clock.SelectedIndex = 7
            Case Else
                cb_spi_pro_clock.SelectedIndex = 2
        End Select
        cb_spi_clock.Items.Clear()
        cb_spi_clock.Items.Add("1MHz")
        cb_spi_clock.Items.Add("2MHz")
        cb_spi_clock.Items.Add("4MHz")
        cb_spi_clock.Items.Add("8MHz")
        Select Case MySettings.SPI_CLOCK_CLASSIC
            Case SPI.SPI_CLOCK_SPEED.MHZ_1
                cb_spi_clock.SelectedIndex = 0
            Case SPI.SPI_CLOCK_SPEED.MHZ_2
                cb_spi_clock.SelectedIndex = 1
            Case SPI.SPI_CLOCK_SPEED.MHZ_4
                cb_spi_clock.SelectedIndex = 2
            Case SPI.SPI_CLOCK_SPEED.MHZ_8
                cb_spi_clock.SelectedIndex = 3
            Case Else
                cb_spi_clock.SelectedIndex = 3
        End Select
        If MySettings.SPI_FASTREAD Then
            rb_fastread_op.Checked = True
        Else
            rb_read_op.Checked = True
        End If
        Dim all_bytes(254) As String
        For i = 1 To 255
            all_bytes(i - 1) = "0x" & Hex(i).PadLeft(2, "0")
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
        cb_mismatch.Checked = MySettings.NAND_MismatchSkip
        Dim markers As Integer = MySettings.NAND_BadBlockMarkers
        If (markers And FlashcatSettings.BadBlockMarker._1stByte_FirstPage) > 0 Then
            cb_badmarker_1st_page1.Checked = True
        End If
        If (markers And FlashcatSettings.BadBlockMarker._1stByte_SecondPage) > 0 Then
            cb_badmarker_1st_page2.Checked = True
        End If
        If (markers And FlashcatSettings.BadBlockMarker._1stByte_LastPage) > 0 Then
            cb_badmarker_1st_lastpage.Checked = True
        End If
        If (markers And FlashcatSettings.BadBlockMarker._6thByte_FirstPage) > 0 Then
            cb_badmarker_6th_page1.Checked = True
        End If
        If (markers And FlashcatSettings.BadBlockMarker._6thByte_SecondPage) > 0 Then
            cb_badmarker_6th_page2.Checked = True
        End If
        Select Case MySettings.NAND_BadBlockManager
            Case FlashcatSettings.BadBlockMode.Disabled
                cb_badblock_disabled.Checked = True
            Case FlashcatSettings.BadBlockMode.Enabled
                cb_badblock_enabled.Checked = True
        End Select
        Select Case MySettings.NAND_Layout
            Case FlashcatSettings.NandMemLayout.Seperated
                rb_mainspare_default.Checked = True
            Case FlashcatSettings.NandMemLayout.Combined
                rb_mainspare_all.Checked = True
            Case FlashcatSettings.NandMemLayout.Segmented
                rb_mainspare_segmented.Checked = True
        End Select
        SetupSpiEeprom()
        Setup_i2C()
        If USBCLIENT.HW_MODE = FCUSB_BOARD.NotConnected Then
        ElseIf USBCLIENT.HW_MODE = FCUSB_BOARD.Professional Then
            cb_spi_clock.Enabled = False
        Else
            cb_spi_pro_clock.Enabled = False
            rb_speed_100khz.Enabled = False
            rb_speed_1mhz.Enabled = False
            rb_speed_400khz.Checked = True
            rb_read_op.Enabled = False
            rb_fastread_op.Enabled = False
            rb_read_op.Checked = True
        End If
        cb_spi_quad.Checked = MySettings.SPI_QUAD
        cb_spinand_disable_ecc.Checked = MySettings.SPI_NAND_DISABLE_ECC
        cb_nand_image_readverify.Checked = MySettings.NAND_Verify
        cb_ECC_ReadEnable.Checked = MySettings.ECC_READ_ENABLED
        cb_ECC_WriteEnable.Checked = MySettings.ECC_WRITE_ENABLED
        cb_ecc_seperate.Checked = MySettings.ECC_Seperate
        cb_rs_reverse_data.Checked = MySettings.ECC_Reverse
        Select Case MySettings.ECC_Algorithum
            Case 0
                rb_ECC_Hamming.Checked = True
            Case 1
                rb_ECC_ReedSolomon.Checked = True
            Case 2
                rb_ECC_BHC.Checked = True
        End Select
        SetBitErrorLevel(MySettings.ECC_BitError)
        txt_ecc_location.Text = "0x" & Hex(MySettings.ECC_Location).PadLeft(2, "0")
        Select Case MySettings.ECC_SymWidth
            Case 9
                cb_sym_width.SelectedIndex = 0
            Case 10
                cb_sym_width.SelectedIndex = 1
        End Select
        ECC_CheckIfEnabled()
        Dim otp_index As Integer = 0
        Dim otp_counter As Integer = 1
        cb_otp_device_list.Items.Add("(Not Selected)")
        Dim d() As FlashMemory.Device = FlashDatabase.GetFlashDevices(FlashMemory.MemoryType.PARALLEL_NOR)
        For Each mem In d
            If mem.GetType Is GetType(FlashMemory.OTP_EPROM) Then
                Dim o As FlashMemory.OTP_EPROM = DirectCast(mem, FlashMemory.OTP_EPROM)
                otp_devices.Add(o)
                Dim size_str As String
                If (o.FLASH_SIZE < 131072) Then
                    size_str = (o.FLASH_SIZE / 128).ToString & "Kbit"
                Else
                    size_str = (o.FLASH_SIZE / 131072).ToString & "Mbit"
                End If
                Dim list_str As String = o.NAME & " (" & size_str & ")"
                cb_otp_device_list.Items.Add(list_str)
                If o.MFG_CODE = MySettings.OTP_MFG AndAlso o.ID1 = MySettings.OTP_ID Then
                    otp_index = otp_counter
                End If
                otp_counter += 1
            End If
        Next
        cb_otp_device_list.SelectedIndex = otp_index
        cb_s93_devices.SelectedIndex = MySettings.S93_DEVICE_INDEX
        cb_s93_org.SelectedIndex = MySettings.S93_DEVICE_ORG
        cb_retry_write.SelectedIndex = (MySettings.VERIFY_COUNT - 1)
        'BETA VERSION ONLY - DISABLE THESE
        MyTabs.TabPages.Remove(TP_JTAG)
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
        rb_mainspare_default.Text = RM.GetString("settings_seperate") '"Seperate"
        rb_mainspare_segmented.Text = RM.GetString("settings_segmented") '"Segmented"
        rb_mainspare_all.Text = RM.GetString("settings_combined") '"Combined"
        lbl_nandecc_enabled.Text = RM.GetString("nandecc_enabled") '"Enabled"
        lbl_nandecc_algorithm.Text = RM.GetString("nandecc_algorithm") '"Algorithm"
        lbl_nandecc_biterror.Text = RM.GetString("nandecc_biterror") '"Bit-error"
        lbl_nandecc_location.Text = RM.GetString("nandecc_ecclocation") '"ECC location"
        cb_ECC_ReadEnable.Text = RM.GetString("nandecc_read_operation") '"Read operation (auto-correct)"
        cb_ECC_WriteEnable.Text = RM.GetString("nandecc_write_operation") '"Write operation (write ECC)"
        lbl_nandecc_changes.Text = RM.GetString("nandecc_changes") '"* Changes take effect on device detect event"
        gb_nandecc_title.Text = RM.GetString("nandecc_groupbox") '"Software ECC Feature"
        cb_rs_reverse_data.Text = RM.GetString("nandecc_revbyteorder") '"Reverse byte order"
        lbl_sym_width.Text = RM.GetString("nandecc_symwidth") '"Symbol width"
        If MySettings.LanguageName = "Spanish" Then
            cb_badmarker_1st_page1.Location = New Point(64, 45)
            cb_badmarker_6th_page1.Location = New Point(64, 66)
            cb_badmarker_1st_lastpage.Location = New Point(362, 45)
        End If
    End Sub

    Private Sub FrmSettings_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        Select Case cb_spi_pro_clock.SelectedIndex
            Case 0
                MySettings.SPI_CLOCK_PRO = SPI.SPI_CLOCK_SPEED.MHZ_5
            Case 1
                MySettings.SPI_CLOCK_PRO = SPI.SPI_CLOCK_SPEED.MHZ_8
            Case 2
                MySettings.SPI_CLOCK_PRO = SPI.SPI_CLOCK_SPEED.MHZ_10
            Case 3
                MySettings.SPI_CLOCK_PRO = SPI.SPI_CLOCK_SPEED.MHZ_12
            Case 4
                MySettings.SPI_CLOCK_PRO = SPI.SPI_CLOCK_SPEED.MHZ_15
            Case 5
                MySettings.SPI_CLOCK_PRO = SPI.SPI_CLOCK_SPEED.MHZ_20
            Case 6
                MySettings.SPI_CLOCK_PRO = SPI.SPI_CLOCK_SPEED.MHZ_24
            Case 7
                MySettings.SPI_CLOCK_PRO = SPI.SPI_CLOCK_SPEED.MHZ_30
        End Select
        Select Case cb_spi_clock.SelectedIndex
            Case 0
                MySettings.SPI_CLOCK_CLASSIC = SPI.SPI_CLOCK_SPEED.MHZ_1
            Case 1
                MySettings.SPI_CLOCK_CLASSIC = SPI.SPI_CLOCK_SPEED.MHZ_2
            Case 2
                MySettings.SPI_CLOCK_CLASSIC = SPI.SPI_CLOCK_SPEED.MHZ_4
            Case 3
                MySettings.SPI_CLOCK_CLASSIC = SPI.SPI_CLOCK_SPEED.MHZ_8
        End Select
        MySettings.SPI_FASTREAD = rb_fastread_op.Checked
        CustomDevice_SaveSettings()
        MySettings.NAND_Preserve = cb_preserve.Checked
        MySettings.NAND_MismatchSkip = cb_mismatch.Checked
        If cb_badblock_disabled.Checked Then
            MySettings.NAND_BadBlockManager = FlashcatSettings.BadBlockMode.Disabled
        End If
        If cb_badblock_enabled.Checked Then
            MySettings.NAND_BadBlockManager = FlashcatSettings.BadBlockMode.Enabled
        End If
        MySettings.NAND_BadBlockMarkers = 0
        If cb_badmarker_1st_page1.Checked Then
            MySettings.NAND_BadBlockMarkers = MySettings.NAND_BadBlockMarkers Or (FlashcatSettings.BadBlockMarker._1stByte_FirstPage)
        End If
        If cb_badmarker_1st_page2.Checked Then
            MySettings.NAND_BadBlockMarkers = MySettings.NAND_BadBlockMarkers Or (FlashcatSettings.BadBlockMarker._1stByte_SecondPage)
        End If
        If cb_badmarker_1st_lastpage.Checked Then
            MySettings.NAND_BadBlockMarkers = MySettings.NAND_BadBlockMarkers Or (FlashcatSettings.BadBlockMarker._1stByte_LastPage)
        End If
        If cb_badmarker_6th_page1.Checked Then
            MySettings.NAND_BadBlockMarkers = MySettings.NAND_BadBlockMarkers Or (FlashcatSettings.BadBlockMarker._6thByte_FirstPage)
        End If
        If cb_badmarker_6th_page2.Checked Then
            MySettings.NAND_BadBlockMarkers = MySettings.NAND_BadBlockMarkers Or (FlashcatSettings.BadBlockMarker._6thByte_SecondPage)
        End If
        If rb_mainspare_default.Checked Then
            MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Seperated
        ElseIf rb_mainspare_segmented.Checked Then
            MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Segmented
        ElseIf rb_mainspare_all.Checked Then
            MySettings.NAND_Layout = FlashcatSettings.NandMemLayout.Combined
        End If
        'i2c tab
        Dim i2c_address As Byte = &HA0 'Initial address
        If cbI2C_A2.Checked Then i2c_address = i2c_address Or (1 << 3)
        If cbI2C_A1.Checked Then i2c_address = i2c_address Or (1 << 2)
        If cbI2C_A0.Checked Then i2c_address = i2c_address Or (1 << 1)
        MySettings.I2C_ADDRESS = i2c_address
        If rb_speed_100khz.Checked Then
            MySettings.I2C_SPEED = FlashcatSettings.I2C_SPEED_MODE._100kHz
        ElseIf rb_speed_400khz.Checked Then
            MySettings.I2C_SPEED = FlashcatSettings.I2C_SPEED_MODE._400kHz
        ElseIf rb_speed_1mhz.Checked Then
            MySettings.I2C_SPEED = FlashcatSettings.I2C_SPEED_MODE._1MHz
        End If
        Select Case cbi2cDensity.SelectedIndex
            Case 0
                MySettings.I2C_SIZE = 0
            Case 1
                MySettings.I2C_SIZE = 128
            Case 2
                MySettings.I2C_SIZE = 256
            Case 3
                MySettings.I2C_SIZE = 512
            Case 4
                MySettings.I2C_SIZE = 1024
            Case 5
                MySettings.I2C_SIZE = 2048
            Case 6
                MySettings.I2C_SIZE = 4096
            Case 7
                MySettings.I2C_SIZE = 8192
            Case 8
                MySettings.I2C_SIZE = 16384
            Case 9
                MySettings.I2C_SIZE = 32768
            Case 10
                MySettings.I2C_SIZE = 65536
            Case 11
                MySettings.I2C_SIZE = 131072
            Case 12
                MySettings.I2C_SIZE = 262144
        End Select
        MySettings.SPI_EEPROM = cb_spi_eeprom.SelectedIndex
        MySettings.SPI_QUAD = cb_spi_quad.Checked
        MySettings.SPI_NAND_DISABLE_ECC = cb_spinand_disable_ecc.Checked
        MySettings.NAND_Verify = cb_nand_image_readverify.Checked
        MySettings.ECC_READ_ENABLED = cb_ECC_ReadEnable.Checked
        MySettings.ECC_WRITE_ENABLED = cb_ECC_WriteEnable.Checked
        MySettings.ECC_Seperate = cb_ecc_seperate.Checked
        MySettings.ECC_Reverse = cb_rs_reverse_data.Checked
        If rb_ECC_Hamming.Checked Then
            MySettings.ECC_Algorithum = 0
        ElseIf rb_ECC_ReedSolomon.Checked Then
            MySettings.ECC_Algorithum = 1
        ElseIf rb_ECC_BHC.Checked Then
            MySettings.ECC_Algorithum = 2
        End If
        MySettings.ECC_BitError = GetBitErrorLevel()
        Select Case cb_sym_width.SelectedIndex
            Case 0
                MySettings.ECC_SymWidth = 9
            Case 1
                MySettings.ECC_SymWidth = 10
        End Select
        Select Case cb_otp_device_list.SelectedIndex
            Case 0
                MySettings.OTP_MFG = 0
                MySettings.OTP_ID = 0
            Case Else
                Dim o As FlashMemory.OTP_EPROM = otp_devices(cb_otp_device_list.SelectedIndex - 1)
                MySettings.OTP_MFG = o.MFG_CODE
                MySettings.OTP_ID = o.ID1
        End Select
        MySettings.VERIFY_COUNT = cb_retry_write.SelectedIndex + 1
        MySettings.S93_DEVICE_INDEX = cb_s93_devices.SelectedIndex
        MySettings.S93_DEVICE_ORG = cb_s93_org.SelectedIndex
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
        SPICUSTOM_SetDensity(CUSTOM_SPI_DEV.FLASH_SIZE)
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
            Case FlashMemory.Kb032
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
        CUSTOM_SPI_DEV.OP_COMMANDS.READ = (op_read.SelectedIndex + 1)
        CUSTOM_SPI_DEV.OP_COMMANDS.PROG = (op_prog.SelectedIndex + 1)
        CUSTOM_SPI_DEV.OP_COMMANDS.SE = (op_sectorerase.SelectedIndex + 1)
        CUSTOM_SPI_DEV.OP_COMMANDS.WREN = (op_we.SelectedIndex + 1)
        CUSTOM_SPI_DEV.OP_COMMANDS.BE = (op_ce.SelectedIndex + 1)
        CUSTOM_SPI_DEV.OP_COMMANDS.RDSR = (op_rs.SelectedIndex + 1)
        CUSTOM_SPI_DEV.OP_COMMANDS.WRSR = (op_ws.SelectedIndex + 1)
        CUSTOM_SPI_DEV.OP_COMMANDS.EWSR = (op_ewsr.SelectedIndex + 1)
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
                CUSTOM_SPI_DEV.PAGE_SIZE_EXTENDED = CUSTOM_SPI_DEV.PAGE_SIZE + 8
            Case 2
                CUSTOM_SPI_DEV.PAGE_SIZE_EXTENDED = CUSTOM_SPI_DEV.PAGE_SIZE + 16
            Case 3
                CUSTOM_SPI_DEV.PAGE_SIZE_EXTENDED = CUSTOM_SPI_DEV.PAGE_SIZE + 32
            Case 4
                CUSTOM_SPI_DEV.PAGE_SIZE_EXTENDED = CUSTOM_SPI_DEV.PAGE_SIZE + 64
            Case 5
                CUSTOM_SPI_DEV.PAGE_SIZE_EXTENDED = CUSTOM_SPI_DEV.PAGE_SIZE + 128
        End Select
        CUSTOM_SPI_DEV.PAGE_COUNT = (CUSTOM_SPI_DEV.FLASH_SIZE / CUSTOM_SPI_DEV.PAGE_SIZE)
    End Sub

    Private Sub SPICUSTOM_SetDensity(ByVal size As UInt32)
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

    Private Sub Setup_i2C()
        If ((MySettings.I2C_ADDRESS And (1 << 3)) > 0) Then
            cbI2C_A2.Checked = True
        Else
            cbI2C_A2.Checked = False
        End If
        If ((MySettings.I2C_ADDRESS And (1 << 2)) > 0) Then
            cbI2C_A1.Checked = True
        Else
            cbI2C_A1.Checked = False
        End If
        If ((MySettings.I2C_ADDRESS And (1 << 1)) > 0) Then
            cbI2C_A0.Checked = True
        Else
            cbI2C_A0.Checked = False
        End If
        Select Case MySettings.I2C_SIZE
            Case 0
                cbi2cDensity.SelectedIndex = 0
            Case 128
                cbi2cDensity.SelectedIndex = 1
            Case 256
                cbi2cDensity.SelectedIndex = 2
            Case 512
                cbi2cDensity.SelectedIndex = 3
            Case 1024
                cbi2cDensity.SelectedIndex = 4
            Case 2048
                cbi2cDensity.SelectedIndex = 5
            Case 4096
                cbi2cDensity.SelectedIndex = 6
            Case 8192
                cbi2cDensity.SelectedIndex = 7
            Case 16384
                cbi2cDensity.SelectedIndex = 8
            Case 32768
                cbi2cDensity.SelectedIndex = 9
            Case 65536
                cbi2cDensity.SelectedIndex = 10
            Case 131072
                cbi2cDensity.SelectedIndex = 11
            Case 262144
                cbi2cDensity.SelectedIndex = 12
            Case Else
                cbi2cDensity.SelectedIndex = 7
        End Select
        Select Case MySettings.I2C_SPEED
            Case FlashcatSettings.I2C_SPEED_MODE._100kHz
                rb_speed_100khz.Checked = True
            Case FlashcatSettings.I2C_SPEED_MODE._400kHz
                rb_speed_400khz.Checked = True
            Case FlashcatSettings.I2C_SPEED_MODE._1MHz
                rb_speed_1mhz.Checked = True
        End Select
    End Sub

    Private Sub cbi2cDensity_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbi2cDensity.SelectedIndexChanged
        cbI2C_A0.Enabled = True
        cbI2C_A1.Enabled = True
        cbI2C_A2.Enabled = True
        If cbi2cDensity.SelectedIndex = 11 Then
            cbI2C_A0.Checked = False
            cbI2C_A0.Enabled = False
        ElseIf cbi2cDensity.SelectedIndex = 12 Then
            cbI2C_A0.Checked = False
            cbI2C_A1.Checked = False
            cbI2C_A0.Enabled = False
            cbI2C_A1.Enabled = False
        End If
    End Sub

    Private Sub SetupSpiEeprom()
        cb_spi_eeprom.Items.Add("(Not selected)") 'Index 0
        For Each item In SPI_EEPROM_LIST
            cb_spi_eeprom.Items.Add(item.NAME)
        Next
        cb_spi_eeprom.SelectedIndex = MySettings.SPI_EEPROM
    End Sub

    Private Sub rb_mainspare_default_CheckedChanged(sender As Object, e As EventArgs) Handles rb_mainspare_default.CheckedChanged
        If rb_mainspare_default.Checked Then
            nand_box.Image = My.Resources.nand_page_seperate
            gb_block_manager.Enabled = True
            cb_ecc_seperate.Enabled = True
            gb_nandecc_title.Enabled = True
        End If
    End Sub

    Private Sub rb_mainspare_segmented_CheckedChanged(sender As Object, e As EventArgs) Handles rb_mainspare_segmented.CheckedChanged
        If rb_mainspare_segmented.Checked Then
            nand_box.Image = My.Resources.nand_page_segmented
            gb_block_manager.Enabled = True
            cb_ecc_seperate.Enabled = False
        Else
            cb_ecc_seperate.Enabled = True
        End If
    End Sub

    Private Sub rb_mainspare_all_CheckedChanged(sender As Object, e As EventArgs) Handles rb_mainspare_all.CheckedChanged
        If rb_mainspare_all.Checked Then
            nand_box.Image = My.Resources.nand_page_combined
            gb_block_manager.Enabled = False
            gb_nandecc_title.Enabled = False
        Else
            gb_block_manager.Enabled = True
            gb_nandecc_title.Enabled = True
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

#Region "NAND (ECC) TAB"

    Private Sub rb_ECC_Hamming_CheckedChanged(sender As Object, e As EventArgs) Handles rb_ECC_Hamming.CheckedChanged
        If rb_ECC_Hamming.Checked Then
            cb_ECC_BITERR.SelectedIndex = 0
            cb_ECC_BITERR.Enabled = False 'Only 1-bit ECC supported
            cb_sym_width.Enabled = False
            UpdateEccEngine()
        End If
    End Sub

    Private Sub rb_ECC_ReedSolomon_CheckedChanged(sender As Object, e As EventArgs) Handles rb_ECC_ReedSolomon.CheckedChanged
        If rb_ECC_ReedSolomon.Checked Then
            cb_ECC_BITERR.Enabled = True
            cb_sym_width.Enabled = True
            UpdateEccEngine()
        End If
    End Sub

    Private Sub rb_ECC_BHC_CheckedChanged(sender As Object, e As EventArgs) Handles rb_ECC_BHC.CheckedChanged
        If rb_ECC_BHC.Checked Then
            cb_ECC_BITERR.Enabled = True
            cb_sym_width.Enabled = False
            UpdateEccEngine()
        End If
    End Sub

    Private Sub cb_ECC_ReadEnable_CheckedChanged(sender As Object, e As EventArgs) Handles cb_ECC_ReadEnable.CheckedChanged
        ECC_CheckIfEnabled()
    End Sub

    Private Sub cb_ECC_WriteEnable_CheckedChanged(sender As Object, e As EventArgs) Handles cb_ECC_WriteEnable.CheckedChanged
        ECC_CheckIfEnabled()
    End Sub

    Private Sub ECC_CheckIfEnabled()
        If cb_ECC_ReadEnable.Checked Or cb_ECC_WriteEnable.Checked Then
            rb_ECC_Hamming.Enabled = True
            rb_ECC_ReedSolomon.Enabled = True
            rb_ECC_BHC.Enabled = True
            txt_ecc_location.Enabled = True
            If cb_ECC_ReadEnable.Checked Then
                If rb_ECC_ReedSolomon.Checked Then
                    cb_sym_width.Enabled = True
                Else
                    cb_sym_width.Enabled = False
                End If
            End If
            cb_rs_reverse_data.Enabled = True
            cb_ecc_seperate.Enabled = True
            If rb_ECC_Hamming.Checked Then
                cb_ECC_BITERR.Enabled = False
                cb_ECC_BITERR.SelectedIndex = 0
            Else
                cb_ECC_BITERR.Enabled = True
            End If
        Else
            rb_ECC_Hamming.Enabled = False
            rb_ECC_ReedSolomon.Enabled = False
            rb_ECC_BHC.Enabled = False
            cb_ECC_BITERR.Enabled = False
            txt_ecc_location.Enabled = False
            cb_sym_width.Enabled = False
            cb_rs_reverse_data.Enabled = False
            cb_ecc_seperate.Enabled = False
        End If
    End Sub

    Private Sub UpdateEccEngine()
        Dim bit_lvl As UInt16 = GetBitErrorLevel()
        If bit_lvl = 0 Then
            lbl_ECC_size.Text = "ECC data size: 0 bytes"
            Exit Sub
        End If
        Dim ecc_eng_example As Engine = Nothing
        If rb_ECC_Hamming.Checked Then
            ecc_eng_example = New Engine(ecc_algorithum.hamming, bit_lvl)
        ElseIf rb_ECC_ReedSolomon.Checked Then
            ecc_eng_example = New Engine(ecc_algorithum.reedsolomon, bit_lvl)
            Select Case cb_sym_width.SelectedIndex
                Case 0
                    ecc_eng_example.SetSymbolWidth(9)
                Case 1
                    ecc_eng_example.SetSymbolWidth(10)
            End Select
        ElseIf rb_ECC_BHC.Checked Then
            ecc_eng_example = New Engine(ecc_algorithum.bhc, bit_lvl)
        End If
        If ecc_eng_example Is Nothing Then
            lbl_ECC_size.Text = "ECC data per 512 byte sector: 0 bytes"
        Else
            Dim ecc_data_size As Integer = ecc_eng_example.GetEccByteSize
            Dim ecc_offset As UInt16 = MySettings.ECC_Location
            lbl_ECC_size.Text = "ECC data per 512 byte sector: " & ecc_data_size.ToString & " bytes"
        End If
    End Sub

    Private Function GetBitErrorLevel() As UInt16
        Select Case cb_ECC_BITERR.SelectedIndex
            Case 0
                Return 1
            Case 1
                Return 2
            Case 2
                Return 4
            Case 3
                Return 8
            Case 4
                Return 10
            Case 5
                Return 14
            Case Else
                Return 0
        End Select
    End Function

    Private Sub SetBitErrorLevel(ByVal value As UInt16)
        Select Case value
            Case 1
                cb_ECC_BITERR.SelectedIndex = 0
            Case 2
                cb_ECC_BITERR.SelectedIndex = 1
            Case 4
                cb_ECC_BITERR.SelectedIndex = 2
            Case 8
                cb_ECC_BITERR.SelectedIndex = 3
            Case 10
                cb_ECC_BITERR.SelectedIndex = 4
            Case 14
                cb_ECC_BITERR.SelectedIndex = 5
            Case Else
                cb_ECC_BITERR.SelectedIndex = 0
        End Select
    End Sub

    Private Sub cb_ECC_BITERR_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cb_ECC_BITERR.SelectedIndexChanged
        UpdateEccEngine()
    End Sub

    Private Sub cb_sym_width_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cb_sym_width.SelectedIndexChanged
        UpdateEccEngine()
    End Sub

    Private Sub txt_ecc_location_LostFocus(sender As Object, e As EventArgs) Handles txt_ecc_location.LostFocus
        Try
            Dim current_value As String = txt_ecc_location.Text.Trim
            If (current_value = "") Then
                MySettings.ECC_Location = 0
            Else
                If current_value.ToUpper.StartsWith("0X") Then
                    If Utilities.IsDataType.Hex(current_value) Then
                        Dim d() As Byte = Utilities.Bytes.FromHexString(current_value)
                        If d IsNot Nothing AndAlso d.Length = 1 Then
                            MySettings.ECC_Location = d(0)
                        End If
                    End If
                ElseIf IsNumeric(current_value) AndAlso CInt(current_value) < 256 Then
                    MySettings.ECC_Location = CInt(current_value)
                End If
            End If
        Catch ex As Exception
        End Try
        txt_ecc_location.Text = "0x" & Hex(MySettings.ECC_Location).PadLeft(2, "0")
    End Sub

#End Region


End Class