using CopyExtension.Helpers;
using CopyExtension.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CopyExtension;

public class Settings
{
    public bool ReserveWriting { get; set; } = true;
    public bool ReserveReading { get; set; } = false;
    public bool EnableCompareNames { get; set; } = true;
    public bool EnableHardlink { get; set; } = true;
    public bool EnableNukeFolder { get; set; } = true;
    public bool EnableNukeFile { get; set; } = true;
    public bool EnableCleanSource { get; set; } = false;
    public int SmallFileLimit { get; set; } = 10;
    public long SmallFileSize { get; set; } = 5 * 1024 * 1024;
    public bool EnableLogging { get; set; } = false;

    public CustomCommandSetting[] CustomCommands { get; set; } = [
        new CustomCommandSetting() {
            Name = "Move shortcuts to Desktop",
            Commands = new[] {
                new CustomCommand() {
                    Type = CustomCommand.CommandType.MoveUnsafe,
                    SourceType = CustomCommand.TargettingType.Context,
                    TargetType = CustomCommand.TargettingType.Static | CustomCommand.TargettingType.SpecialFolder,
                    Target = new[]{ "{Desktop}" },
                    Filters = new[]{ new Filter() { Allow=true, MatchName=true, Text=".lnk", Type= Filter.FilterType.EndsWith } }
                }
            }
        }
    ];

    public static Settings Load()
    {
        var path = SavePath;
        if (!File.Exists(path))
        {
            Save(new Settings());
        }
        using (var s = File.OpenRead(path))
        {
            return JsonSerializer.Deserialize(s, SourceGenerationContext.Default.Settings) ?? new();
        }
    }

    public static void Save(Settings settings)
    {
        var path = SavePath;
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        DirExt.CreateSafe(Path.GetDirectoryName(path));
        using (var s = File.OpenWrite(path))
        {
            JsonSerializer.Serialize(s, settings, SourceGenerationContext.Default.Settings);
        }
        SaveVerb(settings);
    }

    public static void SaveVerb(Settings settings)
    {
        CustomCommandSetting? Custom1 = settings.CustomCommands.FirstOrDefault(c => c.ExplorerId == 1);
        CustomCommandSetting? Custom2 = settings.CustomCommands.FirstOrDefault(c => c.ExplorerId == 2);
        CustomCommandSetting? Custom3 = settings.CustomCommands.FirstOrDefault(c => c.ExplorerId == 3);
        CustomCommandSetting? Custom4 = settings.CustomCommands.FirstOrDefault(c => c.ExplorerId == 4);
        CustomCommandSetting? Custom5 = settings.CustomCommands.FirstOrDefault(c => c.ExplorerId == 5);
        File.WriteAllText(VerbSavePath, $@"EnableCompareNames={(settings.EnableCompareNames ? "true" : "")}
EnableHardlink={(settings.EnableHardlink ? "true" : "")}
EnableNukeFolder={(settings.EnableNukeFolder ? "true" : "")}
EnableNukeFile={(settings.EnableNukeFile ? "true" : "")}
EnableCleanSource={(settings.EnableCleanSource ? "true" : "")}
CustomName1={Custom1?.Name}
CustomName2={Custom2?.Name}
CustomName3={Custom3?.Name}
CustomName4={Custom4?.Name}
CustomName5={Custom5?.Name}");
    }

    public static string SavePath => Path.Combine(Program.LocalAppDir, "settings.json");

    public static string VerbSavePath => Path.Combine(Program.LocalAppDir, "settings.txt");
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Settings))]
internal partial class SourceGenerationContext : JsonSerializerContext { }

public class CustomCommandSetting
{
    public required string Name { get; set; }
    public int? ExplorerId { get; set; }
    public CustomCommand[] Commands { get; set; } = [];

    public CopyTask? ToTask(string[] context, string[] clipboard, ExistsAction? defaultExistsAction)
    {
        if (Commands == null || Commands.Length == 0) { return null; }
        var cs = Commands.Select(c => c.ToTask(context, clipboard, defaultExistsAction)).ToArray();
        CopyTask? last = null;
        foreach (var c in cs)
        {
            last?.FollowUpTask = c;
            last = c;
        }
        return cs[0];
    }
}

public class CustomCommand
{
    public CommandType Type { get; set; }

    public enum CommandType
    { Copy, Move, Compare, Delete, NukeFolder, NukeFile, CopyUnsafe, MoveUnsafe, TouchDir, TouchFile }

    public TargettingType SourceType { get; set; }
    public TargettingType TargetType { get; set; }

    [Flags]
    public enum TargettingType
    { None = 0, Context = 1, Static = 2, SpecialFolder = 4, Environment = 8, Navigation = 16, Clipboard = 32, StaticRelativeToContext = 64, StaticRelativeToClipboard = 128 }

    public string[]? Source { get; set; }
    public string[]? Target { get; set; }
    public Filter[]? Filters { get; set; }

    public bool ExpandSource { get; set; }

    public CopyTask ToTask(string[] context, string[] clipboard, ExistsAction? defaultExistsAction)
    {
        switch (Type)
        {
            case CommandType.Copy:
                return new CopyMoveTask(GetSource(context, clipboard), GetTarget(context, clipboard).Single(), CopyJobType.Copy, Filters) { DefaultExistsAction = defaultExistsAction };

            case CommandType.Move:
                return new CopyMoveTask(GetSource(context, clipboard), GetTarget(context, clipboard).Single(), CopyJobType.Move, Filters) { DefaultExistsAction = defaultExistsAction };

            case CommandType.CopyUnsafe:
                return new CopyMoveTask(GetSource(context, clipboard), GetTarget(context, clipboard).Single(), CopyJobType.CopyUnsafe, Filters) { DefaultExistsAction = defaultExistsAction };

            case CommandType.MoveUnsafe:
                return new CopyMoveTask(GetSource(context, clipboard), GetTarget(context, clipboard).Single(), CopyJobType.MoveUnsafe, Filters) { DefaultExistsAction = defaultExistsAction };

            case CommandType.Compare:
                return new CopyMoveTask(GetSource(context, clipboard), GetTarget(context, clipboard).Single(), CopyJobType.Compare, Filters) { DefaultExistsAction = defaultExistsAction };

            case CommandType.Delete:
                break;

            case CommandType.NukeFolder:
                return new NukeTask(GetTarget(context, clipboard), true, Filters);

            case CommandType.NukeFile:
                return new NukeTask(GetTarget(context, clipboard), false, Filters);

            case CommandType.TouchDir:
                return new TouchTask(GetTarget(context, clipboard), true, Filters);

            case CommandType.TouchFile:
                return new TouchTask(GetTarget(context, clipboard), false, Filters);

            default:
                break;
        }
        throw new NotImplementedException();
    }

    public string[] GetSource(string[] context, string[] clipboard)
    {
        var s = GetTargetInternal(SourceType, Source, context, clipboard);
        if (ExpandSource)
        {
            s = s.SelectMany(ss =>
            {
                if (File.Exists(ss))
                {
                    return new[] { ss };
                }
                if (Directory.Exists(ss))
                {
                    return Directory.GetFileSystemEntries(ss);
                }
                return Array.Empty<string>();
            }).ToArray();
        }
        return s;
    }

    public string[] GetTarget(string[] context, string[] clipboard)
    {
        return GetTargetInternal(TargetType, Target, context, clipboard);
    }

    private string[] GetTargetInternal(TargettingType Type, string[]? text, string[] context, string[] clipboard)
    {
        IEnumerable<string> source = Array.Empty<string>();
        if (Type.HasFlag(TargettingType.Static) && text?.Length > 0)
        {
            source = source.Concat(text);
        }
        if (Type.HasFlag(TargettingType.StaticRelativeToContext) && text?.Length > 0)
        {
            source = source.Concat(text.SelectMany(t => context.Select(c => Path.Combine(c, t))));
        }
        if (Type.HasFlag(TargettingType.StaticRelativeToClipboard) && text?.Length > 0)
        {
            source = source.Concat(text.SelectMany(t => clipboard.Select(c => Path.Combine(c, t))));
        }
        if (Type.HasFlag(TargettingType.Context) && context?.Length > 0)
        {
            source = source.Concat(context);
        }
        if (Type.HasFlag(TargettingType.Clipboard) && clipboard?.Length > 0)
        {
            source = source.Concat(clipboard);
        }
        return source.Where(s => !string.IsNullOrEmpty(s)).Select(s => CreateTargetInternal(Type, s)).ToArray();
    }

    private string CreateTargetInternal(TargettingType Type, string text)
    {
        var source = text ?? string.Empty;
        if (Type.HasFlag(TargettingType.SpecialFolder))
        {
            source = RegexReplace(source, "{", "}", ReplaceSpecialFolder);
        }
        if (Type.HasFlag(TargettingType.Environment))
        {
            source = RegexReplace(source, "%", "%", ReplaceEnvironment);
        }
        if (Type.HasFlag(TargettingType.Navigation))
        {
            source = ReplaceNavigation(source);
        }
        return source;
    }

    private static string RegexReplace(string value, string start, string end, Func<string, string> replacement)
    {
        return Regex.Replace(value, Regex.Escape(start) + "[^" + Regex.Escape(start == end ? start : start + end) + "]*" + Regex.Escape(end), m => replacement(m.Value.Substring(start.Length, m.Value.Length - start.Length - end.Length)), RegexOptions.IgnoreCase);
    }

    private static string ReplaceSpecialFolder(string value)
    {
        if (Enum.TryParse(value, out Environment.SpecialFolder folder))
        {
            return Environment.GetFolderPath(folder);
        }
        return "C:";
    }

    private static string ReplaceEnvironment(string value)
    {
        return Environment.GetEnvironmentVariable(value) ?? "";
    }

    private static string ReplaceNavigation(string value)
    {
        value = value ?? string.Empty;
        var split = value.Split(new[] { "\\.." }, StringSplitOptions.None);
        var end = "";
        foreach (var s in split)
        {
            end = Path.GetDirectoryName(end) + s;
        }
        return end;
    }
}

public class Filter
{
    public bool Allow { get; set; } = false;
    public string? Text { get; set; }
    public FilterType Type { get; set; }

    public enum FilterType
    { Match, Contains, WildCard, Regex, StartsWith, EndsWith, IsFile }

    public bool InvertMatch { get; set; } = false;
    public bool DirectoryContents { get; set; } = false;
    public bool MatchName { get; set; } = false;

    public bool? Matches(string path)
    {
        Text ??= string.Empty;
        if (DirectoryContents)
        {
            string? dir = null;
            if (Directory.Exists(path))
            {
                dir = path;
            }
            if (File.Exists(path))
            {
                dir = Path.GetDirectoryName(path);
            }
            if (dir == null)
            {
                return false;
            }
            return Directory.GetFileSystemEntries(path).Select(MatchInternal).FirstOrDefault(e => e != null);
        }
        return MatchInternal(path);
    }

    private bool? MatchInternal(string path)
    {
        string toMatch = MatchName ? Path.GetFileName(path) : path;
        switch (Type)
        {
            case FilterType.Match:
                return (toMatch == Text != InvertMatch) ? Allow : (bool?)null;

            case FilterType.Contains:
                return (toMatch.Contains(Text) != InvertMatch) ? Allow : (bool?)null;

            case FilterType.WildCard:
                return (MatchRegex(toMatch, WildCardToRegular(Text)) != InvertMatch) ? Allow : (bool?)null;

            case FilterType.Regex:
                return (MatchRegex(toMatch, Text) != InvertMatch) ? Allow : (bool?)null;

            case FilterType.StartsWith:
                return (toMatch.StartsWith(Text) != InvertMatch) ? Allow : (bool?)null;

            case FilterType.EndsWith:
                return (toMatch.EndsWith(Text) != InvertMatch) ? Allow : (bool?)null;

            case FilterType.IsFile:
                return (File.Exists(path) != InvertMatch) ? Allow : (bool?)null;

            default:
                return null;
        }
    }

    private static string WildCardToRegular(string value)
    {
        return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
    }

    private static bool MatchRegex(string value, string regex)
    {
        return Regex.IsMatch(value, regex, RegexOptions.IgnoreCase);
    }
}