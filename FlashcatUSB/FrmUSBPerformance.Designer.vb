<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FrmUSBPerformance
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
        Me.Label1 = New System.Windows.Forms.Label()
        Me.lblreadspeed = New System.Windows.Forms.Label()
        Me.cmdstart = New System.Windows.Forms.Button()
        Me.progress = New System.Windows.Forms.ProgressBar()
        Me.cbMachDownloadSize = New System.Windows.Forms.ComboBox()
        Me.lblwritespeed = New System.Windows.Forms.Label()
        Me.lblstatus = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(29, 23)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(189, 13)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "Perform a large data transfer over USB"
        '
        'lblreadspeed
        '
        Me.lblreadspeed.AutoSize = True
        Me.lblreadspeed.Location = New System.Drawing.Point(52, 94)
        Me.lblreadspeed.Name = "lblreadspeed"
        Me.lblreadspeed.Size = New System.Drawing.Size(70, 13)
        Me.lblreadspeed.TabIndex = 2
        Me.lblreadspeed.Text = "Read Speed:"
        '
        'cmdstart
        '
        Me.cmdstart.Location = New System.Drawing.Point(140, 146)
        Me.cmdstart.Name = "cmdstart"
        Me.cmdstart.Size = New System.Drawing.Size(75, 23)
        Me.cmdstart.TabIndex = 3
        Me.cmdstart.Text = "START"
        Me.cmdstart.UseVisualStyleBackColor = True
        '
        'progress
        '
        Me.progress.Location = New System.Drawing.Point(12, 121)
        Me.progress.Name = "progress"
        Me.progress.Size = New System.Drawing.Size(332, 17)
        Me.progress.TabIndex = 4
        '
        'cbMachDownloadSize
        '
        Me.cbMachDownloadSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbMachDownloadSize.FormattingEnabled = True
        Me.cbMachDownloadSize.Items.AddRange(New Object() {"100MB", "200MB", "300MB", "400MB", "500MB"})
        Me.cbMachDownloadSize.Location = New System.Drawing.Point(243, 20)
        Me.cbMachDownloadSize.Name = "cbMachDownloadSize"
        Me.cbMachDownloadSize.Size = New System.Drawing.Size(84, 21)
        Me.cbMachDownloadSize.TabIndex = 5
        '
        'lblwritespeed
        '
        Me.lblwritespeed.AutoSize = True
        Me.lblwritespeed.Location = New System.Drawing.Point(195, 94)
        Me.lblwritespeed.Name = "lblwritespeed"
        Me.lblwritespeed.Size = New System.Drawing.Size(69, 13)
        Me.lblwritespeed.TabIndex = 6
        Me.lblwritespeed.Text = "Write Speed:"
        '
        'lblstatus
        '
        Me.lblstatus.AutoSize = True
        Me.lblstatus.Location = New System.Drawing.Point(52, 63)
        Me.lblstatus.Name = "lblstatus"
        Me.lblstatus.Size = New System.Drawing.Size(166, 13)
        Me.lblstatus.TabIndex = 7
        Me.lblstatus.Text = "Status: Press start button to begin"
        '
        'FrmUSBPerformance
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(356, 177)
        Me.Controls.Add(Me.lblstatus)
        Me.Controls.Add(Me.lblwritespeed)
        Me.Controls.Add(Me.cbMachDownloadSize)
        Me.Controls.Add(Me.progress)
        Me.Controls.Add(Me.cmdstart)
        Me.Controls.Add(Me.lblreadspeed)
        Me.Controls.Add(Me.Label1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.Name = "FrmUSBPerformance"
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "FlashcatUSB USB Performance"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label1 As Label
    Friend WithEvents lblreadspeed As Label
    Friend WithEvents cmdstart As Button
    Friend WithEvents progress As ProgressBar
    Friend WithEvents cbMachDownloadSize As ComboBox
    Friend WithEvents lblwritespeed As Label
    Friend WithEvents lblstatus As Label
End Class
