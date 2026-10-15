using CopyExtension.Helpers;
using System.CommandLine.Parsing;

namespace CopyExtension;

static class Program
{
    public static Settings Options => field ??= Settings.Load();

    public static ILogger Logger => field ??= Options.EnableLogging ? new Logger(LocalAppDir) : new DummyLogger();

    public static string LocalAppDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CopyExtension");

    public static void Main(string[] args)
    {
        Logger.Log("Program", $"Starting with cmd: {Environment.CommandLine}");
        new SingleInstance().Execute(args);
    }
}
