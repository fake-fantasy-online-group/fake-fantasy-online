using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private PlayerAvatar avatar;
    public PlayerAvatar Doll => avatar;
    private bool dead;
    public bool IsDead => dead;
    private int faceOct = 2;
    public int FaceOct => faceOct;

    public void AimFace(Vector3 worldPos) => FaceTowards(worldPos);

    private void FaceTowards(Vector3 worldPos)
    {
        float dx = worldPos.x - transform.position.x;
        float dy = transform.position.y - worldPos.y;
        if (Mathf.Abs(dx) < 0.0001f && Mathf.Abs(dy) < 0.0001f) return;
        faceOct = (Mathf.RoundToInt(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg / 45f) + 8) % 8;
        curDir = dirByOct[faceOct];
        flip = flipByOct[faceOct];
    }

    private void PlayAttackPose(float swingSec = 0f)
    {
        if (avatar == null) return;
        int variant;
        switch (CharData.atkVariant)
        {
            case 1: variant = 0; break;
            case 2: case 4: variant = 1; break;
            default: variant = 2; break;
        }
        int fpd;
        PlayerAvatar.Pose[,] frames = avatar.AttackPose(variant, out fpd);
        int count = Mathf.Max(1, fpd);
        float seconds = swingSec > 0f ? swingSec / count : 0.06f;
        StartActPose(frames, count, seconds, false, swingSec > 0f);
    }

    public void PlayChant(float holdSec)
    {
        sitting = false;
        if (CharData.riding || avatar == null) return;
        float now = Time.time;
        holdSec = Mathf.Max(0.1f, holdSec);
        chantUntil = now + holdSec;
        int action;
        switch (CharData.atkVariant)
        {
            case 1: action = 8; break;
            case 2: case 4: action = 9; break;
            default: action = 10; break;
        }
        PlayerAvatar.Pose[] poses = avatar.PoseAt(action);
        if (poses == null || ReferenceEquals(poses, avatar.stand)) return;
        if (chantFrames == null) chantFrames = new PlayerAvatar.Pose[DIRS, 1];
        for (int d = 0; d < DIRS; d++) chantFrames[d, 0] = poses[d];
        actAction = 1;
        actFrames = chantFrames;
        actFpd = 1;
        actElapsed = 0f;
        actDur = holdSec;
        actIsAtkSwing = false;
    }

    public void PlayCast(bool physical, Vector3? targetWorldPos = null)
    {
        sitting = false;
        if (CharData.riding) return;
        if (targetWorldPos.HasValue) FaceTowards(targetWorldPos.Value);
        if (physical)
        {
            PlayAttackPose(0f);
            return;
        }
        if (avatar == null) return;
        int fpd;
        PlayerAvatar.Pose[,] frames = avatar.MagicPose(out fpd);
        StartActPose(frames, fpd, 0.07f);
    }

    public void PlayHurt()
    {
        if (dead) return;
        sitting = false;
        lastHurtT = Time.time;
        if (actAction == 1 && actIsAtkSwing) return;
        StopChant();
        if (avatar == null) return;
        int fpd;
        PlayerAvatar.Pose[,] frames = avatar.PoseFrames(11, out fpd);
        StartActPose(frames, fpd, 0.1f);
    }

    internal void BindAvatarForIndependentRuntime(PlayerAvatar value)
    {
        avatar = value;
        DIRS = 5;
        FPD = 8;
        standFrames = value.stand;
        walkFrames = value.walk;
        sitFrames = value.sit;
        rideFrames = value.ride;
    }
    internal void PlayAttackPoseForIndependentRuntime(float swingSec) => PlayAttackPose(swingSec);
    internal PlayerAvatar.Pose[,] ActionFramesForIndependentRuntime => actFrames;
    internal void BindFacingForIndependentRuntime(ReferenceWorldPlayerFacing value)
    {
        faceOct = value.Octant;
        curDir = value.StoredDirection;
        flip = value.FlipX;
    }
    internal ReferenceWorldPlayerFacing FacingForIndependentRuntime =>
        new ReferenceWorldPlayerFacing(faceOct, curDir, flip, OctName[faceOct]);
}

}
