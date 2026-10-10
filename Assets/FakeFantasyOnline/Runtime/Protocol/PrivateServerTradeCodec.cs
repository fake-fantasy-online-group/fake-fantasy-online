using System;
using System.Collections.Generic;
using System.IO;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Official trade open/state/end readers including the 1.5.8 pet and potential-point tails.
/// </summary>
public static partial class PrivateServerTradeCodec
{
    public static bool TryDecodeOpen(byte[] payload, out WorldActiveTradeState state)
    {
        state = null;
        try
        {
            DecodeOpen(payload, out state);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) || exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeOpen(byte[] payload, out WorldActiveTradeState state)
    {
        var reader = new NetClient.Buf(payload);
        int otherEntityId = reader.I32();
        string otherName = reader.Str();

        state = new WorldActiveTradeState(
            true,
            otherEntityId,
            otherName,
            false,
            false,
            false,
            false,
            0,
            0,
            Array.Empty<WorldTradeItemState>(),
            Array.Empty<WorldTradeItemState>(),
            Array.Empty<WorldTradeItemInfoState>(),
            Array.Empty<WorldTradeItemInfoState>());

    }

    public static bool TryDecodeState(
        byte[] payload,
        WorldActiveTradeState current,
        out WorldActiveTradeState state)
    {
        state = null;
        if (payload == null) return false;
        try
        {
            DecodeState(payload, current, out state);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) || exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeState(
        byte[] payload,
        WorldActiveTradeState current,
        out WorldActiveTradeState state)
    {
        var reader = new NetClient.Buf(payload);
        bool myLocked = reader.U8() != 0;
        bool otherLocked = reader.U8() != 0;
        bool myConfirmed = reader.U8() != 0;
        bool otherConfirmed = reader.U8() != 0;
        long myMoney = reader.I64();
        long otherMoney = reader.I64();
        IReadOnlyList<WorldTradeItemState> myItems = ReadItems(reader);
        IReadOnlyList<WorldTradeItemState> otherItems = ReadItems(reader);
        IReadOnlyList<WorldTradeItemInfoState> myInfo =
            reader.Remaining > 0
                ? ReadItemInfo(reader)
                : Array.Empty<WorldTradeItemInfoState>();
        IReadOnlyList<WorldTradeItemInfoState> otherInfo =
            reader.Remaining > 0
                ? ReadItemInfo(reader)
                : Array.Empty<WorldTradeItemInfoState>();
        Dictionary<int, WorldTradePetState> myPets = ReadPets(reader);
        Dictionary<int, WorldTradePetState> otherPets = ReadPets(reader);
        if (reader.Remaining > 0) ReadPetPoints(reader, myPets);
        if (reader.Remaining > 0) ReadPetPoints(reader, otherPets);
        // Dispatch does not assert exhaustion after the optional tails. ReadTradePp
        // returns on a short record, retaining the complete entries already decoded.

        WorldActiveTradeState previous = current ?? WorldActiveTradeState.Empty;
        state = new WorldActiveTradeState(
            previous.Active,
            previous.OtherEntityId,
            previous.OtherName,
            myLocked,
            otherLocked,
            myConfirmed,
            otherConfirmed,
            myMoney,
            otherMoney,
            myItems,
            otherItems,
            myInfo,
            otherInfo,
            myPets,
            otherPets);

    }

    public static bool TryDecodeEnd(byte[] payload, out WorldTradeEndState state)
    {
        state = null;
        try
        {
            DecodeEnd(payload, out state);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) || exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeEnd(byte[] payload, out WorldTradeEndState state)
    {
        var reader = new NetClient.Buf(payload);
        byte colorSelector = (byte)reader.U8();
        string text = reader.Str();
        state = new WorldTradeEndState(colorSelector, text);

    }

    /// <summary>
    /// Byte-array form of the official offer writer for the byte-level Editor audit only. The
    /// writer itself is the one <see cref="NetClient.TradeOffer"/> hands to NetClient.Send:
    /// <see cref="NetClient.WriteOffer"/>, official 0x0600084d. The send path never goes through
    /// this adapter - it runs the writer inside NetClient.Send, after the connected test.
    /// </summary>
    public static byte[] EncodeOffer(
        long money,
        List<(byte Tab, int Slot, int Count)> items)
    {
        NetClient.Buf writer = new NetClient.Buf();
        NetClient.WriteOffer(writer, money, items);
        return writer.ToArray();
    }

    /// <summary>
    /// Byte-array form of the official lock writer 0x0600084f for the byte-level Editor audit;
    /// the send path uses <see cref="NetClient.WriteLock"/> inside NetClient.Send.
    /// </summary>
    public static byte[] EncodeLock(bool locked)
    {
        NetClient.Buf writer = new NetClient.Buf();
        NetClient.WriteLock(writer, locked);
        return writer.ToArray();
    }

    private static IReadOnlyList<WorldTradeItemState> ReadItems(NetClient.Buf reader)
    {
        int count = reader.U8();
        if (count == 0)
        {
            return Array.Empty<WorldTradeItemState>();
        }

        List<WorldTradeItemState> rows = new List<WorldTradeItemState>(count);
        for (int index = 0; index < count; index++)
        {
            rows.Add(
                new WorldTradeItemState(
                    reader.I32(),
                    reader.I32(),
                    reader.Str()));
        }

        return rows;
    }

    private static IReadOnlyList<WorldTradeItemInfoState> ReadItemInfo(NetClient.Buf reader)
    {
        int count = reader.U8();
        if (count == 0)
        {
            return Array.Empty<WorldTradeItemInfoState>();
        }

        List<WorldTradeItemInfoState> rows =
            new List<WorldTradeItemInfoState>(count);
        for (int index = 0; index < count && reader.Remaining > 0; index++)
        {
            rows.Add(new WorldTradeItemInfoState(reader.Str(), (byte)reader.U8()));
        }

        return rows;
    }

    private static Dictionary<int, WorldTradePetState> ReadPets(NetClient.Buf reader)
    {
        var pets = new Dictionary<int, WorldTradePetState>();
        if (reader.Remaining < 1) return pets;
        int count = reader.U8();
        // Native Dispatch checks Remaining > 2 before U16 index + ReadPetBrief.
        for (int i = 0; i < count && reader.Remaining > 2; i++)
        {
            int index = reader.U16();
            pets[index] = new WorldTradePetState(WorldInventoryPetState.FromPetBrief(NetClient.ReadPetBrief(reader)));
        }
        return pets;
    }

    private static void ReadPetPoints(NetClient.Buf reader, Dictionary<int, WorldTradePetState> pets)
    {
        // Official NetClient.ReadTradePp (0x06000713): U8 count, 18-byte records.
        // Read all four integers even when the index has no pet; only existing entries update.
        int count = reader.U8();
        for (int i = 0; i < count; i++)
        {
            if (reader.Remaining < 18) return;
            int index = reader.U16();
            int aiUsed = reader.I32();
            int aiCapacity = reader.I32();
            int shengUsed = reader.I32();
            int shengCapacity = reader.I32();
            if (pets.TryGetValue(index, out WorldTradePetState pet))
                pets[index] = new WorldTradePetState(pet.Pet, aiUsed, aiCapacity,
                    shengUsed, shengCapacity);
        }
    }

    private static bool IsDecodeFailure(Exception exception)
    {
        return exception is EndOfStreamException ||
               exception is InvalidDataException ||
               exception is ArgumentException ||
               exception is OverflowException;
    }
}

}
