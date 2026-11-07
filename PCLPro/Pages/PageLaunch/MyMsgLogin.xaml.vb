Public Class MyMsgLogin
    Private Data As JObject
    Private UserCode As String
    Private DeviceCode As String
    Private Website As String

#Region "弹窗"

    Private ReadOnly MyConverter As MyMsgBoxConverter
    Private ReadOnly Uuid As Integer = GetUuid()

    Public Sub New(Converter As MyMsgBoxConverter)
        Try
            InitializeComponent()
            Btn1.Name = Btn1.Name & GetUuid()
            Btn2.Name = Btn2.Name & GetUuid()
            Btn3.Name = Btn3.Name & GetUuid()
            Btn4.Name = Btn4.Name & GetUuid()
            Btn5.Name = Btn5.Name & GetUuid()
            MyConverter = Converter
            ShapeLine.StrokeThickness = GetWPFSize(1)
            Data = Converter.Content
            Init()
        Catch ex As Exception
            Logger.Error(ex, "登录弹窗初始化失败", LogBehavior.Toast)
        End Try
    End Sub

    Private Sub Load(sender As Object, e As EventArgs) Handles MyBase.Loaded
        Try

            If Settings.Get(Of Integer)("UiUiStyle") = 1 Then GlassAttachDialog(PanBorder)

            Opacity = 0
            AniStart(AaColor(FrmMain.PanMsg, Grid.BackgroundProperty, If(MyConverter.IsWarn, New MyColor(140, 80, 0, 0), New MyColor(90, 0, 0, 0)) - FrmMain.PanMsg.Background, 280), "PanMsg Background")
            AniStart({
                AaOpacity(Me, 1, 240, 90, New AniEaseOutFluent(AniEasePower.Weak)),
                AaDouble(Sub(i) TransformPos.Y += i, -TransformPos.Y, 500, 90, New AniEaseOutBack(AniEasePower.Weak)),
                AaDouble(Sub(i) TransformRotate.Angle += i, -TransformRotate.Angle, 740, 90, New AniEaseOutBack(AniEasePower.Weak))
            }, "MyMsgBox " & Uuid)

            Logger.Info($"登录弹窗：{LabTitle.Text}{vbCrLf}{LabCaption.Text}")
        Catch ex As Exception
            Logger.Error(ex, "登录弹窗加载失败", LogBehavior.Toast)
        End Try
    End Sub
    Private Sub Close()

        AniStart({
            AaCode(
            Sub()
                If Not WaitingMyMsgBox.Any() Then
                    AniStart(AaColor(FrmMain.PanMsg, Grid.BackgroundProperty, New MyColor(0, 0, 0, 0) - FrmMain.PanMsg.Background, 280, Ease:=New AniEaseOutFluent(AniEasePower.Weak)))
                End If
            End Sub, 30),
            AaOpacity(Me, -Opacity, 110, 25),
            AaDouble(Sub(i) TransformPos.Y += i, 24 - TransformPos.Y, 230, 0, New AniEaseInFluent(AniEasePower.Middle)),
            AaDouble(Sub(i) TransformRotate.Angle += i, 6 - TransformRotate.Angle, 200, 0, New AniEaseInFluent(AniEasePower.Weak)),
            AaCode(Sub() CType(Parent, Grid).Children.Remove(Me), , True)
        }, "MyMsgBox " & Uuid)
    End Sub

    Public Sub Btn1_Click() Handles Btn1.Click
    End Sub
    Public Sub Btn3_Click() Handles Btn3.Click
        Finished(New OperationCanceledException)
    End Sub

    Private Sub Btn4_Click() Handles Btn4.Click
        RunInNewThread(AddressOf OpenWebsiteIncognito, "MyMsgLogin Incognito")
    End Sub
    Private Sub OpenWebsiteIncognito()
        Dim Url As String = Website
        Dim ProgramFilesX86 As String = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        Dim ProgramFiles As String = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        Dim LocalApp As String = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        Dim Candidates As (Exe As String, Args As String)() = {
            (IO.Path.Combine(ProgramFilesX86, "Microsoft\Edge\Application\msedge.exe"), $"--inprivate ""{Url}"""),
            (IO.Path.Combine(ProgramFiles, "Microsoft\Edge\Application\msedge.exe"), $"--inprivate ""{Url}"""),
            (IO.Path.Combine(ProgramFiles, "Google\Chrome\Application\chrome.exe"), $"--incognito ""{Url}"""),
            (IO.Path.Combine(ProgramFilesX86, "Google\Chrome\Application\chrome.exe"), $"--incognito ""{Url}"""),
            (IO.Path.Combine(LocalApp, "Google\Chrome\Application\chrome.exe"), $"--incognito ""{Url}""")
        }
        For Each Item As (Exe As String, Args As String) In Candidates
            If Not File.Exists(Item.Exe) Then Continue For
            Try
                StartProcess(Item.Exe, Item.Args)
                Logger.Info($"已用无痕窗口打开登录网页：{Item.Exe}")
                Hint("已用无痕窗口打开登录网页", HintType.Blue)
                Return
            Catch ex As Exception
                Logger.Warn(ex, $"无痕打开网页失败（{Item.Exe}）")
            End Try
        Next
        Logger.Info("没有找到可用的 Edge 或 Chrome，改用默认浏览器")
        Hint("没有找到 Edge 或 Chrome，已改用默认浏览器打开", HintType.Red)
        OpenWebsite(Url)
    End Sub

    Private Sub Btn5_Click() Handles Btn5.Click
        RunInNewThread(AddressOf ManualLogin, "MyMsgLogin Manual")
    End Sub
    Private Sub ManualLogin()
        Try
            Dim InputToken As String = MyMsgBoxInput(
                "如果网页登录始终失败，可以改用令牌登录：" & vbCrLf &
                $"1. 用手机等其他设备打开 {Website}，输入代码 {UserCode} 完成一次登录；" & vbCrLf &
                "2. 或者直接从其他微软登录启动器（如 HMCL、官方启动器）复制该账号的 refresh_token；" & vbCrLf &
                "3. 把令牌粘贴到下方。PCL 只用它换取登录凭证，不会上传到任何其他地方。",
                "手动登录", "", Nothing, "粘贴 refresh_token（以 M.C 开头）", "登录", "取消")
            If String.IsNullOrWhiteSpace(InputToken) Then Return
            InputToken = InputToken.Trim().Trim(""""c)
            If InputToken.StartsWithF("refresh_token=") Then InputToken = InputToken.Substring("refresh_token=".Length)
            If InputToken.StartsWithF("d=") Then InputToken = InputToken.Substring(2)
            If InputToken.StartsWithF("M.C", True) Then

                Dim Result As String = RefreshTokenByMsa(InputToken)
                Dim ResultJson As JObject = Result.DeserializeJson()
                Finished({ResultJson("access_token").ToString, ResultJson("refresh_token").ToString})
            ElseIf InputToken.Length >= 40 Then

                Hint("已使用 AccessToken 登录，下次启动可能需要重新登录", HintType.Blue)
                Finished({InputToken, ""})
            Else
                Hint("令牌格式不正确，请检查后重试", HintType.Red)
            End If
        Catch ex As Exception
            Logger.Error(ex, "手动令牌登录失败")
            Hint("手动登录失败：" & ex.Message, HintType.Red)
        End Try
    End Sub

    Private Function RefreshTokenByMsa(Token As String) As String
        Try
            Return NetRequestByClient(
                "https://login.live.com/oauth20_token.srf", HttpMethod.Post,
                Content:=$"client_id={OAuthClientIdMsa}&refresh_token={StringUtils.FormUrlEscape(Token)}&grant_type=refresh_token&scope={StringUtils.FormUrlEscape(OAuthScopeMsa)}",
                ContentType:="application/x-www-form-urlencoded", RequireJson:=True)
        Catch ex As HttpRequestCodeException
            Dim OldResponse As String = ex.Response
            If OldResponse.ContainsIgnoreCase("invalid_grant") OrElse OldResponse.ContainsIgnoreCase("invalid_client") OrElse
               OldResponse.ContainsIgnoreCase("invalid_request") OrElse OldResponse.ContainsIgnoreCase("unauthorized_client") Then
                Return NetRequestByClient(
                    "https://login.live.com/oauth20_token.srf", HttpMethod.Post,
                    Content:=$"client_id={OAuthClientId}&refresh_token={StringUtils.FormUrlEscape(Token)}&grant_type=refresh_token&scope=XboxLive.signin%20offline_access",
                    ContentType:="application/x-www-form-urlencoded", RequireJson:=True)
            End If
            Throw
        End Try
    End Function

    Private Sub Drag(sender As Object, e As MouseButtonEventArgs) Handles PanBorder.MouseLeftButtonDown, LabTitle.MouseLeftButtonDown
        On Error Resume Next
        If e.GetPosition(ShapeLine).Y <= 2 Then FrmMain.DragMove()
    End Sub

#End Region

    Private Sub Finished(Result As Object)
        If MyConverter.IsExited Then Return
        MyConverter.IsExited = True
        MyConverter.Result = Result
        RunInUi(AddressOf Close)
        Thread.Sleep(200)
        FrmMain.ShowWindowToTop()
    End Sub

    Private Sub Init()
        UserCode = Data("user_code")
        DeviceCode = Data("device_code")
        Website = Data("verification_uri")

        LabTitle.Text = "登录 Minecraft"
        UpdateUiWithDeviceCode()

        RunInNewThread(AddressOf WorkThread, "MyMsgLogin")
    End Sub

    Private Sub UpdateUiWithDeviceCode()
        LabCaption.Text =
            $"登录网页将自动开启，请在网页中输入 {UserCode}（已自动复制）。" & vbCrLf & vbCrLf &
            $"如果网页一直加载不出来或提示登录失败：" & vbCrLf &
            $"· 先关闭所有 VPN、加速器与系统代理，再重新打开网页；" & vbCrLf &
            $"· 点击下方「无痕打开」，用无痕窗口重新登录（旧 Cookie 或登录态损坏是最常见的原因）；" & vbCrLf &
            $"· 也可以用手机等其他设备打开 {Website} 并输入上述代码。" & vbCrLf &
            $"以上都无效时，可点击「手动登录」，粘贴其他启动器里的 refresh_token 跳过网页登录。"
        CustomEventService.SetEventData(Btn1, Website)
        CustomEventService.SetEventData(Btn2, UserCode)
    End Sub

    Private Function RefreshDeviceCode() As Boolean
        Try
            Dim NewData As JObject = NetRequestByClientRetry(
                "https://login.live.com/oauth20_connect.srf", HttpMethod.Post,
                Content:=$"client_id={OAuthClientIdMsa}&scope={StringUtils.FormUrlEscape(OAuthScopeMsa)}&response_type=device_code",
                ContentType:="application/x-www-form-urlencoded", RequireJson:=True).DeserializeJson()
            Data = NewData
            UserCode = NewData("user_code").ToString
            DeviceCode = NewData("device_code").ToString
            Website = NewData("verification_uri").ToString
            RunInUi(AddressOf UpdateUiWithDeviceCode)
            Thread.Sleep(800)
            OpenWebsite(Website)
            ClipboardSet(UserCode)
            McLaunchLog($"已重新申请设备代码：{UserCode}")
            Return True
        Catch ex As Exception
            Logger.Warn(ex, "重新申请设备代码失败")
            Return False
        End Try
    End Function

    Private Sub WorkThread()
        Thread.Sleep(2000)
        If MyConverter.IsExited Then Return
        OpenWebsite(Website)
        ClipboardSet(UserCode)
        Dim IntervalSec As Integer = 5
        Integer.TryParse(Data("interval")?.ToString, IntervalSec)
        If IntervalSec < 1 OrElse IntervalSec > 60 Then IntervalSec = 5
        Thread.Sleep((IntervalSec - 1) * 1000)

        Dim UnknownFailureCount As Integer = 0
        Dim DeviceCodeRefreshCount As Integer = 0
        Do While Not MyConverter.IsExited
            Try

                Dim Result = NetRequestByClient(
                    "https://login.live.com/oauth20_token.srf", HttpMethod.Post,
                    Content:=
                        "grant_type=urn:ietf:params:oauth:grant-type:device_code" & "&" &
                        "client_id=" & OAuthClientIdMsa & "&" &
                        "device_code=" & StringUtils.FormUrlEscape(DeviceCode) & "&" &
                        "scope=" & StringUtils.FormUrlEscape(OAuthScopeMsa),
                    ContentType:="application/x-www-form-urlencoded",
                    Timeout:=20000 + UnknownFailureCount * 10000,
                    MakeLog:=False, RequireJson:=True)

                Dim ResultJson As JObject = Result.DeserializeJson()
                McLaunchLog($"令牌过期时间：{ResultJson("expires_in")} 秒")
                Hint("网页登录成功！", HintType.Green)
                Finished({ResultJson("access_token").ToString, ResultJson("refresh_token").ToString})
                Return
            Catch ex As Exception

                If TypeOf ex Is HttpRequestCodeException Then
                    Dim Response = CType(ex, HttpRequestCodeException).Response
                    If Response.Contains("authorization_declined") Then
                        Finished(New Exception("$你拒绝了 PCL 申请的权限……"))
                        Return
                    ElseIf Response.Contains("expired_token") Then
                        Finished(New Exception("$登录用时太长啦，重新试试吧！"))
                        Return
                    ElseIf Response.Contains("Account security interrupt") Then
                        Finished(New Exception("$该账号由于安全问题无法登陆，请前往微软账户页获取更多信息。"))
                        Return
                    ElseIf Response.Contains("service abuse") Then
                        Finished(New Exception("$非常抱歉，该账号已被微软封禁，无法登录。"))
                        Return
                    ElseIf Response.Contains("AADSTS70000") Then
                        Finished(New RestartException)
                        Return
                    ElseIf Response.Contains("already been used") OrElse
                           (Response.Contains("invalid_grant") AndAlso Response.Contains("device_code")) Then

                        If DeviceCodeRefreshCount < 1 AndAlso RefreshDeviceCode() Then
                            DeviceCodeRefreshCount += 1
                            UnknownFailureCount = 0
                            Hint("登录会话已刷新，请在新打开的网页中重新输入代码", HintType.Blue)
                            Thread.Sleep(1000)
                            Continue Do
                        End If
                        Finished(New Exception("$登录会话已失效，请重新点击登录。"))
                        Return
                    ElseIf Response.Contains("authorization_pending") Then
                        Thread.Sleep(2000)
                        Continue Do
                    ElseIf Response.Contains("slow_down") Then
                        Thread.Sleep(5000)
                        Continue Do
                    End If
                End If
                If UnknownFailureCount <= 2 Then
                    UnknownFailureCount += 1
                    Logger.Warn(ex, $"登录轮询第 {UnknownFailureCount} 次失败")
                    Thread.Sleep(2000)
                Else
                    Finished(New Exception("登录轮询失败", ex))
                    Return
                End If
            End Try
        Loop
    End Sub

End Class
