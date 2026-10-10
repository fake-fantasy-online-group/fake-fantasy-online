using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    // Original BuildFieldColor 060003e4.
    internal void BuildFieldColor(GsmMap gm, Transform parent, float ppu, float worldW, float worldH)
    {
        int width = gm.fcW + 1, height = gm.fcH + 1;
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                uint color = gm.fcColors[y * width + x];
                pixels[(height - 1 - y) * width + x] = new Color32(
                    (byte)color, (byte)(color >> 8), (byte)(color >> 16), (byte)(color >> 24));
            }
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        var go = new GameObject("FieldColor");
        go.transform.parent = parent;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        renderer.sortingOrder = -9999;
        float pixelWidth = gm.fcW * gm.fcTileW;
        float pixelHeight = gm.fcH * gm.fcTileH;
        go.transform.position = new Vector3(
            (pixelWidth * 0.5f + gm.fcSx - worldW * 0.5f) / ppu,
            (worldH * 0.5f - (pixelHeight * 0.5f + gm.fcSy)) / ppu, 0);
        Vector3 size = renderer.sprite.bounds.size;
        go.transform.localScale = new Vector3((pixelWidth / ppu) / size.x, (pixelHeight / ppu) / size.y, 1);
    }
}

}
