using System;
using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{

internal enum ReferenceWorldGroundLootExit
{
    Success = 0,
    CombatTailFailed = 51,
    DropUnavailable = 52,
    PointerRejected = 53,
    ApproachBoundaryFailed = 54,
    PickupRequestFailed = 55,
    AuthoritativeRemovalFailed = 56,
    InventoryOutcomeFailed = 57,
    Timeout = 58,
    IdentityMismatch = 59,
    Unexpected = 71,
}

internal static class ReferenceWorldGroundLootCheck
{
    public const string Argument = "--ffo-world-ground-loot-check";
    public const string Marker = "FFO_WORLD_GROUND_LOOT_V1";
    public const string ArtifactFileName = "rebuilt-world-ground-loot.json";
    public const string PartialArtifactFileName =
        "rebuilt-world-ground-loot-partial.json";
    public const int FixtureMapId = 6;
    public const int FixtureMonsterDefinitionId = 1074;
    public const int FixtureMonsterModelId = 274;
    public const int FixtureDropSequence = 21;
    public const int FixtureItemId = 3128;
    public const float FixtureDropRatePercent = 10f;
    public const int FixturePlayerX = 10973;
    public const int FixturePlayerY = 2865;
    public const int FixtureItemX = 11022;
    public const int FixtureItemY = 2869;
    public const float TimeoutSeconds = 45f;
    public const float MaximumClickDistancePixels = 50f;
    public const int MaximumPostKillGroundItems = 16;

    public static bool IsRequested(string[] arguments)
    {
        if (arguments == null)
        {
            return false;
        }

        for (int index = 0; index < arguments.Length; index++)
        {
            if (string.Equals(arguments[index], Argument, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static string FailureCode(ReferenceWorldGroundLootExit exit)
    {
        switch (exit)
        {
            case ReferenceWorldGroundLootExit.Success:
                return "ok";
            case ReferenceWorldGroundLootExit.CombatTailFailed:
                return "combat_tail.failed";
            case ReferenceWorldGroundLootExit.DropUnavailable:
                return "ground_item.unavailable";
            case ReferenceWorldGroundLootExit.PointerRejected:
                return "pointer.ground_item_rejected";
            case ReferenceWorldGroundLootExit.ApproachBoundaryFailed:
                return "ground_item.approach_boundary_failed";
            case ReferenceWorldGroundLootExit.PickupRequestFailed:
                return "ground_item.pickup_request_failed";
            case ReferenceWorldGroundLootExit.AuthoritativeRemovalFailed:
                return "ground_item.authoritative_removal_failed";
            case ReferenceWorldGroundLootExit.InventoryOutcomeFailed:
                return "ground_item.inventory_outcome_failed";
            case ReferenceWorldGroundLootExit.Timeout:
                return "stage.timeout";
            case ReferenceWorldGroundLootExit.IdentityMismatch:
                return "ground_item.identity_mismatch";
            default:
                return "client.unexpected";
        }
    }

    public static string SuccessLine(int checkpoints)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} PASS drop=server-authored pointer=formal approach=outside-to-48 " +
            "identity=locked-authoritative-member pickup=exactly-once " +
            "removal=authoritative inventory=delta " +
            "checkpoints={1}",
            Marker,
            Math.Max(0, checkpoints));
    }

    public static string FailureLine(ReferenceWorldGroundLootExit exit)
    {
        return Marker + " FAIL code=" + FailureCode(exit);
    }

    public static string ArtifactLine()
    {
        return Marker + " ARTIFACT trace=" + ArtifactFileName;
    }

    public static string PartialArtifactLine(
        ReferenceWorldGroundLootExit exit,
        string phase,
        int checkpoints)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} PARTIAL path={1} phase={2} code={3} checkpoints={4}",
            Marker,
            PartialArtifactFileName,
            string.IsNullOrEmpty(phase) ? "unknown" : phase,
            FailureCode(exit),
            Math.Max(0, checkpoints));
    }

    public static bool TryWriteBootstrapPartial(ReferenceWorldGroundLootExit exit)
    {
        ReferenceWorldGroundLootPartialArtifact partial =
            new ReferenceWorldGroundLootPartialArtifact
            {
                failureCode = FailureCode(exit),
                phase = "BeforeDriver",
            };
        return ReferenceMonsterRealCombatTailCheck.WriteJsonAtomically(
            PartialArtifactFileName,
            JsonUtility.ToJson(partial, true) + "\n");
    }

    internal static int CountInventoryItem(WorldInventoryState inventory, int itemId)
    {
        if (inventory == null || itemId <= 0)
        {
            return 0;
        }

        int count = 0;
        for (int index = 0; index < inventory.Items.Count; index++)
        {
            WorldInventoryItemState item = inventory.Items[index];
            if (item != null && item.ItemId == itemId && item.Count > 0)
            {
                count += item.Count;
            }
        }

        return count;
    }

    internal static bool IsAcceptedTrace(ReferenceWorldGroundLootArtifact value)
    {
        ReferenceWorldGroundLootSetMember intended;
        ReferenceWorldGroundLootSetMember actual;
        if (value == null || value.schemaVersion != 3 ||
            value.source != "rebuilt-real-service" ||
            value.mapId != FixtureMapId || !value.combatTailSucceeded ||
            value.combatCheckpointCount <= 0 || value.combatCandidateAttempts <= 0 ||
            value.combatCandidateAttempts > 8 || value.combatRetargetCount < 0 ||
            value.combatRetargetCount >= value.combatCandidateAttempts ||
            !IsFinite(value.combatInitialTargetDistancePixels) ||
            !(value.combatInitialTargetDistancePixels > 0f) ||
            value.combatInitialTargetDistancePixels > 50f ||
            !value.combatInitialTargetLineOfSight ||
            !TryValidatePostKillSet(value, out intended, out actual) ||
            value.sameCandidate != (value.intendedGroundId == value.actualGroundId) ||
            value.intendedItemId != FixtureItemId || value.actualItemId <= 0 ||
            value.itemCount <= 0 ||
            string.IsNullOrEmpty(value.itemName) || !value.itemOwned ||
            !value.itemEligible ||
            value.itemLockMilliseconds < 0 || !value.groundActorBuilt ||
            !value.groundActorRootPresent || !value.groundColliderPresent ||
            !value.groundColliderEnabled ||
            value.spawnGroundItemsVersion <= value.groundItemsVersionBaseline ||
            value.spawnAuthoritySequence <= value.authoritySequenceBaseline ||
            !value.pointerAccepted ||
            value.pointerAction != "GroundItemPickupApproach" ||
            value.pickedTargetKind != "GroundItem" ||
            value.pointerOverHud ||
            value.windowLastFailure != "None" ||
            value.windowLastPointerAction != "GroundItemPickupApproach" ||
            !IsFinite(value.pointerScreenX) || !IsFinite(value.pointerScreenY) ||
            !IsFinite(value.pointerWorldX) || !IsFinite(value.pointerWorldY) ||
            !IsFinite(value.pointerMapX) || !IsFinite(value.pointerMapY) ||
            value.pointerScreenX < 0f || value.pointerScreenX > 800f ||
            value.pointerScreenY < 0f || value.pointerScreenY > 600f ||
            Mathf.Abs(value.pointerMapX - intended.x) > 0.01f ||
            Mathf.Abs(value.pointerMapY - intended.y) > 0.01f ||
            value.pathOutcome != "PrimaryPath" ||
            !value.pathMoveToTarget || !value.pathMovementActive ||
            value.pathWaypointCount <= 0 ||
            !value.pendingMatchedActual ||
            value.clickInsidePickupRange ||
            !value.pickupPathScheduled || !value.approachMovementObserved ||
            !IsFinite(value.clickDistancePixels) ||
            !(value.clickDistancePixels >
                ReferenceWorldGroundItemActor.PickupRangePixels) ||
            value.clickDistancePixels > MaximumClickDistancePixels ||
            !IsFinite(value.requestDistancePixels) ||
            value.requestDistancePixels < 0f ||
            value.requestDistancePixels >
                ReferenceWorldGroundItemActor.PickupRangePixels ||
            value.pickupRequestIssuedDelta != 1 ||
            value.pickupRequestSucceededDelta != 1 ||
            !value.requestedMatchedActual || !value.requestWriteSucceeded ||
            !value.authoritativeRemovalObserved || !value.removeMatchedActual ||
            value.removeAuthoritySequence <= value.postKillSetAuthoritySequence ||
            !value.finalGroundSnapshotAbsent || !value.finalGroundActorAbsent ||
            value.inventoryAuthoritySequence <= value.removeAuthoritySequence ||
            value.inventoryStateVersionAfter != value.inventoryStateVersionBefore + 1 ||
            value.inventoryCountBefore < 0 ||
            value.inventoryCountAfter - value.inventoryCountBefore != value.itemCount ||
            value.inventoryDelta != value.itemCount ||
            !string.IsNullOrEmpty(value.lastPickupFailureCode) ||
            value.checkpoints == null || value.checkpoints.Count != 8)
        {
            return false;
        }

        string[] expected =
        {
            "combat.removed",
            "loot.spawned",
            "loot.pointer.selected",
            "loot.approach.moved",
            "loot.requested",
            "loot.request.succeeded",
            "loot.removed",
            "inventory.updated",
        };
        for (int index = 0; index < expected.Length; index++)
        {
            if (value.checkpoints[index] == null ||
                value.checkpoints[index].stage != expected[index])
            {
                return false;
            }
        }

        return HasAcceptedCheckpointEvidence(value);
    }

    private static bool HasAcceptedCheckpointEvidence(
        ReferenceWorldGroundLootArtifact value)
    {
        long[] issued = { 0, 0, 0, 0, 1, 1, 1, 1 };
        long[] succeeded = { 0, 0, 0, 0, -1, 1, 1, 1 };
        for (int index = 0; index < 6; index++)
        {
            ReferenceWorldGroundLootCheckpoint checkpoint = value.checkpoints[index];
            if (checkpoint.requestIssuedDelta != issued[index] ||
                (succeeded[index] >= 0 &&
                 checkpoint.requestSucceededDelta != succeeded[index]) ||
                (index == 4 && checkpoint.requestSucceededDelta != 0 &&
                 checkpoint.requestSucceededDelta != 1) ||
                checkpoint.groundItemsVersion != value.spawnGroundItemsVersion ||
                checkpoint.inventoryStateVersion != value.inventoryStateVersionBefore ||
                checkpoint.authoritySequence != value.postKillSetAuthoritySequence ||
                checkpoint.groundId != 0 || checkpoint.groundSnapshotAbsent ||
                checkpoint.groundActorAbsent || checkpoint.itemId != 0 ||
                checkpoint.inventoryCount != 0 || checkpoint.inventoryDelta != 0)
            {
                return false;
            }
        }

        ReferenceWorldGroundLootCheckpoint removed = value.checkpoints[6];
        bool removedBeforeInventory =
            removed.inventoryStateVersion == value.inventoryStateVersionBefore &&
            removed.inventoryCount == value.inventoryCountBefore &&
            removed.inventoryDelta == 0;
        bool removedAfterInventory =
            removed.inventoryStateVersion == value.inventoryStateVersionAfter &&
            removed.inventoryCount == value.inventoryCountAfter &&
            removed.inventoryDelta == value.inventoryDelta;
        if (removed.requestIssuedDelta != 1 ||
            removed.requestSucceededDelta != 1 ||
            removed.groundItemsVersion != value.spawnGroundItemsVersion + 1 ||
            removed.authoritySequence != value.removeAuthoritySequence ||
            removed.groundId != value.actualGroundId ||
            !removed.groundSnapshotAbsent || !removed.groundActorAbsent ||
            removed.itemId != value.actualItemId ||
            (!removedBeforeInventory && !removedAfterInventory))
        {
            return false;
        }

        ReferenceWorldGroundLootCheckpoint inventory = value.checkpoints[7];
        return inventory.requestIssuedDelta == 1 &&
               inventory.requestSucceededDelta == 1 &&
               inventory.groundItemsVersion == value.spawnGroundItemsVersion + 1 &&
               inventory.inventoryStateVersion == value.inventoryStateVersionAfter &&
               inventory.authoritySequence == value.inventoryAuthoritySequence &&
               inventory.groundId == value.actualGroundId &&
               inventory.groundSnapshotAbsent && inventory.groundActorAbsent &&
               inventory.itemId == value.actualItemId &&
               inventory.inventoryCount == value.inventoryCountAfter &&
               inventory.inventoryDelta == value.inventoryDelta;
    }

    private static bool TryValidatePostKillSet(
        ReferenceWorldGroundLootArtifact value,
        out ReferenceWorldGroundLootSetMember intended,
        out ReferenceWorldGroundLootSetMember actual)
    {
        intended = null;
        actual = null;
        if (value.postKillGroundItems == null ||
            value.postKillGroundItemCount != value.postKillGroundItems.Count ||
            value.postKillGroundItemCount <= 0 ||
            value.postKillGroundItemCount > MaximumPostKillGroundItems ||
            value.spawnGroundItemsVersion !=
                value.groundItemsVersionBaseline + value.postKillGroundItemCount ||
            value.postKillSetAuthoritySequence !=
                value.authoritySequenceBaseline + value.postKillGroundItemCount)
        {
            return false;
        }

        HashSet<int> ids = new HashSet<int>();
        for (int index = 0; index < value.postKillGroundItems.Count; index++)
        {
            ReferenceWorldGroundLootSetMember member = value.postKillGroundItems[index];
            long expectedSequence = value.authoritySequenceBaseline + index + 1;
            if (member == null || member.groundId <= 0 || member.itemId <= 0 ||
                member.count <= 0 || string.IsNullOrEmpty(member.displayName) ||
                member.lockMilliseconds < 0 ||
                member.spawnAuthoritySequence != expectedSequence ||
                !ids.Add(member.groundId))
            {
                return false;
            }

            if (member.groundId == value.intendedGroundId)
            {
                intended = member;
            }
            if (member.groundId == value.actualGroundId)
            {
                actual = member;
            }
        }

        return intended != null && actual != null &&
               intended.itemId == FixtureItemId && intended.count == 1 &&
               intended.x == FixtureItemX && intended.y == FixtureItemY &&
               intended.owned && intended.eligible && intended.actorBuilt &&
               intended.actorRootPresent && intended.colliderPresent &&
               intended.colliderEnabled &&
               actual.itemId > 0 && actual.count > 0 &&
               actual.owned && actual.eligible && actual.actorBuilt &&
               actual.actorRootPresent && actual.colliderPresent &&
               actual.colliderEnabled &&
               actual.spawnAuthoritySequence > value.authoritySequenceBaseline &&
               intended.itemId == value.intendedItemId &&
               actual.itemId == value.actualItemId &&
               actual.count == value.itemCount &&
               actual.x == value.itemX && actual.y == value.itemY &&
               actual.displayName == value.itemName &&
               actual.owned == value.itemOwned &&
               actual.eligible == value.itemEligible &&
               actual.lockMilliseconds == value.itemLockMilliseconds &&
               actual.kind == value.itemKind && actual.quality == value.itemQuality &&
               actual.spawnAuthoritySequence == value.spawnAuthoritySequence &&
               actual.actorBuilt == value.groundActorBuilt &&
               actual.actorRootPresent == value.groundActorRootPresent &&
               actual.colliderPresent == value.groundColliderPresent &&
               actual.colliderEnabled == value.groundColliderEnabled;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

[Serializable]
internal sealed class ReferenceWorldGroundLootArtifact
{
    public int schemaVersion = 3;
    public string source = "rebuilt-real-service";
    public int mapId;
    public bool combatTailSucceeded;
    public int combatCheckpointCount;
    public int combatCandidateAttempts;
    public int combatRetargetCount;
    public float combatInitialTargetDistancePixels;
    public bool combatInitialTargetLineOfSight;
    public long groundItemsVersionBaseline;
    public long authoritySequenceBaseline;
    public int postKillGroundItemCount;
    public long postKillSetAuthoritySequence;
    public List<ReferenceWorldGroundLootSetMember> postKillGroundItems =
        new List<ReferenceWorldGroundLootSetMember>();
    public int intendedGroundId;
    public int intendedItemId;
    public int actualGroundId;
    public int actualItemId;
    public bool sameCandidate;
    public int itemCount;
    public int itemX;
    public int itemY;
    public string itemName = string.Empty;
    public bool itemOwned;
    public bool itemEligible;
    public int itemLockMilliseconds;
    public int itemKind;
    public int itemQuality;
    public bool groundActorBuilt;
    public bool groundActorRootPresent;
    public bool groundColliderPresent;
    public bool groundColliderEnabled;
    public long spawnGroundItemsVersion;
    public long spawnAuthoritySequence;
    public bool pointerAccepted;
    public string pointerAction = string.Empty;
    public float pointerScreenX;
    public float pointerScreenY;
    public float pointerWorldX;
    public float pointerWorldY;
    public float pointerMapX;
    public float pointerMapY;
    public string pickedTargetKind = string.Empty;
    public string windowLastFailure = string.Empty;
    public string windowLastPointerAction = string.Empty;
    public bool pointerOverHud;
    public float clickDistancePixels;
    public bool clickInsidePickupRange;
    public string pathOutcome = string.Empty;
    public bool pathMoveToTarget;
    public bool pathMovementActive;
    public int pathWaypointCount;
    public bool pendingMatchedActual;
    public bool pickupPathScheduled;
    public bool approachMovementObserved;
    public float requestDistancePixels;
    public long pickupRequestIssuedDelta;
    public long pickupRequestSucceededDelta;
    public bool requestedMatchedActual;
    public bool requestWriteSucceeded;
    public bool authoritativeRemovalObserved;
    public long removeAuthoritySequence;
    public bool removeMatchedActual;
    public bool finalGroundSnapshotAbsent;
    public bool finalGroundActorAbsent;
    public long inventoryAuthoritySequence;
    public long inventoryStateVersionBefore;
    public long inventoryStateVersionAfter;
    public int inventoryCountBefore;
    public int inventoryCountAfter;
    public int inventoryDelta;
    public string lastPickupFailureCode = string.Empty;
    public List<ReferenceWorldGroundLootCheckpoint> checkpoints =
        new List<ReferenceWorldGroundLootCheckpoint>();
}

[Serializable]
internal sealed class ReferenceWorldGroundLootSetMember
{
    public int groundId;
    public int itemId;
    public int count;
    public int x;
    public int y;
    public string displayName = string.Empty;
    public bool owned;
    public bool eligible;
    public int lockMilliseconds;
    public int kind;
    public int quality;
    public long spawnAuthoritySequence;
    public bool actorBuilt;
    public bool actorRootPresent;
    public bool colliderPresent;
    public bool colliderEnabled;
}

[Serializable]
internal sealed class ReferenceWorldGroundLootCheckpoint
{
    public string stage = string.Empty;
    public float playerX;
    public float playerY;
    public float distancePixels;
    public long requestIssuedDelta;
    public long requestSucceededDelta;
    public long groundItemsVersion;
    public long inventoryStateVersion;
    public long authoritySequence;
    public int groundId;
    public bool groundSnapshotAbsent;
    public bool groundActorAbsent;
    public int itemId;
    public int inventoryCount;
    public int inventoryDelta;
}

[Serializable]
internal sealed class ReferenceWorldGroundLootPartialArtifact
{
    public int schemaVersion = 3;
    public string source = "rebuilt-real-service-partial";
    public string failureCode = string.Empty;
    public string phase = string.Empty;
    public List<ReferenceWorldGroundLootSetMember> postKillGroundItems =
        new List<ReferenceWorldGroundLootSetMember>();
    public int intendedGroundId;
    public int intendedItemId;
    public int actualGroundId;
    public int actualItemId;
    public bool sameCandidate;
    public float pointerScreenX;
    public float pointerScreenY;
    public float pointerWorldX;
    public float pointerWorldY;
    public float pointerMapX;
    public float pointerMapY;
    public string pickedTargetKind = string.Empty;
    public string windowLastFailure = string.Empty;
    public string windowLastPointerAction = string.Empty;
    public bool pointerOverHud;
    public bool groundActorRootPresent;
    public bool groundColliderPresent;
    public bool groundColliderEnabled;
    public float clickDistancePixels;
    public bool clickInsidePickupRange;
    public string pathOutcome = string.Empty;
    public bool pathMoveToTarget;
    public bool pathMovementActive;
    public int pathWaypointCount;
    public bool pendingMatchedActual;
    public bool requestedMatchedActual;
    public bool removeMatchedActual;
    public long pickupRequestIssuedDelta;
    public long pickupRequestSucceededDelta;
    public long removeAuthoritySequence;
    public long inventoryAuthoritySequence;
    public List<ReferenceWorldGroundLootCheckpoint> checkpoints =
        new List<ReferenceWorldGroundLootCheckpoint>();
}

internal sealed class ReferenceWorldGroundLootDriver
{
    private enum Phase
    {
        Combat,
        WaitingForDrop,
        Approaching,
        WaitingForRequestWrite,
        WaitingForRemoval,
        WaitingForInventory,
        Complete,
        Failed,
    }

    private readonly ClientSessionCoordinator session;
    private readonly ReferenceFirstWorldWindowOwner window;
    private readonly ReferenceMonsterRealCombatTailDriver combat;
    private readonly ReferenceWorldGroundLootArtifact artifact =
        new ReferenceWorldGroundLootArtifact();
    private readonly HashSet<int> initialGroundIds = new HashSet<int>();
    private readonly Dictionary<int, ReferenceWorldGroundLootSetMember>
        postKillGroundItemsById =
            new Dictionary<int, ReferenceWorldGroundLootSetMember>();
    private readonly float deadline;
    private Phase phase = Phase.Combat;
    private WorldGroundItemState groundItem;
    private float clickPlayerX;
    private float clickPlayerY;
    private long requestIssuedBaseline;
    private long requestSucceededBaseline;
    private bool actualIdentityLocked;
    private int lockedActualGroundId = -1;
    private int lockedActualItemId = -1;
    private int lockedActualItemCount;
    private bool approachCheckpointRecorded;
    private bool removalCheckpointRecorded;

    public ReferenceWorldGroundLootDriver(
        ClientSessionCoordinator session,
        ReferenceFirstWorldWindowOwner window)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.window = window ?? throw new ArgumentNullException(nameof(window));
        combat = new ReferenceMonsterRealCombatTailDriver(session, window);
        deadline = Time.realtimeSinceStartup + ReferenceWorldGroundLootCheck.TimeoutSeconds;
        artifact.mapId = window.MapDescriptor.id;
        artifact.groundItemsVersionBaseline = session.GroundItemsVersion;
        WorldGroundLootAuthorityState authority = session.GroundLootAuthorityState ??
            WorldGroundLootAuthorityState.Empty;
        artifact.authoritySequenceBaseline = authority.Sequence;
        foreach (int groundId in session.GroundItems.Keys)
        {
            initialGroundIds.Add(groundId);
        }
    }

    public bool Terminal => phase == Phase.Complete || phase == Phase.Failed;

    public ReferenceWorldGroundLootExit Exit { get; private set; }

    public int CheckpointCount => artifact.checkpoints.Count;

    public string PhaseName => phase.ToString();

    public void Tick()
    {
        if (Terminal)
        {
            return;
        }

        try
        {
            if (Time.realtimeSinceStartup >= deadline)
            {
                Fail(ReferenceWorldGroundLootExit.Timeout);
                return;
            }

            switch (phase)
            {
                case Phase.Combat:
                    TraceCombat();
                    break;
                case Phase.WaitingForDrop:
                    TraceDrop();
                    break;
                case Phase.Approaching:
                    TraceApproach();
                    break;
                case Phase.WaitingForRequestWrite:
                    TraceRequestWrite();
                    break;
                case Phase.WaitingForRemoval:
                case Phase.WaitingForInventory:
                    TraceSettlement();
                    break;
            }
        }
        catch (Exception)
        {
            Fail(ReferenceWorldGroundLootExit.Unexpected);
        }
    }

    private void TraceCombat()
    {
        combat.Tick();
        if (!combat.Terminal)
        {
            return;
        }
        if (combat.Exit != ReferenceMonsterRealCombatTailExit.Success)
        {
            Fail(ReferenceWorldGroundLootExit.CombatTailFailed);
            return;
        }

        artifact.combatTailSucceeded = true;
        artifact.combatCheckpointCount = combat.CheckpointCount;
        artifact.combatCandidateAttempts = combat.CandidateAttemptsForGroundLoot;
        artifact.combatRetargetCount = combat.RetargetCountForGroundLoot;
        artifact.combatInitialTargetDistancePixels =
            combat.InitialTargetDistancePixelsForGroundLoot;
        artifact.combatInitialTargetLineOfSight =
            combat.InitialTargetLineOfSightForGroundLoot;
        Record("combat.removed", 0f);
        phase = Phase.WaitingForDrop;
        TraceDrop();
    }

    private void TraceDrop()
    {
        List<KeyValuePair<int, WorldGroundItemState>> postKillStates =
            new List<KeyValuePair<int, WorldGroundItemState>>();
        foreach (KeyValuePair<int, WorldGroundItemState> pair in session.GroundItems)
        {
            WorldGroundItemState state = pair.Value;
            if (state == null || initialGroundIds.Contains(pair.Key))
            {
                continue;
            }

            postKillStates.Add(pair);
        }

        if (postKillStates.Count == 0)
        {
            return;
        }
        if (postKillStates.Count >
            ReferenceWorldGroundLootCheck.MaximumPostKillGroundItems)
        {
            Fail(ReferenceWorldGroundLootExit.DropUnavailable);
            return;
        }

        WorldGroundLootAuthorityState authority = session.GroundLootAuthorityState ??
            WorldGroundLootAuthorityState.Empty;
        if (session.GroundItemsVersion - artifact.groundItemsVersionBaseline !=
                postKillStates.Count ||
            authority.LastSpawnSequence - artifact.authoritySequenceBaseline !=
                postKillStates.Count ||
            authority.Sequence != authority.LastSpawnSequence)
        {
            Fail(ReferenceWorldGroundLootExit.DropUnavailable);
            return;
        }

        List<ReferenceWorldGroundLootSetMember> captured =
            new List<ReferenceWorldGroundLootSetMember>(postKillStates.Count);
        ReferenceWorldGroundLootSetMember intended = null;
        ReferenceWorldGroundItemActor intendedActor = null;
        WorldGroundItemState intendedState = null;
        bool expectedRowPresent = false;
        for (int index = 0; index < postKillStates.Count; index++)
        {
            WorldGroundItemState state = postKillStates[index].Value;
            ReferenceWorldGroundItemActor actor = null;
            if (window.Frame != null)
            {
                window.Frame.GroundItemActors.TryGetValue(state.GroundId, out actor);
            }
            if (actor != null && !actor.Matches(state))
            {
                Fail(ReferenceWorldGroundLootExit.DropUnavailable);
                return;
            }

            bool actorBuilt = actor != null && actor.Built;
            ReferenceWorldGroundLootSetMember member =
                new ReferenceWorldGroundLootSetMember
                {
                    groundId = state.GroundId,
                    itemId = state.ItemId,
                    count = state.Count,
                    x = state.X,
                    y = state.Y,
                    displayName = state.DisplayName,
                    owned = state.Owned,
                    eligible = state.Owned || actor != null &&
                        !actor.IsProtected(Time.realtimeSinceStartup),
                    lockMilliseconds = state.LockMilliseconds,
                    kind = state.Kind,
                    quality = state.Quality,
                    spawnAuthoritySequence =
                        artifact.authoritySequenceBaseline + index + 1,
                    actorBuilt = actorBuilt,
                    actorRootPresent = actor != null && actor.Root != null,
                    colliderPresent = actor != null && actor.PickBox != null,
                    colliderEnabled = actor != null && actor.PickBox != null &&
                        actor.PickBox.enabled,
                };
            captured.Add(member);
            bool expectedRow =
                member.itemId == ReferenceWorldGroundLootCheck.FixtureItemId &&
                member.count == 1 &&
                member.x == ReferenceWorldGroundLootCheck.FixtureItemX &&
                member.y == ReferenceWorldGroundLootCheck.FixtureItemY;
            expectedRowPresent = expectedRowPresent || expectedRow;
            if (expectedRow &&
                member.owned && member.eligible && member.actorBuilt &&
                member.actorRootPresent && member.colliderPresent &&
                member.colliderEnabled)
            {
                intended = member;
                intendedActor = actor;
                intendedState = state;
            }
        }

        if (intended == null && expectedRowPresent)
        {
            return;
        }
        if (authority.LastSpawnGroundId != captured[captured.Count - 1].groundId ||
            intended == null || intendedActor == null || intendedState == null)
        {
            Fail(ReferenceWorldGroundLootExit.DropUnavailable);
            return;
        }

        artifact.postKillGroundItemCount = captured.Count;
        artifact.postKillSetAuthoritySequence = authority.LastSpawnSequence;
        artifact.postKillGroundItems.AddRange(captured);
        postKillGroundItemsById.Clear();
        for (int index = 0; index < captured.Count; index++)
        {
            postKillGroundItemsById.Add(captured[index].groundId, captured[index]);
        }
        artifact.intendedGroundId = intended.groundId;
        artifact.intendedItemId = intended.itemId;
        artifact.spawnGroundItemsVersion = session.GroundItemsVersion;

        float intendedClickDistance = DistanceToGround(intendedState);
        Record("loot.spawned", intendedClickDistance);
        if (!(intendedClickDistance > ReferenceWorldGroundItemActor.PickupRangePixels) ||
            intendedClickDistance >
                ReferenceWorldGroundLootCheck.MaximumClickDistancePixels)
        {
            Fail(ReferenceWorldGroundLootExit.ApproachBoundaryFailed);
            return;
        }

        Camera camera = window.Camera;
        if (camera == null || intendedActor.Root == null)
        {
            Fail(ReferenceWorldGroundLootExit.PointerRejected);
            return;
        }
        Vector3 screen = camera.WorldToScreenPoint(intendedActor.Root.transform.position);
        if (screen.z < 0f || screen.x < 0f || screen.x > Screen.width ||
            screen.y < 0f || screen.y > Screen.height)
        {
            Fail(ReferenceWorldGroundLootExit.PointerRejected);
            return;
        }

        requestIssuedBaseline = window.GroundPickupRequestIssuedCountForGroundLoot;
        requestSucceededBaseline =
            window.GroundPickupRequestSucceededCountForGroundLoot;
        clickPlayerX = window.Frame.Player.WorldX;
        clickPlayerY = window.Frame.Player.WorldY;
        artifact.pointerScreenX = screen.x;
        artifact.pointerScreenY = screen.y;
        bool accepted = window.HandlePointerInput(
            screen,
            true,
            true,
            false,
            false,
            false,
            Time.unscaledTime,
            Time.time,
            Rect.zero);
        artifact.pointerAccepted = accepted;
        artifact.pointerAction = window.LastPointerAction.ToString();
        artifact.pointerWorldX = window.LastPointerWorldPosition.x;
        artifact.pointerWorldY = window.LastPointerWorldPosition.y;
        artifact.pointerMapX = window.LastPointerMapPosition.x;
        artifact.pointerMapY = window.LastPointerMapPosition.y;
        artifact.pickedTargetKind = window.LastPointerPickedTargetKind;
        artifact.windowLastFailure = window.LastFailure.ToString();
        artifact.windowLastPointerAction = window.LastPointerAction.ToString();
        artifact.pointerOverHud = window.LastPointerOverHud;
        artifact.pathOutcome = window.Frame.Player.LastSetPathOutcome.ToString();
        artifact.pathMoveToTarget = window.Frame.Player.LastSetPathMoveToTarget;
        artifact.pathMovementActive = window.Frame.Player.MovementActive;
        artifact.pathWaypointCount = window.Frame.Player.MovementPathCount;
        artifact.pickupPathScheduled = window.Frame.Player.MovementActive &&
            window.Frame.Player.MovementPathCount > 0;
        if (!accepted ||
            window.LastPointerAction !=
                ReferenceFirstWorldPointerAction.GroundItemPickupApproach ||
            window.LastPointerPickedTargetKind != "GroundItem" ||
            window.GroundPickupRequestIssuedCountForGroundLoot !=
                requestIssuedBaseline)
        {
            Fail(ReferenceWorldGroundLootExit.PointerRejected);
            return;
        }
        ReferenceWorldGroundLootSetMember actual;
        WorldGroundItemState actualState;
        ReferenceWorldGroundItemActor actualActor;
        int selectedGroundId = window.LastPointerPickedTargetId;
        bool selectedMemberFound = postKillGroundItemsById.TryGetValue(
            selectedGroundId,
            out actual);
        int selectedItemId = selectedMemberFound && actual != null ? actual.itemId : 0;
        if (!TryLockActualIdentity(selectedGroundId, selectedItemId) ||
            !artifact.pendingMatchedActual || lockedActualGroundId <= 0 ||
            lockedActualItemId <= 0 || !selectedMemberFound || actual == null ||
            !session.GroundItems.TryGetValue(lockedActualGroundId, out actualState) ||
            actualState == null ||
            !window.Frame.GroundItemActors.TryGetValue(
                lockedActualGroundId,
                out actualActor) ||
            actualActor == null || !actualActor.Matches(actualState) ||
            actual.itemId <= 0 || actual.count <= 0 ||
            !actual.owned || !actual.eligible || !actual.actorBuilt ||
            !actual.actorRootPresent || !actual.colliderPresent ||
            !actual.colliderEnabled || !actualActor.Built ||
            actualActor.Root == null || actualActor.PickBox == null ||
            !actualActor.PickBox.enabled ||
            actual.spawnAuthoritySequence <= artifact.authoritySequenceBaseline)
        {
            Fail(ReferenceWorldGroundLootExit.IdentityMismatch);
            return;
        }

        groundItem = actualState;
        artifact.itemCount = actual.count;
        lockedActualItemCount = actual.count;
        artifact.itemX = actual.x;
        artifact.itemY = actual.y;
        artifact.itemName = actual.displayName;
        artifact.itemOwned = actual.owned;
        artifact.itemEligible = actual.eligible;
        artifact.itemLockMilliseconds = actual.lockMilliseconds;
        artifact.itemKind = actual.kind;
        artifact.itemQuality = actual.quality;
        artifact.groundActorBuilt = actual.actorBuilt;
        artifact.groundActorRootPresent = actual.actorRootPresent;
        artifact.groundColliderPresent = actual.colliderPresent;
        artifact.groundColliderEnabled = actual.colliderEnabled;
        artifact.spawnAuthoritySequence = actual.spawnAuthoritySequence;
        artifact.inventoryStateVersionBefore = session.InventoryStateVersion;
        artifact.inventoryCountBefore = ReferenceWorldGroundLootCheck.CountInventoryItem(
            session.InventoryState,
            lockedActualItemId);
        artifact.clickDistancePixels = DistanceToGround();
        artifact.clickInsidePickupRange = artifact.clickDistancePixels <=
            ReferenceWorldGroundItemActor.PickupRangePixels;
        if (!(artifact.clickDistancePixels >
                ReferenceWorldGroundItemActor.PickupRangePixels) ||
            artifact.clickDistancePixels >
                ReferenceWorldGroundLootCheck.MaximumClickDistancePixels)
        {
            Fail(ReferenceWorldGroundLootExit.ApproachBoundaryFailed);
            return;
        }
        if (!artifact.pickupPathScheduled ||
            window.Frame.Player.LastSetPathOutcome !=
                ReferenceWorldSetPathOutcome.PrimaryPath ||
            !window.Frame.Player.LastSetPathMoveToTarget)
        {
            Fail(ReferenceWorldGroundLootExit.ApproachBoundaryFailed);
            return;
        }

        Record("loot.pointer.selected", artifact.clickDistancePixels);
        phase = Phase.Approaching;
    }

    private void TraceApproach()
    {
        long issued = window.GroundPickupRequestIssuedCountForGroundLoot -
            requestIssuedBaseline;
        long succeeded = window.GroundPickupRequestSucceededCountForGroundLoot -
            requestSucceededBaseline;
        if (!ActualIdentityMatchesLock())
        {
            Fail(ReferenceWorldGroundLootExit.IdentityMismatch);
            return;
        }
        if (issued < 0 || issued > 1 || succeeded < 0 || succeeded > issued ||
            !string.IsNullOrEmpty(window.LastGroundPickupFailureCode))
        {
            Fail(ReferenceWorldGroundLootExit.PickupRequestFailed);
            return;
        }
        float distance = DistanceToGround();
        float moved = Mathf.Sqrt(
            Mathf.Pow(window.Frame.Player.WorldX - clickPlayerX, 2f) +
            Mathf.Pow(window.Frame.Player.WorldY - clickPlayerY, 2f));
        if (!approachCheckpointRecorded && moved > 0f)
        {
            approachCheckpointRecorded = true;
            artifact.approachMovementObserved = true;
            Record("loot.approach.moved", distance);
        }

        if (issued == 0)
        {
            if (!session.GroundItems.ContainsKey(lockedActualGroundId) ||
                !window.Frame.GroundItemActors.ContainsKey(lockedActualGroundId))
            {
                Fail(ReferenceWorldGroundLootExit.AuthoritativeRemovalFailed);
                return;
            }
            if (window.PendingGroundPickupId != lockedActualGroundId)
            {
                Fail(ReferenceWorldGroundLootExit.ApproachBoundaryFailed);
                return;
            }
            return;
        }

        artifact.requestedMatchedActual =
            window.LastGroundPickupRequestedIdForGroundLoot == lockedActualGroundId;
        if (!artifact.requestedMatchedActual)
        {
            Fail(ReferenceWorldGroundLootExit.IdentityMismatch);
            return;
        }

        if (!approachCheckpointRecorded ||
            distance > ReferenceWorldGroundItemActor.PickupRangePixels ||
            window.PendingGroundPickupId >= 0)
        {
            Fail(ReferenceWorldGroundLootExit.ApproachBoundaryFailed);
            return;
        }

        artifact.requestDistancePixels = distance;
        artifact.pickupRequestIssuedDelta = issued;
        artifact.pickupRequestSucceededDelta = succeeded;
        Record("loot.requested", distance);
        phase = Phase.WaitingForRequestWrite;
        TraceRequestWrite();
    }

    private void TraceRequestWrite()
    {
        long issued = window.GroundPickupRequestIssuedCountForGroundLoot -
            requestIssuedBaseline;
        long succeeded = window.GroundPickupRequestSucceededCountForGroundLoot -
            requestSucceededBaseline;
        if (!ActualIdentityMatchesLock())
        {
            Fail(ReferenceWorldGroundLootExit.IdentityMismatch);
            return;
        }
        if (issued != 1 || succeeded < 0 || succeeded > 1 ||
            !string.IsNullOrEmpty(window.LastGroundPickupFailureCode) ||
            window.LastGroundPickupRequestedIdForGroundLoot != lockedActualGroundId)
        {
            Fail(ReferenceWorldGroundLootExit.PickupRequestFailed);
            return;
        }
        if (succeeded == 0)
        {
            return;
        }

        artifact.pickupRequestIssuedDelta = issued;
        artifact.pickupRequestSucceededDelta = succeeded;
        artifact.requestWriteSucceeded = true;
        Record("loot.request.succeeded", DistanceToGround());
        phase = Phase.WaitingForRemoval;
        TraceSettlement();
    }

    private void TraceSettlement()
    {
        long issued = window.GroundPickupRequestIssuedCountForGroundLoot -
            requestIssuedBaseline;
        long succeeded = window.GroundPickupRequestSucceededCountForGroundLoot -
            requestSucceededBaseline;
        if (!ActualIdentityMatchesLock())
        {
            Fail(ReferenceWorldGroundLootExit.IdentityMismatch);
            return;
        }
        if (issued != 1 || succeeded != 1 ||
            !string.IsNullOrEmpty(window.LastGroundPickupFailureCode) ||
            window.LastGroundPickupRequestedIdForGroundLoot != lockedActualGroundId)
        {
            Fail(ReferenceWorldGroundLootExit.PickupRequestFailed);
            return;
        }

        bool snapshotPresent = session.GroundItems.ContainsKey(lockedActualGroundId);
        bool actorPresent = window.Frame.GroundItemActors.ContainsKey(lockedActualGroundId);
        int currentInventoryCount = ReferenceWorldGroundLootCheck.CountInventoryItem(
            session.InventoryState,
            lockedActualItemId);
        if (snapshotPresent)
        {
            if (!actorPresent ||
                currentInventoryCount != artifact.inventoryCountBefore)
            {
                Fail(ReferenceWorldGroundLootExit.InventoryOutcomeFailed);
            }
            return;
        }

        WorldGroundLootAuthorityState authority;
        long inventoryStateVersion;
        if (!TryReadSettlementAuthority(
                out authority,
                out inventoryStateVersion,
                out currentInventoryCount))
        {
            return;
        }
        if (!removalCheckpointRecorded)
        {
            artifact.removeMatchedActual =
                authority.LastRemoveGroundId == lockedActualGroundId;
            if (actorPresent || !artifact.removeMatchedActual ||
                authority.LastRemoveSequence <= artifact.postKillSetAuthoritySequence)
            {
                Fail(ReferenceWorldGroundLootExit.AuthoritativeRemovalFailed);
                return;
            }

            removalCheckpointRecorded = true;
            artifact.authoritativeRemovalObserved = true;
            artifact.removeAuthoritySequence = authority.LastRemoveSequence;
            artifact.finalGroundSnapshotAbsent = true;
            artifact.finalGroundActorAbsent = true;
            RecordSettlement(
                "loot.removed",
                DistanceToGround(),
                artifact.removeAuthoritySequence,
                inventoryStateVersion,
                lockedActualGroundId,
                true,
                true,
                lockedActualItemId,
                currentInventoryCount,
                currentInventoryCount - artifact.inventoryCountBefore);
            phase = Phase.WaitingForInventory;
        }

        if (inventoryStateVersion <= artifact.inventoryStateVersionBefore)
        {
            return;
        }

        artifact.inventoryAuthoritySequence = authority.LastInventorySequence;
        artifact.inventoryStateVersionAfter = inventoryStateVersion;
        artifact.inventoryCountAfter = currentInventoryCount;
        artifact.inventoryDelta = currentInventoryCount - artifact.inventoryCountBefore;
        artifact.lastPickupFailureCode = window.LastGroundPickupFailureCode;
        artifact.pickupRequestIssuedDelta = issued;
        artifact.pickupRequestSucceededDelta = succeeded;
        RecordSettlement(
            "inventory.updated",
            DistanceToGround(),
            artifact.inventoryAuthoritySequence,
            artifact.inventoryStateVersionAfter,
            lockedActualGroundId,
            true,
            true,
            lockedActualItemId,
            artifact.inventoryCountAfter,
            artifact.inventoryDelta);
        if (!ReferenceWorldGroundLootCheck.IsAcceptedTrace(artifact) ||
            !ReferenceMonsterRealCombatTailCheck.WriteJsonAtomically(
                ReferenceWorldGroundLootCheck.ArtifactFileName,
                JsonUtility.ToJson(artifact, true) + "\n"))
        {
            Fail(ReferenceWorldGroundLootExit.InventoryOutcomeFailed);
            return;
        }

        Exit = ReferenceWorldGroundLootExit.Success;
        phase = Phase.Complete;
    }

    private bool TryLockActualIdentity(int groundId, int itemId)
    {
        if (actualIdentityLocked)
        {
            return false;
        }

        actualIdentityLocked = true;
        lockedActualGroundId = groundId;
        lockedActualItemId = itemId;
        artifact.actualGroundId = groundId;
        artifact.actualItemId = itemId;
        artifact.sameCandidate = groundId == artifact.intendedGroundId;
        artifact.pendingMatchedActual = window.PendingGroundPickupId == groundId;
        return true;
    }

    private bool ActualIdentityMatchesLock()
    {
        return actualIdentityLocked && lockedActualGroundId > 0 &&
               lockedActualItemId > 0 && lockedActualItemCount > 0 &&
               artifact.actualGroundId == lockedActualGroundId &&
               artifact.actualItemId == lockedActualItemId &&
               artifact.itemCount == lockedActualItemCount &&
               groundItem != null && groundItem.GroundId == lockedActualGroundId &&
               groundItem.ItemId == lockedActualItemId &&
               groundItem.Count == lockedActualItemCount;
    }

    private bool TryReadSettlementAuthority(
        out WorldGroundLootAuthorityState authority,
        out long inventoryStateVersion,
        out int inventoryCount)
    {
        authority = session.GroundLootAuthorityState ??
            WorldGroundLootAuthorityState.Empty;
        inventoryStateVersion = session.InventoryStateVersion;
        WorldInventoryState inventory = session.InventoryState;
        long confirmedInventoryStateVersion = session.InventoryStateVersion;
        WorldGroundLootAuthorityState confirmedAuthority =
            session.GroundLootAuthorityState ?? WorldGroundLootAuthorityState.Empty;
        if (!ReferenceEquals(authority, confirmedAuthority) ||
            inventoryStateVersion != confirmedInventoryStateVersion)
        {
            inventoryCount = 0;
            return false;
        }

        inventoryCount = ReferenceWorldGroundLootCheck.CountInventoryItem(
            inventory,
            lockedActualItemId);
        if (inventoryStateVersion == artifact.inventoryStateVersionBefore)
        {
            return inventoryCount == artifact.inventoryCountBefore;
        }

        return inventoryStateVersion == artifact.inventoryStateVersionBefore + 1 &&
               authority.LastInventorySequence > authority.LastRemoveSequence;
    }

    private float DistanceToGround()
    {
        return DistanceToGround(groundItem);
    }

    private float DistanceToGround(WorldGroundItemState state)
    {
        if (state == null || window.Frame == null || window.Frame.Player == null)
        {
            return 0f;
        }

        float dx = window.Frame.Player.WorldX - state.X;
        float dy = window.Frame.Player.WorldY - state.Y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private void Record(string stage, float distance)
    {
        WorldGroundLootAuthorityState authority = session.GroundLootAuthorityState ??
            WorldGroundLootAuthorityState.Empty;
        artifact.checkpoints.Add(new ReferenceWorldGroundLootCheckpoint
        {
            stage = stage,
            playerX = window.Frame == null || window.Frame.Player == null
                ? 0f
                : window.Frame.Player.WorldX,
            playerY = window.Frame == null || window.Frame.Player == null
                ? 0f
                : window.Frame.Player.WorldY,
            distancePixels = distance,
            requestIssuedDelta = requestIssuedBaseline == 0 &&
                                 artifact.checkpoints.Count == 0
                ? 0
                : Math.Max(
                    0,
                    window.GroundPickupRequestIssuedCountForGroundLoot -
                    requestIssuedBaseline),
            requestSucceededDelta = requestSucceededBaseline == 0 &&
                                    artifact.checkpoints.Count == 0
                ? 0
                : Math.Max(
                    0,
                    window.GroundPickupRequestSucceededCountForGroundLoot -
                    requestSucceededBaseline),
            groundItemsVersion = session.GroundItemsVersion,
            inventoryStateVersion = session.InventoryStateVersion,
            authoritySequence = authority.Sequence,
        });
    }

    private void RecordSettlement(
        string stage,
        float distance,
        long authoritySequence,
        long inventoryStateVersion,
        int groundId,
        bool groundSnapshotAbsent,
        bool groundActorAbsent,
        int itemId,
        int inventoryCount,
        int inventoryDelta)
    {
        artifact.checkpoints.Add(new ReferenceWorldGroundLootCheckpoint
        {
            stage = stage,
            playerX = window.Frame == null || window.Frame.Player == null
                ? 0f
                : window.Frame.Player.WorldX,
            playerY = window.Frame == null || window.Frame.Player == null
                ? 0f
                : window.Frame.Player.WorldY,
            distancePixels = distance,
            requestIssuedDelta = Math.Max(
                0,
                window.GroundPickupRequestIssuedCountForGroundLoot -
                requestIssuedBaseline),
            requestSucceededDelta = Math.Max(
                0,
                window.GroundPickupRequestSucceededCountForGroundLoot -
                requestSucceededBaseline),
            groundItemsVersion = session.GroundItemsVersion,
            inventoryStateVersion = inventoryStateVersion,
            authoritySequence = authoritySequence,
            groundId = groundId,
            groundSnapshotAbsent = groundSnapshotAbsent,
            groundActorAbsent = groundActorAbsent,
            itemId = itemId,
            inventoryCount = inventoryCount,
            inventoryDelta = inventoryDelta,
        });
    }

    private void Fail(ReferenceWorldGroundLootExit exit)
    {
        if (Terminal)
        {
            return;
        }

        Exit = exit;
        string failedPhase = phase.ToString();
        phase = Phase.Failed;
        ReferenceWorldGroundLootPartialArtifact partial =
            new ReferenceWorldGroundLootPartialArtifact
            {
                failureCode = ReferenceWorldGroundLootCheck.FailureCode(exit),
                phase = failedPhase,
                postKillGroundItems = new List<ReferenceWorldGroundLootSetMember>(
                    artifact.postKillGroundItems),
                intendedGroundId = artifact.intendedGroundId,
                intendedItemId = artifact.intendedItemId,
                actualGroundId = artifact.actualGroundId,
                actualItemId = artifact.actualItemId,
                sameCandidate = artifact.sameCandidate,
                pointerScreenX = artifact.pointerScreenX,
                pointerScreenY = artifact.pointerScreenY,
                pointerWorldX = artifact.pointerWorldX,
                pointerWorldY = artifact.pointerWorldY,
                pointerMapX = artifact.pointerMapX,
                pointerMapY = artifact.pointerMapY,
                pickedTargetKind = artifact.pickedTargetKind,
                windowLastFailure = artifact.windowLastFailure,
                windowLastPointerAction = artifact.windowLastPointerAction,
                pointerOverHud = artifact.pointerOverHud,
                groundActorRootPresent = artifact.groundActorRootPresent,
                groundColliderPresent = artifact.groundColliderPresent,
                groundColliderEnabled = artifact.groundColliderEnabled,
                clickDistancePixels = artifact.clickDistancePixels,
                clickInsidePickupRange = artifact.clickInsidePickupRange,
                pathOutcome = artifact.pathOutcome,
                pathMoveToTarget = artifact.pathMoveToTarget,
                pathMovementActive = artifact.pathMovementActive,
                pathWaypointCount = artifact.pathWaypointCount,
                pendingMatchedActual = artifact.pendingMatchedActual,
                requestedMatchedActual = artifact.requestedMatchedActual,
                removeMatchedActual = artifact.removeMatchedActual,
                pickupRequestIssuedDelta = Math.Max(
                    0,
                    window.GroundPickupRequestIssuedCountForGroundLoot -
                    requestIssuedBaseline),
                pickupRequestSucceededDelta = Math.Max(
                    0,
                    window.GroundPickupRequestSucceededCountForGroundLoot -
                    requestSucceededBaseline),
                removeAuthoritySequence = artifact.removeAuthoritySequence,
                inventoryAuthoritySequence = artifact.inventoryAuthoritySequence,
                checkpoints = new List<ReferenceWorldGroundLootCheckpoint>(
                    artifact.checkpoints),
            };
        ReferenceMonsterRealCombatTailCheck.WriteJsonAtomically(
            ReferenceWorldGroundLootCheck.PartialArtifactFileName,
            JsonUtility.ToJson(partial, true) + "\n");
    }
}

}
