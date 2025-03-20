using System;
using System.Collections.Generic;
//using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ZetaLongPaths;

namespace CopyExtension
{
    internal class HardlinkTask : CopyTask
    {
        public override long SpeedSmoothingTolerance => 4;
        public override string CurrentSpeedUnit => "items";

        private string[] sourcefolders;
        private string target;

        public HardlinkTask(string[] sourcefolders, string target, Filter[] filters) : base(filters)
        {
            this.sourcefolders = sourcefolders.Select(f => f.TrimEnd('\\')).ToArray();
            this.target = target.TrimEnd('\\');
            this.ReadingVolume = ZlpPathHelper.GetPathRoot(this.sourcefolders.First()).TrimEnd('\\');
            this.WritingVolume = ZlpPathHelper.GetPathRoot(this.target).TrimEnd('\\');
            this.Action = "Hardlinking";
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
                var sources = sourcefolders.SelectMany(s => GetFiles(s, ZlpPathHelper.GetDirectoryPathNameFromFilePath(s), target)).ToList();
                {
                    var files = sources.Select(item => item.Source).OfType<ZlpFileInfo>().Count();
                    this.TotalItems = files;
                    this.TotalProgress = files;
                }
                DoStatus(true);
                sources.RemoveAll(i => !MatchesFilters(i.Source));
                if (CheckCancel()) { return; }
                foreach (var item in sources)
                {
                    if (item.Source is ZlpFileInfo sourcefile && item.Target is ZlpFileInfo targetfile)
                    {
                        var same = false;
                        while (!same && !IsCancelled)
                        {
                            if (CheckCancel()) { return; }
                            try
                            {
                                CurrentAction = "Linking";
                                CurrentName = sourcefile.Name;
                                DoStatus(false);
                                targetfile.Refresh();
                                if (targetfile.Exists)
                                {
                                    targetfile.Delete();
                                }
                                targetfile.Directory.SafeCheckCreate();
                                same = CreateHardLink(IOHelper.CheckAddLongPathPrefix(targetfile.FullName), IOHelper.CheckAddLongPathPrefix(sourcefile.FullName), IntPtr.Zero);
                                if (same)
                                {
                                    CurrentItems++;
                                    CurrentProgress++;
                                    DoStatus(false);
                                }
                            }
                            catch (Exception e)
                            {
                                CurrentAction = "Errored";
                                CurrentName = $"{sourcefile.Name}:{e.GetType().Name}: {e.Message}";
                                DoStatus(true);
                                Thread.Sleep(5000);
                            }
                        }
                    }
                    else if (item.Target is ZlpDirectoryInfo td)
                    {
                        td.Create();
                    }
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
                yield return new FileJob(file, new ZlpFileInfo(Replacedir(f, source, target)));
            }
        }

        private IEnumerable<FileJob> GetFiles(ZlpDirectoryInfo dir, string source, string target)
        {
            yield return new FileJob(dir, new ZlpDirectoryInfo(Replacedir(dir.FullName, source, target)));
            foreach (var file in dir.GetFiles())
            {
                yield return new FileJob(file, new ZlpFileInfo(Replacedir(file.FullName, source, target)));
            }
            foreach (var subdir in dir.GetDirectories())
            {
                foreach (var item in GetFiles(subdir, source, target))
                {
                    yield return item;
                }
            }
        }

        private class FileJob
        {
            public FileJob(IZlpFileSystemInfo Source, IZlpFileSystemInfo Target)
            {
                this.Source = Source;
                this.Target = Target;
            }

            public IZlpFileSystemInfo Source { get; set; }

            public IZlpFileSystemInfo Target { get; set; }
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool CreateHardLink(
          string lpFileName,
          string lpExistingFileName,
          IntPtr lpSecurityAttributes
          );
    }
}
