using Avalonia.Controls;
using CopyExtension.Tasks;
using System.Text;

namespace CopyExtension.Helpers;

internal static class Extensions
{
    public static void ForEach<T>(this IEnumerable<T> tasks, Action<T> action)
    {
        foreach (var task in tasks)
        {
            action(task);
        }
    }
    public static T GetResultSync<T>(this ValueTask<T> task)
    {
        if (task.IsCompleted)
        {
            return task.Result;
        }
        return task.AsTask().Result;
    }


    public static bool IsAnyMoveOrCopy(this CopyJobType CopyType)
    {
        return CopyType == CopyJobType.Copy || CopyType == CopyJobType.CopyUnsafe || CopyType == CopyJobType.Move || CopyType == CopyJobType.MoveUnsafe;
    }
    public static bool IsSafeMoveOrCopy(this CopyJobType CopyType)
    {
        return CopyType == CopyJobType.Copy || CopyType == CopyJobType.Move;
    }
    public static bool AddRange<T>(this HashSet<T> set, IEnumerable<T> items)
    {
        return items.All(set.Add);
    }

    internal static string GetHumanReadable(this long item, string unit)
    {
        string[] suf = { "", "K", "M", "G", "T", "P", "E" }; //Longs run out around EB
        if (item == 0)
            return "0 " + unit;
        long bytes = Math.Abs(item);
        int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        double num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return $"{Math.Sign(item) * num} {suf[place]}{unit}";
    }
    public static string? ToHumanDuration(this TimeSpan? duration)
    {
        if (duration == null) return null;
        var builder = new StringBuilder();
        duration = duration.Value;

        if (duration.Value.Days > 0)
        {
            builder.Append($"{duration.Value.Days}d ");
        }

        if (duration.Value.Hours > 0)
        {
            builder.Append($"{duration.Value.Hours}h ");
        }

        if (duration.Value.Minutes > 0)
        {
            builder.Append($"{duration.Value.Minutes}m ");
        }

        if (duration.Value.TotalHours < 1)
        {
            builder.Append(duration.Value.Seconds);
            builder.Append("s ");
        }

        if (builder.Length <= 1)
        {
            builder.Append(" <1ms ");
        }

        builder.Remove(builder.Length - 1, 1);

        return builder.ToString();
    }

    public static T? DequeueOrDefault<T>(this Queue<T> queue)
    {
        return queue.Count > 0 ? queue.Dequeue() : default;
    }

    public static void SetGridLocation(this Control c, int row, int column)
    {
        Grid.SetRow(c, row);
        Grid.SetColumn(c, column);
    }

    public static void SetGridSpan(this Control c, int row, int column)
    {
        Grid.SetRowSpan(c, row);
        Grid.SetColumnSpan(c, column);
    }

    public static void AutoGrid(this Grid g, GridLength row, string columns, IEnumerable<Control?> controls)
    {
        var cols = ColumnDefinitions.Parse(columns);
        g.ColumnDefinitions = cols;
        g.Children.Clear();
        int count = 0;
        foreach (var c in controls)
        {
            if (c != null)
            {
                c.SetGridLocation(count / cols.Count, count % cols.Count);
                g.Children.Add(c);
            }
            count++;
        }
        count--;
        var rows = (count / cols.Count) + 1;
        g.RowDefinitions.AddRange(Enumerable.Repeat(row, rows).Select(l => new RowDefinition(l)));
    }

    public static void AutoGridWithSpan(this Grid g, GridLength row, string columns, IEnumerable<Control?> controls)
    {
        var cols = ColumnDefinitions.Parse(columns);
        g.ColumnDefinitions = cols;
        g.Children.Clear();
        Control? last = null;
        int span = 0;
        int count = 0;
        foreach (var c in controls)
        {
            if (c == last)
            {
                if (c != null)
                {
                    span++;
                }
            }
            else
            {
                if (span > 0)
                {
                    var rowspan = (span / cols.Count) + 1;
                    var colspan = Math.Min(span + 1, cols.Count);
                    last?.SetGridSpan(rowspan, colspan);
                    span = 0;
                }
                if (c != null)
                {
                    c.SetGridLocation(count / cols.Count, count % cols.Count);
                    g.Children.Add(c);
                }
                last = c;
            }
            count++;
        }
        if (span > 0)
        {
            var rowspan = (span / cols.Count) + 1;
            var colspan = Math.Min(span + 1, cols.Count);
            last?.SetGridSpan(rowspan, colspan);
        }
        count--;
        var rows = (count / cols.Count) + 1;
        g.RowDefinitions.AddRange(Enumerable.Repeat(row, rows).Select(l => new RowDefinition(l)));
    }
}
internal static class DirExt
{
    public static bool CreateSafe(string? path)
    {
        if (path == null) { return true; }
        try
        {
            Directory.CreateDirectory(path);
            return true;
        }
        catch { }
        return false;
    }
    public static bool DeleteSafe(string? path, bool recursive = true)
    {
        if (path == null) { return false; }
        for (int i = 0; i < 3; i++)
        {
            try
            {
                Directory.Delete(path, recursive);
                return true;
            }
            catch { }
            try
            {
                var a = File.GetAttributes(path);
                a &= ~(FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System);
                File.SetAttributes(path, a);
            }
            catch { }
        }
        return false;
    }
    public static bool IsEmpty(string path)
    {
        return !Directory.EnumerateFileSystemEntries(path).Any(f => !FileExt.IsSystemFile(f));
    }
    public static bool HasNoFiles(string path)
    {
        return !Directory.EnumerateFiles(path).Any(f => !FileExt.IsSystemFile(f));
    }
}
internal static class FileExt
{
    public static bool CreateSafe(string? path)
    {
        if (path == null) { return true; }
        try
        {
            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, []);
            }
            return true;
        }
        catch { }
        return false;
    }
    public static bool DeleteSafe(string? path)
    {
        if (path == null) { return false; }
        for (int i = 0; i < 3; i++)
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch { }
            try
            {
                var a = File.GetAttributes(path);
                a &= ~(FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System);
                File.SetAttributes(path, a);
            }
            catch { }
        }
        return false;
    }
    public static bool IsSystemFile(ReadOnlySpan<char> path)
    {
        var name = Path.GetFileName(path);
        return name.Equals("Thumbs.db", StringComparison.Ordinal) || name.Equals("desktop.ini", StringComparison.Ordinal);
    }
}
internal static class PathExt
{
    public static string GetVolume(string? path)
    {
        if (string.IsNullOrEmpty(path)) { return "C"; }
        return Path.GetPathRoot(path)?.TrimEnd('\\') ?? "C";
    }
}
