using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class NpcChatter
{
    // Official 1.5.8 token 0600091c.
    private static Texture2D MagicSymTex(int magicId)
    {
        if (symCache.TryGetValue(magicId, out Texture2D cached)) return cached;
        Texture2D texture = ResLoader.I != null
            ? ResLoader.I.LoadTex(string.Format("magic/magic/magic{0}.gso", magicId), 0)
            : null;
        if (texture != null)
        {
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
        }
        symCache[magicId] = texture;
        return texture;
    }

    // Official 1.5.8 token 0600091d.
    private static int SymMagicId(int a, int b)
    {
        return (a == 1 ? 5015 : 5018) + (b == 1 ? 0 : b == 2 ? 1 : 2);
    }
}

}
