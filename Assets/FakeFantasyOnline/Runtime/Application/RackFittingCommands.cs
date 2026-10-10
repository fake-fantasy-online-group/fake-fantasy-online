using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
    public interface IRackFittingCommands
    {
        Task<Result> RequestFittingCatalogAsync(CancellationToken cancellationToken);
        Task<Result> RequestFittingPageAsync(byte kind, string part, string search, int page, int perPage, CancellationToken cancellationToken);
        Task<Result> RequestFittingDescriptionsAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken);
    }
}
