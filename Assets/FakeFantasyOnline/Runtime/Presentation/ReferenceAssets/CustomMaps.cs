namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official CustomMaps source, 1.5.8 tokens 0x060001cf Real / 0x060001d0 IsCustom /
/// 0x060001d1 SrcOf (arm64 0x3590d4 / 0x3590f8 / 0x359130, x86_64 0x36b190 / 0x36b1b0 /
/// 0x36b1e0). The 060001BB..060001BD range belongs to the historical 1.3.4 baseline and must
/// not be used to name these members.
/// </summary>
public static class CustomMaps
{
    public const int Base = 90000;
    public const int Max = 99999;
    public const int TaoyuanArena = 90037;

    private static int Real(int mapId)
    {
        return mapId >= 100000 ? mapId / 1000 : mapId;
    }

    public static bool IsCustom(int mapId)
    {
        int real = Real(mapId);
        return real >= Base && real <= Max;
    }

    // Official 1.6.0 0x060001d6 (ARM64 0x3e1988): decode an instance map ID first.
    public static bool IsDuelArena(int mapId)
    {
        return Real(mapId) == 90120;
    }

    public static int SrcOf(int mapId)
    {
        int real = Real(mapId);
        return real >= Base && real <= Max ? real - Base : real;
    }
}

}
