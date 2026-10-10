using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerAvatar
{
    private static void ApplyBudgetScale()
    {
        float scale = ArtDetail.BudgetScale;
        _spriteBudget.BudgetBytes = (long)(SpriteBudgetBase * scale);
        _fileBudget.BudgetBytes = (long)(FileBudgetBase * scale);
    }

    private static byte[] ReadPkg(string path)
    {
        byte[] bytes;
        if (_fileCache.TryGetValue(path, out bytes))
        {
            _fileBudget.Touch(path);
            return bytes;
        }
        ResLoader loader = ResLoader.I;
        if (loader == null) return null;
        bytes = loader.ReadRes24First(path);
        if (bytes == null) return null;
        _fileCache[path] = bytes;
        _fileBudget.Note(path, bytes.Length);
        _fileBudget.EvictAtSafePoint(k => _fileCache.Remove(k));
        return bytes;
    }

    private static bool HasSprite(string key)
    {
        Sprite sprite;
        return key != null && _spriteCache.TryGetValue(key, out sprite) && sprite != null;
    }

    private static Pose MakePose(List<Layer> layers, float ppu)
    {
        LayerInst[] result = new LayerInst[layers.Count];
        for (int i = 0; i < layers.Count; i++)
        {
            Layer layer = layers[i];
            result[i] = new LayerInst
            {
                spr = MakeLayerSprite(layer, ppu),
                offX = (float)(layer.ox + layer.ax) / ppu,
                offY = (float)(layer.oy + layer.ay) / ppu,
                tag = layer.tag,
                additive = layer.additive
            };
        }
        return new Pose { layers = result };
    }

    private static Sprite MakeLayerSprite(Layer L, float ppu)
    {
        Sprite sprite;
        if (L.key != null && _spriteCache.TryGetValue(L.key, out sprite) && sprite != null)
        {
            _spriteBudget.Touch(L.key);
            return sprite;
        }
        if (L.w <= 0) return null;
        if (L.key != null && L.h > 0 && L.px == null && L.px16 == null &&
            !_warnedFrames.ContainsKey("NOPX:" + L.key))
        {
            _warnedFrames["NOPX:" + L.key] = true;
            Debug.LogWarning("[Avatar] 层 " + L.key +
                " 只解了几何却要建精灵(缓存判据不一致) —— 该层这次不画, 请把这行发出来。");
        }
        if (L.h <= 0 || (L.px == null && L.px16 == null)) return null;

        Texture2D texture;
        if (L.px16 != null)
        {
            texture = new Texture2D(L.w, L.h, TextureFormat.ARGB4444, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            NativeArray<byte> data = texture.GetRawTextureData<byte>();
            for (int y = 0; y < L.h; y++)
                NativeArray<byte>.Copy(L.px16, (L.h - 1 - y) * L.w * 2, data, y * L.w * 2, L.w * 2);
        }
        else
        {
            texture = new Texture2D(L.w, L.h, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            NativeArray<Color32> data = texture.GetRawTextureData<Color32>();
            for (int y = 0; y < L.h; y++)
                NativeArray<Color32>.Copy(L.px, (L.h - 1 - y) * L.w, data, y * L.w, L.w);
        }
        texture.Apply(false, false);
        int scale = System.Math.Max(1, L.scale);
        sprite = Sprite.Create(texture, new Rect(0f, 0f, L.w, L.h),
            new Vector2((float)L.ax * scale / L.w, ((float)L.h - (float)L.ay * scale) / L.h), ppu * scale);
        if (L.key != null)
        {
            sprite.name = L.key;
            _spriteCache[L.key] = sprite;
            _spriteBudget.Note(L.key, (long)L.w * L.h * (L.px16 != null ? 2 : 4));
        }
        return sprite;
    }

    private static void AddPoseKeys(Pose p)
    {
        if (p == null || p.layers == null) return;
        foreach (LayerInst layer in p.layers)
        {
            if (layer.spr != null && layer.spr.name != null) _inUseKeys.Add(layer.spr.name);
        }
    }

    private void CollectInUseKeys()
    {
        if (stand != null) foreach (Pose p in stand) AddPoseKeys(p);
        if (standAnim != null)
            foreach (Pose[] direction in standAnim)
                if (direction != null) foreach (Pose p in direction) AddPoseKeys(p);
        if (walk != null) foreach (Pose p in walk) AddPoseKeys(p);
        if (sit != null) foreach (Pose p in sit) AddPoseKeys(p);
        if (ride != null) foreach (Pose p in ride) AddPoseKeys(p);
        if (_mag != null) foreach (Pose p in _mag) AddPoseKeys(p);
        foreach (Pose[,] frames in _atk)
            if (frames != null) foreach (Pose p in frames) AddPoseKeys(p);
        if (_poseCache != null)
            foreach (KeyValuePair<int, Pose[]> pair in _poseCache)
                if (pair.Value != null) foreach (Pose p in pair.Value) AddPoseKeys(p);
        if (_poseFramesCache != null)
            foreach (KeyValuePair<int, Pose[,]> pair in _poseFramesCache)
                if (pair.Value != null) foreach (Pose p in pair.Value) AddPoseKeys(p);
    }

    public static int EvictSpriteCacheAtSafePoint()
    {
        _inUseKeys.Clear();
        for (int i = _liveAvatars.Count - 1; i >= 0; i--)
        {
            PlayerAvatar avatar = _liveAvatars[i].Target as PlayerAvatar;
            if (avatar == null) _liveAvatars.RemoveAt(i);
            else avatar.CollectInUseKeys();
        }
        return _spriteBudget.EvictAtSafePoint(k =>
        {
            Sprite sprite;
            if (_spriteCache.TryGetValue(k, out sprite) && sprite != null)
            {
                if (sprite.texture != null) UnityEngine.Object.Destroy(sprite.texture);
                UnityEngine.Object.Destroy(sprite);
            }
            _spriteCache.Remove(k);
        }, k => !_inUseKeys.Contains(k));
    }

    public static void ClearSpriteCache()
    {
        foreach (Sprite sprite in _spriteCache.Values)
        {
            if (sprite != null)
            {
                if (sprite.texture != null) UnityEngine.Object.Destroy(sprite.texture);
                UnityEngine.Object.Destroy(sprite);
            }
        }
        _spriteCache.Clear();
        _spriteBudget.Reset();
    }

    // Official 0x0600098d: total bytes currently accounted by the sprite-cache budget. Both
    // architectures load _spriteBudget (static offset 0x08) and return its Bytes field (0x38).
    public static long SpriteCacheBytes => _spriteBudget.Bytes;

    // Official 0x0600098f: whether the sprite-cache budget is currently exceeded; delegates to
    // the recovered ArtBudget.OverBudget (Bytes > BudgetBytes) on _spriteBudget.
    public static bool SpriteCacheOverBudget => _spriteBudget.OverBudget;

    // Official 0x0600098c: sprite-cache budget diagnostic line; delegates to ArtBudget.Stat().
    public static string SpriteCacheStat() => _spriteBudget.Stat();

    // Official 0x060009a6: file-cache budget diagnostic line; the same ArtBudget.Stat() on _fileBudget.
    public static string FileCacheStat() => _fileBudget.Stat();

    // Official 0x06000994: live-avatar diagnostic line. The frozen metadata literal (ordinal 17746,
    // state exact) is "活avatar {0}(上次淘汰时占用key {1}/挡下{2}条)"; both architectures format
    // _liveAvatars.Count, _inUseKeys.Count and _spriteBudget.PinnedLastRun.
    public static string LiveAvatarStat()
    {
        return string.Format("活avatar {0}(上次淘汰时占用key {1}/挡下{2}条)",
            _liveAvatars.Count, _inUseKeys.Count, _spriteBudget.PinnedLastRun);
    }

    // Official 0x0600098e: hand every live cached sprite's texture to the sink for ArtCaches
    // eviction accounting. The native body reads _spriteCache.Values (get_Values), enumerates it,
    // skips null sprites via Object.op_Inequality and invokes the Action with each sprite texture,
    // matching the recovered ClearSpriteCache iteration idiom.
    public static void CollectTextures(Action<Texture2D> sink)
    {
        foreach (Sprite sprite in _spriteCache.Values)
            if (sprite != null) sink(sprite.texture);
    }

    public void Dispose()
    {
        if (_stillCache != null)
            foreach (KeyValuePair<int, Sprite> pair in _stillCache)
                if (pair.Value != null)
                {
                    if (pair.Value.texture != null) UnityEngine.Object.Destroy(pair.Value.texture);
                    UnityEngine.Object.Destroy(pair.Value);
                }
        stand = null;
        standAnim = null;
        walk = null;
        sit = null;
        ride = null;
        _mag = null;
        for (int i = 0; i < 3; i++)
        {
            _atk[i] = null;
            _atkBuilt[i] = false;
        }
        _magBuilt = false;
        _poseCache = null;
        _poseFramesCache = null;
        _stillCache = null;
        _headFr = null;
        ok = false;
    }
}

}
