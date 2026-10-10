using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// The exact observable result of RemotePlayer.OnMouseDown before combat ownership is applied.
/// </summary>
internal enum ReferenceWorldRemotePointerSelection
{
    None = 0,
    FriendlySelection = 1,
    PlayerVsPlayerSelection = 2
}

/// <summary>
/// Historical explicit-pointer diagnostic classification; live clicks use RemotePlayer.OnMouseDown.
/// </summary>
internal static class ReferenceWorldTargetSelectionMath
{
    public static bool IsFriendly(
        int localFaction,
        int remoteFaction,
        bool openWorldHostile)
    {
        if (localFaction != 0 && remoteFaction != 0 &&
            localFaction != remoteFaction)
        {
            return false;
        }

        return !openWorldHostile;
    }

    public static ReferenceWorldRemotePointerSelection PointerSelection(
        bool pointerOverUi,
        int localFaction,
        int remoteFaction,
        bool openWorldHostile)
    {
        if (pointerOverUi)
        {
            return ReferenceWorldRemotePointerSelection.None;
        }

        return IsFriendly(localFaction, remoteFaction, openWorldHostile)
            ? ReferenceWorldRemotePointerSelection.FriendlySelection
            : ReferenceWorldRemotePointerSelection.PlayerVsPlayerSelection;
    }
}

/// <summary>
/// Decision-free independent-runtime projection from the current remote actor to ISelectable.
/// Friendliness is supplied by the already-computed pointer branch.
/// </summary>
internal sealed class ReferenceWorldRemoteSelectable : ISelectable
{
    private readonly ReferenceWorldRemotePlayerActor actor;
    private readonly bool friendly;
    // Keep the CLR reference even after Unity destruction, matching the original type test.
    internal RemotePlayer SourceIdentityForIndependentRuntime { get; }

    public ReferenceWorldRemoteSelectable(
        ReferenceWorldRemotePlayerActor actor,
        bool friendly)
    {
        this.actor = actor ?? throw new ArgumentNullException(nameof(actor));
        this.friendly = friendly;
        SourceIdentityForIndependentRuntime = actor.SourceIdentityForIndependentRuntime;
    }

    public Transform SelTransform
    {
        get { return actor.Root; }
    }

    public string SelName
    {
        get { return actor.State.DisplayName; }
    }

    public int SelLevel
    {
        get { return actor.State.Level; }
    }

    public int SelHp
    {
        get { return actor.State.HitPoints; }
    }

    public int SelMaxHp
    {
        get { return actor.State.MaximumHitPoints; }
    }

    public bool SelFriendly
    {
        get { return friendly; }
    }

    public int SelOwnerId
    {
        get { return actor.State.EntityId; }
    }

    public Vector3 SelFeetPos
    {
        get
        {
            Transform value = actor.Root;
            return value ? value.position : Vector3.zero;
        }
    }
}

/// <summary>
/// Migration-only pointer/HUD/combat facade over the actual TargetSelect/TargetRing owners.
/// </summary>
/// <remarks>
/// A hostile remote result remains an intent. CombatWorld.SetPvpTarget's attack-state mutation
/// belongs to the later combat owner and is deliberately not fabricated here.
/// </remarks>
internal sealed class ReferenceWorldTargetSelection : IDisposable
{
    private ReferenceWorldRemotePlayerActor current;
    // The object TargetSelect keeps for the remote arm. It is the clicked RemotePlayer for entries
    // that run an official owner (06000b32 / 0600014c) and the migration wrapper only for the
    // remaining product-only direct selection; one object per selection is compared by reference.
    private ISelectable currentRemoteSelection;
    private ReferenceWorldMonsterActor currentMonster;
    private bool currentFriendly;
    private PlayerController currentSelf;
    private ReferenceWorldNpcActor currentNpc;
    private bool disposed;

    public ReferenceWorldRemotePlayerActor Current => current;
    public ReferenceWorldMonsterActor CurrentMonster => currentMonster;
    public bool CurrentFriendly => TargetSelect.Current is RemotePlayer remote && remote != null ? remote.SelFriendly : currentNpc != null || currentSelf != null || (current != null || currentMonster != null) && currentFriendly;
    internal PlayerController CurrentSelf => currentSelf;
    public int SelectedEntityId => TargetSelect.Current is RemotePlayer remote && remote != null ? remote.id : currentNpc != null ? currentNpc.State.EntityId : currentSelf != null ? NetClient.SelfEntityId : current != null
        ? current.State.EntityId
        : currentMonster == null ? 0 : currentMonster.State.EntityId;
    public int SelectedMonsterEntityId => currentMonster == null
        ? 0
        : currentMonster.State.EntityId;

    // Observation only: the original caller already selected the exact native object.
    // Missing optional views do not reject that selection or cause another native write.
    internal void AdoptCurrentNativeSelectionForIndependentRuntime(
        IReadOnlyDictionary<int, ReferenceWorldRemotePlayerActor> remotes,
        IReadOnlyDictionary<int, ReferenceWorldMonsterActor> monsters,
        IReadOnlyDictionary<int, ReferenceWorldNpcActor> npcs)
    {
        ThrowIfDisposed();
        ISelectable selected = TargetSelect.Current;
        ResetSnapshots();
        if (selected is PlayerController self) { currentSelf = self; currentFriendly = true; }
        else if (selected is RemotePlayer remote)
        {
            currentRemoteSelection = remote;
            if (remotes.TryGetValue(remote.id, out ReferenceWorldRemotePlayerActor view) &&
                ReferenceEquals(view.SourceIdentityForIndependentRuntime, remote)) current = view;
        }
        else if (selected is MonsterEntity monster)
        {
            currentFriendly = monster.friendly;
            if (monsters.TryGetValue(monster.id, out ReferenceWorldMonsterActor view) &&
                ReferenceEquals(view.SelectableOwner, monster)) currentMonster = view;
        }
        else if (selected is NpcInteract npc)
        {
            currentFriendly = true;
            foreach (ReferenceWorldNpcActor view in npcs.Values)
                if (ReferenceEquals(view.SelectableOwner, npc)) { currentNpc = view; break; }
        }
    }

    internal bool TrySelectSelf(PlayerController player)
    {
        ThrowIfDisposed();
        TargetSelect.Select(player);
        if (!ReferenceEquals(TargetSelect.Current, player)) { ResetSnapshots(); return false; }
        currentNpc = null;
        current = null;
        currentRemoteSelection = null;
        currentMonster = null;
        currentFriendly = true;
        currentSelf = player;
        return true;
    }

    internal bool TrySelectNpc(ReferenceWorldNpcActor actor)
    {
        ThrowIfDisposed();
        NpcInteract owner = actor == null ? null : actor.SelectableOwner;
        TargetSelect.Select(owner);
        ResetSnapshots();
        if (owner == null || !ReferenceEquals(TargetSelect.Current, owner)) return false;
        currentNpc = actor;
        currentFriendly = true;
        return true;
    }

    public bool TrySelect(
        ReferenceWorldRemotePlayerActor actor,
        int localFaction,
        bool openWorldHostile)
    {
        ThrowIfDisposed();
        if (actor == null || actor.Root == null)
        {
            Clear();
            return false;
        }

        bool friendly = ReferenceWorldTargetSelectionMath.IsFriendly(
            localFaction,
            actor.State.Faction,
            openWorldHostile);
        // This carrier only Selects the product wrapper and has no Runtime caller; it is not the two
        // official arms. This round removed just its mirrored pre-clear, which neither b32 arm has.
        ReferenceWorldRemoteSelectable selectable =
            new ReferenceWorldRemoteSelectable(actor, friendly);
        TargetSelect.Select(selectable);
        if (!ReferenceEquals(TargetSelect.Current, selectable))
        {
            ResetSnapshots();
            return false;
        }

        currentNpc = null;
        currentSelf = null;
        current = actor;
        currentRemoteSelection = selectable;
        currentMonster = null;
        currentFriendly = friendly;
        return true;
    }

    public bool TrySelect(ReferenceWorldMonsterActor actor)
    {
        ThrowIfDisposed();
        MonsterEntity owner = actor == null ? null : actor.SelectableOwner;
        if (actor == null || actor.RootObject == null || !owner)
        {
            Clear();
            return false;
        }

        TargetSelect.Select(owner);
        if (!ReferenceEquals(TargetSelect.Current, owner))
        {
            ResetSnapshots();
            return false;
        }

        CombatWorld.SetTargetForIndependentRuntime(owner);
        currentNpc = null;
        currentSelf = null;
        current = null;
        currentRemoteSelection = null;
        currentMonster = actor;
        currentFriendly = false;
        return true;
    }

    // Observe the result of the recovered MonsterEntity.OnMouseDown primary-click owner. That
    // method has already selected the target and, for a hostile monster, set CombatWorld state.
    internal bool ObserveMonsterClickForIndependentRuntime(ReferenceWorldMonsterActor actor)
    {
        ThrowIfDisposed();
        MonsterEntity owner = actor == null ? null : actor.SelectableOwner;
        if (!owner || !ReferenceEquals(TargetSelect.Current, owner))
        {
            ResetSnapshots();
            return false;
        }

        currentNpc = null;
        currentSelf = null;
        current = null;
        currentRemoteSelection = null;
        currentMonster = actor;
        currentFriendly = owner.NoAttack;
        return true;
    }

    // Official PlayerController.Update short right-click branch (0x06000a00, arm64 :1401-1423 /
    // x86_64 :1344-1366), the real user entry for one short right click on a picked MonsterEntity:
    // after the GetMouseButtonUp(1) release, the +0x1bc hold threshold and the 8-unit move limit, the
    // PlayerActionMenu.get_Visible (0x06000971) and CombatWorld.get_AwaitingTarget (0x06000150)
    // guards, PlayerController.PickSelectable (0x060009fb) runs again and its result takes one of
    // three arms - null goes to CombatWorld.ClearTarget (0x0600014d), a result that is not a
    // MonsterEntity (TypeInfo metadata_usage_02058, exact in the 1.5.8 atlas) or a MonsterEntity
    // whose instance byte +0x136 is set goes to TargetSelect.Select (0x06000e54), and every other
    // MonsterEntity goes to CombatWorld.SetTarget (0x0600014b) with attack = false.
    // +0x136 is MonsterEntity.friendly: field token 0x040008cc, System.Boolean, offset 310 (0x136)
    // in the 1.5.8 atlas field table; it is the byte MonsterEntity.Init stores from its own
    // `friendly` argument (MonsterEntity.Init.c: *(char *)(param_6 + 0x136) = (char)param_13) and the
    // byte MonsterEntity.Update and the other MonsterEntity owners read as their friendly test.
    // It is deliberately not MonsterEntity.NoAttack (friendly || ally): an ally-only monster takes
    // the SetTarget arm here while MonsterEntity.OnMouseDown sends the same monster to Select.
    // CombatWorld.SetTarget is the one owner of target/pvpTarget/autoAttacking/atkState/atkPhaseT
    // and ends with the single TargetSelect.Select of this click, so this host must not Select first
    // and must not bind a second object afterwards: TargetSelect.Current, the target ring, the HUD
    // snapshot and TryTick all close on the MonsterEntity itself. The right click also starts no
    // attack - attack = false is official - and never moves the local player.
    internal bool TrySelectMonsterIdentityForIndependentRuntime(ReferenceWorldMonsterActor actor)
    {
        ThrowIfDisposed();
        MonsterEntity owner = actor == null || actor.RootObject == null
            ? null
            : actor.SelectableOwner;
        if (!owner)
        {
            return false;
        }

        if (owner.friendly)
        {
            TargetSelect.Select(owner);
        }
        else
        {
            CombatWorld.SetTarget(owner, false);
        }

        if (!ReferenceEquals(TargetSelect.Current, owner))
        {
            ResetSnapshots();
            return false;
        }

        currentNpc = null;
        currentSelf = null;
        current = null;
        currentRemoteSelection = null;
        currentMonster = actor;
        currentFriendly = owner.friendly;
        return true;
    }

    public bool TryHandlePointerDown(
        ReferenceWorldRemotePlayerActor actor,
        bool pointerOverUi,
        int localFaction,
        bool openWorldHostile,
        out ReferenceWorldRemotePointerSelection result)
    {
        ThrowIfDisposed();
        result = actor == null
            ? ReferenceWorldRemotePointerSelection.None
            : ReferenceWorldTargetSelectionMath.PointerSelection(
                pointerOverUi,
                localFaction,
                actor.State.Faction,
                openWorldHostile);
        if (result == ReferenceWorldRemotePointerSelection.None)
        {
            return actor != null;
        }

        return TrySelectRemoteIdentity(actor);
    }

    // Explicit diagnostic entry only. The live component receives Unity's OnMouseDown itself;
    // this retained entry invokes that same complete body, then observes its selected identity.
    // No hostility decision or second Select/SetPvpTarget is owned by this projection.
    private bool TrySelectRemoteIdentity(ReferenceWorldRemotePlayerActor actor)
    {
        ThrowIfDisposed();
        if (actor == null || actor.Root == null)
        {
            Clear();
            return false;
        }

        // The official entry passes the component that owns the collider; a product actor without it
        // cannot express "this" of that body, so the click reports failure instead of selecting a
        // stand-in object. The layout and prior behaviour of that case are unchanged.
        RemotePlayer identity = actor.SourceIdentityForIndependentRuntime;
        if (identity == null)
        {
            Clear();
            return false;
        }

        identity.OnMouseDown();

        if (!ReferenceEquals(TargetSelect.Current, identity))
        {
            ResetSnapshots();
            return false;
        }

        currentNpc = null;
        currentSelf = null;
        current = actor;
        currentRemoteSelection = identity;
        currentMonster = null;
        currentFriendly = identity.SelFriendly;
        return true;
    }

    // Official PlayerController.Update short right-click branch (0x06000a00, arm64 :1382-1417 /
    // x86_64 :1328-1360), the real user entry for one short right click on a remote player: after
    // the GetMouseButtonUp(1) release, the +0x1bc hold threshold and the 8-unit move limit, the
    // PlayerActionMenu.get_Visible (0x06000971) and CombatWorld.get_AwaitingTarget (0x06000150)
    // guards, this branch calls PlayerController.PickSelectable (0x060009fb) again and sends a
    // result that is not a MonsterEntity straight to TargetSelect.Select (0x06000e54).
    // This is deliberately not RemotePlayer.OnMouseDown (0x06000b32): neither
    // CombatWorld.SetPvpTarget (0x0600014c) nor CombatWorld.ClearTarget (0x0600014d) is reachable
    // for a picked target - ClearTarget sits only in the arm where PickSelectable returned null
    // (arm64 :1409 / x86_64 :1352) - so a right click selects the clicked RemotePlayer itself and
    // leaves the attack state and any monster target alone. The previous monster target is also
    // not cleared here; the writers stay SetTarget (0x0600014b) and SetPvpTarget (0x0600014c),
    // and the hostile left arm does null it - inside SetPvpTarget, not by an extra clear.
    internal bool TrySelectRemoteIdentityForIndependentRuntime(
        ReferenceWorldRemotePlayerActor actor)
    {
        ThrowIfDisposed();
        RemotePlayer identity = actor == null || actor.Root == null
            ? null
            : actor.SourceIdentityForIndependentRuntime;
        if (identity == null)
        {
            Clear();
            return false;
        }

        TargetSelect.Select(identity);
        if (!ReferenceEquals(TargetSelect.Current, identity))
        {
            ResetSnapshots();
            return false;
        }

        currentNpc = null;
        currentSelf = null;
        current = actor;
        currentRemoteSelection = identity;
        currentMonster = null;
        currentFriendly = !identity.HostileToLocal;
        return true;
    }

    public void OnGone(ReferenceWorldRemotePlayerActor actor, bool observationOnly = false)
    {
        ThrowIfDisposed();
        if (!ReferenceEquals(current, actor))
        {
            return;
        }

        if (!observationOnly) TargetSelect.OnGone(currentRemoteSelection);
        ResetSnapshots();
    }

    public void OnGone(ReferenceWorldMonsterActor actor, bool observationOnly = false)
    {
        ThrowIfDisposed();
        if (!ReferenceEquals(currentMonster, actor))
        {
            return;
        }

        if (!observationOnly)
        {
            MonsterEntity owner = currentMonster.SelectableOwner;
            CombatWorld.ClearTargetForIndependentRuntime(owner);
            TargetSelect.OnGone(owner);
        }
        ResetSnapshots();
    }

    /// <summary>
    /// Host half of the official empty-pick right-click arm after CombatWorld.ClearTarget ran.
    /// </summary>
    /// <remarks>
    /// CombatWorld.ClearTarget (0x0600014d) already performed every official write of that arm and
    /// its single TargetSelect.Clear. This only drops the projection this host keeps for the target
    /// HUD and the approach plan and writes no CombatWorld, TargetSelect or TargetRing state, so the
    /// clear itself stays single-sided and is not repeated here.
    /// </remarks>
    internal void AdoptOfficialTargetClearForIndependentRuntime()
    {
        ThrowIfDisposed();
        ResetSnapshots();
    }

    public void Clear()
    {
        MonsterEntity owner = currentMonster == null
            ? null
            : currentMonster.SelectableOwner;
        if (owner)
        {
            CombatWorld.ClearTargetForIndependentRuntime(owner);
        }

        TargetSelect.Clear();
        ResetSnapshots();
    }

    public ReferenceFirstWorldTargetHudSnapshot CreateHudSnapshot(int localPlayerLevel)
    {
        ThrowIfDisposed();
        // Project the actual native selection; the HUD must not reselect through a legacy wrapper.
        RemotePlayer remote = TargetSelect.Current as RemotePlayer;
        if (remote != null)
            return new ReferenceFirstWorldTargetHudSnapshot(remote.id, remote.SelName,
                remote.SelLevel, remote.SelHp, remote.SelMaxHp, remote.SelFriendly,
                remote.SelFriendly, true, localPlayerLevel);
        // The HUD still projects an already selected native owner when an optional view is
        // unavailable. These are the same fields used below; no selection/query is performed here.
        if (currentNpc == null && TargetSelect.Current is NpcInteract nativeNpc && nativeNpc != null)
            return new ReferenceFirstWorldTargetHudSnapshot(0, nativeNpc.SelName, nativeNpc.SelLevel,
                nativeNpc.SelHp, nativeNpc.SelMaxHp, true, true, false, localPlayerLevel, queryTargetStatus: false);
        if (currentMonster == null && TargetSelect.Current is MonsterEntity nativeMonster && nativeMonster != null)
            return new ReferenceFirstWorldTargetHudSnapshot(nativeMonster.id, nativeMonster.SelName,
                nativeMonster.level, nativeMonster.hp, nativeMonster.maxHp, nativeMonster.friendly,
                nativeMonster.petOwnerId == 0 || nativeMonster.friendly, false, localPlayerLevel);
        if (currentNpc != null && currentNpc.SelectableOwner != null)
        {
            NpcInteract npc = currentNpc.SelectableOwner;
            return new ReferenceFirstWorldTargetHudSnapshot(currentNpc.State.EntityId, npc.SelName,
                npc.SelLevel, npc.SelHp, npc.SelMaxHp, true, true, false, localPlayerLevel, queryTargetStatus: false);
        }
        if (currentSelf != null)
            return new ReferenceFirstWorldTargetHudSnapshot(NetClient.SelfEntityId, currentSelf.SelName,
                currentSelf.SelLevel, currentSelf.SelHp, currentSelf.SelMaxHp, true, true, false, localPlayerLevel, queryTargetStatus: false);
        if (current == null || current.Root == null)
        {
            if (currentMonster == null || currentMonster.RootObject == null)
            {
                return null;
            }

            WorldEntityState monster = currentMonster.State;
            bool useNeutralFrame = monster.OwnerEntityId == 0 ||
                                   monster.FriendlyFlag != 0;
            return new ReferenceFirstWorldTargetHudSnapshot(
                monster.EntityId,
                monster.DisplayName,
                monster.Level,
                monster.HitPointsOrRadiusPixels,
                monster.MaximumHitPoints,
                monster.FriendlyFlag != 0,
                useNeutralFrame,
                false,
                localPlayerLevel);
        }

        WorldRemotePlayerState state = current.State;
        return new ReferenceFirstWorldTargetHudSnapshot(
            state.EntityId,
            state.DisplayName,
            state.Level,
            state.HitPoints,
            state.MaximumHitPoints,
            currentFriendly,
            currentFriendly,
            true,
            localPlayerLevel);
    }

    public bool TryTick()
    {
        ThrowIfDisposed();
        if (currentNpc != null &&
            !ReferenceEquals(TargetSelect.Current, currentNpc.SelectableOwner))
        {
            currentNpc = null;
            currentFriendly = false;
        }

        if (!ReferenceEquals(currentSelf, null) &&
            !ReferenceEquals(TargetSelect.Current, currentSelf))
        {
            currentSelf = null;
            currentFriendly = false;
        }

        if (currentRemoteSelection != null &&
            !ReferenceEquals(TargetSelect.Current, currentRemoteSelection))
        {
            current = null;
            currentRemoteSelection = null;
            currentFriendly = false;
        }

        if (currentMonster != null &&
            !ReferenceEquals(TargetSelect.Current, currentMonster.SelectableOwner))
        {
            // Observing TargetSelect changes only invalidates this HUD projection. Native
            // SelectPlayer and its caller's Clear do not clear the combat monster target.
            currentMonster = null;
        }

        return true;
    }

    public void Dispose()
    {
        if (disposed) return;
        MonsterEntity monster = currentMonster == null ? null : currentMonster.SelectableOwner;
        if (monster) CombatWorld.ClearTargetForIndependentRuntime(monster);
        TargetSelect.OnGone(currentRemoteSelection);
        TargetSelect.OnGone(monster);
        TargetSelect.OnGone(currentSelf);
        TargetSelect.OnGone(currentNpc == null ? null : currentNpc.SelectableOwner);
        ResetSnapshots();
        disposed = true;
    }

    private void ResetSnapshots()
    {
        currentNpc = null;
        currentSelf = null;
        current = null;
        currentRemoteSelection = null;
        currentMonster = null;
        currentFriendly = false;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceWorldTargetSelection));
        }
    }
}

}
