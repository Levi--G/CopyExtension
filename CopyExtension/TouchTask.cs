using System;
using System.Collections.Generic;
//using System.IO;
using System.Linq;
using System.Threading;
using ZetaLongPaths;

namespace CopyExtension
{
    internal class TouchTask : CopyTask
    {
        public override string FullAction => $"{Action} on {WritingVolume}";

        public override long SpeedSmoothingTolerance => 4;
        public override string CurrentSpeedUnit => "items";

        private string[] target;
        private bool dirmode;
        public TouchTask(IEnumerable<string> target, bool dirmode, Filter[] filters) : base(filters)
        {
            this.target = target.Select(f => f.TrimEnd('\\')).ToArray();
            this.dirmode = dirmode;
            this.WritingVolume = ZlpPathHelper.GetPathRoot(this.target.First()).TrimEnd('\\');
            this.Action = "Creating " + (dirmode ? "folders" : "files");
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
                CurrentAction = Action;
                this.TotalItems = target.Length;
                this.TotalProgress = TotalItems;
                DoStatus(true);
                foreach (var item in target)
                {
                    if (CheckCancel()) { return; }
                    try
                    {
                        if (dirmode)
                        {
                            var d = new ZlpDirectoryInfo(item);
                            d.SafeCheckCreate();
                        }
                        else
                        {
                            var f = new ZlpFileInfo(item);
                            if (!f.Exists)
                            {
                                f.Directory.SafeCheckCreate();
                                f.WriteAllBytes(new byte[0]);
                            }
                        }
                    }
                    catch { }
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
                IsDone = true;
                DoComplete();
            }
        }
    }
}
