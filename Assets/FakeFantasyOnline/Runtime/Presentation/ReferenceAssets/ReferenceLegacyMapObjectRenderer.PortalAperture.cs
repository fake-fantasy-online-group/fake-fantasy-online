using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceLegacyMapObjectRenderer
{
    private struct PortalAnchor
    {
        internal int X, ScreenY, Order;
    }
    private sealed class ApertureStrip
    {
        internal int Left, Width, FrontY;
        internal Sprite Front, Rear;
    }
    private readonly List<PortalAnchor> portalAnchors = new List<PortalAnchor>();
    private readonly Dictionary<Texture2D, List<ApertureStrip>> apertureStrips =
        new Dictionary<Texture2D, List<ApertureStrip>>();

    private void PreparePortalAnchors(GsmMap sourceMap)
    {
        portalAnchors.Clear();
        if (ResLoader.I == null) return;
        foreach (GsmMap.Obj item in sourceMap.libObjs)
        {
            if (item.id != ReferencePortalApertureProfile.PortalId) continue;
            ResLoader.ObjSprite image = ResLoader.I.LoadObj(item.id, item.xl, item.yl);
            if (image.tex == null || image.frames <= 1) continue;
            portalAnchors.Add(new PortalAnchor {
                X = item.sx + item.xl / 2,
                ScreenY = item.sy + image.clipBy - item.pz,
                Order = item.sy + image.clipBy
            });
        }
    }

    private bool TryPlacePortalAperture(GameBootstrap receiver, ReferenceGsmObject value,
        Transform parent, float ppu, float worldWidth, float worldHeight,
        ReferenceMapObjectKind kind, int objectIndex, ResLoader.ObjSprite image)
    {
        if (GameBootstrap.UseIsoY || image.frames != 1 ||
            kind != ReferenceMapObjectKind.Library ||
            (image.clipArr != null && image.clipArr.Length != 0) ||
            !ReferencePortalApertureProfile.TryGet(value.Id, image.path, out int[] polygon))
            return false;
        int scale = Math.Max(image.hdScale, 1);
        int expectedHeight = value.Id == 0x182d012d ? 305 : 306;
        if (value.Width != 221 || value.Height != expectedHeight ||
            image.nLeft < 0 || image.nTop < 0 ||
            image.nLeft + image.tex.width / (float)scale > value.Width ||
            image.nTop + image.tex.height / (float)scale > value.Height)
            return false;
        int portalOrder = int.MaxValue;
        int matches = 0;
        foreach (PortalAnchor portal in portalAnchors)
        {
            int localX = portal.X - value.SourceX;
            int localY = portal.ScreenY - (value.SourceY - value.PivotZ);
            // Pair only an actual portal anchored within this doorway image.
            // No map number, player position, or global effect order is involved.
            if (localX >= 0 && localX < value.Width && localY >= 0 && localY < value.Height &&
                ReferencePortalApertureProfile.Column(polygon, localX, out float top, out float bottom) &&
                localY >= top)
            {
                portalOrder = portal.Order;
                matches++;
            }
        }
        if (matches != 1) return false;
        if (!apertureStrips.TryGetValue(image.tex, out List<ApertureStrip> strips))
        {
            List<FootprintStrip> footprint;
            if (!footprintStrips.TryGetValue(image.tex, out footprint))
            {
                footprint = CreateFootprintStrips(image, ppu);
                footprintStrips.Add(image.tex, footprint);
            }
            if (footprint == null) return false;
            strips = new List<ApertureStrip>();
            try
            {
                foreach (FootprintStrip strip in footprint)
                {
                    var split = new ApertureStrip { Left = strip.Left, Width = strip.Width, FrontY = strip.FrontY };
                    strips.Add(split);
                    split.Front = CreateApertureSprite(image, strip.Left, strip.Width, ppu, polygon, false);
                    split.Rear = CreateApertureSprite(image, strip.Left, strip.Width, ppu, polygon, true);
                }
                apertureStrips.Add(image.tex, strips);
            }
            catch
            {
                foreach (ApertureStrip strip in strips) { DestroyOwned(strip.Front); DestroyOwned(strip.Rear); }
                throw;
            }
        }
        foreach (ApertureStrip strip in strips)
        {
            int originalOrder = value.SourceY + strip.FrontY;
            if (strip.Front != null)
                PlaceSpriteCore(receiver, value, parent, ppu, worldWidth, worldHeight,
                    kind, objectIndex, image.path, image, strip.Left, strip.Width, originalOrder,
                    false, strip.Front, strip.Left / scale);
            if (strip.Rear != null)
                PlaceSpriteCore(receiver, value, parent, ppu, worldWidth, worldHeight,
                    kind, objectIndex, image.path, image, strip.Left, strip.Width,
                    Math.Min(originalOrder, portalOrder - 1), false, strip.Rear, strip.Left / scale);
        }
        return true;
    }

    private static Sprite CreateApertureSprite(ResLoader.ObjSprite image, int left, int width,
        float ppu, int[] polygon, bool rear)
    {
        int scale = Math.Max(image.hdScale, 1);
        int height = image.tex.height;
        var vertices = new List<Vector2>();
        var triangles = new List<ushort>();
        // Integer texel partition: the two meshes have no overlapping pixel area
        // and omit no source pixel. They borrow the original texture and UVs.
        for (int x = left; x < left + width; x++)
        {
            int top = 0, bottom = 0;
            if (ReferencePortalApertureProfile.Column(polygon,
                image.nLeft + (x + 0.5f) / scale, out float logicalTop, out float logicalBottom))
            {
                top = Math.Max(0, Math.Min(height, (int)Math.Ceiling((logicalTop - image.nTop) * scale)));
                bottom = Math.Max(top, Math.Min(height, (int)Math.Ceiling((logicalBottom - image.nTop) * scale)));
            }
            if (rear) Quad(x, top, bottom);
            else { Quad(x, 0, top); Quad(x, bottom, height); }
        }
        if (vertices.Count == 0) return null;
        Sprite sprite = Sprite.Create(image.tex, new Rect(left, 0, width, height),
            new Vector2(0, 1), ppu * scale);
        try { sprite.OverrideGeometry(vertices.ToArray(), triangles.ToArray()); }
        catch { DestroyOwned(sprite); throw; }
        return sprite;

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

    private void DisposePortalApertures()
    {
        foreach (List<ApertureStrip> strips in apertureStrips.Values)
            foreach (ApertureStrip strip in strips) { DestroyOwned(strip.Front); DestroyOwned(strip.Rear); }
        apertureStrips.Clear();
        portalAnchors.Clear();
    }
}
}
