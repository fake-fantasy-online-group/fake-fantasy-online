using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading;

namespace FakeFantasyOnline.Foundation
{
/// <summary>Injectable transfer boundary. Implementations may be called concurrently for distinct files;
/// they must not follow redirects/send credentials. Progress reports the initial retained offset
/// (zero for a fresh/200-restarted transfer) before reporting newly received bytes.</summary>
public interface IMobileResourceTransport
{
    void Download(Uri uri, string destination, long maximumBytes, CancellationToken cancellation, Action<long> progress);
}

/// <summary>The caller binds partial bytes to the same URL, expected length and full-file hash.</summary>
public interface IMobileResourceResumableTransport : IMobileResourceTransport
{
    void DownloadResumable(Uri uri, string destination, long expectedBytes, long offset, CancellationToken cancellation, Action<long> progress);
}

/// <summary>Runs on a worker thread; bounded streaming, normal TLS validation, no redirects/cookies/authentication.</summary>
public interface IMobileResourceVerifiedTransport : IMobileResourceResumableTransport
{
    MobileResourceVerifiedFile DownloadVerifiedResumable(Uri uri, string destination, long expectedBytes, long offset,
        CancellationToken cancellation, Action<long> progress, Action<long> prefixProgress = null);
}

/// <summary>Internal transfer proof, valid only for the exact bytes and metadata just written.</summary>
public sealed class MobileResourceVerifiedFile
{
    private readonly string path, hash;
    private readonly long length, stamp;
    internal MobileResourceVerifiedFile(string path, long length, string hash, long stamp)
    { this.path = Path.GetFullPath(path); this.length = length; this.hash = hash; this.stamp = stamp; }
    internal bool Matches(string candidate, long bytes, string digest, long ticks)
    { return Path.GetFullPath(candidate) == path && bytes == length && digest == hash && ticks == stamp; }
}

public sealed class MobileResourceHttpTransport : IMobileResourceVerifiedTransport
{
    public const int ConcurrentConnectionLimit = 4;
    public void Download(Uri uri, string destination, long maximumBytes, CancellationToken cancellation, Action<long> progress)
    {
        if (maximumBytes < 0) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        Transfer(uri, destination, maximumBytes, 0, false, cancellation, progress);
    }

    public void DownloadResumable(Uri uri, string destination, long expectedBytes, long offset, CancellationToken cancellation, Action<long> progress)
    {
        if (expectedBytes < 0) throw new ArgumentOutOfRangeException(nameof(expectedBytes));
        // Empty resources still need a direct empty response. Completed nonempty files belong
        // to the caller's full-file verification path, never an unsatisfiable Range request.
        if (offset < 0 || offset > expectedBytes || (offset == expectedBytes && offset != 0))
            throw new ArgumentOutOfRangeException(nameof(offset));
        Transfer(uri, destination, expectedBytes, offset, true, cancellation, progress);
    }

    public MobileResourceVerifiedFile DownloadVerifiedResumable(Uri uri, string destination, long expectedBytes, long offset,
        CancellationToken cancellation, Action<long> progress, Action<long> prefixProgress = null)
    {
        if (expectedBytes < 0 || offset < 0 || offset > expectedBytes || (offset == expectedBytes && offset != 0))
            throw new ArgumentOutOfRangeException(nameof(offset));
        return Transfer(uri, destination, expectedBytes, offset, true, cancellation, progress, true, prefixProgress);
    }

    private static MobileResourceVerifiedFile Transfer(Uri uri, string destination, long maximumBytes, long offset, bool exact,
        CancellationToken cancellation, Action<long> progress, bool verify = false, Action<long> prefixProgress = null)
    {
        if (uri == null) throw new ArgumentNullException(nameof(uri));
        if (!uri.IsAbsoluteUri || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("Resource transfers require an HTTP(S) URL without credentials.", nameof(uri));
        cancellation.ThrowIfCancellationRequested();
        if (offset > 0 && (!File.Exists(destination) || new FileInfo(destination).Length != offset))
            throw new InvalidDataException("Partial resource length does not match the requested offset.");
        var request = (HttpWebRequest)WebRequest.Create(uri);
        request.Method = "GET";
        request.AllowAutoRedirect = false;
        request.UseDefaultCredentials = false;
        request.Credentials = null;
        request.CookieContainer = null;
        request.Proxy = null;
        // Bound the installer to four workers, but do not let the legacy HTTP
        // endpoint default of two connections silently serialize their transfers.
        // This changes only this origin's pool, never TLS/authentication settings.
        var endpoint = request.ServicePoint;
        lock (endpoint)
            if (endpoint.ConnectionLimit < ConcurrentConnectionLimit)
                endpoint.ConnectionLimit = ConcurrentConnectionLimit;
        request.AutomaticDecompression = DecompressionMethods.None;
        request.Timeout = 30000;
        request.ReadWriteTimeout = 30000;
        if (offset > 0) request.AddRange(offset);
        // No shared authentication, client certificate or certificate-validation override.
        try
        {
            using (cancellation.Register(request.Abort))
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.ResponseUri != uri || (!string.IsNullOrEmpty(response.ContentEncoding) &&
                    !string.Equals(response.ContentEncoding, "identity", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException("Resource server must return direct, unencoded responses.");
                bool append = offset > 0 && response.StatusCode == HttpStatusCode.PartialContent;
                if (append)
                    ValidateContentRange(response.Headers[HttpResponseHeader.ContentRange], offset, maximumBytes);
                else if (response.StatusCode != HttpStatusCode.OK)
                    throw new InvalidDataException("Resource server returned an unexpected HTTP status.");
                long initialBytes = append ? offset : 0;
                long remaining = maximumBytes - initialBytes;
                if (response.ContentLength > remaining || (exact && response.ContentLength >= 0 && response.ContentLength != remaining))
                    throw new InvalidDataException("Resource response length does not match its declared size.");
                cancellation.ThrowIfCancellationRequested();
                // Validate all response headers before opening or truncating the partial file.
                // A server that ignores Range and sends 200 starts again at byte zero.
                using (var input = response.GetResponseStream())
                // Abort alone does not interrupt every runtime's synchronous response Read.
                using (cancellation.Register(input.Dispose))
                using (var output = OpenOutput(destination, offset, append))
                using (var hash = verify ? SHA256.Create() : null)
                {
                    if (hash != null && initialBytes > 0)
                    {
                        // A resumed hash includes the retained prefix. Never persist opaque
                        // hash state or trust only the newly received suffix.
                        output.Position = 0; var buffer = new byte[65536]; long done = 0;
                        while (done < initialBytes)
                        {
                            cancellation.ThrowIfCancellationRequested();
                            int count = output.Read(buffer, 0, (int)Math.Min(buffer.Length, initialBytes - done));
                            if (count == 0) throw new InvalidDataException("Partial resource changed while hashing prefix.");
                            hash.TransformBlock(buffer, 0, count, buffer, 0); done += count; prefixProgress?.Invoke(done);
                        }
                        output.Position = initialBytes;
                    }
                    long received = CopyBoundedFrom(input, output, maximumBytes, initialBytes, cancellation, progress, hash);
                    if (exact && (received != maximumBytes || output.Length != maximumBytes))
                        throw new InvalidDataException("Resource response ended before its declared size.");
                    output.Flush(true);
                    if (hash != null)
                    {
                        hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                        string digest = BitConverter.ToString(hash.Hash).Replace("-", "").ToLowerInvariant();
                        // Close before capturing final metadata, and retain no authority if
                        // the file is subsequently changed before the atomic commit.
                        output.Dispose();
                        return new MobileResourceVerifiedFile(destination, received, digest, File.GetLastWriteTimeUtc(destination).Ticks);
                    }
                }
                return null;
            }
        }
        catch (WebException error)
        {
            // HTTP errors (including 416) never open the destination, but their response
            // still owns a connection. Cancellation and network failures preserve bytes.
            error.Response?.Dispose();
            cancellation.ThrowIfCancellationRequested();
            throw;
        }
        catch (IOException)
        {
            cancellation.ThrowIfCancellationRequested();
            throw;
        }
        catch (ObjectDisposedException)
        {
            cancellation.ThrowIfCancellationRequested();
            throw;
        }
    }

    private static FileStream OpenOutput(string destination, long offset, bool append)
    {
        if (offset == 0) return new FileStream(destination, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        var output = new FileStream(destination, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        try
        {
            // Recheck under the exclusive file handle in case the partial changed during HTTP.
            if (output.Length != offset)
                throw new InvalidDataException("Partial resource length changed during the request.");
            if (!append) output.SetLength(0);
            output.Position = append ? offset : 0;
            return output;
        }
        catch { output.Dispose(); throw; }
    }

    private static void ValidateContentRange(string value, long offset, long expectedBytes)
    {
        long start, end, total;
        int dash = value == null || value.Length < 6 ? -1 : value.IndexOf('-', 6);
        int slash = dash < 0 ? -1 : value.IndexOf('/', dash + 1);
        if (string.IsNullOrEmpty(value) || !value.StartsWith("bytes ", StringComparison.OrdinalIgnoreCase) ||
            dash < 7 || slash <= dash + 1 ||
            !long.TryParse(value.Substring(6, dash - 6), NumberStyles.None, CultureInfo.InvariantCulture, out start) ||
            !long.TryParse(value.Substring(dash + 1, slash - dash - 1), NumberStyles.None, CultureInfo.InvariantCulture, out end) ||
            !long.TryParse(value.Substring(slash + 1), NumberStyles.None, CultureInfo.InvariantCulture, out total) ||
            start != offset || end != expectedBytes - 1 || total != expectedBytes)
            throw new InvalidDataException("Resource server returned an invalid Content-Range.");
    }

    // Also tested with HTTP-like streams that omit/lie about Content-Length.
    public static void CopyBounded(Stream input, Stream output, long maximumBytes, CancellationToken cancellation, Action<long> progress)
    {
        if (maximumBytes < 0) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        CopyBoundedFrom(input, output, maximumBytes, 0, cancellation, progress);
    }

    private static long CopyBoundedFrom(Stream input, Stream output, long maximumBytes, long initialBytes,
        CancellationToken cancellation, Action<long> progress, HashAlgorithm hash = null)
    {
        var buffer = new byte[65536];
        long received = initialBytes;
        progress?.Invoke(received);
        while (true)
        {
            cancellation.ThrowIfCancellationRequested();
            int count = input.Read(buffer, 0, buffer.Length);
            cancellation.ThrowIfCancellationRequested();
            if (count == 0) break;
            if (count > maximumBytes - received)
                throw new InvalidDataException("Resource response exceeds its declared size limit.");
            output.Write(buffer, 0, count);
            hash?.TransformBlock(buffer, 0, count, buffer, 0);
            received += count;
            progress?.Invoke(received);
        }
        output.Flush();
        return received;
    }
}
}
