using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CopyExtension;

public static partial class DesktopActions
{
    [LibraryImport("user32.dll", EntryPoint = "GetDesktopWindow")]
    private static partial IntPtr GetDesktopWindow();
    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    private static partial IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
    [LibraryImport("user32.dll", EntryPoint = "LockWorkStation")]
    private static partial IntPtr LockWorkStation();
    [LibraryImport("Powrprof.dll", EntryPoint = "SetSuspendState")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetSuspendState([MarshalAs(UnmanagedType.Bool)] bool hiberate, [MarshalAs(UnmanagedType.Bool)] bool forceCritical, [MarshalAs(UnmanagedType.Bool)] bool disableWakeEvent);
    [LibraryImport("user32.dll", EntryPoint = "ExitWindowsEx")]
    private static partial int ExitWindowsEx(int operationFlag, int operationReason);

    private const int SC_SCREENSAVE = 0xF140;
    private const int WM_SYSCOMMAND = 0x0112;
    public static void Lock()
    {
        SendMessage(GetDesktopWindow(), WM_SYSCOMMAND, SC_SCREENSAVE, 0);
        LockWorkStation();
    }
    public static void Logoff()
    {
        _ = ExitWindowsEx(0, 0);
    }
    public static void Sleep()
    {
        SetSuspendState(false, true, true);
    }
    public static void Hibernate()
    {
        SetSuspendState(true, true, true);
    }
    public static void Shutdown()
    {
        Process.Start(new ProcessStartInfo("shutdown", "/s /t 0")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }
    public static void Reboot()
    {
        Process.Start(new ProcessStartInfo("shutdown", "/r /t 0")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }
}
