using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class EffectPlayer
{
    // Official 1.5.8 token 060002a2.
    private static void DropUnusedFrames(DimgDecoder.Anim animation, int keepFrom, int keepCount)
    {
        if (animation.f == null) return;
        for (int i = 0; i < animation.f.Length; i++)
        {
            if (i < keepFrom || i >= keepFrom + keepCount)
            {
                Texture2D texture = animation.f[i].tex;
                if (texture != null) UnityEngine.Object.Destroy(texture);
            }
        }
    }
}

}
