using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    public int level;
    public int faction;
    public byte pkState;
    private BoxCollider2D pickBox;

    // 06000ae9
    public bool HostileToLocal
    {
        get
        {
            if (FreeMapHostile) return true;
            if (CombatWorld.LocalFaction != 0 && faction != 0 && CombatWorld.LocalFaction != faction)
                return true;
            return OpenWorldHostile;
        }
    }

    // 06000aea
    public bool FreeMapHostile
    {
        get
        {
            if (CharData.pkMapFree == 0 || dead || level < CharData.pkProtectLv ||
                CharData.level < CharData.pkProtectLv) return false;
            if (CharData.pkMode == 2) return !SocialClient.InParty(id);
            return true;
        }
    }

    // 06000aeb: support hostility is faction-only; free/open-world PK rules are separate.
    public bool SupportHostileToLocal => CombatWorld.LocalFaction != 0 &&
        faction != 0 && CombatWorld.LocalFaction != faction;

    // 06000aec
    public bool OpenWorldHostile
    {
        get
        {
            if (CombatWorld.LocalFaction != 0 || faction != 0 || dead ||
                CharData.pkMapSafe != 0 || CharData.pkMode == 0 ||
                level < CharData.pkProtectLv || CharData.level < CharData.pkProtectLv) return false;
            if (CharData.pkMode == 4 && pkState == 0) return false;
            if (CharData.pkMode == 2) return !SocialClient.InParty(id);
            return true;
        }
    }

    // The pickbox portion of 06000b21, after visibility/reappearance and before statusFx hiding.
    internal void UpdatePickBoxVisibilityForIndependentRuntime()
    {
        bool ready = avatar != null && avatar.ok;
        bool fallback = !ready && rebuildTries >= MaxRebuildTries;
        bool allowed = true;
        if (invis && CharData.pkMapFree != 0) allowed = !HostileToLocal;
        bool hidden = _hideEntity;
        if (pickBox)
        {
            bool enabled = pickBox.enabled;
            bool desired = (ready || fallback) && allowed && !hidden;
            if (enabled != desired) pickBox.enabled = desired;
        }
    }

    internal BoxCollider2D PickBoxForIndependentRuntime => pickBox;
}

}
