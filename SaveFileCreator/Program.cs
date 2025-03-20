using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CopyExtension;

namespace SaveFileCreator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Settings s = new Settings();
            s.CustomCommands = new[]
            {
                new CustomCommandSetting()
                {
                    Name = "Test",
                    Commands = new[]
                    {
                        new CustomCommand()
                        {
                            Type = CustomCommand.CommandType.Copy,
                            SourceType = CustomCommand.TargettingType.Context,
                            TargetType = CustomCommand.TargettingType.Static,
                            Target = new[] { @"D:\Target1" },
                            Filters = new[]
                            {
                                new Filter()
                                {
                                    Allow = false,
                                    Text = ".txt",
                                    MatchName = true,
                                    Type = Filter.FilterType.EndsWith
                                }
                            }
                        },
                        new CustomCommand()
                        {
                            Type = CustomCommand.CommandType.MoveUnsafe,
                            SourceType = CustomCommand.TargettingType.Context,
                            TargetType = CustomCommand.TargettingType.Static,
                            Target = new[] { @"D:\Target2" }
                        },
                        new CustomCommand()
                        {
                            Type = CustomCommand.CommandType.NukeFolder,
                            TargetType = CustomCommand.TargettingType.Static,
                            Target = new[] { @"D:\Target3" }
                        }
                    }
                }
            };
            Settings.Save(s);
        }
    }
}
