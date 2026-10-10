using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : INewbieTipReceiveContext,
    ISkillBookStreamReceiveOwner, INewbieSkillBookObservationContext
{
    void INewbieTipReceiveContext.SetTipCharacterName(string name) => CharData.name = name;
    void INewbieTipReceiveContext.SeedNewbieTips(List<int> ids) => NewbieTipBox.SeedServer(ids);

    void ISkillBookStreamReceiveOwner.ReceiveSkillBook(ISkillBookReader reader,
        out WorldSkillBookObservation observation) =>
        CharData.ReceiveSkillsForIndependentRuntime(reader, out observation);

    void ISkillBookStreamReceiveOwner.ReceiveNewbieSkillBookTip(WorldSkillBookObservation observation)
    {
        if (observation.HasValue && observation.SkillCount > 0) NewbieTipBox.Push(7);
    }

    void INewbieSkillBookObservationContext.ReceiveNewbieSkillBook(WorldSkillBookObservation observation)
    {
        if (observation.SkillCount > 0) NewbieTipBox.Push(7);
    }

    WorldSkillBookState ISkillBookReceiveOwner.ReceiveSkillBook(WorldSkillBookReceipt receipt) =>
        CharData.ReceiveSkillsForIndependentRuntime(receipt);

    void INewbieTipReceiveContext.ReceiveNewbieSkillBook(WorldSkillBookState skills)
    {
        if (skills.Skills.Count > 0) NewbieTipBox.Push(7);
    }

    void INewbieTipReceiveContext.ReceiveNewbiePetSkills(WorldPlayerPetStatusState pet)
    {
        CharData.BindPetSkillsForIndependentRuntime(pet);
        foreach (WorldPlayerPetSkillState skill in pet.Skills)
            if (skill.Level > 0)
            {
                NewbieTipBox.Push(51);
                break;
            }
        foreach (WorldPlayerPetSkillState skill in pet.Skills)
            if (skill.Id == 13002 && skill.Level >= 1)
            {
                NewbieTipBox.Push(52);
                break;
            }
    }
}
}
