using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Visual apertures and upper faces of audited models, in untrimmed
// logical pixels. Collision masks are deliberately NOT modified or interpreted
// as rear-wall geometry. Pixels outside the rear and explicitly preserved
// foreground polygons keep their old depth. Only the four JXMG foreground
// silhouettes may move immediately ahead of their paired portal.
internal static class ReferencePortalApertureProfile
{
    internal const int PortalId = 0x013005dd;
    private static readonly int[] StairRight = { 59,142, 65,126, 80,111, 95,117,
        111,132, 125,151, 137,177, 137,259, 59,233 };
    private static readonly int[] StairLeft = { 80,176, 91,150, 108,126, 125,112,
        138,119, 154,137, 161,149, 161,235, 80,276 };
    // Original 0501/0502 pixels: conservatively inside the inner arch and floor.
    // The outer arch, medallions, jambs and foreground pillars stay in front.
    private static readonly int[] ArchLeft = { 122,216, 137,183, 157,155, 182,136,
        205,121, 216,124, 231,137, 243,155, 251,181, 254,273, 122,340 };
    private static readonly int[] ArchRight = { 304,216, 289,183, 269,155, 244,136,
        221,121, 210,124, 195,137, 183,155, 175,181, 172,273, 304,340 };


    // Audited original pixels. Each rear plane excludes separately preserved
    // front silhouettes (chains, statues, teeth, braziers and crystal pillars).
    private static readonly int[] SWS4701 = { 25, 205, 79, 127, 101, 99, 146, 72, 192, 61, 232, 64, 278, 82, 322, 108, 347, 137, 354, 166, 337, 184, 305, 202, 262, 218, 226, 230, 179, 255, 155, 278, 103, 255, 52, 225 };
    private static readonly int[][] SWS4701Front = {
        new int[] { 42, 112, 55, 91, 72, 96, 85, 122, 83, 171, 68, 181, 48, 164 },
        new int[] { 174, 198, 176, 181, 185, 167, 201, 163, 218, 176, 230, 190, 232, 208, 226, 238, 208, 254, 180, 244 },
        new int[] { 227, 218, 262, 208, 300, 197, 337, 180, 354, 160, 365, 165, 357, 191, 311, 217, 263, 235, 226, 240 },
    };
    private static readonly int[] DFYW1301 = { 215, 363, 232, 323, 249, 293, 264, 244, 281, 204, 301, 181, 329, 172, 349, 187, 367, 208, 397, 239, 412, 281, 412, 329, 429, 371, 429, 411, 380, 437, 329, 441, 283, 427, 235, 401 };
    private static readonly int[][] DFYW1301Front = {
        new int[] { 348, 184, 372, 190, 388, 204, 381, 244, 356, 284 },
        new int[] { 256, 350, 267, 320, 278, 343, 288, 365, 289, 403, 280, 418, 258, 411, 249, 395 },
        new int[] { 278, 337, 302, 354, 327, 365, 348, 369, 372, 360, 398, 337, 415, 317, 421, 324, 405, 351, 378, 373, 350, 382, 322, 378, 301, 367, 280, 354 },
    };
    private static readonly int[] JXMG0124 = { 66, 461, 103, 419, 129, 355, 145, 311, 170, 274, 213, 262, 216, 184, 239, 253, 250, 144, 277, 229, 300, 156, 320, 119, 325, 92, 350, 60, 363, 20, 372, 0, 393, 50, 414, 101, 423, 122, 433, 171, 439, 214, 420, 267, 401, 319, 420, 367, 438, 382, 460, 396, 418, 426, 372, 453, 306, 477, 226, 489, 149, 490, 79, 474 };
    private static readonly int[][] JXMG0124Front = {
        new int[] { 298, 300, 317, 313, 338, 338, 359, 376, 367, 413, 351, 431, 329, 420, 319, 394, 316, 365 },
        new int[] { 84, 349, 83, 321, 70, 296, 64, 295, 74, 362, 84, 374 },
    };
    private static readonly int[] JXMG0125 = { 210, 396, 232, 382, 250, 367, 269, 319, 250, 267, 231, 214, 237, 171, 247, 122, 256, 101, 277, 50, 298, 0, 307, 20, 320, 60, 345, 92, 350, 119, 370, 156, 393, 229, 420, 144, 431, 253, 454, 184, 457, 262, 500, 274, 525, 311, 541, 355, 567, 392, 585, 423, 612, 452, 618, 477, 588, 487, 554, 488, 505, 474, 462, 455, 433, 445, 410, 438, 386, 449, 349, 428, 318, 416, 283, 427, 252, 426 };
    private static readonly int[][] JXMG0125Front = {
        new int[] { 369, 300, 350, 313, 329, 339, 308, 375, 301, 412, 316, 431, 337, 420, 347, 393, 350, 364 },
        new int[] { 580, 350, 586, 320, 600, 296, 607, 295, 598, 363, 586, 375 },
    };
    private static readonly int[] JXMG0126 = { 278, 287, 277, 210, 293, 192, 317, 193, 339, 215, 367, 222, 380, 255, 389, 261, 396, 266, 402, 270, 408, 269, 417, 263, 435, 255, 476, 269, 459, 323, 455, 337, 507, 382, 531, 400, 501, 407, 454, 397, 397, 374, 364, 356, 330, 347, 296, 322 };
    private static readonly int[][] JXMG0126Front = {
        new int[] { 258, 331, 275, 320, 309, 320, 325, 329, 339, 350, 334, 366, 327, 392, 313, 412, 297, 416, 275, 401, 270, 382, 262, 363 },
        new int[] { 409, 266, 435, 255, 459, 257, 476, 269, 473, 280, 465, 293, 462, 316, 457, 330, 449, 328, 446, 308, 427, 295, 418, 281 },
    };
    private static readonly int[] JXMG0127 = { 270, 287, 271, 210, 255, 192, 231, 193, 209, 215, 181, 222, 168, 255, 159, 261, 152, 266, 146, 270, 140, 269, 131, 263, 113, 255, 72, 269, 89, 323, 93, 337, 41, 382, 17, 400, 47, 407, 94, 397, 151, 374, 184, 356, 218, 347, 252, 322 };
    private static readonly int[][] JXMG0127Front = {
        new int[] { 290, 331, 273, 320, 239, 320, 223, 329, 209, 350, 214, 366, 221, 392, 235, 412, 251, 416, 273, 401, 278, 382, 286, 363 },
        new int[] { 139, 266, 113, 255, 89, 257, 72, 269, 75, 280, 83, 293, 86, 316, 91, 330, 99, 328, 102, 308, 121, 295, 130, 281 },
    };
    private static readonly int[] HJ2401 = { 222, 260, 230, 223, 245, 202, 267, 189, 287, 190, 314, 209, 338, 234, 365, 255, 399, 271, 407, 288, 418, 322, 397, 344, 363, 365, 327, 390, 285, 407, 271, 370, 272, 354, 254, 327, 236, 306, 225, 284 };

    // Original wooden upper faces, verified against the map's walkable cells
    // and legacy trigger polygons. These are support surfaces, not tree walls.
    // Their lips, side walls, flowers and foliage keep the original depth.
    private static readonly int[] HYYSC2101 = { 213, 0, 476, 0, 476, 178,
        451, 177, 425, 173, 401, 165, 379, 155, 358, 140, 344, 123,
        337, 111, 328, 108, 329, 99, 338, 88, 350, 81, 338, 80,
        322, 86, 310, 99, 297, 117, 287, 119, 292, 108, 287, 97,
        292, 84, 284, 75, 282, 66, 272, 54, 279, 45, 263, 35,
        247, 30, 235, 32, 224, 25, 210, 27, 200, 22 };
    private static readonly int[] HYYSC2102 = { 0, 0, 44, 7, 83, 16,
        108, 29, 121, 41, 120, 45, 134, 54, 142, 68, 143, 86,
        139, 101, 132, 114, 123, 123, 116, 136, 88, 146,
        43, 154, 0, 158 };
    private static readonly int[][] HYYSC2102Front = {
        new int[] { 110, 112, 115, 108, 121, 113, 126, 117, 131, 119,
            129, 126, 124, 127, 132, 134, 145, 137, 146, 145,
            135, 150, 115, 154, 91, 155, 83, 151, 84, 146,
            95, 140, 88, 137, 87, 132, 95, 130, 103, 133,
            109, 127, 102, 124, 102, 119 }
    };
    private static readonly int[] HYYSC1602 = { 190, 253, 202, 226,
        225, 206, 252, 191, 279, 181, 301, 178, 323, 179, 339, 183,
        354, 191, 393, 197, 422, 209, 448, 225, 465, 241,
        464, 250, 453, 258, 451, 268, 447, 281, 436, 293,
        422, 308, 399, 316, 372, 323, 341, 327, 306, 330,
        271, 329, 236, 325, 219, 317, 209, 303, 199, 283, 192, 266 };
    private static readonly int[][] HYYSC1602Front = {
        new int[] { 196, 293, 202, 286, 209, 289, 214, 296, 225, 297,
            228, 304, 236, 304, 242, 309, 240, 316, 232, 319,
            247, 321, 248, 326, 239, 330, 216, 332, 199, 327,
            192, 320, 184, 318, 182, 313, 190, 308, 201, 307,
            198, 302, 192, 300 }
    };

    internal static bool TryGet(int id, string path, out int[] polygon)
        => TryGet(id, path, out polygon, out _, out _);

    internal static bool TryGet(int id, string path, out int[] polygon,
        out int width, out int height)
    {
        polygon = null;
        width = height = 0;
        if (id == 0x182d012d && path != null &&
            path.EndsWith("/JZ_DXWG_0301.gso", StringComparison.OrdinalIgnoreCase))
        { polygon = StairRight; width = 221; height = 305; }
        else if (id == 0x182d0c1e && path != null &&
            path.EndsWith("/JZ_DXWG_3102.gso", StringComparison.OrdinalIgnoreCase))
        { polygon = StairLeft; width = 221; height = 306; }
        else if (id == 0x182d01f5 && path != null &&
            path.EndsWith("/JZ_DXWG_0501.gso", StringComparison.OrdinalIgnoreCase))
        { polygon = ArchLeft; width = 427; height = 452; }
        else if (id == 0x182d01f6 && path != null &&
            path.EndsWith("/JZ_DXWG_0502.gso", StringComparison.OrdinalIgnoreCase))
        { polygon = ArchRight; width = 427; height = 452; }
        else if (id == 0x172d125d && path != null &&
            path.EndsWith("/JZ_SWS_4701.gso", StringComparison.OrdinalIgnoreCase))
        { polygon = SWS4701; width = 401; height = 322; }
        else if (id == 0x062d0515 && path != null &&
            path.EndsWith("/JZ_DFYW_1301.gso", StringComparison.OrdinalIgnoreCase))
        { polygon = DFYW1301; width = 695; height = 531; }
        else if (id == 0x1c2d007c && path != null &&
            path.EndsWith("/JZ_JXMG_0124.gso", StringComparison.OrdinalIgnoreCase))
        { polygon = JXMG0124; width = 565; height = 507; }
        else if (id == 0x1c2d007d && path != null &&
            path.EndsWith("/JZ_JXMG_0125.gso", StringComparison.OrdinalIgnoreCase))
        { polygon = JXMG0125; width = 623; height = 505; }
        else if (id == 0x1c2d007e && path != null &&
            path.EndsWith("/JZ_JXMG_0126.gso", StringComparison.OrdinalIgnoreCase))
        { polygon = JXMG0126; width = 549; height = 439; }
        else if (id == 0x1c2d007f && path != null &&
            path.EndsWith("/JZ_JXMG_0127.gso", StringComparison.OrdinalIgnoreCase))
        { polygon = JXMG0127; width = 467; height = 437; }
        else if (id == 0x0c2d0961 && path != null &&
            path.EndsWith("/JZ_HJ_2401.gso", StringComparison.OrdinalIgnoreCase))
        { polygon = HJ2401; width = 615; height = 530; }
        else if (id == 0x1f2d0835 && path != null &&
            path.EndsWith("/JZ_HYYSC_2101.gso", StringComparison.OrdinalIgnoreCase))
        { polygon = HYYSC2101; width = 477; height = 457; }
        else if (id == 0x1f2d0836 && path != null &&
            path.EndsWith("/JZ_HYYSC_2102.gso", StringComparison.OrdinalIgnoreCase))
        { polygon = HYYSC2102; width = 463; height = 528; }
        else if (id == 0x1f2d0642 && path != null &&
            path.EndsWith("/JZ_HYYSC_1602.gso", StringComparison.OrdinalIgnoreCase))
        { polygon = HYYSC1602; width = 777; height = 534; }
        return polygon != null;
    }

    internal static int[][] PreservedFront(int[] polygon)
    {
        if (ReferenceEquals(polygon, SWS4701)) return SWS4701Front;
        if (ReferenceEquals(polygon, DFYW1301)) return DFYW1301Front;
        if (ReferenceEquals(polygon, JXMG0124)) return JXMG0124Front;
        if (ReferenceEquals(polygon, JXMG0125)) return JXMG0125Front;
        if (ReferenceEquals(polygon, JXMG0126)) return JXMG0126Front;
        if (ReferenceEquals(polygon, JXMG0127)) return JXMG0127Front;
        if (ReferenceEquals(polygon, HYYSC2102)) return HYYSC2102Front;
        if (ReferenceEquals(polygon, HYYSC1602)) return HYYSC1602Front;
        return Array.Empty<int[]>();
    }

    // These foreground teeth/braziers span footprint columns on both sides of
    // the paired portal depth. Keep each explicitly traced silhouette whole;
    // only these pixels may be placed immediately ahead of the portal.
    internal static bool ContinuousForeground(int id) =>
        id == 0x1c2d007c || id == 0x1c2d007d || id == 0x1c2d007e || id == 0x1c2d007f;

    // Only observed portal model identities/dimensions are eligible as anchors.
    // Decorative DH animations and different model variants cannot pair.
    internal static bool IsPortal(int id, string path, int width, int height, int clipBy)
    {
        if (path == null) return false;
        string asset; int expectedWidth, expectedHeight, expectedFoot;
        switch (id)
        {
            case PortalId: asset = "DH_FY_1501"; expectedWidth = 109; expectedHeight = 205; expectedFoot = 179; break;
            case 0x013000c9: asset = "DH_FY_0201"; expectedWidth = 192; expectedHeight = 177; expectedFoot = 176; break;
            case 0x013006a5: asset = "DH_FY_1701"; expectedWidth = 219; expectedHeight = 282; expectedFoot = 246; break;
            case 0x01300709: asset = "DH_FY_1801"; expectedWidth = 171; expectedHeight = 182; expectedFoot = 159; break;
            case 0x0130076d: asset = "DH_FY_1901"; expectedWidth = 145; expectedHeight = 156; expectedFoot = 136; break;
            default: return false;
        }
        return width == expectedWidth && height == expectedHeight && clipBy == expectedFoot &&
            path.EndsWith("/" + asset + ".gso", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool CanPair(int doorId, int portalId)
    {
        switch (doorId)
        {
            case 0x182d012d: case 0x182d0c1e: case 0x182d01f5: case 0x182d01f6:
                return portalId == PortalId;
            case 0x172d125d:
                return portalId == 0x013000c9 || portalId == 0x01300709 || portalId == 0x0130076d;
            case 0x062d0515: case 0x1c2d007c: case 0x1c2d007d: case 0x1c2d007e: case 0x1c2d007f: case 0x0c2d0961:
            case 0x1f2d0835: case 0x1f2d0836: case 0x1f2d0642:
                return portalId == 0x013006a5;
            default: return false;
        }
    }

    internal static bool IsSupportSurface(int id) =>
        id == 0x1f2d0835 || id == 0x1f2d0836 || id == 0x1f2d0642;

    internal static bool AcceptsAnchor(int id, int[] polygon, int x, int y,
        int width, int height)
    {
        if (IsSupportSurface(id))
        {
            // 2101/2102 are authored adjoining halves of one upper face. A
            // 219px-wide FY1701 centered just left of 2102 can overlap its
            // left edge. Bound that support continuation to half the portal
            // width and the actual shared face height; never an entire image.
            if (id == 0x1f2d0836 && x >= -109 && x < 0 && y >= 0 && y < 158)
                return true;
            if (x < 0 || x >= width || y < 0 || y >= height) return false;
            // Unlike an arch, a support-surface anchor must lie inside an
            // actual upper-face interval, not merely below its upper edge.
            var crossings = new List<float>();
            ColumnIntersections(polygon, x, crossings);
            for (int i = 0; i + 1 < crossings.Count; i += 2)
                if (y >= crossings[i] && y < crossings[i + 1]) return true;
            return false;
        }
        return x >= 0 && x < width && y >= 0 && y < height &&
            Column(polygon, x, out float top, out _) && y >= top;
    }

    // Half-open edge samples, sorted into even/odd interior intervals. A rear
    // plane or preserved silhouette may be concave and cross a column four or
    // more times; its gaps must never become lowered rear pixels.
    internal static void ColumnIntersections(int[] polygon, float x, List<float> intersections)
    {
        intersections.Clear();
        if (polygon == null) return;
        int count = polygon.Length / 2;
        for (int i = 0; i < count; i++)
        {
            int j = (i + 1) % count;
            float ax = polygon[i * 2], bx = polygon[j * 2];
            if (x < Math.Min(ax, bx) || x >= Math.Max(ax, bx)) continue;
            intersections.Add(polygon[i * 2 + 1] + (x - ax) *
                (polygon[j * 2 + 1] - polygon[i * 2 + 1]) / (bx - ax));
        }
        intersections.Sort();
    }

    // Envelope used only to locate an anchor below the doorway's upper edge.
    // Mesh generation uses every interior interval, never this min/max envelope.
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
