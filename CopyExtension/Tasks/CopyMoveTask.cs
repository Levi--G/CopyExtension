using CopyExtension.Helpers;

namespace CopyExtension.Tasks;

public class CopyMoveTask : CopyTask
{
    public override string CurrentSpeedUnit => "B";

    public override long SpeedSmoothingTolerance => 10 * 1024 * 1024;

    private string[] sourcefolders;
    private string target;
    private CopyJobType copyType;

    public CopyMoveTask(string[] sourcefolders, string target, CopyJobType copyType, Filter[]? filters) : base(filters)
    {
        this.sourcefolders = sourcefolders.Select(f => f.TrimEnd('\\')).ToArray();
        this.target = target.TrimEnd('\\');
        this.copyType = copyType;
        this.ReadingVolume = PathExt.GetVolume(this.sourcefolders.First());
        this.WritingVolume = PathExt.GetVolume(this.target);
        this.Action = (copyType == CopyJobType.Compare || copyType == CopyJobType.CompareNames) ? "Comparing" : (copyType == CopyJobType.Move || copyType == CopyJobType.MoveUnsafe) ? "Moving" : "Copying";
    }

    public override void Start()
    {
        base.Start();
        new Thread(DoWork).Start();
    }


    public ExistsAction? DefaultExistsAction { get; set; }
    private ExistsAction ShowOverwriteDialog(IEnumerable<FileJob> items, int total)
    {
        return DefaultExistsAction ??= OptionGui.GetChoiceResult($"{items.Count()}/{total} files already exist, do you want to retry?",
                        $"The following files already exist:{Environment.NewLine}{string.Join(Environment.NewLine, items.Select(e => $"{e.Source} to {e.Target}"))}",
                        new Dictionary<string, ExistsAction>() { { "Overwrite", ExistsAction.Overwrite }, { "Rename", ExistsAction.Rename }, { "Skip", ExistsAction.Skip }, { "Compare", ExistsAction.Compare } });
    }

    private void DoWork()
    {
        try
        {
            CurrentAction = "Discovery";
            DoStatus(true);
            List<FileJob> jobs = Discover(sourcefolders, target);
            jobs.RemoveAll(i => !MatchesFilters(i.Source));
            this.TotalItems = jobs.Count;
            while (jobs.Count > 0)
            {
                jobs.ForEach(j => j.ResetProgress());
                this.TotalItems = jobs.Count;
                this.TotalProgress = jobs.Sum(j => j.TotalProgress);
                CurrentItems = 0;
                CurrentProgress = 0;
                DoStatus(true);
                if (CheckCancel()) { return; }
                var results = RunJobs(jobs).Where(r => !r.Success).ToList();
                jobs = results.Select(r => r.Job).ToList();
                if (CheckCancel() || results.Count == 0) { return; }
                if (results.Any(r => r.IsOverWriteError))
                {
                    var err = results.Where(r => r.IsOverWriteError).Select(r => r.Job).ToList();
                    var method = ShowOverwriteDialog(err, results.Count);
                    jobs.ForEach(j => j.ExistsAction = method);
                }
                if (results.Any(r => !r.IsOverWriteError))
                {
                    var err = results.Where(r => !r.IsOverWriteError).ToList();
                    var method = OptionGui.GetChoiceResult($"{err.Count}/{TotalItems} jobs errored, do you want to retry?",
                        $"The following files failed to copy:{Environment.NewLine}{string.Join(Environment.NewLine, err.Select(e => $"{e.Job.Source}: {e.Reason}"))}",
                        new Dictionary<string, RetryAction>() { { "Retry", RetryAction.Retry }, { "Skip", RetryAction.Skip } });
                    if (method != RetryAction.Retry)
                    {
                        return;
                    }
                }
            }
            IsSuccess = true;
        }
        catch (Exception e)
        {
            Program.Logger.Log(this, e);
            CurrentAction = "Errored";
            CurrentName = $"{e.GetType().Name}: {e.Message}";
            DoStatus(true);
            IsPaused = true;
            CheckCancel();
        }
        finally
        {
            DoComplete();
        }
    }

    private enum RetryAction
    {
        Retry, Skip
    }

    private List<FileResult> RunJobs(List<FileJob> jobs)
    {
        var results = new List<FileResult>();
        var key = new Lock();
        RunJobs(jobs, (j, batched) =>
        {
            long start = CurrentProgress;
            Action prog = null;
            Action<string> proga = null;
            if (!batched)
            {
                prog = () =>
                {
                    CurrentProgress = start + j.CurrentProgress;
                    DoStatus(false);
                };
                j.OnProgress += prog;
                proga = (s) =>
                {
                    CurrentAction = s;
                    DoStatus(true);
                };
                j.OnAction += proga;
                CurrentName = j.Name;
            }
            var r = j.Copy();
            CurrentItems++;
            if (!batched)
            {
                j.OnProgress -= prog;
                j.OnAction -= proga;
                if (r.Success)
                {
                    CurrentProgress = start + j.TotalProgress;
                }
                else
                {
                    CurrentProgress = start;
                }
            }
            else
            {
                if (r.Success)
                {
                    lock (key)
                    {
                        CurrentProgress += j.TotalProgress;
                    }
                }
                DoStatus(false);
            }
            lock (key)
            {
                results.Add(r);
            }
        });
        return results;
    }

    private void RunJobs(List<FileJob> jobs, Action<FileJob, bool> action)
    {
        if (Program.Options.SmallFileSize < 1)
        {
            foreach (var job in jobs)
            {
                action(job, false);
                if (CheckCancel()) { break; }
            }
            return;
        }
        if (jobs.Count < 1) { return; }
        var q = new Queue<FileJob>(jobs);
        var current = q.Dequeue();
        while (current != null && !CheckCancel())
        {
            var next = q.Count > 0 ? q.Peek() : null;
            if (current.IsFile)
            {
                if (current.FileSize > Program.Options.SmallFileSize)
                {
                    action(current, false);
                    current = q.Count > 0 ? q.Dequeue() : null;
                }
                else
                {
                    CurrentAction = (copyType == CopyJobType.Copy || copyType == CopyJobType.CopyUnsafe) ? FileJob.COPYING : ((copyType == CopyJobType.Move || copyType == CopyJobType.MoveUnsafe) ? FileJob.MOVING : FileJob.COMPARING);
                    CurrentName = "Bulk operation";
                    var key = new object();
                    var busy = 0;

                    while (current != null)
                    {
                        if (current.FileSize < Program.Options.SmallFileSize)
                        {
                            while (!CheckCancel())
                            {
                                lock (key)
                                {
                                    if (busy < 5)
                                    {
                                        break;
                                    }
                                }
                                Thread.Sleep(10);
                            }
                            lock (key)
                            {
                                busy++;
                            }
                            var threadeditem = current;
                            ThreadPool.QueueUserWorkItem((a) =>
                            {
                                try
                                {
                                    action(threadeditem, true);
                                }
                                finally
                                {
                                    lock (key)
                                    {
                                        busy--;
                                    }
                                }
                            });
                        }
                        else
                        {
                            break;
                        }
                        current = q.Count > 0 ? q.Dequeue() : null;
                    }
                    while (!CheckCancel())
                    {
                        lock (key)
                        {
                            if (busy == 0)
                            {
                                break;
                            }
                        }
                        Thread.Sleep(10);
                    }
                }
            }
            else
            {
                action(current, true);
                current = q.Count > 0 ? q.Dequeue() : null;
            }
        }
    }

    private List<FileJob> Discover(string[] sourcefolders, string target)
    {
        if (copyType != CopyJobType.CompareNames)
        {
            var sources = sourcefolders.SelectMany(s => GetFiles(s, Path.GetDirectoryName(s), target)).ToList();
            if (copyType.IsAnyMoveOrCopy())
            {
                var existing = sources.Where(j => File.Exists(j.Target)).ToList();
                if (existing.Count > 0)
                {
                    var method = ShowOverwriteDialog(existing, sources.Count);
                    sources.ForEach(j => j.ExistsAction = method);
                }
            }
            else
            {
                //TODO: sort the sources with files first so the empty dirs can be deleted
            }
            return sources;
        }
        else if (copyType == CopyJobType.CompareNames)
        {
            IEnumerable<string> Recurse(string d)
            {
                foreach (var dir in Directory.EnumerateDirectories(d))
                {
                    foreach (var file in Recurse(dir))
                    {
                        yield return file;
                    }
                }
                foreach (var file in Directory.EnumerateFiles(d))
                {
                    yield return file;
                }
            }
            IEnumerable<string> RecurseIfDir(string s)
            {
                if (Directory.Exists(s))
                {
                    foreach (var item in Recurse(s))
                    {
                        yield return item;
                    }
                }
                else if (File.Exists(s))
                {
                    yield return s;
                }
            }
            var names = sourcefolders.SelectMany(s => RecurseIfDir(s)).Select(Path.GetFileName).ToList();
            var targetfiles = RecurseIfDir(target).Where(f => names.Contains(Path.GetFileName(f))).Select(f => new FileJob(f, f, true, CopyJobType.CompareNames, CheckCancel)).ToList();
            return targetfiles;
        }
        throw new NotImplementedException();
    }

    //todo catch errors if enum fails

    private IEnumerable<FileJob> GetFiles(string fileordir, string? source, string target)
    {
        if (Directory.Exists(fileordir))
        {
            foreach (var item in GetFilesRecursive(fileordir, source, target))
            {
                yield return item;
            }
        }
        else if (File.Exists(fileordir))
        {
            yield return new FileJob(fileordir, Replacedir(fileordir, source, target), true, copyType, CheckCancel);
        }
    }

    private IEnumerable<FileJob> GetFilesRecursive(string dir, string? source, string target)
    {
        yield return new FileJob(dir, Replacedir(dir, source, target), false, copyType, CheckCancel);
        foreach (var file in Directory.GetFiles(dir))
        {
            yield return new FileJob(file, Replacedir(file, source, target), true, copyType, CheckCancel);
        }
        foreach (var subdir in Directory.GetDirectories(dir))
        {
            foreach (var item in GetFilesRecursive(subdir, source, target))
            {
                yield return item;
            }
        }
    }
}