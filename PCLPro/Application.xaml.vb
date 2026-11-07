Imports System.Reflection
Imports System.Windows.Threading

Public Class Application

    Private Shared Sub EnsureExecutableName()
        Try
            Dim SelfPath As String = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName
            Dim Expected As String = Assembly.GetEntryAssembly().GetName().Name & ".exe"
            If String.Equals(System.IO.Path.GetFileName(SelfPath), Expected, StringComparison.OrdinalIgnoreCase) Then Return
            Dim Target As String = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(SelfPath), Expected)
            If System.IO.File.Exists(Target) Then

                Try
                    If Reflection.AssemblyName.GetAssemblyName(Target).Version >= Assembly.GetEntryAssembly().GetName().Version Then
                        SyncExecutableConfig(SelfPath, Target)
                        System.Diagnostics.Process.Start(Target)
                        Environment.Exit(ProcessReturnValues.TaskDone)
                    End If
                Catch

                End Try
                Return
            End If
            System.IO.File.Copy(SelfPath, Target)
            SyncExecutableConfig(SelfPath, Target)
            System.Diagnostics.Process.Start(Target)
            Environment.Exit(ProcessReturnValues.TaskDone)
        Catch

        End Try
    End Sub

    Private Shared Sub SyncExecutableConfig(SelfPath As String, TargetPath As String)
        Try
            Dim SourceConfig As String = SelfPath & ".config"
            Dim TargetConfig As String = TargetPath & ".config"
            If System.IO.File.Exists(SourceConfig) AndAlso Not System.IO.File.Exists(TargetConfig) Then
                System.IO.File.Copy(SourceConfig, TargetConfig)
            End If
        Catch

        End Try
    End Sub

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        Try

            MeloongCore.Main.Init("PCL")
            MeloongCore.Wpf.Main.Init()

            If e.Args.Length = 0 Then EnsureExecutableName()
            If Not (e.Args.Contains("--update") OrElse e.Args.Contains("--gpu") OrElse e.Args.Contains("--memory")) Then
                WaitingMutex(e)
                Logger.Instance = New PclLogger With {.logFolder = Paths.BaseThenName, .MinLevel = If(ModeDebug, LogLevel.Trace, LogLevel.Info)}
            End If

            Thread.CurrentThread.Priority = ThreadPriority.Highest

            If BuildType = BuildTypes.Debug Then
                Try
                    ModDevelop.Start()
                Catch ex As Exception
                    Logger.Error(ex, "开发者模式测试出错", LogBehavior.Alert)
                End Try
            End If

            Try
                If CInt(RegistryUtils.TryRead("HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full", "Release", 999999)) < 528040 Then OldEnvironmentAssert()
            Catch ex As Exception
                Logger.Warn(ex, "检查 .NET Framework 版本失败")
            End Try

            Try
                Dim _unused As New FormattedText("", Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Fonts.SystemTypefaces.First, 96, New MyColor, DPI)
            Catch ex As UriFormatException
                Environment.SetEnvironmentVariable("windir", Environment.GetEnvironmentVariable("SystemRoot"), EnvironmentVariableTarget.User)
                Dim _unused As New FormattedText("", Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Fonts.SystemTypefaces.First, 96, New MyColor, DPI)
            End Try

            If e.Args.Length > 0 Then
                If e.Args(0) = "--update" Then

                    UpdateReplaceFromEnvironment()
                    Environment.Exit(ProcessReturnValues.TaskDone)
                ElseIf e.Args(0) = "--gpu" Then

                    Try
                        SetGPUPreference(e.Args(1).Trim(""""))
                        Environment.Exit(ProcessReturnValues.TaskDone)
                    Catch ex As Exception
                        Environment.Exit(ProcessReturnValues.Fail)
                    End Try
                ElseIf e.Args(0) = "--memory" Then

                    Dim Ram = My.Computer.Info.AvailablePhysicalMemory
                    Try
                        PageOtherTest.MemoryOptimizeInternal(False)
                    Catch ex As Exception
                        MsgBox(ex.Message, MsgBoxStyle.Critical, "内存优化失败")
                        Environment.Exit(-1)
                    End Try
                    If My.Computer.Info.AvailablePhysicalMemory < Ram Then
                        Environment.Exit(0)
                    Else
                        Environment.Exit((My.Computer.Info.AvailablePhysicalMemory - Ram) / 1024)
                    End If
                End If
            End If

            Try
                DirectoryUtils.Create(Paths.Base & "PCL\Pictures\")
                DirectoryUtils.Create(Paths.Base & "PCL\Musics\")
                CheckPermissionWithException(Paths.Base & "PCL\")
            Catch ex As Exception
                MsgBox($"PCL 没有对当前文件夹的权限（{Paths.Base}PCL\），请尝试：" & vbCrLf &
                  "1. 将 PCL 移动到其他文件夹" & If(PathUtils.GetDiskName(Paths.Base) = "C"c, "，例如 C 盘和桌面以外的其他位置。", "。") & vbCrLf &
                  "2. 删除当前目录中的 PCL 文件夹，然后再试。" & vbCrLf &
                  "3. 右键 PCL 选择属性，打开 兼容性 中的 以管理员身份运行此程序。",
                MsgBoxStyle.Critical, "运行环境错误")
                Environment.[Exit](ProcessReturnValues.Cancel)
            End Try
RetryCacheCheck:
            Try
                DirectoryUtils.Create(PathTemp)
                CheckPermissionWithException(PathTemp)
            Catch ex As Exception
                If PathUtils.IsSame(PathTemp, Path.GetTempPath() & "PCL\") Then
                    MyMsgBox("PCL 无法访问缓存文件夹，可能导致程序出错或无法正常使用！" & vbCrLf & vbCrLf & "错误原因：" & ex.GetDisplay(True), "缓存文件夹不可用")
                Else
                    MyMsgBox("手动设置的缓存文件夹不可用，PCL 将使用默认缓存文件夹。" & vbCrLf & vbCrLf & "错误原因：" & ex.GetDisplay(True), "缓存文件夹不可用")
                    Settings.Set("SystemSystemCache", "")
                    PathTemp = Path.GetTempPath() & "PCL\"
                    GoTo RetryCacheCheck
                End If
            End Try
            DirectoryUtils.Create(PathTemp & "Cache\")

            Try
                Dim UpdateDir As String = PathTemp & "Update\"
                If Directory.Exists(UpdateDir) Then Directory.Delete(UpdateDir, True)
            Catch
            End Try
            DirectoryUtils.Create(Paths.AppDataThenName)

            ToolTipService.InitialShowDelayProperty.OverrideMetadata(GetType(DependencyObject), New FrameworkPropertyMetadata(100))

            ServicePointManager.Expect100Continue = False
            ServicePointManager.DefaultConnectionLimit = 10000
            ServicePointManager.UseNagleAlgorithm = False
            ServicePointManager.EnableDnsRoundRobin = True
            ServicePointManager.ReusePort = True
            If Environment.OSVersion.Version < New Version(6, 2) Then
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 Or SecurityProtocolType.Tls Or SecurityProtocolType.Tls11 Or SecurityProtocolType.Tls12
            End If

            Logger.Info($"程序版本：{VersionDisplay} ({VersionCode}{If(CommitHash = "", "", $"，#{CommitHash}")})")
            If BuildType = BuildTypes.Snapshot Then
                Logger.Info($"识别码：{Identify}{If(ThemeCheckOne(9), "，已解锁反馈主题", "，未解锁反馈主题")}")
            Else
                Logger.Info($"识别码：{Identify}")
            End If
            Logger.Info($"程序路径：{PathExe}")
            Logger.Info($"系统编码：{Encoding.Default.HeaderName} ({Encoding.Default.CodePage}, GBK={IsGBKEncoding})")
            Logger.Info($"管理员权限：{WindowsUtils.HasAdminRole()}")

            If Paths.Base.Contains(Path.GetTempPath()) OrElse Paths.Base.Contains("AppData\Local\Temp\") Then
                MyMsgBox("请将 PCL 从压缩包中解压后再使用！" & vbCrLf & "如果不会解压，可以在网上寻找教程。", "需要解压！", "我知道了", IsWarn:=True, ForceWait:=True)
                FormMain.EndProgramForce(ProcessReturnValues.Cancel)
            End If
            If Not Environment.Is64BitOperatingSystem Then
                MyMsgBox("PCL 和新版 Minecraft 均不再支持 32 位系统，请重装为 64 位系统后再进行游戏！", "环境警告", "我知道了", IsWarn:=True, ForceWait:=True)
                FormMain.EndProgramForce(ProcessReturnValues.Cancel)
            End If

            Logger.Info($"第一阶段加载用时：{GetTimeMs() - ApplicationStartTick} ms")
            ApplicationStartTick = GetTimeMs()
            AniControlEnabled += 1
        Catch ex As Exception
            Dim FilePath As String = Nothing
            Try
                FilePath = PathExe
            Catch
            End Try
            MsgBox(ex.GetDisplay(True) & vbCrLf & "PCL 所在路径：" & If(String.IsNullOrEmpty(FilePath), "获取失败", FilePath), MsgBoxStyle.Critical, "PCL 初始化错误")
            FormMain.EndProgramForce(ProcessReturnValues.Exception)
        End Try
    End Sub

    Private Shared Sub WaitingMutex(e As StartupEventArgs)
        If BuildType = BuildTypes.Debug Then Return
        Try
            Dim MutexCreatedNew As Boolean
            ' 互斥量名与产品绑定：改名后与旧版 PCL FE 不再互相判为同一实例，两者可以并存
            PclMutex = New Mutex(True, "PCL Professional Edition_SingletonMutex", MutexCreatedNew)
            If MutexCreatedNew Then Return
            Logger.Warn("已有一个 PCL 实例正在运行")

            If e.Args.Length > 0 AndAlso e.Args(0) = "--wait" Then
                Try
                    If PclMutex.WaitOne(10000) Then Return
                Catch Ignored As AbandonedMutexException
                    Return
                End Try
            End If

            Dim WindowHwnd As IntPtr = FindWindow(Nothing, "PCL PE　")
            If WindowHwnd = IntPtr.Zero Then WindowHwnd = FindPclWindowHwnd()
            If WindowHwnd = IntPtr.Zero Then

                Logger.Warn("未找到已有 PCL 实例的窗口，等待旧进程退出后继续启动")
                Dim Acquired As Boolean = False
                Try
                    Acquired = PclMutex.WaitOne(8000)
                Catch Ignored As AbandonedMutexException
                    Acquired = True
                Catch ex As Exception
                    Logger.Warn(ex, "等待单例锁失败")
                End Try
                If Acquired Then Logger.Info("旧实例已退出，本次按正常启动继续")
                Return
            End If

            ShowWindow(WindowHwnd, SW_SHOW)
            ShowWindowToTop(WindowHwnd)

            Beep()
            Environment.[Exit](ProcessReturnValues.Cancel)
        Catch ex As Exception
            Logger.Warn(ex, "单例检查失败")
        End Try
    End Sub

    Private Shared Function FindPclWindowHwnd() As IntPtr
        Try
            For Each Proc As Diagnostics.Process In Diagnostics.Process.GetProcesses()
                Try
                    If Proc.MainWindowHandle = IntPtr.Zero Then Continue For
                    Dim Title As String = If(Proc.MainWindowTitle, "").Trim()
                    If Title.Contains("PCL PE") Then Return Proc.MainWindowHandle
                Catch
                End Try
            Next
        Catch ex As Exception
            Logger.Warn(ex, "按窗口标题查找已有实例失败")
        End Try
        Try
            For Each Proc As Diagnostics.Process In Diagnostics.Process.GetProcesses()
                Try
                    If Proc.MainWindowHandle = IntPtr.Zero Then Continue For
                    If Proc.ProcessName.StartsWith("PCL Professional Edition", StringComparison.OrdinalIgnoreCase) Then Return Proc.MainWindowHandle
                Catch
                End Try
            Next
        Catch ex As Exception
            Logger.Warn(ex, "按进程名查找已有窗口失败")
        End Try
        Return IntPtr.Zero
    End Function
    Private Shared PclMutex As Mutex

    Private Sub Application_SessionEnding(sender As Object, e As SessionEndingCancelEventArgs) Handles Me.SessionEnding
        FrmMain?.EndProgram(False)
    End Sub

    Private Sub Application_DispatcherUnhandledException(sender As Object, e As DispatcherUnhandledExceptionEventArgs) Handles Me.DispatcherUnhandledException
        On Error Resume Next

        If FrmMain?.PageLeft IsNot Nothing AndAlso TypeOf FrmMain.PageLeft Is IDispatcherUnhandledException Then
            CType(FrmMain.PageLeft, IDispatcherUnhandledException).DispatcherUnhandledException(sender, e)
            If e.Handled Then Return
        End If
        If FrmMain?.PageRight IsNot Nothing AndAlso TypeOf FrmMain.PageRight Is IDispatcherUnhandledException Then
            CType(FrmMain.PageRight, IDispatcherUnhandledException).DispatcherUnhandledException(sender, e)
            If e.Handled Then Return
        End If

        e.Handled = True
        If IsProgramEnding Then Return
        FeedbackInfo()
        Dim Detail As String = e.Exception.GetDisplay(True)
        If Detail.Contains("System.Windows.Threading.Dispatcher.Invoke") OrElse Detail.Contains("MS.Internal.AppModel.ITaskbarList.HrInit") OrElse Detail.Contains("未能加载文件或程序集") OrElse
           Detail.Contains(".NET Framework") Then
            OldEnvironmentAssert(e.Exception)
        Else
            Logger.Error(e.Exception, "程序出现未知错误", LogBehavior.AlertThenCrash)
        End If
    End Sub
    Private Shared Sub OldEnvironmentAssert(Optional ex As Exception = Nothing)
        OpenWebsite("https://dotnet.microsoft.com/zh-cn/download/dotnet-framework/thank-you/net48-offline-installer")
        If ex Is Nothing Then
            Logger.Error($"你的 .NET Framework 版本过低或损坏，请下载并重新安装 .NET Framework 4.8！{vbCrLf}若安装失败，可以先卸载高版本的 .NET Framework 然后再试。", LogBehavior.AlertThenCrash)
        Else
            Logger.Error(ex, $"你的 .NET Framework 版本过低或损坏，请下载并重新安装 .NET Framework 4.8！{vbCrLf}若安装失败，可以先卸载高版本的 .NET Framework 然后再试。", LogBehavior.AlertThenCrash)
        End If
    End Sub

    Private Sub New()
        Static Prefixes As String() = {"NAudio", "Newtonsoft.Json", "Ookii.Dialogs.Wpf", "Imazen.WebP", "CacheCow.Common", "CacheCow.Client.FileStore", "CacheCow.Client", "ThrottleDebounce", "System.Net.Http.Formatting", "Microsoft.Win32.Registry", "PCLCS", "MeloongCore.Wpf", "MeloongCore"}
        Static LoadedAssemblies As New ConcurrentDictionary(Of String, Lazy(Of Assembly))(StringComparer.Ordinal)
        AddHandler AppDomain.CurrentDomain.AssemblyResolve,
        Function(sender As Object, Args As ResolveEventArgs) As Assembly
            Dim Prefix As String = Prefixes.FirstOrDefault(Function(p) Args.Name.StartsWith(p, StringComparison.Ordinal))
            If Prefix Is Nothing Then Return Nothing
            Dim LazyAssembly = LoadedAssemblies.GetOrAdd(Prefix, Function(p) New Lazy(Of Assembly)(
            Function()
                Dim LoadedAssembly = Assembly.Load(DirectCast(My.Resources.ResourceManager.GetObject(p), Byte()))
                If p = "Imazen.WebP" Then ExtractLibwebp()
                Return LoadedAssembly
            End Function, LazyThreadSafetyMode.ExecutionAndPublication))
            Return LazyAssembly.Value
        End Function
    End Sub
    Private Shared Sub ExtractLibwebp()
        SetDllDirectory(PathUtils.RemoveSlashSuffix(PathPure.Value))
        Try
            ExtractResources(PathPure.Value & "libwebp.dll", "libwebp64")
        Catch ex As Exception
            Logger.Warn(ex, "写入 libwebp.dll 失败")
        End Try
    End Sub
    Private Declare Function SetDllDirectory Lib "kernel32" Alias "SetDllDirectoryA" (lpPathName As String) As Boolean

    Private Sub MyIconButton_Click(sender As Object, e As EventArgs)
        Select Case Settings.Get(Of McLoginType)("LoginType")
            Case McLoginType.Ms

                Dim MsJson As JObject = Settings.Get(Of String)("LoginMsJson").DeserializeJson()
                MsJson.Remove(sender.Tag)
                Settings.Set("LoginMsJson", MsJson.ToString(Newtonsoft.Json.Formatting.None))
                If FrmLoginMs.ComboAccounts.SelectedItem Is sender.Parent Then FrmLoginMs.ComboAccounts.SelectedIndex = 0
                FrmLoginMs.ComboAccounts.Items.Remove(sender.Parent)
            Case McLoginType.Legacy

                Dim Names As New List(Of String)
                Names.AddRange(Settings.Get(Of String)("LoginLegacyName").ToString.Split("¨"))
                Names.Remove(sender.Tag)
                Settings.Set("LoginLegacyName", Names.Join("¨"c))
                FrmLoginLegacy.ComboName.ItemsSource = Names
                FrmLoginLegacy.ComboName.Text = If(Names.Any, Names(0), "")
            Case Else

                Dim Token As String = Settings.Get(Of McLoginType)("LoginType").ToString()
                Dim Dict As New Dictionary(Of String, String)
                Dim Names As New List(Of String)
                Dim Passs As New List(Of String)
                If Not Settings.Get(Of String)("Login" & Token & "Email") = "" Then Names.AddRange(Settings.Get(Of String)("Login" & Token & "Email").Split("¨"))
                If Not Settings.Get(Of String)("Login" & Token & "Pass") = "" Then Passs.AddRange(Settings.Get(Of String)("Login" & Token & "Pass").Split("¨"))
                For i = 0 To Names.Count - 1
                    Dict.Add(Names(i), Passs(i))
                Next
                Dict.Remove(sender.Tag)
                Settings.Set("Login" & Token & "Email", Dict.Keys.Join("¨"c))
                Settings.Set("Login" & Token & "Pass", Dict.Values.Join("¨"c))
                Select Case Token
                    Case "Nide"
                        FrmLoginNide.ComboName.ItemsSource = Dict.Keys
                        FrmLoginNide.ComboName.Text = If(Dict.Keys.Any, Dict.Keys(0), "")
                        FrmLoginNide.TextPass.Password = If(Dict.Values.Any, Dict.Values(0), "")
                    Case "Auth"
                        FrmLoginAuth.ComboName.ItemsSource = Dict.Keys
                        FrmLoginAuth.ComboName.Text = If(Dict.Keys.Any, Dict.Keys(0), "")
                        FrmLoginAuth.TextPass.Password = If(Dict.Values.Any, Dict.Values(0), "")
                End Select
        End Select
    End Sub

End Class
