using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CopyExtension
{
    public static class Test
    {
        public static void Main()
        {

            var a = new CopyExtension();
            var s = new Stopwatch();
            s.Start();
            //MainForm.AddTask(new HardlinkTask(new[] { @"O:\qbittorrent\Done\Battlestar.Galactica.S01-S04.COMPLETE.1080p.BluRay.REMUX.DTS-HD.MA.5.1-decibeL" }, @"O:\Sonarr\Battlestar Galactica (2003)"));
            //MainForm.AddTask(new CopyMoveTask(new[] { @"T:\Movie Night\BeautyandtheBeast.mkv" }, @"T:\", false, false));
            MainForm.AddTask(new CopyMoveTask(new[] { @"T:\Movie Night\BeautyandtheBeast.mkv" }, @"D:\", false, false));
            //Directory.CreateDirectory(@"test\test1\test1");
            //Directory.CreateDirectory(@"test\test2\test1");
            //Directory.CreateDirectory(@"test\test1\test2");
            //Directory.CreateDirectory(@"test\test2\test2");
            //File.WriteAllBytes(@"test\test2\test2\test.txt", new byte[0]);
            //Process.Start(Path.Combine(Environment.CurrentDirectory, "test"));
            //MainForm.AddTask(new NukeTask(new[] { Path.Combine(Environment.CurrentDirectory, "test") }, true));
            //Thread.Sleep(5000);
            //MainForm.AddTask(new NukeTask(new[] { Path.Combine(Environment.CurrentDirectory, "test") }, false));
            //Thread.Sleep(5000);
            //MainForm.AddTask(new NukeTask(new[] { Path.Combine(Environment.CurrentDirectory, "test") }, true));
            while (MainForm.IsBusy)
            {
                Thread.Sleep(10);
            }
            s.Stop();
            Console.WriteLine(s.Elapsed);
            Console.ReadKey();
        }

        class DummyTask : CopyTask
        {
            public DummyTask()
            {
                this.Action = "Copying";
                this.CurrentAction = "Copying";
                this.CurrentProgress = 1;
                this.TotalProgress = 10;
                this.CurrentName = "Godzilla King of the Monsters 2019.mkv";
                this.CurrentTime = "1 hour";
                this.CurrentItems = 1;
                this.TotalItems = 10;
                this.ReadingVolume = "C:";
                this.WritingVolume = "D:";
                this.IsStarted = true;
            }

            public override string CurrentSpeed => "";

            public override long CurrentSpeedValue => 0;
        }
    }
}
