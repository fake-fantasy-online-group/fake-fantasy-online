using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IStallOwnerReceiveContext
{
    void IStallOwnerReceiveContext.ReceiveStallOwner(int ownerId, byte type, string name)
    {
        // The gateway has already published the immutable owner map before this callback.
        // Refresh the legacy renderer from that snapshot instead of mutating its cache directly.
        if (_session != null)
            StallDlg.BindOwnersForIndependentRuntime(_session.StallOwners);
    }

    void IStallOwnerReceiveContext.ReceiveStallClosed(int ownerId)
    {
        if (_session != null)
            StallDlg.BindOwnersForIndependentRuntime(_session.StallOwners);
        ShopDlg.CloseStallIf(ownerId);
        if (ownerId == NetClient.SelfEntityId)
        {
            _stallContentsState = WorldStallContentsState.Empty;
            _stallContentsStateVersion = unchecked(_stallContentsStateVersion + 1L);
            StallConfigDlg.LocalStalling = false;
            StallConfigDlg.LocalName = "";
        }
    }
}
}
