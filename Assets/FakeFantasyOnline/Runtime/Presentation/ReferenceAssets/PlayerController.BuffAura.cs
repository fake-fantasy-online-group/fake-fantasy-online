using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private readonly Dictionary<string, int> _auraWant = new Dictionary<string, int>();
    // 06000a20 allocates the aura dictionary before the movement path. Keep these
    // two allocating initializers in one partial declaration so their order is defined.
    private readonly List<Vector2> path = new List<Vector2>();

    private void UpdateBuffAura()
    {
        List<CharData.BuffView> buffs = CharData.buffs;
        float now = Time.realtimeSinceStartup;
        _auraWant.Clear();
        Dictionary<string, int> want = _auraWant;
        if (buffs != null)
        {
            foreach (CharData.BuffView buff in buffs)
            {
                if (string.IsNullOrEmpty(buff.name) || buff.posMode < 0) continue;
                if (buff.remainSec >= 0 &&
                    unchecked(buff.remainSec - Mathf.FloorToInt(now - buff.recvTime)) <= 0) continue;
                want[AuraEffName(buff.name)] = buff.posMode;
            }
        }
        if (!string.IsNullOrEmpty(CharData.avEffect)) want[CharData.avEffect] = 2;
        EnsureActorFx();
        actorFx.SyncAuras(want);
    }

    private void EnsureActorFx()
    {
        if (actorFx == null)
        {
            actorFx = gameObject.AddComponent<ActorEffects>();
            actorFx.ppu = ppu;
            actorFx.FacingProvider = () => (curDir, flip);
        }
        if (avatar != null && avatar.headPx > 0)
            actorFx.bodyHeightU = (float)avatar.headPx / ppu;
        actorFx.playerSr = sr;
    }

    internal ActorEffects EnsureEffectsForIndependentRuntime()
    {
        EnsureActorFx();
        return actorFx;
    }
}

internal static partial class CharData
{
    internal sealed class BuffView
    {
        internal int skillId;
        internal int iconId;
        internal int remainSec;
        internal string name = string.Empty;
        internal string desc = string.Empty;
        internal float recvTime;
        internal bool good = true;
        internal int posMode = -1;
    }




}

}
