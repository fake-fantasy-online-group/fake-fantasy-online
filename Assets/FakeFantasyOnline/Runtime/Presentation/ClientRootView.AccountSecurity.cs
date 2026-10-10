using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IAccountSecurityReceiveContext
{
    void IAccountSecurityReceiveContext.SeedAccountSecurity(bool hasCode, int windowMilliseconds) =>
        AccountSec.Seed(hasCode, windowMilliseconds);

    void IAccountSecurityReceiveContext.ReceiveAccountSecurityResult(int kind, int code, string message, int tab, int slot, int itemId) =>
        AccountSec.OnResult(kind, code, message, tab, slot, itemId);
}
}
