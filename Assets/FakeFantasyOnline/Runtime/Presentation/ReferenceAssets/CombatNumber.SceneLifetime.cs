using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatNumber
{
    private static readonly HashSet<CombatNumber> activeExperience = new HashSet<CombatNumber>();

    private void TrackExperienceScene()
    {
        if (type == 4) activeExperience.Add(this);
    }

    private void ForgetExperienceScene() => activeExperience.Remove(this);

    // Active popups are pooled scene-independent GameObjects. Explicitly retire the
    // departing scene's already-created experience visuals before a new world exists.
    // This changes no reward/accounting state and does not touch other popup kinds.
    internal static void ReleaseExperienceForSceneTransition()
    {
        while (activeExperience.Count > 0)
        {
            CombatNumber number;
            using (var rows = activeExperience.GetEnumerator())
            {
                rows.MoveNext();
                number = rows.Current;
            }
            activeExperience.Remove(number);
            if (number) number.ReturnToPool();
        }
        lastSpawnT = -9f;
        spawnStack = 0;
    }
}

}
