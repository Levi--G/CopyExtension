using System;

namespace CopyExtension
{
    internal static class IOHelper
    {
        public static string CheckAddLongPathPrefix(string path)
        {
            if (string.IsNullOrEmpty(path) || path.StartsWith(@"\\?\"))
            {
                return path;
            }
            else if (path.Length > 247 || path.Contains(@"~"))
            {
                return ForceAddLongPathPrefix(path);
            }
            else
            {
                return path;
            }
        }

        public static string ForceRemoveLongPathPrefix(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith(@"\\?\"))
            {
                return path;
            }
            else if (path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
            {
                return @"\\" + path.Substring(@"\\?\UNC\".Length);
            }
            else
            {
                return path.Substring(@"\\?\".Length);
            }
        }

        public static string ForceAddLongPathPrefix(string path)
        {
            if (string.IsNullOrEmpty(path) || path.StartsWith(@"\\?\"))
            {
                return path;
            }
            else
            {
                // http://msdn.microsoft.com/en-us/library/aa365247.aspx
                if (path.StartsWith(@"\\"))
                {
                    // UNC.
                    return @"\\?\UNC\" + path.Substring(2);
                }
                else
                {
                    return @"\\?\" + path;
                }
            }
        }
    }
}