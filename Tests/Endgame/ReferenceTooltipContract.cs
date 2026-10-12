using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

internal static class ReferenceTooltipContract
{
    private static int checks;

    private static void Check(bool passed, string message)
    {
        checks++;
        if (!passed) throw new Exception(message);
    }

    private static void Row(ReferenceFirstWorldUiTipRow row, string text,
        byte r, byte g, byte b, bool centered = false)
    {
        Check(row.Text == text, "Decoded row text: " + text);
        Check(Math.Abs(row.Color.r - r / 255f) < 0.0001f &&
            Math.Abs(row.Color.g - g / 255f) < 0.0001f &&
            Math.Abs(row.Color.b - b / 255f) < 0.0001f &&
            Math.Abs(row.Color.a - 1f) < 0.0001f, "Decoded row color: " + text);
        Check(row.Centered == centered, "Decoded row alignment: " + text);
    }

    public static int Main()
    {
        ReferenceFirstWorldUiTip tip;
        Check(ReferenceFirstWorldUiTip.TryCreate(new ReferenceResourceLoader(), out tip),
            "Diagnostic owner constructs against the deterministic bitmap font");

        // Explicit markers must beat title, set-name and +/- auto-color heuristics.
        var white = tip.BuildLayout("\u00030基础属性 +10\n\u00030套装基础 -5", 1f);
        Check(white.Rows.Count == 2, "Quality zero row count");
        Row(white.Rows[0], "基础属性 +10", 247, 245, 245);
        Row(white.Rows[1], "套装基础 -5", 247, 245, 245);

        var gold = tip.BuildLayout("\u0007\u00035装备名称\n\u00035新词缀 +10\n\u00035套装属性 -5", 1f);
        Check(gold.Rows.Count == 3, "Quality five row count");
        Row(gold.Rows[0], "装备名称", 183, 155, 98, true);
        Row(gold.Rows[1], "新词缀 +10", 183, 155, 98);
        Row(gold.Rows[2], "套装属性 -5", 183, 155, 98);

        var existing = tip.BuildLayout("\u00031绿色\n\u00032蓝色\n\u00033紫色\n\u00034普通稀有\n\u0006传奇特效 +10", 1f);
        Check(existing.Rows.Count == 5, "Existing quality/effect row count");
        Row(existing.Rows[0], "绿色", 64, 224, 96);
        Row(existing.Rows[1], "蓝色", 82, 150, 252);
        Row(existing.Rows[2], "紫色", 210, 90, 255);
        Row(existing.Rows[3], "普通稀有", 255, 180, 40);
        Row(existing.Rows[4], "传奇特效 +10", 255, 136, 48);

        // The stand-in font measures every character at six pixels, making wrapping exact.
        string longText = new string('A', 26);
        foreach (float scale in new[] { 1f, 2f })
        {
            var wrapped = tip.BuildLayout("\u0007\u00035" + longText + "\\n\u00030基础", scale);
            Check(wrapped.Rows.Count == 3, "Wrapping and escaped newline at scale " + scale);
            Row(wrapped.Rows[0], new string('A', 25), 183, 155, 98, true);
            Row(wrapped.Rows[1], "A", 183, 155, 98, true);
            Row(wrapped.Rows[2], "基础", 247, 245, 245);
            Check(wrapped.Width == 162f * scale && wrapped.Height == 56f * scale,
                "Markers do not contribute to dimensions at scale " + scale);
        }

        var fallback = tip.BuildLayout("未标记标题\n套装属性\n力量 +10\n基础属性", 1f);
        Row(fallback.Rows[0], "未标记标题", 255, 220, 120);
        Row(fallback.Rows[1], "套装属性", 96, 160, 255);
        Row(fallback.Rows[2], "力量 +10", 96, 230, 230);
        Row(fallback.Rows[3], "基础属性", 255, 255, 255);

        tip.Dispose();
        Check(tip.BuildLayout("\u00035已释放", 1f) == null, "Disposed owner rejects layout");
        Console.WriteLine("PASS " + checks + " diagnostic tooltip production-source contracts (deterministic stubs, no Unity rendering)");
        return 0;
    }
}
