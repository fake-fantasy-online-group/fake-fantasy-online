using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class NetClient
{
    // Official 1.5.8 token 0600077a; exposed internally for the receive adapter.
    internal static float DispatchMsPerFrame => Mathf.Max(2f, GameSettings.TargetFrameMs * 0.36f);
}

}
