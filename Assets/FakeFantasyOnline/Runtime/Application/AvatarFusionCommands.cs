using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
public interface IAvatarFusionCommands
{
    Task<Result> ApplyAvatarAsync(byte bagTab, int bagSlot, int targetSlot, byte kind, byte targetKind,
        byte targetTab, CancellationToken cancellationToken);
}
}
