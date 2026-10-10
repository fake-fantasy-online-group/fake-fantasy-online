using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
public interface IAscendSkillCommands
{
    Task<Result> AscendSkillAsync(int skillId, CancellationToken cancellationToken);
}
}
