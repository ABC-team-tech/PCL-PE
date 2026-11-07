Public Class PageInstanceOverall

    Private IsLoad As Boolean = False
    Private Sub PageSetupLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        PanBack.ScrollToHome()

        Reload()

        If IsLoad Then Return
        IsLoad = True
        PanDisplay.TriggerForceResize()

    End Sub

    Public InstanceHeaderItem As MyListItem

    Private Sub Reload()
        AniControlEnabled += 1

        ComboDisplayType.SelectedIndex = ReadIni(PageInstanceLeft.Instance.PathVersion & "PCL\Setup.ini", "DisplayType", McInstanceCardType.Auto)
        BtnDisplayStar.Text = If(PageInstanceLeft.Instance.IsStar, "从收藏夹中移除", "加入收藏夹")
        BtnFolderMods.Visibility = If(PageInstanceLeft.Instance.Modable, Visibility.Visible, Visibility.Collapsed)

        PanDisplayItem.Children.Clear()
        InstanceHeaderItem = PageInstanceLeft.Instance.ToListItem().Init()
        InstanceHeaderItem.IsHitTestVisible = False
        PanDisplayItem.Children.Add(InstanceHeaderItem)
        FrmMain.PageNameRefresh()

        ComboDisplayLogo.SelectedIndex = 0
        Dim Logo As String = ReadIni(PageInstanceLeft.Instance.PathVersion & "PCL\Setup.ini", "Logo", "")
        Dim LogoCustom As Boolean = ReadIni(PageInstanceLeft.Instance.PathVersion & "PCL\Setup.ini", "LogoCustom", "False")
        If LogoCustom Then
            For Each Selection As MyComboBoxItem In ComboDisplayLogo.Items
                If Selection.Tag = Logo OrElse (Selection.Tag = "PCL\Logo.png" AndAlso Logo.EndsWithF("PCL\Logo.png")) Then
                    ComboDisplayLogo.SelectedItem = Selection
                    Exit For
                End If
            Next
        End If

        AniControlEnabled -= 1
    End Sub

#Region "卡片：个性化"

    Private Sub ComboDisplayType_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboDisplayType.SelectionChanged
        If Not (IsLoad AndAlso AniControlEnabled = 0) Then Return
        If ComboDisplayType.SelectedIndex <> 1 Then

            Try

                WriteIni(PageInstanceLeft.Instance.PathVersion & "PCL\Setup.ini", "DisplayType", ComboDisplayType.SelectedIndex)
                PageInstanceLeft.Instance.DisplayType = ReadIni(PageInstanceLeft.Instance.PathVersion & "PCL\Setup.ini", "DisplayType", McInstanceCardType.Auto)
                FrmInstanceLeft.RefreshModDisabled()

                WriteIni(McFolderSelected & "PCL.ini", "InstanceCache", "")
                LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
            Catch ex As Exception
                Logger.Error(ex, $"修改版本分类失败（{PageInstanceLeft.Instance.Name}）")
            End Try
            Reload()
        Else

            Try
                If Not Settings.Get(Of Boolean)("HintHide") Then
                    If MyMsgBox("确认要从版本列表中隐藏该版本吗？隐藏该版本后，它将不再出现于 PCL 显示的版本列表中。" & vbCrLf & "此后，在版本列表页面按下 F11 才可以查看被隐藏的版本。", "隐藏版本提示",, "取消") <> 1 Then
                        ComboDisplayType.SelectedIndex = 0
                        Return
                    End If
                    Settings.Set("HintHide", True)
                End If
                WriteIni(PageInstanceLeft.Instance.PathVersion & "PCL\Setup.ini", "DisplayType", McInstanceCardType.Hidden)
                WriteIni(McFolderSelected & "PCL.ini", "InstanceCache", "")
                LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
            Catch ex As Exception
                Logger.Error(ex, $"隐藏版本 {PageInstanceLeft.Instance.Name} 失败")
            End Try
        End If
    End Sub

    Private Sub BtnDisplayDesc_Click(sender As Object, e As EventArgs) Handles BtnDisplayDesc.Click
        Try
            Dim OldInfo As String = ReadIni(PageInstanceLeft.Instance.PathVersion & "PCL\Setup.ini", "CustomInfo")
            Dim NewInfo As String = MyMsgBoxInput("更改描述", "修改版本的描述文本，留空则使用 PCL 的默认描述。", OldInfo, New ObjectModel.Collection(Of Validate), "默认描述")
            If NewInfo IsNot Nothing AndAlso OldInfo <> NewInfo Then WriteIni(PageInstanceLeft.Instance.PathVersion & "PCL\Setup.ini", "CustomInfo", NewInfo)
            PageInstanceLeft.Instance = New McInstance(PageInstanceLeft.Instance.Name).Load()
            Reload()
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
        Catch ex As Exception
            Logger.Error(ex, $"版本 {PageInstanceLeft.Instance.Name} 描述更改失败", LogBehavior.Alert)
        End Try
    End Sub

    Private Sub BtnDisplayRename_Click(sender As Object, e As EventArgs) Handles BtnDisplayRename.Click
        Try
            If McInstanceLock.IsLocked(PageInstanceLeft.Instance) Then
                Hint("该版本正在被修改，请稍后再重命名！", HintType.Red)
                Return
            End If

            Dim OldName As String = PageInstanceLeft.Instance.Name

            Dim NewName As String = MyMsgBoxInput("重命名版本", "", OldName, New ObjectModel.Collection(Of Validate) From {
                New ValidateFolderName(McFolderSelected & "versions", IgnoreList:=New List(Of String) From {OldName})
            })
            If String.IsNullOrWhiteSpace(NewName) Then Return
            Dim NewInstance = PageInstanceLeft.Instance.Rename(NewName)

            Hint("重命名成功！", HintType.Green)
            PageInstanceLeft.Instance = NewInstance
            If Not IsNothing(McInstanceSelected) AndAlso McInstanceSelected.Equals(PageInstanceLeft.Instance) Then WriteIni(McFolderSelected & "PCL.ini", "Version", NewName)
            Reload()
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
        Catch ex As Exception
            Logger.Error(ex, "重命名版本失败", LogBehavior.Alert)
        End Try
    End Sub

    Private Sub ComboDisplayLogo_SelectionChanged() Handles ItemDisplayLogoCustom.PreviewMouseLeftButtonUp, ComboDisplayLogo.SelectionChanged
        If Not (IsLoad AndAlso AniControlEnabled = 0) Then Return

        Try
            If ComboDisplayLogo.SelectedItem Is ItemDisplayLogoCustom Then
                Dim FileName As String = Dialogs.SelectFile("选择图片", False, filter:={({"png", "jpeg", "jpg", "gif", "webp"}, "常用图片文件")}).FirstOrDefault()
                If String.IsNullOrEmpty(FileName) Then
                    Reload()
                    Return
                End If
                FileUtils.Copy(FileName, PageInstanceLeft.Instance.PathVersion & "PCL\Logo.png")
            Else
                FileUtils.Delete(PageInstanceLeft.Instance.PathVersion & "PCL\Logo.png")
            End If
        Catch ex As Exception
            Logger.Error(ex, $"更改自定义版本图标失败（{PageInstanceLeft.Instance.Name}）")
        End Try

        Try
            Dim NewLogo As String = ComboDisplayLogo.SelectedItem.Tag
            WriteIni(PageInstanceLeft.Instance.PathVersion & "PCL\Setup.ini", "Logo", NewLogo)
            WriteIni(PageInstanceLeft.Instance.PathVersion & "PCL\Setup.ini", "LogoCustom", Not NewLogo = "")

            WriteIni(McFolderSelected & "PCL.ini", "InstanceCache", "")
            PageInstanceLeft.Instance = New McInstance(PageInstanceLeft.Instance.Name).Load()
            Reload()
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
        Catch ex As Exception
            Logger.Error(ex, $"更改版本图标失败（{PageInstanceLeft.Instance.Name}）")
        End Try
    End Sub

    Private Sub BtnDisplayStar_Click(sender As Object, e As EventArgs) Handles BtnDisplayStar.Click
        Try
            WriteIni(PageInstanceLeft.Instance.PathVersion & "PCL\Setup.ini", "IsStar", Not PageInstanceLeft.Instance.IsStar)
            PageInstanceLeft.Instance = New McInstance(PageInstanceLeft.Instance.Name).Load()
            Reload()
            McInstanceListForceRefresh = True
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
        Catch ex As Exception
            Logger.Error(ex, $"版本 {PageInstanceLeft.Instance.Name} 收藏状态更改失败", LogBehavior.Alert)
        End Try
    End Sub

#End Region

#Region "卡片：快捷方式"

    Private Sub BtnFolderVersion_Click() Handles BtnFolderVersion.Click
        OpenInstanceFolder(PageInstanceLeft.Instance)
    End Sub
    Public Shared Sub OpenInstanceFolder(Instance As McInstance)
        OpenExplorer(Instance.PathVersion)
    End Sub

    Private Sub BtnFolderSaves_Click() Handles BtnFolderSaves.Click
        Dim Folder As String = PageInstanceLeft.Instance.PathIndie & "saves\"
        DirectoryUtils.Create(Folder)
        OpenExplorer(Folder)
    End Sub

    Private Sub BtnFolderMods_Click() Handles BtnFolderMods.Click
        Dim Folder As String = PageInstanceLeft.Instance.PathIndie & "mods\"
        DirectoryUtils.Create(Folder)
        OpenExplorer(Folder)
    End Sub

    Private Sub BtnFolderScreen_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnFolderScreen.Click
        Dim Folder As String = PageInstanceLeft.Instance.PathIndie & "screenshots\"
        DirectoryUtils.Create(Folder)
        OpenExplorer(Folder)
    End Sub

#End Region

#Region "卡片：管理"

    Private Sub BtnManageScript_Click() Handles BtnManageScript.Click
        Try

            Dim SavePath As String = Dialogs.SaveFile("选择脚本保存位置", "启动 " & PageInstanceLeft.Instance.Name & ".bat", filter:={("bat", "批处理文件")})
            If SavePath Is Nothing Then Return

            If McLaunchLoader.State = LoadState.Loading Then
                Hint("请在当前启动任务结束后再试！", HintType.Red)
                Return
            End If

            If McLaunchStart(New McLaunchOptions With {.SaveBatch = SavePath, .Instance = PageInstanceLeft.Instance}) Then
                If Settings.Get(Of McLoginType)("LoginType") = McLoginType.Legacy Then
                    Hint("正在导出启动脚本……")
                Else
                    Hint("正在导出启动脚本……（注意，使用脚本启动可能会导致登录失效！）")
                End If
            End If
        Catch ex As Exception
            Logger.Error(ex, $"导出启动脚本失败（{PageInstanceLeft.Instance.Name}）", LogBehavior.Alert)
        End Try
    End Sub

    Private Sub BtnManageCheck_Click(sender As Object, e As EventArgs) Handles BtnManageCheck.Click
        Try
            If McInstanceLock.IsLocked(PageInstanceLeft.Instance) Then
                Hint("该版本正在被修改，请稍后再补全文件！", HintType.Red)
                Return
            End If

            If ShouldIgnoreFileCheck(PageInstanceLeft.Instance) Then
                Hint("请先关闭 [版本设置 → 设置 → 高级启动选项 → 关闭文件校验]，然后再尝试补全文件！", HintType.Blue)
                Return
            End If

            For Each OngoingLoader In LoaderTaskbar
                If OngoingLoader.Name <> PageInstanceLeft.Instance.Name & " 文件补全" Then Continue For
                Hint("正在处理中，请稍候！", HintType.Red)
                Return
            Next

            Dim Loader As New LoaderCombo(Of String)(PageInstanceLeft.Instance.Name & " 文件补全", DlClientFix(PageInstanceLeft.Instance, True, False))
            Loader.OnStateChanged =
            Sub()
                Select Case Loader.State
                    Case LoadState.Finished
                        Hint(Loader.Name & "成功！", HintType.Green)
                    Case LoadState.Failed
                        Hint(Loader.Name & "失败：" & Loader.Error.GetDisplay(False), HintType.Red)
                    Case LoadState.Canceled
                        Hint(Loader.Name & "已取消！", HintType.Blue)
                End Select
            End Sub
            Loader.Start(PageInstanceLeft.Instance.Name)
            LoaderTaskbarAdd(Loader)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()
        Catch ex As Exception
            Logger.Error(ex, $"尝试补全文件失败（{PageInstanceLeft.Instance.Name}）", LogBehavior.Alert)
        End Try
    End Sub

    Private Sub BtnManageDelete_Click(sender As Object, e As EventArgs) Handles BtnManageDelete.Click
        Try
            If McInstanceLock.IsLocked(PageInstanceLeft.Instance) Then
                Hint("该版本正在被修改，请稍后再删除！", HintType.Red)
                Return
            End If
            Dim IsShiftPressed As Boolean = My.Computer.Keyboard.ShiftKeyDown
            Dim IsHintIndie As Boolean = PageInstanceLeft.Instance.State <> McInstance.McInstanceState.Error AndAlso PageInstanceLeft.Instance.PathIndie <> McFolderSelected
            Dim SavesFolder = DirectoryUtils.GetInfo(PageInstanceLeft.Instance.PathIndie & "saves\")
            Dim SaveEntries = If(IsHintIndie AndAlso SavesFolder.Exists, SavesFolder.EnumerateDirectories.OrderByDescending(Function(Save) Save.LastWriteTime).Select(Function(Save) $"{Save.Name}（上次修改：{StringUtils.FormatTimeSpan(Save.LastWriteTime - Date.Now, False)}）").ToList, New List(Of String))
            Select Case MyMsgBox($"你确定要{If(IsShiftPressed, "永久", "")}删除版本 {PageInstanceLeft.Instance.Name} 吗？" &
                        If(IsHintIndie, vbCrLf & "该版本对应的存档、资源包、Mod 等文件也将被一并删除！" &
                            If(SaveEntries.Any, vbCrLf & vbCrLf & "这会删除以下存档：" & vbCrLf & "· " & SaveEntries.Join(vbCrLf & "· "), ""), ""),
                        "版本删除确认", , "取消",, IsHintIndie OrElse IsShiftPressed)
                Case 1
                    PageInstanceLeft.Instance.ResetSettingsCache()
                    DirectoryUtils.Delete(PageInstanceLeft.Instance.PathVersion, Not IsShiftPressed)
                    Hint("已删除版本 " & PageInstanceLeft.Instance.Name & "！", HintType.Green)
                Case 2
                    Return
            End Select
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
            FrmMain.PageBack()
        Catch ex As OperationCanceledException
            Logger.Warn(ex, $"删除版本 {PageInstanceLeft.Instance.Name} 被主动取消")
        Catch ex As Exception
            Logger.Error(ex, $"删除版本 {PageInstanceLeft.Instance.Name} 失败", LogBehavior.Alert)
        End Try
    End Sub

#End Region

End Class
