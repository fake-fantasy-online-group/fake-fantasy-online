using System.Collections.Generic;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class EquipFuseDlg
{
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);

    private static bool Click(Rect r, Vector2 m) =>
        r.Contains(m) && Event.current.type == EventType.MouseDown && Event.current.button == 0;

    private void Box(Rect r, Texture2D ico, bool hi, float border)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = hi ? new Color(1f, 0.86f, 0.44f, 0.95f) : new Color(0.44f, 0.36f, 0.2f, 0.9f);
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, border), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.yMax - border, r.width, border), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.y, border, r.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.xMax - border, r.y, border, r.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (ico != null) ItemIconUI.Draw(new Rect(r.x + 3f * s, r.y + 3f * s, r.width - 6f * s, r.height - 6f * s), ico, s);
    }

    private static string Ellipsis(string t, float maxW)
    {
        if (!BitmapFont.Ready || string.IsNullOrEmpty(t)) return t ?? "";
        if (BitmapFont.Measure(t, 1f) * UIScale.Factor <= maxW) return t;
        while (t.Length > 1 && BitmapFont.Measure(t + "…", 1f) * UIScale.Factor > maxW)
            t = t.Substring(0, t.Length - 1);
        return t + "…";
    }

    private List<string> WrapTip(string t)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(t)) return lines;
        foreach (string part in t.Split('\n'))
        {
            if (BitmapFont.Ready) lines.AddRange(BitmapFont.Wrap(part, 324f * s, s));
            else lines.Add(part);
        }
        return lines;
    }
}
}
