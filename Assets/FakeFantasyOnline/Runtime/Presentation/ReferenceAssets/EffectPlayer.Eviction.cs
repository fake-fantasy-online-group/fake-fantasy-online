using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class EffectPlayer
{
    // Official 1.5.8 token 060002a3 and its original drop/predicate callbacks.
    public static int EvictAtSafePoint()
    {
        if (!_fxBudget.OverBudget) return 0;
        var inUse = new HashSet<Sprite>();
        foreach (EffectPlayer effect in UnityEngine.Object.FindObjectsByType<EffectPlayer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (effect != null && effect._live) effect.CollectBorrowed(inUse);
        }
        return _fxBudget.EvictAtSafePoint(key =>
        {
            if (!spriteCache.TryGetValue(key, out Sprite[] cached)) return;
            if (cached != null)
            {
                var textures = new HashSet<Texture2D>();
                foreach (Sprite sprite in cached)
                {
                    if (sprite == null) continue;
                    Texture2D texture = sprite.texture;
                    UnityEngine.Object.Destroy(sprite);
                    if (texture != null && textures.Add(texture)) UnityEngine.Object.Destroy(texture);
                }
            }
            spriteCache.Remove(key);
        }, key => CanEvictFx(key, inUse));
    }

    // Official 1.5.8 token 060002a4.
    private static bool CanEvictFx(string key, HashSet<Sprite> inUse)
    {
        if (_fxPinned.Contains(key)) return false;
        if (!spriteCache.TryGetValue(key, out Sprite[] cached) || cached == null) return false;
        foreach (Sprite sprite in cached)
            if (sprite != null && inUse.Contains(sprite)) return false;
        return true;
    }

    // Official 1.5.8 token 060002a5.
    private void CollectBorrowed(HashSet<Sprite> inUse)
    {
        if (frames != null)
            foreach (Sprite[] layer in frames)
                if (layer != null)
                    foreach (Sprite sprite in layer)
                        if (sprite != null) inUse.Add(sprite);
        if (dirFrames != null)
            foreach (Sprite[][] layer in dirFrames)
                if (layer != null)
                    foreach (Sprite[] direction in layer)
                        if (direction != null)
                            foreach (Sprite sprite in direction)
                                if (sprite != null) inUse.Add(sprite);
        if (_layerObjs != null)
            foreach (SpriteRenderer renderer in _layerObjs)
                if (renderer != null && renderer.sprite != null) inUse.Add(renderer.sprite);
    }
}

}
