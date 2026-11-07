Imports System.Windows.Markup

<ContentProperty("SearchTags")>
Public Class PageResource

#Region "属性"

    Public ReadOnly Property SearchTags As ItemCollection
        Get
            Return ComboSearchTag.Items
        End Get
    End Property

    Public Property TypeName As String
        Get
            Return _TypeName
        End Get
        Set(Value As String)
            If _TypeName = Value Then Return
            _TypeName = Value
            Loader.Name = $"社区资源获取：{Value}"
        End Set
    End Property
    Private _TypeName As String = ""

    Public Property TypeNameSpaced As String
        Get
            Return _TypeNameSpaced
        End Get
        Set(Value As String)
            If _TypeNameSpaced = Value Then Return
            _TypeNameSpaced = Value
            PanAlways.Title = $"搜索{Value}"
            Load.Text = $"正在获取{Value}列表"
        End Set
    End Property
    Private _TypeNameSpaced As String = ""

    Public Property PageType As ResourceTypes
        Get
            Return _Type
        End Get
        Set(Value As ResourceTypes)
            If _Type = Value Then Return
            _Type = Value
            BtnSearchInstallModPack.Visibility = If(Value = ResourceTypes.ModPack AndAlso Not RestrictMcDownload, Visibility.Visible, Visibility.Collapsed)
        End Set
    End Property
    Private _Type As ResourceTypes = -1

#End Region

#Region "加载"

    Public Shared TargetInstance As McInstance = Nothing

    Public Shared TargetName As String = Nothing

    Public Loader As LoaderTask(Of ResourceSearcher.SearchRequest, Integer) = ResourceSearcher.GetLoader(AddressOf LoaderInput)

    Private Sub PageResource_Loaded(sender As Object, e As EventArgs) Handles Me.Loaded
        Static IsFirstLoaded As Boolean = False
        ApplyCurseForgeOnly()

        If TargetInstance IsNot Nothing Then

            ResetFilter()
            TextSearchVersion.Text = TargetInstance.Version.VanillaName
            Dim GetTargetItemByName =
            Function(Name As String) As MyComboBoxItem
                For Each Item As MyComboBoxItem In ComboSearchLoader.Items
                    If Item.Content = Name Then Return Item
                Next
                Return ComboSearchLoader.Items(0)
            End Function
            If TargetInstance.Version.ModLoaders.HasFlagF(ModLoaders.Forge) Then
                ComboSearchLoader.SelectedItem = GetTargetItemByName("Forge")
            ElseIf TargetInstance.Version.ModLoaders.HasFlagF(ModLoaders.Fabric) Then
                ComboSearchLoader.SelectedItem = GetTargetItemByName("Fabric")
            ElseIf TargetInstance.Version.ModLoaders.HasFlagF(ModLoaders.NeoForge) Then
                ComboSearchLoader.SelectedItem = GetTargetItemByName("NeoForge")
            End If
            TargetInstance = Nothing
            If TargetName IsNot Nothing Then
                TextSearchName.Text = TargetName
                TargetName = Nothing
            End If

            If IsFirstLoaded Then StartNewSearch()
            ScrollToHome()
        End If

        If IsFirstLoaded Then Return
        IsFirstLoaded = True
        CType(Parent, MyPageRight).PageLoaderInit(Load, PanLoad, PanContent, PanAlways, Loader, AddressOf Load_OnFinish, AddressOf LoaderInput)

        If AllDrops IsNot Nothing AndAlso AllDrops.First > 250 Then
            Dim HighestVersion As String = McVersion.DropToVersion(AllDrops.First)
            If CType(TextSearchVersion.Items(1), MyComboBoxItem).Content.ToString <> HighestVersion Then
                TextSearchVersion.Items.Insert(1, New MyComboBoxItem With {.Content = HighestVersion})
            End If
        End If
    End Sub
    Private Function LoaderInput() As ResourceSearcher.SearchRequest
        Dim Request As New ResourceSearcher.SearchRequest(PageType, Storage, (Page + 1) * PAGE_SIZE)
        Dim GameVersion As String = If(TextSearchVersion.Text = "全部 (也可自行输入)", Nothing,
            If(TextSearchVersion.Text.Contains(".") OrElse TextSearchVersion.Text.Contains("w"), TextSearchVersion.Text, Nothing))
        With Request
            .SearchText = TextSearchName.Text
            .GameVersion = GameVersion
            .Tag = ComboSearchTag.SelectedItem.Tag
            .ModLoaders = If(PageType = ResourceTypes.Mod, Val(ComboSearchLoader.SelectedItem.Tag), ModLoaders.None)
            .Sources = CType(Val(ComboSearchSource.SelectedItem.Tag), ResourcePlatforms)
        End With
        Return Request
    End Function

#End Region

    Public Storage As New ResourceSearcher.SearchResult

    Public Const PAGE_SIZE = 40
    Public Page As Integer = 0

    Private Sub Load_OnFinish()
        Try
            Logger.Info($"开始可视化{TypeNameSpaced}列表，已储藏 {Storage.Results.Count} 个结果，当前在第 {Page + 1} 页")

            PanProjects.Children.Clear()
            Dim Index As Integer = Math.Min(Page * PAGE_SIZE, Storage.Results.Count - 1)
            For Each Result In Storage.Results.GetRange(Index, Math.Min(Storage.Results.Count - Index, PAGE_SIZE))
                PanProjects.Children.Add(Result.ToResourceItem(
                    ShowMcVersionDesc:=Loader.Input.GameVersion Is Nothing,
                    ShowLoaderDesc:=Loader.Input.ModLoaders = ModLoaders.None AndAlso (PageType = ResourceTypes.Mod OrElse PageType = ResourceTypes.ModPack)))
            Next

            CardPages.Visibility = If(Storage.Results.Count > 40 OrElse
                                      Storage.CurseForgeOffset < Storage.CurseForgeTotal OrElse Storage.ModrinthOffset < Storage.ModrinthTotal,
                                      Visibility.Visible, Visibility.Collapsed)
            LabPage.Text = Page + 1
            BtnPageFirst.IsEnabled = Page > 1
            BtnPageFirst.Opacity = If(Page > 1, 1, 0.2)
            BtnPageLeft.IsEnabled = Page > 0
            BtnPageLeft.Opacity = If(Page > 0, 1, 0.2)
            Dim IsRightEnabled As Boolean =
                Storage.Results.Count > PAGE_SIZE * (Page + 1) OrElse
                Storage.CurseForgeOffset < Storage.CurseForgeTotal OrElse Storage.ModrinthOffset < Storage.ModrinthTotal
            BtnPageRight.IsEnabled = IsRightEnabled
            BtnPageRight.Opacity = If(IsRightEnabled, 1, 0.2)

            If Storage.ErrorMessage Is Nothing Then
                HintError.Visibility = Visibility.Collapsed
            Else
                HintError.Visibility = Visibility.Visible
                HintError.Text = Storage.ErrorMessage
            End If

            ScrollToTop()
        Catch ex As Exception
            Logger.Error(ex, $"可视化{TypeNameSpaced}列表出错")
        End Try
    End Sub

    Private Sub Load_State(sender As Object, state As MyLoading.MyLoadingState, oldState As MyLoading.MyLoadingState) Handles Load.StateChanged
        Select Case Loader.State
            Case LoadState.Failed
                Dim ErrorMessage As String = ""
                If Loader.Error IsNot Nothing Then ErrorMessage = Loader.Error.Message
                If ErrorMessage.Contains("不是有效的 json 文件") Then
                    Logger.Warn($"下载的{TypeNameSpaced}列表 json 文件损坏，已自动重试")
                    CType(Parent, MyPageRight).PageLoaderRestart()
                End If
        End Select
    End Sub

    Private Sub BtnPageFirst_Click(sender As Object, e As RoutedEventArgs) Handles BtnPageFirst.Click
        ChangePage(0)
    End Sub
    Private Sub BtnPageLeft_Click(sender As Object, e As RoutedEventArgs) Handles BtnPageLeft.Click
        ChangePage(Page - 1)
    End Sub
    Private Sub BtnPageRight_Click(sender As Object, e As RoutedEventArgs) Handles BtnPageRight.Click
        ChangePage(Page + 1)
    End Sub
    Private Sub ChangePage(NewPage As Integer)
        CardPages.IsEnabled = False
        Page = NewPage
        FrmMain.BackToTop()
        Logger.Info($"{TypeName}：切换到第 {Page + 1} 页")
        RunInThread(
        Sub()
            Thread.Sleep(100)
            RunInUi(Sub() CardPages.IsEnabled = True)
            Loader.Start()
        End Sub)
    End Sub

#Region "搜索"

    Private Sub StartNewSearch() Handles BtnSearchRun.Click
        Page = 0
        If Loader.ShouldStart(LoaderInput()) Then Storage = New ResourceSearcher.SearchResult
        Loader.Start()
    End Sub
    Private Sub EnterTrigger(sender As Object, e As KeyEventArgs) Handles TextSearchName.KeyDown, TextSearchVersion.KeyDown
        If e.Key = Key.Enter Then StartNewSearch()
    End Sub

    Private Sub ResetFilter() Handles BtnSearchReset.Click
        TextSearchName.Text = ""
        TextSearchVersion.Text = "全部 (也可自行输入)"
        TextSearchVersion.SelectedIndex = 0
        ComboSearchSource.SelectedIndex = If(PageType = ResourceTypes.Map, 1, 0)
        ComboSearchTag.SelectedIndex = 0
        ComboSearchLoader.SelectedIndex = 0
        Loader.LastFinishedTime = 0
    End Sub

    Private Sub ApplyCurseForgeOnly()
        If PageType <> ResourceTypes.Map OrElse ComboSearchSource Is Nothing Then Return
        For Each Item In ComboSearchSource.Items
            Dim ComboItem = TryCast(Item, MyComboBoxItem)
            If ComboItem Is Nothing Then Continue For
            If ComboItem.Tag Is Nothing OrElse ComboItem.Tag.ToString() <> CStr(CInt(ResourcePlatforms.CurseForge)) Then
                ComboItem.Visibility = Visibility.Collapsed
            End If
        Next
        ComboSearchSource.SelectedIndex = 1
    End Sub

    Private Sub TextSearchVersion_TextChanged() Handles TextSearchVersion.TextChanged
        If Not TextSearchVersion.IsDropDownOpen Then UpdateSearchLoaderVisibility()
    End Sub
    Private Sub UpdateSearchLoaderVisibility() Handles TextSearchVersion.DropDownClosed
        If PageType = ResourceTypes.Mod AndAlso (TextSearchVersion.Text.Contains(".") OrElse TextSearchVersion.Text.Contains("w")) Then
            ComboSearchLoader.Visibility = Visibility.Visible
            Grid.SetColumnSpan(TextSearchVersion, 1)
        Else
            ComboSearchLoader.Visibility = Visibility.Collapsed
            Grid.SetColumnSpan(TextSearchVersion, 2)
            ComboSearchLoader.SelectedIndex = 0
        End If
    End Sub

#End Region

    Private Sub BtnSearchInstallModPack_Click(sender As Object, e As EventArgs) Handles BtnSearchInstallModPack.Click
        ModpackInstall()
    End Sub

End Class
