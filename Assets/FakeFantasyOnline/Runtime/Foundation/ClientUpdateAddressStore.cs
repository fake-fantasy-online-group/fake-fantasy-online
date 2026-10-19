using System;
using System.IO;
using System.Text;

namespace FakeFantasyOnline.Foundation
{
/// <summary>One non-secret endpoint. Same-directory rename/replace preserves the last complete address.</summary>
public static class ClientUpdateAddressStore
{
    public const string FileName = "update-dispatch-url.txt";
    public static string Read(string directory)
    {
        string path = Path.Combine(directory, FileName);
        RejectPath(path);
        if (!File.Exists(path)) return "";
        if (new FileInfo(path).Length > MobileResourceBootstrapConfig.MaxEndpointBytes) throw new InvalidDataException("Saved dispatch address is too large.");
        return new UTF8Encoding(false, true).GetString(File.ReadAllBytes(path));
    }
    public static void Save(string directory, string address, bool developmentHttp)
    {
        MobileResourceBootstrapConfig.ValidateEndpoint(address, developmentHttp);
        RejectPath(directory);
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, FileName), part = path + "." + Guid.NewGuid().ToString("N") + ".part";
        RejectPath(path); RejectPath(part);
        byte[] bytes = new UTF8Encoding(false, true).GetBytes(address);
        try
        {
            using (var stream = new FileStream(part, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (File.Exists(path)) File.Replace(part, path, null);
            else File.Move(part, path);
        }
        finally { if (File.Exists(part)) File.Delete(part); }
    }
    private static void RejectPath(string path)
    {
        for (string current = Path.GetFullPath(path); current != null; current = Path.GetDirectoryName(current))
            RejectLink(current);
    }
    private static void RejectLink(string path)
    {
        try { if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Update configuration cannot be a symbolic link."); }
        catch (FileNotFoundException) { } catch (DirectoryNotFoundException) { }
    }
}
}
