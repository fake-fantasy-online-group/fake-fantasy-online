using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameBootstrap
{
    // BuildWorld060003d4 x86 4335af..43363c. This is an ordinary scene root:
    // neither a child of the replaceable map nor a DontDestroyOnLoad object.
    internal static void EnsureDialogUIForIndependentRuntime()
    {
        if (DialogUI.I == null)
            new GameObject("DialogUI").AddComponent<DialogUI>();
    }
}
}
