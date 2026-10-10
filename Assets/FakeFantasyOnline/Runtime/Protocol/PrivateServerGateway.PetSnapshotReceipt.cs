using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private IWorldPetSnapshotReceiveOwner _petSnapshotObservationOwner;
    private bool _petSnapshotObservationDirty;
    private WorldPlayerStatusState _petSnapshotProjectedStatus;

    private WorldPlayerStatusState ReadPlayerStatusState()
    {
        lock (_worldStateSync)
        {
            WorldPlayerStatusState current = Volatile.Read(ref _playerStatusState);
            if (_petSnapshotObservationOwner != null && current != null &&
                (_petSnapshotObservationDirty || !ReferenceEquals(current, _petSnapshotProjectedStatus)))
            {
                // A later 806f receipt or queued observation can change the status reference.
                // Re-read the same native roots instead of replaying an old full pet snapshot.
                current = current.WithPetSnapshot(_petSnapshotObservationOwner.CapturePetSnapshot());
                Volatile.Write(ref _playerStatusState, current);
                _petSnapshotProjectedStatus = current;
                _petSnapshotObservationDirty = false;
            }
            return current;
        }
    }

    private WorldPlayerPetStatusState ReceivePetSnapshot(byte[] payload,
        IWorldPetSnapshotReceiveOwner owner, bool publishTips)
    {
        var reader = new NetClient.Buf(payload);
        lock (_worldStateSync)
        {
            try
            {
                owner.ReceivePetSnapshot(reader, publishTips);
            }
            finally
            {
                // Metadata only. A read/store/tip exception retains all reached native roots
                // and cannot be replaced by an allocating observation in this finally block.
                if (owner.HasPetSnapshotReceipt)
                {
                    _petSnapshotObservationOwner = owner;
                    _petSnapshotObservationDirty = true;
                    Interlocked.Increment(ref _playerStatusStateVersion);
                }
            }
            return owner.CapturePetSnapshot();
        }
    }
}
}
