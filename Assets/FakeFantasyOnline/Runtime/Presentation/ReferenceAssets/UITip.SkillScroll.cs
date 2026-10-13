using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class UITip
{
    private static bool reqIsSkill;
    private static string skillScrollText;
    private static int skillFirstRow;

    // Explicit opt-in from profession skill cards/hotbar only. Inventory,
    // equipment, comparisons, wardrobe and life-skill requests keep Show/Render.
    internal static void ShowSkill(string text, float scale)
    {
        if (string.IsNullOrEmpty(text)) return;
        Show(text, scale);
        reqIsSkill = true;
        Event current = Event.current;
        if (current != null && current.type == EventType.ScrollWheel && ScrollSkill(text, scale, current.delta.y))
            current.Use();
    }

    private static void ResetSkillScroll()
    {
        skillScrollText = null;
        skillFirstRow = 0;
    }

    private static void SelectSkillScroll(string text)
    {
        if (skillScrollText == text) return;
        skillScrollText = text;
        skillFirstRow = 0;
    }

    private static int SkillVisibleRows(float scale)
    {
        // Preserve the native 16px line pitch and 8px vertical padding. Only
        // overflowing cards reserve one native gray row for the wheel affordance.
        return Mathf.Max(1, (int)((Screen.height - 8f * scale) / (16f * scale)) - 1);
    }

    internal static bool ScrollSkill(string text, float scale, float wheel)
    {
        if (!BitmapFont.Ready || string.IsNullOrEmpty(text) || scale <= 0f) return false;
        SelectSkillScroll(text);
        var layout = Layout(text, scale);
        if (layout.Item3 <= Screen.height)
        {
            skillFirstRow = 0;
            return false;
        }
        int last = Mathf.Max(0, layout.Item1.Count - SkillVisibleRows(scale));
        int movement = wheel > 0f ? 3 : wheel < 0f ? -3 : 0;
        skillFirstRow = Mathf.Max(0, Mathf.Min(last, skillFirstRow + movement));
        return true;
    }

    private static void RenderSkill(string text, float scale, Vector2 mouse)
    {
        if (!BitmapFont.Ready) return;
        SelectSkillScroll(text);
        var layout = Layout(text, scale);
        if (layout.Item3 <= Screen.height)
        {
            skillFirstRow = 0;
            Render(text, null, scale, mouse);
            return;
        }
        Ensure();
        int count = Mathf.Min(SkillVisibleRows(scale), layout.Item1.Count);
        skillFirstRow = Mathf.Max(0, Mathf.Min(layout.Item1.Count - count, skillFirstRow));
        var visible = layout.Item1.GetRange(skillFirstRow, count);
        string hint = "滚轮查看 " + (skillFirstRow + 1) + "/" + layout.Item1.Count;
        visible.Add((hint, new Color32(128, 128, 128, 255), false));
        float width = Mathf.Max(layout.Item2, BitmapFont.Measure(hint, scale) + 12f * scale);
        float height = 8f * scale + visible.Count * 16f * scale;
        float x = mouse.x + 16f * scale;
        float y = mouse.y + 16f * scale;
        if (x + width > Screen.width) x = mouse.x - width - 4f * scale;
        if (y + height > Screen.height) y = mouse.y - height - 4f * scale;
        x = Mathf.Clamp(x, 0f, Mathf.Max(0f, Screen.width - width));
        y = Mathf.Clamp(y, 0f, Mathf.Max(0f, Screen.height - height));
        DrawBox(new Rect(x, y, width, height), visible, scale);
    }
}
}
