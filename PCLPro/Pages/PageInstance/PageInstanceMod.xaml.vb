Public Class PageInstanceMod
    Implements IRefreshable

#Region "初始化"

    Private IsLoad As Boolean = False
    Public Sub PageOther_Loaded() Handles Me.Loaded

        If FrmMain.PageLast.Page <> FormMain.PageType.ResourceDetail Then PanBack.ScrollToHome()
        AniControlEnabled += 1
        SelectedMods.Clear()
        ReloadModList()
        ChangeAllSelected(False)
        AniControlEnabled -= 1

        If IsLoad Then Return
        IsLoad = True

        AddHandler FrmMain.KeyDown, AddressOf FrmMain_KeyDown

        For Each Btn As MyRadioButton In PanFilter.Children
            Btn.LabText.Margin = New Thickness(-2, 0, 8, 0)
        Next

    End Sub

    Public Sub ReloadModList(Optional ForceReload As Boolean = False)
        If Not LoaderRun(If(ForceReload, LoaderFolderRunType.ForceRun, LoaderFolderRunType.RunOnUpdated)) Then Return
        Logger.Info("已刷新 Mod 列表")
        Filter = FilterType.All
        PanBack.ScrollToHome()
        SearchBox.Text = ""
    End Sub

    Private Sub RefreshSelf() Implements IRefreshable.Refresh
        Refresh()
    End Sub
    Public Shared Sub Refresh()
        Try
            ResourceProject.Cache.Clear()
            ResourceVersion.ProjectFilesCache.Clear()
            FileUtils.Delete(PathTemp & "Cache\LocalMod.json")
            Logger.Info("由于点击刷新按钮，清理本地 Mod 信息缓存")
        Catch ex As Exception
            Logger.Warn(ex, "强制刷新时清理本地 Mod 信息缓存失败")
        End Try
        If FrmInstanceMod IsNot Nothing Then FrmInstanceMod.ReloadModList(True)
        FrmInstanceLeft.ItemMod.Checked = True
        Hint("正在刷新……", Log:=False)
    End Sub

    Private Sub LoaderInit() Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanAllBack, Nothing, LocalResourceLoader, AddressOf LoadUIFromLoaderOutput, AutoRun:=False)
    End Sub
    Private Sub Load_Click(sender As Object, e As MouseButtonEventArgs) Handles Load.Click
        If LocalResourceLoader.State = LoadState.Failed Then LoaderRun(LoaderFolderRunType.ForceRun)
    End Sub
    Public Function LoaderRun(Type As LoaderFolderRunType) As Boolean
        Return LoaderFolderRun(LocalResourceLoader, PageInstanceLeft.Instance.PathIndie & "mods\", Type)
    End Function

#End Region

#Region "UI 化"

    Public ModItems As New ConcurrentDictionary(Of String, MyLocalModItem)

    Private Sub LoadUIFromLoaderOutput()
        Try

            If LocalResourceLoader.Output.Any() Then
                PanBack.Visibility = Visibility.Visible
                PanEmpty.Visibility = Visibility.Collapsed
            Else
                PanEmpty.Visibility = Visibility.Visible
                PanBack.Visibility = Visibility.Collapsed
                Return
            End If

            ModItems.Clear()
            For Each ModEntity As LocalResourceFile In LocalResourceLoader.Output
                ModItems(ModEntity.EnabledName) = McModListItem(ModEntity)
            Next

            Filter = FilterType.All
            SearchBox.Text = ""
            RefreshUI()
        Catch ex As Exception
            Logger.Error(ex, "加载 Mod 列表 UI 失败")
        End Try
    End Sub
    Private Function McModListItem(Entry As LocalResourceFile) As MyLocalModItem
        AniControlEnabled += 1
        Dim NewItem As New MyLocalModItem With {.SnapsToDevicePixels = True, .Entry = Entry,
            .ButtonHandler = AddressOf McModContent, .Checked = SelectedMods.Contains(Entry.EnabledName)}
        AddHandler Entry.OnUpdate, Sub() RunInUi(AddressOf NewItem.Refresh)
        AniControlEnabled -= 1
        Return NewItem
    End Function
    Private Sub McModContent(sender As MyLocalModItem, e As EventArgs)

        AddHandler sender.Changed, AddressOf CheckChanged
        AddHandler sender.Click, Sub(ss As MyLocalModItem, ee As EventArgs) ss.Checked = Not ss.Checked

        Dim BtnOpen As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonOpen, .Tag = sender}
        BtnOpen.ToolTip = "打开文件位置"
        AddHandler BtnOpen.Click, AddressOf Open_Click
        Dim BtnCont As New MyIconButton With {.LogoScale = 1, .Logo = Logo.IconButtonInfo, .Tag = sender}
        BtnCont.ToolTip = "详情"
        AddHandler BtnCont.Click, AddressOf Info_Click
        AddHandler sender.MouseRightButtonUp, AddressOf Info_Click
        Dim BtnDelete As New MyIconButton With {.LogoScale = 1, .Logo = Logo.IconButtonDelete, .Tag = sender}
        BtnDelete.ToolTip = "删除"
        AddHandler BtnDelete.Click, AddressOf Delete_Click
        Dim BtnED As New MyIconButton With {.LogoScale = 1, .Logo = If(sender.Entry.IsEnabled, Logo.IconButtonStop, Logo.IconButtonCheck), .Tag = sender}
        BtnED.ToolTip = If(sender.Entry.IsEnabled, "禁用", "启用")
        AddHandler BtnED.Click, AddressOf ED_Click
        sender.Buttons = {BtnCont, BtnOpen, BtnED, BtnDelete}
    End Sub

    Public Sub RefreshUI()
        If PanList Is Nothing Then Return
        Dim ShowingMods = If(IsSearching, SearchResult, If(LocalResourceLoader.Output, New List(Of LocalResourceFile))).Where(Function(m) CanPassFilter(m)).ToList

        AniControlEnabled += 1
        If ShowingMods.Any() Then
            PanList.Visibility = Visibility.Visible
            PanList.Children.Clear()
            For Each TargetMod In ShowingMods
                Dim Item As MyLocalModItem = Nothing
                If Not ModItems.TryGetValue(TargetMod.EnabledName, Item) Then Continue For
                Item.Checked = SelectedMods.Contains(TargetMod.EnabledName)
                PanList.Children.Add(Item)
            Next
        Else
            PanList.Visibility = Visibility.Collapsed
        End If
        AniControlEnabled -= 1
        SelectedMods = SelectedMods.Where(Function(m) ShowingMods.Any(Function(s) s.EnabledName = m)).ToList
        RefreshBars()
    End Sub

    Public Sub RefreshBars()

        Dim AnyCount As Integer = 0
        Dim EnabledCount As Integer = 0
        Dim UpdateCount As Integer = 0
        For Each ModItem In If(IsSearching, SearchResult, If(LocalResourceLoader.Output, New List(Of LocalResourceFile)))
            AnyCount += 1
            If ModItem.HasUpdate Then UpdateCount += 1
            If ModItem.IsEnabled Then EnabledCount += 1
        Next
        Dim DisabledCount As Integer = AnyCount - EnabledCount

        BtnFilterAll.Text = If(IsSearching, "搜索结果", "全部") & $" ({AnyCount})"
        BtnFilterCanUpdate.Text = $"可更新 ({UpdateCount})"
        BtnFilterCanUpdate.Visibility = If(Filter = FilterType.CanUpdate OrElse UpdateCount > 0, Visibility.Visible, Visibility.Collapsed)
        BtnFilterEnabled.Text = $"启用 ({EnabledCount})"
        BtnFilterEnabled.Visibility = If(Filter = FilterType.Enabled OrElse (EnabledCount > 0 AndAlso EnabledCount < AnyCount), Visibility.Visible, Visibility.Collapsed)
        BtnFilterDisabled.Text = $"禁用 ({DisabledCount})"
        BtnFilterDisabled.Visibility = If(Filter = FilterType.Disabled OrElse DisabledCount > 0, Visibility.Visible, Visibility.Collapsed)

        Dim NewCount As Integer = SelectedMods.Count
        Dim Selected = NewCount > 0
        If Selected Then LabSelect.Text = $"已选择 {NewCount} 个文件"

        If Selected Then
            Dim HasUpdate As Boolean = False
            Dim HasEnabled As Boolean = False
            Dim HasDisabled As Boolean = False
            For Each ModEntity In LocalResourceLoader.Output
                If Not SelectedMods.Contains(ModEntity.EnabledName) Then Continue For
                If ModEntity.HasUpdate Then HasUpdate = True
                If ModEntity.IsEnabled Then HasEnabled = True
                If Not ModEntity.IsEnabled Then HasDisabled = True
            Next
            BtnSelectDisable.IsEnabled = HasEnabled
            BtnSelectEnable.IsEnabled = HasDisabled
            BtnSelectUpdate.IsEnabled = HasUpdate
        End If

        If AniControlEnabled = 0 Then
            PanListBack.Margin = New Thickness(0, 0, 0, If(Selected, 95, 15))
            If Selected Then

                If BottomBarShownCount >= NewCount Then
                    BottomBarShownCount = NewCount
                    Return
                Else
                    BottomBarShownCount = NewCount
                End If

                CardSelect.Visibility = Visibility.Visible
                AniStart({
                    AaOpacity(CardSelect, 1 - CardSelect.Opacity, 60),
                    AaTranslateY(CardSelect, -27 - TransSelect.Y, 120, Ease:=New AniEaseOutFluent(AniEasePower.Weak)),
                    AaTranslateY(CardSelect, 3, 150, 120, Ease:=New AniEaseInoutFluent(AniEasePower.Weak)),
                    AaTranslateY(CardSelect, -1, 90, 270, Ease:=New AniEaseInoutFluent(AniEasePower.Weak))
                }, "Mod Sidebar")
            Else

                If BottomBarShownCount = 0 Then Return
                BottomBarShownCount = 0

                AniStart({
                    AaOpacity(CardSelect, -CardSelect.Opacity, 90),
                    AaTranslateY(CardSelect, -10 - TransSelect.Y, 90, Ease:=New AniEaseInFluent(AniEasePower.Weak)),
                    AaCode(Sub() CardSelect.Visibility = Visibility.Collapsed, After:=True)
                }, "Mod Sidebar")
            End If
        Else
            AniStop("Mod Sidebar")
            BottomBarShownCount = NewCount
            If Selected Then
                CardSelect.Visibility = Visibility.Visible
                CardSelect.Opacity = 1
                TransSelect.Y = -25
            Else
                CardSelect.Visibility = Visibility.Collapsed
                CardSelect.Opacity = 0
                TransSelect.Y = -10
            End If
        End If
    End Sub
    Private BottomBarShownCount As Integer = 0

#End Region

#Region "管理"

    Private Sub BtnManageOpen_Click(sender As Object, e As EventArgs) Handles BtnManageOpen.Click, BtnHintOpen.Click
        Try
            DirectoryUtils.Create(PageInstanceLeft.Instance.PathIndie & "mods\")
            OpenExplorer(PageInstanceLeft.Instance.PathIndie & "mods\")
        Catch ex As Exception
            Logger.Error(ex, "打开 Mods 文件夹失败", LogBehavior.Alert)
        End Try
    End Sub

    Private Sub BtnManageSelectAll_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnManageSelectAll.Click
        ChangeAllSelected(SelectedMods.Count < PanList.Children.Count)
    End Sub

    Private Sub BtnManageInstall_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnManageInstall.Click, BtnHintInstall.Click
        Dim FileList = Dialogs.SelectFile("选择要安装的 Mod", True, filter:={({"jar", "litemod", "disabled", "old"}, "Mod 文件")})
        If Not FileList.Any Then Return
        InstallMods(FileList)
    End Sub

    Public Shared Function InstallMods(FilePathList As IEnumerable(Of String)) As Boolean
        Dim Extension As String = PathUtils.GetExtension(FilePathList.First)

        If Not {"jar", "litemod", "disabled", "old"}.Any(Function(t) t = Extension) Then Return False
        Logger.Info("文件为 jar/litemod 格式，尝试作为 Mod 安装")

        If FilePathList.First.Contains(":\$RECYCLE.BIN\") Then
            Hint("请先将文件从回收站还原，再尝试安装！", HintType.Red)
            Return True
        End If

        Dim Instance As McInstance = McInstanceSelected
        If FrmMain.PageCurrent = FormMain.PageType.InstanceSetup Then Instance = PageInstanceLeft.Instance
        If FrmMain.PageCurrent = FormMain.PageType.InstanceSelect OrElse Instance Is Nothing OrElse Not Instance.Modable Then

            Hint("若要安装 Mod，请先选择一个可以安装 Mod 的版本！")
        ElseIf Not (FrmMain.PageCurrent = FormMain.PageType.InstanceSetup AndAlso FrmMain.PageCurrentSub = FormMain.PageSubType.InstanceMod) Then

            If MyMsgBox($"是否要将这{If(FilePathList.IsSingle, "个", "些")}文件作为 Mod 安装到 {Instance.Name}？", "Mod 安装确认", "确定", "取消") = 1 Then GoTo Install
        Else

Install:
            Try
                For Each ModFile In FilePathList
                    Dim NewFileName = (New LocalResourceFile(ModFile)).EnabledName
                    If Not NewFileName.Contains(".") Then NewFileName += ".jar"
                    NewFileName = Instance.PathIndie & "mods\" & NewFileName
                    If PathUtils.IsSame(ModFile, NewFileName) Then Continue For
                    FileUtils.Delete(NewFileName, True)
                    FileUtils.Copy(ModFile, NewFileName)
                Next
                If FilePathList.IsSingle Then
                    Hint($"已安装 {(New LocalResourceFile(FilePathList.First)).EnabledName}！", HintType.Green)
                Else
                    Hint($"已安装 {FilePathList.Count} 个 Mod！", HintType.Green)
                End If

                If FrmMain.PageCurrent = FormMain.PageType.InstanceSetup AndAlso FrmMain.PageCurrentSub = FormMain.PageSubType.InstanceMod Then
                    LoaderFolderRun(LocalResourceLoader, Instance.PathIndie & "mods\", LoaderFolderRunType.ForceRun)
                End If
            Catch ex As Exception
                Logger.Error(ex, "复制 Mod 文件失败", LogBehavior.Alert)
            End Try
        End If
        Return True
    End Function

    Private Sub BtnManageDownload_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnManageDownload.Click, BtnHintDownload.Click

        PageResource.TargetInstance = PageInstanceLeft.Instance
        PageResource.TargetName = SearchBox.Text
        FrmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadMod)
    End Sub

#End Region

#Region "选择"

    Public SelectedMods As New List(Of String)

    Public Sub CheckChanged(sender As MyLocalModItem, e As RouteEventArgs)
        If AniControlEnabled <> 0 Then Return

        Dim SelectedKey As String = sender.Entry.EnabledName
        If sender.Checked Then
            If Not SelectedMods.Contains(SelectedKey) Then SelectedMods.Add(SelectedKey)
        Else
            SelectedMods.Remove(SelectedKey)
        End If
        RefreshBars()
    End Sub

    Private Sub ChangeAllSelected(Value As Boolean)
        AniControlEnabled += 1
        SelectedMods.Clear()
        For Each Item As MyLocalModItem In ModItems.Values

            Dim ShouldSelected As Boolean = Value AndAlso PanList.Children.Contains(Item)
            Item.Checked = ShouldSelected
            If ShouldSelected Then SelectedMods.Add(Item.Entry.EnabledName)
        Next
        AniControlEnabled -= 1
        RefreshBars()
    End Sub
    Private Sub UnselectedAllWithAnimation() Handles Load.StateChanged, Me.PageExit
        Dim CacheAniControlEnabled = AniControlEnabled
        AniControlEnabled = 0
        ChangeAllSelected(False)
        AniControlEnabled += CacheAniControlEnabled
    End Sub
    Private Sub FrmMain_KeyDown(sender As Object, e As KeyEventArgs)
        If FrmMain.PageRight IsNot Me Then Return
        If My.Computer.Keyboard.CtrlKeyDown AndAlso e.Key = Key.A Then ChangeAllSelected(True)
    End Sub
    Private Sub SearchBox_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles SearchBox.PreviewKeyDown

        If SearchBox.Text.Any Then Return
        If My.Computer.Keyboard.CtrlKeyDown AndAlso e.Key = Key.A Then PanBack.Focus()
    End Sub

#End Region

#Region "筛选"

    Private _Filter As FilterType = FilterType.All
    Public Property Filter As FilterType
        Get
            Return _Filter
        End Get
        Set(value As FilterType)
            If _Filter = value Then Return
            _Filter = value
            Select Case value
                Case FilterType.All
                    BtnFilterAll.Checked = True
                Case FilterType.Enabled
                    BtnFilterEnabled.Checked = True
                Case FilterType.Disabled
                    BtnFilterDisabled.Checked = True
                Case Else
                    BtnFilterCanUpdate.Checked = True
            End Select
            RefreshUI()
        End Set
    End Property
    Public Enum FilterType As Integer
        All = 0
        Enabled = 1
        Disabled = 2
        CanUpdate = 3
    End Enum

    Private Function CanPassFilter(CheckingMod As LocalResourceFile) As Boolean
        Select Case Filter
            Case FilterType.All
                Return True
            Case FilterType.Enabled
                Return CheckingMod.IsEnabled
            Case FilterType.Disabled
                Return Not CheckingMod.IsEnabled
            Case FilterType.CanUpdate
                Return CheckingMod.HasUpdate
            Case Else
                Return False
        End Select
    End Function

    Private Sub ChangeFilter(sender As MyRadioButton, raiseByMouse As Boolean) Handles BtnFilterAll.Check, BtnFilterCanUpdate.Check, BtnFilterDisabled.Check, BtnFilterEnabled.Check
        Filter = sender.Tag
        RefreshUI()
    End Sub

#End Region

#Region "下边栏"

    Private Sub BtnSelectED_Click(sender As MyIconTextButton, e As RouteEventArgs) Handles BtnSelectEnable.Click, BtnSelectDisable.Click
        EDMods(LocalResourceLoader.Output.Where(Function(m) SelectedMods.Contains(m.EnabledName)),
               Not sender.Equals(BtnSelectDisable))
        ChangeAllSelected(False)
    End Sub
    Private Sub EDMods(ModList As IEnumerable(Of LocalResourceFile), IsEnable As Boolean)
        Dim IsSuccessful As Boolean = True
        For Each ModE In ModList.ToList
            Dim ModEntity = ModE
            Dim NewPath As String = Nothing
            If ModEntity.IsEnabled AndAlso Not IsEnable Then

                NewPath = PathUtils.RemoveExtendedPrefix(ModEntity.File.FullName) & If(FileUtils.Exists(ModEntity.File.FullName & ".old"), ".old", ".disabled")
            ElseIf Not ModEntity.IsEnabled AndAlso IsEnable Then

                NewPath = ModEntity.EnabledFullName
            Else
                Continue For
            End If

            Try
                If FileUtils.Exists(NewPath) Then
                    If FileUtils.Exists(ModEntity.File.FullName) Then

                        If CryptographyUtils.ComputeFileHash(ModEntity.File.FullName) <> CryptographyUtils.ComputeFileHash(NewPath) Then
                            MyMsgBox($"目前同时存在启用和禁用的两个 Mod 文件：{vbCrLf} - {PathUtils.GetLastPart(NewPath)}{vbCrLf} - {ModEntity.File.Name}{vbCrLf}{vbCrLf}注意，这两个文件的内容并不相同。{vbCrLf}在手动删除或重命名其中一个文件后，才能继续操作。", "存在文件冲突")
                            Continue For
                        End If
                    Else

                        Logger.Warn("Mod 的状态已被切换")
                        Continue For
                    End If
                End If
                FileUtils.Move(ModEntity.File.FullName, NewPath)
            Catch ex As FileNotFoundException
                Logger.Error(ex, $"未找到需要重命名的 Mod（{If(ModEntity.File.FullName, "null")}）")
                ReloadModList(True)
                Return
            Catch ex As Exception
                Logger.Warn(ex, $"重命名 Mod 失败（{If(ModEntity.File.FullName, "null")}）")
                IsSuccessful = False
            End Try

            Dim NewModEntity As New LocalResourceFile(NewPath)
            NewModEntity.FromJson(ModEntity.ToJson)
            If LocalResourceLoader.Output.Contains(ModEntity) Then
                Dim IndexOfLoader As Integer = LocalResourceLoader.Output.IndexOf(ModEntity)
                LocalResourceLoader.Output.RemoveAt(IndexOfLoader)
                LocalResourceLoader.Output.Insert(IndexOfLoader, NewModEntity)
            End If
            If SearchResult IsNot Nothing AndAlso SearchResult.Contains(ModEntity) Then
                Dim IndexOfResult As Integer = SearchResult.IndexOf(ModEntity)
                SearchResult.Remove(ModEntity)
                SearchResult.Insert(IndexOfResult, NewModEntity)
            End If

            Dim NewItem As MyLocalModItem = McModListItem(NewModEntity)
            ModItems(ModEntity.EnabledName) = NewItem
            Dim IndexOfUi As Integer = PanList.Children.IndexOf(PanList.Children.OfType(Of MyLocalModItem).FirstOrDefault(Function(i) i.Entry Is ModEntity))
            If IndexOfUi = -1 Then Continue For
            PanList.Children.RemoveAt(IndexOfUi)
            PanList.Children.Insert(IndexOfUi, NewItem)
            NewItem.UpdateLayout()
        Next
        If IsSuccessful Then
            RefreshBars()
        Else
            Hint("由于文件被占用，Mod 的状态切换失败，请尝试关闭正在运行的游戏后再试！", HintType.Red)
            ReloadModList(True)
        End If
        LoaderRun(LoaderFolderRunType.UpdateOnly)
    End Sub

    Private Sub BtnSelectUpdate_Click() Handles BtnSelectUpdate.Click
        Dim UpdateList As List(Of LocalResourceFile) = LocalResourceLoader.Output.Where(Function(m) SelectedMods.Contains(m.EnabledName) AndAlso m.HasUpdate).ToList()
        If Not UpdateList.Any() Then Return
        UpdateMods(UpdateList)
        ChangeAllSelected(False)
    End Sub

    Public Shared UpdatingInstanceModFolders As New List(Of String)
    Public Sub UpdateMods(ModList As IEnumerable(Of LocalResourceFile))

        If Not Settings.Get(Of Boolean)("HintUpdateMod") OrElse ModList.Count >= 15 Then
            If MyMsgBox($"新版本 Mod 可能不兼容旧存档或者其他 Mod，这可能导致游戏崩溃，甚至永久损坏存档！{vbCrLf}如果你在游玩整合包，请千万不要自行更新 Mod！{vbCrLf}{vbCrLf}在更新前，请先备份存档，并检查 Mod 的更新日志。{vbCrLf}如果更新后出现问题，你也可以在回收站找回更新前的 Mod。", "Mod 更新警告", "我已了解风险，继续更新", "取消", IsWarn:=True) = 1 Then
                Settings.Set("HintUpdateMod", True)
            Else
                Return
            End If
        End If
        Try

            ModList = ModList.ToList()
            Dim FileList As New List(Of NetFile)
            Dim FileCopyList As New Dictionary(Of String, String)
            For Each Entry As LocalResourceFile In ModList
                Dim File As ResourceVersion = Entry.UpdateFile
                If Not File.DownloadAvailable Then Continue For

                Dim CurrentReplaceName = PathUtils.GetFileNameWithoutExtension(Entry.ProjectVersion.FileName)
                Dim NewestReplaceName = PathUtils.GetFileNameWithoutExtension(Entry.UpdateFile.FileName)
                Dim CurrentSegs = CurrentReplaceName.Split("-"c).ToList()
                Dim NewestSegs = NewestReplaceName.Split("-"c).ToList()
                Dim Shortened As Boolean = False
                Do While True
                    If Not CurrentSegs.Any() OrElse Not NewestSegs.Any() Then Exit Do
                    If CurrentSegs.First <> NewestSegs.First Then Exit Do
                    CurrentSegs.RemoveAt(0)
                    NewestSegs.RemoveAt(0)
                    Shortened = True
                Loop
                Do While True
                    If Not CurrentSegs.Any() OrElse Not NewestSegs.Any() Then Exit Do
                    If CurrentSegs.Last <> NewestSegs.Last Then Exit Do
                    CurrentSegs.RemoveAt(CurrentSegs.Count - 1)
                    NewestSegs.RemoveAt(NewestSegs.Count - 1)
                    Shortened = True
                Loop
                If Shortened AndAlso CurrentSegs.Any() AndAlso NewestSegs.Any() Then
                    CurrentReplaceName = CurrentSegs.Join("-")
                    NewestReplaceName = NewestSegs.Join("-")
                End If

                Dim TempAddress As String = PathTemp & "mods\" & Entry.File.Name.Replace(CurrentReplaceName, NewestReplaceName)
                Dim RealAddress As String = Path.Combine(PathUtils.RemoveLastPart(Entry.File.FullName), Entry.File.Name.Replace(CurrentReplaceName, NewestReplaceName))
                FileList.Add(File.ToNetFile(TempAddress, ResourceVersion.DownloadReason.Update, PageInstanceLeft.Instance.Version.VanillaName, PageInstanceLeft.Instance.Version.ModLoaders))
                FileCopyList(TempAddress) = RealAddress
            Next

            Dim InstallLoaders As New List(Of LoaderBase)
            Dim FinishedFileNames As New List(Of String)
            InstallLoaders.Add(New LoaderDownload("下载新版 Mod 文件", FileList) With {.ProgressWeight = ModList.Count * 1.5})
            InstallLoaders.Add(New LoaderTask(Of Integer, Integer)("替换旧版 Mod 文件",
            Sub()
                Try
                    For Each Entry As LocalResourceFile In ModList
                        If FileUtils.Exists(Entry.File.FullName) Then
                            FileUtils.Delete(Entry.File.FullName, True)
                        Else
                            Logger.Warn($"未找到更新前的 Mod 文件，跳过对它的删除：{Entry.File.FullName}")
                        End If
                    Next
                    For Each Entry As KeyValuePair(Of String, String) In FileCopyList
                        If DirectoryUtils.Exists(PathUtils.RemoveLastPart(Entry.Value)) Then
                            FileUtils.Move(Entry.Key, Entry.Value)
                            FinishedFileNames.Add(PathUtils.GetLastPart(Entry.Value))
                        Else
                            Logger.Warn($"更新后的目标文件夹已被删除：{Entry.Value}")
                        End If
                    Next
                Catch ex As OperationCanceledException
                    Logger.Warn(ex, "替换旧版 Mod 文件时被主动取消")
                End Try
            End Sub))

            Dim Loader As New LoaderCombo(Of IEnumerable(Of LocalResourceFile))("Mod 更新：" & PageInstanceLeft.Instance.Name, InstallLoaders)
            Dim PathMods As String = PageInstanceLeft.Instance.PathIndie & "mods\"
            Loader.OnStateChanged =
            Sub()

                Select Case Loader.State
                    Case LoadState.Finished
                        Select Case FinishedFileNames.Count
                            Case 0
                                Logger.Info($"没有 Mod 被成功更新")
                            Case 1
                                Hint($"已成功更新 {FinishedFileNames.Single}！", HintType.Green)
                            Case Else
                                Hint($"已成功更新 {FinishedFileNames.Count} 个 Mod！", HintType.Green)
                        End Select
                    Case LoadState.Failed
                        Hint("Mod 更新失败：" & Loader.Error.GetDisplay(False), HintType.Red)
                    Case LoadState.Canceled
                        Hint("Mod 更新已中止！", HintType.Blue)
                    Case Else
                        Return
                End Select
                Logger.Info($"已从正在进行 Mod 更新的文件夹列表移除：{PathMods}")
                UpdatingInstanceModFolders.Remove(PathMods)

                RunInNewThread(
                Sub()
                    Try
                        For Each TempFile In FileCopyList.Keys
                            FileUtils.Delete(TempFile)
                        Next
                    Catch ex As Exception
                        Logger.Warn(ex, "清理 Mod 更新缓存失败")
                    End Try
                End Sub, "Clean Mod Update Cache", ThreadPriority.BelowNormal)
            End Sub

            Logger.Info($"开始更新 {ModList.Count} 个 Mod：{ModList.Select(Function(m) m.File.Name).Join("，")}")
            UpdatingInstanceModFolders.Add(PathMods)
            Loader.Start()
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()
            ReloadModList(True)
        Catch ex As Exception
            Logger.Warn(ex, "初始化 Mod 更新失败")
        End Try
    End Sub

    Private Sub BtnSelectDelete_Click() Handles BtnSelectDelete.Click
        DeleteMods(LocalResourceLoader.Output.Where(Function(m) SelectedMods.Contains(m.EnabledName)))
        ChangeAllSelected(False)
    End Sub
    Private Sub DeleteMods(ModsToDelete As IEnumerable(Of LocalResourceFile))
        Try
            Dim IsSuccessful As Boolean = True
            Dim IsShiftPressed As Boolean = My.Computer.Keyboard.ShiftKeyDown

            Dim ModList = ModsToDelete.SelectMany(
            Function(Target As LocalResourceFile)
                If Target.IsEnabled Then
                    Return {Target.File.FullName, Target.File.FullName & If(FileUtils.Exists(Target.File.FullName & ".old"), ".old", ".disabled")}
                Else
                    Return {Target.File.FullName, Target.EnabledFullName}
                End If
            End Function).Distinct.Where(Function(m) FileUtils.Exists(m)).Select(Function(m) New LocalResourceFile(m)).ToList()

            For Each ModEntity In ModList

                Try
                    FileUtils.Delete(ModEntity.File.FullName, Not IsShiftPressed)
                Catch ex As OperationCanceledException
                    Logger.Warn(ex, "删除 Mod 被主动取消")
                    ReloadModList(True)
                    Return
                Catch ex As Exception
                    Logger.Error(ex, $"删除 Mod 失败（{ModEntity.File.FullName}）", LogBehavior.Alert)
                    IsSuccessful = False
                End Try

                SelectedMods.Remove(ModEntity.EnabledName)

                LocalResourceLoader.Output.Remove(ModEntity)
                SearchResult?.Remove(ModEntity)
                ModItems.Remove(ModEntity.EnabledName)
                Dim IndexOfUi As Integer = PanList.Children.IndexOf(PanList.Children.OfType(Of MyLocalModItem).FirstOrDefault(Function(i) i.Entry.Equals(ModEntity)))
                If IndexOfUi >= 0 Then PanList.Children.RemoveAt(IndexOfUi)
            Next
            RefreshBars()
            If Not IsSuccessful Then
                Hint("由于文件被占用，Mod 删除失败，请尝试关闭正在运行的游戏后再试！", HintType.Red)
                ReloadModList(True)
            ElseIf PanList.Children.Count = 0 Then
                ReloadModList(True)
            Else
                RefreshBars()
            End If

            If Not IsSuccessful Then Return
            If ModList.IsSingle Then
                Hint($"已删除 {ModList.Single.File.Name}！", HintType.Green)
            Else
                Hint($"已删除 {ModList.Count} 个文件！", HintType.Green)
            End If
        Catch ex As OperationCanceledException
            Logger.Warn(ex, "删除 Mod 被主动取消")
            ReloadModList(True)
        Catch ex As Exception
            Logger.Error(ex, "删除 Mod 出现未知错误")
            ReloadModList(True)
        End Try
        LoaderRun(LoaderFolderRunType.UpdateOnly)
    End Sub

    Private Sub BtnSelectCancel_Click() Handles BtnSelectCancel.Click
        ChangeAllSelected(False)
    End Sub

#End Region

#Region "单个 Mod 项"

    Public Sub Info_Click(sender As Object, e As EventArgs)
        Try

            Dim ModEntry As LocalResourceFile = CType(If(TypeOf sender Is MyIconButton, sender.Tag, sender), MyLocalModItem).Entry

            If ModEntry.Project IsNot Nothing Then

                FrmMain.PageChange(New FormMain.PageStackData With {.Page = FormMain.PageType.ResourceDetail,
                    .Additional = {ModEntry.Project, New List(Of String), PageInstanceLeft.Instance.Version.VanillaName, PageInstanceLeft.Instance.Version.ModLoaders, ResourceTypes.Mod}})
            Else

                Dim ContentLines As New List(Of String)
                If ModEntry.Description IsNot Nothing Then ContentLines.Add(ModEntry.Description & vbCrLf)
                ContentLines.Add("文件：" & ModEntry.File.Name & "（" & StringUtils.FormatByteSize(ModEntry.File.Length) & "）")
                If ModEntry.Version IsNot Nothing Then ContentLines.Add("版本：" & ModEntry.Version)
                Dim DebugInfo As New List(Of String)
                If DebugInfo.Any Then
                    ContentLines.Add("")
                    ContentLines.AddRange(DebugInfo)
                End If

                Dim ModOriginalName As String = ModEntry.Display.Replace(" ", "+")
                Dim ModSearchName As String = ModOriginalName.Substring(0, 1)
                For i = 1 To ModOriginalName.Count - 1
                    Dim IsLastLower As Boolean = ModOriginalName(i - 1).ToString.Lower.Equals(ModOriginalName(i - 1).ToString)
                    Dim IsCurrentLower As Boolean = ModOriginalName(i).ToString.Lower.Equals(ModOriginalName(i).ToString)
                    If IsLastLower AndAlso Not IsCurrentLower Then

                        ModSearchName += "+"
                    End If
                    ModSearchName += ModOriginalName(i)
                Next
                ModSearchName = ModSearchName.Replace("++", "+").Replace("pti+Fine", "ptiFine")

                If MyMsgBox(ContentLines.Join(vbCrLf), ModEntry.Display, "百科搜索", "返回") = 1 Then
                    OpenWebsite("https://www.mcmod.cn/s?key=" & ModSearchName & "&site=all&filter=0")
                End If
            End If
        Catch ex As Exception
            Logger.Error(ex, "获取 Mod 详情失败")
        End Try
    End Sub

    Public Sub Open_Click(sender As MyIconButton, e As EventArgs)
        Try
            Dim ListItem As MyLocalModItem = sender.Tag
            OpenExplorer(ListItem.Entry.File.FullName)
        Catch ex As Exception
            Logger.Error(ex, "打开 Mod 文件位置失败")
        End Try
    End Sub

    Public Sub Delete_Click(sender As MyIconButton, e As EventArgs)
        Dim ListItem As MyLocalModItem = sender.Tag
        DeleteMods({ListItem.Entry})
    End Sub

    Public Sub ED_Click(sender As MyIconButton, e As EventArgs)
        Dim ListItem As MyLocalModItem = sender.Tag
        EDMods({ListItem.Entry}, Not ListItem.Entry.IsEnabled)
    End Sub

#End Region

#Region "搜索"

    Public ReadOnly Property IsSearching As Boolean
        Get
            Return Not String.IsNullOrWhiteSpace(SearchBox.Text)
        End Get
    End Property
    Private SearchResult As List(Of LocalResourceFile)
    Public Sub SearchRun() Handles SearchBox.TextChanged
        If IsSearching Then

            Dim QueryList As New List(Of SearchEntry(Of LocalResourceFile))
            For Each Entry As LocalResourceFile In LocalResourceLoader.Output
                Dim SearchSources As New List(Of SearchSource)
                SearchSources.Add(New SearchSource(Entry.Display, 1))
                SearchSources.Add(New SearchSource(Entry.File.Name, 1))
                If Entry.Version IsNot Nothing Then
                    SearchSources.Add(New SearchSource(Entry.Version, 0.2))
                End If
                If Entry.Description IsNot Nothing AndAlso Entry.Description <> "" Then
                    SearchSources.Add(New SearchSource(Entry.Description, 0.4))
                End If
                If Entry.Project IsNot Nothing Then
                    If Entry.Project.RawName <> Entry.Display Then SearchSources.Add(New SearchSource(Entry.Project.RawName, 1))
                    If Entry.Project.TranslatedName <> Entry.Project.RawName Then SearchSources.Add(New SearchSource(Entry.Project.TranslatedName, 1))
                    If Entry.Project.Description <> Entry.Description Then SearchSources.Add(New SearchSource(Entry.Project.Description, 0.4))
                    SearchSources.Add(New SearchSource(String.Join("", Entry.Project.Tags), 0.2))
                End If
                QueryList.Add(New SearchEntry(Of LocalResourceFile) With {.Item = Entry, .SearchSource = SearchSources})
            Next

            SearchResult = Search(QueryList, SearchBox.Text, MaxBlurCount:=6, MinBlurSimilarity:=0.35).Select(Function(r) r.Item).ToList
        End If
        RefreshUI()
    End Sub

#End Region

End Class
