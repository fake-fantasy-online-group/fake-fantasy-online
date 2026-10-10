namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class WindowCenter
{
    // Original macOS 1.5.8 body: ARM 0x3d41e0 and x86 0x3e8460.
    // This platform does not supply a maximum-client-area bound.
    public static bool TryGetMaxClient(out int w, out int h)
    {
        w = 0;
        h = 0;
        return false;
    }
}

}
