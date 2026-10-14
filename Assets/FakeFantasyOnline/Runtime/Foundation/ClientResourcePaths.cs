using System;

namespace FakeFantasyOnline.Foundation
{
/// <summary>File-backed resource root. Android APK/jar StreamingAssets is never a filesystem root.</summary>
public static class ClientResourcePaths
{
    public static string InstalledRoot { get; private set; }

    public static string Resolve(string streamingAssetsPath, bool mobile)
    {
        if (!mobile) return streamingAssetsPath;
        if (string.IsNullOrEmpty(InstalledRoot))
            throw new InvalidOperationException("Mobile resources have not finished installation.");
        return InstalledRoot;
    }

    public static void Activate(string root)
    {
        if (string.IsNullOrEmpty(root) || !System.IO.Path.IsPathRooted(root) || root.Contains("://"))
            throw new ArgumentException("Installed resources require an absolute filesystem path.", nameof(root));
        InstalledRoot = root;
    }
}
}
