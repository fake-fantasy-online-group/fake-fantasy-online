using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    private static bool _holdSkyBlack;

    // The recovered camera setup calls this at the original BuildWorld sky assignment.
    internal static void SetPendingSkyForIndependentRuntime(Camera camera, Color sky)
    {
        _pendingSky = sky;
        camera.backgroundColor = _holdSkyBlack ? Color.black : _pendingSky;
    }

    // Official 1.5.8 token 060003df.
    private static void ApplyPendingSky()
    {
        _holdSkyBlack = false;
        Camera camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindAnyObjectByType<Camera>();
        if (camera != null) camera.backgroundColor = _pendingSky;
    }
}

}
