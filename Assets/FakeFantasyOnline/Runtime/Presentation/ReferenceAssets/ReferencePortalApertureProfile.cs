using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Visual apertures of the two verified DXWG stair-door images, in untrimmed
// logical pixels. Collision masks are deliberately NOT modified or interpreted
// as rear-wall geometry. Pixels outside these polygons keep their old depth.
internal static class ReferencePortalApertureProfile
{
    internal const int PortalId = 0x013005dd;
    private static readonly int[] StairRight = { 59,142, 65,126, 80,111, 95,117,
        111,132, 125,151, 137,177, 137,259, 59,233 };
    private static readonly int[] StairLeft = { 80,176, 91,150, 108,126, 125,112,
        138,119, 154,137, 161,149, 161,235, 80,276 };

    internal static bool TryGet(int id, string path, out int[] polygon)
    {
        polygon = null;
        if (id == 0x182d012d && path != null &&
            path.EndsWith("/JZ_DXWG_0301.gso", StringComparison.OrdinalIgnoreCase))
            polygon = StairRight;
        else if (id == 0x182d0c1e && path != null &&
            path.EndsWith("/JZ_DXWG_3102.gso", StringComparison.OrdinalIgnoreCase))
            polygon = StairLeft;
        return polygon != null;
    }

    // Half-open column sample. All authored profiles are convex; the two
    // intersections describe only the rear doorway plane, not its front pillars.
    internal static bool Column(int[] polygon, float x, out float top, out float bottom)
    {
        top = float.MaxValue;
        bottom = float.MinValue;
        if (polygon == null) return false;
        int count = polygon.Length / 2;
        for (int i = 0; i < count; i++)
        {
            int j = (i + 1) % count;
            float ax = polygon[i * 2], bx = polygon[j * 2];
            if (x < Math.Min(ax, bx) || x >= Math.Max(ax, bx)) continue;
            float y = polygon[i * 2 + 1] + (x - ax) *
                (polygon[j * 2 + 1] - polygon[i * 2 + 1]) / (bx - ax);
            top = Math.Min(top, y);
            bottom = Math.Max(bottom, y);
        }
        return top < bottom;
    }
}
}
