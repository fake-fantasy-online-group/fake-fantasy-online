using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : ISocialFeedStateSource
{
    private WorldFriendState _friendState = WorldFriendState.Empty;
    private long _friendStateVersion;
    private WorldApprenticeState _apprenticeState = WorldApprenticeState.Empty;
    private long _apprenticeStateVersion;
    private WorldBlockState _blockState = WorldBlockState.Empty;
    private long _blockStateVersion;

    public WorldFriendState FriendState => Volatile.Read(ref _friendState);
    public long FriendStateVersion => Interlocked.Read(ref _friendStateVersion);
    public WorldApprenticeState ApprenticeState => Volatile.Read(ref _apprenticeState);
    public long ApprenticeStateVersion => Interlocked.Read(ref _apprenticeStateVersion);
    public WorldBlockState BlockState => Volatile.Read(ref _blockState);
    public long BlockStateVersion => Interlocked.Read(ref _blockStateVersion);

    // NetClient.Dispatch 0x8037: U8 count, then (Str name, U8 online) per friend.
    private static WorldFriendState ReadFriendState(byte[] payload)
    {
        var reader = new NetClient.Buf(payload);
        int count = reader.U8();
        var rows = new List<(string, bool)>(count);
        for (int i = 0; i < count && reader.Remaining > 0; i++)
            rows.Add((reader.Str(), reader.U8() != 0));
        // Official 060007b5: online first, then the unmodified ordinal name comparison.
        rows.Sort((a, b) => a.Item2 == b.Item2
            ? string.CompareOrdinal(a.Item1, b.Item1)
            : (a.Item2 ? -1 : 1));
        return new WorldFriendState(rows);
    }

    private void PublishFriendState(WorldFriendState state, CancellationToken cancellationToken)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _friendState, state);
            Interlocked.Increment(ref _friendStateVersion);
        }
    }

    // NetClient.Dispatch 0x805a: master (Str, I32, U8), U8 peer count of (Str, I32, U8),
    // then U8 apprentice count of (Str, I32, U8).
    private static WorldApprenticeState ReadApprenticeState(byte[] payload)
    {
        var reader = new NetClient.Buf(payload);
        string masterName = reader.Str();
        int masterLevel = reader.I32();
        bool masterOnline = reader.U8() != 0;
        var peers = new List<(string, int, bool)>();
        int peerCount = reader.U8();
        for (int i = 0; i < peerCount && reader.Remaining > 0; i++)
            peers.Add((reader.Str(), reader.I32(), reader.U8() != 0));
        var apprentices = new List<(string, int, bool)>();
        int apprenticeCount = reader.U8();
        for (int i = 0; i < apprenticeCount && reader.Remaining > 0; i++)
            apprentices.Add((reader.Str(), reader.I32(), reader.U8() != 0));
        return new WorldApprenticeState(masterName, masterLevel, masterOnline, peers, apprentices);
    }

    private void PublishApprenticeState(WorldApprenticeState state, CancellationToken cancellationToken)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _apprenticeState, state);
            Interlocked.Increment(ref _apprenticeStateVersion);
        }
    }

    // NetClient.Dispatch 0x8068, two passes: U16 count then (I64 charId, Str name) records,
    // then U16 scopeCount and one U8 scope per record. Both tails are guarded by the reader's
    // remaining length, which is how the official client treats them as optional.
    private static WorldBlockState ReadBlockState(byte[] payload)
    {
        var reader = new NetClient.Buf(payload);
        int count = reader.U16();
        var entries = new List<(long, string, bool)>(count);
        for (int i = 0; i < count && reader.Remaining > 0; i++)
            entries.Add((reader.I64(), reader.Str(), false));
        if (reader.Remaining >= 2)
        {
            int scopeCount = reader.U16();
            for (int i = 0; i < scopeCount && i < entries.Count && reader.Remaining > 0; i++)
            {
                (long charId, string name, bool _) = entries[i];
                entries[i] = (charId, name, reader.U8() != 0);
            }
        }
        return new WorldBlockState(entries);
    }

    private void PublishBlockState(WorldBlockState state, CancellationToken cancellationToken)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _blockState, state);
            Interlocked.Increment(ref _blockStateVersion);
        }
    }
}
}
