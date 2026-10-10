using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class EffectPlayer
{
    // Official 1.5.8 token 060002be.
    private static Sprite[][] LoadMagicFramesAllDirs(string name, out bool multiDir)
    {
        multiDir = false;
        var result = new Sprite[5][];
        if (!name.StartsWith("magic") || !int.TryParse(name.Substring(5), out int id))
        {
            Sprite[] single = LoadMagicFrames(name, 0f);
            for (int i = 0; i < 5; i++) result[i] = single;
            return result;
        }

        if (dirsCache.TryGetValue(name, out int knownDirections))
        {
            multiDir = knownDirections > 1;
            bool complete = true;
            for (int i = 0; i < 5; i++)
            {
                string key = multiDir ? name + "@d" + Math.Min(i, knownDirections - 1) : name + "@d";
                if (!spriteCache.TryGetValue(key, out Sprite[] cached))
                {
                    complete = false;
                    break;
                }
                _fxBudget.Touch(key);
                result[i] = cached;
            }
            if (complete) return result;
        }

        byte[] data = ReadRes(string.Format("magic/magic/magic{0:D4}.gso", id));
        DimgDecoder.Anim animation = data == null ? default : DimgDecoder.DecodeAnim(data);
        if (data == null || !animation.ok || animation.f == null || animation.f.Length == 0)
        {
            spriteCache[name + "@d"] = null;
            dirsCache[name] = 1;
            return result;
        }

        attribCache[name] = animation.attrib;
        int directions = Math.Max(animation.dirs, 1);
        dirsCache[name] = directions;
        int perDirection = Math.Max(animation.frames / directions, 1);
        multiDir = animation.dirs > 1;
        int buildDirections = multiDir ? Math.Min(directions, 5) : 1;
        for (int direction = 0; direction < buildDirections; direction++)
        {
            string key = multiDir ? name + "@d" + direction : name + "@d";
            int start = direction * perDirection;
            if (!spriteCache.TryGetValue(key, out Sprite[] cached) || cached == null)
            {
                var sprites = new List<Sprite>();
                for (int i = start; i < start + perDirection && i < animation.f.Length; i++)
                {
                    DimgDecoder.FrameTex frame = animation.f[i];
                    if (frame.tex != null)
                        sprites.Add(MakeSprite(frame.tex, animation.boundCx, animation.boundCy,
                            frame.nLeft, frame.nTop, animation.scale));
                }
                cached = sprites.Count > 0 ? sprites.ToArray() : null;
                spriteCache[key] = cached;
                NoteFx(key, cached);
            }
            else
            {
                _fxBudget.Touch(key);
                for (int i = start; i < start + perDirection && i < animation.f.Length; i++)
                {
                    if (animation.f[i].tex != null)
                    {
                        UnityEngine.Object.Destroy(animation.f[i].tex);
                        animation.f[i].tex = null;
                    }
                }
            }
        }
        DropUnusedFrames(animation, 0, buildDirections * perDirection);
        for (int i = 0; i < 5; i++)
        {
            string key = multiDir ? name + "@d" + Math.Min(i, directions - 1) : name + "@d";
            spriteCache.TryGetValue(key, out result[i]);
        }
        return result;
    }
}

}
