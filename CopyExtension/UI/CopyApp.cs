using Avalonia;
using Avalonia.Controls;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace CopyExtension.UI;

internal static class CopyApp
{
    public static uint BackGroundBrush = 0xFF2C2C2C;

    public static WindowIcon? TryGetIcon()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Images", "Square44x44Logo.targetsize-16.png");
            if (File.Exists(path))
            {
                using var s = File.OpenRead(path);
                return new WindowIcon(s);
            }
        }
        catch { }
        return null;
    }

    public static T StartAvaloniaWindow<T>(Func<T> constr) where T : Window
    {
        T? Instance = null;
        Thread t = new((StartApp) =>
        {
            RunAvaloniaWindow(() =>
            {
                var i = constr();
                Instance = i;
                return i;
            });
        });
        t.TrySetApartmentState(ApartmentState.STA);
        t.Start();
        while (Instance == null)
        {
            Thread.Sleep(20);
        }
        while (!Instance.IsInitialized)
        {
            Thread.Sleep(20);
        }
        return Instance;
    }

    public static void RunAvaloniaWindow<T>(Func<T> constr) where T : Window
    {
        AppBuilder.Configure<Application>()
            .UseHarfBuzz()
            .UseWin32()
            .UseSkia()
            .Start(
            (app, args) =>
            {
                Dispatcher.UIThread.UnhandledException += OnUnhandledException;
                app.Styles.Add(new FluentTheme());
                var window = constr();
                window.Show();
                app.Run(window);
            }, []);
    }

    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Program.Logger.Log("Dispatcher", "Unhandled UI thread exception: " + e.Exception.Message);
        e.Handled = true;
    }
}
