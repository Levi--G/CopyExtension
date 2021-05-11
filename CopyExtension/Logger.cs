using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace CopyExtension
{
    public class Logger
    {
        private static string currentDirectory = string.Empty;

        public static string ExeDirectory
        {
            get
            {
                if (currentDirectory == string.Empty)
                {
                    currentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Substring(8));
                }
                return currentDirectory;
            }
        }

        public string LogFile { get; } = "log.log";
        public string BackupLogFile { get; } = "log.old.log";

        public Logger(string File = null, string BackupFile = null)
        {
            this.LogFile = File ?? Path.Combine(ExeDirectory, this.LogFile);
            this.BackupLogFile = BackupFile ?? Path.Combine(ExeDirectory, this.BackupLogFile);
        }

        public Func<object, string, string> CustomFormat { get; set; } = null;

        private static ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();

        private int lognumber;

        public void Log(object sender = null, params string[] Logs)
        {
            Lock.EnterWriteLock();
            try
            {
                LogInternal(sender, Logs);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public void DebugLog(object sender = null, params string[] Logs)
        {
#if DEBUG
            Log(sender, Logs);
#endif
        }

        private void LogInternal(object sender, params string[] Logs)
        {
            IEnumerable<string> logs = Logs;
            lognumber++;
            if (lognumber >= 10)
            {
                lognumber = 0;
                if (File.Exists(LogFile) && new FileInfo(LogFile).Length > 1024 * 1024)
                {
                    if (File.Exists(BackupLogFile))
                    {
                        File.Delete(BackupLogFile);
                    }
                    File.Move(LogFile, BackupLogFile);
                }
            }

            if (CustomFormat != null)
            {
                logs = logs.Select(l => CustomFormat(sender, l));
            }
            else
            {
                logs = logs.Select(l => $"{DateTime.Now}.{DateTime.Now.Millisecond:D3} ({Thread.CurrentThread.ManagedThreadId:D2}): {((sender != null) ? $"[{((sender is string) ? sender : sender.GetType().Name)}]" : "")}: {l}");
            }
            File.AppendAllLines(LogFile, logs);
        }

        public void DebugLog(object sender, Exception ex)
        {
#if DEBUG
            Log(sender, ex);
#endif
        }

        public void Log(object sender, Exception ex)
        {
            Lock.EnterWriteLock();
            try
            {
                while (ex != null)
                {
                    LogInternal(sender, "Exception occured: " + ex.GetType().FullName, ex.Message, ex.StackTrace);
                    ex = ex.InnerException;
                }
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }
    }
}