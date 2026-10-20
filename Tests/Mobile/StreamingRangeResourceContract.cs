using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

// Standalone managed contracts: fake resource bytes, temporary files and loopback HTTP only.
internal static class StreamingRangeResourceContract
{
    private static int checks;
    private const string Content = "abcdefghij";
    private static readonly MobileResourceHttpTransport Transport = new MobileResourceHttpTransport();

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }

    private static void Reject(Action action, string message)
    {
        try { action(); }
        catch (InvalidDataException) { checks++; return; }
        catch (IOException) { checks++; return; }
        catch (WebException) { checks++; return; }
        throw new Exception("Expected rejected transfer: " + message);
    }

    private static void Throws<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { checks++; return; }
        throw new Exception("Expected " + typeof(T).Name + ": " + message);
    }

    private static string Response(string status, string headers, string body)
    {
        return "HTTP/1.1 " + status + "\r\nConnection: close\r\n" + headers + "\r\n" + body;
    }

    private static void Send(NetworkStream stream, string bytes)
    {
        byte[] data = Encoding.ASCII.GetBytes(bytes);
        stream.Write(data, 0, data.Length);
        stream.Flush();
    }

    private static string Serve(string response, Action<Uri> action)
    {
        return Serve(stream => Send(stream, response), action);
    }

    private static string Serve(Action<NetworkStream> response, Action<Uri> action)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        string headers = null;
        Task server = Task.Run(() =>
        {
            using (var socket = listener.AcceptTcpClient())
            using (var stream = socket.GetStream())
            {
                stream.ReadTimeout = 5000;
                stream.WriteTimeout = 5000;
                using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true))
                {
                    var text = new StringBuilder();
                    string line;
                    while (!string.IsNullOrEmpty(line = reader.ReadLine())) text.Append(line).Append('\n');
                    headers = text.ToString();
                }
                response(stream);
            }
        });
        try
        {
            action(new Uri("http://127.0.0.1:" + port + "/fixture"));
            Assert(server.Wait(5000), "Loopback HTTP server completed");
            Assert(headers != null && headers.IndexOf("Authorization:", StringComparison.OrdinalIgnoreCase) < 0 &&
                headers.IndexOf("Cookie:", StringComparison.OrdinalIgnoreCase) < 0, "No credentials/cookies sent");
            return headers;
        }
        finally { listener.Stop(); }
    }

    private static void Download(Uri uri, string file, long size, long offset, Action<long> progress = null)
    {
        var proof = Transport.DownloadVerifiedResumable(uri, file, size, offset, CancellationToken.None, progress);
        Assert(proof.Matches(file, new FileInfo(file).Length, MobileResourceManifest.HashFile(file), File.GetLastWriteTimeUtc(file).Ticks), "Streaming digest covers the full committed bytes");
    }

    public static void Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "ffo-range-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string file = Path.Combine(root, "resource.part");
            TestStreamingProofs(file);
            TestSuccessfulTransfers(file);
            TestRejectedHeaders(file);
            TestIncompleteAndOversizedBodies(file);
            TestCancellation(file);
            TestOffsetValidation(file);
            TestChangedPartial(file);
            Console.WriteLine("PASS resumable HTTP resource contracts (" + checks +
                " checks; loopback HTTP/temp files only, no Unity/device/production validation)");
        }
        finally { Directory.Delete(root, true); }
    }

    private static void TestStreamingProofs(string file)
    {
        string root = Path.GetDirectoryName(file);
        byte[] bytes = Encoding.ASCII.GetBytes(Content); string digest;
        using (var sha = SHA256.Create()) digest = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        var manifest = MobileResourceManifest.Parse("FFO-MOBILE-1\n@client\tandroid\t1.0.0\nfixture\tgameassets/file.bin\t10\t" + digest + "\n");
        string targetRoot = Path.Combine(root,"proof-install");Directory.CreateDirectory(Path.Combine(targetRoot,"gameassets"));
        var verification = new MobileResourceVerification(manifest,targetRoot,Path.Combine(root,"proof-receipt"));
        MobileResourceVerifiedFile proof = null; long prefix = 0;
        Serve(Response("200 OK","Content-Length: 10\r\n",Content),uri=>proof=Transport.DownloadVerifiedResumable(uri,file,10,0,CancellationToken.None,null,b=>prefix=b));
        long reread=-1;verification.CommitFile(0,file,proof,b=>reread=b);verification.CommitReceipt();
        Assert(reread==-1 && prefix==0,"fresh streaming proof avoids post-download payload hash/read");
        Assert(File.ReadAllText(Path.Combine(targetRoot,"gameassets/file.bin"))==Content,"verified streaming bytes committed exactly");
        File.WriteAllText(file,"aXc");
        Serve(Response("206 Partial Content","Content-Range: bytes 3-9/10\r\nContent-Length: 7\r\n","defghij"),uri=>proof=Transport.DownloadVerifiedResumable(uri,file,10,3,CancellationToken.None,null,b=>prefix=b));
        Assert(prefix==3,"resume hashes retained prefix once");
        Reject(()=>verification.CommitFile(0,file,proof),"corrupt retained prefix cannot pass full-file expected digest");
        File.WriteAllText(file,"abc");
        Serve(Response("206 Partial Content","Content-Range: bytes 3-9/10\r\nContent-Length: 7\r\n","defghij"),uri=>proof=Transport.DownloadVerifiedResumable(uri,file,10,3,CancellationToken.None,null));
        File.SetLastWriteTimeUtc(file,File.GetLastWriteTimeUtc(file).AddSeconds(3));
        Reject(()=>verification.CommitFile(0,file,proof),"changed file metadata invalidates streaming proof before commit");
        File.WriteAllText(file,"OLD");prefix=0;
        Serve(Response("200 OK","Content-Length: 10\r\n",Content),uri=>proof=Transport.DownloadVerifiedResumable(uri,file,10,3,CancellationToken.None,null,b=>prefix=b));
        Assert(prefix==0,"ignored Range restart does not hash stale prefix");
        verification.CommitFile(0,file,proof);verification.CommitReceipt();
    }

    private static void TestSuccessfulTransfers(string file)
    {
        IMobileResourceTransport original = Transport;
        Assert(original is IMobileResourceResumableTransport, "Existing transport contract remains compatible");
        var progress = new List<long>();
        string request = Serve(Response("200 OK", "Content-Length: 10\r\n", Content),
            uri => Download(uri, file, 10, 0, progress.Add));
        Assert(File.ReadAllText(file) == Content, "Fresh exact transfer writes body");
        Assert(!request.Contains("Range:"), "Offset zero does not send Range");
        Assert(progress[0] == 0 && progress[progress.Count - 1] == 10, "Fresh progress is total bytes");

        File.WriteAllText(file, "abc");
        progress.Clear();
        request = Serve(Response("206 Partial Content", "Content-Range: bytes 3-9/10\r\nContent-Length: 7\r\n", "defghij"),
            uri => Download(uri, file, 10, 3, progress.Add));
        Assert(request.Contains("Range: bytes=3-"), "Resume sends the exact Range offset");
        Assert(File.ReadAllText(file) == Content, "Valid 206 appends exactly once");
        Assert(progress[0] == 3 && progress[progress.Count - 1] == 10, "Resume progress includes existing bytes");
        for (int i = 1; i < progress.Count; i++) Assert(progress[i] >= progress[i - 1] && progress[i] <= 10, "Resume progress is bounded/monotonic");

        File.WriteAllText(file, "OLD");
        progress.Clear();
        Serve(Response("200 OK", "Content-Length: 10\r\n", Content), uri => Download(uri, file, 10, 3, progress.Add));
        Assert(File.ReadAllText(file) == Content, "Range ignored with 200 truncates old prefix and restarts");
        Assert(progress[0] == 0 && progress[progress.Count - 1] == 10, "200 fallback resets progress to full-body bytes");

        File.WriteAllText(file, "abc");
        Serve(Response("206 Partial Content", "Content-Range: bytes 3-9/10\r\n", "defghij"), uri => Download(uri, file, 10, 3));
        Assert(File.ReadAllText(file) == Content, "Close-delimited range still requires exact length");

        File.WriteAllText(file, "abc");
        Serve(Response("206 Partial Content", "Content-Range: bytes 3-9/10\r\nTransfer-Encoding: chunked\r\n", "3\r\ndef\r\n4\r\nghij\r\n0\r\n\r\n"),
            uri => Download(uri, file, 10, 3));
        Assert(File.ReadAllText(file) == Content, "Chunked range validates exact decoded transfer-body length");

        File.WriteAllText(file, "abcdefghi");
        Serve(Response("206 Partial Content", "Content-Range: bytes 9-9/10\r\nContent-Length: 1\r\nContent-Encoding: identity\r\n", "j"),
            uri => Download(uri, file, 10, 9));
        Assert(File.ReadAllText(file) == Content, "Last-byte range works");

        Serve(Response("200 OK", "Content-Length: 0\r\n", ""), uri => Download(uri, file, 0, 0));
        Assert(new FileInfo(file).Length == 0, "Empty resource can complete from offset zero");

        Serve(Response("200 OK", "Content-Length: 3\r\n", "abc"),
            uri => original.Download(uri, file, 100, CancellationToken.None, null));
        Assert(File.ReadAllText(file) == "abc", "Original Download remains a maximum-bound transfer");
    }

    private static void TestRejectedHeaders(string file)
    {
        foreach (string range in new[] { null, "x", "bytes", "bytes ", "bytes 0-9/10", "bytes 4-9/10", "bytes 3-8/10", "bytes 3-10/10",
            "bytes 3-9/11", "bytes 3-9/*", "bytes */10", "bytes +3-9/10", "bytes 3-9/+10", "bytes 3-9/9223372036854775808",
            "items 3-9/10", "bytes 3-9/10, 3-9/10", "bytes 3-9/10/10", "bytes -3-9/10" })
        {
            File.WriteAllText(file, "abc");
            string header = range == null ? "" : "Content-Range: " + range + "\r\n";
            Serve(Response("206 Partial Content", header + "Content-Length: 7\r\n", "defghij"),
                uri => Reject(() => Download(uri, file, 10, 3), "Malformed range " + range));
            Assert(File.ReadAllText(file) == "abc", "Invalid Content-Range never touches the existing partial");
        }
        foreach (string response in new[]
        {
            Response("416 Range Not Satisfiable", "Content-Range: bytes */10\r\nContent-Length: 0\r\n", ""),
            Response("302 Found", "Location: http://127.0.0.1:1/do-not-follow\r\nContent-Length: 0\r\n", ""),
            Response("206 Partial Content", "Content-Range: bytes 3-9/10\r\nContent-Length: 7\r\nContent-Encoding: gzip\r\n", "defghij"),
            Response("206 Partial Content", "Content-Range: bytes 3-9/10\r\nContent-Length: 6\r\n", "defghi"),
            Response("206 Partial Content", "Content-Range: bytes 3-9/10\r\nContent-Length: 8\r\n", "defghijk"),
            Response("200 OK", "Content-Length: 7\r\n", "defghij"),
            Response("200 OK", "Content-Length: 11\r\n", "abcdefghijk"),
            Response("200 OK", "Content-Length: 10\r\nContent-Encoding: br\r\n", Content),
            Response("204 No Content", "Content-Length: 0\r\n", "")
        })
        {
            File.WriteAllText(file, "abc");
            Serve(response, uri => Reject(() => Download(uri, file, 10, 3), "Invalid response status/length/encoding"));
            Assert(File.ReadAllText(file) == "abc", "Rejected status/length/encoding preserves the previous partial");
        }
        File.WriteAllText(file, "original");
        Serve(Response("206 Partial Content", "Content-Range: bytes 0-9/10\r\nContent-Length: 10\r\n", Content),
            uri => Reject(() => Download(uri, file, 10, 0), "Unsolicited 206"));
        Assert(File.ReadAllText(file) == "original", "Offset zero rejects unsolicited partial responses without truncation");
    }

    private static void TestIncompleteAndOversizedBodies(string file)
    {
        foreach (string headers in new[] { "Content-Length: 7\r\n", "" })
        {
            File.WriteAllText(file, "abc");
            Serve(Response("206 Partial Content", "Content-Range: bytes 3-9/10\r\n" + headers, "def"),
                uri => Reject(() => Download(uri, file, 10, 3), "Premature EOF or disconnected response"));
            Assert(File.ReadAllText(file) == "abcdef", "Interrupted range retains all successfully written bytes");
            string request = Serve(Response("206 Partial Content", "Content-Range: bytes 6-9/10\r\nContent-Length: 4\r\n", "ghij"),
                uri => Download(uri, file, 10, 6));
            Assert(request.Contains("Range: bytes=6-") && File.ReadAllText(file) == Content, "Retry resumes the retained prefix to exact completion");
        }
        File.WriteAllText(file, "OLD");
        Serve(Response("200 OK", "", "abcdef"), uri => Reject(() => Download(uri, file, 10, 3), "Short full-body restart"));
        Assert(File.ReadAllText(file) == "abcdef", "Interrupted 200 restart retains new bytes, never appends to old prefix");

        File.WriteAllText(file, "abc");
        Serve(Response("206 Partial Content", "Content-Range: bytes 3-9/10\r\n", "defghijk"),
            uri => Reject(() => Download(uri, file, 10, 3), "Oversized unknown-length range"));
        Assert(new FileInfo(file).Length <= 10 && File.ReadAllText(file).StartsWith("abc"), "Oversized range cannot write beyond total limit");

        File.WriteAllText(file, "abc");
        Serve(Response("206 Partial Content", "Content-Range: bytes 3-9/10\r\nTransfer-Encoding: chunked\r\n", "8\r\ndefghijk\r\n0\r\n\r\n"),
            uri => Reject(() => Download(uri, file, 10, 3), "Oversized chunked range"));
        Assert(new FileInfo(file).Length <= 10, "Chunked body cannot bypass total length bound");
    }

    private static void TestCancellation(string file)
    {
        foreach (bool restart in new[] { false, true })
        {
            using (var cancellation = new CancellationTokenSource())
            using (var releaseServer = new ManualResetEventSlim(false))
            {
                File.WriteAllText(file, restart ? "OLD" : "abc");
                const int size = 65539;
                Serve(stream =>
                {
                    string headers = "Content-Length: " + (restart ? size : size - 3) + "\r\n";
                    if (!restart) headers += "Content-Range: bytes 3-65538/65539\r\n";
                    Send(stream, Response(restart ? "200 OK" : "206 Partial Content", headers, new string('d', 16384)));
                    if (!releaseServer.Wait(5000)) throw new Exception("Cancellation progress callback did not arrive");
                }, uri => Throws<OperationCanceledException>(() => Transport.DownloadVerifiedResumable(uri, file, size, 3, cancellation.Token, total =>
                {
                    if (total > 3) { cancellation.Cancel(); releaseServer.Set(); }
                }), "Cancellation keeps partial bytes"));
                string partial = File.ReadAllText(file);
                Assert(partial.Length > 3 && partial.Length < size, "Cancelled transfer retains a bounded incomplete file");
                Assert(restart ? partial == new string('d', partial.Length) : partial == "abc" + new string('d', partial.Length - 3),
                    "Cancellation preserves correct prefix after " + (restart ? "200 restart" : "206 append"));
            }
        }
        using (var cancellation = new CancellationTokenSource())
        using (var progressSeen = new ManualResetEventSlim(false))
        using (var releaseServer = new ManualResetEventSlim(false))
        {
            File.WriteAllText(file, "abc");
            Serve(stream =>
            {
                Send(stream, Response("206 Partial Content", "Content-Range: bytes 3-9/10\r\nContent-Length: 7\r\n", "d"));
                if (!releaseServer.Wait(5000)) throw new Exception("Blocked-read cancellation did not finish");
            }, uri =>
            {
                Task<Exception> transfer = Task.Run(() =>
                {
                    try
                    {
                        Transport.DownloadVerifiedResumable(uri, file, 10, 3, cancellation.Token,
                            total => { if (total > 3) progressSeen.Set(); });
                        return null;
                    }
                    catch (Exception error) { return error; }
                });
                try
                {
                    bool received = progressSeen.Wait(5000);
                    cancellation.Cancel();
                    bool stopped = transfer.Wait(5000);
                    Assert(received, "Received first range byte before the server stalls");
                    Assert(stopped && transfer.Result is OperationCanceledException, "External cancellation aborts a stalled response read");
                    Assert(File.ReadAllText(file) == "abcd", "Cancellation while waiting for more data retains the flushed prefix");
                }
                finally { releaseServer.Set(); }
            });
        }
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            File.WriteAllText(file, "abc");
            Throws<OperationCanceledException>(() => Transport.DownloadVerifiedResumable(new Uri("http://127.0.0.1:1/unused"), file, 10, 3,
                cancellation.Token, null), "Already cancelled before request");
            Assert(File.ReadAllText(file) == "abc", "Pre-cancelled request does not touch partial");
        }
    }

    private static void TestOffsetValidation(string file)
    {
        var uri = new Uri("http://127.0.0.1:1/not-requested");
        File.WriteAllText(file, "abc");
        foreach (long offset in new long[] { -1, 10, 11, long.MaxValue })
            Throws<ArgumentOutOfRangeException>(() => Download(uri, file, 10, offset), "Invalid resume offset");
        Throws<ArgumentOutOfRangeException>(() => Download(uri, file, -1, 0), "Negative expected bytes");
        Reject(() => Download(uri, file, 10, 2), "Partial length mismatches offset");
        Reject(() => Download(uri, file + ".missing", 10, 3), "Nonexistent partial cannot resume");
        Throws<ArgumentException>(() => Download(new Uri("http://user:password@127.0.0.1:1/unused"), file, 10, 3), "Credentials in URI rejected");
        Throws<ArgumentException>(() => Download(new Uri("file:///unused"), file, 10, 3), "Non-HTTP scheme rejected");
        Assert(File.ReadAllText(file) == "abc", "Invalid arguments never touch partial");
    }

    private static void TestChangedPartial(string file)
    {
        foreach (bool restart in new[] { false, true })
        {
            File.WriteAllText(file, "abc");
            Serve(stream =>
            {
                File.WriteAllText(file, "changed");
                Send(stream, restart ? Response("200 OK", "Content-Length: 10\r\n", Content) :
                    Response("206 Partial Content", "Content-Range: bytes 3-9/10\r\nContent-Length: 7\r\n", "defghij"));
            }, uri => Reject(() => Download(uri, file, 10, 3), "Partial changed while awaiting response"));
            Assert(File.ReadAllText(file) == "changed", "Recheck under file lock prevents stale-offset write/truncation");
        }
    }
}
