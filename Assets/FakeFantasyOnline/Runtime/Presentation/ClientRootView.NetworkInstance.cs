using System;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView
{
    private INetClientInstanceHost _networkInstanceHost;

    internal void BindNetworkInstanceForIndependentRuntime(
        INetClientInstanceHost host, Action projectState, Func<bool> startupAddressResult)
    {
        _networkInstanceHost = host ?? throw new ArgumentNullException(nameof(host));
        _session = host.Session;
        BindNativeStateProjectionForIndependentRuntime(projectState);
        BindStartupAddressResultForIndependentRuntime(startupAddressResult);
        _gameBootstrap.BindNativeWorldHostForIndependentRuntime(
            _session, transform, _combatSound, _processNpcChatterNames);
    }
}
}
