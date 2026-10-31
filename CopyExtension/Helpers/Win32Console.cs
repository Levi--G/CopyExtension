using System.Runtime.InteropServices;

namespace CopyExtension;

internal partial class Win32Console : IDisposable
{
    static bool Attached = false;

    public Win32Console AttachOrAlloc()
    {
        if (!Attached)
        {
            if (!AttachConsole(ATTACH_PARENT_PROCESS))
            {
                if (!AllocConsole())
                {
                    return this;
                }
            }
            Attached = true;
        }
        return this;
    }

    public static void Attach()
    {
        if (!Attached)
        {
            if (AttachConsole(ATTACH_PARENT_PROCESS))
            {
                Attached = true;
            }
        }
    }

    ~Win32Console()
    {
        Detach();
    }

    public void Dispose()
    {
        Detach();
    }

    public static void Detach()
    {
        if (Attached)
        {
            FreeConsole();
            Attached = false;
        }
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(int dwProcessId);
    private const int ATTACH_PARENT_PROCESS = -1;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsole();

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FreeConsole();
}