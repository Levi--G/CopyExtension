using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace CopyExtension.UI;

public class OptionControl : UserControl
{

    string details;
    public OptionControl(string text, string details, IEnumerable<string> choices)
    {
        ZIndex = 10;
        this.details = details;

        var Grid = new Grid
        {
            RowDefinitions = RowDefinitions.Parse("auto,*,auto"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new SolidColorBrush(MainWindow.BackGroundBrush)
        };
        Grid.Children.Add(new TextBlock() { Text = text });
        var flow = new WrapPanel() { VerticalAlignment = VerticalAlignment.Center };
        Grid.SetRow(flow, 1);
        Grid.Children.Add(flow);
        foreach (var item in choices)
        {
            var b = new Button() { Content = new TextBlock() { Text = item, HorizontalAlignment=HorizontalAlignment.Center }, Margin = new Thickness(2), Width = 80, Height = 32 };
            b.Click += (s, e) =>
            {
                OnChoose?.Invoke(item);
            };
            flow.Children.Add(b);
        }
        var btn = new Button() { Content = "Details", Margin = new Thickness(2)/*, Padding = new Thickness(2)*/ };
        btn.Click += Btn_Click;
        Grid.SetRow(btn, 2);
        Grid.Children.Add(btn);
        Content = Grid;
    }

    private void Btn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard("Details", details).ShowWindowAsync();
    }

    public event Action<string>? OnChoose;
}