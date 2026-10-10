using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
public sealed partial class ClientSessionCoordinator
{
    // Stays true after native title destruction: a late request completion must not regain
    // publication authority merely because OnCharList has just been cleared.
    public bool UsesNativeCharacterRosterReceipts { get; private set; }

    public void BindTitleCharacterRosterForIndependentRuntime(
        Action<IReadOnlyList<ServerCharacterRecord>> receive,
        ICharacterRosterReceiptSource receiver = null)
    {
        ICharacterRosterReceiptSource source = receiver ?? (_gateway as ICharacterRosterReceiptSource);
        if (source == null) return;
        UsesNativeCharacterRosterReceipts = true;
        if (receive == null)
        {
            source.BindCharacterRosterReceipt(null);
            return;
        }

        source.BindCharacterRosterReceipt(records =>
        {
            _characters = MapRoster(records);
            receive(_characters);
        });
    }

    private IReadOnlyList<ServerCharacterRecord> ObserveCharacterRosterResponse(
        IReadOnlyList<ServerCharacterRecord> records)
    {
        // Preserve the request's own returned data, including non-receipt gateway consumers.
        // A production title's current roster was already published at receipt, and a later
        // packet may already have replaced it before this await continuation runs.
        IReadOnlyList<ServerCharacterRecord> response = MapRoster(records);
        if (!UsesNativeCharacterRosterReceipts) _characters = response;
        return response;
    }
}
}
