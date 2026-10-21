using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered ActorEffects owner for persistent aura, soul and cast attachments.
/// </summary>
public sealed partial class ActorEffects : MonoBehaviour
{
    private enum Kind
    {
        Aura,
        Cast,
    }

    private sealed class Inst
    {
        public EffectPlayer ep;
        public int mode;
        public Kind kind;
        public int idx;
        public int slot;
        public float groundOffsetY;
    }

    private const float OrbitRX = 30f;
    private const float OrbitRY = 10f;

    public float ppu = 100f;
    public float bodyHeightU = 1.4f;
    public Vector2 mountOffsetU;
    public SpriteRenderer playerSr;
    public Func<(int, bool)> FacingProvider;

    private readonly Dictionary<string, Inst> auras =
        new Dictionary<string, Inst>();
    private readonly List<Inst> casts = new List<Inst>();
    private readonly Dictionary<string, Inst> soulFx =
        new Dictionary<string, Inst>();
    private readonly List<string> _goneBuf = new List<string>();
    private bool _hidden;

    public bool Hidden => _hidden;

    // Official 1.6.0: 0x06000029.
    public bool SoulFxPending
    {
        get
        {
            foreach (Inst instance in soulFx.Values)
            {
                if (instance.ep == null) return true;
            }
            return false;
        }
    }

    internal int CastCountForAudit => casts.Count;

    internal void ConfigureForIndependentRuntime(
        float pixelsPerUnit,
        float bodyHeight,
        Vector2 mountOffset,
        SpriteRenderer bodyRenderer,
        Func<(int, bool)> facingProvider)
    {
        ppu = pixelsPerUnit > 0f ? pixelsPerUnit : 100f;
        bodyHeightU = Mathf.Max(0f, bodyHeight);
        mountOffsetU = mountOffset;
        playerSr = bodyRenderer;
        FacingProvider = facingProvider;
    }

    public void PlayCast(
        string effName,
        int attach,
        int dir = -1,
        bool flip = false)
    {
        if (_hidden)
        {
            return;
        }

        EffectPlayer player = EffectPlayer.PlayNamed(
            transform.position,
            effName,
            ppu,
            1,
            dir,
            flip,
            false);
        if (!player)
        {
            return;
        }

        player.loopMode = 2;
        player.selfDestroy = true;
        player.transform.SetParent(transform, false);
        Inst instance = new Inst
        {
            ep = player,
            mode = attach,
            kind = Kind.Cast,
        };
        casts.Add(instance);
    }

    public void SetHidden(bool h)
    {
        if (_hidden == h)
        {
            return;
        }

        _hidden = h;
        foreach (Inst instance in auras.Values)
        {
            if (instance.ep) instance.ep.gameObject.SetActive(!h);
        }
        foreach (Inst instance in soulFx.Values)
        {
            if (instance.ep) instance.ep.gameObject.SetActive(!h);
        }
        if (h)
        {
            foreach (Inst instance in casts)
            {
                if (instance.ep) instance.ep.Stop();
            }
            casts.Clear();
        }
    }

    private void Update()
    {
        long stamp = LoadTrace.On ? LoadTrace.SegStamp() : 0L;
        try
        {
            float now = Time.realtimeSinceStartup;
            foreach (Inst instance in auras.Values) Place(instance, now);
            if (soulFx.Count > 0)
            {
                (int dir, bool flip) facing = FacingProvider?.Invoke() ?? (-1, false);
                foreach (Inst instance in soulFx.Values)
                {
                    if (!instance.ep) continue;
                    if (FacingProvider != null)
                    {
                        instance.ep.facingDir = facing.dir;
                        instance.ep.facingFlip = facing.flip;
                    }
                    Place(instance, now);
                }
            }

            for (int index = casts.Count - 1; index >= 0; index--)
            {
                if (!casts[index].ep)
                {
                    casts.RemoveAt(index);
                    continue;
                }
                Place(casts[index], now);
            }
        }
        finally
        {
            if (LoadTrace.On) LoadTrace.AddSeg(LoadTrace.Seg.特效, stamp);
        }
    }

    private void Place(Inst instance, float now)
    {
        if (!instance.ep)
        {
            return;
        }

        Transform effectTransform = instance.ep.transform;
        switch (instance.mode)
        {
            case 4:
                effectTransform.localPosition = new Vector3(
                    mountOffsetU.x,
                    bodyHeightU + 0.1f + mountOffsetU.y,
                    0f);
                return;
            case 2:
                effectTransform.localPosition = new Vector3(0f, instance.groundOffsetY / ppu, 0f);
                return;
            case 1:
                float angle = now * Mathf.PI;
                if (instance.slot == 5) angle = -angle;
                float sin = Mathf.Sin(angle);
                float cos = Mathf.Cos(angle);
                float x = cos * OrbitRX / ppu;
                float y = (6 - instance.slot) * OrbitRX / ppu + sin * OrbitRY / ppu;
                effectTransform.localPosition = new Vector3(
                    x + mountOffsetU.x,
                    y + mountOffsetU.y,
                    0f);
                instance.ep.SetBehind(
                    sin < 0f,
                    playerSr ? playerSr.sortingOrder : int.MinValue);
                return;
            default:
                effectTransform.localPosition = new Vector3(
                    mountOffsetU.x,
                    mountOffsetU.y,
                    0f);
                return;
        }
    }
}

}
