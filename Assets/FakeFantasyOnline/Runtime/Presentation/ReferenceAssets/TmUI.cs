using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static class TmUI
{
    public static readonly Color GOLD = new Color(1f, .92f, .66f, 1f);
    public static readonly Color GRAY = new Color(.66f, .64f, .55f, 1f);
    public static void Panel(Rect r)
    {
        GUI.color = new Color(.08f, .07f, .11f, .97f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = new Color(.4f, .34f, .2f, 1f);
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.yMax - 2, r.width, 2), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
    public static void Box(Rect r, Texture2D texture)
    {
        if (texture != null) { GUI.DrawTexture(r, texture); return; }
        GUI.color = new Color(0, 0, 0, .6f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
    public static bool Btn(Rect r, Texture2D texture, string text, Vector2 pointer, float scale)
    {
        bool hover = r.Contains(pointer);
        if (texture != null)
        {
            GUI.color = hover ? new Color(1.25f, 1.25f, 1.25f, 1f) : Color.white;
            GUI.DrawTexture(r, texture);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = hover ? new Color(.55f, .42f, .18f, .95f) : new Color(.3f, .26f, .16f, .88f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (BitmapFont.Ready) BitmapFont.CenterOutline(r, text, GOLD, scale);
        }
        return Click(r, pointer);
    }
    public static bool Click(Rect r, Vector2 pointer)
    {
        if (!r.Contains(pointer) || SysMsgBox.Visible || Event.current.type != EventType.MouseDown || Event.current.button != 0) return false;
        Event.current.Use();
        PlayerController.SuppressWorldClick();
        return true;
    }
}
}
