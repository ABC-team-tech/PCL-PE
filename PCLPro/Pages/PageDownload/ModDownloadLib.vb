Public Module ModDownloadLib

#Region "Minecraft 下载"

    Private Function McDownloadClientLoader(Id As String, JsonUrl As String, VersionFolder As String) As List(Of LoaderBase)
        Dim InstanceName = PathUtils.GetLastPart(VersionFolder)
        Dim Loaders As New List(Of LoaderBase)
        Dim NewInstance As McInstance = Nothing

        If JsonUrl Is Nothing Then
            Loaders.Add(New LoaderTask(Of String, List(Of NetFile))("获取原版 json 文件下载地址",
            Sub(Task As LoaderTask(Of String, List(Of NetFile)))
                Dim JsonAddress As String = DlClientListGet(Id)
                Task.Output = New List(Of NetFile) From {New NetFile(DlSourceLauncherOrMetaGet(JsonAddress), VersionFolder & InstanceName & ".json")}
            End Sub) With {.ProgressWeight = 2, .Show = False})
        End If
        Loaders.Add(New LoaderDownload(McDownloadClientJsonName, New List(Of NetFile) From {
            New NetFile(DlSourceLauncherOrMetaGet(If(JsonUrl, "")), VersionFolder & InstanceName & ".json", New FileChecker With {.IsJson = True})
        }) With {.ProgressWeight = 3})

        Loaders.Add(New LoaderTask(Of String, String)("读取 json 文件",
            Sub(Task As LoaderTask(Of String, String))
                NewInstance = New McInstance(VersionFolder)
                NewInstance.Load()
            End Sub) With {.ProgressWeight = 0, .Show = False})

        Dim LoadersLib As New List(Of LoaderBase)
        LoadersLib.Add(New LoaderTask(Of String, List(Of NetFile))("分析原版支持库文件（副加载器）",
        Sub(Task As LoaderTask(Of String, List(Of NetFile)))
            Logger.Info($"开始分析原版支持库文件：{VersionFolder}")
            Task.Output = McLibNetFilesFromInstance(NewInstance)
        End Sub) With {.ProgressWeight = 1, .Show = False})
        LoadersLib.Add(New LoaderDownload("下载原版支持库文件（副加载器）", New List(Of NetFile)) With {.ProgressWeight = 13, .Show = False})
        Loaders.Add(New LoaderCombo(Of String)(McDownloadClientLibName, LoadersLib) With {.Block = False, .ProgressWeight = 14})

        Dim LoadersAssets As New List(Of LoaderBase)
        LoadersAssets.Add(New LoaderTask(Of String, List(Of NetFile))("分析资源文件索引地址（副加载器）",
        Sub(Task As LoaderTask(Of String, List(Of NetFile)))
            Try
                Dim AssetIndex = DlClientAssetIndexGet(NewInstance)
                Task.Output = If(AssetIndex Is Nothing, New List(Of NetFile), New List(Of NetFile) From {AssetIndex})
            Catch ex As Exception
                Throw New Exception("分析资源文件索引地址失败", ex)
            End Try

            Try
                Dim InstanceJson As JObject = FileUtils.ReadAsJson(VersionFolder & InstanceName & ".json")
                InstanceJson("clientVersion") = Id
                FileUtils.Write($"{VersionFolder}{InstanceName}.json", InstanceJson.ToString)
            Catch ex As Exception
                Throw New Exception("添加客户端版本失败", ex)
            End Try
        End Sub) With {.ProgressWeight = 1, .Show = False})
        LoadersAssets.Add(New LoaderDownload("下载资源文件索引（副加载器）", New List(Of NetFile)) With {.ProgressWeight = 3, .Show = False})
        LoadersAssets.Add(New LoaderTask(Of String, List(Of NetFile))("分析所需资源文件（副加载器）",
            Sub(Task) Task.Output = McAssetsFixList(NewInstance, True, Task)) With {.ProgressWeight = 0.01, .Show = False})
        LoadersAssets.Add(New LoaderDownload("下载资源文件（副加载器）", New List(Of NetFile)) With {.ProgressWeight = 14, .Show = False})
        Loaders.Add(New LoaderCombo(Of String)("下载原版资源文件", LoadersAssets) With {.Block = False, .ProgressWeight = 18})

        Return Loaders

    End Function
    Private Const McDownloadClientLibName As String = "下载原版支持库文件"
    Private Const McDownloadClientJsonName As String = "下载原版 json 文件"

#End Region

#Region "Minecraft 下载菜单"

    Public Function McDownloadListItem(Entry As JObject, OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean) As MyVirtualizingElement(Of MyListItem)
        Return New MyVirtualizingElement(Of MyListItem)(
        Function()

            Dim Logo As String
            Select Case Entry("type")
                Case "release"
                    Logo = PathImage & "Blocks/Grass.png"
                Case "snapshot"
                    Logo = PathImage & "Blocks/CommandBlock.png"
                Case "special"
                    Logo = PathImage & "Blocks/GoldBlock.png"
                Case Else
                    Logo = PathImage & "Blocks/CobbleStone.png"
            End Select

            Dim NewItem As New MyListItem With {.Logo = Logo, .SnapsToDevicePixels = True, .Title = Entry("id").ToString, .Height = 42, .Type = MyListItem.CheckType.Clickable, .Tag = Entry}
            If Entry("lore") Is Nothing Then
                NewItem.Info = Entry("releaseTime").Value(Of Date).ToString("yyyy'/'MM'/'dd HH':'mm")
            Else
                NewItem.Info = Entry("lore").ToString
            End If
            If Entry("url").ToString.Contains("pcl") Then NewItem.Info = "[PCL 特供下载] " & NewItem.Info
            AddHandler NewItem.Click, OnClick

            If IsSaveOnly Then
                NewItem.ContentHandler = AddressOf McDownloadSaveMenuBuild
            Else
                NewItem.ContentHandler = AddressOf McDownloadMenuBuild
            End If

            Return NewItem
        End Function) With {.Height = 42}
    End Function
    Private Sub McDownloadSaveMenuBuild(sender As Object, e As EventArgs)
        Dim BtnInfo As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonInfo, .ToolTip = "更新日志"}
        AddHandler BtnInfo.Click, AddressOf McDownloadMenuLog
        Dim BtnServer As New MyIconButton With {.LogoScale = 1, .Logo = Logo.IconButtonServer, .ToolTip = "下载服务端"}
        AddHandler BtnServer.Click, AddressOf McDownloadMenuSaveServer
        sender.Buttons = {BtnServer, BtnInfo}
    End Sub
    Private Sub McDownloadMenuBuild(sender As Object, e As EventArgs)
        Dim BtnSave As New MyIconButton With {.Logo = Logo.IconButtonSave, .ToolTip = "另存为"}
        AddHandler BtnSave.Click, AddressOf McDownloadMenuSave
        Dim BtnInfo As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonInfo, .ToolTip = "更新日志"}
        AddHandler BtnInfo.Click, AddressOf McDownloadMenuLog
        Dim BtnServer As New MyIconButton With {.LogoScale = 1, .Logo = Logo.IconButtonServer, .ToolTip = "下载服务端"}
        AddHandler BtnServer.Click, AddressOf McDownloadMenuSaveServer
        sender.Buttons = {BtnSave, BtnInfo, BtnServer}
    End Sub
    Private Sub McDownloadMenuLog(sender As Object, e As RoutedEventArgs)
        Dim Version As JToken
        If sender.Tag IsNot Nothing Then
            Version = sender.Tag
        ElseIf sender.Parent.Tag IsNot Nothing Then
            Version = sender.Parent.Tag
        Else
            Version = sender.Parent.Parent.Tag
        End If
        McUpdateLogShow(Version)
    End Sub
    Private Sub McDownloadMenuSaveServer(sender As Object, e As RoutedEventArgs)
        Dim Version As MyListItem
        If TypeOf sender Is MyListItem Then
            Version = sender
        ElseIf TypeOf sender.Parent Is MyListItem Then
            Version = sender.Parent
        Else
            Version = sender.Parent.Parent
        End If
        Try
            Dim Id = Version.Title
            Dim JsonUrl = Version.Tag("url").ToString
            Dim VersionFolder As String = Dialogs.SelectFolder("选择目标文件夹", False).FirstOrDefault
            If VersionFolder Is Nothing Then Return
            VersionFolder = VersionFolder & Id & "\"

            For Each OngoingLoader In LoaderTaskbar.ToList()
                If OngoingLoader.Name <> $"Minecraft {Id} 服务端下载" Then Continue For
                Hint("该服务端正在下载中！", HintType.Red)
                Return
            Next

            Dim Loaders As New List(Of LoaderBase)

            Loaders.Add(New LoaderDownload("下载版本 JSON 文件", New List(Of NetFile) From {
                New NetFile(DlSourceLauncherOrMetaGet(JsonUrl), VersionFolder & Id & ".json", New FileChecker With {.IsJson = True})
            }) With {.ProgressWeight = 2})

            Loaders.Add(New LoaderTask(Of String, List(Of NetFile))("构建服务端",
            Sub(Task As LoaderTask(Of String, List(Of NetFile)))

                Dim Instance As New McInstance(VersionFolder)
                If Instance.JsonObject("downloads") Is Nothing OrElse Instance.JsonObject("downloads")("server") Is Nothing OrElse Instance.JsonObject("downloads")("server")("url") Is Nothing Then
                    FileUtils.Delete(VersionFolder & Id & ".json")
                    If Not DirectoryUtils.GetInfo(VersionFolder).GetFileSystemInfos.Any() Then DirectoryUtils.Delete(VersionFolder)
                    Task.Output = New List(Of NetFile)
                    Hint($"Mojang 没有给 Minecraft {Id} 提供官方服务端下载，没法下，撤退！", HintType.Red)
                    Thread.Sleep(2000)
                    Task.Cancel()
                    Return
                End If
                Dim JarUrl As String = Instance.JsonObject("downloads")("server")("url")
                Dim Checker As New FileChecker With {.MinSize = 1024, .ActualSize = If(Instance.JsonObject("downloads")("server")("size"), -1), .Hash = Instance.JsonObject("downloads")("server")("sha1")}
                Task.Output = New List(Of NetFile) From {New NetFile(DlSourceLauncherOrMetaGet(JarUrl), VersionFolder & Id & "-server.jar", Checker)}

                Dim Bat As String =
$"@echo off
title {Id} 原版服务端
echo 如果服务端立即停止，请右键编辑该脚本，将下一行开头的 java 替换为适合该 Minecraft 版本的完整 java.exe 的路径。
echo 你可以在 PCL 的 [设置 → 启动选项] 中查看已安装的 java，所需的 java.exe 一般在其中的 bin 文件夹下。
echo ------------------------------
echo 如果提示 ""You need to agree to the EULA in order to run the server""，请打开 eula.txt，按说明阅读并同意 Minecraft EULA 后，将该文件最后一行中的 eula=false 改为 eula=true。
echo ------------------------------
""java"" -server -XX:+UseG1GC -Xmx6144M -Xms1024M -jar {Id}-server.jar nogui
echo ----------------------
echo 服务端已停止。
pause"
                FileUtils.Write(VersionFolder & "Launch Server.bat", Bat,
                        encoding:=If(Encoding.Default.Equals(Encoding.UTF8), Encoding.UTF8, Encoding.GetEncoding("GB18030")))

                FileUtils.Delete(VersionFolder & Id & ".json")
            End Sub) With {.ProgressWeight = 0.5, .Show = False})

            Loaders.Add(New LoaderDownload("下载服务端文件", New List(Of NetFile)) With {.ProgressWeight = 5})

            Dim Loader As New LoaderCombo(Of String)("Minecraft " & Id & " 服务端下载", Loaders) With {.OnStateChanged = AddressOf LoaderStateChangedHintOnly}
            Loader.Start(Id)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()
        Catch ex As Exception
            Logger.Error(ex, "开始 Minecraft 服务端下载失败")
        End Try
    End Sub
    Public Sub McDownloadMenuSave(sender As Object, e As RoutedEventArgs)
        Dim Version As MyListItem
        If TypeOf sender Is MyListItem Then
            Version = sender
        ElseIf TypeOf sender.Parent Is MyListItem Then
            Version = sender.Parent
        Else
            Version = sender.Parent.Parent
        End If
        Try
            Dim Id = Version.Title
            Dim JsonUrl = Version.Tag("url").ToString
            Dim VersionFolder As String = Dialogs.SelectFolder("选择目标文件夹", False).FirstOrDefault
            If VersionFolder Is Nothing Then Return
            VersionFolder = VersionFolder & Id & "\"

            For Each OngoingLoader In LoaderTaskbar.ToList()
                If OngoingLoader.Name <> $"Minecraft {Id} 下载" Then Continue For
                Hint("该版本正在下载中！", HintType.Red)
                Return
            Next

            Dim Loaders As New List(Of LoaderBase)

            Loaders.Add(New LoaderDownload("下载版本 JSON 文件", New List(Of NetFile) From {
                New NetFile(DlSourceLauncherOrMetaGet(JsonUrl), VersionFolder & Id & ".json", New FileChecker With {.IsJson = True})
            }) With {.ProgressWeight = 2})

            Loaders.Add(New LoaderTask(Of String, List(Of NetFile))("分析版本 JAR 文件下载地址",
                Sub(Task) Task.Output = New List(Of NetFile) From {DlClientJarGet(New McInstance(VersionFolder), False)}
            ) With {.ProgressWeight = 0.5, .Show = False})

            Loaders.Add(New LoaderDownload("下载版本 JAR 文件", New List(Of NetFile)) With {.ProgressWeight = 5})

            Dim Loader As New LoaderCombo(Of String)("Minecraft " & Id & " 下载", Loaders) With {.OnStateChanged = AddressOf LoaderStateChangedHintOnly}
            Loader.Start(Id)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()
        Catch ex As Exception
            Logger.Error(ex, "开始 Minecraft 下载失败")
        End Try
    End Sub

    Public Sub McUpdateLogShow(InstanceJson As JToken)
        Dim WikiName As String
        Dim Id As String = InstanceJson("id").ToString.Lower
        If Id = "3d shareware v1.34" Then
            WikiName = "3D_Shareware_v1.34"
        ElseIf Id = "2.0" Then
            WikiName = "Java版2.0"
        ElseIf Id = "1.rv-pre1" Then
            WikiName = "Java版1.RV-Pre1"
        ElseIf Id = "combat test 1" OrElse Id.Contains("combat-1") OrElse Id.Contains("combat-212796") Then
            WikiName = "Java版1.14.3_-_Combat_Test"
        ElseIf Id = "combat test 2" OrElse Id.Contains("combat-2") OrElse Id.Contains("combat-0") Then
            WikiName = "Java版Combat_Test_2"
        ElseIf Id = "combat test 3" OrElse Id = "1.14_combat-3" Then
            WikiName = "Java版Combat_Test_3"
        ElseIf Id = "combat test 4" OrElse Id = "1.15_combat-1" Then
            WikiName = "Java版Combat_Test_4"
        ElseIf Id = "combat test 5" OrElse Id = "1.15_combat-6" Then
            WikiName = "Java版Combat_Test_5"
        ElseIf Id = "combat test 6" OrElse Id = "1.16_combat-0" Then
            WikiName = "Java版Combat_Test_6"
        ElseIf Id = "combat test 7c" OrElse Id = "1.16_combat-3" Then
            WikiName = "Java版Combat_Test_7c"
        ElseIf Id = "combat test 8b" OrElse Id = "1.16_combat-5" Then
            WikiName = "Java版Combat_Test_8b"
        ElseIf Id = "combat test 8c" OrElse Id = "1.16_combat-6" Then
            WikiName = "Java版Combat_Test_8c"
        ElseIf Id = "1.0.0-rc2-2" Then
            WikiName = "Java版RC2"
        ElseIf Id.StartsWithF("1.19_deep_dark_experimental_snapshot-") OrElse Id.StartsWithF("1_19_deep_dark_experimental_snapshot-") Then
            WikiName = Id.Replace("1_19", "1.19").Replace("1.19_deep_dark_experimental_snapshot-", "Java版Deep_Dark_Experimental_Snapshot_")
        ElseIf Id = "b1.9-pre6" Then
            WikiName = "Java版Beta_1.9_Prerelease_6"
        ElseIf Id.Contains("b1.9") Then
            WikiName = "Java版Beta_1.9_Prerelease"
        ElseIf InstanceJson("type") = "release" OrElse InstanceJson("type") = "snapshot" OrElse InstanceJson("type") = "special" Then
            WikiName = If(Id.Contains("w"), "", "Java版") & Id.Replace(" Pre-Release ", "-pre")
        ElseIf Id.StartsWithF("b") Then
            WikiName = "Java版" & Id.TrimEnd("a", "b", "c", "d", "e").Replace("b", "Beta_")
        ElseIf Id.StartsWithF("a") Then
            WikiName = "Java版" & Id.TrimEnd("a", "b", "c", "d", "e").Replace("a", "Alpha_v")
        ElseIf Id = "inf-20100618" Then
            WikiName = "Java版Infdev_20100618"
        ElseIf Id = "c0.30_01c" OrElse Id = "c0.30_survival" OrElse Id.Contains("生存测试") Then
            WikiName = "Java版Classic_0.30（生存模式）"
        ElseIf Id.StartsWithF("c0.31") Then
            WikiName = "Java版Indev_0.31_20100130"
        ElseIf Id.StartsWithF("c") Then
            WikiName = "Java版" & Id.Replace("c", "Classic_")
        ElseIf Id.StartsWithF("rd-") Then
            WikiName = "Java版Pre-classic_" & Id
        Else
            Logger.Error($"未知的版本格式：{Id}。")
            Return
        End If
        OpenWebsite("https://zh.minecraft.wiki/w/" & WikiName.Replace("_experimental-snapshot-", "-exp"))
    End Sub

#End Region

#Region "OptiFine 下载"

    Private Sub McDownloadOptiFineSave(DownloadInfo As DlOptiFineListEntry)
        Try
            Dim Target As String = Dialogs.SaveFile("选择保存位置", DownloadInfo.FileName, filter:={("jar", "OptiFine Jar")})
            If Target Is Nothing Then Return

            For Each OngoingLoader In LoaderTaskbar.ToList()
                If OngoingLoader.Name <> $"OptiFine {DownloadInfo.DisplayName} 下载" Then Continue For
                Hint("该版本正在下载中！", HintType.Red)
                Return
            Next

            Dim Loader As New LoaderCombo(Of DlOptiFineListEntry)("OptiFine " & DownloadInfo.DisplayName & " 下载", McDownloadOptiFineSaveLoader(DownloadInfo, Target)) With {.OnStateChanged = AddressOf LoaderStateChangedHintOnly}
            Loader.Start(DownloadInfo)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()

        Catch ex As Exception
            Logger.Error(ex, "开始 OptiFine 下载失败")
        End Try
    End Sub
    Private Sub McDownloadOptiFineInstall(BaseMcFolderHome As String, Target As String, Task As LoaderTask(Of List(Of NetFile), Boolean), UseJavaWrapper As Boolean)
        Dim JavaRange = ValueRange(Of Version).AtLeast(New Version(21, 0))
        Try
            Using Installer = FileUtils.OpenZip(Target)
                Dim InstallerClass = Installer.GetEntry("optifine/Installer.class")
                If InstallerClass Is Nothing Then Throw New FileNotFoundException("未找到 optifine/Installer.class")
                Using Stream = InstallerClass.Open(), Reader As New BinaryReader(Stream)
                    Dim Header = Reader.ReadBytes(8)
                    If Header.Length < 8 Then Throw New IOException("Installer.class 长度不足")
                    If Header(0) <> &HCA OrElse Header(1) <> &HFE OrElse Header(2) <> &HBA OrElse Header(3) <> &HBE Then Throw New InvalidDataException("Installer.class 文件头无效")
                    Dim ClassVersion = Header(6) * &H100 + Header(7)
                    If ClassVersion < 49 Then Throw New InvalidDataException("Installer.class 版本过低")
                    If ClassVersion > 100 Then Throw New InvalidDataException("Installer.class 版本过高")
                    JavaRange = ValueRange(Of Version).AtLeast(New Version(ClassVersion - 44, 0))
                    Logger.Info($"OptiFine 安装器 class 版本：{ClassVersion}，需要 Java {JavaRange}")
                End Using
            End Using
        Catch ex As Exception
            Logger.Warn(ex, "读取 OptiFine 安装器 Java 版本失败")
        End Try

        Dim Java As Java = SelectOrDownloadJava(JavaRange, True, Task.CreateCancellationToken, Task.CreateSyncProgressProvider(0, 0.2))
        If Task.IsCanceled Then Return
        If Java Is Nothing Then Throw New OperationCanceledException

        Dim Arguments As String
        If UseJavaWrapper AndAlso Not Settings.Get(Of Boolean)("LaunchAdvanceDisableJLW") Then
            Arguments = $"-Doolloo.jlw.tmpdir=""{PathUtils.RemoveSlashSuffix(PathPure.Value)}"" -Duser.home=""{PathUtils.RemoveSlashSuffix(BaseMcFolderHome)}"" -cp ""{Target}"" -jar ""{LaunchUtils.ExtractPatch("Launch\JavaWrapper.jar", PathPure.Value)}"" optifine.Installer"
        Else
            Arguments = $"-Duser.home=""{PathUtils.RemoveSlashSuffix(BaseMcFolderHome)}"" -cp ""{Target}"" optifine.Installer"
        End If
        If Java.Version.Major >= 9 Then Arguments = "--add-exports cpw.mods.bootstraplauncher/cpw.mods.bootstraplauncher=ALL-UNNAMED " & Arguments

        SyncLock InstallSyncLock
            Dim Info = New ProcessStartInfo With {
                .FileName = Java.JavaExePath,
                .Arguments = Arguments,
                .UseShellExecute = False,
                .CreateNoWindow = True,
                .RedirectStandardError = True,
                .RedirectStandardOutput = True,
                .WorkingDirectory = PathUtils.ToShortPath(BaseMcFolderHome)
            }
            If Info.EnvironmentVariables.ContainsKey("appdata") Then
                Info.EnvironmentVariables("appdata") = BaseMcFolderHome
            Else
                Info.EnvironmentVariables.Add("appdata", BaseMcFolderHome)
            End If
            Logger.Info($"开始安装 OptiFine：{Target}")
            Dim TotalLength As Integer = 0
            Dim process As New Process With {.StartInfo = Info}
            Dim LastResult As String = ""
            Using outputWaitHandle As New AutoResetEvent(False)
                Using errorWaitHandle As New AutoResetEvent(False)
                    AddHandler process.OutputDataReceived,
                    Function(sender, e)
                        Try
                            If e.Data Is Nothing Then
                                outputWaitHandle.[Set]()
                            Else
                                LastResult = e.Data
                                Logger.Trace(LastResult)
                                TotalLength += 1
                                Task.Progress += 0.7 / 7000
                            End If
                        Catch ex As ObjectDisposedException
                        Catch ex As Exception
                            Logger.Warn(ex, "读取 OptiFine 安装器信息失败")
                        End Try
                        Try
                            If Task.State = LoadState.Canceled AndAlso Not process.HasExited Then
                                Logger.Info("由于任务取消，已中止 OptiFine 安装")
                                process.Kill()
                            End If
                        Catch
                        End Try
                        Return Nothing
                    End Function
                    AddHandler process.ErrorDataReceived,
                    Function(sender, e)
                        Try
                            If e.Data Is Nothing Then
                                errorWaitHandle.[Set]()
                            Else
                                LastResult = e.Data
                                Logger.Trace(LastResult)
                                TotalLength += 1
                                Task.Progress += 0.7 / 7000
                            End If
                        Catch ex As ObjectDisposedException
                        Catch ex As Exception
                            Logger.Warn(ex, "读取 OptiFine 安装器错误信息失败")
                        End Try
                        Try
                            If Task.State = LoadState.Canceled AndAlso Not process.HasExited Then
                                Logger.Info("由于任务取消，已中止 OptiFine 安装")
                                process.Kill()
                            End If
                        Catch
                        End Try
                        Return Nothing
                    End Function
                    process.Start()
                    process.BeginOutputReadLine()
                    process.BeginErrorReadLine()

                    Do Until process.HasExited
                        Thread.Sleep(10)
                    Loop

                    outputWaitHandle.WaitOne(10000)
                    errorWaitHandle.WaitOne(10000)
                    process.Dispose()
                    If TotalLength < 1000 OrElse LastResult.Contains("at ") Then Throw New Exception("安装器运行出错，末行为 " & LastResult)
                End Using
            End Using
        End SyncLock
    End Sub

    Private Function McDownloadOptiFineLoader(DownloadInfo As DlOptiFineListEntry, McFolder As String, ClientDownloadLoader As LoaderCombo(Of String), ClientFolder As String) As List(Of LoaderBase)

        Dim Id As String = DownloadInfo.InstanceName
        Dim VersionFolder As String = McFolder & "versions\" & Id & "\"
        Dim IsNewerVersion As Boolean = DownloadInfo.Inherit.Contains("w") OrElse McVersion.VersionToDrop(DownloadInfo.Inherit) >= 140
        Dim Target As String = If(IsNewerVersion,
            $"{RequestTaskTempFolder(True)}OptiFine.jar",
            $"{McFolder}libraries\optifine\OptiFine\{PathUtils.GetFileNameWithoutExtension(DownloadInfo.FileName).Replace("OptiFine_", "").Replace("preview_", "")}\{DownloadInfo.FileName.Replace("OptiFine_", "OptiFine-").Replace("preview_", "")}")
        Dim Loaders As New List(Of LoaderBase)

        Loaders.Add(New LoaderTask(Of String, List(Of NetFile))("获取 OptiFine 主文件下载地址",
        Sub(Task As LoaderTask(Of String, List(Of NetFile)))
            Dim Sources As New List(Of String)

            Dim BmclapiInherit As String = DownloadInfo.Inherit
            If BmclapiInherit = "1.8" OrElse BmclapiInherit = "1.9" Then BmclapiInherit &= ".0"
            If DownloadInfo.IsPreview Then
                Sources.Add("https://bmclapi2.bangbang93.com/optifine/" & BmclapiInherit & "/HD_U_" & DownloadInfo.DisplayName.Replace(DownloadInfo.Inherit & " ", "").Replace(" ", "/"))
            Else
                Sources.Add("https://bmclapi2.bangbang93.com/optifine/" & BmclapiInherit & "/HD_U/" & DownloadInfo.DisplayName.Replace(DownloadInfo.Inherit & " ", ""))
            End If

            Dim PageData As String
            Try
                PageData = NetRequestByClientRetry("https://optifine.net/adloadx?f=" & DownloadInfo.FileName,
                    Encoding:=New UTF8Encoding(False), Accept:="text/html", SimulateBrowserHeaders:=True)
                Task.Progress = 0.8
                Sources.Add("https://optifine.net/" & PageData.RegexSearch("downloadx\?f=[^""']+").First)
                Logger.Info($"OptiFine {DownloadInfo.DisplayName} 官方下载地址：{Sources.Last}")
            Catch ex As Exception
                Logger.Warn(ex, $"获取 OptiFine {DownloadInfo.DisplayName} 官方下载地址失败")
            End Try

            Task.Output = New List(Of NetFile) From {New NetFile(Sources.ToArray, Target, New FileChecker With {.MinSize = 300 * 1024})}
        End Sub) With {.ProgressWeight = 8})
        Loaders.Add(New LoaderDownload("下载 OptiFine 主文件", New List(Of NetFile)) With {.ProgressWeight = 8})
        Loaders.Add(New LoaderTask(Of List(Of NetFile), Boolean)("等待原版下载",
        Sub(Task As LoaderTask(Of List(Of NetFile), Boolean))

            Dim TargetLoaders As List(Of LoaderBase) =
               ClientDownloadLoader.GetLoaderList.Where(Function(l) l.Name = McDownloadClientLibName OrElse l.Name = McDownloadClientJsonName).
               Where(Function(l) l.State <> LoadState.Finished).ToList
            If TargetLoaders.Any Then Logger.Info("OptiFine 安装正在等待原版文件下载完成")
            Do While TargetLoaders.Any AndAlso Not Task.IsCanceled
                TargetLoaders = TargetLoaders.Where(Function(l) l.State <> LoadState.Finished).ToList
                Thread.Sleep(50)
            Loop
            If Task.IsCanceled Then Return

            SyncLock VanillaSyncLock
                Dim ClientName As String = PathUtils.GetLastPart(ClientFolder)
                DirectoryUtils.Create(McFolder & "versions\" & DownloadInfo.Inherit)
                If Not FileUtils.Exists(McFolder & "versions\" & DownloadInfo.Inherit & "\" & DownloadInfo.Inherit & ".json") Then
                    FileUtils.Copy($"{ClientFolder}{ClientName}.json", $"{McFolder}versions\{DownloadInfo.Inherit}\{DownloadInfo.Inherit}.json")
                End If
                If Not FileUtils.Exists(McFolder & "versions\" & DownloadInfo.Inherit & "\" & DownloadInfo.Inherit & ".jar") Then
                    FileUtils.Copy($"{ClientFolder}{ClientName}.jar", $"{McFolder}versions\{DownloadInfo.Inherit}\{DownloadInfo.Inherit}.jar")
                End If
            End SyncLock
        End Sub) With {.ProgressWeight = 0.1, .Show = False})

        If IsNewerVersion Then
            Logger.Info($"检测为新版 OptiFine：{DownloadInfo.Inherit}")
            Loaders.Add(New LoaderTask(Of List(Of NetFile), Boolean)("安装 OptiFine（方式 A）",
            Sub(Task As LoaderTask(Of List(Of NetFile), Boolean))
                Dim BaseMcFolderHome As String = RequestTaskTempFolder(True)
                Dim BaseMcFolder As String = BaseMcFolderHome & ".minecraft\"
                Try

                    If DirectoryUtils.Exists(BaseMcFolder & "versions\" & DownloadInfo.Inherit) Then
                        DirectoryUtils.Delete(BaseMcFolder & "versions\" & DownloadInfo.Inherit)
                    End If
                    DirectoryUtils.Create(BaseMcFolder & "versions\" & DownloadInfo.Inherit & "\")
                    McFolderLauncherProfilesJsonCreate(BaseMcFolder)
                    FileUtils.Copy(McFolder & "versions\" & DownloadInfo.Inherit & "\" & DownloadInfo.Inherit & ".json",
                              BaseMcFolder & "versions\" & DownloadInfo.Inherit & "\" & DownloadInfo.Inherit & ".json")
                    FileUtils.Copy(McFolder & "versions\" & DownloadInfo.Inherit & "\" & DownloadInfo.Inherit & ".jar",
                              BaseMcFolder & "versions\" & DownloadInfo.Inherit & "\" & DownloadInfo.Inherit & ".jar")
                    Task.Progress = 0.06

                    Dim UseJavaWrapper As Boolean = True
Retry:
                    Try
                        McDownloadOptiFineInstall(BaseMcFolderHome, Target, Task, UseJavaWrapper)
                    Catch ex As Exception
                        If UseJavaWrapper Then
                            Logger.Warn(ex, "使用 JavaWrapper 安装 OptiFine 失败，将不使用 JavaWrapper 并重试")
                            UseJavaWrapper = False
                            GoTo Retry
                        Else
                            Throw New Exception("运行 OptiFine 安装器失败", ex)
                        End If
                    End Try
                    Task.Progress = 0.96

                    FileUtils.Delete(BaseMcFolder & "launcher_profiles.json")
                    DirectoryUtils.Copy(BaseMcFolder, McFolder)
                    Task.Progress = 0.98

                    FileUtils.Delete(Target)
                    DirectoryUtils.Delete(BaseMcFolderHome)
                Catch ex As Exception
                    Throw New Exception("安装 OptiFine（方式 A）失败", ex)
                End Try
            End Sub) With {.ProgressWeight = 8})
        Else
            Logger.Info($"检测为旧版 OptiFine：{DownloadInfo.Inherit}")
            Loaders.Add(New LoaderTask(Of List(Of NetFile), Boolean)("安装 OptiFine（方式 B）",
            Sub(Task As LoaderTask(Of List(Of NetFile), Boolean))
                Try

                    FileUtils.Copy(McFolder & "versions\" & DownloadInfo.Inherit & "\" & DownloadInfo.Inherit & ".jar", VersionFolder & Id & ".jar")
                    Task.Progress = 0.7

                    Dim InheritInstance As New McInstance(McFolder & "versions\" & DownloadInfo.Inherit)
                    Dim Json As String = "{
    ""id"": """ & Id & """,
    ""inheritsFrom"": """ & DownloadInfo.Inherit & """,
    ""time"": """ & If(DownloadInfo.ReleaseTime = "", InheritInstance.ReleaseTime.ToString("yyyy'-'MM'-'dd"), DownloadInfo.ReleaseTime.Replace("/", "-")) & "T23:33:33+08:00"",
    ""releaseTime"": """ & If(DownloadInfo.ReleaseTime = "", InheritInstance.ReleaseTime.ToString("yyyy'-'MM'-'dd"), DownloadInfo.ReleaseTime.Replace("/", "-")) & "T23:33:33+08:00"",
    ""type"": ""release"",
    ""libraries"": [
        {""name"": ""optifine:OptiFine:" & PathUtils.GetFileNameWithoutExtension(DownloadInfo.FileName).Replace("OptiFine_", "").Replace("preview_", "") & """},
        {""name"": ""net.minecraft:launchwrapper:1.12""}
    ],
    ""mainClass"": ""net.minecraft.launchwrapper.Launch"","
                    Task.Progress = 0.8
                    If InheritInstance.IsOldJson Then

                        Json += "
    ""minimumLauncherVersion"": 18,
    ""minecraftArguments"": """ & InheritInstance.JsonObject("minecraftArguments").ToString & "  --tweakClass optifine.OptiFineTweaker""
}"
                    Else

                        Json += "
    ""minimumLauncherVersion"": ""21"",
    ""arguments"": {
        ""game"": [
            ""--tweakClass"",
            ""optifine.OptiFineTweaker""
        ]
    }
}"
                    End If
                    FileUtils.Write(VersionFolder & Id & ".json", Json)
                Catch ex As Exception
                    Throw New Exception("安装 OptiFine（方式 B）失败", ex)
                End Try
            End Sub) With {.ProgressWeight = 1})
        End If

        Return Loaders
    End Function

    Private Function McDownloadOptiFineSaveLoader(DownloadInfo As DlOptiFineListEntry, TargetFolder As String) As List(Of LoaderBase)
        Dim Loaders As New List(Of LoaderBase)

        Loaders.Add(New LoaderTask(Of DlOptiFineListEntry, List(Of NetFile))("获取 OptiFine 下载地址",
        Sub(Task As LoaderTask(Of DlOptiFineListEntry, List(Of NetFile)))
            Dim Sources As New List(Of String)

            Dim BmclapiInherit As String = DownloadInfo.Inherit
            If BmclapiInherit = "1.8" OrElse BmclapiInherit = "1.9" Then BmclapiInherit &= ".0"
            If DownloadInfo.IsPreview Then
                Sources.Add("https://bmclapi2.bangbang93.com/optifine/" & BmclapiInherit & "/HD_U_" & DownloadInfo.DisplayName.Replace(DownloadInfo.Inherit & " ", "").Replace(" ", "/"))
            Else
                Sources.Add("https://bmclapi2.bangbang93.com/optifine/" & BmclapiInherit & "/HD_U/" & DownloadInfo.DisplayName.Replace(DownloadInfo.Inherit & " ", ""))
            End If

            Dim PageData As String
            Try
                PageData = NetRequestByClientRetry("https://optifine.net/adloadx?f=" & DownloadInfo.FileName,
                    Encoding:=New UTF8Encoding(False), Accept:="text/html", SimulateBrowserHeaders:=True)
                Task.Progress = 0.8
                Sources.Add("https://optifine.net/" & PageData.RegexSearch("downloadx\?f=[^""']+").First)
                Logger.Info($"OptiFine {DownloadInfo.DisplayName} 官方下载地址：{Sources.Last}")
            Catch ex As Exception
                Logger.Warn(ex, $"获取 OptiFine {DownloadInfo.DisplayName} 官方下载地址失败")
            End Try
            Task.Progress = 0.9

            Task.Output = New List(Of NetFile) From {New NetFile(Sources.ToArray, TargetFolder, New FileChecker With {.MinSize = 64 * 1024})}
        End Sub) With {.ProgressWeight = 6})

        Loaders.Add(New LoaderDownload("下载 OptiFine 主文件", New List(Of NetFile)) With {.ProgressWeight = 10, .Block = True})
        Return Loaders
    End Function

#End Region

#Region "OptiFine 下载菜单"

    Public Function OptiFineDownloadListItem(Entry As DlOptiFineListEntry, OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean) As MyVirtualizingElement(Of MyListItem)
        Return New MyVirtualizingElement(Of MyListItem)(
        Function()

            Dim NewItem As New MyListItem With {
                .Title = Entry.DisplayName, .SnapsToDevicePixels = True, .Height = 42, .Type = MyListItem.CheckType.Clickable, .Tag = Entry,
                .Info = If(Entry.IsPreview, "测试版", "正式版") &
                        If(Entry.ReleaseTime = "", "", "，发布于 " & Entry.ReleaseTime) &
                        If(Entry.RequiredForgeVersion Is Nothing, "，不兼容 Forge", If(Entry.RequiredForgeVersion = "", "", "，兼容 Forge " & Entry.RequiredForgeVersion)),
                .Logo = PathImage & "Blocks/GrassPath.png"
            }
            AddHandler NewItem.Click, OnClick

            If IsSaveOnly Then
                NewItem.ContentHandler = AddressOf OptiFineSaveContMenuBuild
            Else
                NewItem.ContentHandler = AddressOf OptiFineContMenuBuild
            End If
            Return NewItem
        End Function) With {.Height = 42}
    End Function
    Private Sub OptiFineSaveContMenuBuild(sender As Object, e As EventArgs)
        Dim BtnInfo As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonInfo, .ToolTip = "更新日志"}
        AddHandler BtnInfo.Click, AddressOf OptiFineLog_Click
        sender.Buttons = {BtnInfo}
    End Sub
    Private Sub OptiFineContMenuBuild(sender As Object, e As EventArgs)
        Dim BtnSave As New MyIconButton With {.Logo = Logo.IconButtonSave, .ToolTip = "另存为"}
        AddHandler BtnSave.Click, AddressOf OptiFineSave_Click
        Dim BtnInfo As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonInfo, .ToolTip = "更新日志"}
        AddHandler BtnInfo.Click, AddressOf OptiFineLog_Click
        sender.Buttons = {BtnSave, BtnInfo}
    End Sub
    Private Sub OptiFineLog_Click(sender As Object, e As RoutedEventArgs)
        Dim Version As DlOptiFineListEntry
        If sender.Tag IsNot Nothing Then
            Version = sender.Tag
        ElseIf sender.Parent.Tag IsNot Nothing Then
            Version = sender.Parent.Tag
        Else
            Version = sender.Parent.Parent.Tag
        End If
        OpenWebsite("https://optifine.net/changelog?f=" & Version.FileName)
    End Sub
    Public Sub OptiFineSave_Click(sender As Object, e As RoutedEventArgs)
        Dim Version As DlOptiFineListEntry
        If sender.Tag IsNot Nothing Then
            Version = sender.Tag
        ElseIf sender.Parent.Tag IsNot Nothing Then
            Version = sender.Parent.Tag
        Else
            Version = sender.Parent.Parent.Tag
        End If
        McDownloadOptiFineSave(Version)
    End Sub

#End Region

#Region "LiteLoader 下载"

    Private Sub McDownloadLiteLoaderSave(DownloadInfo As DlLiteLoaderListEntry)
        Try
            Dim Id As String = DownloadInfo.Inherit
            Dim Target As String = Dialogs.SaveFile("选择保存位置", DownloadInfo.FileName.Replace("-SNAPSHOT", ""), filter:={("jar", "LiteLoader 安装器")})
            If Target Is Nothing Then Return

            For Each OngoingLoader In LoaderTaskbar.ToList()
                If OngoingLoader.Name <> $"LiteLoader {Id} 下载" Then Continue For
                Hint("该版本正在下载中！", HintType.Red)
                Return
            Next

            Dim Loaders As New List(Of LoaderBase)

            Dim Address As New List(Of String)
            If DownloadInfo.IsLegacy Then

                Select Case DownloadInfo.Inherit
                    Case "1.7.10"
                        Address.Add("https://dl.liteloader.com/redist/1.7.10/liteloader-installer-1.7.10-04.jar")
                    Case "1.7.2"
                        Address.Add("https://dl.liteloader.com/redist/1.7.2/liteloader-installer-1.7.2-04.jar")
                    Case "1.6.4"
                        Address.Add("https://dl.liteloader.com/redist/1.6.4/liteloader-installer-1.6.4-01.jar")
                    Case "1.6.2"
                        Address.Add("https://dl.liteloader.com/redist/1.6.2/liteloader-installer-1.6.2-04.jar")
                    Case "1.5.2"
                        Address.Add("https://dl.liteloader.com/redist/1.5.2/liteloader-installer-1.5.2-01.jar")
                    Case Else
                        Throw New NotSupportedException("未知的 Minecraft 版本（" & DownloadInfo.Inherit & "）")
                End Select
            Else

                Address.Add("http://jenkins.liteloader.com/job/LiteLoaderInstaller%20" & DownloadInfo.Inherit & "/lastSuccessfulBuild/artifact/" & If(DownloadInfo.Inherit = "1.8", "ant/dist/", "build/libs/") & DownloadInfo.FileName)
            End If
            Loaders.Add(New LoaderDownload("下载主文件", New List(Of NetFile) From {New NetFile(Address.ToArray, Target, New FileChecker With {.MinSize = 1024 * 1024})}) With {.ProgressWeight = 15})

            Dim Loader As New LoaderCombo(Of DlLiteLoaderListEntry)("LiteLoader " & Id & " 安装器下载", Loaders) With {.OnStateChanged = AddressOf LoaderStateChangedHintOnly}
            Loader.Start(DownloadInfo)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()

        Catch ex As Exception
            Logger.Error(ex, "开始 LiteLoader 安装器下载失败")
        End Try
    End Sub

    Private Function McDownloadLiteLoaderLoader(DownloadInfo As DlLiteLoaderListEntry, McFolder As String) As List(Of LoaderBase)
        Dim Loaders As New List(Of LoaderBase)

        Loaders.Add(New LoaderTask(Of String, String)("安装 LiteLoader",
        Sub(Task As LoaderTask(Of String, String))
            Try
                Dim VersionName As String = DownloadInfo.Inherit & "-LiteLoader"
                Dim VersionFolder As String = McFolder & "versions\" & VersionName & "\"

                Dim VersionJson As New JObject
                VersionJson.Add("id", VersionName)
                VersionJson.Add("time", Date.ParseExact(DownloadInfo.ReleaseTime, "yyyy/MM/dd HH:mm", Globalization.CultureInfo.InvariantCulture))
                VersionJson.Add("releaseTime", Date.ParseExact(DownloadInfo.ReleaseTime, "yyyy/MM/dd HH:mm", Globalization.CultureInfo.InvariantCulture))
                VersionJson.Add("type", "release")
                VersionJson.Add("arguments", ("{""game"":[""--tweakClass"",""" & DownloadInfo.JsonToken("tweakClass").ToString & """]}").DeserializeJson())
                VersionJson.Add("libraries", DownloadInfo.JsonToken("libraries"))
                CType(VersionJson("libraries"), JContainer).Add(("{""name"": ""com.mumfrey:liteloader:" & DownloadInfo.JsonToken("version").ToString & """,""url"": ""https://dl.liteloader.com/versions/""}").DeserializeJson())
                VersionJson.Add("mainClass", "net.minecraft.launchwrapper.Launch")
                VersionJson.Add("minimumLauncherVersion", 18)
                VersionJson.Add("inheritsFrom", DownloadInfo.Inherit)
                VersionJson.Add("jar", DownloadInfo.Inherit)

                FileUtils.Write(VersionFolder & VersionName & ".json", VersionJson.ToString)
            Catch ex As Exception
                Throw New Exception("安装新 LiteLoader 版本失败", ex)
            End Try
        End Sub) With {.ProgressWeight = 1})

        Return Loaders
    End Function

#End Region

#Region "LiteLoader 下载菜单"

    Public Function LiteLoaderDownloadListItem(Entry As DlLiteLoaderListEntry, OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean) As MyVirtualizingElement(Of MyListItem)
        Return New MyVirtualizingElement(Of MyListItem)(
        Function()

            Dim NewItem As New MyListItem With {
                .Title = Entry.Inherit, .SnapsToDevicePixels = True, .Height = 42, .Type = MyListItem.CheckType.Clickable, .Tag = Entry,
                .Info = If(Entry.IsPreview, "测试版", "稳定版") & If(Entry.ReleaseTime = "", "", "，发布于 " & Entry.ReleaseTime),
                .Logo = PathImage & "Blocks/Egg.png"
            }
            AddHandler NewItem.Click, OnClick

            If IsSaveOnly Then
                NewItem.ContentHandler = AddressOf LiteLoaderSaveContMenuBuild
            Else
                NewItem.ContentHandler = AddressOf LiteLoaderContMenuBuild
            End If

            Return NewItem
        End Function) With {.Height = 42}
    End Function
    Private Sub LiteLoaderSaveContMenuBuild(sender As MyListItem, e As EventArgs)
        If sender.Tag.IsLegacy Then
            sender.Buttons = {}
        Else
            Dim BtnList As New MyIconButton With {.Logo = Logo.IconButtonList, .ToolTip = "查看全部版本", .Tag = sender}
            AddHandler BtnList.Click, AddressOf LiteLoaderAll_Click
            sender.Buttons = {BtnList}
        End If
    End Sub
    Private Sub LiteLoaderContMenuBuild(sender As MyListItem, e As EventArgs)
        Dim BtnSave As New MyIconButton With {.Logo = Logo.IconButtonSave, .ToolTip = "保存安装器", .Tag = sender}
        AddHandler BtnSave.Click, AddressOf LiteLoaderSave_Click
        If sender.Tag.IsLegacy Then
            sender.Buttons = {BtnSave}
        Else
            Dim BtnList As New MyIconButton With {.Logo = Logo.IconButtonList, .ToolTip = "查看全部版本", .Tag = sender}
            AddHandler BtnList.Click, AddressOf LiteLoaderAll_Click
            sender.Buttons = {BtnSave, BtnList}
        End If
    End Sub
    Private Sub LiteLoaderAll_Click(sender As Object, e As RoutedEventArgs)
        Dim Version As DlLiteLoaderListEntry
        If TypeOf sender.Tag Is DlLiteLoaderListEntry Then
            Version = sender.Tag
        Else
            Version = sender.Tag.Tag
        End If
        OpenWebsite("https://jenkins.liteloader.com/view/" & Version.Inherit)
    End Sub
    Public Sub LiteLoaderSave_Click(sender As Object, e As RoutedEventArgs)

        Dim Version As DlLiteLoaderListEntry
        If TypeOf sender.Tag Is DlLiteLoaderListEntry Then
            Version = sender.Tag
        Else
            Version = sender.Tag.Tag
        End If
        McDownloadLiteLoaderSave(Version)
    End Sub

#End Region

#Region "Forgelike 下载"

    Public Sub McDownloadForgelikeSave(Info As DlForgelikeEntry)
        Try
            Dim Target As String = Dialogs.SaveFile("选择保存位置", $"{Info.LoaderName}-{Info.Inherit}-{Info.VersionName}.{Info.FileExtension}",
                                            filter:={(Info.FileExtension, $"{Info.LoaderName} 安装器")})
            If Target Is Nothing Then Return
            Dim DisplayName As String = $"{Info.LoaderName} {Info.Inherit} - {Info.VersionName}"

            For Each OngoingLoader In LoaderTaskbar.ToList()
                If OngoingLoader.Name <> $"{DisplayName} 下载" Then Continue For
                Hint("该版本正在下载中！", HintType.Red)
                Return
            Next

            Dim Files As New List(Of NetFile)
            If Info.IsNeoForge Then

                Dim NeoForge As DlNeoForgeListEntry = Info
                Dim Url As String = NeoForge.UrlBase & "-installer.jar"
                Files.Add(New NetFile({
                    Url.Replace("maven.neoforged.net/releases", "bmclapi2.bangbang93.com/maven"), Url
                }, Target, New FileChecker With {.MinSize = 64 * 1024}))
            Else

                Dim Forge As DlForgeVersionEntry = Info
                Files.Add(New NetFile({
                    $"https://bmclapi2.bangbang93.com/maven/net/minecraftforge/forge/{Forge.Inherit}-{Forge.FileVersion}/forge-{Forge.Inherit}-{Forge.FileVersion}-{Forge.Category}.{Forge.FileExtension}",
                    $"https://files.minecraftforge.net/maven/net/minecraftforge/forge/{Forge.Inherit}-{Forge.FileVersion}/forge-{Forge.Inherit}-{Forge.FileVersion}-{Forge.Category}.{Forge.FileExtension}"
                }, Target, New FileChecker With {.MinSize = 64 * 1024, .Hash = Forge.Hash}))
            End If

            Dim Loaders As New List(Of LoaderBase)
            Loaders.Add(New LoaderDownload("下载主文件", Files) With {.ProgressWeight = 6})

            Dim Loader = New LoaderCombo(Of DlForgelikeEntry)(DisplayName & " 下载", Loaders) With {.OnStateChanged = AddressOf LoaderStateChangedHintOnly}
            Loader.Start(Info)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()

        Catch ex As Exception
            Logger.Error(ex, $"开始 {Info.LoaderName} 安装器下载失败")
        End Try
    End Sub

    Private Sub ForgelikeInjector(Target As String, Task As LoaderTask(Of Boolean, Boolean), McFolder As String, UseJavaWrapper As Boolean, IsNeoForge As Boolean)

        Dim Java As Java = SelectOrDownloadJava(ValueRange(Of Version).AtLeast(New Version(8, 0, 60)), True, Task.CreateCancellationToken, Task.CreateSyncProgressProvider(0, 0.2))
        If Task.IsCanceled Then Return
        If Java Is Nothing Then Throw New OperationCanceledException

        Dim Arguments As String
        If UseJavaWrapper AndAlso Not Settings.Get(Of Boolean)("LaunchAdvanceDisableJLW") Then
            Arguments = $"-Doolloo.jlw.tmpdir=""{PathUtils.RemoveSlashSuffix(PathPure.Value)}"" -cp ""{PathTemp}Cache\forge_installer.jar;{Target}"" -jar ""{LaunchUtils.ExtractPatch("Launch\JavaWrapper.jar", PathPure.Value)}"" com.bangbang93.ForgeInstaller ""{McFolder}"
        Else
            Arguments = $"-cp ""{PathTemp}Cache\forge_installer.jar;{Target}"" com.bangbang93.ForgeInstaller ""{McFolder}"
        End If
        If Java.Version.Major >= 9 Then Arguments = "--add-exports cpw.mods.bootstraplauncher/cpw.mods.bootstraplauncher=ALL-UNNAMED " & Arguments

        SyncLock InstallSyncLock
            Dim Info = New ProcessStartInfo With {
                .FileName = Java.JavaExePath,
                .Arguments = Arguments,
                .UseShellExecute = False,
                .CreateNoWindow = True,
                .RedirectStandardError = True,
                .RedirectStandardOutput = True
            }
            Dim LoaderName As String = If(IsNeoForge, "NeoForge", "Forge")
            Logger.Info($"开始安装 {LoaderName}：{Arguments}")
            Dim process As New Process With {.StartInfo = Info}
            Dim LastResults As New Queue(Of String)
            Using outputWaitHandle As New AutoResetEvent(False)
                Using errorWaitHandle As New AutoResetEvent(False)
                    AddHandler process.OutputDataReceived,
                    Function(sender, e)
                        Try
                            If e.Data Is Nothing Then
                                outputWaitHandle.[Set]()
                            Else
                                LastResults.Enqueue(e.Data)
                                If LastResults.Count > 100 Then LastResults.Dequeue()
                                ForgelikeInjectorLine(e.Data, Task)
                            End If
                        Catch ex As ObjectDisposedException
                        Catch ex As Exception
                            Logger.Warn(ex, $"读取 {LoaderName} 安装器信息失败")
                        End Try
                        Try
                            If Task.State = LoadState.Canceled AndAlso Not process.HasExited Then
                                Logger.Info($"由于任务取消，已中止 {LoaderName} 安装")
                                process.Kill()
                            End If
                        Catch
                        End Try
                        Return Nothing
                    End Function
                    AddHandler process.ErrorDataReceived,
                    Function(sender, e)
                        Try
                            If e.Data Is Nothing Then
                                errorWaitHandle.[Set]()
                            Else
                                LastResults.Enqueue(e.Data)
                                If LastResults.Count > 100 Then LastResults.Dequeue()
                                ForgelikeInjectorLine(e.Data, Task)
                            End If
                        Catch ex As ObjectDisposedException
                        Catch ex As Exception
                            Logger.Warn(ex, $"读取 {LoaderName} 安装器错误信息失败")
                        End Try
                        Try
                            If Task.State = LoadState.Canceled AndAlso Not process.HasExited Then
                                Logger.Info($"由于任务取消，已中止 {LoaderName} 安装")
                                process.Kill()
                            End If
                        Catch
                        End Try
                        Return Nothing
                    End Function
                    process.Start()
                    process.BeginOutputReadLine()
                    process.BeginErrorReadLine()

                    Do Until process.HasExited
                        Thread.Sleep(10)
                    Loop

                    outputWaitHandle.WaitOne(10000)
                    errorWaitHandle.WaitOne(10000)
                    process.Dispose()

                    If LastResults.Reverse().Take(5).Any(Function(l) l = "true") Then Return
                    Logger.Warn(LastResults.Join(vbCrLf))
                    Dim LastLines As String = ""
                    For i As Integer = Math.Max(0, LastResults.Count - 5) To LastResults.Count - 1
                        LastLines &= vbCrLf & LastResults(i)
                    Next
                    Throw New Exception($"{LoaderName} 安装器出错，日志结束部分为：" & LastLines)
                End Using
            End Using
        End SyncLock
    End Sub
    Private Sub ForgelikeInjectorLine(Content As String, Task As LoaderTask(Of Boolean, Boolean))
        Select Case Content
            Case "Extracting json"
                Task.Progress = 0.27
            Case "Downloading libraries"
                Task.Progress = 0.28
            Case "  File exists: Checksum validated."
                Task.Progress += 0.003
            Case "Building Processors"
                Task.Progress = 0.38
            Case "Task: DOWNLOAD_MOJMAPS"
                Task.Progress = 0.4
            Case "Task: MERGE_MAPPING"
                Task.Progress = 0.5
            Case "Splitting: "
                Task.Progress = 0.55
            Case "Parameter Annotations"
                Task.Progress = 0.6
            Case "Processing Complete"
                Task.Progress = 0.67
            Case "log: null"
                Task.Progress = 0.67
            Case "Sorting"
                Task.Progress = 0.8
            Case "Remapping final jar"
                Task.Progress = 0.85
            Case "Remapping jar... 50%"
                Task.Progress = 0.9
            Case "Remapping jar... 100%"
                Task.Progress = 0.95
            Case "Injecting profile"
                Task.Progress = 0.98
            Case Else
                Logger.Trace(Content)
                Return
        End Select
        Logger.Info(Content)
    End Sub

    Private Function McDownloadForgelikeLoader(IsNeoForge As Boolean, LoaderVersion As String, NewInstanceName As String, Inherit As String, Info As DlForgelikeEntry, McFolder As String, ClientDownloadLoader As LoaderCombo(Of String), ClientFolder As String) As List(Of LoaderBase)

        If IsNeoForge AndAlso Info Is Nothing Then

            If Inherit = "1.20.1" AndAlso Not LoaderVersion.StartsWithF("1.20.1-") Then
                Info = New DlNeoForgeListEntry("1.20.1-" & LoaderVersion)
            Else
                Info = New DlNeoForgeListEntry(LoaderVersion)
            End If
        End If
        If Not IsNeoForge AndAlso LoaderVersion.StartsWithF("1.") AndAlso LoaderVersion.Contains("-") Then

            Inherit = LoaderVersion.BeforeFirst("-")
            LoaderVersion = LoaderVersion.AfterLast("-")
        End If
        Dim LoaderName As String = If(IsNeoForge, "NeoForge", "Forge")
        Dim InstallerAddress As String = RequestTaskTempFolder(True) & "forge_installer.jar"
        Dim VersionFolder As String = $"{McFolder}versions\{NewInstanceName}\"
        Dim Loaders As New List(Of LoaderBase)

        If Info Is Nothing Then
            Loaders.Add(New LoaderTask(Of String, String)($"获取 {LoaderName} 详细信息",
            Sub(Task As LoaderTask(Of String, String))

                Dim ForgeLoader = New LoaderTask(Of String, List(Of DlForgeVersionEntry))("McDownloadForgeLoader " & Inherit, AddressOf DlForgeVersionMain)
                ForgeLoader.WaitForExit(Inherit)
                Task.Progress = 0.8

                For Each ForgeVersion In ForgeLoader.Output
                    If McUtils.CompareVersion(ForgeVersion.Version.ToString, LoaderVersion) = 0 Then
                        Info = ForgeVersion
                        Return
                    End If
                Next
                Throw New Exception($"未能找到 {LoaderName} " & Inherit & "-" & LoaderVersion & " 的详细信息！")
            End Sub) With {.ProgressWeight = 3})
        End If

        Loaders.Add(New LoaderTask(Of String, List(Of NetFile))($"准备下载 {LoaderName}",
        Sub(Task As LoaderTask(Of String, List(Of NetFile)))

            Dim Files As New List(Of NetFile)
            If Info.IsNeoForge Then

                Dim Neo As DlNeoForgeListEntry = Info
                Dim Url As String = Neo.UrlBase & "-installer.jar"
                Files.Add(New NetFile({
                    Url.Replace("maven.neoforged.net/releases", "bmclapi2.bangbang93.com/maven"), Url
                }, InstallerAddress, New FileChecker With {.MinSize = 64 * 1024}))
            Else

                Dim Forge As DlForgeVersionEntry = Info
                Dim FileName As String =
                    $"{Forge.Inherit.Replace("-", "_")}-{Forge.FileVersion}/forge-{Forge.Inherit.Replace("-", "_")}-{Forge.FileVersion}-{Forge.Category}.{Forge.FileExtension}"
                Files.Add(New NetFile({
                    $"https://bmclapi2.bangbang93.com/maven/net/minecraftforge/forge/{FileName}",
                    $"https://files.minecraftforge.net/maven/net/minecraftforge/forge/{FileName}"
                }, InstallerAddress, New FileChecker With {.MinSize = 64 * 1024, .Hash = Forge.Hash}))
            End If
            Task.Output = Files
        End Sub) With {.ProgressWeight = 0.5, .Show = False})
        Loaders.Add(New LoaderDownload($"下载 {LoaderName} 主文件", New List(Of NetFile)) With {.ProgressWeight = 9})

        Dim CurrentMcFolder = McFolderSelected
        If IsNeoForge OrElse LoaderVersion.BeforeFirst(".") >= 20 Then
            Logger.Info($"检测为{If(IsNeoForge, " Neo", "新版 ")}Forge：{LoaderVersion}")
            Dim Libs As List(Of McLibEntry) = Nothing
            Loaders.Add(New LoaderTask(Of String, List(Of NetFile))($"分析 {LoaderName} 支持库文件",
            Sub(Task As LoaderTask(Of String, List(Of NetFile)))
                Task.Output = New List(Of NetFile)
                Dim Installer As ZipArchive = Nothing
                Try

                    Installer = FileUtils.OpenZip(InstallerAddress)
                    Task.Progress = 0.2
                    Dim Json As JObject = Installer.GetEntry("install_profile.json").Open().ReadString().DeserializeJson()
                    Dim Json2 As JObject = Installer.GetEntry("version.json").Open().ReadString().DeserializeJson()
                    Json.Merge(Json2)

                    Libs = McLibListGetWithJson(Json, True, CurrentMcFolder)

                    If Json("data") IsNot Nothing AndAlso Json("data")("MOJMAPS") IsNot Nothing Then

                        Task.Progress = 0.4
                        Dim RawJson As JObject = NetRequestByLoader(DlSourceLauncherOrMetaGet(DlClientListGet(Inherit)), IsJson:=True).DeserializeJson()

                        Dim OriginalName As String = Json("data")("MOJMAPS")("client").ToString.Trim("[]".ToCharArray()).BeforeFirst("@")
                        Dim Address = Path.ChangeExtension(McLibGet(OriginalName, CustomMcFolder:=CurrentMcFolder), "." & Json("data")("MOJMAPS")("client").ToString.Trim("[]".ToCharArray()).Split("@")(1))
                        Dim ClientMappings As JToken = RawJson("downloads")("client_mappings")
                        Libs.Add(New McLibEntry With {
                                 .IsNatives = False, .LocalPath = Address, .OriginalName = OriginalName,
                                 .Url = ClientMappings("url"), .Size = ClientMappings("size"), .SHA1 = ClientMappings("sha1")})
                        Logger.Info($"需要下载 Mappings：{ClientMappings("url")} (SHA1: {ClientMappings("sha1")})")
                    End If
                    Task.Progress = 0.8

                    For i = 0 To Libs.Count - 1
                        If Libs(i).LocalPath.EndsWithF($"{LoaderName.Lower}-{Inherit}-{LoaderVersion}.jar") OrElse
                           Libs(i).LocalPath.EndsWithF($"{LoaderName.Lower}-{Inherit}-{LoaderVersion}-client.jar") Then
                            Logger.Warn($"已从待下载 {LoaderName} 支持库中移除：{Libs(i).LocalPath}")
                            Libs.RemoveAt(i)
                            Exit For
                        End If
                    Next
                    Task.Output = McLibNetFilesFromTokens(Libs, CurrentMcFolder)
                Catch ex As Exception
                    Throw New Exception($"获取{If(IsNeoForge, " Neo", "新版 ")}Forge 支持库列表失败", ex)
                Finally
                    If Installer IsNot Nothing Then Installer.Dispose()
                End Try
            End Sub) With {.ProgressWeight = 1})
            Loaders.Add(New LoaderDownload($"下载 {LoaderName} 支持库文件", New List(Of NetFile)) With {.ProgressWeight = 12})
            Loaders.Add(New LoaderTask(Of List(Of NetFile), Boolean)($"获取 {LoaderName} 支持库文件",
            Sub(Task As LoaderTask(Of List(Of NetFile), Boolean))
#Region "Forgelike 文件"
                For Each LibFile As McLibEntry In Libs
                    Dim RealPath As String = Path.Combine(McFolder, LibFile.LocalPath.Substring(CurrentMcFolder.Length))
                    If Not FileUtils.Exists(RealPath) Then FileUtils.Copy(LibFile.LocalPath, RealPath)
                    Logger.Trace($"复制的 {LoaderName} 支持库文件：{LibFile.LocalPath}")
                Next
#End Region
#Region "原版文件"

                Dim TargetLoaders As List(Of LoaderBase) =
                    ClientDownloadLoader.GetLoaderList.Where(Function(l) l.Name = McDownloadClientLibName OrElse l.Name = McDownloadClientJsonName).
                    Where(Function(l) l.State <> LoadState.Finished).ToList()
                If TargetLoaders.Any Then Logger.Info($"{LoaderName} 安装正在等待原版文件下载完成")
                Do While TargetLoaders.Any AndAlso Not Task.IsCanceled
                    TargetLoaders = TargetLoaders.Where(Function(l) l.State <> LoadState.Finished).ToList
                    Thread.Sleep(50)
                Loop
                If Task.IsCanceled Then Return

                SyncLock VanillaSyncLock
                    Dim ClientName As String = PathUtils.GetLastPart(ClientFolder)
                    DirectoryUtils.Create(McFolder & "versions\" & Inherit)
                    If Not FileUtils.Exists(McFolder & "versions\" & Inherit & "\" & Inherit & ".json") Then
                        FileUtils.Copy(ClientFolder & ClientName & ".json", McFolder & "versions\" & Inherit & "\" & Inherit & ".json")
                    End If
                    If Not FileUtils.Exists(McFolder & "versions\" & Inherit & "\" & Inherit & ".jar") Then
                        FileUtils.Copy(ClientFolder & ClientName & ".jar", McFolder & "versions\" & Inherit & "\" & Inherit & ".jar")
                    End If
                End SyncLock
#End Region
            End Sub) With {.ProgressWeight = 0.1, .Show = False})
            Loaders.Add(New LoaderTask(Of Boolean, Boolean)(If(IsNeoForge, "安装 NeoForge", "安装 Forge（方式 A）"),
            Sub(Task As LoaderTask(Of Boolean, Boolean))
                Try
                    Logger.Info($"开始进行 Forgelike 安装：{InstallerAddress}")

                    Dim OldList = DirectoryUtils.EnumerateDirectories(McFolder & "versions\").ToList()

                    DirectoryUtils.Create(VersionFolder)
                    Task.Progress = 0.04

                    McFolderLauncherProfilesJsonCreate(McFolder)
                    Task.Progress = 0.05

                    Dim UseJavaWrapper As Boolean = True
Retry:
                    Try

                        ExtractResources(PathTemp & "Cache\forge_installer.jar", "ForgeInstaller")
                        Task.Progress = 0.06

                        ForgelikeInjector(InstallerAddress, Task, McFolder, UseJavaWrapper, IsNeoForge)
                        Task.Progress = 0.97
                    Catch ex As Exception
                        If UseJavaWrapper Then
                            Logger.Warn(ex, $"使用 JavaWrapper 安装 {LoaderName} 失败，将不使用 JavaWrapper 并重试")
                            UseJavaWrapper = False
                            GoTo Retry
                        Else
                            Throw New Exception($"运行 {LoaderName} 安装器失败", ex)
                        End If
                    End Try

                    Dim DeltaList = DirectoryUtils.EnumerateDirectories(McFolder & "versions\").
                        SkipWhile(Function(i) OldList.Contains(i)).Select(Function(f) DirectoryUtils.GetInfo(f)).ToList()
                    If DeltaList.Count > 1 Then

                        DeltaList = DeltaList.Where(Function(l) l.Name.ContainsIgnoreCase("forge") AndAlso l.EnumerateFiles.Any).ToList
                    End If
                    If DeltaList.Count = 1 Then

                        Dim JsonFile As FileInfo = DeltaList(0).EnumerateFiles.First()
                        FileUtils.Copy(JsonFile.FullName, VersionFolder & NewInstanceName & ".json")
                        Logger.Info($"已复制新增的版本 JSON 文件：{JsonFile.FullName} -> {VersionFolder}{NewInstanceName}.json")
                    ElseIf DeltaList.Count > 1 Then

                        Logger.Info($"有多个疑似的新增版本，无法确定：{DeltaList.Select(Function(d) d.Name).Join(";")}")
                    Else

                        Logger.Info("未找到新增的版本文件夹")
                    End If
                Catch ex As Exception
                    Throw New Exception($"安装新 {LoaderName} 版本失败", ex)
                Finally

                    Try
                        FileUtils.Delete(InstallerAddress)
                    Catch ex As Exception
                        Logger.Warn(ex, $"安装 {LoaderName} 清理文件时出错")
                    End Try
                End Try
            End Sub) With {.ProgressWeight = 10})
        Else
            Logger.Info($"检测为非新版 Forge：{LoaderVersion}")
            Loaders.Add(New LoaderTask(Of List(Of NetFile), Boolean)($"安装 {LoaderName}（方式 B）",
            Sub(Task As LoaderTask(Of List(Of NetFile), Boolean))
                Dim Installer As ZipArchive = Nothing
                Try

                    Installer = FileUtils.OpenZip(InstallerAddress)
                    Task.Progress = 0.2
                    Dim Json As JObject = Installer.GetEntry("install_profile.json").Open().ReadString().DeserializeJson()
                    Task.Progress = 0.4

                    DirectoryUtils.Create(VersionFolder)
                    Task.Progress = 0.5
                    If Json("install") Is Nothing Then

                        Logger.Info($"开始进行 Forge 安装，Legacy 方式 1：{InstallerAddress}")

                        Dim JsonVersion As JObject = Installer.GetEntry(Json("json").ToString.TrimStart("/")).Open().ReadString().DeserializeJson()
                        JsonVersion("id") = NewInstanceName
                        FileUtils.Write(VersionFolder & NewInstanceName & ".json", JsonVersion.ToString)
                        Task.Progress = 0.6

                        Installer.Dispose()
                        FileUtils.ExtractToDirectory(InstallerAddress, InstallerAddress & "_unrar\")
                        If DirectoryUtils.Exists(InstallerAddress & "_unrar\maven\") Then DirectoryUtils.Copy(InstallerAddress & "_unrar\maven\", McFolder & "libraries\")
                        DirectoryUtils.Delete(InstallerAddress & "_unrar\")
                    Else

                        Logger.Info($"开始进行 Forge 安装，Legacy 方式 2：{InstallerAddress}")

                        Dim JarAddress As String = McLibGet(Json("install")("path"), CustomMcFolder:=McFolder)
                        FileUtils.Write(JarAddress, Installer.GetEntry(Json("install")("filePath")).Open)
                        Task.Progress = 0.9

                        Json("versionInfo")("id") = NewInstanceName
                        If Json("versionInfo")("inheritsFrom") Is Nothing Then CType(Json("versionInfo"), JObject).Add("inheritsFrom", Inherit)
                        FileUtils.Write(VersionFolder & NewInstanceName & ".json", Json("versionInfo").ToString)
                    End If
                Catch ex As Exception
                    Throw New Exception("非新版方式安装 Forge 失败", ex)
                Finally
                    Try

                        If Installer IsNot Nothing Then Installer.Dispose()
                        FileUtils.Delete(InstallerAddress)
                        DirectoryUtils.Delete(InstallerAddress & "_unrar\")
                    Catch ex As Exception
                        Logger.Warn(ex, "非新版方式安装 Forge 清理文件时出错")
                    End Try
                End Try
            End Sub) With {.ProgressWeight = 1})
        End If

        Return Loaders
    End Function

#End Region

#Region "Forge 下载菜单"

    Public Sub ForgeDownloadListItemPreload(Stack As StackPanel, Entries As List(Of DlForgeVersionEntry), OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean)

        If Entries.IsSingle Then Return

        Dim FreshVersion As DlForgeVersionEntry = Nothing
        If Entries.Any Then
            FreshVersion = Entries(0)
        Else
            Logger.Info("未找到可用的 Forge 版本")
        End If
        Dim RecommendedVersion As DlForgeVersionEntry = Nothing
        For Each Entry In Entries
            If Entry.IsRecommended Then RecommendedVersion = Entry
        Next

        If FreshVersion IsNot Nothing AndAlso FreshVersion Is RecommendedVersion Then FreshVersion = Nothing

        If RecommendedVersion IsNot Nothing Then
            Dim Recommended = ForgeDownloadListItem(RecommendedVersion, OnClick, IsSaveOnly).Init()
            Recommended.Info = "推荐版" & If(Recommended.Info = "", "", "，" & Recommended.Info)
            Stack.Children.Add(Recommended)
        End If
        If FreshVersion IsNot Nothing Then
            Dim Fresh = ForgeDownloadListItem(FreshVersion, OnClick, IsSaveOnly).Init()
            Fresh.Info = "最新版" & If(Fresh.Info = "", "", "，" & Fresh.Info)
            Stack.Children.Add(Fresh)
        End If

        Stack.Children.Add(New TextBlock With {.Text = "全部版本 (" & Entries.Count & ")", .HorizontalAlignment = HorizontalAlignment.Left, .Margin = New Thickness(6, 13, 0, 4)})
    End Sub
    Public Function ForgeDownloadListItem(Entry As DlForgeVersionEntry, OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean) As MyVirtualizingElement(Of MyListItem)
        Return New MyVirtualizingElement(Of MyListItem)(
        Function()

            Dim NewItem As New MyListItem With {
                .Title = Entry.VersionName, .SnapsToDevicePixels = True, .Height = 42, .Type = MyListItem.CheckType.Clickable, .Tag = Entry,
                .Info = {If(Entry.ReleaseTime = "", "", "发布于 " & Entry.ReleaseTime), If(ModeDebug, "种类：" & Entry.Category, "")}.
                    Where(Function(d) d <> "").Join("，"),
                .Logo = PathImage & "Blocks/Anvil.png"
            }
            AddHandler NewItem.Click, OnClick

            If IsSaveOnly Then
                NewItem.ContentHandler = AddressOf ForgeSaveContMenuBuild
            Else
                NewItem.ContentHandler = AddressOf ForgeContMenuBuild
            End If

            Return NewItem
        End Function) With {.Height = 42}
    End Function
    Private Sub ForgeContMenuBuild(sender As MyListItem, e As EventArgs)
        Dim BtnSave As New MyIconButton With {.Logo = Logo.IconButtonSave, .ToolTip = "另存为"}
        AddHandler BtnSave.Click, AddressOf ForgeSave_Click
        Dim BtnInfo As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonInfo, .ToolTip = "更新日志"}
        AddHandler BtnInfo.Click, AddressOf ForgeLog_Click
        sender.Buttons = {BtnSave, BtnInfo}
    End Sub
    Private Sub ForgeSaveContMenuBuild(sender As MyListItem, e As EventArgs)
        Dim BtnInfo As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonInfo, .ToolTip = "更新日志"}
        AddHandler BtnInfo.Click, AddressOf ForgeLog_Click
        sender.Buttons = {BtnInfo}
    End Sub
    Private Sub ForgeLog_Click(sender As Object, e As RoutedEventArgs)
        Dim Version As DlForgeVersionEntry
        If sender.Tag IsNot Nothing Then
            Version = sender.Tag
        ElseIf sender.Parent.Tag IsNot Nothing Then
            Version = sender.Parent.Tag
        Else
            Version = sender.Parent.Parent.Tag
        End If
        OpenWebsite($"https://files.minecraftforge.net/maven/net/minecraftforge/forge/{Version.Inherit}-{Version.VersionName}/forge-{Version.Inherit}-{Version.VersionName}-changelog.txt")
    End Sub
    Public Sub ForgeSave_Click(sender As Object, e As RoutedEventArgs)
        Dim Version As DlForgeVersionEntry
        If sender.Tag IsNot Nothing Then
            Version = sender.Tag
        ElseIf sender.Parent.Tag IsNot Nothing Then
            Version = sender.Parent.Tag
        Else
            Version = sender.Parent.Parent.Tag
        End If
        McDownloadForgelikeSave(Version)
    End Sub

#End Region

#Region "Forge 推荐版本获取"

    Public Sub McDownloadForgeRecommendedRefresh()
        If IsForgeRecommendedRefreshed Then Return
        IsForgeRecommendedRefreshed = True
        RunInNewThread(
        Sub()
            Try
                Logger.Info("刷新 Forge 推荐版本缓存开始")
                Dim Result As String = NetRequestByClientRetry("https://bmclapi2.bangbang93.com/forge/promos", RequireJson:=True)
                If Result.Length < 1000 Then Throw New Exception("获取的结果过短（" & Result & "）")
                Dim ResultJson As JContainer = Result.DeserializeJson()

                Dim RecommendedList As New List(Of String)
                For Each Version As JObject In ResultJson
                    If Version("name") Is Nothing OrElse Version("build") Is Nothing Then Continue For
                    Dim Name As String = Version("name")
                    If Not Name.EndsWithF("-recommended") Then Continue For

                    RecommendedList.Add("""" & Name.Replace("-recommended", """:""" & Version("build")("version").ToString & """"))
                Next
                If RecommendedList.Count < 5 Then Throw New Exception("获取的推荐版本数过少（" & Result & "）")

                Dim CacheJson As String = "{" & RecommendedList.Join(",") & "}"
                FileUtils.Write(PathTemp & "Cache\ForgeRecommendedList.json", CacheJson)
                Logger.Info("刷新 Forge 推荐版本缓存成功")
            Catch ex As Exception
                Logger.Warn(ex, "刷新 Forge 推荐版本缓存失败")
            End Try
        End Sub, "ForgeRecommendedRefresh")
    End Sub
    Private IsForgeRecommendedRefreshed As Boolean = False

    Public Function McDownloadForgeRecommendedGet(McVersionName As String) As String
        Try
            If McVersionName Is Nothing Then Return Nothing
            Dim List As String = FileUtils.TryReadAsString(PathTemp & "Cache\ForgeRecommendedList.json")
            If String.IsNullOrEmpty(List) Then
                Logger.Info("没有 Forge 推荐版本缓存文件")
                Return Nothing
            End If
            Dim Json As JObject = List.DeserializeJson()
            If Json Is Nothing OrElse (Not If(McVersionName, "").Contains(".")) OrElse Not Json.ContainsKey(McVersionName) Then Return Nothing
            Return If(Json(McVersionName), "").ToString
        Catch ex As Exception
            Logger.Error(ex, $"获取 Forge 推荐版本失败（{If(McVersionName, "null")}）")
            Return Nothing
        End Try
    End Function

#End Region

#Region "NeoForge 下载菜单"

    Public Sub NeoForgeDownloadListItemPreload(Stack As StackPanel, Entries As List(Of DlNeoForgeListEntry), OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean)

        If Entries.IsSingle Then Return

        Dim FreshStableVersion As DlNeoForgeListEntry = Nothing
        Dim FreshBetaVersion As DlNeoForgeListEntry = Nothing
        If Entries.Any() Then
            For Each Entry In Entries.ToList()
                If Entry.IsBeta Then
                    If FreshBetaVersion Is Nothing Then FreshBetaVersion = Entry
                Else
                    FreshStableVersion = Entry
                    Exit For
                End If
            Next
        Else
            Logger.Warn("未找到可用的 NeoForge 版本")
        End If

        If FreshStableVersion IsNot Nothing Then
            Dim Fresh = NeoForgeDownloadListItem(FreshStableVersion, OnClick, IsSaveOnly).Init()
            Fresh.Info = If(Fresh.Info = "", "最新稳定版", "最新" & Fresh.Info)
            Stack.Children.Add(Fresh)
        End If
        If FreshBetaVersion IsNot Nothing Then
            Dim Fresh = NeoForgeDownloadListItem(FreshBetaVersion, OnClick, IsSaveOnly).Init()
            Fresh.Info = If(Fresh.Info = "", "最新测试版", "最新" & Fresh.Info)
            Stack.Children.Add(Fresh)
        End If

        Stack.Children.Add(New TextBlock With {.Text = "全部版本 (" & Entries.Count & ")", .HorizontalAlignment = HorizontalAlignment.Left, .Margin = New Thickness(6, 13, 0, 4)})
    End Sub
    Public Function NeoForgeDownloadListItem(Info As DlNeoForgeListEntry, OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean) As MyVirtualizingElement(Of MyListItem)
        Return New MyVirtualizingElement(Of MyListItem)(
        Function()

            Dim NewItem As New MyListItem With {
                .Title = Info.VersionName, .SnapsToDevicePixels = True, .Height = 42, .Type = MyListItem.CheckType.Clickable, .Tag = Info,
                .Info = If(Info.IsBeta, "测试版", "稳定版"),
                .Logo = PathImage & "Blocks/NeoForge.png"
            }
            AddHandler NewItem.Click, OnClick

            If IsSaveOnly Then
                NewItem.ContentHandler = AddressOf NeoForgeSaveContMenuBuild
            Else
                NewItem.ContentHandler = AddressOf NeoForgeContMenuBuild
            End If
            Return NewItem
        End Function
        ) With {.Height = 42}
    End Function
    Private Sub NeoForgeContMenuBuild(sender As MyListItem, e As EventArgs)
        Dim BtnSave As New MyIconButton With {.Logo = Logo.IconButtonSave, .ToolTip = "另存为"}
        AddHandler BtnSave.Click, AddressOf NeoForgeSave_Click
        Dim BtnInfo As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonInfo, .ToolTip = "更新日志"}
        AddHandler BtnInfo.Click, AddressOf NeoForgeLog_Click
        sender.Buttons = {BtnSave, BtnInfo}
    End Sub
    Private Sub NeoForgeSaveContMenuBuild(sender As MyListItem, e As EventArgs)
        Dim BtnInfo As New MyIconButton With {.LogoScale = 1.05, .Logo = Logo.IconButtonInfo, .ToolTip = "更新日志"}
        AddHandler BtnInfo.Click, AddressOf NeoForgeLog_Click
        sender.Buttons = {BtnInfo}
    End Sub
    Private Sub NeoForgeLog_Click(sender As Object, e As RoutedEventArgs)
        Dim Info As DlNeoForgeListEntry
        If sender.Tag IsNot Nothing Then
            Info = sender.Tag
        ElseIf sender.Parent.Tag IsNot Nothing Then
            Info = sender.Parent.Tag
        Else
            Info = sender.Parent.Parent.Tag
        End If
        OpenWebsite(Info.UrlBase & "-changelog.txt")
    End Sub
    Public Sub NeoForgeSave_Click(sender As Object, e As RoutedEventArgs)
        Dim Info As DlNeoForgeListEntry
        If sender.Tag IsNot Nothing Then
            Info = sender.Tag
        ElseIf sender.Parent.Tag IsNot Nothing Then
            Info = sender.Parent.Tag
        Else
            Info = sender.Parent.Parent.Tag
        End If
        McDownloadForgelikeSave(Info)
    End Sub

#End Region

#Region "Fabric 下载"

    Public Sub McDownloadFabricLoaderSave(DownloadInfo As JObject)
        Try
            Dim Url As String = DownloadInfo("url").ToString
            Dim FileName As String = PathUtils.GetLastPart(Url)
            Dim Version As String = PathUtils.GetLastPart(DownloadInfo("version").ToString)
            Dim Target As String = Dialogs.SaveFile("选择保存位置", FileName, filter:={("jar", "Fabric 安装器")})
            If Target Is Nothing Then Return

            For Each OngoingLoader In LoaderTaskbar.ToList()
                If OngoingLoader.Name <> $"Fabric {Version} 安装器下载" Then Continue For
                Hint("该版本正在下载中！", HintType.Red)
                Return
            Next

            Dim Loaders As New List(Of LoaderBase)

            Dim Address As New List(Of String)
            Address.Add(Url)
            Loaders.Add(New LoaderDownload("下载主文件", New List(Of NetFile) From {New NetFile(Address.ToArray, Target, New FileChecker With {.MinSize = 1024 * 64})}) With {.ProgressWeight = 15})

            Dim Loader As New LoaderCombo(Of JObject)("Fabric " & Version & " 安装器下载", Loaders) With {.OnStateChanged = AddressOf LoaderStateChangedHintOnly}
            Loader.Start(DownloadInfo)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()

        Catch ex As Exception
            Logger.Error(ex, "开始 Fabric 安装器下载失败")
        End Try
    End Sub

    Private Function McDownloadFabricLoader(FabricVersion As String, MinecraftName As String, McFolder As String) As List(Of LoaderBase)

        Dim Id As String = "fabric-loader-" & FabricVersion & "-" & MinecraftName
        Dim VersionFolder As String = McFolder & "versions\" & Id & "\"
        Dim Loaders As New List(Of LoaderBase)

        MinecraftName = MinecraftName.Replace("∞", "infinite")
        Loaders.Add(New LoaderTask(Of String, List(Of NetFile))("获取 Fabric 主文件下载地址",
        Sub(Task As LoaderTask(Of String, List(Of NetFile)))
            Task.Output = New List(Of NetFile) From {New NetFile({
                "https://bmclapi2.bangbang93.com/fabric-meta/v2/versions/loader/" & MinecraftName & "/" & FabricVersion & "/profile/json",
                "https://meta.fabricmc.net/v2/versions/loader/" & MinecraftName & "/" & FabricVersion & "/profile/json"
            }, VersionFolder & Id & ".json", New FileChecker With {.IsJson = True})}
        End Sub) With {.ProgressWeight = 0.5})
        Loaders.Add(New LoaderDownload("下载 Fabric 主文件", New List(Of NetFile)) With {.ProgressWeight = 2.5})

        Return Loaders
    End Function

#End Region

#Region "Fabric 下载菜单"

    Public Function FabricDownloadListItem(Entry As JObject, OnClick As MyListItem.ClickEventHandler) As MyVirtualizingElement(Of MyListItem)
        Return New MyVirtualizingElement(Of MyListItem)(
        Function()
            Dim NewItem As New MyListItem With {
                .Title = Entry("version").ToString.Replace("+build", ""), .SnapsToDevicePixels = True, .Height = 42, .Type = MyListItem.CheckType.Clickable, .Tag = Entry,
                .Info = If(Entry("stable").ToObject(Of Boolean), "稳定版", "测试版"),
                .Logo = PathImage & "Blocks/Fabric.png"
            }
            AddHandler NewItem.Click, OnClick
            Return NewItem
        End Function) With {.Height = 42}
    End Function
    Public Function FabricApiDownloadListItem(Entry As ResourceVersion, OnClick As MyListItem.ClickEventHandler) As MyVirtualizingElement(Of MyListItem)
        Return New MyVirtualizingElement(Of MyListItem)(
        Function()
            Dim NewItem As New MyListItem With {
                .Title = Entry.Display.Split("]")(1).Replace("Fabric API ", "").Replace(" build ", ".").BeforeFirst("+").Trim, .SnapsToDevicePixels = True, .Height = 42, .Type = MyListItem.CheckType.Clickable, .Tag = Entry,
                .Info = Entry.ReleaseTypeDisplay & "，发布于 " & Entry.ReleaseDate.ToString("yyyy'/'MM'/'dd HH':'mm"),
                .Logo = PathImage & "Blocks/Fabric.png"
            }
            AddHandler NewItem.Click, OnClick
            Return NewItem
        End Function) With {.Height = 42}
    End Function
    Public Function OptiFabricDownloadListItem(Entry As ResourceVersion, OnClick As MyListItem.ClickEventHandler) As MyVirtualizingElement(Of MyListItem)
        Return New MyVirtualizingElement(Of MyListItem)(
        Function()
            Dim NewItem As New MyListItem With {
               .Title = Entry.Display.Lower.Replace("optifabric-", "").Replace(".jar", "").Trim.TrimStart("v"), .SnapsToDevicePixels = True, .Height = 42, .Type = MyListItem.CheckType.Clickable, .Tag = Entry,
               .Info = Entry.ReleaseTypeDisplay & "，发布于 " & Entry.ReleaseDate.ToString("yyyy'/'MM'/'dd HH':'mm"),
               .Logo = PathImage & "Blocks/OptiFabric.png"
            }
            AddHandler NewItem.Click, OnClick
            Return NewItem
        End Function) With {.Height = 42}
    End Function

#End Region

#Region "安装"

    Public Class McInstallRequest

        Public NewInstanceName As String

        Public InstanceFolder As String

        Public Components As McComponents

        Public MinecraftJson As String = Nothing

        Public ForgeEntry As DlForgeVersionEntry = Nothing

        Public NeoForgeEntry As DlNeoForgeListEntry = Nothing

        Public FabricApi As ResourceVersion = Nothing

        Public OptiFabric As ResourceVersion = Nothing

        Public LiteLoaderEntry As DlLiteLoaderListEntry = Nothing

        Public IsIntermediateOutput As Boolean = False
    End Class

    Public Function McInstallLoadersGet(Request As McInstallRequest) As List(Of LoaderBase)

        Dim TempMcFolder As String = RequestTaskTempFolder(Request.Components.OptiFineEntry IsNot Nothing OrElse
            Request.Components.ModLoaderVersions.ContainsKey(ModLoaders.Forge) OrElse Request.Components.ModLoaderVersions.ContainsKey(ModLoaders.NeoForge))

        Dim OptiFineEntry = Request.Components.OptiFineEntry?.ToObject(Of DlOptiFineListEntry)()
        Dim OptiFineFolder = If(OptiFineEntry Is Nothing, Nothing, $"{TempMcFolder}versions\{OptiFineEntry.InstanceName}")
        Dim ForgeVersion = If(Request.Components.ModLoaderVersions.ContainsKey(ModLoaders.Forge), Request.Components.ModLoaderVersions(ModLoaders.Forge), Nothing)
        Dim ForgeFolder = If(ForgeVersion Is Nothing, Nothing, $"{TempMcFolder}versions\forge-{ForgeVersion}")
        Dim NeoForgeVersion = If(Request.Components.ModLoaderVersions.ContainsKey(ModLoaders.NeoForge), Request.Components.ModLoaderVersions(ModLoaders.NeoForge), Nothing)
        Dim NeoForgeFolder = If(NeoForgeVersion Is Nothing, Nothing, $"{TempMcFolder}versions\neoforge-{NeoForgeVersion}")
        Dim LiteLoaderVersion = If(Request.Components.ModLoaderVersions.ContainsKey(ModLoaders.LiteLoader), Request.Components.ModLoaderVersions(ModLoaders.LiteLoader), Nothing)
        Dim LiteLoaderFolder = If(LiteLoaderVersion Is Nothing, Nothing, $"{TempMcFolder}versions\{Request.Components.MinecraftVersion}-LiteLoader")
        Dim FabricVersion = If(Request.Components.ModLoaderVersions.ContainsKey(ModLoaders.Fabric), Request.Components.ModLoaderVersions(ModLoaders.Fabric), Nothing)
        Dim FabricFolder = If(FabricVersion Is Nothing, Nothing, $"{TempMcFolder}versions\fabric-loader-{FabricVersion}-{Request.Components.MinecraftVersion}")

        Dim Modable = Request.Components.ModLoaderVersions.Any
        Dim ModsTempFolder As String = $"{TempMcFolder}mods\"
        Dim OptiFineAsMod = OptiFineEntry IsNot Nothing AndAlso Modable
        If OptiFineAsMod Then
            Logger.Info("OptiFine 将作为 Mod 进行下载")
            If LiteLoaderVersion IsNot Nothing Then
                OptiFineFolder = $"{ModsTempFolder}{Request.Components.MinecraftVersion}\"
            Else
                OptiFineFolder = ModsTempFolder
            End If
        End If

        If OptiFineFolder IsNot Nothing Then Logger.Info($"OptiFine 缓存：{OptiFineFolder}")
        If ForgeFolder IsNot Nothing Then Logger.Info($"Forge 缓存：{ForgeFolder}")
        If NeoForgeFolder IsNot Nothing Then Logger.Info($"NeoForge 缓存：{NeoForgeFolder}")
        If FabricFolder IsNot Nothing Then Logger.Info($"Fabric 缓存：{FabricFolder}")
        If LiteLoaderFolder IsNot Nothing Then Logger.Info($"LiteLoader 缓存：{LiteLoaderFolder}")
        Logger.Info($"对应的原版版本：{Request.Components.MinecraftVersion}")

        Dim VersionFolder As String = PathUtils.AddSlashSuffix(Request.InstanceFolder)
        If FileUtils.Exists($"{VersionFolder}{Request.NewInstanceName}.json") Then
            Hint("版本 " & Request.NewInstanceName & " 已经存在！", HintType.Red)
            Throw New OperationCanceledException
        End If

        Dim LoaderList As New List(Of LoaderBase)
        Dim ManagedFileSources As New Dictionary(Of String, String)

        LoaderList.Add(New LoaderTask(Of Integer, Integer)("添加忽略标识", Sub() FileUtils.Write(VersionFolder & ".pclignore", "用于临时地在 PCL 的版本列表中屏蔽此版本。")) With {.Show = False})

        If Request.Components.FabricApiVersion IsNot Nothing Then
            Dim DownloadFile = Request.FabricApi.ToNetFile(ModsTempFolder, ResourceVersion.DownloadReason.Dependency, Request.Components.MinecraftVersion, ModLoaders.Fabric)
            ManagedFileSources.Add("FabricApi", DownloadFile.LocalPath)
            LoaderList.Add(New LoaderDownload("下载 Fabric API", New List(Of NetFile) From {DownloadFile}) With {
                .ProgressWeight = 3, .Show = True, .Block = False})
        End If

        If Request.Components.OptiFabricVersion IsNot Nothing Then
            Dim DownloadFile = Request.OptiFabric.ToNetFile(ModsTempFolder, ResourceVersion.DownloadReason.Dependency, Request.Components.MinecraftVersion, ModLoaders.Fabric)
            ManagedFileSources.Add("OptiFabric", DownloadFile.LocalPath)
            LoaderList.Add(New LoaderDownload("下载 OptiFabric", New List(Of NetFile) From {DownloadFile}) With {
                .ProgressWeight = 3, .Show = True, .Block = False})
        End If

        Dim ClientLoader = New LoaderCombo(Of String)("下载原版 " & Request.Components.MinecraftVersion,
            McDownloadClientLoader(Request.Components.MinecraftVersion, Request.MinecraftJson, VersionFolder)) With {
            .ProgressWeight = 39, .Show = False, .Block = Not Modable AndAlso OptiFineEntry Is Nothing}
        LoaderList.Add(ClientLoader)

        If OptiFineEntry IsNot Nothing Then
            If OptiFineAsMod Then ManagedFileSources.Add("OptiFine", OptiFineFolder & OptiFineEntry.FileName)
            LoaderList.Add(New LoaderCombo(Of String)("下载 OptiFine " & OptiFineEntry.DisplayName,
                If(OptiFineAsMod,
                    McDownloadOptiFineSaveLoader(OptiFineEntry, OptiFineFolder & OptiFineEntry.FileName),
                    McDownloadOptiFineLoader(OptiFineEntry, TempMcFolder, ClientLoader, VersionFolder))) With {
                .ProgressWeight = If(Modable, 16, 24), .Show = False, .Block = Not Modable})
        End If

        If ForgeVersion IsNot Nothing Then
            LoaderList.Add(New LoaderCombo(Of String)("下载 Forge " & ForgeVersion,
                McDownloadForgelikeLoader(False, ForgeVersion, "forge-" & ForgeVersion, Request.Components.MinecraftVersion, Request.ForgeEntry, TempMcFolder, ClientLoader, VersionFolder)) With {
                    .ProgressWeight = 25, .Show = False, .Block = FabricVersion Is Nothing AndAlso Request.LiteLoaderEntry Is Nothing AndAlso Request.NeoForgeEntry Is Nothing})
        End If

        If NeoForgeVersion IsNot Nothing Then
            LoaderList.Add(New LoaderCombo(Of String)("下载 NeoForge " & NeoForgeVersion,
                McDownloadForgelikeLoader(True, NeoForgeVersion, "neoforge-" & NeoForgeVersion, Request.Components.MinecraftVersion, Request.NeoForgeEntry, TempMcFolder, ClientLoader, VersionFolder)) With {
                .ProgressWeight = 25, .Show = False, .Block = Request.ForgeEntry Is Nothing AndAlso FabricVersion Is Nothing AndAlso Request.LiteLoaderEntry Is Nothing})
        End If

        If LiteLoaderVersion IsNot Nothing Then
            LoaderList.Add(New LoaderCombo(Of String)("下载 LiteLoader " & Request.Components.MinecraftVersion,
                McDownloadLiteLoaderLoader(Request.LiteLoaderEntry, TempMcFolder)) With {
                    .ProgressWeight = 1, .Show = False, .Block = FabricVersion Is Nothing})
        End If

        If FabricVersion IsNot Nothing Then
            LoaderList.Add(New LoaderCombo(Of String)("下载 Fabric " & FabricVersion,
                McDownloadFabricLoader(FabricVersion, Request.Components.MinecraftVersion, TempMcFolder)) With {
                .ProgressWeight = 2, .Show = False, .Block = True})
        End If

        Dim MinecraftFolder = McFolderSelected
        Dim TargetFolder As String = Nothing
        LoaderList.Add(New LoaderTask(Of String, String)("安装游戏",
        Sub(Task As LoaderTask(Of String, String))

            MergeJson(VersionFolder, OptiFineFolder, OptiFineAsMod, ForgeFolder, NeoForgeFolder, FabricFolder, LiteLoaderFolder)
            Task.Progress = 0.2

            DirectoryUtils.Copy(TempMcFolder & "libraries", MinecraftFolder & "libraries")
            Task.Progress = 0.8

            TargetFolder = If(Request.IsIntermediateOutput, VersionFolder, New McInstance(VersionFolder).PathIndie)
            Dim ModsFolder = TargetFolder & "mods\"
            If DirectoryUtils.Exists(ModsTempFolder) Then
                DirectoryUtils.Copy(ModsTempFolder, ModsFolder)
            ElseIf Modable Then
                DirectoryUtils.Create(ModsFolder)
                Logger.Info($"自动创建 Mod 文件夹：{ModsFolder}")
            End If
            Dim ResourcepacksFolder = TargetFolder & "resourcepacks\"
            DirectoryUtils.Create(ResourcepacksFolder)
            Logger.Info($"自动创建资源包文件夹：{ResourcepacksFolder}")
        End Sub) With {.ProgressWeight = 2, .Block = True})

        Dim LoadersLib As New List(Of LoaderBase)
        LoadersLib.Add(New LoaderTask(Of String, List(Of NetFile))("分析游戏支持库文件（副加载器）", Sub(Task) Task.Output = McLibNetFilesFromInstance(New McInstance(VersionFolder))) With {.ProgressWeight = 1, .Show = False})
        LoadersLib.Add(New LoaderDownload("下载游戏支持库文件（副加载器）", New List(Of NetFile)) With {.ProgressWeight = 7, .Show = False})
        LoaderList.Add(New LoaderCombo(Of String)("下载游戏支持库文件", LoadersLib) With {.ProgressWeight = 8})

        LoaderList.Add(New LoaderTask(Of Integer, Integer)("记录安装清单",
        Sub()
            Dim ManagedFiles As New List(Of McInstallManifest.ManagedFile)

            For Each Entry In ManagedFileSources
                Dim RelativePath = Entry.Value.Substring(ModsTempFolder.Length)
                Dim FullPath = PathUtils.ForCompare(Path.Combine(TargetFolder, "mods", RelativePath))
                ManagedFiles.Add(New McInstallManifest.ManagedFile("mods/" & RelativePath.Replace("\", "/").ToLowerInvariant(),
                    CryptographyUtils.ComputeFileHash(FullPath, CryptographyUtils.HashMethod.Sha1), FileUtils.GetInfo(FullPath).LastWriteTimeUtc, Entry.Key))
            Next

            Dim TempLoaderVersions As New Dictionary(Of ModLoaders, String)(Request.Components.ModLoaderVersions)
            If ForgeVersion IsNot Nothing AndAlso ForgeVersion.StartsWithF("1.") AndAlso ForgeVersion.Contains("-") Then
                TempLoaderVersions(ModLoaders.Forge) = ForgeVersion.AfterLast("-")
            End If
            If NeoForgeVersion IsNot Nothing AndAlso NeoForgeVersion.StartsWithF("1.20.1-") Then
                TempLoaderVersions(ModLoaders.NeoForge) = NeoForgeVersion.Replace("1.20.1-", "")
            End If

            Dim Target = New McComponents(Request.Components.MinecraftVersion, TempLoaderVersions,
                Request.Components.OptiFineEntry, Request.Components.FabricApiVersion, Request.Components.OptiFabricVersion)
            Dim InstanceConfig = New McInstance(VersionFolder).Config
            Configs.McInstallManifest.Set(New McInstallManifest(Versions.McInstallManifestVersion, Target, Date.UtcNow, ManagedFiles), InstanceConfig)
            InstanceConfig.Save()
        End Sub) With {.Show = False})

        If Not Request.IsIntermediateOutput Then
            LoaderList.Add(New LoaderTask(Of Integer, Integer)("删除忽略标识", Sub() FileUtils.Delete(VersionFolder & ".pclignore")) With {.Show = False})
        End If

        Return LoaderList
    End Function
    Private Sub MergeJson(VersionFolder As String, OptiFineFolder As String, OptiFineAsMod As Boolean,
                          ForgeFolder As String, NeoForgeFolder As String, FabricFolder As String, LiteLoaderFolder As String)
        Dim ProfileSources As New List(Of (Name As String, Folder As String)) From {("Minecraft", VersionFolder)}
        If OptiFineFolder IsNot Nothing AndAlso Not OptiFineAsMod Then ProfileSources.Add(("OptiFine", OptiFineFolder))
        If ForgeFolder IsNot Nothing Then ProfileSources.Add(("Forge", ForgeFolder))
        If NeoForgeFolder IsNot Nothing Then ProfileSources.Add(("NeoForge", NeoForgeFolder))
        If LiteLoaderFolder IsNot Nothing Then ProfileSources.Add(("LiteLoader", LiteLoaderFolder))
        If FabricFolder IsNot Nothing Then ProfileSources.Add(("Fabric", FabricFolder))
        Logger.Info($"开始进行版本合并，输出：{VersionFolder}，" & String.Join("，", ProfileSources.Select(Function(Source) $"{Source.Name}：{Source.Folder}")))

        Dim LoadedProfiles As New List(Of (Name As String, Json As JObject))
        For Each Source In ProfileSources
            Dim ProfilePath = Path.Combine(Source.Folder, PathUtils.GetLastPart(Source.Folder) & ".json")
            Dim ProfileText = If(FileUtils.TryReadAsString(ProfilePath), "")
            If Not ProfileText.StartsWithF("{") Then Throw New Exception($"{Source.Name} json 有误，地址：{ProfilePath}，前段内容：{ProfileText.Substring(0, Math.Min(ProfileText.Length, 1000))}")
            LoadedProfiles.Add((Source.Name, ProfileText.DeserializeJson(Of JObject)()))
        Next

        Dim AllArguments = LoadedProfiles.Select(Function(Profile) If(Profile.Json("minecraftArguments")?.ToString(), " ")).Join(" ")

        Dim MergedJson = LoadedProfiles(0).Json
        For Each Loaded In LoadedProfiles.Skip(1)
            Loaded.Json.Remove("releaseTime")
            Loaded.Json.Remove("time")
            MergedJson.Merge(Loaded.Json)
        Next

        Dim SplitArguments As New List(Of String)
        For Each Argument In AllArguments.Split(" ", True).Select(Function(Value) Value.Trim())
            If Not Argument.StartsWithF("-") AndAlso SplitArguments.Any Then
                Dim LastArgument = SplitArguments.Last
                If LastArgument.StartsWithF("-") AndAlso Not LastArgument.Contains(" ") Then
                    SplitArguments(SplitArguments.Count - 1) = LastArgument & " " & Argument
                    Continue For
                End If
            End If
            SplitArguments.Add(Argument)
        Next
        If SplitArguments.Any() Then MergedJson("minecraftArguments") = SplitArguments.Distinct().Join(" ")

        MergedJson.Remove("_comment_")
        MergedJson.Remove("inheritsFrom")
        MergedJson.Remove("jar")
        Dim VersionName = PathUtils.GetLastPart(VersionFolder)
        MergedJson("id") = VersionName

        FileUtils.Write(Path.Combine(VersionFolder, VersionName & ".json"), MergedJson.ToString())
        Logger.Info($"版本合并 {VersionName} 完成")
    End Sub

    Public Function McInstallStart(Request As McInstallRequest) As Boolean
        Try
            Dim Loader As New LoaderCombo(Of String)(Request.NewInstanceName & " 安装", McInstallLoadersGet(Request)) With {.OnStateChanged = AddressOf McInstallState}

            Loader.Start(Request.InstanceFolder)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()
            Return True
        Catch ex As Exception
            Logger.Error(ex, "开始合并安装失败")
            Return False
        End Try
    End Function

    Public Function McModificationStart(Request As McInstallRequest, Instance As McInstance) As Boolean
        Try

            Dim TargetFolder = Instance.PathIndie
            Dim SubLoaders As New List(Of LoaderBase)
            SubLoaders.Add(New LoaderTask(Of Integer, Integer)("检查 Natives 文件占用",
            Sub()
                For Each File In DirectoryUtils.EnumerateFiles(GetNativesFolder(Instance), includeSubDirectories:=True, searchPattern:="*.dll")
                    If Not FileUtils.IsAvailable(File) Then Throw New Exception($"Natives 文件正在被占用，请关闭游戏后再次尝试：{File}")
                Next
            End Sub) With {.Show = False})

            Dim BuildFolder = RequestTaskTempFolder() & Instance.Name & "\"
            Dim BuildRequest As New McInstallRequest With {
                .NewInstanceName = Instance.Name,
                .InstanceFolder = BuildFolder,
                .Components = Request.Components,
                .MinecraftJson = Request.MinecraftJson,
                .ForgeEntry = Request.ForgeEntry,
                .NeoForgeEntry = Request.NeoForgeEntry,
                .FabricApi = Request.FabricApi,
                .OptiFabric = Request.OptiFabric,
                .LiteLoaderEntry = Request.LiteLoaderEntry,
                .IsIntermediateOutput = True
            }
            SubLoaders.AddRange(McInstallLoadersGet(BuildRequest))

            Dim RenameFailed As Boolean = False
            SubLoaders.Add(New LoaderTask(Of Integer, Integer)("修改版本",
            Sub(Task)

                McInstallCommit(Instance, Configs.McInstallManifest.Get(New McInstance(BuildFolder).Config), BuildFolder, TargetFolder)

                Dim NewInstance As McInstance = Nothing
                If Request.NewInstanceName Is Nothing Then
                    NewInstance = New McInstance(Instance.PathVersion).Load()
                Else
                    Try
                        NewInstance = Instance.Rename(Request.NewInstanceName)
                    Catch ex As Exception
                        RenameFailed = True
                        Logger.Warn(ex, $"版本修改成功，但重命名为 {Request.NewInstanceName} 失败：{ex.GetDisplay(False)}", LogBehavior.None)
                        NewInstance = New McInstance(Instance.PathVersion).Load()
                    End Try
                End If
                If NewInstance Is Nothing Then NewInstance = New McInstance(Instance.PathVersion).Load()
                RunInUiWait(
                Sub()
                    WriteIni(McFolderSelected & "PCL.ini", "InstanceCache", "")

                    If PageInstanceLeft.Instance IsNot Nothing AndAlso
                       PathUtils.IsSame(PageInstanceLeft.Instance.PathVersion, Instance.PathVersion) Then
                        PageInstanceLeft.Instance = NewInstance
                    End If
                    If McInstanceSelected IsNot Nothing AndAlso
                       PathUtils.IsSame(McInstanceSelected.PathVersion, Instance.PathVersion) Then
                        McInstanceSelected = NewInstance
                    End If
                End Sub)
            End Sub) With {.ProgressWeight = 1, .Block = True})

            Dim Loader As New LoaderCombo(Of String)(Instance.Name & " 修改", SubLoaders) With {.OnStateChanged =
            Sub(CurrentLoader)

                Select Case CurrentLoader.State
                    Case LoadState.Finished
                        If Not RenameFailed Then
                            Hint(CurrentLoader.Name & "成功！", HintType.Green)
                        Else
                            Hint(CurrentLoader.Name & "成功，但重命名失败！", HintType.Green)
                        End If
                    Case LoadState.Failed
                        MyMsgBox(CurrentLoader.Error.GetDisplay(True), CurrentLoader.Name & "失败", IsWarn:=True)
                    Case LoadState.Canceled
                        Hint(CurrentLoader.Name & "已取消！", HintType.Blue)
                End Select
            End Sub}
            Dim Worker As New RedoableWorker(Sub() Loader.WaitForExit(Instance.PathVersion))

            If McInstanceLock.TryAcquire(Worker, Instance.PathVersion) Then
                AddHandler Worker.Stopped, Sub() RunInUi(Sub() LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\"))
                Worker.Start()

                LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
            Else
                Logger.Error("版本被其他 Worker 占用，版本修改失败")
                Return False
            End If
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()
            Return True
        Catch ex As Exception
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
            Logger.Error(ex, "开始修改版本失败", LogBehavior.Toast)
            Return False
        End Try
    End Function

    Public Sub McInstallCommit(Instance As McInstance, NewManifest As McInstallManifest, BuildFolder As String, TargetFolder As String)
        Dim OldFiles = If(Configs.McInstallManifest.Get(Instance.Config)?.ManagedFiles, {})
        Dim NewFiles = NewManifest.ManagedFiles

        Dim FilesToDelete As New List(Of String)
        Dim FilesToMove As New List(Of (Source As String, Target As String))

        For Each NewFile In NewFiles
            FilesToMove.Add((Path.Combine(BuildFolder, NewFile.RelativePath), Path.Combine(TargetFolder, NewFile.RelativePath)))
        Next

        Dim NewFilePaths As New HashSet(Of String)(NewFiles.Select(Function(File) File.RelativePath))
        For Each OldFile In OldFiles
            If NewFilePaths.Contains(OldFile.RelativePath) Then Continue For
            FilesToDelete.Add(PathUtils.ForCompare(Path.Combine(TargetFolder, OldFile.RelativePath)))
        Next

        If DirectoryUtils.Exists(BuildFolder & "resources\") Then DirectoryUtils.Copy(BuildFolder & "resources\", TargetFolder & "resources\")

        Dim OldJsonPath As String = Instance.GetJsonPath()
        FilesToDelete.Add(OldJsonPath)
        FilesToDelete.Add(Path.ChangeExtension(OldJsonPath, ".jar"))
        FilesToDelete.Add(Instance.PathVersion & Instance.Name & ".jar")
        FilesToDelete = FilesToDelete.Distinct(StringComparer.OrdinalIgnoreCase).ToList()

        Instance.Config.Save()
        For Each TargetPath In FilesToDelete
            FileUtils.Delete(TargetPath, True)
        Next
        FileUtils.Move(BuildFolder & Instance.Name & ".jar", Instance.PathVersion & Instance.Name & ".jar")
        FileUtils.Move(BuildFolder & Instance.Name & ".json", OldJsonPath)
        For Each File In FilesToMove
            DirectoryUtils.Create(PathUtils.RemoveLastPart(File.Target))
            FileUtils.Copy(File.Source, File.Target)
        Next
        Dim ManagedFiles = NewFiles.Select(Function(File) New McInstallManifest.ManagedFile(File.RelativePath, File.Sha1,
            FileUtils.GetInfo(Path.Combine(TargetFolder, File.RelativePath)).LastWriteTimeUtc, File.Component)).ToList()
        Configs.McInstallManifest.Set(New McInstallManifest(NewManifest.SchemaVersion, NewManifest.Components, NewManifest.LastModifiedUtc, ManagedFiles), Instance.Config)
        Instance.Config.Save()
    End Sub

    Public Sub LoaderStateChangedHintOnly(Loader As LoaderBase)
        Select Case Loader.State
            Case LoadState.Finished
                Hint(Loader.Name & "成功！", HintType.Green)
            Case LoadState.Failed
                Hint(Loader.Name & "失败：" & Loader.Error.GetDisplay(False), HintType.Red)
            Case LoadState.Canceled
                Hint(Loader.Name & "已取消！", HintType.Blue)
        End Select
    End Sub

    Public Sub McInstallState(Loader As LoaderBase)
        Select Case Loader.State
            Case LoadState.Finished
                WriteIni(McFolderSelected & "PCL.ini", "InstanceCache", "")
                Hint(Loader.Name & "成功！", HintType.Green)

            Case LoadState.Failed
                MyMsgBox(Loader.Error.GetDisplay(True), Loader.Name & "失败", IsWarn:=True)
            Case LoadState.Canceled
                Hint(Loader.Name & "已取消！", HintType.Blue)
            Case LoadState.Loading
                Return
        End Select
        McInstallFailedClearFolder(Loader)
        LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
    End Sub
    Public Sub McInstallFailedClearFolder(Loader)
        If Loader.State <> LoadState.Failed AndAlso Loader.State <> LoadState.Canceled Then Return
        RunInThread(
        Sub()
            Try
                Thread.Sleep(1000)

                If DirectoryUtils.Exists(Loader.Input & "saves\") OrElse DirectoryUtils.Exists(Loader.Input & "versions\") Then
                    Logger.Warn($"由于版本已被独立启动，不清理版本文件夹：{Loader.Input}")
                Else
                    Logger.Warn($"由于下载失败或取消，清理版本文件夹：{Loader.Input}")
                    DirectoryUtils.Delete(Loader.Input)
                End If
            Catch ex As Exception
                Logger.Warn(ex, "下载失败或取消后清理版本文件夹失败")
            End Try
        End Sub)
    End Sub

#End Region

    Private InstallSyncLock As New Object

    Private VanillaSyncLock As New Object

End Module
