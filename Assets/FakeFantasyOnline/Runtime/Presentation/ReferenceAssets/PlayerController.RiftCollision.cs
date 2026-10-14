using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class PlayerController
{
    private EndgameClientState riftCollisionState;
    private EndgameRiftHazardState riftCollision;
    private EndgameRiftView riftCollisionObservedView, riftCollisionBlockedView;
    private Transform riftCollisionRoot;
    private long riftCollisionVersion = -1, riftCollisionGeneration = -1;
    private int riftCollisionMap;
    private double riftCollisionNow;
    private float riftPathGoalX, riftPathGoalY;

    // Scope the overlay to the actual native world. A previous floor's view cannot
    // attach to a new root even when two floors reuse the same map template.
    private bool RefreshRiftCollision()
    {
        var state = EndgamePanel.WorldState;
        var world = GameBootstrap.I;
        var root = world == null ? null : world.WorldRootForMapConstructionForIndependentRuntime;
        var view = state?.Rift;
        bool reset = !ReferenceEquals(state, riftCollisionState) || state != null && state.Generation != riftCollisionGeneration;
        if (reset)
        {
            riftCollisionState = state; riftCollisionGeneration = state == null ? -1 : state.Generation;
            riftCollisionRoot = null; riftCollisionObservedView = riftCollisionBlockedView = null;
        }
        if (world == null || root == null || world.Switching)
        {
            if (!ReferenceEquals(riftCollisionRoot, null)) riftCollisionBlockedView = view;
        }
        else
        {
            if (!ReferenceEquals(riftCollisionRoot, null) && riftCollisionRoot != root && ReferenceEquals(view, riftCollisionObservedView))
                riftCollisionBlockedView = view;
            riftCollisionRoot = root; riftCollisionObservedView = view;
        }
        riftCollisionNow = EndgameClientState.MonotonicMS;
        EndgameRiftHazardState next = world != null && root != null && !world.Switching && state != null && view != null &&
            !ReferenceEquals(view, riftCollisionBlockedView) && ReferenceEquals(state.RiftHazards.Snapshot, view) &&
            view.MapID == GameBootstrap.CurMapId && state.CanUseRiftHazards(GameBootstrap.CurMapId, root) ? state.RiftHazards : null;
        if (next != null) next.Tick(riftCollisionNow);
        long version = next == null ? -1 : next.WallVersion;
        bool changed = reset || !ReferenceEquals(next, riftCollision) || version != riftCollisionVersion || riftCollisionMap != GameBootstrap.CurMapId;
        riftCollision = next; riftCollisionVersion = version; riftCollisionMap = GameBootstrap.CurMapId;
        return changed;
    }
    private bool RiftPathWalkable(double fromX, double fromY, double toX, double toY) =>
        riftCollision == null || !riftCollision.Blocks(riftCollisionMap, fromX, fromY, toX, toY, riftCollisionNow);
    private bool RiftCellWalkable(int x, int y) => RiftPathWalkable((x + 0.5) * maskCW, (y + 0.5) * maskCH,
        (x + 0.5) * maskCW, (y + 0.5) * maskCH);
    private bool RiftCellEdgeWalkable(int x, int y, int x2, int y2) => RiftPathWalkable((x + 0.5) * maskCW, (y + 0.5) * maskCH,
        (x2 + 0.5) * maskCW, (y2 + 0.5) * maskCH);
    private bool MovementSegmentWalkable(float x, float y) => IsWalkable(x, y) && RiftPathWalkable(worldX, worldY, x, y);
    private bool ReplanRiftPathIfChanged()
    {
        bool changed = RefreshRiftCollision();
        if (!changed || !moveToTarget) return false;
        // A wall set/expiry invalidates the whole cached path, including pending hops.
        long navigationOwner = pluginNavigationRevision;
        SetPathTo(riftPathGoalX, riftPathGoalY);
        // This is the same movement intent. Do not cancel a pending addon action
        // or detach a combat approach merely because the server changed walls.
        pluginNavigationRevision = navigationOwner;
        return true;
    }
}
}
