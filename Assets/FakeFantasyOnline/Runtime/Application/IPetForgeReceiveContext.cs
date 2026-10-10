using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
public interface IPetForgeReceiveContext
{
    void ReceivePetForgeInfo(int kind, int petTab, int petSlot, int itemTab, int itemSlot,
        int itemId, string petName, string species, int petLevel, bool ok, string tip,
        string confirm, int fu, int fc, int lu, int lc,
        List<(int, string, int, int, int, byte)> skills, int bookSkillId, int bookSkillLv, string bookSkillName);
    void ReceivePetFuseInfo(int petTier, string pet2Name, string pet2Species,
        int pet2Level, int pet2Tier, List<(int, int)> odds, int reset);
}
}
