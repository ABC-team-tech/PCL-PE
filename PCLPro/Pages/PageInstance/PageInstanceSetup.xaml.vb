Public Class PageInstanceSetup

    Private LoadedFirst As Boolean = False
    Private Sub PageSetupSystem_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        PanBack.ScrollToHome()
        RefreshRam(False)

        AniControlEnabled += 1
        Reload()
        AniControlEnabled -= 1

        If LoadedFirst Then Return
        LoadedFirst = True

        AddHandler PageInstanceLeft.CurrentJavaWorker.Started, AddressOf UpdateJavaRelatedUi
        AddHandler PageInstanceLeft.CurrentJavaWorker.Stopped, AddressOf UpdateJavaRelatedUi

        TextArgumentJavaRange.ValidateRules = New ObjectModel.Collection(Of Validate) From {New ValidateFunc(
        Function(Str As String)
            Try
                If String.IsNullOrWhiteSpace(Str) Then Return "不能为空"
                Dim Range = ValueRange(Of Version).FromString(Str, Function(s) StringUtils.ParseVersionWithDefaults(s))
                If Range.IsEmpty Then Return "范围下限的版本号比上限更高，导致该范围无法匹配到任何版本"
                If Range.HasLower AndAlso Range.Lower.Major <= 1 AndAlso (Range.Lower.Minor > 0 OrElse Range.Lower.Build > 0 OrElse Range.Lower.Revision > 0) Then Return "不应使用 1.x 格式进行匹配（例如，若想匹配 Java 17，应填写 17 而非 1.17）"
                If Range.HasUpper AndAlso Range.Upper.Major <= 1 AndAlso (Range.Upper.Minor > 0 OrElse Range.Upper.Build > 0 OrElse Range.Upper.Revision > 0) Then Return "不应使用 1.x 格式进行匹配（例如，若想匹配 Java 17，应填写 17 而非 1.17）"
                If Range.HasUpper AndAlso Range.Upper.Major <= 4 Then Return "该范围所要求的 Java 版本过低"
                If Range.HasUpper AndAlso Range.IsUpperInclusive AndAlso Range.Upper.Minor <= 0 AndAlso Range.Upper.Build <= 0 Then Return $"右侧闭区间的意图并不明确。如果不想允许 Java {Range.Upper.Major}，请改为 {vbLQ}{Range.Upper.Major}){vbRQ}。如果想允许 Java {Range.Upper.Major}，请改为 {vbLQ}{Range.Upper.Major + 1}){vbRQ}。"
                If Range.Intersect(ValueRange(Of Version).OpenClosed(New Version(5, 0), New Version(99, 0))).IsEmpty() Then Return "该范围无法匹配到任何常见的 Java"
                Return ""
            Catch ex As Exception
                Return ex.Message
            End Try
        End Function)}

        Dim timer As New Threading.DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 1)}
        AddHandler timer.Tick, AddressOf RefreshRam
        timer.Start()

    End Sub
    Public Sub Reload()
        Try

            Dim _unused = PageInstanceLeft.Instance.PathIndie
            ComboArgumentIndieV2.SelectedIndex = If(Settings.Get(Of Boolean)("VersionArgumentIndieV2", Instance:=PageInstanceLeft.Instance), 0, 1)
            UpdateJavaList()

            ComboServerLogin.SelectedIndex = Settings.Get(Of Integer)("VersionServerLogin", Instance:=PageInstanceLeft.Instance)
            ComboServerLoginLast = ComboServerLogin.SelectedIndex
            UpdateServerLoginUI()

            If Settings.Get(Of Integer)("VersionAdvanceAssets", Instance:=PageInstanceLeft.Instance) = 2 Then
                Logger.Info("已迁移老版本的关闭文件校验设置")
                Settings.Reset("VersionAdvanceAssets", Instance:=PageInstanceLeft.Instance)
                Settings.Set("VersionAdvanceAssetsV2", True, Instance:=PageInstanceLeft.Instance)
            End If

            SettingService.RefreshSettings(Me)

            OnVersionRamTypeChanged(Settings.Get(Of Integer)("VersionRamType", Instance:=PageInstanceLeft.Instance))

            RefreshContentUI()

        Catch ex As Exception
            Logger.Error(ex, "重载版本独立设置时出错")
        End Try
    End Sub

    Public Sub Reset()
        Try

            SettingService.ResetSettings(Me)

            Settings.Reset("VersionServerLogin", Instance:=PageInstanceLeft.Instance)
            Settings.Reset("VersionArgumentIndieV2", Instance:=PageInstanceLeft.Instance)
            Settings.Reset("VersionAdvanceAssets", Instance:=PageInstanceLeft.Instance)
            Configs.JavaForced.Reset(PageInstanceLeft.Instance.Config)
            JavaListRefreshWorker.Start()

            Logger.Info("已初始化版本独立设置")
            Hint("已初始化版本独立设置！", HintType.Green, False)
        Catch ex As Exception
            Logger.Error(ex, "初始化版本独立设置失败", LogBehavior.Alert)
        End Try

        Reload()
    End Sub

#Region "游戏内存"

    Public Shared Sub OnVersionRamTypeChanged(Type As Integer)
        If FrmInstanceSetup Is Nothing Then Return
        FrmInstanceSetup.RamType(Type)
    End Sub

    Public Sub RamType(Type As Integer)
        If SliderRamCustom Is Nothing Then Return
        SliderRamCustom.IsEnabled = (Type = 1)
    End Sub

    Public Sub RefreshRam(ShowAnim As Boolean)
        If LabRamGame Is Nothing OrElse LabRamUsed Is Nothing OrElse FrmMain.PageCurrent <> FormMain.PageType.InstanceSetup OrElse FrmInstanceLeft.PageID <> FormMain.PageSubType.InstanceSetup Then Return

        Dim RamGame As Double = Math.Round(GetRam(PageInstanceLeft.Instance), 5)
        Dim RamTotal As Double = Math.Round(My.Computer.Info.TotalPhysicalMemory / 1024 / 1024 / 1024, 1)
        Dim RamAvailable As Double = Math.Round(My.Computer.Info.AvailablePhysicalMemory / 1024 / 1024 / 1024, 1)
        Dim RamGameActual As Double = Math.Round(Math.Min(RamGame, RamAvailable), 5)
        Dim RamUsed As Double = Math.Round(RamTotal - RamAvailable, 5)
        Dim RamEmpty As Double = Math.Round((RamTotal - RamUsed - RamGame).Clamp(0, 1000), 1)

        If RamTotal <= 1.5 Then
            SliderRamCustom.MaxValue = Math.Max(Math.Floor((RamTotal - 0.3) / 0.1), 1)
        ElseIf RamTotal <= 8 Then
            SliderRamCustom.MaxValue = Math.Floor((RamTotal - 1.5) / 0.5) + 12
        ElseIf RamTotal <= 16 Then
            SliderRamCustom.MaxValue = Math.Floor((RamTotal - 8) / 1) + 25
        Else
            SliderRamCustom.MaxValue = Math.Floor((RamTotal - 16) / 2) + 33
        End If

        LabRamGame.Text = If(RamGame = Math.Floor(RamGame), RamGame & ".0", RamGame) & " GB" &
                          If(RamGame <> RamGameActual, " (可用 " & If(RamGameActual = Math.Floor(RamGameActual), RamGameActual & ".0", RamGameActual) & " GB)", "")
        LabRamUsed.Text = If(RamUsed = Math.Floor(RamUsed), RamUsed & ".0", RamUsed) & " GB"
        LabRamTotal.Text = " / " & If(RamTotal = Math.Floor(RamTotal), RamTotal & ".0", RamTotal) & " GB"
        If ShowAnim Then

            AniStart({
                AaGridLengthWidth(ColumnRamUsed, RamUsed - ColumnRamUsed.Width.Value, 800,, New AniEaseOutFluent(AniEasePower.Strong)),
                AaGridLengthWidth(ColumnRamGame, RamGameActual - ColumnRamGame.Width.Value, 800,, New AniEaseOutFluent(AniEasePower.Strong)),
                AaGridLengthWidth(ColumnRamEmpty, RamEmpty - ColumnRamEmpty.Width.Value, 800,, New AniEaseOutFluent(AniEasePower.Strong))
            }, "VersionSetup Ram Grid")
        Else

            ColumnRamUsed.Width = New GridLength(RamUsed, GridUnitType.Star)
            ColumnRamGame.Width = New GridLength(RamGameActual, GridUnitType.Star)
            ColumnRamEmpty.Width = New GridLength(RamEmpty, GridUnitType.Star)
        End If
    End Sub
    Private Sub RefreshRam() Handles SliderRamCustom.Change, RadioRamType0.Check, RadioRamType1.Check, RadioRamType2.Check
        RefreshRam(True)
    End Sub

    Private RamTextLeft As Integer = 2, RamTextRight As Integer = 1

    Private Sub RefreshRamText() Handles RectRamGame.SizeChanged, RectRamEmpty.SizeChanged, LabRamGame.SizeChanged

        Dim RectUsedWidth = RectRamUsed.ActualWidth
        Dim TotalWidth = PanRamDisplay.ActualWidth
        Dim LabGameWidth = LabRamGame.ActualWidth, LabUsedWidth = LabRamUsed.ActualWidth, LabTotalWidth = LabRamTotal.ActualWidth
        Dim LabGameTitleWidth = LabRamGameTitle.ActualWidth, LabUsedTitleWidth = LabRamUsedTitle.ActualWidth

        Dim Left As Integer
        If RectUsedWidth - 30 < LabUsedWidth OrElse RectUsedWidth - 30 < LabUsedTitleWidth Then

            Left = 0
        ElseIf RectUsedWidth - 25 < (LabUsedWidth + LabTotalWidth) Then

            Left = 1
        Else

            Left = 2
        End If
        If RamTextLeft <> Left Then
            RamTextLeft = Left
            Select Case Left
                Case 0
                    AniStart({
                            AaOpacity(LabRamUsed, -LabRamUsed.Opacity, 100),
                            AaOpacity(LabRamTotal, -LabRamTotal.Opacity, 100),
                            AaOpacity(LabRamUsedTitle, -LabRamUsedTitle.Opacity, 100)
                        }, "VersionSetup Ram TextLeft")
                Case 1
                    AniStart({
                            AaOpacity(LabRamUsed, 1 - LabRamUsed.Opacity, 100),
                            AaOpacity(LabRamTotal, -LabRamTotal.Opacity, 100),
                            AaOpacity(LabRamUsedTitle, 0.7 - LabRamUsedTitle.Opacity, 100)
                        }, "VersionSetup Ram TextLeft")
                Case 2
                    AniStart({
                            AaOpacity(LabRamUsed, 1 - LabRamUsed.Opacity, 100),
                            AaOpacity(LabRamTotal, 1 - LabRamTotal.Opacity, 100),
                            AaOpacity(LabRamUsedTitle, 0.7 - LabRamUsedTitle.Opacity, 100)
                        }, "VersionSetup Ram TextLeft")
            End Select
        End If

        Dim Right As Integer
        If TotalWidth < LabGameWidth + 2 + RectUsedWidth OrElse TotalWidth < LabGameTitleWidth + 2 + RectUsedWidth Then

            Right = 0
        Else

            Right = 1
        End If
        If Right = 0 Then
            If AniControlEnabled = 0 AndAlso (RamTextRight <> Right OrElse AniIsRun("VersionSetup Ram TextRight")) Then

                AniStart({
                        AaX(LabRamGame, TotalWidth - LabGameWidth - LabRamGame.Margin.Left, 100,, New AniEaseOutFluent(AniEasePower.Weak)),
                        AaX(LabRamGameTitle, TotalWidth - LabGameTitleWidth - LabRamGameTitle.Margin.Left, 100,, New AniEaseOutFluent(AniEasePower.Weak))
                }, "VersionSetup Ram TextRight")
            Else

                LabRamGame.Margin = New Thickness(TotalWidth - LabGameWidth, 3, 0, 0)
                LabRamGameTitle.Margin = New Thickness(TotalWidth - LabGameTitleWidth, 0, 0, 5)
            End If
        Else
            If AniControlEnabled = 0 AndAlso (RamTextRight <> Right OrElse AniIsRun("VersionSetup Ram TextRight")) Then

                AniStart({
                        AaX(LabRamGame, 2 + RectUsedWidth - LabRamGame.Margin.Left, 100,, New AniEaseOutFluent(AniEasePower.Weak)),
                        AaX(LabRamGameTitle, 2 + RectUsedWidth - LabRamGameTitle.Margin.Left, 100,, New AniEaseOutFluent(AniEasePower.Weak))
                }, "VersionSetup Ram TextRight")
            Else

                LabRamGame.Margin = New Thickness(2 + RectUsedWidth, 3, 0, 0)
                LabRamGameTitle.Margin = New Thickness(2 + RectUsedWidth, 0, 0, 5)
            End If
        End If
        RamTextRight = Right
    End Sub

    Public Shared Function GetRam(Instance As McInstance) As Double

        If Settings.Get(Of Integer)("VersionRamType", Instance:=Instance) = 2 Then
            Return PageSetupLaunch.GetRam(Instance, True)
        End If

        Dim RamGive As Double
        If Settings.Get(Of Integer)("VersionRamType", Instance:=Instance) = 0 Then

            Dim RamAvailable As Double = Math.Round(My.Computer.Info.AvailablePhysicalMemory / 1024 / 1024 / 1024 * 10) / 10

            Dim RamMininum As Double
            Dim RamTarget1 As Double
            Dim RamTarget2 As Double
            Dim RamTarget3 As Double
            If Instance IsNot Nothing AndAlso Not Instance.IsLoaded Then Instance.Load()
            If Instance IsNot Nothing AndAlso Instance.Modable Then

                Dim ModDir = DirectoryUtils.GetInfo(Instance.PathIndie & "mods\")
                Dim ModCount As Integer = If(ModDir.Exists, ModDir.GetFiles.Count(Function(f) {".jar", ".zip", ".litemod"}.Contains(f.Extension.Lower)), 0)
                RamMininum = 0.5 + ModCount / 150
                RamTarget1 = 1.5 + ModCount / 90
                RamTarget2 = 2.7 + ModCount / 50
                RamTarget3 = 4.5 + ModCount / 25
            ElseIf Instance IsNot Nothing AndAlso Instance.Version.HasOptiFine Then

                RamMininum = 0.5
                RamTarget1 = 1.5
                RamTarget2 = 3
                RamTarget3 = 5
            Else

                RamMininum = 0.5
                RamTarget1 = 1.5
                RamTarget2 = 2.5
                RamTarget3 = 4
            End If
            Dim RamDelta As Double

            RamDelta = RamTarget1
            RamGive += Math.Min(RamAvailable, RamDelta)
            RamAvailable -= RamDelta
            If RamAvailable < 0.1 Then GoTo PreFin

            RamDelta = RamTarget2 - RamTarget1
            RamGive += Math.Min(RamAvailable * 0.7, RamDelta)
            RamAvailable -= RamDelta / 0.7
            If RamAvailable < 0.1 Then GoTo PreFin

            RamDelta = RamTarget3 - RamTarget2
            RamGive += Math.Min(RamAvailable * 0.4, RamDelta)
            RamAvailable -= RamDelta / 0.4
            If RamAvailable < 0.1 Then GoTo PreFin

            RamDelta = RamTarget3
            RamGive += Math.Min(RamAvailable * 0.15, RamDelta)
            RamAvailable -= RamDelta / 0.15
            If RamAvailable < 0.1 Then GoTo PreFin
PreFin:

            RamGive = Math.Round(Math.Max(RamGive, RamMininum), 1)
        Else

            Dim Value As Integer = Settings.Get(Of Integer)("VersionRamCustom", Instance:=Instance)
            If Value <= 12 Then
                RamGive = Value * 0.1 + 0.3
            ElseIf Value <= 25 Then
                RamGive = (Value - 12) * 0.5 + 1.5
            ElseIf Value <= 33 Then
                RamGive = (Value - 25) * 1 + 8
            Else
                RamGive = (Value - 33) * 2 + 16
            End If
        End If
        Return RamGive
    End Function

#End Region

#Region "服务器"

    Public Shared Sub OnVersionServerLoginChanged(Type As Integer)
        If FrmInstanceSetup Is Nothing Then Return
        WriteIni(McFolderSelected & "PCL.ini", "InstanceCache", "")
        If PageInstanceLeft.Instance Is Nothing Then Return
        PageInstanceLeft.Instance = New McInstance(PageInstanceLeft.Instance.Name).Load()
        LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
    End Sub

    Private Sub TextServerEnter_Change() Handles TextServerEnter.TextChanged
        Dim NewText = TextServerEnter.Text.Replace("：", ":").Replace("。", ".")
        If NewText = TextServerEnter.Text Then Return
        Dim CurrentPosition = TextServerEnter.SelectionStart
        TextServerEnter.Text = NewText
        TextServerEnter.SelectionStart = CurrentPosition
    End Sub

    Private ComboServerLoginLast As Integer
    Private Sub ComboServerLogin_Changed() Handles ComboServerLogin.SelectionChanged, TextServerNide.ValidatedTextChanged, TextServerAuthServer.ValidatedTextChanged, TextServerAuthRegister.ValidatedTextChanged
        If AniControlEnabled <> 0 Then Return
        UpdateServerLoginUI()

        If ComboServerLogin.SelectedIndex = 3 AndAlso Not TextServerNide.IsValidated Then Return
        If ComboServerLogin.SelectedIndex = 4 AndAlso Not TextServerAuthServer.IsValidated Then Return

        If ComboServerLoginLast = ComboServerLogin.SelectedIndex Then Return

        ComboServerLoginLast = ComboServerLogin.SelectedIndex
        Settings.Set("VersionServerLogin", ComboServerLogin.SelectedIndex, Instance:=PageInstanceLeft.Instance)
    End Sub
    Private Sub UpdateServerLoginUI()
        If LabServerNide Is Nothing Then Return
        Dim Type = ComboServerLogin.SelectedIndex
        LabServerNide.Visibility = If(Type = 3, Visibility.Visible, Visibility.Collapsed)
        TextServerNide.Visibility = If(Type = 3, Visibility.Visible, Visibility.Collapsed)
        PanServerNide.Visibility = If(Type = 3, Visibility.Visible, Visibility.Collapsed)
        LabServerAuthName.Visibility = If(Type = 4, Visibility.Visible, Visibility.Collapsed)
        TextServerAuthName.Visibility = If(Type = 4, Visibility.Visible, Visibility.Collapsed)
        LabServerAuthRegister.Visibility = If(Type = 4, Visibility.Visible, Visibility.Collapsed)
        TextServerAuthRegister.Visibility = If(Type = 4, Visibility.Visible, Visibility.Collapsed)
        LabServerAuthServer.Visibility = If(Type = 4, Visibility.Visible, Visibility.Collapsed)
        TextServerAuthServer.Visibility = If(Type = 4, Visibility.Visible, Visibility.Collapsed)
        BtnServerAuthLittle.Visibility = If(Type = 4, Visibility.Visible, Visibility.Collapsed)
        CardServer.TriggerForceResize()
    End Sub

    Private Sub BtnServerAuthLittle_Click(sender As Object, e As EventArgs) Handles BtnServerAuthLittle.Click
        If TextServerAuthServer.Text <> "" AndAlso TextServerAuthServer.Text <> "https://littleskin.cn/api/yggdrasil" AndAlso
            MyMsgBox("即将把第三方登录设置覆盖为 LittleSkin 登录。" & vbCrLf & "除非你是服主，或者服主要求你这样做，否则请不要继续。" & vbCrLf & vbCrLf & "是否确实需要覆盖当前设置？",
                     "设置覆盖确认", "继续", "取消") = 2 Then Return
        TextServerAuthServer.Text = "https://littleskin.cn/api/yggdrasil"
        TextServerAuthRegister.Text = "https://littleskin.cn/auth/register"
        TextServerAuthName.Text = "LittleSkin 登录"
    End Sub

#End Region

#Region "Java 主类别和预览"

    Private Sub ReloadCurrentJava() Handles ComboArgumentJava.SelectionChanged, TextArgumentJavaRange.ValidatedTextChanged
        PageInstanceLeft.ReloadCurrentJava()
    End Sub

    Private Sub UpdateJavaRelatedUi() Handles ComboArgumentJava.SelectionChanged, TextArgumentJavaRange.TextChanged
        RunInUi(AddressOf _UpdateJavaRelatedUi)
    End Sub
    Private Sub _UpdateJavaRelatedUi()
        Dim JavaWorker = PageInstanceLeft.CurrentJavaWorker
        Select Case ComboArgumentJava.SelectedIndex
            Case 0, 2
                HintArgumentJava.Visibility = Visibility.Visible
                TextArgumentJavaRange.Visibility = Visibility.Collapsed
                PanArgumentJavaSelect.Visibility = Visibility.Collapsed

                If Not JavaWorker.LastSucceeded OrElse JavaWorker.Running Then
                    If Not JavaWorker.Running Then JavaWorker.Start()
                    HintArgumentJava.Text = $"正在查找 Java……"
                    HintArgumentJava.Theme = MyHint.Themes.Blue
                ElseIf JavaWorker.LastResult Is Nothing Then
                    If ComboArgumentJava.SelectedIndex = 0 Then
                        HintArgumentJava.Text = $"你的电脑上没有可供该版本使用的 Java，PCL 会在启动游戏时自动下载。"
                        HintArgumentJava.Theme = MyHint.Themes.Yellow
                    Else
                        HintArgumentJava.Text = $"该版本的版本文件夹中没有发现任何 Java！{vbCrLf}请点击此处打开版本文件夹，然后将 Java 文件夹复制进去。"
                        HintArgumentJava.Theme = MyHint.Themes.Red
                    End If
                Else
                    HintArgumentJava.Text = $"将会使用：{JavaWorker.LastResult}"
                    HintArgumentJava.Theme = MyHint.Themes.Blue
                End If
            Case 1
                HintArgumentJava.Visibility = Visibility.Visible
                TextArgumentJavaRange.Visibility = Visibility.Visible
                PanArgumentJavaSelect.Visibility = Visibility.Collapsed

                If Not Settings.HasSaved("VersionArgumentJavaRange", PageInstanceLeft.Instance) Then
                    Dim DefaultRange As String = GetJavaRequirement(PageInstanceLeft.Instance).Range.ToString.Replace("-∞", "").Replace("+∞", "")
                    Settings.Set("VersionArgumentJavaRange", DefaultRange, PageInstanceLeft.Instance)
                    TextArgumentJavaRange.Text = DefaultRange
                End If

                If Not String.IsNullOrEmpty(TextArgumentJavaRange.ValidateResult) Then
                    HintArgumentJava.Text = $"Java 版本范围错误：{TextArgumentJavaRange.ValidateResult}"
                    HintArgumentJava.Theme = MyHint.Themes.Red
                    Return
                End If

                If Not JavaWorker.LastSucceeded OrElse JavaWorker.Running Then
                    If Not JavaWorker.Running Then JavaWorker.Start()
                    HintArgumentJava.Text = $"正在查找 Java……"
                    HintArgumentJava.Theme = MyHint.Themes.Blue
                ElseIf JavaWorker.LastResult Is Nothing Then
                    HintArgumentJava.Text = $"你的电脑上没有任何 Java 符合该版本范围！{vbCrLf}如果 Mojang 提供了对应版本的 Java，PCL 会在启动游戏时自动下载。"
                    HintArgumentJava.Theme = MyHint.Themes.Yellow
                Else
                    HintArgumentJava.Text = $"将会使用：{JavaWorker.LastResult}"
                    HintArgumentJava.Theme = MyHint.Themes.Blue
                End If
            Case 3
                HintArgumentJava.Visibility = Visibility.Collapsed
                TextArgumentJavaRange.Visibility = Visibility.Collapsed
                PanArgumentJavaSelect.Visibility = Visibility.Visible
        End Select
    End Sub
    Private Sub HintArgumentJava_Click(sender As Object, e As MouseButtonEventArgs) Handles HintArgumentJava.Click
        If ComboArgumentJava.SelectedIndex = 2 Then PageInstanceOverall.OpenInstanceFolder(PageInstanceLeft.Instance)
    End Sub

#End Region

#Region "选择特定 Java 的下拉框"

    Public Sub UpdateJavaList()
        If ComboArgumentJavaSelect Is Nothing OrElse LabArgumentJava Is Nothing OrElse Not ComboArgumentJavaSelect.IsLoaded Then Return
        ComboArgumentJavaSelect.Items.Clear()

        If JavaListRefreshWorker.Running Then
            BtnArgumentJavaSearch.IsEnabled = False
            ComboArgumentJavaSelect.IsEnabled = False
            LabArgumentJava.Text = "搜索中 …"
            Return
        End If

        BtnArgumentJavaSearch.IsEnabled = True
        ComboArgumentJavaSelect.IsEnabled = True
        Dim SelectedJava As Java = Configs.JavaForced.Get(PageInstanceLeft.Instance.Config)

        If Not Configs.JavaList.Get().Any() Then
            LabArgumentJava.Text = "未找到 Java，点击以导入已有的 Java"
        ElseIf SelectedJava Is Nothing Then
            LabArgumentJava.Text = "点击选择 Java …"
        Else
            LabArgumentJava.Text = SelectedJava.ToString
        End If

        Try
            For Each JavaEntry In Configs.JavaList.Get()
                Dim JavaItem As New MyListItem With {
                    .FontSize = 13, .Height = 24, .IsScaleAnimationEnabled = False, .Type = MyListItem.CheckType.Clickable,
                    .Tag = JavaEntry, .Title = JavaEntry.ToString}
                AddHandler JavaItem.MouseLeftButtonUp, Sub(sender As Object, e As MouseButtonEventArgs) e.Handled = True
                AddHandler JavaItem.Click,
                Sub()
                    Configs.JavaForced.Set(JavaEntry, PageInstanceLeft.Instance.Config)
                    UpdateJavaList()
                    ComboArgumentJavaSelect.IsDropDownOpen = False
                End Sub
                ComboArgumentJavaSelect.Items.Add(JavaItem)
                Dim Buttons As New List(Of MyIconButton)

                Dim IsOfficial As Boolean = PathUtils.IsParentOf($"{Paths.AppData}.minecraft\runtime\", JavaEntry.Folder)
                Dim DeleteButton As New MyIconButton With {.Logo = Logo.IconButtonStop, .Height = 24, .Width = 24}
                DeleteButton.IsEnabled = Not IsOfficial
                DeleteButton.ToolTip = If(DeleteButton.IsEnabled, "从列表中移除", "无法移除官方 Java")
                AddHandler DeleteButton.Click, Sub() ManuallyRemoveJava(JavaEntry)
                ToolTipService.SetShowOnDisabled(DeleteButton, True)
                Buttons.Add(DeleteButton)

                Dim OpenButton As New MyIconButton With {.Logo = Logo.IconButtonOpen, .LogoScale = 1.1, .Height = 24, .Width = 24}
                OpenButton.ToolTip = "打开文件夹"
                AddHandler OpenButton.Click, Sub() OpenExplorer(JavaEntry.JavaExePath)
                Buttons.Add(OpenButton)
                JavaItem.Buttons = Buttons
            Next
            Dim ImportItem As New MyListItem With {
                .FontSize = 13, .Height = 24, .IsScaleAnimationEnabled = False, .Title = " 导入电脑中已有的 Java…", .Type = MyListItem.CheckType.Clickable}
            AddHandler ImportItem.MouseLeftButtonUp,
            Sub(sender As Object, e As MouseButtonEventArgs)
                e.Handled = True
                ManuallyImportJava()
            End Sub
            ComboArgumentJavaSelect.Items.Add(ImportItem)
        Catch ex As Exception
            Logger.Error(ex, "更新 Java 下拉框失败")
        End Try
    End Sub
    Private Sub BtnAdvanceJavaSearch_Click() Handles BtnArgumentJavaSearch.Click
        ManuallySearchJava()
    End Sub

#End Region

    Private Sub ComboArgumentIndieV2_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboArgumentIndieV2.SelectionChanged
        If AniControlEnabled <> 0 Then Return
        Static IsReverting As Boolean = False
        If IsReverting Then Return
        If MyMsgBox("调整版本隔离设置后，你需要游戏存档、Mod 等文件手动迁移到新的游戏文件夹中。" & vbCrLf &
                    "如果发现存档消失，把这项设置改回来就能恢复。" & vbCrLf &
                    "如果你不会迁移存档，不建议修改这项设置！",
                    "警告", "我知道我在做什么", "取消", IsWarn:=True) = 2 Then
            IsReverting = True
            If e.RemovedItems.Count > 0 Then ComboArgumentIndieV2.SelectedItem = e.RemovedItems(0)
            IsReverting = False
        End If

        Settings.Set("VersionArgumentIndieV2", sender.SelectedIndex = 0, Instance:=PageInstanceLeft.Instance)
    End Sub

    Private Sub TextAdvanceRun_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextAdvanceRun.TextChanged
        CheckAdvanceRunWait.Visibility = (TextAdvanceRun.Text <> "").ToVisibility
    End Sub

    Private Sub BtnSwitch_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSwitch.Click
        FrmMain.PageChange(FormMain.PageType.Setup, FormMain.PageSubType.SetupLaunch)
    End Sub

    Private Sub ReplaceEnter(sender As MyTextBox, e As TextChangedEventArgs) Handles TextAdvanceJvm.TextChanged, TextAdvanceGame.TextChanged
        Dim NewText = sender.Text.ReplaceLineEndings(" ", mergeMultiple:=True)
        If NewText = sender.Text Then Return
        Dim CaretIndex = sender.CaretIndex
        sender.Text = NewText
        sender.CaretIndex = Math.Max(0, CaretIndex - 1)
    End Sub

#Region "卡片：内容管理"

    Private Sub RefreshContentUI()
        If LabContentScreenshot Is Nothing Then Return
        Try
            Dim PathIndie As String = PageInstanceLeft.Instance.PathIndie
            LabContentScreenshot.Text = ContentSummary(PathIndie & "screenshots\", {".png", ".jpg", ".jpeg", ".bmp", ".gif"})
            LabContentResourcePack.Text = ContentSummary(PathIndie & "resourcepacks\", {".zip"})
            LabContentShaderPack.Text = ContentSummary(PathIndie & "shaderpacks\", {".zip"})
            LabContentSchematic.Text = ContentSummary(PathIndie & "schematics\", {".litematic", ".schematic", ".schem", ".nbt"})
        Catch ex As Exception
            Logger.Warn(ex, "刷新内容管理信息失败")
        End Try
    End Sub

    Private Shared Function ContentSummary(Folder As String, Extensions As String()) As String
        Try
            If Not DirectoryUtils.Exists(Folder) Then Return "文件夹不存在"
            Dim Count As Integer = 0
            Dim Size As Long = 0
            For Each File As String In DirectoryUtils.EnumerateFiles(Folder)
                If Not Extensions.Contains(IO.Path.GetExtension(File).ToLowerInvariant()) Then Continue For
                Count += 1
                Size += New IO.FileInfo(File).Length
            Next
            If Count = 0 Then Return "暂无文件"
            Return $"{Count} 个文件 · {FormatSize(Size)}"
        Catch ex As Exception
            Logger.Warn(ex, $"统计内容文件夹失败（{Folder}）")
            Return "读取失败"
        End Try
    End Function

    Private Shared Function FormatSize(Size As Long) As String
        If Size >= 1073741824L Then Return $"{Math.Round(Size / 1073741824.0, 1)} GB"
        If Size >= 1048576L Then Return $"{Math.Round(Size / 1048576.0, 1)} MB"
        If Size >= 1024L Then Return $"{Math.Round(Size / 1024.0, 0)} KB"
        Return $"{Size} B"
    End Function

    Private Sub BtnContentScreenshotOpen_Click() Handles BtnContentScreenshotOpen.Click
        OpenContentFolder("screenshots")
    End Sub
    Private Sub BtnContentResourcePackOpen_Click() Handles BtnContentResourcePackOpen.Click
        OpenContentFolder("resourcepacks")
    End Sub
    Private Sub BtnContentShaderPackOpen_Click() Handles BtnContentShaderPackOpen.Click
        OpenContentFolder("shaderpacks")
    End Sub
    Private Sub BtnContentSchematicOpen_Click() Handles BtnContentSchematicOpen.Click
        OpenContentFolder("schematics")
    End Sub

    Private Sub OpenContentFolder(FolderName As String)
        Try
            Dim Folder As String = PageInstanceLeft.Instance.PathIndie & FolderName & "\"
            DirectoryUtils.Create(Folder)
            OpenExplorer(Folder)
        Catch ex As Exception
            Logger.Error(ex, $"打开内容文件夹失败（{FolderName}）", LogBehavior.Alert)
        End Try
    End Sub

    Private Sub BtnContentSchematicPreview_Click() Handles BtnContentSchematicPreview.Click
        Dim Folder As String = PageInstanceLeft.Instance.PathIndie & "schematics\"
        Dim FileName As String = ""
        Try
            DirectoryUtils.Create(Folder)
            FileName = Dialogs.SelectFile("选择投影原理图", False, Folder,
                                          {({"litematic", "schematic", "schem", "nbt"}, "投影文件")}).FirstOrDefault()
        Catch ex As Exception
            Logger.Error(ex, "选择投影原理图失败", LogBehavior.Alert)
            Return
        End Try
        If String.IsNullOrEmpty(FileName) Then Return
        Hint("正在读取投影文件…")
        RunInNewThread(
        Sub()
            Try
                Dim Info As NbtPreview = ReadNbtPreview(FileName)
                Dim Text As String = BuildSchematicText(FileName, Info)
                RunInUi(Sub() MyMsgBox(Text, "投影原理图预览", HighLight:=False))
            Catch ex As Exception
                Logger.Error(ex, $"读取投影原理图失败（{FileName}）")
                RunInUi(Sub() Hint("读取投影原理图失败：" & ex.Message, HintType.Red))
            End Try
        End Sub, "投影原理图预览")
    End Sub

    Private Shared Function BuildSchematicText(FileName As String, Info As NbtPreview) As String
        Dim Lines As New List(Of String)
        Dim FileSize As String = ""
        Try
            FileSize = FormatSize(New IO.FileInfo(FileName).Length)
        Catch
        End Try
        Lines.Add($"文件：{IO.Path.GetFileName(FileName)}" & If(FileSize = "", "", $"（{FileSize}）"))

        Dim Name As String = Info.FirstValue("Metadata/Name")
        If Name <> "" Then Lines.Add("名称：" & Name)
        Dim Author As String = Info.FirstValue("Metadata/Author")
        If Author <> "" Then Lines.Add("作者：" & Author)
        Dim Description As String = Info.FirstValue("Metadata/Description")
        If Description <> "" Then
            If Description.Length > 160 Then Description = Description.Substring(0, 160) & "…"
            Lines.Add("描述：" & Description.ReplaceLineEndings(" "))
        End If

        Dim SizeText As String = ""
        If Info.Regions.Count > 0 Then
            Dim Region As String = Info.Regions(0)
            Dim X As String = Info.FirstValue($"Regions/{Region}/Size/x")
            Dim Y As String = Info.FirstValue($"Regions/{Region}/Size/y")
            Dim Z As String = Info.FirstValue($"Regions/{Region}/Size/z")
            If X <> "" AndAlso Y <> "" AndAlso Z <> "" Then SizeText = $"{AbsNumber(X)} × {AbsNumber(Y)} × {AbsNumber(Z)}"
        End If
        If SizeText = "" Then SizeText = Info.FirstValue("size")
        If SizeText = "" Then
            Dim W As String = Info.FirstValue("Width", "Schematic/Width")
            Dim H As String = Info.FirstValue("Height", "Schematic/Height")
            Dim L As String = Info.FirstValue("Length", "Schematic/Length")
            If W <> "" AndAlso H <> "" AndAlso L <> "" Then SizeText = $"{AbsNumber(W)} × {AbsNumber(H)} × {AbsNumber(L)}"
        End If
        If SizeText <> "" Then Lines.Add($"尺寸：{SizeText}（宽 × 高 × 长，单位：方块）")

        Dim RegionCount As String = Info.FirstValue("Metadata/RegionCount")
        If RegionCount <> "" Then Lines.Add($"区域数：{RegionCount}")
        Dim TotalVolume As String = Info.FirstValue("Metadata/TotalVolume")
        If TotalVolume <> "" Then Lines.Add($"总体积：{TotalVolume} 方块")
        Dim TotalBlocks As String = Info.FirstValue("Metadata/TotalBlocks")
        If TotalBlocks <> "" Then Lines.Add($"方块总数：{TotalBlocks}")

        If Lines.Count = 1 Then Lines.Add("未能识别该投影文件的尺寸信息，可能不是标准的投影或结构文件。")
        Return String.Join(vbCrLf, Lines)
    End Function

    Private Shared Function AbsNumber(Text As String) As String
        Try
            Return Math.Abs(CLng(Text)).ToString()
        Catch
            Return Text
        End Try
    End Function

    Private Shared Function ReadNbtPreview(FileName As String) As NbtPreview
        Using Reader As New NbtReader(FileName)
            Try
                Reader.ReadRoot()
            Catch ex As StopPreviewException

            End Try
            Return Reader.Info
        End Using
    End Function

    Private Class NbtPreview
        Public ReadOnly Values As New Dictionary(Of String, String)
        Public ReadOnly Regions As New List(Of String)

        Public Sub SetValue(Path As String, Value As String)
            Values(Path) = Value
        End Sub

        Public Function FirstValue(ParamArray Paths() As String) As String
            For Each Path As String In Paths
                If Values.ContainsKey(Path) Then Return Values(Path)
            Next
            Return ""
        End Function
    End Class

    Private Class StopPreviewException
        Inherits Exception
    End Class

    Private Class NbtReader
        Implements IDisposable

        Private ReadOnly Strm As IO.Stream
        Public ReadOnly Info As New NbtPreview()

        Public Sub New(FileName As String)
            Dim Raw As IO.Stream = IO.File.OpenRead(FileName)
            Dim Head(1) As Byte
            Dim ReadCount As Integer = Raw.Read(Head, 0, 2)
            Raw.Position = 0
            If ReadCount = 2 AndAlso Head(0) = &H1F AndAlso Head(1) = &H8B Then
                Strm = New IO.Compression.GZipStream(Raw, IO.Compression.CompressionMode.Decompress)
            Else
                Strm = Raw
            End If
        End Sub

        Public Sub ReadRoot()
            Dim RootType As Byte = ReadByte()
            If RootType <> 10 Then Throw New IO.InvalidDataException("不是有效的 NBT 文件")
            ReadString()
            ReadCompound("")
        End Sub

        Private Sub ReadCompound(Path As String)
            Do
                Dim TagType As Byte = ReadByte()
                If TagType = 0 Then Exit Do
                Dim TagName As String = ReadString()
                Dim ChildPath As String = If(Path = "", TagName, Path & "/" & TagName)
                Select Case TagType
                    Case 1, 2, 3, 4
                        Info.SetValue(ChildPath, ReadNumber(If(TagType = 1, 1, If(TagType = 2, 2, If(TagType = 3, 4, 8)))).ToString())
                    Case 5, 6

                        Skip(TagType, 0)
                    Case 7
                        SkipBytes(ReadNumber(4))
                    Case 8
                        Dim Value As String = ReadString()
                        Info.SetValue(ChildPath, Value)

                        If (Path = "" OrElse Path = "Schematic") AndAlso TagName = "Length" Then Throw New StopPreviewException()
                    Case 9
                        Dim ItemType As Byte = ReadByte()
                        Dim Count As Long = ReadNumber(4)
                        If Count < 0 OrElse Count > 100000000L Then Throw New IO.InvalidDataException("NBT 列表长度异常")
                        If Path = "" AndAlso TagName = "size" AndAlso ItemType = 3 AndAlso Count >= 3 Then
                            Info.SetValue(ChildPath, $"{ReadNumber(4)} × {ReadNumber(4)} × {ReadNumber(4)}")
                            If Count = 3 Then Throw New StopPreviewException()
                            For i As Long = 4 To Count
                                Skip(ItemType, 1)
                            Next
                        Else
                            For i As Long = 1 To Count
                                Skip(ItemType, 1)
                            Next
                        End If
                    Case 10
                        If Path = "Regions" Then

                            Info.Regions.Add(TagName)
                            ReadCompound(ChildPath)
                            Throw New StopPreviewException()
                        Else
                            ReadCompound(ChildPath)
                        End If
                    Case 11
                        SkipBytes(ReadNumber(4) * 4)
                    Case 12
                        SkipBytes(ReadNumber(4) * 8)
                    Case Else
                        Throw New IO.InvalidDataException($"未知的 NBT 标签类型 {TagType}")
                End Select
            Loop
        End Sub

        Private Function ReadByte() As Byte
            Return ReadBytes(1)(0)
        End Function

        Private Function ReadNumber(Size As Integer) As Long
            Dim Buffer As Byte() = ReadBytes(Size)
            Dim Value As Long = 0
            For i As Integer = 0 To Size - 1
                Value = (Value << 8) Or Buffer(i)
            Next
            If Size = 2 AndAlso Value >= 32768L Then Value -= 65536L
            If Size = 4 AndAlso Value >= 2147483648L Then Value -= 4294967296L
            Return Value
        End Function

        Private Function ReadString() As String
            Dim Length As Integer = CInt(ReadNumber(2))
            If Length < 0 OrElse Length > 1048576 Then Throw New IO.InvalidDataException("NBT 字符串长度异常")
            If Length = 0 Then Return ""
            Return System.Text.Encoding.UTF8.GetString(ReadBytes(Length))
        End Function

        Private Function ReadBytes(Count As Integer) As Byte()
            Dim Buffer(Count - 1) As Byte
            Dim Done As Integer = 0
            While Done < Count
                Dim N As Integer = Strm.Read(Buffer, Done, Count - Done)
                If N <= 0 Then Throw New IO.EndOfStreamException()
                Done += N
            End While
            Return Buffer
        End Function

        Private Sub SkipBytes(Count As Long)
            If Count <= 0 Then Return
            Const ChunkSize As Integer = 4096
            Dim Buffer(ChunkSize - 1) As Byte
            While Count > 0
                Dim Want As Integer = CInt(Math.Min(Count, ChunkSize))

                Dim Done As Integer = 0
                While Done < Want
                    Dim N As Integer = Strm.Read(Buffer, Done, Want - Done)
                    If N <= 0 Then Throw New IO.EndOfStreamException()
                    Done += N
                End While
                Count -= Want
            End While
        End Sub

        Private Sub Skip(TagType As Byte, Depth As Integer)
            If Depth > 256 Then Throw New IO.InvalidDataException("NBT 嵌套层级过深")
            Select Case TagType
                Case 1, 2, 3, 4
                    ReadNumber(If(TagType = 1, 1, If(TagType = 2, 2, If(TagType = 3, 4, 8))))
                Case 5
                    ReadBytes(4)
                Case 6
                    ReadBytes(8)
                Case 7
                    SkipBytes(ReadNumber(4))
                Case 8
                    ReadString()
                Case 9
                    Dim ItemType As Byte = ReadByte()
                    Dim Count As Long = ReadNumber(4)
                    If Count < 0 OrElse Count > 100000000L Then Throw New IO.InvalidDataException("NBT 列表长度异常")
                    For i As Long = 1 To Count
                        Skip(ItemType, Depth + 1)
                    Next
                Case 10
                    Do
                        Dim ChildType As Byte = ReadByte()
                        If ChildType = 0 Then Exit Do
                        ReadString()
                        Skip(ChildType, Depth + 1)
                    Loop
                Case 11
                    SkipBytes(ReadNumber(4) * 4)
                Case 12
                    SkipBytes(ReadNumber(4) * 8)
                Case Else
                    Throw New IO.InvalidDataException($"未知的 NBT 标签类型 {TagType}")
            End Select
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Strm.Dispose()
        End Sub
    End Class

#End Region

End Class
