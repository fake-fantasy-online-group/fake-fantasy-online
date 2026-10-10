using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
public interface ICharacterReviveCommands
{
    Task<Result> ReviveCharacterAsync(byte option, CancellationToken cancellationToken);
}
}
