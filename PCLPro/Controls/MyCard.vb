Public Class MyCard

    Inherits Grid
    Implements IGlassPanel
    Private ReadOnly MainGrid As Grid
    Public ReadOnly Property MainChrome As MyDropShadow
    Private ReadOnly MainBorder As Border
    Public Property BorderChild As UIElement
        Get
            Return MainBorder.Child
        End Get
        Set(value As UIElement)
            MainBorder.Child = value
        End Set
    End Property
    Private _MainTextBlock As TextBlock
    Public Property MainTextBlock As TextBlock
        Get
            Init()
            Return _MainTextBlock
        End Get
        Set(value As TextBlock)
            _MainTextBlock = value
        End Set
    End Property
    Private _MainSwap As Shapes.Path
    Public Property MainSwap As Shapes.Path
        Get
            Init()
            Return _MainSwap
        End Get
        Set(value As Shapes.Path)
            _MainSwap = value
        End Set
    End Property

    Public Uuid As Integer = GetUuid()
    Public ReadOnly Property Inlines As InlineCollection
        Get
            Return MainTextBlock.Inlines
        End Get
    End Property
    Public Property CornerRadius As CornerRadius
        Get
            Return MainChrome.CornerRadius
        End Get
        Set(value As CornerRadius)
            MainChrome.CornerRadius = value
            MainBorder.CornerRadius = value
        End Set
    End Property
    Public Property Title As String
        Get
            Return GetValue(TitleProperty)
        End Get
        Set(value As String)
            SetValue(TitleProperty, value)
            If _MainTextBlock IsNot Nothing Then MainTextBlock.Text = value
        End Set
    End Property
    Public Shared ReadOnly TitleProperty As DependencyProperty = DependencyProperty.Register("Title", GetType(String), GetType(MyCard), New PropertyMetadata(""))

    Public Sub New()
        MainChrome = New MyDropShadow With {.Opacity = DropShadowIdleOpacity}
        MainChrome.SetResourceReference(MyDropShadow.ColorProperty, "ColorObject1")
        Children.Insert(0, MainChrome)
        MainBorder = New Border With {.IsHitTestVisible = False}
        Children.Insert(1, MainBorder)
        ApplyStyle()
        MainGrid = New Grid
        Children.Add(MainGrid)
        SyncLock AllCards
            AllCards.Add(New WeakReference(Me))
        End SyncLock
    End Sub

    Private Shared ReadOnly AllCards As New List(Of WeakReference)

    Public Shared Sub ApplyAllStyles()
        SyncLock AllCards
            Dim Alive As New List(Of WeakReference)
            For Each Ref As WeakReference In AllCards
                Dim Card As MyCard = TryCast(Ref.Target, MyCard)
                If Card Is Nothing Then Continue For
                Alive.Add(Ref)
                Try
                    Card.ApplyStyle()
                Catch ex As Exception
                    Logger.Warn(ex, "刷新卡片样式失败")
                End Try
            Next
            AllCards.Clear()
            AllCards.AddRange(Alive)
        End SyncLock
    End Sub

    Private FrostLayer As Border
    Private IsGlassStyle As Boolean = False

    Private GlassBrush As ImageBrush = Nothing

    Public Sub ApplyStyle()

        IsGlassStyle = Settings.Get(Of Integer)("UiUiStyle") = 1
        If IsGlassStyle Then

            MainChrome.Margin = New Thickness(-9, -4, -9, -11)
            MainChrome.ShadowRadius = 14
            MainChrome.CornerRadius = New CornerRadius(GlassCornerRadius)
            MainBorder.CornerRadius = New CornerRadius(Math.Max(0, GlassCornerRadius - 1))
            MainBorder.BorderThickness = New Thickness(1)
            MainBorder.BorderBrush = ModGlass.GlassCardEdgeBrush()
            If FrostLayer Is Nothing Then
                FrostLayer = New Border With {.IsHitTestVisible = False}
                Children.Insert(2, FrostLayer)
            End If
            FrostLayer.CornerRadius = New CornerRadius(Math.Max(0, GlassCornerRadius - 1))
            FrostLayer.Visibility = Visibility.Visible

            GlassApplyBackdrop()
        Else

            MainChrome.Margin = New Thickness(-3, -3, -3, -3 - GetWPFSize(1))
            MainChrome.ShadowRadius = 3
            MainChrome.CornerRadius = New CornerRadius(5)
            MainBorder.Background = New SolidColorBrush(Color.FromArgb(245, 255, 255, 255))
            MainBorder.CornerRadius = New CornerRadius(5)
            MainBorder.BorderThickness = New Thickness(0)
            MainBorder.BorderBrush = Nothing
            If FrostLayer IsNot Nothing Then FrostLayer.Visibility = Visibility.Collapsed
        End If
        LastGlassRect = Rect.Empty
    End Sub

    Private Sub MyCard_LayoutUpdated(sender As Object, e As EventArgs) Handles Me.LayoutUpdated
        If Not IsGlassStyle OrElse FrmMain Is Nothing OrElse FrmMain.ImgBack Is Nothing Then Return
        Try

            If ActualWidth < 1 OrElse ActualHeight < 1 Then Return
            Dim Offset As Point = TranslatePoint(New Point(0, 0), FrmMain.ImgBack)
            If Double.IsNaN(Offset.X) OrElse Double.IsNaN(Offset.Y) Then Return
            Dim Now As New Rect(Offset.X, Offset.Y, ActualWidth, ActualHeight)
            If Now = LastGlassRect Then Return
            LastGlassRect = Now

            GlassRefreshBackdrop()
        Catch
        End Try
    End Sub
    Private LastGlassRect As Rect = Rect.Empty
    Private LastGlassCheck As Integer = 0
    Public Sub GlassRefreshBackdrop() Implements IGlassPanel.GlassRefreshBackdrop
        If Not IsGlassStyle Then Return
        GlassApplyBackdrop()
    End Sub

    Private Sub GlassApplyBackdrop()
        Try
            If GlassBrush Is Nothing Then GlassBrush = ModGlass.CreateSurfaceBrush()

            If Not Object.ReferenceEquals(MainBorder.Background, GlassBrush) AndAlso Not ModGlass.IsVideoBackdrop Then
                MainBorder.Background = GlassBrush
            End If

            If ModGlass.AlignSurfaceBrush(GlassBrush, MainBorder, False, True) Then
                If FrostLayer IsNot Nothing Then FrostLayer.Background = ModGlass.GlassCardTint()
            ElseIf ModGlass.IsVideoBackdrop Then

                GlassBrush.ImageSource = Nothing
                MainBorder.Background = ModGlass.GlassVideoTint()
                If FrostLayer IsNot Nothing Then FrostLayer.Background = Nothing
                GlassBrush = Nothing
            Else
                GlassBrush.ImageSource = Nothing
                If FrostLayer IsNot Nothing Then FrostLayer.Background = ModGlass.GlassCardTint()
            End If
        Catch
        End Try
    End Sub
    Private IsLoad As Boolean = False
    Private Sub Init() Handles Me.Loaded
        If IsLoad Then Return
        IsLoad = True

        If MainTextBlock Is Nothing Then
            MainTextBlock = New TextBlock With {.HorizontalAlignment = HorizontalAlignment.Left, .VerticalAlignment = VerticalAlignment.Top, .Margin = New Thickness(15, 12, 0, 0), .FontWeight = FontWeights.Bold, .IsHitTestVisible = False}
            MainTextBlock.SetResourceReference(TextBlock.FontSizeProperty, "UiFontSize")
            MainTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush1")
            MainTextBlock.SetBinding(TextBlock.TextProperty, New Binding("Title") With {.Source = Me, .Mode = BindingMode.OneWay})
            MainGrid.Children.Add(MainTextBlock)
        End If
        If CanSwap OrElse SwapControl IsNot Nothing Then

            If SwapControl Is Nothing Then
                For Each Child As UIElement In Children
                    If Child Is MainChrome OrElse Child Is MainBorder OrElse Child Is MainGrid OrElse Child Is FrostLayer Then Continue For
                    SwapControl = Child
                    Exit For
                Next
            End If
            MainSwap = New Shapes.Path With {.HorizontalAlignment = HorizontalAlignment.Right, .Stretch = Stretch.Uniform, .Height = 6, .Width = 10, .VerticalAlignment = VerticalAlignment.Top, .Margin = New Thickness(0, 17, 16, 0), .Data = New GeometryConverter().ConvertFromString("M2,4 l-2,2 10,10 10,-10 -2,-2 -8,8 -8,-8 z"), .RenderTransform = New RotateTransform(180), .RenderTransformOrigin = New Point(0.5, 0.5)}
            MainSwap.SetResourceReference(Shapes.Path.FillProperty, "ColorBrush1")
            MainGrid.Children.Add(MainSwap)
        End If

        If IsSwapped AndAlso SwapControl IsNot Nothing Then
            MainSwap.RenderTransform = New RotateTransform(If(SwapLogoRight, 270, 0))
            SwapControl.Visibility = Visibility.Collapsed

            Dim RawUseAnimation As Boolean = UseAnimation
            UseAnimation = False
            Height = SwapedHeight
            AniStop("MyCard Height " & Uuid)
            IsHeightAnimating = False
            RunInUi(Sub() UseAnimation = RawUseAnimation, True)
        End If
    End Sub

    Private ContentBuilder As Action(Of Panel)
    Public Sub SetContentBuilder(Builder As Action(Of Panel))
        ContentBuilder = Builder
    End Sub
    Public Sub SetContentBuilder(Of T)(Items As IEnumerable(Of T), ItemBuilder As Func(Of T, UIElement))
        SetContentBuilder(
        Sub(Stack)
            For Each Item In Items
                Stack.Children.Add(ItemBuilder(Item))
            Next
            Stack.Children.Add(New FrameworkElement With {.Height = 18})
        End Sub)
    End Sub

    Public Sub BuildContent()
        If ContentBuilder Is Nothing Then Return
        Dim Stack = TryCast(SwapControl, Panel)
        If Stack Is Nothing Then Return
        Try
            ContentBuilder(Stack)
            ContentBuilder = Nothing
        Catch
            Stack.Children.Clear()
            Throw
        End Try
        TriggerForceResize()
    End Sub

    Private Const DropShadowIdleOpacity As Double = 0.07
    Private Const DropShadowHoverOpacity As Double = 0.4
    Public Property HasMouseAnimation As Boolean = True
    Private Sub MyCard_MouseEnter(sender As Object, e As MouseEventArgs) Handles Me.MouseEnter
        If Not HasMouseAnimation Then Return
        Dim AniList As New List(Of AniData)
        If Not IsNothing(MainTextBlock) Then AniList.Add(AaColor(MainTextBlock, TextBlock.ForegroundProperty, "ColorBrush2", 90))
        If Not IsNothing(MainSwap) Then AniList.Add(AaColor(MainSwap, Shapes.Path.FillProperty, "ColorBrush2", 90))
        AniList.AddRange({
            AaColor(MainChrome, MyDropShadow.ColorProperty, "ColorObject4", 90),
            AaOpacity(MainChrome, DropShadowHoverOpacity - MainChrome.Opacity, 90)
        })
        AniStart(AniList, "MyCard Mouse " & Uuid)
    End Sub
    Private Sub MyCard_MouseLeave(sender As Object, e As MouseEventArgs) Handles Me.MouseLeave
        If Not HasMouseAnimation Then Return
        Dim AniList As New List(Of AniData)
        If Not IsNothing(MainTextBlock) Then AniList.Add(AaColor(MainTextBlock, TextBlock.ForegroundProperty, "ColorBrush1", 90))
        If Not IsNothing(MainSwap) Then AniList.Add(AaColor(MainSwap, Shapes.Path.FillProperty, "ColorBrush1", 90))
        AniList.AddRange({
            AaColor(MainChrome, MyDropShadow.ColorProperty, "ColorObject1", 90),
            AaOpacity(MainChrome, DropShadowIdleOpacity - MainChrome.Opacity, 90)
        })
        AniStart(AniList, "MyCard Mouse " & Uuid)
    End Sub

#Region "高度改变动画"

    Public Property UseAnimation As Boolean = True
    Private IsHeightAnimating As Boolean = False
    Private ActualUsedHeight As Double
    Private Sub MySizeChanged(sender As Object, e As SizeChangedEventArgs) Handles Me.SizeChanged
        If Not UseAnimation Then Return
        Dim DeltaHeight As Double = If(IsSwapped, SwapedHeight, e.NewSize.Height) - e.PreviousSize.Height

        If e.PreviousSize.Height = 0 OrElse IsHeightAnimating OrElse Math.Abs(DeltaHeight) < 1 OrElse ActualHeight = 0 Then Return
        StartHeightAnimation(DeltaHeight, e.PreviousSize.Height, False)
    End Sub
    Private Sub StartHeightAnimation(Delta As Double, PreviousHeight As Double, IsLoadAnimation As Boolean)
        If IsHeightAnimating OrElse FrmMain Is Nothing Then Return

        Dim AnimList As New List(Of AniData)
        Dim AbsDelta = Math.Abs(Delta)

        If AbsDelta <= 800 Then

            AnimList.Add(AaHeight(Me, Delta, 150,, New AniEaseOutFluent(AniEasePower.ExtraStrong)))
        Else
            Dim EaseLength As Integer, EaseTime As Integer
            Dim InitSpeed As Integer
            If Delta < 0 AndAlso AbsDelta - EaseLength > 5000 * 0.1 Then

                EaseLength = 200
                EaseTime = 150
                InitSpeed = (AbsDelta - EaseLength) / 0.1
            ElseIf Delta > 0 AndAlso AbsDelta - EaseLength > 5000 * 0.6 Then

                InitSpeed = 5000
                EaseLength = AbsDelta - InitSpeed * 0.3
                EaseTime = 400
            Else

                EaseLength = 150
                EaseTime = 200
                InitSpeed = 4000
            End If

            AnimList.Add(AaHeight(Me, (AbsDelta - EaseLength) * Math.Sign(Delta),
                (AbsDelta - EaseLength) / InitSpeed * 1000))

            AnimList.Add(AaHeight(Me, EaseLength * Math.Sign(Delta),
                EaseTime,, New AniEaseOutFluentWithInitial(InitSpeed, EaseTime / 1000, EaseLength), True))
        End If

        AnimList.Add(AaCode(
        Sub()
            IsHeightAnimating = False
            Height = ActualUsedHeight
            If IsSwapped AndAlso SwapControl IsNot Nothing Then SwapControl.Visibility = Visibility.Collapsed
        End Sub,, True))
        AniStart(AnimList, "MyCard Height " & Uuid)
        IsHeightAnimating = True
        ActualUsedHeight = If(IsSwapped, SwapedHeight, Height)
        Height = PreviousHeight
    End Sub

    Public Sub TriggerForceResize()
        Height = If(IsSwapped, SwapedHeight, Double.NaN)
        AniStop("MyCard Height " & Uuid)
        IsHeightAnimating = False
    End Sub

#End Region

#Region "折叠"

    Public SwapControl As FrameworkElement
    Public Property CanSwap As Boolean = False

    Public Property IsSwapped As Boolean
        Get
            Return _IsSwapped
        End Get
        Set(value As Boolean)
            If _IsSwapped = value Then Return
            _IsSwapped = value
            If SwapControl Is Nothing Then Return

            If Not IsSwapped Then BuildContent()

            If Not IsLoaded Then Return

            SwapControl.Visibility = Visibility.Visible
            TriggerForceResize()
            UpdateCursorStyle()

            AniStart(AaRotateTransform(MainSwap, If(_IsSwapped, If(SwapLogoRight, 270, 0), 180) - CType(MainSwap.RenderTransform, RotateTransform).Angle, 250,, New AniEaseOutFluent(AniEasePower.ExtraStrong)), "MyCard Swap " & Uuid, True)
        End Set
    End Property
    Private _IsSwapped As Boolean = False
    <Obsolete("IsSwaped 存在拼写错误，请换用 IsSwapped 属性。")>
    Public Property IsSwaped As Boolean
        Get
            Return GetValue(IsSwapedProperty)
        End Get
        Set(value As Boolean)
            SetValue(IsSwapedProperty, value)
        End Set
    End Property
    Public Shared ReadOnly IsSwapedProperty As DependencyProperty = DependencyProperty.Register("IsSwaped", GetType(Boolean), GetType(MyCard), New PropertyMetadata(False,
    Sub(sender, e) If sender IsNot Nothing AndAlso TypeOf sender Is MyCard Then CType(sender, MyCard).IsSwapped = CType(e.NewValue, Boolean)))

    Public Property SwapLogoRight As Boolean = False
    Private IsSwapMouseDown As Boolean = False
    Private IsCustomMouseDown As Boolean = False
    Public Event PreviewSwap(sender As Object, e As RouteEventArgs)
    Public Event Swap(sender As Object, e As RouteEventArgs)
    Public Const SwapedHeight As Integer = 40
    Private Sub MyCard_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonDown
        Dim Pos As Double = Mouse.GetPosition(Me).Y
        If Not IsSwapped AndAlso
            (Pos > If(IsSwapped, SwapedHeight, SwapedHeight - 6) OrElse (Pos = 0 AndAlso Not IsMouseDirectlyOver)) Then Return
        IsCustomMouseDown = True
        If Not IsSwapped AndAlso
            (SwapControl Is Nothing OrElse Pos > If(IsSwapped, SwapedHeight, SwapedHeight - 6) OrElse (Pos = 0 AndAlso Not IsMouseDirectlyOver)) Then Return
        IsSwapMouseDown = True
    End Sub
    Private Sub MyCard_MouseLeftButtonUp() Handles Me.MouseLeftButtonUp
        If Not IsCustomMouseDown Then Return
        IsCustomMouseDown = False
        RaiseCustomEvent()

        If Not IsSwapMouseDown Then Return
        IsSwapMouseDown = False

        Dim Pos As Double = Mouse.GetPosition(Me).Y
        If Not IsSwapped AndAlso
            (SwapControl Is Nothing OrElse Pos > If(IsSwapped, SwapedHeight, SwapedHeight - 6) OrElse (Pos = 0 AndAlso Not IsMouseDirectlyOver)) Then Return

        Dim e = New RouteEventArgs(True)
        RaiseEvent PreviewSwap(Me, e)
        If e.Handled Then
            IsSwapMouseDown = False
            Return
        End If

        IsSwapped = Not IsSwapped
        Logger.Info($"{If(IsSwapped, "折叠卡片", "展开卡片")}{If(Title Is Nothing, "", "：" & Title)}")
        RaiseEvent Swap(Me, e)
    End Sub
    Private Sub MyCard_MouseLeave_Swap(sender As Object, e As MouseEventArgs) Handles Me.MouseLeave
        IsSwapMouseDown = False
    End Sub
    Private Sub UpdateCursorStyle() Handles Me.MouseMove
        If _MainSwap Is Nothing Then Cursor = Nothing : Return
        Dim Pos As Double = Mouse.GetPosition(Me).Y
        Cursor = If(MainSwap.Visibility = Visibility.Visible AndAlso (IsSwapped OrElse (Pos <= SwapedHeight - 6 AndAlso Not (Pos = 0 AndAlso Not IsMouseDirectlyOver))),
            Cursors.Hand, Nothing)
    End Sub

#End Region

End Class
Partial Public Module ModAnimation
    Public Sub AniDispose(Control As MyCard, RemoveFromChildren As Boolean, Optional CallBack As ParameterizedThreadStart = Nothing)
        If Control.IsHitTestVisible Then
            Control.IsHitTestVisible = False
            AniStart({
                AaScaleTransform(Control, -0.08, 200,, New AniEaseInFluent),
                AaOpacity(Control, -1, 200,, New AniEaseOutFluent),
                AaHeight(Control, -Control.ActualHeight, 150, 100, New AniEaseOutFluent),
                AaCode(
                Sub()
                    If RemoveFromChildren Then
                        If Control.Parent Is Nothing Then Return
                        CType(Control.Parent, Object).Children.Remove(Control)
                    Else
                        Control.Visibility = Visibility.Collapsed
                    End If
                    If CallBack IsNot Nothing Then CallBack(Control)
                End Sub,, True)
            }, "MyCard Dispose " & Control.Uuid)
        Else
            If RemoveFromChildren Then
                If Control.Parent Is Nothing Then Return
                CType(Control.Parent, Object).Children.Remove(Control)
            Else
                Control.Visibility = Visibility.Collapsed
            End If
            If CallBack IsNot Nothing Then CallBack(Control)
        End If
    End Sub
End Module
