using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.PluginApi
{

public sealed class PositionSnapshot
{
    public PositionSnapshot(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public float X { get; }

    public float Y { get; }

    public float Z { get; }
}

public sealed class PlayerSnapshot
{
    public PlayerSnapshot(
        string characterId,
        string displayName,
        int level,
        string mapId,
        PositionSnapshot position)
    {
        CharacterId = characterId ?? throw new ArgumentNullException(nameof(characterId));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Level = level;
        MapId = mapId ?? throw new ArgumentNullException(nameof(mapId));
        Position = position ?? throw new ArgumentNullException(nameof(position));
    }

    public string CharacterId { get; }

    public string DisplayName { get; }

    public int Level { get; }

    public string MapId { get; }

    public PositionSnapshot Position { get; }
}

public sealed class CharacterRosterSnapshot
{
    public CharacterRosterSnapshot(IReadOnlyList<PlayerSnapshot> characters)
    {
        if (characters == null)
        {
            throw new ArgumentNullException(nameof(characters));
        }

        List<PlayerSnapshot> copy = new List<PlayerSnapshot>(characters);
        Characters = new ReadOnlyCollection<PlayerSnapshot>(copy);
    }

    public IReadOnlyList<PlayerSnapshot> Characters { get; }
}

public interface IPluginStateQueries
{
    bool TryGetCurrentPlayer(out PlayerSnapshot snapshot);

    CharacterRosterSnapshot GetCharacterRoster();
}
}
