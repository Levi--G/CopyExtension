using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia;
using Avalonia.Layout;
using CopyExtension.Tasks;
using CopyExtension.Helpers;

namespace CopyExtension.UI;

class TaskControl : UserControl, IOptionGui
{
    private CopyTask task;
    DateTime Lastupdate = DateTime.MinValue;
    Panel ContentPanel = new();
    Grid Grid = new();
    TextBlock top = new() { FontSize = 12, Margin = new Thickness(2) };
    TextBlock pct = new() { FontSize = 16, Margin = new Thickness(2) };
    TextBlock speed = new() { ZIndex = 2, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(2) };
    TextBlock current = new() { FontSize = 12, Margin = new Thickness(2) };
    TextBlock time = new() { FontSize = 12, Margin = new Thickness(2) };
    TextBlock items = new() { FontSize = 12, Margin = new Thickness(2) };
    ProgressBar bar = new() { FontSize = 12, ShowProgressText = false, Minimum = 0, Maximum = 100, Margin = new Thickness(2), Height = 20 };


    public TaskControl(CopyTask task)
    {
        Width = 400;
        //Height = 210;
        Padding = new Thickness(20, 10);
        Content = ContentPanel;
        ContentPanel.Children.Add(Grid);
        Grid.RowDefinitions = RowDefinitions.Parse("*,*,*,*,*,*");//.AddRange(Enumerable.Repeat(new RowDefinition(GridLength.Auto), 6));
        Grid.SetRow(top, 0);
        Grid.SetRow(pct, 1);
        var sp = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var pau = new Button() { Content = "⏸️", ZIndex = 1, Padding = new Thickness(2), Margin = new Thickness(2) };
        pau.Click += Pau_Click;
        sp.Children.Add(pau);
        var can = new Button() { Content = "❌", ZIndex = 1, Padding = new Thickness(2), Margin = new Thickness(2) };
        can.Click += Can_Click;
        sp.Children.Add(can);
        Grid.SetRow(sp, 1);
        Grid.SetRow(speed, 2);
        Grid.SetRow(bar, 2);
        Grid.SetRow(current, 3);
        Grid.SetRow(time, 4);
        Grid.SetRow(items, 5);
        Grid.Children.AddRange([top, pct, sp, speed, bar, current, time, items]);
        this.task = task;
        task.OptionGui = this;
        task.OnNewStatus += Task_OnNewStatus;
        task.OnComplete += Task_OnComplete;
        UpdateStatus();
    }

    private void Task_OnComplete(CopyTask obj)
    {
        Dispatcher.Post(() =>
        {
            if (Parent is Panel p)
            {
                p.Children.Remove(this);
            }
        });
    }

    private void Can_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        task.Cancel();
    }

    private void Pau_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        task.Pause();
    }

    private void Task_OnNewStatus(bool force)
    {
        var d = DateTime.Now;
        if (force || d - Lastupdate > MainWindow.UpdateInterval)
        {
            Lastupdate = d;
            Dispatcher.InvokeAsync(UpdateStatus);
        }
    }

    void UpdateStatus()
    {
        top.Text = task.FullAction ?? $"{task.Action} {task.TotalItems} items from {task.ReadingVolume} to {task.WritingVolume}";
        pct.Text = $"{GetStateFromTask()} - {task.Progress}% complete";
        bar.Value = Math.Min((byte)100, task.Progress);
        current.Text = $"Name: {task.CurrentName}";
        var speedval = task.CurrentSpeedValue;
        time.Text = $"Time remaining: {task.GetRemainingTime(speedval).ToHumanDuration()}";
        items.Text = $"Items remaining: {task.TotalItems - task.CurrentItems} ({(task.TotalProgress - task.CurrentProgress).GetHumanReadable(task.CurrentSpeedUnit)})";
        speed.Text = $"{speedval.GetHumanReadable(task.CurrentSpeedUnit)}/s";
    }

    string GetStateFromTask()
    {
        if (!task.IsStarted)
        {
            return "Waiting";
        }
        if (!task.IsDone)
        {
            if (!task.IsCancelled)
            {
                if (!task.IsPaused)
                {
                    return task.CurrentAction;
                }
                return "Paused";
            }
            return "Cancelling";
        }
        return "Done";
    }

    public T? GetChoiceResult<T>(string text, string details, Dictionary<string, T> Options)
    {
        T? choice = default;
        bool chosen = false;
        Dispatcher.Post(() =>
        {
            var op = new OptionControl(text, details, Options.Keys);
            op.OnChoose += c =>
            {
                choice = Options.GetValueOrDefault(c);
                chosen = true;
                ContentPanel.Children.Remove(op);
            };
            ContentPanel.Children.Add(op);
        });
        while (!chosen && task?.IsDone == false && task?.IsCancelled == false)
        {
            Thread.Sleep(100);
        }
        return choice;
    }

}
