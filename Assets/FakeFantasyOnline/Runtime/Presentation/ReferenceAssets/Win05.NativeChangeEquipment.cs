using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    // Original .cctor 0600105e Def entry; ARM 0x1127c10 (w,h,cx,cy). Now the "changeequipdlg" row
    // of Defs (Win05.NestedData.cs) so the lookup and this adapter share one object.
    private static Def ChangeEquipmentDefinition => Defs["changeequipdlg"];

    // Original 0600104e: ARM64 0x3bce44 / x86_64 0x3d0b10.
    private void DrawChangeEquip(Func<float, float, float, float, Rect> L, Def d, Event ev)
    {
        if (changeEquipmentCorner != null)
            GUI.DrawTexture(L(3f, 3f, 30f, 30f), changeEquipmentCorner);
        bool held = InventoryDlg.HasHeld;
        int hoverCell = -1;
        string hoverName = null, hoverDesc = null;
        byte hoverQuality = 0, hoverBlocked = 0;
        for (int i = 0; i < 11; i++)
        {
            int part = ChgSlotPart(i);
            Rect r = L(ChgSlotPos[i].x, ChgSlotPos[i].y, 30f, 30f);
            ChgItem? entry = ChgAt(part);
            if (entry.HasValue)
            {
                ChgItem item = entry.Value;
                if (item.icon != null) GUI.DrawTexture(r, item.icon);
                if (item.blocked != 0)
                {
                    GUI.color = new Color(1f, .2f, .2f, .35f);
                    GUI.DrawTexture(r, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                if (item.refine > 0 && BitmapFont.Ready)
                    BitmapFont.DrawOutline(r.xMax - 12f * s, r.yMax - 12f * s,
                        "+" + item.refine, new Color(1f, .85f, .3f, 1f), s);
                if (r.Contains(ev.mousePosition))
                {
                    hoverCell = part;
                    hoverName = item.name;
                    hoverDesc = item.desc;
                    hoverQuality = item.quality;
                    hoverBlocked = item.blocked;
                }
            }
            else if (held && r.Contains(ev.mousePosition))
                Edge(r, new Color(1f, .9f, .4f, .8f));
            if (ev.type == EventType.MouseDown && ev.button == 1 && entry.HasValue &&
                r.Contains(ev.mousePosition))
            {
                if (NetClient.I != null) NetClient.I.ChangeSetTakeForIndependentRuntime(part);
                PlayerController.SuppressWorldClick();
                ev.Use();
            }
        }
        if (hoverCell >= 0)
            UITip.Show(TooltipContent.Fmt(TooltipContent.QualityName(hoverName, hoverQuality), hoverDesc) +
                (hoverBlocked != 0 ? "\n★当前穿不上(等级/职业/属性不足), 换装时会被拒绝" : "") +
                "\n右键取回背包", s);

        Texture2D set = ResLoader.I != null ? ResLoader.I.LoadGsa2021First(
            "res/uires/equipdlg/setchgwearbtn.gsa") : null;
        Texture2D swap = ResLoader.I != null ? ResLoader.I.LoadGsa2021First(
            "res/uires/equipdlg/changeequipbtn.gsa") : null;
        Rect setRect = L(33f, 297f, 70f, 22f);
        Rect swapRect = L(162f, 297f, 70f, 22f);
        Color gray = new Color(150f / 255f, 150f / 255f, 150f / 255f, 1f);
        GUI.color = new Color(1f, 1f, 1f, .35f);
        if (set != null) GUI.DrawTexture(setRect, set);
        else if (BitmapFont.Ready)
        {
            Edge(setRect, gray);
            BitmapFont.CenterOutline(setRect, "设置换装", Color.white, 1f);
        }
        GUI.color = Color.white;
        if (swap != null) GUI.DrawTexture(swapRect, swap);
        else if (BitmapFont.Ready)
        {
            Edge(swapRect, gray);
            BitmapFont.CenterOutline(swapRect, "换装", Color.white, 1f);
        }
        if (setRect.Contains(ev.mousePosition))
            UITip.Show("直接把要换的装备拖进上面的格子即可，\n不需要先设置。点【换装】(或按 X)就会把这几个部位换上身。", s);
        if (swapRect.Contains(ev.mousePosition))
            UITip.Show("一键换装（快捷键 X）：方案里有的部位就换，\n没有的部位保持不变（不会把你脱光）。\n换下来的那几件会回到本窗对应格子里，再按一次就换回去。\n不经过背包，不需要空格。方案里有一件穿不上就整体不换。", s);
        if (ev.type == EventType.MouseDown && ev.button == 0 && swapRect.Contains(ev.mousePosition))
        {
            if (NetClient.I != null) NetClient.I.ChangeSetSwapForIndependentRuntime();
            PlayerController.SuppressWorldClick();
            ev.Use();
        }
        GUI.Button(setRect, GUIContent.none, GUIStyle.none);
        if (BitmapFont.Ready)
            BitmapFont.CenterOutline(L(0f, 64f, d.w, 12f), changeSet.Count > 0
                ? string.Format("换装方案 {0}/11 件 — 点【换装】与身上对调", changeSet.Count)
                : "把装备拖进格子, 再点【换装】", gray, 1f);
    }

    // Original 06001053; the border is one screen pixel, independent of UI scale.
    private static void Edge(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.yMax - 1f, r.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.y, 1f, r.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.xMax - 1f, r.y, 1f, r.height), Texture2D.whiteTexture);
        GUI.color = old;
    }
}
}
