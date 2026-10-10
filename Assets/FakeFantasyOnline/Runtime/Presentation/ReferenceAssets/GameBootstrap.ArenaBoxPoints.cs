using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    // StringLiteral[17876] of the frozen 1.5.8 metadata usage table.
    private const string ArenaBoxName = "空投宝箱";

    // 0x060003d6 ArenaBoxPoints (arm64 0x42380c / 604B, x86_64 0x438b10 / 576B; both agree).
    // Clear runs first and a null list raises through the non-returning helper (arm64 FUN_00177a74
    // / x86_64 FUN_0016c1b0); then the authoritative rows are walked, a null row or an
    // IsNullOrEmpty name is skipped and a name carrying the literal above contributes (x, y).
    // The native retained-row dictionary is also the receive owner's authority.
    internal static void ArenaBoxPoints(List<Vector2> outp)
    {
        outp.Clear();
        foreach (NpcEntry row in _netNpcs.Values)
        {
            if (row == null || string.IsNullOrEmpty(row.name))
            {
                continue;
            }

            if (row.name.Contains(ArenaBoxName))
            {
                outp.Add(new Vector2(row.x, row.y));
            }
        }
    }
}

}
