using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source translation of MonsterEntity.ShowStatusFx and ActorEffects.
/// </summary>
/// <remarks>
/// The exact 中毒 animation retains its specialized clock while every other named template enters
/// the generic EffectPlayer path with the same lifetime bookkeeping.
/// </remarks>
internal sealed class ReferenceWorldMonsterStatusEffectView : IDisposable
{
    public const string PoisonEffectName = "中毒";
    public const string PoisonEffectPath = "effect/中毒.eff";
    public const string PoisonMagicPath = "magic/magic/magic0098.gso";
    public const string RootName = PoisonEffectName;
    public const int RequiredEffectBytes = 303;
    public const int RequiredMagicBytes = 69196;
    public const uint RequiredMagicVersion = ReferenceGsoDecoder.Version101;
    public const int RequiredMagicHeaderSize = 24;
    public const int RequiredFrameCount = 11;
    public const int RequiredDirectionCount = 1;
    public const float PixelsPerUnit = 100f;
    public const float MinimumLifetimeSeconds = 0.1f;
    public const float FrameMilliseconds = 100f;
    public const float SettledMilliseconds = 1100f;
    public const float OrbitRadiusXPixels = 30f;
    public const float OrbitRadiusYPixels = 10f;
    public const float OrbitRowPixels = 30f;
    public const int OrbitFirstSlot = 4;
    public const int OrbitSlotCount = 3;
    public const int BehindSortingOffset = -2;
    public const int FrontSortingOffset = 20;

    private const string EffectSha256 =
        "a908a436eeb4fde955ec5730b7df05ffbcf04171d2508a206335f1c6bb77f213";
    private const string MagicSha256 =
        "a1a85187f2fb84925ba3d34623978687d4bf98000362f94159482f0dae0f2544";

    private readonly ReferenceResourceLoader resources;
    private readonly Transform owner;
    private readonly SpriteRenderer bodyRenderer;
    private readonly List<RuntimeEffect> effects = new List<RuntimeEffect>();
    private readonly Texture2D[] textures = new Texture2D[RequiredFrameCount];
    private readonly Sprite[] sprites = new Sprite[RequiredFrameCount];
    private GameObject root;
    private SpriteRenderer renderer;
    private Material material;
    private float bodyHeightUnits;
    private int activeCount;
    private int frameIndex = -1;
    private int poisonSlot = -1;
    private bool built;
    private bool disposed;

    public ReferenceWorldMonsterStatusEffectView(
        ReferenceResourceLoader resources,
        Transform owner,
        SpriteRenderer bodyRenderer)
    {
        this.resources = resources ?? throw new ArgumentNullException(nameof(resources));
        this.owner = owner == null ? throw new ArgumentNullException(nameof(owner)) : owner;
        this.bodyRenderer = bodyRenderer == null
            ? throw new ArgumentNullException(nameof(bodyRenderer))
            : bodyRenderer;
    }

    public bool Built => built;
    public GameObject Root => root;
    public SpriteRenderer Renderer => renderer;
    public int ActiveCount => activeCount;
    public int FrameIndex => frameIndex;
    public int PoisonSlot => poisonSlot;

    public float ExpirationAt(string effectName)
    {
        RuntimeEffect effect = Find(effectName);
        return effect == null ? float.NaN : effect.ExpiresAt;
    }

    public bool Build()
    {
        if (disposed || built || owner == null || bodyRenderer == null ||
            bodyRenderer.sprite == null)
        {
            return false;
        }

        byte[] effectBytes = resources.ReadRes(PoisonEffectPath);
        byte[] magicBytes = resources.ReadRes(PoisonMagicPath);
        ReferenceGsoImage image;
        if (effectBytes == null || effectBytes.Length != RequiredEffectBytes ||
            !string.Equals(Sha256Hex(effectBytes), EffectSha256, StringComparison.Ordinal) ||
            magicBytes == null || magicBytes.Length != RequiredMagicBytes ||
            !string.Equals(Sha256Hex(magicBytes), MagicSha256, StringComparison.Ordinal) ||
            !ReferenceGsoDecoder.TryDecode(magicBytes, out image) || image == null ||
            image.Version != RequiredMagicVersion ||
            image.HeaderSize != RequiredMagicHeaderSize ||
            image.FrameCount != RequiredFrameCount ||
            image.DirectionCount != RequiredDirectionCount)
        {
            return false;
        }

        try
        {
            for (int index = 0; index < RequiredFrameCount; index++)
            {
                if (!TryCreateSprite(
                        image,
                        image.Frames[index],
                        index,
                        out textures[index],
                        out sprites[index]))
                {
                    ReleaseOwnedState();
                    return false;
                }
            }

            material = EffectPlayer.BorrowAlphaMaterial();
            if (material == null)
            {
                ReleaseOwnedState();
                return false;
            }

            root = new GameObject(RootName);
            root.transform.SetParent(owner, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            renderer = root.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = material;
            renderer.color = new Color(1f, 1f, 1f, 0.9f);
            bodyHeightUnits = bodyRenderer.sprite.bounds.size.y;
            root.SetActive(false);
            built = renderer != null;
            return built;
        }
        catch
        {
            ReleaseOwnedState();
            return false;
        }
    }

    public bool TrySet(
        IReadOnlyList<WorldEntityStatusEffect> statusEffects,
        float nowSeconds)
    {
        if (disposed || !built || statusEffects == null || !IsFinite(nowSeconds))
        {
            return false;
        }

        HashSet<string> desired = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < statusEffects.Count; index++)
        {
            WorldEntityStatusEffect row = statusEffects[index];
            if (row == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(row.Effect))
            {
                desired.Add(row.Effect);
            }
        }

        for (int index = 0; index < statusEffects.Count; index++)
        {
            WorldEntityStatusEffect row = statusEffects[index];
            if (string.IsNullOrEmpty(row.Effect))
            {
                continue;
            }

            RuntimeEffect current = Find(row.Effect);
            if (current != null && current.Revision == row.Revision)
            {
                continue;
            }

            float expiresAt = nowSeconds + Mathf.Max(
                MinimumLifetimeSeconds,
                row.DurationMilliseconds / 1000f);
            if (current != null && current.ExpiresAt > nowSeconds)
            {
                // ShowStatusFx refreshes the expiry dictionary, while SyncAuras retains the
                // already-created EffectPlayer, its placement mode, slot and animation clock.
                current.Revision = row.Revision;
                current.ExpiresAt = expiresAt;
                continue;
            }

            int slot = IsNormalOrbit(row.Effect, row.PositionMode)
                ? OrbitFirstSlot + ActiveNormalOrbitCount(nowSeconds) % OrbitSlotCount
                : -1;
            RuntimeEffect replacement = new RuntimeEffect(
                row.Effect,
                row.PositionMode,
                row.Revision,
                nowSeconds,
                expiresAt,
                slot);
            if (!string.Equals(
                    row.Effect,
                    PoisonEffectName,
                    StringComparison.Ordinal))
            {
                EffectPlayer player = EffectPlayer.PlayNamed(
                    owner.position,
                    row.Effect,
                    PixelsPerUnit,
                    1,
                    -1,
                    false,
                    false);
                if (player)
                {
                    player.followTarget = owner;
                    player.attachSort = bodyRenderer;
                }
            }
            if (current == null)
            {
                effects.Add(replacement);
            }
            else
            {
                effects[effects.IndexOf(current)] = replacement;
            }
        }

        // The Application snapshot accumulates keyed live packets, including expired tombstones.
        // A missing name therefore denotes a complete spawn-root replacement, not normal expiry.
        for (int index = effects.Count - 1; index >= 0; index--)
        {
            if (!desired.Contains(effects[index].Name))
            {
                effects.RemoveAt(index);
            }
        }

        return TryTick(nowSeconds);
    }

    public bool TryTick(float nowSeconds)
    {
        if (disposed || !built || root == null || renderer == null ||
            bodyRenderer == null || !IsFinite(nowSeconds))
        {
            return false;
        }

        activeCount = 0;
        RuntimeEffect poison = null;
        for (int index = 0; index < effects.Count; index++)
        {
            RuntimeEffect effect = effects[index];
            if (effect.ExpiresAt <= nowSeconds)
            {
                continue;
            }

            activeCount++;
            if (poison == null && string.Equals(
                    effect.Name,
                    PoisonEffectName,
                    StringComparison.Ordinal))
            {
                poison = effect;
            }
        }

        bool visible = poison != null;
        if (root.activeSelf != visible)
        {
            root.SetActive(visible);
        }

        if (!visible)
        {
            frameIndex = -1;
            poisonSlot = -1;
            return true;
        }

        float elapsedMilliseconds = SettledMilliseconds +
                                    (nowSeconds - poison.CreatedAt) * 1000f;
        frameIndex = Mathf.FloorToInt(elapsedMilliseconds / FrameMilliseconds) %
                     RequiredFrameCount;
        if (frameIndex < 0)
        {
            frameIndex += RequiredFrameCount;
        }

        renderer.sprite = sprites[frameIndex];
        poisonSlot = poison.Slot;
        Place(poison, nowSeconds);
        return renderer.sprite != null;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        ReleaseOwnedState();
    }

    private void Place(RuntimeEffect effect, float nowSeconds)
    {
        if (effect.PositionMode == 4)
        {
            root.transform.localPosition = new Vector3(0f, bodyHeightUnits + 0.1f, 0f);
            renderer.sortingOrder = bodyRenderer.sortingOrder + FrontSortingOffset;
            return;
        }

        if (effect.PositionMode == 2)
        {
            root.transform.localPosition = Vector3.zero;
            renderer.sortingOrder = bodyRenderer.sortingOrder + FrontSortingOffset;
            return;
        }

        if (effect.PositionMode != 1 || effect.Slot < OrbitFirstSlot)
        {
            root.transform.localPosition = Vector3.zero;
            renderer.sortingOrder = bodyRenderer.sortingOrder + FrontSortingOffset;
            return;
        }

        float angle = nowSeconds * Mathf.PI;
        if (effect.Slot == OrbitFirstSlot + 1)
        {
            angle = -angle;
        }

        float sine = Mathf.Sin(angle);
        root.transform.localPosition = new Vector3(
            Mathf.Cos(angle) * OrbitRadiusXPixels / PixelsPerUnit,
            (OrbitFirstSlot + OrbitSlotCount - 1 - effect.Slot) * OrbitRowPixels /
                PixelsPerUnit + sine * OrbitRadiusYPixels / PixelsPerUnit,
            0f);
        renderer.sortingOrder = bodyRenderer.sortingOrder +
                                (sine < 0f
                                    ? BehindSortingOffset
                                    : FrontSortingOffset);
    }

    private int ActiveNormalOrbitCount(float nowSeconds)
    {
        int count = 0;
        for (int index = 0; index < effects.Count; index++)
        {
            RuntimeEffect effect = effects[index];
            if (effect.ExpiresAt > nowSeconds &&
                IsNormalOrbit(effect.Name, effect.PositionMode))
            {
                count++;
            }
        }

        return count;
    }

    private RuntimeEffect Find(string effectName)
    {
        for (int index = 0; index < effects.Count; index++)
        {
            if (string.Equals(
                    effects[index].Name,
                    effectName,
                    StringComparison.Ordinal))
            {
                return effects[index];
            }
        }

        return null;
    }

    private static bool IsNormalOrbit(string effectName, byte positionMode)
    {
        return positionMode == 1 &&
               !effectName.StartsWith("TFX", StringComparison.Ordinal);
    }

    private static bool TryCreateSprite(
        ReferenceGsoImage image,
        ReferenceGsoFrame frame,
        int index,
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
            name = "RecoveredMonsterPoisonTexture" + index,
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
            PixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
        if (sprite == null)
        {
            DestroyOwned(texture);
            texture = null;
            return false;
        }

        sprite.name = "RecoveredMonsterPoisonFrame" + index;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return true;
    }

    private void ReleaseOwnedState()
    {
        built = false;
        effects.Clear();
        activeCount = 0;
        frameIndex = -1;
        poisonSlot = -1;
        renderer = null;
        DestroyOwned(root);
        root = null;
        for (int index = 0; index < RequiredFrameCount; index++)
        {
            DestroyOwned(sprites[index]);
            DestroyOwned(textures[index]);
            sprites[index] = null;
            textures[index] = null;
        }
        material = null;
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

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
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

    private sealed class RuntimeEffect
    {
        public RuntimeEffect(
            string name,
            byte positionMode,
            long revision,
            float createdAt,
            float expiresAt,
            int slot)
        {
            Name = name;
            PositionMode = positionMode;
            Revision = revision;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
            Slot = slot;
        }

        public string Name { get; }
        public byte PositionMode { get; }
        public long Revision { get; set; }
        public float CreatedAt { get; }
        public float ExpiresAt { get; set; }
        public int Slot { get; }
    }
}

}
