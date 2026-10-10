using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    // Official 1.5.8 06000ee0. The frame overlays the normal image; it does not replace it.
    // Owner-only: no original caller has been established, so creation keeps using RadioImg.
    private bool RadioFrame(Rect r, Texture2D norm, Texture2D frm, bool on)
    {
        if (norm) GUI.DrawTexture(r, norm);
        if (on && frm) GUI.DrawTexture(r, frm);
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    // Official 1.5.8 06000ee1. Selection draws the border even without a normal image.
    // Owner-only: no new input or activation route is introduced.
    private bool Radio05(Rect r, Texture2D norm, bool on)
    {
        if (norm) GUI.DrawTexture(r, norm);
        if (on) RecoveredTitleCharacterSurface.DrawBorder(r, 2f, new Color(0.4f, 1f, 0.4f, 1f));
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }
}
}
