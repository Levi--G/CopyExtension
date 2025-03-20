using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using ZetaLongPaths;

namespace CopyExtension
{
    public class Settings
    {
        public bool ReserveWriting { get; set; } = true;
        public bool ReserveReading { get; set; } = false;
        public bool EnableCopy { get; set; } = true;
        public bool EnableMove { get; set; } = true;
        public bool EnableCompare { get; set; } = true;
        public bool EnableHardlink { get; set; } = true;
        public bool EnableNukeFolder { get; set; } = true;
        public bool EnableNukeFile { get; set; } = true;
        public int SmallFileLimit { get; set; } = 10;
        public long SmallFileSize { get; set; } = 5 * 1024 * 1024;

        public CustomCommandSetting[] CustomCommands { get; set; } = new CustomCommandSetting[] {
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
        };

        public static Settings Load()
        {
            var path = SavePath;
            var ser = new XmlSerializer(typeof(Settings));
            if (!File.Exists(path))
            {
                Save(new Settings());
            }
            using (var s = File.OpenRead(path))
            {
                return (Settings)ser.Deserialize(s);
            }
        }

        public static void Save(Settings settings)
        {
            var path = SavePath;
            var ser = new XmlSerializer(typeof(Settings));
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            using (var s = File.OpenWrite(path))
            {
                ser.Serialize(s, settings);
            }
        }

        public static string SavePath => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.xml");
    }

    public class CustomCommandSetting
    {
        public string Name { get; set; }
        public CustomCommand[] Commands { get; set; } = new CustomCommand[] { };

        public CopyTask ToTask(string[] context, string[] clipboard)
        {
            if (Commands == null || Commands.Length == 0) { return null; }
            var cs = Commands.Select(c => c.ToTask(context, clipboard)).ToArray();
            CopyTask last = null;
            foreach (var c in cs)
            {
                if (last != null)
                {
                    last.FollowUpTask = c;
                }
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

        public string[] Source { get; set; }
        public string[] Target { get; set; }
        public Filter[] Filters { get; set; }

        public bool ExpandSource { get; set; }

        public CopyTask ToTask(string[] context, string[] clipboard)
        {
            switch (Type)
            {
                case CommandType.Copy:
                    return new CopyMoveTask(GetSource(context, clipboard), GetTarget(context, clipboard).Single(), CopyJobType.Copy, Filters);

                case CommandType.Move:
                    return new CopyMoveTask(GetSource(context, clipboard), GetTarget(context, clipboard).Single(), CopyJobType.Move, Filters);

                case CommandType.CopyUnsafe:
                    return new CopyMoveTask(GetSource(context, clipboard), GetTarget(context, clipboard).Single(), CopyJobType.CopyUnsafe, Filters);

                case CommandType.MoveUnsafe:
                    return new CopyMoveTask(GetSource(context, clipboard), GetTarget(context, clipboard).Single(), CopyJobType.MoveUnsafe, Filters);

                case CommandType.Compare:
                    return new CopyMoveTask(GetSource(context, clipboard), GetTarget(context, clipboard).Single(), CopyJobType.Compare, Filters);

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
                    if (ZlpIOHelper.FileExists(ss))
                    {
                        return new[] { ss };
                    }
                    if (ZlpIOHelper.DirectoryExists(ss))
                    {
                        return ZlpIOHelper.GetFileSystemInfos(ss).Select(f => f.FullName);
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

        private string[] GetTargetInternal(TargettingType Type, string[] text, string[] context, string[] clipboard)
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
            return Environment.GetEnvironmentVariable(value);
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
        public string Text { get; set; }
        public FilterType Type { get; set; }

        public enum FilterType
        { Match, Contains, WildCard, Regex, StartsWith, EndsWith, IsFile }

        public bool InvertMatch { get; set; } = false;
        public bool DirectoryContents { get; set; } = false;
        public bool MatchName { get; set; } = false;

        public bool? Matches(IZlpFileSystemInfo path)
        {
            Text = Text ?? string.Empty;
            if (DirectoryContents)
            {
                ZlpDirectoryInfo dir = null;
                if (path is ZlpDirectoryInfo d)
                {
                    dir = d;
                }
                if (path is ZlpFileInfo f)
                {
                    dir = f.Directory;
                }
                return dir.GetFileSystemInfos().Select(MatchInternal).FirstOrDefault(e => e != null);
            }
            return MatchInternal(path);
        }

        private bool? MatchInternal(IZlpFileSystemInfo path)
        {
            string toMatch = MatchName ? path.Name : path.FullName;
            switch (Type)
            {
                case FilterType.Match:
                    return ((toMatch == Text) != InvertMatch) ? Allow : (bool?)null;

                case FilterType.Contains:
                    return ((toMatch.Contains(Text)) != InvertMatch) ? Allow : (bool?)null;

                case FilterType.WildCard:
                    return ((MatchRegex(toMatch, WildCardToRegular(Text))) != InvertMatch) ? Allow : (bool?)null;

                case FilterType.Regex:
                    return ((MatchRegex(toMatch, Text)) != InvertMatch) ? Allow : (bool?)null;

                case FilterType.StartsWith:
                    return ((toMatch.StartsWith(Text)) != InvertMatch) ? Allow : (bool?)null;

                case FilterType.EndsWith:
                    return ((toMatch.EndsWith(Text)) != InvertMatch) ? Allow : (bool?)null;

                case FilterType.IsFile:
                    return ((path is ZlpFileInfo) != InvertMatch) ? Allow : (bool?)null;

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
}