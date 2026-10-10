using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    private float s;

    // Original Bk 0600103e. Other existing window adapters have not migrated yet.
    private Texture2D Bk(Def d)
    {
        if (d.gsa == null) return null;
        if (bkCache.TryGetValue(d.gsa, out Texture2D texture)) return texture;
        texture = null;
        if (ResLoader.I != null)
        {
            texture = d.g2021 ? ResLoader.I.LoadGsa2021First(d.gsa)
                : d.src24 ? ResLoader.I.LoadGsa24(d.gsa) : ResLoader.I.LoadGsa(d.gsa);
        }
        if (texture != null && (d.src24 || d.iron)) texture = ToBlackIron(texture);
        if (texture != null) texture.filterMode = FilterMode.Point;
        bkCache[d.gsa] = texture;
        return texture;
    }

    private Texture2D[] patch9;

    private void PatchPanel(Rect r)
    {
        Color old = GUI.color;
        GUI.color = new Color(16f / 255f, 16f / 255f, 16f / 255f, 224f / 255f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
        if (patch9 == null && ResLoader.I != null)
        {
            string[] names = { "patchupleft", "patchupmiddle", "patchupright", "patchleftmiddle",
                "patchrightmiddle", "patchdownleft", "patchdownmiddle", "patchdownright" };
            patch9 = new Texture2D[8];
            for (int i = 0; i < 8; i++)
                patch9[i] = ResLoader.I.LoadGsa("res/uires/patch/" + names[i] + ".gsa");
        }
        if (patch9 == null || patch9[0] == null) return;
        float c = 11f * s;
        float t = 3f * s;
        GUI.DrawTexture(new Rect(r.x, r.y, c, c), patch9[0]);
        GUI.DrawTexture(new Rect(r.xMax - c, r.y, c, c), patch9[2]);
        GUI.DrawTexture(new Rect(r.x, r.yMax - c, c, c), patch9[5]);
        GUI.DrawTexture(new Rect(r.xMax - c, r.yMax - c, c, c), patch9[7]);
        GUI.DrawTexture(new Rect(r.x + c, r.y, r.width - (c + c), t), patch9[1]);
        GUI.DrawTexture(new Rect(r.x + c, r.yMax - t, r.width - (c + c), t), patch9[6]);
        GUI.DrawTexture(new Rect(r.x, r.y + c, t, r.height - (c + c)), patch9[3]);
        GUI.DrawTexture(new Rect(r.xMax - t, r.y + c, t, r.height - (c + c)), patch9[4]);
        GUI.color = old;
    }

    // Original WrapText 06001044, measured in full-width character columns.
    private static List<string> WrapText(string text, int maxCN)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text)) return lines;
        var line = new StringBuilder();
        float width = 0f;
        foreach (char ch in text)
        {
            if (ch == '\n')
            {
                lines.Add(line.ToString());
                line.Clear();
                width = 0f;
                continue;
            }
            float tokenWidth = ch > 127 ? 1f : 0.5f;
            if (width + tokenWidth > maxCN)
            {
                lines.Add(line.ToString());
                line.Clear();
                width = 0f;
            }
            line.Append(ch);
            width = tokenWidth + width;
        }
        if (line.Length > 0) lines.Add(line.ToString());
        return lines;
    }
}
}
