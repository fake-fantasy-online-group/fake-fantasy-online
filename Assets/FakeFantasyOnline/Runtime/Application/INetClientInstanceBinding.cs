using System;

namespace FakeFantasyOnline.Application
{
// The callback slots and entry admission all belong to one immutable transport instance.
public interface INetClientInstanceBinding : ITitleResultReceiptSource,
    ICharacterRosterReceiptSource, IWorldEntryPrefixReceiptSource, IWorldEntryAdmissionSource
{
    void BindNativeNetworkErrorForIndependentRuntime(Action<string> receive);
    void BindNativeNetworkNoticeForIndependentRuntime(Action<string> receive);
    void BindRegisterResultReceipt(Action<int, string> receive);
    void BindResetResultReceipt(Action<int, string> receive);
    void BindAccountResults(Action<int, string> register, Action<int, string> reset);
}
}
