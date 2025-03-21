using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZetaLongPaths;

namespace CopyExtension
{
    internal static class Extensions
    {
        public static void ForceDelete(this ZlpFileInfo info)
        {
            try
            {
                info.Delete();
                return;
            }
            catch { }
            //try removing attributes
            var old = info.Attributes;
            info.Attributes &= ~(ZetaLongPaths.Native.FileAttributes.Hidden | ZetaLongPaths.Native.FileAttributes.Readonly | ZetaLongPaths.Native.FileAttributes.System);
            try
            {
                info.Delete();
                return;
            }
            catch (Exception ex)
            {
                try
                {
                    info.Attributes = old;
                }
                catch { }
                throw ex;
            }
        }
        public static void ForceDelete(this ZlpDirectoryInfo info)
        {
            try
            {
                info.Delete(recursive: true);
                return;
            }
            catch { }
            var old = info.Attributes;
            info.Attributes &= ~(ZetaLongPaths.Native.FileAttributes.Hidden | ZetaLongPaths.Native.FileAttributes.Readonly | ZetaLongPaths.Native.FileAttributes.System);

            try
            {
                ZlpFileInfo[] files = info.GetFiles();
                foreach (ZlpFileInfo f in files)
                {
                    try
                    {
                        f.ForceDelete();
                    }
                    catch { }
                }

                ZlpDirectoryInfo[] directories = info.GetDirectories();
                foreach (ZlpDirectoryInfo zlpDirectoryInfo in directories)
                {
                    try
                    {
                        zlpDirectoryInfo.ForceDelete();
                    }
                    catch { }
                }
                info.Delete(recursive: true);
            }
            catch (Exception ex)
            {
                try
                {
                    info.Attributes = old;
                }
                catch { }
                throw ex;
            }
        }
    }
}
