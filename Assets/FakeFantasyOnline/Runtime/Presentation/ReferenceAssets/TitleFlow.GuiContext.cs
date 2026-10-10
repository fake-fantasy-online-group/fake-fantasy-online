using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    // The common 06000ec7 frame prefix, reached by exactly one page from the existing
    // ClientRootView.OnGUI dispatcher. Page adapters only supply its existing state < 5
    // versus state 5/6 distinction; all resources are read from this same title owner.
    internal Matrix4x4 BeginGuiFrameForIndependentRuntime(bool characterPage)
    {
        float scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
        float offsetX = (Screen.width - scale * 800f) * 0.5f;
        float offsetY = (Screen.height - scale * 600f) * 0.5f;
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f),
            Quaternion.identity, new Vector3(scale, scale, 1f));

        Texture2D background = characterPage ? selBg ?? cbg ?? bg : loginBg ?? bg;
        if (background)
        {
            GUI.matrix = Matrix4x4.identity;
            // The native call is the three-argument overload. Its callee supplies
            // alphaBlend=true; the zero at the call site is MethodInfo, not alpha.
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), background,
                ScaleMode.ScaleAndCrop);
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f),
                Quaternion.identity, new Vector3(scale, scale, 1f));
        }

        // Each page restores only this matrix after its ordinary return. There is no
        // native finally or enclosing color/enabled save, override or restoration.
        return previousMatrix;
    }
}
}
