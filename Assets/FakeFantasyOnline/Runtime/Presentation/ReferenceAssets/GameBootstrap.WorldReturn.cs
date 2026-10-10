using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    private Action<bool, bool> titleReturnForIndependentRuntime;

    // OnNetDropped tests the live Unity Transform, not an admitted entry snapshot.
    internal bool HasWorldRootForIndependentRuntime => worldRoot != null;

    internal void BindTitleReturnForIndependentRuntime(Action<bool, bool> destination)
    {
        titleReturnForIndependentRuntime = destination ?? throw new ArgumentNullException(nameof(destination));
    }

    public void ReturnToTitle(bool characterSelection)
    {
        // ReleaseWorld clears the native-startup admission; retain the actual source
        // of this return rather than inventing an authenticated session afterward.
        bool nativeWorldReturn = HasNativeStartupWorldForIndependentRuntime;
        ReleaseWorld();
        I = this;
        titleReturnForIndependentRuntime(characterSelection, nativeWorldReturn);
    }
}
}
