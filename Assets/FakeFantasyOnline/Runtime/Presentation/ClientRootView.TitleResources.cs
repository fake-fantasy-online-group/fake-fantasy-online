using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView
{
    // This is the synchronous argument supplied to the next active-host AddComponent.
    // The newly attached component captures it during its real Awake; it is not a
    // lifetime/completion flag and does not schedule or replay any initialization.
    private Action _titleAwakeTailForIndependentRuntime;
    private Action _setupTitleNetworkForIndependentRuntime;
    private Action _sendNativeWorldReturnForIndependentRuntime;

    internal void BindTitleNetworkSetupForIndependentRuntime(Action setupNet, Action sendWorldReturn)
    {
        _setupTitleNetworkForIndependentRuntime = setupNet ?? throw new ArgumentNullException(nameof(setupNet));
        _sendNativeWorldReturnForIndependentRuntime = sendWorldReturn ?? throw new ArgumentNullException(nameof(sendWorldReturn));
    }

    internal TitleFlow InitializeTitleForIndependentRuntime()
    {
        _music = MusicManager.I;
        return CreateTitleResourcesForIndependentRuntime(() =>
        {
            using (ReferenceResourceLoader resources =
                ReferenceResourceLoader.OpenStreamingAssets())
            {
                if (!_music.ConfigureIndependentRuntime(resources.RootDirectory) ||
                    !_music.TryPlayLogin())
                {
                    _status =
                        "title.music.fail_closed: The official 1.5.8 login track " +
                        "or source-owned music layout is unavailable.";
                }
            }

            if (_audioSourceCheckRequested)
            {
                _audioStartDeadline = Time.realtimeSinceStartup +
                    ReferenceAudioSourceCheck.StartTimeoutSeconds;
            }
            else if (_backgroundSessionCheckRequested)
            {
                BeginBackgroundSessionCheck();
            }
            _setupTitleNetworkForIndependentRuntime();
        });
    }

    internal void BindTitleBootstrapForIndependentRuntime(TitleFlow title)
    {
        // Keep the exact AddComponent result: Awake may fail after publishing only
        // a prefix, while Unity still owns the attached component and this return.
        title.boot = _gameBootstrap;
    }

    private TitleFlow CreateTitleResourcesForIndependentRuntime(Action completeAwake)
    {
        _titleAwakeTailForIndependentRuntime = completeAwake;
        return _gameBootstrap.gameObject.AddComponent<TitleFlow>();
    }

    internal Action PublishTitleOwnerForIndependentRuntime(TitleFlow owner)
    {
        // Current composition acquires GameBootstrap from this same GameObject.
        // The actual component is published by Unity before these adapter shells.
        _titleCharacterCreation = new RecoveredTitleCharacterCreationSurface();
        owner.BindHostForIndependentRuntime(_titleCharacterCreation.Reset,
            PerformCharacterSelectionAction, message => owner.netMsg = message);
        _titleLogin = owner;
        owner.BindLoginInputForIndependentRuntime(this, _noticeUri, _titleCharacterCreation);
        _titleCharacter = new RecoveredTitleCharacterSurface(owner);
        return _titleAwakeTailForIndependentRuntime;
    }
}
}
