Imports System.IO
Imports System.Text

Public Module ModFirewall

    Private HasChecked As Boolean = False

    Public Sub CheckAtStartup()
        If Settings.Get(Of Boolean)("UiFirewallGuideShown") Then Return
        RunInNewThread(
            Sub()
                Thread.Sleep(3000)
                RunInUi(
                Sub()
                    Try
                        If Settings.Get(Of Boolean)("UiFirewallGuideShown") Then Return
                        Settings.Set("UiFirewallGuideShown", True)
                        Dim Apps As List(Of String) = GetTerracottaApps()
                        Dim Missing As New List(Of String)
                        For Each App As String In Apps
                            If Not IsAllowedByFirewall(App) Then Missing.Add(App)
                        Next
                        If Missing.Count = 0 Then
                            Logger.Info("防火墙检查：陶瓦联机所需的程序均已放行")
                            Return
                        End If
                        Logger.Info("防火墙首次启动引导：" & String.Join("、", Missing))
                        ShowFirewallGuide(Missing)
                    Catch ex As Exception
                        Logger.Warn(ex, "防火墙首次启动引导失败")
                    End Try
                End Sub)
            End Sub, "防火墙引导")
    End Sub

    Public Sub CheckTerracottaFirewall()
        If HasChecked Then Return
        HasChecked = True
        Try
            Dim Apps As List(Of String) = GetTerracottaApps()
            If Apps.Count = 0 Then Return
            Dim Missing As New List(Of String)
            For Each App As String In Apps
                If Not IsAllowedByFirewall(App) Then Missing.Add(App)
            Next
            If Missing.Count = 0 Then
                Logger.Info("防火墙检查：陶瓦联机所需的程序均已放行")
                Return
            End If
            Logger.Warn("防火墙检查：以下程序可能被拦截：" & String.Join("、", Missing))
            If Settings.Get(Of Boolean)("UiFirewallGuideShown") Then Return
            Settings.Set("UiFirewallGuideShown", True)
            ShowFirewallGuide(Missing)
        Catch ex As Exception
            Logger.Warn(ex, "防火墙检查失败，已跳过")
        End Try
    End Sub

    Private Function GetTerracottaApps() As List(Of String)
        Dim Result As New List(Of String)
        Dim TcFolders As String() = {Paths.AppDataThenName & "Terracotta\", Paths.Base & "Terracotta\"}
        For Each Folder As String In TcFolders
            AddIfExists(Result, Folder & "terracotta.exe")

            Try
                If Directory.Exists(Folder) Then
                    For Each File As String In Directory.GetFiles(Folder, "easytier*.exe")
                        AddIfExists(Result, File)
                    Next
                End If
            Catch ex As Exception
                Logger.Warn(ex, $"枚举 EasyTier 程序失败：{Folder}")
            End Try
        Next

        Try
            Dim JavaPath As String = McLaunchJavaSelected?.JavaExePath
            AddIfExists(Result, JavaPath)
        Catch
        End Try
        Return Result
    End Function

    Private Sub AddIfExists(List As List(Of String), Path As String)
        If String.IsNullOrWhiteSpace(Path) Then Return
        If Not File.Exists(Path) Then Return
        AddDistinct(List, Path)
    End Sub

    Private Sub AddDistinct(List As List(Of String), Path As String)
        If String.IsNullOrWhiteSpace(Path) Then Return
        For Each Existing As String In List
            If String.Equals(Existing, Path, StringComparison.OrdinalIgnoreCase) Then Return
        Next
        List.Add(Path)
    End Sub

    Private Function IsAllowedByFirewall(AppPath As String) As Boolean
        Try
            Dim Policy As Object = CreateObject("HNetCfg.FwPolicy2")
            For Each Rule As Object In Policy.Rules
                Try
                    If CBool(Rule.Enabled) = False Then Continue For

                    If CInt(Rule.Direction) <> 1 Then Continue For
                    If CInt(Rule.Action) <> 1 Then Continue For
                    Dim Name As String = If(Rule.ApplicationName, "")
                    If Name = "" Then Continue For
                    If Not String.Equals(Name, AppPath, StringComparison.OrdinalIgnoreCase) Then Continue For

                    Dim Profiles As Integer = CInt(Rule.Profiles)
                    If Profiles = Integer.MaxValue OrElse (Profiles And 2) <> 0 Then Return True
                Catch
                End Try
            Next
        Catch ex As Exception
            Logger.Warn(ex, "读取防火墙规则失败，跳过该项检测")
            Return True
        End Try
        Return False
    End Function

    Private Function WindowsVersion() As String
        Dim V As System.Version = Environment.OSVersion.Version
        Dim IsServer As Boolean = False
        Try
            Dim Key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion")
            If Key IsNot Nothing Then
                Try
                    Dim Product As String = CStr(Key.GetValue("ProductName", ""))
                    IsServer = Product.Contains("Server")
                Finally
                    Key.Dispose()
                End Try
            End If
        Catch
        End Try
        If IsServer Then Return "Windows Server（" & V.Major & "." & V.Minor & "." & V.Build & "）"
        If V.Major < 10 Then
            If V.Major = 6 AndAlso V.Minor = 2 Then Return "Windows 8"
            If V.Major = 6 AndAlso V.Minor = 3 Then Return "Windows 8.1"
            Return "Windows 7"
        End If
        If V.Build >= 22000 Then Return "Windows 11"
        Return "Windows 10"
    End Function

    Private Function BuildSteps(VersionName As String) As String
        Select Case VersionName
            Case "Windows 10", "Windows 11"
                Return "1. 按 Win + S 搜索「Windows 安全中心」，打开「防火墙和网络保护」；" & vbCrLf &
                       "2. 点击「允许应用通过防火墙」（需要管理员权限）；" & vbCrLf &
                       "3. 点击「更改设置」→「允许其他应用…」→「浏览」，把上面的程序逐个添加进来；" & vbCrLf &
                       "4. 勾选它们的「专用」列，务必取消「公用」列的勾选；" & vbCrLf &
                       "5. 点击「确定」保存。"
            Case "Windows 8", "Windows 8.1"
                Return "1. 打开「控制面板」→「系统和安全」→「Windows 防火墙」；" & vbCrLf &
                       "2. 点击「允许应用或功能通过 Windows 防火墙」；" & vbCrLf &
                       "3. 点击「更改设置」→「允许其他应用…」→「浏览」，把上面的程序逐个添加进来；" & vbCrLf &
                       "4. 只勾选「专用」列，取消「公用」列；" & vbCrLf &
                       "5. 点击「确定」保存。"
            Case "Windows 7"
                Return "1. 打开「控制面板」→「系统和安全」→「Windows 防火墙」；" & vbCrLf &
                       "2. 点击左侧「允许程序或功能通过 Windows 防火墙」；" & vbCrLf &
                       "3. 点击「更改设置」→「允许其他程序…」→「浏览」，把上面的程序逐个添加进来；" & vbCrLf &
                       "4. 只勾选「家庭/工作(专用)」列，取消「公用」列；" & vbCrLf &
                       "5. 点击「确定」保存。"
            Case Else
                Return "1. 打开「服务器管理器」→「工具」→「高级安全 Windows 防火墙」（或直接运行 wf.msc）；" & vbCrLf &
                       "2. 左侧选择「入站规则」→「新建规则…」→「程序」；" & vbCrLf &
                       "3. 「浏览」选择上面的程序，选择「允许连接」；" & vbCrLf &
                       "4. 配置文件只勾选「专用」→「完成」；对每个程序重复一次。"
        End Select
    End Function

    Private Sub ShowFirewallGuide(Missing As List(Of String))
        Dim Names As New List(Of String)
        For Each App As String In Missing
            Names.Add("· " & IO.Path.GetFileName(App) & "（" & App & "）")
        Next
        Dim Content As String =
            "为了让朋友能连进你的房间，陶瓦联机需要放行以下程序通过 Windows 防火墙（仅入站）：" & vbCrLf &
            vbCrLf & String.Join(vbCrLf, Names) & vbCrLf & vbCrLf &
            "点「一键放行」会做这些事：" & vbCrLf &
            "· 只为上面列出的程序添加「专用网络」的入站允许规则；" & vbCrLf &
            "· 不开放任何端口，不会改动「公用网络」规则，也不会动其它程序；" & vbCrLf &
            "· 规则存在系统防火墙中，之后可在「允许应用通过防火墙」里随时取消。" & vbCrLf &
            vbCrLf &
            "介意修改系统防火墙设置的话请不要点「一键放行」，点「打开防火墙设置」按下面的步骤自己添加即可：" & vbCrLf &
            "（当前系统：" & WindowsVersion() & "）" & vbCrLf &
            BuildSteps(WindowsVersion())
        Dim Choice As Integer = MyMsgBox(Content, "陶瓦联机防火墙放行", "一键放行（推荐）", "打开防火墙设置", "先跳过")
        If Choice = 1 Then
            If TryAddRules(Missing) Then
                Hint("已自动放行陶瓦联机所需的程序（仅专用网络），之后不再提示。", HintType.Green)
            Else
                Hint("自动放行失败（可能需要管理员权限），已改为打开防火墙设置，请手动添加。", HintType.Red)
                OpenFirewallAllowPage()
            End If
        ElseIf Choice = 2 Then
            If Not OpenFirewallAllowPage() Then
                Hint("无法自动打开防火墙设置，请按上面的步骤手动操作。", HintType.Red)
            End If
        End If
    End Sub

    Private Function TryAddRules(Apps As List(Of String)) As Boolean
        Dim AllOk As Boolean = True
        For Each App As String In Apps
            Dim RuleName As String = "PCL PE - 陶瓦联机 - " & IO.Path.GetFileName(App)
            Dim Args As String = $"advfirewall firewall add rule name=""{RuleName}"" dir=in action=allow program=""{App}"" enable=yes profile=private"
            If Not RunNetsh(Args) Then AllOk = False
        Next

        Dim StillMissing As Boolean = False
        For Each App As String In Apps
            If Not IsAllowedByFirewall(App) Then StillMissing = True
        Next
        Return AllOk AndAlso Not StillMissing
    End Function

    Private Function RunNetsh(Arguments As String) As Boolean
        Try
            Dim StartInfo As New ProcessStartInfo("netsh.exe", Arguments) With {
                .UseShellExecute = False,
                .CreateNoWindow = True,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .StandardOutputEncoding = Encoding.UTF8
            }
            Using P As System.Diagnostics.Process = System.Diagnostics.Process.Start(StartInfo)
                Dim Output As String = P.StandardOutput.ReadToEnd()
                P.WaitForExit(10000)
                Logger.Info($"netsh {Arguments} → 退出码 {P.ExitCode}：{Output.Trim()}")
                Return P.ExitCode = 0
            End Using
        Catch ex As Exception
            Logger.Warn(ex, "执行 netsh 失败")
            Return False
        End Try
    End Function

    Private Function OpenFirewallAllowPage() As Boolean
        Dim V As System.Version = Environment.OSVersion.Version
        Try
            If V.Major >= 10 AndAlso V.Build >= 16299 Then

                StartProcess("explorer.exe", "shell:::{4026492F-2F69-46B8-B9BF-5654FC07E423} -Microsoft.WindowsFirewall\pageConfigureApps")
                Logger.Info("已打开「Windows Defender 防火墙 - 允许的应用」")
                Return True
            End If

            StartProcess("control.exe", "/name Microsoft.WindowsFirewall")
            Logger.Info("已打开经典 Windows 防火墙面板")
            Return True
        Catch ex As Exception
            Logger.Warn(ex, "打开防火墙设置失败，尝试备用方式")
            Try
                System.Diagnostics.Process.Start(New ProcessStartInfo("ms-settings:windowsdefender-firewall") With {.UseShellExecute = True})
                Return True
            Catch ex2 As Exception
                Logger.Warn(ex2, "备用方式也失败")
                Return False
            End Try
        End Try
    End Function

End Module
