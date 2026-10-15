using System.Runtime.InteropServices;

namespace CopyExtension;

internal partial class Win32Console
{
    bool Allocated = false;

    public Win32Console Attach()
    {
        if (!Allocated)
        {
            if (!AttachConsole(ATTACH_PARENT_PROCESS))
            {
                AllocConsole();
                Allocated = true;
            }
        }
        return this;
    }

    ~Win32Console()
    {
        Detach();
    }

    public void Detach()
    {
        if (Allocated)
        {
            FreeConsole();
            Allocated = false;
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
