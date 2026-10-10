using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Deterministic source translation of the legacy branch of FloorRenderer.
/// </summary>
/// <remarks>
/// The planning path translates Refresh (06000A2A), RefreshLegacy (06000A2B),
/// and IsAnimated (06000A29). It consumes raw GSM terrain values; GsmMap.VCode
/// is a separate gameplay projection and is not the floor image lookup.
/// </remarks>
public static class ReferenceLegacyFloorPlanner
{
    public const int TileWidth = 128;
    public const int TileHeight = 64;
    public const int CellWidth = 64;
    public const int CellHeight = 32;
    public const int Margin = 1;

    public static ReferenceLegacyFloorPixelBounds PixelBoundsForCamera(
        Camera camera,
        float pixelsPerUnit,
        float worldWidth,
        float worldHeight)
    {
        if (camera == null)
        {
            throw new ArgumentNullException(nameof(camera));
        }

        if (!(pixelsPerUnit > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit));
        }

        float size = camera.orthographicSize;
        float halfWidth = size * camera.aspect;
        Vector3 position = camera.transform.position;
        float x0 = pixelsPerUnit * (position.x - halfWidth) + worldWidth * 0.5f;
        float x1 = pixelsPerUnit * (position.x + halfWidth) + worldWidth * 0.5f;
        float y0 = worldHeight * 0.5f - pixelsPerUnit * (position.y + size);
        float y1 = worldHeight * 0.5f - pixelsPerUnit * (position.y - size);
        return new ReferenceLegacyFloorPixelBounds(x0, x1, y0, y1);
    }

    public static ReferenceLegacyFloorPlan Plan(
        ReferenceGsmMap map,
        ReferenceTerrainShapeLibrary shapes,
        ReferenceLegacyFloorPixelBounds pixels,
        int frame)
    {
        if (map == null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        if (shapes == null)
        {
            throw new ArgumentNullException(nameof(shapes));
        }

        ReferenceLegacyFloorIndexBounds bounds = IndexBoundsForPixels(map, pixels);
        return Plan(map, shapes, bounds, frame);
    }

    public static ReferenceLegacyFloorIndexBounds IndexBoundsForPixels(
        ReferenceGsmMap map,
        ReferenceLegacyFloorPixelBounds pixels)
    {
        if (map == null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        int originX = map.GridHeight * CellWidth - CellWidth;
        return new ReferenceLegacyFloorIndexBounds(
            Mathf.FloorToInt(
                (pixels.MinimumX - originX - TileWidth) / CellWidth) - Margin,
            Mathf.CeilToInt(
                (pixels.MaximumX - originX) / CellWidth) + Margin,
            Mathf.FloorToInt(
                (pixels.MinimumY - TileHeight) / CellHeight) - Margin,
            Mathf.CeilToInt(pixels.MaximumY / CellHeight) + Margin);
    }

    internal static ReferenceLegacyFloorPlan Plan(
        ReferenceGsmMap map,
        ReferenceTerrainShapeLibrary shapes,
        ReferenceLegacyFloorIndexBounds bounds,
        int frame)
    {
        int minA = bounds.MinimumA;
        int maxA = bounds.MaximumA;
        int minB = bounds.MinimumB;
        int maxB = bounds.MaximumB;

        Dictionary<int, bool> animationCache = new Dictionary<int, bool>();
        List<ReferenceLegacyFloorCell> cells = new List<ReferenceLegacyFloorCell>();
        if (minA <= maxA && minB <= maxB)
        {
            for (int b = minB; b <= maxB; b++)
            {
                for (int a = minA; a <= maxA; a++)
                {
                    if (((a ^ b) & 1) != 0)
                    {
                        continue;
                    }

                    int gridX = (b + a) >> 1;
                    int gridY = (b - a) >> 1;
                    if ((gridX | gridY) < 0 ||
                        gridX >= map.GridWidth || gridY >= map.GridHeight)
                    {
                        continue;
                    }

                    int code = map.Grids[gridY * map.GridWidth + gridX];
                    if (code < 0)
                    {
                        continue;
                    }

                    bool animated;
                    if (!animationCache.TryGetValue(code, out animated))
                    {
                        animated = IsAnimated(shapes, code);
                        animationCache[code] = animated;
                    }

                    int imageId = shapes.GetTile(code, animated ? frame : 0);
                    if (imageId < 0)
                    {
                        continue;
                    }

                    long cellId = ((long)gridX << 32) | (uint)gridY;
                    int pixelX = ((map.GridHeight - gridY + gridX) * CellWidth) -
                                 CellWidth;
                    int pixelY = (gridY + gridX) * CellHeight;
                    cells.Add(new ReferenceLegacyFloorCell(
                        cellId,
                        gridX,
                        gridY,
                        code,
                        animated,
                        imageId,
                        pixelX,
                        pixelY));
                }
            }
        }

        return new ReferenceLegacyFloorPlan(
            minA,
            maxA,
            minB,
            maxB,
            frame,
            cells.ToArray());
    }

    public static bool IsAnimated(ReferenceTerrainShapeLibrary shapes, int code)
    {
        if (shapes == null)
        {
            throw new ArgumentNullException(nameof(shapes));
        }

        int first = shapes.GetTile(code, 0);
        return shapes.GetTile(code, 1) != first ||
               shapes.GetTile(code, 2) != first ||
               shapes.GetTile(code, 3) != first;
    }
}

public struct ReferenceLegacyFloorPixelBounds
{
    public ReferenceLegacyFloorPixelBounds(
        float minimumX,
        float maximumX,
        float minimumY,
        float maximumY)
    {
        MinimumX = minimumX;
        MaximumX = maximumX;
        MinimumY = minimumY;
        MaximumY = maximumY;
    }

    public float MinimumX { get; }
    public float MaximumX { get; }
    public float MinimumY { get; }
    public float MaximumY { get; }
}

public struct ReferenceLegacyFloorIndexBounds
{
    public ReferenceLegacyFloorIndexBounds(
        int minimumA,
        int maximumA,
        int minimumB,
        int maximumB)
    {
        MinimumA = minimumA;
        MaximumA = maximumA;
        MinimumB = minimumB;
        MaximumB = maximumB;
    }

    public int MinimumA { get; }
    public int MaximumA { get; }
    public int MinimumB { get; }
    public int MaximumB { get; }
}

public sealed class ReferenceLegacyFloorPlan
{
    private readonly ReferenceLegacyFloorCell[] cells;

    public ReferenceLegacyFloorPlan(
        int minimumA,
        int maximumA,
        int minimumB,
        int maximumB,
        int frame,
        ReferenceLegacyFloorCell[] cells)
    {
        MinimumA = minimumA;
        MaximumA = maximumA;
        MinimumB = minimumB;
        MaximumB = maximumB;
        Frame = frame;
        this.cells = cells ?? Array.Empty<ReferenceLegacyFloorCell>();
        Cells = Array.AsReadOnly(this.cells);
    }

    public int MinimumA { get; }
    public int MaximumA { get; }
    public int MinimumB { get; }
    public int MaximumB { get; }
    public int Frame { get; }
    public IReadOnlyList<ReferenceLegacyFloorCell> Cells { get; }
}

public sealed class ReferenceLegacyFloorCell
{
    public ReferenceLegacyFloorCell(
        long cellId,
        int gridX,
        int gridY,
        int code,
        bool animated,
        int imageId,
        int pixelX,
        int pixelY)
    {
        CellId = cellId;
        GridX = gridX;
        GridY = gridY;
        Code = code;
        Animated = animated;
        ImageId = imageId;
        PixelX = pixelX;
        PixelY = pixelY;
    }

    public long CellId { get; }
    public int GridX { get; }
    public int GridY { get; }
    public int Code { get; }
    public bool Animated { get; }
    public int ImageId { get; }
    public int PixelX { get; }
    public int PixelY { get; }
}

/// <summary>
/// Unity lifecycle owner for the recovered FloorRenderer.LateUpdate method.
/// </summary>
/// <remarks>
/// The official 1.3.4 method at token 06000A24 checks the initialized flag and invokes
/// Refresh(false) once from LateUpdate. CameraFollow owns its separate LateUpdate; neither owner
/// is driven again from the containing GameBootstrap/ClientRootView Update path.
/// </remarks>
[DisallowMultipleComponent]
public sealed class ReferenceLegacyFloorLateUpdateOwner : MonoBehaviour
{
    private ReferenceLegacyFloorRenderer floorRenderer;
    private Camera cameraComponent;

    internal bool Configured => floorRenderer != null && cameraComponent != null;
    internal int RefreshCallCount { get; private set; }

    internal bool Configure(
        ReferenceLegacyFloorRenderer valueRenderer,
        Camera valueCamera)
    {
        if (valueRenderer == null || valueCamera == null || Configured)
        {
            return false;
        }

        floorRenderer = valueRenderer;
        cameraComponent = valueCamera;
        return true;
    }

    internal void Release()
    {
        floorRenderer = null;
        cameraComponent = null;
    }

    internal bool RefreshForAudit()
    {
        return RefreshOwnedFloor();
    }

    private void LateUpdate()
    {
        RefreshOwnedFloor();
    }

    private bool RefreshOwnedFloor()
    {
        if (!Configured)
        {
            return false;
        }

        RefreshCallCount++;
        return floorRenderer.Refresh(cameraComponent, false);
    }
}

/// <summary>
/// Unity-facing owner for the recovered legacy floor sprites.
/// </summary>
/// <remarks>
/// This translates Place (06000A2D), Cull (06000A2E), NewSR (06000A2F),
/// GetLegacySprite (06000A30), and the legacy portion of TickAnim (06000A25).
/// The class owns every Sprite, Texture2D, and child renderer that it creates.
/// </remarks>
public sealed class ReferenceLegacyFloorRenderer : IDisposable
{
    public const float DefaultPixelsPerUnit = 100f;
    public const int DefaultSortingOrder = -10000;
    public const float DefaultTerrainFrameSeconds = 0.2f;

    private readonly ReferenceGsmMap map;
    private readonly ReferenceTileImageLibrary tiles;
    private readonly ReferenceTerrainShapeLibrary shapes;
    private readonly Transform root;
    private readonly Material material;
    private readonly float pixelsPerUnit;
    private readonly float worldWidth;
    private readonly float worldHeight;
    private readonly int sortingOrder;
    private readonly Dictionary<int, Sprite> spriteCache;
    private readonly Dictionary<long, SpriteRenderer> active;
    private readonly Stack<SpriteRenderer> pool;
    private readonly HashSet<long> visibleNow;
    private readonly List<long> toRemove;
    private readonly Dictionary<long, int> animationCodes;

    private int lastMinimumA;
    private int lastMaximumA;
    private int lastMinimumB;
    private int lastMaximumB;
    private int currentFrame;
    private bool disposed;

    public ReferenceLegacyFloorRenderer(
        ReferenceGsmMap map,
        ReferenceTileImageLibrary tiles,
        ReferenceTerrainShapeLibrary shapes,
        Transform root,
        Material material = null,
        float pixelsPerUnit = DefaultPixelsPerUnit,
        int sortingOrder = DefaultSortingOrder)
    {
        this.map = map ?? throw new ArgumentNullException(nameof(map));
        this.tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
        this.shapes = shapes ?? throw new ArgumentNullException(nameof(shapes));
        this.root = root == null ? throw new ArgumentNullException(nameof(root)) : root;
        if (!map.Ok || !map.IsLegacy)
        {
            throw new ArgumentException("The recovered floor renderer requires a valid legacy map.",
                nameof(map));
        }

        if (!(pixelsPerUnit > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit));
        }

        this.material = material;
        this.pixelsPerUnit = pixelsPerUnit;
        this.sortingOrder = sortingOrder;
        worldWidth = map.MapWidth;
        worldHeight = map.MapHeight;
        spriteCache = new Dictionary<int, Sprite>();
        active = new Dictionary<long, SpriteRenderer>();
        pool = new Stack<SpriteRenderer>();
        visibleNow = new HashSet<long>();
        toRemove = new List<long>();
        animationCodes = new Dictionary<long, int>();
        lastMinimumA = int.MaxValue;
    }

    public int ActiveCount => active.Count;
    public int PooledCount => pool.Count;
    public int RendererObjectCount => active.Count + pool.Count;
    public int CachedSpriteCount => spriteCache.Count;
    public int AnimatedCellCount => animationCodes.Count;
    public int CurrentFrame => currentFrame;

    public bool Refresh(Camera camera, bool force)
    {
        ThrowIfDisposed();
        ReferenceLegacyFloorPixelBounds pixels =
            ReferenceLegacyFloorPlanner.PixelBoundsForCamera(
                camera,
                pixelsPerUnit,
                worldWidth,
                worldHeight);
        return RefreshPixels(pixels, currentFrame, force);
    }

    public bool RefreshPixels(
        ReferenceLegacyFloorPixelBounds pixels,
        int frame,
        bool force)
    {
        ThrowIfDisposed();
        // The official RefreshLegacy branch stores and compares these four bounds before it
        // clears/creates any visible-cell collections. Preserve that early return so a stationary
        // or sub-cell camera does not allocate a complete floor plan every rendered frame.
        ReferenceLegacyFloorIndexBounds bounds =
            ReferenceLegacyFloorPlanner.IndexBoundsForPixels(map, pixels);
        if (!force && bounds.MinimumA == lastMinimumA &&
            bounds.MaximumA == lastMaximumA && bounds.MinimumB == lastMinimumB &&
            bounds.MaximumB == lastMaximumB)
        {
            return false;
        }

        ReferenceLegacyFloorPlan plan =
            ReferenceLegacyFloorPlanner.Plan(map, shapes, bounds, frame);
        lastMinimumA = bounds.MinimumA;
        lastMaximumA = bounds.MaximumA;
        lastMinimumB = bounds.MinimumB;
        lastMaximumB = bounds.MaximumB;
        visibleNow.Clear();

        foreach (ReferenceLegacyFloorCell cell in plan.Cells)
        {
            visibleNow.Add(cell.CellId);
            if (!active.ContainsKey(cell.CellId))
            {
                Sprite sprite = GetLegacySprite(cell.ImageId);
                if (sprite != null)
                {
                    Place(cell, sprite);
                }
            }

            if (cell.Animated)
            {
                animationCodes[cell.CellId] = cell.Code;
            }
        }

        Cull();
        return true;
    }

    public bool TickAnimation(float elapsedSeconds)
    {
        ThrowIfDisposed();
        if (animationCodes.Count < 1)
        {
            return false;
        }

        float frameSeconds = Mathf.Max(0.02f, DefaultTerrainFrameSeconds);
        int nextFrame = ((int)(elapsedSeconds / frameSeconds)) & 3;
        if (nextFrame == currentFrame)
        {
            return false;
        }

        currentFrame = nextFrame;
        foreach (KeyValuePair<long, int> entry in animationCodes)
        {
            int imageId = shapes.GetTile(entry.Value, currentFrame);
            if (imageId < 0)
            {
                continue;
            }

            SpriteRenderer renderer;
            if (active.TryGetValue(entry.Key, out renderer) && renderer != null)
            {
                Sprite sprite = GetLegacySprite(imageId);
                if (sprite != null)
                {
                    renderer.sprite = sprite;
                }
            }
        }

        return true;
    }

    public bool TryGetActiveRenderer(long cellId, out SpriteRenderer renderer)
    {
        ThrowIfDisposed();
        return active.TryGetValue(cellId, out renderer);
    }

    public bool TryGetCachedSprite(int imageId, out Sprite sprite)
    {
        ThrowIfDisposed();
        return spriteCache.TryGetValue(imageId, out sprite);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        HashSet<GameObject> rendererObjects = new HashSet<GameObject>();
        foreach (SpriteRenderer renderer in active.Values)
        {
            if (renderer != null)
            {
                rendererObjects.Add(renderer.gameObject);
            }
        }

        foreach (SpriteRenderer renderer in pool)
        {
            if (renderer != null)
            {
                rendererObjects.Add(renderer.gameObject);
            }
        }

        foreach (GameObject value in rendererObjects)
        {
            DestroyOwned(value);
        }

        foreach (Sprite sprite in spriteCache.Values)
        {
            if (sprite == null)
            {
                continue;
            }

            Texture2D texture = sprite.texture;
            DestroyOwned(sprite);
            if (texture != null)
            {
                DestroyOwned(texture);
            }
        }

        active.Clear();
        pool.Clear();
        spriteCache.Clear();
        visibleNow.Clear();
        toRemove.Clear();
        animationCodes.Clear();
    }

    private Sprite GetLegacySprite(int imageId)
    {
        Sprite cached;
        if (spriteCache.TryGetValue(imageId, out cached))
        {
            return cached;
        }

        Color32[] decoded;
        if (!tiles.TryDecodeTile(imageId, out decoded) || decoded == null)
        {
            return null;
        }

        Texture2D texture = new Texture2D(
            ReferenceTileImageLibrary.TileWidth,
            ReferenceTileImageLibrary.TileHeight,
            TextureFormat.RGBA32,
            false,
            false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32[] flipped = new Color32[decoded.Length];
        for (int sourceRow = 0;
             sourceRow < ReferenceTileImageLibrary.TileHeight;
             sourceRow++)
        {
            int destinationRow = ReferenceTileImageLibrary.TileHeight - sourceRow - 1;
            Array.Copy(
                decoded,
                sourceRow * ReferenceTileImageLibrary.TileWidth,
                flipped,
                destinationRow * ReferenceTileImageLibrary.TileWidth,
                ReferenceTileImageLibrary.TileWidth);
        }

        texture.SetPixels32(flipped);
        texture.Apply(false, false);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(
                0f,
                0f,
                ReferenceTileImageLibrary.TileWidth,
                ReferenceTileImageLibrary.TileHeight),
            Vector2.zero,
            pixelsPerUnit);
        spriteCache[imageId] = sprite;
        return sprite;
    }

    private void Place(ReferenceLegacyFloorCell cell, Sprite sprite)
    {
        SpriteRenderer renderer = pool.Count > 0 ? pool.Pop() : NewRenderer();
        renderer.sprite = sprite;
        renderer.enabled = true;
        renderer.gameObject.SetActive(true);
        renderer.transform.position = new Vector3(
            (cell.PixelX - worldWidth * 0.5f) / pixelsPerUnit,
            (worldHeight * 0.5f - cell.PixelY) / pixelsPerUnit,
            0f);
        renderer.transform.localScale = Vector3.one;
        active[cell.CellId] = renderer;
    }

    private SpriteRenderer NewRenderer()
    {
        GameObject value = new GameObject("Floor");
        value.transform.SetParent(root, false);
        SpriteRenderer renderer = value.AddComponent<SpriteRenderer>();
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }

        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void Cull()
    {
        toRemove.Clear();
        foreach (KeyValuePair<long, SpriteRenderer> entry in active)
        {
            if (!visibleNow.Contains(entry.Key))
            {
                toRemove.Add(entry.Key);
            }
        }

        foreach (long cellId in toRemove)
        {
            SpriteRenderer renderer = active[cellId];
            active.Remove(cellId);
            animationCodes.Remove(cellId);
            if (renderer != null)
            {
                renderer.gameObject.SetActive(false);
                pool.Push(renderer);
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceLegacyFloorRenderer));
        }
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
