using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    private Transform dollRoot;
    private SpriteRenderer[] dollSr;
    private SpriteRenderer dollBodySr;
    private bool invis;
    private ReferenceWorldBlobShadow shadow;
    private PlayerAvatar.Pose lastPf;
    private bool lastFlip;
    private int lastSO;
    private bool hasLast;
    private MountAvatar.Data mountData;

    // 0x06000b01. The existing actor assembler may supply this same hierarchy before the call.
    private void EnsureDoll()
    {
        if (dollRoot != null || body == null) return;

        GameObject root = new GameObject("DollRoot");
        root.transform.SetParent(transform, false);
        dollRoot = root.transform;
        dollSr = new SpriteRenderer[14];
        for (int i = 0; i < dollSr.Length; i++)
        {
            GameObject layer = new GameObject("L" + i);
            layer.transform.SetParent(dollRoot, false);
            SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = body.sharedMaterial;
            renderer.enabled = false;
            dollSr[i] = renderer;
        }
        shadow = ReferenceWorldBlobShadow.Attach(transform, null, 0f);
    }

    // 0x06000b02. Mount data production remains separate; the original default is null.
    private void SyncShadowSize()
    {
        if (shadow == null) return;
        if (shadow.diameterU <= 0f && avatar != null && avatar.bodyBoundW > 0)
            shadow.SetDiameterPx(avatar.bodyBoundW, ppu, 87);
        if (shadow.mountDiameterU <= 0f && mountData != null && mountData.boundW > 0 && ppu > 0f)
            shadow.mountDiameterU = ReferenceWorldBlobShadow.DiameterPx(mountData.boundW, 87) / ppu;
    }

    // 0x06000b03. All callers sharing the doll renderers also share this original pose cache.
    private void ApplyPose(PlayerAvatar.Pose pf, bool flip, int baseSO)
    {
        long stamp = LoadTrace.On ? LoadTrace.SegStamp() : 0L;
        try
        {
            if (hasLast && lastPf == pf && lastFlip == flip)
            {
                if (lastSO == baseSO) return;
                lastSO = baseSO;
                if (dollSr != null)
                {
                    for (int i = 0; i < dollSr.Length; i++)
                    {
                        SpriteRenderer renderer = dollSr[i];
                        if (renderer != null && renderer.enabled)
                            renderer.sortingOrder = unchecked(baseSO + i);
                    }
                }
                return;
            }

            lastPf = pf;
            lastFlip = flip;
            lastSO = baseSO;
            hasLast = true;
            Color32 tint = invis ? new Color32(64, 64, 128, 80) : (Color32)Color.white;
            EnsureDoll();
            dollBodySr = null;
            if (dollSr == null) return;
            if (pf == null || pf.layers == null)
            {
                for (int i = 0; i < dollSr.Length; i++) dollSr[i].enabled = false;
                return;
            }

            int count = Mathf.Min(pf.layers.Length, dollSr.Length);
            for (int i = 0; i < count; i++)
            {
                PlayerAvatar.LayerInst layer = pf.layers[i];
                SpriteRenderer renderer = dollSr[i];
                if (layer.spr == null)
                {
                    renderer.enabled = false;
                    continue;
                }

                renderer.enabled = true;
                renderer.sprite = layer.spr;
                renderer.flipX = flip;
                renderer.transform.localPosition = new Vector3(
                    flip ? -layer.offX : layer.offX, -layer.offY, 0f);
                renderer.sortingOrder = unchecked(baseSO + i);
                if (layer.additive)
                {
                    int model = avatar == null ? 0 : avatar.GlowModelOf(layer.tag);
                    renderer.sharedMaterial = EffectPlayer.GlowLayerMat(
                        glowMode, SuitFxDB.IsGlowPremulModel(model));
                }
                else
                {
                    renderer.sharedMaterial = body.sharedMaterial;
                }
                renderer.color = tint;
                if (layer.tag == 0) dollBodySr = renderer;
            }

            for (int i = count; i < dollSr.Length; i++) dollSr[i].enabled = false;
            if (shadow != null)
            {
                shadow.src = dollBodySr;
                SyncShadowSize();
            }
        }
        finally
        {
            if (LoadTrace.On) LoadTrace.AddSeg((LoadTrace.Seg)5, stamp);
        }
    }

    internal void BindPoseStateForIndependentRuntime(bool invis, int glowMode)
    {
        this.invis = invis;
        this.glowMode = glowMode;
    }

    internal void ApplyPoseForIndependentRuntime(PlayerAvatar.Pose pf, bool flip, int baseSO) =>
        ApplyPose(pf, flip, baseSO);

    internal SpriteRenderer DollBodyRendererForIndependentRuntime => dollBodySr;
    internal PlayerAvatar.Pose LastPoseForIndependentRuntime => lastPf;
    internal Transform DollRootForIndependentRuntime => dollRoot;
    internal SpriteRenderer[] DollRenderersForIndependentRuntime => dollSr;
    internal ReferenceWorldBlobShadow ShadowForIndependentRuntime => shadow;
}

}
