using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
public interface IEquipmentFusionCommands
{
    Task<Result> EquipFuseAsync(byte op, byte mainKind, byte mainTab, int mainSlot,
        byte subTab, int subSlot, int sig, CancellationToken cancellationToken);
}
}
