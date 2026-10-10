using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class NetClient
{
    private static Texture2D Desaturate(Texture2D source)
    {
        if (source == null) return null;
        try
        {
            Color32[] pixels = source.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 color = pixels[i];
                if (color.a == 0) continue;
                int gray = (color.r * 299 + color.g * 587 + color.b * 114) / 1000;
                byte value = (byte)Math.Min(255, gray * 88 / 100);
                pixels[i] = new Color32(value, value, value, color.a);
            }
            Texture2D result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            result.filterMode = source.filterMode;
            result.wrapMode = TextureWrapMode.Clamp;
            result.SetPixels32(pixels);
            result.Apply(false, false);
            TexScale.Register(result, TexScale.Of(source));
            return result;
        }
        catch
        {
            return source;
        }
    }
}

}
