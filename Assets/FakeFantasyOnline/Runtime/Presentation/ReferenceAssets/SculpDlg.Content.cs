using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class SculpDlg
{
    private void DrawInfo(Rect area)
    {
        if (!BitmapFont.Ready) return;
        float y = area.y + 4f * s, wrapW = area.width - 12f * s;
        void Line(string text, Color color)
        {
            foreach (string line in BitmapFont.Wrap(text, wrapW, s))
            {
                if (y + 12f * s > area.yMax) break;
                BitmapFont.DrawOutline(area.x + 6f * s, y, line, color, s); y += 16f * s;
            }
        }
        if (!SocketUI.HasTarget || SocketUI.CurLevel < 0)
        { Line("把要打孔/镶嵌的装备从背包拖进装备槽", CraftUI.GRAY); return; }
        int maxHoles = SocketUI.MaxHoles;
        Line(maxHoles <= 0 ? "该部位不能打孔" : string.Format("已开孔 {0}/{1}", SocketUI.HoleCount, maxHoles), CraftUI.GOLD);
        if (!SocketUI.CanDrill)
        {
            if (SocketUI.Reason.Length > 0) Line(SocketUI.Reason, new Color(1f, 0.6f, 0.4f, 1f));
            else if (maxHoles > 0 && SocketUI.NextSuccess <= 0)
                Line(string.Format("已打满{0}孔", maxHoles), new Color(1f, 0.6f, 0.4f, 1f));
        }
        if (SocketUI.NextSuccess > 0)
        {
            Color color = SocketUI.NextSuccess >= 60 ? CraftUI.GOLD : new Color(1f, SocketUI.NextSuccess >= 30 ? 0.85f : 0.5f, 0.4f, 1f);
            Line(string.Format("打第{0}孔 成功率{1}%", SocketUI.HoleCount + 1, SocketUI.NextSuccess), color);
            foreach (var mat in SocketUI.DrillMats)
            {
                if (mat.icon != null) ItemIconUI.Draw(new Rect(area.x + 6f * s, y, 14f * s, 14f * s), mat.icon, s);
                Line(string.Format("  {0} {1}/{2}", mat.name, mat.have, mat.need),
                    mat.have >= mat.need ? CraftUI.GOLD : new Color(1f, 0.45f, 0.45f, 1f));
            }
        }
        if (SocketUI.HoleCount > 0)
        {
            Line("把背包星篆/符文拖到空孔，逐枚镶嵌", CraftUI.GRAY);
            Line("星篆单枚生效；孔满且底材、宿序符合才成阵", CraftUI.GRAY);
        }
    }
    private void Coin(Texture2D icon, float x, long value)
    {
        if (icon != null) GUI.DrawTexture(L(x, 428, 12, 12), icon);
        if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + (x + 13f) * s, winY + 428f * s, value.ToString(), CraftUI.GOLD, s);
    }
}
}
