namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class AddonUiHost
{
    internal static bool IsMobilePassiveLauncher(object owner)
    {
        foreach (AddonUiHost host in hosts)
            if (host!=null && object.ReferenceEquals(host.launcherOwner,owner)) return true;
        return false;
    }
}
}
