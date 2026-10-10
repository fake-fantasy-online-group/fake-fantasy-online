using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

public sealed partial class PrivateServerGateway
{
    // Dispatch 0600077f case 0x804a: arm64 253eac..254020 / 256280..2562a4;
    // x86_64 263095..263280 / 2657b7..2657dd. All reads precede the owner call,
    // including the zero-count path. No remaining-byte, kind or coordinate gate.
    private void ReceiveGatherSpots(byte[] payload)
    {
        var reader = new NetClient.Buf(payload);
        int count = reader.U16();
        var zones = new List<ValueTuple<int, int, int, int, int>>(count);
        for (int index = 0; index < count; index++)
        {
            int kind = reader.U8();
            int x1 = reader.I32();
            int y1 = reader.I32();
            int x2 = reader.I32();
            int y2 = reader.I32();
            zones.Add(new ValueTuple<int, int, int, int, int>(kind, x1, y1, x2, y2));
        }

        ((IWorldGatherReceiveOwner)_worldReceiveOwner).ReceiveGatherSpots(zones);
    }
}

}
