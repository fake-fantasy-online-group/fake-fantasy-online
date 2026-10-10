using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    internal bool MouseOverAvatarForIndependentRuntime(Vector2 point, float scale) =>
        showAvatar && new Rect(0f, 0f, 171f * scale, 102f * scale).Contains(point);

    internal bool MouseOverMenuForIndependentRuntime(Vector2 point, float width, float height, float scale) =>
        !GameSettings.HotbarBottom && showMenu &&
        new Rect(width - 382f * scale, height - 62f * scale, 382f * scale, 62f * scale).Contains(point);

    public static Rect TargetBarRect()
    {
        float scale = UIScale.Factor;
        return new Rect(72f * scale, 108f * scale, 128f * scale, 40f * scale);
    }
    internal bool MouseOverTargetForIndependentRuntime()
    {
        if (!showTarget || !(TargetSelect.CurrentSourceForIndependentRuntime is RemotePlayer)) return false;
        Rect rect = TargetBarRect();
        return rect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
    }
}
}
