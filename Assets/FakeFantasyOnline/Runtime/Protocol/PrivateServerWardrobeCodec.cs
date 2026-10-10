using System;
using System.Collections.Generic;
using System.IO;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Exact wardrobe writers and Win05.SetWardrobe replacement reader.
/// </summary>
internal static class PrivateServerWardrobeCodec
{
    public static bool TryDecode(byte[] payload, out WorldWardrobeState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            WireReader reader = new WireReader(payload);
            int capacity = reader.ReadInt32();
            int count = reader.ReadUInt16();
            List<WorldWardrobeItemState> items =
                new List<WorldWardrobeItemState>(count);
            for (int index = 0; index < count; index++)
            {
                int itemId = reader.ReadInt32();
                byte category = reader.ReadByte();
                byte worn = reader.ReadByte();
                byte blocked = reader.ReadByte();
                string name = reader.ReadString();
                string description = reader.ReadString();
                items.Add(new WorldWardrobeItemState(
                    itemId,
                    category,
                    worn != 0,
                    blocked != 0,
                    name,
                    description));
            }

            state = new WorldWardrobeState(capacity, items);
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException ||
            exception is EndOfStreamException ||
            exception is InvalidDataException ||
            exception is OverflowException)
        {
            state = null;
            return false;
        }
    }

    public static bool TryEncodeStore(byte bagTab, int bagSlot, out byte[] payload)
    {
        payload = null;
        if (bagTab > 3 || bagSlot < 0 || bagSlot >= 100)
        {
            return false;
        }

        WireWriter writer = new WireWriter();
        writer.WriteByte(bagTab);
        writer.WriteInt32(bagSlot);
        payload = writer.ToArray();
        return true;
    }

    public static bool TryEncodeItem(int itemId, out byte[] payload)
    {
        payload = null;
        if (itemId <= 0)
        {
            return false;
        }

        WireWriter writer = new WireWriter();
        writer.WriteInt32(itemId);
        payload = writer.ToArray();
        return true;
    }

    public static bool TryEncodeMove(
        int itemId,
        int categoryIndex,
        out byte[] payload)
    {
        payload = null;
        if (itemId <= 0 || categoryIndex < 0 || categoryIndex >= 100)
        {
            return false;
        }

        WireWriter writer = new WireWriter();
        writer.WriteInt32(itemId);
        writer.WriteInt32(categoryIndex);
        payload = writer.ToArray();
        return true;
    }
}

}
