<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class vendor_at45
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
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.rb1_528 = New System.Windows.Forms.RadioButton()
        Me.rb2_512 = New System.Windows.Forms.RadioButton()
        Me.cmdSave = New System.Windows.Forms.Button()
        Me.lblInfo = New System.Windows.Forms.Label()
        Me.GroupBox1.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.rb2_512)
        Me.GroupBox1.Controls.Add(Me.rb1_528)
        Me.GroupBox1.Location = New System.Drawing.Point(6, 3)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(305, 64)
        Me.GroupBox1.TabIndex = 82
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Page Size Configuration"
        '
        'rb1_528
        '
        Me.rb1_528.AutoSize = True
        Me.rb1_528.Location = New System.Drawing.Point(6, 33)
        Me.rb1_528.Name = "rb1_528"
        Me.rb1_528.Size = New System.Drawing.Size(72, 17)
        Me.rb1_528.TabIndex = 83
        Me.rb1_528.TabStop = True
        Me.rb1_528.Text = "528 Bytes"
        Me.rb1_528.UseVisualStyleBackColor = True
        '
        'rb2_512
        '
        Me.rb2_512.AutoSize = True
        Me.rb2_512.Location = New System.Drawing.Point(106, 33)
        Me.rb2_512.Name = "rb2_512"
        Me.rb2_512.Size = New System.Drawing.Size(72, 17)
        Me.rb2_512.TabIndex = 84
        Me.rb2_512.TabStop = True
        Me.rb2_512.Text = "512 Bytes"
        Me.rb2_512.UseVisualStyleBackColor = True
        '
        'cmdSave
        '
        Me.cmdSave.Location = New System.Drawing.Point(223, 155)
        Me.cmdSave.Name = "cmdSave"
        Me.cmdSave.Size = New System.Drawing.Size(73, 26)
        Me.cmdSave.TabIndex = 83
        Me.cmdSave.Text = "Save"
        Me.cmdSave.UseVisualStyleBackColor = True
        '
        'lblInfo
        '
        Me.lblInfo.AutoSize = True
        Me.lblInfo.Location = New System.Drawing.Point(9, 75)
        Me.lblInfo.Name = "lblInfo"
        Me.lblInfo.Size = New System.Drawing.Size(134, 13)
        Me.lblInfo.TabIndex = 84
        Me.lblInfo.Text = "LBL: Information goes here"
        '
        'vendor_at45
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.lblInfo)
        Me.Controls.Add(Me.cmdSave)
        Me.Controls.Add(Me.GroupBox1)
        Me.Name = "vendor_at45"
        Me.Size = New System.Drawing.Size(321, 187)
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents rb2_512 As RadioButton
    Friend WithEvents rb1_528 As RadioButton
    Friend WithEvents cmdSave As Button
    Friend WithEvents lblInfo As Label
End Class
