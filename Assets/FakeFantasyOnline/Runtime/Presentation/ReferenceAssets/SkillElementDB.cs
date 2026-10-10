using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered skill-element lookup used by charge effects and targeting-outline colors.
/// </summary>
public static partial class SkillElementDB
{
    // Official static layout is ELEM (private static initonly string[]) at statics +0x0 and
    // map at +0x8; the six entries are written by SkillElementDB..cctor (0x06000cfd) in the
    // order 冰/火/电/地/暗/风 (StringLiteral 17097, 17759, 17810, 17240, 17652, 18259).
    private static readonly string[] ELEM =
    {
        "冰",
        "火",
        "电",
        "地",
        "暗",
        "风",
    };

    private static Dictionary<int, int> map;

    private static void Ensure()
    {
        if (map != null)
        {
            return;
        }

        map = new Dictionary<int, int>();
        string text = GameTables.LoadText("skillelement");
        JFile file = text == null ? null : JsonUtility.FromJson<JFile>(text);
        if (file == null || file.elems == null)
        {
            return;
        }

        for (int index = 0; index < file.elems.Length; index++)
        {
            // Official arm64/x86_64 dereference every row before ContainsKey: a null row or a
            // null map reaches LAB_0031f150 / FUN_00177a74 on arm64 and FUN_0016c1b0 on
            // x86_64, neither of which returns, so a null row throws instead of being skipped.
            // The first duplicate id still wins because Add only runs when ContainsKey is false.
            JElem row = file.elems[index];
            if (!map.ContainsKey(row.id))
            {
                map.Add(row.id, row.e);
            }
        }
    }

    public static string ChargeEff(int skillId)
    {
        Ensure();
        int element;
        return map != null && map.TryGetValue(skillId, out element) &&
               element >= 0 && element < ELEM.Length
            ? ELEM[element] + "系聚气"
            : null;
    }

    public static int Element(int skillId)
    {
        Ensure();
        int element;
        return map.TryGetValue(skillId, out element) ? element : -1;
    }
}

}
