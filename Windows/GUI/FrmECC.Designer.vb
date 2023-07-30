<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FrmECC
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
        Me.cbAlgorithm = New System.Windows.Forms.ComboBox()
        Me.lbl_nandecc_algorithm = New System.Windows.Forms.Label()
        Me.cb_rs_reverse_data = New System.Windows.Forms.CheckBox()
        Me.cb_sym_width = New System.Windows.Forms.ComboBox()
        Me.lbl_sym_width = New System.Windows.Forms.Label()
        Me.cb_ECC_BITERR = New System.Windows.Forms.ComboBox()
        Me.lbl_nandecc_biterror = New System.Windows.Forms.Label()
        Me.cbPageSize = New System.Windows.Forms.ComboBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.cbSpareSize = New System.Windows.Forms.ComboBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.cmdCancel = New System.Windows.Forms.Button()
        Me.cmdOkay = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'cbAlgorithm
        '
        Me.cbAlgorithm.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbAlgorithm.FormattingEnabled = True
        Me.cbAlgorithm.Items.AddRange(New Object() {"Hamming", "Reed-Solomon", "Binary BHC"})
        Me.cbAlgorithm.Location = New System.Drawing.Point(200, 30)
        Me.cbAlgorithm.Name = "cbAlgorithm"
        Me.cbAlgorithm.Size = New System.Drawing.Size(107, 21)
        Me.cbAlgorithm.TabIndex = 45
        '
        'lbl_nandecc_algorithm
        '
        Me.lbl_nandecc_algorithm.AutoSize = True
        Me.lbl_nandecc_algorithm.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbl_nandecc_algorithm.Location = New System.Drawing.Point(197, 13)
        Me.lbl_nandecc_algorithm.Name = "lbl_nandecc_algorithm"
        Me.lbl_nandecc_algorithm.Size = New System.Drawing.Size(59, 13)
        Me.lbl_nandecc_algorithm.TabIndex = 46
        Me.lbl_nandecc_algorithm.Text = "Algorithm"
        '
        'cb_rs_reverse_data
        '
        Me.cb_rs_reverse_data.AutoSize = True
        Me.cb_rs_reverse_data.Location = New System.Drawing.Point(210, 86)
        Me.cb_rs_reverse_data.Name = "cb_rs_reverse_data"
        Me.cb_rs_reverse_data.Size = New System.Drawing.Size(116, 17)
        Me.cb_rs_reverse_data.TabIndex = 43
        Me.cb_rs_reverse_data.Text = "Reverse byte order"
        Me.cb_rs_reverse_data.UseVisualStyleBackColor = True
        '
        'cb_sym_width
        '
        Me.cb_sym_width.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_sym_width.FormattingEnabled = True
        Me.cb_sym_width.Items.AddRange(New Object() {"9-bit", "10-bit"})
        Me.cb_sym_width.Location = New System.Drawing.Point(114, 84)
        Me.cb_sym_width.Name = "cb_sym_width"
        Me.cb_sym_width.Size = New System.Drawing.Size(80, 21)
        Me.cb_sym_width.TabIndex = 41
        '
        'lbl_sym_width
        '
        Me.lbl_sym_width.AutoSize = True
        Me.lbl_sym_width.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbl_sym_width.Location = New System.Drawing.Point(111, 67)
        Me.lbl_sym_width.Name = "lbl_sym_width"
        Me.lbl_sym_width.Size = New System.Drawing.Size(81, 13)
        Me.lbl_sym_width.TabIndex = 42
        Me.lbl_sym_width.Text = "Symbol width"
        '
        'cb_ECC_BITERR
        '
        Me.cb_ECC_BITERR.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cb_ECC_BITERR.FormattingEnabled = True
        Me.cb_ECC_BITERR.Items.AddRange(New Object() {"1-bit", "2-bit", "4-bit", "8-bit", "10-bit", "14-bit"})
        Me.cb_ECC_BITERR.Location = New System.Drawing.Point(28, 84)
        Me.cb_ECC_BITERR.Name = "cb_ECC_BITERR"
        Me.cb_ECC_BITERR.Size = New System.Drawing.Size(80, 21)
        Me.cb_ECC_BITERR.TabIndex = 39
        '
        'lbl_nandecc_biterror
        '
        Me.lbl_nandecc_biterror.AutoSize = True
        Me.lbl_nandecc_biterror.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbl_nandecc_biterror.Location = New System.Drawing.Point(25, 67)
        Me.lbl_nandecc_biterror.Name = "lbl_nandecc_biterror"
        Me.lbl_nandecc_biterror.Size = New System.Drawing.Size(52, 13)
        Me.lbl_nandecc_biterror.TabIndex = 40
        Me.lbl_nandecc_biterror.Text = "Bit-error"
        '
        'cbPageSize
        '
        Me.cbPageSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbPageSize.FormattingEnabled = True
        Me.cbPageSize.Items.AddRange(New Object() {"512", "2048", "4096", "8192"})
        Me.cbPageSize.Location = New System.Drawing.Point(28, 30)
        Me.cbPageSize.Name = "cbPageSize"
        Me.cbPageSize.Size = New System.Drawing.Size(80, 21)
        Me.cbPageSize.TabIndex = 47
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.Location = New System.Drawing.Point(25, 13)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(62, 13)
        Me.Label1.TabIndex = 48
        Me.Label1.Text = "Page size"
        '
        'cbSpareSize
        '
        Me.cbSpareSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbSpareSize.FormattingEnabled = True
        Me.cbSpareSize.Items.AddRange(New Object() {"16", "64", "112", "128", "224", "232", "256", "436", "448", "512", "640", "744", "1024"})
        Me.cbSpareSize.Location = New System.Drawing.Point(114, 30)
        Me.cbSpareSize.Name = "cbSpareSize"
        Me.cbSpareSize.Size = New System.Drawing.Size(80, 21)
        Me.cbSpareSize.TabIndex = 49
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.Location = New System.Drawing.Point(111, 13)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(66, 13)
        Me.Label2.TabIndex = 50
        Me.Label2.Text = "Spare size"
        '
        'cmdCancel
        '
        Me.cmdCancel.Location = New System.Drawing.Point(103, 122)
        Me.cmdCancel.Name = "cmdCancel"
        Me.cmdCancel.Size = New System.Drawing.Size(70, 24)
        Me.cmdCancel.TabIndex = 51
        Me.cmdCancel.Text = "Cancel"
        Me.cmdCancel.UseVisualStyleBackColor = True
        '
        'cmdOkay
        '
        Me.cmdOkay.Location = New System.Drawing.Point(179, 122)
        Me.cmdOkay.Name = "cmdOkay"
        Me.cmdOkay.Size = New System.Drawing.Size(70, 24)
        Me.cmdOkay.TabIndex = 52
        Me.cmdOkay.Text = "Okay"
        Me.cmdOkay.UseVisualStyleBackColor = True
        '
        'FrmECC
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(352, 157)
        Me.Controls.Add(Me.cmdOkay)
        Me.Controls.Add(Me.cmdCancel)
        Me.Controls.Add(Me.cbSpareSize)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.cbPageSize)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.cbAlgorithm)
        Me.Controls.Add(Me.lbl_nandecc_algorithm)
        Me.Controls.Add(Me.cb_rs_reverse_data)
        Me.Controls.Add(Me.cb_sym_width)
        Me.Controls.Add(Me.lbl_sym_width)
        Me.Controls.Add(Me.cb_ECC_BITERR)
        Me.Controls.Add(Me.lbl_nandecc_biterror)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.Name = "FrmECC"
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "ECC Settings"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents cbAlgorithm As ComboBox
    Friend WithEvents lbl_nandecc_algorithm As Label
    Friend WithEvents cb_rs_reverse_data As CheckBox
    Friend WithEvents cb_sym_width As ComboBox
    Friend WithEvents lbl_sym_width As Label
    Friend WithEvents cb_ECC_BITERR As ComboBox
    Friend WithEvents lbl_nandecc_biterror As Label
    Friend WithEvents cbPageSize As ComboBox
    Friend WithEvents Label1 As Label
    Friend WithEvents cbSpareSize As ComboBox
    Friend WithEvents Label2 As Label
    Friend WithEvents cmdCancel As Button
    Friend WithEvents cmdOkay As Button
End Class
