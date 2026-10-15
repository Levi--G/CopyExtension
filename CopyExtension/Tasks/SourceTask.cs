using CopyExtension.Helpers;

namespace CopyExtension.Tasks;

public class SourceTask : CopyTask
{
    public override string FullAction => $"{Action} on {WritingVolume}";

    public override long SpeedSmoothingTolerance => 4;
    public override string CurrentSpeedUnit => "items";

    private string[] target;
    public SourceTask(IEnumerable<string> target, Filter[]? filters) : base(filters)
    {
        this.target = target.Select(f => f.TrimEnd('\\')).Where(t => Directory.Exists(t)).ToArray();
        this.WritingVolume = PathExt.GetVolume(this.target.First());
        this.Action = "Cleaning source";
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
            foreach (var dir in target)
            {
                FindToDeleteRecursive(dir, todelete);
                if (todelete.Contains(dir))
                {
                    todelete.Remove(dir);
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
                    DirExt.DeleteSafe(item);
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

    readonly string[] csexts = { ".cs", ".csproj", ".sln" };
    private void FindToDeleteRecursive(string Dir, List<string> todelete)
    {
        if (IsCancelled) { return; }
        bool iscs = Directory.EnumerateFiles(Dir).Any(f => csexts.Contains(Path.GetExtension(f).ToLower()));
        bool ispio = Directory.EnumerateFiles(Dir).Any(f => Path.GetFileName(f).ToLower() == "platformio.ini");
        foreach (var subdir in Directory.EnumerateDirectories(Dir))
        {
            string name = Path.GetFileName(subdir).ToLower();
            if (iscs && (name == "bin" || name == "obj" || name == ".vs"))
            {
                todelete.Add(subdir);
                continue;
            }
            if (ispio && name == ".pio")
            {
                todelete.Add(subdir);
                continue;
            }
            FindToDeleteRecursive(subdir, todelete);
        }
    }
}
