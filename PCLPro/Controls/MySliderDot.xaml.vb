Public Class MySliderDot

    Public Uuid As Integer = GetUuid()
    Public Event Change(sender As Object, user As Boolean)
    Public Event PreviewChange(sender As Object, e As RouteEventArgs)

    Private _MaxValue As Integer = 100
    Public Property MaxValue As Integer
        Get
            Return _MaxValue
        End Get
        Set(value As Integer)
            If value = _MaxValue Then Exit Property
            _MaxValue = value
            RefreshHeight(Nothing, Nothing)
        End Set
    End Property
    Private ChangeByKey As Boolean = False
    Private _Value As Integer = 0
    Public Property Value As Integer
        Get
            Return _Value
        End Get
        Set(newValue As Integer)
            Try

                newValue = MathRange(newValue, 0, MaxValue)
                If _Value = newValue Then Exit Property

                Dim OldValue = _Value
                _Value = newValue
                If AniControlEnabled = 0 Then
                    Dim e = New RouteEventArgs(False)
                    RaiseEvent PreviewChange(Me, e)
                    If e.Handled Then
                        _Value = OldValue
                        Exit Property
                    End If
                End If

                If IsLoaded AndAlso AniControlEnabled = 0 Then
                    If ActualHeight < 16 Then Exit Property
                    Dim NewHeight As Double = _Value / MaxValue * (ActualHeight - 16)
                    Dim DeltaProcess As Double = Math.Abs(LineFore.Height / (ActualHeight - 16) - _Value / MaxValue)
                    Dim Time As Double = (1 - Math.Pow(1 - DeltaProcess, 3)) * 400 + If(ChangeByKey, 100, 0)
                    AniStart({
                            AaHeight(LineFore, Math.Max(0, NewHeight + If(NewHeight < 0.5, 0, 0.5)) - LineFore.Height, Time,, If(Time > 50, New AniEaseOutFluent, New AniEaseLinear)),
                            AaHeight(LineBack, Math.Max(0, ActualHeight - 16 - NewHeight + If(ActualHeight - 16 - NewHeight < 0.5, 0, 0.5)) - LineBack.Height, Time,, If(Time > 50, New AniEaseOutFluent, New AniEaseLinear)),
                            AaY(ShapeDot, NewHeight - ShapeDot.Margin.Top, Time,, If(Time > 50, New AniEaseOutFluent, New AniEaseLinear))
                         }, "MySlider Progress " & Uuid)
                Else
                    RefreshHeight(Nothing, Nothing)
                End If
                If AniControlEnabled = 0 Then RaiseEvent Change(Me, False)

            Catch ex As Exception
                Logger.Warning(ex, "滑动条进度改变出错", LogLevel.Hint)
            End Try
        End Set
    End Property
    Private Sub RefreshHeight(sender As Object, e As SizeChangedEventArgs) Handles Me.SizeChanged
        If Not IsNothing(e) Then PanMain.Height = e.NewSize.Height
        AniStop("MySlider Progress " & Uuid)
        Dim NewHeight As Double = _Value / MaxValue * (ActualHeight - 16)
        LineFore.Height = Math.Max(0, NewHeight + If(NewHeight < 0.5, 0, 0.5))
        LineBack.Height = Math.Max(0, ActualHeight - 16 - NewHeight + If(ActualHeight - 16 - NewHeight < 0.5, 0, 0.5))
        SetTop(ShapeDot, NewHeight)
    End Sub

    Public GetHintText As [Delegate]
    Private Sub DragStart(sender As Object, e As MouseButtonEventArgs) Handles ShapeDot.MouseLeftButtonDown
        e.Handled = True
        DragControl = Me
        AniStart({
                 AaScaleTransform(ShapeDot, 2 - CType(ShapeDot.RenderTransform, ScaleTransform).ScaleX, 100,, New AniEaseOutFluent)
            }, "MySlider Scale " & Uuid)
    End Sub
    Public Sub DragDoing()
        Dim Percent As Double = MathRange((Mouse.GetPosition(PanMain).Y - 8) / (ActualHeight - 16), 0, 1)
        Dim NewValue As Integer = Percent * MaxValue
        If Not NewValue = Value Then Value = NewValue
    End Sub
    Public Sub DragStop()
        RefreshColor()
        AniStart({
                 AaScaleTransform(ShapeDot, 2.3 - CType(ShapeDot.RenderTransform, ScaleTransform).ScaleX, 200,, New AniEaseOutFluent)
            }, "MySlider Scale " & Uuid)
        If Value > 0 Then
            Value = MaxValue
        Else
            FrmMain.SliderDrag_Finish()
        End If
    End Sub

    Private Sub RefreshColor() Handles Me.IsEnabledChanged, Me.MouseEnter, Me.MouseLeave

    End Sub

End Class
