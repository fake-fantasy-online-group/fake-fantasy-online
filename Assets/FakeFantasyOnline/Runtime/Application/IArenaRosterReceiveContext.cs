using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
public interface IArenaRosterReceiveContext
{
    void ReceiveArenaRoster(IReadOnlyList<ArenaRosterRowData> rows,
        Dictionary<int, string> descriptions);
}

// Transient typed receive values; no Unity objects or wire readers cross this boundary.
public sealed class ArenaRosterCardData
{
    public ArenaRosterCardData(int skillId, int level, string name)
    { SkillId = skillId; Level = level; Name = name; }

    public int SkillId { get; }
    public int Level { get; }
    public string Name { get; }
}

public sealed class ArenaRosterRowData
{
    public ArenaRosterRowData(int entityId, string name, bool alive, int deaths,
        IReadOnlyList<ArenaRosterCardData> cards)
    { EntityId = entityId; Name = name; Alive = alive; Deaths = deaths; Cards = cards; }

    public int EntityId { get; }
    public string Name { get; }
    public bool Alive { get; }
    public int Deaths { get; }
    public IReadOnlyList<ArenaRosterCardData> Cards { get; }
}
}
