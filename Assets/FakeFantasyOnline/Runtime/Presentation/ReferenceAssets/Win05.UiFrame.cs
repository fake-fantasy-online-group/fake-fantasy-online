using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class Win05
{
    // Original UiFrameAll 06001023. Populate through UiFrame even on a cache miss,
    // then return the cached array itself, including a cached null result.
    private Texture2D[] UiFrameAll(string path)
    {
        UiFrame(path, 0);
        uiFrameCache.TryGetValue(path, out Texture2D[] textures);
        return textures;
    }

    // Original UiFrame 06001050.
    private Texture2D UiFrame(string path, int frame)
    {
        if (!uiFrameCache.TryGetValue(path, out Texture2D[] textures))
        {
            textures = null;
            byte[] data = ResLoader.I?.Pkg?.Read(path) ?? ResLoader.I?.Pkg24?.Read(path);
            if (data != null)
            {
                DimgDecoder.Anim animation = DimgDecoder.DecodeAnim(data);
                if (animation.ok && animation.f != null)
                {
                    textures = new Texture2D[animation.f.Length];
                    for (int i = 0; i < textures.Length; i++)
                    {
                        textures[i] = animation.f[i].tex;
                        TexScale.Register(textures[i], animation.scale);
                    }
                }
            }
            uiFrameCache[path] = textures;
        }
        return textures == null || textures.Length == 0 ? null : textures[Mathf.Clamp(frame, 0, textures.Length - 1)];
    }
}

}
