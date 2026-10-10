using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{
    // Official GameHud.miniRing (+0x1d8). The only native slice that touches it, GameHud.OnGUI
    // (06000414, the sole caller of 0600042b), writes +0x1d8 while it is still null and never
    // destroys it: arm64 06000414.c:2811-2818, x86_64 06000414.c:3264-3271. One ring per HUD.
    private Texture2D miniRing;

    // 0x0600042b static UnityEngine.Texture2D GameHud.BuildFrameTex(System.Int32 W, System.Int32 H).
    // arm64 0x4382a8/668B and x86_64 0x44f210/1392B (queries/hud-drag-{arm64,x86_64}-20260910/
    // 0600042b.c). The three bands are the frozen 32-bit stores 0xff090a0c / 0xff2e3438 /
    // 0xff15181a decoded in Color32 memory order (r, g, b, a, low byte first), the same order the
    // already carried GameHud.BuildDisc packed colour uses (0xff101318 -> 0x18, 0x13, 0x10).
    public static Texture2D BuildFrameTex(int W, int H)
    {
        Color32[] pixels = new Color32[H * W];
        float halfWidth = (float)W * 0.5f;
        float halfHeight = (float)H * 0.5f;
        float spanWidth = halfWidth - 0.5f;
        float spanHeight = halfHeight - 0.5f;
        float inner = spanWidth;
        if (spanHeight <= spanWidth)
        {
            inner = spanHeight;
        }

        if (0 < H && 0 < W)
        {
            float band = (float)W / 132f;
            float outerLimit = band * 5f;
            float edgeLimit = band * 1.2f;
            float middleLimit = band * 4f;
            for (int y = 0; y < H; y++)
            {
                float ny = ((float)y + 0.5f - halfHeight) / spanHeight;
                for (int x = 0; x < W; x++)
                {
                    float nx = ((float)x + 0.5f - halfWidth) / spanWidth;
                    float distance = inner * (1f - Mathf.Sqrt(nx * nx + ny * ny));
                    // Official gate is two machine compares - "skip when distance < 0" and "skip
                    // when distance > outerLimit" (arm64 0600042b.c:74-84) - so an unordered value
                    // is painted and then falls through both band tests to the inner colour; with
                    // W = H = 276 no pixel reaches it.
                    if (distance < 0f || distance > outerLimit)
                    {
                        continue;
                    }

                    pixels[y * W + x] = distance < edgeLimit
                        ? new Color32(0x0c, 0x0a, 0x09, 0xff)
                        : (distance <= middleLimit
                            ? new Color32(0x38, 0x34, 0x2e, 0xff)
                            : new Color32(0x1a, 0x18, 0x15, 0xff));
                }
            }
        }

        Texture2D texture = new Texture2D(W, H, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    // 0x0600042c. This original outline builder has no established activation caller;
    // it is distinct from the frame, resource-backed masks and stamina gradient.
    private static Texture2D BuildRingTex(int W, int H, Color32 col)
    {
        Color32[] pixels = new Color32[H * W];
        if (0 < H)
        {
            float halfWidth = (float)W * 0.5f;
            float halfHeight = (float)H * 0.5f;
            float spanWidth = halfWidth - 1f;
            float spanHeight = halfHeight - 1f;
            float inner = spanWidth < spanHeight ? spanWidth : spanHeight;
            if (0 < W)
            {
                float thickness = 1.6f / inner;
                for (int y = 0; y < H; y++)
                {
                    float ny = ((float)y + 0.5f - halfHeight) / spanHeight;
                    float nySquared = ny * ny;
                    for (int x = 0; x < W; x++)
                    {
                        float nx = ((float)x + 0.5f - halfWidth) / spanWidth;
                        if (Mathf.Abs(Mathf.Sqrt(nySquared + nx * nx) - 1f) <= thickness)
                        {
                            pixels[y * W + x] = col;
                        }
                    }
                }
            }
        }

        Texture2D texture = new Texture2D(W, H, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    // The official lazy build of +0x1d8, at the official size (0x114 = 276 on both slices). The
    // texture stays owned by the HUD instance exactly as the official body leaves it: built on the
    // first frame that needs it, never rebuilt and never destroyed.
    internal Texture2D EnsureMiniRingForIndependentRuntime()
    {
        if (!miniRing)
        {
            miniRing = BuildFrameTex(276, 276);
        }

        return miniRing;
    }
}

}
