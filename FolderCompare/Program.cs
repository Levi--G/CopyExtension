

using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

var source = "";
//DELETES FILES HERE
var destination = "";

bool requirefulldrimatch = true;

var sourcedirs = GetFolders(source);
var destinationdirs = GetFolders(destination);

foreach (var dir in destinationdirs)
{

}



List<NameAndPath> GetFolders(string rootPath)
{
    var folders = new List<NameAndPath>();
    GetFoldersRecursive(rootPath, folders);
    return folders;
}
void GetFoldersRecursive(string rootPath, List<NameAndPath> list)
{
    foreach (var dir in Directory.GetDirectories(rootPath))
    {
        var name = Path.GetFileName(dir);
        list.Add(new NameAndPath(name, dir));
        GetFoldersRecursive(dir, list);
    }
}



bool Compare(string f, string f2, Action<long> progresscallback, int buffersize, bool shouldexist, CancellationToken token)
{
    if (! File.Exists(f) || !File.Exists(f2)) { return false; }
    if (f.Length != f2.Length) { return false; }
    byte[] buffer1 = new byte[buffersize];
    byte[] buffer2 = new byte[buffersize];
    byte[] buffer3 = new byte[buffersize];
    byte[] buffer4 = new byte[buffersize];
    using (var s = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.Read, buffersize, FileOptions.SequentialScan))
    using (var s2 = new FileStream(f2, FileMode.Open, FileAccess.Read, FileShare.Read, buffersize, FileOptions.SequentialScan))
    {
        long total = 0;
        int read = buffersize;
        Task<bool>? lastcomp = null;
        bool swap = false;
        while (read == buffersize)
        {
            if (token.IsCancellationRequested) { return false; }
            swap = !swap;
            var b1 = s.ReadAsync(swap ? buffer1 : buffer3, 0, buffersize);
            var b2 = s2.ReadAsync(swap ? buffer2 : buffer4, 0, buffersize);
            Task.WhenAll(b1, b2).Wait();
            if ((read = b1.Result) != b2.Result) { return false; }
            total += read;
            progresscallback?.Invoke(total);
            lastcomp?.Wait();
            if (lastcomp != null && !lastcomp.Result) { return false; }
            lastcomp = ByteArrayCompareAsync(swap ? buffer1 : buffer3, swap ? buffer2 : buffer4, b1.Result);
        }
        lastcomp?.Wait();
        if (lastcomp != null && !lastcomp.Result) { return false; }
    }
    return true;
}

static Task<bool> ByteArrayCompareAsync(byte[] b1, byte[] b2, long count)
{
    return Task.Factory.StartNew(() => MemoryExtensions.SequenceEqual(b1, b2));
    //return Task.Factory.StartNew(() => memcmp(b1, b2, count) == 0);
}

//[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
//static extern int memcmp(byte[] b1, byte[] b2, long count);

record NameAndPath(string Name, string Path);