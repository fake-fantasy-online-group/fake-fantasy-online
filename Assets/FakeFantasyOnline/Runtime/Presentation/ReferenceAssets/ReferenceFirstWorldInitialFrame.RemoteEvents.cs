using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class ReferenceFirstWorldInitialFrame
{
    private bool remoteReceiveEnabled;
    private bool remoteReceiveMapMatches;
    private long remoteReceiveGeneration = -1;
    private long remoteReceiveSequence;
    private readonly Dictionary<int, long> remoteIncarnations = new Dictionary<int, long>();
    private WorldPartyState remoteReceiveParty = WorldPartyState.Empty;
    private readonly Dictionary<int, WorldStallOwnerState> remoteReceiveStalls =
        new Dictionary<int, WorldStallOwnerState>();

    private WorldRemoteEventWindow PrepareWorldReceiveContext(ClientSessionCoordinator session, int nameMode)
    {
        WorldRemoteEventWindow receiveWindow = session.CaptureRemoteEvents();
        remoteReceiveEnabled = receiveWindow != null;
        entityReceiveEnabled = receiveWindow?.IncludesEntityEvents ?? false;
        nativeCombatReceiveEnabled = receiveWindow?.IncludesCombatEvents ?? false;
        CharData.nameMode = nameMode;
        hideOtherPets = GameSettings.HideOtherPets;
        // WBB-074: CharData.level (+0x20), CharData.hp (+0x58) and CharData.maxHp (+0x5c) have
        // exactly three official writers - TitleFlow.MirrorSlotToCharData (0x06000efb, the roster
        // preview at selection), TitleFlow.DoEnterWorld (0x06000efd) at entry and the 0x8007 case
        // of NetClient.Dispatch (0x0600077f) - and the bound receive owner
        // (ReferenceWorldReceiveOwner.ReceiveEnterAttributeFields / ReceivePlayerAttributeFields)
        // reproduces the last two with the official short-array gates: the entry stores nothing
        // at all for a null or empty attrs array and element 0 into level only for a length of
        // one to six, while 0x8007 stores nothing below seven. This frame used to re-project the
        // Application snapshot over the three statics on every receive pass; that projection has
        // no official store behind it and, because WorldPlayerStatusState.FromEntry turned an
        // absent index into 0 (WBB-075 keeps the value the official skip leaves there instead),
        // it pushed a 0 over the value the official skip keeps - the roster
        // level written by 0x06000efb when the entry array is null or empty, and the hp/maxHp a
        // 0x8007 had already stored when the entry array is shorter than seven. The projection is
        // removed so the receive owner stays the single writer and the official skip survives
        // instead of being undone one frame later.
        CharData.BindPkStateForIndependentRuntime(session.SelfPkState);
        WorldLocalFactionState localFaction = session.LocalFactionState;
        // Official 0x8044 writes CombatWorld.LocalFaction inside the case body, and the static
        // is never reset on world exit, so a session that never received 0x8044 must not push a
        // 0 over a value an earlier session already received. The projection therefore only
        // re-states a snapshot that exists; the receive-time write in PrivateServerGateway is
        // what sets the owner, with or without a built world frame.
        if (localFaction != null)
        {
            CombatWorld.LocalFaction = localFaction.Faction;
        }

        return receiveWindow;
    }

    internal bool ApplyPublishedWorldEventsForIndependentRuntime(ClientSessionCoordinator session, int nameMode)
    {
        if (disposed || !built) return true;
        WorldRemoteEventWindow window = PrepareWorldReceiveContext(session, nameMode);
        if (window == null) return true;
        if (!ApplyRemoteReceiveWindow(session, window, Time.time))
        {
            LastRevisionFailure = ReferenceWorldRevisionFailure.CaptureRejected;
            return false;
        }
        return true;
    }

    private void ApplyLocalRename(WorldPlayerRenameState rename)
    {
        if (rename == null) return;
        CharData.name = rename.Name;
        playerNameTag?.ApplyReceivedNameForIndependentRuntime(rename.Name);
    }

    private void BindRemoteSocialInputs()
    {
        // SocialClient party roots already belong to the actual 8033 receipt.
        StallDlg.BindOwnersForIndependentRuntime(remoteReceiveStalls);
    }

    // Hands one authoritative 0x8018 path to the official owner for an id that has no receive
    // actor. CombatWorld.MovePath classifies the id itself; nothing here decides what it is.
    // The gateway appends this edge before WithRevision runs, so value.Revision is 0 and no
    // snapshot dictionary holds it: the single application of one such message comes from the
    // window sequence test (the continue at 180, the gap rejection at 181 and the assignment at
    // 312), not from a revision.
    private void ApplyAuthoritativePathForIndependentRuntime(WorldRemotePathState value)
    {
        if (value == null || value.NativeMovementHandled)
        {
            return;
        }

        int[] xs = new int[value.Points.Count];
        int[] ys = new int[value.Points.Count];
        for (int index = 0; index < xs.Length; index++)
        {
            xs[index] = value.Points[index].X;
            ys[index] = value.Points[index].Y;
        }

        CombatWorld.MovePath(value.EntityId, xs, ys, value.Speed);
    }

    private void RemoveRemoteReceiveActor(int id, byte reason = 0, bool observationOnly = false)
    {
        ReferenceWorldRemotePlayerActor actor;
        if (remoteActors.TryGetValue(id, out actor))
        {
            targetSelection?.OnGone(actor, observationOnly);
            if (!observationOnly) CombatWorld.Remove(id, reason);
            actor.Dispose();
            remoteActors.Remove(id);
        }
        remoteIncarnations.Remove(id);
        appliedRemotePaths.Remove(id);
        appliedRemoteActions.Remove(id);
    }

    private bool AddRemoteReceiveActor(WorldRemotePlayerState state, long incarnation, float time)
    {
        ReferenceWorldRemotePlayerActor actor = new ReferenceWorldRemotePlayerActor(
            map, resources, root, state, remoteFeedbackAssets);
        if (!actor.Build(time, true))
        {
            actor.Dispose();
            return false;
        }
        // Native SpawnPlayer has already published and applied its optional death arm. This
        // dictionary stores only the observer, and no catch may roll back the native identity.
        remoteActors.Add(state.EntityId, actor);
        remoteIncarnations[state.EntityId] = incarnation;
        actor.ApplySpawnDispatchTail(state);
        return true;
    }

    private bool ApplyRemoteReceiveWindow(ClientSessionCoordinator session, WorldRemoteEventWindow window, float time)
    {
        remoteReceiveMapMatches = window.MapId <= 0 || window.MapId == character.MapId;
        if (!remoteReceiveMapMatches) return true;
        // Lifecycle wiring only: CombatWorld.MovePath and CombatWorld.Remove resolve their targets
        // through the static CombatWorld.I, so the frame that consumes this window must be the one
        // CombatWorld.I points at. The ordinary local-movement branch
        // (ReferenceFirstWorldInitialFrame.cs:1667) is not always reached first - sitting, riding,
        // an immobile state, death, work or a map change all skip it - so the activation cannot
        // depend on it. This only re-points the existing owner; no second CombatWorld is created
        // and no classification decision moves into the protocol layer.
        combatWorld?.ActivateForIndependentRuntime();
        if (window.Generation != remoteReceiveGeneration)
        {
            // A receive checkpoint does not contain native clocks, retained pose matrices or
            // Life/Path/Action execution state. A fresh native consumer must start at the scene
            // boundary; never fabricate its state by replaying latest-value checkpoint rows.
            if (window.BaseSequence != 0) return false;
            foreach (int id in new List<int>(remoteActors.Keys))
                RemoveRemoteReceiveActor(id, observationOnly: window.NativeRegistryAtReceipt);
            if (window.IncludesEntityEvents)
            {
                if (!window.NativeRegistryAtReceipt) combatWorld?.ReleaseTrapsForIndependentRuntime();
                foreach (int id in new List<int>(monsterActors.Keys))
                    RemoveEntityReceiveActor(id, observationOnly: window.NativeRegistryAtReceipt);
                entityReceiveIncarnations.Clear();
                entityReceiveBirthSequences.Clear();
            }
            if (window.IncludesNpcEvents)
            {
                // Native scene/reset owns all roots. A protocol-generation change only
                // refreshes this frame's borrowed views; it cannot destroy native objects.
                ObserveNativeNpcsForIndependentRuntime();
            }
            ApplyLocalRename(window.Baseline.LocalRename);
            remoteReceiveParty = window.Baseline.Party;
            remoteReceiveStalls.Clear();
            foreach (var pair in window.Baseline.Stalls) remoteReceiveStalls[pair.Key] = pair.Value;
            BindRemoteSocialInputs();
            remoteReceiveGeneration = window.Generation;
            remoteReceiveSequence = window.BaseSequence;
            lastPartyStateVersion = -1;
            lastStallOwnersVersion = -1;
        }
        if (remoteReceiveSequence < window.BaseSequence) return false;
        BindRemoteSocialInputs();
        foreach (WorldRemoteEvent edge in window.Events)
        {
            if (edge.Sequence <= remoteReceiveSequence) continue;
            if (edge.Sequence != remoteReceiveSequence + 1) return false;
            if (edge.Kind == WorldRemoteEventKind.DuelHud)
            {
                if (edge.DuelHud == null) return false;
                // Receipt already owns Apply/Clear and scene-root creation. This edge only
                // advances observation order, including for an unbound diagnostic gateway.
            }
            else if (edge.Kind == WorldRemoteEventKind.ChatBubble)
            {
                // Receipt already used the actual owner, target and clock. Never replay it.
                if (!edge.NativeRegistryHandled)
                {
                    combatWorld?.ActivateForIndependentRuntime();
                    CombatWorld.ShowPlayerBubble(edge.EntityId, edge.BubbleText, edge.BubbleTheme);
                }
            }
            else if (edge.Kind == WorldRemoteEventKind.EntityBubble)
            {
                // Receipt already used the actual owner, target and clock. Never replay it.
                if (!edge.NativeRegistryHandled)
                {
                    combatWorld?.ActivateForIndependentRuntime();
                    CombatWorld.ShowBubble(edge.EntityId, edge.BubbleText);
                }
            }
            else if (edge.Kind == WorldRemoteEventKind.Emote)
            {
                // Receipt already used the actual owner, target and clock. Never replay it.
                if (!edge.NativeRegistryHandled)
                {
                    combatWorld?.ActivateForIndependentRuntime();
                    CombatWorld.ShowEmote(edge.EntityId, edge.EmoteFace.Value);
                }
            }
            else if (edge.Kind == WorldRemoteEventKind.Combat)
            {
                if (!window.IncludesCombatEvents || edge.Combat == null) return false;
                WorldCombatResultState combat = edge.Combat;
                if (!combat.NativeCombatHandled)
                    CombatWorld.OnCombat(combat.AttackerEntityId, combat.TargetEntityId,
                        combat.Damage, combat.Flags, combat.RawTargetHitPoints);
                if (remoteActors.TryGetValue(combat.TargetEntityId, out var remoteTarget))
                    remoteTarget.ObserveCombatStateForIndependentRuntime();
                if (monsterActors.TryGetValue(combat.TargetEntityId, out var monsterTarget))
                    monsterTarget.ObserveCombatStateForIndependentRuntime();
            }
            else if (edge.Kind is WorldRemoteEventKind.EntitySpawn or WorldRemoteEventKind.EntityRemove or
                WorldRemoteEventKind.EntityPath or WorldRemoteEventKind.EntityStatusFx or
                WorldRemoteEventKind.EntityStatusIcons or WorldRemoteEventKind.EntityHp or
                // Receipt-bound 0x8043 is observation-only; detached inputs retain ordered replay.
                WorldRemoteEventKind.EntityOwner)
            {
                if (!window.IncludesEntityEvents || !ApplyEntityReceiveEvent(edge, time)) return false;
            }
            else if (edge.Kind == WorldRemoteEventKind.NpcSpawn ||
                edge.Kind == WorldRemoteEventKind.NpcRemove)
            {
                // The receipt owner already performed the native operation; observe it once.
                if (!window.IncludesNpcEvents || !ApplyNpcReceiveEvent(edge)) return false;
            }
            else if (edge.Kind == WorldRemoteEventKind.Party)
            {
                remoteReceiveParty = edge.Party;
                // Observe the receipt; do not write the native static lists again.
            }
            else if (edge.Kind == WorldRemoteEventKind.Stall)
            {
                if (edge.Stall == null) remoteReceiveStalls.Remove(edge.EntityId);
                else remoteReceiveStalls[edge.EntityId] = edge.Stall;
                StallDlg.BindOwnersForIndependentRuntime(remoteReceiveStalls);
            }
            else if (edge.Kind == WorldRemoteEventKind.Rename)
            {
                if (edge.EntityId != 0 && remoteActors.TryGetValue(edge.EntityId, out var renamedActor) &&
                    remoteIncarnations.TryGetValue(edge.EntityId, out var renamedIncarnation) &&
                    renamedIncarnation == edge.Incarnation)
                    renamedActor.ApplyRenameEvent(edge.Rename, edge.Player);
                if (edge.EntityId == NetClient.SelfEntityId) ApplyLocalRename(edge.Rename);
            }
            else if (edge.Kind == WorldRemoteEventKind.Spawn && edge.NativeRegistryHandled)
            {
                // A retained edge only observes its actual receipt identity. A cleared,
                // destroyed or replaced registry can never be repopulated by this frame.
                RemotePlayer identity = edge.ReceivedIdentity as RemotePlayer;
                if (identity != null && ReferenceEquals(CombatWorld.FindPlayer(edge.EntityId), identity))
                {
                    if (remoteActors.TryGetValue(edge.EntityId, out var observed) &&
                        ReferenceEquals(observed.SourceIdentityForIndependentRuntime, identity))
                    {
                        if (!observed.AdoptReceivedIdentityForIndependentRuntime(edge.Player, identity)) return false;
                    }
                    else
                    {
                        RemoveRemoteReceiveActor(edge.EntityId, observationOnly: true);
                        observed = new ReferenceWorldRemotePlayerActor(map, resources, root,
                            edge.Player, remoteFeedbackAssets);
                        if (!observed.AdoptReceivedIdentityForIndependentRuntime(edge.Player, identity))
                        {
                            observed.Dispose();
                            return false;
                        }
                        remoteActors.Add(edge.EntityId, observed);
                    }
                    remoteIncarnations[edge.EntityId] = edge.Incarnation;
                }
            }
            else if (edge.Kind == WorldRemoteEventKind.Spawn)
            {
                ReferenceWorldRemotePlayerActor actor;
                long incarnation;
                bool same = remoteActors.TryGetValue(edge.EntityId, out actor) &&
                    remoteIncarnations.TryGetValue(edge.EntityId, out incarnation) && incarnation == edge.Incarnation;
                if (same)
                {
                    // Existing SpawnPlayer changes vitals, then Dispatch still applies its three tails.
                    if (!actor.ApplyRepeatedSpawnCoreForIndependentRuntime(edge.Player)) return false;
                    actor.ApplySpawnDispatchTail(edge.Player);
                }
                else
                {
                    RemoveRemoteReceiveActor(edge.EntityId);
                    if (!AddRemoteReceiveActor(edge.Player, edge.Incarnation, time)) return false;
                }
            }
            else
            {
                ReferenceWorldRemotePlayerActor actor;
                long incarnation;
                if (remoteActors.TryGetValue(edge.EntityId, out actor) &&
                    remoteIncarnations.TryGetValue(edge.EntityId, out incarnation) && incarnation == edge.Incarnation)
                {
                    switch (edge.Kind)
                    {
                        case WorldRemoteEventKind.Remove:
                            if (!edge.NativeRegistryHandled ||
                                !ReferenceEquals(CombatWorld.FindPlayer(edge.EntityId), actor.SourceIdentityForIndependentRuntime))
                                RemoveRemoteReceiveActor(edge.EntityId, edge.RemoveReason, edge.NativeRegistryHandled);
                            break;
                        case WorldRemoteEventKind.Hair:
                            if (edge.NativeRegistryHandled) actor.ObserveRegistryEventForIndependentRuntime(edge.Player);
                            else actor.ApplyHairEvent(edge.Player);
                            break;
                        case WorldRemoteEventKind.Appearance:
                            if (edge.NativeRegistryHandled) actor.ObserveRegistryEventForIndependentRuntime(edge.Player);
                            else actor.ApplyAppearanceEvent(edge.Appearance, edge.Player);
                            break;
                        case WorldRemoteEventKind.Path:
                            if (!actor.TryApplyPath(edge.Path, time)) return false;
                            break;
                        case WorldRemoteEventKind.Action:
                            if (!ApplyRemoteActionSnapshot(new Dictionary<int, WorldRemoteActionState>
                                { { edge.EntityId, edge.Action } })) return false;
                            break;
                        case WorldRemoteEventKind.Life:
                            if (edge.NativeRegistryHandled) actor.ObserveRegistryEventForIndependentRuntime(edge.Player);
                            else actor.ApplyLifeEvent(edge.Alive.Value, edge.Player);
                            break;
                        case WorldRemoteEventKind.Riding:
                            if (edge.NativeRegistryHandled) actor.ObserveRegistryEventForIndependentRuntime(edge.Player);
                            else actor.ApplyRidingEvent(edge.Riding, edge.Player);
                            break;
                        case WorldRemoteEventKind.Pk:
                            if (edge.NativeRegistryHandled) actor.ObserveRegistryEventForIndependentRuntime(edge.Player);
                            else actor.ApplyPkEvent(edge.Player);
                            break;
                        case WorldRemoteEventKind.Invis:
                            // Only an actor that is live under this exact incarnation receives the
                            // write, which is the product form of the official null lookup: an id
                            // with no identity consumes the packet without any effect and nothing
                            // is held for a later actor of the same id.
                            if (!edge.NativeRegistryHandled) actor.ApplyInvisEvent(edge.Invisible.Value);
                            break;
                        case WorldRemoteEventKind.StatusFx:
                            if (edge.NativeRegistryHandled) actor.ObserveRegistryEventForIndependentRuntime(edge.Player);
                            else actor.ApplyStatusEffectEvent(edge.StatusEffect);
                            break;
                        case WorldRemoteEventKind.Hp:
                            actor.ApplyHpEvent(edge.HitPoints.Value);
                            break;
                    }
                }
                else if (edge.Kind == WorldRemoteEventKind.Path &&
                    !remoteActors.ContainsKey(edge.EntityId))
                {
                    // An id with no live receive actor is still one authoritative move. The
                    // official owner decides what it means: a local-player id warps, a registered
                    // monster reaches MonsterEntity.SetPath, and an id nobody owns is recorded by
                    // CombatWorld.NoteNoTarget. The protocol layer publishes the decoded path and
                    // makes no classification decision of its own.
                    // This edge carries Revision 0 (appended before WithRevision) and is never
                    // retained in a snapshot, so it is applied once by the sequence test at 180
                    // and 181 and by the assignment at 312 - there is no revision to compare and
                    // none is invented. A later incarnation of the same id is a different actor
                    // with its own revision state; none of the retained guards are reused here.
                    ApplyAuthoritativePathForIndependentRuntime(edge.Path);
                }
            }
            remoteReceiveSequence = edge.Sequence;
        }
        return session.AcknowledgeRemoteEvents(window);
    }
}

}
