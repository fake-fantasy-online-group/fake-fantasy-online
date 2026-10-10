using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
public interface INewbieTipReceiveContext : ISkillBookReceiveOwner
{
    void SetTipCharacterName(string name);
    void SeedNewbieTips(List<int> ids);
    // Called after the gateway has applied and published this receipt through the skill owner.
    // This callback evaluates the tip only; it must not replace the already received roots.
    void ReceiveNewbieSkillBook(WorldSkillBookState skills);
    void ReceiveNewbiePetSkills(WorldPlayerPetStatusState pet);
}
}
