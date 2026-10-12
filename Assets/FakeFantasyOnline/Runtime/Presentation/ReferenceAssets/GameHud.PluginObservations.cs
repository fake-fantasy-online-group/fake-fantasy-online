using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    // Read-only counterpart to SkillCdFrac: observation must not prune native cooldown state.
    internal static double ObserveSkillCooldownSecondsForPlugins(int skill)
    {
        if (!skillCd.TryGetValue(skill, out Vector2 state)) return 0;
        return Math.Max(0, state.y - (Time.time - state.x));
    }
}
}
