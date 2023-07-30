<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class MemIOControl
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
        Me.components = New System.ComponentModel.Container()
        Me.pb_ecc = New System.Windows.Forms.PictureBox()
        Me.cmd_edit = New System.Windows.Forms.CheckBox()
        Me.cmd_compare = New System.Windows.Forms.Button()
        Me.cmd_erase = New System.Windows.Forms.Button()
        Me.cmd_write = New System.Windows.Forms.Button()
        Me.cmd_read = New System.Windows.Forms.Button()
        Me.cmd_area = New System.Windows.Forms.Button()
        Me.cmd_cancel = New System.Windows.Forms.Button()
        Me.menu_tip = New System.Windows.Forms.ToolTip(Me.components)
        CType(Me.pb_ecc, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'pb_ecc
        '
        Me.pb_ecc.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pb_ecc.InitialImage = Global.FlashcatUSB.My.Resources.Resources.ecc_blue
        Me.pb_ecc.Location = New System.Drawing.Point(182, 5)
        Me.pb_ecc.Name = "pb_ecc"
        Me.pb_ecc.Size = New System.Drawing.Size(20, 20)
        Me.pb_ecc.TabIndex = 49
        Me.pb_ecc.TabStop = False
        '
        'cmd_edit
        '
        Me.cmd_edit.Appearance = System.Windows.Forms.Appearance.Button
        Me.cmd_edit.AutoSize = True
        Me.cmd_edit.Image = Global.FlashcatUSB.My.Resources.Resources.edit_file
        Me.cmd_edit.Location = New System.Drawing.Point(114, 2)
        Me.cmd_edit.Name = "cmd_edit"
        Me.cmd_edit.Padding = New System.Windows.Forms.Padding(2)
        Me.cmd_edit.Size = New System.Drawing.Size(26, 23)
        Me.cmd_edit.TabIndex = 45
        Me.cmd_edit.Text = "   "
        Me.cmd_edit.UseVisualStyleBackColor = True
        '
        'cmd_compare
        '
        Me.cmd_compare.Image = Global.FlashcatUSB.My.Resources.Resources.chip_verify
        Me.cmd_compare.Location = New System.Drawing.Point(86, 2)
        Me.cmd_compare.Name = "cmd_compare"
        Me.cmd_compare.Size = New System.Drawing.Size(24, 24)
        Me.cmd_compare.TabIndex = 46
        Me.cmd_compare.UseVisualStyleBackColor = True
        '
        'cmd_erase
        '
        Me.cmd_erase.Image = Global.FlashcatUSB.My.Resources.Resources.chip_erase
        Me.cmd_erase.Location = New System.Drawing.Point(58, 2)
        Me.cmd_erase.Name = "cmd_erase"
        Me.cmd_erase.Size = New System.Drawing.Size(24, 24)
        Me.cmd_erase.TabIndex = 44
        Me.cmd_erase.UseVisualStyleBackColor = True
        '
        'cmd_write
        '
        Me.cmd_write.Image = Global.FlashcatUSB.My.Resources.Resources.chip_write
        Me.cmd_write.Location = New System.Drawing.Point(30, 2)
        Me.cmd_write.Name = "cmd_write"
        Me.cmd_write.Size = New System.Drawing.Size(24, 24)
        Me.cmd_write.TabIndex = 43
        Me.cmd_write.UseVisualStyleBackColor = True
        '
        'cmd_read
        '
        Me.cmd_read.Image = Global.FlashcatUSB.My.Resources.Resources.chip_read
        Me.cmd_read.Location = New System.Drawing.Point(2, 2)
        Me.cmd_read.Name = "cmd_read"
        Me.cmd_read.Size = New System.Drawing.Size(24, 24)
        Me.cmd_read.TabIndex = 42
        Me.cmd_read.UseVisualStyleBackColor = True
        '
        'cmd_area
        '
        Me.cmd_area.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmd_area.Location = New System.Drawing.Point(208, 3)
        Me.cmd_area.Name = "cmd_area"
        Me.cmd_area.Size = New System.Drawing.Size(54, 23)
        Me.cmd_area.TabIndex = 48
        Me.cmd_area.Text = "(Area)"
        Me.cmd_area.UseVisualStyleBackColor = True
        '
        'cmd_cancel
        '
        Me.cmd_cancel.Location = New System.Drawing.Point(2, 2)
        Me.cmd_cancel.Name = "cmd_cancel"
        Me.cmd_cancel.Size = New System.Drawing.Size(86, 24)
        Me.cmd_cancel.TabIndex = 47
        Me.cmd_cancel.Text = "Cancel"
        Me.cmd_cancel.UseVisualStyleBackColor = True
        '
        'menu_tip
        '
        Me.menu_tip.AutoPopDelay = 5000
        Me.menu_tip.InitialDelay = 1000
        Me.menu_tip.ReshowDelay = 100
        '
        'MemIOControl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.pb_ecc)
        Me.Controls.Add(Me.cmd_edit)
        Me.Controls.Add(Me.cmd_compare)
        Me.Controls.Add(Me.cmd_erase)
        Me.Controls.Add(Me.cmd_write)
        Me.Controls.Add(Me.cmd_read)
        Me.Controls.Add(Me.cmd_area)
        Me.Controls.Add(Me.cmd_cancel)
        Me.Name = "MemIOControl"
        Me.Size = New System.Drawing.Size(264, 28)
        CType(Me.pb_ecc, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents pb_ecc As PictureBox
    Friend WithEvents cmd_edit As CheckBox
    Friend WithEvents cmd_compare As Button
    Friend WithEvents cmd_erase As Button
    Friend WithEvents cmd_write As Button
    Friend WithEvents cmd_read As Button
    Friend WithEvents cmd_area As Button
    Friend WithEvents cmd_cancel As Button
    Friend WithEvents menu_tip As ToolTip
End Class
