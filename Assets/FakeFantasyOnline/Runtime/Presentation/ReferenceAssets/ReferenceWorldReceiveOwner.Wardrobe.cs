using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : IWardrobeObservationReceiveOwner
{
    public WorldWardrobeState ReceiveWardrobe(IWardrobeReader reader) =>
        ReceiveWardrobeObservation(reader).State;

    public IWorldWardrobeObservation ReceiveWardrobeObservation(IWardrobeReader reader) =>
        NetClient.ReceiveWardrobeForIndependentRuntime(reader);
}
}
