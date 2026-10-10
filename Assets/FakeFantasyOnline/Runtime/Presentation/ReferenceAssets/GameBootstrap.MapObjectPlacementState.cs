using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    // Original PlaceMapObjects/PlaceObj state. Resets belong to admitted object
    // placement, not BuildWorld entry, and are independent of LoadTrace.On.
    internal static int ObjN, ObjSliceN, ObjAnimN, ObjPlainN;
    internal static float ObjLoadMs, ObjSliceMs, ObjSpawnMs, ObjFadeMs;

    internal static void ResetMapObjectPlacementMetricsForIndependentRuntime()
    {
        ObjLoadMs = ObjSliceMs = ObjSpawnMs = ObjFadeMs = 0f;
        ObjN = ObjSliceN = ObjAnimN = ObjPlainN = 0;
    }

    // The ordinary caller supplies its original per-object group and order. The
    // existing recovered trim/body owner remains the sole fade policy.
    internal void RegisterMapObjectFadeForIndependentRuntime(List<SpriteRenderer> group,
        int drawX, float oy, ResLoader.ObjSprite image, int imageId, float worldW, float ppu, int order) =>
        RegisterFadeObjTrim(group, drawX, oy, image.tex, imageId, worldW, ppu, order, image.hdScale);

    internal static StringBuilder BeginMapObjectSeamForIndependentRuntime(
        int id, int sx, int sy, int drawX, float worldW, float ppu, ResLoader.ObjSprite image)
    {
        if (!DebugSeam || _seamDumpCount >= 3) return null;
        var text = new StringBuilder();
        text.Append(string.Format(
            "[SEAM] obj id={0} sx={1} sy={2} drawX={3} worldW={4} worldW/2 frac={5:0.###} ppu={6} hdScale={7} tex={8}x{9} clipW={10} clipN={11}\n",
            id, sx, sy, drawX, worldW, worldW * 0.5f - Mathf.Floor(worldW * 0.5f),
            ppu, image.hdScale, image.tex.width, image.tex.height, image.clipWidth, image.clipArr.Length));
        return text;
    }

    internal static void AppendMapObjectSeamForIndependentRuntime(StringBuilder text,
        Sprite sprite, SpriteRenderer renderer, int xOffset, int ground, float wx, float ppu, int hdScale)
    {
        float phase = wx * ppu;
        float rounded = Mathf.Round(phase);
        float width = sprite.rect.width;
        text.Append(string.Format(
            "  strip xOff={0} rectW={1} wx={2:0.####} texelPhase={3:0.###} rightWx={4:0.####} clipBy={5} SO={6} mat={7} localScale={8:0.###}\n",
            xOffset, sprite.rect.width, wx, phase - rounded,
            wx + width / (ppu * Mathf.Max(hdScale, 1)), ground, renderer.sortingOrder,
            renderer.sharedMaterial != null ? renderer.sharedMaterial.shader.name : "<null>",
            renderer.gameObject.transform.localScale.x));
    }

    internal static void EndMapObjectSeamForIndependentRuntime(StringBuilder text)
    {
        // The shipping body builds the bounded text and increments this counter;
        // there is no reachable Debug.Log/LoadTrace.Line emission here.
        if (text != null) _seamDumpCount++;
    }
}

}
