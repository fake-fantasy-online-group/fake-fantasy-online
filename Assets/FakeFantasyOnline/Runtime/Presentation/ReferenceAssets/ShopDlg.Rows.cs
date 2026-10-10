using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ShopDlg
{
    private bool Clicked() => Event.current.type == EventType.MouseDown && Event.current.button == 0;
    private bool RightClicked() => Event.current.type == EventType.MouseDown && Event.current.button == 1;

    private bool GsaBtn(Rect r, Texture2D tex, string fallback, Vector2 mp)
    {
        bool hover = r.Contains(mp);
        if (tex != null)
        {
            float tint = hover ? 1.25f : 1f;
            GUI.color = new Color(tint, tint, tint, 1f);
            GUI.DrawTexture(r, tex);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = hover ? new Color(0.55f, 0.42f, 0.18f, 0.95f) : new Color(0.3f, 0.26f, 0.16f, 0.85f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (BitmapFont.Ready) BitmapFont.CenterOutline(r, fallback, Color.white, s);
        }
        if (!hover || !Clicked()) return false;
        Event.current.Use();
        PlayerController.SuppressWorldClick();
        return true;
    }

    private void DrawCart(float w, float rowH, bool block)
    {
        Vector2 mp = Event.current.mousePosition;
        for (int i = 0; i < cart.Count; i++)
        {
            Cart entry = cart[i];
            Rect r = new Rect(0, rowH * i, w, 39f * s);
            string label = string.Format("{0} ×{1}", entry.name, entry.qty);
            RowCard(r, entry.icon, label, unchecked(entry.unit * entry.qty), !block && r.Contains(mp),
                itemBgR, false, entry.quality, entry.id);
            if (!block)
            {
                if (r.Contains(mp)) tipText = TooltipContent.Fmt(TooltipContent.QualityName(entry.name, entry.quality), entry.desc);
                if (GsaBtn(new Rect(r.xMax - 73f * s, r.y + 8f * s, 70f * s, 22f * s), giveupBtn, "放弃", mp))
                {
                    cart.RemoveAt(i);
                    Event.current.Use();
                    return;
                }
            }
        }
    }

    private void RowCard(Rect r, Texture2D icon, string name, long price, bool hover,
        Texture2D bk = null, bool blocked = false, int quality = 0, int itemId = 0)
    {
        bk = bk ?? itemBg;
        if (bk != null) GUI.DrawTexture(r, bk);
        if (hover)
        {
            GUI.color = new Color(1f, 1f, 0.85f, 0.2f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        Rect iconRect = new Rect(r.x + 5f * s, r.y + 7f * s, 28f * s, 28f * s);
        if (icon != null) ItemIconUI.Draw(iconRect, icon, s);
        if (blocked)
        {
            Texture2D focus = InventoryDlg.FocusBk;
            GUI.color = InventoryDlg.ItemFocusBkColor;
            GUI.DrawTexture(iconRect, focus != null ? focus : Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        GameHud.DrawWashScrollTag(iconRect, itemId, s);
        if (!BitmapFont.Ready) return;
        BitmapFont.DrawOutline(r.x + 48f * s, r.y + 7f * s, name,
            quality > 0 ? (Color)UITip.QualityColor(quality) : Color.white, s);
        if (price >= 0) MoneyInline(r.x + 48f * s, r.y + 20f * s, price);
    }

    private void MoneyInline(float x, float y, long m)
    {
        if (m >= 1000000) x = CoinPart(x, y, goldIc, m / 1000000);
        if (m % 1000000 >= 1000) x = CoinPart(x, y, silverIc, (m % 1000000) / 1000);
        CoinPart(x, y, copperIc, m % 1000);
    }

    private float CoinPart(float x, float y, Texture2D ic, long v)
    {
        if (ic != null)
        {
            GUI.DrawTexture(new Rect(x, y, 11f * s, 11f * s), ic);
            x += 13f * s;
        }
        string text = v.ToString();
        if (BitmapFont.Ready)
            BitmapFont.DrawOutline(x, y, text, new Color(1f, 0.95f, 0.7f, 1f), s);
        return x + s * unchecked(text.Length * 7 + 4);
    }

    private void Coins(float x, float y, long m, Color _)
    {
        x = Coin(341f, y, goldIc, m / 1000000);
        x = Coin(x, y, silverIc, (m % 1000000) / 1000);
        Coin(x, y, copperIc, m % 1000);
    }

    private float Coin(float x, float y, Texture2D ic, long v)
    {
        if (ic != null)
            GUI.DrawTexture(new Rect(winX + x * s, winY + y * s, 11f * s, 11f * s), ic);
        string text = v.ToString();
        if (BitmapFont.Ready)
            BitmapFont.DrawOutline(winX + (x + 13f) * s, winY + y * s, text, new Color(1f, 0.95f, 0.7f, 1f), s);
        float width = BitmapFont.Ready ? BitmapFont.Measure(text, 1f) : text.Length * 6f;
        return x + 13f + width + 6f;
    }
}
}
