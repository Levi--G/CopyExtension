using System;
using System.Collections.Generic;
//using System.IO;
using System.Linq;
using System.Threading;
using ZetaLongPaths;

namespace CopyExtension
{
    internal class DeleteTask : CopyTask
    {
        public override string FullAction => $"{Action} on {WritingVolume}";

        public override long SpeedSmoothingTolerance => 4;
        public override string CurrentSpeedUnit => "items";

        private string[] target;
        public DeleteTask(IEnumerable<string> target, Filter[] filters) : base(filters)
        {
            this.target = target.Select(f => f.TrimEnd('\\')).ToArray();
            this.WritingVolume = ZlpPathHelper.GetPathRoot(this.target.First()).TrimEnd('\\');
            this.Action = "Delete";
        }

        public override void Start()
        {
            new Thread(DoWork).Start();
            base.Start();
        }

        private void DoWork()
        {
            try
            {
                CurrentAction = "Discovery";
                DoStatus(true);
                var targets = target.Select(t => ZlpIOHelper.DirectoryExists(t) ? new ZlpDirectoryInfo(t) : (ZlpIOHelper.FileExists(t) ? new ZlpFileInfo(t) : (IZlpFileSystemInfo)null)).Where(t => t != null).ToList();
                var todelete = new List<IZlpFileSystemInfo>();
                foreach (var fd in targets)
                {
                    if (fd is ZlpDirectoryInfo dir)
                    {
                        FindItemsRecursive(dir, todelete);
                    }
                }
                todelete.RemoveAll(i => !MatchesFilters(i));
                if (CheckCancel()) { return; }
                CurrentAction = "Deleting";
                this.TotalItems = todelete.Count;
                this.TotalProgress = TotalItems;
                DoStatus(true);
                foreach (var item in todelete)
                {
                    if (CheckCancel()) { return; }
                    try
                    {
                        CurrentName = item.FullName;
                        DoStatus(false);
                        if (item is ZlpDirectoryInfo dir)
                        {
                            if (dir.SafeExists())
                            {
                                dir.SafeDeleteContents();
                                dir.SafeDelete();
                            }
                        }
                        else if (item is ZlpFileInfo file)
                        {
                            file.SafeDelete();
                        }
                    }
                    catch
                    {

                    }
                    CurrentItems++;
                    CurrentProgress = CurrentItems;
                }
                IsSuccess = true;
            }
            catch (Exception e)
            {
                CurrentAction = "Errored";
                CurrentName = $"{e.GetType().Name}: {e.Message}";
                DoStatus(true);
                Thread.Sleep(5000);
                IsPaused = true;
                CheckCancel();
            }
            finally
            {
                IsDone = true;
                DoComplete();
            }
        }

        private void FindItemsRecursive(ZlpDirectoryInfo Dir, List<IZlpFileSystemInfo> items)
        {
            if (IsCancelled) { return; }
            foreach (var subdir in Dir.GetDirectories())
            {
                items.Add(subdir);
                FindItemsRecursive(subdir, items);
            }
            foreach (var file in Dir.GetFiles())
            {
                items.Add(file);
            }
        }
    }
}
