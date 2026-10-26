using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using FakeFantasyOnline.Foundation;

// Inert loopback HTTP bytes exercise the real transport, retry wrapper, identity-bound
// artifact resume and full SHA verification. No installer, Unity or external endpoint.
internal static class ClientNetworkRecoveryContract
{
    private static int checks;
    private static readonly byte[] Payload = Encoding.ASCII.GetBytes("synthetic-update-package-only");
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    private sealed class Server : IDisposable
    {
        private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        private readonly Thread worker;
        private Exception failure;
        private volatile bool stopped;
        public int Requests;
        public string Url { get; private set; }
        public Server(Func<int, string, byte[]> response)
        {
            listener.Start(); Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/client.zip";
            worker = new Thread(() => {
                try
                {
                    while (true)
                    using (var client = listener.AcceptTcpClient())
                    using (var stream = client.GetStream())
                    {
                        stream.ReadTimeout = stream.WriteTimeout = 5000;
                        var request = new StringBuilder();
                        while (!request.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
                        {
                            int next = stream.ReadByte(); if (next < 0 || request.Length >= 16384) throw new IOException("Invalid synthetic request");
                            request.Append((char)next);
                        }
                        byte[] bytes = response(Interlocked.Increment(ref Requests), request.ToString());
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }
                catch (Exception error) { if (!stopped) failure = error; }
            }) { IsBackground = true };
            worker.Start();
        }
        public void Dispose()
        {
            stopped = true; listener.Stop();
            if (!worker.Join(6000)) throw new Exception("Synthetic HTTP worker failed to stop");
            if (failure != null) throw new Exception("Synthetic HTTP worker failed", failure);
        }
    }
    private static byte[] Response(string status, string headers, int offset, int length, bool corrupt = false)
    {
        byte[] prefix = Encoding.ASCII.GetBytes("HTTP/1.1 " + status + "\r\nConnection: close\r\n" + headers + "\r\n");
        var bytes = new byte[prefix.Length + length]; Buffer.BlockCopy(prefix, 0, bytes, 0, prefix.Length);
        if (!corrupt) Buffer.BlockCopy(Payload, offset, bytes, prefix.Length, length);
        return bytes;
    }
    private static ClientUpdatePlan.ClientArtifact Artifact(string url)
    {
        string hash;
        using (var sha = System.Security.Cryptography.SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(Payload)).Replace("-", "").ToLowerInvariant();
        return new ClientUpdatePlan.ClientArtifact { Version = "2.0.0", MinVersion = "1.0.0", Url = url, SizeBytes = Payload.Length, Sha256 = hash };
    }
    private static void ResumeAfterInterruptedBody(string root, bool advertisedLength)
    {
        int retries = 0; bool rangeSeen = false;
        using (var server = new Server((attempt, request) => {
            if (attempt == 1) return Response("200 OK", advertisedLength ? "Content-Length: " + Payload.Length + "\r\n" : "", 0, 5);
            rangeSeen = request.IndexOf("Range: bytes=5-", StringComparison.OrdinalIgnoreCase) >= 0;
            return Response("206 Partial Content", "Content-Length: " + (Payload.Length - 5) + "\r\nContent-Range: bytes 5-" + (Payload.Length - 1) + "/" + Payload.Length + "\r\n", 5, Payload.Length - 5);
        }))
        {
            var downloader = new ClientArtifactDownload(root, "1.0.0", "windows", true, new MobileResourceHttpTransport(), p => long.MaxValue);
            var artifact = Artifact(server.Url);
            string path = ClientUpdateAutomation.WithNetworkRetry(() => downloader.Download(artifact, CancellationToken.None),
                CancellationToken.None, attempt => retries++, (delay, token) => { });
            Check(server.Requests == 2 && retries == 1 && rangeSeen, "interrupted " + (advertisedLength ? "fixed-length" : "close-delimited") + " body automatically resumes retained offset");
            Check(Convert.ToBase64String(File.ReadAllBytes(path)) == Convert.ToBase64String(Payload), "resumed payload passes exact full-file verification before completion");
        }
    }
    private static void NonRetryableFailures(string root)
    {
        using (var server = new Server((attempt, request) => Response("200 OK", "Content-Length: " + Payload.Length + "\r\n", 0, Payload.Length, true)))
        {
            int retries = 0;
            var downloader = new ClientArtifactDownload(Path.Combine(root, "hash"), "1.0.0", "windows", true, new MobileResourceHttpTransport(), p => long.MaxValue);
            try { ClientUpdateAutomation.WithNetworkRetry(() => downloader.Download(Artifact(server.Url), CancellationToken.None), CancellationToken.None,
                attempt => retries++, (delay, token) => { }); throw new Exception("Expected hash failure"); }
            catch (InvalidDataException) { }
            Check(server.Requests == 1 && retries == 0, "completed hash mismatch is not retried or trusted");
        }
        using (var server = new Server((attempt, request) => Response("200 OK", "Content-Length: " + Payload.Length + "\r\n", 0, Payload.Length)))
        {
            int retries = 0;
            try { ClientUpdateAutomation.WithNetworkRetry(() => {
                new MobileResourceHttpTransport().Download(new Uri(server.Url), Path.Combine(root, "missing-parent", "file"), Payload.Length, CancellationToken.None, null); return true;
            }, CancellationToken.None, attempt => retries++, (delay, token) => { }); throw new Exception("Expected local filesystem failure"); }
            catch (DirectoryNotFoundException) { }
            Check(server.Requests == 1 && retries == 0, "local file-open IO failure is not normalized into network retry");
        }
    }
    public static void Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "client-network-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            ResumeAfterInterruptedBody(Path.Combine(root, "fixed-length"), true);
            ResumeAfterInterruptedBody(Path.Combine(root, "close-delimited"), false);
            NonRetryableFailures(root);
            Console.WriteLine("PASS client network recovery: " + checks + " checks; real inert loopback HTTP, no Unity/device/install execution.");
        }
        finally { Directory.Delete(root, true); }
    }
}
