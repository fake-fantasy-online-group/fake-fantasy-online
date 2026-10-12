using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public enum ReferenceMapObjectKind
{
    Tile = 0,
    Library = 1
}

/// <summary>
/// One full-image or horizontally sliced SpriteRenderer placement.
/// </summary>
public sealed class ReferenceLegacyMapObjectPlacement
{
    public ReferenceLegacyMapObjectPlacement(
        ReferenceMapObjectKind kind,
        int objectIndex,
        int imageId,
        string resourcePath,
        int rectangleX,
        int rectangleWidth,
        int rectangleHeight,
        int screenX,
        int screenY,
        int sortingOrder)
    {
        Kind = kind;
        ObjectIndex = objectIndex;
        ImageId = imageId;
        ResourcePath = resourcePath;
        RectangleX = rectangleX;
        RectangleWidth = rectangleWidth;
        RectangleHeight = rectangleHeight;
        ScreenX = screenX;
        ScreenY = screenY;
        SortingOrder = sortingOrder;
    }

    public ReferenceMapObjectKind Kind { get; }
    public int ObjectIndex { get; }
    public int ImageId { get; }
    public string ResourcePath { get; }
    public int RectangleX { get; }
    public int RectangleWidth { get; }
    public int RectangleHeight { get; }
    public int ScreenX { get; }
    public int ScreenY { get; }
    public int SortingOrder { get; }
}

/// <summary>
/// Unity lifetime adapter for the recovered ordinary map-object placement chain.
/// </summary>
/// <remarks>
/// Carries GameBootstrap.PlaceMapObjects (060003e6) and PlaceObj (060003cb)
/// through the live static composition, borrowing the original resource, sprite,
/// slice, animation and fade owners. The user-approved JZ footprint candidate is
/// retained separately. Standalone inspection uses the caller-supplied root;
/// production creates MapObjs at the recovered point after city-table setup.
/// </remarks>
public sealed partial class ReferenceLegacyMapObjectRenderer : IDisposable
{
    public const float DefaultPixelsPerUnit = 100f;
    public const int FirstTileSortingOrder = -9998;

    private readonly ReferenceGsmMap map;
    private readonly ResLoader resources;
    private readonly GameBootstrap objectOwner;
    private readonly Transform root;
    private readonly bool createMapObjectRoot;
    private GameObject ownedMapObjectRoot;
    private readonly Material material;
    private readonly float pixelsPerUnit;
    private readonly Dictionary<int, ResLoader.ObjSprite> imageCache;
    private readonly Dictionary<SpriteKey, Sprite> spriteCache;
    private readonly List<SpriteRenderer> renderers;
    private readonly List<FrameAnimator> animators;
    private readonly List<ReferenceLegacyMapObjectPlacement> placements;
    private bool built;
    private bool disposed;

    public ReferenceLegacyMapObjectRenderer(
        ReferenceGsmMap map,
        ResLoader resources,
        Transform root,
        Material material = null,
        float pixelsPerUnit = DefaultPixelsPerUnit,
        GameBootstrap objectOwner = null,
        bool createMapObjectRoot = false)
    {
        this.map = map ?? throw new ArgumentNullException(nameof(map));
        this.resources = resources ?? throw new ArgumentNullException(nameof(resources));
        this.objectOwner = objectOwner;
        this.createMapObjectRoot = createMapObjectRoot;
        this.root = root == null ? throw new ArgumentNullException(nameof(root)) : root;
        if (!map.Ok && !map.HasCover)
        {
            throw new ArgumentException(
                "The recovered object renderer requires a parsed terrain map or cover.",
                nameof(map));
        }

        if (!(pixelsPerUnit > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit));
        }

        this.material = material;
        this.pixelsPerUnit = pixelsPerUnit;
        imageCache = new Dictionary<int, ResLoader.ObjSprite>();
        spriteCache = new Dictionary<SpriteKey, Sprite>();
        renderers = new List<SpriteRenderer>();
        animators = new List<FrameAnimator>();
        placements = new List<ReferenceLegacyMapObjectPlacement>();
        Placements = placements.AsReadOnly();
    }

    // Production owns only disposal/observations here. Its original map, parent,
    // receiver and scalar inputs are supplied at the placement call below.
    internal ReferenceLegacyMapObjectRenderer(ResLoader resources,
        GameBootstrap objectOwner, bool createMapObjectRoot)
    {
        this.resources = resources ?? throw new ArgumentNullException(nameof(resources));
        this.objectOwner = objectOwner;
        this.createMapObjectRoot = createMapObjectRoot;
        imageCache = new Dictionary<int, ResLoader.ObjSprite>();
        spriteCache = new Dictionary<SpriteKey, Sprite>();
        renderers = new List<SpriteRenderer>();
        animators = new List<FrameAnimator>();
        placements = new List<ReferenceLegacyMapObjectPlacement>();
        Placements = placements.AsReadOnly();
    }

    public IReadOnlyList<ReferenceLegacyMapObjectPlacement> Placements { get; }
    public int RendererCount => renderers.Count;
    public int CachedImageCount => imageCache.Count;
    public int CachedTextureCount
    {
        get
        {
            var textures = new HashSet<Texture2D>();
            foreach (var image in imageCache.Values)
                if (image.tex != null) textures.Add(image.tex);
            return textures.Count;
        }
    }
    public int CachedSpriteCount => spriteCache.Count;
    public int AnimatedRendererCount => animators.Count;
    public bool Built => built;

    public bool Build()
    {
        ThrowIfDisposed();
        if (built)
        {
            return false;
        }
        PlaceMapObjectsForIndependentRuntime(objectOwner, map.Native, root,
            pixelsPerUnit, map.MapWidth, map.MapHeight);
        return true;
    }

    // The native void call receives its map and scalar arguments here. The public
    // Build latch above is retained only for historical standalone inspection.
    internal void PlaceMapObjectsForIndependentRuntime(GameBootstrap receiver,
        GsmMap sourceMap, Transform suppliedParent, float ppu, float worldWidth, float worldHeight)
    {
        if (sourceMap == null || !sourceMap.objsOk)
        {
            built = true;
            return;
        }

        GameBootstrap.ResetMapObjectPlacementMetricsForIndependentRuntime();
        if (ResLoader.I != null)
        {
            var imageIds = new List<int>();
            foreach (GsmMap.Obj item in sourceMap.tileObjs) imageIds.Add(item.id);
            foreach (GsmMap.Obj item in sourceMap.libObjs) imageIds.Add(item.id);
            ResLoader.I.BuildCityTable(imageIds);
        }

        Transform objectParent = suppliedParent;
        // Read this original receiver's worldRoot after city-table work, at the
        // recovered creation/parenting point, instead of rereading the singleton.
        if (createMapObjectRoot)
        {
            ownedMapObjectRoot = new GameObject("MapObjs");
            Transform parent = ReferenceEquals(receiver, null)
                ? suppliedParent : receiver.WorldRootForMapConstructionForIndependentRuntime;
            objectParent = ownedMapObjectRoot.transform;
            if (parent != null) objectParent.parent = parent;
        }

        PreparePortalAnchors(sourceMap);
        HashSet<int> blocked = receiver?.CreateObjectBlockListForIndependentRuntime();
        int tileOrder = FirstTileSortingOrder;
        int index = 0;
        foreach (GsmMap.Obj source in sourceMap.tileObjs)
        {
            int objectIndex = index++;
            if ((blocked != null && blocked.Contains(source.id)) || IsTrapMachinery(source.id)) continue;
            PlaceObject(receiver, source, objectParent, ppu, worldWidth, worldHeight,
                tileOrder, ReferenceMapObjectKind.Tile, objectIndex);
            tileOrder++;
        }

        index = 0;
        foreach (GsmMap.Obj source in sourceMap.libObjs)
        {
            int objectIndex = index++;
            if ((blocked != null && blocked.Contains(source.id)) || IsTrapMachinery(source.id)) continue;
            if (!ReferenceEquals(receiver, null) && receiver.SkipLibraryObjectForIndependentRuntime(source.id, source.sx, source.sy)) continue;
            PlaceObject(receiver, source, objectParent, ppu, worldWidth, worldHeight,
                !ReferenceEquals(receiver, null)
                    ? receiver.LibraryObjectOrderForIndependentRuntime(source.id, source.sy,
                        source.xl, source.yl, worldHeight)
                    : int.MinValue, ReferenceMapObjectKind.Library, objectIndex);
        }

        PlaceEmbeddedObjects(sourceMap, objectParent, ppu, worldWidth, worldHeight);
        receiver?.RegisterMapObjectOccludersForIndependentRuntime(objectParent);
        built = true;
    }

    private static ReferenceGsmObject ProjectObject(GsmMap.Obj value) =>
        new ReferenceGsmObject(value.id, value.sx, value.sy, value.xl, value.yl, value.py, value.pz);

#if UNITY_EDITOR
    // Historical raw-pixel inspection only; product rendering borrows source textures.
    public bool TryGetImage(int imageId, out ReferenceGsoImage image)
    {
        ThrowIfDisposed();
        image = null;
        if (!imageCache.TryGetValue(imageId, out ResLoader.ObjSprite source) || source.path == null) return false;
        return ReferenceGsoDecoder.TryDecode(resources.ReadRes(source.path), out image);
    }
#endif

    public bool TryGetTexture(int imageId, out Texture2D texture)
    {
        ThrowIfDisposed();
        bool found = imageCache.TryGetValue(imageId, out ResLoader.ObjSprite image);
        texture = image.tex;
        return found;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null)
            {
                DestroyOwned(renderer.gameObject);
            }
        }

        // Production sprites belong to the original GameBootstrap cache, not this scene.
        if (ReferenceEquals(objectOwner, null))
            foreach (Sprite sprite in spriteCache.Values) DestroyOwned(sprite);

        foreach (Sprite sprite in embeddedSprites) DestroyOwned(sprite);
        foreach (Texture2D texture in embeddedTextures) DestroyOwned(texture);
        DisposePortalApertures();
        DisposeFootprintStrips();
        embeddedSprites.Clear();
        embeddedTextures.Clear();
        renderers.Clear();
        animators.Clear();
        placements.Clear();
        spriteCache.Clear();
        imageCache.Clear();
        DestroyOwned(ownedMapObjectRoot);
        ownedMapObjectRoot = null;
    }

    private void PlaceObject(GameBootstrap receiver, GsmMap.Obj source, Transform parent,
        float pixelsPerUnit, float worldWidth, float worldHeight, int requestedSortingOrder,
        ReferenceMapObjectKind kind, int objectIndex)
    {
        // Obj is a value type: this seven-field diagnostic projection is exact.
        // Geometry and the lazy factory use the arguments captured by this call.
        ReferenceGsmObject value = ProjectObject(source);
        float framePixelsPerUnit = pixelsPerUnit;
        long loadStart = LoadTrace.Now;
        ResLoader.ObjSprite image = ResLoader.I != null
            ? ResLoader.I.LoadObj(value.Id, value.Width, value.Height) : default;
        if (LoadTrace.On)
        {
            GameBootstrap.ObjLoadMs += LoadTrace.Ms(loadStart);
            GameBootstrap.ObjN++;
        }
        imageCache[value.Id] = image;
        // Native PlaceObj skips a missing source texture without rejecting the whole map.
        if (image.tex == null) return;
        string path = image.path;
        receiver?.TouchMapObjectForIndependentRuntime(value.Id);
        int firstRenderer = renderers.Count;
        // PlaceObj checks the live loader again before selecting animation. If it
        // disappeared, even a multi-frame image falls through to slices/static art.
        bool animate = requestedSortingOrder == int.MinValue && image.frames > 1 &&
            ResLoader.I != null;

        if (requestedSortingOrder == int.MinValue && !animate &&
            image.clipArr != null && image.clipArr.Length > 1 && image.clipWidth > 0)
        {
            PlaceSlices(receiver, value, parent, pixelsPerUnit, worldWidth, worldHeight, kind, objectIndex, path, image);
            receiver?.NoteMapObjectForIndependentRuntime(value.Id);
            return;
        }

        if (requestedSortingOrder == int.MinValue &&
            TryPlacePortalAperture(receiver, value, parent, pixelsPerUnit, worldWidth,
                worldHeight, kind, objectIndex, image))
        {
            receiver?.NoteMapObjectForIndependentRuntime(value.Id);
            return;
        }

        if (requestedSortingOrder == int.MinValue && image.frames <= 1 &&
            kind == ReferenceMapObjectKind.Library && ((value.Id >> 16) & 0xff) == 45 &&
            (image.clipArr == null || image.clipArr.Length == 0) &&
            TryPlaceFootprintSlices(receiver, value, parent, pixelsPerUnit, worldWidth, worldHeight, kind, objectIndex, image))
        {
            receiver?.NoteMapObjectForIndependentRuntime(value.Id);
            return;
        }

        // Cached sprite lookup/creation is outside the native spawn interval.
        Sprite sprite = GetSprite(receiver, pixelsPerUnit, value.Id, image, 0, 0, image.tex.width);
        long spawnStart = LoadTrace.Now;
        int sortingOrder = animate ? value.SourceY + image.clipBy : requestedSortingOrder;

        Func<(Sprite[], Vector3[])> animationLoader = null;
        if (animate)
        {
            int imageId = value.Id;
            int sourceX = value.SourceX;
            int sourceY = value.SourceY;
            int pivotZ = value.PivotZ;
            animationLoader = () => CreateAnimationFrames(
                imageId, sourceX, sourceY, pivotZ, worldWidth, worldHeight, framePixelsPerUnit);
        }
        PlaceSpriteCore(receiver, value, parent, pixelsPerUnit, worldWidth, worldHeight, kind, objectIndex, path, image, 0, image.tex.width,
            sortingOrder, animate, sprite, animationLoader: animationLoader,
            nativePlacement: true);
        if (animate)
        {
            SpriteRenderer renderer = renderers[firstRenderer];
            RegisterObjectFade(receiver, value, pixelsPerUnit, worldWidth, worldHeight, image, new List<SpriteRenderer> { renderer }, renderer.sortingOrder);
        }
        if (LoadTrace.On) GameBootstrap.ObjSpawnMs += LoadTrace.Ms(spawnStart);
        receiver?.NoteMapObjectForIndependentRuntime(value.Id);
    }

    private static int StaticObjectOrder(ReferenceGsmObject value,
        ReferenceMapObjectKind kind, ResLoader.ObjSprite image, float mapHeight)
    {
        // OCCLUSION-JZ-001: user-directed candidate, not recovered official behavior.
        // JZ library buildings without clip strips still have an authored world-space
        // ground anchor (GsmMap.Obj.py). Keep their original image intact and interleave
        // it with actors there, instead of flattening it into the negative ground band.
        // Tile decorations, explicit QQ/XQQ ground bands, animated objects and authored
        // clip strips retain their original branches. Do not infer height from pixels.
        const int buildingObjectKind = 45; // ResLoader.MapObjNames: JZ
        if (image.frames <= 1 && kind == ReferenceMapObjectKind.Library &&
            ((value.Id >> 16) & 0xff) == buildingObjectKind)
            return value.PivotY;

        // Official 1.5.8 owner of the ground-band calculation is GameBootstrap.GroundBandOrder
        // (060003cc); the renderer only borrows it for the compatibility first-map placement.
        return GameBootstrap.GroundBandOrder(value.SourceY + image.clipBy, mapHeight);
    }

    private void RegisterObjectFade(GameBootstrap receiver, ReferenceGsmObject value,
        float pixelsPerUnit, float worldWidth, float worldHeight, ResLoader.ObjSprite image,
        List<SpriteRenderer> group, int order)
    {
        receiver?.RegisterMapObjectFadeForIndependentRuntime(group,
            value.SourceX + image.nLeft,
            (worldHeight * 0.5f - ((value.SourceY + image.nTop) - value.PivotZ)) / pixelsPerUnit,
            image, value.Id, worldWidth, pixelsPerUnit, order);
    }

    private bool PlaceSlices(
        GameBootstrap receiver, ReferenceGsmObject value, Transform parent,
        float pixelsPerUnit, float worldWidth, float worldHeight,
        ReferenceMapObjectKind kind,
        int objectIndex,
        string path,
        ResLoader.ObjSprite image)
    {
        if (!ReferenceEquals(receiver, null))
        {
            var group = new List<SpriteRenderer>();
            StringBuilder seam = GameBootstrap.BeginMapObjectSeamForIndependentRuntime(
                value.Id, value.SourceX, value.SourceY, value.SourceX + image.nLeft,
                worldWidth, pixelsPerUnit, image);
            long sliceStart = LoadTrace.Now;
            var slices = receiver.GetObjectSlicesForIndependentRuntime(value.Id, image, pixelsPerUnit);
            if (LoadTrace.On) GameBootstrap.ObjSliceMs += LoadTrace.Ms(sliceStart);
            long spawnStart = LoadTrace.Now;
            int maxOrder = int.MinValue;
            foreach (var slice in slices)
            {
                Sprite sprite = slice.Item1;
                // Native assigns even a null cached sprite. Diagnostic rectangle
                // observation must not invent a placement-admission predicate.
                int rectangleX = sprite != null ? (int)sprite.rect.x : 0;
                int rectangleWidth = sprite != null ? (int)sprite.rect.width : 0;
                PlaceSpriteCore(receiver, value, parent, pixelsPerUnit, worldWidth, worldHeight, kind, objectIndex, path, image, rectangleX, rectangleWidth,
                    value.SourceY + slice.Item3, false, sprite, slice.Item2,
                    nativePlacement: true);
                SpriteRenderer renderer = renderers[renderers.Count - 1];
                group.Add(renderer);
                if (renderer.sortingOrder > maxOrder) maxOrder = renderer.sortingOrder;
                spriteCache[new SpriteKey(value.Id, 0, rectangleX, rectangleWidth)] = sprite;
                if (seam != null)
                {
                    float wx = (value.SourceX + image.nLeft + slice.Item2 - worldWidth * 0.5f) / pixelsPerUnit;
                    GameBootstrap.AppendMapObjectSeamForIndependentRuntime(seam, sprite, renderer,
                        slice.Item2, slice.Item3, wx, pixelsPerUnit, image.hdScale);
                }
            }
            GameBootstrap.EndMapObjectSeamForIndependentRuntime(seam);
            if (LoadTrace.On) GameBootstrap.ObjSpawnMs += LoadTrace.Ms(spawnStart);
            long fadeStart = LoadTrace.Now;
            RegisterObjectFade(receiver, value, pixelsPerUnit, worldWidth, worldHeight, image, group, maxOrder);
            if (LoadTrace.On) GameBootstrap.ObjFadeMs += LoadTrace.Ms(fadeStart);
            return true;
        }

        int start = 0;
        bool placed = false;
        while (start < image.clipArr.Length)
        {
            int end = start + 1;
            int first = image.clipArr[start];
            while (end < image.clipArr.Length &&
                   Mathf.Abs(image.clipArr[end] - first) <= 3)
            {
                end++;
            }

            int rectangleX = start * image.clipWidth * image.hdScale;
            int rectangleEnd = Mathf.Min(
                image.tex.width,
                end * image.clipWidth * image.hdScale);
            if (rectangleEnd > rectangleX)
            {
                int middle = (start + end - 1) / 2;
                int clipY = image.clipArr[middle];
                if (clipY < 1)
                {
                    clipY = image.clipBy;
                }

                placed |= PlaceSprite(
                    receiver, value, parent, pixelsPerUnit, worldWidth, worldHeight,
                    kind,
                    objectIndex,
                    path,
                    image,
                    rectangleX,
                    rectangleEnd - rectangleX,
                    value.SourceY + clipY,
                    false);
            }

            start = end;
        }

        return placed;
    }

    private bool PlaceSprite(
        GameBootstrap receiver, ReferenceGsmObject value, Transform parent,
        float pixelsPerUnit, float worldWidth, float worldHeight,
        ReferenceMapObjectKind kind,
        int objectIndex,
        string path,
        ResLoader.ObjSprite image,
        int rectangleX,
        int rectangleWidth,
        int sortingOrder,
        bool animate,
        Func<(Sprite[], Vector3[])> animationLoader = null)
    {
        return PlaceSpriteCore(receiver, value, parent, pixelsPerUnit, worldWidth, worldHeight, kind, objectIndex, path, image, rectangleX, rectangleWidth,
            sortingOrder, animate, GetSprite(receiver, pixelsPerUnit, value.Id, image, 0, rectangleX, rectangleWidth),
            animationLoader: animationLoader);
    }

    private bool PlaceSpriteCore(GameBootstrap receiver, ReferenceGsmObject value, Transform parent,
        float pixelsPerUnit, float worldWidth, float worldHeight, ReferenceMapObjectKind kind, int objectIndex,
        string path, ResLoader.ObjSprite image, int rectangleX, int rectangleWidth, int sortingOrder,
        bool animate, Sprite sprite, int? logicalOffset = null,
        Func<(Sprite[], Vector3[])> animationLoader = null, bool nativePlacement = false)
    {
        if (!nativePlacement && sprite == null)
        {
            return false;
        }

        int screenX = value.SourceX + image.nLeft + (logicalOffset ?? rectangleX / Mathf.Max(image.hdScale, 1));
        int screenY = value.SourceY + image.nTop - value.PivotZ;
        if (nativePlacement)
        {
            // Shipping code evaluates this diagnostic name but uses the parameterless ctor.
            if (animate) _ = "obj" + value.Id.ToString() + "_anim";
            else if (logicalOffset.HasValue) _ = "obj" + value.Id.ToString() + "_c" + logicalOffset.Value.ToString();
            else _ = "obj" + value.Id.ToString();
        }
        GameObject gameObject = new GameObject();
        gameObject.transform.parent = parent;
        if (nativePlacement && LoadTrace.On)
        {
            if (animate) GameBootstrap.ObjAnimN++;
            else if (logicalOffset.HasValue) GameBootstrap.ObjSliceN++;
            else GameBootstrap.ObjPlainN++;
        }
        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        Material objectMaterial = !ReferenceEquals(receiver, null) ? GameBootstrap.IsoYObjMat() : material;
        if (objectMaterial != null)
        {
            renderer.sharedMaterial = objectMaterial;
        }
        // PlaceObj 060003cb: animated order precedes world position; sliced/static
        // position precedes order. Parenting preserves world transform; no scale store.
        if (animate) renderer.sortingOrder = sortingOrder;
        gameObject.transform.position = new Vector3(
            (screenX - worldWidth * 0.5f) / pixelsPerUnit,
            (worldHeight * 0.5f - screenY) / pixelsPerUnit,
            0f);
        if (!animate)
        {
            if (nativePlacement && sortingOrder == int.MinValue && !logicalOffset.HasValue)
                sortingOrder = StaticObjectOrder(value, kind, image, worldHeight);
            renderer.sortingOrder = sortingOrder;
        }

        if (animate)
        {
            FrameAnimator animator = gameObject.AddComponent<FrameAnimator>();
            animator.frameMs = 200f;
            animator.loop = true;
            animator.loader = animationLoader;
            animators.Add(animator);
        }

        renderers.Add(renderer);
        placements.Add(new ReferenceLegacyMapObjectPlacement(
            kind,
            objectIndex,
            value.Id,
            path,
            rectangleX,
            rectangleWidth,
            image.tex.height,
            screenX,
            screenY,
            sortingOrder));
        return true;
    }

    private Sprite GetSprite(
        GameBootstrap receiver, float pixelsPerUnit, int imageId,
        ResLoader.ObjSprite image,
        int frameIndex,
        int rectangleX,
        int rectangleWidth)
    {
        SpriteKey key = new SpriteKey(imageId, frameIndex, rectangleX, rectangleWidth);
        if (!ReferenceEquals(receiver, null))
        {
            Sprite shared = receiver.GetObjectSpriteForIndependentRuntime(imageId, image, pixelsPerUnit);
            spriteCache[key] = shared;
            return shared;
        }
        Sprite cached;
        if (spriteCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        Texture2D texture = image.tex;
        if (texture == null)
        {
            return null;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(rectangleX, 0f, rectangleWidth, image.tex.height),
            new Vector2(0f, 1f),
            pixelsPerUnit * Mathf.Max(1, image.hdScale));
        spriteCache[key] = sprite;
        return sprite;
    }

    private static (Sprite[], Vector3[]) CreateAnimationFrames(
        int imageId, int sourceX, int sourceY, int pivotZ,
        float worldWidth, float worldHeight, float pixelsPerUnit)
    {
        // Original PlaceObj lazy callback borrows the source owner's animation arrays.
        ResLoader.ObjAnim animation = ResLoader.I.LoadObjAnim(imageId, pixelsPerUnit);
        if (animation.frames == null) return (null, null);
        Vector3[] positions = new Vector3[animation.frames.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] = new Vector3(
                (animation.nLeft[i] + sourceX - worldWidth * 0.5f) / pixelsPerUnit,
                (worldHeight * 0.5f - ((animation.nTop[i] + sourceY) - pivotZ)) / pixelsPerUnit,
                0f);
        }
        return (animation.frames, positions);
    }

    private static bool IsTrapMachinery(int imageId)
    {
        return ((uint)imageId >> 16) == 6426;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceLegacyMapObjectRenderer));
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

    private struct SpriteKey : IEquatable<SpriteKey>
    {
        public SpriteKey(int imageId, int frameIndex, int rectangleX, int rectangleWidth)
        {
            ImageId = imageId;
            FrameIndex = frameIndex;
            RectangleX = rectangleX;
            RectangleWidth = rectangleWidth;
        }

        public int ImageId { get; }
        public int FrameIndex { get; }
        public int RectangleX { get; }
        public int RectangleWidth { get; }

        public bool Equals(SpriteKey other)
        {
            return ImageId == other.ImageId && FrameIndex == other.FrameIndex &&
                   RectangleX == other.RectangleX && RectangleWidth == other.RectangleWidth;
        }

        public override bool Equals(object value)
        {
            return value is SpriteKey && Equals((SpriteKey)value);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ImageId;
                hash = (hash * 397) ^ FrameIndex;
                hash = (hash * 397) ^ RectangleX;
                return (hash * 397) ^ RectangleWidth;
            }
        }
    }

}

}
