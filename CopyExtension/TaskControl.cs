using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CopyExtension
{
    public partial class TaskControl : UserControl
    {
        CopyTask task;
        DateTime Lastupdate = DateTime.MinValue;

        public TaskControl(CopyTask task)
        {
            this.task = task;
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
            label4.Text = $"Time remaining: {task.CurrentTime}";
            label5.Text = $"Items remaining: {task.TotalItems - task.CurrentItems} ({GetHumanReadable(task.TotalProgress - task.CurrentProgress)})";
            label6.Text = task.CurrentSpeed;
        }

        public static string GetHumanReadable(long item)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (item == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(item);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return $"{Math.Sign(item) * num} {suf[place]}";
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
    }
}
