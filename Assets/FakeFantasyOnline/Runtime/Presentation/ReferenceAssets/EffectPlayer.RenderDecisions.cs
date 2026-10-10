using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official EffectPlayer renderer material-selection dependency.
/// </summary>
public partial class EffectPlayer : MonoBehaviour
{
    private static bool IsAdditiveIntensityMap(string name, Sprite[] fr)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        bool cached;
        if (darkMapCache.TryGetValue(name, out cached))
        {
            return cached;
        }

        bool result = false;
        Texture2D texture = null;
        if (fr != null && fr.Length > 0 && fr[0])
        {
            texture = fr[0].texture;
        }

        if (texture)
        {
            Color32[] pixels = texture.GetPixels32();
            int step = Math.Max(pixels.Length / 200000, 1);
            if (pixels.Length > 0)
            {
                int visible = 0;
                int dark = 0;
                for (int index = 0; index < pixels.Length; index += step)
                {
                    Color32 pixel = pixels[index];
                    if (pixel.a > 8)
                    {
                        visible++;
                        if (pixel.r < 25 && pixel.g < 25 && pixel.b < 25)
                        {
                            dark++;
                        }
                    }
                }

                result = visible > 199 && visible * 40 <= dark * 100;
            }
        }

        darkMapCache[name] = result;
        return result;
    }
}

}
