using CopyExtension.UI;
using System.Diagnostics;

namespace CopyExtension.Tasks;

public abstract class CopyTask
{
    public event Action<CopyTask>? OnComplete;
    public event Action<bool>? OnNewStatus;
    public CopyTask? FollowUpTask { get; set; }
    public IOptionGui? OptionGui { get; set; }

    public string? ReadingVolume { get; protected set; }
    public string? WritingVolume { get; protected set; }
    public virtual string FullAction => null;
    public string Action { get; protected set; }
    public string CurrentAction { get; protected set; }
    public abstract string CurrentSpeedUnit { get; }

    private DateTime last;
    private long LastSpeedProgress;
    private long lastspeedvalue;
    public long CurrentSpeedValue
    {
        get
        {
            var now = DateTime.Now;
            var time = (long)(now - last).TotalMilliseconds;
            if (time >= 500)
            {
                var c = CurrentProgress;
                var newspeedvalue = Math.Max(0, (c - LastSpeedProgress) * 1000 / time);
                lastspeedvalue = Math.Abs(newspeedvalue - lastspeedvalue) < SpeedSmoothingTolerance ? newspeedvalue : (newspeedvalue + lastspeedvalue) / 2;
                last = now;
                LastSpeedProgress = c;
            }
            return lastspeedvalue;
        }
    }
    public abstract long SpeedSmoothingTolerance { get; }

    public long CurrentProgress { get; protected set; }
    public long TotalProgress { get; protected set; }
    public byte Progress => TotalProgress == 0 ? (byte)0 : (byte)(CurrentProgress * 100 / TotalProgress);

    public string CurrentName { get; protected set; }
    public int CurrentItems { get; protected set; }
    public int TotalItems { get; protected set; }

    public bool IsStarted { get; protected set; }
    public bool IsPaused { get; protected set; }
    public bool IsCancelled { get; protected set; }
    public bool IsDone { get; private set; }
    public bool IsSuccess { get; protected set; }
    bool IsDoneExecuted = false;
    private Filter[]? filters;

    public CopyTask(Filter[]? filters)
    {
        this.filters = filters;
    }

    public TimeSpan? GetRemainingTime(long speed)
    {
        if (speed == 0 || TotalProgress == 0)
        {
            return null;
        }
        var remaining = (TotalProgress - CurrentProgress) / speed;
        return TimeSpan.FromSeconds(remaining);
    }

    public virtual void Start()
    {
        IsStarted = true;
        DoStatus(true);
    }

    public virtual void Pause()
    {
        if (!IsStarted)
        {
            Start();
        }
        else
        {
            IsPaused = !IsPaused;
            DoStatus(true);
        }
    }

    public virtual void Cancel()
    {
        IsCancelled = true;
        //IsDone = true;
        DoStatus(true);
    }

    protected void DoStatus(bool force)
    {
        OnNewStatus?.Invoke(force);
        if (IsDone)
        {
            DoComplete();
        }
    }

    protected void DoComplete()
    {
        IsDone = true;
        if (!IsDoneExecuted)
        {
            IsDoneExecuted = true;
            OnComplete?.Invoke(this);
        }
    }

    protected bool CheckCancel()
    {
        if (IsPaused || IsCancelled)
        {
            DoStatus(true);
            while (IsPaused && !IsCancelled)
            {
                Thread.Sleep(1000);
            }
        }
        return IsCancelled;
    }

    protected bool MatchesFilters(string path)
    {
        if (filters == null)
        {
            return true;
        }
        return filters.Select(f => f.Matches(path)).FirstOrDefault(r => r != null) ?? !filters.Any(f => f.Allow);
    }

    protected static string Replacedir(string f, string? source, string target)
    {
        var rel = f.AsSpan();
        Debug.Assert(!string.IsNullOrEmpty(source));
        if (!string.IsNullOrEmpty(source))
        {
            Debug.Assert(rel.StartsWith(source));
            rel = rel[source.Length..];
        }
        return Path.Join(target, rel.TrimStart('\\'));
    }
}
