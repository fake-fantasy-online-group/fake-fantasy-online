using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class MonsterEntity
{
    private string riftOriginalName;
    private int riftClass;
    private SpriteRenderer riftBodyGlow;
    internal int RiftClass => riftClass;
    internal void UpdateRiftIdentity(int classification, string originalName)
    {
        if (riftOriginalName == null) riftOriginalName = mname;
        string name = string.IsNullOrEmpty(originalName) ? riftOriginalName : originalName;
        riftClass = classification;
        mname = RiftMonsterName(name, classification);
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
