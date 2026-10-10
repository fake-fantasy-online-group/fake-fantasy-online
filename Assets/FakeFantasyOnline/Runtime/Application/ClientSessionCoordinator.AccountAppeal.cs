namespace FakeFantasyOnline.Application
{
public sealed partial class ClientSessionCoordinator
{
    public void TheftAppeal(string account, string when, string reason) =>
        _gateway.TheftAppeal(account, when, reason);
}
}
