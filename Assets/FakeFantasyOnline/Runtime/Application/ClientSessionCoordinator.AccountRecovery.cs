namespace FakeFantasyOnline.Application
{
public sealed partial class ClientSessionCoordinator
{
    public void ResetPassword(string account, string securityCode, string newPassword) =>
        _gateway.ResetPassword(account, securityCode, newPassword);
}
}
