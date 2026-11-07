Public Module ModJava

    Public Sub JavaInit()
        Try

            If Configs.JavaConfigVersion.Get() = 0 Then
                Try

                    Logger.Warn("正在从老版本迁移 Java 列表")
                    Dim OldJavaList = Settings.Get(Of String)("LaunchArgumentJavaAll").DeserializeJson(Of JArray).
                                      Select(Function(JavaObject) New Java(JavaObject("Path").ToString)).ToList()
                    OldJavaList = JavaUtils.SortAsync(OldJavaList).Run()
                    Logger.Info($"已从老版本设置中发现 {OldJavaList.Count} 个 Java")

                    Dim SelectedJavaSetting = Settings.Get(Of String)("LaunchArgumentJavaSelect")
                    Dim SelectedJava = If(SelectedJavaSetting.StartsWithF("{"), SelectedJavaSetting.DeserializeJson(Of JObject)()("Path").ToString, "")
                    Dim Match = OldJavaList.FirstOrDefault(Function(j) PathUtils.IsSame(j.Folder, SelectedJava))
                    If Match IsNot Nothing Then
                        OldJavaList.Remove(Match)
                        OldJavaList.Insert(0, Match)
                        Logger.Info($"已将老版本设置中强制选择的 Java 置顶：{Match}")
                    End If
                    Configs.JavaList.Set(New ConcurrentList(Of Java)(OldJavaList))

                    Configs.JavaConfigVersion.Set(Versions.JavaConfigVersion)
                Catch ex As Exception
                    Logger.Error(ex, "从老版本 PCL 迁移 Java 列表失败")
                End Try
            End If

            Dim List = Configs.JavaList.Get()
            If List.Any Then
                Logger.Info($"共有 {List.Count} 个可用的 Java：{vbCrLf}- {List.Select(Function(j) j.ToString).Join(vbCrLf + "- ")}")
                Return
            End If

            Logger.Warn("初始化未找到可用的 Java，将自动触发搜索")
            JavaListRefreshWorker.Start()
        Catch ex As Exception
            Logger.Error(ex, "初始化 Java 列表失败")
            Configs.JavaList.Reset()
        End Try
    End Sub

    Public WithEvents JavaListRefreshWorker As New RedoableWorker(Sub(c, p) JavaUtils.RefreshListAsync(c, p).Run())

#Region "UI 事件"

    Public Sub UpdateJavaLists() Handles JavaListRefreshWorker.Started, JavaListRefreshWorker.Stopped
        RunInUi(
        Sub()
            FrmSetupLaunch?.UpdateJavaList()
            FrmInstanceSetup?.UpdateJavaList()
        End Sub)
    End Sub

    Public Sub ManuallyImportJava()

        Dim NewJavaPath As String = Dialogs.SelectFile("选择 java.exe（可能在 bin 文件夹中）", False, filter:="java.exe|java.exe").FirstOrDefault()
        If String.IsNullOrEmpty(NewJavaPath) Then Return

        Dim NewJava As New Java(PathUtils.RemoveLastPart(NewJavaPath))
        If Not NewJava.CheckAsync().Run() Then
            Hint("该 Java 存在异常，无法使用", HintType.Red)
            Return
        End If

        Configs.JavaRemovedList.Edit(Sub(ByRef List) List.RemoveIf(Function(f) PathUtils.IsSame(f, NewJava.Folder)))

        Configs.JavaList.Edit(
        Sub(ByRef List)
            List.Remove(NewJava)
            List.Insert(0, NewJava)
        End Sub)
        Hint($"已将 Java 加入列表：{NewJavaPath}", HintType.Green)
        UpdateJavaLists()
    End Sub

    Public Sub ManuallyRemoveJava(JavaEntry As Java)
        Configs.JavaList.Edit(Sub(ByRef List) List.Remove(JavaEntry))
        Configs.JavaRemovedList.Edit(Function(List) List.Append(JavaEntry.Folder).DistinctBy(Function(s) s.Lower()).ToList())
        Hint($"已将 Java 移除出列表：{JavaEntry}", HintType.Green)
        UpdateJavaLists()
        PageInstanceLeft.ReloadCurrentJava()
    End Sub

    Public Sub ManuallySearchJava(Optional CancellationToken As CancellationToken = Nothing)
        RunInThread(
        Sub()
            Configs.JavaList.Get().ForAll(Sub(java) java.InvalidateChecked())
            JavaListRefreshWorker.Start(cancellationToken:=CancellationToken)
            JavaListRefreshWorker.WaitIfRunningAsync(cancellationToken:=CancellationToken).Run()
            If Configs.JavaList.Get().Any Then
                Hint("已找到 " & Configs.JavaList.Get().Count & " 个 Java！", HintType.Green)
            Else
                Hint("未找到可用的 Java！", HintType.Red)
            End If
        End Sub)
    End Sub

#End Region

#Region "选取"

    Public Function GetJavaRequirement(Instance As McInstance) As (Range As ValueRange(Of Version), RecommendedComponent As String)

        If Settings.Get(Of Integer)("VersionArgumentJavaV2", Instance) = 1 Then
            Try
                Dim Range = ValueRange(Of Version).FromString(Settings.Get(Of String)("VersionArgumentJavaRange", Instance), Function(s) StringUtils.ParseVersionWithDefaults(s))
                Return (Range, Nothing)
            Catch ex As Exception
                Logger.Warn($"版本设置中指定的 Java 版本范围有误（{Settings.Get(Of String)("VersionArgumentJavaRange", Instance)}），这可能是因为首次修改此设置，尚未初始化")
            End Try
        End If

        Dim JavaRange = ValueRange(Of Version).All()
        Dim AddConstraint =
        Sub(Constraint As ValueRange(Of Version))
            JavaRange = JavaRange?.Intersect(Constraint)
            If JavaRange.IsEmpty Then
                Logger.Warn($"Java 版本要求冲突，若启动失败请在版本设置手动指定 Java：当前追加的要求为 {Constraint}，游戏版本为 {Instance.VersionDisplayName}", LogBehavior.Toast)
                JavaRange = Constraint
            End If
        End Sub

        If (Not Instance.Version.Valid AndAlso Instance.ReleaseTime >= New Date(2024, 4, 2)) OrElse
           (Instance.Version.Valid AndAlso Instance.Version.Vanilla >= New Version(20, 0, 5)) Then

            AddConstraint(ValueRange(Of Version).AtLeast(New Version(21, 0)))
        ElseIf (Not Instance.Version.Valid AndAlso Instance.ReleaseTime >= New Date(2021, 11, 16)) OrElse
               (Instance.Version.Valid AndAlso Instance.Version.Vanilla.Major >= 18) Then

            AddConstraint(ValueRange(Of Version).AtLeast(New Version(17, 0)))
        ElseIf (Not Instance.Version.Valid AndAlso Instance.ReleaseTime >= New Date(2021, 5, 11)) OrElse
               (Instance.Version.Valid AndAlso Instance.Version.Vanilla.Major >= 17) Then

            AddConstraint(ValueRange(Of Version).AtLeast(New Version(16, 0)))
        ElseIf Instance.ReleaseTime.Year >= 2017 Then

            AddConstraint(ValueRange(Of Version).AtLeast(New Version(8, 0)))
        ElseIf Instance.ReleaseTime <= New Date(2013, 5, 1) AndAlso Instance.ReleaseTime.Year >= 2001 Then

            AddConstraint(ValueRange(Of Version).LessThan(New Version(9, 0)))
        End If

        Dim RecommendedComponent As String = Nothing
        Dim RecommendedCode As Integer =
            If(Instance.JsonObject?("javaVersion")?("majorVersion")?.ToObject(Of Integer),
            If(Instance.JsonVersion?("java_version")?.ToObject(Of Integer), 0))
        If RecommendedCode >= 22 Then
            Logger.Info("Mojang 要求至少使用 Java " & RecommendedCode)
            AddConstraint(ValueRange(Of Version).AtLeast(New Version(RecommendedCode, 0)))
            RecommendedComponent =
                If(Instance.JsonObject?("javaVersion")?("component")?.ToString,
                   Instance.JsonVersion?("java_component")?.ToString)
            If RecommendedComponent = "" Then RecommendedComponent = Nothing
        End If

        If Instance.Version.HasOptiFine AndAlso Instance.Version.Valid Then
            If Instance.Version.Vanilla.Major < 7 Then

                AddConstraint(ValueRange(Of Version).LessThan(New Version(9, 0)))
            ElseIf Instance.Version.Vanilla.Major >= 8 AndAlso Instance.Version.Vanilla.Major < 12 Then

                AddConstraint(ValueRange(Of Version).ClosedOpen(New Version(8, 0), New Version(9, 0)))
            ElseIf Instance.Version.Vanilla.Major = 12 Then

                AddConstraint(ValueRange(Of Version).LessThan(New Version(9, 0)))
            End If
        End If

        If Instance.Version.ModLoaders.HasFlagF(ModLoaders.Forge) Then
            If Instance.Version.Vanilla >= New Version(6, 0, 1) AndAlso Instance.Version.Vanilla <= New Version(7, 0, 2) Then

                AddConstraint(ValueRange(Of Version).ClosedOpen(New Version(7, 0), New Version(8, 0)))
            ElseIf Instance.Version.Vanilla.Major <= 12 OrElse Not Instance.Version.Valid Then

                AddConstraint(ValueRange(Of Version).LessThan(New Version(9, 0)))
            ElseIf Instance.Version.Vanilla.Major <= 14 Then

                AddConstraint(ValueRange(Of Version).ClosedOpen(New Version(8, 0), New Version(11, 0)))
            ElseIf Instance.Version.Vanilla.Major = 15 Then

                AddConstraint(ValueRange(Of Version).ClosedOpen(New Version(8, 0), New Version(16, 0)))
            ElseIf McUtils.CompareVersionGE(Instance.Version.ModLoaderVersions(ModLoaders.Forge), "34.0.0") AndAlso McUtils.CompareVersionGE("36.2.25", Instance.Version.ModLoaderVersions(ModLoaders.Forge)) Then

                AddConstraint(ValueRange(Of Version).AtMost(New Version(8, 0, 320)))
            ElseIf McUtils.CompareVersionGE(Instance.Version.ModLoaderVersions(ModLoaders.Forge), "36.2.26") AndAlso McUtils.CompareVersionGE("36.999999.999999", Instance.Version.ModLoaderVersions(ModLoaders.Forge)) Then

                AddConstraint(ValueRange(Of Version).LessThan(New Version(24, 0)))
            ElseIf McUtils.CompareVersionGE(Instance.Version.ModLoaderVersions(ModLoaders.Forge), "37.0.0") AndAlso McUtils.CompareVersionGE("37.0.79", Instance.Version.ModLoaderVersions(ModLoaders.Forge)) Then

                AddConstraint(ValueRange(Of Version).LessThan(New Version(17, 0)))
            ElseIf Instance.Version.Vanilla.Major = 18 AndAlso Instance.Version.HasOptiFine Then

                AddConstraint(ValueRange(Of Version).LessThan(New Version(19, 0)))
            ElseIf McUtils.CompareVersionGE(Instance.Version.ModLoaderVersions(ModLoaders.Forge), "45.0.21") AndAlso McUtils.CompareVersionGE("45.0.65", Instance.Version.ModLoaderVersions(ModLoaders.Forge)) Then

                AddConstraint(ValueRange(Of Version).LessThan(New Version(20, 0)))
            ElseIf McUtils.CompareVersionGE(Instance.Version.ModLoaderVersions(ModLoaders.Forge), "45.0.66") AndAlso McUtils.CompareVersionGE("47.4.8", Instance.Version.ModLoaderVersions(ModLoaders.Forge)) Then

                AddConstraint(ValueRange(Of Version).LessThan(New Version(22, 0)))
            End If
        End If

        If Instance.Version.ModLoaders.HasFlagF(ModLoaders.NeoForge) Then
            If Instance.Version.Vanilla = New Version(20, 0, 1) OrElse
               (McUtils.CompareVersionGE("20.2.62-beta", Instance.Version.ModLoaderVersions(ModLoaders.NeoForge)) AndAlso Not Instance.Version.ModLoaderVersions(ModLoaders.NeoForge).Contains("25w14craftmine")) Then

                AddConstraint(ValueRange(Of Version).LessThan(New Version(22, 0)))
            End If
        End If

        If Instance.Version.ModLoaders.HasFlagF(ModLoaders.Fabric) AndAlso Instance.Version.Valid Then
            If Instance.Version.Vanilla.Major >= 15 AndAlso Instance.Version.Vanilla.Major <= 16 Then

                AddConstraint(ValueRange(Of Version).AtLeast(New Version(8, 0)))
            ElseIf Instance.Version.Vanilla.Major >= 18 Then

                AddConstraint(ValueRange(Of Version).AtLeast(New Version(17, 0)))
            End If
            If Instance.Version.ModLoaderVersions(ModLoaders.Fabric) <> "未知版本" AndAlso McUtils.CompareVersion(Instance.Version.ModLoaderVersions(ModLoaders.Fabric), "0.17.0") < 0 Then

                AddConstraint(ValueRange(Of Version).LessThan(New Version(25, 0)))
            End If
        End If

        If Instance.Version.ModLoaders.HasFlagF(ModLoaders.LiteLoader) AndAlso Instance.Version.Valid Then

            AddConstraint(ValueRange(Of Version).LessThan(New Version(9, 0)))
        End If

        If Settings.Get(Of McLoginType)("LoginType") = McLoginType.Nide Then

            AddConstraint(ValueRange(Of Version).AtLeast(New Version(8, 0, 141)))
        End If

        Return (JavaRange, RecommendedComponent)
    End Function

    Public Function SelectOrDownloadJava(Target As OneOf(Of ValueRange(Of Version), McInstance), TryFixOrHint As Boolean, Optional c As CancellationToken = Nothing, Optional p As ProgressProvider = Nothing) As Java
        Try
            If Target.Is(Of McInstance) Then MigrateInstanceJavaSettings(Target.As(Of McInstance), c, p?.SplitTo(0.2))
            Dim Instance As McInstance = If(Target.Is(Of McInstance), Target.As(Of McInstance), Nothing)
            Select Case If(Instance Is Nothing, 0, Settings.Get(Of Integer)("VersionArgumentJavaV2", Instance))

                Case 2

                    Logger.Info($"版本设置要求使用版本文件夹中的 Java：{Instance.PathVersion}")
                    Dim JavaFound = JavaUtils.SearchFoldersAsync(True, {Instance.PathVersion}, c, p?.SplitTo(1)).Run().FirstOrDefault()
                    If JavaFound IsNot Nothing Then
                        Logger.Info($"已发现版本文件夹中的 Java：{JavaFound}")
                        Return JavaFound
                    End If

                    If TryFixOrHint Then
                        SwitchToInstanceSetup(Instance)
                        MyMsgBox($"该版本的版本设置选择了 {vbLQ}使用版本文件夹中的 Java{vbRQ}，但版本文件夹里没能找到 Java。{vbCrLf}请先修改启动选项中的 Java 设置，或在版本文件夹中放一个 Java，然后再试。",
                            "未找到 Java", IsWarn:=True, ForceWait:=True)
                    End If
                    Return Nothing

                Case 3

                    Dim ChosenJava = Configs.JavaForced.Get(Instance.Config)
                    Logger.Info($"版本设置中强制指定的 Java：{Instance.PathVersion} → {ChosenJava}")
                    If ChosenJava Is Nothing Then
                        If TryFixOrHint Then
                            SwitchToInstanceSetup(Instance)
                            MyMsgBox($"该版本的版本设置选择了 {vbLQ}使用指定的 Java{vbRQ}，但还没有指定任何 Java。{vbCrLf}请先选择你想使用的 Java，然后再试。",
                                "未选择 Java", IsWarn:=True, ForceWait:=True)
                        End If
                        p?.Skip() : Return Nothing
                    End If
                    If Not Configs.JavaList.Get().Contains(ChosenJava) Then
                        Configs.JavaForced.Reset(Instance.Config)
                        UpdateJavaLists()
                        If TryFixOrHint Then
                            SwitchToInstanceSetup(Instance)
                            Logger.Error("版本设置中指定的 Java 不在 Java 列表中，请重新选择一个 Java！", LogBehavior.Alert)
                        End If
                        p?.Skip() : Return Nothing
                    End If
                    If Not ChosenJava.CheckAsync(c).Run() Then
                        Configs.JavaForced.Reset(Instance.Config)
                        UpdateJavaLists()
                        If TryFixOrHint Then
                            SwitchToInstanceSetup(Instance)
                            Logger.Error("版本设置中指定的 Java 已无法使用，请重新选择一个 Java，然后再试。", LogBehavior.Alert)
                        Else
                            Logger.Error("版本设置中指定的 Java 已无法使用，请重新选择一个 Java！", LogBehavior.Toast)
                        End If
                        p?.Finish() : Return Nothing
                    End If
                    p?.Finish() : Return ChosenJava

                Case Else

                    Dim Range As ValueRange(Of Version) =
                        If(Target.Is(Of McInstance), GetJavaRequirement(Instance).Range, Target.As(Of ValueRange(Of Version)))
                    Dim RecommendedComponent As String =
                        If(Target.Is(Of McInstance), GetJavaRequirement(Instance).RecommendedComponent, Nothing)
                    Logger.Info($"自动选择以下范围的 Java：{Range} {RecommendedComponent}")
                    If Range.IsEmpty Then Throw New Exception("Java 版本需求存在冲突，导致没有任何可能适配的 Java，请查看 PCL 的日志了解详细信息！")

                    Dim SelectJavaFromJavaList =
                    Function() As Java
                        JavaListRefreshWorker.WaitIfRunning()
                        For Each ChosenJava In Configs.JavaList.Get()
                            If Not ChosenJava.CheckAsync(c).Run() Then
                                Logger.Warn($"Java 检查失败，将尝试列表中的下一个 Java：{ChosenJava}")
                                Continue For
                            End If
                            If ChosenJava.Version Is Nothing OrElse Not Range.Contains(ChosenJava.Version) Then Continue For
                            Logger.Info($"已选定 Java：{ChosenJava}")
                            Return ChosenJava
                        Next
                        Return Nothing
                    End Function
                    p?.Set(0.25)
                    Dim Result = SelectJavaFromJavaList()
                    If Result IsNot Nothing Then p?.Skip() : Return Result

                    SyncLock JavaLock

                        Logger.Warn($"既有列表中没有合适的 Java，重新触发搜索")
                        JavaListRefreshWorker.Start(c, p?.SplitTo(0.3))
                        Result = SelectJavaFromJavaList()
                        If Result IsNot Nothing Then p?.Skip() : Return Result
                        If Not TryFixOrHint Then Return Nothing

                        Logger.Warn($"电脑中没有合适的 Java，开始获取 Mojang 提供的 Java 下载列表")
                        Static DownloadListWorker As New RedoableWorker(Of List(Of JavaDownloadInfo))(
                        Function(cancellationToken)

                            Dim IndexFileStr As String = NetRequestByLoader(DlVersionListOrder(
                                {"https://piston-meta.mojang.com/v1/products/java-runtime/2ec0cc96c44e5a76b9c8b7c39df7210883d12871/all.json"},
                                {"https://bmclapi2.bangbang93.com/v1/products/java-runtime/2ec0cc96c44e5a76b9c8b7c39df7210883d12871/all.json"}
                            ), IsJson:=True)
                            cancellationToken?.ThrowIfCancellationRequested()

                            Dim Results = New List(Of JavaDownloadInfo)
                            For Each Prop As JProperty In IndexFileStr.DeserializeJson(Of JObject)()("windows-x64")
                                cancellationToken?.ThrowIfCancellationRequested()
                                If Prop.Name = "minecraft-java-exe" Then Continue For
                                Try
                                    Dim Entry = Prop.Value.FirstOrDefault
                                    If Entry Is Nothing Then Continue For
                                    Dim JavaName = Entry("version")("name").ToString
                                    If JavaName.Contains(".") Then
                                        Dim Parts As String() = JavaName.Split(".")
                                        Results.Add(New JavaDownloadInfo With {.ComponentName = Prop.Name, .Java = Entry, .JavaVersion = New Version(Val(Parts(0)), Val(Parts(1)), If(Parts.Length >= 3, Val(Parts(2)), 0), If(Parts.Length >= 4, Val(Parts(3)), 0))})
                                    ElseIf JavaName.StartsWithF("8u51-") Then
                                        Results.Add(New JavaDownloadInfo With {.ComponentName = Prop.Name, .Java = Entry, .JavaVersion = New Version(8, 0, 51, 0)})
                                    Else
                                        Results.Add(New JavaDownloadInfo With {.ComponentName = Prop.Name, .Java = Entry, .JavaVersion = New Version(Val(JavaName.BeforeFirstOfAny({"u", "."})), 0, 0, 0)})
                                    End If
                                Catch ex As Exception
                                    Logger.Warn(ex, $"解析 Mojang 提供的 Java 列表项失败，已跳过（{Prop.Name}）")
                                End Try
                            Next
                            Return Results
                        End Function)
                        If Not DownloadListWorker.HasSucceeded Then DownloadListWorker.Start(c, p?.SplitTo(0.4))
                        DownloadListWorker.WaitIfRunning(cancellationToken:=c)
                        p?.Set(0.4, skiped:=True)
                        Dim Components = DownloadListWorker.LastResult

                        Dim JavaToDownload As JavaDownloadInfo = Components.FirstOrDefault(
                            Function(Comp) Comp.Java("version")("name").ToString = RecommendedComponent,
                            Function(Comp) Range.Contains(Comp.JavaVersion))
                        If JavaToDownload Is Nothing Then
                            Logger.Warn($"Mojang 提供的 Java 列表中没有合适的 Java，无法自动下载")

                            Dim Description As String
                            Dim HasPureLower As Boolean = Range.HasLower AndAlso Range.IsLowerInclusive AndAlso
                            Range.Lower.Minor <= 0 AndAlso Range.Lower.Build <= 0 AndAlso Range.Lower.Revision <= 0
                            Dim HasPureUpper As Boolean = Range.HasUpper AndAlso Not Range.IsUpperInclusive AndAlso
                            Range.Upper.Minor <= 0 AndAlso Range.Upper.Build <= 0 AndAlso Range.Upper.Revision <= 0
                            If Range.HasLower AndAlso Range.HasUpper Then
                                If HasPureLower AndAlso HasPureUpper Then
                                    If Range.Lower.Major = Range.Upper.Major - 1 Then
                                        Description = $" Java {Range.Lower.Major}"
                                    Else
                                        Description = $" Java {Range.Lower.Major} ~ {Range.Upper.Major - 1}"
                                    End If
                                Else
                                    Description = $" Java {Range}"
                                End If
                            ElseIf Range.HasLower Then
                                If HasPureLower Then
                                    Description = $" Java {Range.Lower.Major} 或更高版本的 Java"
                                ElseIf Range.IsLowerInclusive Then
                                    Description = $" Java {Range.Lower} 或更高版本的 Java"
                                Else
                                    Description = $"版本高于 {Range.Lower} 的 Java"
                                End If
                            Else
                                If HasPureUpper Then
                                    Description = $" Java {Range.Upper.Major - 1} 或更低版本的 Java"
                                ElseIf Range.IsUpperInclusive Then
                                    Description = $" Java {Range.Upper} 或更低版本的 Java"
                                Else
                                    Description = $"版本低于 {Range.Upper} 的 Java"
                                End If
                            End If
                            MyMsgBox($"PCL 没能找到合适的 Java。{vbCrLf}请在网上搜索并手动安装{Description}， 然后在【设置 → 启动 → 高级选项 → Java 列表】中导入它。", "需要手动安装 Java")
                            Return Nothing
                        End If

                        Dim JavaLoader = CreateJavaDownloadLoader(JavaToDownload)
                        Try
                            JavaLoader.WaitForExit(IsForceRestart:=True, c:=c, p:=p?.SplitTo(0.95))
                        Finally
                            JavaLoader.Cancel()
                        End Try
                        Result = SelectJavaFromJavaList()
                        If Result IsNot Nothing Then p?.Finish() : Return Result
                        Throw New Exception($"下载 Java {JavaToDownload.ComponentName} 后仍然无法找到合适的 Java")
                    End SyncLock
            End Select
        Catch ex As Exception
            ex.ThrowIfCanceled()
            Logger.Error(ex, "选择 Java 时出现异常")
            p?.Skip() : Return Nothing
        End Try
    End Function
    Private Sub SwitchToInstanceSetup(Instance As McInstance)
        RunInUi(
        Sub()
            PageInstanceLeft.Instance = Instance
            FrmMain.PageChange(FormMain.PageType.InstanceSetup, FormMain.PageSubType.InstanceSetup)
        End Sub)
    End Sub
    Private ReadOnly JavaLock As New Object

    Private Sub MigrateInstanceJavaSettings(Instance As McInstance, Optional c As CancellationToken = Nothing, Optional p As ProgressProvider = Nothing)
        If Configs.JavaMigrated.Get(Instance.Config) Then p?.Skip() : Return
        If FrmInstanceSetup IsNot Nothing Then RunInUiWait(Sub() FrmInstanceSetup.ComboArgumentJava.IsEnabled = False)
        Try
            Logger.Info($"正在尝试迁移版本独立的 Java 设置（{Instance.Name}）")

            Dim ForcedJavaSetting = Settings.Get(Of String)("VersionArgumentJavaSelect", Instance)
            If Not ForcedJavaSetting.StartsWithF("{") Then
                p?.Set(0.4, True) : GoTo NoForcedJava
            End If
            Dim ForcedJava = New Java(ForcedJavaSetting.DeserializeJson(Of JObject)()("Path").ToString)
            If Not ForcedJava.CheckAsync(c).Run() Then
                Hint("该版本的设置中强制指定的 Java 存在异常，该设置已重置", HintType.Red)
                p?.Set(0.4) : GoTo NoForcedJava
            End If
            Logger.Info($"将版本独立设置改为老版本中强制指定的 Java：{ForcedJava}")
            If Not Configs.JavaList.Get().Contains(ForcedJava) Then
                Configs.JavaList.Edit(Sub(ByRef List) List.Add(ForcedJava))
                Logger.Info($"已将强制指定的 Java 加入 Java 列表底部：{ForcedJava}")
            End If
            Configs.JavaForced.Set(ForcedJava, Instance.Config)
            Settings.Set("VersionArgumentJavaV2", 3, Instance)
            UpdateJavaLists()
            Configs.JavaMigrated.Set(True, Instance.Config)
            p?.Finish() : Return
NoForcedJava:
            Dim JavaFound = JavaUtils.SearchFoldersAsync(True, {Instance.PathVersion}, c, p?.SplitTo(1)).Run()
            If JavaFound.Any() Then

                Logger.Info($"在版本文件夹中发现了 Java，将版本独立设置改为优先使用版本文件夹的 Java：{JavaFound}")
                Settings.Set("VersionArgumentJavaV2", 2, Instance)
            Else

                Logger.Info("没有需要迁移的内容，自动选择 Java")
                Settings.Set("VersionArgumentJavaV2", 0, Instance)
            End If
            Configs.JavaMigrated.Set(True, Instance.Config)
        Finally
            If FrmInstanceSetup IsNot Nothing Then RunInUiWait(
            Sub()
                SettingService.RefreshSettings(FrmInstanceSetup.ComboArgumentJava)
                SettingService.RefreshSettings(FrmInstanceSetup.ComboArgumentJavaSelect)
                FrmInstanceSetup.ComboArgumentJava.IsEnabled = True
            End Sub)
        End Try
    End Sub

#End Region

#Region "下载（均为内部工具方法，无外部调用）"

    Private Class JavaDownloadInfo
        Public Property ComponentName As String
        Public Property Java As JObject
        Public Property JavaVersion As Version
    End Class

    Private Function CreateJavaDownloadLoader(Target As JavaDownloadInfo) As LoaderCombo(Of Integer)

        Dim Address As String = Target.Java("manifest")("url")
        Logger.Info($"准备下载 Java {Target.Java("version")("name")}（{Target.ComponentName}）：{Address}")
        Dim ListFileStr As String = NetRequestByLoader(DlSourceOrder({Address}, {Address.Replace("piston-meta.mojang.com", "bmclapi2.bangbang93.com")}), IsJson:=True)
        Dim BaseDir As String = $"{Paths.AppData}.minecraft\runtime\{Target.ComponentName}\"
        Dim FilesToDownload As New List(Of NetFile)
        For Each File As JProperty In CType(ListFileStr.DeserializeJson(), JObject)("files")
            If CType(File.Value, JObject)("downloads")?("raw") Is Nothing Then Continue For
            Dim Info As JObject = CType(File.Value, JObject)("downloads")("raw")
            Dim Checker As New FileChecker With {.ActualSize = Info("size"), .Hash = Info("sha1")}
            If Checker.Hash = "12976a6c2b227cbac58969c1455444596c894656" OrElse Checker.Hash = "c80e4bab46e34d02826eab226a4441d0970f2aba" OrElse Checker.Hash = "84d2102ad171863db04e7ee22a259d1f6c5de4a5" Then
                Continue For
            End If
            Dim Url As String = Info("url")
            FilesToDownload.Add(New NetFile(
                DlSourceOrder({Url}, {Url.Replace("piston-data.mojang.com", "bmclapi2.bangbang93.com")}),
                BaseDir & File.Name, Checker))
        Next
        Logger.Info($"该 Java 需要下载 {FilesToDownload.Count} 个文件，目标文件夹：{BaseDir}")

        Dim Loader = New LoaderCombo(Of Integer)("下载 Java", {
            New LoaderDownload("下载 Java 文件", FilesToDownload) With {.ProgressWeight = 10},
            New LoaderWorker("刷新 Java 列表", JavaListRefreshWorker)
        }) With {.OnStateChanged =
        Sub(RawLoader As LoaderCombo(Of Integer))
            If RawLoader.State = LoadState.Failed OrElse RawLoader.State = LoadState.Canceled Then
                Logger.Warn($"由于 Java 下载未完成，清理未下载完成的 Java 文件夹：{BaseDir}")
                DirectoryUtils.Delete(BaseDir)
            End If
        End Sub}
        Return Loader

    End Function

#End Region

End Module
