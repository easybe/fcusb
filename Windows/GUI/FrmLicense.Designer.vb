<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FrmLicense
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.txt_license_file = New System.Windows.Forms.TextBox()
        Me.cmdClose = New System.Windows.Forms.Button()
        Me.lbl_licensed_to = New System.Windows.Forms.Label()
        Me.lbl_exp = New System.Windows.Forms.Label()
        Me.cmd_enter_key = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'txt_license_file
        '
        Me.txt_license_file.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txt_license_file.Location = New System.Drawing.Point(12, 36)
        Me.txt_license_file.Multiline = True
        Me.txt_license_file.Name = "txt_license_file"
        Me.txt_license_file.ReadOnly = True
        Me.txt_license_file.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txt_license_file.Size = New System.Drawing.Size(433, 238)
        Me.txt_license_file.TabIndex = 0
        '
        'cmdClose
        '
        Me.cmdClose.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdClose.Location = New System.Drawing.Point(367, 279)
        Me.cmdClose.Name = "cmdClose"
        Me.cmdClose.Size = New System.Drawing.Size(78, 23)
        Me.cmdClose.TabIndex = 1
        Me.cmdClose.Text = "Close"
        Me.cmdClose.UseVisualStyleBackColor = True
        '
        'lbl_licensed_to
        '
        Me.lbl_licensed_to.AutoSize = True
        Me.lbl_licensed_to.Location = New System.Drawing.Point(12, 9)
        Me.lbl_licensed_to.Name = "lbl_licensed_to"
        Me.lbl_licensed_to.Size = New System.Drawing.Size(109, 13)
        Me.lbl_licensed_to.TabIndex = 2
        Me.lbl_licensed_to.Text = "Licensed to: (BLANK)"
        '
        'lbl_exp
        '
        Me.lbl_exp.AutoSize = True
        Me.lbl_exp.Location = New System.Drawing.Point(300, 9)
        Me.lbl_exp.Name = "lbl_exp"
        Me.lbl_exp.Size = New System.Drawing.Size(124, 13)
        Me.lbl_exp.TabIndex = 3
        Me.lbl_exp.Text = "Expiration date: (BLANK)"
        '
        'cmd_enter_key
        '
        Me.cmd_enter_key.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmd_enter_key.Location = New System.Drawing.Point(283, 279)
        Me.cmd_enter_key.Name = "cmd_enter_key"
        Me.cmd_enter_key.Size = New System.Drawing.Size(78, 23)
        Me.cmd_enter_key.TabIndex = 4
        Me.cmd_enter_key.Text = "Enter Key"
        Me.cmd_enter_key.UseVisualStyleBackColor = True
        '
        'FrmLicense
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(457, 307)
        Me.Controls.Add(Me.cmd_enter_key)
        Me.Controls.Add(Me.lbl_exp)
        Me.Controls.Add(Me.lbl_licensed_to)
        Me.Controls.Add(Me.cmdClose)
        Me.Controls.Add(Me.txt_license_file)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.Name = "FrmLicense"
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Software License (Commericial use)"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents txt_license_file As TextBox
    Friend WithEvents cmdClose As Button
    Friend WithEvents lbl_licensed_to As Label
    Friend WithEvents lbl_exp As Label
    Friend WithEvents cmd_enter_key As Button
End Class
