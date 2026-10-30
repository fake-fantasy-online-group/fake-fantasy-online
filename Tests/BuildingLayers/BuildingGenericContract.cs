using System;
using System.Collections.Generic;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

class BuildingGenericContract
{
    static int checks;
    static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
    static byte[] Mask()
    {
        var data = new byte[62 + 9 * 4];
        void Write(int offset, int value) => Array.Copy(BitConverter.GetBytes(value), 0, data, offset, 4);
        Write(0, -1); Write(4, 65538); Write(58, 4);
        for (int i = 0; i < 4; i++) { Write(62 + 8 * i, 5 + 10 * i); Write(66 + 8 * i, 15 + 10 * i); data[94 + i] = 129; }
        return data;
    }
    static void Main()
    {
        // Fabricated IDs/paths/dimensions. The middle byte is the existing generic
        // library category, not an identifier copied from a game resource.
        const int id = (1 << 24) | (45 << 16) | 7;
        foreach (bool owned in new[] { false, true })
        foreach (int scale in new[] { 1, 2, 4 })
        foreach (int pz in new[] { -13, 0, 17 })
        foreach (int mode in new[] { 0, 1, 2 })
        {
            var loader = new ResLoader(); ResLoader.I = loader;
            var image = new ResLoader.ObjSprite {
                path = "synthetic/building.gso", tex = new Texture2D(40 * scale, 60 * scale),
                frames = 1, hdScale = scale, clipWidth = 16, clipBy = 11,
                clipArr = mode == 0 ? new[] { 5, 9, 13 } : null
            };
            loader.Images[id] = image;
            if (mode == 1) loader.Pkg.Files["synthetic/building.mask"] = Mask();
            var map = new ReferenceGsmMap { Native = new GsmMap() };
            map.Native.libObjs.Add(new GsmMap.Obj { id = id, sx = 31, sy = 73, py = 99, pz = pz, xl = 41, yl = 61 });
            var owner = owned ? new GameBootstrap() : null;
            using (var renderer = new ReferenceLegacyMapObjectRenderer(map, loader,
                new GameObject("Synthetic").transform, objectOwner: owner))
            {
                Check(renderer.Build(), "first Build"); Check(!renderer.Build(), "Build latch");
                int count = mode == 0 ? 3 : mode == 1 ? 4 : 1;
                Check(renderer.Placements.Count == count, "ordinary branch count");
                int covered = 0;
                for (int i = 0; i < count; i++)
                {
                    var p = renderer.Placements[i]; var sprite = renderer.Rendered[i].sprite;
                    Check(p.RectangleX == covered, "no source gap or overlap"); covered += p.RectangleWidth;
                    Check(p.RectangleHeight == image.tex.height, "full source height retained");
                    Check(sprite.vertices == null && sprite.triangles == null, "no asset-specific mesh");
                    Check(sprite.texture == image.tex && sprite.ppu == 100 * scale, "texture ownership and HD");
                    Check(p.ScreenY == 73 - pz, "PZ projects appearance only");
                    int expected = mode == 0 ? 73 + image.clipArr[i] : mode == 1 ? 93 + 10 * i : 99;
                    Check(p.SortingOrder == expected, "generic authored/footprint/static order");
                }
                Check(covered == image.tex.width, "all source pixels retained once");
                if (owned && mode == 0) Check(owner.FadeCalls == 1 && owner.FadeGroup.Count == 3 && owner.FadeOrder == 86, "authored fade preserved");
            }
        }
        Console.WriteLine("PASS " + checks + " synthetic ordinary building dispatch checks (managed host, not Unity)");
    }
}
