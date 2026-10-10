using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
    public interface IFamilyCommands
    {
        Task<Result> InviteAsync(int id, string name, CancellationToken ct);
        Task<Result> LeaveAsync(CancellationToken ct);
        Task<Result> CreateAsync(string name, string proclaim, CancellationToken ct);
        Task<Result> ListAsync(CancellationToken ct);
        Task<Result> ApplyAsync(string name, CancellationToken ct);
        Task<Result> ManageAsync(byte op, string name, int id, string text, CancellationToken ct);
        Task<Result> MailAsync(string title, string body, CancellationToken ct);
        Task<Result> PositionsAsync(CancellationToken ct);
        Task<Result> RefreshAsync(CancellationToken ct);
        Task<Result> PositionNameAsync(int id, string name, CancellationToken ct);
        Task<Result> StashAsync(int op, int bagTab, int bagSlot, int index, CancellationToken ct);
        Task<Result> StashPermissionAsync(int pos, CancellationToken ct);
    }
}
