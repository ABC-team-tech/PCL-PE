Public Module ModLoader

    Public MustInherit Class LoaderBase
        Implements ILoadingTrigger

        Public ReadOnly Property IsLoader As Boolean = True Implements ILoadingTrigger.IsLoader

        Public Uuid As Integer = GetUuid()

        Public Name As String = "未命名任务 " & Uuid & "#"

        Public ReadOnly LockState As New Object

        Public Parent As LoaderBase = Nothing

        Public ReadOnly Property RealParent As LoaderBase
            Get
                Try
                    RealParent = Parent
                    While RealParent IsNot Nothing AndAlso RealParent.Parent IsNot Nothing
                        RealParent = RealParent.Parent
                    End While
                Catch ex As Exception
                    Logger.Error(ex, $"获取父加载器失败（{Name}）")
                    Return Nothing
                End Try
            End Get
        End Property

        Public Overridable Sub InitParent(Parent As LoaderBase)
            Me.Parent = Parent
        End Sub

        Public Event OnStateChangedThread(Loader As LoaderBase, NewState As LoadState, OldState As LoadState)
        Public HasOnStateChangedThread As Boolean = False

        Public Event OnStateChangedUi(Loader As LoaderBase, NewState As LoadState, OldState As LoadState)

        Public WriteOnly Property OnStateChanged As Action(Of LoaderBase)
            Set(value As Action(Of LoaderBase))
                AddHandler OnStateChangedUi, Sub(Loader As LoaderBase, NewState As LoadState, OldState As LoadState) value(Loader)
            End Set
        End Property

        Public Event PreviewFinish(Loader As LoaderBase)
        Protected Sub RaisePreviewFinish()
            RaiseEvent PreviewFinish(Me)
        End Sub

        Public Property State As LoadState
            Get
                Return _State
            End Get
            Set(value As LoadState)
                If _State = value Then Return
                Dim OldState = _State
                If value = LoadState.Finished AndAlso Settings.Get(Of Boolean)("SystemDebugDelay") Then Thread.Sleep(RandomInteger(100, 2000))
                _State = value
                If value = LoadState.Canceled Then CancellationTokenSource?.Cancel()
                Logger.Log($"加载器 {[GetType].Name} {Name} 状态改变：{value}", If(value = LoadState.Failed, LogLevel.Error, LogLevel.Info))

                RunInUi(
                Sub()
                    Select Case value
                        Case LoadState.Loading
                            LoadingState = MyLoading.MyLoadingState.Run
                        Case LoadState.Failed
                            LoadingState = MyLoading.MyLoadingState.Error
                        Case Else
                            LoadingState = MyLoading.MyLoadingState.Stop
                    End Select
                    RaiseEvent OnStateChangedUi(Me, value, OldState)
                End Sub)
                If HasOnStateChangedThread Then RunInThread(Sub() RaiseEvent OnStateChangedThread(Me, value, OldState))
            End Set
        End Property
        Private _State As LoadState = LoadState.Waiting
        Public Property LoadingState As MyLoading.MyLoadingState Implements ILoadingTrigger.LoadingState
            Get
                Return _LoadingState
            End Get
            Set(value As MyLoading.MyLoadingState)
                If _LoadingState = value Then Return
                Dim OldState = _LoadingState
                _LoadingState = value
                RaiseEvent LoadingStateChanged(value, OldState)
            End Set
        End Property
        Private _LoadingState As MyLoading.MyLoadingState = MyLoading.MyLoadingState.Stop
        Public Event LoadingStateChanged(NewState As MyLoading.MyLoadingState, OldState As MyLoading.MyLoadingState) Implements ILoadingTrigger.LoadingStateChanged

        Public Property [Error] As Exception = Nothing

        Public Block As Boolean = True

        Public Show As Boolean = True

        Public IsForceRestarting As Boolean = False

        Public Overridable Property Progress As Double
            Get
                Select Case State
                    Case LoadState.Waiting
                        Return 0
                    Case LoadState.Loading
                        Return If(_Progress = -1, 0, _Progress)
                    Case Else
                        Return 1
                End Select
            End Get
            Set(value As Double)
                If _Progress = value Then Return
                Dim OldValue = _Progress
                _Progress = value
                RaiseEvent ProgressChanged(value, OldValue)
            End Set
        End Property
        Private _Progress As Double = -1
        Public Event ProgressChanged(NewProgress As Double, OldProgress As Double) Implements ILoadingTrigger.ProgressChanged

        Public Function CreateSyncProgressProvider(Optional FromPercentage As Double = 0, Optional ToPercentage As Double = 1) As ProgressProvider
            Dim Provider As New ProgressProvider
            AddHandler Provider.ProgressChanged,
            Sub(ChangedProgress)
                Dim RawProgress = Provider.GetRaw()
                Progress = FromPercentage + (RawProgress.actual + RawProgress.skiped) * (ToPercentage - FromPercentage)
            End Sub
            Return Provider
        End Function

        Public Property ProgressWeight As Double = 1

        Public MustOverride Sub Start(Optional Input As Object = Nothing, Optional IsForceRestart As Boolean = False)
        Public MustOverride Sub Cancel()
        Public MustOverride Sub Failed(Ex As Exception)
        Private CancellationTokenSource As CancellationTokenSource

        Public Function CreateCancellationToken() As CancellationToken
            SyncLock LockState
                If CancellationTokenSource Is Nothing OrElse CancellationTokenSource.IsCancellationRequested Then
                    CancellationTokenSource = New CancellationTokenSource
                    CancellationTokenSource.Token.Register(AddressOf Cancel)
                End If
                If State = LoadState.Canceled Then CancellationTokenSource.Cancel()
                Return CancellationTokenSource.Token
            End SyncLock
        End Function

        Public Const WaitForExitTimeoutMessage As String = "等待加载器执行超时。"

        Public Sub WaitForExit(Optional Input As Object = Nothing, Optional LoaderToSyncProgress As LoaderBase = Nothing, Optional IsForceRestart As Boolean = False,
                               Optional c As CancellationToken = Nothing, Optional p As ProgressProvider = Nothing)
            Start(Input, IsForceRestart)
            Do While State = LoadState.Loading
                If LoaderToSyncProgress IsNot Nothing Then LoaderToSyncProgress.Progress = Progress
                p?.Set(Progress)
                c.ThrowIfCancellationRequested()
                Thread.Sleep(10)
            Loop
            If State = LoadState.Finished Then
                Return
            ElseIf State = LoadState.Canceled Then
                Throw New OperationCanceledException("加载器执行已中断")
            ElseIf IsNothing([Error]) Then
                Throw New Exception("未知错误！")
            Else
                Throw New Exception([Error].Message, [Error])
            End If
        End Sub

        Public Sub WaitForExitTime(Timeout As Integer, Optional Input As Object = Nothing, Optional TimeoutMessage As String = WaitForExitTimeoutMessage, Optional LoaderToSyncProgress As Object = Nothing, Optional IsForceRestart As Boolean = False)
            Start(Input, IsForceRestart)
            Do While State = LoadState.Loading
                If LoaderToSyncProgress IsNot Nothing Then LoaderToSyncProgress.Progress = Progress
                Thread.Sleep(10)
                Timeout -= 10
                If Timeout < 0 Then Throw New TimeoutException(TimeoutMessage)
            Loop
            If State = LoadState.Finished Then
                Return
            ElseIf State = LoadState.Canceled Then
                Throw New OperationCanceledException("加载器执行已中断")
            ElseIf IsNothing([Error]) Then
                Throw New Exception("未知错误！")
            Else
                Throw [Error]
            End If
        End Sub

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim base = TryCast(obj, LoaderBase)
            Return base IsNot Nothing AndAlso Uuid = base.Uuid
        End Function

    End Class

    Public Class LoaderTask(Of TIn, TOut)
        Inherits LoaderBase

        Protected Friend ThreadPriority As ThreadPriority

        Protected Friend LoadDelegate As Action(Of LoaderTask(Of TIn, TOut))
        Protected Friend InputDelegate As Func(Of Object)

        Public ReadOnly Property IsCanceled As Boolean
            Get
                Return IsCanceledWithThread(Thread.CurrentThread)
            End Get
        End Property

        Public Function IsCanceledWithThread(Thread As Thread) As Boolean
            Return LastRunningThread Is Nothing OrElse Not ReferenceEquals(Thread, LastRunningThread) OrElse State = LoadState.Canceled
        End Function

        Public ReloadTimeout As Integer = -1

        Public LastFinishedTime As Long = 0

        Public LastRunningThread As Thread = Nothing

        Public Input As TIn = Nothing
        Public Output As TOut = Nothing

        Public Function StartGetInput(Optional Input As TIn = Nothing, Optional InputDelegate As Func(Of TIn) = Nothing) As TIn
            If InputDelegate Is Nothing Then InputDelegate = Me.InputDelegate
            Dim NewInput As TIn = Nothing
            If (Input Is Nothing OrElse (NewInput IsNot Nothing AndAlso Input.Equals(NewInput))) AndAlso InputDelegate IsNot Nothing Then
                RunInUiWait(Sub() Input = InputDelegate())
            End If
            Return Input
        End Function

        Public Function ShouldStart(ByRef Input As TIn, Optional IsForceRestart As Boolean = False, Optional IgnoreReloadTimeout As Boolean = False) As Boolean

            Try
                Input = StartGetInput(Input)
            Catch ex As Exception
                Logger.Error(ex, $"加载输入获取失败（{Name}）", LogBehavior.Toast)
                [Error] = ex
                SyncLock LockState
                    State = LoadState.Failed
                End SyncLock
            End Try

            If IsForceRestart Then Return True
            If ((Input Is Nothing) <> (Me.Input Is Nothing)) OrElse (Input IsNot Nothing AndAlso Not Input.Equals(Me.Input)) Then Return True

            If (State = LoadState.Loading OrElse State = LoadState.Finished) AndAlso
               (IgnoreReloadTimeout OrElse ReloadTimeout = -1 OrElse
               LastFinishedTime = 0 OrElse GetTimeMs() - LastFinishedTime < ReloadTimeout) Then
                Return False
            Else
                Return True
            End If
        End Function
        Public Overrides Sub Start(Optional Input As Object = Nothing, Optional IsForceRestart As Boolean = False)

            If ShouldStart(Input, IsForceRestart) Then

                If State = LoadState.Loading Then TriggerThreadInterrupt()
                Me.Input = Input
                SyncLock LockState
                    State = LoadState.Loading
                    Progress = -1
                End SyncLock
            Else
                Return
            End If

            LastRunningThread = New Thread(
            Sub()
                Try
                    IsForceRestarting = IsForceRestart
                    Logger.Trace(Function() $"加载线程 {Name} 已{If(IsForceRestarting, "强制", "")}启动")
                    LoadDelegate(Me)
                    If IsCanceled Then
                        Logger.Warn($"加载线程 {Name} 已中断但线程正常运行至结束，输出被弃用（最新线程：{If(LastRunningThread Is Nothing, -1, LastRunningThread.ManagedThreadId)}）")
                        Return
                    End If
                    Logger.Trace(Function() $"加载线程 {Name} 已完成")
                    RaisePreviewFinish()
                    State = LoadState.Finished
                    LastFinishedTime = GetTimeMs()
                Catch ex As Exception
                    If ex.IsCanceled Then

                        Logger.Trace(ex, $"加载线程 {Name} 已取消，当前已完成 {Math.Round(Progress * 100)}%")
                        If Not IsCanceled Then State = LoadState.Canceled
                    Else
                        Failed(ex)
                    End If
                End Try
            End Sub) With {.Name = "L/" & Name, .Priority = ThreadPriority}
            Try
                LastRunningThread.Start()
            Catch ex As ThreadStateException
                Thread.Sleep(500)
                LastRunningThread.Start()
            End Try
        End Sub
        Public Overrides Sub Failed(ex As Exception)
            [Error] = ex
            SyncLock LockState
                If IsCanceled OrElse State >= LoadState.Finished Then Return
                State = LoadState.Failed
            End SyncLock
            Logger.Warn(ex, $"加载线程 {Name} 出错，已完成 {Math.Round(Progress * 100)}%")
            TriggerThreadInterrupt()
        End Sub
        Public Overrides Sub Cancel()
            SyncLock LockState
                If State <> LoadState.Loading Then Return
                State = LoadState.Canceled
            End SyncLock
            TriggerThreadInterrupt()
        End Sub
        Private Sub TriggerThreadInterrupt()
            If LastRunningThread Is Nothing Then Return
            If LastRunningThread.IsAlive Then
                LastRunningThread.Interrupt()
                Logger.Trace(Function() $"加载线程 {Name} 已中断")
            End If
            LastRunningThread = Nothing
        End Sub

        Public Sub New()

        End Sub
        Public Sub New(Name As String, LoadDelegate As Action(Of LoaderTask(Of TIn, TOut)), Optional InputDelegate As Func(Of TIn) = Nothing, Optional Priority As ThreadPriority = ThreadPriority.Normal)
            Me.Name = Name
            Me.LoadDelegate = LoadDelegate
            Me.InputDelegate = InputDelegate
            ThreadPriority = Priority
        End Sub

    End Class

    Public Class LoaderWorker
        Inherits LoaderBase
        Public WithEvents Worker As IWorker
        Public Sub New(Name As String, Worker As IWorker)
            Me.Name = Name
            Me.Worker = Worker
            AddHandler Worker.Progress.ProgressChanged, Sub(Raw) Progress = Raw.actual + Raw.skiped
        End Sub

        Public Overrides Sub Start(Optional Input As Object = Nothing, Optional IsForceRestart As Boolean = False)
            SyncLock LockState
                State = LoadState.Loading
            End SyncLock
            Logger.Trace(Function() $"Worker {Name} 已启动")
            IsForceRestarting = IsForceRestart
            Worker.Start()
        End Sub
        Private Sub Worker_Succeeded() Handles Worker.Succeeded
            Logger.Trace(Function() $"Worker {Name} 已完成")
            RaisePreviewFinish()
            SyncLock LockState
                State = LoadState.Finished
            End SyncLock
        End Sub
        Public Overrides Sub Cancel() Handles Worker.Canceled
            SyncLock LockState
                If State <> LoadState.Loading Then Return
                State = LoadState.Canceled
            End SyncLock
            Logger.Trace(Function() $"Worker {Name} 已取消，当前已完成 {Math.Round(Progress * 100)}%")
            Worker.Cancel()
        End Sub
        Public Overrides Sub Failed(Ex As Exception) Handles Worker.Failed
            SyncLock LockState
                If State >= LoadState.Finished Then Return
                State = LoadState.Failed
            End SyncLock
            Logger.Warn(Ex, $"Worker {Name} 出错，已完成 {Math.Round(Progress * 100)}%")
            [Error] = Ex
            Worker.Cancel()
        End Sub
    End Class

    Public Class LoaderCombo(Of InputType)
        Inherits LoaderBase

        Public Overrides Property Progress() As Double
            Get
                Select Case State
                    Case LoadState.Waiting
                        Return 0
                    Case LoadState.Loading
                        Dim Total As Double = 0, Finished As Double = 0
                        For Each Loader In Loaders
                            Total += Loader.ProgressWeight
                            Finished += Loader.ProgressWeight * Loader.Progress
                        Next
                        If Total = 0 Then Return 0
                        Return Finished / Total
                    Case Else
                        Return 1
                End Select
            End Get
            Set(value As Double)
                Throw New Exception("多重加载器不支持设置进度")
            End Set
        End Property

        Public Loaders As New List(Of LoaderBase)
        Public Input As InputType
        Public Sub New(Name As String, Loaders As IEnumerable(Of LoaderBase))
            Me.Loaders.Clear()
            For Each Loader As LoaderBase In Loaders
                If Loader IsNot Nothing Then
                    Me.Loaders.Add(Loader)
                    AddHandler Loader.OnStateChangedThread, AddressOf SubTaskStateChanged
                    Loader.HasOnStateChangedThread = True
                End If
            Next
            InitParent(Nothing)
            Me.Name = Name
        End Sub
        Public Overrides Sub InitParent(Parent As LoaderBase)
            Me.Parent = Parent
            For Each Loader In Loaders
                Loader.InitParent(Me)
            Next
        End Sub
        Public Overrides Sub Start(Optional Input As Object = Nothing, Optional IsForceRestart As Boolean = False)
            IsForceRestarting = IsForceRestart

            SyncLock LockState
                If State = LoadState.Loading Then
                    Return
                Else
                    State = LoadState.Loading
                End If
            End SyncLock

            Me.Input = Input
            If IsForceRestart Then
                For Each Loader In Loaders
                    Loader.State = LoadState.Waiting
                Next
            End If
            RunInThread(AddressOf Update)
        End Sub
        Public Overrides Sub Cancel()

            SyncLock LockState
                If State = LoadState.Loading OrElse State = LoadState.Waiting Then
                    State = LoadState.Canceled
                Else
                    Return
                End If
            End SyncLock
            RunInThread(
            Sub()

                For Each Loader In Loaders
                    Loader.Cancel()
                Next
            End Sub)
        End Sub
        Public Overrides Sub Failed(Ex As Exception)
            [Error] = Ex
            SyncLock LockState
                If State >= LoadState.Finished Then Return
                State = LoadState.Failed
            End SyncLock
            For Each Loader In Loaders
                Loader.Cancel()
            Next
            FrmMain.BtnExtraDownload.ShowRefresh()
        End Sub

        Private Sub SubTaskStateChanged(Loader As LoaderBase, NewState As LoadState, OldState As LoadState)
            Select Case NewState
                Case LoadState.Loading

                Case LoadState.Waiting

                Case LoadState.Finished

                    Update()
                Case LoadState.Canceled

                    Cancel()
                Case LoadState.Failed

                    Failed(New Exception(Loader.Name & "失败", Loader.Error))
            End Select
        End Sub

        Private Sub Update()
            If State = LoadState.Finished OrElse State = LoadState.Failed OrElse State = LoadState.Canceled Then Return
            Dim IsFinished As Boolean = True
            Dim Blocked As Boolean = False
            Dim Input As Object = Me.Input
            For Each Loader In Loaders
                Select Case Loader.State
                    Case LoadState.Finished

                        If GeneralUtils.IsGenericInstanceOf(Loader, GetType(LoaderTask(Of ,))) Then
                            If CType(Loader, Object).ShouldStart(If(Input IsNot Nothing AndAlso Loader.GetType.GenericTypeArguments.First Is Input.GetType, Input, Nothing), IgnoreReloadTimeout:=True) Then
                                Logger.Info($"由于输入条件变更，重启已完成的加载器 {Loader.Name}")
                                GoTo Restart
                            End If

                            Input = CType(Loader, Object).Output
                        End If

                        If Loader.Block AndAlso Not IsFinished Then Blocked = True
                    Case LoadState.Loading

                        If GeneralUtils.IsGenericInstanceOf(Loader, GetType(LoaderTask(Of ,))) Then
                            If CType(Loader, Object).ShouldStart(If(Input IsNot Nothing AndAlso Loader.GetType.GenericTypeArguments.First Is Input.GetType, Input, Nothing), IgnoreReloadTimeout:=True) Then
                                Logger.Warn($"由于输入条件变更，重启进行中的加载器 {Loader.Name}")
                                GoTo Restart
                            End If
                        End If

                        IsFinished = False
                        Blocked = True
                    Case Else
Restart:

                        IsFinished = False
                        If Blocked Then Continue For
                        If Input IsNot Nothing Then

                            If GeneralUtils.IsGenericInstanceOf(Loader, GetType(LoaderTask(Of ,))) OrElse GeneralUtils.IsGenericInstanceOf(Loader, GetType(LoaderCombo(Of ))) Then
                                Loader.Start(If(Loader.GetType.GenericTypeArguments.First Is Input.GetType, Input, Nothing), IsForceRestarting)
                            ElseIf TypeOf Loader Is LoaderWorker Then
                                Loader.Start(IsForceRestart:=IsForceRestarting)
                            ElseIf TypeOf Loader Is LoaderDownload Then
                                Loader.Start(If(TypeOf Input Is List(Of NetFile), Input, Nothing), IsForceRestarting)
                            Else
                                Throw New Exception("未知的加载器类型（" & Loader.GetType.Name & "）")
                            End If
                        Else
                            Loader.Start(IsForceRestart:=IsForceRestarting)
                        End If

                        If Loader.Block Then Blocked = True
                End Select
            Next
            If IsFinished Then

                RaisePreviewFinish()
                State = LoadState.Finished
                FrmMain.BtnExtraDownload.ShowRefresh()
            End If
        End Sub

        Public Shared Sub GetLoaderList(Loader As Object, ByRef List As List(Of LoaderBase), Optional RequireShow As Boolean = True)
            For Each SubLoader In Loader.Loaders
                If SubLoader.Show OrElse Not RequireShow Then List.Add(SubLoader)
                If GeneralUtils.IsGenericInstanceOf(SubLoader, GetType(LoaderCombo(Of ))) Then GetLoaderList(SubLoader, List)
            Next
        End Sub

        Public Sub GetLoaderList(ByRef List As List(Of LoaderBase), Optional RequireShow As Boolean = True)
            GetLoaderList(Me, List, RequireShow)
        End Sub

        Public Function GetLoaderList(Optional RequireShow As Boolean = True) As List(Of LoaderBase)
            Dim List As New List(Of LoaderBase)
            GetLoaderList(List, RequireShow)
            Return List
        End Function

    End Class

    Public LoaderTaskbar As New ConcurrentList(Of LoaderBase)
    Public LoaderTaskbarProgress As Double = 0
    Private LoaderTaskbarProgressLast As Shell.TaskbarItemProgressState = Shell.TaskbarItemProgressState.None

    Public Sub LoaderTaskbarAdd(Of T)(Loader As LoaderCombo(Of T))
        If FrmSpeedLeft IsNot Nothing Then FrmSpeedLeft.TaskRemove(Loader)
        LoaderTaskbar.Add(Loader)
        Logger.Info($"{Loader.Name} 已加入任务列表")
    End Sub
    Public Sub LoaderTaskbarProgressRefresh()
        Try
            Dim NewState As Shell.TaskbarItemProgressState
            Dim NewProgress As Double = LoaderTaskbarProgressGet()

            For Each Task In LoaderTaskbar
                If LoaderTaskbar.All(Function(l) l.State <> LoadState.Loading) OrElse
                   (Task.State = LoadState.Waiting OrElse Task.State = LoadState.Canceled) Then
                    FrmSpeedLeft?.TaskRefresh(Task)
                    LoaderTaskbar.Remove(Task)
                    Logger.Info($"{Task.Name} 已移出任务列表")
                End If
            Next

            If NewProgress <= 0 OrElse NewProgress >= 1 OrElse LoaderTaskbarProgress > NewProgress Then
                LoaderTaskbarProgress = NewProgress
            Else
                LoaderTaskbarProgress = LoaderTaskbarProgress * 0.9 + NewProgress * 0.1
            End If
            RunInUi(Sub() FrmMain.BtnExtraDownload.Progress = LoaderTaskbarProgress)

            If Not LoaderTaskbar.Any() OrElse LoaderTaskbarProgress = 1 Then
                NewState = Shell.TaskbarItemProgressState.None
            ElseIf LoaderTaskbarProgress < 0.015 Then
                NewState = Shell.TaskbarItemProgressState.Indeterminate
            Else
                NewState = Shell.TaskbarItemProgressState.Normal
                FrmMain.TaskbarItemInfo.ProgressValue = LoaderTaskbarProgress
            End If
            If LoaderTaskbarProgressLast <> NewState Then
                LoaderTaskbarProgressLast = NewState
                FrmMain.TaskbarItemInfo.ProgressState = NewState
                FrmMain.BtnExtraDownload.ShowRefresh()
            End If
        Catch ex As Exception
            Logger.Error(ex, "刷新任务栏进度显示失败")
        End Try
    End Sub
    Public Function LoaderTaskbarProgressGet() As Double
        Try
            If Not LoaderTaskbar.Any Then Return 1
            Return LoaderTaskbar.Select(Function(l) l.Progress).Average().Clamp(0, 1)
        Catch ex As Exception
            Logger.Error(ex, "获取任务栏进度出错")
            Return 0.5
        End Try
    End Function

    Private LoaderFolderDictionary As New Dictionary(Of LoaderBase, LoaderFolderDictionaryEntry)
    Private Structure LoaderFolderDictionaryEntry
        Public LastCheckTime As Date?
        Public FolderPath As String
        Public Overrides Function Equals(obj As Object) As Boolean
            If TypeOf obj IsNot LoaderFolderDictionaryEntry Then Return False
            Dim entry = DirectCast(obj, LoaderFolderDictionaryEntry)
            Return EqualityComparer(Of Date?).Default.Equals(LastCheckTime, entry.LastCheckTime) AndAlso
                   FolderPath = entry.FolderPath
        End Function
    End Structure
    Public Enum LoaderFolderRunType
        RunOnUpdated
        ForceRun
        UpdateOnly
    End Enum

    Public Function LoaderFolderRun(Loader As LoaderBase, FolderPath As String, Type As LoaderFolderRunType, Optional MaxDepth As Integer = 0, Optional ExtraPath As String = "", Optional WaitForExit As Boolean = False) As Boolean
        Dim Value As New LoaderFolderDictionaryEntry With {.FolderPath = FolderPath & ExtraPath, .LastCheckTime = Nothing}
        Try

            Dim FolderInfo = DirectoryUtils.GetInfo(FolderPath & ExtraPath)
            Value.LastCheckTime = If(FolderInfo.Exists, GetActualLastWriteTimeUtc(FolderInfo, MaxDepth), DirectCast(Nothing, Date?))

            If Type = LoaderFolderRunType.RunOnUpdated AndAlso LoaderFolderDictionary.ContainsKey(Loader) Then
                If FolderInfo.Exists Then
                    If LoaderFolderDictionary(Loader).LastCheckTime IsNot Nothing AndAlso
                       Value.Equals(LoaderFolderDictionary(Loader)) Then Return False
                Else
                    If LoaderFolderDictionary(Loader).LastCheckTime Is Nothing Then Return False
                End If
            End If
        Catch ex As Exception
            Logger.Warn(ex, "文件夹加载器启动检测出错")
        End Try

        LoaderFolderDictionary(Loader) = Value

        If Type = LoaderFolderRunType.UpdateOnly Then Return False
        If WaitForExit Then
            Loader.WaitForExit(FolderPath, IsForceRestart:=True)
        Else
            Loader.Start(FolderPath, IsForceRestart:=True)
        End If
        Return True
    End Function
    Private Function GetActualLastWriteTimeUtc(FolderInfo As DirectoryInfo, MaxDepth As Integer) As Date
        Dim Time As Date = FolderInfo.LastWriteTimeUtc
        If MaxDepth > 0 Then
            For Each Folder In FolderInfo.EnumerateDirectories
                Dim FolderTime As Date = GetActualLastWriteTimeUtc(Folder, MaxDepth - 1)
                If FolderTime > Time Then Time = FolderTime
            Next
        End If
        Return Time
    End Function

End Module
