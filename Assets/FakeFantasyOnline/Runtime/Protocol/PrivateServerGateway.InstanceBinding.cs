using System;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : INetClientInstanceBinding
{
    // Separate stores preserve SetupNet's delegate-allocation/store order.
    public void BindRegisterResultReceipt(Action<int, string> receive) =>
        _netClient.OnRegisterResult = receive;

    public void BindResetResultReceipt(Action<int, string> receive) =>
        _netClient.OnResetResult = receive;
}
}
