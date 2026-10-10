using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

public sealed partial class PrivateServerGateway
{
    private ISkillCastReceiveOwner SkillCastReceiverAtReceipt(IWorldReceiveContext context)
    {
        if (context is ISkillCastReceiveOwner contextual) return contextual;
        if (_worldReceiveOwner is ISkillCastReceiveOwnerProvider provider)
            return provider.SkillCastOwnerAtReceipt;
        return _worldReceiveOwner as ISkillCastReceiveOwner;
    }

    private IWorldReceiveSceneContext SceneReceiverAtReceipt(IWorldReceiveContext context) =>
        (IWorldReceiveSceneContext)SkillCastReceiverAtReceipt(context) ??
        context as IWorldReceiveSceneContext;

    private bool TryReceiveSkillCastPacket(ushort opcode, byte[] payload, ISkillCastReceiveOwner owner)
    {
        if (opcode != ProtocolOpcodes.SkillCastState) return false;

        // Required reads fail here before a callback or retained envelope. Native optional
        // tails still use their existing Remaining gates on this one Buf.
        PrivateServerWorldStateDecoder.DecodeSkillCastAction(payload, out WorldSkillCastState state);
        if (_worldReceiveOwner is IRemotePlayerTailReceiveOwner native)
            native.ReceiveSkillCast(state);
        else if (owner != null)
            owner.TryReceiveSkillCast(state);

        // Every production cast has already reached its current native owners, even without
        // a built window. A native exception leaves this dispatch before publication. Copies
        // retain the same one-shot provenance and never become frame-driven native work.
        DispatchPacket(new ProtocolPacket(opcode, payload, receivedSkillCast: state));
        return true;
    }
}

}
