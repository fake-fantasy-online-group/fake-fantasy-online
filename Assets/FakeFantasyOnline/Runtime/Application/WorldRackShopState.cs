using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.Application
{
    /// <summary>Immutable mirror of RackShopDlg.Good from the official 1.5.8 client.</summary>
    public sealed class WorldRackShopGood
    {
        public WorldRackShopGood(int id, string name, string description, int weight, byte group,
            int price, byte flags, int remain, string part, byte blocked)
            : this(id, name, description, weight, group, price, flags, remain, part, blocked, id) { }
        public WorldRackShopGood(int id, string name, string description, int weight, byte group,
            int price, byte flags, int remain, string part, byte blocked, int iconItemId)
        {
            Id = id; Name = name ?? string.Empty; Description = description ?? string.Empty;
            Weight = weight; Group = group; Price = price; Flags = flags; Remain = remain;
            Part = part ?? string.Empty; Blocked = blocked; IconItemId = iconItemId;
        }
        public int Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int Weight { get; }
        public byte Group { get; }
        public int Price { get; }
        public byte Flags { get; }
        public int Remain { get; }
        public string Part { get; }
        public byte Blocked { get; }
        public int IconItemId { get; }
    }

    /// <summary>Immutable mirror of RackShopDlg.FitItem used by the fitting catalog.</summary>
    public sealed class WorldRackShopFitItem
    {
        public WorldRackShopFitItem(int id, string name, byte kind, string part, int price, int model, string description)
        {
            Id = id; Name = name ?? string.Empty; Kind = kind; Part = part ?? string.Empty;
            Price = price; Model = model; Description = description ?? string.Empty;
        }
        public int Id { get; }
        public string Name { get; }
        public byte Kind { get; }
        public string Part { get; }
        public int Price { get; }
        public int Model { get; }
        public string Description { get; }
    }

    public sealed class WorldRackShopCategory
    {
        public WorldRackShopCategory(byte id, string name) { Id = id; Name = name ?? string.Empty; }
        public byte Id { get; }
        public string Name { get; }
    }

    public sealed class WorldRackShopRefund
    {
        public WorldRackShopRefund(int itemId, int unitPrice, int bundle, int day, int shares,
            int have, int gain, int leftDays, string name)
        { ItemId = itemId; UnitPrice = unitPrice; Bundle = bundle; Day = day; Shares = shares; Have = have; Gain = gain; LeftDays = leftDays; Name = name ?? string.Empty; }
        public int ItemId { get; }
        public int UnitPrice { get; }
        public int Bundle { get; }
        public int Day { get; }
        public int Shares { get; }
        public int Have { get; }
        public int Gain { get; }
        public int LeftDays { get; }
        public string Name { get; }
    }

    public sealed class WorldRackShopFittingDescription
    {
        public WorldRackShopFittingDescription(int id, string name, int price, string description)
        { Id = id; Name = name ?? string.Empty; Price = price; Description = description ?? string.Empty; }
        public int Id { get; }
        public string Name { get; }
        public int Price { get; }
        public string Description { get; }
    }

    /// <summary>Official RackShopDlg.OnDepositInfo receipt (QR data remains server authored).</summary>
    public sealed class WorldRackShopDepositInfo
    {
        public WorldRackShopDepositInfo(int ok, string chain, string address, int amount, long caiyu, string warning, string qrBase64)
        { Ok = ok; Chain = chain ?? string.Empty; Address = address ?? string.Empty; Amount = amount; Caiyu = caiyu; Warning = warning ?? string.Empty; QrBase64 = qrBase64 ?? string.Empty; }
        public int Ok { get; }
        public string Chain { get; }
        public string Address { get; }
        public int Amount { get; }
        public long Caiyu { get; }
        public string Warning { get; }
        public string QrBase64 { get; }
    }

    public sealed class WorldRackShopState
    {
        public static readonly WorldRackShopState Empty = new WorldRackShopState(
            Array.Empty<WorldRackShopCategory>(), Array.Empty<WorldRackShopGood>(), false,
            Array.Empty<WorldRackShopFitItem>(), null, 0, 0, string.Empty,
            Array.Empty<WorldRackShopRefund>(), true, Array.Empty<WorldRackShopFittingDescription>());
        public WorldRackShopState(IReadOnlyList<WorldRackShopGood> goods, bool holidayActive,
            IReadOnlyList<WorldRackShopFitItem> fittingCatalog)
            : this(Array.Empty<WorldRackShopCategory>(), goods, holidayActive, fittingCatalog, null) { }
        public WorldRackShopState(IReadOnlyList<WorldRackShopCategory> categories,
            IReadOnlyList<WorldRackShopGood> goods, bool holidayActive,
            IReadOnlyList<WorldRackShopFitItem> fittingCatalog, WorldRackShopDepositInfo depositInfo)
            : this(categories, goods, holidayActive, fittingCatalog, depositInfo, 0, 0, string.Empty,
                Array.Empty<WorldRackShopRefund>(), true) { }
        public WorldRackShopState(IReadOnlyList<WorldRackShopCategory> categories,
            IReadOnlyList<WorldRackShopGood> goods, bool holidayActive,
            IReadOnlyList<WorldRackShopFitItem> fittingCatalog, WorldRackShopDepositInfo depositInfo,
            int refundPercent, int refundDays, string refundCategories,
            IReadOnlyList<WorldRackShopRefund> refunds, bool refundsUsedOk)
            : this(categories, goods, holidayActive, fittingCatalog, depositInfo, refundPercent, refundDays,
                refundCategories, refunds, refundsUsedOk, Array.Empty<WorldRackShopFittingDescription>()) { }
        public WorldRackShopState(IReadOnlyList<WorldRackShopCategory> categories,
            IReadOnlyList<WorldRackShopGood> goods, bool holidayActive,
            IReadOnlyList<WorldRackShopFitItem> fittingCatalog, WorldRackShopDepositInfo depositInfo,
            int refundPercent, int refundDays, string refundCategories,
            IReadOnlyList<WorldRackShopRefund> refunds, bool refundsUsedOk,
            IReadOnlyList<WorldRackShopFittingDescription> fittingDescriptions, bool refundTabEnabled = true, bool refundLoaded = false)
        {
            Categories = new ReadOnlyCollection<WorldRackShopCategory>(new List<WorldRackShopCategory>(categories ?? Array.Empty<WorldRackShopCategory>()));
            Goods = new ReadOnlyCollection<WorldRackShopGood>(new List<WorldRackShopGood>(goods ?? Array.Empty<WorldRackShopGood>()));
            HolidayActive = holidayActive;
            FittingCatalog = new ReadOnlyCollection<WorldRackShopFitItem>(new List<WorldRackShopFitItem>(fittingCatalog ?? Array.Empty<WorldRackShopFitItem>()));
            DepositInfo = depositInfo;
            RefundPercent = refundPercent; RefundDays = refundDays; RefundCategories = refundCategories ?? string.Empty;
            Refunds = new ReadOnlyCollection<WorldRackShopRefund>(new List<WorldRackShopRefund>(refunds ?? Array.Empty<WorldRackShopRefund>()));
            RefundsUsedOk = refundsUsedOk;
            RefundTabEnabled = refundTabEnabled;
            RefundLoaded = refundLoaded;
            FittingDescriptions = new ReadOnlyCollection<WorldRackShopFittingDescription>(new List<WorldRackShopFittingDescription>(fittingDescriptions ?? Array.Empty<WorldRackShopFittingDescription>()));
        }
        public IReadOnlyList<WorldRackShopCategory> Categories { get; }
        public IReadOnlyList<WorldRackShopGood> Goods { get; }
        public bool HolidayActive { get; }
        // Compatibility observation only. Live catalog/page replies invoke the native owner
        // directly at receipt; this snapshot has no accumulation or callback authority.
        public IReadOnlyList<WorldRackShopFitItem> FittingCatalog { get; }
        public WorldRackShopDepositInfo DepositInfo { get; }
        public int RefundPercent { get; }
        public int RefundDays { get; }
        public string RefundCategories { get; }
        public IReadOnlyList<WorldRackShopRefund> Refunds { get; }
        public bool RefundsUsedOk { get; }
        public bool RefundTabEnabled { get; }
        public bool RefundLoaded { get; }
        public IReadOnlyList<WorldRackShopFittingDescription> FittingDescriptions { get; }
    }

    public interface IRackShopStateSource
    {
        WorldRackShopState RackShopState { get; }
        long RackShopStateVersion { get; }
        long RackShopGoodsVersion { get; }
    }
}
