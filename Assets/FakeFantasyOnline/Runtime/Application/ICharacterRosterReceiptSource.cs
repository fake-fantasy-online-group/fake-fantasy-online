using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
// The existing host-thread receipt owns publication; binding never replays an old roster.
public interface ICharacterRosterReceiptSource
{
    void BindCharacterRosterReceipt(Action<IReadOnlyList<ServerCharacterRecord>> receive);
}
}
