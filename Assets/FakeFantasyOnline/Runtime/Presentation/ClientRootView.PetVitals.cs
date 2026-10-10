using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IWorldPetVitalsReceiveOwner
{
    WorldPetVitalsState IWorldPetVitalsReceiveOwner.PetVitals =>
        CharData.ReadPetVitalsForIndependentRuntime();
    void IWorldPetVitalsReceiveOwner.ReceivePetVital(WorldPetVital field, int value) =>
        CharData.ReceivePetVitalForIndependentRuntime(field, value);
}
}
