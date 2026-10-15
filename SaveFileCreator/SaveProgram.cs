using CopyExtension;

namespace SaveFileCreator
{
    internal class SaveProgram
    {
        static void Main(string[] args)
        {
            Settings s = new Settings
            {
                EnableLogging = true,
                EnableCleanSource = true,
                ReserveWriting = true,
                ReserveReading = true,
                CustomCommands =
                [
                    new CustomCommandSetting()
                    {
                        Name = "Copy Series to VTU",
                        ExplorerId = 1,
                        Commands =
                        [
                            new CustomCommand()
                            {
                                Type = CustomCommand.CommandType.Copy,
                                SourceType = CustomCommand.TargettingType.Context,
                                TargetType = CustomCommand.TargettingType.Static,
                                Target = [@"P:\VTU\VideoSourceFolder"],
                                Filters =
                                [
                                    new Filter()
                                    {
                                        Allow = false,
                                        Text = ".vjob",
                                        MatchName = true,
                                        Type = Filter.FilterType.EndsWith
                                    }
                                ]
                            },
                            new CustomCommand()
                            {
                                Type = CustomCommand.CommandType.MoveUnsafe,
                                SourceType = CustomCommand.TargettingType.Context,
                                TargetType = CustomCommand.TargettingType.Static,
                                Target = [@"\\HOMESERVER\Videos\Series_Busy"]
                            },
                            new CustomCommand()
                            {
                                Type = CustomCommand.CommandType.NukeFolder,
                                TargetType = CustomCommand.TargettingType.Static,
                                Target = [@"\\HOMESERVER\Videos\Series_Todo"]
                            }
                        ]
                    },
                    new CustomCommandSetting()
                    {
                        Name = "Copy Movies to VTU",
                        ExplorerId = 2,
                        Commands =
                        [
                            new CustomCommand()
                            {
                                Type = CustomCommand.CommandType.Copy,
                                SourceType = CustomCommand.TargettingType.Context,
                                TargetType = CustomCommand.TargettingType.Static,
                                Target = [@"P:\VTU\VideoSourceFolder"],
                                Filters =
                                [
                                    new Filter()
                                    {
                                        Allow = false,
                                        Text = ".vjob",
                                        MatchName = true,
                                        Type = Filter.FilterType.EndsWith
                                    }
                                ]
                            },
                            new CustomCommand()
                            {
                                Type = CustomCommand.CommandType.TouchFile,
                                TargetType = CustomCommand.TargettingType.StaticRelativeToContext,
                                Target = [@"Levi busy.txt"]
                            }
                        ]
                    },
                    new CustomCommandSetting()
                    {
                        Name = "Copy Done to server",
                        ExplorerId = 3,
                        Commands =
                        [
                            new CustomCommand()
                            {
                                Type = CustomCommand.CommandType.Move,
                                SourceType = CustomCommand.TargettingType.Static,
                                Source = [@"P:\VTU\Movies"],
                                TargetType = CustomCommand.TargettingType.Static,
                                Target = [@"\\HOMESERVER\Videos\Series_Done"],
                                ExpandSource = true
                            },
                            new CustomCommand()
                            {
                                Type = CustomCommand.CommandType.NukeFolder,
                                TargetType = CustomCommand.TargettingType.Static,
                                Target = [@"P:\VTU\Movies"]
                            }
                        ]
                    }
                ]
            };
            Settings.Save(s);
        }
    }
}
