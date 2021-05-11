using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace CopyExtension
{
    public partial class TaskControl : UserControl, IOptionGui
    {
        CopyTask task;
        DateTime Lastupdate = DateTime.MinValue;

        public TaskControl(CopyTask task)
        {
            this.task = task;
            task.OptionGui = this;
            InitializeComponent();
            task.OnNewStatus += Task_OnNewStatus;
            UpdateStatus();
        }

        private void Task_OnNewStatus(bool force)
        {
            var d = DateTime.Now;
            if (force || d - Lastupdate > MainForm.UpdateInterval)
            {
                Lastupdate = d;
                this.BeginInvoke((Action)UpdateStatus);
            }
        }

        void UpdateStatus()
        {
            label1.Text = task.FullAction ?? $"{task.Action} {task.TotalItems} items from {task.ReadingVolume} to {task.WritingVolume}";
            label2.Text = $"{GetStateFromTask()} - {task.Progress}% complete";
            progressBar1.Maximum = 100;
            progressBar1.Value = task.Progress;
            label3.Text = $"Name: {task.CurrentName}";
            var speed = task.CurrentSpeedValue;
            label4.Text = $"Time remaining: {ToHumanDuration(RemainingTime(speed))}";
            label5.Text = $"Items remaining: {task.TotalItems - task.CurrentItems} ({GetHumanReadable(task.TotalProgress - task.CurrentProgress, task.CurrentSpeedUnit)})";
            label6.Text = $"{GetHumanReadable(speed, task.CurrentSpeedUnit)}/s";
        }

        TimeSpan? RemainingTime(long speed)
        {
            if (speed == 0 || task.TotalProgress == 0)
            {
                return null;
            }
            var remaining = (task.TotalProgress - task.CurrentProgress) / speed;
            return TimeSpan.FromSeconds(remaining);
        }

        public static string GetHumanReadable(long item, string unit)
        {
            string[] suf = { "", "K", "M", "G", "T", "P", "E" }; //Longs run out around EB
            if (item == 0)
                return "0 " + unit;
            long bytes = Math.Abs(item);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return $"{Math.Sign(item) * num} {suf[place]}{unit}";
        }

        string GetStateFromTask()
        {
            if (!task.IsStarted)
            {
                return "Waiting";
            }
            if (!task.IsDone)
            {
                if (!task.IsCancelled)
                {
                    if (!task.IsPaused)
                    {
                        return task.CurrentAction;
                    }
                    return "Paused";
                }
                return "Cancelling";
            }
            return "Done";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            task.Pause();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            task.Cancel();
        }

        public T GetChoiceResult<T>(string text, string details, T[] Options)
        {
            T choice = default;
            bool chosen = false;
            this.Invoke((Action)(() =>
            {
                var op = new OptionControl(text, details, Options.Cast<object>().ToArray());
                op.OnChoose += c =>
                {
                    choice = (T)c;
                    chosen = true;
                    Controls.Remove(op);
                };
                this.Controls.Add(op);
                op.BringToFront();
            }));
            while (!chosen)
            {
                Thread.Sleep(100);
            }
            return choice;
        }

        public string ToHumanDuration(TimeSpan? duration)
        {
            if (duration == null) return null;
            var builder = new StringBuilder();
            duration = duration.Value;

            if (duration.Value.Days > 0)
            {
                builder.Append($"{duration.Value.Days}d ");
            }

            if (duration.Value.Hours > 0)
            {
                builder.Append($"{duration.Value.Hours}h ");
            }

            if (duration.Value.Minutes > 0)
            {
                builder.Append($"{duration.Value.Minutes}m ");
            }

            if (duration.Value.TotalHours < 1)
            {
                builder.Append(duration.Value.Seconds);
                builder.Append("s ");
            }

            if (builder.Length <= 1)
            {
                builder.Append(" <1ms ");
            }

            builder.Remove(builder.Length - 1, 1);

            return builder.ToString();
        }
    }
}
