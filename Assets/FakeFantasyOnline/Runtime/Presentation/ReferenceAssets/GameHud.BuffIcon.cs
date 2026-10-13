using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud : MonoBehaviour
{

    public static Texture2D BuffIcon(int iconId)
    {
        if (iconId < 1) return null;
        if (buffIcons.TryGetValue(iconId, out Texture2D texture) && texture) return texture;
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
        // Resource initialization may lag behind the first status packet. Do not cache a
        // miss forever, or an otherwise valid icon stays blank after resources become ready.
        if (texture) buffIcons[iconId] = texture;
        return texture;
    }
}

}
