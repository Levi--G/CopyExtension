namespace CopyExtension.Helpers;
public class Logger : ILogger
{
    public const string LogFileName = "log.log";
    public const string BackupLogFileName = "log.old.log";
    public string LogFile { get; }
    public string BackupLogFile { get; }

    private static Lock Lock = new();
    private int lognumber = 9;

    public Logger(string localappdir)
    {
        LogFile = Path.Combine(localappdir, LogFileName);
        BackupLogFile = Path.Combine(localappdir, BackupLogFileName);
    }

    public void Log(object? sender = null, params IEnumerable<string> Logs)
    {
        lock (Lock)
        {
            LogInternal(sender, Logs);
        }
    }

    public void DebugLog(object? sender = null, params IEnumerable<string> Logs)
    {
#if DEBUG
        Log(sender, Logs);
#endif
    }

    private void LogInternal(object? sender, params IEnumerable<string> logs)
    {
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
        var senderstr = (sender is string) ? sender : sender?.GetType().Name;
        logs = logs.Select(l => $"{DateTime.Now}.{DateTime.Now.Millisecond:D3} [{senderstr}] ({Environment.CurrentManagedThreadId:D2}): {l}");

        File.AppendAllLines(LogFile, logs);
    }

    public void DebugLog(object? sender, Exception ex)
    {
#if DEBUG
        Log(sender, ex);
#endif
    }

    public void Log(object? sender, Exception? ex)
    {
        lock (Lock)
        {
            while (ex != null)
            {
                LogInternal(sender, ["Exception occured: " + ex.GetType().FullName, ex.Message, .. ex.StackTrace?.Split(Environment.NewLine) ?? []]);
                ex = ex.InnerException;
            }
        }
    }
}