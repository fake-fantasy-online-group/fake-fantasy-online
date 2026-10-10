using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private void LoadFrames()
    {
        int gender = CharData.gender > 0 ? 1 : 0;
        int head = Mathf.Clamp(CharData.head, 1, 8);
        int hair = Mathf.Clamp(CharData.hair, 1, 8);
        PlayerAvatar.Appear appear = CharData.CombinedAppear();
        PlayerAvatar next = PlayerAvatar.Build(gender, head, hair, appear, ppu);
        if (!next.ok) { next.Dispose(); return; }
        _apVersion = CharData.apVersion;
        if (avatar != null && avatar != next) avatar.Dispose();
        avatar = next;
        DIRS = 5;
        FPD = 8;
        standFrames = next.stand;
        walkFrames = next.walk;
        sitFrames = next.sit;
        rideFrames = next.ride;
        if (standFrames != null)
        {
            PlayerAvatar.Pose first = standFrames[0];
            if (first != null) ApplyPose(first, false, GroupBase(), Color.white);
        }
        PlayerNameTag tag = GetComponent<PlayerNameTag>();
        if (tag != null && next.nameTopPx > 0)
            tag.nameHeight = (float)unchecked(next.nameTopPx + 3) / ppu;
        CombatNumber.RegisterPlayer(transform, 80f / ppu, ppu);
    }

    internal void LoadFramesForIndependentRuntime() => LoadFrames();

    internal void ReleaseAvatarForIndependentRuntime()
    {
        if (avatar == null) return;
        avatar.Dispose();
        avatar = null;
    }
}

internal static partial class CharData
{
}

}
