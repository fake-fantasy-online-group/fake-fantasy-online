using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class WinZ
{
    internal static bool AnyLayoutBlockingWindow
    {
        get
        {
            int frame=Time.frameCount;
            foreach (var pair in reg)
                if (unchecked(frame-pair.Value.frame)<=2 && !AddonUiHost.IsMobilePassiveLauncher(pair.Key)) return true;
            return false;
        }
    }

}
}
