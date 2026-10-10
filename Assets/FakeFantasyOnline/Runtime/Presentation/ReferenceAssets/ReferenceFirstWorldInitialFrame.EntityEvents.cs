using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class ReferenceFirstWorldInitialFrame
{
    private bool entityReceiveEnabled;
    private bool nativeCombatReceiveEnabled;

    private bool MatchesEntityReceiveIncarnation(int id, long incarnation, long receiveSequence) =>
        incarnation != 0 && entityReceiveIncarnations.TryGetValue(id, out long current) && current == incarnation &&
        entityReceiveBirthSequences.TryGetValue(id, out long bornAt) && receiveSequence >= bornAt;

    private readonly Dictionary<int, long> entityReceiveBirthSequences = new Dictionary<int, long>();
    private readonly Dictionary<int, long> entityReceiveIncarnations = new Dictionary<int, long>();

    private void RemoveEntityReceiveActor(int id, byte reason = 0, bool observationOnly = false)
    {
        if (monsterActors.TryGetValue(id, out var actor))
        {
            targetSelection?.OnGone(actor, observationOnly);
            if (!observationOnly) CombatWorld.Remove(id, reason);
            actor.Dispose();
            monsterActors.Remove(id);
        }
        entityReceiveIncarnations.Remove(id);
        entityReceiveBirthSequences.Remove(id);
        appliedMonsterPaths.Remove(id);
        terminalWorldEntityTombstones.Remove(id);
    }

    private bool ApplyEntityReceiveEvent(WorldRemoteEvent edge, float time)
    {
        if (edge.Entity != null && edge.Entity.Kind == WorldEntityKind.Trap)
        {
            if (edge.Kind == WorldRemoteEventKind.EntitySpawn)
            {
                if (!edge.NativeRegistryHandled)
                    CombatWorld.SpawnTrap(edge.EntityId, edge.Entity.ModelOrSkillId,
                        edge.Entity.X, edge.Entity.Y, edge.Entity.HitPointsOrRadiusPixels);
                return true;
            }
            if (edge.Kind == WorldRemoteEventKind.EntityRemove)
            {
                if (!edge.NativeRegistryHandled) CombatWorld.Remove(edge.EntityId, edge.RemoveReason);
                return true;
            }
        }
        if (edge.Kind == WorldRemoteEventKind.EntitySpawn)
        {
            // The original generic dispatcher routes every nontrap kind through the same owner.
            // Spawn owns duplicate membership and pet admission; replay adds no kind/model/map gate.
            if (edge.Entity == null) return false;
            if (!edge.NativeRegistryHandled)
                CombatWorld.Spawn(edge.EntityId, edge.Entity.ModelOrSkillId, edge.Entity.DisplayName,
                    edge.Entity.Level, edge.Entity.X, edge.Entity.Y, edge.Entity.HitPointsOrRadiusPixels,
                    edge.Entity.MaximumHitPoints, (int)edge.Entity.Kind, edge.Entity.OwnerEntityId,
                    edge.Entity.EliteFlags, edge.Entity.FriendlyFlag);
            MonsterEntity owner = edge.NativeRegistryHandled
                ? edge.ReceivedIdentity as MonsterEntity
                : CombatWorld.MonsterForIndependentRuntime(edge.EntityId);
            if (!owner || (edge.NativeRegistryHandled &&
                !ReferenceEquals(CombatWorld.MonsterForIndependentRuntime(edge.EntityId), owner))) return true;
            if (monsterActors.TryGetValue(edge.EntityId, out var previous))
            {
                if (ReferenceEquals(previous.SelectableOwner, owner))
                {
                    entityReceiveIncarnations[edge.EntityId] = edge.Incarnation;
                    entityReceiveBirthSequences[edge.EntityId] = edge.Sequence;
                    return true;
                }
                targetSelection?.OnGone(previous, edge.NativeRegistryHandled);
                previous.Dispose();
                monsterActors.Remove(edge.EntityId);
            }
            var next = new ReferenceWorldMonsterActor(map, resources, root, edge.Entity,
                remoteFeedbackAssets, camera, character.Level, additiveMaterial);
            try
            {
                if (!next.Build(owner)) return false;
                monsterActors.Add(edge.EntityId, next);
                entityReceiveIncarnations[edge.EntityId] = edge.Incarnation;
                entityReceiveBirthSequences[edge.EntityId] = edge.Sequence;
                next = null;
                return true;
            }
            finally
            {
                if (next != null)
                {
                    if (monsterActors.TryGetValue(edge.EntityId, out var published) &&
                        ReferenceEquals(published, next)) monsterActors.Remove(edge.EntityId);
                    if (!edge.NativeRegistryHandled)
                        combatWorld?.UnregisterMonsterForIndependentRuntime(next.SelectableOwner);
                    entityReceiveIncarnations.Remove(edge.EntityId);
                    entityReceiveBirthSequences.Remove(edge.EntityId);
                    next.Dispose();
                }
            }
        }
        if (edge.Kind == WorldRemoteEventKind.EntityOwner)
        {
            // Receipt-bound packets already called the original owner against the registry
            // that existed then. Legacy detached inputs keep their ordered application route.
            if (!edge.NativeRegistryHandled) CombatWorld.SetOwner(edge.EntityId, edge.OwnerId);
            return true;
        }
        if (!entityReceiveIncarnations.TryGetValue(edge.EntityId, out long current) ||
            current != edge.Incarnation || !monsterActors.TryGetValue(edge.EntityId, out var actor))
            return true;
        switch (edge.Kind)
        {
            case WorldRemoteEventKind.EntityRemove:
                if (!edge.NativeRegistryHandled ||
                    !ReferenceEquals(CombatWorld.MonsterForIndependentRuntime(edge.EntityId), actor.SelectableOwner))
                    RemoveEntityReceiveActor(edge.EntityId, edge.RemoveReason, edge.NativeRegistryHandled);
                return true;
            case WorldRemoteEventKind.EntityPath:
                return actor.TryApplyPath(edge.Path);
            case WorldRemoteEventKind.EntityStatusFx:
                if (edge.NativeRegistryHandled) actor.ObserveStatusEventForIndependentRuntime(edge.Entity);
                else actor.ApplyStatusEffectEvent(edge.StatusEffect, edge.Entity);
                return true;
            case WorldRemoteEventKind.EntityStatusIcons:
                if (edge.NativeRegistryHandled) actor.ObserveStatusEventForIndependentRuntime(edge.Entity);
                else actor.ApplyStatusIconsEvent(edge.Entity);
                return true;
            case WorldRemoteEventKind.EntityHp:
                actor.ApplyHitPointsEvent(edge.HitPoints.Value);
                return true;
            default:
                return false;
        }
    }
}

}
