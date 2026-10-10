namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class LoadTrace
{
    private static float _gridMainMs;

    // 060001d8 is a literal-false getter, with no backing field or settings switch.
    public static bool AutoOnMapSwitch => false;

    // 060001e8.
    public static void Phase(string name, float ms)
    {
        if (!On) return;

        _phases.Add((name, ms));
        Line(string.Format("相位 {0} {1:F0}ms", name, ms));
    }

    // 060001ec. Cache hits retain their row and contribute to the same main-thread total.
    public static void Grid(int actId, int model, string name, string mode,
        long fileBytes, int scale, long actualBytes, float mainMs)
    {
        if (!On) return;

        _gridRows.Add((actId, model, name ?? "", mode, fileBytes, scale, actualBytes,
            mainMs, -1f, 0, 0L));
        if (mode != "缓存命中")
        {
            Line(string.Format(
                "网格 act{0} model={1} {2} [{3}] 文件{4} scale={5} 实际{6} 主线程{7:F0}ms",
                actId, model, name, mode, Kb(fileBytes), scale,
                string.Format("{0:F1}MB", actualBytes / 1048576f), mainMs));
        }

        _gridMainMs = mainMs + _gridMainMs;
    }
}

}
