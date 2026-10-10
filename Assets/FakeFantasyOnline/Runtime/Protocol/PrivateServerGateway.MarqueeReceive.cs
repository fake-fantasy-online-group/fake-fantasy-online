using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

public sealed partial class PrivateServerGateway
{
    // Dispatch 0600077f: arm64 2534e4..253608,255db4..255dec;
    // x86_64 26260d..26272f,26541c..26545f. No remaining-byte or mode range gate.
    private void ReceiveMarquee(byte[] payload)
    {
        var reader = new NetClient.Buf(payload);
        byte mode = (byte)reader.U8();
        int count = reader.U16();
        var lines = new List<ValueTuple<int, string>>(count);
        for (int index = 0; index < count; index++)
        {
            int bracket = reader.I32();
            string text = reader.Str();
            lines.Add(new ValueTuple<int, string>(bracket, text));
        }
        int speed = reader.I32();
        int intervalSeconds = reader.I32();
        byte enabled = (byte)reader.U8();
        int periodMinutes = reader.I32();
        (_worldReceiveOwner as IWorldMarqueeReceiveOwner)?.ReceiveMarquee(
            new WorldMarqueeState(mode, lines, speed, intervalSeconds, enabled, periodMinutes));
    }
}

}
