using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class PetInfoTooltip
{
    private static Texture2D[] patch9;

    private static void DrawLines(List<(string, Color)> lines, Vector2 mouse, float s)
    {
        float pad = 6f * s;
        float lineHeight = 13f * s;
        float gap = 3f * s;
        float width = 0f;
        foreach (var line in lines)
            width = Mathf.Max(width, BitmapFont.Measure(line.Item1, s));
        width = pad + pad + width;
        float height = pad + pad + lineHeight * lines.Count + gap * (lines.Count - 1);
        float x = mouse.x + 14f * s;
        float y = mouse.y + 8f * s;
        if (x + width > Screen.width) x = mouse.x - width - pad;
        if (y + height > Screen.height) y = Screen.height - height - 2f;
        x = Mathf.Max(0f, x);
        y = Mathf.Max(0f, y);
        GUI.color = new Color32(16, 16, 16, 220);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        PatchPanel(new Rect(x, y, width, height), s);
        float rowY = pad + y;
        foreach (var line in lines)
        {
            BitmapFont.DrawOutline(pad + x, rowY, line.Item1, line.Item2, s);
            rowY = lineHeight + gap + rowY;
        }
    }

    private static void PatchPanel(Rect r, float s)
    {
        if (patch9 == null && ResLoader.I != null)
        {
            string[] names = { "patchupleft", "patchupmiddle", "patchupright", "patchleftmiddle",
                "patchrightmiddle", "patchdownleft", "patchdownmiddle", "patchdownright" };
            patch9 = new Texture2D[8];
            for (int i = 0; i < names.Length; i++)
                patch9[i] = ResLoader.I.LoadGsa("res/uires/patch/" + names[i] + ".gsa");
        }
        if (patch9 == null || patch9[0] == null) return;
        float corner = 11f * s;
        GUI.DrawTexture(new Rect(r.x, r.y, corner, corner), patch9[0]);
        GUI.DrawTexture(new Rect(r.xMax - corner, r.y, corner, corner), patch9[2]);
        GUI.DrawTexture(new Rect(r.x, r.yMax - corner, corner, corner), patch9[5]);
        GUI.DrawTexture(new Rect(r.xMax - corner, r.yMax - corner, corner, corner), patch9[7]);
        float edge = 3f * s;
        GUI.DrawTexture(new Rect(r.x + corner, r.y, r.width - (corner + corner), edge), patch9[1]);
        GUI.DrawTexture(new Rect(r.x + corner, r.yMax - edge, r.width - (corner + corner), edge), patch9[6]);
        GUI.DrawTexture(new Rect(r.x, r.y + corner, edge, r.height - (corner + corner)), patch9[3]);
        GUI.DrawTexture(new Rect(r.xMax - edge, r.y + corner, edge, r.height - (corner + corner)), patch9[4]);
    }
}
}
