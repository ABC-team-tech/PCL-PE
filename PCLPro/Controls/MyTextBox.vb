Public Class MyTextBox
    Inherits TextBox
    Implements ISettingControl

    Public Property HasBackground As Boolean = True
    Public Property ShowValidateResult As Boolean = True
    Public Property CornerRadius As CornerRadius
        Get
            Return GetValue(CornerRadiusProperty)
        End Get
        Set(value As CornerRadius)
            SetValue(CornerRadiusProperty, value)
        End Set
    End Property
    Public Shared ReadOnly CornerRadiusProperty As DependencyProperty =
        DependencyProperty.Register("CornerRadius", GetType(CornerRadius), GetType(MyTextBox), New PropertyMetadata(New CornerRadius(3)))

    Private _labWrong As TextBlock = Nothing
    Private ReadOnly Property labWrong As TextBlock
        Get
            If Template Is Nothing Then Return Nothing
            If _labWrong Is Nothing Then _labWrong = Template.FindName("labWrong", Me)
            Return _labWrong
        End Get
    End Property
    Private _labHint As TextBlock = Nothing
    Private ReadOnly Property labHint As TextBlock
        Get
            If Template Is Nothing Then Return Nothing
            If _labHint Is Nothing Then _labHint = Template.FindName("labHint", Me)
            Return _labHint
        End Get
    End Property
    Public Overrides Sub OnApplyTemplate()
        MyBase.OnApplyTemplate()
        If HintText = "" OrElse labHint.Text <> "" Then Return
        labHint.Text = If(Text = "", HintText, "")
    End Sub

    Public Uuid As Integer = GetUuid()
    Public Shared Event ValidateChanged(sender As Object, e As EventArgs)
    Private _ChangedEventList As List(Of RoutedEventHandler)
    Public ReadOnly Property ChangedEventList As List(Of RoutedEventHandler)
        Get
            If _ChangedEventList Is Nothing Then _ChangedEventList = New List(Of RoutedEventHandler)
            Return _ChangedEventList
        End Get
    End Property
    Public Custom Event ValidatedTextChanged As RoutedEventHandler
        AddHandler(value As RoutedEventHandler)
            ChangedEventList.Add(value)
        End AddHandler
        RemoveHandler(value As RoutedEventHandler)
            ChangedEventList.Remove(value)
        End RemoveHandler
        RaiseEvent(sender As Object, e As TextChangedEventArgs)
            For Each handler As RoutedEventHandler In ChangedEventList
                If Not IsNothing(handler) Then handler.Invoke(sender, e)
            Next
        End RaiseEvent
    End Event

    Public Property ValidateResult As String
        Get
            Return GetValue(ValidateResultProperty)
        End Get
        Set(value As String)
            SetValue(ValidateResultProperty, value)
        End Set
    End Property
    Public Shared ReadOnly ValidateResultProperty As DependencyProperty =
        DependencyProperty.Register("ValidateResult", GetType(String), GetType(MyTextBox), New PropertyMetadata("",
        Sub(d As MyTextBox, e As DependencyPropertyChangedEventArgs)
            d.SetValue(IsValidatedPropertyKey, String.IsNullOrEmpty(e.NewValue))
        End Sub))

    Public ReadOnly Property IsValidated As Boolean
        Get
            Return GetValue(IsValidatedProperty)
        End Get
    End Property
    Private Shared ReadOnly IsValidatedPropertyKey As DependencyPropertyKey =
        DependencyProperty.RegisterReadOnly("IsValidated", GetType(Boolean), GetType(MyTextBox), New PropertyMetadata(True))
    Public Shared ReadOnly IsValidatedProperty As DependencyProperty = IsValidatedPropertyKey.DependencyProperty

    Public Property ValidateRules As ObjectModel.Collection(Of Validate)
        Get
            Return _ValidateRules
        End Get
        Set(Value As ObjectModel.Collection(Of Validate))
            _ValidateRules = Value
            Validate()
        End Set
    End Property
    Private _ValidateRules As New ObjectModel.Collection(Of Validate)

    Public Sub Validate() Handles Me.Loaded

        ValidateResult = ModValidate.Validate(Text, ValidateRules)

        If ShownValidateResult <> If(IsValidated, ValidateState.Success, ValidateState.FailedAndShowDetail) Then
            If IsLoaded AndAlso labWrong IsNot Nothing Then
                ChangeValidateResult(IsValidated, True)
            Else
                RunInNewThread(
                Sub()
                    Thread.Sleep(30)
                    RunInUi(Sub() ChangeValidateResult(IsValidated, False))
                End Sub, "DelayedValidate Change")
            End If
        End If

        If ShowValidateResult AndAlso Not IsValidated Then
            If IsLoaded AndAlso labWrong IsNot Nothing Then
                labWrong.Text = ValidateResult
            Else
                RunInNewThread(
                Sub()
                    Dim IsFinished As Boolean = False
                    Do Until IsFinished
                        Thread.Sleep(20)
                        RunInUiWait(
                        Sub()
                            If labWrong IsNot Nothing Then
                                labWrong.Text = ValidateResult
                                IsFinished = True
                            End If
                            If Not IsLoaded Then IsFinished = True
                        End Sub)
                    Loop
                End Sub, "DelayedValidate Text")
            End If
        End If
    End Sub

    Public Sub ForceShowAsSuccess()
        IsTextChanged = False
        ChangeValidateResult(IsValidated, True)
    End Sub
    Private ShownValidateResult As ValidateState = ValidateState.NotInited
    Private Sub ChangeValidateResult(IsSuccessful As Boolean, IsLoaded As Boolean)
        If IsLoaded AndAlso AniControlEnabled = 0 AndAlso labWrong IsNot Nothing Then
            If IsSuccessful OrElse Not IsTextChanged Then

                ShownValidateResult = If(IsSuccessful, ValidateState.Success, ValidateState.FailedButTextNotChanged)
                AniStart({
                     AaOpacity(labWrong, -labWrong.Opacity, 150),
                     AaHeight(labWrong, -labWrong.Height, 150,, New AniEaseOutFluent),
                     AaCode(Sub() labWrong.Visibility = Visibility.Collapsed,, True)
                }, "MyTextBox Validate " & Uuid)
            ElseIf ShowValidateResult Then

                ShownValidateResult = ValidateState.FailedAndShowDetail
                labWrong.Visibility = Visibility.Visible
                AniStart({
                     AaOpacity(labWrong, 1 - labWrong.Opacity, 150),
                     AaHeight(labWrong, 21 - labWrong.Height, 150,, New AniEaseOutFluent)
                }, "MyTextBox Validate " & Uuid)
            Else

                ShownValidateResult = ValidateState.FailedAndHideDetail
            End If
        Else
            ShownValidateResult = ValidateState.NotLoaded
        End If
        RefreshColor()
        RaiseEvent ValidateChanged(Me, New EventArgs)
    End Sub
    Private Enum ValidateState
        NotInited
        Success
        FailedButTextNotChanged
        FailedAndShowDetail
        FailedAndHideDetail
        NotLoaded
    End Enum

    Private IsTextChanged As Boolean = False
    Public Property HintText As String
        Get
            Return GetValue(HintTextProperty)
        End Get
        Set(value As String)
            SetValue(HintTextProperty, value)
        End Set
    End Property
    Public Shared ReadOnly HintTextProperty As DependencyProperty =
        DependencyProperty.Register("HintText", GetType(String), GetType(MyTextBox), New PropertyMetadata("",
        Sub(t As MyTextBox, e As DependencyPropertyChangedEventArgs)
            If t.labHint IsNot Nothing Then t.labHint.Text = If(t.Text = "", t.HintText, "")
        End Sub))
    Private Sub MyTextBox_TextChanged(sender As MyTextBox, e As TextChangedEventArgs) Handles Me.TextChanged
        Try

            If labHint IsNot Nothing Then labHint.Text = If(Text = "", HintText, "")

            IsTextChanged = IsLoaded

            Validate()
            If Not IsValidated Then Return

            RaiseEvent ValidatedTextChanged(sender, e)
        Catch ex As Exception
            Logger.Error(ex, "进行输入验证时出错")
        End Try
    End Sub

    Private Sub RefreshColor() Handles Me.IsEnabledChanged, Me.MouseEnter, Me.MouseLeave, Me.GotFocus, Me.LostFocus
        Try

            If TemplatedParent IsNot Nothing AndAlso TypeOf TemplatedParent Is MyComboBox Then Return

            Dim ForeColorName As String, BackColorName As String
            Dim AnimationTime As Integer
            If IsEnabled Then
                If IsValidated OrElse Not IsTextChanged Then
                    If IsFocused Then
                        ForeColorName = "ColorBrush3"
                        BackColorName = "ColorBrush7"
                        AnimationTime = 10
                    ElseIf IsMouseOver Then
                        ForeColorName = "ColorBrush4"
                        BackColorName = "ColorBrush7"
                        AnimationTime = 100
                    Else
                        ForeColorName = "ColorBrushBg0"
                        BackColorName = "ColorBrushHalfWhite"
                        AnimationTime = 100
                    End If
                Else
                    ForeColorName = "ColorBrushRedLight"
                    BackColorName = "ColorBrushRedBack"
                    AnimationTime = 200
                End If
            Else
                ForeColorName = "ColorBrushGray5"
                BackColorName = "ColorBrushGray6"
                AnimationTime = 200
            End If
            If Not HasBackground Then BackColorName = "ColorBrushTransparent"

            If IsLoaded AndAlso AniControlEnabled = 0 Then

                AniStart({
                         AaColor(Me, BorderBrushProperty, ForeColorName, AnimationTime),
                         AaColor(Me, BackgroundProperty, BackColorName, AnimationTime)
                     }, "MyTextBox Color " & Uuid)
            Else

                AniStop("MyTextBox Color " & Uuid)
                SetResourceReference(BorderBrushProperty, ForeColorName)
                SetResourceReference(BackgroundProperty, BackColorName)
            End If

        Catch ex As Exception
            Logger.Warn(ex, "文本框颜色改变出错")
        End Try
    End Sub
    Private Sub RefreshTextColor() Handles Me.IsEnabledChanged
        Dim NewColor As MyColor = If(IsEnabled, ColorGray1, ColorGray4)
        If CType(Foreground, SolidColorBrush).Color.R = NewColor.R Then Return
        If IsLoaded AndAlso AniControlEnabled = 0 AndAlso Not Text = "" Then

            AniStart({AaColor(Me, ForegroundProperty, If(IsEnabled, "ColorBrushGray1", "ColorBrushGray4"), 200)}, "MyTextBox TextColor " & Uuid)
        Else

            AniStop("MyTextBox TextColor " & Uuid)
            Foreground = NewColor
        End If
    End Sub

    Private Sub MyTextBox_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        If e.Key = Key.Enter Then RaiseCustomEvent()
    End Sub

#Region "设置"

    Private Sub RefreshSetting(NewValue As String) Implements ISettingControl.RefreshSetting
        Text = NewValue
    End Sub

    Private Function GetCurrentSetting() As String Implements ISettingControl.GetCurrentSetting
        Return Text
    End Function

    Private Sub SaveSetting() Handles Me.ValidatedTextChanged
        SettingService.SaveSetting(Me)
    End Sub

#End Region

End Class
