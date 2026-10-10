using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class PopUI
{
    // GOLD is declared once, in PopUI.Box.cs, as `public static readonly` (official
    // field flags 0x36 = Public | Static | InitOnly, static offset 0). It used to be
    // redeclared here as private, which collided with the official declaration.

    // Official 1.5.8 PopUI.Btn/Click 0x06000a43–0x06000a44.
    public static bool Btn(Rect r, Texture2D tex, string fb, Vector2 mp, float s)
    {
        bool hover = r.Contains(mp);
        if (tex != null)
        {
            float brightness = hover ? 1.25f : 1f;
            GUI.color = new Color(brightness, brightness, brightness, 1f);
            GUI.DrawTexture(r, tex);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = hover
                ? new Color(0.55f, 0.42f, 0.18f, 0.95f)
                : new Color(0.3f, 0.26f, 0.16f, 0.88f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (BitmapFont.Ready) BitmapFont.CenterOutline(r, fb, GOLD, s);
        }
        if (!hover || SysMsgBox.Visible) return false;
        if (Event.current.type != EventType.MouseDown || Event.current.button != 0) return false;
        Event.current.Use();
        PlayerController.SuppressWorldClick();
        return true;
    }

    public static bool Click(Rect r, Vector2 mp)
    {
        if (!r.Contains(mp) || SysMsgBox.Visible) return false;
        if (Event.current.type != EventType.MouseDown || Event.current.button != 0) return false;
        Event.current.Use();
        PlayerController.SuppressWorldClick();
        return true;
    }

}
}
