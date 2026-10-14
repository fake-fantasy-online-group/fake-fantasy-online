using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class MonsterEntity
{
    private string riftOriginalName;
    private int riftClass;
    private SpriteRenderer riftBodyGlow;
    private RiftEliteActorVisual riftEliteActor;
    internal int RiftClass => riftClass;
    internal void UpdateRiftEliteState(RiftEliteVisualAssets assets, bool golden, bool agility, bool clone, double serverNow)
    {
        if (body == null || dying || !golden && !agility && !clone) { ClearRiftEliteState(); return; }
        if (riftEliteActor == null)
        {
            var root = new GameObject("RiftEliteActor"); root.transform.SetParent(transform, false);
            riftEliteActor = root.AddComponent<RiftEliteActorVisual>();
            riftEliteActor.Bind(body, assets);
        }
        riftEliteActor.SetState(golden, agility, clone, serverNow);
        if (riftBodyGlow != null && (golden || clone)) riftBodyGlow.enabled = false;
    }
    internal void ClearRiftEliteState()
    {
        if (riftEliteActor != null) { riftEliteActor.Release(); Destroy(riftEliteActor.gameObject); }
        riftEliteActor = null;
    }
    internal void UpdateRiftIdentity(int classification, string originalName)
    {
        if (riftOriginalName == null) riftOriginalName = mname;
        string name = string.IsNullOrEmpty(originalName) ? riftOriginalName : originalName;
        riftClass = classification;
        mname = classification == 0 ? name : RiftMonsterName(name, classification);
        if (classification == 0)
        {
            if (riftBodyGlow != null) { riftBodyGlow.enabled = false; Destroy(riftBodyGlow.gameObject); riftBodyGlow = null; }
            if (eliteRing != null) eliteRing.ClearRiftGlow();
            return;
        }
        if (eliteRing != null) eliteRing.SetRiftGlow(classification == 1 ? 8 : 5);
        if (body == null) return;
        if (riftBodyGlow == null)
        {
            var root = new GameObject("RiftIdentityGlow"); root.transform.SetParent(body.transform, false);
            root.transform.localScale = new Vector3(1.025f, 1.025f, 1);
            riftBodyGlow = root.AddComponent<SpriteRenderer>();
        }
        riftBodyGlow.sprite = body.sprite; riftBodyGlow.flipX = body.flipX;
        riftBodyGlow.sortingOrder = body.sortingOrder + 2; riftBodyGlow.sortingLayerID = body.sortingLayerID;
        riftBodyGlow.sharedMaterial = EffectPlayer.GlowLayerMat(classification == 1 ? 8 : 5);
        riftBodyGlow.color = new Color(1, 1, 1, .38f);
        riftBodyGlow.enabled = body.enabled && !dying;
    }
    internal static string RiftMonsterName(string originalName, int classification)
    {
        string suffix = classification == 1 ? "（精英）" : classification == 2 ? "（首领）" : "（镇境劫影）";
        // New servers already put this exact name on native spawn packets.
        return originalName.EndsWith(suffix, System.StringComparison.Ordinal) ? originalName : originalName + suffix;
    }
    internal void ClearRiftIdentity()
    {
        ClearRiftEliteState();
        if (riftOriginalName != null) mname = riftOriginalName;
        riftOriginalName = null; riftClass = 0;
        if (riftBodyGlow != null) { riftBodyGlow.enabled = false; riftBodyGlow.gameObject.SetActive(false); Destroy(riftBodyGlow.gameObject); }
        riftBodyGlow = null;
        if (eliteRing != null) eliteRing.ClearRiftGlow();
    }
}
public partial class EffectPlayer
{
    private Material[] riftOriginalMaterials;
    internal void SetRiftGlow(int mode)
    {
        if (rends == null) return;
        if (riftOriginalMaterials == null)
        {
            riftOriginalMaterials = new Material[rends.Length];
            for (int i = 0; i < rends.Length; i++) if (rends[i] != null) riftOriginalMaterials[i] = rends[i].sharedMaterial;
        }
        for (int i = 0; i < rends.Length; i++) if (rends[i] != null) rends[i].sharedMaterial = GlowLayerMat(mode);
    }
    internal void ClearRiftGlow()
    {
        if (riftOriginalMaterials == null) return;
        for (int i = 0; i < rends.Length && i < riftOriginalMaterials.Length; i++)
            if (rends[i] != null) rends[i].sharedMaterial = riftOriginalMaterials[i];
        riftOriginalMaterials = null;
    }
}
}
