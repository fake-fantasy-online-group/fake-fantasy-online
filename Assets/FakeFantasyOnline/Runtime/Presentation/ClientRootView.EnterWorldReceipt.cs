using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView
{
    // Preserve DoEnterWorld's captured receiver through the synchronous construction attempt.
    private GameBootstrap _worldEntryBootstrap;

    internal void SetupNativeTitleEnterWorldForIndependentRuntime()
    {
        RecoveredTitleCharacterSurface source = _titleCharacter;
        ClientSessionCoordinator session = _session;
        session.BindTitleWorldEntryForIndependentRuntime(
            entry => ContinueTitleWorldEntry(source, entry),
            () => BeginTitleWorldEntry(source));
        source.BindEnterWorldReceiptReleaseForIndependentRuntime(
            () => session.BindTitleWorldEntryForIndependentRuntime(null));
    }

    private void BeginTitleWorldEntry(RecoveredTitleCharacterSurface source)
    {
        // Original 06000efd starts with entering=false. Its Unity-live NetClient arm
        // then re-reads I separately for the Notice clear and the caps call.
        source.CompleteEnterForIndependentRuntime();
        if (NetClient.I != null)
        {
            NetClient.I.ClearTitleEntryNoticeForIndependentRuntime();
            NetClient.I.SendTitleEntryCapsForIndependentRuntime();
        }
    }

    private void ContinueTitleWorldEntry(RecoveredTitleCharacterSurface source, WorldEntryRecord entry)
    {
        GameBootstrap boot = source.NativeOwnerForIndependentRuntime.boot;
        if (entry.MapId > 0)
        {
            if (ResLoader.I != null)
                MapDef.EnsureLoaded(ResLoader.I.Pkg);
            string mapName = GameBootstrap.MapSourceName(entry.MapId);
            // Native tests the raw reference after resolving the map, not Unity liveness.
            if (ReferenceEquals(boot, null)) throw new NullReferenceException();
            boot.mapName = mapName;
            boot.spawnX = entry.X;
            boot.spawnY = entry.Y;
        }
        GameBootstrap.CurMapId = Math.Max(0, entry.MapId);
        // Request destruction of this exact title, as the native entry does. Unity
        // dispatches its OnDestroy; BuildWorld remains on the current callback stack.
        UnityEngine.Object.Destroy(source.NativeOwnerForIndependentRuntime);
        // Nonpositive map IDs reach this first boot-null test only after title destruction.
        if (ReferenceEquals(boot, null)) throw new NullReferenceException();
        ReleaseFirstWorldWindow();
        _worldEntryBootstrap = boot;
        _world = entry;
        _worldBuildAttempted = false;
        _status = "World entry accepted by the server.";

        int viewportWidth = _backgroundSessionCheckRequested
            ? ReferenceBackgroundSessionCheck.ViewportWidth
            : Screen.width;
        int viewportHeight = _backgroundSessionCheckRequested
            ? ReferenceBackgroundSessionCheck.ViewportHeight
            : Screen.height;
        // The real builder is synchronous. An admitted receipt must reach its normal return
        // (including a native early return) before the live callback and inbox can continue.
        // Raw Screen dimensions do not defer the original construction prefix. Native
        // CameraFollow observes its own viewport later, through Unity's real callback.
        BuildTitleWorldEntry(viewportWidth, viewportHeight);
    }

    private bool BuildTitleWorldEntry(int viewportWidth, int viewportHeight)
    {
        _worldBuildAttempted = true;
        GameBootstrap boot = _worldEntryBootstrap;
        if (!boot.ConfigureWorld(
                _session,
                transform,
                _world,
                _music,
                _combatSound,
                _processNpcChatterNames,
                viewportWidth,
                viewportHeight,
                ReferenceWorldCameraFollow.DefaultDpiScale))
        {
            _status = WorldFailureStatus(
                ReferenceFirstWorldWindowFailure.UnexpectedConstructionFailure);
            if (_backgroundSessionCheckRequested)
            {
                FinishBackgroundSessionCheck(
                    ReferenceBackgroundSessionCheckExit.WorldOwnerFailed);
            }
            return false;
        }

        boot.BuildWorld();
        // Complete after a normal attempt, including BuildWorld's early-return paths.
        // Never put this in finally or gate it on the candidate's success.
        _session.CompleteWorldEntryBuildForIndependentRuntime(_world);
        ReferenceFirstWorldWindowOwner candidate = boot.Window;
        if (candidate == null)
        {
            ReferenceFirstWorldWindowFailure failure =
                boot.LastBuildFailure;
            _status = WorldFailureStatus(failure);
            if (_backgroundSessionCheckRequested)
            {
                Debug.LogError(
                    ReferenceBackgroundSessionCheck.OwnerFailureLine(
                        failure));
            }
            if (_backgroundSessionCheckRequested)
            {
                FinishBackgroundSessionCheck(
                    ReferenceBackgroundSessionCheckExit.WorldOwnerFailed);
            }
            return false;
        }

        _firstWorldWindow = candidate;
        if (_backgroundSessionCheckRequested)
        {
            // The separate readiness budget measures the authoritative startup stream after
            // the production world owner is ready; Metal map construction is not server wait.
            _backgroundWorldReadinessDeadline =
                Time.realtimeSinceStartup +
                ReferenceBackgroundSessionCheck.WorldReadinessTimeoutSeconds;
        }
        IReadOnlyList<WorldTeleportState> teleports = _session.TeleportStates;
        _observedTeleportSequence = teleports.Count == 0
            ? 0
            : teleports[teleports.Count - 1].Sequence;
        _lastReportedWorldRevisionFailure = ReferenceWorldRevisionFailure.None;
        _status = string.Empty;
        return true;
    }
}
}
