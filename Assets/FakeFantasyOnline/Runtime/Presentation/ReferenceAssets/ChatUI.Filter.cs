using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    // Official 1.6.0 static +0x70/+0x78 and instance +0x98. These start zero/null.
    private static int filterHide;
    private static string _filterChar;
    private Texture2D[] checkbox;

    // 1.6.0 060000e7, ARM64 0x232178..0x232248. Keep the original float order.
    private Rect FilterPanel(float barY, float sc)
    {
        float height = 6f * sc + (FilterChans.Length * 20) * sc;
        return new Rect(25f * sc, (barY - 96f * sc) + 20f * sc - height,
            96f * sc, height);
    }

    // The existing map pointer router supplies its own captured point/bar geometry.
    // It borrows this owner's flag and original panel calculation, with no second state.
    internal bool FilterMouseOverForIndependentRuntime(Vector2 point, float barY, float scale)
        => filterOpen && FilterPanel(barY, scale).Contains(point);

    // 1.6.0 060000e9, ARM64 0x232430..0x232590. An empty character leaves both
    // cached fields intact; publish the new suffix before reading its preference.
    private static void LoadFilter()
    {
        string suffix = CharData.PrefSuffix;
        if (string.IsNullOrEmpty(suffix)) return;
        if (_filterChar == suffix) return;
        _filterChar = suffix;
        filterHide = CharData.GetPref("chatfilter_", 0);
    }

    // 1.6.0 060000ea, ARM64 0x232594..0x2326e0. Unknown channels, including
    // system/broadcast/whisper, are visible. A set bit hides the first matching row.
    public static bool ChanShown(string ch)
    {
        if (filterHide == 0) return true;
        for (int index = 0; index < FilterChans.Length; index++)
        {
            if (FilterChans[index].ch == ch)
                return ((filterHide >> index) & 1) == 0;
        }
        return true;
    }

    // The 1.6.0 OnGUI filter tail follows the channel panel. The existing draw owner
    // calls this directly; it neither creates another GUI driver nor another state copy.
    private void DrawFilterPanel(float barY, float scale, Event current)
    {
        Rect panel = FilterPanel(barY, scale);
        Color previous = GUI.color;
        GUI.color = new Color(0.06f, 0.05f, 0.04f, 0.94f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = new Color(0.4f, 0.7f, 0.4f, 0.7f);
        DrawBorder(panel);
        GUI.color = previous;

        LoadFilter();
        for (int index = 0; index < FilterChans.Length; index++)
        {
            scale = s;
            Rect row = new Rect(panel.x + 3f * scale,
                (panel.y + 4f * scale) + (index * 20) * scale,
                panel.width - 6f * scale, 16f * scale);
            if (row.Contains(current.mousePosition))
            {
                GUI.color = new Color(0.4f, 0.7f, 0.4f, 0.3f);
                GUI.DrawTexture(row, Texture2D.whiteTexture);
                GUI.color = previous;
            }

            bool shown = (filterHide & (1 << index)) == 0;
            scale = s;
            Rect box = new Rect(row.x + (scale + scale), row.y + 3f * scale,
                10f * scale, 10f * scale);
            if (checkbox != null && checkbox.Length > 0 && checkbox[0] != null)
                GUI.DrawTexture(box, checkbox[0]);
            if (shown && checkbox != null && checkbox.Length > 1 && checkbox[1] != null)
                GUI.DrawTexture(box, checkbox[1]);

            // The native path draws this border even after a checkbox texture succeeds.
            GUI.color = new Color(0.47f, 0.47f, 0.47f, 1f);
            DrawBorder(box);
            GUI.color = previous;

            if (BitmapFont.Ready)
            {
                scale = s;
                BitmapFont.DrawOutline(row.x + 16f * scale, row.y + (scale + scale),
                    FilterChans[index].cap,
                    shown ? new Color32(190, 255, 190, 255) : new Color32(120, 120, 120, 255),
                    s);
            }

            if (GUI.Button(row, GUIContent.none, GUIStyle.none))
            {
                filterHide ^= 1 << index;
                CharData.SetPref("chatfilter_", filterHide);
            }
        }
    }
}
}
