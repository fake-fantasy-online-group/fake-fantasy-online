using System.Threading;
using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private void ReceiveGroundSpawn(byte[] payload, IGroundItemReceiveContext context)
    {
        if (!PrivateServerGroundItemCodec.TryDecodeSpawn(payload, out WorldGroundItemState state)) return;
        if (context.SpawnGroundItem(state)) PublishGroundItemState(state, CancellationToken.None, replaceExisting: true);
    }
    private void ReceiveGroundRemoval(byte[] payload, IGroundItemReceiveContext context)
    {
        int id = new NetClient.Buf(payload).I32();
        context.RemoveGroundItem(id);
        RemoveGroundItemState(id, CancellationToken.None);
    }
}
}
