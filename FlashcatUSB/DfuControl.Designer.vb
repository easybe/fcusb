<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class DfuControl
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
        Me.lblAvrCrc = New System.Windows.Forms.Label()
        Me.lblAvrRange = New System.Windows.Forms.Label()
        Me.lblAvrFn = New System.Windows.Forms.Label()
        Me.cmdAvrProg = New System.Windows.Forms.Button()
        Me.cmdAvrStart = New System.Windows.Forms.Button()
        Me.cmdAvrLoad = New System.Windows.Forms.Button()
        Me.DfuPbBar = New System.Windows.Forms.ProgressBar()
        Me.AvrEditor = New FlashcatUSB.HexEditor_v2()
        Me.SuspendLayout()
        '
        'lblAvrCrc
        '
        Me.lblAvrCrc.AutoSize = True
        Me.lblAvrCrc.ForeColor = System.Drawing.Color.Gray
        Me.lblAvrCrc.Location = New System.Drawing.Point(351, 4)
        Me.lblAvrCrc.Name = "lblAvrCrc"
        Me.lblAvrCrc.Size = New System.Drawing.Size(82, 13)
        Me.lblAvrCrc.TabIndex = 21
        Me.lblAvrCrc.Text = "CRC: 0x000000"
        '
        'lblAvrRange
        '
        Me.lblAvrRange.AutoSize = True
        Me.lblAvrRange.ForeColor = System.Drawing.Color.Gray
        Me.lblAvrRange.Location = New System.Drawing.Point(232, 4)
        Me.lblAvrRange.Name = "lblAvrRange"
        Me.lblAvrRange.Size = New System.Drawing.Size(118, 13)
        Me.lblAvrRange.TabIndex = 20
        Me.lblAvrRange.Text = "Range: 0x0000-0x0000"
        '
        'lblAvrFn
        '
        Me.lblAvrFn.AutoSize = True
        Me.lblAvrFn.Location = New System.Drawing.Point(5, 4)
        Me.lblAvrFn.Name = "lblAvrFn"
        Me.lblAvrFn.Size = New System.Drawing.Size(135, 13)
        Me.lblAvrFn.TabIndex = 19
        Me.lblAvrFn.Text = "File: no file currently loaded"
        '
        'cmdAvrProg
        '
        Me.cmdAvrProg.Location = New System.Drawing.Point(153, 22)
        Me.cmdAvrProg.Name = "cmdAvrProg"
        Me.cmdAvrProg.Size = New System.Drawing.Size(112, 22)
        Me.cmdAvrProg.TabIndex = 16
        Me.cmdAvrProg.Text = "Program"
        Me.cmdAvrProg.UseVisualStyleBackColor = True
        '
        'cmdAvrStart
        '
        Me.cmdAvrStart.Location = New System.Drawing.Point(297, 23)
        Me.cmdAvrStart.Name = "cmdAvrStart"
        Me.cmdAvrStart.Size = New System.Drawing.Size(136, 22)
        Me.cmdAvrStart.TabIndex = 18
        Me.cmdAvrStart.Text = "Start Application"
        Me.cmdAvrStart.UseVisualStyleBackColor = True
        '
        'cmdAvrLoad
        '
        Me.cmdAvrLoad.Location = New System.Drawing.Point(8, 23)
        Me.cmdAvrLoad.Name = "cmdAvrLoad"
        Me.cmdAvrLoad.Size = New System.Drawing.Size(113, 22)
        Me.cmdAvrLoad.TabIndex = 17
        Me.cmdAvrLoad.Text = "Load File"
        Me.cmdAvrLoad.UseVisualStyleBackColor = True
        '
        'DfuPbBar
        '
        Me.DfuPbBar.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DfuPbBar.Location = New System.Drawing.Point(3, 216)
        Me.DfuPbBar.Name = "DfuPbBar"
        Me.DfuPbBar.Size = New System.Drawing.Size(430, 12)
        Me.DfuPbBar.TabIndex = 23
        '
        'AvrEditor
        '
        Me.AvrEditor.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.AvrEditor.BaseOffset = CType(0UI, UInteger)
        Me.AvrEditor.BaseSize = CType(0UI, UInteger)
        Me.AvrEditor.Location = New System.Drawing.Point(3, 48)
        Me.AvrEditor.Name = "AvrEditor"
        Me.AvrEditor.Size = New System.Drawing.Size(430, 162)
        Me.AvrEditor.TabIndex = 22
        Me.AvrEditor.TopAddress = CType(0UI, UInteger)
        '
        'DfuControl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.DfuPbBar)
        Me.Controls.Add(Me.AvrEditor)
        Me.Controls.Add(Me.lblAvrCrc)
        Me.Controls.Add(Me.lblAvrRange)
        Me.Controls.Add(Me.lblAvrFn)
        Me.Controls.Add(Me.cmdAvrProg)
        Me.Controls.Add(Me.cmdAvrStart)
        Me.Controls.Add(Me.cmdAvrLoad)
        Me.Name = "DfuControl"
        Me.Size = New System.Drawing.Size(440, 231)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents AvrEditor As HexEditor_v2
    Friend WithEvents lblAvrCrc As Label
    Friend WithEvents lblAvrRange As Label
    Friend WithEvents lblAvrFn As Label
    Friend WithEvents cmdAvrProg As Button
    Friend WithEvents cmdAvrStart As Button
    Friend WithEvents cmdAvrLoad As Button
    Friend WithEvents DfuPbBar As ProgressBar
End Class
