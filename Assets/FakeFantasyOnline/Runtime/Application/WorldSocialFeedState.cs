using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
/// <summary>
/// NetClient.Dispatch 0x8037 friend list. The receive reader orders online friends first,
/// then names ordinally; this snapshot preserves the supplied row order for the UI.
/// </summary>
public sealed class WorldFriendState
{
    public static readonly WorldFriendState Empty = new WorldFriendState(
        Array.Empty<(string, bool)>());

    public WorldFriendState(IReadOnlyList<(string, bool)> entries)
    {
        Entries = new List<(string, bool)>(entries).AsReadOnly();
    }

    public IReadOnlyList<(string, bool)> Entries { get; }
}

/// <summary>
/// NetClient.Dispatch 0x805a apprentice snapshot: master, peers (same master) and apprentices.
/// Maps onto the official SocialClient MasterName/MasterLevel/MasterOnline, Peers and Apprentices.
/// </summary>
public sealed class WorldApprenticeState
{
    public static readonly WorldApprenticeState Empty = new WorldApprenticeState(
        string.Empty, 0, false,
        Array.Empty<(string, int, bool)>(), Array.Empty<(string, int, bool)>());

    public WorldApprenticeState(string masterName, int masterLevel, bool masterOnline,
        IReadOnlyList<(string, int, bool)> peers,
        IReadOnlyList<(string, int, bool)> apprentices)
    {
        MasterName = masterName;
        MasterLevel = masterLevel;
        MasterOnline = masterOnline;
        Peers = new List<(string, int, bool)>(peers).AsReadOnly();
        Apprentices = new List<(string, int, bool)>(apprentices).AsReadOnly();
    }

    public string MasterName { get; }
    public int MasterLevel { get; }
    public bool MasterOnline { get; }
    public IReadOnlyList<(string, int, bool)> Peers { get; }
    public IReadOnlyList<(string, int, bool)> Apprentices { get; }
}

/// <summary>
/// NetClient.Dispatch 0x8068 block list. Its entry is a triple: the char id, the name and the
/// scope flag (0 = this character only, 1 = every character on that account). The official client
/// delivers it in two passes - all (charId, name) records first, then a second count and one U8
/// scope per record.
/// </summary>
public sealed class WorldBlockState
{
    public static readonly WorldBlockState Empty = new WorldBlockState(
        Array.Empty<(long, string, bool)>());

    public WorldBlockState(IReadOnlyList<(long, string, bool)> entries)
    {
        Entries = new List<(long, string, bool)>(entries).AsReadOnly();
    }

    public IReadOnlyList<(long, string, bool)> Entries { get; }
}

public interface ISocialFeedStateSource
{
    WorldFriendState FriendState { get; }
    long FriendStateVersion { get; }
    WorldApprenticeState ApprenticeState { get; }
    long ApprenticeStateVersion { get; }
    WorldBlockState BlockState { get; }
    long BlockStateVersion { get; }
}
}
