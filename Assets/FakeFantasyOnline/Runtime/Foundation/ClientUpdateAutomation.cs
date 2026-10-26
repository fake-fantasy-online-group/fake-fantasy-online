using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace FakeFantasyOnline.Foundation
{
/// <summary>Bounded network recovery and a durable stop against reinstall/rollback loops.</summary>
public static class ClientUpdateAutomation
{
    public const int MaximumNetworkAttempts = 3;
    public static T WithNetworkRetry<T>(Func<T> operation, CancellationToken cancellation,
        Action<int> retrying = null, Action<int, CancellationToken> wait = null)
    {
        for (int attempt = 1; ; attempt++)
        {
            cancellation.ThrowIfCancellationRequested();
            try { return operation(); }
            catch (WebException error)
            {
                if (attempt >= MaximumNetworkAttempts || !Transient(error)) throw;
                retrying?.Invoke(attempt);
                int milliseconds = attempt == 1 ? 1000 : 3000;
                if (wait != null) wait(milliseconds, cancellation);
                else if (cancellation.WaitHandle.WaitOne(milliseconds)) cancellation.ThrowIfCancellationRequested();
            }
        }
    }
    private static bool Transient(WebException error)
    {
        switch (error.Status)
        {
            case WebExceptionStatus.Timeout: case WebExceptionStatus.ConnectFailure:
            case WebExceptionStatus.ConnectionClosed: case WebExceptionStatus.NameResolutionFailure:
            case WebExceptionStatus.ReceiveFailure: case WebExceptionStatus.SendFailure: return true;
            case WebExceptionStatus.ProtocolError:
                var response = error.Response as HttpWebResponse;
                if (response == null) return false;
                int code = (int)response.StatusCode;
                return code == 408 || code == 429 || code >= 500 && code <= 599;
            default: return false;
        }
    }

    // Written BEFORE native preparation. If the old app returns after a failed/rolled-back
    // install, it must not repeatedly exit and reinstall the same artifact. A deliberate
    // Retry may overwrite this marker; a newer app or a different artifact is a new attempt.
    private static string AttemptPath(string root) => Path.Combine(root, "client-update-attempt.txt");
    private static string Key(string currentVersion, string identity)
    {
        using (var hash = SHA256.Create())
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(currentVersion + "\n" + identity))).Replace("-", "");
    }
    public static bool HasAttempt(string root, string currentVersion, string identity)
    {
        string path = AttemptPath(root);
        DesktopUpdateFiles.NoLinks(path);
        if (!File.Exists(path)) return false;
        if (new FileInfo(path).Length > 128) throw new InvalidDataException("Invalid client update attempt record.");
        string saved = File.ReadAllText(path);
        if (saved.Length != 64) throw new InvalidDataException("Incomplete client update attempt record.");
        foreach (char c in saved)
            if (!(c >= '0' && c <= '9') && !(c >= 'A' && c <= 'F'))
                throw new InvalidDataException("Invalid client update attempt record.");
        return saved == Key(currentVersion, identity);
    }
    public static void RecordAttempt(string root, string currentVersion, string identity)
    {
        string path = AttemptPath(root);
        DesktopUpdateFiles.NoLinks(path);
        Directory.CreateDirectory(root);
        // A truncated/interrupted write is deliberately treated as blocked, not permission
        // to automatically reinstall. One tiny record never contains URLs or credentials.
        byte[] bytes = Encoding.UTF8.GetBytes(Key(currentVersion, identity));
        using (var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
        { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
    }
}
}
