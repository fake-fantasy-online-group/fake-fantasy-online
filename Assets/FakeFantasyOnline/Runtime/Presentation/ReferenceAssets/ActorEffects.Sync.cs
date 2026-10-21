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
            case "套装光环·战士": return "TFX074_40";
            case "套装光环·剑客": return "TFX028_40";
            case "套装光环·刺客": return "TFX7012_40";
            case "套装光环·术士": return "TFX7015_40";
            case "套装光环·药师": return "TFX7018_40";
            case "白虎守城":
            case "白虎卫身": return "钢铁";
            case "倒骑观火":
            case "朱鸟衔烬":
            case "余烬庇佑": return "火之盾";
            case "空瓢济厄":
            case "圣羽护佑": return "TFX074_40";
            case "雨洗空符":
            case "纸驴驮霜":
            case "玄武镇门": return "TFX028_40";
            default: return key;
        }
    }

    // Only the explicitly selected class-set visuals loop. Ordinary sustained
    // buffs must retain their authored settled/transparent final state.
    internal static bool EquipmentAuraLoops(string key)
    {
        return key == "套装光环·战士" || key == "套装光环·剑客" ||
            key == "套装光环·刺客" || key == "套装光环·术士" ||
            key == "套装光环·药师";
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

            string statusAsset = PlayerController.AuraEffName(pair.Key);
            string asset = EquipmentAuraAsset(statusAsset);
            // Equipment aliases keep the server-selected foot/orbit placement.
            bool classSetLoop = EquipmentAuraLoops(pair.Key);
            bool tfx = (statusAsset?.StartsWith("TFX") ?? false) || classSetLoop;
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

            // Reuse the existing bounded full-template clock. Keep native fade
            // in/out, tint, layer scale and sprite animation; never freeze a
            // hand-picked opaque frame or alter ordinary buff rendering.
            player.loopMode = classSetLoop ? 1 : 0;
            player.selfDestroy = false;
            player.transform.SetParent(transform, false);
            if (!classSetLoop) player.SettleToEnd();
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
