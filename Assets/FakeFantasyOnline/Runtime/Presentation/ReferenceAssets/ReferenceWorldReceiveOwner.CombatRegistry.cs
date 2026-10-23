using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : ICombatRegistryReceiveOwner
{
    bool IWorldReceiveContext.Switching => GameBootstrap.I != null && GameBootstrap.I.Switching;
    int IWorldReceiveContext.CurrentMapId => GameBootstrap.CurMapId;

    public object ReceiveRemotePlayer(WorldRemotePlayerState value)
    {
        CharacterAppearanceRecord a = value.Appearance;
        CombatWorld.SpawnPlayer(value.EntityId, value.DisplayName, value.Title, value.Level,
            value.X, value.Y, value.Gender, value.Head, value.Hair, a.AttackVariant,
            a.Body, a.Cap, a.Backpack, a.RightWeapon, a.LeftWeapon, a.Face, value.Dead,
            value.AvatarAura, value.SoulEffects, value.Faction, value.HitPoints,
            value.MaximumHitPoints, value.TitleEffect, value.GlowMode);
        // Dispatch re-resolves the native registry after SpawnPlayer, separately before each of its three tails.
        RemotePlayer player = CombatWorld.FindPlayer(value.EntityId);
        if (!ReferenceEquals(player, null))
        {
            WorldRemoteRidingState riding = value.RidingState;
            player.SetRiding(riding.Riding, riding.MountModel, riding.MountTianmo,
                riding.RiderSeat, riding.RideAnchorId, riding.MountSeats);
        }
        player = CombatWorld.FindPlayer(value.EntityId);
        if (!ReferenceEquals(player, null)) player.SetInvis(value.Invisible);
        player = CombatWorld.FindPlayer(value.EntityId);
        if (!ReferenceEquals(player, null)) player.SetWeapon(a.WeaponCategory, a.AttackDistance, a.NativeAttackDistance);
        return player;
    }

    public object ReceiveWorldEntity(WorldEntityState value)
    {
        // These existing owners are the prerequisite for Remove's shared-registry order.
        if (value.Kind == WorldEntityKind.Trap)
        {
            CombatWorld.SpawnTrap(value.EntityId, value.ModelOrSkillId,
                value.X, value.Y, value.HitPointsOrRadiusPixels);
            return null;
        }
        // Dispatch prepares its icon list before Spawn. Complete this representation
        // conversion before native side effects too, rather than failing after a successful
        // Spawn or status-effect tail while constructing the final icon argument.
        var statusIcons = value.HasStatusIconsUpdate
            ? ReferenceWorldMonsterActor.ConvertStatusIcons(value.StatusIcons)
            : null;
        CombatWorld.Spawn(value.EntityId, value.ModelOrSkillId, value.DisplayName,
            value.Level, value.X, value.Y, value.HitPointsOrRadiusPixels,
            value.MaximumHitPoints, (int)value.Kind, value.OwnerEntityId,
            value.EliteFlags, value.FriendlyFlag);
        foreach (WorldEntityStatusEffect effect in value.StatusEffects)
            if (!string.IsNullOrEmpty(effect.Effect) && effect.DurationMilliseconds > 0)
                CombatWorld.ShowEntityStatus(value.EntityId, effect.Effect,
                    effect.PositionMode, effect.DurationMilliseconds);
        if (value.HasStatusIconsUpdate)
            CombatWorld.SetEntityStatusIcons(value.EntityId, statusIcons);
        return CombatWorld.MonsterForIndependentRuntime(value.EntityId);
    }

    public void ReceiveWorldRemoval(int id, byte reason, out bool trapPresent,
        out bool playerPresent, out bool monsterPresent)
    {
        CombatWorld.Remove(id, reason);
        CombatWorld.ObserveRegistryPresenceForIndependentRuntime(id,
            out trapPresent, out playerPresent, out monsterPresent);
    }

    public bool ReceiveRemotePk(int id, byte state)
    {
        if (id == NetClient.SelfEntityId)
        {
            CharData.SetLocalPkStateFromServer(state);
            return true;
        }
        CombatWorld.SetPlayerPkState(id, state);
        return false;
    }

    public void ReceiveRemotePlayerLife(int id, bool alive) => CombatWorld.PlayerDeadState(id, !alive);

    public PlayerLifeReceiveResult ReceivePlayerLife(int id, bool alive, byte flags)
    {
        PlayerController player = PlayerController.I;
        bool local = NetClient.HandleLocalPlayerLifeForIndependentRuntime(id, alive, flags);
        if (!local) CombatWorld.PlayerDeadState(id, !alive);
        return new PlayerLifeReceiveResult(local, local && player != null, NetClient.SelfEntityId);
    }
}
}
