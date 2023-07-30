Public Class vendor_at45
    Private FCUSB_PROG As MemoryDeviceUSB
    Private SPI_Flash As SPI.SPI_Programmer

    Public Property PAGE_SIZE_CHANGED As Boolean = False

    Sub New(mem_dev_programmer As MemoryDeviceUSB)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Me.FCUSB_PROG = mem_dev_programmer
    End Sub

    Private Sub vendor_at45_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        lblInfo.Text = ""
        Me.SPI_Flash = DirectCast(FCUSB_PROG, SPI.SPI_Programmer)
        If Me.SPI_Flash.ExtendedPage Then
            rb1_528.Checked = True
        Else
            rb2_512.Checked = True
        End If
    End Sub

    Private Sub cmdSave_Click(sender As Object, e As EventArgs) Handles cmdSave.Click
        Dim IsExtended As Boolean = rb1_528.Checked
        If Me.SPI_Flash.AT45_SetPageConfiguration(IsExtended) Then
            Me.PAGE_SIZE_CHANGED = True
            Me.lblInfo.Text = "Successfully programmed Page Size Config"
        Else
            Me.lblInfo.Text = "Error programming the Page Size Config"
        End If
    End Sub

End Class
