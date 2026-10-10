using System.Threading;
using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private void ReceiveGroundSpawn(byte[] payload, IGroundItemReceiveContext context)
    {
        var reader = new NetClient.Buf(payload);
        int id = reader.I32();
        int itemId = reader.I32();
        int count = reader.I32();
        int x = reader.I32();
        int y = reader.I32();
        string name = reader.Str();
        bool owned = reader.U8() != 0;
        int lockMs = reader.I32();
        byte kind = (byte)reader.U8();
        byte quality = reader.Remaining > 0 ? (byte)reader.U8() : (byte)0;
        var state = WorldGroundItemState.FromNative(id, itemId, count, x, y, name, owned, lockMs, kind, quality);
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
