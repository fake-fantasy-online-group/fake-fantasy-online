using System.IO;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Official NetClient.Dispatch 0x803a root: mapId:I32, x:I32, y:I32.
/// </summary>
public static class PrivateServerTeleportCodec
{
    public const int PayloadSize = sizeof(int) * 3;

    public static bool TryDecode(byte[] payload, out WorldTeleportState state)
    {
        state = null;
        if (payload == null || payload.Length < PayloadSize)
        {
            return false;
        }

        try
        {
            WireReader reader = new WireReader(payload);
            int mapId = reader.ReadInt32();
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            state = new WorldTeleportState(0, mapId, x, y);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }
}

}
