using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class ActorEffects
{
    // Keep equipment aura identities separate from ordinary buffs even when
    // their authored art reuses an existing effect package.
    internal static string EquipmentAuraAsset(string key)
    {
        switch (key)
        {
            case "白虎卫身": return "钢铁";
            case "余烬庇佑": return "火之盾";
            case "圣羽护佑": return "TFX074_40";
            default: return key;
        }
    }

    // 0x06000027
    public void SyncAuras(Dictionary<string, int> want)
    {
        _goneBuf.Clear();
        foreach (KeyValuePair<string, Inst> pair in auras)
        {
            if (!want.ContainsKey(pair.Key))
            {
                if (pair.Value.ep) pair.Value.ep.Stop();
                _goneBuf.Add(pair.Key);
            }
        }
        foreach (string key in _goneBuf) auras.Remove(key);

        int orbitCount = 0;
        foreach (Inst instance in auras.Values)
        {
            if (instance.mode == 1) orbitCount++;
        }

        foreach (KeyValuePair<string, int> pair in want)
        {
            int mode = pair.Value;
            if (mode < 0 || auras.ContainsKey(pair.Key)) continue;

            string asset = EquipmentAuraAsset(pair.Key);
            // Equipment aliases keep the server-selected foot/orbit placement.
            bool tfx = pair.Key?.StartsWith("TFX") ?? false;
            EffectPlayer player = EffectPlayer.PlayNamed(
                transform.position, asset, ppu, 1, -1, false, false);
            int slot = 0;
            bool orbit = false;
            if (mode == 4 || tfx)
            {
                mode = mode == 4 ? 4 : 3;
            }
            else if (mode == 1)
            {
                slot = 4 + orbitCount++ % 3;
                orbit = true;
            }

            if (!player)
            {
                auras[pair.Key] = new Inst { ep = null, mode = mode, slot = slot };
                continue;
            }

            player.loopMode = 0;
            player.selfDestroy = false;
            player.transform.SetParent(transform, false);
            player.SettleToEnd();
            if (!orbit && playerSr)
            {
                player.attachSort = playerSr;
                player.attachUnder = true;
            }
            if (_hidden) player.gameObject.SetActive(false);
            auras[pair.Key] = new Inst
            {
                ep = player,
                mode = mode,
                kind = Kind.Aura,
                idx = auras.Count,
                slot = slot,
            };
        }
    }

    // 0x06000028
    public void SyncSoulFx(IEnumerable<string> names)
    {
        HashSet<string> want = new HashSet<string>();
        if (names != null)
        {
            foreach (string name in names)
            {
                if (!string.IsNullOrEmpty(name)) want.Add(name);
            }
        }

        List<string> gone = new List<string>();
        foreach (KeyValuePair<string, Inst> pair in soulFx)
        {
            if (!want.Contains(pair.Key))
            {
                if (pair.Value.ep) pair.Value.ep.Stop();
                gone.Add(pair.Key);
            }
        }
        foreach (string key in gone) soulFx.Remove(key);

        foreach (string name in want)
        {
            if (soulFx.TryGetValue(name, out Inst existing) && existing.ep != null) continue;
            EffectPlayer player = EffectPlayer.PlayNamed(
                transform.position, name, ppu, 1, -1, false, true);
            if (player == null)
            {
                if (!soulFx.ContainsKey(name))
                {
                    UnityEngine.Debug.LogWarning(
                        "[ActorEffects] 部位灵/龙霸特效没播出来: effect/" + name +
                        ".eff(素材缺失或还没就绪) —— 下次外观变化时会自动再试");
                }
                soulFx[name] = new Inst { ep = null, mode = 2 };
                continue;
            }

            player.loopMode = 0;
            player.selfDestroy = false;
            player.transform.SetParent(transform, false);
            if (playerSr)
            {
                player.attachSort = playerSr;
                player.attachUnder = false;
            }
            if (_hidden) player.gameObject.SetActive(false);
            soulFx[name] = new Inst
            {
                ep = player,
                mode = 2,
                kind = Kind.Aura,
                idx = soulFx.Count,
            };
        }
    }
}

}
