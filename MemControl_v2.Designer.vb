<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MemControl_v2
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
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
        Me.pbar = New System.Windows.Forms.ProgressBar()
        Me.cmd_area = New System.Windows.Forms.Button()
        Me.lbl_status = New System.Windows.Forms.Label()
        Me.gb_flash = New System.Windows.Forms.GroupBox()
        Me.Editor = New FlashcatUSB.HexEditor_v2()
        Me.txtAddress = New System.Windows.Forms.TextBox()
        Me.cmd_erase = New System.Windows.Forms.Button()
        Me.cmd_write = New System.Windows.Forms.Button()
        Me.cmd_read = New System.Windows.Forms.Button()
        Me.cmd_cancel = New System.Windows.Forms.Button()
        Me.gb_flash.SuspendLayout()
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
        Me.cmd_area.TabIndex = 18
        Me.cmd_area.Text = "(Area)"
        Me.cmd_area.UseVisualStyleBackColor = True
        '
        'lbl_status
        '
        Me.lbl_status.AutoSize = True
        Me.lbl_status.Location = New System.Drawing.Point(102, 25)
        Me.lbl_status.Name = "lbl_status"
        Me.lbl_status.Size = New System.Drawing.Size(80, 13)
        Me.lbl_status.TabIndex = 19
        Me.lbl_status.Text = "Status: (Ready)"
        '
        'gb_flash
        '
        Me.gb_flash.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.gb_flash.Controls.Add(Me.Editor)
        Me.gb_flash.Controls.Add(Me.txtAddress)
        Me.gb_flash.Controls.Add(Me.cmd_erase)
        Me.gb_flash.Controls.Add(Me.cmd_write)
        Me.gb_flash.Controls.Add(Me.cmd_read)
        Me.gb_flash.Controls.Add(Me.lbl_status)
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
        'Editor
        '
        Me.Editor.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Editor.BaseAddress = CType(0, Long)
        Me.Editor.Location = New System.Drawing.Point(2, 66)
        Me.Editor.Name = "Editor"
        Me.Editor.Size = New System.Drawing.Size(368, 139)
        Me.Editor.TabIndex = 24
        '
        'txtAddress
        '
        Me.txtAddress.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtAddress.Location = New System.Drawing.Point(275, 22)
        Me.txtAddress.Name = "txtAddress"
        Me.txtAddress.Size = New System.Drawing.Size(92, 20)
        Me.txtAddress.TabIndex = 23
        '
        'cmd_erase
        '
        Me.cmd_erase.Image = Global.FlashcatUSB.My.Resources.Resources.chip_erase
        Me.cmd_erase.Location = New System.Drawing.Point(72, 19)
        Me.cmd_erase.Name = "cmd_erase"
        Me.cmd_erase.Size = New System.Drawing.Size(24, 24)
        Me.cmd_erase.TabIndex = 22
        Me.cmd_erase.UseVisualStyleBackColor = True
        '
        'cmd_write
        '
        Me.cmd_write.Image = Global.FlashcatUSB.My.Resources.Resources.chip_write
        Me.cmd_write.Location = New System.Drawing.Point(41, 19)
        Me.cmd_write.Name = "cmd_write"
        Me.cmd_write.Size = New System.Drawing.Size(24, 24)
        Me.cmd_write.TabIndex = 21
        Me.cmd_write.UseVisualStyleBackColor = True
        '
        'cmd_read
        '
        Me.cmd_read.Image = Global.FlashcatUSB.My.Resources.Resources.chip_read
        Me.cmd_read.Location = New System.Drawing.Point(10, 19)
        Me.cmd_read.Name = "cmd_read"
        Me.cmd_read.Size = New System.Drawing.Size(24, 24)
        Me.cmd_read.TabIndex = 20
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
        'MemControl_v2
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.gb_flash)
        Me.Name = "MemControl_v2"
        Me.Size = New System.Drawing.Size(379, 218)
        Me.gb_flash.ResumeLayout(False)
        Me.gb_flash.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents pbar As ProgressBar
    Friend WithEvents cmd_area As Button
    Friend WithEvents lbl_status As Label
    Friend WithEvents gb_flash As GroupBox
    Friend WithEvents cmd_write As Button
    Friend WithEvents cmd_read As Button
    Friend WithEvents cmd_erase As Button
    Friend WithEvents txtAddress As TextBox
    Friend WithEvents Editor As HexEditor_v2
    Friend WithEvents cmd_cancel As Button
End Class
