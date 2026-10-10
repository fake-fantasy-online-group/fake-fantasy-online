using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    // 06000a21: preserve allocation, population and publication order in one partial.
    private static readonly Dictionary<string, string> AuraEffOverride = new Dictionary<string, string>
    {
        ["春风"] = "补血",
        ["补充生命"] = "补血",
        ["补充法力"] = "补魔法",
        ["魔镜术"] = "魔法反弹",
        ["瞬发"] = "鹰眼",
        ["燃烧"] = "燃烧1",
        ["仙佑符"] = "TFX074_40",
        ["灵聚之术符"] = "TFX7011_40",
        ["神返咒符"] = "TFX7012_40",
        ["砺金之咒符"] = "TFX028_40",
        ["坚甲之术符"] = "TFX028_40",
        ["灵护之术符"] = "TFX028_40",
        ["御法之术符"] = "TFX028_40",
        ["裂刃枪法符"] = "TFX068_40",
        ["野性之赐符"] = "TFX7015_40",
        ["心与剑随符"] = "TFX056_40",
        ["凌虚步法符"] = "TFX7018_40",
        ["灵印法符"] = "TFX1082_30",
    };

    public static bool DbgFx = false;
    private static readonly int[] DX8 = { 1, -1, 0, 0, 1, 1, -1, -1 };
    private static readonly int[] DY8 = { 0, 0, 1, -1, 1, -1, 1, -1 };
    private static readonly Collider2D[] _overlapBuf = new Collider2D[64];
    private static readonly HashSet<int> _mountDiagWarned = new HashSet<int>();
    private static readonly HashSet<int> _mountPixWarned = new HashSet<int>();
    private static float suppressWorldClickUntil = -1f;
    private static int followId = 0;
    private static readonly int[] dirByOct = { 1, 0, 4, 0, 1, 2, 3, 2 };
    private static readonly bool[] flipByOct = { true, true, false, false, false, false, false, true };
    private static readonly string[] OctName = { "东", "东南", "南", "西南", "西", "西北", "北", "东北" };
}

}
