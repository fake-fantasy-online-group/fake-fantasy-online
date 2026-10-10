using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
    public interface IRackCommands
    {
        Task<Result> OpenRackAsync(CancellationToken ct);
        Task<Result> BuyAsync(int itemId, byte source, int count, CancellationToken ct);
        Task<Result> RequestDepositAsync(string chain, int amount, CancellationToken ct);
        Task<Result> RequestRefundListAsync(CancellationToken ct);
        Task<Result> RefundAsync(int itemId, int unitPrice, int bundle, int day, int shares, CancellationToken ct);
        Task<Result> HelpAsync(CancellationToken ct);
    }
}
