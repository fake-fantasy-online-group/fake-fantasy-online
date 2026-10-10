using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class Win05
{
    // Original Win05.WardrobeItem (020002a3). The icon belongs to the published
    // row, including a null result; rendering and pickup borrow that exact texture.
    public struct WardrobeItem
    {
        public int id;
        public byte cat;
        public bool worn;
        public bool blocked;
        public string name;
        public string desc;
        public Texture2D icon;
    }

    private static int wardrobeCap;

    internal static void SetWardrobe(int cap, List<WardrobeItem> items)
    {
        wardrobeCap = cap;
        wardrobe.Clear();
        if (items != null) wardrobe.AddRange(items);
    }
}

}
