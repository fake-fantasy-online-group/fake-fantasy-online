using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class FittingRoomPreview
{
    private void TryLoadPetGrid(string path)
    {
        DimgDecoder.Anim anim = DecodeAnimPath(path);
        if (!anim.ok || anim.f == null || anim.f.Length == 0) return;
        int dirs = Mathf.Max(1, anim.dirs);
        int framesPerDir = Mathf.Max(1, anim.frames / dirs);
        var grid = new Sprite[dirs, framesPerDir];
        for (int dir = 0; dir < dirs; dir++)
            for (int frame = 0; frame < framesPerDir; frame++)
            {
                int index = dir * framesPerDir + frame;
                if (index >= anim.f.Length) continue;
                DimgDecoder.FrameTex source = anim.f[index];
                if (source.tex != null)
                    grid[dir, frame] = AnchorSprite(source.tex, anim.boundCx, anim.boundCy, source.nLeft, source.nTop, anim.scale);
            }
        petGrid = grid;
        DestroyUnusedFrames(anim, grid);
        petGlowGrid = BuildGlowGrid(path.Substring(0, path.Length - 4) + "e.gsn", anim, dirs, framesPerDir);
    }

    private Sprite[,] BuildGlowGrid(string glowPath, DimgDecoder.Anim baseAnim, int bdirs, int bfpd)
    {
        DimgDecoder.Anim glow = DecodeAnimPath(glowPath);
        if (!glow.ok || glow.f == null || glow.f.Length == 0) return null;
        int dirs = Mathf.Max(1, glow.dirs);
        int framesPerDir = Mathf.Max(1, glow.frames / dirs);
        var grid = new Sprite[bdirs, bfpd];
        bool any = false;
        for (int dir = 0; dir < bdirs; dir++)
            for (int frame = 0; frame < bfpd; frame++)
            {
                int baseIndex = dir * bfpd + frame;
                if (baseIndex >= baseAnim.f.Length) continue;
                int glowIndex = Mathf.Min(dir, dirs - 1) * framesPerDir + frame % framesPerDir;
                if (glowIndex >= glow.f.Length) continue;
                DimgDecoder.FrameTex baseFrame = baseAnim.f[baseIndex];
                DimgDecoder.FrameTex glowFrame = glow.f[glowIndex];
                if (glowFrame.tex == null || (glowFrame.flags & 0x100) == 0) continue;
                grid[dir, frame] = AnchorSprite(glowFrame.tex, baseAnim.boundCx, baseAnim.boundCy,
                    baseFrame.nLeft + glowFrame.nLeft, baseFrame.nTop + glowFrame.nTop, glow.scale);
                any |= grid[dir, frame] != null;
            }
        if (!any) grid = null;
        DestroyUnusedFrames(glow, grid);
        return grid;
    }

    private DimgDecoder.Anim DecodeAnimPath(string path)
    {
        byte[] bytes = ResLoader.I?.Pkg?.Read(path) ?? ResLoader.Read24Res(path);
        return bytes == null ? default : DimgDecoder.DecodeAnim(bytes);
    }

    private static Sprite AnchorSprite(Texture2D texture, int bcx, int bcy, int nLeft, int nTop, int scale)
    {
        int width = texture.width, height = texture.height;
        scale = Mathf.Max(1, scale);
        Vector2 pivot = bcx == 0 && bcy == 0 ? new Vector2(.5f, .06f) : new Vector2(
            (bcx - nLeft) * (float)scale / width, 1f - (bcy - nTop) * (float)scale / height);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), pivot, ppu * scale);
    }

    private static void DestroyUnusedFrames(DimgDecoder.Anim anim, Sprite[,] grid)
    {
        if (anim.f == null) return;
        var used = new HashSet<Texture2D>();
        if (grid != null)
            foreach (Sprite sprite in grid)
                if (sprite != null && sprite.texture != null) used.Add(sprite.texture);
        foreach (DimgDecoder.FrameTex frame in anim.f)
            if (frame.tex != null && !used.Contains(frame.tex)) Object.Destroy(frame.tex);
    }

    private void DestroyPetGrids()
    {
        if (petSr != null) { petSr.sprite = null; petSr.enabled = false; }
        if (petGlowSr != null) { petGlowSr.sprite = null; petGlowSr.enabled = false; }
        var textures = new HashSet<Texture2D>();
        DestroyGrid(ref petGrid, textures);
        DestroyGrid(ref petGlowGrid, textures);
    }

    private static void DestroyGrid(ref Sprite[,] grid, HashSet<Texture2D> textures)
    {
        if (grid == null) return;
        foreach (Sprite sprite in grid)
        {
            if (sprite == null) continue;
            Texture2D texture = sprite.texture;
            if (texture != null && textures.Add(texture)) Object.Destroy(texture);
            Object.Destroy(sprite);
        }
        grid = null;
    }
}
}
