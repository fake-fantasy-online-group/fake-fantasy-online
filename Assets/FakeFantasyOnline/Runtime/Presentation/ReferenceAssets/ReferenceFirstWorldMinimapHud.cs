using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public enum ReferenceSketchMapParseFailure
{
    None = 0,
    MissingBytes = 1,
    InvalidSignature = 2,
    InvalidDimensions = 4,
    InvalidFormat = 5,
    TruncatedPixels = 6
}

/// <summary>
/// Source translation of the .smp reader that 06000425 (GameHud.LoadSketch) runs inline after
/// ResLoader.ReadMap (06000c04). Every offset, guard and loop below is the frozen
/// arm64/x86_64 body, not a rediscovered file format: the official entry gate is only
/// "length &gt; 0x78 and byte 0 == 0x54", the header version at 0x34 is never read, and the
/// payload is a chain of tile headers starting at 0x60, one per planes * levels entry.
/// </summary>
public sealed class ReferenceSketchMapSample
{
    // Official entry gate, identical in both frozen exports: 0x78 < bytes.Length. 0x60 is the
    // first tile header, 0x10 its size, so the first payload byte of tile 0 sits at 0x70.
    private const int MinimumLength = 0x78;
    private const byte SignatureByte = 0x54;
    private const int FirstTileHeaderOffset = 0x60;
    private const int TileHeaderSize = 0x10;

    private readonly Color32[] pixels;

    private ReferenceSketchMapSample(
        float cellWidth,
        float cellHeight,
        int width,
        int height,
        int tileWidth,
        int tileHeight,
        int firstTilePitch,
        float canvasWidth,
        float canvasHeight,
        Color32[] pixels)
    {
        CellWidth = cellWidth;
        CellHeight = cellHeight;
        Width = width;
        Height = height;
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        FirstTilePitch = firstTilePitch;
        CanvasWidth = canvasWidth;
        CanvasHeight = canvasHeight;
        this.pixels = pixels;
    }

    public float CellWidth { get; }
    public float CellHeight { get; }
    public int Width { get; }
    public int Height { get; }
    /// <summary>Official header 0x50: the width of one tile of the planes x levels grid.</summary>
    public int TileWidth { get; }
    /// <summary>Official header 0x54: the height of one tile of the planes x levels grid.</summary>
    public int TileHeight { get; }
    /// <summary>Row pitch (0x68) of the first tile header; later tiles carry their own.</summary>
    public int FirstTilePitch { get; }
    /// <summary>
    /// Official +0x1bc: the canvas half 06000425 writes only while the header dimensions and
    /// both cell halves are positive, so it stays at zero whenever that guard fails even though
    /// the cell halves themselves were already published.
    /// </summary>
    public float CanvasWidth { get; }
    /// <summary>Official +0x1c0, written under the same guard as <see cref="CanvasWidth"/>.</summary>
    public float CanvasHeight { get; }
    /// <summary>
    /// Whether the tile chain published +0x188 / +0x190 / +0x194. The entry gate publishes the
    /// header geometry first and the tile walk runs after it, so the geometry above is official
    /// even when this is false.
    /// </summary>
    public bool RasterPublished => pixels != null;
    public IReadOnlyList<Color32> Pixels => Array.AsReadOnly(pixels ?? Array.Empty<Color32>());
    internal Color32[] PixelsForIndependentRuntime => pixels;

    public static bool TryParse(
        byte[] bytes,
        out ReferenceSketchMapSample sample,
        out ReferenceSketchMapParseFailure failure,
        GameHud publicationOwner = null)
    {
        sample = null;
        failure = ReferenceSketchMapParseFailure.None;
        // Official entry gate, both frozen exports: only the payload length and byte 0 are
        // tested. The header version at 0x34 is never read by 06000425, so no version
        // condition exists here.
        if (bytes == null || bytes.Length <= MinimumLength)
        {
            failure = ReferenceSketchMapParseFailure.MissingBytes;
            return false;
        }

        if (bytes[0] != SignatureByte)
        {
            failure = ReferenceSketchMapParseFailure.InvalidSignature;
            return false;
        }

        // Official arm64 06000425.c:193-196 / x86_64 06000425.c:179-183: both cell halves are
        // published straight after the entry gate and before any later check runs, so a payload
        // the tile chain rejects still leaves +0x1ac / +0x1b0 at their official header values.
        float cellWidth = ReadSingle(bytes, 0x38);
        if (!ReferenceEquals(publicationOwner, null)) publicationOwner.cellW = cellWidth;
        float cellHeight = ReadSingle(bytes, 0x3c);
        if (!ReferenceEquals(publicationOwner, null)) publicationOwner.cellH = cellHeight;
        int width = ReadInt32(bytes, 0x40);
        int height = ReadInt32(bytes, 0x44);
        int tileWidth = ReadInt32(bytes, 0x50);
        int tileHeight = ReadInt32(bytes, 0x54);
        int planes = ReadInt32(bytes, 0x58);
        int levels = ReadInt32(bytes, 0x5c);

        // 0xf < (uint)(planes - 1) || 0xf < (uint)(levels - 1) collapses both counts to 1.
        // Everything in [1,16] is kept as-is and drives the tile grid below.
        if (unchecked((uint)(planes - 1)) > 0xf || unchecked((uint)(levels - 1)) > 0xf)
        {
            planes = 1;
            levels = 1;
        }

        // Official arm64 06000425.c:207-212 / x86_64 06000425.c:203-215: the canvas halves are
        // written only while both dimensions and both cell halves are positive, and the two
        // origin halves follow from the same guard. The guard is narrower than the one that arms
        // the tile walk, so a header whose cell size is not positive still gets its raster.
        bool dimensioned = width > 0 && height > 0;
        bool sized = dimensioned && cellWidth > 0f && cellHeight > 0f;
        float canvasWidth = sized ? cellWidth * width : 0f;
        float canvasHeight = sized ? cellHeight * height : 0f;
        if (sized && !ReferenceEquals(publicationOwner, null))
            publicationOwner.PublishSketchCanvasForIndependentRuntime(canvasWidth, canvasHeight);

        // Official arm64 06000425.c:223-225 / x86_64 06000425.c:225-227: the raster is allocated
        // only once the tile grid fits as well, and every later tile header, format and length
        // rejection jumps to the common exit without publishing it. The two results are kept
        // apart on purpose: the geometry above is already official at that point, +0x188 /
        // +0x190 / +0x194 are not.
        Color32[] pixels = null;
        int firstTilePitch = 0;
        if (!dimensioned || tileWidth <= 0 || tileHeight <= 0 ||
            width > planes * tileWidth || height > levels * tileHeight)
        {
            failure = ReferenceSketchMapParseFailure.InvalidDimensions;
        }
        else
        {
            // The sample raster is allocated before the tile walk and only the cells a tile
            // covers are written, exactly like the official body: every uncovered cell keeps the
            // default Color32 (0,0,0,0) and stays transparent.
            Color32[] raster = new Color32[height * width];
            int tileCount = planes * levels;
            int tileOffset = FirstTileHeaderOffset;
            int pitchOfFirstTile = 0;
            bool rasterRejected = false;
            for (int tileIndex = 0; tileIndex != tileCount; tileIndex++)
            {
                int dataOffset = tileOffset + TileHeaderSize;
                if (bytes.Length < dataOffset)
                {
                    failure = ReferenceSketchMapParseFailure.TruncatedPixels;
                    rasterRejected = true;
                    break;
                }

                int tilePixelWidth = ReadInt32(bytes, tileOffset);
                int tilePixelHeight = ReadInt32(bytes, tileOffset + 4);
                int pitch = ReadInt32(bytes, tileOffset + 8);
                int tileFormat = ReadInt32(bytes, tileOffset + 0xc);
                if (tileFormat != 1 || tilePixelWidth < 1 || tilePixelHeight < 1 ||
                    pitch < tilePixelWidth * 2)
                {
                    failure = ReferenceSketchMapParseFailure.InvalidFormat;
                    rasterRejected = true;
                    break;
                }

                if (bytes.Length < (long)pitch * tilePixelHeight + dataOffset)
                {
                    failure = ReferenceSketchMapParseFailure.TruncatedPixels;
                    rasterRejected = true;
                    break;
                }

                if (tileIndex == 0)
                {
                    pitchOfFirstTile = pitch;
                }

                // Tile order: index / planes is the tile row and index % planes the tile column.
                int destinationX = (tileIndex % planes) * tileWidth;
                int destinationY = (tileIndex / planes) * tileHeight;
                for (int y = 0; y < tilePixelHeight; y++)
                {
                    if (destinationY + y >= height)
                    {
                        break;
                    }

                    for (int x = 0; x < tilePixelWidth; x++)
                    {
                        if (destinationX + x >= width)
                        {
                            break;
                        }

                        int source = dataOffset + y * pitch + x * 2;
                        byte low = bytes[source];
                        byte high = bytes[source + 1];
                        raster[(destinationY + y) * width + destinationX + x] = new Color32(
                            ExpandNibble((byte)(high & 0x0f)),
                            ExpandNibble((byte)(low >> 4)),
                            ExpandNibble((byte)(low & 0x0f)),
                            ExpandNibble((byte)(high >> 4)));
                    }
                }

                tileOffset = dataOffset + pitch * tilePixelHeight;
            }

            if (!rasterRejected)
            {
                pixels = raster;
                firstTilePitch = pitchOfFirstTile;
                if (!ReferenceEquals(publicationOwner, null))
                    publicationOwner.PublishSketchRasterForIndependentRuntime(pixels, width, height);
            }
        }

        // The native caller already owns these slots. A sample wrapper is only needed by
        // the existing detached parser API, never an extra allocation between native stages.
        if (ReferenceEquals(publicationOwner, null))
        {
            sample = new ReferenceSketchMapSample(
                cellWidth,
                cellHeight,
                width,
                height,
                tileWidth,
                tileHeight,
                firstTilePitch,
                canvasWidth,
                canvasHeight,
                pixels);
        }
        return pixels != null;
    }

    /// <summary>
    /// Bilinear read over any published +0x188 raster: the official sample is the .smp sample,
    /// but 06000425 also publishes the row-reversed sketch gsa pixels there, so the sampler
    /// takes the raster instead of assuming the .smp parsed.
    /// </summary>
    internal static Color32 SampleBilinear(
        Color32[] pixels,
        int width,
        int height,
        float x,
        float y,
        Color32 fallback)
    {
        int left = Mathf.FloorToInt(x);
        int top = Mathf.FloorToInt(y);
        float fractionX = x - left;
        float fractionY = y - top;
        Color32 topLeft = PixelOr(pixels, width, height, left, top, fallback);
        Color32 topRight = PixelOr(pixels, width, height, left + 1, top, fallback);
        Color32 bottomLeft = PixelOr(pixels, width, height, left, top + 1, fallback);
        Color32 bottomRight = PixelOr(pixels, width, height, left + 1, top + 1, fallback);
        return new Color32(
            InterpolateChannel(
                topLeft.r, topRight.r, bottomLeft.r, bottomRight.r, fractionX, fractionY),
            InterpolateChannel(
                topLeft.g, topRight.g, bottomLeft.g, bottomRight.g, fractionX, fractionY),
            InterpolateChannel(
                topLeft.b, topRight.b, bottomLeft.b, bottomRight.b, fractionX, fractionY),
            InterpolateChannel(
                topLeft.a, topRight.a, bottomLeft.a, bottomRight.a, fractionX, fractionY));
    }

    private static Color32 PixelOr(
        Color32[] pixels,
        int width,
        int height,
        int x,
        int y,
        Color32 fallback)
    {
        return x >= 0 && y >= 0 && x < width && y < height
            ? pixels[y * width + x]
            : fallback;
    }

    // GameHud.SampleBilin (0600042a) inlines 06000466: both horizontal blends remain
    // floats, then the vertical blend is truncated to a byte once, without a clamp.
    // 1.5.8 arm64 0x43ed50..0x43ed94 / x86_64 0x456ad0..0x456b2d.
    private static byte InterpolateChannel(
        byte topLeft,
        byte topRight,
        byte bottomLeft,
        byte bottomRight,
        float fractionX,
        float fractionY)
    {
        float top = topLeft + (topRight - topLeft) * fractionX;
        float bottom = bottomLeft + (bottomRight - bottomLeft) * fractionX;
        return unchecked((byte)(top + (bottom - top) * fractionY));
    }

    private static byte ExpandNibble(byte value)
    {
        return (byte)(value << 4 | value);
    }

    private static int ReadInt32(byte[] bytes, int offset)
    {
        return bytes[offset] |
            bytes[offset + 1] << 8 |
            bytes[offset + 2] << 16 |
            bytes[offset + 3] << 24;
    }

    private static float ReadSingle(byte[] bytes, int offset)
    {
        // Original 06000425 calls BitConverter.ToSingle(bytes, offset) directly.
        // An intermediate four-byte allocation would add an absent failure boundary.
        return BitConverter.ToSingle(bytes, offset);
    }

    private static bool IsPositiveFinite(float value)
    {
        return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

/// <summary>
/// Source translation of the default-visible compact GameHud sketch-map branch.
/// </summary>
public sealed class ReferenceFirstWorldMinimapHud : IDisposable
{
    public const float DefaultZoom = 2f;
    public const float CoordinateWorldScale = 0.03125f;
    public const int LogicalMapPixels = 138;
    public const float DiagramImageAlpha = 0.55f;

    // Shared with the official GameHud owner rather than copied: UpdateMiniView (0x06000427)
    // composites the same map fall-back in integer lanes (arm64 06000427.c:384-401) and the
    // official body is the one place that decision lives.
    internal static readonly Color32 FallbackColor = new Color32(96, 99, 47, 255);
    private static readonly GUIStyle TransparentButton = GUIStyle.none;
    // 06000414 arm64 :2770-:2777 and :3748-:3756 (x86_64 same two pairs): the halo and the core
    // tint of one air-drop chest point, both drawn from ZoneOverlay.Dot (0x060010a6).
    private static readonly Color BoxPointHalo = new Color(0.85f, 0.85f, 0.88f, 0.9f);
    private static readonly Color BoxPointCore = new Color(0.04f, 0.04f, 0.05f, 1f);

    private readonly ReferenceFirstWorldHudResources resources;
    private readonly ReferenceWorldBitmapFont font;
    private ReferenceSketchMapSample sample;
    private Texture2D detachedSketchTexture;
    private Texture2D detachedDiagramTexture;
    private Texture2D sketchTexture
    {
        get => hudOwner != null ? hudOwner.SketchTextureForIndependentRuntime : detachedSketchTexture;
        set => detachedSketchTexture = value;
    }
    private Texture2D diagramTexture
    {
        get => hudOwner != null ? hudOwner.DiagramTextureForIndependentRuntime : detachedDiagramTexture;
        set => detachedDiagramTexture = value;
    }
    private string mapName;
    // Official GameHud +0x188 / +0x190 / +0x194. They are the .smp sample when the tile chain
    // published one and the row-reversed sketch gsa pixels otherwise, so they are kept apart
    // from the .smp-only geometric sample above.
    private Color32[] detachedSketchPixels;
    private int detachedSketchPixelWidth;
    private int detachedSketchPixelHeight;
    private Color32[] sketchPixels
    {
        get => hudOwner != null ? hudOwner.SketchPixelsForIndependentRuntime : detachedSketchPixels;
        set => detachedSketchPixels = value;
    }
    private int sketchPixelWidth
    {
        get => hudOwner != null ? hudOwner.SketchWidthForIndependentRuntime : detachedSketchPixelWidth;
        set => detachedSketchPixelWidth = value;
    }
    private int sketchPixelHeight
    {
        get => hudOwner != null ? hudOwner.SketchHeightForIndependentRuntime : detachedSketchPixelHeight;
        set => detachedSketchPixelHeight = value;
    }
    // Official GameHud +0x1bc / +0x1c0 before the trailing fall-back: zero unless the .smp cell
    // size produced a canvas, in which case +0x1b4 / +0x1b8 were written with it.
    private float mapCanvasWidth;
    private float mapCanvasHeight;
    // Official GameHud +0x1e8: the List<Vector2> GameBootstrap.ArenaBoxPoints (0x060003d6) clears
    // and refills, so both chest branches read the current rows and never an older frame's set.
    private readonly List<Vector2> boxPts = new List<Vector2>();
    private bool disposed;

    private ReferenceFirstWorldMinimapHud(
        ReferenceFirstWorldHudResources resources,
        ReferenceWorldBitmapFont font,
        ReferenceSketchMapSample sample,
        Texture2D sketchTexture,
        Texture2D diagramTexture,
        string mapName,
        Color32[] sketchPixels,
        int sketchPixelWidth,
        int sketchPixelHeight,
        float mapCanvasWidth,
        float mapCanvasHeight)
    {
        this.resources = resources;
        this.font = font;
        this.sample = sample;
        this.sketchTexture = sketchTexture;
        this.diagramTexture = diagramTexture;
        this.mapName = mapName;
        this.sketchPixels = sketchPixels;
        this.sketchPixelWidth = sketchPixelWidth;
        this.sketchPixelHeight = sketchPixelHeight;
        this.mapCanvasWidth = mapCanvasWidth;
        this.mapCanvasHeight = mapCanvasHeight;
    }

    // The official 06000425 leaves every sketch slot at zero when neither source exists and the
    // world window still runs, so the HUD shell is ready without a raster; only the raster and
    // the markers it places are gated on the published pixels and canvas. Bitmap availability
    // likewise belongs to the two native text branches, not the persistent owner: losing the
    // font must not make Draw fail and tear down an already admitted world.
    public bool Ready => !disposed && resources != null && resources.Ready &&
        font != null;

    // Official +0x1bc / +0x1c0 after the trailing fall-back (arm64 06000425.c:405-411 /
    // x86_64 06000425.c:411-415): the whole-world fall-back is latched once inside LoadSketch, so
    // the halves a draw consumes are the latched owner values and CurWorldW/H is read at that
    // single time point, not again on every draw. The same cache guard that keys on CurMapName
    // keeps the latch when the name repeats, so a later world-extent change does not retime it.
    // WB-151: the canvas and the two origin halves now have one reader. Every consumer that
    // reaches them runs behind a bound GameHud - the world window binds one at build time and the
    // diagram audit mounts one - so an unbound arm could only add a canvas and an origin the
    // official bodies do not have; the latched owner values are the only ones a draw consumes.
    private float EffectiveCanvasWidth => hudOwner.SketchCanvasWidthForIndependentRuntime;
    private float EffectiveCanvasHeight => hudOwner.SketchCanvasHeightForIndependentRuntime;
    // Official +0x1b4 / +0x1b8: written only together with the canvas, and latched with it, so
    // they stay at zero whenever the .smp cell size produced none.
    private float SampleOriginX => hudOwner.SketchOriginXForIndependentRuntime;
    private float SampleOriginY => hudOwner.SketchOriginYForIndependentRuntime;
    private GameHud hudOwner;
    private bool detachedMiniOpen = true;
    public bool MiniOpen
    {
        get => hudOwner != null ? hudOwner.MiniOpenForIndependentRuntime : detachedMiniOpen;
        private set
        {
            if (hudOwner != null) hudOwner.MiniOpenForIndependentRuntime = value;
            else detachedMiniOpen = value;
        }
    }
    internal void BindNativeOwnerForIndependentRuntime(GameHud owner) => hudOwner = owner;

    internal static bool TryCreateNativeView(ReferenceResourceLoader loader,
        ReferenceFirstWorldHudResources resources, out ReferenceFirstWorldMinimapHud owner)
    {
        owner = null;
        if (loader == null || resources == null || !resources.Ready) return false;
        if (!ReferenceWorldBitmapFont.TryCreate(loader, out var font) || font == null) return false;
        owner = new ReferenceFirstWorldMinimapHud(resources, font, null, null, null,
            string.Empty, null, 0, 0, 0f, 0f);
        return true;
    }

    internal void BindHudOwnerForIndependentRuntime(GameHud owner)
    {
        hudOwner = owner;
        // Official GameHud instance fields smpOrgX (0x06000426 reads +0x1b4), smpOrgY (+0x1b8),
        // canvasWX (+0x1bc) and canvasWY (+0x1c0) are written by LoadSketch (0x06000425) and read
        // by DiagramFrac (0x06000426). The canvas halves this carrier computed are published into
        // their official owner; the origin halves are not, because the official body writes them
        // from GameBootstrap.CurWorldW/H inside LoadSketch, at a point where the current world
        // extent is already published, and never from the extent the window was built with.
        // The same carrier also owns +0x170, +0x180, +0x188, +0x190/0x194 and the +0x1ac/0x1b0
        // cell size, all of which are published with them.
        owner.BindSketchForIndependentRuntime(
            detachedSketchPixels,
            detachedSketchPixelWidth,
            detachedSketchPixelHeight,
            detachedSketchTexture,
            detachedSketchTexture == null ? detachedDiagramTexture : null,
            mapCanvasWidth,
            mapCanvasHeight,
            sample == null ? 0f : sample.CellWidth,
            sample == null ? 0f : sample.CellHeight);
    }
    // Official GameHud +0x1c5 (0x06000451 ToggleMapDiagram flips it) and +0x1c8
    // (0x06000450 MinimapZoom steps it). Both live on the official owner when one is bound, so a
    // bound HUD has a single diagram flag and a single zoom value. The two static accessors of
    // that same byte - get_DiagramOpen (0x06000452) and CloseDiagram (0x06000453) - reach it
    // through the GameHud statics slot at +0x18 (GameHud.I) behind one Object.op_Inequality gate
    // and return false / write nothing when that slot is empty, so the flag has one storage
    // location and no unbound copy of its own.
    public bool DiagramOpen
    {
        get => hudOwner != null && hudOwner.diagramOpen;
        private set
        {
            if (hudOwner != null) hudOwner.diagramOpen = value;
        }
    }
    public bool DiagramAvailable => Ready && diagramTexture;
    public bool WorldMapRequested { get; private set; }
    // Official GameHud +0x1c8 is the only zoom slot; seeing it through a detached default keeps a
    // second clamping decision out of this class, because nothing here steps it any more.
    public float Zoom => hudOwner != null ? hudOwner.miniZoom : DefaultZoom;
    public string MapName => hudOwner != null ? hudOwner.mapName : mapName;
    public ReferenceSketchMapSample Sample => sample;
    // The single published compact raster: official GameHud +0x178, allocated, replaced and
    // published inside UpdateMiniView (06000427) and read back here. There is no second carrier
    // texture and therefore nothing to reconcile.
    private Texture2D MiniViewTexture =>
        hudOwner == null ? null : hudOwner.MiniViewForIndependentRuntime;
    public Texture2D MiniView => MiniViewTexture;
    public Texture2D DiagramTexture => diagramTexture;
    // Official GameHud +0x170 is the sketch gsa and +0x180 the diagram texture built from the
    // .smp sample; they are only the same object when the gsa exists, in which case +0x180 stays
    // null exactly as the official body leaves it.
    internal Texture2D OfficialDiagramTextureForIndependentRuntime =>
        sketchTexture == null ? diagramTexture : null;
    internal Texture2D SelfMarkerTexture => resources.SketchMarkSelf;
    public Rect LastBarRect { get; private set; }
    public Rect LastToggleRect { get; private set; }
    public Rect LastFrameRect { get; private set; }
    public Rect LastMapRect { get; private set; }
    public Rect LastWorldButtonRect { get; private set; }
    public Rect LastPlusButtonRect { get; private set; }
    public Rect LastMinusButtonRect { get; private set; }
    public Rect LastSelfMarkerRect { get; private set; }
    public int LastPartyMarkerCount { get; private set; }
    public Rect LastDiagramRect { get; private set; }
    public Rect LastDiagramSelfMarkerRect { get; private set; }
    public int LastDiagramPartyMarkerCount { get; private set; }

    public static Rect BarRect(int screenWidth, float scale)
    {
        return new Rect(
            screenWidth - 151f * scale,
            0f,
            151f * scale,
            20f * scale);
    }

    public static Rect ToggleRect(int screenWidth, float scale)
    {
        return new Rect(
            screenWidth - 20f * scale,
            0f,
            20f * scale,
            19f * scale);
    }

    public static Rect FrameRect(int screenWidth, float scale)
    {
        return RoundedRect(
            screenWidth - 179f * scale,
            20f * scale,
            176f * scale,
            153f * scale);
    }

    public static Rect MapRect(int screenWidth, float scale)
    {
        return RoundedRect(
            screenWidth - 160f * scale,
            32f * scale,
            LogicalMapPixels * scale,
            LogicalMapPixels * scale);
    }

    public static Rect WorldButtonRect(int screenWidth, float scale)
    {
        return RoundedRect(
            screenWidth - 39f * scale,
            30f * scale,
            26f * scale,
            26f * scale);
    }

    public static Rect PlusButtonRect(int screenWidth, float scale)
    {
        return RoundedRect(
            screenWidth - 29f * scale,
            57f * scale,
            26f * scale,
            26f * scale);
    }

    public static Rect MinusButtonRect(int screenWidth, float scale)
    {
        return RoundedRect(
            screenWidth - 24f * scale,
            83f * scale,
            26f * scale,
            26f * scale);
    }

    public static Rect DiagramRect(
        int screenWidth,
        int screenHeight,
        Texture2D texture,
        float scale)
    {
        if (screenWidth < 1 || screenHeight < 1 || !texture ||
            !IsPositiveFinite(scale))
        {
            return default(Rect);
        }

        float diagramScale = Mathf.Max(1f, Mathf.Round(scale));
        float width = GsaTextureScale.Width(texture) * diagramScale;
        float height = GsaTextureScale.Height(texture) * diagramScale;
        return new Rect(
            Mathf.Round((screenWidth - width) * 0.5f),
            Mathf.Round((screenHeight - height) * 0.5f),
            Mathf.Round(width),
            Mathf.Round(height));
    }

    public static bool TryCreate(
        ReferenceResourceLoader loader,
        ReferenceFirstWorldHudResources resources,
        MapDef.Entry descriptor,
        out ReferenceFirstWorldMinimapHud owner)
    {
        owner = null;
        if (loader == null || resources == null || !resources.Ready ||
            string.IsNullOrEmpty(descriptor.file))
        {
            return false;
        }

        ReferenceWorldBitmapFont font;
        if (!ReferenceWorldBitmapFont.TryCreate(loader, out font) || font == null)
        {
            return false;
        }

        // Official 06000425 loads "res/sketchmap/" + CurMapName + ".gsa" into GameHud +0x170 and
        // then folds every Color32 whose alpha is neither 0 nor 0xff: below 0x60 the whole pixel
        // is zeroed, from 0x60 up only the alpha is forced to 0xff, and the batch is pushed back
        // with SetPixels32/Apply. The pass is idempotent and belongs at the load, not at draw.
        // The gsa is loaded before the .smp is read, so a rejected .smp never skips it.
        Texture2D sketchTexture = loader.LoadGsa(
            "res/sketchmap/" + descriptor.file + ".gsa");
        NormalizeSketchAlpha(sketchTexture);

        // Official 06000425 reads the .smp through ResLoader.ReadMap (06000c04), which only
        // consults the Pkg archive and then the Pkg24 archive (arm64 0x300c68 / x86_64
        // 0x312f00, confirmed by get_Pkg 06000bf8 = +0x20 and get_Pkg24 06000bfa = +0x28).
        // ReadRes is a different channel: it also walks the loose 24-bit directories. A missing
        // or rejected .smp is not a failure: the official body keeps going with every sketch
        // slot still zero and only the world window's minimap raster ends up empty.
        byte[] bytes = loader.ReadMap("map/" + descriptor.file + ".smp");
        ReferenceSketchMapSample sample;
        ReferenceSketchMapParseFailure failure;
        // Two independent official results: the header geometry the entry gate publishes
        // (+0x1ac / +0x1b0 and, under its own guard, +0x1bc / +0x1c0 with the two origin halves)
        // and the raster the tile chain publishes. A tile rejection leaves the first official and
        // the second unpublished, so the sample is kept instead of being cleared.
        bool rasterPublished = ReferenceSketchMapSample.TryParse(bytes, out sample, out failure);

        // Official +0x188 / +0x190 / +0x194: the .smp sample the tile chain published, and when
        // there is none the sketch gsa pixels copied row by row in reverse order
        // (arm64 LAB_00436f44 / x86_64 LAB_0044db19) with the gsa texture's own width/height.
        Color32[] pixels = null;
        int pixelWidth = 0;
        int pixelHeight = 0;
        if (rasterPublished)
        {
            pixels = sample.PixelsForIndependentRuntime;
            pixelWidth = sample.Width;
            pixelHeight = sample.Height;
        }
        else if (sketchTexture != null)
        {
            pixels = CopyRowsReversed(sketchTexture, out pixelWidth, out pixelHeight);
        }

        // The official +0x180 diagram texture only exists when the sketch gsa is missing: a new
        // RGBA32 texture sized from the .smp sample, Point filtered, clamped, filled with the
        // sample rows in reverse order. When the gsa is present the official body destroys any
        // previous +0x180 and leaves it null, and this carrier draws +0x170 itself.
        Texture2D diagramTexture = sketchTexture != null
            ? sketchTexture
            : BuildDiagramTexture(sample);

        // Official +0x1ac/0x1b0 -> +0x1bc/0x1c0: both cell halves are published as soon as the
        // entry gate passes, the canvas pair only while the dimensions and both cell halves are
        // positive, and zero otherwise, which is what arms the whole-world fall-back LoadSketch
        // owns. A payload the tile chain rejected still carries its cell halves and, when the
        // header guard passed, the canvas the native body had already written.
        float canvasWidth = 0f;
        float canvasHeight = 0f;
        if (sample != null)
        {
            canvasWidth = sample.CanvasWidth;
            canvasHeight = sample.CanvasHeight;
        }

        ReferenceFirstWorldMinimapHud candidate = new ReferenceFirstWorldMinimapHud(
            resources,
            font,
            sample,
            sketchTexture,
            diagramTexture,
            descriptor.name,
            pixels,
            pixelWidth,
            pixelHeight,
            canvasWidth,
            canvasHeight);
        if (!candidate.Ready)
        {
            candidate.Dispose();
            return false;
        }

        owner = candidate;
        return true;
    }

    /// <summary>
    /// The official 06000425 alpha pass that follows the sketch gsa load.
    /// </summary>
    internal static void NormalizeSketchAlpha(Texture2D texture)
    {
        if (texture == null)
        {
            return;
        }

        Color32[] pixels = texture.GetPixels32();
        bool changed = false;
        for (int index = 0; index < pixels.Length; index++)
        {
            byte alpha = pixels[index].a;
            if (alpha == 0 || alpha == 0xff)
            {
                continue;
            }

            pixels[index] = alpha < 0x60
                ? new Color32(0, 0, 0, 0)
                : new Color32(pixels[index].r, pixels[index].g, pixels[index].b, 0xff);
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
    }

    /// <summary>
    /// The official 06000425 gsa fall-back for +0x188 (arm64 LAB_00436f44 / x86_64
    /// LAB_0044db19): width and height come from the gsa texture itself and every source row is
    /// copied into the mirror row of a fresh Color32[width * height]. The copy is a product
    /// algorithm over the already decoded gsa pixels, not a re-encode of any asset.
    /// </summary>
    private static Color32[] CopyRowsReversed(
        Texture2D texture,
        out int width,
        out int height)
    {
        width = texture.width;
        height = texture.height;
        if (width < 1 || height < 1)
        {
            return null;
        }

        Color32[] source = texture.GetPixels32();
        Color32[] flipped = new Color32[height * width];
        CopyRowsReversedInto(source, flipped, width, height);
        return flipped;
    }

    internal static void CopyRowsReversedInto(Color32[] source, Color32[] flipped, int width, int height)
    {
        for (int row = 0; row < height; row++)
        {
            Array.Copy(source, width * (height - 1 - row), flipped, width * row, width);
        }
    }

    /// <summary>
    /// The official 06000425 +0x180 diagram texture, built only when the sketch gsa is missing.
    /// </summary>
    private static Texture2D BuildDiagramTexture(ReferenceSketchMapSample sample)
    {
        if (sample == null)
        {
            return null;
        }

        return BuildDiagramTexture(sample.Width, sample.Height, sample.PixelsForIndependentRuntime);
    }

    internal static Texture2D BuildDiagramTexture(int width, int height, Color32[] source,
        GameHud publicationOwner = null)
    {
        if (width < 1 || height < 1 || source == null)
        {
            return null;
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        if (!ReferenceEquals(publicationOwner, null)) publicationOwner.diagramTex = texture;
        Color32[] flipped = new Color32[height * width];
        for (int row = 0; row < height; row++)
        {
            Array.Copy(source, width * row, flipped, width * (height - 1 - row), width);
        }

        texture.SetPixels32(flipped);
        texture.Apply();
        return texture;
    }

    public bool Draw(
        float scale,
        float playerWorldX,
        float playerWorldY,
        int? viewportWidth = null,
        int? viewportHeight = null)
    {
        if (!Ready || !IsPositiveFinite(scale) ||
            !IsFinite(playerWorldX) || !IsFinite(playerWorldY))
        {
            return false;
        }

        return DrawForNativeOwner(scale, viewportWidth, viewportHeight);
    }

    internal bool DrawForNativeOwner(float scale, int? viewportWidth = null, int? viewportHeight = null,
        bool touchControls = false)
    {
        if (!Ready || !IsPositiveFinite(scale)) return false;

        // Official GameHud.OnGUI (06000414) holds exactly one LoadSketch (06000425) call and it is
        // the first statement of the showMinimap block (GameHud fields: showMinimap = +0x46,
        // s = +0x4c), called with no further condition:
        //   arm64  0x42d174 ldrb w8,[x26,#0x46] ; 0x42d178 cmp w8,#1 ; 0x42d180 b.ne -> 0x430354
        //          0x42d184 mov x0,x26 ; 0x42d188 bl LoadSketch   (0x42d148 SyncHotbar 0600043d and
        //          0x42d170 DrawDragIcon 06000441 with +0x4c run immediately before it, and the bar
        //          x "Screen.width - 151f * +0x4c" is computed at 0x42d18c right after it)
        //   x86_64 0x442d15 cmpb $0x1,0x46(%rbx) ; 0x442d19 je -> 0x442d50 ; 0x442d1b jmp -> 0x446a72
        //          0x442d50 movq %rbx,%rdi ; 0x442d53 callq LoadSketch (same neighbours: 0x442cf3
        //          SyncHotbar, 0x442d10/0x442d41 DrawDragIcon, 0x442d58 the -151f multiply)
        // LoadSketch's own cache guard turns every later call into a no-op, so one call per frame
        // here latches the canvas fall-back and the two origin halves at the same time point the
        // official OnGUI does: the first frame of the world, after GameBootstrap has published
        // CurWorldW/H. The draw below consumes that latch; nothing else writes the four slots.
        if (hudOwner != null && hudOwner.showMinimap)
        {
            hudOwner.LoadSketchForIndependentRuntime();
        }

        if (hudOwner != null && !hudOwner.showMinimap)
        {
            LastBarRect = Rect.zero;
            LastToggleRect = Rect.zero;
            // showMinimap gates the diagram as well as the compact map in 06000414.
            ClearOpenRects();
            return true;
        }
        int screenWidth = viewportWidth ?? Screen.width;
        // LoadSketch precedes the native bar coordinate/T scale reads.
        float barScale = hudOwner != null ? hudOwner.s : scale;
        float barX = screenWidth - 151f * barScale;
        Texture2D sketchBar = resources.SketchBar;
        LastBarRect = BarRect(viewportWidth ?? Screen.width, barScale);
        TexScale.Draw(barX, 0f, sketchBar, 0f, 0f,
            hudOwner != null ? hudOwner.s : scale);
        // 06000414 arm64 :1995-2003 / x86_64 :2389-2397: the bar is already drawn when
        // Ready and then nonempty mapName admit the centered, outlined title. Do not call a
        // font method to warm it here: Measure/Draw would Ensure on the native skip-text arm.
        if (font.Ready && !string.IsNullOrEmpty(MapName))
        {
            float titleScale = hudOwner != null ? hudOwner.s : scale;
            font.CenterOutline(
                new Rect(barX + 30f * titleScale, 5f * titleScale, 72f * titleScale, 12f * titleScale),
                MapName,
                Color.white,
                titleScale);
        }

        if (hudOwner != null)
        {
            float toggleScale = hudOwner.s;
            LastToggleRect = hudOwner.Px(barX + 131f * toggleScale, 0f, 20f, 19f);
        }
        else
        {
            LastToggleRect = new Rect(barX + 131f * scale, 0f, 20f * scale, 19f * scale);
        }
        if (!touchControls && resources.SketchToggle != null) GUI.DrawTexture(LastToggleRect, resources.SketchToggle);
        if (!touchControls && GUI.Button(LastToggleRect, GUIContent.none, TransparentButton))
        {
            MiniOpen = !MiniOpen;
        }

        if (!MiniOpen)
        {
            ClearOpenRects();
            DrawDiagram(scale, viewportWidth, viewportHeight);
            return true;
        }

        // One gate opens the whole compact minimap slice: Official GameHud.OnGUI (06000414) tests
        // sketchPx (+0x188) != null once, at arm64 06000414.c:2044 (LAB_0042d3b4), and every step
        // below is a child of that single test - UpdateMiniView (:2047), the view draw (:2141),
        // BuildGateMarks from both the drawn and the no-texture branch (:2143 / :2124, call sites
        // 0x42d5e8 and 0x42d64c), the mark hover (:2149-2211) and the two marker draws that read
        // the slots UpdateMiniView published (:2214-2244). Nothing narrower may open it: the body
        // it calls carries its own canvas <= 0 projection fall-back (06000427 :128-135), so a
        // canvas > 0 adapter condition here would leave that official arm unreachable.
        bool minimapOpen = hudOwner != null && hudOwner.SketchRasterLoadedForIndependentRuntime;
        if (!minimapOpen)
        {
            ClearOpenRects();
            DrawDiagram(scale, viewportWidth, viewportHeight);
            return true;
        }

        float framePositionScale = hudOwner.s;
        float baseX = barX - 28f * framePositionScale;
        float baseY = 20f * framePositionScale;
        hudOwner.UpdateMiniViewForIndependentRuntime();
        float mapScale = hudOwner.s;
        LastMapRect = RoundedRect(baseX + 19f * mapScale, baseY + 12f * mapScale,
            LogicalMapPixels * mapScale, LogicalMapPixels * mapScale);
        if (MiniViewTexture != null) GUI.DrawTexture(LastMapRect, MiniViewTexture);

        if (minimapOpen)
        {
            // BuildGateMarks' own guard makes every repeat call a no-op while transferProcs keeps
            // the same List reference, so this stays at the official point in the frame without
            // re-walking the polygons each time; the hover that follows consumes what it built.
            hudOwner.BuildGateMarksForIndependentRuntime();
            if (!touchControls) ShowNearestGateTip(LastMapRect, mapScale);
        }

        if (minimapOpen)
        {
            DrawMarkers(mapScale);
            hudOwner.DrawCompactZoneRings(LastMapRect, MapRadiusLimit(mapScale));
            // Native order: player/party markers, current/next zone rings, chest markers,
            // then the frame. The frame covers the outside edges of those overlays.
            DrawBoxPointMarkers(mapScale);
        }
        else
        {
            LastSelfMarkerRect = default(Rect);
            LastPartyMarkerCount = 0;
        }

        // Official frame slot, 06000414 arm64 :2789-2821 (x86_64 :3250-3271): skFrame (+0x168) is
        // the texture when it exists, otherwise the same DrawTexture (:2910 / x86_64 :3379) takes
        // the generated miniRing (+0x1d8). The null arm only swaps the texture: its rect is
        // (:2803-2806) fVar53 / fVar54 / fVar36 / fVar36 - the rounded map rect the compact slice
        // drew the raster into (:2056, :2076, :2157), not the 176-by-153 frame rect.
        LastFrameRect = FrameRect(viewportWidth ?? Screen.width, scale);
        Texture2D frameTexture = resources.SketchFrame;
        if (frameTexture != null)
        {
            float frameX = Mathf.Round(baseX);
            float frameY = Mathf.Round(baseY);
            float frameSizeScale = hudOwner.s;
            LastFrameRect = new Rect(frameX, frameY,
                Mathf.Round(176f * frameSizeScale), Mathf.Round(153f * frameSizeScale));
            GUI.DrawTexture(LastFrameRect, resources.SketchFrame);
        }
        else if (hudOwner != null)
        {
            DrawStretched(LastMapRect, hudOwner.EnsureMiniRingForIndependentRuntime());
        }

        // Official coordinate readout (06000414 arm64 :2907-2955, x86_64 :3381-3427). The two
        // numbers are the player's own world point, not a camera centre, and the line is drawn by
        // BitmapFont.CenterOutline (060000ba) over a rect derived from the map rect, not left
        // aligned from the map corner; see DrawCoordinateText.
        DrawCoordinateText(scale);

        if (!touchControls && GUI.Button(LastMapRect, GUIContent.none, TransparentButton))
        {
            DiagramOpen = !DiagramOpen;
        }

        float worldButtonScale = hudOwner.s;
        LastWorldButtonRect = new Rect(baseX + 140f * worldButtonScale, baseY + 10f * worldButtonScale,
            26f * worldButtonScale, 26f * worldButtonScale);
        if (!touchControls && resources.SketchWorldButton != null) GUI.DrawTexture(LastWorldButtonRect, resources.SketchWorldButton);
        if (!touchControls && GUI.Button(LastWorldButtonRect, GUIContent.none, TransparentButton))
        {
            DiagramOpen = false;
            WorldMapRequested = true;
            WorldMapDlg.Toggle();
        }

        float plusButtonScale = hudOwner.s;
        LastPlusButtonRect = new Rect(baseX + 150f * plusButtonScale, baseY + 37f * plusButtonScale,
            26f * plusButtonScale, 26f * plusButtonScale);
        if (!touchControls && resources.SketchPlus != null) GUI.DrawTexture(LastPlusButtonRect, resources.SketchPlus);
        if (!touchControls && GUI.Button(LastPlusButtonRect, GUIContent.none, TransparentButton))
        {
            StepZoom(true);
        }

        float minusButtonScale = hudOwner.s;
        LastMinusButtonRect = new Rect(baseX + 155f * minusButtonScale, baseY + 63f * minusButtonScale,
            26f * minusButtonScale, 26f * minusButtonScale);
        if (!touchControls && resources.SketchMinus != null) GUI.DrawTexture(LastMinusButtonRect, resources.SketchMinus);
        if (!touchControls && GUI.Button(LastMinusButtonRect, GUIContent.none, TransparentButton))
        {
            StepZoom(false);
        }

        DrawDiagram(scale, viewportWidth, viewportHeight);
        return true;
    }

    // 06000414 arm64 0x42ea80-0x42ec30 / x86_64 :3381-3427; long form in Build/WB-148-report.md.
    // Gates in official order - PlayerT != null (0x42eaa4), get_Ready (060000b0, 0x42eacc),
    // 0.0 < CurWorldW (0x42eaec) - all three fail to 0x42ec34 and skip the line. position is read
    // twice, x from 0x42eb04 and y from 0x42eb68, with CurWorldH read in between (0x42eb5c). Output
    // is BitmapFont.CenterOutline (060000ba) at 0x42ec30 over Rect(map x, map y + s*8, map width,
    // s*12), opaque white, scale s; the official map rect SW - 160s / 32s / 138s is MapRect.
    private void DrawCoordinateText(float scale)
    {
        Transform playerT = GameBootstrap.PlayerT;
        if (playerT == null || !font.Ready || !(GameBootstrap.CurWorldW > 0f))
        {
            return;
        }

        Vector3 firstPosition = playerT.position;
        int worldX = Mathf.RoundToInt(
            (firstPosition.x * GameBootstrap.CurPpu + GameBootstrap.CurWorldW * 0.5f) *
            CoordinateWorldScale);
        // CurWorldH comes before the second position read (0x42eb5c then 0x42eb68).
        float worldHeight = GameBootstrap.CurWorldH;
        Vector3 secondPosition = playerT.position;
        int worldY = Mathf.RoundToInt(
            (worldHeight * 0.5f - secondPosition.y * GameBootstrap.CurPpu) *
            CoordinateWorldScale);

        // StringLiteral 10989 = "/" and 10604 = " v4"; the trailing " v4" is an official literal.
        float coordinateScale = hudOwner != null ? hudOwner.s : scale;
        font.CenterOutline(
            new Rect(
                LastMapRect.x,
                LastMapRect.y + 8f * coordinateScale,
                LastMapRect.width,
                12f * coordinateScale),
            worldX.ToString(CultureInfo.CurrentCulture) + "/" +
                worldY.ToString(CultureInfo.CurrentCulture) + " v4",
            Color.white,
            hudOwner != null ? hudOwner.s : scale);
    }

    public void ToggleDiagram()
    {
        if (!disposed)
        {
            DiagramOpen = !DiagramOpen;
        }
    }

    // The two zoom slices of official UIManager.Update (0x06000fa8) instead of a second key binding
    // elsewhere: arm64 0x3a7fd8 GetKeyDown(0x3d) or 0x3a7fe8 GetKeyDown(0x10e) call GameHud.MinimapZoom
    // with zoomIn = 1, 0x3a8074 GetKeyDown(0x2d) or 0x3a8084 GetKeyDown(0x10d) call it with
    // zoomIn = 0, i.e. '=' / Keypad+ and '-' / Keypad-. Both are independent ifs of the guarded
    // chain, so a frame carrying all four key-downs performs both steps, in before out.
    public void ZoomMinimap(bool zoomIn)
    {
        StepZoom(zoomIn);
    }

    // Pointer and keyboard share one slice: both call GameHud.MinimapZoom (06000450) on the bound
    // owner, which owns the step, the clamp to [0.5f, 3f] and the unconditional miniDirty set, so
    // there is no second clamp here for either to drift from. An unbound carrier publishes no
    // raster at all - the compact minimap only draws inside the owner sketchPx gate - so there is
    // nothing for a step to invalidate either.
    private void StepZoom(bool zoomIn)
    {
        if (disposed || hudOwner == null)
        {
            return;
        }

        hudOwner.MinimapZoom(zoomIn);
    }

    public void CloseDiagram()
    {
        DiagramOpen = false;
    }

    public bool ConsumeWorldMapRequest()
    {
        bool requested = WorldMapRequested;
        WorldMapRequested = false;
        return requested;
    }

    public bool MouseOver(Vector2 topOriginPointer, int screenWidth, float scale)
    {
        if (hudOwner != null) return hudOwner.MouseOverMinimapForIndependentRuntime(topOriginPointer, screenWidth, scale);
        return GameHud.MinimapContainsForIndependentRuntime(topOriginPointer, screenWidth, scale,
            true, MiniOpen, sketchPixels != null);
    }

    // Official 06000427 takes no centre argument and no second algorithm: the body derives its own
    // sample centre from GameBootstrap.PlayerT (arm64 06000427.c:114-137) and there is exactly one
    // place that composes the raster - the bound GameHud owner. The frame scale crossing here is
    // the value GameHud.OnGUI writes into +0x04c (06000414 arm64 :279-293); nothing else is
    // published into the body. A caller that wants a compact raster therefore has to supply the
    // official inputs (GameBootstrap.PlayerT / CurPpu / CurWorldW,H), not a camera-derived centre:
    // ReferenceFirstWorldWindowOwner.ResolveMinimapCenter was that camera quantity and is gone as
    // of WB-148; the coordinate readout above reads the same official point.
    internal bool UpdateMiniView(float scale)
    {
        if (disposed || hudOwner == null)
        {
            return false;
        }

        hudOwner.PublishUiScaleForIndependentRuntime(scale);
        hudOwner.UpdateMiniViewForIndependentRuntime();
        return true;
    }

    // Official GameHud +0x210 / +0x214, written by UpdateMiniView from the same body that publishes
    // the raster; there is no second offset that a marker could read.
    private float MarkerOffsetX => hudOwner.MiniMarkOffsetXForIndependentRuntime;
    private float MarkerOffsetY => hudOwner.MiniMarkOffsetYForIndependentRuntime;

    // The mark radius every marker test uses (06000414 arm64 :2159-2161): half the drawn map size
    // minus one, computed from the same rounded size the map rect was drawn with, so the hover and
    // the two marker draws share one limit instead of each deriving its own.
    private static float MapRadiusLimit(float scale)
    {
        return Mathf.Round(LogicalMapPixels * scale) * 0.5f - 1f;
    }
    internal Vector2 DiagramFraction(float worldX, float worldY)
    {
        return new Vector2(
            (worldX - SampleOriginX) / EffectiveCanvasWidth,
            (worldY - SampleOriginY) / EffectiveCanvasHeight);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        // One destroy for the one compact raster. 06000427 replaces +0x178 without destroying the
        // previous texture when the size changes (arm64 :149-156), so a resize leaks the old GPU
        // object exactly as the official body does; that native behaviour is kept rather than
        // "fixed", which leaves the current instance as the only texture to release here.
        DestroyOwned(MiniViewTexture);
        if (hudOwner != null)
        {
            hudOwner.ClearMiniViewForIndependentRuntime();
        }
        // Official 06000425 destroys the +0x180 diagram texture it built from the .smp sample
        // before it is replaced. The gsa-backed texture is the ResLoader cache entry and is
        // never destroyed, exactly as the official body leaves +0x170 alone.
        if (sketchTexture == null)
        {
            DestroyOwned(diagramTexture);
        }

        disposed = true;
    }

    // The shortest consuming stretch the marks have: GameHud.OnGUI (06000414) projects every built
    // mark with MiniOffsetOf and shows the nearest hit inside the same +0x188 gate that called
    // UpdateMiniView, drew the raster and ran BuildGateMarks (arm64 06000414.c:2149-2211, cross-
    // checked with x86_64 06000414.c:2555 + 2847-2859, where the single mousePosition read lands
    // in one Vector2 whose both halves the distance uses). Anchors, arm64:
    //   :2153 Event.current null and :2156 gateMarks null both -> LAB_00431d68 -> FUN_00177a74(),
    //         the non-returning helper, so both raise instead of leaving the frame tip-less;
    //   :2157-2162 centre of the drawn rect and rlim = (uint)(size * 0.5 - 1);
    //   :2163-2168 tolerance = max(miEz * 5f, s * 5f);
    //   :2180-2207 nearest wins: distance must stay inside the tolerance and below the best kept
    //             candidate, starting from +float.Max; there is no Count pre-test, an empty list
    //             simply keeps no candidate;
    //   :2209-2211 UITip.Show (06000fbc) after nothing but a null test on the kept tip.
    // Nothing here adds a click-to-travel branch, a world map surface or a tip of its own.
    private void ShowNearestGateTip(Rect mapRect, float scale)
    {
        if (hudOwner == null)
        {
            throw new NullReferenceException();
        }

        Event current = Event.current;
        if (current == null)
        {
            throw new NullReferenceException();
        }

        IReadOnlyList<GameHud.GateMark> marks = hudOwner.GateMarksForIndependentRuntime;
        if (marks == null)
        {
            throw new NullReferenceException();
        }

        Vector2 pointer = current.mousePosition;
        float centerX = mapRect.x + mapRect.width * 0.5f;
        float centerY = mapRect.y + mapRect.height * 0.5f;
        float limit = (int)MapRadiusLimit(scale);
        float tolerance = Mathf.Max(
            hudOwner.MiniEffectiveZoomForIndependentRuntime * 5f,
            scale * 5f);
        float best = float.MaxValue;
        string tip = null;
        for (int index = 0; index < marks.Count; index++)
        {
            GameHud.GateMark mark = marks[index];
            float offsetX;
            float offsetY;
            if (!hudOwner.MiniOffsetOfForIndependentRuntime(
                    mark.wx,
                    mark.wy,
                    limit,
                    out offsetX,
                    out offsetY))
            {
                continue;
            }

            float deltaX = pointer.x - (centerX + offsetX);
            float deltaY = pointer.y - (centerY + offsetY);
            float distance = deltaX * deltaX + deltaY * deltaY;
            if (distance > tolerance * tolerance || distance >= best)
            {
                continue;
            }

            best = distance;
            tip = mark.tip;
        }

        // No empty test on the way out: BuildGateMarks only writes "传送点" (StringLiteral[17031])
        // or "通往 " + MapDef name (06000429 arm64 :214-257), so a kept tip is never empty and the
        // official single null test is kept verbatim.
        if (tip != null)
        {
            UITip.Show(tip, scale);
        }
    }

    // Mate markers, 06000414 arm64 :2337-:2432 / x86_64 :2738-:2891; long form in
    // Build/WB-148-report.md. Party (metadata_usage_02421, +0x48) is read for Count (+0x18) with no
    // null arm - null reaches the non-returning LAB_00431d68 - then re-read as the enumerator source,
    // so foreach over SocialClient.Party is exact; SelfEntityId (metadata_usage_02073, +0x7c) is read
    // per entry and MatePos (0600013f) is the only position source. Nothing is snapshotted, each
    // branch walks the roster at its own gate (:2333 canvas > 0); 06000428 owns the projection.
    private void DrawMarkers(float scale)
    {
        if (hudOwner == null)
        {
            return;
        }

        float centerX = LastMapRect.x + LastMapRect.width * 0.5f;
        float centerY = LastMapRect.y + LastMapRect.height * 0.5f;
        float limit = MapRadiusLimit(scale);
        Texture2D selfTexture = resources.SketchMarkSelf;
        float selfOffsetX = MarkerOffsetX;
        float selfOffsetY = MarkerOffsetY;
        if (selfTexture && selfOffsetX * selfOffsetX + selfOffsetY * selfOffsetY <= limit * limit)
        {
            LastSelfMarkerRect = MarkerRect(
                centerX + selfOffsetX,
                centerY + selfOffsetY,
                selfTexture,
                scale);
            DrawStretched(LastSelfMarkerRect, selfTexture);
        }
        else
        {
            LastSelfMarkerRect = default(Rect);
        }

        LastPartyMarkerCount = 0;
        Texture2D mateTexture = resources.SketchMarkMate;
        if (!mateTexture ||
            hudOwner.SketchCanvasWidthForIndependentRuntime <= 0f)
        {
            return;
        }

        if (SocialClient.Party.Count <= 1)
        {
            return;
        }

        int rlim = (int)limit;
        foreach (var member in SocialClient.Party)
        {
            if (member.Item1 == NetClient.SelfEntityId)
            {
                continue;
            }

            float worldX;
            float worldY;
            if (!CombatWorld.MatePos(member.Item1, out worldX, out worldY))
            {
                continue;
            }

            float offsetX;
            float offsetY;
            if (!hudOwner.MiniOffsetOfForIndependentRuntime(
                    worldX,
                    worldY,
                    rlim,
                    out offsetX,
                    out offsetY))
            {
                continue;
            }

            Rect rect = MarkerRect(centerX + offsetX, centerY + offsetY, mateTexture, scale);
            DrawStretched(rect, mateTexture);
            LastPartyMarkerCount++;
        }
    }

    // Air-drop chest points of the compact minimap, 060003d6 + the 06000414 compact slice (arm64
    // 0x42e304-0x42ea00, x86_64 :3117-:3241). Gate: canvasWX (+0x1bc) > 0 and
    // CustomMaps.IsCustom(CurMapId). Size = max(s * 3, min(s * 14, miEz * 60)) and every point is
    // one MiniOffsetOf against (int)MapRadiusLimit: inside the radius, a halo of size + 2 at
    // (px - 1, py - 1) is drawn first and the core of size after it, both ZoneOverlay.Dot.
    private void DrawBoxPointMarkers(float scale)
    {
        float centerX = LastMapRect.x + LastMapRect.width * 0.5f;
        float centerY = LastMapRect.y + LastMapRect.height * 0.5f;
        float limit = MapRadiusLimit(scale);
        if (hudOwner == null || !(EffectiveCanvasWidth > 0f) ||
            !CustomMaps.IsCustom(GameBootstrap.CurMapId))
        {
            return;
        }

        GameBootstrap.ArenaBoxPoints(boxPts);
        if (boxPts.Count <= 0)
        {
            return;
        }

        float size = Mathf.Max(
            scale * 3f,
            Mathf.Min(scale * 14f, hudOwner.MiniEffectiveZoomForIndependentRuntime * 60f));
        int rlim = (int)limit;
        Texture2D dot = ZoneOverlay.Dot;
        Color previous = GUI.color;
        for (int index = 0; index < boxPts.Count; index++)
        {
            float offsetX;
            float offsetY;
            if (!hudOwner.MiniOffsetOfForIndependentRuntime(
                    boxPts[index].x,
                    boxPts[index].y,
                    rlim,
                    out offsetX,
                    out offsetY))
            {
                continue;
            }

            float x = Mathf.RoundToInt(centerX + offsetX - size * 0.5f);
            float y = Mathf.RoundToInt(centerY + offsetY - size * 0.5f);
            GUI.color = BoxPointHalo;
            GUI.DrawTexture(new Rect(x - 1f, y - 1f, size + 2f, size + 2f), dot);
            GUI.color = BoxPointCore;
            GUI.DrawTexture(new Rect(x, y, size, size), dot);
        }

        GUI.color = previous;
    }

    private void DrawDiagram(
        float scale,
        int? viewportWidth,
        int? viewportHeight)
    {
        LastDiagramRect = default(Rect);
        LastDiagramSelfMarkerRect = default(Rect);
        LastDiagramPartyMarkerCount = 0;
        if (!DiagramOpen || !diagramTexture)
        {
            return;
        }

        scale = hudOwner != null ? hudOwner.s : scale;
        LastDiagramRect = DiagramRect(
            viewportWidth ?? Screen.width,
            viewportHeight ?? Screen.height,
            diagramTexture,
            scale);
        Color previous = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, DiagramImageAlpha);
        DrawStretched(LastDiagramRect, diagramTexture);
        GUI.color = previous;

        // This is the inlined 06000414 self-marker input gate, not the separately
        // callable DiagramFrac helper (which would perform another LoadSketch).
        Transform player = GameBootstrap.PlayerT;
        if (player != null && GameBootstrap.CurWorldW > 0f)
        {
            float worldX = player.position.x * GameBootstrap.CurPpu + GameBootstrap.CurWorldW * 0.5f;
            float worldY = GameBootstrap.CurWorldH * 0.5f - player.position.y * GameBootstrap.CurPpu;
            Vector2 selfFraction = DiagramFraction(worldX, worldY);
            LastDiagramSelfMarkerRect = DiagramMarkerRect(
                LastDiagramRect,
                selfFraction,
                resources.SketchMarkSelf,
                scale);
            DrawStretched(LastDiagramSelfMarkerRect, resources.SketchMarkSelf);
        }

        // Official 1.6.0 OnGUI 0600041b, full-map party markers: W/H and the two live
        // scale reads precede enumeration (ARM 43a56c-43a5d4, x86 450631-4506c7).
        Texture2D mateTexture = resources.SketchMarkMate;
        if (mateTexture && SocialClient.Party.Count > 1)
        {
            float markerWidth = GsaDecoder.W(resources.SketchMarkMate) * hudOwner.s;
            float markerHeight = GsaDecoder.H(resources.SketchMarkMate) * hudOwner.s;
            foreach (var member in SocialClient.Party)
            {
                if (member.Item1 == NetClient.SelfEntityId)
                {
                    continue;
                }

                float worldX;
                float worldY;
                if (!CombatWorld.MatePos(member.Item1, out worldX, out worldY))
                {
                    continue;
                }

                // Each axis takes its own positive-canvas branch. The fallback divides the
                // raw coordinate by the current world dimension, without origin subtraction,
                // clamping or a positive-world guard (ARM 43a794-43a844, x86 45092d-450a02).
                float canvasWidth = EffectiveCanvasWidth;
                float fractionX = canvasWidth > 0f
                    ? (worldX - SampleOriginX) / canvasWidth
                    : worldX / GameBootstrap.CurWorldW;
                float canvasHeight = EffectiveCanvasHeight;
                float fractionY = canvasHeight > 0f
                    ? (worldY - SampleOriginY) / canvasHeight
                    : worldY / GameBootstrap.CurWorldH;
                Rect marker = new Rect(
                    Mathf.Round(LastDiagramRect.x + fractionX * LastDiagramRect.width - markerWidth * 0.5f),
                    Mathf.Round(LastDiagramRect.y + fractionY * LastDiagramRect.height - markerHeight * 0.5f),
                    Mathf.Round(markerWidth),
                    Mathf.Round(markerHeight));
                // The texture is re-read at the draw; the original adds no second Unity-null gate.
                GUI.DrawTexture(marker, resources.SketchMarkMate);
                LastDiagramPartyMarkerCount++;
            }
        }

        DrawDiagramZoneOverlay(scale);
    }

    // Air-drop chest points of the full-map overlay, 060003d6 + the 06000414 slice behind
    // ZoneOverlay.Showing and CurWorldW > 0 (arm64 0x42fd78-0x430354, x86_64 :4098-:4300). Size
    // = max(9, s * 9) and a point is drawn only while both DiagramFrac halves stay in [0, 1];
    // the halo of size + 2 at (px - 1, py - 1) again precedes the core of size, both
    // ZoneOverlay.Dot, and the rectangle is the diagram rect the markers above already use.
    private void DrawDiagramZoneOverlay(float scale)
    {
        if (!ZoneOverlay.Showing || !(GameBootstrap.CurWorldW > 0f))
        {
            return;
        }

        // Native full-map zone prefix allocates the shared circle buffer before Dot, saves
        // color before collecting boxes, and always reaches the rings even with no boxes.
        if (hudOwner._zonePts == null) hudOwner._zonePts = new Vector2[96];
        Texture2D dot = ZoneOverlay.Dot;
        Color previous = GUI.color;
        float dotSize = Mathf.Max(2f, scale + scale);
        GameBootstrap.ArenaBoxPoints(boxPts);
        float size = Mathf.Max(9f, scale * 9f);
        for (int index = 0; index < boxPts.Count; index++)
        {
            Vector2 fraction = DiagramFraction(boxPts[index].x, boxPts[index].y);
            if (!(fraction.x >= 0f) || !(fraction.x <= 1f) ||
                !(fraction.y >= 0f) || !(fraction.y <= 1f))
            {
                continue;
            }

            float x = Mathf.RoundToInt(
                LastDiagramRect.x + fraction.x * LastDiagramRect.width - size * 0.5f);
            float y = Mathf.RoundToInt(
                LastDiagramRect.y + fraction.y * LastDiagramRect.height - size * 0.5f);
            GUI.color = BoxPointHalo;
            GUI.DrawTexture(new Rect(x - 1f, y - 1f, size + 2f, size + 2f), dot);
            GUI.color = BoxPointCore;
            GUI.DrawTexture(new Rect(x, y, size, size), dot);
        }

        hudOwner.DrawDiagramZoneRing(ZoneOverlay.cx, ZoneOverlay.cy, ZoneOverlay.r,
            new Color(0.85f, 0.45f, 1f, 0.95f), 1, LastDiagramRect, dotSize, dot);
        if (ZoneOverlay.nextR > 1f && Mathf.Abs(ZoneOverlay.nextR - ZoneOverlay.r) > 1f)
        {
            hudOwner.DrawDiagramZoneRing(ZoneOverlay.nextCx, ZoneOverlay.nextCy, ZoneOverlay.nextR,
                new Color(0.8f, 1f, 1f, 0.9f), 2, LastDiagramRect, dotSize, dot);
        }

        GUI.color = previous;
    }

    internal static Rect DiagramMarkerRect(
        Rect diagramRect,
        Vector2 fraction,
        Texture2D texture,
        float scale)
    {
        return MarkerRect(
            diagramRect.x + fraction.x * diagramRect.width,
            diagramRect.y + fraction.y * diagramRect.height,
            texture,
            scale);
    }

    private static Rect MarkerRect(
        float centerX,
        float centerY,
        Texture2D texture,
        float scale)
    {
        float width = GsaTextureScale.Width(texture) * scale;
        float height = GsaTextureScale.Height(texture) * scale;
        return new Rect(
            Mathf.Round(centerX - width * 0.5f),
            Mathf.Round(centerY - height * 0.5f),
            Mathf.Round(width),
            Mathf.Round(height));
    }

    internal static Color32 CompositeFallback(Color32 source)
    {
        if (source.a >= 250)
        {
            return source;
        }

        int inverse = 255 - source.a;
        return new Color32(
            (byte)((source.a * source.r + inverse * FallbackColor.r) / 255),
            (byte)((source.a * source.g + inverse * FallbackColor.g) / 255),
            (byte)((source.a * source.b + inverse * FallbackColor.b) / 255),
            255);
    }

    private static Rect RoundedRect(float x, float y, float width, float height)
    {
        return new Rect(
            Mathf.Round(x),
            Mathf.Round(y),
            Mathf.Round(width),
            Mathf.Round(height));
    }

    private static Rect NaturalRect(float x, float y, Texture2D texture, float scale)
    {
        return new Rect(
            x,
            y,
            GsaTextureScale.Width(texture) * scale,
            GsaTextureScale.Height(texture) * scale);
    }

    private static void DrawNatural(float x, float y, Texture2D texture, float scale)
    {
        if (texture)
        {
            GUI.DrawTexture(NaturalRect(x, y, texture, scale), texture);
        }
    }

    private static void DrawStretched(Rect rect, Texture2D texture)
    {
        if (texture)
        {
            GUI.DrawTexture(rect, texture);
        }
    }

    private void ClearOpenRects()
    {
        LastFrameRect = default(Rect);
        LastMapRect = default(Rect);
        LastWorldButtonRect = default(Rect);
        LastPlusButtonRect = default(Rect);
        LastMinusButtonRect = default(Rect);
        LastSelfMarkerRect = default(Rect);
        LastPartyMarkerCount = 0;
        LastDiagramRect = default(Rect);
        LastDiagramSelfMarkerRect = default(Rect);
        LastDiagramPartyMarkerCount = 0;
    }

    private static bool IsPositiveFinite(float value)
    {
        return value > 0f && IsFinite(value);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    internal static void DestroyOwned(Texture2D texture)
    {
        if (!texture)
        {
            return;
        }

#if UNITY_EDITOR
        if (!UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.DestroyImmediate(texture);
            return;
        }
#endif
        UnityEngine.Object.Destroy(texture);
    }
}

}
