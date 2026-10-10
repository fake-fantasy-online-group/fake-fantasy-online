using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IAttributeWashReceiveContext
{
    void IAttributeWashReceiveContext.ReceiveAttributeWashInfo(int kind, int tab, int slot, int itemId,
        int quality, List<string> affixes, List<bool> atMax) =>
        AttrWashDlg.OnInfo(kind, tab, slot, itemId, quality, affixes, atMax);
}
}
