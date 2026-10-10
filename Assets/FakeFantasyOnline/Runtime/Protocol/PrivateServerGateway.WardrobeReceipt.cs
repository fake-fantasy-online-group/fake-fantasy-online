using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IWorldWardrobeObservationSource
{
    public IWorldWardrobeObservation WardrobeObservation =>
        Volatile.Read(ref _wardrobeState);

    private bool TryReceiveWardrobePacket(ushort opcode, byte[] payload, IWardrobeReceiveOwner owner)
    {
        if (opcode != ProtocolOpcodes.WardrobeSnapshot || owner == null) return false;
        var reader = new NetClient.Buf(payload);
        IWorldWardrobeObservation state = owner is IWardrobeObservationReceiveOwner nativeOwner
            ? nativeOwner.ReceiveWardrobeObservation(reader)
            : owner.ReceiveWardrobe(reader);
        // Both Pump routes reach this on the host thread before stage queues or polling.
        // Native read/icon/publication failures escape unchanged; only a completed receipt
        // produces this observation. The bound owner returns its existing list, so publication
        // does not allocate a DTO; custom DTO receivers keep their original path and failures.
        PublishWardrobeState(state, CancellationToken.None);
        return true;
    }
}
}
