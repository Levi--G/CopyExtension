using Avalonia.Threading;
using CopyExtension.Helpers;
using CopyExtension.Tasks;
using CopyExtensionAOT;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;

namespace CopyExtension.UI
{
    internal class ConsoleWindow : ICopyWindow, IOptionGui
    {
        public event Action? ExitRequested;

        static Win32Console W32Console => field ??= new Win32Console();
        DateTime Lastupdate = DateTime.MinValue;
        private readonly WindowMgr windowMgr;
        PosixSignalRegistration? sigquit;
        PosixSignalRegistration? sigterm;
        Lock OptionLock = new();

        public ConsoleWindow(WindowMgr windowMgr)
        {
            this.windowMgr = windowMgr;
            Console.CancelKeyPress += Console_CancelKeyPress;
            sigquit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, Posix_Quit);
            sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, Posix_Quit);
        }

        private void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            ExitRequested?.Invoke();
            e.Cancel = true;
        }

        private void Posix_Quit(PosixSignalContext c)
        {
            ExitRequested?.Invoke();
            c.Cancel = true;
        }

        void UpdateConsole()
        {
            if (OptionLock.TryEnter())
            {
                try
                {
                    UpdateConsoleInternal();
                }
                finally
                {
                    OptionLock.Exit();
                }
            }
        }

        void UpdateConsoleInternal()
        {
            windowMgr.GetTasks(tasks =>
            {
                Console.Clear();
                //Console.SetCursorPosition(0, 0);
                var count = tasks.Count;
                Console.WriteLine($"Processing {count} tasks:");
                Console.WriteLine();
                Span<char> chars = stackalloc char[Console.WindowWidth - 1];
                foreach (var task in tasks)
                {
                    RenderTask(chars, task, count < 5);
                }
            });
        }

        void RenderTask(Span<char> line, CopyTask task, bool full)
        {
            Console.WriteLine(task.FullAction ?? $"{task.Action} {task.TotalItems} items from {task.ReadingVolume} to {task.WritingVolume}");
            var speedval = task.CurrentSpeedValue;
            line.Fill(' ');
            RenderProgressBar(line, task.Progress);
            RenderText(line, $"{task.Progress}%-{speedval.GetHumanReadable(task.CurrentSpeedUnit)}/s", 50);
            Console.WriteLine(line);
            if (full)
            {
                Console.WriteLine($"Name: {task.CurrentName}");
                Console.WriteLine($"Time remaining: {task.GetRemainingTime(speedval).ToHumanDuration()}");
                Console.WriteLine($"Items remaining: {task.TotalItems - task.CurrentItems} ({(task.TotalProgress - task.CurrentProgress).GetHumanReadable(task.CurrentSpeedUnit)})");
            }
            Console.WriteLine();
        }

        static void RenderProgressBar(Span<char> line, int percent)
        {
            if (line.IsEmpty)
                return;
            percent = Math.Clamp(percent, 0, 100);
            int barWidth = line.Length - 1;
            int filledWidth = (int)((long)barWidth * percent / 100);
            for (int i = 0; i < barWidth; i++)
                line[i + 1] = i < filledWidth ? '#' : '-';
            line[0] = '[';
            line[barWidth] = ']';
        }

        static void RenderText(Span<char> line, string text, int positionpercent)
        {
            if (line.IsEmpty)
                return;
            positionpercent = Math.Clamp(positionpercent, 0, 100);
            int position = (int)((long)(line.Length - 1) * positionpercent / 100) - (text.Length / 2);
            for (int i = 0; i < text.Length && position + i < line.Length; i++)
                line[position + i] = text[i];
        }

        public static ICopyWindow Start(WindowMgr windowMgr)
        {
            W32Console.Attach();
            return new ConsoleWindow(windowMgr);
        }

        public void AddTask(CopyTask task)
        {
            task.OnNewStatus += Task_OnNewStatus;
            task.OptionGui = this;
            UpdateConsole();
        }

        private void Task_OnNewStatus(bool force)
        {
            var d = DateTime.Now;
            if (force || (d - Lastupdate).TotalSeconds > 1)
            {
                Lastupdate = d;
                Post(w => UpdateConsole());
            }
        }

        public HashSet<string> GetClipboardFiles()
        {
            var wmgr = new WindowMgr(false);
            wmgr.Start();
            var clip = wmgr.GetClipboardFiles();
            wmgr.Quit();
            wmgr.WaitForExit();
            return clip;
        }

        public void Quit()
        {
            Console.CancelKeyPress -= Console_CancelKeyPress;
            sigquit?.Dispose(); sigquit = null;
            sigterm?.Dispose(); sigterm = null;
            W32Console.Detach();
        }

        public void Invoke(Action<ICopyWindow> torun)
        {
            torun(this);
        }

        public void Post(Action<ICopyWindow> torun)
        {
            Task.Run(() => torun(this));
        }

        public T Invoke<T>(Func<ICopyWindow, T> torun)
        {
            return torun(this);
        }

        public T? GetChoiceResult<T>(string text, string details, Dictionary<string, T> Options)
        {
            lock (OptionLock)
            {
                while (Console.KeyAvailable)
                {
                    Console.ReadKey();
                }
                while (true)
                {
                    UpdateConsoleInternal();
                    Console.WriteLine(text);
                    Console.WriteLine(details);
                    Console.WriteLine(string.Join(",", Options.Select(o => $"[{char.ToLower(o.Key.First())}]{o.Key}")) + "?");
                    Thread.Sleep(1000);
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey();
                        var options = Options.Where(o => char.ToLower(o.Key.First()) == char.ToLower(key.KeyChar));
                        if (options.Any())
                        {
                            return options.First().Value;
                        }
                    }
                }
            }
        }
    }
}
