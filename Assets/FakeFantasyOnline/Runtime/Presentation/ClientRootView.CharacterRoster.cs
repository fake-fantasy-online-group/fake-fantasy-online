using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView
{
    internal void SetupNativeTitleRosterForIndependentRuntime()
    {
        RecoveredTitleCharacterSurface source = _titleCharacter;
        var title = source.NativeOwnerForIndependentRuntime;
        ClientSessionCoordinator session = _session;
        session.BindTitleCharacterRosterForIndependentRuntime(characters =>
        {
            _characters = characters;
            title.ReceiveRosterForIndependentRuntime(characters);
        });
        // Original OnDestroy clears +0x50 before releasing its avatars. Use the existing
        // actual title component's OnDestroy edge, never an InWorld/page flag or build result.
        source.BindRosterReceiptReleaseForIndependentRuntime(
            () => session.BindTitleCharacterRosterForIndependentRuntime(null));
    }

    private void ObserveTitleCharacterRosterResponse(IReadOnlyList<ServerCharacterRecord> characters,
        RecoveredTitleCharacterSurface source)
    {
        if (_session.UsesNativeCharacterRosterReceipts) return;
        // Preserve existing compatibility for a gateway without the receipt capability.
        _characters = characters;
        source?.BindRosterForIndependentRuntime(characters);
    }
}
}
