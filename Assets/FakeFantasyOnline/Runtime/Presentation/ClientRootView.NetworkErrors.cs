using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView
{
    private Action<Action<string>> _bindTitleNotice;

    // The composition root supplies the existing protocol owner's mutable slot. Initial
    // setup follows its OnError assignment; a rebuilt title installs its callback again.
    internal void BindNativeTitleNoticeForIndependentRuntime(Action<Action<string>> bindNotice)
    {
        _bindTitleNotice = bindNotice ?? throw new ArgumentNullException(nameof(bindNotice));
        SetupNativeTitleNoticeForIndependentRuntime();
    }

    private void SetupNativeTitleNoticeForIndependentRuntime()
    {
        // Capture the actual title when the slot is assigned. Notice intentionally
        // survives that component's destruction; a later setup replaces the slot.
        TitleFlow title = _titleCharacter.NativeOwnerForIndependentRuntime;
        _bindTitleNotice?.Invoke(title.ReceiveNativeNetworkNotice);
    }
}
}
