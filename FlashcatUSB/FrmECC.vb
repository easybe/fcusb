Public Class FrmECC

    Public MyConfiguration As ECC_LIB.ECC_Configuration_Entry

    Sub New(cfg As ECC_LIB.ECC_Configuration_Entry)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Me.MyConfiguration = cfg
    End Sub

    Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

        MyConfiguration = New ECC_LIB.ECC_Configuration_Entry
        MyConfiguration.Algorithm = ECC_LIB.ecc_algorithum.reedsolomon
        MyConfiguration.PageSize = 2048
        MyConfiguration.SpareSize = 64
        MyConfiguration.BitError = 4
        MyConfiguration.SymSize = 9
        MyConfiguration.ReverseData = False
    End Sub

    Private Sub FrmECC_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        lbl_nandecc_algorithm.Text = RM.GetString("nandecc_algorithm") '"Algorithm"
        lbl_nandecc_biterror.Text = RM.GetString("nandecc_biterror") '"Bit-error"
        cb_rs_reverse_data.Text = RM.GetString("nandecc_revbyteorder") '"Reverse byte order"
        lbl_sym_width.Text = RM.GetString("nandecc_symwidth") '"Symbol width"
        cbAlgorithm.SelectedIndex = MyConfiguration.Algorithm
        For i = 0 To cbPageSize.Items.Count - 1
            If cbPageSize.Items(i).Equals(MyConfiguration.PageSize.ToString) Then
                cbPageSize.SelectedIndex = i
                Exit For
            End If
        Next
        If cbPageSize.SelectedIndex = -1 Then cbPageSize.SelectedIndex = 0
        For i = 0 To cbSpareSize.Items.Count - 1
            If cbSpareSize.Items(i).Equals(MyConfiguration.SpareSize.ToString) Then
                cbSpareSize.SelectedIndex = i
                Exit For
            End If
        Next
        If cbSpareSize.SelectedIndex = -1 Then cbSpareSize.SelectedIndex = 0
        Select Case MyConfiguration.BitError
            Case 1
                cb_ECC_BITERR.SelectedIndex = 0
            Case 2
                cb_ECC_BITERR.SelectedIndex = 1
            Case 4
                cb_ECC_BITERR.SelectedIndex = 2
            Case 8
                cb_ECC_BITERR.SelectedIndex = 3
            Case 10
                cb_ECC_BITERR.SelectedIndex = 4
            Case 14
                cb_ECC_BITERR.SelectedIndex = 5
            Case Else
                cb_ECC_BITERR.SelectedIndex = 1
        End Select
        Select Case MyConfiguration.SymSize
            Case 9
                cb_sym_width.SelectedIndex = 0
            Case 10
                cb_sym_width.SelectedIndex = 1
            Case Else
                cb_sym_width.SelectedIndex = 0
        End Select
        cb_rs_reverse_data.Checked = MyConfiguration.ReverseData
        cmdOkay.Select()
        cmdOkay.Focus()
    End Sub

    Private Sub cmdCancel_Click(sender As Object, e As EventArgs) Handles cmdCancel.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub cmdOkay_Click(sender As Object, e As EventArgs) Handles cmdOkay.Click
        Me.DialogResult = DialogResult.OK
        MyConfiguration.Algorithm = cbAlgorithm.SelectedIndex
        MyConfiguration.PageSize = cbPageSize.SelectedItem
        MyConfiguration.SpareSize = cbSpareSize.SelectedItem
        Select Case cb_ECC_BITERR.SelectedIndex
            Case 0
                MyConfiguration.BitError = 1
            Case 1
                MyConfiguration.BitError = 2
            Case 2
                MyConfiguration.BitError = 4
            Case 3
                MyConfiguration.BitError = 8
            Case 4
                MyConfiguration.BitError = 10
            Case 5
                MyConfiguration.BitError = 14
        End Select
        Select Case cb_sym_width.SelectedIndex
            Case 0
                MyConfiguration.SymSize = 9
            Case 1
                MyConfiguration.SymSize = 10
        End Select
        MyConfiguration.ReverseData = cb_rs_reverse_data.Checked
        Dim sector_count As Integer = (MyConfiguration.PageSize / 512)
        Dim ecc_data As Integer = ECC_LIB.GetEccDataSize(MyConfiguration)
        If MyConfiguration.EccRegion Is Nothing OrElse Not MyConfiguration.EccRegion.Length = sector_count Then
            ReDim MyConfiguration.EccRegion(sector_count - 1)
            Dim ptr As Integer = 2
            For i = 0 To sector_count - 1
                MyConfiguration.EccRegion(i) = ptr
                ptr += ecc_data
            Next
        End If
        Me.Close()
    End Sub

    Private Sub cbAlgorithm_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbAlgorithm.SelectedIndexChanged
        Select Case cbAlgorithm.SelectedIndex
            Case 0 'Hamming
                cb_ECC_BITERR.SelectedIndex = 0
                cb_ECC_BITERR.Enabled = False 'Only 1-bit ECC supported
                cb_sym_width.Enabled = False
            Case 1 'Reed-Solomon
                cb_ECC_BITERR.Enabled = True
                cb_sym_width.Enabled = True
            Case 2 'Binary BHC
                cb_ECC_BITERR.Enabled = True
                cb_sym_width.Enabled = False
        End Select
    End Sub

    Private Sub cbPageSize_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbPageSize.SelectedIndexChanged
        If cbPageSize.SelectedIndex = 0 Then
            If Not cbSpareSize.Items(0).Equals("16") Then
                cbSpareSize.Items.Insert(0, "16")
            End If
            cbAlgorithm.SelectedIndex = 0
            cbAlgorithm.Enabled = False
            cbSpareSize.SelectedIndex = 0
            cbSpareSize.Enabled = False
        Else
            cbAlgorithm.Enabled = True
            cbSpareSize.Enabled = True
            If cbSpareSize.Items(0).Equals("16") Then
                cbSpareSize.Items.RemoveAt(0)
                cbSpareSize.SelectedIndex = 0
            End If
        End If
    End Sub

End Class