Public Class PageDownloadInstall

    Public Sub ControlInit()
        DisabledPageAnimControls.Add(BtnStart)
        LoadOptiFine.State = DlOptiFineListLoader
        LoadLiteLoader.State = DlLiteLoaderListLoader
        LoadFabric.State = DlFabricListLoader
        LoadFabricApi.State = DlFabricApiLoader
        LoadNeoForge.State = DlNeoForgeListLoader
        LoadOptiFabric.State = DlOptiFabricLoader

        McDownloadForgeRecommendedRefresh()
    End Sub

    Public Overridable Sub LoaderInit() Handles Me.Initialized
        ControlInit()
        If RestrictMcDownload Then
            PageLoaderInit(LoadMinecraft, PanLoad, PanAllBack, Nothing, New LoaderTask(Of String, DlClientListResult)("关闭下载", Sub() Throw New Exception("为遵守相关法律法规，PCL 不再提供 MC 下载功能，非常抱歉 qwq……")), AddressOf LoadMinecraft_OnFinish)
        Else
            PageLoaderInit(LoadMinecraft, PanLoad, PanAllBack, Nothing, DlClientListLoader, AddressOf LoadMinecraft_OnFinish)
        End If
    End Sub

    Private IsLoad As Boolean = False
    Public Overridable Sub Init() Handles Me.Loaded
        If IsLoad Then
            IsSelectNameEdited = False
            ExitSelectPage()
            If DlClientListLoader.State = LoadState.Finished Then TrySelectWaitingVersion()
        End If
        IsLoad = True
        PanBack.ScrollToHome()
        DlOptiFineListLoader.Start()
        DlLiteLoaderListLoader.Start()
        DlFabricListLoader.Start()
        DlNeoForgeListLoader.Start()

        TextSelectName.ValidateRules = New ObjectModel.Collection(Of Validate) From {New ValidateFolderName(McFolderSelected & "versions")}
        TextSelectName.Validate()
        ReloadSelected()
    End Sub

#Region "页面切换"

    Public IsInSelectPage As Boolean = False

    Public Sub PrepareSelectPage()
        IsInSelectPage = True
        PanInner.Margin = New Thickness(25, 10, 25, 40)
        PanSelect.Visibility = Visibility.Visible
        PanSelect.IsHitTestVisible = True
        PanMinecraft.IsHitTestVisible = False
        PanBack.IsHitTestVisible = False
        PanBack.ScrollToHome()

        DisabledPageAnimControls.Remove(BtnStart)
        BtnStart.Show = True
        For Each Card In {CardOptiFine, CardLiteLoader, CardForge, CardNeoForge, CardFabric, CardFabricApi, CardOptiFabric}
            Card.IsSwapped = True
        Next

        For Each Control In GetAllAnimControls(PanSelect)
            Control.Opacity = 1
            If Control.RenderTransform Is Nothing OrElse TypeOf Control.RenderTransform Is TranslateTransform Then
                Control.RenderTransform = New TranslateTransform
            End If
        Next
    End Sub

    Private Sub EnterSelectPage()
        If IsInSelectPage Then Return
        AutoSelectedFabricApi = False
        AutoSelectedOptiFabric = False
        PrepareSelectPage()

        If McVersion.IsStandardFormat(VanillaName) Then
            Dim ForgeLoader = New LoaderTask(Of String, List(Of DlForgeVersionEntry))("DlForgeVersion " & VanillaName, AddressOf DlForgeVersionMain)
            LoadForge.State = ForgeLoader
            ForgeLoader.Start(VanillaName)
        End If

        DlFabricApiLoader.Start()
        DlOptiFabricLoader.Start()

        AniStart({
            AaOpacity(PanMinecraft, -PanMinecraft.Opacity, 70, 10),
            AaTranslateX(PanMinecraft, -50 - CType(PanMinecraft.RenderTransform, TranslateTransform).X, 90, 10),
            AaCode(
            Sub()
                PanBack.ScrollToHome()
                ReloadComponentLists()
                PanMinecraft.Visibility = Visibility.Collapsed
            End Sub, After:=True),
            AaOpacity(PanSelect, 1 - PanSelect.Opacity, 70, 100),
            AaTranslateX(PanSelect, -CType(PanSelect.RenderTransform, TranslateTransform).X, 160, 100, Ease:=New AniEaseOutFluent(AniEasePower.ExtraStrong)),
            AaCode(Sub() PanBack.IsHitTestVisible = True,, True)
        }, "FrmDownloadInstall SelectPageSwitch", True)
    End Sub

    Public Sub ReloadComponentLists()
        TextSelectName.Validate()
        OptiFine_Loaded()
        LiteLoader_Loaded()
        Forge_Loaded()
        NeoForge_Loaded()
        Fabric_Loaded()
        FabricApi_Loaded()
        OptiFabric_Loaded()
        ReloadSelected()
    End Sub

    Public Sub ExitSelectPage() Handles BtnBack.Click
        If Not IsInSelectPage Then Return
        IsInSelectPage = False

        PanInner.Margin = New Thickness(25, 10, 25, 25)

        DisabledPageAnimControls.Add(BtnStart)
        BtnStart.Show = False
        ClearSelected()
        PanMinecraft.Visibility = Visibility.Visible
        PanSelect.IsHitTestVisible = False
        PanMinecraft.IsHitTestVisible = True
        PanBack.IsHitTestVisible = False
        PanBack.ScrollToHome()

        AniStart({
            AaOpacity(PanSelect, -PanSelect.Opacity, 70, 10),
            AaTranslateX(PanSelect, 50 - CType(PanSelect.RenderTransform, TranslateTransform).X, 90, 10),
            AaCode(Sub() PanBack.ScrollToHome(), After:=True),
            AaOpacity(PanMinecraft, 1 - PanMinecraft.Opacity, 70, 100),
            AaTranslateX(PanMinecraft, -CType(PanMinecraft.RenderTransform, TranslateTransform).X, 160, 100, Ease:=New AniEaseOutFluent(AniEasePower.ExtraStrong)),
            AaCode(
            Sub()
                PanSelect.Visibility = Visibility.Collapsed
                PanBack.IsHitTestVisible = True
            End Sub,, True)
        }, "FrmDownloadInstall SelectPageSwitch")
    End Sub
    Public Overridable Sub MinecraftSelected(sender As MyListItem, e As MouseButtonEventArgs)
        VanillaName = sender.Title
        VanillaData = sender.Tag
        VanillaIcon = sender.Logo
        LabMinecraftVersion.Text = VanillaName
        EnterSelectPage()
    End Sub

#End Region

#Region "选择"

    Public VanillaName As String
    Public VanillaData As JObject
    Public VanillaIcon As String
    Private ReadOnly Property VanillaDrop As Integer
        Get
            Return McVersion.VersionToDrop(VanillaName)
        End Get
    End Property

    Public SelectedOptiFine As DlOptiFineListEntry = Nothing
    Public SelectedLiteLoader As DlLiteLoaderListEntry = Nothing
    Public SelectedForge As DlForgeVersionEntry = Nothing
    Public SelectedNeoForge As DlNeoForgeListEntry = Nothing
    Public SelectedFabric As String = Nothing
    Public SelectedFabricApi As ResourceVersion = Nothing
    Public SelectedOptiFabric As ResourceVersion = Nothing

    Public Overridable Sub ReloadSelected() Handles CardOptiFine.Swap, LoadOptiFine.StateChanged, CardForge.Swap, LoadForge.StateChanged, CardNeoForge.Swap, LoadNeoForge.StateChanged, CardFabric.Swap, LoadFabric.StateChanged, CardFabricApi.Swap, LoadFabricApi.StateChanged, CardOptiFabric.Swap, LoadOptiFabric.StateChanged, CardLiteLoader.Swap, LoadLiteLoader.StateChanged
        Static Ongoing As Boolean = False
        If VanillaName Is Nothing OrElse Ongoing Then Return
        Ongoing = True

        SelectNameUpdate()
        ImgLogo.Source = GetSelectLogo()
        ImgMinecraft.Source = VanillaIcon

        Dim OptiFineError As String = LoadOptiFineGetError()
        CardOptiFine.MainSwap.Visibility = If(OptiFineError Is Nothing, Visibility.Visible, Visibility.Collapsed)
        If OptiFineError IsNot Nothing Then CardOptiFine.IsSwapped = True
        SetPanelVisibility(PanOptiFineInfo, CardOptiFine.IsSwapped)
        If SelectedOptiFine Is Nothing Then
            BtnOptiFineClear.Visibility = Visibility.Collapsed
            ImgOptiFine.Visibility = Visibility.Collapsed
            LabOptiFine.Text = If(OptiFineError, "可以添加")
            LabOptiFine.Foreground = ColorGray4
        Else
            BtnOptiFineClear.Visibility = Visibility.Visible
            ImgOptiFine.Visibility = Visibility.Visible
            LabOptiFine.Text = SelectedOptiFine.DisplayName.Replace(VanillaName & " ", "")
            LabOptiFine.Foreground = ColorGray1
        End If

        If VanillaDrop >= 130 Then
            CardLiteLoader.Visibility = Visibility.Collapsed
        Else
            CardLiteLoader.Visibility = Visibility.Visible
            Dim LiteLoaderError As String = LoadLiteLoaderGetError()
            CardLiteLoader.MainSwap.Visibility = If(LiteLoaderError Is Nothing, Visibility.Visible, Visibility.Collapsed)
            If LiteLoaderError IsNot Nothing Then CardLiteLoader.IsSwapped = True
            SetPanelVisibility(PanLiteLoaderInfo, CardLiteLoader.IsSwapped)
            If SelectedLiteLoader Is Nothing Then
                BtnLiteLoaderClear.Visibility = Visibility.Collapsed
                ImgLiteLoader.Visibility = Visibility.Collapsed
                LabLiteLoader.Text = If(LiteLoaderError, "可以添加")
                LabLiteLoader.Foreground = ColorGray4
            Else
                BtnLiteLoaderClear.Visibility = Visibility.Visible
                ImgLiteLoader.Visibility = Visibility.Visible
                LabLiteLoader.Text = SelectedLiteLoader.Inherit
                LabLiteLoader.Foreground = ColorGray1
            End If
        End If

        If Not McVersion.IsStandardFormat(VanillaName) Then
            CardForge.Visibility = Visibility.Collapsed
        Else
            CardForge.Visibility = Visibility.Visible
            Dim ForgeError As String = LoadForgeGetError()
            CardForge.MainSwap.Visibility = If(ForgeError Is Nothing, Visibility.Visible, Visibility.Collapsed)
            If ForgeError IsNot Nothing Then CardForge.IsSwapped = True
            SetPanelVisibility(PanForgeInfo, CardForge.IsSwapped)
            If SelectedForge Is Nothing Then
                BtnForgeClear.Visibility = Visibility.Collapsed
                ImgForge.Visibility = Visibility.Collapsed
                LabForge.Text = If(ForgeError, "可以添加")
                LabForge.Foreground = ColorGray4
            Else
                BtnForgeClear.Visibility = Visibility.Visible
                ImgForge.Visibility = Visibility.Visible
                LabForge.Text = SelectedForge.VersionName
                LabForge.Foreground = ColorGray1
            End If
        End If

        If VanillaData("releaseTime").ToObject(Of Date) < New Date(2023, 6, 11) Then
            CardNeoForge.Visibility = Visibility.Collapsed
        Else
            CardNeoForge.Visibility = Visibility.Visible
            Dim NeoForgeError As String = LoadNeoForgeGetError()
            CardNeoForge.MainSwap.Visibility = If(NeoForgeError Is Nothing, Visibility.Visible, Visibility.Collapsed)
            If NeoForgeError IsNot Nothing Then CardNeoForge.IsSwapped = True
            SetPanelVisibility(PanNeoForgeInfo, CardNeoForge.IsSwapped)
            If SelectedNeoForge Is Nothing Then
                BtnNeoForgeClear.Visibility = Visibility.Collapsed
                ImgNeoForge.Visibility = Visibility.Collapsed
                LabNeoForge.Text = If(NeoForgeError, "可以添加")
                LabNeoForge.Foreground = ColorGray4
            Else
                BtnNeoForgeClear.Visibility = Visibility.Visible
                ImgNeoForge.Visibility = Visibility.Visible
                LabNeoForge.Text = SelectedNeoForge.VersionName
                LabNeoForge.Foreground = ColorGray1
            End If
        End If

        If VanillaDrop <= 130 Then
            CardFabric.Visibility = Visibility.Collapsed
        Else
            CardFabric.Visibility = Visibility.Visible
            Dim FabricError As String = LoadFabricGetError()
            CardFabric.MainSwap.Visibility = If(FabricError Is Nothing, Visibility.Visible, Visibility.Collapsed)
            If FabricError IsNot Nothing Then CardFabric.IsSwapped = True
            SetPanelVisibility(PanFabricInfo, CardFabric.IsSwapped)
            If SelectedFabric Is Nothing Then
                BtnFabricClear.Visibility = Visibility.Collapsed
                ImgFabric.Visibility = Visibility.Collapsed
                LabFabric.Text = If(FabricError, "可以添加")
                LabFabric.Foreground = ColorGray4
            Else
                BtnFabricClear.Visibility = Visibility.Visible
                ImgFabric.Visibility = Visibility.Visible
                LabFabric.Text = SelectedFabric.Replace("+build", "")
                LabFabric.Foreground = ColorGray1
            End If
        End If

        If SelectedFabric Is Nothing Then
            CardFabricApi.Visibility = Visibility.Collapsed
        Else
            CardFabricApi.Visibility = Visibility.Visible
            Dim FabricApiError As String = LoadFabricApiGetError()
            CardFabricApi.MainSwap.Visibility = If(FabricApiError Is Nothing, Visibility.Visible, Visibility.Collapsed)
            If FabricApiError IsNot Nothing OrElse SelectedFabric Is Nothing Then CardFabricApi.IsSwapped = True
            SetPanelVisibility(PanFabricApiInfo, CardFabricApi.IsSwapped)
            If SelectedFabricApi Is Nothing Then
                BtnFabricApiClear.Visibility = Visibility.Collapsed
                ImgFabricApi.Visibility = Visibility.Collapsed
                LabFabricApi.Text = If(FabricApiError, "可以添加")
                LabFabricApi.Foreground = ColorGray4
            Else
                BtnFabricApiClear.Visibility = Visibility.Visible
                ImgFabricApi.Visibility = Visibility.Visible
                LabFabricApi.Text = SelectedFabricApi.Display.Split("]")(1).Replace("Fabric API ", "").Replace(" build ", ".").Split("+").First.Trim
                LabFabricApi.Foreground = ColorGray1
            End If
        End If

        If SelectedFabric Is Nothing OrElse SelectedOptiFine Is Nothing Then
            CardOptiFabric.Visibility = Visibility.Collapsed
        Else
            CardOptiFabric.Visibility = Visibility.Visible
            Dim OptiFabricError As String = LoadOptiFabricGetError()
            CardOptiFabric.MainSwap.Visibility = If(OptiFabricError Is Nothing, Visibility.Visible, Visibility.Collapsed)
            If OptiFabricError IsNot Nothing OrElse SelectedFabric Is Nothing Then CardOptiFabric.IsSwapped = True
            SetPanelVisibility(PanOptiFabricInfo, CardOptiFabric.IsSwapped)
            If SelectedOptiFabric Is Nothing Then
                BtnOptiFabricClear.Visibility = Visibility.Collapsed
                ImgOptiFabric.Visibility = Visibility.Collapsed
                LabOptiFabric.Text = If(OptiFabricError, "可以添加")
                LabOptiFabric.Foreground = ColorGray4
            Else
                BtnOptiFabricClear.Visibility = Visibility.Visible
                ImgOptiFabric.Visibility = Visibility.Visible
                LabOptiFabric.Text = SelectedOptiFabric.Display.Lower.Replace("optifabric-", "").Replace(".jar", "").Trim.TrimStart("v")
                LabOptiFabric.Foreground = ColorGray1
            End If
        End If

        If SelectedFabric IsNot Nothing AndAlso SelectedFabricApi Is Nothing Then
            HintFabricAPI.Visibility = Visibility.Visible
        Else
            HintFabricAPI.Visibility = Visibility.Collapsed
        End If
        If SelectedFabric IsNot Nothing AndAlso SelectedOptiFine IsNot Nothing AndAlso SelectedOptiFabric Is Nothing Then
            If VanillaDrop >= 140 AndAlso VanillaDrop <= 150 Then
                HintOptiFabric.Visibility = Visibility.Collapsed
                HintOptiFabricOld.Visibility = Visibility.Visible
            Else
                HintOptiFabric.Visibility = Visibility.Visible
                HintOptiFabricOld.Visibility = Visibility.Collapsed
            End If
        Else
            HintOptiFabric.Visibility = Visibility.Collapsed
            HintOptiFabricOld.Visibility = Visibility.Collapsed
        End If
        If VanillaDrop >= 160 AndAlso SelectedOptiFine IsNot Nothing AndAlso
           (SelectedForge IsNot Nothing OrElse SelectedFabric IsNot Nothing) Then
            HintModOptiFine.Visibility = Visibility.Visible
        Else
            HintModOptiFine.Visibility = Visibility.Collapsed
        End If

        Ongoing = False
    End Sub

    Public Sub ClearSelected()
        VanillaName = Nothing
        VanillaData = Nothing
        VanillaIcon = Nothing
        SelectedOptiFine = Nothing
        SelectedLiteLoader = Nothing
        SelectedForge = Nothing
        SelectedNeoForge = Nothing
        SelectedFabric = Nothing
        SelectedFabricApi = Nothing
        SelectedOptiFabric = Nothing
        For Each ClearButton In {BtnOptiFineClear, BtnLiteLoaderClear, BtnForgeClear, BtnNeoForgeClear,
                                 BtnFabricClear, BtnFabricApiClear, BtnOptiFabricClear}
            ClearButton.Visibility = Visibility.Collapsed
        Next
    End Sub

    Private Sub SetPanelVisibility(Panel As Grid, IsVisible As Boolean)
        If Panel.Tag = IsVisible.ToString Then Return
        Panel.Tag = IsVisible.ToString
        If IsVisible Then
            AniStart({
                AaTranslateY(Panel, -CType(Panel.RenderTransform, TranslateTransform).Y, 150, Ease:=New AniEaseOutFluent),
                AaOpacity(Panel, 1 - Panel.Opacity, 60)
            }, "PageDownloadInstall Visibility " & Panel.Name)
        Else
            AniStart({
                AaTranslateY(Panel, 6 - CType(Panel.RenderTransform, TranslateTransform).Y, 60),
                AaOpacity(Panel, -Panel.Opacity, 60)
            }, "PageDownloadInstall Visibility " & Panel.Name)
        End If
    End Sub

    Public Overridable Function GetSelectLogo() As String
        If SelectedFabric IsNot Nothing Then
            Return "pack://application:,,,/images/Blocks/Fabric.png"
        ElseIf SelectedForge IsNot Nothing Then
            Return "pack://application:,,,/images/Blocks/Anvil.png"
        ElseIf SelectedNeoForge IsNot Nothing Then
            Return "pack://application:,,,/images/Blocks/NeoForge.png"
        ElseIf SelectedLiteLoader IsNot Nothing Then
            Return "pack://application:,,,/images/Blocks/Egg.png"
        ElseIf SelectedOptiFine IsNot Nothing Then
            Return "pack://application:,,,/images/Blocks/GrassPath.png"
        Else
            Return VanillaIcon
        End If
    End Function

    Public Function GetSelectName() As String
        Dim Name As String = VanillaName
        If SelectedFabric IsNot Nothing Then Name += "-Fabric " & SelectedFabric.BeforeFirst("+")
        If SelectedForge IsNot Nothing Then Name += "-Forge_" & SelectedForge.VersionName
        If SelectedNeoForge IsNot Nothing Then Name += "-NeoForge_" & SelectedNeoForge.VersionName.BeforeFirst("+")
        If SelectedLiteLoader IsNot Nothing Then Name += "-LiteLoader"
        If SelectedOptiFine IsNot Nothing Then Name += "-OptiFine_" & SelectedOptiFine.DisplayName.Replace(VanillaName & " ", "").Replace(" ", "_")
        Return Name
    End Function
    Public IsSelectNameEdited As Boolean = False
    Private Sub SelectNameUpdate()
        If IsSelectNameEdited Then Return
        TextSelectName.Text = GetSelectName()
        IsSelectNameEdited = False
    End Sub
    Private Sub TextSelectName_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextSelectName.TextChanged
        IsSelectNameEdited = True
    End Sub
    Private Sub TextSelectName_ValidateChanged(sender As Object, e As EventArgs) Handles TextSelectName.ValidateChanged
        BtnStart.IsEnabled = TextSelectName.IsValidated
        HintSelectName.Text = TextSelectName.ValidateResult
        HintSelectName.Visibility = If(TextSelectName.IsValidated, Visibility.Collapsed, Visibility.Visible)
    End Sub

#End Region

#Region "加载器"
    Public MinecraftVersions As JArray
    Private Sub LoadMinecraft_OnFinish()
        MinecraftVersions = CType(DlClientListLoader.Output.Value("versions").DeepClone(), JArray)
        ShowMinecraftVersionList()
        TrySelectWaitingVersion()
    End Sub

    Public Overridable Sub ShowMinecraftVersionList()
        ExitSelectPage()
        Try
            Dim Dict As New Dictionary(Of String, List(Of JObject)) From {
                {"正式版", New List(Of JObject)}, {"预览版", New List(Of JObject)}, {"远古版", New List(Of JObject)}, {"愚人节版", New List(Of JObject)}
            }
            For Each Version As JObject In MinecraftVersions

                Dim Type As String = Version("type")
                Select Case Type
                    Case "release"
                        Type = "正式版"
                    Case "snapshot"
                        Type = "预览版"

                        If Version("id").ToString.StartsWithF("1.") AndAlso
                            Not Version("id").ToString.Lower.Contains("combat") AndAlso
                            Not Version("id").ToString.Lower.Contains("rc") AndAlso
                            Not Version("id").ToString.Lower.Contains("experimental") AndAlso
                            Not Version("id").ToString.Lower.Contains("pre") Then
                            Type = "正式版"
                            Version("type") = "release"
                        End If

                        Select Case Version("id").ToString.Lower
                            Case "20w14infinite", "20w14∞"
                                Type = "愚人节版"
                                Version("id") = "20w14∞"
                                Version("type") = "special"
                                Version.Add("lore", GetMcFoolName(Version("id")))
                            Case "3d shareware v1.34", "1.rv-pre1", "15w14a", "2.0", "22w13oneblockatatime", "23w13a_or_b", "24w14potato", "25w14craftmine", "26w14a"
                                Type = "愚人节版"
                                Version("type") = "special"
                                Version.Add("lore", GetMcFoolName(Version("id")))
                            Case Else
                                Dim ReleaseDate = Version("releaseTime").Value(Of Date).ToUniversalTime().AddHours(2)
                                If ReleaseDate.Month = 4 AndAlso ReleaseDate.Day = 1 Then
                                    Type = "愚人节版"
                                    Version("type") = "special"
                                End If
                        End Select
                    Case "special"

                        Type = "愚人节版"
                    Case Else
                        Type = "远古版"
                End Select

                Dict(Type).Add(Version)
            Next

            For Each Pair In Dict.ToList
                Dict(Pair.Key) = Pair.Value.OrderByDescending(Function(j) j("releaseTime").Value(Of Date)).ToList
            Next

            PanMinecraft.Children.Clear()

            Dim CardInfo As New MyCard With {.Title = "最新版本", .Margin = New Thickness(0, 15, 0, 15)}
            Dim PinnedVersions As New List(Of JObject)
            Dim Release As JObject = Dict("正式版")(0).DeepClone()
            Release("lore") = "最新正式版，发布于 " & Release("releaseTime").Value(Of Date).ToString("yyyy'/'MM'/'dd HH':'mm")
            PinnedVersions.Add(Release)
            If Dict("正式版")(0)("releaseTime").Value(Of Date) < Dict("预览版")(0)("releaseTime").Value(Of Date) Then
                Dim Snapshot As JObject = Dict("预览版")(0).DeepClone()
                Snapshot("lore") = "最新预览版，发布于 " & Snapshot("releaseTime").Value(Of Date).ToString("yyyy'/'MM'/'dd HH':'mm")
                PinnedVersions.Add(Snapshot)
            End If
            Dim PanInfo As New StackPanel With {.Margin = New Thickness(20, MyCard.SwapedHeight, 18, 0), .VerticalAlignment = VerticalAlignment.Top, .RenderTransform = New TranslateTransform(0, 0)}
            CardInfo.Children.Add(PanInfo)
            CardInfo.SwapControl = PanInfo
            CardInfo.SetContentBuilder(PinnedVersions,
                Function(Version) McDownloadListItem(Version, Sub(sender, e) MinecraftSelected(sender, e), False))
            CardInfo.BuildContent()
            PanMinecraft.Children.Insert(0, CardInfo)

            For Each Pair As KeyValuePair(Of String, List(Of JObject)) In Dict
                If Not Pair.Value.Any() Then Continue For

                Dim NewCard As New MyCard With {.Title = Pair.Key, .Margin = New Thickness(0, 0, 0, 15)}
                Dim NewStack As New StackPanel With {.Margin = New Thickness(20, MyCard.SwapedHeight, 18, 0), .VerticalAlignment = VerticalAlignment.Top, .RenderTransform = New TranslateTransform(0, 0)}
                NewCard.Children.Add(NewStack)
                NewCard.SwapControl = NewStack
                NewCard.SetContentBuilder(Pair.Value,
                    Function(Version) McDownloadListItem(Version, Sub(sender, e) MinecraftSelected(sender, e), False))
                NewCard.IsSwapped = True
                PanMinecraft.Children.Add(NewCard)
            Next
        Catch ex As Exception
            Logger.Error(ex, "可视化安装版本列表出错")
        End Try
    End Sub
    Private Sub TrySelectWaitingVersion()
        If VersionWaitingSelect Is Nothing Then Return
        For Each Version As JObject In MinecraftVersions
            If Version("id").ToString <> VersionWaitingSelect Then Continue For
            Logger.Info($"自动选择 MC 版本：{VersionWaitingSelect}")
            MinecraftSelected(McDownloadListItem(Version, Nothing, False).Init(), Nothing)
            VersionWaitingSelect = Nothing
            Return
        Next
    End Sub

    Public Shared VersionWaitingSelect As String = Nothing

#End Region

#Region "OptiFine 列表"

    Public Function LoadOptiFineGetError() As String
        If SelectedNeoForge IsNot Nothing Then Return "与 NeoForge 不兼容"

        If SelectedForge IsNot Nothing AndAlso
            McUtils.CompareVersionGE(VanillaName, "1.13") AndAlso McUtils.CompareVersionGE("1.14.3", VanillaName) Then
            Return "与 Forge 不兼容"
        End If

        If SelectedFabric IsNot Nothing AndAlso
            McUtils.CompareVersion(VanillaName, "1.20.4") > 0 Then
            Return "与 Fabric 不兼容"
        End If

        If GetLoaderError(LoadOptiFine) IsNot Nothing Then Return GetLoaderError(LoadOptiFine)

        Dim HasAny As Boolean = False
        Dim HasRequiredVersion As Boolean = False
        For Each OptiFineVersion As DlOptiFineListEntry In DlOptiFineListLoader.Output.Value
            If Not OptiFineVersion.DisplayName.StartsWithF(VanillaName & " ") Then Continue For
            HasAny = True
            If SelectedForge Is Nothing Then Return Nothing
            If IsOptiFineSuitForForge(OptiFineVersion, SelectedForge) Then Return Nothing
            If OptiFineVersion.RequiredForgeVersion IsNot Nothing Then HasRequiredVersion = True
        Next
        If Not HasAny Then
            Return "无"
        ElseIf HasRequiredVersion Then
            Return "仅兼容特定版本的 Forge"
        Else
            Return "与 Forge 不兼容"
        End If
    End Function

    Public Shared Function IsOptiFineSuitForForge(OptiFine As DlOptiFineListEntry, Forge As DlForgeVersionEntry) As Boolean
        If Forge.Inherit <> OptiFine.Inherit Then Return False
        If OptiFine.RequiredForgeVersion Is Nothing Then Return False
        If String.IsNullOrWhiteSpace(OptiFine.RequiredForgeVersion) Then Return True
        If OptiFine.RequiredForgeVersion.Contains(".") Then
            Return McUtils.CompareVersion(Forge.Version.ToString, OptiFine.RequiredForgeVersion) = 0
        Else
            Return Forge.Version.Revision = OptiFine.RequiredForgeVersion
        End If
    End Function

    Private Sub CardOptiFine_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardOptiFine.PreviewSwap
        If LoadOptiFineGetError() IsNot Nothing Then e.Handled = True
    End Sub

    Private Sub OptiFine_Loaded() Handles LoadOptiFine.StateChanged
        Try
            If DlOptiFineListLoader.State <> LoadState.Finished Then Return

            Dim Versions As New List(Of DlOptiFineListEntry)
            For Each Version As DlOptiFineListEntry In DlOptiFineListLoader.Output.Value
                If SelectedForge IsNot Nothing AndAlso Not IsOptiFineSuitForForge(Version, SelectedForge) Then Continue For
                If Version.DisplayName.StartsWithF(VanillaName & " ") Then Versions.Add(Version)
            Next
            If Not Versions.Any() Then Return

            Versions = Versions.SortByComparison(
            Function(Left As DlOptiFineListEntry, Right As DlOptiFineListEntry) As Boolean
                If Not Left.IsPreview AndAlso Right.IsPreview Then Return True
                If Left.IsPreview AndAlso Not Right.IsPreview Then Return False
                Return McUtils.CompareVersionGE(Left.DisplayName, Right.DisplayName)
            End Function)

            PanOptiFine.Children.Clear()
            For Each Version In Versions
                PanOptiFine.Children.Add(OptiFineDownloadListItem(Version, AddressOf OptiFine_Selected, False))
            Next
        Catch ex As Exception
            Logger.Error(ex, "可视化 OptiFine 安装版本列表出错")
        End Try
    End Sub

    Private Sub OptiFine_Selected(sender As MyListItem, e As EventArgs)
        SelectedOptiFine = sender.Tag
        If SelectedForge IsNot Nothing AndAlso Not IsOptiFineSuitForForge(SelectedOptiFine, SelectedForge) Then SelectedForge = Nothing
        OptiFabric_Loaded()
        Forge_Loaded()
        NeoForge_Loaded()
        CardOptiFine.IsSwapped = True
        ReloadSelected()
    End Sub
    Private Sub OptiFine_Clear(sender As Object, e As EventArgs) Handles BtnOptiFineClear.Click
        SelectedOptiFine = Nothing
        SelectedOptiFabric = Nothing
        BtnOptiFabricClear.Visibility = Visibility.Collapsed
        AutoSelectedOptiFabric = False
        CardOptiFine.IsSwapped = True
        Forge_Loaded()
        NeoForge_Loaded()
        ReloadSelected()
    End Sub

#End Region

#Region "LiteLoader 列表"

    Public Function LoadLiteLoaderGetError() As String

        If GetLoaderError(LoadLiteLoader) IsNot Nothing Then Return GetLoaderError(LoadLiteLoader)

        Return If(DlLiteLoaderListLoader.Output.Value.Any(Function(v) v.Inherit = VanillaName), Nothing, "无")
    End Function

    Private Sub CardLiteLoader_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardLiteLoader.PreviewSwap
        If LoadLiteLoaderGetError() IsNot Nothing Then e.Handled = True
    End Sub

    Private Sub LiteLoader_Loaded() Handles LoadLiteLoader.StateChanged
        Try
            If DlLiteLoaderListLoader.State <> LoadState.Finished Then Return

            Dim Versions As New List(Of DlLiteLoaderListEntry)
            For Each Version As DlLiteLoaderListEntry In DlLiteLoaderListLoader.Output.Value
                If Version.Inherit = VanillaName Then Versions.Add(Version)
            Next
            If Not Versions.Any() Then Return

            PanLiteLoader.Children.Clear()
            For Each Version In Versions
                PanLiteLoader.Children.Add(LiteLoaderDownloadListItem(Version, AddressOf LiteLoader_Selected, False))
            Next
        Catch ex As Exception
            Logger.Error(ex, "可视化 LiteLoader 安装版本列表出错")
        End Try
    End Sub

    Private Sub LiteLoader_Selected(sender As MyListItem, e As EventArgs)
        SelectedLiteLoader = sender.Tag
        CardLiteLoader.IsSwapped = True
        ReloadSelected()
    End Sub
    Private Sub LiteLoader_Clear(sender As Object, e As EventArgs) Handles BtnLiteLoaderClear.Click
        SelectedLiteLoader = Nothing
        CardLiteLoader.IsSwapped = True
        ReloadSelected()
    End Sub

#End Region

#Region "Forge 列表"

    Public Function LoadForgeGetError() As String
        If McUtils.CompareVersionGE("1.5.1", VanillaName) AndAlso McUtils.CompareVersionGE(VanillaName, "1.1") Then Return "无"

        If GetLoaderError(LoadForge) IsNot Nothing Then Return GetLoaderError(LoadForge)
        Dim Loader As LoaderTask(Of String, List(Of DlForgeVersionEntry)) = LoadForge.State
        If VanillaName <> Loader.Input Then Return "获取中……"

        For Each Version In Loader.Output
            If Version.Category = "universal" OrElse Version.Category = "client" Then Continue For
            If SelectedNeoForge IsNot Nothing Then Return "与 NeoForge 不兼容"
            If SelectedFabric IsNot Nothing Then Return "与 Fabric 不兼容"
            If SelectedOptiFine IsNot Nothing AndAlso
                McUtils.CompareVersionGE(VanillaName, "1.13") AndAlso McUtils.CompareVersionGE("1.14.3", VanillaName) Then
                Return "与 OptiFine 不兼容"
            End If
            If SelectedOptiFine IsNot Nothing AndAlso Not IsOptiFineSuitForForge(SelectedOptiFine, Version) Then Continue For
            Return Nothing
        Next
        Return "与 OptiFine 不兼容"
    End Function

    Private Sub CardForge_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardForge.PreviewSwap
        If LoadForgeGetError() IsNot Nothing Then e.Handled = True
    End Sub

    Private Sub Forge_Loaded() Handles LoadForge.StateChanged
        Try
            If Not LoadForge.State.IsLoader Then Return
            Dim Loader As LoaderTask(Of String, List(Of DlForgeVersionEntry)) = LoadForge.State
            If VanillaName <> Loader.Input Then Return
            If Loader.State <> LoadState.Finished Then Return

            Dim Versions = Loader.Output.ToList
            If Not Loader.Output.Any() Then Return
            PanForge.Children.Clear()
            Versions = Versions.Where(
            Function(v)
                If v.Category = "universal" OrElse v.Category = "client" Then Return False
                If SelectedOptiFine IsNot Nothing AndAlso Not IsOptiFineSuitForForge(SelectedOptiFine, v) Then Return False
                Return True
            End Function).OrderByDescending(Function(v) v).ToList()
            ForgeDownloadListItemPreload(PanForge, Versions, AddressOf Forge_Selected, False)
            For Each Version In Versions
                PanForge.Children.Add(ForgeDownloadListItem(Version, AddressOf Forge_Selected, False))
            Next
        Catch ex As Exception
            Logger.Error(ex, "可视化 Forge 安装版本列表出错")
        End Try
    End Sub

    Private Sub Forge_Selected(sender As MyListItem, e As EventArgs)
        SelectedForge = sender.Tag
        CardForge.IsSwapped = True
        If SelectedOptiFine IsNot Nothing AndAlso Not IsOptiFineSuitForForge(SelectedOptiFine, SelectedForge) Then SelectedOptiFine = Nothing
        OptiFine_Loaded()
        ReloadSelected()
    End Sub
    Private Sub Forge_Clear(sender As Object, e As EventArgs) Handles BtnForgeClear.Click
        SelectedForge = Nothing
        CardForge.IsSwapped = True
        OptiFine_Loaded()
        ReloadSelected()
    End Sub

#End Region

#Region "NeoForge 列表"

    Public Function LoadNeoForgeGetError() As String
        If SelectedOptiFine IsNot Nothing Then Return "与 OptiFine 不兼容"
        If SelectedForge IsNot Nothing Then Return "与 Forge 不兼容"
        If SelectedFabric IsNot Nothing Then Return "与 Fabric 不兼容"

        If GetLoaderError(LoadNeoForge) IsNot Nothing Then Return GetLoaderError(LoadNeoForge)

        Return If(DlNeoForgeListLoader.Output.Value.Any(Function(v) v.Inherit = VanillaName), Nothing, "无")
    End Function

    Private Sub CardNeoForge_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardNeoForge.PreviewSwap
        If LoadNeoForgeGetError() IsNot Nothing Then e.Handled = True
    End Sub

    Private Sub NeoForge_Loaded() Handles LoadNeoForge.StateChanged
        Try

            If DlNeoForgeListLoader.State <> LoadState.Finished Then Return
            Dim Versions = DlNeoForgeListLoader.Output.Value.Where(Function(v) v.Inherit = VanillaName).ToList
            If Not Versions.Any() Then Return

            PanNeoForge.Children.Clear()
            NeoForgeDownloadListItemPreload(PanNeoForge, Versions, AddressOf NeoForge_Selected, False)
            For Each Version In Versions
                PanNeoForge.Children.Add(NeoForgeDownloadListItem(Version, AddressOf NeoForge_Selected, False))
            Next
        Catch ex As Exception
            Logger.Error(ex, "可视化 NeoForge 安装版本列表出错")
        End Try
    End Sub

    Private Sub NeoForge_Selected(sender As MyListItem, e As EventArgs)
        SelectedNeoForge = sender.Tag
        CardNeoForge.IsSwapped = True
        OptiFine_Loaded()
        ReloadSelected()
    End Sub
    Private Sub NeoForge_Clear(sender As Object, e As EventArgs) Handles BtnNeoForgeClear.Click
        SelectedNeoForge = Nothing
        CardNeoForge.IsSwapped = True
        OptiFine_Loaded()
        ReloadSelected()
    End Sub

#End Region

#Region "Fabric 列表"

    Public Function LoadFabricGetError() As String

        If SelectedOptiFine IsNot Nothing AndAlso McUtils.CompareVersionGE(VanillaName, "1.20.5") Then Return "与 OptiFine 不兼容"

        If GetLoaderError(LoadFabric) IsNot Nothing Then Return GetLoaderError(LoadFabric)

        For Each Version As JObject In DlFabricListLoader.Output.Value("game")
            If Version("version").ToString = VanillaName.Replace("∞", "infinite").Replace("Combat Test 7c", "1.16_combat-3") Then
                If SelectedForge IsNot Nothing Then Return "与 Forge 不兼容"
                If SelectedNeoForge IsNot Nothing Then Return "与 NeoForge 不兼容"
                Return Nothing
            End If
        Next
        Return "无"
    End Function

    Private Sub CardFabric_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardFabric.PreviewSwap
        If LoadFabricGetError() IsNot Nothing Then e.Handled = True
    End Sub

    Private Sub Fabric_Loaded() Handles LoadFabric.StateChanged
        Try
            If DlFabricListLoader.State <> LoadState.Finished Then Return

            Dim Versions As JArray = DlFabricListLoader.Output.Value("loader")
            If Not Versions.Any() Then Return

            PanFabric.Children.Clear()
            CardFabric.SwapControl = PanFabric
            CardFabric.SetContentBuilder(Versions.OfType(Of JObject),
                Function(Version) FabricDownloadListItem(Version, AddressOf Fabric_Selected))
            If Not CardFabric.IsSwapped Then CardFabric.BuildContent()
        Catch ex As Exception
            Logger.Error(ex, "可视化 Fabric 安装版本列表出错")
        End Try
    End Sub

    Public Sub Fabric_Selected(sender As MyListItem, e As EventArgs)
        SelectedFabric = sender.Tag("version").ToString
        FabricApi_Loaded()
        OptiFabric_Loaded()
        CardFabric.IsSwapped = True
        ReloadSelected()
    End Sub
    Private Sub Fabric_Clear(sender As Object, e As EventArgs) Handles BtnFabricClear.Click
        SelectedFabric = Nothing
        SelectedFabricApi = Nothing
        BtnFabricApiClear.Visibility = Visibility.Collapsed
        AutoSelectedFabricApi = False
        SelectedOptiFabric = Nothing
        BtnOptiFabricClear.Visibility = Visibility.Collapsed
        AutoSelectedOptiFabric = False
        CardFabric.IsSwapped = True
        ReloadSelected()
    End Sub

#End Region

#Region "Fabric API 列表"

    Public Shared Function IsFabricApiCompatible(FabricApi As ResourceVersion, MinecraftVersion As String) As Boolean
        Dim FabricApiName = FabricApi.Display
        Try
            If FabricApiName Is Nothing OrElse MinecraftVersion Is Nothing Then Return False
            FabricApiName = FabricApiName.Lower
            Dim TargetName = MinecraftVersion.Replace("∞", "infinite").Replace("Combat Test 7c", "1.16_combat-3").Lower
            If FabricApiName.StartsWithF("[" & TargetName & "]") Then Return True
            If Not FabricApiName.Contains("/") OrElse Not FabricApiName.Contains("]") Then Return False

            For Each Part As String In FabricApiName.BeforeFirst("]").TrimStart("[").Split("/")
                If Part = TargetName Then Return True
            Next

            Dim Lefts = FabricApiName.BeforeFirst("]").RegexSearch("[a-z/]+|[0-9/]+").ToList
            Dim Rights = TargetName.BeforeFirst("]").RegexSearch("[a-z/]+|[0-9/]+").ToList

            Dim i As Integer = 0
            While True

                If Lefts.Count - 1 < i AndAlso Rights.Count - 1 < i Then Return True

                Dim LeftValue As String = If(Lefts.Count - 1 < i, "-1", Lefts(i))
                Dim RightValue As String = If(Rights.Count - 1 < i, "-1", Rights(i))
                If Not LeftValue.Contains("/") Then
                    If LeftValue <> RightValue Then Return False
                Else

                    If Not LeftValue.Contains(RightValue) Then Return False
                End If
                i += 1
            End While
            Return True
        Catch ex As Exception
            Logger.Warn(ex, $"判断 Fabric API 版本适配性出错（{FabricApiName}, {MinecraftVersion}）")
            Return False
        End Try
    End Function

    Public Function LoadFabricApiGetError() As String

        If GetLoaderError(LoadFabricApi) IsNot Nothing Then Return GetLoaderError(LoadFabricApi)
        If DlFabricApiLoader.Output Is Nothing Then Return If(SelectedFabric Is Nothing, "需要安装 Fabric", "获取中……")

        If DlFabricApiLoader.Output.Any(Function(f) IsFabricApiCompatible(f, VanillaName)) Then
            Return If(SelectedFabric Is Nothing, "需要安装 Fabric", Nothing)
        Else
            Return "无"
        End If
    End Function

    Private Sub CardFabricApi_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardFabricApi.PreviewSwap
        If LoadFabricApiGetError() IsNot Nothing Then e.Handled = True
    End Sub

    Public AutoSelectedFabricApi As Boolean = False

    Private Sub FabricApi_Loaded() Handles LoadFabricApi.StateChanged
        Try
            If DlFabricApiLoader.State <> LoadState.Finished Then Return
            If VanillaName Is Nothing OrElse SelectedFabric Is Nothing Then Return

            Dim Versions As New List(Of ResourceVersion)
            For Each Version In DlFabricApiLoader.Output
                If IsFabricApiCompatible(Version, VanillaName) Then
                    If Not Version.Display.StartsWithF("[") Then
                        Logger.Warn($"已特判修改 Fabric API 显示名：{Version.Display}")
                        Version.Display = "[" & VanillaName & "] " & Version.Display
                    End If
                    Versions.Add(Version)
                End If
            Next
            If Not Versions.Any() Then Return
            Versions = Versions.OrderByDescending(Function(v) v.ReleaseDate).ToList

            PanFabricApi.Children.Clear()
            For Each Version In Versions
                PanFabricApi.Children.Add(FabricApiDownloadListItem(Version, AddressOf FabricApi_Selected))
            Next

            If Not AutoSelectedFabricApi Then
                AutoSelectedFabricApi = True
                Dim Item As MyListItem = MyVirtualizingElement.TryInit(PanFabricApi.Children(0))
                Logger.Info($"已自动选择 Fabric API：{Item.Title}")
                FabricApi_Selected(Item, Nothing)
            End If
        Catch ex As Exception
            Logger.Error(ex, "可视化 Fabric API 安装版本列表出错")
        End Try
    End Sub

    Private Sub FabricApi_Selected(sender As MyListItem, e As EventArgs)
        SelectedFabricApi = sender.Tag
        CardFabricApi.IsSwapped = True
        ReloadSelected()
    End Sub
    Private Sub FabricApi_Clear(sender As Object, e As EventArgs) Handles BtnFabricApiClear.Click
        SelectedFabricApi = Nothing
        CardFabricApi.IsSwapped = True
        ReloadSelected()
    End Sub

#End Region

#Region "OptiFabric 列表"

    Public Shared Function IsOptiFabricCompatible(ModFile As ResourceVersion, MinecraftVersion As String) As Boolean
        Try
            If MinecraftVersion Is Nothing Then Return False
            Return ModFile.GameVersions.Contains(MinecraftVersion)
        Catch ex As Exception
            Logger.Warn(ex, $"判断 OptiFabric 版本适配性出错（{MinecraftVersion}）")
            Return False
        End Try
    End Function

    Public AutoSelectedOptiFabric As Boolean = False

    Public Function LoadOptiFabricGetError() As String
        If VanillaDrop >= 140 AndAlso VanillaDrop <= 150 Then Return "不兼容老版本 Fabric，请手动下载 OptiFabric Origins"

        If GetLoaderError(LoadOptiFabric) IsNot Nothing Then Return GetLoaderError(LoadOptiFabric)

        If DlOptiFabricLoader.Output Is Nothing Then
            If SelectedFabric Is Nothing AndAlso SelectedOptiFine Is Nothing Then Return "需要安装 OptiFine 与 Fabric"
            If SelectedFabric Is Nothing Then Return "需要安装 Fabric"
            If SelectedOptiFine Is Nothing Then Return "需要安装 OptiFine"
            Return "获取中……"
        End If
        For Each Version In DlOptiFabricLoader.Output
            If Not IsOptiFabricCompatible(Version, VanillaName) Then Continue For
            If SelectedFabric Is Nothing AndAlso SelectedOptiFine Is Nothing Then Return "需要安装 OptiFine 与 Fabric"
            If SelectedFabric Is Nothing Then Return "需要安装 Fabric"
            If SelectedOptiFine Is Nothing Then Return "需要安装 OptiFine"
            Return Nothing
        Next
        Return "无"
    End Function

    Private Sub CardOptiFabric_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardOptiFabric.PreviewSwap
        If LoadOptiFabricGetError() IsNot Nothing Then e.Handled = True
    End Sub

    Private Sub OptiFabric_Loaded() Handles LoadOptiFabric.StateChanged
        Try
            If DlOptiFabricLoader.State <> LoadState.Finished Then Return
            If VanillaName Is Nothing OrElse SelectedFabric Is Nothing OrElse SelectedOptiFine Is Nothing Then Return

            Dim Versions As New List(Of ResourceVersion)
            For Each Version In DlOptiFabricLoader.Output
                If IsOptiFabricCompatible(Version, VanillaName) Then Versions.Add(Version)
            Next
            If Not Versions.Any() Then Return

            Versions = Versions.OrderByDescending(Function(v) v.ReleaseDate).ToList

            PanOptiFabric.Children.Clear()
            For Each Version In Versions
                PanOptiFabric.Children.Add(OptiFabricDownloadListItem(Version, AddressOf OptiFabric_Selected))
            Next

            If AutoSelectedOptiFabric OrElse VanillaDrop >= 140 AndAlso VanillaDrop <= 150 Then Return
            AutoSelectedOptiFabric = True
            Dim Item As MyListItem = MyVirtualizingElement.TryInit(PanOptiFabric.Children(0))
            Logger.Info($"已自动选择 OptiFabric：{Item.Title}")
            OptiFabric_Selected(Item, Nothing)
        Catch ex As Exception
            Logger.Error(ex, "可视化 OptiFabric 安装版本列表出错")
        End Try
    End Sub

    Private Sub OptiFabric_Selected(sender As MyListItem, e As EventArgs)
        SelectedOptiFabric = sender.Tag
        CardOptiFabric.IsSwapped = True
        ReloadSelected()
    End Sub
    Private Sub OptiFabric_Clear(sender As Object, e As EventArgs) Handles BtnOptiFabricClear.Click
        SelectedOptiFabric = Nothing
        CardOptiFabric.IsSwapped = True
        ReloadSelected()
    End Sub

#End Region

#Region "安装"

    Public Function GetSelectedComponents() As McComponents
        Return New McComponents(
            minecraftVersion:=VanillaName,
            optiFineEntry:=If(SelectedOptiFine Is Nothing, Nothing, JObject.FromObject(SelectedOptiFine)),
            forgeVersion:=SelectedForge?.VersionName,
            neoForgeVersion:=SelectedNeoForge?.VersionName,
            fabricVersion:=SelectedFabric,
            fabricApiVersion:=If(SelectedFabricApi?.Version, If(SelectedFabricApi?.Id, SelectedFabricApi?.Display)),
            optiFabricVersion:=If(SelectedOptiFabric?.Version, If(SelectedOptiFabric?.Id, SelectedOptiFabric?.Display)),
            liteLoaderVersion:=SelectedLiteLoader?.JsonToken?("version").ToString())
    End Function

    Private Sub TextSelectName_KeyDown(sender As Object, e As KeyEventArgs) Handles TextSelectName.KeyDown
        If e.Key = Key.Enter AndAlso IsInSelectPage AndAlso BtnStart.IsEnabled Then BtnStart_Click()
    End Sub
    Private Sub BtnStart_Click() Handles BtnStart.Click

        Dim InstanceName As String = TextSelectName.Text
        Dim Request As New McInstallRequest With {
            .NewInstanceName = InstanceName,
            .InstanceFolder = $"{McFolderSelected}versions\{InstanceName}\",
            .Components = GetSelectedComponents(),
            .MinecraftJson = VanillaData("url").ToString,
            .ForgeEntry = SelectedForge,
            .NeoForgeEntry = SelectedNeoForge,
            .FabricApi = SelectedFabricApi,
            .OptiFabric = SelectedOptiFabric,
            .LiteLoaderEntry = SelectedLiteLoader
        }
        StartInstall(Request)
    End Sub

    Public Overridable Sub StartInstall(Request As McInstallRequest)

        If (SelectedForge IsNot Nothing OrElse SelectedNeoForge IsNot Nothing OrElse SelectedFabric IsNot Nothing) AndAlso
           (Settings.Get(Of Integer)("LaunchArgumentIndieV2") = 0 OrElse Settings.Get(Of Integer)("LaunchArgumentIndieV2") = 2) Then
            If MyMsgBox("你尚未开启版本隔离，多个 MC 版本会共用同一个 Mod 文件夹。" & vbCrLf &
                        "因此，游戏可能会因为读取到与当前版本不符的 Mod 而崩溃。" & vbCrLf &
                        "推荐先在 设置 → 启动选项 → 默认版本隔离 中开启版本隔离！", "版本隔离提示", "取消下载", "继续") = 1 Then
                Return
            End If
        End If
        If Not McInstallStart(Request) Then Return

        ExitSelectPage()
    End Sub

#End Region

    Private Function GetLoaderError(Loader As MyLoading) As String
        If Loader Is Nothing Then Return "获取中……"
        If Not Loader.State.IsLoader Then Return "获取中……"
        Select Case Loader.State.LoadingState
            Case MyLoading.MyLoadingState.Run
                Return "获取中……"
            Case MyLoading.MyLoadingState.Error
                Dim Message As String = CType(Loader.State, LoaderBase).Error.Message
                Return If(Message = "无", "无", "获取失败：" & Message)
            Case MyLoading.MyLoadingState.Unloaded
                Return "未知错误，状态为 Unloaded"
            Case Else
                Return Nothing
        End Select
    End Function

End Class
