using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private static void ReceiveIconOverrides(byte[] payload, IItemIconOverrideReceiveContext context)
    {
        var reader = new NetClient.Buf(payload);
        context.EnsureIconMap();
        int count = reader.U16();
        for (int i = 0; i < count; i++)
        {
            int itemId = reader.I32();
            int iconId = reader.I32();
            context.ApplyIconOverride(itemId, iconId);
        }
        context.ClearGroundIconCache();
    }
}
}
