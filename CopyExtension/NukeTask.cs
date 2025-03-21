using System;
using System.Collections.Generic;
//using System.IO;
using System.Linq;
using System.Threading;
using ZetaLongPaths;

namespace CopyExtension
{
    public class NukeTask : CopyTask
    {
        public override string FullAction => $"{Action} on {WritingVolume}";

        public override long SpeedSmoothingTolerance => 4;
        public override string CurrentSpeedUnit => "items";

        private string[] target;
        private bool dirmode;
        public NukeTask(IEnumerable<string> target, bool dirmode, Filter[] filters) : base(filters)
        {
            this.target = target.Select(f => f.TrimEnd('\\')).Where(t => ZlpIOHelper.DirectoryExists(t)).ToArray();
            this.dirmode = dirmode;
            this.WritingVolume = ZlpPathHelper.GetPathRoot(this.target.First()).TrimEnd('\\');
            this.Action = "Nuking folders";
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
                var todelete = new List<IZlpFileSystemInfo>();
                if (dirmode)
                {
                    foreach (var dir in target)
                    {
                        var root = new ZlpDirectoryInfo(dir);
                        FindEmptyDirsRecursive(root, todelete);
                        if (todelete.Contains(root))
                        {
                            todelete.Remove(root);
                        }
                    }
                }
                else
                {
                    foreach (var dir in target)
                    {
                        FindEmptyFilesRecursive(new ZlpDirectoryInfo(dir), todelete);
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
                                try
                                {
                                    dir.ForceDelete();
                                }
                                catch { }
                            }
                        }
                        else if (item is ZlpFileInfo file)
                        {
                            try
                            {
                                file.ForceDelete();
                            }
                            catch { }
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

        private bool FindEmptyDirsRecursive(ZlpDirectoryInfo Dir, List<IZlpFileSystemInfo> emptydirs)
        {
            if (IsCancelled) { return false; }
            var empty = true;
            foreach (var subdir in Dir.GetDirectories())
            {
                empty &= FindEmptyDirsRecursive(subdir, emptydirs);
            }
            if (empty && !Dir.GetFiles().Any(f => f.Name != "Thumbs.db"))
            {
                emptydirs.Add(Dir);
                return true;
            }
            return false;
        }

        private void FindEmptyFilesRecursive(ZlpDirectoryInfo Dir, List<IZlpFileSystemInfo> emptyfiles)
        {
            if (IsCancelled) { return; }
            foreach (var subdir in Dir.GetDirectories())
            {
                FindEmptyFilesRecursive(subdir, emptyfiles);
            }
            foreach (var file in Dir.GetFiles())
            {
                if (file.Length == 0)
                {
                    emptyfiles.Add(file);
                }
            }
        }
    }
}
