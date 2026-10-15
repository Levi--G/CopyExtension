using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using CopyExtension.Helpers;
using CopyExtension.Tasks;
using MsBox.Avalonia.ViewModels.Commands;
using System.Data;

namespace CopyExtension.UI;

class MainWindow : Window, ICopyWindow
{
    public static TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(500);
    public static uint BackGroundBrush = 0xFF2C2C2C;
    public event Action? ExitRequested;

    Panel panel = new WrapPanel
    {
        Orientation = Orientation.Vertical
    };
    bool active = true;
    private readonly WindowMgr windowMgr;

    public MainWindow(WindowMgr windowMgr)
    {
        Title = "Running tasks";
        Background = new SolidColorBrush(BackGroundBrush);
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Images", "Square44x44Logo.targetsize-16.png");
            if (File.Exists(path))
            {
                using var s = File.OpenRead(path);
                Icon = new WindowIcon(s);
            }
        }
        catch { }
        //Width = 400;
        //Height = 300;
        Panel g = new();
        g.Children.Add(panel);
        var optionscm = new MenuFlyout();
        var finishmi = new MenuItem() { Header = "On finish" };
        finishmi.Items.Add(new MenuItem() { Header = "Do nothing", Command = new RelayCommand(s => windowMgr.SetFinishAction(null)) });
        finishmi.Items.Add(new MenuItem() { Header = "Lock", Command = new RelayCommand(s => windowMgr.SetFinishAction(DesktopActions.Lock)) });
        finishmi.Items.Add(new MenuItem() { Header = "Logoff", Command = new RelayCommand(s => windowMgr.SetFinishAction(DesktopActions.Logoff)) });
        finishmi.Items.Add(new MenuItem() { Header = "Sleep", Command = new RelayCommand(s => windowMgr.SetFinishAction(DesktopActions.Sleep)) });
        finishmi.Items.Add(new MenuItem() { Header = "Hibernate", Command = new RelayCommand(s => windowMgr.SetFinishAction(DesktopActions.Hibernate)) });
        finishmi.Items.Add(new MenuItem() { Header = "Shutdown", Command = new RelayCommand(s => windowMgr.SetFinishAction(DesktopActions.Shutdown)) });
        finishmi.Items.Add(new MenuItem() { Header = "Reboot", Command = new RelayCommand(s => windowMgr.SetFinishAction(DesktopActions.Reboot)) });
        optionscm.Items.Add(finishmi);
        var optionsbtn = new Button()
        {
            Content = "⚙️",
            ZIndex = 20,
            Padding = new Thickness(2),
            Margin = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Flyout = optionscm
        };
        g.Children.Add(optionsbtn);
        Content = g;
        SizeToContent = SizeToContent.WidthAndHeight;
        this.Resized += UpdateState;
        this.Activated += Form1_Activated;
        this.Deactivated += Form1_Deactivate;
        this.Closing += Form1_FormClosing;
        this.windowMgr = windowMgr;
    }

    private void Form1_FormClosing(object? sender, WindowClosingEventArgs e)
    {
        ExitRequested?.Invoke();
    }

    private void Form1_Deactivate(object? sender, EventArgs e)
    {
        active = false;
        UpdateState(sender, e);
    }

    private void Form1_Activated(object? sender, EventArgs e)
    {
        active = true;
        UpdateState(sender, e);
    }

    private void UpdateState(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            UpdateInterval = TimeSpan.FromSeconds(20);
        }
        else if (WindowState == WindowState.Normal)
        {
            if (active)
            {
                UpdateInterval = TimeSpan.FromMilliseconds(500);
            }
            else
            {
                UpdateInterval = TimeSpan.FromMilliseconds(2000);
            }
        }
    }

    public HashSet<string> GetClipboardFiles()
    {
        HashSet<string> ret = [];
        var files = Clipboard?.TryGetFilesAsync().Result;
        if (files != null)
        {
            ret.AddRange(files.Select(f => f.Path.LocalPath));
        }
        var text = Clipboard?.TryGetTextAsync().Result;
        if (!string.IsNullOrEmpty(text))
        {
            ret.AddRange(text.Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        }
        return ret;
    }

    public void Quit()
    {
        Close();
    }

    public void AddTask(CopyTask task)
    {
        var c = new TaskControl(task);
        panel.Children.Add(c);
        Title = $"Running {panel.Children.Count - 1} tasks";
    }

    public static MainWindow Start(WindowMgr windowMgr)
    {
        MainWindow? Instance = null;
        Thread t = new((StartApp) =>
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
                        var window = new MainWindow(windowMgr);
                        Instance = window;
                        window.Show();
                        app.Run(window);
                    }, []);
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

    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Program.Logger.Log("Dispatcher", "Unhandled UI thread exception: " + e.Exception.Message);
        e.Handled = true;
    }

    public void Invoke(Action<ICopyWindow> torun)
    {
        Dispatcher.Invoke(() => torun(this));
    }

    public void Post(Action<ICopyWindow> torun)
    {
        Dispatcher.Post(() => torun(this));
    }

    public T Invoke<T>(Func<ICopyWindow, T> torun)
    {
        return Dispatcher.Invoke(() => torun(this));
    }
}
