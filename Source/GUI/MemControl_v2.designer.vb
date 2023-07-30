<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MemControl_v2
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.pbar = New System.Windows.Forms.ProgressBar()
        Me.gb_flash = New System.Windows.Forms.GroupBox()
        Me.txtAddress = New System.Windows.Forms.TextBox()
        Me.IO_Control = New FlashcatUSB.MemIOControl()
        Me.HexEditor64 = New FlashcatUSB.HexEditor_v2()
        Me.gb_flash.SuspendLayout()
        Me.SuspendLayout()
        '
        'pbar
        '
        Me.pbar.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pbar.Location = New System.Drawing.Point(8, 48)
        Me.pbar.Name = "pbar"
        Me.pbar.Size = New System.Drawing.Size(359, 12)
        Me.pbar.TabIndex = 16
        '
        'gb_flash
        '
        Me.gb_flash.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.gb_flash.Controls.Add(Me.IO_Control)
        Me.gb_flash.Controls.Add(Me.HexEditor64)
        Me.gb_flash.Controls.Add(Me.txtAddress)
        Me.gb_flash.Controls.Add(Me.pbar)
        Me.gb_flash.Location = New System.Drawing.Point(2, 4)
        Me.gb_flash.Name = "gb_flash"
        Me.gb_flash.Size = New System.Drawing.Size(377, 211)
        Me.gb_flash.TabIndex = 20
        Me.gb_flash.TabStop = False
        Me.gb_flash.Text = "(FLASH_NAME PART_NUMBER)"
        '
        'txtAddress
        '
        Me.txtAddress.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtAddress.Location = New System.Drawing.Point(275, 22)
        Me.txtAddress.Name = "txtAddress"
        Me.txtAddress.Size = New System.Drawing.Size(92, 20)
        Me.txtAddress.TabIndex = 40
        '
        'IO_Control
        '
        Me.IO_Control.AllowEdit = True
        Me.IO_Control.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.IO_Control.FlashAvailable = CType(0, Long)
        Me.IO_Control.FlashName = Nothing
        Me.IO_Control.Location = New System.Drawing.Point(6, 16)
        Me.IO_Control.MemoryArea = FlashcatUSB.FlashMemory.FlashArea.Main
        Me.IO_Control.Name = "IO_Control"
        Me.IO_Control.Size = New System.Drawing.Size(264, 28)
        Me.IO_Control.SREC_DATAWIDTH = FlashcatUSB.SREC.RECORD_DATAWIDTH.[BYTE]
        Me.IO_Control.TabIndex = 41
        '
        'HexEditor64
        '
        Me.HexEditor64.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.HexEditor64.BaseOffset = CType(0, Long)
        Me.HexEditor64.BaseSize = CType(0, Long)
        Me.HexEditor64.EDIT_MODE = False
        Me.HexEditor64.HexDataByteSize = 0
        Me.HexEditor64.Location = New System.Drawing.Point(4, 66)
        Me.HexEditor64.Margin = New System.Windows.Forms.Padding(4)
        Me.HexEditor64.Name = "HexEditor64"
        Me.HexEditor64.Size = New System.Drawing.Size(368, 139)
        Me.HexEditor64.TabIndex = 24
        Me.HexEditor64.TopAddress = CType(0, Long)
        '
        'MemControl_v2
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.gb_flash)
        Me.Name = "MemControl_v2"
        Me.Size = New System.Drawing.Size(379, 218)
        Me.gb_flash.ResumeLayout(False)
        Me.gb_flash.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents pbar As ProgressBar
    Friend WithEvents gb_flash As GroupBox
    Friend WithEvents txtAddress As TextBox
    Friend WithEvents HexEditor64 As HexEditor_v2
    Friend WithEvents IO_Control As MemIOControl
End Class
