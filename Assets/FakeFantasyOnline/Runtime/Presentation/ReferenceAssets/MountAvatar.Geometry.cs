using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class MountAvatar
{
    private static void ApplyBodyGeom(Data d, LayerTask layer)
    {
        DimgDecoder.AnimGeom geom = layer.geom;
        int dirs = Mathf.Max(geom.dirs, 1);
        int fpd = Mathf.Max(geom.frames / dirs, 1);
        layer.dirs = dirs;
        layer.fpd = fpd;
        layer.dirDone = new bool[dirs];
        Sprite[,] grid = new Sprite[dirs, fpd];
        int seats = 1;
        foreach (DimgDecoder.FrameGeom frame in geom.f)
        {
            if (frame.binds != null && frame.binds.Length > 1)
            {
                seats = frame.binds.Length / 2;
                break;
            }
        }
        Vector2[][,] saddles = new Vector2[seats][,];
        for (int seat = 0; seat < seats; seat++) saddles[seat] = new Vector2[dirs, fpd];
        for (int dir = 0; dir < dirs; dir++)
        {
            for (int f = 0; f < fpd; f++)
            {
                int index = dir * fpd + f;
                if (index >= geom.f.Length) continue;
                DimgDecoder.FrameGeom frame = geom.f[index];
                if (!frame.hasBind) continue;
                for (int seat = 0; seat < seats; seat++)
                {
                    int x = frame.bindx;
                    int y = frame.bindy;
                    if (frame.binds != null && seat * 2 + 1 < frame.binds.Length)
                    {
                        x = frame.binds[seat * 2];
                        y = frame.binds[seat * 2 + 1];
                    }
                    saddles[seat][dir, f] = new Vector2(
                        frame.nLeft + x - geom.boundCx, frame.nTop + y - geom.boundCy);
                }
            }
        }
        if (layer.walk)
        {
            d.walk = grid;
            d.walkSeatSaddle = saddles;
        }
        else
        {
            d.stand = grid;
            d.standSeatSaddle = saddles;
        }
        d.seatCount = Mathf.Max(d.seatCount, seats);
        if (layer.walk)
        {
            d.dirs = dirs;
            d.dirsFromWalk = true;
        }
        else if (!d.dirsFromWalk) d.dirs = dirs;

        if (!layer.walk || d.stand == null)
        {
            d.boundCy = geom.boundCy;
            List<int> widths = new List<int>(geom.f.Length);
            int scale = Mathf.Max(1, geom.scale);
            foreach (DimgDecoder.FrameGeom frame in geom.f)
                if (frame.w > 0) widths.Add(frame.w / scale);
            if (widths.Count == 0) d.boundW = geom.boundW;
            else
            {
                widths.Sort();
                d.boundW = widths[widths.Count / 2];
            }
        }
        if (!layer.walk || d.stand == null)
            d.belowAnchor = Mathf.Max(geom.boundH - geom.boundCy, 0);
        d.ok = true;
    }

    private static void ApplyOverlayGeom(Data d, LayerTask layer, DimgDecoder.AnimGeom body)
    {
        DimgDecoder.AnimGeom geom = layer.geom;
        int dirs = Mathf.Max(geom.dirs, 1);
        int fpd = Mathf.Max(geom.frames / dirs, 1);
        int bodyDirs = Mathf.Max(body.dirs, 1);
        int bodyFpd = Mathf.Max(body.frames / bodyDirs, 1);
        layer.dirs = dirs;
        layer.fpd = fpd;
        layer.dirDone = new bool[dirs];
        Sprite[,] grid = new Sprite[dirs, fpd];
        Vector2[,] offsets = new Vector2[dirs, fpd];
        for (int dir = 0; dir < dirs; dir++)
        {
            int bodyDir = Mathf.Min(dir, bodyDirs - 1);
            for (int f = 0; f < fpd; f++)
            {
                int index = dir * fpd + f;
                if (index >= geom.f.Length) continue;
                DimgDecoder.FrameGeom frame = geom.f[index];
                int bodyIndex = bodyDir * bodyFpd + Mathf.Min(f, bodyFpd - 1);
                int left = 0, top = 0;
                if (bodyIndex >= 0 && bodyIndex < body.f.Length)
                {
                    left = body.f[bodyIndex].nLeft;
                    top = body.f[bodyIndex].nTop;
                }
                offsets[dir, f] = new Vector2(
                    left + frame.nLeft - body.boundCx, top + frame.nTop - body.boundCy);
            }
        }
        if (layer.kind == 1)
        {
            if (layer.walk) { d.walkTop = grid; d.walkTopOff = offsets; }
            else { d.standTop = grid; d.standTopOff = offsets; }
        }
        else
        {
            if (layer.walk) { d.walkFx = grid; d.walkFxOff = offsets; }
            else { d.standFx = grid; d.standFxOff = offsets; }
        }
    }

    private static Sprite[,] GridOf(Data d, LayerTask layer)
    {
        if (layer.kind == 1) return layer.walk ? d.walkTop : d.standTop;
        if (layer.kind == 0) return layer.walk ? d.walk : d.stand;
        return layer.walk ? d.walkFx : d.standFx;
    }

    private static LayerTask BodyTaskFor(Job job, bool walking)
    {
        foreach (LayerTask layer in job.layers)
            if (layer.kind == 0 && layer.walk == walking) return layer;
        return null;
    }
}

}
