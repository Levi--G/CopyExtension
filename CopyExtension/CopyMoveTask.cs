using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZetaLongPaths;

namespace CopyExtension
{
    public class CopyMoveTask : CopyTask
    {
        public override string CurrentSpeedUnit => "B";

        public override long SpeedSmoothingTolerance => 10 * 1024 * 1024;

        private string[] sourcefolders;
        private string target;
        private CopyJobType copyType;

        public CopyMoveTask(string[] sourcefolders, string target, CopyJobType copyType, Filter[] filters) : base(filters)
        {
            this.sourcefolders = sourcefolders.Select(f => f.TrimEnd('\\')).ToArray();
            this.target = target.TrimEnd('\\');
            this.copyType = copyType;
            this.ReadingVolume = ZlpPathHelper.GetPathRoot(this.sourcefolders.First()).TrimEnd('\\');
            this.WritingVolume = ZlpPathHelper.GetPathRoot(this.target).TrimEnd('\\');
            this.Action = copyType == CopyJobType.Compare ? "Comparing" : (copyType == CopyJobType.Move || copyType == CopyJobType.MoveUnsafe) ? "Moving" : "Copying";
        }

        public override void Start()
        {
            base.Start();
            new Thread(DoWork).Start();
        }

        private ExistsAction ShowOverwriteDialog(IEnumerable<FileJob> items)
        {
            return OptionGui.GetChoiceResult($"{items.Count()}/{TotalItems} files already exist, do you want to retry?",
                            $"The following files already exist:{Environment.NewLine}{string.Join(Environment.NewLine, items.Select(e => $"{e.Source.OriginalPath} to {e.Target.OriginalPath}"))}",
                            new[] { ExistsAction.Overwrite, ExistsAction.OverwriteFix, ExistsAction.Rename, ExistsAction.Skip });
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
                    jobs.ForEach(j => j.Reset());
                    this.TotalItems = jobs.Count;
                    this.TotalProgress = jobs.Sum(j => j.TotalProgress);
                    DoStatus(true);
                    if (CheckCancel()) { return; }
                    var results = RunJobs(jobs).Where(r => !r.Success).ToList();
                    jobs = results.Select(r => r.Job).ToList();
                    if (CheckCancel() || results.Count == 0) { return; }
                    if (results.Any(r => r.IsOverWriteError))
                    {
                        var err = results.Where(r => r.IsOverWriteError).Select(r => r.Job).ToList();
                        var method = ShowOverwriteDialog(err);
                        jobs.ForEach(j => j.ExistsAction = method);
                    }
                    if (results.Any(r => !r.IsOverWriteError))
                    {
                        var err = results.Where(r => !r.IsOverWriteError).ToList();
                        var method = OptionGui.GetChoiceResult($"{err.Count}/{TotalItems} jobs errored, do you want to retry?",
                            $"The following files failed to copy:{Environment.NewLine}{string.Join(Environment.NewLine, err.Select(e => $"{e.Job.Source.OriginalPath}: {e.Reason}"))}",
                            new[] { RetryAction.Retry, RetryAction.Skip });
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
                CopyExtension.Logger.Log(this, e);
                CurrentAction = "Errored";
                CurrentName = $"{e.GetType().Name}: {e.Message}";
                DoStatus(true);
                IsPaused = true;
                CheckCancel();
            }
            finally
            {
                IsDone = true;
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
            var key = new object();
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
            if (CopyExtension.Options.SmallFileSize < 1)
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
                    if (current.FileSize > CopyExtension.Options.SmallFileSize)
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
                            if (current.FileSize < CopyExtension.Options.SmallFileSize)
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
            var sources = sourcefolders.SelectMany(s => GetFiles(s, ZlpPathHelper.GetDirectoryPathNameFromFilePath(s), target)).ToList();
            if (copyType == CopyJobType.Copy || copyType == CopyJobType.Move || copyType == CopyJobType.CopyUnsafe || copyType == CopyJobType.MoveUnsafe)
            {
                var existing = sources.Where(j => j.Target is ZlpFileInfo && j.Target.Exists).ToList();
                if (existing.Count > 0)
                {
                    var method = ShowOverwriteDialog(existing);
                    sources.ForEach(j => j.ExistsAction = method);
                }
            }
            return sources;
        }

        private IEnumerable<FileJob> GetFiles(string f, string source, string target)
        {
            if (ZlpIOHelper.DirectoryExists(f))
            {
                ZlpDirectoryInfo dir = new ZlpDirectoryInfo(f);
                foreach (var item in GetFiles(dir, source, target))
                {
                    yield return item;
                }
            }
            else if (ZlpIOHelper.FileExists(f))
            {
                ZlpFileInfo file = new ZlpFileInfo(f);
                yield return new FileJob(file, new ZlpFileInfo(Replacedir(f, source, target)), copyType, CheckCancel);
            }
        }

        private IEnumerable<FileJob> GetFiles(ZlpDirectoryInfo dir, string source, string target)
        {
            yield return new FileJob(dir, new ZlpDirectoryInfo(Replacedir(dir.FullName, source, target)), copyType, CheckCancel);
            foreach (var file in dir.GetFiles())
            {
                yield return new FileJob(file, new ZlpFileInfo(Replacedir(file.FullName, source, target)), copyType, CheckCancel);
            }
            foreach (var subdir in dir.GetDirectories())
            {
                foreach (var item in GetFiles(subdir, source, target))
                {
                    yield return item;
                }
            }
        }
    }
}