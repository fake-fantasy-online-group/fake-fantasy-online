using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.Application
{
/// <summary>
/// Full-snapshot owner with an authoritative server clock advanced by a monotonic local clock.
/// Neither repeated snapshots nor a local wall-clock adjustment can restart an effect lifetime.
/// Tombstones last until the original or observed conflicting deadline; removed owners cannot reappear early.
/// </summary>
public sealed class EndgameRiftHazardState
{
    private sealed class Lifetime
    {
        internal EndgameRiftHazardView View;
        internal bool Retired;
        internal long RetiredUntilMS;
    }
    private readonly Dictionary<string, Lifetime> lifetimes = new Dictionary<string, Lifetime>(StringComparer.Ordinal);
    private readonly List<EndgameRiftHazardView> active = new List<EndgameRiftHazardView>();
    public IReadOnlyList<EndgameRiftHazardView> CurrentHazards { get; }
    public bool SnapshotCurrent => Snapshot != null && !suspended;
    public EndgameRiftHazardState() { CurrentHazards = new ReadOnlyCollection<EndgameRiftHazardView>(active); }
    private double serverAnchor, localAnchor, latestLocal, nextExpiry = double.PositiveInfinity;
    private long snapshotTime;
    private bool suspended;
    public EndgameRiftView Snapshot { get; private set; }
    public long Version { get; private set; }
    public long WallVersion { get; private set; }
    public void Reset()
    {
        Snapshot = null; lifetimes.Clear(); active.Clear(); suspended = false;
        serverAnchor = localAnchor = latestLocal = 0; nextExpiry = double.PositiveInfinity; snapshotTime = 0; Version++; WallVersion++;
    }
    public void Suspend()
    {
        suspended = true; active.Clear(); nextExpiry = double.PositiveInfinity; Version++; WallVersion++;
    }
    private double ObserveLocal(double monotonicMS)
    {
        if (double.IsNaN(monotonicMS) || double.IsInfinity(monotonicMS) || monotonicMS < 0)
            throw new ArgumentOutOfRangeException(nameof(monotonicMS));
        latestLocal = Math.Max(latestLocal, monotonicMS);
        return latestLocal;
    }
    public double ServerTimeMS(double monotonicMS) => serverAnchor + Math.Max(0, ObserveLocal(monotonicMS) - localAnchor);
    public bool Apply(EndgameRiftView next, double monotonicMS) => Apply(next, monotonicMS, 0);
    public bool Apply(EndgameRiftView next, double monotonicMS, double snapshotAgeMS)
    {
        if (double.IsNaN(snapshotAgeMS) || double.IsInfinity(snapshotAgeMS) || snapshotAgeMS < 0) throw new ArgumentOutOfRangeException(nameof(snapshotAgeMS));
        double localNow = ObserveLocal(monotonicMS);
        if (next == null || next.ServerNowMS == 0)
        {
            if (Snapshot != null || active.Count != 0) { Snapshot = null; lifetimes.Clear(); active.Clear(); nextExpiry = double.PositiveInfinity; Version++; WallVersion++; }
            return true;
        }
        if (next.ServerNowMS < snapshotTime) return false;
        bool sameRun = Snapshot != null && next.RunID == Snapshot.RunID;
        if (sameRun && (next.FloorIndex < Snapshot.FloorIndex || EndgameClientState.CompareRevision(next.Revision, Snapshot.Revision) < 0)) return false;
        bool sameScope = sameRun && next.MapID == Snapshot.MapID && next.FloorIndex == Snapshot.FloorIndex;
        if (suspended && sameScope && next.ServerNowMS <= snapshotTime) return false;
        // An existing effect ID identifies immutable geometry and a fixed deadline.
        if (sameScope)
            foreach (var hazard in next.Hazards)
                if (lifetimes.TryGetValue(hazard.ID, out Lifetime prior) && !SameEffect(prior.View, hazard))
                {
                    prior.Retired = true;
                    prior.RetiredUntilMS = Math.Max(prior.RetiredUntilMS, Math.Max(prior.View.ExpiresMS, hazard.ExpiresMS));
                } // Isolate a conflicting effect ID; keep progress and unrelated effects.
        // Page assembly must not restart an effect clock after a slow transfer.
        double receivedServerNow = next.ServerNowMS + snapshotAgeMS;
        double serverNow = snapshotTime == 0 ? receivedServerNow : Math.Max(ServerTimeMS(localNow), receivedServerNow);
        serverAnchor = serverNow; localAnchor = localNow; snapshotTime = next.ServerNowMS;
        if (!sameScope) lifetimes.Clear();
        var present = new HashSet<string>(StringComparer.Ordinal);
        foreach (var hazard in next.Hazards) present.Add(hazard.ID);
        var expired = new List<string>();
        foreach (var pair in lifetimes)
        {
            if (Math.Max(pair.Value.View.ExpiresMS, pair.Value.RetiredUntilMS) <= serverNow) expired.Add(pair.Key);
            else if (!present.Contains(pair.Key)) pair.Value.Retired = true;
        }
        foreach (string id in expired) lifetimes.Remove(id);
        foreach (var hazard in next.Hazards)
            if (hazard.ExpiresMS > serverNow && !lifetimes.ContainsKey(hazard.ID))
                lifetimes.Add(hazard.ID, new Lifetime { View = hazard });
        Snapshot = next; suspended = false;
        RebuildActive(serverNow);
        return true;
    }
    private void RebuildActive(double serverNow)
    {
        var next = new List<EndgameRiftHazardView>();
        bool live = !suspended && Snapshot != null && !Snapshot.Transitioning &&
            (Snapshot.Phase == "active" || Snapshot.Phase == "boss" || Snapshot.Phase == "boss_arriving");
        if (live)
            foreach (var hazard in Snapshot.Hazards)
                if (lifetimes.TryGetValue(hazard.ID, out Lifetime lifetime) && !lifetime.Retired && hazard.ExpiresMS > serverNow)
                    next.Add(lifetime.View);
        bool changed = active.Count != next.Count;
        if (!changed) for (int i = 0; i < next.Count; i++) if (!ReferenceEquals(active[i], next[i])) { changed = true; break; }
        if (!changed) return;
        if (!SameWallSet(active, next)) WallVersion++;
        active.Clear(); active.AddRange(next);
        nextExpiry = double.PositiveInfinity;
        foreach (var hazard in active) nextExpiry = Math.Min(nextExpiry, hazard.ExpiresMS);
        Version++;
    }
    private static bool SameWallSet(List<EndgameRiftHazardView> a, List<EndgameRiftHazardView> b)
    {
        int x = 0, y = 0;
        while (true)
        {
            while (x < a.Count && a[x].Walls.Count == 0) x++;
            while (y < b.Count && b[y].Walls.Count == 0) y++;
            if (x == a.Count || y == b.Count) return x == a.Count && y == b.Count;
            if (!ReferenceEquals(a[x++], b[y++])) return false;
        }
    }
    public void Tick(double monotonicMS)
    {
        double now = ServerTimeMS(monotonicMS);
        if (now >= nextExpiry) RebuildActive(now);
    }
    public bool Blocks(int mapID, double fromX, double fromY, double toX, double toY, double monotonicMS)
    {
        Tick(monotonicMS);
        if (Snapshot == null || Snapshot.MapID != mapID) return false;
        foreach (var hazard in active)
            foreach (var wall in hazard.Walls)
                if (EndgameRiftWallCollision.Blocks(wall, fromX, fromY, toX, toY)) return true;
        return false;
    }
    public bool HasWalls(int mapID, double monotonicMS)
    {
        Tick(monotonicMS);
        if (Snapshot == null || Snapshot.MapID != mapID) return false;
        foreach (var hazard in active) if (hazard.Walls.Count != 0) return true;
        return false;
    }
    public int RemainingMS(EndgameRiftHazardView hazard, double monotonicMS)
    {
        if (hazard == null || !lifetimes.TryGetValue(hazard.ID, out Lifetime lifetime) || lifetime.Retired || !SameEffect(lifetime.View, hazard)) return 0;
        return (int)Math.Max(0, Math.Min(60000, Math.Ceiling(hazard.ExpiresMS - ServerTimeMS(monotonicMS))));
    }
    private static bool SameEffect(EndgameRiftHazardView a, EndgameRiftHazardView b)
    {
        if (a.ID != b.ID || a.SourceID != b.SourceID || a.AbilityID != b.AbilityID || a.Mechanism != b.Mechanism ||
            a.X != b.X || a.Y != b.Y || a.Radius != b.Radius || a.Width != b.Width || a.BornMS != b.BornMS || a.ExpiresMS != b.ExpiresMS ||
            a.FirstPulseMS != b.FirstPulseMS || a.PulseMS != b.PulseMS || a.PropagationMS != b.PropagationMS || a.Walls.Count != b.Walls.Count) return false;
        for (int i = 0; i < a.Walls.Count; i++)
        {
            var x = a.Walls[i]; var y = b.Walls[i];
            if (x.ID != y.ID || x.X != y.X || x.Y != y.Y || x.X2 != y.X2 || x.Y2 != y.Y2 || x.Width != y.Width) return false;
        }
        return true;
    }
}
}
