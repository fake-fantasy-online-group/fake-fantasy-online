using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FakeFantasyOnline.PluginApi
{

public interface IPluginStorage
{
    Task<string> GetAsync(string key, CancellationToken cancellationToken);

    Task SetAsync(string key, string value, CancellationToken cancellationToken);
}

public interface IPluginContext
{
    string ApiVersion { get; }

    IReadOnlyCollection<string> GrantedCapabilities { get; }

    IPluginStateQueries State { get; }

    IPluginEventStream Events { get; }

    IPluginCommandGateway Commands { get; }

    IPluginUi Ui { get; }

    IPluginStorage Storage { get; }
}
}
