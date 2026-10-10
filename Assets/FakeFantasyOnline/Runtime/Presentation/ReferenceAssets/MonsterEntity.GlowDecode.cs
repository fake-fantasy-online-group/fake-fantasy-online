using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official MonsterEntity glow decode that takes the decoded Anim directly, which is the
/// form 0600062c DecodeGrid passes: it calls DimgDecoder.DecodeAnim and hands the resulting Anim on.
/// </summary>
public partial class MonsterEntity
{
    private void DecodeGlowInto(int actId, DimgDecoder.Anim anim, Sprite[,] baseGrid)
    {
        if (baseGrid == null)
        {
            return;
        }

        if (_glowGridCache.ContainsKey(actId))
        {
            return;
        }

        if (friendly && !AnyPetGlowWanted)
        {
            return;
        }

        if (!(IsMyPet && !GameSettings.HideMyPetGlow) && GridCacheBytes > GlowHighWaterBytes)
        {
            return;
        }

        byte[] bytes = ReadNpcGsn(actId, "e", ArtDetail.PreferNativeArt);
        if (bytes == null)
        {
            _glowGridCache[actId] = null;
            return;
        }

        DimgDecoder.Anim glow = DimgDecoder.DecodeAnim(bytes);
        if (!glow.ok || glow.f == null)
        {
            _glowGridCache[actId] = null;
            return;
        }

        int directions = baseGrid.GetLength(0);
        int framesPerDirection = baseGrid.GetLength(1);
        int glowDirections = glow.dirs < 2 ? 1 : glow.dirs;
        int glowFramesPerDirection = glow.frames / glowDirections;
        if (glowFramesPerDirection < 2)
        {
            glowFramesPerDirection = 1;
        }

        Sprite[,] glowGrid = new Sprite[directions, framesPerDirection];
        for (int direction = 0; direction < directions; direction++)
        {
            for (int frameNumber = 0; frameNumber < framesPerDirection; frameNumber++)
            {
                int baseIndex = frameNumber + framesPerDirection * direction;
                int clampedDirection = glowDirections - 1 <= direction ? glowDirections - 1 : direction;
                int glowIndex = frameNumber % glowFramesPerDirection +
                    clampedDirection * glowFramesPerDirection;
                if (baseIndex >= anim.f.Length || glowIndex >= glow.f.Length)
                {
                    continue;
                }

                DimgDecoder.FrameTex baseFrame = anim.f[baseIndex];
                DimgDecoder.FrameTex glowFrame = glow.f[glowIndex];
                if (glowFrame.tex == null || (glowFrame.flags & 256) == 0)
                {
                    continue;
                }

                Sprite sprite = AnchorSpriteAt(
                    glowFrame.tex,
                    anim.boundCx,
                    anim.boundCy,
                    glowFrame.nLeft + baseFrame.nLeft,
                    glowFrame.nTop + baseFrame.nTop,
                    glow.scale,
                    ppu);
                glowGrid[direction, frameNumber] = sprite;
                if (baseGrid[direction, frameNumber] != null && sprite != null)
                {
                    _glowOf[baseGrid[direction, frameNumber]] = sprite;
                }
            }
        }

        _glowGridCache[actId] = glowGrid;
    }
}

}
