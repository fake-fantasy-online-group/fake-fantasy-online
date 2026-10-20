using System;
using System.IO;

namespace FakeFantasyOnline.Foundation
{
/// <summary>Windows payloads follow the installation; settings and other platforms retain user storage.</summary>
public static class ClientResourceStorage
{
    public const string WindowsDirectory = "DownloadedResources";

    public static string RemoteRoot(string dataPath, string persistentPath, bool windowsPlayer)
    {
        if (!Path.IsPathRooted(persistentPath) || (windowsPlayer && !Path.IsPathRooted(dataPath)))
            throw new ArgumentException("Resource storage requires absolute application paths.");
        return windowsPlayer
            ? Path.GetFullPath(Path.Combine(dataPath, "..", WindowsDirectory, "Remote"))
            : Path.GetFullPath(Path.Combine(persistentPath, "MobileResources", "Remote"));
    }
}
}
