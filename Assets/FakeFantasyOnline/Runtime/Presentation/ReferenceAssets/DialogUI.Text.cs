using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    private Font fallbackFont;
    private GUIStyle nameTextStyle;
    private GUIStyle greetingTextStyle;
    private GUIStyle optionTextStyle;

    // Official DialogUI.BuildStyles (06000226): fresh styles on each visible OnGUI pass.
    // The native scale argument is unused; all three font sizes remain 12.
    private void BuildStyles(float scale)
    {
        nameTextStyle = new GUIStyle
        {
            font = fallbackFont, fontSize = 12, alignment = TextAnchor.MiddleLeft
        };
        nameTextStyle.normal.textColor = NpcNameColor;
        greetingTextStyle = new GUIStyle
        {
            font = fallbackFont, fontSize = 12,
            alignment = TextAnchor.UpperLeft, wordWrap = false
        };
        greetingTextStyle.normal.textColor = Color.white;
        optionTextStyle = new GUIStyle
        {
            font = fallbackFont, fontSize = 12, alignment = TextAnchor.MiddleLeft
        };
        optionTextStyle.normal.textColor = Color.white;
    }

    // Official DialogUI.OutLabel (06000228): four outline labels, main label, then restore.
    private static void OutLabel(Rect rect, string text, GUIStyle style, Color main, float offset)
    {
        Color previous = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
        GUI.Label(new Rect(rect.x - offset, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + offset, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y - offset, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y + offset, rect.width, rect.height), text, style);
        style.normal.textColor = main;
        GUI.Label(rect, text, style);
        style.normal.textColor = previous;
    }

    // Official DialogUI.NpcCore (06000213). Owner-only: no caller is established.
    private static string NpcCore(string n)
    {
        if (string.IsNullOrEmpty(n)) return n;
        foreach (string prefix in TitlePrefixes)
            if (n.StartsWith(prefix) && n.Length - prefix.Length >= 2)
                return n.Substring(prefix.Length);
        return n;
    }

    // Official DialogUI.QuestLineFor (0x06000214): first nonempty exact-destination task.
    private string QuestLineFor(string npc)
    {
        if (string.IsNullOrEmpty(npc)) return null;
        foreach (TaskClient.ActiveTask task in TaskClient.Active)
            if (task.to == npc && !string.IsNullOrEmpty(task.text)) return task.text;
        return null;
    }

    // Official BuildRich (0x06000227), 29 RGB integers from field D6B4C2B9...A7406.
    private static Color[] BuildRich()
    {
        int[] rgb = { 0x000000, 0xfed900, 0xa0eb91, 0xa8e1a8, 0xff8200, 0xfe768f, 0x28d7bc, 0xc67dfa, 0xb0762e, 0xb9b9b9, 0x2affff, 0xe1c89b, 0xf6f1ba, 0x5296fc, 0x0afdde, 0xea35ff, 0x28f049, 0xf4f112, 0xc4c4c4, 0xf7f5f5, 0xfd4100, 0xe7e9ed, 0xff5a5a, 0x0000f0, 0x581afd, 0xe5b753, 0x6464ff, 0xf78989, 0x8f60f8 };
        Color[] colors = new Color[100];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
        for (int i = 0; i < rgb.Length; i++)
            colors[i] = new Color(((rgb[i] >> 16) & 255) / 255f,
                ((rgb[i] >> 8) & 255) / 255f, (rgb[i] & 255) / 255f, 1f);
        return colors;
    }

    // Official DrawWrapped (0x06000229): codes take no space, width wraps by glyph,
    // and a row is drawn only when its complete line height fits the body rectangle.
    private void DrawWrapped(float x, float y, float width, float height, string text, GUIStyle style, float lineH)
    {
        if (string.IsNullOrEmpty(text)) return;
        bool bitmap = BitmapFont.Ready;
        Color previous = GUI.color;
        int length = text.Length;
        float right = x + width, bottom = y + height;
        Color color = Color.white;
        float px = x, py = y;
        for (int i = 0; i < length;)
        {
            char c = text[i];
            if (c == '/' && i + 3 < length && text[i + 1] == 'C' &&
                char.IsDigit(text[i + 2]) && char.IsDigit(text[i + 3]))
            {
                int index = (text[i + 2] - '0') * 10 + text[i + 3] - '0';
                color = index == 0 ? Color.white : RICH[index];
                i += 4;
                continue;
            }
            if (c == '\n') { px = x; py += lineH; i++; continue; }
            string glyph = c.ToString();
            float advance = bitmap ? BitmapFont.Measure(glyph, bfs)
                : style.CalcSize(new GUIContent(glyph)).x;
            if (px + advance > right) { px = x; py += lineH; }
            if (py + lineH > bottom) break;
            if (bitmap) BitmapFont.Draw(Mathf.Round(px), Mathf.Round(py), c.ToString(), color, bfs);
            else
            {
                GUI.color = color;
                GUI.Label(new Rect(Mathf.Round(px), Mathf.Round(py), advance + 2f, lineH),
                    c.ToString(), style);
            }
            px += advance;
            i++;
        }
        GUI.color = previous;
    }
}
}
