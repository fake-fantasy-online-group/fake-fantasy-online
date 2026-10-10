using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    private bool riding;
    private int mountModel;
    private int mountFrame;
    private float mountTimer;
    private SpriteRenderer srMount;
    private SpriteRenderer srMountTop;
    private SpriteRenderer srMountFx;
    private float ridingRaiseY;
    private float ridingNameOffX;
    private bool mountTianmo;
    private EffectPlayer mountRing;
    private bool mountRingFailed;
    private int riderSeat;
    // This is also the product counterpart of official RemotePlayer.get_RideAnchorId (0x06000b24):
    // both frozen bodies are a bare read of +0x218, the atlas has no call edge for that token, and
    // its one native consumer FollowRideAnchor (0x06000b27) inlines the same field read, so no
    // named accessor is added for the token alone.
    private int rideAnchorId;
    private int mountSeats;

    // Official 1.5.8 RemotePlayer.get_CanCarryPassenger (0x06000b25): arm64 0x2e0cfc / 44 bytes and
    // x86_64 0x2f1fa0 / 48 bytes both evaluate
    //   riding(0x1bc) && rideAnchorId(0x218) == 0 && 1 < mountSeats(0x21c)
    // and return false on every other path. A mounted passenger (anchor != 0) never carries a
    // second player even with spare seats, and an unmounted owner never does either.
    public bool CanCarryPassenger => riding && rideAnchorId == 0 && mountSeats > 1;

    // Official 1.5.8 RemotePlayer.SetRiding (0x06000b26, arm64 0x2e0d28 / 980 bytes,
    // x86_64 0x2f1fd0) - verified against both bodies while resolving the 0x8023 optional
    // riding tail, so this owner is the only place that decides the riding normalization:
    //   arm64  0x2e0d88 strb w21,[x19,#0x1bc]      riding  = on
    //          0x2e0d8c str  w25,[x19,#0x1c0]      model   = model        (unconditional)
    //          0x2e0d90 strb w24,[x19,#0x200]      tianmo  = tianmo       (unconditional)
    //          0x2e0d98 csel w8, w23, wzr, ne      seat    = on ? seat : 0
    //          0x2e0da0 csel w8, w22, wzr, ne      anchor  = on ? anchor : 0
    //          0x2e0da8 csel w8, w20, wzr, ne      seats   = on ? seats : 0
    //          0x2e0db0 tbnz w21,#0,end            the mount teardown only runs when off
    //   x86_64 0x2f2021 mov [r12+0x1bc],r13b ; 0x2f2029 mov [r12+0x1c0],edx ;
    //          0x2f203a mov [r12+0x200],cl ; 0x2f2036/0x2f204a/0x2f2056 cmove r15d/r14d/ebx,eax
    //          0x2f2061 jne end
    // The wire values are therefore forwarded raw by the 0x8023 decode adapter and normalized
    // here; nothing else may pre-zero them, and model and tianmo are deliberately not cleared
    // when riding is false.
    public void SetRiding(bool on, int model, bool tianmo, int seat, int anchorId, int seats)
    {
        riding = on;
        mountModel = model;
        mountTianmo = tianmo;
        riderSeat = on ? seat : 0;
        rideAnchorId = on ? anchorId : 0;
        mountSeats = on ? seats : 0;
        if (on) return;
        MountAvatar.Release(mountData);
        mountData = null;
        loadedMount = -1;
        if (srMount) srMount.enabled = false;
        if (srMountTop) srMountTop.enabled = false;
        if (srMountFx) srMountFx.enabled = false;
        if (dollRoot) dollRoot.localPosition = Vector3.zero;
        ridingRaiseY = 0f;
        ridingNameOffX = 0f;
        UpdatePickBox(0f, 0f);
        if (statusFx) statusFx.mountOffsetU = Vector2.zero;
        if (mountRing)
        {
            Object.Destroy(mountRing.gameObject);
            mountRing = null;
        }
        mountRingFailed = false;
        hasLast = false;
    }

    public void DropMountArt()
    {
        if (!riding && loadedMount == -1 && mountData == null) return;
        MountAvatar.Release(mountData);
        mountData = null;
        loadedMount = -1;
        if (srMount) srMount.enabled = false;
        if (srMountTop) srMountTop.enabled = false;
        if (srMountFx) srMountFx.enabled = false;
        if (dollRoot) dollRoot.localPosition = Vector3.zero;
        ridingRaiseY = 0f;
        ridingNameOffX = 0f;
        if (mountRing)
        {
            Object.Destroy(mountRing.gameObject);
            mountRing = null;
        }
        hasLast = false;
    }

    internal void DropMountArtForIndependentRuntime() => DropMountArt();
}

}
