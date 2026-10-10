using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Func<string> readTitleServerHost;
    private Func<int> readTitleServerPort;
    private Action requestTitleServerStatus;
    private Action<string> requestTitleEntry;
    private Action<Action<bool>> connectTitleLogin;
    private Action<string, string> sendTitleLogin;
    private Action clearTitleEntryNotice;
    private Action sendTitleEntryCaps;

    internal void BindTitleEntryPrefixForIndependentRuntime(Action clearNotice, Action sendCaps)
    {
        clearTitleEntryNotice = clearNotice;
        sendTitleEntryCaps = sendCaps;
    }

    internal void ClearTitleEntryNoticeForIndependentRuntime() => clearTitleEntryNotice();
    internal void SendTitleEntryCapsForIndependentRuntime() => sendTitleEntryCaps();

    internal void BindTitleLoginForIndependentRuntime(Action<Action<bool>> connect,
        Action<string, string> login)
    {
        connectTitleLogin = connect;
        sendTitleLogin = login;
    }

    internal void ConnectTitleLoginForIndependentRuntime(Action<bool> onDone) => connectTitleLogin(onDone);
    internal void SendTitleLoginForIndependentRuntime(string account, string password) =>
        sendTitleLogin(account, password);

    internal void BindTitleEntryForIndependentRuntime(Action<string> request) =>
        requestTitleEntry = request;

    // The original TitleFlow body owns admission and state; the existing Protocol
    // EnterGame/Send writer owns the connected check and exact outgoing frame.
    internal void RequestTitleEntryForIndependentRuntime(string name) => requestTitleEntry(name);

    internal void BindTitleServerConnectionForIndependentRuntime(Func<bool> connected,
        Func<string> host, Func<int> port, Action requestStatus)
    {
        BindConnectedReaderForIndependentRuntime(connected);
        readTitleServerHost = host;
        readTitleServerPort = port;
        requestTitleServerStatus = requestStatus;
    }

    internal string TitleServerHostForIndependentRuntime => readTitleServerHost();
    internal int TitleServerPortForIndependentRuntime => readTitleServerPort();
    // The caller controls Unity lifetime/Connected/cadence and re-reads I at this call site.
    // The existing bound adapter reaches the native void sender on this same host stack.
    internal void RequestTitleServerStatusForIndependentRuntime() => requestTitleServerStatus();
}
}
