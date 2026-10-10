namespace FakeFantasyOnline.Application
{
public interface IAccountRecoveryCommands
{
    // The caller reads these values when the native connect callback runs, not at click time.
    void ResetPassword(string account, string securityCode, string newPassword);
}
}
