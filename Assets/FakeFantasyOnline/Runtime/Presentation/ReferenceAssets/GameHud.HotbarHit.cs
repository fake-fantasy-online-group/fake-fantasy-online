using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    internal static bool HotbarContainsForIndependentRuntime(Vector2 point, float width, float height,
        float scale, bool bottom, bool visible, bool expanded)
    {
        if (bottom)
        {
            float sh = BottomHotbarScaleForIndependentRuntime(scale, width);
            float bx = BottomHotbarXForIndependentRuntime(sh, width);
            if (!visible)
                return new Rect((bx + 815f) * sh, height - 21f * sh, 20f * sh, 19f * sh).Contains(point);
            float y = height - (expanded ? 95f : 50f) * sh;
            return new Rect(bx * sh, y, 837f * sh, height - y).Contains(point) ||
                new Rect((bx + 815f) * sh, height - 90f * sh, 20f * sh, 60f * sh).Contains(point);
        }
        float x = HotbarXForIndependentRuntime(scale, width);
        return visible
            ? new Rect(x * scale, 0f, 434f * scale, (expanded ? 94f : 49f) * scale).Contains(point)
            : new Rect((x + 403f) * scale, 4f * scale, 20f * scale, 19f * scale).Contains(point);
    }
}
}
