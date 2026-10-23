using CopyExtension.Helpers;
using CopyExtension.Tasks;
using CopyExtension.UI;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Text;

namespace CopyExtension;

internal class Cmd
{
    static WindowMgr? windowMgr;

    Command CreateCommand()
    {
        Option<bool> clipOption = new("--clip", "-c")
        {
            Description = "Use clipboard data as source",
            Recursive = true,
            Arity = ArgumentArity.Zero
        };
        Option<string[]> sourceOption = new("--source", "-s")
        {
            Description = "The source of the files to copy or process",
            Recursive = true,
            Arity = ArgumentArity.OneOrMore,
            AllowMultipleArgumentsPerToken = true
        };
        sourceOption.AcceptLegalFilePathsOnly();
        Option<bool> logOption = new("--log", "-l")
        {
            Description = "(Debug) Opens the log file for error analysis",
            Recursive = true,
            Arity = ArgumentArity.Zero
        };
        Option<bool> optionsOption = new("--options", "-o")
        {
            Description = "(Debug) Opens the options file",
            Recursive = true,
            Arity = ArgumentArity.Zero
        };
        Option<bool> consoleOption = new("--console", "-n")
        {
            Description = "(Debug) Opens a console window",
            Recursive = true,
            Arity = ArgumentArity.Zero
        };
        Option<bool> waitOption = new("--waitforexit", "-e")
        {
            Description = "(Debug) Waits for input before exiting the console to give a chance to read the output, requires -n",
            Recursive = true,
            Arity = ArgumentArity.Zero
        };
        waitOption.Validators.Add(r => { if (r.GetValue(waitOption) == true && r.GetValue(consoleOption) != true) { r.AddError("-e (--waitforexit) needs -n (--console) to work"); } });
        Argument<string> destinationArgument = new("destination")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        destinationArgument.AcceptLegalFilePathsOnly();
        Argument<string?> nonreqdestinationArgument = new("destination")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        nonreqdestinationArgument.AcceptLegalFilePathsOnly();


        Option<ExistsAction?> existsOption = new("--exists", "-x")
        {
            Description = "Sets a default action to take when encountering already existing files",
            Arity = ArgumentArity.ExactlyOne
        };
        Option<bool> unsafeOption = new("--unsafe")
        {
            Description = "(Caution) Moves/copies without verifying but still using all features (not recommended)",
            Arity = ArgumentArity.Zero
        };
        Option<bool> namesOption = new("--names")
        {
            Description = "(Caution) Only checks names of files, not the data contained. Warning! This will delete data that is not equal but only matches in filename!",
            Arity = ArgumentArity.Zero
        };
        Option<bool> filesOption = new("--files", "-f")
        {
            Description = "Delete empty files with 0 bytes in the source directories",
            Arity = ArgumentArity.Zero
        };
        Option<bool> dirsOption = new("--directories", "-d")
        {
            Description = "Delete empty directories in the source directories",
            Arity = ArgumentArity.Zero
        };
        Option<bool> devOption = new("--dev", "-v")
        {
            Description = "Detetes .vs, obj, bin, .pio folders from c# or pio projects",
            Arity = ArgumentArity.Zero
        };
        Option<string> nameOption = new("--name", "-a")
        {
            Description = "The name of the custom command to execute",
            Arity = ArgumentArity.ExactlyOne
        };
        Option<int?> idOption = new("--id", "-i")
        {
            Description = "The explorerid of the custom command to execute",
            Arity = ArgumentArity.ExactlyOne
        };
        Option<int> secondsOption = new("--seconds", "-i")
        {
            Description = "How many seconds to wait, defaults to 5",
            Arity = ArgumentArity.ExactlyOne,
            DefaultValueFactory = r => 5
        };

        //[Option('w', "windowsless")]
        //public bool WindowLess { get; set; }

        RootCommand rootCommand = new("CopyExtension CLI is a collection of various tools copying, moving, comparing, nuking, ... files. Also available with an explorer shell extension.");
        rootCommand.Options.Add(consoleOption);
        rootCommand.Options.Add(clipOption);
        rootCommand.Options.Add(sourceOption);
        rootCommand.Options.Add(logOption);
        rootCommand.Options.Add(optionsOption);
        rootCommand.Options.Add(waitOption);
        Action<CommandResult> sourcevalidator = item =>
        {
            if (!item.GetValue(clipOption) && (item.GetValue(sourceOption)?.Length ?? 0) < 1)
            {
                item.AddError("No source provided. One of -c (--clip) or -s (--source) needs to be supplied as source for the commands.");
            }
        };

        Command copyCommand = new("copy", "Copies files and verifies integrity");
        copyCommand.Validators.Add(sourcevalidator);
        copyCommand.Arguments.Add(destinationArgument);
        copyCommand.Options.Add(existsOption);
        copyCommand.Options.Add(unsafeOption);
        Command moveCommand = new("move", "Copies files and verifies integrity before deleting source files");
        moveCommand.Validators.Add(sourcevalidator);
        moveCommand.Arguments.Add(destinationArgument);
        moveCommand.Options.Add(existsOption);
        moveCommand.Options.Add(unsafeOption);
        Command compareCommand = new("compare", "Compares the files and when verified deletes the one in the destination");
        compareCommand.Validators.Add(sourcevalidator);
        compareCommand.Arguments.Add(destinationArgument);
        compareCommand.Options.Add(namesOption);
        Command nukeCommand = new("nuke", "Nukes specific files, empty files or empty directories in the source folders");
        nukeCommand.Validators.Add(sourcevalidator);
        nukeCommand.Options.Add(filesOption);
        nukeCommand.Options.Add(dirsOption);
        nukeCommand.Options.Add(devOption);
        nukeCommand.Validators.Add(item => { if (!item.GetValue(filesOption) && !item.GetValue(dirsOption) && !item.GetValue(devOption)) { item.AddError("No operation provided. One of -f -d or -v needs to be supplied to the nuke command."); } });
        Command waitCommand = new("wait", "Waits for a number of seconds while locking volumes");
        waitCommand.Arguments.Add(nonreqdestinationArgument);
        waitCommand.Options.Add(secondsOption);
        Command hardlinkCommand = new("hardlink", "Hardlinks full file and directory structures");
        hardlinkCommand.Validators.Add(sourcevalidator);
        hardlinkCommand.Arguments.Add(destinationArgument);

        Command customCommand = new("custom", "Execute custom actions");
        customCommand.Arguments.Add(destinationArgument);
        customCommand.Options.Add(existsOption);
        customCommand.Options.Add(nameOption);
        customCommand.Options.Add(idOption);
        customCommand.Validators.Add(item => { if (string.IsNullOrEmpty(item.GetValue(nameOption)) && (item.GetValue(idOption) ?? 0) < 1) { item.AddError("No operation provided. One of -n or -i needs to be supplied to the custom command."); } });

        rootCommand.Subcommands.Add(copyCommand);
        rootCommand.Subcommands.Add(moveCommand);
        rootCommand.Subcommands.Add(compareCommand);
        rootCommand.Subcommands.Add(nukeCommand);
        rootCommand.Subcommands.Add(waitCommand);
        rootCommand.Subcommands.Add(customCommand);

        void GetOptions(ParseResult parseResult, out bool clip, out string[]? source, out bool console, out bool log, out bool options)
        {
            clip = parseResult.GetValue(clipOption);
            source = parseResult.GetValue(sourceOption);
            console = parseResult.GetValue(consoleOption);
            log = parseResult.GetValue(logOption);
            options = parseResult.GetValue(optionsOption);
        }

        copyCommand.SetAction(parseResult =>
        {
            GetOptions(parseResult, out var clip, out var source, out var console, out var log, out var options);
            DoExtraPre(parseResult, log, options);
            var destination = parseResult.GetValue(destinationArgument) ?? throw new ArgumentException("destination is mandatory");
            var exists = parseResult.GetValue(existsOption);
            var unsafeo = parseResult.GetValue(unsafeOption);
            windowMgr ??= new(console);
            windowMgr.Start();
            windowMgr.AddTask(new CopyMoveTask(GetSource(windowMgr, source, clip), destination, unsafeo ? CopyJobType.CopyUnsafe : CopyJobType.Copy, null) { DefaultExistsAction = exists });
            DoExtraPost(parseResult, parseResult.GetValue(waitOption));
        });

        moveCommand.SetAction(parseResult =>
        {
            GetOptions(parseResult, out var clip, out var source, out var console, out var log, out var options);
            DoExtraPre(parseResult, log, options);
            var destination = parseResult.GetValue(destinationArgument) ?? throw new ArgumentException("destination is mandatory");
            var exists = parseResult.GetValue(existsOption);
            var unsafeo = parseResult.GetValue(unsafeOption);
            windowMgr ??= new(console);
            windowMgr.Start();
            windowMgr.AddTask(new CopyMoveTask(GetSource(windowMgr, source, clip), destination, unsafeo ? CopyJobType.MoveUnsafe : CopyJobType.Move, null) { DefaultExistsAction = exists });
            DoExtraPost(parseResult, parseResult.GetValue(waitOption));
        });

        compareCommand.SetAction(parseResult =>
        {
            GetOptions(parseResult, out var clip, out var source, out var console, out var log, out var options);
            DoExtraPre(parseResult, log, options);
            var destination = parseResult.GetValue(destinationArgument) ?? throw new ArgumentException("destination is mandatory");
            var names = parseResult.GetValue(namesOption);
            windowMgr ??= new(console);
            windowMgr.Start();
            windowMgr.AddTask(new CopyMoveTask(GetSource(windowMgr, source, clip), destination, names ? CopyJobType.CompareNames : CopyJobType.Compare, null));
            DoExtraPost(parseResult, parseResult.GetValue(waitOption));
        });

        nukeCommand.SetAction(parseResult =>
        {
            GetOptions(parseResult, out var clip, out var source, out var console, out var log, out var options);
            DoExtraPre(parseResult, log, options);
            windowMgr ??= new(console);
            windowMgr.Start();
            if (parseResult.GetValue(devOption))
            {
                windowMgr.AddTask(new SourceTask(GetSource(windowMgr, source, clip), null));
            }
            if (parseResult.GetValue(filesOption))
            {
                windowMgr.AddTask(new NukeTask(GetSource(windowMgr, source, clip), false, null));
            }
            if (parseResult.GetValue(dirsOption))
            {
                windowMgr.AddTask(new NukeTask(GetSource(windowMgr, source, clip), true, null));
            }
            DoExtraPost(parseResult, parseResult.GetValue(waitOption));
        });

        waitCommand.SetAction(parseResult =>
        {
            GetOptions(parseResult, out var clip, out var source, out var console, out var log, out var options);
            DoExtraPre(parseResult, log, options);
            var seconds = parseResult.GetValue(secondsOption);
            var destination = parseResult.GetValue(nonreqdestinationArgument);
            windowMgr ??= new(console);
            windowMgr.Start();
            var fullsource = GetSource(windowMgr, source, clip);
            var rvol = fullsource.Length > 0 ? PathExt.GetVolume(fullsource.First()) : null;
            var wvol = destination != null ? PathExt.GetVolume(fullsource.First()) : null;
            windowMgr.AddTask(new WaitTask(seconds, rvol, wvol, null));
            DoExtraPost(parseResult, parseResult.GetValue(waitOption));
        });

        hardlinkCommand.SetAction(parseResult =>
        {
            GetOptions(parseResult, out var clip, out var source, out var console, out var log, out var options);
            DoExtraPre(parseResult, log, options);
            var destination = parseResult.GetValue(destinationArgument) ?? throw new ArgumentException("destination is mandatory");
            windowMgr ??= new(console);
            windowMgr.Start();
            windowMgr.AddTask(new HardlinkTask(GetSource(windowMgr, source, clip), destination, null));
            DoExtraPost(parseResult, parseResult.GetValue(waitOption));
        });

        customCommand.SetAction(parseResult =>
        {
            GetOptions(parseResult, out var clip, out var source, out var console, out var log, out var options);
            DoExtraPre(parseResult, log, options);
            var destination = parseResult.GetValue(destinationArgument) ?? throw new ArgumentException("destination is mandatory");
            var exists = parseResult.GetValue(existsOption);
            var name = parseResult.GetValue(nameOption);
            var id = parseResult.GetValue(idOption);
            CustomCommandSetting? command = null;
            if (!string.IsNullOrEmpty(name))
            {
                command = Program.Options.CustomCommands.FirstOrDefault(c => c.Name == name);
            }
            if ((id ?? 0) > 0)
            {
                command ??= Program.Options.CustomCommands.FirstOrDefault(c => c.ExplorerId == id);
            }
            if (command == null) { throw new Exception("-n or -i must be supplied"); }
            windowMgr ??= new(console);
            windowMgr.Start();
            var t = command.ToTask(GetSource(windowMgr, [destination], false), GetSource(windowMgr, source, true), exists);
            if (t != null)
            {
                windowMgr.AddTask(t);
            }
            DoExtraPost(parseResult, parseResult.GetValue(waitOption));
        });

        return rootCommand;
    }

    void DoExtraPre(ParseResult result, bool log, bool options)
    {
        if (log)
        {
            Program.Logger.Log("Opening log file");
            if (File.Exists(Program.Logger.LogFile))
            {
                Process.Start(new ProcessStartInfo(Program.Logger.LogFile) { UseShellExecute = true, CreateNewProcessGroup = true });
            }
            else
            {
                Console.WriteLine("Logging is not enabled, enable logging in settings.");
            }
        }
        if (options)
        {
            Settings.Save(Program.Options);
            if (File.Exists(Settings.SavePath))
            {
                Process.Start(new ProcessStartInfo(Settings.SavePath) { UseShellExecute = true, CreateNewProcessGroup = true });
            }
            else
            {
                Console.WriteLine("Settings file is not found?");
            }
        }
    }

    void DoExtraPost(ParseResult result, bool wait)
    {
        windowMgr?.WaitForExit();
        if (wait)
        {
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
        }
        windowMgr?.Finish();
    }

    public ParseResult Parse(string commandline)
    {
        return CreateCommand().Parse(commandline);
    }

    public ParseResult Parse(string[] args)
    {
        return CreateCommand().Parse(args);
    }

    public void Execute(ParseResult result)
    {
        result.Invoke();
    }

    static string[] GetSource(WindowMgr windowMgr, string[]? source, bool clipboard)
    {
        if (clipboard)
        {
            var files = windowMgr.GetClipboardFiles();
            if (source != null && source.Length > 0)
            {
                files.AddRange(source.SelectMany(s => s.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));
            }
            return files.ToArray();
        }
        return source?.SelectMany(s => s.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToArray() ?? [];
    }

    public static string GetCommandline(string[] args)
    {
        var stringBuilder = new StringBuilder();

        foreach (string argument in args)
        {
            AppendArgument(stringBuilder, argument);
        }

        return stringBuilder.ToString();
    }

    public static string[] SplitCommandline(string args)
    {
        return CommandLineParser.SplitCommandLine(args).ToArray();
    }

    private const char Quote = '\"';
    private const char Backslash = '\\';
    public static void AppendArgument(StringBuilder stringBuilder, string argument)
    {
        if (stringBuilder.Length != 0)
        {
            stringBuilder.Append(' ');
        }

        if (argument.Length != 0 && ContainsNoWhitespaceOrQuotes(argument))
        {
            stringBuilder.Append(argument);
        }
        else
        {
            stringBuilder.Append(Quote);
            int idx = 0;
            while (idx < argument.Length)
            {
                char c = argument[idx++];
                if (c == Backslash)
                {
                    int numBackSlash = 1;
                    while (idx < argument.Length && argument[idx] == Backslash)
                    {
                        idx++;
                        numBackSlash++;
                    }

                    if (idx == argument.Length)
                    {
                        stringBuilder.Append(Backslash, numBackSlash * 2);
                    }
                    else if (argument[idx] == Quote)
                    {
                        stringBuilder.Append(Backslash, numBackSlash * 2 + 1);
                        stringBuilder.Append(Quote);
                        idx++;
                    }
                    else
                    {
                        stringBuilder.Append(Backslash, numBackSlash);
                    }

                    continue;
                }

                if (c == Quote)
                {
                    stringBuilder.Append(Backslash);
                    stringBuilder.Append(Quote);
                    continue;
                }

                stringBuilder.Append(c);
            }

            stringBuilder.Append(Quote);
        }
    }

    private static bool ContainsNoWhitespaceOrQuotes(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsWhiteSpace(c) || c == Quote)
            {
                return false;
            }
        }

        return true;
    }
}