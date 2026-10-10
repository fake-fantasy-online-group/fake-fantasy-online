using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static class ReferenceWorldCombatFeedbackMath
{
    public const byte OpaqueAlphaThreshold = 16;
    public const float GlyphHeightScale = 0.7f;
    public const float GlyphOverlapRatio = 0.12f;
    public const float MissGlyphHeightScale = 0.9f;
    public const float MissGlyphOverlapRatio = 0.06f;

    public static Vector3 Offset(float normalized, float risePixels, int horizontalSign)
    {
        float horizontalPixels = horizontalSign == 0 ? 0f :
            horizontalSign * 10f * Mathf.Sin(normalized * Mathf.PI * 2f);
        return new Vector3(
            horizontalPixels / ReferenceWorldCombatFeedback.PixelsPerUnit,
            normalized * risePixels /
                ReferenceWorldCombatFeedback.PixelsPerUnit,
            0f);
    }

    public static void ScaleAndAlpha(
        float normalized,
        bool miss,
        bool critical,
        out float scale,
        out float alpha)
    {
        if (miss)
        {
            scale = Mathf.Min(normalized / 0.3f, 1f) * 0.6f + 0.4f;
            alpha = normalized < 0.6f ? 1f :
                Mathf.Clamp01(1f - (normalized - 0.6f) / 0.4f);
            return;
        }

        if (critical)
        {
            scale = Mathf.Min(normalized * 4f, 1f) * 1.2f + 0.8f;
            alpha = normalized < 0.7f ? 1f :
                Mathf.Clamp01(1f - (normalized - 0.7f) / 0.3f);
            return;
        }

        scale = 0.8f;
        alpha = 1f - normalized;
    }

    public static bool TryOpaqueBounds(Texture2D texture, out Rect bounds)
    {
        bounds = default(Rect);
        if (texture == null || texture.width < 1 || texture.height < 1)
        {
            return false;
        }

        Color32[] pixels = texture.GetPixels32();
        int minX = texture.width;
        int minY = texture.height;
        int maxX = -1;
        int maxY = -1;
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                if (pixels[y * texture.width + x].a <= OpaqueAlphaThreshold)
                {
                    continue;
                }

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return false;
        }

        bounds = new Rect(
            minX,
            minY,
            maxX - minX + 1,
            maxY - minY + 1);
        return true;
    }
}

/// <summary>
/// Source-owned projection of the locked CombatNumber and MissFx branches.
/// </summary>
internal sealed class ReferenceWorldCombatFeedback : IDisposable
{
    public const string NumberOnePath = "res/effect/num1.gsa";
    public const string NumberTwoPath = "res/effect/num2.gsa";
    public const string NumberThreePath = "res/effect/num3.gsa";
    public const string ExperiencePrefixPath = "res/exp/EXP.gsa";
    public const string MissPath = "res/effect/miss.gsa";
    public const float NormalLifeSeconds = 1.2f;
    public const float ExperienceLifeSeconds = 1.1f;
    public const float MissLifeSeconds = 0.7f;
    public const float PixelsPerUnit = 100f;
    public const int DealtDamageType = 0;
    public const int DealtCriticalDamageType = 1;
    public const int ReceivedDamageType = 2;
    public const int ReceivedCriticalDamageType = 3;
    public const string NormalImpactEffectPrefix = "BFX00";
    public const string CriticalImpactEffectName = "BFX007";
    public const float LocalHarmAnchorPixels = 80f;

    private readonly ReferenceResourceLoader resources;
    private readonly Transform parent;
    private readonly List<Popup> popups = new List<Popup>();
    private bool disposed;

    public ReferenceWorldCombatFeedback(
        ReferenceResourceLoader resources,
        Transform parent)
    {
        this.resources = resources ?? throw new ArgumentNullException(nameof(resources));
        this.parent = parent == null
            ? throw new ArgumentNullException(nameof(parent))
            : parent;
    }

    public int ActiveCount => popups.Count;

    // Historical content observation; native number creation supports partial loads and font fallback.
    public bool Ready =>
        resources.GsaFrameCount(NumberOnePath) == 10 &&
        resources.GsaFrameCount(NumberTwoPath) == 10 &&
        resources.GsaFrameCount(NumberThreePath) == 10 &&
        resources.GsaFrameCount(ExperiencePrefixPath) == 1 &&
        resources.GsaFrameCount(MissPath) == 4;

    public bool TrySpawnResult(
        ReferenceWorldMonsterActor actor,
        WorldCombatResultState result)
    {
        ThrowIfDisposed();
        if (result != null && result.Healing)
        {
            if (actor == null || actor.RootObject == null || result.TargetEntityId != actor.State.EntityId)
                return false;
            if (result.Damage <= 0) return true;
            Vector3 healPosition = actor.RootObject.transform.position;
            Vector3 up = Vector3.up;
            float healPpu = CombatWorld.PixelsPerUnitForIndependentRuntime;
            healPosition += up * (80f / healPpu);
            return TrySpawnNumber(healPosition, result.Damage, 5, healPpu);
        }

        if (actor == null || result == null || actor.RootObject == null ||
            result.TargetEntityId != actor.State.EntityId)
        {
            return false;
        }

        Vector3 position = actor.RootObject.transform.position +
                           Vector3.up * actor.HeadTopUnits;
        if (result.Missed)
        {
            return TrySpawnMiss(position);
        }

        if (!TrySpawnNumber(
                position,
                result.Damage,
                result.Critical ? DealtCriticalDamageType : DealtDamageType))
        {
            return false;
        }

        // CombatWorld.OnCombat ignores EffectPlayer.Play's return value. Missing effect art must
        // not reject the authoritative damage edge, while the complete package set supplies the
        // ordinary BFX004..BFX006 and critical BFX007 templates in normal production.
        EffectPlayer.PlayNamed(
            actor.RootObject.transform.position,
            ImpactEffectName(result.Damage, result.Critical),
            PixelsPerUnit);
        return true;
    }

    public bool TrySpawnLocalResult(
        ReferenceWorldLocalPlayerDoll player,
        WorldCombatResultState result)
    {
        ThrowIfDisposed();
        if (player == null || player.Root == null || result == null ||
            result.TargetEntityId != player.EntityId)
        {
            return false;
        }

        Vector3 position = player.Root.position +
                           Vector3.up * (LocalHarmAnchorPixels / PixelsPerUnit);
        return result.Missed
            ? TrySpawnMiss(position)
            : TrySpawnNumber(
                position,
                result.Damage,
                result.Critical ? ReceivedCriticalDamageType : ReceivedDamageType);
    }

    // Official 1.5.8 NetClient.Dispatch case 0x800c (arm64 0x2517e0) reads U8 target / I32 value /
    // U8 type from the same Buf and tail-calls CombatNumber.SpawnOn (0x06000117, arm64 0x23a734)
    // with no local-player lookup and no type, target or sign filter, so this adapter forwards the
    // bytes as read and lets SpawnOn keep its own native type == 5 per-frame cap of two, its
    // target == 1 riding/pet selection and its own null return when the static playerT/petT is
    // missing. The former player/Root precondition was a product gate that suppressed numbers
    // whenever the local doll was absent even though a static transform was already registered.
    public bool TrySpawnWorldNumber(WorldCombatNumberState state)
    {
        ThrowIfDisposed();
        if (state == null)
        {
            return false;
        }

        CombatNumber.SpawnOn(state.Target, state.Value, state.Type);
        return true;
    }

    public bool TryAdvance(float deltaTimeSeconds)
    {
        ThrowIfDisposed();
        if (float.IsNaN(deltaTimeSeconds) || float.IsInfinity(deltaTimeSeconds) ||
            deltaTimeSeconds < 0f)
        {
            return false;
        }

        for (int index = popups.Count - 1; index >= 0; index--)
        {
            Popup popup = popups[index];
            if (popup.TryAdvance(deltaTimeSeconds))
            {
                continue;
            }

            popup.Dispose();
            popups.RemoveAt(index);
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
        foreach (Popup popup in popups)
        {
            popup.Dispose();
        }

        popups.Clear();
    }

    private bool TrySpawnMiss(Vector3 position)
    {
        MissFx.Spawn(position, PixelsPerUnit);
        return true;
    }

    private bool TrySpawnNumber(
        Vector3 position,
        int value,
        int type,
        float pixelsPerUnit = PixelsPerUnit)
    {
        CombatNumber.Spawn(position, value, type, pixelsPerUnit);
        return true;
    }

    internal static string ImpactEffectName(int damage, bool critical)
    {
        if (critical)
        {
            return CriticalImpactEffectName;
        }

        int magnitude = damage < 0 ? unchecked(-damage) : damage;
        int suffix = magnitude % 3 + 4;
        return NormalImpactEffectPrefix + suffix;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceWorldCombatFeedback));
        }
    }

    private sealed class Popup : IDisposable
    {
        private readonly GameObject root;
        private readonly List<Sprite> sprites;
        private readonly List<SpriteRenderer> renderers;
        private readonly Vector3 basePosition;
        private readonly float life;
        private readonly float risePixels;
        private readonly bool miss;
        private readonly bool critical;
        private readonly int horizontalSign;
        private float elapsed;

        private Popup(
            GameObject root,
            List<Sprite> sprites,
            List<SpriteRenderer> renderers,
            Vector3 basePosition,
            float life,
            float risePixels,
            bool miss,
            bool critical,
            int horizontalSign)
        {
            this.root = root;
            this.sprites = sprites;
            this.renderers = renderers;
            this.basePosition = basePosition;
            this.life = life;
            this.risePixels = risePixels;
            this.miss = miss;
            this.critical = critical;
            this.horizontalSign = horizontalSign;
        }

        public static Popup TryCreate(
            Transform parent,
            Vector3 position,
            IReadOnlyList<Texture2D> glyphs,
            float life,
            float risePixels,
            bool miss,
            bool critical,
            int horizontalSign)
        {
            if (glyphs == null || glyphs.Count < 1 || life <= 0f)
            {
                return null;
            }

            GameObject root = new GameObject(miss
                ? "RecoveredMissFx"
                : "RecoveredCombatNumber");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            List<Sprite> sprites = new List<Sprite>(glyphs.Count);
            List<SpriteRenderer> renderers = new List<SpriteRenderer>(glyphs.Count);
            List<float> scales = new List<float>(glyphs.Count);
            List<float> renderedWidths = new List<float>(glyphs.Count);
            float artScale = 1f;
            for (int index = 0; index < glyphs.Count; index++)
            {
                artScale = Mathf.Max(artScale, GsaDecoder.Scale(glyphs[index]));
            }

            float denominator = PixelsPerUnit * artScale;
            float maximumHeight = 0f;
            for (int index = 0; index < glyphs.Count; index++)
            {
                Texture2D texture = glyphs[index];
                Rect spriteRect;
                if (!ReferenceWorldCombatFeedbackMath.TryOpaqueBounds(
                        texture,
                        out spriteRect))
                {
                    DestroyOwned(root);
                    foreach (Sprite owned in sprites)
                    {
                        DestroyOwned(owned);
                    }
                    return null;
                }

                Sprite sprite = Sprite.Create(
                    texture,
                    spriteRect,
                    new Vector2(0.5f, 0f),
                    miss ? denominator : PixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);
                if (sprite == null)
                {
                    DestroyOwned(root);
                    foreach (Sprite owned in sprites)
                    {
                        DestroyOwned(owned);
                    }
                    return null;
                }

                sprites.Add(sprite);
                maximumHeight = Mathf.Max(
                    maximumHeight,
                    sprite.rect.height / denominator);
            }

            if (maximumHeight <= 0.01f)
            {
                maximumHeight = 0.46f;
            }

            float glyphHeightScale = miss
                ? ReferenceWorldCombatFeedbackMath.MissGlyphHeightScale
                : ReferenceWorldCombatFeedbackMath.GlyphHeightScale;
            float glyphOverlapRatio = miss
                ? ReferenceWorldCombatFeedbackMath.MissGlyphOverlapRatio
                : ReferenceWorldCombatFeedbackMath.GlyphOverlapRatio;
            float totalWidth = 0f;
            for (int index = 0; index < sprites.Count; index++)
            {
                Sprite sprite = sprites[index];
                float scale;
                float renderedWidth = sprite.rect.width / denominator;
                float height = sprite.rect.height / denominator;
                scale = height > 0.001f
                    ? (maximumHeight / height) * glyphHeightScale
                    : glyphHeightScale;
                renderedWidth *= scale;

                scales.Add(scale);
                renderedWidths.Add(renderedWidth);
                totalWidth += renderedWidth;
            }

            float overlap = maximumHeight * glyphOverlapRatio * glyphHeightScale;
            float cursor = -(totalWidth - overlap * Mathf.Max(0, glyphs.Count - 1)) * 0.5f;
            for (int index = 0; index < sprites.Count; index++)
            {
                Sprite sprite = sprites[index];
                GameObject child = new GameObject("Glyph" + index);
                child.transform.SetParent(root.transform, false);
                child.transform.localPosition = new Vector3(
                    cursor + renderedWidths[index] * 0.5f,
                    0f,
                    0f);
                child.transform.localScale = new Vector3(
                    scales[index],
                    scales[index],
                    1f);
                cursor += renderedWidths[index] - overlap;
                SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 32760;
                renderers.Add(renderer);
            }

            return new Popup(
                root,
                sprites,
                renderers,
                position,
                life,
                risePixels,
                miss,
                critical,
                horizontalSign);
        }

        public bool TryAdvance(float deltaTimeSeconds)
        {
            elapsed += deltaTimeSeconds;
            float normalized = Mathf.Clamp01(elapsed / life);
            float alpha;
            float scale;
            ReferenceWorldCombatFeedbackMath.ScaleAndAlpha(
                normalized,
                miss,
                critical,
                out scale,
                out alpha);
            root.transform.position = basePosition +
                ReferenceWorldCombatFeedbackMath.Offset(
                    normalized,
                    risePixels,
                    horizontalSign);
            root.transform.localScale = new Vector3(scale, scale, 1f);
            Color color = new Color(1f, 1f, 1f, alpha);
            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.color = color;
            }

            return elapsed < life;
        }

        public void Dispose()
        {
            DestroyOwned(root);
            foreach (Sprite sprite in sprites)
            {
                DestroyOwned(sprite);
            }
            sprites.Clear();
            renderers.Clear();
        }

        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (value == null)
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
}

}
