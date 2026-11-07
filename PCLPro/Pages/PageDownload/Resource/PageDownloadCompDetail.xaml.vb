Imports System.Collections.ObjectModel

Public Class PageDownloadResourceDetail
    Private ResourceItem As MyResourceItem = Nothing

    Private TargetResourceType As ResourceTypes
    Private Project As ResourceProject
    Private TargetVersion As String
    Private TargetLoaders As ModLoaders
    Public Sub LoadPageArguments() Handles Me.Loaded
        Project = FrmMain.PageCurrent.Additional(0)
        TargetVersion = FrmMain.PageCurrent.Additional(2)
        TargetLoaders = CType(FrmMain.PageCurrent.Additional(3), ModLoaders)
        If TargetLoaders <> ModLoaders.None AndAlso Not TargetLoaders.Flags.IsSingle Then TargetLoaders = TargetLoaders.Flags.First()
        TargetResourceType = FrmMain.PageCurrent.Additional(4)
    End Sub

#Region "加载器"

    Private ResourceFileLoader As New LoaderTask(Of Integer, List(Of ResourceVersion))(
        "Resource File",
        Sub(Task)
            LoadPageArguments()
            Dim Result = ResourceVersion.FromProjectId(Project.Id, Project.Platform)
            If Task.IsCanceled Then Return
            Task.Output = Result
        End Sub)

    Private Sub PageDownloadResourceDetail_Inited(sender As Object, e As EventArgs) Handles Me.Initialized
        LoadPageArguments()
        PageLoaderInit(Load, PanLoad, PanMain, CardIntro, ResourceFileLoader, AddressOf Load_OnFinish)
    End Sub

    Private Sub Load_State(sender As Object, state As MyLoading.MyLoadingState, oldState As MyLoading.MyLoadingState) Handles Load.StateChanged
        Select Case ResourceFileLoader.State
            Case LoadState.Failed
                Dim ErrorMessage As String = ""
                If ResourceFileLoader.Error IsNot Nothing Then ErrorMessage = ResourceFileLoader.Error.Message
                If ErrorMessage.Contains("不是有效的 json 文件") Then
                    Logger.Warn("下载的文件 json 列表损坏，已自动重试")
                    PageLoaderRestart()
                End If
        End Select
    End Sub

    Private Class CardSorter
        Implements IComparer(Of String)
        Public Topmost As String = ""
        Public Function Compare(x As String, y As String) As Integer Implements IComparer(Of String).Compare

            If x = y Then Return 0

            If x = Topmost Then Return -1
            If y = Topmost Then Return 1

            Dim IsXSpecial As Boolean = Not x.Contains(".")
            Dim IsYSpecial As Boolean = Not y.Contains(".")
            If IsXSpecial AndAlso IsYSpecial Then Return x.CompareTo(y)
            If IsXSpecial Then Return 1
            If IsYSpecial Then Return -1

            Dim VersionCodeSort = -McUtils.CompareVersion(x.Replace(x.BeforeFirst(" ") & " ", ""), y.Replace(y.BeforeFirst(" ") & " ", ""))
            If VersionCodeSort <> 0 Then Return VersionCodeSort

            Return -McUtils.CompareVersion(x, y)
        End Function
        Public Sub New(Optional Topmost As String = "")
            Me.Topmost = If(Topmost, "")
        End Sub
    End Class

    Private VersionFilter As String
    Private GroupedDrop As Boolean
    Private GroupedOld As Boolean

    Private Function GetResults() As List(Of ResourceVersion)
        Dim Results As List(Of ResourceVersion) = ResourceFileLoader.Output
        If TargetResourceType = CType(0, ResourceTypes) Then

            Return Results.Where(Function(r) r.ResourceType <> ResourceTypes.Plugin).ToList
        End If
        Select Case TargetResourceType
            Case ResourceTypes.Any
                Results = Results.Where(Function(r) r.ResourceType <> ResourceTypes.Plugin).ToList
            Case ResourceTypes.Shader, ResourceTypes.ResourcePack

            Case Else
                Results = Results.Where(Function(r) r.ResourceType = TargetResourceType).ToList
        End Select

        If Results.Count = 0 Then
            Logger.Warn($"按类型 {TargetResourceType} 过滤后版本列表为空，已退回展示全部版本")
            Results = ResourceFileLoader.Output.Where(Function(r) r.ResourceType <> ResourceTypes.Plugin).ToList
        End If
        Return Results
    End Function
    Private Sub Load_OnFinish()
        Dim Results = GetResults()

        Dim Filters As List(Of String) = Nothing
        Dim UpdateFilters =
        Sub()
            Filters = Results.SelectMany(Function(v) v.GameVersions).Select(Function(v) GetGroupedVersionName(v, GroupedDrop, GroupedOld)).
                Distinct.OrderByDescending(Function(s) s, Comparer(Of String).Create(Function(x, y) McUtils.CompareVersion(x, y))).ToList
        End Sub

        GroupedDrop = False : GroupedOld = False
        UpdateFilters()
        If Filters.Count < 9 Then GoTo GroupDone
        GroupedDrop = True : GroupedOld = False
        UpdateFilters()
        If Filters.Count < 9 Then GoTo GroupDone
        GroupedDrop = False : GroupedOld = True
        UpdateFilters()
        If Filters.Count < 9 Then GoTo GroupDone
        GroupedDrop = True : GroupedOld = True
        UpdateFilters()
GroupDone:

        PanFilter.Children.Clear()
        If Filters.Count < 2 Then
            CardFilter.Visibility = Visibility.Collapsed
            VersionFilter = Nothing
        Else
            CardFilter.Visibility = Visibility.Visible
            Filters.Insert(0, "全部")

            For Each Version As String In Filters
                Dim NewButton As New MyRadioButton With {
                    .Text = Version, .Margin = New Thickness(2, 0, 2, 0), .ColorType = MyRadioButton.ColorState.Highlight}
                NewButton.LabText.Margin = New Thickness(-2, 0, 10, 0)
                AddHandler NewButton.Check,
                Sub(sender As MyRadioButton, raiseByMouse As Boolean)
                    VersionFilter = If(sender.Text = "全部", Nothing, sender.Text)
                    UpdateFilterResult()
                End Sub
                PanFilter.Children.Add(NewButton)
            Next

            Dim ToCheck As MyRadioButton = Nothing
            If TargetVersion <> "" Then
                Dim TargetFile = Results.FirstOrDefault(Function(v) v.GameVersions.Contains(TargetVersion))
                If TargetFile IsNot Nothing Then
                    Dim TargetGroup = GetGroupedVersionName(TargetVersion, GroupedDrop, GroupedOld)
                    For Each Button As MyRadioButton In PanFilter.Children
                        If Button.Text <> TargetGroup Then Continue For
                        ToCheck = Button
                        Exit For
                    Next
                End If
            End If
            If ToCheck Is Nothing Then ToCheck = PanFilter.Children(0)
            ToCheck.Checked = True
        End If

        UpdateFilterResult()
    End Sub
    Private Sub UpdateFilterResult()
        Dim Results = GetResults()

        Dim TargetCardName As String = If(TargetVersion <> "" OrElse TargetLoaders <> ModLoaders.None,
            $"{If(TargetLoaders <> ModLoaders.None, TargetLoaders.ToString + " ", "")}{TargetVersion}（所选版本）", "")

        Dim Dict As New SortedDictionary(Of String, List(Of ResourceVersion))(New CardSorter(TargetCardName))
        Dict.Add("其他", New List(Of ResourceVersion))
        For Each Version As ResourceVersion In Results
            For Each GameVersion In Version.GameVersions

                If VersionFilter IsNot Nothing AndAlso
                    GetGroupedVersionName(GameVersion, GroupedDrop, GroupedOld) <> VersionFilter Then Continue For

                Dim VerName As String = GetGroupedVersionName(GameVersion, False, False)

                Dim Loaders As New List(Of String)
                If Project.ModLoaders.Flags.Count > 1 AndAlso
                    Version.ResourceType = ResourceTypes.Mod AndAlso
                    McVersion.IsStandardFormat(VerName) Then
                    Loaders.AddRange(Version.ModLoaders.Flags.Select(Function(f) f.ToString & " "))
                End If
                If Not Loaders.Any() Then Loaders.Add("")

                For Each Loader In Loaders
                    Dim TargetCard As String = Loader & VerName
                    If Not Dict.ContainsKey(TargetCard) Then Dict.Add(TargetCard, New List(Of ResourceVersion))
                    If Not Dict(TargetCard).Contains(Version) Then Dict(TargetCard).Add(Version)
                Next
            Next
        Next

        If TargetCardName <> "" AndAlso (VersionFilter Is Nothing OrElse GetGroupedVersionName(TargetVersion, GroupedDrop, GroupedOld).StartsWithF(VersionFilter)) Then
            Dict.Add(TargetCardName, New List(Of ResourceVersion))
            For Each Version As ResourceVersion In Results
                If Version.GameVersions.Contains(TargetVersion) AndAlso
                   (TargetLoaders = ModLoaders.None OrElse TargetLoaders.Flags.Intersect(Version.ModLoaders.Flags).Any) Then

                    If VersionFilter IsNot Nothing AndAlso
                        Not Version.GameVersions.Any(Function(v) GetGroupedVersionName(v, GroupedDrop, GroupedOld) = VersionFilter) Then Continue For
                    If Not Dict(TargetCardName).Contains(Version) Then Dict(TargetCardName).Add(Version)
                End If
            Next
        End If

        Try
            PanResults.Children.Clear()
            For Each Pair As KeyValuePair(Of String, List(Of ResourceVersion)) In Dict
                If Not Pair.Value.Any() Then Continue For
                If Pair.Key = TargetCardName.Replace("（所选版本）", "") Then Continue For

                Dim NewCard As New MyCard With {.Title = Pair.Key, .Margin = New Thickness(0, 0, 0, 15)}
                Dim NewStack As New StackPanel With {.Margin = New Thickness(20, MyCard.SwapedHeight, 18, 0), .VerticalAlignment = VerticalAlignment.Top, .RenderTransform = New TranslateTransform(0, 0), .Tag = Pair.Value}
                NewCard.Children.Add(NewStack)
                NewCard.SwapControl = NewStack
                NewCard.SetContentBuilder(AddressOf InitializeStackResourceVersions)
                PanResults.Children.Add(NewCard)

                If Pair.Key = TargetCardName OrElse
                   (FrmMain.PageCurrent.Additional IsNot Nothing AndAlso
                   CType(FrmMain.PageCurrent.Additional(1), List(Of String)).Contains(NewCard.Title)) Then
                    NewCard.BuildContent()
                Else
                    NewCard.IsSwapped = True
                End If

                If Pair.Key = "其他" Then
                    NewStack.Children.Add(New MyHint With {.Text = "由于版本信息更新缓慢，可能无法识别刚更新的 MC 版本。几天后即可正常识别。", .Theme = MyHint.Themes.Yellow, .Margin = New Thickness(5, 0, 0, 8)})
                End If
            Next

            If PanResults.Children.Count = 1 Then
                CType(PanResults.Children(0), MyCard).IsSwapped = False
            End If

            If Project.Types = ResourceTypes.ModOrDataPack AndAlso (TargetResourceType = ResourceTypes.Mod OrElse TargetResourceType = ResourceTypes.DataPack) Then
                HintAlternative.Visibility = Visibility.Visible
                HintAlternative.Text = If(TargetResourceType = ResourceTypes.Mod,
                    "以下是该项目的 Mod 版本。点击这里查看其数据包版本。", "以下是该项目的数据包版本。点击这里查看其 Mod 版本。")
            Else
                HintAlternative.Visibility = Visibility.Collapsed
            End If
        Catch ex As Exception
            Logger.Error(ex, "可视化工程下载列表出错")
        End Try
    End Sub

    Private Sub InitializeStackResourceVersions(Stack As StackPanel)
        Dim Versions = CType(Stack.Tag, List(Of ResourceVersion)).SortByComparison(Function(a, b) a.ReleaseDate > b.ReleaseDate)
        Dim HasDuplicates = Versions.DistinctBy(Function(a) a.Display).Count <> Versions.Count
        Dim Dependences As List(Of String)

        If TargetResourceType = ResourceTypes.ModPack Then
            Dependences = New List(Of String)
        Else
            Dependences = Versions.SelectMany(Function(f) f.Dependencies).Distinct.Ordered.ToList()
        End If
        If Dependences.Any() Then
            Dependences = Dependences.Where(
            Function(d)
                If Not ResourceProject.Cache.ContainsKey(d) Then Logger.Warn($"未找到 ID {d} 的前置信息")
                Return ResourceProject.Cache.ContainsKey(d)
            End Function).ToList

            Stack.Children.Add(New TextBlock With {.Text = "前置资源", .FontSize = 14, .HorizontalAlignment = HorizontalAlignment.Left, .Margin = New Thickness(6, 2, 0, 5)})

            For Each Dep In Dependences
                Dim Item = ResourceProject.Cache(Dep).ToResourceItem(False, False)
                Stack.Children.Add(Item)
            Next

            Stack.Children.Add(New TextBlock With {.Text = "版本列表", .FontSize = 14, .HorizontalAlignment = HorizontalAlignment.Left, .Margin = New Thickness(6, 12, 0, 5)})
        End If

        For Each Version In Versions
            If Not RestrictMcDownload AndAlso TargetResourceType = ResourceTypes.ModPack Then
                Stack.Children.Add(Version.ToListItem(AddressOf Install_Click, AddressOf Save_Click, IsBadDisplay:=HasDuplicates))
            Else
                Stack.Children.Add(Version.ToListItem(AddressOf Save_Click, IsBadDisplay:=HasDuplicates))
            End If
        Next
        Stack.Children.Add(New FrameworkElement With {.Height = 18})
        Stack.Tag = Nothing
    End Sub
    Private Function GetGroupedVersionName(Name As String, GroupedByDrop As Boolean, FoldOld As Boolean) As String
        Dim Drop = McVersion.VersionToDrop(Name)
        If Name Is Nothing Then
            Return "其他"
        ElseIf Name.Contains("w") OrElse Drop = 209 Then
            Return "快照版"
        ElseIf Not McVersion.IsStandardFormat(Name) OrElse (FoldOld AndAlso Drop < 120) Then
            Return "远古版"
        ElseIf GroupedByDrop Then
            Return McVersion.DropToVersion(Drop)
        Else
            Return Name
        End If
    End Function

#End Region

    Private IsFirstInit As Boolean = True
    Public Sub Init() Handles Me.PageEnter
        AniControlEnabled += 1
        Project = FrmMain.PageCurrent.Additional(0)
        PanBack.ScrollToHome()

        If IsFirstInit Then

            IsFirstInit = False
        Else
            PageLoaderRestart(IsForceRestart:=True)
        End If

        If ResourceItem IsNot Nothing Then PanIntro.Children.Remove(ResourceItem)
        ResourceItem = Project.ToResourceItem(True, True).Init()
        ResourceItem.CanInteraction = False
        ResourceItem.Margin = New Thickness(-7, -7, 0, 8)
        PanIntro.Children.Insert(0, ResourceItem)

        BtnIntroWeb.Text = $"转到 {Project.Platform}"
        BtnIntroWiki.Visibility = If(Project.WikiId = 0, Visibility.Collapsed, Visibility.Visible)

        AniControlEnabled -= 1
    End Sub

    Public Sub Install_Click(sender As MyListItem, e As EventArgs)
        Try

            Dim File As ResourceVersion = sender.Tag
            Dim LoaderName As String = $"{Project.Platform} 整合包下载：{Project.TranslatedName} "

            Dim PackName As String = Project.TranslatedName.Replace(".zip", "").Replace(".rar", "").Replace(".mrpack", "").Replace("\", "＼").Replace("/", "／").Replace("|", "｜").Replace(":", "：").Replace("<", "＜").Replace(">", "＞").Replace("*", "＊").Replace("?", "？").Replace("""", "").Replace("： ", "：")
            Dim Validate As New ValidateFolderName(McFolderSelected & "versions")
            If Validate.Validate(PackName) <> "" Then PackName = ""
            Dim InstanceName As String = MyMsgBoxInput("输入版本名称", "", PackName, New Collection(Of Validate) From {Validate})
            If String.IsNullOrEmpty(InstanceName) Then Return

            Dim Loaders As New List(Of LoaderBase)
            Dim Target As String = $"{McFolderSelected}versions\{InstanceName}\原始整合包.{If(Project.Platform = ResourcePlatforms.CurseForge, "zip", "mrpack")}"
            Dim LogoFileAddress As String = ResourceItem.PathLogo.ActualSource
            Loaders.Add(New LoaderDownload("下载整合包文件", {
                File.ToNetFile(Target, ResourceVersion.DownloadReason.Standalone, File.GameVersions.FirstOrDefault, File.ModLoaders)
            }) With {.ProgressWeight = 10, .Block = True})
            Loaders.Add(New LoaderTask(Of Integer, Integer)("准备安装整合包",
            Sub() ModpackInstall(Target, InstanceName, If(FileUtils.Exists(LogoFileAddress), LogoFileAddress, Nothing))) With {.ProgressWeight = 0.1})

            Dim Loader As New LoaderCombo(Of String)(LoaderName, Loaders) With {.OnStateChanged =
            Sub(MyLoader)
                Select Case MyLoader.State
                    Case LoadState.Failed
                        Hint(MyLoader.Name & "失败：" & MyLoader.Error.GetDisplay(False), HintType.Red)
                    Case LoadState.Canceled
                        Hint(MyLoader.Name & "已取消！", HintType.Blue)
                    Case LoadState.Loading
                        Return
                End Select
                McInstallFailedClearFolder(MyLoader)
            End Sub}
            Loader.Start(McFolderSelected & "versions\" & InstanceName & "\")
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.PageChange(FormMain.PageType.TaskManager)

        Catch ex As Exception
            Logger.Error(ex, "下载资源整合包失败")
        End Try
    End Sub

    Public Shared CachedFolder As New Dictionary(Of ResourceTypes, String)
    Public Sub Save_Click(sender As Object, e As EventArgs)
        Dim File As ResourceVersion = If(TypeOf sender Is MyListItem, sender, sender.Parent).Tag
        RunInNewThread(
        Sub()
            Try
                Dim Desc As String = Nothing
                Select Case File.ResourceType
                    Case ResourceTypes.ModPack : Desc = "整合包"
                    Case ResourceTypes.Mod : Desc = "Mod "
                    Case ResourceTypes.ResourcePack : Desc = "资源包"
                    Case ResourceTypes.Shader : Desc = "光影包"
                    Case ResourceTypes.DataPack : Desc = "数据包"
                    Case ResourceTypes.Map : Desc = "地图"
                End Select

                Dim DefaultFolder As String = Nothing
                If File.ResourceType <> ResourceTypes.ModPack Then
                    Dim SubFolder As String = Nothing
                    Select Case File.ResourceType
                        Case ResourceTypes.Mod : SubFolder = "mods\"
                        Case ResourceTypes.ResourcePack : SubFolder = "resourcepacks\"
                        Case ResourceTypes.Shader : SubFolder = "shaderpacks\"
                        Case ResourceTypes.DataPack : SubFolder = ""
                        Case ResourceTypes.Map : SubFolder = ""
                    End Select
                    Dim IsInstanceSuitable As Func(Of McInstance, Boolean) = Nothing

                    Dim AllowedLoaders As ModLoaders = ModLoaders.None
                    If File.ModLoaders <> ModLoaders.None Then
                        AllowedLoaders = File.ModLoaders
                    ElseIf Project.ModLoaders <> ModLoaders.None Then
                        AllowedLoaders = Project.ModLoaders
                    End If
                    Logger.Info($"{Desc}要求的加载器种类：{AllowedLoaders}")

                    IsInstanceSuitable =
                    Function(Instance As McInstance)
                        If Not Instance.IsLoaded Then Instance.Load()

                        If File.ResourceType = ResourceTypes.Mod OrElse File.ResourceType = ResourceTypes.DataPack Then
                            If Not File.GameVersions.Any(
                            Function(FileVersion)
                                Dim FileIsSnapshot As Boolean = FileVersion.Contains("预览版") OrElse Not FileVersion.Contains(".")
                                Dim InstanceIsSnapshot As Boolean = Instance.Version.VanillaName.Contains("snapshot") OrElse Not Instance.Version.VanillaName.Contains(".")
                                If FileIsSnapshot <> InstanceIsSnapshot Then

                                    Return False
                                ElseIf Not FileIsSnapshot AndAlso Not InstanceIsSnapshot Then

                                    Return FileVersion = Instance.Version.VanillaName
                                Else

                                    Dim FileIsNewFormat As Boolean = FileVersion.Contains(".") AndAlso Val(FileVersion.BeforeFirst(".")) > 1
                                    Dim InstanceIsNewFormat As Boolean = Instance.Version.Drop > 250
                                    If FileIsNewFormat <> InstanceIsNewFormat Then

                                        Return False
                                    ElseIf FileIsNewFormat AndAlso InstanceIsNewFormat Then

                                        Return FileVersion.BeforeFirst(" ") = Instance.Version.VanillaName.BeforeFirst("-")
                                    Else

                                        If FileVersion.Contains("w") AndAlso Instance.Version.VanillaName.Contains("w") Then

                                            Return FileVersion = Instance.Version.VanillaName
                                        Else

                                            Return True
                                        End If
                                    End If
                                End If
                            End Function) Then Return False
                        End If

                        Return AllowedLoaders = ModLoaders.None OrElse AllowedLoaders.Flags.Intersect(Instance.Version.ModLoaders.Flags).Any
                    End Function

                    If CachedFolder.ContainsKey(File.ResourceType) Then
                        DefaultFolder = CachedFolder(File.ResourceType)
                        Logger.Info($"使用上次下载时的文件夹作为默认下载位置：{DefaultFolder}")
                    ElseIf McInstanceSelected IsNot Nothing AndAlso IsInstanceSuitable(McInstanceSelected) Then
                        DefaultFolder = $"{McInstanceSelected.PathIndie}{SubFolder}"
                        DirectoryUtils.Create(DefaultFolder)
                        Logger.Info($"使用当前版本作为默认下载位置：{DefaultFolder}")
                    Else

                        Dim NeedLoad As Boolean = McInstanceListLoader.State <> LoadState.Finished
                        If NeedLoad Then
                            Hint("正在查找适合的游戏版本……")
                            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\", WaitForExit:=True)
                        End If
                        Dim SuitableInstanceList = McInstanceList.Values.SelectMany(Function(l) l).Where(Function(v) IsInstanceSuitable(v)).
                            Select(Function(v) DirectoryUtils.GetInfo($"{v.PathIndie}{SubFolder}"))
                        If SuitableInstanceList.Any Then
                            Dim SelectedInstance = SuitableInstanceList.
                                OrderByDescending(Function(Dir) If(Dir.Exists, Dir.LastWriteTimeUtc, Date.MinValue)).
                                ThenByDescending(Function(Dir) If(Dir.Exists, Dir.GetFiles().Length, -1)).
                                First()
                            DefaultFolder = PathUtils.RemoveExtendedPrefix(SelectedInstance.FullName)
                            DirectoryUtils.Create(DefaultFolder)
                            Logger.Info($"使用适合的游戏版本作为默认下载位置：{DefaultFolder}")
                        Else
                            DefaultFolder = McFolderSelected
                            If NeedLoad Then
                                Hint($"当前 MC 文件夹中没有找到合适的版本！")
                            Else
                                Logger.Info("由于当前版本不兼容，使用当前的 MC 文件夹作为默认下载位置")
                            End If
                        End If
                    End If
                End If

                Dim FileName As String
                If Project.TranslatedName = Project.RawName Then
                    FileName = File.FileName
                Else
                    Dim ChineseName As String = Project.TranslatedName.BeforeFirst(" (").BeforeFirst(" - ").
                        Replace("\", "＼").Replace("/", "／").Replace("|", "｜").Replace(":", "：").Replace("<", "＜").Replace(">", "＞").Replace("*", "＊").Replace("?", "？").Replace("""", "").Replace("： ", "：")
                    Select Case Settings.Get(Of Integer)("ToolDownloadTranslateV2")
                        Case 0
                            FileName = $"【{ChineseName}】{File.FileName}"
                        Case 1
                            FileName = $"[{ChineseName}] {File.FileName}"
                        Case 2
                            FileName = $"{ChineseName}-{File.FileName}"
                        Case 3
                            FileName = $"{File.FileName}-{ChineseName}"
                        Case Else
                            FileName = File.FileName
                    End Select
                End If
                If File.ResourceType = ResourceTypes.Mod Then FileName = FileName.Replace("~", "-")

                Dim DepCandidates As New List(Of Tuple(Of ResourceProject, ResourceVersion))
                If File.ResourceType = ResourceTypes.Mod Then
                    Try
                        DepCandidates = GetSuitableDependencyFiles(File)
                        If DepCandidates.Any Then Logger.Info($"检测到 {DepCandidates.Count} 个前置候选：{DepCandidates.Select(Function(d) d.Item1.TranslatedName).Join("、")}")
                    Catch ex As Exception
                        Logger.Warn(ex, "获取 Mod 前置列表失败")
                    End Try
                End If
                RunInUi(
                Sub()

                    Dim DepFiles As New List(Of Tuple(Of ResourceProject, ResourceVersion))
                    If File.ResourceType = ResourceTypes.Mod AndAlso DepCandidates.Any Then

                        Dim InstalledDeps As New List(Of String)
                        For Each Dep In DepCandidates
                            If IsDependencyInstalled(DefaultFolder, Dep.Item1) Then
                                InstalledDeps.Add(Dep.Item1.TranslatedName)
                            Else
                                DepFiles.Add(Dep)
                            End If
                        Next
                        If InstalledDeps.Any Then Logger.Info($"已安装的前置，跳过下载：{InstalledDeps.Join("、")}")
                        If DepFiles.Any Then
                            Dim DepNames As String = DepFiles.Select(Function(d) d.Item1.TranslatedName).Join("、")
                            Dim InstalledHint As String = If(InstalledDeps.Any, vbCrLf & $"（检测到 {InstalledDeps.Count} 个前置已安装，将自动跳过：{InstalledDeps.Join("、")}）", "")
                            If MyMsgBox($"检测到该 Mod 需要 {DepFiles.Count} 个前置：{DepNames}。{InstalledHint}" & vbCrLf & vbCrLf &
                                        "要一并下载这些前置吗？下载时会自动跳过保存位置中已存在的前置。", "自动补全前置", "一并下载", "不用了") = 1 Then
                                Logger.Info($"用户确认一并下载前置：{DepNames}")
                            Else
                                DepFiles.Clear()
                            End If
                        ElseIf InstalledDeps.Any Then
                            Hint($"该 Mod 需要的 {InstalledDeps.Count} 个前置均已安装，无需重复下载", HintType.Green)
                        End If
                    End If

                    Dim Target As String
                    Dim Ext As String = If(File.ResourceType = ResourceTypes.Mod,
                        If(PathUtils.GetExtension(File.FileName) = "litemod", "litemod", "jar"),
                        If(PathUtils.GetExtension(File.FileName) = "mrpack", "mrpack", "zip"))
                    Target = Dialogs.SaveFile("选择保存位置", FileName, DefaultFolder, {(Ext, Desc & "文件")})
                    If Target Is Nothing Then Return
                    Dim DepFolder As String = PathUtils.RemoveLastPart(Target)

                    Dim LoaderName As String = Desc & "下载：" & PathUtils.GetFileNameWithoutExtension(Target) & " "
                    If Not PathUtils.IsSame(PathUtils.RemoveLastPart(Target), DefaultFolder) Then CachedFolder(File.ResourceType) = PathUtils.RemoveLastPart(Target)
                    Dim Loaders As New List(Of LoaderBase)
                    Dim DownloadList As New List(Of NetFile) From {
                        File.ToNetFile(Target, If(TargetResourceType = ResourceTypes.Any, ResourceVersion.DownloadReason.Dependency, ResourceVersion.DownloadReason.Standalone))
                    }
                    For Each Dep In DepFiles
                        Dim DepTarget As String = DepFolder & "\" & Dep.Item2.FileName
                        If FileUtils.Exists(DepTarget) Then
                            Logger.Info($"前置已存在，跳过下载：{Dep.Item2.FileName}")
                        Else
                            DownloadList.Add(Dep.Item2.ToNetFile(DepTarget, ResourceVersion.DownloadReason.Dependency))
                        End If
                    Next
                    Loaders.Add(New LoaderDownload("下载文件", DownloadList) With {.ProgressWeight = 6, .Block = True})

                    Dim Loader As New LoaderCombo(Of Integer)(LoaderName, Loaders) With {.OnStateChanged =
                    Sub(MyLoader)
                        LoaderStateChangedHintOnly(MyLoader)
                        If MyLoader.State = LoadState.Finished AndAlso File.ResourceType = ResourceTypes.ModPack Then OpenExplorer(Target)
                    End Sub}
                    Loader.Start(1)
                    LoaderTaskbarAdd(Loader)
                    FrmMain.BtnExtraDownload.ShowRefresh()
                    FrmMain.BtnExtraDownload.Ribble()
                End Sub)
            Catch ex As Exception
                Logger.Error(ex, "保存资源文件失败")
            End Try
        End Sub, "Download Resource Detail Save")
    End Sub

    Private Function GetSuitableDependencyFiles(File As ResourceVersion) As List(Of Tuple(Of ResourceProject, ResourceVersion))
        Dim Result As New List(Of Tuple(Of ResourceProject, ResourceVersion))
        If File.ResourceType <> ResourceTypes.Mod OrElse Not File.Dependencies.Any Then Return Result
        For Each DepId In File.Dependencies
            Dim DepProject As ResourceProject = Nothing
            If ResourceProject.Cache.ContainsKey(DepId) Then
                DepProject = ResourceProject.Cache(DepId)
            Else
                Try
                    DepProject = ResourceProject.FromProjectId(DepId, Project.Platform)
                Catch ex As Exception
                    Logger.Warn($"获取前置项目 {DepId} 的信息失败：{ex.Message}")
                    Continue For
                End Try
            End If
            Try
                Dim DepVersions = ResourceVersion.FromProjectId(DepId, Project.Platform)
                Dim DepFile = DepVersions.Where(Function(v) v.DownloadAvailable AndAlso
                    v.GameVersions.Any(Function(g) File.GameVersions.Contains(g)) AndAlso
                    (v.ModLoaders = ModLoaders.None OrElse File.ModLoaders = ModLoaders.None OrElse v.ModLoaders.Flags.Intersect(File.ModLoaders.Flags).Any)).
                    OrderByDescending(Function(v) v.ReleaseDate).FirstOrDefault()
                If DepFile Is Nothing Then

                    DepFile = DepVersions.Where(Function(v) v.DownloadAvailable AndAlso
                        (Not v.GameVersions.Any() OrElse v.GameVersions.Any(Function(g) g.Contains("未知"))) AndAlso
                        (v.ModLoaders = ModLoaders.None OrElse File.ModLoaders = ModLoaders.None OrElse v.ModLoaders.Flags.Intersect(File.ModLoaders.Flags).Any)).
                        OrderByDescending(Function(v) v.ReleaseDate).FirstOrDefault()
                End If
                If DepFile IsNot Nothing Then
                    Result.Add(New Tuple(Of ResourceProject, ResourceVersion)(DepProject, DepFile))
                Else
                    Logger.Warn($"前置 {DepProject.TranslatedName} 没有与当前 Mod 版本匹配的可用版本")
                End If
            Catch ex As Exception
                Logger.Warn($"获取前置 {DepProject.TranslatedName} 的版本列表失败：{ex.Message}")
            End Try
        Next
        Return Result
    End Function

    Private Function IsDependencyInstalled(Folder As String, DepProject As ResourceProject) As Boolean
        Try
            If Not Directory.Exists(Folder) Then Return False
            Dim Normalize As Func(Of String, String) =
                Function(Raw As String) As String
                    If Raw Is Nothing Then Return ""
                    Dim Builder As New Text.StringBuilder
                    For Each C As Char In Raw.ToLower
                        If Char.IsLetterOrDigit(C) Then Builder.Append(C)
                    Next
                    Return Builder.ToString()
                End Function
            Dim RawName As String = Normalize(DepProject.RawName)
            Dim Translated As String = Normalize(DepProject.TranslatedName)
            For Each DepFile As String In Directory.GetFiles(Folder, "*.*", SearchOption.AllDirectories)
                Dim Ext As String = PathUtils.GetExtension(DepFile).ToLower
                If Ext <> "jar" AndAlso Ext <> "litemod" Then Continue For
                Dim NameText As String = Normalize(PathUtils.GetFileNameWithoutExtension(DepFile))
                If NameText.Length < 4 Then Continue For
                If RawName.Length >= 3 AndAlso NameText.Contains(RawName) Then Return True
                If Translated.Length >= 2 AndAlso NameText.Contains(Translated) Then Return True
            Next
            Return False
        Catch ex As Exception
            Logger.Warn(ex, "判断前置 Mod 是否已安装失败")
            Return False
        End Try
    End Function

    Private Sub BtnIntroWeb_Click(sender As Object, e As EventArgs) Handles BtnIntroWeb.Click
        OpenWebsite(Project.Website)
    End Sub
    Private Sub BtnIntroWiki_Click(sender As Object, e As EventArgs) Handles BtnIntroWiki.Click
        OpenWebsite("https://www.mcmod.cn/class/" & Project.WikiId & ".html")
    End Sub
    Private Sub BtnIntroCopy_Click(sender As Object, e As EventArgs) Handles BtnIntroCopy.Click
        ClipboardSet(ResourceItem.LabTitle.Text & ResourceItem.LabTitleRaw.Text)
    End Sub

    Private Sub HintAlternative_Click() Handles HintAlternative.Click
        TargetResourceType = If(TargetResourceType = ResourceTypes.Mod, ResourceTypes.DataPack, ResourceTypes.Mod)
        FrmMain.PageCurrent.Additional(4) = TargetResourceType
        PageLoaderRestart(IsForceRestart:=True)
    End Sub

End Class
