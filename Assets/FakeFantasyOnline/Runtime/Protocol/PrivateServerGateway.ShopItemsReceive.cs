using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private void ReceiveShopItems(byte[] payload, IShopItemsReceiveContext context)
    {
        var reader = new NetClient.Buf(payload);
        int shopId = reader.I32();
        int count = reader.U16();
        IShopItemsReceiveBuffer items = context.CreateShopItemsBuffer(count);
        var snapshot = new List<WorldShopItemState>(count);
        for (int i = 0; i < count; i++)
        {
            int id = reader.I32();
            int price = reader.I32();
            string name = reader.Str();
            string desc = reader.Str();
            items.Add(id, price, name, desc);
            snapshot.Add(new WorldShopItemState(id, price, name, desc, (byte)0));
        }
        var blocked = NetClient.ReadBlockedTail(reader);
        for (int i = 0; i < blocked.Count && i < count; i++)
        {
            items.SetBlocked(i, blocked[i]);
            WorldShopItemState item = snapshot[i];
            snapshot[i] = new WorldShopItemState(item.ItemId, item.Price, item.Name, item.Description, blocked[i]);
        }
        items.Commit(shopId);
        PublishShopState(WorldShopState.FromNative(shopId, snapshot), CancellationToken.None);
    }
}
}
