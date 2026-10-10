using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class FloorRenderer
{
    private struct AnimCellV27
    {
        public int lb, lt, rt, rb;
        public string name;
    }

    public float worldW, worldH;
    public int sortingOrder = -10000;
    private Material mat;
    private readonly Dictionary<long, SpriteRenderer> active = new Dictionary<long, SpriteRenderer>();
    private readonly Stack<SpriteRenderer> pool = new Stack<SpriteRenderer>();
    private readonly HashSet<long> visibleNow = new HashSet<long>();
    private readonly List<long> toRemove = new List<long>();
    private readonly Dictionary<long, int> animCode = new Dictionary<long, int>();
    private readonly Dictionary<long, AnimCellV27> v27Anim = new Dictionary<long, AnimCellV27>();

#if UNITY_EDITOR
    // Read-only projections retained for existing editor diagnostics.
    internal int ActiveCount => active.Count;
    internal int PooledCount => pool.Count;
    internal int CachedSpriteCount => spriteCache.Count;
#endif

    public static FloorRenderer Any { get; private set; }
    private void OnEnable() => Any = this;
    private void OnDisable()
    {
        if (ReferenceEquals(Any, this)) Any = null;
    }

    private SpriteRenderer NewSR()
    {
        GameObject go = new GameObject("cell");
        go.transform.parent = transform;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        if (mat != null) renderer.sharedMaterial = mat;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void Place(long cellId, Sprite sprite, int pxX, int pxY)
    {
        SpriteRenderer renderer = pool.Count > 0 ? pool.Pop() : NewSR();
        renderer.sprite = sprite;
        renderer.enabled = true;
        renderer.gameObject.SetActive(true);
        renderer.transform.localPosition = new Vector3((pxX - worldW * 0.5f) / ppu,
            (worldH * 0.5f - pxY) / ppu, 0);
        renderer.transform.localScale = Vector3.one;
        active[cellId] = renderer;
    }

    private void Cull()
    {
        toRemove.Clear();
        foreach (var pair in active)
            if (!visibleNow.Contains(pair.Key)) toRemove.Add(pair.Key);
        for (int i = 0; i < toRemove.Count; i++)
        {
            long id = toRemove[i];
            SpriteRenderer renderer = active[id];
            active.Remove(id);
            animCode.Remove(id);
            v27Anim.Remove(id);
            renderer.gameObject.SetActive(false);
            pool.Push(renderer);
        }
    }

    // Official 0x06000ba3: walk the recovered sprite cache and hand every live sprite's texture
    // to the supplied sink. ArtCaches.Tally (0x06000b67) drives this for eviction accounting; the
    // native body iterates spriteCache directly and invokes the Action on each non-null sprite's
    // texture, throwing NullReferenceException through the invoke when sink is null.
    public void CollectTextures(Action<Texture2D> sink)
    {
        foreach (var pair in spriteCache)
        {
            Sprite sprite = pair.Value;
            if (sprite) sink(sprite.texture);
        }
    }

    // Official 0x06000ba4: sum the RGBA byte size of every live cached sprite texture. Both
    // architectures iterate spriteCache, skip null sprites and null textures, and accumulate
    // (long)texture.width * texture.height * 4.
    public long TileArtBytes
    {
        get
        {
            long total = 0;
            foreach (var pair in spriteCache)
            {
                Sprite sprite = pair.Value;
                if (!sprite) continue;
                Texture2D texture = sprite.texture;
                if (!texture) continue;
                total += (long)texture.width * texture.height * 4;
            }

            return total;
        }
    }

    // Official 0x06000ba5: diagnostic line reporting the cached tile-art kind count and size in MB.
    // The frozen metadata literal is "地板瓦片 {0}种/{1:F1}MB"; the native body formats spriteCache.Count
    // and TileArtBytes scaled by 1/1048576 (the x86_64 export multiplies by the reciprocal constant).
    public string TileArtStat()
    {
        return string.Format("地板瓦片 {0}种/{1:F1}MB", spriteCache.Count, TileArtBytes / 1048576f);
    }
}

}
