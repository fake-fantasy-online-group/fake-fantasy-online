namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public partial class RemotePlayer
    {
        // Official 0x06000ae0.
        public float HeadU => avatar != null && avatar.headPx > 0 ? avatar.headPx / ppu : 1.4f;

        // Official 0x06000ae1..0x06000ae3.
        public int Gender => gender;
        public int HairId => head;
        public int HairColor => hair;
    }
}
