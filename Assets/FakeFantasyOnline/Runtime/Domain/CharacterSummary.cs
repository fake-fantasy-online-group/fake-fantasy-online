using System;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Domain
{

public sealed class CharacterSummary
{
    public CharacterSummary(CharacterId id, string displayName, int level)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
        }

        if (level < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        Id = id;
        DisplayName = displayName;
        Level = level;
    }

    public CharacterId Id { get; }

    public string DisplayName { get; }

    public int Level { get; }
}
}
