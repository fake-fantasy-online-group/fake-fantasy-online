using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    // Extracted original PlaceMapObjects source-map admission, not new map policy.
    internal HashSet<int> CreateObjectBlockListForIndependentRuntime()
    {
        if (mapName == "qw0007")
            return new HashSet<int> { 0x032d01af, 0x032d01ac, 0x261906a6 };
        return null;
    }

    internal int LibraryObjectOrderForIndependentRuntime(int id, int y, int width, int height, float worldHeight)
    {
        int kind = (id >> 16) & 0xff;
        if (kind != 0x33 && kind != 0x2c) return int.MinValue;
        ResLoader.ObjSprite source = ResLoader.I != null ? ResLoader.I.LoadObj(id, width, height) : default;
        int ground = source.tex != null ? source.clipBy : 0;
        int order = Mathf.RoundToInt(((float)(y + ground) * 1999f) / Mathf.Max(worldHeight, 1f));
        return Mathf.Clamp(order, 0, 1999) - 6000;
    }

    internal bool SkipLibraryObjectForIndependentRuntime(int id, int x, int y) =>
        mapName == "qw0037" && id == 0x04090065 &&
        ((x == 0x17fb && y == 0xa1) || (x == 0x15ca && y == -0x9e));
}

}
