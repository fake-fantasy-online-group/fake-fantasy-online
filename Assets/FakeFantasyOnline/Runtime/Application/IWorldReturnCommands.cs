using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
public interface IWorldReturnCommands
{
    Task<Result<IReadOnlyList<ServerCharacterRecord>>> LeaveWorldForTitleAsync(CancellationToken cancellationToken);
}
}
