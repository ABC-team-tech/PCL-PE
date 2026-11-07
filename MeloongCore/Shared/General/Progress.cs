namespace MeloongCore;

public class ProgressProvider {

    public event Action<(double actual, double skiped)>? ProgressChanged;
    private event Action? ChildProgressChanged;
    private void InvokeProgressChanged() {
        ProgressChanged?.Invoke(GetRaw());
        ChildProgressChanged?.Invoke();
    }

    private (double actual, double skiped, double splited) progressParts = (0, 0, 0);
    private double progressSum => progressParts.actual + progressParts.skiped + progressParts.splited;

    private bool _Set(double value, bool skiped) {
        value = value.Clamp(0, 1);
        double delta = value - progressSum;
        if (delta < 0) {
            progressParts = (progressParts.actual * value / progressSum,
                              progressParts.skiped * value / progressSum,
                              progressParts.splited * value / progressSum);
        } else if (skiped) {
            progressParts.skiped += delta;
        } else {
            progressParts.actual += delta;
        }
        return delta != 0;
    }

    public void Set(double value, bool skiped = false, ChildrenAction action = ChildrenAction.Finished) {
        bool changed;
        lock (this) {
            changed = _Set(value, skiped);
            changed |= AssumeChildrenAs(action);
        }
        if (changed) InvokeProgressChanged();
    }

    public void Add(double value, bool skiped = false, ChildrenAction action = ChildrenAction.Finished) {
        bool changed;
        lock (this) {
            changed = _Set(value + progressSum, skiped);
            changed |= AssumeChildrenAs(action);
        }
        if (changed) InvokeProgressChanged();
    }

    public void Finish(ChildrenAction action = ChildrenAction.Finished) => Set(1, skiped: false, action: action);

    public void Skip(ChildrenAction action = ChildrenAction.Skiped) => Set(1, skiped: true, action: action);

    public void Reset() {
        bool changed;
        lock (this) {
            changed = progressSum > 0;
            progressParts = (0, 0, 0);
            childrens.ForEach(c => c.child.ChildProgressChanged -= InvokeProgressChanged);
            childrens.Clear();
        }
        if (changed) InvokeProgressChanged();
    }

    private readonly List<(double percentage, ProgressProvider child)> childrens = [];

    public ProgressProvider SplitTo(double value) {
        lock (this) return SplitBy(value - progressSum).First();
    }

    public List<ProgressProvider> SplitBy(params double[] percentages) {
        lock (this) {
            if (percentages.Any(p => p <= 0)) throw new ArgumentException("子项进度必须为正数且");
            progressParts.splited += (percentages.Sum() + progressSum).Clamp(0, 1) - progressSum;
            return percentages.Select(percentage => {
                var sub = new ProgressProvider();
                sub.ChildProgressChanged += InvokeProgressChanged;
                childrens.Add((percentage, sub));
                return sub;
            }).ToList();
        }
    }

    public enum ChildrenAction {

        None,

        Finished,

        Skiped
    }

    private bool AssumeChildrenAs(ChildrenAction action) {
        if (action == ChildrenAction.None) return false;
        lock (this) {
            bool changed = false;
            if (childrens.Any(c => c.percentage > 0)) {
                var mult = progressParts.splited / childrens.Sum(c => c.percentage);
                childrens.ForEach(c => {
                    var (subActual, subSkiped) = c.child.GetRaw();
                    progressParts.actual += mult * c.percentage * (action == ChildrenAction.Finished ? (1 - subSkiped) : subActual);
                    progressParts.skiped += mult * c.percentage * (action == ChildrenAction.Skiped ? (1 - subActual) : subSkiped);
                    c.child.ChildProgressChanged -= InvokeProgressChanged;
                    if (subActual + subSkiped < 0.9999999) changed = true;
                });
            }
            progressParts.splited = 0;
            childrens.Clear();
            return changed;
        }
    }

    public (double actual, double skiped) GetRaw() {
        lock (this) {
            (double actual, double skiped) current = (progressParts.actual, progressParts.skiped);
            if (childrens.Any(c => c.percentage > 0)) {
                var mult = progressParts.splited / childrens.Sum(c => c.percentage);
                childrens.ForEach(c => {
                    var (subActual, subSkiped) = c.child.GetRaw();
                    current.actual += mult * c.percentage * subActual;
                    current.skiped += mult * c.percentage * subSkiped;
                });
            }
            return current;
        }
    }

    public bool IsFinished => GetRaw().actual + GetRaw().skiped > 0.9999999;

    private (double actual, double skiped) observedProgress = (0, 0);
    private double incrementProgress = 0;

    public double GetIncreasing() {
        lock (this) {
            (double actual, double skiped) current = GetRaw();
            if (current == observedProgress) return incrementProgress;
            if (IsFinished) {
                incrementProgress = 1;
            } else if (current.actual >= observedProgress.actual) {
                incrementProgress += (1 - incrementProgress) *
                    (current.actual - observedProgress.actual) / (1 - observedProgress.actual - current.skiped);
            }
            observedProgress = current;
            return incrementProgress;
        }
    }

}

public class ProgressObserver : RateLimitedWorker {
    private readonly ProgressProvider provider;
    public ProgressObserver(ProgressProvider provider, Action<double> updateAction, double minimalIntervalMs = 70)
        : base(() => Observe(provider, updateAction), minimalIntervalMs, RateLimitMode.ImmediateThenMerge) {
        this.provider = provider;
        provider.ProgressChanged += StartObserver;
    }
    public override void Dispose() {
        provider.ProgressChanged -= StartObserver;
        base.Dispose();
    }
    private void StartObserver((double actual, double skiped) raw) => Start();
    private static void Observe(ProgressProvider provider, Action<double> updateAction) {
        double progress = provider.GetIncreasing();

        updateAction(progress);
    }

}
