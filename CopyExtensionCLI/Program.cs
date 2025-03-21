using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CopyExtension
{
    internal class Program
    {
        [Verb("Copy", true, HelpText = "Copies files or folders and checks if the transfer was done correctly")]
        public class CopyOptions
        {
            //[Option('w', "windowsless")]
            //public bool WindowLess { get; set; }
            //[Option('p', "progress")]
            //public bool Progress { get; set; }
            //[Option('n', "noprompt")]
            //public bool NoPrompt { get; set; }
            //[Option('e', "ExistsAction")]
            //public ExistsAction ExistsAction { get; set; }
            //[Option('r', "retries")]
            //public int Retries { get; set; }
            [Value(0, Required = true, HelpText = "The folders or files to copy, separated by |")]
            public string Source { get; set; }
            [Value(1, Required = true, HelpText = "The target directory")]
            public string Destination { get; set; }
        }
        [Verb("Move", HelpText = "Moves files or folders and checks if the transfer was done correctly")]
        public class MoveOptions
        {
            [Value(0, Required = true, HelpText = "The folders or files to move, separated by |")]
            public string Source { get; set; }
            [Value(1, Required = true, HelpText = "The target directory")]
            public string Destination { get; set; }
        }
        [Verb("Compare", HelpText = "Compares files or folders and deletes identical files in the target directory")]
        public class CompareOptions
        {
            [Value(0, Required = true, HelpText = "The folders or files to compare, separated by |")]
            public string Source { get; set; }
            [Value(1, Required = true, HelpText = "The target directory, where files get deleted")]
            public string Destination { get; set; }
        }
        [Verb("Nuke", HelpText = "Nukes empty folders or files inside the target directories")]
        public class NukeOptions
        {
            [Option('f', "file", HelpText = "File mode deletes empty files (no data) instead of folders")]
            public bool FileMode { get; set; }
            [Option('d', "directory", HelpText = "Enable deleting empty folders even in file mode")]
            public bool DirectoryMode { get; set; }
            [Value(0, Required = true, HelpText = "The target directories, separated by |")]
            public string Target { get; set; }
        }
        [Verb("Custom", HelpText = "Runs a custom command")]
        public class CustomOptions
        {
            [Value(0, Required = true, HelpText = "The name of the custom command")]
            public string Name { get; set; }
            [Value(1, Required = false, HelpText = "Agruments for the custom command Context setting, separated by |")]
            public string Context { get; set; }
            [Value(2, Required = false, HelpText = "Arguments for the custom command Clipboard setting, separated by |")]
            public string Clipboard { get; set; }
        }
        public static void Main(string[] args)
        {
            var cli = Parser.Default.ParseArguments<CopyOptions, MoveOptions, CompareOptions, NukeOptions, CustomOptions>(args);
            cli.WithParsed<CopyOptions>(c => RunTask(new CopyMoveTask(c.Source.Split('|'), c.Destination, CopyJobType.Copy, null)));
            cli.WithParsed<MoveOptions>(c => RunTask(new CopyMoveTask(c.Source.Split('|'), c.Destination, CopyJobType.Move, null)));
            cli.WithParsed<CompareOptions>(c => RunTask(new CopyMoveTask(c.Source.Split('|'), c.Destination, CopyJobType.Compare, null)));
            cli.WithParsed<NukeOptions>(c =>
            {
                if (c.FileMode)
                {
                    RunTask(new NukeTask(c.Target.Split('|'), false, null));
                }
                if (c.DirectoryMode || c.FileMode == false)
                {
                    RunTask(new NukeTask(c.Target.Split('|'), true, null));
                }
            });
            cli.WithParsed<CustomOptions>(c =>
            {
                CopyExtension.Load();
                var command = CopyExtension.Options.CustomCommands?.FirstOrDefault(cc => cc.Name == c.Name);
                if (command != null)
                {
                    var context = c.Context?.Split('|') ?? Array.Empty<string>();
                    var clip = c.Clipboard?.Split('|') ?? Array.Empty<string>();
                    RunTask(command.ToTask(context, clip));
                }
            });
        }

        static bool RunTask(CopyTask task)
        {
            CopyExtension.Load();
            MainForm.AddTask(task);
            while (MainForm.IsBusy)
            {
                System.Threading.Thread.Sleep(1000);
            }
            return task.IsSuccess;
        }
    }
}
