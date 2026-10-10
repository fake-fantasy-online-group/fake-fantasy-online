namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class StallConfigDlg
{
    private void DoCreateConfirmed()
    {
        if (NetClient.I == null) return;
        string name = string.IsNullOrWhiteSpace(shopName)
            ? CharData.name + (shoptype == 1 ? "的收购商店" : "的出售商店") : shopName;
        NetClient.I.StallCreateForIndependentRuntime(shoptype, name);
        foreach (Staged entry in staged)
        {
            if (shoptype == 1) NetClient.I.StallAddForIndependentRuntime(0, -1, entry.itemId, entry.count, entry.price);
            else NetClient.I.StallAddForIndependentRuntime(entry.bagTab, entry.bagSlot, entry.itemId, entry.count, entry.price);
        }
        byte localType = shoptype;
        LocalName = name;
        LocalType = localType;
        visible = false;
    }
    public static void EndLocal()
    {
        LocalName = "";
        LocalStalling = false;
        NetClient.I?.StallEndForIndependentRuntime();
    }
    public static void ResetLocal()
    {
        LocalStalling = false;
        LocalName = "";
        LocalType = 0;
    }
}
}
