using System;

namespace FakeFantasyOnline.Application
{
public sealed partial class ClientSessionCoordinator
{
    // Receipt publication stays authoritative after the title slots have been cleared;
    // late successful Task completions must not regain page/status publication ownership.
    public bool UsesNativeTitleResultReceipts { get; private set; }

    public void BindTitleLoginResultForIndependentRuntime(
        Action<int, string> receive, ITitleResultReceiptSource receiver = null)
    {
        ITitleResultReceiptSource source = receiver ?? (_gateway as ITitleResultReceiptSource);
        if (source == null) return;
        UsesNativeTitleResultReceipts = true;
        if (receive == null)
        {
            source.BindLoginResultReceipt(null);
            return;
        }
        source.BindLoginResultReceipt((code, message) =>
        {
            receive(code, message);
            if (code == 0) TransitionTo(ClientSessionState.ServerSelection, string.Empty);
        });
    }

    public void BindTitleCreateResultForIndependentRuntime(
        Action<int, string> receive, ITitleResultReceiptSource receiver = null)
    {
        ITitleResultReceiptSource source = receiver ?? (_gateway as ITitleResultReceiptSource);
        if (source == null) return;
        UsesNativeTitleResultReceipts = true;
        if (receive == null)
        {
            source.BindCreateResultReceipt(null);
            return;
        }
        source.BindCreateResultReceipt((code, message) =>
        {
            receive(code, message);
            if (code == 0) TransitionTo(ClientSessionState.CharacterSelection, string.Empty);
        });
    }
}
}
