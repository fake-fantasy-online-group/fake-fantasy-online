namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public partial class PlayerController
    {
        // Official 0x06000a1c.
        public float HeadU => avatar != null && avatar.headPx > 0 ? avatar.headPx / ppu : 1.4f;

        // Official 0x06000a18. A missing avatar is not an error: the body returns literal zero.
        public int SitNameTopPx => avatar != null ? avatar.sitTopPx : 0;
    }
}
