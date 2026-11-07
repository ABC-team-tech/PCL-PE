Imports System.Windows.Markup

<ContentProperty("Inlines")>
Public Class MyListItem
    Implements IMyRadio

    Public Event Click(sender As Object, e As MouseButtonEventArgs)
    Public Event LogoClick(sender As Object, e As MouseButtonEventArgs)
    Public Event Check(sender As Object, e As RouteEventArgs) Implements IMyRadio.Check
    Public Event Changed(sender As Object, e As RouteEventArgs) Implements IMyRadio.Changed

#Region "后加载控件"

    Private _RectBack As Border = Nothing
    Public ReadOnly Property RectBack As Border
        Get
            If _RectBack Is Nothing Then
                Dim Rect As New Border With {
                    .Name = "RectBack",
                    .CornerRadius = New CornerRadius(If(IsScaleAnimationEnabled OrElse Height > 40, 6, 0)),
                    .RenderTransform = If(IsScaleAnimationEnabled, New ScaleTransform(0.8, 0.8), Nothing),
                    .RenderTransformOrigin = New Point(0.5, 0.5),
                    .BorderThickness = New Thickness(GetWPFSize(1)),
                    .SnapsToDevicePixels = True,
                    .IsHitTestVisible = False,
                    .Opacity = 0
                }
                Rect.SetResourceReference(Border.BackgroundProperty, "ColorBrush7")
                Rect.SetResourceReference(Border.BorderBrushProperty, "ColorBrush6")
                SetColumnSpan(Rect, 999)
                SetRowSpan(Rect, 999)
                Children.Insert(0, Rect)
                _RectBack = Rect

            End If
            Return _RectBack
        End Get
    End Property

    Public ButtonStack As FrameworkElement

    Public PathLogo As FrameworkElement

    Public RectCheck As Border

    Private _LabInfo As TextBlock = Nothing
    Public ReadOnly Property LabInfo As TextBlock
        Get
            If _LabInfo Is Nothing Then
                Dim Lab As New TextBlock With {
                    .Name = "LabInfo",
                    .SnapsToDevicePixels = False,
                    .UseLayoutRounding = False,
                    .HorizontalAlignment = HorizontalAlignment.Left,
                    .IsHitTestVisible = False,
                    .TextTrimming = TextTrimming.CharacterEllipsis,
                    .Visibility = Visibility.Collapsed,
                    .FontSize = 12,
                    .Margin = New Thickness(4, 0, 0, 0),
                    .Opacity = 0.6
                }
                SetColumn(Lab, 3)
                SetRow(Lab, 2)
                Children.Add(Lab)
                _LabInfo = Lab

            End If
            Return _LabInfo
        End Get
    End Property

#End Region

#Region "自定义属性"

    Public Uuid As Integer = GetUuid()

    Public Property IsScaleAnimationEnabled As Boolean
        Get
            Return _IsScaleAnimationEnabled
        End Get
        Set
            _IsScaleAnimationEnabled = Value
            If _RectBack IsNot Nothing Then RectBack.CornerRadius = New CornerRadius(If(Value, 6, 0))
        End Set
    End Property
    Private _IsScaleAnimationEnabled As Boolean = True

    Public Property PaddingLeft As Integer
        Get
            Return ColumnPaddingLeft.Width.Value
        End Get
        Set(value As Integer)
            ColumnPaddingLeft.Width = New GridLength(value)
        End Set
    End Property

    Public Property MinPaddingRight As Integer = 4

    Private _Buttons As IEnumerable(Of MyIconButton)
    Public Property Buttons As IEnumerable(Of MyIconButton)
        Get
            Return _Buttons
        End Get
        Set(value As IEnumerable(Of MyIconButton))
            _Buttons = value

            If ButtonStack IsNot Nothing Then
                Children.Remove(ButtonStack)
                ButtonStack = Nothing
            End If

            Select Case value.Count
                Case 0

                Case 1

                    For Each Btn As MyIconButton In value
                        If Btn.Height.Equals(Double.NaN) Then Btn.Height = 25
                        If Btn.Width.Equals(Double.NaN) Then Btn.Width = 25
                        With Btn
                            .Opacity = 0
                            .Margin = New Thickness(0, 0, 5, 0)
                            .SnapsToDevicePixels = False
                            .HorizontalAlignment = HorizontalAlignment.Right
                            .VerticalAlignment = VerticalAlignment.Center
                            .SnapsToDevicePixels = False
                            .UseLayoutRounding = False
                        End With
                        SetColumnSpan(Btn, 10) : SetRowSpan(Btn, 10)
                        Children.Add(Btn)
                        ButtonStack = Btn
                    Next
                Case Else

                    ButtonStack = New StackPanel With {.Opacity = 0, .Margin = New Thickness(0, 0, 5, 0), .SnapsToDevicePixels = False, .Orientation = Orientation.Horizontal, .HorizontalAlignment = HorizontalAlignment.Right, .VerticalAlignment = VerticalAlignment.Center, .UseLayoutRounding = False}
                    SetColumnSpan(ButtonStack, 10) : SetRowSpan(ButtonStack, 10)

                    For Each Btn As MyIconButton In value
                        If Btn.Height.Equals(Double.NaN) Then Btn.Height = 25
                        If Btn.Width.Equals(Double.NaN) Then Btn.Width = 25
                        CType(ButtonStack, StackPanel).Children.Add(Btn)
                    Next
                    Children.Add(ButtonStack)
            End Select
        End Set
    End Property

    Public ReadOnly Property Inlines As InlineCollection
        Get
            Return LabTitle.Inlines
        End Get
    End Property
    Public Property Title As String
        Get
            Return GetValue(TitleProperty)
        End Get
        Set(value As String)
            SetValue(TitleProperty, value.ReplaceLineEndings(""))
        End Set
    End Property
    Public Shared ReadOnly TitleProperty As DependencyProperty = DependencyProperty.Register("Title", GetType(String), GetType(MyListItem))

    Public Property FontSize As Double
        Get
            Return GetValue(FontSizeProperty)
        End Get
        Set(value As Double)
            SetValue(FontSizeProperty, value)
        End Set
    End Property
    Public Shared ReadOnly FontSizeProperty As DependencyProperty = DependencyProperty.Register("FontSize", GetType(Double), GetType(MyListItem), New PropertyMetadata(CType(14, Double)))

    Private _Info As String = ""
    Public Property Info As String
        Get
            Return _Info
        End Get
        Set(value As String)
            If _Info = value Then Return
            value = value.ReplaceLineEndings("")
            _Info = value
            LabInfo.Text = value
            LabInfo.Visibility = If(value = "", Visibility.Collapsed, Visibility.Visible)
        End Set
    End Property

    Private _Logo As String = ""
    Public Property Logo As String
        Get
            Return _Logo
        End Get
        Set(value As String)
            If _Logo = value Then Return
            _Logo = value

            If Not IsNothing(PathLogo) Then Children.Remove(PathLogo)

            If Not _Logo = "" Then
                If _Logo.StartsWithF("http", True) Then

                    PathLogo = New MyImage With {
                            .Tag = Me,
                            .IsHitTestVisible = LogoClickable,
                            .Source = _Logo,
                            .RenderTransformOrigin = New Point(0.5, 0.5),
                            .RenderTransform = New ScaleTransform With {.ScaleX = LogoScale, .ScaleY = LogoScale},
                            .SnapsToDevicePixels = True, .UseLayoutRounding = False}
                    RenderOptions.SetBitmapScalingMode(PathLogo, BitmapScalingMode.Linear)
                ElseIf _Logo.EndsWithF(".png", True) OrElse _Logo.EndsWithF(".jpg", True) OrElse _Logo.EndsWithF(".webp", True) Then

                    PathLogo = New Canvas With {
                            .Tag = Me,
                            .IsHitTestVisible = LogoClickable,
                            .Background = New MyBitmap(_Logo),
                            .RenderTransformOrigin = New Point(0.5, 0.5),
                            .RenderTransform = New ScaleTransform With {.ScaleX = LogoScale, .ScaleY = LogoScale},
                            .SnapsToDevicePixels = True, .UseLayoutRounding = False,
                            .HorizontalAlignment = HorizontalAlignment.Stretch, .VerticalAlignment = VerticalAlignment.Stretch
                    }
                    RenderOptions.SetBitmapScalingMode(PathLogo, BitmapScalingMode.Linear)
                Else

                    PathLogo = New Shapes.Path With {
                        .Tag = Me,
                        .IsHitTestVisible = LogoClickable, .HorizontalAlignment = HorizontalAlignment.Center, .VerticalAlignment = VerticalAlignment.Center, .Stretch = Stretch.Uniform,
                        .Data = (New GeometryConverter).ConvertFromString(_Logo),
                        .RenderTransformOrigin = New Point(0.5, 0.5),
                        .RenderTransform = New ScaleTransform With {.ScaleX = LogoScale, .ScaleY = LogoScale},
                        .SnapsToDevicePixels = False, .UseLayoutRounding = False}
                    PathLogo.SetBinding(Shapes.Path.FillProperty, New Binding("Foreground") With {.Source = Me})
                End If
                SetColumn(PathLogo, 2)
                SetRowSpan(PathLogo, 4)
                OnSizeChanged()
                Children.Add(PathLogo)

                If LogoClickable Then
                    AddHandler PathLogo.MouseLeave, Sub(sender, e) IsLogoDown = False
                    AddHandler PathLogo.MouseLeftButtonDown, Sub(sender, e) IsLogoDown = True
                    AddHandler PathLogo.MouseLeftButtonUp, Sub(sender, e) If IsLogoDown Then IsLogoDown = False : RaiseEvent LogoClick(sender.Tag, e)
                End If
            End If

            ColumnLogo.Width = New GridLength(If(_Logo = "", 0, 34) + If(Height < 40, 0, 4))
        End Set
    End Property
    Private _LogoScale As Double = 1
    Public Property LogoScale() As Double
        Get
            Return _LogoScale
        End Get
        Set(value As Double)
            _LogoScale = value
            If Not IsNothing(PathLogo) Then PathLogo.RenderTransform = New ScaleTransform With {.ScaleX = LogoScale, .ScaleY = LogoScale}
        End Set
    End Property

    Public Property LogoClickable As Boolean = False
    Private IsLogoDown As Boolean = False

    Public Enum CheckType
        None
        Clickable
        RadioBox
        CheckBox
    End Enum
    Private _Type As CheckType = CheckType.None
    Public Property Type As CheckType
        Get
            Return _Type
        End Get
        Set(value As CheckType)
            If _Type = value Then Return
            _Type = value

            ColumnCheck.Width = New GridLength(If(_Type = CheckType.None OrElse _Type = CheckType.Clickable, If(Height < 40, 4, 2), 6))

            If _Type = CheckType.None OrElse _Type = CheckType.Clickable Then

                If Not IsNothing(RectCheck) Then
                    Children.Remove(RectCheck)
                    RectCheck = Nothing
                End If
                SetChecked(False, False, False)
            Else

                If IsNothing(RectCheck) Then
                    RectCheck = New Border With {
                        .Width = 5, .Height = If(Checked, Double.NaN, 0), .CornerRadius = New CornerRadius(2, 2, 2, 2),
                        .VerticalAlignment = If(Checked, VerticalAlignment.Stretch, VerticalAlignment.Center),
                        .HorizontalAlignment = HorizontalAlignment.Left, .UseLayoutRounding = False, .SnapsToDevicePixels = False,
                        .Margin = If(Checked, New Thickness(-1, 6, 0, 6), New Thickness(-1, 0, 0, 0))}
                    RectCheck.SetResourceReference(Border.BackgroundProperty, "ColorBrush3")
                    SetRowSpan(RectCheck, 4)
                    Children.Add(RectCheck)
                End If
            End If
        End Set
    End Property

    Private Sub OnSizeChanged() Handles Me.SizeChanged
        ColumnCheck.Width = New GridLength(If(_Type = CheckType.None OrElse _Type = CheckType.Clickable, If(Height < 40, 4, 2), 6))
        ColumnLogo.Width = New GridLength(If(_Logo = "", 0, 34) + If(Height < 40, 0, 4))
        If PathLogo IsNot Nothing Then
            If _Logo.EndsWithF(".png", True) OrElse _Logo.EndsWithF(".jpg", True) OrElse _Logo.EndsWithF(".webp", True) Then
                PathLogo.Margin = New Thickness(4, 5, 3, 5)
            Else
                PathLogo.Margin = New Thickness(If(Height < 40, 6, 8), 8, If(Height < 40, 4, 6), 8)
            End If
        End If
        LabTitle.Margin = New Thickness(4, 0, 0, If(Height < 40, 0, 2))
    End Sub

    Private _Checked As Boolean = False
    Public Property Checked As Boolean
        Get
            Return _Checked
        End Get
        Set(value As Boolean)
            SetChecked(value, False, value <> _Checked)
        End Set
    End Property

    Public Sub SetChecked(value As Boolean, user As Boolean, anime As Boolean)
        Try

            Dim ChangedEventArgs As New RouteEventArgs(user)
            Dim RawValue = _Checked
            If Type = CheckType.RadioBox Then
                If IsInitialized AndAlso Not value = _Checked Then
                    _Checked = value
                    RaiseEvent Changed(Me, ChangedEventArgs)
                    If ChangedEventArgs.Handled Then
                        _Checked = RawValue
                        Return
                    End If
                End If
                _Checked = value
            Else
                If value = _Checked Then Return
                _Checked = value
                If IsInitialized Then
                    RaiseEvent Changed(Me, ChangedEventArgs)
                    If ChangedEventArgs.Handled Then
                        _Checked = RawValue
                        Return
                    End If
                End If
            End If
            If value Then
                Dim CheckEventArgs As New RouteEventArgs(user)
                RaiseEvent Check(Me, CheckEventArgs)
                If CheckEventArgs.Handled Then Return
            End If

            If Type = CheckType.RadioBox Then
                If IsNothing(Parent) Then Return
                Dim RadioboxList As New List(Of MyListItem)
                Dim CheckedCount As Integer = 0

                For Each Control In CType(Parent, Object).Children
                    Control = MyVirtualizingElement.TryInit(Control)
                    If TypeOf Control Is MyListItem AndAlso CType(Control, MyListItem).Type = CheckType.RadioBox Then
                        RadioboxList.Add(Control)
                        If Control.Checked Then CheckedCount += 1
                    End If
                Next

                Select Case CheckedCount
                    Case 0

                        RadioboxList(0).Checked = True
                    Case Is > 1

                        If Me.Checked Then

                            For Each Control As MyListItem In RadioboxList
                                If Control.Checked AndAlso Not Control.Equals(Me) Then Control.Checked = False
                            Next
                        Else

                            Dim FirstChecked = False
                            For Each Control As MyListItem In RadioboxList
                                If Control.Checked Then
                                    If FirstChecked Then
                                        Control.Checked = False
                                    Else
                                        FirstChecked = True
                                    End If
                                End If
                            Next
                        End If
                End Select
            End If

            If IsLoaded AndAlso AniControlEnabled = 0 AndAlso anime Then
                Dim Anim As New List(Of AniData)
                If Checked Then

                    If Not IsNothing(RectCheck) Then
                        Dim Delta = ActualHeight - RectCheck.ActualHeight - 12
                        Anim.Add(AaHeight(RectCheck, Delta * 0.4, 200,, New AniEaseOutFluent(AniEasePower.Weak)))
                        Anim.Add(AaHeight(RectCheck, Delta * 0.6, 300,, New AniEaseOutBack(AniEasePower.Weak)))
                        Anim.Add(AaOpacity(RectCheck, 1 - RectCheck.Opacity, 30))
                        RectCheck.VerticalAlignment = VerticalAlignment.Center
                        RectCheck.Margin = New Thickness(-1, 0, 0, 0)
                    End If
                    Anim.Add(AaColor(Me, ForegroundProperty, If(Height < 40, "ColorBrush3", "ColorBrush2"), 200))
                Else

                    If Not IsNothing(RectCheck) Then

                        Anim.Add(AaHeight(RectCheck, -RectCheck.ActualHeight, 120,, New AniEaseInFluent(AniEasePower.Weak)))
                        Anim.Add(AaOpacity(RectCheck, -RectCheck.Opacity, 70, 40))
                        RectCheck.VerticalAlignment = VerticalAlignment.Center
                    End If
                    Anim.Add(AaColor(Me, ForegroundProperty, "ColorBrush1", 120))
                End If
                AniStart(Anim, "MyListItem Checked " & Uuid)
            Else

                AniStop("MyListItem Checked " & Uuid)
                If Checked Then
                    If Not IsNothing(RectCheck) Then
                        RectCheck.Height = Double.NaN
                        RectCheck.Margin = New Thickness(-1, 6, 0, 6)
                        RectCheck.Opacity = 1
                        RectCheck.VerticalAlignment = VerticalAlignment.Stretch
                    End If
                    SetResourceReference(ForegroundProperty, If(Height < 40, "ColorBrush3", "ColorBrush2"))
                Else
                    If Not IsNothing(RectCheck) Then
                        RectCheck.Height = 0
                        RectCheck.Margin = New Thickness(-1, 0, 0, 0)
                        RectCheck.Opacity = 0
                        RectCheck.VerticalAlignment = VerticalAlignment.Center
                    End If
                    SetResourceReference(ForegroundProperty, "ColorBrush1")
                End If
            End If

        Catch ex As Exception
            Logger.Warn(ex, "设置 Checked 失败")
        End Try
    End Sub

    Public Property Foreground As Brush
        Get
            Return GetValue(ForegroundProperty)
        End Get
        Set(value As Brush)
            SetValue(ForegroundProperty, value)
        End Set
    End Property
    Public Shared ReadOnly ForegroundProperty As DependencyProperty = DependencyProperty.Register("Foreground", GetType(Brush), GetType(MyListItem), New PropertyMetadata(CType(Color1, SolidColorBrush)))

    Public ContentHandler As Action(Of MyListItem, EventArgs)

#End Region

#Region "点击"

    Private Sub Button_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonUp
        If Not IsMouseDown Then Return
        RaiseEvent Click(sender, e)
        If e.Handled Then Return

        If CustomEventService.GetEventType(sender) <> CustomEvent.EventType.None OrElse
           CustomEventService.GetEvents(sender).Events.Count > 0 Then
            RaiseCustomEvent()
            e.Handled = True
        End If
        If e.Handled Then Return

        Select Case Type
            Case CheckType.Clickable
                Logger.Info($"按下单击列表项：{Title}")
            Case CheckType.RadioBox
                Logger.Info($"按下单选列表项：{Title}")
                If Not Checked Then SetChecked(True, True, True)
            Case CheckType.CheckBox
                Logger.Info($"按下复选列表项（{Not Checked}）：{Title}")
                SetChecked(Not Checked, True, True)
        End Select
    End Sub

    Private IsMouseDown As Boolean = False
    Private Sub Button_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonDown
        If IsMouseDirectlyOver AndAlso Not Type = CheckType.None Then
            IsMouseDown = True
            If ButtonStack IsNot Nothing Then ButtonStack.IsHitTestVisible = False
        End If
    End Sub
    Private Sub Button_MouseLeave(sender As Object, e As Object) Handles Me.MouseLeave, Me.PreviewMouseLeftButtonUp
        IsMouseDown = False
        If ButtonStack IsNot Nothing Then ButtonStack.IsHitTestVisible = True
    End Sub

#End Region

    Private StateLast As String
    Public IsMouseOverAnimationEnabled As Boolean = True
    Public Sub RefreshColor(sender As Object, e As EventArgs) Handles Me.MouseEnter, Me.MouseLeave, Me.PreviewMouseLeftButtonDown, Me.PreviewMouseLeftButtonUp

        If ContentHandler IsNot Nothing Then
            ContentHandler(Me, e)
            ContentHandler = Nothing
        End If

        Dim StateNew As String, Time As Integer
        If IsMouseDown AndAlso Not (Type = CheckType.RadioBox AndAlso Checked) Then
            StateNew = "MouseDown"
            Time = 120
        Else
            If IsMouseOver AndAlso IsMouseOverAnimationEnabled Then
                StateNew = "MouseOver"
                Time = 120
            Else
                StateNew = "Idle"
                Time = 180
            End If
        End If
        If StateLast = StateNew Then Return
        StateLast = StateNew

        If IsLoaded AndAlso AniControlEnabled = 0 Then

            Dim Ani As New List(Of AniData)
            If IsMouseOver AndAlso IsMouseOverAnimationEnabled Then
                If ButtonStack IsNot Nothing Then
                    Ani.Add(AaOpacity(ButtonStack, 1 - ButtonStack.Opacity, Time * 0.7, Time * 0.3))
                    Ani.Add(AaDouble(Sub(i) ColumnPaddingRight.Width = New GridLength(Math.Max(0, ColumnPaddingRight.Width.Value + i)),
                                     Math.Max(MinPaddingRight, 5 + Buttons.Count * 25) - ColumnPaddingRight.Width.Value, Time * 0.3, Time * 0.7))
                End If
                Ani.AddRange({
                             AaColor(RectBack, Border.BackgroundProperty, If(IsMouseDown, "ColorBrush6", "ColorBrushBg1"), Time),
                             AaOpacity(RectBack, 1 - RectBack.Opacity, Time,, New AniEaseOutFluent)
                         })
                If IsScaleAnimationEnabled Then
                    Ani.Add(AaScaleTransform(RectBack, 1 - CType(RectBack.RenderTransform, ScaleTransform).ScaleX, Time * 1.6,, New AniEaseOutFluent))
                    If IsMouseDown Then
                        Ani.Add(AaScaleTransform(Me, 0.98 - CType(Me.RenderTransform, ScaleTransform).ScaleX, Time * 0.9,, New AniEaseOutFluent))
                    Else
                        Ani.Add(AaScaleTransform(Me, 1 - CType(Me.RenderTransform, ScaleTransform).ScaleX, Time * 1.2,, New AniEaseOutFluent))
                    End If
                End If
            Else
                If ButtonStack IsNot Nothing Then
                    Ani.Add(AaOpacity(ButtonStack, -ButtonStack.Opacity, Time * 0.4))
                    Ani.Add(AaDouble(Sub(i) ColumnPaddingRight.Width = New GridLength(Math.Max(0, ColumnPaddingRight.Width.Value + i)),
                                     MinPaddingRight - ColumnPaddingRight.Width.Value, Time * 0.4))
                End If
                Ani.Add(AaOpacity(RectBack, -RectBack.Opacity, Time))
                If IsScaleAnimationEnabled Then
                    Ani.AddRange({
                        AaColor(RectBack, Border.BackgroundProperty, If(IsMouseDown, "ColorBrush6", "ColorBrush7"), Time),
                        AaScaleTransform(Me, 1 - CType(RenderTransform, ScaleTransform).ScaleX, Time * 3,, New AniEaseOutFluent),
                        AaScaleTransform(RectBack, 0.996 - CType(RectBack.RenderTransform, ScaleTransform).ScaleX, Time,, New AniEaseOutFluent),
                        AaScaleTransform(RectBack, -0.246, 1,,, True)
                    })
                End If
            End If
            AniStart(Ani, "ListItem Color " & Uuid)
        Else

            If IsMouseOver AndAlso IsMouseOverAnimationEnabled Then
                If ButtonStack IsNot Nothing Then
                    ButtonStack.Opacity = 1
                    ColumnPaddingRight.Width = New GridLength(Math.Max(MinPaddingRight, 5 + Buttons.Count * 25))
                End If

                RectBack.Background = ColorBg1
                RectBack.Opacity = 1
                RectBack.RenderTransform = New ScaleTransform(1, 1)
                Me.RenderTransform = New ScaleTransform(1, 1)
            Else
                If ButtonStack IsNot Nothing Then
                    ButtonStack.Opacity = 0
                    ColumnPaddingRight.Width = New GridLength(MinPaddingRight)
                End If
                Me.RenderTransform = New ScaleTransform(1, 1)
                If _RectBack IsNot Nothing Then
                    If IsScaleAnimationEnabled Then RectBack.RenderTransform = New ScaleTransform(0.75, 0.75)
                    RectBack.Background = Color7
                    RectBack.Opacity = 0
                End If
            End If
            AniStop("ListItem Color " & Uuid)
        End If
    End Sub

    Private Sub MyListItem_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If Checked Then
            SetResourceReference(ForegroundProperty, If(Height < 40, "ColorBrush3", "ColorBrush2"))
        Else
            SetResourceReference(ForegroundProperty, "ColorBrush1")
        End If
        ColumnPaddingRight.Width = New GridLength(MinPaddingRight)
    End Sub
    Public Overrides Function ToString() As String
        Return Title
    End Function

End Class
