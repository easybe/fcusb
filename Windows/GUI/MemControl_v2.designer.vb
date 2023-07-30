<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MemControl_v2
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
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
        Me.pbar = New System.Windows.Forms.ProgressBar()
        Me.cmd_area = New System.Windows.Forms.Button()
        Me.gb_flash = New System.Windows.Forms.GroupBox()
        Me.cmd_edit = New System.Windows.Forms.CheckBox()
        Me.pb_ecc = New System.Windows.Forms.PictureBox()
        Me.cmd_compare = New System.Windows.Forms.Button()
        Me.HexEditor64 = New FlashcatUSB.HexEditor_v2()
        Me.txtAddress = New System.Windows.Forms.TextBox()
        Me.cmd_erase = New System.Windows.Forms.Button()
        Me.cmd_write = New System.Windows.Forms.Button()
        Me.cmd_read = New System.Windows.Forms.Button()
        Me.cmd_cancel = New System.Windows.Forms.Button()
        Me.menu_tip = New System.Windows.Forms.ToolTip(Me.components)
        Me.gb_flash.SuspendLayout()
        CType(Me.pb_ecc, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'pbar
        '
        Me.pbar.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pbar.Location = New System.Drawing.Point(8, 48)
        Me.pbar.Name = "pbar"
        Me.pbar.Size = New System.Drawing.Size(359, 12)
        Me.pbar.TabIndex = 16
        '
        'cmd_area
        '
        Me.cmd_area.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmd_area.Location = New System.Drawing.Point(216, 20)
        Me.cmd_area.Name = "cmd_area"
        Me.cmd_area.Size = New System.Drawing.Size(54, 23)
        Me.cmd_area.TabIndex = 30
        Me.cmd_area.Text = "(Area)"
        Me.cmd_area.UseVisualStyleBackColor = True
        '
        'gb_flash
        '
        Me.gb_flash.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.gb_flash.Controls.Add(Me.cmd_edit)
        Me.gb_flash.Controls.Add(Me.pb_ecc)
        Me.gb_flash.Controls.Add(Me.cmd_compare)
        Me.gb_flash.Controls.Add(Me.HexEditor64)
        Me.gb_flash.Controls.Add(Me.txtAddress)
        Me.gb_flash.Controls.Add(Me.cmd_erase)
        Me.gb_flash.Controls.Add(Me.cmd_write)
        Me.gb_flash.Controls.Add(Me.cmd_read)
        Me.gb_flash.Controls.Add(Me.pbar)
        Me.gb_flash.Controls.Add(Me.cmd_area)
        Me.gb_flash.Controls.Add(Me.cmd_cancel)
        Me.gb_flash.Location = New System.Drawing.Point(2, 4)
        Me.gb_flash.Name = "gb_flash"
        Me.gb_flash.Size = New System.Drawing.Size(377, 211)
        Me.gb_flash.TabIndex = 20
        Me.gb_flash.TabStop = False
        Me.gb_flash.Text = "(FLASH_NAME PART_NUMBER)"
        '
        'cmd_edit
        '
        Me.cmd_edit.Appearance = System.Windows.Forms.Appearance.Button
        Me.cmd_edit.AutoSize = True
        Me.cmd_edit.Image = Global.FlashcatUSB.My.Resources.Resources.edit_file
        Me.cmd_edit.Location = New System.Drawing.Point(122, 19)
        Me.cmd_edit.Name = "cmd_edit"
        Me.cmd_edit.Padding = New System.Windows.Forms.Padding(2)
        Me.cmd_edit.Size = New System.Drawing.Size(26, 23)
        Me.cmd_edit.TabIndex = 23
        Me.cmd_edit.Text = "   "
        Me.menu_tip.SetToolTip(Me.cmd_edit, "Enable edit buffer")
        Me.cmd_edit.UseVisualStyleBackColor = True
        '
        'pb_ecc
        '
        Me.pb_ecc.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pb_ecc.Image = Global.FlashcatUSB.My.Resources.Resources.ecc_blue
        Me.pb_ecc.Location = New System.Drawing.Point(190, 22)
        Me.pb_ecc.Name = "pb_ecc"
        Me.pb_ecc.Size = New System.Drawing.Size(20, 20)
        Me.pb_ecc.TabIndex = 28
        Me.pb_ecc.TabStop = False
        '
        'cmd_compare
        '
        Me.cmd_compare.Image = Global.FlashcatUSB.My.Resources.Resources.chip_verify
        Me.cmd_compare.Location = New System.Drawing.Point(94, 19)
        Me.cmd_compare.Name = "cmd_compare"
        Me.cmd_compare.Size = New System.Drawing.Size(24, 24)
        Me.cmd_compare.TabIndex = 24
        Me.menu_tip.SetToolTip(Me.cmd_compare, "Compare memory contents")
        Me.cmd_compare.UseVisualStyleBackColor = True
        '
        'HexEditor64
        '
        Me.HexEditor64.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.HexEditor64.BaseOffset = CType(0, Long)
        Me.HexEditor64.BaseSize = CType(0, Long)
        Me.HexEditor64.EDIT_MODE = False
        Me.HexEditor64.HexDataByteSize = 0
        Me.HexEditor64.Location = New System.Drawing.Point(4, 66)
        Me.HexEditor64.Margin = New System.Windows.Forms.Padding(4)
        Me.HexEditor64.Name = "HexEditor64"
        Me.HexEditor64.Size = New System.Drawing.Size(368, 139)
        Me.HexEditor64.TabIndex = 24
        Me.HexEditor64.TopAddress = CType(0, Long)
        '
        'txtAddress
        '
        Me.txtAddress.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtAddress.Location = New System.Drawing.Point(275, 22)
        Me.txtAddress.Name = "txtAddress"
        Me.txtAddress.Size = New System.Drawing.Size(92, 20)
        Me.txtAddress.TabIndex = 40
        '
        'cmd_erase
        '
        Me.cmd_erase.Image = Global.FlashcatUSB.My.Resources.Resources.chip_erase
        Me.cmd_erase.Location = New System.Drawing.Point(66, 19)
        Me.cmd_erase.Name = "cmd_erase"
        Me.cmd_erase.Size = New System.Drawing.Size(24, 24)
        Me.cmd_erase.TabIndex = 22
        Me.menu_tip.SetToolTip(Me.cmd_erase, "Erase all memory")
        Me.cmd_erase.UseVisualStyleBackColor = True
        '
        'cmd_write
        '
        Me.cmd_write.Image = Global.FlashcatUSB.My.Resources.Resources.chip_write
        Me.cmd_write.Location = New System.Drawing.Point(38, 19)
        Me.cmd_write.Name = "cmd_write"
        Me.cmd_write.Size = New System.Drawing.Size(24, 24)
        Me.cmd_write.TabIndex = 21
        Me.menu_tip.SetToolTip(Me.cmd_write, "Write data to memory")
        Me.cmd_write.UseVisualStyleBackColor = True
        '
        'cmd_read
        '
        Me.cmd_read.Image = Global.FlashcatUSB.My.Resources.Resources.chip_read
        Me.cmd_read.Location = New System.Drawing.Point(10, 19)
        Me.cmd_read.Name = "cmd_read"
        Me.cmd_read.Size = New System.Drawing.Size(24, 24)
        Me.cmd_read.TabIndex = 20
        Me.menu_tip.SetToolTip(Me.cmd_read, "Read memory to disk")
        Me.cmd_read.UseVisualStyleBackColor = True
        '
        'cmd_cancel
        '
        Me.cmd_cancel.Location = New System.Drawing.Point(10, 19)
        Me.cmd_cancel.Name = "cmd_cancel"
        Me.cmd_cancel.Size = New System.Drawing.Size(86, 24)
        Me.cmd_cancel.TabIndex = 25
        Me.cmd_cancel.Text = "Cancel"
        Me.cmd_cancel.UseVisualStyleBackColor = True
        '
        'menu_tip
        '
        Me.menu_tip.AutoPopDelay = 5000
        Me.menu_tip.InitialDelay = 1000
        Me.menu_tip.ReshowDelay = 100
        '
        'MemControl_v2
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.gb_flash)
        Me.Name = "MemControl_v2"
        Me.Size = New System.Drawing.Size(379, 218)
        Me.gb_flash.ResumeLayout(False)
        Me.gb_flash.PerformLayout()
        CType(Me.pb_ecc, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents pbar As ProgressBar
    Friend WithEvents cmd_area As Button
    Friend WithEvents gb_flash As GroupBox
    Friend WithEvents cmd_write As Button
    Friend WithEvents cmd_read As Button
    Friend WithEvents cmd_erase As Button
    Friend WithEvents txtAddress As TextBox
    Friend WithEvents HexEditor64 As HexEditor_v2
    Friend WithEvents cmd_cancel As Button
    Friend WithEvents cmd_compare As Button
    Friend WithEvents menu_tip As ToolTip
    Friend WithEvents pb_ecc As PictureBox
    Friend WithEvents cmd_edit As CheckBox
End Class
