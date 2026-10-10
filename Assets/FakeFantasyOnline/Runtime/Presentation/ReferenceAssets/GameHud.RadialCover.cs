using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    // Official 0x06000439. Owner-only: no activation caller is established.
    // This rotates white-texture strips; it does not build or consume a ring texture.
    private static void DrawRadialCover(Rect r, float frac, Color col)
    {
        if (frac <= 0f) return;
        if (frac > 1f) frac = 1f;

        Vector2 center = new Vector2(r.x + r.width * 0.5f, r.y + r.height * 0.5f);
        float radius = Mathf.Sqrt(r.width * r.width + r.height * r.height) * 0.5f;
        int segments = Mathf.Max(1, Mathf.CeilToInt(frac * 48f));
        float step = frac * 360f / segments;

        Color savedColor = GUI.color;
        GUI.color = col;
        Matrix4x4 savedMatrix = GUI.matrix;
        float stripWidth = radius * (2f * Mathf.PI) * step / 360f + 2f;
        Rect strip = new Rect(center.x + stripWidth * -0.5f,
            center.y - radius, stripWidth, radius);

        for (int i = 0; i < segments; i++)
        {
            GUIUtility.RotateAroundPivot(step * -i, center);
            GUI.DrawTexture(strip, Texture2D.whiteTexture);
            GUI.matrix = savedMatrix;
        }
        GUI.color = savedColor;
    }
}
}
