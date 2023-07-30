<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class NAND_Block_Management
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(NAND_Block_Management))
        Me.cmdClose = New System.Windows.Forms.Button()
        Me.cmdAnalyze = New System.Windows.Forms.Button()
        Me.lbl_desc = New System.Windows.Forms.Label()
        Me.StatusStrip1 = New System.Windows.Forms.StatusStrip()
        Me.MyStatus = New System.Windows.Forms.ToolStripStatusLabel()
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.BlockMap = New System.Windows.Forms.PictureBox()
        Me.lbl_no_error = New System.Windows.Forms.Label()
        Me.lbl_bad_block = New System.Windows.Forms.Label()
        Me.lbl_user_marked = New System.Windows.Forms.Label()
        Me.lbl_write_error = New System.Windows.Forms.Label()
        Me.PictureBox4 = New System.Windows.Forms.PictureBox()
        Me.PictureBox3 = New System.Windows.Forms.PictureBox()
        Me.PictureBox2 = New System.Windows.Forms.PictureBox()
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
        Me.cb_write_bad_block_marker = New System.Windows.Forms.CheckBox()
        Me.StatusStrip1.SuspendLayout()
        Me.Panel1.SuspendLayout()
        CType(Me.BlockMap, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PictureBox4, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PictureBox3, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PictureBox2, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'cmdClose
        '
        Me.cmdClose.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdClose.Location = New System.Drawing.Point(460, 356)
        Me.cmdClose.Name = "cmdClose"
        Me.cmdClose.Size = New System.Drawing.Size(75, 23)
        Me.cmdClose.TabIndex = 1
        Me.cmdClose.Text = "Close"
        Me.cmdClose.UseVisualStyleBackColor = True
        '
        'cmdAnalyze
        '
        Me.cmdAnalyze.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAnalyze.Location = New System.Drawing.Point(5, 356)
        Me.cmdAnalyze.Name = "cmdAnalyze"
        Me.cmdAnalyze.Size = New System.Drawing.Size(75, 23)
        Me.cmdAnalyze.TabIndex = 2
        Me.cmdAnalyze.Text = "Analyze"
        Me.cmdAnalyze.UseVisualStyleBackColor = True
        '
        'lbl_desc
        '
        Me.lbl_desc.AutoSize = True
        Me.lbl_desc.Location = New System.Drawing.Point(12, 9)
        Me.lbl_desc.Name = "lbl_desc"
        Me.lbl_desc.Size = New System.Drawing.Size(92, 13)
        Me.lbl_desc.TabIndex = 3
        Me.lbl_desc.Text = "NAND Block Map"
        '
        'StatusStrip1
        '
        Me.StatusStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.MyStatus})
        Me.StatusStrip1.Location = New System.Drawing.Point(0, 384)
        Me.StatusStrip1.Name = "StatusStrip1"
        Me.StatusStrip1.Size = New System.Drawing.Size(540, 22)
        Me.StatusStrip1.TabIndex = 4
        Me.StatusStrip1.Text = "StatusStrip1"
        '
        'MyStatus
        '
        Me.MyStatus.Name = "MyStatus"
        Me.MyStatus.Size = New System.Drawing.Size(55, 17)
        Me.MyStatus.Text = "NO TEXT"
        '
        'Panel1
        '
        Me.Panel1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Panel1.AutoScroll = True
        Me.Panel1.Controls.Add(Me.BlockMap)
        Me.Panel1.Location = New System.Drawing.Point(5, 27)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(530, 288)
        Me.Panel1.TabIndex = 5
        '
        'BlockMap
        '
        Me.BlockMap.Location = New System.Drawing.Point(3, 3)
        Me.BlockMap.Name = "BlockMap"
        Me.BlockMap.Size = New System.Drawing.Size(483, 281)
        Me.BlockMap.TabIndex = 0
        Me.BlockMap.TabStop = False
        '
        'lbl_no_error
        '
        Me.lbl_no_error.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lbl_no_error.AutoSize = True
        Me.lbl_no_error.Location = New System.Drawing.Point(91, 332)
        Me.lbl_no_error.Name = "lbl_no_error"
        Me.lbl_no_error.Size = New System.Drawing.Size(45, 13)
        Me.lbl_no_error.TabIndex = 10
        Me.lbl_no_error.Text = "No error"
        '
        'lbl_bad_block
        '
        Me.lbl_bad_block.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lbl_bad_block.AutoSize = True
        Me.lbl_bad_block.Location = New System.Drawing.Point(164, 332)
        Me.lbl_bad_block.Name = "lbl_bad_block"
        Me.lbl_bad_block.Size = New System.Drawing.Size(90, 13)
        Me.lbl_bad_block.TabIndex = 11
        Me.lbl_bad_block.Text = "Bad block marker"
        '
        'lbl_user_marked
        '
        Me.lbl_user_marked.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lbl_user_marked.AutoSize = True
        Me.lbl_user_marked.Location = New System.Drawing.Point(285, 332)
        Me.lbl_user_marked.Name = "lbl_user_marked"
        Me.lbl_user_marked.Size = New System.Drawing.Size(67, 13)
        Me.lbl_user_marked.TabIndex = 12
        Me.lbl_user_marked.Text = "User marked"
        '
        'lbl_write_error
        '
        Me.lbl_write_error.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lbl_write_error.AutoSize = True
        Me.lbl_write_error.Location = New System.Drawing.Point(383, 332)
        Me.lbl_write_error.Name = "lbl_write_error"
        Me.lbl_write_error.Size = New System.Drawing.Size(56, 13)
        Me.lbl_write_error.TabIndex = 13
        Me.lbl_write_error.Text = "Write error"
        '
        'PictureBox4
        '
        Me.PictureBox4.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.PictureBox4.BackgroundImage = Global.FlashcatUSB.My.Resources.Resources.BLOCK_RED
        Me.PictureBox4.Location = New System.Drawing.Point(363, 331)
        Me.PictureBox4.Name = "PictureBox4"
        Me.PictureBox4.Size = New System.Drawing.Size(14, 14)
        Me.PictureBox4.TabIndex = 9
        Me.PictureBox4.TabStop = False
        '
        'PictureBox3
        '
        Me.PictureBox3.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.PictureBox3.BackgroundImage = Global.FlashcatUSB.My.Resources.Resources.BLOCK_BLUE
        Me.PictureBox3.Location = New System.Drawing.Point(265, 331)
        Me.PictureBox3.Name = "PictureBox3"
        Me.PictureBox3.Size = New System.Drawing.Size(14, 14)
        Me.PictureBox3.TabIndex = 8
        Me.PictureBox3.TabStop = False
        '
        'PictureBox2
        '
        Me.PictureBox2.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.PictureBox2.BackgroundImage = Global.FlashcatUSB.My.Resources.Resources.BLOCK_BLACK
        Me.PictureBox2.Location = New System.Drawing.Point(144, 332)
        Me.PictureBox2.Name = "PictureBox2"
        Me.PictureBox2.Size = New System.Drawing.Size(14, 14)
        Me.PictureBox2.TabIndex = 7
        Me.PictureBox2.TabStop = False
        '
        'PictureBox1
        '
        Me.PictureBox1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.PictureBox1.BackgroundImage = Global.FlashcatUSB.My.Resources.Resources.BLOCK_GREEN
        Me.PictureBox1.Location = New System.Drawing.Point(71, 331)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(14, 14)
        Me.PictureBox1.TabIndex = 6
        Me.PictureBox1.TabStop = False
        '
        'cb_write_bad_block_marker
        '
        Me.cb_write_bad_block_marker.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.cb_write_bad_block_marker.AutoSize = True
        Me.cb_write_bad_block_marker.Checked = True
        Me.cb_write_bad_block_marker.CheckState = System.Windows.Forms.CheckState.Checked
        Me.cb_write_bad_block_marker.Location = New System.Drawing.Point(94, 360)
        Me.cb_write_bad_block_marker.Name = "cb_write_bad_block_marker"
        Me.cb_write_bad_block_marker.Size = New System.Drawing.Size(219, 17)
        Me.cb_write_bad_block_marker.TabIndex = 14
        Me.cb_write_bad_block_marker.Text = "Write BAD BLOCK markers to spare area"
        Me.cb_write_bad_block_marker.UseVisualStyleBackColor = True
        '
        'NAND_Block_Management
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(540, 406)
        Me.ControlBox = False
        Me.Controls.Add(Me.cb_write_bad_block_marker)
        Me.Controls.Add(Me.lbl_write_error)
        Me.Controls.Add(Me.lbl_user_marked)
        Me.Controls.Add(Me.lbl_bad_block)
        Me.Controls.Add(Me.lbl_no_error)
        Me.Controls.Add(Me.PictureBox4)
        Me.Controls.Add(Me.PictureBox3)
        Me.Controls.Add(Me.PictureBox2)
        Me.Controls.Add(Me.PictureBox1)
        Me.Controls.Add(Me.Panel1)
        Me.Controls.Add(Me.StatusStrip1)
        Me.Controls.Add(Me.lbl_desc)
        Me.Controls.Add(Me.cmdAnalyze)
        Me.Controls.Add(Me.cmdClose)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "NAND_Block_Management"
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "NAND Block Management"
        Me.StatusStrip1.ResumeLayout(False)
        Me.StatusStrip1.PerformLayout()
        Me.Panel1.ResumeLayout(False)
        CType(Me.BlockMap, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PictureBox4, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PictureBox3, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PictureBox2, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents BlockMap As PictureBox
    Friend WithEvents cmdClose As Button
    Friend WithEvents cmdAnalyze As Button
    Friend WithEvents lbl_desc As Label
    Friend WithEvents StatusStrip1 As StatusStrip
    Friend WithEvents Panel1 As Panel
    Friend WithEvents MyStatus As ToolStripStatusLabel
    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents PictureBox2 As PictureBox
    Friend WithEvents PictureBox3 As PictureBox
    Friend WithEvents PictureBox4 As PictureBox
    Friend WithEvents lbl_no_error As Label
    Friend WithEvents lbl_bad_block As Label
    Friend WithEvents lbl_user_marked As Label
    Friend WithEvents lbl_write_error As Label
    Friend WithEvents cb_write_bad_block_marker As CheckBox
End Class
