using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CopyExtension
{
    public abstract class CopyTask
    {
        public event Action OnComplete;
        public event Action<bool> OnNewStatus;
        public IOptionGui OptionGui { get; set; }

        public string ReadingVolume { get; protected set; }
        public string WritingVolume { get; protected set; }
        public virtual string FullAction => null;
        public string Action { get; protected set; }
        public string CurrentAction { get; protected set; }
        public abstract string CurrentSpeedUnit { get; }
        public abstract long CurrentSpeedValue { get; }

        public long CurrentProgress { get; protected set; }
        public long TotalProgress { get; protected set; }
        public byte Progress => TotalProgress == 0 ? (byte)0 : (byte)(CurrentProgress * 100 / TotalProgress);

        public string CurrentName { get; protected set; }
        public int CurrentItems { get; protected set; }
        public int TotalItems { get; protected set; }

        public bool IsStarted { get; protected set; }
        public bool IsPaused { get; protected set; }
        public bool IsCancelled { get; protected set; }
        public bool IsDone { get; protected set; }
        bool IsDoneExecuted;

        public virtual void Start()
        {
            IsStarted = true;
            DoStatus(true);
        }

        public virtual void Pause()
        {
            if (!IsStarted)
            {
                Start();
            }
            else
            {
                IsPaused = !IsPaused;
                DoStatus(true);
            }
        }

        public virtual void Cancel()
        {
            IsCancelled = true;
            IsDone = true;
            DoStatus(true);
        }

        protected void DoStatus(bool force)
        {
            OnNewStatus?.Invoke(force);
            if (IsDone && !IsDoneExecuted)
            {
                IsDoneExecuted = true;
                OnComplete?.Invoke();
            }
        }

        protected void DoComplete()
        {
            OnComplete?.Invoke();
        }

        protected bool CheckCancel()
        {
            while (IsPaused && !IsCancelled)
            {
                DoStatus(true);
                Thread.Sleep(1000);
            }
            return IsCancelled;
        }
    }
}
