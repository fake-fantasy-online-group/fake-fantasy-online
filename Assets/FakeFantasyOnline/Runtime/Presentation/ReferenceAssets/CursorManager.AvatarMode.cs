using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class CursorManager
{
    internal Texture2D AvatarTextureForIndependentRuntime => avatarCur;
    internal Texture2D NormalTextureForIndependentRuntime => normalCur;

    private bool avatarTried;

    public void SetAvatarMode(bool on)
    {
        if (avatarMode == on) return;
        if (on && !avatarTried)
        {
            avatarTried = true;
            ResLoader loader = ResLoader.I;
            Texture2D source = loader != null ? loader.LoadGsaFrame("res/uires/cursor/avataritemcuscor.gsa", 0) : null;
            avatarCur = ToCursorTex(source);
        }
        avatarMode = on;
        Refresh();
    }

}
}
