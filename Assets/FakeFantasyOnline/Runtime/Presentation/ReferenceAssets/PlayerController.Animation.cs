using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private PlayerAvatar.Pose[] standFrames;
    private PlayerAvatar.Pose[] sitFrames;
    private PlayerAvatar.Pose[] rideFrames;
    private int FPD = 8;
    private int curFrame;
    private float animDist;
    private float distPerFrame = 18f;

    internal void UpdateAnimationForIndependentRuntime(float dx, float dy, bool moving, Color32 tint)
    {
        dollTint = tint;
        UpdateAnim(dx, dy, moving);
        UpdateBuffAura();
    }

    internal void UpdateRidingForIndependentRuntime(float dx, float dy, bool moving, Color32 tint)
    {
        dollTint = tint;
        if (!dead && CharData.rideAnchorId != 0)
        {
            FollowRideAnchor();
            path.Clear();
            moveToTarget = false;
            followMode = false;
            followId = 0;
            if (sr) sr.sortingOrder = GroupBase();
            IsMoving = false;
            UpdateAnim(0f, 0f, false);
            UpdateBuffAura();
            return;
        }
        UpdateAnim(dx, dy, moving);
        UpdateBuffAura();
    }

    internal int AnimationFrameForIndependentRuntime => curFrame;
    internal float AnimationDistanceForIndependentRuntime => animDist;

    internal int GroupBaseForIndependentRuntime => GroupBase();

    private int GroupBase()
    {
        if (sr && sr.sharedMaterial && sr.sharedMaterial.shader &&
            sr.sharedMaterial.shader.name == "Custom/IsoDepthY") return 0;
        return unchecked(Mathf.Clamp(unchecked((int)worldY), 0, 30000) + 1);
    }

    private void UpdateAnim(float dx, float dy, bool moving)
    {
        if (walkFrames == null) return;
        if (dead)
        {
            HideMount();
            int dir = Mathf.Clamp(curDir, 0, DIRS - 1);
            if (dieFrames == null) return;
            dieElapsed += Time.deltaTime;
            int frame = Mathf.Clamp(unchecked((int)(dieElapsed / 0.09f)), 0, dieFpd - 1);
            PlayerAvatar.Pose pf = dieFrames[dir, frame];
            if (pf != null) ApplyPose(pf, flip, GroupBase(), dollTint);
            return;
        }
        if (CharData.riding && CharData.petModel <= 0) CharData.riding = false;
        if (CharData.riding && standFrames != null)
        {
            if (moving && (dx != 0f || dy != 0f))
            {
                faceOct = unchecked(Mathf.RoundToInt(Mathf.Atan2(dy, dx) * 57.29578f / 45f) + 8) % 8;
                curDir = dirByOct[faceOct];
                flip = flipByOct[faceOct];
            }
            animDist = 0f;
            CharData.rideDir = curDir;
            CharData.rideFlip = flip;
            ComposeMount(moving && (dx != 0f || dy != 0f), faceOct);
            int action = NpcActions.RideActionIndex(CharData.mountModel);
            PlayerAvatar.Pose[] poses = (avatar != null ? avatar.PoseAt(action) : standFrames) ?? standFrames;
            int dir = Mathf.Clamp(curDir, 0, poses.Length - 1);
            if (dollRoot) dollRoot.localPosition = new Vector3(CharData.mountSaddleX / ppu, -CharData.mountSaddleY / ppu, 0f);
            if (actorFx) actorFx.mountOffsetU = new Vector2(CharData.mountSaddleX / ppu, -CharData.mountSaddleY / ppu);
            PlayerAvatar.Pose pf = ActPoseNow(curDir) ?? poses[dir];
            ApplyPose(pf, flip, unchecked(riderSoBase + 1), dollTint);
            return;
        }
        HideMount();
        if (moving && !sitting && (dx != 0f || dy != 0f))
        {
            int oct = unchecked(Mathf.RoundToInt(Mathf.Atan2(dy, dx) * 57.29578f / 45f) + 8) % 8;
            curDir = dirByOct[oct];
            flip = flipByOct[oct];
        }
        if (actAction >= 0 && sitting)
        {
            actAction = -1;
            actFrames = null;
            actLoop = false;
        }
        else
        {
            PlayerAvatar.Pose action = ActPoseNow(curDir);
            if (action != null) { ApplyPose(action, flip, GroupBase(), dollTint); return; }
        }
        if (sitting)
        {
            animDist = 0f;
            PlayerAvatar.Pose pf = curDir < DIRS && sitFrames != null ? sitFrames[curDir] : null;
            if (pf == null) pf = avatar != null ? avatar.StandAt(curDir, Time.time)
                : curDir < DIRS && standFrames != null ? standFrames[curDir] : null;
            if (pf != null) ApplyPose(pf, flip, GroupBase(), dollTint);
            return;
        }
        if (!moving || (dx == 0f && dy == 0f))
        {
            animDist = 0f;
            PlayerAvatar.Pose pf = avatar != null ? avatar.StandAt(curDir, Time.time)
                : standFrames != null && curDir < DIRS ? standFrames[curDir] : null;
            if (pf != null) ApplyPose(pf, flip, GroupBase(), dollTint);
            return;
        }
        animDist += Mathf.Sqrt(dx * dx + dy * dy);
        curFrame = unchecked((int)(animDist / distPerFrame)) % FPD;
        if (curDir < DIRS && walkFrames[curDir, curFrame] != null)
            ApplyPose(walkFrames[curDir, curFrame], flip, GroupBase(), dollTint);
    }
}

internal static partial class CharData
{
    internal static int petModel;
}

}
