using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class StallDetailDlg
{
    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) { tried = false; return; }
        bg = loader.LoadGsa("res/uires/grocerydlg/grocerydetail.gsa");
        okBtn = loader.LoadGsa("res/uires/common/affirmbtn.gsa");
        cancelBtn = loader.LoadGsa("res/uires/common/cancelbtn.gsa");
        closeIc = loader.LoadGsa("res/uires/common/miniclose.gsa");
        goldIc = loader.LoadGsa("res/uires/common/gold.gsa");
        silverIc = loader.LoadGsa("res/uires/common/silver.gsa");
        copperIc = loader.LoadGsa("res/uires/common/copper.gsa");
    }
    private bool Btn(Rect r, Texture2D texture, Vector2 mp)
    {
        bool hover = r.Contains(mp);
        if (texture != null)
        {
            GUI.color = hover ? new Color(1.25f, 1.25f, 1.25f, 1f) : Color.white;
            GUI.DrawTexture(r, texture);
            GUI.color = Color.white;
        }
        if (hover && Event.current.type == EventType.MouseDown && Event.current.button == 0)
        { Event.current.Use(); return true; }
        return false;
    }
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
            if (BitmapFont.Ready) BitmapFont.CenterOutline(L(55f, 7f, 150f, 14f), "商品定价", GOLD, s);
            if (BitmapFont.Ready)
            {
                BitmapFont.DrawOutline(winX + 28f * s, winY + 53f * s, BitmapFont.Elide(itemName, 215f * s, s),
                    quality > 0 ? (Color)UITip.QualityColor(quality) : Color.white, s);
                if (BitmapFont.Measure(itemName, s) > 215f * s && L(28f, 49f, 215f, 16f).Contains(mp))
                    UITip.Show(TooltipContent.QualityName(itemName, quality), s);
            }
            if (BitmapFont.Ready)
            {
                BitmapFont.DrawOutline(winX + 95f * s, winY + 84f * s, ((refPrice / 1000000) % 1000000).ToString(), GRAY, s);
                BitmapFont.DrawOutline(winX + 141f * s, winY + 84f * s, ((refPrice / 1000) % 1000).ToString(), GRAY, s);
                BitmapFont.DrawOutline(winX + 183f * s, winY + 84f * s, (refPrice % 1000).ToString(), GRAY, s);
            }
            if (edit == null)
            {
                edit = new GUIStyle(GUI.skin.textField);
                edit.font = NpcChatter.CnFont();
                edit.alignment = TextAnchor.MiddleCenter;
                edit.fontSize = Mathf.RoundToInt(12f * s);
                // grocerydetail already contains the gold, silver and quantity slots.
                // Keep TextField's caret/selection, without painting Unity's focus skin over them.
                edit.normal.background = edit.hover.background = edit.active.background = edit.focused.background = null;
                edit.onNormal.background = edit.onHover.background = edit.onActive.background = edit.onFocused.background = null;
                edit.normal.textColor = edit.hover.textColor = edit.active.textColor = edit.focused.textColor = Color.white;
                edit.onNormal.textColor = edit.onHover.textColor = edit.onActive.textColor = edit.onFocused.textColor = Color.white;
                edit.padding = new RectOffset(1, 1, 0, 0);
            }
            edit.fontSize = Mathf.RoundToInt(12f * s);
            gold = GUI.TextField(L(94f, 125f, 26f, 16f), gold, 6, edit);
            silver = GUI.TextField(L(137f, 125f, 26f, 16f), silver, 3, edit);
            copper = "";
            if (BitmapFont.Ready)
            {
                BitmapFont.CenterOutline(L(180f, 125f, 26f, 16f), "0", GRAY, s);
                BitmapFont.DrawOutline(winX + 24f * s, winY + 105f * s, "(摆摊定价最小单位为 1 银)", GRAY, s);
            }
            count = GUI.TextField(L(99f, 148f, 66f, 16f), count, 5, edit);
            long total = PI(gold) * 1000000L + PI(silver) * 1000L + PI(copper);
            if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + 24f * s, winY + 168f * s,
                "合计单价: " + StallDlg.MoneyWords(total), total != 0 ? GOLD : GRAY, s);
            if (Btn(L(34f, 185f, 70f, 22f), okBtn, mp)) Confirm();
            if (Btn(L(159f, 185f, 70f, 22f), cancelBtn, mp)) visible = false;
            Rect close = L(230f, 6f, 20f, 19f);
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
