using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
public interface ICharacterWorkCommands
{
    Task<Result> BeginCharacterWorkAsync(int skill, CancellationToken cancellationToken);
    Task<Result> GatherCharacterWorkAsync(int skill, CancellationToken cancellationToken);
}
}
