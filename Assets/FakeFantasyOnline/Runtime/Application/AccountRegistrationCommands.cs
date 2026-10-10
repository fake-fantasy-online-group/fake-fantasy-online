using System;

namespace FakeFantasyOnline.Application
{
// Host-thread boundary for the existing NetClient, not a second transport or request queue.
// Connection completion can be inline, delivered by Pump, or silently dropped by the native
// connecting latch. Replies are owner callbacks, independent of the number of submissions.
public interface IAccountRegistrationCommands
{
    void ConnectForAccount(Action<bool> onDone);
    void BindAccountResults(Action<int, string> register, Action<int, string> reset);
    void RegisterAccount(string account, string password, string securityCode, string inviteCode);
}
}
