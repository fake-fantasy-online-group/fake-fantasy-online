using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Shared decoded source assets for the official baowupiaoshi ground sparkle.
/// </summary>
internal sealed class ReferenceWorldGroundSparkleAssets : IDisposable
{
    public const string EffectPath = "effect/baowupiaoshi.eff";
    public const string Magic343Path = "magic/magic/magic0343.gso";
    public const string Magic344Path = "magic/magic/magic0344.gso";
    public const float FrameSeconds = 0.06f;
    public const float Magic343LifetimeSeconds = 0.75f;
    public const float Magic344LifetimeSeconds = 0.9f;

    private readonly List<Texture2D> textures = new List<Texture2D>();
    private readonly List<Sprite> sprites = new List<Sprite>();
    private bool disposed;

    private ReferenceWorldGroundSparkleAssets(
        ReferenceGsoImage magic343,
        ReferenceGsoImage magic344,
        Sprite[] magic343Sprites,
        Sprite[] magic344Sprites)
    {
        Magic343 = magic343;
        Magic344 = magic344;
        Magic343Sprites = magic343Sprites;
        Magic344Sprites = magic344Sprites;
    }

    public ReferenceGsoImage Magic343 { get; }
    public ReferenceGsoImage Magic344 { get; }
    public Sprite[] Magic343Sprites { get; }
    public Sprite[] Magic344Sprites { get; }

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        out ReferenceWorldGroundSparkleAssets assets)
    {
        assets = null;
        if (resources == null)
        {
            return false;
        }

        byte[] template = resources.ReadRes(EffectPath);
        ReferenceGsoImage magic343;
        ReferenceGsoImage magic344;
        if (!MatchesLockedTemplate(template) ||
            !ReferenceGsoDecoder.TryDecode(resources.ReadRes(Magic343Path), out magic343) ||
            !ReferenceGsoDecoder.TryDecode(resources.ReadRes(Magic344Path), out magic344) ||
            magic343 == null || magic344 == null ||
            magic343.FrameCount != 4 || magic344.FrameCount != 4)
        {
            return false;
        }

        ReferenceWorldGroundSparkleAssets candidate =
            new ReferenceWorldGroundSparkleAssets(
                magic343,
                magic344,
                new Sprite[magic343.FrameCount],
                new Sprite[magic344.FrameCount]);
        if (!candidate.BuildSprites(magic343, candidate.Magic343Sprites) ||
            !candidate.BuildSprites(magic344, candidate.Magic344Sprites))
        {
            candidate.Dispose();
            return false;
        }

        assets = candidate;
        return true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        for (int index = 0; index < sprites.Count; index++)
        {
            DestroyOwned(sprites[index]);
        }

        for (int index = 0; index < textures.Count; index++)
        {
            DestroyOwned(textures[index]);
        }

        sprites.Clear();
        textures.Clear();
    }

    public static Vector3 FrameLocalPosition(
        ReferenceGsoImage image,
        int frameIndex,
        float pixelsPerUnit)
    {
        ReferenceGsoFrame frame = image.Frames[frameIndex];
        return new Vector3(
            (frame.Left - image.BoundCenterX) / pixelsPerUnit,
            (image.BoundCenterY - frame.Top) / pixelsPerUnit,
            0f);
    }

    private bool BuildSprites(ReferenceGsoImage image, Sprite[] destination)
    {
        for (int frameIndex = 0; frameIndex < image.Frames.Count; frameIndex++)
        {
            ReferenceGsoFrame frame = image.Frames[frameIndex];
            Color32[] bottomUp = new Color32[frame.Pixels.Length];
            for (int sourceY = 0; sourceY < frame.Height; sourceY++)
            {
                Array.Copy(
                    frame.Pixels,
                    sourceY * frame.Width,
                    bottomUp,
                    (frame.Height - sourceY - 1) * frame.Width,
                    frame.Width);
            }

            Texture2D texture = new Texture2D(
                frame.Width,
                frame.Height,
                TextureFormat.RGBA32,
                false,
                false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(bottomUp);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, frame.Width, frame.Height),
                new Vector2(0f, 1f),
                ReferenceWorldLocalPlayerDoll.PixelsPerUnit);
            if (!sprite)
            {
                DestroyOwned(texture);
                return false;
            }

            textures.Add(texture);
            sprites.Add(sprite);
            destination[frameIndex] = sprite;
        }

        return true;
    }

    private static bool MatchesLockedTemplate(byte[] bytes)
    {
        if (bytes == null || bytes.Length != 569)
        {
            return false;
        }

        string text;
        try
        {
            text = System.Text.Encoding.ASCII.GetString(bytes);
        }
        catch (ArgumentException)
        {
            return false;
        }

        return text.Contains("(344, ('magic344', 0, 60, 0)") &&
               text.Contains("[660,") && text.Contains("[900,") &&
               text.Contains("0.69999999999999996") &&
               text.Contains("(343, ('magic343', 0, 60, 0)") &&
               text.Contains("[510,") && text.Contains("[750,") &&
               text.Contains("0.80000000000000004");
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

internal struct ReferenceWorldGroundPickupPlan
{
    public ReferenceWorldGroundPickupPlan(int requestGroundId, bool stopMovement)
    {
        RequestGroundId = requestGroundId;
        StopMovement = stopMovement;
    }

    public int RequestGroundId { get; }
    public bool StopMovement { get; }
}

/// <summary>
/// Exact pending-id, 15-second expiry and 48-pixel single-send state machine.
/// </summary>
internal sealed class ReferenceWorldGroundPickupController
{
    private int groundId = -1;
    private int x;
    private int y;
    private float expire;

    public int GroundId => groundId;
    public int X => x;
    public int Y => y;

    public bool Begin(int nextGroundId, int nextX, int nextY, float realtime)
    {
        if (nextGroundId <= 0 || realtime < 0f || float.IsNaN(realtime) ||
            float.IsInfinity(realtime))
        {
            return false;
        }

        groundId = nextGroundId;
        x = nextX;
        y = nextY;
        expire = realtime + ReferenceWorldGroundItemActor.PickupExpireSeconds;
        return true;
    }

    public bool TryAdvance(
        float playerX,
        float playerY,
        float realtime,
        out ReferenceWorldGroundPickupPlan plan)
    {
        plan = new ReferenceWorldGroundPickupPlan(0, false);
        if (realtime < 0f || float.IsNaN(realtime) || float.IsInfinity(realtime) ||
            float.IsNaN(playerX) || float.IsInfinity(playerX) ||
            float.IsNaN(playerY) || float.IsInfinity(playerY))
        {
            return false;
        }

        if (groundId < 0)
        {
            return true;
        }

        if (realtime > expire)
        {
            groundId = -1;
            return true;
        }

        float dx = playerX - x;
        float dy = playerY - y;
        float range = ReferenceWorldGroundItemActor.PickupRangePixels;
        if (dx * dx + dy * dy > range * range)
        {
            return true;
        }

        int requested = groundId;
        groundId = -1;
        plan = new ReferenceWorldGroundPickupPlan(requested, true);
        return true;
    }

    public void Cancel()
    {
        groundId = -1;
    }
}

/// <summary>
/// Source-recovered Unity projection of one authoritative ground-item row.
/// </summary>
internal sealed class ReferenceWorldGroundItemActor : IDisposable
{
    public const float SparklePeriodSeconds = 3f;
    public const float PickupRangePixels = 48f;
    public const float PickupExpireSeconds = 15f;

    private readonly ReferenceGsmMap map;
    private readonly Transform parent;
    private readonly Camera camera;
    private readonly ReferenceItemIconCatalog icons;
    private readonly ReferenceWorldBitmapFont font;
    private readonly WorldGroundItemState state;
    private readonly float lockEnd;
    private GameObject root;
    private SpriteRenderer renderer;
    private BoxCollider2D pickBox;
    private Sprite iconSprite;
    private readonly GroundItems.Item sourceItem = new GroundItems.Item();
    private GroundItems sourceOwner;
    private bool nativeReleased;
    private bool disposed;

    public ReferenceWorldGroundItemActor(
        ReferenceGsmMap map,
        Transform parent,
        Camera camera,
        ReferenceItemIconCatalog icons,
        ReferenceWorldGroundSparkleAssets sparkleAssets,
        ReferenceWorldBitmapFont font,
        WorldGroundItemState state,
        float receiveRealtime)
    {
        this.map = map ?? throw new ArgumentNullException(nameof(map));
        this.parent = parent == null ? throw new ArgumentNullException(nameof(parent)) : parent;
        this.camera = camera == null ? throw new ArgumentNullException(nameof(camera)) : camera;
        this.icons = icons ?? throw new ArgumentNullException(nameof(icons));
        this.font = font ?? throw new ArgumentNullException(nameof(font));
        this.state = state ?? throw new ArgumentNullException(nameof(state));
        lockEnd = receiveRealtime + state.LockMilliseconds / 1000f;
    }

    private ReferenceWorldGroundItemActor(WorldGroundItemState state, GroundItems owner, GroundItems.Item item)
    {
        this.state = state;
        sourceOwner = owner;
        sourceItem = item;
        root = item.go;
        renderer = item.sr;
        pickBox = root != null ? root.GetComponent<BoxCollider2D>() : null;
    }
    internal static ReferenceWorldGroundItemActor AdoptForIndependentRuntime(WorldGroundItemState state,
        GroundItems owner, GroundItems.Item item) => new ReferenceWorldGroundItemActor(state, owner, item);
    internal bool ReferencesForIndependentRuntime(GroundItems.Item item) => ReferenceEquals(sourceItem, item);

    public WorldGroundItemState State => state;
    public GameObject Root => root;
    public BoxCollider2D PickBox => pickBox;
    public bool Built => root && renderer && pickBox;
    public string DisplayName => state.Count > 1
        ? state.DisplayName + " ×" + state.Count
        : state.DisplayName;

    public bool Build()
    {
        if (disposed || root)
        {
            return false;
        }

        Texture2D icon = GroundItems.LoadIconForIndependentRuntime(state.ItemId);
        root = new GameObject("Ground:" + state.GroundId + ":" + state.DisplayName);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(
            (state.X - map.MapWidth * 0.5f) / ReferenceWorldLocalPlayerDoll.PixelsPerUnit,
            (map.MapHeight * 0.5f - state.Y) / ReferenceWorldLocalPlayerDoll.PixelsPerUnit,
            0f);
        renderer = root.AddComponent<SpriteRenderer>();
        sourceItem.go = root;
        sourceItem.sr = renderer;
        sourceItem.name = DisplayName;
        sourceItem.owned = state.Owned;
        sourceItem.lockEnd = Time.time + state.LockMilliseconds / 1000f;
        sourceItem.kind = state.Kind;
        sourceItem.quality = state.Quality;
        sourceItem.itemId = state.ItemId;
        renderer.sortingOrder = Mathf.Clamp(state.Y, 0, 30000);
        if (icon)
        {
            iconSprite = GsaTextureScale.MakeSprite(
                icon,
                ReferenceWorldLocalPlayerDoll.PixelsPerUnit,
                0.5f,
                0.2f);
            renderer.sprite = iconSprite;
        }

        pickBox = root.AddComponent<BoxCollider2D>();
        pickBox.size = renderer.sprite
            ? (Vector2)renderer.sprite.bounds.size * 2f
            : new Vector2(0.4f, 0.4f);
        GroundItemEntity entity = root.AddComponent<GroundItemEntity>();
        entity.id = state.GroundId;
        entity.gx = state.X;
        entity.gy = state.Y;
        return Built;
    }

    internal void RegisterForIndependentRuntime(GroundItems owner)
    {
        sourceOwner = owner;
        owner.RegisterForIndependentRuntime(state.GroundId, sourceItem);
    }
    internal void NoteNativeReleasedForIndependentRuntime() => nativeReleased = true;

    public bool Matches(WorldGroundItemState candidate)
    {
        return candidate != null &&
               candidate.GroundId == state.GroundId && candidate.ItemId == state.ItemId &&
               candidate.Count == state.Count && candidate.X == state.X && candidate.Y == state.Y &&
               candidate.DisplayName == state.DisplayName && candidate.Owned == state.Owned &&
               candidate.LockMilliseconds == state.LockMilliseconds &&
               candidate.Kind == state.Kind && candidate.Quality == state.Quality;
    }

    public bool IsProtected(float realtime)
    {
        return !sourceItem.owned && !(Time.time >= sourceItem.lockEnd);
    }

    public bool TryAdvance(float deltaTime) =>
        Built && deltaTime >= 0f && !float.IsNaN(deltaTime) && !float.IsInfinity(deltaTime);

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (!nativeReleased)
        {
            nativeReleased = sourceOwner != null && sourceOwner.ReleaseForIndependentRuntime(state.GroundId, sourceItem);
            if (!nativeReleased) GroundItems.DropSpriteForIndependentRuntime(sourceItem);
        }
        iconSprite = null;
        if (!nativeReleased && root) DestroyOwned(root);
        root = null;
        sourceOwner = null;

        renderer = null;
        pickBox = null;
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
