Public Class PageDownloadLeft
    Implements IRefreshable

#Region "页面切换"

    Public PageID As FormMain.PageSubType = FormMain.PageSubType.DownloadInstall

    Public Sub New()
        InitializeComponent()
        If Not RestrictMcDownload Then Return
        ItemInstall.Visibility = Visibility.Collapsed
        LabResource.Visibility = Visibility.Collapsed
        PageID = FormMain.PageSubType.DownloadMod
        ItemMod.Checked = True
    End Sub

    Private Sub PageCheck(sender As FrameworkElement, e As RouteEventArgs) Handles ItemInstall.Check, ItemMod.Check, ItemPack.Check, ItemResourcePack.Check, ItemShader.Check, ItemDataPack.Check, ItemMap.Check

        If sender.Tag IsNot Nothing Then PageChange(Val(sender.Tag))
    End Sub

    Public Function PageGet(Optional ID As FormMain.PageSubType = -1)
        If ID = -1 Then ID = PageID
        Select Case ID
            Case FormMain.PageSubType.DownloadInstall
                If FrmDownloadInstall Is Nothing Then FrmDownloadInstall = New PageDownloadInstall
                Return FrmDownloadInstall
            Case FormMain.PageSubType.DownloadMod
                If FrmDownloadMod Is Nothing Then FrmDownloadMod = New PageDownloadMod
                Return FrmDownloadMod
            Case FormMain.PageSubType.DownloadPack
                If FrmDownloadPack Is Nothing Then FrmDownloadPack = New PageDownloadPack
                Return FrmDownloadPack
            Case FormMain.PageSubType.DownloadResourcePack
                If FrmDownloadResourcePack Is Nothing Then FrmDownloadResourcePack = New PageDownloadResourcePack
                Return FrmDownloadResourcePack
            Case FormMain.PageSubType.DownloadShader
                If FrmDownloadShader Is Nothing Then FrmDownloadShader = New PageDownloadShader
                Return FrmDownloadShader
            Case FormMain.PageSubType.DownloadDataPack
                If FrmDownloadDataPack Is Nothing Then FrmDownloadDataPack = New PageDownloadDataPack
                Return FrmDownloadDataPack
            Case FormMain.PageSubType.DownloadMap
                If FrmDownloadMap Is Nothing Then FrmDownloadMap = New PageDownloadMap
                Return FrmDownloadMap
            Case Else
                Throw New Exception("未知的下载子页面种类：" & ID)
        End Select
    End Function

    Public Sub PageChange(ID As FormMain.PageSubType)
        If PageID = ID Then Return
        AniControlEnabled += 1
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
            AaCode(
            Sub()
                CType(FrmMain.PanMainRight.Child, MyPageRight).PageOnForceExit()
                FrmMain.PanMainRight.Child = FrmMain.PageRight
                FrmMain.PageRight.Opacity = 0
            End Sub, 130),
            AaCode(
            Sub()

                FrmMain.PageRight.Opacity = 1
                FrmMain.PageRight.PageOnEnter()
            End Sub, 30, True)
        }, "PageLeft PageChange")
    End Sub

#End Region

    Public Sub Refresh_Click(sender As Object, e As EventArgs)
        Refresh(Val(sender.Tag))
    End Sub
    Public Sub Refresh() Implements IRefreshable.Refresh
        Refresh(FrmMain.PageCurrentSub)
    End Sub
    Public Sub Refresh(SubType As FormMain.PageSubType)
        ResourceProject.Cache.Clear()
        ResourceVersion.ProjectFilesCache.Clear()
        Select Case SubType
            Case FormMain.PageSubType.DownloadInstall
                DlClientListLoader.Start(IsForceRestart:=True)
                DlOptiFineListLoader.Start(IsForceRestart:=True)
                DlForgeListLoader.Start(IsForceRestart:=True)
                DlNeoForgeListLoader.Start(IsForceRestart:=True)
                DlLiteLoaderListLoader.Start(IsForceRestart:=True)
                DlFabricListLoader.Start(IsForceRestart:=True)
                DlFabricApiLoader.Start(IsForceRestart:=True)
                DlOptiFabricLoader.Start(IsForceRestart:=True)
                ItemInstall.Checked = True
            Case FormMain.PageSubType.DownloadMod
                If FrmDownloadMod IsNot Nothing Then
                    FrmDownloadMod.Content.Storage = New ResourceSearcher.SearchResult
                    FrmDownloadMod.Content.Page = 0
                    FrmDownloadMod.PageLoaderRestart()
                End If
                ItemMod.Checked = True
            Case FormMain.PageSubType.DownloadPack
                If FrmDownloadPack IsNot Nothing Then
                    FrmDownloadPack.Content.Storage = New ResourceSearcher.SearchResult
                    FrmDownloadPack.Content.Page = 0
                    FrmDownloadPack.PageLoaderRestart()
                End If
                ItemPack.Checked = True
            Case FormMain.PageSubType.DownloadResourcePack
                If FrmDownloadResourcePack IsNot Nothing Then
                    FrmDownloadResourcePack.Content.Storage = New ResourceSearcher.SearchResult
                    FrmDownloadResourcePack.Content.Page = 0
                    FrmDownloadResourcePack.PageLoaderRestart()
                End If
                ItemResourcePack.Checked = True
            Case FormMain.PageSubType.DownloadShader
                If FrmDownloadShader IsNot Nothing Then
                    FrmDownloadShader.Content.Storage = New ResourceSearcher.SearchResult
                    FrmDownloadShader.Content.Page = 0
                    FrmDownloadShader.PageLoaderRestart()
                End If
                ItemShader.Checked = True
            Case FormMain.PageSubType.DownloadDataPack
                If FrmDownloadDataPack IsNot Nothing Then
                    FrmDownloadDataPack.Content.Storage = New ResourceSearcher.SearchResult
                    FrmDownloadDataPack.Content.Page = 0
                    FrmDownloadDataPack.PageLoaderRestart()
                End If
                ItemDataPack.Checked = True
            Case FormMain.PageSubType.DownloadMap
                If FrmDownloadMap IsNot Nothing Then
                    FrmDownloadMap.Content.Storage = New ResourceSearcher.SearchResult
                    FrmDownloadMap.Content.Page = 0
                    FrmDownloadMap.PageLoaderRestart()
                End If
                ItemMap.Checked = True
        End Select
        Hint("正在刷新……", Log:=False)
    End Sub

    Private Sub ItemInstall_Click(sender As Object, e As MouseButtonEventArgs) Handles ItemInstall.Click
        If Not ItemInstall.Checked Then Return
        FrmDownloadInstall.ExitSelectPage()
    End Sub

End Class
