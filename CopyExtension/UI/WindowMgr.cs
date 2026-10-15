using CopyExtension.Helpers;
using CopyExtension.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace CopyExtension.UI;

internal class WindowMgr(bool console)
{
    public bool IsBusy => Instance != null;
    public bool HasTasks { get { lock (tasksKey) { return tasks.Count > 0; } } }

    private Lock InstanceKey = new();
    private volatile ICopyWindow? Instance;
    private Action? onfinish;
    volatile bool allowquit = false;
    List<CopyTask> tasks = [];
    private Lock tasksKey = new();

    public void Start()
    {
        lock (InstanceKey)
        {
            Instance ??= console ? Attach(ConsoleWindow.Start(this)) : Attach(MainWindow.Start(this));
            allowquit = false;
        }
    }

    ICopyWindow Attach(ICopyWindow window)
    {
        window.ExitRequested += Window_ExitRequested;
        return window;
    }

    private void Window_ExitRequested()
    {
        SetFinishAction(null); // user cancelled, so most likely doesn't want this
        Quit();
    }

    [MemberNotNull(nameof(Instance))]
    void ThrowIfNoInstance()
    {
        if (Instance == null)
        {
            throw new Exception("App has already exited!");
        }
    }

    public HashSet<string> GetClipboardFiles()
    {
        lock (InstanceKey)
        {
            ThrowIfNoInstance();
            return Instance?.Invoke(i => i.GetClipboardFiles()) ?? [];
        }
    }

    public void AddTask(CopyTask task)
    {
        lock (tasksKey)
        {
            lock (InstanceKey)
            {
                ThrowIfNoInstance();
                tasks.Add(task);
                task.OnComplete += Task_OnComplete;
                Instance.Invoke(i => i.AddTask(task));
                Task.Run(CheckStartTasks);
            }
        }
    }

    private void Task_OnComplete(CopyTask task)
    {
        if (task.FollowUpTask != null && IsBusy)
        {
            AddTask(task.FollowUpTask);
        }
        lock (tasksKey)
        {
            tasks.Remove(task);
            if (tasks.Count == 0 && allowquit && IsBusy)
            {
                Quit();
            }
        }
            Task.Run(CheckStartTasks);
    }

    void CheckStartTasks()
    {
        lock (tasksKey)
        {
            lock (InstanceKey)
            {
                if (Instance == null) { return; }
                if (Program.Options.ReserveReading || Program.Options.ReserveWriting)
                {
                    var volumes = new HashSet<string>();
                    if (Program.Options.ReserveWriting)
                    {
                        volumes.AddRange(tasks.Where(c => c.IsStarted).Select(c => c.WritingVolume));
                    }
                    if (Program.Options.ReserveReading)
                    {
                        volumes.AddRange(tasks.Where(c => c.IsStarted).Select(c => c.ReadingVolume));
                    }
                    foreach (var c in tasks)
                    {
                        if (!c.IsStarted && (!Program.Options.ReserveWriting || !volumes.Contains(c.WritingVolume)) && (!Program.Options.ReserveReading || !volumes.Contains(c.ReadingVolume)))
                        {
                            if (Program.Options.ReserveWriting)
                            {
                                volumes.Add(c.WritingVolume);
                            }
                            if (Program.Options.ReserveReading)
                            {
                                volumes.Add(c.ReadingVolume);
                            }
                            c.Start();
                        }
                    }
                }
                else
                {
                    foreach (var t in tasks)
                    {
                        if (!t.IsCancelled && !t.IsStarted)
                        {
                            t.Start();
                        }
                    }
                }
            }
        }
    }

    public void AllowQuit()
    {
        lock (tasksKey)
        {
            lock (InstanceKey)
            {
                allowquit = true;
                if (tasks.Count == 0 && IsBusy)
                {
                    QuitInternal();
                }
            }
        }
    }

    public void Quit()
    {
        lock (tasksKey)
        {
            lock (InstanceKey)
            {
                QuitInternal();
            }
            tasks.ForEach(t => t.Cancel());
        }
    }

    //Needs lock (tasksKey) && lock (InstanceKey)
    private void QuitInternal()
    {
        Instance?.ExitRequested -= Window_ExitRequested;
        var instance = Instance;
        Instance = null;
        if (instance != null)
        {
            Task.Run(() =>
            {
                WaitForTaskCompletion();
                instance?.Post(static i => i.Quit());
            });
        }
    }

    public void WaitForTaskCompletion()
    {
        while (HasTasks)
        {
            Thread.Sleep(1000);
        }
    }

    public void WaitForExit()
    {
        AllowQuit();
        while (IsBusy)
        {
            Thread.Sleep(1000);
        }
    }

    public void Finish()
    {
        onfinish?.Invoke();
    }

    public void SetFinishAction(Action? onfinish)
    {
        this.onfinish = onfinish;
    }

    public T GetTasks<T>(Func<List<CopyTask>, T> torun)
    {
        lock (tasksKey)
        {
            lock (InstanceKey)
            {
                return torun(tasks);
            }
        }
    }

    public void GetTasks(Action<List<CopyTask>> torun)
    {
        lock (tasksKey)
        {
            lock (InstanceKey)
            {
                torun(tasks);
            }
        }
    }
}
