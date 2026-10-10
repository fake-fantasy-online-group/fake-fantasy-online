using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class CraftUpgradeUI
{
    public static void DrawRefineSlot(Rect area, float s)
    {
        if (RefBagSlot < 0)
        {
            if (BitmapFont.Ready) BitmapFont.CenterOutline(new Rect(area.x, area.y + area.height * 0.5f - 6f * s,
                area.width, 12f * s), "把要加工的装备从背包拖进来", CraftUI.GRAY, s);
            return;
        }
        var icon = new Rect(area.x + area.width * 0.5f - 24f * s, area.y + 26f * s, 48f * s, 48f * s);
        GUI.color = new Color(0.55f, 0.42f, 0.18f, 0.35f);
        GUI.DrawTexture(new Rect(icon.x - 4f * s, icon.y - 4f * s, 56f * s, 56f * s), Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (RefIcon != null) ItemIconUI.Draw(icon, RefIcon, s);
        if (!BitmapFont.Ready) return;
        BitmapFont.CenterOutline(new Rect(area.x, icon.yMax + 8f * s, area.width, 12f * s),
            BitmapFont.Elide(RefName, area.width - 8f * s, s), CraftUI.GOLD, s);
        var line = new Rect(area.x, icon.yMax + 26f * s, area.width, 12f * s);
        if (CurLevel == -2) BitmapFont.CenterOutline(line, "已满级/不可加工", new Color(1f, 0.6f, 0.4f, 1f), s);
        else if (CurLevel >= 0)
        {
            BitmapFont.CenterOutline(line, string.Format("精炼 {0} → {1}", CurLevel, CurLevel + 1), CraftUI.GOLD, s);
            Color color = SuccessRate >= 70 ? CraftUI.GOLD : new Color(1f, SuccessRate >= 40 ? 0.85f : 0.5f, 0.4f, 1f);
            BitmapFont.CenterOutline(new Rect(area.x, icon.yMax + 42f * s, area.width, 12f * s),
                string.Format("成功率 {0}%", SuccessRate), color, s);
        }
        else BitmapFont.CenterOutline(line, "查询中…", CraftUI.GRAY, s);
    }
}
}
