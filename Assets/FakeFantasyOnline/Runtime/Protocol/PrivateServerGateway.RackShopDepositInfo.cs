using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway
    {
        // Official Dispatch 0x8042: all seven fields, including QR, are required.
        private static WorldRackShopDepositInfo ReadRackShopDepositInfo(byte[] payload)
        {
            var reader = new NetClient.Buf(payload);
            return new WorldRackShopDepositInfo(reader.U8(), reader.Str(), reader.Str(),
                reader.I32(), reader.I64(), reader.Str(), reader.Str());
        }

        private void PublishRackShopDepositInfo(WorldRackShopDepositInfo receipt, CancellationToken cancellationToken)
        {
            lock (_worldStateSync)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = RackShopState;
                Volatile.Write(ref _rackShopState, new WorldRackShopState(current.Categories, current.Goods,
                    current.HolidayActive, current.FittingCatalog, receipt, current.RefundPercent,
                    current.RefundDays, current.RefundCategories, current.Refunds, current.RefundsUsedOk,
                    current.FittingDescriptions, current.RefundTabEnabled, current.RefundLoaded));
                Interlocked.Increment(ref _rackShopStateVersion);
            }
        }
    }
}
