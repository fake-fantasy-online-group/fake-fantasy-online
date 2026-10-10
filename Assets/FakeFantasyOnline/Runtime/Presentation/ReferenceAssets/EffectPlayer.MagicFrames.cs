using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official EffectPlayer sprite factory and magic-frame dependency cone used by
/// TargetRing. Unreached general effect branches remain open recovery, not replacement code.
/// </summary>
public partial class EffectPlayer : MonoBehaviour
{
    private static Sprite MakeSprite(
        Texture2D tex,
        int boundCx,
        int boundCy,
        int nLeft,
        int nTop,
        int scale = 1)
    {
        int width = tex.width;
        int height = tex.height;
        scale = Math.Max(scale, 1);
        Vector2 pivot = boundCx == 0 && boundCy == 0
            ? new Vector2(0.5f, 0.5f)
            : new Vector2(
                (float)(boundCx - nLeft) * scale / width,
                1f - (float)(boundCy - nTop) * scale / height);
        return Sprite.Create(
            tex,
            new Rect(0f, 0f, width, height),
            pivot,
            scale * 100f);
    }

    public static Sprite[] GetMagicFrames(int id)
    {
        return LoadMagicFrames("magic" + id, 0f);
    }

    public static Sprite[] GetMagicFramesAll(int id)
    {
        string key = "magicAll" + id;
        _fxPinned.Add(key);
        Sprite[] cached;
        if (spriteCache.TryGetValue(key, out cached))
        {
            _fxBudget.Touch(key);
            return cached;
        }

        Sprite[] result = null;
        byte[] data = ReadRes(string.Format(
            "magic/magic/magic{0:D4}.gso",
            id));
        if (data != null)
        {
            DimgDecoder.Anim animation = DimgDecoder.DecodeAnim(data);
            if (animation.ok && animation.f != null && animation.f.Length > 0)
            {
                List<Sprite> frames = new List<Sprite>();
                for (int index = 0; index < animation.f.Length; index++)
                {
                    DimgDecoder.FrameTex frame = animation.f[index];
                    if (frame.tex)
                    {
                        frames.Add(MakeSprite(
                            frame.tex,
                            animation.boundCx,
                            animation.boundCy,
                            frame.nLeft,
                            frame.nTop,
                            animation.scale));
                    }
                }

                if (frames.Count > 0)
                {
                    result = frames.ToArray();
                }
            }
        }

        spriteCache[key] = result;
        NoteFx(key, result);
        return result;
    }

    private static Sprite[] LoadMagicFramesDir(
        string name,
        int dir,
        out bool multiDir)
    {
        multiDir = false;
        int id;
        if (!name.StartsWith("magic") ||
            !int.TryParse(name.Substring(5), out id))
        {
            return LoadMagicFrames(name, 0f);
        }

        int knownDirections;
        if (dirsCache.TryGetValue(name, out knownDirections))
        {
            multiDir = knownDirections > 1;
        }

        string key = multiDir
            ? name + "@d" + dir
            : name + "@d";
        Sprite[] cached;
        if (spriteCache.TryGetValue(key, out cached))
        {
            multiDir = dirsCache.TryGetValue(name, out knownDirections) &&
                knownDirections > 1;
            _fxBudget.Touch(key);
            return cached;
        }

        Sprite[] result = null;
        byte[] data = ReadRes(string.Format(
            "magic/magic/magic{0:D4}.gso",
            id));
        if (data != null)
        {
            DimgDecoder.Anim animation = DimgDecoder.DecodeAnim(data);
            if (animation.ok && animation.f != null && animation.f.Length > 0)
            {
                attribCache[name] = animation.attrib;
                int directions = Math.Max(animation.dirs, 1);
                dirsCache[name] = directions;
                multiDir = animation.dirs > 1;
                key = multiDir
                    ? name + "@d" + dir
                    : name + "@d";

                int perDirection = animation.frames / directions;
                perDirection = Math.Max(perDirection, 1);
                int direction = multiDir
                    ? Math.Max(Math.Min(dir, directions - 1), 0)
                    : 0;
                int start = direction * perDirection;
                List<Sprite> frames = new List<Sprite>();
                for (int offset = 0;
                    offset < perDirection && start + offset < animation.f.Length;
                    offset++)
                {
                    DimgDecoder.FrameTex frame = animation.f[start + offset];
                    if (frame.tex)
                    {
                        frames.Add(MakeSprite(
                            frame.tex,
                            animation.boundCx,
                            animation.boundCy,
                            frame.nLeft,
                            frame.nTop,
                            animation.scale));
                    }
                }

                if (frames.Count > 0)
                {
                    result = frames.ToArray();
                }
                DropUnusedFrames(animation, start, perDirection);
            }
        }

        spriteCache[key] = result;
        NoteFx(key, result);
        return result;
    }

    private static Sprite[] LoadMagicFrames(string name, float ppu)
    {
        Sprite[] cached;
        if (spriteCache.TryGetValue(name, out cached))
        {
            _fxBudget.Touch(name);
            return cached;
        }

        Sprite[] result = null;
        int id;
        // Official 1.5.8 dereferences name unconditionally: both frozen exports test the name
        // pointer right after the spriteCache lookup and branch to the non-returning helper
        // (arm64 FUN_00177a74, x86_64 FUN_0016c1b0), so a null name throws instead of falling
        // through to the null cache write.
        if (name.StartsWith("magic") &&
            int.TryParse(name.Substring(5), out id))
        {
            byte[] data = ReadRes(string.Format(
                "magic/magic/magic{0:D4}.gso",
                id));
            if (data != null)
            {
                DimgDecoder.Anim animation = DimgDecoder.DecodeAnim(data);
                attribCache[name] = animation.ok ? animation.attrib : 1;
                if (animation.ok && animation.f != null && animation.f.Length > 0)
                {
                    int directions = Math.Max(animation.dirs, 1);
                    int count = animation.frames / directions;
                    count = count > 0
                        ? Math.Min(count, animation.f.Length)
                        : animation.f.Length;
                    List<Sprite> frames = new List<Sprite>();
                    for (int index = 0; index < count; index++)
                    {
                        DimgDecoder.FrameTex frame = animation.f[index];
                        if (frame.tex)
                        {
                            frames.Add(MakeSprite(
                                frame.tex,
                                animation.boundCx,
                                animation.boundCy,
                                frame.nLeft,
                                frame.nTop,
                                animation.scale));
                        }
                    }

                    if (frames.Count > 0)
                    {
                        result = frames.ToArray();
                    }
                    DropUnusedFrames(animation, 0, count);
                }

                if (result == null)
                {
                    DimgDecoder.Result single = DimgDecoder.Decode(data);
                    if (single.ok && single.tex)
                    {
                        result = new[]
                        {
                            MakeSprite(
                                single.tex,
                                single.boundCx,
                                single.boundCy,
                                single.nLeft,
                                single.nTop,
                                single.scale)
                        };
                    }
                }
            }
        }

        spriteCache[name] = result;
        NoteFx(name, result);
        return result;
    }

}

}
