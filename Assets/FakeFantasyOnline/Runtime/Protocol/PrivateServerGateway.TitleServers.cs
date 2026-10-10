using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : ITitleServerEndpointPort
{
    public string TitleServerHost => _netClient.host;
    public int TitleServerPort => _netClient.port;
    public int TitleServerCount => _netClient.ServersForIndependentRuntime?.Length ?? 0;

    // Read the already-published resolver array only when the original tree rebuilds. The
    // existing DTO projection remains lossless for its consumer; no discovery or cache starts.
    public IReadOnlyList<ServerSelectionItem> ReadTitleServers()
    {
        ClientConfig.Srv[] source = _netClient.ServersForIndependentRuntime;
        if (source == null) return null;
        var rows = new ServerSelectionItem[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            ClientConfig.Srv row = source[i];
            rows[i] = row == null ? null : new ServerSelectionItem(
                i, row.name, row.zone, row.pvp, row.busy, row.host, row.port);
        }
        return rows;
    }

    // Called only by the existing endpoint-switch bridge after Close/Dispose and before Login.
    // These are the same fields EstablishConnection consumes; no connect or send is initiated.
    public void SetTitleServerEndpoint(string host, int port)
    {
        _netClient.host = host;
        _netClient.port = port;
    }
}
}
