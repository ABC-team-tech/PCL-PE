Public Class ResourceVersion

    Public Id As String

    Public ResourceType As ResourceTypes

    Public Display As String

    Public ReleaseDate As Date

    Public DownloadCount As Integer

    Public Version As String

    Public ModLoaders As ModLoaders = ModLoaders.None

    Public GameVersions As List(Of String)

    Public ReleaseType As ReleaseTypes

    Public ReadOnly Property ReleaseTypeDisplay As String
        Get
            Select Case ReleaseType
                Case ReleaseTypes.Release
                    Return "正式版"
                Case ReleaseTypes.Beta
                    Return If(ModeDebug, "Beta 版", "测试版")
                Case Else
                    Return If(ModeDebug, "Alpha 版", "早期测试版")
            End Select
        End Get
    End Property
    Public Enum ReleaseTypes

        Release = 1
        Beta = 2
        Alpha = 3
    End Enum

    Public ReadOnly Property DownloadAvailable As Boolean
        Get
            Return FileName IsNot Nothing AndAlso DownloadUrls IsNot Nothing
        End Get
    End Property

    Public FileName As String = Nothing

    Public DownloadUrls As List(Of String)

    Public Hash As String = Nothing

    Public Size As Integer = -1

    Private RawDependencies As New List(Of String)

    Public Dependencies As New List(Of String)

    Public Shared Function FromPlatformJson(Data As JObject, ExpectedResourceType As ResourceTypes) As ResourceVersion
        Dim Result As New ResourceVersion
        With Result
            .ResourceType = ExpectedResourceType
            If Data.ContainsKey("gameId") Then
#Region "CurseForge"

                .Id = Data("id")
                .Display = Data("displayName").ToString.Replace("	", "").Trim(" ")
                .Version = Nothing
                .ReleaseDate = Data("fileDate")
                .ReleaseType = CType(Data("releaseType").ToObject(Of Integer), ReleaseTypes)
                .DownloadCount = Data("downloadCount")
                .FileName = Data("fileName")
                .Size = Data("fileLength")
                If .Size = 0 Then .Size = -1
                .Hash = CType(Data("hashes"), JArray).ToList.FirstOrDefault(Function(s) s("algo").ToObject(Of Integer) = 1)?("value")?.ToString().Lower()
                If .Hash Is Nothing Then .Hash = CType(Data("hashes"), JArray).ToList.FirstOrDefault(Function(s) s("algo").ToObject(Of Integer) = 2)?("value")?.ToString().Lower()

                Dim Url = Data("downloadUrl").ToString
                If Url = "" Then Url = $"https://edge.forgecdn.net/files/{CInt(.Id.ToString.Substring(0, 4))}/{CInt(.Id.ToString.Substring(4))}/{ .FileName}"
                .DownloadUrls = ParseCurseForgeDownloadUrls(Url.Replace(.FileName, StringUtils.UrlEscape(.FileName)))
                .DownloadUrls.Add(DlSourceModGet(Url))
                .DownloadUrls = .DownloadUrls.Distinct.ToList

                If Data.ContainsKey("dependencies") Then
                    .RawDependencies = Data("dependencies").
                            Where(Function(d) d("relationType").ToObject(Of Integer) = 3 AndAlso
                                              d("modId").ToObject(Of Integer) <> 306612 AndAlso d("modId").ToObject(Of Integer) <> 634179).
                            Select(Function(d) d("modId").ToString).ToList
                End If

                Dim RawVersions As List(Of String) = Data("gameVersions").Select(Function(t) t.ToString.Trim.Lower).ToList
                .GameVersions = RawVersions.
                        Where(Function(v) McVersion.IsStandardFormat(v)).
                        Select(Function(v) v.Replace("-snapshot", " 预览版")).
                        Distinct.ToList
                If .GameVersions.IsSingle Then
                    .GameVersions = .GameVersions.ToList
                ElseIf .GameVersions.Count > 1 Then
                    .GameVersions = .GameVersions.SortByComparison(AddressOf McUtils.CompareVersionGE).ToList
                    If .ResourceType = ResourceTypes.ModPack Then .GameVersions = { .GameVersions(0)}.ToList
                Else
                    .GameVersions = New List(Of String) From {"未知版本"}
                End If

                .ModLoaders = ModLoaders.None
                For Each Loader In EnumUtils.GetAllFlags(Of ModLoaders)()
                    If RawVersions.Contains(Loader.ToString.Lower) Then .ModLoaders = .ModLoaders Or Loader
                Next
#End Region
            Else
#Region "Modrinth"

                .Id = Data("id")
                .Display = Data("name").ToString.Replace("	", "").Trim(" ")
                .Version = Data("version_number")
                .ReleaseDate = Data("date_published")
                .ReleaseType = If(Data("version_type").ToString = "release", ReleaseTypes.Release, If(Data("version_type").ToString = "beta", ReleaseTypes.Beta, ReleaseTypes.Alpha))
                .DownloadCount = Data("downloads")
                If CType(Data("files"), JArray).Any() Then
                    Dim File As JToken = Data("files")(0)
                    .FileName = File("filename")
                    .DownloadUrls = New List(Of String) From {File("url"), DlSourceModGet(File("url"))}.Distinct.ToList
                    .Size = File("size")
                    .Hash = File("hashes")("sha1")?.ToString.Lower()
                End If

                Dim RawLoaders As List(Of String) = Data("loaders").Select(Function(v) v.ToString).ToList
                .ModLoaders = ModLoaders.None
                For Each Loader As ModLoaders In EnumUtils.GetAllFlags(Of ModLoaders)()
                    If RawLoaders.Contains(Loader.ToString.Lower) Then .ModLoaders = .ModLoaders Or Loader
                Next
                If .ResourceType.HasFlagF(ResourceTypes.Mod) OrElse .ResourceType.HasFlagF(ResourceTypes.DataPack) Then
                    If RawLoaders.Intersect({"bukkit", "folia", "paper", "purpur", "spigot"}).Any() Then .ResourceType = ResourceTypes.Plugin
                    If RawLoaders.Contains("datapack") Then .ResourceType = ResourceTypes.DataPack
                    If .ModLoaders.Flags().Any() Then .ResourceType = ResourceTypes.Mod
                Else

                End If

                If Data.ContainsKey("dependencies") Then
                    .RawDependencies = Data("dependencies").
                            Where(Function(d) d("dependency_type") = "required" AndAlso
                                              d("project_id") <> "P7dR8mSH" AndAlso d("project_id") <> "qvIfYCYJ" AndAlso
                                              d("project_id").ToString.Length > 0).
                            Select(Function(d) d("project_id").ToString).ToList
                End If

                Dim RawVersions As List(Of String) = Data("game_versions").Select(Function(t) t.ToString.Trim.Lower).ToList
                .GameVersions = RawVersions.Where(Function(v) v.Contains(".")).
                                               Select(Function(v) If(v.Contains("-"), v.BeforeFirst("-") & " 预览版", If(v.StartsWithF("b1."), "远古版本", v))).
                                               Distinct.ToList
                If .GameVersions.IsSingle Then

                ElseIf .GameVersions.Count > 1 Then
                    .GameVersions = .GameVersions.SortByComparison(AddressOf McUtils.CompareVersionGE).ToList
                    If .ResourceType = ResourceTypes.ModPack Then .GameVersions = New List(Of String) From { .GameVersions(0)}
                ElseIf RawVersions.Any(Function(v) v.RegexCheck("[0-9]{2}w[0-9]{2}[a-z]")) Then
                    .GameVersions = RawVersions.Where(Function(v) v.RegexCheck("[0-9]{2}w[0-9]{2}[a-z]")).ToList
                Else
                    .GameVersions = New List(Of String) From {"未知版本"}
                End If
#End Region
            End If
        End With
        Return Result
    End Function

    Public Shared Function FromProjectId(ProjectId As String, Platform As ResourcePlatforms, Optional LoadDependencies As Boolean = True) As List(Of ResourceVersion)
        If Not Platform.Flags.IsSingle Then Throw New ArgumentException($"必须指定单一平台，当前值为 {Platform}")
        Dim TargetProject = ResourceProject.FromProjectId(ProjectId, Platform)

        If Not ProjectFilesCache.ContainsKey(ProjectId) Then
            Logger.Info($"开始获取项目版本列表：{ProjectId}")
            Dim ResultJsonArray As JArray = Nothing
            If Platform = ResourcePlatforms.CurseForge Then
                For RetryCount As Integer = 0 To 2
                    Dim ResultJson As JObject = DlModRequest($"https://api.curseforge.com/v1/mods/{ProjectId}/files?pageSize=" &
                    (10000 + RetryCount))

                    If ResultJson("pagination")("resultCount").ToObject(Of Integer) = ResultJson("pagination")("totalCount").ToObject(Of Integer) Then
                        ResultJsonArray = ResultJson("data")
                        Exit For
                    ElseIf RetryCount < 2 Then
                        Logger.Warn($"CurseForge 返回的资源版本列表存在缺失，即将进行第 {RetryCount + 1} 次重试")
                        Logger.Info($"返回的原始内容如下：{vbCrLf}{ResultJson}")
                    Else
                        Logger.Info($"CurseForge 返回的资源版本列表存在缺失，返回的原始内容如下：{vbCrLf}{ResultJson}")
                        Throw New Exception("CurseForge 返回的资源版本列表存在缺失")
                    End If
                Next
            Else
                ResultJsonArray = DlModRequest($"https://api.modrinth.com/v2/project/{ProjectId}/version?include_changelog=false")
            End If
            ProjectFilesCache(ProjectId) = ResultJsonArray.Select(Function(a) FromPlatformJson(a, TargetProject.Types)).
                Where(Function(a) a.DownloadAvailable).
                DistinctBy(Function(a) a.Id).ToList
        End If
        Dim Result As List(Of ResourceVersion) = ProjectFilesCache(ProjectId)
        If Not LoadDependencies Then Return Result

        Dim Dependencies As List(Of String) = Result.SelectMany(Function(f) f.RawDependencies).Distinct().ToList
        If Dependencies.Any Then
            Logger.Info($"项目 {ProjectId} 中需要加载的前置：{Dependencies.Join("，")}")
            For Each DependencyProject In ResourceProject.FromProjectIds(Dependencies, TargetProject.Platform)
                For Each File In Result
                    If File.RawDependencies.Contains(DependencyProject.Id) AndAlso DependencyProject.Id <> ProjectId AndAlso Not File.Dependencies.Contains(DependencyProject.Id) Then
                        File.Dependencies.Add(DependencyProject.Id)
                    End If
                Next
            Next
        End If
        Return Result
    End Function

    Protected Friend Shared ProjectFilesCache As New Dictionary(Of String, List(Of ResourceVersion))

    Public Shared Function FromCacheJson(Data As JObject) As ResourceVersion
        Dim Result As New ResourceVersion
        With Result
            .ResourceType = Data("ResourceType").ToObject(Of ResourceTypes)
            .Id = Data("Id").ToString
            .Display = Data("DisplayName").ToString
            If Data.ContainsKey("Version") Then .Version = Data("Version").ToString
            .ReleaseDate = Data("ReleaseDate").ToObject(Of Date)
            .DownloadCount = Data("DownloadCount").ToObject(Of Integer)
            .ReleaseType = Data("ReleaseType").ToObject(Of ReleaseTypes)
            If Data.ContainsKey("FileName") Then .FileName = Data("FileName").ToString
            If Data.ContainsKey("DownloadUrls") Then .DownloadUrls = Data("DownloadUrls").ToObject(Of List(Of String))
            If Data.ContainsKey("ModLoaders") Then .ModLoaders = Data("ModLoaders").ToObject(Of ModLoaders)
            If Data.ContainsKey("Size") Then .Size = Data("Size").ToObject(Of Integer)
            If Data.ContainsKey("Hash") Then .Hash = Data("Hash").ToString
            If Data.ContainsKey("GameVersions") Then .GameVersions = Data("GameVersions").ToObject(Of List(Of String))
            If Data.ContainsKey("RawDependencies") Then .RawDependencies = Data("RawDependencies").ToObject(Of List(Of String))
            If Data.ContainsKey("Dependencies") Then .Dependencies = Data("Dependencies").ToObject(Of List(Of String))
        End With
        Return Result
    End Function

    Public Function ToCacheJson() As JObject
        Dim Result As New JObject
        With Result
            .Add("ResourceType", CInt(ResourceType))
            .Add("Id", Id)
            If Version IsNot Nothing Then .Add("Version", Version)
            .Add("DisplayName", Display)
            .Add("ReleaseDate", ReleaseDate)
            .Add("DownloadCount", DownloadCount)
            .Add("ModLoaders", CInt(ModLoaders))
            .Add("GameVersions", New JArray(GameVersions))
            .Add("ReleaseType", CInt(ReleaseType))
            If FileName IsNot Nothing Then .Add("FileName", FileName)
            If DownloadUrls IsNot Nothing Then .Add("DownloadUrls", New JArray(DownloadUrls))
            If Hash IsNot Nothing Then .Add("Hash", Hash)
            If Size >= 0 Then .Add("Size", Size)
            .Add("RawDependencies", New JArray(RawDependencies))
            .Add("Dependencies", New JArray(Dependencies))
        End With
        Return Result
    End Function

    Public Function ToListItem(ClickHandler As MyListItem.ClickEventHandler, Optional SaveAsButtonHandler As MyIconButton.ClickEventHandler = Nothing,
                               Optional IsBadDisplay As Boolean = False) As MyVirtualizingElement(Of MyListItem)
        Return New MyVirtualizingElement(Of MyListItem)(
        Function()
            Dim Result As New MyListItem
            With Result

                .SnapsToDevicePixels = True
                .Height = 42
                .Type = MyListItem.CheckType.Clickable
                .Tag = Me
                .Title = If(IsBadDisplay, FileName, Display).BeforeLastOfAny({".zip", ".jar", ".mrpack", ".litemod"}, True)
                .Logo = $"{PathImage}ReleaseTypes/{ReleaseType}.png"
                AddHandler .Click, ClickHandler

                .Info =
                Iterator Function()
                    If ModLoaders <> ModLoaders.None Then Yield ModLoaders.Flags.Join("、"c)
                    If .Title <> PathUtils.GetFileNameWithoutExtension(FileName) Then Yield PathUtils.GetFileNameWithoutExtension(FileName)
                    If Dependencies.Any Then Yield $"{Dependencies.Count} 项前置"
                    If GameVersions.All(Function(v) Not v.Contains(".") OrElse {"w", "snapshot", "rc", "pre", "experimental", "-"}.Any(Function(s) v.ContainsIgnoreCase(s))) Then Yield $"游戏版本 {GameVersions.Join("、"c)}"
                    If DownloadCount > 0 Then Yield $"下载 {If(DownloadCount > 100000, Math.Round(DownloadCount / 10000) & " 万", DownloadCount.ToString() + " ")}次"
                    Yield $"更新于 {StringUtils.FormatTimeSpan(ReleaseDate - Date.Now, False)}"
                    If ReleaseType <> ReleaseTypes.Release Then Yield ReleaseTypeDisplay
                End Function().Join("，"c)

                If SaveAsButtonHandler IsNot Nothing Then
                    Dim BtnSave As New MyIconButton With {.Logo = Logo.IconButtonSave, .ToolTip = "另存为"}
                    AddHandler BtnSave.Click, SaveAsButtonHandler
                    .Buttons = {BtnSave}
                End If

            End With
            Return Result
        End Function) With {.Height = 42}
    End Function

    Public Function ToNetFile(LocalFileOrFolder As String,
            Optional TrackDownloadReason As DownloadReason? = Nothing, Optional TrackGameVersion As String = Nothing, Optional TrackLoader As ModLoaders = ModLoaders.None) As NetFile
        Return New NetFile(
            ParseModrinthTrackArguments(DownloadUrls, TrackDownloadReason, TrackGameVersion, TrackLoader),
            LocalFileOrFolder & If(LocalFileOrFolder.EndsWithF("\"), FileName, ""),
            New FileChecker With {.Hash = Hash, .ActualSize = Size},
            SimulateBrowserHeaders:=True)
    End Function

    Public Shared Iterator Function ParseModrinthTrackArguments(Urls As List(Of String),
            Optional TrackDownloadReason As DownloadReason? = Nothing, Optional TrackGameVersion As String = Nothing, Optional TrackLoader As ModLoaders = ModLoaders.None) As IEnumerable(Of String)
        Dim Arguments As New Dictionary(Of String, String)
        If TrackDownloadReason IsNot Nothing Then Arguments.Add("mr_download_reason", TrackDownloadReason.ToString.Lower)
        If TrackGameVersion IsNot Nothing Then Arguments.Add("mr_game_version", TrackGameVersion)
        If TrackLoader.Flags.Any Then Arguments.Add("mr_loader", TrackLoader.Flags.First.ToString.Lower)
        Dim Argument = Arguments.Select(Function(a) $"{a.Key}={a.Value}").Join("&")
        For Each Url In Urls
            If Not Url.ContainsIgnoreCase("cdn.modrinth.com/") Then Yield Url
            Yield $"{Url}{If(Url.Contains("?"), "&", "?")}{Argument}"
        Next
    End Function
    Public Enum DownloadReason
        Standalone
        Dependency
        ModPack
        Update
    End Enum

    Public Shared Function ParseCurseForgeDownloadUrls(Url As String) As List(Of String)
        Return {
            Url.Replace("-service.overwolf.wtf", ".forgecdn.net").Replace("://edge.", "://mediafilez.").Replace("://media.", "://mediafilez."),
            Url.Replace("://edge.", "://mediafilez.").Replace("://media.", "://mediafilez."),
            Url.Replace("-service.overwolf.wtf", ".forgecdn.net"),
            Url.Replace("://media.", "://edge."),
            Url
        }.Distinct.ToList
    End Function
    Public Overrides Function ToString() As String
        Return $"{Id}: {FileName}"
    End Function

End Class
