using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class ReferenceLegacyMapObjectRenderer
{
    private readonly List<Texture2D> embeddedTextures = new List<Texture2D>();
    private readonly List<Sprite> embeddedSprites = new List<Sprite>();

    private void PlaceEmbeddedObjects(GsmMap sourceMap, Transform parent,
        float pixelsPerUnit, float worldWidth, float worldHeight)
    {
        int objectIndex = 0;
        foreach (GsmMap.FixObj item in sourceMap.fixObjs)
        {
            if (item.frameImgs == null || item.frameImgs.Length == 0) continue;
            GsmMap.FixFrame sourceFrame = item.frameImgs[0];
            GsmMap.NakedImg frame = sourceFrame.img;
            if (frame.data == null || frame.w <= 0 || frame.h <= 0) continue;
            Color32[] pixels = DimgDecoder.ToColors(frame.w, frame.h, frame.pitch, frame.attrib, frame.data);
            if (pixels == null) continue;
            Texture2D texture = new Texture2D(frame.w, frame.h, TextureFormat.RGBA32, false);
            embeddedTextures.Add(texture);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32[] bottomUp = new Color32[pixels.Length];
            for (int y = 0; y < frame.h; y++)
                Array.Copy(pixels, y * frame.w, bottomUp, (frame.h - 1 - y) * frame.w, frame.w);
            texture.SetPixels32(bottomUp);
            texture.Apply(false, false);
            int drawX = item.sx + sourceFrame.xOff;
            float oy = (worldHeight * 0.5f - (item.sy + sourceFrame.yOff)) / pixelsPerUnit;
            if (item.clipBy == null || item.clipBy.Length < 2 || item.clipWidth <= 0)
            {
                PlaceEmbeddedSprite(parent, pixelsPerUnit, worldWidth, texture, 0, frame.w, frame.h, drawX, oy,
                    item.sy + item.yl, objectIndex, false);
            }
            else
            {
                for (int i = 0; i < item.clipBy.Length; i++)
                {
                    int left = i * item.clipWidth;
                    int remaining = frame.w - left;
                    if (remaining <= 0) break;
                    int width = Mathf.Min(item.clipWidth, remaining);
                    PlaceEmbeddedSprite(parent, pixelsPerUnit, worldWidth, texture, left, width, frame.h, drawX + left, oy,
                        item.sy + item.clipBy[i], objectIndex, true);
                }
            }
            objectIndex++;
        }
    }

    private void PlaceEmbeddedSprite(Transform parent, float pixelsPerUnit, float worldWidth,
        Texture2D texture, int left, int width, int height,
        int drawX, float oy, int order, int objectIndex, bool sliced)
    {
        Sprite sprite = Sprite.Create(texture, new Rect(left, 0, width, height), new Vector2(0, 1), pixelsPerUnit);
        embeddedSprites.Add(sprite);
        if (sliced) _ = string.Format("fixobj{0}_c{1}", objectIndex, left);
        else _ = string.Format("fixobj{0}", objectIndex);
        GameObject go = new GameObject();
        go.transform.parent = parent;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderers.Add(renderer);
        renderer.sprite = sprite;
        Material objectMaterial = GameBootstrap.IsoYObjMat();
        if (objectMaterial != null) renderer.sharedMaterial = objectMaterial;
        go.transform.position = new Vector3((drawX - worldWidth * 0.5f) / pixelsPerUnit, oy, 0);
        renderer.sortingOrder = order;
    }
}

}
