using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ShopDlg
{
    public static void Open(int shop, bool sell)
    {
        Ensure();
        I.stallMode = false;
        I.shopId = shop;
        I.sellMode = sell;
        I.visible = true;
        I.cart.Clear();
        I.picking = false;
        I.scrollL = Vector2.zero;
        I.scrollR = Vector2.zero;
        if (NetClient.I != null) NetClient.I.OpenShopForIndependentRuntime(shop);
    }
}
}
