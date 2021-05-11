using System;
using System.Collections.Generic;
//using System.IO;
using System.Linq;
using System.Threading;
using ZetaLongPaths;

namespace CopyExtension
{
    internal class NukeTask : CopyTask
    {
        public override string FullAction => $"{Action} on {WritingVolume}";

        private DateTime last;
        private long LastSpeedProgress;
        private long lastspeedvalue;
        public override string CurrentSpeedUnit => "items";
        public override long CurrentSpeedValue
        {
            get
            {
                var now = DateTime.Now;
                var time = (long)(now - last).TotalMilliseconds;
                if (time >= 500)
                {
                    var c = CurrentProgress;
                    var newspeedvalue = Math.Max(0, (c - LastSpeedProgress) * 1000 / time);
                    lastspeedvalue = Math.Abs(newspeedvalue - lastspeedvalue) < 4 ? newspeedvalue : (newspeedvalue + lastspeedvalue) / 2;
                    last = now;
                    LastSpeedProgress = c;
                }
                return lastspeedvalue;
            }
        }

        private string[] target;
        private bool dirmode;
        public NukeTask(IEnumerable<string> target, bool dirmode)
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
                var todelete = new List<string>();
                if (dirmode)
                {
                    foreach (var dir in target)
                    {
                        FindEmptyDirsRecursive(new ZlpDirectoryInfo(dir), todelete);
                        if (todelete.Contains(dir))
                        {
                            todelete.Remove(dir);
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
                        CurrentName = item;
                        DoStatus(false);
                        if (dirmode)
                        {
                            if (ZlpIOHelper.DirectoryExists(item))
                            {
                                ZlpIOHelper.DeleteDirectory(item, true);
                            }
                        }
                        else
                        {
                            if (ZlpIOHelper.FileExists(item))
                            {
                                ZlpIOHelper.DeleteFile(item);
                            }
                        }
                    }
                    catch
                    {

                    }
                    CurrentItems++;
                    CurrentProgress = CurrentItems;
                }
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

        private bool FindEmptyDirsRecursive(ZlpDirectoryInfo Dir, List<string> emptydirs)
        {
            if (IsCancelled) { return false; }
            var empty = true;
            foreach (var subdir in Dir.GetDirectories())
            {
                empty &= FindEmptyDirsRecursive(subdir, emptydirs);
            }
            if (empty && Dir.GetFiles().Length == 0)
            {
                emptydirs.Add(Dir.FullName);
                return true;
            }
            return false;
        }

        private void FindEmptyFilesRecursive(ZlpDirectoryInfo Dir, List<string> emptyfiles)
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
                    emptyfiles.Add(file.FullName);
                }
            }
        }
    }
}
