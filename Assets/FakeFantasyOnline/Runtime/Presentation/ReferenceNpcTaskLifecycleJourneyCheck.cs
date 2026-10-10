using System;
using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{

internal enum ReferenceNpcTaskLifecycleJourneyExit
{
    Success = 0,
    InitialAuthorityUnavailable = 90,
    NpcPointerRejected = 91,
    OfferUnavailable = 92,
    AcceptRequestFailed = 93,
    AcceptedStateMissing = 94,
    TravelFailed = 95,
    CombatTargetUnavailable = 96,
    CombatRequestFailed = 97,
    FatalResultMissing = 98,
    GoalDropMissing = 99,
    GoalPickupFailed = 100,
    ProgressMismatch = 101,
    TurnInUnavailable = 102,
    CompleteRequestFailed = 103,
    RewardMismatch = 104,
    Timeout = 105,
    BaselineRecoveryFailed = 106,
    WorldRevisionRejected = 107,
    TerminalCancellationFailed = 108,
    DialogTextEdgeWindowRejected = 109,
    Unexpected = 119,
}

internal enum ReferenceNpcTaskTravelRecoveryDecision
{
    Fail = 0,
    WaitForHurtEnd = 1,
    ReissueSameDestination = 2,
}

internal enum ReferenceNpcTaskTravelLegKind
{
    None = 0,
    WaitCombatMap = 1,
    WaitCombatArea = 2,
    ApproachCombatTarget = 3,
    WaitReturnMap = 4,
    WaitNpcReturnAnchor = 5,
    WaitNpcArrival = 6,
    WaitCombatReturnAnchor = 7,
}

internal enum ReferenceNpcTaskCombatTargetRejection
{
    None = 0,
    MissingActorOrState = 1,
    ModelMismatch = 2,
    SourceOwnerNotLive = 3,
    Dying = 4,
    RootMissing = 5,
    ColliderMissing = 6,
    ColliderDisabled = 7,
    FriendlyOrOwned = 8,
    NonPositiveHitPoints = 9,
}

internal static class ReferenceNpcTaskLifecycleJourneyCheck
{
    public const string Argument = "--ffo-npc-task-lifecycle-check";
    public const string Marker = "FFO_NPC_TASK_LIFECYCLE_V1";
    public const string ArtifactFileName = "rebuilt-npc-task-lifecycle.json";
    public const string PartialArtifactFileName =
        "rebuilt-npc-task-lifecycle-partial.json";
    public const int InitialMapId = 7;
    public const int CombatMapId = 6;
    public const string NpcName = "守城卫兵阿蒙";
    public const string TaskTitle = "守卫的委托：消灭木头人";
    public const string TaskDescription =
        "接受龙城守城卫兵阿蒙的委托，去龙城西郊消灭木头人，并取回10个木头零件。";
    public const string GoalItemName = "木头零件";
    public const string AwardText = "9银币。";
    public const int GoalItemId = 4148;
    public const int GoalCount = 10;
    public const int CombatMonsterModelId = 262;
    public const int InitialPlayerX = 13270;
    public const int InitialPlayerY = 6521;
    public const int InitialLevel = 16;
    public const float CityToCombatGateX = 5575f;
    public const float CityToCombatGateY = 2773f;
    public const float CombatAreaX = 6767f;
    public const float CombatAreaY = 5826f;
    public const float CombatToCityGateX = 10828f;
    public const float CombatToCityGateY = 4970f;
    public const float AuthoritativeReturnLandingX = 6425f;
    public const float AuthoritativeReturnLandingY = 3485f;
    public const float CityReturnAnchorX = 9935f;
    public const float CityReturnAnchorY = 4125f;
    public const float ReturnNpcX = 13270f;
    public const float ReturnNpcY = 6521f;
    public const float TimeoutSeconds = 420f;
    public const float TravelArrivalDistance = 90f;
    public const float TargetPointerDistance = 50f;
    public const float TargetRescanSeconds = 0.1f;
    public const float StateWaitSeconds = 8f;
    public const float TravelWaitSeconds = 75f;
    public const int MaximumTravelHitRecoveries = 8;
    public const int MaximumTravelLegs = 64;
    public const int MaximumApproachReplansPerTarget = 16;
    public const int MovementFrameRingCapacity = 32;
    public const long RewardMoneyDelta = 9000L;
    public const long RewardHonorDelta = 0L;
    public const int MaximumIncidentalPickups = 30;
    public const string RewardFeedbackText = "奖励金钱：9银币";
    public const string FreshAcceptReplacementPattern =
        "preaccept-state0-settled/postaccept-state1";
    public static readonly TimeSpan TerminalCancellationTimeout =
        TimeSpan.FromSeconds(2);

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

    public static string FailureCode(ReferenceNpcTaskLifecycleJourneyExit exit)
    {
        switch (exit)
        {
            case ReferenceNpcTaskLifecycleJourneyExit.Success:
                return "ok";
            case ReferenceNpcTaskLifecycleJourneyExit.InitialAuthorityUnavailable:
                return "task.initial_authority_unavailable";
            case ReferenceNpcTaskLifecycleJourneyExit.NpcPointerRejected:
                return "npc.pointer_rejected";
            case ReferenceNpcTaskLifecycleJourneyExit.OfferUnavailable:
                return "task.offer_unavailable";
            case ReferenceNpcTaskLifecycleJourneyExit.AcceptRequestFailed:
                return "task.accept_request_failed";
            case ReferenceNpcTaskLifecycleJourneyExit.AcceptedStateMissing:
                return "task.accepted_state_missing";
            case ReferenceNpcTaskLifecycleJourneyExit.TravelFailed:
                return "task.travel_failed";
            case ReferenceNpcTaskLifecycleJourneyExit.CombatTargetUnavailable:
                return "task.combat_target_unavailable";
            case ReferenceNpcTaskLifecycleJourneyExit.CombatRequestFailed:
                return "task.combat_request_failed";
            case ReferenceNpcTaskLifecycleJourneyExit.FatalResultMissing:
                return "task.fatal_result_missing";
            case ReferenceNpcTaskLifecycleJourneyExit.GoalDropMissing:
                return "task.goal_drop_missing";
            case ReferenceNpcTaskLifecycleJourneyExit.GoalPickupFailed:
                return "task.goal_pickup_failed";
            case ReferenceNpcTaskLifecycleJourneyExit.ProgressMismatch:
                return "task.progress_mismatch";
            case ReferenceNpcTaskLifecycleJourneyExit.TurnInUnavailable:
                return "task.turnin_unavailable";
            case ReferenceNpcTaskLifecycleJourneyExit.CompleteRequestFailed:
                return "task.complete_request_failed";
            case ReferenceNpcTaskLifecycleJourneyExit.RewardMismatch:
                return "task.reward_mismatch";
            case ReferenceNpcTaskLifecycleJourneyExit.Timeout:
                return "stage.timeout";
            case ReferenceNpcTaskLifecycleJourneyExit.BaselineRecoveryFailed:
                return "task.baseline_recovery_failed";
            case ReferenceNpcTaskLifecycleJourneyExit.WorldRevisionRejected:
                return "task.world_revision_rejected";
            case ReferenceNpcTaskLifecycleJourneyExit.TerminalCancellationFailed:
                return "task.terminal_cancellation_failed";
            case ReferenceNpcTaskLifecycleJourneyExit.DialogTextEdgeWindowRejected:
                return "task.dialog_text_edge_window_rejected";
            default:
                return "client.unexpected";
        }
    }

    public static string SuccessLine(int checkpoints)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} PASS npc=same offer=available accept=authoritative " +
            "baseline=server-authoritative " +
            "combat=model262 pickup=ordinary progress=server-authored " +
            "turnin=authoritative reward=inventory+currency+experience-state " +
            "checkpoints={1}",
            Marker,
            Math.Max(0, checkpoints));
    }

    public static string FailureLine(ReferenceNpcTaskLifecycleJourneyExit exit)
    {
        return Marker + " FAIL code=" + FailureCode(exit);
    }

    public static string ArtifactLine()
    {
        return Marker + " ARTIFACT trace=" + ArtifactFileName;
    }

    public static string PartialArtifactLine(
        ReferenceNpcTaskLifecycleJourneyExit exit,
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

    public static bool TryWriteBootstrapPartial(
        ReferenceNpcTaskLifecycleJourneyExit exit,
        bool cancellationSettled)
    {
        ReferenceNpcTaskLifecycleJourneyPartialArtifact partial =
            new ReferenceNpcTaskLifecycleJourneyPartialArtifact
            {
                failureCode = FailureCode(exit),
                phase = "BeforeDriver",
                terminalCancellationRequested = true,
                terminalCancellationSettled = cancellationSettled,
            };
        return ReferenceMonsterRealCombatTailCheck.WriteJsonAtomically(
            PartialArtifactFileName,
            JsonUtility.ToJson(partial, true) + "\n");
    }

    internal static bool IsAcceptedTrace(ReferenceNpcTaskLifecycleJourneyArtifact value)
    {
        if (value == null || value.prelude == null ||
            value.readyTurnInPrelude == null)
        {
            return false;
        }
        bool directAvailableBaseline = value.baselineAlreadyAvailable &&
            !value.baselineRecoverableActive &&
            !value.baselineAfterPreludeAvailable &&
            !value.baselineAfterReadyTurnInAvailable &&
            !value.baselineRepeatableAvailableDone &&
            !value.prelude.required &&
            !value.readyTurnInPrelude.required &&
            value.baselineAbandonRequestIssuedDelta == 0 &&
            value.baselineAbandonRequestSucceededDelta == 0 &&
            !value.baselineTaskLogReplaced && !value.baselineOfferReplaced;
        bool recoveredResidualBaseline = !value.baselineAlreadyAvailable &&
            value.baselineRecoverableActive &&
            !value.baselineAfterPreludeAvailable &&
            !value.baselineAfterReadyTurnInAvailable &&
            !value.baselineRepeatableAvailableDone &&
            !value.prelude.required &&
            !value.readyTurnInPrelude.required &&
            value.baselineAbandonRequestIssuedDelta == 1 &&
            value.baselineAbandonRequestSucceededDelta == 1 &&
            value.baselineTaskLogReplaced && value.baselineOfferReplaced;
        bool repeatableAvailableDoneBaseline = value.baselineAlreadyAvailable &&
            !value.baselineRecoverableActive &&
            !value.baselineAfterPreludeAvailable &&
            !value.baselineAfterReadyTurnInAvailable &&
            value.baselineRepeatableAvailableDone &&
            !value.prelude.required &&
            !value.readyTurnInPrelude.required &&
            value.baselineAbandonRequestIssuedDelta == 0 &&
            value.baselineAbandonRequestSucceededDelta == 0 &&
            !value.baselineTaskLogReplaced && !value.baselineOfferReplaced;
        bool preludeBaseline = value.baselineAlreadyAvailable &&
            !value.baselineRecoverableActive &&
            value.baselineAfterPreludeAvailable &&
            !value.baselineAfterReadyTurnInAvailable &&
            !value.baselineRepeatableAvailableDone &&
            !value.readyTurnInPrelude.required &&
            value.baselineAbandonRequestIssuedDelta == 0 &&
            value.baselineAbandonRequestSucceededDelta == 0 &&
            !value.baselineTaskLogReplaced && !value.baselineOfferReplaced &&
            IsAcceptedPrelude(value.prelude);
        bool readyTurnInBaseline = value.baselineAlreadyAvailable &&
            !value.baselineRecoverableActive &&
            !value.baselineAfterPreludeAvailable &&
            value.baselineAfterReadyTurnInAvailable &&
            !value.baselineRepeatableAvailableDone &&
            !value.prelude.required &&
            value.baselineAbandonRequestIssuedDelta == 0 &&
            value.baselineAbandonRequestSucceededDelta == 0 &&
            !value.baselineTaskLogReplaced && !value.baselineOfferReplaced &&
            IsAcceptedReadyTurnInPrelude(value.readyTurnInPrelude);
        bool preAcceptOfferVersionAccepted =
            value.openOfferReplacementDelta == 1 &&
            value.openOfferResponseVersionSettled ==
                value.openOfferVersionBaseline + 1 &&
            value.preAcceptOfferVersionSettled ==
                value.openOfferResponseVersionSettled +
                (recoveredResidualBaseline ? 1 : 0) &&
            value.preAcceptOfferVersionObserved ==
                value.preAcceptOfferVersionSettled &&
            value.preAcceptOfferVersionUnchanged &&
            value.acceptOfferVersionBaseline ==
                value.preAcceptOfferVersionSettled;
        if (value.schemaVersion != 1 ||
            value.source != "rebuilt-real-service" ||
            value.initialMapId != InitialMapId || value.combatMapId != CombatMapId ||
            value.npcName != NpcName || value.taskTitle != TaskTitle ||
            value.goalItemName != GoalItemName || value.goalCount != GoalCount ||
            !(directAvailableBaseline || recoveredResidualBaseline ||
              repeatableAvailableDoneBaseline || preludeBaseline ||
              readyTurnInBaseline) ||
            !value.terminalCancellationRequested ||
            !value.terminalCancellationSettled ||
            value.baselineAvailableAuthoritative != true ||
            value.offerAvailable != true || value.acceptDetailAction != true ||
            !preAcceptOfferVersionAccepted ||
            value.acceptRequestIssuedDelta != 1 ||
            value.acceptRequestSucceededDelta != 1 ||
            value.acceptTaskLogReplacementDelta != 1 ||
            value.acceptOfferReplacementDelta != 1 ||
            value.acceptFinalOfferState != 1 ||
            !value.acceptFinalOfferExactState1 ||
            value.acceptConflictingTransition ||
            value.acceptOfferReplacementPattern != FreshAcceptReplacementPattern ||
            value.acceptedAuthoritative != true || value.mapTransitions != 2 ||
            value.travelPathSetCount < 4 ||
            !IsAcceptedTravelLegDiagnostics(
                value.travelLegs,
                value.travelPathSetCount,
                value.travelPathResumeCount,
                value.travelAuthoritativeHitInterruptions,
                value.travelPixelCollisionStops,
                value.travelLegCompletedCount,
                value.travelLegAbandonedCount,
                value.travelMaximumLegRecoveryCount) ||
            value.combatModelId != CombatMonsterModelId ||
            value.combatKills != GoalCount || value.goalPickups != GoalCount ||
            value.attackRequestIssuedDelta < GoalCount ||
            value.attackRequestSucceededDelta < GoalCount ||
            value.pickupRequestIssuedDelta != GoalCount + value.incidentalPickups ||
            value.pickupRequestSucceededDelta != GoalCount + value.incidentalPickups ||
            value.incidentalPickups < 0 ||
            value.incidentalPickups > MaximumIncidentalPickups ||
            value.progressAuthoritative != true || value.readyAuthoritative != true ||
            value.sameNpcReturn != true || value.turnInOffer != true ||
            value.claimRewardAction != true ||
            !IsAcceptedTurnInOpenOfferOrdering(value.turnInOpen) ||
            value.turnInOpen.rewardDialogTextBaselineRevision !=
                value.rewardDialogTextBaselineRevision ||
            value.turnInOpen.rewardMoneyBaseline !=
                value.freshJourneyMoneyBaseline ||
            value.completeRequestIssuedDelta != 1 ||
            value.completeRequestSucceededDelta != 1 ||
            value.activeRemoved != true || value.doneContainsTask != true ||
            value.doneAuthoritative != true || value.repeatableOfferRestored != true ||
            value.rewardFeedback != true ||
            value.inventoryBefore != 0 || value.inventoryReady != GoalCount ||
            value.inventoryAfter != 0 ||
            value.moneyDelta != RewardMoneyDelta ||
            !value.freshJourneyMoneyBaselineCaptured ||
            value.freshJourneyFinalMoney - value.freshJourneyMoneyBaseline !=
                RewardMoneyDelta ||
            value.honorDelta != RewardHonorDelta ||
            value.taskRewardExperienceDelta != 0 ||
            value.combatExperienceAdvanced != true ||
            !(value.firstCombatDistance > 0f) || value.firstCombatDistance > 50f ||
            value.firstCombatLineOfSight != true ||
            !IsAcceptedRewardDialogTextEdges(value) ||
            !IsAcceptedEntityPublication(value.entityPublicationDiagnostic) ||
            value.checkpoints == null || value.checkpoints.Count != GoalCount + 7)
        {
            return false;
        }

        string[] fixedStages =
        {
            "offer.available",
            "task.accepted",
            "travel.combat",
        };
        for (int index = 0; index < fixedStages.Length; index++)
        {
            if (value.checkpoints[index] == null ||
                value.checkpoints[index].stage != fixedStages[index])
            {
                return false;
            }
        }
        for (int count = 1; count <= GoalCount; count++)
        {
            ReferenceNpcTaskLifecycleJourneyCheckpoint checkpoint =
                value.checkpoints[count + 2];
            if (checkpoint == null ||
                checkpoint.stage != "combat.progress." + count.ToString(
                    CultureInfo.InvariantCulture) ||
                checkpoint.collected != count ||
                checkpoint.progress != ExpectedProgress(count) ||
                checkpoint.ready != (count == GoalCount))
            {
                return false;
            }
        }
        string[] tail =
        {
            "task.ready",
            "travel.return",
            "turnin.available",
            "reward.applied",
        };
        for (int index = 0; index < tail.Length; index++)
        {
            ReferenceNpcTaskLifecycleJourneyCheckpoint checkpoint =
                value.checkpoints[GoalCount + 3 + index];
            if (checkpoint == null || checkpoint.stage != tail[index])
            {
                return false;
            }
        }
        return value.checkpoints[0].money == value.freshJourneyMoneyBaseline &&
               value.checkpoints[value.checkpoints.Count - 1].money ==
                   value.freshJourneyFinalMoney;
    }

    internal static bool IsAcceptedRewardDialogTextEdges(
        ReferenceNpcTaskLifecycleJourneyArtifact value)
    {
        if (value == null || value.rewardDialogTextEdges == null ||
            value.rewardDialogTextEdges.Count < 1 ||
            value.rewardDialogTextEdges.Count > 256 ||
            value.rewardDialogTextWindowVersionBaseline < 0 ||
            value.rewardDialogTextWindowVersionObserved <
                value.rewardDialogTextWindowVersionBaseline ||
            value.rewardDialogTextBaselineRevision < 0 ||
            value.rewardDialogTextObservedThroughRevision <
                value.rewardDialogTextBaselineRevision ||
            value.rewardFeedbackEdgeRevision <=
                value.rewardDialogTextBaselineRevision ||
            !string.IsNullOrEmpty(value.rewardDialogTextCursorFailure))
        {
            return false;
        }

        long expectedRevision = value.rewardDialogTextBaselineRevision + 1;
        int exactFeedbackCount = 0;
        for (int index = 0; index < value.rewardDialogTextEdges.Count; index++)
        {
            ReferenceNpcTaskDialogTextEdgeArtifact edge =
                value.rewardDialogTextEdges[index];
            if (edge == null || edge.revision != expectedRevision ||
                edge.displayMode < byte.MinValue || edge.displayMode > byte.MaxValue)
            {
                return false;
            }
            if (edge.displayMode == 6 && edge.text == RewardFeedbackText)
            {
                exactFeedbackCount++;
                if (edge.revision != value.rewardFeedbackEdgeRevision)
                {
                    return false;
                }
            }
            expectedRevision++;
        }

        return exactFeedbackCount == 1 &&
               value.rewardDialogTextObservedThroughRevision ==
                   value.rewardDialogTextEdges[
                       value.rewardDialogTextEdges.Count - 1].revision;
    }

    internal static bool IsAcceptedPrelude(
        ReferenceNpcTaskLifecyclePreludeArtifact value)
    {
        if (value == null || !value.required || !value.completed ||
            value.initialCollected != 1 ||
            value.combatKills != GoalCount - 1 ||
            value.goalPickups != GoalCount - 1 ||
            value.incidentalPickups < 0 ||
            value.incidentalPickups > MaximumIncidentalPickups ||
            value.mapTransitions != 2 || value.travelPathSetCount < 4 ||
            !IsAcceptedTravelLegDiagnostics(
                value.travelLegs,
                value.travelPathSetCount,
                value.travelPathResumeCount,
                value.travelAuthoritativeHitInterruptions,
                value.travelPixelCollisionStops,
                value.travelLegCompletedCount,
                value.travelLegAbandonedCount,
                value.travelMaximumLegRecoveryCount) ||
            value.attackRequestIssuedDelta < GoalCount - 1 ||
            value.attackRequestSucceededDelta < GoalCount - 1 ||
            value.pickupRequestIssuedDelta != GoalCount - 1 +
                value.incidentalPickups ||
            value.pickupRequestSucceededDelta != GoalCount - 1 +
                value.incidentalPickups ||
            !value.progressAuthoritative || !value.readyAuthoritative ||
            value.completeRequestIssuedDelta != 1 ||
            value.completeRequestSucceededDelta != 1 ||
            !IsAcceptedTurnInOpenOfferOrdering(value.turnInOpen) ||
            !value.doneAuthoritative || !value.repeatableOfferRestored ||
            !value.rewardFeedback || value.inventoryReady != GoalCount ||
            value.inventoryAfter != 0 || value.moneyDelta != RewardMoneyDelta ||
            value.honorDelta != RewardHonorDelta ||
            value.taskRewardExperienceDelta != 0 ||
            !value.combatExperienceAdvanced || value.checkpoints == null ||
            value.checkpoints.Count != GoalCount + 5)
        {
            return false;
        }

        if (value.checkpoints[0].stage != "prelude.resume" ||
            value.checkpoints[0].collected != 1 ||
            value.checkpoints[0].progress != ExpectedProgress(1) ||
            value.checkpoints[1].stage != "prelude.travel.combat")
        {
            return false;
        }
        for (int count = 2; count <= GoalCount; count++)
        {
            ReferenceNpcTaskLifecycleJourneyCheckpoint checkpoint =
                value.checkpoints[count];
            if (checkpoint == null || checkpoint.stage !=
                    "prelude.combat.progress." + count.ToString(
                        CultureInfo.InvariantCulture) ||
                checkpoint.collected != count ||
                checkpoint.progress != ExpectedProgress(count) ||
                checkpoint.ready != (count == GoalCount))
            {
                return false;
            }
        }

        string[] tail =
        {
            "prelude.task.ready",
            "prelude.travel.return",
            "prelude.turnin.available",
            "prelude.reward.applied",
        };
        for (int index = 0; index < tail.Length; index++)
        {
            ReferenceNpcTaskLifecycleJourneyCheckpoint checkpoint =
                value.checkpoints[GoalCount + 1 + index];
            if (checkpoint == null || checkpoint.stage != tail[index])
            {
                return false;
            }
        }
        return true;
    }

    internal static bool IsAcceptedReadyTurnInPrelude(
        ReferenceNpcTaskLifecycleReadyTurnInPreludeArtifact value)
    {
        if (value == null || !value.required || !value.completed ||
            value.initialCollected != GoalCount || !value.activeReady ||
            !value.state2Offer || !value.claimRewardAction ||
            !IsAcceptedTurnInOpenOfferOrdering(value.turnInOpen) ||
            value.turnInOpen.rewardMoneyBaseline != value.moneyBaseline ||
            value.turnInOpen.rewardMoneyFinal != value.finalMoney ||
            value.completeRequestIssuedDelta != 1 ||
            value.completeRequestSucceededDelta != 1 ||
            !value.doneAuthoritative || !value.repeatableOfferRestored ||
            !value.rewardFeedback || value.inventoryAfter != 0 ||
            !value.moneyBaselineCaptured ||
            value.finalMoney - value.moneyBaseline != RewardMoneyDelta ||
            value.moneyDelta != RewardMoneyDelta ||
            value.honorDelta != RewardHonorDelta ||
            value.taskRewardExperienceDelta != 0 || value.checkpoints == null ||
            value.checkpoints.Count != 3)
        {
            return false;
        }

        string[] stages =
        {
            "ready10.resume",
            "ready10.turnin.available",
            "ready10.reward.applied",
        };
        for (int index = 0; index < stages.Length; index++)
        {
            ReferenceNpcTaskLifecycleJourneyCheckpoint checkpoint =
                value.checkpoints[index];
            if (checkpoint == null || checkpoint.stage != stages[index])
            {
                return false;
            }
        }
        return value.checkpoints[0].collected == GoalCount &&
               value.checkpoints[0].progress == ExpectedProgress(GoalCount) &&
               value.checkpoints[0].ready &&
               value.checkpoints[1].collected == GoalCount &&
               value.checkpoints[1].ready &&
               value.checkpoints[2].collected == 0 &&
               value.checkpoints[0].money == value.moneyBaseline &&
               value.checkpoints[1].money == value.moneyBaseline &&
               value.checkpoints[2].money == value.finalMoney;
    }

    internal static bool IsAcceptedTravelLegDiagnostics(
        IReadOnlyList<ReferenceNpcTaskTravelLegArtifact> legs,
        int totalPathSets,
        int totalRecoveries,
        int totalHitInterruptions,
        int totalPixelCollisionStops,
        int completedLegs,
        int abandonedLegs,
        int maximumLegRecoveries)
    {
        if (legs == null || legs.Count < 6 || legs.Count > MaximumTravelLegs ||
            totalPathSets < 6 || totalRecoveries < 0 ||
            totalHitInterruptions != totalRecoveries ||
            totalPixelCollisionStops != 0 || completedLegs < 6 ||
            abandonedLegs < 0 || maximumLegRecoveries < 0 ||
            maximumLegRecoveries > MaximumTravelHitRecoveries)
        {
            return false;
        }

        int pathSets = 0;
        int recoveries = 0;
        int hitInterruptions = 0;
        int collisionStops = 0;
        int arrived = 0;
        int abandoned = 0;
        int maximum = 0;
        int outbound = 0;
        int combatArea = 0;
        int combatReturnAnchor = 0;
        int returnGate = 0;
        int returnAnchor = 0;
        int returnNpc = 0;
        for (int index = 0; index < legs.Count; index++)
        {
            ReferenceNpcTaskTravelLegArtifact leg = legs[index];
            if (leg == null || leg.sequence != index + 1 ||
                string.IsNullOrEmpty(leg.kind) || string.IsNullOrEmpty(leg.key) ||
                leg.pathSetCount < 1 ||
                leg.recoveryCount < 0 ||
                leg.recoveryCount > MaximumTravelHitRecoveries ||
                leg.authoritativeHitInterruptions != leg.recoveryCount ||
                leg.pixelCollisionStops != 0 || leg.arrived == leg.abandoned ||
                float.IsNaN(leg.targetX) || float.IsInfinity(leg.targetX) ||
                float.IsNaN(leg.targetY) || float.IsInfinity(leg.targetY))
            {
                return false;
            }

            pathSets += leg.pathSetCount;
            recoveries += leg.recoveryCount;
            hitInterruptions += leg.authoritativeHitInterruptions;
            collisionStops += leg.pixelCollisionStops;
            maximum = Math.Max(maximum, leg.recoveryCount);
            if (leg.arrived)
            {
                arrived++;
            }
            else
            {
                abandoned++;
            }

            if (leg.kind == ReferenceNpcTaskTravelLegKind.WaitCombatMap.ToString())
            {
                outbound += leg.arrived ? 1 : -MaximumTravelLegs;
            }
            else if (leg.kind == ReferenceNpcTaskTravelLegKind.WaitCombatArea.ToString())
            {
                combatArea += leg.arrived ? 1 : -MaximumTravelLegs;
            }
            else if (leg.kind ==
                     ReferenceNpcTaskTravelLegKind.WaitCombatReturnAnchor.ToString())
            {
                combatReturnAnchor += leg.arrived ? 1 : -MaximumTravelLegs;
            }
            else if (leg.kind == ReferenceNpcTaskTravelLegKind.WaitReturnMap.ToString())
            {
                returnGate += leg.arrived ? 1 : -MaximumTravelLegs;
            }
            else if (leg.kind ==
                     ReferenceNpcTaskTravelLegKind.WaitNpcReturnAnchor.ToString())
            {
                returnAnchor += leg.arrived ? 1 : -MaximumTravelLegs;
            }
            else if (leg.kind == ReferenceNpcTaskTravelLegKind.WaitNpcArrival.ToString())
            {
                returnNpc += leg.arrived ? 1 : -MaximumTravelLegs;
            }
            else if (leg.kind !=
                     ReferenceNpcTaskTravelLegKind.ApproachCombatTarget.ToString())
            {
                return false;
            }
            if (leg.kind ==
                    ReferenceNpcTaskTravelLegKind.ApproachCombatTarget.ToString()
                ? leg.targetEntityId <= 0 || leg.key != leg.kind + ":" +
                    leg.targetEntityId.ToString(CultureInfo.InvariantCulture)
                : leg.targetEntityId != 0 || leg.key != leg.kind)
            {
                return false;
            }
        }

        return pathSets == totalPathSets && recoveries == totalRecoveries &&
               hitInterruptions == totalHitInterruptions &&
               collisionStops == totalPixelCollisionStops &&
               arrived == completedLegs && abandoned == abandonedLegs &&
               maximum == maximumLegRecoveries && outbound == 1 &&
               combatArea == 1 && combatReturnAnchor == 1 &&
               returnGate == 1 && returnAnchor == 1 && returnNpc == 1;
    }

    internal static bool IsAcceptedEntityPublication(
        ReferenceNpcTaskLifecycleTargetDiagnostic value)
    {
        return value != null && value.sceneGeneration >= 2 &&
               value.publicationCaptureStable &&
               value.publicationMapId == CombatMapId &&
               value.decodedSpawnFrameCount >= 128 &&
               value.admittedSpawnFrameCount >= 128 &&
               value.rejectedMapGuardSpawnFrameCount == 0 &&
               value.publishedSnapshotVersion > 0 &&
               value.publishedEntityCount > 0 &&
               value.worldEntityCount == value.publishedEntityCount &&
               value.worldModel262Count > 0 &&
               value.windowBuildGeneration == value.sceneGeneration &&
               value.actorRebuildGeneration > 0 &&
               value.actorSceneGeneration == value.sceneGeneration &&
               value.actorSnapshotMapId == CombatMapId &&
               value.actorSnapshotVersion == value.publishedSnapshotVersion &&
               value.actorSnapshotEntityCount == value.publishedEntityCount &&
               value.actorCount > 0 && value.actorModel262Count > 0;
    }

    internal static bool IsSettledOpenOfferReplacement(
        long offerVersionDelta,
        bool admittedSemanticBaseline)
    {
        return offerVersionDelta == 1 && admittedSemanticBaseline;
    }

    internal static bool IsPostCompleteRewardFeedbackReady(
        long offerVersionBaseline,
        long offerVersionObserved,
        bool dialogVisible,
        bool dialogInSubView,
        bool singleLeaveOption,
        bool rewardFeedbackObserved,
        bool exactAvailableOffer)
    {
        return offerVersionBaseline >= 0 &&
               offerVersionObserved == offerVersionBaseline + 1 &&
               dialogVisible && dialogInSubView && singleLeaveOption &&
               rewardFeedbackObserved && exactAvailableOffer;
    }

    internal static bool IsAcceptedTurnInOpenOfferOrdering(
        ReferenceNpcTaskLifecycleTurnInOpenArtifact value)
    {
        return value != null &&
               value.turnInOpenOffersVersionBaseline >= 0 &&
               value.turnInOpenOfferReplacementDelta == 1 &&
               value.turnInOpenOffersResponseVersionSettled ==
                   value.turnInOpenOffersVersionBaseline + 1 &&
               value.turnInOpenFinalState2Ready &&
               value.preCompleteOffersVersionObserved ==
                   value.turnInOpenOffersResponseVersionSettled &&
               value.preCompleteOfferVersionUnchanged &&
               value.rewardTaskLogVersionBaseline >= 0 &&
               value.rewardInventoryVersionBaseline >= 0 &&
               value.rewardOffersVersionBaseline ==
                   value.turnInOpenOffersResponseVersionSettled &&
               value.rewardDialogTextBaselineRevision >= 0 &&
               value.rewardMoneyBaselineCaptured &&
               value.rewardMoneyBaseline >= 0 &&
               value.rewardHonorBaseline >= 0 &&
               value.rewardTaskLogReplacementDelta == 1 &&
               value.rewardInventoryReplacementDelta == 1 &&
               value.rewardOfferReplacementDelta == 1 &&
               value.rewardFinalExactState0 &&
               value.rewardMoneyFinal - value.rewardMoneyBaseline ==
                   RewardMoneyDelta &&
               value.rewardHonorFinal == value.rewardHonorBaseline;
    }

    internal static bool IsAcceptedFreshAcceptReplacementOrdering(
        long acceptIssuedDelta,
        long acceptSucceededDelta,
        long taskLogReplacementDelta,
        long offerReplacementDelta,
        bool activeExactZeroOfTen,
        bool finalExactState1Offer,
        bool doneAbsent,
        bool goalItemsAbsent,
        bool conflictingTransition,
        out string pattern)
    {
        bool accepted = acceptIssuedDelta == 1 && acceptSucceededDelta == 1 &&
            taskLogReplacementDelta == 1 && offerReplacementDelta == 1 &&
            activeExactZeroOfTen && finalExactState1Offer && doneAbsent &&
            goalItemsAbsent && !conflictingTransition;
        pattern = accepted
            ? FreshAcceptReplacementPattern
            : "task" + SignedDelta(taskLogReplacementDelta) +
              "/offer" + SignedDelta(offerReplacementDelta) +
              "/final-" + (finalExactState1Offer ? "state1" : "not-state1") +
              "/conflict-" + (conflictingTransition ? "yes" : "no");
        return accepted;
    }

    private static string SignedDelta(long value)
    {
        return value >= 0
            ? "+" + value.ToString(CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string ExpectedProgress(int count)
    {
        return "已收集" + GoalItemName + " " +
               count.ToString(CultureInfo.InvariantCulture) + "/" +
               GoalCount.ToString(CultureInfo.InvariantCulture);
    }

    internal static bool UsesTravelDeadline(string phaseName)
    {
        return phaseName == "WaitCombatMap" ||
               phaseName == "WaitCombatArea" ||
               phaseName == "ApproachCombatTarget" ||
               phaseName == "WaitCombatReturnAnchor" ||
               phaseName == "WaitReturnMap" ||
               phaseName == "WaitNpcReturnAnchor" ||
               phaseName == "WaitNpcArrival";
    }

    internal static float WaitSecondsForPhase(string phaseName)
    {
        return UsesTravelDeadline(phaseName)
            ? TravelWaitSeconds
            : StateWaitSeconds;
    }

    internal static bool TryMarkActiveTravelDeadlineExpired(
        string phaseName,
        float now,
        float phaseDeadline,
        ReferenceWorldLocalPlayerDoll player)
    {
        return UsesTravelDeadline(phaseName) &&
               !float.IsNaN(now) && !float.IsInfinity(now) &&
               !float.IsNaN(phaseDeadline) && !float.IsInfinity(phaseDeadline) &&
               now >= phaseDeadline && player != null && player.MovementActive &&
               player.MarkActiveMovementDeadlineExpiredForIndependentRuntime();
    }

    internal static bool IsAdmittedTravelHit(
        WorldCombatResultState value,
        int playerEntityId,
        long pathBaselineRevision,
        long lastRecoveredRevision)
    {
        return value != null && playerEntityId != 0 &&
               value.Revision > pathBaselineRevision &&
               value.Revision > lastRecoveredRevision &&
               value.TargetEntityId == playerEntityId && !value.Missed &&
               !value.Fatal && value.TargetHitPoints > 0;
    }

    internal static ReferenceNpcTaskTravelRecoveryDecision EvaluateTravelRecovery(
        WorldCombatResultState value,
        int playerEntityId,
        long pathBaselineRevision,
        long lastRecoveredRevision,
        ReferenceWorldPlayerMovementStopReason stopReason,
        bool hurtActionActive,
        int completedRecoveries)
    {
        if (stopReason != ReferenceWorldPlayerMovementStopReason.AuthoritativeHurt ||
            completedRecoveries < 0 ||
            completedRecoveries >= MaximumTravelHitRecoveries ||
            !IsAdmittedTravelHit(
                value,
                playerEntityId,
                pathBaselineRevision,
                lastRecoveredRevision))
        {
            return ReferenceNpcTaskTravelRecoveryDecision.Fail;
        }
        return hurtActionActive
            ? ReferenceNpcTaskTravelRecoveryDecision.WaitForHurtEnd
            : ReferenceNpcTaskTravelRecoveryDecision.ReissueSameDestination;
    }

    internal static bool IsRecoverySetPathAccepted(
        ReferenceWorldSetPathOutcome outcome,
        bool moveToTarget)
    {
        return moveToTarget &&
               outcome != ReferenceWorldSetPathOutcome.Stopped &&
               outcome != ReferenceWorldSetPathOutcome.RetryPath;
    }

    internal static bool IsInitialSetPathAccepted(
        ReferenceWorldSetPathOutcome outcome,
        bool moveToTarget)
    {
        return moveToTarget && outcome != ReferenceWorldSetPathOutcome.Stopped;
    }

    internal static bool IsFixedCitySetPathAccepted(
        ReferenceWorldSetPathOutcome outcome,
        bool moveToTarget,
        int pendingHops)
    {
        return moveToTarget && pendingHops == 0 &&
               outcome == ReferenceWorldSetPathOutcome.PrimaryPath;
    }

    internal static bool IsRewardMoneyTransitionAccepted(
        bool preludeMode,
        bool readyTurnInPreludeMode,
        bool rewardMoneyBaselineCaptured,
        long rewardMoneyBaseline,
        bool freshJourneyMoneyBaselineCaptured,
        long freshJourneyMoneyBaseline,
        long finalMoney)
    {
        if (!rewardMoneyBaselineCaptured ||
            finalMoney - rewardMoneyBaseline != RewardMoneyDelta)
        {
            return false;
        }

        return preludeMode || readyTurnInPreludeMode ||
               freshJourneyMoneyBaselineCaptured &&
               finalMoney - freshJourneyMoneyBaseline == RewardMoneyDelta;
    }
}

[Serializable]
internal sealed class ReferenceNpcTaskLifecycleJourneyArtifact
{
    public int schemaVersion = 1;
    public string source = "rebuilt-real-service";
    public int initialMapId;
    public int combatMapId;
    public string npcName = string.Empty;
    public string taskTitle = string.Empty;
    public string goalItemName = string.Empty;
    public int goalCount;
    public bool baselineAlreadyAvailable;
    public bool baselineRecoverableActive;
    public bool baselineAfterPreludeAvailable;
    public bool baselineAfterReadyTurnInAvailable;
    public bool baselineRepeatableAvailableDone;
    public long baselineAbandonRequestIssuedDelta;
    public long baselineAbandonRequestSucceededDelta;
    public bool baselineTaskLogReplaced;
    public bool baselineOfferReplaced;
    public bool baselineAvailableAuthoritative;
    public long openOfferVersionBaseline;
    public long openOfferResponseVersionSettled;
    public long openOfferReplacementDelta;
    public long preAcceptOfferVersionSettled;
    public long preAcceptOfferVersionObserved;
    public bool preAcceptOfferVersionUnchanged;
    public bool offerAvailable;
    public bool acceptDetailAction;
    public long acceptRequestIssuedDelta;
    public long acceptRequestSucceededDelta;
    public long acceptTaskLogVersionBaseline;
    public long acceptOfferVersionBaseline;
    public long acceptTaskLogReplacementDelta;
    public long acceptOfferReplacementDelta;
    public int acceptFinalOfferState = -1;
    public bool acceptFinalOfferExactState1;
    public bool acceptConflictingTransition;
    public string acceptOfferReplacementPattern = string.Empty;
    public bool acceptedAuthoritative;
    public int mapTransitions;
    public int travelPathSetCount;
    public int travelPathResumeCount;
    public int travelAuthoritativeHitInterruptions;
    public int travelPixelCollisionStops;
    public int travelLegCompletedCount;
    public int travelLegAbandonedCount;
    public int travelMaximumLegRecoveryCount;
    public List<ReferenceNpcTaskTravelLegArtifact> travelLegs =
        new List<ReferenceNpcTaskTravelLegArtifact>();
    public int combatModelId;
    public int combatKills;
    public int goalPickups;
    public long attackRequestIssuedDelta;
    public long attackRequestSucceededDelta;
    public long pickupRequestIssuedDelta;
    public long pickupRequestSucceededDelta;
    public int incidentalPickups;
    public bool progressAuthoritative;
    public bool readyAuthoritative;
    public bool sameNpcReturn;
    public bool turnInOffer;
    public bool claimRewardAction;
    public ReferenceNpcTaskLifecycleTurnInOpenArtifact turnInOpen =
        new ReferenceNpcTaskLifecycleTurnInOpenArtifact();
    public long completeRequestIssuedDelta;
    public long completeRequestSucceededDelta;
    public bool doneAuthoritative;
    public bool activeRemoved;
    public bool doneContainsTask;
    public bool repeatableOfferRestored;
    public bool rewardFeedback;
    public int inventoryBefore;
    public int inventoryReady;
    public int inventoryAfter;
    public long moneyDelta;
    public bool freshJourneyMoneyBaselineCaptured;
    public long freshJourneyMoneyBaseline;
    public long freshJourneyFinalMoney;
    public long honorDelta;
    public int taskRewardExperienceDelta;
    public bool combatExperienceAdvanced;
    public bool terminalCancellationRequested;
    public bool terminalCancellationSettled;
    public ReferenceNpcTaskLifecyclePreludeArtifact prelude =
        new ReferenceNpcTaskLifecyclePreludeArtifact();
    public ReferenceNpcTaskLifecycleReadyTurnInPreludeArtifact readyTurnInPrelude =
        new ReferenceNpcTaskLifecycleReadyTurnInPreludeArtifact();
    public float firstCombatDistance;
    public bool firstCombatLineOfSight;
    public long rewardDialogTextWindowVersionBaseline;
    public long rewardDialogTextBaselineRevision;
    public long rewardDialogTextWindowVersionObserved;
    public long rewardDialogTextObservedThroughRevision;
    public long rewardFeedbackEdgeRevision;
    public string rewardDialogTextCursorFailure = string.Empty;
    public List<ReferenceNpcTaskDialogTextEdgeArtifact> rewardDialogTextEdges =
        new List<ReferenceNpcTaskDialogTextEdgeArtifact>();
    public ReferenceNpcTaskLifecycleTargetDiagnostic entityPublicationDiagnostic =
        new ReferenceNpcTaskLifecycleTargetDiagnostic();
    public List<ReferenceNpcTaskLifecycleJourneyCheckpoint> checkpoints =
        new List<ReferenceNpcTaskLifecycleJourneyCheckpoint>();
}

[Serializable]
internal sealed class ReferenceNpcTaskLifecyclePreludeArtifact
{
    public bool required;
    public bool completed;
    public int initialCollected;
    public int combatKills;
    public int goalPickups;
    public int incidentalPickups;
    public int mapTransitions;
    public int travelPathSetCount;
    public int travelPathResumeCount;
    public int travelAuthoritativeHitInterruptions;
    public int travelPixelCollisionStops;
    public int travelLegCompletedCount;
    public int travelLegAbandonedCount;
    public int travelMaximumLegRecoveryCount;
    public List<ReferenceNpcTaskTravelLegArtifact> travelLegs =
        new List<ReferenceNpcTaskTravelLegArtifact>();
    public long attackRequestIssuedDelta;
    public long attackRequestSucceededDelta;
    public long pickupRequestIssuedDelta;
    public long pickupRequestSucceededDelta;
    public long completeRequestIssuedDelta;
    public long completeRequestSucceededDelta;
    public ReferenceNpcTaskLifecycleTurnInOpenArtifact turnInOpen =
        new ReferenceNpcTaskLifecycleTurnInOpenArtifact();
    public bool progressAuthoritative;
    public bool readyAuthoritative;
    public bool doneAuthoritative;
    public bool repeatableOfferRestored;
    public bool rewardFeedback;
    public int inventoryReady;
    public int inventoryAfter;
    public long moneyDelta;
    public long honorDelta;
    public int taskRewardExperienceDelta;
    public bool combatExperienceAdvanced;
    public List<ReferenceNpcTaskLifecycleJourneyCheckpoint> checkpoints =
        new List<ReferenceNpcTaskLifecycleJourneyCheckpoint>();
}

[Serializable]
internal sealed class ReferenceNpcTaskLifecycleReadyTurnInPreludeArtifact
{
    public bool required;
    public bool completed;
    public int initialCollected;
    public bool activeReady;
    public bool state2Offer;
    public bool claimRewardAction;
    public ReferenceNpcTaskLifecycleTurnInOpenArtifact turnInOpen =
        new ReferenceNpcTaskLifecycleTurnInOpenArtifact();
    public long completeRequestIssuedDelta;
    public long completeRequestSucceededDelta;
    public bool doneAuthoritative;
    public bool repeatableOfferRestored;
    public bool rewardFeedback;
    public int inventoryAfter;
    public bool moneyBaselineCaptured;
    public long moneyBaseline;
    public long finalMoney;
    public long moneyDelta;
    public long honorDelta;
    public int taskRewardExperienceDelta;
    public List<ReferenceNpcTaskLifecycleJourneyCheckpoint> checkpoints =
        new List<ReferenceNpcTaskLifecycleJourneyCheckpoint>();
}

[Serializable]
internal sealed class ReferenceNpcTaskLifecycleTurnInOpenArtifact
{
    public long turnInOpenOffersVersionBaseline;
    public long turnInOpenOffersResponseVersionSettled;
    public long turnInOpenOfferReplacementDelta;
    public bool turnInOpenFinalState2Ready;
    public long preCompleteOffersVersionObserved;
    public bool preCompleteOfferVersionUnchanged;
    public long rewardTaskLogVersionBaseline;
    public long rewardInventoryVersionBaseline;
    public long rewardOffersVersionBaseline;
    public long rewardDialogTextBaselineRevision;
    public bool rewardMoneyBaselineCaptured;
    public long rewardMoneyBaseline;
    public long rewardHonorBaseline;
    public long rewardTaskLogReplacementDelta;
    public long rewardInventoryReplacementDelta;
    public long rewardOfferReplacementDelta;
    public bool rewardFinalExactState0;
    public long rewardMoneyFinal;
    public long rewardHonorFinal;
}

[Serializable]
internal sealed class ReferenceNpcTaskLifecycleJourneyCheckpoint
{
    public string stage = string.Empty;
    public int mapId;
    public int collected;
    public string progress = string.Empty;
    public bool ready;
    public long taskLogVersion;
    public long inventoryVersion;
    public long playerStatusVersion;
    public long money;
    public long honor;
}

[Serializable]
internal sealed class ReferenceNpcTaskDialogTextEdgeArtifact
{
    public long revision;
    public string text = string.Empty;
    public int displayMode;
}

[Serializable]
internal sealed class ReferenceNpcTaskLifecycleJourneyPartialArtifact
{
    public int schemaVersion = 1;
    public string source = "rebuilt-real-service-partial";
    public string failureCode = string.Empty;
    public string phase = string.Empty;
    public int mapId;
    public int combatKills;
    public int goalPickups;
    public int incidentalPickups;
    public int collected;
    public int checkpointCount;
    public bool terminalCancellationRequested;
    public bool terminalCancellationSettled;
    public bool baselineAlreadyAvailable;
    public bool baselineRecoverableActive;
    public bool baselineAfterPreludeAvailable;
    public bool baselineAfterReadyTurnInAvailable;
    public bool baselineRepeatableAvailableDone;
    public long baselineAbandonRequestIssuedDelta;
    public long baselineAbandonRequestSucceededDelta;
    public bool baselineTaskLogReplaced;
    public bool baselineOfferReplaced;
    public bool baselineAvailableAuthoritative;
    public long openOfferVersionBaseline;
    public long openOfferResponseVersionSettled;
    public long openOfferReplacementDelta;
    public long preAcceptOfferVersionSettled;
    public long preAcceptOfferVersionObserved;
    public bool preAcceptOfferVersionUnchanged;
    public ReferenceNpcTaskLifecyclePreludeArtifact prelude =
        new ReferenceNpcTaskLifecyclePreludeArtifact();
    public ReferenceNpcTaskLifecycleReadyTurnInPreludeArtifact readyTurnInPrelude =
        new ReferenceNpcTaskLifecycleReadyTurnInPreludeArtifact();
    public ReferenceNpcTaskLifecycleTurnInOpenArtifact turnInOpen =
        new ReferenceNpcTaskLifecycleTurnInOpenArtifact();
    public long acceptRequestIssuedDelta;
    public long acceptRequestSucceededDelta;
    public long acceptTaskLogVersionBaseline;
    public long acceptOfferVersionBaseline;
    public long acceptTaskLogReplacementDelta;
    public long acceptOfferReplacementDelta;
    public int acceptFinalOfferState = -1;
    public bool acceptFinalOfferExactState1;
    public bool acceptConflictingTransition;
    public string acceptOfferReplacementPattern = string.Empty;
    public long completeRequestIssuedDelta;
    public long completeRequestSucceededDelta;
    public long attackRequestIssuedDelta;
    public long attackRequestSucceededDelta;
    public long pickupRequestIssuedDelta;
    public long pickupRequestSucceededDelta;
    public float firstCombatDistance;
    public bool firstCombatLineOfSight;
    public long rewardDialogTextWindowVersionBaseline;
    public long rewardDialogTextBaselineRevision;
    public long rewardDialogTextWindowVersionObserved;
    public long rewardDialogTextObservedThroughRevision;
    public long rewardFeedbackEdgeRevision;
    public string rewardDialogTextCursorFailure = string.Empty;
    public List<ReferenceNpcTaskDialogTextEdgeArtifact> rewardDialogTextEdges =
        new List<ReferenceNpcTaskDialogTextEdgeArtifact>();
    public float playerFinalX;
    public float playerFinalY;
    public float pathTargetX;
    public float pathTargetY;
    public float pathRemainingDistance;
    public bool pathArrivalPredicate;
    public bool pathMovementActive;
    public int pathIndex;
    public int pathCount;
    public int pathPendingHops;
    public int pathSetOutcome;
    public string pathSetOutcomeName = string.Empty;
    public bool pathSetMoveToTarget;
    public int pathStopReason;
    public string pathStopReasonName = string.Empty;
    public bool pathFinalPixelWalkable;
    public long pathLastLocalHitRevision;
    public int pathLastLocalHitAttackerEntityId;
    public bool pathLastLocalHitFatal;
    public int pathLastLocalHitPoints;
    public int travelPathSetCount;
    public int travelPathResumeCount;
    public int travelAuthoritativeHitInterruptions;
    public int travelPixelCollisionStops;
    public int travelLegCompletedCount;
    public int travelLegAbandonedCount;
    public int travelMaximumLegRecoveryCount;
    public List<ReferenceNpcTaskTravelLegArtifact> travelLegs =
        new List<ReferenceNpcTaskTravelLegArtifact>();
    public string currentTravelLegKey = string.Empty;
    public int currentTravelLegRecoveryCount;
    public ReferenceNpcTaskLifecycleTargetDiagnostic targetDiagnostic =
        new ReferenceNpcTaskLifecycleTargetDiagnostic();
    public List<ReferenceNpcTaskLifecycleMovementFrame> movementFrames =
        new List<ReferenceNpcTaskLifecycleMovementFrame>();
}

[Serializable]
internal sealed class ReferenceNpcTaskLifecycleMovementFrame
{
    public long sequence;
    public string phase = string.Empty;
    public float playerX;
    public float playerY;
    public float targetX;
    public float targetY;
    public float remainingDistance;
    public bool arrivalPredicate;
    public bool movementActive;
    public int pathIndex;
    public int pathCount;
    public int pendingHops;
    public int setPathOutcome;
    public string setPathOutcomeName = string.Empty;
    public bool setPathMoveToTarget;
    public int stopReason;
    public string stopReasonName = string.Empty;
    public bool finalPixelWalkable;
    public bool actionActive;
    public int actionPoseKind;
    public long localCombatRevision;
    public bool localHit;
    public int localHitAttackerEntityId;
    public bool localHitFatal;
    public int localHitPoints;
}

[Serializable]
internal sealed class ReferenceNpcTaskTravelLegArtifact
{
    public int sequence;
    public string kind = string.Empty;
    public string key = string.Empty;
    public int targetEntityId;
    public float targetX;
    public float targetY;
    public int pathSetCount;
    public int recoveryCount;
    public int authoritativeHitInterruptions;
    public int pixelCollisionStops;
    public bool arrived;
    public bool abandoned;
}

internal sealed class ReferenceNpcTaskTravelLegLedger
{
    private readonly List<ReferenceNpcTaskTravelLegArtifact> legs;
    private ReferenceNpcTaskTravelLegArtifact active;

    internal ReferenceNpcTaskTravelLegLedger(
        List<ReferenceNpcTaskTravelLegArtifact> legs)
    {
        this.legs = legs ?? throw new ArgumentNullException(nameof(legs));
    }

    internal int ActiveRecoveryCount => active == null ? 0 : active.recoveryCount;
    internal string ActiveKey => active == null ? string.Empty : active.key;
    internal ReferenceNpcTaskTravelLegKind ActiveKind => active == null
        ? ReferenceNpcTaskTravelLegKind.None
        : (ReferenceNpcTaskTravelLegKind)Enum.Parse(
            typeof(ReferenceNpcTaskTravelLegKind),
            active.kind);
    internal int TotalPathSetCount { get; private set; }
    internal int TotalRecoveryCount { get; private set; }
    internal int TotalAuthoritativeHitInterruptions { get; private set; }
    internal int TotalPixelCollisionStops { get; private set; }
    internal int CompletedLegCount { get; private set; }
    internal int AbandonedLegCount { get; private set; }
    internal int MaximumLegRecoveryCount { get; private set; }

    internal bool Begin(
        ReferenceNpcTaskTravelLegKind kind,
        int targetEntityId,
        float targetX,
        float targetY)
    {
        if (active != null || kind == ReferenceNpcTaskTravelLegKind.None ||
            legs.Count >= ReferenceNpcTaskLifecycleJourneyCheck.MaximumTravelLegs ||
            (kind == ReferenceNpcTaskTravelLegKind.ApproachCombatTarget &&
             targetEntityId <= 0) ||
            (kind != ReferenceNpcTaskTravelLegKind.ApproachCombatTarget &&
             targetEntityId != 0) ||
            float.IsNaN(targetX) || float.IsInfinity(targetX) ||
            float.IsNaN(targetY) || float.IsInfinity(targetY))
        {
            return false;
        }

        active = new ReferenceNpcTaskTravelLegArtifact
        {
            sequence = legs.Count + 1,
            kind = kind.ToString(),
            key = kind == ReferenceNpcTaskTravelLegKind.ApproachCombatTarget
                ? kind + ":" + targetEntityId.ToString(CultureInfo.InvariantCulture)
                : kind.ToString(),
            targetEntityId = targetEntityId,
            targetX = targetX,
            targetY = targetY,
        };
        legs.Add(active);
        return true;
    }

    internal bool UpdateDestination(float targetX, float targetY)
    {
        if (active == null || float.IsNaN(targetX) || float.IsInfinity(targetX) ||
            float.IsNaN(targetY) || float.IsInfinity(targetY))
        {
            return false;
        }
        active.targetX = targetX;
        active.targetY = targetY;
        return true;
    }

    internal bool RecordPathSet(bool recovery)
    {
        if (active == null || recovery && active.recoveryCount >=
            ReferenceNpcTaskLifecycleJourneyCheck.MaximumTravelHitRecoveries)
        {
            return false;
        }
        active.pathSetCount++;
        TotalPathSetCount++;
        if (recovery)
        {
            active.recoveryCount++;
            TotalRecoveryCount++;
            MaximumLegRecoveryCount = Math.Max(
                MaximumLegRecoveryCount,
                active.recoveryCount);
        }
        return true;
    }

    internal bool RecordAuthoritativeHitInterruption()
    {
        if (active == null)
        {
            return false;
        }
        active.authoritativeHitInterruptions++;
        TotalAuthoritativeHitInterruptions++;
        return true;
    }

    internal bool RecordPixelCollisionStop()
    {
        if (active == null)
        {
            return false;
        }
        active.pixelCollisionStops++;
        TotalPixelCollisionStops++;
        return true;
    }

    internal bool Complete(ReferenceNpcTaskTravelLegKind expected)
    {
        if (active == null || ActiveKind != expected || active.pathSetCount < 1)
        {
            return false;
        }
        active.arrived = true;
        CompletedLegCount++;
        active = null;
        return true;
    }

    internal bool Abandon(ReferenceNpcTaskTravelLegKind expected)
    {
        if (active == null || ActiveKind != expected)
        {
            return false;
        }
        active.abandoned = true;
        AbandonedLegCount++;
        active = null;
        return true;
    }
}

[Serializable]
internal sealed class ReferenceNpcTaskLifecycleTargetDiagnostic
{
    public long sceneGeneration;
    public int publicationMapId;
    public long decodedSpawnFrameCount;
    public long admittedSpawnFrameCount;
    public long rejectedMapGuardSpawnFrameCount;
    public long receivedRemovalFrameCount;
    public long publishedSnapshotVersion;
    public int publishedEntityCount;
    public long windowBuildGeneration;
    public long actorRebuildGeneration;
    public long actorSceneGeneration;
    public int actorSnapshotMapId;
    public long actorSnapshotVersion;
    public int actorSnapshotEntityCount;
    public bool publicationCaptureStable;
    public int worldEntityCount;
    public int worldMonsterCount;
    public int worldModel206Count;
    public int worldModel262Count;
    public int worldModel274Count;
    public int worldOtherModelCount;
    public int actorCount;
    public int actorModel206Count;
    public int actorModel262Count;
    public int actorModel274Count;
    public int actorOtherModelCount;
    public int admissibleModel262Count;
    public int rejectedMissingActorOrState;
    public int rejectedModelMismatch;
    public int rejectedSourceOwnerNotLive;
    public int rejectedDying;
    public int rejectedRootMissing;
    public int rejectedColliderMissing;
    public int rejectedColliderDisabled;
    public int rejectedFriendlyOrOwned;
    public int rejectedNonPositiveHitPoints;
    public int model262RendererMissing;
    public int model262SpriteMissing;
    public int model262CombatWorldMissing;
    public int model262WorldStateMissing;
    public int model262SourceOwnerLiveCount;
    public int model262NonDyingCount;
    public int model262RootPresentCount;
    public int model262RendererPresentCount;
    public int model262SpritePresentCount;
    public int model262ColliderPresentCount;
    public int model262ColliderEnabledCount;
    public int model262HostileCount;
    public int model262UnownedCount;
    public int model262PositiveHitPointsCount;
    public int model262CombatWorldPresentCount;
    public int model262WorldStatePresentCount;
    public int model262PathEvaluatedCount;
    public int model262PathAdmissibleCount;
    public int model262LineOfSightCount;
    public int model262PointerRangeCount;
    public int model262OnScreenCount;
    public bool samplePresent;
    public float sampleSourceX;
    public float sampleSourceY;
    public float sampleCurrentX;
    public float sampleCurrentY;
    public int sampleHitPoints;
    public bool sampleSourceOwnerLive;
    public bool sampleDying;
    public bool sampleRootPresent;
    public bool sampleRendererPresent;
    public bool sampleSpritePresent;
    public bool sampleColliderPresent;
    public bool sampleColliderEnabled;
    public bool sampleSelectableOwnerLive;
    public bool sampleCombatWorldPresent;
    public bool sampleWorldStatePresent;
    public float sampleDistance;
    public bool sampleLineOfSight;
    public float sampleScreenX;
    public float sampleScreenY;
    public float sampleScreenZ;
    public bool sampleScreenVisible;
    public string sampleRejection = string.Empty;
}

internal sealed class ReferenceNpcTaskLifecycleJourneyDriver
{
    private enum Phase
    {
        OpenOffer,
        WaitOffer,
        WaitBaselineAvailable,
        SelectOffer,
        AcceptOffer,
        WaitAccepted,
        TravelToCombatGate,
        WaitCombatMap,
        TravelToCombatArea,
        WaitCombatArea,
        SelectCombatTarget,
        ApproachCombatTarget,
        WaitAttack,
        WaitFatal,
        WaitGoalDrop,
        PickGoalDrop,
        WaitGoalPickup,
        TravelToCombatReturnAnchor,
        WaitCombatReturnAnchor,
        TravelToReturnGate,
        WaitReturnMap,
        TravelToNpcReturnAnchor,
        WaitNpcReturnAnchor,
        TravelToNpc,
        WaitNpcArrival,
        OpenTurnIn,
        WaitTurnInOffer,
        SelectTurnInOffer,
        ClaimReward,
        WaitReward,
        Complete,
        Failed,
    }

    private readonly ClientSessionCoordinator session;
    private ReferenceNpcTaskLifecycleJourneyArtifact artifact =
        new ReferenceNpcTaskLifecycleJourneyArtifact();
    private ReferenceNpcTaskTravelLegLedger travelLegs;
    private readonly HashSet<int> usedCombatEntities = new HashSet<int>();
    private readonly HashSet<int> groundBaseline = new HashSet<int>();
    private readonly HashSet<int> initialGroundIds = new HashSet<int>();
    private readonly HashSet<int> authorizedGroundIds = new HashSet<int>();
    private readonly float deadline;
    private Phase phase = Phase.OpenOffer;
    private float phaseDeadline;
    private float nextTargetScan;
    private ReferenceFirstWorldWindowOwner scheduledPathWindow;
    private ReferenceFirstWorldWindowOwner lastObservedWindow;
    private float scheduledPathX;
    private float scheduledPathY;
    private bool scheduledPathRequiresPrimary;
    private long scheduledPathLocalHitBaseline;
    private long lastRecoveredLocalHitRevision;
    private int targetApproachReplans;
    private int taskId;
    private int targetEntityId;
    private int goalGroundId;
    private int currentPickupGroundId;
    private int currentPickupItemId;
    private int currentPickupItemCount;
    private int currentPickupItemInventoryBaseline;
    private bool currentPickupIsGoal;
    private bool preludeMode;
    private bool readyTurnInPreludeMode;
    private bool freshAfterPrelude;
    private long taskAcceptIssuedBaseline;
    private long taskAcceptSucceededBaseline;
    private long taskAbandonIssuedBaseline;
    private long taskAbandonSucceededBaseline;
    private long abandonTaskLogVersionBaseline;
    private long abandonOffersVersionBaseline;
    private long openOffersVersionBaseline;
    private long openOffersResponseVersionSettled;
    private long preAcceptOffersVersionSettled;
    private bool openOfferReplacementSettled;
    private long acceptTaskLogVersionBaseline;
    private long acceptOffersVersionBaseline;
    private long turnInOpenOffersVersionBaseline;
    private long turnInOpenOffersResponseVersionSettled;
    private bool turnInOpenOfferReplacementSettled;
    private long taskCompleteIssuedBaseline;
    private long taskCompleteSucceededBaseline;
    private long attackIssuedBaseline;
    private long attackSucceededBaseline;
    private long pickupIssuedBaseline;
    private long pickupSucceededBaseline;
    private long pickupInventoryVersionBaseline;
    private long pickupTaskLogVersionBaseline;
    private long combatResultRevision;
    private int inventoryCountBeforeKill;
    private int initialLevel;
    private int initialExperience;
    private int preRewardLevel;
    private int preRewardExperience;
    private long rewardMoneyBaseline;
    private long rewardHonorBaseline;
    private long npcDialogTextEdgeWindowVersion;
    private long npcDialogTextEdgeCursor;
    private bool rewardDialogTextCaptureActive;
    private bool rewardFeedbackObserved;
    private long rewardTaskLogVersionBaseline;
    private long rewardInventoryVersionBaseline;
    private long rewardOffersVersionBaseline;
    private bool failurePending;
    private bool terminalArtifactWritten;
    private ReferenceNpcTaskLifecycleJourneyPartialArtifact terminalPartial;
    private long movementFrameSequence;
    private ReferenceNpcTaskLifecycleTargetDiagnostic targetDiagnostic =
        new ReferenceNpcTaskLifecycleTargetDiagnostic();
    private readonly List<ReferenceNpcTaskLifecycleMovementFrame> movementFrames =
        new List<ReferenceNpcTaskLifecycleMovementFrame>();

    public ReferenceNpcTaskLifecycleJourneyDriver(ClientSessionCoordinator session)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        deadline = Time.realtimeSinceStartup +
                   ReferenceNpcTaskLifecycleJourneyCheck.TimeoutSeconds;
        artifact.initialMapId = ReferenceNpcTaskLifecycleJourneyCheck.InitialMapId;
        artifact.combatMapId = ReferenceNpcTaskLifecycleJourneyCheck.CombatMapId;
        artifact.npcName = ReferenceNpcTaskLifecycleJourneyCheck.NpcName;
        artifact.taskTitle = ReferenceNpcTaskLifecycleJourneyCheck.TaskTitle;
        artifact.goalItemName = ReferenceNpcTaskLifecycleJourneyCheck.GoalItemName;
        artifact.goalCount = ReferenceNpcTaskLifecycleJourneyCheck.GoalCount;
        artifact.combatModelId =
            ReferenceNpcTaskLifecycleJourneyCheck.CombatMonsterModelId;
        travelLegs = new ReferenceNpcTaskTravelLegLedger(artifact.travelLegs);
        WorldPlayerStatusState status = session.PlayerStatusState;
        initialLevel = status == null ? -1 : status.Level;
        initialExperience = status == null ? -1 : status.Experience;
        artifact.inventoryBefore = CountGoalItems();
        WorldNpcDialogTextEdgeWindow dialogTextEdges =
            session.NpcDialogTextEdgeWindow ?? WorldNpcDialogTextEdgeWindow.Empty;
        npcDialogTextEdgeWindowVersion = dialogTextEdges.Version;
        npcDialogTextEdgeCursor = dialogTextEdges.LastRevision;
        foreach (int id in session.GroundItems.Keys)
        {
            initialGroundIds.Add(id);
        }
        SetPhaseDeadline();
    }

    public bool Terminal => phase == Phase.Complete || phase == Phase.Failed;

    public ReferenceNpcTaskLifecycleJourneyExit Exit { get; private set; }

    public int CheckpointCount => artifact.checkpoints.Count +
        artifact.prelude.checkpoints.Count +
        artifact.readyTurnInPrelude.checkpoints.Count;

    public string PhaseName => terminalPartial == null
        ? phase.ToString()
        : terminalPartial.phase;

    internal void FailFromWorldRevision(
        ReferenceNpcTaskLifecycleJourneyExit exit)
    {
        Fail(exit);
    }

    internal bool TryWriteTerminalArtifact(bool cancellationSettled)
    {
        if (!Terminal || terminalArtifactWritten)
        {
            return false;
        }

        artifact.terminalCancellationRequested = true;
        artifact.terminalCancellationSettled = cancellationSettled;
        if (!cancellationSettled)
        {
            string interruptedPhase = PhaseName;
            Exit = ReferenceNpcTaskLifecycleJourneyExit.TerminalCancellationFailed;
            terminalPartial = CreateTerminalPartial(Exit, interruptedPhase);
            PopulateFailureMovement(terminalPartial);
            phase = Phase.Failed;
        }
        if (Exit == ReferenceNpcTaskLifecycleJourneyExit.Success &&
            !ReferenceNpcTaskLifecycleJourneyCheck.IsAcceptedTrace(artifact))
        {
            Exit = ReferenceNpcTaskLifecycleJourneyExit.RewardMismatch;
            terminalPartial = CreateTerminalPartial(Exit, "Complete");
            PopulateFailureMovement(terminalPartial);
            phase = Phase.Failed;
        }

        if (Exit == ReferenceNpcTaskLifecycleJourneyExit.Success)
        {
            if (!ReferenceMonsterRealCombatTailCheck.WriteJsonAtomically(
                    ReferenceNpcTaskLifecycleJourneyCheck.ArtifactFileName,
                    JsonUtility.ToJson(artifact, true) + "\n"))
            {
                return false;
            }
        }
        else
        {
            if (terminalPartial == null)
            {
                return false;
            }
            terminalPartial.terminalCancellationRequested = true;
            terminalPartial.terminalCancellationSettled = cancellationSettled;
            if (!ReferenceMonsterRealCombatTailCheck.WriteJsonAtomically(
                    ReferenceNpcTaskLifecycleJourneyCheck.PartialArtifactFileName,
                    JsonUtility.ToJson(terminalPartial, true) + "\n"))
            {
                return false;
            }
        }

        terminalArtifactWritten = true;
        return true;
    }

    public void Tick(
        ReferenceFirstWorldWindowOwner window,
        GameBootstrap bootstrap)
    {
        if (Terminal || failurePending)
        {
            return;
        }

        try
        {
            if (Time.realtimeSinceStartup >= deadline)
            {
                Fail(ReferenceNpcTaskLifecycleJourneyExit.Timeout);
                return;
            }
            if (window == null || !window.Built || bootstrap == null)
            {
                return;
            }

            lastObservedWindow = window;
            CaptureMovementFrame(window);
            if (!ConsumeNpcDialogTextEdges())
            {
                Fail(
                    ReferenceNpcTaskLifecycleJourneyExit.DialogTextEdgeWindowRejected);
                return;
            }
            TickCore(window, bootstrap);
        }
        catch (Exception)
        {
            Fail(ReferenceNpcTaskLifecycleJourneyExit.Unexpected);
        }
    }

    private void TickCore(
        ReferenceFirstWorldWindowOwner window,
        GameBootstrap bootstrap)
    {
        switch (phase)
        {
            case Phase.OpenOffer:
                if (!InitialAuthorityReady(window))
                {
                    Fail(ReferenceNpcTaskLifecycleJourneyExit.NpcPointerRejected);
                    return;
                }
                CaptureOpenOfferVersionBaseline();
                CaptureTurnInOpenOfferVersionBaseline();
                if (!ClickNpc(window))
                {
                    Fail(ReferenceNpcTaskLifecycleJourneyExit.NpcPointerRejected);
                    return;
                }
                Advance(Phase.WaitOffer);
                break;
            case Phase.WaitOffer:
                bool invalidOpenOfferReplacement;
                if (!TrySettleOpenOfferReplacement(
                        out invalidOpenOfferReplacement))
                {
                    if (invalidOpenOfferReplacement)
                    {
                        Fail(ReferenceNpcTaskLifecycleJourneyExit.OfferUnavailable);
                    }
                    else
                    {
                        FailExpired(ReferenceNpcTaskLifecycleJourneyExit.OfferUnavailable);
                    }
                    return;
                }
                int availableTaskId;
                int preludeTaskId;
                int readyTurnInTaskId;
                if (!freshAfterPrelude &&
                    TryFindReadyTurnInPreludeOffer(out readyTurnInTaskId))
                {
                    taskId = readyTurnInTaskId;
                    bool invalidReadyTurnInOpenReplacement;
                    if (!TrySettleTurnInOpenOfferReplacement(
                            out invalidReadyTurnInOpenReplacement))
                    {
                        Fail(ReferenceNpcTaskLifecycleJourneyExit.TurnInUnavailable);
                        return;
                    }
                    readyTurnInPreludeMode = true;
                    artifact.readyTurnInPrelude.required = true;
                    artifact.readyTurnInPrelude.initialCollected =
                        ReferenceNpcTaskLifecycleJourneyCheck.GoalCount;
                    artifact.readyTurnInPrelude.activeReady = true;
                    artifact.readyTurnInPrelude.state2Offer = true;
                    artifact.turnInOffer = true;
                    Record("resume", window);
                    Advance(Phase.SelectTurnInOffer);
                }
                else if (!freshAfterPrelude &&
                    TryFindPreludeActiveOffer(out preludeTaskId))
                {
                    taskId = preludeTaskId;
                    preludeMode = true;
                    artifact.prelude.required = true;
                    artifact.prelude.initialCollected = 1;
                    Record("resume", window);
                    Advance(Phase.TravelToCombatGate);
                }
                else if (TryFindOffer(0, out availableTaskId) &&
                    session.TaskLogState.FindActive(availableTaskId) == null &&
                    (!ContainsDone(availableTaskId) || freshAfterPrelude ||
                     DoneContainsExactly(availableTaskId)) &&
                    CountGoalItems() == 0)
                {
                    bool repeatableAvailableDone = !freshAfterPrelude &&
                        DoneContainsExactly(availableTaskId);
                    taskId = availableTaskId;
                    artifact.baselineAlreadyAvailable = true;
                    artifact.baselineRepeatableAvailableDone =
                        repeatableAvailableDone;
                    artifact.baselineAfterPreludeAvailable = freshAfterPrelude &&
                        artifact.prelude.completed;
                    artifact.baselineAfterReadyTurnInAvailable =
                        freshAfterPrelude &&
                        artifact.readyTurnInPrelude.completed;
                    artifact.baselineAvailableAuthoritative = true;
                    artifact.offerAvailable = true;
                    WorldInventoryState freshInventory = session.InventoryState;
                    if (freshInventory == null)
                    {
                        Fail(ReferenceNpcTaskLifecycleJourneyExit.InitialAuthorityUnavailable);
                        return;
                    }
                    artifact.freshJourneyMoneyBaselineCaptured = true;
                    artifact.freshJourneyMoneyBaseline = freshInventory.Money;
                    SetPreAcceptOfferVersionSettled(
                        openOffersResponseVersionSettled);
                    Record("offer.available", window);
                    Advance(Phase.SelectOffer);
                }
                else if (TryFindRecoverableActiveOffer(out taskId))
                {
                    artifact.baselineRecoverableActive = true;
                    taskAbandonIssuedBaseline =
                        window.NpcTaskAbandonRequestIssuedCountForLifecycle;
                    taskAbandonSucceededBaseline =
                        window.NpcTaskAbandonRequestSucceededCountForLifecycle;
                    abandonTaskLogVersionBaseline = session.TaskLogStateVersion;
                    abandonOffersVersionBaseline = session.NpcTaskOffersStateVersion;
                    if (!window.TryAbandonNpcTaskLifecycleBaseline(taskId))
                    {
                        Fail(ReferenceNpcTaskLifecycleJourneyExit.BaselineRecoveryFailed);
                        return;
                    }
                    Advance(Phase.WaitBaselineAvailable);
                }
                else
                {
                    FailExpired(ReferenceNpcTaskLifecycleJourneyExit.OfferUnavailable);
                }
                break;
            case Phase.WaitBaselineAvailable:
                TraceBaselineAvailable(window);
                break;
            case Phase.SelectOffer:
                if (!ClickTaskRow(window, taskId, 0) ||
                    window.NpcDialog == null || !window.NpcDialog.InSubView ||
                    window.NpcDialog.Options.Count != 2 ||
                    window.NpcDialog.Options[0].Label !=
                        ReferenceFirstWorldNpcDialog.AcceptLabel ||
                    window.NpcDialog.LastOfferIntent == null ||
                    window.NpcDialog.LastOfferIntent.Award !=
                        ReferenceNpcTaskLifecycleJourneyCheck.AwardText)
                {
                    Fail(ReferenceNpcTaskLifecycleJourneyExit.OfferUnavailable);
                    return;
                }
                artifact.acceptDetailAction = true;
                Advance(Phase.AcceptOffer);
                break;
            case Phase.AcceptOffer:
                artifact.preAcceptOfferVersionObserved =
                    session.NpcTaskOffersStateVersion;
                artifact.preAcceptOfferVersionUnchanged =
                    openOfferReplacementSettled &&
                    preAcceptOffersVersionSettled > 0 &&
                    artifact.preAcceptOfferVersionObserved ==
                        preAcceptOffersVersionSettled;
                int preAcceptTaskId;
                if (!artifact.preAcceptOfferVersionUnchanged ||
                    !TryFindOffer(0, out preAcceptTaskId) ||
                    preAcceptTaskId != taskId)
                {
                    Fail(ReferenceNpcTaskLifecycleJourneyExit.AcceptRequestFailed);
                    return;
                }
                taskAcceptIssuedBaseline =
                    window.NpcTaskAcceptRequestIssuedCountForLifecycle;
                taskAcceptSucceededBaseline =
                    window.NpcTaskAcceptRequestSucceededCountForLifecycle;
                acceptTaskLogVersionBaseline = session.TaskLogStateVersion;
                acceptOffersVersionBaseline = session.NpcTaskOffersStateVersion;
                artifact.acceptTaskLogVersionBaseline =
                    acceptTaskLogVersionBaseline;
                artifact.acceptOfferVersionBaseline =
                    acceptOffersVersionBaseline;
                if (!ClickDialogOption(window, 0))
                {
                    Fail(ReferenceNpcTaskLifecycleJourneyExit.AcceptRequestFailed);
                    return;
                }
                Advance(Phase.WaitAccepted);
                break;
            case Phase.WaitAccepted:
                TraceAccepted(window);
                break;
            case Phase.TravelToCombatGate:
                BeginPath(
                    window,
                    ReferenceNpcTaskLifecycleJourneyCheck.CityToCombatGateX,
                    ReferenceNpcTaskLifecycleJourneyCheck.CityToCombatGateY,
                    Phase.WaitCombatMap,
                    ReferenceNpcTaskTravelLegKind.WaitCombatMap);
                break;
            case Phase.WaitCombatMap:
                if (window.MapDescriptor.id ==
                    ReferenceNpcTaskLifecycleJourneyCheck.CombatMapId)
                {
                    artifact.mapTransitions++;
                    if (!CompleteTravelLeg(
                            ReferenceNpcTaskTravelLegKind.WaitCombatMap))
                    {
                        Fail(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
                        return;
                    }
                    scheduledPathWindow = null;
                    Record("travel.combat", window);
                    Advance(Phase.TravelToCombatArea);
                }
                else
                {
                    CheckTravelStillActive(window, bootstrap);
                }
                break;
            case Phase.TravelToCombatArea:
                BeginPath(
                    window,
                    ReferenceNpcTaskLifecycleJourneyCheck.CombatAreaX,
                    ReferenceNpcTaskLifecycleJourneyCheck.CombatAreaY,
                    Phase.WaitCombatArea,
                    ReferenceNpcTaskTravelLegKind.WaitCombatArea);
                break;
            case Phase.WaitCombatArea:
                if (PlayerDistance(
                        window,
                        ReferenceNpcTaskLifecycleJourneyCheck.CombatAreaX,
                        ReferenceNpcTaskLifecycleJourneyCheck.CombatAreaY) <=
                    ReferenceNpcTaskLifecycleJourneyCheck.TravelArrivalDistance)
                {
                    if (!CompleteTravelLeg(
                            ReferenceNpcTaskTravelLegKind.WaitCombatArea))
                    {
                        Fail(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
                        return;
                    }
                    scheduledPathWindow = null;
                    Advance(Phase.SelectCombatTarget);
                }
                else
                {
                    CheckPathStillActive(window);
                }
                break;
            case Phase.SelectCombatTarget:
                SelectCombatTarget(window);
                break;
            case Phase.ApproachCombatTarget:
                ApproachCombatTarget(window);
                break;
            case Phase.WaitAttack:
                TraceAttack(window);
                break;
            case Phase.WaitFatal:
                TraceFatal(window);
                break;
            case Phase.WaitGoalDrop:
                TraceGoalDrop(window);
                break;
            case Phase.PickGoalDrop:
                PickGoalDrop(window);
                break;
            case Phase.WaitGoalPickup:
                TraceGoalPickup(window);
                break;
            case Phase.TravelToCombatReturnAnchor:
                // The final authoritative monster position can put the portal click just beyond
                // AStarCells' 30,000 primary cap. Reuse the already audited combat-area click as
                // one independent fixture leg; production SetPathTo still owns both routes.
                BeginPath(
                    window,
                    ReferenceNpcTaskLifecycleJourneyCheck.CombatAreaX,
                    ReferenceNpcTaskLifecycleJourneyCheck.CombatAreaY,
                    Phase.WaitCombatReturnAnchor,
                    ReferenceNpcTaskTravelLegKind.WaitCombatReturnAnchor,
                    true);
                break;
            case Phase.WaitCombatReturnAnchor:
                if (PlayerPathFinishedWithinArrival(
                        window,
                        ReferenceNpcTaskLifecycleJourneyCheck.CombatAreaX,
                        ReferenceNpcTaskLifecycleJourneyCheck.CombatAreaY))
                {
                    if (!CompleteTravelLeg(
                            ReferenceNpcTaskTravelLegKind.WaitCombatReturnAnchor))
                    {
                        Fail(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
                        return;
                    }
                    scheduledPathWindow = null;
                    Advance(Phase.TravelToReturnGate);
                }
                else
                {
                    CheckPathStillActive(window);
                }
                break;
            case Phase.TravelToReturnGate:
                BeginPath(
                    window,
                    ReferenceNpcTaskLifecycleJourneyCheck.CombatToCityGateX,
                    ReferenceNpcTaskLifecycleJourneyCheck.CombatToCityGateY,
                    Phase.WaitReturnMap,
                    ReferenceNpcTaskTravelLegKind.WaitReturnMap,
                    true);
                break;
            case Phase.WaitReturnMap:
                if (window.MapDescriptor.id ==
                    ReferenceNpcTaskLifecycleJourneyCheck.InitialMapId)
                {
                    artifact.mapTransitions++;
                    if (!CompleteTravelLeg(
                            ReferenceNpcTaskTravelLegKind.WaitReturnMap))
                    {
                        Fail(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
                        return;
                    }
                    scheduledPathWindow = null;
                    Record("travel.return", window);
                    Advance(Phase.TravelToNpcReturnAnchor);
                }
                else
                {
                    CheckTravelStillActive(window, bootstrap);
                }
                break;
            case Phase.TravelToNpcReturnAnchor:
                BeginPath(
                    window,
                    ReferenceNpcTaskLifecycleJourneyCheck.CityReturnAnchorX,
                    ReferenceNpcTaskLifecycleJourneyCheck.CityReturnAnchorY,
                    Phase.WaitNpcReturnAnchor,
                    ReferenceNpcTaskTravelLegKind.WaitNpcReturnAnchor,
                    true);
                break;
            case Phase.WaitNpcReturnAnchor:
                if (PlayerPathFinishedWithinArrival(
                        window,
                        ReferenceNpcTaskLifecycleJourneyCheck.CityReturnAnchorX,
                        ReferenceNpcTaskLifecycleJourneyCheck.CityReturnAnchorY))
                {
                    if (!CompleteTravelLeg(
                            ReferenceNpcTaskTravelLegKind.WaitNpcReturnAnchor))
                    {
                        Fail(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
                        return;
                    }
                    scheduledPathWindow = null;
                    Advance(Phase.TravelToNpc);
                }
                else
                {
                    CheckPathStillActive(window);
                }
                break;
            case Phase.TravelToNpc:
                BeginPath(
                    window,
                    ReferenceNpcTaskLifecycleJourneyCheck.ReturnNpcX,
                    ReferenceNpcTaskLifecycleJourneyCheck.ReturnNpcY,
                    Phase.WaitNpcArrival,
                    ReferenceNpcTaskTravelLegKind.WaitNpcArrival,
                    true);
                break;
            case Phase.WaitNpcArrival:
                if (PlayerPathFinishedWithinArrival(
                        window,
                        ReferenceNpcTaskLifecycleJourneyCheck.ReturnNpcX,
                        ReferenceNpcTaskLifecycleJourneyCheck.ReturnNpcY))
                {
                    if (!CompleteTravelLeg(
                            ReferenceNpcTaskTravelLegKind.WaitNpcArrival))
                    {
                        Fail(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
                        return;
                    }
                    scheduledPathWindow = null;
                    Advance(Phase.OpenTurnIn);
                }
                else
                {
                    CheckPathStillActive(window);
                }
                break;
            case Phase.OpenTurnIn:
                CaptureTurnInOpenOfferVersionBaseline();
                if (!ClickNpc(window))
                {
                    Fail(ReferenceNpcTaskLifecycleJourneyExit.NpcPointerRejected);
                    return;
                }
                artifact.sameNpcReturn = true;
                Advance(Phase.WaitTurnInOffer);
                break;
            case Phase.WaitTurnInOffer:
                bool invalidTurnInOpenReplacement;
                if (!TrySettleTurnInOpenOfferReplacement(
                        out invalidTurnInOpenReplacement))
                {
                    if (invalidTurnInOpenReplacement)
                    {
                        Fail(ReferenceNpcTaskLifecycleJourneyExit.TurnInUnavailable);
                    }
                    else
                    {
                        FailExpired(
                            ReferenceNpcTaskLifecycleJourneyExit.TurnInUnavailable);
                    }
                    return;
                }
                artifact.turnInOffer = true;
                Record("turnin.available", window);
                Advance(Phase.SelectTurnInOffer);
                break;
            case Phase.SelectTurnInOffer:
                if (!TurnInOpenOfferVersionStillSettled() ||
                    !ClickTaskRow(window, taskId, 2) ||
                    window.NpcDialog == null || !window.NpcDialog.InSubView ||
                    window.NpcDialog.Options.Count != 2 ||
                    window.NpcDialog.Options[0].Label !=
                        ReferenceFirstWorldNpcDialog.ClaimRewardLabel)
                {
                    Fail(ReferenceNpcTaskLifecycleJourneyExit.TurnInUnavailable);
                    return;
                }
                artifact.claimRewardAction = true;
                if (readyTurnInPreludeMode)
                {
                    artifact.readyTurnInPrelude.claimRewardAction = true;
                    Record("turnin.available", window);
                }
                Advance(Phase.ClaimReward);
                break;
            case Phase.ClaimReward:
                if (!CapturePreCompleteTurnInOpenOfferVersion())
                {
                    Fail(ReferenceNpcTaskLifecycleJourneyExit.TurnInUnavailable);
                    return;
                }
                if (!CaptureRewardBaseline(window))
                {
                    Fail(
                        ReferenceNpcTaskLifecycleJourneyExit.DialogTextEdgeWindowRejected);
                    return;
                }
                if (!ClickDialogOption(window, 0))
                {
                    Fail(ReferenceNpcTaskLifecycleJourneyExit.CompleteRequestFailed);
                    return;
                }
                Advance(Phase.WaitReward);
                break;
            case Phase.WaitReward:
                TraceReward(window);
                break;
        }
    }

    private bool InitialAuthorityReady(ReferenceFirstWorldWindowOwner window)
    {
        WorldPlayerStatusState status = session.PlayerStatusState;
        WorldInventoryState inventory = session.InventoryState;
        return window.MapDescriptor.id ==
                   ReferenceNpcTaskLifecycleJourneyCheck.InitialMapId &&
               status != null && status.Level ==
                   ReferenceNpcTaskLifecycleJourneyCheck.InitialLevel &&
                inventory != null &&
               (CountGoalItems() == 0 || CountGoalItems() == 1 ||
                CountGoalItems() == ReferenceNpcTaskLifecycleJourneyCheck.GoalCount) &&
               window.Frame != null && window.Frame.NpcActors.Count > 0;
    }

    private void CaptureOpenOfferVersionBaseline()
    {
        openOffersVersionBaseline = session.NpcTaskOffersStateVersion;
        openOffersResponseVersionSettled = 0;
        preAcceptOffersVersionSettled = 0;
        openOfferReplacementSettled = false;
        artifact.openOfferVersionBaseline = openOffersVersionBaseline;
        artifact.openOfferResponseVersionSettled = 0;
        artifact.openOfferReplacementDelta = 0;
        artifact.preAcceptOfferVersionSettled = 0;
        artifact.preAcceptOfferVersionObserved = 0;
        artifact.preAcceptOfferVersionUnchanged = false;
    }

    private bool TrySettleOpenOfferReplacement(out bool invalidReplacement)
    {
        invalidReplacement = false;
        long observedVersion = session.NpcTaskOffersStateVersion;
        long versionDelta = observedVersion - openOffersVersionBaseline;
        artifact.openOfferReplacementDelta = versionDelta;
        if (versionDelta == 0)
        {
            return false;
        }

        bool admittedSemanticBaseline = HasAdmittedOpenOfferSemantic();
        if (!ReferenceNpcTaskLifecycleJourneyCheck.IsSettledOpenOfferReplacement(
                versionDelta,
                admittedSemanticBaseline))
        {
            invalidReplacement = true;
            return false;
        }

        openOffersResponseVersionSettled = observedVersion;
        openOfferReplacementSettled = true;
        artifact.openOfferResponseVersionSettled = observedVersion;
        return true;
    }

    private bool HasAdmittedOpenOfferSemantic()
    {
        int matchedTaskId;
        if (!freshAfterPrelude &&
            (TryFindReadyTurnInPreludeOffer(out matchedTaskId) ||
             TryFindPreludeActiveOffer(out matchedTaskId)))
        {
            return true;
        }

        if (TryFindOffer(0, out matchedTaskId) &&
            session.TaskLogState.FindActive(matchedTaskId) == null &&
            (!ContainsDone(matchedTaskId) || freshAfterPrelude ||
             DoneContainsExactly(matchedTaskId)) &&
            CountGoalItems() == 0)
        {
            return true;
        }

        return TryFindRecoverableActiveOffer(out matchedTaskId);
    }

    private void SetPreAcceptOfferVersionSettled(long version)
    {
        preAcceptOffersVersionSettled = version;
        artifact.preAcceptOfferVersionSettled = version;
    }

    private void CaptureTurnInOpenOfferVersionBaseline()
    {
        turnInOpenOffersVersionBaseline = session.NpcTaskOffersStateVersion;
        turnInOpenOffersResponseVersionSettled = 0;
        turnInOpenOfferReplacementSettled = false;
        artifact.turnInOpen =
            new ReferenceNpcTaskLifecycleTurnInOpenArtifact
            {
                turnInOpenOffersVersionBaseline =
                    turnInOpenOffersVersionBaseline,
            };
    }

    private bool TrySettleTurnInOpenOfferReplacement(
        out bool invalidReplacement)
    {
        invalidReplacement = false;
        long observedVersion = session.NpcTaskOffersStateVersion;
        long versionDelta = observedVersion - turnInOpenOffersVersionBaseline;
        artifact.turnInOpen.turnInOpenOfferReplacementDelta = versionDelta;
        if (versionDelta == 0)
        {
            return false;
        }

        bool exactState2Ready = HasExactTurnInReadyOffer();
        if (!ReferenceNpcTaskLifecycleJourneyCheck.IsSettledOpenOfferReplacement(
                versionDelta,
                exactState2Ready))
        {
            invalidReplacement = true;
            return false;
        }

        turnInOpenOffersResponseVersionSettled = observedVersion;
        turnInOpenOfferReplacementSettled = true;
        artifact.turnInOpen.turnInOpenOffersResponseVersionSettled =
            observedVersion;
        artifact.turnInOpen.turnInOpenFinalState2Ready = true;
        return true;
    }

    private bool HasExactTurnInReadyOffer()
    {
        int matchedTaskId;
        return taskId > 0 &&
               TryFindReadyTurnInPreludeOffer(out matchedTaskId) &&
               matchedTaskId == taskId;
    }

    private bool TurnInOpenOfferVersionStillSettled()
    {
        return turnInOpenOfferReplacementSettled &&
               turnInOpenOffersResponseVersionSettled > 0 &&
               session.NpcTaskOffersStateVersion ==
                   turnInOpenOffersResponseVersionSettled &&
               HasExactTurnInReadyOffer();
    }

    private bool CapturePreCompleteTurnInOpenOfferVersion()
    {
        long observedVersion = session.NpcTaskOffersStateVersion;
        artifact.turnInOpen.preCompleteOffersVersionObserved = observedVersion;
        artifact.turnInOpen.preCompleteOfferVersionUnchanged =
            turnInOpenOfferReplacementSettled &&
            observedVersion == turnInOpenOffersResponseVersionSettled &&
            HasExactTurnInReadyOffer();
        return artifact.turnInOpen.preCompleteOfferVersionUnchanged;
    }

    private bool TryFindRecoverableActiveOffer(out int matchedTaskId)
    {
        if (!TryFindOffer(1, out matchedTaskId))
        {
            return false;
        }

        WorldActiveTaskState active = session.TaskLogState.FindActive(matchedTaskId);
        return active != null &&
               active.Progress ==
                   ReferenceNpcTaskLifecycleJourneyCheck.ExpectedProgress(0) &&
               !active.Ready &&
               active.TurnInNpc == ReferenceNpcTaskLifecycleJourneyCheck.NpcName &&
               !ContainsDone(matchedTaskId) && CountGoalItems() == 0;
    }

    private bool TryFindPreludeActiveOffer(out int matchedTaskId)
    {
        if (!TryFindOffer(1, out matchedTaskId))
        {
            return false;
        }

        WorldActiveTaskState active = session.TaskLogState.FindActive(matchedTaskId);
        return active != null &&
               active.Progress ==
                   ReferenceNpcTaskLifecycleJourneyCheck.ExpectedProgress(1) &&
               !active.Ready &&
               active.TurnInNpc == ReferenceNpcTaskLifecycleJourneyCheck.NpcName &&
               !ContainsDone(matchedTaskId) && CountGoalItems() == 1;
    }

    private bool TryFindReadyTurnInPreludeOffer(out int matchedTaskId)
    {
        if (!TryFindOffer(2, out matchedTaskId))
        {
            return false;
        }

        WorldActiveTaskState active = session.TaskLogState.FindActive(matchedTaskId);
        return active != null &&
               active.Progress ==
                   ReferenceNpcTaskLifecycleJourneyCheck.ExpectedProgress(
                       ReferenceNpcTaskLifecycleJourneyCheck.GoalCount) &&
               active.Ready && active.IsTerminal &&
               active.TurnInNpc == ReferenceNpcTaskLifecycleJourneyCheck.NpcName &&
               !ContainsDone(matchedTaskId) && CountGoalItems() ==
                   ReferenceNpcTaskLifecycleJourneyCheck.GoalCount;
    }

    private void TraceBaselineAvailable(ReferenceFirstWorldWindowOwner window)
    {
        long issued = window.NpcTaskAbandonRequestIssuedCountForLifecycle -
                      taskAbandonIssuedBaseline;
        long succeeded = window.NpcTaskAbandonRequestSucceededCountForLifecycle -
                         taskAbandonSucceededBaseline;
        int availableTaskId;
        bool available = TryFindOffer(0, out availableTaskId) &&
                         availableTaskId == taskId;
        if (!string.IsNullOrEmpty(window.LastNpcTaskFailureCode) || issued > 1 ||
            succeeded > 1)
        {
            Fail(ReferenceNpcTaskLifecycleJourneyExit.BaselineRecoveryFailed);
            return;
        }

        if (issued == 1 && succeeded == 1 &&
            session.TaskLogStateVersion == abandonTaskLogVersionBaseline + 1 &&
            session.NpcTaskOffersStateVersion == abandonOffersVersionBaseline + 1 &&
            session.TaskLogState.FindActive(taskId) == null && available &&
            CountGoalItems() == 0)
        {
            artifact.baselineAbandonRequestIssuedDelta = issued;
            artifact.baselineAbandonRequestSucceededDelta = succeeded;
            artifact.baselineTaskLogReplaced = true;
            artifact.baselineOfferReplaced = true;
            artifact.baselineAvailableAuthoritative = true;
            artifact.offerAvailable = true;
            SetPreAcceptOfferVersionSettled(
                session.NpcTaskOffersStateVersion);
            Record("offer.available", window);
            Advance(Phase.SelectOffer);
            return;
        }

        FailExpired(ReferenceNpcTaskLifecycleJourneyExit.BaselineRecoveryFailed);
    }

    private void TraceAccepted(ReferenceFirstWorldWindowOwner window)
    {
        long issued = window.NpcTaskAcceptRequestIssuedCountForLifecycle -
                      taskAcceptIssuedBaseline;
        long succeeded = window.NpcTaskAcceptRequestSucceededCountForLifecycle -
                         taskAcceptSucceededBaseline;
        long taskLogDelta = session.TaskLogStateVersion -
                            acceptTaskLogVersionBaseline;
        long offerDelta = session.NpcTaskOffersStateVersion -
                          acceptOffersVersionBaseline;
        WorldActiveTaskState active = session.TaskLogState.FindActive(taskId);
        bool activeExact = active != null &&
            active.Progress == ReferenceNpcTaskLifecycleJourneyCheck.ExpectedProgress(0) &&
            !active.Ready && active.TurnInNpc ==
                ReferenceNpcTaskLifecycleJourneyCheck.NpcName;
        byte finalOfferState;
        bool hasFinalOfferState = TryFindSemanticOfferState(
            taskId,
            out finalOfferState);
        bool finalExactState1 = hasFinalOfferState && finalOfferState == 1;
        bool doneAbsent = !ContainsDone(taskId);
        bool goalItemsAbsent = CountGoalItems() == 0;
        bool conflictingTransition = taskLogDelta < 0 || offerDelta < 0 ||
            taskLogDelta > 1 || offerDelta > 1 ||
            taskLogDelta == 1 && (!activeExact || !doneAbsent || !goalItemsAbsent) ||
            offerDelta == 1 && !finalExactState1;
        string pattern;
        bool accepted =
            ReferenceNpcTaskLifecycleJourneyCheck.
                IsAcceptedFreshAcceptReplacementOrdering(
                    issued,
                    succeeded,
                    taskLogDelta,
                    offerDelta,
                    activeExact,
                    finalExactState1,
                    doneAbsent,
                    goalItemsAbsent,
                    conflictingTransition,
                    out pattern);
        artifact.acceptTaskLogReplacementDelta = taskLogDelta;
        artifact.acceptOfferReplacementDelta = offerDelta;
        artifact.acceptRequestIssuedDelta = issued;
        artifact.acceptRequestSucceededDelta = succeeded;
        artifact.acceptFinalOfferState = hasFinalOfferState
            ? finalOfferState
            : -1;
        artifact.acceptFinalOfferExactState1 = finalExactState1;
        artifact.acceptConflictingTransition = conflictingTransition;
        artifact.acceptOfferReplacementPattern = pattern;
        if (!string.IsNullOrEmpty(window.LastNpcTaskFailureCode) || issued > 1 ||
            succeeded > 1)
        {
            Fail(ReferenceNpcTaskLifecycleJourneyExit.AcceptRequestFailed);
            return;
        }
        if (accepted)
        {
            artifact.acceptedAuthoritative = true;
            Record("task.accepted", window);
            Advance(Phase.TravelToCombatGate);
            return;
        }

        if (conflictingTransition)
        {
            Fail(ReferenceNpcTaskLifecycleJourneyExit.AcceptedStateMissing);
            return;
        }

        FailExpired(ReferenceNpcTaskLifecycleJourneyExit.AcceptedStateMissing);
    }

    private void SelectCombatTarget(ReferenceFirstWorldWindowOwner window)
    {
        if (Time.time < nextTargetScan)
        {
            return;
        }
        nextTargetScan = Time.time +
            ReferenceNpcTaskLifecycleJourneyCheck.TargetRescanSeconds;

        targetDiagnostic = CaptureCombatTargetDiagnostic(window);
        artifact.entityPublicationDiagnostic = targetDiagnostic;
        if (!ReferenceNpcTaskLifecycleJourneyCheck.IsAcceptedEntityPublication(
                targetDiagnostic))
        {
            FailExpired(ReferenceNpcTaskLifecycleJourneyExit.CombatTargetUnavailable);
            return;
        }
        ReferenceWorldMonsterActor selected = null;
        float selectedDistance = float.PositiveInfinity;
        foreach (KeyValuePair<int, ReferenceWorldMonsterActor> pair in
                 window.Frame.MonsterActors)
        {
            ReferenceWorldMonsterActor actor = pair.Value;
            if (usedCombatEntities.Contains(pair.Key) || !LiveCombatTarget(actor))
            {
                continue;
            }
            float dx = actor.WorldX - window.Frame.Player.WorldX;
            float dy = actor.WorldY - window.Frame.Player.WorldY;
            float distance = dx * dx + dy * dy;
            if (distance > 1f && distance < selectedDistance)
            {
                selectedDistance = distance;
                selected = actor;
                targetEntityId = pair.Key;
            }
        }
        if (selected == null)
        {
            FailExpired(ReferenceNpcTaskLifecycleJourneyExit.CombatTargetUnavailable);
            return;
        }

        targetApproachReplans = 0;
        float distancePixels = Mathf.Sqrt(selectedDistance);
        if (distancePixels > ReferenceNpcTaskLifecycleJourneyCheck.TargetPointerDistance)
        {
            ReferenceWorldApproachEvaluation approach;
            if (!window.TryEvaluateNpcTaskLifecycleApproach(
                    targetEntityId,
                    out approach) || !approach.Admissible ||
                !BeginTravelLeg(
                    ReferenceNpcTaskTravelLegKind.ApproachCombatTarget,
                    approach.FinalX,
                    approach.FinalY,
                    targetEntityId) ||
                !SchedulePath(window, approach.FinalX, approach.FinalY, false))
            {
                Fail(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
                return;
            }
            Advance(Phase.ApproachCombatTarget);
            return;
        }

        BeginCombatPointer(window, selected);
    }

    private void ApproachCombatTarget(ReferenceFirstWorldWindowOwner window)
    {
        ReferenceWorldMonsterActor actor;
        if (!window.Frame.MonsterActors.TryGetValue(targetEntityId, out actor) ||
            !LiveCombatTarget(actor))
        {
            if (!AbandonTravelLeg(
                    ReferenceNpcTaskTravelLegKind.ApproachCombatTarget))
            {
                Fail(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
                return;
            }
            usedCombatEntities.Add(targetEntityId);
            targetEntityId = 0;
            scheduledPathWindow = null;
            Advance(Phase.SelectCombatTarget);
            return;
        }

        float distance = Distance(window.Frame.Player.WorldX, window.Frame.Player.WorldY,
            actor.WorldX, actor.WorldY);
        if (distance <= ReferenceNpcTaskLifecycleJourneyCheck.TargetPointerDistance)
        {
            scheduledPathWindow = null;
            BeginCombatPointer(window, actor);
            return;
        }
        if (!window.Frame.Player.MovementActive)
        {
            if (TryResumeAfterAuthoritativeHurt(window))
            {
                FailExpired(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
                return;
            }
            if (window.Frame.Player.LastMovementStopReason ==
                ReferenceWorldPlayerMovementStopReason.PixelCollisionBlocked)
            {
                RecordTravelPixelCollisionStop();
                Fail(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
                return;
            }
            ReferenceWorldApproachEvaluation approach;
            if (!window.TryEvaluateNpcTaskLifecycleApproach(
                    targetEntityId,
                out approach) || !approach.Admissible ||
                targetApproachReplans >=
                    ReferenceNpcTaskLifecycleJourneyCheck.MaximumApproachReplansPerTarget ||
                !travelLegs.UpdateDestination(approach.FinalX, approach.FinalY) ||
                !SchedulePath(window, approach.FinalX, approach.FinalY, false))
            {
                Fail(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
                return;
            }
            targetApproachReplans++;
        }
        else
        {
            FailExpired(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
        }
    }

    private void BeginCombatPointer(
        ReferenceFirstWorldWindowOwner window,
        ReferenceWorldMonsterActor actor)
    {
        float pointerDistance = Distance(
            window.Frame.Player.WorldX,
            window.Frame.Player.WorldY,
            actor.WorldX,
            actor.WorldY);
        bool pointerLineOfSight = HasLineOfSight(window, actor);
        if (!(pointerDistance > 0f) ||
            pointerDistance > ReferenceNpcTaskLifecycleJourneyCheck.TargetPointerDistance ||
            !pointerLineOfSight)
        {
            if (!AbandonTravelLeg(
                    ReferenceNpcTaskTravelLegKind.ApproachCombatTarget))
            {
                Fail(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
                return;
            }
            usedCombatEntities.Add(targetEntityId);
            targetEntityId = 0;
            Advance(Phase.SelectCombatTarget);
            return;
        }
        if (artifact.combatKills == 0)
        {
            artifact.firstCombatDistance = pointerDistance;
            artifact.firstCombatLineOfSight = pointerLineOfSight;
        }
        Vector3 screen = window.Camera.WorldToScreenPoint(actor.RootObject.transform.position);
        if (screen.z <= 0f || screen.x < 0f || screen.x > Screen.width ||
            screen.y < 0f || screen.y > Screen.height)
        {
            ReferenceWorldApproachEvaluation approach;
            if (!window.TryEvaluateNpcTaskLifecycleApproach(
                    targetEntityId,
                    out approach) || !approach.Admissible ||
                !EnsureCombatTargetApproachLeg(approach.FinalX, approach.FinalY) ||
                !SchedulePath(window, approach.FinalX, approach.FinalY, false))
            {
                Fail(ReferenceNpcTaskLifecycleJourneyExit.CombatTargetUnavailable);
                return;
            }
            Advance(Phase.ApproachCombatTarget);
            return;
        }

        attackIssuedBaseline = window.BasicAttackRequestIssuedCountForRealCombatTail;
        attackSucceededBaseline =
            window.BasicAttackRequestSucceededCountForRealCombatTail;
        combatResultRevision = LatestCombatResultRevision();
        groundBaseline.Clear();
        foreach (int id in session.GroundItems.Keys)
        {
            groundBaseline.Add(id);
        }
        inventoryCountBeforeKill = CountGoalItems();
        if (!window.HandlePointerInput(
                screen,
                true,
                false,
                false,
                false,
                false,
                Time.unscaledTime,
                Time.time,
                Rect.zero) ||
            window.LastPointerAction !=
                ReferenceFirstWorldPointerAction.MonsterAttackIntentSelected)
        {
            Fail(ReferenceNpcTaskLifecycleJourneyExit.CombatTargetUnavailable);
            return;
        }
        if (!CompleteTravelLegIfActive(
                ReferenceNpcTaskTravelLegKind.ApproachCombatTarget))
        {
            Fail(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
            return;
        }
        Advance(Phase.WaitAttack);
    }

    private void TraceAttack(ReferenceFirstWorldWindowOwner window)
    {
        long issued = window.BasicAttackRequestIssuedCountForRealCombatTail -
                      attackIssuedBaseline;
        long succeeded = window.BasicAttackRequestSucceededCountForRealCombatTail -
                         attackSucceededBaseline;
        if (!string.IsNullOrEmpty(window.LastBasicAttackFailureCode))
        {
            Fail(ReferenceNpcTaskLifecycleJourneyExit.CombatRequestFailed);
            return;
        }
        if (issued > 0 && succeeded > 0)
        {
            artifact.attackRequestIssuedDelta += issued;
            artifact.attackRequestSucceededDelta += succeeded;
            Advance(Phase.WaitFatal);
            TraceFatal(window);
            return;
        }
        FailExpired(ReferenceNpcTaskLifecycleJourneyExit.CombatRequestFailed);
    }

    private void TraceFatal(ReferenceFirstWorldWindowOwner window)
    {
        IReadOnlyList<WorldCombatResultState> results = session.CombatResults;
        for (int index = 0; index < results.Count; index++)
        {
            WorldCombatResultState result = results[index];
            if (result == null || result.Revision <= combatResultRevision)
            {
                continue;
            }
            combatResultRevision = result.Revision;
            if (result.TargetEntityId == targetEntityId && result.Fatal &&
                !result.Missed && result.TargetHitPoints <= 0)
            {
                usedCombatEntities.Add(targetEntityId);
                artifact.combatKills++;
                Advance(Phase.WaitGoalDrop);
                TraceGoalDrop(window);
                return;
            }
        }
        FailExpired(ReferenceNpcTaskLifecycleJourneyExit.FatalResultMissing);
    }

    private void TraceGoalDrop(ReferenceFirstWorldWindowOwner window)
    {
        foreach (KeyValuePair<int, WorldGroundItemState> pair in session.GroundItems)
        {
            WorldGroundItemState state = pair.Value;
            if (!initialGroundIds.Contains(pair.Key) && state != null && state.Owned)
            {
                authorizedGroundIds.Add(pair.Key);
            }
            if (groundBaseline.Contains(pair.Key) || state == null ||
                state.ItemId != ReferenceNpcTaskLifecycleJourneyCheck.GoalItemId ||
                state.Count != 1 || !state.Owned)
            {
                continue;
            }
            ReferenceWorldGroundItemActor actor;
            if (!window.Frame.GroundItemActors.TryGetValue(pair.Key, out actor) ||
                actor == null || !actor.Built || actor.Root == null ||
                actor.PickBox == null || !actor.PickBox.enabled)
            {
                continue;
            }
            goalGroundId = pair.Key;
            Advance(Phase.PickGoalDrop);
            return;
        }
        FailExpired(ReferenceNpcTaskLifecycleJourneyExit.GoalDropMissing);
    }

    private void PickGoalDrop(ReferenceFirstWorldWindowOwner window)
    {
        ReferenceWorldGroundItemActor actor;
        if (!window.Frame.GroundItemActors.TryGetValue(goalGroundId, out actor) ||
            actor == null || actor.Root == null)
        {
            Fail(ReferenceNpcTaskLifecycleJourneyExit.GoalDropMissing);
            return;
        }
        Vector3 screen = window.Camera.WorldToScreenPoint(actor.Root.transform.position);
        pickupIssuedBaseline = window.GroundPickupRequestIssuedCountForGroundLoot;
        pickupSucceededBaseline = window.GroundPickupRequestSucceededCountForGroundLoot;
        pickupInventoryVersionBaseline = session.InventoryStateVersion;
        pickupTaskLogVersionBaseline = session.TaskLogStateVersion;
        if (screen.z <= 0f || !window.HandlePointerInput(
                screen,
                true,
                false,
                false,
                false,
                false,
                Time.unscaledTime,
                Time.time,
                Rect.zero) ||
            window.LastPointerAction !=
                ReferenceFirstWorldPointerAction.GroundItemPickupApproach ||
            window.GroundPickupRequestIssuedCountForGroundLoot != pickupIssuedBaseline)
        {
            Fail(ReferenceNpcTaskLifecycleJourneyExit.GoalPickupFailed);
            return;
        }
        currentPickupGroundId = window.LastPointerPickedTargetId;
        WorldGroundItemState selected;
        if (currentPickupGroundId <= 0 ||
            !authorizedGroundIds.Contains(currentPickupGroundId) ||
            !session.GroundItems.TryGetValue(currentPickupGroundId, out selected) ||
            selected == null || !selected.Owned)
        {
            Fail(ReferenceNpcTaskLifecycleJourneyExit.GoalPickupFailed);
            return;
        }
        currentPickupIsGoal =
            selected.ItemId == ReferenceNpcTaskLifecycleJourneyCheck.GoalItemId;
        currentPickupItemId = selected.ItemId;
        currentPickupItemCount = selected.Count;
        currentPickupItemInventoryBaseline = CountInventoryItem(selected.ItemId);
        Advance(Phase.WaitGoalPickup);
    }

    private void TraceGoalPickup(ReferenceFirstWorldWindowOwner window)
    {
        long issued = window.GroundPickupRequestIssuedCountForGroundLoot -
                      pickupIssuedBaseline;
        long succeeded = window.GroundPickupRequestSucceededCountForGroundLoot -
                         pickupSucceededBaseline;
        int expected = inventoryCountBeforeKill + 1;
        WorldActiveTaskState active = session.TaskLogState.FindActive(taskId);
        bool removed = !session.GroundItems.ContainsKey(currentPickupGroundId) &&
                       !window.Frame.GroundItemActors.ContainsKey(currentPickupGroundId);
        bool progress = active != null &&
            active.Progress == ReferenceNpcTaskLifecycleJourneyCheck.ExpectedProgress(expected) &&
            active.Ready == (expected == ReferenceNpcTaskLifecycleJourneyCheck.GoalCount);
        if (!string.IsNullOrEmpty(window.LastGroundPickupFailureCode) || issued > 1 ||
            succeeded > 1)
        {
            Fail(ReferenceNpcTaskLifecycleJourneyExit.GoalPickupFailed);
            return;
        }
        if (issued == 1 && succeeded == 1 && removed &&
            window.LastGroundPickupRequestedIdForGroundLoot == currentPickupGroundId &&
            session.InventoryStateVersion == pickupInventoryVersionBaseline + 1 &&
            session.TaskLogStateVersion == pickupTaskLogVersionBaseline + 1 &&
            CountInventoryItem(currentPickupItemId) ==
                currentPickupItemInventoryBaseline + currentPickupItemCount &&
            !currentPickupIsGoal && CountGoalItems() == inventoryCountBeforeKill &&
            active != null &&
            active.Progress == ReferenceNpcTaskLifecycleJourneyCheck.ExpectedProgress(
                inventoryCountBeforeKill) &&
            active.Ready == (inventoryCountBeforeKill ==
                ReferenceNpcTaskLifecycleJourneyCheck.GoalCount))
        {
            artifact.pickupRequestIssuedDelta += issued;
            artifact.pickupRequestSucceededDelta += succeeded;
            artifact.incidentalPickups++;
            if (artifact.incidentalPickups >
                ReferenceNpcTaskLifecycleJourneyCheck.MaximumIncidentalPickups)
            {
                Fail(ReferenceNpcTaskLifecycleJourneyExit.GoalPickupFailed);
                return;
            }
            currentPickupGroundId = 0;
            currentPickupItemId = 0;
            currentPickupItemCount = 0;
            currentPickupItemInventoryBaseline = 0;
            currentPickupIsGoal = false;
            Advance(Phase.PickGoalDrop);
            return;
        }
        if (issued == 1 && succeeded == 1 && removed && currentPickupIsGoal &&
            window.LastGroundPickupRequestedIdForGroundLoot == currentPickupGroundId &&
            session.InventoryStateVersion == pickupInventoryVersionBaseline + 1 &&
            session.TaskLogStateVersion == pickupTaskLogVersionBaseline + 1 &&
            CountInventoryItem(currentPickupItemId) ==
                currentPickupItemInventoryBaseline + currentPickupItemCount &&
            CountGoalItems() == expected &&
            progress)
        {
            artifact.pickupRequestIssuedDelta += issued;
            artifact.pickupRequestSucceededDelta += succeeded;
            artifact.goalPickups++;
            Record(
                "combat.progress." + expected.ToString(CultureInfo.InvariantCulture),
                window);
            targetEntityId = 0;
            goalGroundId = 0;
            currentPickupGroundId = 0;
            currentPickupItemId = 0;
            currentPickupItemCount = 0;
            currentPickupItemInventoryBaseline = 0;
            currentPickupIsGoal = false;
            if (expected == ReferenceNpcTaskLifecycleJourneyCheck.GoalCount)
            {
                artifact.inventoryReady = expected;
                artifact.progressAuthoritative = true;
                artifact.readyAuthoritative = true;
                WorldPlayerStatusState status = session.PlayerStatusState;
                artifact.combatExperienceAdvanced = StatusAdvanced(
                    initialLevel,
                    initialExperience,
                    status == null ? -1 : status.Level,
                    status == null ? -1 : status.Experience);
                Record("task.ready", window);
                Advance(Phase.TravelToCombatReturnAnchor);
            }
            else
            {
                Advance(Phase.SelectCombatTarget);
            }
            return;
        }
        FailExpired(ReferenceNpcTaskLifecycleJourneyExit.ProgressMismatch);
    }

    private bool ConsumeNpcDialogTextEdges()
    {
        WorldNpcDialogTextEdgeWindow next = session.NpcDialogTextEdgeWindow;
        if (next == null)
        {
            artifact.rewardDialogTextCursorFailure = "window_missing";
            return false;
        }
        if (next.Version < npcDialogTextEdgeWindowVersion)
        {
            artifact.rewardDialogTextCursorFailure = "version_regressed";
            return false;
        }
        if (npcDialogTextEdgeCursor < next.DroppedThroughRevision)
        {
            artifact.rewardDialogTextCursorFailure = "history_overflow";
            return false;
        }
        if (npcDialogTextEdgeCursor > next.LastRevision)
        {
            artifact.rewardDialogTextCursorFailure = "cursor_ahead";
            return false;
        }

        long expectedRevision = npcDialogTextEdgeCursor + 1;
        for (int index = 0; index < next.Edges.Count; index++)
        {
            WorldNpcDialogTextEdge edge = next.Edges[index];
            if (edge.Revision <= npcDialogTextEdgeCursor)
            {
                continue;
            }
            if (edge.Revision != expectedRevision)
            {
                artifact.rewardDialogTextCursorFailure =
                    "revision_not_contiguous";
                return false;
            }

            if (rewardDialogTextCaptureActive)
            {
                if (artifact.rewardDialogTextEdges.Count >= 256)
                {
                    artifact.rewardDialogTextCursorFailure =
                        "diagnostic_overflow";
                    return false;
                }
                artifact.rewardDialogTextEdges.Add(
                    new ReferenceNpcTaskDialogTextEdgeArtifact
                    {
                        revision = edge.Revision,
                        text = edge.Text,
                        displayMode = edge.DisplayMode,
                    });
                if (!rewardFeedbackObserved && edge.DisplayMode == 6 &&
                    edge.Text ==
                        ReferenceNpcTaskLifecycleJourneyCheck.RewardFeedbackText)
                {
                    rewardFeedbackObserved = true;
                    artifact.rewardFeedbackEdgeRevision = edge.Revision;
                }
            }

            npcDialogTextEdgeCursor = edge.Revision;
            expectedRevision++;
        }
        if (npcDialogTextEdgeCursor != next.LastRevision)
        {
            artifact.rewardDialogTextCursorFailure = "unseen_edge_missing";
            return false;
        }

        npcDialogTextEdgeWindowVersion = next.Version;
        if (rewardDialogTextCaptureActive)
        {
            artifact.rewardDialogTextWindowVersionObserved = next.Version;
            artifact.rewardDialogTextObservedThroughRevision =
                npcDialogTextEdgeCursor;
        }
        return true;
    }

    private bool CaptureRewardBaseline(ReferenceFirstWorldWindowOwner window)
    {
        if (!ConsumeNpcDialogTextEdges())
        {
            artifact.rewardDialogTextCursorFailure =
                "baseline_window_rejected";
            return false;
        }

        WorldInventoryState inventory = session.InventoryState;
        WorldPlayerStatusState status = session.PlayerStatusState;
        rewardMoneyBaseline = inventory == null ? -1 : inventory.Money;
        rewardHonorBaseline = inventory == null ? -1 : inventory.Honor;
        if (readyTurnInPreludeMode && rewardMoneyBaseline >= 0)
        {
            artifact.readyTurnInPrelude.moneyBaselineCaptured = true;
            artifact.readyTurnInPrelude.moneyBaseline = rewardMoneyBaseline;
        }
        artifact.rewardDialogTextWindowVersionBaseline =
            npcDialogTextEdgeWindowVersion;
        artifact.rewardDialogTextBaselineRevision = npcDialogTextEdgeCursor;
        artifact.rewardDialogTextWindowVersionObserved =
            npcDialogTextEdgeWindowVersion;
        artifact.rewardDialogTextObservedThroughRevision =
            npcDialogTextEdgeCursor;
        artifact.rewardDialogTextEdges.Clear();
        artifact.rewardFeedbackEdgeRevision = 0;
        artifact.rewardDialogTextCursorFailure = string.Empty;
        rewardFeedbackObserved = false;
        rewardDialogTextCaptureActive = true;
        rewardTaskLogVersionBaseline = session.TaskLogStateVersion;
        rewardInventoryVersionBaseline = session.InventoryStateVersion;
        rewardOffersVersionBaseline = session.NpcTaskOffersStateVersion;
        preRewardLevel = status == null ? -1 : status.Level;
        preRewardExperience = status == null ? -1 : status.Experience;
        taskCompleteIssuedBaseline =
            window.NpcTaskCompleteRequestIssuedCountForLifecycle;
        taskCompleteSucceededBaseline =
            window.NpcTaskCompleteRequestSucceededCountForLifecycle;
        artifact.turnInOpen.rewardTaskLogVersionBaseline =
            rewardTaskLogVersionBaseline;
        artifact.turnInOpen.rewardInventoryVersionBaseline =
            rewardInventoryVersionBaseline;
        artifact.turnInOpen.rewardOffersVersionBaseline =
            rewardOffersVersionBaseline;
        artifact.turnInOpen.rewardDialogTextBaselineRevision =
            artifact.rewardDialogTextBaselineRevision;
        artifact.turnInOpen.rewardMoneyBaselineCaptured =
            rewardMoneyBaseline >= 0;
        artifact.turnInOpen.rewardMoneyBaseline = rewardMoneyBaseline;
        artifact.turnInOpen.rewardHonorBaseline = rewardHonorBaseline;
        return rewardMoneyBaseline >= 0 && rewardHonorBaseline >= 0;
    }

    private void TraceReward(ReferenceFirstWorldWindowOwner window)
    {
        long issued = window.NpcTaskCompleteRequestIssuedCountForLifecycle -
                      taskCompleteIssuedBaseline;
        long succeeded = window.NpcTaskCompleteRequestSucceededCountForLifecycle -
                         taskCompleteSucceededBaseline;
        WorldInventoryState inventory = session.InventoryState;
        WorldPlayerStatusState status = session.PlayerStatusState;
        bool done = session.TaskLogState.FindActive(taskId) == null &&
                    ContainsDone(taskId);
        int restored;
        bool availableAgain = TryFindOffer(0, out restored) && restored == taskId;
        bool rewardFeedback = rewardFeedbackObserved &&
            artifact.rewardFeedbackEdgeRevision >
                artifact.rewardDialogTextBaselineRevision;
        bool rewardMoneyExact = inventory != null &&
            ReferenceNpcTaskLifecycleJourneyCheck.IsRewardMoneyTransitionAccepted(
                preludeMode,
                readyTurnInPreludeMode,
                rewardMoneyBaseline >= 0,
                rewardMoneyBaseline,
                artifact.freshJourneyMoneyBaselineCaptured,
                artifact.freshJourneyMoneyBaseline,
                inventory.Money);
        artifact.turnInOpen.rewardTaskLogReplacementDelta =
            session.TaskLogStateVersion - rewardTaskLogVersionBaseline;
        artifact.turnInOpen.rewardInventoryReplacementDelta =
            session.InventoryStateVersion - rewardInventoryVersionBaseline;
        artifact.turnInOpen.rewardOfferReplacementDelta =
            session.NpcTaskOffersStateVersion - rewardOffersVersionBaseline;
        artifact.turnInOpen.rewardFinalExactState0 = availableAgain;
        if (inventory != null)
        {
            artifact.turnInOpen.rewardMoneyFinal = inventory.Money;
            artifact.turnInOpen.rewardHonorFinal = inventory.Honor;
        }
        if (!string.IsNullOrEmpty(window.LastNpcTaskFailureCode) || issued > 1 ||
            succeeded > 1)
        {
            Fail(ReferenceNpcTaskLifecycleJourneyExit.CompleteRequestFailed);
            return;
        }
        if (issued == 1 && succeeded == 1 &&
            session.TaskLogStateVersion == rewardTaskLogVersionBaseline + 1 &&
            session.InventoryStateVersion == rewardInventoryVersionBaseline + 1 &&
            session.NpcTaskOffersStateVersion == rewardOffersVersionBaseline + 1 &&
            done && availableAgain && rewardFeedback &&
            inventory != null && CountGoalItems() == 0 &&
            rewardMoneyExact &&
            inventory.Honor - rewardHonorBaseline ==
                ReferenceNpcTaskLifecycleJourneyCheck.RewardHonorDelta &&
            status != null && status.Level == preRewardLevel &&
            status.Experience == preRewardExperience)
        {
            artifact.completeRequestIssuedDelta = issued;
            artifact.completeRequestSucceededDelta = succeeded;
            artifact.doneAuthoritative = true;
            artifact.activeRemoved = true;
            artifact.doneContainsTask = true;
            artifact.repeatableOfferRestored = true;
            artifact.rewardFeedback = true;
            artifact.inventoryAfter = 0;
            artifact.moneyDelta = inventory.Money - rewardMoneyBaseline;
            artifact.freshJourneyFinalMoney = inventory.Money;
            artifact.honorDelta = inventory.Honor - rewardHonorBaseline;
            artifact.taskRewardExperienceDelta = status.Experience - preRewardExperience;
            Record("reward.applied", window);
            if (readyTurnInPreludeMode)
            {
                if (!CompleteReadyTurnInPreludeAndBeginFresh(window))
                {
                    Fail(ReferenceNpcTaskLifecycleJourneyExit.RewardMismatch);
                }
                return;
            }
            if (preludeMode)
            {
                if (!CompletePreludeAndBeginFresh(window))
                {
                    Fail(ReferenceNpcTaskLifecycleJourneyExit.RewardMismatch);
                }
                return;
            }
            Exit = ReferenceNpcTaskLifecycleJourneyExit.Success;
            phase = Phase.Complete;
            return;
        }
        FailExpired(ReferenceNpcTaskLifecycleJourneyExit.RewardMismatch);
    }

    private bool CompletePreludeAndBeginFresh(
        ReferenceFirstWorldWindowOwner window)
    {
        ReferenceNpcTaskLifecyclePreludeArtifact prelude = artifact.prelude;
        prelude.completed = true;
        prelude.combatKills = artifact.combatKills;
        prelude.goalPickups = artifact.goalPickups;
        prelude.incidentalPickups = artifact.incidentalPickups;
        prelude.mapTransitions = artifact.mapTransitions;
        prelude.travelPathSetCount = artifact.travelPathSetCount;
        prelude.travelPathResumeCount = artifact.travelPathResumeCount;
        prelude.travelAuthoritativeHitInterruptions =
            artifact.travelAuthoritativeHitInterruptions;
        prelude.travelPixelCollisionStops = artifact.travelPixelCollisionStops;
        prelude.travelLegCompletedCount = artifact.travelLegCompletedCount;
        prelude.travelLegAbandonedCount = artifact.travelLegAbandonedCount;
        prelude.travelMaximumLegRecoveryCount =
            artifact.travelMaximumLegRecoveryCount;
        prelude.travelLegs.AddRange(artifact.travelLegs);
        prelude.attackRequestIssuedDelta = artifact.attackRequestIssuedDelta;
        prelude.attackRequestSucceededDelta = artifact.attackRequestSucceededDelta;
        prelude.pickupRequestIssuedDelta = artifact.pickupRequestIssuedDelta;
        prelude.pickupRequestSucceededDelta = artifact.pickupRequestSucceededDelta;
        prelude.completeRequestIssuedDelta = artifact.completeRequestIssuedDelta;
        prelude.completeRequestSucceededDelta = artifact.completeRequestSucceededDelta;
        prelude.turnInOpen = artifact.turnInOpen;
        prelude.progressAuthoritative = artifact.progressAuthoritative;
        prelude.readyAuthoritative = artifact.readyAuthoritative;
        prelude.doneAuthoritative = artifact.doneAuthoritative;
        prelude.repeatableOfferRestored = artifact.repeatableOfferRestored;
        prelude.rewardFeedback = artifact.rewardFeedback;
        prelude.inventoryReady = artifact.inventoryReady;
        prelude.inventoryAfter = artifact.inventoryAfter;
        prelude.moneyDelta = artifact.moneyDelta;
        prelude.honorDelta = artifact.honorDelta;
        prelude.taskRewardExperienceDelta = artifact.taskRewardExperienceDelta;
        prelude.combatExperienceAdvanced = artifact.combatExperienceAdvanced;
        if (!ReferenceNpcTaskLifecycleJourneyCheck.IsAcceptedPrelude(prelude))
        {
            return false;
        }

        artifact = new ReferenceNpcTaskLifecycleJourneyArtifact
        {
            initialMapId = ReferenceNpcTaskLifecycleJourneyCheck.InitialMapId,
            combatMapId = ReferenceNpcTaskLifecycleJourneyCheck.CombatMapId,
            npcName = ReferenceNpcTaskLifecycleJourneyCheck.NpcName,
            taskTitle = ReferenceNpcTaskLifecycleJourneyCheck.TaskTitle,
            goalItemName = ReferenceNpcTaskLifecycleJourneyCheck.GoalItemName,
            goalCount = ReferenceNpcTaskLifecycleJourneyCheck.GoalCount,
            combatModelId = ReferenceNpcTaskLifecycleJourneyCheck.CombatMonsterModelId,
            inventoryBefore = 0,
            prelude = prelude,
        };
        return ResetForFreshJourney(
            window,
            prelude.turnInOpen.rewardOffersVersionBaseline);
    }

    private bool ResetForFreshJourney(
        ReferenceFirstWorldWindowOwner window,
        long retainedOfferVersionBaseline)
    {
        if (window == null || window.MapDescriptor.id !=
                ReferenceNpcTaskLifecycleJourneyCheck.InitialMapId)
        {
            return false;
        }

        int availableTaskId;
        bool exactAvailableOffer = TryFindOffer(0, out availableTaskId) &&
            session.TaskLogState.FindActive(availableTaskId) == null &&
            DoneContainsExactly(availableTaskId) && CountGoalItems() == 0;
        ReferenceFirstWorldNpcDialog dialog = window.NpcDialog;
        bool singleLeaveOption = dialog != null && dialog.Options.Count == 1 &&
            dialog.Options[0].Leaves &&
            dialog.Options[0].Label == ReferenceFirstWorldNpcDialog.LeaveLabel;
        long observedVersion = session.NpcTaskOffersStateVersion;
        if (!ReferenceNpcTaskLifecycleJourneyCheck.
                IsPostCompleteRewardFeedbackReady(
                    retainedOfferVersionBaseline,
                    observedVersion,
                    dialog != null && dialog.Visible,
                    dialog != null && dialog.InSubView,
                    singleLeaveOption,
                    rewardFeedbackObserved,
                    exactAvailableOffer) ||
            !ClickDialogOption(window, 0) || dialog.Visible)
        {
            return false;
        }

        travelLegs = new ReferenceNpcTaskTravelLegLedger(artifact.travelLegs);
        preludeMode = false;
        readyTurnInPreludeMode = false;
        freshAfterPrelude = true;
        taskId = 0;
        targetEntityId = 0;
        goalGroundId = 0;
        currentPickupGroundId = 0;
        currentPickupItemId = 0;
        currentPickupItemCount = 0;
        currentPickupItemInventoryBaseline = 0;
        currentPickupIsGoal = false;
        targetApproachReplans = 0;
        usedCombatEntities.Clear();
        groundBaseline.Clear();
        initialGroundIds.Clear();
        authorizedGroundIds.Clear();
        foreach (int id in session.GroundItems.Keys)
        {
            initialGroundIds.Add(id);
        }
        scheduledPathWindow = null;
        scheduledPathX = 0f;
        scheduledPathY = 0f;
        scheduledPathRequiresPrimary = false;
        scheduledPathLocalHitBaseline = 0;
        lastRecoveredLocalHitRevision = 0;
        turnInOpenOffersVersionBaseline = 0;
        turnInOpenOffersResponseVersionSettled = 0;
        turnInOpenOfferReplacementSettled = false;
        movementFrameSequence = 0;
        movementFrames.Clear();
        rewardDialogTextCaptureActive = false;
        rewardFeedbackObserved = false;
        targetDiagnostic = new ReferenceNpcTaskLifecycleTargetDiagnostic();
        WorldPlayerStatusState status = session.PlayerStatusState;
        initialLevel = status == null ? -1 : status.Level;
        initialExperience = status == null ? -1 : status.Experience;
        // Official mode-6 reward feedback is the last DialogUI edge of the completion tick. The
        // fixture closes that one-option public subview, then performs the ordinary NPC pointer
        // open on the next phase so one fresh authoritative offer replacement is observed.
        Advance(Phase.OpenOffer);
        return true;
    }

    private bool CompleteReadyTurnInPreludeAndBeginFresh(
        ReferenceFirstWorldWindowOwner window)
    {
        ReferenceNpcTaskLifecycleReadyTurnInPreludeArtifact prelude =
            artifact.readyTurnInPrelude;
        prelude.completed = true;
        prelude.completeRequestIssuedDelta = artifact.completeRequestIssuedDelta;
        prelude.completeRequestSucceededDelta = artifact.completeRequestSucceededDelta;
        prelude.turnInOpen = artifact.turnInOpen;
        prelude.doneAuthoritative = artifact.doneAuthoritative;
        prelude.repeatableOfferRestored = artifact.repeatableOfferRestored;
        prelude.rewardFeedback = artifact.rewardFeedback;
        prelude.inventoryAfter = artifact.inventoryAfter;
        prelude.finalMoney = artifact.freshJourneyFinalMoney;
        prelude.moneyDelta = artifact.moneyDelta;
        prelude.honorDelta = artifact.honorDelta;
        prelude.taskRewardExperienceDelta = artifact.taskRewardExperienceDelta;
        if (!ReferenceNpcTaskLifecycleJourneyCheck.IsAcceptedReadyTurnInPrelude(prelude))
        {
            return false;
        }

        ReferenceNpcTaskLifecyclePreludeArtifact itemOnePrelude = artifact.prelude;
        artifact = new ReferenceNpcTaskLifecycleJourneyArtifact
        {
            initialMapId = ReferenceNpcTaskLifecycleJourneyCheck.InitialMapId,
            combatMapId = ReferenceNpcTaskLifecycleJourneyCheck.CombatMapId,
            npcName = ReferenceNpcTaskLifecycleJourneyCheck.NpcName,
            taskTitle = ReferenceNpcTaskLifecycleJourneyCheck.TaskTitle,
            goalItemName = ReferenceNpcTaskLifecycleJourneyCheck.GoalItemName,
            goalCount = ReferenceNpcTaskLifecycleJourneyCheck.GoalCount,
            combatModelId = ReferenceNpcTaskLifecycleJourneyCheck.CombatMonsterModelId,
            inventoryBefore = 0,
            prelude = itemOnePrelude,
            readyTurnInPrelude = prelude,
        };
        return ResetForFreshJourney(
            window,
            prelude.turnInOpen.rewardOffersVersionBaseline);
    }

    private bool ClickNpc(ReferenceFirstWorldWindowOwner window)
    {
        ReferenceWorldNpcActor actor = null;
        foreach (KeyValuePair<int, ReferenceWorldNpcActor> pair in window.Frame.NpcActors)
        {
            if (pair.Value != null && pair.Value.State != null &&
                pair.Value.State.DisplayName ==
                    ReferenceNpcTaskLifecycleJourneyCheck.NpcName)
            {
                actor = pair.Value;
                break;
            }
        }
        if (actor == null || actor.RootObject == null || actor.PickBox == null ||
            !actor.PickBox.enabled)
        {
            return false;
        }
        Vector3 screen = window.Camera.WorldToScreenPoint(actor.RootObject.transform.position);
        return screen.z > 0f &&
               window.HandlePointerInput(
                   screen,
                   true,
                   false,
                   false,
                   false,
                   false,
                   Time.unscaledTime,
                   Time.time,
                   Rect.zero) &&
               window.LastPointerAction ==
                   ReferenceFirstWorldPointerAction.NpcDialogOpened &&
               window.NpcDialog != null && window.NpcDialog.Visible &&
               window.NpcDialog.NpcName == ReferenceNpcTaskLifecycleJourneyCheck.NpcName;
    }

    private bool ClickTaskRow(
        ReferenceFirstWorldWindowOwner window,
        int expectedTaskId,
        byte state)
    {
        ReferenceFirstWorldNpcDialog dialog = window.NpcDialog;
        if (dialog == null || !dialog.Visible)
        {
            return false;
        }
        for (int index = 0; index < dialog.Options.Count; index++)
        {
            WorldNpcTaskOfferState offer = dialog.Options[index].Offer;
            if (offer != null && offer.TaskId == expectedTaskId && offer.State == state &&
                offer.Title == ReferenceNpcTaskLifecycleJourneyCheck.TaskTitle)
            {
                return ClickDialogOption(window, index);
            }
        }
        return false;
    }

    private static bool ClickDialogOption(
        ReferenceFirstWorldWindowOwner window,
        int index)
    {
        ReferenceFirstWorldNpcDialog dialog = window.NpcDialog;
        if (dialog == null || index < 0 || index >= dialog.Options.Count)
        {
            return false;
        }
        float scale = UIScale.Factor;
        Rect box = dialog.OptionRect(Screen.width, Screen.height, scale);
        Vector2 point = new Vector2(
            box.x + 34f * scale + 191.5f * scale,
            box.y + (4f + index * ReferenceFirstWorldNpcDialog.OptionRowHeight) * scale +
            ReferenceFirstWorldNpcDialog.OptionRowHeight * scale * 0.5f);
        return window.HandleNpcTaskLifecycleDialogPrimaryDown(point, scale);
    }

    private bool TryFindOffer(byte state, out int matchedTaskId)
    {
        matchedTaskId = 0;
        WorldNpcTaskOffersState offers = session.NpcTaskOffersState;
        if (offers == null || offers.NpcName != ReferenceNpcTaskLifecycleJourneyCheck.NpcName)
        {
            return false;
        }
        for (int index = 0; index < offers.Offers.Count; index++)
        {
            WorldNpcTaskOfferState offer = offers.Offers[index];
            if (offer != null && offer.State == state &&
                offer.Title == ReferenceNpcTaskLifecycleJourneyCheck.TaskTitle &&
                offer.Description ==
                    ReferenceNpcTaskLifecycleJourneyCheck.TaskDescription &&
                offer.Award == ReferenceNpcTaskLifecycleJourneyCheck.AwardText)
            {
                if (matchedTaskId != 0)
                {
                    return false;
                }
                matchedTaskId = offer.TaskId;
            }
        }
        return matchedTaskId > 0;
    }

    private bool TryFindSemanticOfferState(int selectedTaskId, out byte state)
    {
        state = 0;
        WorldNpcTaskOffersState offers = session.NpcTaskOffersState;
        if (selectedTaskId <= 0 || offers == null ||
            offers.NpcName != ReferenceNpcTaskLifecycleJourneyCheck.NpcName)
        {
            return false;
        }

        for (int index = 0; index < offers.Offers.Count; index++)
        {
            WorldNpcTaskOfferState offer = offers.Offers[index];
            if (offer != null && offer.TaskId == selectedTaskId &&
                offer.Title == ReferenceNpcTaskLifecycleJourneyCheck.TaskTitle &&
                offer.Description ==
                    ReferenceNpcTaskLifecycleJourneyCheck.TaskDescription &&
                offer.Award == ReferenceNpcTaskLifecycleJourneyCheck.AwardText)
            {
                state = offer.State;
                return true;
            }
        }
        return false;
    }

    private bool BeginTravelLeg(
        ReferenceNpcTaskTravelLegKind kind,
        float targetX,
        float targetY,
        int approachTargetEntityId = 0)
    {
        bool began = travelLegs.Begin(
            kind,
            approachTargetEntityId,
            targetX,
            targetY);
        SyncTravelLegDiagnostics();
        return began;
    }

    private bool EnsureCombatTargetApproachLeg(float targetX, float targetY)
    {
        if (travelLegs.ActiveKind ==
            ReferenceNpcTaskTravelLegKind.ApproachCombatTarget)
        {
            return travelLegs.ActiveKey ==
                       ReferenceNpcTaskTravelLegKind.ApproachCombatTarget + ":" +
                       targetEntityId.ToString(CultureInfo.InvariantCulture) &&
                   travelLegs.UpdateDestination(targetX, targetY);
        }
        return BeginTravelLeg(
            ReferenceNpcTaskTravelLegKind.ApproachCombatTarget,
            targetX,
            targetY,
            targetEntityId);
    }

    private bool CompleteTravelLeg(ReferenceNpcTaskTravelLegKind kind)
    {
        bool completed = travelLegs.Complete(kind);
        SyncTravelLegDiagnostics();
        return completed;
    }

    private bool CompleteTravelLegIfActive(ReferenceNpcTaskTravelLegKind kind)
    {
        if (travelLegs.ActiveKind == ReferenceNpcTaskTravelLegKind.None)
        {
            return true;
        }
        return CompleteTravelLeg(kind);
    }

    private bool AbandonTravelLeg(ReferenceNpcTaskTravelLegKind kind)
    {
        if (travelLegs.ActiveKind == ReferenceNpcTaskTravelLegKind.None)
        {
            return true;
        }
        bool abandoned = travelLegs.Abandon(kind);
        SyncTravelLegDiagnostics();
        return abandoned;
    }

    private void RecordTravelPixelCollisionStop()
    {
        travelLegs.RecordPixelCollisionStop();
        SyncTravelLegDiagnostics();
    }

    private void SyncTravelLegDiagnostics()
    {
        artifact.travelPathSetCount = travelLegs.TotalPathSetCount;
        artifact.travelPathResumeCount = travelLegs.TotalRecoveryCount;
        artifact.travelAuthoritativeHitInterruptions =
            travelLegs.TotalAuthoritativeHitInterruptions;
        artifact.travelPixelCollisionStops = travelLegs.TotalPixelCollisionStops;
        artifact.travelLegCompletedCount = travelLegs.CompletedLegCount;
        artifact.travelLegAbandonedCount = travelLegs.AbandonedLegCount;
        artifact.travelMaximumLegRecoveryCount =
            travelLegs.MaximumLegRecoveryCount;
    }

    private void BeginPath(
        ReferenceFirstWorldWindowOwner window,
        float x,
        float y,
        Phase next,
        ReferenceNpcTaskTravelLegKind legKind,
        bool requirePrimaryPath = false)
    {
        if (!BeginTravelLeg(legKind, x, y) ||
            !SchedulePath(window, x, y, false, requirePrimaryPath))
        {
            Fail(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
            return;
        }
        Advance(next);
    }

    private bool SchedulePath(
        ReferenceFirstWorldWindowOwner window,
        float x,
        float y,
        bool resumedAfterHit,
        bool requirePrimaryPath = false)
    {
        ReferenceWorldLocalPlayerDoll player = window == null || window.Frame == null
            ? null
            : window.Frame.Player;
        if (player == null || !window.TrySetNpcTaskLifecyclePath(x, y))
        {
            return false;
        }
        bool pathAccepted = requirePrimaryPath
            ? ReferenceNpcTaskLifecycleJourneyCheck.IsFixedCitySetPathAccepted(
                player.LastSetPathOutcome,
                player.LastSetPathMoveToTarget,
                player.PendingTargetHops)
            : resumedAfterHit
                ? ReferenceNpcTaskLifecycleJourneyCheck.IsRecoverySetPathAccepted(
                    player.LastSetPathOutcome,
                    player.LastSetPathMoveToTarget)
                : ReferenceNpcTaskLifecycleJourneyCheck.IsInitialSetPathAccepted(
                player.LastSetPathOutcome,
                player.LastSetPathMoveToTarget);
        if (!pathAccepted)
        {
            return false;
        }

        scheduledPathWindow = window;
        scheduledPathX = x;
        scheduledPathY = y;
        scheduledPathRequiresPrimary = requirePrimaryPath;
        if (!travelLegs.UpdateDestination(x, y) ||
            !travelLegs.RecordPathSet(resumedAfterHit))
        {
            return false;
        }
        SyncTravelLegDiagnostics();
        if (!resumedAfterHit)
        {
            scheduledPathLocalHitBaseline = LatestLocalHitRevision(player.EntityId);
            lastRecoveredLocalHitRevision = scheduledPathLocalHitBaseline;
        }
        return true;
    }

    private void CheckPathStillActive(ReferenceFirstWorldWindowOwner window)
    {
        if (ReferenceEquals(window, scheduledPathWindow) &&
            !window.Frame.Player.MovementActive)
        {
            if (TryResumeAfterAuthoritativeHurt(window))
            {
                FailExpired(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
                return;
            }
            if (window.Frame.Player.LastMovementStopReason ==
                ReferenceWorldPlayerMovementStopReason.PixelCollisionBlocked)
            {
                RecordTravelPixelCollisionStop();
            }
            Fail(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
            return;
        }
        FailExpired(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
    }

    private void CheckTravelStillActive(
        ReferenceFirstWorldWindowOwner window,
        GameBootstrap bootstrap)
    {
        if (!bootstrap.Switching && ReferenceEquals(window, scheduledPathWindow) &&
            !window.Frame.Player.MovementActive)
        {
            if (TryResumeAfterAuthoritativeHurt(window))
            {
                FailExpired(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
                return;
            }
            if (window.Frame.Player.LastMovementStopReason ==
                ReferenceWorldPlayerMovementStopReason.PixelCollisionBlocked)
            {
                RecordTravelPixelCollisionStop();
            }
            Fail(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
            return;
        }
        FailExpired(ReferenceNpcTaskLifecycleJourneyExit.TravelFailed);
    }

    private bool TryResumeAfterAuthoritativeHurt(
        ReferenceFirstWorldWindowOwner window)
    {
        ReferenceWorldLocalPlayerDoll player = window == null || window.Frame == null
            ? null
            : window.Frame.Player;
        if (player == null || !ReferenceEquals(window, scheduledPathWindow) ||
            player.LastMovementStopReason !=
                ReferenceWorldPlayerMovementStopReason.AuthoritativeHurt)
        {
            return false;
        }

        WorldCombatResultState hit;
        if (!TryFindPendingLocalHit(player.EntityId, out hit))
        {
            return false;
        }

        ReferenceNpcTaskTravelRecoveryDecision decision =
            ReferenceNpcTaskLifecycleJourneyCheck.EvaluateTravelRecovery(
                hit,
                player.EntityId,
                scheduledPathLocalHitBaseline,
                lastRecoveredLocalHitRevision,
                player.LastMovementStopReason,
                player.ActionActive,
                travelLegs.ActiveRecoveryCount);
        if (decision == ReferenceNpcTaskTravelRecoveryDecision.WaitForHurtEnd)
        {
            return true;
        }
        if (decision != ReferenceNpcTaskTravelRecoveryDecision.ReissueSameDestination)
        {
            return false;
        }

        // PlayHurt owns a 0.1-second non-looping action and clears movement. A normal user cannot
        // retain the old click; after that action the driver supplies the unchanged destination
        // to production SetPathTo once for this unique authoritative revision.
        lastRecoveredLocalHitRevision = hit.Revision;
        if (!travelLegs.RecordAuthoritativeHitInterruption())
        {
            return false;
        }
        SyncTravelLegDiagnostics();
        return SchedulePath(
            window,
            scheduledPathX,
            scheduledPathY,
            true,
            scheduledPathRequiresPrimary);
    }

    private bool TryFindPendingLocalHit(
        int playerEntityId,
        out WorldCombatResultState selected)
    {
        selected = null;
        IReadOnlyList<WorldCombatResultState> results = session.CombatResults;
        for (int index = 0; index < results.Count; index++)
        {
            WorldCombatResultState candidate = results[index];
            if (!ReferenceNpcTaskLifecycleJourneyCheck.IsAdmittedTravelHit(
                    candidate,
                    playerEntityId,
                    scheduledPathLocalHitBaseline,
                    lastRecoveredLocalHitRevision))
            {
                continue;
            }
            if (selected == null || candidate.Revision < selected.Revision)
            {
                selected = candidate;
            }
        }
        return selected != null;
    }

    private long LatestLocalHitRevision(int playerEntityId)
    {
        long revision = 0;
        IReadOnlyList<WorldCombatResultState> results = session.CombatResults;
        for (int index = 0; index < results.Count; index++)
        {
            WorldCombatResultState result = results[index];
            if (result != null && result.TargetEntityId == playerEntityId)
            {
                revision = Math.Max(revision, result.Revision);
            }
        }
        return revision;
    }

    private bool LiveCombatTarget(ReferenceWorldMonsterActor actor)
    {
        return CombatTargetRejection(actor) ==
            ReferenceNpcTaskCombatTargetRejection.None;
    }

    private static ReferenceNpcTaskCombatTargetRejection CombatTargetRejection(
        ReferenceWorldMonsterActor actor)
    {
        WorldEntityState state = actor == null ? null : actor.State;
        if (state == null)
        {
            return ReferenceNpcTaskCombatTargetRejection.MissingActorOrState;
        }
        if (state.ModelOrSkillId !=
            ReferenceNpcTaskLifecycleJourneyCheck.CombatMonsterModelId)
        {
            return ReferenceNpcTaskCombatTargetRejection.ModelMismatch;
        }
        if (!actor.SourceOwnerLive)
        {
            return ReferenceNpcTaskCombatTargetRejection.SourceOwnerNotLive;
        }
        if (actor.Dying)
        {
            return ReferenceNpcTaskCombatTargetRejection.Dying;
        }
        if (actor.RootObject == null)
        {
            return ReferenceNpcTaskCombatTargetRejection.RootMissing;
        }
        if (actor.PickBox == null)
        {
            return ReferenceNpcTaskCombatTargetRejection.ColliderMissing;
        }
        if (!actor.PickBox.enabled)
        {
            return ReferenceNpcTaskCombatTargetRejection.ColliderDisabled;
        }
        if (state.FriendlyFlag != 0 || state.OwnerEntityId != 0)
        {
            return ReferenceNpcTaskCombatTargetRejection.FriendlyOrOwned;
        }
        if (state.HitPointsOrRadiusPixels <= 0)
        {
            return ReferenceNpcTaskCombatTargetRejection.NonPositiveHitPoints;
        }
        return ReferenceNpcTaskCombatTargetRejection.None;
    }

    private ReferenceNpcTaskLifecycleTargetDiagnostic CaptureCombatTargetDiagnostic(
        ReferenceFirstWorldWindowOwner window)
    {
        ReferenceNpcTaskLifecycleTargetDiagnostic value =
            new ReferenceNpcTaskLifecycleTargetDiagnostic();
        WorldEntitySceneSnapshot sceneSnapshot = session.WorldEntitySceneSnapshot;
        WorldEntityPublicationState publication = sceneSnapshot == null
            ? null
            : sceneSnapshot.Publication;
        IReadOnlyDictionary<int, WorldEntityState> worldEntitySnapshot =
            sceneSnapshot == null
                ? new Dictionary<int, WorldEntityState>()
                : sceneSnapshot.Entities;
        value.publicationCaptureStable = sceneSnapshot != null &&
            publication != null &&
            publication.SceneGeneration == sceneSnapshot.SceneGeneration &&
            publication.MapId == sceneSnapshot.MapId &&
            publication.SnapshotVersion == sceneSnapshot.SnapshotVersion &&
            publication.EntityCount == worldEntitySnapshot.Count;
        if (publication != null)
        {
            value.sceneGeneration = publication.SceneGeneration;
            value.publicationMapId = publication.MapId;
            value.decodedSpawnFrameCount = publication.DecodedSpawnFrameCount;
            value.admittedSpawnFrameCount = publication.AdmittedSpawnFrameCount;
            value.rejectedMapGuardSpawnFrameCount =
                publication.RejectedMapGuardSpawnFrameCount;
            value.receivedRemovalFrameCount = publication.ReceivedRemovalFrameCount;
            value.publishedSnapshotVersion = publication.SnapshotVersion;
            value.publishedEntityCount = publication.EntityCount;
        }
        if (window != null)
        {
            value.windowBuildGeneration = window.BuildGeneration;
            ReferenceFirstWorldInitialFrame observedFrame = window.Frame;
            if (observedFrame != null)
            {
                value.actorRebuildGeneration =
                    observedFrame.WorldEntityActorRebuildGeneration;
                value.actorSceneGeneration =
                    observedFrame.LastWorldEntitySceneGeneration;
                value.actorSnapshotMapId =
                    observedFrame.LastWorldEntitySnapshotMapId;
                value.actorSnapshotVersion = observedFrame.LastWorldEntitiesVersion;
                value.actorSnapshotEntityCount =
                    observedFrame.LastWorldEntitySnapshotCount;
            }
        }
        foreach (KeyValuePair<int, WorldEntityState> pair in worldEntitySnapshot)
        {
            WorldEntityState state = pair.Value;
            value.worldEntityCount++;
            if (state == null || state.Kind != WorldEntityKind.Monster)
            {
                continue;
            }
            value.worldMonsterCount++;
            IncrementModelCount(
                state.ModelOrSkillId,
                ref value.worldModel206Count,
                ref value.worldModel262Count,
                ref value.worldModel274Count,
                ref value.worldOtherModelCount);
        }

        float nearestSquared = float.PositiveInfinity;
        int nearestEntityId = 0;
        ReferenceWorldLocalPlayerDoll player = window == null || window.Frame == null
            ? null
            : window.Frame.Player;
        foreach (KeyValuePair<int, ReferenceWorldMonsterActor> pair in
                 window.Frame.MonsterActors)
        {
            ReferenceWorldMonsterActor actor = pair.Value;
            WorldEntityState state = actor == null ? null : actor.State;
            value.actorCount++;
            if (state != null)
            {
                IncrementModelCount(
                    state.ModelOrSkillId,
                    ref value.actorModel206Count,
                    ref value.actorModel262Count,
                    ref value.actorModel274Count,
                    ref value.actorOtherModelCount);
            }

            ReferenceNpcTaskCombatTargetRejection rejection =
                CombatTargetRejection(actor);
            IncrementRejection(value, rejection);
            if (rejection == ReferenceNpcTaskCombatTargetRejection.None)
            {
                value.admissibleModel262Count++;
            }
            if (state == null || state.ModelOrSkillId !=
                ReferenceNpcTaskLifecycleJourneyCheck.CombatMonsterModelId)
            {
                continue;
            }

            if (actor.SourceOwnerLive)
            {
                value.model262SourceOwnerLiveCount++;
            }
            if (!actor.Dying)
            {
                value.model262NonDyingCount++;
            }
            if (actor.RootObject != null)
            {
                value.model262RootPresentCount++;
            }
            if (actor.Renderer == null)
            {
                value.model262RendererMissing++;
            }
            else
            {
                value.model262RendererPresentCount++;
                if (actor.Renderer.sprite == null)
                {
                    value.model262SpriteMissing++;
                }
                else
                {
                    value.model262SpritePresentCount++;
                }
            }
            if (actor.PickBox != null)
            {
                value.model262ColliderPresentCount++;
                if (actor.PickBox.enabled)
                {
                    value.model262ColliderEnabledCount++;
                }
            }
            if (state.FriendlyFlag == 0)
            {
                value.model262HostileCount++;
            }
            if (state.OwnerEntityId == 0)
            {
                value.model262UnownedCount++;
            }
            if (state.HitPointsOrRadiusPixels > 0)
            {
                value.model262PositiveHitPointsCount++;
            }
            bool combatPresent =
                CombatWorld.ContainsMonsterForIndependentRuntime(pair.Key);
            if (combatPresent)
            {
                value.model262CombatWorldPresentCount++;
            }
            else
            {
                value.model262CombatWorldMissing++;
            }
            bool worldStatePresent = session.WorldEntities.ContainsKey(pair.Key);
            if (worldStatePresent)
            {
                value.model262WorldStatePresentCount++;
            }
            else
            {
                value.model262WorldStateMissing++;
            }

            float dx = player == null ? 0f : actor.WorldX - player.WorldX;
            float dy = player == null ? 0f : actor.WorldY - player.WorldY;
            float squared = player == null
                ? float.PositiveInfinity
                : dx * dx + dy * dy;
            float distance = player == null ? 0f : Mathf.Sqrt(squared);
            bool lineOfSight = player != null && HasLineOfSight(window, actor);
            if (lineOfSight)
            {
                value.model262LineOfSightCount++;
            }
            if (distance > 0f && distance <=
                ReferenceNpcTaskLifecycleJourneyCheck.TargetPointerDistance)
            {
                value.model262PointerRangeCount++;
            }
            Vector3 screen = Vector3.zero;
            bool screenVisible = false;
            if (actor.RootObject != null)
            {
                screen = window.Camera.WorldToScreenPoint(
                    actor.RootObject.transform.position);
                screenVisible = screen.z > 0f && screen.x >= 0f &&
                    screen.x <= Screen.width && screen.y >= 0f &&
                    screen.y <= Screen.height;
                if (screenVisible)
                {
                    value.model262OnScreenCount++;
                }
            }
            if (value.samplePresent && squared >= nearestSquared)
            {
                continue;
            }
            nearestSquared = squared;
            nearestEntityId = pair.Key;
            value.samplePresent = true;
            value.sampleSourceX = state.X;
            value.sampleSourceY = state.Y;
            value.sampleCurrentX = actor.WorldX;
            value.sampleCurrentY = actor.WorldY;
            value.sampleHitPoints = state.HitPointsOrRadiusPixels;
            value.sampleSourceOwnerLive = actor.SourceOwnerLive;
            value.sampleDying = actor.Dying;
            value.sampleRootPresent = actor.RootObject != null;
            value.sampleRendererPresent = actor.Renderer != null;
            value.sampleSpritePresent = actor.Renderer != null &&
                actor.Renderer.sprite != null;
            value.sampleColliderPresent = actor.PickBox != null;
            value.sampleColliderEnabled = actor.PickBox != null &&
                actor.PickBox.enabled;
            value.sampleSelectableOwnerLive = actor.SelectableOwner != null &&
                actor.SourceOwnerLive;
            value.sampleCombatWorldPresent =
                combatPresent;
            value.sampleWorldStatePresent = worldStatePresent;
            value.sampleDistance = distance;
            value.sampleLineOfSight = lineOfSight;
            value.sampleRejection = rejection.ToString();
            value.sampleScreenX = screen.x;
            value.sampleScreenY = screen.y;
            value.sampleScreenZ = screen.z;
            value.sampleScreenVisible = screenVisible;
        }
        if (nearestEntityId > 0)
        {
            ReferenceWorldApproachEvaluation approach;
            value.model262PathEvaluatedCount = 1;
            if (window.TryEvaluateNpcTaskLifecycleApproach(
                    nearestEntityId,
                    out approach) && approach.Admissible)
            {
                value.model262PathAdmissibleCount = 1;
            }
        }
        return value;
    }

    private static void IncrementModelCount(
        int modelId,
        ref int model206,
        ref int model262,
        ref int model274,
        ref int other)
    {
        if (modelId == ReferenceWorldMonsterActor.Map6TurtleModelId)
        {
            model206++;
        }
        else if (modelId == ReferenceWorldMonsterActor.Map6WoodenManModelId)
        {
            model262++;
        }
        else if (modelId == ReferenceWorldMonsterActor.Model274Id)
        {
            model274++;
        }
        else
        {
            other++;
        }
    }

    private static void IncrementRejection(
        ReferenceNpcTaskLifecycleTargetDiagnostic value,
        ReferenceNpcTaskCombatTargetRejection rejection)
    {
        switch (rejection)
        {
            case ReferenceNpcTaskCombatTargetRejection.MissingActorOrState:
                value.rejectedMissingActorOrState++;
                break;
            case ReferenceNpcTaskCombatTargetRejection.ModelMismatch:
                value.rejectedModelMismatch++;
                break;
            case ReferenceNpcTaskCombatTargetRejection.SourceOwnerNotLive:
                value.rejectedSourceOwnerNotLive++;
                break;
            case ReferenceNpcTaskCombatTargetRejection.Dying:
                value.rejectedDying++;
                break;
            case ReferenceNpcTaskCombatTargetRejection.RootMissing:
                value.rejectedRootMissing++;
                break;
            case ReferenceNpcTaskCombatTargetRejection.ColliderMissing:
                value.rejectedColliderMissing++;
                break;
            case ReferenceNpcTaskCombatTargetRejection.ColliderDisabled:
                value.rejectedColliderDisabled++;
                break;
            case ReferenceNpcTaskCombatTargetRejection.FriendlyOrOwned:
                value.rejectedFriendlyOrOwned++;
                break;
            case ReferenceNpcTaskCombatTargetRejection.NonPositiveHitPoints:
                value.rejectedNonPositiveHitPoints++;
                break;
        }
    }

    private static bool HasLineOfSight(
        ReferenceFirstWorldWindowOwner window,
        ReferenceWorldMonsterActor actor)
    {
        ReferenceWorldLocalPlayerDoll player = window.Frame.Player;
        ReferenceWorldCollisionMask mask = window.CollisionMask;
        int ax;
        int ay;
        int bx;
        int by;
        return player != null && actor != null && mask != null &&
               mask.TryWorldPixelToCell(player.WorldX, player.WorldY, out ax, out ay) &&
               mask.TryWorldPixelToCell(actor.WorldX, actor.WorldY, out bx, out by) &&
               mask.IsLineWalkable(ax, ay, bx, by);
    }

    private int CountGoalItems()
    {
        return CountInventoryItem(ReferenceNpcTaskLifecycleJourneyCheck.GoalItemId);
    }

    private int CountInventoryItem(int itemId)
    {
        return ReferenceWorldGroundLootCheck.CountInventoryItem(
            session.InventoryState,
            itemId);
    }

    private bool ContainsDone(int id)
    {
        IReadOnlyList<int> done = session.TaskLogState.Done;
        for (int index = 0; index < done.Count; index++)
        {
            if (done[index] == id)
            {
                return true;
            }
        }
        return false;
    }

    private bool DoneContainsExactly(int id)
    {
        IReadOnlyList<int> done = session.TaskLogState.Done;
        return done.Count == 1 && done[0] == id;
    }

    private long LatestCombatResultRevision()
    {
        long revision = 0;
        IReadOnlyList<WorldCombatResultState> results = session.CombatResults;
        for (int index = 0; index < results.Count; index++)
        {
            if (results[index] != null)
            {
                revision = Math.Max(revision, results[index].Revision);
            }
        }
        return revision;
    }

    private void Record(string stage, ReferenceFirstWorldWindowOwner window)
    {
        WorldActiveTaskState active = taskId <= 0
            ? null
            : session.TaskLogState.FindActive(taskId);
        WorldInventoryState inventory = session.InventoryState;
        ReferenceNpcTaskLifecycleJourneyCheckpoint checkpoint =
            new ReferenceNpcTaskLifecycleJourneyCheckpoint
        {
            stage = readyTurnInPreludeMode
                ? "ready10." + stage
                : preludeMode ? "prelude." + stage : stage,
            mapId = window.MapDescriptor.id,
            collected = CountGoalItems(),
            progress = active == null ? string.Empty : active.Progress,
            ready = active != null && active.Ready,
            taskLogVersion = session.TaskLogStateVersion,
            inventoryVersion = session.InventoryStateVersion,
            playerStatusVersion = session.PlayerStatusStateVersion,
            money = inventory == null ? 0 : inventory.Money,
            honor = inventory == null ? 0 : inventory.Honor,
        };
        if (readyTurnInPreludeMode)
        {
            artifact.readyTurnInPrelude.checkpoints.Add(checkpoint);
        }
        else if (preludeMode)
        {
            artifact.prelude.checkpoints.Add(checkpoint);
        }
        else
        {
            artifact.checkpoints.Add(checkpoint);
        }
    }

    private void Advance(Phase next)
    {
        phase = next;
        SetPhaseDeadline();
    }

    private void SetPhaseDeadline()
    {
        phaseDeadline = Time.realtimeSinceStartup +
            ReferenceNpcTaskLifecycleJourneyCheck.WaitSecondsForPhase(
                phase.ToString());
    }

    private void FailExpired(ReferenceNpcTaskLifecycleJourneyExit exit)
    {
        float now = Time.realtimeSinceStartup;
        if (now >= phaseDeadline)
        {
            ReferenceWorldLocalPlayerDoll player =
                lastObservedWindow == null || lastObservedWindow.Frame == null
                    ? null
                    : lastObservedWindow.Frame.Player;
            if (exit == ReferenceNpcTaskLifecycleJourneyExit.TravelFailed &&
                ReferenceEquals(lastObservedWindow, scheduledPathWindow) &&
                ReferenceNpcTaskLifecycleJourneyCheck.TryMarkActiveTravelDeadlineExpired(
                    phase.ToString(),
                    now,
                    phaseDeadline,
                    player))
            {
                // Preserve the exact active path after its terminal cause is marked and before
                // the existing root/window terminal owner clears movement and settles actions.
                CaptureMovementFrame(lastObservedWindow);
            }
            Fail(exit);
        }
    }

    private void Fail(ReferenceNpcTaskLifecycleJourneyExit exit)
    {
        if (Terminal || failurePending)
        {
            return;
        }
        failurePending = true;
        Exit = exit;
        terminalPartial = CreateTerminalPartial(exit, phase.ToString());
        PopulateFailureMovement(terminalPartial);
        failurePending = false;
        phase = Phase.Failed;
    }

    private ReferenceNpcTaskLifecycleJourneyPartialArtifact CreateTerminalPartial(
        ReferenceNpcTaskLifecycleJourneyExit exit,
        string phaseName)
    {
        return new ReferenceNpcTaskLifecycleJourneyPartialArtifact
            {
                failureCode = ReferenceNpcTaskLifecycleJourneyCheck.FailureCode(exit),
                phase = phaseName,
                mapId = session.State == ClientSessionState.InWorld &&
                        session.PlayerStatusState != null
                    ? (lastObservedWindow == null ? 0 : lastObservedWindow.MapDescriptor.id)
                    : 0,
                combatKills = artifact.combatKills,
                goalPickups = artifact.goalPickups,
                incidentalPickups = artifact.incidentalPickups,
                collected = CountGoalItems(),
                checkpointCount = artifact.checkpoints.Count +
                    artifact.prelude.checkpoints.Count +
                    artifact.readyTurnInPrelude.checkpoints.Count,
                baselineAlreadyAvailable = artifact.baselineAlreadyAvailable,
                baselineRecoverableActive = artifact.baselineRecoverableActive,
                baselineAfterPreludeAvailable =
                    artifact.baselineAfterPreludeAvailable,
                baselineAfterReadyTurnInAvailable =
                    artifact.baselineAfterReadyTurnInAvailable,
                baselineRepeatableAvailableDone =
                    artifact.baselineRepeatableAvailableDone,
                baselineAbandonRequestIssuedDelta =
                    artifact.baselineAbandonRequestIssuedDelta,
                baselineAbandonRequestSucceededDelta =
                    artifact.baselineAbandonRequestSucceededDelta,
                baselineTaskLogReplaced = artifact.baselineTaskLogReplaced,
                baselineOfferReplaced = artifact.baselineOfferReplaced,
                baselineAvailableAuthoritative =
                    artifact.baselineAvailableAuthoritative,
                openOfferVersionBaseline = artifact.openOfferVersionBaseline,
                openOfferResponseVersionSettled =
                    artifact.openOfferResponseVersionSettled,
                openOfferReplacementDelta =
                    artifact.openOfferReplacementDelta,
                preAcceptOfferVersionSettled =
                    artifact.preAcceptOfferVersionSettled,
                preAcceptOfferVersionObserved =
                    artifact.preAcceptOfferVersionObserved,
                preAcceptOfferVersionUnchanged =
                    artifact.preAcceptOfferVersionUnchanged,
                prelude = artifact.prelude,
                readyTurnInPrelude = artifact.readyTurnInPrelude,
                turnInOpen = artifact.turnInOpen,
                acceptRequestIssuedDelta = artifact.acceptRequestIssuedDelta,
                acceptRequestSucceededDelta = artifact.acceptRequestSucceededDelta,
                acceptTaskLogVersionBaseline =
                    artifact.acceptTaskLogVersionBaseline,
                acceptOfferVersionBaseline = artifact.acceptOfferVersionBaseline,
                acceptTaskLogReplacementDelta =
                    artifact.acceptTaskLogReplacementDelta,
                acceptOfferReplacementDelta =
                    artifact.acceptOfferReplacementDelta,
                acceptFinalOfferState = artifact.acceptFinalOfferState,
                acceptFinalOfferExactState1 =
                    artifact.acceptFinalOfferExactState1,
                acceptConflictingTransition =
                    artifact.acceptConflictingTransition,
                acceptOfferReplacementPattern =
                    artifact.acceptOfferReplacementPattern,
                completeRequestIssuedDelta = artifact.completeRequestIssuedDelta,
                completeRequestSucceededDelta = artifact.completeRequestSucceededDelta,
                attackRequestIssuedDelta = artifact.attackRequestIssuedDelta,
                attackRequestSucceededDelta = artifact.attackRequestSucceededDelta,
                pickupRequestIssuedDelta = artifact.pickupRequestIssuedDelta,
                pickupRequestSucceededDelta = artifact.pickupRequestSucceededDelta,
                firstCombatDistance = artifact.firstCombatDistance,
                firstCombatLineOfSight = artifact.firstCombatLineOfSight,
                rewardDialogTextWindowVersionBaseline =
                    artifact.rewardDialogTextWindowVersionBaseline,
                rewardDialogTextBaselineRevision =
                    artifact.rewardDialogTextBaselineRevision,
                rewardDialogTextWindowVersionObserved =
                    artifact.rewardDialogTextWindowVersionObserved,
                rewardDialogTextObservedThroughRevision =
                    artifact.rewardDialogTextObservedThroughRevision,
                rewardFeedbackEdgeRevision =
                    artifact.rewardFeedbackEdgeRevision,
                rewardDialogTextCursorFailure =
                    artifact.rewardDialogTextCursorFailure,
                rewardDialogTextEdges = artifact.rewardDialogTextEdges,
                travelPathSetCount = artifact.travelPathSetCount,
                travelPathResumeCount = artifact.travelPathResumeCount,
                travelAuthoritativeHitInterruptions =
                    artifact.travelAuthoritativeHitInterruptions,
                travelPixelCollisionStops = artifact.travelPixelCollisionStops,
                travelLegCompletedCount = artifact.travelLegCompletedCount,
                travelLegAbandonedCount = artifact.travelLegAbandonedCount,
                travelMaximumLegRecoveryCount =
                    artifact.travelMaximumLegRecoveryCount,
                travelLegs = artifact.travelLegs,
                currentTravelLegKey = travelLegs.ActiveKey,
                currentTravelLegRecoveryCount = travelLegs.ActiveRecoveryCount,
                targetDiagnostic = targetDiagnostic,
            };
    }

    private void PopulateFailureMovement(
        ReferenceNpcTaskLifecycleJourneyPartialArtifact partial)
    {
        ReferenceFirstWorldWindowOwner window = lastObservedWindow;
        ReferenceWorldLocalPlayerDoll player = window == null || window.Frame == null
            ? null
            : window.Frame.Player;
        if (player == null)
        {
            return;
        }

        WorldCombatResultState local = LatestLocalCombatResult(player.EntityId);
        partial.playerFinalX = player.WorldX;
        partial.playerFinalY = player.WorldY;
        partial.pathTargetX = scheduledPathX;
        partial.pathTargetY = scheduledPathY;
        partial.pathRemainingDistance = Distance(
            player.WorldX,
            player.WorldY,
            scheduledPathX,
            scheduledPathY);
        partial.pathArrivalPredicate = partial.pathRemainingDistance <=
            ReferenceNpcTaskLifecycleJourneyCheck.TravelArrivalDistance;
        partial.pathMovementActive = player.MovementActive;
        partial.pathIndex = player.MovementPathIndex;
        partial.pathCount = player.MovementPathCount;
        partial.pathPendingHops = player.PendingTargetHops;
        partial.pathSetOutcome = (int)player.LastSetPathOutcome;
        partial.pathSetOutcomeName = player.LastSetPathOutcome.ToString();
        partial.pathSetMoveToTarget = player.LastSetPathMoveToTarget;
        partial.pathStopReason = (int)player.LastMovementStopReason;
        partial.pathStopReasonName = player.LastMovementStopReason.ToString();
        partial.pathFinalPixelWalkable = window.CollisionMask != null &&
            window.CollisionMask.IsMaskPixelWalkable(
                player.WorldX,
                player.WorldY);
        if (local != null)
        {
            partial.pathLastLocalHitRevision = local.Revision;
            partial.pathLastLocalHitAttackerEntityId = local.AttackerEntityId;
            partial.pathLastLocalHitFatal = local.Fatal;
            partial.pathLastLocalHitPoints = local.TargetHitPoints;
        }
        partial.movementFrames.AddRange(movementFrames);
    }

    private void CaptureMovementFrame(ReferenceFirstWorldWindowOwner window)
    {
        if (!ReferenceEquals(window, scheduledPathWindow) || window.Frame == null ||
            window.Frame.Player == null)
        {
            return;
        }

        ReferenceWorldLocalPlayerDoll player = window.Frame.Player;
        WorldCombatResultState local = LatestLocalCombatResult(player.EntityId);
        float remaining = Distance(
            player.WorldX,
            player.WorldY,
            scheduledPathX,
            scheduledPathY);
        ReferenceNpcTaskLifecycleMovementFrame frame =
            new ReferenceNpcTaskLifecycleMovementFrame
            {
                sequence = ++movementFrameSequence,
                phase = phase.ToString(),
                playerX = player.WorldX,
                playerY = player.WorldY,
                targetX = scheduledPathX,
                targetY = scheduledPathY,
                remainingDistance = remaining,
                arrivalPredicate = remaining <=
                    ReferenceNpcTaskLifecycleJourneyCheck.TravelArrivalDistance,
                movementActive = player.MovementActive,
                pathIndex = player.MovementPathIndex,
                pathCount = player.MovementPathCount,
                pendingHops = player.PendingTargetHops,
                setPathOutcome = (int)player.LastSetPathOutcome,
                setPathOutcomeName = player.LastSetPathOutcome.ToString(),
                setPathMoveToTarget = player.LastSetPathMoveToTarget,
                stopReason = (int)player.LastMovementStopReason,
                stopReasonName = player.LastMovementStopReason.ToString(),
                finalPixelWalkable = window.CollisionMask != null &&
                    window.CollisionMask.IsMaskPixelWalkable(
                        player.WorldX,
                        player.WorldY),
                actionActive = player.ActionActive,
                actionPoseKind = (int)player.CurrentActionPoseKind,
                localCombatRevision = local == null ? 0 : local.Revision,
                localHit = local != null && !local.Missed,
                localHitAttackerEntityId = local == null ? 0 : local.AttackerEntityId,
                localHitFatal = local != null && local.Fatal,
                localHitPoints = local == null ? 0 : local.TargetHitPoints,
            };
        movementFrames.Add(frame);
        if (movementFrames.Count >
            ReferenceNpcTaskLifecycleJourneyCheck.MovementFrameRingCapacity)
        {
            movementFrames.RemoveAt(0);
        }
    }

    private WorldCombatResultState LatestLocalCombatResult(int playerEntityId)
    {
        WorldCombatResultState selected = null;
        IReadOnlyList<WorldCombatResultState> results = session.CombatResults;
        for (int index = 0; index < results.Count; index++)
        {
            WorldCombatResultState candidate = results[index];
            if (candidate != null && candidate.TargetEntityId == playerEntityId &&
                (selected == null || candidate.Revision > selected.Revision))
            {
                selected = candidate;
            }
        }
        return selected;
    }

    private static bool StatusAdvanced(
        int beforeLevel,
        int beforeExperience,
        int afterLevel,
        int afterExperience)
    {
        return afterLevel > beforeLevel ||
               afterLevel == beforeLevel && afterExperience > beforeExperience;
    }

    private static bool PlayerPathFinishedWithinArrival(
        ReferenceFirstWorldWindowOwner window,
        float targetX,
        float targetY)
    {
        ReferenceWorldLocalPlayerDoll player =
            window == null || window.Frame == null ? null : window.Frame.Player;
        return player != null && !player.MovementActive &&
               player.MovementPathCount == 0 && player.PendingTargetHops == 0 &&
               player.LastMovementStopReason ==
                   ReferenceWorldPlayerMovementStopReason.Arrived &&
               PlayerDistance(window, targetX, targetY) <=
                   ReferenceNpcTaskLifecycleJourneyCheck.TravelArrivalDistance;
    }

    private static float PlayerDistance(
        ReferenceFirstWorldWindowOwner window,
        float x,
        float y)
    {
        ReferenceWorldLocalPlayerDoll player = window.Frame == null
            ? null
            : window.Frame.Player;
        return player == null
            ? float.PositiveInfinity
            : Distance(player.WorldX, player.WorldY, x, y);
    }

    private static float Distance(float ax, float ay, float bx, float by)
    {
        float dx = bx - ax;
        float dy = by - ay;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
}

}
