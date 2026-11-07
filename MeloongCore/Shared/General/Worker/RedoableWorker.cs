namespace MeloongCore;

public interface IWorker {

    void Start(CancellationToken cancellationToken = default, ProgressProvider? progressOverride = null);

    void Cancel();

    bool WaitIfRunning(int millisecondsTimeout = -1, CancellationToken cancellationToken = default);

    Task<bool> WaitIfRunningAsync(int millisecondsTimeout = -1, CancellationToken cancellationToken = default);

    event Action? Started;

    event Action? Stopped;

    public event Action? Succeeded;

    public event Action<Exception>? Failed;

    event Action? Canceled;

    bool Running { get; }

    bool HasSucceeded { get; }

    bool LastSucceeded { get; }

    ProgressProvider Progress { get; }
}

public abstract class RedoableWorkerBase<TOut>(Func<CancellationToken?, ProgressProvider?, TOut> workload,
    ProgressProvider? progress = null, [CallerMemberName] string creatorMemberName = "") : IWorker {

    public bool Running { get { lock (this) return running; } }
    private bool running;

    public event Action? Started;

    public event Action? Stopped;

    public bool HasSucceeded { get { lock (this) return hasSucceeded; } }
    private bool hasSucceeded;

    public event Action? Succeeded;

    public event Action<Exception>? Failed;

    public event Action? Canceled;

    public bool LastSucceeded { get { lock (this) return lastSucceeded; } }
    private bool lastSucceeded;

    public TOut LastResult {
        get {
            lock (this) {
                if (typeof(TOut) == typeof(NoType)) throw new InvalidOperationException("该 Worker 没有返回值。");
                if (!hasSucceeded) throw new InvalidOperationException("从未成功完成过。");
                return lastResult == null ? default! : (TOut) lastResult;
            }
        }
    }
    private object? lastResult;

    public ProgressProvider Progress { get; private set; } = progress ?? new ProgressProvider();

    private bool pendingRedo;

    private CancellationTokenSource? realCts;
    private CancellationToken lastToken;
    private readonly ManualResetEventSlim idleEvent = new(initialState: true);

    public void Start(CancellationToken cancellationToken = default, ProgressProvider? progressOverride = null) {

        lock (this) {
            lastToken = cancellationToken;
            if (progressOverride != null) Progress = progressOverride;
            if (running) {
                Logger.Info($"{creatorMemberName}：接到重启请求");
                pendingRedo = true;
                realCts?.Cancel();
                return;
            }
            running = true; idleEvent.Reset();
            realCts = CancellationTokenSource.CreateLinkedTokenSource(lastToken);
        }
        var th = new Thread(_Start) { IsBackground = true, Name = $"W/{creatorMemberName}" };
        th.Start();
    }
    private void _Start() {
        Logger.Info($"{creatorMemberName}：运行开始");
        Started?.Invoke();
        while (true) {
            try {
                realCts!.Token.ThrowIfCancellationRequested();
                Progress?.Reset();
                TOut result = workload(realCts.Token, Progress);
                realCts.Token.ThrowIfCancellationRequested();
                lock (this) {
                    realCts?.Dispose();
                    if (pendingRedo) {
                        pendingRedo = false;
                        realCts = CancellationTokenSource.CreateLinkedTokenSource(lastToken);
                        continue;
                    }
                    realCts = null;
                    lastSucceeded = true;
                    lastResult = result;
                    hasSucceeded = true;
                    running = false; idleEvent.Set();
                    Progress?.Finish();
                }
                Logger.Info($"{creatorMemberName}：运行成功");
                Succeeded?.Invoke();
            } catch (Exception ex) {
                if (!ex.IsCanceled())
                    Logger.Log(ex, $"{creatorMemberName}：运行失败{(pendingRedo ? "，但即将重启，或可忽略" : "")}",
                        pendingRedo ? LogLevel.Warn : LogLevel.Error, LogBehavior.ToastIfDebug);
                lock (this) {
                    realCts?.Dispose();
                    if (pendingRedo) {
                        Logger.Info($"{creatorMemberName}：重启开始");
                        pendingRedo = false;
                        realCts = CancellationTokenSource.CreateLinkedTokenSource(lastToken);
                        continue;
                    }
                    realCts = null;
                    lastSucceeded = false;
                    running = false; idleEvent.Set();
                    Progress?.Skip();
                }
                if (ex.IsCanceled()) {
                    Logger.Info($"{creatorMemberName}：运行已取消");
                    Canceled?.Invoke();
                } else {
                    Failed?.Invoke(ex);
                }
            }
            break;
        }
        Stopped?.Invoke();
    }

    public void Cancel() {
        lock (this) {
            pendingRedo = false;
            Logger.Info($"{creatorMemberName}：接到取消请求");
            realCts?.Cancel();
        }
    }

    public bool WaitIfRunning(int millisecondsTimeout = -1, CancellationToken cancellationToken = default)
        => idleEvent.Wait(millisecondsTimeout, cancellationToken);

    public async Task<bool> WaitIfRunningAsync(int millisecondsTimeout = -1, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waitHandle = ThreadPool.RegisterWaitForSingleObject(idleEvent.WaitHandle,
            (_, timedOut) => completionSource.TrySetResult(!timedOut), null, millisecondsTimeout, executeOnlyOnce: true);
        using var cancellationRegistration = cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken));
        try {
            return await completionSource.Task.NoCapture();
        } finally {
            waitHandle.Unregister(null);
        }
    }

}

public class RedoableWorker<TOut> : RedoableWorkerBase<TOut> {
    public RedoableWorker(Func<CancellationToken?, ProgressProvider?, TOut> workload, [CallerMemberName] string creatorMemberName = "")
        : base(workload, creatorMemberName: creatorMemberName) { }
    public RedoableWorker(Func<CancellationToken?, TOut> workload, [CallerMemberName] string creatorMemberName = "")
        : base((c, _) => workload(c), creatorMemberName: creatorMemberName) { }
    public RedoableWorker(Func<TOut> workload, [CallerMemberName] string creatorMemberName = "")
        : base((_, _) => workload(), creatorMemberName: creatorMemberName) { }
}

public class RedoableWorker : RedoableWorkerBase<NoType?> {
    public RedoableWorker(Action<CancellationToken?, ProgressProvider?> workload, [CallerMemberName] string creatorMemberName = "")
        : base((c, p) => { workload(c, p); return null; }, creatorMemberName: creatorMemberName) { }
    public RedoableWorker(Action<CancellationToken?> workload, [CallerMemberName] string creatorMemberName = "")
        : base((c, _) => { workload(c); return null; }, creatorMemberName: creatorMemberName) { }
    public RedoableWorker(Action workload, [CallerMemberName] string creatorMemberName = "")
        : base((_, _) => { workload(); return null; }, creatorMemberName: creatorMemberName) { }
}

public class NoType { }
