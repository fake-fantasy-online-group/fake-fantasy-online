using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    // The native DH_FY_1701 gate used by ordinary maps: 219x282, 15 frames,
    // authored ground anchor (109,246). ResLoader owns the unchanged source art.
    private const int RiftPortalImageID = 0x013006a5;
    private const int RiftPortalAnchorX = 109;
    private GameObject riftScenePortal;
    private Transform riftPortalRoot, riftObservedRoot;
    private ResLoader riftPortalResources;
    private EndgameRiftView riftObservedView, riftBlockedView;
    private string riftPortalRun;
    private long riftPortalGeneration = -1;
    private int riftPortalFloor;
    private float riftPortalX, riftPortalY;

    private void ReleaseRiftScenePortal()
    {
        if (riftScenePortal != null)
        {
            // Unity destruction is deferred; retire it immediately on a scene edge.
            riftScenePortal.SetActive(false);
            Destroy(riftScenePortal);
        }
        riftScenePortal = null; riftPortalRoot = null; riftPortalResources = null;
    }

    private void ClearRiftScenePortal()
    {
        ReleaseRiftScenePortal();
        riftObservedRoot = null; riftObservedView = riftBlockedView = null;
        riftPortalRun = null; riftPortalGeneration = -1; riftPortalFloor = 0;
    }

    private void OnDisable() => ClearRiftScenePortal();

    private void SyncRiftScenePortal()
    {
        if (Current != this) { ClearRiftScenePortal(); return; }
        GameBootstrap world = GameBootstrap.I;
        EndgameRiftView view = State?.Rift;
        Transform root = world == null ? null : world.WorldRootForMapConstructionForIndependentRuntime;
        if (world == null || root == null || world.Switching)
        {
            // The old floor's response must never paint a gate into a newly built
            // root. Its map-ready response supplies a fresh authoritative view.
            if (!ReferenceEquals(riftObservedRoot, null)) riftBlockedView = view;
            ReleaseRiftScenePortal();
            return;
        }
        if (!ReferenceEquals(riftObservedRoot, null) && riftObservedRoot != root && ReferenceEquals(view, riftObservedView))
            riftBlockedView = view;
        riftObservedRoot = root; riftObservedView = view;
        if (!TryGetRiftPortalFromWorld(out string run, out int floor, out long epoch, out float x, out float y) ||
            ReferenceEquals(view, riftBlockedView) || ResLoader.I == null || !(GameBootstrap.CurPpu > 0))
        { ReleaseRiftScenePortal(); return; }
        if (riftScenePortal != null && riftPortalRoot == root && riftPortalResources == ResLoader.I &&
            riftPortalRun == run && riftPortalFloor == floor && riftPortalGeneration == epoch &&
            riftPortalX == x && riftPortalY == y) return;
        ReleaseRiftScenePortal();
        ResLoader loader = ResLoader.I;
        ResLoader.ObjSprite image = loader.LoadObj(RiftPortalImageID, 219, 282);
        if (image.tex == null) return;
        float ppu = GameBootstrap.CurPpu, width = GameBootstrap.CurWorldW, height = GameBootstrap.CurWorldH;
        float sourceX = x - RiftPortalAnchorX, sourceY = y - image.clipBy;
        Sprite sprite = world.GetObjectSpriteForIndependentRuntime(RiftPortalImageID, image, ppu);
        if (sprite == null) return;
        riftScenePortal = new GameObject("RiftNextFloorPortal");
        riftScenePortal.transform.parent = root;
        SpriteRenderer renderer = riftScenePortal.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = GameBootstrap.IsoYObjMat();
        renderer.sortingOrder = (int)y;
        riftScenePortal.transform.position = RiftPortalFramePosition(sourceX, sourceY,
            image.nLeft, image.nTop, width, height, ppu);
        if (image.frames > 1)
        {
            FrameAnimator animator = riftScenePortal.AddComponent<FrameAnimator>();
            animator.frameMs = 200f; animator.loop = true;
            animator.loader = () =>
            {
                ResLoader.ObjAnim animation = loader.LoadObjAnim(RiftPortalImageID, ppu);
                if (animation.frames == null) return (null, null);
                Vector3[] positions = new Vector3[animation.frames.Length];
                for (int i = 0; i < positions.Length; i++)
                    positions[i] = RiftPortalFramePosition(sourceX, sourceY,
                        animation.nLeft[i], animation.nTop[i], width, height, ppu);
                return (animation.frames, positions);
            };
        }
        riftPortalRoot = root; riftPortalResources = loader; riftPortalRun = run;
        riftPortalFloor = floor; riftPortalGeneration = epoch; riftPortalX = x; riftPortalY = y;
    }

    private static Vector3 RiftPortalFramePosition(float sourceX, float sourceY,
        int left, int top, float width, float height, float ppu) =>
        new Vector3((sourceX + left - width * .5f) / ppu,
            (height * .5f - sourceY - top) / ppu, 0);
}
}
