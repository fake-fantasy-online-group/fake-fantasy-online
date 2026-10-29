using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceLegacyMapObjectRenderer
{
    private sealed class WalkableDeckStrip
    {
        internal Sprite Sprite;
        internal int Left, Width, Depth;
        internal bool RearPlane;
        internal readonly List<Vector2> Vertices = new List<Vector2>();
        internal readonly List<ushort> Triangles = new List<ushort>();
    }
    private readonly Dictionary<(Texture2D Texture, int PivotZ), List<WalkableDeckStrip>> walkableDeckStrips =
        new Dictionary<(Texture2D Texture, int PivotZ), List<WalkableDeckStrip>>();

    private bool TryPlaceWalkableDeck(GameBootstrap receiver, ReferenceGsmObject value,
        Transform parent, float ppu, float worldWidth, float worldHeight,
        ReferenceMapObjectKind kind, int objectIndex, ResLoader.ObjSprite image)
    {
        if (ReferenceEquals(receiver, null) || GameBootstrap.UseIsoY ||
            kind != ReferenceMapObjectKind.Library || image.frames != 1)
            return false;
        int scale = Math.Max(image.hdScale, 1);
        bool stage = ReferenceWalkableDeckProfile.TryGet(value.Id, image.path, value.Width, value.Height, out int[] columns);
        if (stage)
        {
            if (!ReferenceWalkableDeckProfile.MatchesClips(image.clipWidth, image.clipBy, image.clipArr) ||
                image.nLeft != 0 || image.nTop != 0 || image.tex.width != 668 * scale || image.tex.height != 810 * scale)
                return false;
        }
        bool authoredDeck = stage || ReferenceWalkableDeckProfile.TryGetAuthoredDeck(value.Id, image.path, value.Width, value.Height, image, out columns);
        bool staticDeck = false;
        if (!authoredDeck && !ReferenceWalkableDeckProfile.TryGetMaskDeck(value.Id, image.path, value.Width, value.Height, image, out columns))
        {
            staticDeck = ReferenceWalkableDeckProfile.TryGetStaticDeck(value.Id, image.path, value.Width, value.Height,
                image, value.PivotY - value.SourceY, out columns);
            if (!staticDeck) return false;
        }
        int columnWidth = ReferenceWalkableDeckProfile.ColumnWidth(value.Id);
        var cacheKey = (image.tex, value.PivotZ);
        if (!walkableDeckStrips.TryGetValue(cacheKey, out List<WalkableDeckStrip> strips))
        {
            // Keep the actual authored-slice branch, including the native <=3
            // coalescing and midpoint selection. A collision-mask max contour is
            // not this object's live branch and must not replace its front depth.
            var original = new List<(int Left, int Width, int Depth)>();
            if (authoredDeck)
            {
                foreach (var slice in receiver.GetObjectSlicesForIndependentRuntime(value.Id, image, ppu))
                {
                    if (slice.Item1 == null) return false;
                    original.Add(((int)slice.Item1.rect.x, (int)slice.Item1.rect.width, slice.Item3));
                }
            }
            else
            {
                // The selected support corridor refines the existing footprint
                // branch. Preserve every old front-strip order outside it.
                if (!footprintStrips.TryGetValue(image.tex, out List<FootprintStrip> footprint))
                {
                    footprint = CreateFootprintStrips(image, ppu);
                    footprintStrips.Add(image.tex, footprint);
                }
                if (staticDeck)
                {
                    // The recorded asset took the native static fallback. A new
                    // compatible footprint would change that premise: fall back
                    // rather than silently applying an old support signature.
                    if (footprint != null) return false;
                    original.Add((0, image.tex.width, value.PivotY - value.SourceY));
                }
                else
                {
                    if (footprint == null) return false;
                    foreach (FootprintStrip strip in footprint)
                        original.Add((strip.Left, strip.Width, strip.FrontY));
                }
            }
            var cutSet = new SortedSet<int> { 0, image.tex.width };
            for (int left = columnWidth * scale; left < image.tex.width; left += columnWidth * scale) cutSet.Add(left);
            foreach (var source in original)
            {
                cutSet.Add(source.Left);
                cutSet.Add(source.Left + source.Width);
            }
            var cuts = new List<int>(cutSet);
            strips = new List<WalkableDeckStrip>();
            try
            {
                for (int segment = 0; segment < cuts.Count - 1; segment++)
                {
                    int left = cuts[segment], width = cuts[segment + 1] - left;
                    int front = int.MinValue;
                    foreach (var source in original)
                        if (left >= source.Left && left < source.Left + source.Width)
                        { front = source.Depth; break; }
                    if (front == int.MinValue) throw new InvalidOperationException("Missing authored sprite column.");
                    int column = left / (columnWidth * scale);
                    var verticalCuts = new SortedSet<int> { 0, image.tex.height };
                    for (int i = 0; i < columns.Length; i += 4)
                        if (columns[i] == column)
                        {
                            verticalCuts.Add(columns[i + 1] * scale);
                            verticalCuts.Add(columns[i + 2] * scale);
                        }
                    var edges = new List<int>(verticalCuts);
                    for (int edge = 0; edge < edges.Count - 1; edge++)
                    {
                        int top = edges[edge], bottom = edges[edge + 1], depth = front;
                        bool rearPlane = false;
                        for (int i = 0; i < columns.Length; i += 4)
                            if (columns[i] == column && top >= columns[i + 1] * scale && bottom <= columns[i + 2] * scale)
                            { depth = columns[i + 3]; rearPlane = true; break; }
                        // Resolve each source column BEFORE merging. Two columns
                        // with the same support anchor may have different original
                        // ceilings; taking a group-wide minimum lowers unrelated
                        // walls. The cache includes Z because this clamp uses it.
                        int effectiveDepth = rearPlane ? Math.Min(front, depth - value.PivotZ) : front;
                        Add(image.tex.height - bottom, image.tex.height - top, effectiveDepth, rearPlane);
                    }
                    void Add(int bottom, int top, int depth, bool rearPlane)
                    {
                        // One mesh per effective depth, not one renderer per 10px
                        // column. Disjoint quads may share a mesh because their
                        // original source pixels and ordering are identical.
                        WalkableDeckStrip strip = strips.Find(s => s.Depth == depth && s.RearPlane == rearPlane);
                        if (strip == null)
                        {
                            strip = new WalkableDeckStrip { Left = 0, Width = image.tex.width, Depth = depth, RearPlane = rearPlane };
                            strips.Add(strip);
                        }
                        ushort v = checked((ushort)strip.Vertices.Count);
                        strip.Vertices.AddRange(new[] {
                            new Vector2(left, bottom), new Vector2(left + width, bottom),
                            new Vector2(left + width, top), new Vector2(left, top)
                        });
                        strip.Triangles.AddRange(new[] {v, (ushort)(v+1), (ushort)(v+2), v, (ushort)(v+2), (ushort)(v+3)});
                    }
                }
                foreach (WalkableDeckStrip strip in strips)
                {
                    strip.Sprite = Sprite.Create(image.tex,
                        new Rect(0, 0, image.tex.width, image.tex.height), new Vector2(0, 1), ppu * scale);
                    strip.Sprite.OverrideGeometry(strip.Vertices.ToArray(), strip.Triangles.ToArray());
                    strip.Vertices.Clear();
                    strip.Triangles.Clear();
                }
                walkableDeckStrips.Add(cacheKey, strips);
            }
            catch
            {
                foreach (WalkableDeckStrip strip in strips) DestroyOwned(strip.Sprite);
                throw;
            }
        }
        var group = new List<SpriteRenderer>(strips.Count);
        int maxOrder = int.MinValue;
        long spawnStart = LoadTrace.Now;
        foreach (WalkableDeckStrip strip in strips)
        {
            PlaceSpriteCore(receiver, value, parent, ppu, worldWidth, worldHeight,
                kind, objectIndex, image.path, image, strip.Left, strip.Width,
                // Actor feet use projected map pixels; raised copies of this deck
                // have no separate actor Z. Only the repaired rear plane follows
                // that projection. Every original front-rail depth stays intact.
                value.SourceY + strip.Depth,
                false, strip.Sprite, strip.Left / scale);
            SpriteRenderer renderer = renderers[renderers.Count - 1];
            group.Add(renderer);
            maxOrder = Math.Max(maxOrder, renderer.sortingOrder);
        }
        if (LoadTrace.On) GameBootstrap.ObjSpawnMs += LoadTrace.Ms(spawnStart);
        long fadeStart = LoadTrace.Now;
        RegisterObjectFade(receiver, value, ppu, worldWidth, worldHeight, image, group, maxOrder);
        if (LoadTrace.On) GameBootstrap.ObjFadeMs += LoadTrace.Ms(fadeStart);
        return true;
    }

    private void DisposeWalkableDeckStrips()
    {
        foreach (List<WalkableDeckStrip> strips in walkableDeckStrips.Values)
            foreach (WalkableDeckStrip strip in strips) DestroyOwned(strip.Sprite);
        walkableDeckStrips.Clear();
    }
}
}
