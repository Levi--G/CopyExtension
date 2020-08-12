using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ZetaLongPaths;

namespace CopyExtension
{
    internal class CopyMoveTask : CopyTask
    {
        private const int BUFFERSIZE = 1024 * 1024; //81920

        private DateTime last;
        private long LastSpeedProgress;
        private long lastspeedvalue;
        public override string CurrentSpeed => $"{TaskControl.GetHumanReadable(CurrentSpeedValue)}/s";

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
                    lastspeedvalue = Math.Abs(newspeedvalue - lastspeedvalue) < 10 * 1024 * 1024 ? newspeedvalue : (newspeedvalue + lastspeedvalue) / 2;
                    last = now;
                    LastSpeedProgress = c;
                }
                return lastspeedvalue;
            }
        }

        private string[] sourcefolders;
        private string target;
        private bool move;
        private bool compare;

        public CopyMoveTask(string[] sourcefolders, string target, bool move = false, bool compare = false)
        {
            this.sourcefolders = sourcefolders.Select(f => f.TrimEnd('\\')).ToArray();
            this.target = target.TrimEnd('\\');
            this.move = move;
            this.compare = compare;
            this.ReadingVolume = ZlpPathHelper.GetPathRoot(this.sourcefolders.First()).TrimEnd('\\');
            this.WritingVolume = ZlpPathHelper.GetPathRoot(this.target).TrimEnd('\\');
            this.Action = move ? "Moving" : compare ? "Comparing" : "Copying";
        }

        public override void Start()
        {
            new Thread(DoWork).Start();
            base.Start();
        }

        private bool CheckCancel(ZlpFileInfo targetfile)
        {
            if (CheckCancel())
            {
                if (!compare)
                {
                    try
                    {
                        targetfile.Refresh();
                        if (targetfile.Exists)
                        {
                            targetfile.Delete();
                        }
                    }
                    catch { }
                }
                return true;
            }
            return false;
        }

        private void DoWork()
        {
            try
            {
                CurrentAction = "Discovery";
                DoStatus(true);
                var sources = sourcefolders.SelectMany(s => GetFiles(s, ZlpPathHelper.GetDirectoryPathNameFromFilePath(s), target)).ToList();
                {
                    var files = 0;
                    long total = 0;
                    foreach (var f in sources.Select(item => item.Source).OfType<ZlpFileInfo>())
                    {
                        files++;
                        total += f.Length;
                    }
                    if (!compare)
                    {
                        total *= 2;
                    }
                    this.TotalItems = files;
                    this.TotalProgress = total;
                }
                DoStatus(true);
                if (CheckCancel()) { return; }
                long lastcopy = 0;
                foreach (var item in sources)
                {
                    if (item.Source is ZlpFileInfo sourcefile && item.Target is ZlpFileInfo targetfile)
                    {
                        var length = sourcefile.Length;
                        var same = false;
                        while (!same && !IsCancelled)
                        {
                            if (CheckCancel()) { return; }
                            try
                            {
                                if (!compare)
                                {
                                    targetfile.Refresh();
                                    if (targetfile.Exists)
                                    {
                                        targetfile.Delete();
                                    }
                                    CurrentAction = "Copying";
                                    CurrentName = sourcefile.Name;
                                    FastCopy(sourcefile, targetfile, (current) => { CurrentProgress = lastcopy + current; DoStatus(false); }, BUFFERSIZE);
                                    //await CopyFileAsync(file.FullName, t.FullName);
                                }
                                CurrentProgress = lastcopy + length;
                                CurrentAction = "Comparing";
                                DoStatus(false);
                                if (CheckCancel(targetfile)) { return; }
                                Thread.Sleep(100);
                                for (int i = 0; i < 3; i++)
                                {
                                    try
                                    {
                                        same = Compare(sourcefile, targetfile, (current) => { CurrentProgress = lastcopy + length + current; DoStatus(false); }, BUFFERSIZE);
                                        break;
                                    }
                                    catch (Exception e)
                                    {
                                        CurrentAction = "Errored";
                                        CurrentName = $"Compare {sourcefile.Name}:{e.GetType().Name}: {e.Message}";
                                        DoStatus(true);
                                        Thread.Sleep(2000);
                                        CurrentAction = "Comparing";
                                        CurrentName = sourcefile.Name;
                                        DoStatus(true);
                                    }
                                }
                                if (same)
                                {
                                    if (move)
                                    {
                                        CurrentAction = "Deleting";
                                        while (ZlpIOHelper.FileExists(sourcefile.FullName))
                                        {
                                            try
                                            {
                                                sourcefile.Delete();
                                            }
                                            catch (Exception e)
                                            {
                                                CurrentAction = "Errored";
                                                CurrentName = $"Delete {sourcefile.Name}:{e.GetType().Name}: {e.Message}";
                                                DoStatus(true);
                                                Thread.Sleep(5000);
                                                CurrentAction = "Deleting";
                                                CurrentName = sourcefile.Name;
                                                DoStatus(true);
                                            }
                                        }
                                    }
                                    if (compare)
                                    {
                                        CurrentAction = "Deleting";
                                        while (ZlpIOHelper.FileExists(targetfile.FullName))
                                        {
                                            try
                                            {
                                                targetfile.Delete();
                                            }
                                            catch (Exception e)
                                            {
                                                CurrentAction = "Errored";
                                                CurrentName = $"Delete {sourcefile.Name}:{e.GetType().Name}: {e.Message}";
                                                DoStatus(true);
                                                Thread.Sleep(5000);
                                                CurrentAction = "Deleting";
                                                CurrentName = sourcefile.Name;
                                                DoStatus(true);
                                            }
                                        }
                                    }
                                }
                                if (same || compare)
                                {
                                    CurrentItems++;
                                    lastcopy += length;
                                    if (!compare)
                                    {
                                        lastcopy += length;
                                    }
                                    DoStatus(false);
                                }
                                else
                                {
                                    if (CheckCancel(targetfile)) { return; }
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
                    else if (item.Target is ZlpDirectoryInfo td && !compare)
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

        private void FastCopy(ZlpFileInfo file, ZlpFileInfo destination, Action<long> progresscallback, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize], buffer2 = new byte[bufferSize];
            bool swap = false;
            int read;
            long len = file.Length;
            Task writer = null;

            using (var source = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan))
            using (var dest = new FileStream(destination.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write, bufferSize, FileOptions.SequentialScan))
            {
                dest.SetLength(source.Length);
                for (long size = 0; size < len; size += read)
                {
                    if (CheckCancel()) { return; }
                    progresscallback(size);
                    read = source.Read(swap ? buffer : buffer2, 0, bufferSize);
                    writer?.Wait();
                    writer = dest.WriteAsync(swap ? buffer : buffer2, 0, read);
                    swap = !swap;
                }
                writer?.Wait();
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
                yield return new FileJob(file, new ZlpFileInfo(replacedir(f, source, target)));
            }
        }

        private IEnumerable<FileJob> GetFiles(ZlpDirectoryInfo dir, string source, string target)
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

        private string replacedir(string f, string source, string target)
        {
            return ZlpPathHelper.Combine(target + "\\", f.Replace(source, "").TrimStart('\\'));
        }

        private bool Compare(ZlpFileInfo f, ZlpFileInfo f2, Action<long> progresscallback, int buffersize)
        {
            for (int i = 0; i < 50; i++)
            {
                f.Refresh();
                f2.Refresh();
                if (f.Exists && f2.Exists)
                {
                    break;
                }
                Thread.Sleep(100);
            }
            if (!f.Exists || !f2.Exists) { return false; }
            if (f.Length != f2.Length) { return false; }
            byte[] buffer1 = new byte[buffersize];
            byte[] buffer2 = new byte[buffersize];
            byte[] buffer3 = new byte[buffersize];
            byte[] buffer4 = new byte[buffersize];
            using (var s = new FileStream(f.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, buffersize, FileOptions.SequentialScan))
            using (var s2 = new FileStream(f2.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, buffersize, FileOptions.SequentialScan))
            {
                long total = 0;
                int read = buffersize;
                Task<bool> lastcomp = null;
                bool swap = false;
                while (read == buffersize)
                {
                    if (CheckCancel()) { return false; }
                    swap = !swap;
                    var b1 = s.ReadAsync(swap ? buffer1 : buffer3, 0, buffersize);
                    var b2 = s2.ReadAsync(swap ? buffer2 : buffer4, 0, buffersize);
                    Task.WhenAll(b1, b2).Wait();
                    if ((read = b1.Result) != b2.Result) { return false; }
                    total += read;
                    progresscallback(total);
                    lastcomp?.Wait();
                    if (lastcomp != null && !lastcomp.Result) { return false; }
                    lastcomp = ByteArrayCompareAsync(swap ? buffer1 : buffer3, swap ? buffer2 : buffer4, b1.Result);
                }
                lastcomp?.Wait();
                if (lastcomp != null && !lastcomp.Result) { return false; }
            }
            return true;
        }

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int memcmp(byte[] b1, byte[] b2, long count);

        private static Task<bool> ByteArrayCompareAsync(byte[] b1, byte[] b2, long count)
        {
            return Task.Factory.StartNew(() => memcmp(b1, b2, count) == 0);
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
    }
}