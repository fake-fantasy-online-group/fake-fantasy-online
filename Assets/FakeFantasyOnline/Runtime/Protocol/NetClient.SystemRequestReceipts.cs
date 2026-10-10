using System.Collections.Generic;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Original Dispatch 0x8035 read/list prefix. SetList receives this same list through
    // the existing social owner seam. Preserve the parameterless list constructor and
    // construct each value tuple only after all five reads, before reading another row.
    internal static List<(int, byte, int, string, string)> ReadSystemRequestRows(Buf reader)
    {
        int count = reader.U8();
        var requests = new List<(int, byte, int, string, string)>();
        for (int index = 0; index < count; index++)
        {
            int requestId = reader.I32();
            byte kind = (byte)reader.U8();
            int fromId = reader.I32();
            string fromName = reader.Str();
            string text = reader.Str();
            requests.Add((requestId, kind, fromId, fromName, text));
        }
        return requests;
    }
}
}
