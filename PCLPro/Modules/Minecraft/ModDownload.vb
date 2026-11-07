Public Module ModDownload

#Region "DlClient* | Minecraft 客户端"

    Public Function DlClientJarGet(Instance As McInstance, ReturnNothingOnFileUseable As Boolean) As NetFile

        Try
            Do While Not String.IsNullOrEmpty(Instance.InheritName)
                Instance = New McInstance(Instance.InheritName)
            Loop
        Catch ex As Exception
            Logger.Warn(ex, "获取底层继承版本失败")
        End Try

        If Instance.JsonObject("downloads") Is Nothing OrElse Instance.JsonObject("downloads")("client") Is Nothing OrElse Instance.JsonObject("downloads")("client")("url") Is Nothing Then
            Throw New Exception("底层版本 " & Instance.Name & " 中无 jar 文件下载信息")
        End If

        Dim Checker As New FileChecker With {.MinSize = 1024, .ActualSize = If(Instance.JsonObject("downloads")("client")("size"), -1), .Hash = Instance.JsonObject("downloads")("client")("sha1")}
        If ReturnNothingOnFileUseable AndAlso Checker.Check(Instance.PathVersion & Instance.Name & ".jar") Is Nothing Then Return Nothing

        Dim JarUrl As String = Instance.JsonObject("downloads")("client")("url")
        Return New NetFile(DlSourceLauncherOrMetaGet(JarUrl), Instance.PathVersion & Instance.Name & ".jar", Checker)
    End Function

    Public Function DlClientAssetIndexGet(Instance As McInstance) As NetFile

        Do While Not String.IsNullOrEmpty(Instance.InheritName)
            Instance = New McInstance(Instance.InheritName)
        Loop

        Dim IndexInfo = McAssetsGetIndex(Instance, True, True)
        Dim IndexAddress As String = McFolderSelected & "assets\indexes\" & IndexInfo("id").ToString & ".json"
        Logger.Info($"版本 {Instance.Name} 对应的资源文件索引为 {IndexInfo("id")}")
        Dim IndexUrl As String = If(IndexInfo("url"), "")
        If IndexUrl = "" Then
            Return Nothing
        Else
            Return New NetFile(DlSourceLauncherOrMetaGet(IndexUrl), IndexAddress, New FileChecker With {.IsJson = True})
        End If
    End Function

    Public Function DlClientFix(Instance As McInstance, CheckAssetsHash As Boolean, DownloadAssetIndexInBackground As Boolean, Optional QuickCheck As Boolean = False) As List(Of LoaderBase)
        Dim Loaders As New List(Of LoaderBase)

        Dim QuickFix As Func(Of List(Of NetFile), List(Of NetFile)) =
        Function(Files As List(Of NetFile)) As List(Of NetFile)
            If Not QuickCheck OrElse Files Is Nothing Then Return Files
            For Each File In Files
                If File Is Nothing OrElse File.Check Is Nothing Then Continue For
                File.Check.Hash = Nothing
            Next
            Logger.Info($"启动快速检查：跳过 {Files.Count} 个文件的 Hash 校验")
            Return Files
        End Function

#Region "下载支持库文件"
        Dim LoadersLib As New List(Of LoaderBase) From {
            New LoaderTask(Of String, List(Of NetFile))("分析缺失支持库文件", Sub(Task As LoaderTask(Of String, List(Of NetFile))) Task.Output = QuickFix(McLibNetFilesFromInstance(Instance))) With {.ProgressWeight = 1},
            New LoaderDownload("下载支持库文件", New List(Of NetFile)) With {.ProgressWeight = 15}
        }

        Loaders.Add(New LoaderCombo(Of String)("下载支持库文件（主加载器）", LoadersLib) With {.Block = False, .Show = False, .ProgressWeight = 16})
#End Region

#Region "下载资源文件"
        If ShouldIgnoreFileCheck(Instance) Then
            Logger.Warn("设置要求跳过所有 Assets 检查，文件可能不是最新的", LogBehavior.None)
        Else
            Dim LoadersAssets As New List(Of LoaderBase)

            Dim TempAddressMain As String = Nothing
            Dim RealAddressMain As String = Nothing
            LoadersAssets.Add(New LoaderTask(Of String, List(Of NetFile))("分析资源文件索引地址",
            Sub(Task As LoaderTask(Of String, List(Of NetFile)))
                Try
                    Dim IndexFile = DlClientAssetIndexGet(Instance)
                    If IndexFile Is Nothing Then
                        Task.Output = New List(Of NetFile)
                        Logger.Warn($"未找到版本 {Instance.Name} 的合适的资源索引下载地址，游戏 assets 可能缺失")
                        Return
                    End If
                    Dim IndexBackgroundFile = PathTemp & "Cache\" & IndexFile.LocalName & "_background"
                    If FileUtils.Exists(IndexBackgroundFile) AndAlso
                       FileUtils.GetInfo(IndexBackgroundFile).LastWriteTime.Date > FileUtils.GetInfo(IndexFile.LocalPath).LastWriteTime.Date Then
                        Logger.Info($"发现后台更新的资源索引缓存文件：{IndexBackgroundFile}")
                        FileUtils.Move(IndexBackgroundFile, IndexFile.LocalPath)
                    End If
                    If DownloadAssetIndexInBackground Then

                        If IndexFile.Check.Check(IndexFile.LocalPath) Is Nothing Then
                            Task.Output = New List(Of NetFile)
                        Else
                            Task.Output = New List(Of NetFile) From {IndexFile}
                        End If
                    Else

                        RealAddressMain = IndexFile.LocalPath
                        TempAddressMain = PathTemp & "Cache\" & IndexFile.LocalName
                        IndexFile.LocalPath = TempAddressMain
                        Task.Output = New List(Of NetFile) From {IndexFile}
                    End If
                Catch ex As Exception
                    Throw New Exception("分析资源文件索引地址失败", ex)
                End Try
            End Sub) With {.ProgressWeight = 0.5, .Show = False})

            LoadersAssets.Add(New LoaderDownload("下载资源文件索引", New List(Of NetFile)) With {.ProgressWeight = 2})

            LoadersAssets.Add(New LoaderTask(Of String, List(Of NetFile))("获取资源文件列表",
            Sub(Task)
                If TempAddressMain IsNot Nothing Then FileUtils.Move(TempAddressMain, RealAddressMain)
                Task.Output = McAssetsFixList(Instance, CheckAssetsHash, Task)
            End Sub) With {.ProgressWeight = 0.01})

            LoadersAssets.Add(New LoaderDownload("下载资源文件", New List(Of NetFile)) With {.ProgressWeight = 25})

            Loaders.Add(New LoaderCombo(Of String)("下载资源文件（主加载器）", LoadersAssets) With {.Block = False, .Show = False, .ProgressWeight = 27})

            If DownloadAssetIndexInBackground Then
                Dim RealAddressBg As String = Nothing
                Dim TempAddressBg As String = Nothing
                Dim LoadersAssetsUpdate As New List(Of LoaderBase)
                LoadersAssetsUpdate.Add(New LoaderTask(Of String, List(Of NetFile))("后台分析资源文件索引地址",
                Sub(Task As LoaderTask(Of String, List(Of NetFile)))
                    Dim IndexFile As NetFile = DlClientAssetIndexGet(Instance)
                    If IndexFile Is Nothing Then
                        Logger.Warn($"未找到版本 {Instance.Name} 的合适的资源索引下载地址，游戏 assets 可能缺失")
                        Task.Cancel()
                        Throw New OperationCanceledException
                    End If
                    RealAddressBg = IndexFile.LocalPath
                    TempAddressBg = PathTemp & "Cache\" & IndexFile.LocalName & "_background"
                    IndexFile.LocalPath = TempAddressBg
                    Task.Output = New List(Of NetFile) From {IndexFile}

                    Dim LastUpdate As Date = FileUtils.GetInfo(RealAddressBg).LastWriteTime.Date
                    If FileUtils.Exists(RealAddressBg) AndAlso Math.Abs((LastUpdate - Now.Date).TotalDays) < 1 Then
                        Logger.Info($"无需更新资源文件索引，上次更新于 {LastUpdate}")
                        Task.Cancel()
                    End If
                End Sub))
                LoadersAssetsUpdate.Add(New LoaderDownload("后台下载资源文件索引", New List(Of NetFile)))
                Dim Updater As New LoaderCombo(Of String)("后台更新资源文件索引", LoadersAssetsUpdate)
                Logger.Info("开始后台检查资源文件索引")
                Updater.Start()
            End If
        End If
#End Region

        Return Loaders
    End Function

#End Region

#Region "DlClientList | Minecraft 客户端 版本列表"

    Public Property AllDrops As List(Of Integer)
        Get
            If _AllDrops Is Nothing Then
                _AllDrops = Settings.Get(Of String)("CacheDrops").ToString.Split(",", True).Select(Function(d) CInt(Val(d))).ToList()
            End If
            Return If(_AllDrops.Any, _AllDrops, Nothing)
        End Get
        Set(value As List(Of Integer))
            _AllDrops = value
            Settings.Set("CacheDrops", value.Join(","c))
        End Set
    End Property
    Private _AllDrops As List(Of Integer) = Nothing

    Public Structure DlClientListResult

        Public SourceName As String

        Public IsOfficial As Boolean

        Public Value As JObject
    End Structure

    Public DlClientListLoader As New LoaderTask(Of String, DlClientListResult)("DlClientList Main", AddressOf DlClientListMain)
    Private Sub DlClientListMain(Loader As LoaderTask(Of String, DlClientListResult))
        Select Case Settings.Get(Of Integer)("ToolDownloadVersion")
            Case 0
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of String, DlClientListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of String, DlClientListResult), Integer)(DlClientListBmclapiLoader, 30),
                    New KeyValuePair(Of LoaderTask(Of String, DlClientListResult), Integer)(DlClientListMojangLoader, 30 + 60)
                }, Loader.IsForceRestarting)
            Case 1
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of String, DlClientListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of String, DlClientListResult), Integer)(DlClientListMojangLoader, 5),
                    New KeyValuePair(Of LoaderTask(Of String, DlClientListResult), Integer)(DlClientListBmclapiLoader, 5 + 30)
                }, Loader.IsForceRestarting)
            Case Else
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of String, DlClientListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of String, DlClientListResult), Integer)(DlClientListMojangLoader, 60),
                    New KeyValuePair(Of LoaderTask(Of String, DlClientListResult), Integer)(DlClientListBmclapiLoader, 60 + 60)
                }, Loader.IsForceRestarting)
        End Select

        Dim Drops As New List(Of Integer)
        For Each Version As JObject In Loader.Output.Value("versions")
            Dim Id As String = Version("id").ToString
            If Id.Contains("-") Then Continue For
            Dim Drop As Integer = McVersion.VersionToDrop(Id)
            If Drop = 209 Then Continue For
            Drops.Add(Drop)
        Next
        AllDrops = Drops.Distinct.OrderByDescending(Function(d) d).ToList()
    End Sub

    Public DlClientListMojangLoader As New LoaderTask(Of String, DlClientListResult)("DlClientList Mojang", AddressOf DlClientListMojangMain)
    Private Sub DlClientListMojangMain(Loader As LoaderTask(Of String, DlClientListResult))
        Dim StartTime As Long = GetTimeMs()
        Dim Json As JObject = NetRequestByClientRetry("https://launchermeta.mojang.com/mc/game/version_manifest.json", RequireJson:=True).DeserializeJson()
        Try
            Dim Versions As JArray = Json("versions")
            If Versions.Count < 200 Then Throw New Exception("获取到的版本列表长度不足（" & Json.ToString & "）")

            Static ProbeOnce As Boolean = False
            If Not ProbeOnce Then
                ProbeOnce = True
                Logger.Info($"Mojang 版本清单加载耗时：{GetTimeMs() - StartTime}ms")
                DlProbeSourceSpeed()
            End If

            Loader.Output = New DlClientListResult With {.IsOfficial = True, .SourceName = "Mojang 官方源", .Value = Json}

            Static IsHinted As Boolean = False
            Dim Version As String

            Version = Json("latest")("snapshot")
            If Settings.Get(Of Boolean)("ToolUpdateSnapshot") AndAlso Not Settings.Get(Of String)("ToolUpdateSnapshotLast") = "" AndAlso
               Settings.Get(Of String)("ToolUpdateSnapshotLast") <> Version AndAlso Not IsHinted Then
                IsHinted = True
                McDownloadClientUpdateHint(Version, Json)
            End If
            Settings.Set("ToolUpdateSnapshotLast", If(Version, "Nothing"))

            Version = Json("latest")("release")
            If Settings.Get(Of Boolean)("ToolUpdateRelease") AndAlso Not Settings.Get(Of String)("ToolUpdateReleaseLast") = "" AndAlso
               Settings.Get(Of String)("ToolUpdateReleaseLast") <> Version AndAlso Not IsHinted Then
                IsHinted = True
                McDownloadClientUpdateHint(Version, Json)
            End If
            Settings.Set("ToolUpdateReleaseLast", Version)
        Catch ex As Exception
            Throw New Exception("Minecraft 官方源版本列表解析失败", ex)
        End Try
    End Sub

    Public DlClientListBmclapiLoader As New LoaderTask(Of String, DlClientListResult)("DlClientList Bmclapi", AddressOf DlClientListBmclapiMain)
    Private Sub DlClientListBmclapiMain(Loader As LoaderTask(Of String, DlClientListResult))
        Dim Json As JObject = NetRequestByClientRetry("https://bmclapi2.bangbang93.com/mc/game/version_manifest.json", RequireJson:=True).DeserializeJson()
        Try
            Dim Versions As JArray = Json("versions")
            If Versions.Count < 200 Then Throw New Exception("获取到的版本列表长度不足（" & Json.ToString & "）")

            If FileUtils.Exists(PathTemp & "Cache\download.json") Then Versions.Merge(FileUtils.ReadAsJson(PathTemp & "Cache\download.json"))

            If Not String.IsNullOrEmpty(Loader.Input) Then
                Dim Id = Loader.Input
                If DlClientListLoader.Output.Value IsNot Nothing AndAlso Not DlClientListLoader.Output.Value("versions").Any(Function(v) v("id") = Id) Then
                    Throw New Exception("BMCLAPI 源未包含目标版本 " & Id)
                End If
            End If

            Loader.Output = New DlClientListResult With {.IsOfficial = False, .SourceName = "BMCLAPI", .Value = Json}
        Catch ex As Exception
            Throw New Exception("Minecraft BMCLAPI 版本列表解析失败（" & Json.ToString & "）", ex)
        End Try
    End Sub

    Public Function DlClientListGet(Id As String)
        Try

            Id = Id.Replace("_", "-")
            If Id <> "1.0" AndAlso Id.EndsWithF(".0") Then Id = Left(Id, Id.Length - 2)

            Select Case DlClientListLoader.State
                Case LoadState.Finished

                    For Each Version As JObject In DlClientListLoader.Output.Value("versions")
                        If Version("id") = Id Then Return Version("url").ToString
                    Next

                    DlClientListLoader.WaitForExit(Id, IsForceRestart:=True)
                Case LoadState.Loading
                    DlClientListLoader.WaitForExit(Id)
                Case LoadState.Failed, LoadState.Canceled, LoadState.Waiting
                    DlClientListLoader.WaitForExit(Id, IsForceRestart:=True)
            End Select

            For Each Version As JObject In DlClientListLoader.Output.Value("versions")
                If Version("id") = Id Then Return Version("url").ToString
            Next
            Logger.Warn($"未发现版本 {Id} 的 json 下载地址，版本列表返回为：{vbCrLf}{DlClientListLoader.Output.Value}")
            Return Nothing
        Catch ex As Exception
            Logger.Warn(ex, $"获取版本 {Id} 的 json 下载地址失败")
            Return Nothing
        End Try
    End Function

#End Region

#Region "DlOptiFineList | OptiFine 版本列表"

    Public Structure DlOptiFineListResult

        Public SourceName As String

        Public IsOfficial As Boolean

        Public Value As List(Of DlOptiFineListEntry)
    End Structure

    Public Class DlOptiFineListEntry

        Public DisplayName As String

        Public FileName As String

        Public InstanceName As String

        Public IsPreview As Boolean

        Public Property Inherit As String
            Get
                Return _inherit
            End Get
            Set(value As String)
                If value.EndsWithF(".0") Then value = Left(value, value.Length - 2)
                _inherit = value
            End Set
        End Property
        Private _inherit As String

        Public ReleaseTime As String

        Public RequiredForgeVersion As String
    End Class

    Public DlOptiFineListLoader As New LoaderTask(Of Integer, DlOptiFineListResult)("DlOptiFineList Main", AddressOf DlOptiFineListMain)
    Private Sub DlOptiFineListMain(Loader As LoaderTask(Of Integer, DlOptiFineListResult))
        Select Case Settings.Get(Of Integer)("ToolDownloadVersion")
            Case 0
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)(DlOptiFineListBmclapiLoader, 30),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)(DlOptiFineListOfficialLoader, 30 + 60)
                }, Loader.IsForceRestarting)
            Case 1
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)(DlOptiFineListOfficialLoader, 5),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)(DlOptiFineListBmclapiLoader, 5 + 30)
                }, Loader.IsForceRestarting)
            Case Else
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)(DlOptiFineListOfficialLoader, 60),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlOptiFineListResult), Integer)(DlOptiFineListBmclapiLoader, 60 + 60)
                }, Loader.IsForceRestarting)
        End Select
    End Sub

    Public DlOptiFineListOfficialLoader As New LoaderTask(Of Integer, DlOptiFineListResult)("DlOptiFineList Official", AddressOf DlOptiFineListOfficialMain)
    Private Sub DlOptiFineListOfficialMain(Loader As LoaderTask(Of Integer, DlOptiFineListResult))
        Dim Result As String = NetRequestByClientRetry("https://optifine.net/downloads", Encoding:=Encoding.Default)
        If Result.Length < 200 Then Throw New Exception("获取到的版本列表长度不足（" & Result & "）")
        Try

            Dim Forge As List(Of String) = Result.RegexSearch("(?<=colForge'>)[^<]*").ToList
            Dim ReleaseTime As List(Of String) = Result.RegexSearch("(?<=colDate'>)[^<]+").ToList
            Dim Name As List(Of String) = Result.RegexSearch("(?<=OptiFine_)[0-9A-Za-z_.]+(?=.jar"")").ToList
            If Not ReleaseTime.Count = Name.Count Then Throw New Exception("版本与发布时间数据无法对应")
            If Not Forge.Count = Name.Count Then Throw New Exception("版本与 Forge 兼容数据无法对应")
            If ReleaseTime.Count < 10 Then Throw New Exception("获取到的版本数量不足（" & Result & "）")

            Dim Versions As New List(Of DlOptiFineListEntry)
            For i = 0 To ReleaseTime.Count - 1
                Name(i) = Name(i).Replace("_", " ")
                Dim Entry As New DlOptiFineListEntry With {
                    .DisplayName = Name(i).Replace("HD U ", "").Replace(".0 ", " "),
                    .ReleaseTime = {ReleaseTime(i).Split(".")(2), ReleaseTime(i).Split(".")(1), ReleaseTime(i).Split(".")(0)}.Join("/"c),
                    .IsPreview = Name(i).ContainsIgnoreCase("pre"),
                    .Inherit = Name(i).ToString.Split(" ")(0),
                    .FileName = If(Name(i).ContainsIgnoreCase("pre"), "preview_", "") & "OptiFine_" & Name(i).Replace(" ", "_") & ".jar",
                    .RequiredForgeVersion = Forge(i).Replace("Forge ", "").Replace("#", "")}
                If Entry.RequiredForgeVersion.Contains("N/A") Then Entry.RequiredForgeVersion = Nothing
                Entry.InstanceName = Entry.Inherit & "-OptiFine_" & Name(i).AfterFirst(" ").Replace(" ", "_")
                Versions.Add(Entry)
            Next
            Loader.Output = New DlOptiFineListResult With {.IsOfficial = True, .SourceName = "OptiFine 官方源", .Value = Versions}
        Catch ex As Exception
            Throw New Exception("OptiFine 官方源版本列表解析失败（" & Result & "）", ex)
        End Try
    End Sub

    Public DlOptiFineListBmclapiLoader As New LoaderTask(Of Integer, DlOptiFineListResult)("DlOptiFineList Bmclapi", AddressOf DlOptiFineListBmclapiMain)
    Private Sub DlOptiFineListBmclapiMain(Loader As LoaderTask(Of Integer, DlOptiFineListResult))
        Dim Json As JArray = NetRequestByClientRetry("https://bmclapi2.bangbang93.com/optifine/versionList", RequireJson:=True).DeserializeJson()
        Try
            Dim Versions As New List(Of DlOptiFineListEntry)
            For Each Token As JObject In Json
                Dim Entry As New DlOptiFineListEntry With {
                    .DisplayName = (Token("mcversion").ToString & Token("type").ToString.Replace("HD_U", "").Replace("_", " ") & " " & Token("patch").ToString).Replace(".0 ", " "),
                    .ReleaseTime = "",
                    .IsPreview = Token("patch").ToString.ContainsIgnoreCase("pre"),
                    .Inherit = Token("mcversion").ToString,
                    .FileName = Token("filename").ToString,
                    .RequiredForgeVersion = If(Token("forge"), "").ToString.Replace("Forge ", "").Replace("#", "")
                }
                If Entry.RequiredForgeVersion.Contains("N/A") Then Entry.RequiredForgeVersion = Nothing
                Entry.InstanceName = Entry.Inherit & "-OptiFine_" & (Token("type").ToString & " " & Token("patch").ToString).Replace(".0 ", " ").Replace(" ", "_").Replace(Entry.Inherit & "_", "")
                Versions.Add(Entry)
            Next
            Loader.Output = New DlOptiFineListResult With {.IsOfficial = False, .SourceName = "BMCLAPI", .Value = Versions}
        Catch ex As Exception
            Throw New Exception("OptiFine BMCLAPI 版本列表解析失败（" & Json.ToString & "）", ex)
        End Try
    End Sub

#End Region

#Region "DlForgeList | Forge Minecraft 版本列表"

    Public Structure DlForgeListResult

        Public SourceName As String

        Public IsOfficial As Boolean

        Public Value As List(Of String)
    End Structure

    Public DlForgeListLoader As New LoaderTask(Of Integer, DlForgeListResult)("DlForgeList Main", AddressOf DlForgeListMain)
    Private Sub DlForgeListMain(Loader As LoaderTask(Of Integer, DlForgeListResult))
        Select Case Settings.Get(Of Integer)("ToolDownloadVersion")
            Case 0
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)(DlForgeListBmclapiLoader, 30),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)(DlForgeListOfficialLoader, 30 + 60)
                }, Loader.IsForceRestarting)
            Case 1
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)(DlForgeListOfficialLoader, 5),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)(DlForgeListBmclapiLoader, 5 + 30)
                }, Loader.IsForceRestarting)
            Case Else
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)(DlForgeListOfficialLoader, 60),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlForgeListResult), Integer)(DlForgeListBmclapiLoader, 60 + 60)
                }, Loader.IsForceRestarting)
        End Select
    End Sub

    Public DlForgeListOfficialLoader As New LoaderTask(Of Integer, DlForgeListResult)("DlForgeList Official", AddressOf DlForgeListOfficialMain)
    Private Sub DlForgeListOfficialMain(Loader As LoaderTask(Of Integer, DlForgeListResult))
        Dim Result As String = NetRequestByClientRetry("https://files.minecraftforge.net/maven/net/minecraftforge/forge/index_1.2.4.html", Encoding:=Encoding.Default, Accept:="text/html", SimulateBrowserHeaders:=True)
        If Result.Length < 200 Then Throw New Exception("获取到的版本列表长度不足（" & Result & "）")

        Dim Names As List(Of String) = Result.RegexSearch("(?<=a href=""index_)[0-9.]+((_pre|[_-]snapshot[_-]?)[0-9]?)?(?=.html)").ToList
        Names.Add("1.2.4")
        If Names.Count < 10 Then Throw New Exception("获取到的版本数量不足（" & Result & "）")
        Loader.Output = New DlForgeListResult With {.IsOfficial = True, .SourceName = "Forge 官方源", .Value = Names}
    End Sub

    Public DlForgeListBmclapiLoader As New LoaderTask(Of Integer, DlForgeListResult)("DlForgeList Bmclapi", AddressOf DlForgeListBmclapiMain)
    Private Sub DlForgeListBmclapiMain(Loader As LoaderTask(Of Integer, DlForgeListResult))
        Dim Result As JArray = NetRequestByClientRetry("https://bmclapi2.bangbang93.com/forge/minecraft",
                                                       Encoding:=Encoding.Default, RequireJson:=True).DeserializeJson()
        Dim Names As List(Of String) = Result.Select(Function(v) v.ToString).ToList
        If Names.Count < 10 Then Throw New Exception("获取到的版本数量不足")
        Loader.Output = New DlForgeListResult With {.IsOfficial = False, .SourceName = "BMCLAPI", .Value = Names}
    End Sub

#End Region

#Region "DlForgeVersion | Forge 版本列表"

    Public MustInherit Class DlForgelikeEntry
        Implements IComparable(Of DlForgelikeEntry)

        Public IsNeoForge As Boolean

        Public MustOverride ReadOnly Property LoaderName As String

        Public MustOverride ReadOnly Property FileExtension As String

        Public Version As Version

        Public VersionName As String

        Public Inherit As String

        Public Function CompareTo(other As DlForgelikeEntry) As Integer Implements IComparable(Of DlForgelikeEntry).CompareTo
            If Version <> other.Version Then
                Return Version.CompareTo(other.Version)
            Else
                Return McUtils.CompareVersion(VersionName, other.VersionName)
            End If
        End Function
    End Class

    Public Class DlForgeVersionEntry
        Inherits DlForgelikeEntry

        Public ReleaseTime As String

        Public Hash As String = Nothing

        Public IsRecommended As Boolean

        Public Category As String

        Public FileVersion As String

        Public Overrides ReadOnly Property LoaderName As String
            Get
                Return "Forge"
            End Get
        End Property

        Public Overrides ReadOnly Property FileExtension As String
            Get
                Return If(Category = "installer", "jar", "zip")
            End Get
        End Property

        Public Sub New(Version As String, Branch As String, Inherit As String)

            If Version = "11.15.1.2318" OrElse Version = "11.15.1.1902" OrElse Version = "11.15.1.1890" Then Branch = "1.8.9"
            If Branch Is Nothing AndAlso Inherit = "1.7.10" AndAlso Version.Split(".")(3) >= 1300 Then Branch = "1.7.10"

            IsNeoForge = False
            VersionName = Version
            Me.Version = New Version(Version)
            Me.Inherit = Inherit
            FileVersion = Version & If(Branch Is Nothing, "", "-" & Branch)
        End Sub

    End Class

    Public Sub DlForgeVersionMain(Loader As LoaderTask(Of String, List(Of DlForgeVersionEntry)))
        Dim DlForgeVersionOfficialLoader As New LoaderTask(Of String, List(Of DlForgeVersionEntry))("DlForgeVersion Official", AddressOf DlForgeVersionOfficialMain)
        Dim DlForgeVersionBmclapiLoader As New LoaderTask(Of String, List(Of DlForgeVersionEntry))("DlForgeVersion Bmclapi", AddressOf DlForgeVersionBmclapiMain)
        Select Case Settings.Get(Of Integer)("ToolDownloadVersion")
            Case 0
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)(DlForgeVersionBmclapiLoader, 30),
                    New KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)(DlForgeVersionOfficialLoader, 30 + 60)
                }, Loader.IsForceRestarting)
            Case 1
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)(DlForgeVersionOfficialLoader, 5),
                    New KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)(DlForgeVersionBmclapiLoader, 5 + 30)
                }, Loader.IsForceRestarting)
            Case Else
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)(DlForgeVersionOfficialLoader, 60),
                    New KeyValuePair(Of LoaderTask(Of String, List(Of DlForgeVersionEntry)), Integer)(DlForgeVersionBmclapiLoader, 60 + 60)
                }, Loader.IsForceRestarting)
        End Select
    End Sub

    Public Sub DlForgeVersionOfficialMain(Loader As LoaderTask(Of String, List(Of DlForgeVersionEntry)))
        Dim Result As String
        Try
            Result = NetRequestByLoader("https://files.minecraftforge.net/maven/net/minecraftforge/forge/index_" &
                                          Loader.Input.Replace("-", "_") &
                                          ".html", SimulateBrowserHeaders:=True)
        Catch ex As Exception
            If ex.GetDisplay(False).Contains("(404)") Then
                Throw New Exception("无")
            Else
                Throw
            End If
        End Try
        If Result.Length < 1000 Then Throw New Exception("获取到的版本列表长度不足（" & Result & "）")
        Dim Versions As New List(Of DlForgeVersionEntry)
        Try

            Dim VersionCodes = Result.Substring(0, Result.LastIndexOfF("</table>")).Split("<td class=""download-version")

            For i = 1 To VersionCodes.Count - 1
                Dim VersionCode = VersionCodes(i)
                Try

                    Dim Name As String = VersionCode.RegexSeek("(?<=[^(0-9)]+)[0-9\.]+")
                    Dim IsRecommended As Boolean = VersionCode.Contains("fa promo-recommended")
                    Dim Inherit As String = Loader.Input

                    Dim Branch As String = VersionCode.RegexSeek($"(?<=-{Name}-)[^-""]+(?=-[a-z]+.[a-z]{{3}})")
                    If String.IsNullOrWhiteSpace(Branch) Then Branch = Nothing

                    Dim ReleaseTimeOriginal = VersionCode.RegexSeek("(?<=""download-time"" title="")[^""]+")
                    Dim ReleaseTimeSplit = ReleaseTimeOriginal.Split(" -:".ToCharArray)
                    Dim ReleaseDate As New Date(ReleaseTimeSplit(0), ReleaseTimeSplit(1), ReleaseTimeSplit(2),
                                                ReleaseTimeSplit(3), ReleaseTimeSplit(4), ReleaseTimeSplit(5),
                                                0, DateTimeKind.Utc)
                    Dim ReleaseTime As String = ReleaseDate.ToLocalTime.ToString("yyyy'/'MM'/'dd HH':'mm")

                    Dim MD5 As String, Category As String
                    If VersionCode.Contains("classifier-installer""") Then

                        VersionCode = VersionCode.Substring(VersionCode.IndexOfF("installer.jar"))
                        MD5 = VersionCode.RegexSeek("(?<=MD5:</strong> )[^<]+")
                        Category = "installer"
                    ElseIf VersionCode.Contains("classifier-universal""") Then

                        VersionCode = VersionCode.Substring(VersionCode.IndexOfF("universal.zip"))
                        MD5 = VersionCode.RegexSeek("(?<=MD5:</strong> )[^<]+")
                        Category = "universal"
                    ElseIf VersionCode.Contains("client.zip") Then

                        VersionCode = VersionCode.Substring(VersionCode.IndexOfF("client.zip"))
                        MD5 = VersionCode.RegexSeek("(?<=MD5:</strong> )[^<]+")
                        Category = "client"
                    Else

                        Continue For
                    End If

                    Versions.Add(New DlForgeVersionEntry(Name, Branch, Inherit) With {.Category = Category, .IsRecommended = IsRecommended, .Hash = MD5.ReplaceLineEndings(""), .ReleaseTime = ReleaseTime})
                Catch ex As Exception
                    Throw New Exception("Forge 官方源版本信息提取失败（" & VersionCode & "）", ex)
                End Try
            Next
        Catch ex As Exception
            Throw New Exception("Forge 官方源版本列表解析失败（" & Result & "）", ex)
        End Try
        If Not Versions.Any() Then Throw New Exception("无")
        Loader.Output = Versions
    End Sub

    Public Sub DlForgeVersionBmclapiMain(Loader As LoaderTask(Of String, List(Of DlForgeVersionEntry)))
        Dim Json As JArray = NetRequestByClientRetry("https://bmclapi2.bangbang93.com/forge/minecraft/" &
            Loader.Input.Replace("-", "_"), RequireJson:=True).DeserializeJson()
        Dim Versions As New List(Of DlForgeVersionEntry)
        Try
            Dim Recommended As String = McDownloadForgeRecommendedGet(Loader.Input)
            For Each Token As JObject In Json

                Dim Hash As String = Nothing, Category As String = "unknown", Proi As Integer = -1
                For Each File As JObject In Token("files")
                    Select Case File("category").ToString
                        Case "installer"
                            If File("format").ToString = "jar" Then

                                Hash = File("hash")
                                Category = "installer"
                                Proi = 2
                            End If
                        Case "universal"
                            If Proi <= 1 AndAlso File("format").ToString = "zip" Then

                                Hash = File("hash")
                                Category = "universal"
                                Proi = 1
                            End If
                        Case "client"
                            If Proi <= 0 AndAlso File("format").ToString = "zip" Then

                                Hash = File("hash")
                                Category = "client"
                                Proi = 0
                            End If
                    End Select
                Next

                Dim Branch As String = Token("branch")
                Dim Name As String = Token("version")

                Dim Entry = New DlForgeVersionEntry(Name, Branch, Loader.Input) With {.Hash = Hash, .Category = Category, .IsRecommended = Recommended = Name}
                Dim TimeSplit = Token("modified").ToString.Split("-"c, "T"c, ":"c, "."c, " "c, "/"c)
                Entry.ReleaseTime = Token("modified").ToObject(Of Date).ToLocalTime.ToString("yyyy'/'MM'/'dd HH':'mm")

                Versions.Add(Entry)
            Next
        Catch ex As Exception
            Throw New Exception("Forge BMCLAPI 版本列表解析失败（" & Json.ToString & "）", ex)
        End Try
        If Not Versions.Any() Then Throw New Exception("无")
        Loader.Output = Versions
    End Sub

#End Region

#Region "DlNeoForgeList | NeoForge 版本列表"

    Public Structure DlNeoForgeListResult

        Public SourceName As String

        Public IsOfficial As Boolean

        Public Value As List(Of DlNeoForgeListEntry)
    End Structure

    Public Class DlNeoForgeListEntry
        Inherits DlForgelikeEntry

        Public IsBeta As Boolean

        Public ApiName As String

        Public ReadOnly Property UrlBase As String
            Get
                Dim PackageName As String = If(Inherit = "1.20.1", "forge", "neoforge")
                Return $"https://maven.neoforged.net/releases/net/neoforged/{PackageName}/{ApiName}/{PackageName}-{ApiName}"
            End Get
        End Property

        Public Overrides ReadOnly Property LoaderName As String
            Get
                Return "NeoForge"
            End Get
        End Property

        Public Overrides ReadOnly Property FileExtension As String
            Get
                Return "jar"
            End Get
        End Property

        Public Sub New(ApiName As String)
            IsNeoForge = True
            Me.ApiName = ApiName
            IsBeta = ApiName.ContainsIgnoreCase("beta") OrElse ApiName.ContainsIgnoreCase("alpha")
            If ApiName.Contains("1.20.1") Then
                VersionName = ApiName.Replace("1.20.1-", "")
                Version = New Version("19." & VersionName)
                Inherit = "1.20.1"
            ElseIf ApiName.StartsWithF("0.") Then
                VersionName = ApiName
                Dim Segments = ApiName.BeforeFirst("-").Split("."c)
                Version = New Version(0, 0, Segments.Last)
                Inherit = Segments(1)
            Else
                VersionName = ApiName
                Version = New Version(ApiName.BeforeFirst("-"))
                If Version.Major >= 24 Then
                    Inherit = $"{Version.Major}.{Version.Minor}.{Version.Build}"
                Else
                    Inherit = $"1.{Version.Major}.{Version.Minor}"
                End If
                If Inherit.EndsWithF(".0") Then Inherit = Left(Inherit, Inherit.Length - 2)
                If VersionName.Contains("+") Then Inherit &= "-" & VersionName.AfterFirst("+")
            End If
        End Sub
    End Class

    Public DlNeoForgeListLoader As New LoaderTask(Of Integer, DlNeoForgeListResult)("DlNeoForgeList Main", AddressOf DlNeoForgeListMain)
    Private Sub DlNeoForgeListMain(Loader As LoaderTask(Of Integer, DlNeoForgeListResult))
        Select Case Settings.Get(Of Integer)("ToolDownloadVersion")
            Case 0
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlNeoForgeListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlNeoForgeListResult), Integer)(DlNeoForgeListBmclapiLoader, 30),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlNeoForgeListResult), Integer)(DlNeoForgeListOfficialLoader, 30 + 60)
                }, Loader.IsForceRestarting)
            Case 1
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlNeoForgeListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlNeoForgeListResult), Integer)(DlNeoForgeListOfficialLoader, 5),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlNeoForgeListResult), Integer)(DlNeoForgeListBmclapiLoader, 5 + 30)
                }, Loader.IsForceRestarting)
            Case Else
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlNeoForgeListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlNeoForgeListResult), Integer)(DlNeoForgeListOfficialLoader, 60),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlNeoForgeListResult), Integer)(DlNeoForgeListBmclapiLoader, 60 + 60)
                }, Loader.IsForceRestarting)
        End Select
    End Sub

    Public DlNeoForgeListOfficialLoader As New LoaderTask(Of Integer, DlNeoForgeListResult)("DlNeoForgeList Official", AddressOf DlNeoForgeListOfficialMain)
    Private Sub DlNeoForgeListOfficialMain(Loader As LoaderTask(Of Integer, DlNeoForgeListResult))

        Dim ResultLatest As String = NetRequestByClientRetry("https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/neoforge", RequireJson:=True)
        Dim ResultLegacy As String = NetRequestByClientRetry("https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/forge", RequireJson:=True)
        If ResultLatest.Length < 100 OrElse ResultLegacy.Length < 100 Then Throw New Exception("获取到的版本列表长度不足（" & ResultLatest & "）")

        Try
            Loader.Output = New DlNeoForgeListResult With {.IsOfficial = True, .SourceName = "NeoForge 官方源",
                .Value = GetNeoForgeEntries(ResultLatest, ResultLegacy)}
        Catch ex As Exception
            Throw New Exception("NeoForge 官方源版本列表解析失败（" & ResultLatest & vbCrLf & vbCrLf & ResultLegacy & "）", ex)
        End Try
    End Sub

    Public DlNeoForgeListBmclapiLoader As New LoaderTask(Of Integer, DlNeoForgeListResult)("DlNeoForgeList Bmclapi", AddressOf DlNeoForgeListBmclapiMain)
    Public Sub DlNeoForgeListBmclapiMain(Loader As LoaderTask(Of Integer, DlNeoForgeListResult))

        Dim ResultLatest As String = NetRequestByClientRetry("https://bmclapi2.bangbang93.com/neoforge/meta/api/maven/details/releases/net/neoforged/neoforge", RequireJson:=True)
        Dim ResultLegacy As String = NetRequestByClientRetry("https://bmclapi2.bangbang93.com/neoforge/meta/api/maven/details/releases/net/neoforged/forge", RequireJson:=True)
        If ResultLatest.Length < 100 OrElse ResultLegacy.Length < 100 Then Throw New Exception("获取到的版本列表长度不足（" & ResultLatest & "）")

        Try
            Loader.Output = New DlNeoForgeListResult With {.IsOfficial = True, .SourceName = "BMCLAPI",
                .Value = GetNeoForgeEntries(ResultLatest, ResultLegacy)}
        Catch ex As Exception
            Throw New Exception("NeoForge BMCLAPI 版本列表解析失败（" & ResultLatest & vbCrLf & vbCrLf & ResultLegacy & "）", ex)
        End Try
    End Sub

    Private Function GetNeoForgeEntries(LatestJson As String, LatestLegacyJson As String) As List(Of DlNeoForgeListEntry)
        Dim VersionNames = (LatestLegacyJson & LatestJson).RegexSearch(
            "(?<="")(1\.20\.1-)?\d+\.[^\.]+\.\d+(\.\d+)?(-(beta|alpha)(\.\d+)?)?(\+snapshot-\d+)?(?="")")
        Dim Versions = VersionNames.
            Where(Function(name) name <> "47.1.82").
            Select(Function(name) New DlNeoForgeListEntry(name)).
            OrderByDescending(Function(a) a).ToList
        If Not Versions.Any() Then Throw New Exception("无")
        Return Versions
    End Function

#End Region

#Region "DlLiteLoaderList | LiteLoader 版本列表"

    Public Structure DlLiteLoaderListResult

        Public SourceName As String

        Public IsOfficial As Boolean

        Public Value As List(Of DlLiteLoaderListEntry)

        Public OfficialError As Exception
    End Structure

    Public Class DlLiteLoaderListEntry

        Public FileName As String

        Public IsPreview As Boolean

        Public Inherit As String

        Public IsLegacy As Boolean

        Public ReleaseTime As String

        Public MD5 As String

        Public JsonToken As JToken
    End Class

    Public DlLiteLoaderListLoader As New LoaderTask(Of Integer, DlLiteLoaderListResult)("DlLiteLoaderList Main", AddressOf DlLiteLoaderListMain)
    Private Sub DlLiteLoaderListMain(Loader As LoaderTask(Of Integer, DlLiteLoaderListResult))
        Select Case Settings.Get(Of Integer)("ToolDownloadVersion")
            Case 0
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)(DlLiteLoaderListBmclapiLoader, 30),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)(DlLiteLoaderListOfficialLoader, 30 + 60)
                }, Loader.IsForceRestarting)
            Case 1
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)(DlLiteLoaderListOfficialLoader, 5),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)(DlLiteLoaderListBmclapiLoader, 5 + 30)
                }, Loader.IsForceRestarting)
            Case Else
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)(DlLiteLoaderListOfficialLoader, 60),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlLiteLoaderListResult), Integer)(DlLiteLoaderListBmclapiLoader, 60 + 60)
                }, Loader.IsForceRestarting)
        End Select
    End Sub

    Public DlLiteLoaderListOfficialLoader As New LoaderTask(Of Integer, DlLiteLoaderListResult)("DlLiteLoaderList Official", AddressOf DlLiteLoaderListOfficialMain)
    Private Sub DlLiteLoaderListOfficialMain(Loader As LoaderTask(Of Integer, DlLiteLoaderListResult))
        Dim Result As JObject = NetRequestByClientRetry("https://dl.liteloader.com/versions/versions.json", RequireJson:=True).DeserializeJson()
        Try
            Dim Json As JObject = Result("versions")
            Dim Versions As New List(Of DlLiteLoaderListEntry)
            For Each Pair As KeyValuePair(Of String, JToken) In Json
                If Pair.Key.StartsWithF("1.6") OrElse Pair.Key.StartsWithF("1.5") Then Continue For
                Dim RealEntry As JToken = If(Pair.Value("artefacts"), Pair.Value("snapshots"))("com.mumfrey:liteloader")("latest")
                Versions.Add(New DlLiteLoaderListEntry With {
                             .Inherit = Pair.Key,
                             .IsLegacy = Pair.Key.Split(".")(1) < 8,
                             .IsPreview = RealEntry("stream").ToString.Lower = "snapshot",
                             .FileName = "liteloader-installer-" & Pair.Key & If(Pair.Key = "1.8" OrElse Pair.Key = "1.9", ".0", "") & "-00-SNAPSHOT.jar",
                             .MD5 = RealEntry("md5"),
                             .ReleaseTime = GetLocalTime(GetDate(RealEntry("timestamp"))).ToString("yyyy'/'MM'/'dd HH':'mm"),
                             .JsonToken = RealEntry
                         })
            Next
            Loader.Output = New DlLiteLoaderListResult With {.IsOfficial = True, .SourceName = "LiteLoader 官方源", .Value = Versions}
        Catch ex As Exception
            Throw New Exception("LiteLoader 官方源版本列表解析失败（" & Result.ToString & "）", ex)
        End Try
    End Sub

    Public DlLiteLoaderListBmclapiLoader As New LoaderTask(Of Integer, DlLiteLoaderListResult)("DlLiteLoaderList Bmclapi", AddressOf DlLiteLoaderListBmclapiMain)
    Private Sub DlLiteLoaderListBmclapiMain(Loader As LoaderTask(Of Integer, DlLiteLoaderListResult))
        Dim Result As JObject = NetRequestByClientRetry("https://bmclapi2.bangbang93.com/maven/com/mumfrey/liteloader/versions.json", RequireJson:=True).DeserializeJson()
        Try
            Dim Json As JObject = Result("versions")
            Dim Versions As New List(Of DlLiteLoaderListEntry)
            For Each Pair As KeyValuePair(Of String, JToken) In Json
                If Pair.Key.StartsWithF("1.6") OrElse Pair.Key.StartsWithF("1.5") Then Continue For
                Dim RealEntry As JToken = If(Pair.Value("artefacts"), Pair.Value("snapshots"))("com.mumfrey:liteloader")("latest")
                Versions.Add(New DlLiteLoaderListEntry With {
                             .Inherit = Pair.Key,
                             .IsLegacy = Pair.Key.Split(".")(1) < 8,
                             .IsPreview = RealEntry("stream").ToString.Lower = "snapshot",
                             .FileName = "liteloader-installer-" & Pair.Key & If(Pair.Key = "1.8" OrElse Pair.Key = "1.9", ".0", "") & "-00-SNAPSHOT.jar",
                             .MD5 = RealEntry("md5"),
                             .ReleaseTime = GetLocalTime(GetDate(RealEntry("timestamp"))).ToString("yyyy'/'MM'/'dd HH':'mm"),
                             .JsonToken = RealEntry
                         })
            Next
            Loader.Output = New DlLiteLoaderListResult With {.IsOfficial = False, .SourceName = "BMCLAPI", .Value = Versions}
        Catch ex As Exception
            Throw New Exception("LiteLoader BMCLAPI 版本列表解析失败（" & Result.ToString & "）", ex)
        End Try
    End Sub

#End Region

#Region "DlFabricList | Fabric 列表"

    Public Structure DlFabricListResult

        Public SourceName As String

        Public IsOfficial As Boolean

        Public Value As JObject
    End Structure

    Public DlFabricListLoader As New LoaderTask(Of Integer, DlFabricListResult)("DlFabricList Main", AddressOf DlFabricListMain)
    Private Sub DlFabricListMain(Loader As LoaderTask(Of Integer, DlFabricListResult))
        Select Case Settings.Get(Of Integer)("ToolDownloadVersion")
            Case 0
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)(DlFabricListBmclapiLoader, 30),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)(DlFabricListOfficialLoader, 30 + 60)
                }, Loader.IsForceRestarting)
            Case 1
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)(DlFabricListOfficialLoader, 5),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)(DlFabricListBmclapiLoader, 5 + 30)
                }, Loader.IsForceRestarting)
            Case Else
                DlSourceLoader(Loader, New List(Of KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)) From {
                    New KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)(DlFabricListOfficialLoader, 60),
                    New KeyValuePair(Of LoaderTask(Of Integer, DlFabricListResult), Integer)(DlFabricListBmclapiLoader, 60 + 60)
                }, Loader.IsForceRestarting)
        End Select
    End Sub

    Public DlFabricListOfficialLoader As New LoaderTask(Of Integer, DlFabricListResult)("DlFabricList Official", AddressOf DlFabricListOfficialMain)
    Private Sub DlFabricListOfficialMain(Loader As LoaderTask(Of Integer, DlFabricListResult))
        Dim Result As JObject = NetRequestByClientRetry("https://meta.fabricmc.net/v2/versions", RequireJson:=True).DeserializeJson()
        Try
            Dim Output = New DlFabricListResult With {.IsOfficial = True, .SourceName = "Fabric 官方源", .Value = Result}
            If Output.Value("game") Is Nothing OrElse Output.Value("loader") Is Nothing OrElse Output.Value("installer") Is Nothing Then Throw New Exception("获取到的列表缺乏必要项")
            Loader.Output = Output
        Catch ex As Exception
            Throw New Exception("Fabric 官方源版本列表解析失败（" & Result.ToString & "）", ex)
        End Try
    End Sub

    Public DlFabricListBmclapiLoader As New LoaderTask(Of Integer, DlFabricListResult)("DlFabricList Bmclapi", AddressOf DlFabricListBmclapiMain)
    Private Sub DlFabricListBmclapiMain(Loader As LoaderTask(Of Integer, DlFabricListResult))
        Dim Result As JObject = NetRequestByClientRetry("https://bmclapi2.bangbang93.com/fabric-meta/v2/versions", RequireJson:=True).DeserializeJson()
        Try
            Dim Output = New DlFabricListResult With {.IsOfficial = False, .SourceName = "BMCLAPI", .Value = Result}
            If Output.Value("game") Is Nothing OrElse Output.Value("loader") Is Nothing OrElse Output.Value("installer") Is Nothing Then Throw New Exception("获取到的列表缺乏必要项")
            Loader.Output = Output
        Catch ex As Exception
            Throw New Exception("Fabric BMCLAPI 版本列表解析失败（" & Result.ToString & "）", ex)
        End Try
    End Sub

    Public DlFabricApiLoader As New LoaderTask(Of Integer, List(Of ResourceVersion))("Fabric API List Loader",
        Sub(Task As LoaderTask(Of Integer, List(Of ResourceVersion))) Task.Output = ResourceVersion.FromProjectId("fabric-api", ResourcePlatforms.Modrinth, LoadDependencies:=False))

    Public DlOptiFabricLoader As New LoaderTask(Of Integer, List(Of ResourceVersion))("OptiFabric List Loader",
        Sub(Task As LoaderTask(Of Integer, List(Of ResourceVersion))) Task.Output = ResourceVersion.FromProjectId("322385", ResourcePlatforms.CurseForge, LoadDependencies:=False))

#End Region

#Region "DlMod | Mod 镜像源请求"

    Public Function DlModRequest(Url As String, Optional Method As HttpMethod = Nothing,
                                 Optional Content As String = Nothing, Optional ContentType As String = Nothing) As JToken
        Dim Urls As New List(Of (Url As String, Timeout As Integer))
        Dim McimUrl As String = DlSourceModGet(Url)
        If McimUrl <> Url Then
            Select Case Settings.Get(Of Integer)("ToolDownloadMod")
                Case 0
                    Urls.Add((McimUrl, 10))
                    Urls.Add((McimUrl, 10))
                    Urls.Add((Url, 30))
                Case 1
                    Urls.Add((Url, If(Url.Contains("modrinth"), 20, 10)))
                    Urls.Add((McimUrl, 10))
                    Urls.Add((Url, 30))
                    Urls.Add((McimUrl, 30))
                Case Else
                    Urls.Add((Url, If(Url.Contains("modrinth"), 20, 10)))
                    Urls.Add((Url, 30))
                    Urls.Add((McimUrl, 30))
            End Select
        Else
            Urls.Add((Url, If(Url.Contains("modrinth"), 20, 10)))
            Urls.Add((Url, 30))
        End If
        Dim Errors As New List(Of Exception)
        For Each Source In Urls
            Try
                Return NetRequestByClient(Source.Url, Method, Content, ContentType, Timeout:=Source.Timeout * 1000, Encoding:=Encoding.UTF8, RequireJson:=True).DeserializeJson()
            Catch ex As Exception
                Errors.Add(ex)
            End Try
        Next
        Throw New AggregateException(Errors)
    End Function

#End Region

#Region "DlSource | 镜像下载源"

    Private DlPreferMojang As Boolean = False
    Private DlProbeTime As Long = 0
    Private DlProbeStarted As Boolean = False

    Private Sub DlProbeSourceSpeed()

        If DlProbeTime > 0 AndAlso GetTimeMs() - DlProbeTime < 600000 Then Return
        DlProbeTime = GetTimeMs()

        Const ProbePath As String = "/com/google/guava/guava/31.1-jre/guava-31.1-jre.pom"

        Dim OfficialMs As Long = Math.Min(DlProbeLatency("https://libraries.minecraft.net" & ProbePath),
                                          DlProbeLatency("https://libraries.minecraft.net" & ProbePath))
        Dim MirrorMs As Long = Math.Min(DlProbeLatency("https://bmclapi2.bangbang93.com/libraries" & ProbePath),
                                        DlProbeLatency("https://bmclapi2.bangbang93.com/libraries" & ProbePath))

        DlPreferMojang = OfficialMs > 0 AndAlso OfficialMs < 1200 AndAlso
                         MirrorMs > 0 AndAlso OfficialMs * 2 < MirrorMs
        Logger.Info($"下载源测速：官方源 {(If(OfficialMs > 0, OfficialMs & "ms", "不可用"))}，镜像源 {(If(MirrorMs > 0, MirrorMs & "ms", "不可用"))}，" &
                    If(DlPreferMojang, "优先使用官方源", "优先使用 BMCLAPI 镜像源"))
    End Sub

    Private Function DlProbeLatency(Url As String) As Long
        Dim Start As Long = GetTimeMs()
        Try
            NetRequestByClient(Url, Timeout:=6000, MakeLog:=False)
            Return GetTimeMs() - Start
        Catch
            Return -1
        End Try
    End Function

    Public ReadOnly Property DlSourcePreferMojang As Boolean
        Get
            Return Settings.Get(Of Integer)("ToolDownloadSource") = 2 OrElse (Settings.Get(Of Integer)("ToolDownloadSource") = 1 AndAlso DlPreferMojang)
        End Get
    End Property

    Public Function DlSourceOrder(OfficialUrls As IEnumerable(Of String), MirrorUrls As IEnumerable(Of String)) As IEnumerable(Of String)

        If Not DlProbeStarted Then
            DlProbeStarted = True
            RunInNewThread(Sub() DlProbeSourceSpeed(), "下载源测速")
        End If
        Return If(DlSourcePreferMojang, OfficialUrls.Union(MirrorUrls), MirrorUrls.Union(OfficialUrls))
    End Function

    Public ReadOnly Property DlVersionListPreferMojang As Boolean
        Get
            Return Settings.Get(Of Integer)("ToolDownloadVersion") = 2 OrElse (Settings.Get(Of Integer)("ToolDownloadVersion") = 1 AndAlso DlPreferMojang)
        End Get
    End Property

    Public Function DlVersionListOrder(OfficialUrls As IEnumerable(Of String), MirrorUrls As IEnumerable(Of String)) As IEnumerable(Of String)
        Return If(DlVersionListPreferMojang, OfficialUrls.Union(MirrorUrls), MirrorUrls.Union(OfficialUrls))
    End Function

    Public Function DlSourceAssetsGet(Original As String) As IEnumerable(Of String)
        Original = Original.Replace("http://resources.download.minecraft.net", "https://resources.download.minecraft.net")
        Return DlSourceOrder(
            {Original},
            {Original.
                Replace("https://piston-data.mojang.com", "https://bmclapi2.bangbang93.com/assets").
                Replace("https://piston-meta.mojang.com", "https://bmclapi2.bangbang93.com/assets").
                Replace("https://resources.download.minecraft.net", "https://bmclapi2.bangbang93.com/assets")
            })
    End Function

    Public Function DlSourceLibraryGet(Original As String) As IEnumerable(Of String)
        If {"minecraftforge", "fabricmc", "neoforged"}.Any(Function(k) Original.Contains(k)) Then
            Return {
                Original.
                    Replace("https://piston-data.mojang.com", "https://bmclapi2.bangbang93.com/maven").
                    Replace("https://piston-meta.mojang.com", "https://bmclapi2.bangbang93.com/maven").
                    Replace("https://libraries.minecraft.net", "https://bmclapi2.bangbang93.com/maven"),
                Original.
                    Replace("https://piston-data.mojang.com", "https://bmclapi2.bangbang93.com/libraries").
                    Replace("https://piston-meta.mojang.com", "https://bmclapi2.bangbang93.com/libraries").
                    Replace("https://libraries.minecraft.net", "https://bmclapi2.bangbang93.com/libraries")
            }
        Else
            Return DlSourceOrder(
                {Original},
                {Original.
                    Replace("https://piston-data.mojang.com", "https://bmclapi2.bangbang93.com/maven").
                    Replace("https://piston-meta.mojang.com", "https://bmclapi2.bangbang93.com/maven").
                    Replace("https://libraries.minecraft.net", "https://bmclapi2.bangbang93.com/maven"),
                 Original.
                    Replace("https://piston-data.mojang.com", "https://bmclapi2.bangbang93.com/libraries").
                    Replace("https://piston-meta.mojang.com", "https://bmclapi2.bangbang93.com/libraries").
                    Replace("https://libraries.minecraft.net", "https://bmclapi2.bangbang93.com/libraries")
                })
        End If
    End Function

    Public Function DlSourceLauncherOrMetaGet(Original As String) As IEnumerable(Of String)
        If Original Is Nothing Then Throw New Exception("无对应的 json 下载地址")
        Return DlSourceOrder(
            {Original},
            {Original.
                Replace("https://piston-data.mojang.com", "https://bmclapi2.bangbang93.com").
                Replace("https://piston-meta.mojang.com", "https://bmclapi2.bangbang93.com").
                Replace("https://launcher.mojang.com", "https://bmclapi2.bangbang93.com").
                Replace("https://launchermeta.mojang.com", "https://bmclapi2.bangbang93.com")
            })
    End Function

    Public Function DlSourceModGet(Original As String) As String
        Return Original.
            Replace("api.modrinth.com", "mod.mcimirror.top/modrinth").
            Replace("staging-api.modrinth.com", "mod.mcimirror.top/modrinth").
            Replace("cdn.modrinth.com", "mod.mcimirror.top").
            Replace("api.curseforge.com", "mod.mcimirror.top/curseforge").
            Replace("edge.forgecdn.net", "mod.mcimirror.top").
            Replace("mediafilez.forgecdn.net", "mod.mcimirror.top").
            Replace("media.forgecdn.net", "mod.mcimirror.top")
    End Function

    Private Sub DlSourceLoader(Of InputType, OutputType)(MainLoader As LoaderTask(Of InputType, OutputType),
                                                         LoaderList As List(Of KeyValuePair(Of LoaderTask(Of InputType, OutputType), Integer)),
                                                         Optional IsForceRestart As Boolean = False)
        Dim WaitCycle As Integer = 0
        Do While True

            Dim BeforeLoadersAllFailed As Boolean = True
            For Each SubLoader In LoaderList
                If WaitCycle = 0 Then
                    If IsForceRestart Then Continue For
                    If (SubLoader.Key.Input Is Nothing Xor MainLoader.Input Is Nothing) OrElse
                       (SubLoader.Key.Input IsNot Nothing AndAlso Not SubLoader.Key.Input.Equals(MainLoader.Input)) Then Continue For
                End If
                If SubLoader.Key.State <> LoadState.Failed Then BeforeLoadersAllFailed = False
                If SubLoader.Key.State = LoadState.Finished Then

                    MainLoader.Output = SubLoader.Key.Output
                    DlSourceLoaderCancel(LoaderList)
                    Return
                ElseIf BeforeLoadersAllFailed Then

                    If WaitCycle < SubLoader.Value * 100 Then WaitCycle = SubLoader.Value * 100
                End If
            Next

            If WaitCycle = 0 Then
                LoaderList.First.Key.Start(MainLoader.Input, IsForceRestart)
                For Each Loader In LoaderList.Skip(1)
                    Loader.Key.State = LoadState.Waiting
                Next
            End If

            For i = 0 To LoaderList.Count - 1
                If WaitCycle <> LoaderList(i).Value * 100 Then Continue For
                If i < LoaderList.Count - 1 AndAlso Not LoaderList.All(Function(l) l.Key.State = LoadState.Failed) Then

                    LoaderList(i + 1).Key.Start(MainLoader.Input, IsForceRestart)
                Else

                    Dim ErrorInfo As Exception = Nothing
                    For ii = 0 To LoaderList.Count - 1
                        LoaderList(ii).Key.Input = Nothing
                        If LoaderList(ii).Key.Error IsNot Nothing Then
                            If ErrorInfo Is Nothing OrElse LoaderList(ii).Key.Error.Message = "无" Then
                                ErrorInfo = LoaderList(ii).Key.Error
                            End If
                        End If
                    Next
                    If ErrorInfo Is Nothing Then ErrorInfo = New TimeoutException("下载源连接超时")
                    DlSourceLoaderCancel(LoaderList)
                    Throw ErrorInfo
                End If
                Exit For
            Next

            Thread.Sleep(10)
            WaitCycle += 1

            If MainLoader.IsCanceled Then
                DlSourceLoaderCancel(LoaderList)
                Return
            End If
        Loop
    End Sub
    Private Sub DlSourceLoaderCancel(Of InputType, OutputType)(LoaderList As List(Of KeyValuePair(Of LoaderTask(Of InputType, OutputType), Integer)))
        For Each Loader In LoaderList
            If Loader.Key.State = LoadState.Loading Then Loader.Key.Cancel()
        Next
    End Sub

#End Region

End Module
