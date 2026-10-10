using System.Collections.Generic;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    internal static List<byte> ReadBlockedTail(Buf reader)
    {
        var values = new List<byte>();
        if (reader.Remaining >= 2)
        {
            int count = reader.U16();
            for (int i = 0; i < count && reader.Remaining > 0; i++) values.Add((byte)reader.U8());
        }
        return values;
    }
}
}
