using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private SpriteRenderer sr;
    private Transform dollRoot;
    private SpriteRenderer[] dollSr;
    private SpriteRenderer dollBodySr;
    private ReferenceWorldBlobShadow shadow;
    private MaterialPropertyBlock _dollMpb;
    // Observation only; never used for native selection, cache gates or rendering decisions.
    private PlayerAvatar.Pose appliedPoseForIndependentRuntime;

    private void EnsureDoll()
    {
        if (dollRoot != null || sr == null) return;
        GameObject root = new GameObject("DollRoot");
        root.transform.SetParent(transform, false);
        dollRoot = root.transform;
        dollSr = new SpriteRenderer[14];
        for (int i = 0; i < dollSr.Length; i++)
        {
            GameObject layer = new GameObject("L" + i);
            layer.transform.SetParent(dollRoot, false);
            SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = sr.sharedMaterial;
            renderer.enabled = false;
            dollSr[i] = renderer;
        }
        shadow = ReferenceWorldBlobShadow.Attach(transform, null, 0f);
    }

    private void SyncShadowSize()
    {
        if (!shadow) return;
        if (shadow.diameterU <= 0f && avatar != null && avatar.bodyBoundW > 0)
            shadow.SetDiameterPx(avatar.bodyBoundW, ppu, 87);
        if (shadow.mountDiameterU <= 0f && mountData != null && mountData.boundW > 0 && ppu > 0f)
            shadow.mountDiameterU = ReferenceWorldBlobShadow.DiameterPx(mountData.boundW, 87) / ppu;
    }

    private void ApplyPose(PlayerAvatar.Pose pf, bool flip, int baseSO, Color32 tint)
    {
        EnsureDoll();
        dollBodySr = null;
        if (dollSr == null) return;
        if (pf == null || pf.layers == null)
        {
            foreach (SpriteRenderer renderer in dollSr) renderer.enabled = false;
            appliedPoseForIndependentRuntime = pf;
            return;
        }
        if (_dollMpb == null) _dollMpb = new MaterialPropertyBlock();
        float depthY = transform.position.y;
        Color color = tint;
        int count = Mathf.Min(pf.layers.Length, dollSr.Length);
        for (int i = 0; i < count; i++)
        {
            PlayerAvatar.LayerInst layer = pf.layers[i];
            SpriteRenderer renderer = dollSr[i];
            if (!layer.spr)
            {
                renderer.enabled = false;
                continue;
            }
            renderer.enabled = true;
            renderer.sprite = layer.spr;
            renderer.flipX = flip;
            renderer.transform.localPosition = new Vector3(flip ? -layer.offX : layer.offX, -layer.offY, 0f);
            renderer.sortingOrder = unchecked(baseSO + i);
            if (layer.additive)
            {
                renderer.color = Color.white;
                int glowModel = avatar != null ? avatar.GlowModelOf(layer.tag) : 0;
                renderer.sharedMaterial = DollGlowMat(glowModel);
            }
            else
            {
                renderer.color = color;
                renderer.sharedMaterial = sr.sharedMaterial;
                renderer.GetPropertyBlock(_dollMpb);
                _dollMpb.SetFloat("_DepthYOverride", depthY);
                renderer.SetPropertyBlock(_dollMpb);
            }
            if (layer.tag == 0) dollBodySr = renderer;
        }
        for (int i = count; i < dollSr.Length; i++) dollSr[i].enabled = false;
        if (shadow)
        {
            shadow.src = dollBodySr;
            SyncShadowSize();
        }
        appliedPoseForIndependentRuntime = pf;
    }

    internal PlayerAvatar.Pose AppliedPoseForIndependentRuntime => appliedPoseForIndependentRuntime;
    internal void BindRendererForIndependentRuntime(SpriteRenderer renderer)
    {
        sr = renderer;
        sr.color = Color.white;
        Material material = GameBootstrap.IsoYCharMat();
        if (material)
        {
            sr.sharedMaterial = material;
            sr.sortingOrder = 0;
        }
        sr.enabled = false;
        EnsureDoll();
    }
    internal void ApplyPoseForIndependentRuntime(PlayerAvatar.Pose pf, bool mirror, int baseSO, Color32 tint) =>
        ApplyPose(pf, mirror, baseSO, tint);
    internal Transform DollRootForIndependentRuntime => dollRoot;
    internal SpriteRenderer[] DollRenderersForIndependentRuntime => dollSr;
    internal SpriteRenderer DollBodyForIndependentRuntime => dollBodySr;
    internal ReferenceWorldBlobShadow ShadowForIndependentRuntime => shadow;
}

}
