using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{

internal enum ReferenceMonsterRealCombatTailExit
{
    Success = 0,
    FixtureUnavailable = 40,
    FixtureUnreachable = 41,
    RequestFailed = 42,
    FatalResultTimeout = 43,
    FatalProjectionTimeout = 44,
    FatalProjectionMismatch = 45,
    DeathLifecycleFailed = 46,
    RemovalFailed = 47,
    Timeout = 48,
    Unexpected = 70,
}

internal static class ReferenceMonsterRealCombatTailCheck
{
    public const string Argument = "--ffo-monster-real-combat-tail-check";
    public const string Marker = "FFO_MONSTER_REAL_COMBAT_TAIL_V1";
    public const string ArtifactFileName = "rebuilt-monster-real-combat-tail.json";
    public const string PartialArtifactFileName =
        "rebuilt-monster-real-combat-tail-partial.json";
    public const int FixtureMapId = 6;
    public const string FixtureMapFile = "qw0006";
    public const int FixtureSpawnId = 12;
    public const int FixtureMonsterDefinitionId = 1074;
    public const int ModelId = 274;
    public const int FixtureSpawnX = 11022;
    public const int FixtureSpawnY = 2869;
    public const int FixturePlayerX = 10995;
    public const int FixturePlayerY = 2865;
    public const float TimeoutSeconds = 35f;
    public const int MaximumCandidateAttempts = 8;
    public const int MaximumSelectionsPerEntity = 2;
    public const float CandidateRescanIntervalSeconds = 0.1f;
    public const float CandidateRetryMovementPixels = 24f;
    public const float ProgressWindowSeconds = 0.75f;
    public const float ProgressDistancePixels = 1f;
    public const float FatalProjectionTimeoutSeconds = 2f;

    internal static int CandidateTier(ReferenceWorldApproachEvaluation evaluation)
    {
        if (!evaluation.Evaluated)
        {
            return 2;
        }
        if (evaluation.AlreadyInRange && evaluation.CurrentLineOfSight)
        {
            return 0;
        }
        return !evaluation.AlreadyInRange && evaluation.MoveToTarget &&
               evaluation.EndpointLineOfSight && evaluation.Admissible
            ? 1
            : 2;
    }

    internal static int CompareCandidateRank(
        int leftTier,
        float leftDistanceSquared,
        int leftEntityId,
        int rightTier,
        float rightDistanceSquared,
        int rightEntityId)
    {
        int tierOrder = leftTier.CompareTo(rightTier);
        if (tierOrder != 0)
        {
            return tierOrder;
        }
        int distanceOrder = leftDistanceSquared.CompareTo(rightDistanceSquared);
        return distanceOrder != 0
            ? distanceOrder
            : leftEntityId.CompareTo(rightEntityId);
    }

    internal static string CandidateApproachCategory(int tier)
    {
        return tier == 0 ? "in_range_los" : "path_endpoint_los";
    }

    internal static bool CanRetryCandidate(
        int priorSelections,
        float targetDisplacementPixels)
    {
        return priorSelections > 0 &&
               priorSelections < MaximumSelectionsPerEntity &&
               targetDisplacementPixels >= CandidateRetryMovementPixels;
    }

    internal static bool ShouldRetainCurrentTarget(
        bool targetValid,
        bool attackable,
        float progressElapsedSeconds,
        float playerProgressPixels)
    {
        if (!targetValid)
        {
            return false;
        }
        return attackable || playerProgressPixels >= ProgressDistancePixels ||
               progressElapsedSeconds < ProgressWindowSeconds;
    }

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

    public static string SuccessLine(int checkpoints)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} PASS request=issued+succeeded server_fatal=observed " +
            "live_fatal=processed death_frames=0..9 fade=mid " +
            "removal=CombatWorld.Remove checkpoints={1}",
            Marker,
            Math.Max(0, checkpoints));
    }

    public static string FailureLine(ReferenceMonsterRealCombatTailExit exit)
    {
        return Marker + " FAIL code=" + FailureCode(exit);
    }

    public static string FailureCode(ReferenceMonsterRealCombatTailExit exit)
    {
        switch (exit)
        {
            case ReferenceMonsterRealCombatTailExit.Success:
                return "ok";
            case ReferenceMonsterRealCombatTailExit.FixtureUnavailable:
                return "fixture.unavailable";
            case ReferenceMonsterRealCombatTailExit.FixtureUnreachable:
                return "fixture.unreachable";
            case ReferenceMonsterRealCombatTailExit.RequestFailed:
                return "real.request_failed";
            case ReferenceMonsterRealCombatTailExit.FatalResultTimeout:
                return "real.fatal_result_timeout";
            case ReferenceMonsterRealCombatTailExit.FatalProjectionTimeout:
                return "real.fatal_projection_timeout";
            case ReferenceMonsterRealCombatTailExit.FatalProjectionMismatch:
                return "real.fatal_projection_mismatch";
            case ReferenceMonsterRealCombatTailExit.DeathLifecycleFailed:
                return "real.death_lifecycle_failed";
            case ReferenceMonsterRealCombatTailExit.RemovalFailed:
                return "real.removal_failed";
            case ReferenceMonsterRealCombatTailExit.Timeout:
                return "stage.timeout";
            default:
                return "client.unexpected";
        }
    }

    public static string ArtifactLine()
    {
        return Marker + " ARTIFACT trace=" + ArtifactFileName;
    }

    public static string PartialArtifactLine(
        ReferenceMonsterRealCombatTailExit exit,
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
        ReferenceMonsterRealCombatTailExit exit)
    {
        ReferenceMonsterRealCombatTailPartialArtifact partial =
            new ReferenceMonsterRealCombatTailPartialArtifact
            {
                failureCode = FailureCode(exit),
                phase = "BeforeDriver",
                stage = "client.bootstrap",
            };
        return WriteJsonAtomically(
            PartialArtifactFileName,
            JsonUtility.ToJson(partial, true) + "\n");
    }

    internal static bool WriteJsonAtomically(string fileName, string json)
    {
        string temporaryPath = string.Empty;
        try
        {
            string path = Path.GetFullPath(Path.Combine(
                UnityEngine.Application.dataPath,
                "../..",
                fileName));
            temporaryPath = path + ".tmp";
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
            File.WriteAllText(temporaryPath, json);
            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, null);
            }
            else
            {
                File.Move(temporaryPath, path);
            }
            return new FileInfo(path).Length > 0;
        }
        catch (Exception)
        {
            if (!string.IsNullOrEmpty(temporaryPath) && File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
            return false;
        }
    }
}

[Serializable]
internal sealed class ReferenceMonsterRealCombatTailArtifact
{
    public int schemaVersion = 1;
    public string source = "rebuilt-real-service";
    public int mapId;
    public int attackerEntityId;
    public int targetEntityId;
    public bool requestIssued;
    public bool requestSucceeded;
    public long requestIssuedDelta;
    public long requestSucceededDelta;
    public bool serverFatal;
    public int fatalAttackerEntityId;
    public int fatalTargetEntityId;
    public long fatalResultRevision;
    public int fatalTargetHitPoints;
    public bool fatalFlag;
    public bool liveFatalProjection;
    public int fatalProjectionTargetEntityId;
    public long processedFatalResultRevision;
    public bool fatalProjectionSourceOwnerLive;
    public bool fatalProjectionDying;
    public int fatalProjectionDeathFrame = -1;
    public bool fatalProjectionColliderEnabled;
    public bool fatalProjectionSelected;
    public bool fatalProjectionCombatContains;
    public bool fatalProjectionFrameContains;
    public List<int> deathFrameSequence = new List<int>();
    public bool deathTerminal;
    public int deathTerminalFrame = -1;
    public float deathTerminalAlpha;
    public bool deathMidFade;
    public int deathMidFadeFrame = -1;
    public float deathMidFadeAlpha;
    public long removeCallRevision;
    public int removeCallTargetEntityId;
    public int removeCallReason;
    public bool removeCallMonsterPresent;
    public bool removeCallSourceOwnerLive;
    public bool removeCallDying;
    public int removeCallDeathFrame;
    public float removeCallDeathAlpha;
    public bool removed;
    public bool finalFrameAbsent;
    public bool finalCombatWorldAbsent;
    public bool finalTargetSelectAbsent;
    public int candidateAttempts;
    public int retargetCount;
    public float initialTargetDistancePixels;
    public bool initialTargetLineOfSight;
    public List<int> excludedEntityIds = new List<int>();
    public List<ReferenceMonsterRealCombatTailCheckpoint> checkpoints =
        new List<ReferenceMonsterRealCombatTailCheckpoint>();
}

[Serializable]
internal sealed class ReferenceMonsterRealCombatTailCheckpoint
{
    public string stage = string.Empty;
    public int entityId;
    public int semanticTick;
    public float semanticMilliseconds;
    public float worldX;
    public float worldY;
    public int deathFrame;
    public float deathAlpha;
    public bool colliderEnabled;
    public bool selected;
    public bool dying;
    public bool sourceOwnerLive;
    public bool combatContains;
    public bool frameContains;
}

[Serializable]
internal sealed class ReferenceMonsterRealCombatTailDiagnostic
{
    public long windowTickCount;
    public string windowLastFailure = string.Empty;
    public string windowLastWorldRevisionFailure = string.Empty;
    public string lastBasicAttackFailureCode = string.Empty;
    public int basicAttackSelectedEntityId;
    public bool basicAttackAutoAttacking;
    public string basicAttackPhase = string.Empty;
    public float basicAttackPhaseSeconds;
    public bool playerAvailable;
    public float playerWorldX;
    public float playerWorldY;
    public bool playerMovementActive;
    public bool targetFrameContains;
    public bool targetCombatContains;
    public bool targetSourceOwnerLive;
    public float targetWorldX;
    public float targetWorldY;
    public int targetHitPoints;
    public bool targetDying;
    public bool targetColliderEnabled;
    public string targetSelectType = string.Empty;
    public int targetSelectEntityId;
    public float distance;
    public int attackRange;
    public bool inRange;
    public bool lineOfSight;
    public bool approachEvaluated;
    public bool approachAlreadyInRange;
    public string approachOutcome = string.Empty;
    public bool approachMoveToTarget;
    public int approachWaypointCount;
    public float approachFinalX;
    public float approachFinalY;
    public float approachFinalDistance;
    public bool approachCurrentLineOfSight;
    public bool approachEndpointLineOfSight;
    public bool approachAdmissible;
}

[Serializable]
internal sealed class ReferenceMonsterRealCombatTailAttemptEvidence
{
    public int entityId;
    public int selectionOrdinal;
    public int selectionGeneration;
    public float selectionTimeSeconds;
    public string initialApproachCategory = string.Empty;
    public float initialDistancePixels;
    public float retryMovementPixels;
    public string exclusionReason = string.Empty;
    public float elapsedSeconds;
    public float playerProgressPixels;
    public float targetDisplacementPixels;
}

[Serializable]
internal sealed class ReferenceMonsterRealCombatTailPartialArtifact
{
    public int schemaVersion = 3;
    public string source = "rebuilt-real-service-partial";
    public int fixtureMapId = ReferenceMonsterRealCombatTailCheck.FixtureMapId;
    public string fixtureMapFile =
        ReferenceMonsterRealCombatTailCheck.FixtureMapFile;
    public int fixtureSpawnId = ReferenceMonsterRealCombatTailCheck.FixtureSpawnId;
    public int fixtureMonsterDefinitionId =
        ReferenceMonsterRealCombatTailCheck.FixtureMonsterDefinitionId;
    public int fixtureMonsterModelId = ReferenceMonsterRealCombatTailCheck.ModelId;
    public int fixtureSpawnX = ReferenceMonsterRealCombatTailCheck.FixtureSpawnX;
    public int fixtureSpawnY = ReferenceMonsterRealCombatTailCheck.FixtureSpawnY;
    public int fixturePlayerX = ReferenceMonsterRealCombatTailCheck.FixturePlayerX;
    public int fixturePlayerY = ReferenceMonsterRealCombatTailCheck.FixturePlayerY;
    public float initialTargetDistancePixels;
    public bool initialTargetLineOfSight;
    public string failureCode = string.Empty;
    public string exceptionType = string.Empty;
    public string phase = string.Empty;
    public string stage = string.Empty;
    public int semanticTick;
    public float semanticMilliseconds;
    public int targetEntityId;
    public long requestIssuedBaseline;
    public long requestSucceededBaseline;
    public long requestIssuedCurrent;
    public long requestSucceededCurrent;
    public long requestIssuedDelta;
    public long requestSucceededDelta;
    public int candidateAttempts;
    public int retargetCount;
    public List<int> excludedEntityIds = new List<int>();
    public float candidateRescanIntervalSeconds =
        ReferenceMonsterRealCombatTailCheck.CandidateRescanIntervalSeconds;
    public float candidateWaitElapsedSeconds;
    public int maximumSelectionsPerEntity =
        ReferenceMonsterRealCombatTailCheck.MaximumSelectionsPerEntity;
    public float candidateRetryMovementPixels =
        ReferenceMonsterRealCombatTailCheck.CandidateRetryMovementPixels;
    public List<ReferenceMonsterRealCombatTailAttemptEvidence> attemptHistory =
        new List<ReferenceMonsterRealCombatTailAttemptEvidence>();
    public bool requestLocked;
    public bool currentAdmission;
    public float progressWindowSeconds =
        ReferenceMonsterRealCombatTailCheck.ProgressWindowSeconds;
    public float progressElapsedSeconds;
    public float progressAnchorX;
    public float progressAnchorY;
    public float progressCurrentX;
    public float progressCurrentY;
    public float progressDistance;
    public long pendingFatalResultRevision = 0L;
    public int pendingFatalTargetHitPoints = 0;
    public bool pendingFatalFlag = false;
    public int pendingFatalAttackerEntityId = 0;
    public long frameLastProcessedCombatResultRevision = 0L;
    public long frameLastCombatResultsVersion = 0L;
    public float fatalProjectionWaitElapsedSeconds = 0f;
    public long removalObservationRevisionBaseline = 0L;
    public long removalObservationRevisionCurrent = 0L;
    public bool matchingRemovalObserved = false;
    public int removalObservedReason = 0;
    public bool removalObservedMonsterPresent = false;
    public bool removalObservedSourceOwnerLive = false;
    public bool removalObservedDying = false;
    public int removalObservedDeathFrame = -1;
    public float removalObservedDeathAlpha = 0f;
    public List<ReferenceMonsterRealCombatTailCheckpoint> checkpoints =
        new List<ReferenceMonsterRealCombatTailCheckpoint>();
    public ReferenceMonsterRealCombatTailDiagnostic diagnostic =
        new ReferenceMonsterRealCombatTailDiagnostic();
}

internal sealed class ReferenceMonsterRealCombatTailDriver
{
    private struct Candidate
    {
        public Candidate(
            int entityId,
            int tier,
            float distanceSquared,
            bool currentLineOfSight,
            float targetX,
            float targetY,
            int selectionGeneration,
            float retryMovementPixels)
        {
            EntityId = entityId;
            Tier = tier;
            DistanceSquared = distanceSquared;
            CurrentLineOfSight = currentLineOfSight;
            TargetX = targetX;
            TargetY = targetY;
            SelectionGeneration = selectionGeneration;
            RetryMovementPixels = retryMovementPixels;
        }

        public int EntityId { get; }
        public int Tier { get; }
        public float DistanceSquared { get; }
        public bool CurrentLineOfSight { get; }
        public float TargetX { get; }
        public float TargetY { get; }
        public int SelectionGeneration { get; }
        public float RetryMovementPixels { get; }
    }

    private sealed class EntityAttemptState
    {
        public int SelectionCount;
        public bool Excluded;
        public float ExcludedTargetX;
        public float ExcludedTargetY;
    }

    private enum Phase
    {
        Selecting,
        WaitingForCandidate,
        WaitingForRequest,
        WaitingForFatalResult,
        WaitingForFatalProjection,
        WaitingForRemoval,
        Complete,
        Failed,
    }

    private readonly ClientSessionCoordinator session;
    private readonly ReferenceFirstWorldWindowOwner window;
    private readonly ReferenceMonsterRealCombatTailArtifact artifact =
        new ReferenceMonsterRealCombatTailArtifact();
    private readonly List<int> excludedEntityIds = new List<int>();
    private readonly Dictionary<int, EntityAttemptState> entityAttemptStates =
        new Dictionary<int, EntityAttemptState>();
    private readonly List<ReferenceMonsterRealCombatTailAttemptEvidence> attemptHistory =
        new List<ReferenceMonsterRealCombatTailAttemptEvidence>();
    private Phase phase = Phase.Selecting;
    private readonly float semanticStartTime;
    private readonly float deadline;
    private long resultRevisionBaseline;
    private long requestIssuedBaseline;
    private long requestSucceededBaseline;
    private readonly long removalObservationRevisionBaseline;
    private bool requestIssuanceObserved;
    private bool requestSuccessObserved;
    private int targetEntityId;
    private bool initialTargetEvidenceRecorded;
    private float initialTargetDistancePixels;
    private bool initialTargetLineOfSight;
    private int candidateAttempts;
    private int retargetCount;
    private int successfulSelections;
    private bool requestLocked;
    private bool currentAdmission;
    private int activeAttemptIndex = -1;
    private float activeAttemptPlayerStartX;
    private float activeAttemptPlayerStartY;
    private float activeAttemptTargetStartX;
    private float activeAttemptTargetStartY;
    private float activeAttemptLastTargetX;
    private float activeAttemptLastTargetY;
    private float candidateWaitStartedTime = -1f;
    private float candidateWaitElapsedSeconds;
    private float nextCandidateScanTime;
    private float progressStartTime;
    private float progressAnchorX;
    private float progressAnchorY;
    private float progressCurrentX;
    private float progressCurrentY;
    private float progressDistance;
    private float progressElapsedSeconds;
    private long pendingFatalResultRevision;
    private int pendingFatalTargetHitPoints;
    private bool pendingFatalFlag;
    private int pendingFatalAttackerEntityId;
    private float pendingFatalObservedTime;
    private float fatalProjectionWaitElapsedSeconds;
    private int lastDeathFrame = -1;
    private bool deathTerminalObserved;
    private bool deathMidFadeObserved;
    private int semanticTick;
    private bool failurePending;
    private ReferenceMonsterRealCombatTailExit pendingFailureExit;
    private string pendingFailurePhase = string.Empty;
    private string pendingFailureExceptionType = string.Empty;
    private bool pendingPartialWriteFailureReported;

    public ReferenceMonsterRealCombatTailDriver(
        ClientSessionCoordinator session,
        ReferenceFirstWorldWindowOwner window)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.window = window ?? throw new ArgumentNullException(nameof(window));
        artifact.mapId = window.MapDescriptor.id;
        semanticStartTime = Time.time;
        deadline = Time.realtimeSinceStartup +
                   ReferenceMonsterRealCombatTailCheck.TimeoutSeconds;
        resultRevisionBaseline = LatestResultRevision();
        requestIssuedBaseline = window.BasicAttackRequestIssuedCountForRealCombatTail;
        requestSucceededBaseline =
            window.BasicAttackRequestSucceededCountForRealCombatTail;
        removalObservationRevisionBaseline =
            CombatWorld.RemovalObservationRevisionForIndependentRuntime;
    }

    public bool Terminal => phase == Phase.Complete || phase == Phase.Failed;

    public ReferenceMonsterRealCombatTailExit Exit { get; private set; }

    public int CheckpointCount => artifact.checkpoints.Count;

    internal int CandidateAttemptsForGroundLoot => candidateAttempts;

    internal int RetargetCountForGroundLoot => retargetCount;

    internal float InitialTargetDistancePixelsForGroundLoot =>
        initialTargetDistancePixels;

    internal bool InitialTargetLineOfSightForGroundLoot =>
        initialTargetLineOfSight;

    public void Tick()
    {
        if (Terminal)
        {
            return;
        }
        if (failurePending)
        {
            TryFinishPendingFailure();
            return;
        }

        semanticTick++;
        try
        {
            if (Time.realtimeSinceStartup >= deadline)
            {
                Fail(phase == Phase.WaitingForFatalResult
                    ? ReferenceMonsterRealCombatTailExit.FatalResultTimeout
                    : ReferenceMonsterRealCombatTailExit.Timeout);
                return;
            }
            TickCore();
        }
        catch (Exception error)
        {
            Fail(
                ReferenceMonsterRealCombatTailExit.Unexpected,
                error.GetType().FullName);
        }
    }

    private void TickCore()
    {
        switch (phase)
        {
            case Phase.Selecting:
                if (!TryStartAttempt())
                {
                    WaitForCandidateOrFail();
                    return;
                }
                phase = Phase.WaitingForRequest;
                break;
            case Phase.WaitingForCandidate:
                TraceCandidateWait();
                break;
            case Phase.WaitingForRequest:
                TraceRequest();
                break;
            case Phase.WaitingForFatalResult:
                TraceFatalResult();
                break;
            case Phase.WaitingForFatalProjection:
                TraceFatalProjection();
                break;
            case Phase.WaitingForRemoval:
                TraceRemoval();
                break;
        }
    }

    private void TraceCandidateWait()
    {
        if (Time.time < nextCandidateScanTime)
        {
            return;
        }
        nextCandidateScanTime = Time.time +
            ReferenceMonsterRealCombatTailCheck.CandidateRescanIntervalSeconds;
        if (TryStartAttempt())
        {
            EndCandidateWait();
            phase = Phase.WaitingForRequest;
            return;
        }
        if (candidateAttempts >=
            ReferenceMonsterRealCombatTailCheck.MaximumCandidateAttempts ||
            !HasLiveHostileCandidate())
        {
            EndCandidateWait();
            Fail(candidateAttempts >=
                ReferenceMonsterRealCombatTailCheck.MaximumCandidateAttempts
                ? ReferenceMonsterRealCombatTailExit.FixtureUnreachable
                : ReferenceMonsterRealCombatTailExit.FixtureUnavailable);
        }
    }

    private void TraceRequest()
    {
        long issued = window.BasicAttackRequestIssuedCountForRealCombatTail;
        long succeeded = window.BasicAttackRequestSucceededCountForRealCombatTail;
        if (!requestIssuanceObserved && issued > requestIssuedBaseline)
        {
            requestIssuanceObserved = true;
            requestLocked = true;
            artifact.requestIssued = true;
        }

        if (requestIssuanceObserved && !requestSuccessObserved &&
            !string.IsNullOrEmpty(window.LastBasicAttackFailureCode))
        {
            Fail(ReferenceMonsterRealCombatTailExit.RequestFailed);
            return;
        }

        if (!requestIssuanceObserved && !MaintainPreRequestTarget())
        {
            return;
        }

        if (requestIssuanceObserved && !requestSuccessObserved &&
            succeeded > requestSucceededBaseline)
        {
            requestSuccessObserved = true;
            artifact.requestSucceeded = true;
            artifact.requestIssuedDelta = issued - requestIssuedBaseline;
            artifact.requestSucceededDelta = succeeded - requestSucceededBaseline;
            Record("real.requested");
            phase = Phase.WaitingForFatalResult;
            TraceFatalResult();
        }
    }

    private void TraceFatalResult()
    {
        IReadOnlyList<WorldCombatResultState> results = session.CombatResults;
        for (int index = 0; index < results.Count; index++)
        {
            WorldCombatResultState result = results[index];
            if (result == null || result.Revision <= resultRevisionBaseline)
            {
                continue;
            }
            resultRevisionBaseline = Math.Max(resultRevisionBaseline, result.Revision);
            if (result.TargetEntityId != targetEntityId ||
                result.AttackerEntityId != window.LocalEntityIdForRealCombatTail ||
                result.Missed || (!result.Fatal && result.TargetHitPoints > 0))
            {
                continue;
            }
            if (!requestSuccessObserved)
            {
                Fail(ReferenceMonsterRealCombatTailExit.RequestFailed);
                return;
            }

            pendingFatalResultRevision = result.Revision;
            pendingFatalTargetHitPoints = result.TargetHitPoints;
            pendingFatalFlag = result.Fatal;
            pendingFatalAttackerEntityId = result.AttackerEntityId;
            pendingFatalObservedTime = Time.time;
            fatalProjectionWaitElapsedSeconds = 0f;
            requestLocked = true;
            artifact.serverFatal = true;
            artifact.fatalAttackerEntityId = result.AttackerEntityId;
            artifact.fatalTargetEntityId = result.TargetEntityId;
            artifact.fatalResultRevision = result.Revision;
            artifact.fatalTargetHitPoints = result.TargetHitPoints;
            artifact.fatalFlag = result.Fatal;
            Record("real.result.fatal");
            phase = Phase.WaitingForFatalProjection;
            TraceFatalProjection();
            return;
        }
    }

    private void TraceFatalProjection()
    {
        fatalProjectionWaitElapsedSeconds = Mathf.Max(
            0f,
            Time.time - pendingFatalObservedTime);
        ReferenceFirstWorldInitialFrame frame = window.Frame;
        if (frame == null || pendingFatalResultRevision <= 0 ||
            pendingFatalAttackerEntityId != window.LocalEntityIdForRealCombatTail)
        {
            Fail(ReferenceMonsterRealCombatTailExit.FatalProjectionMismatch);
            return;
        }
        if (frame.LastCombatResultRevisionForRealCombatTail <
            pendingFatalResultRevision)
        {
            if (fatalProjectionWaitElapsedSeconds >=
                ReferenceMonsterRealCombatTailCheck.FatalProjectionTimeoutSeconds)
            {
                Fail(ReferenceMonsterRealCombatTailExit.FatalProjectionTimeout);
            }
            return;
        }

        ReferenceWorldMonsterActor actor;
        if (!frame.MonsterActors.TryGetValue(targetEntityId, out actor) ||
            actor == null || actor.State == null ||
            actor.State.EntityId != targetEntityId || !actor.SourceOwnerLive ||
            !actor.Dying || actor.CurrentFrame != 0 || actor.PickBox == null ||
            actor.PickBox.enabled || TargetSelect.Current != null ||
            !CombatWorld.ContainsMonsterForIndependentRuntime(targetEntityId))
        {
            Fail(ReferenceMonsterRealCombatTailExit.FatalProjectionMismatch);
            return;
        }

        artifact.liveFatalProjection = true;
        artifact.attackerEntityId = pendingFatalAttackerEntityId;
        artifact.targetEntityId = targetEntityId;
        artifact.fatalProjectionTargetEntityId = actor.State.EntityId;
        artifact.processedFatalResultRevision =
            frame.LastCombatResultRevisionForRealCombatTail;
        artifact.fatalProjectionSourceOwnerLive = actor.SourceOwnerLive;
        artifact.fatalProjectionDying = actor.Dying;
        artifact.fatalProjectionDeathFrame = actor.CurrentFrame;
        artifact.fatalProjectionColliderEnabled = actor.PickBox.enabled;
        artifact.fatalProjectionSelected =
            TargetSelect.Current == actor.SelectableOwner;
        artifact.fatalProjectionCombatContains =
            CombatWorld.ContainsMonsterForIndependentRuntime(targetEntityId);
        artifact.fatalProjectionFrameContains = true;
        Record("real.fatal");
        lastDeathFrame = 0;
        artifact.deathFrameSequence.Add(lastDeathFrame);
        Record("real.death.frame.0");
        phase = Phase.WaitingForRemoval;
    }

    private void TraceRemoval()
    {
        ReferenceWorldMonsterActor actor;
        if (window.Frame.MonsterActors.TryGetValue(targetEntityId, out actor))
        {
            if (actor == null || !actor.Dying || actor.PickBox == null ||
                actor.PickBox.enabled || TargetSelect.Current == actor.SelectableOwner)
            {
                Fail(ReferenceMonsterRealCombatTailExit.DeathLifecycleFailed);
                return;
            }
            if (actor.CurrentFrame != lastDeathFrame)
            {
                if (actor.CurrentFrame != lastDeathFrame + 1 ||
                    actor.CurrentFrame < 1 || actor.CurrentFrame > 9)
                {
                    Fail(ReferenceMonsterRealCombatTailExit.DeathLifecycleFailed);
                    return;
                }
                lastDeathFrame = actor.CurrentFrame;
                artifact.deathFrameSequence.Add(lastDeathFrame);
                Record("real.death.frame." + lastDeathFrame.ToString(
                    CultureInfo.InvariantCulture));
            }
            if (actor.CurrentFrame == 9 && !deathTerminalObserved)
            {
                deathTerminalObserved = true;
                artifact.deathTerminal = true;
                artifact.deathTerminalFrame = actor.CurrentFrame;
                artifact.deathTerminalAlpha = actor.DeathAlpha;
                Record("real.death.terminal");
            }
            if (actor.CurrentFrame == 9 && deathTerminalObserved &&
                !deathMidFadeObserved && actor.DeathAlpha > 0f &&
                actor.DeathAlpha <= 0.5f)
            {
                deathMidFadeObserved = true;
                artifact.deathMidFade = true;
                artifact.deathMidFadeFrame = actor.CurrentFrame;
                artifact.deathMidFadeAlpha = actor.DeathAlpha;
                Record("real.death.fade.mid");
            }
            return;
        }

        ReferenceCombatWorldRemovalObservation removal;
        bool matchingRemovalObserved =
            CombatWorld.TryGetRemovalObservationForIndependentRuntime(
                targetEntityId,
                out removal) &&
            removal.Revision > removalObservationRevisionBaseline &&
            removal.EntityId == targetEntityId && removal.Reason == 0 &&
            removal.MonsterWasPresent && removal.SourceOwnerLive &&
            removal.Dying && removal.DeathFrame == 9 &&
            removal.DeathAlpha <= 0.001f;
        if (!artifact.requestIssued || !artifact.requestSucceeded ||
            !artifact.serverFatal || !artifact.liveFatalProjection ||
            !artifact.deathTerminal || !artifact.deathMidFade ||
            artifact.deathFrameSequence.Count != 10 ||
            lastDeathFrame != 9 || TargetSelect.Current != null ||
            CombatWorld.ContainsMonsterForIndependentRuntime(targetEntityId) ||
            !matchingRemovalObserved)
        {
            Fail(ReferenceMonsterRealCombatTailExit.RemovalFailed);
            return;
        }

        artifact.removeCallRevision = removal.Revision;
        artifact.removeCallTargetEntityId = removal.EntityId;
        artifact.removeCallReason = removal.Reason;
        artifact.removeCallMonsterPresent = removal.MonsterWasPresent;
        artifact.removeCallSourceOwnerLive = removal.SourceOwnerLive;
        artifact.removeCallDying = removal.Dying;
        artifact.removeCallDeathFrame = removal.DeathFrame;
        artifact.removeCallDeathAlpha = removal.DeathAlpha;
        artifact.removed = true;
        artifact.finalFrameAbsent =
            !window.Frame.MonsterActors.ContainsKey(targetEntityId);
        artifact.finalCombatWorldAbsent =
            !CombatWorld.ContainsMonsterForIndependentRuntime(targetEntityId);
        artifact.finalTargetSelectAbsent = TargetSelect.Current == null;
        artifact.candidateAttempts = candidateAttempts;
        artifact.retargetCount = retargetCount;
        artifact.initialTargetDistancePixels = initialTargetDistancePixels;
        artifact.initialTargetLineOfSight = initialTargetLineOfSight;
        artifact.excludedEntityIds = new List<int>(excludedEntityIds);
        artifact.requestIssuedDelta =
            window.BasicAttackRequestIssuedCountForRealCombatTail -
            requestIssuedBaseline;
        artifact.requestSucceededDelta =
            window.BasicAttackRequestSucceededCountForRealCombatTail -
            requestSucceededBaseline;
        Record("real.removed");
        if (!WriteArtifact())
        {
            Fail(ReferenceMonsterRealCombatTailExit.RemovalFailed);
            return;
        }
        Exit = ReferenceMonsterRealCombatTailExit.Success;
        phase = Phase.Complete;
    }

    private bool TryStartAttempt()
    {
        while (candidateAttempts <
               ReferenceMonsterRealCombatTailCheck.MaximumCandidateAttempts)
        {
            Candidate candidate;
            if (!TryChooseCandidate(out candidate))
            {
                return false;
            }
            candidateAttempts++;
            targetEntityId = candidate.EntityId;
            BeginAttemptEvidence(candidate);
            currentAdmission = true;
            if (!window.TryBeginMonsterRealCombatTail(targetEntityId))
            {
                currentAdmission = false;
                ExcludeAttempt("selection_rejected");
                continue;
            }
            ResetProgress();
            if (successfulSelections > 0)
            {
                retargetCount++;
                Record("real.retarget." + retargetCount.ToString(
                    CultureInfo.InvariantCulture));
            }
            else
            {
                Record("real.selected");
            }
            successfulSelections++;
            return true;
        }
        return false;
    }

    private bool TryChooseCandidate(out Candidate selected)
    {
        selected = default(Candidate);
        if (window.Frame == null || window.Frame.Player == null)
        {
            return false;
        }
        float playerX = window.Frame.Player.WorldX;
        float playerY = window.Frame.Player.WorldY;
        List<Candidate> candidates = new List<Candidate>();
        foreach (KeyValuePair<int, ReferenceWorldMonsterActor> pair in
                 window.Frame.MonsterActors)
        {
            ReferenceWorldMonsterActor actor = pair.Value;
            int selectionGeneration;
            float retryMovementPixels;
            if (!IsLiveHostileCandidate(pair.Key, actor) ||
                !RetryGenerationEligible(
                    pair.Key,
                    actor,
                    out selectionGeneration,
                    out retryMovementPixels))
            {
                continue;
            }

            ReferenceWorldApproachEvaluation evaluation;
            if (!window.TryEvaluateMonsterRealCombatTailApproach(
                    pair.Key,
                    out evaluation))
            {
                continue;
            }
            int tier = ReferenceMonsterRealCombatTailCheck.CandidateTier(evaluation);
            if (tier >= 2)
            {
                continue;
            }
            float deltaX = actor.WorldX - playerX;
            float deltaY = actor.WorldY - playerY;
            candidates.Add(new Candidate(
                pair.Key,
                tier,
                deltaX * deltaX + deltaY * deltaY,
                evaluation.CurrentLineOfSight,
                actor.WorldX,
                actor.WorldY,
                selectionGeneration,
                retryMovementPixels));
        }
        candidates.Sort((left, right) =>
            ReferenceMonsterRealCombatTailCheck.CompareCandidateRank(
                left.Tier,
                left.DistanceSquared,
                left.EntityId,
                right.Tier,
                right.DistanceSquared,
                right.EntityId));
        if (candidates.Count == 0)
        {
            return false;
        }
        selected = candidates[0];
        return true;
    }

    private bool HasLiveHostileCandidate()
    {
        if (window.Frame == null)
        {
            return false;
        }
        foreach (KeyValuePair<int, ReferenceWorldMonsterActor> pair in
                 window.Frame.MonsterActors)
        {
            if (IsLiveHostileCandidate(pair.Key, pair.Value))
            {
                return true;
            }
        }
        return false;
    }

    private bool TryGetLiveHostileCandidate(
        int entityId,
        out ReferenceWorldMonsterActor actor)
    {
        actor = null;
        return window.Frame != null &&
               window.Frame.MonsterActors.TryGetValue(entityId, out actor) &&
               IsLiveHostileCandidate(entityId, actor);
    }

    private static bool IsLiveHostileCandidate(
        int entityId,
        ReferenceWorldMonsterActor actor)
    {
        WorldEntityState state = actor == null ? null : actor.State;
        return entityId > 0 && state != null && actor.SourceOwnerLive &&
               !actor.Dying &&
               state.ModelOrSkillId == ReferenceMonsterRealCombatTailCheck.ModelId &&
               state.FriendlyFlag == 0 && state.OwnerEntityId == 0 &&
               state.HitPointsOrRadiusPixels > 0;
    }

    private bool RetryGenerationEligible(
        int entityId,
        ReferenceWorldMonsterActor actor,
        out int selectionGeneration,
        out float retryMovementPixels)
    {
        selectionGeneration = 1;
        retryMovementPixels = 0f;
        EntityAttemptState state;
        if (!entityAttemptStates.TryGetValue(entityId, out state))
        {
            return true;
        }
        if (state.SelectionCount >=
            ReferenceMonsterRealCombatTailCheck.MaximumSelectionsPerEntity)
        {
            return false;
        }
        if (!state.Excluded)
        {
            selectionGeneration = state.SelectionCount + 1;
            return true;
        }
        float deltaX = actor.WorldX - state.ExcludedTargetX;
        float deltaY = actor.WorldY - state.ExcludedTargetY;
        retryMovementPixels = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
        selectionGeneration = state.SelectionCount + 1;
        return ReferenceMonsterRealCombatTailCheck.CanRetryCandidate(
            state.SelectionCount,
            retryMovementPixels);
    }

    private void WaitForCandidateOrFail()
    {
        if (candidateAttempts >=
            ReferenceMonsterRealCombatTailCheck.MaximumCandidateAttempts)
        {
            EndCandidateWait();
            Fail(ReferenceMonsterRealCombatTailExit.FixtureUnreachable);
            return;
        }
        if (!HasLiveHostileCandidate())
        {
            EndCandidateWait();
            Fail(ReferenceMonsterRealCombatTailExit.FixtureUnavailable);
            return;
        }
        if (phase != Phase.WaitingForCandidate)
        {
            candidateWaitStartedTime = Time.time;
            nextCandidateScanTime = Time.time +
                ReferenceMonsterRealCombatTailCheck.CandidateRescanIntervalSeconds;
            phase = Phase.WaitingForCandidate;
        }
    }

    private void EndCandidateWait()
    {
        if (candidateWaitStartedTime < 0f)
        {
            return;
        }
        candidateWaitElapsedSeconds += Mathf.Max(
            0f,
            Time.time - candidateWaitStartedTime);
        candidateWaitStartedTime = -1f;
    }

    private float CandidateWaitElapsedNow()
    {
        return candidateWaitElapsedSeconds + (candidateWaitStartedTime < 0f
            ? 0f
            : Mathf.Max(0f, Time.time - candidateWaitStartedTime));
    }

    private void BeginAttemptEvidence(Candidate candidate)
    {
        ReferenceWorldLocalPlayerDoll player = window.Frame == null
            ? null
            : window.Frame.Player;
        EntityAttemptState state;
        if (!entityAttemptStates.TryGetValue(candidate.EntityId, out state))
        {
            state = new EntityAttemptState();
            entityAttemptStates.Add(candidate.EntityId, state);
        }
        state.SelectionCount++;
        state.Excluded = false;

        activeAttemptPlayerStartX = player == null ? 0f : player.WorldX;
        activeAttemptPlayerStartY = player == null ? 0f : player.WorldY;
        activeAttemptTargetStartX = candidate.TargetX;
        activeAttemptTargetStartY = candidate.TargetY;
        activeAttemptLastTargetX = candidate.TargetX;
        activeAttemptLastTargetY = candidate.TargetY;
        if (!initialTargetEvidenceRecorded)
        {
            initialTargetEvidenceRecorded = true;
            initialTargetDistancePixels = Mathf.Sqrt(candidate.DistanceSquared);
            initialTargetLineOfSight = candidate.CurrentLineOfSight;
        }
        attemptHistory.Add(new ReferenceMonsterRealCombatTailAttemptEvidence
        {
            entityId = candidate.EntityId,
            selectionOrdinal = candidateAttempts,
            selectionGeneration = candidate.SelectionGeneration,
            selectionTimeSeconds = SemanticElapsedSeconds(),
            initialApproachCategory =
                ReferenceMonsterRealCombatTailCheck.CandidateApproachCategory(
                    candidate.Tier),
            initialDistancePixels = Mathf.Sqrt(candidate.DistanceSquared),
            retryMovementPixels = candidate.RetryMovementPixels,
        });
        activeAttemptIndex = attemptHistory.Count - 1;
    }

    private void UpdateActiveAttemptMetrics(
        ReferenceWorldLocalPlayerDoll player,
        ReferenceWorldMonsterActor actor)
    {
        if (activeAttemptIndex < 0 || activeAttemptIndex >= attemptHistory.Count)
        {
            return;
        }
        if (actor != null)
        {
            activeAttemptLastTargetX = actor.WorldX;
            activeAttemptLastTargetY = actor.WorldY;
        }
        ReferenceMonsterRealCombatTailAttemptEvidence evidence =
            attemptHistory[activeAttemptIndex];
        UpdateActiveAttemptEvidence(evidence, player);
    }

    private void UpdateActiveAttemptEvidence(
        ReferenceMonsterRealCombatTailAttemptEvidence evidence,
        ReferenceWorldLocalPlayerDoll player = null)
    {
        if (evidence == null)
        {
            return;
        }
        if (player == null && window.Frame != null)
        {
            player = window.Frame.Player;
        }
        float playerX = player == null ? activeAttemptPlayerStartX : player.WorldX;
        float playerY = player == null ? activeAttemptPlayerStartY : player.WorldY;
        float playerDeltaX = playerX - activeAttemptPlayerStartX;
        float playerDeltaY = playerY - activeAttemptPlayerStartY;
        float targetDeltaX = activeAttemptLastTargetX - activeAttemptTargetStartX;
        float targetDeltaY = activeAttemptLastTargetY - activeAttemptTargetStartY;
        evidence.elapsedSeconds = Mathf.Max(
            0f,
            SemanticElapsedSeconds() - evidence.selectionTimeSeconds);
        evidence.playerProgressPixels = Mathf.Sqrt(
            playerDeltaX * playerDeltaX + playerDeltaY * playerDeltaY);
        evidence.targetDisplacementPixels = Mathf.Sqrt(
            targetDeltaX * targetDeltaX + targetDeltaY * targetDeltaY);
    }

    private List<ReferenceMonsterRealCombatTailAttemptEvidence>
        AttemptHistorySnapshot()
    {
        ReferenceWorldLocalPlayerDoll player = window.Frame == null
            ? null
            : window.Frame.Player;
        ReferenceWorldMonsterActor actor;
        if (activeAttemptIndex >= 0 &&
            TryGetLiveHostileCandidate(targetEntityId, out actor))
        {
            UpdateActiveAttemptMetrics(player, actor);
        }
        else if (activeAttemptIndex >= 0)
        {
            UpdateActiveAttemptMetrics(player, null);
        }

        List<ReferenceMonsterRealCombatTailAttemptEvidence> result =
            new List<ReferenceMonsterRealCombatTailAttemptEvidence>(
                attemptHistory.Count);
        for (int index = 0; index < attemptHistory.Count; index++)
        {
            ReferenceMonsterRealCombatTailAttemptEvidence source =
                attemptHistory[index];
            result.Add(new ReferenceMonsterRealCombatTailAttemptEvidence
            {
                entityId = source.entityId,
                selectionOrdinal = source.selectionOrdinal,
                selectionGeneration = source.selectionGeneration,
                selectionTimeSeconds = source.selectionTimeSeconds,
                initialApproachCategory = source.initialApproachCategory,
                initialDistancePixels = source.initialDistancePixels,
                retryMovementPixels = source.retryMovementPixels,
                exclusionReason = source.exclusionReason,
                elapsedSeconds = source.elapsedSeconds,
                playerProgressPixels = source.playerProgressPixels,
                targetDisplacementPixels = source.targetDisplacementPixels,
            });
        }
        return result;
    }

    private float SemanticElapsedSeconds()
    {
        return Mathf.Max(0f, Time.time - semanticStartTime);
    }

    private bool MaintainPreRequestTarget()
    {
        ReferenceWorldLocalPlayerDoll player = window.Frame == null
            ? null
            : window.Frame.Player;
        if (player == null)
        {
            Fail(ReferenceMonsterRealCombatTailExit.FixtureUnavailable);
            return false;
        }

        ReferenceWorldMonsterActor actor;
        bool targetValid = TryGetLiveHostileCandidate(targetEntityId, out actor);
        UpdateActiveAttemptMetrics(player, actor);
        if (!targetValid)
        {
            currentAdmission = false;
            return RetargetBeforeRequest("target_invalid");
        }

        ReferenceWorldApproachEvaluation evaluation;
        bool evaluated = window.TryEvaluateMonsterRealCombatTailApproach(
            targetEntityId,
            out evaluation);
        currentAdmission = evaluated && evaluation.Admissible;
        progressCurrentX = player.WorldX;
        progressCurrentY = player.WorldY;
        float deltaX = progressCurrentX - progressAnchorX;
        float deltaY = progressCurrentY - progressAnchorY;
        progressDistance = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
        progressElapsedSeconds = Mathf.Max(0f, Time.time - progressStartTime);
        bool attackable = evaluated && evaluation.AlreadyInRange &&
            evaluation.CurrentLineOfSight;
        if (!ReferenceMonsterRealCombatTailCheck.ShouldRetainCurrentTarget(
                true,
                attackable,
                progressElapsedSeconds,
                progressDistance))
        {
            return RetargetBeforeRequest("no_progress");
        }
        if (attackable || progressDistance >=
            ReferenceMonsterRealCombatTailCheck.ProgressDistancePixels)
        {
            ResetProgress();
        }
        return true;
    }

    private bool RetargetBeforeRequest(string exclusionReason)
    {
        if (window.BasicAttackRequestIssuedCountForRealCombatTail >
            requestIssuedBaseline)
        {
            requestIssuanceObserved = true;
            requestLocked = true;
            artifact.requestIssued = true;
            return true;
        }
        ExcludeAttempt(exclusionReason);
        window.ClearMonsterRealCombatTailSelection();
        if (TryStartAttempt())
        {
            return false;
        }
        WaitForCandidateOrFail();
        return false;
    }

    private void ExcludeAttempt(string reason)
    {
        int entityId = targetEntityId;
        if (activeAttemptIndex >= 0 && activeAttemptIndex < attemptHistory.Count)
        {
            ReferenceMonsterRealCombatTailAttemptEvidence evidence =
                attemptHistory[activeAttemptIndex];
            evidence.exclusionReason = reason ?? string.Empty;
            UpdateActiveAttemptEvidence(evidence);
        }
        EntityAttemptState state;
        if (entityId > 0 && entityAttemptStates.TryGetValue(entityId, out state))
        {
            state.Excluded = true;
            state.ExcludedTargetX = activeAttemptLastTargetX;
            state.ExcludedTargetY = activeAttemptLastTargetY;
        }
        if (entityId > 0 && !excludedEntityIds.Contains(entityId))
        {
            excludedEntityIds.Add(entityId);
        }
        activeAttemptIndex = -1;
    }

    private void ResetProgress()
    {
        ReferenceWorldLocalPlayerDoll player = window.Frame == null
            ? null
            : window.Frame.Player;
        progressStartTime = Time.time;
        progressAnchorX = player == null ? 0f : player.WorldX;
        progressAnchorY = player == null ? 0f : player.WorldY;
        progressCurrentX = progressAnchorX;
        progressCurrentY = progressAnchorY;
        progressDistance = 0f;
        progressElapsedSeconds = 0f;
    }

    private void Record(string stage)
    {
        ReferenceWorldMonsterActor actor = null;
        if (window.Frame != null)
        {
            window.Frame.MonsterActors.TryGetValue(targetEntityId, out actor);
        }
        artifact.checkpoints.Add(new ReferenceMonsterRealCombatTailCheckpoint
        {
            stage = stage,
            entityId = targetEntityId,
            semanticTick = semanticTick,
            semanticMilliseconds = (Time.time - semanticStartTime) * 1000f,
            worldX = actor == null ? 0f : actor.WorldX,
            worldY = actor == null ? 0f : actor.WorldY,
            deathFrame = actor == null ? -1 : actor.CurrentFrame,
            deathAlpha = actor == null ? 0f : actor.DeathAlpha,
            colliderEnabled = actor != null && actor.PickBox != null &&
                actor.PickBox.enabled,
            selected = actor != null && TargetSelect.Current == actor.SelectableOwner,
            dying = actor != null && actor.Dying,
            sourceOwnerLive = actor != null && actor.SourceOwnerLive,
            combatContains = CombatWorld.ContainsMonsterForIndependentRuntime(
                targetEntityId),
            frameContains = actor != null,
        });
    }

    private long LatestResultRevision()
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

    private bool WriteArtifact()
    {
        return ReferenceMonsterRealCombatTailCheck.WriteJsonAtomically(
            ReferenceMonsterRealCombatTailCheck.ArtifactFileName,
            JsonUtility.ToJson(artifact, true) + "\n");
    }

    private bool WritePartialArtifact(
        ReferenceMonsterRealCombatTailExit exit,
        string failurePhase,
        string exceptionType)
    {
        long issuedCurrent = window == null
            ? 0
            : window.BasicAttackRequestIssuedCountForRealCombatTail;
        long succeededCurrent = window == null
            ? 0
            : window.BasicAttackRequestSucceededCountForRealCombatTail;
        ReferenceFirstWorldInitialFrame frame = window == null ? null : window.Frame;
        ReferenceCombatWorldRemovalObservation removal =
            default(ReferenceCombatWorldRemovalObservation);
        bool matchingRemovalObserved = targetEntityId > 0 &&
            CombatWorld.TryGetRemovalObservationForIndependentRuntime(
                targetEntityId,
                out removal) &&
            removal.Revision > removalObservationRevisionBaseline;
        ReferenceMonsterRealCombatTailPartialArtifact partial =
            new ReferenceMonsterRealCombatTailPartialArtifact
            {
                initialTargetDistancePixels = initialTargetDistancePixels,
                initialTargetLineOfSight = initialTargetLineOfSight,
                failureCode = ReferenceMonsterRealCombatTailCheck.FailureCode(exit),
                exceptionType = exceptionType ?? string.Empty,
                phase = failurePhase ?? string.Empty,
                stage = LastCheckpointStage(),
                semanticTick = semanticTick,
                semanticMilliseconds = (Time.time - semanticStartTime) * 1000f,
                targetEntityId = targetEntityId,
                requestIssuedBaseline = requestIssuedBaseline,
                requestSucceededBaseline = requestSucceededBaseline,
                requestIssuedCurrent = issuedCurrent,
                requestSucceededCurrent = succeededCurrent,
                requestIssuedDelta = issuedCurrent - requestIssuedBaseline,
                requestSucceededDelta = succeededCurrent - requestSucceededBaseline,
                candidateAttempts = candidateAttempts,
                retargetCount = retargetCount,
                excludedEntityIds = new List<int>(excludedEntityIds),
                candidateWaitElapsedSeconds = CandidateWaitElapsedNow(),
                attemptHistory = AttemptHistorySnapshot(),
                requestLocked = requestLocked || issuedCurrent > requestIssuedBaseline,
                currentAdmission = currentAdmission,
                progressElapsedSeconds = progressElapsedSeconds,
                progressAnchorX = progressAnchorX,
                progressAnchorY = progressAnchorY,
                progressCurrentX = progressCurrentX,
                progressCurrentY = progressCurrentY,
                progressDistance = progressDistance,
                pendingFatalResultRevision = pendingFatalResultRevision,
                pendingFatalTargetHitPoints = pendingFatalTargetHitPoints,
                pendingFatalFlag = pendingFatalFlag,
                pendingFatalAttackerEntityId = pendingFatalAttackerEntityId,
                frameLastProcessedCombatResultRevision = frame == null
                    ? 0
                    : frame.LastCombatResultRevisionForRealCombatTail,
                frameLastCombatResultsVersion = frame == null
                    ? 0
                    : Math.Max(0L, frame.LastCombatResultsVersion),
                fatalProjectionWaitElapsedSeconds = pendingFatalResultRevision <= 0
                    ? 0f
                    : Mathf.Max(
                        fatalProjectionWaitElapsedSeconds,
                        Mathf.Max(0f, Time.time - pendingFatalObservedTime)),
                removalObservationRevisionBaseline =
                    removalObservationRevisionBaseline,
                removalObservationRevisionCurrent = matchingRemovalObserved
                    ? removal.Revision
                    : CombatWorld.RemovalObservationRevisionForIndependentRuntime,
                matchingRemovalObserved = matchingRemovalObserved,
                removalObservedReason = matchingRemovalObserved ? removal.Reason : 0,
                removalObservedMonsterPresent =
                    matchingRemovalObserved && removal.MonsterWasPresent,
                removalObservedSourceOwnerLive =
                    matchingRemovalObserved && removal.SourceOwnerLive,
                removalObservedDying = matchingRemovalObserved && removal.Dying,
                removalObservedDeathFrame = matchingRemovalObserved
                    ? removal.DeathFrame
                    : -1,
                removalObservedDeathAlpha = matchingRemovalObserved
                    ? removal.DeathAlpha
                    : 0f,
                checkpoints = new List<ReferenceMonsterRealCombatTailCheckpoint>(
                    artifact.checkpoints),
            };
        try
        {
            if (window != null)
            {
                partial.diagnostic =
                    window.CaptureMonsterRealCombatTailDiagnostic(targetEntityId);
            }
        }
        catch (Exception)
        {
            partial.diagnostic = new ReferenceMonsterRealCombatTailDiagnostic();
        }
        return ReferenceMonsterRealCombatTailCheck.WriteJsonAtomically(
            ReferenceMonsterRealCombatTailCheck.PartialArtifactFileName,
            JsonUtility.ToJson(partial, true) + "\n");
    }

    private string LastCheckpointStage()
    {
        int count = artifact.checkpoints.Count;
        return count == 0 || artifact.checkpoints[count - 1] == null
            ? string.Empty
            : artifact.checkpoints[count - 1].stage ?? string.Empty;
    }

    private void Fail(
        ReferenceMonsterRealCombatTailExit exit,
        string exceptionType = "")
    {
        if (Terminal || failurePending)
        {
            return;
        }
        failurePending = true;
        pendingFailureExit = exit;
        pendingFailurePhase = phase.ToString();
        pendingFailureExceptionType = exceptionType ?? string.Empty;
        pendingPartialWriteFailureReported = false;
        Exit = exit;
        TryFinishPendingFailure();
    }

    private void TryFinishPendingFailure()
    {
        if (!failurePending || Terminal)
        {
            return;
        }
        bool partialWritten = false;
        try
        {
            partialWritten = WritePartialArtifact(
                pendingFailureExit,
                pendingFailurePhase,
                pendingFailureExceptionType);
        }
        catch (Exception)
        {
            partialWritten = false;
        }
        if (!partialWritten)
        {
            if (!pendingPartialWriteFailureReported)
            {
                pendingPartialWriteFailureReported = true;
                Debug.LogError(
                    ReferenceMonsterRealCombatTailCheck.Marker +
                    " PARTIAL_WRITE_FAILED path=" +
                    ReferenceMonsterRealCombatTailCheck.PartialArtifactFileName +
                    " phase=" + pendingFailurePhase);
            }
            return;
        }
        Debug.Log(
            ReferenceMonsterRealCombatTailCheck.PartialArtifactLine(
                pendingFailureExit,
                pendingFailurePhase,
                artifact.checkpoints.Count));
        failurePending = false;
        phase = Phase.Failed;
    }
}

}
