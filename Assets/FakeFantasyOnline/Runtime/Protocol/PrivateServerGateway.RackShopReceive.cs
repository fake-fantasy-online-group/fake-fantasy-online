using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private bool TryReceiveRackShopPacket(ushort opcode, byte[] payload, IRackShopReceiveOwner owner)
    {
        // The product injects ReferenceWorldReceiveOwner and ClientRootView supplies the same
        // capability. Owner-less protocol hosts retain the existing observation-only path;
        // no snapshot or delayed world adapter can replay those observations into native state.
        if (owner == null) return false;
        switch (opcode)
        {
            case 0x803b:
                PublishRackShopGoodsResponse(ReadRackShopGoods(payload, owner), CancellationToken.None);
                return true;
            case 0x8042:
                WorldRackShopDepositInfo receipt = ReadRackShopDepositInfo(payload);
                owner.ReceiveRackShopDepositInfo(receipt);
                PublishRackShopDepositInfo(receipt, CancellationToken.None);
                return true;
            case 0x8059:
                WorldRackShopFittingResponse fitting = ReadRackShopFittingResponse(payload);
                owner.ReceiveRackShopFitting(fitting);
                PublishRackShopFittingResponse(fitting, CancellationToken.None);
                return true;
            case 0x806c:
                RackShopRefundResponse refunds = ReadRackShopRefunds(payload);
                owner.ReceiveRackShopRefunds(refunds.Percent, refunds.Days, refunds.Rows,
                    string.Empty, refunds.UsedOk);
                PublishRackShopRefundResponse(refunds, CancellationToken.None);
                return true;
            case 0x806e:
                var descriptions = ReadRackShopFittingDescriptions(payload);
                owner.ReceiveRackShopFittingDescriptions(descriptions);
                PublishRackShopFittingDescriptions(descriptions, CancellationToken.None);
                return true;
            default:
                return false;
        }
    }
}
}
