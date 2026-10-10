using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : IWorldPetSnapshotReceiveOwner
{
    bool IWorldPetSnapshotReceiveOwner.HasPetSnapshotReceipt => CharData.PetSnapshotReceiptOwned;
    void IWorldPetSnapshotReceiveOwner.ReceivePetSnapshot(IWorldPetSnapshotReader reader, bool publishTips) =>
        CharData.ReceivePetSnapshotForIndependentRuntime(reader, publishTips);
    WorldPlayerPetStatusState IWorldPetSnapshotReceiveOwner.CapturePetSnapshot() =>
        CharData.CapturePetSnapshotForIndependentRuntime();
}
}
