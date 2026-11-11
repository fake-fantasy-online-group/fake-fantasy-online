#if FFO_DEBUG_SERVER_SWITCH
using System;
using System.IO;
using System.Text;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
/// <summary>
/// Debug-build-only server choice (FFO_DEBUG_SERVER_SWITCH, set per build by --debug-server-switch).
/// The next start reads the existing persistentDataPath/server.json override: "local" writes it,
/// "online" deletes it. Startup validation (PrivateServerOptions.FromLocalServerFile) is reused,
/// and an invalid choice never replaces the previous file. Never compiled without the define.
/// </summary>
public static class DebugServerSwitchStore
{
    public const string FileName = "server.json";
    public const string DefaultLocalHost = "127.0.0.1";
    public const int DefaultLocalPort = 19000;

    /// <summary>What the running process selected at startup.</summary>
    public static string Describe(PrivateServerOptions running)
    {
        if (running == null) return "未知";
        if (running.LocalServer != null) return "本地 " + Format(running.LocalServer);
        return running.DispatchUri == null ? "未知" : "线上 " + running.DispatchUri.Authority;
    }

    /// <summary>What the next start will select. Throws InvalidDataException for an invalid file.</summary>
    public static string DescribeNext(string persistentDirectory, string streamingDirectory)
    {
        PrivateServerOptions next = PrivateServerOptions.FromLocalServerFiles(persistentDirectory, streamingDirectory);
        return next == null ? "线上" : "本地 " + Format(next.LocalServer);
    }

    public static void SelectLocal(string persistentDirectory, string host, int port)
    {
        if (string.IsNullOrEmpty(persistentDirectory)) throw new ArgumentException("Missing persistent directory.", nameof(persistentDirectory));
        Directory.CreateDirectory(persistentDirectory);
        string path = Path.Combine(persistentDirectory, FileName);
        string part = path + "." + Guid.NewGuid().ToString("N") + ".part";
        string json = "{\"host\":" + Quote(host ?? string.Empty) + ",\"port\":" + port + "}\n";
        try
        {
            using (var stream = new FileStream(part, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            // Same validation as startup, before the previous choice is replaced.
            PrivateServerOptions.FromLocalServerFile(part);
            if (File.Exists(path)) File.Replace(part, path, null);
            else File.Move(part, path);
        }
        finally { if (File.Exists(part)) File.Delete(part); }
    }

    /// <summary>Removes the local override. Returns false when it was already absent.</summary>
    public static bool SelectOnline(string persistentDirectory)
    {
        string path = Path.Combine(persistentDirectory, FileName);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    private static string Format(ClientConfig.Srv server) => server.host + ":" + server.port;

    private static string Quote(string value)
    {
        var builder = new StringBuilder("\"");
        foreach (char c in value)
        {
            if (c == '"' || c == '\\') builder.Append('\\').Append(c);
            else if (c < ' ') builder.Append("\\u").Append(((int)c).ToString("x4"));
            else builder.Append(c);
        }
        return builder.Append('"').ToString();
    }
}
}
#endif
