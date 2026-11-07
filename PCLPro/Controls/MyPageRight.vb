Public Class MyPageRight
    Inherits AdornerDecorator
    Public PageUuid As Integer = GetUuid()

    Public Property PanScroll As String
        Get
            Return GetValue(PanScrollProperty)
        End Get
        Set(value As String)
            SetValue(PanScrollProperty, value)
        End Set
    End Property
    Private Shared ReadOnly PanScrollProperty =
        DependencyProperty.Register("PanScroll", GetType(String), GetType(MyPageRight), New PropertyMetadata(Nothing))

    Public Enum PageStates
        Empty
        LoaderWait
        LoaderEnter
        LoaderStayForce
        LoaderStay
        LoaderExit
        ContentEnter
        ContentStay
        ContentExit
        PageExit
    End Enum
    Private _PageState As PageStates = PageStates.Empty
    Public Property PageState As PageStates
        Get
            Return _PageState
        End Get
        Set(value As PageStates)
            If _PageState = value Then Return
            _PageState = value
            Logger.Trace($"页面状态切换为 {value}")
        End Set
    End Property

#Region "加载器"

    Private PageLoader As LoaderBase
    Private PageLoaderInputInvoke
    Private PageLoaderUi As MyLoading
    Private PanLoader As FrameworkElement
    Private PanContent As FrameworkElement
    Private PanAlways As FrameworkElement
    Private PageLoaderAutoRun As Boolean

    Public Sub PageLoaderInit(Of TIn, TOut)(LoaderUi As MyLoading, PanLoader As FrameworkElement, PanContent As FrameworkElement, PanAlways As FrameworkElement,
                              RealLoader As LoaderTask(Of TIn, TOut),
                              Optional FinishedInvoke As Action(Of LoaderTask(Of TIn, TOut)) = Nothing, Optional InputInvoke As Func(Of TIn) = Nothing,
                              Optional AutoRun As Boolean = True)

        Me.PanLoader = PanLoader
        Me.PanContent = PanContent
        Me.PanAlways = PanAlways
        Me.PageLoader = RealLoader
        Me.PageLoaderUi = LoaderUi
        Me.PageLoaderInputInvoke = InputInvoke
        Me.PageLoaderAutoRun = AutoRun

        If FinishedInvoke IsNot Nothing Then
            AddHandler RealLoader.PreviewFinish,
            Sub()
                Do While PageState = MyPageRight.PageStates.PageExit OrElse PageState = MyPageRight.PageStates.ContentExit
                    Thread.Sleep(10)
                Loop
                RunInUiWait(Sub() FinishedInvoke(RealLoader))
                Thread.Sleep(20)
            End Sub
        End If
        AddHandler RealLoader.OnStateChangedUi, Sub(Loader As LoaderBase, NewState As LoadState, OldState As LoadState) RunInUi(Sub() PageLoaderState(Loader, NewState, OldState))

        PanLoader.Visibility = Visibility.Collapsed
        PanContent.Visibility = Visibility.Collapsed
        If PanAlways IsNot Nothing Then PanAlways.Visibility = Visibility.Collapsed

        If PageLoaderAutoRun Then

            PageLoader.Start(RealLoader.StartGetInput(Nothing, PageLoaderInputInvoke))
        End If
        If PageLoader.State = LoadState.Finished AndAlso FinishedInvoke IsNot Nothing Then
            RunInUiWait(Sub() FinishedInvoke(RealLoader))
        End If

        PageLoaderUi.State = RealLoader
        AddHandler PageLoaderUi.Click, Sub() If RealLoader.State = LoadState.Failed Then PageLoaderRestart()
    End Sub

    Public Sub PageLoaderInit(Of TIn)(LoaderUi As MyLoading, PanLoader As FrameworkElement, PanContent As FrameworkElement, PanAlways As FrameworkElement,
                              RealLoader As LoaderCombo(Of TIn),
                              Optional FinishedInvoke As Action(Of LoaderCombo(Of TIn)) = Nothing, Optional InputInvoke As Func(Of TIn) = Nothing,
                              Optional AutoRun As Boolean = True)

        Me.PanLoader = PanLoader
        Me.PanContent = PanContent
        Me.PanAlways = PanAlways
        Me.PageLoader = RealLoader
        Me.PageLoaderUi = LoaderUi
        Me.PageLoaderInputInvoke = InputInvoke
        Me.PageLoaderAutoRun = AutoRun

        If FinishedInvoke IsNot Nothing Then
            AddHandler RealLoader.PreviewFinish,
            Sub()
                Do While PageState = MyPageRight.PageStates.PageExit OrElse PageState = MyPageRight.PageStates.ContentExit
                    Thread.Sleep(10)
                Loop
                RunInUiWait(Sub() FinishedInvoke(RealLoader))
                Thread.Sleep(20)
            End Sub
        End If
        AddHandler RealLoader.OnStateChangedUi, Sub(Loader As LoaderBase, NewState As LoadState, OldState As LoadState) RunInUi(Sub() PageLoaderState(Loader, NewState, OldState))

        PanLoader.Visibility = Visibility.Collapsed
        PanContent.Visibility = Visibility.Collapsed
        If PanAlways IsNot Nothing Then PanAlways.Visibility = Visibility.Collapsed

        If PageLoaderAutoRun Then

            Dim Input = Nothing
            If PageLoaderInputInvoke IsNot Nothing Then Input = PageLoaderInputInvoke()
            PageLoader.Start(Input)
        End If
        If PageLoader.State = LoadState.Finished AndAlso FinishedInvoke IsNot Nothing Then
            RunInUiWait(Sub() FinishedInvoke(RealLoader))
        End If

        PageLoaderUi.State = RealLoader
        AddHandler PageLoaderUi.Click, Sub() If RealLoader.State = LoadState.Failed Then PageLoaderRestart()
    End Sub

    Public Sub PageLoaderRestart(Optional Input As Object = Nothing, Optional IsForceRestart As Boolean = True)
        If Not PageLoaderAutoRun Then Return
        If GeneralUtils.IsGenericInstanceOf(PageLoader, GetType(LoaderTask(Of ,))) OrElse TypeOf PageLoader Is LoaderWorker Then
            PageLoader.Start(CType(PageLoader, Object).StartGetInput(Input, PageLoaderInputInvoke), IsForceRestart:=IsForceRestart)
        Else
            If Input Is Nothing AndAlso PageLoaderInputInvoke IsNot Nothing Then Input = PageLoaderInputInvoke()
            PageLoader.Start(Input, IsForceRestart:=IsForceRestart)
        End If
    End Sub

#End Region

#Region "事件"

    Public Sub PageOnEnter()
        Logger.Trace("已触发 PageOnEnter")
        RaiseEvent PageEnter()
        Select Case PageState
            Case PageStates.Empty
                If PageLoader Is Nothing OrElse PageLoader.State = LoadState.Finished OrElse PageLoader.State = LoadState.Waiting OrElse PageLoader.State = LoadState.Canceled Then

                    PageState = PageStates.ContentEnter
                    TriggerEnterAnimation(PanAlways, If(PanContent, Child))
                ElseIf PageLoader.State = LoadState.Loading Then
                    PageState = PageStates.LoaderWait
                    AniStart(AaCode(AddressOf PageOnLoaderWaitFinished, 400), "PageRight PageChange " & PageUuid)
                Else
                    PageState = PageStates.LoaderEnter
                    TriggerEnterAnimation(PanAlways, PanLoader)
                End If
            Case PageStates.ContentExit

                If PageLoader Is Nothing OrElse PageLoader.State = LoadState.Finished OrElse PageLoader.State = LoadState.Waiting OrElse PageLoader.State = LoadState.Canceled Then
                    PageState = PageStates.ContentEnter
                    TriggerEnterAnimation(If(PanContent, Child))
                ElseIf PageLoader.State = LoadState.Loading Then
                    PageState = PageStates.LoaderWait
                    AniStart(AaCode(AddressOf PageOnLoaderWaitFinished, 400), "PageRight PageChange " & PageUuid)
                Else
                    PageState = PageStates.LoaderEnter
                    TriggerEnterAnimation(PanLoader)
                End If
            Case PageStates.ContentEnter

            Case Else
                Throw New Exception("在状态为 " & PageState.ToString() & " 时触发了 PageOnEnter 事件。")
        End Select
    End Sub
    Public Event PageEnter()

    Public Sub PageOnExit()
        Logger.Trace("已触发 PageOnExit")
        RaiseEvent PageExit()
        Select Case PageState
            Case PageStates.ContentEnter, PageStates.ContentStay
                PageState = PageStates.PageExit
                TriggerExitAnimation(PanAlways, If(PanContent, Child))
            Case PageStates.LoaderEnter, PageStates.LoaderStayForce, PageStates.LoaderStay
                PageState = PageStates.PageExit
                TriggerExitAnimation(PanAlways, PanLoader)
            Case PageStates.LoaderWait
                PageState = PageStates.PageExit
                TriggerExitAnimation(PanAlways)
            Case PageStates.LoaderExit, PageStates.ContentExit
                PageState = PageStates.PageExit
                If PanAlways IsNot Nothing Then TriggerExitAnimation(PanAlways, If(PanContent, Child))
            Case PageStates.PageExit, PageStates.Empty
        End Select
    End Sub
    Public Event PageExit()

    Public Sub PageOnForceExit()
        If PageState = PageStates.Empty Then Return
        Logger.Trace("已触发 PageOnForceExit")
        PageState = PageStates.Empty
        AniStop("PageRight PageChange " & PageUuid)

        If PageLoader Is Nothing Then
            Child.Visibility = Visibility.Collapsed
        Else
            PanContent.Visibility = Visibility.Collapsed
            PanLoader.Visibility = Visibility.Collapsed
            If PanAlways IsNot Nothing Then PanAlways.Visibility = Visibility.Collapsed
        End If
    End Sub

    Public Sub PageOnContentExit()
        Logger.Trace("已触发 PageOnContentExit")
        If PageLoader IsNot Nothing AndAlso PageLoader.State = LoadState.Loading Then

            Throw New Exception("在调用 PageOnContentExit 时，加载器不能为 Loading 状态")
        End If
        Select Case PageState
            Case PageStates.ContentEnter, PageStates.ContentStay
                PageState = PageStates.ContentExit
                TriggerExitAnimation(PanContent)
            Case PageStates.LoaderExit
                PageState = PageStates.ContentExit
            Case PageStates.LoaderEnter, PageStates.LoaderStayForce, PageStates.LoaderStay
                PageState = PageStates.ContentExit
                TriggerExitAnimation(PanLoader)
            Case PageStates.LoaderWait
                PageOnEnter()
            Case PageStates.Empty

                If IsLoaded Then PageOnEnter()
        End Select
    End Sub

    Private Sub PageOnEnterAnimationFinished()
        Logger.Trace("已触发 PageOnEnterAnimationFinished")
        Select Case PageState
            Case PageStates.ContentEnter
                PageState = PageStates.ContentStay
            Case PageStates.LoaderEnter
                PageState = PageStates.LoaderStayForce
                AniStart(AaCode(AddressOf PageOnLoaderStayFinished, 400), "PageRight PageChange " & PageUuid)
            Case Else
                Throw New Exception("在状态为 " & PageState.ToString() & " 时触发了 PageOnEnterAnimationFinished 事件。")
        End Select
    End Sub

    Private Sub PageOnExitAnimationFinished()
        Logger.Trace("已触发 PageOnExitAnimationFinished")
        Select Case PageState
            Case PageStates.PageExit
                PageState = PageStates.Empty
            Case PageStates.ContentExit
                PageOnEnter()
            Case PageStates.LoaderExit
                PageState = PageStates.ContentEnter
                TriggerEnterAnimation(PanContent)
            Case Else
                Throw New Exception("在状态为 " & PageState.ToString() & " 时触发了 PageOnExitAnimationFinished 事件。")
        End Select
    End Sub

    Private Sub PageOnLoaderWaitFinished()
        Logger.Trace("已触发 PageOnLoaderWaitFinished")
        Select Case PageState
            Case PageStates.LoaderWait
                PageState = PageStates.LoaderEnter
                If PanAlways IsNot Nothing AndAlso PanAlways.Visibility = Visibility.Collapsed Then
                    TriggerEnterAnimation(PanAlways, PanLoader)
                Else
                    TriggerEnterAnimation(PanLoader)
                End If
            Case Else
                Throw New Exception("在状态为 " & PageState.ToString() & " 时触发了 PageOnLoaderWaitFinished 事件。")
        End Select
    End Sub

    Private Sub PageOnLoaderStayFinished()
        Logger.Trace("已触发 PageOnLoaderStayFinished")
        Select Case PageState
            Case PageStates.LoaderStayForce
                If PageLoader.State = LoadState.Finished Then
                    PageState = PageStates.LoaderExit
                    TriggerExitAnimation(PanLoader)
                Else
                    PageState = PageStates.LoaderStay
                End If
            Case Else
                Throw New Exception("在状态为 " & PageState.ToString() & " 时触发了 PageOnLoaderWaitFinished 事件。")
        End Select
    End Sub

    Private Sub PageLoaderState(sender As Object, NewState As LoadState, OldState As LoadState)
        Select Case NewState
            Case LoadState.Failed, LoadState.Loading
                If OldState = LoadState.Failed OrElse OldState = LoadState.Loading Then Return
                Logger.Trace("已触发 PageLoaderState (Start/Refresh)")

                Select Case PageState
                    Case PageStates.ContentEnter, PageStates.ContentStay
                        PageState = PageStates.ContentExit
                        TriggerExitAnimation(PanContent)
                    Case PageStates.LoaderExit
                        PageState = PageStates.ContentExit
                End Select
            Case LoadState.Finished, LoadState.Canceled, LoadState.Waiting
                If Not (OldState = LoadState.Failed OrElse OldState = LoadState.Loading) Then Return
                Logger.Trace("已触发 PageLoaderState (Stop/Canceled)")

                Select Case PageState
                    Case PageStates.LoaderWait
                        PageState = PageStates.ContentEnter
                        If PanAlways IsNot Nothing AndAlso PanAlways.Visibility = Visibility.Collapsed Then
                            TriggerEnterAnimation(PanAlways, PanContent)
                        Else
                            TriggerEnterAnimation(PanContent)
                        End If
                    Case PageStates.LoaderStay
                        PageState = PageStates.LoaderExit
                        TriggerExitAnimation(PanLoader)
                End Select
        End Select
    End Sub

#End Region

#Region "动画"

    Public Sub TriggerEnterAnimation(ParamArray Elements As FrameworkElement())
        Dim RealElements = Elements.Where(Function(e) e IsNot Nothing)
        For Each Element In RealElements
            Element.Visibility = Visibility.Visible
        Next
        Dim AniList As New List(Of AniData)
        Dim Delay As Integer = 0

        For Each Element In RealElements
            For Each Control As FrameworkElement In GetAllAnimControls(Element, True)

                Control.IsHitTestVisible = True
                If Control.RenderTransform IsNot Nothing AndAlso TypeOf Control.RenderTransform Is TranslateTransform Then Control.RenderTransform = Nothing
            Next
            For Each Control As FrameworkElement In GetAllAnimControls(Element)
                If TypeOf Control Is MyExtraTextButton Then
                    CType(Control, MyExtraTextButton).Show = True
                Else
                    Control.Opacity = 0
                    Control.RenderTransform = New TranslateTransform(0, -16)
                    AniList.Add(AaOpacity(Control, 1, 100, Delay, New AniEaseOutFluent(AniEasePower.Weak)))
                    AniList.Add(AaTranslateY(Control, 5, 250, Delay, New AniEaseOutFluent))
                    AniList.Add(AaTranslateY(Control, 11, 350, Delay, New AniEaseOutBack))
                    Delay += 25
                End If
            Next
        Next

        Dim Scroll As MyScrollBar = GetFirstScrollViewer(RealElements)
        If Scroll IsNot Nothing Then
            If TypeOf Scroll.RenderTransform IsNot TranslateTransform Then Scroll.RenderTransform = New TranslateTransform(10, 0)
            AniList.Add(AaTranslateX(Scroll, -CType(Scroll.RenderTransform, TranslateTransform).X, 350, 0, New AniEaseOutFluent))
        End If

        AniList.Add(AaCode(Sub() PageOnEnterAnimationFinished(),, True))
        AniStart(AniList, "PageRight PageChange " & PageUuid, True)
    End Sub

    Public Sub TriggerExitAnimation(ParamArray Elements As FrameworkElement())
        Dim RealElements = Elements.Where(Function(e) e IsNot Nothing)
        Dim AniList As New List(Of AniData)
        Dim Delay As Integer = 0
        For Each Element In RealElements
            For Each Control As FrameworkElement In GetAllAnimControls(Element)
                If TypeOf Control Is MyExtraTextButton Then
                    CType(Control, MyExtraTextButton).Show = False
                Else
                    Control.IsHitTestVisible = False
                    AniList.Add(AaOpacity(Control, -1, 70, Delay))
                    AniList.Add(AaTranslateY(Control, -6, 70, Delay))
                    Delay += 15
                End If
            Next
        Next

        Dim Scroll As MyScrollBar = GetFirstScrollViewer(RealElements)
        If Scroll IsNot Nothing Then
            If TypeOf Scroll.RenderTransform IsNot TranslateTransform Then Scroll.RenderTransform = New TranslateTransform
            AniList.Add(AaTranslateX(Scroll, 10 - CType(Scroll.RenderTransform, TranslateTransform).X, 90, 0, New AniEaseInFluent))
        End If

        AniList.Add(AaCode(
        Sub()
            For Each Element In RealElements
                Element.Visibility = Visibility.Collapsed
            Next
            PageOnExitAnimationFinished()
        End Sub,, True))
        AniStart(AniList, "PageRight PageChange " & PageUuid)
    End Sub

    Public DisabledPageAnimControls As New List(Of FrameworkElement)

    Friend Function GetAllAnimControls(Element As FrameworkElement, Optional IgnoreInvisibility As Boolean = False) As IEnumerable(Of FrameworkElement)
        Dim AllControls As New List(Of FrameworkElement)
        _GetAllAnimControls(Element, AllControls, IgnoreInvisibility)
        Return AllControls.Except(DisabledPageAnimControls)
    End Function
    Private Sub _GetAllAnimControls(Element As FrameworkElement, ByRef AllControls As List(Of FrameworkElement), IgnoreInvisibility As Boolean)
        If Not IgnoreInvisibility AndAlso Element.Visibility = Visibility.Collapsed Then Return
        If TypeOf Element Is MyCard OrElse TypeOf Element Is MyHint OrElse TypeOf Element Is MyExtraTextButton OrElse TypeOf Element Is TextBlock OrElse TypeOf Element Is MyTextButton Then
            AllControls.Add(Element)
        ElseIf TypeOf Element Is ContentControl Then
            Dim Content = CType(Element, ContentControl).Content
            If Content IsNot Nothing AndAlso TypeOf Content Is FrameworkElement Then _GetAllAnimControls(Content, AllControls, IgnoreInvisibility)
        ElseIf TypeOf Element Is Panel Then
            For Each Element2 In CType(Element, Panel).Children
                If TypeOf Element2 Is FrameworkElement Then _GetAllAnimControls(Element2, AllControls, IgnoreInvisibility)
            Next
        End If
    End Sub

    Private Function GetFirstScrollViewer(Elements As IEnumerable(Of FrameworkElement)) As MyScrollBar
        Dim Viewer As MyScrollViewer = Nothing
        For Each Element In Elements
            If TypeOf Element Is MyScrollViewer Then
                Viewer = Element
                GoTo FindViewer
            End If
            For Each Control In LogicalTreeHelper.GetChildren(Element)
                If TypeOf Control Is MyScrollViewer Then
                    Viewer = Control
                    GoTo FindViewer
                End If
            Next
        Next
        Return Nothing
FindViewer:
        If Viewer.ComputedVerticalScrollBarVisibility <> Visibility.Visible Then Return Nothing
        Return Viewer.ScrollBar
    End Function

#End Region

End Class
