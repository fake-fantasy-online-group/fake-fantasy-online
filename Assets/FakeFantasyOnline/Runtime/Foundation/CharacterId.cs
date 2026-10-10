using System;

namespace FakeFantasyOnline.Foundation
{

public readonly struct CharacterId : IEquatable<CharacterId>
{
    public CharacterId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Character id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public bool Equals(CharacterId other)
    {
        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return obj is CharacterId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
    }

    public override string ToString()
    {
        return Value ?? string.Empty;
    }
}
}
