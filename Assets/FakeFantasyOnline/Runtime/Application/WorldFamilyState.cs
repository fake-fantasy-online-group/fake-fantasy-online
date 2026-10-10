using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
/// <summary>The server-authored family membership and permission snapshot.</summary>
public sealed class WorldFamilyState
{
    public static readonly WorldFamilyState Empty = new WorldFamilyState(
        string.Empty, 0, 0, string.Empty, false, 0, false, false, 0,
        Array.Empty<(string, int, string, string, bool)>());

    public WorldFamilyState(string name, int level, int memberCapacity, string proclaim,
        bool resist, int myPosition, bool canInvite, bool canMail, byte openFlag,
        IReadOnlyList<(string, int, string, string, bool)> members)
    {
        Name = name;
        Level = level;
        MemberCapacity = memberCapacity;
        Proclaim = proclaim;
        Resist = resist;
        MyPosition = myPosition;
        CanInvite = canInvite;
        CanMail = canMail;
        OpenFlag = openFlag;
        Members = new List<(string, int, string, string, bool)>(members).AsReadOnly();
    }

    public string Name { get; }
    public int Level { get; }
    public int MemberCapacity { get; }
    public string Proclaim { get; }
    public bool Resist { get; }
    public int MyPosition { get; }
    public bool CanInvite { get; }
    public bool CanMail { get; }
    // Retain the byte: the native UI-open branch compares exactly with 1, not nonzero.
    public byte OpenFlag { get; }
    public IReadOnlyList<(string, int, string, string, bool)> Members { get; }
}

/// <summary>Server replacement for the browsable family directory.</summary>
public sealed class WorldFamilyBrowseState
{
    public static readonly WorldFamilyBrowseState Empty = new WorldFamilyBrowseState(
        Array.Empty<(string, int, int, int, string)>());

    public WorldFamilyBrowseState(IReadOnlyList<(string, int, int, int, string)> entries)
    {
        Entries = new List<(string, int, int, int, string)>(entries).AsReadOnly();
    }

    public IReadOnlyList<(string, int, int, int, string)> Entries { get; }
}

/// <summary>Server replacement for the assignable family positions.</summary>
public sealed class WorldFamilyPositionsState
{
    public static readonly WorldFamilyPositionsState Empty = new WorldFamilyPositionsState(
        Array.Empty<(int Id, string Name, string Note)>());

    public WorldFamilyPositionsState(IReadOnlyList<(int Id, string Name, string Note)> entries)
    {
        Entries = new List<(int Id, string Name, string Note)>(entries).AsReadOnly();
    }

    public IReadOnlyList<(int Id, string Name, string Note)> Entries { get; }
}

public interface IFamilyStateSource
{
    WorldFamilyState FamilyState { get; }
    long FamilyStateVersion { get; }
    WorldFamilyBrowseState FamilyBrowseState { get; }
    long FamilyBrowseStateVersion { get; }
    WorldFamilyPositionsState FamilyPositionsState { get; }
    long FamilyPositionsStateVersion { get; }
}
}
