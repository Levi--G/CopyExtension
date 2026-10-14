using Avalonia.Data;
using Avalonia.Reactive;
using CommandLine;
using CopyExtension.Helpers;
using CopyExtension.Tasks;
using CopyExtension.UI;
using CopyExtensionAOT;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace CopyExtension;

internal class Cmd : ICmd
{
    static WindowMgr? windowMgr;
    public void Execute(string[] args)
    {
        //CopyExtension.Logger.Log(this, $"starting with cmd: {Environment.CommandLine}");
        bool wait = false;
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
            Arity = ArgumentArity.Zero,
            Action = new DelegateCommandLineAction(pr =>
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
            })
        };
        Option<bool> optionsOption = new("--options", "-o")
        {
            Description = "(Debug) Opens the options file",
            Recursive = true,
            Arity = ArgumentArity.Zero,
            Action = new DelegateCommandLineAction(pr =>
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
            })
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
            Arity = ArgumentArity.Zero,
            Action = new DelegateCommandLineAction(pr =>
            {
                wait = true;
            }),
        };
        waitOption.Validators.Add(r => { if (r.GetValue(waitOption) == true && r.GetValue(consoleOption) != true) { r.AddError("-e (--waitforexit) needs -n (--console) to work"); } });
        Argument<string> destinationArgument = new("destination")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        destinationArgument.AcceptLegalFilePathsOnly();


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
        rootCommand.Subcommands.Add(copyCommand);
        rootCommand.Subcommands.Add(moveCommand);
        rootCommand.Subcommands.Add(compareCommand);
        rootCommand.Subcommands.Add(nukeCommand);

        copyCommand.SetAction(parseResult =>
        {
            var clip = parseResult.GetValue(clipOption);
            var source = parseResult.GetValue(sourceOption);
            var destination = parseResult.GetValue(destinationArgument) ?? throw new ArgumentException("destination is mandatory");
            var console = parseResult.GetValue(consoleOption);
            var exists = parseResult.GetValue(existsOption);
            var unsafeo = parseResult.GetValue(unsafeOption);
            windowMgr ??= new(console);
            windowMgr.Start();
            windowMgr.AddTask(new CopyMoveTask(GetSource(windowMgr, source, clip), destination, unsafeo ? CopyJobType.CopyUnsafe : CopyJobType.Copy, null) { DefaultExistsAction = exists });
            windowMgr.AllowQuit();
        });

        moveCommand.SetAction(parseResult =>
        {
            var clip = parseResult.GetValue(clipOption);
            var source = parseResult.GetValue(sourceOption);
            var destination = parseResult.GetValue(destinationArgument) ?? throw new ArgumentException("destination is mandatory");
            var console = parseResult.GetValue(consoleOption);
            var exists = parseResult.GetValue(existsOption);
            var unsafeo = parseResult.GetValue(unsafeOption);
            windowMgr ??= new(console);
            windowMgr.Start();
            windowMgr.AddTask(new CopyMoveTask(GetSource(windowMgr, source, clip), destination, unsafeo ? CopyJobType.MoveUnsafe : CopyJobType.Move, null) { DefaultExistsAction = exists });
            windowMgr.AllowQuit();
        });

        compareCommand.SetAction(parseResult =>
        {
            var clip = parseResult.GetValue(clipOption);
            var source = parseResult.GetValue(sourceOption);
            var destination = parseResult.GetValue(destinationArgument) ?? throw new ArgumentException("destination is mandatory");
            var console = parseResult.GetValue(consoleOption);
            var names = parseResult.GetValue(namesOption);
            windowMgr ??= new(console);
            windowMgr.Start();
            windowMgr.AddTask(new CopyMoveTask(GetSource(windowMgr, source, clip), destination, names ? CopyJobType.CompareNames : CopyJobType.Compare, null));
            windowMgr.AllowQuit();
        });

        nukeCommand.SetAction(parseResult =>
        {
            var clip = parseResult.GetValue(clipOption);
            var source = parseResult.GetValue(sourceOption);
            var console = parseResult.GetValue(consoleOption);
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
            windowMgr.AllowQuit();
        });

        //hardlink

        //custom

        rootCommand.Parse(args).Invoke();

        windowMgr?.WaitForExit();

        if (wait)
        {
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
        }
        windowMgr?.Finish();
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
}

sealed class DelegateCommandLineAction : SynchronousCommandLineAction
{
    private readonly Action<ParseResult> _callback;

    public DelegateCommandLineAction(Action<ParseResult> callback)
    {
        _callback = callback;
    }

    public override bool Terminating => false;

    public override int Invoke(ParseResult parseResult)
    {
        _callback(parseResult);
        return 0;
    }
}
