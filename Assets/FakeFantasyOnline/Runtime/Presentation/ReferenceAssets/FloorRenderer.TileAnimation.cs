using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class FloorRenderer
{
    private class AnimTil
    {
        public int frameNum;
        public float frameMs;
        public Color32[][] frames;
    }

    private readonly Dictionary<string, AnimTil> animTilCache = new Dictionary<string, AnimTil>();

    private AnimTil GetAnimTil(string name)
    {
        if (animTilCache.TryGetValue(name, out AnimTil cached)) return cached;
        AnimTil result = null;
        byte[] data = pkg.Read("tile/" + name + ".til");
        if (data != null && data.Length >= 16 && data[0] == 'E' && data[1] == 'L' && data[2] == 'I' && data[3] == 'T')
        {
            int version = I32(data, 8);
            int total = I32(data, 12);
            if (version > 0x1002)
            {
                int milliseconds = I32(data, 0x13c);
                int requestedFrames = I32(data, 0x140);
                if (requestedFrames > 1 && milliseconds > 0)
                {
                    int count = Math.Min(requestedFrames, total);
                    if (0x144 + count * 65536 <= data.Length)
                    {
                        var frames = new Color32[count][];
                        for (int i = 0; i < count; i++) frames[i] = DecodeFull(data, 0x144 + i * 65536);
                        result = new AnimTil { frameNum = count, frameMs = milliseconds, frames = frames };
                    }
                }
            }
        }
        animTilCache[name] = result;
        return result;
    }

    private string FirstAnim(int code)
    {
        if (code < 0 || code2name == null || !code2name.TryGetValue(code, out string name)) return null;
        return GetAnimTil(name) != null ? name : null;
    }

    private string AnimNameOf(int lb, int lt, int rt, int rb) =>
        FirstAnim(lb) ?? FirstAnim(lt) ?? FirstAnim(rt) ?? FirstAnim(rb);
}

}
