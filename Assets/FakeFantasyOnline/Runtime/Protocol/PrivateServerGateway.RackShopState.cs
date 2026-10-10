using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway : IRackShopStateSource
    {
        private WorldRackShopState _rackShopState = WorldRackShopState.Empty;
        private long _rackShopStateVersion;
        private long _rackShopGoodsVersion;
        public WorldRackShopState RackShopState => Volatile.Read(ref _rackShopState);
        public long RackShopStateVersion => Interlocked.Read(ref _rackShopStateVersion);
        public long RackShopGoodsVersion => Interlocked.Read(ref _rackShopGoodsVersion);

        // Observation-only compatibility publisher. Live 0x803b preserves other fields through
        // PublishRackShopGoodsResponse; no publisher can invoke or replay a native callback.
        internal void PublishRackShopState(IReadOnlyList<WorldRackShopGood> goods, bool holidayActive,
            IReadOnlyList<WorldRackShopFitItem> fittingCatalog)
            => PublishRackShopState(null, goods, holidayActive, fittingCatalog, null);

        internal void PublishRackShopState(IReadOnlyList<WorldRackShopCategory> categories,
            IReadOnlyList<WorldRackShopGood> goods, bool holidayActive,
            IReadOnlyList<WorldRackShopFitItem> fittingCatalog, WorldRackShopDepositInfo depositInfo)
        {
            lock (_worldStateSync)
            {
                Volatile.Write(ref _rackShopState, new WorldRackShopState(categories, goods, holidayActive, fittingCatalog, depositInfo));
                Interlocked.Increment(ref _rackShopGoodsVersion);
                Interlocked.Increment(ref _rackShopStateVersion);
            }
        }

        internal void PublishRackShopRefunds(int percent, int days, string categories,
            IReadOnlyList<WorldRackShopRefund> refunds, bool usedOk, CancellationToken cancellationToken = default)
        {
            lock (_worldStateSync)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = RackShopState;
                Volatile.Write(ref _rackShopState, new WorldRackShopState(current.Categories, current.Goods,
                    current.HolidayActive, current.FittingCatalog, current.DepositInfo, percent, days,
                    categories,
                    refunds, usedOk, current.FittingDescriptions, current.RefundTabEnabled, true));
                Interlocked.Increment(ref _rackShopStateVersion);
            }
        }

        internal void PublishRackShopFittingDescriptions(IReadOnlyList<WorldRackShopFittingDescription> descriptions, CancellationToken cancellationToken = default)
        {
            lock (_worldStateSync)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = RackShopState;
                // Official AddFittingDesc writes one dictionary entry and never clears other ids.
                var merged = new Dictionary<int, WorldRackShopFittingDescription>();
                foreach (var row in current.FittingDescriptions) merged[row.Id] = row;
                foreach (var row in descriptions) merged[row.Id] = row;
                Volatile.Write(ref _rackShopState, new WorldRackShopState(current.Categories, current.Goods,
                    current.HolidayActive, current.FittingCatalog, current.DepositInfo, current.RefundPercent,
                    current.RefundDays, current.RefundCategories, current.Refunds, current.RefundsUsedOk,
                    new List<WorldRackShopFittingDescription>(merged.Values), current.RefundTabEnabled, current.RefundLoaded));
                Interlocked.Increment(ref _rackShopStateVersion);
            }
        }
    }
}
