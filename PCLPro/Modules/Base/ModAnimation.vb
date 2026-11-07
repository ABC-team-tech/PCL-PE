Imports System.Windows.Media.Effects

Public Module ModAnimation

#Region "声明"

    Public ReadOnly Property AniSpeed As Double
        Get
            Dim Setting As Integer = Settings.Get(Of Integer)("SystemDebugAnim")
            If Setting >= 30 Then Return 200
            Dim Speed As Double = (Setting * 0.1 + 0.1).Clamp(0.1, 3)

            Select Case Settings.Get(Of Integer)("UiAnimationSpeed")
                Case 0
                    Return Speed * 0.6
                Case 2
                    Return Speed * 1.5
                Case 3
                    Return 200
                Case Else
                    Return Speed
            End Select
        End Get
    End Property

    Public AniGroups As New Dictionary(Of String, AniGroupEntry)
    Public Class AniGroupEntry
        Public Data As List(Of AniData)
        Public StartTick As Long
        Public Uuid As Integer = GetUuid()
    End Class

    Private AniLastTick As Long

    Public AniRunning As Boolean = False
    Private _AniControlEnabled As Integer = 0
    Private ReadOnly AniControlEnabledLock As New Object

    Public Property AniControlEnabled() As Integer
        Get
            Return _AniControlEnabled
        End Get
        Set(value As Integer)
            SyncLock AniControlEnabledLock
                _AniControlEnabled = value
            End SyncLock
        End Set
    End Property

#End Region

#Region "类与枚举"

    Public Structure AniData

        Public TypeMain As AniType

        Public TypeSub As AniTypeSub

        Public TimeTotal As Integer

        Public TimeFinished As Double

        Public TimePercent As Double

        Public IsAfter As Boolean

        Public Ease As AniEase

        Public Obj As Object

        Public Value As Object

        Public ValueLast As Object

        Public Overrides Function ToString() As String
            Return TypeMain.ToString() & " | " & TimeFinished & "/" & TimeTotal & "(" & Math.Round(TimePercent * 100) & "%)" & If(Obj Is Nothing, "", " | " & Obj.ToString & "(" & Obj.GetType.Name & ")")
        End Function

    End Structure

    Public Enum AniType

        Number

        Color

        Scale

        TextAppear

        Code

        ScaleTransform

        RotateTransform
    End Enum

    Public Enum AniTypeSub
        X
        Y
        Width
        Height
        Opacity
        Value
        Radius
        BorderThickness
        StrokeThickness
        TranslateX
        TranslateY
        [Double]
        DoubleParam
        GridLengthWidth
    End Enum

#End Region

#Region "种类"

    Public Function AaX(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.X,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaY(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.Y,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaWidth(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.Width,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaHeight(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.Height,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaOpacity(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.Opacity,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaValue(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.Value,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaRadius(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.Radius,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaBorderThickness(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.BorderThickness,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaStrokeThickness(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.StrokeThickness,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaGridLengthWidth(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.GridLengthWidth,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaDouble(Obj As Object, Prop As DependencyProperty, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.Double, .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = {Obj, Prop, ""}, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaDouble(Lambda As ParameterizedThreadStart, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.DoubleParam, .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Lambda, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaColor(Obj As FrameworkElement, Prop As DependencyProperty, Value As MyColor, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Color, .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = {Obj, Prop, ""}, .Value = Value, .IsAfter = After, .TimeFinished = -Delay, .ValueLast = New MyColor(0, 0, 0, 0)}
    End Function

    Public Function AaColor(Obj As FrameworkElement, Prop As DependencyProperty, Res As String, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Color, .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = {Obj, Prop, Res}, .Value = New MyColor(Application.Current.FindResource(Res)) - New MyColor(Obj.GetValue(Prop)), .IsAfter = After, .TimeFinished = -Delay, .ValueLast = New MyColor(0, 0, 0, 0)}
    End Function

    Public Function AaScale(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False, Optional Absolute As Boolean = False) As AniData
        Dim ChangeRect As MyRect
        If Absolute Then
            ChangeRect = New MyRect(-0.5 * Value, -0.5 * Value, Value, Value)
        Else
            ChangeRect = New MyRect(-0.5 * Obj.ActualWidth * Value, -0.5 * Obj.ActualHeight * Value, Obj.ActualWidth * Value, Obj.ActualHeight * Value)
        End If
        Return New AniData With {.TypeMain = AniType.Scale, .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = ChangeRect, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaTextAppear(Obj As Object, Optional Hide As Boolean = False, Optional TimePerText As Boolean = True, Optional Time As Integer = 70, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData

        Return New AniData With {.TypeMain = AniType.TextAppear, .Ease = If(Ease, New AniEaseLinear), .TimeTotal = If(TimePerText, Time * If(TypeOf Obj Is TextBlock, Obj.Text, Obj.Context.ToString).ToString.Length, Time), .Obj = Obj, .Value = {If(TypeOf Obj Is TextBlock, Obj.Text, Obj.Context.ToString), Hide}, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaCode(Code As ThreadStart, Optional Delay As Integer = 0, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Code,
                                   .TimeTotal = 1, .Value = Code, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaScaleTransform(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.ScaleTransform, .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaRotateTransform(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.RotateTransform, .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaTranslateX(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.TranslateX,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaTranslateY(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.TranslateY,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    Public Function AaStack(Stack As StackPanel, Optional Time As Integer = 100, Optional Delay As Integer = 25) As List(Of AniData)
        Dim Result = New List(Of AniData)
        Dim AniDelay As Integer = 0
        For Each Item In Stack.Children
            Item.Opacity = 0
            Result.Add(AaOpacity(Item, 1, Time, AniDelay))
            AniDelay += Delay
        Next
        Return Result
    End Function

#End Region

#Region "缓动函数"

    Public Enum AniEasePower As Integer
        Weak = 2
        Middle = 3
        Strong = 4
        ExtraStrong = 5
    End Enum

    Public MustInherit Class AniEase

        Public MustOverride Function GetValue(t As Double) As Double

        Public Overridable Function GetDelta(t1 As Double, t0 As Double) As Double
            Return GetValue(t1) - GetValue(t0)
        End Function
    End Class

    Public Class AniEaseInout
        Inherits AniEase
        Private ReadOnly EaseIn As AniEase, EaseOut As AniEase, EaseInPercent As Double
        Public Sub New(EaseIn As AniEase, EaseOut As AniEase, Optional EaseInPercent As Double = 0.5)
            Me.EaseIn = EaseIn : Me.EaseOut = EaseOut : Me.EaseInPercent = EaseInPercent
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            If t < EaseInPercent Then
                Return EaseInPercent * EaseIn.GetValue(t / EaseInPercent)
            Else
                Return (1 - EaseInPercent) * EaseOut.GetValue((t - EaseInPercent) / (1 - EaseInPercent)) + EaseInPercent
            End If
        End Function
    End Class

    Public Class AniEaseLinear
        Inherits AniEase
        Public Overrides Function GetValue(t As Double) As Double
            Return t.Clamp(0, 1)
        End Function
        Public Overrides Function GetDelta(t1 As Double, t0 As Double) As Double
            Return t1.Clamp(0, 1) - t0.Clamp(0, 1)
        End Function
    End Class

    Public Class AniEaseInFluent
        Inherits AniEase
        Private ReadOnly p As AniEasePower
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            p = Power
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return t.Clamp(0, 1) ^ p
        End Function
    End Class

    Public Class AniEaseOutFluent
        Inherits AniEase
        Private ReadOnly p As AniEasePower
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            p = Power
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return 1 - (1 - t).Clamp(0, 1) ^ p
        End Function
    End Class

    Public Class AniEaseInoutFluent
        Inherits AniEase
        Private Ease As AniEaseInout
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle, Optional Middle As Double = 0.5)
            Ease = New AniEaseInout(New AniEaseInFluent(Power), New AniEaseOutFluent(Power), Middle)
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return Ease.GetValue(t)
        End Function
    End Class

    Public Class AniEaseOutFluentWithInitial
        Inherits AniEase
        Private ReadOnly alpha As Double

        Public Sub New(InitialPixelPerSecond As Double, TotalSecond As Double, TotalDistance As Double)
            Dim v0_norm As Double = InitialPixelPerSecond * TotalSecond / TotalDistance
            alpha = v0_norm - 1.0
            If alpha < 0 Then alpha = 0
        End Sub
        Public Overrides Function GetValue(percent As Double) As Double
            Dim p As Double = percent.Clamp(0, 1)
            If alpha = 0 Then Return p
            Return (alpha + 1) * p / (1 + alpha * p)
        End Function
    End Class

    Public Class AniEaseInBack
        Inherits AniEase
        Private ReadOnly p As Double
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            p = 3 - Power * 0.5
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            t = t.Clamp(0, 1)
            Return t ^ p * Math.Cos(1.5 * Math.PI * (1 - t))
        End Function
    End Class

    Public Class AniEaseOutBack
        Inherits AniEase
        Private ReadOnly p As Double
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            p = 3 - Power * 0.5
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            t = t.Clamp(0, 1)
            Return 1 - (1 - t) ^ p * Math.Cos(1.5 * Math.PI * t)
        End Function
    End Class

    Public Class AniEaseInCar
        Inherits AniEase
        Private Ease As AniEaseInout
        Public Sub New(Optional Middle As Double = 0.7, Optional Power As AniEasePower = AniEasePower.Middle)
            Ease = New AniEaseInout(New AniEaseInBack(Power), New AniEaseOutFluent(Power), Middle)
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return Ease.GetValue(t)
        End Function
    End Class

    Public Class AniEaseOutCar
        Inherits AniEase
        Private Ease As AniEaseInout
        Public Sub New(Optional Middle As Double = 0.3, Optional Power As AniEasePower = AniEasePower.Middle)
            Ease = New AniEaseInout(New AniEaseInFluent(Power), New AniEaseOutBack(Power), Middle)
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return Ease.GetValue(t)
        End Function
    End Class

    Public Class AniEaseInElastic
        Inherits AniEase
        Private ReadOnly p As Integer
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            p = Power + 4
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            t = t.Clamp(0, 1)
            Return t ^ ((p - 1) * 0.25) * Math.Cos((p - 3.5) * Math.PI * (1 - t) ^ 1.5)
        End Function
    End Class

    Public Class AniEaseOutElastic
        Inherits AniEase
        Private ReadOnly p As Integer
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            p = Power + 4
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            t = 1 - t.Clamp(0, 1)
            Return 1 - t ^ ((p - 1) * 0.25) * Math.Cos((p - 3.5) * Math.PI * (1 - t) ^ 1.5)
        End Function
    End Class

#End Region

#Region "接口（开始、中断、检测）"

    Public Sub AniStart(AniGroup As IList, Optional Name As String = "", Optional RefreshTime As Boolean = False)
        If RefreshTime Then AniLastTick = GetTimeMs()

        Dim AllAnis = New List(Of AniData)
        For Each Element In AniGroup
            If TypeOf Element Is ICollection Then
                AllAnis.AddRange(Element)
            Else
                AllAnis.Add(Element)
            End If
        Next
        Dim NewEntry As New AniGroupEntry With {.Data = AllAnis, .StartTick = GetTimeMs()}
        If Name = "" Then
            Name = NewEntry.Uuid
        Else
            AniStop(Name)
        End If
        AniGroups.Add(Name, NewEntry)
    End Sub

    Public Sub AniStart(AniGroup As AniData, Optional Name As String = "", Optional RefreshTime As Boolean = False)
        AniStart(New List(Of AniData) From {AniGroup}, Name, RefreshTime)
    End Sub

    Public Sub AniStop(Name As String)
        AniGroups.Remove(Name)
    End Sub

    Public Function AniIsRun(Name As String) As Boolean
        Return AniGroups.ContainsKey(Name)
    End Function

#End Region

    Private AniCount As Integer = 0
    Private AniFPSCounter As Integer = 0
    Private AniFPSTimer As Long = 0

    Public AniFPS As Integer = 0

    Public Sub AniStart()

        AniLastTick = GetTimeMs()
        AniFPSTimer = AniLastTick
        AniRunning = True

        RunInNewThread(
        Sub()
            Try
                Logger.Info("动画线程开始")
                Do While True

                    Dim DeltaTime As Long = (GetTimeMs() - AniLastTick).Clamp(0, 100000)
                    If DeltaTime < 3 Then GoTo Sleeper
                    AniLastTick = GetTimeMs()

                    If ModeDebug Then
                        If (AniLastTick - AniFPSTimer).Clamp(0, 100000) >= 500 Then
                            AniFPS = AniFPSCounter
                            AniFPSCounter = 0
                            AniFPSTimer = AniLastTick
                        End If
                        AniFPSCounter += 2
                    End If

                    RunInUiWait(
                    Sub()
                        AniCount = 0
                        AniTimer(DeltaTime * AniSpeed)
                        If RandomInteger(0, 64 * If(ModeDebug, 5, 30)) = 0 AndAlso ((AniFPS < 62 AndAlso AniFPS > 0) OrElse AniCount > 4 OrElse NetManager.FileRemain <> 0) Then
                            Logger.Info($"FPS {AniFPS}, 动画 {AniCount}, 下载中 {NetManager.FileRemain}（{StringUtils.FormatByteSize(NetManager.Speed)}/s）")
                            If NetManager.FileRemain < 5 Then
                                For Each File In NetManager.AllFiles.Values
                                    If File.State < NetState.Finished Then Logger.Trace($"- {File.State} {StringUtils.FormatByteSize(File.Speed)}/s {File.LocalPath}")
                                Next
                            End If
                        End If
                    End Sub)
Sleeper:

                    Thread.Sleep(1)
                Loop
            Catch ex As Exception
                Logger.Error(ex, "动画帧执行失败", LogBehavior.AlertThenCrash)
            End Try
        End Sub, "Animation", ThreadPriority.AboveNormal)
    End Sub

    Public Sub AniTimer(DeltaTick As Double)
        Try
            If DeltaTick / AniSpeed > 100 Then Logger.Info($"两个动画帧间隔 {DeltaTick} ms")
            Dim i As Integer = -1

            Do While i + 1 < AniGroups.Count
                i += 1

                Dim Entry As AniGroupEntry = AniGroups.Values(i)
                If Entry.StartTick > AniLastTick Then Continue Do
                Dim CanRemoveAfter = True
                Dim ii = 0

                Do While ii < Entry.Data.Count
                    Dim Anim As AniData = Entry.Data(ii)

                    If Anim.IsAfter = False Then
                        CanRemoveAfter = False

                        Anim.TimeFinished += DeltaTick

                        If Anim.TimeFinished > 0 Then
                            Anim = AniRun(Anim)
                            AniCount += 1
                        End If

                        If Anim.TimeFinished >= Anim.TimeTotal Then

                            If Anim.TypeMain = AniType.Color AndAlso Not Anim.Obj(2) = "" Then Anim.Obj(0).SetResourceReference(Anim.Obj(1), Anim.Obj(2))

                            Entry.Data.RemoveAt(ii)
                            GoTo NextAni
                        End If
                        Entry.Data(ii) = Anim
                    Else
                        If CanRemoveAfter Then

                            CanRemoveAfter = False
                            Anim.IsAfter = False
                            Entry.Data(ii) = Anim

                            GoTo NextAni
                        Else

                            Exit Do
                        End If
                    End If
                    ii += 1
NextAni:
                Loop

                If Not Entry.Data.Any() Then

                    For Current = 0 To AniGroups.Count - 1
                        If AniGroups.ElementAt(Current).Value.Uuid = Entry.Uuid Then
                            AniGroups.Remove(AniGroups.ElementAt(Current).Key)
                            Exit For
                        End If
                    Next
                    i -= 1
                End If
            Loop

        Catch ex As Exception
            Logger.Error(ex, "动画刻执行失败", LogBehavior.Toast)
        End Try
    End Sub

    Private Function AniRun(Ani As AniData) As AniData
        Try
            Select Case Ani.TypeMain

                Case AniType.Number
                    Dim Delta As Double = MathUtils.Lerp(0, Ani.Value, Ani.Ease.GetDelta(Ani.TimeFinished / Ani.TimeTotal, Ani.TimePercent))
                    If Delta <> 0 Then
                        Select Case Ani.TypeSub
                            Case AniTypeSub.X
                                If TypeOf Ani.Obj Is Window Then
                                    CType(Ani.Obj, Window).Left += Delta
                                Else
                                    Select Case Ani.Obj.HorizontalAlignment
                                        Case HorizontalAlignment.Left, HorizontalAlignment.Stretch
                                            Ani.Obj.Margin = New Thickness(Ani.Obj.Margin.Left + Delta, Ani.Obj.Margin.Top, Ani.Obj.Margin.Right, Ani.Obj.Margin.Bottom)
                                        Case HorizontalAlignment.Right
                                            Ani.Obj.Margin = New Thickness(Ani.Obj.Margin.Left, Ani.Obj.Margin.Top, Ani.Obj.Margin.Right - Delta, Ani.Obj.Margin.Bottom)
                                    End Select
                                End If
                            Case AniTypeSub.Y
                                If TypeOf Ani.Obj Is Window Then
                                    CType(Ani.Obj, Window).Top += Delta
                                Else
                                    Select Case Ani.Obj.VerticalAlignment
                                        Case VerticalAlignment.Top
                                            Ani.Obj.Margin = New Thickness(Ani.Obj.Margin.Left, Ani.Obj.Margin.Top + Delta, Ani.Obj.Margin.Right, Ani.Obj.Margin.Bottom)
                                        Case VerticalAlignment.Bottom
                                            Ani.Obj.Margin = New Thickness(Ani.Obj.Margin.Left, Ani.Obj.Margin.Top, Ani.Obj.Margin.Right, Ani.Obj.Margin.Bottom - Delta)
                                    End Select
                                End If
                            Case AniTypeSub.Opacity
                                If TypeOf Ani.Obj Is UIElement Then
                                    Dim Obj As UIElement = Ani.Obj
                                    Obj.Opacity = (Obj.Opacity + Delta).Clamp(0, 1)
                                ElseIf TypeOf Ani.Obj Is DropShadowEffect Then
                                    Dim Obj As DropShadowEffect = Ani.Obj
                                    Obj.Opacity = (Obj.Opacity + Delta).Clamp(0, 1)
                                End If
                            Case AniTypeSub.Width
                                Dim Obj As FrameworkElement = Ani.Obj
                                Obj.Width = Math.Max(If(Double.IsNaN(Obj.Width), Obj.ActualWidth, Obj.Width) + Delta, 0)
                            Case AniTypeSub.Height
                                Dim Obj As FrameworkElement = Ani.Obj
                                Obj.Height = Math.Max(If(Double.IsNaN(Obj.Height), Obj.ActualHeight, Obj.Height) + Delta, 0)
                            Case AniTypeSub.Value
                                Ani.Obj.Value += Delta
                            Case AniTypeSub.Radius
                                Ani.Obj.Radius += Delta
                            Case AniTypeSub.StrokeThickness
                                Ani.Obj.StrokeThickness = Math.Max(Ani.Obj.StrokeThickness + Delta, 0)
                            Case AniTypeSub.BorderThickness
                                Ani.Obj.BorderThickness = New Thickness(CType(Ani.Obj.BorderThickness, Thickness).Bottom + Delta)
                            Case AniTypeSub.TranslateX
                                If IsNothing(Ani.Obj.RenderTransform) OrElse TypeOf Ani.Obj.RenderTransform IsNot TranslateTransform Then Ani.Obj.RenderTransform = New TranslateTransform(0, 0)
                                CType(Ani.Obj.RenderTransform, TranslateTransform).X += Delta
                            Case AniTypeSub.TranslateY
                                If IsNothing(Ani.Obj.RenderTransform) OrElse TypeOf Ani.Obj.RenderTransform IsNot TranslateTransform Then Ani.Obj.RenderTransform = New TranslateTransform(0, 0)
                                CType(Ani.Obj.RenderTransform, TranslateTransform).Y += Delta
                            Case AniTypeSub.Double
                                Ani.Obj(0).SetValue(Ani.Obj(1), Ani.Obj(0).GetValue(Ani.Obj(1)) + Delta)
                            Case AniTypeSub.DoubleParam
                                CType(Ani.Obj, ParameterizedThreadStart)(Delta)
                            Case AniTypeSub.GridLengthWidth
                                Ani.Obj.Width = New GridLength(Math.Max(Ani.Obj.Width.Value + Delta, 0), GridUnitType.Star)
                        End Select
                    End If

                Case AniType.Color

                    Dim Delta As MyColor = MyColor.Lerp(New MyColor(0, 0, 0, 0), Ani.Value, Ani.Ease.GetDelta(Ani.TimeFinished / Ani.TimeTotal, Ani.TimePercent)) + Ani.ValueLast
                    Dim Obj As FrameworkElement = Ani.Obj(0)
                    Dim Prop As DependencyProperty = Ani.Obj(1)
                    Dim NewColor As MyColor = New MyColor(Obj.GetValue(Prop)) + Delta
                    Obj.SetValue(Prop, If(Prop.PropertyType.Name = "Color", CType(NewColor, Color), CType(NewColor, SolidColorBrush)))
                    Ani.ValueLast = NewColor - New MyColor(Obj.GetValue(Prop))

                Case AniType.Scale
                    Dim Obj As FrameworkElement = Ani.Obj
                    Dim Delta As Double = Ani.Ease.GetDelta(Ani.TimeFinished / Ani.TimeTotal, Ani.TimePercent)
                    Obj.Margin = New Thickness(Obj.Margin.Left + MathUtils.Lerp(0, Ani.Value.Left, Delta), Obj.Margin.Top + MathUtils.Lerp(0, Ani.Value.Top, Delta), Obj.Margin.Right + MathUtils.Lerp(0, Ani.Value.Left, Delta), Obj.Margin.Bottom + MathUtils.Lerp(0, Ani.Value.Top, Delta))
                    Obj.Width = Math.Max(Obj.Width + MathUtils.Lerp(0, Ani.Value.Width, Delta), 0)
                    Obj.Height = Math.Max(Obj.Height + MathUtils.Lerp(0, Ani.Value.Height, Delta), 0)

                Case AniType.TextAppear
                    Dim Text As String = Ani.Value(0)
                    Dim TextCount As Integer = If(Ani.Value(1), Text.Length, 0) +
                                               Math.Round(Text.Length * If(Ani.Value(1), -1, 1) * Ani.Ease.GetDelta(Ani.TimeFinished / Ani.TimeTotal, 0))
                    Dim NewText As String = Text.Substring(0, TextCount)

                    If TextCount < Text.Length Then
                        Dim NextText As String = Text.Substring(TextCount, 1)
                        If Convert.ToInt32(Convert.ToChar(NextText)) >= Convert.ToInt32(Convert.ToChar(128)) Then
                            NewText &= Encoding.GetEncoding("GB18030").GetString({RandomInteger(16 + 160, 87 + 160), RandomInteger(1 + 160, 89 + 160)})
                        Else
                            NewText &= RandomInteger(0, 85).ConvertRadix(86)
                        End If
                    End If

                    If TypeOf Ani.Obj Is TextBlock Then
                        Ani.Obj.Text = NewText
                    Else
                        Ani.Obj.Context = NewText
                    End If

                Case AniType.Code
                    Ani.TimeFinished = Ani.TimeTotal
                    CType(Ani.Value, ThreadStart)()

                Case AniType.ScaleTransform
                    Dim Obj As FrameworkElement = Ani.Obj
                    If TypeOf Obj.RenderTransform IsNot ScaleTransform Then
                        If Obj.RenderTransformOrigin = New Point(0, 0) Then Obj.RenderTransformOrigin = New Point(0.5, 0.5)
                        Obj.RenderTransform = New ScaleTransform(1, 1)
                    End If
                    Dim Delta As Double = MathUtils.Lerp(0, Ani.Value, Ani.Ease.GetDelta(Ani.TimeFinished / Ani.TimeTotal, Ani.TimePercent))
                    CType(Obj.RenderTransform, ScaleTransform).ScaleX = Math.Max(CType(Obj.RenderTransform, ScaleTransform).ScaleX + Delta, 0)
                    CType(Obj.RenderTransform, ScaleTransform).ScaleY = Math.Max(CType(Obj.RenderTransform, ScaleTransform).ScaleY + Delta, 0)

                Case AniType.RotateTransform
                    Dim Obj As FrameworkElement = Ani.Obj
                    If TypeOf Obj.RenderTransform IsNot RotateTransform Then
                        If Obj.RenderTransformOrigin = New Point(0, 0) Then Obj.RenderTransformOrigin = New Point(0.5, 0.5)
                        Obj.RenderTransform = New RotateTransform(0)
                    End If
                    Dim Delta As Double = MathUtils.Lerp(0, Ani.Value, Ani.Ease.GetDelta(Ani.TimeFinished / Ani.TimeTotal, Ani.TimePercent))
                    CType(Obj.RenderTransform, RotateTransform).Angle = CType(Obj.RenderTransform, RotateTransform).Angle + Delta

            End Select
            Ani.TimePercent = Ani.TimeFinished / Ani.TimeTotal
        Catch ex As Exception
            Logger.Error(ex, $"执行动画失败：{Ani}", LogBehavior.Toast)
        End Try
        Return Ani
    End Function

End Module
