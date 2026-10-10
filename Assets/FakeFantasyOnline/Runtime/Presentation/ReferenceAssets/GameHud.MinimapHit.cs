using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    internal static bool MinimapContainsForIndependentRuntime(Vector2 point, float width, float scale,
        bool shown, bool open, bool hasSketchPixels)
    {
        if (!shown) return false;
        float x = width - 151f * scale;
        if (new Rect(x, 0f, 151f * scale, 20f * scale).Contains(point)) return true;
        return open && hasSketchPixels &&
            new Rect(x - 28f * scale, 20f * scale, 184f * scale, 153f * scale).Contains(point);
    }
    internal bool MouseOverMinimapForIndependentRuntime(Vector2 point, float width, float scale) =>
        MinimapContainsForIndependentRuntime(point, width, scale, showMinimap, miniOpen, sketchPx != null);
}
}
