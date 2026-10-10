using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway
    {
        /// <summary>0x806e: U16 count followed by fitting description rows.</summary>
        private static IReadOnlyList<WorldRackShopFittingDescription> ReadRackShopFittingDescriptions(byte[] payload)
        {
            var reader = new NetClient.Buf(payload);
            int count = reader.U16();
            var rows = new List<WorldRackShopFittingDescription>(count);
            for (int i = 0; i < count; i++)
                rows.Add(new WorldRackShopFittingDescription(reader.I32(), reader.Str(), reader.I32(), reader.Str()));
            return rows;
        }
    }
}
