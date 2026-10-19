using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace FakeFantasyOnline.Foundation
{
/// <summary>Small on-disk contract shared by the game and its separately packaged updater.</summary>
public sealed class DesktopInstallIdentity
{
    public const string FileName = "ffo-desktop-install.json";
    public string Platform, Version, InstallId, Executable, MacTeamId, WindowsSignerSha256;
    public static string MarkerPath(string root, string platform)
    { return Path.Combine(root, platform == "macos" ? "Contents/Resources/" + FileName : FileName); }
    public static DesktopInstallIdentity Read(string root, string platform)
    {
        var json = DesktopUpdateFiles.ReadObject(MarkerPath(root, platform));
        if (DesktopUpdateFiles.Number(json, "schemaVersion") != 1) throw new InvalidDataException("Unsupported desktop install marker.");
        var identity = new DesktopInstallIdentity {
            Platform = DesktopUpdateFiles.Text(json, "platform"), Version = DesktopUpdateFiles.Text(json, "version"),
            InstallId = DesktopUpdateFiles.Text(json, "installId"), Executable = DesktopUpdateFiles.Text(json, "executable"),
            MacTeamId = DesktopUpdateFiles.Text(json, "macTeamId", true), WindowsSignerSha256 = DesktopUpdateFiles.Text(json, "windowsSignerSha256", true)
        };
        if (identity.Platform != platform || (platform != "windows" && platform != "macos") ||
            !ClientProductVersion.IsValid(identity.Version) || identity.InstallId.Length > 200)
            throw new InvalidDataException("Invalid desktop install identity.");
        foreach (char c in identity.InstallId) if (!(char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_'))
            throw new InvalidDataException("Invalid desktop install identifier.");
        DesktopUpdateFiles.Relative(identity.Executable);
        if (platform == "windows" && (identity.Executable.IndexOf('/') >= 0 || !identity.Executable.EndsWith(".exe", StringComparison.Ordinal)))
            throw new InvalidDataException("Windows entry point must be a root executable.");
        if (platform == "macos" && (!identity.Executable.StartsWith("Contents/MacOS/", StringComparison.Ordinal) ||
            identity.Executable.Substring("Contents/MacOS/".Length).Contains("/")))
            throw new InvalidDataException("macOS entry point must be under Contents/MacOS.");
        if (identity.MacTeamId.Length != 0)
        {
            if (identity.MacTeamId.Length != 10) throw new InvalidDataException("Invalid macOS signing team.");
            foreach (char c in identity.MacTeamId) if (!(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9'))
                throw new InvalidDataException("Invalid macOS signing team.");
        }
        if (identity.WindowsSignerSha256.Length != 0) DesktopUpdateFiles.Digest(identity.WindowsSignerSha256);
        return identity;
    }
    public void RequireUpdate(DesktopInstallIdentity next, string version)
    {
        if (next.Platform != Platform || next.Version != version || ClientProductVersion.Compare(next.Version, Version) <= 0 ||
            next.InstallId != InstallId || next.Executable != Executable || next.MacTeamId != MacTeamId ||
            !next.WindowsSignerSha256.Equals(WindowsSignerSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Package identity, version or signing policy does not match this installation.");
    }
}

public sealed class DesktopUpdateRequest
{
    public string JobDirectory, InstallRoot, PersistentRoot, PackagePath, Sha256, Version, Platform, CurrentVersion;
    public long SizeBytes, ParentStartTicks;
    public int ParentProcessId;
    public string RequestPath { get { return Path.Combine(JobDirectory, "request.json"); } }
    public string StatusPath { get { return Path.Combine(JobDirectory, "status.txt"); } }
    public string CommitPath { get { return Path.Combine(JobDirectory, "commit"); } }
    public string CancelPath { get { return Path.Combine(JobDirectory, "cancel"); } }
    public static DesktopUpdateRequest Read(string path)
    {
        var obj = DesktopUpdateFiles.ReadObject(path);
        if (DesktopUpdateFiles.Number(obj, "schemaVersion") != 1) throw new InvalidDataException("Unsupported desktop update request.");
        var r = new DesktopUpdateRequest { JobDirectory = Path.GetDirectoryName(Path.GetFullPath(path)),
            InstallRoot = DesktopUpdateFiles.Text(obj, "installRoot"), PersistentRoot = DesktopUpdateFiles.Text(obj, "persistentRoot"),
            PackagePath = DesktopUpdateFiles.Text(obj, "packagePath"), Sha256 = DesktopUpdateFiles.Text(obj, "sha256"),
            Version = DesktopUpdateFiles.Text(obj, "version"), Platform = DesktopUpdateFiles.Text(obj, "platform"),
            CurrentVersion = DesktopUpdateFiles.Text(obj, "currentVersion"), SizeBytes = DesktopUpdateFiles.Number(obj, "sizeBytes"),
            ParentStartTicks = DesktopUpdateFiles.Number(obj, "parentStartTicks"), ParentProcessId = checked((int)DesktopUpdateFiles.Number(obj, "parentProcessId")) };
        r.Validate(); return r;
    }
    public void Validate()
    {
        foreach (string path in new[] { InstallRoot, PersistentRoot, PackagePath, JobDirectory })
        {
            if (string.IsNullOrEmpty(path) || !Path.IsPathRooted(path) || Path.GetFullPath(path) != path || path == Path.GetPathRoot(path))
                throw new InvalidDataException("Updater requires canonical, non-root absolute paths.");
            DesktopUpdateFiles.NoLinks(path);
        }
        if (DesktopUpdateFiles.Overlaps(InstallRoot, PersistentRoot) || DesktopUpdateFiles.Overlaps(InstallRoot, JobDirectory) ||
            DesktopUpdateFiles.Overlaps(InstallRoot, PackagePath) || !DesktopUpdateFiles.Below(PersistentRoot, JobDirectory) ||
            !DesktopUpdateFiles.Below(PersistentRoot, PackagePath))
            throw new InvalidDataException("Update cache and user data must be outside the application directory.");
        if ((Platform != "windows" && Platform != "macos") || !ClientProductVersion.IsValid(CurrentVersion) ||
            !ClientProductVersion.IsValid(Version) || ClientProductVersion.Compare(Version, CurrentVersion) <= 0 ||
            SizeBytes <= 0 || SizeBytes > 8L * 1024 * 1024 * 1024 || ParentProcessId <= 0 || ParentStartTicks <= 0 ||
            !PackagePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Invalid desktop ZIP update request.");
        DesktopUpdateFiles.Digest(Sha256);
        if (Platform == "macos" && !InstallRoot.EndsWith(".app", StringComparison.Ordinal))
            throw new InvalidDataException("macOS target must be an application bundle.");
    }
    public void Save()
    {
        Validate();
        DesktopUpdateFiles.WriteNew(RequestPath, BoundedJson.Serialize(new Dictionary<string, object> {
            {"schemaVersion",1}, {"installRoot",InstallRoot}, {"persistentRoot",PersistentRoot}, {"packagePath",PackagePath},
            {"sha256",Sha256}, {"version",Version}, {"platform",Platform}, {"currentVersion",CurrentVersion},
            {"sizeBytes",SizeBytes}, {"parentProcessId",ParentProcessId}, {"parentStartTicks",ParentStartTicks}
        }));
    }
}

public static class DesktopUpdateFiles
{
    public static bool Below(string root, string child)
    { return child.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase); }
    public static bool Overlaps(string first, string second)
    { return string.Equals(first, second, StringComparison.OrdinalIgnoreCase) || Below(first, second) || Below(second, first); }
    public static void NoLinks(string path)
    {
        for (string current = Path.GetFullPath(path); !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
        {
            try { if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Updater refuses symbolic links or reparse points: " + current); }
            catch (FileNotFoundException) { } catch (DirectoryNotFoundException) { }
        }
    }
    public static void Relative(string path)
    {
        if (string.IsNullOrEmpty(path) || path.Length > 512 || path[0] == '/' || path.IndexOf('\\') >= 0)
            throw new InvalidDataException("Invalid archive path.");
        foreach (string part in path.Split('/'))
        {
            if (part.Length == 0 || part == "." || part == ".." || part.EndsWith(".") || part.EndsWith(" "))
                throw new InvalidDataException("Invalid archive path component.");
            foreach (char c in part) if (char.IsControl(c) || c == ':' || c == '*' || c == '?' || c == '"' || c == '<' || c == '>' || c == '|')
                throw new InvalidDataException("Invalid archive path character.");
            string stem = part.Split('.')[0].ToUpperInvariant();
            if (stem == "CON" || stem == "PRN" || stem == "AUX" || stem == "NUL" ||
                (stem.Length == 4 && (stem.StartsWith("COM") || stem.StartsWith("LPT")) && stem[3] >= '0' && stem[3] <= '9'))
                throw new InvalidDataException("Reserved archive filename.");
        }
    }
    public static void Digest(string value)
    {
        if (value == null || value.Length != 64) throw new InvalidDataException("Invalid SHA-256.");
        foreach (char c in value) if (!((c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') || (c >= '0' && c <= '9')))
            throw new InvalidDataException("Invalid SHA-256.");
    }
    public static Dictionary<string, object> ReadObject(string path)
    {
        NoLinks(path);
        if (new FileInfo(path).Length > 16384) throw new InvalidDataException("Updater metadata exceeds size limit.");
        return BoundedJson.Parse(File.ReadAllText(path, new UTF8Encoding(false, true))) as Dictionary<string, object>
            ?? throw new InvalidDataException("Invalid updater metadata.");
    }
    public static string Text(Dictionary<string, object> obj, string key, bool empty = false)
    {
        object raw; string value;
        if (!obj.TryGetValue(key, out raw) || (value = raw as string) == null || value.Length > 4096 ||
            (!empty && string.IsNullOrWhiteSpace(value)) || value != value.Trim()) throw new InvalidDataException("Invalid " + key);
        foreach (char c in value) if (char.IsControl(c)) throw new InvalidDataException("Invalid " + key);
        return value;
    }
    public static long Number(Dictionary<string, object> obj, string key)
    {
        object value;
        if (!obj.TryGetValue(key, out value) || !(value is long) || (long)value <= 0) throw new InvalidDataException("Invalid " + key);
        return (long)value;
    }
    public static void WriteNew(string path, string text)
    {
        NoLinks(path); byte[] bytes = new UTF8Encoding(false, true).GetBytes(text);
        using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        { output.Write(bytes, 0, bytes.Length); output.Flush(true); }
    }
    public static string QuoteArgument(string value)
    {
        // Windows CommandLineToArgvW rules, also understood by .NET ProcessStartInfo on Unix.
        var b = new StringBuilder("\""); int slashes = 0;
        foreach (char c in value)
        {
            if (c == '\\') { slashes++; continue; }
            if (c == '"') b.Append('\\', slashes * 2 + 1); else b.Append('\\', slashes);
            slashes = 0; b.Append(c);
        }
        b.Append('\\', slashes * 2); return b.Append('"').ToString();
    }
}
}
