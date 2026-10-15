using System;

namespace FakeFantasyOnline.Application
{
// The callback slots and entry admission all belong to one immutable transport instance.
public interface INetClientInstanceBinding : ITitleResultReceiptSource,
    ICharacterRosterReceiptSource, IWorldEntryPrefixReceiptSource, IWorldEntryAdmissionSource
{
    // Dismissal carries the exact locally owned retry string, never a blanket error reset.
    // Replacing/clearing the error receiver also replaces/clears this optional callback.
    void BindNativeNetworkErrorForIndependentRuntime(Action<string> receive,
        Action<string> clearAddressRetryNotice = null);
    void BindNativeNetworkNoticeForIndependentRuntime(Action<string> receive);
    void BindRegisterResultReceipt(Action<int, string> receive);
    void BindResetResultReceipt(Action<int, string> receive);
    void BindAccountResults(Action<int, string> register, Action<int, string> reset);
}
}
