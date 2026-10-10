using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : IRackShopReceiveOwner
{
    public IRackShopGoodsReceiveBuffer CreateRackShopGoodsBuffer(int capacity) =>
        ReferenceRackShopReceipt.CreateGoodsBuffer(capacity);
    public void ReceiveRackShopDepositInfo(WorldRackShopDepositInfo receipt) =>
        ReferenceRackShopReceipt.Deposit(receipt);
    public void ReceiveRackShopFitting(WorldRackShopFittingResponse response) =>
        ReferenceRackShopReceipt.Fitting(response);
    public void ReceiveRackShopRefunds(int percent, int days, IReadOnlyList<WorldRackShopRefund> rows,
        string categories, bool usedOk) => ReferenceRackShopReceipt.Refunds(percent, days, rows, categories, usedOk);
    public void ReceiveRackShopFittingDescriptions(IReadOnlyList<WorldRackShopFittingDescription> rows) =>
        ReferenceRackShopReceipt.Descriptions(rows);
}

// Projection only. Both Pump forms reach these same existing native owners at receipt time;
// nothing retains a world window, command capability, snapshot, or replayable callback.
internal static class ReferenceRackShopReceipt
{
    internal static IRackShopGoodsReceiveBuffer CreateGoodsBuffer(int capacity) => new GoodsBuffer(capacity);

    private sealed class GoodsBuffer : IRackShopGoodsReceiveBuffer
    {
        private readonly List<RackShopDlg.Good> goods;
        internal GoodsBuffer(int capacity) { goods = new List<RackShopDlg.Good>(capacity); }
        public void Add(WorldRackShopGood row)
        {
            goods.Add(new RackShopDlg.Good { id = row.Id, name = row.Name,
                desc = row.Description, weight = row.Weight, group = row.Group,
                price = row.Price, flags = row.Flags, remain = row.Remain, part = row.Part,
                blocked = row.Blocked, icon = NetClient.LoadItemIcon(row.Id > 89999999 ? 3385 : row.Id) });
        }
        public void SetBlocked(int index, byte blocked)
        {
            RackShopDlg.Good row = goods[index];
            row.blocked = blocked;
            goods[index] = row;
        }
        public void Commit(IReadOnlyList<WorldRackShopCategory> categories, bool holiday, bool refundTabEnabled)
        {
            var cats = new List<(byte, string)>(categories.Count);
            foreach (WorldRackShopCategory row in categories) cats.Add((row.Id, row.Name));
            RackShopDlg.SetGoods(goods);
            RackShopDlg.SetCats(cats);
            RackShopDlg.SetHoliday(holiday);
            RackShopDlg.SetRefundTab(refundTabEnabled);
        }
    }

    internal static void Deposit(WorldRackShopDepositInfo receipt) =>
        RackShopDlg.OnDepositInfo(receipt.Ok, receipt.Chain, receipt.Address,
            receipt.Amount, receipt.Caiyu, receipt.Warning, receipt.QrBase64);

    internal static void Fitting(WorldRackShopFittingResponse response)
    {
        var items = new List<RackShopDlg.FitItem>(response.Items.Count);
        foreach (WorldRackShopFitItem row in response.Items)
            items.Add(new RackShopDlg.FitItem { id = row.Id, name = row.Name,
                kind = row.Kind, part = row.Part, price = row.Price,
                model = row.Model, desc = row.Description });
        if (response.IsPage) RackShopDlg.SetFittingPage(items, response.Total, response.Page);
        else RackShopDlg.AddFittingCatalog(items, response.More);
    }

    internal static void Refunds(int percent, int days, IReadOnlyList<WorldRackShopRefund> rows,
        string categories, bool usedOk)
    {
        var refunds = new List<RackShopDlg.RefundRow>(rows.Count);
        foreach (WorldRackShopRefund row in rows)
            refunds.Add(new RackShopDlg.RefundRow { itemId = row.ItemId, name = row.Name,
                unitPrice = row.UnitPrice, bundle = row.Bundle, day = row.Day,
                shares = row.Shares, have = row.Have, gain = row.Gain,
                leftDays = row.LeftDays, icon = NetClient.LoadItemIcon(row.ItemId) });
        RackShopDlg.SetRefunds(percent, days, refunds, categories, usedOk);
    }

    internal static void Descriptions(IReadOnlyList<WorldRackShopFittingDescription> rows)
    {
        foreach (WorldRackShopFittingDescription row in rows)
            RackShopDlg.AddFittingDesc(row.Id, row.Name, row.Price, row.Description);
    }
}
}
