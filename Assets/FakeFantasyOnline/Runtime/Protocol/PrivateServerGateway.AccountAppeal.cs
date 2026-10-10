using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IAccountAppealCommands
{
    // Native send only: no task, send semaphore, stage gate or invented successful verdict.
    // The private server's unimplemented appeal operation remains a server-side limitation.
    public void TheftAppeal(string account, string when, string reason) =>
        _netClient.TheftAppeal(account, when, reason);
}
}
