using System;
using System.Collections.Generic;
//using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZetaLongPaths;

namespace CopyExtension
{
    class HardlinkTask : CopyTask
    {
        DateTime last;
        long LastSpeedProgress;
        long lastspeedvalue;
        public override string CurrentSpeed => $"{lastspeedvalue} items/s";

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

        string[] sourcefolders;
        string target;

        public HardlinkTask(string[] sourcefolders, string target)
        {
            this.sourcefolders = sourcefolders.Select(f => f.TrimEnd('\\')).ToArray();
            this.target = target.TrimEnd('\\');
            //this.ReadingVolume = Path.GetPathRoot(this.sourcefolders.First()).TrimEnd('\\');
            //this.WritingVolume = Path.GetPathRoot(this.target).TrimEnd('\\');
            this.ReadingVolume = ZlpPathHelper.GetPathRoot(this.sourcefolders.First()).TrimEnd('\\');
            this.WritingVolume = ZlpPathHelper.GetPathRoot(this.target).TrimEnd('\\');
            this.Action = "Hardlinking";
        }

        public override void Start()
        {
            new Thread(DoWork).Start();
            base.Start();
        }

        void DoWork()
        {
            try
            {
                CurrentAction = "Discovery";
                DoStatus(true);
                //var sources = sourcefolders.SelectMany(s => GetFiles(s, Path.GetDirectoryName(s), target)).ToList();
                var sources = sourcefolders.SelectMany(s => GetFiles(s, ZlpPathHelper.GetDirectoryPathNameFromFilePath(s), target)).ToList();
                {
                    //var files = sources.Select(item => item.Source).OfType<FileInfo>().Count();
                    var files = sources.Select(item => item.Source).OfType<ZlpFileInfo>().Count();
                    this.TotalItems = files;
                    this.TotalProgress = files;
                }
                DoStatus(true);
                if (CheckCancel()) { return; }
                foreach (var item in sources)
                {
                    //if (item.Source is FileInfo sourcefile && item.Target is FileInfo targetfile)
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
                                same = CreateHardLink(IOHelper.CheckAddLongPathPrefix(targetfile.FullName), IOHelper.CheckAddLongPathPrefix(sourcefile.FullName), IntPtr.Zero);
                                if (same)
                                {
                                    CurrentItems++;
                                    CurrentProgress++;
                                    DoStatus(false);
                                }
                                else
                                {

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
                    //else if (item.Target is DirectoryInfo td)
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

        //IEnumerable<FileJob> GetFiles(string f, string source, string target)
        //{
        //    if (Directory.Exists(f))
        //    {
        //        DirectoryInfo dir = new DirectoryInfo(f);
        //        foreach (var item in GetFiles(dir, source, target))
        //        {
        //            yield return item;
        //        }
        //    }
        //    else if (File.Exists(f))
        //    {
        //        FileInfo file = new FileInfo(f);
        //        yield return new FileJob(file, new FileInfo(replacedir(f, source, target)));
        //    }
        //}

        IEnumerable<FileJob> GetFiles(string f, string source, string target)
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
                yield return new FileJob(file, new ZlpFileInfo(replacedir(f, source, target)));
            }
        }

        //IEnumerable<FileJob> GetFiles(DirectoryInfo dir, string source, string target)
        //{
        //    yield return new FileJob(dir, new DirectoryInfo(replacedir(dir.FullName, source, target)));
        //    foreach (var file in dir.EnumerateFileSystemInfos())
        //    {
        //        yield return new FileJob(file, new FileInfo(replacedir(file.FullName, source, target)));
        //    }
        //    foreach (var subdir in dir.GetDirectories())
        //    {
        //        foreach (var item in GetFiles(subdir, source, target))
        //        {
        //            yield return item;
        //        }
        //    }
        //}

        IEnumerable<FileJob> GetFiles(ZlpDirectoryInfo dir, string source, string target)
        {
            yield return new FileJob(dir, new ZlpDirectoryInfo(replacedir(dir.FullName, source, target)));
            foreach (var file in dir.GetFiles())
            {
                yield return new FileJob(file, new ZlpFileInfo(replacedir(file.FullName, source, target)));
            }
            foreach (var subdir in dir.GetDirectories())
            {
                foreach (var item in GetFiles(subdir, source, target))
                {
                    yield return item;
                }
            }
        }

        string replacedir(string f, string source, string target)
        {
            //return Path.Combine(target + "\\", f.Replace(source, "").TrimStart('\\'));
            return ZlpPathHelper.Combine(target + "\\", f.Replace(source, "").TrimStart('\\'));
        }

        //class FileJob
        //{
        //    public FileJob(FileSystemInfo Source, FileSystemInfo Target)
        //    {
        //        this.Source = Source;
        //        this.Target = Target;
        //    }

        //    public FileSystemInfo Source { get; set; }

        //    public FileSystemInfo Target { get; set; }
        //}

        class FileJob
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
        static extern bool CreateHardLink(
          string lpFileName,
          string lpExistingFileName,
          IntPtr lpSecurityAttributes
          );
    }
}
