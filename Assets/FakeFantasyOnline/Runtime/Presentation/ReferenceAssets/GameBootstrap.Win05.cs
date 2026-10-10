using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameBootstrap
{
    // The native Toggle/Open root is not a child of worldRoot. Ordinary TransitionTo
    // destroys only that map root, so later hosts borrow the same Win05 state and caches.
    // There is deliberately no DDOL or bespoke final reset here: the already-recovered
    // TeardownWorldUi scene-root purge (060003f3/060003f7) owns final destruction.
    internal static Win05 AcquireWin05ForIndependentRuntime()
    {
        if (Win05.I == null) Win05.I = new GameObject("Win05").AddComponent<Win05>();
        return Win05.I;
    }
}
}
