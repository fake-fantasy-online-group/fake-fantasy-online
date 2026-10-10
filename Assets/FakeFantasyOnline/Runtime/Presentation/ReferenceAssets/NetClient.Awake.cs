using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official 060006b8, through the singleton/lifetime prefix only. The existing
    // ReferenceCharacterSkillUsePort AddComponent is the actual Unity caller.
    // Keep both Unity liveness comparisons and destroy the duplicate GameObject,
    // not just this component, before publishing or retaining the accepted owner.
    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        // The native suffix starts ResolveServerAddress and then CheckVersion.
        // The former retains its existing bound host launch; the latter cannot
        // be activated until its private manifest configuration is established.
        // This prefix does not claim the whole original Awake launch sequence.
    }
}
}
