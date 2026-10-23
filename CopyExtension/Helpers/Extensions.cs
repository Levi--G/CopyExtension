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
}
internal static class DirExt
{
    public static void CreateSafe(string? path)
    {
        if (path == null) { return; }
        try
        {
            Directory.CreateDirectory(path);
        }
        catch { }
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
        }
        return false;
    }
}
internal static class FileExt
{
    public static void CreateSafe(string? path)
    {
        if (path == null) { return; }
        try
        {
            File.WriteAllBytes(path, []);
            //File.Create(path).Dispose();
        }
        catch { }
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
        }
        return false;
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
