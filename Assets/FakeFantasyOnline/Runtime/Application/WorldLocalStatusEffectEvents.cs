using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.Application
{

/// <summary>Optional connection-owned delivery of local EffectPlayer.SpawnOn requests.</summary>
public interface IWorldLocalStatusEffectSource
{
    WorldLocalStatusEffectWindow CaptureLocalStatusEffects();
    bool IsCurrentLocalStatusEffect(WorldLocalStatusEffectWindow window, long revision);
    bool AcknowledgeLocalStatusEffects(WorldLocalStatusEffectWindow window, long consumedThrough);
}

/// <summary>Every unacknowledged local pulse through one immutable connection capture.</summary>
public sealed class WorldLocalStatusEffectWindow
{
    private readonly Guid sourceId;

    public WorldLocalStatusEffectWindow(Guid sourceId, long connectionGeneration,
        long baseRevision, long lastRevision, IEnumerable<WorldEntityStatusEffect> edges)
    {
        if (baseRevision < 0 || lastRevision < baseRevision)
            throw new ArgumentOutOfRangeException(nameof(baseRevision));
        if (edges == null) throw new ArgumentNullException(nameof(edges));
        List<WorldEntityStatusEffect> copy = new List<WorldEntityStatusEffect>();
        long revision = baseRevision;
        foreach (WorldEntityStatusEffect edge in edges)
        {
            if (edge == null || edge.Revision != checked(revision + 1))
                throw new ArgumentException("Local pulse capture must contain a contiguous prefix.", nameof(edges));
            revision = edge.Revision;
            copy.Add(edge);
        }
        if (revision != lastRevision)
            throw new ArgumentException("Local pulse capture must end at its last revision.", nameof(edges));
        this.sourceId = sourceId;
        ConnectionGeneration = connectionGeneration;
        BaseRevision = baseRevision;
        LastRevision = lastRevision;
        Edges = new ReadOnlyCollection<WorldEntityStatusEffect>(copy);
    }

    public long ConnectionGeneration { get; }
    public long BaseRevision { get; }
    public long LastRevision { get; }
    public IReadOnlyList<WorldEntityStatusEffect> Edges { get; }
    public bool IsFrom(Guid source) => sourceId == source;
}

}
