using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{

public sealed partial class ClientRootView
{
    private async void BeginReturnFromWorld(bool characterSelection, bool nativeWorldReturn)
    {
        try
        {
            TitleFlow title = RebuildRecoveredTitleSurfaces();

            if (nativeWorldReturn)
            {
                // The native local world never owned a server world/roster wait. Send
                // through the original writer (including its disconnected gate), then
                // choose the requested title page without fabricating a response.
                _sendNativeWorldReturnForIndependentRuntime();
                title.SelectTitlePageForIndependentRuntime(characterSelection ? 5 : 4);
                _session.SelectTitleAfterNativeWorldReturnForIndependentRuntime(characterSelection);
                return;
            }

            var returnTask = _session.ReturnFromWorldAsync(characterSelection, _lifetime.Token);
            title.SelectTitlePageForIndependentRuntime(characterSelection ? 5 : 4);
            Result<IReadOnlyList<ServerCharacterRecord>> result = await returnTask;
            if (_lifetime == null || _lifetime.IsCancellationRequested)
                return;
            if (result.IsSuccess)
                ObserveTitleCharacterRosterResponse(result.Value, _titleCharacter);
            if (_session.State == ClientSessionState.CharacterSelection ||
                _session.State == ClientSessionState.ServerSelection ||
                _session.State == ClientSessionState.CharacterCreation)
            {
                // A newer live title result may have arrived during this roster wait.
                if (!result.IsSuccess || !_session.UsesNativeTitleResultReceipts)
                    _status = result.IsSuccess ? string.Empty : SafeError(result.Error);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            _status = "client.unexpected: World return failed unexpectedly.";
        }
    }

    // Official 1.5.8 never reuses the title component across a world return: both
    // GameBootstrap.ReturnToTitle (0x060003f2) and GameBootstrap.DisconnectToLogin
    // (0x060003f1) add a fresh TitleFlow (WBC-086, dual-arch AddComponent evidence).
    // DisconnectToLogin does not remove a different title still attached to the host.
    // TitleFlow..ctor (0x06000efe) then re-initialises account
    // (+0x1d8) and password (+0x1e0) to the empty-string literal (usage 10335;
    // arm64 :96-99, x86_64 :102-105) before the base ctor runs (arm64 :131,
    // x86_64 :137). The two host fields are passed to DrawLogin by reference each
    // frame, so this rebuild site is the only place that stands in for those ctor
    // writes; it runs once per world return, never per frame, and introduces no cache,
    // no global and no reset inside Draw. Nothing here touches the remember flags or
    // the process-level pref latch: because that latch is already true after the first
    // title load, the new surface does not re-read PlayerPrefs, so both boxes reopen
    // empty — which is the ctor behaviour the two fields are being released to mirror.
    // ClientRootView.OnDestroy releases its adapters without rebuilding, so no mirror is
    // needed there.
    private TitleFlow RebuildRecoveredTitleSurfaces(bool retireExistingTitle = true)
    {
        _firstWorldWindow = null;
        _world = null;
        _worldEntryBootstrap = null;
        _worldBuildAttempted = false;
        _lastReportedWorldRevisionFailure = ReferenceWorldRevisionFailure.None;
        _characters = Array.Empty<ServerCharacterRecord>();
        _creatingCharacter = false;
        // New title construction supplies fresh zero-initialized login state. Retire only
        // the existing host diagnostic route here, without writing the old title's stamp.
        _loginDiagnosticsActive = false;

        if (retireExistingTitle)
        {
            _titleLogin?.ReleaseGeneratedLoginTexturesForIndependentRuntime();
            TitleFlow previousTitle = _gameBootstrap.gameObject.GetComponent<TitleFlow>();
            if (previousTitle != null)
                Destroy(previousTitle);
            _titleCharacterCreation?.Dispose();
        }

        // The actual new TitleFlow now owns the original empty credential/message
        // initializers; do not clear fields on the retired component before creation.
        TitleFlow title = CreateTitleResourcesForIndependentRuntime(() =>
        {
            if (MusicManager.I != null)
                MusicManager.I.PlayLogin();
            _setupTitleNetworkForIndependentRuntime();
        });
        BindTitleBootstrapForIndependentRuntime(title);
        return title;
    }

    // GameBootstrap.DisconnectToLogin (0x060003f1), the single System.Action target of both slots
    // in NoteDisconnectedForIndependentRuntime. Frozen dual-arch bodies
    // Build/ReferenceReverseGhidra-1.5.8/queries/wb169-
    // disconnecttologin-{arm64,x86_64}/060003f1.c (arm64 0x4267cc / 140B, x86_64 0x43bb20 /
    // 128B) are three steps with no branch that returns: TeardownWorldUi (0x060003f3), then
    // GameObject.AddComponent<TitleFlow>() on this gameObject (usage 07690 MethodRef[45033]),
    // then TitleFlow.NoteDisconnected (0x06000ec0) on the new instance. gameObject null or
    // AddComponent null falls into the non-returning null helper (arm64 :35 / x86_64 :38), an
    // exception, not a skip; the product can only reach this callback on the box it owns, and the
    // host objects already exist. Rebuild finishes the new title's Awake/SetupNet before storing
    // this host's GameBootstrap in its boot field (+0x20, arm64 :29-30 / x86_64 :27-28).
    // The successful entry consumes that exact owner for map/spawn and the deferred build.
    // Order is kept: world teardown, fresh title and boot assignment, then the note.
    private void BeginDisconnectToLoginFromWorld()
    {
        // 060003f1 always enters 060003f3, even if the prompt outlives its world.
        // The general host release helper can skip that native teardown when empty.
        _gameBootstrap.ReleaseWorld();
        // 060003f1 adds its fresh title without retiring another component on this host.
        // The native root purge preserves the host and any title still attached to it.
        TitleFlow title = RebuildRecoveredTitleSurfaces(retireExistingTitle: false);
        // Project the unconditional Login store only after this callback has rebuilt the title.
        // The earlier world-root branch itself keeps the title silent until this click.
        _session?.NoteTransportDisconnectedFromWorld();
        ApplyDisconnectedNote(title);
    }
}
}
