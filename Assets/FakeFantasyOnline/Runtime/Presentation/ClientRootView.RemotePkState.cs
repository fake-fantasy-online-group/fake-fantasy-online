using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IRemotePkStateReceiveContext
{
    // Official 1.5.8 Dispatch case 0x8053 (arm64 0x24d5f8 / x86_64 0x25b11e) reads the entity
    // id and one byte, compares the id with NetClient.SelfEntityId (the int32 static at +0x7c,
    // field token 0x040009ee) and takes two different arms:
    //   equal   -> arm64 strb w19,[x8,#0xe8] at 0x24d66c / x86_64 movb %bl,0xe8(%rax) at
    //              0x25b18e, i.e. the local CharData.pkState store, then leave the case.
    //              SysConfigDlg.SyncPkFromServer is not reached on this arm.
    //   unequal -> tail call CombatWorld.SetPlayerPkState(int, byte) (0x06000134, arm64
    //              0x23f5b4 / x86_64 0x24d310).
    bool IRemotePkStateReceiveContext.ReceiveRemotePkState(int entityId, byte pkState)
    {
        if (entityId != NetClient.SelfEntityId)
        {
            return false;
        }

        CharData.SetLocalPkStateFromServer(pkState);
        return true;
    }
}
}
