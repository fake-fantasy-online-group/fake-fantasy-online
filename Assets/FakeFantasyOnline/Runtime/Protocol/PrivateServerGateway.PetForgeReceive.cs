using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private static void ReceivePetForge(byte[] payload, IPetForgeReceiveContext context)
    {
        var reader = new NetClient.Buf(payload);
        int kind = reader.U8();
        int petTab = reader.U8();
        int petSlot = reader.I32();
        int itemTab = reader.U8();
        int itemSlot = reader.I32();
        int itemId = reader.I32();
        string petName = reader.Str();
        string species = reader.Str();
        int petLevel = reader.I32();
        bool ok = reader.U8() != 0;
        string tip = reader.Str();
        string confirm = reader.Str();
        int fu = reader.I32();
        int fc = reader.I32();
        int lu = reader.I32();
        int lc = reader.I32();
        int count = reader.U8();
        var skills = new List<(int, string, int, int, int, byte)>(count);
        for (int i = 0; i < count; i++)
            skills.Add((reader.I32(), reader.Str(), reader.I32(),
                reader.I32(), reader.I32(), (byte)reader.U8()));
        int bookSkillId = reader.I32();
        int bookSkillLv = reader.I32();
        string bookSkillName = reader.Str();
        context.ReceivePetForgeInfo(kind, petTab, petSlot, itemTab, itemSlot, itemId,
            petName, species, petLevel, ok, tip, confirm, fu, fc, lu, lc,
            skills, bookSkillId, bookSkillLv, bookSkillName);

        int petTier = reader.Remaining > 0 ? reader.U8() : 0;
        string pet2Name = "";
        string pet2Species = "";
        int pet2Level = 0;
        int pet2Tier = 0;
        if (reader.Remaining > 0)
        {
            pet2Name = reader.Str();
            pet2Species = reader.Str();
            pet2Level = reader.I32();
            pet2Tier = reader.U8();
        }
        var odds = new List<(int, int)>();
        if (reader.Remaining > 0)
        {
            int oddsCount = reader.U8();
            for (int i = 0; i < oddsCount && reader.Remaining >= 2; i++)
                odds.Add((reader.U8(), reader.U8()));
        }
        int reset = reader.Remaining > 0 ? reader.U8() : 0;
        context.ReceivePetFuseInfo(petTier, pet2Name, pet2Species, pet2Level, pet2Tier, odds, reset);
    }
}
}
