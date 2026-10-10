using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    private ActorEffects statusFx;

    public void ShowStatusFx(string eff, int posMode, int durMs)
    {
        if (string.IsNullOrEmpty(eff) || body == null) return;
        statusMode[eff] = posMode;
        statusExpire[eff] = Time.time + Mathf.Max(0.1f, durMs / 1000f);
        ApplyAuras();
    }

    private void TickStatus()
    {
        if (statusFx == null || statusExpire.Count == 0) return;
        List<string> expired = new List<string>();
        foreach (KeyValuePair<string, float> entry in statusExpire)
        {
            if (Time.time >= entry.Value) expired.Add(entry.Key);
        }
        if (expired.Count == 0) return;
        foreach (string key in expired)
        {
            statusExpire.Remove(key);
            statusMode.Remove(key);
        }
        ApplyAuras();
    }

    internal void TickStatusForIndependentRuntime() => TickStatus();

    private void EnsureFx()
    {
        if (statusFx) return;
        statusFx = gameObject.AddComponent<ActorEffects>();
        statusFx.ppu = ppu;
        statusFx.bodyHeightU = avatar != null && avatar.headPx > 0
            ? (float)avatar.headPx / ppu
            : body.sprite ? body.sprite.bounds.size.y : 1.4f;
        statusFx.playerSr = body;
        statusFx.FacingProvider = () => (curDir, flip);
        statusFx.SetHidden(_hideEntity);
    }

    private void ApplyAuras()
    {
        if (body == null || (statusFx == null && ShouldHideNow())) return;
        EnsureFx();
        Dictionary<string, int> values = new Dictionary<string, int>(statusMode);
        if (!string.IsNullOrEmpty(avatarAura)) values[avatarAura] = 2;
        statusFx.SyncAuras(values);
    }

    public void SetAvatarAura(string eff)
    {
        eff = eff ?? string.Empty;
        if (eff == avatarAura) return;
        avatarAura = eff;
        ApplyAuras();
    }

    public void SetTitleFx(string eff)
    {
        eff = eff ?? string.Empty;
        if (eff == titleAura) return;
        titleAura = eff;
        if (body) ApplyAuras();
    }

    public void SetSoulFx(string csv)
    {
        csv = csv ?? string.Empty;
        if (csv == soulFxCsv && !(statusFx != null && statusFx.SoulFxPending)) return;
        soulFxCsv = csv;
        if (body == null || (statusFx == null && ShouldHideNow())) return;
        EnsureFx();
        statusFx.SyncSoulFx(csv.Length == 0 ? null : csv.Split(','));
    }

    public void SetInvis(bool value)
    {
        if (invis == value) return;
        invis = value;
        hasLast = false;
    }

    // 06000afe (arm64 0x2da69c, x86_64 0x2eb7b0). The official getter short-circuits on invis
    // itself: SocialClient.InParty is only consulted for an actor that is actually invisible,
    // and no GM, faction or stall condition participates. invis (+0x134) is the same byte
    // SetInvis (0x06000af9) writes.
    public bool HiddenByInvis => invis && !SocialClient.InParty(id);

    public void SetGlowMode(int m)
    {
        m = Mathf.Clamp(m, 0, 16);
        if (glowMode == m) return;
        glowMode = m;
        hasLast = false;
    }

    internal ActorEffects EnsureEffectsForIndependentRuntime()
    {
        EnsureFx();
        return statusFx;
    }
    internal ActorEffects EffectsForIndependentRuntime => statusFx;
}

}
