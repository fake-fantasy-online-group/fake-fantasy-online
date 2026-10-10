using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
public sealed class ItemLockRequest
{
    public ItemLockRequest(int tab, int slot, bool on, string securityCode = "", int itemId = 0)
    { Tab = tab; Slot = slot; On = on; SecurityCode = securityCode; ItemId = itemId; }
    public int Tab { get; }
    public int Slot { get; }
    public bool On { get; }
    public string SecurityCode { get; }
    public int ItemId { get; }
}
public interface IItemLockCommands
{
    Task<Result> SetItemLockAsync(ItemLockRequest request, CancellationToken cancellationToken);
}
}
