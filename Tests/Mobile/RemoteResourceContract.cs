using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

internal static class RemoteResourceContract
{
    private static int checks;
    private const string ManifestUrl = "https://cdn.example.com/releases/1/mobile-resources.tsv";
    private static readonly byte[] A = Encoding.UTF8.GetBytes("small package fixture"), B = Encoding.UTF8.GetBytes("{\"fixture\":true}");
    private static void Assert(bool value, string name) { if (!value) throw new Exception(name); checks++; }
    private static void Reject(Action action, string name)
    {
        try { action(); }
        catch (Exception e) when (e is InvalidDataException || e is IOException || e is ArgumentException || e is OperationCanceledException || e is WebException || e is UnauthorizedAccessException)
        { checks++; return; }
        throw new Exception(name);
    }
    private static string Hash(byte[] b) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(b)).Replace("-", "").ToLowerInvariant(); }
    private static string Row(string source, string destination, byte[] b) { return source + "\t" + destination + "\t" + b.Length + "\t" + Hash(b) + "\n"; }
    private static string Manifest(string version = "1.0.0")
    { return "FFO-MOBILE-1\n@client\tandroid\t" + version + "\n" + Row("packages/a.pkg", "data/a.pkg", A) + Row("tables/a.json", "gameassets/tables/a.json", B); }
    private sealed class FakeHttp : IMobileResourceTransport
    {
        public readonly Dictionary<string, byte[]> Bodies = new Dictionary<string, byte[]>();
        public readonly List<string> Requests = new List<string>();
        public string FailUrl;
        public Action<string> AfterWrite;
        public FakeHttp(string manifest = null)
        {
            Bodies[ManifestUrl] = Encoding.UTF8.GetBytes(manifest ?? Manifest());
            Bodies["https://cdn.example.com/releases/1/packages/a.pkg"] = A;
            Bodies["https://cdn.example.com/releases/1/tables/a.json"] = B;
        }
        public void Download(Uri uri, string destination, long max, CancellationToken cancellation, Action<long> progress)
        {
            Requests.Add(uri.AbsoluteUri);
            using (var input = new MemoryStream(Bodies[uri.AbsoluteUri]))
            using (var output = File.Create(destination))
            {
                if (uri.AbsoluteUri == FailUrl)
                { output.WriteByte(7); throw new IOException("simulated interrupted connection or full disk"); }
                MobileResourceHttpTransport.CopyBounded(input, output, max, cancellation, progress);
            }
            AfterWrite?.Invoke(destination);
        }
    }
    private static MobileRemoteResourceInstall Installer(string root, FakeHttp http)
    { return new MobileRemoteResourceInstall(root, "1.0.0", false, http); }

    public static void Main()
    {
        string temp = Path.Combine(Path.GetTempPath(), "ffo-remote-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            TestConfiguration(); TestManifests(); TestBoundedStreams();
            TestInstallation(Path.Combine(temp, "installation"));
            TestFailures(Path.Combine(temp, "failures"));
            TestActivationFailure(Path.Combine(temp, "activation"));
            TestSymlinks(Path.Combine(temp, "symlink"));
            TestHttp(Path.Combine(temp, "http"));
            Console.WriteLine("PASS remote resource contracts (" + checks + " checks; fake content/HTTP only, no Unity/device validation)");
        }
        finally { Directory.Delete(temp, true); }
    }

    private static void TestConfiguration()
    {
        var config = new MobileResourceBootstrapConfig { schemaVersion = 1, mode = "resource-free", appVersion = "1.0.0", resourceManifestUrl = "", dispatchUrl = "" };
        config.Validate(false); checks++;
        config.resourceManifestUrl = ManifestUrl; config.dispatchUrl = "https://game.example.com/dispatch.json";
        config.Validate(false); checks++;
        Assert(config.resourceManifestUrl != config.dispatchUrl, "resource and dispatch configuration are distinct");
        config.developmentHttp = true;
        Reject(() => config.Validate(false), "development HTTP requires development build");
        config.resourceManifestUrl = "http://192.168.1.9:8080/mobile-resources.tsv";
        config.Validate(true); checks++;
        foreach (string url in new[] { "http://example.com/a", "http://192.168.1.9/a", "https://u:p@example.com/a", "https://example.com/a?token=secret", "https://example.com/a#secret", "file:///etc/passwd", "ftp://example.com/a", "https://127.0.0.1/a", "https://0.0.0.0/a", "https://[::]/a", "https://example.com/a\n", "https://example.com/\ta", "https://example.com\\a" })
            Reject(() => MobileResourceBootstrapConfig.ValidateEndpoint(url, false), "unsafe/non-HTTPS endpoint rejected: " + url);
        foreach (string url in new[] { "http://example.com/a", "http://8.8.8.8/a", "http://127.0.0.1/a", "http://169.254.169.254/a", "http://172.32.0.1/a" })
            Reject(() => MobileResourceBootstrapConfig.ValidateEndpoint(url, true), "LAN exception is restricted: " + url);
        foreach (string url in new[] { "http://10.1.2.3/a", "http://172.16.0.1/a", "http://192.168.1.1/a" })
            Assert(MobileResourceBootstrapConfig.ValidateEndpoint(url, true).Scheme == "http", "explicit private LAN works");
        config.schemaVersion = 9; Reject(() => config.Validate(true), "config schema rejected");
    }

    private static void TestManifests()
    {
        MobileRemoteResourceInstall.ValidateManifest(MobileResourceManifest.Parse(Manifest()), "1.0.0"); checks++;
        Reject(() => MobileRemoteResourceInstall.ValidateManifest(MobileResourceManifest.Parse(Manifest("2.0.0")), "1.0.0"), "wrong app rejected");
        Reject(() => MobileRemoteResourceInstall.ValidateManifest(MobileResourceManifest.Parse(Manifest().Replace("android", "windows")), "1.0.0"), "wrong platform rejected");
        Reject(() => MobileRemoteResourceInstall.ValidateManifest(MobileResourceManifest.Parse(Manifest().Replace("@client\tandroid\t1.0.0\n", "")), "1.0.0"), "remote requires compatibility marker");
        Reject(() => MobileRemoteResourceInstall.ValidateManifest(MobileResourceManifest.Parse(Manifest().Replace("FFO-MOBILE-1", "FFO-MOBILE-OFFLINE-1")), "1.0.0"), "offline manifest cannot enable remote downloads");
        Reject(() => MobileResourceManifest.Parse(Manifest() + "@client\tandroid\t1.0.0\n"), "duplicate compatibility declaration");
        foreach (string path in new[] { "../a.pkg", "/a.pkg", "https://evil.com/a.pkg", "a/%2e%2e/a.pkg", "a\\b.pkg", "a//b.pkg", "a/./b.pkg", "a.pkg.part", "a?secret.pkg", "a/CON.pkg", "a/a\u0001.pkg", "a/a|b.pkg", new string('x', 1025) + ".pkg" })
            Reject(() => MobileRemoteResourceInstall.ValidateManifest(MobileResourceManifest.Parse("FFO-MOBILE-1\n@client\tandroid\t1.0.0\n" + Row(path, "data/a.pkg", A)), "1.0.0"), "unsafe source rejected: " + path);
        foreach (string path in new[] { "AddOns/payload.lua", "gameassets/payload.dll", "gameassets/payload.cs", "gameassets/payload.so", "gameassets/payload.js", "mobile-dispatch.txt", "data/sub/a.pkg", ".resource-manifest.tsv", "ReferenceClient/other/a.idx" })
            Reject(() => MobileRemoteResourceInstall.ValidateManifest(MobileResourceManifest.Parse("FFO-MOBILE-1\n@client\tandroid\t1.0.0\n" + Row("a.pkg", path, A)), "1.0.0"), "plugin/config destination rejected: " + path);
        Reject(() => MobileResourceManifest.Parse(Manifest() + Row("other.pkg", "DATA/A.PKG", A)), "case-insensitive collision");
        Reject(() => MobileRemoteResourceInstall.ValidateManifest(MobileResourceManifest.Parse(Manifest() + Row("other.json", "gameassets/Tables/other.json", B)), "1.0.0"), "directory case conflict");
        MobileRemoteResourceInstall.ValidateManifest(MobileResourceManifest.Parse(Manifest() + Row("class.png", "ReferenceClient/creation-textures/class0.png", A) + Row("cursor.png", "ReferenceClient/Cursor/Normal.png", A) + Row("font/gbk12.png", "ReferenceClient/font/gbk12.png", A) + Row("font/gbk12.idx", "ReferenceClient/font/gbk12.idx", A) + Row("font/gbk12.wid", "ReferenceClient/font/gbk12.wid", A)), "1.0.0"); checks++;
        Reject(() => MobileRemoteResourceInstall.ValidateManifest(MobileResourceManifest.Parse(Manifest() + Row("a.json", "gameassets/tables/a.json/child.json", B)), "1.0.0"), "file-directory prefix collision");
        string huge = "FFO-MOBILE-1\n@client\tandroid\t1.0.0\na.pkg\tdata/a.pkg\t68719476737\t" + Hash(A) + "\n";
        Reject(() => MobileRemoteResourceInstall.ValidateManifest(MobileResourceManifest.Parse(huge), "1.0.0"), "64GiB total limit");
        Assert(MobileRemoteResourceInstall.ResourceUri(new Uri(ManifestUrl), "packages/a b.pkg").AbsoluteUri == "https://cdn.example.com/releases/1/packages/a%20b.pkg", "URL remains under manifest directory and encodes path");
    }

    private static void TestBoundedStreams()
    {
        long done = -1;
        using (var output = new MemoryStream())
        {
            MobileResourceHttpTransport.CopyBounded(new MemoryStream(A), output, A.Length, CancellationToken.None, b => done = b);
            Assert(done == A.Length && output.Length == A.Length, "real transferred byte progress");
        }
        using (var output = new MemoryStream())
        {
            Reject(() => MobileResourceHttpTransport.CopyBounded(new MemoryStream(A), output, A.Length - 1, CancellationToken.None, null), "stream cannot exceed length even without HTTP header");
            Assert(output.Length <= A.Length - 1, "oversized chunk never written");
        }
        using (var cancellation = new CancellationTokenSource())
        { cancellation.Cancel(); Reject(() => MobileResourceHttpTransport.CopyBounded(new MemoryStream(A), new MemoryStream(), A.Length, cancellation.Token, null), "cancelled transfer rejected"); }
    }

    private static void TestInstallation(string storage)
    {
        var http = new FakeHttp();
        string active = Installer(storage, http).Install(ManifestUrl, CancellationToken.None, null);
        Assert(Directory.Exists(active) && !active.EndsWith(".staging"), "only complete renamed set returned");
        Assert(File.ReadAllBytes(Path.Combine(active, "data/a.pkg")).Length == A.Length, "package installed");
        Assert(http.Requests.Count == 3 && http.Requests[0] == ManifestUrl, "only manifest-listed files requested");
        Assert(Installer(storage, new FakeHttp()).UseInstalled(CancellationToken.None, null) == active, "offline startup revalidates active set");
        var unchanged = new FakeHttp();
        Assert(Installer(storage, unchanged).Install(ManifestUrl, CancellationToken.None, null) == active && unchanged.Requests.Count == 1, "same manifest reuses verified active resources without redownloading");
        string marker = File.ReadAllText(Path.Combine(storage, "active-resource-set.txt"));
        string update = Manifest().Replace("tables/a.json\tgameassets", "tables/a2.json\tgameassets");
        var interrupted = new FakeHttp(update);
        interrupted.Bodies["https://cdn.example.com/releases/1/tables/a2.json"] = B;
        interrupted.FailUrl = "https://cdn.example.com/releases/1/tables/a2.json";
        Reject(() => Installer(storage, interrupted).Install(ManifestUrl, CancellationToken.None, null), "interrupted update rejected");
        Assert(File.ReadAllText(Path.Combine(storage, "active-resource-set.txt")) == marker && Directory.Exists(active), "old activation and bytes preserved on failure");
        string staging = Path.Combine(storage, MobileResourceManifest.Parse(update).Version + ".staging");
        Assert(File.Exists(Path.Combine(staging, "data/a.pkg")), "completed staged file retained");
        Assert(!File.Exists(Path.Combine(staging, "gameassets/tables/a.json.part")), "interrupted current file discarded");
        // A killed older attempt can leave a part beside an already valid final file.
        File.WriteAllText(Path.Combine(staging, "data/a.pkg.part"), "stale");
        var retry = new FakeHttp(update);
        retry.Bodies["https://cdn.example.com/releases/1/tables/a2.json"] = B;
        string next = Installer(storage, retry).Install(ManifestUrl, CancellationToken.None, null);
        Assert(retry.Requests.Count == 2 && !retry.Requests.Contains("https://cdn.example.com/releases/1/packages/a.pkg"), "retry reuses verified completed files and restarts failed file");
        Assert(next != active && Directory.Exists(active), "successful update keeps previous valid generation");
        Assert(!File.Exists(Path.Combine(next, "data/a.pkg.part")), "no stale partial payload enters activated root");
        File.WriteAllText(Path.Combine(next, ".resource-verified.part"), "interrupted metadata receipt");
        Assert(Installer(storage, new FakeHttp()).UseInstalled(CancellationToken.None, null) == next, "partial metadata receipt cannot invalidate a complete resource set");
        using (var held = new FileStream(Path.Combine(storage, ".install.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Reject(() => Installer(storage, new FakeHttp()).Install(ManifestUrl, CancellationToken.None, null), "concurrent install cannot modify shared staging");
        File.WriteAllText(Path.Combine(next, "data/a.pkg"), "corrupt");
        Reject(() => Installer(storage, new FakeHttp()).UseInstalled(CancellationToken.None, null), "offline activation rejects corrupt file");
        File.WriteAllText(Path.Combine(storage, "active-resource-set.txt"), "../../outside");
        Reject(() => Installer(storage, new FakeHttp()).UseInstalled(CancellationToken.None, null), "activation marker path traversal rejected");
    }

    private static void TestFailures(string basePath)
    {
        var wrongHash = new FakeHttp(); wrongHash.Bodies["https://cdn.example.com/releases/1/packages/a.pkg"] = new byte[A.Length];
        string storage = Path.Combine(basePath, "hash");
        Reject(() => Installer(storage, wrongHash).Install(ManifestUrl, CancellationToken.None, null), "wrong SHA rejected");
        Assert(!File.Exists(Path.Combine(storage, "active-resource-set.txt")), "failed hash never activates");
        var tooLong = new FakeHttp(); tooLong.Bodies["https://cdn.example.com/releases/1/packages/a.pkg"] = new byte[A.Length + 1];
        Reject(() => Installer(Path.Combine(basePath, "long"), tooLong).Install(ManifestUrl, CancellationToken.None, null), "oversize resource rejected");
        var tooShort = new FakeHttp(); tooShort.Bodies["https://cdn.example.com/releases/1/packages/a.pkg"] = new byte[A.Length - 1];
        Reject(() => Installer(Path.Combine(basePath, "short"), tooShort).Install(ManifestUrl, CancellationToken.None, null), "truncated resource rejected");
        var invalidUtf8 = new FakeHttp(); invalidUtf8.Bodies[ManifestUrl] = new byte[] { 0xc3, 0x28 };
        Reject(() => Installer(Path.Combine(basePath, "utf8"), invalidUtf8).Install(ManifestUrl, CancellationToken.None, null), "invalid UTF8 rejected");
        var tooLarge = new FakeHttp(); tooLarge.Bodies[ManifestUrl] = new byte[MobileResourceManifest.MaxManifestBytes + 1];
        Reject(() => Installer(Path.Combine(basePath, "large"), tooLarge).Install(ManifestUrl, CancellationToken.None, null), "manifest stream size bound");
        var incompatible = new FakeHttp(Manifest("2.0.0"));
        Reject(() => Installer(Path.Combine(basePath, "incompatible"), incompatible).Install(ManifestUrl, CancellationToken.None, null), "incompatible content rejected before package transfer");
        Assert(incompatible.Requests.Count == 1, "no resource requested after incompatible manifest");
        var extra = new FakeHttp();
        extra.AfterWrite = path => { if (path.EndsWith("a.json.part")) File.WriteAllText(Path.Combine(Path.GetDirectoryName(path), "injected.lua"), "evil"); };
        Reject(() => Installer(Path.Combine(basePath, "extra"), extra).Install(ManifestUrl, CancellationToken.None, null), "unexpected staged file rejected");
    }

    private static void TestActivationFailure(string storage)
    {
        string previous = Installer(storage, new FakeHttp()).Install(ManifestUrl, CancellationToken.None, null);
        string marker = Path.Combine(storage, "active-resource-set.txt");
        string before = File.ReadAllText(marker);
        // A filesystem write failure at the final metadata boundary must keep the old set.
        Directory.CreateDirectory(marker + ".part");
        var update = new FakeHttp(Manifest().Replace("tables/a.json\tgameassets", "tables/a2.json\tgameassets"));
        update.Bodies["https://cdn.example.com/releases/1/tables/a2.json"] = B;
        Reject(() => Installer(storage, update).Install(ManifestUrl, CancellationToken.None, null), "failed activation marker write does not publish the new set");
        Assert(File.ReadAllText(marker) == before && Directory.Exists(previous), "old marker and generation survive activation failure");
        Directory.Delete(marker + ".part");
        Assert(Installer(storage, new FakeHttp()).UseInstalled(CancellationToken.None, null) == previous, "old set remains recoverable after activation failure");
    }

    private static void TestSymlinks(string storage)
    {
        string stage = Path.Combine(storage, MobileResourceManifest.Parse(Manifest()).Version + ".staging");
        Directory.CreateDirectory(stage);
        string outside = Path.Combine(storage, "outside"); Directory.CreateDirectory(outside);
        using (var process = Process.Start(new ProcessStartInfo("ln", "-s \"" + outside + "\" \"" + Path.Combine(stage, "data") + "\"") { UseShellExecute = false }))
        { process.WaitForExit(); Assert(process.ExitCode == 0, "symlink test fixture created"); }
        Reject(() => Installer(storage, new FakeHttp()).Install(ManifestUrl, CancellationToken.None, null), "symlink staging traversal rejected");
        Assert(Directory.GetFiles(outside).Length == 0, "nothing written outside staging");
        Directory.Delete(Path.Combine(stage, "data"));
    }

    private static void TestHttp(string root)
    {
        Directory.CreateDirectory(root);
        Func<string, int, long, bool, string> serve = (response, declaredBodyBytes, maximum, expectFailure) =>
        {
            var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            string headers = null;
            Task server = Task.Run(() =>
            {
                using (var socket = listener.AcceptTcpClient())
                using (var stream = socket.GetStream())
                {
                    var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
                    var text = new StringBuilder(); string line;
                    while (!string.IsNullOrEmpty(line = reader.ReadLine())) text.Append(line).Append('\n');
                    headers = text.ToString();
                    byte[] bytes = Encoding.ASCII.GetBytes(response); stream.Write(bytes, 0, bytes.Length); stream.Flush();
                }
            });
            try
            {
                Action call = () => new MobileResourceHttpTransport().Download(new Uri("http://127.0.0.1:" + port + "/fixture"), Path.Combine(root, "out"), maximum, CancellationToken.None, null);
                if (expectFailure) Reject(call, "HTTP response rejected"); else { call(); checks++; }
                Assert(server.Wait(5000), "fake HTTP server completed");
                Assert(headers != null && !headers.Contains("Authorization:") && !headers.Contains("Cookie:"), "no authentication or cookies sent");
                return headers;
            }
            finally { listener.Stop(); }
        };
        serve("HTTP/1.1 200 OK\r\nContent-Length: 3\r\nConnection: close\r\n\r\nabc", 3, 3, false);
        Assert(File.ReadAllText(Path.Combine(root, "out")) == "abc", "production HTTP transport writes exact body");
        serve("HTTP/1.1 302 Found\r\nLocation: http://127.0.0.1:1/never-request\r\nContent-Length: 0\r\nConnection: close\r\n\r\n", 0, 3, true);
        serve("HTTP/1.1 200 OK\r\nContent-Length: 4\r\nConnection: close\r\n\r\nabcd", 4, 3, true);
        serve("HTTP/1.1 200 OK\r\nContent-Encoding: gzip\r\nContent-Length: 3\r\nConnection: close\r\n\r\nabc", 3, 3, true);
        serve("HTTP/1.1 200 OK\r\nConnection: close\r\n\r\nabcd", 4, 3, true);
    }
}
