namespace FakeFantasyOnline.Application
{
// The existing Buf instance crosses this read-only assembly seam without a second reader,
// copy or cursor. The native mutable owner controls the exact read/store interleaving.
public interface IWorldPetSnapshotReader
{
    int I32();
    int U8();
    string Str();
    int Remaining { get; }
    PetBrief ReadPetBrief();
}

public interface IWorldPetSnapshotReceiveOwner
{
    bool HasPetSnapshotReceipt { get; }
    void ReceivePetSnapshot(IWorldPetSnapshotReader reader, bool publishTips);
    WorldPlayerPetStatusState CapturePetSnapshot();
}
}
