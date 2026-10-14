namespace CopyExtension.Helpers;

public class DummyLogger : ILogger
{
    public string LogFile => "";

    public void DebugLog(object? sender = null, params IEnumerable<string> Logs)
    {
    }

    public void DebugLog(object? sender, Exception ex)
    {
    }

    public void Log(object? sender = null, params IEnumerable<string> Logs)
    {
    }

    public void Log(object? sender, Exception? ex)
    {
    }
}
