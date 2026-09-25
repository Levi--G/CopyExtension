using System;
using System.Collections.Generic;
//using System.IO;
using System.Linq;
using System.Threading;
using ZetaLongPaths;

namespace CopyExtension
{
    public class SourceTask : CopyTask
    {
        public override string FullAction => $"{Action} on {WritingVolume}";

        public override long SpeedSmoothingTolerance => 4;
        public override string CurrentSpeedUnit => "items";

        private string[] target;
        public SourceTask(IEnumerable<string> target, Filter[] filters) : base(filters)
        {
            this.target = target.Select(f => f.TrimEnd('\\')).Where(t => ZlpIOHelper.DirectoryExists(t)).ToArray();
            this.WritingVolume = ZlpPathHelper.GetPathRoot(this.target.First()).TrimEnd('\\');
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
                var todelete = new List<IZlpFileSystemInfo>();
                foreach (var dir in target)
                {
                    var root = new ZlpDirectoryInfo(dir);
                    FindToDeleteRecursive(root, todelete);
                    if (todelete.Contains(root))
                    {
                        todelete.Remove(root);
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
                        CurrentName = item.FullName;
                        DoStatus(false);
                        if (item is ZlpDirectoryInfo dir)
                        {
                            if (dir.SafeExists())
                            {
                                try
                                {
                                    dir.ForceDelete();
                                }
                                catch { }
                            }
                        }
                        else if (item is ZlpFileInfo file)
                        {
                            try
                            {
                                file.ForceDelete();
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
                IsDone = true;
                DoComplete();
            }
        }

        readonly string[] csexts = { ".cs", ".csproj", ".sln" };
        private void FindToDeleteRecursive(ZlpDirectoryInfo Dir, List<IZlpFileSystemInfo> todelete)
        {
            if (IsCancelled) { return; }
            bool iscs = Dir.GetFiles().Any(f => csexts.Contains(f.Extension.ToLower()));
            bool ispio = Dir.GetFiles().Any(f => f.Name.ToLower() == "platformio.ini");
            foreach (var subdir in Dir.GetDirectories())
            {
                if (iscs && (subdir.Name.ToLower() == "bin" || subdir.Name.ToLower() == "obj" || subdir.Name.ToLower() == ".vs"))
                {
                    todelete.Add(subdir);
                    continue;
                    //todelete.AddRange(Dir.GetDirectories().Where(d => d.Name.ToLower() == "bin" || d.Name.ToLower() == "obj" || d.Name.ToLower() == ".vs"));
                }
                if (ispio && subdir.Name.ToLower() == ".pio")
                {
                    todelete.Add(subdir);
                    continue;
                    //todelete.AddRange(Dir.GetDirectories().Where(d => d.Name.ToLower() == ".pio"));
                }
                FindToDeleteRecursive(subdir, todelete);
            }
        }
    }
}
