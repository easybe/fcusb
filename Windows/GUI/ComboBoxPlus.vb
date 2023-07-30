Imports System.Collections
Imports System.ComponentModel
Imports System.Drawing
Imports System.Security.Permissions
Imports System.Windows.Forms.Design
Imports System.Runtime.InteropServices

Namespace CustomComboPlus

    <SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags:=SecurityPermissionFlag.UnmanagedCode)> _
    <Designer(GetType(CustomComboBoxDesigner))> _
    Public Class CustomComboPlus
        Inherits ComboBox
        Implements IPopupControlHost

        Private m_popupCtrl As New PopupControl()
        Private m_dropDownCtrl As Control
        Private m_bDroppedDown As Boolean = False
        Private m_sizeMode As SizeMode = SizeMode.UseComboSize
        Private m_lastHideTime As DateTime = DateTime.Now
        Private m_timerAutoFocus As Timer
        Private m_sizeOriginal As New Size(1, 1)
        Private m_sizeCombo As Size
        Private m_bIsResizable As Boolean = True
        Private m_current_text As String = ""

        Public Sub New()
            MyBase.New()
            m_sizeCombo = New Size(MyBase.DropDownWidth, MyBase.DropDownHeight)
            AddHandler m_popupCtrl.Closing, New ToolStripDropDownClosingEventHandler(AddressOf m_dropDown_Closing)
            Me.DrawMode = DrawMode.OwnerDrawFixed
        End Sub

        Private Sub m_dropDown_Closing(sender As Object, e As ToolStripDropDownClosingEventArgs)
            m_lastHideTime = DateTime.Now
        End Sub

        Public Sub New(dropControl As Control)
            Me.New()
            DropDownControl = dropControl
        End Sub

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If m_timerAutoFocus IsNot Nothing Then
                    m_timerAutoFocus.Dispose()
                    m_timerAutoFocus = Nothing
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        Private Sub timerAutoFocus_Tick(sender As Object, e As EventArgs)
            If m_popupCtrl.Visible AndAlso Not DropDownControl.Focused Then
                DropDownControl.Focus()
                m_timerAutoFocus.Enabled = False
            End If

            If MyBase.DroppedDown Then
                MyBase.DroppedDown = False
            End If
        End Sub

        Private Sub m_dropDown_LostFocus(sender As Object, e As EventArgs)
            m_lastHideTime = DateTime.Now
        End Sub

        Public Overrides Property Text As String
            Get
                'Return MyBase.Text
                Return m_current_text
            End Get
            Set(value As String)
                'MyBase.Text = value
                m_current_text = value
                Me.Refresh()
            End Set
        End Property

        Private Sub CustomComboPlus_DrawItem(sender As Object, e As DrawItemEventArgs) Handles Me.DrawItem
         
            e.Graphics.DrawString(m_current_text, e.Font, Brushes.Black, e.Bounds)

        End Sub


#Region "Events"

        Public Shadows Event DropDown As EventHandler
        Public Shadows Event DropDownClosed As EventHandler

        Public Shadows Event SelectedValueChanged As OldNewEventHandler(Of Object)

        Public Sub RaiseDropDownEvent()
            RaiseEvent DropDown(Me, EventArgs.Empty)
        End Sub

        Public Sub RaiseDropDownClosedEvent()
            RaiseEvent DropDownClosed(Me, EventArgs.Empty)
        End Sub

        Public Sub RaiseSelectedValueChangedEvent(oldValue As Object, newValue As Object)
            RaiseEvent SelectedValueChanged(Me, New OldNewEventArgs(Of Object)(oldValue, newValue))
        End Sub

#End Region

#Region "IPopupControlHost Members"

        Public Overridable Sub ShowDropDown() Implements IPopupControlHost.ShowDropDown
            If m_popupCtrl IsNot Nothing AndAlso Not IsDroppedDown Then
                ' Raise drop-down event.
                RaiseDropDownEvent()

                ' Restore original control size.
                AutoSizeDropDown()

                Dim location As Point = PointToScreen(New Point(0, Height))

                ' Actually show popup.
                Dim resizeMode As PopupResizeMode = (If(Me.m_bIsResizable, PopupResizeMode.BottomRight, PopupResizeMode.None))
                m_popupCtrl.Show(Me.DropDownControl, location.X, location.Y, Width - 3, Height, resizeMode)
                m_bDroppedDown = True

                m_popupCtrl.PopupControlHost = Me

                ' Initialize automatic focus timer?
                If m_timerAutoFocus Is Nothing Then
                    m_timerAutoFocus = New Timer()
                    m_timerAutoFocus.Interval = 10
                    AddHandler m_timerAutoFocus.Tick, New EventHandler(AddressOf timerAutoFocus_Tick)
                End If
                ' Enable the timer!
                m_timerAutoFocus.Enabled = True
                m_sShowTime = DateTime.Now
            End If
        End Sub

        Public Overridable Sub HideDropDown() Implements IPopupControlHost.HideDropDown
            If m_popupCtrl IsNot Nothing AndAlso IsDroppedDown Then
                ' Hide drop-down control.
                m_popupCtrl.Hide()
                m_bDroppedDown = False

                ' Disable automatic focus timer.
                If m_timerAutoFocus IsNot Nothing AndAlso m_timerAutoFocus.Enabled Then
                    m_timerAutoFocus.Enabled = False
                End If

                ' Raise drop-down closed event.
                RaiseDropDownClosedEvent()
            End If
        End Sub

#End Region

#Region "Methods"

        ''' <summary>
        ''' Automatically resize drop-down from properties.
        ''' </summary>
        Protected Sub AutoSizeDropDown()
            If DropDownControl IsNot Nothing Then
                Select Case DropDownSizeMode
                    Case SizeMode.UseComboSize
                        DropDownControl.Size = New Size(Width, m_sizeCombo.Height)
                        Exit Select

                    Case SizeMode.UseControlSize
                        DropDownControl.Size = New Size(m_sizeOriginal.Width, m_sizeOriginal.Height)
                        Exit Select

                    Case SizeMode.UseDropDownSize
                        DropDownControl.Size = m_sizeCombo
                        Exit Select
                End Select
            End If
        End Sub

        ''' <summary>
        ''' Assigns control to custom drop-down area of combo box.
        ''' </summary>
        ''' <param name="control">Control to be used as drop-down. Please note that this control must not be contained elsewhere.</param>
        Protected Overridable Sub AssignControl(control As Control)
            ' If specified control is different then...
            If control IsNot DropDownControl Then
                ' Preserve original container size.
                m_sizeOriginal = control.Size

                ' Reference the user-specified drop down control.
                m_dropDownCtrl = control
            End If
        End Sub

#End Region

#Region "Win32 message handlers"

        Public Const WM_COMMAND As UInteger = &H111
        Public Const WM_USER As UInteger = &H400
        Public Const WM_REFLECT As UInteger = WM_USER + &H1C00
        Public Const WM_LBUTTONDOWN As UInteger = &H201

        Public Const CBN_DROPDOWN As UInteger = 7
        Public Const CBN_CLOSEUP As UInteger = 8

        Public Shared Function HIWORD(n As Integer) As UInteger
            Return CUInt(n >> 16) And &HFFFFUI
        End Function

        Public Overrides Function PreProcessMessage(ByRef m As Message) As Boolean
            If m.Msg = (WM_REFLECT + WM_COMMAND) Then
                If HIWORD(CInt(m.WParam)) = CBN_DROPDOWN Then
                    Return False
                End If
            End If
            Return MyBase.PreProcessMessage(m)
        End Function

        Private Shared m_sShowTime As DateTime = DateTime.Now

        Private Sub AutoDropDown()
            If m_popupCtrl IsNot Nothing AndAlso m_popupCtrl.Visible Then
                HideDropDown()
            ElseIf (DateTime.Now - m_lastHideTime).Milliseconds > 50 Then
                ShowDropDown()
            End If
        End Sub

        Protected Overrides Sub WndProc(ByRef m As Message)
            If m.Msg = WM_LBUTTONDOWN Then
                AutoDropDown()
                Return
            End If

            If m.Msg = (WM_REFLECT + WM_COMMAND) Then
                Select Case HIWORD(CInt(m.WParam))
                    Case CBN_DROPDOWN
                        AutoDropDown()
                        Return

                    Case CBN_CLOSEUP
                        If (DateTime.Now - m_sShowTime).Seconds > 1 Then
                            HideDropDown()
                        End If
                        Return
                End Select
            End If

            MyBase.WndProc(m)
        End Sub

#End Region

#Region "Enumerations"

        Public Enum SizeMode
            UseComboSize
            UseControlSize
            UseDropDownSize
        End Enum

#End Region

#Region "Properties"

        ''' <summary>
        ''' Actual drop-down control itself.
        ''' </summary>
        <Browsable(False)> _
        Public Property DropDownControl() As Control
            Get
                Return m_dropDownCtrl
            End Get
            Set(value As Control)
                AssignControl(value)
            End Set
        End Property

        ''' <summary>
        ''' Indicates if drop-down is currently shown.
        ''' </summary>
        <Browsable(False)> _
        Public ReadOnly Property IsDroppedDown() As Boolean
            Get
                '&& m_popupCtrl.Visible
                Return Me.m_bDroppedDown
            End Get
        End Property

        ''' <summary>
        ''' Indicates if drop-down is resizable.
        ''' </summary>
        <Category("Custom Drop-Down"), Description("Indicates if drop-down is resizable.")> _
        Public Property AllowResizeDropDown() As Boolean
            Get
                Return Me.m_bIsResizable
            End Get
            Set(value As Boolean)
                Me.m_bIsResizable = value
            End Set
        End Property

        ''' <summary>
        ''' Indicates current sizing mode.
        ''' </summary>
        <Category("Custom Drop-Down"), Description("Indicates current sizing mode."), DefaultValue(SizeMode.UseComboSize)> _
        Public Property DropDownSizeMode() As SizeMode
            Get
                Return Me.m_sizeMode
            End Get
            Set(value As SizeMode)
                If value <> Me.m_sizeMode Then
                    Me.m_sizeMode = value
                    AutoSizeDropDown()
                End If
            End Set
        End Property

        <Category("Custom Drop-Down")> _
        Public Property DropSize() As Size
            Get
                Return m_sizeCombo
            End Get
            Set(value As Size)
                m_sizeCombo = value
                If DropDownSizeMode = SizeMode.UseDropDownSize Then
                    AutoSizeDropDown()
                End If
            End Set
        End Property

        <Category("Custom Drop-Down"), Browsable(False)> _
        Public Property ControlSize() As Size
            Get
                Return m_sizeOriginal
            End Get
            Set(value As Size)
                m_sizeOriginal = value
                If DropDownSizeMode = SizeMode.UseControlSize Then
                    AutoSizeDropDown()
                End If
            End Set
        End Property

#End Region

#Region "Hide some unwanted properties"

        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> <Browsable(False), [ReadOnly](True)> _
        Public Shadows Property DropDownStyle() As ComboBoxStyle
            Get
                Return MyBase.DropDownStyle
            End Get
            Set(value As ComboBoxStyle)
                MyBase.DropDownStyle = value
                'If value = ComboBoxStyle.DropDownList Then
                '    Me.BackColor = SystemColors.ControlLight
                'End If
            End Set
        End Property

        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> <Browsable(False), [ReadOnly](True)> _
        Public Shadows ReadOnly Property Items() As ObjectCollection
            Get
                Return MyBase.Items
            End Get
        End Property

        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> <Browsable(False), [ReadOnly](True)> _
        Public Shadows Property ItemHeight() As Integer
            Get
                Return MyBase.ItemHeight
            End Get
            Set(value As Integer)
            End Set
        End Property

        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> <Browsable(False), [ReadOnly](True)> _
        Public Shadows Property MaxDropDownItems() As Integer
            Get
                Return MyBase.MaxDropDownItems
            End Get
            Set(value As Integer)
            End Set
        End Property

        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> <Browsable(False), [ReadOnly](True)> _
        Public Shadows Property DisplayMember() As String
            Get
                Return MyBase.DisplayMember
            End Get
            Set(value As String)
            End Set
        End Property

        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> <Browsable(False), [ReadOnly](True)> _
        Public Shadows Property ValueMember() As String
            Get
                Return MyBase.ValueMember
            End Get
            Set(value As String)
            End Set
        End Property

        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> <Browsable(False), [ReadOnly](True)> _
        Public Shadows Property DropDownWidth() As Integer
            Get
                Return MyBase.DropDownWidth
            End Get
            Set(value As Integer)
            End Set
        End Property

        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> <Browsable(False), [ReadOnly](True)> _
        Public Shadows Property DropDownHeight() As Integer
            Get
                Return MyBase.DropDownHeight
            End Get
            Set(value As Integer)
            End Set
        End Property

        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> <Browsable(False), [ReadOnly](True)> _
        Public Shadows Property IntegralHeight() As Boolean
            Get
                Return MyBase.IntegralHeight
            End Get
            Set(value As Boolean)
            End Set
        End Property

        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> <Browsable(False), [ReadOnly](True)> _
        Public Shadows Property Sorted() As Boolean
            Get
                Return MyBase.Sorted
            End Get
            Set(value As Boolean)
            End Set
        End Property

#End Region


    End Class

    Friend Class CustomComboBoxDesigner
        Inherits ParentControlDesigner

        Protected Overrides Sub PreFilterProperties(properties As IDictionary)
            MyBase.PreFilterProperties(properties)
            properties.Remove("DropDownStyle")
            properties.Remove("Items")
            properties.Remove("ItemHeight")
            properties.Remove("MaxDropDownItems")
            properties.Remove("DisplayMember")
            properties.Remove("ValueMember")
            properties.Remove("DropDownWidth")
            properties.Remove("DropDownHeight")
            properties.Remove("IntegralHeight")
            properties.Remove("Sorted")
        End Sub

    End Class

    Public Class OldNewEventArgs(Of T)
        Inherits EventArgs
        Public Sub New(oldValue__1 As T, newValue__2 As T)
            OldValue = oldValue__1
            NewValue = newValue__2
        End Sub

        Public Property OldValue() As T
            Get
                Return Me.m_oldValue
            End Get
            Protected Set(value As T)
                Me.m_oldValue = value
            End Set
        End Property
        Public Property NewValue() As T
            Get
                Return Me.m_newValue
            End Get
            Protected Set(value As T)
                Me.m_newValue = value
            End Set
        End Property

        Private m_oldValue As T = Nothing
        Private m_newValue As T = Nothing
    End Class

    Public Delegate Sub OldNewEventHandler(Of T)(sender As Object, e As OldNewEventArgs(Of T))

    Public Enum PopupResizeMode
        None = 0

        ' Individual styles.
        Left = 1
        Top = 2
        Right = 4
        Bottom = 8

        ' Combined styles.
        All = (Top Or Left Or Bottom Or Right)
        TopLeft = (Top Or Left)
        TopRight = (Top Or Right)
        BottomLeft = (Bottom Or Left)
        BottomRight = (Bottom Or Right)
    End Enum

    Public Enum GripAlignMode
        TopLeft
        TopRight
        BottomLeft
        BottomRight
    End Enum

    Public NotInheritable Class GripRenderer
#Region "Construction and destruction"

        Private Sub New()
        End Sub

#End Region

#Region "Methods"

        Private Shared Sub InitializeGripBitmap(g As Graphics, size As Size, forceRefresh As Boolean)
            If m_sGripBitmap Is Nothing OrElse forceRefresh OrElse size <> m_sGripBitmap.Size Then
                ' Draw size grip into a bitmap image.
                m_sGripBitmap = New Bitmap(size.Width, size.Height, g)
                Using gripG As Graphics = Graphics.FromImage(m_sGripBitmap)
                    ControlPaint.DrawSizeGrip(gripG, SystemColors.ButtonFace, 0, 0, size.Width, size.Height)
                End Using
            End If
        End Sub

        Public Shared Sub RefreshSystemColors(g As Graphics, size As Size)
            InitializeGripBitmap(g, size, True)
        End Sub

        Public Shared Sub Render(g As Graphics, location As Point, size As Size, mode As GripAlignMode)
            InitializeGripBitmap(g, size, False)

            ' Calculate display size and position of grip.
            Select Case mode
                Case GripAlignMode.TopLeft
                    size.Height = -size.Height
                    size.Width = -size.Width
                    Exit Select

                Case GripAlignMode.TopRight
                    size.Height = -size.Height
                    Exit Select

                Case GripAlignMode.BottomLeft
                    size.Width = -size.Height
                    Exit Select
            End Select

            ' Reverse size grip for left-aligned.
            If size.Width < 0 Then
                location.X -= size.Width
            End If
            If size.Height < 0 Then
                location.Y -= size.Height
            End If

            g.DrawImage(GripBitmap, location.X, location.Y, size.Width, size.Height)
        End Sub

        Public Shared Sub Render(g As Graphics, location As Point, mode As GripAlignMode)
            Render(g, location, New Size(16, 16), mode)
        End Sub

#End Region

#Region "Properties"

        Private Shared ReadOnly Property GripBitmap() As Bitmap
            Get
                Return m_sGripBitmap
            End Get
        End Property

#End Region

#Region "Attributes"

        Private Shared m_sGripBitmap As Bitmap

#End Region
    End Class

    Public Class PopupDropDown
        Inherits ToolStripDropDown

#Region "Construction and destruction"

        Public Sub New(autoSize__1 As Boolean)
            AutoSize = autoSize__1
            Padding = InlineAssignHelper(Margin, Padding.Empty)
        End Sub

#End Region

        Protected Overrides Sub OnClosing(e As ToolStripDropDownClosingEventArgs)
            Dim hostedControl As Control = GetHostedControl()
            If hostedControl IsNot Nothing Then
                RemoveHandler hostedControl.SizeChanged, AddressOf hostedControl_SizeChanged
            End If
            MyBase.OnClosing(e)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)

            GripBounds = Rectangle.Empty

            If CompareResizeMode(PopupResizeMode.BottomLeft) Then
                ' Draw grip area at bottom-left of popup.
                e.Graphics.FillRectangle(SystemBrushes.ButtonFace, 1, Height - 16, Width - 2, 14)
                GripBounds = New Rectangle(1, Height - 16, 16, 16)
                GripRenderer.Render(e.Graphics, GripBounds.Location, GripAlignMode.BottomLeft)
            ElseIf CompareResizeMode(PopupResizeMode.BottomRight) Then
                ' Draw grip area at bottom-right of popup.
                e.Graphics.FillRectangle(SystemBrushes.ButtonFace, 1, Height - 16, Width - 2, 14)
                GripBounds = New Rectangle(Width - 17, Height - 16, 16, 16)
                GripRenderer.Render(e.Graphics, GripBounds.Location, GripAlignMode.BottomRight)
            ElseIf CompareResizeMode(PopupResizeMode.TopLeft) Then
                ' Draw grip area at top-left of popup.
                e.Graphics.FillRectangle(SystemBrushes.ButtonFace, 1, 1, Width - 2, 14)
                GripBounds = New Rectangle(1, 0, 16, 16)
                GripRenderer.Render(e.Graphics, GripBounds.Location, GripAlignMode.TopLeft)
            ElseIf CompareResizeMode(PopupResizeMode.TopRight) Then
                ' Draw grip area at top-right of popup.
                e.Graphics.FillRectangle(SystemBrushes.ButtonFace, 1, 1, Width - 2, 14)
                GripBounds = New Rectangle(Width - 17, 0, 16, 16)
                GripRenderer.Render(e.Graphics, GripBounds.Location, GripAlignMode.TopRight)
            End If
        End Sub

        Protected Overrides Sub OnSizeChanged(e As EventArgs)
            MyBase.OnSizeChanged(e)

            ' When drop-down window is being resized by the user (i.e. not locked),
            ' update size of hosted control.
            If Not m_lockedThisSize Then
                RecalculateHostedControlLayout()
            End If
        End Sub

        Protected Sub hostedControl_SizeChanged(sender As Object, e As EventArgs)
            ' Only update size of this container when it is not locked.
            If Not m_lockedHostedControlSize Then
                ResizeFromContent(-1)
            End If
        End Sub

#Region "Methods"

        Public Shadows Sub Show(x As Integer, y As Integer)
            Show(x, y, -1, -1)
        End Sub

        Public Shadows Sub Show(x As Integer, y As Integer, width As Integer, height__1 As Integer)
            ' If no hosted control is associated, this procedure is pointless!
            Dim hostedControl As Control = GetHostedControl()
            If hostedControl Is Nothing Then
                Return
            End If

            ' Initially hosted control should be displayed within a drop down of 1x1, however
            ' its size should exceed the dimensions of the drop-down.
            If True Then
                m_lockedHostedControlSize = True
                m_lockedThisSize = True

                ' Display actual popup and occupy just 1x1 pixel to avoid automatic reposition.
                Size = New Size(1, 1)
                MyBase.Show(x, y)

                m_lockedHostedControlSize = False
                m_lockedThisSize = False
            End If

            ' Resize drop-down to fit its contents.
            ResizeFromContent(width)

            ' If client area was enlarged using the minimum width paramater, then the hosted
            ' control must also be enlarged.
            If m_refreshSize Then
                RecalculateHostedControlLayout()
            End If

            ' If popup is overlapping the initial position then move above!
            If y > Top AndAlso y <= Bottom Then
                Top = y - Height - (If(height__1 <> -1, height__1, 0))

                Dim previous As PopupResizeMode = ResizeMode
                If ResizeMode = PopupResizeMode.BottomLeft Then
                    ResizeMode = PopupResizeMode.TopLeft
                ElseIf ResizeMode = PopupResizeMode.BottomRight Then
                    ResizeMode = PopupResizeMode.TopRight
                End If

                If ResizeMode <> previous Then
                    RecalculateHostedControlLayout()
                End If
            End If

            ' Assign event handler to control.
            AddHandler hostedControl.SizeChanged, AddressOf hostedControl_SizeChanged
        End Sub

        Protected Sub ResizeFromContent(width As Integer)
            If m_lockedThisSize Then
                Return
            End If

            ' Prevent resizing hosted control to 1x1 pixel!
            m_lockedHostedControlSize = True

            ' Resize from content again because certain information was not available before.
            Dim bounds__1 As Rectangle = Bounds
            bounds__1.Size = SizeFromContent(width)

            If Not CompareResizeMode(PopupResizeMode.None) Then
                If width > 0 AndAlso bounds__1.Width - 2 > width Then
                    If Not CompareResizeMode(PopupResizeMode.Right) Then
                        bounds__1.X -= bounds__1.Width - 2 - width
                    End If
                End If
            End If

            Bounds = bounds__1

            m_lockedHostedControlSize = False
        End Sub

        Protected Sub RecalculateHostedControlLayout()
            If m_lockedHostedControlSize Then
                Return
            End If

            m_lockedThisSize = True

            ' Update size of hosted control.
            Dim hostedControl As Control = GetHostedControl()
            If hostedControl IsNot Nothing Then
                ' Fetch control bounds and adjust as necessary.
                Dim bounds As Rectangle = hostedControl.Bounds
                If CompareResizeMode(PopupResizeMode.TopLeft) OrElse CompareResizeMode(PopupResizeMode.TopRight) Then
                    bounds.Location = New Point(1, 16)
                Else
                    bounds.Location = New Point(1, 1)
                End If

                bounds.Width = ClientRectangle.Width - 2
                bounds.Height = ClientRectangle.Height - 2
                If IsGripShown Then
                    bounds.Height -= 16
                End If

                If bounds.Size <> hostedControl.Size Then
                    hostedControl.Size = bounds.Size
                End If
                If bounds.Location <> hostedControl.Location Then
                    hostedControl.Location = bounds.Location
                End If
            End If

            m_lockedThisSize = False
        End Sub

        Public Function GetHostedControl() As Control
            If Items.Count > 0 Then
                Dim host As ToolStripControlHost = TryCast(Items(0), ToolStripControlHost)
                If host IsNot Nothing Then
                    Return host.Control
                End If
            End If
            Return Nothing
        End Function

        Public Function CompareResizeMode(resizeMode__1 As PopupResizeMode) As Boolean
            Return (ResizeMode And resizeMode__1) = resizeMode__1
        End Function

        Protected Function SizeFromContent(width As Integer) As Size
            Dim contentSize As Size = Size.Empty

            m_refreshSize = False

            ' Fetch hosted control.
            Dim hostedControl As Control = GetHostedControl()
            If hostedControl IsNot Nothing Then
                If CompareResizeMode(PopupResizeMode.TopLeft) OrElse CompareResizeMode(PopupResizeMode.TopRight) Then
                    hostedControl.Location = New Point(1, 16)
                Else
                    hostedControl.Location = New Point(1, 1)
                End If
                contentSize = SizeFromClientSize(hostedControl.Size)

                ' Use minimum width (if specified).
                If width > 0 AndAlso contentSize.Width < width Then
                    contentSize.Width = width
                    m_refreshSize = True
                End If
            End If

            ' If a grip box is shown then add it into the drop down height.
            If IsGripShown Then
                contentSize.Height += 16
            End If

            ' Add some additional space to allow for borders.
            contentSize.Width += 2
            contentSize.Height += 2

            Return contentSize
        End Function

#End Region

#Region "Win32 message processing"

#Region "Win32 stuff"

        Protected Const WM_GETMINMAXINFO As Integer = &H24
        Protected Const WM_NCHITTEST As Integer = &H84

        Protected Const HTTRANSPARENT As Integer = -1
        Protected Const HTLEFT As Integer = 10
        Protected Const HTRIGHT As Integer = 11
        Protected Const HTTOP As Integer = 12
        Protected Const HTTOPLEFT As Integer = 13
        Protected Const HTTOPRIGHT As Integer = 14
        Protected Const HTBOTTOM As Integer = 15
        Protected Const HTBOTTOMLEFT As Integer = 16
        Protected Const HTBOTTOMRIGHT As Integer = 17

        <StructLayout(LayoutKind.Sequential)> _
        Friend Structure MINMAXINFO
            Public reserved As Point
            Public maxSize As Size
            Public maxPosition As Point
            Public minTrackSize As Size
            Public maxTrackSize As Size
        End Structure

        Protected Shared Function HIWORD(n As Integer) As Integer
            Return (n >> 16) And &HFFFF
        End Function
        Protected Shared Function HIWORD(n As IntPtr) As Integer
            Return HIWORD(CInt(CLng(n)))
        End Function
        Protected Shared Function LOWORD(n As Integer) As Integer
            Return n And &HFFFF
        End Function
        Protected Shared Function LOWORD(n As IntPtr) As Integer
            Return LOWORD(CInt(CLng(n)))
        End Function

#End Region

        <SecurityPermission(SecurityAction.LinkDemand, Flags:=SecurityPermissionFlag.UnmanagedCode)> _
        Protected Overrides Sub WndProc(ByRef m As Message)
            If Not ProcessGrip(m, False) Then
                MyBase.WndProc(m)
            End If
        End Sub

        ''' <summary>
        ''' Processes the resizing messages.
        ''' </summary>
        ''' <param name="m">The message.</param>
        ''' <returns>true, if the WndProc method from the base class shouldn't be invoked.</returns>
        <SecurityPermission(SecurityAction.LinkDemand, Flags:=SecurityPermissionFlag.UnmanagedCode)> _
        Public Function ProcessGrip(ByRef m As Message) As Boolean
            Return ProcessGrip(m, True)
        End Function

        <SecurityPermission(SecurityAction.LinkDemand, Flags:=SecurityPermissionFlag.UnmanagedCode)> _
        Private Function ProcessGrip(ByRef m As Message, contentControl As Boolean) As Boolean
            If ResizeMode <> PopupResizeMode.None Then
                Select Case m.Msg
                    Case WM_NCHITTEST
                        Return OnNcHitTest(m, contentControl)

                    Case WM_GETMINMAXINFO
                        Return OnGetMinMaxInfo(m)
                End Select
            End If
            Return False
        End Function

        <SecurityPermission(SecurityAction.LinkDemand, Flags:=SecurityPermissionFlag.UnmanagedCode)> _
        Private Function OnGetMinMaxInfo(ByRef m As Message) As Boolean
            Dim hostedControl As Control = GetHostedControl()
            If hostedControl IsNot Nothing Then
                Dim minmax As MINMAXINFO = CType(Marshal.PtrToStructure(m.LParam, GetType(MINMAXINFO)), MINMAXINFO)

                ' Maximum size.
                If hostedControl.MaximumSize.Width <> 0 Then
                    minmax.maxTrackSize.Width = hostedControl.MaximumSize.Width
                End If
                If hostedControl.MaximumSize.Height <> 0 Then
                    minmax.maxTrackSize.Height = hostedControl.MaximumSize.Height
                End If

                ' Minimum size.
                minmax.minTrackSize = New Size(32, 32)
                If hostedControl.MinimumSize.Width > minmax.minTrackSize.Width Then
                    minmax.minTrackSize.Width = hostedControl.MinimumSize.Width
                End If
                If hostedControl.MinimumSize.Height > minmax.minTrackSize.Height Then
                    minmax.minTrackSize.Height = hostedControl.MinimumSize.Height
                End If

                Marshal.StructureToPtr(minmax, m.LParam, False)
            End If
            Return True
        End Function

        Private Function OnNcHitTest(ByRef m As Message, contentControl As Boolean) As Boolean
            Dim location As Point = PointToClient(New Point(LOWORD(m.LParam), HIWORD(m.LParam)))
            Dim transparent As New IntPtr(HTTRANSPARENT)

            ' Check for simple gripper dragging.
            If GripBounds.Contains(location) Then
                If CompareResizeMode(PopupResizeMode.BottomLeft) Then
                    m.Result = If(contentControl, transparent, CType(HTBOTTOMLEFT, IntPtr))
                    Return True
                ElseIf CompareResizeMode(PopupResizeMode.BottomRight) Then
                    m.Result = If(contentControl, transparent, CType(HTBOTTOMRIGHT, IntPtr))
                    Return True
                ElseIf CompareResizeMode(PopupResizeMode.TopLeft) Then
                    m.Result = If(contentControl, transparent, CType(HTTOPLEFT, IntPtr))
                    Return True
                ElseIf CompareResizeMode(PopupResizeMode.TopRight) Then
                    m.Result = If(contentControl, transparent, CType(HTTOPRIGHT, IntPtr))
                    Return True
                End If
            Else
                ' Check for edge based dragging.
                Dim rectClient As Rectangle = ClientRectangle
                If location.X > rectClient.Right - 3 AndAlso location.X <= rectClient.Right AndAlso CompareResizeMode(PopupResizeMode.Right) Then
                    m.Result = If(contentControl, transparent, CType(HTRIGHT, IntPtr))
                    Return True
                ElseIf location.Y > rectClient.Bottom - 3 AndAlso location.Y <= rectClient.Bottom AndAlso CompareResizeMode(PopupResizeMode.Bottom) Then
                    m.Result = If(contentControl, transparent, CType(HTBOTTOM, IntPtr))
                    Return True
                ElseIf location.X > -1 AndAlso location.X < 3 AndAlso CompareResizeMode(PopupResizeMode.Left) Then
                    m.Result = If(contentControl, transparent, CType(HTLEFT, IntPtr))
                    Return True
                ElseIf location.Y > -1 AndAlso location.Y < 3 AndAlso CompareResizeMode(PopupResizeMode.Top) Then
                    m.Result = If(contentControl, transparent, CType(HTTOP, IntPtr))
                    Return True
                End If
            End If
            Return False
        End Function

#End Region

#Region "Properties"

        ''' <summary>
        ''' Type of resize mode, grips are automatically drawn at bottom-left and bottom-right corners.
        ''' </summary>
        Public Property ResizeMode() As PopupResizeMode
            Get
                Return m_resizeMode
            End Get
            Set(value As PopupResizeMode)
                If value <> m_resizeMode Then
                    m_resizeMode = value
                    Invalidate()
                End If
            End Set
        End Property

        ''' <summary>
        ''' Bounds of active grip box position.
        ''' </summary>
        Protected Property GripBounds() As Rectangle
            Get
                Return Me.m_gripBounds
            End Get
            Set(value As Rectangle)
                Me.m_gripBounds = value
            End Set
        End Property

        ''' <summary>
        ''' Indicates when a grip box is shown.
        ''' </summary>
        Protected ReadOnly Property IsGripShown() As Boolean
            Get
                Return (ResizeMode = PopupResizeMode.TopLeft OrElse ResizeMode = PopupResizeMode.TopRight OrElse ResizeMode = PopupResizeMode.BottomLeft OrElse ResizeMode = PopupResizeMode.BottomRight)
            End Get
        End Property

#End Region

#Region "Attributes"

        Private m_resizeMode As PopupResizeMode = PopupResizeMode.None
        Private m_gripBounds As Rectangle = Rectangle.Empty

        Private m_lockedHostedControlSize As Boolean = False
        Private m_lockedThisSize As Boolean = False
        Private m_refreshSize As Boolean = False
        Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
            target = value
            Return value
        End Function

#End Region
    End Class

    Public Interface IPopupControlHost
        Sub ShowDropDown()
        Sub HideDropDown()
    End Interface

    Public Class PopupControl

        Public Sub New()
            InitializeDropDown()
        End Sub

        Private Sub m_dropDown_Closed(sender As Object, e As ToolStripDropDownClosedEventArgs)
            If AutoResetWhenClosed Then
                DisposeHost()
            End If

            ' Hide drop down within popup control.
            If PopupControlHost IsNot Nothing Then
                PopupControlHost.HideDropDown()
            End If
        End Sub

        Public Custom Event Closing As ToolStripDropDownClosingEventHandler
            AddHandler(value As ToolStripDropDownClosingEventHandler)
                AddHandler m_dropDown.Closing, value
            End AddHandler
            RemoveHandler(value As ToolStripDropDownClosingEventHandler)
                RemoveHandler m_dropDown.Closing, value
            End RemoveHandler
            RaiseEvent(sender As System.Object, e As System.EventArgs)
            End RaiseEvent
        End Event


#Region "Methods"

        Public Sub Show(control As Control, x As Integer, y As Integer)
            Show(control, x, y, PopupResizeMode.None)
        End Sub

        Public Sub Show(control As Control, x As Integer, y As Integer, resizeMode As PopupResizeMode)
            Show(control, x, y, -1, -1, resizeMode)
        End Sub

        Public Sub Show(control As Control, x As Integer, y As Integer, width As Integer, height As Integer, resizeMode As PopupResizeMode)
            If control Is Nothing Then Exit Sub
            Dim controlSize As Size = control.Size
            InitializeHost(control)
            m_dropDown.ResizeMode = resizeMode
            m_dropDown.Show(x, y, width, height)
            control.Focus()
        End Sub

        Public Sub Hide()
            If m_dropDown IsNot Nothing AndAlso m_dropDown.Visible Then
                m_dropDown.Hide()
                DisposeHost()
            End If
        End Sub

        Public Sub Reset()
            DisposeHost()
        End Sub

#End Region

#Region "Internal methods"

        Protected Sub DisposeHost()
            If m_host IsNot Nothing Then
                ' Make sure host is removed from drop down.
                If m_dropDown IsNot Nothing Then
                    m_dropDown.Items.Clear()
                End If

                ' Dispose of host.
                m_host = Nothing
            End If

            PopupControlHost = Nothing
        End Sub

        Protected Sub InitializeHost(control__1 As Control)
            InitializeDropDown()

            ' If control is not yet being hosted then initialize host.
            If control__1 IsNot Control Then
                DisposeHost()
            End If

            ' Create a new host?
            If m_host Is Nothing Then
                m_host = New ToolStripControlHost(control__1)
                m_host.AutoSize = False
                m_host.Padding = Padding
                m_host.Margin = Margin
            End If

            ' Add control to drop-down.
            m_dropDown.Items.Clear()
            m_dropDown.Padding = InlineAssignHelper(m_dropDown.Margin, Padding.Empty)
            m_dropDown.Items.Add(m_host)
        End Sub

        Protected Sub InitializeDropDown()
            ' Does a drop down exist?
            If m_dropDown Is Nothing Then
                m_dropDown = New PopupDropDown(False)
                AddHandler m_dropDown.Closed, New ToolStripDropDownClosedEventHandler(AddressOf m_dropDown_Closed)
            End If
        End Sub

#End Region

#Region "Properties"

        Public ReadOnly Property Visible() As Boolean
            Get
                Return If((Me.m_dropDown IsNot Nothing AndAlso Me.m_dropDown.Visible), True, False)
            End Get
        End Property

        Public ReadOnly Property Control() As Control
            Get
                Return If((Me.m_host IsNot Nothing), Me.m_host.Control, Nothing)
            End Get
        End Property

        Public Property Padding() As Padding
            Get
                Return Me.m_padding
            End Get
            Set(value As Padding)
                Me.m_padding = value
            End Set
        End Property

        Public Property Margin() As Padding
            Get
                Return Me.m_margin
            End Get
            Set(value As Padding)
                Me.m_margin = value
            End Set
        End Property

        Public Property AutoResetWhenClosed() As Boolean
            Get
                Return Me.m_autoReset
            End Get
            Set(value As Boolean)
                Me.m_autoReset = value
            End Set
        End Property

        ''' <summary>
        ''' Gets or sets the popup control host, this is used to hide/show popup.
        ''' </summary>
        Public Property PopupControlHost() As IPopupControlHost
            Get
                Return m_PopupControlHost
            End Get
            Set(value As IPopupControlHost)
                m_PopupControlHost = value
            End Set
        End Property
        Private m_PopupControlHost As IPopupControlHost

#End Region

#Region "Attributes"

        Private m_host As ToolStripControlHost
        Private m_dropDown As PopupDropDown

        Private m_padding As Padding = Padding.Empty
        Private m_margin As New Padding(1, 1, 1, 1)

        Private m_autoReset As Boolean = False
        Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
            target = value
            Return value
        End Function

#End Region

    End Class

End Namespace



