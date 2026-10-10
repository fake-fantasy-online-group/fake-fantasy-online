using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source translation of the reviewed hostile branch in GameHud.TargetNameColor.
/// </summary>
internal static class ReferenceWorldMonsterFeedbackMath
{
    public const byte OpaqueAlphaThreshold = 17;
    public const float NameOffsetUnits = 0.15f;
    public const float HeadHpOffsetPixels = 17f;

    public static Color32 HostileNameColor(int monsterLevel, int localPlayerLevel)
    {
        int difference = monsterLevel - localPlayerLevel;
        if (difference >= 10)
        {
            return new Color32(246, 18, 200, 255);
        }

        if (difference >= 3)
        {
            return new Color32(253, 91, 20, 255);
        }

        if (difference >= 0)
        {
            return new Color32(236, 215, 33, 255);
        }

        if (difference >= -9)
        {
            return new Color32(77, 197, 11, 255);
        }

        return new Color32(190, 180, 180, 255);
    }

    /// <summary>
    /// Exact readable-texture alpha scan used by MonsterEntity.ComputeHeadTopUncached.
    /// </summary>
    public static float ComputeHeadTop(DimgFrame frame, Sprite sprite)
    {
        if (frame == null || !frame.HasPixels)
        {
            return sprite ? sprite.bounds.max.y : 1f;
        }

        int firstOpaqueTopRow = -1;
        for (int sourceY = 0; sourceY < frame.Height && firstOpaqueTopRow < 0; sourceY++)
        {
            int rowOffset = sourceY * frame.Width;
            for (int sourceX = 0; sourceX < frame.Width; sourceX++)
            {
                if (frame.Pixels[rowOffset + sourceX].a >= OpaqueAlphaThreshold)
                {
                    firstOpaqueTopRow = sourceY;
                    break;
                }
            }
        }

        if (firstOpaqueTopRow < 0)
        {
            return sprite ? sprite.bounds.max.y : 1f;
        }

        int anchorY = frame.BoundCenterY - frame.Top;
        return (anchorY - firstOpaqueTopRow) /
            ReferenceWorldLocalPlayerDoll.PixelsPerUnit;
    }
}

/// <summary>
/// Disposable source translation of the hostile MonsterEntity.UpdateNameTag branch.
/// </summary>
internal sealed class ReferenceWorldMonsterNameTagView : IDisposable
{
    public const int SortingOrder = 32000;

    private readonly ReferenceWorldRemoteFeedbackAssets assets;
    private GameObject root;
    private SpriteRenderer renderer;
    private Sprite sprite;
    private Texture2D texture;
    private string lastName;
    private Color32 lastColor;
    private bool lastReady;
    private bool disposed;

    public ReferenceWorldMonsterNameTagView(ReferenceWorldRemoteFeedbackAssets assets)
    {
        this.assets = assets ?? throw new ArgumentNullException(nameof(assets));
    }

    public GameObject Root => root;
    public SpriteRenderer Renderer => renderer;
    public Texture2D Texture => texture;
    public Color32 LastColor => lastColor;

    public bool TryTick(
        Transform owner,
        bool show,
        string displayName,
        Color32 color,
        float localY,
        Camera worldCamera,
        float uiScale,
        int screenHeight)
    {
        if (disposed || owner == null)
        {
            return false;
        }

        bool ready = assets.Font.Ready;
        if (!show || string.IsNullOrEmpty(displayName) || !ready)
        {
            if (root && root.activeSelf)
            {
                root.SetActive(false);
            }

            lastReady = ready;
            return true;
        }

        bool changed = !root || lastName != displayName ||
                       !SameColor(lastColor, color) || lastReady != ready;
        if (changed && !TryRebuild(owner, displayName, color))
        {
            return false;
        }

        lastName = displayName;
        lastColor = color;
        lastReady = ready;
        if (!root.activeSelf)
        {
            root.SetActive(true);
        }

        PlaceSharp(root.transform, localY, worldCamera, uiScale, screenHeight);
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
    }

    private bool TryRebuild(Transform owner, string displayName, Color32 color)
    {
        Texture2D nextTexture = assets.Font.BakeNameTag(
            displayName,
            string.Empty,
            color,
            color,
            1,
            null,
            null,
            null,
            null,
            null);
        if (!nextTexture)
        {
            return false;
        }

        Sprite nextSprite = Sprite.Create(
            nextTexture,
            new Rect(0f, 0f, nextTexture.width, nextTexture.height),
            new Vector2(0.5f, 0f),
            ReferenceWorldLocalPlayerDoll.PixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
        if (!nextSprite)
        {
            DestroyOwned(nextTexture);
            return false;
        }

        nextSprite.name = "RecoveredMonsterNameTagSprite";
        if (!root)
        {
            root = new GameObject("RecoveredMonsterNameTag");
            root.transform.SetParent(owner, false);
            renderer = root.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = SortingOrder;
            Material material = assets.NameTagMaterial;
            if (material)
            {
                renderer.sharedMaterial = material;
            }
        }

        Sprite oldSprite = sprite;
        Texture2D oldTexture = texture;
        sprite = nextSprite;
        texture = nextTexture;
        renderer.sprite = sprite;
        DestroyOwned(oldSprite);
        DestroyOwned(oldTexture);
        return true;
    }

    internal static void PlaceSharp(
        Transform tag,
        float localY,
        Camera worldCamera,
        float uiScale,
        int screenHeight)
    {
        tag.localPosition = new Vector3(
            0f,
            ReferenceWorldHeadHpBarMath.PixelSnap(
                localY,
                ReferenceWorldLocalPlayerDoll.PixelsPerUnit),
            0f);
        float scale = ReferenceWorldHeadHpBarMath.ScreenScale(
            ReferenceWorldLocalPlayerDoll.PixelsPerUnit,
            uiScale,
            worldCamera != null && worldCamera.orthographic,
            worldCamera == null ? 0f : worldCamera.orthographicSize,
            screenHeight);
        tag.localScale = new Vector3(scale, scale, 1f);
    }

    private static bool SameColor(Color32 left, Color32 right)
    {
        return left.r == right.r && left.g == right.g && left.b == right.b &&
               left.a == right.a;
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
