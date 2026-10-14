using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class UITip
{
    // Fixed, paged details retain authored tooltip colour/line formatting and frame artwork.
    internal static void DrawMobilePage(Rect rect, string text, float scale, ref int firstRow)
    {
        if (!BitmapFont.Ready) return;
        Ensure();
        var layout = Layout(text, scale);
        int count = Mathf.Max(1, (int)((rect.height - 52 * scale) / (16 * scale)));
        firstRow = Mathf.Clamp(firstRow, 0, Mathf.Max(0, layout.Item1.Count - count));
        var rows = layout.Item1.GetRange(firstRow, Mathf.Min(count, layout.Item1.Count - firstRow));
        GUI.BeginGroup(rect);
        DrawBox(new Rect(0, 0, rect.width, rect.height), rows, scale);
        float width = rect.width / 2;
        Rect prev = new Rect(0, rect.height - 44 * scale, width, 44 * scale);
        Rect next = new Rect(width, rect.height - 44 * scale, width, 44 * scale);
        ArtworkSkin.ButtonFace(prev, "上页", scale, firstRow > 0, false, pressed: false);
        ArtworkSkin.ButtonFace(next, "下页", scale, firstRow + count < layout.Item1.Count, false, pressed: false);
        if (GUI.Button(prev, GUIContent.none, GUIStyle.none)) firstRow = Mathf.Max(0, firstRow - count);
        if (GUI.Button(next, GUIContent.none, GUIStyle.none)) firstRow = Mathf.Min(Mathf.Max(0, layout.Item1.Count-count), firstRow + count);
        GUI.EndGroup();
    }
}
}
