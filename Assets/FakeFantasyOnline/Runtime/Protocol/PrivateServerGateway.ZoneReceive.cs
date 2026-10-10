using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    // Official 1.5.8 NetClient.Dispatch (0600077f), bounded 0x805f branch.
    private static void ReceiveZoneOverlay(byte[] payload, IZoneOverlayReceiveContext context)
    {
        var reader = new NetClient.Buf(payload);
        context.EnsureArenaRoster();
        bool active = reader.U8() != 0;
        int cx = reader.I32();
        int cy = reader.I32();
        int radius = reader.I32();
        int nextCx = reader.I32();
        int nextCy = reader.I32();
        int nextRadius = reader.I32();
        byte phase = (byte)reader.U8();
        int phaseSeconds = reader.I32();
        int ring = reader.I16();
        int rings = reader.I16();
        int alive = reader.I16();
        bool outside = reader.U8() != 0;
        int damagePerSecond = reader.I32();

        bool canSurrender = false;
        int cards = 0;
        int deaths = 0;
        string reason = "";
        if (reader.Remaining >= 3)
        {
            canSurrender = reader.U8() != 0;
            cards = reader.U8();
            deaths = reader.U8();
            if (reader.Remaining > 0) reason = reader.Str();
        }
        int radiusBonus = reader.Remaining >= 4 ? reader.I32() : 0;
        context.ApplyZoneOverlay(active, cx, cy, radius, nextCx, nextCy, nextRadius,
            phase, phaseSeconds, ring, rings, alive, outside, damagePerSecond,
            canSurrender, cards, deaths, reason, radiusBonus);
    }
}
}
