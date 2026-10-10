using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IRackShopReceiveOwner
{
    IRackShopGoodsReceiveBuffer IRackShopReceiveOwner.CreateRackShopGoodsBuffer(int capacity) =>
        ReferenceRackShopReceipt.CreateGoodsBuffer(capacity);
    void IRackShopReceiveOwner.ReceiveRackShopDepositInfo(WorldRackShopDepositInfo receipt) =>
        ReferenceRackShopReceipt.Deposit(receipt);
    void IRackShopReceiveOwner.ReceiveRackShopFitting(WorldRackShopFittingResponse response) =>
        ReferenceRackShopReceipt.Fitting(response);
    void IRackShopReceiveOwner.ReceiveRackShopRefunds(int percent, int days,
        IReadOnlyList<WorldRackShopRefund> rows, string categories, bool usedOk) =>
        ReferenceRackShopReceipt.Refunds(percent, days, rows, categories, usedOk);
    void IRackShopReceiveOwner.ReceiveRackShopFittingDescriptions(IReadOnlyList<WorldRackShopFittingDescription> rows) =>
        ReferenceRackShopReceipt.Descriptions(rows);
}
}
