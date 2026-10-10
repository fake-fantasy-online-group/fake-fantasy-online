using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
public interface ICharacterAppearanceCommands
{
    Task<Result> ChangeHairAsync(byte mode, byte targetId, CancellationToken cancellationToken);
}
}
