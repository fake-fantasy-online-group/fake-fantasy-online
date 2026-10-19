using System;
using System.IO;
using System.Text.RegularExpressions;

namespace FakeFantasyOnline.Editor
{
/// <summary>Strict local helper payload boundary; no recursive copy of a user's configuration directories.</summary>
public static class DesktopBuildPayload
{
    [Serializable]
    public sealed class InstallMarker
    {
        public int schemaVersion;
        public string platform, version, installId, executable, macTeamId, windowsSignerSha256;
    }

    private static string[] Names(string platform)
    {
        if (platform == "windows") return new[] { "DesktopUpdater.exe" };
        if (platform == "macos") return new[] { "DesktopUpdater-osx-x64", "DesktopUpdater-osx-arm64" };
        throw new ArgumentException("Desktop helper platform must be windows or macos.");
    }

    public static void ValidateIdentity(string platform, bool formal, string appId, string teamId, string signer)
    {
        if (string.IsNullOrWhiteSpace(appId) || appId.Length > 200 ||
            !Regex.IsMatch(appId, @"\A[A-Za-z][A-Za-z0-9_-]*(\.[A-Za-z][A-Za-z0-9_-]*)+\z"))
            throw new InvalidOperationException("Configure a stable desktop application identifier in Unity before packaging.");
        if ((!string.IsNullOrEmpty(teamId) || (formal && platform == "macos")) &&
            (teamId == null || !Regex.IsMatch(teamId, @"\A[A-Z0-9]{10}\z")))
            throw new InvalidOperationException("Formal macOS builds require the existing Developer ID team identifier in FFO_MAC_TEAM_ID.");
        if ((!string.IsNullOrEmpty(signer) || (formal && platform == "windows")) &&
            (signer == null || !Regex.IsMatch(signer, @"\A[0-9a-fA-F]{64}\z")))
            throw new InvalidOperationException("Formal Windows builds require the existing Authenticode certificate SHA-256 in FFO_WINDOWS_SIGNER_SHA256.");
    }

    public static void ValidateHelpers(string directory, string platform)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory) ||
            (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Supply a real local directory containing the prebuilt self-contained desktop updater.");
        string[] names = Names(platform);
        foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
            if (Array.IndexOf(names, Path.GetFileName(entry)) < 0 || Directory.Exists(entry) ||
                (File.GetAttributes(entry) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Desktop updater input contains an unexpected or linked entry; only the exact helper binaries are allowed.");
        foreach (string name in names)
            if (!File.Exists(Path.Combine(directory, name)) || new FileInfo(Path.Combine(directory, name)).Length == 0)
                throw new IOException("The required self-contained desktop updater binary is missing or empty.");
    }

    public static void CopyHelpers(string source, string destination, string platform)
    {
        ValidateHelpers(source, platform);
        if (Directory.Exists(destination) || File.Exists(destination))
            throw new IOException("Desktop updater staging destination must be new.");
        Directory.CreateDirectory(destination);
        foreach (string name in Names(platform)) File.Copy(Path.Combine(source, name), Path.Combine(destination, name), false);
    }
}
}
