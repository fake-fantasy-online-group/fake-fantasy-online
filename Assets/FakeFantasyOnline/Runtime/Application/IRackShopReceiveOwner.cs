using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
// One host-thread callback per reply, before world admission or any map window exists.
// The native owner decides instance existence, catalog accumulation and cache mutation.
public interface IRackShopReceiveOwner
{
    IRackShopGoodsReceiveBuffer CreateRackShopGoodsBuffer(int capacity);
    void ReceiveRackShopDepositInfo(WorldRackShopDepositInfo receipt);
    void ReceiveRackShopFitting(WorldRackShopFittingResponse response);
    void ReceiveRackShopRefunds(int percent, int days, IReadOnlyList<WorldRackShopRefund> rows,
        string categories, bool usedOk);
    void ReceiveRackShopFittingDescriptions(IReadOnlyList<WorldRackShopFittingDescription> rows);
}

// Ephemeral 0x803b row list: load each icon at the original row position, but publish only
// after all required rows/categories and the optional tails have been read successfully.
public interface IRackShopGoodsReceiveBuffer
{
    void Add(WorldRackShopGood row);
    void SetBlocked(int index, byte blocked);
    void Commit(IReadOnlyList<WorldRackShopCategory> categories, bool holiday, bool refundTabEnabled);
}
}
