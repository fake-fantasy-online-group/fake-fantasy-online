namespace FakeFantasyOnline.Application
{
public sealed partial class ServerEndpoint
{
    private ServerEndpoint(string host, int port, bool nativeAddress)
    {
        Host = host;
        Port = port;
    }

    // The native resolver already owns its exact first-row gate (including no upper port
    // bound). This data projection must not introduce a second gate on any directory row.
    public static ServerEndpoint FromNativeAddressForIndependentRuntime(string host, int port) =>
        new ServerEndpoint(host, port, true);
}

public sealed partial class ServerDirectoryEntry
{
    private ServerDirectoryEntry(string displayName, ServerEndpoint endpoint, string zoneName,
        bool isPlayerVersusPlayer, int busyLevel, bool nativeAddress)
    {
        DisplayName = displayName;
        Endpoint = endpoint;
        ZoneName = zoneName;
        IsPlayerVersusPlayer = isPlayerVersusPlayer;
        BusyLevel = busyLevel;
    }

    public static ServerDirectoryEntry FromNativeAddressForIndependentRuntime(
        string displayName, string host, int port, string zoneName,
        bool isPlayerVersusPlayer, int busyLevel) =>
        new ServerDirectoryEntry(displayName,
            ServerEndpoint.FromNativeAddressForIndependentRuntime(host, port), zoneName,
            isPlayerVersusPlayer, busyLevel, true);
}
}
