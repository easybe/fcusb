<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class vendor_issi
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
        Me.cb_bp2 = New System.Windows.Forms.CheckBox()
        Me.cb_bp1 = New System.Windows.Forms.CheckBox()
        Me.cb_bp0 = New System.Windows.Forms.CheckBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.group_sr1 = New System.Windows.Forms.GroupBox()
        Me.cb_qspi = New System.Windows.Forms.CheckBox()
        Me.cb_bp3 = New System.Windows.Forms.CheckBox()
        Me.cmd_write_config = New System.Windows.Forms.Button()
        Me.cmd_read_config = New System.Windows.Forms.Button()
        Me.group_sr1.SuspendLayout()
        Me.SuspendLayout()
        '
        'cb_bp2
        '
        Me.cb_bp2.AutoSize = True
        Me.cb_bp2.Location = New System.Drawing.Point(57, 45)
        Me.cb_bp2.Name = "cb_bp2"
        Me.cb_bp2.Size = New System.Drawing.Size(46, 17)
        Me.cb_bp2.TabIndex = 4
        Me.cb_bp2.Text = "BP2"
        Me.cb_bp2.UseVisualStyleBackColor = True
        '
        'cb_bp1
        '
        Me.cb_bp1.AutoSize = True
        Me.cb_bp1.Location = New System.Drawing.Point(109, 45)
        Me.cb_bp1.Name = "cb_bp1"
        Me.cb_bp1.Size = New System.Drawing.Size(46, 17)
        Me.cb_bp1.TabIndex = 5
        Me.cb_bp1.Text = "BP1"
        Me.cb_bp1.UseVisualStyleBackColor = True
        '
        'cb_bp0
        '
        Me.cb_bp0.AutoSize = True
        Me.cb_bp0.Location = New System.Drawing.Point(161, 45)
        Me.cb_bp0.Name = "cb_bp0"
        Me.cb_bp0.Size = New System.Drawing.Size(46, 17)
        Me.cb_bp0.TabIndex = 6
        Me.cb_bp0.Text = "BP0"
        Me.cb_bp0.UseVisualStyleBackColor = True
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(6, 28)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(91, 13)
        Me.Label1.TabIndex = 7
        Me.Label1.Text = "Block Protect Bits"
        '
        'group_sr1
        '
        Me.group_sr1.Controls.Add(Me.cb_qspi)
        Me.group_sr1.Controls.Add(Me.cb_bp3)
        Me.group_sr1.Controls.Add(Me.Label1)
        Me.group_sr1.Controls.Add(Me.cb_bp0)
        Me.group_sr1.Controls.Add(Me.cb_bp1)
        Me.group_sr1.Controls.Add(Me.cb_bp2)
        Me.group_sr1.Location = New System.Drawing.Point(3, 3)
        Me.group_sr1.Name = "group_sr1"
        Me.group_sr1.Size = New System.Drawing.Size(333, 111)
        Me.group_sr1.TabIndex = 8
        Me.group_sr1.TabStop = False
        Me.group_sr1.Text = "Non-Volatile Register"
        '
        'cb_qspi
        '
        Me.cb_qspi.AutoSize = True
        Me.cb_qspi.Location = New System.Drawing.Point(9, 84)
        Me.cb_qspi.Name = "cb_qspi"
        Me.cb_qspi.Size = New System.Drawing.Size(88, 17)
        Me.cb_qspi.TabIndex = 8
        Me.cb_qspi.Text = "Quad Enable"
        Me.cb_qspi.UseVisualStyleBackColor = True
        '
        'cb_bp3
        '
        Me.cb_bp3.AutoSize = True
        Me.cb_bp3.Location = New System.Drawing.Point(10, 45)
        Me.cb_bp3.Name = "cb_bp3"
        Me.cb_bp3.Size = New System.Drawing.Size(46, 17)
        Me.cb_bp3.TabIndex = 8
        Me.cb_bp3.Text = "BP3"
        Me.cb_bp3.UseVisualStyleBackColor = True
        '
        'cmd_write_config
        '
        Me.cmd_write_config.Location = New System.Drawing.Point(173, 119)
        Me.cmd_write_config.Name = "cmd_write_config"
        Me.cmd_write_config.Size = New System.Drawing.Size(75, 23)
        Me.cmd_write_config.TabIndex = 11
        Me.cmd_write_config.Text = "Write"
        Me.cmd_write_config.UseVisualStyleBackColor = True
        '
        'cmd_read_config
        '
        Me.cmd_read_config.Location = New System.Drawing.Point(254, 119)
        Me.cmd_read_config.Name = "cmd_read_config"
        Me.cmd_read_config.Size = New System.Drawing.Size(75, 23)
        Me.cmd_read_config.TabIndex = 10
        Me.cmd_read_config.Text = "Read"
        Me.cmd_read_config.UseVisualStyleBackColor = True
        '
        'vendor_issi
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.cmd_write_config)
        Me.Controls.Add(Me.group_sr1)
        Me.Controls.Add(Me.cmd_read_config)
        Me.Name = "vendor_issi"
        Me.Size = New System.Drawing.Size(341, 146)
        Me.group_sr1.ResumeLayout(False)
        Me.group_sr1.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents cb_bp2 As CheckBox
    Friend WithEvents cb_bp1 As CheckBox
    Friend WithEvents cb_bp0 As CheckBox
    Friend WithEvents Label1 As Label
    Friend WithEvents group_sr1 As GroupBox
    Friend WithEvents cmd_write_config As Button
    Friend WithEvents cmd_read_config As Button
    Friend WithEvents cb_qspi As CheckBox
    Friend WithEvents cb_bp3 As CheckBox
End Class
