namespace FakeFantasyOnline.Application
{
public interface IAccountSecurityReceiveContext
{
    void SeedAccountSecurity(bool hasCode, int windowMilliseconds);
    void ReceiveAccountSecurityResult(int kind, int code, string message, int tab, int slot, int itemId);
}
}
