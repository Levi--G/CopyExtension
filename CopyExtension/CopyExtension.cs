﻿using SharpShell.Attributes;
using SharpShell.SharpContextMenu;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CopyExtension
{
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.Directory)]
    [COMServerAssociation(AssociationType.DirectoryBackground)]
    [COMServerAssociation(AssociationType.Drive)]
    public partial class CopyExtension : SharpContextMenu
    {
        public static Settings Options { get; private set; }

        public static Logger Logger { get; } = new Logger();

        public static void Load()
        {
            if (Options == null)
            {
                Options = Settings.Load();
            }
        }

        public CopyExtension()
        {
            Load();
        }

        protected override bool CanShowMenu()
        {
            return true;
        }

        protected override ContextMenuStrip CreateMenu()
        {
            //  Create the menu strip.
            var menu = new ContextMenuStrip();

            //  Create an item.
            var item = new ToolStripMenuItem
            {
                Text = "Safe Copy",
                Image = Image.FromStream(new MemoryStream(Properties.Resources.Copy))
            };

            if (DataOnClipboard && FolderExists && !IsMultiPath)
            {
                if (Options.EnableMove)
                {
                    var move = new ToolStripMenuItem
                    {
                        Text = "Move here"
                    };

                    move.Click += Move_Click;

                    item.DropDownItems.Add(move);
                }

                if (Options.EnableCopy)
                {
                    var copy = new ToolStripMenuItem
                    {
                        Text = "Copy here"
                    };

                    copy.Click += Copy_Click;

                    item.DropDownItems.Add(copy);
                }

                if (Options.EnableCompare)
                {
                    var comp = new ToolStripMenuItem
                    {
                        Text = "Compare here"
                    };

                    comp.Click += Comp_Click;

                    item.DropDownItems.Add(comp);
                }

                if (Options.EnableHardlink)
                {
                    var copy = new ToolStripMenuItem
                    {
                        Text = "Create hardlinks"
                    };

                    copy.Click += Hardlinks_Click;

                    item.DropDownItems.Add(copy);
                }
            }
            if (Options.EnableNukeFolder)
            {
                var folder = new ToolStripMenuItem
                {
                    Text = "Nuke Empty Folders"
                };

                folder.Click += Folder_Click;

                item.DropDownItems.Add(folder);
            }

            if (Options.EnableNukeFile)
            {
                var files = new ToolStripMenuItem
                {
                    Text = "Nuke Empty Files"
                };

                files.Click += Files_Click;

                item.DropDownItems.Add(files);
            }

            //  Add the item to the context menu.
            menu.Items.Add(item);

            //  Return the menu.
            return menu;
        }

        private void Copy_Click(object sender, EventArgs e)
        {
            if (DataOnClipboard && FolderExists)
            {
                MainForm.AddTask(new CopyMoveTask(Clipboard.GetFileDropList().Cast<string>().ToArray(), SingleFolderPath, CopyJobType.Copy));
            }
        }

        private void Move_Click(object sender, EventArgs e)
        {
            if (DataOnClipboard && FolderExists)
            {
                MainForm.AddTask(new CopyMoveTask(Clipboard.GetFileDropList().Cast<string>().ToArray(), SingleFolderPath, CopyJobType.Move));
            }
        }

        private void Comp_Click(object sender, EventArgs e)
        {
            if (DataOnClipboard && FolderExists)
            {
                MainForm.AddTask(new CopyMoveTask(Clipboard.GetFileDropList().Cast<string>().ToArray(), SingleFolderPath, CopyJobType.Compare));
            }
        }

        private void Hardlinks_Click(object sender, EventArgs e)
        {
            if (DataOnClipboard && FolderExists)
            {
                MainForm.AddTask(new HardlinkTask(Clipboard.GetFileDropList().Cast<string>().ToArray(), SingleFolderPath));
            }
        }

        private void Folder_Click(object sender, EventArgs e)
        {
            if (FolderExists)
            {
                MainForm.AddTask(new NukeTask(MultiFolderPaths, true));
            }
        }

        private void Files_Click(object sender, EventArgs e)
        {
            if (FolderExists)
            {
                MainForm.AddTask(new NukeTask(MultiFolderPaths, false));
            }
        }

        private bool DataOnClipboard => Clipboard.GetFileDropList()?.Count > 0;

        private bool FolderExists => !string.IsNullOrEmpty(SingleFolderPath) && Directory.Exists(SingleFolderPath);

        private string SingleFolderPath => string.IsNullOrEmpty(FolderPath) ? SelectedItemPaths?.FirstOrDefault() : FolderPath;

        private bool MultiPathExists => SelectedItemPaths != null && SelectedItemPaths.Count() > 0;

        private bool IsMultiPath => MultiPathExists && SelectedItemPaths.Count() > 1;

        private IEnumerable<string> MultiFolderPaths
        {
            get
            {
                if (!string.IsNullOrEmpty(FolderPath) && Directory.Exists(FolderPath))
                {
                    return new[] { FolderPath };
                }
                else if (MultiPathExists)
                {
                    return SelectedItemPaths;
                }
                else
                {
                    return new string[] { };
                }
            }
        }
    }
}
