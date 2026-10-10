using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Compatibility boundary for existing consumers. GsmMap.Parse is the only parser.
public static class ReferenceGsmMapParser
{
    public static bool TryParse(byte[] data, out ReferenceGsmMap map)
    {
        ReferenceGsmParseFailure failure;
        return TryParse(data, out map, out failure);
    }

    public static bool TryParse(byte[] data, out ReferenceGsmMap map,
        out ReferenceGsmParseFailure failure)
    {
        map = null;
        failure = ReferenceGsmParseFailure.None;
        try
        {
            map = new ReferenceGsmMap(GsmMap.Parse(data));
        }
        catch (IndexOutOfRangeException)
        {
            failure = ReferenceGsmParseFailure.InvalidObjectSection;
            return false;
        }
        if (map != null && (map.Ok || map.HasCover)) return true;
        failure = data == null || data.Length < 0x6c
            ? ReferenceGsmParseFailure.TruncatedHeader
            : ReferenceGsmParseFailure.LegacyFieldNotFound;
        map = null;
        return false;
    }
}

public enum ReferenceGsmObjectParseOutcome
{
    NotRequested = 0,
    Parsed = 1,
    Partial = 2
}

public enum ReferenceGsmParseFailure
{
    None = 0,
    TruncatedHeader = 1,
    UnsupportedHeader = 2,
    TruncatedCoverHeader = 3,
    UnsupportedCoverBranch = 4,
    UnsupportedObjectBranch = 5,
    UnsupportedModernField = 6,
    InvalidGridShape = 7,
    LegacyFieldNotFound = 8,
    TruncatedLegacyField = 9,
    TruncatedSky = 10,
    TruncatedObjectSection = 11,
    InvalidObjectSection = 12
}

public sealed class ReferenceGsmMap
{
    private readonly GsmMap native;
    private readonly UnityEngine.Texture2D textureBackground;
    private readonly int textureSceneWidth;
    private readonly int textureSceneHeight;
    private IReadOnlyList<ReferenceGsmObject> tileProjection;
    private IReadOnlyList<ReferenceGsmObject> libraryProjection;
    private IReadOnlyList<ReferenceGsmEmbeddedObject> fixedProjection;
    private ReferenceGsmCover coverProjection;
    private ReferenceGsmFieldColor colorProjection;
    private ReferenceGsmSky skyProjection;
    private IReadOnlyList<int> gridProjection;

    internal ReferenceGsmMap(GsmMap owner) => native = owner;

    internal ReferenceGsmMap(GsmMap owner, UnityEngine.Texture2D textureBackground,
        int sceneWidth, int sceneHeight) : this(owner)
    {
        this.textureBackground = textureBackground;
        textureSceneWidth = sceneWidth;
        textureSceneHeight = sceneHeight;
    }

    // The restored official members take the native owner directly (for example
    // 06000b82 CoverFloor.Build(GsmMap)), so the wrapped instance is exposed to
    // same-assembly callers instead of forcing them through the projection.
    internal GsmMap Native => native;

    // BuildWorld's image branch supplies scene dimensions separately from its real
    // partial GsmMap. Keep the original parser fields/flags and borrowed asset intact.
    internal bool HasTextureBackgroundForIndependentRuntime =>
        !ReferenceEquals(textureBackground, null);
    internal UnityEngine.Texture2D TextureBackgroundForIndependentRuntime => textureBackground;
    public int MapWidth => HasTextureBackgroundForIndependentRuntime
        ? textureSceneWidth : native.mapWidth;
    public int MapHeight => HasTextureBackgroundForIndependentRuntime
        ? textureSceneHeight : native.mapHeight;
    public uint Version => native.version;
    public uint Flags => native.flags;
    public int GridWidth => native.vw;
    public int GridHeight => native.vh;
    public IReadOnlyList<int> Grids =>
        gridProjection ??= Array.AsReadOnly(native.grids ?? Array.Empty<int>());
    public int TileSet => native.tileSet;
    public int LegacyFieldOffset => native.legacyFieldOffset;
    public int ParsedLength => native.parsedLength;
    public int TrailingByteCount => Math.Max(0, native.sourceLength - native.parsedLength);
    public int ObjectSectionEnd => native.objectSectionEnd;
    public ReferenceGsmObjectParseOutcome ObjectParseOutcome => native.objsOk
        ? ReferenceGsmObjectParseOutcome.Parsed : ReferenceGsmObjectParseOutcome.Partial;
    public IReadOnlyList<ReferenceGsmObject> TileObjects =>
        tileProjection ??= ProjectObjects(native.tileObjs);
    public IReadOnlyList<ReferenceGsmObject> LibraryObjects =>
        libraryProjection ??= ProjectObjects(native.libObjs);
    public ReferenceGsmSky Sky => !native.hasSky ? null :
        skyProjection ??= new ReferenceGsmSky(native.skyColor, native.cloudType,
            native.cloudDensity, native.cloudDir, native.cloudFdir, native.cloudSpeed);
    public IReadOnlyList<ReferenceGsmEmbeddedObject> EmbeddedObjects =>
        fixedProjection ??= ProjectFixedObjects();
    public bool Ok => native.ok;
    public bool IsLegacy => native.isLegacy;
    public ReferenceGsmFieldColor FieldColor => native.fcColors == null ? null :
        colorProjection ??= new ReferenceGsmFieldColor(native.fcSx, native.fcSy,
            native.fcW, native.fcH, native.fcTileW, native.fcTileH, native.fcColors);
    public bool HasFieldColor => native.hasFieldColor;
    public bool ObjectsOk => native.objsOk;
    public ReferenceGsmCover Cover => native.coverImgs == null ? null :
        coverProjection ??= ProjectCover();
    public bool HasCover => native.hasCover;
    public bool HasSky => native.hasSky;
    public UnityEngine.Color SkyColorUnity => native.SkyColorUnity;
    public int VCode(int x, int y) => native.VCode(x, y);

    private static IReadOnlyList<ReferenceGsmObject> ProjectObjects(List<GsmMap.Obj> source)
    {
        var result = new ReferenceGsmObject[source.Count];
        for (int i = 0; i < result.Length; i++)
        {
            GsmMap.Obj item = source[i];
            result[i] = new ReferenceGsmObject(item.id, item.sx, item.sy,
                item.xl, item.yl, item.py, item.pz);
        }
        return Array.AsReadOnly(result);
    }

    private ReferenceGsmCover ProjectCover()
    {
        var result = new ReferenceGsmCover
        {
            PixelWidth = native.coverPw, PixelHeight = native.coverPh,
            BlockWidth = native.coverBw, BlockHeight = native.coverBh,
            Columns = native.coverCx, Rows = native.coverCy,
            Images = new ReferenceGsmCoverImage[native.coverImgs.Length]
        };
        for (int i = 0; i < native.coverImgs.Length; i++)
        {
            GsmMap.NakedImg img = native.coverImgs[i];
            result.Images[i] = new ReferenceGsmCoverImage
            {
                Width = img.w, Height = img.h, Pitch = img.pitch,
                Attribute = img.attrib, Data = img.data
            };
        }
        return result;
    }

    private IReadOnlyList<ReferenceGsmEmbeddedObject> ProjectFixedObjects()
    {
        var result = new ReferenceGsmEmbeddedObject[native.fixObjs.Count];
        for (int i = 0; i < result.Length; i++)
        {
            GsmMap.FixObj obj = native.fixObjs[i];
            var projected = new ReferenceGsmEmbeddedObject
            {
                SourceX = obj.sx, SourceY = obj.sy, ExtentX = obj.xl,
                ExtentY = obj.yl, FrameCount = obj.frames,
                ClipWidth = obj.clipWidth, ClipCount = obj.clipCount,
                Flags = obj.flags, Effect = obj.effect, ClipOffsets = obj.clipBy,
                Frames = new ReferenceGsmEmbeddedFrame[obj.frameImgs.Length]
            };
            for (int frameIndex = 0; frameIndex < projected.Frames.Length; frameIndex++)
            {
                GsmMap.FixFrame frame = obj.frameImgs[frameIndex];
                projected.Frames[frameIndex] = new ReferenceGsmEmbeddedFrame
                {
                    Left = frame.xOff, Top = frame.yOff,
                    Width = frame.img.w, Height = frame.img.h,
                    Pitch = frame.img.pitch, Attribute = frame.img.attrib,
                    Data = frame.img.data
                };
            }
            result[i] = projected;
        }
        return Array.AsReadOnly(result);
    }
}

public sealed class ReferenceGsmObject
{
    public ReferenceGsmObject(
        int id,
        int sourceX,
        int sourceY,
        int width,
        int height,
        int pivotY,
        int pivotZ)
    {
        Id = id;
        SourceX = sourceX;
        SourceY = sourceY;
        Width = width;
        Height = height;
        PivotY = pivotY;
        PivotZ = pivotZ;
    }

    public int Id { get; }
    public int SourceX { get; }
    public int SourceY { get; }
    public int Width { get; }
    public int Height { get; }
    public int PivotY { get; }
    public int PivotZ { get; }
}

public sealed class ReferenceGsmSky
{
    public ReferenceGsmSky(
        uint color,
        int cloudType,
        int cloudDensity,
        int cloudDirection,
        int cloudFineDirection,
        int cloudSpeed)
    {
        Color = color;
        CloudType = cloudType;
        CloudDensity = cloudDensity;
        CloudDirection = cloudDirection;
        CloudFineDirection = cloudFineDirection;
        CloudSpeed = cloudSpeed;
    }

    public uint Color { get; }
    public int CloudType { get; }
    public int CloudDensity { get; }
    public int CloudDirection { get; }
    public int CloudFineDirection { get; }
    public int CloudSpeed { get; }
}

}
