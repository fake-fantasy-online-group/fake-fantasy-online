using System.Collections.ObjectModel;
using System.Threading;
using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    // Native Dispatch reads I32 entity, U8 head, U8 hair. Zero aliases the local player.
    private void ReceivePlayerHair(byte[] payload, IPlayerHairReceiveContext context,
        IRemotePlayerTailReceiveOwner remoteOwner = null)
    {
        var reader = new NetClient.Buf(payload);
        int id = reader.I32();
        int head = reader.U8();
        int hair = reader.U8();
        if (id == 0 || id == (remoteOwner?.SelfEntityId ?? context.SelfEntityId))
        {
            context.ReceiveLocalPlayerHair(head, hair);
            return;
        }
        remoteOwner?.ReceiveRemoteHair(id, head, hair);
        lock (_worldStateSync)
        {
            // Native UpdatePlayerHair ignores a missing remote identity. Keeping the same
            // incarnation and an ordered edge prevents later equipment/spawn events undoing it.
            if (!_remotePlayers.TryGetValue(id, out var previous)) return;
            WorldRemotePlayerState updated = previous.WithHair(head, hair);
            var players = CopyRemotePlayers(_remotePlayers, 0);
            players[id] = updated;
            Volatile.Write(ref _remotePlayers, new ReadOnlyDictionary<int, WorldRemotePlayerState>(players));
            Interlocked.Increment(ref _remotePlayersVersion);
            AppendRemoteEventLocked(WorldRemoteEventKind.Hair, id, player: updated,
                nativeRegistryHandled: remoteOwner != null);
        }
    }
}
}
