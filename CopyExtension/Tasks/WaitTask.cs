namespace CopyExtension.Tasks
{
    internal class WaitTask : CopyTask
    {
        private readonly int seconds;

        public WaitTask(int seconds, string? readingvolume, string? writingvolume, Filter[]? filters) : base(filters)
        {
            this.seconds = seconds;
            this.Action = "Waiting";
            ReadingVolume = readingvolume;
            WritingVolume = writingvolume;
        }

        public override string CurrentSpeedUnit => "s";

        public override long SpeedSmoothingTolerance => 1;
        public override string FullAction => $"Waiting";

        public override void Start()
        {
            new Thread(DoWork).Start();
            base.Start();
        }

        private void DoWork()
        {
            try
            {
                CurrentAction = Action;
                this.TotalItems = seconds;
                this.TotalProgress = seconds;
                DoStatus(true);
                for (int i = 0; i < seconds && !CheckCancel(); i++)
                {
                    Thread.Sleep(1000);
                    this.CurrentItems = Math.Min(CurrentItems + 1, TotalItems);
                    CurrentProgress = CurrentItems;
                    DoStatus(false);
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
    }
}
