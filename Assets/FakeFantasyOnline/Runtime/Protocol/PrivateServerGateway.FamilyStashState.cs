using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IFamilyStashStateSource
{
    private WorldFamilyStashState _familyStashState = WorldFamilyStashState.Empty;
    private long _familyStashStateVersion;
    public WorldFamilyStashState FamilyStashState => Volatile.Read(ref _familyStashState);
    public long FamilyStashStateVersion => Interlocked.Read(ref _familyStashStateVersion);

    private static WorldFamilyStashState ReadFamilyStashState(byte[] payload)
    {
        var reader = new NetClient.Buf(payload);
        int capacity = reader.I32();
        int takePosition = reader.U8();
        bool canTake = reader.U8() != 0;
        bool leader = reader.U8() != 0;
        int count = reader.U16();
        var items = new List<WorldFamilyStashItem>(count);
        for (int i = 0; i < count; i++)
            items.Add(new WorldFamilyStashItem(reader.I32(), reader.I32(), reader.Str(), reader.Str(),
                reader.I32(), reader.I32(), reader.I32(), reader.Str()));
        return new WorldFamilyStashState(capacity, takePosition, canTake, leader, items);
    }

    private void PublishFamilyStashState(WorldFamilyStashState state, CancellationToken cancellationToken)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _familyStashState, state);
            Interlocked.Increment(ref _familyStashStateVersion);
        }
    }
}
}
