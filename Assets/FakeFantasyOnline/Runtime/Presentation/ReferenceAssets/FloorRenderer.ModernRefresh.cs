using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class FloorRenderer
{
    public ReferenceGsmMap gm;
    public Camera cam;
    private int lastMinA = int.MaxValue;
    private int lastMaxA, lastMinB, lastMaxB;

    // Original 06000baf; x/y bounds are map-pixel coordinates.
    private void RefreshV27(float x0, float x1, float y0, float y1, bool force)
    {
        int width = gm.GridWidth;
        int height = gm.GridHeight;
        int minA = Mathf.FloorToInt(x0 / 128f) - 1;
        int maxA = Mathf.CeilToInt(x1 / 128f) + 1;
        int minB = Mathf.FloorToInt(y0 / 128f) - 1;
        int maxB = Mathf.CeilToInt(y1 / 128f) + 1;
        if (!force && minA == lastMinA && maxA == lastMaxA && minB == lastMinB && maxB == lastMaxB) return;
        lastMinA = minA;
        lastMaxA = maxA;
        lastMinB = minB;
        lastMaxB = maxB;
        visibleNow.Clear();
        for (int b = Mathf.Max(0, minB); b <= Mathf.Min(height - 2, maxB); b++)
            for (int a = Mathf.Max(0, minA); a <= Mathf.Min(width - 2, maxA); a++)
            {
                int lb = gm.VCode(a, b + 1);
                int lt = gm.VCode(a, b);
                int rt = gm.VCode(a + 1, b);
                int rb = gm.VCode(a + 1, b + 1);
                if ((lb & lt & rt & rb) < 0) continue;
                long id = ((long)a << 32) | (uint)b;
                string name = AnimNameOf(lb, lt, rt, rb);
                if (name != null)
                {
                    AnimTil animation = GetAnimTil(name);
                    int frame = (int)(Time.time * 1000f / animation.frameMs) % animation.frameNum;
                    Sprite animated = GetV27Cell(lb, lt, rt, rb, name, frame);
                    if (animated != null)
                    {
                        visibleNow.Add(id);
                        if (!active.ContainsKey(id)) Place(id, animated, a * 128, b * 128);
                        v27Anim[id] = new AnimCellV27 { lb = lb, lt = lt, rt = rt, rb = rb, name = name };
                        continue;
                    }
                }
                Sprite sprite = GetV27Cell(lb, lt, rt, rb, null, 0);
                if (sprite == null) continue;
                visibleNow.Add(id);
                if (!active.ContainsKey(id)) Place(id, sprite, a * 128, b * 128);
            }
        Cull();
    }
}

}
