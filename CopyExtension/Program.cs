using CopyExtension.Helpers;
using CopyExtensionAOT;

namespace CopyExtension;

static class Program
{
    public static Settings Options => field ??= Settings.Load();

    public static ILogger Logger => field ??= Options.EnableLogging ? new Logger(LocalAppDir) : new DummyLogger();

    public static string LocalAppDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CopyExtension");

    public static void Main(string[] args)
    {
        new Cmd().Execute(args);
    }
}
