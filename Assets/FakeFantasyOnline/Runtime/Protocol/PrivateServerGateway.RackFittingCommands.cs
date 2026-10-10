using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway : IRackFittingCommands
    {
        public Task<Result> RequestFittingCatalogAsync(CancellationToken cancellationToken) =>
            SendRack(() => _netClient.FittingCatalog(), cancellationToken);

        public Task<Result> RequestFittingPageAsync(byte kind, string part, string search, int page,
            int perPage, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.FittingCatalogPage(kind, part, search, page, perPage);
                return Task.FromResult(Result.Success());
            }

            return SendRack(() => _netClient.FittingCatalogPage(kind, part, search, page, perPage), cancellationToken);
        }

        public Task<Result> RequestFittingDescriptionsAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled && _host.IsHostThread && (ids == null || ids is List<int>))
            {
                _netClient.FittingDesc((List<int>)ids);
                return Task.FromResult(Result.Success());
            }

            // Native Send serializes immediately. Preserve that call-time list across the async send lock.
            List<int> snapshot = ids == null ? null : new List<int>(ids);
            return SendRack(() => _netClient.FittingDesc(snapshot), cancellationToken);
        }
    }
}
