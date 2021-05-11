using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ZetaLongPaths;

namespace CopyExtension
{
    internal class FileJob
    {
        public const string COPYING = "Copying";
        public const string COMPARING = "Comparing";
        public const string DELETING = "Deleting";
        public const string MOVING = "Deleting";
        //const string ERRORED = "Errored";

        private const int BUFFERSIZE = 1024 * 1024; //1M for fast or 81920 for default .net
        private Func<bool> checkCancel;

        public FileJob(IZlpFileSystemInfo Source, IZlpFileSystemInfo Target, CopyJobType CopyType, Func<bool> checkCancel)
        {
            this.Source = Source;
            this.Target = Target;
            this.CopyType = CopyType;
            this.checkCancel = checkCancel;
            FileSize = (Source as ZlpFileInfo)?.Length ?? (Target as ZlpFileInfo)?.Length ?? 1;
            TotalProgress = (CopyType == CopyJobType.Copy || CopyType == CopyJobType.Move) ? FileSize * 2 : FileSize;
            IsFile = Source is ZlpFileInfo;
        }

        public event Action<string> OnAction;
        public event Action OnProgress;

        public IZlpFileSystemInfo Source { get; set; }

        public IZlpFileSystemInfo Target { get; set; }

        public ZlpFileInfo SourceFile => Source as ZlpFileInfo;

        public ZlpFileInfo TargetFile => Target as ZlpFileInfo;

        public CopyJobType CopyType { get; set; }

        public ExistsAction ExistsAction { get; set; } = ExistsAction.None;

        public long FileSize { get; }

        public long TotalProgress { get; }

        public long CurrentProgress { get; private set; }

        public bool FileCreated { get; private set; }

        public string Name => Source.Name;

        public bool IsFile { get; }

        private bool CheckCancel()
        {
            if (checkCancel())
            {
                Cleanup();
                return true;
            }
            return false;
        }

        private void Cleanup()
        {
            if (FileCreated && Target is ZlpFileInfo target)
            {
                try
                {
                    target.Delete();
                }
                catch { }
            }
        }

        public FileResult Copy()
        {
            FileResult result = null;
            for (int i = 0; i < 3; i++)
            {
                result = FileCopy();
                if (result.Success)
                {
                    CurrentProgress = TotalProgress;
                    OnProgress?.Invoke();
                    break;
                }
            }
            if (result?.Success != true)
            {
                Cleanup();
            }
            return result;
        }

        private void ProgressP1(long p)
        {
            CurrentProgress = Math.Min(p, TotalProgress);
            OnProgress?.Invoke();
        }

        private void ProgressP2(long p)
        {
            CurrentProgress = Math.Min(p + FileSize, TotalProgress);
            OnProgress?.Invoke();
        }

        private FileResult FileCopy()
        {
            try
            {
                if (CheckCancel())
                {
                    return new FileResult() { Job = this, Success = false, Reason = $"Cancelled by user" };
                }
                if (IsFile)
                {
                    if (!FileCreated)
                    {
                        SourceFile.Refresh();
                        TargetFile.Refresh();
                        //FastResume(sourcefile, targetfile, null, BUFFERSIZE);
                        if ((CopyType == CopyJobType.Copy || CopyType == CopyJobType.Move) && Target.Exists)
                        {
                            switch (ExistsAction)
                            {
                                case ExistsAction.Overwrite:
                                case ExistsAction.OverwriteFix:
                                    break;
                                case ExistsAction.Skip:
                                    return new FileResult() { Job = this, Success = true, Reason = "Skipped" };
                                case ExistsAction.Rename:
                                    Rename();
                                    break;
                                case ExistsAction.None:
                                default:
                                    return new FileResult() { Job = this, Success = false, IsOverWriteError = true, Reason = "File already exists" };
                            }
                        }
                    }
                    var sourcefile = SourceFile;
                    var targetfile = TargetFile;
                    if (CopyType == CopyJobType.Copy || CopyType == CopyJobType.Move)
                    {
                        FileCreated = true;
                        OnAction?.Invoke(COPYING);
                        FastCopy(sourcefile, targetfile, ProgressP1, BUFFERSIZE);
                        OnAction?.Invoke(COMPARING);
                        if (Compare(sourcefile, targetfile, ProgressP2, BUFFERSIZE))
                        {
                            if (CopyType == CopyJobType.Move)
                            {
                                OnAction?.Invoke(DELETING);
                                sourcefile.Delete();
                            }
                            return new FileResult() { Job = this, Success = true };
                        }
                        else
                        {
                            return new FileResult() { Job = this, Success = false, Reason = "Compare NOK" };
                        }
                    }
                    if (CopyType == CopyJobType.Compare)
                    {
                        OnAction?.Invoke(COMPARING);
                        if (Compare(sourcefile, targetfile, ProgressP1, BUFFERSIZE))
                        {
                            targetfile.Delete();
                        }
                        return new FileResult() { Job = this, Success = true };
                    }
                }
                else if (Target is ZlpDirectoryInfo td)
                {
                    td.Create();
                    return new FileResult() { Job = this, Success = true };
                }
                return new FileResult() { Job = this, Success = false };
            }
            catch (Exception e)
            {
                return new FileResult() { Job = this, Success = false, Reason = $"{e.GetType().Name}: {e.Message}" };
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
                    progresscallback?.Invoke(size);
                    read = source.Read(swap ? buffer : buffer2, 0, bufferSize);
                    writer?.Wait();
                    writer = dest.WriteAsync(swap ? buffer : buffer2, 0, read);
                    swap = !swap;
                }
                writer?.Wait();
            }
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
                    progresscallback?.Invoke(total);
                    lastcomp?.Wait();
                    if (lastcomp != null && !lastcomp.Result) { return false; }
                    lastcomp = ByteArrayCompareAsync(swap ? buffer1 : buffer3, swap ? buffer2 : buffer4, b1.Result);
                }
                lastcomp?.Wait();
                if (lastcomp != null && !lastcomp.Result) { return false; }
            }
            return true;
        }

        //private void FastResume(ZlpFileInfo file, ZlpFileInfo destination, Action<long> progresscallback, int bufferSize)
        //{
        //    byte[] buffer = new byte[bufferSize], buffer2 = new byte[bufferSize];
        //    byte[] buffer3 = new byte[bufferSize], buffer4 = new byte[bufferSize];
        //    bool swap = false;
        //    int read;
        //    long len = file.Length;
        //    Task writer = null;
        //    Task<bool> lastcomp = null;

        //    using (var source = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan))
        //    using (var dest = new FileStream(destination.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write, bufferSize, FileOptions.SequentialScan))
        //    {
        //        dest.SetLength(source.Length);
        //        bool writing = false;
        //        for (long size = 0; size < len; size += read)
        //        {
        //            if (CheckCancel()) { return; }
        //            progresscallback?.Invoke(size);
        //            if (!writing)
        //            {
        //                swap = !swap;
        //                var b1 = source.ReadAsync(swap ? buffer : buffer3, 0, bufferSize);
        //                var b2 = dest.ReadAsync(swap ? buffer2 : buffer4, 0, bufferSize);
        //                Task.WhenAll(b1, b2).Wait();
        //                if ((read = b1.Result) != b2.Result) { return false; }
        //                lastcomp?.Wait();
        //                if (lastcomp != null && !lastcomp.Result) { return false; }
        //                lastcomp = ByteArrayCompareAsync(swap ? buffer : buffer3, swap ? buffer2 : buffer4, b1.Result);
        //            }
        //            else
        //            {
        //                read = source.Read(swap ? buffer : buffer2, 0, bufferSize);
        //                writer?.Wait();
        //                writer = dest.WriteAsync(swap ? buffer : buffer2, 0, read);
        //                swap = !swap;
        //            }
        //        }
        //        lastcomp?.Wait();
        //        if (lastcomp != null && !lastcomp.Result) { return false; }
        //        writer?.Wait();
        //    }
        //}

        private void Rename()
        {
            var path = Target.FullName;
            var dir = ZlpPathHelper.GetDirectoryPathNameFromFilePath(path);
            var name = ZlpPathHelper.GetFileNameWithoutExtension(path);
            var ext = ZlpPathHelper.GetExtension(path);
            for (int i = 1; i < 100; i++)
            {
                var n = ZlpPathHelper.Combine(dir, $"{name}({i}){ext}");
                if (!ZlpIOHelper.FileExists(n))
                {
                    Target = new ZlpFileInfo(n);
                    return;
                }
            }
            Target = null;
        }

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int memcmp(byte[] b1, byte[] b2, long count);

        private static Task<bool> ByteArrayCompareAsync(byte[] b1, byte[] b2, long count)
        {
            return Task.Factory.StartNew(() => memcmp(b1, b2, count) == 0);
        }
    }

    internal enum CopyJobType
    {
        Copy, Move, Compare
    }

    internal enum ExistsAction
    {
        None, Overwrite, OverwriteFix, Skip, Rename
    }

    internal class FileResult
    {
        public FileJob Job { get; set; }

        public bool Success { get; set; }

        public bool IsOverWriteError { get; set; }

        public string Reason { get; set; }
    }
}
