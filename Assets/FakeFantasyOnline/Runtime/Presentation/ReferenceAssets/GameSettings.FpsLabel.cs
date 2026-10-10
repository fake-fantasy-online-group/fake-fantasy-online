namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class GameSettings
{
    // Official 1.5.8 token 06000e15. The native body is a pure constant selection:
    // mode 0 yields the 60 Hz label, mode 1 the 120+ label, and every other mode the
    // follow-refresh-rate label.
    public static string FpsLabel(int mode)
    {
        return mode == 0 ? "标准 60" : mode == 1 ? "流畅 120+" : "极致 跟随刷新率";
    }
}

}
