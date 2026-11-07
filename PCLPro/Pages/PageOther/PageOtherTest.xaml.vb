Public Class PageOtherTest

#Region "趣味内容"

    Private Shared ReadOnly Caves As String() = {
        "洞里传来回声：别睡，再挖一格就到钻石了。",
        "洞里传来回声：你的显卡风扇正在替你喊加油。",
        "洞里传来回声：苦力怕说它只是想抱抱你。",
        "洞里传来回声：存档已经三天没备份了哦。",
        "洞里传来回声：明明是来挖矿的，怎么变成盖火柴盒了。",
        "洞里传来回声：那个箱子你明明已经翻过两遍了。",
        "洞里传来回声：水桶才是这个游戏最伟大的发明。",
        "洞里传来回声：天黑了，请务必记得带床出门。",
        "洞里传来回声：岩浆桶放下去了，你确定要收回来吗？",
        "洞里传来回声：这座山，它本来可以更圆的。",
        "洞里传来回声：你脚下的那一格，下面是虚空。",
        "洞里传来回声：村民又涨价了，这次真的不怪你。",
        "洞里传来回声：末影人只是想帮你搬砖而已。",
        "洞里传来回声：退出前记得按一下 F3 + D。",
        "洞里传来回声：今天适合开新档，不适合写模组。",
        "洞里传来回声：那个被你命名为「临时」的箱子，已经是第五个了。",
        "洞里传来回声：你挖的矿洞，比我家还大。",
        "洞里传来回声：船在陆地上也能开，别问为什么。",
        "洞里传来回声：既然都走到这了，不如再挖一格。",
        "洞里传来回声：别急，加载条马上就到 99% 了。",
        "洞里传来回声：这一格沙子，它曾经也是一座山。",
        "洞里传来回声：猫会赶走幻翼，猫是这个游戏的神。",
        "洞里传来回声：你的火把插得比星星还密。",
        "洞里传来回声：掉进岩浆的东西，就让它留在那里吧。"
    }

    Private Shared ReadOnly Hints As String() = {
        "在 更多 → 帮助 里按关键词搜索，可以找到绝大部分问题的答案。",
        "设置 → 个性化 里可以换主题、换背景图，还能拖动滑块自定义主色调。",
        "下载页可以一键安装 Fabric、Forge、NeoForge、Quilt、OptiFine 等加载器。",
        "版本设置 → 内存设置 可以手动指定分配给游戏的内存，一般建议不超过物理内存的一半。",
        "把整合包文件直接拖到 PCL 窗口上，就能导入安装。",
        "在 更多 → 百宝箱 里可以一键做内存优化和垃圾清理。",
        "右键主题按钮往往有惊喜，玄素黑以外的主题都值得一试。",
        "启动游戏时按住 Shift 可以临时调整启动参数。",
        "下载速度慢时，可以在 设置 → 其他 里更换下载源。",
        "版本列表右键可以打开游戏目录，直接管理 mods 与存档。",
        "窗口右下角的提示条点击后可以展开，能看到更详细的信息。",
        "设置 → 启动 里可以勾选「启动前自动检查更新」。",
        "在 联机 页可以用邀请码加入朋友的房间，不需要公网 IP。",
        "Mod 管理与资源下载支持 Modrinth 源，中文搜索更友好。",
        "PCL 支持离线登录，没有正版账号也能正常进单人世界。",
        "把游戏窗口标题留空，就会自动跟随全局设置的标题。",
        "日志文件夹里的 Log1.txt 是最新的日志，出问题时把它发给别人最好排查。",
        "导出整合包时可以顺手把 mods 和存档一起打包给朋友。"
    }

    Private Shared ReadOnly PresetHints As String() = {
        "试试换个主页预设，也许会有新气象？",
        "预设可以自己写，具体标记用法可以在帮助里搜索「替换标记」。",
        "这个预设看起来不错，就是有点费眼睛。",
        "听说多换几次预设，游戏启动速度会变快（并没有）。",
        "预设里的 {cave} 会随机变成一句回声洞的话哦。",
        "预设里的 {hint} 会变成一条随机小贴士。"
    }
    Private Shared ReadOnly Randomizer As New Random()
    Private Shared Function Pick(Items As String()) As String
        SyncLock Randomizer
            Return Items(Randomizer.Next(0, Items.Length))
        End SyncLock
    End Function
    Public Shared Function GetRandomCave() As String
        Return Pick(Caves)
    End Function
    Public Shared Function GetRandomHint() As String
        Return Pick(Hints)
    End Function
    Public Shared Function GetRandomPresetHint() As String
        Return Pick(PresetHints)
    End Function

    Private Shared Function GetJrrpValue() As Integer
        Dim Key As String = DateTime.Now.ToString("yyyy/MM/dd") & "|" & Identify
        Dim Hash As Long = 5381
        For Each Letter In Key
            Hash = ((Hash << 5) + Hash + AscW(Letter)) And &H7FFFFFFFL
        Next
        Return CInt(Hash Mod 101)
    End Function
    Public Shared Sub Jrrp()
        Dim Value As Integer = GetJrrpValue()
        Dim Comment As String
        Select Case Value
            Case 0 To 5
                Comment = "今天……建议先把存档备份一遍再启动游戏。"
            Case 6 To 25
                Comment = "运气偏低，下矿请带好水桶和床。"
            Case 26 To 50
                Comment = "中规中矩，适合照常挖矿盖房子。"
            Case 51 To 75
                Comment = "状态不错，可以去碰碰运气了。"
            Case 76 To 94
                Comment = "手气很红，今天适合开新档。"
            Case Else
                Comment = "天选之子！快去开坑，今天不出货都难。"
        End Select
        RunInUi(Sub()
                    MyMsgBox($"你今天的人品值是 {Value} / 100。{vbCrLf}{Comment}{vbCrLf}{vbCrLf}（每天刷新一次，同一天结果不变）", "今日人品")
                End Sub)
    End Sub

#End Region

#Region "内存优化与垃圾清理"

    <System.Runtime.InteropServices.DllImport("psapi.dll")>
    Private Shared Function EmptyWorkingSet(hProcess As IntPtr) As Boolean
    End Function
    <System.Runtime.InteropServices.DllImport("kernel32.dll")>
    Private Shared Function OpenProcess(DesiredAccess As Integer, InheritHandle As Boolean, ProcessId As Integer) As IntPtr
    End Function
    <System.Runtime.InteropServices.DllImport("kernel32.dll")>
    Private Shared Function CloseHandle(hObject As IntPtr) As Boolean
    End Function
    Private Const ProcessQueryInformation As Integer = &H400
    Private Const ProcessSetQuota As Integer = &H100

    Private Const TokenAdjustPrivileges As Integer = &H20
    Private Const TokenQuery As Integer = &H8
    Private Const SePrivilegeEnabled As Integer = &H2
    Private Const SystemMemoryListInformation As Integer = 80
    Private Const MemoryPurgeStandbyList As Integer = 4

    <Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Sequential)>
    Private Structure TcLuid
        Public LowPart As Integer
        Public HighPart As Integer
    End Structure
    <Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Sequential)>
    Private Structure TcTokenPrivileges
        Public PrivilegeCount As Integer
        Public Luid As TcLuid
        Public Attributes As Integer
    End Structure

    <System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError:=True, CharSet:=Runtime.InteropServices.CharSet.Unicode)>
    Private Shared Function OpenProcessToken(ProcessHandle As IntPtr, DesiredAccess As Integer, ByRef TokenHandle As IntPtr) As Boolean
    End Function
    <System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError:=True, CharSet:=Runtime.InteropServices.CharSet.Unicode)>
    Private Shared Function LookupPrivilegeValue(SystemName As String, Name As String, ByRef Luid As TcLuid) As Boolean
    End Function
    <System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError:=True)>
    Private Shared Function AdjustTokenPrivileges(TokenHandle As IntPtr, DisableAllPrivileges As Boolean,
                                                  ByRef NewState As TcTokenPrivileges, BufferLength As Integer,
                                                  PreviousState As IntPtr, ReturnLength As IntPtr) As Boolean
    End Function
    <System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function SetSystemFileCacheSize(MinimumFileCacheSize As IntPtr, MaximumFileCacheSize As IntPtr, Flags As Integer) As Boolean
    End Function
    <System.Runtime.InteropServices.DllImport("ntdll.dll")>
    Private Shared Function NtSetSystemInformation(InfoClass As Integer, Info As IntPtr, Length As Integer) As Integer
    End Function

    Private Shared Function EnablePrivilege(Name As String) As Boolean
        Dim Token As IntPtr = IntPtr.Zero
        Try
            If Not OpenProcessToken(Process.GetCurrentProcess().Handle, TokenAdjustPrivileges Or TokenQuery, Token) Then Return False
            Dim Luid As New TcLuid
            If Not LookupPrivilegeValue(Nothing, Name, Luid) Then Return False
            Dim State As New TcTokenPrivileges With {.PrivilegeCount = 1, .Luid = Luid, .Attributes = SePrivilegeEnabled}
            Return AdjustTokenPrivileges(Token, False, State, 0, IntPtr.Zero, IntPtr.Zero)
        Catch
            Return False
        Finally
            If Token <> IntPtr.Zero Then CloseHandle(Token)
        End Try
    End Function

    Private Shared Sub PurgeStandbyList()
        Try
            Dim Buffer As IntPtr = Runtime.InteropServices.Marshal.AllocHGlobal(4)
            Try
                Runtime.InteropServices.Marshal.WriteInt32(Buffer, MemoryPurgeStandbyList)
                NtSetSystemInformation(SystemMemoryListInformation, Buffer, 4)
            Finally
                Runtime.InteropServices.Marshal.FreeHGlobal(Buffer)
            End Try
        Catch
        End Try
    End Sub

    Public Shared Sub MemoryOptimize(ShowHint As Boolean)
        Dim Before As Long = My.Computer.Info.AvailablePhysicalMemory
        Dim FreedKb As Long = 0
        Dim Success As Boolean = False
        If WindowsUtils.HasAdminRole() Then
            Try
                MemoryOptimizeInternal(False)
                Success = True
            Catch ex As Exception
                Logger.Warn(ex, "内存优化失败")
            End Try
        Else
            Try

                FreedKb = Math.Max(0, RunAsAdmin("--memory"))
                Success = True
            Catch ex As Exception

                Logger.Info($"内存优化未执行：{ex.Message}")
            End Try
        End If
        If Not Success Then
            If ShowHint Then Hint("已取消内存优化（需要管理员权限才能清理系统内存）。", HintType.Blue)
            Return
        End If
        Dim Freed As Long = If(FreedKb > 0, FreedKb * 1024L, Math.Max(0L, CLng(My.Computer.Info.AvailablePhysicalMemory) - Before))
        Logger.Info($"内存优化：清理了 {Math.Round(Freed / 1048576.0, 1)} MB 物理内存")
        If ShowHint Then
            Hint($"内存优化完成，共释放 {Math.Round(Freed / 1048576.0, 1)} MB 内存", HintType.Green)
        End If
        LastMemoryText = $"PCL 当前占用内存：{Math.Round(Process.GetCurrentProcess().WorkingSet64 / 1048576.0, 1)} MB"
    End Sub

    Public Shared Sub MemoryOptimizeInternal(ShowHint As Boolean)
        Dim SelfId As Integer = Process.GetCurrentProcess().Id

        EnablePrivilege("SeDebugPrivilege")
        EnablePrivilege("SeProfileSingleProcessPrivilege")
        EnablePrivilege("SeIncreaseQuotaPrivilege")

        PurgeStandbyList()
        Try

            SetSystemFileCacheSize(New IntPtr(-1), New IntPtr(-1), 0)
        Catch
        End Try
        For Each Proc As Process In Process.GetProcesses()
            Try
                If Proc.Id <> SelfId Then
                    Dim Handle As IntPtr = OpenProcess(ProcessQueryInformation Or ProcessSetQuota, False, Proc.Id)
                    If Handle <> IntPtr.Zero Then
                        Try
                            EmptyWorkingSet(Handle)
                        Finally
                            CloseHandle(Handle)
                        End Try
                    End If
                End If
            Catch ex As Exception

            Finally
                Proc.Dispose()
            End Try
        Next
        Try
            GC.Collect(2, GCCollectionMode.Forced, True)
            GC.WaitForPendingFinalizers()
            GC.Collect(2, GCCollectionMode.Forced, True)
            EmptyWorkingSet(Process.GetCurrentProcess().Handle)
        Catch ex As Exception
            Logger.Trace(ex, "清空工作集失败")
        End Try
    End Sub

    Private Shared Function GetFolderSize(Folder As String) As Long
        Dim Total As Long = 0
        Try
            If Not Directory.Exists(Folder) Then Return 0
            For Each File In Directory.EnumerateFiles(Folder, "*", SearchOption.AllDirectories)
                Try
                    Total += New FileInfo(File).Length
                Catch
                End Try
            Next
        Catch
        End Try
        Return Total
    End Function

    Private Shared Function ClearFolder(Folder As String, ByRef Count As Integer) As Long
        Dim Freed As Long = 0
        Try
            If Not Directory.Exists(Folder) Then Return 0
            For Each File In Directory.EnumerateFiles(Folder, "*", SearchOption.AllDirectories)
                Try
                    Freed += New FileInfo(File).Length
                    System.IO.File.Delete(File)
                    Count += 1
                Catch
                End Try
            Next
            For Each SubFolder In Directory.EnumerateDirectories(Folder, "*", SearchOption.AllDirectories).Reverse()
                Try
                    Directory.Delete(SubFolder, False)
                Catch
                End Try
            Next
        Catch ex As Exception
            Logger.Warn(ex, $"清理文件夹失败：{Folder}")
        End Try
        Return Freed
    End Function
    Private Shared Function FormatSize(Size As Long) As String
        Select Case Size
            Case Is >= 1073741824
                Return $"{Math.Round(Size / 1073741824.0, 2)} GB"
            Case Is >= 1048576
                Return $"{Math.Round(Size / 1048576.0, 1)} MB"
            Case Else
                Return $"{Math.Round(Size / 1024.0, 1)} KB"
        End Select
    End Function
    Public Shared Sub RubbishClear()
        Dim FreedSize As Long = 0
        Dim FreedCount As Integer = 0

        FreedSize += ClearFolder(PathTemp, FreedCount)

        Try
            Dim PclFolder As String = Paths.Base & "PCL\"
            If Directory.Exists(PclFolder) Then
                For Each File In Directory.GetFiles(PclFolder, "Log*.txt")
                    If Path.GetFileName(File).ToLower() = "log1.txt" Then Continue For
                    Try
                        FreedSize += New FileInfo(File).Length
                        System.IO.File.Delete(File)
                        FreedCount += 1
                    Catch
                    End Try
                Next
                For Each File In Directory.GetFiles(PclFolder, "*.dmp")
                    Try
                        FreedSize += New FileInfo(File).Length
                        System.IO.File.Delete(File)
                        FreedCount += 1
                    Catch
                    End Try
                Next

                Dim UpdateFile As String = PclFolder & "Plain Craft Launcher Pro.exe"
                If System.IO.File.Exists(UpdateFile) Then
                    FreedSize += New FileInfo(UpdateFile).Length
                    System.IO.File.Delete(UpdateFile)
                    FreedCount += 1
                End If
            End If
        Catch ex As Exception
            Logger.Warn(ex, "清理 PCL 目录失败")
        End Try
        Logger.Info($"垃圾清理完成：释放 {FormatSize(FreedSize)}，共 {FreedCount} 个文件")
        If FreedCount = 0 Then
            Hint("没有找到可以清理的垃圾，你的 PCL 很干净！", HintType.Blue)
        Else
            Hint($"垃圾清理完成：释放 {FormatSize(FreedSize)}，共 {FreedCount} 个文件", HintType.Green)
        End If
    End Sub

#End Region

#Region "自定义下载"

    Public Shared Sub StartCustomDownload(Url As String, FileName As String, Optional Folder As String = Nothing)
        If Not (Url.StartsWithF("http://", True) OrElse Url.StartsWithF("https://", True)) Then
            Hint("下载失败：网址必须以 http:// 或 https:// 开头", HintType.Red)
            Return
        End If
        If Folder Is Nothing Then Folder = Paths.Base & "PCL\Custom\"
        RunInNewThread(
        Sub()
            Try
                Directory.CreateDirectory(Folder)
                Dim SafeName As String = FileName
                For Each Bad In Path.GetInvalidFileNameChars()
                    SafeName = SafeName.Replace(Bad, "_"c)
                Next
                If SafeName.Trim() = "" Then SafeName = "未知文件"
                Dim LocalPath As String = Folder & SafeName
                Hint("正在下载文件……", HintType.Blue)
                NetDownloadByClient(Url, LocalPath)
                Hint($"下载完成：{SafeName}（{FormatSize(New FileInfo(LocalPath).Length)}）", HintType.Green)
                OpenExplorer(Folder)
            Catch ex As Exception
                Logger.Error(ex, "自定义下载失败")
                Hint($"下载失败：{ex.Message}", HintType.Red)
            End Try
        End Sub, "百宝箱 自定义下载")
    End Sub

#End Region

#Region "界面"

    Private Shared LastMemoryText As String = ""
    Private IsUiLoad As Boolean = False
    Private Sub PageOtherTest_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If IsUiLoad Then Return
        IsUiLoad = True
        LabTestCave.Text = GetRandomCave()
        LabTestHint.Text = GetRandomHint()
        RefreshStats()
    End Sub

    Private Sub RefreshStats()
        RunInNewThread(
        Sub()
            Dim Count As Integer = Settings.Get(Of Integer)("SystemCount")
            Dim Memory As String = $"PCL 当前占用内存：{Math.Round(Process.GetCurrentProcess().WorkingSet64 / 1048576.0, 1)} MB"
            LastMemoryText = Memory
            Dim Size As Long = GetFolderSize(Paths.Base & "PCL\")
            RunInUi(
            Sub()
                If LabTestStat Is Nothing Then Return
                LabTestStat.Text = $"启动次数：{Count} 次　|　{Memory}　|　数据目录大小：{FormatSize(Size)}"
            End Sub)
        End Sub, "百宝箱 统计")
    End Sub

    Private Sub BtnTestJrrp_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnTestJrrp.Click
        Jrrp()
    End Sub
    Private Sub BtnTestCave_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnTestCave.Click
        LabTestCave.Text = GetRandomCave()
    End Sub
    Private Sub BtnTestHint_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnTestHint.Click
        LabTestHint.Text = GetRandomHint()
    End Sub
    Private Sub BtnTestMemory_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnTestMemory.Click

        Dim Choice As Integer = MyMsgBox(
            "内存优化会清空系统的备用内存与文件缓存，并把各程序的内存换出到页面文件。" & vbCrLf &
            "· 执行后短时间内程序与游戏读盘会变慢，机械硬盘尤其明显；" & vbCrLf &
            "· 这是实验性功能，内存充裕的电脑上可能看不出明显效果；" & vbCrLf &
            "· 需要管理员权限，会弹出 UAC 提示。" & vbCrLf & vbCrLf &
            "建议在启动游戏前执行。确定继续吗？",
            "内存优化（实验性）", "继续优化", "取消", IsWarn:=True)
        If Choice <> 1 Then Return
        RunInThread(
        Sub()
            MemoryOptimize(True)
            RunInUi(AddressOf RefreshStats)
        End Sub)
    End Sub
    Private Sub BtnTestClean_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnTestClean.Click
        RunInThread(
        Sub()
            RubbishClear()
            RunInUi(AddressOf RefreshStats)
        End Sub)
    End Sub
    Private Sub BtnTestOpenData_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnTestOpenData.Click
        OpenExplorer(Paths.Base & "PCL\")
    End Sub
    Private Sub BtnTestOpenLog_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnTestOpenLog.Click
        OpenExplorer(Paths.Base & "PCL\Log1.txt")
    End Sub
    Private Sub BtnTestDownload_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnTestDownload.Click
        Dim Url As String = If(TextTestUrl.Text, "").Trim()
        If Url = "" Then
            Hint("请先填入要下载的网址！", HintType.Red)
            Return
        End If
        StartCustomDownload(Url, PathUtils.GetLastPart(Url))
    End Sub

#End Region

End Class
