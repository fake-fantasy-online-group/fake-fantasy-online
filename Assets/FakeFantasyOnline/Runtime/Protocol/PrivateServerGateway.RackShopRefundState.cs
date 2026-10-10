using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway
    {
        private sealed class RackShopRefundResponse
        {
            public RackShopRefundResponse(int percent, int days, IReadOnlyList<WorldRackShopRefund> rows, bool usedOk)
            { Percent = percent; Days = days; Rows = rows; UsedOk = usedOk; }
            public int Percent { get; }
            public int Days { get; }
            public IReadOnlyList<WorldRackShopRefund> Rows { get; }
            public bool UsedOk { get; }
        }

        private void PublishRackShopRefundResponse(RackShopRefundResponse response, CancellationToken cancellationToken)
        {
            PublishRackShopRefunds(response.Percent, response.Days, string.Empty, response.Rows, response.UsedOk, cancellationToken);
        }

        /// <summary>0x806c: U8 percent, U16 days, U16 row count, refund rows, optional used-ok tail.</summary>
        private static RackShopRefundResponse ReadRackShopRefunds(byte[] payload)
        {
            var reader = new NetClient.Buf(payload);
            int percent = reader.U8();
            int days = reader.U16();
            int count = reader.U16();
            var rows = new List<WorldRackShopRefund>(count);
            for (int i = 0; i < count; i++)
            {
                int itemId = reader.I32();
                string name = reader.Str();
                int unitPrice = reader.I32();
                int bundle = reader.I32();
                int day = reader.I32();
                int shares = reader.I32();
                int have = reader.I32();
                int gain = reader.I32();
                int leftDays = reader.I32();
                rows.Add(new WorldRackShopRefund(itemId, unitPrice, bundle, day, shares, have, gain, leftDays, name));
            }
            bool usedOk = reader.Remaining > 0 ? reader.U8() != 0 : true;
            return new RackShopRefundResponse(percent, days, rows, usedOk);
        }
    }
}
