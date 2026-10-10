using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class AttrWashDlg
{
    public static void SetTarget(int kind, int tab, int slot, string name, Texture2D icon)
    {
        if (I == null || !I.visible) return;
        I.tKind = kind;
        I.tTab = tab;
        I.tSlot = slot;
        I.tName = name ?? "";
        I.tIcon = icon;
        NetClient.I?.WashAffixForIndependentRuntime(unchecked((byte)kind), unchecked((byte)tab), slot, -1);
    }
}
}
