using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CopyExtension
{
    public partial class MainForm : Form
    {
        public static TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(200);
        public static bool IsBusy => Instance != null;

        private static object InstanceKey = new object();
        private static MainForm Instance;
        ConcurrentDictionary<CopyTask, TaskControl> controls = new ConcurrentDictionary<CopyTask, TaskControl>();

        bool active = true;

        MainForm()
        {
            InitializeComponent();
            this.Resize += UpdateState;
            this.Activated += Form1_Activated;
            this.Deactivate += Form1_Deactivate;
            FormClosing += Form1_FormClosing;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            lock (InstanceKey)
            {
                if (controls.Count > 0 || Instance == this)
                {
                    e.Cancel = true;
                    foreach (var control in controls)
                    {
                        control.Key.Cancel();
                    }
                }
            }
        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            active = false;
            UpdateState(sender, e);
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            active = true;
            UpdateState(sender, e);
        }

        private void UpdateState(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                UpdateInterval = TimeSpan.FromSeconds(20);
            }
            else if (WindowState == FormWindowState.Normal)
            {
                if (active)
                {
                    UpdateInterval = TimeSpan.FromMilliseconds(250);
                }
                else
                {
                    UpdateInterval = TimeSpan.FromMilliseconds(500);
                }
            }
        }

        void RefreshTasks()
        {
            if (this.IsHandleCreated)
            {
                //Add/remove tasks visuall or close self when not in use
                //check for starting tasks sharing volume after completion
                this.BeginInvoke((Action)(() =>
                {
                    lock (InstanceKey)
                    {
                        controls.Where(c => c.Key.IsDone).ToList().ForEach(d =>
                        {
                            flowLayoutPanel1.Controls.Remove(d.Value);
                            controls.TryRemove(d.Key, out _);
                        });
                        controls.Where(c => c.Value == null).ToList().ForEach(n =>
                        {
                            var c = new TaskControl(n.Key);
                            n.Key.OnComplete += RefreshTasks;
                            flowLayoutPanel1.Controls.Add(c);
                            controls.TryUpdate(n.Key, c, n.Value);
                        });
                        if (CopyExtension.Options.ReserveReading || CopyExtension.Options.ReserveWriting)
                        {
                            var volumes = new List<string>();
                            if (CopyExtension.Options.ReserveWriting)
                            {
                                volumes.AddRange(controls.Where(c => c.Key.IsStarted).Select(c => c.Key.WritingVolume));
                            }
                            if (CopyExtension.Options.ReserveReading)
                            {
                                volumes.AddRange(controls.Where(c => c.Key.IsStarted).Select(c => c.Key.ReadingVolume));
                            }
                            foreach (var c in controls)
                            {
                                if (!c.Key.IsStarted && (!CopyExtension.Options.ReserveWriting || !volumes.Contains(c.Key.WritingVolume)) && (!CopyExtension.Options.ReserveReading || !volumes.Contains(c.Key.ReadingVolume)))
                                {
                                    if (CopyExtension.Options.ReserveWriting)
                                    {
                                        volumes.Add(c.Key.WritingVolume);
                                    }
                                    if (CopyExtension.Options.ReserveReading)
                                    {
                                        volumes.Add(c.Key.ReadingVolume);
                                    }
                                    c.Key.Start();
                                }
                            }
                        }
                        else
                        {
                            foreach (var c in controls)
                            {
                                if (!c.Key.IsStarted)
                                {
                                    c.Key.Start();
                                }
                            }
                        }
                        if (controls.Count == 0)
                        {
                            Instance = null;
                            Close();
                        }
                    }
                }));
            }
        }

        void AddTaskInternal(CopyTask task)
        {
            //Add
            controls.TryAdd(task, null);
            RefreshTasks();
        }

        public static void AddTask(CopyTask task)
        {
            lock (InstanceKey)
            {
                if (Instance == null)
                {
                    Instance = new MainForm();
                    Thread t = new Thread(new ThreadStart(() => { Instance.ShowDialog(); }));
                    t.SetApartmentState(ApartmentState.STA);
                    t.Start();
                }
                while (!Instance.IsHandleCreated)
                {
                    Thread.Sleep(20);
                }
                Instance.BeginInvoke((Action)(() =>
                {
                    Instance.AddTaskInternal(task);
                }));
            }
        }
    }
}
