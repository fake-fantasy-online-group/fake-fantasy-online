using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class MountAvatar
{
    public const int SortBoostCapPx = 64;
    private static readonly Dictionary<int, Data> cache = new Dictionary<int, Data>();
    private static readonly Data empty = new Data();
    private const long BudgetBase = 469762048L;
    private static long BudgetBytes = BudgetBase;
    private static bool _budgetHooked;
    private const float IdleEvictSec = 180f;
    private const int MaxUploadBytesPerFrame = 4194304;
    private const int MaxTexPerFrame = 10;
    private static int _mountUploadedThisFrame;
    private const int MaxPendingBytes = 16777216;
    private const float HighWaterFrac = 0.45f;
    private static readonly HashSet<int> _texFailWarned = new HashSet<int>();
    private static readonly HashSet<int> _missWarned = new HashSet<int>();
    private static readonly List<Data> evictBuf = new List<Data>();
    private static Driver driver;

    private static void EnsureBudgetHook()
    {
        if (_budgetHooked) return;
        _budgetHooked = true;
        ArtDetail.OnBudgetScaleChanged += () =>
        {
            BudgetBytes = (long)(BudgetBase * ArtDetail.BudgetScale);
        };
        BudgetBytes = (long)(BudgetBase * ArtDetail.BudgetScale);
    }

    public static Data Acquire(int model, float ppu, bool mine)
    {
        EnsureBudgetHook();
        if (model < 1) return empty;
        EnsureDriver();
        int key = unchecked(model * 1000 + Mathf.Clamp(Mathf.RoundToInt(ppu), 0, 999));
        if (mine && cache.TryGetValue(key, out Data existing) && existing != null) existing.mine = true;
        if (!cache.TryGetValue(key, out Data d) || d == null)
        {
            d = new Data { key = key, model = model, ppu = ppu, mine = mine };
            int standMs = NpcActions.Interval(model, "Stand");
            int walkMs = NpcActions.Interval(model, "Walk");
            d.standInterval = standMs > 0 ? standMs * 0.5f / 1000f : 0.07f;
            d.walkInterval = walkMs > 0 ? walkMs * 0.5f / 1000f : 0.07f;
            cache[key] = d;
            StartJob(d);
        }
        d.refCount++;
        d.lastUse = Time.realtimeSinceStartup;
        return d;
    }

    public static void Release(Data d)
    {
        if (d == null || d.key == 0) return;
        d.refCount = Mathf.Max(d.refCount - 1, 0);
        d.lastUse = Time.realtimeSinceStartup;
    }

    public static long CacheBytes
    {
        get
        {
            long total = 0;
            foreach (var pair in cache) total += pair.Value.bytes;
            return total;
        }
    }

    public static void CollectTextures(Action<Texture2D> sink)
    {
        foreach (var pair in cache)
        {
            Data d = pair.Value;
            if (d == null) continue;
            Sprite[][,] grids = { d.stand, d.walk, d.standTop, d.walkTop, d.standFx, d.walkFx };
            foreach (Sprite[,] grid in grids)
            {
                if (grid == null) continue;
                foreach (Sprite sprite in grid)
                    if (sprite) sink(sprite.texture);
            }
        }
    }

    public static string CacheStat()
    {
        long total = 0;
        int warming = 0;
        foreach (var pair in cache)
        {
            total += pair.Value.bytes;
            if (pair.Value.Warming) warming++;
        }
        int low = 0, busy = 0;
        foreach (var pair in cache)
        {
            if (pair.Value.lowDetail) low++;
            if (pair.Value.refCount > 0) busy++;
        }
        return string.Format("坐骑缓存 {0} 只 / {1:F0}MB(预算{2:F0}",
            cache.Count, total / (1024f * 1024f), BudgetBytes / (1024f * 1024f)) +
            string.Format(" 占用中{0}只淘汰不掉", busy) +
            (low > 0 ? string.Format(" 降级{0}只", low) : string.Empty) +
            string.Format(") / 补帧中 {0}", warming);
    }

    private static void EvictIfNeeded(float now)
    {
        long total = CacheBytes;
        evictBuf.Clear();
        foreach (var pair in cache)
        {
            Data d = pair.Value;
            if (d.refCount <= 0 && d.job == null &&
                (total > BudgetBytes || now - d.lastUse > IdleEvictSec)) evictBuf.Add(d);
        }
        if (evictBuf.Count == 0) return;
        evictBuf.Sort((a, b) => a.lastUse.CompareTo(b.lastUse));
        foreach (Data d in evictBuf)
        {
            float idle = now - d.lastUse;
            if (total > BudgetBytes || idle > IdleEvictSec)
            {
                total -= d.bytes;
                cache.Remove(d.key);
                d.DisposeNow();
            }
        }
    }

    private static void EnsureDriver()
    {
        if (driver) return;
        GameObject go = new GameObject("MountAvatarCache");
        go.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DontDestroyOnLoad(go);
        driver = go.AddComponent<Driver>();
    }

    private class Driver : MonoBehaviour
    {
        private void Update() => Tick();
    }
}

}
