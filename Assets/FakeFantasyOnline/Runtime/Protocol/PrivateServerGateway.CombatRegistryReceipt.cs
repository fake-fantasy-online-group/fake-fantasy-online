using System.Collections.ObjectModel;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private bool TryReceiveCombatRegistryPacket(ushort opcode, byte[] payload,
        IWorldReceiveContext context = null)
    {
        if (TryReceiveCombatStatusPacket(opcode, payload)) return true;
        ICombatRegistryReceiveOwner owner = _worldReceiveOwner as ICombatRegistryReceiveOwner;
        if (owner == null) return false; // Legacy detached diagnostic inputs retain their route.
        IWorldReceiveContext live = context ?? owner;
        if (opcode == ProtocolOpcodes.RemotePlayer)
        {
            PrivateServerWorldStateDecoder.DecodeRemotePlayerState(payload, live,
                out WorldRemotePlayerState state, out _);
            if (state == null) return true;
            object identity = owner.ReceiveRemotePlayer(state);
            PublishRemotePlayerState(state, CancellationToken.None, true, identity);
            return true;
        }
        if (opcode == ProtocolOpcodes.EntitySpawn)
        {
            // The native receiver retains the original decoded value list. Compatibility
            // rows are projected only after Spawn and its native status tails return.
            WorldEntityState state = owner.ReceiveWorldEntity(new NetClient.Buf(payload),
                live, out object identity);
            if (state == null) return true;
            PublishWorldEntityState(state, CancellationToken.None,
                admittedOnMainThread: true, nativeRegistryHandled: true, receivedIdentity: identity);
            return true;
        }
        if (opcode == ProtocolOpcodes.EntityRemove)
        {
            PrivateServerWorldStateDecoder.DecodeWorldEntityRemoval(payload, out int id, out byte reason);
            owner.ReceiveWorldRemoval(id, reason, out bool trap, out bool player, out bool monster);
            PublishRegistryRemovalObservation(id, reason, trap, player, monster);
            return true;
        }
        if (opcode == ProtocolOpcodes.RemotePkState)
        {
            PrivateServerWorldStateDecoder.DecodeRemotePkState(payload, out int id, out byte state);
            if (!owner.ReceiveRemotePk(id, state))
                PublishRemotePlayerPkState(id, state, CancellationToken.None, true);
            return true;
        }
        if (opcode == ProtocolOpcodes.PlayerLifeState)
        {
            PrivateServerWorldStateDecoder.DecodePlayerLifeState(payload, out int id,
                out bool alive, out int flags);
            PlayerLifeReceiveResult received;
            if (context is IPlayerLifeReceiveContext life)
            {
                received = life.ReceivePlayerLife(id, alive, (byte)flags);
                if (!received.Local) owner.ReceiveRemotePlayerLife(id, alive);
            }
            else received = owner.ReceivePlayerLife(id, alive, (byte)flags);
            if (received.Local)
            {
                if (received.Applied) RecordAppliedPlayerLife(received.EntityId, alive, (byte)flags);
            }
            else PublishPlayerLifeState(id, alive, flags, CancellationToken.None, true, true);
            return true;
        }
        return false;
    }

    private void PublishRegistryRemovalObservation(int id, byte reason,
        bool trapPresent, bool playerPresent, bool monsterPresent)
    {
        lock (_worldStateSync)
        {
            // Presence comes from the real dictionaries AFTER the original Remove returns.
            // A stale/missing mirror cannot choose which native registry receives the action.
            if (!trapPresent && _worldTraps.TryGetValue(id, out WorldEntityState trap))
            {
                AppendEntityEventLocked(WorldRemoteEventKind.EntityRemove, id, entity: trap,
                    reason: reason, nativeRegistryHandled: true);
                _worldTraps.Remove(id);
                _trapIncarnations.Remove(id);
            }
            if (!playerPresent) RemoveRemotePlayerLocked(id, reason, true);
            if (!monsterPresent && _worldEntities.TryGetValue(id, out WorldEntityState monster))
            {
                AppendEntityEventLocked(WorldRemoteEventKind.EntityRemove, id, entity: monster,
                    reason: reason, nativeRegistryHandled: true);
                var entities = CopyWorldEntities(_worldEntities, 0);
                entities.Remove(id);
                _worldEntities = new ReadOnlyDictionary<int, WorldEntityState>(entities);
                Interlocked.Increment(ref _worldEntitiesVersion);
                _entityIncarnations.Remove(id);
                ClearMonsterPathLocked(id);
            }
            WorldEntityPublicationState prior = _worldEntityPublicationState;
            PublishWorldEntitySceneSnapshotLocked(_worldEntities, _worldEntitiesVersion,
                new WorldEntityPublicationState(prior.SceneGeneration, _currentWorldMapId,
                    prior.DecodedSpawnFrameCount, prior.AdmittedSpawnFrameCount,
                    prior.RejectedMapGuardSpawnFrameCount, prior.ReceivedRemovalFrameCount + 1,
                    _worldEntitiesVersion, _worldEntities.Count));
        }
    }

}
}
