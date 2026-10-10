using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
public interface IShopCommands
{
    Task<Result> OpenNativeShopAsync(int shopId, CancellationToken cancellationToken);
    Task<Result> BuyItemAsync(int itemId, int count, CancellationToken cancellationToken);
    Task<Result> SellItemAsync(byte tab, int slot, int count, int itemId, bool confirmed, CancellationToken cancellationToken);
    Task<Result> StallDealAsync(int ownerId, int index, int count, byte bagTab, int bagSlot,
        int expectItemId, long expectPrice, CancellationToken cancellationToken);
}
}
