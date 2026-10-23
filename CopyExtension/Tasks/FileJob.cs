using CopyExtension.Helpers;

namespace CopyExtension.Tasks;

internal class FileJob
{
    public const string COPYING = "Copying";
    public const string COMPARING = "Comparing";
    public const string DELETING = "Deleting";
    public const string MOVING = "Moving";
    //const string ERRORED = "Errored";

    private const int BUFFERSIZE = 1024 * 1024; //1M for fast or 81920 for default .net
    private Func<bool> checkCancel;

    public FileJob(string Source, string Target, bool isFile, CopyJobType CopyType, Func<bool> checkCancel)
    {
        this.Source = Source;
        this.Target = Target;
        this.CopyType = CopyType;
        this.checkCancel = checkCancel;
        IsFile = isFile;
        FileSize = GetLengthSafe(Source, Target, isFile);
        TotalProgress = CopyType.IsSafeMoveOrCopy() ? FileSize * 2 : FileSize;
    }

    static long GetLengthSafe(string fileordir, string fileordir2, bool isFile)
    {
        if (isFile)
        {
            try
            {
                var f = new FileInfo(fileordir);
                if (f.Exists)
                {
                    return f.Length;
                }
            }
            catch { }
            try
            {
                var f = new FileInfo(fileordir2);
                if (f.Exists)
                {
                    return f.Length;
                }
            }
            catch { }
        }
        return 4;
    }

    public event Action<string>? OnAction;
    public event Action? OnProgress;

    public string Source { get; set; }

    public string Target { get; set; }

    public CopyJobType CopyType { get; set; }

    public ExistsAction ExistsAction { get; set; } = ExistsAction.None;

    public long FileSize { get; }

    public long TotalProgress { get; }

    public long CurrentProgress { get; private set; }

    public bool FileCreated { get; private set; }

    public string Name => Path.GetFileName(Source);

    public bool IsFile { get; }

    public void ResetProgress()
    {
        CurrentProgress = 0;
    }

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
        if (FileCreated && CopyType.IsAnyMoveOrCopy() && IsFile && File.Exists(Target))
        {
            try
            {
                File.Delete(Target);
            }
            catch { }
        }
        ResetProgress();
    }

    public FileResult Copy()
    {
        FileResult? result = null;
        for (int i = 0; i < 3; i++)
        {
            result = FileCopy();
            if (result.Success)
            {
                CurrentProgress = TotalProgress;
                OnProgress?.Invoke();
                return result;
            }
        }
        if (result?.Success != true)
        {
            Cleanup();
        }
        return result ?? new() { Success = false, Job = this, Reason = "Error" };
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

    private void ProgressFull(long p)
    {
        //we faking the double speed to make the progressbar fill up
        CurrentProgress = Math.Min(p * 2, TotalProgress);
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
                    if ((CopyType == CopyJobType.Copy || CopyType == CopyJobType.Move || CopyType == CopyJobType.CopyUnsafe || CopyType == CopyJobType.MoveUnsafe) && File.Exists(Target))
                    {
                        switch (ExistsAction)
                        {
                            case ExistsAction.Compare:
                                {
                                    if (Compare(ProgressFull, false))
                                    {
                                        return new() { Job = this, Success = true, Reason = "Files are identical" };
                                    }
                                    else
                                    {
                                        ResetProgress();
                                        return new() { Job = this, Success = false, IsOverWriteError = true, Reason = "File already exists" };
                                    }
                                }
                            case ExistsAction.Overwrite:
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
                if (CopyType == CopyJobType.CopyUnsafe)
                {
                    FileCreated = true;
                    OnAction?.Invoke(COPYING);
                    try
                    {
                        DirExt.CreateSafe(Path.GetDirectoryName(Target));
                        File.Copy(Source, Target, true);
                        TryCloneDates();
                        return new FileResult() { Job = this, Success = true };
                    }
                    catch { }
                }
                if (CopyType == CopyJobType.MoveUnsafe)
                {
                    FileCreated = true;
                    OnAction?.Invoke(COPYING);
                    try
                    {
                        DirExt.CreateSafe(Path.GetDirectoryName(Target));
                        File.Move(Source, Target, true);
                        TryCloneDates();
                        return new FileResult() { Job = this, Success = true };
                    }
                    catch { }
                }
                if (CopyType == CopyJobType.Copy || CopyType == CopyJobType.Move)
                {
                    FileCreated = true;
                    OnAction?.Invoke(COPYING);
                    DirExt.CreateSafe(Path.GetDirectoryName(Target));
                    if (!FastCopy())
                    {
                        return new FileResult() { Job = this, Success = false, Reason = "Copy Failed" };
                    }
                    TryCloneDates();
                    OnAction?.Invoke(COMPARING);
                    if (Compare(ProgressP2, true))
                    {
                        if (CopyType == CopyJobType.Move)
                        {
                            OnAction?.Invoke(DELETING);
                            if (!FileExt.DeleteSafe(Source))
                            {
                                return new FileResult() { Job = this, Success = false, Reason = "Delete Failed" };
                            }
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
                    if (Compare(ProgressP1, false))
                    {
                        if (!FileExt.DeleteSafe(Target))
                        {
                            return new FileResult() { Job = this, Success = false, Reason = "Delete Failed" };
                        }
                    }
                    return new FileResult() { Job = this, Success = true };
                }
                if (CopyType == CopyJobType.CompareNames)
                {
                    OnAction?.Invoke(COMPARING);
                    if (!FileExt.DeleteSafe(Target))
                    {
                        return new FileResult() { Job = this, Success = false, Reason = "Delete Failed" };
                    }
                    return new FileResult() { Job = this, Success = true };
                }
            }
            else
            {
                if (CopyType.IsAnyMoveOrCopy())
                {
                    DirExt.CreateSafe(Target);
                    return new FileResult() { Job = this, Success = true };
                }
                else
                {
                    try
                    {
                        if (Directory.Exists(Target))
                        {
                            if (!Directory.EnumerateFileSystemEntries(Target).Any())
                            {
                                DirExt.DeleteSafe(Target, false);
                            }
                        }
                    }
                    catch { }
                }
            }
            return new FileResult() { Job = this, Success = false };
        }
        catch (Exception e)
        {
            return new FileResult() { Job = this, Success = false, Reason = $"{e.GetType().Name}: {e.Message}" };
        }
    }

    private bool FastCopy()
    {
        if (CheckCancel()) { return false; }
        try
        {
            byte[] buffer = new byte[BUFFERSIZE], buffer2 = new byte[BUFFERSIZE];
            bool swap = false;
            int read = 0;
            long len = new FileInfo(Source).Length;
            Task? writer = null;
            using (var source = new FileStream(Source, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFERSIZE, FileOptions.SequentialScan))
            using (var dest = new FileStream(Target, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write, BUFFERSIZE, FileOptions.SequentialScan))
            {
                dest.SetLength(source.Length);
                long size = 0;
                for (; size < len; size += read)
                {
                    if (CheckCancel()) { return false; }
                    ProgressP1(size);
                    read = source.ReadAtLeast(swap ? buffer : buffer2, BUFFERSIZE, false);
                    writer?.Wait();
                    if (read == 0) { break; }
                    writer = dest.WriteAsync(swap ? buffer : buffer2, 0, read);
                    swap = !swap;
                }
                writer?.Wait();
                if (size < len)
                {
                    Program.Logger.Log(this, $"FastCopy failed: {size} < {len}");
                    return false;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            Program.Logger.Log(this, $"FastCopy failed with exception:");
            Program.Logger.Log(this, ex);
        }
        return false;
    }

    private bool Compare(Action<long>? progresscallback, bool checkExists)
    {
        if (CheckCancel()) { return false; }
        if (checkExists)
        {
            for (int i = 0; i < 50; i++)
            {
                if (File.Exists(Source) && File.Exists(Target))
                {
                    break;
                }
                Thread.Sleep(100);
            }
        }
        if (!File.Exists(Source) || !File.Exists(Target)) { return false; }
        long len = new FileInfo(Source).Length;
        if (len != new FileInfo(Target).Length) { return false; }
        byte[] buffer1 = new byte[BUFFERSIZE];
        byte[] buffer2 = new byte[BUFFERSIZE];
        using (var s = new FileStream(Source, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFERSIZE, FileOptions.SequentialScan))
        using (var s2 = new FileStream(Target, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFERSIZE, FileOptions.SequentialScan))
        {
            long total = 0;
            int read = 1;
            while (read != 0 && total <= len)
            {
                if (CheckCancel()) { return false; }
                var b1 = s.ReadAtLeastAsync(buffer1, BUFFERSIZE, false);
                var b2 = s2.ReadAtLeastAsync(buffer2, BUFFERSIZE, false);
                read = b1.GetResultSync();
                total += read;
                progresscallback?.Invoke(total);
                if (read != b2.GetResultSync()) { return false; } // this should never happen btw
                if (!buffer1.AsSpan()[..read].SequenceEqual(buffer2.AsSpan()[..read])) { return false; }
            }
            if (total != len) { return false; }
        }
        return true;
    }

    private void TryCloneDates()
    {
        try
        {
            DateTime creationTime = File.GetCreationTimeUtc(Source);
            DateTime lastAccessTime = File.GetCreationTimeUtc(Source);
            DateTime lastWriteTime = File.GetCreationTimeUtc(Source);
            if (creationTime > DateTime.MinValue)
            {
                File.SetCreationTimeUtc(Target, creationTime);
            }
            if (lastAccessTime > DateTime.MinValue)
            {
                File.SetLastAccessTimeUtc(Target, lastAccessTime);
            }
            if (lastWriteTime > DateTime.MinValue)
            {
                File.SetLastWriteTimeUtc(Target, lastWriteTime);
            }
        }
        catch { }
    }

    private void Rename()
    {
        var path = Target;
        var dir = Path.GetDirectoryName(path) ?? throw new Exception("Dir not found");
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        for (int i = 1; i < 100; i++)
        {
            var n = Path.Combine(dir, $"{name}({i}){ext}");
            if (!File.Exists(n))
            {
                Target = n;
                return;
            }
        }
        throw new Exception("Could not find a unique name to rename the file");
    }
}

public enum CopyJobType
{
    Copy, Move, Compare, CopyUnsafe, MoveUnsafe, CompareNames
}

public enum ExistsAction
{
    None, Overwrite, Skip, Rename, Compare
}

internal class FileResult
{
    public required FileJob Job { get; set; }

    public required bool Success { get; set; }

    public bool IsOverWriteError { get; set; }

    public string? Reason { get; set; }
}
