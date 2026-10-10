using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    // Original 06001048. The owner cache retains null misses as well as decoded textures.
    private Texture2D ItemIcon(int id)
    {
        if (itemIconCache.TryGetValue(id, out Texture2D cached)) return cached;
        Texture2D texture = null;
        if (ResLoader.I != null)
        {
            byte[] data = ResLoader.I.ReadRes(string.Format("item/item/item{0}.gso", id));
            if (data != null)
            {
                DimgDecoder.Result decoded = DimgDecoder.Decode(data, 0);
                if (decoded.ok)
                {
                    texture = decoded.tex;
                    TexScale.Register(texture, decoded.scale);
                }
            }
        }
        itemIconCache[id] = texture;
        return texture;
    }
}
}
