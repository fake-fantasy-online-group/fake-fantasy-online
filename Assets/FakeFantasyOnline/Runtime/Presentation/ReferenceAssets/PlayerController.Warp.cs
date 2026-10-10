using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official PlayerController.Warp.
/// </summary>
/// <remarks>
/// Verified against 0x060009ed (arm64 0x2a9e30; x86_64 label base 0x2ba7f0 with the hot body
/// reached by the MovePath tail call at 0x250ae2, i.e. 0x2ba910, and the two architectures agree
/// instruction by instruction). The official body clamps the incoming world position into the map
/// rectangle first - worldX (+0xe4) from x against worldW (+0xd0) and worldY (+0xe8) from y
/// against worldH (+0xd4), with a negative value collapsing to 0 - then clears the navigation
/// state in this order: path (+0x80) with an inlined List&lt;Vector2&gt;.Clear (_size = 0 and
/// _version incremented), pathIdx (+0x88), pendHops (+0xa0), moveToTarget (+0x190) and followMode
/// (+0x1c0). It finishes with transform.position
/// set from the rounded projection (Mathf.Round(worldX - worldW * 0.5f) / ppu,
/// Mathf.Round(worldH * 0.5f - worldY) / ppu, 0), which is the same projection the recovered
/// ordinary movement step already publishes through
/// ProjectMovementForIndependentRuntime (PlayerController.Displacement.cs).
/// The original throws when path or the transform is null; both are non-null for a live
/// controller, and the plain field writes keep that behavior without extra guards.
/// No speed, facing, animation or network state is touched here.
/// </remarks>
public partial class PlayerController
{
    public void Warp(float x, float y)
    {
        worldX = Mathf.Clamp(x, 0f, worldW);
        worldY = Mathf.Clamp(y, 0f, worldH);
        path.Clear();
        pathIdx = 0;
        pendHops = 0;
        moveToTarget = false;
        followMode = false;
        ProjectMovementForIndependentRuntime();
    }
}

}
