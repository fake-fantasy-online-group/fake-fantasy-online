using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway
    {
        private sealed class RackShopGoodsResponse
        {
            public RackShopGoodsResponse(IReadOnlyList<WorldRackShopGood> goods,
                IReadOnlyList<WorldRackShopCategory> categories, bool holiday, bool refundTabEnabled)
            { Goods = goods; Categories = categories; Holiday = holiday; RefundTabEnabled = refundTabEnabled; }
            public IReadOnlyList<WorldRackShopGood> Goods { get; }
            public IReadOnlyList<WorldRackShopCategory> Categories { get; }
            public bool Holiday { get; }
            public bool RefundTabEnabled { get; }
        }

        // Official Dispatch 0x803b: required goods/categories, then optional flags/masks.
        private static RackShopGoodsResponse ReadRackShopGoods(byte[] payload, IRackShopReceiveOwner owner = null)
        {
            var reader = new NetClient.Buf(payload);
            int count = reader.U16();
            IRackShopGoodsReceiveBuffer receipt = owner?.CreateRackShopGoodsBuffer(count);
            var goods = new List<WorldRackShopGood>(count);
            for (int i = 0; i < count; i++)
            {
                int id = reader.I32();
                string name = reader.Str();
                string description = reader.Str();
                int weight = reader.I32();
                byte group = (byte)reader.U8();
                int price = reader.I32();
                byte flags = (byte)reader.U8();
                int remain = reader.I32();
                string part = reader.Str();
                var row = new WorldRackShopGood(id, name, description, weight, group,
                    price, flags, remain, part, 0, id > 89999999 ? 3385 : id);
                receipt?.Add(row);
                goods.Add(row);
            }
            int categoryCount = reader.U8();
            var categories = new List<WorldRackShopCategory>(categoryCount);
            for (int i = 0; i < categoryCount; i++)
                categories.Add(new WorldRackShopCategory((byte)reader.U8(), reader.Str()));
            bool holiday = reader.Remaining > 0 && reader.U8() != 0;
            List<byte> masks = NetClient.ReadBlockedTail(reader);
            for (int i = 0; i < goods.Count && i < masks.Count; i++)
            {
                WorldRackShopGood row = goods[i];
                receipt?.SetBlocked(i, masks[i]);
                goods[i] = new WorldRackShopGood(row.Id, row.Name, row.Description, row.Weight,
                    row.Group, row.Price, row.Flags, row.Remain, row.Part, masks[i], row.IconItemId);
            }
            bool refundTabEnabled = reader.Remaining > 0 ? reader.U8() != 0 : true;
            receipt?.Commit(categories, holiday, refundTabEnabled);
            return new RackShopGoodsResponse(goods, categories, holiday, refundTabEnabled);
        }

        private void PublishRackShopGoodsResponse(RackShopGoodsResponse response, CancellationToken cancellationToken)
        {
            lock (_worldStateSync)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = RackShopState;
                Volatile.Write(ref _rackShopState, new WorldRackShopState(response.Categories, response.Goods,
                    response.Holiday, current.FittingCatalog, current.DepositInfo, current.RefundPercent,
                    current.RefundDays, current.RefundCategories, current.Refunds, current.RefundsUsedOk,
                    current.FittingDescriptions, response.RefundTabEnabled, current.RefundLoaded));
                Interlocked.Increment(ref _rackShopGoodsVersion);
                Interlocked.Increment(ref _rackShopStateVersion);
            }
        }
    }
}
