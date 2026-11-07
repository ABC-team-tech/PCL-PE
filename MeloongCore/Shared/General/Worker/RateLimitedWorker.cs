namespace MeloongCore;

public enum RateLimitMode {

    ImmediateThenMerge,

    ImmediateThenIgnore,

    WaitUnillNoNewInvoke,
}

public class RateLimitedWorker(Action workload, double minimalIntervalMs, RateLimitMode mode) : IDisposable {

    private readonly RateLimitedAction action = mode switch {
        RateLimitMode.ImmediateThenMerge => Throttler.Throttle(workload, TimeSpan.FromMilliseconds(minimalIntervalMs), leading: true, trailing: true),
        RateLimitMode.WaitUnillNoNewInvoke => Debouncer.Debounce(workload, TimeSpan.FromMilliseconds(minimalIntervalMs), leading: false, trailing: true),
        _ => Debouncer.Debounce(workload, TimeSpan.FromMilliseconds(minimalIntervalMs), leading: true, trailing: false)
    };

    public void Start() {
        if (!disposed) action.Invoke();
    }

    private bool disposed = false;
    public virtual void Dispose() {
        if (disposed) return;
        disposed = true;
        action.Dispose();
    }

}
