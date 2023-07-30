<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FrmRangeForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
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
        Me.cmd_okay = New System.Windows.Forms.Button()
        Me.cmd_cancel = New System.Windows.Forms.Button()
        Me.lbl_base = New System.Windows.Forms.Label()
        Me.lbl_length = New System.Windows.Forms.Label()
        Me.txtBaseOffset = New System.Windows.Forms.TextBox()
        Me.txtLength = New System.Windows.Forms.TextBox()
        Me.pb_background = New System.Windows.Forms.PictureBox()
        CType(Me.pb_background, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'cmd_okay
        '
        Me.cmd_okay.Location = New System.Drawing.Point(86, 49)
        Me.cmd_okay.Name = "cmd_okay"
        Me.cmd_okay.Size = New System.Drawing.Size(70, 22)
        Me.cmd_okay.TabIndex = 0
        Me.cmd_okay.Text = "OK"
        Me.cmd_okay.UseVisualStyleBackColor = True
        '
        'cmd_cancel
        '
        Me.cmd_cancel.Location = New System.Drawing.Point(9, 49)
        Me.cmd_cancel.Name = "cmd_cancel"
        Me.cmd_cancel.Size = New System.Drawing.Size(70, 22)
        Me.cmd_cancel.TabIndex = 1
        Me.cmd_cancel.Text = "Cancel"
        Me.cmd_cancel.UseVisualStyleBackColor = True
        '
        'lbl_base
        '
        Me.lbl_base.AutoSize = True
        Me.lbl_base.Location = New System.Drawing.Point(6, 5)
        Me.lbl_base.Name = "lbl_base"
        Me.lbl_base.Size = New System.Drawing.Size(72, 13)
        Me.lbl_base.TabIndex = 2
        Me.lbl_base.Text = "Base Address"
        '
        'lbl_length
        '
        Me.lbl_length.AutoSize = True
        Me.lbl_length.Location = New System.Drawing.Point(101, 5)
        Me.lbl_length.Name = "lbl_length"
        Me.lbl_length.Size = New System.Drawing.Size(40, 13)
        Me.lbl_length.TabIndex = 3
        Me.lbl_length.Text = "Length"
        '
        'txtBase
        '
        Me.txtBaseOffset.Location = New System.Drawing.Point(8, 23)
        Me.txtBaseOffset.Name = "txtBase"
        Me.txtBaseOffset.Size = New System.Drawing.Size(70, 20)
        Me.txtBaseOffset.TabIndex = 4
        '
        'txtLength
        '
        Me.txtLength.Location = New System.Drawing.Point(88, 23)
        Me.txtLength.Name = "txtLength"
        Me.txtLength.Size = New System.Drawing.Size(70, 20)
        Me.txtLength.TabIndex = 5
        '
        'pb_background
        '
        Me.pb_background.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.pb_background.Location = New System.Drawing.Point(0, 0)
        Me.pb_background.Name = "pb_background"
        Me.pb_background.Size = New System.Drawing.Size(167, 77)
        Me.pb_background.TabIndex = 6
        Me.pb_background.TabStop = False
        '
        'FrmRangeForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(167, 77)
        Me.ControlBox = False
        Me.Controls.Add(Me.txtLength)
        Me.Controls.Add(Me.txtBaseOffset)
        Me.Controls.Add(Me.lbl_length)
        Me.Controls.Add(Me.lbl_base)
        Me.Controls.Add(Me.cmd_cancel)
        Me.Controls.Add(Me.cmd_okay)
        Me.Controls.Add(Me.pb_background)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Name = "FrmRangeForm"
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.Text = "FrmRangeForm"
        CType(Me.pb_background, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents cmd_okay As Button
    Friend WithEvents cmd_cancel As Button
    Friend WithEvents lbl_base As Label
    Friend WithEvents lbl_length As Label
    Friend WithEvents txtBaseOffset As TextBox
    Friend WithEvents txtLength As TextBox
    Friend WithEvents pb_background As PictureBox
End Class
