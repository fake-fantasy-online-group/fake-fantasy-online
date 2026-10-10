using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    private class FadeObj
    {
        public SpriteRenderer[] srs;
        public Material[] orig;
        public float xMin, xMax, yBot, yTop;
        public int order;
        public float a;
    }


    // Original sliced/animated PlaceObj registration tail, preserving one record per object.
    internal void RegisterPlacedObjectFadeForIndependentRuntime(List<SpriteRenderer> renderers, int first,
        int drawX, float oy, ResLoader.ObjSprite image, int imageId, float worldW, float ppu)
    {
        var group = renderers.GetRange(first, renderers.Count - first);
        int order = int.MinValue;
        foreach (SpriteRenderer renderer in group)
            if (renderer.sortingOrder > order) order = renderer.sortingOrder;
        RegisterFadeObjTrim(group, drawX, oy, image.tex, imageId, worldW, ppu, order, image.hdScale);
    }

    // Official 1.5.8 token 060003c2.
    private void RegisterFadeObj(List<SpriteRenderer> srs, float xMin, float xMax,
        float yBot, float yTop, int order)
    {
        if (srs == null || srs.Count == 0) return;
        var item = new FadeObj
        {
            srs = srs.ToArray(), xMin = xMin, xMax = xMax,
            yBot = yBot, yTop = yTop, order = order, a = 1f
        };
        item.orig = new Material[item.srs.Length];
        for (int i = 0; i < item.srs.Length; i++)
            item.orig[i] = item.srs[i] != null ? item.srs[i].sharedMaterial : null;
        fadeRegs.Add(item);
    }

    // Official 1.5.8 token 060003c4.
    private void RegisterFadeObjTrim(List<SpriteRenderer> srs, int drawX, float oy,
        Texture2D tex, int imageId, float worldW, float ppu, int order, int hdScale = 1)
    {
        if (!EnableOccFade || tex == null) return;
        var bounds = OpaqueBounds(imageId, tex);
        float scale = Mathf.Max(hdScale, 1);
        RegisterFadeObj(srs,
            (bounds.Item1 / scale + drawX - worldW * 0.5f) / ppu,
            (bounds.Item2 / scale + drawX - worldW * 0.5f) / ppu,
            oy - (bounds.Item4 / scale) / ppu,
            oy - (bounds.Item3 / scale) / ppu, order);
    }
}

}
