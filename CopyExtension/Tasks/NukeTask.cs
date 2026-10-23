using CopyExtension.Helpers;

namespace CopyExtension.Tasks;

public class NukeTask : CopyTask
{
    public override string FullAction => $"{Action} on {WritingVolume}";

    public override long SpeedSmoothingTolerance => 4;
    public override string CurrentSpeedUnit => "items";

    private string[] target;
    private bool dirmode;
    public NukeTask(IEnumerable<string> target, bool dirmode, Filter[]? filters) : base(filters)
    {
        this.target = target.Select(f => f.TrimEnd('\\')).Where(t => Directory.Exists(t)).ToArray();
        this.dirmode = dirmode;
        this.WritingVolume = Path.GetPathRoot(this.target.First())!.TrimEnd('\\');
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
                    FindEmptyDirsRecursive(dir, todelete);
                    todelete.Remove(dir);
                }
            }
            else
            {
                foreach (var dir in target)
                {
                    FindEmptyFilesRecursive(dir, todelete);
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
                    CurrentName = item;
                    DoStatus(false);
                    if (dirmode)
                    {
                        DirExt.DeleteSafe(item);
                    }
                    else
                    {
                        FileExt.DeleteSafe(item);
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
            DoComplete();
        }
    }

    private bool FindEmptyDirsRecursive(string Dir, List<string> emptydirs)
    {
        if (CheckCancel()) { return false; }
        var empty = true;
        try
        {
            foreach (var subdir in Directory.EnumerateDirectories(Dir))
            {
                empty &= FindEmptyDirsRecursive(subdir, emptydirs);
            }
            if (empty && DirExt.HasNoFiles(Dir))
            {
                emptydirs.Add(Dir);
                return true;
            }
        }
        catch { }
        return false;
    }

    private void FindEmptyFilesRecursive(string Dir, List<string> emptyfiles)
    {
        if (CheckCancel()) { return; }
        try
        {
            foreach (var subdir in Directory.EnumerateDirectories(Dir))
            {
                FindEmptyFilesRecursive(subdir, emptyfiles);
            }
            foreach (var file in Directory.EnumerateFiles(Dir))
            {
                if (file.Length == 0)
                {
                    emptyfiles.Add(file);
                }
            }
        }
        catch { }
    }
}
