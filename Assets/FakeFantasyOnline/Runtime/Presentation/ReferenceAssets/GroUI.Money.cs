using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class GroUI
{
    private static Texture2D _cg, _cs, _cc;
    private static bool _coinTried;
    private static void EnsureCoins()
    {
        if (_coinTried) return;
        ResLoader loader = ResLoader.I;
        if (loader == null) return;
        _coinTried = true;
        _cg = loader.LoadGsa("res/uires/common/gold.gsa");
        _cs = loader.LoadGsa("res/uires/common/silver.gsa");
        _cc = loader.LoadGsa("res/uires/common/copper.gsa");
    }
    public static void MoneyInline(float x, float y, long m, float s)
    {
        EnsureCoins();
        if (m >= 1000000) x = Coin(x, y, _cg, m / 1000000, s);
        if (m % 1000000 >= 1000) x = Coin(x, y, _cs, (m % 1000000) / 1000, s);
        Coin(x, y, _cc, m % 1000, s);
    }
    public static float MoneyWidth(long m, float s)
    {
        long gold = m / 1000000;
        long silver = (m % 1000000) / 1000;
        long copper = m % 1000;
        float width = 0f;
        if (m >= 1000000)
            width = s * 13f + s * (gold.ToString().Length * 7 + 4) + 0f;
        if (m % 1000000 >= 1000)
            width += s * 13f + s * (silver.ToString().Length * 7 + 4);
        return width + (s * 13f + s * (copper.ToString().Length * 7 + 4));
    }

    public static void MoneyInlineCentered(float x, float y, float width, long m, float s)
    {
        MoneyInline(x + (width - MoneyWidth(m, s)) * 0.5f, y, m, s);
    }

    private static float Coin(float x, float y, Texture2D icon, long value, float s)
    {
        if (icon != null)
        {
            GUI.DrawTexture(new Rect(x, y, 11f * s, 11f * s), icon);
            x += 13f * s;
        }
        string text = value.ToString();
        if (BitmapFont.Ready) BitmapFont.DrawOutline(x, y, text, new Color(1f, 0.95f, 0.7f, 1f), s);
        return x + s * (text.Length * 7 + 4);
    }
}
}
