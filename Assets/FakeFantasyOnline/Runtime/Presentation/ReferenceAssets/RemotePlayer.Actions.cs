using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    private int atkVariant;
    private int weaponCType;
    private int atkDistPx;
    private PlayerAvatar.Appear appear;
    private PlayerAvatar.Pose[,] actFrames;
    private int actFpd;
    private float actElapsed;
    private float actDur;
    private float actFrameSec;
    private PlayerAvatar.Pose[,] dieFrames;
    private int dieFpd;
    private float dieElapsed;
    private const float DIE_FRAME_SEC = 0.09f;

    public int WeaponCType => weaponCType;
    public int AtkDistPx => atkDistPx;

    public void SetWeapon(int cType, int px)
    {
        weaponCType = cType;
        atkDistPx = px;
    }

    private static int AtkSub(int variant)
    {
        switch (variant)
        {
            case 1: return 0;
            case 2: return 1;
            case 3: return 2;
            case 4: return 1;
            default: return 2;
        }
    }

    public void PlayAttack(Vector3? facePos = null)
    {
        if (avatar == null || !avatar.ok) return;
        if (facePos.HasValue) FaceTo(facePos.Value);
        int fpd;
        PlayerAvatar.Pose[,] frames;
        if (CombatWorld.BasicAtkUsesMagicPose(weaponCType, atkDistPx))
        {
            frames = avatar.MagicPose(out fpd);
            if (frames != null)
            {
                StartAct(frames, fpd, 0.07f);
                return;
            }
        }
        frames = avatar.AttackPose(AtkSub(atkVariant), out fpd);
        if (frames != null) StartAct(frames, fpd, 0.06f);
    }

    public void PlayCast()
    {
        if (avatar == null || !avatar.ok) return;
        int fpd;
        PlayerAvatar.Pose[,] frames = avatar.MagicPose(out fpd);
        if (frames != null) StartAct(frames, fpd, 0.07f);
    }

    private void FaceTo(Vector3 worldPos)
    {
        float dx = worldPos.x - transform.position.x;
        float dy = transform.position.y - worldPos.y;
        if (Mathf.Abs(dx) < 0.0001f && Mathf.Abs(dy) < 0.0001f) return;
        int oct = (Mathf.RoundToInt(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg / 45f) + 8) % 8;
        curDir = dirByOct[oct];
        flip = flipByOct[oct];
    }

    private void StartAct(PlayerAvatar.Pose[,] frames, int fpd, float sec)
    {
        if (frames == null) return;
        actFrames = frames;
        actFpd = Mathf.Max(1, fpd);
        actFrameSec = sec;
        actElapsed = 0f;
        actDur = actFpd * actFrameSec;
        acting = true;
        moving = false;
    }

    private void StepAct()
    {
        actElapsed += Time.deltaTime;
        if (actElapsed >= actDur)
        {
            acting = false;
            ApplyPose(StandPose(curDir), flip, SortFor(worldY));
            return;
        }
        int frame = Mathf.Clamp((int)(actElapsed / actFrameSec), 0, actFpd - 1);
        int dir = Mathf.Clamp(curDir, 0, actFrames.GetLength(0) - 1);
        PlayerAvatar.Pose pf = actFrames[dir, frame];
        if (pf != null) ApplyPose(pf, flip, SortFor(worldY));
    }

    public void PlayDie()
    {
        acting = false;
        dead = true;
        moving = false;
        dieElapsed = 0f;
        if (avatar != null && avatar.ok) dieFrames = avatar.PoseFrames(3, out dieFpd);
    }

    public void Revive()
    {
        dead = false;
        dieFrames = null;
        if (body) ApplyPose(StandPose(curDir), flip, SortFor(worldY));
    }

    private void StepDie()
    {
        if (dieFrames == null && avatar != null && avatar.ok)
            dieFrames = avatar.PoseFrames(3, out dieFpd);
        if (dieFrames == null)
        {
            ApplyPose(StandPose(curDir), flip, SortFor(worldY));
            return;
        }
        int last = Mathf.Max(1, dieFpd) - 1;
        if (dieElapsed < last * DIE_FRAME_SEC) dieElapsed += Time.deltaTime;
        int frame = Mathf.Clamp((int)(dieElapsed / DIE_FRAME_SEC), 0, last);
        int dir = Mathf.Clamp(curDir, 0, dieFrames.GetLength(0) - 1);
        PlayerAvatar.Pose pf = dieFrames[dir, frame];
        if (pf != null) ApplyPose(pf, flip, SortFor(worldY));
    }

    // Original entity/appearance data is supplied by the current assembly boundary.
    internal void BindActionAppearanceForIndependentRuntime(FakeFantasyOnline.Application.CharacterAppearanceRecord value)
    {
        appear = new PlayerAvatar.Appear
        {
            Body = value.Body, Cap = value.Cap, Backpack = value.Backpack,
            WeaponR = value.RightWeapon, WeaponL = value.LeftWeapon, Face = value.Face
        };
        atkVariant = value.AttackVariant;
        SetWeapon(value.WeaponCategory, value.AttackDistance);
    }

    internal void StepActForIndependentRuntime() => StepAct();
    internal void StepDieForIndependentRuntime() => StepDie();
    internal int AttackVariantForIndependentRuntime => AtkSub(atkVariant);
    internal int ActionFramesPerDirectionForIndependentRuntime => actFpd;
    internal PlayerAvatar.Pose[,] DeathFramesForIndependentRuntime => dieFrames;
    internal int DeathFramesPerDirectionForIndependentRuntime => dieFpd;
    internal float ActionElapsedForIndependentRuntime => actElapsed;
    internal float ActionFrameSecondsForIndependentRuntime => actFrameSec;
    internal float DeathElapsedForIndependentRuntime => dieElapsed;
    internal int ActionFrameForIndependentRuntime =>
        actFrames == null ? 0 : Mathf.Clamp((int)(actElapsed / actFrameSec), 0, actFpd - 1);
    internal int DeathFrameForIndependentRuntime =>
        dieFrames == null ? 0 : Mathf.Clamp((int)(dieElapsed / DIE_FRAME_SEC), 0, Mathf.Max(1, dieFpd) - 1);
}

}
