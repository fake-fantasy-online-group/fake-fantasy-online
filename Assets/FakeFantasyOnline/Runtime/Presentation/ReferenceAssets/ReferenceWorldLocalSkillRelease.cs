using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Full-table source projection of SkillEffectDB and SkillSoundDB for local release feedback.
/// </summary>
public sealed class ReferenceSkillReleaseCatalog : IDisposable
{
    public const string SkillShowRuntimePath = "tables/skillshow.json";
    public const string SkillSoundRuntimePath = "tables/skillsound.json";
    public const int ExpectedSkillShowRows = 675;
    public const int ExpectedSkillSoundRows = 243;
    private readonly Dictionary<int, ReferenceSkillReleasePlan> plans;
    private bool disposed;

    private ReferenceSkillReleaseCatalog(
        Dictionary<int, ReferenceSkillReleasePlan> plans,
        int skillShowRowCount,
        int skillSoundRowCount)
    {
        this.plans = plans;
        SkillShowRowCount = skillShowRowCount;
        SkillSoundRowCount = skillSoundRowCount;
        Ready = true;
    }

    public bool Ready { get; private set; }

    internal int SkillShowRowCount { get; private set; }

    internal int SkillSoundRowCount { get; private set; }

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        out ReferenceSkillReleaseCatalog owner)
    {
        owner = null;
        if (resources == null)
        {
            return false;
        }

        byte[] showBytes = resources.ReadSourceOwned(SkillShowRuntimePath);
        byte[] soundBytes = resources.ReadSourceOwned(SkillSoundRuntimePath);
        if (showBytes == null || showBytes.Length == 0 ||
            soundBytes == null || soundBytes.Length == 0)
        {
            return false;
        }

        SkillShowTable shows;
        SkillSoundTable sounds;
        try
        {
            shows = JsonUtility.FromJson<SkillShowTable>(
                Encoding.UTF8.GetString(showBytes));
            sounds = JsonUtility.FromJson<SkillSoundTable>(
                Encoding.UTF8.GetString(soundBytes));
        }
        catch (ArgumentException)
        {
            return false;
        }

        Dictionary<int, ReferenceSkillReleasePlan> parsed;
        if (!TryValidate(shows, sounds, out parsed))
        {
            return false;
        }

        owner = new ReferenceSkillReleaseCatalog(
            parsed,
            shows.shows.Length,
            sounds.sounds.Length);
        return true;
    }

    internal bool TryGetPlan(int skillId, out ReferenceSkillReleasePlan plan)
    {
        ThrowIfDisposed();
        if (!Ready)
        {
            plan = null;
            return false;
        }

        return plans.TryGetValue(skillId, out plan);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        plans.Clear();
        Ready = false;
        disposed = true;
    }

    private static bool TryValidate(
        SkillShowTable shows,
        SkillSoundTable sounds,
        out Dictionary<int, ReferenceSkillReleasePlan> plans)
    {
        plans = null;
        if (shows == null || shows.shows == null ||
            shows.shows.Length != ExpectedSkillShowRows ||
            sounds == null || sounds.sounds == null ||
            sounds.sounds.Length != ExpectedSkillSoundRows)
        {
            return false;
        }

        Dictionary<int, SkillShowRow> showById =
            new Dictionary<int, SkillShowRow>();
        for (int index = 0; index < shows.shows.Length; index++)
        {
            SkillShowRow row = shows.shows[index];
            if (row == null || row.id <= 0 ||
                (row.phy != 0 && row.phy != 1) ||
                showById.ContainsKey(row.id))
            {
                return false;
            }

            showById.Add(row.id, row);
        }

        Dictionary<int, SkillSoundRow> soundById =
            new Dictionary<int, SkillSoundRow>();
        for (int index = 0; index < sounds.sounds.Length; index++)
        {
            SkillSoundRow row = sounds.sounds[index];
            if (row == null || row.id <= 0 || row.cast < 0 || row.cd < 0 ||
                soundById.ContainsKey(row.id))
            {
                return false;
            }

            soundById.Add(row.id, row);
        }

        plans = new Dictionary<int, ReferenceSkillReleasePlan>();
        HashSet<int> skillIds = new HashSet<int>(showById.Keys);
        skillIds.UnionWith(soundById.Keys);
        foreach (int skillId in skillIds)
        {
            SkillShowRow show;
            SkillSoundRow sound;
            showById.TryGetValue(skillId, out show);
            soundById.TryGetValue(skillId, out sound);
            string effectName = string.Empty;
            int effectDelay = 0;
            if (show != null && show.ph != null)
            {
                for (int index = 0; index < show.ph.Length; index++)
                {
                    SkillShowPhase phase = show.ph[index];
                    if (phase == null || phase.e < 0 || phase.d < 0)
                    {
                        return false;
                    }
                    if (effectName.Length == 0 && phase.e > 0 && phase.s == 40)
                    {
                        effectName = string.Format(
                            System.Globalization.CultureInfo.InvariantCulture,
                            "TFX{0:D3}_{1}",
                            phase.e,
                            phase.s);
                        effectDelay = phase.d;
                    }
                }
            }

            int[] fireSounds = Array.Empty<int>();
            int[] fireDelays = Array.Empty<int>();
            if (sound != null)
            {
                if (sound.fire == null || sound.fd == null ||
                    sound.fire.Length != sound.fd.Length)
                {
                    return false;
                }

                fireSounds = (int[])sound.fire.Clone();
                fireDelays = (int[])sound.fd.Clone();
                for (int index = 0; index < fireSounds.Length; index++)
                {
                    if (fireSounds[index] < 0 || fireDelays[index] < 0)
                    {
                        return false;
                    }
                }
            }

            plans.Add(
                skillId,
                new ReferenceSkillReleasePlan(
                    skillId,
                    effectName,
                    effectDelay,
                    sound == null ? 0 : sound.cast,
                    sound == null ? 0 : sound.cd,
                    fireSounds,
                    fireDelays));
        }
        return true;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceSkillReleaseCatalog));
        }
    }

    [Serializable]
    private sealed class SkillShowTable
    {
        public SkillShowRow[] shows;
    }

    [Serializable]
    private sealed class SkillShowRow
    {
        public int id;
        public int phy;
        public SkillShowPhase[] ph;
    }

    [Serializable]
    private sealed class SkillShowPhase
    {
        public int s;
        public int e;
        public int d;
    }

    [Serializable]
    private sealed class SkillSoundTable
    {
        public SkillSoundRow[] sounds;
    }

    [Serializable]
    private sealed class SkillSoundRow
    {
        public int id;
        public int cast;
        public int cd;
        public int[] fire;
        public int[] fd;
    }
}

internal sealed class ReferenceSkillReleasePlan
{
    public ReferenceSkillReleasePlan(
        int skillId,
        string effectName,
        int effectDelayMilliseconds,
        int castSoundId,
        int castDelayMilliseconds,
        int[] fireSoundIds,
        int[] fireDelayMilliseconds)
    {
        SkillId = skillId;
        EffectName = effectName;
        EffectDelayMilliseconds = effectDelayMilliseconds;
        CastSoundId = castSoundId;
        CastDelayMilliseconds = castDelayMilliseconds;
        FireSoundIds = fireSoundIds ?? Array.Empty<int>();
        FireDelayMilliseconds = fireDelayMilliseconds ?? Array.Empty<int>();
    }

    public int SkillId { get; }
    public string EffectName { get; }
    public int EffectDelayMilliseconds { get; }
    public int CastSoundId { get; }
    public int CastDelayMilliseconds { get; }
    public IReadOnlyList<int> FireSoundIds { get; }
    public IReadOnlyList<int> FireDelayMilliseconds { get; }
    public int FireSoundId => FireSoundIds.Count == 0 ? 0 : FireSoundIds[0];
    public int FirstFireDelayMilliseconds =>
        FireDelayMilliseconds.Count == 0 ? 0 : FireDelayMilliseconds[0];
}

/// <summary>
/// Source-owned translation of SkillCaster.Cast, SkillSoundDB.PlayCast/PlayFire and EffectPlayer
/// runtime. The exact 10307 effect keeps its specialized sampled renderer; other table rows use
/// the generic EffectPlayer path.
/// </summary>
public sealed class ReferenceWorldLocalSkillRelease : IDisposable
{
    public const string BlessingEffectName = "TFX024_40";
    public const string BlessingEffectPath = "effect/tfx024_40.eff";
    public const int BlessingEffectLifeMilliseconds = 1670;
    public const int BlessingLayerCount = 4;
    public const float PixelsPerUnit = 100f;
    public const int FrontPlaneSortingOffset = 20;

    private const int Magic4026 = 4026;
    private const int Magic4049 = 4049;
    private const int Magic4051 = 4051;
    private const string EffectSha256 =
        "c95c48d72b8e5e9e8c8b4347d8c4b76121c9bbabdd4ae6e708c72bd9c37e1490";

    private readonly ReferenceSkillReleaseCatalog catalog;
    private readonly ReferenceWorldCombatSound sound;
    private readonly Dictionary<int, ReferenceMagicFrameSet> magicFrames;
    private readonly List<ReferenceActiveSkillEffect> activeEffects =
        new List<ReferenceActiveSkillEffect>();
    private Material alphaMaterial;
    private Material additiveMaterial;
    private bool disposed;

    private ReferenceWorldLocalSkillRelease(
        ReferenceSkillReleaseCatalog catalog,
        ReferenceWorldCombatSound sound,
        Dictionary<int, ReferenceMagicFrameSet> magicFrames,
        Material alphaMaterial,
        Material additiveMaterial)
    {
        this.catalog = catalog;
        this.sound = sound;
        this.magicFrames = magicFrames;
        this.alphaMaterial = alphaMaterial;
        this.additiveMaterial = additiveMaterial;
        Ready = true;
    }

    public bool Ready { get; private set; }

    public int ActiveEffectCount => activeEffects.Count;

    public int LastSkillId { get; private set; }

    public string LastEffectName { get; private set; } = string.Empty;

    public int LastCastSoundId { get; private set; }

    public int LastFireSoundId { get; private set; }

    internal ReferenceSkillReleaseCatalog Catalog => catalog;

    internal float LastEffectElapsedMilliseconds => activeEffects.Count == 0
        ? 0f
        : activeEffects[activeEffects.Count - 1].ElapsedMilliseconds;

    internal IReadOnlyList<SpriteRenderer> LastLayerRenderers =>
        activeEffects.Count == 0
            ? Array.Empty<SpriteRenderer>()
            : activeEffects[activeEffects.Count - 1].Renderers;

    internal int LastLayerFrameIndex(int layerIndex)
    {
        return activeEffects.Count == 0
            ? -1
            : activeEffects[activeEffects.Count - 1].FrameIndex(layerIndex);
    }

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        ReferenceWorldCombatSound sound,
        out ReferenceWorldLocalSkillRelease owner)
    {
        owner = null;
        if (resources == null || sound == null || !sound.Configured)
        {
            return false;
        }

        ReferenceSkillReleaseCatalog catalog;
        if (!ReferenceSkillReleaseCatalog.TryCreate(resources, out catalog) ||
            catalog == null || !catalog.Ready)
        {
            catalog?.Dispose();
            return false;
        }

        byte[] effect = resources.ReadRes(BlessingEffectPath);
        if (effect == null || effect.Length != 1159 ||
            !string.Equals(Sha256Hex(effect), EffectSha256, StringComparison.Ordinal))
        {
            catalog.Dispose();
            return false;
        }

        Dictionary<int, ReferenceMagicFrameSet> frames =
            new Dictionary<int, ReferenceMagicFrameSet>();
        Material alpha = null;
        Material additive = null;
        try
        {
            ReferenceMagicFrameSet magic4026 = null;
            ReferenceMagicFrameSet magic4049 = null;
            ReferenceMagicFrameSet magic4051 = null;
            if (!ReferenceMagicFrameSet.TryCreate(
                    resources,
                    Magic4026,
                    "magic/magic/magic4026.gso",
                    65540,
                    30,
                    out magic4026) ||
                !ReferenceMagicFrameSet.TryCreate(
                    resources,
                    Magic4049,
                    "magic/magic/magic4049.gso",
                    65537,
                    13,
                    out magic4049) ||
                !ReferenceMagicFrameSet.TryCreate(
                    resources,
                    Magic4051,
                    "magic/magic/magic4051.gso",
                    65537,
                    1,
                    out magic4051))
            {
                magic4026?.Dispose();
                magic4049?.Dispose();
                magic4051?.Dispose();
                catalog.Dispose();
                return false;
            }

            frames.Add(Magic4026, magic4026);
            frames.Add(Magic4049, magic4049);
            frames.Add(Magic4051, magic4051);

            alpha = EffectPlayer.BorrowAlphaMaterial();
            additive = EffectPlayer.BorrowAdditiveMaterial();
            if (alpha == null || additive == null)
            {
                DisposeFrames(frames);
                catalog.Dispose();
                return false;
            }

            owner = new ReferenceWorldLocalSkillRelease(
                catalog,
                sound,
                frames,
                alpha,
                additive);
            return true;
        }
        catch
        {
            DisposeFrames(frames);
            catalog.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Source phase-zero sound trigger for every row in SkillSoundDB.
    /// </summary>
    public bool TryPlayCastSound(int skillId)
    {
        ThrowIfDisposed();
        ReferenceSkillReleasePlan plan;
        if (!catalog.TryGetPlan(skillId, out plan))
        {
            return true;
        }

        if (plan.CastSoundId > 0 &&
            !sound.RequestPlayId(plan.CastSoundId, plan.CastDelayMilliseconds))
        {
            return false;
        }

        LastSkillId = skillId;
        LastCastSoundId = plan.CastSoundId;
        return true;
    }

    /// <summary>
    /// Creates the table-owned caster-attached effect and requests every fire-sound row.
    /// </summary>
    public bool TryPlayRelease(
        int skillId,
        Transform attach,
        int attachSortingOrder)
    {
        ThrowIfDisposed();
        ReferenceSkillReleasePlan plan;
        if (!catalog.TryGetPlan(skillId, out plan))
        {
            return true;
        }

        if (attach == null)
        {
            return false;
        }

        if (string.Equals(plan.EffectName, BlessingEffectName, StringComparison.Ordinal) &&
            plan.EffectDelayMilliseconds == 0)
        {
            ReferenceActiveSkillEffect effect = new ReferenceActiveSkillEffect(
                attach,
                attachSortingOrder,
                magicFrames,
                alphaMaterial,
                additiveMaterial);
            if (effect.Build())
            {
                activeEffects.Add(effect);
            }
            else
            {
                effect.Dispose();
            }
        }
        else if (!string.IsNullOrEmpty(plan.EffectName) &&
                 plan.EffectDelayMilliseconds == 0)
        {
            EffectPlayer.PlayNamed(
                attach.position,
                plan.EffectName,
                PixelsPerUnit,
                1,
                -1,
                false,
                false);
        }

        for (int index = 0; index < plan.FireSoundIds.Count; index++)
        {
            int soundId = plan.FireSoundIds[index];
            if (soundId > 0 &&
                !sound.RequestPlayId(
                    soundId,
                    plan.FireDelayMilliseconds[index]))
            {
                return false;
            }
        }

        LastSkillId = skillId;
        LastEffectName = plan.EffectName;
        LastFireSoundId = plan.FireSoundId;
        return true;
    }

    public bool Advance(float deltaTime)
    {
        ThrowIfDisposed();
        if (!IsNonNegativeFinite(deltaTime))
        {
            return false;
        }

        float deltaMilliseconds = deltaTime * 1000f;
        for (int index = activeEffects.Count - 1; index >= 0; index--)
        {
            ReferenceActiveSkillEffect effect = activeEffects[index];
            if (effect.Advance(deltaMilliseconds))
            {
                continue;
            }

            effect.Dispose();
            activeEffects.RemoveAt(index);
        }

        return true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        for (int index = 0; index < activeEffects.Count; index++)
        {
            activeEffects[index].Dispose();
        }

        activeEffects.Clear();
        catalog.Dispose();
        DisposeFrames(magicFrames);
        alphaMaterial = null;
        additiveMaterial = null;
        Ready = false;
    }

    internal static ReferenceSkillEffectSample Sample(
        IReadOnlyList<ReferenceSkillEffectKey> keys,
        float milliseconds)
    {
        if (keys == null || keys.Count == 0)
        {
            return ReferenceSkillEffectSample.Identity;
        }

        if (milliseconds <= keys[0].Milliseconds)
        {
            return keys[0].Sample;
        }

        int lastIndex = keys.Count - 1;
        if (milliseconds >= keys[lastIndex].Milliseconds)
        {
            return keys[lastIndex].Sample;
        }

        for (int index = 0; index < lastIndex; index++)
        {
            ReferenceSkillEffectKey from = keys[index];
            ReferenceSkillEffectKey to = keys[index + 1];
            if (milliseconds < from.Milliseconds || milliseconds > to.Milliseconds)
            {
                continue;
            }

            float span = to.Milliseconds - from.Milliseconds;
            float amount = span > 0.01f
                ? Mathf.Clamp01((milliseconds - from.Milliseconds) / span)
                : 0f;
            return ReferenceSkillEffectSample.Lerp(from.Sample, to.Sample, amount);
        }

        return keys[lastIndex].Sample;
    }

    private static string Sha256Hex(byte[] value)
    {
        using (SHA256 hash = SHA256.Create())
        {
            return BitConverter.ToString(hash.ComputeHash(value))
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }
    }

    private static void DisposeFrames(
        IDictionary<int, ReferenceMagicFrameSet> frames)
    {
        if (frames == null)
        {
            return;
        }

        foreach (ReferenceMagicFrameSet value in frames.Values)
        {
            value?.Dispose();
        }

        frames.Clear();
    }

    private static bool IsNonNegativeFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }

    private static void DestroyOwned(UnityEngine.Object value)
    {
        if (!value)
        {
            return;
        }

        if (UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.Destroy(value);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(value);
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceWorldLocalSkillRelease));
        }
    }
}

internal sealed class ReferenceMagicFrameSet : IDisposable
{
    private readonly Texture2D[] textures;
    private readonly Sprite[] sprites;
    private bool disposed;

    private ReferenceMagicFrameSet(
        int magicId,
        Texture2D[] textures,
        Sprite[] sprites)
    {
        MagicId = magicId;
        this.textures = textures;
        this.sprites = sprites;
    }

    public int MagicId { get; }

    public int FrameCount => sprites.Length;

    public Sprite Frame(int index)
    {
        if (disposed || index < 0 || index >= sprites.Length)
        {
            return null;
        }

        return sprites[index];
    }

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        int magicId,
        string path,
        uint expectedVersion,
        int expectedFrameCount,
        out ReferenceMagicFrameSet result)
    {
        result = null;
        byte[] bytes = resources == null ? null : resources.ReadRes(path);
        if (DimgDecoder.Version(bytes) != expectedVersion ||
            DimgDecoder.FrameCount(bytes) != expectedFrameCount ||
            DimgDecoder.DirectionCount(bytes) != 1)
        {
            return false;
        }

        Texture2D[] textures = new Texture2D[expectedFrameCount];
        Sprite[] sprites = new Sprite[expectedFrameCount];
        try
        {
            for (int index = 0; index < expectedFrameCount; index++)
            {
                DimgFrame frame;
                if (!DimgDecoder.TryDecodeFrame(bytes, index, out frame) || frame == null)
                {
                    DestroyAll(sprites);
                    DestroyAll(textures);
                    return false;
                }

                if (!frame.HasPixels)
                {
                    continue;
                }

                if (!TryCreateSprite(
                        magicId,
                        index,
                        frame,
                        out textures[index],
                        out sprites[index]))
                {
                    DestroyAll(sprites);
                    DestroyAll(textures);
                    return false;
                }
            }

            result = new ReferenceMagicFrameSet(magicId, textures, sprites);
            return true;
        }
        catch
        {
            DestroyAll(sprites);
            DestroyAll(textures);
            throw;
        }
    }

    public static bool TryCreateGso(
        ReferenceResourceLoader resources,
        int magicId,
        string path,
        uint expectedVersion,
        int expectedHeaderSize,
        int expectedFrameCount,
        out ReferenceMagicFrameSet result)
    {
        result = null;
        byte[] bytes = resources == null ? null : resources.ReadRes(path);
        ReferenceGsoImage image;
        if (!ReferenceGsoDecoder.TryDecode(bytes, out image) || image == null ||
            image.Version != expectedVersion ||
            image.HeaderSize != expectedHeaderSize ||
            image.FrameCount != expectedFrameCount ||
            image.DirectionCount != 1)
        {
            return false;
        }

        Texture2D[] textures = new Texture2D[expectedFrameCount];
        Sprite[] sprites = new Sprite[expectedFrameCount];
        try
        {
            for (int index = 0; index < expectedFrameCount; index++)
            {
                if (!TryCreateGsoSprite(
                        magicId,
                        index,
                        image,
                        image.Frames[index],
                        out textures[index],
                        out sprites[index]))
                {
                    DestroyAll(sprites);
                    DestroyAll(textures);
                    return false;
                }
            }

            result = new ReferenceMagicFrameSet(magicId, textures, sprites);
            return true;
        }
        catch
        {
            DestroyAll(sprites);
            DestroyAll(textures);
            throw;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        DestroyAll(sprites);
        DestroyAll(textures);
    }

    private static bool TryCreateSprite(
        int magicId,
        int frameIndex,
        DimgFrame frame,
        out Texture2D texture,
        out Sprite sprite)
    {
        texture = null;
        sprite = null;
        Color32[] bottomUp = new Color32[frame.Pixels.Length];
        for (int sourceY = 0; sourceY < frame.Height; sourceY++)
        {
            Array.Copy(
                frame.Pixels,
                sourceY * frame.Width,
                bottomUp,
                (frame.Height - 1 - sourceY) * frame.Width,
                frame.Width);
        }

        texture = new Texture2D(
            frame.Width,
            frame.Height,
            TextureFormat.RGBA32,
            false,
            false)
        {
            name = "RecoveredMagic" + magicId + "Texture" + frameIndex,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixels32(bottomUp);
        texture.Apply(false, false);

        Vector2 pivot = new Vector2(
            (float)(frame.BoundCenterX - frame.Left) / frame.Width,
            1f - (float)(frame.BoundCenterY - frame.Top) / frame.Height);
        sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, frame.Width, frame.Height),
            pivot,
            ReferenceWorldLocalSkillRelease.PixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
        if (!sprite)
        {
            DestroyOwned(texture);
            texture = null;
            return false;
        }

        sprite.name = "RecoveredMagic" + magicId + "Frame" + frameIndex;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return true;
    }

    private static bool TryCreateGsoSprite(
        int magicId,
        int frameIndex,
        ReferenceGsoImage image,
        ReferenceGsoFrame frame,
        out Texture2D texture,
        out Sprite sprite)
    {
        texture = null;
        sprite = null;
        if (image == null || frame == null || frame.Width < 1 || frame.Height < 1 ||
            frame.Pixels == null || frame.Pixels.Length != frame.Width * frame.Height)
        {
            return false;
        }

        Color32[] bottomUp = new Color32[frame.Pixels.Length];
        for (int sourceY = 0; sourceY < frame.Height; sourceY++)
        {
            Array.Copy(
                frame.Pixels,
                sourceY * frame.Width,
                bottomUp,
                (frame.Height - 1 - sourceY) * frame.Width,
                frame.Width);
        }

        texture = new Texture2D(
            frame.Width,
            frame.Height,
            TextureFormat.RGBA32,
            false,
            false)
        {
            name = "RecoveredMagic" + magicId + "Texture" + frameIndex,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixels32(bottomUp);
        texture.Apply(false, false);

        Vector2 pivot = new Vector2(
            (float)(image.BoundCenterX - frame.Left) / frame.Width,
            1f - (float)(image.BoundCenterY - frame.Top) / frame.Height);
        sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, frame.Width, frame.Height),
            pivot,
            ReferenceWorldLocalSkillRelease.PixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
        if (!sprite)
        {
            DestroyOwned(texture);
            texture = null;
            return false;
        }

        sprite.name = "RecoveredMagic" + magicId + "Frame" + frameIndex;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return true;
    }

    private static void DestroyAll(UnityEngine.Object[] values)
    {
        if (values == null)
        {
            return;
        }

        for (int index = 0; index < values.Length; index++)
        {
            DestroyOwned(values[index]);
            values[index] = null;
        }
    }

    private static void DestroyOwned(UnityEngine.Object value)
    {
        if (!value)
        {
            return;
        }

        if (UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.Destroy(value);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(value);
        }
    }
}

internal sealed class ReferenceActiveSkillEffect : IDisposable
{
    private static readonly ReferenceSkillEffectKey[] AlphaKeys =
    {
        new ReferenceSkillEffectKey(690f, ReferenceSkillEffectSample.Create(0f, 0f, 0f, 1f, 1f, 0f, 1f)),
        new ReferenceSkillEffectKey(1290f, ReferenceSkillEffectSample.Create(0f, 0f, 0f, 1f, 1f, 0f, 1f)),
        new ReferenceSkillEffectKey(1670f, ReferenceSkillEffectSample.Create(0f, 0f, 0f, 1f, 1f, 0f, 0f)),
    };

    private static readonly ReferenceSkillEffectKey[] AddKeys =
    {
        new ReferenceSkillEffectKey(600f, ReferenceSkillEffectSample.Create(0f, 0f, 0f, 0.5f, 0.5f, 0f, 1f)),
        new ReferenceSkillEffectKey(1200f, ReferenceSkillEffectSample.Create(0f, 0f, 0f, 0.5f, 0.5f, 0f, 1f)),
        new ReferenceSkillEffectKey(1540f, ReferenceSkillEffectSample.Create(0f, 0f, 0f, 0.5f, 0.5f, 0f, 0f)),
    };

    private static readonly ReferenceSkillEffectKey[] RaisedKeys =
    {
        new ReferenceSkillEffectKey(600f, ReferenceSkillEffectSample.Create(0f, 0f, -80f, 0.5f, 0.5f, 0f, 0f)),
        new ReferenceSkillEffectKey(725f, ReferenceSkillEffectSample.Create(0f, 0f, -80f, 0.5f, 0.5f, 0f, 1f)),
        new ReferenceSkillEffectKey(1545f, ReferenceSkillEffectSample.Create(0f, 0f, -80f, 0.6f, 0.6f, 0f, 0f)),
    };

    private readonly Transform attach;
    private readonly int attachSortingOrder;
    private readonly IDictionary<int, ReferenceMagicFrameSet> magicFrames;
    private readonly Material alphaMaterial;
    private readonly Material additiveMaterial;
    private readonly List<SpriteRenderer> renderers =
        new List<SpriteRenderer>(ReferenceWorldLocalSkillRelease.BlessingLayerCount);
    private readonly int[] frameIndices =
        new int[ReferenceWorldLocalSkillRelease.BlessingLayerCount];
    private GameObject root;
    private bool disposed;

    public ReferenceActiveSkillEffect(
        Transform attach,
        int attachSortingOrder,
        IDictionary<int, ReferenceMagicFrameSet> magicFrames,
        Material alphaMaterial,
        Material additiveMaterial)
    {
        this.attach = attach;
        this.attachSortingOrder = attachSortingOrder;
        this.magicFrames = magicFrames;
        this.alphaMaterial = alphaMaterial;
        this.additiveMaterial = additiveMaterial;
    }

    public float ElapsedMilliseconds { get; private set; }

    public IReadOnlyList<SpriteRenderer> Renderers => renderers;

    public bool Build()
    {
        if (disposed || root != null || attach == null ||
            magicFrames == null || alphaMaterial == null || additiveMaterial == null)
        {
            return false;
        }

        root = new GameObject(ReferenceWorldLocalSkillRelease.BlessingEffectName);
        root.transform.SetParent(attach, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        for (int index = 0; index < ReferenceWorldLocalSkillRelease.BlessingLayerCount; index++)
        {
            GameObject layer = new GameObject("Layer" + index);
            layer.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = attachSortingOrder + index +
                ReferenceWorldLocalSkillRelease.FrontPlaneSortingOffset;
            renderer.sharedMaterial = index == 1 || index == 3
                ? additiveMaterial
                : alphaMaterial;
            renderers.Add(renderer);
        }

        Apply(0f);
        return true;
    }

    public bool Advance(float deltaMilliseconds)
    {
        if (disposed || root == null || float.IsNaN(deltaMilliseconds) ||
            float.IsInfinity(deltaMilliseconds) || deltaMilliseconds < 0f)
        {
            return false;
        }

        ElapsedMilliseconds += deltaMilliseconds;
        if (ElapsedMilliseconds > ReferenceWorldLocalSkillRelease.BlessingEffectLifeMilliseconds)
        {
            return false;
        }

        Apply(ElapsedMilliseconds);
        return true;
    }

    public int FrameIndex(int layerIndex)
    {
        return layerIndex >= 0 && layerIndex < frameIndices.Length
            ? frameIndices[layerIndex]
            : -1;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        renderers.Clear();
        DestroyOwned(root);
        root = null;
    }

    private void Apply(float milliseconds)
    {
        ApplyLayer(0, 4049, 76f, 0f, 0f, AlphaKeys, milliseconds);
        ApplyLayer(1, 4026, 32f, 0f, 0f, AddKeys, milliseconds);
        ApplyLayer(2, 4051, 100f, 1f, -79f, RaisedKeys, milliseconds);
        ApplyLayer(3, 4026, 32f, 0f, 0f, AddKeys, milliseconds);
    }

    private void ApplyLayer(
        int layerIndex,
        int magicId,
        float frameMilliseconds,
        float offsetX,
        float offsetY,
        IReadOnlyList<ReferenceSkillEffectKey> keys,
        float milliseconds)
    {
        ReferenceMagicFrameSet frames = magicFrames[magicId];
        int frameIndex = frames.FrameCount <= 1
            ? 0
            : Mathf.FloorToInt(milliseconds / frameMilliseconds) % frames.FrameCount;
        frameIndices[layerIndex] = frameIndex;

        ReferenceSkillEffectSample sample =
            ReferenceWorldLocalSkillRelease.Sample(keys, milliseconds);
        SpriteRenderer renderer = renderers[layerIndex];
        renderer.sprite = frames.Frame(frameIndex);
        renderer.color = sample.Color;
        renderer.transform.localPosition = new Vector3(
            (sample.X + offsetX) / ReferenceWorldLocalSkillRelease.PixelsPerUnit,
            (sample.Y - sample.Z * 0.5f - offsetY) /
                ReferenceWorldLocalSkillRelease.PixelsPerUnit,
            0f);
        renderer.transform.localScale = new Vector3(
            sample.ScaleX,
            sample.ScaleY,
            1f);
        renderer.transform.localRotation = Quaternion.Euler(0f, 0f, sample.Rotation);
    }

    private static void DestroyOwned(UnityEngine.Object value)
    {
        if (!value)
        {
            return;
        }

        if (UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.Destroy(value);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(value);
        }
    }
}

internal struct ReferenceSkillEffectKey
{
    public ReferenceSkillEffectKey(
        float milliseconds,
        ReferenceSkillEffectSample sample)
    {
        Milliseconds = milliseconds;
        Sample = sample;
    }

    public float Milliseconds { get; }
    public ReferenceSkillEffectSample Sample { get; }
}

internal struct ReferenceSkillEffectSample
{
    public static readonly ReferenceSkillEffectSample Identity =
        Create(0f, 0f, 0f, 1f, 1f, 0f, 1f);

    public ReferenceSkillEffectSample(
        float x,
        float y,
        float z,
        float scaleX,
        float scaleY,
        float rotation,
        Color color)
    {
        X = x;
        Y = y;
        Z = z;
        ScaleX = scaleX;
        ScaleY = scaleY;
        Rotation = rotation;
        Color = color;
    }

    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public float ScaleX { get; }
    public float ScaleY { get; }
    public float Rotation { get; }
    public Color Color { get; }

    public static ReferenceSkillEffectSample Create(
        float x,
        float y,
        float z,
        float scaleX,
        float scaleY,
        float rotation,
        float alpha)
    {
        return new ReferenceSkillEffectSample(
            x,
            y,
            z,
            scaleX,
            scaleY,
            rotation,
            new Color(1f, 1f, 1f, alpha));
    }

    public static ReferenceSkillEffectSample Lerp(
        ReferenceSkillEffectSample from,
        ReferenceSkillEffectSample to,
        float amount)
    {
        float t = Mathf.Clamp01(amount);
        return new ReferenceSkillEffectSample(
            Mathf.Lerp(from.X, to.X, t),
            Mathf.Lerp(from.Y, to.Y, t),
            Mathf.Lerp(from.Z, to.Z, t),
            Mathf.Lerp(from.ScaleX, to.ScaleX, t),
            Mathf.Lerp(from.ScaleY, to.ScaleY, t),
            Mathf.Lerp(from.Rotation, to.Rotation, t),
            Color.Lerp(from.Color, to.Color, t));
    }
}

}
