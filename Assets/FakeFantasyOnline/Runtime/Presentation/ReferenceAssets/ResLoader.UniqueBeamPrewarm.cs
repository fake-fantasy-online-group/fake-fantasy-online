using System.Collections;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class ResLoader
{
    // Awake has completed package mounting before Unity invokes Start. Warm the selected loot
    // appearance during resource startup, not the first kill. This does not pin its budget keys;
    // late resource mounting and later eviction still recover through the normal loot retry.
    private IEnumerator Start()
    {
        yield return null; // Let initial startup settle before the one-time managed generation.
        if (I == this) EffectPlayer.PrewarmUniqueBeamC();
    }
}
}
