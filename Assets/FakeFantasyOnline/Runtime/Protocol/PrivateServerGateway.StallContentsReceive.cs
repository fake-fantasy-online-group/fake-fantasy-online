using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private static void ReceiveStallContents(byte[] payload, IStallContentsReceiveContext context)
    {
        var reader = new NetClient.Buf(payload);
        int ownerId = reader.I32();
        string ownerName = reader.Str();
        byte type = (byte)reader.U8();
        string shopName = reader.Str();
        int count = reader.U16();
        IStallContentsReceiveBuffer contents = context.CreateStallContentsBuffer(count);
        for (int i = 0; i < count; i++)
        {
            int itemId = reader.I32();
            int quantity = reader.I32();
            long price = reader.I64();
            string name = reader.Str();
            string description = reader.Str();
            contents.AddRow(itemId, quantity, price, name, description);
        }
        int salesCount = reader.U16();
        contents.BeginSales(salesCount);
        for (int i = 0; i < salesCount; i++)
        {
            long time = reader.I64();
            string item = reader.Str();
            int quantity = reader.I32();
            long money = reader.I64();
            contents.AddSale(time, item, quantity, money);
        }
        var blocked = NetClient.ReadBlockedTail(reader);
        for (int i = 0; i < blocked.Count && i < count; i++) contents.SetBlocked(i, blocked[i]);
        if (reader.Remaining >= 2)
        {
            int petCount = reader.U16();
            for (int i = 0; i < petCount && reader.Remaining >= 2; i++)
            {
                int index = reader.U16();
                PetBrief pet = NetClient.ReadPetBrief(reader);
                if (index < count) contents.SetPet(index, pet);
            }
        }
        if (reader.Remaining >= 2)
        {
            int qualityCount = reader.U16();
            for (int i = 0; i < qualityCount && reader.Remaining > 0; i++)
            {
                byte quality = (byte)reader.U8();
                if (i < count) contents.SetQuality(i, quality);
            }
        }
        if (reader.Remaining >= 2)
        {
            int pointsCount = reader.U16();
            for (int i = 0; i < pointsCount; i++)
            {
                if (reader.Remaining < 18) break;
                int index = reader.U16();
                int aiUsed = reader.I32();
                int aiCapacity = reader.I32();
                int shengUsed = reader.I32();
                int shengCapacity = reader.I32();
                if (index < count && contents.HasPet(index))
                    contents.SetPetPoints(index, aiUsed, aiCapacity, shengUsed, shengCapacity);
            }
        }
        contents.Commit(ownerId, ownerName, type, shopName);
    }
}
}
