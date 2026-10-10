using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class MonsterEntity
{
    private ActorEffects statusFx;

    public void ShowStatusFx(string eff, int posMode, int durMs)
    {
        if (string.IsNullOrEmpty(eff) || !body) return;
        if (!statusFx)
        {
            statusFx = gameObject.AddComponent<ActorEffects>();
            statusFx.ppu = ppu;
            statusFx.bodyHeightU = body.sprite ? body.sprite.bounds.size.y : 1.4f;
            statusFx.playerSr = body;
        }
        statusMode[eff] = posMode;
        statusExpire[eff] = Time.time + Mathf.Max(0.1f, durMs / 1000f);
        statusFx.SyncAuras(new Dictionary<string, int>(statusMode));
    }

    private void TickStatus()
    {
        if (!statusFx || statusExpire.Count == 0) return;
        var expired = new List<string>();
        foreach (KeyValuePair<string, float> pair in statusExpire)
            if (Time.time >= pair.Value) expired.Add(pair.Key);
        if (expired.Count == 0) return;
        foreach (string key in expired)
        {
            statusExpire.Remove(key);
            statusMode.Remove(key);
        }
        statusFx.SyncAuras(new Dictionary<string, int>(statusMode));
    }
}

}
