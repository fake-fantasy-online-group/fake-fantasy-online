using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud : MonoBehaviour
{

    public static Texture2D BuffIcon(int iconId)
    {
        if (iconId < 1) return null;
        if (buffIcons.TryGetValue(iconId, out Texture2D texture)) return texture;
        texture = null;
        if (ResLoader.I)
        {
            byte[] data = ResLoader.I.ReadRes(string.Format("item/item/item{0}.gso", iconId));
            if (data != null)
            {
                DimgDecoder.Result result = DimgDecoder.Decode(data, 0);
                if (result.ok)
                {
                    texture = result.tex;
                    TexScale.Register(texture, result.scale);
                }
            }
        }
        buffIcons[iconId] = texture;
        return texture;
    }
}

}
