using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    // The common 06000ec7 frame prefix, reached by exactly one page from the existing
    // ClientRootView.OnGUI dispatcher. Login, server selection and character selection
    // share the independently bundled background. Creation keeps its existing background;
    // avatar/stage drawing remains owned by the title and is never replaced by this image.
    internal Matrix4x4 BeginGuiFrameForIndependentRuntime(bool creationPage)
    {
        float scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
        float offsetX = (Screen.width - scale * 800f) * 0.5f;
        float offsetY = (Screen.height - scale * 600f) * 0.5f;
        if (MobileClientPresentation.Enabled)
        {
            Rect safe = MobileClientPresentation.SafeAreaGui;
            scale = Mathf.Min(safe.width / 800f, safe.height / 600f);
            offsetX = safe.x + (safe.width - scale * 800f) * .5f;
            offsetY = safe.y + (safe.height - scale * 600f) * .5f;
            if (mobileTitleInput.Editing)
                offsetY = MobileTitleTextInput.PanY(offsetY, scale, mobileTitleInput.FocusRect,
                    safe, TouchScreenKeyboard.area);
        }
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f),
            Quaternion.identity, new Vector3(scale, scale, 1f));

        if (!creationPage)
        {
            Matrix4x4 contentMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            EmbeddedClientBackground.Draw(new Rect(0f, 0f, Screen.width, Screen.height));
            GUI.matrix = contentMatrix;
        }
        else
        {
            Texture2D background = selBg ?? cbg ?? bg;
            if (background)
            {
                GUI.matrix = Matrix4x4.identity;
                // Keep the recovered creation-page artwork and alpha-blending behavior.
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), background,
                    ScaleMode.ScaleAndCrop);
                GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f),
                    Quaternion.identity, new Vector3(scale, scale, 1f));
            }
        }

        // Each page restores only this matrix after its ordinary return. There is no
        // native finally or enclosing color/enabled save, override or restoration.
        return previousMatrix;
    }
}
}
