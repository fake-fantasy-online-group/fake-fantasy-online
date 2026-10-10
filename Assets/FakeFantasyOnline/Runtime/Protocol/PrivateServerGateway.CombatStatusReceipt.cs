using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private bool TryReceiveCombatStatusPacket(ushort opcode, byte[] payload)
    {
        var owner = _worldReceiveOwner as ICombatStatusReceiveOwner;
        if (owner == null) return false;
        if (opcode == ProtocolOpcodes.EntityStatusEffect)
        {
            WorldEntityStatusEffect effect = owner.ReceiveEntityStatusEffect(
                new NetClient.Buf(payload), out int entityId);
            PublishWorldEntityStatusEffect(entityId, effect, CancellationToken.None,
                owner.SelfEntityId, nativeRegistryHandled: true);
            return true;
        }
        if (opcode == ProtocolOpcodes.EntityStatusIcons)
        {
            IReadOnlyList<WorldEntityStatusIcon> icons = owner.ReceiveEntityStatusIcons(
                new NetClient.Buf(payload), out int entityId);
            PublishWorldEntityStatusIcons(entityId, icons, CancellationToken.None,
                nativeRegistryHandled: true);
            return true;
        }
        return false;
    }
}
}
