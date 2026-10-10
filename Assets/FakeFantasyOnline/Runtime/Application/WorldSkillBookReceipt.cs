using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.Application
{
/// <summary>
/// Reached character-skill stores from one receive. A null receipt means points were not read;
/// null Skills means count was not read; null DamageBonuses means the required rows did not
/// finish. Empty collections mean a real replacement, not an absent store. Rows retain read
/// order and duplicates so the native owner performs each completed dictionary assignment.
/// </summary>
public sealed class WorldSkillBookReceipt
{
    public WorldSkillBookReceipt(int skillPoints, IReadOnlyList<WorldLearnedSkillState> skills,
        IReadOnlyDictionary<int, int> damageBonuses)
    {
        SkillPoints = skillPoints;
        if (skills != null)
        {
            var rows = new List<WorldLearnedSkillState>(skills.Count);
            foreach (WorldLearnedSkillState row in skills)
                rows.Add(row ?? throw new ArgumentException("A reached skill row cannot be null.", nameof(skills)));
            Skills = new ReadOnlyCollection<WorldLearnedSkillState>(rows);
        }
        if (damageBonuses != null)
        {
            var bonuses = new Dictionary<int, int>();
            foreach (var pair in damageBonuses) bonuses[pair.Key] = pair.Value;
            DamageBonuses = new ReadOnlyDictionary<int, int>(bonuses);
        }
    }

    public int SkillPoints { get; }
    public IReadOnlyList<WorldLearnedSkillState> Skills { get; }
    public IReadOnlyDictionary<int, int> DamageBonuses { get; }

    /// <summary>Data-only projection for the codec and a gateway with no native owner.</summary>
    public WorldSkillBookState ApplyToSnapshot(WorldSkillBookState previous)
    {
        if (previous == null) throw new ArgumentNullException(nameof(previous));
        IReadOnlyList<WorldLearnedSkillState> rows = previous.Skills;
        if (Skills != null)
        {
            var values = new Dictionary<int, int>();
            foreach (WorldLearnedSkillState row in Skills) values[row.Id] = row.Level;
            var uniqueRows = new List<WorldLearnedSkillState>(values.Count);
            foreach (var pair in values) uniqueRows.Add(new WorldLearnedSkillState(pair.Key, pair.Value));
            rows = uniqueRows;
        }
        return new WorldSkillBookState(SkillPoints, rows, DamageBonuses ?? previous.DamageBonuses);
    }
}

/// <summary>
/// Host-bound skill-root owner. Apply only reached stores, in their read order, and return a
/// copied snapshot of the actual resulting roots. This operation never evaluates newbie tips.
/// One gateway binds one owner for its lifetime; contexts and the composition-root adapter
/// refer to the same character roots. No wire reader or transport lifetime crosses this port.
/// </summary>
public interface ISkillBookReceiveOwner
{
    WorldSkillBookState ReceiveSkillBook(WorldSkillBookReceipt receipt);
}

// The native owner keeps its completed dictionaries until their next replacement. This
// value borrows those roots and captures the scalar; it allocates/copies only when an
// explicit caller asks for the immutable DTO. No mutable dictionary escapes this value.
public readonly struct WorldSkillBookObservation
{
    private readonly WorldSkillBookState state;
    private readonly IReadOnlyDictionary<int, int> skills;
    private readonly IReadOnlyDictionary<int, int> bonuses;
    private readonly int points;

    public WorldSkillBookObservation(WorldSkillBookState state)
    {
        this.state = state;
        skills = null;
        bonuses = null;
        points = 0;
        HasValue = state != null;
    }

    private WorldSkillBookObservation(int points, IReadOnlyDictionary<int, int> skills,
        IReadOnlyDictionary<int, int> bonuses)
    {
        state = null;
        this.skills = skills;
        this.bonuses = bonuses;
        this.points = points;
        HasValue = true;
    }

    // Ownership transfer: these roots must not be mutated after the receipt returns.
    public static WorldSkillBookObservation FromNativeRoots(int points,
        IReadOnlyDictionary<int, int> skills, IReadOnlyDictionary<int, int> bonuses) =>
        new WorldSkillBookObservation(points, skills, bonuses);

    public bool HasValue { get; }
    public int SkillCount => state != null ? state.Skills.Count : skills.Count;
    public WorldSkillBookState MaterializedState => state;

    public WorldSkillBookState ToSnapshot()
    {
        if (state != null || !HasValue) return state;
        var rows = new List<WorldLearnedSkillState>(skills.Count);
        foreach (var pair in skills) rows.Add(new WorldLearnedSkillState(pair.Key, pair.Value));
        return new WorldSkillBookState(points, rows, bonuses);
    }
}

public interface ISkillBookReader
{
    int I32();
    int U16();
    int Remaining { get; }
}

public interface ISkillBookStreamReceiveOwner : ISkillBookReceiveOwner
{
    void ReceiveSkillBook(ISkillBookReader reader, out WorldSkillBookObservation observation);
    void ReceiveNewbieSkillBookTip(WorldSkillBookObservation observation);
}

public interface INewbieSkillBookObservationContext
{
    void ReceiveNewbieSkillBook(WorldSkillBookObservation observation);
}

public interface IWorldSkillBookObservationSource
{
    WorldSkillBookObservation SkillBookObservation { get; }
}

}
