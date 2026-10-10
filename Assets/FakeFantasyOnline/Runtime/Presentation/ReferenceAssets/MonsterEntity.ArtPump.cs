using System.Threading;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official MonsterEntity art pump body: the fair-share upload passes and the scheduling
/// pass that PumpArtJobs wraps in the upload clock.
/// </summary>
public partial class MonsterEntity
{
    private static void PumpArtJobsInner()
    {
        _artPumpBuf.Clear();
        foreach (ArtJob job in _artJobs.Values)
        {
            _artPumpBuf.Add(job);
        }

        long pendingBytes = 0L;
        for (int index = 0; index < _artPumpBuf.Count; index++)
        {
            pendingBytes += Interlocked.CompareExchange(ref _artPumpBuf[index].pendingBytes, 0, 0);
        }

        int divisor = _artPumpBuf.Count < 2 ? 1 : _artPumpBuf.Count;
        int fairBytes = ArtUploadBytesPerFrame / divisor;
        if (fairBytes <= 262144)
        {
            fairBytes = 262144;
        }

        int fairTextures = ArtTexPerFrame / divisor;
        if (fairTextures < 3)
        {
            fairTextures = 2;
        }

        int remainingBytes = ArtUploadBytesPerFrame;
        int remainingTextures = ArtTexPerFrame;
        for (int index = 0; index < _artPumpBuf.Count; index++)
        {
            ArtJob job = _artPumpBuf[index];
            if (job.cancelled || remainingBytes <= 0 || remainingTextures <= 0)
            {
                continue;
            }

            int budget = remainingBytes < fairBytes ? remainingBytes : fairBytes;
            int quota = remainingTextures < fairTextures ? remainingTextures : fairTextures;
            int beforeBudget = budget;
            int beforeQuota = quota;
            UploadReady(job, ref budget, ref quota);
            remainingBytes -= beforeBudget - budget;
            remainingTextures -= beforeQuota - quota;
        }

        for (int index = 0; index < _artPumpBuf.Count; index++)
        {
            ArtJob job = _artPumpBuf[index];
            if (job.cancelled || remainingBytes <= 0 || remainingTextures <= 0)
            {
                continue;
            }

            UploadReady(job, ref remainingBytes, ref remainingTextures);
        }

        for (int index = 0; index < _artPumpBuf.Count; index++)
        {
            ArtJob job = _artPumpBuf[index];
            if (job.cancelled || pendingBytes > ArtPendingBytesMax)
            {
                continue;
            }

            if (job.running == null || job.running.IsCompleted)
            {
                job.running = null;
                ScheduleNextArt(job);
            }
        }
    }
}

}
