using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class RepairDlg
{
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) { tried = false; return; }
        bg = loader.LoadGsa("res/uires/repairdlg/repairdlg.gsa");
        closeIc = loader.LoadGsa("res/uires/common/miniclose.gsa");
        repairBtn = loader.LoadGsa("res/uires/repairdlg/repairbtn.gsa");
        cancelBtn = loader.LoadGsa("res/uires/common/cancelbtn.gsa");
        goldIc = loader.LoadGsa("res/uires/common/gold.gsa");
        silverIc = loader.LoadGsa("res/uires/common/silver.gsa");
        copperIc = loader.LoadGsa("res/uires/common/copper.gsa");
    }

    private void Coin(float x, float y, Texture2D ic, long v, bool red)
    {
        if (ic != null) GUI.DrawTexture(L(x, y, 11, 11), ic);
        if (BitmapFont.Ready)
            BitmapFont.DrawOutline(winX + (x + 12f) * s, winY + y * s, v.ToString(),
                red ? new Color(1f, 0.4f, 0.4f, 1f) : new Color(1f, 0.95f, 0.7f, 1f), s);
    }

    private bool Btn(Rect r, Texture2D texture, string fallback, Vector2 mp)
    {
        bool hover = r.Contains(mp);
        if (texture != null)
        {
            float tint = hover ? 1.25f : 1f;
            GUI.color = new Color(tint, tint, tint, 1f);
            GUI.DrawTexture(r, texture);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = hover ? new Color(0.55f, 0.42f, 0.18f, 0.95f) : new Color(0.3f, 0.26f, 0.16f, 0.85f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (BitmapFont.Ready) BitmapFont.CenterOutline(r, fallback, Color.white, s);
        }
        if (!hover || SysMsgBox.Visible || Event.current.type != EventType.MouseDown || Event.current.button != 0) return false;
        Event.current.Use();
        return true;
    }

    private void OnGUI()
    {
        DrawMobilePickCancel();
        if (Picking && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            CancelPick();
            Event.current.Use();
        }
        if (Picking && Event.current.type == EventType.MouseDown && Event.current.button == 1)
        {
            CancelPick();
            Event.current.Use();
        }
        if (!visible) return;
        if (!tried) Load();
        s = UIScale.Factor;
        winX = (Screen.width - NW * s) * 0.5f;
        winY = (Screen.height - NH * s) * 0.5f;
        Vector2 mp = Event.current.mousePosition;
        var stacking = WinZ.Frame(this, L(0, 0, NW, NH), 10);
        Vector2 savedMouse = Event.current.mousePosition;
        if (stacking.Item2) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            GUI.depth = stacking.Item1;
            if (bg != null) GUI.DrawTexture(L(0, 0, NW, NH), bg);
            else
            {
                GUI.color = new Color(0.08f, 0.07f, 0.1f, 0.98f);
                GUI.DrawTexture(L(0, 0, NW, NH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + 51f * s, winY + 21f * s, "装备修理", new Color(1f, 0.9f, 0.6f, 1f), s);
            if (icon != null) GUI.DrawTexture(L(79, 68, 36, 36), icon);
            if (BitmapFont.Ready)
            {
                string text = string.IsNullOrEmpty(info) ? "" : info;
                if (count > 1) text += string.Format(" ×{0}", count);
                BitmapFont.DrawOutline(winX + 42f * s, winY + 112f * s, text, Color.white, s);
            }
            bool shortOfMoney = fee > InventoryDlg.Money;
            Coin(60, 134, goldIc, fee / 1000000, shortOfMoney);
            Coin(97, 134, silverIc, (fee % 1000000) / 1000, shortOfMoney);
            Coin(133, 134, copperIc, fee % 1000, shortOfMoney);
            long money = InventoryDlg.Money;
            Coin(60, 160, goldIc, money / 1000000, false);
            Coin(97, 160, silverIc, (money % 1000000) / 1000, false);
            Coin(133, 160, copperIc, money % 1000, false);
            if (Btn(L(13, 186, 70, 22), repairBtn, "修理", mp))
            {
                if (shortOfMoney)
                {
                    if (BitmapFont.Ready) ChatUI.Add("系统", "金钱不足", new Color(1f, 120f / 255f, 120f / 255f, 1f));
                }
                else
                {
                    if (NetClient.I != null) NetClient.I.RepairConfirmForIndependentRuntime(mode, slot, targetKind, tab);
                    visible = false;
                }
            }
            if (Btn(L(113, 186, 70, 22), cancelBtn, "取消", mp)) visible = false;
            Rect close = L(169, 17, 20, 19);
            if (closeIc != null) GUI.DrawTexture(close, closeIc);
            else if (BitmapFont.Ready) BitmapFont.DrawOutline(close.x + 4f * s, close.y + 2f * s, "x", Color.red, s);
            if (close.Contains(mp) && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                visible = false;
                Event.current.Use();
            }
        }
        finally
        {
            if (stacking.Item2) Event.current.mousePosition = savedMouse;
        }
    }
}
}
