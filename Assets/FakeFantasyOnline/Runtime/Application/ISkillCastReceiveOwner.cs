namespace FakeFantasyOnline.Application
{

/// <summary>
/// Host-thread cast receipt capability. The selected owner also supplies the existing scene
/// synchronization boundary, so earlier received scene mutations precede this cast.
/// </summary>
public interface ISkillCastReceiveOwner : IWorldReceiveSceneContext
{
    // Presentation applies the native case synchronously without a ready-window gate. The
    // Boolean remains a host capability result; native actor/phase decisions stay in the case.
    bool TryReceiveSkillCast(WorldSkillCastState state);
}

/// <summary>Optional injected projection, resolved at receipt rather than cached by Protocol.</summary>
public interface ISkillCastReceiveOwnerProvider
{
    ISkillCastReceiveOwner SkillCastOwnerAtReceipt { get; }
}

}
