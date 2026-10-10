using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class EffectPlayer
{
    // Official 0x0600029d (EffectPlayer..cctor) builds this with exactly one entry, the level-up
    // effect (usage 17152 = "升级特效"), and assigns static +0x78 after PlaneOverAllow (+0x70) and
    // before LastGlowDiag (+0x80): it is a static-constructor statement, not a field initializer,
    // because a field initializer would run before every statement of the explicit cctor.
    // PrewarmCommon walks it on start-up so the very first level-up does not decode its sprites
    // while the effect is already playing.
    private static readonly string[] PrewarmList;

    // Official 0x060002ba. Reads PrewarmList (static +0x78) once and prewarms every entry; a null
    // list throws before the loop, which is why the static constructor assigns a non-null array
    // instead of filling the field lazily.
    public static void PrewarmCommon()
    {
        foreach (string effName in PrewarmList)
        {
            Prewarm(effName);
        }
    }

    // Official 0x060002bb. For every layer of the template: pin the layer sprite name so the
    // budget never evicts it, then either touch the already decoded sprite strip or drop the
    // stale entry and decode it again.
    public static void Prewarm(string effName)
    {
        Template template = LoadTemplate(effName);
        if (template == null)
        {
            return;
        }

        foreach (Layer layer in template.layers)
        {
            string name = layer.sprite;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            _fxPinned.Add(name);
            Sprite[] cached;
            if (spriteCache.TryGetValue(name, out cached) && cached != null && cached.Length > 0
                && cached[0] != null)
            {
                _fxBudget.Touch(name);
                continue;
            }

            spriteCache.Remove(name);
            LoadMagicFrames(name, 0f);
        }
    }

    // Official 0x060002a0. Hands every decoded sprite texture to the caller; EvictAtSafePoint uses
    // it through the same delegate shape to collect the textures that are still on screen. A null
    // sink throws after the texture has already been fetched, which is the C# argument-then-invoke
    // order the official body emits.
    public static void CollectTextures(Action<Texture2D> sink)
    {
        foreach (Sprite[] cached in spriteCache.Values)
        {
            if (cached == null)
            {
                continue;
            }

            foreach (Sprite sprite in cached)
            {
                if (sprite == null)
                {
                    continue;
                }

                sink(sprite.texture);
            }
        }
    }
}

}
