using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source translation of MonsterEntity.Set/Apply/Place/TickStatusIcons.
/// </summary>
internal sealed class ReferenceWorldMonsterStatusIconView : IDisposable
{
    public const string RootName = "MonStatusIcons";
    public const string IconPathFormat = "item/item/item{0}.gso";
    public const int SortingOrder = 32000;
    public const int MaximumVisibleIcons = 8;
    public const int IconSizePixels = 16;
    public const int CellStridePixels = 17;
    public const int ContentSizePixels = 14;
    public const float MinimumTimedLifetimeSeconds = 0.1f;
    public const float SourceAlphaThreshold = 0.02f;
    public static readonly Color32 BeneficialBorder = new Color32(70, 210, 90, 255);
    public static readonly Color32 HarmfulBorder = new Color32(225, 70, 70, 255);

    private readonly ReferenceResourceLoader resources;
    private readonly Material material;
    private GameObject root;
    private SpriteRenderer renderer;
    private Sprite sprite;
    private Texture2D texture;
    private WorldEntityStatusIcon[] rows = Array.Empty<WorldEntityStatusIcon>();
    private float[] expiresAt = Array.Empty<float>();
    private string renderedSignature = string.Empty;
    private int activeCount;
    private bool disposed;

    public ReferenceWorldMonsterStatusIconView(
        ReferenceResourceLoader resources,
        Material material)
    {
        this.resources = resources ?? throw new ArgumentNullException(nameof(resources));
        this.material = material;
    }

    public GameObject Root => root;
    public SpriteRenderer Renderer => renderer;
    public Texture2D Texture => texture;
    public int ActiveCount => activeCount;

    public float ExpirationAt(int index)
    {
        if (index < 0 || index >= expiresAt.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return expiresAt[index];
    }

    public bool TryBuild(Transform owner)
    {
        if (disposed || owner == null || root)
        {
            return false;
        }

        root = new GameObject(RootName);
        root.transform.SetParent(owner, false);
        renderer = root.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = SortingOrder;
        renderer.sharedMaterial = material;
        root.SetActive(false);
        return renderer != null;
    }

    public bool TrySet(
        IReadOnlyList<WorldEntityStatusIcon> statusIcons,
        float nowSeconds)
    {
        if (disposed || !root || statusIcons == null || !IsFinite(nowSeconds))
        {
            return false;
        }

        rows = new WorldEntityStatusIcon[statusIcons.Count];
        expiresAt = new float[statusIcons.Count];
        for (int index = 0; index < statusIcons.Count; index++)
        {
            WorldEntityStatusIcon row = statusIcons[index];
            if (row == null)
            {
                return false;
            }

            rows[index] = row;
            expiresAt[index] = row.RemainingMilliseconds < 0
                ? -1f
                : nowSeconds + Mathf.Max(
                    MinimumTimedLifetimeSeconds,
                    row.RemainingMilliseconds / 1000f);
        }

        return TryApplyActive(nowSeconds);
    }

    public bool TryTick(
        bool show,
        float localY,
        Camera worldCamera,
        float uiScale,
        int screenHeight,
        float nowSeconds)
    {
        if (!TryTickActiveForLifecycle(nowSeconds))
        {
            return false;
        }

        return TryPlaceForLifecycle(
            show,
            localY,
            worldCamera,
            uiScale,
            screenHeight);
    }

    internal bool TryTickActiveForLifecycle(float nowSeconds)
    {
        return !disposed && root && IsFinite(nowSeconds) &&
               TryApplyActive(nowSeconds);
    }

    internal bool TryPlaceForLifecycle(
        bool show,
        float localY,
        Camera worldCamera,
        float uiScale,
        int screenHeight)
    {
        if (disposed || !root)
        {
            return false;
        }

        bool visible = show && activeCount > 0 && renderer != null && renderer.sprite;
        if (root.activeSelf != visible)
        {
            root.SetActive(visible);
        }

        if (visible)
        {
            ReferenceWorldMonsterNameTagView.PlaceSharp(
                root.transform,
                localY,
                worldCamera,
                uiScale,
                screenHeight);
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
        DestroyOwned(root);
        DestroyOwned(sprite);
        DestroyOwned(texture);
        root = null;
        renderer = null;
        sprite = null;
        texture = null;
        rows = Array.Empty<WorldEntityStatusIcon>();
        expiresAt = Array.Empty<float>();
        activeCount = 0;
        renderedSignature = string.Empty;
    }

    private bool TryApplyActive(float nowSeconds)
    {
        int[] active = new int[Math.Min(rows.Length, MaximumVisibleIcons)];
        int count = 0;
        StringBuilder signature = new StringBuilder();
        for (int index = 0;
             index < rows.Length && count < MaximumVisibleIcons;
             index++)
        {
            if (expiresAt[index] >= 0f && expiresAt[index] <= nowSeconds)
            {
                continue;
            }

            WorldEntityStatusIcon row = rows[index];
            active[count++] = index;
            signature.Append(row.StatusId.ToString(CultureInfo.InvariantCulture));
            signature.Append(':');
            signature.Append(row.IconId.ToString(CultureInfo.InvariantCulture));
            signature.Append(':');
            signature.Append(row.Beneficial.ToString(CultureInfo.InvariantCulture));
            signature.Append(';');
        }

        string nextSignature = signature.ToString();
        activeCount = count;
        if (count == 0)
        {
            renderedSignature = string.Empty;
            if (root.activeSelf)
            {
                root.SetActive(false);
            }

            return true;
        }

        if (nextSignature == renderedSignature && texture && renderer && renderer.sprite)
        {
            return true;
        }

        int width = count * CellStridePixels - 1;
        Color32[] pixels = new Color32[width * IconSizePixels];
        for (int itemIndex = 0; itemIndex < count; itemIndex++)
        {
            WorldEntityStatusIcon row = rows[active[itemIndex]];
            int startX = itemIndex * CellStridePixels;
            Color32 border = row.Beneficial != 0 ? BeneficialBorder : HarmfulBorder;
            Texture2D icon = resources.LoadItemIconFirstVisible(
                string.Format(
                    CultureInfo.InvariantCulture,
                    IconPathFormat,
                    row.IconId));
            for (int y = 0; y < IconSizePixels; y++)
            {
                for (int x = 0; x < IconSizePixels; x++)
                {
                    int output = y * width + startX + x;
                    if (x == 0 || x == IconSizePixels - 1 ||
                        y == 0 || y == IconSizePixels - 1)
                    {
                        pixels[output] = border;
                        continue;
                    }

                    if (icon)
                    {
                        Color sample = icon.GetPixelBilinear(
                            (x - 0.5f) / ContentSizePixels,
                            (y - 0.5f) / ContentSizePixels);
                        if (sample.a > SourceAlphaThreshold)
                        {
                            pixels[output] = (Color32)sample;
                        }
                    }
                }
            }
        }

        Texture2D nextTexture = new Texture2D(
            width,
            IconSizePixels,
            TextureFormat.RGBA32,
            false,
            false);
        nextTexture.name = "RecoveredMonsterStatusIconsTexture";
        nextTexture.filterMode = FilterMode.Point;
        nextTexture.wrapMode = TextureWrapMode.Clamp;
        nextTexture.SetPixels32(pixels);
        nextTexture.Apply(false, false);
        Sprite nextSprite = Sprite.Create(
            nextTexture,
            new Rect(0f, 0f, width, IconSizePixels),
            new Vector2(0.5f, 1f),
            ReferenceWorldLocalPlayerDoll.PixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
        if (!nextSprite)
        {
            DestroyOwned(nextTexture);
            return false;
        }

        nextSprite.name = "RecoveredMonsterStatusIconsSprite";
        Sprite oldSprite = sprite;
        Texture2D oldTexture = texture;
        sprite = nextSprite;
        texture = nextTexture;
        renderer.sprite = sprite;
        renderedSignature = nextSignature;
        DestroyOwned(oldSprite);
        DestroyOwned(oldTexture);
        return true;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
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
}
