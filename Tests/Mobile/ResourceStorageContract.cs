using System;
using System.IO;
using FakeFantasyOnline.Foundation;

public static class ResourceStorageContract
{
    public static int Main()
    {
        string root = Path.GetFullPath(Path.GetTempPath());
        string data = Path.Combine(root, "Game", "Game_Data"), user = Path.Combine(root, "user");
        string windows = ClientResourceStorage.RemoteRoot(data, user, true);
        if (windows != Path.Combine(root, "Game", "DownloadedResources", "Remote"))
            throw new Exception("Windows resources must be in the installation directory.");
        string oldCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(root);
            if (windows != ClientResourceStorage.RemoteRoot(data, user, true))
                throw new Exception("Storage must not depend on the working directory.");
        }
        finally { Directory.SetCurrentDirectory(oldCwd); }
        if (ClientResourceStorage.RemoteRoot(data, user, false) != Path.Combine(user, "MobileResources", "Remote"))
            throw new Exception("Non-Windows and Editor storage must remain unchanged.");
        bool rejected = false;
        try { ClientResourceStorage.RemoteRoot("relative/Game_Data", user, true); }
        catch (ArgumentException) { rejected = true; }
        if (!rejected) throw new Exception("Relative application paths must be rejected.");
        Console.WriteLine("PASS resource storage selection: Windows install-relative, cwd-independent, sandbox unchanged, relative paths rejected.");
        return 0;
    }
}
