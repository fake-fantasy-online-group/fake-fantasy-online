using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
/// <summary>
/// Synchronous host-thread delivery to the existing private-chat and request data owners.
/// Both callbacks share the packet receipt order; neither requires a built world window.
/// A gateway without this capability exposes compatibility observations only.
/// </summary>
public interface ISocialNoticeReceiveOwner
{
    void ReceivePrivateChat(string sender, string text,
        IReadOnlyList<WorldChatSharedEntryState> shares);
    void ReceiveSystemRequests(List<(int, byte, int, string, string)> requests);
    // Explicit immutable compatibility delivery; the native receipt uses the original list.
    void ReceiveSystemRequests(IReadOnlyList<WorldSystemRequestState> requests);
}
}
