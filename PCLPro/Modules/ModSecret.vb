Friend Module ModSecret

    Public Const VersionBranchMain As String = "OpenSource"

    Public Const RegFolder As String = "PCLDebug"

    Public OAuthClientId As String = If(Environment.GetEnvironmentVariable("PCL_MS_CLIENT_ID"), "04b07795-8ddb-461a-bbee-02f9e1bf7b46")

    Public OAuthClientIdMsa As String = If(Environment.GetEnvironmentVariable("PCL_MS_CLIENT_ID_MSA"), "00000000402b5328")

    Public Const OAuthScopeMsa As String = "service::user.auth.xboxlive.com::MBI_SSL"

    Public CurseForgeAPIKey As String = If(Environment.GetEnvironmentVariable("PCL_CURSEFORGE_API_KEY"), "")

    Public Const ClsBaseUrl As String = ""

#Region "网络鉴权"

    Public Const RestrictMcDownload As Boolean = False
    Friend Function SecretCdnSign(UrlWithMark As String) As String
        If Not UrlWithMark.EndsWithF("{CDN}") Then Return UrlWithMark
        Return UrlWithMark.Replace("{CDN}", "").Replace(" ", "%20")
    End Function

    Friend Sub SecretHeadersSign(Url As String, ByRef Req As HttpRequestMessage, Optional SimulateBrowserHeaders As Boolean = False)
        If Not Req.Headers.UserAgent.Any Then
            If Url.Contains("baidupcs.com") OrElse Url.Contains("baidu.com") Then
                Req.Headers.Add("User-Agent", "LogStatistic")
            ElseIf SimulateBrowserHeaders Then
                Req.Headers.Add("User-Agent", $"PCL2/{VersionBaseName}.{CInt(BuildType)} Mozilla/5.0 AppleWebKit/537.36 Chrome/63.0.3239.132 Safari/537.36")
            Else
                Req.Headers.Add("User-Agent", $"PCL2/{VersionBaseName}.{CInt(BuildType)}")
            End If
        End If
        If Not SimulateBrowserHeaders Then Req.Headers.Add("Referer", $"http://{VersionCode}.open.pcl2.server/")
        If Url.Contains("api.curseforge.com") OrElse Url.Contains("forgecdn.net") Then Req.Headers.Add("x-api-key", CurseForgeAPIKey)
    End Sub

#End Region

#Region "主题"

    Public Color1 As New MyColor(52, 61, 74)
    Public Color2 As New MyColor(11, 91, 203)
    Public Color3 As New MyColor(19, 112, 243)
    Public Color4 As New MyColor(72, 144, 245)
    Public Color5 As New MyColor(150, 192, 249)
    Public Color6 As New MyColor(213, 230, 253)
    Public Color7 As New MyColor(222, 236, 253)
    Public Color8 As New MyColor(234, 242, 254)
    Public ColorBg0 As New MyColor(150, 192, 249)
    Public ColorBg1 As New MyColor(190, Color7)
    Public ColorGray1 As New MyColor(64, 64, 64)
    Public ColorGray2 As New MyColor(115, 115, 115)
    Public ColorGray3 As New MyColor(140, 140, 140)
    Public ColorGray4 As New MyColor(166, 166, 166)
    Public ColorGray5 As New MyColor(204, 204, 204)
    Public ColorGray6 As New MyColor(235, 235, 235)
    Public ColorGray7 As New MyColor(240, 240, 240)
    Public ColorGray8 As New MyColor(245, 245, 245)
    Public ColorSemiTransparent As New MyColor(1, Color8)

    Public ThemeNow As Integer = -1
    Public ColorHue As Integer = 210, ColorSat As Integer = 85, ColorLightAdjust As Integer = 0, ColorHueTopbarDelta As OneOf(Of Integer, Integer()) = 0
    Public ThemeDontClick As Integer = 0

    Public Sub ThemeRefresh(Optional NewTheme As Integer = -1)
        Try
            If ThemeNow = NewTheme AndAlso NewTheme >= 0 Then Return
            If NewTheme >= 0 Then ThemeNow = NewTheme

            Dim ApplyTheme As Integer = If(ThemeNow >= 0, ThemeNow, 0)
            If ApplyTheme >= 15 Then ApplyTheme = 0
            Select Case ApplyTheme
                Case 0
                    ColorHue = 210 : ColorSat = 85 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 0
                Case 1
                    ColorHue = 174 : ColorSat = 78 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 14
                Case 2
                    ColorHue = 112 : ColorSat = 58 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 12
                Case 3
                    ColorHue = 45 : ColorSat = 92 : ColorLightAdjust = 2 : ColorHueTopbarDelta = 10
                Case 4
                    ColorHue = 28 : ColorSat = 46 : ColorLightAdjust = -2 : ColorHueTopbarDelta = 8
                Case 5
                    ColorHue = 215 : ColorSat = 6 : ColorLightAdjust = -8 : ColorHueTopbarDelta = 0
                Case 6
                    ColorHue = 336 : ColorSat = 78 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 12
                Case 7
                    ColorHue = 272 : ColorSat = 62 : ColorLightAdjust = -2 : ColorHueTopbarDelta = 16
                Case 8
                    ColorHue = 38 : ColorSat = 76 : ColorLightAdjust = -2 : ColorHueTopbarDelta = 10
                Case 9
                    ColorHue = 22 : ColorSat = 90 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 10
                Case 10
                    ColorHue = 353 : ColorSat = 78 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 12
                Case 11
                    ColorHue = 220 : ColorSat = 92 : ColorLightAdjust = -8 : ColorHueTopbarDelta = 14
                Case 12
                    ColorHue = CInt((Date.Now.Minute * 6 + Date.Now.Hour * 17) Mod 360) : ColorSat = 88 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 46
                Case 13
                    ColorHue = RandomInteger(0, 359) : ColorSat = 88 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 32
                Case 14
                    ColorHue = Settings.Get(Of Integer)("UiLauncherHue")
                    ColorSat = Settings.Get(Of Integer)("UiLauncherSat")
                    ColorHueTopbarDelta = Settings.Get(Of Integer)("UiLauncherDelta")
                    ColorLightAdjust = Settings.Get(Of Integer)("UiLauncherLight") - 20
                Case Else
                    ColorHue = 210 : ColorSat = 85 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 0
            End Select

            Color1 = New MyColor().FromHSL2(ColorHue, ColorSat * 0.2, 25 + ColorLightAdjust * 0.3)
            Color2 = New MyColor().FromHSL2(ColorHue, ColorSat, 45 + ColorLightAdjust)
            Color3 = New MyColor().FromHSL2(ColorHue, ColorSat, 55 + ColorLightAdjust)
            Color4 = New MyColor().FromHSL2(ColorHue, ColorSat, 65 + ColorLightAdjust)
            Color5 = New MyColor().FromHSL2(ColorHue, ColorSat, 80 + ColorLightAdjust * 0.4)
            Color6 = New MyColor().FromHSL2(ColorHue, ColorSat, 91 + ColorLightAdjust * 0.1)
            Color7 = New MyColor().FromHSL2(ColorHue, ColorSat, 95)
            Color8 = New MyColor().FromHSL2(ColorHue, ColorSat, 97)
            ColorBg0 = Color4 * 0.4 + Color5 * 0.4 + ColorGray4 * 0.2
            ColorBg1 = New MyColor(190, Color7)

            ColorSemiTransparent = New MyColor(1, Color8)
            Application.Current.Resources("ColorBrush1") = New SolidColorBrush(Color1)
            Application.Current.Resources("ColorBrush2") = New SolidColorBrush(Color2)
            Application.Current.Resources("ColorBrush3") = New SolidColorBrush(Color3)
            Application.Current.Resources("ColorBrush4") = New SolidColorBrush(Color4)
            Application.Current.Resources("ColorBrush5") = New SolidColorBrush(Color5)
            Application.Current.Resources("ColorBrush6") = New SolidColorBrush(Color6)
            Application.Current.Resources("ColorBrush7") = New SolidColorBrush(Color7)
            Application.Current.Resources("ColorBrush8") = New SolidColorBrush(Color8)
            Application.Current.Resources("ColorBrushBg0") = New SolidColorBrush(ColorBg0)
            Application.Current.Resources("ColorBrushBg1") = New SolidColorBrush(ColorBg1)
            Application.Current.Resources("ColorObject1") = CType(Color1, Color)
            Application.Current.Resources("ColorObject2") = CType(Color2, Color)
            Application.Current.Resources("ColorObject3") = CType(Color3, Color)
            Application.Current.Resources("ColorObject4") = CType(Color4, Color)
            Application.Current.Resources("ColorObject5") = CType(Color5, Color)
            Application.Current.Resources("ColorObject6") = CType(Color6, Color)
            Application.Current.Resources("ColorObject7") = CType(Color7, Color)
            Application.Current.Resources("ColorObject8") = CType(Color8, Color)
            Application.Current.Resources("ColorObjectBg0") = CType(ColorBg0, Color)
            Application.Current.Resources("ColorObjectBg1") = CType(ColorBg1, Color)
            ThemeRefreshMain()
        Catch ex As Exception
            Logger.Error(ex, "刷新主题颜色失败", LogBehavior.Toast)
        End Try
    End Sub

    Public Sub ThemeRefreshMain()
        RunInUi(
        Sub()
            If Not FrmMain.IsLoaded Then Return

            Dim Brush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
            Dim Deltas = ColorHueTopbarDelta.Switch(Function(d) New Integer() {-d, 0, d}, Function(d) d)
            Brush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor().FromHSL2(ColorHue + Deltas(0), ColorSat, 48 + ColorLightAdjust)})
            Brush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor().FromHSL2(ColorHue + Deltas(1), ColorSat, 54 + ColorLightAdjust)})
            Brush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor().FromHSL2(ColorHue + Deltas(2), ColorSat, 48 + ColorLightAdjust)})
            FrmMain.PanTitle.Background = Brush
            FrmMain.PanTitle.Background.Freeze()

            If Settings.Get(Of Boolean)("UiBackgroundColorful") Then
                Brush = New LinearGradientBrush With {.EndPoint = New Point(0.1, 1), .StartPoint = New Point(0.9, 0)}
                Brush.GradientStops.Add(New GradientStop With {.Offset = -0.1, .Color = New MyColor().FromHSL2(ColorHue - 15, ColorSat * 0.8, 91)})
                Brush.GradientStops.Add(New GradientStop With {.Offset = 0.4, .Color = New MyColor().FromHSL2(ColorHue, ColorSat * 0.8, 91)})
                Brush.GradientStops.Add(New GradientStop With {.Offset = 1.1, .Color = New MyColor().FromHSL2(ColorHue + 15, ColorSat * 0.8, 91)})
                FrmMain.PanForm.Background = Brush
            Else
                FrmMain.PanForm.Background = New MyColor(245, 245, 245)
            End If
            FrmMain.PanForm.Background.Freeze()

            FormMain.FontColorRefresh()
        End Sub)
    End Sub
    Friend Sub ThemeCheckAll(EffectSetup As Boolean)
    End Sub
    Friend Function ThemeCheckOne(Id As Integer) As Boolean
        Return True
    End Function
    Friend Function ThemeUnlock(Id As Integer, Optional ShowDoubleHint As Boolean = True, Optional UnlockHint As String = Nothing) As Boolean
        Return False
    End Function

#End Region

#Region "更新"

    Friend Sub UpdateCheckByButton()
        CheckUpdateManually()
    End Sub

    Friend IsUpdateWaitingRestart As Boolean = False
    Public Sub UpdateRestart(TriggerRestartAndByEnd As Boolean)
    End Sub
    Public Sub UpdateReplace(ProcessId As Integer, OldFileName As String, NewFileName As String, TriggerRestart As Boolean)
    End Sub

    Public Sub UpdateReplaceFromEnvironment()
        Dim OldPath As String = Environment.GetEnvironmentVariable("PCL_UPDATE_OLD")
        Dim NewPath As String = Environment.GetEnvironmentVariable("PCL_UPDATE_NEW")
        Dim OldPid As String = Environment.GetEnvironmentVariable("PCL_UPDATE_PID")
        Dim SelfPath As String = Environment.GetEnvironmentVariable("PCL_UPDATE_SELF")
        Dim Restart As Boolean = Environment.GetEnvironmentVariable("PCL_UPDATE_RESTART") = "1"
        If String.IsNullOrEmpty(OldPath) OrElse String.IsNullOrEmpty(NewPath) Then
            Environment.Exit(1)
        End If

        Try
            Dim OldProcess As Diagnostics.Process = Diagnostics.Process.GetProcessById(CInt(OldPid))
            OldProcess.WaitForExit(120 * 1000)
        Catch
        End Try

        Dim Replaced As Boolean = False
        If IO.File.Exists(NewPath) Then
            For i = 1 To 60
                Try
                    IO.File.Copy(NewPath, OldPath, True)
                    Replaced = True
                    Exit For
                Catch ex As Exception
                    Threading.Thread.Sleep(250)
                End Try
            Next
        End If

        If Restart AndAlso IO.File.Exists(OldPath) Then
            Try
                Diagnostics.Process.Start(New Diagnostics.ProcessStartInfo(OldPath) With {
                    .UseShellExecute = True,
                    .WorkingDirectory = IO.Path.GetDirectoryName(OldPath)})
            Catch
            End Try
        End If

        If Replaced Then
            Try
                IO.File.Delete(NewPath)
            Catch
            End Try
        End If
        If Not String.IsNullOrEmpty(SelfPath) AndAlso Not String.Equals(SelfPath, OldPath, StringComparison.OrdinalIgnoreCase) Then
            Try
                IO.File.Delete(SelfPath)
            Catch
            End Try
        End If
        Environment.Exit(0)
    End Sub

    Friend Sub DownloadLatestPCL(Optional LoaderToSyncProgress As LoaderBase = Nothing)

    End Sub

#End Region

#Region "联网配置"

    Public ServerConfig As JObject

    Public ServerLoader As New LoaderTask(Of Integer, Integer)("PCL 配置更新", AddressOf LoadLocalServerConfig, Priority:=ThreadPriority.BelowNormal) With
        {.ReloadTimeout = 1000 * 60 * 60}

    Private Sub LoadLocalServerConfig()
        ServerConfig = JObject.Parse("{
  ""Link"": {
    ""EasyTierVersion"": 264,
    ""Downloads"": [
      ""https://github.com/EasyTier/EasyTier/releases/download/v2.6.4/easytier-windows-{arch}-v2.6.4.zip"",
      ""https://gh-proxy.com/https://github.com/EasyTier/EasyTier/releases/download/v2.6.4/easytier-windows-{arch}-v2.6.4.zip"",
      ""https://ghfast.top/https://github.com/EasyTier/EasyTier/releases/download/v2.6.4/easytier-windows-{arch}-v2.6.4.zip""
    ],
    ""Argument"": """",
    ""DiscoverPeerId"": 0,
    ""DiscoverPeer"": ""tcp://public.easytier.cn:11010"",
    ""PeersBlackList"": [],
    ""RandomPeer"": 3,
    ""Peers"": [""tcp://public.easytier.cn:11010""],
    ""MandatoryPeers"": []
  }
}")
        Logger.Info("已加载内置的本地服务器配置（联机模块：EasyTier v2.6.4）")
    End Sub

#End Region

#Region "赞助等级"

    Public ReadOnly Property CurrentRank As DonationRank
        Get
            Return DonationRank.None
        End Get
    End Property

    Public Sub InputPotatoCode(IsUpdating As Boolean)
    End Sub
    Friend Sub GeneratePotatoCode()
    End Sub

    Friend Function GetIdentify() As String
        Return "0000-0000-0000-0000"
    End Function

#End Region

End Module
