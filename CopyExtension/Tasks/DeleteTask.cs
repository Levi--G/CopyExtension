namespace CopyExtension.Tasks;

internal class DeleteTask : CopyTask
{
    public override string FullAction => $"{Action} on {WritingVolume}";

    public override long SpeedSmoothingTolerance => 4;
    public override string CurrentSpeedUnit => "items";

    private string[] target;
    public DeleteTask(IEnumerable<string> target, Filter[] filters) : base(filters)
    {
        this.target = target.Select(f => f.TrimEnd('\\')).ToArray();
        this.WritingVolume = Path.GetPathRoot(this.target.First())!.TrimEnd('\\');
        this.Action = "Delete";
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
            foreach (var fd in target)
            {
                if (Directory.Exists(fd))
                {
                    FindItemsRecursive(fd, todelete);
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
                    if (Directory.Exists(item))
                    {
                        try
                        {
                            Directory.Delete(item, true);
                        }
                        catch { }
                    }
                    else
                    {
                        try
                        {
                            File.Delete(item);
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
            DoComplete();
        }
    }

    private void FindItemsRecursive(string Dir, List<string> items)
    {
        if (IsCancelled) { return; }
        try
        {
            foreach (var subdir in Directory.GetDirectories(Dir))
            {
                items.Add(subdir);
                FindItemsRecursive(subdir, items);
            }
            foreach (var file in Directory.GetFiles(Dir))
            {
                items.Add(file);
            }
        }
        catch { }
    }
}
