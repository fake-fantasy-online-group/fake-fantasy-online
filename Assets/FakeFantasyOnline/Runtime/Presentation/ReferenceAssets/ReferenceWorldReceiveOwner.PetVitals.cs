using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : IWorldPetVitalsReceiveOwner
{
    public WorldPetVitalsState PetVitals => CharData.ReadPetVitalsForIndependentRuntime();
    public void ReceivePetVital(WorldPetVital field, int value) =>
        CharData.ReceivePetVitalForIndependentRuntime(field, value);
}
}
