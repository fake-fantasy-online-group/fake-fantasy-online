using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class SysConfigDlg
{
    private void Band(float sy0, float sy1, float dy, float dh)
    {
        GUI.DrawTextureWithTexCoords(
            L(0f, dy, BG_W, dh), bg,
            new Rect(0f, 1f - sy1 / BG_H, 1f, (sy1 - sy0) / BG_H));
    }

    private void DrawExtensionBackground()
    {
        Rect target = L(26f, 360f, 224f, 52f);
        if (bg == null)
        {
            ArtworkSkin.Panel(target, s);
            return;
        }
        // Reuse the same verified clean strip as MaskBaked. Tile at native logical
        // scale so the extra controls inherit the panel texture without any old labels.
        const float sampleX = 45f, sampleY = 108f, sampleW = 193f, sampleH = 5f;
        for (float y = 0f; y < 52f; y += sampleH)
        for (float x = 0f; x < 224f; x += sampleW)
        {
            float width = Mathf.Min(sampleW, 224f - x);
            float height = Mathf.Min(sampleH, 52f - y);
            GUI.DrawTextureWithTexCoords(L(26f + x, 360f + y, width, height), bg,
                new Rect(sampleX / BG_W, 1f - (sampleY + height) / BG_H,
                    width / BG_W, height / BG_H));
        }
    }

    private void MaskBaked(float dy)
    {
        float y = dy - 1f;
        Rect uv = new Rect(.16853933f, .7460674f, .72284645f, .011235955f);
        GUI.DrawTextureWithTexCoords(L(45f, y, 193f, 5f), bg, uv);
        GUI.DrawTextureWithTexCoords(L(45f, y + 5f, 193f, 5f), bg, uv);
        GUI.DrawTextureWithTexCoords(L(45f, y + 10f, 193f, 5f), bg, uv);
    }
}
}
