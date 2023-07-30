Public Class DropPanelList

    Public Event ItemClicked(ByVal text As String)

    Private ObjCollection As New ArrayList
    Private ItemHeight As Integer = 15
    Private MyDropLimited As Integer = 10

    Private Sub DropPanelList_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load

    End Sub

    Public Sub AddItems(ByVal ItemName As String, ByVal Items() As String)
        ObjCollection.Add(New dpl_item With {.Name = ItemName, .Items = Items})
        RefreshItems()
    End Sub

    Public Function GetItem(ByVal listindex As Integer, ByVal index As Integer) As String
        Dim d As dpl_item = ObjCollection(listindex)
        Return d.Items(index)
    End Function

    Public Function GetPrefferedWidth() As Integer
        Dim boxwidth As Integer = 0
        Dim d As dpl_item
        For Each d In ObjCollection
            Dim s() As String = d.Items
            For Each l In s
                Dim lbltxt As New Label
                lbltxt.AutoSize = True
                lbltxt.Text = l
                Dim sugg As Integer = lbltxt.PreferredWidth
                If (sugg + 20) > boxwidth Then boxwidth = (sugg + 20)
            Next
        Next
        Return boxwidth
    End Function

    Private Structure dpl_item
        Dim Name As String
        Dim Items() As String
    End Structure

    Private Sub RefreshItems()
        Dim labelfont As New Font("Microsoft Sans Serif", 8.25, FontStyle.Bold)
        Dim AnchorFlags As Integer = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right

        Dim MyPanelWidth As Integer = Me.Width
        Dim TotalItemCount As Integer = 0
        Dim d As dpl_item
        For Each d In ObjCollection
            TotalItemCount += d.Items.Length + 1
        Next
        If TotalItemCount > MyDropLimited Then
            Me.Height = MyDropLimited * ItemHeight '+ 2
        Else
            Me.Height = TotalItemCount * ItemHeight '+ 2
        End If

        Dim x As Integer = 0
        For Each d In ObjCollection
            Dim newp As New Panel With {.Width = MyPanelWidth, .Height = ItemHeight, .Top = x, .Anchor = AnchorFlags}
            newp.Controls.Add(New Label With {.Text = d.Name, .Font = labelfont})
            'Me.Controls.Add(newp)
            Me.MainPanel.Controls.Add(newp)
            x += ItemHeight
            For Each itemtext In d.Items
                Dim itempanel As New Panel With {.Width = MyPanelWidth, .Height = ItemHeight, .Top = x, .Anchor = AnchorFlags}
                Dim itemlabel As New Label With {.Text = itemtext, .Width = MyPanelWidth, .Anchor = AnchorFlags}
                AddHandler itemlabel.MouseEnter, AddressOf Label_MouseEnter
                AddHandler itemlabel.MouseLeave, AddressOf Label_MouseLeave
                AddHandler itemlabel.MouseClick, AddressOf Label_MouseClick
                itempanel.Controls.Add(itemlabel)
                'Me.Controls.Add(itempanel)
                Me.MainPanel.Controls.Add(itempanel)
                x += ItemHeight
            Next
        Next

    End Sub

    Private Sub Label_MouseEnter(sender As System.Object, e As System.EventArgs)
        Dim p As Label = sender
        p.BackColor = Color.FromArgb(10, 36, 106)
        p.ForeColor = Color.White
    End Sub

    Private Sub Label_MouseLeave(sender As System.Object, e As System.EventArgs)
        Dim p As Label = sender
        p.BackColor = Color.White
        p.ForeColor = Color.Black
    End Sub

    Private Sub Label_MouseClick(sender As System.Object, e As System.EventArgs)
        Dim p As Label = sender
        RaiseEvent ItemClicked(p.Text)
    End Sub

    Private Sub DropPanelList_MouseWheel(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseWheel
        Try
            Dim v As Integer = MainPanel.VerticalScroll.Value
            Dim n As Integer
            If e.Delta = 0 Then
                Exit Sub
            ElseIf e.Delta > 0 Then 'Scroll up
                n = v - CInt(Math.Abs(e.Delta))
                If n < 0 Then n = 0
            Else 'Scroll down
                n = v + CInt(Math.Abs(e.Delta))
            End If
            MainPanel.VerticalScroll.Value = n
        Catch ex As Exception
        End Try
    End Sub

End Class
