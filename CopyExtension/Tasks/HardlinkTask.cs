using CopyExtension.Helpers;
using System.Runtime.InteropServices;

namespace CopyExtension.Tasks;

internal class HardlinkTask : CopyTask
{
    public override long SpeedSmoothingTolerance => 4;
    public override string CurrentSpeedUnit => "items";

    private string[] sourcefolders;
    private string target;

    public HardlinkTask(string[] sourcefolders, string target, Filter[]? filters) : base(filters)
    {
        this.sourcefolders = sourcefolders.Select(f => f.TrimEnd('\\')).ToArray();
        this.target = target.TrimEnd('\\');
        this.ReadingVolume = PathExt.GetVolume(this.sourcefolders.First());
        this.WritingVolume = PathExt.GetVolume(this.target);
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
            var sources = Discover();
            sources.RemoveAll(i => !MatchesFilters(i.Source));
            this.TotalItems = sources.Count;
            this.TotalProgress = sources.Count;
            DoStatus(true);
            if (CheckCancel()) { return; }
            foreach (var item in sources)
            {
                if (File.Exists(item.Source))
                {
                    var same = false;
                    while (!same && !IsCancelled)
                    {
                        if (CheckCancel()) { return; }
                        try
                        {
                            CurrentAction = "Linking";
                            CurrentName = Path.GetFileName(item.Source);
                            DoStatus(false);
                            if (File.Exists(item.Target))
                            {
                                FileExt.DeleteSafe(item.Target);
                            }
                            DirExt.CreateSafe(Path.GetDirectoryName(item.Target));
                            same = CreateHardLink(IOHelper.CheckAddLongPathPrefix(item.Target), IOHelper.CheckAddLongPathPrefix(item.Source), IntPtr.Zero);
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
                            CurrentName = $"{item.Source}:{e.GetType().Name}: {e.Message}";
                            DoStatus(true);
                            Thread.Sleep(5000);
                        }
                    }
                }
                else if (Directory.Exists(item.Source))
                {
                    DirExt.CreateSafe(item.Target);
                    CurrentItems++;
                    CurrentProgress++;
                    DoStatus(false);
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
            DoComplete();
        }
    }

    private List<FileJob> Discover()
    {
        List<FileJob> dirs = [];
        List<FileJob> files = [];
        foreach (var source in sourcefolders)
        {
            GetFiles(source, Path.GetDirectoryName(source), target, files, dirs);
        }
        dirs.AddRange(files);
        return dirs;
    }

    private void GetFiles(string fileordir, string? source, string target, List<FileJob> files, List<FileJob> dirs)
    {
        if (Directory.Exists(fileordir))
        {
            GetFilesRecursive(fileordir, source, target, files, dirs);
        }
        else if (File.Exists(fileordir))
        {
            files.Add(new FileJob(fileordir, Replacedir(fileordir, source, target), true));
        }
    }

    private void GetFilesRecursive(string dir, string? source, string target, List<FileJob> files, List<FileJob> dirs)
    {
        try
        {
            foreach (var subdir in Directory.GetDirectories(dir))
            {
                GetFilesRecursive(subdir, source, target, files, dirs);
            }
            foreach (var file in Directory.GetFiles(dir))
            {
                files.Add(new FileJob(file, Replacedir(file, source, target), true));
            }
            dirs.Add(new FileJob(dir, Replacedir(dir, source, target), false));
        }
        catch { }//missing permissions doesn't concern us for now
    }

    private class FileJob(string Source, string Target, bool IsFile)
    {
        public string Source { get; set; } = Source;

        public string Target { get; set; } = Target;
        public bool IsFile { get; } = IsFile;
    }

    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool CreateHardLink(
      string lpFileName,
      string lpExistingFileName,
      IntPtr lpSecurityAttributes
      );
}
