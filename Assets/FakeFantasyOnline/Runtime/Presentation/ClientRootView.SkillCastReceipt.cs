using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation
{

public sealed partial class ClientRootView : ISkillCastReceiveOwner
{
    bool ISkillCastReceiveOwner.TryReceiveSkillCast(WorldSkillCastState state)
    {
        ReferenceAssets.ReferenceWorldReceiveOwner.ReceiveSkillCastForIndependentRuntime(state);
        return true;
    }
}

}
