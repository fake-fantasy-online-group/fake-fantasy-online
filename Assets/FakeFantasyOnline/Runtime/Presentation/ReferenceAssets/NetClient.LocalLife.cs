namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class NetClient
{
    // Local branch of original Dispatch(0x801a); false leaves remote routing to its owner.
    internal static bool HandleLocalPlayerLifeForIndependentRuntime(int targetId, bool alive, byte flags)
    {
        if (targetId != 0 && targetId != SelfEntityId)
            return false;
        if (alive)
        {
            if (PlayerController.I != null)
                PlayerController.I.Revive();
            else
                _pendSelfAlive = 1;
            ReviveDlg.Hide();
            ArenaDeathDlg.Close();
            // Official 1.5.8 Dispatch case 0x801a local revive tail: ReviveDlg.Hide
            // (0x06000c7e, arm64 0x30f38c / x86_64 0x321f60), then ArenaDeathDlg.Close
            // (0x06000033, arm64 0x21552c / x86_64 0x221700), then SysOptionDlg.Hide
            // (0x06000e28, arm64 0x346ce0 / x86_64 0x359590) as the method tail call.
            SysOptionDlg.Hide();
        }
        else
        {
            if (PlayerController.I != null)
                PlayerController.I.PlayDie();
            else
            {
                _pendSelfAlive = 0;
                _pendReviveFlags = flags;
            }
            if (!CustomMaps.IsCustom(GameBootstrap.CurMapId))
                ReviveDlg.Open(flags);
        }
        return true;
    }
}
}
