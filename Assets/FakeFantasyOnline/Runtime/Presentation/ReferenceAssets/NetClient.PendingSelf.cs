namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class NetClient
{
    private static byte _pendReviveFlags;

    internal static bool ApplyPendingSelfStateForIndependentRuntime(out bool alive, out byte flags)
    {
        alive = false;
        flags = 0;
        if (_pendSelfAlive < 0 || PlayerController.I == null) return false;
        alive = _pendSelfAlive != 0;
        flags = _pendReviveFlags;
        ApplyPendingSelfState();
        return true;
    }

    public static void ApplyPendingSelfState()
    {
        if (_pendSelfAlive < 0 || PlayerController.I == null)
            return;
        int alive = _pendSelfAlive;
        byte flags = _pendReviveFlags;
        _pendSelfAlive = -1;
        _pendReviveFlags = 0;
        if (alive != 0)
        {
            PlayerController.I.Revive();
            // Official 1.5.8 NetClient.ApplyPendingSelfState (0x0600071c, arm64
            // 0x26cac0 / x86_64 0x27c100) ends the deferred replay with ReviveDlg.Hide
            // (0x06000c7e) only: ArenaDeathDlg.Close (0x06000033) and SysOptionDlg.Hide
            // (0x06000e28) are tails of the immediate 0x801a local revive branch in
            // NetClient.LocalLife.cs, which already ran them before the controller
            // existed, so they are intentionally not repeated here.
            ReviveDlg.Hide();
        }
        else
        {
            PlayerController.I.PlayDie();
            if (!CustomMaps.IsCustom(GameBootstrap.CurMapId))
                ReviveDlg.Open(flags);
        }
    }
}
}
