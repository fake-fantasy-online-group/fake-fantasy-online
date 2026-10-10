using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal class StallSalesBox : MonoBehaviour
{
    public struct Sale
    {
        public long t;
        public string item;
        public int cnt;
        public long money;
    }
    private static StallSalesBox I;
    private static readonly List<Sale> sales = new List<Sale>();
    private static byte shopType;
    private static readonly Color GOLD = new Color(1f, 0.86f, 0.4f, 1f);
    private static readonly Color GRAY = new Color(0.7f, 0.7f, 0.7f, 1f);
    private static readonly Color WHITE = new Color(0.92f, 0.92f, 0.92f, 1f);
    private Texture2D goldIc, silverIc, copperIc;
    private bool iconsTried;

    private static void Ensure()
    {
        if (I != null) return;
        var go = new GameObject("StallSalesBox");
        I = go.AddComponent<StallSalesBox>();
        DontDestroyOnLoad(go);
    }
    public static void SetSales(byte type, List<Sale> list)
    {
        Ensure();
        shopType = type;
        sales.Clear();
        if (list != null) sales.AddRange(list);
    }
    private void LoadIcons()
    {
        iconsTried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) { iconsTried = false; return; }
        goldIc = loader.LoadGsa("res/uires/common/gold.gsa");
        silverIc = loader.LoadGsa("res/uires/common/silver.gsa");
        copperIc = loader.LoadGsa("res/uires/common/copper.gsa");
    }
    private void OnGUI()
    {
        if (!StallConfigDlg.LocalStalling) return;
        if (!iconsTried) LoadIcons();
        float s = UIScale.Factor;
        int count = Math.Min(sales.Count, 8);
        float rowHeight = 16f * s;
        float headerHeight = 36f * s;
        float x = 8f * s;
        float y = Screen.height * 0.3f;
        float width = 244f * s;
        float height = 8f * s + (headerHeight + rowHeight * Math.Max(count, 1));
        Color saved = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.78f);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.82f, 0.35f, 0.5f);
        GUI.DrawTexture(new Rect(x, y, width, 1f), Texture2D.whiteTexture);
        GUI.color = saved;
        long total = 0;
        foreach (Sale sale in sales) total = unchecked(total + sale.money);
        float textX = x + x;
        Line(textX, y + 6f * s, shopType == 1 ? "收购流水" : "销售流水", GOLD, s);
        float summaryY = y + 21f * s;
        float moneyX = Line(textX, summaryY, shopType == 1 ? "共支出 " : "共收入 ", GRAY, s);
        DrawMoney(moneyX, summaryY, total, s);
        float rowY = y + headerHeight;
        if (sales.Count == 0) Line(textX, rowY, "暂无成交", GRAY, s);
        else
        {
            for (int i = 0; i < count; i++)
            {
                Sale sale = sales[sales.Count - 1 - i];
                string time = DateTimeOffset.FromUnixTimeSeconds(sale.t).ToLocalTime().ToString("HH:mm");
                string suffix = (sale.cnt > 1 ? "×" + sale.cnt.ToString() : "") + " ";
                float reserved = BitmapFont.Ready
                    ? BitmapFont.Measure("[" + time + "] ", s) + BitmapFont.Measure(suffix, s)
                    : 56f * s;
                string name = BitmapFont.Ready
                    ? BitmapFont.Elide(sale.item, width - rowHeight - reserved - 92f * s, s)
                    : sale.item;
                float end = Line(textX, rowY, string.Concat("[", time, "] ", name, suffix), WHITE, s);
                DrawMoney(end, rowY, sale.money, s);
                rowY += rowHeight;
            }
        }
    }
    private float DrawMoney(float x, float y, long copper, float s)
    {
        long amount = Math.Max(0L, copper);
        long gold = amount / 1000000;
        long silver = (amount % 1000000) / 1000;
        long remainder = amount % 1000;
        float cx = x;
        if (gold > 0) Seg(goldIc, gold);
        if (silver > 0) Seg(silverIc, silver);
        if (remainder != 0 || gold == 0 && silver == 0) Seg(copperIc, remainder);
        return cx;

        void Seg(Texture2D ic, long value)
        {
            if (ic != null)
            {
                GUI.DrawTexture(new Rect(cx, y + s, 11f * s, 11f * s), ic);
                cx += 13f * s;
            }
            string text = value.ToString();
            if (BitmapFont.Ready) BitmapFont.DrawOutline(cx, y, text, WHITE, s);
            cx += (BitmapFont.Ready ? BitmapFont.Measure(text, s) : text.Length * 7 * s) + 4f * s;
        }
    }
    private static float Line(float x, float y, string t, Color c, float s)
    {
        if (BitmapFont.Ready)
        {
            BitmapFont.DrawOutline(x, y, t, c, s);
            return x + BitmapFont.Measure(t, s);
        }
        var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(11f * s) };
        style.normal.textColor = c;
        GUI.Label(new Rect(x, y, 400f, 16f * s), t, style);
        return x + t.Length * 7 * s;
    }
}
}
