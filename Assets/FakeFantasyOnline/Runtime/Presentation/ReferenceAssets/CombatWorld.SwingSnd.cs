namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    // Official CombatWorld.SwingSnd (0600015e, static System.Int32
    // CombatWorld.SwingSnd(System.Int32 cType), arm64 0x247cc8 / 56 bytes,
    // x86_64 0x2554b0 / 64 bytes). Both frozen exports are the same body:
    //   if (cType - 1U < 8) return *(int32 *)(&DAT + (ulong)(cType - 1U) * 4);
    //   return cType == 0x10 ? 0x4e7 : (cType == 0x20 ? 0x4ea : 0);
    // The eight-entry table is baked into __TEXT and is read here straight out of the frozen
    // 1.5.8 Mach-O, with no 1.3.4 comparison and no listening test involved:
    //   arm64   Ghidra DAT_01126fa4 -> image base 0x10000000 -> __TEXT vm/file 0x126fa4,
    //           fat-file offset 0x2686fa4;
    //   x86_64  Ghidra DAT_01118d24 -> image base 0x10000000 -> __TEXT vm/file 0x118d24,
    //           fat-file offset 0x111cd24.
    // Both slices hold the same 32 bytes (little-endian int32, 4 bytes per element, values are
    // non-negative so the signed/unsigned reading of an element is the same):
    //   e8040000 e6040000 00000000 e9040000 00000000 e6040000 00000000 eb040000
    //   sha256 856dec99663003b267eddaa0aa0d40ca7729ca9d3f556ff9a171538e2dd9c804 (both slices)
    // i.e. cType 1 -> 1256, 2 -> 1254, 3 -> 0, 4 -> 1257, 5 -> 0, 6 -> 1254, 7 -> 0, 8 -> 1259,
    // plus 16 -> 0x4e7 = 1255 and 32 -> 0x4ea = 1258 from the body constants, and 0 for
    // everything else - including every cType below 1, because `cType - 1U` wraps.
    private static int SwingSnd(int cType)
    {
        switch (cType)
        {
            case 1: return 1256;
            case 2:
            case 6:
                return 1254;
            case 4: return 1257;
            case 8: return 1259;
            case 16: return 1255;
            case 32: return 1258;
            default: return 0;
        }
    }

    internal static int SwingSndForIndependentRuntime(int cType) => SwingSnd(cType);
}

}
