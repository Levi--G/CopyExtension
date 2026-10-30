using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CopyExtension.Helpers;

namespace CopyExtension.UI;

class SettingsWindow : Window
{
    public SettingsWindow()
    {
        Title = "SFCopyExtension Settings";
        Background = new SolidColorBrush(CopyApp.BackGroundBrush);
        Icon = CopyApp.TryGetIcon();
        Padding = new Thickness(10);

        static Button ibtn(string text) => new()
        {
            Content = new TextBlock() { Text = "ℹ️", FontSize = 16 },
            Margin = new Thickness(2),
            Padding = new Thickness(-2, -3),
            Flyout = new Flyout() { Content = new TextBlock() { Width = 400, TextWrapping = TextWrapping.WrapWithOverflow, Text = text } }
        };
        static TextBlock leftlbl(string text) => new TextBlock() { Text = text, Margin = new Thickness(5, 0), VerticalAlignment = VerticalAlignment.Center };
        static TextBlock rightlbl(string text) => new TextBlock() { Text = text, Margin = new Thickness(20, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center };

        Grid g = new();
        var ibtn1 = ibtn("Default: On. Ensures only one copy task is writing on the same volume or share to prevent fragmentation and slowness. Recommended to leave on, even with ssds results in faster copies by default. Specific copy tasks can still be enabled manually when needed.");
        var lbl1 = leftlbl("Reserve writing: ");
        var writing = new ToggleSwitch() { IsChecked = Program.Options.ReserveWriting, Margin = new Thickness(5) };
        writing.IsCheckedChanged += (s, e) => { Program.Options.ReserveWriting ^= true; writing.IsChecked = Program.Options.ReserveWriting; };

        var ibtn2 = ibtn("Default: Off. Ensures only one copy task is reading on the same volume or share to prevent fragmentation and slowness. When combined with \"Reserve writing\" will ensure any operation is only using free volumes. Recommended to leave on when using any hdd storage (even over smb/nfs).");
        var lbl2 = rightlbl("Reserve reading: ");
        var reading = new ToggleSwitch() { IsChecked = Program.Options.ReserveReading, Margin = new Thickness(5) };
        reading.IsCheckedChanged += (s, e) => { Program.Options.ReserveReading ^= true; reading.IsChecked = Program.Options.ReserveReading; };

        var ibtn3 = ibtn("Default: Off. Enables logging, only enable when encountering issues and you want to upload the logs to github. Warning: Logs are currently not scrubbed of personal information like usernames in paths. Always check the file before uploading! We do not automatically collect these, you need to manually send them if you want them analysed!");
        var lbl3 = leftlbl("Enable logging ⚠️: ");
        var logging = new ToggleSwitch() { IsChecked = Program.Options.EnableLogging, Margin = new Thickness(5) };
        logging.IsCheckedChanged += (s, e) => { Program.Options.EnableLogging ^= true; reading.IsChecked = Program.Options.EnableLogging; };

        var ibtn4 = ibtn("Default: Off. Enables the \"Compare Names\" option in explorer. This mode only checks names, not content, leave off if you do not know what it is! (check github for more info)");
        var lbl4 = rightlbl("Enable CompareNames ⚠️: ");
        var comparenames = new ToggleSwitch() { IsChecked = Program.Options.EnableCompareNames, Margin = new Thickness(5) };
        comparenames.IsCheckedChanged += (s, e) => { Program.Options.EnableCompareNames ^= true; reading.IsChecked = Program.Options.EnableCompareNames; };

        var ibtn5 = ibtn("Default: Off. Enables the \"Hardlink\" option in explorer. This mode copies files/directories/structures but uses hardlinks instead of copies for the individual files. Do not enable if you do not understand hardlinks and how they are not backups nor copies!");
        var lbl5 = leftlbl("Enable Hardlink: ");
        var hardlink = new ToggleSwitch() { IsChecked = Program.Options.EnableHardlink, Margin = new Thickness(5) };
        hardlink.IsCheckedChanged += (s, e) => { Program.Options.EnableHardlink ^= true; reading.IsChecked = Program.Options.EnableHardlink; };

        var ibtn6 = ibtn("Default: Off. Enables the \"Clean Source\" option in explorer. This mode cleans certain compiler generated directories from c# and platformio project folders. Leave off if you do not know what it does.");
        var lbl6 = rightlbl("Enable Clean Source ⚠️: ");
        var clean = new ToggleSwitch() { IsChecked = Program.Options.EnableCleanSource, Margin = new Thickness(5) };
        clean.IsCheckedChanged += (s, e) => { Program.Options.EnableCleanSource ^= true; reading.IsChecked = Program.Options.EnableCleanSource; };

        var ibtn7 = ibtn("Default: On. Enables the \"Nuke Folder\" option in explorer. This mode will delete any empty directories and directory trees. System files (thumbs.db, desktop.ini) are ignored and count as empty.");
        var lbl7 = leftlbl("Enable Nuke Folder: ");
        var folder = new ToggleSwitch() { IsChecked = Program.Options.EnableNukeFolder, Margin = new Thickness(5) };
        folder.IsCheckedChanged += (s, e) => { Program.Options.EnableNukeFolder ^= true; reading.IsChecked = Program.Options.EnableNukeFolder; };

        var ibtn8 = ibtn("Default: On. Enables the \"Nuke File\" option in explorer. This mode will delete any empty (0B) files.");
        var lbl8 = rightlbl("Enable Nuke File: ");
        var file = new ToggleSwitch() { IsChecked = Program.Options.EnableNukeFile, Margin = new Thickness(5) };
        file.IsCheckedChanged += (s, e) => { Program.Options.EnableNukeFile ^= true; reading.IsChecked = Program.Options.EnableNukeFile; };

        var ibtn9 = ibtn("Default: 10. Determines how many small files are queued at once when operating in bulk mode.");
        var lbl9 = leftlbl("Small file concurrency limit: ");
        var limit = new NumericUpDown() { Value = Program.Options.SmallFileLimit, Increment = 1, Margin = new Thickness(5) };
        limit.ValueChanged += (s, e) => { Program.Options.SmallFileLimit = (int)limit.Value; };

        var ibtn10 = ibtn("Default: 5MB. Determines what counts as a small file to engage the bulk mode queue.");
        var lbl10 = rightlbl("Small file size limit (B): ");
        var size = new NumericUpDown() { Value = Program.Options.SmallFileSize, Increment = 1, Margin = new Thickness(5) };

        Button b = new() { Content = "Save & Close", Margin = new Thickness(2) };
        b.Click += B_Click;

        TextBlock howto = new() { Text = "How To:", Margin = new Thickness(5), FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center };
        Grid.SetColumnSpan(howto, 6);
        TextBlock info = new() { Width = 750, TextWrapping = TextWrapping.Wrap, Text = "Right click in or on a directory in windows explorer to view the available options. Copy or cut a directory or file and use right click=>Copy here, Move here or Compare here to use the respective functions. Use CopyExtension.exe --help from the command line to view how to use the tool in scripts.", Margin = new Thickness(5) };
        Grid.SetColumnSpan(info, 6);
        HyperlinkButton link = new() { NavigateUri = new Uri("https://github.com/Levi--G/CopyExtension"), Margin = new Thickness(5), Content = "Visit the github for more information!", HorizontalAlignment = HorizontalAlignment.Center };
        Grid.SetColumnSpan(link, 6);
        g.AutoGrid(GridLength.Auto, "auto,auto,auto,auto,auto,auto",
            [ibtn1, lbl1, writing, ibtn2, lbl2, reading,
            ibtn3, lbl3, logging, ibtn4, lbl4, comparenames,
            ibtn5, lbl5, hardlink, ibtn6, lbl6, clean,
            ibtn7, lbl7, folder, ibtn8, lbl8, file,
            ibtn9, lbl9, limit, ibtn10, lbl10, size,
            null, b, null, null, null, null,
            howto, null, null, null, null, null,
            info, null, null, null, null, null,
            link]
            );
        Content = g;
        SizeToContent = SizeToContent.WidthAndHeight;
    }

    private void B_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Program.Options.Save();
        Close();
    }

    public static void ShowDialog()
    {
        CopyApp.RunAvaloniaWindow(() => new SettingsWindow());
    }
}
