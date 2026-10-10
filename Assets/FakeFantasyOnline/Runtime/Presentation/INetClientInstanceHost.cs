using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation
{
// Composition capability held by the actual Unity owner, never a process-wide registry.
internal interface INetClientInstanceHost
{
    ClientSessionCoordinator Session { get; }
    void AttachView(ClientRootView view);
    void DetachView(ClientRootView view);
    void SendTitleWorldReturn();
}
}
