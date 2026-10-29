using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Reviewed open-deck geometry, not a rule for every building or collision hole.
// Coordinates are in the original, untrimmed logical GSO image. Each row names
// a ten-pixel column, support span top/bottom, and its rear depth.
// Source textures, map collision, NPC positions and actor ordering stay unchanged.
internal static partial class ReferenceWalkableDeckProfile
{
    // These narrowly reviewed components meet diagonal awnings, props or rails;
    // 10px bins would either swallow foreground or leave artificial body cuts.
    internal static int ColumnWidth(int id) => id == 221053330 ? 1 : id == 53283369 ? 1 : id == 523043617 ? 1 : id == 523044617 ? 1 : id == 455936354 ? 1 : id == 674038465 ? 1 : id == 674038462 ? 1 : id == 674038464 ? 1 : id == 103613517 ? 1 : id == 36505853 ? 1 : id == 36506153 ? 1 : id == 36506954 ? 1 : id == 388829789 ? 1 : id == 19726838 ? 1 : id == 103612518 ? 1 : id == 539820934 ? 1 : id == 221053329 ? 1 : id == 221053331 ? 1 : id == 53283770 ? 1 : id == 53283771 ? 1 : id == 455934353 ? 1 : id == 455935053 ? 1 : id == 455935653 ? 1 : id == 455936053 ? 1 : 10;

    // Independently reviewed inner jambs only. The strict source signature is
    // checked before these are used; outside cliff and foreground bowls stay native.
    internal static int[] PortalJamb(int id) => id == 0x1c2d007e ? ShellRightJamb : id == 0x1c2d007f ? ShellLeftJamb : null;
    private static readonly int[] ShellRightJamb = { 356,166,368,150,382,158,396,181,409,195,420,215,422,238,422,254,414,266,407,274,396,269,382,260,373,243,367,223,358,218,358,198,352,184 };
    private static readonly int[] ShellLeftJamb = { 192,166,180,150,166,158,152,181,139,195,128,215,126,238,126,254,134,266,141,274,152,269,166,260,175,243,181,223,190,218,190,198,196,184 };

    internal const int ChangleStageId = 0x052d0191;
    private static readonly int[] ChangleStage = {
        21,0,420,400,
        22,0,430,400,
        23,0,430,390,
        24,0,440,390,
        25,0,440,380,
        26,0,440,380,
        27,0,450,380,
        28,0,420,370,
        29,0,420,370,
        30,0,420,360,
        31,0,420,360,
        32,0,430,360,
        33,0,450,350,
        34,0,450,350,
        35,0,450,350,
        36,0,440,340,
        37,0,440,330,
        38,0,430,320,
        39,0,410,320,
        40,0,400,310,
        41,0,380,310,
        42,0,360,300,
        43,0,360,300,
        44,0,360,290,
        45,0,360,290,
        46,0,360,280,
        47,0,370,280,
        48,0,360,270,
        49,0,360,270,
        50,0,330,260,
        51,0,330,250,
        52,0,330,250,
        53,0,330,250,
        54,0,330,250,
        55,0,340,250,
        56,0,340,250,
        57,0,340,250,
        58,0,330,250,
        59,0,330,250,
        60,0,320,250,
        61,0,320,250,
        62,0,310,250,
        63,0,310,250,
        64,0,310,250,
        65,0,310,250,
        66,0,310,250,
    };

    private static readonly int[] ChangleNativeClips = {
        432,433,435,437,438,440,442,444,445,447,449,450,452,454,
        455,457,459,460,462,464,465,467,469,470,472,474,476,477,
        479,481,482,484,486,487,489,491,492,494,496,497,499,501
    };

    internal static bool MatchesClips(int width, int foot, int[] clips)
    {
        if (width != 16 || foot != 432 || clips == null || clips.Length != ChangleNativeClips.Length)
            return false;
        for (int i = 0; i < clips.Length; i++) if (clips[i] != ChangleNativeClips[i]) return false;
        return true;
    }

    internal static bool TryGet(int id, string path, int width, int height, out int[] columns)
    {
        columns = null;
        if (id != ChangleStageId || width != 669 || height != 811 ||
            !string.Equals(path, "object/JZ/JZ_CL_0401.gso", StringComparison.OrdinalIgnoreCase))
            return false;
        columns = ChangleStage;
        return true;
    }
}
}
