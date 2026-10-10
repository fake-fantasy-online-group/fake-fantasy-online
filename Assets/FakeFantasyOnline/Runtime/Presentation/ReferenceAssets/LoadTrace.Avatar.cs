namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class LoadTrace
{
    private static int _avN;
    private static float _avMs;
    private static int _frAv;

    // 0x060001eb
    public static void Avatar(string who, string appear, float ms, bool ok)
    {
        if (!On) return;
        _avN = unchecked(_avN + 1);
        _avMs += ms;
        _avTop.Add((who, appear, ms, ok));
        Line(string.Format("纸娃娃 {0} {1:F0}ms {2} | {3}", who, ms, ok ? "ok" : "失败", appear));
    }

    // 0x060001f3
    public static void AvatarBuiltThisFrame()
    {
        if (On) _frAv = unchecked(_frAv + 1);
    }
}

}
