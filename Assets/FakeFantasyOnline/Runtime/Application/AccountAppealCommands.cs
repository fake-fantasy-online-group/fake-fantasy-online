namespace FakeFantasyOnline.Application
{
public interface IAccountAppealCommands
{
    // Invocation-local trimmed strings captured before ConnectAsync. No dedicated appeal
    // result is registered by the native client; generic notices/errors remain independent.
    void TheftAppeal(string account, string when, string reason);
}
}
