using System;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView
{
    private Action<Action<string>> _bindTitleError;

    // The shared factory has already completed SetupNet's Unity-null creation arm.
    // Re-read I once here, then keep that exact receiver for all eight assignments.
    internal void SetupNativeTitleNetworkForIndependentRuntime()
    {
        NetClient receiver = NetClient.I;
        INetClientInstanceBinding binding = receiver.InstanceBindingForIndependentRuntime;
        if (binding == null)
            throw new InvalidOperationException("The current NetClient has no bound protocol owner.");
        RecoveredTitleCharacterSurface source = _titleCharacter;
        TitleFlow title = source.NativeOwnerForIndependentRuntime;
        ClientSessionCoordinator session = _session;

        binding.BindNativeNetworkErrorForIndependentRuntime(title.ReceiveNativeNetworkError);
        binding.BindNativeNetworkNoticeForIndependentRuntime(title.ReceiveNativeNetworkNotice);

        session.BindTitleLoginResultForIndependentRuntime(title.ReceiveNativeTitleLoginResult, binding);
        session.BindTitleCharacterRosterForIndependentRuntime(characters =>
        {
            _characters = characters;
            title.ReceiveRosterForIndependentRuntime(characters);
        }, binding);
        session.BindTitleCreateResultForIndependentRuntime(title.ReceiveNativeTitleCreateResult, binding);

        session.BindTitleWorldEntryForIndependentRuntime(
            entry => ContinueTitleWorldEntry(source, entry),
            () => BeginTitleWorldEntry(source), binding, binding);
        binding.BindRegisterResultReceipt(title.ReceiveRegisterResult);
        binding.BindResetResultReceipt(title.ReceiveResetResult);
    }

    internal void BindNativeTitleErrorForIndependentRuntime(Action<Action<string>> bindError)
    {
        _bindTitleError = bindError ?? throw new ArgumentNullException(nameof(bindError));
        SetupNativeTitleErrorForIndependentRuntime();
    }

    private void SetupNativeTitleErrorForIndependentRuntime()
    {
        Action<Action<string>> bindError = _bindTitleError;
        var title = _titleCharacter.NativeOwnerForIndependentRuntime;
        bindError(title.ReceiveNativeNetworkError);
        _titleCharacter.BindErrorReceiptReleaseForIndependentRuntime(() => bindError(null));
    }

    private void BindNativeTitleAccountResultsReleaseForIndependentRuntime()
    {
        ClientSessionCoordinator session = _session;
        _titleCharacter.BindAccountResultsReceiptReleaseForIndependentRuntime(
            () => session.BindAccountResults(null, null));
    }

    internal void SetupNativeTitleAccountResultsForIndependentRuntime()
    {
        _session.BindAccountResults(_titleLogin.ReceiveRegisterResult, _titleLogin.ReceiveResetResult);
        BindNativeTitleAccountResultsReleaseForIndependentRuntime();
    }
}
}
