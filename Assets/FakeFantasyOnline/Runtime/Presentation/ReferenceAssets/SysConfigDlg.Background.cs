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
