using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class PetForgeDlg
{
    // Native OnGUI local Box helper (06000953).
    private void Box(Rect r, Texture2D ico, bool hi = false)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        float edge = Mathf.Max(1f, Mathf.Round(s));
        GUI.color = hi ? new Color(1f, 0.86f, 0.44f, 0.95f) : new Color(0.44f, 0.36f, 0.2f, 0.9f);
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, edge), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.yMax - edge, r.width, edge), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.y, edge, r.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.xMax - edge, r.y, edge, r.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
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
        GUI.color = !enabled ? new Color(0.22f, 0.2f, 0.16f, 0.8f) :
            hover ? new Color(0.55f, 0.42f, 0.18f, 0.95f) : new Color(0.32f, 0.27f, 0.16f, 0.9f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (BitmapFont.Ready)
            BitmapFont.CenterOutline(r, label,
                enabled ? new Color(1f, 0.92f, 0.66f, 1f) : new Color(0.55f, 0.55f, 0.5f, 1f), s);
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
