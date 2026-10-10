using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class StallConfigDlg
{
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen))
        { winX = (Screen.width - NW * s) * 0.5f; winY = (Screen.height - NH * s) * 0.5f; }
        var stacking = WinZ.Frame(this, new Rect(winX, winY, NW * s, NH * s), 10);
        Vector2 originalMouse = Event.current.mousePosition;
        if (stacking.Item2) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            Vector2 mp = Event.current.mousePosition;
            GUI.depth = stacking.Item1;
            if (bg != null) GUI.DrawTexture(L(0f, 0f, NW, NH), bg);
            else
            {
                GUI.color = new Color(0.08f, 0.07f, 0.06f, 0.98f);
                GUI.DrawTexture(L(0f, 0f, NW, NH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            if (BitmapFont.Ready)
                BitmapFont.CenterOutline(L(56f, 9f, 174f, 14f), shoptype == 1 ? "收购商店" : "出售商店", GOLD, s);
            Rect view = L(18f, 35f, 240f, 319f);
            GUI.color = new Color(0f, 0f, 0f, 0.18f);
            GUI.DrawTexture(view, Texture2D.whiteTexture);
            GUI.color = Color.white;
            float contentWidth = view.width - 8f * s;
            float contentHeight = s * staged.Count * 36f;
            int hoverIndex = -1;
            scroll = GUI.BeginScrollView(view, scroll, new Rect(0f, 0f, contentWidth, contentHeight), GUIStyle.none, GUIStyle.none);
            for (int i = 0; i < staged.Count; i++)
            {
                Staged item = staged[i];
                float y = i * 36f * s;
                Rect row = new Rect(0f, y, contentWidth, 35f * s);
                if (rowBk != null) GUI.DrawTexture(row, rowBk);
                else
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.05f);
                    GUI.DrawTexture(row, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                Rect iconRect = new Rect(4f * s, y + 4f * s, 28f * s, 28f * s);
                if (item.icon != null) ItemIconUI.Draw(iconRect, item.icon, s);
                GameHud.DrawWashScrollTag(iconRect, item.itemId, s);
                string name = item.count > 1 ? string.Format("{0} ×{1}", item.name, item.count) : item.name;
                if (BitmapFont.Ready)
                {
                    BitmapFont.DrawOutline(48f * s, y + 6f * s, BitmapFont.Elide(name, 108f * s, s),
                        item.quality > 0 ? (Color)UITip.QualityColor(item.quality) : Color.white, s);
                    BitmapFont.DrawOutline(48f * s, y + 20f * s, "单价 ", YEL, s);
                    GroUI.MoneyInline(48f * s + BitmapFont.Measure("单价 ", s), y + 20f * s, item.price, s);
                }
                if (row.Contains(Event.current.mousePosition)) hoverIndex = i;
                Rect remove = new Rect(160f * s, y + 8f * s, 70f * s, 22f * s);
                bool hover = remove.Contains(Event.current.mousePosition);
                ArtworkSkin.ButtonFace(remove, "下架", s, GUI.enabled, hover,
                    pressed: hover && Input.GetMouseButton(0));
                if (hover && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    staged.RemoveAt(i); SaveTemplate(); Event.current.Use();
                    // No tooltip may borrow the removed row (including the former last row).
                    hoverIndex = -1;
                    break;
                }
            }
            GUI.EndScrollView();
            if (hoverIndex >= 0 && view.Contains(mp))
            {
                Staged item = staged[hoverIndex];
                string name = item.count > 1 ? string.Format("{0} ×{1}", item.name, item.count) : item.name;
                UITip.Show(TooltipContent.Fmt(TooltipContent.QualityName(name, item.quality), item.desc), s);
            }
            NativeScroll.Wheel(view, view.height, contentHeight, ref scroll, 36f * s);
            if (contentHeight > view.height + 1f && slideBtn != null)
            {
                Rect track = L(264f, 36f, 15f, 317f);
                float thumbH = 20f * s;
                float range = contentHeight - view.height;
                float travel = track.height - thumbH;
                float ratio = range > 0f ? Mathf.Clamp01(scroll.y / range) : 0f;
                Rect thumb = new Rect(track.x, track.y + travel * ratio, track.width, thumbH);
                GUI.DrawTexture(thumb, slideBtn);
                if (thumb.Contains(mp) && Event.current.type == EventType.MouseDown)
                { draggingSb = true; sbGrabOff = mp.y - thumb.y; Event.current.Use(); }
                else if (track.Contains(mp) && Event.current.type == EventType.MouseDown && travel > 0f)
                { scroll.y = Mathf.Clamp01((mp.y - track.y - thumbH * 0.5f) / travel) * range; Event.current.Use(); }
                if (draggingSb && Event.current.type == EventType.MouseDrag && travel > 0f)
                { scroll.y = Mathf.Clamp01((mp.y - sbGrabOff - track.y) / travel) * range; Event.current.Use(); }
                if (Event.current.type == EventType.MouseUp) draggingSb = false;
            }
            if (staged.Count == 0 && BitmapFont.Ready)
                BitmapFont.CenterOutline(view, IsSell ? "双击背包物品上架" : "双击背包物品设为收购", GRAY, s);
            if (nameSt == null)
            {
                nameSt = new GUIStyle(GUI.skin.textField);
                nameSt.font = NpcChatter.CnFont();
                nameSt.alignment = TextAnchor.MiddleLeft;
                nameSt.padding = new RectOffset(4, 4, 0, 0);
                nameSt.normal.background = nameSt.focused.background = nameSt.active.background = nameSt.hover.background = null;
                nameSt.normal.textColor = nameSt.focused.textColor = nameSt.active.textColor = nameSt.hover.textColor = Color.white;
            }
            nameSt.fontSize = Mathf.RoundToInt(12f * s);
            // The product UiInput.Field -> UiInput.Draw (0x06000fd1) chain already draws the menu
            // for the control that opened it, and the frozen dependency set of StallConfigDlg.OnGUI
            // (0x06000d92) records no UiInput.DrawMenu (0x06000fd2) edge, so this window must not
            // draw the menu a second time.
            string nextName = UiInput.Field(L(18f, 376f, 256f, 14f), "stallName", shopName ?? "", 12, nameSt, s);
        if (nextName != shopName) shopName = nextName;
            if (Btn(L(22f, 394f, 90f, 22f), beginBtn, mp)) DoCreate();
            if (Btn(L(204f, 394f, 70f, 22f), cancelBtn, mp)) visible = false;
            Rect close = L(262f, 6f, 20f, 19f);
            if (closeIc != null) GUI.DrawTexture(close, closeIc);
            if (Btn(close, null, mp)) visible = false;
            WinDrag.Handle(this, L(0f, 0f, NW, 24f), ref winX, ref winY);
            if (Event.current.type == EventType.MouseDown && L(0f, 0f, NW, NH).Contains(mp) && !SysMsgBox.Visible)
                Event.current.Use();
        }
        finally { if (stacking.Item2) Event.current.mousePosition = originalMouse; }
    }
}
}
