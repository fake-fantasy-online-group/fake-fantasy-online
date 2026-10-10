using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
    public interface IPartyCommands
    {
        Task<Result> CreateAndInviteAsync(string name, IReadOnlyList<PartyInviteTarget> targets, CancellationToken cancellationToken);
        Task<Result> InviteAsync(int targetId, string targetName, CancellationToken cancellationToken);
        Task<Result> JoinAsync(int targetId, string targetName, CancellationToken cancellationToken);
        Task<Result> KickAsync(int targetId, CancellationToken cancellationToken);
        Task<Result> SetLeaderAsync(int targetId, CancellationToken cancellationToken);
        Task<Result> CreateAsync(string name, CancellationToken cancellationToken);
        Task<Result> LeaveAsync(CancellationToken cancellationToken);
        Task<Result> DismissAsync(CancellationToken cancellationToken);
    }

    public sealed class PartyInviteTarget
    {
        public PartyInviteTarget(int entityId, string name)
        { EntityId = entityId; Name = name ?? string.Empty; }
        public int EntityId { get; }
        public string Name { get; }
    }
}
