using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    internal partial class Buf : IWorldPetSnapshotReader
    {
        PetBrief IWorldPetSnapshotReader.ReadPetBrief() => NetClient.ReadPetBrief(this);
    }
}
}
