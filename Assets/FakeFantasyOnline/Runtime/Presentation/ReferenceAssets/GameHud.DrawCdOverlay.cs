using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    // Official instance field +0x138. Textures remain owned by the HUD resources.
    private Texture2D[] cooldownFrames;

    internal void BindCooldownFramesForIndependentRuntime(IReadOnlyList<Texture2D> frames)
    {
        if (frames == null)
        {
            cooldownFrames = null;
            return;
        }
        if (frames is Texture2D[] array)
        {
            cooldownFrames = array;
            return;
        }
        cooldownFrames = new Texture2D[frames.Count];
        for (int i = 0; i < frames.Count; i++) cooldownFrames[i] = frames[i];
    }

    // Official 0x06000438. The shared renderer owns SkillCdFrac, the frame lookup and
    // GUI colour save/draw/restore. The native index clamps negative values to zero;
    // the arm64 sign mask is not an absolute-value operation.
    public static void DrawCdOverlay(Rect r, int id)
    {
        if (I == null) return;
        Texture2D[] frames = I.cooldownFrames;
        if (frames == null) return;
        DrawSkillCooldownForIndependentRuntime(r, id, frames);
    }
}
}
