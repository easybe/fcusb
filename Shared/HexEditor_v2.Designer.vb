<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class HexEditor_v2
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
        Me.MainBox = New System.Windows.Forms.GroupBox()
        Me.ScrollBar = New System.Windows.Forms.VScrollBar()
        Me.PB = New System.Windows.Forms.PictureBox()
        Me.MainBox.SuspendLayout()
        CType(Me.PB, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'MainBox
        '
        Me.MainBox.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.MainBox.Controls.Add(Me.ScrollBar)
        Me.MainBox.Controls.Add(Me.PB)
        Me.MainBox.Location = New System.Drawing.Point(3, -2)
        Me.MainBox.Name = "MainBox"
        Me.MainBox.Size = New System.Drawing.Size(228, 128)
        Me.MainBox.TabIndex = 1
        Me.MainBox.TabStop = False
        '
        'ScrollBar
        '
        Me.ScrollBar.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ScrollBar.Enabled = False
        Me.ScrollBar.Location = New System.Drawing.Point(210, 6)
        Me.ScrollBar.Name = "ScrollBar"
        Me.ScrollBar.Size = New System.Drawing.Size(16, 120)
        Me.ScrollBar.TabIndex = 1
        '
        'PB
        '
        Me.PB.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.PB.BackColor = System.Drawing.SystemColors.Control
        Me.PB.Location = New System.Drawing.Point(2, 7)
        Me.PB.Name = "PB"
        Me.PB.Size = New System.Drawing.Size(224, 119)
        Me.PB.TabIndex = 0
        Me.PB.TabStop = False
        '
        'HexEditor_v2
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.MainBox)
        Me.Name = "HexEditor_v2"
        Me.Size = New System.Drawing.Size(234, 128)
        Me.MainBox.ResumeLayout(False)
        CType(Me.PB, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents MainBox As GroupBox
    Friend WithEvents ScrollBar As VScrollBar
    Friend WithEvents PB As PictureBox
End Class
