using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private bool TryReceiveCombatEntityTailPacket(ushort opcode, byte[] payload)
    {
        ICombatEntityTailReceiveOwner owner = _worldReceiveOwner as ICombatEntityTailReceiveOwner;
        if (owner == null) return false;
        if (opcode == ProtocolOpcodes.EntityMovePath)
        {
            // Original 0x8018 allocates xs, then ys, before the interleaved required reads.
            // No mirror, frame or stage may decide whether the native MovePath call is reached.
            NetClient.Buf reader = new NetClient.Buf(payload);
            int id = reader.I32();
            int speed = reader.I32();
            int count = reader.U8();
            int[] xs = new int[count];
            int[] ys = new int[count];
            for (int index = 0; index < count; index++)
            {
                xs[index] = reader.I32();
                ys[index] = reader.I32();
            }
            owner.ReceiveEntityPath(id, xs, ys, speed);
            // Only normal return creates the optional immutable observation. It cannot replay
            // this action on a later same-id actor or after a previously absent world appears.
            var points = new List<WorldPathPoint>(count);
            for (int index = 0; index < count; index++)
                points.Add(new WorldPathPoint(xs[index], ys[index]));
            PublishRemotePathState(new WorldRemotePathState(id, speed, points,
                nativeMovementHandled: true), CancellationToken.None);
            return true;
        }
        if (opcode == ProtocolOpcodes.CombatState)
        {
            // Original 0x8011: three I32, U8 flags, I32 HP, then the complete OnCombat owner.
            // In particular HP cannot be split from preceding attack/effect/sound failures.
            NetClient.Buf reader = new NetClient.Buf(payload);
            int attackerId = reader.I32();
            int targetId = reader.I32();
            int damage = reader.I32();
            byte flags = (byte)reader.U8();
            int targetHp = reader.I32();
            owner.ReceiveCombatResult(attackerId, targetId, damage, flags, targetHp);
            PublishCombatReceive(new WorldCombatResultState(attackerId, targetId, damage,
                flags, targetHp, nativeCombatHandled: true), CancellationToken.None);
            return true;
        }
        if (opcode == ProtocolOpcodes.EntityOwner)
        {
            PrivateServerWorldStateDecoder.DecodeWorldEntityOwner(payload, out int id, out int ownerId);
            owner.ReceiveEntityOwner(id, ownerId);
            lock (_worldStateSync)
                AppendEntityEventLocked(WorldRemoteEventKind.EntityOwner, id,
                    ownerId: ownerId, nativeRegistryHandled: true);
            return true;
        }
        return false;
    }
}
}
