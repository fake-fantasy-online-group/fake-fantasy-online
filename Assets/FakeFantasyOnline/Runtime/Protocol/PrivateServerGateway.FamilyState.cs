using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IFamilyStateSource
{
    private WorldFamilyState _familyState = WorldFamilyState.Empty;
    private long _familyStateVersion;
    private WorldFamilyBrowseState _familyBrowseState = WorldFamilyBrowseState.Empty;
    private long _familyBrowseStateVersion;

    public WorldFamilyState FamilyState => Volatile.Read(ref _familyState);
    public long FamilyStateVersion => Interlocked.Read(ref _familyStateVersion);
    public WorldFamilyBrowseState FamilyBrowseState => Volatile.Read(ref _familyBrowseState);
    public long FamilyBrowseStateVersion => Interlocked.Read(ref _familyBrowseStateVersion);

    private WorldFamilyPositionsState _familyPositionsState = WorldFamilyPositionsState.Empty;
    private long _familyPositionsStateVersion;

    public WorldFamilyPositionsState FamilyPositionsState => Volatile.Read(ref _familyPositionsState);
    public long FamilyPositionsStateVersion => Interlocked.Read(ref _familyPositionsStateVersion);

    // Successful 0x8034 contract, including independent optional U16 tails. The raw flag is
    // receipt-local; observations contain no pending action. Exact failed-membership stores
    // and native member-list allocation/clear timing are outside the frozen admission.
    private static WorldFamilyState ReadFamilyState(byte[] payload, out byte openFlag)
    {
        var reader = new NetClient.Buf(payload);
        string name = reader.Str();
        int level = reader.U8();
        int capacity = reader.U8();
        string proclaim = reader.Str();
        bool resist = reader.U8() != 0;
        int position = reader.U8();
        bool canInvite = reader.U8() != 0;
        bool canMail = reader.U8() != 0;
        openFlag = (byte)reader.U8();
        int count = reader.U8();
        var members = new List<(string, int, string, string, bool)>(count);
        for (int i = 0; i < count; i++) members.Add(ReadFamilyMember(reader));
        if (reader.Remaining >= 2) capacity = reader.U16();
        if (reader.Remaining >= 2)
        {
            int extraCount = reader.U16();
            for (int i = 0; i < extraCount && reader.Remaining > 0; i++)
                members.Add(ReadFamilyMember(reader));
        }
        return new WorldFamilyState(name, level, capacity, proclaim, resist, position,
            canInvite, canMail, 0, members);
    }

    private static (string, int, string, string, bool) ReadFamilyMember(NetClient.Buf reader) =>
        (reader.Str(), reader.I32(), reader.Str(), reader.Str(), reader.U8() != 0);

    // Observations follow reached native effects, without a cancellation/reset writeback.
    private void PublishFamilyState(WorldFamilyState state)
    {
        lock (_worldStateSync)
        {
            Volatile.Write(ref _familyState, state);
            Interlocked.Increment(ref _familyStateVersion);
        }
    }

    private void PublishFamilyBrowseState(List<(string, int, int, int, string)> rows)
    {
        lock (_worldStateSync)
        {
            Volatile.Write(ref _familyBrowseState, new WorldFamilyBrowseState(rows));
            Interlocked.Increment(ref _familyBrowseStateVersion);
        }
    }

    private void PublishFamilyPositionsState(List<(int, string, string)> rows)
    {
        lock (_worldStateSync)
        {
            Volatile.Write(ref _familyPositionsState, new WorldFamilyPositionsState(rows));
            Interlocked.Increment(ref _familyPositionsStateVersion);
        }
    }
}
}
