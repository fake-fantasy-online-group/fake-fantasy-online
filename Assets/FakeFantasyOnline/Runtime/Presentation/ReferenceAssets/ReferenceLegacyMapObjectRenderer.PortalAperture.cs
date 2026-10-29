using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceLegacyMapObjectRenderer
{
    private struct PortalAnchor
    {
        internal int Id, X, ScreenY, Order;
    }
    private sealed class ApertureStrip
    {
        internal int Left, Width, FrontY;
        internal readonly List<ActorAperturePart> ActorParts = new List<ActorAperturePart>();
        internal Sprite Front, Rear, Foreground;
    }
    private sealed class ActorAperturePart
    {
        internal Sprite Sprite;
        internal int Depth;
        internal bool WasRear;
    }
    private readonly List<PortalAnchor> portalAnchors = new List<PortalAnchor>();
    private readonly Dictionary<(Texture2D Texture, bool ActorSupport), List<ApertureStrip>> apertureStrips =
        new Dictionary<(Texture2D Texture, bool ActorSupport), List<ApertureStrip>>();

    private void PreparePortalAnchors(GsmMap sourceMap)
    {
        portalAnchors.Clear();
        if (ResLoader.I == null) return;
        foreach (GsmMap.Obj item in sourceMap.libObjs)
        {
            // Limit loads to the audited FY portal family, not every map animation.
            if (item.id != ReferencePortalApertureProfile.PortalId && item.id != 0x013000c9 &&
                item.id != 0x013006a5 && item.id != 0x01300709 && item.id != 0x0130076d) continue;
            ResLoader.ObjSprite image = ResLoader.I.LoadObj(item.id, item.xl, item.yl);
            if (image.tex == null || image.frames <= 1 ||
                !ReferencePortalApertureProfile.IsPortal(item.id, image.path, item.xl, item.yl, image.clipBy)) continue;
            portalAnchors.Add(new PortalAnchor {
                Id = item.id,
                X = item.sx + item.xl / 2,
                ScreenY = item.sy + image.clipBy - item.pz,
                Order = item.sy + image.clipBy
            });
        }
    }

    // This separately-authored model contains only the wooden doorway's rear
    // wall/floor. Its stone/wood front jambs are different scene objects. Keep
    // its native clipArr rectangles and lower only paired rear-plane slices.
    private int PortalBackplaneOrderLimit(ReferenceGsmObject value,
        ResLoader.ObjSprite image, ReferenceMapObjectKind kind)
    {
        if (GameBootstrap.UseIsoY || kind != ReferenceMapObjectKind.Library ||
            value.Id != 0x142d1a2d || value.Width != 109 || value.Height != 169 ||
            image.frames != 1 || image.path == null ||
            !image.path.EndsWith("/JZ_TTD_6701.gso", StringComparison.OrdinalIgnoreCase) ||
            image.nLeft != 0 || image.nTop != 0 || image.clipBy != 147 || image.clipWidth != 16 ||
            image.tex.width != 108 * Math.Max(image.hdScale, 1) ||
            image.tex.height != 168 * Math.Max(image.hdScale, 1) ||
            image.clipArr == null || image.clipArr.Length != 7) return int.MaxValue;
        int[] expected = { -16, -8, 0, 8, 16, 24, 33 };
        for (int i = 0; i < expected.Length; i++)
            if (image.clipArr[i] != expected[i]) return int.MaxValue;
        int matches = 0, limit = int.MaxValue;
        foreach (PortalAnchor portal in portalAnchors)
        {
            if (portal.Id != 0x013000c9) continue;
            int x = portal.X - value.SourceX;
            int y = portal.ScreenY - (value.SourceY - value.PivotZ);
            if (x < 0 || x >= value.Width || y < 0 || y >= value.Height + image.clipWidth) continue;
            matches++; limit = portal.Order - 1;
        }
        return matches == 1 ? limit : int.MaxValue;
    }

    private bool TryPlacePortalAperture(GameBootstrap receiver, ReferenceGsmObject value,
        Transform parent, float ppu, float worldWidth, float worldHeight,
        ReferenceMapObjectKind kind, int objectIndex, ResLoader.ObjSprite image)
    {
        if (GameBootstrap.UseIsoY || image.frames != 1 ||
            kind != ReferenceMapObjectKind.Library ||
            (image.clipArr != null && image.clipArr.Length != 0) ||
            !ReferencePortalApertureProfile.TryGet(value.Id, image.path, out int[] polygon,
                out int expectedWidth, out int expectedHeight))
            return false;
        int scale = Math.Max(image.hdScale, 1);
        if (value.Width != expectedWidth || value.Height != expectedHeight ||
            image.nLeft < 0 || image.nTop < 0 ||
            image.nLeft + image.tex.width / (float)scale > value.Width ||
            image.nTop + image.tex.height / (float)scale > value.Height)
            return false;
        int portalOrder = int.MaxValue;
        int matches = 0;
        foreach (PortalAnchor portal in portalAnchors)
        {
            if (!ReferencePortalApertureProfile.CanPair(value.Id, portal.Id)) continue;
            int localX = portal.X - value.SourceX;
            int localY = portal.ScreenY - (value.SourceY - value.PivotZ);
            // Pair only an actual portal in this profile's doorway/support region.
            // No map number, player position, or global effect order is involved.
            if (ReferencePortalApertureProfile.AcceptsAnchor(value.Id, polygon,
                localX, localY, value.Width, value.Height))
            {
                portalOrder = portal.Order;
                matches++;
            }
        }
        if (matches != 1) return false;
        List<FootprintStrip> footprint;
        if (!footprintStrips.TryGetValue(image.tex, out footprint))
        {
            footprint = CreateFootprintStrips(image, ppu);
            footprintStrips.Add(image.tex, footprint);
        }
        bool hasNativeFootprint = footprint != null;
        if (!hasNativeFootprint)
        {
            // Keep this model's original static-pivot fallback, not an inferred
            // collision contour. Any newly supported footprint retains its own path.
            if (value.Id != 0x172d125d) return false;
            footprint = new List<FootprintStrip> {
                new FootprintStrip { Left = 0, Width = image.tex.width, FrontY = int.MinValue }
            };
        }
        int[] actorCorridor = null, staticCorridor = null;
        bool actorDeck = ReferenceWalkableDeckProfile.TryGetMaskDeck(value.Id, image.path, value.Width, value.Height, image, out actorCorridor);
        bool staticActorDeck = !hasNativeFootprint && value.Id == 0x172d125d &&
            ReferenceWalkableDeckProfile.TryGetStaticDeck(value.Id, image.path, value.Width, value.Height,
                image, value.PivotY - value.SourceY, out staticCorridor);
        if (staticActorDeck) { actorDeck = true; actorCorridor = staticCorridor; }
        int actorColumnWidth = ReferenceWalkableDeckProfile.ColumnWidth(value.Id);
        // The static signature includes the instance pivot offset. Validate it
        // BEFORE cache selection: a nonmatching copy must use the old portal
        // partition even when the same texture previously matched the repair.
        var apertureKey = (image.tex, actorDeck);
        if (!apertureStrips.TryGetValue(apertureKey, out List<ApertureStrip> strips))
        {
            strips = new List<ApertureStrip>();
            try
            {
                // A placed support may take this paired-portal branch before
                // the ordinary support hook. Refine its existing rear-plane
                // depth too, without bypassing foreground/island geometry.
                // Some reviewed wood tops/backplanes were classified as Front by
                // the older portal-only profile. Refine only their authored spans,
                // never all Front. Existing preserved islands take precedence.
                bool frontSupports = actorDeck && (ReferencePortalApertureProfile.IsSupportSurface(value.Id) || value.Id == 0x1c2d007c || staticActorDeck);
                int[] jamb = actorDeck ? ReferenceWalkableDeckProfile.PortalJamb(value.Id) : null;
                // A support span does not require a separate renderer per 10px
                // column. Group only equal original ceilings; all local depths
                // and WasRear caps remain distinct and are applied per instance.
                // Re-emit from source texel coordinates, never Sprite.vertices
                // (whose getter is in Unity units, not OverrideGeometry pixels).
                var groups = new List<FootprintStrip>();
                var groupColumns = new List<bool[]>();
                if (actorDeck)
                {
                    // Bound each merged mesh before allocating it. Every source
                    // edge contributes at most one vertical crossing per texel;
                    // support spans add two. This conservative bound also works
                    // for HD textures and keeps every ushort triangle index safe.
                    int maxSpans = 0;
                    var spanCounts = new Dictionary<int, int>();
                    for (int i = 0; i < actorCorridor.Length; i += 4)
                    {
                        spanCounts.TryGetValue(actorCorridor[i], out int count);
                        spanCounts[actorCorridor[i]] = ++count; maxSpans = Math.Max(maxSpans, count);
                    }
                    int bands = 1 + polygon.Length / 2 + (jamb == null ? 0 : jamb.Length / 2) + 2 * maxSpans;
                    foreach (int[] front in ReferencePortalApertureProfile.PreservedFront(polygon)) bands += front.Length / 2;
                    int columnsPerMesh = Math.Max(1, 65532 / checked(4 * bands));
                    var byCeiling = new Dictionary<(int Ceiling, int Chunk), int>();
                    foreach (FootprintStrip original in footprint)
                        for (int x = original.Left; x < original.Left + original.Width; x++)
                        {
                            var key = (original.FrontY, x / columnsPerMesh);
                            if (!byCeiling.TryGetValue(key, out int index))
                            {
                                index = groups.Count; byCeiling.Add(key, index);
                                groups.Add(new FootprintStrip { Left = 0, Width = image.tex.width, FrontY = original.FrontY });
                                groupColumns.Add(new bool[image.tex.width]);
                            }
                            groupColumns[index][x] = true;
                        }
                }
                else
                    foreach (FootprintStrip original in footprint) { groups.Add(original); groupColumns.Add(null); }
                for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
                {
                    FootprintStrip strip = groups[groupIndex];
                    bool[] columns = groupColumns[groupIndex];
                    var split = new ApertureStrip { Left = strip.Left, Width = strip.Width, FrontY = strip.FrontY };
                    strips.Add(split);
                    bool continuousFront = ReferencePortalApertureProfile.ContinuousForeground(value.Id);
                    split.Front = CreateApertureSprite(image, split.Left, split.Width, ppu, polygon,
                        false, suppressPreserved: continuousFront, actorSpans: actorCorridor,
                        frontSupports: frontSupports, jamb: jamb, columns: columns, actorColumnWidth: actorColumnWidth);
                    if (continuousFront)
                        split.Foreground = CreateApertureSprite(image, split.Left, split.Width, ppu, polygon,
                            false, preservedOnly: true, columns: columns);
                    split.Rear = CreateApertureSprite(image, split.Left, split.Width, ppu, polygon, true,
                        actorSpans: actorCorridor, frontSupports: frontSupports, jamb: jamb, columns: columns, actorColumnWidth: actorColumnWidth);
                    if (actorDeck)
                    {
                        var depths = new HashSet<int>();
                        for (int i = 0; i < actorCorridor.Length; i += 4)
                        {
                            int left = actorCorridor[i] * actorColumnWidth * scale;
                            int right = Math.Min(image.tex.width, left + actorColumnWidth * scale);
                            for (int x = left; x < right; x++)
                                if (columns[x]) { depths.Add(actorCorridor[i + 3]); break; }
                        }
                        if (jamb != null) depths.Add(270);
                        foreach (int depth in depths)
                            foreach (bool wasRear in new[] { false, true })
                            {
                                Sprite part = CreateApertureSprite(image, split.Left, split.Width, ppu, polygon, wasRear,
                                    actorSpans: actorCorridor, frontSupports: frontSupports, jamb: jamb,
                                    actorPart: true, actorDepth: depth, columns: columns, actorColumnWidth: actorColumnWidth);
                                if (part != null) split.ActorParts.Add(new ActorAperturePart { Sprite = part, Depth = depth, WasRear = wasRear });
                            }
                    }
                }
                apertureStrips.Add(apertureKey, strips);
            }
            catch
            {
                foreach (ApertureStrip strip in strips) DestroyApertureStrip(strip);
                throw;
            }
        }
        foreach (ApertureStrip strip in strips)
        {
            int originalOrder = strip.FrontY == int.MinValue ? value.PivotY : value.SourceY + strip.FrontY;
            if (strip.Front != null)
                PlaceSpriteCore(receiver, value, parent, ppu, worldWidth, worldHeight,
                    kind, objectIndex, image.path, image, strip.Left, strip.Width, originalOrder,
                    false, strip.Front, strip.Left / scale);
            if (strip.Foreground != null)
                PlaceSpriteCore(receiver, value, parent, ppu, worldWidth, worldHeight,
                    kind, objectIndex, image.path, image, strip.Left, strip.Width,
                    Math.Max(originalOrder, portalOrder + 1), false, strip.Foreground, strip.Left / scale);
            if (strip.Rear != null)
                PlaceSpriteCore(receiver, value, parent, ppu, worldWidth, worldHeight,
                    kind, objectIndex, image.path, image, strip.Left, strip.Width,
                    Math.Min(originalOrder, portalOrder - 1), false, strip.Rear, strip.Left / scale);
            foreach (ActorAperturePart part in strip.ActorParts)
                PlaceSpriteCore(receiver, value, parent, ppu, worldWidth, worldHeight,
                    kind, objectIndex, image.path, image, strip.Left, strip.Width,
                    Math.Min(Math.Min(originalOrder, value.SourceY + part.Depth - value.PivotZ),
                        part.WasRear ? portalOrder - 1 : int.MaxValue), false, part.Sprite, strip.Left / scale);
        }
        return true;
    }

    private static Sprite CreateApertureSprite(ResLoader.ObjSprite image, int left, int width,
        float ppu, int[] polygon, bool rear, bool preservedOnly = false, bool suppressPreserved = false,
        int[] actorSpans = null, bool frontSupports = false, int[] jamb = null,
        bool actorPart = false, int actorDepth = int.MaxValue, bool[] columns = null, int actorColumnWidth = 10)
    {
        int scale = Math.Max(image.hdScale, 1);
        int height = image.tex.height;
        var vertices = new List<Vector2>();
        var triangles = new List<ushort>();
        int[][] preserved = ReferencePortalApertureProfile.PreservedFront(polygon);
        var boundaries = new List<int>();
        var intersections = new List<float>();
        var rearEdges = new List<int>();
        var jambEdges = new List<int>();
        var frontEdges = new List<int>[preserved.Length];
        for (int i = 0; i < frontEdges.Length; i++) frontEdges[i] = new List<int>();
        // Integer texel partition: the complementary meshes have no overlapping pixel area
        // and omit no source pixel. They borrow the original texture and UVs.
        for (int x = left; x < left + width; x++)
        {
            if (columns != null && !columns[x]) continue;
            float logicalX = image.nLeft + (x + 0.5f) / scale;
            boundaries.Clear(); boundaries.Add(0); boundaries.Add(height);
            Sample(polygon, logicalX, rearEdges);
            for (int i = 0; i < preserved.Length; i++) Sample(preserved[i], logicalX, frontEdges[i]);
            jambEdges.Clear();
            if (jamb != null) Sample(jamb, logicalX, jambEdges);
            int column = (int)logicalX / actorColumnWidth;
            if (actorSpans != null)
                for (int i = 0; i < actorSpans.Length; i += 4)
                    if (actorSpans[i] == column)
                    {
                        boundaries.Add(Math.Max(0, Math.Min(height, (actorSpans[i + 1] - image.nTop) * scale)));
                        boundaries.Add(Math.Max(0, Math.Min(height, (actorSpans[i + 2] - image.nTop) * scale)));
                    }
            boundaries.Sort();
            for (int i = 1; i < boundaries.Count; i++)
            {
                int a = boundaries[i - 1], b = boundaries[i];
                if (a == b) continue;
                bool inFront = false;
                for (int j = 0; !inFront && j < frontEdges.Length; j++)
                    inFront = ContainsSpan(frontEdges[j], a, b);
                bool isRear = ContainsSpan(rearEdges, a, b) && !inFront;
                int supportDepth = int.MaxValue;
                if (!inFront)
                {
                    if ((isRear || frontSupports) && actorSpans != null)
                        for (int j = 0; j < actorSpans.Length; j += 4)
                            if (actorSpans[j] == column && a >= (actorSpans[j + 1] - image.nTop) * scale &&
                                b <= (actorSpans[j + 2] - image.nTop) * scale)
                            { supportDepth = actorSpans[j + 3]; break; }
                    // A jamb is a separate solid component with its own base,
                    // not a new floor and not permission to lower the outer cliff.
                    if (!isRear && ContainsSpan(jambEdges, a, b)) supportDepth = 270;
                }
                bool selected = actorPart
                    ? !inFront && rear == isRear && supportDepth == actorDepth
                    : preservedOnly ? inFront
                    : supportDepth == int.MaxValue && (!suppressPreserved || !inFront) && rear == isRear;
                if (selected) Quad(x, a, b);
            }
        }
        if (vertices.Count == 0) return null;
        Sprite sprite = Sprite.Create(image.tex, new Rect(left, 0, width, height),
            new Vector2(0, 1), ppu * scale);
        try { sprite.OverrideGeometry(vertices.ToArray(), triangles.ToArray()); }
        catch { DestroyOwned(sprite); throw; }
        return sprite;

        void Sample(int[] outline, float x, List<int> edges)
        {
            ReferencePortalApertureProfile.ColumnIntersections(outline, x, intersections);
            edges.Clear();
            // Reject a malformed odd crossing count conservatively: no rear
            // pixels are inferred from an incomplete interval.
            if (intersections.Count % 2 != 0) return;
            foreach (float y in intersections)
            {
                int edge = Math.Max(0, Math.Min(height, (int)Math.Ceiling((y - image.nTop) * scale)));
                edges.Add(edge); boundaries.Add(edge);
            }
        }

        bool ContainsSpan(List<int> edges, int top, int bottom)
        {
            for (int i = 0; i + 1 < edges.Count; i += 2)
                if (top >= edges[i] && bottom <= edges[i + 1]) return true;
            return false;
        }

        void Quad(int x, int top, int bottom)
        {
            if (top >= bottom) return;
            ushort start = checked((ushort)vertices.Count);
            // OverrideGeometry takes rect-local pixel positions (bottom-up),
            // unlike Sprite.vertices. Unity applies pivot and PPU itself.
            float x0 = x - left, x1 = x - left + 1;
            vertices.Add(new Vector2(x0, height - top));
            vertices.Add(new Vector2(x1, height - top));
            vertices.Add(new Vector2(x1, height - bottom));
            vertices.Add(new Vector2(x0, height - bottom));
            triangles.Add(start); triangles.Add((ushort)(start + 1)); triangles.Add((ushort)(start + 2));
            triangles.Add(start); triangles.Add((ushort)(start + 2)); triangles.Add((ushort)(start + 3));
        }
    }

    private static void DestroyApertureStrip(ApertureStrip strip)
    {
        DestroyOwned(strip.Front); DestroyOwned(strip.Rear); DestroyOwned(strip.Foreground);
        foreach (ActorAperturePart part in strip.ActorParts) DestroyOwned(part.Sprite);
    }

    private void DisposePortalApertures()
    {
        foreach (List<ApertureStrip> strips in apertureStrips.Values)
            foreach (ApertureStrip strip in strips) DestroyApertureStrip(strip);
        apertureStrips.Clear();
        portalAnchors.Clear();
    }
}
}
