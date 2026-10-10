using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class CraftUpgradeUI
{
    // Official 1.5.8 060001bb. The line renderer is the official local function
    // <DrawInfo>g__Line|23_0 (060001bd), kept here as a real local function over the same
    // captured state: running y, scale, wrap width and the area rect.
    public static void DrawInfo(Rect area, float s, string act)
    {
        float y = area.y + s * 4f;
        float wrapW = area.width - s * 12f;

        void Line(string text, Color color)
        {
            if (!BitmapFont.Ready) { y += s * 16f; return; }
            foreach (string piece in BitmapFont.Wrap(text, wrapW, s))
            {
                if (area.y + area.height < y + s * 12f) break;
                BitmapFont.DrawOutline(area.x + s * 6f, y, piece, color, s);
                y += s * 16f;
            }
        }

        int slot = Mode != 0 ? SelSlot : RefBagSlot;
        if (slot < 0)
        {
            Line(Mode == 0 ? "把装备从背包拖进炉中" : "点装备槽选择要" + act + "的装备", CraftUI.GRAY);
            return;
        }
        if (CurLevel == -1) { Line("查询中…", CraftUI.GRAY); return; }
        if (CurLevel == -2) { Line("该装备已满级或不可" + act, new Color(1f, 0.6f, 0.4f, 1f)); return; }
        Line(string.Format("当前等级 {0} → {1}", CurLevel, CurLevel + 1), CraftUI.GOLD);
        if (Mode == 0)
        {
            Color rate = SuccessRate < 70
                ? new Color(SuccessRate < 40 ? 0.5f : 0.85f, 1f, 1f, 0.4f)
                : CraftUI.GOLD;
            Line(string.Format("成功率 {0}%", SuccessRate), rate);
            if (FailDestroys)
                Line("★失败会【损毁装备】, 用「免爆加工」可保住(耗1颗免爆宝石)", new Color(1f, 0.35f, 0.35f, 1f));
            else if (CurLevel < SafeLevel)
                Line(string.Format("安全区(+{0} 以内): 失败不掉级、不损毁", SafeLevel), new Color(0.5f, 0.9f, 0.5f, 1f));
            else
                Line(string.Format("★失败会掉回 +{0}(装备不会损毁), 用「免爆加工」可保住等级", SafeLevel), new Color(1f, 0.7f, 0.35f, 1f));
        }
        if (Mats.Count == 0) Line("  无需材料(仅花费金钱)", CraftUI.GRAY);
        foreach (Mat mat in Mats)
        {
            if (mat.icon != null)
                ItemIconUI.Draw(new Rect(area.x + s * 6f, y, s * 14f, s * 14f), mat.icon, s);
            Line(string.Format("  {0} {1}/{2}", mat.name, mat.have, mat.need),
                mat.have < mat.need ? new Color(1f, 0.45f, 0.45f, 1f) : CraftUI.GOLD);
        }
    }
}
}
