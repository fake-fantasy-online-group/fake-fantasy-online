using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IPetForgeReceiveContext
{
    void IPetForgeReceiveContext.ReceivePetForgeInfo(int kind, int petTab, int petSlot, int itemTab, int itemSlot,
        int itemId, string petName, string species, int petLevel, bool ok, string tip,
        string confirm, int fu, int fc, int lu, int lc,
        List<(int, string, int, int, int, byte)> skills, int bookSkillId, int bookSkillLv, string bookSkillName) =>
        PetForgeDlg.OnInfo(kind, petTab, petSlot, itemTab, itemSlot, itemId, petName, species,
            petLevel, ok, tip, confirm, fu, fc, lu, lc, skills, bookSkillId, bookSkillLv, bookSkillName);

    void IPetForgeReceiveContext.ReceivePetFuseInfo(int petTier, string pet2Name, string pet2Species,
        int pet2Level, int pet2Tier, List<(int, int)> odds, int reset) =>
        PetForgeDlg.OnFuseInfo(petTier, pet2Name, pet2Species, pet2Level, pet2Tier, odds, reset);
}
}
