using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{

/// <summary>Shared native skill-use behavior entry, implemented by the recovered client owner.</summary>
public interface ICharacterSkillUsePort
{
    bool RequestSkill(int skillId, float rangePixels, CancellationToken cancellationToken);
    Task<Result> CastAtTargetAsync(int skillId, CancellationToken cancellationToken);
    Task<Result> UseSkillAsync(int skillId, int targetId, CancellationToken cancellationToken);
    Task<Result> UseSkillAtAsync(int skillId, int x, int y, int targetId, CancellationToken cancellationToken);
}

}
