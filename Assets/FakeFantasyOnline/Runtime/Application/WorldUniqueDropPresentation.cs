using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
/// <summary>Process-lifetime receipt journal. Snapshot reconciliation never creates sound edges.</summary>
public sealed class WorldUniqueDropPresentation
{
    private readonly HashSet<long> observed = new HashSet<long>();
    public bool TryConsume(int uniqueId, bool freshDrop, long dropToken)
    {
        // Quality 4 includes ordinary yellow gear; only authoritative unique identity counts.
        if (uniqueId <= 0 || dropToken <= 0) return false;
        // Observe snapshots too: a subsequent stale creation packet cannot revive old audio.
        if (!observed.Add(dropToken)) return false;
        return freshDrop;
    }
}
}
