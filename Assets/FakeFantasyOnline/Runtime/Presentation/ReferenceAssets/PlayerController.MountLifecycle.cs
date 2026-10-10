using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private SpriteRenderer srMount;
    private SpriteRenderer srMountTop;
    private SpriteRenderer srMountFx;
    private EffectPlayer mountRing;
    private bool mountRingFailed;
    private MountAvatar.Data mountData;
    private int loadedMount = -1;
    private int mountFrame;
    private float mountTimer;
    private ActorEffects actorFx;
    private Color32 dollTint = new Color32(255, 255, 255, 255);

    // PlayerController.Update prefix, before F11 and every early-return branch.
    internal void UpdateFrameTintForIndependentRuntime()
    {
        dollTint = CharData.invisible ? new Color32(64, 64, 128, 80) : new Color32(255, 255, 255, 255);
        if (srMount) srMount.color = dollTint;
        if (srMountTop) srMountTop.color = dollTint;
    }

    // The independent host releases its lease on teardown without altering a newer scene's globals.
    internal void ReleaseMountLeaseForIndependentRuntime()
    {
        if (mountData == null) return;
        MountAvatar.Release(mountData);
        mountData = null;
    }

    internal SpriteRenderer MountBodyForIndependentRuntime => srMount;
    internal SpriteRenderer MountTopForIndependentRuntime => srMountTop;
    internal SpriteRenderer MountFxForIndependentRuntime => srMountFx;

    private void EnsureMount()
    {
        if (srMount != null || sr == null) return;
        GameObject root = new GameObject("MountPet");
        root.transform.SetParent(transform, false);
        srMount = root.AddComponent<SpriteRenderer>();
        srMount.sharedMaterial = sr.sharedMaterial;
        root = new GameObject("MountTop");
        root.transform.SetParent(transform, false);
        srMountTop = root.AddComponent<SpriteRenderer>();
        srMountTop.sharedMaterial = sr.sharedMaterial;
        root = new GameObject("MountFx");
        root.transform.SetParent(transform, false);
        srMountFx = root.AddComponent<SpriteRenderer>();
        srMountFx.sharedMaterial = sr.sharedMaterial;
        Color color = dollTint;
        srMountTop.color = color;
        srMount.color = color;
        if (shadow) shadow.mountSrc = srMount;
    }

    private void HideMount()
    {
        if (mountData != null)
        {
            MountAvatar.Release(mountData);
            mountData = null;
            loadedMount = -1;
        }
        if (srMount) srMount.enabled = false;
        if (srMountTop) srMountTop.enabled = false;
        if (srMountFx) srMountFx.enabled = false;
        if (mountRing)
        {
            Destroy(mountRing.gameObject);
            mountRing = null;
        }
        mountRingFailed = false;
        if (dollRoot) dollRoot.localPosition = Vector3.zero;
        if (actorFx) actorFx.mountOffsetU = Vector2.zero;
        CharData.mountNameOffXU = 0f;
    }
}

internal static partial class CharData
{
}

}
