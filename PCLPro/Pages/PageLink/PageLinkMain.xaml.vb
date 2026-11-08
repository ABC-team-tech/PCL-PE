Imports System.Collections.ObjectModel
Imports System.Globalization
Imports System.IO
Imports System.Net.Sockets
Imports System.Numerics
Imports System.Reflection
Imports System.Security.Cryptography
Imports PCL

Public Class PageLinkMain

    Private Const TC_CHARS As String = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ"

    Private Const TC_CODE_LEN As Integer = 21

    Private Shared ReadOnly TC_CodeRange As BigInteger = BigInteger.Pow(34, 16)

    Private Shared Function TcRandomRoom() As Tuple(Of String, String, String)
        Dim Bytes(15) As Byte
        Using Rng As RandomNumberGenerator = RandomNumberGenerator.Create()
            Rng.GetBytes(Bytes)
        End Using
        Dim Buffer(16) As Byte
        Array.Copy(Bytes, Buffer, 16)
        Dim Value As New BigInteger(Buffer)
        Value = Value Mod TC_CodeRange
        Value -= Value Mod 7
        Return TcFormat(Value)
    End Function

    Private Shared Function TcFormat(Value As BigInteger) As Tuple(Of String, String, String)
        Return TcFormatCore(Value, False)
    End Function

    Private Shared Function TcFormatLegacy(Value As BigInteger) As Tuple(Of String, String, String)
        Return TcFormatCore(Value, True)
    End Function
    Private Shared Function TcFormatCore(Value As BigInteger, LittleEndian As Boolean) As Tuple(Of String, String, String)
        Dim Chars(15) As Char
        If LittleEndian Then
            For i = 0 To 15
                Chars(i) = TC_CHARS(CInt(Value Mod 34))
                Value = BigInteger.Divide(Value, 34)
            Next
        Else
            For i = 15 To 0 Step -1
                Chars(i) = TC_CHARS(CInt(Value Mod 34))
                Value = BigInteger.Divide(Value, 34)
            Next
        End If
        Dim Code As String = "U/" & New String(Chars, 0, 4) & "-" & New String(Chars, 4, 4) & "-" &
                             New String(Chars, 8, 4) & "-" & New String(Chars, 12, 4)
        Dim Name As String = "scaffolding-mc-" & New String(Chars, 0, 4) & "-" & New String(Chars, 4, 4)
        Dim Secret As String = New String(Chars, 8, 4) & "-" & New String(Chars, 12, 4)
        Return New Tuple(Of String, String, String)(Code, Name, Secret)
    End Function

    Private Shared Function TcParse(Code As String) As Tuple(Of String, String, String)
        If String.IsNullOrEmpty(Code) Then Return Nothing
        Dim Index As Integer = Code.IndexOf("U/", StringComparison.Ordinal)
        If Index < 0 OrElse Code.Length < Index + TC_CODE_LEN Then Return Nothing
        Dim Body As String = Code.Substring(Index + 2, TC_CODE_LEN - 2)
        If Body.Length <> 19 Then Return Nothing
        If Body(4) <> "-"c OrElse Body(9) <> "-"c OrElse Body(14) <> "-"c Then Return Nothing
        Body = Body.Replace("-", "")

        Dim Digits(15) As Integer
        For i = 0 To 15
            Dim Ch As Char = Char.ToUpperInvariant(Body(i))
            If Ch = "I"c Then Ch = "1"c
            If Ch = "O"c Then Ch = "0"c
            Dim Pos As Integer = TC_CHARS.IndexOf(Ch)
            If Pos < 0 Then Return Nothing
            Digits(i) = Pos
        Next

        Dim Value As BigInteger = BigInteger.Zero
        For i = 0 To 15
            Value = Value * 34 + Digits(i)
        Next
        If Value Mod 7 = 0 Then Return TcFormat(Value)

        Value = BigInteger.Zero
        Dim Power As BigInteger = BigInteger.One
        For i = 0 To 15
            Value += Digits(i) * Power
            Power *= 34
        Next
        If Value Mod 7 = 0 Then
            Logger.Info("检测到旧版 PCL PE 生成的房间码（低位在前），已按旧格式解析")
            Return TcFormatLegacy(Value)
        End If
        Return Nothing
    End Function

#Region "状态管理"

    Public Enum LinkStates
        Waiting
        Loading
        Failed
        Finished
    End Enum
    Public Shared LinkState As LinkStates = LinkStates.Waiting

    Private Sub ChangeState(NewState As LinkStates)
        If LinkState = NewState Then Return
        Dim OldState = LinkState
        LinkState = NewState
        Logger.Info($"主状态由 {OldState} 变更为 {NewState}")

        RunInUi(
        Sub()
            Select Case NewState
                Case LinkStates.Waiting
                    SwitchToWaiting(OldState)
                Case LinkStates.Loading
                    SwitchToLoading(OldState)
                Case LinkStates.Failed
                    SwitchToFailed(OldState)
                Case LinkStates.Finished
                    SwitchToFinished(OldState)
            End Select
        End Sub)
    End Sub

    Private Sub LoaderInit() Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanContent, PanAlways, LinkLoader, AutoRun:=False)
        AddHandler LinkLoader.OnStateChangedUi,
        Sub(Loader As LoaderBase, NewState As LoadState, OldState As LoadState)
            Select Case NewState
                Case LoadState.Finished
                    ChangeState(LinkStates.Finished)
                Case LoadState.Failed
                    Telemetry("联机失败", "Exception", FilterUserName(Loader.Error.GetDisplay(True), "*"))
                    ChangeState(LinkStates.Failed)
            End Select
        End Sub
    End Sub

    Private Sub UpdatePanelVisibility() Handles Me.PageEnter
        ' 尚未选择过联机方式时，优先展示选择界面
        Dim ShowChoose As Boolean = LinkState = LinkStates.Waiting AndAlso String.IsNullOrEmpty(LinkMethod)
        Dim ShowMctier As Boolean = LinkState = LinkStates.Waiting AndAlso Not ShowChoose AndAlso LinkMethod = LinkMethodMctier
        Dim ShowSelect As Boolean = LinkState = LinkStates.Waiting AndAlso Not ShowChoose AndAlso Not ShowMctier
        FrmLinkMain.PanChoose.Visibility = If(ShowChoose, Visibility.Visible, Visibility.Collapsed)
        FrmLinkMain.PanMctier.Visibility = If(ShowMctier, Visibility.Visible, Visibility.Collapsed)
        FrmLinkMain.PanSelect.Visibility = If(ShowSelect, Visibility.Visible, Visibility.Collapsed)
        FrmLinkMain.PanFinish.Visibility = If(LinkState = LinkStates.Finished, Visibility.Visible, Visibility.Collapsed)
        FrmLinkMain.LabAlwaysSource.Text = If(LinkMethod = LinkMethodMctier,
            "联机由 MCTier 提供，启动器仅负责拉起与引导",
            "联机功能由陶瓦联机 Terracotta 提供（AGPL-3.0）")
        If ShowMctier Then
            RefreshMcTierPanel(McTierControlAlive())
        End If
    End Sub

#End Region

#Region "联机方式选择"

    Private Const LinkMethodTerracotta As String = "Terracotta"
    Private Const LinkMethodMctier As String = "MCTier"

    Private Shared ReadOnly Property LinkMethod As String
        Get
            Dim Value As String = Settings.Get(Of String)("LinkMethodChosen")
            Return If(Value, "")
        End Get
    End Property

    Private Sub BtnChooseTerracotta_Click() Handles BtnChooseTerracotta.Click
        ApplyLinkMethod(LinkMethodTerracotta)
    End Sub

    Private Sub BtnChooseMctier_Click() Handles BtnChooseMctier.Click
        ApplyLinkMethod(LinkMethodMctier)
    End Sub

    Private Sub BtnSwitchMethod_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSwitchMethod.MouseLeftButtonUp
        If LinkState <> LinkStates.Waiting Then
            Hint("请先退出当前联机房间，再切换联机方式。", HintType.Red)
            Return
        End If
        If MyMsgBox($"当前使用的是 {LinkMethodDisplay(LinkMethod)}。" & vbCrLf & vbCrLf &
                    "切换后不影响已保存的房间码，但需要重新创建或加入房间。" & vbCrLf &
                    "是否切换联机方式？", "切换联机方式", "切换", "取消") = 1 Then
            Settings.Set("LinkMethodChosen", "")
            UpdatePanelVisibility()
        End If
    End Sub

    Private Shared Function LinkMethodDisplay(Method As String) As String
        Return If(Method = LinkMethodMctier, "MCTier", "陶瓦联机")
    End Function

    ''' <summary>统一的方式切换入口：写入设置、刷新界面、按方式给出后续指引。</summary>
    Private Sub ApplyLinkMethod(Method As String)
        Settings.Set("LinkMethodChosen", Method)
        UpdatePanelVisibility()
        If Method = LinkMethodMctier Then
            EnterMcTierMethod()
        End If
    End Sub

    Private Sub EnterMcTierMethod()
        Dim LocalExe As String = McTierLocalPath()
        If LocalExe Is Nothing Then
            If MyMsgBox("MCTier 是独立的联机工具，需要单独下载（约 141 MB）。" & vbCrLf & vbCrLf &
                        "· 启动器会下载并解压到本地，之后可离线使用" & vbCrLf &
                        "· 下载源为本项目的 GitHub Release（已按上游要求重建 EasyTier，不含 Npcap）" & vbCrLf & vbCrLf &
                        "是否现在下载？",
                        "需要下载 MCTier", "下载并安装", "稍后") = 1 Then
                DownloadMcTier()
            End If
            Return
        End If

        Dim Result As Integer = MyMsgBox(
            "已切换到 MCTier 联机方式。" & vbCrLf & vbCrLf &
            $"检测到本机已安装 MCTier：{LocalExe}" & vbCrLf & vbCrLf &
            McTierStepsText(),
            "MCTier 联机", "启动 MCTier", "复制联机步骤", "稍后")
        Select Case Result
            Case 1
                ' 复用按钮流程：以 --hmcl2 方式拉起并等待控制接口
                BtnLaunchMctier_Click()
            Case 2
                ClipboardSet(McTierStepsText(), False)
                Hint("联机步骤已复制到剪贴板", HintType.Green)
        End Select
    End Sub

    ''' <summary>MCTier 无命令行接口，只能由用户在它自己的界面内创建/加入大厅，这里给出可照做的步骤。</summary>
    Private Shared Function McTierStepsText() As String
        Return "联机步骤：" & vbCrLf &
               "1. 在 MCTier 中创建或加入大厅，把大厅邀请发给朋友" & vbCrLf &
               "2. 进入游戏存档，在暂停菜单选择「对局域网开放」，记下聊天栏显示的端口" & vbCrLf &
               "3. 房主在 MCTier 的「游戏快连」里找到自己的世界，朋友复制该地址后直接连接" & vbCrLf &
               vbCrLf &
               "说明：MCTier 没有提供命令行接口，启动器只能拉起它，无法代替它建立组网。"
    End Function

    ''' <summary>
    ''' 以 --hmcl2 方式启动 MCTier，并等待其写出控制端口。
    ''' 返回 True 表示控制接口可用（可驱动组网）；False 表示无法建立控制通道。
    ''' </summary>
    Private Shared Function StartMcTierWithControl(LocalExe As String) As Boolean
        Try
            ' 若已有可用的控制端口，直接复用
            If McTierControlAlive() Then Return True

            ' MCTier 是单实例应用：已存在实例时，新的 --hmcl2 进程会直接退出、不写端口文件。
            ' 因此必须先结束已有实例，否则必然拿不到控制端口。
            If Not McTierEnsureNoRunningInstance() Then Return False

            Dim PortDir As String = IO.Path.Combine(IO.Path.GetTempPath(), "PCLPro-MCTier-" & Guid.NewGuid().ToString("N"))
            DirectoryUtils.Create(PortDir)
            Dim PortFile As String = IO.Path.Combine(PortDir, "http")

            Logger.Info($"正在以控制接口方式启动 MCTier：{LocalExe} --hmcl2 {PortFile}")
            Dim Info As New ProcessStartInfo(LocalExe, $"--hmcl2 ""{PortFile}""") With {
                .WorkingDirectory = IO.Path.GetDirectoryName(LocalExe),
                .UseShellExecute = False
            }
            Process.Start(Info)

            ' 轮询端口文件，最多 30 秒（Tauri 冷启动含 WebView2 初始化，通常 2 秒内就绪）
            For i = 1 To 300
                If IO.File.Exists(PortFile) Then
                    Dim Text As String = IO.File.ReadAllText(PortFile).Trim()
                    Dim Port As Integer
                    If Integer.TryParse(Text, Port) AndAlso Port > 0 Then
                        McTierPort = Port
                        Logger.Info($"MCTier 控制接口端口：{Port}")
                        Return True
                    End If
                End If
                Thread.Sleep(100)
            Next
            Logger.Warn("等待 MCTier 控制端口超时，可能当前 MCTier 版本不支持 --hmcl2")
            McTierPort = 0
            Return False
        Catch ex As Exception
            Logger.Warn(ex, "以控制接口方式启动 MCTier 失败")
            McTierPort = 0
            Return False
        End Try
    End Function

    ''' <summary>
    ''' 确保没有 MCTier 实例在运行。返回 False 表示无法结束已有实例，调用方应中止启动。
    ''' </summary>
    Private Shared Function McTierEnsureNoRunningInstance() As Boolean
        Dim Running = Diagnostics.Process.GetProcessesByName("MCTier")
        If Running.Length = 0 Then Return True

        Logger.Info($"检测到 {Running.Length} 个 MCTier 进程，正在结束以便建立控制接口")
        For Each Proc As Diagnostics.Process In Running
            Try
                Proc.Kill()
                Proc.WaitForExit(5000)
            Catch ex As Exception
                Logger.Warn(ex, $"结束 MCTier 进程失败（PID {Proc.Id}）")
            End Try
        Next
        ' 等待进程真正退出，避免单实例锁未释放
        For i = 1 To 30
            If Diagnostics.Process.GetProcessesByName("MCTier").Length = 0 Then Return True
            Thread.Sleep(100)
        Next
        Return Diagnostics.Process.GetProcessesByName("MCTier").Length = 0
    End Function

    Private Shared McTierPort As Integer = 0

    Private Shared Function McTierControlAlive() As Boolean
        If McTierPort <= 0 Then Return False
        Try
            Dim Body As String = Nothing
            Return McTierApiStatus("/state", Body, 1200) = 200
        Catch
            Return False
        End Try
    End Function

    Private Shared Function McTierApi(Path As String, Optional TimeoutMs As Integer = 4000) As String
        Dim Body As String = Nothing
        McTierApiStatus(Path, Body, TimeoutMs)
        Return Body
    End Function

    Private Shared Function McTierApiStatus(Path As String, ByRef Body As String, Optional TimeoutMs As Integer = 4000,
                                            Optional Method As String = "GET", Optional Json As String = Nothing) As Integer
        Body = Nothing
        If McTierPort <= 0 Then Return -1
        Try
            Dim Request As HttpWebRequest = CType(WebRequest.Create($"http://127.0.0.1:{McTierPort}{Path}"), HttpWebRequest)
            Request.Method = Method
            Request.Timeout = TimeoutMs
            Request.ReadWriteTimeout = TimeoutMs
            Request.Proxy = Nothing
            Request.KeepAlive = False
            Request.ContentType = "application/json; charset=utf-8"
            If Json IsNot Nothing Then
                Dim Bytes As Byte() = Encoding.UTF8.GetBytes(Json)
                Request.ContentLength = Bytes.Length
                Using Stream As IO.Stream = Request.GetRequestStream()
                    Stream.Write(Bytes, 0, Bytes.Length)
                End Using
            End If
            Using Response As HttpWebResponse = CType(Request.GetResponse(), HttpWebResponse)
                Using Reader As New IO.StreamReader(Response.GetResponseStream(), Encoding.UTF8)
                    Body = Reader.ReadToEnd()
                End Using
                Return CInt(Response.StatusCode)
            End Using
        Catch ex As WebException
            If ex.Response IsNot Nothing Then
                Try
                    Using Reader As New IO.StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8)
                        Body = Reader.ReadToEnd()
                    End Using
                    Return CInt(CType(ex.Response, HttpWebResponse).StatusCode)
                Catch
                End Try
            End If
            Logger.Warn(ex, $"调用 MCTier 控制接口失败（{Path}）")
            Return -1
        Catch ex As Exception
            Logger.Warn(ex, $"调用 MCTier 控制接口失败（{Path}）")
            Return -1
        End Try
    End Function

    ''' <summary>本机已「对局域网开放」的游戏端口，找不到返回 0。</summary>
    Private Shared Function TcDetectLanPort() As Integer
        For Each Candidate As Integer In {25565, 25566, 25567}
            If TcGamePortAlive(Candidate) Then Return Candidate
        Next
        For Port As Integer = 1024 To 25565
            If TcGamePortAlive(Port) Then Return Port
        Next
        Return 0
    End Function

    Private Sub BtnLaunchMctier_Click() Handles BtnLaunchMctier.Click
        Dim LocalExe As String = McTierLocalPath()
        If LocalExe Is Nothing Then
            If MyMsgBox("没有找到已安装的 MCTier。" & vbCrLf & "是否打开下载页面？", "MCTier 未安装", "打开下载页", "取消") = 1 Then
                OpenWebsite("https://github.com/pmh1314520/MCTier/releases/latest")
            End If
            Return
        End If

        RunInNewThread(
        Sub()
            Dim HasControl As Boolean = StartMcTierWithControl(LocalExe)
            Dim Port As Integer = TcDetectLanPort()
            RunInUi(
            Sub()
                If HasControl Then
                    Hint("MCTier 已启动并接入控制接口，可直接在下方创建或加入房间。", HintType.Green)
                    RefreshMcTierPanel(True)
                Else
                    RefreshMcTierPanel(False)
                    Dim Result As Integer = MyMsgBox(
                        "MCTier 已启动，但启动器未能接管它（控制接口没有响应）。" & vbCrLf & vbCrLf &
                        "常见原因：" & vbCrLf &
                        "· 本机 MCTier 不是集成版，没有 --hmcl2 控制接口" & vbCrLf &
                        "· 安全软件拦截了本地端口监听" & vbCrLf & vbCrLf &
                        "可以点「重新下载」获取集成版；也可以直接在 MCTier 界面里手动创建或加入大厅。",
                        "MCTier 未接管", "重新下载", "知道了")
                    If Result = 1 Then DownloadMcTier()
                    If Port > 0 Then ClipboardSet(Port.ToString(), False)
                End If
            End Sub)
        End Sub, "MCTier Launcher")
    End Sub

    ''' <summary>根据控制接口可用性刷新 MCTier 面板。控制接口可用时开放建/进房按钮。</summary>
    Private Sub RefreshMcTierPanel(HasControl As Boolean)
        FrmLinkMain.BtnMctierCreate.IsEnabled = HasControl
        FrmLinkMain.BtnMctierJoin.IsEnabled = HasControl
        FrmLinkMain.TextMctierCode.IsEnabled = HasControl
        FrmLinkMain.LabMctierStatus.Text = If(HasControl,
            "控制接口已连接：可直接建房或输入房主的大厅名加入。",
            "控制接口未连接：需要装有支持 --hmcl2 的 MCTier 版本，或在其界面内手动操作。")
    End Sub

    Private Sub BtnMctierSteps_Click() Handles BtnMctierSteps.Click
        ClipboardSet(McTierStepsText(), False)
        Hint("联机步骤已复制到剪贴板", HintType.Green)
    End Sub

    Private Sub BtnMctierDownload_Click() Handles BtnMctierDownload.Click
        Dim LocalExe As String = McTierLocalPath()
        If LocalExe Is Nothing Then
            If MyMsgBox("MCTier 尚未安装，是否现在下载（约 141 MB）？", "下载 MCTier", "下载并安装", "稍后") = 1 Then
                DownloadMcTier()
            End If
            Return
        End If
        If MyMsgBox($"MCTier 已安装在：{vbCrLf}{LocalExe}{vbCrLf}{vbCrLf}是否重新下载并覆盖？", "重新下载 MCTier", "重新下载", "取消") = 1 Then
            DownloadMcTier()
        End If
    End Sub

    ''' <summary>MCTier 内置的默认 EasyTier 节点与信令服务器，与它前端 LobbyForm 的默认值保持一致。</summary>
    Private Const McTierDefaultNode As String = "tcp://easytier.weiai.org.cn:11010"
    Private Const McTierDefaultSignaling As String = "wss://mctier.pmhs.top/signaling"

    ''' <summary>
    ''' 取得本机的信令身份指纹，用作 create/join 的 player_id。
    ''' MCTier 要求 player_id 是它自己签发的 64 位十六进制身份指纹（用于派生 hosts 域名），
    ''' 不能由启动器自造 UUID，否则会报「信令身份 ID 无效」。
    ''' </summary>
    Private Shared Function McTierPlayerId() As String
        Dim Body As String = McTierApi("/signaling/identity", 15000)
        If String.IsNullOrWhiteSpace(Body) Then Return Nothing
        Try
            Dim Json As JObject = Body.DeserializeJson()
            Dim Id As String = If(Json("clientId")?.ToString, "")
            If Id.RegexCheck("^[0-9a-f]{64}$") Then Return Id
            Logger.Warn($"MCTier 返回的信令身份格式异常：{Id}")
        Catch ex As Exception
            Logger.Warn(ex, "解析 MCTier 信令身份失败")
        End Try
        Return Nothing
    End Function

    Private Sub BtnMctierCreate_Click() Handles BtnMctierCreate.Click
        Dim Room As String = InputBoxOrCancel("输入大厅名称", "创建 MCTier 大厅", $"PCL-{RandomInteger(1000, 9999)}")
        If Room Is Nothing Then Return
        Dim PlayerId As String = McTierPlayerId()
        If PlayerId Is Nothing Then
            MyMsgBox("无法获取 MCTier 的信令身份，请确认 MCTier 已启动且控制接口已连接。", "创建失败", IsWarn:=True)
            Return
        End If
        Dim Port As Integer = TcDetectLanPort()
        Dim Player As String = TcPlayerName()
        Dim Request As JObject = New JObject From {
            {"name", Room}, {"password", ""}, {"player_name", Player}, {"player_id", PlayerId},
            {"server_node", McTierDefaultNode}, {"signaling_server", McTierDefaultSignaling}, {"address_attempt", 0}
        }
        If Port > 0 Then Request("mc_port") = Port

        Dim Body As String = Nothing
        Dim Code As Integer = McTierApiStatus("/lobby/create", Body, 60000, "POST", Request.ToString(Newtonsoft.Json.Formatting.None))
        If Code = 200 Then
            Hint($"已创建大厅「{Room}」，游戏端口 {(If(Port > 0, Port.ToString(), "未检测到"))} 已开始广播。", HintType.Green)
        Else
            MyMsgBox("创建 MCTier 大厅失败：" & vbCrLf & If(Body, "（无响应）"), "创建失败", IsWarn:=True)
        End If
    End Sub

    Private Sub BtnMctierJoin_Click() Handles BtnMctierJoin.Click
        Dim Room As String = FrmLinkMain.TextMctierCode.Text
        If String.IsNullOrWhiteSpace(Room) Then
            Hint("请输入房主的大厅名称", HintType.Red)
            Return
        End If
        Dim PlayerId As String = McTierPlayerId()
        If PlayerId Is Nothing Then
            MyMsgBox("无法获取 MCTier 的信令身份，请确认 MCTier 已启动且控制接口已连接。", "加入失败", IsWarn:=True)
            Return
        End If
        Dim Port As Integer = TcDetectLanPort()
        Dim Player As String = TcPlayerName()
        Dim Request As JObject = New JObject From {
            {"name", Room.Trim()}, {"password", ""}, {"player_name", Player}, {"player_id", PlayerId},
            {"server_node", McTierDefaultNode}, {"signaling_server", McTierDefaultSignaling}, {"address_attempt", 0}
        }
        If Port > 0 Then Request("mc_port") = Port

        Dim Body As String = Nothing
        Dim Code As Integer = McTierApiStatus("/lobby/join", Body, 60000, "POST", Request.ToString(Newtonsoft.Json.Formatting.None))
        If Code = 200 Then
            Hint($"已加入大厅「{Room}」，房主的世界应会出现在多人游戏列表中。", HintType.Green)
        Else
            MyMsgBox("加入 MCTier 大厅失败：" & vbCrLf & If(Body, "（无响应）"), "加入失败", IsWarn:=True)
        End If
    End Sub

    Private Shared Function InputBoxOrCancel(Title As String, Caption As String, DefaultValue As String) As String
        Return MyMsgBoxInput(Caption, "", DefaultInput:=DefaultValue)
    End Function

    ''' <summary>MCTier 发布源（本项目 Release，内置去 Npcap 的 EasyTier）。</summary>
    Private Const McTierReleaseRepo As String = "ABC-team-tech/mctier-pcl-pe"
    Private Const McTierAssetName As String = "MCTier-PCLPE-v3.10.0.zip"
    Private Const McTierAssetSha256 As String = "F2C7111E392259909B4D1513D472CDBA313F45FBEBBFB6F867D4E7131F40E315"

    ''' <summary>下载并安装 MCTier 到启动器目录下的 MCTier\ 子目录。</summary>
    Private Sub DownloadMcTier()
        Dim TargetDir As String = Paths.Base & "MCTier\"
        Dim ZipPath As String = PathTmpMcTier & McTierAssetName
        Dim Urls As New List(Of String) From {
            $"https://github.com/{McTierReleaseRepo}/releases/download/v3.10.0-pclpe/{McTierAssetName}",
            $"https://ghfast.top/https://github.com/{McTierReleaseRepo}/releases/download/v3.10.0-pclpe/{McTierAssetName}",
            $"https://gh-proxy.com/https://github.com/{McTierReleaseRepo}/releases/download/v3.10.0-pclpe/{McTierAssetName}"
        }

        UpdateProgressShow("下载 MCTier")
        RunInNewThread(
        Sub()
            Try
                DirectoryUtils.Create(PathTmpMcTier)
                UpdateProgressSet(2, "正在连接下载源……")
                Logger.Info($"开始下载 MCTier：{Urls(0)}")

                ' 带 SHA256 校验下载，任一镜像可用即可
                NetDownloadByLoader(Urls, ZipPath, Check:=New FileChecker With {.Hash = McTierAssetSha256, .MinSize = 100 * 1024 * 1024})
                UpdateProgressSet(65, "下载完成，正在校验……")

                UpdateProgressSet(70, "正在解压……")
                Dim ExtractDir As String = PathTmpMcTier & "extract\"
                DirectoryUtils.Delete(ExtractDir)
                DirectoryUtils.Create(ExtractDir)
                FileUtils.ExtractToDirectory(ZipPath, ExtractDir)

                UpdateProgressSet(90, "正在安装……")
                ' 包内结构为 MCTier\MCTier.exe；兼容直接放在根的情况
                Dim SourceExe As String = IO.Path.Combine(ExtractDir, "MCTier", "MCTier.exe")
                If Not IO.File.Exists(SourceExe) Then SourceExe = IO.Path.Combine(ExtractDir, "MCTier.exe")
                If Not IO.File.Exists(SourceExe) Then Throw New Exception("压缩包内未找到 MCTier.exe")

                DirectoryUtils.Create(TargetDir)
                Dim TargetExe As String = IO.Path.Combine(TargetDir, "MCTier.exe")
                FileUtils.Copy(SourceExe, TargetExe)
                FileUtils.Delete(ZipPath)
                DirectoryUtils.Delete(ExtractDir)
                UpdateProgressSet(100, "安装完成")

                RunInUi(
                Sub()
                    UpdateProgressClose()
                    Hint("MCTier 已下载并安装完成", HintType.Green)
                    RefreshMcTierPanel(False)
                End Sub)
            Catch ex As Exception
                Logger.Warn(ex, "下载 MCTier 失败")
                RunInUi(
                Sub()
                    UpdateProgressClose()
                    MyMsgBox("下载 MCTier 失败：" & vbCrLf & ex.Message & vbCrLf & vbCrLf &
                             $"可手动访问 https://github.com/{McTierReleaseRepo}/releases 下载，" & vbCrLf &
                             "解压后把 MCTier.exe 放到：" & vbCrLf & TargetDir,
                             "下载失败", IsWarn:=True)
                End Sub)
            End Try
        End Sub, "MCTier Downloader")
    End Sub

    Private ReadOnly Property PathTmpMcTier As String
        Get
            Return PathTemp & "McTierDownload\"
        End Get
    End Property

    ''' <summary>在常见位置查找已安装的 MCTier，找不到返回 Nothing。</summary>
    Private Shared Function McTierLocalPath() As String
        Dim Candidates As New List(Of String)
        Try
            Candidates.Add(Paths.Base & "MCTier\MCTier.exe")
            Candidates.Add(Paths.Base & "MCTier.exe")
            For Each Folder As String In {"ProgramFiles", "ProgramFiles(x86)", "LocalAppData"}
                Dim Root As String = Environment.GetEnvironmentVariable(Folder)
                If Not String.IsNullOrEmpty(Root) Then Candidates.Add(IO.Path.Combine(Root, "MCTier", "MCTier.exe"))
            Next
            Candidates.Add(IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "MCTier", "MCTier.exe"))
        Catch ex As Exception
            Logger.Warn(ex, "枚举 MCTier 安装位置失败")
        End Try
        For Each Candidate In Candidates
            Try
                If IO.File.Exists(Candidate) Then Return Candidate
            Catch
            End Try
        Next
        Return Nothing
    End Function

#End Region

#Region "初始页面 (Waiting)"

    Private Sub SwitchToWaiting(OldState As LinkStates)

        HostGuideToken += 1
        LinkLoader.Cancel()
        SyncLock LinkLoader.LockState
            LinkLoader.State = LoadState.Waiting
        End SyncLock
        ProcessStop()

        If OldState <> LinkStates.Finished Then

            UpdatePanelVisibility()
        End If
        PageOnContentExit()
    End Sub

    Private LastCreateTrigger As Long = 0
    Private Sub BtnCreate_Click() Handles BtnCreate.Click

        If Math.Abs(GetTimeMs() - LastCreateTrigger) < 500 Then Return
        LastCreateTrigger = GetTimeMs()

        If Not TcGameRunning() Then
            If MyMsgBox("还没有检测到正在运行的游戏。" & vbCrLf &
                        "请先进入存档并在游戏里选择「对局域网开放」，聊天栏会显示本地游戏端口。" & vbCrLf &
                        "记下端口后再创建房间，朋友才能连进来。" & vbCrLf & vbCrLf &
                        "是否仍然创建房间？", "创建联机房间", "仍然创建", "取消") = 2 Then Return
        End If

        Dim PortInput As String = MyMsgBoxInput("输入游戏端口",
            "请填写游戏聊天栏「本地游戏已在端口 XXXXX 上开放」里的端口号。",
            DefaultInput:=If(HostPort > 0, HostPort.ToString(), "25565"),
            ValidateRules:=New Collection(Of Validate) From {New ValidateInteger(1, 65535)})
        If PortInput Is Nothing Then Return
        Dim NewPort As Integer
        If Not Integer.TryParse(PortInput.Trim(), NewPort) OrElse NewPort <= 0 OrElse NewPort > 65535 Then Return
        Dim PortAlive As Boolean = TcGamePortAlive(NewPort)
        If Not PortAlive Then
            If MyMsgBox($"端口 {NewPort} 现在没有在监听。" & vbCrLf &
                        "请确认游戏里已经选择了「对局域网开放」，并且端口没有填错。" & vbCrLf & vbCrLf &
                        "是否仍然用这个端口创建房间？", "端口未在监听", "仍然创建", "取消", IsWarn:=True) = 2 Then Return
        End If
        ManualPort = NewPort
        Logger.Info($"手动指定游戏端口 {NewPort}（当前监听：{PortAlive}）")

        IsServerSide = True

        Dim NodeItem = TryCast(ComboCreateNode.SelectedItem, MyComboBoxItem)
        If NodeItem IsNot Nothing Then TcSelectedNode = TryCast(NodeItem.Tag, String)
        Dim Room = TcRandomRoom()
        RoomCode = Room.Item1
        NetworkName = Room.Item2
        NetworkSecret = Room.Item3
        Logger.Info($"尝试创建陶瓦房间，房间码 {RoomCode}，网络名 {NetworkName}")

        ModFirewall.CheckTerracottaFirewall()

        ChangeState(LinkStates.Loading)
    End Sub

    Private Shared LastCode As String = Nothing

    Public Shared Sub Join()

        Try
            If FrmLinkMain IsNot Nothing AndAlso FrmLinkMain.TextJoinCode IsNot Nothing AndAlso FrmLinkMain.PanSelectJoin.IsVisible Then
                FrmLinkMain.TextJoinCode.Focus()
                Return
            End If
        Catch
        End Try

        Dim Code As String = MyMsgBoxInput("输入房间码", "输入房主发给你的房间码。",
            HintText:=If(String.IsNullOrEmpty(LastCode), "", "使用上一次的房间码"))
        If Not String.IsNullOrEmpty(LastCode) AndAlso Code IsNot Nothing AndAlso Code = "" Then Code = LastCode
        If Code Is Nothing Then Return
        Join(Code)
    End Sub
    Private Sub BtnJoin_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnJoin.Click
        Dim Code As String = TextJoinCode.Text
        If String.IsNullOrWhiteSpace(Code) Then
            Hint("请先输入房主发你的房间码！", HintType.Red)
            TextJoinCode.Focus()
            Return
        End If
        JoinInternal(Code.Trim())
    End Sub
    Private Sub TextJoinCode_KeyDown(sender As Object, e As KeyEventArgs) Handles TextJoinCode.KeyDown
        If e.Key = Key.Enter Then BtnJoin_Click(Nothing, Nothing)
    End Sub
    Public Sub JoinInternal(Code As String)
        If Code Is Nothing Then Return

        Dim ValidateResult = ValidateCodeFormat(Code)
        If ValidateResult IsNot Nothing Then
            Hint(ValidateResult, HintType.Red)
            Return
        End If
        Code = FixCodeFormat(Code)
        Logger.Info($"实际使用的联机房间码：{Code}")

        Dim Room = TcParse(Code)
        If Room Is Nothing Then
            Hint("房间码有误，请让房主重新发送房间码！", HintType.Red)
            Return
        End If
        IsServerSide = False
        RoomCode = Room.Item1
        NetworkName = Room.Item2
        NetworkSecret = Room.Item3
        Logger.Info($"尝试加入陶瓦房间，房间码 {RoomCode}，网络名 {NetworkName}")

        ModFirewall.CheckTerracottaFirewall()

        LastCode = Code
        ChangeState(LinkStates.Loading)
    End Sub
    Public Shared Function ValidateCodeFormat(Code As String) As String
        If String.IsNullOrWhiteSpace(Code) Then Return "房间码为空！"
        Code = FixCodeFormat(Code)
        If TcParse(Code) Is Nothing Then Return "房间码有误，请让房主重新发送房间码！"
        Return Nothing
    End Function

    Private Shared Function FixCodeFormat(Code As String) As String
        Code = Code.Between("【", "】").Between("[", "]").Upper.Trim
        Code = TcNormalizeWidth(Code)
        Code = Code.Replace(" ", "").Replace(vbCrLf, "").Replace(vbCr, "")
        Dim Index As Integer = Code.IndexOf("U/", StringComparison.Ordinal)
        If Index > 0 Then Code = Code.Substring(Index)
        Return Code
    End Function

    Private Shared Function TcNormalizeWidth(Text As String) As String
        If String.IsNullOrEmpty(Text) Then Return Text
        Dim Buffer As New Text.StringBuilder(Text.Length)
        For Each C As Char In Text
            Dim CharCode As Integer = AscW(C)
            Select Case CharCode
                Case &HFF01 To &HFF5E
                    Buffer.Append(ChrW(CharCode - &HFEE0))
                Case &H3000
                    Buffer.Append(" "c)
                Case &H2010, &H2011, &H2013, &H2014, &H2212
                    Buffer.Append("-"c)
                Case Else
                    Buffer.Append(C)
            End Select
        Next
        Return Buffer.ToString()
    End Function

    Public Shared Sub Join(Code As String)
        If LinkState <> LinkStates.Waiting Then
            Hint("你已经在联机房间中了！", HintType.Red)
        ElseIf FrmMain.PageCurrent = FormMain.PageType.Link Then
            FrmLinkMain.JoinInternal(Code)
        Else
            AutoJoinCode = Code
            FrmMain.PageChange(FormMain.PageType.Link, FormMain.PageSubType.LinkMain)
        End If
    End Sub

    Private Shared AutoJoinCode As String = Nothing
    Private Sub PanSelectJoin_Loaded() Handles PanSelectJoin.Loaded
        If AutoJoinCode Is Nothing Then Return
        If LinkState = LinkStates.Waiting Then JoinInternal(AutoJoinCode)
        AutoJoinCode = Nothing
    End Sub

#End Region

#Region "加载页面 (Loading | Failed)"

    Private Sub SwitchToLoading(OldState As LinkStates)

        TcFailCount = 0
        HostGuideToken += 1
        HostPort = 0
        HostNodeText = ""
        ClientAddress = Nothing

        LinkLoader.Start(IsForceRestart:=True)

        UpdateProgressBar(0)
        LabLoadTitle.Text = If(IsServerSide, "创建房间中", "加入房间中")
        UpdateLoadingPage("正在初始化……", "准备初始化")
    End Sub

    Private Sub SwitchToFailed(OldState As LinkStates)

        HostGuideToken += 1
        LinkLoader.Cancel()
        ProcessStop()

        UpdateProgressBar(1)

        LabLoadTitle.Text = FailReason
        Dim Brief As String = LinkLoader.Error.GetDisplay(False)
        Dim InnerEx As Exception = LinkLoader.Error
        If InnerEx.Message.StartsWithF("$") Then Brief = InnerEx.Message
        Do Until InnerEx.InnerException Is Nothing
            InnerEx = InnerEx.InnerException
            If InnerEx.Message.StartsWithF("$") Then Brief = InnerEx.Message
        Loop
        LabLoadDesc.Text = If(Brief.StartsWithF("$"), Brief.TrimStart("$"), LinkLoader.Error.GetDisplay(True))
        Logger.Warn(LinkLoader.Error, LabLoadTitle.Text)
    End Sub

    Private Sub UpdateLoadingPage(Title As String, FailBrief As String)
        If FailReason = FailBrief & "失败" Then Return
        FailReason = FailBrief & "失败"
        Logger.Info($"开始步骤：{Title}")
        RunInUiWait(
        Sub()
            If FrmLinkMain Is Nothing OrElse Not FrmLinkMain.LabLoadDesc.IsLoaded Then Return
            FrmLinkMain.LabLoadDesc.Text = Title
        End Sub)
    End Sub
    Private FailReason As String = "准备初始化"

    Private Sub BtnLoadCancel_Click() Handles BtnLoadCancel.Click
        ChangeState(LinkStates.Waiting)
    End Sub

    Private Sub CardLoad_MouseLeftButtonUp() Handles CardLoad.MouseLeftButtonUp
        ChangeState(LinkStates.Loading)
    End Sub

    Private Sub UpdateProgressBar(Optional Value As Double = -1)
        If Value = -1 Then Value = LinkLoader.Progress
        Dim DisplayingProgress As Double = ColumnProgressA.Width.Value
        If Math.Round(Value - DisplayingProgress, 3) = 0 Then Return
        If DisplayingProgress <= Value Then
            Dim NewProgress As Double = If(Value = 1, 1, DisplayingProgress +
                (Value - DisplayingProgress) ^ 2 * 0.5)
            AniStart({
                AaGridLengthWidth(ColumnProgressA, NewProgress - ColumnProgressA.Width.Value, 200),
                AaGridLengthWidth(ColumnProgressB, (1 - NewProgress) - ColumnProgressB.Width.Value, 200)
            }, "Link Progress")
        Else
            ColumnProgressA.Width = New GridLength(Value, GridUnitType.Star)
            ColumnProgressB.Width = New GridLength(1 - Value, GridUnitType.Star)
            AniStop("Link Progress")
        End If
    End Sub
    Private Sub CardLoad_SizeChanged() Handles CardLoad.SizeChanged
        RectProgressClip.Rect = New Rect(0, 0, CardLoad.ActualWidth, 12)
    End Sub

#End Region

#Region "完成页面 (Finished)"

    Private Sub SwitchToFinished(OldState As LinkStates)

        UpdatePanelVisibility()

        If IsServerSide Then
            LabFinishTitle.Text = "已创建房间"
            HostNodeText = TcNodeText()
            RefreshHostGuide()
            StartHostGuideWatch()
            BtnFinishExit.Text = "关闭"
            BtnFinishPing.ToolTip = "网络延迟"
            BtnFinishCopy.Visibility = Visibility.Visible
            Copy()

            BtnFinishPort.Visibility = Visibility.Collapsed
            LabFinishPort.Visibility = Visibility.Collapsed
            LineFinishPort.Visibility = Visibility.Collapsed
            BtnFinishIp.Visibility = Visibility.Collapsed
            LabFinishIp.Visibility = Visibility.Collapsed
        Else
            LabFinishTitle.Text = "已加入房间"
            LabFinishDesc.Text = $"在多人游戏页面的最下方就能找到联机房间！{vbCrLf}注意：使用离线登录时不要手动输入 IP！"
            BtnFinishExit.Text = "离开"
            BtnFinishPing.ToolTip = "与房主的延迟"
            BtnFinishCopy.Visibility = Visibility.Collapsed

            BtnFinishPort.Visibility = Visibility.Collapsed
            LabFinishPort.Visibility = Visibility.Collapsed
            LineFinishPort.Visibility = Visibility.Collapsed
            LabFinishIp.Visibility = Visibility.Visible
            LabFinishIp.Text = ClientAddress
            BtnFinishIp.Visibility = Visibility.Visible
        End If
        Update()
    End Sub

    Private Sub BtnFinishExit_Click() Handles BtnFinishExit.Click
        TryExit(False, False)
    End Sub

    Public Function TryExit(Silent As Boolean, Closing As Boolean) As Boolean
        If LinkState = LinkStates.Waiting OrElse LinkState = LinkStates.Failed Then Return False
        If Not Silent Then
            If IsServerSide Then
                If MyMsgBox("你确定要关闭联机房间吗？" & vbCrLf & "所有玩家都需要重新输入房间码才可加入游戏！", "退出联机", "确定", "取消", IsWarn:=True) = 2 Then Return True
            ElseIf Closing Then
                If MyMsgBox(If(IsServerSide, "你确定要关闭联机房间吗？", "你确定要离开联机房间吗？"), "退出联机", "确定", "取消") = 2 Then Return True
            End If
        End If
        ChangeState(LinkStates.Waiting)
        Return False
    End Function

    Private Sub Copy() Handles BtnFinishCopy.Click
        Dim CodeText As String = $"在 PCL PE 启动器中输入房间码【{GetInviteCode()}】，即可加入联机房间！"
        ClipboardSet(CodeText, False)
        Settings.Set("LinkLastAutoJoinInviteCode", CodeText)
        Hint("已复制房间码！", HintType.Green)
    End Sub
    Private Function GetInviteCode() As String
        Return RoomCode
    End Function

    Private Sub RefreshHostGuide()
        If FrmLinkMain Is Nothing Then Return
        Dim Alive As Boolean = HostPort > 0 AndAlso TcGamePortAlive(HostPort)
        Dim Text As String = $"把房间码发给朋友，让大家加入房间吧！{vbCrLf}房间码：{GetInviteCode()}{vbCrLf}节点：{HostNodeText}"
        If ManualPort > 0 Then Text &= vbCrLf & $"游戏端口：{ManualPort}（手动指定）"
        If HostPort > 0 AndAlso Not Alive Then
            Text &= vbCrLf & vbCrLf &
                    "注意：还没有检测到游戏端口。请现在进入游戏并选择「对局域网开放」，否则朋友会一直卡在「连接房主」。"
        End If
        If Not TcHasReachableNode Then
            Text &= vbCrLf & vbCrLf &
                    "注意：当前没有可用的联机节点，朋友很可能连不进来。请检查网络或代理后重新创建房间。"
        End If
        LabFinishDesc.Text = Text
    End Sub

    Private Sub StartHostGuideWatch()

        HostGuideToken += 1
        Dim MyToken As Integer = HostGuideToken

        If ManualPort > 0 Then TcBroadcastPort(ManualPort)
        RunInNewThread(
        Sub()
            For i = 1 To 600
                Thread.Sleep(3000)
                If MyToken <> HostGuideToken OrElse LinkState <> LinkStates.Finished Then Exit For

                If ManualPort > 0 Then TcBroadcastPort(ManualPort)

                Dim Json As JObject = TcGetStateJson(1500)
                Dim Port As Integer = 0
                If Json IsNot Nothing Then Integer.TryParse(If(Json("port")?.ToString, ""), Port)
                If Port <= 0 Then Port = HostPort
                Dim PortChanged As Boolean = Port <> HostPort
                If PortChanged Then HostPort = Port

                If PortChanged Then
                    RunInUi(Sub()
                                If MyToken <> HostGuideToken Then Return
                                RefreshHostGuide()
                            End Sub)
                End If
            Next
        End Sub, "陶瓦房主端口检测")
    End Sub

    Private Shared Function TcGamePortAlive(Port As Integer) As Boolean
        If Port <= 0 Then Return False
        Try
            Using Client As New TcpClient()
                Dim ConnectTask = Client.ConnectAsync("127.0.0.1", Port)
                Return ConnectTask.Wait(700) AndAlso Client.Connected
            End Using
        Catch
            Return False
        End Try
    End Function

    Private Shared Sub TcBroadcastPort(Port As Integer)
        If Port <= 0 OrElse Port > 65535 Then Return
        Try
            Dim Payload As Byte() = System.Text.Encoding.UTF8.GetBytes($"[MOTD]PCL PE 精确端口[/MOTD][AD]{Port}[/AD]")
            Using Udp As New UdpClient()
                Udp.EnableBroadcast = True
                For Each Ip As System.Net.IPAddress In System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName())
                    If Ip.AddressFamily <> AddressFamily.InterNetwork OrElse System.Net.IPAddress.IsLoopback(Ip) Then Continue For
                    If Ip.ToString().StartsWithF("10.144.144.") Then Continue For
                    Try
                        Udp.Send(Payload, Payload.Length, New System.Net.IPEndPoint(Ip, 4445))
                    Catch
                    End Try
                Next
                Try
                    Udp.Send(Payload, Payload.Length, New System.Net.IPEndPoint(System.Net.IPAddress.Parse("224.0.2.60"), 4445))
                Catch
                End Try
            End Using
        Catch ex As Exception
            Logger.Warn(ex, $"向陶瓦广播游戏端口失败（{Port}）")
        End Try
    End Sub

    Private Shared Function TcGameRunning() As Boolean
        Try
            Dim Game As Process = McLaunchProcess
            If Game Is Nothing Then Return False
            Return Not Game.HasExited
        Catch
            Return False
        End Try
    End Function

    Private Sub BtnFinishIp_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles BtnFinishIp.MouseLeftButtonUp
        ClipboardSet(ClientAddress, False)
        Hint("已复制服务器地址！", HintType.Green)
    End Sub

#End Region

    Private ReadOnly PathTerracotta As String = Paths.AppDataThenName & "Terracotta\"
    Private Const TcExeName As String = "terracotta.exe"

    Private Const TcStartupTimeout As Integer = 60

    Private Const TcRoomTimeout As Integer = 180

    Private IsServerSide As Boolean

    Private ClientAddress As String = Nothing

    Private HostPort As Integer = 0

    Private ManualPort As Integer = 0

    Private HostNodeText As String = ""

    Private Shared HostGuideToken As Integer = 0

    Private RoomCode As String

    Private NetworkName As String, NetworkSecret As String

    Private Shared ProcessTc As Process

    Private Shared TcPortDir As String

    Private Shared TcPort As Integer = 0

    Private Shared TcOwned As Boolean = False

    Private Shared LastTcState As String = Nothing

    Private Shared TcFailCount As Integer = 0

    Private Shared ReadOnly LogHistory As New Queue(Of String)(20)

#Region "加载"

    Private WithEvents LinkLoader As New LoaderCombo(Of Integer)("联机", {
        New LoaderTask(Of Integer, Integer)("获取配置", AddressOf InitConfig) With {.ProgressWeight = 4},
        New LoaderTask(Of Integer, Integer)("准备陶瓦联机模块", AddressOf InitPrepare) With {.ProgressWeight = 6},
        New LoaderTask(Of Integer, Integer)("启动陶瓦联机模块", AddressOf InitLaunch) With {.ProgressWeight = 20},
        New LoaderTask(Of Integer, Integer)("进入联机房间", AddressOf InitEnterRoom) With {.ProgressWeight = 70}
    })

    Private Sub InitConfig(Task As LoaderTask(Of Integer, Integer))
        UpdateLoadingPage("正在获取联机配置……", "获取联机配置")
        Try
            ServerLoader.WaitForExit(LoaderToSyncProgress:=Task)
        Catch ex As Exception
            Logger.Warn(ex, "获取联机配置失败，将直接使用陶瓦联机")
        End Try
        If ServerConfig Is Nothing OrElse ServerConfig("Link") Is Nothing Then
            Logger.Warn("没有可用的联机配置，将直接使用陶瓦联机")
            Return
        End If

        Dim DisableReason = ServerConfig("Link")?("DisableReason2")?.ToString
        If Not String.IsNullOrEmpty(DisableReason) Then Throw New Exception("$" & DisableReason)
    End Sub

    Private Shared Function ExtractEmbeddedTerracotta(TargetFolder As String) As Boolean
        Try
            Dim Asm As Reflection.Assembly = Reflection.Assembly.GetExecutingAssembly()
            Dim ResName As String = Nothing
            For Each Name As String In Asm.GetManifestResourceNames()
                If Name.EndsWith("Terracotta_Module.zip", StringComparison.OrdinalIgnoreCase) Then
                    ResName = Name
                    Exit For
                End If
            Next
            If ResName Is Nothing Then Return False
            IO.Directory.CreateDirectory(TargetFolder)
            Using Stream As IO.Stream = Asm.GetManifestResourceStream(ResName)
                Using Zip As New IO.Compression.ZipArchive(Stream, IO.Compression.ZipArchiveMode.Read)
                    For Each Entry As IO.Compression.ZipArchiveEntry In Zip.Entries
                        Dim Dest As String = IO.Path.Combine(TargetFolder, Entry.FullName)
                        IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(Dest))
                        Using EntryStream As IO.Stream = Entry.Open()
                            Using Out As IO.FileStream = IO.File.Create(Dest)
                                EntryStream.CopyTo(Out)
                            End Using
                        End Using
                    Next
                End Using
            End Using
            Logger.Info($"已从内嵌资源释放陶瓦联机模块：{TargetFolder}")
            Return True
        Catch ex As Exception
            Logger.Warn(ex, "释放内嵌陶瓦联机模块失败")
            Return False
        End Try
    End Function

    Private Sub InitPrepare(Task As LoaderTask(Of Integer, Integer))
        UpdateLoadingPage("正在准备陶瓦联机模块……", "准备联机模块")
        If Not System.IO.Directory.Exists(PathTerracotta) Then System.IO.Directory.CreateDirectory(PathTerracotta)
        Dim Target As String = PathTerracotta & TcExeName
        If Not FileUtils.Exists(Target) Then
            Dim Source As String = Nothing
            For Each Candidate As String In {Paths.Base & "Terracotta\" & TcExeName, Paths.Base & TcExeName}
                If FileUtils.Exists(Candidate) Then
                    Source = Candidate
                    Exit For
                End If
            Next
            If Source Is Nothing Then

                UpdateLoadingPage("正在准备陶瓦联机模块……", "解压联机模块")
                If ExtractEmbeddedTerracotta(PathTerracotta) Then
                    Source = Target
                End If
            End If
            If Source Is Nothing Then

                UpdateLoadingPage("正在下载陶瓦联机模块……", "下载联机模块")
                If DownloadTerracottaModule(Paths.Base & "Terracotta\") Then
                    Source = Paths.Base & "Terracotta\" & TcExeName
                Else
                    Throw New Exception($"$找不到陶瓦联机模块（{TcExeName}），自动下载也失败了。{vbCrLf}请检查网络后重试，或手动把 terracotta.exe 放到：{vbCrLf}{PathTerracotta}")
                End If
            End If
            If Not FileUtils.Exists(Target) Then
                FileUtils.Copy(Source, Target)
                Logger.Info($"已复制陶瓦联机模块：{Source} -> {Target}")
            End If
        End If

        For Each DllName As String In {"VCRUNTIME140.dll", "MSVCP140.dll"}
            If Not FileUtils.Exists(PathTerracotta & DllName) Then
                For Each Candidate As String In {Paths.Base & "Terracotta\" & DllName, Paths.Base & DllName}
                    If FileUtils.Exists(Candidate) Then
                        FileUtils.Copy(Candidate, PathTerracotta & DllName)
                        Exit For
                    End If
                Next
            End If
        Next
        Task.Progress = 0.1
    End Sub

    Private Sub InitLaunch(Task As LoaderTask(Of Integer, Integer))
        UpdateLoadingPage("正在启动陶瓦联机……", "启动联机模块")

        TcKillStaleProcesses()

        ProcessStop()

        TcPortDir = IO.Path.Combine(IO.Path.GetTempPath(), "PCLPro-Terracotta-" & Guid.NewGuid().ToString("N"))
        If Not IO.Directory.Exists(TcPortDir) Then IO.Directory.CreateDirectory(TcPortDir)
        Dim PortFile As String = IO.Path.Combine(TcPortDir, "http")

        Dim Info As New ProcessStartInfo With {
            .FileName = PathTerracotta & TcExeName,
            .Arguments = $"--hmcl2 ""{PortFile}""",
            .WorkingDirectory = PathTerracotta,
            .UseShellExecute = False,
            .CreateNoWindow = True,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .StandardOutputEncoding = Encoding.UTF8,
            .StandardErrorEncoding = Encoding.UTF8
        }
        Logger.Info($"正在启动陶瓦联机：{Info.FileName} {Info.Arguments}")
        Try
            ProcessTc = Process.Start(Info)
        Catch ex As Exception
            Throw New Exception($"$无法启动陶瓦联机模块：{ex.Message}{vbCrLf}请检查安全软件是否拦截了 {TcExeName}。", ex)
        End Try
        If ProcessTc Is Nothing Then Throw New Exception("$无法启动陶瓦联机模块。")
        TcOwned = True
        AddHandler ProcessTc.OutputDataReceived, Sub(sender, e) TcLogLine(e.Data)
        AddHandler ProcessTc.ErrorDataReceived, Sub(sender, e) TcLogLine(e.Data)
        ProcessTc.BeginOutputReadLine()
        ProcessTc.BeginErrorReadLine()

        Dim Start As Long = GetTimeMs()
        Do
            If Task.IsCanceled Then Throw New OperationCanceledException
            If FileUtils.Exists(PortFile) Then
                Try
                    Dim Text As String = System.IO.File.ReadAllText(PortFile).Trim()
                    Dim Port As Integer = 0
                    If Not Integer.TryParse(Text, Port) Then
                        Dim Json = Text.DeserializeJson()
                        If TypeOf Json Is JObject Then Integer.TryParse(CStr(CType(Json, JObject)("port")), Port)
                    End If
                    If Port > 0 AndAlso Port <= 65535 Then
                        TcPort = Port
                        Exit Do
                    End If
                Catch ex As Exception
                    Logger.Warn(ex, "读取陶瓦端口文件失败")
                End Try
            End If
            If ProcessTc.HasExited Then Throw New Exception("$陶瓦联机模块启动后意外退出。" & vbCrLf & "近期日志：" & vbCrLf & String.Join(vbCrLf, LogHistory.ToArray()))
            If GetTimeMs() - Start > TcStartupTimeout * 1000 Then Throw New Exception($"$等待陶瓦联机模块启动超时（{TcStartupTimeout} 秒）。请检查是否有安全软件阻止了 {TcExeName}。")
            Task.Progress = 0.1 + Math.Min((GetTimeMs() - Start) / (TcStartupTimeout * 1000.0), 1) * 0.1
            Thread.Sleep(150)
        Loop
        Logger.Info($"陶瓦本地接口端口：{TcPort}")

        Dim ApiStart As Long = GetTimeMs()
        Do
            If Task.IsCanceled Then Throw New OperationCanceledException
            If TcGetStateJson(800) IsNot Nothing Then Exit Do
            If GetTimeMs() - ApiStart > 15000 Then Throw New Exception("$陶瓦联机模块的本地接口没有响应，请重试或检查安全软件设置。")
            Thread.Sleep(200)
        Loop
        Task.Progress = 0.25
    End Sub

    Private Sub InitEnterRoom(Task As LoaderTask(Of Integer, Integer))

        If Not IsServerSide AndAlso Not TcEnsureNodesUsable() Then
            Throw New Exception("$找不到可用的陶瓦联机节点，无法加入房间。" & vbCrLf &
                                "当前网络下陶瓦的公共节点都连不上（它内置的节点已经失效）。" & vbCrLf &
                                "请检查网络、代理或防火墙后点击重试；如果仍然不行，请让房主重新创建房间。")
        End If
        Dim Start As Long = GetTimeMs()
        Dim LastRequest As Long = 0
        Dim FailCount As Integer = 0
        Dim EnterFailCount As Integer = 0
        Dim RecoverCount As Integer = 0
        Dim Succeeded As Boolean = False
        Dim CurState As String = ""
        Dim LastStateIndex As Integer = -1
        Do
            If Task.IsCanceled Then Throw New OperationCanceledException

            If (CurState = "" OrElse CurState = "waiting") AndAlso GetTimeMs() - LastRequest > 1000 Then
                LastRequest = GetTimeMs()
                Dim EnterBody As String = Nothing
                Dim EnterStatus As Integer
                If IsServerSide Then
                    EnterStatus = TcApiStatus($"/state/scanning?player={Uri.EscapeDataString(TcPlayerName())}{TcPublicNodesQuery()}", EnterBody)
                Else
                    EnterStatus = TcApiStatus($"/state/guesting?room={Uri.EscapeDataString(RoomCode)}&player={Uri.EscapeDataString(TcPlayerName())}{TcPublicNodesQuery()}", EnterBody, 4000)
                End If

                If EnterStatus >= 400 Then
                    EnterFailCount += 1
                    Logger.Warn($"陶瓦拒绝联机请求（HTTP {EnterStatus}，第 {EnterFailCount} 次）：{EnterBody}")

                    If EnterFailCount >= 8 Then
                        Throw New Exception("$" & $"陶瓦联机模块拒绝了{If(IsServerSide, "创建房间", "加入房间")}请求（HTTP {EnterStatus}）。{vbCrLf}服务端回应：{If(EnterBody, "（无）")}{vbCrLf}请点击重试；如果反复失败，请让房主重新创建房间后把新的房间码发给你。")
                    End If
                    TcApi("/state/ide", 1200)
                    LastStateIndex = -1
                    Thread.Sleep(600 + EnterFailCount * 250)
                ElseIf EnterStatus > 0 Then
                    EnterFailCount = 0
                    Logger.Info($"陶瓦已接受联机请求（HTTP {EnterStatus}）：{EnterBody}")
                Else

                    Logger.Warn($"调用陶瓦联机接口无响应：{If(IsServerSide, "/state/scanning", "/state/guesting")}")
                End If
            End If

            Dim Json As JObject = TcGetStateJson(1500)
            If Json Is Nothing Then
                FailCount += 1
                If FailCount >= 25 Then Throw New Exception("$无法与陶瓦联机模块通信，模块可能已被结束。" & vbCrLf & "近期日志：" & vbCrLf & String.Join(vbCrLf, LogHistory.ToArray()))
                Thread.Sleep(250)
                Continue Do
            End If
            FailCount = 0
            Dim State As String = If(Json("state")?.ToString, "")

            Dim StateIndex As Integer = -1
            Integer.TryParse(If(Json("index")?.ToString, ""), StateIndex)
            If StateIndex >= 0 AndAlso StateIndex <= LastStateIndex AndAlso State <> "host-ok" AndAlso State <> "guest-ok" Then
                Thread.Sleep(300)
                Continue Do
            End If
            If StateIndex >= 0 Then LastStateIndex = StateIndex
            CurState = State
            LastTcState = State
            Select Case State
                Case "host-scanning", "waiting"
                    If IsServerSide Then
                        Dim RoomInScan As String = If(Json("room")?.ToString, "")
                        If Not String.IsNullOrEmpty(RoomInScan) Then RoomCode = RoomInScan
                        If String.IsNullOrEmpty(RoomCode) Then
                            UpdateLoadingPage("请在游戏内选择「对局域网开放」，陶瓦正在自动扫描游戏端口……", "扫描游戏端口")
                        Else
                            UpdateLoadingPage($"请在游戏内选择「对局域网开放」，陶瓦正在自动扫描游戏端口…… 房间码：{RoomCode}", "扫描游戏端口")
                        End If
                    Else
                        UpdateLoadingPage("正在等待房主响应……", "连接房主")
                    End If
                Case "host-starting"
                    UpdateLoadingPage($"正在创建陶瓦联机房间……{vbCrLf}节点：{TcNodeText()}", "创建联机房间")
                Case "guest-connecting"
                    UpdateLoadingPage("正在连接陶瓦公共节点……", "连接联机节点")
                Case "guest-starting"

                    Dim Difficulty As String = If(Json("difficulty")?.ToString, "").ToUpperInvariant
                    Dim GuestText As String = "正在与房主建立点对点连接……"
                    Select Case Difficulty
                        Case "EASIEST", "SIMPLE"
                            GuestText &= vbCrLf & "网络穿透难度：较低，通常很快就能连上。"
                        Case "MEDIUM", "TOUGH"
                            GuestText &= vbCrLf & "网络穿透难度：较高，可能需要多等一会儿；若长时间连不上，请让房主换个节点重新创建房间。"
                    End Select
                    UpdateLoadingPage(GuestText, "连接房主")
                Case "host-ok"
                    RoomCode = If(Json("room")?.ToString, RoomCode)
                    If String.IsNullOrEmpty(RoomCode) Then Continue Do
                    Integer.TryParse(If(Json("port")?.ToString, ""), HostPort)
                    Succeeded = True
                Case "guest-ok"
                    ClientAddress = If(Json("url")?.ToString, "")
                    If String.IsNullOrEmpty(ClientAddress) Then ClientAddress = "127.0.0.1"
                    Succeeded = True
                Case "exception"
                    Dim Kind As Integer = -1
                    Integer.TryParse(If(Json("type")?.ToString, ""), Kind)

                    If Not IsServerSide AndAlso (Kind = 0 OrElse Kind = 1 OrElse Kind = 5) AndAlso RecoverCount < 10 Then
                        RecoverCount += 1
                        Logger.Info($"陶瓦返回异常 {Kind}，复位后重试第 {RecoverCount} 次")

                        Dim RetryText As String
                        Select Case Kind
                            Case 1
                                RetryText = $"房主拒绝了连接，正在重试……（第 {RecoverCount} 次尝试）{vbCrLf}请让房主确认游戏内已选择「对局域网开放」。"
                            Case 5
                                RetryText = $"与房主的握手失败，正在重试……（第 {RecoverCount} 次尝试）{vbCrLf}若一直失败，请让房主重新创建房间。"
                            Case Else
                                RetryText = $"正在等待房主开放游戏端口……（第 {RecoverCount} 次尝试）{vbCrLf}若多次失败，请先确认房间码是否输入正确。"
                        End Select
                        UpdateLoadingPage(RetryText, "重试连接")
                        TcApi("/state/ide", 1200)
                        LastStateIndex = -1
                        CurState = ""
                        Thread.Sleep(900)
                        Continue Do
                    End If
                    Throw New Exception("$" & TcExceptionText(Json))
            End Select
            If Succeeded Then Exit Do

            Dim Elapsed As Double = (GetTimeMs() - Start) / 1000.0
            If Elapsed > TcRoomTimeout Then
                If IsServerSide Then
                    Throw New Exception($"$等待游戏端口超时。{vbCrLf}请先在游戏内选择「对局域网开放」，然后点击重试。")
                Else
                    Throw New Exception($"$连接房主超时。可能的原因：{vbCrLf}- 房间码输入有误{vbCrLf}- 房主已关闭房间{vbCrLf}- 你或房主的网络环境不佳")
                End If
            End If
            Task.Progress = 0.25 + Math.Min(Elapsed / 20, 1) * 0.7
            Thread.Sleep(300)
        Loop
        Logger.Info($"陶瓦联机就绪：房间码 {RoomCode}，连接地址 {If(ClientAddress, "（房主）")}")
        Telemetry("联机成功", "Server", IsServerSide)
        Task.Progress = 1
    End Sub

    Private Shared Function TcExceptionText(Json As JObject) As String
        Dim Kind As Integer = -1
        Integer.TryParse(If(Json("type")?.ToString, ""), Kind)
        Select Case Kind
            Case 0
                Return "无法与房主建立联机通道。" & vbCrLf &
                       "常见原因：" & vbCrLf &
                       "- 房主还没有在游戏内选择「对局域网开放」" & vbCrLf &
                       "- 你和房主之间没有可用的联机节点（可换一个节点重试，并检查代理与防火墙）"
            Case 1
                Return "房主拒绝了连接，请让房主检查游戏内的「对局域网开放」。"
            Case 2
                Return "本机的联机组网组件崩溃了，请点击重试。"
            Case 3
                Return "房主的联机组网组件崩溃了，请让房主重新创建房间。"
            Case 4
                Return "与游戏服务器的连接被重置，请确认游戏仍在运行。"
            Case 5
                Return "陶瓦联机握手失败，请让房主重新创建房间。"
            Case Else
                Return "陶瓦联机出现未知错误，请点击重试。"
        End Select
    End Function

    Private Function TcPlayerName() As String
        Return If(IsServerSide, "PCL-Host-", "PCL-Guest-") & Identify.GetStableHashCode().ConvertRadix(36).TrimStart("-"c).EnsureLength("0", 6)
    End Function

    Private Shared PublicNodeCache As List(Of String) = Nothing

    Private Const TcNodeCacheMs As Integer = 10 * 60 * 1000
    Private Shared PublicNodeCacheTime As Long = 0

    Private Shared TcHasReachableNode As Boolean = False

    Private Shared Function TcFetchPublicNodes() As List(Of String)
        If PublicNodeCache IsNot Nothing AndAlso GetTimeMs() - PublicNodeCacheTime < TcNodeCacheMs Then Return PublicNodeCache
        Dim Nodes As New List(Of String)
        Dim Sources As New List(Of String)

        Try
            Dim Text As String = TcHttpGet("https://terracotta.glavo.site/nodes")
            Dim Json = Text.DeserializeJson()
            If TypeOf Json Is JArray Then
                For Each Item In CType(Json, JArray)
                    Dim Url As String = CStr(Item("url")).Trim()
                    If Url <> "" AndAlso Not Sources.Contains(Url) Then Sources.Add(Url)
                Next
            End If
        Catch ex As Exception
            Logger.Warn(ex, "获取陶瓦节点清单失败")
        End Try

        If Sources.Count = 0 Then
            Sources.Add("https://etnode.zkitefly.eu.org/node1")
            Sources.Add("https://etnode.zkitefly.eu.org/node2")
        End If

        For Each Source As String In Sources
            Try
                TcResolveNode(Source, Nodes)
            Catch ex As Exception
                Logger.Warn(ex, "解析陶瓦公共节点失败：" & Source)
            End Try
        Next

        Dim Reachable As New List(Of String)
        For Each Node As String In Nodes
            If TcNodeReachable(Node) Then Reachable.Add(Node)
        Next
        If Reachable.Count > 0 Then
            TcHasReachableNode = True
            Nodes = Reachable
        Else
            TcHasReachableNode = False
            Logger.Warn("没有可连通的陶瓦公共节点：" & If(Nodes.Count > 0, String.Join("，", Nodes), "（节点清单为空）"))
        End If
        Logger.Info("陶瓦公共节点：" & If(Nodes.Count > 0, String.Join("，", Nodes), "（无）"))

        If Nodes.Count > 0 Then
            PublicNodeCache = Nodes
            PublicNodeCacheTime = GetTimeMs()
        Else
            PublicNodeCache = Nothing
            PublicNodeCacheTime = 0
        End If
        Return Nodes
    End Function

    Private Shared Function TcNodeReachable(Node As String) As Boolean
        If Node.StartsWith("udp://", StringComparison.OrdinalIgnoreCase) Then Return True
        Return TcPingNode(Node) >= 0
    End Function

    Private Shared Function TcEnsureNodesUsable() As Boolean
        TcFetchPublicNodes()
        Return TcHasReachableNode
    End Function

    Private Shared Sub TcResolveNode(Source As String, Nodes As List(Of String))
        For Each Prefix As String In {"tcp://", "udp://", "ws://", "wss://"}
            If Source.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) Then
                If Not Nodes.Contains(Source) Then Nodes.Add(Source)
                Return
            End If
        Next
        If Not Source.StartsWith("http", StringComparison.OrdinalIgnoreCase) Then Return
        Dim Text As String = TcHttpGet(Source)
        For Each Line As String In Text.Split({vbCrLf, vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            Dim Node As String = Line.Trim()
            For Each Prefix As String In {"tcp://", "udp://", "ws://", "wss://"}
                If Node.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) Then
                    If Not Nodes.Contains(Node) Then Nodes.Add(Node)
                    Return
                End If
            Next
        Next
        Logger.Warn($"节点解析地址没有返回有效中继：{Source}")
    End Sub

    Private Shared Function TcHttpGet(Url As String) As String
        Dim Request As HttpWebRequest = CType(WebRequest.Create(Url), HttpWebRequest)
        Request.Timeout = 5000
        Request.UserAgent = "PCL"
        Using Response As WebResponse = Request.GetResponse()
            Using Reader As New StreamReader(Response.GetResponseStream(), Text.Encoding.UTF8)
                Return Reader.ReadToEnd()
            End Using
        End Using
    End Function

    Private Shared Function TcPublicNodesQuery() As String
        Dim Nodes As List(Of String) = TcFetchPublicNodes()
        Dim Query As New Text.StringBuilder
        If Not String.IsNullOrEmpty(TcSelectedNode) Then Query.Append("&public_nodes=" & Uri.EscapeDataString(TcSelectedNode))
        For Each Node As String In Nodes
            If Node = TcSelectedNode Then Continue For
            Query.Append("&public_nodes=" & Uri.EscapeDataString(Node))
        Next
        Return Query.ToString()
    End Function

    Private Shared TcSelectedNode As String = ""

    Private Shared Function TcNodeText() As String
        Dim Node As String = If(TcSelectedNode, "")
        Dim Nodes = TcFetchPublicNodes()
        If Node = "" Then Node = If(Nodes.Count > 0, Nodes(0), "")
        If Node = "" Then Return "未获取到可用节点"
        Dim Index As Integer = Nodes.IndexOf(Node)
        Return TcNodeName(If(Index < 0, 0, Index))
    End Function

    Private Shared Function TcNodeName(Index As Integer) As String
        Const Numerals As String = "一二三四五六七八九十"
        Dim Text As String = If(Index < Numerals.Length, Numerals.Substring(Index, 1), (Index + 1).ToString())
        Return Text & "号公共节点"
    End Function

    Private Shared Function TcPingColor(Ping As Integer) As String
        If Ping < 0 Then Return "#B8B8B8"
        If Ping <= 80 Then Return "#2FA84F"
        If Ping <= 200 Then Return "#E8A33D"
        Return "#E05B5B"
    End Function

    Private Shared Function TcPingText(Ping As Integer) As String
        Return If(Ping >= 0, $"{Ping} ms", "不可用")
    End Function

    Private Shared Function TcPingNode(Node As String) As Integer
        Try
            If Node.StartsWith("udp://", StringComparison.OrdinalIgnoreCase) Then Return -1
            Dim SchemeEnd As Integer = Node.IndexOf("://", StringComparison.Ordinal)
            If SchemeEnd < 0 Then Return -1
            Dim HostPort As String = Node.Substring(SchemeEnd + 3)
            Dim Colon As Integer = HostPort.LastIndexOf(":"c)
            If Colon <= 0 Then Return -1
            Dim Host As String = HostPort.Substring(0, Colon)
            Dim Port As Integer
            If Not Integer.TryParse(HostPort.Substring(Colon + 1), Port) OrElse Port <= 0 Then Return -1
            Dim Watch As Stopwatch = Stopwatch.StartNew()
            Using Client As New TcpClient()
                Dim Task = Client.ConnectAsync(Host, Port)
                If Not Task.Wait(2500) Then Return -1
                If Not Client.Connected Then Return -1
            End Using
            Return CInt(Math.Min(Watch.ElapsedMilliseconds, Integer.MaxValue))
        Catch
            Return -1
        End Try
    End Function

    Private Sub ComboCreateNode_Loaded(sender As Object, e As RoutedEventArgs)
        If ComboCreateNode.Items.Count > 0 Then Return
        ComboCreateNode.Items.Add(New MyComboBoxItem With {.Content = "正在获取公共节点……", .Tag = ""})
        ComboCreateNode.SelectedIndex = 0
        RunInNewThread(
            Sub()
                Dim Nodes = TcFetchPublicNodes()
                Dim Pings = Nodes.Select(Function(N) New KeyValuePair(Of String, Integer)(N, TcPingNode(N))).ToList()
                RunInUi(
                    Sub()
                        ComboCreateNode.Items.Clear()
                        Dim BestIndex As Integer = 0, BestPing As Integer = Integer.MaxValue
                        For i = 0 To Pings.Count - 1
                            Dim Item = Pings(i)
                            Dim Row As New StackPanel With {.Orientation = Orientation.Horizontal, .VerticalAlignment = VerticalAlignment.Center}
                            Row.Children.Add(New TextBlock With {.Text = TcNodeName(i), .VerticalAlignment = VerticalAlignment.Center})
                            Row.Children.Add(New TextBlock With {
                                .Text = TcPingText(Item.Value),
                                .VerticalAlignment = VerticalAlignment.Center,
                                .Margin = New Thickness(10, 0, 0, 0),
                                .FontSize = 12.5,
                                .Foreground = New System.Windows.Media.SolidColorBrush(System.Windows.Media.ColorConverter.ConvertFromString(TcPingColor(Item.Value)))
                            })
                            Dim NodeItem As New MyComboBoxItem With {
                                .Content = Row,
                                .Tag = Item.Key,
                                .ToolTip = If(Item.Value >= 0, $"{TcNodeName(i)}：{Item.Value} ms", $"{TcNodeName(i)}：不可用")
                            }
                            System.Windows.Automation.AutomationProperties.SetName(NodeItem, If(Item.Value >= 0, $"{TcNodeName(i)}，延迟 {Item.Value} ms", $"{TcNodeName(i)}，不可用"))
                            ComboCreateNode.Items.Add(NodeItem)
                            If Item.Value >= 0 AndAlso Item.Value < BestPing Then
                                BestPing = Item.Value
                                BestIndex = i
                            End If
                        Next
                        If Pings.Count = 0 Then
                            LabNodeStatus.Text = "没有可用的公共节点，联机可能无法建立"
                        Else
                            ComboCreateNode.SelectedIndex = BestIndex
                            LabNodeStatus.Text = $"已测速，共 {Pings.Count} 个节点"
                        End If
                    End Sub)
            End Sub, "TcNodePing")
    End Sub

    Private Sub ComboCreateNode_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        Dim Item = TryCast(ComboCreateNode.SelectedItem, MyComboBoxItem)
        If Item IsNot Nothing Then TcSelectedNode = TryCast(Item.Tag, String)
    End Sub

    Private Shared Sub TcKillStaleProcesses()
        For Each Proc As Process In Process.GetProcesses()
            Try
                If Not Proc.ProcessName.StartsWith("terracotta", StringComparison.OrdinalIgnoreCase) Then Continue For
                Dim FilePath As String = Nothing
                Try
                    FilePath = Proc.MainModule?.FileName
                Catch

                End Try
                If Not String.IsNullOrEmpty(FilePath) Then
                    Dim IsOwnedPath As Boolean =
                        FilePath.StartsWith(Paths.AppData, StringComparison.OrdinalIgnoreCase) OrElse
                        FilePath.StartsWith(Paths.Base, StringComparison.OrdinalIgnoreCase) OrElse
                        FilePath.StartsWith(IO.Path.GetTempPath, StringComparison.OrdinalIgnoreCase)
                    If Not IsOwnedPath Then Continue For
                End If
                Logger.Info($"发现残留的陶瓦进程，正在结束：PID {Proc.Id}，路径：{If(FilePath, "未知")}")

                Try
                    TcApi("/panic?peaceful=true", 800)
                Catch
                End Try
                Proc.Kill()
                Proc.WaitForExit(1500)
            Catch
            End Try
        Next

        Thread.Sleep(300)
    End Sub

#End Region

#Region "接口与进程"

    Private Shared Function TcApi(Path As String, Optional TimeoutMs As Integer = 1500) As String
        Dim Body As String = Nothing
        TcApiStatus(Path, Body, TimeoutMs)
        Return Body
    End Function

    Private Shared Function TcApiStatus(Path As String, ByRef Body As String, Optional TimeoutMs As Integer = 1500) As Integer
        Body = Nothing
        If TcPort <= 0 Then Return -1
        Try
            Dim Request As HttpWebRequest = CType(WebRequest.Create($"http://127.0.0.1:{TcPort}{Path}"), HttpWebRequest)
            Request.Method = "GET"
            Request.Timeout = TimeoutMs
            Request.ReadWriteTimeout = TimeoutMs
            Request.Proxy = Nothing
            Request.KeepAlive = False
            Using Response As HttpWebResponse = CType(Request.GetResponse(), HttpWebResponse)
                Using Reader As New System.IO.StreamReader(Response.GetResponseStream(), Encoding.UTF8)
                    Body = Reader.ReadToEnd()
                End Using
                Return CInt(Response.StatusCode)
            End Using
        Catch ex As WebException

            Try
                If ex.Response IsNot Nothing Then
                    Dim ErrorResponse As HttpWebResponse = CType(ex.Response, HttpWebResponse)
                    Using Reader As New System.IO.StreamReader(ErrorResponse.GetResponseStream(), Encoding.UTF8)
                        Body = Reader.ReadToEnd()
                    End Using
                    Return CInt(ErrorResponse.StatusCode)
                End If
            Catch
            End Try
            Return -1
        Catch ex As Exception
            Return -1
        End Try
    End Function

    Private Shared Function TcGetStateJson(Optional TimeoutMs As Integer = 1500) As JObject
        Dim RawText As String = TcApi("/state", TimeoutMs)
        If String.IsNullOrEmpty(RawText) Then Return Nothing
        Try
            Dim Json = RawText.DeserializeJson()
            If TypeOf Json Is JObject Then Return CType(Json, JObject)
        Catch ex As Exception
            Logger.Warn(ex, "解析陶瓦状态失败：" & RawText)
        End Try
        Return Nothing
    End Function

    Private Shared Sub TcLogLine(Line As String)
        If String.IsNullOrWhiteSpace(Line) Then Return
        Logger.Info("[Terracotta] " & Line)
        SyncLock LogHistory
            LogHistory.Enqueue(Line)
            Do While LogHistory.Count > 20
                LogHistory.Dequeue()
            Loop
        End SyncLock
    End Sub

    Public Shared Sub ProcessStop()
        Try
            If TcPort > 0 Then

                TcApi("/state/ide", 800)
                If TcOwned Then

                    TcApi("/panic?peaceful=true", 800)
                    Thread.Sleep(250)
                End If
            End If
        Catch ex As Exception
            Logger.Warn(ex, "退出陶瓦联机房间失败")
        End Try
        Try
            If ProcessTc IsNot Nothing AndAlso Not ProcessTc.HasExited Then
                Logger.Info($"停止所启动的陶瓦联机，PID：{ProcessTc.Id}")
                ProcessTc.Kill()
                ProcessTc.WaitForExit(2000)
            End If
        Catch ex As Exception
            Logger.Warn(ex, "结束陶瓦联机进程失败")
        End Try
        Try
            If ProcessTc IsNot Nothing Then ProcessTc.Dispose()
        Catch
        End Try
        ProcessTc = Nothing
        TcPort = 0
        TcOwned = False
        LastTcState = Nothing
        TcFailCount = 0
        Try
            If TcPortDir IsNot Nothing AndAlso IO.Directory.Exists(TcPortDir) Then IO.Directory.Delete(TcPortDir, True)
        Catch ex As Exception
            Logger.Warn(ex, "清理陶瓦端口文件目录失败")
        End Try
        TcPortDir = Nothing
    End Sub

    Private Sub Panic(Brief As String, Detail As String)
        If LinkState = LinkStates.Loading Then
            FailReason = Brief
            LinkLoader.Failed(New Exception(Detail))
        Else
            MyMsgBox(Detail, "联机出错：" & Brief, IsWarn:=True)
            ChangeState(LinkStates.Waiting)
            Telemetry("联机失败", "Exception", FilterUserName(Brief & "：" & Detail, "*"))
        End If
    End Sub

#End Region

#Region "定时任务"

    Private Sub StartTimerThread() Handles Me.Loaded
        Static IsTimerStarted As Boolean = False
        If IsTimerStarted Then Return
        RunInNewThread(
        Sub()
            Dim Counter As Integer = 0
            Do While True
                Try
                    Thread.Sleep(200)
                    Counter += 200

                    If Counter >= 1000 Then
                        Counter = 0
                        Update()
                    End If

                    If LinkState = LinkStates.Loading Then RunInUi(AddressOf UpdateProgressBar)
                Catch ex As Exception
                    Logger.Error(ex, "联机模块主时钟出错")
                    Thread.Sleep(10000)
                End Try
            Loop
        End Sub, "Link Timer")
    End Sub

    Private Sub Update() Handles Me.Loaded
        If LinkState <> LinkStates.Finished Then Return

        If FrmMain.PageCurrent = FormMain.PageType.Link Then
            RunInUi(
            Sub()
                AniStart(AaRotateTransform(ImgFinishLogo, 500, 5000), "Link Logo Rotation")
                LabFinishPing.Text = "已连接"
                LabFinishPlayer.Text = "陶瓦联机"
            End Sub)
        End If

        Dim Json As JObject = TcGetStateJson(1000)
        If Json Is Nothing Then
            TcFailCount += 1
            If TcFailCount >= 5 Then

                TcFailCount = 0
                CheckCrash()
            End If
            Return
        End If
        TcFailCount = 0
        Dim State As String = If(Json("state")?.ToString, "")
        Select Case State
            Case "waiting"
                Logger.Info("陶瓦联机房间已结束，回到初始页面")
                ChangeState(LinkStates.Waiting)
            Case "exception"
                Panic("联机出错", TcExceptionText(Json))
            Case Else
                If State <> LastTcState Then Logger.Info($"陶瓦状态：{LastTcState} -> {State}")
                LastTcState = State
        End Select
    End Sub

    Private Sub CheckCrash()
        If LinkState <> LinkStates.Finished Then Return
        If TcPort > 0 AndAlso TcGetStateJson(800) IsNot Nothing Then Return
        Panic("联机已断开", "无法与陶瓦联机模块通信，模块可能已被结束。" & vbCrLf & "近期日志：" & vbCrLf & String.Join(vbCrLf, LogHistory.ToArray()))
    End Sub

#End Region

End Class
