using UnityEngine;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    private int gender;
    private int head;
    private int hair;
    private int rebuildTries;
    private float nextRebuildT;
    private const int MaxRebuildTries = 6;
    private static int _buildFrame;
    private static int _buildsThisFrame;
    private const float BuildStarveSec = 0.4f;
    private bool _hideEntity;
    private bool _wasHidden;
    private bool _isStallOwner;
    private bool _f11Exempt;

    public void UpdateAppearance(int bodyModel, int cap, int back, int wr, int wl, int face, int variant)
    {
        bool changed = appear.Body != bodyModel || appear.Cap != cap || appear.Backpack != back ||
            appear.WeaponR != wr || appear.WeaponL != wl || appear.Face != face || atkVariant != variant;
        appear.Body = bodyModel;
        appear.Cap = cap;
        appear.Backpack = back;
        appear.WeaponR = wr;
        appear.WeaponL = wl;
        appear.Face = face;
        atkVariant = variant;
        if (!changed && avatar != null && avatar.ok) return;
        rebuildTries = 0;
        nextRebuildT = 0f;
        if (ShouldHideNow())
        {
            if (avatar != null) avatar.Dispose();
            avatar = null;
            hasLast = false;
            workFrames = null;
            workFrame = 0;
            return;
        }
        Rebuild();
    }

    public void SetHair(int newHead, int newHair)
    {
        if (head == newHead && hair == newHair) return;
        head = newHead;
        hair = newHair;
        rebuildTries = 0;
        nextRebuildT = 0f;
        if (ShouldHideNow())
        {
            if (avatar != null) avatar.Dispose();
            avatar = null;
            hasLast = false;
            return;
        }
        Rebuild();
    }

    private void Rebuild()
    {
        PlayerAvatar old = avatar;
        long stamp = LoadTrace.Now;
        ArtUploadClock.Resume();
        try
        {
            avatar = PlayerAvatar.Build(gender, head, hair, appear, ppu);
        }
        finally
        {
            ArtUploadClock.Pause();
        }
        if (LoadTrace.On)
        {
            string label = string.Format("{0}#{1}", pname, id);
            string detail = string.Format("body={0} cap={1} back={2} wr={3} wl={4} face={5} head={6}/{7}",
                appear.Body, appear.Cap, appear.Backpack, appear.WeaponR, appear.WeaponL, appear.Face, head, hair);
            LoadTrace.Avatar(label, detail, LoadTrace.Ms(stamp), avatar != null && avatar.ok);
            LoadTrace.AvatarBuiltThisFrame();
        }
        if (old != null && old != avatar) old.Dispose();
        workFrames = null;
        workFrame = 0;
        hasLast = false;
        if (statusFx && avatar != null && avatar.ok && avatar.headPx > 0)
            statusFx.bodyHeightU = (float)avatar.headPx / ppu;
        if (avatar != null && avatar.ok && body)
            ApplyPose(StandPose(curDir), flip, SortFor(worldY));
    }

    private bool ShouldHideNow()
    {
        bool exempt = false;
        if (CharData.nameMode == 4) exempt = SocialClient.InParty(id);
        else if (CharData.nameMode == 5) exempt = StallDlg.IsStallOwner(id);
        if (CharData.nameMode >= 3 && !exempt) return true;
        if (!invis) return false;
        return !SocialClient.InParty(id);
    }

    private static int MaxBuildsPerFrame => GameSettings.TargetFrameMs >= 13f ? 2 : 1;

    private static bool TakeBuildBudget()
    {
        if (_buildFrame != Time.frameCount)
        {
            _buildFrame = Time.frameCount;
            _buildsThisFrame = 0;
        }
        if (_buildsThisFrame >= MaxBuildsPerFrame) return false;
        if (ArtUploadClock.Over && Time.unscaledTime - _lastBuildAt < BuildStarveSec) return false;
        _buildsThisFrame++;
        _lastBuildAt = Time.unscaledTime;
        return true;
    }

    // The original visibility/reappearance slice before the null-avatar branch in Update.
    internal void UpdateAppearanceVisibilityForIndependentRuntime()
    {
        bool hideInvis = invis && !InvisVisibleToMe();
        _isStallOwner = StallDlg.IsStallOwner(id);
        if (CharData.nameMode == 4) _f11Exempt = SocialClient.InParty(id);
        else if (CharData.nameMode == 5) _f11Exempt = _isStallOwner;
        else _f11Exempt = false;
        _hideEntity = (CharData.nameMode >= 3 && !_f11Exempt) || hideInvis;
        if (dollRoot && dollRoot.gameObject.activeSelf == _hideEntity)
            dollRoot.gameObject.SetActive(!_hideEntity);
        if (_hideEntity) _wasHidden = true;
        else
        {
            if (avatar == null) nextRebuildT = 0f;
            if (_wasHidden)
            {
                _wasHidden = false;
                ApplyAuras();
                if (!string.IsNullOrEmpty(soulFxCsv))
                {
                    string value = soulFxCsv;
                    soulFxCsv = string.Empty;
                    SetSoulFx(value);
                }
            }
        }
    }

    // Returns whether the original missing/not-ok avatar branch consumed this entire Update.
    internal bool UpdateMissingAvatarForIndependentRuntime()
    {
        if (avatar != null && avatar.ok) return false;
        AdvancePath();
        if (!_hideEntity && rebuildTries < MaxRebuildTries && Time.time >= nextRebuildT && TakeBuildBudget())
        {
            rebuildTries++;
            nextRebuildT = Time.time + rebuildTries * 0.5f;
            Rebuild();
        }
        return true;
    }

    internal PlayerAvatar AvatarForIndependentRuntime => avatar;
    internal CharacterAppearanceRecord AppearanceForIndependentRuntime => new CharacterAppearanceRecord(
        appear.Body, appear.Cap, appear.Backpack, appear.WeaponR, appear.WeaponL, appear.Face,
        atkVariant, weaponCType, atkDistPx, nativeAtkDistPx);
    internal bool AvatarReadyForIndependentRuntime => avatar != null && avatar.ok;
    internal bool UpdateBodyPresentForIndependentRuntime => body != null;
    internal bool AvatarSelectableForIndependentRuntime => (avatar != null && avatar.ok) || rebuildTries >= MaxRebuildTries;
    internal void UpdateEffectsVisibilityForIndependentRuntime()
    {
        if (statusFx) statusFx.SetHidden(_hideEntity);
    }
    internal bool HiddenForIndependentRuntime => _hideEntity;
    internal bool F11ExemptForIndependentRuntime => _f11Exempt;
    internal bool StallOwnerForIndependentRuntime => _isStallOwner;
}

}
