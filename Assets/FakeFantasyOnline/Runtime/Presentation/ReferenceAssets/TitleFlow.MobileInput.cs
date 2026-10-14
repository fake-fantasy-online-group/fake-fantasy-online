using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    private readonly MobileTitleTextInput mobileTitleInput = new MobileTitleTextInput();

    private string MobileField(string id, Rect rect, string value, int limit, bool secure, GUIStyle style)
    {
        // Expand vertically only within the existing row spacing. Never overlap adjacent rows.
        rect.y -= 4f;
        rect.height += 8f;
        return mobileTitleInput.Draw(id, rect, value, limit, secure, style);
    }

    private void OnDisable() => mobileTitleInput.Close();
    private void OnApplicationFocus(bool focused) => mobileTitleInput.FocusChanged(focused,
        UnityEngine.Application.platform == RuntimePlatform.Android);
    private void OnApplicationPause(bool paused) { if (paused) mobileTitleInput.Close(); }
}
}
