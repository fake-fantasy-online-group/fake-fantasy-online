using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

public sealed partial class PrivateServerGateway
{
    // Dispatch 0600077f: arm64 2553d8..255748, x86_64 26493a..264c68.
    // Row count/rank/kills and myRank/players are U8; scores retain their signed width.
    // Complete rows and Data are private until the one ArenaResultDlg.Show tail.
    private void ReceiveArenaResult(byte[] payload)
    {
        var reader = new NetClient.Buf(payload);
        int count = reader.U8();
        var rows = new List<WorldArenaResultRow>(count);
        for (int index = 0; index < count; index++)
        {
            rows.Add(new WorldArenaResultRow(
                reader.Str(),
                reader.U8(),
                reader.U8(),
                reader.I32(),
                reader.I32(),
                reader.I32(),
                reader.U8() != 0));
        }
        var state = new WorldArenaResultState(
            rows,
            reader.U8(),
            reader.U8(),
            reader.I32(),
            reader.I32(),
            reader.I32(),
            reader.I32(),
            reader.I32(),
            reader.Str(),
            reader.I32(),
            reader.I64(),
            reader.U8() != 0,
            reader.I32(),
            reader.I64(),
            reader.Str(),
            reader.I64(),
            reader.I32(),
            reader.I32(),
            reader.I32(),
            reader.I32(),
            reader.I32(),
            reader.I32(),
            reader.I64(),
            reader.I32());
        (_worldReceiveOwner as IWorldArenaResultReceiveOwner)?.ReceiveArenaResult(state);
    }
}

}
