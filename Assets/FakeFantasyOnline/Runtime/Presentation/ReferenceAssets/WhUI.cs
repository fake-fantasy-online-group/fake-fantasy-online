using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 1.5.8 warehouse controls, 06000fd5–06000fd7.
internal static class WhUI
{
    public static readonly Color GOLD = new Color(1, .92f, .66f, 1);
    public static readonly Color GRAY = new Color(.66f, .64f, .55f, 1);
    public static bool Btn(Rect rect, Texture2D image, string fallback, Vector2 pointer, float scale)
    {
        bool hover = rect.Contains(pointer);
        if (image != null)
        { GUI.color = hover ? new Color(1.25f, 1.25f, 1.25f, 1) : Color.white; GUI.DrawTexture(rect, image); GUI.color = Color.white; }
        else
        {
            GUI.color = hover ? new Color(.55f, .42f, .18f, .95f) : new Color(.3f, .26f, .16f, .88f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = Color.white;
            if (BitmapFont.Ready) BitmapFont.CenterOutline(rect, fallback, GOLD, scale);
        }
        Event e = Event.current;
        if (!hover || SysMsgBox.Visible || e.type != EventType.MouseDown || e.button != 0) return false;
        e.Use(); return true;
    }
    public static bool Click(Rect rect, Vector2 pointer)
    {
        Event e = Event.current;
        if (!rect.Contains(pointer) || SysMsgBox.Visible || e.type != EventType.MouseDown || e.button != 0) return false;
        e.Use(); PlayerController.SuppressWorldClick(); return true;
    }
}
}
