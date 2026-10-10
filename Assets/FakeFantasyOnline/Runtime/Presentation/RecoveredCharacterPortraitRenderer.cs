using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{

// Compatibility contract for non-selection portrait consumers. The native TitleFlow page
// owns its PlayerAvatar slots independently and never supplies or probes this compositor.
internal interface IRecoveredCharacterPortraitRenderer
{
    bool CanRender(CharacterSelectionItem character, int slot);

    void Draw(CharacterSelectionItem character, int slot, Rect clientRect);

    void ReleaseSlotPortraits();
}

// The historical TitleFlow mapping comments below describe this former selection compositor.
// They are retained for the existing standalone Editor consumers. Current selection uses
// TitleFlow.CharacterSelection and its actual PlayerAvatar owner; these methods do not run there.
/// <summary>
/// Source-owned stand-direction portrait compositor for the reviewed PlayerAvatar asset cone.
/// </summary>
/// <remarks>
/// The layer offsets and depth-plus-Q21-rank ordering follow PlayerAvatar.FramesOf (06000857);
/// source-over blending and union bounds follow BakeStill (0600085B). Missing body, head, binding,
/// palette, depth input, or requested equipment art fails closed.
/// </remarks>
/// <remarks>
/// Frozen dual-architecture bodies of this TitleFlow slice are copied unchanged into
/// Build/ReferenceReverseGhidra-1.5.8/queries/wbc118-copy-of-wbc116r-titleflow-[arm64|x86_64]-20261001/
/// with hashes in queries/COPY-MANIFEST-wbc118-titleflow.json; the comments below cite those copies
/// as &lt;token&gt;.c:&lt;line&gt; inside the matching architecture directory.
/// </remarks>
internal sealed class RecoveredCharacterPortraitRenderer :
    IRecoveredCharacterPortraitRenderer,
    IDisposable
{
    private readonly ReferenceResourceLoader _resources;
    private readonly AvatarDepthTable _avatarDepth;
    // Official 1.5.8 per-slot store: TitleFlow.slotDoll (+0x1c8) holds one PlayerAvatar per roster
    // row, SlotDoll (0x06000ebc) returns that entry and only Builds when it is still null,
    // DrawCharSelect (0x06000ef7) asks for row i every frame (06000ef7.c:207 and :220), and both
    // <SetupNet>b__96_3 (0x06000f03, arm64 06000f03.c:36-77) and OnDestroy (0x06000ec3, arm64
    // 06000ec3.c:67-90) Dispose every non-null entry and null it. One store keyed by slot index;
    // there is no second portrait store or second avatar owner. SingleAppearanceSlot below is an
    // entry of this same store, not a second one.
    private readonly Dictionary<int, Portrait> _portraits = new Dictionary<int, Portrait>();

    // Supplies that have no roster row of their own - the Editor appearance audits and the fitted
    // draw - use this one reserved entry so every Texture2D this type creates sits in the store
    // and leaves it through ReleaseSlotPortraits / Dispose. The character-selection rows the
    // surface asks for are its visible slots 0..2, so this index is outside that range and cannot
    // shadow a drawn row; a built supplementary supply releases the previous entry first, which
    // is how its inputs change without leaking.
    private const int SingleAppearanceSlot = int.MaxValue;
    private bool _disposed;

    // Official 1.5.8 TitleFlow.DOLL_FLIP: the System.Boolean static at static offset 19 (0x13) of
    // TitleFlow. Inside the frozen slice 0x06000ebc..0x06000f0a its only writer is TitleFlow..cctor
    // (0x06000eff), which assigns 1 (arm64 06000eff.c:100), and its only reader is DrawDollInFrame
    // (0x06000ee2). It stays a read-only field rather than a C# const because DrawDollInFrame
    // re-reads the static at every branch that consumes it - twice on arm64 (06000ee2.c:82 and
    // :110), three times on x86_64 (0x3a26bd, 0x3a270e, 0x3a2846 in the body at rva 0x3a2580) -
    // and a const would make one arm unreachable.
    private static readonly bool DollFlip = true;

    public RecoveredCharacterPortraitRenderer()
    {
        _resources = ReferenceResourceLoader.OpenStreamingAssets();
        _avatarDepth = AvatarDepthTable.Load(_resources);
    }

    public bool CanRender(CharacterSelectionItem character, int slot)
    {
        if (_disposed || slot < 0)
        {
            return false;
        }

        ReferenceAvatarAssets assets;
        if (!ReferenceAvatarAssetPlan.TryCreate(character, out assets))
        {
            return false;
        }

        return PortraitForSlot(slot, assets) != null;
    }

    // Official counterpart: TitleFlow.<SetupNet>b__96_3 (0x06000f03) walks the whole slotDoll
    // array on every roster receive, Disposes the non-null PlayerAvatar of every row and nulls the
    // entry (arm64 06000f03.c:36-77); TitleFlow.OnDestroy (0x06000ec3) runs the same loop when the
    // title component goes away (arm64 06000ec3.c:67-90). Roster receive, create, delete and
    // return-to-login all arrive as a new roster list instance, so RecoveredTitleCharacterSurface
    // calls this from the existing EnsureRoster change edge instead of polling anything.
    public void ReleaseSlotPortraits()
    {
        if (_disposed)
        {
            return;
        }

        foreach (Portrait portrait in _portraits.Values)
        {
            DestroyPortrait(portrait);
        }

        _portraits.Clear();
    }

#if UNITY_EDITOR
    internal bool CanRenderAuditPlan(
        int gender,
        int body,
        int head,
        int hair,
        int cap,
        int backpack,
        int rightWeapon,
        int leftWeapon,
        int face)
    {
        CharacterSelectionItem character = AuditCharacter(
            gender,
            body,
            head,
            hair,
            cap,
            backpack,
            rightWeapon,
            leftWeapon,
            face);
        ReferenceAvatarAssets assets;
        if (!ReferenceAvatarAssetPlan.TryCreate(character, out assets))
        {
            return false;
        }

        Portrait portrait = SupplySingleAppearance(assets);
        return portrait != null && portrait.Texture;
    }

    internal bool TryGetAuditVisibleBounds(
        int gender,
        int body,
        int head,
        int hair,
        int cap,
        int backpack,
        int rightWeapon,
        int leftWeapon,
        int face,
        out Rect visibleBounds)
    {
        visibleBounds = default(Rect);
        CharacterSelectionItem character = AuditCharacter(
            gender,
            body,
            head,
            hair,
            cap,
            backpack,
            rightWeapon,
            leftWeapon,
            face);
        ReferenceAvatarAssets assets;
        if (!ReferenceAvatarAssetPlan.TryCreate(character, out assets))
        {
            return false;
        }

        // The Editor audits measure one appearance at a time and never draw a roster row, so they
        // supply SingleAppearanceSlot instead of a visible slot - same build pipeline, same store,
        // and the entry they leave behind is what ReleaseSlotPortraits and Dispose destroy.
        Portrait portrait = SupplySingleAppearance(assets);
        if (portrait == null || !portrait.Texture)
        {
            return false;
        }

        const float clientWidth = 172f;
        const float clientHeight = 120f;
        // Same TitleFlow.DrawDollInFrame (0x06000ee2) placement as Draw, including DOLL_FLIP. The
        // mirrored draw also mirrors every body sub-rectangle inside the texture, so the left edge
        // of the cached body bounds becomes texture.width - xMax while width and height - the only
        // measurements this gate consumes - stay unchanged.
        float drawX = DollDrawX(clientWidth, portrait);
        float bodyX = DollFlip
            ? portrait.Texture.width - portrait.BodyOpaqueBounds.xMax
            : portrait.BodyOpaqueBounds.x;
        float drawY = PortraitDrawY(
            clientHeight,
            portrait.NameTopPixels,
            portrait.HeadPixels,
            portrait.DrawOffsetY);
        Rect opaque = new Rect(
            drawX + bodyX,
            drawY + portrait.BodyOpaqueBounds.y,
            portrait.BodyOpaqueBounds.width,
            portrait.BodyOpaqueBounds.height);
        float minX = Mathf.Max(0f, opaque.xMin);
        float minY = Mathf.Max(0f, opaque.yMin);
        float maxX = Mathf.Min(clientWidth, opaque.xMax);
        float maxY = Mathf.Min(clientHeight, opaque.yMax);
        if (maxX <= minX || maxY <= minY)
        {
            return false;
        }

        visibleBounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }
#endif

    public void Draw(CharacterSelectionItem character, int slot, Rect clientRect)
    {
        if (_disposed || slot < 0)
        {
            return;
        }

        ReferenceAvatarAssets assets;
        if (!ReferenceAvatarAssetPlan.TryCreate(character, out assets))
        {
            return;
        }

        Portrait portrait = PortraitForSlot(slot, assets);
        if (portrait == null || !portrait.Texture)
        {
            return;
        }

        // TitleFlow.DrawDollInFrame clips the stand sprite to the supplied slot group. Drawing in
        // absolute GUI space lets tall equipment/wing pixels spill into the name and property
        // rows, which also makes the correctly anchored Doll appear vertically displaced.
        GUI.BeginGroup(clientRect);
        try
        {
            float x = DollDrawX(clientRect.width, portrait);
            float y = PortraitDrawY(
                clientRect.height,
                portrait.NameTopPixels,
                portrait.HeadPixels,
                portrait.DrawOffsetY);
            Rect destination =
                new Rect(x, y, portrait.Texture.width, portrait.Texture.height);
            if (DollFlip)
            {
                // The texCoords rectangle starts at u = 1 and runs one unit to the left, which
                // mirrors the sprite horizontally: arm64 passes the literal group 0x3f800000, 0,
                // 0xbf800000, 0x3f800000 after the destination rectangle
                // (06000ee2.c:114-116), and x86_64 loads the same floats from 0x1117c7c (1.0) and
                // 0x111c770 (-1.0, 1.0) at 0x3a2865/0x3a286d. Both reach the 3-argument
                // GUI.DrawTextureWithTexCoords wrapper, whose fourth argument it fills with 1, so
                // the effective alphaBlend is true; the non-flipped arm reaches the 2-argument
                // GUI.DrawTexture wrapper, i.e. StretchToFill with alphaBlend true and
                // imageAspect 0.
                GUI.DrawTextureWithTexCoords(
                    destination,
                    portrait.Texture,
                    new Rect(1f, 0f, -1f, 1f));
            }
            else
            {
                GUI.DrawTexture(destination, portrait.Texture, ScaleMode.StretchToFill, true);
            }
        }
        finally
        {
            GUI.EndGroup();
        }
    }

    // TitleFlow.DrawDollInFrame (0x06000ee2): the destination x is client.width * 0.5 minus the
    // horizontal anchor, and the anchor is PlayerAvatar.anchorX (+0x1c) when DOLL_FLIP is clear but
    // sprite.texture.width - anchorX when it is set, because the mirrored sprite puts that many
    // pixels to the LEFT of the anchor. Both architectures read it that way: arm64
    // 06000ee2.c:83-93 and :108, and x86_64 0x3a26df (`sub eax, [r14+0x1c]` right after the
    // get_width call at 0x3a26d9) versus 0x3a2714 (`mov eax, [r14+0x1c]`), with r14 holding the
    // avatar since 0x3a2599.
    private static float DollDrawX(float clientWidth, Portrait portrait)
    {
        return clientWidth * 0.5f -
               (DollFlip ? portrait.Texture.width - portrait.DrawOffsetX : portrait.DrawOffsetX);
    }

    // Same member's vertical term: top = max(max(nameTopPx, headPx), 1),
    // available = max(0, clientHeight - top) and y = clientHeight - available * 0.5 - anchorY
    // (+0x20). arm64 06000ee2.c:64-78 and :109; x86_64 0x3a2674-0x3a268a (the integer top),
    // 0x3a2765-0x3a278b (maxss then mulss by the 0.5 literal at 0x1117c18) and 0x3a282d
    // (`subps xmm0, xmm3` against the cvtdq2ps pair built from the two anchors at
    // 0x3a2718-0x3a2724).
    internal static float PortraitDrawY(
        float clientHeight,
        int nameTopPixels,
        int headPixels,
        int anchorY)
    {
        int top = Math.Max(Math.Max(nameTopPixels, headPixels), 1);
        float available = Math.Max(clientHeight - top, 0f);
        return clientHeight - available * 0.5f - anchorY;
    }

    internal static int PortraitAnchorY(int minimumLayerY)
    {
        return -minimumLayerY;
    }

    internal bool DrawFitted(
        CharacterSelectionItem character,
        Rect bounds,
        float maximumScale)
    {
        if (_disposed || character == null || !(maximumScale > 0f))
        {
            return false;
        }

        ReferenceAvatarAssets assets;
        if (!ReferenceAvatarAssetPlan.TryCreate(character, out assets))
        {
            return false;
        }

        // No official counterpart in DrawCharSelect: this fitted draw is not a roster row, so it
        // supplies SingleAppearanceSlot and releases whatever that entry held before, instead of
        // baking a Texture2D that no store entry would own.
        Portrait portrait = SupplySingleAppearance(assets);
        if (portrait == null || !portrait.Texture)
        {
            return false;
        }

        float factor = Mathf.Min(
            maximumScale,
            bounds.width / portrait.Texture.width,
            bounds.height / portrait.Texture.height);
        if (!(factor > 0f))
        {
            return false;
        }

        float width = portrait.Texture.width * factor;
        float height = portrait.Texture.height * factor;
        GUI.DrawTexture(
            new Rect(
                bounds.x + (bounds.width - width) * 0.5f,
                bounds.yMax - height,
                width,
                height),
            portrait.Texture,
            ScaleMode.StretchToFill,
            true);
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ReleaseSlotPortraits();
        _resources.Dispose();
        _disposed = true;
    }

    // TitleFlow.SlotDoll (0x06000ebc) reads slotDoll[slot] first and only calls PlayerAvatar.Build
    // for the rows whose entry is still null, then stores what Build returned back into that slot
    // (arm64 06000ebc.c:58-62 and :91-99). The native test is "entry != 0", so a null entry is not
    // a reusable result: it is asked again on the next frame. Only a non-null entry short-circuits,
    // which this reflects with `cached != null`; nothing here treats a drawn-but-incomplete
    // portrait as null, and no retry timing was added.
    private Portrait PortraitForSlot(int slot, ReferenceAvatarAssets assets)
    {
        Portrait cached;
        if (_portraits.TryGetValue(slot, out cached) && cached != null)
        {
            return cached;
        }

        Portrait built = BuildPortrait(assets);
        _portraits[slot] = built;
        return built;
    }

    // Supply for the audits and the fitted draw: one reserved entry, its inputs change by
    // replacing it, and the replacement goes through the same destroy path the roster release
    // uses, so those textures never outlive the store they sit in.
    private Portrait SupplySingleAppearance(ReferenceAvatarAssets assets)
    {
        if (_disposed)
        {
            return null;
        }

        ReleaseSlot(SingleAppearanceSlot);
        return PortraitForSlot(SingleAppearanceSlot, assets);
    }

    private void ReleaseSlot(int slot)
    {
        if (_disposed)
        {
            return;
        }

        Portrait portrait;
        if (!_portraits.TryGetValue(slot, out portrait))
        {
            return;
        }

        DestroyPortrait(portrait);
        _portraits.Remove(slot);
    }

    private static void DestroyPortrait(Portrait portrait)
    {
        if (portrait == null || !portrait.Texture)
        {
            return;
        }

#if UNITY_EDITOR
        if (!UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.DestroyImmediate(portrait.Texture);
            return;
        }
#endif
        UnityEngine.Object.Destroy(portrait.Texture);
    }

    private Portrait BuildPortrait(ReferenceAvatarAssets assets)
    {
        ReferenceAvatarStandSourcePose pose;
        if (!ReferenceAvatarStandAssembler.TryAssemble(
                _resources, _avatarDepth, assets, out pose))
        {
            return null;
        }

        int headPixels;
        int nameTopPixels;
        if (!TryMeasureTitleTop(assets, out headPixels, out nameTopPixels))
        {
            return null;
        }

        List<Layer> layers = new List<Layer>(pose.Layers.Count);
        foreach (ReferenceAvatarStandSourceLayer source in pose.Layers)
        {
            layers.Add(new Layer(
                source.Tag,
                source.Frame,
                source.OriginX,
                source.OriginY));
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        foreach (Layer layer in layers)
        {
            minX = Math.Min(minX, layer.X);
            minY = Math.Min(minY, layer.Y);
            maxX = Math.Max(maxX, layer.X + layer.Frame.Width);
            maxY = Math.Max(maxY, layer.Y + layer.Frame.Height);
        }

        int width = maxX - minX;
        int height = maxY - minY;
        if (width < 1 || height < 1 || width > 1024 || height > 1024)
        {
            return null;
        }

        Color32[] topDown = new Color32[width * height];
        foreach (Layer layer in layers)
        {
            Composite(topDown, width, height, layer, minX, minY);
        }

        Rect opaqueBounds;
        if (!TryOpaqueBounds(topDown, width, height, out opaqueBounds))
        {
            return null;
        }

        Rect bodyOpaqueBounds = default(Rect);
        bool hasBodyOpaqueBounds = false;
        foreach (Layer layer in layers)
        {
            if (layer.Tag != 0)
            {
                continue;
            }

            Rect bodyFrameBounds;
            if (!TryOpaqueBounds(
                    layer.Frame.Pixels,
                    layer.Frame.Width,
                    layer.Frame.Height,
                    out bodyFrameBounds))
            {
                return null;
            }

            bodyOpaqueBounds = new Rect(
                layer.X - minX + bodyFrameBounds.x,
                layer.Y - minY + bodyFrameBounds.y,
                bodyFrameBounds.width,
                bodyFrameBounds.height);
            hasBodyOpaqueBounds = true;
            break;
        }
        if (!hasBodyOpaqueBounds)
        {
            return null;
        }

        Color32[] bottomUp = new Color32[topDown.Length];
        for (int sourceY = 0; sourceY < height; sourceY++)
        {
            Array.Copy(
                topDown,
                sourceY * width,
                bottomUp,
                (height - 1 - sourceY) * width,
                width);
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        texture.SetPixels32(bottomUp);
        texture.Apply(false, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return new Portrait(
            texture,
            -minX,
            PortraitAnchorY(minY),
            nameTopPixels,
            headPixels,
            opaqueBounds,
            bodyOpaqueBounds);
    }

    private static CharacterSelectionItem AuditCharacter(
        int gender,
        int body,
        int head,
        int hair,
        int cap,
        int backpack,
        int rightWeapon,
        int leftWeapon,
        int face)
    {
        return new CharacterSelectionItem(
            "audit",
            "audit",
            0,
            gender,
            head,
            hair,
            1,
            string.Empty,
            new CharacterAppearanceRecord(
                body,
                cap,
                backpack,
                rightWeapon,
                leftWeapon,
                face,
                0,
                0,
                0),
            Array.Empty<int>(),
            0);
    }

    private static bool TryOpaqueBounds(
        Color32[] pixels,
        int width,
        int height,
        out Rect bounds)
    {
        bounds = default(Rect);
        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a == 0)
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

        bounds = Rect.MinMaxRect(minX, minY, maxX + 1, maxY + 1);
        return true;
    }

    private bool TryMeasureTitleTop(
        ReferenceAvatarAssets assets,
        out int headPixels,
        out int nameTopPixels)
    {
        headPixels = 0;
        nameTopPixels = 0;
        byte[] bodyBytes = _resources.ReadRes(assets.BodyPath);
        byte[] headBytes = _resources.ReadRes(assets.HeadPath);
        int bodyFramesPerDirection = DimgDecoder.FramesPerDir(bodyBytes);
        int headFramesPerDirection = DimgDecoder.FramesPerDir(headBytes);
        if (bodyFramesPerDirection < 1 || headFramesPerDirection < 1)
        {
            return false;
        }

        byte[] capBytes = null;
        foreach (ReferenceAvatarPart part in assets.Parts)
        {
            if (part.Tag != 1)
            {
                continue;
            }

            for (int index = 0; index < part.CandidatePaths.Length; index++)
            {
                byte[] candidate = _resources.ReadRes(part.CandidatePaths[index]);
                if (candidate != null && DimgDecoder.FramesPerDir(candidate) > 0)
                {
                    capBytes = candidate;
                    break;
                }
            }

            if (capBytes == null)
            {
                return false;
            }
            break;
        }

        int capFramesPerDirection = capBytes == null
            ? 0
            : DimgDecoder.FramesPerDir(capBytes);
        for (int direction = 0; direction < 5; direction++)
        {
            DimgFrame body;
            DimgFrame head;
            if (!DimgDecoder.TryDecodeFrame(
                    bodyBytes,
                    direction * bodyFramesPerDirection,
                    out body) ||
                !DimgDecoder.TryDecodeFrame(
                    headBytes,
                    direction * headFramesPerDirection,
                    out head) ||
                body.Bindings.Length < 1)
            {
                return false;
            }

            int bodyCenterY = body.HasBind
                ? body.CenterY
                : body.BoundCenterY - body.Top;
            int headTop = -((body.Bindings[0].Y - head.CenterY) - bodyCenterY);
            headPixels = Math.Max(headPixels, headTop);
            nameTopPixels = Math.Max(nameTopPixels, headTop);

            if (capBytes == null)
            {
                continue;
            }

            DimgFrame cap;
            if (!DimgDecoder.TryDecodeFrame(
                    capBytes,
                    direction * capFramesPerDirection,
                    out cap))
            {
                return false;
            }

            int capTop = -((body.Bindings[0].Y - cap.CenterY) - bodyCenterY);
            nameTopPixels = Math.Max(nameTopPixels, capTop);
        }

        headPixels = Math.Max(headPixels, 0);
        nameTopPixels = Math.Max(nameTopPixels, headPixels);
        return true;
    }

    private static void Composite(
        Color32[] destination,
        int destinationWidth,
        int destinationHeight,
        Layer layer,
        int minX,
        int minY)
    {
        int targetX = layer.X - minX;
        int targetY = layer.Y - minY;
        for (int y = 0; y < layer.Frame.Height; y++)
        {
            int destinationY = targetY + y;
            if (destinationY < 0 || destinationY >= destinationHeight)
            {
                continue;
            }

            for (int x = 0; x < layer.Frame.Width; x++)
            {
                int destinationX = targetX + x;
                if (destinationX < 0 || destinationX >= destinationWidth)
                {
                    continue;
                }

                Color32 source = layer.Frame.Pixels[y * layer.Frame.Width + x];
                if (source.a == 0)
                {
                    continue;
                }

                int index = destinationY * destinationWidth + destinationX;
                destination[index] = SourceOver(source, destination[index]);
            }
        }
    }

    private static Color32 SourceOver(Color32 source, Color32 destination)
    {
        if (source.a == 255 || destination.a == 0)
        {
            return source;
        }

        int destinationAlpha = (destination.a * (255 - source.a) + 127) / 255;
        int alpha = source.a + destinationAlpha;
        if (alpha == 0)
        {
            return default(Color32);
        }

        int rounding = alpha / 2;
        return new Color32(
            (byte)((source.r * source.a + destination.r * destinationAlpha + rounding) / alpha),
            (byte)((source.g * source.a + destination.g * destinationAlpha + rounding) / alpha),
            (byte)((source.b * source.a + destination.b * destinationAlpha + rounding) / alpha),
            (byte)alpha);
    }

    private sealed class Portrait
    {
        public Portrait(
            Texture2D texture,
            int drawOffsetX,
            int drawOffsetY,
            int nameTopPixels,
            int headPixels,
            Rect opaqueBounds,
            Rect bodyOpaqueBounds)
        {
            Texture = texture;
            DrawOffsetX = drawOffsetX;
            DrawOffsetY = drawOffsetY;
            NameTopPixels = nameTopPixels;
            HeadPixels = headPixels;
            OpaqueBounds = opaqueBounds;
            BodyOpaqueBounds = bodyOpaqueBounds;
        }

        public Texture2D Texture { get; }
        public int DrawOffsetX { get; }
        public int DrawOffsetY { get; }
        public int NameTopPixels { get; }
        public int HeadPixels { get; }
        public Rect OpaqueBounds { get; }
        public Rect BodyOpaqueBounds { get; }
    }

    private struct Layer
    {
        public Layer(int tag, DimgFrame frame, int x, int y)
        {
            Tag = tag;
            Frame = frame;
            X = x;
            Y = y;
        }

        public int Tag { get; }
        public DimgFrame Frame { get; }
        public int X { get; }
        public int Y { get; }
    }

}
}
