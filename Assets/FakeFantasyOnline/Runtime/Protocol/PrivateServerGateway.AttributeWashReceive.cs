using System.Collections.Generic;
using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private static void ReceiveAttributeWash(byte[] payload, IAttributeWashReceiveContext context)
    {
        var reader = new NetClient.Buf(payload);
        int kind = reader.U8();
        int tab = reader.U8();
        int slot = reader.I32();
        int itemId = reader.I32();
        int quality = reader.U8();
        int count = reader.U8();
        var affixes = new List<string>(count);
        for (int i = 0; i < count; i++) affixes.Add(reader.Str());
        var atMax = new List<bool>();
        if (reader.Remaining > 0)
        {
            int flagCount = reader.U8();
            for (int i = 0; i < flagCount && reader.Remaining > 0; i++) atMax.Add(reader.U8() != 0);
        }
        context.ReceiveAttributeWashInfo(kind, tab, slot, itemId, quality, affixes, atMax);
    }
}
}
