<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class vendor_spansion
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
        Me.cb_sr1_7 = New System.Windows.Forms.CheckBox()
        Me.cb_sr1_6 = New System.Windows.Forms.CheckBox()
        Me.cb_sr1_5 = New System.Windows.Forms.CheckBox()
        Me.cb_sr1_4 = New System.Windows.Forms.CheckBox()
        Me.cb_sr1_3 = New System.Windows.Forms.CheckBox()
        Me.cb_sr1_2 = New System.Windows.Forms.CheckBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.group_sr1 = New System.Windows.Forms.GroupBox()
        Me.group_sr2 = New System.Windows.Forms.GroupBox()
        Me.cmd_write_config = New System.Windows.Forms.Button()
        Me.cmd_read_config = New System.Windows.Forms.Button()
        Me.cb_sr2_1 = New System.Windows.Forms.CheckBox()
        Me.group_sr1.SuspendLayout()
        Me.group_sr2.SuspendLayout()
        Me.SuspendLayout()
        '
        'cb_sr1_7
        '
        Me.cb_sr1_7.AutoSize = True
        Me.cb_sr1_7.Location = New System.Drawing.Point(12, 22)
        Me.cb_sr1_7.Name = "cb_sr1_7"
        Me.cb_sr1_7.Size = New System.Drawing.Size(135, 17)
        Me.cb_sr1_7.TabIndex = 1
        Me.cb_sr1_7.Text = "Status Register Protect"
        Me.cb_sr1_7.UseVisualStyleBackColor = True
        '
        'cb_sr1_6
        '
        Me.cb_sr1_6.AutoSize = True
        Me.cb_sr1_6.Location = New System.Drawing.Point(12, 45)
        Me.cb_sr1_6.Name = "cb_sr1_6"
        Me.cb_sr1_6.Size = New System.Drawing.Size(132, 17)
        Me.cb_sr1_6.TabIndex = 2
        Me.cb_sr1_6.Text = "Sector / Block Protect"
        Me.cb_sr1_6.UseVisualStyleBackColor = True
        '
        'cb_sr1_5
        '
        Me.cb_sr1_5.AutoSize = True
        Me.cb_sr1_5.Location = New System.Drawing.Point(12, 68)
        Me.cb_sr1_5.Name = "cb_sr1_5"
        Me.cb_sr1_5.Size = New System.Drawing.Size(126, 17)
        Me.cb_sr1_5.TabIndex = 3
        Me.cb_sr1_5.Text = "Top / Bottom Protect"
        Me.cb_sr1_5.UseVisualStyleBackColor = True
        '
        'cb_sr1_4
        '
        Me.cb_sr1_4.AutoSize = True
        Me.cb_sr1_4.Location = New System.Drawing.Point(12, 114)
        Me.cb_sr1_4.Name = "cb_sr1_4"
        Me.cb_sr1_4.Size = New System.Drawing.Size(46, 17)
        Me.cb_sr1_4.TabIndex = 4
        Me.cb_sr1_4.Text = "BP2"
        Me.cb_sr1_4.UseVisualStyleBackColor = True
        '
        'cb_sr1_3
        '
        Me.cb_sr1_3.AutoSize = True
        Me.cb_sr1_3.Location = New System.Drawing.Point(64, 114)
        Me.cb_sr1_3.Name = "cb_sr1_3"
        Me.cb_sr1_3.Size = New System.Drawing.Size(46, 17)
        Me.cb_sr1_3.TabIndex = 5
        Me.cb_sr1_3.Text = "BP1"
        Me.cb_sr1_3.UseVisualStyleBackColor = True
        '
        'cb_sr1_2
        '
        Me.cb_sr1_2.AutoSize = True
        Me.cb_sr1_2.Location = New System.Drawing.Point(116, 114)
        Me.cb_sr1_2.Name = "cb_sr1_2"
        Me.cb_sr1_2.Size = New System.Drawing.Size(46, 17)
        Me.cb_sr1_2.TabIndex = 6
        Me.cb_sr1_2.Text = "BP0"
        Me.cb_sr1_2.UseVisualStyleBackColor = True
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(9, 98)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(91, 13)
        Me.Label1.TabIndex = 7
        Me.Label1.Text = "Block Protect Bits"
        '
        'group_sr1
        '
        Me.group_sr1.Controls.Add(Me.cb_sr1_7)
        Me.group_sr1.Controls.Add(Me.Label1)
        Me.group_sr1.Controls.Add(Me.cb_sr1_6)
        Me.group_sr1.Controls.Add(Me.cb_sr1_2)
        Me.group_sr1.Controls.Add(Me.cb_sr1_5)
        Me.group_sr1.Controls.Add(Me.cb_sr1_3)
        Me.group_sr1.Controls.Add(Me.cb_sr1_4)
        Me.group_sr1.Location = New System.Drawing.Point(3, 3)
        Me.group_sr1.Name = "group_sr1"
        Me.group_sr1.Size = New System.Drawing.Size(342, 137)
        Me.group_sr1.TabIndex = 8
        Me.group_sr1.TabStop = False
        Me.group_sr1.Text = "Status Register-1"
        '
        'group_sr2
        '
        Me.group_sr2.Controls.Add(Me.cb_sr2_1)
        Me.group_sr2.Location = New System.Drawing.Point(3, 146)
        Me.group_sr2.Name = "group_sr2"
        Me.group_sr2.Size = New System.Drawing.Size(342, 48)
        Me.group_sr2.TabIndex = 9
        Me.group_sr2.TabStop = False
        Me.group_sr2.Text = "Status Register-2"
        '
        'cmd_write_config
        '
        Me.cmd_write_config.Location = New System.Drawing.Point(189, 200)
        Me.cmd_write_config.Name = "cmd_write_config"
        Me.cmd_write_config.Size = New System.Drawing.Size(75, 23)
        Me.cmd_write_config.TabIndex = 11
        Me.cmd_write_config.Text = "Write"
        Me.cmd_write_config.UseVisualStyleBackColor = True
        '
        'cmd_read_config
        '
        Me.cmd_read_config.Location = New System.Drawing.Point(270, 200)
        Me.cmd_read_config.Name = "cmd_read_config"
        Me.cmd_read_config.Size = New System.Drawing.Size(75, 23)
        Me.cmd_read_config.TabIndex = 10
        Me.cmd_read_config.Text = "Read"
        Me.cmd_read_config.UseVisualStyleBackColor = True
        '
        'cb_sr2_1
        '
        Me.cb_sr2_1.AutoSize = True
        Me.cb_sr2_1.Location = New System.Drawing.Point(12, 21)
        Me.cb_sr2_1.Name = "cb_sr2_1"
        Me.cb_sr2_1.Size = New System.Drawing.Size(88, 17)
        Me.cb_sr2_1.TabIndex = 8
        Me.cb_sr2_1.Text = "Quad Enable"
        Me.cb_sr2_1.UseVisualStyleBackColor = True
        '
        'NonVol_2
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.cmd_write_config)
        Me.Controls.Add(Me.cmd_read_config)
        Me.Controls.Add(Me.group_sr2)
        Me.Controls.Add(Me.group_sr1)
        Me.Name = "NonVol_2"
        Me.Size = New System.Drawing.Size(355, 228)
        Me.group_sr1.ResumeLayout(False)
        Me.group_sr1.PerformLayout()
        Me.group_sr2.ResumeLayout(False)
        Me.group_sr2.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents cb_sr1_7 As CheckBox
    Friend WithEvents cb_sr1_6 As CheckBox
    Friend WithEvents cb_sr1_5 As CheckBox
    Friend WithEvents cb_sr1_4 As CheckBox
    Friend WithEvents cb_sr1_3 As CheckBox
    Friend WithEvents cb_sr1_2 As CheckBox
    Friend WithEvents Label1 As Label
    Friend WithEvents group_sr1 As GroupBox
    Friend WithEvents group_sr2 As GroupBox
    Friend WithEvents cmd_write_config As Button
    Friend WithEvents cmd_read_config As Button
    Friend WithEvents cb_sr2_1 As CheckBox
End Class
