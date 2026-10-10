using System;

namespace FakeFantasyOnline.Application
{
public sealed partial class ClientSessionCoordinator
{
    // Host adapter result for LoginAsync only. Native ConnectAsync did not call its delegate;
    // this is neither connection failure nor success and must not clear the title login latch.
    public const string NativeConnectCallbackDroppedCode = "connection.callback.dropped";

    public void ConnectForAccount(Action<bool> onDone) => _gateway.ConnectForAccount(onDone);

    public void BindAccountResults(Action<int, string> register, Action<int, string> reset) =>
        _gateway.BindAccountResults(register, reset);

    // The native title-state store neither closes the socket nor clears the roster. The
    // persistent product view also serves the world, where the old title page is not drawn.
    public void ShowLoginForAccountResult()
    {
        if (_state != ClientSessionState.InWorld)
            TransitionTo(ClientSessionState.Offline, string.Empty);
    }

    public void RegisterAccount(string account, string password, string securityCode, string inviteCode) =>
        _gateway.RegisterAccount(account, password, securityCode, inviteCode);
}
}
