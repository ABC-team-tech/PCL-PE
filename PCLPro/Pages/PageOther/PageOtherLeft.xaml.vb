Public Class PageOtherLeft

    Private IsLoad As Boolean = False
    Private IsPageSwitched As Boolean = False
    Private Sub PageOtherLeft_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        Dim IsHiddenPage As Boolean = False
        If ItemAbout.Checked AndAlso Settings.Get(Of Boolean)("UiHiddenOtherAbout") Then IsHiddenPage = True
        If ItemTest.Checked AndAlso Settings.Get(Of Boolean)("UiHiddenOtherTest") Then IsHiddenPage = True
        If ItemFeedback.Checked AndAlso Settings.Get(Of Boolean)("UiHiddenOtherFeedback") Then IsHiddenPage = True
        If PageSetupUI.HiddenForceShow Then IsHiddenPage = False

        If IsLoad AndAlso Not IsHiddenPage Then Return
        IsLoad = True

        PageSetupUI.HiddenRefresh()

        If IsPageSwitched Then Return
        If Not Settings.Get(Of Boolean)("UiHiddenOtherAbout") Then
            ItemAbout.SetChecked(True, False, False)
        ElseIf Not Settings.Get(Of Boolean)("UiHiddenOtherTest") Then
            ItemTest.SetChecked(True, False, False)
        Else
            ItemFeedback.SetChecked(True, False, False)
        End If
    End Sub
    Private Sub PageOtherLeft_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
        IsPageSwitched = False
    End Sub

#Region "页面切换"

    Public PageID As FormMain.PageSubType
    Public Sub New()
        InitializeComponent()

        If Not Settings.Get(Of Boolean)("UiHiddenOtherAbout") Then
            PageID = FormMain.PageSubType.OtherAbout
        ElseIf Not Settings.Get(Of Boolean)("UiHiddenOtherTest") Then
            PageID = FormMain.PageSubType.OtherTest
        Else
            PageID = FormMain.PageSubType.OtherFeedback
        End If
    End Sub

    Private Sub PageCheck(sender As FrameworkElement, e As RouteEventArgs) Handles ItemAbout.Check, ItemTest.Check, ItemFeedback.Check

        If sender.Tag IsNot Nothing Then PageChange(Val(sender.Tag))
    End Sub

    Public Function PageGet(Optional ID As FormMain.PageSubType = -1)
        If ID = -1 Then ID = PageID
            Select Case ID
                Case FormMain.PageSubType.OtherAbout
                If FrmOtherAbout Is Nothing Then FrmOtherAbout = New PageOtherAbout
                Return FrmOtherAbout
            Case FormMain.PageSubType.OtherTest
                If FrmOtherTest Is Nothing Then FrmOtherTest = New PageOtherTest
                Return FrmOtherTest
            Case FormMain.PageSubType.OtherFeedback
                If FrmOtherFeedback Is Nothing Then FrmOtherFeedback = New PageOtherFeedback
                Return FrmOtherFeedback
            Case Else
                Throw New Exception("未知的更多子页面种类：" & ID)
        End Select
    End Function

    Public Sub PageChange(ID As FormMain.PageSubType)
        If PageID = ID Then Return
        AniControlEnabled += 1
        IsPageSwitched = True
        Try
            PageChangeRun(PageGet(ID))
            PageID = ID
        Catch ex As Exception
            Logger.Error(ex, $"切换分页面失败（ID {ID}）")
        Finally
            AniControlEnabled -= 1
        End Try
    End Sub
    Private Shared Sub PageChangeRun(Target As MyPageRight)
        AniStop("FrmMain PageChangeRight")
        If Target.Parent IsNot Nothing Then Target.SetValue(ContentPresenter.ContentProperty, Nothing)
        FrmMain.PageRight = Target
        CType(FrmMain.PanMainRight.Child, MyPageRight).PageOnExit()
        AniStart({
                         AaCode(Sub()
                                    CType(FrmMain.PanMainRight.Child, MyPageRight).PageOnForceExit()
                                    FrmMain.PanMainRight.Child = FrmMain.PageRight
                                    FrmMain.PageRight.Opacity = 0
                                End Sub, 130),
                         AaCode(Sub()

                                    FrmMain.PageRight.Opacity = 1
                                    FrmMain.PageRight.PageOnEnter()
                                End Sub, 30, True)
                     }, "PageLeft PageChange")
    End Sub

#End Region

    Public Sub Refresh(sender As Object, e As EventArgs)
        Hint("正在刷新……", Log:=False)
    End Sub
End Class
