using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static class TeleportData
{
    [Serializable]
    public class Dest
    {
        public string name;
        public int map;
        public float x;
        public float y;
        public int price;
    }
}

}
