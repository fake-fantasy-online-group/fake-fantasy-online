using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class PetForgeDlg
{
    // Native OnGUI local Box helper (06000953).
    private void Box(Rect r, Texture2D ico, bool hi = false)
    {
        ArtworkSkin.Input(r, s, hi);
        if (ico != null)
            ItemIconUI.Draw(new Rect(r.x + 3f * s, r.y + 3f * s, r.width - 6f * s, r.height - 6f * s), ico, s);
    }

    private float Para(float y, string text, Color col, float width, int maxLines = 4, float lh = 14f)
    {
        if (!BitmapFont.Ready || string.IsNullOrEmpty(text)) return y;
        float textWidth = (width - 6f) * s;
        var lines = Wrap(text, textWidth);
        int count = Mathf.Min(lines.Count, maxLines);
        for (int i = 0; i < count; i++)
            BitmapFont.CenterOutline(L(CX0 + (CW - width) * 0.5f, y + lh * i, width, lh),
                Ellipsis(lines[i], textWidth), col, s);
        return y + lh * count;
    }

    private bool Btn(Rect r, string label, Vector2 mp, bool enabled)
    {
        bool hover = enabled && r.Contains(mp);
        ArtworkSkin.ButtonFace(r, label, s, enabled && !SysMsgBox.Visible,
            selected: hover, pressed: hover && Input.GetMouseButton(0));
        if (!hover || SysMsgBox.Visible || Event.current.type != EventType.MouseDown || Event.current.button != 0)
            return false;
        Event.current.Use();
        return true;
    }

    private bool Click(Rect r, Vector2 mp)
    {
        if (!r.Contains(mp) || SysMsgBox.Visible || Event.current.type != EventType.MouseDown || Event.current.button != 0)
            return false;
        Event.current.Use();
        return true;
    }
}
}
