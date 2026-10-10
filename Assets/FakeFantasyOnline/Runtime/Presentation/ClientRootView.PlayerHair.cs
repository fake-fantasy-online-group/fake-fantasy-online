using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IPlayerHairReceiveContext
{
    int IPlayerHairReceiveContext.SelfEntityId => NetClient.SelfEntityId;

    void IPlayerHairReceiveContext.ReceiveLocalPlayerHair(int head, int hair)
    {
        CharData.SetHair(head, hair);
        if (_world != null) _world = _world.WithHair(head, hair);
        if (_gameBootstrap != null) _gameBootstrap.ProjectPlayerHairForIndependentRuntime(head, hair);
        if (_firstWorldWindow != null) _firstWorldWindow.ProjectPlayerHairForIndependentRuntime(head, hair);
    }
}
}
