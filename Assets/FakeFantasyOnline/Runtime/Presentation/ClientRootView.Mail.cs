using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation
{

public sealed partial class ClientRootView : IMailReceiveOwner
{
    bool IMailReceiveOwner.TryReceiveMailList(WorldMailState state)
    {
        if (_gameBootstrap == null || _session == null) return false;
        _gameBootstrap.AcquireMailForIndependentRuntime(_session).ReceiveList(state);
        return true;
    }

    bool IMailReceiveOwner.TryReceiveMailNew(bool hasNew)
    {
        if (_gameBootstrap == null || _session == null) return false;
        _gameBootstrap.AcquireMailForIndependentRuntime(_session).ReceiveNew(hasNew);
        return true;
    }
}

}
