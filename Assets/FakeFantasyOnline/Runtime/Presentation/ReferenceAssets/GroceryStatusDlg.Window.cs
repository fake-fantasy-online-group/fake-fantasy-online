using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class GroceryStatusDlg
{
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
    private void Cap(float x, float y, float w, float h, string t)
    {
        if (BitmapFont.Ready) BitmapFont.CenterOutline(L(x, y, w, h), t, GroUI.GOLD, s);
    }
    private void Lbl(float x, float y, string t, Color c)
    {
        if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + x * s, winY + y * s, t, c, s);
    }
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        Vector2 mp = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(mp);
    }

    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        if (!StallConfigDlg.LocalStalling) { visible = false; return; }
        var stacking = WinZ.Frame(this, L(0, 0, NW, NH), 10);
        Vector2 savedMouse = Event.current.mousePosition;
        if (stacking.Item2) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            s = UIScale.Factor;
            if (WinDrag.Stale(winX, ref _wdGen))
            {
                winX = (Screen.width - NW * s) * 0.5f;
                winY = (Screen.height - NH * s) * 0.5f;
            }
            Vector2 mp = Event.current.mousePosition;
            GUI.depth = stacking.Item1;
            if (bg != null) GUI.DrawTexture(L(0, 0, NW, NH), bg);
            else
            {
                GUI.color = new Color(0.09f, 0.08f, 0.12f, 0.98f);
                GUI.DrawTexture(L(0, 0, NW, NH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            Cap(109, 27, 130, 12, GroUI.ShopName[GroUI.ShopType + 2]);
            if (GroUI.ShopType == 1)
            {
                if (icoBuyLabel != null) GUI.DrawTexture(L(7, 18, 46, 26), icoBuyLabel);
                else Lbl(10, 24, "收购", new Color(1f, 0.7f, 0.4f, 1f));
            }
            Rect viewport = L(39, 59, 216, 350);
            GroUI.Box(viewport);
            if (rows == null || rows.Count == 0)
            {
                if (BitmapFont.Ready)
                    BitmapFont.CenterOutline(L(39, 228, 216, 12),
                        GroUI.ShopType == 1 ? "暂无收购需求" : "摊位暂无商品", GroUI.GRAY, s);
            }
            else
            {
                float contentWidth = viewport.width - 4f * s;
                float contentHeight = rows.Count * 39f * s;
                statusScroll = GUI.BeginScrollView(viewport, statusScroll,
                    new Rect(0, 0, contentWidth, contentHeight), GUIStyle.none, GUIStyle.none);
                int hover = -1;
                for (int i = 0; i < rows.Count; i++)
                {
                    StallDlg.Row row = rows[i];
                    Rect area = new Rect(0, i * 39f * s, contentWidth, 37f * s);
                    GUI.color = new Color(1f, 1f, 1f, 0.05f);
                    GUI.DrawTexture(area, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    Rect iconArea = new Rect(area.x + 4f * s, area.y + 4f * s, 28f * s, 28f * s);
                    if (row.icon != null) ItemIconUI.Draw(iconArea, row.icon, s);
                    GameHud.DrawWashScrollTag(iconArea, row.itemId, s);
                    if (BitmapFont.Ready)
                    {
                        BitmapFont.DrawOutline(area.x + 40f * s, area.y + 5f * s,
                            row.count > 1 ? string.Format("{0} ×{1}", row.name, row.count) : row.name,
                            row.quality != 0 ? (Color)UITip.QualityColor(row.quality) : Color.white, s);
                        string label = GroUI.ShopType == 1 ? "收价 " : "单价 ";
                        BitmapFont.DrawOutline(area.x + 40f * s, area.y + 21f * s, label, new Color(1f, 1f, 0.4f, 1f), s);
                        GroUI.MoneyInline(area.x + 40f * s + BitmapFont.Measure(label, s), area.y + 21f * s, row.price, s);
                    }
                    if (area.Contains(Event.current.mousePosition)) hover = i;
                    if (area.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown &&
                        Event.current.button == 0 && InventoryDlg.ShareModHeld(Event.current))
                    {
                        InventoryDlg.ShareToChat(3, 0, i, row.name);
                        Event.current.Use();
                    }
                }
                GUI.EndScrollView();
                if (hover >= 0 && viewport.Contains(mp))
                {
                    StallDlg.Row row = rows[hover];
                    if (row.pet.HasValue) PetInfoTooltip.Show(row.pet.Value, mp, s, "(我的摊位)");
                    else
                    {
                        string name = row.count > 1 ? string.Format("{0} ×{1}", row.name, row.count) : row.name;
                        UITip.Show(TooltipContent.Fmt(TooltipContent.QualityName(name, row.quality), row.desc), s);
                    }
                }
                if (contentHeight > viewport.height + 1f && slideBtn != null)
                {
                    Rect track = L(267, 62, 15, 344);
                    float thumbHeight = 20f * s;
                    float maximum = contentHeight - viewport.height;
                    float fraction = maximum > 0f ? Mathf.Clamp01(statusScroll.y / maximum) : 0f;
                    float travel = track.height - thumbHeight;
                    Rect thumb = new Rect(track.x, track.y + fraction * travel, track.width, thumbHeight);
                    GUI.DrawTexture(thumb, slideBtn);
                    Event ev = Event.current;
                    if (thumb.Contains(mp) && ev.type == EventType.MouseDown)
                    {
                        sbDrag = true;
                        sbGrab = mp.y - thumb.y;
                        ev.Use();
                    }
                    else if (track.Contains(mp) && ev.type == EventType.MouseDown && travel > 0f)
                    {
                        statusScroll.y = maximum * Mathf.Clamp01((mp.y - track.y - thumbHeight * 0.5f) / travel);
                        ev.Use();
                    }
                    if (sbDrag && ev.type == EventType.MouseDrag && travel > 0f)
                    {
                        statusScroll.y = maximum * Mathf.Clamp01((mp.y - sbGrab - track.y) / travel);
                        ev.Use();
                    }
                    if (ev.type == EventType.MouseUp) sbDrag = false;
                }
            }
            if (GroUI.Btn(L(32, 418, 90, 22), icoEnd, "收摊", mp, s))
            {
                StallConfigDlg.EndLocal();
                visible = false;
            }
            Rect help = L(246, 22, 20, 19);
            if (icoHelp != null) GUI.DrawTexture(help, icoHelp);
            Rect close = L(266, 22, 20, 19);
            if (icoClose != null) GUI.DrawTexture(close, icoClose);
            if (GroUI.Click(close, mp)) visible = false;
            WinDrag.Handle(this, L(0, 0, NW, 46), ref winX, ref winY);
            if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(mp) && !SysMsgBox.Visible)
                Event.current.Use();
        }
        finally
        {
            if (stacking.Item2) Event.current.mousePosition = savedMouse;
        }
    }
}
}
