using System;
using System.IO;
using System.Net;
using System.Threading;

namespace FakeFantasyOnline.Foundation
{
/// <summary>Injectable transfer boundary. Implementations must not follow redirects or send credentials.</summary>
public interface IMobileResourceTransport
{
    void Download(Uri uri, string destination, long maximumBytes, CancellationToken cancellation, Action<long> progress);
}

/// <summary>Runs on a worker thread; bounded streaming, normal TLS validation, no redirects/cookies/authentication.</summary>
public sealed class MobileResourceHttpTransport : IMobileResourceTransport
{
    public void Download(Uri uri, string destination, long maximumBytes, CancellationToken cancellation, Action<long> progress)
    {
        if (maximumBytes < 0) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        cancellation.ThrowIfCancellationRequested();
        var request = (HttpWebRequest)WebRequest.Create(uri);
        request.Method = "GET";
        request.AllowAutoRedirect = false;
        request.UseDefaultCredentials = false;
        request.Credentials = null;
        request.CookieContainer = null;
        request.Proxy = null;
        request.AutomaticDecompression = DecompressionMethods.None;
        request.Timeout = 30000;
        request.ReadWriteTimeout = 30000;
        // No shared authentication, client certificate or certificate-validation override.
        using (cancellation.Register(request.Abort))
        using (var response = (HttpWebResponse)request.GetResponse())
        {
            if (response.StatusCode != HttpStatusCode.OK || response.ResponseUri != uri ||
                (!string.IsNullOrEmpty(response.ContentEncoding) && response.ContentEncoding != "identity"))
                throw new InvalidDataException("Resource server must return direct, unencoded HTTP 200 responses.");
            if (response.ContentLength > maximumBytes)
                throw new InvalidDataException("Resource response exceeds its declared size limit.");
            using (var input = response.GetResponseStream())
            using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                CopyBounded(input, output, maximumBytes, cancellation, progress);
                output.Flush(true);
            }
        }
    }

    // Also tested with HTTP-like streams that omit/lie about Content-Length.
    public static void CopyBounded(Stream input, Stream output, long maximumBytes, CancellationToken cancellation, Action<long> progress)
    {
        var buffer = new byte[65536];
        long received = 0;
        progress?.Invoke(0);
        while (true)
        {
            cancellation.ThrowIfCancellationRequested();
            int count = input.Read(buffer, 0, buffer.Length);
            if (count == 0) break;
            if (count > maximumBytes - received)
                throw new InvalidDataException("Resource response exceeds its declared size limit.");
            output.Write(buffer, 0, count);
            received += count;
            progress?.Invoke(received);
        }
        output.Flush();
    }
}
}
