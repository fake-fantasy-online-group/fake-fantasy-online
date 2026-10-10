using System;
using System.IO;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Strict codecs for the reviewed ground-item spawn, removal and pickup roots.
/// </summary>
public static class PrivateServerGroundItemCodec
{
    public const int PickupPayloadSize = sizeof(int);
    public const int RemovalPayloadSize = sizeof(int);

    public static bool TryDecodeSpawn(byte[] payload, out WorldGroundItemState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            WireReader reader = new WireReader(payload);
            int groundId = reader.ReadInt32();
            int itemId = reader.ReadInt32();
            int count = reader.ReadInt32();
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            string displayName = reader.ReadString();
            byte owned = reader.ReadByte();
            int lockMilliseconds = reader.ReadInt32();
            byte kind = reader.ReadByte();
            byte quality = 0;
            if (reader.Remaining == 1)
            {
                quality = reader.ReadByte();
            }
            else if (reader.Remaining != 0)
            {
                return false;
            }

            reader.EnsureFinished();
            if (groundId <= 0 || itemId <= 0 || count <= 0 || owned > 1 ||
                lockMilliseconds < 0)
            {
                return false;
            }

            state = new WorldGroundItemState(
                groundId,
                itemId,
                count,
                x,
                y,
                displayName,
                owned != 0,
                lockMilliseconds,
                kind,
                quality);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException ||
            exception is ArgumentException ||
            exception is OverflowException)
        {
            return false;
        }
    }

    public static bool TryDecodeRemoval(byte[] payload, out int groundId)
    {
        groundId = 0;
        if (payload == null || payload.Length != RemovalPayloadSize)
        {
            return false;
        }

        try
        {
            WireReader reader = new WireReader(payload);
            groundId = reader.ReadInt32();
            reader.EnsureFinished();
            return groundId > 0;
        }
        catch (Exception exception) when (
            exception is IOException ||
            exception is ArgumentException)
        {
            groundId = 0;
            return false;
        }
    }

    public static bool TryEncodePickup(int groundId, out byte[] payload)
    {
        payload = null;
        if (groundId <= 0)
        {
            return false;
        }

        WireWriter writer = new WireWriter();
        writer.WriteInt32(groundId);
        payload = writer.ToArray();
        return payload.Length == PickupPayloadSize;
    }
}

}
