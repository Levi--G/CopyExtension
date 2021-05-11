using System.Linq;

namespace CopyExtension
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var operation = args[0];
            var from = args.Skip(1).Take(args.Length - 2).ToArray();
            var to = args.Length > 2 ? args.Last() : null;

            CopyExtension.Load();

            if (operation.ToLower() == "copy")
            {
                MainForm.AddTask(new CopyMoveTask(from, to, CopyJobType.Copy));
            }
            else if (operation.ToLower() == "move")
            {
                MainForm.AddTask(new CopyMoveTask(from, to, CopyJobType.Move));
            }
            else if (operation.ToLower() == "compare")
            {
                MainForm.AddTask(new CopyMoveTask(from, to, CopyJobType.Compare));
            }
            else if (operation.ToLower() == "nuke")
            {
                MainForm.AddTask(new NukeTask(args.Skip(1).ToArray(), true));
            }
            else if (operation.ToLower() == "filenuke")
            {
                MainForm.AddTask(new NukeTask(args.Skip(1).ToArray(), false));
            }
        }
    }
}
