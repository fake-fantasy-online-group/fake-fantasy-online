using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static class CraftUI
{
    public static readonly Color GOLD = new Color(1f, 0.92f, 0.66f, 1f);
    public static readonly Color GRAY = new Color(0.66f, 0.64f, 0.55f, 1f);
    private static float _denyAt;
    private static string _denyWhy;
    public const float DenyHoldSec = 2.5f;
    private static readonly Color DenyColor = new Color(1f, 2f / 3f, 110f / 255f, 1f);

    public static void Panel(Rect r)
    {
        GUI.color = new Color(0.08f, 0.07f, 0.11f, 0.97f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = new Color(0.4f, 0.34f, 0.2f, 1f);
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.yMax - 2f, r.width, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    public static void Box(Rect r)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.16f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    public static void Coin(Rect ic, Texture2D texture, float lx, float ly, long value, float scale, Color color)
    {
        if (texture != null) GUI.DrawTexture(ic, texture);
        if (BitmapFont.Ready) BitmapFont.DrawOutline(lx, ly, value.ToString(), color, scale);
    }

    public static bool Btn(Rect r, Texture2D texture, string label, Vector2 mp, float scale)
    {
        bool hover = r.Contains(mp);
        if (texture != null)
        {
            float tint = hover ? 1.25f : 1f;
            GUI.color = new Color(tint, tint, tint, 1f);
            GUI.DrawTexture(r, texture);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = hover ? new Color(0.55f, 0.42f, 0.18f, 0.95f) : new Color(0.3f, 0.26f, 0.16f, 0.88f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (BitmapFont.Ready) BitmapFont.CenterOutline(r, label, GOLD, scale);
        }
        return Click(r, mp);
    }

    public static bool Click(Rect r, Vector2 mp)
    {
        if (!r.Contains(mp) || SysMsgBox.Visible || Event.current.type != EventType.MouseDown || Event.current.button != 0)
            return false;
        Event.current.Use(); PlayerController.SuppressWorldClick(); return true;
    }

    public static void Deny(string why)
    {
        if (string.IsNullOrEmpty(why) || Time.unscaledTime - _denyAt < 1f) return;
        _denyAt = Time.unscaledTime; _denyWhy = why;
        ChatUI.Add("系统", why, DenyColor);
    }

    public static void DrawDeny(Rect r, float scale)
    {
        if (string.IsNullOrEmpty(_denyWhy)) return;
        if (Time.unscaledTime - _denyAt > DenyHoldSec) { _denyWhy = ""; return; }
        if (BitmapFont.Ready) BitmapFont.CenterOutline(r, _denyWhy, DenyColor, scale);
    }
}
}
