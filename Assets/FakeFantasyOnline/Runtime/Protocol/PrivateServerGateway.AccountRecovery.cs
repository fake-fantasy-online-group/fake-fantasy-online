using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IAccountRecoveryCommands
{
    public void ResetPassword(string account, string securityCode, string newPassword) =>
        _netClient.ResetPwd(account, securityCode, newPassword);
}
}
