using System;
namespace FakeFantasyOnline.Foundation
{
/// <summary>Rate from payload byte deltas and monotonic time, never from hash/copy progress or a resume offset.</summary>
public sealed class ClientDownloadRate
{
    private string key;
    private long startBytes, previousBytes;
    private double startTime, lastMovement;
    private double? measured;
    public void Reset() { key = null; measured = null; }
    public void Observe(string transferKey, long bytes, double seconds, bool networkTransfer)
    {
        if (!networkTransfer || bytes < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) { Reset(); return; }
        if (key != transferKey || bytes < previousBytes || seconds < startTime)
        {
            key = transferKey; startBytes = previousBytes = bytes; startTime = lastMovement = seconds; measured = null;
            return; // Includes the initial retained Range offset; none of it was just downloaded.
        }
        if (bytes > previousBytes) lastMovement = seconds;
        previousBytes = bytes;
        if (seconds - startTime >= .35)
        {
            measured = (bytes - startBytes) / (seconds - startTime);
            startBytes = bytes; startTime = seconds;
        }
    }
    public double? BytesPerSecond(double seconds)
    {
        if (key == null) return null;
        if (seconds - lastMovement >= 1.5) return 0;
        return measured;
    }
}
}
