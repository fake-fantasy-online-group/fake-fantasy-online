using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public static partial class PrivateServerTradeCodec
{
    internal static void ReadStateInto(byte[] payload, ITradeStateReceiptOwner owner)
    {
        var reader = new NetClient.Buf(payload);
        owner.StoreTradeFlag(TradeReceiptFlag.MyLocked, reader.U8() != 0);
        owner.StoreTradeFlag(TradeReceiptFlag.OtherLocked, reader.U8() != 0);
        owner.StoreTradeFlag(TradeReceiptFlag.MyConfirmed, reader.U8() != 0);
        owner.StoreTradeFlag(TradeReceiptFlag.OtherConfirmed, reader.U8() != 0);
        owner.StoreTradeMoney(true, reader.I64());
        owner.StoreTradeMoney(false, reader.I64());
        ReadItemsInto(reader, owner, true);
        ReadItemsInto(reader, owner, false);

        owner.ClearTradeItemInfo(true);
        owner.ClearTradeItemInfo(false);
        ReadItemInfoInto(reader, owner, true);
        ReadItemInfoInto(reader, owner, false);

        owner.ClearTradePets(true);
        owner.ClearTradePets(false);
        ReadPetsInto(reader, owner, true);
        ReadPetsInto(reader, owner, false);
        if (reader.Remaining > 0) ReadPetPointsInto(reader, owner, true);
        if (reader.Remaining > 0) ReadPetPointsInto(reader, owner, false);
    }

    private static void ReadItemsInto(NetClient.Buf reader, ITradeStateReceiptOwner owner, bool ownSide)
    {
        int count = reader.U8();
        owner.ClearTradeItems(ownSide);
        for (int i = 0; i < count; i++)
        {
            int itemId = reader.I32();
            int amount = reader.I32();
            string name = reader.Str();
            owner.AddTradeItem(ownSide, itemId, amount, name);
        }
    }

    private static void ReadItemInfoInto(NetClient.Buf reader, ITradeStateReceiptOwner owner, bool ownSide)
    {
        if (reader.Remaining < 1) return;
        int count = reader.U8();
        for (int i = 0; i < count && reader.Remaining > 0; i++)
        {
            string text = reader.Str();
            byte quality = (byte)reader.U8();
            owner.AddTradeItemInfo(ownSide, text, quality);
        }
    }

    private static void ReadPetsInto(NetClient.Buf reader, ITradeStateReceiptOwner owner, bool ownSide)
    {
        if (reader.Remaining < 1) return;
        int count = reader.U8();
        for (int i = 0; i < count && reader.Remaining > 2; i++)
        {
            int index = reader.U16();
            PetBrief pet = NetClient.ReadPetBrief(reader);
            owner.StoreTradePet(ownSide, index, pet);
        }
    }

    private static void ReadPetPointsInto(NetClient.Buf reader, ITradeStateReceiptOwner owner, bool ownSide)
    {
        int count = reader.U8();
        for (int i = 0; i < count; i++)
        {
            if (reader.Remaining < 18) return;
            int index = reader.U16();
            int aiUsed = reader.I32();
            int aiCapacity = reader.I32();
            int shengUsed = reader.I32();
            int shengCapacity = reader.I32();
            owner.StoreTradePetPoints(ownSide, index, aiUsed, aiCapacity, shengUsed, shengCapacity);
        }
    }
}
}
