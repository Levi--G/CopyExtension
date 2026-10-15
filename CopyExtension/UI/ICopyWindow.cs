using CopyExtension.Tasks;

namespace CopyExtension.UI;

internal interface ICopyWindow
{
    event Action? ExitRequested;

    void Invoke(Action<ICopyWindow> torun);
    void Post(Action<ICopyWindow> torun);
    T Invoke<T>(Func<ICopyWindow, T> torun);
    void AddTask(CopyTask task);
    HashSet<string> GetClipboardFiles();
    void Quit();
}