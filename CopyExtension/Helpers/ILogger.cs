namespace CopyExtension.Helpers;

public interface ILogger
{
    string LogFile { get; }

    void DebugLog(object? sender = null, params IEnumerable<string> Logs);
    void DebugLog(object? sender, Exception ex);
    void Log(object? sender = null, params IEnumerable<string> Logs);
    void Log(object? sender, Exception? ex);
}