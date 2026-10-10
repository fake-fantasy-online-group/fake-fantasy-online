using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway
    {
        // Official Dispatch 0x8059: absent mode means catalog; nonzero mode is a page.
        private static WorldRackShopFittingResponse ReadRackShopFittingResponse(byte[] payload)
        {
            var reader = new NetClient.Buf(payload);
            int count = reader.U16();
            var rows = new List<WorldRackShopFitItem>(count);
            for (int i = 0; i < count; i++)
                rows.Add(new WorldRackShopFitItem(reader.I32(), reader.Str(), (byte)reader.U8(),
                    reader.Str(), reader.I32(), reader.I32(), reader.Str()));
            bool more = reader.Remaining > 0 && reader.U8() != 0;
            bool isPage = reader.Remaining > 0 && reader.U8() != 0;
            int total = reader.Remaining >= 4 ? reader.I32() : 0;
            int page = reader.Remaining >= 4 ? reader.I32() : 0;
            return new WorldRackShopFittingResponse(rows, more, isPage, total, page);
        }

        private void PublishRackShopFittingResponse(WorldRackShopFittingResponse response,
            CancellationToken cancellationToken)
        {
            lock (_worldStateSync)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Native accumulation/page/cache changes already ran once at receipt. This
                // version is observational only; the compatibility catalog is never replayed.
                Interlocked.Increment(ref _rackShopStateVersion);
            }
        }
    }
}
