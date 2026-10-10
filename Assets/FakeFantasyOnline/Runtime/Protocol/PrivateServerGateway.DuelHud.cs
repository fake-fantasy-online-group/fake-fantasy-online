using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private static WorldDuelHudState ReadDuelHudState(byte[] payload)
    {
        var reader = new NetClient.Buf(payload);
        bool active = reader.U8() != 0;
        byte phase = (byte)reader.U8(), role = (byte)reader.U8();
        int countdown = reader.I32(), centerX = reader.I32(), centerY = reader.I32();
        int radius = reader.I32(), gap = reader.I32();
        string a = reader.Str(), b = reader.Str(), winner = reader.Str(), reason = reader.Str(), loot = reader.Str();
        int mode = 0, entry = 0;
        if (reader.Remaining >= 5) { mode = reader.U8(); entry = reader.I32(); }
        return new WorldDuelHudState(active, phase, role, countdown, centerX, centerY, radius, gap,
            a, b, winner, reason, loot, mode, entry);
    }
    private void ReceiveDuelHud(byte[] payload, IDuelHudReceiveOwner context = null)
    {
        WorldDuelHudState state = ReadDuelHudState(payload);
        IDuelHudReceiveOwner owner = _worldReceiveOwner as IDuelHudReceiveOwner ?? context;
        owner?.ReceiveDuelHud(state);
        // Publish only after the native transition returns. An unbound diagnostic gateway
        // retains an observation, never an operation to replay after a later owner binds.
        PublishDuelHudState(state, CancellationToken.None);
    }

    private void PublishDuelHudState(WorldDuelHudState state, CancellationToken cancellationToken)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendRemoteEventLocked(WorldRemoteEventKind.DuelHud, duelHud: state);
        }
    }
}
}
