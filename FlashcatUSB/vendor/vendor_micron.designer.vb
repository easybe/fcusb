<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class vendor_micron
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.group_sr = New System.Windows.Forms.GroupBox()
        Me.group_nonvol = New System.Windows.Forms.GroupBox()
        Me.cmd_read_config = New System.Windows.Forms.Button()
        Me.cmd_write_config = New System.Windows.Forms.Button()
        Me.cb_status_ro = New System.Windows.Forms.CheckBox()
        Me.cb_protected_area = New System.Windows.Forms.ComboBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.cb_block_bp3 = New System.Windows.Forms.CheckBox()
        Me.cb_block_bp2 = New System.Windows.Forms.CheckBox()
        Me.cb_block_bp1 = New System.Windows.Forms.CheckBox()
        Me.cb_block_bp0 = New System.Windows.Forms.CheckBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.cb_dummy = New System.Windows.Forms.ComboBox()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.cb_xip_mode = New System.Windows.Forms.ComboBox()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.cb_output_drv = New System.Windows.Forms.ComboBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.cb_reset_disable = New System.Windows.Forms.CheckBox()
        Me.cb_serial_mode = New System.Windows.Forms.ComboBox()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.cb_segment = New System.Windows.Forms.ComboBox()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.cb_address_mode = New System.Windows.Forms.ComboBox()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.group_sr.SuspendLayout()
        Me.group_nonvol.SuspendLayout()
        Me.SuspendLayout()
        '
        'group_sr
        '
        Me.group_sr.Controls.Add(Me.Label2)
        Me.group_sr.Controls.Add(Me.cb_protected_area)
        Me.group_sr.Controls.Add(Me.cb_block_bp0)
        Me.group_sr.Controls.Add(Me.cb_block_bp1)
        Me.group_sr.Controls.Add(Me.cb_block_bp2)
        Me.group_sr.Controls.Add(Me.cb_block_bp3)
        Me.group_sr.Controls.Add(Me.Label1)
        Me.group_sr.Controls.Add(Me.cb_status_ro)
        Me.group_sr.Location = New System.Drawing.Point(3, 3)
        Me.group_sr.Name = "group_sr"
        Me.group_sr.Size = New System.Drawing.Size(444, 98)
        Me.group_sr.TabIndex = 0
        Me.group_sr.TabStop = False
        Me.group_sr.Text = "Status Register"
        '
        'group_nonvol
        '
        Me.group_nonvol.Controls.Add(Me.cb_address_mode)
        Me.group_nonvol.Controls.Add(Me.Label8)
        Me.group_nonvol.Controls.Add(Me.cb_reset_disable)
        Me.group_nonvol.Controls.Add(Me.Label3)
        Me.group_nonvol.Controls.Add(Me.cb_segment)
        Me.group_nonvol.Controls.Add(Me.cb_dummy)
        Me.group_nonvol.Controls.Add(Me.Label7)
        Me.group_nonvol.Controls.Add(Me.cb_output_drv)
        Me.group_nonvol.Controls.Add(Me.Label5)
        Me.group_nonvol.Controls.Add(Me.Label4)
        Me.group_nonvol.Controls.Add(Me.cb_serial_mode)
        Me.group_nonvol.Controls.Add(Me.cb_xip_mode)
        Me.group_nonvol.Controls.Add(Me.Label6)
        Me.group_nonvol.Location = New System.Drawing.Point(3, 107)
        Me.group_nonvol.Name = "group_nonvol"
        Me.group_nonvol.Size = New System.Drawing.Size(444, 152)
        Me.group_nonvol.TabIndex = 1
        Me.group_nonvol.TabStop = False
        Me.group_nonvol.Text = "Nonvolatile Configuration Register"
        '
        'cmd_read_config
        '
        Me.cmd_read_config.Location = New System.Drawing.Point(366, 263)
        Me.cmd_read_config.Name = "cmd_read_config"
        Me.cmd_read_config.Size = New System.Drawing.Size(75, 23)
        Me.cmd_read_config.TabIndex = 2
        Me.cmd_read_config.Text = "Read"
        Me.cmd_read_config.UseVisualStyleBackColor = True
        '
        'cmd_write_config
        '
        Me.cmd_write_config.Location = New System.Drawing.Point(285, 263)
        Me.cmd_write_config.Name = "cmd_write_config"
        Me.cmd_write_config.Size = New System.Drawing.Size(75, 23)
        Me.cmd_write_config.TabIndex = 3
        Me.cmd_write_config.Text = "Write"
        Me.cmd_write_config.UseVisualStyleBackColor = True
        '
        'cb_status_ro
        '
        Me.cb_status_ro.AutoSize = True
        Me.cb_status_ro.Location = New System.Drawing.Point(10, 19)
        Me.cb_status_ro.Name = "cb_status_ro"
        Me.cb_status_ro.Size = New System.Drawing.Size(151, 17)
        Me.cb_status_ro.TabIndex = 0
        Me.cb_status_ro.Text = "Status Register Read-Only"
        Me.cb_status_ro.UseVisualStyleBackColor = True
        '
        'cb_protected_area
        '
        Me.cb_protected_area.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_protected_area.FormattingEnabled = True
        Me.cb_protected_area.Items.AddRange(New Object() {"Top", "Bottom"})
        Me.cb_protected_area.Location = New System.Drawing.Point(132, 42)
        Me.cb_protected_area.Name = "cb_protected_area"
        Me.cb_protected_area.Size = New System.Drawing.Size(100, 21)
        Me.cb_protected_area.TabIndex = 4
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(7, 45)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(119, 13)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Protected memory area:"
        '
        'cb_block_bp3
        '
        Me.cb_block_bp3.AutoSize = True
        Me.cb_block_bp3.Location = New System.Drawing.Point(124, 73)
        Me.cb_block_bp3.Name = "cb_block_bp3"
        Me.cb_block_bp3.Size = New System.Drawing.Size(46, 17)
        Me.cb_block_bp3.TabIndex = 1
        Me.cb_block_bp3.Text = "BP3"
        Me.cb_block_bp3.UseVisualStyleBackColor = True
        '
        'cb_block_bp2
        '
        Me.cb_block_bp2.AutoSize = True
        Me.cb_block_bp2.Location = New System.Drawing.Point(176, 73)
        Me.cb_block_bp2.Name = "cb_block_bp2"
        Me.cb_block_bp2.Size = New System.Drawing.Size(46, 17)
        Me.cb_block_bp2.TabIndex = 2
        Me.cb_block_bp2.Text = "BP2"
        Me.cb_block_bp2.UseVisualStyleBackColor = True
        '
        'cb_block_bp1
        '
        Me.cb_block_bp1.AutoSize = True
        Me.cb_block_bp1.Location = New System.Drawing.Point(228, 73)
        Me.cb_block_bp1.Name = "cb_block_bp1"
        Me.cb_block_bp1.Size = New System.Drawing.Size(46, 17)
        Me.cb_block_bp1.TabIndex = 3
        Me.cb_block_bp1.Text = "BP1"
        Me.cb_block_bp1.UseVisualStyleBackColor = True
        '
        'cb_block_bp0
        '
        Me.cb_block_bp0.AutoSize = True
        Me.cb_block_bp0.Location = New System.Drawing.Point(282, 73)
        Me.cb_block_bp0.Name = "cb_block_bp0"
        Me.cb_block_bp0.Size = New System.Drawing.Size(46, 17)
        Me.cb_block_bp0.TabIndex = 4
        Me.cb_block_bp0.Text = "BP0"
        Me.cb_block_bp0.UseVisualStyleBackColor = True
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(7, 73)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(106, 13)
        Me.Label2.TabIndex = 5
        Me.Label2.Text = "Block protection bits:"
        '
        'cb_dummy
        '
        Me.cb_dummy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_dummy.FormattingEnabled = True
        Me.cb_dummy.Items.AddRange(New Object() {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"})
        Me.cb_dummy.Location = New System.Drawing.Point(15, 39)
        Me.cb_dummy.Name = "cb_dummy"
        Me.cb_dummy.Size = New System.Drawing.Size(160, 21)
        Me.cb_dummy.TabIndex = 7
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(12, 22)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(107, 13)
        Me.Label3.TabIndex = 6
        Me.Label3.Text = "Dummy clock cycles:"
        '
        'cb_xip_mode
        '
        Me.cb_xip_mode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_xip_mode.FormattingEnabled = True
        Me.cb_xip_mode.Items.AddRange(New Object() {"XIP: Fast Read", "XIP: Dual Output Fast Read", "XIP: Dual I/O Fast Read", "XIP: Quad Output Fast Read", "XIP: Quad I/O Fast Read", "Disabled"})
        Me.cb_xip_mode.Location = New System.Drawing.Point(15, 81)
        Me.cb_xip_mode.Name = "cb_xip_mode"
        Me.cb_xip_mode.Size = New System.Drawing.Size(160, 21)
        Me.cb_xip_mode.TabIndex = 9
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(12, 64)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(56, 13)
        Me.Label4.TabIndex = 8
        Me.Label4.Text = "XIP mode:"
        '
        'cb_output_drv
        '
        Me.cb_output_drv.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_output_drv.FormattingEnabled = True
        Me.cb_output_drv.Items.AddRange(New Object() {"90 Ohms", "60 Ohms", "45 Ohms", "30 Ohms", "20 Ohms", "15 Ohms"})
        Me.cb_output_drv.Location = New System.Drawing.Point(15, 123)
        Me.cb_output_drv.Name = "cb_output_drv"
        Me.cb_output_drv.Size = New System.Drawing.Size(160, 21)
        Me.cb_output_drv.TabIndex = 11
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(14, 106)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(112, 13)
        Me.Label5.TabIndex = 10
        Me.Label5.Text = "Output driver strength:"
        '
        'cb_reset_disable
        '
        Me.cb_reset_disable.AutoSize = True
        Me.cb_reset_disable.Location = New System.Drawing.Point(304, 41)
        Me.cb_reset_disable.Name = "cb_reset_disable"
        Me.cb_reset_disable.Size = New System.Drawing.Size(134, 17)
        Me.cb_reset_disable.TabIndex = 6
        Me.cb_reset_disable.Text = "Disable Reset/#HOLD"
        Me.cb_reset_disable.UseVisualStyleBackColor = True
        '
        'cb_serial_mode
        '
        Me.cb_serial_mode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_serial_mode.FormattingEnabled = True
        Me.cb_serial_mode.Items.AddRange(New Object() {"SPI", "Dual I/O", "Quad I/O"})
        Me.cb_serial_mode.Location = New System.Drawing.Point(191, 39)
        Me.cb_serial_mode.Name = "cb_serial_mode"
        Me.cb_serial_mode.Size = New System.Drawing.Size(100, 21)
        Me.cb_serial_mode.TabIndex = 13
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(192, 22)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(84, 13)
        Me.Label6.TabIndex = 12
        Me.Label6.Text = "Serial I/O mode:"
        '
        'cb_segment
        '
        Me.cb_segment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_segment.FormattingEnabled = True
        Me.cb_segment.Items.AddRange(New Object() {"Upper 128Mbit segment", "Lower 128Mbit segment"})
        Me.cb_segment.Location = New System.Drawing.Point(191, 81)
        Me.cb_segment.Name = "cb_segment"
        Me.cb_segment.Size = New System.Drawing.Size(156, 21)
        Me.cb_segment.TabIndex = 15
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(188, 64)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(122, 13)
        Me.Label7.TabIndex = 14
        Me.Label7.Text = "128Mbit segment select:"
        '
        'cb_address_mode
        '
        Me.cb_address_mode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_address_mode.FormattingEnabled = True
        Me.cb_address_mode.Items.AddRange(New Object() {"Use 4-byte address", "Use 3-byte address"})
        Me.cb_address_mode.Location = New System.Drawing.Point(191, 123)
        Me.cb_address_mode.Name = "cb_address_mode"
        Me.cb_address_mode.Size = New System.Drawing.Size(156, 21)
        Me.cb_address_mode.TabIndex = 17
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(188, 106)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(91, 13)
        Me.Label8.TabIndex = 16
        Me.Label8.Text = "Addressing mode:"
        '
        'NonVol_1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.cmd_write_config)
        Me.Controls.Add(Me.cmd_read_config)
        Me.Controls.Add(Me.group_nonvol)
        Me.Controls.Add(Me.group_sr)
        Me.Name = "NonVol_1"
        Me.Size = New System.Drawing.Size(455, 289)
        Me.group_sr.ResumeLayout(False)
        Me.group_sr.PerformLayout()
        Me.group_nonvol.ResumeLayout(False)
        Me.group_nonvol.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents group_sr As GroupBox
    Friend WithEvents group_nonvol As GroupBox
    Friend WithEvents cmd_read_config As Button
    Friend WithEvents cmd_write_config As Button
    Friend WithEvents Label1 As Label
    Friend WithEvents cb_status_ro As CheckBox
    Friend WithEvents cb_protected_area As ComboBox
    Friend WithEvents Label2 As Label
    Friend WithEvents cb_block_bp0 As CheckBox
    Friend WithEvents cb_block_bp1 As CheckBox
    Friend WithEvents cb_block_bp2 As CheckBox
    Friend WithEvents cb_block_bp3 As CheckBox
    Friend WithEvents cb_dummy As ComboBox
    Friend WithEvents Label3 As Label
    Friend WithEvents cb_xip_mode As ComboBox
    Friend WithEvents Label4 As Label
    Friend WithEvents cb_reset_disable As CheckBox
    Friend WithEvents cb_output_drv As ComboBox
    Friend WithEvents Label5 As Label
    Friend WithEvents cb_serial_mode As ComboBox
    Friend WithEvents Label6 As Label
    Friend WithEvents cb_segment As ComboBox
    Friend WithEvents Label7 As Label
    Friend WithEvents cb_address_mode As ComboBox
    Friend WithEvents Label8 As Label
End Class
