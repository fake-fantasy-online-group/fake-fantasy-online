using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.Application
{
    public enum WorldRackShopUpdateKind { Goods, Refunds, Descriptions, Deposit, Fitting }

    // One decoded reply. The receive capability hands this directly to the native owner;
    // catalog chunks and callbacks cannot be reconstructed from a last-value snapshot.
    public sealed class WorldRackShopFittingResponse
    {
        public WorldRackShopFittingResponse(IReadOnlyList<WorldRackShopFitItem> items,
            bool more, bool isPage, int total, int page)
        {
            Items = new ReadOnlyCollection<WorldRackShopFitItem>(
                new List<WorldRackShopFitItem>(items ?? Array.Empty<WorldRackShopFitItem>()));
            More = more; IsPage = isPage; Total = total; Page = page;
        }
        public IReadOnlyList<WorldRackShopFitItem> Items { get; }
        public bool More { get; }
        public bool IsPage { get; }
        public int Total { get; }
        public int Page { get; }
    }

    // Legacy observation envelope retained for source compatibility; there is no native
    // replay queue or world-window consumer for this type.
    public sealed class WorldRackShopUpdate
    {
        private WorldRackShopUpdate(WorldRackShopUpdateKind kind, WorldRackShopState state = null,
            IReadOnlyList<WorldRackShopFittingDescription> descriptions = null,
            WorldRackShopDepositInfo deposit = null, WorldRackShopFittingResponse fitting = null)
        {
            Kind = kind; State = state; DepositInfo = deposit; FittingResponse = fitting;
            FittingDescriptions = new ReadOnlyCollection<WorldRackShopFittingDescription>(
                new List<WorldRackShopFittingDescription>(descriptions ?? Array.Empty<WorldRackShopFittingDescription>()));
        }
        public WorldRackShopUpdateKind Kind { get; }
        public WorldRackShopState State { get; }
        public IReadOnlyList<WorldRackShopFittingDescription> FittingDescriptions { get; }
        public WorldRackShopDepositInfo DepositInfo { get; }
        public WorldRackShopFittingResponse FittingResponse { get; }
        public static WorldRackShopUpdate Goods(WorldRackShopState state) => new WorldRackShopUpdate(WorldRackShopUpdateKind.Goods, state);
        public static WorldRackShopUpdate Refunds(WorldRackShopState state) => new WorldRackShopUpdate(WorldRackShopUpdateKind.Refunds, state);
        public static WorldRackShopUpdate Descriptions(IReadOnlyList<WorldRackShopFittingDescription> rows) => new WorldRackShopUpdate(WorldRackShopUpdateKind.Descriptions, descriptions: rows);
        public static WorldRackShopUpdate Deposit(WorldRackShopDepositInfo receipt) => new WorldRackShopUpdate(WorldRackShopUpdateKind.Deposit, deposit: receipt);
        public static WorldRackShopUpdate Fitting(WorldRackShopFittingResponse response) => new WorldRackShopUpdate(WorldRackShopUpdateKind.Fitting, fitting: response);
    }
}
