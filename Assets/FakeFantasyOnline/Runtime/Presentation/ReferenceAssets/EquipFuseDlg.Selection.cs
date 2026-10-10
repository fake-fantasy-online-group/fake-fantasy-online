using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class EquipFuseDlg
{
    public static void Open()
    {
        Ensure();
        I.visible = true;
        I.mainTab = -1; I.mainIcon = null; I.mainName = ""; I.mainDesc = "";
        I.mainId = 0; I.mainQ = 0;
        I.subTab = -1; I.subIcon = null; I.subName = ""; I.subDesc = "";
        I.subId = 0; I.subQ = 0;
        I.pickSub = false; I.ok = false; I.tip = ""; I.confirmText = ""; I.sig = 0;
        I.Query();
    }

    public static void SetEquip(byte tab, int bagIdx, int itemId, int quality, string name, Texture2D icon)
    {
        if (I == null || !I.visible) return;
        if (I.mainTab == tab && I.mainSlot == bagIdx)
        {
            I.mainTab = -1; I.mainIcon = null; I.mainName = ""; I.mainDesc = "";
            I.mainId = 0; I.mainQ = 0; I.pickSub = false;
            I.Query();
            return;
        }
        if (I.subTab == tab && I.subSlot == bagIdx)
        {
            I.subTab = -1; I.subIcon = null; I.subName = ""; I.subDesc = "";
            I.subId = 0; I.subQ = 0; I.pickSub = true;
            I.Query();
            return;
        }
        if ((!I.pickSub && I.mainTab < 0) || I.subTab >= 0)
        {
            I.mainTab = tab; I.mainSlot = bagIdx; I.mainId = itemId; I.mainQ = quality;
            I.mainName = name ?? ""; I.mainIcon = icon; I.pickSub = true;
        }
        else
        {
            I.subTab = tab; I.subSlot = bagIdx; I.subId = itemId; I.subQ = quality;
            I.subName = name ?? ""; I.subIcon = icon; I.pickSub = false;
        }
        I.Query();
    }

    private void Query()
    {
        NetClient.I?.EquipFuseForIndependentRuntime(0, 0, unchecked((byte)System.Math.Max(0, mainTab)), mainTab < 0 ? -1 : mainSlot,
            unchecked((byte)System.Math.Max(0, subTab)), subTab < 0 ? -1 : subSlot, 0);
    }
}
}
