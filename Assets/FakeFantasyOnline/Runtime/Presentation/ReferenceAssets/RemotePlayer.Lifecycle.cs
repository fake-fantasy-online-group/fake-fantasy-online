using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    /// <summary>
    /// Official 1.5.8 RemotePlayer.Init (0x06000af6, arm64 0x2d9148 / 668 bytes,
    /// x86_64 0x2ea310 / 768 bytes) - the spawn-time owner of the remote identity.
    /// </summary>
    /// <remarks>
    /// Both bodies were exported from the frozen 1.5.8 persistent Ghidra project and agree on
    /// every store, so the order below is the official order and not a re-ordering:
    /// <list type="bullet">
    /// <item>id (+0x20) is stored first, then pname (+0x28) and title (+0x30) - the two managed
    /// string stores with their write barriers - then level (+0x24), then gender (+0x38),
    /// head (+0x3c), hair (+0x40) and atkVariant (+0x44). This is the literal store order of both
    /// exports (arm64 lines 41-50, x86_64 lines 40-49): level is written after the two strings,
    /// not between id and pname.</item>
    /// <item>appear (+0x48) is the whole 24-byte <see cref="PlayerAvatar.Appear"/> value copied
    /// from the argument, not a reference.</item>
    /// <item>worldX (+0x98), worldY (+0x9c), ppu (+0xa0), worldW (+0xa4) and worldH (+0xa8) are
    /// stored before <c>transform.position</c> is computed from them, so the position uses the
    /// incoming world size and never a default.</item>
    /// <item><c>transform.position</c> is <c>((wx - worldW * 0.5f) / ppu, (worldH * 0.5f - wy) / ppu, 0)</c>
    /// with no rounding - the rounding in SetPath/AdvancePath belongs to those owners only.</item>
    /// <item>body (+0xb0) is <c>gameObject.AddComponent&lt;SpriteRenderer&gt;()</c> and is disabled
    /// before EnsureDoll (0x06000b01) runs, then ShouldHideNow (0x06000b20) and
    /// TakeBuildBudget (0x06000b05) gate one Rebuild (0x06000b06).</item>
    /// <item>body.sortingOrder is <c>Mathf.Clamp((int)worldY, 0, 30000)</c> (arm64 clears the value
    /// for a non-finite worldY, x86_64 reaches the same 0 through its own conversion).</item>
    /// <item>pickBox (+0x120) is the second AddComponent and receives the two official static
    /// readonly Vector2 values PickSizeFoot and PickOffFoot, in that order.</item>
    /// </list>
    /// Init deliberately does not touch dead, faction, pkState, invis, glowMode, hp or the mount:
    /// those are written by the spawn tail that follows it.
    /// <para>
    /// This member is the only producer of a remote player's avatar. The <c>Rebuild</c> call above is
    /// behind the official hide and frame-budget gate, so a spawn that is hidden or that arrives after
    /// the frame budget is spent legitimately leaves <c>avatar</c> null; the owner's own
    /// missing-avatar update branch re-tries it on later frames with the official backoff and try
    /// limit. No caller may hand an avatar in from outside, and no caller may build one for the same
    /// spawn: a second avatar would either replace the gated one - disposing it immediately - or
    /// bypass the gate entirely. Hosts read <c>AvatarForIndependentRuntime</c> and wrap the frames
    /// they get into a non-owning view; disposing that view never disposes the avatar.
    /// </para>
    /// </remarks>
    public void Init(int entityId, string name, string title, int level, float wx, float wy,
        int gender, int head, int hair, int atkVariant, PlayerAvatar.Appear ap,
        float pixelsPerUnit, float width, float height)
    {
        id = entityId;
        pname = name;
        this.title = title;
        this.level = level;
        this.gender = gender;
        this.head = head;
        this.hair = hair;
        this.atkVariant = atkVariant;
        appear = ap;
        worldX = wx;
        worldY = wy;
        ppu = pixelsPerUnit;
        worldW = width;
        worldH = height;
        transform.position = WorldToLocal(worldX, worldY);
        body = gameObject.AddComponent<SpriteRenderer>();
        body.enabled = false;
        EnsureDoll();
        if (!ShouldHideNow() && TakeBuildBudget()) Rebuild();
        body.sortingOrder = SortFor(worldY);
        pickBox = gameObject.AddComponent<BoxCollider2D>();
        pickBox.size = PickSizeFoot;
        pickBox.offset = PickOffFoot;
    }

    /// <summary>
    /// Official 1.5.8 RemotePlayer.OnDestroy (0x06000b37, arm64 0x2e41ec / 620 bytes,
    /// x86_64 0x2f51e0 / 576 bytes) - the single teardown owner of one remote player.
    /// </summary>
    /// <remarks>
    /// Unity raises this message when the root object goes away, so it is the only place that may
    /// release these resources; the host actor no longer repeats any of them.
    /// <list type="bullet">
    /// <item>avatar (+0x60) is disposed only when non-null (CLR null test).</item>
    /// <item>MountAvatar.Release (0x06000673) receives mountData (+0x1c8) unconditionally, then
    /// mountData is cleared through a write barrier and loadedMount (+0x1d0) goes back to -1.</item>
    /// <item>mountRing (+0x208) is tested with the UnityEngine.Object inequality - a destroyed
    /// mount ring is already gone - and its gameObject is destroyed before the field is cleared.</item>
    /// <item>tagSr (+0x250) keeps the sprite/texture pair UpdateNameTag baked: the sprite is read
    /// twice by the original body, the texture is taken from the first read, and the sprite is
    /// destroyed before the texture, so the texture is no longer referenced by the sprite.</item>
    /// <item>hpBarView (+0x288) is disposed and TargetSelect.OnGone (0x06000e56) is then handed
    /// this player; both sit behind the original null check, which is why a missing bar throws.</item>
    /// </list>
    /// The null semantics are official and must not be softened: the avatar, mount-ring and name-tag
    /// arms are guarded, but the head HP bar arm is not. In both exports a null bar reaches the
    /// non-returning helper (arm64 <c>FUN_00177a74</c> at LAB_002e4454, x86_64 counterpart), and the
    /// same label is also reached when the mount ring or the name-tag renderer becomes null between
    /// its own inequality test and its dereference - IL2CPP emits those duplicate guards
    /// (arm64 lines 57, 71, 82-84, 86). The field is initialised inline in
    /// <c>RemotePlayer.NameAndHp.cs</c>, so it is non-null from the moment the component is added and
    /// the arm is never taken in practice; <c>HeadHpBarView.Dispose</c> is itself idempotent, so a
    /// repeated teardown cannot double-destroy its root.
    /// <para>
    /// This member is the only disposer of the avatar. A stand-pose view wrapping the avatar's frames
    /// is non-owning and disposes only its own wrapped poses, so disposing a view before teardown
    /// leaves the avatar alive and disposing it twice is not possible.
    /// </para>
    /// </remarks>
    private void OnDestroy()
    {
        if (avatar != null) avatar.Dispose();
        MountAvatar.Release(mountData);
        mountData = null;
        loadedMount = -1;
        if (mountRing)
        {
            Object.Destroy(mountRing.gameObject);
            mountRing = null;
        }
        if (tagSr && tagSr.sprite)
        {
            Texture2D texture = tagSr.sprite.texture;
            Object.Destroy(tagSr.sprite);
            if (texture) Object.Destroy(texture);
        }
        hpBarView.Dispose();
        TargetSelect.OnGone(this);
    }
}

}
